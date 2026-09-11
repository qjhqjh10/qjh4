# -*- coding: utf-8 -*-
# G2 卡牌翻译: Dark Angels 暗黑天使 + Black Legion 黑色军团 (180 张 g2.json)
# 术语权威: _tmp_rulebook_terms.json + 规则书中文翻译 (Armour=护甲/Blast=爆裂/Vulnerable=脆弱/
#   Stun=眩晕/Quest 任务点/Secret=隐秘/Skulls=骷髅头) + 项目 rule_core.gd (quest 语义)
import json, re, io, sys

sys.stdout.reconfigure(encoding="utf-8")

SRC = r"d:/2/Warpforge_tools/scripts/_tmp_slices/g2.json"
OUT = r"d:/2/Warpforge_tools/scripts/_tmp_zhcards_g2.json"
TERMS = r"d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json"

data = json.load(io.open(SRC, encoding="utf-8"))
gloss = json.load(io.open(TERMS, encoding="utf-8"))

# 英文名 -> (中文名, 中文效果)
T = {
# ================= 黑色军团 · 单位 =================
"Abaddon the Despoiler": ("掠夺者阿巴顿", "四神宠儿"),
"Aspiring Champion": ("野心冠军", "获得一个黑暗契约后，获得 +1 远程攻击和 +1 生命"),
"Black Legionary": ("黑色军团战士", ""),
"Bringer of decay": ("腐朽使者", "敌人具有脆弱 1"),
"Chaos Legionary": ("混沌军团战士", "获得一个黑暗契约后，对随机一个敌人造成 2 点伤害"),
"Chaos Lord": ("混沌领主", "当你部署部队时，给予其一个随机黑暗契约。集结：获得一个随机黑暗契约"),
"Chaos Sergeant": ("混沌军士", "集结：获得一个黑暗契约"),
"Chaos Spawn": ("混沌卵", "侧翼"),
"Chaos Terminator": ("混沌终结者", "先锋"),
"Child of Torment": ("折磨之子", "当任意单位受到伤害时，本回合获得 +1 远程攻击"),
"Chosen Champion": ("神选冠军", "猛击：获得一个随机黑暗契约"),
"Chosen": ("神选者", "集结：获得一个黑暗契约"),
"Daemon Blade Legionary": ("恶魔之刃军团战士", "猛击：治疗 2 点"),
"Dark Apostle": ("黑暗使徒", "给予每个我方部队一个随机黑暗契约"),
"Dark Disciple": ("黑暗门徒", "将一张阿巴顿神选置入手牌"),
"Ghallaron's Champion": ("加拉隆的冠军", "你的回合结束时，将一张狂热者军团战士置入手牌。天赋：苦涩之击"),
"Ghallaron's Disciple": ("加拉隆的门徒", "本回合给予所有敌人脆弱 1"),
"Havoc Champion": ("破坏者冠军", "给予所有敌方部队脆弱 2"),
"Havoc": ("破坏者", "当我方部队获得黑暗契约时，对随机一个敌人造成 1 点伤害"),
"Hound of Abaddon": ("阿巴顿猎犬", "先锋"),
"Iskandar Khayon": ("伊坎达尔·卡永", "将一张随机的黑色军团战术置入手牌。其费用减少 2。"),
"Khorne Berzerker": ("恐虐狂战士", "当敌人死亡时，获得一个鲜血黑暗契约"),
"Lheorvine Ukris": ("莱奥尔文·乌克里斯", "猛击：获得一个鲜血黑暗契约"),
"Master of Executions": ("处刑大师", "对敌方督军造成 3-5 点伤害"),
"Master of Possession": ("附魔大师", "天赋：附魔仪式"),
"Meltagun Legionary": ("热熔枪军团战士", "爆裂 2。获得一个黑暗契约后，对随机一个敌人造成 3 点伤害"),
"Noise Marine": ("噪声战士", "当你的督军受到伤害时，获得一个纵欲黑暗契约"),
"Obliterator": ("湮灭者", ""),
"Plague Marine": ("瘟疫战士", "当我方部队死亡时，获得一个韧性黑暗契约。"),
"Possessed Champion": ("附魔冠军", "获得一个随机黑暗契约"),
"Possessed Marine": ("附魔战士", "侧翼"),
"Rubric Marine": ("咒缚战士", "当你抽牌时，获得一个命运黑暗契约。"),
"Sorcerer": ("巫师", "天赋：随机一个黑色军团灵能"),
"Terminator Champion": ("终结者冠军", "护甲 1"),
"Veteran Havoc": ("老兵破坏者", "对敌方一个部队造成 4 点伤害"),
"Veteran Legionary": ("老兵军团战士", "先锋"),
"Warp Talon": ("亚空间爪", "飞行。潜行"),
"Zealot Legionary": ("狂热者军团战士", "集结：对一个敌人造成 1 点伤害"),
# ================= 黑色军团 · 战术/杂项 =================
"Daemonic Feast": ("恶魔盛宴", ""),
"Helfire Outburst": ("地狱火爆发", ""),
"Normal Conditions": ("正常环境", ""),
"Warp Storm": ("亚空间风暴", ""),
"Abaddons Chosen": ("阿巴顿神选", "给予一个我方部队一个黑暗契约"),
"Bitter Blows": ("苦涩之击", "消灭生命值最低的敌方部队。"),
"Black Crusade": ("黑色远征", "从你的卡组随机部署 4 个部队"),
"Daemonic Frenzy": ("恶魔狂暴", "给予一个我方部队 +2 近战攻击，其上的每个黑暗契约再额外给予 +2 近战攻击"),
"Daemonic Pact": ("恶魔契约", "让你的督军获得无敌，持续到你的下个回合，并给予所有敌方部队脆弱 2"),
"Dark Oratory": ("黑暗布道", "给予所有我方部队一个黑暗契约"),
"Delightful Agonies": ("欢愉之痛", "为一个我方单位治疗 5 点。其上的每个黑暗契约，对随机一个敌人造成 2 点伤害"),
"Diabolic Strength": ("恶魔之力", "本回合给予所有我方单位 +2 近战攻击，并治疗所有我方单位 1 点"),
"Disgustingly Resilient": ("恶心坚韧", "给予一个我方部队护甲 1，其上的每个黑暗契约再额外给予护甲 1"),
"Drachnyen": ("德拉赫尼恩", "给予一个我方单位 +4 近战攻击"),
"Execution": ("处决", "消灭一个受伤的敌方部队"),
"Gifts of Chaos": ("混沌恩赐", "给予一个我方部队 2 个随机黑暗契约"),
"Heldrake Strike": ("地狱飞龙突袭", "对三个随机敌人造成 3-6 点伤害"),
"Helfire Pit": ("地狱火坑", "对随机一个敌方部队造成 3 点伤害"),
"Helfire Torch": ("地狱火火炬", "给予一个我方部队一个混沌黑暗契约"),
"Helspear Assault": ("地狱之矛突击", "临时。你的督军本回合获得“爆裂 2 与斩杀：部署一个黑色军团战士”"),
"Hideous Mutation": ("恐怖变异", "给予一个我方部队护甲 1"),
"Hosts of Chaos": ("混沌大军", "抽 2 张牌。每抽到一张部队牌，将一张阿巴顿神选的复制品置入手牌"),
"Idolatrous Despoilers": ("偶像崇拜掠夺者", "本场战斗持续生效：当敌方部队被部署时，其获得脆弱 1"),
"Infernal Gaze": ("地狱凝视", "对随机一个敌人造成 2-3 点伤害"),
"Legacy of Vengeance": ("复仇遗产", "场上有多少个敌人，你的督军本回合便获得 +1 近战攻击"),
"Litany of Despair": ("绝望祷文", "临时。给予一个敌方部队脆弱 2，并附带反噬：给予随机一个敌方部队一个黑暗契约"),
"Malicious Volleys": ("恶毒齐射", "对一个敌人造成 2 点伤害。若其为部队，使其眩晕"),
"Khorne": ("恐虐", ""),
"Nurgle": ("纳垢", ""),
"Slaanesh": ("色孽", ""),
"Tzeench": ("奸奇", ""),
"Method to the Madness": ("疯狂自有章法", ""),
"Murderous Desires": ("嗜杀欲望", "使一个受伤的我方单位自行攻击"),
"Possession": ("附魔", "将一个我方部队的近战攻击与生命值翻倍"),
"Rites of Possession": ("附魔仪式", "造成 2-4 点伤害。若目标死亡，部署一个附魔战士"),
"Skeins of Fate": ("命运丝线", "给予一个我方部队潜行。其上的每个黑暗契约使你抽 1 张牌"),
"Spawndom": ("混沌卵之域", "每有一个敌方单位，部署一个混沌卵"),
"Spreading Corruption": ("蔓延腐化", "给予一个敌方部队脆弱 4。若其本回合死亡，将此效果施加于另一个随机敌方部队"),
"Throne of the Heretic": ("异端王座", "选择一张黑色军团部队牌置入手牌。其费用减少 1。"),
"Traitors Hate": ("叛徒之恨", "对所有单位造成 2 点伤害"),
"Unhallowed Gifts": ("亵渎恩赐", "临时。给予一个我方部队一个黑暗契约"),
"Unholy Smite": ("亵渎重击", "对随机一个敌人造成 6 点伤害。我方单位上每有一个黑暗契约，此牌费用减少 1"),
"Warmaster": ("战争统帅", ""),
# ================= 黑色军团 · 恶魔引擎/英雄 =================
"Accursed Helbrute": ("受诅地狱蛮兽", "当我方部队获得黑暗契约时，此部队同样获得该黑暗契约"),
"Helbrute": ("地狱蛮兽", "护甲 2。当我方部队获得黑暗契约时，对随机一个敌人造成 1-2 点伤害"),
"Maulerfiend": ("蹂躏魔", "我方部队上每有一个黑暗契约，对所有敌人造成 1 点伤害"),
"Venomcrawler": ("毒液爬虫", "护甲 1。你的战术费用减少 1"),
"Vex Machinator": ("维克·马奇内特", "集结：对一个敌人造成 5 点伤害。若目标是载具，将其摧毁"),
"Haarken Worldclaimer": ("哈尔肯·夺世者", "你的回合期间飞行。天赋：地狱之矛突击"),
"Sylar Hexcorn": ("赛拉尔·赫克科恩", "游戏开始时，你的手牌中有一张阿巴顿神选。天赋：随机一个黑色军团灵能"),
"Ghallaron the Pious": ("虔诚者加拉隆", "当具有脆弱的敌人死亡时，治疗 1 点。天赋：绝望祷文"),
# ================= 暗黑天使 · 单位 =================
"Aggressor": ("侵略者", "猛击：获得 1 点任务"),
"Apothecary": ("医师", "当我方单位获得护盾时，其治疗 2 点。集结：为一个我方单位治疗 2 点"),
"Bladeguard Veteran": ("剑卫老兵", "护甲 1。先锋。议程：给予另一个单位护甲 2，持续到你的下个回合"),
"Captain": ("队长", "从你的卡组选择一张部队牌置于卡组顶部"),
"Chaplain": ("牧师", "集结：从你的卡组选择一张牌置于卡组顶部"),
"Company Champion": ("连队冠军", "斩杀：对攻击力最高的敌人造成 3 点伤害"),
"Company Master": ("连队队长", "当你抽牌时，该牌本回合费用减少 1"),
"Company Veteran": ("连队老兵", "当你获得护盾时，治疗 2 点"),
"Deathwing Champion": ("死翼冠军", "护甲 1。传送：获得 +1 攻击和迅捷。"),
"Deathwing Knight Master": ("死翼骑士长", "护甲 2。传送：对所有敌人造成 3 点伤害，并给予所有我方单位 +2 攻击。"),
"Deathwing Knight": ("死翼骑士", "护甲 2。传送：获得先锋并自行攻击"),
"Deathwing Strikemaster": ("死翼打击大师", "护甲 1。传送：给予所有我方部队 +1 攻击、+1 护甲和 +1 生命"),
"Deathwing Terminator": ("死翼终结者", "护甲 1。传送：对三个随机敌人造成 2 点伤害。"),
"Ezekiel": ("以西结", "护盾。伪装。天赋：审讯大师"),
"Inner Circle Companion": ("内环同伴", "获得先锋与 2 点任务"),
"Intercessor": ("仲裁者", "集结：获得 1 点任务"),
"Interrogator Chaplain": ("审讯牧师", "当敌人死亡时，获得 1 点任务"),
"Librarian": ("智库", "抽取你卡组中的下一张战术并获得 1 点任务。"),
"Master Lazarus": ("拉撒路大师", "护甲 1。议程：触发一个我方单位的传送与斩杀效果，并获得 1 点任务"),
"Reiver": ("掠夺者", "斩杀：抽 1 张牌"),
"Sergeant Naaman": ("纳曼军士", "斩杀：获得 1 点任务"),
"Techmarine": ("技术军士", "当我方载具攻击时，获得 1 点能量。若其存活，为其治疗 3 点"),
# ================= 暗黑天使 · 战术/防御 =================
"Ancient Reliquary": ("圣物匣", "给予一个我方部队 +3 攻击、+3 护甲或 +3 生命"),
"Asteroid Zone": ("小行星区", ""),
"Blacksword Missiles": ("黑剑导弹", "对所有具有飞行的敌人造成 3 点伤害，对其他所有敌人造成 1 点伤害"),
"Convoke the Circle": ("召集圆环", "抽 3 张牌并使其费用降低 3"),
"Covert Operation": ("秘密行动", "将一个我方部队与一个随机敌方部队移回其卡组顶部"),
"Deadly Hunters": ("致命猎手", "每有一个敌方部队，部署一个黑骑士"),
"Deathwing Assault": ("死翼突袭", "给予我方部队 +2 攻击、+2 护甲与 +2 生命，并触发其传送能力。"),
"Defensive Turrets": ("防御炮塔", "对随机一个敌方部队造成 2 点伤害"),
"Desperate Mission": ("孤注一掷", "抽三张部队牌。本回合其费用减少 2"),
"Exemplar of Hate": ("憎恨典范", ""),
"Fury of the Unforgiven": ("宽恕者之怒", "消灭一个敌方部队。获得 2 点任务。"),
"Grim Effigy": ("肃穆雕像", "获得 2 点任务"),
"Grim Resolve": ("钢铁决心", "造成 2 点伤害。若目标死亡，获得 1 点能量。"),
"Hunt the Fallen": ("追猎堕落者", "抽两张牌。每抽到一张部队牌，获得 1 点任务"),
"Inner Circle": ("内环", "从你的卡组选择一张牌置于卡组顶部。其费用减少 1"),
"March of Vengeance": ("复仇进军", "给予一个我方单位 +1 攻击。获得 2 点任务"),
"Martial Superiority": ("武力优势", "为一个我方单位治疗 2 点。\n获得 1 点任务。"),
"Master Interromancer": ("审讯大师", "临时。对一个敌人造成 3-5 点伤害。若目标死亡，获得 2 点任务。"),
"Master of Manoeuvre": ("机动大师", "临时。将一个我方载具移回手牌。其费用减少 4"),
"Master of the Deathwing": ("死翼大师", "本回合给予你的督军 +2 攻击与护甲 2"),
"None Must Know": ("无人知晓", "消灭一个敌方部队。使相邻单位眩晕"),
"Obscure Ritual": ("晦涩仪式", "补满你的能量。抽一张部队牌"),
"Plasma Generator": ("等离子发电机", "获得 1 点能量"),
"Reconnaissance Mission": ("侦察任务", "选择对手手牌中的一张牌。当其被打出时，获得 3 点任务。"),
"Rites of Penance": ("赎罪礼", "给予一个我方单位 +3 近战、+3 远程与无敌，持续到你的下个回合"),
"Sacred Standard": ("神圣旗帜", "本回合给予所有我方单位 +1 攻击与爆裂 1"),
"Secret Agenda": ("隐秘议程", "选择一张暗黑天使隐秘牌加入你的卡组。若你控制一个载具，抽 1 张牌"),
"Smothering Decree": ("窒息敕令", "对所有敌人造成 3 点伤害。每有一个因此死亡的敌人，为一个我方单位治疗 3 点"),
"Star Orbiting": ("环绕恒星", ""),
"Steadfast Warriors": ("坚定战士", "给予所有我方单位护甲 2，持续到你的下个回合"),
"Stubborn Defiance": ("顽强抵抗", "场上有多少个敌人，本回合即给予一个我方单位 +1 攻击"),
"Supreme Grand Master": ("至高大师", "临时。给予我方部队 +1 攻击与 +1 护甲。若你未控制任何部队，将一张仲裁者置入手牌。"),
"The Rock": ("磐石", "选择一项：获得 2 点任务；部署一个塔曼军士；为你的所有单位治疗 3 点"),
"Unwavering Zeal": ("坚定狂热", "对三个随机敌人造成 3 点伤害"),
"Void Combat": ("虚空作战", ""),
"Wages of Retribution": ("报应代价", "本场战斗持续生效：当敌人死亡时，你获得 1 点能量。"),
# ================= 暗黑天使 · 渡鸦翼/载具 =================
"Black Knight": ("黑骑士", "侧翼。斩杀：获得 1 点任务"),
"Dark Talon": ("暗爪", "使一个敌人眩晕"),
"Darkshroud": ("暗影笼罩", "给予一个我方部队潜行。若其是载具，获得 1 点任务。"),
"Land Speeder Vengeance": ("复仇者兰德飞车", "若你未控制其他部队，获得侧翼"),
"Nephilim Jetfighter": ("尼非利姆喷气机", "集结：对一个敌人造成 3 点伤害。若其具有飞行，改为造成 5 点伤害并获得 1 点护甲"),
"Ravenwing Ancient": ("渡鸦翼旗手", "斩杀：给予所有我方载具 +1 攻击与 +1 护甲"),
"Ravenwing Bikes": ("渡鸦翼摩托", "侧翼。议程：获得 1 点任务"),
"Ravenwing Champion": ("渡鸦翼冠军", "集结：获得 1 点任务。当你生成一张隐秘牌时，对随机一个敌人造成 3 点伤害"),
"Ravenwing Speeder": ("渡鸦翼飞车", "议程：造成 2 点伤害。若目标存活，获得 1 点任务。"),
"Ravenwing Talonmaster": ("渡鸦翼队长", "我方载具具有侧翼。议程：对所有处于潜行的敌人造成 3 点伤害，且其失去潜行。"),
"Repulsor": ("驱逐者", "护甲 1。爆裂 4。集结：对一个敌人造成 6 点伤害；若目标死亡，获得 3 点任务。"),
"Sammael": ("萨玛尔", "将一张随机的暗黑天使载具置入手牌。天赋：机动大师"),
"Unforgiven Redemptor": ("宽恕者救赎者", "护甲 1。当你获得任务点时，对随机一个敌人造成 2 点伤害"),
# ================= 暗黑天使 · 英雄 =================
"Asmodai": ("阿斯莫代", "议程：获得 1 点任务。天赋：忏悔大师"),
"Azrael": ("阿兹瑞尔", "从你的卡组选择一张牌置于卡组顶部。至高大师"),
"Belial": ("贝利亚", "斩杀：抽你卡组中的下一张部队牌。天赋：死翼大师"),
"Refuse to Yield": ("拒不退让", "临时。为一个我方单位治疗 2 点，并且本回合每有一个敌方单位，使其获得 +1 攻击"),
"Assault Terminator": ("突击终结者", "护甲 1。传送：消灭一个随机敌方部队。"),
"Chaplain Gabutheron": ("加布瑟隆牧师", "护甲 2。传送：本回合给予你的单位 +3 攻击与无敌。议程：为一个我方单位治疗 4 点并获得 +1 能量。"),
"Vengeful Brethren Bladeguard": ("复仇兄弟剑卫", "护甲 1。当我方部队死亡时，获得 +2 攻击与护甲 1。"),
"Vengeful Brethren Hellblaster": ("复仇兄弟地狱火枪手", "当我方部队死亡时，获得爆裂 2"),
"Vengeful Brethren Intercessor": ("复仇兄弟仲裁者", "当我方部队死亡时，获得 +1 远程攻击。"),
"Watcher in the Dark": ("暗中守望者", "选择一张非传说暗黑天使卡加入你的手牌。获得 1 点任务。"),
"Hunters of Heretics": ("异端猎手", "给予一个敌方部队脆弱 4 与“反噬：你的对手获得 2 个骷髅头”"),
"Relic Munitions": ("圣物弹药", "本回合给予你的单位 +1 攻击。本局你每打出一张隐秘牌，重复一次。"),
"Vengeful Brethren": ("复仇兄弟", "给予一个我方部队 +1 攻击与 +1 生命。在其卡组顶部生成一张复制。"),
"Ravenwing Ballistus Dreadnought": ("渡鸦翼弩炮无畏机甲", "护甲 1。当你生成或打出一张隐秘牌时，对所有敌人造成 2-4 点伤害。"),
"Master Zacharial": ("扎卡里尔大师", "议程：选择手牌中的一张牌并将其移回你的卡组。抽 1 张牌。获得 1 点任务。天赋：拒不退让"),
"Master of Repentance": ("忏悔大师", "临时。造成 1 点伤害。若目标存活，获得 1 点任务。"),
"Hellfire Torch": ("地狱火火炬", "给予一个我方部队一个混沌黑暗契约"),
"Hellfire Pit": ("地狱火坑", "对随机一个敌方部队造成 3 点伤害"),
"Sergeant Taaman": ("塔曼军士", "潜行（The Rock 部署的部队）"),
}

def junk(name):
    return bool(re.search(r"[_0-9]|v2|^$", name)) or name.strip() == ""

out = {}
skipped = []
missing = []
for e in data:
    n = e["name"].strip()
    if junk(n):
        skipped.append(n)
        continue
    if n not in T:
        missing.append(n)
        continue
    cn, cd = T[n]
    out[n] = {"n": cn, "d": cd}

# 术语表命中(卡名精确键)
hits = [n for n in out if n in gloss]
near = [n for n in out if n not in gloss and any(g != n and n[:6] in g or g.startswith(n.split()[0]) for g in gloss)]

json.dump(out, io.open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=0)

print("源条目:", len(data))
print("输出卡数:", len(out))
print("跳过(杂乱名):", skipped)
print("缺失(未翻译):", missing)
print("术语表精确命中卡名:", len(hits), hits)
print("写入:", OUT)
