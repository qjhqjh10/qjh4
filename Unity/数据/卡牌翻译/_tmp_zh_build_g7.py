# -*- coding: utf-8 -*-
"""g7 卡牌中文翻译生成器 (Tau 钛帝国 + Tyranids 泰伦虫族 173 张)
规则: 术语表(官方中文规则书)命中照用; 效果文本规则文体; [icon] 标记原样保留
"""
import json, io, sys, os

SRC = r"d:/2/Warpforge_tools/scripts/_tmp_slices/g7.json"
OUT = r"d:/2/Warpforge_tools/scripts/_tmp_zhcards_g7.json"
TERMS = r"d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json"

# (英文名, 中文名, 中文效果) —— 与源文件顺序一一对应, 共 173 条
T = [
("Broadside Battlesuit", "宽面甲", "护甲 1。远射。伴生 2：导弹无人机"),
("Coldstar Battlesuit", "冷星甲", "当你部署部队时，给予其 +2 生命。\n伴生 2：标记无人机"),
("Crisis Battlesuit", "危机甲", "飞行"),
("Crisis Bodyguard", "危机护卫", "护盾。先锋"),
("Enforcer Battlesuit", "执法者甲", "先锋。相邻单位获得 +1 远程攻击。\n伴生 2：守护无人机"),
("Riptide Battlesuit", "激流甲", "天赋：护盾发生器"),
("Stealth Battlesuit", "隐匿甲", "潜行。天赋：归航信标"),
("Stealth Shas\u2019ui", "隐匿火氏士", "潜行。伴生 2：潜行无人机"),
("Stormsurge Battlesuit", "风暴甲", "天赋：部署锚桩"),
("Grav Inhibitor Drone", "重力抑制无人机", ""),
("Guardian Drone", "守护无人机", "先锋"),
("Gun Drone", "枪无人机", "你的回合开始时，对 1 个随机敌人造成 2 点伤害"),
("Marker Drone", "标记无人机", "飞行。你的回合开始时，给予 1 个随机敌人标记光 1"),
("Missile Drone", "导弹无人机", "飞行。爆裂 2"),
("Sniper Drone", "狙击无人机", "敌人获得标记光时获得远射"),
("Stealth Drone", "潜行无人机", "你的回合结束时，给予 1 个相邻部队潜行"),
("Aunshi Ethereal", "昂希以太", "护盾。你的部队费用 -1。天赋：宁静统合者"),
("Breacher Fire Warrior", "突破者火氏战士", "先锋"),
("Breacher ShasUi", "突破者火氏士", "先锋"),
("Cadre Fireblade", "火刃干部", "你的其他步兵和战斗服部队获得 +2"),
("Darkstrider", "暗行者", "潜行。集结：给予相邻无人机潜行。天赋：结构分析器"),
("Fire Warrior Sniper", "火氏狙击手", ""),
("Fire Warrior", "火氏战士", "远射"),
("Firesight Marksman", "锐眼射手", "潜行。当你部署无人机时，给予其 +1 远程攻击。\n集结：给予 1 个敌方部队标记光 3"),
("Flesh Shaper", "血肉塑形者", "伪装。有敌人死亡时，给予你的单位 +1 [attack]"),
("Honoured Ethereal", "荣耀以太", "护盾。相邻部队获得先锋。天赋：宁静统合者"),
("Kroot Carnivore", "克鲁特食肉者", "集结：若你场上没有其他部队，将 1 张克鲁特食肉者加入你的手牌"),
("Kroot Hound", "克鲁特猎犬", "集结：部署 1 个克鲁特猎犬"),
("Krootox Rampager", "克鲁特狂暴兽", "侧翼。集结：本回合获得 +3"),
("Krootox Rider", "克鲁特骑手", "集结：对 1 个敌人造成 2 点伤害"),
("Lone-spear", "独矛", "集结：若你场上没有其他部队，获得侧翼"),
("Pathfinder", "探路者", "集结：给予 1 个敌方部队标记光 2。\n伴生 2：标记无人机"),
("Strike Team", "打击小队", "伴生：支援炮塔"),
("Trail Shaper", "踪迹塑形者", "集结：将 1 个友方步兵返还你的手牌。其费用降至 1"),
("Vespid Stingwings", "黄蜂族刺翼", "飞行。侧翼。护甲 1"),
("Normal Conditions", "正常条件", ""),
("Electro-Static Interference", "静电干扰", ""),
("Radiation Storm", "辐射风暴", ""),
("Solar Eclipse", "日食", ""),
("Burgeoning Empire", "昌盛帝国", "抽 3 张牌。每有 1 个敌人死亡，费用 -1"),
("Coordinated Engagement", "协同作战", "给予 1 个敌方部队标记光 2。若其已有标记光，改为将其摧毁"),
("Deploy Anchors", "部署锚桩", "给予 1 个友方战斗服或载具 +5 远程攻击"),
("Drone Companion", "无人机伴生", "在你的手牌创建 1 个枪无人机、守护无人机或标记无人机"),
("Dynamic Offensive", "动态攻势", "从你的牌库抽 2 张部队牌。若为战斗服，给予其侧翼"),
("Emergency Dispensation", "紧急豁免", "选择你牌库中的 1 张部队牌。抽取它并使其费用 -3"),
("Ethereal Supreme", "至尊以太", "给予你的督军和相邻单位 +1 生命。在你的手牌创建 1 张宁静统合者的复制"),
("Experimental Drone", "实验无人机", "直至本场战斗结束，给予你部署的所有无人机护盾"),
("For the Greater Good", "为至高善意", "给予 1 个友方单位先锋和护甲 3，直到你的下个回合"),
("Fortified Positions", "坚固阵地", "给予你的所有单位远射"),
("Grisly Feast", "血腥盛宴", "给予你的督军和步兵部队本回合 +1 攻击和 +1 生命"),
("Hero of the Empire", "帝国英雄", "给予 1 个敌方部队标记光 1，或给予 1 个友方单位本回合 +1 远程攻击"),
("Hidden Hunters", "隐匿猎手", "给予你的步兵部队 +1 [Power] 和潜行"),
("Homing Beacon", "归航信标", "临时。部署 1 个危机甲并给予其侧翼"),
("Hunters Instincts", "猎手本能", "每有 1 个受伤的敌人，部署 1 个克鲁特猎犬，并给予其侧翼"),
("Inspired to Greatness", "伟业感召", "给予 1 个友方部队 +2 远程攻击和 +2 生命"),
("Kauyon Tactics", "考恩战术", "给予所有敌方部队 [Markerlight] 标记光 2"),
("Priority Target", "优先目标", "给予 1 个敌人标记光 2"),
("Pulse Onslaught", "脉冲猛攻", "造成 3 点伤害"),
("Razorshark Strike", "剃刀鲨突袭", "对 1 个敌方部队造成 5 点伤害"),
("Recon Sweep", "侦察扫荡", "使 1 个敌人眩晕，并给予其标记光 1"),
("Relentless Fusillade", "无情齐射", "对所有敌人造成 2 点伤害，对每个带标记光的敌人额外造成 2 点伤害"),
("Saviour Protocolst", "救世协议", "直至本场战斗结束，你的无人机费用 -1。选择 1 个无人机并将其加入你的手牌"),
("Sense of Stone", "石之感知", "给予你的部队护甲 1"),
("Serene Unifier", "宁静统合者", "临时。选择 1 道计策并放入你的手牌"),
("Shield Generator", "护盾发生器", "临时。给予 1 个友方战斗服护盾"),
("Storm of Fire", "风暴之火", "给予你的单位本回合 +2 远程攻击"),
("Structural Analyzer", "结构分析器", "给予 1 个敌人标记光 2"),
("Technological Supremacy", "科技霸权", "给予 1 个友方单位 +3 生命。若其为载具或战斗服，给予其远射"),
("Thundering Rampage", "雷霆暴怒", "给予 1 个友方单位本回合 +3。若其为步兵，同时给予其侧翼"),
("Tidewall Droneport", "潮墙无人机港", "部署 1 个守护无人机"),
("Tidewall Gunrig", "潮墙炮架", "对 1 个敌方部队造成 3 点伤害"),
("Valued Sacrifice", "崇高牺牲", "部署 3 个随机无人机并给予其 [Vanguard] 先锋"),
("Watch Tower", "瞭望塔", "给予 1 个敌人标记光 1"),
("Zephyr's Grace", "和风之恩", "给予 1 个友方单位 [Shield] 护盾"),
("DS8 Support Turret", "支援炮塔", "你的回合开始时，对所有敌人造成 2 点伤害"),
("Devilfish", "魔鬼鱼", "护甲 1。友方步兵和无人机获得侧翼"),
("Hammerhead", "锤头鲨", "远射。伴生 2：枪无人机"),
("Longstrike", "长袭者", "集结：给予 1 个敌方部队标记光 3"),
("Piranha", "食人鲳", "你的无人机费用 -1"),
("Razorshark", "剃刀鲨", "造成 2 点伤害"),
("Sky Ray Gunship", "天雷炮艇", "远射"),
("Sun Shark Bomber", "日鲨轰炸机", "集结：给予 1 个敌人标记光 2"),
("Aun'Va", "昂瓦", "至尊以太"),
("Commander O'Maisos", "奥迈索斯指挥官", "护盾。你的回合内拥有飞行。天赋：无人机伴生"),
("Shadowsun", "影阳", "天赋：帝国英雄"),
("War Shaper", "战争塑形者", "猛击：治疗 1。\n天赋：血腥盛宴"),
("Hive Guard", "蜂巢卫兵", "先锋"),
("Crushing Claws", "粉碎之爪", ""),
("Terror of Vardenghast", "瓦登加斯特之恐", "斩杀：治疗 1。天赋：阿尔法战士"),
("Barbgaunt", "倒刺根", "虫群。潮涌 2。爆裂 2"),
("Biovore", "孢子兽", "你的回合结束时，部署 1 个孢子雷"),
("Broodlord", "繁育领主", "本单位触发突触时，效果生效两次"),
("Deathleaper", "死神猎手", "使 1 个随机敌人眩晕"),
("Gargoyle", "石像鬼", "虫群。飞行。潮涌 2。侧翼"),
("Genestealer", "基因窃取者", "你的回合开始时，在你的手牌创建 1 个基因窃取者"),
("Hormagaunt", "荷尔马根", "虫群。潮涌 4"),
("Lictor", "利克特", "潜行"),
("Mucolid Spore", "黏液孢子", "猛击：摧毁本部队"),
("Neurogaunt Nodebeast", "神经节虫巢兽", "突触。虫群。潮涌 1"),
("Neurogaunt", "神经节虫", "突触。虫群。潮涌 2"),
("Neuroloid", "神经虫体", "突触"),
("Pyrovore", "喷焰兽", "有其他部队死亡时，治疗 2"),
("Ravener", "掠夺虫", "集结：对 1 个随机潜行敌人造成 3 点伤害，并使其失去潜行"),
("Ripper Swarm", "撕裂虫群", "虫群。潮涌 3"),
("Spore Mine", "孢子雷", "你的回合开始时，摧毁本单位并对 1 个随机敌人造成 3 点伤害"),
("Stranded Termagant", "搁浅泰玛根", ""),
("Termagant Brood", "泰玛根巢群", "潮涌 3。友方单位触发虫群时，获得 +1 [Attack]"),
("Termagant", "泰玛根", "虫群。潮涌 2"),
("Tyranid Prime", "泰伦首相", "突触。友方单位触发虫群时，给予其 +1 近战攻击和 +1 生命"),
("Tyranid Warrior", "泰伦战士", "突触"),
("Tyrant Guard", "暴君卫队", "先锋。护甲 2"),
("Venomthrope", "毒瘴兽", "摧毁任何被本单位攻击的部队"),
("Von Ryans Leaper", "瑞恩跳跃者", "虫群。潮涌 1。潜行"),
("Zoanthrope", "兽人脑", "突触。友方单位触发突触时，对 1 个随机敌人造成 1-3 点伤害"),
("Carnifex", "屠夫兽", "集结：选择并获得一项增益（+2 近战、+2 远程或护甲 1）"),
("Exocrine", "外泌兽", "给予 1 个敌方部队及相邻部队脆弱 2"),
("Harpy", "鹰身女妖", "飞行。敌人攻击时，部署 1 个孢子雷"),
("Haruspex", "内脏占卜者", "猛击：治疗 4"),
("Hive Crone", "蜂巢老妪", "对 1 个敌人造成 3 点伤害"),
("Hive Tyrant", "蜂巢暴君", "突触。天赋：突触律令"),
("Maleceptor", "灵能受体兽", "给予 1 个随机友方单位护盾"),
("Mawloc", "巨口虫", "猛击：将此牌返回你的牌库并使其费用 -3"),
("Neurotyrant", "神经元暴君", "每当你打出非临时战术时，在你的手牌创建 1 张其临时复制"),
("Norn Emissary", "诺恩使者", "每当友方单位触发突触时，费用 -2"),
("Psychophage", "噬魂兽", "集结：获得 +2、+2，且每有 1 个受伤的敌人获得 +1 生命"),
("Screamer-Killer", "尖啸杀手", "护甲。爆裂"),
("Sporocyst", "孢子囊", "部署 1 个孢子雷"),
("Tyrannocyte", "泰伦孢囊", "飞行。天赋：空中播种"),
("Tyrannofex", "泰伦巨兽", "猛击：对 1 个随机敌人造成 1 点伤害，共 8 次"),
("Winged Tyrant", "有翼暴君", "集结：本回合你的下一个战术费用为 0"),
("Acid Rain", "酸雨", ""),
("Blazing Biomass", "炽燃生物质", ""),
("Normal Conditions", "正常条件", ""),
("Sweeping Infestation", "蔓延虫灾", ""),
("Adrenal Surge", "肾上腺激涌", "给予 1 个友方单位本回合 +2 近战和 +2 远程攻击"),
("Aerial Seeding", "空中播种", "选择 1 个 2 费利维坦部队并将其部署"),
("Alpha Warrior", "阿尔法战士", "临时。本回合你的督军获得飞行和 +2 近战攻击，每有 1 个友方部队再额外 +1"),
("Ambush Predator", "伏击掠食者", "给予 1 个友方单位伪装"),
("Apex Predator", "顶级掠食者", "给予 1 个友方单位本回合无敌和 +3 攻击"),
("Armoured Exoskeleton", "装甲外骨骼", ""),
("Augmented Ferocity", "强化狂暴", "治疗友方单位 3 点，并给予其本回合 +2 近战攻击"),
("Bestial Rage", "兽性狂怒", "给予所有友方部队 +4"),
("Bounding Leap", "腾跃", "给予 1 个友方单位本回合爆裂 2"),
("Brood Progenitor", "巢群繁育者", "在你的手牌创建 1 张泰玛根。本回合你的下一个部队费用 -1"),
("Darkened Skies", "昏暗天空", "对所有敌人造成 1 点伤害"),
("Devourer Cannon", "吞噬者炮", ""),
("Digestion Pool", "消化池", "本回合你的下一个部队费用 -2"),
("Encircle the Prey", "合围猎物", "给予 1 个友方部队潜行"),
("Enhanced Organism", "强化生物体", "给予 1 个友方单位 +3 生命"),
("Feeding Frenzy", "进食狂潮", "对 1 个受伤的敌人造成 2 点伤害。若目标死亡，给予你的所有单位 +1 生命"),
("Hardened Biology", "坚韧躯壳", "给予 1 个友方部队护甲 2 和 +1 生命"),
("Hive Commander", "蜂巢指挥官", "临时。给予 1 个友方部队 +2 攻击、+2 护甲和 +1 生命"),
("Hive Fleet Leviathan", "蜂巢舰队利维坦", "直至本场战斗结束，你的部队费用 -1。从你的牌库抽 1 张部队牌"),
("Hyper-adaptation", "超速适应", "选择 1 项效果并给予 1 个友方部队"),
("Infestation Node", "侵染节点", ""),
("Infinite Biomorphologies", "无限生物形态", "选择 1 项效果并给予你手牌中的所有部队"),
("Leviathans Tendrils", "利维坦触须", "抽 2 张牌。给予抽到的部队 +1 生命"),
("Overwhelming Swarm", "压倒性虫群", "本回合你的步兵部队费用 -1"),
("Predator Instincts", "掠食者本能", "给予 1 个友方部队侧翼"),
("Rapacious Hunger", "贪婪饥渴", "对所有敌人造成 3 点伤害。每有 1 个死亡，治疗 1 个随机友方单位 2 点"),
("Ravenous Hunter", "饥渴猎手", "给予 1 个友方单位本回合嗜血"),
("Spirit Leech", "灵魂吸取", "临时。对 1 个敌人造成 1 点伤害，或给予 1 个友方单位 +1 生命"),
("Sporecaster Biostructure", "孢子投掷生物结构", "给予 1 个友方单位本回合 +2 近战"),
("Swarming Masses", "虫群大军", "在你的手牌创建 3 个随机带虫群的利维坦部队"),
("Synaptic Imperative", "突触律令", "给予 1 个友方部队 +2 近战攻击、+2 远程攻击和 +2 生命"),
("Toxic Entanglement", "毒素缠绕", "使 1 个敌方部队眩晕，并给予其脆弱 5"),
("Toxic Miasma", "毒雾", "使 2 个随机敌人失明"),
("Toxic Vapours", "毒气", "对 1 个敌人造成 1-3 点伤害"),
("Tyranid Invasion", "泰伦入侵", "部署 4 个随机 2 费利维坦部队"),
("Neurothrope", "神经脑虫", "突触。天赋：灵魂吸取"),
("SwarmLord", "虫群领主", "天赋：蜂巢指挥官"),
("Tervigon", "繁育母兽", "天赋：巢群繁育者"),
("Protective Bio-structure", "防护生物结构", "给予 1 个友方单位本回合 +2 近战"),
]

def main():
    src = json.load(io.open(SRC, "r", encoding="utf-8"))
    terms = json.load(io.open(TERMS, "r", encoding="utf-8"))
    assert len(src) == 173, "源卡数 != 173: %d" % len(src)
    assert len(T) == 173, "翻译表 != 173: %d" % len(T)
    # 顺序一致性校验: 名字逐一对应
    for i, (card, tr) in enumerate(zip(src, T)):
        if card["name"] != tr[0]:
            sys.exit("名字顺序不匹配 idx=%d: src=%r tr=%r" % (i, card["name"], tr[0]))
    out = {}
    blanks = 0
    for card, tr in zip(src, T):
        en, zh, d = tr
        if card.get("desc"):
            assert d != "", "desc 非空但译文为空: %s" % en
        else:
            if d != "":
                sys.exit("desc 为空但译文非空: %s -> %r" % (en, d))
            blanks += 1
        out[en] = {"n": zh, "d": d}
    # 术语表命中统计 (卡名)
    name_hits = [en for en, v in out.items() if terms.get(en) == v["n"]]
    with io.open(OUT, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=0)
    print("输出卡数(唯一名):", len(out))
    print("跳过数:", 173 - len(src))
    print("空 desc 数:", blanks)
    print("术语表卡名命中数:", len(name_hits))
    for h in sorted(name_hits):
        print("  HIT:", h, "=", out[h]["n"])
    # 中文译名重复校验
    from collections import Counter
    dup = [k for k, v in Counter(x["n"] for x in out.values()).items() if v > 1]
    print("译名重复:", dup)

if __name__ == "__main__":
    main()
