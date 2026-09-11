# -*- coding: utf-8 -*-
# G4 Goff Orks (高夫兽人) 卡牌中文化生成器
# 权威依据: d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json (术语表)
#           d:/2/Warpforge部队卡片/Warpforge_Offline_Rulebook_1_5-3_中文翻译.md (关键词全表+规则文体)
import json, re, sys, io

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

SRC = r"d:/2/Warpforge_tools/scripts/_tmp_slices/g4.json"
OUT = r"d:/2/Warpforge_tools/scripts/_tmp_zhcards_g4.json"

# <英文名> -> (中文名, 中文效果)
T = {
"Ghazghkull Thraka": ("加兹古尔·斯拉卡", ""),
"Beast Snagga Boy": ("猎兽小子", "潮涌 1。践踏。集结：如果你控制一只野兽，获得 +1 生命值。"),
"Beastboss on Squigosaur": ("猪龙野兽头目", "友方野兽费用 -1，并且拥有“斩杀：本回合获得嗜血”。"),
"Gargantuan Squiggoth": ("巨型猪兽", "践踏。集结：使一个敌人及其相邻单位眩晕。群体：对所有敌人造成 1 点伤害。"),
"Grot Orderly": ("地精勤务兵", "集结：治疗一个友方单位 2 点生命值。你的回合开始时，返回你的手牌。"),
"Grot on Bombsquig": ("炸弹猪上的地精", "反噬：部署一个炸弹猪。"),
"Hunta Rig": ("猎兽战车", "践踏。集结：选择敌方手牌中的一个部队并部署它。该部队攻击它。"),
"Kill Rig": ("杀戮战车", "对一个敌人造成 3-4 点伤害。如果其拥有护甲，改为造成 5-7 点伤害。"),
"Nob on Smasha Squig": ("重装砸猪老大", "其他友方野兽获得 +1[攻击] 和 +1[生命值]。"),
"Painboss": ("痛医头目", "集结：部署一个本局阵亡的随机友方野兽。天赋：医官的工具。"),
"Runtherd": ("放牧者", "群体：在你的手牌中生成一个费用 5 或以下的随机兽人野兽。"),
"Snakebite Grot": ("蛇咬地精", "潮涌 1。使其所攻击的部队眩晕。"),
"Squighog Boyz": ("猪骑士小子", "潮涌 2。践踏。集结：如果你没有控制其他部队，获得侧翼。"),
"Thump Gun Beast Snagga": ("重锤炮猎兽小子", "本回合获得爆裂 1。"),
"Wurrboy": ("嚎哭小子", "你的回合结束时，为你场上的每个友方部队，对一个随机敌人造成 1-2 点伤害。"),
"Zodgrod Wortsnagga": ("佐德格罗德·沃茨纳加", "随机部署一个地精或蛇咬地精。"),
"Cyber\u2011augmentation": ("赛博强化", "给予你手牌中的一个随机部队 +1 攻击和 +1 生命值，以及侧翼。"),
"Da Hunt is On": ("狩猎开始！", "造成 2 点伤害。你打出的下一只野兽获得侧翼。"),
"Da Old Ways": ("老规矩", "给予一个友方单位[践踏]。如果其已经拥有[践踏]，改为本回合给予其 +2[攻击]。"),
"Dok\u2019s Toolz (Painboss' talent)": ("医官的工具（痛医头目天赋）", "给予所有友方单位 +2 生命值。如果你控制一只野兽，为你的督军治疗 4 点生命值。"),
"Drag it Down": ("拖倒它！", "为你场上的每个友方单位，对一个敌人造成 1 点伤害。如果目标死亡，给予你手牌中的所有友方部队 +1 生命值。"),
"Fasta Than Yooz": ("比你们快", "在你的手牌中生成三个随机兽人载具。它们费用 -1。"),
"Ferocious Rage (Beastboss' Talent)": ("凶暴之怒（野兽头目天赋）", "你的督军本回合获得 +1[拳头]，以及“[骷髅头] 斩杀：抽一张野兽”。"),
"Gnarled and Rugged": ("皮糙肉厚", "给予一个友方单位 +2 生命值。如果它是野兽，同时给予其 +2 护甲。"),
"Monster Hunters": ("怪物猎手", "部署一个猎兽小子；如果你的对手控制一个生命值为 5 或以上的部队，则改为部署 3 个。"),
"Power of the Waaagh!": ("哇！战吼之力", "你的督军获得“群体：直到你的下个回合获得 +1[护甲]”。"),
"Skrag Every Stash!": ("抄光所有藏货！", "对所有敌人造成 1 点伤害，并且他们失去潜行。"),
"Thundering Stampede": ("雷鸣狂奔", "抽一张野兽。你手牌中的所有野兽费用 -1。"),
"Snakebite Battlewagon": ("蛇咬战斗车", "部署一个猎兽小子并给予其先锋。"),
"Beastboss Morgrim": ("野兽头目莫格里姆", "当你部署一只野兽时，本回合给予你的督军 +1[攻击]。"),
"Mozrog Skragbad": ("莫兹罗格·斯克拉格巴德", "践踏。\n天赋：它们越大……"),
"Beast Snagga Nob": ("猎兽老大", "你的回合结束时，给予你手牌中的所有野兽 +1 攻击。"),
"Snakebite Nob": ("蛇咬老大", "给予你手牌中的一个随机野兽 +1 攻击和 +1 生命值。"),
"Unleashed TramplaSquig": ("脱缰践踏猪", "每个回合开始时，攻击一个随机的非飞行敌人。"),
"Weirdboy": ("灵能小子", "不稳定。护盾。天赋：哇！战吼能量。"),
"Da Bigger Dey Iz... (Mozrog's Talent)": ("它们越大……（莫兹罗格天赋）", "临时。对一个敌人造成 1 点伤害。如果目标存活，本回合给予你的督军 +1 攻击、+1 生命值和践踏。"),
"Special Dose (Zodgrod Wortsnagga Talent)": ("特制药剂（佐德格罗德·沃茨纳加天赋）", "给予一个友方步兵或野兽 +3[攻击] 和 +1 生命值。"),
"Waaagh! Energy (Weirdboy Talent)": ("哇！战吼能量（灵能小子天赋）", "临时。对所有友方单位造成 1 点伤害，并本回合给予它们 +2 攻击和 +2 生命值。"),
"Ardshell Gurk": ("硬壳古尔克", "护甲 2。先锋。天赋：高克的追随者。"),
"Banner Nob": ("旗手老大", "相邻单位获得 +1[攻击] 和 +1[武器]。"),
"Big Choppa Nob": ("大砍刀老大", "当一个友方单位触发群体时，会额外触发一次。"),
"Big Krumpaz": ("暴揍打手", "集结：对一个随机敌人造成 3 点伤害。"),
"Big Shoota Boy": ("大枪小子", "对三个随机敌人各造成 1 点伤害。"),
"Bomb Squig": ("炸弹猪", "反噬：对所有敌人造成 1-2 点伤害。"),
"Boss Nob": ("头目老大", "群体：给予相邻单位 +1 生命值。"),
"Burna Boy": ("喷火小子", "集结：如果你控制一个兽人扳手或技师小子加兹梅克，对所有敌人造成 2-3 点伤害。"),
"Deffgun Loota": ("死枪掠夺者", "当另一个部队死亡时，对一个随机敌人造成 1-2 点伤害。"),
"Grot": ("地精", "潮涌。"),
"Hornhelmz Boy": ("角盔小子", ""),
"Kommando": ("敢死队", "潜行。猛击：获得潜行。"),
"Makari the Grot": ("地精马卡里", "相邻单位获得 +1 攻击。反噬：返回你的手牌，并且本回合费用 +2。"),
"Meganob Ugrak": ("重甲老大乌格拉克", "爆裂 2。群体：在你的手牌中生成一个斯鲁格小子。"),
"Mekboy Gazmek": ("技师小子加兹梅克", "天赋：技师狂人。群体：你手牌中的一个随机载具费用 -1。"),
"Ork Boy": ("兽人小子", ""),
"Ork Nob": ("兽人老大", "当你打出一个部队时，给予其侧翼。"),
"Ork Spanner": ("兽人扳手", "给予一个友方载具一个随机增益。"),
"PainBoy Sniklaw": ("痛医官斯尼克拉夫", "集结：治疗一个友方单位 3 点生命值。"),
"Rokkit Boy": ("火箭小子", "不稳定。爆裂 3。"),
"Shoota Boy": ("射枪小子", "群体：对一个随机敌人造成 1-2 点伤害。"),
"Skarboy Nob": ("伤疤老大", "获得 +1[攻击]。"),
"Slugga Boy": ("斯鲁格小子", "每有一个其他友方斯鲁格小子，获得 +1 护甲。"),
"Stikkbomb Boy": ("粘弹小子", "使其所攻击的敌人眩晕。"),
"Stormboy": ("风暴小子", "潮涌 1。飞行。"),
"Tankbusta": ("破坦者", "集结：对一个随机敌方载具造成 4 点伤害。"),
"Trukk Boy": ("卡车小子", "集结：如果你控制一个载具，获得先锋。"),
"Veteran Flyboy": ("飞行老兵", "当你部署一个风暴小子时，给予其侧翼。"),
"Dust Storm": ("尘暴", ""),
"Night Attack": ("夜袭", ""),
"Normal Conditions": ("正常环境", ""),
"Spore Cloud": ("孢子云", ""),
"Ard As Nails": ("硬如钉子", "给予你的单位 +2 生命值。"),
"Attack Squig": ("突袭猪", "对一个敌人造成 1 点伤害并使其眩晕。"),
"Cloud Of Smoke": ("烟幕", "使一个随机敌人失明。每有一个友方载具，重复此效果。"),
"Da Green Horde": ("绿皮大军", "你的部队本回合费用 -1。"),
"Da Irongob": ("铁嘴", "你的督军获得震荡（直到你的下个回合），并治疗 5 点生命值。"),
"Da Red Waaagh": ("红色哇战吼", "抽一个部队并给予其 +1 攻击和 +1 生命值。"),
"Dead Choppy": ("贼能砍", "给予一个友方载具嗜血，或给予另一个友方部队 +2[攻击]。"),
"Extractor Rig": ("回收塔车", "你的下一个载具费用 -2。"),
"Follower of Gork": ("高克的追随者", "给予一个友方部队 +2 攻击和 +2 生命值。"),
"Get'em ladz!": ("上啊，小子们！", "本回合给予你的单位 +2 攻击和 +2 生命值。"),
"Greatest Warboss": ("最伟大的战头目", "部署一个地精，并给予你的部队 +1 护甲。"),
"Gretchin Mob": ("地精帮", "部署 3 个地精并给予它们先锋。"),
"Gretchin Tower": ("地精塔", "部署两个地精。"),
"Grizzled Skarboy": ("久经沙场的伤疤小子", "给予你的督军护甲 1，并且每有一个受伤的敌人，你的督军额外获得 +1 生命值。"),
"Grot Bomb": ("地精炸弹", "对一个随机敌人造成 3 点伤害。"),
"Krump da Gitz": ("揍扁蠢货们！", "你本回合打出的所有部队获得侧翼。"),
"Mekaniak": ("技师狂人", "给予一个友方载具一个由你选择的定制改装。"),
"More Dakka": ("更多火力", "给予一个友方载具 +2 攻击和侧翼。"),
"No Mukkin About": ("别磨蹭！", "摧毁一个随机敌方部队。"),
"Ork Encampment": ("兽人营地", "给予你的部队伪装和护甲 1。"),
"Proper Killy": ("真够狠", "你的督军本回合获得嗜血。"),
"Prophet of the Waaagh": ("哇！战吼先知", "从你的牌库中选择一个战术并抽取它。"),
"Pyromaniaks": ("纵火狂", "本回合给予你的单位爆裂 3。"),
"Ramshackle": ("七拼八凑", "给予一个友方载具 +4 生命值。"),
"Rok Invasion": ("火箭入侵", "部署 8 个费用 4 或以下的随机兽人步兵。"),
"Sawbonez": ("锯骨大夫", "造成 1-2 点伤害。如果目标存活，为其治疗 1-5 点生命值。"),
"Scrag \u2019Em": ("干翻他们！", "给予一个友方部队侧翼。如果它是载具，同时给予其先锋。"),
"Skorcha Assault": ("喷火突击", "对三个随机敌人各造成 2-4 点伤害。"),
"Stomp Em": ("踩扁他们！", "本回合给予你的单位 +1[攻击]，并触发它们的[群体]群体能力。"),
"Stormboyz Strike": ("风暴小子突袭", "部署一个风暴小子。每有一个敌方单位，重复此效果。"),
"Tide of Muscle": ("肌肉狂潮", "抽两个部队并给予它们 +1 攻击和 +1 生命值。"),
"Toxic Bonfire": ("剧毒篝火", "对一个敌方部队造成 2 点伤害并使其眩晕。"),
"Uge Choppa": ("超大砍刀", "临时。你的督军本回合获得“+2[攻击] 和[斩杀]斩杀：治疗 3 点生命值”。"),
"Unbridled Carnage": ("无度屠戮", "对一个随机敌人造成 1-2 点伤害。每有一个友方部队，重复此效果。"),
"Will of Gork": ("高克之志", "摧毁场上的所有部队。"),
"Worst Temper": ("暴脾气", "你的督军本回合获得 +1[攻击]、[护盾]护甲 1 和[翅膀]飞行。"),
"Wreckin Ball": ("拆迁铁球", "给予一个友方载具 +2[攻击] 和震荡。"),
"Battlewagon": ("战斗马车", "你手牌中的一个随机步兵费用 -3。"),
"Boomdakka Snazzwagon": ("爆轰炫车", "不稳定。侧翼。潮涌。"),
"Deff Dread": ("死恐无畏", "不稳定。群体：对一个随机敌人造成 1-3 点伤害。"),
"Deffkopta": ("死亡直升机", "潮涌 2。集结：其他友方死亡直升机对一个随机敌人造成 2 点伤害。"),
"Gorkanaut": ("高克机甲", "护甲 1。"),
"Grot Tank": ("地精坦克", "不稳定。护甲 1。"),
"Gunwagon": ("炮战车", "不稳定。震荡。"),
"Killa Kan": ("杀戮罐", "群体：本回合获得 +1 护甲。"),
"Krumpaklaw": ("暴揍爪", "群体：获得 +2 攻击和 +2 生命值，并获得护甲 1。"),
"Rukkatrukk Squigbuggy": ("隆隆猪车", "护甲 1。不稳定。猛击：部署一个炸弹猪。"),
"Stompa": ("踩踏机甲", "护甲 2。震荡。不稳定。集结：使一个随机敌人眩晕。"),
"Warbiker": ("战争摩托手", "集结：如果你没有控制其他部队，获得迅捷。"),
"Boss Zagstruk": ("头目扎格斯特鲁克", "天赋：暴脾气。"),
"Grukk Face-Rippa": ("古鲁克·撕面者", "你的拥有潮涌的部队获得 +1 攻击。天赋：红色哇战吼。"),
"Warboss Gordrang": ("战头目戈德朗", "天赋：超大砍刀。"),
"Mega Blasta Deffkopta": ("重爆死亡直升机", "不稳定。飞行。爆裂 5。集结：在你的手牌中生成一个地精炸弹。"),
}

def bad_name(n):
    if not n or not n.strip():
        return True
    if re.search(r"[_\d]", n):
        return True
    if re.match(r"^[A-Za-z]{0,2}$", n.strip()):
        return True
    return False

src = json.load(open(SRC, encoding="utf-8"))
out = {}
skipped = []
missing = []
hits = 0
for e in src:
    name = e["name"]
    if bad_name(name):
        skipped.append(name)
        continue
    if name not in T:
        missing.append(name)
        continue
    n, d = T[name]
    out[name] = {"n": n, "d": d}

if missing:
    print("MISSING:", missing)
if skipped:
    print("SKIPPED:", skipped)
json.dump(out, open(OUT, "w", encoding="utf-8"), ensure_ascii=False, indent=0)
print("total cards:", len(src), "output:", len(out), "skipped:", len(skipped))
