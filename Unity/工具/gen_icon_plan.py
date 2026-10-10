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
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_plan.py --write    # 落盘
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_doc.py             # 再铺一遍人看的文档
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
                              "⚠️ 五张 markOf* 切片**逐字节相同**，认不出是哪位邪神，只能按名字选"),
}

# ── 方括号 token：**按卡**给答案 ───────────────────────────────────────────────
#      每条都有证据（成卡图路径 / 还原表原文）。同一条证据覆盖多张卡时逐条列出，别用通配。
TOKEN_BY_CARD = {
    # ---- 名字骗人最狠的四对（卡面看过的）----
    # ⚪ 2026-10-10（`A1411`）：`("EC8","[Health]")` **已删** —— 死键（该卡文本里已无 `[Health]`）；
    #    它当年那个答案（Melee / 第一个圆徽 = 粉拳）**已由下面那条 `("EC8","[Attack]")` 承接**（同答案）⇒ 删掉不丢覆盖。
    # 🔴 2026-10-18：这条原来是 `("EC8","[Attack]") → Ranged`（第二个圆徽 = 紫枪）。
    #    那张卡池文本今天写的是 `Rally: Give -2 Attack and -2 Ranged to an enemy troop.` ——
    #    **第一个位置 = 拳、第二个 = 枪**，所以 `Attack` 那一处是**拳**；
    #    本批把两个裸词都改成了记号（`-2 [Attack] and -2 [Ranged]`）⇒ 这条的答案跟着改。
    #    （旧答案不是错，是给**改名之前**那个 token 写的 —— 那个 token 已经不在文本里了。）
    # 🔴 2026-10-10（`A1411`）：键由 `[Health]` **订正成 `[Ranged]`** —— 死键，卡表那一处 2026-10-18
    #    已写成 `[Ranged]`（英文第二个属性）。答案不变（Ranged）、证据沿用原来那份。
    ("SAU_Hardwired_Destruction", "[Ranged]"): ("Ranged", "卡图 Hardwired Destruction：第二个圆徽 = 紫枪"),
    ("SAU_Hardwired_Destruction", "[Attack]"): ("Melee", "还原表：`Give +1Melee and +1Ranged`"),
    ("GOF87", "[attack]"): ("Melee", "还原表 Nob on Smasha Squig：`+1 Melee and +1 Ranged`"),
    # ⚪ 2026-10-10（`A1411`）：`("GOF87","[health]")` **已删** —— 死键；
    #    同位置的 `("GOF87","[ranged]")` / `("GOF87","[远程]")` 两条一直活着（同答案 Ranged）。
    ("GOF16", "[attack]"): ("Melee", "卡图 Banner Nob：第一个圆徽 = 粉拳"),
    ("GOF16", "[weapon]"): ("Ranged", "卡图 Banner Nob：第二个圆徽 = 紫枪（另 4 张同款也核过，见 CLAUDE.md 铁律 7）"),
    # ⚪ 2026-10-10（`A1411`）：`("UM_Avenging_Zeal","[health icon]")` **已删** —— 死键（那条 `[health icon]`
    #    早退场，本卡文本现在是 `[Attack]`/`[Ranged]`/`[Codex icon]`）；同位置第一条由本卡的 `[Attack]` 条目承接。
    # 🔴 2026-10-10（`A1411`）：这一条也是死键，**键订正成 `[Ranged]`**（卡表那一处已写成 `[Ranged]`）——
    #    它当年那个答案（Ranged）**和现在的卡表一致**（写着 attack、画的是枪），所以是换键、不是丢弃。
    ("UM_Avenging_Zeal", "[Ranged]"): ("Ranged", "卡图 + NCC 0.688（写着 attack、画的是枪），键 2026-10-10 由 `[attack icon]` 订正"),
    # ---- 其余逐条 ----
    ("GOF_Ferocious_Rage_Beastboss_Talent", "[fist]"): ("Melee", "卡图：粉拳圆徽"),
    ("GOF_Ferocious_Rage_Beastboss_Talent", "[skull]"): ("slay", "卡图：橙红骷髅 + 橙准星 = slay.png"),
    ("UM_Avenging_Zeal", "[Codex icon]"): ("codex", "卡图 + NCC 0.815"),
    ("UM_Death_from_Above", "[Codex]"): ("codex", "卡图 + NCC 0.777"),
    ("UM_Phobos_Lieutenant", "[eye icon]"): ("stealth", "卡图 + NCC 0.955"),
    # ⚪ 2026-10-10（`A1411`）：`("UM_Phobos_Lieutenant","[Talent]")` **已删** —— 死键，而且**那个位置整处没了**：
    #    本卡 `desc` 今天是 `Long Range. Oath 1: Gain [eye icon] Stealth. Slay: …`（无 `Talent` 词、`descZh` 也无「天赋」）
    #    ⇒ 没有可换的键，只能删（删前实测：本卡的 `plan` 条目一字未变）。
    ("EC39", "[Melee]"): ("Melee", "卡图 + NCC 0.599"),
    ("EC39", "[Ranged]"): ("Ranged", "卡图 + NCC 0.578"),
    ("SOR40", "[Faith Icon]"): ("faith", "卡图 + NCC 0.900"),
    ("SOR7", "[Icon]"): ("faith", "卡图：深棕方底 + 金太阳（与 faith.png 同图）"),
    ("SOR50", "[Icon]"): ("faith", "卡图 + NCC 0.917"),
    ("SOR74", "[icon]"): ("faith", "卡图 + NCC 0.964"),
    ("SOR12", "[faith]"): ("faith", "卡图 Preacher：同一张金太阳"),
    ("SOR4", "[Faith]"): ("faith", "卡面 `8 ☀:` = 信仰付费前缀"),
    ("TAU47", "[Markerlight]"): ("markerlight", "卡面：图标 + 词 `Markerlight 2`"),
    ("TAU65", "[Vanguard]"): ("vanguard", "卡面：图标 + 词 `Vanguard`"),
    ("TAU49", "[Shield]"): ("shield", "卡面：图标 + 词 `Shield`"),
    ("TAU74", "[Power]"): ("Melee", "卡图：粉拳圆徽（**写着 Power、画的是拳**）"),
    ("GSC43", "[Strength]"): ("Melee", "卡图 Patriarch：粉拳圆徽"),
    ("UM87", "[strength]"): ("Melee", "卡图 Bladeguard Ancient：粉拳圆徽"),
    ("EC28", "[Might]"): ("Melee", "卡图 Flawless Blade Champion：粉拳圆徽"),
    ("SOR55", "[Might]"): ("Melee", "卡图 Fiery Conviction：粉拳圆徽"),
    ("GSC51", "[fist]"): ("Melee", "卡面就是粉拳"),
    ("SW63", "[fist]"): ("Melee", "同族，还原表 `+1 Melee`"),
    ("ASH44", "[Invulnerable]"): ("invulnerable", "卡图 Wraithknight + NCC 0.831"),
    ("SOR52", "[Invulnerable]"): ("invulnerable", "卡面：图标 + 词 `Invulnerable`"),
    ("GOF100", "[Stomp]"): ("stomp", "卡图 + NCC 0.866 / 0.848（同一张卡两处）"),
    ("GOF_Stomp_Em", "[Mob]"): ("mob", "卡图 + NCC 0.857"),
    ("GOF_Stomp_Em", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("GOF_Worst_Temper", "[Shield]"): ("shield", "卡面：图标 + 词 `Armour 1`"),
    ("GOF_Worst_Temper", "[Wings]"): ("flying", "卡面：图标 + 词 `Flying`"),
    ("BL51", "[Dark Pact]"): ("markOfChaos", "卡图 + NCC 0.932（暗红盘 + 白八芒星）"),
    # ⚪ 2026-10-10（`A1411`）：`("BL_Helfire_Torch","[Chaos]")` **已删** —— **卡 id 写错了**：
    #    池里没有 `BL_Helfire_Torch`，真卡是下一行那个 `BL69 Hellfire Torch`（`Hellfire` 不是 `Helfire`），
    #    而 `("BL69","[Chaos]")` **本来就在**（同 sprite）⇒ 这条是错 id 上的重复条目。
    ("BL69", "[Chaos]"): ("markOfChaos", "同上（同一张精灵图）"),
    ("SW68", "[Shield]"): ("shield", "卡面：图标 + 词"),
    ("GOF_Uge_Choppa", "[Slay]"): ("slay", "卡面：图标 + 词 `Slay:`"),
    # ⚪ 2026-10-10（`A1411`）：`("GOF_Uge_Choppa","[Shield]")` **已删** —— 死键，而且那个位置**判给 `Slay` 了**：
    #    卡表 2026-10-18（`8b84a3d`）把 `"+2 [Attack] and [Shield] Shield: Heals 3"` 改成
    #    `"+2 [Attack] and [Slay] Slay: Heals 3"`（补回被方括号换掉的词时一并订正）⇒ 上一行那条才是这一处的答案。
    # 🔴 2026-10-18：`("UM84", "[Oath]")` / `("UM84", "[Talent]")` 两条**方括号键**已作废 ——
    #    `UM84` 的 `desc` 改回了**裸写**（`Friendly Oath abilities …` / `Talent: …`，
    #    `cardface_fixes.json` 的 `desc` 列，改动说明见那张表的 `_2026-10-18_Oath裸写`），
    #    两个方括号 token 在文本里**已经不存在** ⇒ 留在本表就是**永远不命中的死条目**。
    #    同两张卡的补法见下面的 `BARE_TOKEN_BY_CARD`。
    ("DA22", "[1]"): ("questPoints1", "暗黑天使任务点：卡面 `gain (①)`。见 `资料/卡表核对_卡图提取/_裁定_图标丢失.md`"),
    ("DA75", "[1]"): ("questPoints1", "同上"),
    # ---- 攻击/远程：还原表逐张写了 Melee / Ranged ----
    ("EC9", "[Attack]"): ("Melee", "还原表：`Gain +1 Melee, +1 Ranged`"),
    ("EC9", "[Ranged]"): ("Ranged", "同上"),
    # ⚪ 2026-10-10（`A1411`）：`("SOR18","[Armor]")` **已删** —— 死键；这一处的答案（Melee）由本卡的
    #    `("SOR18","[攻击]")` + `("SOR18","[Attack]")` 两条承接（同答案）⇒ 删掉不丢覆盖。
    # 🔴 2026-10-10（`A1411`）：下面 5 条的键**由 `[Attack]` 订正成 `[Ranged]`** —— 死键（卡表那一处
    #    2026-10-18 已写成 `[Ranged]`）。它们本来就写着「Ranged」（`[Attack]` 是个骗人的名字，见文件头），
    #    ⇒ 换键后答案一字不变、只是把「这一处 = 紫枪」那份逐卡证据接回到活键上。
    ("EC23", "[Ranged]"): ("Ranged", "还原表：`Give +2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM22", "[Ranged]"): ("Ranged", "还原表：`+2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM42", "[Ranged]"): ("Ranged", "还原表：`+1 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("AM75", "[Ranged]"): ("Ranged", "还原表：`+2 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("DA85", "[Ranged]"): ("Ranged", "还原表：`+1 Ranged`（键 2026-10-10 由 `[Attack]` 订正）"),
    ("DA50", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("DA6", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("DA76", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("EC26", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("EC40", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GSC35", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GSC36", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("GSC55", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GOF75", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("GOF_Dead_Choppy", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GOF_Wreckin_Ball", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("GOF_Special_Dose_Zodgrod_Wortsnagga_Talent", "[Attack]"): ("Melee", "还原表：`+3 Melee`"),
    ("SAU51", "[Attack]"): ("Melee", "还原表：`+1 Melee … +3 Melee`（两处都是拳）"),
    ("SAU_Ramatekh_The_Cruel", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SOR66", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SOR69", "[attack]"): ("Melee", "还原表：`+3 Melee`"),
    ("SOR_Celestian_Sacresant_Aveline", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SW44", "[attack]"): ("Melee", "还原表：`+2 Melee`"),
    ("SW49", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("SW54", "[Attack]"): ("Melee", "还原表：`+2 Melee`"),
    # ⚪ 2026-10-10（`A1411` 顺手）：这里原来还有一条 `("SW62","[Attack]"): ("Melee", "还原表：`+4 Melee`")`
    #    —— 它和本表后面那条同名键（why = `"还原表 Legendary Tenacity：…"`）**是同一个键**，
    #    而 **Python 的字典字面量遇到重复键是「后者静默赢」** ⇒ 这一条从来没生效过
    #    （生成器计划表里 `SW62` 的 `desc [Attack]` 打的 why 一直是后面那条）。
    #    **两条答案相同（Melee）**，所以删掉它**零行为变化**（删前实测：本卡 `plan` 条目逐字未变），
    #    只是把"静默重复"这种坑清掉。
    ("UM92", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("UM_Master_of_Arms", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("UM_Righteous_Fury", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    # ⚪ 2026-10-10（`A1411`）：`("TL79","[Attack]")` **已删** —— 死键，而且**两个答案打架**：
    #    这条写 Melee，而卡表现已写 `[Ranged]`（`+1 Ranged Attack` → `+1 [Ranged]`，`8b84a3d` 改的）
    #    且 `[Ranged]`→Ranged 有按规则条目 ⇒ 这条旧答案**已被卡表推翻**，删（不是换键）。
    ("TAU71", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    # ---- 中文 token ----
    # 🔴 2026-10-10（`A1411`）：键 `[\u62a4\u7532]` **订正成 `[\u653b\u51fb]`**（死键）—— 这一句的**英文那一半**写着
    #    `Attack`（卡面画的是拳），卡表 2026-10-18 把中文也统一成 `+N [\u653b\u51fb]` 了
    #    （`cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。答案不变（Melee）、证据沿用。
    ("GOF103", "[\u653b\u51fb]"): ("Melee", "卡面原文 `Gain +1 Attack` —— **中文数据自己把「攻击」写成了护甲**"
        "（键 2026-10-10 由 `[\u62a4\u7532]` 订正）"),
    # ⚪ 2026-10-10（`A1411`）：`("SOR_Sisters_Repentia","[\u62a4\u7532]")` **已删** —— **卡 id 写错了**：
    #    池里没有 `SOR_Sisters_Repentia`，真卡是 `SOR19 Sisters Repentia`（`Warpforge_19`），
    #    而它那一处今天写 `[Attack]`、本表已有 `("SOR19","[Attack]")`（同答案 Melee）⇒ 错 id 上的重复条目。
    ("GOF21", "[\u653b\u51fb]"): ("Melee", "还原表 Skarboy Nob：`Mob: Gain +1 Melee`"),
    ("ASH75", "[\u653b\u51fb]"): ("Melee", "还原表 Storm of Silence：`Your Warlord gains +2 Melee this turn`"
        "（中文 token 当年写「生命」、卡面画的是拳；键 2026-10-10 由 `[\u751f\u547d]` 订正）"),
    # ---- 2026-09-15 补：把「锚点自动对齐」那几处升级成有证据的（还原表原文见每条）----
    ("AM50", "[attack]"): ("Melee", "还原表：`Give +2 Melee, +2 Ranged and Concussive`"),
    # ⚠️ 2026-09-16：`[armor]` 已按卡面改名成 `[ranged]`（`cardface_fixes.json` 的 desc 列；
    #    卡面第二枚是**紫枪**）—— 键要跟着换，否则这条旧键成了**永远不会命中的死条目**、
    #    新 token 反而掉进「锚点没对上」没有图标。中文字段写的是 `[远程]`。
    # ⚪ 2026-10-10（`A1411` 就地更正）：下面那条 `("AM50","[Ranged]")`（当年是给 `descZh` 的 `[远程]`
    #    当**桥**用的，本行原写着「所以两条都要在」）**已删** —— **那个前提 2026-10-18 起不成立了**：
    #    那一批给 `TOKEN_BY_RULE` 加了 `[远程]` 规则 ⇒ `[远程]` 在 **② 按规则**那一支就答完，
    #    **③ `ZH2EN` 那一支根本轮不到**（实测：现读 `plan` 里 `AM50` 的 `descZh` 带的是规则那条 `why`）
    #    ⇒ 它已经是个**够不着的死键**，删掉不丢覆盖（删前实测：本卡 `plan` 条目一字未变）。
    ("AM50", "[ranged]"): ("Ranged", "同上"),
    ("DA44", "[Attack]"): ("Melee", "卡图 `Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：第一枚 = 粉拳"),
    # 🔴 2026-10-18（A980-c）：键从 `[Armor]` 换成 `[Ranged]` —— 那张卡池文本里**从来没有** `[Armor]`
    #    （注释里也一直写着「卡面第二枚 = 紫枪」，只是键名沿用了更早那版 OCR 写法）⇒ 旧键是**死条目**。
    #    本批把 `desc` 里那个**裸词 `Ranged`** 改成了记号（卡面只印枪、不印词），键跟着换。
    ("DA44", "[Ranged]"): ("Ranged", "同上：第二枚 = 紫枪（键 2026-10-18 由 `[Armor]` 订正过来）"),
    # `BL19` 的中文侧：卡面 `gain a 〔八芒星〕Dark Pact of Excess`（本批亲读
    # `Chaos/3部队/Warpforge_19_Noise-Marine.png`）—— 英文那份靠 `❄` 符号走 `SYMBOLS`，
    # 中文没有那半边 ⇒ 2026-10-18 把中文改成 `[黑暗契约]纵欲黑暗契约`（图标 + 词），这张卡按卡补一条。
    ("BL19", "[黑暗契约]"): ("markOfChaos",
        "卡图 Noise Marine：`gain a 〔暗红八芒星〕Dark Pact of Excess`（图标在词前面）"),
    ("DA4", "[Attack]"): ("Melee", "还原表 Supreme Grand Master：`Give +1Melee and +1Ranged`"),
    # 🔴 2026-10-10（`A1411`）：键 `[Armor]` **订正成 `[Ranged]`**（死键）—— 同一处的键名早就是错的
    #    （`[Armor]` 是 OCR 起的名字，答案从来是「紫枪」），卡表 2026-10-18 已写成 `[Ranged]`。答案不变。
    ("DA4", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("EC27", "[Attack]"): ("Melee", "还原表 Disharmonist：`-2Melee and -2Ranged`"),
    ("EC27", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("EC72", "[Attack]"): ("Melee", "还原表 Antrak Silk：`+1Melee and +1Ranged`"),
    ("EC72", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armor]` 订正）"),
    ("SW62", "[Attack]"): ("Melee", "还原表 Legendary Tenacity：`+4Melee, +4Ranged`"),
    ("SW62", "[Ranged]"): ("Ranged", "同上（键 2026-10-10 由 `[Armour]` 订正）"),
    # ⚪ 2026-10-10（`A1411`）：`("EC17","[armor]")` **已删** —— 死键；这一处（第一枚 = 拳）今天写 `[Attack]`，
    #    本表已有 `("EC17","[Attack]")`（同答案 Melee）⇒ 删掉不丢覆盖。
    ("EC17", "[Ranged]"): ("Ranged", "还原表 Sonic Blaster Noise Marine：`-1Melee and -1Ranged`"
                                     "（第二枚 = 紫枪；键 2026-10-10 由 `[attack]` 订正）"),
    ("EC32", "[attack]"): ("Melee", "还原表 Screamer Kakophonist：`-4Melee and -4Ranged`"),
    ("EC32", "[Ranged]"): ("Ranged", "同上（本批把 token 名换成了它真正的含义 —— 写着 health、画的是枪）"),
    # ⚠️ 2026-09-16：这一张的 desc 已按卡面**把 token 名换成了它真正的含义、顺序也摆正**
    #    （`Give +1 [attack] and +1 [ranged]`，卡面第一枚=粉拳、第二枚=紫枪）。
    #    所以键要从 `[armor]`/`[attack]` 换成 `[ranged]`；`[attack]`→Melee 那条见上面（现在是对的）。
    ("SOR_Celestian_Sacresant_Aveline", "[ranged]"): ("Ranged",
        "还原表：`Pray: Give +1Melee and +1Ranged` —— token 改名后它就是第二枚（紫枪）"),
    ("DA78", "[Ranged]"): ("Ranged", "还原表：`gain +1Ranged`"),
    ("GSC43", "[Ranged]"): ("Ranged", "还原表 Patriarch：`Give +3 Melee and +3 Ranged`"),
    # 🔴 **2026-10-20：`("DA38", "[honour]")` ⛔ 别删、别把它「顺手统一」成 `[Quest Point]`。**
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
    ("DA38", "[honour]"): ("questPoints", "还原表 Unforgiven Redemptor：`When you gain Quest Point, deal 2 damage`"
                          "　⛔ **别删：`[honour]` 是个词、不是图标名，靠本逐卡条目才对上**"
                          "（删条目 / 改 token ⇒ 徽记整枚不画；`DA38` 的 token 本来就**不是** `[Quest Point]`）"),
    ("GOF_Worst_Temper", "[Attack]"): ("Melee", "还原表：`+1Melee, Armour Armour 1 and Flying Flying`"),
    ("GOF_Uge_Choppa", "[Attack]"): ("Melee", "还原表：`+2Melee and Slay Slay:`"),
    ("GOF100", "[Attack]"): ("Melee", "还原表 Da Old Ways：`give it +2 Melee this turn instead`"),
    ("AM61", "[Ranged]"): ("Ranged", "还原表：`Give +2 Ranged to your units`"),
    # 🔴 2026-10-10（`A1411`）：键 `[Armor]` **订正成 `[Ranged]`**（死键）—— 同一处卡表 2026-10-18
    #    已写成 `[Ranged]`（第二枚 = 紫枪）。答案不变（Ranged）、证据沿用原来那份。
    ("SW54", "[Ranged]"): ("Ranged", "还原表 Unbridled Fury：`Give +2Melee and -2Ranged`（键 2026-10-10 由 `[Armor]` 订正）"),
    # ---- 2026-09-16 「`±N` 属性与卡面图标不符」批：token 改名后的键 + 几张自动对齐的转正 ----
    #      ⚠️ 这一批改了 `cardface_fixes.json` 里 31 张卡的 `desc`/`descZh`（判据 = 逐张开 PnP 卡图）。
    #         **改名的 token 要在这里换键**，否则旧键成死条目、新 token 掉进「锚点没对上」没有图标。
    #         🔴 `GOF87` 那条是实测抓到的：不改键的话**锚点会把 `[ranged]` 自动配成盾**（画错图标）。
    ("GOF87", "[ranged]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Orks/3部队/Warpforge_13_Nob-on-Smasha-Squig.png`）："
        "`Other friendly Beasts have +1〔拳〕 and +1〔枪〕` —— 原文 token 写的是 `health`，实为紫枪"),
    ("GOF87", "[远程]"): ("Ranged", "同上（同一张卡的中文写法）"),
    ("AM22", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/3部队/Warpforge_22_Cadian-Standard-Bearer.png`）："
        "`Duty: Give +2〔紫圈枪〕 to your units this turn`，全卡没有粉拳"),
    ("AM42", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/3部队/Warpforge_42_Rogal-Dorn-Tank.png`）："
        "`Regiment: Give +1〔紫圈枪〕`"),
    ("AM75", "[远程]"): ("Ranged",
        "2026-09-16 逐张开图核过（`Astra Militarum/4计策/Warpforge_10_Forged-Killers.png`）：`Give +2〔紫圈枪〕`"),
    ("SAU_Hardwired_Destruction", "[远程]"): ("Ranged",
        "2026-09-16 补上那条**一直挂着的缺口**（本文件 2026-09-15 收口时它还在「锚点没对上」）："
        "同卡的英文 `[Ranged]` 已经由锚点对齐到 Ranged，中文这一枚是同一个位置。"
        "判据：`Necron/2天赋/Warpforge_02_Hardwired-Destruction.png` —— `Give +1〔拳〕 and +1〔准星/枪〕`"),
    ("SOR18", "[攻击]"): ("Melee",
        "2026-09-16 逐张开图核过（`Sorotitas/3部队/Warpforge_18_Simulacrum-Bearer.png`）："
        "`Pray: Give +1〔粉圈拳〕 to your troops` —— 原文英文 token 写 `[Armor]`，实为拳"),
    # ---- 2026-09-15 收口：原来挂着的那「四处缺口」，用户裁决 + 逐张卡图核过，全部接上 ----
    #      ⚠️ 这四条都是**按卡**的，别升级成 token 规则 —— token 名不可信（见文件头），
    #         而且 `[Destroyer]` 这一枚**图集里根本没有 `destroyer.png`**：原版给它用的就是
    #         `frenzied` 那张图，所以按名查永远查不到。
    ("SAU_Hardwired_Destruction", "[Destroyer]"): ("frenzied",
        "2026-09-15 用户裁决。**图集里没有 `destroyer.png` 不是缺口 —— 原版给「毁灭者」用的就是 "
        "`frenzied` 那张图**：`Necron/3部队/Warpforge_24_Skorpekh-Destroyer.png` 卡面直接印着 "
        "`〔此图标〕Destroyer.`（关键词紧挨着它），`Necron/1督军/Warpforge_01_Ramatekh-the-Cruel.png` 同。"
        "并排比对 `icons/frenzied.png`：金圆盘 + 白色张开的爪掌 + 背后两把交叉刀 + 掌下一块小牌，逐处一致"
        "（同批候选 `huntMark` / `rally` 完全不同）。"),
    ("SAU_Hardwired_Destruction", "[毁灭者]"): ("frenzied",
        "同 `[Destroyer]`（同一张卡、同一个位置的中文写法）"),
    ("DA8", "[Quest Point]"): ("questPoints",
        "2026-09-15 用户裁决 + 逐图核过。这枚是**暗黑天使的任务点徽记**（深绿圆盘 + 银灰尖刺环 + "
        "环底铭牌带 + 中央展翅矛纹章）= `icons/questPoints.png`。四条旁证：① `DA43 Repulsor` 同位置画的是"
        "**同一枚带「3」的**（`questPoints3`），中文「获得 3 点任务」；② `DA48 Grim-Resolve` 是带「①」的"
        "（`questPoints1`）；③ `DA16 Librarian` **行首**的 `Shield.` 才是真护盾，是**另一枚**"
        "（青绿圆底 + 盾内一只眼 = `shield`），同卡末尾 `gain ①` 仍是这枚任务点徽记；"
        "④ 规则书中文版 `:199`「任务（Quest）：每获得 3 点任务：向牌库加入 1 张隐秘并洗牌」。"
        "⚠️ 原来的 token `[Shield]` 是 **OCR 认错的** —— 同一枚徽记在 `DA38` 上被写成 `[honour]`"
        "（中文却是「任务点」）。2026-09-15 已把卡表这段文字改成 `[Quest Point]`"
        "（`数据/游戏数据/cardface_fixes.json` 的 `desc`/`descZh` 两列），**引擎触发点也跟着改挂任务点**。"),
    ("DA15", "[Quest Point]"): ("questPoints",
        "同 `DA8`（卡图 `Dark Angels/3部队/Warpforge_15_Company-Veteran.png`：行首 `〔红盾+红骷髅〕Vanguard.` "
        "那枚才是 `vanguard`，`gain` 后面这枚与 `DA8` **逐像素同款**，都是任务点徽记）"),

    # ── 🆕 2026-10-18：`[Attack]` 的**逐卡**近战条目（本批新写的记号）──────────────────
    #  ⚠️ **这一批必须逐卡列，不能升成 `TOKEN_BY_RULE`** —— `[Attack]` 这个**名字**在本工程里
    #    拳/枪都有：老数据 `EC23` / `AM22` / `AM42` / `AM75` / `DA85` 是**枪**、`DA44` / `DA50` 是**拳**
    #    （文件头那条「token 名不可信」讲的就是它）。升成规则 = 哪天有人写了个「实为枪」的
    #    `[Attack]`，会被**静默**画成拳 ⇒ 宁可多 90 行数据。
    #  ⚠️ 本批**新写的**近战记号**一律**用 `[Attack]`（不是 `[Melee]`）是有原因的，别顺手统一：
    #    英文 `desc` 是**引擎输入**，`GivePayload.ReAttr` 认的是字面 `attack` ⇒ 写 `[Attack]`
    #    才能让去括号后的串与改前**逐字节相同**（`EffectText.ParseSegment` 只剥方括号、不剥内容）。
    #    唯一例外 `EC39` 用 `[Melee]` —— 那句走 `TryDouble` 的**字面** `melee and ranged` 匹配。
    ("AM_Lord_Commander", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH41", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH75", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH79", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("ASH_Forewarned", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA29", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA33", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA36", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA42", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA53", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA55", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA57", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA80", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA82", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("DA84", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC1", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC10", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC16", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC17", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC24", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC30", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC33", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC35", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC5", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC58", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC7", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC77", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC8", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF1", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF103", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF21", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF24", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF5", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF50", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF53", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF78", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF8", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF81", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF84", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF97", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF99", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Da_Bigger_Dey_Iz_Mozrog_s_Talent", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Da_Red_Waaagh", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Follower_of_Gork", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Greatest_Warboss", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Krumpaklaw", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GOF_Waaagh_Energy_Weirdboy_Talent", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC13", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC2", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC20", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC21", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC22", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC25", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("GSC7", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("EC22", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR11", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR18", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR19", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR27", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR29", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR30", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR44", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR47", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR6", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR64", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR68", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SOR9", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW13", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW16", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW19", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW26", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW34", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW52", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("SW7", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU72", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU74", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU75", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TAU_Grisly_Feast", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TL4", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TL80", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("TL86", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM73", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM78", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM85", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM96", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Angel_s_Wrath", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Assault_Doctrine", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Avenging_Zeal", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Forward_Deployment", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    # 🔴 2026-10-18 **第二批**：这两张的**英文 `desc` 已改**（`[Attack]` → `[Ranged]`）——
    #     卡面 `Ultramarines/3部队/inceptor srg.png` / `inceptor.png` 那一枚都是**紫圈枪**（本批再亲读一次确认），
    #     而 `[Attack]` 经 `GivePayload.ReAttr` 解出来是 **`attack`（近战）** ⇒ 引擎原来按「近战 +1」结算。
    #     属 `A2`（「裸 `+N` 实为远程」）那一族的漏网两张，本批**做掉**（不是只记录）。
    #     ⚠️ token 名跟着换键，否则旧键成**死条目**。
    ("UM_Inceptor_Sergeant", "[Ranged]"): ("Ranged", "卡图 `Ultramarines/3部队/inceptor srg.png`：那一枚是**紫圈枪**（锚点核对 + 亲读两处一致）"),
    # 🆕 2026-10-18 第二批：`UM101` 的英文 `desc` 原来**截断在 `equal to its`**（少了那一枚记号），
    #    本批补上（判据 = 卡面 `Ultramarines/4计策/Warpforge_37_Indomitus-Crusade.png`：
    #    `Oath 6: Deal damage to all enemy troops equal to its〔**粉拳**〕` —— 本批作者亲读）。
    ("UM101", "[Attack]"): ("Melee", "卡图 `Ultramarines/4计策/Warpforge_37_Indomitus-Crusade.png`：`equal to its〔粉拳〕`（亲读）"),
    ("UM_Outrider", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Paragon_of_Ultramar", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    ("UM_Primarch_of_the_XIII", "[Attack]"): ("Melee", "本批新写的近战记号 —— 原文那一处是**裸词** `Attack`、卡面只印**粉拳**（亲读的 26 张见 `数据/游戏数据/cardface_fixes.json` 的 `_2026-10-18_属性图标方括号`）。"),
    # 🔴 2026-10-18 **第二批**：同上一张，英文 `desc` 已改（`[Attack]` → `[Ranged]`）。
    ("UM_Primaris_Inceptor", "[Ranged]"): ("Ranged", "卡图 `Ultramarines/3部队/inceptor.png`：那一枚是**紫圈枪**（锚点核对 + 亲读两处一致）"),
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
#         逐条推演过：`"Oath"` 在 `UM89` / `UM_Vico_Therbeus` 上会被同卡的 `"Oath 1:"` 那一条挡掉
#         ⇒ 首句那枚**画不出来**（`UM84` 没有 `Oath N:`，用 `"Oath"` 反而能用；为一致，三张都写 `Oath abilities`）。
BARE_TOKEN_BY_CARD = {
    # 原版在**每一个 `Oath` 词**前面都印徽记（含句中）：`Friendly 〔徽记〕Oath abilities …`。
    # 判据 = 成品卡图（`V-OATH` §2.4 把 25 张读完；下面这三张是本批作者**自己开的图**）。
    ("UM84", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_20_Chaplain-Cassius.png`（本批作者亲看整卡）："
        "`Friendly 〔蓝圆盘+白袍人形〕Oath abilities apply an additional time. "
        "〔纸卷〕Talent: Catechism of Death` —— 徽记在**句中**、紧贴词 `Oath`。"
        "2026-10-18 本卡 `desc` 由 `[Oath]`（方括号 ⇒ 整串换掉 ⇒ 词没了）改成**裸写**，"
        "所以这一枚要在这儿按卡补上（改动说明见 `cardface_fixes.json` 的 `_2026-10-18_Oath裸写`）。"),
    ("UM89", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_25_Ferren-Areios.png`（本批作者亲看整卡）：首句 "
        "`〔蓝圆盘+白袍人形〕Oath abilities of friendly troops can be activated up to 3 times each turn.`，"
        "末句 `〔同一枚〕Oath 1: Deal 1 damage` —— **同卡两枚徽记**。"
        "⚠️ 首句这一枚原来**整枚没画**（`Oath abilities` 没有数字、没有冒号 ⇒ `KEYWORD_SCAN` 扫不到）。"),
    ("UM_Vico_Therbeus", "Oath abilities"): ("oath",
        "卡图 `Ultramarines/3部队/Warpforge_12_Vico-Therbeus.png`（本批作者亲看整卡）："
        "`〔眼睛〕Stealth. 〔蓝圆盘+白袍人形〕Oath abilities of friendly troops may be activated on later turns. "
        "〔同一枚〕Oath 1: Gain 〔迷彩〕Camouflage` —— **同卡两枚徽记**，首句这枚原来整枚没画（同 `UM89`）。"),
    # 中文侧：**玩家实际看到的是中文**（`BattleDriver.FaceTextFull`：`DescZh` 有值就用它）
    # ⇒ 只补英文那半 = 卡面照旧少一枚。中文写的是「誓言能力」
    # （`誓言 N：` 那半边的 token 由 `KEYWORD_SCAN_ZH` 自己扫得到，本来就画着）。
    ("UM84", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力额外结算 1 次。`）"),
    ("UM89", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力每回合最多可激活 3 次。`）"),
    ("UM_Vico_Therbeus", "誓言能力"): ("oath", "同上（中文写法；`descZh` = `友方部队的誓言能力可在后续回合激活。`）"),
}

# 🔴 2026-10-11（`A1347`）—— **这四张不要在这儿补条目**：`UM80` / `UM81` / `UM_Scout_Sniper` / `UM74`。
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
#    ⚠️ 原版**没有中文表**（那份中文是我们自己译的）⇒ 中文这半的判据就是「英文那枚在图前、位置一一对应」。
_STUN_CARDS = [
    # (卡 id, 判据出处, 卡图文件名) —— 判据出处 = 逐张抄录里 `Stun` **双写**的那一行
    ("AM47", "`资料/卡表核对_卡图提取/_还原效果文字.md:161`", "Warpforge_47_Pinning-Fire.png"),
    ("AM69", "`资料/卡表核对_卡图提取/_还原效果文字.md:110`", "Warpforge_04_Tempestus-Scion.png"),
    ("AM_Hektor_Thenmann", "`资料/卡表核对_卡图提取/_还原效果文字.md:104`",
     "Warpforge_01_Hektor-Thenmann.png"),
    ("ASH20", "`资料/卡表核对_卡图提取/_还原效果文字.md:31`", "Warpforge_20_Howling-Banshee.png"),
    ("ASH48", "`资料/卡表核对_卡图提取/_还原效果文字.md:71`", "Warpforge_48_Deadly-Ambush.png"),
    ("ASH51", "`资料/卡表核对_卡图提取/_还原效果文字.md:74`", "Warpforge_51_Banshee-Mask.png"),
    ("ASH74", "`资料/卡表核对_卡图提取/_还原效果文字.md:11`", "Warpforge_01_Jain-Zar.png"),
    ("ASH75", "`资料/卡表核对_卡图提取/_还原效果文字.md:12`", "Warpforge_02_Storm-of-Silence.png"),
    ("ASH82", "`资料/卡表核对_卡图提取/_还原效果文字.md:19`", "Warpforge_09_Yrlla-the-Huntress.png"),
    ("BL75", "`资料/卡表核对_卡图提取/_还原效果文字.md:226`", "Warpforge_11_Malicious-Volleys.png"),
    ("DA39", "`资料/卡表核对_卡图提取/_还原效果文字.md:316`", "Warpforge_39_Dark-Talon.png"),
    ("DA_None_Must_Know", "`资料/卡表核对_卡图提取/DarkAngels__3.md:9` **＋** 同文件 `:18`（形状存疑条）",
     "Dark Angels/6秘密/IMG_3696.jpg"),
    ("EC17", "`资料/卡表核对_卡图提取/_还原效果文字.md:359`",
     "Warpforge_17_Sonic-Blaster-Noise-Marine.png"),
    ("GOF39", "`资料/卡表核对_卡图提取/_还原效果文字.md:660`", "Warpforge_39_Stompa.png"),
    ("GOF40", "`资料/卡表核对_卡图提取/_还原效果文字.md:667`", "Warpforge_40_Attack-Squig.png"),
    ("GOF62", "`资料/卡表核对_卡图提取/_还原效果文字.md:689`", "Warpforge_62_Toxic-Bonfire.png"),
    ("GOF77", "`资料/卡表核对_卡图提取/_还原效果文字.md:585`", "Warpforge_03_Snakebite-Grot.png"),
    ("GOF96", "`资料/卡表核对_卡图提取/_还原效果文字.md:640`",
     "Warpforge_22_Gargantuan-Squiggoth.png"),
    ("GOF_Stikkbomb_Boy", "`资料/卡表核对_卡图提取/_还原效果文字.md:581`",
     "Warpforge_02_Stikkbomb-Boy.png"),
    ("GSC17", "`资料/卡表核对_卡图提取/_还原效果文字.md:437`", "Warpforge_17_Locus.png"),
    ("GSC28", "`资料/卡表核对_卡图提取/_还原效果文字.md:448`", "Warpforge_28_Clamavus.png"),
    ("GSC35", "`资料/卡表核对_卡图提取/_还原效果文字.md:455`",
     "Warpforge_35_Hulking-Aberrant.png"),
    ("GSC78", "`资料/卡表核对_卡图提取/_还原效果文字.md:460`",
     "Warpforge_13_Roaming-Outriders.png"),
    ("SAU23", "`资料/卡表核对_卡图提取/_还原效果文字.md:516`", "Warpforge_23_Psychomancer.png"),
    ("SOR59", "`资料/卡表核对_卡图提取/_还原效果文字.md:760`", "Warpforge_59_Imperial-Creed.png"),
    ("TAU57", "`资料/卡表核对_卡图提取/_还原效果文字.md:902`", "Warpforge_57_Recon-Sweep.png"),
    ("TL26", "`资料/卡表核对_卡图提取/_还原效果文字.md:935`", "Warpforge_26_Deathleaper.png"),
    ("TL84", "`资料/卡表核对_卡图提取/_还原效果文字.md:961`",
     "Warpforge_11_Toxic-Entanglement.png"),
    ("UM32", "`资料/卡表核对_卡图提取/_还原效果文字.md:1058`",
     "Warpforge_32_Primaris-Judiciar.png"),
    ("UM71", "`资料/卡表核对_卡图提取/_还原效果文字.md:1016`", "Warpforge_07_Incursor.png"),
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
# ③ 🔴 **「词在正文里、卡面却【没画】图标」的 3 张卡本批【有意不补】**
#    （补了 = 给卡面加上原版根本没有的东西 = 工程红线；逐张亲读成品图定的）：
#    · `GOF90 Beastboss on Squigosaur`（`Blood Thirst`）—— 亲读
#      `Orks/3部队/Warpforge_16_Beastboss-on-Squigosaur.png`：卡面
#      `〔狼头〕Stomp. Friendly Beasts cost 1 less and have "〔橙红骷髅 + 准星〕Slay: Gain Blood Thirst this turn"`，
#      `Blood Thirst` 前面**只有 `Slay` 那一枚**、**没有嗜血徽记**（还原表 `:628` 同判、未双写）。
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
#    ⚠️ 原版**没有中文表**（那份中文是我们自己译的）⇒ 中文这半的判据就是「英文那枚在图前、位置一一对应」。
# (卡 id, 英文 token, 中文 token, sprite, 判据出处, 卡图文件名)
#   中文 token 为 `None` ⇒ **该卡中文卡面里没有那个词、不补**（补了就是死条目）。
#   判据出处 = 逐张抄录里那个词**双写**的那一行（`资料/卡表核对_卡图提取/_还原效果文字.md:NN`）。
_FAMILY8 = [
    # ---- `vulnerable`（14 张）----
    ("ASH_Fate_Inescapable", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:93`", "Zrzut ekranu 2026-04-16 o 18.45.25.png"),
    ("BL31", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:203`", "Warpforge_31_Bringer-of-Decay.png"),
    ("BL_Ghallaron_s_Disciple", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:211`", "Warpforge_3_Ghallarons-Disciple.png"),
    ("BL73", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:264`", "Warpforge_9_Havoc-Champion.png"),
    ("BL60", "Vulnerable 2", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:248`", "Warpforge_60_Daemonic-Pact.png"),
    ("BL76", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:227`", "Warpforge_12_Idolatrous-Despoilers.png"),
    ("BL_Litany_of_Despair", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:201`", "Warpforge_2_Litany-of-Despair.png"),
    ("BL77", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:228`", "Warpforge_13_Spreading-Corruption.png"),
    ("BL_Ghallaron_the_Pious", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:191`", "Warpforge_1_Ghallaron-the-Pious.png"),
    ("DA86", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:312`", "Warpforge_12_Hunters-of-Heretics.png"),
    ("EC4", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:346`", "Warpforge_04_Duelists-Hubris.png"),
    # ⚠️ 中文 token 是 **`易伤`** —— 全池 14 张里**只有这一张**这么写（其余 13 张写 `脆弱`），
    #    本批逐卡核过 `descZh`；⛔ 别按「统一成 `脆弱`」去改（改了就与卡面对不上、还会变死条目）。
    ("SAU54", "Vulnerable", "易伤", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:554`", "Warpforge_54_Curse-of-the-Phaeron.png"),
    ("TL40", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:953`", "Warpforge_40_Exocrine.png"),
    ("TL84", "Vulnerable", "脆弱", "vulnerable",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:961`", "Warpforge_11_Toxic-Entanglement.png"),
    # ---- `huntMark`（22 张）----
    ("SW14", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:780`", "Warpforge_14_Blood-Claw-Pack-Leader.png"),
    ("SW16", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:782`", "Warpforge_16_Fenrisian-Wolf.png"),
    ("SW17", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:783`", "Warpforge_17_Grey-Hunter.png"),
    ("SW20", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:786`", "Warpforge_20_Thunderwolf-Cavalry.png"),
    ("SW22", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:788`", "Warpforge_22_Wolf-Priest.png"),
    ("SW25", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:791`", "Warpforge_25_Long-Fang.png"),
    ("SW26", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:792`", "Warpforge_26_Thunderwolf-Cavalry-Pack-Leader.png"),
    ("SW29", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:795`", "Warpforge_29_Arjac-Rockfist.png"),
    ("SW30", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:796`", "Warpforge_30_Blackmane-Inceptor.png"),
    ("SW36", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:802`", "Warpforge_36_Sons-of-Morkai-Eliminator.png"),
    ("SW37", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:803`", "Warpforge_37_Terminator-Rune-Priest.png"),
    ("SW40", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:806`", "Warpforge_40_Stormwolf.png"),
    ("SW41", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:807`", "Warpforge_41_Vindicator.png"),
    ("SW43", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:809`", "Warpforge_43_Venerable-Dreadnought.png"),
    ("SW48", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:814`", "Warpforge_48_Death-Shun.png"),
    ("SW55", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:821`", "Warpforge_55_Embers-of-Prospero.png"),
    ("SW57", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:823`", "Warpforge_57_Hunters-Guile.png"),
    ("SW60", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:826`", "Warpforge_60_Fenrisian-Monstrosities.png"),
    ("SW61", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:827`", "Warpforge_61_Hordeslayer.png"),
    ("SW65", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:831`", "Warpforge_65_Fenrisian-Wolfpack.png"),
    ("SW67", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:833`", "Warpforge_67_Orbital-Defences.png"),
    ("SW10", "Hunt Mark", "猎杀标记", "huntMark",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:776`", "Warpforge_10_War-Howl.png"),
    # ---- `sabotage`（14 张）----
    ("GSC10", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:431`", "Warpforge_10_Neophyte-Hybrid.png"),
    ("GSC11", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:432`", "Warpforge_11_Neophyte-Specialist.png"),
    ("GSC21", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:441`", "Warpforge_21_Acolyte-Heavy.png"),
    ("GSC23", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:443`", "Warpforge_23_Acolyte-Specialist.png"),
    ("GSC29", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:449`", "Warpforge_29_Vox-Hacker.png"),
    ("GSC34", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:454`", "Warpforge_34_Achilles-Ridgerunner.png"),
    ("GSC48", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:471`", "Warpforge_48_Backstab.png"),
    ("GSC49", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:472`", "Warpforge_49_Clandestine-Operation.png"),
    ("GSC60", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:483`", "Warpforge_60_Underground-Network.png"),
    ("GSC61", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:484`", "Warpforge_61_A-Trap-Sprung.png"),
    ("GSC67", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:490`", "Warpforge_67_Pilfered-Supplies.png"),
    ("GSC69", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:419`", "Warpforge_04_Atalan-Jackal.png"),
    ("GSC72", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:426`", "Warpforge_07_Jackal-Specialist.png"),
    ("GSC_Master_Outrider", "Sabotage", "破坏", "sabotage",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:415`", "Warpforge_02_Master-Outrider.png"),
    # ---- `sniper`（5 张）----
    ("ASH9", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:91`", "Warpforge_9_Ranger.png"),
    ("ASH14", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:24`", "Warpforge_14_Shroud-Runner.png"),
    ("UM17", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1044`", "Warpforge_17_Eliminator.png"),
    ("GSC_Master_Outrider", "Sniper", "狙击", "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:415`", "Warpforge_02_Master-Outrider.png"),
    ("ASH_Bright_Lance_Vyper", "Sniper", None, "sniper",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:97`", "Zrzut ekranu 2026-04-16 o 18.54.28.png"),
    # ---- `bloodThirst`（7 张）----
    ("AM21", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:132`", "Warpforge_21_Attilan-Rough-Rider.png"),
    ("EC18", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:360`", "Warpforge_18_Threnodic-Noise-Marine.png"),
    ("GOF_Dead_Choppy", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:618`", "Warpforge_09_Dead-Choppy.png"),
    ("GOF48", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:675`", "Warpforge_48_Proper-Killy.png"),
    ("SW39", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:805`", "Warpforge_39_Murderfang.png"),
    ("SW58", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:824`", "Warpforge_58_Proper-Hunt.png"),
    ("TL59", "Blood Thirst", "嗜血", "bloodThirst",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:979`", "Warpforge_59_Ravenous-Hunter.png"),
    # ---- `blind`（5 张）----
    ("GOF43", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:670`", "Warpforge_43_Cloud-of-Smoke.png"),
    ("SW56", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:822`", "Warpforge_56_Fenrisian-Blizzard.png"),
    ("TL58", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:978`", "Warpforge_58_Toxic-Miasma.png"),
    ("UM10", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1022`", "Warpforge_10_Octavio-Infiltrator.png"),
    ("UM28", "Blind", "失明", "blind",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:1056`", "Warpforge_28_Sergeant-Allectius.png"),
    # ---- `fast`（5 张）----
    ("ASH78", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:15`", "Warpforge_05_Hornet.png"),
    ("DA36", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:313`", "Warpforge_36_Deathwing-Champion.png"),
    ("GSC64", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:487`", "Warpforge_64_Telepathic-Domination.png"),
    ("SAU9", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:570`", "Warpforge_9_Flayed-One.png"),
    ("GOF15", "Fast", "迅捷", "fast",
     "`资料/卡表核对_卡图提取/_还原效果文字.md:625`", "Warpforge_15_Warbiker.png"),
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
    if _f8_cid == "BL60" and _f8_en == "Vulnerable 2":
        _f8_why += ("⚠️ 本卡 token 取 `Vulnerable 2`（不是 `Vulnerable`）—— 同一段正文里还有 `Invulnerable`，"
                    "而 `Rewrite` 是**纯子串替换**，写 `Vulnerable` 会把 `Invulnerable` 里那半截一起换掉。见本块 ④。")
    BARE_TOKEN_BY_CARD[(_f8_cid, _f8_en)] = (_f8_sprite, _f8_why)
    if _f8_zh is not None:
        BARE_TOKEN_BY_CARD[(_f8_cid, _f8_zh)] = (
            _f8_sprite, _f8_why + f"中文侧同一条（`descZh` 里写「{_f8_zh}」，位置与英文那枚一一对应）。")

# ── 🆕 2026-10-10（`A1400`）：同类【第 9 个】词 —— `Invulnerable`（「蓝紫圆 + 白骷髅 + 横条」）──
# 🔴 **全部读数 / 逐卡判据 / 误伤面两层验证**：`资料/普查产出_第十四会话/W_A1400_Invulnerable.md`。
#
# ① **根因与 `A1366` / `A1376` 是同一个**：`main()` 里那道
#    `if not toks and not syms and not kws and not res and not btok: continue` 会把**整份字段**跳过，
#    而裸 token 表在它**之后**才查。那道判据 `A1366` 已修好（把 `btok` 并了进去）
#    ⇒ **本批只补表、判据一字未动** —— 但本批**自己复验了一遍它对这些卡确实生效**（不是假设）：
#    28 条新增里有 **10 条**落在「改动前整份字段一条记号都没有（`AM36` / `GSC47` / `GSC58` /
#    `SW18` / `UM52` 五张卡的 `desc` + `descZh`）」里 —— 若那道判据没修，这 10 条**一条都不会出现**。
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
#    ⚠️ 原版**没有中文表**（那份中文是我们自己译的）⇒ 中文这半的判据就是
#    「英文那枚在图前、位置一一对应」。
# (卡 id, 判据出处, 卡图文件名)
#   判据出处 = 逐张抄录里 `Invulnerable` **双写**的那一行（`资料/卡表核对_卡图提取/_还原效果文字.md:NN`）。
_INVULNERABLE = [
    ("ASH21", "`资料/卡表核对_卡图提取/_还原效果文字.md:32`",
     "Aeldari/3部队/Warpforge_21_Nuadhu-Fireheart.png"),
    ("AM36", "`资料/卡表核对_卡图提取/_还原效果文字.md:147`",
     "Astra Militarum/2天赋/Warpforge_36_Renowned-Bodyguard.png"),
    ("BL60", "`资料/卡表核对_卡图提取/_还原效果文字.md:248`",
     "Chaos/4计策/Warpforge_60_Daemonic-Pact.png"),
    ("DA82", "`资料/卡表核对_卡图提取/_还原效果文字.md:280`",
     "Dark Angels/3部队/Warpforge_08_Chaplain-Gabutheron.png"),
    ("GSC47", "`资料/卡表核对_卡图提取/_还原效果文字.md:470`",
     "Genestealer Cult/4计策/Warpforge_47_Perfect-Ambush.png"),
    ("GSC58", "`资料/卡表核对_卡图提取/_还原效果文字.md:481`",
     "Genestealer Cult/4计策/Warpforge_58_Open-Insurrection.png"),
    ("SOR38", "`资料/卡表核对_卡图提取/_还原效果文字.md:739`",
     "Sorotitas/3部队/Warpforge_38_Saint-Celestine.png"),
    ("SW18", "`资料/卡表核对_卡图提取/_还原效果文字.md:784`",
     "Space Wolves/3部队/Warpforge_18_Fyrri-Askar.png"),
    ("SW29", "`资料/卡表核对_卡图提取/_还原效果文字.md:795`",
     "Space Wolves/3部队/Warpforge_29_Arjac-Rockfist.png"),
    ("TL61", "`资料/卡表核对_卡图提取/_还原效果文字.md:982`",
     "Tyranid/4计策/Warpforge_61_Apex-Predator.png"),
    ("UM88", "`资料/卡表核对_卡图提取/_还原效果文字.md:1052`",
     "Ultramarines/3部队/Warpforge_24_Terminator-Captain.png"),
    ("UM96", "`资料/卡表核对_卡图提取/_还原效果文字.md:1092`",
     "Ultramarines/4计策/Warpforge_32_Shall-Know-No-Fear.png"),
    ("UM52", "`资料/卡表核对_卡图提取/_还原效果文字.md:1102`",
     "Ultramarines/4计策/Warpforge_52_Humanitys-Shield.png"),
    # ⚠️ 第 14 张**不在** `_还原效果文字.md` 里（它来自 `6秘密/` 的 5 张**手机翻拍**，
    #    同 `DA_None_Must_Know`）⇒ 判据 = 本批作者**亲读** `IMG_3698.jpg`（见 ②）。
    ("DA_Rites_of_Penance", "`资料/卡表核对_卡图提取/DarkAngels__3.md:11` **＋** 亲读 "
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
    if _inv_cid == "BL60":
        _inv_why += ("⚠️ 本卡同一段正文里**还有** `Vulnerable 2`（`A1376` 按卡补的）——"
                     "两条**安全共存**（`Rewrite` 是 `Ordinal` 替换、`invulnerable` 图名不含大写 `Vulnerable`），"
                     "见本块 ④。")
    if _inv_cid == "DA_Rites_of_Penance":
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
#    · `Blind` / `失明`         —— 6 张里 5 张带（`SAU55 Solar Pulse` 同）
#    · `Relentless`             —— 0/1（`SAU3` 卡面上那个词是**天赋名** `Relentless March`）
#    ⇒ 这三条仍在 `_FAMILY8` / 不建条目（判据见那个块 ③）。
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
]

# 认得出、但**盘上没有这张图**的记号（真缺口，如实列出来，别静默跳过）
# ── **按卡**的已知缺口（认得出位置、但没有可用的图）────────────────────────────
# ⚠️ 2026-09-15 更正：这里原来挂着 `("DA8","[Shield]")` 与 `("DA15","[shield]")` 两条，
#    写着「原版卡面这枚图标没认定 / 要核实，别猜」。**现在认定了** —— 是暗黑天使的**任务点徽记**
#    （`icons/questPoints.png`），已升级成 `TOKEN_BY_CARD` 里两条带证据的（见那段末尾）。
#    当时的错因：只有「暗色圆徽 / 螺旋」这种**形状描述**、没有并排比对图；实测形状名不能当键
#    （78 张图被起过 180 个形状名）。**结论：卡面图标对不上时，直接裁下来跟 78 张图并排比，
#    别靠文字描述转述。**
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
    #     `AM75`/`DA85` 是枪、`DA44`/`DA50` 是拳）⇒ 名字不可信，**只能逐卡定**
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
KEYWORD_SCAN = re.compile(
    r"(?:^|[.;]\s+)([A-Z][A-Za-z'\-]*(?:\s+(?:of\s+)?[A-Z][A-Za-z'\-]*){0,2})(?:\s+(\d+))?\s*([:.])")
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
KEYWORD_SCAN_ZH = re.compile(r"(?:^|[。；,，]\s*)([一-龥]{1,8})(?:\s*(\d+))?\s*([：。])")


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
    """
    pos = 0
    while pos <= len(text):
        m = scan.search(text, pos)
        if m is None:
            return
        yield m
        pos = max(m.start(3), pos + 1)

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
            token = text[m.start(1):m.end(3)]
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
    "Energy": "faith",              # 还原表的小表把「太阳」记成 Energy；卡图 = faith.png
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
# ⚠️ 2026-09-15：`("DA8", "[Shield]")` 那条已随 token 改名换成 `[Quest Point]`
#    （卡表文字改过了，见 `TOKEN_BY_CARD` 里那两条的说明）—— 留着旧 token 就成了永远不会命中的死条目。
ANCHOR_NOISE = {("DA75", "[1]"), ("GOF87", "[health]"), ("GOF_Uge_Choppa", "[Slay]"), ("DA8", "[Quest Point]"),
                # 2026-09-16：`GOF87` 的 `[health]` 按卡面改名成 `[ranged]`（实为紫枪），
                # 而**锚点对这一枚一直给 `armour`** —— 那句话里紧邻的前一个图标是 `Armour 1.` 的银盾，
                # 锚点被它带偏了。判据是逐张开图（`Orks/3部队/Warpforge_13_Nob-on-Smasha-Squig.png`：
                # `+1〔拳〕 and +1〔枪〕`），所以按 `TOKEN_BY_CARD` 的答案，不按锚点。
                ("GOF87", "[ranged]")}


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
    # `A1378` 按词裸 token 规则的逐词读数（token → 分档 → 卡 id 集合），最后打出来
    rule_audit = collections.defaultdict(lambda: collections.defaultdict(set))
    # `A1411`：③「中文走英文对家（`ZH2EN`）」那一支**真被查到**的键 —— 它们**不是死条目**
    # （token 不在文本里是**设计如此**：它的用途就是给 `descZh` 当桥）
    zh_bridge_used = set()

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
            toks = list(TOK.finditer(text))
            syms = [ch for ch in text if ch in SYMBOLS]
            # 句首「关键词 + :/.」——`desc` 是 OCR 的、这些图标没被记下来，按名字补（见 KEYWORD_PREFIX）
            kws = keyword_prefixes(text, KWIDX, ZH_KW, [(m.start(), m.end()) for m in toks])
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
              f"全池出现 {len(_a['全池出现'])} 卡{_flag}")

    if WRITE:
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
    return 1 if (missing or unresolved) else 0


if __name__ == "__main__":
    sys.exit(main())
