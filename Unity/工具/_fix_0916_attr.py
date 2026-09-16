# -*- coding: utf-8 -*-
"""2026-09-16 第二批：**`±N` 后面的属性与卡面图标不符**（加成静默加在错属性上）。
判据：4 个子代理**逐张开 PnP 成品卡图**核过（拳=近战 · 紫枪=远程 · 生命一律写英文词、
护甲从不跟在 `±N` 后面）。28 张要改、2 张（`TAU_Grisly_Feast`/`UM96`）**我们是对的、不改**。

⚠️ `desc` 用**裸词** `Attack`/`Ranged`（同 `Simulacrum Imperialis` 那批的约定）；
   `descZh` **保持该卡原本的写法**（原来是 `[攻击]` 方括号的仍用 `[远程]`，原来是词的仍用词）。
"""
import io, json, collections

P = r'd:/4/Unity/数据/游戏数据/cardface_fixes.json'
b = io.open(P, 'rb').read()
assert b.count(b'\r\n') == b.count(b'\n')
d = json.loads(b.decode('utf-8'), object_pairs_hook=collections.OrderedDict)
desc, descZh = d['desc'], d['descZh']

# (卡名, 旧 desc, 新 desc, 旧 descZh 或 None, 新 descZh 或 None)
EDITS = [
    ('Cadian Standard Bearer', 'Rally: Deploy a Shock Trooper. Duty: Give +2 [Attack] to your units this turn',
     'Rally: Deploy a Shock Trooper. Duty: Give +2 Ranged to your units this turn',
     '集结：部署一个震荡突击兵。职责：本回合给予你的单位 +2 [攻击]。',
     '集结：部署一个震荡突击兵。职责：本回合给予你的单位 +2 [远程]。'),
    ('Rogal Dorn Tank', 'Regiment: Give +1 [Attack] to your units this turn.',
     'Regiment: Give +1 Ranged to your units this turn.',
     '团：本回合给予你的单位 +1 [攻击]。', '团：本回合给予你的单位 +1 [远程]。'),
    ('Tempestor Sergeant',
     'Regiment: Give +1 Attack to a random friendly troop. Duty: Give +1 Health to all friendly Infantry.',
     'Regiment: Give +1 Ranged to a random friendly troop. Duty: Give +1 Health to all friendly Infantry.',
     '团：给予一个随机友方部队 +1 攻击。职责：给予所有友方步兵 +1 生命。',
     '团：给予一个随机友方部队 +1 远程攻击。职责：给予所有友方步兵 +1 生命。'),
    ('Forged Killers', 'Give +2 [Attack] to a friendly troop and adjacent troops',
     'Give +2 Ranged to a friendly troop and adjacent troops',
     '给予一个友方部队及其相邻部队 +2 [攻击]。', '给予一个友方部队及其相邻部队 +2 [远程]。'),
    ('Supreme Grand Master',
     "Ephemeral. Give +1 [Attack] and +1 [Armor] to your troops. If you don't control any, create an Intercessor in your hand.",
     "Ephemeral. Give +1 Attack and +1 Ranged to your troops. If you don't control any, create an Intercessor in your hand.",
     '临时。给予我方部队 +1 攻击与 +1 护甲。若你未控制任何部队，将一张仲裁者置入手牌。',
     '临时。给予我方部队 +1 近战攻击与 +1 远程攻击。若你未控制任何部队，将一张仲裁者置入手牌。'),
    ('Relic Munitions', 'Give +1 [Attack] to your units this turn. Repeat for each Secret you played this game',
     'Give +1 Ranged to your units this turn. Repeat for each Secret you played this game',
     '本回合给予你的单位 +1 攻击。本局你每打出一张隐秘牌，重复一次。',
     '本回合给予你的单位 +1 远程攻击。本局你每打出一张隐秘牌，重复一次。'),
    ('Deathwing Assault', 'Give +2 Attack, +2 Armor and +2 Health to your troops and trigger their Teleport abilities.',
     'Give +2 Attack, +2 Ranged and +2 Health to your troops and trigger their Teleport abilities.',
     '给予我方部队 +2 攻击、+2 护甲与 +2 生命，并触发其传送能力。',
     '给予我方部队 +2 近战攻击、+2 远程攻击与 +2 生命，并触发其传送能力。'),
    ('Deathwing Strikemaster', 'Armour 1. Teleport: Give +1, +1 and +1 Health to all friendly troops',
     'Armour 1. Teleport: Give +1 Attack, +1 Ranged and +1 Health to all friendly troops',
     None, None),
    ('Sonic Blaster Noise Marine', 'Stun enemy troops attacked and give them -1 [armor] and -1 [attack]',
     'Stun enemy troops attacked and give them -1 Attack and -1 Ranged',
     '被本单位攻击的敌方部队获得眩晕，并获得 -1 护甲和 -1 攻击。',
     '被本单位攻击的敌方部队获得眩晕，并获得 -1 近战和 -1 远程。'),
    ('Disharmonist', 'Give -2 [Attack] and -2 [Armor] to a random enemy troop',
     'Give -2 Attack and -2 Ranged to a random enemy troop',
     '给予一个随机敌方部队 -2 攻击和 -2 护甲。', '给予一个随机敌方部队 -2 近战和 -2 远程。'),
    ('Antrak Silk', 'Deal 1 damage to a friendly unit and give it +1 [Attack] and +1 [Armor] this turn',
     'Deal 1 damage to a friendly unit and give it +1 Attack and +1 Ranged this turn',
     '对一个友方单位造成 1 点伤害，并使其本回合获得 +1 攻击和 +1 护甲。',
     '对一个友方单位造成 1 点伤害，并使其本回合获得 +1 近战和 +1 远程。'),
    ('Alluress', 'Rally: Give -2 [Health] and -2 [Attack] to an enemy troop. Friendly Daemonette have Flank.',
     'Rally: Give -2 Attack and -2 Ranged to an enemy troop. Friendly Daemonette have Flank.',
     '集结：给予一个敌方部队 -2 生命和 -2 攻击。友方恶魔少女具有侧翼。',
     '集结：给予一个敌方部队 -2 近战和 -2 远程。友方恶魔少女具有侧翼。'),
    ('Acolyte Heavy', 'Ambush: Gain +2 armor. Strike: Create a random Sabotage in the enemy hand',
     'Ambush: Gain +2 Attack. Strike: Create a random Sabotage in the enemy hand',
     '伏击：获得 +2 护甲。猛击：在对手手牌中生成一张随机破坏卡。',
     '伏击：获得 +2 近战攻击。猛击：在对手手牌中生成一张随机破坏卡。'),
    ('Neophyte Trooper', 'Uprising: Gain Flank and +1', 'Uprising: Gain Flank and +1 Ranged', None, None),
    ('Acolyte Hybrid', 'Ambush: Gain +2 and +2', 'Ambush: Gain +2 Attack and +2 Ranged', None, None),
    ('Slugga Boy', 'Gain +1 Armor for each other friendly Slugga Boy',
     'Gain +1 Attack for each other friendly Slugga Boy',
     '每有一个其他友方斯鲁格小子，获得 +1 护甲。', '每有一个其他友方斯鲁格小子，获得 +1 近战攻击。'),
    ('Beast Snagga Boy', 'Tide 1. Stomp. Rally: If you control a Beast, gain +1 Health.',
     'Tide 1. Stomp. Rally: If you control a Beast, gain +1 Attack.',
     '潮涌 1。践踏。集结：如果你控制一只野兽，获得 +1 生命值。',
     '潮涌 1。践踏。集结：如果你控制一只野兽，获得 +1 近战攻击。'),
    ('Nob on Smasha Squig', 'Other friendly Beasts have +1 [attack] and +1 [health].',
     'Other friendly Beasts have +1 [attack] and +1 [ranged].',
     '其他友方野兽获得 +1[攻击] 和 +1[生命值]。', '其他友方野兽获得 +1[攻击] 和 +1[远程]。'),
    ('Gnarled and Rugged', "Give +2 Health to a friendly unit. If it's a Beast, give it +2 Armor as well.",
     "Give +2 Health to a friendly unit. If it's a Beast, give it +2 Attack as well.",
     '给予一个友方单位 +2 生命值。如果它是野兽，同时给予其 +2 护甲。',
     '给予一个友方单位 +2 生命值。如果它是野兽，同时给予其 +2 近战攻击。'),
    ('Greatest Warboss', 'Deploy a Grot and Give +1 Armor to your troops',
     'Deploy a Grot and Give +1 Attack to your troops',
     '部署一个地精，并给予你的部队 +1 护甲。', '部署一个地精，并给予你的部队 +1 近战攻击。'),
    ('More Dakka', 'Give +2 Attack and Flank to a friendly Vehicle',
     'Give +2 Ranged and Flank to a friendly Vehicle',
     '给予一个友方载具 +2 攻击和侧翼。', '给予一个友方载具 +2 远程攻击和侧翼。'),
    ("Tempest's Wrath", 'Ephemeral. Give -2 Attack to an enemy troop until your next turn.',
     'Ephemeral. Give -2 Ranged to an enemy troop until your next turn.',
     '临时。给予 1 个敌方部队 -2 攻击，直到你的下个回合。',
     '临时。给予 1 个敌方部队 -2 远程攻击，直到你的下个回合。'),
    ('Blackmane Reiver', 'Ferocity: Draw a troop. Give it +1 Attack, +1 Armor, and +1 Health.',
     'Ferocity: Draw a troop. Give it +1 Attack, +1 Ranged, and +1 Health.',
     '狂暴：抽 1 个部队。给予其 +1 攻击、+1 装甲和 +1 生命。',
     '狂暴：抽 1 个部队。给予其 +1 近战攻击、+1 远程攻击和 +1 生命。'),
    ('Grey Hunter Pack Leader', 'Give +3 Attack, +3 Armor and +3 Health to a friendly troop.',
     'Give +3 Attack, +3 Ranged and +3 Health to a friendly troop.',
     '给予 1 个友方部队 +3 攻击、+3 装甲和 +3 生命。',
     '给予 1 个友方部队 +3 近战攻击、+3 远程攻击和 +3 生命。'),
    ('Wolf Guard Terminator Pack Leader', 'Armour 2. Slay: Gain +1 Attack, +1 Armour and +1 Health.',
     'Armour 2. Slay: Gain +1 Attack, +1 Ranged and +1 Health.',
     '护甲 2。斩杀：获得 +1 攻击、+1 装甲和 +1 生命。',
     '护甲 2。斩杀：获得 +1 近战攻击、+1 远程攻击和 +1 生命。'),
    ('Unbridled Fury', 'Give +2 [Attack] and -2 [Armor] to your Infantry until your next turn',
     'Give +2 Attack and -2 Ranged to your Infantry until your next turn',
     '给予你的步兵 +2 攻击和 -2 装甲，直到你的下个回合。',
     '给予你的步兵 +2 近战攻击和 -2 远程攻击，直到你的下个回合。'),
    ('Simulacrum Bearer', 'Give +1 [Armor] to your troops, and to your Warlord this turn',
     'Give +1 Attack to your troops, and to your Warlord this turn',
     '给予你的部队 +1 [护甲]，本回合同样给予你的督军',
     '给予你的部队 +1 [攻击]，本回合同样给予你的督军'),
    ('Seraphim', '3: Gain +1 and +1 Health', '3: Gain +1 Ranged and +1 Health', None, None),
    ('Apex Predator', 'Give Invulnerable and +3 Attack to a friendly unit this turn',
     'Give Invulnerable and +3 Ranged to a friendly unit this turn',
     '给予 1 个友方单位本回合无敌和 +3 攻击',
     '给予 1 个友方单位本回合无敌和 +3 远程攻击'),
    ('Attack Bike', 'Oath 2: Gain +1 and Flank', 'Oath 2: Gain +1 Ranged and Flank',
     '誓言 2：获得 +1 和侧翼。', '誓言 2：获得 +1 远程攻击和侧翼。'),
    ('Devastator Doctrine', 'Give Blast 3 to a friendly unit this turn. Codex: Give it +2 this turn as well',
     'Give Blast 3 to a friendly unit this turn. Codex: Give it +2 Ranged this turn as well',
     '本回合给予 1 个友方单位爆裂 3。典籍：本回合额外给予它 +2。',
     '本回合给予 1 个友方单位爆裂 3。典籍：本回合额外给予它 +2 远程攻击。'),
]

n = 0
for name, old_d, new_d, old_z, new_z in EDITS:
    desc[name] = new_d
    if new_z is not None:
        descZh[name] = new_z
    n += 1

d['_manual_desc_note']['_2026-09-16_属性图标_第二批'] = (
    '2026-09-16 **「`±N` 后面的属性与卡面图标不符」批（28 张）** —— 与上一批同一族缺陷：'
    '我们 `desc` 的 token 经 `GivePayload.ReAttr` 会算成 A 属性，而卡面画的是 B 图标 ⇒ '
    '**加成静默加在错属性上**（数值看着对、另一个属性一点没加）。'
    '**证据链**：4 个子代理**逐张开 PnP 成品卡图**核过（部分还做了像素级环色+字形比对），'
    '先导是一次全池对账（`_合并总表.md` 的独立开图抄录 vs 我们的 token）。'
    '⚠️ 对账本身的**误报也吃了两张**：`Grisly Feast`（卡面确有 `+1[拳]`，抄录漏了）与 '
    '`Shall Know No Fear`（卡面确有 `+8[拳] and +8[枪]` 两个）—— **我们是对的、没动**。'
    '写法一律**裸词 `Attack`/`Ranged`**（同 `Simulacrum Imperialis` 那批的约定），'
    '**不是**去放宽 `GivePayload.ReAttr` 的 `Armor` 词表（放宽会连带打到真护甲卡）。'
    '⚠️ 顺带：`Deathwing Strikemaster`/`Neophyte Trooper`/`Acolyte Hybrid`/`Seraphim`/`Attack Bike`/'
    '`Devastator Doctrine` 这几张原来是**裸 `+N`**（`GivePayload` 兜底当近战），现在补成有词。')

d['_manual_descZh_note']['_2026-09-16_属性图标_第二批'] = (
    '2026-09-16 同上批（**只改显示层中文**）。凡是中文里写错属性的（「护甲/装甲/生命/攻击」）'
    '都跟着英文一起改成「近战攻击/远程攻击/近战/远程」。'
    '⚠️ **写法沿用该卡原本的风格**：原来用方括号 token 的（`[攻击]`）仍写成 `[远程]`，'
    '原来是词的仍写词 —— 因为 `card_icon_plan.json` 是按 token 画图标的，换风格会丢图标。'
    '⚠️ 有几张**中文本来就是对的**（`Deathwing Strikemaster`/`Neophyte Trooper`/`Acolyte Hybrid`/'
    '`Seraphim` 的中文早写「近战攻击/远程攻击」）—— 说明当初翻译时看的是卡图、错只错在英文 `desc`。')

io.open(P, 'wb').write(
    json.dumps(d, ensure_ascii=False, indent=1).replace('\n', '\r\n').encode('utf-8'))
print('OK', n, 'cards')
