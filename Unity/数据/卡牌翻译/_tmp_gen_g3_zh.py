# -*- coding: utf-8 -*-
"""g3 slice 中文翻译生成器: Emperor's Children + Genestealer Cults (173 张).
术语依据: Warpforge部队卡片/_tmp_rulebook_terms.json (权威术语表)
+ Warpforge_Offline_Rulebook_1_5-3_中文翻译.md (官方规则书中文, 名称/句式权威)
输出: Warpforge_tools/scripts/_tmp_zhcards_g3.json
"""
import json
import io

SRC = r"d:/2/Warpforge_tools/scripts/_tmp_slices/g3.json"
OUT = r"d:/2/Warpforge_tools/scripts/_tmp_zhcards_g3.json"

# (英文名(规范形, ´→'), 中文名, 中文效果)
D = [
("Alluress", "魅惑者", "集结：给予一个敌方部队 -2 生命和 -2 攻击。友方恶魔少女具有侧翼。"),
("Blissbringer", "极乐使者", "给予所有友方恶魔 +1 攻击。"),
("Daemonette", "恶魔少女", "给予一个敌方部队 -1 攻击和 -1 护甲。"),
("Fiend", "恶鬼", "在你的手牌中生成一张色孽选民。"),
("Slaanesh's Spawn", "色孽之卵", "残忍：获得 +1 和侧翼。"),
("Winged Daemon Prince", "有翼恶魔亲王", "飞行。其他友方恶魔费用 -2，且具有 +2 攻击。残忍：抽一张恶魔。"),
("Blastmaster Noise Marine", "爆鸣者噪声战士", "摧毁被本单位攻击的任何具有护甲的敌方部队。"),
("Disharmonist", "失谐者", "给予一个随机敌方部队 -2 攻击和 -2 护甲。"),
("Dual Screamer Kakophonist", "双重尖啸刺耳者", "集结：本回合给予一个敌方单位 -4 攻击和 -4 生命。"),
("Flawless Blade Champion", "无瑕之刃冠军", "斩杀：给予所有友方部队 +1 威能。\n狂喜 3：攻击一个随机敌人。"),
("Flawless Blade", "无瑕之刃", "斩杀：对敌方督军造成 2 点伤害。"),
("Glittering Myriad Infractor", "绚丽万千违规者", "激励：获得 +1 攻击、+1 远程并治疗 1。"),
("Icon of Excess Infractor", "放纵圣像违规者", "狂喜 2：给予所有友方部队纵欲黑暗契约。"),
("Infractor Obsessionist", "违规者痴迷者", "集结：选择一张战斗药剂并在你的手牌中生成它。激励：获得护盾。"),
("Infractor", "违规者", "残忍：获得 +1 攻击和 +1 护甲。"),
("Konstrictus Tormentor", "紧缚折磨者", "你的回合结束时，在你的手牌中生成一张随机战斗药剂。"),
("Lord Exultant", "狂喜领主", "给予所有友方部队 +1 和 +1。"),
("Lord Kakophonist", "刺耳者领主", "激励：直到你的下回合，给予所有敌方单位 -1 近战和 -1 远程。"),
("Malgarash the Adamant", "马尔加拉什·坚毅者", "护甲 1。狂喜 2：治疗 2。激励：获得 +1 武器。"),
("Plasma Gun Tormentor", "等离子枪折磨者", "激励：获得爆裂 1。"),
("Rugged Disharmonist", "粗犷失谐者", "敌方部队具有 -2 攻击。"),
("Slaanesh Sorcerer", "色孽巫师", "给予另一个随机友方部队 +1 和 +1。"),
("Sonic Blaster Noise Marine", "音爆噪声战士", "被本单位攻击的敌方部队获得眩晕，并获得 -1 护甲和 -1 攻击。"),
("Terminator Champion", "终结者冠军", "护甲 1。残忍：对另一个随机友方单位造成 1 点伤害，并获得 +1 攻击和 +1 武器。"),
("Terminator", "终结者", "护甲 1。狂喜 2：获得 +2。"),
("Threnodic Choir Flawless Blade", "悲歌合唱团无瑕之刃", ""),
("Threnodic Choir Flawless", "悲歌合唱团无瑕者", ""),
("Threnodic Choir Noise Marine", "悲歌合唱团噪声战士", "狂喜 2：获得嗜血并治疗 1。"),
("Tormentor Obsessionist", "折磨者痴迷者", "给予所有友方部队 +2 攻击。"),
("Tormentor", "折磨者", "狂喜 1：对一个随机敌人造成 3 点伤害。"),
("Turyan Ghauze", "图尔扬·高兹", "给予相邻友方部队纵欲黑暗契约。"),
("Veteran Noise Marine", "老兵噪声战士", "爆裂 3。"),
("Aural Hijack", "听觉劫持", ""),
("Empyric Rift", "灵能裂隙", ""),
("Stimm-Vents Leak", "兴奋剂排气泄漏", ""),
("Beautiful Death", "美丽死亡", "对一个敌方部队造成 10 点伤害。每有 1 点溢出伤害，治疗你的督军 1。"),
("Carnival of Excess", "纵欲狂欢", "部署 3 个费用 5 或以下的随机帝皇之子恶魔。"),
("Chosen of Slaanesh", "色孽选民", "给予一个友方部队纵欲黑暗契约。"),
("Coterie of the Conceited", "自负者同盟", "你的督军获得：“你的回合开始时，在你的手牌中生成一张随机战斗药剂”。"),
("Dark Apparitions", "黑暗幻影", "给予一个随机友方恶魔潜行。若你没有控制任何恶魔，抽一张恶魔。"),
("Dark Prince's Throne", "黑暗王子王座", ""),
("Duelist's Hubris (Lucius' Talent)", "决斗者的傲慢（卢修斯的天赋）", "择一：给予一个随机敌方部队脆弱 1，或治疗你的督军 1。"),
("Antrak Silk", "安特拉克丝", "对一个友方单位造成 1 点伤害，并使其本回合获得 +1 攻击和 +1 护甲。"),
("Heliotrophos", "日光酮", "本回合给予一个友方单位爆裂 1。"),
("Quail", "鹌鹑", "治疗一个友方单位 2。"),
("Shivversplint", "颤刺", "给予一个友方部队护甲 1。"),
("Skorflense", "斯克芬斯", "给予一个友方部队 +2 攻击。"),
("Xylocil", "克西洛西尔", "给予一个友方部队 +2。"),
("Embrace the Pain", "拥抱痛苦", "造成 2 点伤害。若目标存活，治疗其 5。"),
("Eternal Servitude", "永恒奴役", "选择一张自你的上个回合以来死亡的友方部队并部署它。将其生命降至 1。"),
("Euphoric Strike (Lord Exultant's Talent)", "狂喜重击（狂喜领主的天赋）", "造成 1 点伤害。若目标死亡，在你的手牌中生成一张随机战斗药剂。"),
("Excessive Vigour (Daemon Prince's Talent)", "过度活力（恶魔亲王的天赋）", "给予一个友方部队纵欲黑暗契约。若其为恶魔，另给予 +2 生命。"),
("Exquisite Swordsmanship", "精湛剑术", "你的督军本回合获得 +1。对所有敌人造成等同于你的督军近战的伤害。"),
("Internal Rivalry", "内部纷争", "对所有友方部队造成 1 点伤害，并给予它们纵欲黑暗契约。"),
("Mechanised Murder", "机械杀戮", "抽两张牌。每抽到一张部队，在你的手牌中生成一张随机战斗药剂。"),
("Mercurial Host", "善变大军", "在你的手牌中生成 3 张随机战斗药剂。"),
("Normal", "普通", ""),
("Peerless Bladesmen", "绝世剑士", "目标友方单位攻击攻击力最高的敌人。"),
("Plains of Excess", "纵欲平原", "对所有单位造成 1 点伤害。"),
("Pledge to the Dark Prince", "对黑暗王子的誓言", "给予一个友方部队“斩杀：治疗 2 并获得纵欲黑暗契约”。"),
("Quicksilver Grace", "水银优雅", "给予一个友方部队侧翼。在你的手牌中生成一张随机战斗药剂。"),
("Rapid Evisceration", "急速开膛", "造成 2 点伤害。若目标死亡，部署一个“违规者”。"),
("Rapturous Ruination", "狂喜毁灭", "对所有单位造成 7 点伤害。"),
("Terrifying Crescendo", "骇人渐强", "给予一个敌方部队 -3。然后，若其为 0，将其摧毁。"),
("Thrill Seekers", "寻求刺激者", "对一个友方单位造成 1 点伤害。抽一张牌。"),
("Tools of Torture", "酷刑工具", ""),
("Vengeful Surge", "复仇之潮", "部署一个违规者和一个折磨者。若本回合有友方部队死亡，给予它们侧翼。"),
("Vial Tanks", "药剂罐", "选择一张战斗药剂并在你的手牌中生成它。"),
("Chaos Land Raider", "混沌兰德袭击者", "护甲 2。残忍：部署一个费用 3 或以下的随机帝皇之子步兵。"),
("Chaos Rhino", "混沌犀牛", "部署一个费用 3 或以下的随机帝皇之子步兵。"),
("Heldrake", "地狱飞龙", "对所有敌方单位造成 3 点伤害。"),
("Maulerfiend", "蹂躏魔", "狂喜 5：使本部队的近战和远程翻倍。"),
("Lucius The Eternal", "永恒卢修斯", "激励：对一个随机敌人造成 1 点伤害。天赋：决斗者的傲慢。"),
("Xarahan Daemon Prince", "夏拉罕恶魔亲王", "狂喜 15：获得 +1 力量。天赋：过度活力。"),
("Alien Idol", "异形偶像", "恢复 1 点能量。"),
("Pilfered Supplies", "窃取补给", "选择一张破坏卡并加入对手手牌。"),
("Rusted Vents", "锈蚀通风口", "在你的手牌中生成一个带伏击的随机部队。其费用 -2。"),
("Aberrant Hypermorph", "异形超变体", "护甲 2。先锋。"),
("Aberrant", "异形", "护甲 1。伏击：获得先锋。"),
("Abominant", "异形霸主", "护甲 1。震荡。伏击：获得护甲 1。"),
("Acolyte Heavy", "侍僧重装", "伏击：获得 +2 护甲。猛击：在对手手牌中生成一张随机破坏卡。"),
("Acolyte Hybrid", "侍僧混种", "伏击：获得 +2 和 +2。"),
("Acolyte Leader", "侍僧队长", "当对手打出战术时，给予你的部队 +1 和 +1 生命。"),
("Acolyte Specialist", "侍僧专家", "在对手手牌中生成一张随机破坏卡。"),
("Benefictus", "受福者", "对一个敌方部队造成等同于对手手牌数量的伤害。"),
("Biophagus", "噬生者", "潜行。集结：在对手手牌中生成一张中毒补给。天赋：实验性生物恐怖。"),
("Clamavus", "喧嚣者", "集结：眩晕一个敌人。天赋：声讯黑客。"),
("Concealed Explosives", "隐藏炸药", "无法攻击。你的回合开始时受到 1 点伤害。伏击：对所有敌人造成 2-3 点伤害。"),
("Genestealer Familiar", "基因窃取者魔宠", "相邻单位具有 +1。集结：给予一个友方步兵侧翼。"),
("Hulking Aberrant", "魁梧异形", "震荡。伏击：获得 +2 攻击并眩晕一个随机敌人。"),
("Hybrid Metamorph", "变形混种", "获得 +2 攻击和 +1 生命。"),
("Kelermorph", "凯勒魔", "对一个随机敌人造成 1 点伤害，共 6 次。"),
("Locus", "洛克斯", "潜行。猛击：眩晕一个随机敌人。"),
("Metamorph Iconbearer", "变形偶像持旗者", "集结：选择一张 2 费基因窃取者教派部队，在你的手牌中生成 2 张复制，并使其费用 -1。"),
("Metamorph Leader", "变形队长", "你的部队费用 -1。起义：给予所有友方部队 +1 攻击。"),
("Neophyte Heavy", "新教徒重装", "对一个随机敌人造成 2 点伤害。"),
("Neophyte Hybrid", "新教徒混种", "在对手手牌中生成一张随机破坏卡。"),
("Neophyte Iconbearer", "新教徒持旗者", "当友方单位攻击时，在你的手牌中生成一张新教徒新兵。"),
("Neophyte Initiate", "新教徒新兵", "对一个随机敌人造成 1 点伤害。"),
("Neophyte Leader", "新教徒队长", "给予所有友方部队 +1 和 +1。"),
("Neophyte Specialist", "新教徒专家", "选择一张破坏卡并加入对手手牌。"),
("Neophyte Trooper", "新教徒步兵", "起义：获得侧翼和 +1。"),
("Nexos", "枢纽者", "直到你的下回合，你的单位获得 +1 攻击。"),
("Patriarch", "族长", "伏击：给予你的所有单位 +3 蛮力和 +3 远程。天赋：最致命的杀手。对手手牌中每有一张牌，其费用 -1。"),
("Purestrain Genestealer", "纯血基因窃取者", "侧翼。"),
("Reductus Saboteur", "消减破坏者", "部署一个隐藏炸药。"),
("Sanctus", "圣徒", "摧毁一个随机敌方部队。"),
("Mining Tremors", "采矿震颤", ""),
("Normal Conditions", "正常状况", ""),
("Sump Overspill", "污水外溢", ""),
("Toxic Fumes", "毒性烟雾", ""),
("Ambush", "伏击", ""),
("A Trap Sprung", "陷阱触发", "对一个随机敌人造成 3 点伤害。对手手牌中每有一张破坏卡，重复此效果。"),
("Backstab", "背刺", "造成 2 点伤害。若目标死亡，在对手手牌中生成一张随机破坏卡。"),
("Bore Through", "钻透", "造成 3 点伤害。若目标具有护甲，改为造成 6 点伤害。"),
("Brood Prophet", "教派先知", "选择一张非传说基因窃取者教派部队并加入你的手牌。"),
("Clandestine Operation", "隐秘行动", "选择一张破坏卡并加入对手手牌。抽一张牌。"),
("Cult Propaganda", "教派宣传", "你的回合结束时，在对手手牌中生成一张新教徒新兵。"),
("Cut Them Off", "切断退路", "在对手手牌中生成两张临时路障。"),
("Day of Ascension", "升天之日", "对所有敌人造成等同于对手手牌数量的伤害。"),
("Deadliest Killer", "最致命的杀手", "对一个敌方部队造成 6 点伤害。若其死亡，抽一张部队。"),
("Enhanced Aggression", "强化攻击性", "给予一个友方部队侧翼和“猛击：抽一张牌”。"),
("Enhanced Musculature", "强化肌肉", "给予一个友方部队 +3 拳头。"),
("Enhanced Resilience", "强化韧性", "给予一个友方部队护甲 1 和 +2 生命。"),
("Eve of the Uprising", "起义前夕", "将本场战斗中死亡的至多 5 张友方部队返回你的牌库，并使其费用 -3。"),
("Experimental Bio-Horrors", "实验性生物恐怖", "选择一张基因增强并加入你的手牌。"),
("Gestalt Energy", "聚合能量", "直到你的下回合，给予你的单位 +2 攻击。"),
("Hive Fleet Arrival", "虫巢舰队降临", ""),
("Improvised Barricade", "临时路障", "无效果。"),
("Inscrutable Cunning", "莫测狡诈", "择一：抽一张部队；抽一张战术；或为你的单位治疗 1。"),
("Jammed Communications", "通讯干扰", "当你打出战术时，你的督军受到 1 点伤害。"),
("Living Icon", "活体圣像", "你的督军获得“猛击：将手牌中一张随机部队的费用 -1”。抽一张部队。"),
("Lying in Wait", "伺机而动", "将一张友方部队返回你的手牌，并将其费用降至 1。"),
("Master Outrider", "骑术大师", "临时。你的督军本回合获得狙击和“斩杀：在对手手牌中生成一张随机破坏卡”。"),
("Nexus of Devotion", "虔诚枢纽", "为你的单位治疗 1，并使其本回合获得 +1 攻击和 +1 护甲。"),
("Open Insurrection", "公开叛乱", "本回合给予你的部队无敌，并触发它们的起义能力。"),
("Perfect Ambush", "完美伏击", "直到你的下回合，给予一个友方部队无敌。"),
("Perfect Hosts", "完美宿主", "每有一个敌方单位，在你的手牌中生成一张新教徒混种。"),
("Poisoned Supplies", "中毒补给", "你的回合结束时，你的部队受到 1 点伤害。"),
("Rampant Infestation", "猖獗侵染", "本回合你的部队费用 -3。"),
("Roaming Outriders", "游荡侦骑", "对一个随机敌人造成 1 点伤害并眩晕它。你手牌中每有一张载具，重复此效果，并使其费用 -1。"),
("Rogue Informant", "叛变线人", "选择你牌库中的一张牌并抽取。若其为部队，给予它潜行。"),
("Subterranean Ambush", "地下伏击", "选择一张本场战斗中死亡的友方部队并部署它。"),
("Summon the Cult", "召唤教派", "部署 2 个随机的 2 费基因窃取者教派部队。"),
("Telephatic Domination", "心灵支配", "本回合控制一个敌方部队，并给予它迅捷。"),
("Underground Network", "地下网络", "在对手手牌中生成一张随机破坏卡。本场战斗中，对手手牌中的破坏卡费用 +1。"),
("Vox-Hacker", "声讯黑客", "选择一张破坏卡并加入对手手牌。"),
("Xeno-Mutations", "异形突变", ""),
("Achilles Ridgerunner", "阿喀琉斯山脊车", "猛击：在对手手牌中生成一张随机破坏卡。"),
("Atalan Jackal", "阿塔兰胡狼", "伪装。当你生成一张破坏卡时，对一个随机敌人造成 2 点伤害。"),
("Atalan Leader", "阿塔兰队长", "友方载具费用 -1。猛击：触发所有友方部队的伏击能力。"),
("Atalan Wolfquad", "阿塔兰狼式摩托", "集结：对一个敌人造成 2 点伤害，并对其相邻单位造成 1 点伤害。"),
("Cult Leman Russ", "教派莱曼·鲁斯", "护甲 1。先锋。起义：对一个随机敌人造成 5 点伤害。"),
("Cult Sentinel", "教派哨兵", "当对手打出战术时，对一个随机敌人造成 3 点伤害。"),
("Goliath Rockgrinder", "歌利亚碾石车", "护甲 1。爆裂 4。"),
("Goliath Truck", "歌利亚卡车", "护甲 1。猛击：部署两个新教徒混种。"),
("Jackal Outrider", "胡狼骑兵", "伏击：本回合获得侧翼、爆裂 2 和震荡。"),
("Jackal Scout", "胡狼斥候", "集结：选择对手手牌中的一张牌，使其费用 +1。"),
("Jackal Specialist", "胡狼专家", "在对手手牌中生成一张随机破坏卡。"),
("Spotter Ridgerunner", "观测山脊车", "集结：本回合给予一个敌人先锋。起义：对一个具有潜行的随机敌人造成 3 点伤害，并使其失去潜行。"),
("Acolyte Iconward", "侍僧持旗者", ""),
("Lhaska Szenari", "拉什卡·泽纳里", "天赋：骑术大师。"),
("Magus Uthrel Naas", "大导师乌斯雷尔·纳斯", "天赋：教派先知。"),
("Primus Saffa Rhiannor", "首席萨法·里安诺", "天赋：莫测狡诈。"),
("Dark Pact of Fate", "命运黑暗契约", "给予 +2 生命和伪装。"),
("Dark Pact of Blood", "鲜血黑暗契约", "给予 +2 近战攻击和先锋。"),
("Dark Pact of Excess", "纵欲黑暗契约", "给予 +2 近战攻击和 +2 远程攻击。"),
("Dark Pact of Resilience", "韧性黑暗契约", "给予 +1 生命和再生 1。"),
("Lord Kaphrael", "卡弗拉尔领主", "激励：给予一个随机友方部队 +1。天赋：狂喜重击。"),
("Veldras the Sublime", "维尔德拉斯·崇高者", "残忍：获得 +1 护甲。激励：攻击攻击力最高的敌人。"),
("Armoury of Excess", "纵欲军械库", "造成 1 点伤害。若目标是友方部队，给予它纵欲黑暗契约。若目标是敌人，抽一张牌。"),
("Decadent Throne", "颓废王座", "选择你牌库中的一张部队并抽取，其费用 -1。"),
("Rusted Vent", "锈蚀风口", "在你的手牌中生成一个带伏击的随机部队。其费用 -2。"),
]

def canon(s):
    return s.replace("\u00b4", "'")

src = json.load(open(SRC, encoding="utf-8"))
my = {canon(n): (n, zh, d) for (n, zh, d) in D}

out = {}
missing = []
keydiff = []
for e in src:
    k = canon(e["name"])
    if k not in my:
        missing.append(e["name"])
        continue
    orig, zh, d = my[k]
    if orig != e["name"]:
        keydiff.append((e["name"], orig))
    out[e["name"]] = {"n": zh, "d": d}

unused = []
used_keys = set(canon(e["name"]) for e in src)
for k in my:
    if k not in used_keys:
        unused.append(my[k][0])

print("source entries:", len(src))
print("translated:", len(out))
print("missing:", missing)
print("unused mappings:", unused)
print("key rewrites (source vs mapping):", [(x.replace("´", "<ACUTE>"), y) for x, y in keydiff])

# 术语表命中统计
terms = json.load(open(r"d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json", encoding="utf-8"))
er = set()
for e in src:
    erf = e.get("name") or ""
    if erf == "":
        continue
    kk = canon(erf)
    # 精确命中
    if erf in terms:
        er.add((erf, terms[erf]))
    elif kk in terms:
        er.add((kk, terms[kk]))
print("exact glossary hits (names):", len(er))
for x in sorted(er):
    print("   ", x)

with io.open(OUT, "w", encoding="utf-8") as f:
    json.dump(out, f, ensure_ascii=False, indent=0)
print("written:", OUT)
