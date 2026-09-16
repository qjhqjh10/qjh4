# -*- coding: utf-8 -*-
"""2026-09-16 「卡面字与数据」尾巴批：把核过的修正写进 cardface_fixes.json 的手工列。
判据：逐张开 PnP 成品卡图（铁律 7）。跑完要跟 gen_cards_engine.py + gen_icon_plan.py。
"""
import io, json, collections

P = r'd:/4/Unity/数据/游戏数据/cardface_fixes.json'
b = io.open(P, 'rb').read()
assert b.count(b'\r\n') == b.count(b'\n'), '行尾不是纯 CRLF，先停下'
d = json.loads(b.decode('utf-8'), object_pairs_hook=collections.OrderedDict)
desc, descZh = d['desc'], d['descZh']

# ① 英文 desc：±N 后面的属性被 OCR 读成 armor，卡面实为紫圈枪=远程（6 张，逐张开图核过）
desc['Ancient Reliquary']           = 'Give +3 [Attack], +3 Ranged or +3 Health to a friendly troop'
desc['Legendary Tenacity']          = 'Give +4 [Attack], +4 Ranged and +4 Health to a friendly troop'
desc['Moment of Grace']             = 'Give +2 Attack and +2 Ranged to a friendly unit this turn. 5 Energy: Draw a card'
desc['Imagifier']                   = 'Pray: Give +1 attack, +1 ranged and Flank to a friendly troop.'
desc['Celestian Sacresant Aveline'] = 'Pray: Give +1 [attack] and +1 [ranged] to a friendly troop. Talent: Daemonbreaker'
desc['Thunderous Charge']           = 'Give +2 [attack], +2 [ranged] and Concussive to a friendly troop'

# ② 英文 desc：整句与卡面不符 / 缺句首前缀
desc['Prayer']            = 'Give Shield to a friendly unit. If the unit has Pray gain 1 \u2600'
desc['Primaris Chaplain'] = 'Codex: Give +1 Melee and +1 Ranged Attack to your Infantry troops'
desc['Exorcist']          = '8 [faith]: Deal 5 damage to all enemies'

# ③ 中文 descZh：灵魂石 / 信仰费用前缀 —— 漏印 7 张 + 多印 1 张
descZh['Autarch']                 = '\u2460：给予你的所有部队 +1 近战、+1 远程和 +1 生命'
descZh['Warlock']                 = '\u2460：给予你的所有部队 +1 远程攻击和 +1 生命'
descZh['Wraithblade']             = '\u2460：获得护甲 2'
descZh['Wraithguard']             = '\u2460：获得 +2 远程攻击和 +2 生命'
descZh['Spiritseer']              = '\u2461：部署 1 个幽卫'
descZh['Farseer Skyrunner']       = '\u2461：选择敌方手牌中的 1 张牌并将其洗入其牌库'
descZh['Wraithknight']            = '\u2462：获得 [无敌] 无敌直到你的下个回合'
descZh['Nightshade Interceptors'] = '给予你的载具 +1 远程攻击和 +2 生命'
descZh['Exorcist']                = '8 \u2600：对所有敌人造成 5 点伤害'

# ④ 中文 descZh：关键词裸词补数字（卡面确实带数字）
descZh['Screamer-Killer'] = '护甲 1。爆裂 2。'
descZh['War Walker']      = '护甲 1。爆裂 2。'

# ⑤ 中文 descZh：跟着 ① 一起改（AM50 的中文原本也跟着错的英文写成「装甲」）
descZh['Thunderous Charge'] = '给予一个友方部队 +2 [攻击]、+2 [远程] 和震荡。'

# ⑥ 中文 descZh：补句首 Codex 前缀
descZh['Primaris Chaplain'] = '典籍：给予你的步兵部队 +1 近战攻击和 +1 远程攻击。'

# ---------------- 记事 ----------------
d['_manual_desc_note']['_2026-09-16_验收尾巴_第二批'] = (
    '2026-09-16 「卡面字与数据」尾巴批。**证据一律是逐张开 PnP 成品卡图核过**（铁律 7）。四类：'
    '(1) ±N 后面的属性被 OCR 读成了 armor 家族、卡面实为紫圈枪=远程（6 张）：'
    'Ancient Reliquary / Legendary Tenacity / Moment of Grace（原文档已点名）**加** '
    'Imagifier / Celestian Sacresant Aveline / Thunderous Charge（**新查出，原文档没记**）。'
    '写法用 Ranged（同 Simulacrum Imperialis 那批的约定），**不是**去放宽 GivePayload.ReAttr 的 Armor 词表。'
    '⚠️ Ancient Reliquary 与 Legendary Tenacity 的底层 zh_cards.json 仍写「护甲/装甲」，靠本表 descZh 覆盖兜着。'
    '(2) Prayer 整句与卡面不符：卡面是 Give〔盾〕Shield to a friendly unit. If the unit has Pray gain 1 ☀，'
    '原来写成 Give +1 Attack —— **真改玩法**（Attack → Shield）。'
    '(3) Primaris Chaplain 卡面句首有 Codex:（带蓝徽图标），原来缺；补回不重复结算'
    '（开头关键词同时在 keywords 与 desc 里是全池 206 张的常态）。'
    '(4) Exorcist 卡面是 8 ☀ : Deal 5 damage to all enemies，**我们的 desc 连前缀都没有**'
    '（原文档只记了中文漏印）⇒ 按池内既有写法 8 [faith]: 补回。'
    '⚠️ **这条改的是玩法**（从「不要钱」变成「付 8 信仰」），复验看四条自检与覆盖率报表。')

d['_manual_descZh_note']['_2026-09-16_验收尾巴_第二批'] = (
    '2026-09-16 同上批。**只改显示层中文**，逐张开 PnP 成品卡图核过（铁律 7）。四类：'
    '(1) 灵魂石/信仰费用前缀：Autarch / Warlock / Wraithblade / Wraithguard（各 ①）· Spiritseer（②）· '
    'Farseer Skyrunner（②）· Wraithknight（③）· Exorcist（8 ☀）—— 八张的英文 desc 里**本来就有**前缀'
    '（2026-09-14 灵魂石轮补的），中文这回跟上。⚠️ **数字逐张开图核过**（原文档只点了其中两张）。'
    'Nightshade Interceptors 反过来 —— 卡面**根本没有**前缀，我们多印的 ② 是把**右上角蓝色部署费用圈**'
    '当成了灵魂石，**删掉**。'
    '(2) 关键词裸词补数字：Screamer-Killer / War Walker 卡面是 Armour 1.  Blast 2，我们印成「护甲。爆裂」'
    '（根因：KeywordSegment 的全文子串判据命中了 descZh 里的裸词 ⇒ 整段不补，见 A2/A9）。'
    '⚠️ **Boomdakka Snazzwagon 的「裸词」不成立、没改** —— 开图是 Unstable. Flank. Tide，原版本来就没数字。'
    '(3) Thunderous Charge 的中文原来跟着错的英文一起写成「[装甲]」，一并改成「[远程]」。'
    '(4) Primaris Chaplain 中文补句首「典籍：」。'
    '⚠️ Wraithknight 那条里 [无敌] 之后的「无敌」两字**保留原样**（原版也是「图标 + 词」的形状），本次只加前缀。')

io.open(P, 'wb').write(
    json.dumps(d, ensure_ascii=False, indent=1).replace('\n', '\r\n').encode('utf-8'))
print('OK written', P)
