# -*- coding: utf-8 -*-
"""g1 切分(极限战士+太空野狼, 214 张)英文名/效果 → 简体中文。
术语表: d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json (规则书提取 246 对)
输出: d:/2/Warpforge_tools/scripts/_tmp_zhcards_g1.json
"""
import json, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8")

TERMS = json.load(open(r"d:/2/Warpforge部队卡片/_tmp_rulebook_terms.json", encoding="utf-8"))
SRC = json.load(open(r"d:/2/Warpforge_tools/scripts/_tmp_slices/g1.json", encoding="utf-8"))

# (中文名, 中文效果)  英文名=切片原名; 跳过名: ' Iron Priest' 左侧多余空格(与 Iron Priest 重复)
T = {
"Logan Grimnar": ("洛根·格里姆纳", "开局时手牌中就有提尔纳克与芬里尔。"),
" Iron Priest": None,  # 杂字符名(前导空格), 跳过
"Aggressor": ("突击者", "猛击：获得 1 点。"),
"Arjac Rockfist": ("阿尔贾克·岩拳", "摧毁被其攻击的、带猎杀标记的任意敌方部队。集结：本回合给予 1 个友方单位无敌。"),
"Blackmane Aggressor": ("黑鬃突击者", "给予所有带兽群的友方单位 +1 生命。"),
"Blackmane Reiver": ("黑鬃掠夺者", "狂暴：抽 1 个部队。给予其 +1 攻击、+1 装甲和 +1 生命。"),
"Blood Claw Pack Leader": ("血爪兽群首领", "给予 1 个随机敌方部队猎杀标记。"),
"Blood Claw": ("血爪", "对 1 个敌人造成 3 点伤害。"),
"Deathwolf Scout": ("死亡狼侦察兵", "伪装。当 1 个友方部队触发狂暴时，对 1 个随机敌方部队造成 1 点伤害。"),
"Fenrisian Wolf": ("芬里斯狼", "攻击 1 个非飞行敌方部队。若其带猎杀标记，获得 +1 装甲。"),
"Frost Claw Wulfen": ("霜爪狼人", "兽群。护甲 1"),
"Fyrri Askar": ("菲里·阿斯卡尔", "你的回合期间，带兽群的友方单位拥有无敌。"),
"Grey Hunter Pack Leader": ("灰猎手兽群首领", "给予 1 个友方部队 +3 攻击、+3 装甲和 +3 生命。"),
"Grey Hunter": ("灰猎手", "部署时，给予 1 个随机敌方部队猎杀标记。"),
"Hrolf the Ironhowl": ("铁嚎·赫罗尔夫", "友方野兽费用 -1。⚡集结：你手牌中的战术变为猎狼或芬里斯狼。"),
"Hunting Wolf": ("猎狼", "狂暴：攻击 1 个随机非飞行敌方部队。"),
"Iron Priest": ("铁祭司", "友方载具拥有再生 1。\n集结：抽 1 张载具。"),
"Long Fang": ("长牙", "当此单位攻击带猎杀标记的敌人时，对相邻敌人造成 3 点伤害。"),
"Morkai Eliminator": ("莫凯歼击者", "给予 1 个随机敌方部队猎杀标记。攻击 1 个带猎杀标记的随机敌人。"),
"Terminator Rune Priest": ("终结者符文祭司", "对所有带猎杀标记的敌人造成 2-3 点伤害。"),
"Thunderwolf Cavalry Pack Leader": ("雷狼骑兵兽群首领", "当 1 个敌人获得猎杀标记时，本回合你的单位获得 +1。集结：给予 1 个敌方部队猎杀标记。"),
"Thunderwolf Cavalry": ("雷狼骑兵", "集结：给予 1 个敌方部队猎杀标记。"),
"Tyrnak and Fenrir": ("提尔纳克与芬里尔", "猛击：治疗 3 点。"),
"Wolf Guard Battle Leader": ("狼卫战斗领袖", "相邻单位拥有兽群。"),
"Wolf Guard Headtaker": ("狼卫斩首者", "手牌生成 1 张猎狼。"),
"Wolf Guard Terminator Pack Leader": ("狼卫终结者兽群首领", "护甲 2。斩杀：获得 +1 攻击、+1 装甲和 +1 生命。"),
"Wolf Guard Terminator": ("狼卫终结者", "先锋。护甲 1"),
"Wolf Priest": ("狼牧师", "当带猎杀标记的敌人死亡时，为 1 个随机友方单位治疗 2 点。\n集结：为 1 个友方单位治疗 2 点。"),
"Wulfen Pack Leader": ("狼人兽群首领", "对 1 个随机敌方部队造成 3 点伤害；若你未控制其他部队，则造成 5 点。"),
"Wulfen": ("狼人", ""),
"Default Conditions": ("默认条件", ""),
"Everstorm": ("永暴", ""),
"First Light": ("初光", ""),
"Full moon": ("满月", ""),
"Anvil of Endurance": ("坚忍之砧", "给予 1 个友方部队护甲 1，或为你的督军治疗 4 点。"),
"Birth of a Saga": ("传奇的诞生", "双方各从其牌库部署 3 个部队。以该方式部署的你的部队本回合获得侧翼和护甲 3。"),
"Bjorn's Shrine": ("比约恩的神龛", "给予 1 个友方单位 +2 生命。本回合它下次使用狂暴时，它将留在场上。"),
"Canis Helix": ("卡尼斯螺旋", "给予 1 个友方部队兽群。"),
"Death Shun": ("死亡闪避", "给予 1 个敌方部队猎杀标记。抽 1 张卡。"),
"Embers of Prospero": ("普罗斯佩罗余烬", "给予 1 个敌方部队猎杀标记。若其已有猎杀标记，则改为摧毁它。"),
"Fenrisian Blizzard": ("芬里斯暴风雪", "对所有单位造成 2 点伤害，并使其失明，直到你的下个回合。"),
"Fenrisian Monstrosities": ("芬里斯巨兽", "对所有单位造成 3 点伤害，并对每个带猎杀标记的敌人额外造成 2 点伤害。"),
"Fenrisian Runestones": ("芬里斯符文石", "为你的督军治疗 4 点。"),
"Fenrisian Wolfpack": ("芬里斯狼群", "为每个敌方单位部署 1 张芬里斯狼。敌方部队上每有 1 个猎杀标记，此卡费用 -1。"),
"Halls of Legend": ("传奇殿堂", "给予 1 个友方单位护盾。抽 1 张卡。"),
"High King of Fenris": ("芬里斯至高王", "抽 1 个部队。使提尔纳克与芬里尔的费用 -1。"),
"High Rune Priest": ("高阶符文祭司", "临时。选择 1 枚符文放入你的手牌。"),
"Hordeslayer": ("灭群者", "给予所有敌方部队猎杀标记。"),
"Hunter's Guile": ("猎人诡计", "抽 2 张卡。每抽到 1 个部队，给予 1 个随机敌方部队猎杀标记。"),
"Legendary Tenacity": ("传奇坚韧", "给予 1 个友方部队 +4 攻击、+4 装甲和 +4 生命。"),
"Let Loose": ("释放", "每个友方野兽本回合获得 +1 攻击，并攻击 1 个随机敌人。"),
"Living Lightning": ("活体闪电", "对 3 个随机敌人各造成 1 点伤害。"),
"Murderous Hurricane": ("致命飓风", "本回合给予 1 个友方单位 +2 攻击。"),
"Orbital Defences": ("轨道防御", "对 1 个敌方部队造成 1 点伤害，并给予它猎杀标记。"),
"Pack of One": ("孤狼兽群", "给予 1 个友方部队 +1 攻击和 +1 生命。若你未控制其他部队，给予它侧翼。"),
"Proper Hunt": ("正式狩猎", "你的督军本回合获得嗜血。"),
"Raid Tactics": ("突袭战术", "本场战斗持续生效：当 1 个友方部队使用狂暴时，对敌方督军造成 2 点伤害。"),
"Saga of the Bold": ("无畏者传奇", "给予 1 个友方部队 +2 攻击和 +2 生命。若你控制 3 个或更多部队，抽 1 张卡。"),
"Savage Legacy": ("野蛮传承", "造成 1 点伤害。若目标死亡，部署 1 个灰猎手。"),
"Tempest's Wrath": ("暴风之怒", "临时。给予 1 个敌方部队 -2 攻击，直到你的下个回合。"),
"The Fang": ("巨牙", "选择其一：部署 1 个灰猎手；为 1 个友方单位治疗 4 点；或抽 2 张卡。"),
"Unbridled Fury": ("无拘狂怒", "给予你的步兵 +2 攻击和 -2 装甲，直到你的下个回合。"),
"War Howl": ("战嚎", "临时。对 1 个敌人造成 1 点伤害。若其为部队，给予它猎杀标记。"),
"Bjorn the Fell-Handed": ("比约恩·凶爪", "护甲 1。当 1 个友方单位使用狂暴时，它留在场上。狂暴：对所有敌人造成 2-3 点伤害。"),
"Blackmane Inceptor": ("黑鬃疾袭者", "猛击：若目标是部队且存活，给予它猎杀标记。"),
"Fenrisian Drop Pod": ("芬里斯空降舱", "狂暴：部署 2 个灰猎手。"),
"Land Rider": ("兰德骑手", "先锋"),
"Murderfang": ("弑杀之牙", "斩杀：治疗 2 点，并本回合获得嗜血。"),
"Stormwolf": ("风暴狼", "飞行。当带猎杀标记的敌人死亡时，部署 1 个灰猎手。"),
"Venerable Dreadnought": ("尊贵无畏机甲", "当 1 个敌人获得猎杀标记时，对其及其相邻单位造成 1-2 点伤害。"),
"Vindicator": ("复仇者", "集结：对 1 个敌人造成 3 点伤害；若其带猎杀标记，则造成 6 点。"),
"Wulfen Dreadnought": ("狼人无畏机甲", "护甲 1。兽群。集结：给予所有带兽群的友方单位 +2 攻击。"),
"Njal Stormcaller": ("尼亚尔·风暴召唤者", "当你触发狂暴时，本回合高阶符文祭司费用 -1。天赋：高阶符文祭司。"),
"Ragnar Blackmane": ("拉格纳·黑鬃", ""),
"Lieutenant Titus": ("提图斯中尉", "天赋：模范战士。"),
"Desolation Marine": ("荒芜战士", "爆裂 3"),
"2nd Company Terminator": ("第二连终结者", "护甲 1。爆裂 3。誓言 2：造成 3 点伤害。"),
"Aggressor Sergeant": ("突击者军士", "爆裂 2"),
"Antaro Chronus": ("安塔罗·克罗努斯", "当你部署载具时，给予它侧翼。\n天赋：马库拉格之矛。"),
"Apothecary Polixis": ("药剂师波利克西斯", "典籍：为本部队及其相邻单位治疗 2 点。"),
"Assault Centurion": ("突击百夫长", "先锋。护甲 2"),
"Assault Intercessor": ("突击仲裁者", "先锋"),
"Bladeguard Ancient": ("剑卫老兵", "你的其他单位 +2 攻击。誓言 1：给予 1 个友方部队护甲 1。"),
"Bladeguard Lieutenant": ("剑卫中尉", "护甲 1。先锋。典籍：治疗 3 点。"),
"Bladeguard Sergeant": ("剑卫军士", "护甲 1。集结：若你未控制其他部队，获得护甲 1。"),
"Captain Sicarius": ("西卡里乌斯队长", "典籍：治疗 5 点并获得先锋，直到你的下个回合。天赋：丰功伟绩。"),
"Chairon": ("凯隆", "对 1 个随机敌人造成 1-2 点伤害。自你的上个回合以来，每有 1 个友方部队死亡，重复此效果。"),
"Chaplain Cassius": ("牧师卡西乌斯", "友方部队的誓言能力额外结算 1 次。\n天赋：死亡教义。"),
"Chapter Champion": ("战团冠军", "护甲 1。典籍：获得 +2 攻击。誓言 2：获得护甲 1 和先锋。"),
"Company Ancient": ("连队老兵", "你的其他单位 +1 近战攻击和 +1 远程攻击。"),
"Devastator Centurion": ("毁灭者百夫长", "典籍：获得护甲 1。"),
"Devastator Marine": ("毁灭者战士", "4：对 1 个敌方部队造成 5 点伤害。"),
"Eliminator Sergeant": ("歼击者军士", "当你部署歼击者时，给予它潜行。"),
"Eliminator": ("歼击者", "伪装。狙击"),
"Epistolary Librarian": ("智典智库", "抽 1 张卡，并给予你的督军护盾。"),
"Ferren Areios": ("费伦·阿雷奥斯", "友方部队的誓言能力每回合最多可激活 3 次。誓言 1：造成 1 点伤害。"),
"Firstborn": ("初生者", "典籍：获得 +1 近战攻击、+1 远程攻击和 +1 生命。"),
"Heavy Intercessor": ("重装仲裁者", "护甲 1。典籍：本回合获得 +1 装甲。"),
"Hellblaster": ("炼狱爆弹兵", "爆裂 2"),
"Honour Guard": ("荣誉卫队", "相邻单位拥有护甲 1。"),
"Inceptor Sergeant": ("疾袭者军士", "给予你的其他部队 +1 攻击。"),
"Incursor": ("渗透者", "集结：造成 1 点伤害。誓言 2：使 1 个敌人眩晕。"),
"Intercessor Sergeant": ("仲裁者军士", "典籍：对 1 个随机敌方部队造成 1 点伤害。"),
"Lieutenant Calsius": ("卡尔修斯中尉", "典籍：对敌方督军造成 3 点伤害。"),
"Lieutenant with Combi-Weapon": ("组合武器中尉", ""),
"Octavio Infiltrator": ("奥克塔维奥·渗透者", "集结：给予 1 个受伤敌人失明。"),
"Phobos Librarian": ("暗影智库", "3：对 1 个敌人造成 2-4 点伤害。若目标死亡，获得护盾。"),
"Phobos Lieutenant": ("暗影中尉", "远射。誓言 1：获得潜行。斩杀：手牌生成 1 张随机极限战士卡。"),
"Primaris Chaplain": ("原铸牧师", "给予你的步兵部队 +1 近战攻击和 +1 远程攻击。"),
"Primaris Eradicator": ("原铸歼灭者", "护甲 1。典籍：对 1 个带护甲的随机敌人造成 4 点伤害。"),
"Primaris Inceptor": ("原铸疾袭者", "典籍：本回合获得 +1 攻击。"),
"Primaris Intercessor": ("原铸仲裁者", ""),
"Primaris Judiciar": ("原铸裁决者", "使 1 个随机敌人眩晕。"),
"Primaris Reiver": ("原铸掠夺者", "侧翼"),
"Primaris Techmarine": ("原铸技术军士", "给予你的载具护甲 1，并治疗 2 点。"),
"Scout Sniper": ("侦察狙击手", "1：造成 1 点伤害。"),
"Scout": ("侦察兵", "伪装。集结：对 1 个敌人造成 1 点伤害。"),
"Sergeant Allectius": ("阿莱克修斯军士", "使 1 个随机敌人失明。"),
"Sergeant Gadriel": ("加德里尔军士", "典籍：你的每个单位对 1 个随机敌人各造成 1-2 点伤害。"),
"Sergeant Telion": ("特利昂军士", "使你手牌中 1 个随机步兵的费用 -1。"),
"Sternguard Sergeant": ("铁卫军士", "典籍：对 1 个随机敌人造成 2 点伤害。"),
"Suppressor": ("压制者", "飞行。哨戒 1。誓言 1：选择 1 张本场战斗中打出过的非传说战术，将其返回你的手牌。"),
"Tactical Marine": ("战术战士", "誓言 3：获得 +3 攻击、+3 装甲和 +3 生命。"),
"Terminator Captain": ("终结者队长", "护甲 2。"),
"Terminator": ("终结者", ""),
"Tyrannic War Veteran": ("泰伦战争老兵", "集结：所有敌人失去潜行和伪装。"),
"Vico Therbeus": ("维科·瑟贝乌斯", "友方部队的誓言能力可在后续回合激活。誓言 1：获得伪装。"),
"Fleet Support": ("舰队支援", ""),
"Normal Conditions": ("正常条件", ""),
"Orbital Bombardment": ("轨道轰炸", ""),
"Thunderstorm": ("雷暴", ""),
"Adaptive Strategy": ("适应性战略", "给予所有友方部队 +1 攻击。誓言 1：给予所有友方部队 +1 生命。"),
"Angel's Wrath": ("天使之怒", "临时。给予 1 个友方单位 +1，直到你的下个回合。誓言 2：本回合给予它护甲 2。"),
"Angels of Death": ("死亡天使", "部署 3 个原铸仲裁者并给予它们侧翼。典籍：给予你的原铸仲裁者 +1。"),
"Might of Heroes": ("英雄之力", "给予 1 个友方单位护盾。"),
"Armoured Offensive": ("装甲攻势", "手牌生成 3 张极限战士载具。它们费用 -1。"),
"Armoured Support": ("装甲支援", "本场战斗持续生效：给予你放入场上的载具护甲 1。"),
"Author of the Codex": ("典籍作者", "触发 1 个友方单位的典籍能力，并选择 1 张法典条款放入你的手牌。"),
"Avenging Zeal": ("复仇热忱", "给予你场上和手牌中的所有单位 +2 生命和 +2 攻击。当 1 个友方部队或战术触发典籍时，此卡费用 -1。"),
"Battlefield Supremacy": ("战场霸权", "使你手牌和牌库中所有载具的费用 -1。从你的牌库抽 1 张载具。"),
"Catechism of Death": ("死亡教义", "临时。给予 1 个友方部队 +2，或本回合给予你的督军 +2。誓言 3：额外给予它 +2。"),
"Champions of Humanity": ("人类冠军", "给予所有友方单位 +2 生命。誓言 4：额外给予它们 +3 生命。"),
"Chapter Council": ("战团议会", "从你的牌库选择 1 个部队并抽取。为你的督军治疗 6 点。"),
"Codex Discipline": ("典籍纪律", "给予你的部队 +2 远程攻击，并触发它们的典籍能力。"),
"Death from Above": ("天降死神", "造成 4 点伤害。\n典籍：额外造成 1 点伤害。"),
"Duty's End": ("职责终焉", "给予 1 个友方部队「💀反噬：触发所有友方单位的典籍能力」。"),
"Exhortation of Rage": ("狂怒劝诫", "临时。给予你的部队 +1。若你未控制任何部队，抽 1 个部队。"),
"Fall Back": ("撤退", "将 1 个友方部队返回你的手牌。誓言 2：使其费用 -2，并给予它侧翼。"),
"Firestrike Turrets": ("火击炮塔", "对 1 个敌人造成 2 点伤害。"),
"Forward Deployment": ("前出部署", "给予 1 个友方部队侧翼、+1 攻击和 +1 装甲。"),
"Gather the Company": ("集结战团", "抽 3 张卡。每抽到 1 个步兵，回复 1 点能量。"),
"Greatest Deeds": ("丰功伟绩", "使你手牌中所有卡的费用 -2。"),
"Hero's Respite": ("英雄的喘息", "抽 2 张卡。"),
"Hive Factory": ("蜂巢工厂", "本回合你的下一张卡费用 -1。"),
"Humanity's Shield": ("人类之盾", "你的督军变为无敌，直到你的下个回合。"),
"Indomitus Crusade": ("不屈远征", "部署 1 个费用 6 或更高的极限战士部队。誓言 6：对所有敌方部队造成等同于其近战攻击的伤害。"),
"Inspired Retribution": ("神启报复", "摧毁 1 个敌方部队。"),
"Knights of Macragge": ("马库拉格骑士", "给予你的部队 +1 近战攻击、+1 远程攻击和 1 生命。"),
"Lead From the Front": ("身先士卒", "选择 1 张非传说极限战士卡，在手牌中生成 2 张复制。"),
"Light Cover": ("轻型掩体", "给予 1 个友方步兵护甲 1 和伪装。"),
"Master Tactician": ("战术大师", "抽 1 张卡。典籍：你的督军本回合获得护甲 1。"),
"Master of Arcana": ("秘法大师", "选择 1 张极限战士灵能法术放入你的手牌。"),
"Master of Arms": ("兵械大师", "你的督军本回合获得 +1 攻击和爆裂 2。"),
"Master of Battle": ("战斗大师", "临时。给予 1 个友方单位哨戒 1，直到你的下个回合。"),
"Master of the Fleet": ("舰队大师", "对 1 个敌人及其相邻单位造成 1 点伤害。"),
"No Mercy": ("绝不留情", "对 1 个敌方部队造成 4 点伤害。誓言 4：额外摧毁所有受伤的敌方部队。"),
"No Respite": ("不得喘息", "给予所有友方单位哨戒 1。誓言 3：手牌生成 1 张不得喘息。"),
"Oath of Moment": ("誓言时刻", "给予 1 个友方部队 +2 生命，并给予它「猛击：触发本部队的典籍能力」。"),
"Paragon of Ultramar": ("极限战士典范", "本回合给予你的部队 +1 和 +1。"),
"Pariah Vanguard": ("弃儿先锋", "给予 1 个友方部队 +2 近战攻击和先锋。"),
"Point-Blank Shot": ("抵近射击", "造成 2 点伤害。若目标死亡，抽 1 张卡。"),
"Primarch of the XIII": ("第十三军团基因原体", "本回合给予 1 个友方单位 +1 和 +1。"),
"Rapid Deployment": ("快速部署", "选择你手牌中 1 个部队。使其费用 -2。"),
"Righteous Fury": ("正义之怒", "为你的督军治疗 1 点，并给予它 +1 攻击，直到你的下个回合。"),
"Sacred Bolter": ("神圣爆弹枪", "给予 1 个友方部队 +2 远程攻击。"),
"Scryer's Gaze": ("预言者的凝视", "抽 1 张战术。"),
"Shall Know No Fear": ("不知畏惧", "本回合给予 1 个友方部队无敌。誓言 8：给予它 +8 攻击、+8 生命和 +8 装甲。"),
"Sheltered Location": ("隐蔽位置", "给予你的部队伪装。"),
"Smite": ("天罚", "造成 1-3 点伤害。"),
"Spear of Macragge": ("马库拉格之矛", "抽 1 张载具，并给予它护甲 2。"),
"Stormhawk Interception": ("风暴鹰拦截", "对 1 个敌方单位及其相邻单位造成 2 点伤害。誓言 3：重复此效果。"),
"Stormraven": ("风暴鸦", "对所有敌人造成 2 点伤害。"),
"Supreme Strategist": ("至高战略家", "给予 1 个友方单位护甲 1 和先锋，直到你的下个回合。"),
"Tactical Insight": ("战术洞察", "回复 2 点能量。"),
"The Chapter's Due": ("战团之偿", "选择 1 个本场战斗中死亡的友方步兵，将其返回你的牌库。抽 1 张卡。"),
"7th Company Bike": ("第七连摩托", "集结：对 1 个敌人造成 2 点伤害。"),
"Attack Bike": ("攻击摩托", "誓言 2：获得 +1 和侧翼。"),
"Brutalis Dreadnought": ("蛮横无畏机甲", "誓言 2：对所有敌人造成 3 点伤害。"),
"Dreadnought": ("无畏机甲", "誓言 2：对 1 个敌人及其相邻单位造成 2 点伤害。"),
"Firestrike Servo Turret": ("火击伺服炮塔", "3：获得哨戒 2。"),
"Invictor Warsuit": ("无敌者机甲", "当敌人攻击时，对其造成 2 点伤害。"),
"Land Speeder": ("兰德飞车", "飞行。猛击：抽 1 张卡。"),
"Outrider": ("前哨骑兵", "典籍：获得 +1 攻击和 +1 装甲。"),
"Predator Destructor": ("捕食者毁灭者", ""),
"Primaris Impulsor": ("原铸脉冲装甲车", "典籍：部署 1 个原铸仲裁者。"),
"Primaris Invader": ("原铸入侵者", "典籍：本回合给予你所有载具 +1 远程攻击。"),
"Redemptor Dreadnought": ("救赎者无畏机甲", "对所有敌人造成 1-2 点伤害。"),
"Repulsor Executioner": ("处决者装甲车", "爆裂 4。集结：对 1 个敌方部队造成 8 点伤害。"),
"Scout bike": ("侦察摩托", "当 1 个敌人被部署时，对其造成 2 点伤害。"),
"Storm Speeder Hailstrike": ("风暴速攻艇·雹击者", "典籍：对所有敌人造成 1 点伤害。"),
"Stormtalon": ("风暴利爪", "对 1 个随机敌方部队造成 3 点伤害。"),
"Valtus": ("瓦图斯", "护甲 1。当 1 个友方单位攻击时，对攻击目标造成 3 点伤害。"),
"Whirlwind": ("旋风", "哨戒 4。其他友方单位拥有哨戒 2。"),
"Chaplain Letharius": ("牧师莱塔里乌斯", "为相邻单位治疗 1 点。天赋：狂怒劝诫。"),
"Marneus Calgar": ("马涅乌斯·卡尔加", "天赋：战术大师。"),
"Roboute Guilliman": ("罗伯特·基里曼", "典籍：治疗 1 点。天赋：典籍作者。"),
"Uriel Ventris": ("乌列尔·文特里斯", "天赋：舰队大师。"),
"Valius Paxor": ("瓦利乌斯·帕克索", "你的回合期间拥有飞行。天赋：天使之怒。"),
"Varro Tigurius": ("瓦罗·提古里乌斯", "天赋：秘法大师。"),
"Assault Doctrine": ("突击教义", "给予 1 个友方单位 +3。典籍：给予它护甲 1，并治疗其 2 点。"),
"Devastator Doctrine": ("毁灭者教义", "本回合给予 1 个友方单位爆裂 3。典籍：本回合额外给予它 +2。"),
"Tactical Doctrine": ("战术教义", "本回合给予你的单位 +3。典籍：抽 1 张卡。"),
"Exemplary Warrior": ("模范战士", "你的督军治疗 1 点，并选择 1 个效果。"),
"Predator Annihilator": ("捕食者歼灭者", "对攻击力最高的敌人造成 3 点伤害。"),
}

def main():
    out = {}
    name_hits = 0
    key_hits = 0
    skip = []
    for c in SRC:
        en = c["name"]
        if en not in T:
            skip.append(("MISSING_T", en))
            continue
        tv = T[en]
        if tv is None:
            skip.append(("SKIP", en))
            continue
        name, desc = tv
        # 术语表命中统计: 卡名命中
        if TERMS.get(en.strip()) is not None:
            name_hits += 1
        # 效果文本中出现的术语表键统计
        low = en.strip()
        for k in TERMS:
            if k == en.strip() or (k.lower() in desc.lower() and len(k) >= 4):
                key_hits += 1
        out[en] = {"n": name, "d": desc}
    j = json.dumps(out, ensure_ascii=False, indent=0)
    with open(r"d:/2/Warpforge_tools/scripts/_tmp_zhcards_g1.json", "w", encoding="utf-8") as f:
        f.write(j)
    print("output cards:", len(out), "| skip:", len(skip))
    for s in skip:
        print("  ", s)
    print("term-hit names:", name_hits)
    # 校验: 所有 desc 都以中文标点/关键词开头, 无英文残留感
    bad = []
    for en, tv in out.items():
        d = tv["d"]
        if d and (d[0].isascii() and not d[0].isdigit()):
            bad.append((en, d[:20]))
    for b in bad:
        print("  CHECK-START:", b)
    # 输出非空描述卡数
    nonempty = sum(1 for v in out.values() if v["d"])
    print("desc non-empty:", nonempty, "| empty:", len(out) - nonempty)

if __name__ == "__main__":
    main()
