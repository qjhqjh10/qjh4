# -*- coding: utf-8 -*-
"""卡面图标的「记号 → sprite」计划表生成器（2026-09-15）。

**它解决什么问题**

`cards_engine.json` 的 `desc` / `descZh` 里有一批方括号占位符（`[Attack]` / `[Armor]` /
`[Spirit Stone]` …）和符号（`☀` `①` `💀` …），卡面上它们画的是**图标**。
原来以为「一张全局表 `[Attack] → Melee` 就够了」——**不对**。实测反例（都逐张看过成品卡图）：

    DA44 `+3 [Attack], +3 [Armor]`       卡面 = +3【拳】, +3【枪】   ⇒ [Attack]=拳, [Armor]=枪
    EC8  `-2 [Health] and -2 [Attack]`   卡面 = -2【拳】, -2【枪】   ⇒ [Health]=拳, [Attack]=枪
    GOF16 `+1 [attack] and +1 [weapon]`  卡面 = +1【拳】, +1【枪】   ⇒ [attack]=拳, [weapon]=枪

**同一个 `[Attack]` 在两张卡上指向不同图标**（前者拳、后者枪）。原因是这些 token 名**不是原版数据**，
是**卡图 OCR 猜的**（猜不出就按位置/词形乱起名：`[Health]`、`[Might]`、`[Power]`、`[weapon]`…
实测**画的全是拳或枪**）。⇒ **名字不可信，逐张卡图才可信。**

所以本脚本的答案是 **「按卡」的表**（`TOKEN_BY_CARD`，每条带证据），
并且用 `_还原效果文字.md`（29 个子代理逐张看卡图抄的还原表，1965/1968 已还原）
**做一遍交叉核对**：锚点对不上的、或者锚点说是 A 而表里写着 B 的，**全部报出来**，不静默。

**产物**

* `数据/游戏数据/card_icon_plan.json` —— 每张卡每个记号的答案（带证据）+ 通用表
* `MyGame/Assets/CardPresentation/Resources/card_icon_plan.json` —— **同一份数据写第二遍**
  （运行时那份，`Resources.Load<TextAsset>("card_icon_plan")` 要它在 Resources 下）
* `资料/卡面图标_对照与缺口.md` —— 人看的表 + 缺口清单，**由 `工具/gen_icon_doc.py` 生成**
  （⚠️ 2026-10-10 更正：本节原来把这一项写成本脚本的产物 —— 错的，本脚本只写上面两份 json；
  `OUT_MD` 那个变量也**从没被用过**，见下面它那行的注释）

用法：
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_plan.py            # 只报告
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_plan.py --write    # 落盘（**但有任何未解决问题就不落盘**）
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_doc.py             # 再铺一遍人看的文档

🔴 **`A1626`（2026-10-12）**：退出码非 0 时**一个字节都不写** —— 「非 0」必须等于「没产物」。
   触发条件 = `missing`（sprite 名盘上没有）· `unresolved`（没认出来的记号）· `conflicts`（同字段
   token 互相包含 ⇒ 运行时会画两枚）· `why_conflict`（两条来源 sprite 打架）· `PREFIX_ABSORB` 死条目
   / 接手 token 不在计划里 · `FACE_ICON_COUNT` 卡面枚数对不上 · `A1703` 逐词三分与冻表不一致 /
   位置锁被破 / 【灭自证】控制臂没变红。**改前**这一段排在 `if WRITE:` 之后
   ⇒ 退出码 1 时**照样落盘**（实测：HEAD 版跑一个 rc=1 的输入，两份 json 都写出来了）。
"""
import json
import os
import re
import sys
import collections

sys.stdout.reconfigure(encoding="utf-8")
WRITE = "--write" in sys.argv
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity

RESTORED = os.path.join(ROOT, "资料/卡表核对_卡图提取/_还原效果文字.md")
CARDS = os.path.join(ROOT, "MyGame/Assets/RuleEngine/Resources/cards_engine.json")
OUT_JSON = os.path.join(ROOT, "数据/游戏数据/card_icon_plan.json")
# 运行时那一份（`Resources.Load<TextAsset>("card_icon_plan")` 要它在 Resources 下）——
# **每次都由本脚本一起写**，别手改；两处内容永远一样（同一份数据写两遍）
OUT_RES = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/card_icon_plan.json")
# ⚠️ 死变量（2026-10-10 复核：全脚本只出现这一行）—— `.md` 是 `工具/gen_icon_doc.py` 写的，
#    本脚本**从不**写它。留着只是历史残留，别拿它当「本脚本会铺文档」的依据。
OUT_MD = os.path.join(ROOT, "资料/卡面图标_对照与缺口.md")
TRAITS = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/Art/traits")
UI = os.path.join(ROOT, "MyGame/Assets/CardPresentation/Resources/Art/ui")

SUN, ONE, TWO, THREE, FOUR, FIVE = "\u2600", "\u2460", "\u2461", "\u2462", "\u2463", "\u2464"
SKULL, DAGGER, PISTOL = "\U0001F480", "\U0001F5E1", "\U0001F52B"
SWORDS, SHIELD, BOLT, SNOW = "\u2694", "\U0001F6E1", "\u26A1", "\u2744"

# ── 符号表（desc 里那些**不是方括号**的记号）─────────────────────────────────
#      ⚠️ 逐条照卡图核过，别按字符想当然（`⚡` 是 rally.png 的闪电，不是「能量」）
SYMBOLS = {
    SUN:   ("faith", "☀ 金太阳 = **信仰**（修女会）。逐张核过 7 张：Sister Novitiate / Blade of Faith / "
                     "Sacred Rose / Paragon Warsuit / Preacher / Miraculous Feat / Daemonbreaker 同一张图；"
                     "数字写在图标**外面左侧**"),
    ONE:   ("SpiritStone_1", "① = 灵魂石 1 档 —— **数字烘在图里**（图集 `Atlas_SpiritStone_1..5`，"
                             "逐张看过：绿圈里就是数字；真卡 Spiritseer/Wraithknight 核过）"),
    TWO:   ("SpiritStone_2", "② = 灵魂石 2 档（同上）"),
    THREE: ("SpiritStone_3", "③ = 灵魂石 3 档（同上）"),
    FOUR:  ("SpiritStone_4", "④ = 灵魂石 4 档（同上）"),
    FIVE:  ("SpiritStone_5", "⑤ = 灵魂石 5 档（同上）"),
    SKULL: ("backlash", "💀 = **反噬**触发前缀（卡面 `💀 Backlash:` 里那个骷髅 = 橄榄黄圆底 + 白骷髅）。"
                        "⚠️ 别和 `[skull]` 那个 token 混 —— 那一个在 `Slay:` 前面、是**橙色**骷髅（slay.png）"),
    DAGGER: ("Melee", "🗡 = 近战（Lord Kakophonist 卡面：`-1 🗡 and -1 🔫` = -1 近战 -1 远程）"),
    PISTOL: ("Ranged", "🔫 = 远程"),
    SWORDS: ("Melee", "⚔ = 近战（Canoness 中文卡面：`+2 ⚔ 和 +2 🔫`）"),
    SHIELD: ("shield", "🛡 = 护盾（卡面就是「🛡护盾」= 图标 + 词）"),
    BOLT:  ("rally", "⚡ = **集结**触发前缀（rally.png 就是暗金圆底 + 米白闪电）"),
    SNOW:  ("markOfSlaanesh", "❄ = 暗黑契约·纵欲（Noise Marine「gain a ❄ Dark Pact of Excess」）。"
                              "⚠️ **渲染上认不出是哪位邪神，只能按名字选** —— ⚪ 2026-10-11（`A1476`）"
                              "就地订正：原来这里写「五张 markOf* 切片**逐字节相同**」，**不准确**："
                              "逐字节相同的是 `markOfChaos`/`markOfKhorne`/`markOfNurgle` **三张**；"
                              "`markOfSlaanesh` 只差 **15 个 `alpha=0` 像素**的 RGB（`markOfTzeentch` 差 6）"
                              "⇒ **画面完全一样**，结论（按名字选）不变。出处 → `资料/普查产出_第二十会话/"
                              "C1_卡表数据与文本订正.md` §`A1476` 那段的「顺手小订正」"),
}

# ── 方括号 token：**按卡**给答案 ───────────────────────────────────────────────
#      每条都有证据（成卡图路径 / 还原表原文）。同一条证据覆盖多张卡时逐条列出，别用通配。
# 🔵 **2026-10-14（第三十一会话 · P1 id 置换批）**：本表与 `BARE_TOKEN_BY_CARD` 的**卡 id 键已整族换算**
#   （171 张卡的 id 从「自造名样式」换成真值 —— 60 个键 / 107 处），**答案与注释一字未动**。
TOKEN_BY_CARD = {
    # ---- 名字骗人最狠的四对（卡面看过的）----
    # ⚪ 2026-10-10（`A1411`）：`("EC33","[Health]")` **已删** —— 死键（该卡文本里已无 `[Health]`）；
    #    它当年那个答案（Melee / 第一个圆徽 = 粉拳）**已由下面那条 `("EC33","[Attack]")` 承接**（同答案）⇒ 删掉不丢覆盖。
    # 🔴 2026-10-18：这条原来是 `("EC33","[Attack]") → Ranged`（第二个圆徽 = 紫枪）。
    #    那张卡池文本今天写的是 `Rally: Give -2 Attack and -2 Ranged to an enemy troop.` ——
    #    **第一个位置 = 拳、第二个 = 枪**，所以 `Attack` 那一处是**拳**；
    #    本批把两个裸词都改成了记号（`-2 [Attack] and -2 [Ranged]`）⇒ 这条的答案跟着改。
    #    （旧答案不是错，是给**改名之前**那个 token 写的 —— 那个 token 已经不在文本里了。）
    # 🔴 2026-10-10（`A1411`）：键由 `[Health]` **订正成 `[Ranged]`** —— 死键，卡表那一处 2026-10-18
    #    已写成 `[Ranged]`（英文第二个属性）。答案不变（Ranged）、证据沿用原来那份。
    ("461354d98aa5e3142afa4eb4922e5456", "[Ranged]"): ("Ranged", "卡图 Hardwired Destruction：第二个圆徽 = 紫枪"),
    ("461354d98aa5e3142afa4eb4922e5456", "[Attack]"): ("Melee", "还原表：`Give +1Melee and +1Ranged`"),
    ("GOF88", "[attack]"): ("Melee", "还原表 Nob on Smasha Squig：`+1 Melee and +1 Ranged`"),
    # ⚪ 2026-10-10（`A1411`）：`("GOF88","[health]")` **已删** —— 死键；
    #    同位置的 `("GOF88","[ranged]")` / `("GOF88","[远程]")` 两条一直活着（同答案 Ranged）。
    ("GOF11", "[attack]"): ("Melee", "卡图 Banner Nob：第一个圆徽 = 粉拳"),
    ("GOF11", "[weapon]"): ("Ranged", "卡图 Banner Nob：第二个圆徽 = 紫枪（另 4 张同款也核过，见 CLAUDE.md 铁律 7）"),
    # ⚪ 2026-10-10（`A1411`）：`("UM40","[health icon]")` **已删** —— 死键（那条 `[health icon]`
    #    早退场，本卡文本现在是 `[Attack]`/`[Ranged]`/`[Codex icon]`）；同位置第一条由本卡的 `[Attack]` 条目承接。
    # 🔴 2026-10-10（`A1411`）：这一条也是死键，**键订正成 `[Ranged]`**（卡表那一处已写成 `[Ranged]`）——
    #    它当年那个答案（Ranged）**和现在的卡表一致**（写着 attack、画的是枪），所以是换键、不是丢弃。
    ("UM40", "[Ranged]"): ("Ranged", "卡图 + NCC 0.688（写着 attack、画的是枪），键 2026-10-10 由 `[attack icon]` 订正"),
    # ---- 其余逐条 ----
    ("84d4d192e0f57a34a90ac0f6172c3358", "[fist]"): ("Melee", "卡图：粉拳圆徽"),
    ("84d4d192e0f57a34a90ac0f6172c3358", "[skull]"): ("slay", "卡图：橙红骷髅 + 橙准星 = slay.png"),
    ("UM40", "[Codex icon]"): ("codex", "卡图 + NCC 0.815"),
    ("UM37", "[Codex]"): ("codex", "卡图 + NCC 0.777"),
    ("UM90", "[eye icon]"): ("stealth", "卡图 + NCC 0.955"),
    # ⚪ 2026-10-10（`A1411`）：`("UM90","[Talent]")` **已删** —— 死键，而且**那个位置整处没了**：
    #    本卡 `desc` 今天是 `Long Range. Oath 1: Gain [eye icon] Stealth. Slay: …`（无 `Talent` 词、`descZh` 也无「天赋」）
    #    ⇒ 没有可换的键，只能删（删前实测：本卡的 `plan` 条目一字未变）。
    ("EC30", "[Melee]"): ("Melee", "卡图 + NCC 0.599"),
    ("EC30", "[Ranged]"): ("Ranged", "卡图 + NCC 0.578"),
    # ⚪ 2026-10-12（`A1605`）：**下面 6 条信仰前缀的逐卡键已删** —— 那 6 张卡的文本已统一写成 `☀`
    #     （判据 = 成品卡图逐张亲读，见 `数据/游戏数据/cardface_fixes.json` 的
    #      `_2026-10-12_A1605_信仰前缀统一`）⇒ 那 6 个键**再也不会命中**（一场生成会报 6 条死条目）。
    #     ⛔ **删它们不丢图标**：`☀ → faith` 是 `SYMBOLS` 那条**全局**规则给的，而且它那条证据逐张点名的
    #     正是这几张（Sister Novitiate / Blade of Faith / Sacred Rose / Paragon Warsuit / Preacher）。
    #     删前实测：本批 13 个（卡, 字段）的 `faith` 枚数**一枚都没变**（71 枚 / 37 张，改前改后相同）。
    #     旧键（保留痕迹，别照抄回去）：
    #        ("SOR38", "[Faith Icon]") · ("SOR4", "[Icon]") · ("SOR53", "[Icon]") ·
    #        ("SOR73", "[icon]") · ("SOR9", "[faith]") · ("65cfec7d2bc719040b3b4b88622931b2", "[Faith]")
    #     另：`TOKEN_BY_RULE` 的 `("[Energy]", None, "faith", …)` 那条**今天是死规则**（池里再无 `[Energy]`）——
    #     它不在死条目自检的覆盖面里（本文件只查两张**逐卡**表的死键），**如实留在这里**、不删，
    #     下次谁再写 `[Energy]` 时它仍然是对的（`资料/卡面图标_现状与缺口.md:257` 判过「数据 token 错」）。
    ("TAU39", "[Markerlight]"): ("markerlight", "卡面：图标 + 词 `Markerlight 2`"),
    ("TAU56", "[Vanguard]"): ("vanguard", "卡面：图标 + 词 `Vanguard`"),
    ("TAU41", "[Shield]"): ("shield", "卡面：图标 + 词 `Shield`"),
    ("TAU73", "[Power]"): ("Melee", "卡图：粉拳圆徽（**写着 Power、画的是拳**）"),
    ("GSC37", "[Strength]"): ("Melee", "卡图 Patriarch：粉拳圆徽"),
    ("UM104", "[strength]"): ("Melee", "卡图 Bladeguard Ancient：粉拳圆徽"),
    ("EC12", "[Might]"): ("Melee", "卡图 Flawless Blade Champion：粉拳圆徽"),
    ("SOR54", "[Might]"): ("Melee", "卡图 Fiery Conviction：粉拳圆徽"),
    ("GSC42", "[fist]"): ("Melee", "卡面就是粉拳"),
    ("SW56", "[fist]"): ("Melee", "同族，还原表 `+1 Melee`"),
    ("ASH35", "[Invulnerable]"): ("invulnerable", "卡图 Wraithknight + NCC 0.831"),
    ("SOR47", "[Invulnerable]"): ("invulnerable", "卡面：图标 + 词 `Invulnerable`"),
    ("GOF101", "[Stomp]"): ("stomp", "卡图 + NCC 0.866 / 0.848（同一张卡两处）"),
    ("GOF73", "[Mob]"): ("mob", "卡图 + NCC 0.857"),
    ("GOF73", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("8425b809e9e64e24f9035f709f5aaee4", "[Shield]"): ("shield", "卡面：图标 + 词 `Armour 1`"),
    ("8425b809e9e64e24f9035f709f5aaee4", "[Wings]"): ("flying", "卡面：图标 + 词 `Flying`"),
    ("BL38", "[Dark Pact]"): ("markOfChaos", "卡图 + NCC 0.932（暗红盘 + 白八芒星）"),
    # ⚪ 2026-10-10（`A1411`）：`("BL_Helfire_Torch","[Chaos]")` **已删** —— **卡 id 写错了**：
    #    池里没有 `BL_Helfire_Torch`，真卡是下一行那个 `BL69 Hellfire Torch`（`Hellfire` 不是 `Helfire`），
    #    而 `("BL57","[Chaos]")` **本来就在**（同 sprite）⇒ 这条是错 id 上的重复条目。
    ("BL57", "[Chaos]"): ("markOfChaos", "同上（同一张精灵图）"),
    ("SW61", "[Shield]"): ("shield", "卡面：图标 + 词"),
    ("6899b199f5e36144ebbdd7ed9d9e7afe", "[Slay]"): ("slay", "卡面：图标 + 词 `Slay:`"),
    # ⚪ 2026-10-10（`A1411`）：`("6899b199f5e36144ebbdd7ed9d9e7afe","[Shield]")` **已删** —— 死键，而且那个位置**判给 `Slay` 了**：
    #    卡表 2026-10-18（`8b84a3d`）把 `"+2 [Attack] and [Shield] Shield: Heals 3"` 改成
    #    `"+2 [Attack] and [Slay] Slay: Heals 3"`（补回被方括号换掉的词时一并订正）⇒ 上一行那条才是这一处的答案。
    # 🔴 2026-10-18：`("UM100", "[Oath]")` / `("UM100", "[Talent]")` 两条**方括号键**已作废 ——
    #    `UM84` 的 `desc` 改回了**裸写**（`Friendly Oath abilities …` / `Talent: …`，
    #    `cardface_fixes.json` 的 `desc` 列，改动说明见那张表的 `_2026-10-18_Oath裸写`），
    #    两个方括号 token 在文本里**已经不存在** ⇒ 留在本表就是**永远不命中的死条目**。
    #    同两张卡的补法见下面的 `BARE_TOKEN_BY_CARD`。
    # 🔴 **2026-10-12（第二十一会话 · 写手 `N2` · `A1634`）：`("DA19","[1]")` / `("DA75","[1]")` 两条已删。**
    #    为什么删：那两张的 `desc` 把**任务点徽记**写成了**裸方括号数字 `[1]`**，而 `[1]` 不是任何解析器认得的 token
    #    （旁证：`工具/ruleprobe.sh` 实测 `gain [1]` ⇒ `verb=gainenergy amount=0 payload=[1 energy]`，
    #    **什么都不给**、`unparsed=` 还是空的）。`desc` 已按卡面改成规范写法 `1 Quest Point`
    #    （`数据/游戏数据/cardface_fixes.json`），那枚徽记改由**按资源词的自动那一支**画
    #    （新 token `"1 Quest Point"` → `questPoints1`，`data/游戏数据/card_icon_plan.json` 逐条可见）。
    #    ⛔ 留着这两条就是**永远不会命中的死条目**（生成器的死条目自检会报）—— 同族先例见
    #    `ANCHOR_NOISE` 里 `("DA5","[Shield]")` 当初换成 `[Quest Point]` 那条注释。
    #    ⚠️ 但这两条本身是**独立的一份旁证**：`[1]` 早就被认出是**暗黑天使任务点**
    #    （"卡面 `gain (①)`"，见 `资料/卡表核对_卡图提取/_裁定_图标丢失.md`）⇒ 与 `A1634` 的结论一致。
    # ---- 攻击/远程：还原表逐张写了 Melee / Ranged ----
    ("EC18", "[Attack]"): ("Melee", "还原表：`Gain +1 Melee, +1 Ranged`"),
    ("EC18", "[Ranged]"): ("Ranged", "同上"),
    # ⚪ 2026-10-10（`A1411`）：`("SOR13","[Armor]")` **已删** —— 死键；这一处的答案（Melee）由本卡的
    #    `("SOR13","[攻击]")` + `("SOR13","[Attack]")` 两条承接（同答案）⇒ 删掉不丢覆盖。
    # 🔴 2026-10-10（`A1411`）：下面 5 条的键**由 `[Attack]` 订正成 `[Ranged]`** —— 死键（卡表那一处
    #    2026-10-18 已写成 `[Ranged]`）。它们本来就写着「Ranged」（`[Attack]` 是个骗人的名字，见文件头），
    #    ⇒ 换键后答案一字不变、只是把「这一处 = 紫枪」那份逐卡证据接回到活键上。
    ("EC7", "[Ranged]"): ("Ranged", "还原表：`Give +2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM10", "[Ranged]"): ("Ranged", "还原表：`+2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM37", "[Ranged]"): ("Ranged", "还原表：`+1 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM74", "[Ranged]"): ("Ranged", "还原表：`+2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("DA73", "[Ranged]"): ("Ranged", "还原表：`+1 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("DA45", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("9e0eecf8cb9a8df49bf18cc5d9051109", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("38871853fabb5244d8148766829230cd", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("EC35", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("EC15", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GSC28", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GSC30", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("GSC58", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GOF77", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("GOF72", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("508002f5d7ee54f4686aefa2e6adce41", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("d19e8b68aaa9a884781d06697ad237a6", "[Attack]"): ("Melee", "还原表：`+3 Melee`"),
    ("SAU44", "[Attack]"): ("Melee", "还原表：`+1 Melee … +3 Melee`（两处都是拳）"),
    ("SAU66", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("e01f4d54e8a474d96b5a1167cc28eebf", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SOR68", "[attack]"): ("Melee", "还原表：`+3 Melee`"),
    ("SOR65", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SW37", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("SW41", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SW45", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    # ⚪ 2026-10-10（`A1411` 顺手）：这里原来还有一条 `("SW55","[Attack]"): ("Melee", "还原表：`+4 Melee`")`
    #    —— 它和本表后面那条同名键（why = `"还原表 Legendary Tenacity：…"`）**是同一个键**，
    #    而 **Python 的字典字面量遇到重复键是「后者静默赢」** ⇒ 这一条从来没生效过
    #    （生成器计划表里 `SW55` 的 `desc [Attack]` 打的 why 一直是后面那条）。
    #    **两条答案相同（Melee）**，所以删掉它**零行为变化**（删前实测：本卡 `plan` 条目逐字未变），
    #    只是把"静默重复"这种坑清掉。
    ("UM109", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("55b8e3d2040f86d4190ffb1a91abb5ed", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("26d4ded12b28a3742b5c8fc155e29803", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    # ⚪ 2026-10-10（`A1411`）：`("TL75","[Attack]")` **已删** —— 死键，而且**两个答案打架**：
    #    这条写 Melee，而卡表现已写 `[Ranged]`（`+1 Ranged Attack` → `+1 [Ranged]`，`8b84a3d` 改的）
    #    且 `[Ranged]`→Ranged 有按规则条目 ⇒ 这条旧答案**已被卡表推翻**，删（不是换键）。
    ("TAU70", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    # ---- 中文 token ----
    # 🔴 2026-10-10（`A1411`）：键 `[\u62a4\u7532]` **订正成 `[\u653b\u51fb]`**（死键）—— 这一句的**英文那一半**写着
    #    `Attack`（卡面画的是拳），卡表 2026-10-18 把中文也统一成 `+N [\u653b\u51fb]` 了
    #    （`cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。答案不变（Melee）、证据沿用。
    ("GOF106", "[\u653b\u51fb]"): ("Melee", "卡面原文 `Gain +1 Attack` —— **中文数据自己把「攻击」写成了护甲**"
        "（键 2026-10-10 由 `[\u62a4\u7532]` 订正）"),
    # ⚪ 2026-10-10（`A1411`）：`("SOR10","[\u62a4\u7532]")` **已删** —— **卡 id 写错了**：
    #    池里没有 `SOR_Sisters_Repentia`，真卡是 `SOR19 Sisters Repentia`（`Warpforge_19`），
    #    而它那一处今天写 `[Attack]`、本表已有 `("SOR10","[Attack]")`（同答案 Melee）⇒ 错 id 上的重复条目。
    ("GOF14", "[\u653b\u51fb]"): ("Melee", "还原表 Skarboy Nob：`Mob: Gain +1 Melee`"),
    ("851f73daf747eb642934a16429b391db", "[\u653b\u51fb]"): ("Melee", "还原表 Storm of Silence：`Your Warlord gains +2 Melee this turn`"
        "（中文 token 当年写「生命」、卡面画的是拳；键 2026-10-10 由 `[\u751f\u547d]` 订正）"),
    # ---- 2026-09-15 补：把「锚点自动对齐」那几处升级成有证据的（还原表原文见每条）----
    ("AM45", "[attack]"): ("Melee", "还原表：`Give +2 Melee, +2 Ranged and Concussive`"),
    # ⚠️ 2026-09-16：`[armor]` 已按卡面改名成 `[ranged]`（`cardface_fixes.json` 的 desc 列；
    #    卡面第二枚是**紫枪**）—— 键要跟着换，否则这条旧键成了**永远不会命中的死条目**、
    #    新 token 反而掉进「锚点没对上」没有图标。中文字段写的是 `[远程]`。
    # ⚪ 2026-10-10（`A1411` 就地更正）：下面那条 `("AM45","[Ranged]")`（当年是给 `descZh` 的 `[远程]`
    #    当**桥**用的，本行原写着「所以两条都要在」）**已删** —— **那个前提 2026-10-18 起不成立了**：
    #    那一批给 `TOKEN_BY_RULE` 加了 `[远程]` 规则 ⇒ `[远程]` 在 **② 按规则**那一支就答完，
    #    **③ `ZH2EN` 那一支根本轮不到**（实测：现读 `plan` 里 `AM50` 的 `descZh` 带的是规则那条 `why`）
    #    ⇒ 它已经是个**够不着的死键**，删掉不丢覆盖（删前实测：本卡 `plan` 条目一字未变）。
    ("AM45", "[ranged]"): ("Ranged", "同上"),
    ("DA39", "[Attack]"): ("Melee", "卡图 `Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：第一枚 = 粉拳"),
    # 🔴 2026-10-18（A980-c）：键从 `[Armor]` 换成 `[Ranged]` —— 那张卡池文本里**从来没有** `[Armor]`
    #    （注释里也一直写着「卡面第二枚 = 紫枪」，只是键名沿用了更早那版 OCR 写法）⇒ 旧键是**死条目**。
    #    本批把 `desc` 里那个**裸词 `Ranged`** 改成了记号（卡面只印枪、不印词），键跟着换。
    ("DA39", "[Ranged]"): ("Ranged", "同上：第二枚 = 紫枪（键 2026-10-18 由 `[Armor]` 订正过来）"),
    # ⚪ 2026-10-12（`A1623` · 铁律 5 就地订正）：`("BL8", "[黑暗契约]")` **已删**（原条目：
    #    `→ markOfChaos`，理由写着「2026-10-18 把中文改成 `[黑暗契约]纵欲黑暗契约`」）。
    #    **那个前提早就被同一批改成不成立了**：`A1473`（2026-10-10）把 `BL19` 的 `descZh` 从
    #    `…获得一个 [黑暗契约]纵欲黑暗契约` 改成了 `…获得一个 ❄纵欲黑暗契约`
    #    （出处 → `MyGame/Assets/CardPresentation/Editor/IconSetup.cs` 的 `KnownZhGaps` 那一大段注释，
    #    以及 `资料/普查产出_第十四会话/W_A1420A1444.md` §一）⇒ **`[黑暗契约]` 从此不在任何文本里**。
    #    实测（2026-10-12 本批）：生成器的「逐卡表里的死条目」一节**只报出它这一条**
    #    （`BL19 [黑暗契约]`），而 `BL19` 的英文与中文**两侧都靠 `❄` 走 `SYMBOLS`**（`markOfSlaanesh`）
    #    ⇒ 删掉它**一枚图标都不丢**（删前删后逐字段对比：`BL19` 的计划条目一字未变）。
    #    ⚠️ 这也是账 `A1420` 的收口：它记的「两个来源 sprite 不一致」里，`markOfChaos` 那个来源
    #    **就是这条死条目**（死条目不参与渲染，所以「不一致」本来就落不到画面上；
    #    而五张 `markOf*` 切片**渲染上完全一样**，见 `SYMBOLS` 里 `❄` 那条的说明）。
    #    ⛔ **别把它加回来** —— 加回来就是死条目，生成器的自检会再报一次。
    ("dd535f7a54bcf6241b24ca6d0e81bbb4", "[Attack]"): ("Melee", "还原表 Supreme Grand Master：`Give +1Melee and +1Ranged`"),
    # 🔴 2026-10-10（`A1411`）：键 `[Armor]` **订正成 `[Ranged]`**（死键）—— 同一处的键名早就是错的
    #    （`[Armor]` 是 OCR 起的名字，答案从来是「紫枪」），卡表 2026-10-18 已写成 `[Ranged]`。答案不变。
    ("dd535f7a54bcf6241b24ca6d0e81bbb4", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("EC3", "[Attack]"): ("Melee", "还原表 Disharmonist：`-2Melee and -2Ranged`"),
    ("EC3", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("5a8a11b2a89796f48b34fbec8db6f452", "[Attack]"): ("Melee", "还原表 Antrak Silk：`+1Melee and +1Ranged`"),
    ("5a8a11b2a89796f48b34fbec8db6f452", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("SW55", "[Attack]"): ("Melee", "还原表 Legendary Tenacity：`+4Melee, +4Ranged`"),
    ("SW55", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armour]` 订正）"),
    # ⚪ 2026-10-10（`A1411`）：`("EC1","[armor]")` **已删** —— 死键；这一处（第一枚 = 拳）今天写 `[Attack]`，
    #    本表已有 `("EC1","[Attack]")`（同答案 Melee）⇒ 删掉不丢覆盖。
    ("EC1", "[Ranged]"): ("Ranged", "还原表 Sonic Blaster Noise Marine：`-1Melee and -1Ranged`"
                                     "（第二枚 = 紫枪；键 2026-10-10 由 `[attack]` 订正）"),
    ("EC16", "[attack]"): ("Melee", "还原表 Screamer Kakophonist：`-4Melee and -4Ranged`"),
    ("EC16", "[Ranged]"): ("Ranged", "同上（本批把 token 名换成了它真正的含义 —— 写着 health、画的是枪）"),
    # ⚠️ 2026-09-16：这一张的 desc 已按卡面**把 token 名换成了它真正的含义、顺序也摆正**
    #    （`Give +1 [attack] and +1 [ranged]`，卡面第一枚=粉拳、第二枚=紫枪）。
    #    所以键要从 `[armor]`/`[attack]` 换成 `[ranged]`；`[attack]`→Melee 那条见上面（现在是对的）。
    ("SOR65", "[ranged]"): ("Ranged",
        "还原表：`Pray: Give +1Melee and +1Ranged` —— token 改名后它就是第二枚（紫枪）"),
    ("DA70", "[Ranged]"): ("Ranged", "还原表：`gain +1Ranged`"),
    ("GSC37", "[Ranged]"): ("Ranged", "还原表 Patriarch：`Give +3 Melee and +3 Ranged`"),
    # 🔴 **2026-10-20：`("DA34", "[honour]")` ⛔ 别删、别把它「顺手统一」成 `[Quest Point]`。**
    #    它是**一个小写的词、不是图标名**（所以看着像 OCR 垃圾，换会话最容易被清掉）：
    #    卡面那一枚是**暗黑天使的任务点徽记**（锯齿环 + 中央纹章），OCR 读成了 `[honour]`。
    #    删掉这条 / 改掉 token ⇒ 那枚徽记**整枚不画**（本表是按**字面 token** 查的，
    #    运行时 `CardIcons.Rewrite` 拿的就是 `cardface_fixes.json`/`card_stats.json` 里的原串）。
    #    ⚠️ **别拿 `DA8` 那条注释当依据**：它写的「2026-09-15 已把卡表这段文字改成 `[Quest Point]`」
    #       说的是 **`DA8`/`DA15`**（那两张的 key 是 `Apothecary` / `Company Veteran`，
    #       在 `cardface_fixes.json` 的 `desc`/`descZh` 里）；**`DA38` 从来不是 `[Quest Point]`**
    #       —— 它的 `[honour]` 至今留在 `card_stats.json` 的原始行里。
    #    判据出处 = 还原表 `Unforgiven Redemptor`：`When you gain Quest Point, deal 2 damage`
    #    （中文侧 `descZh` 也写「任务点」，可反证）。数据侧留痕见
    #    `cardface_fixes.json` 的 `_2026-10-20_DA38honour_别删`。
    ("DA34", "[honour]"): ("questPoints", "还原表 Unforgiven Redemptor：`When you gain Quest Point, deal 2 damage`"
                          "　⛔ **别删：`[honour]` 是个词、不是图标名，靠本逐卡条目才对上**"
                          "（删条目 / 改 token ⇒ 徽记整枚不画；`DA38` 的 token 本来就**不是** `[Quest Point]`）"),
    ("8425b809e9e64e24f9035f709f5aaee4", "[Attack]"): ("Melee", "还原表：`+1Melee, Armour Armour 1 and Flying Flying`"),
    ("6899b199f5e36144ebbdd7ed9d9e7afe", "[Attack]"): ("Melee", "还原表：`+2Melee and Slay Slay:`"),
    ("GOF101", "[Attack]"): ("Melee", "还原表 Da Old Ways：`give it +2 Melee this turn instead`"),
    ("AM56", "[Ranged]"): ("Ranged", "还原表：`Give +2 Ranged to your units`"),
    # 🔴 2026-10-10（`A1411`）：键 `[Armor]` **订正成 `[Ranged]`**（死键）—— 同一处卡表 2026-10-18
    #    已写成 `[Ranged]`（第二枚 = 紫枪）。答案不变（Ranged）、证据沿用原来那份。
    ("SW45", "[Ranged]"): ("Ranged", "还原表 Unbridled Fury：`Give +2Melee and -2Ranged`（键 2026-10-10 由 `[Armor]` 订正）"),
    # ---- 2026-09-16 「`±N` 属性与卡面图标不符」批：token 改名后的键 + 几张自动对齐的转正 ----
    #      ⚠️ 这一批改了 `cardface_fixes.json` 里 31 张卡的 `desc`/`descZh`（判据 = 逐张开 PnP 卡图）。
    #         **改名的 token 要在这里换键**，否则旧键成死条目、新 token 掉进「锚点没对上」没有图标。
    #         🔴 `GOF87` 那条是实测抓到的：不改键的话**锚点会把 `[ranged]` 自动配成盾**（画错图标）。
    ("GOF88", "[ranged]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Orks/3部队/Warpforge_13_Nob-on-Smasha-Squig.png`）："
        "`Other friendly Beasts have +1〔拳〕 and +1〔枪〕` —— 原文 token 写的是 `health`，实为紫枪"),
    ("GOF88", "[远程]"): ("Ranged", "同上（同一张卡的中文写法）"),
    ("AM10", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/3部队/Warpforge_22_Cadian-Standard-Bearer.png`）："
        "`Duty: Give +2〔紫圈枪〕 to your units this turn`，全卡没有粉拳"),
    ("AM37", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/3部队/Warpforge_42_Rogal-Dorn-Tank.png`）："
        "`Regiment: Give +1〔紫圈枪〕`"),
    ("AM74", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/4计策/Warpforge_10_Forged-Killers.png`）：`Give +2〔紫圈枪〕`"),
    ("461354d98aa5e3142afa4eb4922e5456", "[远程]"): ("Ranged",
        "2026-09-16 补上那条**一直挂着的缺口**（本文件 2026-09-15 收口时它还在「锚点没对上」）："
        "同卡的英文 `[Ranged]` 已经由锚点对齐到 Ranged，中文这一枚是同一个位置。"
        "判据：`Necron/2天赋/Warpforge_02_Hardwired-Destruction.png` —— `Give +1〔拳〕 and +1〔准星/枪〕`"),
    ("SOR13", "[攻击]"): ("Melee",
        "2026-09-16 逐张开图核过（`Sorotitas/3部队/Warpforge_18_Simulacrum-Bearer.png`）："
        "`Pray: Give +1〔粉圈拳〕 to your troops` —— 原文英文 token 写 `[Armor]`，实为拳"),
    # ---- 2026-09-15 收口：原来挂着的那「四处缺口」，用户裁决 + 逐张卡图核过，全部接上 ----
    #      ⚠️ 这四条都是**按卡**的，别升级成 token 规则 —— token 名不可信（见文件头），
    #         而且 `[Destroyer]` 这一枚**图集里根本没有 `destroyer.png`**：原版给它用的就是
    #         `frenzied` 那张图，所以按名查永远查不到。
    ("461354d98aa5e3142afa4eb4922e5456", "[Destroyer]"): ("frenzied",
        "2026-09-15 用户裁决。**图集里没有 `destroyer.png` 不是缺口 —— 原版给「毁灭者」用的就是 "
        "`frenzied` 那张图**：`Necron/3部队/Warpforge_24_Skorpekh-Destroyer.png` 卡面直接印着 "
        "`〔此图标〕Destroyer.`（关键词紧挨着它），`Necron/1督军/Warpforge_01_Ramatekh-the-Cruel.png` 同。"
        "并排比对 `icons/frenzied.png`：金圆盘 + 白色张开的爪掌 + 背后两把交叉刀 + 掌下一块小牌，逐处一致"
        "（同批候选 `huntMark` / `rally` 完全不同）。"),
    ("461354d98aa5e3142afa4eb4922e5456", "[毁灭者]"): ("frenzied",
        "同 `[Destroyer]`（同一张卡、同一个位置的中文写法）"),
    ("DA5", "[Quest Point]"): ("questPoints",
        "2026-09-15 用户裁决 + 逐图核过。这枚是**暗黑天使的任务点徽记**（深绿圆盘 + 银灰尖刺环 + "
        "环底铭牌带 + 中央展翅矛纹章）= `icons/questPoints.png`。四条旁证：① `DA43 Repulsor` 同位置画的是"
        "**同一枚带「3」的**（`questPoints3`），中文「获得 3 点任务」；② `DA48 Grim-Resolve` 是带「①」的"
        "（`questPoints1`）；③ `DA16 Librarian` **行首**的 `Shield.` 才是真护盾，是**另一枚**"
        "（青绿圆底 + 盾内一只眼 = `shield`），同卡末尾 `gain ①` 仍是这枚任务点徽记；"
        "④ 规则书中文版 `:199`「任务（Quest）：每获得 3 点任务：向牌库加入 1 张隐秘并洗牌」。"
        "⚠️ 原来的 token `[Shield]` 是 **OCR 认错的** —— 同一枚徽记在 `DA38` 上被写成 `[honour]`"
        "（中文却是「任务点」）。2026-09-15 已把卡表这段文字改成 `[Quest Point]`"
        "（`数据/游戏数据/cardface_fixes.json` 的 `desc`/`descZh` 两列），**引擎触发点也跟着改挂任务点**。"),
    ("DA12", "[Quest Point]"): ("questPoints",
        "同 `DA8`（卡图 `Dark Angels/3部队/Warpforge_15_Company-Veteran.png`：行首 `〔红盾+红骷髅〕Vanguard.` "
        "那枚才是 `vanguard`，`gain` 后面这枚与 `DA8` **逐像素同款**，都是任务点徽记）"),

    # ── 🆕 2026-10-18：`[Attack]` 的**逐卡**近战条目（本批新写的记号）──────────────────
    #  ⚠️ **这一批必须逐卡列，不能升成 `TOKEN_BY_RULE`** —— `[Attack]` 这个**名字**在本工程里
    #    拳/枪都有：老数据 `EC23` / `AM22` / `AM42` / `AM75` / `DA73` 是**枪**、`DA44` / `DA50` 是**拳**
    #    （文件头那条「token 名不可信」讲的就是它）。升成规则 = 哪天有人写了个「实为枪」的
    #    `[Attack]`，会被**静默**画成拳 ⇒ 宁可多 90 行数据。
    #  ⚠️ 本批**新写的**近战记号**一律**用 `[Attack]`（不是 `[Melee]`）是有原因的，别顺手统一：
    #    英文 `desc` 是**引擎输入**，`GivePayload.ReAttr` 认的是字面 `attack` ⇒ 写 `[Attack]`
    #    才能让去括号后的串与改前**逐字节相同**（`EffectText.ParseSegment` 只剥方括号、不剥内容）。
    #    唯一例外 `EC39` 用 `[Melee]` —— 那句走 `TryDouble` 的**字面** `melee and ranged` 匹配。
    ("c06453b330af61745beb5fa567ba863e", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH32", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("851f73daf747eb642934a16429b391db", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH69", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH86", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA26", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA30", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA32", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA37", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA48", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA50", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA52", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA68", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA65", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA72", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC56", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC8", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC24", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC1", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC22", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC23", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC26", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC13", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC57", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC47", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC32", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("aa45079b714e3ff498549e533c5954a1", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC33", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF29", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF106", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF14", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF17", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF2", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF39", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF45", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF79", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF4", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF82", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF85", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF98", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF100", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("64a657fc5ed5bbb4fa7b585cf29c1e95", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("20d0f4451f16fc441836a02f99bf77da", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("6ba4b132d24528844a86304f8ce67833", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("3ebb159036778d14ebd1c48451aa6ca0", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF70", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("e3d67d86f73f7564eb0bf886ec848af6", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC9", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("7831725387c8a774dbdf5b6492d928e0", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC18", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC16", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC14", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC24", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC4", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC25", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR7", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR13", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR10", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR23", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR25", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR24", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR56", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR44", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("f104d4c21abe84546abc9365d9eda8dd", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR60", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR67", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR5", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW8", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW9", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW11", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW20", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW27", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW47", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("4d4b9170663891f488c0e96ee8e55ba1", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU71", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU73", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU74", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("b3735ef3d1408d544b0947e31f898e20", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("5b5425f6e2848b449807bd1643d0d936", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TL68", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TL73", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM91", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM96", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("8482077f309b08943b190f18b5874871", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM111", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("4de4fcfb5d7e0554a88b9f419a2cd7c2", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM86", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM40", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM42", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    # 🔴 2026-10-18 **第二批**：这两张的**英文 `desc` 已改**（`[Attack]` → `[Ranged]`）——
    #     卡面 `Ultramarines/3部队/inceptor srg.png` / `inceptor.png` 那一枚都是**紫圈枪**（本批再亲读一次确认），
    #     而 `[Attack]` 经 `GivePayload.ReAttr` 解出来是 **`attack`（近战）** ⇒ 引擎原来按「近战 +1」结算。
    #     属 `A2`（「裸 `+N` 实为远程」）那一族的漏网两张，本批**做掉**（不是只记录）。
    #     ⚠️ token 名跟着换键，否则旧键成**死条目**。
    ("UM22", "[Ranged]"): ("Ranged", "卡图 `Ultramarines/3部队/inceptor srg.png`：那一枚是**紫圈枪**（锚点核对 + 亲读两处一致）"),
    # 🆕 2026-10-18 第二批：`UM118` 的英文 `desc` 原来**截断在 `equal to its`**（少了那一枚记号），
    #    本批补上（判据 = 卡面 `Ultramarines/4计策/Warpforge_37_Indomitus-Crusade.png`：
    #    `Oath 6: Deal damage to all enemy troops equal to its〔**粉拳**〕` —— 本批作者亲读）。
    ("UM118", "[Attack]"): ("Melee", "卡图 `Ultramarines/4计策/Warpforge_37_Indomitus-Crusade.png`：`equal to its〔粉拳〕`（亲读）"),
    ("UM67", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("7693f2b21b2f85644b739da990bc2216", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("e874c2bd84895e64694daa44bd5fd5c2", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    # 🔴 2026-10-18 **第二批**：同上一张，英文 `desc` 已改（`[Attack]` → `[Ranged]`）。
    ("UM9", "[Ranged]"): ("Ranged", "卡图 `Ultramarines/3部队/inceptor.png`：那一枚是**紫圈枪**（锚点核对 + 亲读两处一致）"),
}

# ── 按卡的**裸 token**（卡面上真印着的字，不是方括号占位）──────────────────────────
#      2026-10-18 加。这一类**不能**塞进 `TOKEN_BY_CARD`：
#      ① 那张表的键是**方括号 token**，只在 `for m in toks`（`TOK = \[...\]` 的匹配）里被查
#         —— 裸 token 写进去**永远不会命中**（一条看着在干活、实际什么都不做的条目 = 静默失败）；
#      ② 它也不该进 `KEYWORD_PREFIX`（那支要求「句首 + 紧跟 `:`/`.`」），
#         `Oath abilities of friendly …` 后面既不是句首也不是冒号，扫不到 —— 这正是它原来漏画的机制原因。
#      ⚠️ **token 要写成能唯一指到那枚徽记、而且足够长的字面片段**：
#         运行时 `CardIcons.Rewrite` 按 token **长度降序**换，短 token 撞上同卡更长 token
#         已经插好的结果时会被它的**幂等守卫**（`s.IndexOf(tag + token) >= 0`）**整条跳过**。
#         逐条推演过：`"Oath"` 在 `UM106` / `UM93` 上会被同卡的 `"Oath 1:"` 那一条挡掉
#         ⇒ 首句那枚**画不出来**（`UM84` 没有 `Oath N:`，用 `"Oath"` 反而能用；为一致，三张都写 `Oath abilities`）。
BARE_TOKEN_BY_CARD = {
    # 原版在**每一个 `Oath` 词**前面都印徽记（含句中）：`Friendly 〔徽记〕Oath abilities …`。
    # 判据 = 成品卡图（`V-OATH` §2.4 把 25 张读完；下面这三张是本批作者**自己开的图**）。
    ("UM100", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png`（本批作者亲看整卡）："
        "`Friendly 〔蓝圆盘+白袍人形〕Oath abilities apply an additional time. "
        "〔纸卷〕Talent: Catechism of Death` —— 徽记在**句中**、紧贴词 `Oath`。"
        "2026-10-18 本卡 `desc` 由 `[Oath]`（方括号 ⇒ 整串换掉 ⇒ 词没了）改成**裸写**，"
        "所以这一枚要在这儿按卡补上（改动说明见 `cardface_fixes.json` 的 `_2026-10-18_Oath裸写`）。"),
    ("UM106", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_25_Ferren-Areios.png`（本批作者亲看整卡）：首句 "
        "`〔蓝圆盘+白袍人形〕Oath abilities of friendly troops can be activated up to 3 times each turn.`，"
        "末句 `〔同一枚〕Oath 1: Deal 1 damage` —— **同卡两枚徽记**。"
        "⚠️ 首句这一枚原来**整枚没画**（`Oath abilities` 没有数字、没有冒号 ⇒ `KEYWORD_SCAN` 扫不到）。"),
    ("UM93", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_12_Vico-Therbeus.png`（本批作者亲看整卡）："
        "`〔眼睛〕Stealth. 〔蓝圆盘+白袍人形〕Oath abilities of friendly troops may be activated on later turns. "
        "〔同一枚〕Oath 1: Gain 〔迷彩〕Camouflage` —— **同卡两枚徽记**，首句这枚原来整枚没画（同 `UM106`）。"),
    # 中文侧：**玩家实际看到的是中文**（`BattleDriver.FaceTextFull`：`DescZh` 有值就用它）
    # ⇒ 只补英文那半 = 卡面照旧少一枚。中文写的是「誓言能力」
    # （`誓言 N：` 那半边的 token 由 `KEYWORD_SCAN_ZH` 自己扫得到，本来就画着）。
    ("UM100", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力额外结算 1 次。`）"),
    ("UM106", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力每回合最多可激活 3 次。`）"),
    ("UM93", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力可在后续回合激活。`）"),
}

# 🔴 2026-10-11（`A1347`）—— **这四张不要在这儿补条目**：`UM80` / `UM81` / `UM94` / `UM74`。
#    `A1331` 把它们的 `desc` 段首补回 `Oath ` 之后（=`Oath 4:` / `Oath 3:` / `Oath 1:` / `Oath 3:`），
#    这几个位置落进**句首关键词**那一支：`KEYWORD_SCAN` 扫得到「词 + 数字 + 冒号」、
#    `KEYWORD_PREFIX` 按名字查得到 `Art/traits/oath.png` ⇒ **重新 `--write` 一次就自动出条目**
#    （实测逐字段对账：新增恰好这 4 个 `desc` 字段、旧字段 0 处改动）。
#    ⇒ ⛔ **别再给它们写 `TOKEN_BY_CARD` / `BARE_TOKEN_BY_CARD`** ——
#      `TOKEN_BY_CARD` 的键是**方括号 token**（裸 token 写进去永远不命中）；
#      `BARE_TOKEN_BY_CARD` 那一支有 `_btok in got ⇒ continue` 的守卫 ⇒ 写了也是**整条跳过的冗余条目**。
#    徽记位置也**与原版一致**（紧贴在 `Oath N:` 前、词留着）：判据 = 成品卡图亲读
#    `Ultramarines/3部队/Warpforge_16_Devastator-Marine.png` 的 `Blast 2. 〔徽记〕Oath 4: Deal 5 …`
#    （另三张同目录 `_17_Phobos-Librarian` / `_11_Scout-Sniper` / `_10_Firestrike-Servo-Turret` 同形）。

# ── 🆕 2026-10-10（`A1366`）：`Stun` 那枚**金螺旋**（`traits/stun.png`）—— 改前【全池一条都没画】──
# 🔴 **查证报告**（判据链、行号、最小改法）：`资料/普查产出_第十三会话/R7_A1366到A1369查证.md`。
#
# ① 🔴 **本文件 `KEYWORD_PREFIX` 那段注释里原写着「`Stun an enemy` … **都不带图标**」—— 那句话是错的**
#    （铁律 5：就地更正；那段注释已改）。实测 `Stun` 这个词 **不分位置、不分词性、恒带图标**：
#    句首（`SOR59 Stun three random enemies.`）· 句中动词（`AM47 … and Stun it`）·
#    句中**名词**（`ASH74 When an enemy receives a Stun,`）· 句尾（`GOF77 Stun troops attacked`）**全带**。
# ② **逐张判据 = 成品卡图**（铁律 7），30 张**一张不缺**、每一张都有出处：
#    · **29 张** → `资料/卡表核对_卡图提取/_还原效果文字.md`（29 个子代理逐张看卡图抄出来的还原表）——
#      那张表对这个词**一律双写**（`Stun Stun` / `StunStun` = 图标 + 词），行号逐条列在下面。
#    · 第 **30** 张 `DA_None_Must_Know`：它来自 `6秘密/` 的**手机翻拍**、不在那张表里 ⇒
#      本批作者**亲读** `d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3696.jpg`
#      = `Destroy an enemy troop.〔金棕圆底 + 白色同心螺旋〕Stun adjacent units`
#      （同一枚徽记的旁证：`DarkAngels__3.md:18` 记它「金/棕圈内的白色螺旋纹（旋涡）」，与 `stun.png` 同图）。
#      🔴 **2026-10-11（`A1377` 按族收口）：那不是孤例，是【一族 9 张】**：
#         `D:/2/Warpforge部队卡片/Dark Angels/6秘密/` 下 **IMG_3695..3699.jpg**（全 `.jpg` 手机翻拍，
#         非 900×1200 PnP PNG）= `Convoke the Circle`(3695) · **`None Must Know`(3696 =
#         我们的 `DA_None_Must_Know`)** · `Obscure Ritual`(3697) · `Rites of Penance`(3698) ·
#         `Smothering Decree`(3699)；另有 `Genestealer Cult/6破坏卡/` 的 **IMG_3817..3820.jpg**
#         = `Jammed Communications` / `Poisoned Supplies` / `Improvised Barricade` / `Cult Propaganda`
#         （同一族，`2026-09-16` 补的两份批次文件 `DarkAngels__3.md` / `GenestealerCult__3.md`）。
#         抄录与「非 PnP」的标注在那两份批次文件的表头（`DarkAngels__3.md:1-3`）里。
#         ⇒ **按名搜不到 PnP 成品图时，先想是不是这一族**（`A1366`/`A1400` 各撞过一次）。
#      🔴 **2026-10-12 就地订正（`A1610①` · 铁律 5）：上面这条原来还写着「`_还原效果文字.md` 里**没有**」
#         —— 那句**当时是对的、现在不成立**，已删。真因不是「转录表不覆盖 `.jpg`」，而是
#         **转录表比它的源旧**：`_合并总表.md` 在 `2026-09-16`（`fbbd569`）从 **1118 张 / 29 份批次**
#         长到 **1127 张 / 31 份批次**（就是补进来的这 9 张），而 `restore_card_icons.py --write`
#         **那一轮没跟着重跑** ⇒ 转录表停在 `2026-09-13` 的快照上（文件 mtime 可证）。
#         ✅ **已重跑生成器** ⇒ 这 9 行现在**在表里**（`IMG_3695.jpg` 等 9 行，逐行带 `⚠️`），
#         本文件这 115 处 `_还原效果文字.md:NN` 引用也一并按新表**重算过**（+5 / +9 两档）。
#         🔴 **2026-10-12（第二十一会话 · 写手 `C5` · 铁律 5）就地订正**：这句原来写「**117** 处」——
#            实测（`_还原效果文字.md(.{0,8})` 逐处分类）**带 `:NN` 行号的一共 115 处**（111 个不同的行号），
#            另有 **49 处**只是散文里提到文件名、不带行号（合计 164 处提及）。⇒ 117 这个数是旧的/多算的。
#         ⚠️ **表头的行数不许变**：本脚本那 115 处行号指的全是**表行**，而表行在**表头之后** ——
#            给 `restore_card_icons.py` 的 `--write` 生成物加一行 `>` 说明，就会把这 115 处**全体指歪一行**。
#            核法（两分钟）：扫本文件里每个 `_还原效果文字.md:NN`，取它**后面最近的一个**卡图文件名，
#            再看还原表**第 NN 行**的首列是不是那个文件 —— 115/115 全对上才算没漂。
# ③ **误伤面（实测 = 0）**：全池 `\bStun\b` 命中 **30 张**（`desc`）· `眩晕` 命中 **30 张**（`descZh`），
#    **两个集合完全相同**；每题**恰好 1 处**（max = 1）；全池**没有**更长的词含它
#    （`Stunned` / `stuns` 等 **0 处**、`keywords` 里 **0 处**、`[Stun]` 记号 **0 处**）
#    ⇒ 不会像 `A1347` 那样顶掉别的东西（`Rewrite` 是 `s.Replace(token, bare)`，一条覆盖本字段全部出现）。
# ④ ⛔ **别改成 `KEYWORD_SCAN` / 别放进 `KEYWORD_PREFIX`** —— 那一支要求「句首 + 紧跟 `:`/`.`」，
#    而 `Stun` 后面**永远跟的是词**（`Stun an enemy`），放进去**不命中**、只会成为一条死条目。
# ⑤ ⚠️ **为什么逐卡列、不写成一条「按词」规则**：本表每一条都要能追到**那一张卡**的出处，
#    而 30 张**每张都有**（见 ②）。代价 = **新卡若含 `Stun`，要在这儿补一行**；
#    「盘上有图、计划表一条都没用上」那一**整类**缝由 `A1370` 的对账去兜（不在本表射程里）。
# ⑥ 中文侧**同一条**：玩家实际看到的是中文（`BattleDriver.FaceTextFull`：`DescZh` 有值就用它）
#    ⇒ **只补英文那半 = 中文卡面照旧少一枚**。中文 token = `眩晕`（`descZh` 的写法）。
#    ⚠️ 中文这半的判据就是「英文那枚在图前、位置一一对应」。
#    🔴 **2026-10-14 更正（铁律 5）**：上面那句原来跟在「**原版没有中文表**（那份中文是我们自己译的）」
#       后面 —— **该前提【不成立】**。实际：原版**有**官方中文表（9,440/9,447 条非空），由**远端
#       Addressables 包 `localization_assets_all`**（2025-07-30 快照）下发，**2026-10-14 已解开**
#       并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`。**错因**：把「不在离线包体里
#       （246,807 个文件扫中文串零命中 · 84 个 bundle 无本地化包）」读成了「根本没有」。
#       ✅ **仍然成立的两条**：① **客户端安装目录里**确实没有中文表；② 我们工程里那份中文
#          （`数据/本地化/i18n/zh_CN.csv` / `数据/卡牌翻译/zh_cards.json`）**是我们自己译的**。
#       📌 **判据全文（含「仍然成立的两条」的准确措辞）→ `CLAUDE.md` §一·7 的「2026-10-14 更正」段**
#          （⛔ 别在这儿抄第二份、也别只信本行这一份）。
#    ⇒ **下面那句结论【本身不变】，但【理由要换】**：本表的判据只到「英文卡面那一半」，
#       是因为**我们的 PnP 成品卡图印的是英文** —— 转录表 `资料/卡表核对_卡图提取/_还原效果文字.md`
#       只覆盖英文卡面（下面每条的「判据出处」都指它），**不是**「原版没有中文表」。
#       ⚠️ 官方中文表到手后，**中文那一侧的「文本层」多了一条可核的判据**（官方 `Card_Description/*`，
#          但它是 I2 占位符模板、要等 `Core/CardIcons` 的 `[[…]]`/`((…))` 记号层，见账 `A1763`）——
#          本表**没跟着动**（本笔只订正措辞、一个 token 都没改）。
#          ⚠️ **本文件里凡还写着「原版没有中文表」的旧注（共 7 处），一律以本条为准。**
_STUN_CARDS = [
    # (卡 id, 判据出处, 卡图文件名) —— 判据出处 = 逐张抄录里 `Stun` **双写**的那一行
    ("AM42", "`资料/卡表核对_卡图提取/_还原效果文字.md:161`", "Warpforge_47_Pinning-Fire.png"),
    ("AM68", "`资料/卡表核对_卡图提取/_还原效果文字.md:110`", "Warpforge_04_Tempestus-Scion.png"),
    ("AM66", "`资料/卡表核对_卡图提取/_还原效果文字.md:104`",
     "Warpforge_01_Hektor-Thenmann.png"),
    ("ASH15", "`资料/卡表核对_卡图提取/_还原效果文字.md:31`", "Warpforge_20_Howling-Banshee.png"),
    ("ASH39", "`资料/卡表核对_卡图提取/_还原效果文字.md:71`", "Warpforge_48_Deadly-Ambush.png"),
    ("ASH45", "`资料/卡表核对_卡图提取/_还原效果文字.md:74`", "Warpforge_51_Banshee-Mask.png"),
    ("ASH74", "`资料/卡表核对_卡图提取/_还原效果文字.md:11`", "Warpforge_01_Jain-Zar.png"),
    ("851f73daf747eb642934a16429b391db", "`资料/卡表核对_卡图提取/_还原效果文字.md:12`", "Warpforge_02_Storm-of-Silence.png"),
    ("ASH75", "`资料/卡表核对_卡图提取/_还原效果文字.md:19`", "Warpforge_09_Yrlla-the-Huntress.png"),
    ("BL73", "`资料/卡表核对_卡图提取/_还原效果文字.md:226`", "Warpforge_11_Malicious-Volleys.png"),
    ("DA35", "`资料/卡表核对_卡图提取/_还原效果文字.md:316`", "Warpforge_39_Dark-Talon.png"),
    ("24fcaf45556573549b922c06cad3aa2c", "`资料/卡表核对_卡图提取/DarkAngels__3.md:9` **＋** 同文件 `:18`（形状存疑条）",
     "Dark Angels/6秘密/IMG_3696.jpg"),
    ("EC1", "`资料/卡表核对_卡图提取/_还原效果文字.md:364`",
     "Warpforge_17_Sonic-Blaster-Noise-Marine.png"),
    ("GOF26", "`资料/卡表核对_卡图提取/_还原效果文字.md:669`", "Warpforge_39_Stompa.png"),
    ("GOF30", "`资料/卡表核对_卡图提取/_还原效果文字.md:676`", "Warpforge_40_Attack-Squig.png"),
    ("GOF52", "`资料/卡表核对_卡图提取/_还原效果文字.md:698`", "Warpforge_62_Toxic-Bonfire.png"),
    ("GOF78", "`资料/卡表核对_卡图提取/_还原效果文字.md:594`", "Warpforge_03_Snakebite-Grot.png"),
    ("GOF97", "`资料/卡表核对_卡图提取/_还原效果文字.md:649`",
     "Warpforge_22_Gargantuan-Squiggoth.png"),
    ("GOF65", "`资料/卡表核对_卡图提取/_还原效果文字.md:590`",
     "Warpforge_02_Stikkbomb-Boy.png"),
    ("GSC17", "`资料/卡表核对_卡图提取/_还原效果文字.md:442`", "Warpforge_17_Locus.png"),
    ("GSC21", "`资料/卡表核对_卡图提取/_还原效果文字.md:453`", "Warpforge_28_Clamavus.png"),
    ("GSC28", "`资料/卡表核对_卡图提取/_还原效果文字.md:460`",
     "Warpforge_35_Hulking-Aberrant.png"),
    ("GSC75", "`资料/卡表核对_卡图提取/_还原效果文字.md:465`",
     "Warpforge_13_Roaming-Outriders.png"),
    ("SAU17", "`资料/卡表核对_卡图提取/_还原效果文字.md:525`", "Warpforge_23_Psychomancer.png"),
    ("SOR48", "`资料/卡表核对_卡图提取/_还原效果文字.md:769`", "Warpforge_59_Imperial-Creed.png"),
    ("TAU45", "`资料/卡表核对_卡图提取/_还原效果文字.md:911`", "Warpforge_57_Recon-Sweep.png"),
    ("TL17", "`资料/卡表核对_卡图提取/_还原效果文字.md:944`", "Warpforge_26_Deathleaper.png"),
    ("TL71", "`资料/卡表核对_卡图提取/_还原效果文字.md:970`",
     "Warpforge_11_Toxic-Entanglement.png"),
    ("UM21", "`资料/卡表核对_卡图提取/_还原效果文字.md:1067`",
     "Warpforge_32_Primaris-Judiciar.png"),
    ("UM89", "`资料/卡表核对_卡图提取/_还原效果文字.md:1025`", "Warpforge_07_Incursor.png"),
]
# 英文 + 中文**两半一次生成**（只写英文那半 = 中文卡面照旧少一枚；见上面 ⑥）。
# token 就是那个词本身（`Stun` / `眩晕`）；运行时走 `CardIcons.Rewrite` 的**裸关键词那一支**
# （插在**词前**、词留着、外面包 `<link>` ＋ `<nobr>`）—— 与 `Oath abilities` 那几条同一条路。
for _stun_cid, _stun_src, _stun_png in _STUN_CARDS:
    _stun_why = (f"卡面 `Stun` **前面**那枚金螺旋（= `Resources/Art/traits/stun.png`；"
                 f"**句首 / 句中 / 名词位置恒带**，30/30）—— 逐张判据：{_stun_src}（`{_stun_png}`）。")
    BARE_TOKEN_BY_CARD[(_stun_cid, "Stun")] = ("stun", _stun_why)
    BARE_TOKEN_BY_CARD[(_stun_cid, "眩晕")] = (
        "stun", _stun_why + "中文侧同一条（`descZh` 里写 `眩晕`，位置与英文那枚一一对应）。")

# ── 🆕 2026-10-10（`A1376`）：`A1366` 那道判据牵着的【同类词族】—— 8 个词（本批做完 7 个）──
# 🔴 **查证报告 / 全部读数 / 顺手发现**：`资料/普查产出_第十四会话/W_图标族8词.md`。
#
# ① **根因与 `A1366` 是同一个**：`main()` 里那道
#    `if not toks and not syms and not kws and not res: continue` 会把**整份字段**跳过，
#    而裸 token 表在它**之后**才查。那批已把「本字段有没有裸 token 命中」（`btok`）并进那道判据
#    ⇒ **对下面这 7 个词同样有效**。本批**只补表**，判据一字未动（`--write` 前后报告只多预期那几行）。
#    这 7 个词**全都不在** `KEYWORD_SCAN` / `KEYWORD_PREFIX` 的射程里（那一支要求「句首 + 紧跟 `:`/`.`」）：
#    `Stun`/`Blind`/`Fast` 后面**跟的永远是词**；`Sniper` 常写在**句尾且后面没有 `:`/`.`**（`Waystone. Stealth. Sniper`）；
#    `Vulnerable`/`Hunt Mark`/`Sabotage`/`Blood Thirst` 也都在**句中或句尾**。
#
# ② **逐张判据 = 成品卡图**（铁律 7）。主判据 = `资料/卡表核对_卡图提取/_还原效果文字.md`
#    （29 个子代理逐张看卡图抄的还原表）—— 卡面上「**图标 + 紧跟那个词**」那个写法，它写成**双写**
#    （`VulnerableVulnerable` / `Sniper Sniper` / `FastFast` = 图标 + 词）⇒ **双写 = 那一处真有图标**
#    （该表的「图标数」列就是照 `【图标:X】` 占位符数的，与双写处数一一对应）。
#    本批**每一个词都另亲读一张成品图、把徽记与 `Art/traits/*.png` 并排核过**（见 ⑥）。
#    ⚠️ 还原表**不是唯一入口**：`GSC64` 那一张不在它里面（它把卡名写成 `Telepathic Domination`、
#    我们数据里是 `Telephatic Domination` —— **我们那边少了一个 `a`**），本批亲读卡图定的（见 ⑥）。
#
# ③ 🔴 **「词在正文里、卡面却【没画】图标」的卡本批【有意不补】**
#    （补了 = 给卡面加上原版根本没有的东西 = 工程红线；逐张亲读成品图定的）：
#    · ~~`GOF90 Beastboss on Squigosaur`（`Blood Thirst`）~~ 🔴 **2026-10-13（`A1713`）更正：
#      这一条【被推翻】了 —— 卡面**印了**图标** ⇒ 见下面 `_GOF90_BT_WHY` 那一段（已按卡补上）。
#      **原记载（留痕）**：「`Blood Thirst` 前面**只有 `Slay` 那一枚**、**没有嗜血徽记**
#      （还原表 `:628` 同判、未双写）」—— **两次亲读直接打架，按卡图判 ⇒ 本轮（2026-10-12/13 那次
#      裁图 3× 放大）为准**：`Warpforge_16_Beastboss-on-Squigosaur.png` 卡面那一行是
#      `have "〔橙底骷髅+准星〕Slay: Gain 〔红底银环白獠牙圆徽〕Blood Thirst this turn"`
#      —— **`Blood Thirst` 左侧确实印着那枚獠牙圆徽**。**代价**：那一处改前我们**少画 1 枚**
#      （真缺陷，**`A1713` 已修** —— 本段这一条现在只剩**记载**的价值）。
#    · `SAU55 Solar Pulse`（`Blind`）—— 亲读 `Necron/4计策/Warpforge_55_Solar-Pulse.png`：卡面
#      `Blind all enemies, and they lose 〔眼〕Stealth and 〔迷彩〕Camouflage` —— `Blind` 前**一枚图标都没有**
#      （还原表 `:555` 同判、未双写）。
#    · `SAU3 Nemesor Zahndrekh`（`Relentless`）—— 亲读 `Necron/1督军/Warpforge_3_Nemesor-Zahndrekh.png`：
#      那个词是**天赋名** `Talent: Relentless March`，卡面那一枚是 `talent` 的卷轴
#      ⇒ **`relentless` 全池 0 个缺口**（`relentless.png` 盘上有图，但池里**没有任何一处用它**）。
#      ⇒ 本批**不给它建任何条目**，如实记 0。
#
# ④ 🔴 **`BL60 Daemonic Pact` 的英文 token 取 `Vulnerable 2`（不是 `Vulnerable`）—— 本批唯一的特例**
#    （⛔ **别顺手改回 `Vulnerable`**）：它同一段正文里**同时有** `Invulnerable` 与 `Vulnerable`，
#    而运行时 `CardIcons.Rewrite` 走的是**裸关键词那一支**的 `s = s.Replace(token, bare)`
#    ——**纯子串替换、没有词边界** ⇒ token 写 `Vulnerable` 会把 `Invulnerable` 里那半截**一起换掉**
#    （渲染成 `In〔脆弱徽记〕Vulnerable`）—— **静默、且只在那一张卡上现形**。
#    取「词 + 紧跟的数字」之后在该卡内**唯一**（`Invulnerable` 后面没有数字），
#    实测渲染层全池逐字比对**只动该动的那一处**（报告 §三）。
#    ⚠️ 代价：`<nobr>` 那一层会把数字也包进去（`<nobr>图标 + "Vulnerable 2"</nobr>`）——
#       卡面上这三者本来就在同一行，**形状仍与原版一致**；数字若被改，这条会变成**死条目**（生成器会报）。
#
# ⑤ **误伤面 = 0（两层都量了）**：
#    · **数据层**：7 个词各自的英文集合 = 它在 `desc` 里的**全池命中**、中文集合 = `descZh`（逐词张数见 ⑦）；
#      **没有一个词落在更长的英文词里**，唯一的例外 `Vulnerable ⊂ Invulnerable` 只在 `BL60` 一处、已按 ④ 处置
#      （`Hunt` ⊂ `Hunter(s)` 不适用 —— token 是 `Hunt Mark`；`Fast`/`Blind`/`Sniper`/`Sabotage` 全池 0 处更长的词）。
#    · **渲染层**：按 `CardPresentation/Core/CardIcons.cs` 的 `Rewrite` **逐分支静态复刻**，
#      把「改动前 / 改动后」两份计划表对全池 **1126 张 × 2 字段**各铺一遍、再逐字比 —— 读数见报告 §三。
#      ⚠️ 这是**静态复刻**（不模拟 `<link>` 那一层 `Badges.KeyOf`），**不是 Unity 实跑**。
#
# ⑥ **本批亲读的成品图（每个词至少一张，徽记与 `Art/traits/*.png` 并排核过）**：
#    `vulnerable`   → `Chaos/3部队/Warpforge_31_Bringer-of-Decay.png`（蓝圆 + 白盾 + 蓝闪电）
#    `huntMark`     → `Space Wolves/2天赋/Warpforge_10_War-Howl.png`（金圆 + 白色兽头骨 + 交叉骨）
#    `sabotage`     → `Genestealer Cult/5防御卡/Warpforge_67_Pilfered-Supplies.png`（白/红圆盘 + 红齿轮）
#    `sniper`       → `Aeldari/3部队/Warpforge_9_Ranger.png`（紫圆 + 白准星）
#    `bloodThirst`  → `Astra Militarum/3部队/Warpforge_21_Attilan-Rough-Rider.png`（红圆 + 白尖牙）
#    `blind`        → `Orks/4计策/Warpforge_43_Cloud-of-Smoke.png`（紫圆 + 白被划掉的手枪）
#    `fast`         → `Aeldari/3部队/Warpforge_05_Hornet.png`（金/橄榄圆 + 白旋风箭头）
#                     ＋ 不在还原表里的 `Genestealer Cult/4计策/Warpforge_64_Telepathic-Domination.png`（同枚）
#    （`relentless` 无缺口，见 ③ —— 它的图**盘上有、池里用不到**，本批不动。）
#
# ⑦ **7 个词的落点与读数**（`sprite` / 英文 token / 中文 token / 卡数）：
#    `vulnerable` → `vulnerable.png` · 英文 `Vulnerable` · 中文 `脆弱`（**`SAU54` 那一张写 `易伤`**）· **14 张卡 / 14 条中文条目**
#    `huntMark` → `huntMark.png` · 英文 `Hunt Mark` · 中文 `猎杀标记` · **22 张卡 / 22 条中文条目**
#    `sabotage` → `sabotage.png` · 英文 `Sabotage` · 中文 `破坏` · **14 张卡 / 14 条中文条目**
#    `sniper` → `sniper.png` · 英文 `Sniper` · 中文 `狙击` · **5 张卡 / 4 条中文条目**
#    `bloodThirst` → `bloodThirst.png` · 英文 `Blood Thirst` · 中文 `嗜血` · **7 张卡 / 7 条中文条目**
#    `blind` → `blind.png` · 英文 `Blind` · 中文 `失明` · **5 张卡 / 5 条中文条目**
#    `fast` → `fast.png` · 英文 `Fast` · 中文 `迅捷` · **5 张卡 / 5 条中文条目**
#    ⚠️ 中文 token **逐卡核过、两处例外**（都在 ⑧ 那条口径下按卡处置）：
#      · `SAU54 Curse of the Phaeron` 的 `descZh` 写的是 **`易伤`**（其余 13 张写 `脆弱`）⇒ 按卡取词。
#      · `ASH_Bright_Lance_Vyper` 的 `descZh` 里**根本没有「狙击」**（英文有、中文漏了，
#        是 `descZh` 的数据缺口、**另记**）⇒ 中文这半**不补**（补了就是**死条目**、生成器会报）。
#
# ⑧ 中文侧**同 `A1366`**：玩家实际看到的是中文（`BattleDriver.FaceTextFull`：`DescZh` 有值就用它）
#    ⇒ 只补英文那半 = 中文卡面照旧少一枚。
#    ⚠️ 中文这半的判据就是「英文那枚在图前、位置一一对应」。
#    🔴 **2026-10-14 更正（铁律 5）**：这里原来写「**原版没有中文表**（那份中文是我们自己译的）」
#       —— **不成立**（原版**有**官方中文表）。**判据全文与本文件的口径 → `_STUN_CARDS` 上方那条
#       `2026-10-14` 更正**（⛔ 别在这儿抄第二份）。结论不变，**理由换成「PnP 成品卡图印的是英文」**。
# (卡 id, 英文 token, 中文 token, sprite, 判据出处, 卡图文件名)
#   中文 token 为 `None` ⇒ **该卡中文卡面里没有那个词、不补**（补了就是死条目）。
#   判据出处 = 逐张抄录里那个词**双写**的那一行（`资料/卡表核对_卡图提取/_还原效果文字.md:NN`）。
_FAMILY8 = [
    # ---- `vulnerable`（14 张）----
    ("ASH85", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:93`", "Zrzut ekranu 2026-04-16 o 18.45.25.png"),
    ("BL18", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:203`", "Warpforge_31_Bringer-of-Decay.png"),
    ("BL66", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:211`", "Warpforge_3_Ghallarons-Disciple.png"),
    ("BL71", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:264`", "Warpforge_9_Havoc-Champion.png"),
    ("BL44", "Vulnerable 2", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:248`", "Warpforge_60_Daemonic-Pact.png"),
    ("BL75", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:227`", "Warpforge_12_Idolatrous-Despoilers.png"),
    ("9176318eaf469354e858f51af2333aa3", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:201`", "Warpforge_2_Litany-of-Despair.png"),
    ("BL74", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:228`", "Warpforge_13_Spreading-Corruption.png"),
    ("BL65", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:191`", "Warpforge_1_Ghallaron-the-Pious.png"),
    ("DA74", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:312`", "Warpforge_12_Hunters-of-Heretics.png"),
    ("6ed51d59b9f8ea24ab0dc7ceaef7956c", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:351`", "Warpforge_04_Duelists-Hubris.png"),
    # ⚠️ 中文 token 是 **`易伤`** —— 全池 14 张里**只有这一张**这么写（其余 13 张写 `脆弱`），
    #    本批逐卡核过 `descZh`；⛔ 别按「统一成 `脆弱`」去改（改了就与卡面对不上、还会变死条目）。
    ("SAU47", "Vulnerable", "易伤", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:563`", "Warpforge_54_Curse-of-the-Phaeron.png"),
    ("TL27", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:962`", "Warpforge_40_Exocrine.png"),
    ("TL71", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:970`", "Warpforge_11_Toxic-Entanglement.png"),
    # ---- `huntMark`（22 张）----
    ("SW6", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:789`", "Warpforge_14_Blood-Claw-Pack-Leader.png"),
    ("SW9", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:791`", "Warpforge_16_Fenrisian-Wolf.png"),
    ("SW10", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:792`", "Warpforge_17_Grey-Hunter.png"),
    ("SW12", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:795`", "Warpforge_20_Thunderwolf-Cavalry.png"),
    ("SW14", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:797`", "Warpforge_22_Wolf-Priest.png"),
    ("SW16", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:800`", "Warpforge_25_Long-Fang.png"),
    ("SW20", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:801`", "Warpforge_26_Thunderwolf-Cavalry-Pack-Leader.png"),
    ("SW26", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:804`", "Warpforge_29_Arjac-Rockfist.png"),
    ("SW22", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:805`", "Warpforge_30_Blackmane-Inceptor.png"),
    ("SW29", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:811`", "Warpforge_36_Sons-of-Morkai-Eliminator.png"),
    ("SW30", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:812`", "Warpforge_37_Terminator-Rune-Priest.png"),
    ("SW32", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:815`", "Warpforge_40_Stormwolf.png"),
    ("SW33", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:816`", "Warpforge_41_Vindicator.png"),
    ("SW35", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:818`", "Warpforge_43_Venerable-Dreadnought.png"),
    ("SW44", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:823`", "Warpforge_48_Death-Shun.png"),
    ("SW53", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:830`", "Warpforge_55_Embers-of-Prospero.png"),
    ("SW51", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:832`", "Warpforge_57_Hunters-Guile.png"),
    ("SW54", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:835`", "Warpforge_60_Fenrisian-Monstrosities.png"),
    ("SW50", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:836`", "Warpforge_61_Hordeslayer.png"),
    ("SW58", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:840`", "Warpforge_65_Fenrisian-Wolfpack.png"),
    ("SW59", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:842`", "Warpforge_67_Orbital-Defences.png"),
    ("e3b5735d801fcbd49aa8d3975a5b0c31", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:785`", "Warpforge_10_War-Howl.png"),
    # ---- `sabotage`（14 张）----
    ("GSC7", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:436`", "Warpforge_10_Neophyte-Hybrid.png"),
    ("GSC13", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:437`", "Warpforge_11_Neophyte-Specialist.png"),
    ("GSC16", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:446`", "Warpforge_21_Acolyte-Heavy.png"),
    ("GSC15", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:448`", "Warpforge_23_Acolyte-Specialist.png"),
    ("e9952f50921d2844a94d39ce883430f4", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:454`", "Warpforge_29_Vox-Hacker.png"),
    ("GSC27", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:459`", "Warpforge_34_Achilles-Ridgerunner.png"),
    ("GSC40", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:476`", "Warpforge_48_Backstab.png"),
    ("GSC50", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:477`", "Warpforge_49_Clandestine-Operation.png"),
    ("GSC45", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:488`", "Warpforge_60_Underground-Network.png"),
    ("GSC56", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:489`", "Warpforge_61_A-Trap-Sprung.png"),
    ("GSC65", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:495`", "Warpforge_67_Pilfered-Supplies.png"),
    ("GSC68", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:424`", "Warpforge_04_Atalan-Jackal.png"),
    ("GSC70", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:431`", "Warpforge_07_Jackal-Specialist.png"),
    ("c433e3dde4952cc448220908226127ae", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:420`", "Warpforge_02_Master-Outrider.png"),
    # ---- `sniper`（5 张）----
    ("ASH6", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:91`", "Warpforge_9_Ranger.png"),
    ("ASH10", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:24`", "Warpforge_14_Shroud-Runner.png"),
    ("UM8", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1053`", "Warpforge_17_Eliminator.png"),
    ("c433e3dde4952cc448220908226127ae", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:420`", "Warpforge_02_Master-Outrider.png"),
    ("ASH81", "Sniper", None, "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:97`", "Zrzut ekranu 2026-04-16 o 18.54.28.png"),
    # ---- `bloodThirst`（7 张）----
    ("AM19", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:132`", "Warpforge_21_Attilan-Rough-Rider.png"),
    ("EC19", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:365`", "Warpforge_18_Threnodic-Noise-Marine.png"),
    ("GOF72", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:627`", "Warpforge_09_Dead-Choppy.png"),
    ("GOF38", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:684`", "Warpforge_48_Proper-Killy.png"),
    ("SW34", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:814`", "Warpforge_39_Murderfang.png"),
    ("SW52", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:833`", "Warpforge_58_Proper-Hunt.png"),
    ("TL64", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:988`", "Warpforge_59_Ravenous-Hunter.png"),
    # ---- `blind`（5 张）----
    ("GOF31", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:679`", "Warpforge_43_Cloud-of-Smoke.png"),
    ("SW49", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:831`", "Warpforge_56_Fenrisian-Blizzard.png"),
    ("TL51", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:987`", "Warpforge_58_Toxic-Miasma.png"),
    ("UM2", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1031`", "Warpforge_10_Octavio-Infiltrator.png"),
    ("UM16", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1065`", "Warpforge_28_Sergeant-Allectius.png"),
    # ---- `fast`（5 张）----
    ("ASH67", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:15`", "Warpforge_05_Hornet.png"),
    ("DA32", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:313`", "Warpforge_36_Deathwing-Champion.png"),
    ("GSC49", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:492`", "Warpforge_64_Telepathic-Domination.png"),
    ("SAU3", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:579`", "Warpforge_9_Flayed-One.png"),
    ("GOF7", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:634`", "Warpforge_15_Warbiker.png"),
]


_FAMILY8_MARK = {
    "vulnerable":  "「盾牌 + 闪电」",
    "huntMark":    "「兽头骨 + 交叉骨」",
    "sabotage":    "「基因窃取者齿轮」",
    "sniper":      "「准星」",
    "bloodThirst": "「尖牙」",
    "blind":       "「被划掉的手枪」",
    "fast":        "「旋风箭头」",
}
# 英文 + 中文**两半一次生成**（只写英文那半 = 中文卡面照旧少一枚；见 ⑧）。
# token 就是那个词本身（`Vulnerable` / `脆弱` …）；运行时走 `CardIcons.Rewrite` 的**裸关键词那一支**
# （插在**词前**、词留着、外面包 `<link>` ＋ `<nobr>`）—— 与 `Stun` / `Oath abilities` 同一条路。
for _f8_cid, _f8_en, _f8_zh, _f8_sprite, _f8_src, _f8_png in _FAMILY8:
    _f8_why = (f"卡面 `{_f8_en}` **前面**那枚{_FAMILY8_MARK[_f8_sprite]}（= "
               f"`Resources/Art/traits/{_f8_sprite}.png`）—— 逐张判据：{_f8_src}（`{_f8_png}`）。")
    if _f8_cid == "BL44" and _f8_en == "Vulnerable 2":
        _f8_why += ("⚠️ 本卡 token 取 `Vulnerable 2`（不是 `Vulnerable`）—— 同一段正文里还有 `Invulnerable`，"
                    "而 `Rewrite` 是**纯子串替换**，写 `Vulnerable` 会把 `Invulnerable` 里那半截一起换掉。见本块 ④。")
    BARE_TOKEN_BY_CARD[(_f8_cid, _f8_en)] = (_f8_sprite, _f8_why)
    if _f8_zh is not None:
        BARE_TOKEN_BY_CARD[(_f8_cid, _f8_zh)] = (
            _f8_sprite, _f8_why + f"中文侧同一条（`descZh` 里写「{_f8_zh}」，位置与英文那枚一一对应）。")

# ── 🆕 2026-10-13（`A1713`）：`bloodThirst` 这一族的**第 8 张** —— `GOF90 Beastboss on Squigosaur` ──
# 🔴 **为什么它单独一条、不进 `_FAMILY8`、也不进 `BARE_TOKEN_BY_RULE`**：
#    · `_FAMILY8` 那 7 张是**恒带**的词；`Blood Thirst` **不是恒带**（见 `BARE_TOKEN_BY_RULE` 上方
#      那一段的实测读数），按词规则会把该词的**别的位置**也画上 ⇒ 只能按卡。
#    · 本卡那一处落在**引号里的被授予能力**（`have "Slay: Gain Blood Thirst this turn"`）⇒ 句中、
#      句首关键词那一支（要求行首 / `. ` / `; ` 之后）**扫不到** ⇒ 也只能按卡。
# **判据 = 成品卡图亲读**（铁律 7，第一权威）：`d:/2/Warpforge部队卡片/Orks/3部队/
#    Warpforge_16_Beastboss-on-Squigosaur.png`（可复查裁图 `_tmp_view/a1703_crops/MINE_GOF90_bt.png`，
#    裁 x120-820 / y780-1000，3×）——卡面那一行是
#    `have "〔橙底骷髅+准星徽〕Slay: Gain 〔红底银环白獠牙圆徽〕Blood Thirst this turn"`
#    ⇒ **`Blood Thirst` 左侧确实印着那枚獠牙圆徽**（= `Resources/Art/traits/bloodThirst.png`）。
# 🔴 **转录表在这一处是【假阴性】**：`资料/卡表核对_卡图提取/_还原效果文字.md:628` **没写双写**
#    ⇒ `A1703` 的逐词复算把 `Blood Thirst` 算成「不是恒带、反例 = `Beastboss on Squigosaur`」——
#    **那是转录表的读数、不是卡面的读数**（卡面上有图标）。本表以**卡面**为准（`A1703` 的冻表里
#    那一行另有「亲读更正」标注，见 `A1703_READ_CORRECTED`）。
# ⚠️ **改前实测**：本卡 `desc` 的计划里**只有 `Slay` 一条**、`bloodThirst` 一枚都没有 ⇒
#    **这一条是补出来的、不是恒绿**（与 `_FAMILY8` 那批「只补表、判据一字未动」的情形不同）。
_GOF90_BT_WHY = ("卡面 `Blood Thirst` **前面**那枚「尖牙」（= `Resources/Art/traits/bloodThirst.png`）"
                 "—— 判据 = 成品卡图亲读 `d:/2/Warpforge部队卡片/Orks/3部队/"
                 "Warpforge_16_Beastboss-on-Squigosaur.png`（裁图 `_tmp_view/a1703_crops/MINE_GOF90_bt.png`）："
                 "`have \"〔橙底骷髅+准星徽〕Slay: Gain 〔红底银环白獠牙圆徽〕Blood Thirst this turn\"`。"
                 "⚠️ 这一处在**引号里的被授予能力**（句中）⇒ 句首关键词那一支扫不到，**只能按卡**（`A1713`）。"
                 "⚠️ 转录表 `_还原效果文字.md:628` 在这一处**漏记了图标**（假阴性）—— 本表以卡面为准。")
BARE_TOKEN_BY_CARD[("GOF91", "Blood Thirst")] = ("bloodThirst", _GOF90_BT_WHY)
BARE_TOKEN_BY_CARD[("GOF91", "嗜血")] = (
    "bloodThirst", _GOF90_BT_WHY + "中文侧同一条（`descZh` 里写 `嗜血`，与英文那枚逐处对应）。")

# ── 🆕 2026-10-10（`A1400`）：同类【第 9 个】词 —— `Invulnerable`（「蓝紫圆 + 白骷髅 + 横条」）──
# 🔴 **全部读数 / 逐卡判据 / 误伤面两层验证**：`资料/普查产出_第十四会话/W_A1400_Invulnerable.md`。
#
# ① **根因与 `A1366` / `A1376` 是同一个**：`main()` 里那道
#    `if not toks and not syms and not kws and not res and not btok: continue` 会把**整份字段**跳过，
#    而裸 token 表在它**之后**才查。那道判据 `A1366` 已修好（把 `btok` 并了进去）
#    ⇒ **本批只补表、判据一字未动** —— 但本批**自己复验了一遍它对这些卡确实生效**（不是假设）：
#    28 条新增里有 **10 条**落在「改动前整份字段一条记号都没有（`AM36` / `GSC47` / `GSC58` /
#    `SW15` / `UM52` 五张卡的 `desc` + `descZh`）」里 —— 若那道判据没修，这 10 条**一条都不会出现**。
#
# ② **逐张判据 = 成品卡图**（铁律 7）。主判据 = `资料/卡表核对_卡图提取/_还原效果文字.md`
#    （29 个子代理逐张看卡图抄的还原表）—— 卡面上「**徽记 + 紧跟那个词**」它写成**双写**
#    （`Invulnerable Invulnerable` / `InvulnerableInvulnerable` = 图标 + 词）⇒ 双写 = 那一处真有图标。
#    **全池 16 张卡**里该词一共出现 **16 处**，还原表覆盖其中 **15 张 / 15 处、15/15 全部双写**；
#    第 **16** 张 `DA_Rites_of_Penance` 不在那张表里（它来自 `6秘密/` 的**手机翻拍**）⇒
#    本批作者**亲读** `d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3698.jpg`：
#    `Give +3〔红棕圆+白拳〕, +3〔紫圆+白枪〕 and 〔深蓝圆+白骷髅+带缺口横条〕Invulnerable
#     to a friendly unit until your next turn` —— 同一枚徽记。
#    ⇒ **恒带 16/16**（词在句首 / 句中 / 作表语 `becomes Invulnerable` 全带）。
#
# ③ 🔴 **`ASH44` / `SOR52` 两张【必须排除】**（本表**不列**它们）—— 它们的 `desc` 里那个词
#    **已经被画过了**，走的是**方括号记号**那条路（`TOKEN_BY_CARD` 的 `[Invulnerable]`，
#    见本文件 `:128-129`）。实测这两张的原文是 **`[Invulnerable] Invulnerable` 双写形式**
#    （`ASH44` = `3 [Spirit Stone]: Gains [Invulnerable] Invulnerable until your next turn`）——
#    **方括号那一支是「整串换掉」**（词被吃掉）⇒ 那一枚图标已经出现；再补一条**裸 token**
#    就会在同一个词前**再插一枚、画两枚**。亲读两张成品图各只有**一枚**：
#    `Aeldari/3部队/Warpforge_44_Wraithknight.png`（`Gains〔骷髅徽〕Invulnerable`）与
#    `Sorotitas/4计策/Warpforge_52_Miraculous-Feat.png`（`Give〔骷髅徽〕Invulnerable`）。
#    ⇒ **14 张卡 × 2 字段 = 28 条**（与 `A1400` 台账一致）。
#
# ④ 🔴 **与 `A1376` 那批的 `BL60 Vulnerable 2` 特例【安全共存】—— 实测过，不是推断**：
#    · `Invulnerable`(12 字) 与 `Vulnerable 2`(12 字) **等长** ⇒ 先比长度、再 `CompareOrdinal`
#      ⇒ `"Invulnerable" < "Vulnerable 2"`（`I` 0x49 < `V` 0x56）⇒ **`Invulnerable` 先换**。
#    · `Rewrite` 那一支是 `s.Replace(token, bare)`、**Ordinal（区分大小写）** ——
#      换出来的 `bare` 里是小写 `invulnerable`（图名 + link id），**不含**大写开头的 `Vulnerable`
#      ⇒ 轮到 `Vulnerable 2` 时**只命中剩下那一处**（`give Vulnerable 2 to all enemy troops`）。
#    · 顺带更正 `A1376` ④ 那句话的一个细节：它写「token 写 `Vulnerable` 会把 `Invulnerable` 里
#      那半截一起换掉」—— **按 `Rewrite` 的语义那是不可能的**（`Ordinal` 区分大小写，
#      `Invulnerable` 里那半截是小写 `vulnerable`，大写 `Vulnerable` 匹配不上）。
#      **它最后选 `Vulnerable 2` 仍然是对的**（该卡内唯一、且数字本来就在同一行），
#      ⛔ **本批不改它**（改了要重走一遍验证、收益为 0）。这里只就地把那句话标成「过度保守」。
#    · 实测（§报告 §三 的渲染层全池逐字比对）：`BL60` 那一处**只动该动的两处**、无误伤。
#
# ⑤ **子串冲突：全池枚举过，`Invulnerable` 这一侧为 0**（反向也查了）：
#    · **没有更长的英文词含它**（`\w*invulnerab\w*` 全池扫描：除 `Invulnerable` 本身 **0 处**；
#      大小写不敏感扫 `invulnerable` 也只有那 16 张 16 处）· `keywords` 数组里 **0 处**。
#    · **它也不落在任何别的 token 里**（逐字段枚举「同字段里一个 token 是另一个的子串」——
#      14 张目标卡的 `desc`/`descZh` **两组都没有**；全池也只在 `BL60` 一处有该形状，见 ④）。
#    · 中文侧 `无敌`：全池只在那 16 张的 `descZh` 里各 1 处；**没有更长的中文词含它**
#      （`*无敌*` 全池扫描 **0 处**）。
#
# ⑥ **为什么逐卡列、不写成一条「按词」规则**（同 `A1366` ⑤ / `A1376`）：本表每一条都要能追到
#    **那一张卡**的出处，14 张每张都有（见 ②）。代价 = **新卡若含 `Invulnerable`，要在这儿补一行**；
#    「盘上有图、计划表一条都没用上」那一**整类**缝由 `A1370` 的对账去兜（不在本表射程里）。
#
# ⑦ 中文侧**同 `A1366` / `A1376`**：玩家实际看到的是中文（`BattleDriver.FaceTextFull`：
#    `DescZh` 有值就用它）⇒ **只补英文那半 = 中文卡面照旧少一枚**。中文 token = `无敌`
#    （14 张的 `descZh` 写法**完全一致**、逐张核过，没有第二种译法）。
#    ⚠️ 中文这半的判据就是
#    「英文那枚在图前、位置一一对应」。
#    🔴 **2026-10-14 更正（铁律 5）**：这里原来写「**原版没有中文表**（那份中文是我们自己译的）」——
#       **不成立**（原版**有**官方中文表）。**判据全文与本文件的口径 → `_STUN_CARDS` 上方那条
#       `2026-10-14` 更正**（⛔ 别在这儿抄第二份）。结论不变，**理由换成「PnP 成品卡图印的是英文」**。
# (卡 id, 判据出处, 卡图文件名)
#   判据出处 = 逐张抄录里 `Invulnerable` **双写**的那一行（`资料/卡表核对_卡图提取/_还原效果文字.md:NN`）。
_INVULNERABLE = [
    ("ASH16", "`资料/卡表核对_卡图提取/_还原效果文字.md:32`",
     "Aeldari/3部队/Warpforge_21_Nuadhu-Fireheart.png"),
    ("98cd4c021d615174aba88df6012219c4", "`资料/卡表核对_卡图提取/_还原效果文字.md:147`",
     "Astra Militarum/2天赋/Warpforge_36_Renowned-Bodyguard.png"),
    ("BL44", "`资料/卡表核对_卡图提取/_还原效果文字.md:248`",
     "Chaos/4计策/Warpforge_60_Daemonic-Pact.png"),
    ("DA65", "`资料/卡表核对_卡图提取/_还原效果文字.md:280`",
     "Dark Angels/3部队/Warpforge_08_Chaplain-Gabutheron.png"),
    ("GSC57", "`资料/卡表核对_卡图提取/_还原效果文字.md:475`",
     "Genestealer Cult/4计策/Warpforge_47_Perfect-Ambush.png"),
    ("GSC41", "`资料/卡表核对_卡图提取/_还原效果文字.md:486`",
     "Genestealer Cult/4计策/Warpforge_58_Open-Insurrection.png"),
    ("SOR33", "`资料/卡表核对_卡图提取/_还原效果文字.md:748`",
     "Sorotitas/3部队/Warpforge_38_Saint-Celestine.png"),
    ("SW15", "`资料/卡表核对_卡图提取/_还原效果文字.md:793`",
     "Space Wolves/3部队/Warpforge_18_Fyrri-Askar.png"),
    ("SW26", "`资料/卡表核对_卡图提取/_还原效果文字.md:804`",
     "Space Wolves/3部队/Warpforge_29_Arjac-Rockfist.png"),
    ("TL45", "`资料/卡表核对_卡图提取/_还原效果文字.md:991`",
     "Tyranid/4计策/Warpforge_61_Apex-Predator.png"),
    ("UM102", "`资料/卡表核对_卡图提取/_还原效果文字.md:1061`",
     "Ultramarines/3部队/Warpforge_24_Terminator-Captain.png"),
    ("UM111", "`资料/卡表核对_卡图提取/_还原效果文字.md:1101`",
     "Ultramarines/4计策/Warpforge_32_Shall-Know-No-Fear.png"),
    ("UM60", "`资料/卡表核对_卡图提取/_还原效果文字.md:1111`",
     "Ultramarines/4计策/Warpforge_52_Humanitys-Shield.png"),
    # ⚠️ 第 14 张**不在** `_还原效果文字.md` 里（它来自 `6秘密/` 的 5 张**手机翻拍**，
    #    同 `DA_None_Must_Know`）⇒ 判据 = 本批作者**亲读** `IMG_3698.jpg`（见 ②）。
    ("e01e8e933ad574b44bce5473ae486667", "`资料/卡表核对_卡图提取/DarkAngels__3.md:11` **＋** 亲读 "
                            "`d:/2/Warpforge部队卡片/Dark Angels/6秘密/IMG_3698.jpg`",
     "Dark Angels/6秘密/IMG_3698.jpg"),
]
# 英文 + 中文**两半一次生成**（只写英文那半 = 中文卡面照旧少一枚；见 ⑦）。
# token 就是那个词本身（`Invulnerable` / `无敌`）；运行时走 `CardIcons.Rewrite` 的
# **裸关键词那一支**（插在**词前**、词留着、外面包 `<link>` ＋ `<nobr>`）—— 与 `Stun` / `_FAMILY8` 同一条路。
for _inv_cid, _inv_src, _inv_png in _INVULNERABLE:
    _inv_why = ("卡面 `Invulnerable` **前面**那枚「蓝紫圆 + 白骷髅 + 带缺口横条」"
                "（= `Resources/Art/traits/invulnerable.png`；⛔ **不是** `vulnerable.png` 那张盾+闪电，"
                "两者是不同的两张图 —— 亲读并排核过）—— 逐张判据："
                f"{_inv_src}（`{_inv_png}`）。")
    if _inv_cid == "BL44":
        _inv_why += ("⚠️ 本卡同一段正文里**还有** `Vulnerable 2`（`A1376` 按卡补的）——"
                     "两条**安全共存**（`Rewrite` 是 `Ordinal` 替换、`invulnerable` 图名不含大写 `Vulnerable`），"
                     "见本块 ④。")
    if _inv_cid == "e01e8e933ad574b44bce5473ae486667":
        _inv_why += ("⚠️ 这一张**不在** `_还原效果文字.md` 里（`6秘密/` 的手机翻拍），"
                     "判据 = 本批作者亲读那张 `.jpg`。")
    BARE_TOKEN_BY_CARD[(_inv_cid, "Invulnerable")] = ("invulnerable", _inv_why)
    BARE_TOKEN_BY_CARD[(_inv_cid, "无敌")] = (
        "invulnerable", _inv_why + "中文侧同一条（`descZh` 里写 `无敌`，位置与英文那枚一一对应）。")

# ── 🆕 2026-10-10（`A1378`）：**按词**的裸 token 规则（`BARE_TOKEN_BY_CARD` 那一族的"按规则"版）──
# 🔴 **为什么要有这一张**（`A1378` 的原话）：上面 `_STUN_CARDS` / `_FAMILY8` / `_INVULNERABLE`
#    三块都是**逐卡枚举**，代价 = **新卡若含这个词，要回表补一行**；而生成器**只报「死条目」、
#    报不出「漏了新卡」**（漏了不会有任何提示）。⇒ 凡是**恒带**（卡面上这个词**不分位置、不分词性、
#    一律带图标**）的词，就不该靠枚举撑 —— 改成按词。
#
# **判据（逐词核，2026-10-10 实测）** = ① 英文 `\b词\b` 的**卡集** == 中文那个词的**卡集**；
#    ② 每题恰好 1 处；③ 全池没有「更长的词含它」（按词规则是**纯子串替换**，`_t in text`）。
#    逐词读数见每条后面的括号，全部可复核（复算脚本见
#    `资料/普查产出_第十四会话/W_icon工具侧三笔.md` §二）。
#
# **不改按词的（如实留按卡）** —— 这三条**不是恒带**，按词会把卡面**多画一枚原版没有的图标**：
#    · `Blood Thirst` / `嗜血` —— 8 张里 7 张带（`GOF90 Beastboss on Squigosaur` 卡面**确实没画**）
#      🔴 **2026-10-12 就地更正（铁律 5）：上面这半句是【错的】** —— 主对话裁图亲读
#      `d:/2/Warpforge部队卡片/Orks/3部队/Warpforge_16_Beastboss-on-Squigosaur.png`
#      （裁 x120-820 / y780-1000，3×；可复查图 `_tmp_view/a1703_crops/MINE_GOF90_bt.png`）：
#      卡面 `have "〔橙底骷髅+准星徽〕Slay: Gain 〔红底银环白獠牙圆徽〕Blood Thirst this turn"` ——
#      **`Blood Thirst` 左侧确实印着图标**（红底银环白獠牙圆徽，与 `Slay:` 左那枚同款同尺寸）。
#      **错因**：上一轮那条「确实没画」是**另一次亲读**得出的，本轮亲读把它推翻了（两次亲读直接打架，
#      按卡图判 ⇒ 本轮为准）。⚠️ 连带：转录表
#      `资料/卡表核对_卡图提取/_还原效果文字.md` 在那一处**也漏记了图标** ⇒ 该表**至少有这一处假阴性**。
#      ✅ **2026-10-13（`A1713`）已按卡补上** —— `BARE_TOKEN_BY_CARD[("GOF91", "Blood Thirst")]`
#      / `("GOF91", "嗜血")`（见 `_GOF90_BT_WHY` 那一段），**那一枚从现在起画出来了**。
#      ⛔ 但它**仍然不进本表**（`Blood Thirst` 不是恒带 ⇒ 按词会给别的位置多画，见本段开头）。
#    · `Blind` / `失明`         —— 6 张里 5 张带（`SAU55 Solar Pulse` 同）
#    · `Relentless`             —— 0/1（`SAU3` 卡面上那个词是**天赋名** `Relentless March`）
#    ⇒ 这三条仍在 `_FAMILY8` / 不建条目（判据见那个块 ③）—— ⚠️ `Blood Thirst` 那一条已改成**按卡**补。
#
# 🔴 **两道守卫**（`main()` 里那 9 行；两道都是"**这一枚图标已经有主**，规则别插手"）——
#    缺了它们 = 同一处**画两枚**（静默、只在个别卡上现形）：
#    ① **方括号守卫**：该卡文本里已有 `[token]`（`Invulnerable` 的 `ASH44` / `SOR52` 就是
#       `Gains [Invulnerable] Invulnerable` —— 图标由**方括号那一支**画，紧跟的那个裸词是**词本身**）
#       ⇒ 整卡跳过。⚠️ 这条是**通用**的，不是给那两张卡开的后门。
#    ② **逐卡表优先**：该卡在本字段里已有**同 sprite** 的逐卡裸 token（判据更细）⇒ 规则不覆盖它。
#       它同时把 `A1376` 的两个特例一并兜住：`BL60` 的 `Vulnerable 2`（见那个块 ④）、
#       `SAU54` 的 `易伤` —— 两条都**原样保留**（本条**不动**它们的结论）。
#    ⇒ **因此本表在今天的池子上"一条都不新增"**（恒带 = 每一处都已在逐卡表里）：这是**预期**，
#      不是"规则没生效" —— 生效的证据是**新卡**：往池里加一张含 `Stun` 的卡，它会自动拿到 `stun`
#      （合成池 A/B 实测见报告 §三）。**逐卡表仍然是"判据载体"**（文档逐卡列出处），规则是**覆盖兜底**。
#
# ⚠️ **`Sniper` 的 13/13 与这里无关**：那 13 张里 **8 张只写在 `keywords` 数组里**，
#    而本脚本**只扫 `desc`/`descZh`** ⇒ 本表只管 `desc` 里那 5 张（中文 4 张 ——
#    `ASH_Bright_Lance_Vyper` 的 `descZh` 里**没有「狙击」**，是中文数据缺口、**另记**）。
BARE_TOKEN_BY_RULE = [
    # (裸 token, sprite, 判据 / 实测读数)
    # 🔴 **2026-10-11（第十七会话 · `A1422` 的第一族）：`Stealth` / `潜行` 各加一条**
    #    —— 判据来自 `资料/卡表核对_卡图提取/_还原效果文字.md` 的**逐卡转录**（那 29 个子代理
    #    逐张看卡图抄的，**徽记位置写成 `Word Word` 双写**），逐张读数：
    #    · 「英文文本里有 `Stealth`」而**计划表原来一枚都没画**的 **26 张**，逐张点过
    #      `Stealth` 在 `desc` 里的**出现次数** vs 卡面转录里的**双写次数** ⇒ **28/30 处一一对上**
    #      （30 = 那 26 张的合计处数；`DA30`/`GSC74`/`TL20` 各 2 处，其余 1 处）。
    #      只有两处对不上：`TL24 Von Ryans Leaper`（正文 1 处 / 双写 0）与
    #      `UM_Tyrannic_War_Veteran`（同）—— 那两张**计划表要么本来就没画、要么早已画着**
    #      （`TL24` 有逐卡条目 ⇒ 守卫②跳过），**规则不会多画**。
    #    · 中文侧同理：那批卡的 `descZh`「潜行」与卡面转录的双写**逐处对应**。
    #    ⛔ **这条与 `资料/普查产出_第十四会话/W_中文徽记缺口批.md` §四·3 的结论【打架】**
    #      （那份说「desc 侧会新画的 26 张…那些绝大多数是句中『提及』，而原版卡面那些位置**不印徽记**」）
    #      ⇒ **按铁律 5 以现读为准**：那份当时**没有逐张对转录表**，只做了「加规则会多画多少张」的
    #      计数；本次逐卡对齐了**处数** ⇒ 「不印徽记」那半句**不成立**（先例：`SAU55 Solar Pulse`
    #      的卡面 `…they lose 〔眼〕Stealth and 〔迷彩〕Camouflage`，`W_图标族8词.md` 早已亲读）。
    #    ⚠️ **已知的一处中文多写**：`GSC25` 的 `descZh` 里有「潜行」而英文 `desc` 里没有对应处
    #      （⚠️ 括号里原来写「中文是我们自己译的 ⇒ **原版没有中文卡面可核**」—— 后半句 **2026-10-14 更正
    #        （铁律 5）**「原版没有中文表」**不成立**：原版**有**官方中文表；判据全文与本文件的口径 →
    #        `_STUN_CARDS` 上方那条 `2026-10-14` 更正。**本处结论不变**：「`GSC25` 中文多写」这件事
    #        与官方表无关，它指的是**我们的 `descZh` 与英文 `desc` 逐处对不上**）
    #        —— 那一处**会多画一枚**，如实记在这里。
    ("Stealth", "stealth",
     "**卡面逐处双写 28/30**（26 张「正文里有 `Stealth` 而计划表没画」的卡，逐张核 `_还原效果文字.md`；"
     "`DA30`/`GSC74`/`TL20` 各 2 处）—— 出处与两处对不上的（`TL24` / `UM_Tyrannic_War_Veteran`）见本表上方那段。"),
    ("潜行", "stealth",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）；⚠️ `GSC25` 是中文多写的那一处（见上方那段）。"),
    # 🔴 **2026-10-11（第十七会话 · `A1381` 的「真因」那一笔）：`Shuriken` / `星镖` 各加一条**
    #    —— 触发事实：`ASH65 Inspiring Legacy` 的 `desc` 里那枚记号被 OCR 写成了 `<i>…</i>`
    #    （`数据/游戏数据/card_stats.json` 的 `desc` 就是 `Give <i>Shuriken</i> 2 …`，
    #     它的 `ocrSrc` 指向 PnP 原图 ⇒ **`<i>` 是 OCR 的产物**）。**亲读卡面**：
    #     `d:/2/Warpforge部队卡片/Aeldari/5防御卡/Warpforge_65_Inspiring-Legacy.png`
    #     = `Give 〔旋刃徽记〕Shuriken 2 to a friendly unit this turn` ⇒ **那个位置是一枚图标**、
    #     词是**正体**（不是斜体）⇒ **我们卡面少画了那一枚**（`ASH65` 原来**整张不在计划表里**）。
    #    **判据（逐卡对齐，`_还原效果文字.md` 的转录双写）**：正文里带 `Shuriken`/`星镖`、
    #    而计划表**一枚都没画**的卡 **10 张**，逐张点「出现次数 vs 卡面双写次数」⇒
    #    **9 张 1:1 全对上**（`ASH_Orian_Laratharjos` / `ASH57` / `ASH58` / `ASH_Flickerjump` /
    #    `ASH65` / `ASH4` / `ASH54` / `ASH77` / `ASH76`）。
    #    🔴 **2026-10-12 就地订正（`A1610①` · 铁律 5）**：本行原来写着「第 10 张
    #    `ASH_Ahnakh_Yth_Shrine` **转录表里没有这一行**（只报「没核」）」—— **那句话是错的**，
    #    实测那行**一直在表里**（`_还原效果文字.md` 的 `Ahnakh-Yth Shrine.png` 那行，
    #    双写 `ShurikenShuriken` **1 处**，与 `desc` 里 `Shuriken` 的 1 处 **1:1**）
    #    ⇒ 这一张**是核过的**、结论同另外 9 张。错因大概是当时按**卡 id** 去表里找（表里只有卡名/文件名）。
    #    ⇒ 现在是 **10/10 全对上**。
    #    ⚠️ 出生在那 20 张里的另外 10 张本来就走**句首**那一支（`Waystone. Shuriken 2`）⇒ 守卫③跳过。
    ("Shuriken", "shuriken",
     "**卡面逐处双写 10/10**（10 张「正文里有 `Shuriken` 而计划表没画」的卡，逐张核 `_还原效果文字.md`；"
     "`ASH_Ahnakh_Yth_Shrine` 那一行 2026-10-12 补核 —— 见本表上方那段订正）——"
     "出处与亲读卡面见本表上方那段。"),
    ("星镖", "shuriken",
     "同英文那条（`descZh` 的写法，出现次数与英文那批**逐处对应**）。"),
    ("Stun", "stun",
     "**恒带 30/30**（英文 30 张 30 处 · 中文 `眩晕` 同一集合 30 张 30 处 · 无更长词含它 · 方括号记号 0 处）"
     "—— 逐张判据见上面 `_STUN_CARDS`（含 `DA_None_Must_Know` 那张手机翻拍）。"),
    ("眩晕", "stun", "同上（`descZh` 的写法；与英文那 30 张**一一对应**）。"),
    ("Invulnerable", "invulnerable",
     "**恒带 16/16**（英文 16 张 · 中文 `无敌` 同一集合 16 张 · 无更长词含它）"
     "—— 其中 `ASH44` / `SOR52` 走 `[Invulnerable]` 方括号那一支（守卫①跳过，图标由它画），"
     "其余 14 张见 `_INVULNERABLE`。"),
    ("无敌", "invulnerable", "同上（`descZh` 的写法；`ASH44`/`SOR52` 亦写 `[无敌] 无敌` ⇒ 同样走守卫①）。"),
    ("Vulnerable", "vulnerable",
     "**恒带 14/14**（英文 14 张 14 处 · 中文 `脆弱`/`易伤` 同一集合 14 张 14 处 · 每题恰 1 处）"
     "—— 逐张判据见 `_FAMILY8`；⚠️ `BL60` 那一条（token = `Vulnerable 2`）由逐卡表承接（守卫②）。"),
    ("脆弱", "vulnerable", "同上（13 张的中文写法）。"),
    ("易伤", "vulnerable", "⚠️ `SAU54 Curse of the Phaeron` 的**唯一**中文写法（13 张写 `脆弱`、它写 `易伤`）"
                              "—— 按卡那条一直在，这里列出来是给**新卡**兜底（同 sprite ⇒ 守卫②会让逐卡表赢）。"),
    ("Sabotage", "sabotage",
     "**恒带 14/14**（英文 14 张 15 处 · 中文 `破坏` 同一集合 14 张 15 处；`GSC60` 那个字段有 2 处，"
     "`Rewrite` 是全局替换 ⇒ 两处都画）—— 逐张判据见 `_FAMILY8`。"),
    ("破坏", "sabotage", "同上（`descZh` 的写法）。"),
    ("Sniper", "sniper",
     "**恒带**：`desc` 里 **5/5**（`ASH9` / `ASH14` / `ASH_Bright_Lance_Vyper` / `GSC_Master_Outrider` / `UM17`，"
     "五张全在 `_FAMILY8`）· 中文 `狙击` **4 张**（`ASH_Bright_Lance_Vyper` 的 `descZh` 里没有那个词 = 数据缺口）。"
     "⚠️ `keywords` 数组里另有 8 张只写关键词、**不在本脚本射程**（合并口径见 `W_图标族8词.md`）。"),
    ("狙击", "sniper", "同上（`descZh` 的写法）。"),
    ("Fast", "fast", "**恒带 5/5**（英文 5 张 5 处 · 中文 `迅捷` 同一集合 5 张 5 处）—— 逐张判据见 `_FAMILY8`。"),
    ("迅捷", "fast", "同上（`descZh` 的写法）。"),
    ("Hunt Mark", "huntMark",
     "**恒带 22 张 25 处 / 25 处**（中文 `猎杀标记` 同一集合；三张卡各有 2 处）"
     "—— ⚠️ 它**不是**「每题 1 处」那一档，但两侧**逐处对齐**，且 `Hunt` ⊂ `Hunter(s)` **不适用**"
     "（token 是 `Hunt Mark` 两个词）。逐张判据见 `_FAMILY8`。"),
    ("猎杀标记", "huntMark", "同上（`descZh` 的写法）。"),
    # ══════════════════════════════════════════════════════════════════════════
    # 🆕 **2026-10-12（`A1613①`）**：把 `A1422` 没做完的**其余词**按同一套口径补齐 ——
    #    `A1422` 只做了 `Stealth` / `潜行` 一族（另加 `A1381` 的 `Shuriken` / `星镖`）。
    #
    # 🔴 **判据 = 卡面逐张转录的「双写」**（`资料/卡表核对_卡图提取/_还原效果文字.md`：
    #    29 个子代理逐张看卡图抄的；原版卡面写「图标 + 词」⇒ 转录里就是 `Word Word` / `WordWord`）。
    #    逐词读数 = 「该词在 `desc` 里的出现处数」vs「转录里双写的处数」**逐卡对**：
    #    下面这些词**每一张都对得上**（双写 ≥ 出现），且**没有一张**是「我们提了、卡面没印」。
    #    ⚠️ 中文那半**不是**「原版中文卡面核过」—— 原版没有中文表（中文是我们自己译的），
    #       判据只能到「英文那枚在图前、中文逐处对应」。
    #    🔴 **2026-10-14 更正（铁律 5）**：其中「**原版没有中文表**」**不成立**（原版**有**官方中文表）；
    #       **判据全文与本文件的口径 → `_STUN_CARDS` 上方那条 `2026-10-14` 更正**（⛔ 别抄第二份）。
    #       ⇒ **「判据只能到英文那枚在图前、中文逐处对应」【仍成立】**，理由换成
    #       「**转录表只覆盖英文卡面**（PnP 成品卡图印的是英文）」+「我们的 `descZh` 是自译的」。
    #
    # ⚠️ **三道守卫照旧**（`main()` 里那 9 行，见上面那段）：
    #    ① 本卡文本里有 `[该词]` 方括号记号 ⇒ 整卡跳过（图标已由方括号那一支画）；
    #    ② 本卡已有同 sprite 的**逐卡**裸 token ⇒ 规则不覆盖（判据更细）；
    #    ③ 该词落在**本字段已注定要画**的某条 token 里面 ⇒ 跳过（防「一条是另一条的子串」双画）。
    #
    # 🔴 **逐词读数（2026-10-12 全池实测）**：
    #    · `Flank`/侧翼 → `flank`：66 卡 66 处 · 计划表原画 19 卡 · **卡面双写 67 处** ⇒ 缺 47 卡
    #    · `Shield`/护盾 → `shield`：33 卡 37 处 · 原画 11 卡 · 双写 34 处 ⇒ 缺 23 卡
    #    · `Vanguard`/先锋 → `vanguard`：48 卡 49 处 · 原画 26 卡 · 双写 48 处 ⇒ 缺 22 卡
    #    · `Camouflage`/伪装 → `camouflage`：20 卡 20 处 · 原画 8 卡 · 双写 20 处 ⇒ 缺 12 卡
    #    · `Markerlight`/标记光 → `markerlight`：15 卡 17 处 · 原画 1 卡 · 双写 16 处 ⇒ 缺 14 卡
    #    · `Blast`/爆裂 → `blast`：41 卡 41 处 · 原画 24 卡 · 双写 42 处 ⇒ 缺 17 卡
    #    · `爆破`（中文别名） → `blast`：同上（中文别名写法，`A1418` 起就在词表里）
    #    · `Slay`/斩杀 → `slay`：20 卡 21 处 · 原画 14 卡 · 双写 20 处 ⇒ 缺 6 卡
    #    · `Ferocity`/狂暴 → `ferocity`：10 卡 11 处 · 原画 6 卡 · 双写 11 处 ⇒ 缺 5 卡
    #    · `Backlash`/反噬 → `backlash`：8 卡 8 处 · 原画 4 卡 · 双写 8 处 ⇒ 缺 4 卡
    #    · `Duty`/职责 → `duty`：22 卡 22 处 · 原画 18 卡 · 双写 22 处 ⇒ 缺 4 卡
    #    · `Pack`/兽群 → `pack`：7 卡 8 处 · 原画 4 卡 · 双写 10 处 ⇒ 缺 4 卡
    #    · `Regeneration`/再生 → `regeneration`：9 卡 9 处 · 原画 5 卡 · 双写 9 处 ⇒ 缺 4 卡
    #    · `Sentry`/哨戒 → `sentry`：5 卡 6 处 · 原画 2 卡 · 双写 7 处 ⇒ 缺 4 卡
    #    · `Swarm`/虫群 → `swarm`：11 卡 11 处 · 原画 8 卡 · 双写 12 处 ⇒ 缺 3 卡
    #    · `Synapse`/突触 → `synapse`：10 卡 11 处 · 原画 8 卡 · 双写 12 处 ⇒ 缺 3 卡
    #    · `Mob`/群体 → `mob`：13 卡 14 处 · 原画 11 卡 · 双写 13 处 ⇒ 缺 2 卡
    #    · `Teleport`/传送 → `teleport`：9 卡 9 处 · 原画 7 卡 · 双写 9 处 ⇒ 缺 2 卡
    #    · `Flying`/飞行 → `flying`：42 卡 42 处 · 原画 27 卡 · 双写 46 处 ⇒ 缺 15 卡；中文 44 张（多 `TAU14`/`TAU20`：卡面确实印着那枚，是我们 `desc` 漏了那个词）。
    #    · `Long Range`/远射 → `longrange`：10 卡 10 处 · 原画 7 卡 · 双写 11 处 ⇒ 缺 3 卡；中文 11 张（多 `TAU42`，同上）。
    #    · `Uprising`/起义 → `uprising`：6 卡 6 处 · 原画 5 卡 · 双写 6 处 ⇒ 缺 1 卡
    #    · `Rally`/集结 → `rally`：85 卡 85 处 · 原画 84 卡 · 双写 84 处 ⇒ 缺 1 卡
    #    · `Stomp`/践踏 → `stomp`：7 卡 10 处 · 原画 9 处 · 双写 8 处 ⇒ 缺 1 卡
    #    · `Tide`/潮涌 → `tide`：19 卡 19 处 · 原画 18 卡 · 双写 19 处 ⇒ 缺 1 卡；中文 20 张（多 `TL79`，同上）。
    #
    # 🔴 **本批刻意没走按词、改走逐卡表（`_A1613_GAP`）的词**（每一条都有实测读数，逐个说明见那张表上方）：
    #    `Armour` · `Codex` · `Remnant` · `Strike` · `Ambush` · `Talent` · `Destroyer` ·
    #    `Pray` · `Regiment` · `Ephemeral` · `Penitence` —— 卡面上至少有一处「我们提了、卡面没印」，
    #    或该词是更长词 / 卡名的子串。
    # ✅ **2026-10-12（`A1623`）`Dark Pact` 一族已做**（原写着「仍未做、已记读数：37 卡 39 处」）——
    #    走**按词规则**（见本表末尾那两条），读数与三处例外见那一段的注释。
    # ⏭ **仍未做**：`Quest Point`（已由 `QUEST_WORD` 那条老规则管，读数见报告）。
    ("Flank", "flank",
     "**恒带**（66 卡 66 处 · 计划表原画 19 卡 · **卡面双写 67 处** ⇒ 缺 47 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("侧翼", "flank",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Shield", "shield",
     "**恒带**（33 卡 37 处 · 原画 11 卡 · 双写 34 处 ⇒ 缺 23 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("护盾", "shield",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Vanguard", "vanguard",
     "**恒带**（48 卡 49 处 · 原画 26 卡 · 双写 48 处 ⇒ 缺 22 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("先锋", "vanguard",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Camouflage", "camouflage",
     "**恒带**（20 卡 20 处 · 原画 8 卡 · 双写 20 处 ⇒ 缺 12 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("伪装", "camouflage",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Markerlight", "markerlight",
     "**恒带**（15 卡 17 处 · 原画 1 卡 · 双写 16 处 ⇒ 缺 14 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("标记光", "markerlight",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Blast", "blast",
     "**恒带**（41 卡 41 处 · 原画 24 卡 · 双写 42 处 ⇒ 缺 17 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("爆裂", "blast",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("爆破", "blast",
     "**恒带**（同上（中文别名写法，`A1418` 起就在词表里））—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("Slay", "slay",
     "**恒带**（20 卡 21 处 · 原画 14 卡 · 双写 20 处 ⇒ 缺 6 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("斩杀", "slay",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Ferocity", "ferocity",
     "**恒带**（10 卡 11 处 · 原画 6 卡 · 双写 11 处 ⇒ 缺 5 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("狂暴", "ferocity",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Backlash", "backlash",
     "**恒带**（8 卡 8 处 · 原画 4 卡 · 双写 8 处 ⇒ 缺 4 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("反噬", "backlash",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Duty", "duty",
     "**恒带**（22 卡 22 处 · 原画 18 卡 · 双写 22 处 ⇒ 缺 4 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("职责", "duty",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Pack", "pack",
     "**恒带**（7 卡 8 处 · 原画 4 卡 · 双写 10 处 ⇒ 缺 4 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("兽群", "pack",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Regeneration", "regeneration",
     "**恒带**（9 卡 9 处 · 原画 5 卡 · 双写 9 处 ⇒ 缺 4 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("再生", "regeneration",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Sentry", "sentry",
     "**恒带**（5 卡 6 处 · 原画 2 卡 · 双写 7 处 ⇒ 缺 4 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("哨戒", "sentry",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Swarm", "swarm",
     "**恒带**（11 卡 11 处 · 原画 8 卡 · 双写 12 处 ⇒ 缺 3 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("虫群", "swarm",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Synapse", "synapse",
     "**恒带**（10 卡 11 处 · 原画 8 卡 · 双写 12 处 ⇒ 缺 3 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("突触", "synapse",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Mob", "mob",
     "**恒带**（13 卡 14 处 · 原画 11 卡 · 双写 13 处 ⇒ 缺 2 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("群体", "mob",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Teleport", "teleport",
     "**恒带**（9 卡 9 处 · 原画 7 卡 · 双写 9 处 ⇒ 缺 2 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("传送", "teleport",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Flying", "flying",
     "**恒带**（42 卡 42 处 · 原画 27 卡 · 双写 46 处 ⇒ 缺 15 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。中文 44 张（多 `TAU14`/`TAU20`：卡面确实印着那枚，是我们 `desc` 漏了那个词）。"),
    ("飞行", "flying",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Long Range", "longrange",
     "**恒带**（10 卡 10 处 · 原画 7 卡 · 双写 11 处 ⇒ 缺 3 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。中文 11 张（多 `TAU42`，同上）。"),
    ("远射", "longrange",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Uprising", "uprising",
     "**恒带**（6 卡 6 处 · 原画 5 卡 · 双写 6 处 ⇒ 缺 1 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("起义", "uprising",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Rally", "rally",
     "**恒带**（85 卡 85 处 · 原画 84 卡 · 双写 84 处 ⇒ 缺 1 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("集结", "rally",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Stomp", "stomp",
     "**恒带**（7 卡 10 处 · 原画 9 处 · 双写 8 处 ⇒ 缺 1 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。"),
    ("践踏", "stomp",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    ("Tide", "tide",
     "**恒带**（19 卡 19 处 · 原画 18 卡 · 双写 19 处 ⇒ 缺 1 卡）—— 逐处判据 = `_还原效果文字.md` 的**双写**。中文 20 张（多 `TL79`，同上）。"),
    ("潮涌", "tide",
     "同英文那条（`descZh` 的写法，与英文那批**逐处对应**）。"),
    # ── 🆕 2026-10-12（`A1623`）：`Dark Pact` / `黑暗契约`（**37 卡 / 39 处**）────────────────
    # 🔴 **为什么它能走按词**（`A1378` 的判据三条，逐条实测）：
    #    ① 英文 `Dark Pact` 的**卡集** == 中文 `黑暗契约` 的**卡集**（都 37 张，逐张对过）；
    #    ② **没有一处「卡面没印」** —— 逐张开图亲读（37 张 PnP 成品卡图，本批作者全读了一遍）
    #       ＋ 转录表的**双写**（`Dark PactDark Pact` = 图标 + 词）：**38 处双写对 38 处**（见下）；
    #    ③ 全池没有「更长的词含它」（`Dark Pact` 不被别的 token 包含；
    #       `黑暗契约` 只被 `[黑暗契约]` 包含 —— 那两张卡由**守卫①**挡掉）。
    #
    # **读数**（改前 → 改后，同一度量）：
    #   · `desc`：37 卡 39 处「`Dark Pact`」→ 改前**只画 3 处**（`BL19` 的 `❄`、`BL51` 的 `[Dark Pact]`、
    #     `BL69` 的 `[Chaos]`）⇒ **漏 36 处**；改后 **38 枚**（`BL45` 那一张有 2 处 ⇒ 卡面也是 2 枚）。
    #     ⚠️ 「39 处」比「38 枚」多的那 1 处是 `BL51` 的 `[Dark Pact] Dark Pact`：
    #     **词出现两次、卡面只印一枚**（`Chaos/4计策/Warpforge_51_Dark-Oratory.png` 亲读）。
    #   · `descZh`：41 处 → 改前**只画 3 处**（`BL19` 的 `❄`、`BL51` 的 `[黑暗契约]`、
    #     `BL69` 的 `[黑暗契约]`）⇒ **漏 35 处**；改后 **38 枚**（`BL45` 2 枚）。
    #     ⚠️ 中文侧多出来的那几处（`BL74` / `BL69` / `BL51`）都是**我们自己的译法**把词写了两遍，
    #     卡面只有一枚 ⇒ 见下面 `BL74` 那条与两处例外。
    #
    # 🔴 **三处例外**（本字段的徽记**已经由另一条记号画了**）—— 见 `DARKPACT_ALREADY`：
    #    `BL19` 两半（`❄`）、`BL69` 的 `desc`（`[Chaos]`）。
    #    ⚠️ `BL51` 两半与 `BL69` 的 `descZh` **不列**：它们是**守卫①**（本字段含 `[Dark Pact]` /
    #    `[黑暗契约]`）自动挡掉的 —— 那两处**token 本身就是那枚徽记**（`[Dark Pact] Dark Pact`
    #    渲染成 `〔徽记〕Dark Pact`，与卡面逐字一致）。
    #    ⛔ 别把守卫①那四格也抄进例外表：那会变成「表里写着、实际从没被用到」的死条目（自检会红）。
    #
    # 🔴 **sprite 一律 `markOfChaos`**（⛔ 别按「哪一位邪神」分四枚）—— 判据 = **原版的图集**：
    #    五张 `markOf*`（`Chaos`/`Khorne`/`Nurgle`/`Slaanesh`/`Tzeentch`）里，
    #    **三张逐字节相同**（`markOfChaos` == `markOfKhorne` == `markOfNurgle`，各 `9555` 字节、
    #    同一 sha），另两张只差 **15 / 6 个 `alpha=0` 像素的 RGB**（`markOfSlaanesh` / `markOfTzeentch`）
    #    ⇒ **渲染上一个像素都不差**（出处：`素材/Warpforge原版/UI图集/图集/40ktraiticonatlas/slices/`
    #    与同目录 `Sprite/*.json` 的 `m_Rect` —— 五张的 rect **各不相同**（`252,0`/`840,0`/`588,84`/
    #    `420,0`/`336,84`），即**不是**抽取时切错了同一块，是**原版自己**就这么五份近似的图）。
    #    本批另把 6 张卡的徽记裁下来并排看过（generic / Blood / Fate / Excess / `[Chaos]` / `Chaos Lord`）
    #    —— **同一张图**（红盘 + 白八芒星）。
    #    ⛔ 而 `darkpact.png` **盘上不存在**，拿它当 sprite 会被本脚本的 `missing` 自检报出来；
    #    `Badges.Alias` 那边也写着 `darkpact → markOfChaos`（**同一个口径、两处一致**）。
    #    ✅ 附带好处：只有 `markOfChaos` / `markOfSlaanesh` 在 `Badges.SpriteKey` 里登记成
    #    **规则书键 `darkpact`** ⇒ 走 `markOfChaos` 的图标 tooltip **有**规则书正文；
    #    换成 `markOfKhorne` 那三张会落到「小写化图名」当键（`Loc` 里没有 ⇒ tooltip 只剩名字）。
    ("Dark Pact", "markOfChaos",
     "**恒带**（37 卡 39 处 · 改前只画 3 处 · **卡面 38 枚**）—— 逐处判据 = `_还原效果文字.md` 的"
     "**双写**（`Dark PactDark Pact`）＋ 37 张成品卡图逐张开图亲读。见上面那一段。"),
    ("黑暗契约", "markOfChaos",
     "同英文那条（`descZh` 的写法；41 处 —— 多出来的几处是我们自己的译法把词写了两遍、"
     "而卡面只有一枚 ⇒ 例外见 `DARKPACT_ALREADY` 与 `_A1613_GAP` 里的 `BL74`）。"),
]

# 认得出、但**盘上没有这张图**的记号（真缺口，如实列出来，别静默跳过）
# ── **按卡**的已知缺口（认得出位置、但没有可用的图）────────────────────────────
# ⚠️ 2026-09-15 更正：这里原来挂着 `("DA5","[Shield]")` 与 `("DA12","[shield]")` 两条，
#    写着「原版卡面这枚图标没认定 / 要核实，别猜」。**现在认定了** —— 是暗黑天使的**任务点徽记**
#    （`icons/questPoints.png`），已升级成 `TOKEN_BY_CARD` 里两条带证据的（见那段末尾）。
#    当时的错因：只有「暗色圆徽 / 螺旋」这种**形状描述**、没有并排比对图；实测形状名不能当键
#    （78 张图被起过 180 个形状名）。**结论：卡面图标对不上时，直接裁下来跟 78 张图并排比，
#    别靠文字描述转述。**
# ── 🆕 2026-10-12（`A1613①`）：**逐卡**补 —— 上面按词规则**覆盖不到**的那几个词 ──────────
# 🔴 **为什么这几个词不能走按词规则**（每一种都实测过，读数在下面的注释里）：
#    · `Armour`     —— 108 张里 **2 张**卡面**没印**（`DA37`：我们 `desc` 写 `gain 1 Armour`、
#                      而卡面那处是 `gain 〔任务点〕instead`（**我们的 desc 错了**，数据侧另账）；
#                      `UM_Honour_Guard`：`Adjacent units have Armour 1`，卡面**就是纯文字**）。
#    · `Codex`      —— `UM_Avenging_Zeal` 的记号写作 `[Codex icon]`（守卫①只认 `[Codex]`⇒盖不住）、
#                      `UM87` 的第二次出现落在**天赋名** `Author of the Codex` 里。
#    · `Remnant`    —— `SAU61` 卡面那处没印；且 `Remnant` ⊂ `Remnants`（本表按卡，不受影响）。
#    · `Strike`/`Ambush` —— 卡面那处落在**卡名/天赋名**里（`Euphoric Strike` / `Empyric Ambush`）⇒ 按词会把图标画进名字。
#    · `Talent`     —— `SAU57` 例外。
#    · `Destroyer`/`Pray`/`Regiment`/`Ephemeral` —— 词是**更长词的子串**（`Destroyers` / `Prays` /
#                      中文「团」(⊂ 兵团/军团/…) / `临时路障`）⇒ **按词规则会画到词中间**，只能按卡。
#    ⇒ 这几族一律走**逐卡**（判据更细，正是 `_FAMILY8` 那条路）；每一行的出处 = 转录表里
#      **双写**的那一行（`资料/卡表核对_卡图提取/_还原效果文字.md:NN`）+ 卡图文件名。
#    ⚠️ 本表用到的英文/中文 token **逐卡挑过**（中文有时是「护甲」、有时是「装甲」，按该卡的
#      `descZh` 实际写法取；取不到就写 `None`、只做英文那半）。
# ── 🆕 2026-10-12（`A1613①`）：**逐卡**补 —— 上面按词规则**覆盖不到**的那几个词 ──────────
# 🔴 **为什么这几个词不能走按词规则**（每一种都实测过）：
#    · `Armour`     —— 108 张里 **2 张**卡面**没印**（`DA37`：我们 `desc` 写 `gain 1 Armour`、
#                      而卡面那处是 `gain 〔任务点〕instead`（**我们的 desc 错了**，已另记数据侧）；
#                      `UM_Honour_Guard`：`Adjacent units have Armour 1`，卡面**就是纯文字、没有徽记**）。
#    · `Codex`      —— `UM_Avenging_Zeal` 的记号写作 `[Codex icon]`（守卫①只认 `[Codex]`⇒盖不住）、
#                      `UM87` 第二次出现落在**天赋名** `Author of the Codex` 里。
#    · `Remnant`    —— `SAU61` 卡面那处没印（且 `Remnant` ⊂ `Remnants`）。
#    · `Strike`/`Ambush` —— 卡面那处落在**卡名/天赋名**里（`Euphoric Strike` / `Empyric Ambush`）
#                      ⇒ 按词会把图标画进名字里。
#    · `Talent`     —— `SAU57` 例外。
#    · `Destroyer`/`Pray`/`Regiment`/`Ephemeral`/`Penitence` —— 词是**更长词的子串**
#                      （`Destroyers` / `Prays` / 中文「团」(⊂ 兵团/军团/…) / `临时路障` / `忏悔大师`）
#                      ⇒ 按词规则会画到词中间，只能按卡。
#    ⇒ 这几族一律走**逐卡**（判据更细，正是 `_FAMILY8` 那条路）。每一行的出处 = 转录表里
#      **双写**的那一行 + 卡图文件名。
#
# 🔴 **token 逐条挑过，不是一律用那个词本身**（这一步是必须的）：
#    `CardIcons.Rewrite` 是 `s.Replace(token, tag)`（**纯子串、全局**，且**先换长的**）⇒
#    同一个字段里**两条 token 互相包含**（`Armour` ⊂ `Armour 1.`）时，长那条插进去的标签
#    **里面就含短那条**，再换短那条 ⇒ **同一个位置画两枚**（静默）。
#    ⇒ 本表的 token **按卡按字段现挑**：从词本身开始**向右延长**（只能向右 —— 裸 token 那一支
#      把图标插在 token **起点**，向左延长会把图标插到前一个词前面），直到与本字段**所有**既有
#      token 都不构成包含关系为止（新加的也逐条并进去一起判）。挑不出来的**如实丢掉**、不硬塞：
#      ⛔ 本批丢了 **1 条**（`GSC38 Abominant` 的 `desc`：句首那条是 `Armour 1.`、第二处在词尾
#      写 `Armour 1`，右延长到尽头仍 ⊂ `Armour 1.`）—— 那张卡**只补了中文那半**。
_A1613_GAP = [
    # (卡 id, 要画的 token, 字段, sprite, 转录行号, 卡图文件名, 词名)
    # ---- `Ambush` → `ambush` ----
    ("GSC64", "Ambush", "desc", "ambush", 496, "Warpforge_68_Rusted-Vent.png", "Ambush"),
    ("GSC64", "伏击", "descZh", "ambush", 496, "Warpforge_68_Rusted-Vent.png", "Ambush"),
    ("GSC71", "Ambush", "desc", "ambush", 428, "Warpforge_06_Atalan-Leader.png", "Ambush"),
    ("GSC71", "伏击", "descZh", "ambush", 428, "Warpforge_06_Atalan-Leader.png", "Ambush"),
    # ---- `Armour` → `armour` ----
    ("AM17", "Armour", "desc", "armour", 134, "Warpforge_23_Commissar.png", "Armour"),
    ("AM17", "装甲", "descZh", "armour", 134, "Warpforge_23_Commissar.png", "Armour"),
    ("AM28", "Armour", "desc", "armour", 143, "Warpforge_32_Chimera.png", "Armour"),
    ("AM28", "装甲", "descZh", "armour", 143, "Warpforge_32_Chimera.png", "Armour"),
    ("AM35", "Armour,", "desc", "armour", 154, "Warpforge_40_Leman-Russ-Vanquisher.png", "Armour"),
    ("AM35", "装甲，", "descZh", "armour", 154, "Warpforge_40_Leman-Russ-Vanquisher.png", "Armour"),
    ("AM38", "Armour 1", "desc", "armour", 157, "Warpforge_43_Baneblade-Tank.png", "Armour"),
    ("AM38", "装甲 1", "descZh", "armour", 157, "Warpforge_43_Baneblade-Tank.png", "Armour"),
    ("AM50", "Armour", "desc", "armour", 168, "Warpforge_54_Thermal-Weaponry.png", "Armour"),
    ("AM50", "装甲", "descZh", "armour", 168, "Warpforge_54_Thermal-Weaponry.png", "Armour"),
    ("AM76", "Armour", "desc", "armour", 150, "Warpforge_12_Armoured-Lightning.png", "Armour"),
    ("AM76", "装甲", "descZh", "armour", 150, "Warpforge_12_Armoured-Lightning.png", "Armour"),
    ("ASH14", "Armour", "desc", "armour", 28, "Warpforge_18_Wraithblade.png", "Armour"),
    ("ASH14", "护甲", "descZh", "armour", 28, "Warpforge_18_Wraithblade.png", "Armour"),
    ("ASH24", "Armour", "desc", "armour", 39, "Warpforge_28_Vengeful-Wraithblade.png", "Armour"),
    ("ASH24", "护甲", "descZh", "armour", 39, "Warpforge_28_Vengeful-Wraithblade.png", "Armour"),
    ("ASH30", "Armour", "desc", "armour", 61, "Warpforge_36_Fire-Prism.png", "Armour"),
    ("ASH30", "护甲", "descZh", "armour", 61, "Warpforge_36_Fire-Prism.png", "Armour"),
    ("BL39", "Armour", "desc", "armour", 238, "Warpforge_50_Hideous-Mutation.png", "Armour"),
    ("BL39", "护甲", "descZh", "armour", 238, "Warpforge_50_Hideous-Mutation.png", "Armour"),
    ("BL47", "Armour", "desc", "armour", 243, "Warpforge_55_Disgustingly-Resilient.png", "Armour"),
    ("BL47", "护甲", "descZh", "armour", 243, "Warpforge_55_Disgustingly-Resilient.png", "Armour"),
    ("DA23", "Armour 2", "desc", "armour", 299, "Warpforge_26_Bladeguard-Veteran.png", "Armour"),
    ("DA23", "护甲 2", "descZh", "armour", 299, "Warpforge_26_Bladeguard-Veteran.png", "Armour"),
    ("DA53", "Armour", "desc", "armour", 334, "Warpforge_58_Steadfast-Warriors.png", "Armour"),
    ("DA53", "护甲", "descZh", "armour", 334, "Warpforge_58_Steadfast-Warriors.png", "Armour"),
    ("9e0eecf8cb9a8df49bf18cc5d9051109", "Armour", "desc", "armour", 275, "Warpforge_06_Master-of-the-Deathwing.png", "Armour"),
    ("9e0eecf8cb9a8df49bf18cc5d9051109", "护甲", "descZh", "armour", 275, "Warpforge_06_Master-of-the-Deathwing.png", "Armour"),
    ("EC2", "Armour", "desc", "armour", 366, "Warpforge_19_Blastmaster-Noise-Marine.png", "Armour"),
    ("EC2", "护甲", "descZh", "armour", 366, "Warpforge_19_Blastmaster-Noise-Marine.png", "Armour"),
    ("9215799995ff7a941b14737e27021032", "Armour", "desc", "armour", 415, "Warpforge_75_Shivversplint.png", "Armour"),
    ("9215799995ff7a941b14737e27021032", "护甲", "descZh", "armour", 415, "Warpforge_75_Shivversplint.png", "Armour"),
    ("GOF35", "Armour", "desc", "armour", 680, "Warpforge_44_Ork-Encampment.png", "Armour"),
    ("GOF35", "护甲", "descZh", "armour", 680, "Warpforge_44_Ork-Encampment.png", "Armour"),
    ("GOF95", "Armour", "desc", "armour", 644, "Warpforge_20_Kill-Rig.png", "Armour"),
    ("GOF95", "护甲", "descZh", "armour", 644, "Warpforge_20_Kill-Rig.png", "Armour"),
    ("GOF74", "Armour", "desc", "armour", 628, "Warpforge_10_Grizzled-Skarboy.png", "Armour"),
    ("GOF74", "护甲", "descZh", "armour", 628, "Warpforge_10_Grizzled-Skarboy.png", "Armour"),
    ("GOF70", "Armour", "desc", "armour", 604, "Warpforge_07A_Krumpaklaw.png", "Armour"),
    ("GOF70", "护甲", "descZh", "armour", 604, "Warpforge_07A_Krumpaklaw.png", "Armour"),
    ("8425b809e9e64e24f9035f709f5aaee4", "Armour", "desc", "armour", 587, "Warpforge_02B_Worst-Temper.png", "Armour"),
    ("8425b809e9e64e24f9035f709f5aaee4", "护甲", "descZh", "armour", 587, "Warpforge_02B_Worst-Temper.png", "Armour"),
    # 🔴 **2026-10-12（`A1624`）：`GSC38 Abominant` —— token 取 `Armour 1`（**不是** `Armour`）**。
    #    这张卡是 `A1613` 那一批**唯一挑不出 token** 的一张：字段里同时有段首的 `Armour 1.`（句首关键词
    #    那一支给的）和第二处（**文末**）的 `Armour 1` ⇒ `Armour 1` ⊂ `Armour 1.`、**右延长到文本尽头仍撞**。
    #    ⇒ 本批不再挑 token，改成**让段首那条让位**（见下面 `PREFIX_ABSORB`）：
    #      `Armour 1` 是**全局替换**，两处一起画（本字段这个写法恰好只出现这两处）。
    #    判据 = 卡面亲读 `d:/2/Warpforge部队卡片/Genestealer Cult/3部队/Warpforge_38_Abominant.png`
    #    （`〔盾〕Armour 1.` / `〔黄圆准星〕Concussive.` / `〔暗红圆〕Ambush: Gain 〔盾〕Armour 1`
    #     —— **文末那一枚真的印着**）+ 转录 `_还原效果文字.md:467`（`ArmourArmour 1. … Gain ArmourArmour 1`，双写 2 处）。
    ("GSC32", "Armour 1", "desc", "armour", 467, "Warpforge_38_Abominant.png", "Armour"),
    ("GSC43", "Armour", "desc", "armour", 480, "Warpforge_52_Enhanced-Resilience.png", "Armour"),
    ("GSC43", "护甲", "descZh", "armour", 480, "Warpforge_52_Enhanced-Resilience.png", "Armour"),
    ("GSC46", "Armour", "desc", "armour", 482, "Warpforge_54_Bore-Through.png", "Armour"),
    ("GSC46", "护甲", "descZh", "armour", 482, "Warpforge_54_Bore-Through.png", "Armour"),
    ("SAU55", "Armour", "desc", "armour", 566, "Warpforge_57_Immortal-Pride.png", "Armour"),
    ("SAU55", "装甲", "descZh", "armour", 566, "Warpforge_57_Immortal-Pride.png", "Armour"),
    ("461354d98aa5e3142afa4eb4922e5456", "Armour", "desc", "armour", 503, "Warpforge_02_Hardwired-Destruction.png", "Armour"),
    ("461354d98aa5e3142afa4eb4922e5456", "装甲", "descZh", "armour", 503, "Warpforge_02_Hardwired-Destruction.png", "Armour"),
    ("SOR7", "Armour", "desc", "armour", 718, "Warpforge_11_Crusader.png", "Armour"),
    ("SOR7", "护甲", "descZh", "armour", 718, "Warpforge_11_Crusader.png", "Armour"),
    ("SOR23", "Armour", "desc", "armour", 734, "Warpforge_27_Canoness.png", "Armour"),
    ("SOR23", "护甲", "descZh", "armour", 734, "Warpforge_27_Canoness.png", "Armour"),
    ("SOR55", "Armour", "desc", "armour", 759, "Warpforge_49_Trial-of-Suffering.png", "Armour"),
    ("SOR55", "护甲", "descZh", "armour", 759, "Warpforge_49_Trial-of-Suffering.png", "Armour"),
    ("SOR41", "Armour", "desc", "armour", 761, "Warpforge_51_Conviction-of-Faith.png", "Armour"),
    ("SOR41", "护甲", "descZh", "armour", 761, "Warpforge_51_Conviction-of-Faith.png", "Armour"),
    ("SOR37", "Armour 1", "desc", "armour", 751, "Warpforge_41_Triumph-of-St.-Katherine.png", "Armour"),
    ("SOR37", "护甲 1", "descZh", "armour", 751, "Warpforge_41_Triumph-of-St.-Katherine.png", "Armour"),
    ("SW43", "Armour", "desc", "armour", 826, "Warpforge_51_Anvil-of-Endurance.png", "Armour"),
    ("SW43", "护甲", "descZh", "armour", 826, "Warpforge_51_Anvil-of-Endurance.png", "Armour"),
    ("SW57", "Armour", "desc", "armour", 839, "Warpforge_64_Birth-of-a-Saga.png", "Armour"),
    ("SW57", "护甲", "descZh", "armour", 839, "Warpforge_64_Birth-of-a-Saga.png", "Armour"),
    ("TAU43", "Armour", "desc", "armour", 904, "Warpforge_50_For-The-Greater-Good.png", "Armour"),
    ("TAU43", "护甲", "descZh", "armour", 904, "Warpforge_50_For-The-Greater-Good.png", "Armour"),
    ("TAU46", "Armour", "desc", "armour", 906, "Warpforge_52_Sense-of-Stone.png", "Armour"),
    ("TAU46", "护甲", "descZh", "armour", 906, "Warpforge_52_Sense-of-Stone.png", "Armour"),
    ("TL20", "Armour", "desc", "armour", 943, "Warpforge_25_Carnifex.png", "Armour"),
    ("TL20", "护甲", "descZh", "armour", 943, "Warpforge_25_Carnifex.png", "Armour"),
    ("TL44", "Armour", "desc", "armour", 984, "Warpforge_55_Hardened-Biology.png", "Armour"),
    ("TL44", "护甲", "descZh", "armour", 984, "Warpforge_55_Hardened-Biology.png", "Armour"),
    ("dcbd28525888ce54aaa93a47ba4de1e6", "Armour", "desc", "armour", 1040, "Warpforge_2_Master-Tactician-2.png", "Armour"),
    ("dcbd28525888ce54aaa93a47ba4de1e6", "护甲", "descZh", "armour", 1040, "Warpforge_2_Master-Tactician-2.png", "Armour"),
    ("d94f91051ecfe014f97577ff573571aa", "Armour", "desc", "armour", 1039, "Warpforge_23_Spear-of-Macragge.png", "Armour"),
    ("d94f91051ecfe014f97577ff573571aa", "护甲", "descZh", "armour", 1039, "Warpforge_23_Spear-of-Macragge.png", "Armour"),
    ("UM25", "Armour", "desc", "armour", 1070, "Warpforge_39_Devastator-Centurion.png", "Armour"),
    ("UM25", "护甲", "descZh", "armour", 1070, "Warpforge_39_Devastator-Centurion.png", "Armour"),
    ("UM44", "Armour", "desc", "armour", 1114, "Warpforge_59_Armoured-Support.png", "Armour"),
    ("UM44", "护甲", "descZh", "armour", 1114, "Warpforge_59_Armoured-Support.png", "Armour"),
    ("ef596edae1828bd46bfc4348535a3acc", "Armour", "desc", "armour", 1022, "Warpforge_05_Supreme-Strategist.png", "Armour"),
    ("ef596edae1828bd46bfc4348535a3acc", "护甲", "descZh", "armour", 1022, "Warpforge_05_Supreme-Strategist.png", "Armour"),
    ("UM96", "Armour", "desc", "armour", 1049, "Warpforge_14_Chapter-Champion.png", "Armour"),
    ("UM96", "护甲 1 ", "descZh", "armour", 1049, "Warpforge_14_Chapter-Champion.png", "Armour"),
    ("UM103", "Armour", "desc", "armour", 1056, "Warpforge_18_Terminator.png", "Armour"),
    ("UM103", "护甲", "descZh", "armour", 1056, "Warpforge_18_Terminator.png", "Armour"),
    ("UM104", "Armour", "desc", "armour", 1060, "Warpforge_23_Bladeguard-Ancient.png", "Armour"),
    ("UM104", "护甲", "descZh", "armour", 1060, "Warpforge_23_Bladeguard-Ancient.png", "Armour"),
    ("4de4fcfb5d7e0554a88b9f419a2cd7c2", "Armour", "desc", "armour", 1023, "Warpforge_06B_Angels-Wrath.png", "Armour"),
    ("4de4fcfb5d7e0554a88b9f419a2cd7c2", "护甲", "descZh", "armour", 1023, "Warpforge_06B_Angels-Wrath.png", "Armour"),
    ("UM86", "Armour", "desc", "armour", 1120, "assault doctrine.png", "Armour"),
    ("UM86", "护甲", "descZh", "armour", 1120, "assault doctrine.png", "Armour"),
    ("UM54", "Armour", "desc", "armour", 1123, "cover.png", "Armour"),
    ("UM54", "护甲", "descZh", "armour", 1123, "cover.png", "Armour"),
    ("UM79", "Armour.", "desc", "armour", 1018, "Warpforge_03_Primaris-Eradicator-2.png", "Armour"),
    ("UM79", "护甲的", "descZh", "armour", 1018, "Warpforge_03_Primaris-Eradicator-2.png", "Armour"),
    ("UM68", "Armour", "desc", "armour", 1007, "UPDATED_primaris techmarine.png", "Armour"),
    ("UM68", "护甲", "descZh", "armour", 1007, "UPDATED_primaris techmarine.png", "Armour"),
    # ---- `Codex` → `codex` ----
    ("UM113", "Codex", "desc", "codex", 1100, "Warpforge_31_Dutys-End.png", "Codex"),
    ("UM113", "典籍", "descZh", "codex", 1100, "Warpforge_31_Dutys-End.png", "Codex"),
    ("40667e346ae58a84dbda1033dff70ccb", "Codex", "desc", "codex", 1014, "Warpforge_02_Author-of-the-Codex.png", "Codex"),
    ("40667e346ae58a84dbda1033dff70ccb", "典籍", "descZh", "codex", 1014, "Warpforge_02_Author-of-the-Codex.png", "Codex"),
    ("UM35", "Codex", "desc", "codex", 1122, "codex.png", "Codex"),
    ("UM35", "典籍", "descZh", "codex", 1122, "codex.png", "Codex"),
    ("UM73", "Codex", "desc", "codex", 1128, "oath of moment.png", "Codex"),
    ("UM73", "典籍", "descZh", "codex", 1128, "oath of moment.png", "Codex"),
    # ---- `Destroyer` → `frenzied` ----
    ("SAU44", "Destroyer", "desc", "frenzied", 560, "Warpforge_51_Disruption-Blades.png", "Destroyer"),
    ("SAU44", "毁灭者", "descZh", "frenzied", 560, "Warpforge_51_Disruption-Blades.png", "Destroyer"),
    ("SAU2", "Destroyer", "desc", "frenzied", 578, "Warpforge_8_Canoptek-Plasmacyte.png", "Destroyer"),
    ("SAU2", "毁灭者", "descZh", "frenzied", 578, "Warpforge_8_Canoptek-Plasmacyte.png", "Destroyer"),
    # ---- `Ephemeral` → `ephemeral` ----
    ("TL69", "Ephemeral", "desc", "ephemeral", 1002, "Warpforge_9_Neurotyrant.png", "Ephemeral"),
    ("TL69", "临时", "descZh", "ephemeral", 1002, "Warpforge_9_Neurotyrant.png", "Ephemeral"),
    # ---- `Penitence` → `rage` ----
    ("SOR57", "Penitence", "desc", "rage", 766, "Warpforge_56_Hallowed-Martyrs.png", "Penitence"),
    ("SOR57", "忏悔", "descZh", "rage", 766, "Warpforge_56_Hallowed-Martyrs.png", "Penitence"),
    # ---- `Pray` → `pray` ----
    ("SOR39", "Pray", "desc", "pray", 758, "Warpforge_48_Prayer.png", "Pray"),
    ("SOR39", "祈祷", "descZh", "pray", 758, "Warpforge_48_Prayer.png", "Pray"),
    # ---- `Regiment` → `regiment` ----
    ("AM48", "Regiment", "desc", "regiment", 166, "Warpforge_52_Regimental-Doctrine.png", "Regiment"),
    ("AM48", "团", "descZh", "regiment", 166, "Warpforge_52_Regimental-Doctrine.png", "Regiment"),
    # ---- `Remnant` → `remnant` ----
    ("SAU11", "Remnant", "desc", "remnant", 517, "Warpforge_16_Technomancer.png", "Remnant"),
    ("SAU11", "残骸", "descZh", "remnant", 517, "Warpforge_16_Technomancer.png", "Remnant"),
    ("SAU19", "Remnant", "desc", "remnant", 527, "Warpforge_25_Canoptek-Reanimator.png", "Remnant"),
    ("SAU19", "残骸", "descZh", "remnant", 527, "Warpforge_25_Canoptek-Reanimator.png", "Remnant"),
    ("27a4b748a7bf358499f038aa09895ead", "Remnant", "desc", "remnant", 549, "Warpforge_26_Reanimate.png", "Remnant"),
    ("27a4b748a7bf358499f038aa09895ead", "残骸", "descZh", "remnant", 549, "Warpforge_26_Reanimate.png", "Remnant"),
    ("SAU20", "Remnant", "desc", "remnant", 528, "Warpforge_27_Chronomancer.png", "Remnant"),
    ("SAU20", "残骸", "descZh", "remnant", 528, "Warpforge_27_Chronomancer.png", "Remnant"),
    ("SAU41", "Remnant", "desc", "remnant", 555, "Warpforge_46_Reanimation-Protocol.png", "Remnant"),
    ("SAU41", "残骸", "descZh", "remnant", 555, "Warpforge_46_Reanimation-Protocol.png", "Remnant"),
    ("SAU40", "Remnant", "desc", "remnant", 558, "Warpforge_49_Protection-Protocol.png", "Remnant"),
    ("SAU40", "残骸", "descZh", "remnant", 558, "Warpforge_49_Protection-Protocol.png", "Remnant"),
    ("SAU37", "Remnant", "desc", "remnant", 544, "Warpforge_5_Orikan-the-Diviner.png", "Remnant"),
    ("SAU37", "残骸", "descZh", "remnant", 544, "Warpforge_5_Orikan-the-Diviner.png", "Remnant"),
    ("SAU61", "Remnant", "desc", "remnant", 576, "Warpforge_67_Resurrection-Vault.png", "Remnant"),
    ("SAU61", "残骸", "descZh", "remnant", 576, "Warpforge_67_Resurrection-Vault.png", "Remnant"),
    # ---- `Strike` → `strike` ----
    ("GSC53", "Strike", "desc", "strike", 478, "Warpforge_50_Enhanced-Aggression.png", "Strike"),
    ("GSC53", "猛击", "descZh", "strike", 478, "Warpforge_50_Enhanced-Aggression.png", "Strike"),
    ("GSC51", "Strike", "desc", "strike", 484, "Warpforge_56_Living-Icon.png", "Strike"),
    ("GSC51", "猛击", "descZh", "strike", 484, "Warpforge_56_Living-Icon.png", "Strike"),
    ("UM73", "Strike", "desc", "strike", 1128, "oath of moment.png", "Strike"),
    ("UM73", "猛击", "descZh", "strike", 1128, "oath of moment.png", "Strike"),
    # ---- `Talent` → `talent` ----
    ("UM12", "Talent", "desc", "talent", 1080, "chronus.png", "Talent"),

    # ---- 2026-10-12 追加：**同卡同字段的第二处**（段首那枚已由句首关键词画，
    #      守卫③ 是**整字段**级 ⇒ 规则不补第二处；这里逐卡给**带上下文**的 token，
    #      既避开与段首 token 互相包含、又能对上第二处。见报告「残缺口」那一节）----
    ("AM22", "Shield to", "desc", "shield", 140, "Warpforge_29_Primaris-Psyker.png", "Shield"),
    ("SW36", "Ferocity,", "desc", "ferocity", 817, "Warpforge_42_Bjorn-the-Fell-Handed.png", "Ferocity"),
    ("SW36", "狂暴时", "descZh", "ferocity", 817, "Warpforge_42_Bjorn-the-Fell-Handed.png", "Ferocity"),
    ("UM107", "Sentry 2", "desc", "sentry", 1063, "Warpforge_26_Whirlwind.png", "Sentry"),
    ("UM107", "哨戒 2", "descZh", "sentry", 1063, "Warpforge_26_Whirlwind.png", "Sentry"),
    ("TL18", "Synapse,", "desc", "synapse", 947, "Warpforge_29_Zoanthrope.png", "Synapse"),
    ("TL18", "突触时", "descZh", "synapse", 947, "Warpforge_29_Zoanthrope.png", "Synapse"),
    ("ASH79", "Stealth,", "desc", "stealth", 98, "Zrzut ekranu 2026-04-16 o 18.55.24.png", "Stealth"),
    ("ASH79", "潜行时", "descZh", "stealth", 98, "Zrzut ekranu 2026-04-16 o 18.55.24.png", "Stealth"),
    ("TAU12", "Stealth to", "desc", "stealth", 859, "Warpforge_16_Darkstrider.png", "Stealth"),
    # ---- 🆕 2026-10-12（`A1623`）：`Dark Pact` / `黑暗契约`（**只有中文这一格**）----
    # 🔴 **为什么这一格不能交给按词规则**：`BL74` 的中文是**我们自己译的**，把那个词写了**两遍**
    #    （`当我方部队获得黑暗契约时，此部队同样获得该黑暗契约` —— 第二处是英文 `it` 的译法），
    #    而卡面**只印一枚**徽记（转录 `:181` 双写 1 处、成品卡图
    #    `Chaos/3部队/Warpforge_10_Accursed-Helbrute.png` 亲读 1 枚）。
    #    按词规则是**整字段**的 ⇒ 它会在这儿画两枚（多一枚 = 卡面没有的东西 = 本工程红线）。
    #    ⇒ 按卡给一条**更长**的 token（`黑暗契约时`）—— 裸 token 那一支把图标插在 token **起点**，
    #    所以图标正好落在「黑暗契约」前面、与卡面那一格一致；`Rewrite` 全局替换，这一串只出现一次。
    #    ⚠️ 英文那一半**不用列**（`desc` 里那个词只出现一次，按词规则直接画、位置也对）。
    #    ⚠️ 有了这一条，`descZh` 的守卫②（本卡已有**同 sprite** 的逐卡裸 token）就会让按词规则
    #    整字段跳过 ⇒ **不会**出现「两枚」。
    ("BL72", "黑暗契约时", "descZh", "markOfChaos", 181, "Warpforge_10_Accursed-Helbrute.png", "Dark Pact"),
]
_A1613_MARK = {
    "armour": "「盾牌」", "codex": "「典籍」", "remnant": "「残骸」", "strike": "「猛击」",
    "ambush": "「伏击」", "talent": "「卷轴」", "frenzied": "「毁灭者」（图集里没有 `destroyer.png`）",
    "pray": "「祈祷」", "regiment": "「团」", "ephemeral": "「临时」", "rage": "「忏悔」（图集里没有 `penitence.png`）",
    # 🆕 2026-10-12（`A1623`）：`Dark Pact` 那枚 —— 形状**逐张开图核过**（6 张卡的徽记裁下来并排看 = 同一张图），
    #    所以照实写形状，别退回中性的「图标」。
    "markOfChaos": "「暗红盘 + 白八芒星」（= 黑暗契约徽记；五张 `markOf*` 渲染上完全一样）",
}
for _g_cid, _g_tok, _g_field, _g_sprite, _g_line, _g_png, _g_word in _A1613_GAP:
    # ⚠️ 形状名**只在对得上时**才写形状（`_A1613_MARK`）；没核过的一律退回中性的「图标」——
    #    凭印象写形状 = 把猜测写成判据（本工程红线，见 `资料/卡表核对_卡图提取/_还原效果文字.md` 头部的旧教训）。
    _g_why = (f"卡面 `{_g_word}` **前面**那枚{_A1613_MARK.get(_g_sprite, '图标')}（= "
              f"`Resources/Art/traits/{_g_sprite}.png`）—— 逐张判据："
              f"`资料/卡表核对_卡图提取/_还原效果文字.md:{_g_line}`（`{_g_png}`）。")
    BARE_TOKEN_BY_CARD[(_g_cid, _g_tok)] = (_g_sprite, _g_why)


# ── 🆕 2026-10-12（`A1624`）：**「句首关键词 token 让位给同词的裸 token」**表 ────────────────
# 🔴 **它解决什么**：卡面**句尾那一段不印句号**（印刷约定，`A1476` 记过），而**段首**那个词一定带收尾符
#    （`Armour 1.` / `Stealth.`）⇒ 同一张卡里会出现「**长的段首 token**（`Armour 1.`）⊂/⊃
#    **短的裸 token**（`Armour 1`）覆盖不到的第二处」这个形状。
#    而 `CardIcons.Rewrite` 是 `s.Replace(token, tag)`：**长的先换**（长度降序）⇒ 长的那条先占位、
#    再把**词留着** ⇒ 短的裸 token 要么被生成器的「同字段 token 互相包含」自检拦下来（rc=1）、
#    要么被**守卫③**（整字段让位）整条跳过 ⇒ **第二处静默不画**。
#
# **改法**：不再从 token 身上找活路（`A1613` 那一批已经试过「向右延长」—— `GSC38` 第二处**就在文末**、
#    右延长到尽头仍撞），而是**让段首那条让位**：把 `(卡, 字段, 段首 token)` 从 `kws` 里**摘掉**，
#    由**同词、同 sprite 的裸 token** 去画 —— 裸 token 是**全局替换**，那一处**本来就含在它里面**
#    （段首 token = 裸 token + 纯标点尾巴）⇒ **一个 token 把两处一起画**、位置也对（图标插在词前）。
#
# 🔴 **每一条都必须有卡面判据**（`资料/卡表核对_卡图提取/_还原效果文字.md` 的**双写** = 图标 + 词），
#    因为这条形状里**藏着一个真陷阱**：同词出现两次、而**第二处不是关键词用**（是**卡名 / 天赋名 /
#    伴生单位名**）时，字符层面**分不出来**（`Gain Flank and Shield.` 与 `Gain Sentry 2` 同形）。
#    ⇒ ⛔ **不能写成「按词」的通用规则**（那会给卡面**加**上原版没有的图标 = 工程红线）。
#    实测的**反例三张**（本表**故意不列**，如实留在账上）：
#      · `TAU33 Stealth Shas'ui` —— 转录 `:878` = `StealthStealth.CompanionCompanion 2: Stealth Drone`
#        ⇒ 第二处在**伴生单位名**里、**没有双写** ⇒ 不画；
#      · `TAU42 Riptide Battlesuit` —— 转录 `:897` = `Shield Shield. Long Range Long Range. Talent Talent: Shield Generator`
#        ⇒ 第二颗 `Shield` 在**天赋名** `Shield Generator` 里、没有双写 ⇒ 不画；
#      · `TL37 Hive Tyrant` —— 转录 `:957` = `SynapseSynapse. TalentTalent: Synaptic Imperative`
#        ⇒ 中文那半 `突触律令` 同理（`突触` ⊂ 天赋名）⇒ 不画。
#    ⚠️ 这三张**今天是对的**（计划表不画那处）⇒ **本表不列它们**，列了就是「凭空多画一枚」。
#
# 列 = (卡 id, 字段, **让位**的段首 token, **接手**的裸 token, 转录行号, 卡图文件名)
# ⚠️ 本表**同时**是「让位」的判据载体与查询表 ⇒ 底下那个 `PREFIX_ABSORB` 由它生成（**一处**，别抄两份）。
_PREFIX_ABSORB_ROWS = [
    # ---- 2026-10-12（`A1624`）：`A1613` 收口时**只剩的两张**（+ 本批同族新查出的）----
    ("GSC32", "desc", "Armour 1.", "Armour 1", 467, "Warpforge_38_Abominant.png"),
    ("GOF62", "desc", "Stealth.", "Stealth", 665, "Warpforge_34_Kommando.png"),
    # 🔴 **2026-10-12 现核（`A1621` 那一笔的裁定）**：`UM78` / `TAU14` / `TAU20` 的 **`desc` 段首词
    #    【不进 `desc`】** —— `C2` 当天裁定：段首那一段由**渲染侧**印（`CardText.KeywordSegment`，
    #    `A1345` 的 `DeclaredInBody` 已能印、且带图标按 `keywords` 数组序；见
    #    `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-12_A1621_A1625_段首词与DA37任务点`）。
    #    ⇒ 这三张的 **`desc` 字段里今天没有段首 token 可让位**，本表**不列它们**
    #    （第一版本写进去过 ⇒ 本表自己的**死条目自检当场报出来**，那条自检就是为这种情形设的）。
    #    ⚠️ 若哪天有人照 `A1475③` 把段首词补回 `desc`，这里要**一起**补三行
    #    （`UM78 [desc] Armour 1.→Armour` / `TAU14 [desc] Stealth.→Stealth` / `TAU20 [desc] Long Range.→Long Range`），
    #    否则 `UM78` 会当场报「互相包含」（rc=1、不落盘），另两张会**静默丢掉句中那一枚**
    #    （守卫③ 整字段让位）—— 那条回归由 `FACE_ICON_COUNT` 那三行兜住。
    # ---- 本批**同族新查出**的三张（`A1613` 的残缺口度量没覆盖到它们：度量是「按词」的，
    #      而这几张的第二处**被守卫③整字段挡掉**、计数时看不出来）----
    # `SW37 Wulfen Dreadnought`：转录 `:819` = `Armour Armour 1. Pack Pack. Rally Rally: Give +2 Melee
    #   to all friendly units with Pack Pack` ⇒ **句尾那个 `Pack` 也印着徽记**（双写）。
    #   ⚠️ **只有中文那半要让位**：英文 `desc` 句尾写的是 `with Pack.`（**带句号**）⇒ 段首那条 `Pack.`
    #      是**全局替换**、两处都命中 ⇒ 英文那半**本来就画着**（本批现核：`count('Pack')==count('Pack.')==2`
    #      ⇒ 不在「短 token 覆盖更多」那个判据里）；中文 `descZh` 句尾是 `带兽群的`（**没有 `。`**）⇒ 真缺口。
    ("SW37", "descZh", "兽群。", "兽群", 819, "Warpforge_44_Wulfen-Dreadnought.png"),
    # `TAU14 Stealth Drone`：转录 `:857` = `FlyingFlying.StealthStealth. At the end of your turn,
    #   give StealthStealth to an adjacent troop` ⇒ 句尾 `give Stealth to` 那处**双写**。
    #   ⚠️ **只有中文那半需要让位**：英文 `desc` 里段首词**不进 `desc`**（同上那条裁定）⇒ 句中那枚
    #      由按词规则**直接画**（没有段首 token 挡它）；中文 `descZh` 里段首 `潜行。` 是**我们自己的
    #      中文字**（`_2026-09-15_PnP逐张并排验收_漏印关键词段` 那批补的，卡面无中文可核）⇒ 它挡着。
    ("TAU10", "descZh", "潜行。", "潜行", 857, "Warpforge_14_Stealth-Drone.png"),
    # `TAU20 Sniper Drone`：转录 `:864` = `FlyingFlying.Long RangeLong Range. Gains Long RangeLong Range
    #   when an enemy gains MarkerlightMarkerlight` ⇒ 句中的 `Long Range` **双写**（同 `TAU14` 那条的形状）。
    ("TAU15", "descZh", "远射。", "远射", 864, "Warpforge_20_Sniper-Drone.png"),
]
# 查询表：`(卡 id, 字段, 让位的段首 token)` → 接手的裸 token
PREFIX_ABSORB = {(_c, _f, _t): _k for _c, _f, _t, _k, _ln, _png in _PREFIX_ABSORB_ROWS}

# 🔴 **本表的两条自检**（缺了任何一条 = 静默错，见 `main()` 里那 20 行）：
#   ① **每条都要真的被用到** —— `(卡, 字段, 段首 token)` 必须**真的被 `kws` 产出过**
#      （卡表一改，这条就成了「看起来在干活、实际什么都没做」的死条目 ⇒ 报出来、rc=1）；
#   ② **接手的裸 token 必须真的落进这个字段的计划** —— 否则摘掉段首那条 = **那一枚图标整枚消失**
#      （比改前更糟：改前至少段首那一枚是画着的）⇒ 也要 rc=1。
#   ③ 另有 `FACE_ICON_COUNT`（下面）把「这个字段该画几枚」**按卡面**钉住 —— 它才是**能红**的那条判据。
PREFIX_ABSORB_WHY = {
    ("GSC32", "desc"): "第二处是**文末**的裸 `Armour 1`（本字段**只此两处**，`Armour 1` 全局替换两处一起画）",
    ("GOF62", "desc"): "第二处是**文末**的裸 `Stealth`（`Stealth` 按词规则给的，本字段两处都画）",
    ("SW37", "descZh"): "第二处是句尾 `带兽群` 那一处（中文 `兽群。` 那条**没有**全局命中的运气 —— 尾字是 `的`）",
    ("TAU10", "descZh"): "第二处是句尾 `给予 1 个相邻部队潜行`；`潜行` 是**按词**规则给的",
    ("TAU15", "descZh"): "第二处是句尾 `…时获得远射`；`远射` 是**按词**规则给的",
}

# ── 🆕 2026-10-12（`A1624` / `A1622`）：**卡面枚数抽样自检** ─────────────────────────────
# 🔴 **期望值来自成品卡图**（本批作者逐张开图 + 转录表的**双写**），**不是**从我们自己的实现里算出来的
#    —— 否则就是拿我们的实现证明我们的实现（本工程反复强调的那条）。
# **量什么**：这个字段的**计划表会画几枚** = `Σ 每条 token 在原文里的出现次数`（`Rewrite` 是全局替换，
#    每条覆盖本字段**全部**出现；token 之间**互不包含**由上面那道 `conflicts` 自检保证 ⇒ 不会重复计数）。
#    ⚠️ 它**不含** `CardText.KeywordSegment` 补的那几枚（段首声明行）—— 那几枚单独在判据里写清。
# **为什么要它**：上面那些坑（长 token 挡住短 token、守卫③、段首让位）**全都不在计划表上现形**
#    （计划表看起来永远「有这一条」）⇒ 只有「**画出来几枚**」这个数能把它们抓出来。
#    实测：本批**改前** `GSC38 desc = 3 ≠ 4`、`GOF34 desc = 2 ≠ 3`（其余行当时就对）⇒ **这条判据真会红**。
FACE_ICON_COUNT = [
    # (卡 id, 字段, **本字段（这份正文）**应画枚数, 判据 / 那几枚由谁画)
    ("GSC32", "desc", 4, "卡面 `Genestealer Cult/3部队/Warpforge_38_Abominant.png` 亲读 **4 枚，全在正文里**"
                        "（转录 `:467` 双写 4 处）—— 段首那三项 `Armour 1.`/`Concussive.`/`Ambush:` 就在 `desc` 里"
                        "⇒ `KeywordSegment` 一枚都不补"),
    ("GSC32", "descZh", 4, "中文卡面不存在 ⇒ 判据 = 与英文那 4 枚**逐处对应**（`护甲 1。`两条**都**在正文里）"),
    ("GOF62", "desc", 3, "卡面 `Orks/3部队/Warpforge_34_Kommando.png` 亲读 3 枚（转录 `:665` 双写 3 处）"),
    ("GOF62", "descZh", 3, "中文同英文逐处对应（`潜行。`+句尾 `潜行。`）"),
    ("UM96", "desc", 5, "卡面 `Ultramarines/3部队/Warpforge_14_Chapter-Champion.png` 亲读 **6** 枚 = 正文 5 枚"
                        "（`Codex:`/`Oath 2:`/`+2〔拳〕`/`Armour 1`/`Vanguard`）+ **段首 `Armour 1.` 1 枚**"
                        "（由 `KeywordSegment` 补 —— `desc` 里**不补**那个词，见 `cardface_fixes.json` 的"
                        "`_2026-10-12_A1621_A1625_段首词与DA37任务点`）"),
    ("UM96", "descZh", 6, "中文 6 枚**全在正文里**（段首 `护甲 1。` 是我们自己的中文字，`KeywordSegment` 不再补）"),
    ("SW37", "desc", 5, "卡面 `Space Wolves/3部队/Warpforge_44_Wulfen-Dreadnought.png`（转录 `:819` 双写 5 处，"
                        "含句尾 `with Pack` —— 那一枚是本批补的）"),
    ("SW37", "descZh", 5, "中文同英文逐处对应"),
    ("TAU10", "desc", 1, "卡面 `Tau/3部队/Warpforge_14_Stealth-Drone.png`（转录 `:857` 双写 3 处）= 正文 1 枚"
                         "（句尾 `give Stealth to`）+ **段首 `Flying.` `Stealth.` 2 枚**（`KeywordSegment` 补）"),
    ("TAU10", "descZh", 3, "中文 3 枚全在正文里（段首 `飞行。潜行。` 是我们自己补的中文字）"),
    ("TAU15", "desc", 2, "卡面 `Tau/3部队/Warpforge_20_Sniper-Drone.png`（转录 `:864` 双写 4 处）= 正文 2 枚"
                         "（`Long Range`/`Markerlight`）+ **段首 `Flying.` `Long Range.` 2 枚**（`KeywordSegment` 补）"),
    ("TAU15", "descZh", 4, "中文 4 枚全在正文里（`飞行。远射。`+句中 `远射`+`标记光`）"),
    ("SOR71", "desc", 6, "卡面 `Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` 亲读 **7** 枚 = 正文 6 枚"
                         "（`6☀`/`Gain Flank and Shield`/`Rally:`/尾句两枚 `☀`）+ **段首 `Flying.` 1 枚**"
                         "（由 `KeywordSegment` 补）"),
    ("SOR71", "descZh", 6, "🔴 **2026-10-12（第二十一会话 · 写手 `N2`）期望值 5 → 6**：中文那半现在与英文那 6 枚"
                          "**逐处对应**（`6 ☀：`/`侧翼`/`护盾`/`集结：`/句中的 `6 ☀`/尾句 `1 ☀`）。"
                          "⚠️ **改前的 5 是一条真缺陷、不是口径差**：中间那句我们写的是「**少于 6 点**」，"
                          "英文同处是 `6 ☀`、卡面是 `6〔金太阳〕`"
                          "（`Sorotitas/3部队/Warpforge_08_Adelaide-the-Serene.png` 亲读）⇒ 已按卡面补成 `6 ☀`"
                          "（改在 `数据/游戏数据/cardface_fixes.json` 的 `descZh` 列，见 "
                          "`_2026-10-12_A1633_A1634_同名卡裸键与任务点`）。"
                          "段首 `飞行。` 另由 `KeywordSegment` 补 ⇒ 与卡面 7 枚**只差那一枚段首的**。"
                          "⚠️ 另：`KeywordSegment` 对这一张还会把 `侧翼。`/`护盾。` 也提到段首 = 原版没有 ⇒ "
                          "见 `Core/CardText.cs` 里 `SOR72` 那条残差注释；**与 `descZh` 文本侧这一枚是两笔**"),
    ("AM50", "desc", 2, "卡面 `Astra Militarum/4计策/Warpforge_54_Thermal-Weaponry.png` 亲读 2 枚（转录 `:168`，"
                        "**无声明行** ⇒ `keywords` 为空、`KeywordSegment` 一枚都不补）"),
    ("AM50", "descZh", 2, "中文同英文逐处对应"),
    # 🆕 2026-10-12（`A1683`）：这一行**改前是 4 ≠ 3**（多出来的那一枚是 `faith`）。
    ("851f73daf747eb642934a16429b391db", "desc", 3, "卡面 `Aeldari/2天赋/Warpforge_02_Storm-of-Silence.png` 亲读 **4** 枚 = 正文 3 枚"
                         "（`+2`〔粉拳〕/ `〔绿圈·1〕` 灵魂石 / `Stun` 前那枚〔橙金螺旋〕）+ **段首 `Ephemeral` 1 枚**"
                         "（由 `KeywordSegment` 补 ⇒ `desc` 正文里不补那个词）。🔴 **正文这 3 枚里【没有金太阳】"
                         "—— `ASH75` 是 SaimHann，跟修女会的信仰无关**"),
    ("851f73daf747eb642934a16429b391db", "descZh", 3, "中文同英文逐处对应（`[攻击]`/`①`/`眩晕`）⇒ **3 枚、一枚信仰都不许有**。"
                           "⚠️ **改前这条是 4 ≠ 3**（多出 `[能量]`→`faith`，见 `ASH75_FAITH_TOKENS` 那一节）"
                           "—— 本表就是抓住它的那条判据（`A1683`）"),
    # 🆕 2026-10-13（`A1707`）：这一行**改前是 2 ≠ 1**（多出来的那一枚是天赋名里的 `Shield`）。
    ("TAU36", "desc", 1, "卡面 `Tau/3部队/Warpforge_42_Riptide-Battlesuit.png` 亲读 **3** 枚 = 段首 "
                         "`〔盾〕Shield.` + `〔准星〕Long Range.`（2 枚，由 `KeywordSegment` 补）"
                         "+ 正文 `〔卷轴〕Talent:` 1 枚 ⇒ **正文只有 1 枚**"
                         "（`Shield Generator` 那里**没有盾徽**，裁图 `_tmp_view/a1703_crops/Z2_TAU42.png`）"),
    ("TAU36", "descZh", 3, "中文 3 枚**全在正文里**（`护盾。远射。天赋：护盾发生器` —— 段首那两枚是我们自己的"
                           "中文字、`KeywordSegment` 不再补），与英文那 1 枚逐处对应"
                           "（`护盾发生器` 里那个「护盾」**不画**：卡面 `Shield Generator` 处没印）"),
    # 🆕 2026-10-13（`A1713`）：这一行**改前是 1 ≠ 2**（少掉的那一枚是 `Blood Thirst`）。
    ("GOF91", "desc", 2, "卡面 `Orks/3部队/Warpforge_16_Beastboss-on-Squigosaur.png` 亲读 **3** 枚 = "
                         "段首 `〔狼头〕Stomp.` 1 枚（由 `KeywordSegment` 补，`Stomp` 在 `keywords` 数组里）"
                         "+ 正文 2 枚（`〔骷髅+准星〕Slay:` / `〔獠牙圆徽〕Blood Thirst`，裁图 "
                         "`_tmp_view/a1703_crops/MINE_GOF90_bt.png`）"),
    ("GOF91", "descZh", 2, "中文同英文逐处对应（`斩杀：本回合获得嗜血` 两枚，段首 `践踏。` 另由 `KeywordSegment` 补）"),
]


# ── 🆕 2026-10-12（`A1605`）：**信仰前缀写法统一**（一律 `☀`）──────────────────────────────
# 🔴 **卡面上「信仰」那一枚永远是同一张金太阳**（修女会）。而池里的**数据写法**曾经有 7 种：
#    `☀` · `[faith]` · `[Faith]` · `[Faith Icon]` · `[Icon]` · `[icon]` · `[Energy]` / `[能量]` ——
#    后六种**不是原版数据**，是**卡图 OCR 认不出那枚图**时按词形乱起的名字
#    （同 `_2026-09-16_two_bare_icon_tokens` 那一族；`资料/卡面图标_现状与缺口.md:257` 对它判过「数据 token 错」）。
# ⇒ 2026-10-12 统一成 **`☀`**。判据三条（写全在 `数据/游戏数据/cardface_fixes.json` 的
#    `_2026-10-12_A1605_信仰前缀统一` 里）：① **成品卡图卡面亲读**（本笔读了 5 张，见下表逐条出处；
#    另两张由 `SYMBOLS[SUN]` 那条证据承接 —— 它已经「逐张核过 7 张」，点名的正是这几张）；
#    ② `资料/卡表核对_卡图提取/_还原效果文字.md`（29 个子代理逐张看卡图抄的）那 40 多行一律写 `Faith`；
#    ③ **`descZh` 那一半本来就全写 `☀`**（玩家看到的就是它，`BattleDriver.FaceTextFull` 优先中文）。
#    ⚠️ **引擎中性**（改前实测）：`EffectText` 那条付费前缀正则**本来就同时认 `☀` 与那几个词**
#       （`EffectText.cs:6613` 的带冒号那条 · `:6618` 的不带冒号那条）⇒ `PayloadOp.Cost`/`CostKind`
#       逐字节相同 —— 本笔**只换 token**，标点/空格/数字一个字没动。
#
# 🔴 **下面两道就是那条决定的【机械保证】**（谁把旧写法写回来 ⇒ 当场 rc 非 0、两份 json 都不落盘）。
LEGACY_FAITH_PREFIX = re.compile(
    r"[0-9]\s*\[(?:faith|Faith|Faith Icon|Icon|icon|Energy|能量)\]")
# ⚠️ **别把判据写成「池里不许出现 `[Icon]`」** —— 池里另有 `[Codex icon]` / `[eye icon]` 两个**别的**图标，
#    写宽松了会误报。这里要的是**「数字 + 那串方括号」这个前缀形状**（`[icon]` 那个是子串关系，
#    只有整串 + 前面的数字一起匹配才准）。
# ⚠️ **`[能量]` 照样进这条正则** —— 它在 `ASH75` 上写的是**灵魂石**（`①[能量]：`，英文 `1 [Spirit Stone]:`），
#    但那个 `①`(U+2460) **不是 `[0-9]`**（`No` 类不是 `Nd`，实测 `re.match(r"\d", "①")` 为 `None`）
#    ⇒ 本条**不会**把它误判。`ASH75` 那笔是**另一笔账**（如实报在 `A1605` 的报告 §五），本笔**没动它**。
#    🔴 **改前实测**：这条正则在**统一之前**的数据上命中 **13 处**（正是本笔要改的那 13 个字段）
#       ⇒ **它今天绿、是改出来的，不是恒绿**。

# **统一过的 13 个（卡, 字段）** —— 每个都必须**仍然画 `☀`**（`sprite == "faith"`）且枚数不变。
# 列 = (卡 id, 字段, 本字段 `☀` 的**出现次数**, 卡面判据 / 那几枚是什么)
# ⚠️ 枚数 = `text.count("☀")`（`Rewrite` 是**全局**替换 ⇒ 一个 token 画 N 枚）——
#    本笔**改前/改后逐字段相同**（改前每个字段恰好 1 枚，只是那一枚由旧写法画）。
FAITH_PREFIX_UNIFIED = [
    ("65cfec7d2bc719040b3b4b88622931b2",  "desc",   1, "卡面 `Sorotitas/2天赋/Warpforge_04_Lead-the-Righteous.png` 亲读 `8 〔金太阳〕: Deploy an additional Battle Sister`（原 `[Faith]`）"),
    ("SOR4",  "desc",   1, "卡面 `Sorotitas/3部队/Warpforge_07_Sister-Novitiate.png`（原 `[Icon]`）"),
    ("SOR9", "desc",   1, "卡面 `Sorotitas/3部队/Warpforge_12_Preacher.png`（原 `[faith]`；`SYMBOLS[SUN]` 那条证据点名了它）"),
    ("SOR38", "desc",   1, "卡面 `Sorotitas/3部队/Warpforge_40_Paragon-Warsuit.png` 亲读 `6 〔金太阳〕 : Gain 〔先锋〕Vanguard`（原 `[Faith Icon]`）"),
    ("SOR36", "desc",   1, "卡面 `Sorotitas/3部队/Warpforge_42_Exorcist.png`（原 `[faith]`）"),
    ("SOR53", "desc",   1, "卡面 `Sorotitas/4计策/Warpforge_50_Blade-of-Faith.png`（原 `[Icon]`）"),
    ("SOR73", "desc",   1, "卡面 `Sorotitas/4计策/Warpforge_10_Sacred-Rose.png` 亲读 `6 〔金太阳〕 Give them 〔侧翼〕Flank`（**卡面本来就没有冒号**，原 `[icon]`）"),
    ("SOR47", "desc",   1, "卡面 `Sorotitas/4计策/Warpforge_52_Miraculous-Feat.png` 亲读 `6 〔金太阳〕 : Extend effect until your next turn`（原 `[Energy]` —— **它就是信仰、不是能量**）"),
    ("SOR54", "desc",   1, "卡面 `Sorotitas/4计策/Warpforge_55_Fiery-Conviction.png`（原 `[Energy]`；还原表 `_还原效果文字.md:765` = `4 Faith:`）"),
    ("e01f4d54e8a474d96b5a1167cc28eebf", "desc",   1, "卡面 `Sorotitas/2天赋/Warpforge_02_Daemonbreaker.png` 亲读 `8 〔金太阳〕 : Give it permanently`（原 `[Energy]`）"),
    ("SOR47", "descZh", 1, "中文同英文逐处对应（原来写的是「能量」，本次只改成 `☀`、词与标点一字未动）"),
    ("SOR54", "descZh", 1, "中文同英文逐处对应（原写 `[能量]`）"),
    ("e01f4d54e8a474d96b5a1167cc28eebf", "descZh", 1, "中文同英文逐处对应（原写 `[能量]`）"),
]


# ── 🆕 2026-10-12（`A1683`）：`ASH75` 的中文把「灵魂石」画成了「信仰」────────────────────────
# 🔴 **缺陷**（`W9` 做 `A1605` 时顺手查出、本笔修）：`ASH75 Storm of Silence`（**SaimHann**）的
#    `descZh` 原来写 `你的战将本回合获得 +2 [攻击]。①[能量]：眩晕 1 个随机敌方部队` ——
#    `①` → `SpiritStone_1`（**对**）而 `[能量]` → `faith`（**错**，`TOKEN_BY_RULE` 那条）⇒
#    **同一处画两枚、其中一枚是错的**（那 `[能量]` 是 `A1605` 统一信仰写法时**故意留下的**
#    —— 它当时判「这是另一笔账」，见 `数据/游戏数据/cardface_fixes.json` 的
#    `_2026-10-12_A1605_信仰前缀统一` 末句）。
#    ⚠️ **英文那半一直是对的**：`desc` = `… 1 [Spirit Stone]: Stun a random enemy troop`
#    （一条 `[Spirit Stone]`、**一个信仰记号都没有**）⇒ 缺陷**只在中文这一半**。
#
# **判据 = 成品卡图亲读**（铁律 7，第一权威）：
#    `d:/2/Warpforge部队卡片/Aeldari/2天赋/Warpforge_02_Storm-of-Silence.png` —— 第 2 行印
#    `〔绿圈·数字 1〕〔橙金螺旋〕Stun a random enemy troop`，**没有金太阳**；
#    第 1 行 `+2〔粉拳〕`、段首 `〔Ephemeral〕`。⇒ 正文共 3 枚，**信仰一枚都不该有**。
#    （旁证：`ASH75` 的 `faction` = `SaimHann`，修女会那枚金太阳与它无关。）
#
# **修法**：改 `数据/游戏数据/cardface_fixes.json` 的 `descZh` 列（删掉 `[能量]`，
#    其余一字未动）→ 重跑 `工具/gen_cards_engine.py` → 重跑本脚本 `--write`。
#    ⚠️ **别改 `数据/卡牌翻译/zh_cards.json`**（那份自称「不要手改」，且应用顺序在 `descZh` **之前**）。
#
# 🔴 **`LEGACY_FAITH_PREFIX` 抓不到这一处**（`W9` 已声明）：它的判据是「**数字** + 信仰词」这个前缀形状，
#    而这里前面那个是 `①`(U+2460) —— `re.match(r"\d", "①")` 为 `None` ⇒ **那条正则在改前也是绿的**。
#    ⇒ 本节的判据**另起**（形状不同：这里要的是「**这个字段里一枚信仰都不许有**」）。
#
# 🔴 **下面两道就是这个决定的机械保证**（谁把信仰记号写回 `ASH75` 的中文 ⇒ 当场 rc 非 0、两份 json 都不落盘）：
#   ① `ASH75_FAITH_TOKENS` 一个都不许出现在 `descZh` 里，**且**计划表里不许有 `faith` 条目；
#   ② `①`→`SpiritStone_1` **必须还在**（判「不许画信仰」时别把灵魂石一起删了 —— 那是**另一种**静默错）。
#      ⚠️ 反向的错（**删过头**）由 `FACE_ICON_COUNT` 的 `("851f73daf747eb642934a16429b391db","descZh",3,…)` 那一行兜住。
ASH75_FAITH_TOKENS = ("[能量]", "[Energy]", "[faith]", "[Faith]", "[Faith Icon]",
                      "[Icon]", "[icon]", "☀")


# ── 🆕 2026-10-12（`A1623`）：`Dark Pact` 一族（`BARE_TOKEN_BY_RULE` 末尾那两条）─────────────
# 🔴 **先说结论：那张「哪一位邪神用哪一枚 sprite」的映射表【不用做】—— 五张 `markOf*` 渲染上完全一样。**
#    原版图集本身就那么五份近似图（三张**逐字节相同**、另两张只差 15 / 6 个 `alpha=0` 像素的 RGB），
#    `m_Rect` 各不相同（`252,0` / `840,0` / `588,84` / `420,0` / `336,84`）⇒ **不是抽取时切错了同一块**。
#    本批又把 6 张卡的徽记裁下来并排看（generic / Blood / Fate / Excess / `[Chaos]` / `Chaos Lord`）
#    —— 同一张图。⇒ 一律 `markOfChaos`（判据与理由写全在 `BARE_TOKEN_BY_RULE` 那一段的注释里）。
#
# **本组记号**（`BARE_TOKEN_BY_RULE` 末尾两条产出 / 别处也画得到同一枚的）：
DARKPACT_SPRITES = {"markOfChaos", "markOfKhorne", "markOfNurgle", "markOfSlaanesh", "markOfTzeentch"}
DARKPACT_TOKENS = {"Dark Pact", "黑暗契约", "黑暗契约时",
                   "[Dark Pact]", "[Chaos]", "[黑暗契约]", "❄"}

# 🔴 **这一处 `Dark Pact` 的徽记【已经由另一条记号画了】** ⇒ 按词规则别插手（插了 = 同一处画两枚）。
#    形状与自检**逐条照 `_PREFIX_ABSORB_ROWS` 那一族**（表里的行必须**真被用到**、
#    被点名的记号必须**真在计划里** —— 两条任一不成立就 rc=1，⛔ 别让例外变成静默的丢图标）。
#    列 = (卡 id, 字段, **已经画着那枚**的记号, 判据)
_DARKPACT_ALREADY_ROWS = [
    ("BL8", "desc", "❄",
     "卡面 `Chaos/3部队/Warpforge_19_Noise-Marine.png` 亲读 = `When your Warlord receives damage, "
     "gain a 〔红盘 + 白八芒星〕Dark Pact of Excess` —— 那枚徽记就是 `desc` 里 `❄` 占的那一格。"
     "`❄` 走 `SYMBOLS`（`markOfSlaanesh`，渲染上与 `markOfChaos` 逐像素相同）。"),
    ("BL8", "descZh", "❄",
     "同上那半（中文比英文早就有 `❄`；`A1473` 2026-10-10 才把英文侧对齐过来）。"
     "⚠️ 中文这里 `❄` 与词之间还夹着一个「纵欲」（`❄纵欲黑暗契约`）⇒ **别改成「紧邻才让位」那种判据**"
     "（那样这一格会漏判、同一处画两枚）。"),
    ("BL57", "desc", "[Chaos]",
     "卡面 `Chaos/5防御卡/Warpforge_69_Hellfire-Torch.png` 亲读 = `Give a 〔同一枚红盘白八芒星〕"
     "Dark Pact to a friendly troop` —— `[Chaos]` 那个 token **就是**这一枚（`TOKEN_BY_CARD` 里那条的"
     "`why` 写着「卡图 + NCC 0.932（暗红盘 + 白八芒星）」），词留在后面。"),
    # ⚠️ **故意不列的四格**（列了 = 死条目、自检会红）：`BL51` 两半 与 `BL69` 的 `descZh` ——
    #    它们由**守卫①**（本字段含 `[Dark Pact]` / `[黑暗契约]`）整字段挡掉，而那两个 token
    #    **本身就是那枚徽记**（渲染成 `〔徽记〕Dark Pact` / `〔徽记〕混沌黑暗契约`，与卡面逐字一致）。
]
DARKPACT_ALREADY = {(c, f) for c, f, _t, _w in _DARKPACT_ALREADY_ROWS}

# ── `Dark Pact` 一族的**卡面枚数自检**（期望值来自**成品卡图**，⛔ 不是从我们的实现里算的）──────
# **量什么**：这个字段里**画出的 `Dark Pact` 徽记有几枚** = `Σ 每条 `DARKPACT_TOKENS` 里的 token
#    在原文里的出现次数`（`Rewrite` 是全局替换；`[Dark Pact]` / `[Chaos]` / `❄` 那几条
#    各自只在 `BL51` / `BL69` / `BL19` 上出现，`[`-开头的键不计入「互相包含」自检 —— 见 `conflicts`）。
# **为什么它必须单独有一条**：`A1623` 之前这一族**一枚都不画**，而计划表里「有没有条目」
#    看不出来这件事（`BL19` 那种靠 `❄` 画着的更是看不出）⇒ 只有「**画出来几枚**」能把漏画抓出来。
#     同时它也是**灭自证**那一半的载体：把这几条记号从计划里抽掉 ⇒ 每一行都必须变红（见 `main()`）。
# **判据**：37 张 PnP 成品卡图**逐张开图亲读**（本批作者全读了一遍，见报告的逐张对照表）
#     ＋ `资料/卡表核对_卡图提取/_还原效果文字.md` 的**双写**（`Dark PactDark Pact` = 图标 + 词）：
#     转录表里**恰好 37 行**有双写、与这 37 张卡**一一对应**（集合相等，`ours ⊖ 转录 = ∅`）。
#     ⇒ **每张各 1 枚**，只有 `BL45 Chaos Lord` 是 **2 枚**（卡面亲读 = `give it a random 〔徽记〕Dark Pact.`
#     ＋ `⚡Rally: Gain a random 〔徽记〕Dark Pact`）。
#     ⚠️ `descZh` 的枚数**与 `desc` 一样**：中文卡面不存在，判据 = **英文卡面那一半**
#     （原版客户端没有中文表，全仓已定案）⇒ 中文那一侧只要求「与英文逐处对应」。
#     🔴 **2026-10-14 更正（铁律 5）**：括号里「**原版客户端没有中文表，全仓已定案**」**不成立** ——
#        原版**有**官方中文表（远端 Addressables 包下发、2026-10-14 已解开并落地）；
#        **判据全文与本文件的口径 → `_STUN_CARDS` 上方那条 `2026-10-14` 更正**（⛔ 别抄第二份）。
#        ⇒ **本行结论【不变】**：「中文卡面不存在」说的是**我们手上只有英文的 PnP 卡图/转录表**，
#        与「原版有没有中文表」无关 ⇒ 判据仍只到「英文卡面那一半」。
#     ⚠️ 也正是这条把三处例外钉住了：`BL51`（词出现两次、卡面 1 枚）、`BL69` 的 `descZh`
#     （`[黑暗契约]混沌黑暗契约` = 1 枚）、`BL74` 的 `descZh`（我们译成两处词、卡面 1 枚）。
DARKPACT_FACE_COUNT = [
    # (卡 id, `desc` 该画几枚, `descZh` 该画几枚, 卡图文件名)
    ("97e959444b53f424ea4285e2ca02c7e6", 1, 1, "Warpforge_2_Chosen-of-the-Four.png"),
    ("141c5ed661a9e42bd9a793f0d2a1c682", 1, 1, "Warpforge_8_Unhallowed-Gifts.png"),
    ("BL3", 1, 1, "Warpforge_12_Chaos-Legionary.png"),
    ("BL4", 1, 1, "Warpforge_13_Chaos-Sergeant.png"),
    ("BL6", 1, 1, "Warpforge_15_Rubric-Marine.png"),
    ("BL7", 1, 1, "Warpforge_17_Khorne-Berzerker.png"),
    ("BL8", 1, 1, "Warpforge_19_Noise-Marine.png"),
    ("BL10", 1, 1, "Warpforge_21_Plague-Marine.png"),
    ("BL12", 1, 1, "Warpforge_25_Aspiring-Champion.png"),
    ("BL13", 1, 1, "Warpforge_26_Chosen.png"),
    ("BL16", 1, 1, "Warpforge_29_Possessed-Champion.png"),
    ("BL63", 1, 1, "Warpforge_32_Havoc.png"),
    ("BL21", 1, 1, "Warpforge_33_Lheorvine-Ukris.png"),
    ("BL24", 1, 1, "Warpforge_39_Chosen-Champion.png"),
    ("BL25", 1, 1, "Warpforge_40_Dark-Apostle.png"),
    ("BL28", 1, 1, "Warpforge_42_Helbrute.png"),
    ("BL30", 2, 2, "Warpforge_45_Chaos-Lord.png"),
    ("BL31", 1, 1, "Warpforge_46_Maulerfiend.png"),
    ("BL37", 1, 1, "Warpforge_49_Abaddons-Chosen.png"),
    ("BL38", 1, 1, "Warpforge_51_Dark-Oratory.png"),
    ("BL43", 1, 1, "Warpforge_54_Daemonic-Frenzy.png"),
    ("BL47", 1, 1, "Warpforge_55_Disgustingly-Resilient.png"),
    ("BL46", 1, 1, "Warpforge_56_Gifts-of-Chaos.png"),
    ("BL48", 1, 1, "Warpforge_58_Skeins-of-Fate.png"),
    ("BL49", 1, 1, "Warpforge_61_Delightful-Agonies.png"),
    ("BL52", 1, 1, "Warpforge_66_Unholy-Smite.png"),
    ("BL57", 1, 1, "Warpforge_69_Hellfire-Torch.png"),
    ("BL72", 1, 1, "Warpforge_10_Accursed-Helbrute.png"),
    ("9176318eaf469354e858f51af2333aa3", 1, 1, "Warpforge_2_Litany-of-Despair.png"),
    ("BL69", 1, 1, "Warpforge_7_Meltagun-Legionary.png"),
    ("2621d4280735baf4c952e6afea8d0686", 1, 1, "Warpforge_06_Excessive-Vigour.png"),
    ("EC9", 1, 1, "Warpforge_14_Icon-of-Excess.png"),
    ("EC20", 1, 1, "Warpforge_34_Turyan-Ghauze.png"),
    ("EC36", 1, 1, "Warpforge_42_Chosen-of-Slaanesh.png"),
    ("EC41", 1, 1, "Warpforge_50_Internal-Rivalry.png"),
    ("EC38", 1, 1, "Warpforge_52_Pledge-to-the-Dark-Prince.png"),
    ("EC60", 1, 1, "Warpforge_62_Armoury-of-Excess.png"),
]


def darkpact_marks(got, text):
    """这个字段**画出来的 `Dark Pact` 徽记枚数**（纯函数 —— 灭自证那一半要拿它跑合成表）。"""
    return sum(text.count(t) for t in got if t in DARKPACT_TOKENS)


GAP_BY_CARD = {}

# 同一类：token 认得出、但**图集里根本没有同名的那张图**。
# ⚠️ 2026-09-15 更正：这里原来挂着 `[Destroyer]` / `[毁灭者]`，理由是「78 张图集里没有
#    `destroyer.png`」。**前提就错了** —— 原版给「毁灭者」这个关键词用的图本来就叫
#    `frenzied`（`Skorpekh Destroyer` 卡面上「Destroyer.」旁边印的就是它），
#    所以「按名字找不到」是必然的，不代表缺素材。已升级成 `TOKEN_BY_CARD` 里两条。
NO_SPRITE = {}

# ── 按规则的 token（不是按卡）────────────────────────────────────────────────
#      ⚠️ 每条都要写清「为什么它可以按规则」，否则就该进 `TOKEN_BY_CARD`
TOKEN_BY_RULE = [
    ("[Spirit Stone]", None, "SpiritStone_{n}",
     "档位看紧挨着的数字：`N [Spirit Stone]:` → `SpiritStone_N`（**数字烘在图里**，"
     "所以卡面文本里那个 `N ` 要一起吃掉）。图集 `Atlas_SpiritStone_1..5` 逐张看过；"
     "真卡 Spiritseer(2) / Wraithknight(3) 核过"),
    ("[Energy]", None, "faith",
     "**行首付费前缀**：卡面画的是**这一行的资源图标**。修女会 = 金太阳（信仰）。"
     "逐张核过 Daemonbreaker `8` / Miraculous Feat `6` / Fiery Conviction `4` 三张 + Preacher"),
    # ---- 🆕 2026-10-18：**属性记号**（`[攻击]/[远程]/[Melee]/[Ranged]`）----------------
    #  ⚠️ 这四条和上面那三条**性质不同**，必须先说清「凭什么是按规则的」——
    #     本文件头写着「token 名不可信」，那是针对**卡图 OCR 猜出来的**名字
    #     （`[Health]` 画的是拳、`[Power]` 画的是枪…）。这四条是**我们自己写的**：
    #     2026-10-18 那一批把 `+N 近战攻击 / +N 远程攻击 / +N Melee / +N Ranged` 这批
    #     **卡面上只印图标、不印词**的位置改写成了方括号记号（改动在
    #     `数据/游戏数据/cardface_fixes.json` 的 `desc`/`descZh`，说明见那张表的
    #     `_2026-10-18_属性图标方括号`）。
    #  ✅ **旁证 = 全池既有同形记号，无一例外**（`--write` 前实测扫过）：
    #     `[攻击]` 18 处 → **全部** Melee · `[远程]` 7 处 → **全部** Ranged ·
    #     `[Ranged]`/`[ranged]` 8 处 → **全部** Ranged · `[Melee]` 0 处（新记号）。
    #     ⇒ 加这四条**不改动任何既有卡的答案**（① 逐卡表先命中，那 33 处本来就有答案）。
    #  🔴 `[Attack]` **故意不在**这张表里：它在老数据里拳/枪都有（`EC23`/`AM22`/`AM42`/
    #     `AM75`/`DA73` 是枪、`DA44`/`DA50` 是拳）⇒ 名字不可信，**只能逐卡定**
    #     （本批给它新写的那 94 条在下面 `TOKEN_BY_CARD` 末尾，整块带说明）。
    #  ⚠️ **英文侧「新写的近战记号」用哪个名字，是有约束的**（别顺手统一成 `[Melee]`）：
    #     英文 `desc` 是**引擎输入**，`GivePayload.ReAttr` 认的是**字面** `attack` ⇒
    #     原文写 `Attack` 的地方要写 `[Attack]`（去括号后逐字节相同）；原文写 `Melee` 的地方
    #     才写 `[Melee]`。例外只有 `EC39`（走 `TryDouble` 的**字面** `melee and ranged`）。
    ("[攻击]", None, "Melee", "中文「近战」记号（本批新写的；全池既有 18 处全部 = 拳）"),
    ("[远程]", None, "Ranged", "中文「远程」记号（本批新写的；全池既有 7 处全部 = 枪）"),
    ("[Melee]", None, "Melee",
     "英文近战记号 —— **只给原文本来就写 `Melee`/`Melee Attack` 的位置**用"
     "（那些位置去括号后与改前逐字节相同）。原文写 `Attack` 的用 `[Attack]` + 逐卡条目"),
    ("[Ranged]", None, "Ranged", "英文远程记号（本批新写的；全池既有 8 处全部 = 枪）"),
    ("[\u80fd\u91cf]", None, "faith", "同上（中文写法）"),
]

# 锚点核对用的最大窗口
MAX_CONTEXT = 26


def norm_name(s):
    """卡名归一：小写、去掉撇号/引号/标点 —— 还原表里写的是 `'Uge Choppa` / `Wreckin' Ball`，
    我们卡表里是 `Uge Choppa` / `Wreckin Ball`，**不归一就匹配不上**（实测漏 4 张）。"""
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def load_restored():
    """归一化卡名 → 还原后的效果文字（`资料/卡表核对_卡图提取/_还原效果文字.md`）。"""
    rows = {}
    with open(RESTORED, encoding="utf-8") as f:
        for ln in f:
            if not ln.startswith("|") or ln.startswith("| ---") or ln.startswith("| 文件名"):
                continue
            p = [x.strip() for x in ln.strip().strip("|").split("|")]
            if len(p) >= 4 and p[1]:
                rows[norm_name(p[1])] = p[2]
    return rows


CANON = {}          # 小写 → 词表里的规范写法（`flying` → `flying`、`melee` → `Melee`）
MARK_L, MARK_R = "\u2770", "\u2771"       # 独立图标（卡面只画图标、不印词）
MARK2_L, MARK2_R = "\u2768", "\u2769"     # 图标 + 紧接着还印着词（原版最常见的写法）


def trait_names():
    return [os.path.splitext(f)[0] for f in sorted(os.listdir(TRAITS)) if f.endswith(".png")]


EXTRA_VOCAB = ["Energy", "Spirit Stone", "Spirit Stones", "Quest Point", "Quest Points",
               "Blood Thirst", "Long Range", "Hunt Mark", "Can't Attack", "Dark Pact"]

# ── 卡面上的「关键词前缀」图标（2026-09-15 补）────────────────────────────────
#     卡面上**每个关键词都印成「图标 + 紧跟那个词」**（`【翼】Flying.`、`【闪电】Rally:`），
#     但 `desc` 是**卡图 OCR** 的 —— 这些图标基本没被记下来（只有极少数留下了 `⚡`/`💀` 字符），
#     所以本表原来只覆盖 `[方括号]` 记号，卡面上这一大批位置**全画不出来**
#     （实测：`desc` 句首关键词 42 种 / 526 处，`descZh` 55 种 / 484 处）。
#
#     ✅ **这一类可以按名字查图**：图集文件名就是关键词名（`rally.png` / `agenda.png` / …），
#        中文走 `资料/关键词图标/_规则书关键词表.md` 那 61 条中英对照。
#        **不像数值类 token 那样必须逐卡定** —— 这就是它敢写成规则的理由。
#     ⚠️ 例外逐条进 `KEYWORD_PREFIX`（`Destroyer` 的图叫 `frenzied`；`Penitence` 盘上真没有）。
#     ⚠️ `KEYWORD_PREFIX_SKIP` 那几个**不参与**：`Melee` / `Ranged` 是「数值」写法，
#        位置规则完全不同（见本文件头 / 对照文档 §三），放进来会把 `+2 Melee` 那种位置画错。
#     ⚠️ **只认「句首位置」**（行首 / `. ` / `; ` 之后；中文 `/`。``/``；`` 之后）。
#        卡面**句中**的关键词**有时也带图标**（`Baneblade Tank` 的第二个 `Armour 1`、
#        `Lead by Example` 的 `Duty ability`、`Attilan Rough Rider` 的 `Blood Thirst` —— 都实测过），
#        但那要靠语义判断「这个词是当关键词用、还是当普通名词/动词用」。
#        🔴 **2026-10-10（`A1366`）就地更正**：这里原来举的例子里写「`Stun an enemy` … **都不带图标**」——
#        **那句话是错的**（`Stun` 恰恰**恒带**：句首/句中/名词位置都带，30 张逐张核过）。
#        `Stun` 那一条已按卡补进 `BARE_TOKEN_BY_CARD`（见上面那张 `_STUN_CARDS`）。
#        🔴 **2026-10-10（`A1376`）再就地更正一行**：紧接上一条留下的「仍待核的原例 =
#        `Choose a Sabotage card`（多半不是缺口：破坏走 `subtype`、是卡类名词）」—— **这句也被证伪了**：
#        `Sabotage` **同样恒带**（还原表里 **14 张卡面全部双写**；亲读
#        `Genestealer Cult/5防御卡/Warpforge_67_Pilfered-Supplies.png` 是
#        `Choose a〔白/红圆盘 + 红齿轮〕Sabotage card`）⇒ 已按卡补进 `BARE_TOKEN_BY_CARD`。
#        ⇒ 那两个原例里**只剩「写成词的 `Ranged Attack`」仍是未核**。
#        🔑 **`A1376` 那 7 个词的实测把这条判据钉得更死：要按【词】定，别按「句中/句首」一刀切**——
#        **恒带**（N/N）：`Vulnerable` 14/14 · `Hunt Mark` 22 张 25 处/25 · `Sabotage` 14/14 ·
#        `Sniper` 13/13 · `Fast` 5/5；
#        **不是恒带**：`Blood Thirst` 10 张里 9 张带（`GOF90 Beastboss on Squigosaur` 卡面**确实没画**）·
#          🔴 **2026-10-12 更正**：`GOF90` 那半句**错** —— 卡面**印了**图标（亲读证据见上面 `:1019` 那一段的更正）。
#          ✅ **2026-10-13（`A1713`）已按卡补上**（`BARE_TOKEN_BY_CARD[("GOF91", "Blood Thirst")]`）。
#          ⚠️ **但别把它推广成「该词恒带」** ——
#          「10 张里 9 张带」这个分母是**卡池**，本轮只验了 `GOF90` 这一张；**其余 9 张本轮没逐张核**。
#        `Blind` 6 张里 5 张带（`SAU55 Solar Pulse` 同）· `Relentless` 0/1（卡面那个词是**天赋名**）。
#        ⇒ 前者按卡补、后者**如实不补**（详见那个块的 ③）。
#        **判不了就不画** —— 宁可少画，不给错图标（工程红线）。
KEYWORD_PREFIX = {
    "Destroyer": "frenzied",     # 图集里没有 destroyer.png：原版给「毁灭者」用的就是这张
    "Penitence": "rage",         # 图集里没有 penitence.png —— 原版给它用的就是 rage 那张
}
KEYWORD_PREFIX_SKIP = {"Melee", "Ranged"}

# ── 不带方括号的**资源词**（2026-09-15 逐张卡面核过）────────────────────────────
#      ⚠️ **只有「任务点」这一类真有图标，另外两个没有** —— 别照名字给它们补：
#      · `Quest Point(s)`：**有**。`Gain 1 Quest Point` 卡面画的是 `questPoints1`
#        —— 数字**烘在图里**（`Dark Angels/3部队/Warpforge_12_Aggressor.png` 卡面
#        `Strike: Gain 〔银灰尖刺环里烘着 1〕` 逐张看过；`Repulsor` 的 `gain ③` 同）。
#        没带数字时画无数字的那张（`questPoints`）。
#      · `Spirit Stone(s)`：**没有**。`Aeldari/3部队/Warpforge_35_Spiritseer-Qelenaris.png` 卡面
#        `Waystone. When you collect a Spirit Stone, gain 2 additional Spirit Stones` —— **全是纯文字**。
#        灵魂石图标只出现在**行首付费前缀**（同目录 `Warpforge_26_Spiritseer.png` 行首
#        `〔绿圈里烘着 2〕Deploy a Wraithguard` = `SpiritStone_2`），那条 `TOKEN_BY_RULE` 早就有。
#        ⚠️ 顺带更正一条：`Spirit Stone` **不是** `waystone.png` —— `waystone` 是**另一个关键词**
#        `Waystone` 的图标（蓝三角 + 眼），两者毫无关系。
#      · `Faith`：**没有**。`Sorotitas/3部队/Warpforge_14_Amalia-Novena.png` 卡面
#        `Rally: Deal damage to an enemy troop equal to your Faith` —— 纯文字。信仰图标同样只做行首前缀。
#      ⇒ **按名字给后两个补图标 = 给卡面加上原版根本没有的东西**（工程红线）。
QUEST_WORD = re.compile(r"(?:(\d+)\s+)?Quest\s+Points?")
QUEST_N_ZH = re.compile(r"(\d+)\s*点任务")
QUEST_PLAIN_ZH = re.compile(r"任务点")
RESOURCE_WHY = ("不带方括号的**资源词**：`Quest Point(s)` 卡面画的是 `questPointsN`"
                "（数字烘在图里 —— `Aggressor` 卡面 `Gain 〔银刺环里烘着 1〕` 逐张核过）。"
                "⚠️ 同为资源词的 `Spirit Stone` / `Faith` 写在正文里**没有图标**（卡面实测），"
                "所以本规则**只做任务点**。")

# 句首：行首 / `. ` / `; ` 之后 → 1~3 个大写词 → 可选数字 → `:` 或 `.`
#
# 🔴 **2026-10-11（`A1476` · 收尾符放宽到「或文末」）** —— 改前这里写死 `([:.])`，
#    **要求关键词后面紧跟 `:` 或 `.`**；而卡面**句尾那一段是不印句号的**（印刷约定，
#    `A1444` 那批裁块放大亲读 5 张 + `C1` 再一次亲读 3 张坐实：
#    `ASH81` `… 〔紫爆炸〕Blast 3` · `AM37` `〔准星〕Long Range. 〔紫爆炸〕Blast 3` ·
#    `AM28` `… 〔紫爆炸〕Blast 2`）⇒ 全池 `desc` 以**裸 `Blast N` 收尾的 17 张**
#    那枚徽记**一枚都画不出来**。
#    🔑 **最干净的对照（`AM37 Basilisk`，同一张卡中英两侧互为镜子）**：
#    计划表英文侧只有 `Long Range.`、中文侧却有 `爆破 3。` —— 差的**只是中文那个 `。`**
#    撞上了收尾符要求 ⇒ 这是「真因在扫描器」的**直接读数**，不是推测。
#    ⇒ 改成 `(?:([:.])|(?=$))`：**收尾符照旧要**，但**文末那一种不要求**。
#    ⚠️ **只放宽「文末」这一种**，⛔ 不许把收尾符写成 `?`（那会让 `Blast 3 to a friendly troop`
#       里的 `Blast 3` 也命中 = 凭空多画一枚）。
#    ⚠️ **收尾符不参与时 `group(3)` 是 `None`** ⇒ 取匹配段右端一律走 `keyword_scan_end(m)`
#       （⛔ 别写 `m.end(3)` —— 那是 `-1`，会把最后一个字符切掉）。
KEYWORD_SCAN = re.compile(
    r"(?:^|[.;]\s+)([A-Z][A-Za-z'\-]*(?:\s+(?:of\s+)?[A-Z][A-Za-z'\-]*){0,2})(?:\s+(\d+))?\s*(?:([:.])|(?=$))")
# 中文版：行首 / `。` / `；` / `，` 之后 → **1~8** 个汉字 → 可选数字 → `：` 或 `。`
# 🔴 2026-10-08 修：下界原来是 **2**，而 `资料/关键词图标/_规则书关键词表.md` 里
#    **`团` → `Regiment` 是单字关键词** ⇒ 那一句永远扫不到、卡面**漏画 `regiment` 徽记**。
#    实测：`AM14/AM9/AM12/AM33/AM30`（`团：…`）、`Armoured Sentinel`、`Rogal Dorn Tank`、
#    `Tempestor Sergeant` 等 —— 中文卡面那一枚徽记**一枚都没画**（英文侧 `Regiment:` 早就画着，
#    因为英文那条正则的下界本来就是 1）。
#    卡图判据：`d:/2/Warpforge部队卡片/Astra Militarum/3部队/Warpforge_14_Kasrkin.png`
#    —— 卡面是 `〔骷髅头盔圆徽〕Regiment: Deal 1 damage…`（**图标 + 词**），
#    所以中文侧也必须是 `〔徽记〕团：…`（裸 token 那支：插在词前、词留着）。
#    ⚠️ **下界改成 1 不会误伤**：`([一-龥]{1,8})` 是**贪婪**的，长的先试（`团结一致：` 仍先试
#    `团结一致`、词表里没有 ⇒ 不命中）；而 `zh_en.get(raw)` 那道闸只放行词表里的词，
#    词表里**只有 `团` 一个字是单字**。实测：改完 `--write` 只多出下面那几条，其余一字未动。
# 🔴 **2026-10-15（第三十二会话）：前导分隔符要收 `<br>` 系列。**
#   判据：官方那代文本用 `<br>` 断行（实测 `ASH34.descZh = '护甲 2。<br>②：…<br>天赋：凯恩之怒。'`、
#   `UM23.descZh = '护甲 1。先锋。<br>典籍：治疗 3。'`）—— 旧正则只认 `。；,，` ⇒
#   `天赋` / `典籍` 两次声明**一次都抽不出来** ⇒ 中文卡面那两枚图标**压根没画**
#   （`IconSetup.Verify` 的 ①a `ZhGaps` 报 `ASH34|talent` / `UM23|codex` —— 实测是**真缺口**。
#   同池对照：真换行那几处（`GOF76` / `SW19`）抽得出，只有 `<br>` 那几处抽不出）。
#   出处 = `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·4。
KEYWORD_SCAN_ZH = re.compile(r"(?:^|<br\s*/?>\s*|[。；,，]\s*)([一-龥]{1,8})(?:\s*(\d+))?\s*(?:([：。])|(?=$))")


# 🔴 **2026-10-11（`A1476`）**：上面两条正则的收尾符改成「**照旧要 `:`/`.`/`：`/`。`，
#    但【文末那一种】不要求**」—— 理由（卡面句尾不印句号）与全池读数写在前一条的注释里。
#    ⚠️ **收尾符不参与时 `group(3)` 是 `None`** ⇒ 右端一律走本函数：
#       写 `m.end(3)` 会拿到 **`-1`**，把匹配段的最后一个字符**静默切掉**
#       （token 就是运行时 `s.Replace(token, tag)` 的键 ⇒ 切掉尾巴 = 那个位置**一个图标都不画**）。
def keyword_scan_end(m):
    """匹配段的**右端**（= token 的结束下标）。收尾符可省 ⇒ 逐级回退到有参与的那一组。"""
    for g in (3, 2, 1):
        if m.group(g) is not None:
            return m.end(g)
    return m.end(1)


# 🔴 2026-09-21 修 —— **用户从卡面图上抓到的真 bug：连续句首关键词漏画第 2、4…个**
#
#   上面两条正则的**前导分隔符是匹配的一部分**（`(?:^|[。；,，]\s*)`），而 `finditer` **不重叠**
#   ⇒ 前一句的**收尾符** `。` 已经落在上一个匹配里了，于是**再也当不了后一句的前导符**：
#       `路标石。伪装。猛击：` → 只出 `['路标石。', '猛击：']`，中间的 `伪装` **静默丢掉**。
#
#   ⇒ 扫的时候**手动推进游标，并回退到收尾符本身**（等价于把前导符改成零宽断言）。
#   ⚠️ 不能直接把前导符改写成 lookbehind —— Python `re` 要求 lookbehind **定宽**，
#      `[。；,，]\s*` 这种变宽写法会直接抛 `look-behind requires fixed-width pattern`。
def scan_chained(scan, text):
    """句首关键词的逐句扫描 —— **分隔符不消费**（前一句的收尾符同时是后一句的前导符）。

    每轮至少前进 1 字符（`pos + 1`），不会死循环；`group(3)` 是收尾的 `:` / `.` / `：` / `。`。
    ⚠️ **`A1476` 起收尾符可以【不参与】**（文末那一种，`group(3) is None`）—— 但**推进游标**的
    判据**不许**换成 `keyword_scan_end(m)`：那条规矩是「**回退到收尾符本身**」（收尾符要留给
    下一句当前导符），换成「匹配段右端」= **把收尾符吃掉** ⇒ 下一句的句首关键词**静默漏画**
    （2026-10-11 实测：换成它之后 `关键词前缀` 从 **1355 → 1193**，正是这条）。
    写法 = 收尾符有参与就用 `m.start(3)`，不参与（文末）才退回 `keyword_scan_end(m)`。
    """
    pos = 0
    while pos <= len(text):
        m = scan.search(text, pos)
        if m is None:
            return
        yield m
        pos = max(m.start(3) if m.group(3) is not None else keyword_scan_end(m), pos + 1)

KEYWORD_WHY = ("卡面「图标 + 关键词」：句首这枚走**名字**（图集文件名就是关键词名，中文走 "
               "`_规则书关键词表.md` 那 61 条中英对照）。逐张核过这个写法：`Codex:` / `Armour 1.` / "
               "`Regiment:` / `Duty:` / `Ephemeral.` / `Waystone.` / `Flank.` / `Rally:` / `Blast 4.`")


def load_zh_keywords():
    """`_规则书关键词表.md` → {中文名: 英文名}（61 条）。表体是 `| 中文名 | 英文名 | 效果 | …`。"""
    out = {}
    path = os.path.join(ROOT, "资料/关键词图标/_规则书关键词表.md")
    if not os.path.exists(path):
        return out
    with open(path, encoding="utf-8") as f:
        for ln in f:
            if not ln.startswith("|"):
                continue
            p = [x.strip() for x in ln.strip().strip("|").split("|")]
            if len(p) >= 2 and p[0] and p[1] and p[0] != "中文名" and not set(p[0]) <= set("-: "):
                out[p[0]] = p[1]
    return out


def keyword_prefixes(text, idx_en, zh_en, taken):
    """句首「关键词 [+ 数字] + `:` 或 `.`」→ `[(token, sprite, why)]`。

    `taken` = 已经被 `[方括号]` 记号占掉的区间（跳过它们，别给同一个位置画两次）。
    **token 取原文那一段**（含尾随的 `:` / `.` / 数字）—— 运行时是 `s.Replace(token, tag)`，
    这样只会命中这一个写法，不会误伤同名的普通词。`sprite == ""` 表示认得出关键词但盘上没图。

    ⚠️ 逐句扫要走 `scan_chained`（`finditer` 会漏掉连续句首的第 2、4…个关键词，见它的注释）。
    """
    out = []
    for is_zh, scan in ((False, KEYWORD_SCAN), (True, KEYWORD_SCAN_ZH)):
        for m in scan_chained(scan, text):
            if any(s <= m.start(1) < e for s, e in taken):
                continue
            raw = m.group(1)
            en = None
            if is_zh:
                en = zh_en.get(raw)
            else:
                words = raw.split()
                for L in range(len(words), 0, -1):
                    w = " ".join(words[:L])
                    if w in KEYWORD_PREFIX:
                        en = w
                        break
                    if norm_name(w) in idx_en:
                        en = w
                        break
            if en is None:
                continue
            # 同一个东西两种叫法（`Concussion` → `concussive` / `Dark Pact` → `markOfChaos` …）
            en = ALIAS.get(en, en)
            sprite = KEYWORD_PREFIX[en] if en in KEYWORD_PREFIX else idx_en.get(norm_name(en))
            if sprite is not None and sprite in KEYWORD_PREFIX_SKIP:
                continue
            token = text[m.start(1):keyword_scan_end(m)]
            if not sprite:
                out.append((token, "", "缺口：" + KEYWORD_WHY))
                continue
            out.append((token, sprite, KEYWORD_WHY))
    return out


def resource_tokens(text, taken):
    """不带方括号的**资源词** → `[(token, sprite, why)]`（目前只有「任务点」，见上面那段说明）。

    token 取原文那一段（**含前面的数字**）—— 数字是烘在图里的，运行时必须连它一起吃掉，
    否则会「图标里有 1、旁边又印一个 1」（和 `[Spirit Stone]` 那条同一个道理）。
    """
    out = []

    def emit(m, tok, sprite):
        if any(s <= m.start() < e for s, e in taken):
            return
        out.append((tok, sprite, RESOURCE_WHY))

    for m in QUEST_WORD.finditer(text):
        n = m.group(1)
        emit(m, text[m.start():m.end()], ("questPoints" + n) if n else "questPoints")
    for m in QUEST_N_ZH.finditer(text):
        emit(m, m.group(0), "questPoints" + m.group(1))
    for m in QUEST_PLAIN_ZH.finditer(text):
        emit(m, m.group(0), "questPoints")
    return out


def annotate(text, pat):
    """还原文字 → 带 ⟦图标⟧ 标记的文本（只用来做**交叉核对**）。

    · `FlankFlank` / `Flank Flank`（图标 + 印着词）→ `⟦Flank⟧Flank`
    · `+2 Melee`（独立图标，没有词）              → `+2 ⟦Melee⟧`
    """
    out, pos, skip_until = [], 0, 0
    for m in pat.finditer(text):
        if m.start() < skip_until:
            continue
        word = CANON.get(m.group(0).lower(), m.group(0))   # `Flying` → 词表里的规范写法
        tail = text[m.end():]
        stripped = tail.lstrip(" ")
        gap = len(tail) - len(stripped)
        out.append(text[pos:m.start()])
        if stripped.lower().startswith(word.lower()):      # 双写 = 图标 + 词
            out.append(MARK2_L + word + MARK2_R)
            skip_until = m.end() + gap + len(word)
            pos = m.end()                                  # 词本身留给原文
        else:                                              # 独立图标（后面没有词）
            out.append(MARK_L + word + MARK_R)
            skip_until = pos = m.end()
    out.append(text[pos:])
    return "".join(out)


def norm(s):
    return re.sub(r"\s+", " ", s.replace("\u00a0", " ")).strip()


def find_icon_by_anchor(annotated, pre, post):
    """交叉核对用：按前后锚点在还原表里找那个图标。返回 (sprite, 说明)。"""
    a = norm(annotated)
    pre_n, post_n = norm(pre), norm(post)
    if pre_n and " " in pre_n:
        pre_n = pre_n[pre_n.index(" ") + 1:]
    if post_n and " " in post_n:
        post_n = post_n[:post_n.rindex(" ")]
    for use_pre, use_post in ((True, True), (True, False), (False, True)):
        p = pre_n if use_pre else ""
        q = post_n if use_post else ""
        if not p and not q:
            continue
        i = a.find(p) if p else 0
        while i >= 0:
            j = i + len(p)
            k = a.find(q, j) if q else -1
            if not q or (k >= 0 and k - j <= MAX_CONTEXT + 12):
                seg = a[j:k] if q else a[j:j + MAX_CONTEXT]
                # ⚠️ 三类标记分开看：`[token]` 在卡面上是**独立图标**（没印词），
                #    所以先只认 ⟦独立⟧；一个都没有时再看 ❨图标+词❩（`[Shield] Shield` 那类）。
                #    不分开的话，紧跟其后的「图标+词」会被当成它的答案
                #    （实测 EC40 `[attack]` 因此被配成 Cruelty）。
                hits = re.findall(re.escape(MARK_L) + "([^" + MARK_R + "]+)" + re.escape(MARK_R), seg)
                if not hits:
                    hits = re.findall(re.escape(MARK2_L) + "([^" + MARK2_R + "]+)" + re.escape(MARK2_R), seg)
                if hits:
                    # ⚠️ 有后锚点时取**最后一个**标记 —— 段尾就贴着后锚点，所以最后那个才是它的图标。
                    #    取第一个会撞上相邻的前一个图标（实测 EC8 `[Attack]` 就这样被配成 Melee）。
                    return (hits[-1] if q else hits[0]), "锚点对齐"
                return None, "锚点命中、中间没有图标"
            i = a.find(p, i + 1)
    return None, "锚点没对上"


# ── 还原表用的词 → 我们盘上的 sprite 名（同一个东西两种叫法，别让它变成「缺图」）────
ALIAS = {
    # ⚪ 2026-10-12（第二十一会话 · 写手 `C5` · `A1604`）：`"Energy": "faith"` **已删**。
    #    那条是**上游错标签的补丁** —— `工具/restore_card_icons.py` 原来把「太阳」记成 `Energy`
    #    （真值 `Faith`；判据 = PnP 亲读 + `_图标清单.md:31` + 卡池 `[faith]`/`☀`，
    #    见那个脚本 `STANDALONE['太阳']` 里那一整段）。上游本轮已就地订正
    #    ⇒ 这条成了**死条目**，而且它会**掩盖**以后真的把某枚图标画成 `Energy` 的那种错
    #    （盘上**没有** `energy.png` 这张图，本该当场报「sprite 名盘上没有」）。
    #    ⚠️ `Energy` 仍留在 `EXTRA_VOCAB` 里 —— 那是**另一个用途**：卡面上真印着
    #    `Refill all your Energy` / `Waaagh! Energy` 这类**词**（还原表里 5 处），跟这条别名无关。
    #    ⚠️ `Faith` **不需要**在这张表里加一条：锚点匹配到的词经 `CANON` 归一后就是图集文件名
    #    （`faith.png` → `faith`），直接查得到盘（实测本轮 `🔴 sprite 名盘上没有: 无`）。
    "Quest Point": "questPoints", "Quest Points": "questPoints", "Quest": "questPoints",
    "Spirit Stone": "SpiritStone_1", "Spirit Stones": "SpiritStone_1",
    "Dark Pact": "markOfChaos", "Blood Thirst": "bloodThirst", "Long Range": "longrange",
    "Hunt Mark": "huntMark", "Can't Attack": "cantAttack", "Concussion": "concussive",
    "Mark Of Chaos": "markOfChaos", "MarkOfChaos": "markOfChaos",
}

# ── 中文 token ↔ 英文 token：中文按**它对得上的那个英文 token 的答案**走 ──────────
#      （还原表只有英文卡面，中文没法直接锚点对齐 —— 这条对应表就是替代方案）
ZH2EN = {
    "[攻击]": ["[Attack]", "[attack]", "[Might]", "[Strength]", "[strength]", "[Power]",
                    "[fist]", "[weapon]", "[Health]", "[health]"],
    "[护甲]": ["[Armor]", "[Armour]", "[armor]", "[health]"],
    "[装甲]": ["[Armor]", "[Armour]", "[armor]"],
    "[生命]": ["[Health]", "[health]"], "[生命值]": ["[health]", "[Health]"],
    "[力量]": ["[Might]"], "[能量]": ["[Energy]"],
    "[无敌]": ["[Invulnerable]"], "[护盾]": ["[Shield]"],
    "[拳头]": ["[fist]"], "[骷髅头]": ["[skull]"],
    "[武器]": ["[weapon]"], "[践踏]": ["[Stomp]"], "[群体]": ["[Mob]"],
    "[毁灭者]": ["[Destroyer]"], "[斩杀]": ["[Slay]"],
    "[翅膀]": ["[Wings]"], "[远程]": ["[Ranged]"],
    # 🆕 2026-10-18：③ 那一批「图标 + 词」的中文记号（照现有约定写：`[Shield] Shield` /
    #    `[Shield] 护盾` 两半都要在，见 `TAU49` / `GOF_Worst_Temper`）：
    #    · `[黑暗契约]` —— BL51 的英文对家是 `[Dark Pact]`、BL69 的是 `[Chaos]`（两张都有逐卡条目）
    #      ⇒ 两个都列，按顺序试。
    #    · `[典籍]` —— UM_Avenging_Zeal 的英文对家是 `[Codex icon]`（`Death from Above` 用的是 `[Codex]`）。
    "[黑暗契约]": ["[Dark Pact]", "[Chaos]"],
    "[典籍]": ["[Codex icon]", "[Codex]"],
}


# 锚点核对的**已知错位**（逐条核过：表里的答案另有还原表原文/卡图证据，锚点这边是被相邻的
# 「图标 + 词」或更早的同形文字带偏了）。放在这里是为了让报告只剩**新出现**的不一致。
# ⚠️ 2026-09-15：`("DA5", "[Shield]")` 那条已随 token 改名换成 `[Quest Point]`
#    （卡表文字改过了，见 `TOKEN_BY_CARD` 里那两条的说明）—— 留着旧 token 就成了永远不会命中的死条目。
# 🔴 2026-10-12（`N2`）：`("DA75","[1]")` **已摘掉** —— 那个 token 已随 `desc` 改写（`[1]` → `1 Quest Point`）
#    从卡表里消失，留着就是永远不会命中的死条目（同族先例见本表上面那条 `("DA5","[Shield]")` 的注释）。
ANCHOR_NOISE = {("GOF88", "[health]"), ("6899b199f5e36144ebbdd7ed9d9e7afe", "[Slay]"), ("DA5", "[Quest Point]"),
                # 2026-09-16：`GOF87` 的 `[health]` 按卡面改名成 `[ranged]`（实为紫枪），
                # 而**锚点对这一枚一直给 `armour`** —— 那句话里紧邻的前一个图标是 `Armour 1.` 的银盾，
                # 锚点被它带偏了。判据是逐张开图（`Orks/3部队/Warpforge_13_Nob-on-Smasha-Squig.png`：
                # `+1〔拳〕 and +1〔枪〕`），所以按 `TOKEN_BY_CARD` 的答案，不按锚点。
                ("GOF88", "[ranged]"),
                # 🔴 2026-10-12（第二十一会话 · 写手 `C5` · `A1604` 的下游）：`("ASH35","[Invulnerable]")`。
                #    **卡面/卡池的答案没有疑问**：`ASH44 Wraithknight` 的 `desc` 就是
                #    `3 [Spirit Stone]: Gains [Invulnerable] Invulnerable until your next turn`,
                #    `[Invulnerable]` 这个记号**后面紧跟着印着那个词**（`Invulnerable`）⇒ 答案 = `invulnerable`。
                #    锚点为什么错（本条**实测复现过**，不是推测）：`find_icon_by_anchor` 的第三条兜底
                #    `(False, True)` 里 `p` 为空 ⇒ `j = 0` ⇒ 段是**从文本开头**切到后锚点，
                #    然后**先认 `⟦独立图标⟧`、有就返回最后一个**：
                #      段里 `⟦独立⟧` = `['Spirit Stone']`（`3 [❰Spirit Stone❱]` 那枚）
                #      段里 `❨图标+词❩` = `['armour','shuriken','invulnerable']` ← **正确答案在这里**
                #    ⇒ 被**更早的独立图标**带偏（这正是本表头那句「更早的同形文字带偏了」）。
                #    ⚠️ 试过「把段缩到后锚点前 MAX_CONTEXT 窗口」—— **实测无效**：那枚 `Spirit Stone`
                #    距后锚点只有 ~20 字符，缩到 38 字符的窗口它**还在**（缩到 20 才消失）。
                #    ⇒ 真要修得改成「**离后锚点最近的那一枚**（不分 ⟦⟧/❨❩）」= 换启发式，
                #      影响面不止这一张 ⇒ 本轮**不改**，只记进本表（账见交接）。
                ("ASH35", "[Invulnerable]")}


def dump_plan(plan):
    """落地形状 = `{"cards":[{"id","fields":[{"name","items":[{"token","sprite","why"}]}]}]}`。

    ⚠️ **别改成字典套字典** —— 运行时是 C# 用 `JsonUtility` 读的，它不支持字典。
    """
    cards = []
    for cid, fields in plan.items():
        fs = []
        for fname, got in fields.items():
            items = [{"token": k, "sprite": sp, "why": w}
                     for k, lst in got.items() for sp, w in lst]
            fs.append({"name": fname, "items": items})
        cards.append({"id": cid, "fields": fs})
    return cards


def _A1707_IN_PROPER_NOUN(text, word):
    """🆕 2026-10-13（`A1707`/`A1717`）：这个词在**本字段里**有哪一处**后面紧跟另一个首字母大写的词**吗？

    = 它有一次是某个**多词专有名词**的一部分（`Shield Generator` / `Stealth Drone`）⇒ 卡面那一处
    **只印词、不印徽记** ⇒ 按词规则**不许**在这个字段上画（守卫④，见 `main()` 里那一段）。

    ⚠️ **判据形状 = 「同一个词后面紧跟【同一行内】另一个 `[A-Z][a-z]` 词」**（`[ \\t]+`，⛔ **不是任意空白**）——
    全池实测（`BARE_TOKEN_BY_RULE` 的 token × 1126 张 × 2 字段）满足它的**只有 3 处**：
    · `TAU42` `Talent: Shield Generator`（`A1707`，**改前真多画 1 枚**）
    · `TAU33` `Companion 2: Stealth Drone`（本表 ② 那条反例，当时靠守卫③躲开）
    · `UM_Antaro_Chronus` `give it Flank` **换行** `Talent: Spear of Macragge` —— 🔴 **这一处是【假阳性】**，
      `Flank` 那里是**真关键词**（换行只是段界）⇒ **判据必须是 `[ \\t]+`、不能含换行**
      （本批实测踩过：写成含换行的空白类时 `Flank` 的规则命中 47→46、**静默少画一枚**）。
    ⚠️ **判「有任意一处」而不是「每一处」**：运行时的 `Rewrite` 是**全局** `s.Replace(token, tag)`
    ⇒ 一个 token 会把本字段**所有**出现一起换掉，**没法只画其中的关键词那一处**。
    真出现「同一个词这里该画、那里不该画」的卡，**要给它换成带上下文的逐卡 token** ——
    先例 = `AM29 Primaris Psyker` 的 `Shield to`（`_A1613_GAP` 那一块），那是**本仓既定做法**。
    ⚠️ **调用点之前没有匹配对象可用**：那条规则循环写的是 `if _t not in text`（**纯子串判据、没有 `m`**）
    ⇒ 这里自己 `finditer`；⛔ **别拿循环外残留的 `m`** —— 那会变成「看上一轮循环绑到谁」的**静默 bug**
    （本批实测踩过：`Shield` 那一处当场没生效，而 `Stealth` 那一处**碰巧**生效了）。
    """
    return any(re.match(r"[ \t]+[A-Z][a-z]", text[m.end():])
               for m in re.finditer(re.escape(word), text))


# ══════════════════════════════════════════════════════════════════════════════════════
# 🆕 2026-10-13（`A1703`）：**逐词图标表（三分）+ 一条机械断言** ——「防漂移」，不是「改实现」
# ══════════════════════════════════════════════════════════════════════════════════════
# **它解决什么问题**
#   原版那两个开关（`CardTrait._hasIcon` / `_isInPlayIcon`）**本地取不到**（`A1612`）⇒ 我们今天是拿
#   「**按词规则**」顶的（`BARE_TOKEN_BY_RULE` + `KEYWORD_SCAN`；`Core/Badges.cs:318-346` 如实标注
#   「这不是原版读数」）。而**按词规则唯一合法的形状**是：该词**恒带**图标。
#   若某个词「有时带、有时不带」，按词规则**一定**在某个位置多画一枚。
#
# **判据（本表的来源，⛔ 别推翻重查）**
#   = `资料/卡表核对_卡图提取/_还原效果文字.md`（29 个子代理 2026-09-13 逐张开原版卡图抄的）
#     的**双写**：卡面写「图标 + 词」⇒ 转录里就是 `Word Word` / `WordWord`
#     （`工具/restore_card_icons.py` 的占位符 → 词）。**同一个词连着出现两次 = 那一处印了图标。**
#   ⚠️ 「双写」有**三种写法**，检出器必须都认（2026-10-13 补全；判据与实现见下面
#      `A1703_DOUBLE` / `A1703_DOUBLE_TIGHT` / `_a1703_normalize` 那三段的注释）：
#      ① 单 token 对折 `FlyingFlying` · ② 有空格 `Stun Stun` · ③ **无空格多词** `Dark PactDark Pact`。
#   ⇒ 逐词算 `occ`（转录里该词出现次数）/ `ico`（落在双写区间里的次数）：
#     `ico == occ` ⇒ **恒带**（可写成按词规则）· `ico == 0` ⇒ **从不带图标** ·
#     其余 ⇒ **不是恒带，必须逐卡定**（每种恰好 1 张反例）。
#
# 🔴 **本件的定位 = 防漂移，不是「改实现」**（现状基本对）：
#   ① **卡表 / 转录表一改** ⇒ 三分结果变 ⇒ 当场红（下面 `a1703_classify()` 现场重算，与冻表逐词比）；
#   ② **有人把规则的触发位放宽**（例如从「句首 + 收尾符」改成「只要出现就画」）⇒ 那 11 个「不是恒带」
#      的词会立刻在**它们不该画的位置**冒出图标 ⇒ 由 `A1703_POSITION_LOCK` 当场红。
# ⚠️ **诚实边界**（⛔ 不许当已核过）：
#   · 本表的判据是**第二手的转录表**，不是逐张亲读的 1375 张卡图；转录表**已被证明至少有一处假阴性**
#     （`GOF90` 的 `Blood Thirst`，见上面 `_GOF90_BT_WHY`）⇒ **别拿本表去改卡表数据**。
#   · 「双写」检出器**检不出独立图标**（付费前缀 `2 [Spirit Stone]`、数值圈 `①`、拳/枪徽记）——
#     那些本来就不带词，走 `TOKEN_BY_RULE` / `BARE_TOKEN_BY_CARD`，**不在本表范围**。
#   · 只对**英文卡面**成立（原版没有中文表 ⇒ 中文那半是我们自己译的）。
#     🔴 **2026-10-14 更正（铁律 5）**：「**原版没有中文表**」**不成立**（原版**有**官方中文表）；
#        **判据全文与本文件的口径 → `_STUN_CARDS` 上方那条 `2026-10-14` 更正**（⛔ 别抄第二份）。
#        ⇒ **「只对英文卡面成立」【不变】**，理由换成「**本表的转录表只覆盖英文卡面**」。
#
# **① 恒带（46 种）⇒ 写成【按词规则】是合法的**（小写化的词；大小写两种写法在本表合并）
# 🔴 **2026-10-13：44 → 46** —— 检出器补上「无空格多词双写」那一支之后，下面两词**新进本类**
#    （它们本来就在 `A1703_EXTRA_VOCAB` 里，只是 `occ` 恒 0 ⇒ 连分类都进不去）：
#    · `dark pact` —— 转录 **38 处双写全是无空格写法** ⇒ 现算 `occ=ico=76`（76=38×2）、反例 0 张；
#    · `can't attack` —— `Concealed Explosives` 一处 `Can't AttackCan't Attack` ⇒ 现算 `occ=ico=2`。
#    ⚠️ **「恒带」是【转录表口径】**（转录里每次出现都在双写区间里），**不等于「卡面上没有任何例外」**：
#    `dark pact` 在**卡面**上还有 3 处那枚徽记由别的记号画（`❄` / `[Chaos]`，见 `A1623` 的
#    `DARKPACT_ALREADY`）—— 那是**画哪枚**的事，与本表「转录有没有标」不冲突。
A1703_ALWAYS = {
    "agenda", "artifice", "backlash", "blast", "camouflage", "can't attack", "concussive",
    "cruelty", "dark pact", "destroyer", "duty", "ecstasy", "ephemeral", "fast", "ferocity",
    "flank", "flying", "hunt mark", "invulnerable", "long range", "markerlight", "mob",
    "oath", "pack", "penitence", "pray", "rally", "regeneration", "regiment", "sabotage",
    "sentry", "shuriken", "slay", "sniper", "stimulation", "stomp", "stun", "swarm",
    "synapse", "teleport", "tide", "unstable", "uprising", "vanguard", "vulnerable",
    "waystone",
}
# **② 不是恒带（11 种 —— 每种恰好 1 张反例）⇒ 必须逐卡定，按词规则一定错。**
#    值 = **卡面只印词、没印图标**的那张卡的**转录表卡名**（不是我们的卡 id —— 这一半的判据是转录表本身）。
A1703_SOMETIMES = {
    "ambush":       {"Lhykhis"},              # `Euphoric Ambush`… 天赋名（`ASH_Lhykhis`）
    "armour":       {"Honour Guard"},         # `Adjacent units have Armour 1` —— 光环给别人的
    "blind":        {"Solar Pulse"},          # `Blind all enemies` —— 动词用法
    "blood thirst": {"Beastboss on Squigosaur"},  # 引号里的被授予能力（🔴 转录表这一处是**假阴性**）
    "codex":        {"Roboute Guilliman"},    # 天赋名 `Author of the Codex`
    "companion":    {"Commander O'Maisos"},   # 天赋名 `Drone Companion`
    "remnant":      {"Undying Legions"},      # 普通名词 `becomes a Remnant`
    "shield":       {"Riptide Battlesuit"},   # 天赋名 `Shield Generator`（= `A1707`）
    "stealth":      {"Stealth Shas'ui"},      # 单位名 `Stealth Drone`
    "strike":       {"Lord Kaphrael"},        # 天赋名 `Euphoric Strike`
    "talent":       {"Immortal Pride"},       # 普通名词 `your Warlords' Talent costs 1 less`
}
# **③ 从不带图标（9 个键 / 6 类）** —— 卡面只印词、一次都没印过图标。
#    ⚠️ 这 9 个键是**词形**去重后的（`Spirit Stone(s)` / `Quest Point(s)` / `Rage`+`relentless` 的大小写），
#       对应 2026-10-12 那张表的 6 类：`Melee` · `Ranged` · `faith` · `Spirit Stone` · `Quest Point` ·
#       `Rage`+`Relentless`。
A1703_NEVER = {
    "melee", "ranged", "faith", "spirit stone", "spirit stones",
    "quest point", "quest points", "rage", "relentless",
}
# 🔴 **转录表已知的假阴性（亲读更正）**：这几处的模型读数**不算数**，以**成品卡图**为准。
#    它们**仍然留在上面的三分里**（那一半比的是转录表与冻表，保持可复算）；
#    它们「我们该不该画」由 `A1703_MUST_PAINT` / `FACE_ICON_COUNT` 那一侧管。
A1703_READ_CORRECTED = {
    "blood thirst": "🔴 转录表 `_还原效果文字.md:628` **漏记了图标**（没写双写）⇒ 它把 `Blood Thirst` "
                    "算成「不是恒带、反例 = `Beastboss on Squigosaur`」。**卡面那一处其实印着**"
                    "（亲读 `Orks/3部队/Warpforge_16_Beastboss-on-Squigosaur.png`，裁图 "
                    "`_tmp_view/a1703_crops/MINE_GOF90_bt.png`）⇒ 我们**要画**（`A1713`，见 `A1703_MUST_PAINT`）。",
}
# **词表**（与 2026-10-12 那份复算脚本一致：`_tmp_view/a1703_audit4.py` 的 `EXTRA`）——
#    另加**盘上 78 张切片的名字**、以及**转录里所有双写短语**（见 `a1703_classify()`）。
A1703_EXTRA_VOCAB = ["Berzerk", "Frenzied", "Loot", "Rage", "Relentless", "Resolution",
                     "Mark of Chaos", "Mark of Khorne", "Mark of Nurgle", "Mark of Slaanesh",
                     "Mark of Tzeentch", "Can't Attack", "Dark Pact", "Dark Pacts",
                     "Blood Thirst", "Long Range", "Hunt Mark", "Quest Point", "Quest Points",
                     "Spirit Stone", "Spirit Stones"]
# 双写短语里的**虚词不算关键词**（`Rally and Rally` 那种不许进词表）
A1703_LANE = {"and", "to", "the", "a", "of", "in", "this", "it", "on", "its"}
# 双写 = 同一个短语连着出现两次（**有空格**）→ `\1` 回引
A1703_DOUBLE = re.compile(
    r"([A-Za-z][A-Za-z'\-]*(?: [A-Za-z][A-Za-z'\-]*){0,2})\s+\1\b", re.I)
# 🔴 2026-10-13 盲区修复（`A1703`）：**无空格**双写有两种，`A1703_DOUBLE` 只认有空格那一种。
#   ① 单 token 自身对折：`FlyingFlying`；
#   ② **多词词的对折**（本轮补的那一半）：`Dark PactDark Pact` —— 旧检出器**只做 ①**
#      （`_a1703_split_doubled` 要求整段就是一个词、且两半逐字相同 ⇒ `PactDark` 拆不开），
#      而 `A1703_DOUBLE` 的 `\s+` 又要求交界处有空白 ⇒ 这种写法**两种都漏**。
#   ⇒ 实测后果（`Dark Pact`）：转录表里 **38 处全是无空格双写、有空格写法 0 处** ⇒
#      `occ`/`ico` 恒为 0 ⇒ 该词**连分类都进不去**（`A1703_ALWAYS` 里挂着但它不在现算结果里）。
# ⚠️ **光"覆盖"还不够、必须在交界处补一个空格**：`\bdark pact\b` 在 `Dark PactDark Pact` 里
#    **一个匹配都没有**（第一份右边紧跟 `D`、第二份左边紧跟 `t`，两处 `\b` 都不成立）
#    ⇒ 只有还原成 `Dark Pact Dark Pact`，左右两份才各算一次 `occ`、且都落在双写区间里。
# 首词 `[A-Za-z][A-Za-z'\-]{2,}`（≥3 字符）是旧那个 `n >= 6` 护栏的等价物
# （双写串长 ≥6 ⟺ 单份 ≥3；实测本池最短的真双写是 `MobMob`，首词 3）。
# 实测坐标：`_还原效果文字.md:181` 等 `Chaos/*` 行，形如 `gain a Dark PactDark Pact of Fate`。
A1703_DOUBLE_TIGHT = re.compile(
    r"\b([A-Za-z][A-Za-z'\-]{2,}(?: [A-Za-z][A-Za-z'\-]*){0,2})\1", re.I)


def _a1703_split_doubled(tok):
    """**单 token** 无空格双写（`ArmourArmour`）→ 单份；不是双写返回 `None`。

    🔴 不拆的话 `\\bArmour\\b` 在 `ArmourArmour` 里**根本匹配不到**（右边紧随的是词字符）
    ⇒ 该词被系统性**少计**、分类直接错（原脚本 v1 就把 `Ambush` 判成了「只印词不带图标」）。
    ⚠️ **它只管「单 token」那一种**（多词那一种由 `A1703_DOUBLE_TIGHT` 管）——
    现在只有**灭自证那条臂**（`_a1703_normalize_legacy`）还调它，产线走 `A1703_DOUBLE_TIGHT`。
    """
    n = len(tok)
    if n >= 6 and n % 2 == 0 and tok[:n // 2].lower() == tok[n // 2:].lower():
        return tok[:n // 2]
    return None


def _a1703_normalize(text):
    """把**无空格**双写还原成「词 词」（交界处补空格），剩下的交给 `A1703_DOUBLE`。

    三种双写写法**都认**：
      ① 单 token 对折 `FlyingFlying` —— 本函数补空格（`A1703_DOUBLE_TIGHT` 的 `{0}` 那支）；
      ② 有空格 `Stun Stun` —— 本函数**不动它**，由 `A1703_DOUBLE` 认；
      ③ 无空格多词 `Dark PactDark Pact` —— 本函数补空格（`{0,2}` 那支，2026-10-13 补的盲区）。
    """
    return A1703_DOUBLE_TIGHT.sub(lambda m: m.group(1) + " " + m.group(1), text)


def _a1703_normalize_legacy(text):
    """**2026-10-13 改前那一版**（只认「单 token 对折」）—— ⛔ 产线**不走它**。

    留着只干一件事：给【灭自证】那条臂当**反例检出器** ——
    关掉多词那一支之后重算，**必须**与冻表不同（否则冻表里新加的两个词是白挂的）。
    """
    return re.sub(r"[A-Za-z][A-Za-z'\-]{5,}",
                  lambda m: (_a1703_split_doubled(m.group(0)) + " " +
                             _a1703_split_doubled(m.group(0)))
                  if _a1703_split_doubled(m.group(0)) else m.group(0), text)


def a1703_classify(normalize=None):
    """🔴 **现场重算**全池逐词三分（恒带 / 不是恒带 / 从不带）—— 返回 `{词(小写): (类, 反例卡名集合)}`。

    **纯函数**（只读转录表 + 盘上切片名）⇒ 灭自证那一半可以拿它跑合成输入。
    词表 = `A1703_EXTRA_VOCAB` ∪ `Art/traits/*.png` 的名字 ∪ 转录里所有双写短语（去掉虚词）。
    `normalize` 缺省 = `_a1703_normalize`（三种双写写法都认）；只有【灭自证】那条臂会传
    `_a1703_normalize_legacy`（改前那一版，用来证明新加的那两支**真的在起作用**）。
    """
    norm = normalize or _a1703_normalize
    rows = []
    with open(RESTORED, encoding="utf-8") as f:
        for ln in f:
            if not ln.startswith("|") or ln.startswith("| ---") or ln.startswith("| 文件名"):
                continue
            p = [x.strip() for x in ln.strip().strip("|").split("|")]
            if len(p) >= 4 and p[1]:
                rows.append((p[1], norm(p[2])))
    vocab = set(A1703_EXTRA_VOCAB) | set(trait_names())
    for _, text in rows:
        for m in A1703_DOUBLE.finditer(text):
            ph = m.group(1).strip()
            if ph and ph.lower() not in A1703_LANE:
                vocab.add(ph)
    acc = {}
    for w in sorted(vocab):
        rx = re.compile(r"\b" + re.escape(w) + r"\b", re.I)
        key = w.lower()
        occ = ico = 0
        plain = set()
        for name, text in rows:
            cov = set()
            for m in A1703_DOUBLE.finditer(text):
                cov.update(range(m.start(), m.end()))
            for m in rx.finditer(text):
                occ += 1
                if m.start() in cov:
                    ico += 1
                else:
                    plain.add(name)
        if occ:
            cur = acc.setdefault(key, [0, 0, set()])
            cur[0] += occ
            cur[1] += ico
            cur[2] |= plain
    out = {}
    for w, (occ, ico, plain) in acc.items():
        cls = "恒带" if ico == occ else ("从不带" if ico == 0 else "不是恒带")
        out[w] = (cls, frozenset(plain))
    return out


def _a1703_cmp(got, always, sometimes, never):
    """冻表 vs 现算 —— 返回**差异清单**（`[]` = 逐词相同）。**纯函数**（灭自证那一半要拿它跑合成表）。"""
    diffs = []
    ga = {w for w, (c, _p) in got.items() if c == "恒带"}
    gn = {w for w, (c, _p) in got.items() if c == "从不带"}
    gs = {w: set(p) for w, (c, p) in got.items() if c == "不是恒带"}
    for w in sorted(ga - set(always)):
        diffs.append(f"多出恒带词 {w!r}")
    for w in sorted(set(always) - ga):
        diffs.append(f"少了恒带词 {w!r}（现算 = {got.get(w, ('缺',))[0]}）")
    for w in sorted(gn - set(never)):
        diffs.append(f"多出从不带词 {w!r}")
    for w in sorted(set(never) - gn):
        diffs.append(f"少了从不带词 {w!r}（现算 = {got.get(w, ('缺',))[0]}）")
    for w in sorted(set(sometimes) - set(gs)):
        diffs.append(f"少了「不是恒带」词 {w!r}（现算 = {got.get(w, ('缺',))[0]}）")
    for w in sorted(set(gs) - set(sometimes)):
        diffs.append(f"多出「不是恒带」词 {w!r}")
    for w in sorted(set(sometimes) & set(gs)):
        if gs[w] != set(sometimes[w]):
            diffs.append(f"{w!r} 的反例卡集变了：现算 {sorted(gs[w])} / 冻表 {sorted(sometimes[w])}")
    return diffs


# ── `A1703` 第二半：**位置锁** ——「这些 (卡, 字段, 裸 token) 在计划里必须【不在】/ 【在】」──────
# 🔴 **为什么要有这一半**：上面那三分比的是**转录表与冻表**，它抓不到「有人把规则的触发位放宽」
#    （那种改动**不碰转录表**）。这一半把**成品卡图**判过的那几个位置直接钉在**计划表**上：
#    这 11 个「不是恒带」的词，在它们**卡面没印徽记**的那一处，计划里**一个裸 token 都不许有**。
#    实测（改前）：**12 行里 11 行已经成立、`TAU42` 那一行真红**（= `A1707` 的缺陷）。
#    改后 12 行全绿 ⇒ 这条锁**有判别力、不是恒绿**。
# 列 = (卡 id, 字段, **不许**出现的裸 token, 判据)
A1703_POSITION_LOCK = [
    ("TAU36", "desc", "Shield", "天赋名 `Shield Generator` 里的 `Shield` —— 卡面 "
                                "`Tau/3部队/Warpforge_42_Riptide-Battlesuit.png` 亲读："
                                "`〔盾〕Shield. 〔准星〕Long Range. 〔卷轴〕Talent: Shield Generator`，"
                                "**`Shield Generator` 那里没有盾徽**（`A1707`；裁图 "
                                "`_tmp_view/a1703_crops/Z2_TAU42.png`）"),
    ("TAU26", "desc", "Stealth", "单位名 `Stealth Drone` 里的 `Stealth`（`Companion 2: Stealth Drone`）"
                                 "—— 卡面那处只印词（转录表也同判：反例卡 `Stealth Shas'ui`）"),
    ("UM87", "desc", "Codex",
     "天赋名 `Author of the Codex` 里的 `Codex` —— 卡面那处只印词（`A1717`）"),
    ("SAU66", "desc", "Destroyer",
     "`Your other Destroyers` 里的 `Destroyers` —— 卡面那处只印词，且 `Destroyer` ⊂ `Destroyers`"
     "（按词规则会画到词中间）（`A1717`）"),
    ("GOF73", "desc", "Mob",
     "`[Mob] Mob abilities` 里的 `Mob` —— 卡面那一枚由 `[Mob]` 方括号记号画，后面那个 `Mob` 只印词"
     "（守卫①挡住的；`A1717`）"),
    ("UM14", "desc", "Armour", "`Adjacent units have Armour 1` —— **光环给别人的**，不是自己的关键词"),
    ("SAU55", "desc", "Talent", "`your Warlords' Talent costs 1 less` —— **普通名词**（同一张卡的 `Armour` 照画）"),
    ("SAU53", "desc", "Remnant", "`when a friendly troop becomes a Remnant` —— **普通名词**（同卡的 `Shield` 照画）"),
    ("SAU43", "desc", "Blind", "`Blind all enemies` —— **动词用法**（同卡的 `Stealth`/`Camouflage` 照画）"),
    ("ASH76", "desc", "Ambush", "天赋名 `Empyric Ambush` 里的 `Ambush`"),
    ("EC56", "desc", "Strike", "天赋名 `Euphoric Strike` 里的 `Strike`"),
    ("TAU3", "desc", "Companion", "天赋名 `Drone Companion` 里的 `Companion`"),
]
# **反向锁**：这几处**必须画**（卡面印着徽记、而我们的规则/逐卡表**扫不到那个位置**）。
# 🔴 `GOF90` 那一行就是 `A1713` 的缺陷 —— 改前它**不在**计划里（所以这条锁改前是红的）。
A1703_MUST_PAINT = [
    ("GOF91", "desc", "Blood Thirst",
     "`have \"Slay: Gain Blood Thirst this turn\"` —— 引号里的被授予能力（**句中**，句首那一支扫不到），"
     "卡面印着那枚獠牙圆徽（亲读 `Orks/3部队/Warpforge_16_Beastboss-on-Squigosaur.png`）"),
    ("GOF91", "descZh", "嗜血", "同上那半（`descZh` 写 `嗜血`）"),
]


# ── 🆕 2026-10-15：**官方记号那一代**（`[[…]]` / `((…))` / `$$…$$`）⇒ 本计划表**管不着那些字段** ──
# 判据 = `MyGame/Assets/CardPresentation/Core/CardIcons.cs:227`
#   `if (HasOfficialMarks(text)) return RewriteMarks(text, field == DescZhField);`
#   + 同文件 `:448-453` 的 `HasOfficialMarks` 判的就是 `[[` / `((` / `$$` 这三个前缀。
# ⇒ 带官方记号时 `CardIcons.Rewrite` **在 `HasOfficialMarks` 那一句就分流走了**、**根本不查本表**。
#   由此两条口径：
#   ① **生产侧整字段跳过**。不跳的后果（改前实测）：`TOK` 在 `[[waystone]]` 的**位置 0 匹配不上**
#      （下一个字符是 `[`、被字符类排除）、**位置 1 匹配成功** ⇒ 读出的 token 是 `waystone` ——
#      全池 **558 张卡的 `descZh`** 都是这一代写法，`没认出来: 952` 处里**绝大多数**是这么来的。
#   ② **自检侧**：凡是「**拿本表去数某字段画了几枚**」的判据，对这类字段一律是**载体不适用**
#      （不是期望值写错）⇒ **那一行/那一半跳过**。⛔ 不许去改期望数字、更不许换成别的卡。
# ⚠️ **`desc` 侧今天 0 张带记号**（实测：558 张带官方记号的卡**全在 `descZh`**）⇒ 本跳只影响中文那半。
def has_official_marks(text):
    """`CardIcons.HasOfficialMarks` 的 python 对家（`Core/CardIcons.cs:448-453`）。"""
    return ("[[" in text) or ("((" in text) or ("$$" in text)


def _a1703_lock_check(tokens, texts):
    """位置锁：`tokens = {卡: {字段: [token,…]}}`、`texts = {卡: {字段: 原文}}`。

    返回差异清单（`[]` = 全过）。**纯函数**（灭自证那一半要拿**合成表**跑它）。
    ⚠️ **先判「载体还在不在」**：卡被改名 / 那个字段被清空时，锁会**静默失效**
    （「不许出现」那一类尤其明显 —— 字段空了什么都不会出现）⇒ 载体不在 = 红，不是绿。
    🆕 2026-10-15：**再判「载体适不适用」** —— 字段带**官方记号**时，这一份字段由
    `CardIcons.RewriteMarks` 画、**本计划表根本不被查**（见 `has_official_marks` 那段）
    ⇒ 这条锁对该字段**不适用**，**整行跳过**（⛔ 不是「红」「绿」两种，是第三种「不适用」）。
    """
    diffs = []
    for c_, f_, t_, why in A1703_POSITION_LOCK:
        if not (texts.get(c_, {}).get(f_)):
            diffs.append(f"锁的载体不在：{c_} 没有字段 {f_} 的原文（卡被改名 / 字段被清空？）")
            continue
        if has_official_marks(texts.get(c_, {}).get(f_)):
            continue
        if t_ in tokens.get(c_, {}).get(f_, []):
            diffs.append(f"🔴 {c_} [{f_}] 画了裸词 {t_!r} —— 卡面那一处**没印徽记**：{why}")
    for c_, f_, t_, why in A1703_MUST_PAINT:
        if not (texts.get(c_, {}).get(f_)):
            diffs.append(f"必须画的锁载体不在：{c_} 没有字段 {f_} 的原文")
            continue
        if has_official_marks(texts.get(c_, {}).get(f_)):
            continue
        if t_ not in tokens.get(c_, {}).get(f_, []):
            diffs.append(f"🔴 {c_} [{f_}] 少了裸词 {t_!r} —— 卡面那一处**印着徽记**：{why}")
    return diffs


def main():
    restored = load_restored()
    vocab = sorted(set(trait_names()) | set(EXTRA_VOCAB), key=len, reverse=True)
    # ⚠️ 前面那条 `(?<![A-Za-z0-9])` 是必须的：`Invulnerable` 里**含着** `vulnerable`
    #    （实测没这条守卫时，还原表里的 `Invulnerable` 会被同时标成 vulnerable，锚点核对因此误报）
    # ⚠️ 大小写不敏感 + 守卫**只挡字母**：
    #    · 不敏感是因为还原表写 `Flying`/`Melee`，而词表来自文件名（`flying.png` 小写）
    #    · 守卫挡字母是必须的（`Invulnerable` 里含着 `vulnerable`），
    #      但**不能连数字一起挡** —— 还原表把数字贴着图标写（`have +2Melee`），
    #      挡数字会让这些位置一个标记都不打（实测 EC40 因此白报一次假阳性）
    CANON.update({v.lower(): v for v in vocab})
    # 关键词前缀用：图集名 → 去非字母小写（`bloodThirst` → `bloodthirst`、`Hunt Mark` → `huntmark`）
    KWIDX = {norm_name(v): v for v in vocab}
    ZH_KW = load_zh_keywords()
    pat = re.compile("(?<![A-Za-z])(" + "|".join(re.escape(v) for v in vocab) + ")",
                     re.IGNORECASE)
    with open(CARDS, encoding="utf-8") as f:
        cards = json.load(f)["cards"]

    TOK = re.compile(r"\[([^\[\]]{1,24})\]")
    plan, stat, unresolved, disagree, auto = {}, collections.Counter(), [], [], []
    # 🆕 2026-10-11（`A1476`）：**同一个 token 被两条来源画到、而 sprite 不一致**（真分歧）——
    #   今天应为 0 条；非 0 就是「按名字那条」和「逐卡证据那条」打架 ⇒ 要人判，⛔ 别静默。
    why_conflict = []
    # `A1378` 按词裸 token 规则的逐词读数（token → 分档 → 卡 id 集合），最后打出来
    rule_audit = collections.defaultdict(lambda: collections.defaultdict(set))
    # `A1411`：③「中文走英文对家（`ZH2EN`）」那一支**真被查到**的键 —— 它们**不是死条目**
    # （token 不在文本里是**设计如此**：它的用途就是给 `descZh` 当桥）
    zh_bridge_used = set()
    # 🆕 2026-10-12（`A1624`）：真的被 `PREFIX_ABSORB` 让位掉的段首 token（`(卡, 字段, token, 接手 token, sprite)`）
    #  —— 表里写了、这里没出现的 = **死条目**（卡表改了），下面自检会报出来。
    _absorbed = []
    # 🆕 2026-10-12（`A1623`）：真的被 `DARKPACT_ALREADY` 挡下来的 `(卡, 字段)` —— 表里写了、这里没出现的
    #  = **死条目**（本字段其实没有别的记号在画那一枚）⇒ rc=1。
    _dp_already_seen = set()

    def anchor_answer(name, text, m):
        """锚点对齐（交叉核对 / 兜底）。返回 (sprite, 说明)。"""
        ann = restored.get(norm_name(name))
        if ann is None:
            return None, "还原表里没有这张卡"
        sprite, why = find_icon_by_anchor(annotate(ann, pat),
                                          text[max(0, m.start() - 40):m.start()],
                                          text[m.end():m.end() + 40])
        if sprite:
            sprite = ALIAS.get(sprite, sprite)
        return sprite, why

    for c in cards:
        cid, name = c["id"], c["name"]
        entry = {}
        by_field = {}
        for field in ("desc", "descZh"):
            text = c.get(field) or ""
            # ── 🆕 2026-10-15：**官方记号那一代 ⇒ 整字段跳过**（判据见 `has_official_marks` 上面那一段）──
            #   `CardIcons.cs:227` 在 `HasOfficialMarks` 就分流到 `RewriteMarks`、**不查本表**；
            #   不跳的话 `TOK` 会把 `[[waystone]]` 读成 token `waystone`（位置 1 匹配）⇒ 整族炸。
            if has_official_marks(text):
                stat["跳过的字段（官方记号）"] += 1
                continue
            toks = list(TOK.finditer(text))
            syms = [ch for ch in text if ch in SYMBOLS]
            # 句首「关键词 + :/.」——`desc` 是 OCR 的、这些图标没被记下来，按名字补（见 KEYWORD_PREFIX）
            kws = keyword_prefixes(text, KWIDX, ZH_KW, [(m.start(), m.end()) for m in toks])
            # ── 🆕 2026-10-12（`A1624`）：**段首那条让位给同词的裸 token**（见 `PREFIX_ABSORB` 那段注释）。
            #    ⚠️ **必须在这里**（`btok` / `_pre_tokens` **之前**）：
            #      · 摘掉段首那条之后，`_pre_tokens` 里就没有它了 ⇒ **守卫③ 不再整字段挡那个词**
            #        （`GOF34` 的 `Stealth` / `SW37` 的 `Pack` / `TAU14` 的 `Stealth` / `TAU20` 的 `Long Range`
            #         都是靠这一步才落进计划表的）；
            #      · 摘在 `got` 组装之后 = 那条**照样进计划表**（本表就白写了）。
            #    ⚠️ 只摘**这一条**、不摘别条：段首那枚图标的位置由接手的裸 token 负责，**一个 token 画两处**。
            _kws_keep = []
            for _kt, _ksp, _kw in kws:
                if (cid, field, _kt) in PREFIX_ABSORB:
                    _absorbed.append((cid, field, _kt, PREFIX_ABSORB[(cid, field, _kt)], _ksp))
                    continue
                _kws_keep.append((_kt, _ksp, _kw))
            kws = _kws_keep
            # 不带方括号的资源词（`Gain 1 Quest Point` / 「获得 1 点任务」）—— 见 RESOURCE_WHY
            res = resource_tokens(text, [(m.start(), m.end()) for m in toks])
            # 按卡的**裸 token**（见 `BARE_TOKEN_BY_CARD`）—— 必须在**下面那道 `continue` 之前**算出来。
            # 🔴 2026-10-10（`A1366`）实测踩到：`DA39` 的 `desc` 只有 `Stun an enemy`（`Flying`/`Rally`
            #    在 `keywords` 数组里、不在 `desc` 里）⇒ 这份字段的 `toks/syms/kws/res` **全空**、
            #    被那道 `continue` **整份跳过** ⇒ 裸 token 表**根本没被查**。
            #    后果是**静默的**：`Stun` 那 30 张里 **15 张**一条都没补上（另 15 张字段里正好还有别的记号）。
            #    ⇒ 把「本字段有没有裸 token 命中」并进那道判据 —— 它本来就是「这份字段一条记号都没有」。
            # ⚠️ `_t in text` 让**同一张表同时管 `desc` 与 `descZh`**（英文 token 只在 `desc` 里命中、
            #    中文 token 只在 `descZh` 里命中），别按字段再写一列。
            btok = [(_t, _sp, _why, "按卡裸 token")
                    for (_bcid, _t), (_sp, _why) in BARE_TOKEN_BY_CARD.items()
                    if _bcid == cid and _t in text]
            # ── 🆕 2026-10-10（`A1378`）：按词的裸 token 规则（见 `BARE_TOKEN_BY_RULE` 那段注释）。
            #    逐卡表在前（判据更细、赢），规则在后（只补逐卡表没覆盖的）—— 两道守卫见那张表。
            #    ⚠️ 守恒带词的每一处都已在逐卡表里 ⇒ 本表**今天的池子上一条都不新增**（这是预期）。
            _have_sp = {_sp for _t, _sp, _w, _k in btok if _sp}
            # 守卫③ 用的名单 = **本字段已经注定要画的那几条 token**（句首关键词 / 资源词 / 逐卡裸 token）：
            #   裸词规则给的词只要落在其中任何一条**里面**，就是「一条是另一条的子串」那个形状。
            _pre_tokens = ([_x for _x, _s, _w in kws] + [_x for _x, _s, _w in res] +
                           [_x for _x, _s, _w, _k in btok])
            for _t, _sp, _why in BARE_TOKEN_BY_RULE:
                if _t not in text:
                    continue
                rule_audit[_t]["全池出现"].add(cid)
                if _sp in _have_sp:                  # 守卫②：本卡已有同 sprite 的逐卡裸 token（判据更细）
                    rule_audit[_t]["逐卡表已覆盖"].add(cid)
                    continue
                if ("[" + _t + "]") in text:         # 守卫①：图标已由方括号那一支画
                    rule_audit[_t]["方括号守卫"].add(cid)
                    continue
                # ── 🆕 2026-10-12（`A1623`）：**`Dark Pact` 一族的两处例外**（`DARKPACT_ALREADY`）──
                #   本字段那一枚徽记**已经由另一条记号画了**（`BL19` 的 `❄` / `BL69` 的 `[Chaos]`）
                #   ⇒ 规则再画一次就是**同一处两枚**。
                #   ⚠️ **判据是「卡面那一格被谁占了」，不是位置的算术** —— 中文 `BL19` 里
                #     `❄` 与「黑暗契约」之间**还夹着一个「纵欲」**（`❄纵欲黑暗契约`），
                #     所以「紧邻才让位」那种写法在这一格上会漏判（本批第一版就是那么写的，当场推翻）。
                if _t in DARKPACT_TOKENS and (cid, field) in DARKPACT_ALREADY:
                    rule_audit[_t]["已由别的记号画"].add(cid)
                    _dp_already_seen.add((cid, field))
                    continue
                # 🆕 **守卫③（2026-10-11 · `A1476` 收尾符放宽之后新增）：别和「句首关键词」那一支重叠。**
                #   `A1476` 把收尾符放宽到「或文末」之后，`Sniper.` 这类**带收尾符**的 token 开始被
                #   句首那一支画出来；而同一条裸词规则给的 token 是 `Sniper` —— 它是 `Sniper.` 的**子串**。
                #   🔴 **运行时的替换是全局的**（`CardIcons.Rewrite` 的 `s.Replace(token, tag)`）⇒
                #   先换长的 `Sniper.`（本函数头那条「长的先换」），再换短的 `Sniper` 时，
                #   它会命中**刚插进去的那段标签里的词**（`…<sprite name="sniper">Sniper.</nobr>`）
                #   ⇒ **同一个图标画两枚、`<link>` 套两层**（静默）。
                #   实测：放宽当天 `ASH81` / `SW29` 的 `desc`+`descZh` 共 **4 条**，全池其余 0 条。
                if any(_t in _x for _x in _pre_tokens):
                    rule_audit[_t]["已由句首关键词画"].add(cid)
                    continue
                # ── 🆕 2026-10-13（`A1707` / `A1717`）：**守卫④ —— 词落在【专有名词内部】⇒ 不画。** ──
                # 🔴 **它抓的是什么**：卡面上「图标 + 关键词」那个写法只在**关键词**上出现；
                #    而这些词**当专有名词的一部分**用时（天赋名 `Shield Generator` / 单位名
                #    `Stealth Drone` / …），卡面**只印词、不印徽记** ⇒ 按词规则会在那里**多画一枚**。
                #    判据形状 = **这一处裸词后面【紧跟另一个首字母大写的词】** ⇒ 它属于某个多词专有名词。
                # ⚠️ **为什么这个形状是安全的（全池实测，不是感觉）**：把 `BARE_TOKEN_BY_RULE` 的
                #    **全部英文 token × 全部 1126 张 × 2 字段**逐处扫了一遍 —— **756 处里只有 2 处**
                #    后面跟着大写词，而且**两处都是已知的「卡面没印」反例**：
                #      · `TAU42` `desc`：`Talent: Shield Generator` 里的 `Shield`（`A1707`，**改前真多画 1 枚**）
                #      · `TAU33` `desc`：`Companion 2: Stealth Drone` 里的 `Stealth`（本表 ② 那条反例；
                #        它**当时**是靠守卫③（`Stealth` ⊂ `Stealth.`）躲开的，不是靠规则 ⇒ 现在两条都挡得住）
                #    ⇒ **误伤面 = 0**；中文 token 那边 0 处命中（中文词后面不会跟 `[A-Z][a-z]`）。
                # 🔴 **它不改任何既有结论**：改前/改后**只有 `TAU42` 一张卡的 `desc` 少一条 token**
                #    （实测 `git diff` 见报告），其余一字未动 —— 这正是「改前真缺陷、改后修掉」的读数。
                # ⚠️ **它【不覆盖】的两种情况**（如实记，别以为它管全了）：
                #    · `Mob abilities`（`GOF_Stomp_Em`）—— 后面跟的是**小写**名词 ⇒ 本守卫不命中；
                #      那一处是**守卫①**（本字段含 `[Mob]`）挡住的。两者都已进 `A1703_POSITION_LOCK`。
                #    · `Destroyers`（`SAU_Ramatekh_The_Cruel`）/ `Author of the Codex`
                #      （`UM87`）—— 前者是**更长词的子串**、后者那词**根本不在
                #      `BARE_TOKEN_BY_RULE`**（走逐卡表）⇒ 各自由守卫③ / 逐卡表兜住，也进了那条锁。
                if _A1707_IN_PROPER_NOUN(text, _t):
                    rule_audit[_t]["专有名词内部"].add(cid)
                    continue
                rule_audit[_t]["规则命中"].add(cid)
                btok.append((_t, _sp, _why, "按词规则"))
            if not toks and not syms and not kws and not res and not btok:
                continue
            got = {}
            for _tok, _sprite, _why in kws:
                if _tok in got:            # 同一个写法在本文里出现多次：只记一条（运行时是全局替换）
                    continue
                got[_tok] = [(_sprite, _why)]
                stat["关键词前缀"] += 1
            for _tok, _sprite, _why in res:
                if _tok in got:
                    continue
                got[_tok] = [(_sprite, _why)]
                stat["资源词"] += 1
            for m in toks:
                key = m.group(0)
                answer = None
                if (cid, key) in GAP_BY_CARD:                     # ⓪ 认得出、但原版就没认定 ⇒ 如实报
                    answer = ("", "缺口：" + GAP_BY_CARD[(cid, key)])
                    stat["缺口（未认定）"] += 1
                answer = answer or TOKEN_BY_CARD.get((cid, key))
                if answer:                                        # ① 逐卡表（有证据）
                    stat["逐卡表"] += 1
                    a = anchor_answer(name, text, m)
                    if a[0] and a[0] != answer[0] and (cid, key) not in ANCHOR_NOISE:
                        disagree.append((cid, name, field, key, answer[0], a[0], a[1]))
                else:
                    for tok, _u, sprite_t, why_t in TOKEN_BY_RULE:  # ② 按规则
                        if key == tok:
                            if "{n}" in sprite_t:
                                mm = re.search(r"(\d)\s*$", text[:m.start()])
                                n = mm.group(1) if mm else "1"
                                answer = (sprite_t.format(n=n), why_t + f"（本卡 n={n}）")
                            else:
                                answer = (sprite_t, why_t)
                            stat["按规则"] += 1
                            break
                if answer is None and field == "descZh":           # ③ 中文：走它的英文对家
                    for en_key in ZH2EN.get(key, []):
                        if (cid, en_key) in TOKEN_BY_CARD:
                            answer = TOKEN_BY_CARD[(cid, en_key)]
                            stat["中文对英文"] += 1
                            zh_bridge_used.add((cid, en_key))       # ← 这一条是**活桥**、不是死条目
                            break
                if answer is None and key in NO_SPRITE:             # ③b 认得出，但盘上没这张图
                    answer = ("", "缺口：" + NO_SPRITE[key])
                    stat["缺口（无素材）"] += 1
                if answer is None:                                 # ④ 锚点兜底
                    sprite, why = anchor_answer(name, text, m)
                    if sprite:
                        answer = (sprite, "⚠️ 锚点自动对齐（**没人工核**）：" + why)
                        stat["锚点自动对齐"] += 1
                        auto.append((cid, name, field, key, sprite, text[:90]))
                    else:
                        stat["🔴 没认出来"] += 1
                        unresolved.append((cid, name, field, key, text, why))
                        got.setdefault(key, []).append(("", why))
                        continue
                got.setdefault(key, []).append(answer)
            for ch in dict.fromkeys(syms):
                sprite, why = SYMBOLS[ch]
                got[ch] = [(sprite, why)]
                stat["符号"] += 1
            # 按卡的**裸 token**（见 `BARE_TOKEN_BY_CARD`）—— 卡面真印着的字，图标插在词前面。
            # 名单在进本字段时就算好了（`btok`，见上面那段 —— 它同时是那道 `continue` 的判据之一）。
            for _btok, _bsp, _bwhy, _bkind in btok:
                if _btok in got:
                    # 🆕 **2026-10-11（`A1476`）**：同一个 token 已经被**句首关键词**那一支画过时，
                    #   它的 `why` 是那条**按名字**的通用话术；而逐卡表 / 裸 token 那两条
                    #   **判据更细**（带 `_还原效果文字.md:NN` + 卡图文件名）⇒ **把 `why` 换成更细的那条**。
                    #   ⚠️ **只在 sprite 一致时换**（渲染结果一个字节都不变，只改证据链）——
                    #   **不一致就是真分歧**：那种情形**不静默**，记进下面的冲突段（`why_conflict`）。
                    _cur = got[_btok]
                    if len(_cur) == 1 and _cur[0][0] == _bsp:
                        got[_btok] = [(_bsp, _bwhy)]
                    elif len(_cur) == 1 and _bsp:
                        why_conflict.append((cid, field, _btok, _cur[0][0], _bsp, _bkind))
                    continue
                got[_btok] = [(_bsp, _bwhy)]
                stat[_bkind] += 1
            if got:
                by_field[field] = got
        if by_field:
            plan[cid] = by_field

    have = {os.path.splitext(f)[0] for f in os.listdir(TRAITS) if f.endswith(".png")} |            {os.path.splitext(f)[0] for f in os.listdir(UI) if f.endswith(".png")}
    missing = sorted({s for e in plan.values() for got in e.values() for lst in got.values()
                      for s, _w in lst if s and s not in have})

    print("=== 统计 ===")
    for k, v in stat.most_common():
        print(f"  {k}: {v}")
    print(f"  有记号的卡: {len(plan)}")
    print(f"  🔴 sprite 名盘上没有: {missing if missing else '无'}")
    print("")
    print(f"=== 锚点自动对齐的（{len(auto)} 处，**要人工核**）===")
    for cid, name, field, key, sprite, t in auto:
        print(f"  {cid} {name} [{field}] {key} → {sprite}   |  {t}")
    print("")
    print(f"=== 表里已定、锚点给出另一个答案（{len(disagree)} 处）===")
    for cid, name, field, key, a, b, why in disagree:
        print(f"  {cid} {name} [{field}] {key}：表里={a} / 锚点={b}（{why}）")
    print("")
    print(f"=== 没认出来的（{len(unresolved)} 处，**不猜**）===")
    for cid, name, field, key, text, why in unresolved:
        print(f"  {cid} {name} [{field}] {key} —— {why}  |  {text[:100]}")

    # ── 🆕 2026-10-11（`A1476`）：**同字段内「一条 token 是另一条的子串」自检** ──────────────
    #  判据：`CardIcons.Rewrite` 的替换是**全局** `s.Replace(token, tag)`，而它只保证
    #  「**同一条内**长的先换」—— **管不了跨条**。长短两条一旦有包含关系，先换长的那条之后，
    #  短的还会命中**刚插进去的那段标签里的那个词**（`…<nobr><sprite …>Sniper.</nobr>`）
    #  ⇒ **同一个图标画两枚、`<link>` 套两层**，而且**不报任何错**。
    #  🔴 这条自检是 `A1476` 放宽收尾符那天**踩出来的**（`Sniper` ⊂ `Sniper.`，`ASH81`/`SW29`
    #     的中英两侧共 4 条）⇒ 加进来让它**以后不可能静默**：今天必须是 **0 处**。
    #  ⚠️ 判据只看**不含 `[` 的键** —— 方括号记号那一支换完**词是不留的**（`[Mob]`），
    #     它和其他条不可能互为子串而不被发现；真出现也会在这里报出来。
    conflicts = []
    for _cid, _fields in plan.items():
        for _fname, _got in _fields.items():
            _keys = [k for k in _got.keys() if not k.startswith("[")]
            for _a in _keys:
                for _b in _keys:
                    if _a != _b and _a in _b:
                        conflicts.append((_cid, _fname, _a, _b))
    print("")
    print(f"=== 🔴 同字段内 token 互相包含（{len(conflicts)} 处；**运行时会画两枚**）===")
    for _cid, _fname, _a, _b in conflicts:
        print(f"  {_cid} [{_fname}] {_a!r} ⊂ {_b!r}")
    print("")
    print(f"=== 🔴 同一个 token 两条来源 sprite 不一致（{len(why_conflict)} 处；要人判）===")
    for _cid, _fname, _t, _a, _b, _k in why_conflict:
        print(f"  {_cid} [{_fname}] {_t!r}：句首关键词={_a} / {_k}={_b}")

    # ── 死条目（2026-09-16 加）──────────────────────────────────────────────
    #  `TOKEN_BY_CARD` 是 (卡, token) → sprite 的**逐卡**表。**改了某张卡的 token 名之后，
    #  旧键就永远不会命中** —— 本文件 2026-09-15 那条注释已经吃过一次（`DA8 [Shield]`）。
    #  死条目本身不改行为，但它**看起来还在干活**，下一个会话会以为那张卡的图标有依据。
    #  ⇒ 每次生成都把死条目列出来，让人看到「这条该换键了 / 该删了」，而不是等它咬人。
    texts = {}
    for c in cards:
        texts[c["id"]] = (c.get("desc") or "") + "\n" + (c.get("descZh") or "")
    # 🔴 2026-10-10（`A1411`）修一类**假阳性**：③「中文走英文对家」的**桥条目** token 故意不在文本里
    #    （它就是给 `descZh` 用的），原来会被算成死条目 —— 真要照着"清"就会**静默掉图标**。
    #    判据不用另写一份（另写一份就会跟 `main()` 漂移）：直接用循环里**真被查到**的那些键
    #    （`zh_bridge_used`，`AM50` 那条就是靠它认出来的）。
    dead = [(cid, tok) for (cid, tok) in list(TOKEN_BY_CARD) + list(BARE_TOKEN_BY_CARD)
            if tok not in texts.get(cid, "") and (cid, tok) not in zh_bridge_used]
    # 只报**被这条修法救下来**的（token 不在文本里、但真被当桥用了）——今天 0 条，
    # 留着是为了下次真出现时**看得见**（否则又会有人照着「死条目」清单把它清掉）。
    saved = sorted((cid, tok) for (cid, tok) in list(TOKEN_BY_CARD) + list(BARE_TOKEN_BY_CARD)
                   if tok not in texts.get(cid, "") and (cid, tok) in zh_bridge_used)
    print("")
    print(f"=== 逐卡表里的死条目（{len(dead)} 条，token 已不在该卡的 desc/descZh 里）===")
    for cid, tok in dead:
        print(f"  {cid} {tok}")
    print(f"  （另有 {len(saved)} 条**活桥**被这条判据排除 —— token 不在文本里、但本次真被 ③ `ZH2EN` 用到，"
          f"删了会**静默掉图标**：{saved if saved else '今天 0 条'}）")

    print("")
    print("=== 按词的裸 token 规则（`A1378`，逐词读数）===")
    for _t, _sp, _why in BARE_TOKEN_BY_RULE:
        _a = rule_audit[_t]
        _flag = "　🔴 全池 0 命中（token 写错了？）" if not _a["全池出现"] else ""
        print(f"  {_t} → {_sp}：规则命中 {len(_a['规则命中'])} 卡 · 逐卡表已覆盖 "
              f"{len(_a['逐卡表已覆盖'])} 卡 · 方括号守卫 {len(_a['方括号守卫'])} 卡 · "
              f"已由句首关键词画 {len(_a['已由句首关键词画'])} 卡 · "
              f"已由别的记号画 {len(_a['已由别的记号画'])} 卡 · "
              f"专有名词内部 {len(_a['专有名词内部'])} 卡 · "
              f"全池出现 {len(_a['全池出现'])} 卡{_flag}")

    # ── 🆕 2026-10-12（`A1624`）：段首让位 + 卡面枚数两道自检 ─────────────────────────────
    _abs_seen = {(c_, f_, t_) for c_, f_, t_, _k, _sp in _absorbed}
    _abs_dead, _abs_lost = [], []
    for _c, _f, _t, _k, _ln, _png in _PREFIX_ABSORB_ROWS:
        if (_c, _f, _t) not in _abs_seen:
            # 死条目：这份字段**今天没产出**这条段首 token（卡表改了 / 收尾符变了）⇒ 表里这一行不再生效
            _abs_dead.append((_c, _f, _t, _k))
        elif _k not in plan.get(_c, {}).get(_f, {}):
            # 🔴 更糟的一种：摘掉段首那条、接手的裸 token **又没落进计划** ⇒ 那一枚图标**整枚消失**
            _abs_lost.append((_c, _f, _t, _k))
    print("")
    print(f"=== 段首关键词让位给同词裸 token（`A1624`，{len(_absorbed)} 处）===")
    for _c, _f, _t, _k, _sp in _absorbed:
        print(f"  {_c} [{_f}] 让位 {_t!r} → 接手 {_k!r}（{_sp}）"
              f" —— {PREFIX_ABSORB_WHY.get((_c, _f), '')}")
    if _abs_dead:
        print(f"  🔴 死条目（{len(_abs_dead)} 条 —— 这份字段今天没产出这条段首 token，"
              f"表要跟着卡表改）：")
        for _c, _f, _t, _k in _abs_dead:
            print(f"     {_c} [{_f}] {_t!r} → {_k!r}")
    if _abs_lost:
        print(f"  🔴 **图标丢失**（{len(_abs_lost)} 条 —— 摘了段首那条、接手的裸 token 不在计划里）：")
        for _c, _f, _t, _k in _abs_lost:
            print(f"     {_c} [{_f}] {_t!r} → {_k!r}")

    # **卡面枚数**：期望值来自成品卡图（见 `FACE_ICON_COUNT` 的注释）。
    # ⚠️ 数法 = `Σ 每条 token 在原文里的出现次数`（`Rewrite` 是全局替换；token 之间互不包含由上面
    #    `conflicts` 自检保证）—— **不含** `KeywordSegment` 补的段首那几枚（那一半在判据里写明）。
    _texts = {c_["id"]: {f_: (c_.get(f_) or "") for f_ in ("desc", "descZh")} for c_ in cards}
    face_bad = []
    print("")
    print(f"=== 卡面枚数自检（`A1624`/`A1622`，{len(FACE_ICON_COUNT)} 行）===")
    _face_na = []
    for _c, _f, _want, _why in FACE_ICON_COUNT:
        _txt = _texts.get(_c, {}).get(_f, "")
        # 🆕 2026-10-15：本字段带**官方记号** ⇒ 它由 `CardIcons.RewriteMarks` 画、**本表不被查**
        #   ⇒ 这一行是**载体不适用**（第三种状态，⛔ 不是「改期望值」、更不是换张卡）。
        if has_official_marks(_txt):
            _face_na.append((_c, _f))
            print(f"  ⚪ {_c} [{_f}] **跳过**（本字段带官方记号 ⇒ 载体不适用，"
                  f"见 `CardIcons.cs:227`）—— 期望值 {_want} 枚是给 `RewriteMarks` 那一支的")
            continue
        _got = plan.get(_c, {}).get(_f, {})
        _n = sum(_txt.count(_t) for _t in _got)
        _ok = (_n == _want)
        if not _ok:
            face_bad.append((_c, _f, _want, _n))
        print(f"  {'✅' if _ok else '🔴'} {_c} [{_f}] 画 {_n} 枚 / 应画 {_want} 枚 —— {_why}")
    if _face_na:
        print(f"  ⚪ 另有 {len(_face_na)} 行**载体不适用**（字段带官方记号）："
              f"{'、'.join(f'{_c}[{_f}]' for _c, _f in _face_na)}")

    # ── 🆕 2026-10-12（`A1605`）：**信仰前缀写法统一**的两道自检（见 `LEGACY_FAITH_PREFIX` 那段）────
    #  ① **旧写法必须清零** —— 判据是「数字 + 那串方括号」这个**前缀形状**，全池扫 `desc`/`descZh`。
    #     改前在真数据上命中 **13 处**（就是本笔要改的那 13 个字段）⇒ **它绿是改出来的、不是恒绿**。
    #  ② **图标一枚都没丢** —— `FAITH_PREFIX_UNIFIED` 那 13 行逐行核：本字段 `☀→faith` 真的在计划里，
    #     且枚数 === 期望值。⚠️ 期望值**不是**从我们的实现里算的：改前/改后都是 1 枚（同一处、同一张图），
    #     出处见每行的卡图。
    #  ③ **灭自证**：把 `☀` 那条记号从计划里**抽掉**造一份合成表 ⇒ **13 行必须全部变红**
    #     （谓词若写成「恒等于期望值」这种自引用写法，这一格必红 —— 与 `A1623` 那一族同一形状）。
    _legacy = []
    for _c in cards:
        for _f in ("desc", "descZh"):
            _t = _c.get(_f) or ""
            for _m in LEGACY_FAITH_PREFIX.finditer(_t):
                _legacy.append((_c["id"], _f, _m.group(0), _t[:70]))
    print("")
    print(f"=== 信仰前缀写法统一（`A1605`；`☀` 之外的写法应为 0 处）===")
    if _legacy:
        for _cid, _f, _p, _t in _legacy:
            print(f"  🔴 {_cid} [{_f}] 旧写法 {_p!r} —— {_t}")
    else:
        print("  ✅ 0 处（`[faith]` / `[Faith]` / `[Faith Icon]` / `[Icon]` / `[icon]` / `[Energy]` / `[能量]`"
              " 一个都没有了）")

    def _fp_ok(_c, _f, _want, _plans):
        _e = _plans.get(_c, {}).get(_f, {}).get(SUN)
        if not _e or _e[0][0] != "faith":
            return False
        return (_texts.get(_c, {}).get(_f, "") or "").count(SUN) == _want

    _fp_stripped = {c_: {f_: {t: sp for t, sp in items.items() if t != SUN}
                         for f_, items in fs_.items()}
                    for c_, fs_ in plan.items()}
    _fp_bad, _fp_ctrl_missed = [], []
    print("")
    print(f"=== 统一过的（卡, 字段）仍画 `☀`→`faith`（`A1605`，{len(FAITH_PREFIX_UNIFIED)} 行）===")
    for _c, _f, _want, _why in FAITH_PREFIX_UNIFIED:
        _ok = _fp_ok(_c, _f, _want, plan)
        if not _ok:
            _fp_bad.append((_c, _f, _want,
                            (_texts.get(_c, {}).get(_f, "") or "").count(SUN),
                            (plan.get(_c, {}).get(_f, {}).get(SUN) or [("", "")])[0][0]))
        if _fp_ok(_c, _f, _want, _fp_stripped):
            _fp_ctrl_missed.append((_c, _f))
        print(f"  {'✅' if _ok else '🔴'} {_c:6s} [{_f}] 画 `☀`→"
              f"{(plan.get(_c, {}).get(_f, {}).get(SUN) or [('缺', '')])[0][0]}"
              f" × {(_texts.get(_c, {}).get(_f, '') or '').count(SUN)} 枚 / 应 {_want} 枚 —— {_why}")
    print(f"  ★ 灭自证：把计划里的 `☀` 那条记号**抽掉**之后再判 —— 应当**每一行都变红**："
          f"{len(FAITH_PREFIX_UNIFIED) - len(_fp_ctrl_missed)}/{len(FAITH_PREFIX_UNIFIED)} 行变红"
          + ("（✅ 无漏）" if not _fp_ctrl_missed else f"；🔴 **漏掉** {_fp_ctrl_missed}"))

    # ── 🆕 2026-10-12（`A1683`）：`ASH75` 的中文**一枚信仰都不许有**（见 `ASH75_FAITH_TOKENS` 那段）──
    #  ① 判据形状与 `LEGACY_FAITH_PREFIX` **不同**：那条要的是「数字 + 信仰词」的前缀形状（`①` 不是数字
    #     ⇒ 它抓不到 `ASH75`，改前就是绿的）；本条要的是「**这个字段里一枚信仰都不许有**」。
    #  ② **不许删过头**：`①`→`SpiritStone_1` 必须还在（症状会是「灵魂石那枚也没了」= 另一种静默错）。
    #  ③ **灭自证**：把 `①` 那条记号从计划里**抽掉** ⇒ 这一格必须变红（谓词写成自引用时它必红）。
    _a75_txt = _texts.get("851f73daf747eb642934a16429b391db", {}).get("descZh", "") or ""
    _a75_pl = plan.get("851f73daf747eb642934a16429b391db", {}).get("descZh", {})
    _a75_faith = sorted(t for t, v in _a75_pl.items() if (v[0][0] or "") == "faith")
    _a75_tok = sorted(t for t in ASH75_FAITH_TOKENS if t in _a75_txt)
    _a75_ss = (_a75_pl.get("①") or [("", "")])[0][0]
    _a75_miss = [w for w, ok in (("画了信仰", not _a75_faith),
                                 ("文本里有信仰记号", not _a75_tok),
                                 ("`①`→SpiritStone_1", _a75_ss == "SpiritStone_1")) if not ok]

    def _a75_ok(_plans, _txt):
        _e = _plans.get("851f73daf747eb642934a16429b391db", {}).get("descZh", {})
        if any((v[0][0] or "") == "faith" for v in _e.values()):
            return False
        if any(t in _txt for t in ASH75_FAITH_TOKENS):
            return False
        return (_e.get("①") or [("", "")])[0][0] == "SpiritStone_1" and _txt.count("①") == 1

    _a75_stripped = {c_: {f_: {t: sp for t, sp in items.items() if c_ != "851f73daf747eb642934a16429b391db" or t != "①"}
                          for f_, items in fs_.items()}
                     for c_, fs_ in plan.items()}
    _a75_ctrl = _a75_ok(_a75_stripped, _a75_txt)
    print("")
    print("=== `A1683`：`ASH75 Storm of Silence` 的中文**不许出现信仰**（那处是**灵魂石**）===")
    print(f"  {'✅' if not _a75_miss else '🔴'} `ASH75` [descZh] 画 faith {len(_a75_faith)} 枚"
          f"（{_a75_faith if _a75_faith else '无'}）· 文本里信仰记号 "
          f"{_a75_tok if _a75_tok else '0 处'} · `①`→{_a75_ss} × {_a75_txt.count('①')} 枚"
          + ("" if not _a75_miss else f" —— 🔴 {'、'.join(_a75_miss)}"))
    print(f"  ★ 灭自证：把计划里的 `①` 那条记号**抽掉**之后再判 —— 应当**变红**："
          + ("🔴 **没变红**（谓词是自引用的）" if _a75_ctrl else "✅ 变红（无漏）"))
    print("  判据 = 成品卡图亲读 `d:/2/Warpforge部队卡片/Aeldari/2天赋/"
          "Warpforge_02_Storm-of-Silence.png`（第 2 行 `〔绿圈·1〕〔橙金螺旋〕Stun …`，**没有金太阳**）")

    # ── 🆕 2026-10-12（`A1623`）：`Dark Pact` 一族的两道自检 ────────────────────────────────
    #  ① **例外表**（`DARKPACT_ALREADY`）—— 判据与自检**逐条照 `_PREFIX_ABSORB_ROWS` 那一族**：
    #     · 表里的每一行必须**真被用到**（`_dp_already_seen`）—— 不然它是「看起来在干活」的死条目；
    #     · 它点名的那枚记号必须**真在计划里** —— 不然摘掉规则之后**那一枚整枚消失**
    #       （比不改更糟：改前至少 `❄` / `[Chaos]` 那一枚是画着的）。
    #  ② **卡面枚数**（`DARKPACT_FACE_COUNT`）—— 期望值来自成品卡图 / 转录表双写，**不是**从实现里算的；
    #     数法 = `darkpact_marks()`（`Σ 每条 `DARKPACT_TOKENS` 的 token 在原文里的出现次数`）。
    #     🔴 **灭自证**：把这几条记号从计划里**抽掉**造一份合成表 ⇒ **每一行都必须变红**
    #     （谓词写成「恒等于期望值」这种自引用写法时，这一格必红 —— 与 `③c-b` / `④b` 同一形状）。
    _dp_dead = [(c_, f_, t_) for c_, f_, t_, _w in _DARKPACT_ALREADY_ROWS
                if (c_, f_) not in _dp_already_seen]
    _dp_lost = []
    for _c, _f, _t, _w in _DARKPACT_ALREADY_ROWS:
        _ent = plan.get(_c, {}).get(_f, {}).get(_t)
        _spr = _ent[0][0] if _ent else None
        if _spr not in DARKPACT_SPRITES:
            _dp_lost.append((_c, _f, _t, _spr))
    print("")
    print(f"=== `Dark Pact` 徽记的例外表（`A1623`，{len(DARKPACT_ALREADY)} 处）===")
    for _c, _f, _t, _w in _DARKPACT_ALREADY_ROWS:
        print(f"  {_c} [{_f}] 那一枚已由 {_t!r} 画 ⇒ 按词规则让位 —— {_w}")
    if _dp_dead:
        print(f"  🔴 死条目（{len(_dp_dead)} 条 —— 本字段今天没有产出可让位的记号，表要跟着卡表改）：")
        for _c, _f, _t in _dp_dead:
            print(f"     {_c} [{_f}] {_t!r}")
    if _dp_lost:
        print(f"  🔴 **徽记丢失**（{len(_dp_lost)} 条 —— 让位了、而点名的那条记号不在计划里）：")
        for _c, _f, _t, _s in _dp_lost:
            print(f"     {_c} [{_f}] {_t!r} → {_s!r}")

    # 🆕 2026-10-12（`A1623`）：**「混写」校验** —— 同一字段里**既**落了按词规则那条 token、
    #   **又**落了另一条画同一枚徽记的记号（`❄` / 方括号 / 逐卡 token）⇒ 那多半是
    #   「一处由规则画、另一处由那条记号画」之外的**第三种形状：同一处两枚**（`Rewrite` 是全局替换，
    #   两条都命中同一处就会各插一个）。**今天必须是 0 处** —— 今天那四种形状分别由
    #   例外表 / 守卫① / 守卫② 收掉了（见 `DARKPACT_ALREADY` 那两段注释）。
    #   为什么要有它：例外表是**按卡**的，新来一张「先一枚符号、后面还提一次这个词」的卡
    #   不会被任何一条拦住 ⇒ 靠这条把那种形状**报出来让人判**，⛔ 不让它静默二选一。
    _dp_mixed = []
    for _c, _fs in plan.items():
        for _f, _got in _fs.items():
            _w = [t for t in _got if t in ("Dark Pact", "黑暗契约")]
            if not _w:
                continue
            _other = [t for t, _lst in _got.items() if t not in _w
                      and any(_sp in DARKPACT_SPRITES for _sp, _why in _lst)]
            if _other:
                _dp_mixed.append((_c, _f, _w, _other))
    print("")
    print(f"=== `Dark Pact` 同一字段「混写」校验（`A1623`，应为 0 处）===")
    if _dp_mixed:
        for _c, _f, _w, _o in _dp_mixed:
            print(f"  🔴 {_c} [{_f}]：按词 token {_w} 与 {_o} **同在** ⇒ 可能同一处画两枚，要人判")
    else:
        print("  ✅ 0 处（按词 token 与别的同族记号没有共处一个字段）")

    _dp_bad, _dp_ctrl_missed, _dp_na = [], [], []
    _dp_stripped = {c_: {f_: {t: sp for t, sp in items.items() if t not in DARKPACT_TOKENS}
                         for f_, items in fs_.items()}
                    for c_, fs_ in plan.items()}
    print("")
    print(f"=== `Dark Pact` 一族卡面枚数自检（`A1623`，{len(DARKPACT_FACE_COUNT)} 行 · "
          f"期望值来自成品卡图逐张亲读 + 转录表双写）===")
    for _c, _we, _wz, _png in DARKPACT_FACE_COUNT:
        _et = _texts.get(_c, {}).get("desc", "")
        _zt = _texts.get(_c, {}).get("descZh", "")
        # 🆕 2026-10-15：**官方记号那一代 ⇒ 那一半「载体不适用」**（该字段由 `CardIcons.RewriteMarks`
        #   画、本计划表根本不被查 ⇒ 拿本表去数它**恒为 0**，与卡面印几枚无关）。判据同 `has_official_marks`。
        #   ⚠️ **按字段判、不按行判** ⇒ `desc` 那一半照样被核（今天 37 行的 `desc` 侧全干净、29 行
        #   的 `descZh` 是这一代写法 ⇒ 这 29 行改前恒红、与「改坏没改坏」无关）。
        _e_na = has_official_marks(_et)
        _z_na = has_official_marks(_zt)
        if _z_na:
            _dp_na.append(_c)
        _ge = darkpact_marks(plan.get(_c, {}).get("desc", {}), _et)
        _gz = darkpact_marks(plan.get(_c, {}).get("descZh", {}), _zt)
        _ok = ((_e_na or _ge == _we) and (_z_na or _gz == _wz))
        if not _ok:
            _dp_bad.append((_c, _we, _ge, _wz, _gz))
        # 灭自证：同一份真数据**抽掉 `Dark Pact` 那几条记号** ⇒ 这一行必须**变红**
        #   （载体不适用的那一半不参与 —— 它本来就不由本表画）
        _se = darkpact_marks(_dp_stripped.get(_c, {}).get("desc", {}), _et)
        _sz = darkpact_marks(_dp_stripped.get(_c, {}).get("descZh", {}), _zt)
        if (_e_na or _se == _we) and (_z_na or _sz == _wz):
            _dp_ctrl_missed.append(_c)
        print(f"  {'✅' if _ok else '🔴'} {_c:24s} 画 {_ge}/{_gz} 枚 / 应画 {_we}/{_wz} 枚"
              f"（`desc`/`descZh`） —— 卡图 `{_png}`"
              + ("　⚪ `descZh` 载体不适用（官方记号）" if _z_na else ""))
    _dp_ctrl_rows = [r[0] for r in DARKPACT_FACE_COUNT
                     if not (has_official_marks(_texts.get(r[0], {}).get("desc", ""))
                             and has_official_marks(_texts.get(r[0], {}).get("descZh", "")))]
    if _dp_na:
        print(f"  ⚪ 其中 {len(_dp_na)} 行的 `descZh` **载体不适用**（带官方记号、由 `RewriteMarks` 画）"
              f"⇒ 这几行只核 `desc` 那一半")
    print(f"  ★ 灭自证：把那几条记号**抽掉**之后再数 —— 应当**每一行（载体适用的那一半）都变红**："
          f"{len(_dp_ctrl_rows) - len(_dp_ctrl_missed)}/{len(_dp_ctrl_rows)} 行变红"
          + ("（✅ 无漏）" if not _dp_ctrl_missed else f"；🔴 **漏掉** {_dp_ctrl_missed}"))

    # ── 🆕 2026-10-13（`A1703`）：**逐词图标表（三分）+ 位置锁** 两道自检 ─────────────────────
    #  ① **三分**：`a1703_classify()` **现场重算**（只读转录表）→ 与冻表 `A1703_ALWAYS/…` 逐词比。
    #     它可以红在**两个方向**：转录表改了（有人"修"了它）· 盘上切片名变了（词表跟着变）。
    #  ② **位置锁**：`A1703_POSITION_LOCK`（不许画）/ `A1703_MUST_PAINT`（必须画）逐行核**计划表**
    #     —— 这一半才是「有人把规则触发位放宽」时**当场红**的那条。
    #  ③ **灭自证（五条控制臂，缺一条都可能变成自引用谓词）**：
    #     · 冻表 `A1703_ALWAYS` **删一个词** ⇒ `_a1703_cmp` 必须报差异；
    #     · 冻表 `A1703_SOMETIMES` **把一个反例集清空** ⇒ 必须报差异（那是「② 反例集」那条臂）；
    #     · 位置锁：往计划表里**塞一个本该被禁的 token** ⇒ `_a1703_lock_check` 必须报差异；
    #     · 🆕 **合成输入**直接喂 `_a1703_normalize`（三种双写写法 + 两个反例）⇒ 有一条不对就红
    #       —— 这条**不碰转录表**，所以「把检出器改回去」它**当场就红**；
    #     · 🆕 把检出器**换成改前那一版**（`_a1703_normalize_legacy`）再重算 ⇒ 必须与冻表**不同**
    #       —— 证明冻表里新加的 `dark pact` / `can't attack` 不是白挂的。
    #     🔑 后两条是**2026-10-13 补盲区时一起加的**：光有前三条，**「检出器被改窄 + 冻表跟着删词」
    #       这种**两边一起改回去**的写法看不出红** —— 合成输入那条正是为它准备的。
    _a1703_got = a1703_classify()
    _a1703_bad = _a1703_cmp(_a1703_got, A1703_ALWAYS, A1703_SOMETIMES, A1703_NEVER)
    _a1703_always_mut = set(A1703_ALWAYS)
    _a1703_always_mut.discard(sorted(_a1703_always_mut)[0])
    _a1703_some_mut = {w: set(p) for w, p in A1703_SOMETIMES.items()}
    _a1703_some_mut[sorted(_a1703_some_mut)[0]] = set()
    _a1703_ctrl_missed = []
    if not _a1703_cmp(_a1703_got, _a1703_always_mut, A1703_SOMETIMES, A1703_NEVER):
        _a1703_ctrl_missed.append("冻表恒带集删一个词、重算**没报差异**")
    if not _a1703_cmp(_a1703_got, A1703_ALWAYS, _a1703_some_mut, A1703_NEVER):
        _a1703_ctrl_missed.append("冻表某个「不是恒带」词的反例集被清空、重算**没报差异**")
    # **合成输入**（纯函数、不读转录表）：三种双写写法各一条 + **两个反例**（只出现一次的词**一个
    # 字节都不许动** —— 否则会把普通句式误当图标）。判据 = 卡面双写的**定义**，不是我们自己的实现。
    _a1703_norm_cases = [
        ("Dark PactDark Pact", "Dark Pact Dark Pact", "③ 无空格多词"),
        ("Stun Stun", "Stun Stun", "② 有空格（本函数不许动它）"),
        ("ArmourArmour", "Armour Armour", "① 单 token 对折"),
        ("RallyRally: Gain a Dark PactDark Pact", "Rally Rally: Gain a Dark Pact Dark Pact", "①③ 混在一句里"),
        ("Dark Pact of Fate", "Dark Pact of Fate", "反例：只出现一次 ⇒ 不许动"),
        ("Deal 3 damage to all enemy troops", "Deal 3 damage to all enemy troops", "反例：没有双写"),
    ]
    _a1703_norm_bad = [f"{_n}（{_w}）：{_i!r} → {_a1703_normalize(_i)!r}，应为 {_o!r}"
                       for _i, _o, _n in _a1703_norm_cases
                       if _a1703_normalize(_i) != _o]
    if _a1703_norm_bad:
        _a1703_ctrl_missed.append("合成输入：`_a1703_normalize` 三种双写写法/反例没过："
                                  + "；".join(_a1703_norm_bad))
    if not _a1703_cmp(a1703_classify(normalize=_a1703_normalize_legacy),
                      A1703_ALWAYS, A1703_SOMETIMES, A1703_NEVER):
        _a1703_ctrl_missed.append("换回改前那一版检出器（只认单 token 对折）、重算**没报差异**"
                                  " ⇒ 冻表新加的 `dark pact`/`can't attack` 是白挂的")
    print("")
    print(f"=== `A1703` 逐词图标表（三分，冻表 {len(A1703_ALWAYS)} 恒带 / "
          f"{len(A1703_SOMETIMES)} 不是恒带 / {len(A1703_NEVER)} 从不带）===")
    if _a1703_bad:
        for _d in _a1703_bad:
            print(f"  🔴 {_d}")
    else:
        print("  ✅ 现场重算与冻表**逐词相同**（词集 / 三个分类 / 11 个反例卡集）")
    print(f"  · 现算词数 {len(_a1703_got)}（转录表口径）· 判据 = "
          "`资料/卡表核对_卡图提取/_还原效果文字.md` 的**双写**")
    for _w in sorted(A1703_READ_CORRECTED):
        print(f"  ⚠️ 亲读更正（转录表读数不算数）：`{_w}` —— {A1703_READ_CORRECTED[_w]}")

    _a1703_tokens = {_c: {_f: list(_g.keys()) for _f, _g in _fs.items()}
                     for _c, _fs in plan.items()}
    _a1703_lock_bad = _a1703_lock_check(_a1703_tokens, _texts)
    # 🆕 2026-10-15：**载体不适用**的那几行（字段带官方记号 ⇒ 本计划表不被查）—— 与 `_a1703_lock_check`
    #   里那条 `continue` 用**同一个** `has_official_marks`，这里只是为了把它**打出来**（⛔ 不是第二份判据）。
    _a1703_lock_na = [(c_, f_) for c_, f_, *_ in (A1703_POSITION_LOCK + A1703_MUST_PAINT)
                      if has_official_marks(_texts.get(c_, {}).get(f_, ""))]
    _a1703_lock_mut = {_c: {_f: list(_t) for _f, _t in _fs.items()}
                       for _c, _fs in _a1703_tokens.items()}
    _a1703_lock_c, _a1703_lock_f, _a1703_lock_t, _ = A1703_POSITION_LOCK[0]
    _a1703_lock_mut.setdefault(_a1703_lock_c, {}).setdefault(_a1703_lock_f, []).append(_a1703_lock_t)
    _a1703_lock_ctrl_missed = (not _a1703_lock_check(_a1703_lock_mut, _texts))
    if _a1703_lock_ctrl_missed:
        _a1703_ctrl_missed.append("位置锁：往计划里塞一个本该被禁的 token、核对**没报差异**")
    print("")
    print(f"=== `A1703` 位置锁（{len(A1703_POSITION_LOCK)} 不许画 + "
          f"{len(A1703_MUST_PAINT)} 必须画；判据 = 成品卡图亲读）===")
    if _a1703_lock_na:
        print(f"  ⚪ 另有 {len(_a1703_lock_na)} 行**载体不适用**（字段带官方记号 ⇒ 由 "
              f"`CardIcons.RewriteMarks` 画、本表不被查）："
              f"{'、'.join(f'{_c}[{_f}]' for _c, _f in _a1703_lock_na)}")
    if _a1703_lock_bad:
        for _d in _a1703_lock_bad:
            print(f"  {_d}")
    else:
        print(f"  ✅ 逐行核过：该**不画**的那 {len(A1703_POSITION_LOCK)} 处一个裸 token 都没有、"
              f"该**画**的那 {len(A1703_MUST_PAINT)} 处都在（载体不适用的那几行不计）")
    print(f"  ★ 灭自证：{len(_a1703_ctrl_missed)} 条控制臂没变红"
          + ("（✅ 五条控制臂全部变红、判据有判别力）" if not _a1703_ctrl_missed
             else "：🔴 " + "；".join(_a1703_ctrl_missed)))

    # 🔴 **退出码先算、再决定落不落盘**（`A1626`）——「非 0」必须等于「没写文件」。
    bad = (missing or unresolved or conflicts or why_conflict
           or _abs_dead or _abs_lost or face_bad
           or _legacy or _fp_bad or _fp_ctrl_missed
           or _a75_miss or _a75_ctrl
           or _dp_dead or _dp_lost or _dp_bad or _dp_ctrl_missed or _dp_mixed
           or _a1703_bad or _a1703_lock_bad or _a1703_ctrl_missed)
    if WRITE and bad:
        print("")
        print("🔴 有未解决的问题 ⇒ **本脚本不落盘**（`--write` 被忽略，两份 json 一个字节都没动）：")
        if missing:
            print(f"   · sprite 名盘上没有：{missing}")
        if unresolved:
            print(f"   · 没认出来的记号 {len(unresolved)} 处（见上面那一节）")
        if conflicts:
            print(f"   · 同字段内 token 互相包含 {len(conflicts)} 处（运行时会画两枚）")
        if why_conflict:
            print(f"   · 同一个 token 两条来源 sprite 不一致 {len(why_conflict)} 处（要人判）")
        if _abs_dead:
            print(f"   · `PREFIX_ABSORB` 死条目 {len(_abs_dead)} 条（段首 token 今天不再产出）")
        if _abs_lost:
            print(f"   · `PREFIX_ABSORB` 接手 token 不在计划里 {len(_abs_lost)} 条（那一枚图标会消失）")
        if face_bad:
            print(f"   · 卡面枚数对不上 {len(face_bad)} 行："
                  + "；".join(f"{_c}[{_f}] {_n}≠{_want}" for _c, _f, _want, _n in face_bad))
        if _legacy:
            print(f"   · 信仰前缀又冒出旧写法 {len(_legacy)} 处（`A1605`：一律写 `☀`）："
                  + "；".join(f"{_c}[{_f}] {_p!r}" for _c, _f, _p, _t in _legacy))
        if _fp_bad:
            print(f"   · 统一过的（卡, 字段）没画成 `☀`→`faith` {len(_fp_bad)} 行："
                  + "；".join(f"{_c}[{_f}] 画 {_n} 枚/应 {_want}（sprite={_s!r}）"
                              for _c, _f, _want, _n, _s in _fp_bad))
        if _fp_ctrl_missed:
            print(f"   · 信仰前缀【灭自证】漏掉的（卡, 字段）{len(_fp_ctrl_missed)} 行"
                  f"（抽掉 `☀` 之后没变红）：" + "、".join(f"{_c}[{_f}]" for _c, _f in _fp_ctrl_missed))
        if _a75_miss:
            print(f"   · `A1683`：`ASH75` 的中文又画了信仰 / 或把灵魂石画丢了 —— {'、'.join(_a75_miss)}"
                  f"（画 faith {len(_a75_faith)} 枚 {_a75_faith} · 文本记号 {_a75_tok} · `①`→{_a75_ss}）"
                  "；判据 = `Aeldari/2天赋/Warpforge_02_Storm-of-Silence.png` 亲读")
        if _a75_ctrl:
            print("   · `A1683`【灭自证】把 `①` 从计划里抽掉之后**没变红**（谓词写成自引用了）")
        if _dp_dead:
            print(f"   · `DARKPACT_ALREADY` 死条目 {len(_dp_dead)} 条（本字段今天没有可让位的记号）")
        if _dp_lost:
            print(f"   · `DARKPACT_ALREADY` 点名的那枚记号不在计划里 {len(_dp_lost)} 条（徽记会消失）")
        if _dp_bad:
            print(f"   · `Dark Pact` 卡面枚数对不上 {len(_dp_bad)} 行："
                  + "；".join(f"{_c} {_ge}/{_gz}≠{_we}/{_wz}" for _c, _we, _ge, _wz, _gz in _dp_bad))
        if _dp_ctrl_missed:
            print(f"   · `Dark Pact` 【灭自证】漏掉的卡 {len(_dp_ctrl_missed)} 张（抽掉记号后没变红）："
                  + "、".join(_dp_ctrl_missed))
        if _dp_mixed:
            print(f"   · `Dark Pact` 同一字段「混写」{len(_dp_mixed)} 处（按词 token 与别的同族记号共处一字段）："
                  + "；".join(f"{_c}[{_f}]" for _c, _f, _w, _o in _dp_mixed))
        if _a1703_bad:
            print(f"   · `A1703` 逐词三分与冻表不一致 {len(_a1703_bad)} 条（卡表/转录表/切片名改过？）："
                  + "；".join(_a1703_bad))
        if _a1703_lock_bad:
            print(f"   · `A1703` 位置锁 {len(_a1703_lock_bad)} 条（按词规则画到了卡面没印徽记的位置？）："
                  + "；".join(_a1703_lock_bad))
        if _a1703_ctrl_missed:
            print(f"   · `A1703` 【灭自证】漏掉的 {len(_a1703_ctrl_missed)} 条控制臂（改了冻表/计划却没变红）："
                  + "；".join(_a1703_ctrl_missed))
        print("   ⇒ 先把上面这些解决掉再 `--write`（⛔ 别绕过这道闸 —— 非 0 就该没有产物）。")
    elif WRITE:
        os.makedirs(os.path.dirname(OUT_JSON), exist_ok=True)
        with open(OUT_JSON, "w", encoding="utf-8", newline="\n") as f:
            json.dump({"version": 1,
                       "note": "卡面图标计划：每张卡每个记号画哪张图。由 工具/gen_icon_plan.py 生成，别手改。"
                               "sprite 名在 Resources/Art/traits/ 或 Resources/Art/ui/ 下。",
                       "cards": dump_plan(plan)}, f, ensure_ascii=False, indent=1)
        with open(OUT_RES, "w", encoding="utf-8", newline="\n") as f:
            json.dump({"version": 1,
                       "note": "卡面图标计划（运行时那一份，由 工具/gen_icon_plan.py 与 数据/ 下那份一起写）。",
                       "cards": dump_plan(plan)}, f, ensure_ascii=False, indent=1)
        print("")
        print(f"落盘 {OUT_JSON}")
        print(f"      {OUT_RES}（运行时那份）")
    # 🔴 **2026-10-11（`A1476`）**：`conflicts` / `why_conflict` **也算失败**（退出码非 0）——
    #    它们是「运行时会画两枚」「两条来源打架」，都属于**不许静默**那一类（改前只打日志）。
    # 🔴 **2026-10-12（`A1626`）**：这一段**原来排在 `if WRITE:` 之后** ⇒ 退出码 1 时**照样落盘**
    #    （「非 0」≠「没写文件」）。现在**先算退出码、再决定写不写**，与本工程那条红线对齐
    #    （**工具失败时必须非 0 退出且不出产物**）。
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
