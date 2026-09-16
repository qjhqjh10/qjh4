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
* `资料/卡面图标_对照与缺口.md` —— 人看的表 + 缺口清单

用法：
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_plan.py            # 只报告
  PYTHONIOENCODING=utf-8 python 工具/gen_icon_plan.py --write    # 落盘
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
    ("EC8", "[Health]"): ("Melee", "卡图 `Emperor_s Children/3部队/Warpforge_08_Alluress.png`：第一个圆徽 = 粉拳"),
    ("EC8", "[Attack]"): ("Ranged", "卡图 Alluress：第二个圆徽 = 紫枪"),
    ("SAU_Hardwired_Destruction", "[Health]"): ("Ranged", "卡图 Hardwired Destruction：第二个圆徽 = 紫枪"),
    ("SAU_Hardwired_Destruction", "[Attack]"): ("Melee", "还原表：`Give +1Melee and +1Ranged`"),
    ("GOF87", "[attack]"): ("Melee", "还原表 Nob on Smasha Squig：`+1 Melee and +1 Ranged`"),
    ("GOF87", "[health]"): ("Ranged", "同上"),
    ("GOF16", "[attack]"): ("Melee", "卡图 Banner Nob：第一个圆徽 = 粉拳"),
    ("GOF16", "[weapon]"): ("Ranged", "卡图 Banner Nob：第二个圆徽 = 紫枪（另 4 张同款也核过，见 CLAUDE.md 铁律 7）"),
    ("UM_Avenging_Zeal", "[health icon]"): ("Melee", "卡图 + 模板匹配 NCC 0.718：**写着 health、画的是拳**"),
    ("UM_Avenging_Zeal", "[attack icon]"): ("Ranged", "卡图 + NCC 0.688（写着 attack、画的是枪）"),
    # ---- 其余逐条 ----
    ("GOF_Ferocious_Rage_Beastboss_Talent", "[fist]"): ("Melee", "卡图：粉拳圆徽"),
    ("GOF_Ferocious_Rage_Beastboss_Talent", "[skull]"): ("slay", "卡图：橙红骷髅 + 橙准星 = slay.png"),
    ("UM_Avenging_Zeal", "[Codex icon]"): ("codex", "卡图 + NCC 0.815"),
    ("UM_Death_from_Above", "[Codex]"): ("codex", "卡图 + NCC 0.777"),
    ("UM_Phobos_Lieutenant", "[eye icon]"): ("stealth", "卡图 + NCC 0.955"),
    ("UM_Phobos_Lieutenant", "[Talent]"): ("talent", "卡图 + NCC 0.830（卡牌形图标）"),
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
    ("BL_Helfire_Torch", "[Chaos]"): ("markOfChaos", "卡图 + NCC 0.932"),
    ("BL69", "[Chaos]"): ("markOfChaos", "同上（同一张精灵图）"),
    ("SW68", "[Shield]"): ("shield", "卡面：图标 + 词"),
    ("GOF_Uge_Choppa", "[Slay]"): ("slay", "卡面：图标 + 词 `Slay:`"),
    ("GOF_Uge_Choppa", "[Shield]"): ("shield", "卡面：图标 + 词"),
    ("UM84", "[Oath]"): ("oath", "卡图 Chaplain Cassius + NCC 0.924"),
    ("DA22", "[1]"): ("questPoints1", "暗黑天使任务点：卡面 `gain (①)`。见 `资料/卡表核对_卡图提取/_裁定_图标丢失.md`"),
    ("DA75", "[1]"): ("questPoints1", "同上"),
    # ---- 攻击/远程：还原表逐张写了 Melee / Ranged ----
    ("EC9", "[Attack]"): ("Melee", "还原表：`Gain +1 Melee, +1 Ranged`"),
    ("EC9", "[Ranged]"): ("Ranged", "同上"),
    ("SOR18", "[Armor]"): ("Melee", "还原表：`Give +1 Melee`（**写着 Armor、画的是拳**）"),
    ("EC23", "[Attack]"): ("Ranged", "还原表：`Give +2 Ranged`"),
    ("AM22", "[Attack]"): ("Ranged", "还原表：`+2 Ranged`"),
    ("AM42", "[Attack]"): ("Ranged", "还原表：`+1 Ranged`"),
    ("AM75", "[Attack]"): ("Ranged", "还原表：`+2 Ranged`"),
    ("DA85", "[Attack]"): ("Ranged", "还原表：`+1 Ranged`"),
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
    ("SW62", "[Attack]"): ("Melee", "还原表：`+4 Melee`"),
    ("UM92", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("UM_Master_of_Arms", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("UM_Righteous_Fury", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("TL79", "[Attack]"): ("Melee", "还原表：`+1 Melee`"),
    ("TAU71", "[attack]"): ("Melee", "还原表：`+1 Melee`"),
    # ---- 中文 token ----
    ("GOF103", "[\u62a4\u7532]"): ("Melee", "卡面原文 `Gain +1 Attack` —— **中文数据自己把「攻击」写成了护甲**"),
    ("SOR_Sisters_Repentia", "[\u62a4\u7532]"): ("Melee", "还原表：`Penitence: Gain +2 Melee`"),
    ("GOF21", "[\u653b\u51fb]"): ("Melee", "还原表 Skarboy Nob：`Mob: Gain +1 Melee`"),
    ("ASH75", "[\u751f\u547d]"): ("Melee", "还原表 Storm of Silence：`Your Warlord gains +2 Melee this turn`"
                                                "（中文 token 写「生命」、卡面画的是拳）"),
    # ---- 2026-09-15 补：把「锚点自动对齐」那几处升级成有证据的（还原表原文见每条）----
    ("AM50", "[attack]"): ("Melee", "还原表：`Give +2 Melee, +2 Ranged and Concussive`"),
    # ⚠️ 2026-09-16：`[armor]` 已按卡面改名成 `[ranged]`（`cardface_fixes.json` 的 desc 列；
    #    卡面第二枚是**紫枪**）—— 键要跟着换，否则这条旧键成了**永远不会命中的死条目**、
    #    新 token 反而掉进「锚点没对上」没有图标。中文字段写的是 `[远程]`，
    #    它经 `ZH2EN` 找的是英文对家 `[Ranged]`，所以两条都要在。
    ("AM50", "[ranged]"): ("Ranged", "同上"),
    ("AM50", "[Ranged]"): ("Ranged", "同上（`descZh` 的 `[远程]` 经 `ZH2EN` 找过来）"),
    ("DA44", "[Attack]"): ("Melee", "卡图 `Dark Angels/4计策/Warpforge_44_Ancient-Reliquary.png`：第一枚 = 粉拳"),
    ("DA44", "[Armor]"): ("Ranged", "同上：第二枚 = 紫枪"),
    ("DA4", "[Attack]"): ("Melee", "还原表 Supreme Grand Master：`Give +1Melee and +1Ranged`"),
    ("DA4", "[Armor]"): ("Ranged", "同上"),
    ("EC27", "[Attack]"): ("Melee", "还原表 Disharmonist：`-2Melee and -2Ranged`"),
    ("EC27", "[Armor]"): ("Ranged", "同上"),
    ("EC72", "[Attack]"): ("Melee", "还原表 Antrak Silk：`+1Melee and +1Ranged`"),
    ("EC72", "[Armor]"): ("Ranged", "同上"),
    ("SW62", "[Attack]"): ("Melee", "还原表 Legendary Tenacity：`+4Melee, +4Ranged`"),
    ("SW62", "[Armour]"): ("Ranged", "同上"),
    ("EC17", "[armor]"): ("Melee", "还原表 Sonic Blaster Noise Marine：`-1Melee and -1Ranged`"
                                   " —— 和大多数卡相反（这里 armor 在前 = 拳）"),
    ("EC17", "[attack]"): ("Ranged", "同上"),
    ("EC32", "[attack]"): ("Melee", "还原表 Screamer Kakophonist：`-4Melee and -4Ranged`"),
    ("EC32", "[health]"): ("Ranged", "同上（写着 health、画的是枪）"),
    # ⚠️ 2026-09-16：这一张的 desc 已按卡面**把 token 名换成了它真正的含义、顺序也摆正**
    #    （`Give +1 [attack] and +1 [ranged]`，卡面第一枚=粉拳、第二枚=紫枪）。
    #    所以键要从 `[armor]`/`[attack]` 换成 `[ranged]`；`[attack]`→Melee 那条见上面（现在是对的）。
    ("SOR_Celestian_Sacresant_Aveline", "[ranged]"): ("Ranged",
        "还原表：`Pray: Give +1Melee and +1Ranged` —— token 改名后它就是第二枚（紫枪）"),
    ("DA78", "[Ranged]"): ("Ranged", "还原表：`gain +1Ranged`"),
    ("GSC43", "[Ranged]"): ("Ranged", "还原表 Patriarch：`Give +3 Melee and +3 Ranged`"),
    ("DA38", "[honour]"): ("questPoints", "还原表 Unforgiven Redemptor：`When you gain Quest Point, deal 2 damage`"),
    ("GOF_Worst_Temper", "[Attack]"): ("Melee", "还原表：`+1Melee, Armour Armour 1 and Flying Flying`"),
    ("GOF_Uge_Choppa", "[Attack]"): ("Melee", "还原表：`+2Melee and Slay Slay:`"),
    ("GOF100", "[Attack]"): ("Melee", "还原表 Da Old Ways：`give it +2 Melee this turn instead`"),
    ("AM61", "[Ranged]"): ("Ranged", "还原表：`Give +2 Ranged to your units`"),
    ("SW54", "[Armor]"): ("Ranged", "还原表 Unbridled Fury：`Give +2Melee and -2Ranged`"),
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
    ("UM84", "[Talent]"): ("talent", "还原表 Chaplain Cassius：`TalentTalent: Catechism of Death`（图标 + 词）"),
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
}

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
#        但那要靠语义判断「这个词是当关键词用、还是当普通名词/动词用」：
#        `Stun an enemy` / `Choose a Sabotage card` / 写成词的 `Ranged Attack` **都不带图标**。
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
# 中文版：行首 / `。` / `；` / `，` 之后 → 2~8 个汉字 → 可选数字 → `：` 或 `。`
KEYWORD_SCAN_ZH = re.compile(r"(?:^|[。；,，]\s*)([一-龥]{2,8})(?:\s*(\d+))?\s*([：。])")

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
    """
    out = []
    for is_zh, scan in ((False, KEYWORD_SCAN), (True, KEYWORD_SCAN_ZH)):
        for m in scan.finditer(text):
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
            if not toks and not syms and not kws and not res:
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
    dead = [(cid, tok) for (cid, tok) in TOKEN_BY_CARD if tok not in texts.get(cid, "")]
    print("")
    print(f"=== 逐卡表里的死条目（{len(dead)} 条，token 已不在该卡的 desc/descZh 里）===")
    for cid, tok in dead:
        print(f"  {cid} {tok}")

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
