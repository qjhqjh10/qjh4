#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cards_engine.py — 卡牌原始数据 → 规则引擎用的精简卡表

为什么要有这一道：
  1. `card_stats.json` 有 978 KB，大头是 `art` / `voice` / `ocrSrc` / `face` 这些
     **引擎完全用不到的路径字符串**。精简后能进构建、能进仓库
     （⚠️ **2026-10-14 `A1801` 订正**：这里原写「只剩 273 KB」—— 那是**当时**的读数、早就过期；
     现读 = **387,741 B ≈ 379 KB**。⛔ 别在这种注释里写会烂的死数，要写就写现读的）。
  2. 原始数据里数值字段**大量为 null**（`"cost": null` 有 124 处）。
     C# 侧 `JsonUtility` 遇到显式 null 不可靠 —— 在这里一次性归一化掉，
     生成的 JSON 里每个字段都有确定类型，`CardDatabase.cs` 就不用写防御代码了。

输出：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`
（放 `Resources/` 是为了运行时 `Resources.Load<TextAsset>("cards_engine")` 能取到 ——
  `.json` 进 Assets 默认就按 TextAsset 导入，232 KB 进构建可以接受）

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_cards_engine.py
    ... --write     显式落盘（写 cards_engine.json ＋ 自造 id 清单）
    ... --check     只对账（与盘上比），不写文件
    ⚠️ **不带 `--write` = 干跑**（`A1648`）：照常跑完、把读数全打出来，**一个字节都不写**。
        （原来「不带参数就落盘」—— 本意只想看读数的一次运行**无条件重写**了产物。）
"""
import argparse
import difflib
import json
import os
import re
import sys

SRC = r"d:/4/Unity/数据/游戏数据/card_stats.json"
DST = r"d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
# 稀有度权威来源：vision 逐张看卡面宝石颜色判的，1118/1118 全覆盖
RARITY_SRC = r"d:/4/Unity/资料/卡牌数据表/卡牌宝石稀有度_0824.md"
# 中文卡名/效果文字：键 = 英文卡名。**只有这一份**（原来是 7 个 `_tmp_zhcards_g*.json`，
# 2026-09-12 合并成 zh_cards.json —— `_tmp_` 那个名字有被当临时文件清掉的风险）。
ZH_SRC = r"d:/4/Unity/数据/卡牌翻译/zh_cards.json"
# 🔴 **原版官方中文表**（`A1801`，2026-10-14 起）：原版客户端真有中文表 —— 由远端 Addressables 包
#    `localization_assets_all`（2025-07-30 快照）下发，2026-10-14 解开（I2 `I2Languages` 的 `temp`
#    是**逐字节重复密钥 XOR**，不是压缩）。结构：`terms` 是 dict、值 = `[en, zh-CN]` 两元素数组。
#    ⇒ **卡中文名以它为准**（下面 `load_official_cardname()`），`zh_cards.json` 降为**兜底**
#    （官方表里没有的卡才用它那份自译）。判据出处：`资料/普查产出_第二十七会话/I2词条表_解码路径.md`。
OFFICIAL_LOC_SRC = r"d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json"

# 🔴 **卡改过名之后，中文表里还留着旧名** ⇒ 按新名查落空 ⇒ 卡面印英文名。
#    这张表 = 「引擎卡名 → 中文表里的键」。**只填「同一张卡改了名」的**，
#    值必须是**中文表里那个旧名**（出处：`Unity/数据/本地化/i18n/zh_CN.csv`），
#    ⚠️ **2026-09-18 更正**：原来这里写「**官方本地化**里那个旧名」—— **那个说法站不住**。
#       实测原版客户端**根本没有中文表**（246,807 个文件扫中文串零命中 · 84 个 bundle 无本地化包 ·
#       I2 表在远端 CCD）⇒ 这张 csv **是我们自己译的**，不是官方产物。
#       🔴 **2026-10-14 更正（铁律 5，本会话现核）**：上面这条**只说对了一半、结论写反了** ——
#          原版客户端**有**官方中文表：它**不在客户端安装目录里**，而是由**远端 Addressables 包
#          `localization_assets_all`**（2025-07-30 快照）下发。⇒ 「246,807 个文件扫中文串零命中」
#          **只证明「不在离线包体里」**，**不等于「根本没有」**（当年就是把这两句读成了一句）。
#          该包 2026-10-14 已解开并落进 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`
#          （判据 → `资料/普查产出_第二十七会话/I2词条表_解码路径.md`）。
#        ✅ **仍然成立的两条**：① 这张 `zh_CN.csv` **是我们自己译的**（不是官方产物）；
#          ② **客户端安装目录里**确实没有中文表。这张 csv 继续当**我们内部的命名基准**用。
#          ⛔ 但「中文名以谁为准」已经从「我们自译」改成**官方表**（见 `OFFICIAL_LOC_SRC`）。
#    翻译本身不抄进代码（免得两处各写一份，迟早不一致）。
#    出处：`资料/PnP卡图_逐张对账_0915.md` §四·F 的改名清单（每条都带三处独立证据）。
ZH_NAME_ALIAS = {
    # 引擎名（新）          中文表里的键（旧）        我们的译名（⚠️ 不是「官方中文」，见 :38 的更正）
    "Fire Warrior Marksman":  "Fire Warrior Sniper",   # 火氏狙击手    （zh_CN.csv:1307）
    # 🔴 2026-10-17（B20）：这里原来还有三条 —— `Dogmata`→`Sister Dogmata` ·
    #    `Sons of Morkai Eliminator`→`Morkai Eliminator` · `Land Raider`→`Land Rider`。
    #    **那三条对应的改名已全部撤回**（卡名回到游戏侧的美术资产名）⇒ 引擎名与中文表的键
    #    重新相等，别名退化成自映射 ⇒ 删掉。判据见 `STAT_FIXES` 里那段 2026-10-17 的撤销说明。
    # 🆕 2026-09-18：这两张的**旧键是美术文件名尾段、不是卡名**（见 `STAT_FIXES` 里那段）——
    "Imotekh the Stormlord":  "stormlord",             # 伊摩泰克·风暴领主
    "Orikan the Diviner":     "Diviner",               # 占卜师
    # 🔴 2026-10-15：这一块原来**逐字重复了两遍**（同键同值：后写的赢、前一份静默失效）
    #    ⇒ 删掉重复的那一份，**行为零变化**（两条的值本来就相同）。
    #    与 `cardface_fixes.json` 的 `Fenrisian Drop Pod` 同族（同键两值 ⇒ 后者覆盖前者）。
}

# ⚠️ 这一类**不能靠别名**：卡名里的**数字变了**（`2nd` → `1st`），
#    别名会直接把旧名字的译文端过来（会印成「第二连终结者」，**错**）。
#    这里写**推导值**，并在注释里标清楚是**我们推的、不是官方原文**。
ZH_NAME_OVERRIDE = {
    # ⚠️ 这一条是**官方译文**，只是我们的 `zh_cards.json` 里**没有这个键**（别名接不上）⇒ 直接写值。
    #    出处：`Unity/数据/本地化/i18n/zh_CN.csv:830`（`Chosen of the Four,~,四神宠儿`）。
    "Chosen of the Four": "四神宠儿",

    # 🔴 2026-10-17（B20）：这里原来还有一条 `1st Company Terminator` →「第一连终结者」
    #    （**推导值**：官方对 `2nd Company Terminator` 的译文是「第二连终结者」（zh_CN.csv:465），
    #     当年卡名按 PnP 从 2nd 改成 1st ⇒ 机械换号）。**那条改名已撤回** ⇒ 这条推导值一并删掉：
    #    `zh_cards.json` 里 `2nd Company Terminator` 本来就有官方译文「第二连终结者」，直接命中。
}


# 引擎认识的稀有度取值（`special` = 橙红宝石，防御/药剂/特殊卡）
RARITIES = ("common", "rare", "epic", "legendary", "special")

# 🔴🔴 **原版卡 SO 快照的 `cardRarity` 枚举 → 我们的写法**（账 `A2062`，2026-10-16）
#
# 这是**稀有度那一列的【第 1 层】**（权威序见 `build()` 主循环里 `TRUTH_RARITY` 那一段）：
#   · 值域 `1..5`，**映射是现算的、不是猜的** —— 拿 `TRUTH_CARDS_SRC` 那份快照与本卡池按
#     `uniqueId` join（1009 张）做混淆矩阵，**每个稀有度只对应一个取值、0 例外**
#     （`common=1 · rare=2 · epic=3 · legendary=4 · special=5`；判据与矩阵 →
#      `资料/普查产出_第三十二会话/G3_rarity层与守卫.md` §二）。
#   · `0` / 缺字段 = **「无值」哨兵**，**不是** `common` —— ⛔ **不拿 0 顶**
#     （快照里 4 张 GSC「破坏卡」就是 `0`：`Improvised Barricade` / `Poisoned Supplies` /
#      `Jammed Communications` / `Cult Propaganda`；它们的稀有度**只有卡面宝石一条判据**）。
#
# **为什么把它当第 1 层**（`A2062` 的判据）：
#   ① 它是**游戏自己的枚举**，比宝石表（**人眼看印刷品记下来的色名**）高一级
#      （`CLAUDE.md` 铁律 10 第 3 条：PnP 是印刷品）；
#   ② 实测**全池 0 处不一致**（现读 1005/1005）⇒ **换成它当第 1 层，一个值都不会变**；
#   ③ 它能**从根上消掉**「PnP 写法 vs 我们的卡名」这一整类 join ——
#      `RARITY_BY_FACTION` 那 23 条特判、`RARITY_UNLISTED` 那 21 条、B20 那 4 条、`SOR24` 那次
#      **全属这一类**（宝石表是按**卡面写法**建的键，我们池里有一批卡用的是**美术/资产名**）。
#   ⚠️ **快照比现池旧**（2025-07-30；SW 68 / 新 UM 38 / 新 DA 12 根本不在里面）⇒
#      真值够不到的那 **122 张照旧走** ②宝石表 → ③`RARITY_BY_FACTION` → ④`RARITY_UNLISTED`。
TRUTH_RARITY = {1: "common", 2: "rare", 3: "epic", 4: "legendary", 5: "special"}

# 🔴 **2026-10-18（`K06`）换快照后核过的 1 条真值差异** —— 不是把守卫拔掉，是**显式放行已核过的**：
#   · `e59ac9e46f4ea804784e954c3b820e77` / `Skyborne Deployment`（`m_Name` = `Z-NC SaimHann-Skyborne Deployment`）
#     旧快照（2025-07-30）`cardRarity = 2`（rare）· 当前 build **`= 1`（common）**。
#     ⇒ **游戏侧真的改了这张的稀有度**，不是映射错、也不是 join 错（两个快照里 `uniqueId`/`m_Name` 逐字相同）。
#     按「第 1 层 = 游戏自己的枚举」的口径**取新值**；其余任何新变化照旧进 `_bad`（fuse 仍然有效）。
#   ⚠️ 键 = `(uniqueId, cardName)`，值 = `(旧值, 新值)`；**值也要对上**才放行（防止它再变一次还静默放行）。
RARITY_TRUTH_ACCEPTED = {
    ("e59ac9e46f4ea804784e954c3b820e77", "Skyborne Deployment"): ("rare", "common"),
}


# 宝石表里**同名不同卡**的行 —— 查表若只按卡名建键，两行会互相覆盖（**后写的赢**）。
# 2026-09-12 实测：1118 行里有 5 个卡名重复，其中 **3 个两行的稀有度不同**，不特判就会取错。
# 归位依据两条（都记在这里，免得下个会话重新挖）：
#   ① **批次 → 阵营**：把每个批次里的卡名拿回我们卡池查阵营、投票，得出
#      `C=BlackLegion`（73/76）· `D=DarkAngels`（75/75）· `E=EmperorsChildren`（59/59）· `U=Ultramarines`（130/131）
#   ② **卡面实测**（直接打开图看底部那颗菱形宝石的颜色）：
#      · `Chaos/3部队/Warpforge_44_Terminator-Champion.png`（图上写 Black Legion）→ 浅蓝 = common
#      · `Chaos/3部队/Warpforge_46_Maulerfiend.png`（Black Legion）          → 紫   = epic
#      · `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`             → 绿   = rare
#    三张都与①的推断一致。
# ⚠️ 根子是**卡名当 id**（`项目任务.md:727` 记过同一件事）—— 真要治本得给卡发稳定 id。
RARITY_BY_FACTION = {
    ("BlackLegion", "Terminator Champion"): "common",
    ("BlackLegion", "Maulerfiend"): "epic",
    ("DarkAngels", "Bladeguard Veteran"): "rare",

    # ---- 🆕 2026-09-15 PnP 逐张对账批：卡面宝石**像素取色**实测的三处（引擎原值错）----
    # 判据同下面那批：采样卡图底部菱形宝石取 hue（浅蓝 common / 绿 rare / 紫 epic / 金 legendary）。
    ("Sororitas", "Sister Dogmata"): "rare",           # 卡面绿 = rare（引擎原 legendary；SOR 77 张里唯一一处）
    ("AstraMilitarum", "Leman Russ"): "rare",          # 卡面绿 = rare（引擎原 legendary；卡面印 `Leman Russ Tank`）

    # ---- 🔴 2026-10-17（B20）四张「卡名撤回后的查表守卫」----
    # 这 4 张的卡名当天从 PnP 写法**撤回成美术资产名**（见 `STAT_FIXES` 里那段撤销说明），
    # 而 `资料/卡牌数据表/卡牌宝石稀有度_0824.md` 是**按 PnP 卡面写法建的键**
    # （`Dogmata` / `Land Raider` / `Sons of Morkai Eliminator` / `1st Company Terminator`）
    # ⇒ 按新名查会**落空**，必须在这里按 (阵营, 卡名) 特判。
    # ⚠️ **不写就会掉进 `still_missing`（稀有度空串）** —— 与 `Bladeguard Lieutenant` 那条同一个坑。
    # 值**不是新判的**，直接取上面那张宝石表里同名行的值（行号标在后面）。
    ("SpaceWolves", "Land Rider"): "common",            # 宝石表 :781 `Land Raider` 浅蓝 = common
    ("SpaceWolves", "Morkai Eliminator"): "epic",       # 宝石表 :772 `Sons of Morkai Eliminator` 紫 = epic
    ("Ultramarines", "2nd Company Terminator"): "rare", # 宝石表 :1018 `1st Company Terminator` 绿 = rare

    # ---- 🆕 2026-09-13 第三十二轮：13 处宝石表读错的（**逐张开图采宝石像素**重判）----
    # 来源 `资料/卡表核对_卡图提取/_裁定_稀有度.md`。判定方法：采样卡图底部菱形宝石区
    # （x≈402-486, y≈985-1110）取 hue —— **浅蓝(h≈186, sat≈0.30)=common · 绿 h≈122=rare ·
    # 紫 h≈279=epic · 金 h≈42=legendary · 橙红 h≈15=special**
    # （色→稀有度的映射有官方资产佐证：`Art/原版/去重资源/{1..5}_40k_cardframe_rarity_*.png`
    #  实测 hue 180/120/270/45/15）。
    #
    # ⚠️ **为什么这批会集体读错**：common 那颗宝石是**低饱和的钢蓝**，
    #    缩略图上看着偏灰白 —— 0824 那轮视觉扫描把它读成了紫或绿。
    # ⚠️ 同一份裁定里另报的 12 处**是假差异**（旧值本来就对）、3 处是重名撞车 —— **都没收**。
    #
    # ⚠️ **这是「卡名当 id」这个根病第三次咬人**（前两次：费用修正按卡名、临时卡的实例身份）。
    #    这张表就是为它打的补丁：宝石表按**卡名**查，重名时后写的赢 —— 只能按 (阵营, 卡名) 特判。
    ("AstraMilitarum", "Thunderous Charge"): "common",
    ("DarkAngels", "Intercessor"): "common",
    ("DarkAngels", "Deathwing Terminator"): "common",
    ("DarkAngels", "Unforgiven Redemptor"): "common",
    ("DarkAngels", "Dark Talon"): "common",
    ("DarkAngels", "Steadfast Warriors"): "common",
    ("Sautekh", "Triarch Praetorian"): "common",
    ("Sautekh", "Reanimate"): "common",
    ("Sautekh", "Spyder Nest"): "common",
    ("Sautekh", "Solar Pulse"): "common",
    ("Sautekh", "Dimensional Breach"): "common",
    ("Goff", "Mega Blasta Deffkopta"): "common",
    ("TauEmpire", "Missile Drone"): "common",
    # UM 那张（**原名 = 数字版卡名 = `Bladeguard Lieutenant`**，见 `STAT_FIXES` 里那条更正）：
    # 源表 `rarity` 是**空串**，宝石表那行（批次 U）又是按**卡面印的写法**建的键 ⇒ 只能在这儿特判。
    # 🔴 **2026-09-15 更正**：原来这里写「宝石实读**绿 = rare**（与 DA 那张同名卡一致）」—— **不成立**。
    #    按同一套取样法（底部菱形宝石区 x402-486/y985-1110 取 hue 中位）重测：
    #      · `Ultramarines/3部队/Warpforge_34_Bladeguard-Veteran.png` → **hue 177.7 = 钢蓝 = common**
    #      · `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`   → hue 120.0 = 绿 = **rare**（这条对）
    #    两张**不同色**，旧注把 UM 那张当成跟 DA 一样了（UM/AM 那路子代理独立读数也是 common）。
    # 🔴 **2026-10-17**：键从 `"Bladeguard Veteran"` 改成 `"Bladeguard Lieutenant"` —— 卡名撤回了原版名，
    #    这条特判**必须跟着改**（查表用的是**改名后**的 `name`），否则这张会掉进 `still_missing`（稀有度空串）。
    ("Ultramarines", "Bladeguard Lieutenant"): "common",

    # ---- 🔴 2026-10-15（`G3`）`SOR24` 撤改名 ⇒ 宝石表按 PnP 写法建的键又查不着了 ----
    # 卡名从 `Simulacrum Celestian` **撤回成 `Simulacrum Imperialis`**（用户 2026-10-15 拍板；
    # 佐证 = 原版卡 SO 快照 `SOR24` 的 `cardName` 就是 `Simulacrum Imperialis`，
    # 美术/语音/卡组资产名也全是它），而宝石表
    # `资料/卡牌数据表/卡牌宝石稀有度_0824.md:698` 那一行是 **`Simulacrum Celestian | 紫 | epic`**
    # —— 那张表是**按 PnP 卡面写法建的键** ⇒ 按现名查**落空** ⇒ `rarity` 静默掉成空串。
    # 值取自宝石表那一行（`:698`），另有**独立**佐证：原版卡 SO 快照 `cardRarity = 3`
    # （映射 `1..5 = common/rare/epic/legendary/special`，实算见 `build()` 末尾稀有度守卫那段）。
    # ⚠️ 与上面 `Land Rider` / `Morkai Eliminator` / `2nd Company Terminator` 那三条（B20）**完全同形**：
    #   卡名回美术资产名、宝石表按 PnP 写法建键 ⇒ 只能按 (阵营, 现名) 特判。
    # ⚠️ **别再把它放进 `RARITY_UNLISTED`** —— 那张表的语义是「宝石表里**没有**这张卡」，
    #   而这张宝石表里有（见上面 `RARITY_UNLISTED` 里 2026-10-15 那段）。
    ("Sororitas", "Simulacrum Imperialis"): "epic",
}

# 「宝石表里查不到」的卡 → 稀有度。**语义**：这张卡**在宝石表里没有对应行**，
# 所以只能由本表给值（与 `RARITY_BY_FACTION` 的区别：那张表是「宝石表**有**、只是键对不上」）。
#
# 🔴 **2026-10-16（账 `A2064` ①）就地订正（铁律 5）**：本段原来写
#     「三份 0824 表**没收录**的 **9 张**（`Dark Angels/6秘密` 5 + `Genestealer Cult/6破坏卡` 4，
#      都是手机翻拍 IMG_*.jpg，没进那套 OCR 流水线）」—— **那句读起来像现状、其实早就偏窄**：
#     下面这张表**现读 21 条**（最初那批 9 × `Dark Angels/6秘密` 5 + `Genestealer Cult/6破坏卡` 4
#     ＋ 🆕2026-09-15 那两批共 12 条，其中 `Simulacrum Imperialis` 那条 2026-10-15 已删）
#     —— 后两批**各自带了「🆕 …批」的段落说明**（见下面各段）。
#     ⇒ **别再按「9 张」读它**；条数**现数**（2026-10-16 现数 = **21**）。
# ⚠️ **本表只在【宝石表 + 真值 `cardRarity` 都够不到】时才被查**（`A2062` 起真值是第 1 层：
#    下面这 21 条里凡原版卡 SO 快照里有的，今天已经由第 1 层给值 —— 本表是**兜底**）。
# **2026-09-12 逐张对着卡面宝石核过**（拼图看底部那颗菱形宝石）：
RARITY_UNLISTED = {
    # Dark Angels 秘密卡：宝石**橙红** = special。图 `Dark Angels/6秘密/IMG_3695..3699.jpg`
    "Convoke the Circle": "special",
    "None Must Know": "special",
    "Obscure Ritual": "special",
    "Rites of Penance": "special",
    "Smothering Decree": "special",
    # Genestealer 破坏卡：宝石**浅蓝** = common。图 `Genestealer Cult/6破坏卡/IMG_3817..3820.jpg`
    "Jammed Communications": "common",
    "Poisoned Supplies": "common",
    "Improvised Barricade": "common",
    "Cult Propaganda": "common",

    # 🆕 2026-09-15 PnP 对账：这两张宝石表里没有、引擎 `rarity` 是空串，卡面实测都是 **绿 = rare**
    "Commissar Denkler": "rare",        # Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png（🔴 2026-10-15 改名：官方 `Card_Name/AM3` = `Commissar Denkler`）
    "Medic Scion": "rare",             # Astra Militarum/3部队/…（卡面印 `Scion Medic`）
    # 🔴 2026-10-17（B20）：原来这里还有第三张 `1st Company Terminator` —— 已删，两个理由：
    #    ① 它的卡名撤回成 `2nd Company Terminator` ⇒ 这个键再也不会被查中（死键）；
    #    ② 这张卡**宝石表里有**（:1018，批次 U，绿 = rare），本来就不该在「表外卡」这张表里
    #       （`RARITY_UNLISTED` 只在宝石表查不到时才用）。改走上面的 `RARITY_BY_FACTION`。

    # 🆕 2026-09-15（第二批）剩下 11 张空 `rarity` —— **宝石像素取色**（不是目视）读出来的。
    # 取色法：卡面底部菱形宝石的实心区，取饱和度最高那 1/3 像素的均值 → hue。
    # 校准：10 张已知稀有度的卡 10/10 命中（legendary=金 hue≈35 · epic=紫 hue≈275 ·
    # common=钢蓝 hue≈178 · special=橙红 hue≈12）。这 11 张全部取到实心宝石，**0 张读不清**。
    # 交叉验证：`资料/卡牌数据表` 系 0824 视觉表 + `_tmp_view/pnpcheck/*.md` 的读法 **11/11 一致**。
    # ⚠️ 名字带 `(XXX's Talent)` 的是**天赋卡**，PnP 文件名只写天赋名（`Warpforge_<N>B_<名>.png`）。
    "Lord Kakophonist": "legendary",                       # Emperor_s Children/3部队/Warpforge_37_Varius-Lord-Kakophonist.png（金 hue 35.7）
    "Duelist's Hubris (Lucius' Talent)": "legendary",      # Emperor_s Children/2天赋/Warpforge_04_Duelists-Hubris.png（金）
    "Acolyte Iconward": "rare",                            # Genestealer Cult/1督军/Warpforge_01_Iconward-Malak-Vorenth.png（绿 hue 117.9）
    "Ork Spanner": "epic",                                 # Orks/3部队/Warpforge_14_Spanner.png（紫 hue 275.2）
    "Da Bigger Dey Iz... (Mozrog's Talent)": "legendary",  # Orks/2天赋/Warpforge_02B_Da-Bigger-Dey-Iz.png（金）
    "Dok’s Toolz (Painboss' talent)": "legendary",         # Orks/2天赋/Warpforge_19B_Doks-Toolz.png（金）⚠️ 名字里是全角 ’
    "Ferocious Rage (Beastboss' Talent)": "epic",          # Orks/2天赋/Warpforge_01B_Ferocious-Rage.png（紫）
    "Special Dose (Zodgrod Wortsnagga Talent)": "legendary", # Orks/2天赋/Warpforge_12B_Speshul-Dose.png（金；卡面拼 `Speshul`）
    "Veteran Flyboy": "epic",                              # Orks/3部队/Warpforge_31_Veteran-Stormboy.png（紫；卡面印 `Veteran Stormboy`）
    "Waaagh! Energy (Weirdboy Talent)": "epic",            # Orks/2天赋/Warpforge_17B_Waaagh-Energy.png（紫）
    # 🔴 **2026-10-15 删除**：原来这里还有第 12 张 `"Simulacrum Imperialis": "epic"` ——
    #    它是**改名留下的死键**，两个理由与上面 `1st Company Terminator` 那条**完全同形**：
    #    ① 源表里这张卡现在叫 `Simulacrum Celestian`（`数据/游戏数据/card_stats.json`）
    #       ⇒ 这个键**再也不会被查中**（查表用的是改名后的 `name`）；
    #    ② 这张卡**宝石表里有**（`资料/卡牌数据表/卡牌宝石稀有度_0824.md:698`
    #       `Simulacrum Celestian | 紫 | epic`）⇒ 本来就不该在「表外卡」这张表里
    #       （`RARITY_UNLISTED` 只在宝石表查不到时才用）。
    #    ⚠️ `Sororitas` 另有一张 `Simulacrum Bearer`（宝石表 `:686`，浅蓝 common，池里 `SOR13`）
    #       —— **那是另一张卡**，别混。
    #    ⚠️ **删掉它今天不改变任何输出**（池里这张的 rarity 本来就是宝石表给的 `epic`）。
    #    ⛔ **本笔不把键改成现名** —— 那要用户/调度台裁（见 `STAT_FIXES` 里同日的说明）。
    # 🔴 **2026-10-15（`G3`）就地补记**：上面「源表里这张卡现在叫 `Simulacrum Celestian`」一句
    #    **当天就被推翻** —— 卡名已按用户裁定**撤回成 `Simulacrum Imperialis`**（并行证据 = 原版卡 SO
    #    快照 `SOR24` 的 `cardName` 本来就是它）。⇒ **删掉这条键仍然是对的**（理由②不变：宝石表里有
    #    这张卡，它不该待在这张「表外卡」的表里），**判据的落点是 `RARITY_BY_FACTION`**
    #    （见那张表末尾 2026-10-15 那条）。
    #    ⚠️ 同一天，`card_stats.json:22388` 那一格也从临时的 `"epic"` **改回了 `null`** ——
    #    本仓口径是「`card_stats.json` = OCR 原始源表，**修正一律落在生成器这一侧**」
    #    （`gen_cardface_fixes.py:35` 的措辞 + `STAT_FIXES` 其余 29 条键都保持源表原值不动）。
}

# 数值修正 —— **OCR 读错/漏读**的卡。每条都对着卡面核过，出处写在后面。
# 🔴 **2026-10-19（`A2067`）：这张表里【同时】装着两种条目** ——
#    ① **数值修正**（`cost` / `attack` / `health` / `ranged` 至少一个）；
#    ② **改名**（只带 `"name"`，见下面的 `stormlord` / `Diviner`）。
#    ⇒ 跑完的读数必须**分两栏**（见 `main()`）—— ⛔ 别把改名记成「数值修正」。
# 2026-09-12 抽 43 张对卡面时发现 OCR 有两类错，都出在那个**紫圆（远程）**上：
#   ① **把右侧的护甲盾牌读成了远程**（Baneblade：图上紫圆 12、右侧盾 2，OCR 记 ranged=2）
#   ② **整个远程圈漏读**（Veldras / Predator Annihilator / Lord Kaphrael / Haarken）→ 记 0
# 卡面四个圆的出处（原版 prefab 节点名 + 坐标，见 `CardView.cs:121`）：
#   **费用 = 右上蓝圆 · 近战 = 左下红圆 · 远程 = 左下偏右的紫圆 · 生命 = 右下绿**
#   **护甲 = 右侧那枚盾牌**（不在任何一个圆里，来自 `Armour N` 关键词）
# ⚠️ **2026-09-13 第三十二轮更正**：这几行原来把卡底说成「四个圆」、还把盾和绿框的顺序写反过
#    （`CLAUDE.md` 里更写成了「下方两圆一盾、**中间的盾是生命**」）。
#    亲读卡图核实（`Dark Angels/3部队/Warpforge_20_Deathwing-Terminator.png`）——
#    卡底是**一排四个**：**左圆(红/剑)=近战 · 右圆(紫/枪)=远程 · 银盾=护甲 · 最右绿框=生命**。
#    错因是把「量出来的形状」当成了位置描述，没逐张核。
# ⚠️ 这是**抽样**发现的，不是全量核对 —— 全池还有多少张有同类错，没人量过（见对账文档）。
STAT_FIXES = {
    # 卡名: {字段: 正确值}
    "Baneblade Tank":       {"ranged": 12},   # Astra Militarum/3部队/Warpforge_43_Baneblade-Tank.png：紫圆 12、盾 2
    "Haarken Worldclaimer": {"ranged": 2, "cost": 0},   # Chaos/1督军/Warpforge_3_Haarken-Worldclaimer.png：紫圆 2；督军费用 0（见下）
    "Lord Kaphrael":        {"ranged": 2},    # Emperor_s Children/1督军/Warpforge_01_Lord-Kaphrael.png：紫圆 2
    "Veldras the Sublime":  {"ranged": 1},    # Emperor_s Children/3部队/Warpforge_24_Veldras-the-Sublime.png：紫圆 1
    "Predator Annihilator": {"ranged": 7},    # Ultramarines/3部队/predator anihilator.png：紫圆 7
    "Smothering Decree":    {"cost": 2},      # Dark Angels/6秘密/IMG_3699.jpg：蓝圆 2（左上角那个绿色「1」是卡框装饰，两张都有）

    # ---- 🆕 2026-09-13 第三十二轮补齐的 7 处（**按 (阵营, 卡名) 定位、逐张开卡图读**）----
    # 来源：`资料/卡表核对_卡图提取/_裁定_三围.md` + `_裁定_费用.md`。
    # ⚠️ 上一轮 `_对账.md` 报的 13 处三围差异里 **8 处是假的** ——
    #    根因是 `工具/compare_cards.py` 只按**名字**建桶，跨阵营同名卡被配错了对
    #    （`Terminator` / `Terminator Champion` / `Bladeguard Veteran` 三组）。
    #    这 7 处是**逐张打开卡图亲眼读过**的，不是拿第二份 OCR 表比出来的。

    # 五处 `ranged`：旧值都偏小。⚠️ `Deathwing Terminator` 那一处
    # **正好复现了 Baneblade 的坑** —— 旧值 1 恰等于卡面护甲 1，就是「把盾读成了圆」。
    "Deathwing Terminator": {"ranged": 3},    # Dark Angels/3部队/Warpforge_20_Deathwing-Terminator.png：紫圆 3
    "Chaos Land Raider":    {"ranged": 10},   # Emperor_s Children/3部队/Warpforge_41_Chaos-Land-Raider.png：紫圆 10
    "Lhaska Szenari":       {"ranged": 2, "cost": 0},    # Genestealer Cult/1督军/Warpforge_01_Lhaska-Szenari.png：紫圆 2
    "Nemesor Zahndrekh":    {"ranged": 2, "cost": 0},    # Necron/1督军/Warpforge_3_Nemesor-Zahndrekh.png：紫圆 2
    "Neurothrope":          {"ranged": 2, "cost": 0},    # Tyranid/1督军/Warpforge_1_Neurothrope.png：紫圆 2
    # 一处 `cost`：**这张是我亲自开图复验的**（子代理先报，我另开一次确认）——
    # Aeldari/3部队/Warpforge_44_Wraithknight.png 右上角蓝色六边形清清楚楚是 `10`，
    # 同卡面还写 `Armour 2` / 紫圆 6 / 绿框 12，与表里那三项都对得上。
    "Wraithknight":         {"cost": 10},

    # ---- 🔴 2026-10-17【撤销】2026-09-15 那批「卡名按卡面印的改」（4 条，**已全部撤回**）----
    # 原判据是**三处一致**：PnP 文件名 / 源表 `ocrName` / `card_ids.json` ——
    # 🔴 **那三处全是从 PnP 派生的、不独立**（与 2026-09-16 `Veteran Flyboy` 的结案、
    #    2026-09-24 那批 19 条回滚**同一条教训**：**PnP 是印刷品，不是数字版卡名**，铁律 10 第 3 条）。
    # B20（`资料/普查产出_1017/W_B20_同族四张卡名.md`）用**游戏侧证据**逐张复核，结论：**4 条全撤**。
    # 最硬的一条是**全池普查**：源表里 89 张「美术资产名 ≠ PnP 写法」的卡，我们卡池
    # **79 张采用了美术资产名**、只有 10 张用了 PnP 名 —— 那 10 张里 **6 张就是这一批**
    # （本批 4 张 + `Bladeguard Veteran` + `Helfire`/`Awakening` 那种拼写订正）。
    #    · `Sister Dogmata`（SOR32）：卡图资产 `Sororitas_inf_Sister Dogmata` · 头像 SO
    #      `Avatar_SOR_Forge2_Sister Dogmata` · 头像贴图 `Avatar_SOR_Sister Dogmata` ·
    #      头像 SO 的 `nameTextReference` 就是 `Sister Dogmata`（**4 条一致**）
    #    · `2nd Company Terminator`（UM83）：卡图资产 · 语音 · 头像 SO · 头像贴图 **4/4 全指向 2nd**
    #    · `Morkai Eliminator`（SW29）：卡图资产名 = `SM_SpaceWolves_inf_Morkai Eliminator`
    #    · `Land Rider`（SW38）：卡图资产名 = `SM_SpaceWolves_veh_Land Rider`
    #      （⚠️ 与同一美术管线里的 `CSM_EmperorsChildren_veh_Chaos Land Raider` 只差一个字母 ——
    #       残留不确定性见报告 §⑤，**这条是本批最弱的一张**）
    # ⚠️ 撤回后 **id 不变**：`card_ids.json` 那 4 行的值已同步回改；不改会掉成自造 id，
    #    `prebuilt_decks.json` 会跟着变（硬约束：prebuilt 必须逐字节不变）。
    # ⚠️ 数值修正**与卡名无关、一律保留**：
    #    紫圆 6（`Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png` 那张图现在只当数值判据）。
    "2nd Company Terminator": {"ranged": 6},
    # 9 张督军的「费用」：卡面**费用槽是空的**（子代理逐张开图核过 9/9；对照卡同槽有蓝色六边形）
    # ⇒ 引擎里都该是 0。剩下 6 张（本表上面已有条目的并进去了）。
    #
    # 🔴 **2026-10-15（P2 相 `A1773`）就地补充：这几条的【适用条件】变了，仍然有用、但只在
    #    「真值里没有这张督军」时才生效** —— 现在督军费用走了一条**更权威的路**：
    #    `build()` 里 `trow` 那一段 = **有真值行且 `manaCost >= 0` ⇒ 取真值**（PnP 56/56 不印费用格
    #    ⇒ 已裁⑥ 的闸落到「照真值」那一档；裁决理由与对抗性验证见 `项目任务.md:498-516`）。
    #    · 上面那 9 张里 **`Njal Stormcaller` 是 SpaceWolves** —— 快照整个 SW 阵营 0 张
    #      ⇒ 它**靠这几条保持 0**（否则会退回 OCR 从「阵营徽记」误读出来的那个数）。
    #    · 其余 8 张有真值 ⇒ **被真值覆盖成 `2`**（跑完摘要「督军费用」那一段会逐张打出来）。
    #    这段注释是**曾经的**判据记录，别删（它同时是「右上角徽记 ≠ 费用六边形」那件事的出处）。
    "Njal Stormcaller":       {"cost": 0},
    "War Shaper":             {"cost": 0},
    "Terror of Vardenghast":  {"cost": 0},
    "Tervigon":               {"cost": 0},
    "Varro Tigurius":         {"cost": 0},
    # 🔴 **2026-10-17 更正：这一条原来还带 `"name": "Bladeguard Veteran"` —— 那是错的，已撤掉。**
    #   （原文写「旧表把卡名抄错了 ⇒ 同一个东西被写成了两个名字」—— 那只核了 PnP 一侧。）
    #   **原版数字版这张就叫 `Bladeguard Lieutenant`**，五条游戏侧证据（都在解包资源里，铁律 2）：
    #     · 卡图资产 `bundle_spacemarinesultramarinescardassets_assets_all/Sprite/`
    #       `SM_UM_inf_Bladeguard Lieutenant.json` —— UM 包里**没有** `…Bladeguard Veteran`；
    #       而 `Bladeguard Veteran` 是 **DarkAngels** 那张
    #       （`bundle_spacemarinesdarkangelscardassets_assets_all/Sprite/DarkAngels_inf_Bladeguard Veteran.json`）
    #     · 语音 `VO_UM_Bladeguard Lieutenant - Draw your blade for the Primarch.ogg`
    #     · 头像 SO `bundle_cosmeticsso_assets_all/MonoBehaviour/Avatar_UM_Campaign1_Bladeguard Lieutenant.json`
    #     · 教程 SO `bundle_tutorialso_assets_all/MonoBehaviour/Warpforge_TutorialStage6.json` 里
    #       `"actionType": "PlayCard (Bladeguard Lieutenant)"`（该串由卡名生成，见 `ScriptedAction__UpdateName.c`）
    #     · `数据/本地化/i18n/zh_CN.csv:697` `Bladeguard Lieutenant,~,剑卫中尉`；`:699` 另有
    #       `Bladeguard Veteran,~,剑卫老兵` = **DA 那张**（`zh_cards.json` 两行的 desc 各对各：中尉=典籍、老兵=议程）
    #     PnP `Ultramarines/3部队/Warpforge_34_Bladeguard-Veteran.png` 印的是 `Bladeguard Veteran` ——
    #     那是**印刷品上的写法**（铁律 10 第 3 条），不是数字版卡名。
    #   ⚠️ 那条改名还**凭空造出一组跨阵营同名卡**（UM/DA 都叫 Bladeguard Veteran）⇒
    #     `RARITY_BY_FACTION` 与 `cardface_fixes.json` 都得为它特判；撤掉一并消掉。
    #   ⚠️ id 仍取 `UM34`（`card_ids.json` 已同步改好；不改那行会掉成自造 id `UM_Bladeguard_Lieutenant`）。
    #   ⚠️ 同一行里另外两处修正**保留**：
    #     · `ranged` 0 是漏读 → 2
    #     · 兵种 `Infantry`（源表那一格是空的）走 `cardface_fixes.json` 的 `_manual_subtype`
    #     · 稀有度 `common` 走 `RARITY_BY_FACTION`（判据见那条的注释）
    "Bladeguard Lieutenant": {"ranged": 2},
    # ---- 🆕 2026-09-18 两张死灵督军的卡名是**美术文件名尾段**、不是卡名 ----
    # 三条独立证据一致，且 PnP 编号对得上（`Warpforge_1_` ↔ `SAU1` · `Warpforge_5_` ↔ `SAU5`）：
    #   · PnP 卡面 `Necron/1督军/Warpforge_1_Imotekh-the-Stormlord.png` 印的是 `Imotekh the Stormlord`
    #              `Necron/1督军/Warpforge_5_Orikan-the-Diviner.png`    印的是 `Orikan the Diviner`
    #   · `card_stats.json` 的 `ocrName` 两处都是全名（而 `name` 是 `stormlord`/`Diviner`）
    #   · 美术路径 `Necron_Sautekh_warlord_Imotekh the Stormlord_AA_HB.png`
    # ⚠️ 改名会**连带换 id**（id 按新名查 `card_ids.json`）⇒ `card_ids.json` 的 SAU1/SAU5 **已同步改好**；
    #    不改那两行的话会掉成自造 id `SAU_Imotekh_The_Stormlord`，丢掉原版 id。
    #    立绘**不用重导**（`import_original_art.py:431` 按 **id** 命名输出文件）。
    # 顺带修的东西：**语音表**。导入脚本按卡名认音频文件名，而音频用的是**短名**
    #   （`VO_Sautekh_Imotekh_attack.ogg`）⇒ 旧名一条都对不上，**这两张督军各 20+ 条语音整条线丢了**。
    "stormlord": {"name": "Imotekh the Stormlord"},
    "Diviner":   {"name": "Orikan the Diviner"},

    # ---- 🔴 2026-09-24【撤销】卡名订正批 —— 19 条已回滚，**别再照 PnP 卡图改卡名** ----
    # 起因：一个爱好者做的第三方复刻网站（见 `资料/第三方复刻对照_CardboardConsole.md`）的卡名清单与我们对不上；
    #       我拿「PnP 卡图 + 我们的逐张抄录表 + 那个网站」当证据，改了 19 条卡名。
    # 🔴 **全错，当天回滚。** 根因：那两个来源**都是从 PnP 派生的、不独立**；而
    #   **PnP 是印刷品，印的是「当年重印的写法」，不等于原版数字版的卡名**（铁律 10 第 3 条）。
    # ⚠️ **2026-10-18 更正（铁律 5）：上面那句原来写「✅ 正确的判据（游戏侧权威）：zh_CN.csv —— **原版客户端的本地化表**」—— **不成立**。**
#    实际：① 它由 `工具/sync_from_d2.py:97` 从 **`d:/warpforge/data/i18n`** 同步而来；② **`d:/warpforge/` 是我们自己的 Godot 复刻**（`CLAUDE.md` §一 2026-09-18 更正）；③ `CLAUDE.md` §七 实测「**原版客户端根本没有中文表**」（246,807 个文件扫中文串零命中）。
#    🔴 **2026-10-14 更正（铁律 5）**：上面 ③ **不成立** —— 原版**有**官方中文表（9,440/9,447 条非空），
#       由**远端 Addressables 包 `localization_assets_all`**（2025-07-30 快照）下发，**2026-10-14 已解开**
#       并落至 `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`。**错因** = 把「246,807 个文件扫中文串
#       零命中 · 84 个 bundle 无本地化包」这份**只证明「不在离线包体里」**的实测读成了「根本没有」；
#       `CLAUDE.md` §一·7 那一处**已就地订正**（判据全文看那段，⛔ 别在这儿抄第二份）。
#    ⚠️ **结论【不变】** —— 下面那句「它是我们自己的表、不是原版判据」**仍然成立**，
#       而且**理由更硬了**：它与**官方表**是两份不同的东西（本条上方的 `OFFICIAL_LOC_SRC` 才是官方表）。
#       唯一要换的是**措辞**：不能说「因为原版没有中文表」，只能说「因为这份 csv 不是官方产物」。
#    ⇒ **它是我们自己的表、不是原版判据**；只能当**术语一致性**的参考（⛔ 别拿它当「原版就是这么写的」）。
#    ✅ **现行判据（2026-10-19 `T03` ③ 写清，带出处）**：卡名以**解包资源里的游戏侧字段**为准 ——
#       卡图资产名 / 语音 AudioClip / 头像 SO 的 `nameTextReference` / 教程 SO（四条互相独立）；
#       **PnP 成品卡图是印刷品，只当旁证**（铁律 10 第 3 条）。出处 →
#       `资料/普查产出_1017/W_B20_同族四张卡名.md`（全池普查：89 张「资产名 ≠ PnP 写法」里 **79 张采用资产名**）。
#       `zh_CN.csv`（我们自己的表）只用于**旧名查询 / 术语一致性**，⛔ 不是卡名判据。
    #   实测：**被我改掉的那 19 个旧名，在表里全是精确的键**（连括注都在）：
    #     `Dok’s Toolz (Painboss' talent),~,医官的工具（痛医头目天赋）` · `Commissar Elan,~,政委·埃兰` ·
    #     `Medic Scion,~,医护圣选兵` · `Sisters Repentia,~,忏悔修女` · `Sylar Hexcorn,~,赛拉尔·赫克科恩` ·
    #     `Abaddons Chosen,~,阿巴顿神选`（**无撇号**）· `Extermination Protocols,~,歼灭协议`（**复数**）·
    #     `Acolyte Iconward,~,侍僧持旗者` · `Veteran Flyboy,~,飞行老兵`
    #   ⇒ **我们的卡名本来就是对的**；`(某某的 Talent)` 那个括注是**原版键的一部分**，不是我们自己加的。
    # ⛔ **以后的规矩**：**改卡名之前，先拿 `zh_CN.csv` 精确查键**（第一列，整串相等）。
    #   只查 PnP / 第三方清单 = 会改错。这与 2026-09-16 对 `Veteran Flyboy` 的结案是同一条规矩。
    # 🆕 2026-09-14（数值第二轮对账的**旁支发现**；主对话**逐张亲读卡图复核 5/5**）：
    #   🔴 **督军卡根本没有蓝色费用六边形** —— 这 5 张 Saim-Hann 督军的右上角是
    #   **阵营徽记**（深绿圆盘里一个蛇形 / S 剑纹），OCR 把它读成了数字 **5**。
    #   证据（每张都自己开图看过，顺带记下那四个数值圈）：
    #     · Aeldari/1督军/Warpforge_01_Jain-Zar.png           右上=徽记（非六边形）；红2 / 紫2 / 绿框35
    #     · Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png        同上；红2 / 紫2 / 绿框35
    #     · Aeldari/1督军/Warpforge_3_Eliac-Zephyrblade.png    同上；红2 / 紫2 / 绿框40
    #     · Aeldari/1督军/Warpforge_5_Medreyal-Ghaelyn.png     同上；红2 / 紫2 / 绿框35
    #     · Aeldari/1督军/Zrzut ekranu 2026-04-16 o 18.43.31.png（就是 `Lhykhis`）同上；红2 / 紫2 / 绿框25
    #   ⇒ 费用取 **0**：全表 **56 张督军里 42 张是 0**，而且督军**不从手牌打出**（开场上场）。
    #   ⚠️ **影响面小但不是零**：`CreatePool` 会按 `Cost` 筛卡（`Core/CreatePool.cs:133/:134/:301`），
    #      「找一张 N 费卡」那一族效果本来**有可能挑中这几张督军**。
    #   ✅ **2026-09-26 结案（本次独立复核）**：那 9 张 = `Haarken Worldclaimer` 2 · `Lhaska Szenari` 2 ·
    #      `Nemesor Zahndrekh` 2 · `Neurothrope` 2 · `Njal Stormcaller` 1 · `War Shaper` 1 ·
    #      `Terror of Vardenghast` 3 · `Tervigon` 3 · `Varro Tigurius` 3（名单由 `5245d01^`→`5245d01` 两版表还原），
    #      **已在 `e0f4927` 全部改成 0**。本次把 **13 阵营全部 56 张督军**的卡图逐张裁右上费用槽看过：
    #      **56/56 无蓝色费用六边形**（对照组 = 13 阵营各 1 张 `3部队` 已知费用卡，同槽都有，检测器命中 961–1646 像素）。
    #      ⇒ **当前表督军 cost 全 0 是对的，不必再动**。
    #      🔧 测法留档：**费用六边形的位置包络 = x[628,787] y[291,451]**（13 阵营控制卡实测），裁这个框看图即判。
    #      要查就照上面这个法子：**开图看右上角有没有蓝色费用六边形**。
    #
    # 🔴 **2026-10-15（P2 相 `A1773`）就地订正上面那句结论**（铁律 5）：
    #    上面证明的是 **「PnP 不印督军的费用」**——**这件事仍然成立、而且正是本笔的判据之一**；
    #    但由此得出的 **「所以引擎里该是 0」不再成立**：
    #      · 🔬 2026-10-15 亲开 `Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png`（**就是这 5 张之一**）：
    #        右上角是**阵营徽记**（深绿圆盘里的 Saim-Hann 蛇纹），**没有费用六边形** ✅ 与上面一致；
    #      · 而 `施工设计 §七 已裁⑥` 那句「PnP 在这一点上自相矛盾：大多数督军不印费用格、
    #        `Anvirr Keltoc` 却印 5」——**那半句是照 OCR 误读写的**（OCR 把徽记读成了 5），
    #        PnP 上**并没有那个 5**。⇒ **PnP 在这一点上不矛盾，是「统一不印」**。
    #      · ⇒ 按已裁⑥ 的闸（**PnP 上没有 ⇒ 照真值快照**）⇒ **督军 `cost` 取真值的 `2`**
    #        （`A1773`；裁决与对抗性验证见 `项目任务.md:498-516`）。
    #    ⚠️ 这几条**仍然保留**：它们的适用面缩到「**真值里没有这张督军**」那一档
    #      （`A1773` 里 `hero_cost_kept` 会把这档逐张报出来），5 张 Saim-Hann **全都有真值** ⇒ 都会被改成 `2`。
    "Jain Zar":          {"cost": 0},
    "Anvirr Keltoc":     {"cost": 0},
    "Eliac Zephyrblade": {"cost": 0},
    "Medreyal Ghaelyn":  {"cost": 0},
    "Lhykhis":           {"cost": 0},

    # ---- 🆕 2026-09-16 「逐张并排验收」尾巴批（`资料/PnP卡图_逐张对账_0915.md` §六末）----
    # 这 4 张的共同根因：**紫圈（远程）被 OCR 读成了 `armor`，`ranged_attack` 留空**
    #   ⇒ 引擎里 `ranged` 是 0。判据是 `card_stats.json` 里 `ranged_attack=null 且 armor=N`
    #   （全池只有这 4 张单位/督军命中，另 2 张命中项是改名前的重复行、已由上面两条修掉）。
    # 判据三处独立来源一致，且**主对话逐张亲读卡图复核 4/4**（铁律 7）：
    #   · `Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png`       红2 / 紫2 / 绿框30
    #   · `Genestealer Cult/1督军/Warpforge_01_Iconward-Malak-Vorenth.png` 红2 / 紫2 / 绿框35
    #   · `Orks/3部队/Warpforge_31_Veteran-Stormboy.png`                   红7 / 紫4 / 绿框6
    #   · `Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png`          红5 / 紫4 / 绿框6
    # ⚠️ **卡名别跟着 PnP 改**：这 4 张里 3 张（`Commissar Elan` / `Diviner` 那一类）
    #   🔴 **2026-10-15 就地订正（铁律 5）**：本行原写「`card_ids.json` 的原版 ID 表用的就是
    #   这个名（`AM1=Commissar Elan`）」—— **已过期**：`card_ids.json` 2026-10-13 换真值后现读
    #   **`AM3` → `Commissar Denkler`**；且官方 `Card_Name/AM3` = `Commissar Denkler`、
    #   官方 `Warlord_Lore/AM3` 写的是「**Commissar Elan Denkler**」（Elan 是名、Denkler 是姓）
    #   ⇒ **`Commissar Elan` 是原版美术/VO 资产名（只用名），不是卡名**。本会话已把卡名改成
    #   `Commissar Denkler`，并同步了宝石表 / `OCR_ARMOR_FAMILY` / `STAT_FIXES` / `zh_cards.json`。
    #   PnP 印的是**当年重印的写法**。改名只认「三处证据一致」那一条，见下面。
    "Commissar Denkler":     {"ranged": 2},
    "Acolyte Iconward":      {"ranged": 2},
    # 🔴 **2026-10-15 删除**：原来这里是 `"Simulacrum Imperialis": {"ranged": 4},` ——
    #    同样是一条**改名留下的死键**：源表里这张卡现在叫 `Simulacrum Celestian`，而
    #    `STAT_FIXES` 的键查的是**改名前的名字**（`build()` 里 `STAT_FIXES.get(name_before_rename)`）
    #    ⇒ 这一条再也不会生效。
    #    ⚠️ **删掉它今天不改变任何输出**：`ranged_attack` 已经是 **4** —— 那次数值修正是被**直接写进
    #       源表**的（`数据/游戏数据/card_stats.json` 的 `Simulacrum Celestian` 行 `null → 4`），
    #       与 `STAT_FIXES` 其余 **29** 条「源表保持原值、修正留在表里」**不是同一种形态**。
    #       两条路今天产物相同（池里 `ranged` 都是 4）⇒ **该走哪条要裁**；
    #       ⛔ 本笔只删死键，**不动源表、也不把键改成现名**。
    #    ⇒ 文件末尾新加的**孤立键守卫**（`orphan_stat_fix`）就是为让下一次改名**当场看得见**。
    # ✅ **不改名**（2026-09-16 查清并结案）。原来差点按上面那条「三处证据一致」的规矩改成
    #    `Veteran Stormboy` —— 那三处（PnP 文件名 / `ocrName` / `card_ids.json`）**全是从 PnP 派生的**，
    #    不是三个独立来源。**独立于 PnP 的游戏侧证据指向 `Veteran Flyboy`**：
    #      · 卡图资产名 `bundle_orksgoffcardassets_assets_all/Sprite/Ork_Goff_inf_Veteran Flyboy.json`
    #        （= 原版美术管线里那张卡的资产名，B20 复核补的最硬一条）
    #      · `数据/本地化/i18n/zh_CN.csv:2565` `Veteran Flyboy,~,飞行老兵`
    #      · 同表 `:3407` `Talent: Veteran Flyboy` · `:4596`/`:5790` `Companion 1/2: Veteran Flyboy`
    #      · 抽卡包 `bundle_draftpacks_assets_all/MonoBehaviour/Flyboyz.json`（`packId: GOFF_Flyboyz`）
    #  ⇒ PnP 印的 `Veteran Stormboy` 是**当年另一种写法**，不是原版卡名。**别再翻案。**
    # 🔴 **2026-10-17（B20）更正上面两条论据的分量**（结论不变，理由要改）：
    #    · `Talent: X` / `Companion N: X` 这两族键**不是独立来源** —— 实测 zh_CN.csv 的
    #      `Talent:` 键共 1202 条，与 `card_stats.json` 的 `name`（1199 条）**只差 9 条**
    #      （那 9 条是效果文字碎片）⇒ 这一族的键**是从我们自己的卡表拼出来的**，
    #      「表里有 `Veteran Flyboy` 没有 `Veteran Stormboy`」只证明**我们**当时是这么写的。
    #    · 同理 `zh_CN.csv` 的**卡名**键也来自 `card_stats.json` 的 `name`，而那个字段
    #      就是**卡图资产名**（见 `build_card_index.py`：卡名 = 文件名最后一段）⇒ 它不是第四来源。
    #    ⇒ 「PnP 写法不是数字版卡名」这条结论**靠卡图资产名一条就够了**（本文件 B20 那批同此）。
    # ⚠️ 推论：那条「三处证据一致就改」的规矩**只在有独立于 PnP 的游戏侧资产名背书时才安全**。
    #    ✅ **2026-10-17（B20）已对那 4 张照这个法子各查了一遍，结论是四条全撤** —— 见上面
    #    「2026-10-17【撤销】」那一段，别再把它们当「已按 PnP 改掉、没查过游戏侧」的例子。
    "Veteran Flyboy":        {"ranged": 4},
}

# 🔴 **`card_stats.json` 的 `armor` 一族 —— 显式期望**（`A2043`，2026-10-15 加）
#
# 要守的不变式 = 「源表里 `armor` 列**有值**的那些行」**正好是下面这 6 张**（**(阵营, 卡名)** 比，
# ⛔ **顺序无关**）。守卫实现在 `build()` 里（搜 `ocr_armor_family` / `armor_family_bad`），
# 🔴 **进 `_bad`** ⇒ 不符就 rc=1、**一个字节都不写**。
#
# 为什么这一族非守不可：`armor` 列 **1207/1213 是 null、只有这 6 行有值**，而这 6 个值
# **全部是错的** —— 它们是**卡面紫圈（远程）**的值被 OCR 读进了 `armor`
# （6/6 逐张开 PnP 核过，见 `资料/普查产出_第三十二会话/R8_card_stats审计.md` §一）。
#   其中 `2nd Company Terminator` 与 `Bladeguard Lieutenant` 卡面**真有银盾、真值应为 1**
#   （本笔 ⛔ 不改源表 ⇒ 那两条留在源表里，见下面「不改源表」那条）。
# `gen_cards_engine.py` 把「`ranged_attack=null` 且 `armor≠null`」**当这一族的判据在用**
# （见上面 `STAT_FIXES` 里「紫圈（远程）被 OCR 读成了 `armor`」那一段），`STAT_FIXES` 再逐张**按卡名**
# 把 `ranged` 补回去。
# 🔴 **谁哪天把源表「顺手修对了」（把这几行的 `armor` 清成 null）⇒ 这一族就圈不出这几张 ⇒
#    那条家族判据**静默失效**，症状 = 这几张的 `ranged` 重新变 **0**、**打不到人**，
#    而卡表里「看起来就像这张卡本来就是这个数」⇒ 必须**在这一刻红**。**
# ⚠️ **用户 2026-10-15 拍板：⛔ 不改源表** —— 改了会拆掉上面那条判据不变式；且
#    `工具/gen_cardface_fixes.py:35` 把这份文件叫「OCR 原始源表」，`STAT_FIXES` 其余 29 条键
#    也**全都保持源表原值不动**（那条被改掉的是**另一种形态**，见 `STAT_FIXES` 里 2026-10-15 那条）。
# ⚠️ 每项 = **`(阵营, 卡名, 家族判据命中?)`**。第三格 = 那行在源表里是不是
#    `ranged_attack is None 且 armor is not None` —— 即「**这一张是不是靠家族判据圈出来的**」。
#    🔴 **现读 6 行里只有 5 行命中**（逐行实测，见下），**不是 bug**：
#      · 5 行**命中**（`ranged_attack = null`）⇒ `STAT_FIXES` 按卡名逐张把 `ranged` 补回去；
#      · `Simulacrum Celestian` **不命中** —— 那一行的源表 `ranged_attack` **早就被直接写成 4** 了
#        （`null → 4`，是**当年直接改源表**那一次，`STAT_FIXES` 里那条同名的键 2026-10-15 已删、
#        日志留在 `STAT_FIXES` 那段 2026-10-15 的注释里）。⇒ 它的 `armor=4` 是**唯一一个
#        「家族判据自己圈不出来」的错值行**，**这正是这条守卫非立不可的直接证据**。
#    ⇒ 玩法：把三格**一起**比（集合相等）。这样任何一格变了都红 —— 包括
#      「谁把某一行的 `ranged_attack` 填上 ⇒ 那一行从「命中」变成「不命中」」这种**最隐蔽**的改法。
OCR_ARMOR_FAMILY = (
    ("AstraMilitarum", "Commissar Denkler",      True),
    ("Genestealers",   "Acolyte Iconward",       True),
    ("Goff",           "Veteran Flyboy",         True),
    ("Sororitas",      "Simulacrum Celestian",   False),
    ("Ultramarines",   "2nd Company Terminator", True),
    ("Ultramarines",   "Bladeguard Lieutenant",  True),
)
# ⚠️ **卡名别名 —— 只吸收「名字」这一种变化**：源表里 `Sororitas` 那一行的卡名**可能正被另一个
#    写手改**（`Simulacrum Celestian` ⇄ `Simulacrum Imperialis`；2026-10-15 现读是**前者**）
#    ⇒ 两个名字**都算命中**，免得**假红**把一次合法改名拦下来、把生成器卡死。
#    🔴 **2026-10-15 本笔收尾前实测：这个改名真的发生了** —— 开工时源表是 `Simulacrum Celestian`，
#      当天（本笔写完守卫之后）被改成 `Simulacrum Imperialis`（`ocrName` 仍是旧名）⇒
#      池里那张卡的名字跟着变（`SOR24`）。**若没有这条别名，守卫会当场假红、把那次改名卡死。**
#      ⚠️ **但那次改名顺手带出一个真缺陷**（不属本笔、已报调度台）：这份卡的 `rarity` 从 `epic`
#      掉成 `''` —— 宝石表 `资料/卡牌数据表/卡牌宝石稀有度_0824.md:698` 的键是**旧名**
#      `Simulacrum Celestian`，改名后**按名字 join 落空**。⚠️ **宝石表那一层没有孤立键守卫**
#      （本文件只给 `cardface_fixes` / `STAT_FIXES` 立了），所以它**静默**掉了。
#    ⛔ 成员增删 / `armor` 列被改 / 别的行改名 —— **照旧红**（那些才是这条守卫要挡的）。
OCR_ARMOR_FAMILY_ALIAS = {
    ("Sororitas", "Simulacrum Imperialis"): ("Sororitas", "Simulacrum Celestian"),
}

# `hasStats=false`（OCR 没读到数值）里**确证是真卡**的少数几张 —— 补上费用后照常收。
#
# 为什么需要这个白名单：源表里 `hasStats=false` 的 81 张**绝大多数是真噪音**
# （`Normal Conditions` ×7 / `Normal` ×3 / `Default Conditions` / `v2` / 一堆环境词
#  `Acid Rain` `Thunderstorm` `Solar Eclipse` —— 只有名字、无 desc、无立绘、无 subtype），
# 整批丢掉是对的。但里面混着真卡，`Dark Pact of Fate` 就是。
#
# **收录依据（四条独立证据，缺一不可）** —— 2026-09-12 核：
#   ① 源表里它有正式卡名 + `subtype: "Dark Pact"`（和另外三张同族卡同一个 subtype）
#   ② 它**有中文翻译**（`zh_cards.json` → 「命运黑暗契约」），那 7 张真噪音只有「正常条件」这种通用词
#   ③ 同 subtype 的另外三张（Blood / Excess / Resilience）**在我们的卡表里，全是 `cost: 1`**
#   ④ 它的效果文字 `Give +2 Health and Camouflage` 与规则书 :179「命运：+2 生命与伪装」**完全一致**
# ⇒ 费用取同族值 1（③ 是唯一来源，**不是猜**：同 subtype 同阵营同稀有度的四张是一套）
#
# 🔴 **2026-10-19 就地订正（铁律 5）**：上面 ③ 推出的 `cost: 1` **已作废** —— 用户当天裁定这 4 张
#    `Dark Pact of *` 是**战术卡触发生成的契约/附魔模板**、**没有费用**；当前 build 的卡 SO
#    （`资料/普查产出_1018/allcards_1350/cards_raw.json`，`uniqueId` 直配这 4 张）里
#    `manaCost = -1` —— 原版「**不可直接打出**」的哨兵（`Enchantments` SO 的 markOfChaos 映射：
#    `10→Blood` / `20→Resilience` / `30→Excess` / `40→Fate`，`50` = 四选一随机）。
#    ⇒ 本表这条只保留「**把 Fate 收进卡池**」的作用（`hasStats=False` 白名单），
#      `cost` 最终由 `build()` 的 SO 真值层落成 **`-1`**（见那一段的 2026-10-19 注释）。
STATS_EXCEPTIONS = {
    "Dark Pact of Fate": {"cost": 1},
    # 🆕 2026-09-15 PnP 对账：源表里这条 `hasStats=False`（无数值）⇒ 被整个跳过，
    #    卡池里**根本没有这张**。卡面实测：费 4 · 2/5/—/5 · Infantry · rare · `Long Range. Strike: …Markerlight 2`。
    #    ⚠️ 源表里它的名字是 `Fire Warrior Sniper`（贴图名也是），**卡面印的是 `Fire Warrior Marksman`** ——
    #       已按卡面改名（铁律 7）。
    "Fire Warrior Marksman": {"cost": 4, "attack": 2, "ranged_attack": 5, "health": 5,
                              "rarity": "rare", "subtype": "Infantry",
                              "keywords": ["Long Range", "Strike"],
                              "desc": "Long Range. Strike: If the target survives, give it Markerlight 2",
                              "hasStats": True},
    # 🆕 2026-10-15（P1 收尾笔 · 账 `A1783` 的那 1 张真新增）：源表里这条**早就在**
    #    （`card_stats.json` 的 `Infestation Node` 行：`decks: ["LEV_InfestationNode"]` ·
    #     `art: …/Tyranid_Leviathan_strat_Infestation Node.png`），但 `cost/desc/rarity/subtype` 全 null
    #     + `hasStats=false` ⇒ 在 `build():1016` 被整条跳过 ⇒ **卡池里一直没有这张**。
    #     ⇒ 按本表的既定用法「补上数值后照常收」补进来（**不是**往源表插一行）。
    #
    # **收录依据（五条独立证据）** —— 2026-10-15 核，判据来源逐条给出：
    #   ① 原版 `allcards`（`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`）有 `uniqueId: TL40`：
    #      `cardName "Infestation Node"` · `m_Name "T7 Leviathan-Infestation Node"` · `manaCost 2` ·
    #      `cardType 20`（`CardTypeOptions.Tactic`）· `cardRarity 3`（`CardRarity.Epic`） ·
    #      `cardImageReference.m_SubObjectName "Tyranid_Leviathan_strat_Infestation Node"`
    #   ② 官方本地化（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json`）：`Card_Name/TL40` = (`Infestation Node`, `感染节点`) ·
    #      `Card_Description/TL40` = (`Heal 2 to a friendly troop and create a copy of it in your hand`, 中文齐)
    #   ③ 原版**抽包** SO `bundle_draftpacks_assets_all/MonoBehaviour/Infestation Node.json` 的 `cardIds` 里有 `TL40`
    #      （`packId = LEV_InfestationNode` = 源表那条 `decks` 值 ⇒ 两处自洽）
    #   ④ 立绘本地有：`bundle_tyranidsleviathancardassets_assets_all/Texture2D/Tyranid_Leviathan_strat_Infestation Node.png`
    #   ⑤ 我们有它的中文名（`zh_cards.json` → 「侵染节点」），那 74 张真噪音只有「正常条件」这种通用词
    # ⚠️ **没给 `subtype`**：卡面那行橙字**本地取不到判据** —— PnP 成品卡图那一套（1118 张）**没有这张**
    #    （`d:/2/Warpforge部队卡片/` 全树无 `Infestation`），真值里 `CardTraits`/`ShowTraits` 都是空、
    #    `cardRaceTextReferenceId` 全库 1194 行**全是空串**、`_stratagem` 全库同为 0、`spellType` 同为 0
    #    ⇒ **没有可推出的口径**（实测：`_strat_` 立绘名与 subtype 完全无关，`Spell` 245 / `Tactic` 57 / …）。
    #    ⇒ 宁可留空（卡面不画那一行），**不猜**（铁律 2 / 铁律 11 例外①）。
    "Infestation Node": {"cost": 2, "rarity": "epic",
                         "desc": "Heal 2 to a friendly troop and create a copy of it in your hand",
                         "hasStats": True},
}

# ============================================================================
#  卡面逐张核对修正表（2026-09-13 加）—— **`subtype` 与 `keywords` 两列**
# ============================================================================
#
# 为什么需要（用户 2026-09-13 指出并核实的）：
#   ① **`subtype` 串列**：`card_stats.json` 把**卡的类型**（`Unit`/`Troop`/`Soldier`）
#      填进了**字段**列。而卡面「效果文字下面那行橙色小字」才是字段
#      （部队卡写兵种 `Infantry`/`Vehicle`/`Beast`/`Daemon`…，防御卡写 `Defence`，
#       督军写 `Warlord`，有些计策写 `Dark Pact`/`Overlord Power`/`Combat Elixir`…）。
#      **字段正是 `选/造/给一张 <字段> 卡` 的筛选依据** —— 错了就**筛错卡**：
#      `Hunting Wolf` 是**兽**不是兵（`Draw a Beast` 抽不到它）、`Daemonette` 是**恶魔**不是兵。
#   ② **关键词数值被吃掉**：卡面写 `Armour 1. Blast 2`，我们的 `keywords` 是 `['Armour','Blast']`
#      —— 数字没了，`KwValue("armour")` 取 0 → **护甲实际不生效**。
#   ③ **关键词整个缺失**：`Tide N`（兽人 4 张）/ `Regeneration N` / `Blast 3` 等。
#
# 数据从哪来：`Unity/资料/卡表核对_卡图提取/_合并总表.md` —— **1118 张卡逐张看卡图独立抄的**
#   （29 个子代理，**全程禁止参考任何已有数据**，每份都自证「行数 == 清单张数」）。
#   算出修正条目的脚本是 `Unity/工具/gen_cardface_fixes.py`，它只收**证据确凿**的：
#     · 字段：只改「卡面明确印了那行橙字」的（印 `—`/没印的**不动**，宁缺毋滥）
#     · 关键词：只认「效果动词**之前**的声明区」；**效果里给别人加的不算**
#       （实测排掉了 5 处：`Adjacent units have Armour 1` / `Gain Blast 6` / `Enemies have Vulnerable 1`）
#
# 对账与抽验记录见 `资料/卡表逐张核对_与对账.md`（我另外亲自开图核过 3 张）。
CARD_FACE_FIXES_SRC = r"d:/4/Unity/数据/游戏数据/cardface_fixes.json"


def load_cardface_fixes():
    """读卡面修正表 → **两个表**：`(按卡名, 按阵营+卡名)` —— 见下面「同名卡」那段。

    🆕 **2026-09-15：支持 `"<阵营>/<卡名>"` 这种键**（例 `"Ultramarines/Terminator"`）。
    为什么加：修正表原来**只按卡名查**，而卡池里有**同名跨阵营**的卡
    （`Terminator` / `Terminator Champion` / `Maulerfiend`），
    于是「改一张会连带改另一张」—— 实测 `Ultramarines/Terminator` 的 desc 被
    `EmperorsChildren/Terminator` 的值串掉了（`资料/PnP卡图_逐张对账_0915.md` §四·B）。
    **同名卡要用带阵营的键**；普通卡照旧用裸卡名。带阵营的**优先**。
    🔴 **2026-10-17 实测更正**：这条原来写「卡池里有 **5 组**（`Terminator` / `Terminator Champion` /
    `Aggressor` / `Maulerfiend` / `Bladeguard Veteran`）」—— 两处已过时：`Aggressor` 那组
    2026-09-16 查明是重复行、已从源表删掉；`Bladeguard Veteran` 那组是 2026-09-15 那次改名
    **自己造出来的**、2026-10-17 已撤回（见 `STAT_FIXES` 那条更正）。
    **实测当前 = 3 组**：`Terminator` / `Terminator Champion` / `Maulerfiend`。
    文件不在就返回空表（不静默改数）。

    ⚠️ `desc` 这一列是 **2026-09-13 第三十三轮**加的：那批卡的**效果文字里被 OCR 丢掉了图标**
    （卡面写 `Gain ☀2`，我们只剩 `Gain 2`；见 `cardface_fixes.json` 的 `_manual_desc_note`）。
    修的是**文本**不是数值，所以走这张表、不走 `STAT_FIXES`。
    """
    if not os.path.exists(CARD_FACE_FIXES_SRC):
        print("⚠️ 找不到卡面修正表 %s —— 这次**不做**字段/关键词修正" % CARD_FACE_FIXES_SRC)
        return {}
    with open(CARD_FACE_FIXES_SRC, encoding="utf-8") as f:
        raw = json.load(f)
    out = {}
    by_fac = {}                       # {"<阵营>": {卡名: {…}}} —— **同名卡走这一支**
    for col in ("subtype", "keywords", "desc", "descZh"):
        for k, v in (raw.get(col) or {}).items():
            if "/" in k:
                fac, nm = k.split("/", 1)
                by_fac.setdefault(fac.strip(), {}).setdefault(nm.strip(), {})[col] = v
            else:
                out.setdefault(k, {})[col] = v
    # ⚠️ `descZh` 的应用点必须在**中文表写入之后**（见文件末尾那段）—— 否则会被 `zh_cards.json` 覆盖。
    return out, by_fac

# 引擎需要的字段。`art`/`voice`/`ocrSrc`/`face`/`factionId`/`decks`/`tier` 全部丢掉。
KEEP = ("name", "type", "cost", "attack", "health", "ranged_attack",
        "keywords", "desc", "faction", "rarity")

# 卡牌类型白名单 —— 引擎认识这五种（见 CardDef.Type）
# ⚠️ `enchantment` **不是 OCR 源表里的取值**：它只由下面 `build_enchantment_entries()` 从
#    `cards_raw.json` 的 `cardType 20` 造出来（🆕 2026-10-25 引擎线 `X20`）。
#    ⛔ 别往 `card_stats.json` 里写 `type: enchantment` —— 那会让主循环当真去跑它那一整套 OCR 层
#    （稀有度宝石表 / 卡面修正 / art 守卫），而那些层对隐藏卡**全都没有判据**。
TYPES = ("unit", "tactic", "hero", "defence", "enchantment")


def _norm_name(s):
    """卡名归一化：去掉大小写、空格、标点和各种连字符（含 U+2011 不换行连字符）。"""
    return re.sub(r"[^a-z0-9]", "", (s or "").lower())


def load_rarity():
    """读「卡牌宝石稀有度_0824.md」→ {卡名: 稀有度}。

    **为什么必须覆盖**：`card_stats.json` 的 `rarity` 字段有三类毛病 ——
      ① 239 张是空串（OCR 没读出来）
      ② 102 张写成了 `defence`（那是**卡牌类型**串到稀有度字段里了，不是稀有度）
      ③ **140 张把非传说卡写成了 `legendary`**（2026-09-12 发现的，见调用处的更正注释）
    而这张表是逐张看卡面宝石颜色判的，1118/1118 全覆盖，是唯一的权威来源。
    卡组编辑要用稀有度做筛选和卡表行底色，**卡框还要按它取 tier1–4**，所以这一层不能省。
    """
    tbl = {}
    if not os.path.exists(RARITY_SRC):
        return tbl
    with open(RARITY_SRC, encoding="utf-8") as f:
        for line in f:
            if not line.startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) < 5 or cells[4] not in RARITIES:
                continue
            tbl[cells[2]] = cells[4]
    return tbl


def make_rarity_lookup(tbl):
    """两级查表：①原名 ②归一化名（去大小写/空格/标点）③编辑距离兜底。

    ②③ 是必要的 —— OCR 出来的卡名和宝石表里的写法常有出入：
    `Helfire Pit` vs `Hellfire Pit`、`Land Rider` vs `Land Raider`、
    `Tyrannic War Veteran` vs `Tyranic War Veteran`。没有这两级的话有 341 张补不上。
    ③ 的阈值卡在 0.88，宁可漏也不要错配（错配会让筛选和底色看起来对、其实错）。
    """
    by_name = dict(tbl)
    by_norm = {}
    for k, v in tbl.items():
        by_norm.setdefault(_norm_name(k), v)

    def lookup(name):
        """返回 (稀有度, 匹配方式)；查不到返回 (None, None)。"""
        if name in by_name:
            return by_name[name], "原名"
        n = _norm_name(name)
        if n in by_norm:
            return by_norm[n], "归一化"
        cand = difflib.get_close_matches(n, list(by_norm), n=1, cutoff=0.88)
        if cand:
            return by_norm[cand[0]], f"模糊~{cand[0]}"
        return None, None

    return lookup


# ---- 稳定卡 id（2026-09-13 第三十三轮）---------------------------------------------------
# 为什么要它：卡表一直**拿卡名当身份**（`CardDatabase.cs:75` 的注释自己写着「真出重名再加
# faction 前缀」，而重名早就出了 4 组）。后果有三处，全是**静默的**：
#   ① 费用修正按卡名匹配（`RuleCore.CostOf`）→ 同名卡**一起降价**
#   ② 临时卡 / 复制品与原件是**同一个 CardDef 对象**，分不出「哪一张」
#   ③ 卡组存档存的是**卡名**，跨阵营重名时解析到错的那张
# ⇒ 这一层给每张卡发一个**唯一且稳定**的 id。
# 🔴 **2026-10-18（`W25`）现行做法 = 按原版 `uniqueId` 直取**（`load_direct_ids` / `pick_direct_id`）：
#    `card_stats.json` 的 `id` 列 1,213/1,213 全是 `null`（⛔ 不许改源表）⇒ 只能拿
#    **(阵营, 卡名)** 去真值侧反查 `uniqueId`；落位唯一就**直取**。
#    **名级联（`pick_truth_id` / `pick_id`）降为交叉校验**：直取与它不一致必须出声（见 `build()`）。
#    现池 **1127/1127 直取成功、自造 0 张**（`build()` 末尾有硬断言，见「生成期自检」那段）。
#
# **原版 id 从哪来、可信到什么程度**（都实测过，别重新挖）：
#   · `数据/游戏数据/decklists.json`（236 副原版预组牌）里**真的带 id** ——
#     `heroId:"AM3"` + `cardIds:["AM12",…]`。实测 **5393 个 id 里 13 个前缀与阵营 1:1**
#     （AM=AstraMilitarum 321 · ASH=SaimHann 454 · BL=BlackLegion 439 · UM=Ultramarines 583 ·
#      DA=DarkAngels 352 · EC=EmperorsChildren 308 · GSC=Genestealers 321 · GOF=Goff 495 ·
#      TL=Leviathan 506 · SAU=Sautekh 532 · SOR=Sororitas 352 · SW=SpaceWolves 308 · TAU=TauEmpire 352）
#     —— **前缀↔阵营这一列是原版数据，不是我们猜的**。
#   · `card_ids.json`（996 条 `id → 卡名`）的 note 自称「自建: PnP 卡表编号=游戏卡ID 偏移0」，
#     所以 **id↔卡名 这一列是推出来的，要核**。核法：拿 `D:/2/Warpforge部队卡片/<阵营>/` 下
#     的 `Warpforge_NN_<卡名>.png` 对（⚠️ 编号在各分类目录下**会重号**，只能按「同编号候选集」比）：
#     实测 **818 处逐字一致 · 19 处写法差异 · 159 处编号在卡图里找不到**。
#     那 19 处的绝大多数是**同一张卡的不同写法**（`Sylar Hexcorn`↔`Sylar-Hexscorn` ·
#     `Rusted Vents`↔`Rusted-Vent` · `Gauss Warrior`↔`Gauss Reaper Warrior`），不是配错。
#     ⇒ **结论：id↔卡名 基本可信**（不是逐条验过），所以这里**只认「归一化卡名逐字相等」**的匹配，
#       对不上的**不猜**，自造 id 并在跑完的报告里列出来。
IDS_SRC = r"d:/4/Unity/数据/游戏数据/card_ids.json"
# 自造 id 的**全量清单**（跑一次自检/生成器就重写一份）—— 给「哪些卡没有原版 id」留证据
MADE_LIST = r"d:/4/_tmp_view/card_ids_made.txt"

# ---- 真值 id 索引（2026-10-14 · P1 相 `A1782` / `A1778` / `A1783` / `A1800`）-------------------
# **为什么在 `card_ids.json` 之外单开一层**：上面那张表只能按「卡名逐字相等」查，实测有三类卡必然落空 ——
#   ① 我们池里的卡名有一部分是**美术 / VO 资产名**（Godot 期 `d:/warpforge/data/cards.json` 带过来）
#      ⇒ `Commissar Elan` ↔ 官方 `Commissar Denkler` · `Veteran Flyboy` ↔ `Veteran Stormboy` …
#      **逐字比一定落空**（`A1779` 查实的根因）；
#   ② 原版 259 张卡的 `uniqueId` 是 **32 位 GUID**，而 `pick_id()` 只认「前缀 + 纯数字」
#      ⇒ 表里就算收了也挑不出（`A1800` 的第一根因：「`mapping` 里 0 个 GUID 形键」）；
#   ③ 同名组里「**基准卡**」与「附魔 / 教程副本」同名（`Uge Choppa` = 本体 + `_enchantment` 副本）
#      ⇒ 按名取会把副本当本体。
# ⇒ 这三件事**只有真值侧的数据能判**，所以在 `工具/gen_card_id_truth_index.py` 里**一次性算成索引**
#   （级联 join ①官方 `Card_Name/*` 英文值 → 键 ②真值 `m_Name` 尾巴 ③真值 `cardName`；
#    `base` = 基准卡；`army` = 真值 `cardArmy`，与源表 `factionId` **同口径**）。
#   判据原文 → `Unity/资料/普查产出_第三十会话/乙档六笔_判据查证.md` §`A1800`；
#   施工图（P1 在五相里的位置）→ `Unity/资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §一。
# 🔴 **2026-10-19 订正（铁律 5 · 账 `A1848` 的文档陷阱）：下面这条「快照年代限制」已过期。**
#   原文 = 「⚠️ **快照年代限制**：2025-07-30 之后新增的卡（SpaceWolves 68 / Ultramarines 51 …）索引里没有
#   ⇒ 那些卡**照旧走下面那条老路**（`card_ids.json` → 自造）。这是预期，不是缺陷。」
#   **K06（2026-10-18）换源之后它不成立**：`工具/gen_card_id_truth_index.py` 的 `TRUTH` 与下面
#   `TRUTH_CARDS_SRC` 的两处都已换到**当前 build `allcards_1350`（1,351 张）**（换源段见本文件
#   `TRUTH_CARDS_SRC` 上方那段 `K06` 注释 + `资料/普查产出_1018/allcards_1350/报告.md`）
#   ⇒ **现池 1127/1127 直取、自造 0**，没有「新卡只能走老路」这回事。
#   若哪天索引退回旧快照，表现是**逐张**掉进「名级联兜底」或自造：跑完摘要会把 `id_cascade` /
#   `id_made` 逐张列出来，自造还会触发末尾的硬断言（非零退出、一个字节都不写）。
TRUTH_IDX_SRC = r"d:/4/Unity/数据/游戏数据/card_id_truth_index.json"

# ---- 🔴 真值卡 SO 快照本体（2026-10-15 · P2 相：`A1773` / `A1776` / `A1799`）-------------------
# 上面那份 `card_id_truth_index.json` 只是**它派生出来的 join 索引**；本相要的是**字段值**，
# 所以直接读本体。同一份文件、同一年代口径（Unity 2022.3.38f1 · 缓存 **2025-07-30**）。
#   · 结构 = JSON **数组**，1195 个元素；1194 个 `RawCardScript` + 1 个 `AddressablesBuildNumber`
#     （后者**没有 `uniqueId`/`cardName`/`cardType`** ⇒ 加载时按 `uniqueId` 建键会自然把它排除）。
#   · 键 = `uniqueId`（`AM12` 这种短码，或 32 位 GUID）。
#   · 判据原文 → `资料/普查产出_第二十七会话/allcards_未挖字段盘点.md`（§2 逐字段总表）。
#   · 到手经过 → `d:/2/远端缓存_旧profile_1013/README.md`。
#
# 🔴🔴 **本相的第一纪律 = 「每一处换值先过一道闸」**（用户 2026-10-15 拍板总口径 ·
#   `资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §七 已裁⑥）：
#
#   | 情形 | 取谁 |
#   |---|---|
#   | **PnP 成品卡图（`d:/2/Warpforge部队卡片/`）也印了的内容** | **PnP 优先** |
#   | **PnP 上没有 / 印得自相矛盾的** | **照真值快照** |
#
#   ⇒ **快照比我们要复刻的客户端【旧】**（独立判据：快照 `cardArmy` 只有 12 个阵营、
#     **整个 SpaceWolves 0 张**，而 PnP 里有 SW 督军）⇒ **照快照整列覆盖 = 把新版的数换成旧版的数**。
#   🔬 **本相实测（不是推断）**：2026-10-15 逐张开 PnP 成品图核过 **13 张**「我们 vs 快照」有出入的卡
#     （`Kommando` 7/8/4/7 · `Metamorph Leader` 6/3/4/5 · `Strike Team` 4/2/6/5 · `Acolyte Heavy` 4/5/2/5 ·
#      `Big Shoota Boy` 6/3/6/6 · `Avenging Zeal` 费 12 · `Skrag Every Stash!` 费 3 ·
#      `Skyborne Deployment` 费 1 · `Witchfire` 费 0 · `Waaagh! Energy` 费 0 · `Tactical Insight` 费 0 ·
#      `Abominant` 盾 1 · `Hulking Aberrant` 盾 1 · `Rukkatrukk Squigbuggy` 盾 1），
#     **13/13 = 「我们 == PnP」且「快照 ≠ PnP」** ⇒ **三围/费用/护甲一律保留我们的值**（那就是 PnP 的值）。
#   🔴 **2026-10-18（`W` 笔）就地补注（铁律 5）**：上面那条「三围/费用保留我们的值」**只适用于
#     2022 时代快照那一版**（那次核的 13 张用的就是它）。**当前 build（`allcards_1350`）的 SO 已被裁定为真值** ——
#     2026 build 把督军生命统一下调（35→25 ×39 · 40→30 ×11 · 30→20 · 35→30），那是**版本变化、不是我们抄错**
#     ⇒ `health` / `cost` / `attack` 三列改按本快照覆盖（见 `build()` 里「SO 数值真值覆盖层」那段）。
#     判据 → `资料/普查产出_1018/R_督军10血成因.md` §1 结论 A。`ranged` / `desc` / `keywords` 仍照 PnP。
#   ⇒ **本相真正从快照取的，只有「PnP 不印」的那几样**：**督军 `cost`**（PnP 56/56 不印费用格 ——
#     右上角是**阵营徽记**，`Anvirr Keltoc` 那张亲开图核过）、**`CardTraits`**（补空那一层）、
#     **`armour` 数值兜底**、以及 **A1799 那 10 个 SO 字段**（PnP 上根本没有的东西）。
# 🔴 **2026-10-18（`K06`）换源**：原来是 2022 时代快照（`cards_1195_2022时代.json`，1,195 张）——
#   池子里 118 张比它新的卡在真值列上全是缺值。当前 build 的 `allcards` 已找到（**1,351 张**）
#   ⇒ 换过去；形状 `{bundle,count,cards:[{…,full_raw}]}` 由 `load_truth_cards` 展平。
#   判据 → `资料/普查产出_1018/allcards_1350/报告.md`。
TRUTH_CARDS_SRC = r"d:/4/Unity/资料/普查产出_1018/allcards_1350/cards_raw.json"

# ==========================================================================================
#  🆕 2026-10-19（第五十五会话 · 引擎线 `E12` · 战斗线 B3/B4 的接口）：**逐候选动画时间**
# ==========================================================================================
# 原版多候选「同一拍全播」的原因：`cardAbilities[].abilityLogic[].cardAnimations[]` 每一条
# 都是一件 **`CardAnim` SO**（`CardAnim.animInfo`，`dump.cs:20403-20411` 的 `AnimInfo` 类），
# 它的时间字段全在资产里，我们以前**一条都没读**：
#   · `vfxDelayTime`(0x24) · `startDelay`(0x28) · `timeAtStartPos`(0x2C) ·
#     `timeMoving`(0x30) · `timeAtEndPos`(0x34)；
#   · **时长** = `AnimInfo.GetAnimDuration()`（`AnimInfo__GetAnimDuration.c:3-6`）=
#     `timeMoving + timeAtStartPos + timeAtEndPos + vfxDelayTime`（**`startDelay` 不在里面**）。
# 原版执行链：`AbilityLogic.PlayAnim` 给每条 `cardAnimations` 压一条 `BattleActionType.playAnim(=11)`
#   （`AbilityLogic__PlayAnim.c:232/273` → `BattleManager__AddCardAnim.c`），
# 队列**一次只弹一条**（`BattleManager__ResolveActionFromQueue.c:44-76`），
# 每条动画的协程**等完 `GetAnimDuration` 才结束**（`BattleManager._ResolveAnim_d__466__MoveNext.c:200-210`
# 的 `WaitForSeconds(GetAnimDuration)`）⇒ **候选之间是串行**：第 n 条的起点 = 前 n-1 条的时长之和。
#   ⚠️ 这不是「拍一个固定间隔」—— 每条的时长都从它自己的 SO 读（本脚本不发明任何常数）。
#   ⚠️ 本轮**只落数据**：消费端 `Core/VfxMap.cs`（`CardPresentation/**`）不属本批白名单，
#      它该怎么用写成接口需求（`资料/普查产出_1019/E12_报告.md`）。
# GUID → 资产：`guid_map_bundles.tsv`（新表，**直接枚举本地束**；`className == CardAnim` / `type == GameObject`）；
# 资产内容：`d:/2/新解包资源/assets_full/bundle_<bundle>/MonoBehaviour/<name>.json`。
ANIM_GUID_MAP_TSV = r"d:/4/Unity/资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv"
ANIM_ASSETS_DIR = r"d:/2/新解包资源/assets_full"
ABILITY_TRIGGER_STUB = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs"
# 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`UnitConditions` 枚举桩 ——
#   本列 `unitConditions` 的 `name` 字段从它反查（⛔ 不在脚本里抄一份枚举表：两份迟早不一致）。
UNIT_CONDITIONS_STUB = r"d:/2/Warpforge_code/Scripts/Assembly-CSharp/UnitConditions.cs"

# id 前缀 → 我们卡表里的阵营名。**来源是 decklists.json 的实测统计**（见上面那张表）。
# 别的阵营名一律不许出现在这里 —— 加阵营要连证据一起补。
ID_PREFIX = {
    "AM":  "AstraMilitarum",
    "ASH": "SaimHann",
    "BL":  "BlackLegion",
    "DA":  "DarkAngels",
    "EC":  "EmperorsChildren",
    "GSC": "Genestealers",
    "GOF": "Goff",
    "TL":  "Leviathan",
    "SAU": "Sautekh",
    "SOR": "Sororitas",
    "SW":  "SpaceWolves",
    "TAU": "TauEmpire",
    "UM":  "Ultramarines",
}
PREFIX_OF = {v: k for k, v in ID_PREFIX.items()}

# 自造 id 的形态：`<前缀>_<卡名 slug>`。**故意长得和原版 id 不一样**（原版是「字母前缀+纯数字」，
# 没有下划线）—— 这样一眼能看出这张卡是原版有 id、还是我们补的。
def make_id(faction, name):
    pre = PREFIX_OF.get(faction) or "XX"
    return pre + "_" + re.sub(r"[^A-Za-z0-9]+", "_", name).strip("_")


def load_ids():
    """读 `card_ids.json` → `{归一化卡名: [(id, 阵营), …]}`。

    同名会有多条（原版本来就跨阵营重名），**由调用方按阵营前缀挑** —— 这一层不替它挑。
    """
    if not os.path.exists(IDS_SRC):
        print(f"⚠️ 找不到 {IDS_SRC} —— 卡表将**全部自造 id**（仍唯一，但拿不到原版 id）")
        return {}
    m = json.load(open(IDS_SRC, encoding="utf-8"))["mapping"]
    by_name = {}
    for cid, nm in m.items():
        by_name.setdefault(_norm_name(nm), []).append((cid, nm))
    return by_name


def load_id_mapping():
    """读 `card_ids.json` 的 `mapping`（`{uniqueId: 卡名}`）**原样**返回。

    🔴 2026-10-18（`W25`）：直取表的**名字来源之一**（见 `load_direct_ids`）——
    这一层的值里有 54 条与 SO `cardName` 写法不同（`DA1` → `Azrael` vs `Azrael, Grand Master`），
    正好补上「SO 名和我们池里的名字不是一个写法」那批。⛔ **不要**拿它单独当身份：
    它只有名字、没有阵营，跨阵营同名（`Terminator` 那 3 组）分不开。
    """
    if not os.path.exists(IDS_SRC):
        print(f"⚠️ 找不到 {IDS_SRC} —— 直取表少一路名字来源（SO `cardName` / `m_Name` 尾巴仍然可用）")
        return {}
    return json.load(open(IDS_SRC, encoding="utf-8")).get("mapping") or {}


def pick_id(cands, faction):
    """从候选里挑**这张阵营**的那一个。挑不出（0 个或多个）返回 None —— **不猜**。"""
    pre = PREFIX_OF.get(faction)
    if not pre:
        return None
    hit = [cid for cid, _ in cands if cid.startswith(pre) and cid[len(pre):].isdigit()]
    return hit[0] if len(hit) == 1 else None


def load_truth_index():
    """读真值 id 索引（见 `TRUTH_IDX_SRC` 那段）。文件不在 ⇒ 返回 None 并**出声**（别静默退回旧行为）。"""
    if not os.path.exists(TRUTH_IDX_SRC):
        print(f"⚠️ 找不到 {TRUTH_IDX_SRC} —— 卡 id 这一层将退回「按卡名查 card_ids.json」那条老路"
              f"（自造 id 会明显变多；重出：`python -I -X utf8 d:/4/Unity/工具/gen_card_id_truth_index.py`）")
        return None
    with open(TRUTH_IDX_SRC, encoding="utf-8") as f:
        ix = json.load(f)
    ix["_base"] = set(ix.get("base") or [])
    return ix


def pick_truth_id(ix, faction, faction_id, name, ocrname):
    """按真值索引挑这张卡的 id → `(id or None, 是否只靠 by_name 软匹配)`（**不猜**，挑不出返回 `(None, False)`）。

    口径（判据 → `资料/普查产出_第三十会话/乙档六笔_判据查证.md` §`A1800`）：
      · 候选 = ①官方英文名 ②真值 `m_Name` 尾巴 ③真值 `cardName`（名字**先用 `name`、再退 `ocrname`**）
      · 过滤 = 真值 `cardArmy` == 本卡 `factionId`（**与源表 `factionId` 同口径**）；两边取不到才退回
        「前缀 + 纯数字」那条
      · 取谁 = **基准卡唯一** → 取它；否则**同阵营短 id 唯一** → 取它；否则**候选只剩一个** → 取它
      · 其余（多个候选且不唯一）⇒ **None**（例：`Normal Conditions` 那族在真值里根本没有基准卡）

    🔴 **`A1880`（2026-10-18）：第 ③ 级 `by_name` 降为【报警档】** ——
      它拿**我们卡池的卡名**去比**真值 `cardName`**，而卡名会被改名 / 是美术资产名 / 同阵营重名，
      是三级里**最弱**的一级（`by_official_en` / `by_tail` 都落空时才会用到）。
      本函数把「**只有 by_name 有候选**」这件事通过第二个返回值报出去，由调用方写进 `cascade_how`
      （跑完的 `id_cascade` 摘要里逐条可见）。⛔ 不许再把它当「和另外两级一样可信」直接用。
      ⚠️ 现读 **1127/1127 走 `uniqueId` 直取表**（`load_direct_ids`）⇒ 本函数只做交叉校验、
      `by_name` 今天一次都不会出手；这一条是**防回归**。
    """
    if not ix:
        return None, False
    keys = [k for k in (name, ocrname) if k]
    cands, seen = [], set()
    # ① + ② = 硬判据（官方英文名 / 真值 `m_Name` 尾巴）
    for tbl in ("by_official_en", "by_tail"):
        for k in keys:
            for c in ix[tbl].get(_norm_name(k), []):
                if c not in seen:
                    seen.add(c)
                    cands.append(c)
    # ③ `by_name` = 软匹配：**只在 ①② 完全落空时才用**，并要求调用方把这件事报出来。
    soft = False
    if not cands:
        for k in keys:
            for c in ix["by_name"].get(_norm_name(k), []):
                if c not in seen:
                    seen.add(c)
                    cands.append(c)
        soft = bool(cands)
    pre = PREFIX_OF.get(faction)

    def _short(cid):
        return bool(pre) and cid.startswith(pre) and cid[len(pre):].isdigit()

    army = ix.get("army") or {}
    same = [c for c in cands if faction_id is not None and army.get(c) == faction_id]
    if not same:
        same = [c for c in cands if _short(c)]
    base = [c for c in same if c in ix["_base"]]
    sh = [c for c in same if _short(c)]
    if len(base) == 1:
        return base[0], soft
    if len(sh) == 1:
        return sh[0], soft
    if len(same) == 1:
        return same[0], soft
    return None, False


# ---- 🔴 2026-10-18（`W25`）：按 `uniqueId` **直取**的 id 表 --------------------------------
# 判据 → `资料/普查产出_1018/R_引擎线_完整ID与SO后的调整评估.md` §4 第 4、5 行。
#
# **为什么改成直取**：上面那条名级联是**启发式**（官方英文名 → `m_Name` 尾巴 → `cardName`，
# 再按 `base`/短 id 挑），它能挑对，但「挑对」和「判据唯一」是两件事 —— 任何一环改了次序，
# 挑出来的 id 会**静默换一张卡**。现行口径：**身份 = 原版 `uniqueId`**，落位必须唯一。
#
# **落位表怎么建**（`(阵营码, 归一化卡名) → uniqueId`）：名字取**三处真值侧写法**——
#   ① SO `cardName`（`cards_raw.json`）
#   ② SO `m_Name` 尾段（`Z-NC BlackLegion-Warmaster` → `Warmaster`；分隔符 `-` / `_` 都用）
#   ③ `card_ids.json.mapping` 的值（54 条与 ① 写法不同，补的是「SO 名 ≠ 我们的名」那批）
# 同一键落位到多个 `uniqueId` 时，用**基准卡**消歧（`m_Name` 归一化后以 `cardName` 归一化结尾，
# 与 `工具/gen_card_id_truth_index.py` 的 `is_base` **同一条规则**）；仍不唯一 ⇒ **这个键不收**，
# 记进 `ambiguous` 由调用方报告（⛔ 不猜）。
#
# **实测（2026-10-18 现读）**：池内 1127 张里 **1125 张**按我方卡名唯一落位；
# 另外 2 张（`SAU35 Imotekh the Stormlord` / `SAU37 Orikan the Diviner`）在源表里的名字是
# 短名（`stormlord` / `Diviner`，由 `STAT_FIXES` 改名）⇒ 只有源表的 **`ocrName`** 能落位
# （那两行的 `ocrName` 正是全名）⇒ `pick_direct_id` 第二跳查 `ocrName`，合计 **1127/1127**。


def _is_base_row(r):
    """基准卡 = `m_Name` 归一化后以 `cardName` 归一化结尾（与真值索引生成器同一条规则）。
    同名组里「本体 vs 附魔/教程副本」靠它分开（例：`Uge Choppa` 本体 + `_enchantment` 副本）。"""
    cn = _norm_name(r.get("cardName"))
    return bool(cn) and _norm_name(r.get("m_Name")).endswith(cn)


def load_direct_ids(truth_rows, name_map):
    """建 `(阵营码, 归一化名) → uniqueId` 的唯一落位表 → `(direct, ambiguous)`。

    `truth_rows` = `load_truth_cards()` 的第一个返回值（`{uniqueId: SO 原始行}`）
    `name_map`   = `load_id_mapping()` 的返回值（`{uniqueId: 卡名}`）

    `ambiguous` = 落位不唯一、**没进表**的键：`[(阵营码, 归一化名, [uniqueId…]), …]`
    —— 调用方**必须**报出来（别静默丢）。
    """
    if not truth_rows:
        print("⚠️ 真值卡 SO 快照为空 ⇒ **直取表建不出来** —— 卡 id 会退回名级联（要出声的那个老路）")
        return {}, []
    cand = {}

    def _add(army, nm, uid):
        if army is None or not nm or not uid:
            return
        cand.setdefault((army, nm), set()).add(uid)

    for uid, r in truth_rows.items():
        army = r.get("cardArmy")
        _add(army, _norm_name(r.get("cardName")), uid)
        _add(army, _norm_name(re.split(r"[-_]", r.get("m_Name") or "")[-1]), uid)
        _add(army, _norm_name(name_map.get(uid)), uid)
    direct, ambiguous = {}, []
    for k, uids in cand.items():
        if len(uids) == 1:
            direct[k] = next(iter(uids))
            continue
        bs = [u for u in uids if _is_base_row(truth_rows[u])]
        if len(bs) == 1:
            direct[k] = bs[0]
        else:
            ambiguous.append((k[0], k[1], sorted(uids)))
    print(f"直取 id 表    `(阵营, 卡名) → uniqueId` 唯一落位 {len(direct)} 键"
          f"（真值 {len(truth_rows)} 行 · 落位不唯一、**不收** {len(ambiguous)} 键）")
    return direct, ambiguous


def pick_direct_id(direct, faction_id, name, ocrname):
    """按 `(阵营码, 归一化名)` 直取 `uniqueId`；落不了位返回 `None`（**不猜**）。

    两跳：①我方卡名 ②源表 `ocrName`（`SAU35`/`SAU37` 那两张督军在源表里是短名，只有 `ocrName` 能落位）。
    """
    if not direct or faction_id is None:
        return None
    for k in (name, ocrname):
        if not k:
            continue
        hit = direct.get((faction_id, _norm_name(k)))
        if hit:
            return hit
    return None


# ============================================================================
#  真值卡 SO 快照本体（`A1773` / `A1776` / `A1799` · P2 相，2026-10-15）
# ============================================================================
def load_truth_cards():
    """读 `TRUTH_CARDS_SRC` → `({uniqueId: 原始行}, {归一化名: [uniqueId, …]})`。

    取不到就返回两个空 dict 并**出声**（卡表照旧生成，只是所有真值列退化成 `-1`/空 —— 不许静默）。

    ⚠️ 两条口径（都写死在这里，消费侧别再各判一次）：
      · **键只认 `uniqueId`** —— 我们卡池里 1009/1127 张的 id 就是它（P1 相换过一次；
        其余 118 张是 2025-07-30 之后的新卡，快照里本来就没有）。这是**已裁过的 join**，
        见 `pick_truth_id` 的 docstring —— 本函数**不另开第二条 join**。
      · 那个 `AddressablesBuildNumber` 元素没有 `uniqueId`，建键时自然被丢掉（不特判）。
    """
    if not os.path.exists(TRUTH_CARDS_SRC):
        print(f"⚠️ 找不到真值卡 SO 快照 {TRUTH_CARDS_SRC} —— "
              f"`armour`/`cardTraits`/A1799 那 10 列将**全空**、督军 `cost` 保持现状（⛔ 不猜）")
        return {}, {}
    with open(TRUTH_CARDS_SRC, encoding="utf-8") as f:
        rows = json.load(f)
    # 🆕 **2026-10-18（`K06`）**：当前 build 的解析产物是 `{bundle,count,cards:[{…,full_raw}]}`，
    #   卡 SO 全字段在 `full_raw` ⇒ 统一展平（与 `gen_card_id_truth_index.py` 同口径）。
    #   🔴 **2026-10-19（`A1960`）**：`path_id` **只在元素外层**（`full_raw` 里没有）——
    #   而 `choicesDefinition.filterSpecificCards` 是 **PPtr**、按 `m_PathID` 指卡，
    #   `m_PathID ↔ 卡的 uniqueId` 的唯一桥就是这一格 ⇒ 展平时**必须**把它带进来
    #   （不带 ⇒ 178 条 filter 引用一条都解不出，而且**静默**变成空表）。
    if isinstance(rows, dict) and isinstance(rows.get("cards"), list):
        rows = [dict(c.get("full_raw") or c, path_id=c.get("path_id")) for c in rows["cards"]]
    by_id, by_name = {}, {}
    for r in rows:
        uid = r.get("uniqueId")
        if not uid:
            continue
        by_id[uid] = r
        # 名字索引**只为「把这批被丢的卡归类」用**（`classify_discarded`）—— 不参与 id 解析。
        # 收两条：`cardName` + `m_Name` 的尾段（`Z-NC BlackLegion-Warmaster` → `Warmaster`）。
        for nm in (r.get("cardName"), (r.get("m_Name") or "").split("_")[-1]):
            if nm:
                by_name.setdefault(_norm_name(nm), []).append(uid)
    print(f"真值卡快照   {len(by_id)} 行（源文件 {len(rows)} 个元素 · "
          f"名字索引 {len(by_name)} 键）")
    return by_id, by_name


# ==========================================================================================
#  🆕 2026-10-25（资源审查 F · 数据侧落地）：**异画（AlternateArt）绑定**
# ==========================================================================================
#  原版真值链（逐条核过，判据 → `资料/完全复刻/资源/R6_补核.md` §3 + `R6_AlternateArt映射.tsv`）：
#    `RawCardScript.alternateArts[]` 是 **PPtr 数组**（`{m_FileID, m_PathID}`，**不含 id 字符串**）；
#    它指向同包（allcards，CAB-`fb8f0144…`）里的 `AlternateArtCard`，
#    那个对象才有 `uniqueId = "#HB_<卡id>"` / `style` / `originalCard`（指回卡）。
#  ⇒ 要落进卡表，必须先把 `m_PathID → uniqueId` 这一步在**索引里**做掉（本脚本只读索引，不重解包）。
#  索引由 `d:/4/_tmp_view/wfres/r8_altart.py` 生成，7 条；源文件 = 关服前存档里的 allcards 包。
#  ⚠️ 原版这 7 张的 `currentAlternateArt` **全是 null**（没有「默认选哪张」的偏好）⇒ 本表**不生成**该列。
ALTART_INDEX_SRC = r"d:/4/Unity/数据/索引/altart_index.json"

_ALT_INDEX = None          # {path_id(str): uniqueId}；取不到时是 {} 并**已出声**


def load_altart_index():
    """`数据/索引/altart_index.json` → `{path_id(str): uniqueId}`。

    取不到/为空 ⇒ 返回 `{}` 并**出声**（那 7 张卡的 `alternateArts` 会落成 `[]` —— 与「本来没有异画」同形，
    所以必须出声，⛔ 不许静默；要修就跑 `r8_altart.py` 重生成索引）。
    """
    global _ALT_INDEX
    if _ALT_INDEX is not None:
        return _ALT_INDEX
    if not os.path.exists(ALTART_INDEX_SRC):
        print(f"⚠️ 找不到异画索引 {ALTART_INDEX_SRC} —— 卡表的 `alternateArts` 将**全空**"
              f"（原版有 7 张）；要修 → 跑 `d:/4/_tmp_view/wfres/r8_altart.py`")
        _ALT_INDEX = {}
        return _ALT_INDEX
    with open(ALTART_INDEX_SRC, encoding="utf-8") as f:
        raw = json.load(f)
    _ALT_INDEX = {str(k): v.get("uniqueId") or "" for k, v in (raw.get("alts") or {}).items()}
    print(f"异画索引     {len(_ALT_INDEX)} 条（源 {ALTART_INDEX_SRC}）")
    return _ALT_INDEX


def alt_ids_of(trow):
    """真值行 → 异画 id 列表（`["#HB_GOF28"]`）。缺值落 **`[]`**（契约：`null` = 整列缺席，见 `:3854`）。"""
    if trow is None:
        return []
    idx = load_altart_index()
    out = []
    for p in (trow.get("alternateArts") or []):
        uid = idx.get(str(p.get("m_PathID")))
        if uid:
            out.append(uid)
    return out


# ==========================================================================================
#  🆕 2026-10-19（`E12`）：逐候选动画时间 —— 常量与判据见 `ANIM_GUID_MAP_TSV` 上面那一段
# ==========================================================================================
def load_ability_trigger_names():
    """`AbilityTrigger.cs` 签名桩 → `{枚举数: 名字}`。读不到返回 `{}`（**不猜**）。"""
    if not os.path.isfile(ABILITY_TRIGGER_STUB):
        return {}
    body = re.search(r"\benum\s+AbilityTrigger\s*\{(.*?)\n\}",
                     open(ABILITY_TRIGGER_STUB, encoding="utf-8", errors="replace").read(), re.S)
    if not body:
        return {}
    return {int(m.group(2)): m.group(1)
            for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?",
                                 body.group(1), re.M)}


# ==========================================================================================
#  🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`UnitConditions` 枚举名
# ==========================================================================================
def load_unit_conditions_names():
    """`UnitConditions.cs` 签名桩 → `{枚举数: 名字}`。读不到返回 `{}`（**不猜**）。

    判据：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/UnitConditions.cs`
    （与 `d:/2/tools/il2cpp_out/dump.cs:20735-20819` 的同名枚举逐值一致）。
    ⚠️ **枚举名不等于语义**：`220 = dealtKillDmg` 这个名字会读成「造成过击杀伤害」，**那是错的**
    —— 真身是 `FilterMethods__MeetsConditionCriteria.c:253-257` 的
    `CardScript__EnoughPendingDamageToDie(card)`（语义 = 「这条命已经救不回来了」）。
    本脚本**只用来落 `name` 字段**（消费端按 `cond` 数值判），⛔ 别拿名字猜语义。
    """
    if not os.path.isfile(UNIT_CONDITIONS_STUB):
        return {}
    body = re.search(r"\benum\s+UnitConditions\s*\{(.*?)\n\}",
                     open(UNIT_CONDITIONS_STUB, encoding="utf-8", errors="replace").read(), re.S)
    if not body:
        return {}
    return {int(m.group(2)): m.group(1)
            for m in re.finditer(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(-?\d+)\s*,?",
                                 body.group(1), re.M)}


# ==========================================================================================
#  🆕 2026-10-21（引擎线 `T03·c` 批 B · 第 0 步 · 数据层）：`CardAbility.priority` → 逐卡 `triggerPriority`
# ==========================================================================================
# 判据：`资料/普查产出_1021/W61_引擎线_T03c批B判据.md` §2.1 / §2.2 / §2.6（第一权威 = 全量反编译
#   `d:/2/tools/decomp_full/`）：
#   · 原版 `CardAbility.priority`（`ActionPriority`）**默认 1 = Instant**
#     （`CardAbility__.ctor.c:5` 写 `+0x18 = 1`）⇒ 本列**只落 != 1 的**，缺键即 1。
#   · 分道：0→Player · 1→Instant · 2→Interrupt · 3→DelayedInstant；其他值在
#     `BattleManager__AddAutoActionToQueue` 里 **LogError 并丢弃**（⛔ 不 clamp、不默认）。
#   · 消费序 = Interrupt(2) → Instant(1) → DelayedInstant(3) → Player(0)（⛔ 不是按枚举数值排）。
#
# 🔴 **本列的键 = 引擎侧的【触发关键词】**（与 `Core/CardDef.cs` 的 `KeywordTable.*` 同一套字符串）——
#    原版 priority 是**逐 ability** 的，而我们引擎今天按**关键词**路由触发正文
#    （`CardDef.TriggerOps(keyword)` / `FireTriggerAt` / `FireTriggerAlways` / `FireTriggerOnSide` / `SpiritOps`）
#    ⇒ 只有「引擎真的按这个关键词发」的触发才表达得成一行 `{"keyword", "priority"}`。
#    收词三条（缺一条都不收）：
#      ① `AbilityTrigger` 枚举里真有这个值（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` 现读）；
#      ② `CardDef.RoutableTriggers` 里有这个关键词（引擎收得到正文）；
#      ③ 引擎**有**按它发触发的调用点（`RuleCore` / `EffectResolver` 现核，逐行写在值后面）。
#    ⛔ **没收的那些是【结构性表达不出】，不是漏做**：0 `ThisCardPlayed`（单位 Rally 正文 / 战术卡正文，
#       走 `ResolveDeploy` / `Desc` 那条路，**没有关键词**）· 30/31 `TurnStart*` 与 40/41 `TurnEnd`
#       （走 `ResolveAtTurn(ctx, "turn_start"/"turn_end")`，**没有关键词**）· 190/195 `*UnitSummoned`
#       （`WhenEvent.Deploy`）· 80/90 `*UnitDeath`（`WhenEvent.Die`）· 510/515/520 标记族
#       （`WhenEvent.Gets*`）· 410 / 500 / 605 / 610 / 650 / 695 …（见 `资料/卡牌制作_三张表.md` 表 A
#       的「我们引擎对应事件」那一列）。**这些原版有 priority 的照样在跑完读数里报出来**
#       （`report["unmapped"]`）—— 等队列接进 `WhenEvent` 那一层再落，⛔ 不在这里编造键名。
ABILITY_TRIGGER_KEYWORD = {
    120: "slay",           # Slay           → KeywordTable.Slay（`FireTriggerAlways(Slay)`）
    145: "codex",          # Codex          → KeywordTable.Codex
    220: "sacrifice",      # Sacrifice      → KeywordTable.Sacrifice
    280: "abouttoattack",  # AboutToAttack  → KeywordTable.AboutToAttack
    300: "mob",            # Mob            → KeywordTable.Mob
    301: "regiment",       # Regiment       → KeywordTable.Regiment
    420: "survivor",       # Survivor       → KeywordTable.Survivor
    440: "landing",        # Landing        → KeywordTable.Landing
    560: "artifice",       # Artifice       → KeywordTable.Artifice（`FireTriggerOnSide(Artifice)`）
    580: "strike",         # Strike         → KeywordTable.Strike
    600: "spiritstone",    # UseSpiritStone → KeywordTable.SpiritStone（正文走 `SpiritOps`）
    655: "pray",           # Pray           → KeywordTable.Pray（`UseAlternative`）
    662: "duty",           # Duty           → KeywordTable.Duty（`UseAlternative`）
    670: "ambush",         # Ambush         → KeywordTable.Ambush
    680: "uprising",       # Uprising       → KeywordTable.Uprising（`FireUprising`）
    690: "teleport",       # Teleport       → KeywordTable.Teleport
    705: "agenda",         # Agenda         → KeywordTable.Agenda（`UseAlternative`）
    710: "stimulation",    # Stimulation    → KeywordTable.Stimulation
    715: "ecstasy",        # Ecstasy        → KeywordTable.Ecstasy（`FireTriggerOnBoard`）
    720: "cruelty",        # Cruelty        → KeywordTable.Cruelty
    725: "ferocity",       # Ferocity       → KeywordTable.Ferocity（`UseAlternative`）
}

# 原版默认值 = Instant(1)。**只落 != 它的**（判据见上一段）。
TRIGGER_PRIORITY_DEFAULT = 1
# 原版 `ActionPriority` 的合法值；别的值原版 LogError 丢弃 ⇒ 本生成器照样**出声**、不落（`report["bad"]`）。
TRIGGER_PRIORITY_VALID = (0, 1, 2, 3)


def trigger_priorities_of(cid, name, trow, trig_names, report):
    """一张卡 → `[{"keyword": …, "priority": …}, …]`（**只收 != 1 的**，按 keyword 排序）。

    🔴 **GOF97 陷阱**（判据报告 §4.A 第 0 步）：有的卡在**同一个 trigger** 下有两条 ability ——
       一条 `priority=0` 但 `abilityLogic` **为空**（占位/模板），另一条 `priority=1` 才是真效果。
       实测（2026-10-21 现读）：`Gargantuan Squiggoth`（`GOF97`）第 0 条
       `trigger=0 / priority=0 / logic=0`、第 1 条 `trigger=0 / priority=1 / logic=2`。
       ⇒ **只看「该 trigger 下有 logic 的那条」**：空 logic 的 ability **没有动作可排队**，
       它的 priority **不落**（否则 GOF97 会被记成 `priority=0`、凭空多一条 Player 道）。

    ⚠️ **同一张卡、同一个关键词出现两个不同 priority** ⇒ 本列一格只能一个值、表达不出 ⇒
       **整条丢掉并出声**（`report["conflict"]`），⛔ 不挑一个、不取 max（那就是静默编口径）。
       （2026-10-21 现读：0 条。）
    """
    if trow is None:
        report["no_truth"] += 1
        return []
    by_kw = {}
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        pr = ab.get("priority", TRIGGER_PRIORITY_DEFAULT)
        if isinstance(pr, bool) or not isinstance(pr, int):
            report["bad"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        if pr == TRIGGER_PRIORITY_DEFAULT:
            continue                    # 默认值不落（本列的约定：缺键 = 1）
        if pr not in TRIGGER_PRIORITY_VALID:
            # 原版 `AddAutoActionToQueue` 对非法值 LogError 并**丢弃**（判据报告 §2.2）⇒ 不出声就是静默。
            report["bad"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        if not (ab.get("abilityLogic") or []):
            report["empty_logic"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        tg = ab.get("trigger")
        kw = ABILITY_TRIGGER_KEYWORD.get(tg)
        if kw is None:
            report["unmapped"].append((cid, name, trig_names.get(tg, ""), tg, pr))
            continue
        by_kw.setdefault(kw, set()).add(pr)
    out = []
    for kw in sorted(by_kw):
        prs = by_kw[kw]
        if len(prs) > 1:
            report["conflict"].append((cid, name, kw, sorted(prs)))
            continue
        out.append({"keyword": kw, "priority": next(iter(prs))})
    if out:
        report["cards"] += 1
        report["rows"] += len(out)
    return out


# ==========================================================================================
#  🔴🆕 2026-10-23（引擎线 P1·①）：`CardAbility.priority` → 逐卡 `abilityTriggerPriority`
#    （**按原版 `AbilityTrigger` 枚举名**落，供 `WhenEvent` / 回合 / 打出那一族取道）
# ==========================================================================================
# 为什么另开一列（而不是把 `triggerPriority` 那张表补几个键）：
#   · `triggerPriority`（v13）的键 = **引擎侧触发关键词**（`slay` / `artifice` / `spiritstone` …），
#     只覆盖「引擎按关键词路由正文」的那 21 个触发；
#   · `WhenEvent` / `ResolveAtTurn` / 打出族**没有关键词**（正文走 `Desc` / `ResolveAtTurn` 那几条路），
#     它们手上唯一的名字就是**原版 `AbilityTrigger` 枚举名**（`OtherUnitDeath` / `TurnStartPlayer` /
#     `FollowUpResolution` / `OnCollectQuestPoints` …）—— 与 `abilityContext`（v14）用的是同一套主键。
#   ⇒ 两种主键**不是一套词**，混进同一列会让 `CardDef.TriggerPriority(keyword)` 的语义分叉
#     （「两处写同一条规则 = 迟早不一致」）。本列与 `abilityContext` 同形：每行自带
#     `trigger`（数值）+ `name`（枚举名），消费端按 `name` 查。
#
# 判据（与 `trigger_priorities_of` **共用**，⛔ 不重写一份）：
#   · 默认值 1 = Instant（`CardAbility__.ctor.c:5`）⇒ **只落 != 1 的**；
#   · 0→Player · 1→Instant · 2→Interrupt · 3→DelayedInstant；别的值原版 `AddAutoActionToQueue`
#     LogError 丢弃 ⇒ 本脚本出声（`report["bad"]`）、不落；
#   · **`abilityLogic` 为空的那条不落**（`GOF97` 陷阱：`priority=0` 但 `logic=0` 是占位/模板，
#     没有动作可排队；照 `trigger_priorities_of` 的同一条口径）；
#   · `name` 查不出来（枚举桩缺失）⇒ **不落并出声**（`name` 是消费端主键，⛔ 不猜）。
# 现读（2026-10-23 · 池内 1,127 张）：非默认 **7 条** —— 其中 4 条是关键词那列已有的
#   （`SAU13`/`SAU70` artifice=3 · `UM37` codex=3 · `ASH53` spiritstone=3），
#   **3 条是本列新增**（`DA12` OnCollectQuestPoints=2 · `BL72` UnitReceivesMarkOfChaos=2 ·
#   `DA14` FollowUpResolution=0）。判据 → `资料/普查产出_1023c/引擎深审A_规则语义.md` P1·1。
ABILITY_TRIGGER_PRIORITY_REPORT = {"cards": 0, "rows": 0, "no_truth": 0,
                                   "bad": [], "empty_logic": [], "no_name": [], "conflict": []}


def ability_trigger_priorities_of(cid, name, trow, trig_names, report):
    """一张卡 → `[{"trigger": …, "name": <AbilityTrigger 枚举名>, "priority": …}, …]`。

    **只落 `priority != 1` 的行**（缺键 = 原版默认 1 = Instant）—— 与 `triggerPriority` 同一条省列口径。
    同 `trigger` 出现两个不同 priority ⇒ 本列一格一个值、表达不出 ⇒ **整条丢掉并出声**
    （`report["conflict"]`，⛔ 不挑一个、不取 max）。现读 0 条。
    """
    if trow is None:
        report["no_truth"] += 1
        return []
    by_tg = {}
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        pr = ab.get("priority", TRIGGER_PRIORITY_DEFAULT)
        if isinstance(pr, bool) or not isinstance(pr, int):
            report["bad"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        if pr == TRIGGER_PRIORITY_DEFAULT:
            continue                    # 默认值不落（本列的约定：缺键 = 1）
        if pr not in TRIGGER_PRIORITY_VALID:
            report["bad"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        if not (ab.get("abilityLogic") or []):
            report["empty_logic"].append((cid, name, i, ab.get("trigger"), pr))
            continue
        tg = ab.get("trigger")
        tn = trig_names.get(tg, "")
        if not tn:
            report["no_name"].append((cid, name, i, tg, pr))
            continue
        by_tg.setdefault(tg, {"name": tn, "prs": set()})["prs"].add(pr)
    rows = []
    for tg in sorted(by_tg, key=lambda x: (x is None, x)):
        ent = by_tg[tg]
        if len(ent["prs"]) > 1:
            report["conflict"].append((cid, name, tg, ent["name"], sorted(ent["prs"])))
            continue
        rows.append({"trigger": tg, "name": ent["name"], "priority": next(iter(ent["prs"]))})
    if rows:
        report["cards"] += 1
        report["rows"] += len(rows)
    return rows


# ==========================================================================================
#  🆕 2026-10-22（引擎线 `W63` · `T03·c/d` 数据层）：`CardAbility.abilityContext` → 逐卡 `abilityContext`
# ==========================================================================================
# 判据：`资料/普查产出_1022/W63_abilityContext_施工简报.md` §1/§2/§3
#   （第一权威 = 全量反编译 `d:/2/tools/decomp_full/CardAbility__IsRightContext.c`）：
#   · 原版 `CardAbility.abilityContext`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs:10`，
#     字段偏移 **+0x14**）= **「这条 ability 的载体（thisCard）此刻必须待在哪个区域」**；
#   · 取值只有这六个：0 Play · 10 Hand · 20 Deck · 30 Cemetery ·
#     40 InPlayOrDying（`IsInPlayOrDying`，**含状态 5 待死**）·
#     50 InPlayOrCemetery（`IsInCemetery || IsInPlay`，**不含待死**）
#     —— 🔴 40 与 50 **不等价**（差的就是「待死」那一档），⛔ 别互相顶替；
#   · 读点全库只有一处：`CardAbility.CanTriggerAbility` → `IsRightContext`
#     （**入队前**判 —— `RawCardScript__TriggerAbilities.c:108,207` 在广播那一刻同步调）。
#   ⚠️ **不是所有触发都读它**：`CanTriggerAbility.c:69-95` 有 skip 表（trigger ∈
#     {0 ThisCardPlayed · 80 ThisUnitDeath · 145 Codex · 220 Sacrifice · 500 TriggerEnchantment}
#     整段跳过；600/605 在余额判定前显式检查，`:85-124`）—— **数据全量落、skip 表由消费端照抄**。
#     ⛔ 别在本脚本里替消费端丢数据：那等于用生成器实现一条引擎判据（两边迟早不一致）。
#
# 🔴 **只落 context != 0 的行**（0 = Play = 本列的默认值，省略）—— 与 `triggerPriority`
#    （缺键 = 原版默认 1）同一条口径。消费端契约（那半边不在本文件，⛔ 别在这里写第二份）：
#      **缺键 / 卡上没有这一列 ⇒ `-1` 未知**（`CardDef.AbilityContextOfTrigger`），
#      ⛔ **不许读成 Play(0)** —— 那会把死亡族(40) / 墓地族(50) 整片掐死（简报 §6·2）。
#    代价（**已知、写在这里免得下一个人当成 bug**）：context 全 0 的卡在 JSON 里**没有这一列**，
#    消费端看到的是「未知」而不是「Play」⇒ 它必须按 `-1` 回退旧表。真值快照里**没有这张卡**
#    与「有卡但全 0」在本脚本里**分开记账**（`no_truth` vs 不落列），⛔ 别让读数把两件事混成一件。
# 🔴 **`name` 必须落**（`AbilityTrigger` 枚举名，由 `load_ability_trigger_names()` 反查）：
#    只按 `keyword` 落会**漏掉 trigger 50 `UnitAttack`**（它不在 `ABILITY_TRIGGER_KEYWORD` 里）
#    ⇒ 恰恰修不了简报点名的那 5 张卡（关键词路只覆盖全部非 0 行里的一小半）。
#    `keyword` **只在映射表里有**时才出现 —— 复用同一张 `ABILITY_TRIGGER_KEYWORD`，⛔ 别另立一张。
# 🔴 **同 trigger 同 context 的重复行【合并】**（原版一张卡在同一 trigger 下可以有多条 ability：
#    现读 3 张，`605` / `0` 各有一对同值行）：它对消费端是**纯噪声** ——
#    `CardDatabase.MapAbilityContext` 把「同 key 重复」当冲突出声后**整组丢弃**，
#    不合并会让那 3 张卡**凭白丢掉整列**。合并条数照实打在跑完读数里（`report["dup_same"]`）。
#    同 trigger **不同** context（现读 0 条）⇒ **两行都落**（数据是真的）+ `report["conflict"]` 出声，
#    由消费端按它自己的规则裁（⛔ 生成器不替它挑一个、也不整组丢 —— 那都是静默编口径）。
ABILITY_CONTEXT_VALID = (0, 10, 20, 30, 40, 50)
ABILITY_CONTEXT_DEFAULT = 0        # Play —— 本列省略这一档（缺键 = 未知，见上面那段）


def ability_contexts_of(cid, name, trow, trig_names, report):
    """一张卡 → `[{"trigger":…, "name":…, ["keyword":…,] "context":…}, …]`（按 trigger 排序）。

    `JsonUtility` 不支持字典（与 `talentNameIds` / `triggerPriority` 同一条口径）⇒ 落数组，
    每行自带 `trigger`（`AbilityTrigger` 数值）＋ `name`（枚举名）＋ 可选 `keyword`。

    落值口径（判据与代价 → 上面那段长注释）：**只落 context != 0 的**（0 = Play = 默认）；
    同 `(trigger, context)` 的重复行合并；同 `trigger` 不同 `context` 的两行都落并出声；
    `AbilityTrigger` 名查不出来（枚举桩缺失）⇒ **不落并出声**（`name` 是消费端主键，
    ⛔ 不猜名字）；真值快照里没有这张卡 ⇒ `report["no_truth"]`（≠「全 0」，见上）。
    """
    if trow is None:
        report["no_truth"] += 1
        return []
    rows, seen = [], set()
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        ctx = ab.get("abilityContext")
        if isinstance(ctx, bool) or not isinstance(ctx, int):
            # 缺键 / 非整数：⛔ 不默认成 0（那等于替原版编一个 Play 口径 ⇒ 静默错一批卡）。
            report["bad"].append((cid, name, i, tg, ctx))
            continue
        if ctx == ABILITY_CONTEXT_DEFAULT:
            continue                    # 默认值不落（本列的约定：缺键 = 未知，0 由消费端的默认/skip 表兜）
        if ctx not in ABILITY_CONTEXT_VALID:
            report["bad"].append((cid, name, i, tg, ctx))
            continue
        tn = trig_names.get(tg, "")
        if not tn:
            # 枚举桩读不到（`load_ability_trigger_names()` 返回 `{}`）或出现枚举里没有的数值。
            # ⚠️ 不落：`name` 是消费端的主键，落一个空主键等于给它一批「用不了的行」。
            report["no_name"].append((cid, name, i, tg, ctx))
            continue
        key = (tg, ctx)
        if key in seen:
            report["dup_same"].append((cid, name, tg, ctx))
            continue
        seen.add(key)
        row = {"trigger": tg, "name": tn}
        kw = ABILITY_TRIGGER_KEYWORD.get(tg)
        if kw is not None:
            row["keyword"] = kw
        row["context"] = ctx
        rows.append(row)
    # 排序 = `(trigger, context)`：**字节稳定**靠它（幂等：两次 `--write` 必须逐字节相同）。
    rows.sort(key=lambda r: (r["trigger"], r["context"]))
    per_tg = {}
    for r in rows:
        per_tg.setdefault(r["trigger"], []).append(r["context"])
    for tg in sorted(per_tg):
        if len(per_tg[tg]) > 1:
            report["conflict"].append((cid, name, tg, per_tg[tg]))
    for r in rows:
        report["by_trigger"][r["trigger"]] = report["by_trigger"].get(r["trigger"], 0) + 1
        report["by_context"][r["context"]] = report["by_context"].get(r["context"], 0) + 1
    if rows:
        report["cards"] += 1
        report["rows"] += len(rows)
    return rows


# ==========================================================================================
#  🆕 2026-10-23（引擎线 · `B-16` 残余）：`cardAbilities[].trigger` 的**全量集合**
#    → 逐卡 `abilityTriggers` 列（解 `UnitConditions.hasUseSpiritStone(241)` /
#      `hasUseFaith(242)` 那一层）
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`）：
#   · `FilterMethods__MeetsConditionCriteria.c:286-289`（241）→ `RawCardScript__HasAbilityType(raw, 600)`
#     / `:290-293`（242）→ 同法 `605`；
#   · `RawCardScript__HasAbilityType.c:16-38` = **遍历 `rawCardData.cardAbilities`（`+0x290`）**，
#     看 `CardAbility.trigger(+0x10)` 是否等于参数 —— **只看 trigger，不看 abilityLogic 空不空**
#     （所以这里**不**照 `trigger_priorities_of` 那样丢掉「logic 为空」的占位 ability：
#       那个过滤是给 priority 用的，`HasAbilityType` 没有这道闸）。
#
# 形状：`[int, …]` = `sorted(set(cardAbilities[].trigger))`。
# 🔴 **每张卡都落这一列**（没有 ability 的卡落 `[]`）—— 「缺列」与「空数组」必须分得开：
#    前者 = 真值快照里没有这张卡（未知），后者 = **明确没有 ability**。
#    `HasAbilityType` 对空表返回假，对「未知」我们**不许**返回假（⛔ 那会把缺口静默读成「没有」）。
# ⚠️ 本列**只落数据**：消费端 = `CardDef.AbilityTriggerPresence` → `ConditionEval` 的 241/242。
ABILITY_TRIGGER_REPORT = {"cards": 0, "triggers": 0, "no_truth": 0, "bad": []}


def ability_triggers_of(cid, name, trow, report):
    """一张卡 → `sorted(set(cardAbilities[].trigger))`（**没 ability 就是 `[]`**）。

    真值快照里**没有这张卡** ⇒ 返回 `None`（调用方**不落列** = 未知，⛔ 不是空表）。
    `trigger` 不是整数（坏数据）⇒ 跳过该条并出声（`report["bad"]`）——
    ⛔ 不猜成 0（`0` = `ThisCardPlayed` 是一个**具体**的触发）。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    seen = set()
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            report["bad"].append((cid, name, i, tg))
            continue
        seen.add(tg)
    out = sorted(seen)
    report["cards"] += 1
    report["triggers"] += len(out)
    return out


# ==========================================================================================
#  🆕 2026-10-23（引擎线 · 战斗审计 `B-17`）：逐 ability / 逐 logic 的 `targetsAffected`
#    → 逐卡 `abilityTargets` 列（解「无主语正文 ⇒ 落到己方全体」那条近似）
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c`）：
#   · `iVar6 = *(int *)(criteria + 0x10)` = `TargetCriteria.targetsAffected`，整条函数就是按它分发：
#     `:184-191` `self(20)` → 加 **thisCard**（`param_3`，= 写着这条 ability 的那张卡）；
#     `:761-769` `actingCard(80)` / `attacker(40)` → 加 **actingCard**（`param_4`）；
#     `:721-737` `friendlyHero(120)` → `BattleManager.GetHero(actingCard.isPlayer)` = **施放方督军**；
#     `:738-746` `enemyHero(130)` → 同上取反；
#     `:901-908` `any(0)` → `GetUnitsInPlayList(...)` = **场上全部单位**（两侧，再按 criteria 过滤）；
#     `allFiltered(10)` → 走 criteria 的 friendlyUnits / enemyUnits / includeHero / includeMinions 筛。
#   · 枚举 `TargetsAffected`：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs`（38 个成员）。
#
# 形状：`[{"trigger": int, "ta": [int per abilityLogic]}]`，**按 `cardAbilities` 顺序**。
#   `ta[j]` 就是 `cardAbilities[i].abilityLogic[j].targetCriteria.targetsAffected`；
#   非整数（缺键/坏值）落 **`-1`**（⛔ 不是 0 —— `0` 是合法的 `any`）。
# 🔴 **每张卡都落这一列**（没有 ability 的卡落 `[]`）—— 「缺列」与「空数组」必须分得开（同 `abilityTriggers`）。
# ⚠️ **本列只落数据**：消费端 `EffectText.AssignAbilityTargets` 只在「**整卡能力块数 == ability 数**
#   ∧ **块里 op 数 == 该 ability 的 logic 数**」两道闸都过时才把值挂到 op 上
#   （`EffectOp.SubjectTargetsAffected`）；闸门不过 = **读不到**，结算层照旧走「己方全体」并出声。
ABILITY_TARGETS_REPORT = {"cards": 0, "abilities": 0, "logics": 0, "no_truth": 0, "bad": []}


def ability_targets_of(cid, name, trow, report):
    """一张卡 → `[{"trigger":…, "ta":[…]}]`（**按 `cardAbilities` 顺序**；没 ability = `[]`）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）。
    `targetsAffected` 非整数 ⇒ 该格落 `-1` 并出声（⛔ 不猜成 `0` = `any`）。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            # trigger 只作可读性（消费端按**下标**对齐）⇒ 坏值就落 -1，⛔ 不因此丢掉整条 ability。
            report["bad"].append((cid, name, i, "trigger", tg))
            tg = -1
        ta = []
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            v = (lg.get("targetCriteria") or {}).get("targetsAffected")
            if isinstance(v, bool) or not isinstance(v, int):
                report["bad"].append((cid, name, i, f"lg{j}.targetsAffected", v))
                v = -1
            ta.append(v)
        out.append({"trigger": tg, "ta": ta})
        report["abilities"] += 1
        report["logics"] += len(ta)
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 2026-10-24（引擎线 `A12` · 第 3 件）：`AbilityLogic.furtherMultiplier(+0x38)`
#    → 逐卡 `abilityMultipliers` 列
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `AbilityLogic__PlayAbility.c:181-236`，逐行读过）：
#   · `lVar13 = AbilityLogic.GetTargets(bm, multiplier /*+0x30*/, card, 1, 0)`；随后
#       `multiplier.targetsAffected == 0 (any)`  ⇒ `local_178 = 1`
#       `… ∈ {210, 220}`                        ⇒ 逐个目标累加 trait 值
#       `其余`                                   ⇒ `local_178 = 目标个数`
#   · `:235-236`  `if (0 < *(int *)(param_1 + 0x38)) local_178 = local_178 * *(int *)(param_1 + 0x38);`
#     ⇒ **`furtherMultiplier > 0` 才乘**；`0` = 原版**明确不乘**（`0` 是合法取值，⛔ 不是「缺」）。
#   · 之后 `local_178` 就是这条 logic 的**次数 / 倍数**：
#       召唤族 `do { … } while (iVar8 < local_178)`（`:729` 起 · `0x46/0x47/0x48/0x4b/0x4d`）·
#       伤害族 `*(int*)(lVar13 + 0x10) = local_178 * damageCriteria.damage`（`:708`）·
#       `AbilityLogic.AdjustCardEffect(…, local_178, …)` 那一族（`:839` / `:918` / `:1188` …）。
#
# 形状：`[{"trigger": int, "fm": [int per abilityLogic]}]`，**按 `cardAbilities` 顺序**
#   （与 `abilityTargets` 同形、同一套下标语义）。
#   `fm[j]` = `cardAbilities[i].abilityLogic[j].furtherMultiplier`；
#   **缺键/非整数/负数落 `-1`**（⛔ 不是 0 —— `0` 是「原版明确不乘」）。
# 🔴 **每张卡都落**（没有 ability 的卡落 `[]`）——「缺列」与「空数组」必须分得开。
# ⚠️ **本列只落数据**：消费端 `EffectText.AssignAbilityTargets` 用**同一道两道闸门**
#   把它挂到 op 上（`EffectOp.AbilityFurtherMultiplier`）；挂不上 = 未知 ⇒ 结算层不乘。
ABILITY_MULTIPLIERS_REPORT = {"cards": 0, "abilities": 0, "logics": 0, "no_truth": 0, "bad": []}


def ability_multipliers_of(cid, name, trow, report):
    """一张卡 → `[{"trigger":…, "fm":[…]}]`（**按 `cardAbilities` 顺序**；没 ability = `[]`）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）。
    `furtherMultiplier` 缺键/非整数/负数 ⇒ 该格落 `-1` 并出声（⛔ 不猜成 `0` = 「不乘」）。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            # trigger 只作可读性（消费端按**下标**对齐）⇒ 坏值落 -1，⛔ 不因此丢掉整条 ability。
            report["bad"].append((cid, name, i, "trigger", tg))
            tg = -1
        fm = []
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            v = lg.get("furtherMultiplier")
            if isinstance(v, bool) or not isinstance(v, int) or v < 0:
                report["bad"].append((cid, name, i, f"lg{j}.furtherMultiplier", v))
                v = -1
            fm.append(v)
        out.append({"trigger": tg, "fm": fm})
        report["abilities"] += 1
        report["logics"] += len(fm)
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 2026-10-24（卡表数据 `D11` · 战斗审计 `D10` §6·1）：主动能力**整条链**
#    → 逐卡 `activeAbilityLogic` 列
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`，逐行读过；审计 → `资料/普查产出_1024/D10_abilityTargets管线.md`）：
#   · `RawCardScript__ResolveActiveAbility.c`（全文 71 行）：
#       `:18` `if (hasAlternativeAbility(+0x2AA) == 0)` ⇒ `:21` 取 `activeAbility(+0x260)`；
#       `:26-31` 否则先跑 `BoardAnalysis.MeetsCriteria(+0x2B0 …)` —— **真**走 activeAbility、
#               **假**走 `alternativeAbility(+0x2B8)`（🔴 方向别搞反，见 `RawCardScript__GetActiveAbility.c`）。
#       `:50` `lVar6 = *(lVar6 + 0x48)` = 那个 Ability 对象里的 `activeAbilityLogic`（List）；
#       `:58-67` 逐行 `AbilityLogic__PlayAbility(lVar4, bm, uVar1, 400, 0)`
#               —— **trigger 400 = `ThisUnitAbilityUsed`**（原版给的**虚拟时机**，行数据里没有这个值）。
#       ⚠️ **`alternativeAbility` 里的列表键名也叫 `activeAbilityLogic`**（不是 `alternativeAbilityLogic`，
#          全库 `grep -o '"alternativeAbilityLogic"' = 0`）—— 按后者取会**静默得 0 行**（D10 踩过）。
#   · `AbilityLogic__PlayAbility.c:262` `AbilityLogic__GetTargets(bm, *(param_1 + 0x28) /* targetCriteria */, …)`
#       ⇒ `ta` 取的是**行上的** `activeAbilityLogic[j].targetCriteria.targetsAffected`；
#   · `:235-236` `if (0 < *(int *)(param_1 + 0x38)) local_178 = local_178 * furtherMultiplier`
#       ⇒ `fm` = 行上的 `furtherMultiplier`（`0` = 明确不乘）。
#   · 闸（**决定这些行在本 build 是不是死数据**）：
#       `CardScript__ResolveActiveAbilityPlayed.c:22` `hasActiveAbility(+0x220) == 0` ⇒ **LogWarning + return**
#       ⇒ 有 active/alt 行但 `hasActiveAbility == 0` 的卡，本 build **一条都不消费**（D10 §2·4：池内 45 张 / 52 行）；
#       `alternativeAbility` 那支另外还要求 `hasAlternativeAbility != 0`（`:18`）。
#   · `loadingAbilityLogic` **不在此列**：1,351 张 SO 里 `m_PathID != 0` 的 **0 张**（D10 §1·2）。
#
# 形状（`JsonUtility` 不支持字典，与 `abilityTargets` 同一口径）：
#   `[{"ab": -1|-2, "gate": 1|0|-1, "trigger": 400, "needsLoading": int, "ta": [int…], "fm": [int…]}]`
#     · `ab` = `-1` = `activeAbility` / `-2` = `alternativeAbility`（**与 `unitConditions` 的 `ab` 同词**）。
#     · `gate` = 该分支**静态**可达：active → `hasActiveAbility`；
#                alternative → `hasActiveAbility ∧ hasAlternativeAbility`。
#                `0` = 本 build **死数据**；`-1` = 闸本身未知（⛔ 不拿 0 顶「已知不消费」）。
#                ⚠️ alternative 还有一条**运行期**闸（`MeetsCriteria == false`）静态判不了 ⇒ 这里**不含**它。
#     · `trigger` = **400**（`ThisUnitAbilityUsed`）—— 原版逐行传的虚拟时机；⛔ 别去行数据里找。
#     · `needsLoading` = 该 Ability 对象的 `needsLoading(+0x22)`；缺键/坏值 `-1`（未知）。
#     · `ta[j]` = `activeAbilityLogic[j].targetCriteria.targetsAffected`，坏值 `-1`（⛔ 不猜 0 = `any`）；
#       `fm[j]` = `activeAbilityLogic[j].furtherMultiplier`，坏值/负数 `-1`（⛔ 不猜 0 = 不乘）。
#       （与 `abilityTargets` / `abilityMultipliers` 的 `ta` / `fm` **逐格同契约**。）
#     · 某能力对象**一行都没有 ⇒ 不落那一项**（`[]` 里不出现空对象）。
#     🆕 v24（2026-10-24 · 执行代理 `E1`）**每行再加三个真值键**（⛔ 不是另开一列）：
#       · `ca[j]` = `activeAbilityLogic[j].chosenAbility`（原版 `AbilityEffect` 枚举 —— `10 doDamage` /
#                   `20 doHeal` / `30 buffOthers` / `40 buffSelf` / `60 buffMana` / `70 summonUnit` /
#                   `462 triggerPray` … 全表 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`）。
#                   **这一行「做什么」的唯一真值**；缺键/坏值 `-1`。
#                   🔴 有了它才可能按**能力种类**把行配到 op 上（v23 只能配「1 对象/1 行/1 op」那 38 张，
#                   理由 → `资料/普查产出_1024/G12_activeAbility消费.md` §三·4）。
#       · `pa[j]` = `activeAbilityLogic[j].playersAffected`（`0` = 目标是被作用到的牌/单位；
#                   `10` = 玩家自己 —— 现池 `10` 只出现在 `drawCard`/`buffMana`/`buffHand` 这类行上）。
#                   缺键/坏值 `-1`。用来区分「作用对象是牌」还是「是玩家」。
#       · `dmg[j]` = `{"min":…, "max":…, "opt":…, "type":…}` = `damageCriteria` 原值
#                   （`minDamage` / `maxDamage` / `valueOptions` / `damageType`；缺对象/坏值逐格 `-1`）。
#                   ⚠️ **只有 damage 族（`ca` ∈ `10/12/13/15`）才读**：非伤害行里那对 `(1,1,0,0)` 是
#                   SO 的**序列化默认值**（现池 `ca=10` 的 70 行里 21 行是 `(1,1)`，与「1 点伤害」区分不开）
#                   ⇒ 只能当**旁证/对账**，⛔ 别拿它反推非伤害行的数值。
#     · 形状 = `[{"ab":…,"gate":…,"trigger":400,"needsLoading":…,"ta":[…],"fm":[…],"ca":[…],"pa":[…],"dmg":[{…}]}]`
#   🔴 **死数据照落、带 `gate: 0`**（理由 → `资料/普查产出_1024/D11_activeAbility列.md` §2）：
#     本仓的「缺席 = 未知」（`-1` 契约）是硬规矩 ⇒ 丢掉那 45 张就等于把「已知不消费」读成「没收到」，
#     那正是最贵的那类静默错；并且 `hasActiveAbility` 本身就是**数据驱动的字段**（`+0x2A9` 灌进 `+0x220`），
#     换个 build 翻上去时行数据还得在。
#   🔴 **每张卡都落**（没有 active/alt 行的卡落 `[]`）——「缺列」与「空数组」必须分得开（同 `abilityTargets`）。
#   🔴 **本列只落数据、零行为变化**：消费端（`CardDef` / `EffectText` / `EffectResolver`）**另批做**
#     （当前快照里这些行 0 个运行期消费者，见 D10 §3·1）。
ACTIVE_ABILITY_LOGIC_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "objs": 0, "rows": 0,
    "dead_cards": 0, "dead_rows": 0, "fm_nonzero_rows": 0, "alt_cards": 0, "bad": [],
    "ca_missing": 0, "pa_nonzero": 0, "dmg_read": 0,
}


def active_ability_logic_of(cid, name, trow, report):
    """一张卡 → `[{"ab":…, "gate":…, "trigger":400, "needsLoading":…, "ta":[…], "fm":[…], "ca":[…], "pa":[…], "dmg":[…]}]

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）。
    `hasActiveAbility` / `hasAlternativeAbility` / `needsLoading` / `ta` / `fm` / `ca` / `pa` / `dmg` 的坏值口径见上面那段。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    ha = _gate_int(trow, "hasActiveAbility")
    hb = _gate_int(trow, "hasAlternativeAbility")
    if ha is None:
        report["bad"].append((cid, name, "hasActiveAbility", trow.get("hasActiveAbility")))
        ha = -1
    if hb is None:
        report["bad"].append((cid, name, "hasAlternativeAbility", trow.get("hasAlternativeAbility")))
        hb = -1

    def branch_gate(code):
        """`-1` active / `-2` alternative ⇒ `1` 静态可达 · `0` 死数据 · `-1` 未知。"""
        if ha < 0 or (code == -2 and hb < 0):
            return -1
        if code == -1:
            return 1 if ha == 1 else 0
        return 1 if (ha == 1 and hb == 1) else 0

    out = []
    for code, key in ((-1, "activeAbility"), (-2, "alternativeAbility")):
        ab = trow.get(key)
        if not isinstance(ab, dict):
            # 现核 1,351 张全是 dict；坏值出声（⛔ 不静默当「没有能力」）
            report["bad"].append((cid, name, key, ab))
            continue
        defs = ab.get("activeAbilityLogic")
        if defs is None:
            defs = []
        if not isinstance(defs, list):
            report["bad"].append((cid, name, f"{key}.activeAbilityLogic", defs))
            continue
        if not defs:
            continue                      # 这个能力对象没有行 ⇒ 不落那一项
        nl = _gate_int(ab, "needsLoading")
        if nl is None:
            report["bad"].append((cid, name, f"{key}.needsLoading", ab.get("needsLoading")))
            nl = -1
        ta, fm, ca, pa, dmg = [], [], [], [], []
        for j, lg in enumerate(defs):
            if not isinstance(lg, dict):
                report["bad"].append((cid, name, f"{key}[{j}]", lg))
                ta.append(-1)
                fm.append(-1)
                ca.append(-1)
                pa.append(-1)
                dmg.append({"min": -1, "max": -1, "opt": -1, "type": -1})
                continue
            tc = lg.get("targetCriteria")
            v = tc.get("targetsAffected") if isinstance(tc, dict) else None
            if isinstance(v, bool) or not isinstance(v, int):
                report["bad"].append((cid, name, f"{key}[{j}].targetsAffected", v))
                v = -1
            ta.append(v)
            w = lg.get("furtherMultiplier")
            if isinstance(w, bool) or not isinstance(w, int) or w < 0:
                report["bad"].append((cid, name, f"{key}[{j}].furtherMultiplier", w))
                w = -1
            fm.append(w)
            # ---- v24: chosenAbility（这一行「做什么」）----
            cv = lg.get("chosenAbility")
            if isinstance(cv, bool) or not isinstance(cv, int):
                report["bad"].append((cid, name, f"{key}[{j}].chosenAbility", cv))
                cv = -1
                report["ca_missing"] += 1
            ca.append(cv)
            # ---- v24: playersAffected（0 = 作用到牌/单位 / 10 = 玩家自己）----
            pv = lg.get("playersAffected")
            if isinstance(pv, bool) or not isinstance(pv, int):
                report["bad"].append((cid, name, f"{key}[{j}].playersAffected", pv))
                pv = -1
            elif pv != 0:
                report["pa_nonzero"] += 1
            pa.append(pv)
            # ---- v24: damageCriteria（只有 damage 族才读，见上面那段）----
            dc = lg.get("damageCriteria")
            _d = {"min": -1, "max": -1, "opt": -1, "type": -1}
            if isinstance(dc, dict):
                for _k, _src in (("min", "minDamage"), ("max", "maxDamage"),
                                 ("opt", "valueOptions"), ("type", "damageType")):
                    _dv = dc.get(_src)
                    if isinstance(_dv, bool) or not isinstance(_dv, int):
                        report["bad"].append((cid, name, f"{key}[{j}].damageCriteria.{_src}", _dv))
                    else:
                        _d[_k] = _dv
                report["dmg_read"] += 1
            else:
                report["bad"].append((cid, name, f"{key}[{j}].damageCriteria", dc))
            dmg.append(_d)
        g = branch_gate(code)
        out.append({"ab": code, "gate": g, "trigger": 400,
                    "needsLoading": nl, "ta": ta, "fm": fm,
                    "ca": ca, "pa": pa, "dmg": dmg})
        report["objs"] += 1
        report["rows"] += len(ta)
        report["fm_nonzero_rows"] += sum(1 for x in fm if x > 0)
        if code == -2:
            report["alt_cards"] += 1
        if g == 0:
            report["dead_rows"] += len(ta)
            report["dead_cards"] += 1
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 v25（2026-10-24 · 执行代理 `E9` · `资料/普查产出_1024c/A2_引擎战斗判据总表.md` 的 Q7）：
#    logic / op 级 `targetCriteria`（**其余 36 格**）→ 逐卡
#      `activeAbilityTargets`（主动能力两条支）与 `abilityTargetCriteria`（普通 ability）
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`）：
#   · 原版 `AbilityLogic.targetCriteria` 是**每条 logic 一份完整的 `TargetCriteria`**
#     （`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityLogic.cs:16-17`；
#      字段表 `TargetCriteria.cs` = 38 个字段，其中 `conditionsFilter` / `excludeCondition`
#      已经由 `unitConditions` 落成那一行的 `cond` / `ex` ⇒ **本列落其余 36 格**）。
#   · 消费点 = `AbilityLogic__PlayAbility.c:262`：
#     `GetTargets(param_2, *(param_1 + 0x28) /* = 这一行的 targetCriteria */, param_3, 1, 0)`
#     → `AbilityLogic__GetTargets.c`（`targetsAffected`(+0x10) 先选**候选池**）
#     → `FilterMethods__CheckIfMeetsCriteria.c`（其余 35 格是**过滤**）。
#   · 我们原来只落了**一格**（`targetsAffected` → `abilityTargets` / `activeAbilityLogic[].ta`）
#     ⇒ 「无点目标 ⇒ 落到己方全体」那条近似，缺的正是这 35 格。
#
# 形状（`JsonUtility` 不支持字典 ⇒ 数组；一行的 36 格装进**子对象** `criteria`）：
#   `activeAbilityTargets: [{"ab": -1|-2, "lg": j, "criteria": {36 格}}]`
#   `abilityTargetCriteria: [{"ab": i, "trigger": tg, "lg": j, "criteria": {36 格}}]`
#   · 36 格 = **直接复用 `criteria_set_of()`**（⛔ 别另写一份字段表）：键名 = 原版字段名，
#     缺省/坏值 `-1`（两个引用列表 `null`），布尔落 0/1 整数；
#   · `playerTurn` 在 `AbilityLogic.targetCriteria` 上**结构上没有**（那一格属于 `BoardCriteria`）
#     ⇒ `has_player_turn=False` ⇒ 落 `-1`、且**不进 `set_bad`**（同 v22 那四格的口径）；
#   · 🔴 **为什么 36 格装子对象、不平铺在行上**：`CardDatabase` 那边只有**一份** 36 格 DTO 字段表
#     （`CountSetDto`，与 `unitConditions[].countSet` 共用）。平铺就必须在 C# 里再抄一份 36 格
#     ⇒ 「两处写同一条规则 = 迟早不一致」。⚠️ 与 `A2` Q7 的**建议**形状（平铺）不同，理由如上。
#   · 🔴 **`abilityTargetCriteria` 比 `A2` Q7 的建议多一个 `ab`（`cardAbilities` 下标）**：
#     实测 **89 张卡**有两条 ability **共用同一个 `trigger`** ⇒ `(trigger, lg)` **不唯一**，
#     消费端按它对齐会串行。`ab` / `lg` 的语义与 `unitConditions` 逐字同词
#     （主动能力那一列 `ab = -1` active / `-2` alternative，与 `activeAbilityLogic` / `unitConditions` 同词）。
#   · 🔴 **每张卡都落**（真值里有这张卡但没有对应行 ⇒ `[]`）—— 「缺列」与「空数组」必须分得开
#     （同 `abilityTargets` / `activeAbilityLogic`）；只有「真值快照里没有这张卡」才**不落列**。
#   · `lg` 不是整数（坏行）⇒ 该行照落、`criteria` 全 `-1` 并出声（同 `activeAbilityLogic` 的坏格口径：
#     丢行 = 这一格变成「没有过滤」= 静默放行）。
#
# ⚠️ **本批只落数据、零行为变化**：消费端（`CardDef` / `EffectText` / `EffectResolver`）**另批做**
#   —— `RuleEngine/Core/**` 本轮由别的代理持有。下一步落点写在
#   `资料/普查产出_1024c/E9_卡表v25_logic级判据列.md` 的「消费端下一步」。
ACTIVE_ABILITY_TARGETS_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "rows": 0, "active_rows": 0, "alt_rows": 0,
    "bad": [], "set_bad": [], "set_unresolved": [],
}
ABILITY_TARGET_CRITERIA_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "rows": 0, "abilities": 0,
    "bad": [], "set_bad": [], "set_unresolved": [],
}


def active_ability_targets_of(cid, name, trow, truth_by_path, report):
    """一张卡 → `[{"ab":…, "lg":…, "criteria": {36 格}}]`（**按 active → alternative、再按行序**）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）。
    形状 / 判据 / 为什么带 `ab`、为什么 36 格装子对象 → 上面那一段长注释。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for code, key in ((-1, "activeAbility"), (-2, "alternativeAbility")):
        ab = trow.get(key)
        if not isinstance(ab, dict):
            # 现核 1,351 张都是 dict；坏值出声（⛔ 不静默当「没有这条支」）。
            report["bad"].append((cid, name, key, ab))
            continue
        defs = ab.get("activeAbilityLogic")
        if defs is None:
            defs = []
        if not isinstance(defs, list):
            report["bad"].append((cid, name, f"{key}.activeAbilityLogic", defs))
            continue
        for j, lg in enumerate(defs):
            tc = lg.get("targetCriteria") if isinstance(lg, dict) else None
            if not isinstance(lg, dict):
                # 坏行也照落（丢行 = 消费端读成「没有过滤」）⇒ `criteria` 走全 -1 那一支并出声。
                report["bad"].append((cid, name, f"{key}[{j}]", lg))
            out.append({
                "ab": code,
                "lg": j,
                "criteria": criteria_set_of(cid, name, tc, None, truth_by_path, report,
                                            label=f"{key}[{j}].targetCriteria.",
                                            has_player_turn=False),
            })
            report["rows"] += 1
            if code == -1:
                report["active_rows"] += 1
            else:
                report["alt_rows"] += 1
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


def ability_target_criteria_of(cid, name, trow, truth_by_path, report):
    """一张卡 → `[{"ab":…, "trigger":…, "lg":…, "criteria": {36 格}}]`（**按 `cardAbilities` / 行序**）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）。
    形状 / 判据 / 为什么带 `ab`（`(trigger, lg)` 实测不唯一）→ 上面那一段长注释。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        if not isinstance(ab, dict):
            report["bad"].append((cid, name, f"cardAbilities[{i}]", ab))
            continue
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            # trigger 只作可读性（消费端按 `ab`/`lg` 对齐）⇒ 坏值落 -1，⛔ 不因此丢掉整条 ability。
            report["bad"].append((cid, name, i, "trigger", tg))
            tg = -1
        report["abilities"] += 1
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            tc = lg.get("targetCriteria") if isinstance(lg, dict) else None
            if not isinstance(lg, dict):
                report["bad"].append((cid, name, f"cardAbilities[{i}].abilityLogic[{j}]", lg))
            out.append({
                "ab": i,
                "trigger": tg,
                "lg": j,
                "criteria": criteria_set_of(cid, name, tc, None, truth_by_path, report,
                                            label=f"cardAbilities[{i}].abilityLogic[{j}].targetCriteria.",
                                            has_player_turn=False),
            })
            report["rows"] += 1
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 v26（2026-10-25 · 执行代理 `E2` · `资料/普查产出_1025/A3_引擎_at-turn与criteria管线.md` §A3-2）：
#    **ability 级 `targetCriteria`（其余 36 格）** → 逐卡 `abilityLevelCriteria` 列
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`，逐句读过）：
#   · 原版两个**同名**对象，别混：
#       `AbilityLogic.targetCriteria`   —— logic 级（v25 的 `abilityTargetCriteria` 收的就是它）；
#       **`CardAbility.targetCriteria`** —— **ability 级（`+0x30`）**，本列收它。
#     字段表 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardAbility.cs:16`（`+0x30`）。
#   · 消费点 = `CardAbility__CanTriggerAbility.c:131-135`（触发前的第二道闸）：
#       `FilterMethods__CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`
#     ⚠️ 三个实参的位置读自 `CardAbility__CanTriggerAbility` 的**形参表**
#       （`d:/2/tools/il2cpp_out/dump.cs:20360`：`(trigger, manager, thisCard, actingCard, targetCard, …)`）⇒
#       `param_4 = thisCard` · `param_5 = actingCard` · `param_6 = targetCard`。
#       **第一道闸（`+0x28 actingCardCriteria`）的被筛对象是 `actingCard`**，
#       **第二道闸（`+0x30 targetCriteria`，本列）的被筛对象是 `targetCard`** —— 两闸的候选**不同**。
#     （`FilterMethods__CheckIfMeetsCriteria` 的 `param_2` 就是「被筛的那个」：
#       `lVar9+0x10 = param_2`，随后 `:216-263` 拿它和 `param_1`（thisCard）比阵营/身份。）
#   · `targetsAffected == 0 (any)` ⇒ `CheckIfMeetsCriteria.c:56-59` **直接 `return 1`**
#     ⇒ 其余 35 格全部失效 ⇒ 这一行**零信息**，**不落**（口径 = `criteria_carries_targets`）。
#   · `conditionsFilter` / `excludeCondition` 已由 `unitConditions`（`role="target"` + `lg=-1`）落成
#     那一行的 `cond`/`ex` ⇒ **本列只落其余 36 格**（与 v25 同一分工）。
#
# 形状（`JsonUtility` 不支持字典 ⇒ 数组；36 格装进子对象 `criteria`）：
#   `abilityLevelCriteria: [{"ab": i, "trigger": 50, "criteria": {36 格}}]`
#   · `ab` = `cardAbilities` 下标（**唯一**对齐键；`trigger` 只作可读性 ——
#     实测同卡两条 ability 共用同一个 `trigger` 的情形存在，⛔ 别只按 trigger 对齐）；
#   · 36 格 = **直接复用 `criteria_set_of()`**（⛔ 别另写一份字段表）；
#   · `playerTurn` 在 `CardAbility.targetCriteria` 上**结构上没有**（那格属于 `BoardCriteria`）
#     ⇒ `has_player_turn=False` ⇒ 落 `-1`、且**不进 `set_bad`**；
#   · `lg` **不落**（本列的行恒为 ability 级 = `lg = -1`；DTO 的字段初值就是 `-1`）。
#   · 🔴 **每张卡都落**（没有行 ⇒ `[]`）—— 「缺列」与「空数组」必须分得开（同 v25 两列）。
#
# ⚠️ **本列只落数据**：消费端 = `CardDef.AbilityLevelCriteriaForTrigger`（**唯一入口**），
#   接线点 = `ActionPump.EnqueueAbility` 的触发前闸；定位不唯一 ⇒ 出声并跳过（⛔ 不许猜第几条）。
# ⚠️ **不在本列**：`activeAbility.targetCriteria`（**另一个类**、另一个偏移 ——
#   `RawCardScript__IsValidActiveAbilityTarget.c:103-107` 用的是 `activeAbility(+0x260)` 内部 `+0x38`）
#   ⇒ 它是**主动能力的目标池闸**，不是 `CanTriggerAbility` 那道闸。另账。
# ------------------------------------------------------------------------------------------
#  🆕 v29（2026-10-25 · 执行代理 `X6` · 口径乙 · `资料/待办判据_引擎_1025b.md` §一）：
#    `abilityLevelCriteria` 每行加 **`ca`（能力种类族名）**
# ------------------------------------------------------------------------------------------
#  为什么：这张表里有 **23 张卡**的 `trigger 0` ability **定位不唯一**（同一 trigger 两张
#    ability）⇒ 消费端 `UniqueAbilityIndexForTrigger` 只能回「不唯一」⇒ 那 32 行 criteria
#    一格都没被消费。主对话裁定走**口径乙**：按 `chosenAbility` 的**家族**把行认回来。
#
#  `ca` = 该 ability（`cardAbilities[ab]`）里 `abilityLogic[*].chosenAbility` 的**家族名**，
#    家族表 = `CardDef.ActiveAbilityKindFamily`（C# 侧唯一权威，`CardDef.cs` 的
#    `ActiveAbilityKindFamilyNames` 是允许取值的白名单 —— 自检里有一条**跨语言对账**
#    断言：本文件落出去的 `ca` 必须全部在那张白名单里）。
#
#  三档取值（⛔ 别把「判不了」写成空串以外的花样，C# 侧按同一套判）：
#    · `"damage"` 等族名 —— 该 ability 的 logic 行**家族唯一** ⇒ 可判；
#    · `""`         —— **判不了**：家族不唯一（同一 ability 里混了两种家族，实测 3 行）
#                      或一条 `abilityLogic` 都没有（实测 2 行）；
#    · 键缺失       —— 不给（DTO 侧 `null` 哨兵 ⇒ 出声）。生成器本轮**每行都写**。
#
#  🔴 为什么按 **ability** 而不是按行：这一列的行是 **ability 级**（`lg = -1`，
#    `CardAbility.targetCriteria` `+0x30`），它管的是「这条 ability 触不触发」——
#    而「这条 ability 是什么种类」由它自己的 `abilityLogic[*].chosenAbility` 决定。
#     实测：`ASH30` 两条 ability 都是 damage、`DA33 ab1` 混 damage+resource
#     ⇒ 这 4 张（`ASH30`/`DA33`/`GSC46`/`SOR31`）认不出来，落 `""`（出声 + 放行）。
#
#  ⚠️ 本列**只落数据**；消费端 = `RuleCore.AbilityFamilyTargetAllows`（唯一实现）→
#    `EffectResolver` 出牌正文的逐 op 闸。
ABILITY_KIND_FAMILY = {
    0: "draw", 10: "damage", 12: "damage", 13: "damage", 15: "damage",
    20: "heal",
    30: "buff", 31: "buff", 40: "buff", 50: "buff",
    60: "resource",
    70: "summon", 71: "summon", 72: "summon", 75: "summon", 77: "summon",
    100: "create", 101: "create",
    110: "destroy", 120: "jam", 130: "stun", 135: "stun",
    140: "handmove", 150: "handmove", 160: "handmove", 162: "handmove",
    165: "handmove", 170: "handmove", 175: "handmove", 180: "handmove",
    185: "handmove", 190: "handmove", 200: "handmove", 210: "handmove",
    220: "handmove", 240: "handmove", 250: "handmove", 270: "handmove",
    290: "handmove", 300: "handmove", 301: "handmove", 310: "handmove",
    260: "trigger", 370: "trigger", 375: "trigger", 380: "trigger",
    390: "trigger", 400: "trigger", 410: "trigger", 411: "trigger",
    420: "trigger", 430: "trigger", 440: "trigger", 450: "trigger",
    460: "trigger", 461: "trigger", 462: "trigger", 465: "trigger",
    466: "trigger", 467: "trigger", 468: "trigger",
    # 320 followAfterResolution / 325 followResolutionIfPreviousMeetsCriteria：
    # 语义是「跟着上一条结算」（不是一种效果）⇒ **故意不映射**（与 C# 侧逐条一致）。
}


def ability_kind_family_of(trow, ab_index):
    """`cardAbilities[ab_index]` → (`ca`, 家族集合, 坏值列表)。

    `ca` = 家族唯一时那个族名；否则 `""`（判不了，⛔ 不猜第一行）。
    """
    bad = []
    fams = []
    ab = None
    abs_ = trow.get("cardAbilities") or []
    if 0 <= ab_index < len(abs_):
        ab = abs_[ab_index]
    if not isinstance(ab, dict):
        return "", [], [(ab_index, "cardAbilities")]
    for j, lg in enumerate(ab.get("abilityLogic") or []):
        if not isinstance(lg, dict):
            return "", [], [(ab_index, f"abilityLogic[{j}]")]
        cv = lg.get("chosenAbility")
        if isinstance(cv, bool) or not isinstance(cv, int):
            bad.append((ab_index, f"abilityLogic[{j}].chosenAbility", cv))
            continue
        fam = ABILITY_KIND_FAMILY.get(cv)
        if fam is None:
            # 320/325 与未列值：与 C# 的 `default: return null` 逐条一致 ⇒ 不算坏值。
            continue
        if fam not in fams:
            fams.append(fam)
    return (fams[0] if len(fams) == 1 else ""), fams, bad


ABILITY_LEVEL_CRITERIA_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "rows": 0, "skipped_noinfo": 0,
    "bad": [], "set_bad": [], "set_unresolved": [],
    "ca_single": 0, "ca_mixed": 0, "ca_none": 0, "ca_missing": 0,
    "ca_family_counts": {}, "ca_ambiguous_cards": [],
}


def ability_level_criteria_of(cid, name, trow, truth_by_path, report):
    """一张卡 → `[{"ab":…, "trigger":…, "criteria": {36 格}}]`（按 `cardAbilities` 顺序）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）；否则**照落**（没有行 = `[]`）。
    `targetsAffected == 0`（= `any`，原版直接 `return 1`）的行**不落**（零信息，口径 →
    `criteria_carries_targets`），计进 `skipped_noinfo`。
    形状 / 判据 / 为什么带 `ab`、为什么不落 `lg` → 上面那一段长注释。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        if not isinstance(ab, dict):
            report["bad"].append((cid, name, f"cardAbilities[{i}]", ab))
            continue
        cr = ab.get("targetCriteria")
        if not isinstance(cr, dict):
            report["bad"].append((cid, name, f"cardAbilities[{i}].targetCriteria", cr))
            continue
        if not criteria_carries_targets(cr):
            report["skipped_noinfo"] += 1
            continue
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            # `trigger` 只作可读性（消费端按 `ab` 对齐）⇒ 坏值落 -1，⛔ 不因此丢掉整行。
            report["bad"].append((cid, name, i, "trigger", tg))
            tg = -1
        # ---- v29：`ca`（能力种类族名；判不了 = 空串，判据 → `ABILITY_KIND_FAMILY` 上面那段）----
        ca, fams, fam_bad = ability_kind_family_of(trow, i)
        for _b in fam_bad:
            report["bad"].append((cid, name, f"cardAbilities[{i}].{_b[1]}", _b[2]))
            report["ca_missing"] += 1
        if ca:
            report["ca_single"] += 1
            report["ca_family_counts"][ca] = report["ca_family_counts"].get(ca, 0) + 1
        elif len(fams) >= 2:
            report["ca_mixed"] += 1
        else:
            report["ca_none"] += 1
        out.append({
            "ab": i,
            "trigger": tg,
            "ca": ca,
            "criteria": criteria_set_of(cid, name, cr, None, truth_by_path, report,
                                        label=f"cardAbilities[{i}].targetCriteria.",
                                        has_player_turn=False),
        })
        report["rows"] += 1
    # 🔴 v29：**这张卡按家族可判吗** —— 同一族出现 ≥2 行，或出现过空串（`ca` 判不了），
    #   消费端起闸时都只能「判不了 ⇒ 出声 + 放行」。实测 = 4 张
    #   （`ASH30`/`DA33`/`GSC46`/`SOR31`，与 `待办判据_引擎_1025b.md` §一 的 19/4 一致）。
    _ca = [r["ca"] for r in out]
    if _ca and (any(x == "" for x in _ca) or len(set(_ca)) != len(_ca)):
        report["ca_ambiguous_cards"].append(cid)
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 v27（2026-10-25 · 执行代理 `E3` · `资料/普查产出_1025/E2_ability_criteria管线.md` §六）：
#    **ability 级 `actingCardCriteria`（`+0x28`，其余 36 格）** → 逐卡 `abilityActingCriteria` 列
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`，逐句读过）：
#   · `CardAbility__CanTriggerAbility.c:127-130`（**第一道闸**，本列收它）：
#       `CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`
#     —— 被筛对象 = **`actingCard`**（不是 `targetCard`；那是第二道闸 `+0x30`，
#     见 `ability_level_criteria_of` 上面那段）。
#   · `CardAbility__CanTriggerAbility.c:131-135`（第二道闸）：同一函数里紧跟其后，
#     ⇒ **短路次序 = acting(0x28) → target(0x30) → board(0x38)**（消费端照这个次序挂）。
#   · 形参表 `d:/2/tools/il2cpp_out/dump.cs:20360`：
#       `CanTriggerAbility(trigger, manager, thisCard, actingCard, targetCard, ignoreRandomTrigger)`
#     ⇒ `param_4 = thisCard` · `param_5 = actingCard` · `param_6 = targetCard`。
#   · `targetsAffected == 0 (any)` ⇒ `FilterMethods__CheckIfMeetsCriteria.c:56-59` **直接 `return 1`**
#     ⇒ 其余 35 格全部失效 ⇒ 这一行**零信息**，**不落**（口径 = `criteria_carries_targets`）。
#     ⚠️ **这与「36 格非平凡」不是一回事**：现读池内 93 行「36 格非平凡」里有 **10 行**
#     `ta == 0`（另外 35 格有值）—— 原版仍 `return 1` ⇒ 按本口径**不落** ⇒ 落 **83 行 / 80 张**。
#   · `conditionsFilter` / `excludeCondition` 已由 `unitConditions`（`role="acting"`）落成那一行的
#     `cond`/`ex` ⇒ **本列只落其余 36 格**（与 v25/v26 同一分工）。
#
# 形状（`JsonUtility` 不支持字典 ⇒ 数组；36 格装进子对象 `criteria`）：
#   `abilityActingCriteria: [{"ab": i, "trigger": 50, "criteria": {36 格}}]`
#   · `ab` = `cardAbilities` 下标（**唯一**对齐键；`trigger` 只作可读性）；
#   · 36 格 = **直接复用 `criteria_set_of()`**（⛔ 别另写一份字段表）；
#   · `playerTurn` 在 `CardAbility.actingCardCriteria` 上结构上没有 ⇒ `-1`、**不进 `set_bad`**；
#   · `lg` **不落**（本列的行恒为 ability 级 = `lg = -1`）；
#   · 🔴 **每张卡都落**（没有行 ⇒ `[]`）—— 「缺列」与「空数组」必须分得开。
#
# 🔴 **列名为什么是 `abilityActingCriteria` 而不是 `actingCardCriteria`**：
#   与 v26 的 `abilityLevelCriteria`（同一层 = **`CardAbility` 这个对象**）成套 ——
#   前缀 `ability` 表示「ability 级」，尾巴点出**被筛的候选**（`Acting` = `actingCard`）。
#   另一个候选名 `abilityTargetCriteria` **已被 v25 的 logic 级列占用** ⇒ 不能再拿它当
#   「ability 级 + targetCard」的名字（会撞概念）。原版字段名 `actingCardCriteria` 逐字写在
#   本条注释与 `CardDatabase.CardDto` 的 doc 里（可追溯），JSON 键用上名。
#   ⛔ **不与 `abilityLevelCriteria` 共用一个字典**：两列是**两道闸、两个候选**（键虽同为 `ab`）。
#
# ⚠️ **本列只落数据**：消费端 = `CardDef.ActingLevelCriteriaForTrigger`（**唯一入口**），
#   接线点 = `ActionPump.EnqueueAbility` 的第一道闸（`Attack` / `Die` 两族，见报告 §三）。
ABILITY_ACTING_CRITERIA_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "rows": 0, "skipped_noinfo": 0,
    "bad": [], "set_bad": [], "set_unresolved": [],
}


def ability_acting_criteria_of(cid, name, trow, truth_by_path, report):
    """一张卡 → `[{"ab":…, "trigger":…, "criteria": {36 格}}]`（按 `cardAbilities` 顺序）。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）；否则**照落**（没有行 = `[]`）。
    `targetsAffected == 0`（= `any`，原版直接 `return 1`）的行**不落**（零信息，口径 →
    `criteria_carries_targets`），计进 `skipped_noinfo`。
    形状 / 判据 / 为什么带 `ab`、为什么不落 `lg` → 上面那一段长注释。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        if not isinstance(ab, dict):
            report["bad"].append((cid, name, f"cardAbilities[{i}]", ab))
            continue
        cr = ab.get("actingCardCriteria")
        if not isinstance(cr, dict):
            report["bad"].append((cid, name, f"cardAbilities[{i}].actingCardCriteria", cr))
            continue
        if not criteria_carries_targets(cr):
            report["skipped_noinfo"] += 1
            continue
        tg = ab.get("trigger")
        if isinstance(tg, bool) or not isinstance(tg, int):
            # `trigger` 只作可读性（消费端按 `ab` 对齐）⇒ 坏值落 -1，⛔ 不因此丢掉整行。
            report["bad"].append((cid, name, i, "trigger", tg))
            tg = -1
        out.append({
            "ab": i,
            "trigger": tg,
            "criteria": criteria_set_of(cid, name, cr, None, truth_by_path, report,
                                        label=f"cardAbilities[{i}].actingCardCriteria.",
                                        has_player_turn=False),
        })
        report["rows"] += 1
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：
#    **`ActiveAbility.targetCriteria`（`+0x38`，对象级目标闸）** → 逐卡 `activeAbilityPickCriteria` 列
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`，逐句读过）：
#   · **消费点 = `RawCardScript__IsValidActiveAbilityTarget.c`**（「这个格位能不能当主动能力的目标」）：
#       `:103-107`  `FilterMethods__CheckIfMeetsCriteria(thisCard, 候选, 候选,
#                    *(activeAbility(+0x260) + 0x38), 0)`
#     ⇒ 被筛对象 = **候选目标**；criteria = **`ActiveAbility` 对象自己的 `targetCriteria`**。
#     调用者 = `BattleManager__IsValidActiveAbilityTarget` → `GetAvailableActions` /
#     `ShowPotentialTargets`（高亮）/ `StartChoosingAbilityTargetByClick`（点选）/
#     `IsThereValidAbilityTarget` / `_ResolvePlayActiveAbility`（最后一刻复核）/ `Update`。
#   · 进入 criteria 之前还有几道前置闸（同两个方法体，逐句）：
#       `BattleManager__IsValidActiveAbilityTarget.c:29-52` —— 候选 `IsInPlay` · 教程许可 ·
#       **`thisCard.isPlayer == 当前回合方`** · 异侧且候选带 `stealth(0x46)` ⇒ 假 ·
#       异侧且候选 `isCurrentlyWarded` ⇒ 假 · 候选带 `summonRift(0x2da=730)` ⇒ 假；
#       `RawCardScript__IsValidActiveAbilityTarget.c:76-105` —— 异侧且候选带 `tainted(0xa0=160)`
#       且 `HasVanguardTrait` 不成立且 thisCard 是英雄 ⇒ 假。
#   · `targetsAffected == 0 (any)` ⇒ `CheckIfMeetsCriteria` 直接 `return 1` ⇒ 零信息 ⇒ **不落**
#     （口径 = `criteria_carries_targets`，与 v25/v26/v27 三列一致）。
#
# 🔴 **本列与 `activeAbilityTargets`（v25）不是同一格**：
#   | 列 | 原版字段 | 层 | 键 |
#   | `activeAbilityTargets` | `activeAbilityLogic[j].targetCriteria`（`AbilityLogic +0x28`） | logic 级 | `ab`, `lg` |
#   | **`activeAbilityPickCriteria`（本列）** | **`ActiveAbility.targetCriteria`（`+0x38`）** | **对象级** | `ab`（**没有 `lg`**） |
#   ⛔ 别合并（一个是「这条 logic 从哪儿取目标」，一个是「这个格位能不能被点」）。
#
# 形状：`activeAbilityPickCriteria: [{"ab": -1|-2, "criteria": {36 格}}]`
#   · `ab` = `-1` = `activeAbility` / `-2` = `alternativeAbility`（与 `activeAbilityLogic` /
#     `unitConditions` 的 `ab` **同词**）；
#   · 36 格 = **直接复用 `criteria_set_of()`**；`playerTurn` 这一格在 `ActiveAbility.targetCriteria`
#     上**结构上不存在**（现读真值 1351 张的键表里没有它）⇒ `-1`、**不进 `set_bad`**；
#   · **每张卡都落**（没有行 ⇒ `[]`）—— 「缺列」与「空数组」必须分得开；真值快照里没有这张卡 ⇒ **不落列**。
#   · ⚠️ `conditionsFilter` / `excludeCondition` 两格**不在本列**：现读非零的只有 **1 行**（`SW9`），
#     而 `unitConditions` 里 `ab=-1/-2` 的那些行是 **logic 级**（带 `lg`）的 —— 不是同一格
#     ⇒ 本列**不拼**那两格，消费端按「没有结构化条件」处理（放宽一格，**如实记**在报告里）。
#
# 消费端 = `CardDef.ActiveAbilityPickCriteriaFor(ab)`（唯一入口）→ `RuleCore.CanUseAbility` 的目标闸。
ACTIVE_ABILITY_PICK_CRITERIA_REPORT = {
    "cards": 0, "no_truth": 0, "with_rows": 0, "rows": 0, "skipped_noinfo": 0,
    "bad": [], "set_bad": [], "set_unresolved": [],
}


def active_ability_pick_criteria_of(cid, name, trow, truth_by_path, report):
    """一张卡 → `[{"ab": -1|-2, "criteria": {36 格}}]`。

    真值快照里没有这张卡 ⇒ `None`（调用方**不落列** = 未知）；否则**照落**（没有行 = `[]`）。
    `targetsAffected == 0`（= `any`，原版直接 `return 1`）⇒ 零信息、**不落**（计进 `skipped_noinfo`）。
    形状 / 判据 / 与 `activeAbilityTargets` 的区别 → 上面那一段长注释。
    """
    if trow is None:
        report["no_truth"] += 1
        return None
    out = []
    for key, code in (("activeAbility", -1), ("alternativeAbility", -2)):
        obj = trow.get(key)
        if obj is None:
            continue
        if not isinstance(obj, dict):
            report["bad"].append((cid, name, key, obj))
            continue
        cr = obj.get("targetCriteria")
        if not isinstance(cr, dict):
            report["bad"].append((cid, name, key + ".targetCriteria", cr))
            continue
        if not criteria_carries_targets(cr):
            report["skipped_noinfo"] += 1
            continue
        out.append({
            "ab": code,
            "criteria": criteria_set_of(cid, name, cr, None, truth_by_path, report,
                                        label=key + ".targetCriteria.",
                                        has_player_turn=False),
        })
        report["rows"] += 1
    if out:
        report["with_rows"] += 1
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 2026-10-24（引擎线 `A20` · 第三批 ①/②）：主动能力的**分支闸 / 装填位 / 任务位**
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`，逐句读过；来源 `资料/普查产出_1023e/`）：
#   · **280 `siege`** ← `CardScript__DoesAbilityRequireLoading.c`：
#       `:13` `hasActiveAbility(+0x220)` 为假 ⇒ **假**；
#       `:35` 遍历 `activeEffects(+0x108)`，任一 `HasRemovedTrait(0x1ea = 490 reload)` ⇒ **假**；
#       `:24-31` `GetActiveAbilityCard → RawCardScript.GetActiveAbility` → 取那个 `ActiveAbility`
#       对象内部的 **`+0x22 needsLoading`**（`dump.cs:23758`）。⚠️ 那个 `+0x22` **不是** `CardScript`
#       的偏移（`CardScript` 的 `+0x22` 落在 0x220 那批 `k__BackingField` 里，见 `dump.cs:35444-35449`）。
#   · **320 `hasQuestCompleted`** ← `FilterMethods__MeetsConditionCriteria.c:227-248`：
#       `!HasCurrentTrait(130 jam)` ∧ `rawData._quest(+0x240)` ∧ `HasQuests(entity) <= 0`。
#       另一条同源链（不在这条条件里）：`EntityScript__ReachedQuestCounter.c:5-14` 对
#       `cardType == 40 ∧ spellType == 120 ∧ questTimes > 1` 的卡累计 `questCounter(+0x270)`。
#   · **分支闸** ← `RawCardScript__GetActiveAbility.c`（全文 17 行）：
#       `hasAlternativeAbility(+0x2AA)` 为真 **且** `BoardAnalysis.MeetsCriteria(+0x2B0 …)` 为**假**
#       ⇒ 返回 `alternativeAbility(+0x2B8)`；否则返回 `activeAbility(+0x260)`。
#       🔴 **方向别搞反**：满足 criteria 走【普通】支，不满足才走【替代】。
#       `MeetsCriteria` 逐句 → `BoardAnalysis__MeetsCriteria.c`（criteria/countCriteria 为空 ⇒ 真；
#       `playerTurn` 10 Self / 20 Enemy 是**整条闸**；`triggerIf == 8 RandomChance` 走随机；
#       `countCriteria.targetsAffected == 0(any)` ⇒ 真；否则数个数再按 `triggerIf` 比）。
#   · 数据侧（`资料/普查产出_1018/allcards_1350/cards_raw.json` 的 `cards[*].full_raw`，1,351 张：
#       `hasActiveAbility` 88 张 = 1 · `hasAlternativeAbility` **1 张**（`SW21`）·
#       `activeAbility.needsLoading` / `alternativeAbility.needsLoading` / `_quest` / `questTimes`
#       **全是 0** ⇒ 今天这两条条件都恒假（**数据事实，不是「判不了」**）；
#       `normalAbilityBoardCriteria` 只有 **2 张**不是默认值（`SW9` / `SW21`）。
#
# 🔴 **落值口径**：只落**非默认**的键（`0` 是合法取值「假/不加」）——
#    · `hasActiveAbility` / `hasAlternativeAbility`：`1` 才落；`-1`（真值里没这张卡）也落。
#    · `activeNeedsLoading` / `alternativeNeedsLoading` / `quest` / `questTimes`：非 0 才落（`-1` 也落）。
#    · `normalAbilityBoardCriteria`：只有**不是默认**时才落。默认 = `playerTurn / countOptions /
#      referenceValue` 全 0 **且** `countCriteria` 每个字段都是 0 —— 那一档 `MeetsCriteria` **恒真**
#      （`targetsAffected = any`），落不落行为一样。
#      ⛔ **缺席 ≠ 判不了**：缺席 = 原版给了恒真的默认 criteria；判不了只可能出现在坏值上（见 `bad`）。
#    ⇒ 今天真正落地的只有 88 张 `hasActiveAbility: 1` ＋ `SW21` 的三个键 ＋ `SW9` 的一条 criteria。
ABILITY_GATE_REPORT = {
    "cards": 0, "no_truth": 0, "has_active": 0, "alt": 0, "needs_loading": 0,
    "quest": 0, "criteria": 0, "bad": [],
    # `count_set_of` 要的三个键（与 `unit_conditions_report` 同形，⛔ 别改名）
    "set_bad": [], "set_unresolved": [], "set_rows": 0,
}

# 「默认 criteria」只看这三格（`triggerIf` 的默认是 `1 EqualTo`，全库恒等，⛔ 别把它算进「非默认」）。
_GATE_TOP_FIELDS = ("playerTurn", "countOptions", "referenceValue")


def _gate_int(row, key):
    """真值行的整数列 → `int`；`bool` 折成 0/1；**缺键/非整数 → `None`**（⛔ 不拿 0 顶）。"""
    v = (row or {}).get(key)
    if isinstance(v, bool):
        return 1 if v else 0
    if isinstance(v, int):
        return v
    return None


def _gate_nonzero(v):
    """`MeetsCriteria` 眼里的「这一格不是默认值」。`bool` 先折成 0/1。"""
    if isinstance(v, bool):
        return v
    if isinstance(v, list):
        return len(v) > 0
    return v not in (None, 0)


def board_gate_of(cid, name, nab, truth_by_path, report):
    """`full_raw.normalAbilityBoardCriteria` → 引擎可判的 `{countOptions, triggerIf, referenceValue, countSet}`。

    **默认 criteria 返回 `None`**（= 不落列；那一档 `MeetsCriteria` 恒真，见上面「落值口径」）。
    形状与 `cards_engine.json` 的 `countSet` 列**逐字同形**（复用 `count_set_of`，⛔ 别另写一份字段表）。
    """
    if not isinstance(nab, dict):
        report["bad"].append((cid, name, "normalAbilityBoardCriteria", nab))
        return None
    cc = nab.get("countCriteria")
    if not isinstance(cc, dict):
        report["bad"].append((cid, name, "normalAbilityBoardCriteria.countCriteria", cc))
        return None
    if not any(_gate_nonzero(v) for v in cc.values()) \
       and not any(_gate_nonzero(nab.get(k)) for k in _GATE_TOP_FIELDS):
        return None                      # 默认 criteria ⇒ `MeetsCriteria` 恒真 ⇒ 不落
    report["criteria"] += 1
    out = {}
    for k in _GATE_TOP_FIELDS + ("triggerIf",):
        v = _gate_int(nab, k)
        if v is None:
            report["bad"].append((cid, name, f"normalAbilityBoardCriteria.{k}", nab.get(k)))
            v = -1
        out[k] = v
    # `playerTurn` 由 `count_set_of` 一并收（它就在 `BoardCriteria` 上、与 `count` 行同源）。
    out["countSet"] = count_set_of(cid, name, cc, nab, truth_by_path, report)
    return out


def ability_gate_of(cid, name, trow, truth_by_path, report):
    """一张卡 → 只含**非默认**键的 dict（`hasActiveAbility` / `*NeedsLoading` / `quest` /
    `questTimes` / `normalAbilityBoardCriteria`）。真值里没有这张卡 ⇒ `{}`（且 `no_truth` 计数）。"""
    if trow is None:
        report["no_truth"] += 1
        return {}
    out = {}
    for col, key, bump in (("hasActiveAbility", "hasActiveAbility", "has_active"),
                           ("hasAlternativeAbility", "hasAlternativeAbility", "alt")):
        v = _gate_int(trow, key)
        if v is None:
            report["bad"].append((cid, name, key, trow.get(key)))
            v = -1
        if v != 0:
            out[col] = v
            if bump:
                report[bump] += 1
    for col, src in (("activeNeedsLoading", "activeAbility"),
                     ("alternativeNeedsLoading", "alternativeAbility")):
        ab = trow.get(src)
        v = _gate_int(ab, "needsLoading") if isinstance(ab, dict) else None
        if v is None:
            report["bad"].append((cid, name, f"{src}.needsLoading", (ab or {}).get("needsLoading")
                                  if isinstance(ab, dict) else ab))
            v = -1
        if v != 0:
            out[col] = v
            report["needs_loading"] += 1
    for col, key in (("quest", "_quest"), ("questTimes", "questTimes")):
        v = _gate_int(trow, key)
        if v is None:
            report["bad"].append((cid, name, key, trow.get(key)))
            v = -1
        if v != 0:
            out[col] = v
            report["quest"] += 1
    gate = board_gate_of(cid, name, trow.get("normalAbilityBoardCriteria"), truth_by_path, report)
    if gate is not None:
        out["normalAbilityBoardCriteria"] = gate
    report["cards"] += 1
    return out


# ==========================================================================================
#  🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16` 根因条）：原版**结构化条件** `conditionsFilter`
#    → 逐卡 `unitConditions` 列（B-05 `foreach` / B-06 `istype` 的数据侧同源）
# ==========================================================================================
# 判据（第一权威 = 全量反编译 `d:/2/tools/decomp_full/`）：
#   · 字段：`TargetCriteria.conditionsFilter`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/
#     TargetCriteria.cs:53`；同结构还有 `excludeCondition:58`）——
#     它挂在 **10 个位置**：`cardAbilities[].targetCriteria / actingCardCriteria /
#     boardCriteria.countCriteria / multiCountCriteria`、`cardAbilities[].abilityLogic[].targetCriteria /
#     .multiplier`、`activeAbility` / `alternativeAbility` 的同名槽位，以及**卡级**
#     `choicesDefinition / cardTargetCriteria / targetsAvailableCounter /
#     nonTargetScoringCriteria[] / cardTargetScoringCriteria[]`。
#   · 求值器：`FilterMethods__MeetsConditionCriteria.c`（390 行，逐值 switch）。
#     参数 `(card, UnitConditions conditionsFilter, bool exclude, CardScript thisCard)`，
#     末段 `:384-389`：`exclude ? !match : match`。
#     ⚠️ 桩里那个形参名叫 `excludeTrait`，**是错的** —— 调用点
#     `FilterMethods__CheckIfMeetsCriteria.c:298-300` 传的是 `*(undefined1 *)(param_4 + 0x3d)`
#     = `TargetCriteria.excludeCondition`（`+0x3c` 才是 `excludeTrait`，它给 `MeetsTraitCriteria`）。
#   · 枚举：`UnitConditions`（`d:/2/tools/il2cpp_out/dump.cs:20735-20819`）0/10/20/40/50/100/…/342。
#   · 我们这边为什么现在才做：审计 `资料/普查产出_1022/W64_兜底审计_战斗.md` 的 `B-16` ——
#     条件判定整层走文本正则（`EffectText.ClassifyCondition`），`istype` / `foreach` 判不了
#     （`EffectResolver.cs:9768-9772` 直接 `return false`）。**原版是数据，就按数据做**。
#
# 落值口径：
#   · **只落 `conditionsFilter != 0` 的行**（`none(0)` = 本列默认值、省略）。缺行 = 这一格**没有条件**。
#     🆕 **2026-10-24（执行代理 `G6` · `C6` §1·3 A）：`mult` / `multi` / `avail` / `choice` 四格例外** ——
#     这四格是「**整条 criteria**」行（`multiplierSet` / `multiSet` / …），回答的是「**数谁**」而不是
#     「条件是什么」⇒ `conditionsFilter == 0` **不等于「这一格没东西」**。改前那句 early-return 让
#     `conditionsFilter == 0` 的 criteria **一行都不落**：实测池内 **8 条 `multi` 行（8 张）** 与
#     **27 条 `mult` 行** 全被丢掉（`AM18` / `GOF4` 两条账的共同根因，判据 →
#     `资料/普查产出_1024/C6_最后三条判据.md` §1·3 / §四·1）。
#     ⇒ 这四格在 `conditionsFilter == 0` 时**照落行**（`cond = 0` + `name = null`），但**只落
#     `targetsAffected != 0` 的那些**（见 `criteria_carries_targets` 的 doc：`any(0)` 让其余筛全部失效，
#     落了也只是 1254 + 1574 行噪声）。⛔ **`target` / `acting` / `count` / 卡级计分那几族不动**
#     （它们的「缺行 = 没有条件」已被消费端依赖）。
#   · `ab` = `cardAbilities` 下标；`-1` = `activeAbility`；`-2` = `alternativeAbility`；`-3` = 卡级。
#   · `lg` = 所在子数组的下标（`abilityLogic` / 计分 criteria 数组）；`-1` = 直接挂在 ability/卡对象上。
#   · `role` = 上面那 10 个位置之一（见 `CONDITION_ROLES`）。
#   · `cond` = `UnitConditions` 数值（消费端按它判）；`name` = 枚举名（只给人看，⛔ 别拿名字猜语义）。
#   · `ex` = `excludeCondition`（`0/1`）—— 原版语义是**取反**（`:384-389`）。
#   · `count` 那一种**另带参数**：`opt` = `BoardCriteria.countOptions`（0 AllTargets / 10 EnemiesMinusAllies）、
#     `op` = `BoardCriteria.triggerIf`（`StatFilterOptions`，1 EqualTo / 2 EqualOrHigher / …）、
#     `ref` = `BoardCriteria.referenceValue`。没有这三个，「数出 N 个」这条条件在消费端**读不出来**。
#     ⚠️ `multi`（`multiCountCriteria`）**没有**查清配套参数 ⇒ 不落参数、⛔ 不猜（现读全池 1 条：`UM77`）。
#   · 🆕 **v16**：`count` 行再带一个 **`countSet`** = 「**数谁**」（被数集合），见下面 `COUNT_SET_*` 那一段。
#
# ⚠️ **本列只落数据、零行为变化**：消费端（`EffectResolver` / `RuleCore`）下一波才切；
#    旧的正则解析这一波**不删**（切换时再删，避免行为变化混在一起）。
CONDITION_ROLES = (
    "target",      # TargetCriteria（ability 级）
    "acting",      # actingCardCriteria
    "count",       # boardCriteria.countCriteria（另带 opt/op/ref）
    "multi",       # multiCountCriteria
    "mult",        # abilityLogic[].multiplier（TargetCriteria）
    "avail",       # targetsAvailableCounter（卡级）
    "choice",      # choicesDefinition（卡级）
    "cardtarget",  # cardTargetCriteria（卡级 · AI 评分）
    "nontarget",   # nonTargetScoringCriteria[]（卡级 · AI 评分）
    "targetscore", # cardTargetScoringCriteria[]（卡级 · AI 评分）
)
CONDITION_ROLE_ORDER = {r: i for i, r in enumerate(CONDITION_ROLES)}
# `UnitConditions.none` = 0 ⇒ 本列省略（见上面「落值口径」第一条）。
CONDITION_NONE = 0

# 🆕 **2026-10-24（执行代理 `G6` · `C6` §1·3 A）**：这四格的 criteria **无条件也照落行**。
# 判据：原版里它们都是**一个完整 `TargetCriteria`**（`multiplier` / `multiCountCriteria` /
# `targetsAvailableCounter` / `choicesDefinition`），`conditionsFilter` 只是**其中一个字段**
# （「候选还要满足什么条件」）—— 它等于 0 时，这条 criteria 仍然在回答「**数谁 / 候选池是谁**」。
# 反例（改前）：`AM18` 的 `multiCountCriteria = {ta 10 / friendly / minions / keywordFilter 10(Vehicle)
# / conditionsFilter 0}` 因为 cond = 0 **整行被丢** ⇒ 触发层拿不到「按友方载具数触发 N 遍」这条数据。
CONDITION_ROLES_OPTIONAL_COND = ("mult", "multi", "avail", "choice")


def criteria_carries_targets(cr):
    """这条 criteria 的 `targetsAffected` 是不是「**有信息**」的那一档（`:doc:` `CONDITION_ROLES_OPTIONAL_COND`）。

    判据 = 原版 `AbilityLogic__GetTargets.c:190-192`：
      `targetsAffected == 0 (any)` ⇒ **返回恰好 1 个候选，且「Any disregards all other filters」**
      ⇒ 这一条 criteria 里其余 35 格**全部失效**，它对消费端的含义只有「没有乘数 / 只触发一次 /
      候选池 = 全部」= 与「缺行」同义。
    ⇒ 只落 `targetsAffected != 0` 的（现读 8 条 `multi` + 27 条 `mult`）；
      全默认的（`multi` 1254 条 / `mult` 1574 条）**照旧不落** —— 那是零信息行，而且会让**每一张卡**
      都「有 unitConditions」，动到 `EffectText.AssignAbilityIndices` 的归位闸（⛔ 不在本笔范围内）。
    ⚠️ 坏值（非整数 / 缺键）**不在这里判**：由 `criteria_set_of` 落 `-1` 并出声（`report["set_bad"]`）。
    """
    ta = cr.get("targetsAffected")
    return isinstance(ta, int) and not isinstance(ta, bool) and ta != 0

# ------------------------------------------------------------------------------------------
#  🆕 v16（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16` 的**明确缺口**）：`count` 行的「被数集合」
# ------------------------------------------------------------------------------------------
#  v15 的 `count` 行只落了「条件是什么」（`cond`）＋「数出几个跟谁比」（`opt/op/refValue`），
#  **没落「数谁」** ⇒ 消费端读不出这条 `count` 到底遍历哪个集合（`EC58` 的两条行归不了位）。
#  原版的「数谁」= `BoardCriteria.countCriteria`（一个完整 `TargetCriteria`）＋ `BoardCriteria.playerTurn`：
#    · 字段表 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/{TargetCriteria,BoardCriteria}.cs`
#      （偏移 → `d:/2/tools/il2cpp_out/dump.cs:23823-23881` / `BoardCriteria` 同段）；
#    · 求值 → `d:/2/tools/decomp_full/FilterMethods__CheckIfMeetsCriteria.c`（逐字段判：
#      `+0x10` targetsAffected · `+0x14` friendlyUnits · `+0x15` enemyUnits · `+0x16` includeHero ·
#      `+0x17` includeMinions · `+0x18` includeSpells · `+0x19` includeWhispers · `+0x1b` includeRemnants ·
#      `+0x1d` includeSelf · `+0x1e` excludeNotSelf · `+0x21` excludeFriendlyHero ·
#      `+0x22` excludeEnemyHero · `+0x2c` keywordFilter · `+0x34` traitsFilter · `+0x3c` excludeTrait ·
#      `+0x70` filterSpecificCards · `+0x78` excludeEnchantments …）。
#  ⇒ 每条 `count` 行加一个 **`countSet` 对象**，把那 36 格**逐格落值**。
#    🔴 **⛔ 不省略默认值**：「缺席」与「值 = 默认」在消费端必须分得开（本工程的 `-1` 契约，
#       见 `Core/CardDef.cs` 的 `ConditionUnknown`）—— 缺省/坏值一律 `-1`，不是 `0`/`false`。
#    · 键名 = 原版字段名（逐字）；`playerTurn` 来自 `BoardCriteria`，其余来自 `countCriteria`；
#    · 布尔字段落 **0/1 整数**（`-1` = 缺省/坏值）；
#    · `filterSpecificCards` / `excludeEnchantments` 是 `List<RawCardScript>` ⇒ 按 `m_PathID → uniqueId`
#      解引用（与 `choicesDefinition.filterSpecificCards` 用同一张 `truth_by_path`）；解不出的**逐条出声**，
#      **不落**那一条（⛔ 不拿 path_id 当 id）。
#    · `conditionsFilter` / `excludeCondition` **不进 `countSet`** —— 它们已经是这一行的 `cond` / `ex`。
#    ⚠️ `multi`（`multiCountCriteria`）不是 `boardCriteria` 的子件、没有「被数集合」这一说 ⇒ 不落。
COUNT_SET_BOOL_FIELDS = (
    "friendlyUnits", "enemyUnits",
    "includeHero", "includeMinions", "includeSpells", "includeWhispers",
    "includeWeapons", "includeRemnants", "includeHeroParts", "includeSelf",
    "excludeNotSelf", "excludeSimilarCards", "excludeTargetted",
    "excludeFriendlyHero", "excludeEnemyHero",
    "excludeRace", "excludeTrait", "selectFromChooseResolution",
)
COUNT_SET_INT_FIELDS = (
    "targetsAffected",
    "chosenArmy", "armyFilter", "keywordFilter", "traitsFilter",
    "maxTargets", "targetSelectMethod",
    "attackFilter", "attackFilterOption",
    "currentHealthFilter", "currentHealthFilterOption",
    "manaCostFilter", "manaCostFilterOption",
    "rarityFilter", "rarityFilterOption",
    "spellType",
)
COUNT_SET_REF_FIELDS = ("filterSpecificCards", "excludeEnchantments")
# 原版 `PlayersAffected`（`BoardCriteria.playerTurn`）：0 Both / 10 Self / 20 Enemy。
# 域只有三档 ⇒ 本层**校验**（不是猜）：别的值只能是坏数据。
PLAYERS_AFFECTED_VALUES = (0, 10, 20)


def criteria_set_unknown():
    """一条「整条 criteria 缺失 / 坏行」的全 `-1` / `null` 集合。

    键序与 `criteria_set_of` **逐字相同**（`playerTurn` → 16 整数 → 18 布尔 → 2 引用）——
    字节稳定靠它。⚠️ 字段表**复用**同一个 `COUNT_SET_*` 元组（⛔ 别再抄第三份）。
    """
    out = {"playerTurn": -1}
    for f in COUNT_SET_INT_FIELDS:
        out[f] = -1
    for f in COUNT_SET_BOOL_FIELDS:
        out[f] = -1
    for f in COUNT_SET_REF_FIELDS:
        out[f] = None
    return out


def criteria_set_of(cid, name, cr, player_turn, truth_by_path, report, label="",
                    has_player_turn=True):
    """一条 `TargetCriteria` → `{"playerTurn":…, "targetsAffected":…, …, "filterSpecificCards":[…]}`。

    🆕 2026-10-24（`新账-95` #2 的 `mult`/`multi`/`avail`/`choice` 四格）：本函数从原来的
    `count_set_of`（只服务 `BoardCriteria.countCriteria`）**上提一级**，四个 role 的 criteria
    与 `count` 的「被数集合」用的是**同一套 36 格**（原版都是 `TargetCriteria`）——
    ⛔ 别为每个 role 各写一份字段表（那就是四份判据，迟早不一致）。

    🆕 2026-10-24（v25 · 执行代理 `E9`）：`cr` **不是对象**（整条 criteria 缺失 / 坏行）⇒
    **不抛异常、不逐格报 36 条**：出一声 `<criteria>`，返回 `criteria_set_unknown()`（全 `-1`）。
    调用方（`active_ability_targets_of` / `ability_target_criteria_of`）据此**照落整行** ——
    丢行 = 消费端读成「没有过滤」= 静默放行。

    · `player_turn`：**只有 `BoardCriteria` 有这一格**（`countSet` / `normalAbilityBoardCriteria`）；
      其余四格传 `has_player_turn=False` ⇒ 落 `-1` **且不报 set_bad**（那不是坏数据，是结构上没有）；
    · `label`：进 `report["set_bad"]` 的字段名前缀（`""` = 沿用 `countSet` 的老文案）。

    键的**插入顺序固定**（`playerTurn` → 16 个整数字段 → 18 个布尔字段 → 两个引用列表）
    —— 字节稳定靠它（两次 `--write` 必须逐字节相同）。
    缺省/坏值一律 `-1`（引用列表是 `None`）并进 `report["set_bad"]`/`["set_unresolved"]`（**出声**）。
    """
    out = {}

    if not isinstance(cr, dict):
        report["set_bad"].append((cid, name, label + "<criteria>", cr, "整条 criteria 不是对象"))
        return criteria_set_unknown()

    pv = player_turn
    if not has_player_turn:
        pv = -1                            # 结构上就没有这一格 ⇒ 明确的「不适用」，⛔ 不进 set_bad
    elif isinstance(pv, bool) or not isinstance(pv, int) or pv not in PLAYERS_AFFECTED_VALUES:
        report["set_bad"].append((cid, name, "playerTurn", pv,
                                  "不在 PlayersAffected（0 Both / 10 Self / 20 Enemy）"))
        pv = -1
    out["playerTurn"] = pv

    for f in COUNT_SET_INT_FIELDS:
        v = cr.get(f)
        if isinstance(v, bool) or not isinstance(v, int):
            report["set_bad"].append((cid, name, label + f, v, "不是整数"))
            v = -1
        out[f] = v

    for f in COUNT_SET_BOOL_FIELDS:
        v = cr.get(f)
        if isinstance(v, bool):
            v = 1 if v else 0
        elif not (isinstance(v, int) and v in (0, 1)):
            report["set_bad"].append((cid, name, label + f, v, "布尔字段不是 0/1/bool"))
            v = -1
        out[f] = v

    for f in COUNT_SET_REF_FIELDS:
        lst = cr.get(f)
        if not isinstance(lst, list):
            report["set_bad"].append((cid, name, label + f, lst, "不是数组"))
            out[f] = None
            continue
        ids = []
        for ref in lst:
            pid = ref.get("m_PathID") if isinstance(ref, dict) else None
            uid = truth_by_path.get(pid)
            if uid is None:
                report["set_unresolved"].append((cid, name, label + f, pid))
            else:
                ids.append(uid)
        out[f] = ids

    # ⚠️ `set_rows` 的口径 = **`countSet` 行数**（摘要行就印这个）⇒ 四格 role 的 criteria 由
    #   调用方用 `report["role_set"]` 自己计数，⛔ 别并进这一格（否则摘要会多报 10 条）。
    if has_player_turn:
        report["set_rows"] += 1
    return out


def count_set_of(cid, name, cr, board, truth_by_path, report):
    """`count` 行的「被数集合」（`BoardCriteria.countCriteria` ＋ `BoardCriteria.playerTurn`）。

    🆕 2026-10-24：实体已上提到 <see cref="criteria_set_of"/>（四个 role 共用同一套 36 格），
    本函数只保留「从 `board` 取 `playerTurn`」这一层 —— ⛔ 别再往这里加第二份字段循环。
    """
    return criteria_set_of(cid, name, cr,
                           board.get("playerTurn") if isinstance(board, dict) else None,
                           truth_by_path, report)


def condition_rows_of(cid, name, trow, cond_names, report, truth_by_path):
    """一张卡 → `[{"ab", "lg", "role", "cond", "name", "ex", ["opt","op","refValue"], ["countSet"]}, …]`。

    顺序 = `(ab, lg, role, cond)`（**字节稳定**靠它：两次 `--write` 必须逐字节相同）。
    真值快照里没有这张卡 ⇒ `report["no_truth"]`（≠「没有条件」）；
    枚举桩读不到 / 值不在枚举里 ⇒ `report["bad"]`（**丢弃**，⛔ 不落一个猜出来的名字）；
    `role == "count"` 的行另带 `countSet`（被数集合，v16；见 `count_set_of`）——
    坏值不丢整行（丢行 = 这一格变成「没有条件」= 静默放行），只把那一格落成 `-1` 并出声。
    🆕 **2026-10-24（`G6`）**：`mult` / `multi` / `avail` / `choice` 四格在
    `conditionsFilter == 0` 且 `targetsAffected != 0` 时**也落行**（`cond = 0` / `name = null`）——
    判据 → `CONDITION_ROLES_OPTIONAL_COND` 与 `criteria_carries_targets` 的 doc。
    """
    if trow is None:
        report["no_truth"] += 1
        return []
    rows = []

    def add(ab, lg, role, cr, board=None):
        if not isinstance(cr, dict):
            return
        v = cr.get("conditionsFilter", CONDITION_NONE)
        if isinstance(v, bool) or not isinstance(v, int):
            report["bad"].append((cid, name, ab, lg, role, v, "非整数"))
            return
        nm = None
        if v == CONDITION_NONE:
            # 🆕 2026-10-24（`G6` · `C6` §1·3 A）：四格「整条 criteria」**无条件也照落行**
            # （判据 → `CONDITION_ROLES_OPTIONAL_COND` / `criteria_carries_targets` 的 doc）。
            # 其余 role：none = 本列省略（缺行 = 这一格没有条件）—— 一字不动。
            if role not in CONDITION_ROLES_OPTIONAL_COND or not criteria_carries_targets(cr):
                return
            # `cond = 0` + `name = null`（`UnitConditions.none`）——消费端按「无条件」档收
            # （`CardDatabase.MapUnitConditions` 对这四个 role 放行；其余 role 仍出声丢弃）。
        else:
            nm = cond_names.get(v)
            if not nm:
                # 枚举桩缺失 / 出现枚举里没有的数值 ⇒ **不落**（`cond` 是消费端主键，编不出来）。
                report["bad"].append((cid, name, ab, lg, role, v, "不在 UnitConditions 枚举里"))
                return
        ex = cr.get("excludeCondition", 0)
        if isinstance(ex, bool):
            ex = 1 if ex else 0
        if ex not in (0, 1):
            report["bad"].append((cid, name, ab, lg, role, v, f"excludeCondition={ex!r}"))
            ex = 1 if ex else 0
        row = {"ab": ab, "lg": lg, "role": role, "cond": v, "name": nm, "ex": ex}
        if role == "count":
            if isinstance(board, dict):
                # 参数：`BoardCriteria` 上的三格（缺键一律 `-1`，⛔ 不拿 `0` 顶 —— `0` 是合法取值）。
                # ⚠️ JSON 键叫 `refValue`（不是 `ref`）—— `ref` 是 C# 关键字，DTO 那边落不了同名字段。
                for col, key in (("opt", "countOptions"), ("op", "triggerIf"), ("refValue", "referenceValue")):
                    val = board.get(key)
                    row[col] = val if isinstance(val, int) and not isinstance(val, bool) else -1
            else:
                # 结构上 countCriteria 只挂在 boardCriteria 下 ⇒ 这条不该发生；发生就是坏真值行。
                report["set_bad"].append((cid, name, "boardCriteria", board,
                                          "count 行没有 boardCriteria（被数集合无处可取）"))
            # 🆕 v16：被数集合（`countCriteria` 那 36 格 + `BoardCriteria.playerTurn`）。
            row["countSet"] = count_set_of(cid, name, cr, board if isinstance(board, dict) else {},
                                           truth_by_path, report)
        elif role in ("mult", "multi", "avail", "choice"):
            # 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：这四格也把**整条 criteria** 落下来。
            #
            # 为什么必须落：`cond` 只回答「条件是什么」，**回答不了「数谁 / 候选池是谁」** ——
            #   · `mult`    = `abilityLogic[].multiplier`（`AbilityLogic__PlayAbility.c:181-236`：
            #                 `GetTargets(multiplier)` 的**个数** = `local_178`，`any(0)` ⇒ 1，
            #                 `210/220` ⇒ 累加 `traitsFilter` 的值；再乘 `furtherMultiplier`）；
            #   · `multi`   = `CardAbility.multiCountCriteria`（`+0x40`）——
            #                 `RawCardScript__TriggerAbilities.c:230-250`：`targetsAffected != 0` 时
            #                 **整条 ability 触发 N 遍**（`N` = `GetTargets` 的个数），`0` ⇒ 只触发一次；
            #   · `avail`   = `RawCardScript.targetsAvailableCounter`（`+0x298`）—— 原版**只给 UI 数**：
            #                 `BattleCardUI__ShowAvailableTargets.c:42` → `CardScript.GetTargetsAvailable`；
            #   · `choice`  = `RawCardScript.choicesDefinition`（`+0x2A0`）——
            #                 `BattleManager__GetChoiceFullPool.c:28-35` 的选牌候选池。
            # 四格都是**一个完整的 `TargetCriteria`** ⇒ 与 `countSet` 用同一套 36 格
            # （`criteria_set_of`，⛔ 别各写一份字段表）。它们**没有 `BoardCriteria.playerTurn`**
            # ⇒ `has_player_turn=False`（落 `-1`，且**不算坏值**）。
            _label = {"mult": "multiplierSet.", "multi": "multiSet.",
                      "avail": "availSet.", "choice": "choiceSet."}[role]
            _key = {"mult": "multiplierSet", "multi": "multiSet",
                    "avail": "availSet", "choice": "choiceSet"}[role]
            row[_key] = criteria_set_of(cid, name, cr, None, truth_by_path, report,
                                        label=_label, has_player_turn=False)
            report["role_set"][role] = report["role_set"].get(role, 0) + 1
        rows.append(row)

    def scan_ability(ab, obj):
        if not isinstance(obj, dict):
            return
        add(ab, -1, "target", obj.get("targetCriteria"))
        add(ab, -1, "acting", obj.get("actingCardCriteria"))
        add(ab, -1, "multi", obj.get("multiCountCriteria"))
        board = obj.get("boardCriteria")
        add(ab, -1, "count", (board or {}).get("countCriteria"), board)
        for j, lg in enumerate(obj.get("abilityLogic") or []):
            if not isinstance(lg, dict):
                continue
            add(ab, j, "target", lg.get("targetCriteria"))
            add(ab, j, "mult", lg.get("multiplier"))

    for i, ab in enumerate(trow.get("cardAbilities") or []):
        scan_ability(i, ab)
    scan_ability(-1, trow.get("activeAbility"))
    scan_ability(-2, trow.get("alternativeAbility"))
    # ---- 卡级（只有真值 `full_raw` 里才有这几格；规范化顶层没提上来）----
    add(-3, -1, "choice", trow.get("choicesDefinition"))
    add(-3, -1, "cardtarget", trow.get("cardTargetCriteria"))
    add(-3, -1, "avail", trow.get("targetsAvailableCounter"))
    for k, cr in enumerate(trow.get("nonTargetScoringCriteria") or []):
        add(-3, k, "nontarget", (cr or {}).get("targetCriteria"))
    for k, cr in enumerate(trow.get("cardTargetScoringCriteria") or []):
        add(-3, k, "targetscore", (cr or {}).get("targetCriteria"))

    rows.sort(key=lambda r: (r["ab"], r["lg"], CONDITION_ROLE_ORDER[r["role"]], r["cond"]))
    for r in rows:
        report["by_cond"][r["cond"]] = report["by_cond"].get(r["cond"], 0) + 1
        report["by_role"][r["role"]] = report["by_role"].get(r["role"], 0) + 1
    if rows:
        report["cards"] += 1
        report["rows"] += len(rows)
    return rows


# ==========================================================================================
#  🆕 2026-10-21（引擎线 `T03·b` · 回合流程残余）：
#    **① 开局加成三列** · **② `skipCardDraw` 手牌效果的生产列**
# ==========================================================================================
#  判据（全量反编译 `d:/2/tools/decomp_full/`，逐句）：
#   · **开局加成** —— `RawCardScript.initialHandModifier(+0x118)` /
#     `initialManaModifier(+0x11C)` / `initialSpiritStoneModifier(+0x128)`（`dump.cs:23282-23286`）：
#       `PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:35-76`（起手张数）·
#       `BattleManager__SetupInitialMana.c:20-46`（`ResetMana` 的四个实参）。
#       语义与消费点 → `CardDef.InitialHandModifier` / `InitialManaModifier` /
#       `InitialSpiritStoneModifier` 的 doc（⛔ 别在这里抄第二份）。
#   · **`skipCardDraw`** —— `BuffType.skipCardDraw = 7`（`dump.cs:45441`）；
#       写在 `cardAbilities[].abilityLogic[].handBuff.cardEffect.buffType`，
#       由 `AbilityLogic__PlayAbility.c:1377-1414` → `BattleManager.AddHandEffect`
#       → `PlayerHand__AddHandEffect.c:88-139` 落到 `PlayerHand.activeEffects(+0x48)`，
#       读点是 `PlayerHand__CanDrawTurnStartCard.c:56-99`。
#
#  🔴 **只在「有值」时才落列**（三个 modifier 非 0 / 有 skipCardDraw 的卡）——
#     真值 1,351 张**一个都没有**（2026-10-21 现读：三个 modifier 全 0；`handBuff.buffType`
#     取值只有 0/2/3，**没有 7**）⇒ **卡表字节不变**、`--check` 仍一致。
#     ⚠️ **不单独提版本号**：这三列 + `skipDraws` 的「缺席」与「值 = 0 / 空表」**语义完全相同**
#     （`initial*Modifier` 的 0 是合法取值「不加不减」），所以旧表读进来不会出错、也无需出声
#     —— 与 v9..v13 那几列（缺列会被当成「全池默认」）的口径**不同**，别照抄那条 §version 守卫。
#     ⛔ 真要改这一点，先想清楚「缺席 ⇔ 0」这条等价还成不成立。
INITIAL_MODIFIER_COLS = (
    "initialHandModifier",
    "initialManaModifier",
    "initialSpiritStoneModifier",
)

# 原版 `BuffType.skipCardDraw`（`dump.cs:45441`）。⛔ 不是 `AbilityTrigger` 的编号。
SKIP_DRAW_BUFF_TYPE = 7


def skip_draws_of(cid, name, trow, trig_names, report):
    """一张卡 → `[{"keyword":…, "uses":…, "limited":…, "source":…}]`。

    只收 `abilityLogic[].handBuff.cardEffect.buffType == skipCardDraw(7)` 的条目；
    `trigger`（`AbilityTrigger` 数值）翻成**引擎触发关键词**（与 `ABILITY_TRIGGER_KEYWORD`
    同一张表），翻不出来的**不落并出声**（`report["unmapped"]`）—— ⛔ 不编造键名。
    """
    if trow is None:
        report["no_truth"] += 1
        return []
    out = []
    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            hb = lg.get("handBuff")
            if not hb:
                continue
            ce = hb.get("cardEffect") or {}
            if ce.get("buffType") != SKIP_DRAW_BUFF_TYPE:
                continue
            kw = ABILITY_TRIGGER_KEYWORD.get(tg)
            if kw is None:
                report["unmapped"].append((cid, name, trig_names.get(tg, ""), tg, i, j))
                continue
            uses = hb.get("numberOfUses", 1)
            if isinstance(uses, bool) or not isinstance(uses, int) or uses < 1:
                report["bad_uses"].append((cid, name, uses, i, j))
                uses = 1
            out.append({
                "keyword": kw,
                "uses": uses,
                # 原版 `HandEffect.limitedUses(+0x28)`：真 = 限次。**显式写 0/1**
                # （`JsonUtility` 缺列 = false，而 false 的语义是「不限次」⇒ 少写会把限次变永久）
                "limited": 1 if hb.get("limitedUses") else 0,
                "source": name + "（本回合不抽牌）",
            })
            report["rows"] += 1
    if out:
        report["cards"] += 1
    return out


def load_anim_guid_index(report):
    """`guid_map_bundles.tsv` → `{guid: (name, bundle, type, className)}`。

    ⚠️ 同一 GUID 可能有多行（Sprite + Texture2D 那一族）⇒ `setdefault` **保留第一行**；
        CardAnim 与 GameObject 两类各取第一条即可（实测每个 GUID 只有一个同名资产）。
    """
    if not os.path.isfile(ANIM_GUID_MAP_TSV):
        report["missing_map"] = 1
        print(f"⚠️ 找不到动画 GUID 表 {ANIM_GUID_MAP_TSV} —— `animTimes` 将**全空**"
              f"（重建：`python -I -X utf8 工具/gen_guid_map_from_bundles.py --cards-only`）")
        return {}
    idx = {}
    with open(ANIM_GUID_MAP_TSV, encoding="utf-8") as f:
        for line in f:
            p = line.rstrip("\n").split("\t")
            if len(p) < 9 or not p[3] or not p[8]:
                continue
            idx.setdefault(p[3], (p[8], p[4], p[6], p[7]))
    return idx


def _anim_info_of(guid, idx, cache, report):
    """CardAnim GUID → (`animInfo` dict, CardAnim 资产名, prefab 名)。查不到返回 `(None, None, None)`。"""
    if guid in cache:
        return cache[guid]
    out = (None, None, None)
    rec = idx.get(guid)
    if rec is None:
        report["unresolved_guid"] += 1
    else:
        name, bundle, _typ, cls = rec
        if cls != "CardAnim":
            report["not_cardanim"] += 1
        else:
            b = bundle[:-7] if bundle.endswith(".bundle") else bundle
            path = os.path.join(ANIM_ASSETS_DIR, "bundle_" + b, "MonoBehaviour", name + ".json")
            if not os.path.isfile(path):
                report["missing_asset"] += 1
            else:
                with open(path, encoding="utf-8") as f:
                    info = (json.load(f) or {}).get("animInfo") or {}
                pre_guid = ((info.get("animAdressable") or {}).get("m_AssetGUID") or "")
                pref = idx.get(pre_guid)
                pref_name = pref[0] if (pref and (pref[2] == "GameObject" or pref[3] == "GameObject")) else ""
                if not pref_name:
                    report["no_prefab"] += 1
                out = (info, name, pref_name)
    cache[guid] = out
    return out


def anim_times_of(trow, idx, cache, trig_names, report):
    """一张原版卡 SO → `animTimes` 列表（**逐候选**，顺序 = 原版 `abilityLogic` 索引顺序）。

    每一行：
      · `a`  = `CardAnim` 资产名（= `card_vfx_by_card.json` 的 `es[].a`，**精确 join 键**）
      · `n`  = prefab 名（`es[].n`；`a` 为空的那几条靠它 join）
      · `tg` = 原版 `CardAbility.trigger` 数值 · `tn` = 枚举名 · `rc` = `randomAbilityChoice`
      · `o`  = 原版字段路径的**索引三元组**（`i.j.k` = `cardAbilities[i].abilityLogic[j].cardAnimations[k]`；
               `-.j.k` = `activeAbility.activeAbilityLogic[j].cardAnimations[k]`）—— 审计用，短
      · `d/sd/s/m/e` = `vfxDelayTime / startDelay / timeAtStartPos / timeMoving / timeAtEndPos`
      · `u`  = 时长 = `m + s + e + d`（**原版 `AnimInfo.GetAnimDuration` 逐字**，`startDelay` 不在内）
      · `t`  = **同 `(卡, trigger)` 组内**的起点偏移 = 前面各候选的 `u` 之和（队列串行，判据见文件头）

    ⚠️ `activeAbility.activeAbilityLogic[]` 也收（VFX 表把它归到 `ability`）：`tg = -1`、`tn = ""`。
    ⚠️ 浮点**四舍五入到 4 位**（0.1 ms；原版值实测都是 0.05/0.1 这一档，见报告）。
    """
    if not trow:
        return []
    rows = []
    cursor = {}          # trigger id -> 已累计时长

    def add(guid, src, tg, tn, rc):
        info, name, pref = _anim_info_of(guid, idx, cache, report)
        if info is None:
            return
        d = float(info.get("vfxDelayTime") or 0.0)
        sd = float(info.get("startDelay") or 0.0)
        s = float(info.get("timeAtStartPos") or 0.0)
        m = float(info.get("timeMoving") or 0.0)
        e = float(info.get("timeAtEndPos") or 0.0)
        u = round(m + s + e + d, 4)
        t = round(cursor.get(tg, 0.0), 4)
        cursor[tg] = cursor.get(tg, 0.0) + u
        rows.append({
            "a": name, "n": pref, "tg": tg, "tn": tn, "rc": 1 if rc else 0, "o": src,
            "d": round(d, 4), "sd": round(sd, 4), "s": round(s, 4), "m": round(m, 4),
            "e": round(e, 4), "u": u, "t": t,
        })

    for i, ab in enumerate(trow.get("cardAbilities") or []):
        tg = ab.get("trigger")
        tn = trig_names.get(tg, "")
        rc = bool(ab.get("randomAbilityChoice"))
        for j, lg in enumerate(ab.get("abilityLogic") or []):
            for k, an in enumerate(lg.get("cardAnimations") or []):
                guid = (an or {}).get("m_AssetGUID")
                if guid:
                    add(guid, "%d.%d.%d" % (i, j, k), tg, tn, rc)
    aab = trow.get("activeAbility") or {}
    for j, lg in enumerate(aab.get("activeAbilityLogic") or []):
        for k, an in enumerate(lg.get("cardAnimations") or []):
            guid = (an or {}).get("m_AssetGUID")
            if guid:
                add(guid, "-.%d.%d" % (j, k), -1, "", False)
    return rows


def _norm_talent_key(s):
    """与 `CreatePool.Norm` **逐字同一条**归一（保留 a-z0-9、其余全丢、小写）。
    ⚠️ 两处口径必须一致：运行时会用它把卡面天赋名归一之后再查本表。"""
    if not s:
        return ""
    return "".join(ch for ch in s.lower() if ("a" <= ch <= "z") or ("0" <= ch <= "9"))


def build_talent_name_ids(truth_cards, truth_by_path, cards, report):
    """🆕 2026-10-19（`E12` · 账 `A2142`）：**天赋名 → 天赋卡 id** 的真值表。

    为什么要它：`RuleCore.SpawnTalents` 的最后一条兜底是 `CreatePool.FindByName(卡面天赋名)` ——
    **按显示名**查，卡一改名就静默失配（与 `SortByName` 同一族）。本表把原版那条**对象引用**
    （`cardAbilities[trigger==46].abilityLogic[0].summonCriteria.unitsToSummon[*]`，判据 =
    `CardScript__GetTalentCard.c`）翻成「**归一后的天赋卡名 → 我们池子的 id**」，
    兜底时先查这张表（**按 id** 取，⛔ 不再按名字模糊匹配）。

    键的三种写法都收（都归一到 `_norm_talent_key`）：我们池里的 `name` / `nameZh` ·
    真值 SO 的 `cardName` · `m_Name` 的尾段。只收**落在我们卡池里**的 id。
    ⚠️ **一个名字指向多张卡 ⇒ 整条丢掉并记账**（⛔ 不猜；调用方会退回原来的按名字路）。
    """
    pool = {c["id"]: c for c in cards}
    buckets = {}

    def add(nm, uid):
        k = _norm_talent_key(nm)
        if k:
            buckets.setdefault(k, set()).add(uid)

    for uid, row in truth_cards.items():
        for ab in row.get("cardAbilities") or []:
            if ab.get("trigger") != 46:      # AbilityTrigger.Talent
                continue
            for lg in (ab.get("abilityLogic") or [])[:1]:
                for ref in ((lg.get("summonCriteria") or {}).get("unitsToSummon") or []):
                    cuid = truth_by_path.get(ref.get("m_PathID"))
                    if not cuid or cuid not in pool:
                        continue
                    p = pool[cuid]
                    add(p.get("name"), cuid)
                    add(p.get("nameZh"), cuid)
                    t = truth_cards.get(cuid) or {}
                    add(t.get("cardName"), cuid)
                    add((t.get("m_Name") or "").split("_")[-1], cuid)
    out, conflicts = [], []
    for k in sorted(buckets):
        ids = buckets[k]
        if len(ids) == 1:
            out.append({"n": k, "id": next(iter(ids))})
        else:
            conflicts.append((k, sorted(ids)))
    report["keys"] = len(out)
    report["conflicts"] = len(conflicts)
    report["conflict_rows"] = conflicts
    return out


# 🔴 **真值 `CardTraits[].name` → 我们卡池里的**显示写法**。
#   ⚠️ **这张表不是猜的** —— 每一条的取值都是**我们卡池 `keywords` 里已经在用的那个串**
#   （2026-10-15 现算：拿 `_norm_name` 把两边归一之后逐个反查出来的，45 个 trait 里 42 个直接命中）。
#
#   🔴 **2026-10-15（`AD` 笔 · 卡牌线收口）就地订正（铁律 5）**：上面那句原来接着写
#     「只有三个（`frenzied` / `rage` / `unstoppable`）卡池里一个都没用过 ⇒ 取惯例的 Title Case」
#     —— **那个改法本身是错的**，三处都改了（后两个不再是猜测、第一个的猜法也不对）：
#     · `frenzied` → **`Destroyer`**（**官方显示名**，判据 = 官方本地化表
#       `Card_Trait/frenzied` = `Destroyer`/`毁灭者`；卡面 `SAU18 Skorpekh Destroyer` 亲读 =
#       `Remnant. Destroyer. Flank`，**全图没有 `Frenzied` 这个字**）；
#     · `rage` → **`Penitence`**（同据：`Card_Trait/rage` = `Penitence`/`忏悔`；卡面
#       `SOR10 Sisters Repentia` 亲读 = `… 。` + `[骷髅图标] Penitence: Gain +2 ✊`）；
#       ⚠️ 官方表里另有一条**化石键** `Card_Trait/frenzied_old` = `Frenzied` ⇒ 那是**改名史**的实据。
#     · `unstoppable` → **`Unstoppable`**（**官方表里根本没有这条**：全 9441 条按词搜只有成就标题等命中；
#       卡面 `SAU7 Canoptek Wraith` 亲读**也没印这个词**）。它保留 Title Case **不是「惯例」，是必需** ——
#       引擎侧 `CardDef.Prefixes` 登记的就是字面量 `unstoppable`（`RuleCore.Unstoppable`），
#       写成别的拼法会被 `KeywordTable.Normalize` 丢掉。⇒ **它没有官方显示名，这一格是「引擎的拼法」。**
#   ⚠️ **这张表的键 = 真值枚举名，值 = 进 `keywords` / `cardTraits` 的字面串** —— 两张口径（下表的值
#     与 `merge_truth_traits` 的 `unreg` 判据）**都按「值」走**，别一边按枚举名一边按显示名。
TRAIT_DISPLAY = {
    "agenda": "Agenda", "ambush": "Ambush", "armour": "Armour", "artifice": "Artifice",
    "backlash": "Backlash", "blast": "Blast", "bloodThirst": "Blood Thirst",
    "camouflage": "Camouflage", "cantAttack": "Can't Attack", "codex": "Codex",
    "companion": "Companion", "concussive": "Concussive", "cruelty": "Cruelty",
    "duty": "Duty", "ecstasy": "Ecstasy", "ephemeral": "Ephemeral", "flank": "Flank",
    "flying": "Flying", "frenzied": "Destroyer", "longrange": "Long Range", "mob": "Mob",
    "pray": "Pray", "rage": "Penitence", "rally": "Rally", "regeneration": "Regeneration",
    "regiment": "Regiment", "remnant": "Remnant", "shield": "Shield", "shuriken": "Shuriken",
    "slay": "Slay", "sniper": "Sniper", "stealth": "Stealth", "stimulation": "Stimulation",
    "stomp": "Stomp", "strike": "Strike", "swarm": "Swarm", "synapse": "Synapse",
    "talent": "Talent", "teleport": "Teleport", "tide": "Tide", "unstable": "Unstable",
    "unstoppable": "Unstoppable", "uprising": "Uprising", "vanguard": "Vanguard",
    "waystone": "Waystone",
}

# 🔴 **引擎 `CardDef.Prefixes` 里【没有登记】的 trait** —— 写进 `keywords` 会被
#   `KeywordTable.Normalize` 返回 null ⇒ `Parse` 与 `CardDef` 构造函数**双双静默丢弃**
#   （正是本工程「不许静默失败」红线，`CardDef.cs` 那几张表上一字一句写着的）。
#   ⚠️ **键 = `TRAIT_DISPLAY` 的【值】归一化之后那个串**（`merge_truth_traits` 就是这么查的），
#      ⛔ 不是真值枚举名 —— 改 `TRAIT_DISPLAY` 的值时必须同步看这里。
#
#   🔴 **2026-10-15（`AD` 笔 · 卡牌线收口）：本集合【已空】，下面三条历史留痕（铁律 5 就地订正）** ——
#     原来这里有 `{"frenzied", "rage", "unstoppable"}`（`AD` 之前那笔留下的），判据是
#     「现扫 `Prefixes`，45 个 trait 里有 4 个匹配不上」。
#     🔴 **那条判据【过期了】** —— 引擎侧已于同日登记了这三个词
#     （`Core/CardDef.cs` 的 `Prefixes`：`rage`→`Penitence` · `frenzied`→`Destroyer` ·
#      `unstoppable`→`RuleCore.Unstoppable`；判据与影响面见 `资料/普查产出_第三十一会话/AB_接线批.md` §三）。
#     ⇒ 生成器这一侧**必须跟着改**，否则「引擎认得 / 生成器仍排除」两处写法并存
#       （`RuleEngine/Core/KeywordTable.Dropped` 从 3 变 0 之后，这里还把它们挡在外面）。
#     ⚠️ **本集合仍然留着**（不是删掉）：它是**将来新 trait 的保险闸** —— 往 `TRAIT_DISPLAY`
#        加一个引擎还没登记的名字时，先加到这里、让摘要**逐条报数**（`truth_traits_of` →
#        `merge_truth_traits` 的 `skipped`），而不是静默写进 `keywords` 再被引擎丢掉。
#     ⚠️ **本笔放行的后果（`AD` 已离线逐张量过，如实记）**：真值那一层会往 `keywords` 里追加
#        `Destroyer` 10 张（**全是重名 ⇒ 被 `have` 挡下、0 张真追加**）、`Penitence` 4 张
#        （`SOR8`/`SOR21`/`SOR10`/`SOR27`；`SOR32` 本来就有）、`Unstoppable` 1 张（`SAU7`）。
#        引擎行为**不变**（那几张卡的效果是从 `Desc` 收的 —— `CardDef.CollectTriggerOps` ①，
#        ⛔ 不靠 `Has()`），变的是 `Has(penitence)` 1→5 · `Has(unstoppable)` 0→1 与卡面关键词行。
#        🔴 **2026-10-23 就地订正（引擎线 `新账-95` · 铁律 5）**：最后那半句
#        「`Has(unstoppable)` 0→1」**不成立** —— 上面那笔放行的**最终**结果被**人工层**盖掉了：
#        `数据/游戏数据/cardface_fixes.json` 的 `_manual_keywords["Canoptek Wraith"] = ["Remnant"]`
#        （236 条人工关键词里的一条；`P` 优先级最高）⇒ `SAU7` 的 `keywords` 现读 **`["Remnant"]`**、
#        `Has(unstoppable)` **恒为 0**；`Unstoppable` 只留在 `cardTraits`（**纯落值列、不喂引擎**）。
#        ⇒ 「真值合并会不会追加」与「最终 `keywords` 是什么」是**两层**，别把这行当最终读数。
#   🆕 **2026-10-18（卡牌线数据侧第二波 · `A2073` 全池对账）补一条**：`oathdouble` ——
#     真值 `CardTraits` 里有 `oathDouble`（`DefinedTrait.cs` = 1277，全池 1 张 = `UM100 Chaplain Cassius`），
#     而 `CardDef.Prefixes` **只登记了 `oath`、没有 `oathdouble`** ⇒ 写进 `keywords` 会被 `Normalize` 丢。
#     它**没有官方显示名**（`官方本地化` 的 `Card_Trait/*` 里 0 条）⇒ `TRAIT_DISPLAY` 不给它编一个；
#     `cardTraits` 列**仍照原样落枚举名 `oathDouble`**（那是纯落值列、不喂引擎），
#     但 `keywords` 这一层**不再把它写进去再被静默丢掉** —— 改为进 `skipped`、摘要里逐条报数。
#     引擎的等价能力走 `desc` 那句「Friendly Oath abilities apply an additional time.」（`CardDef.OathDouble`）。
TRAIT_ENGINE_UNREGISTERED = {"oathdouble"}


def truth_traits_of(row):
    """真值行的 `CardTraits` → **我们卡池惯用的显示写法列表**（`["Armour 1", "Long Range"]`）。

    取值的三条口径：
      · 读 **`CardTraits`**，⛔ **不读 `ShowTraits`** —— 实测（`A1748_原版trait真值.md` §6）
        `ShowTraits` 非空只 **32** 张、而与它平行的 `CardTraits` 有 **650** 张，
        且 `A1748` 那 18 张的 `ShowTraits` **全是空**（`CardTraits` 全非空）
        ⇒ **「空 `ShowTraits`」≠「没有 trait」**，判「挂了什么」只能读 `CardTraits`。
      · `_traitValue > 0` ⇒ 写成 `"<显示名> <值>"`（`Blast 2` / `Armour 1` / `Ecstasy 5`）；
        `0` ⇒ **裸词**（原版「没写数字」，与卡面 `Armour` 那种写法一致）。
        ⚠️ 引擎侧 `FirstNumber` 对裸词**兜底返回 1**（`CardDef.cs:4242` 那条）——那是引擎的既有取舍，本相不动。
      · `name == "none"`（`_trait` 0，全快照 1 张）**丢掉**。
    """
    out = []
    for t in row.get("CardTraits") or []:
        nm = t.get("name")
        if not nm or nm == "none":
            continue
        disp = TRAIT_DISPLAY.get(nm)
        if disp is None:
            disp = nm                      # 表里没有的新 trait：原样落，别猜显示写法
        v = t.get("_traitValue")
        v = int(v) if isinstance(v, (int, float)) and not isinstance(v, bool) else 0
        out.append(f"{disp} {v}" if v > 0 else disp)
    return out


def merge_truth_traits(ocr_kw, trow):
    """`keywords` 那一列的 **【权威序 第 1 层】真值铺底**（主对话已裁③，序表见下面的注释）。

    🔴 **只补空、⛔ 不覆盖** —— 逐 trait 判：`keywords` 里**已经有的名字**（归一后比对）不碰，
    只把**真值有、我们没有**的那些追加到**列表末尾**。
    （追加在末尾是**我们挑的**：`keywords` 的先后序**本地没有判据**，而追加对既有显示扰动最小。）

    🔴 **三条断言**（`施工设计 §七 已裁③` 点名要的两条 + 一条完整性；**跑完不变** ⇒ 违反就当场炸、
    ⛔ 不落盘 —— 与 `_bad` 那条同一条口径「结构错不许静默写出去」）：
      ⓐ **只补空**：追进去的每一条，在 OCR 层里都**不该**已经有（有就该被 OCR 覆盖）；
      ⓑ **OCR 已有的原样保留**：真值里与 OCR 同名的那些，返回的列表里**必须还是 OCR 那个写法**
         （不是被真值的写法换掉）；
      ⓒ **完整性**：真值里每个「引擎认得的」trait，必须落在「已有 / 已追加 / 只因引擎不认而跳过」
         三者之一 —— 漏一个就说明这一层静默吞了东西。

    返回 `(新列表, 追加了哪些, 因引擎不认而跳过的哪些, 违例说明列表)`。
    """
    kw = list(ocr_kw or [])
    _ocr_kw = list(ocr_kw or [])
    have = {}
    for k in kw:
        have.setdefault(_norm_name(re.split(r"[:\d]", str(k).strip())[0]), str(k))
    ocr_names = set(have)
    unreg = {_norm_name(x) for x in TRAIT_ENGINE_UNREGISTERED}
    added, skipped, bad = [], [], []
    for t in truth_traits_of(trow):
        nm = _norm_name(re.split(r"[:\d]", t)[0])
        if nm in have:
            continue                                # 已有 ⇒ OCR/卡面那一层说了算（不覆盖）
        if nm in unreg:
            skipped.append(t)                       # 引擎 Prefixes 没登记 ⇒ 写了也会被静默丢弃
            continue
        kw.append(t)
        have[nm] = t
        added.append(t)

    # ---- ⓐ 只补空（**结构上不可能同时满足**的判据：追加的那几条不许是 OCR 层已有的）----
    for t in added:
        if _norm_name(re.split(r"[:\d]", t)[0]) in ocr_names:
            bad.append(f"ⓐ 追加了 OCR 层已有的 `{t}` —— 应当被 OCR 覆盖、不该追加")
    # ---- ⓑ OCR 已有的那几条必须【逐字原样】留在返回列表里（真值不许换掉它们的写法）----
    if kw[:len(_ocr_kw)] != _ocr_kw:
        bad.append(f"ⓑ OCR 层那 {len(_ocr_kw)} 条在返回列表里被改动了（前 N 条必须逐字原样）")
    for t in truth_traits_of(trow):
        nm = _norm_name(re.split(r"[:\d]", t)[0])
        if nm in ocr_names and have.get(nm) is None:
            bad.append(f"ⓑ 真值的 `{t}` 与 OCR 同名、却把 OCR 那条弄丢了")
    # ---- ⓒ 完整性：真值每一条都必须有归宿（已有 / 已追加 / 只因引擎不认而跳过）----
    _all_truth = truth_traits_of(trow)
    _seen_norms = set()
    for t in _all_truth:
        nm = _norm_name(re.split(r"[:\d]", t)[0])
        _seen_norms.add(nm)
    _covered = len(added) + len(skipped) + len(ocr_names & _seen_norms)
    if _covered != len(_all_truth):
        bad.append(f"ⓒ 真值 {len(_all_truth)} 条 trait 只有 {_covered} 条有归宿"
                   f"（OCR 已有 {len(ocr_names & _seen_norms)} · 追加 {len(added)} · 跳过 {len(skipped)}）")
    return kw, added, skipped, bad


def classify_discarded(name, ocr_name, by_name, rows):
    """把「被整批丢掉的卡」按**真值**归类（`A1773` 的判别器那半）。

    返回 `(判词, uid or None, 真值行 or None)`。判词四档（顺序有意义）：
      · `未在快照` —— 快照里根本没有这张（快照 2025-07-30，多为环境/进攻卡）；
        名字有**多条候选**时也走这一档（`同名 N 条候选，不猜`）—— ⛔ 不挑一条。
      · `进攻/防御卡(另一条线)` —— `spellType ∈ {200 OffensiveCard, 210 DefensiveCard}`
        （`Assembly-CSharp/SpellType.cs`，**施工设计点名的判别器**）或
        `inventoryOptions == 5 (CantAddToDeck)` ⇒ 走 `工具/gen_offensive_cards.py` 那条线，不进主池；
      · `非收藏(生成/附魔/教程副本)` —— `inventoryOptions == 10 (NotInInventory)`
        （唯一的权威标志；🔬 这批的 `m_Name` 一律带 **`Z-NC`** 前缀 = 非收藏，教程副本还带 `_Tutorial_`）。
        ⛔ **不拿 `cantBeRandomlyCreated` 当主判据** —— 它 149 张为 1、其中 73 张是短码可收藏卡。
      · 🔴 `疑似误丢(真值是可收藏的卡)` —— 以上都不是 ⇒ **要人来判**（摘要里会逐张打出来）。
    """
    ids = []
    for k in (name, ocr_name):
        if k:
            for uid in by_name.get(_norm_name(k), []):
                if uid not in ids:
                    ids.append(uid)
    if not ids:
        return "未在快照", None, None
    if len(ids) > 1:
        return f"未在快照(同名 {len(ids)} 条候选，不猜)", None, None
    uid = ids[0]
    row = rows.get(uid) or {}
    inv = row.get("inventoryOptions")
    inv = int(inv) if isinstance(inv, (int, float)) and not isinstance(inv, bool) else 0
    spell = row.get("spellType")
    spell = int(spell) if isinstance(spell, (int, float)) and not isinstance(spell, bool) else 0
    # ① 先判「进攻/防御卡」——**这是施工设计点名的判别器**（`spellType`：200 OffensiveCard /
    #    210 DefensiveCard），而且这一档**比「非收藏」更specific**：实测 `EC60`/`EC61`
    #    两个 flag **都**成立（`spellType=210` 且 `inventoryOptions=5`），它们是**防御卡**、
    #    已在我们池里（`Armoury of Excess` / `Decadent Throne`）⇒ 必须先归到这一档，否则会
    #    被误报成「非收藏的生成卡」。⚠️ `inventoryOptions == 5 (CantAddToDeck)` 是同一条判据的第二形态。
    if spell in (200, 210) or inv == 5:
        return "进攻/防御卡(另一条线)", uid, row
    # ② 再判「非收藏」—— **只看 `inventoryOptions == 10 (NotInInventory)`**（唯一的权威标志）。
    #    ⛔ **不拿 `cantBeRandomlyCreated` 当主判据**：它 149 张为 1，其中 **73 张是短码卡**
    #    （可收藏）⇒ 拿它判会把可收藏的卡误归成「非收藏」。它只作**旁证**打出来。
    if inv == 10:
        return "非收藏(生成/附魔/教程副本)", uid, row
    return "🔴 疑似误丢(真值是可收藏的卡)", uid, row


# ---- `A1799`：其余「有真值、我们在猜」的卡 SO 字段 → 卡表新列 -------------------------------
# 判据原文 → `资料/普查产出_第二十七会话/allcards_未挖字段盘点.md` §2 与
#            `资料/普查产出_第三十一会话/Q_A1770与A1799判据.md`「A1799」那张 12 行表。
#
# 🔴 **本笔只【落值】**（与施工设计对 `attackDamageType` 的定调一致）—— 这些列的**机制/消费者另立**：
#   `attackDamageType` 是「补机制不是换数据」（我们引擎**整条没有伤害类型概念**，
#   `RuleCore.cs:5745` 注释自陈）；其余各列同理。⛔ **本笔不把它们接进 `CardDef`**
#   —— `CardDef` 住在 `RuleEngine/Core/`，不在本笔的改动白名单里。
#
# 🔴 **缺值一律 `-1`（整数）/ `""`（字符串），⛔ 不许拿 0 顶**（与 `inventoryOptions` /
#   `initiative` 同一条口径）：真值快照是 2025-07-30 的、比现池旧，我们池里
#   **118 张新卡快照里根本没有** —— 把「没数据」写成 `0` 就是静默把缺值当数据。
#
# 覆盖（2026-10-15 现算，分母 = 1009 张能在快照里按 id 找到的行）：
#   attackDamageType 965 · cardShortId 831 · useOverDraw 572 · cantBeRandomlyCreated 149 ·
#   expansionPack 181 · refNameId **0** · itemDescriptionKey 36 ·
#   fullHeroSpriteReference 50（另有 `m_SubObjectName` 可读——见下面那条特判）·
#   eventSpriteName 44 · targeted 212。
A1799_FIELDS = (
    # 我们的列名                   真值字段                              缺值  说明/出处
    ("attackDamageType",        "attackDamageType",                     -1,
     "`DamageType` 枚举（`Assembly-CSharp/DamageType.cs`）：`0 Normal` ×54（多为督军）·"
     "`4 Attack` ×1138。⚠️ 我们引擎**没有伤害类型概念** ⇒ 本笔只落值。"
     "原版消费点 = `GameStaticData__IsVulnerableDamage`（`(uint)(t-7) < 2`）+ `CardScript._ReceiveDamage:189-197`"),
    ("cardShortId",             "cardShortId",                          -1,
     "短数字 id（1…914、860 种、294 张为 0）。🔴 **⛔ 不许拿它当卡 id 换** —— "
     "Q 现核：它与 `A970` 那套（= PnP 图文件名编号）**0/763 重合**，且按名引用在 `decomp_full` 全库 **0 处**。"
     "实测形状 = 「标的是哪张卡、不是哪个卡 SO」（本体与 `(tutorial)` 副本同号），像内容作者的全局顺序号"),
    ("useOverDraw",             "useOverDraw",                          -1,
     "立绘**溢出卡框**的逐卡 opt-in（0/1）。原版消费点 = "
     "`CardDisplayWindow__ChangeCardPosition.c:75-79` → `BasicCardUI.SetImageOverDraw`。"
     "⚠️ `CardView.cs:1984,5330` 那句「值在服务器、我们拿不到」**已过期**（铁律 5）"),
    ("cantBeRandomlyCreated",   "cantBeRandomlyCreated",                -1,
     "「不能被随机生成」（0/1；301 张 = 1 = 228 张 GUID 卡 + 73 张短码卡）。"
     "⚠️ **读取点没找到**（`allcards_未挖字段盘点.md` §四）⇒ 是不是 `CreatePool` 该用的闸门**未证**"),
    ("expansionPack",           "expansionPack",                        -1,
     "`CardExpansionType` 枚举桩齐（10 LevaithanDrop1 … 80 DarkAngelsDrop1）；201 张非 0（13 种）、"
     "0 = 基础（993 张）。用途（商店/卡包/收藏筛选）另接"),
    ("refNameId",               "refNameId",                            "",
     "卡名的 I2 键后缀（`Card_Name/<值>`）⇒ **这 70 张的显示名 ≠ `cardName`**"
     "（例 `cardName`=`Normal Conditions (Black Legion)`、显示 =「Normal Conditions」）。"
     "⚠️ **本列在现池全空** —— 那 70 条**全在 GUID 卡上**，而它们在 P1 相没进我们池（Q 现核）"),
    ("itemDescriptionKey",      "itemDescriptionKey",                   "",
     "卡面「说明行」的 I2 键。现读只有两种：`Item_Description/OffenceCardExplanation`（进攻卡，"
     "「可选效果…先手可选激活」×47）与 `…/DefenceCardExplanation`（防御卡，「后手加入起手牌」×36）"
     "——**中文已在官方表里**（`itemDescriptionKey` 消费者可用）"),
    ("fullHeroSpriteReference", "fullHeroSpriteReference",              "",
     "督军**整幅立绘**引用的 `m_AssetGUID`（59 张；我们池命中 50）。"
     "⚠️ 有 `m_SubObjectName` 时优先写它（实测 `UI_Deck_…`），否则写 GUID 本身。"
     "🔴 **资产名解析不了**：`guid_map.tsv` 那 9 类引用里**没含它**（命中 0/59）—— 要另跑一次"
     "`工具/_guid_resolve_probe.py` 才拿得到文件名（⛔ 本笔不猜）"),
    ("eventSpriteName",         "eventSpriteName",                      "",
     "督军「事件」立绘的 sprite 名。⚠️ **54 张值全同** = `Warlord_events_Loken` ⇒ 多半是**图集名**、"
     "单图按 id 取（语义**未查清**，`allcards_未挖字段盘点.md` §四）—— 本笔只落值"),
    ("targeted",                "targeted",                             -1,
     "这张牌**要选目标**（0/1；218 张 = 1）。⚠️ 我们**从效果解析推**（`SimpleAI` 引 "
     "`EntityScript__get_targeted`）—— 两者是否等价**未核**，所以本笔只落值、⛔ 不替换现有推断"),
)


def truth_int(row, key, default=-1):
    """真值行的整数列 → int（缺/非数字 → `default`）。"""
    v = (row or {}).get(key)
    if isinstance(v, bool) or not isinstance(v, (int, float)):
        return default
    return int(v)


def truth_ref_name(row, key):
    """真值行的「引用」列（PPtr 或字符串）→ 字符串。

    PPtr 形状（`{m_AssetGUID, m_SubObjectName, m_SubObjectType}`）优先取 `m_SubObjectName`，
    没有再取 `m_AssetGUID`（`fullHeroSpriteReference` 只有 GUID、没有资产名 —— 见上表那条说明）。
    """
    v = (row or {}).get(key)
    if isinstance(v, dict):
        sub = (v.get("m_SubObjectName") or "").strip()
        if sub:
            return sub
        return (v.get("m_AssetGUID") or "").strip()
    return "" if v is None else str(v).strip()


# ============================================================================
#  督军后手那张防御卡（原版 `RawCardScript.goSecondCardInHand`，偏移 +0x150）
# ============================================================================
# 🔴 **2026-10-14（`A1070` 第 ③ 档）：这份数据是【原版真值】，不是我们推的。**
#   来源 = 旧 Windows 用户缓存里抢救出的原版远端包 `allcards_assets_all`（1195 张卡 SO，全字段）；
#   原件与导出 → `d:/2/远端缓存_旧profile_1013/`。判据链：那个字段是**同包内 PPtr**
#   （`m_FileID = 0`）⇒ 按 `m_PathID` 解成卡 id，55/55 全解出、0 失败。
#   在它到手之前，`RuleCore.GoesSecondCard` 的第 ③ 档只能「出声 + 不给卡」。
#   🔴 **2026-10-23 就地订正（铁律 5 · 引擎线 `P1·②`）**：上面那组 **1195 张 / 55/55 / 11 个阵营**
#      是 **2025-07-30 旧快照**的读数；中间表已改由 `工具/gen_warlord_goes_second.py` 从
#      **当前 build**（`资料/普查产出_1018/allcards_1350/cards_raw.json`，1,351 张）重生成
#      ⇒ 现读 **64 条 PPtr / 56 位督军 / 13 个阵营**（EC → `EC59 Vial Tanks` ·
#      SW → `SW59 Orbital Defences` 两张都在池内）。
#
# 🔴 **本表实测的不变量（现读 64/64 全中）**：**同一阵营的督军指向同一张** ——
#   **13 个阵营各一张**（GOF49 / SAU59 / ASH57 / UM53 / BL57 / TL54 / SOR59 / TAU62 /
#   AM60 / GSC63 / DA59 / EC59 / SW59）。
#   ⇒ 我们按【阵营】落位（`resolve_warlord_defence` 每次生成都重新断言这条不变量）。
#   ⚠️ 这是原版数据的**实测形状**，不是我们对规则的推断：原版那个字段确实是督军 SO 上的 PPtr，
#      只是这份快照里同阵营督军填的都是同一张。**断言不成立就抛**，⛔ 不静默取一张。
DEFENCE_SRC = r"d:/4/Unity/数据/游戏数据/warlord_goes_second.json"


def load_warlord_defence():
    """读 `warlord_goes_second.json` → `{督军id: {"refId","refName","warlord"}}`。

    取不到就返回空表并**出声**（卡表照旧生成，只是 `goesSecond` 一列全空 —— 不许静默）。
    """
    if not os.path.exists(DEFENCE_SRC):
        print(f"⚠️ 找不到 {DEFENCE_SRC} —— 卡表的 `goesSecond` 一列将**全空**"
              f"（`RuleCore.GoesSecondCard` 第 ③ 档会退回「出声 + 不给卡」）")
        return {}
    d = json.load(open(DEFENCE_SRC, encoding="utf-8"))
    m = d.get("map") or {}
    if not m:
        print(f"⚠️ {DEFENCE_SRC} 的 `map` 是空的 —— `goesSecond` 一列将全空")
    return m


# ============================================================================
#  卡能不能进卡组（原版 `RawCardScript.inventoryOptions` / `showInCollectionOptions`，`A1798`）
# ============================================================================
# 🔴 **2026-10-14（`A1798`）：这份数据是【原版真值】，不是我们推的。**
#   来源 = 同 `allcards` 包（1195 张卡 SO 全字段）→ `数据/游戏数据/card_inventory_options.json`，
#   由 `工具/gen_card_inventory_options.py` 生成（四级定位：原版 id → 卡名+阵营 → 卡名唯一 →
#   卡名重名但值一致；落不下的**记 -1「未知」，不猜**）。
#   ⚠️ 本件**必须在 `gen_card_inventory_options.py` 之后跑**（那个脚本要读现卡池的 id/阵营）。
#   原版用法 = `DeckUtility__ValidateDeckCards.c:68-72`（`inv != 0` ⇒ 非法）/ `:73-77`（`show != 5` ⇒ 非法）；
#   ⚠️ `CardDeck__CanAddCard.c:40` 写的是 `&&`（与前三处不一致，差集 77 张）—— 解释见那个脚本头部。
INV_SRC = r"d:/4/Unity/数据/游戏数据/card_inventory_options.json"


def load_inventory_options():
    """读 `card_inventory_options.json` → `{我们的卡id: (inv, show)}`（`-1` = 真值里查不到）。

    取不到就返回空表并**出声**（卡表照旧生成，只是那两列全 `-1` ⇒ 引擎按原版口径
    **一张都不会拦** —— 这必须让跑的人看见，不许静默退回旧口径）。
    """
    if not os.path.exists(INV_SRC):
        print(f"⚠️ 找不到 {INV_SRC} —— 卡表的 `inventoryOptions`/`showInCollectionOptions` 两列将**全是 -1**"
              f"（`DeckRules.CannotAddToDeck` 于是谁也拦不住 —— 请先跑 工具/gen_card_inventory_options.py）")
        return {}
    d = json.load(open(INV_SRC, encoding="utf-8"))
    m = d.get("map") or {}
    if not m:
        print(f"⚠️ {INV_SRC} 的 `map` 是空的 —— 两列将全 -1")
    return {k: (v.get("inv"), v.get("show")) for k, v in m.items()}


# ============================================================================
#  先后手：原版 `RawCardScript.initiative`（`A1775`，2026-10-14）
# ============================================================================
# 🔴 **原版真值**（不是我们推的）。来源 = 同 `allcards` 包（1195 张卡 SO 的 `initiative` 字段）
#   → `数据/游戏数据/card_initiative.json`，由 `工具/gen_card_initiative.py` 生成
#   （它**复用** `card_inventory_options.json` 的 `map` 做 id 匹配 —— 一处匹配、两处消费）。
#   `Initiative` 枚举（`Assembly-CSharp/Initiative.cs`）：`Undefined=0 / Low=10 / Medium=20 /
#   High=30 / VeryHigh=40`；真值实测 **1190 行 = 20 · 2 行 = 30（两张教程督军）· 3 行缺**。
#   原版用法（方法体逐句实读）：
#     `BattleManager__GetPlayerGoesFirst.c`（顶层：① 模式特例 ② 比 initiative ③ 平局缓存
#       ④ `MatchData.playerIdGoesFirst` ⑤ 掷硬币）·
#     `SupportMethods__GetPlayerGoesFirstWithInitiative.c`（比大小那一支的真身）。
#   ⚠️ `-1` = 真值里查不到这张卡（快照 2025-07-30 比现池旧）⇒ 判先后手时**不比**（退回 ③④⑤），
#      ⛔ **不许拿默认值顶上**（那会把「没数据」静默变成「有数据」）。
INIT_SRC = r"d:/4/Unity/数据/游戏数据/card_initiative.json"


def load_initiative():
    """读 `card_initiative.json` → `{我们的卡id: initiative}`（`-1` = 真值里查不到）。

    取不到就返回空表并**出声**（卡表照旧生成，只是整列 `-1` ⇒ 先后手判据退化成原版的
    最后一步「掷硬币」= 今天的行为 —— 这必须让跑的人看见，不许静默退回）。
    """
    if not os.path.exists(INIT_SRC):
        print(f"⚠️ 找不到 {INIT_SRC} —— 卡表的 `initiative` 一列将**全是 -1**"
              f"（先后手提不到先攻值、只会掷硬币 —— 请先跑 工具/gen_card_initiative.py）")
        return {}
    d = json.load(open(INIT_SRC, encoding="utf-8"))
    m = d.get("map") or {}
    if not m:
        print(f"⚠️ {INIT_SRC} 的 `map` 是空的 —— 整列将全 -1")
    return {k: v.get("initiative", -1) for k, v in m.items()}


def resolve_warlord_defence(raw_map, cards):
    """把真值表按【阵营】落到我们的卡池上 → `({阵营: 我们的防御卡id}, 报告行)`。

    每一张 refId 都要在我们卡池里**唯一定位**到一张 `type == "defence"` 的同阵营卡：
      ① 按**原版 id** 直接命中（我们卡池里 9/11 是这一档）；
      ② 按**归一化卡名**相等；
      ③ 卡名有出入时**模糊匹配**（difflib ≥ 0.80，只在本阵营的防御卡里挑）——
         实测有一处：原版写 `Helfire Torch`（原版自己的拼写），我们卡池是 `Hellfire Torch`。
    三条都落空、或落到不止一张 ⇒ **当场抛**（数据/卡池对不上是要人判的事，不是可以糊过去的）。
    """
    by_faction = {}
    for c in cards:
        if c.get("type") == "defence":
            by_faction.setdefault(c["faction"], []).append(c)

    ref_of_fac, report = {}, []
    for rid in sorted({v["refId"] for v in raw_map.values()}):
        pre = re.match(r"^[A-Za-z]+", rid)
        fac = ID_PREFIX.get(pre.group(0) if pre else "")
        if not fac:
            raise SystemExit(f"❌ 真值里的防御卡 id `{rid}` 前缀认不出阵营 —— ID_PREFIX 要补")
        cands = by_faction.get(fac) or []
        if not cands:
            raise SystemExit(f"❌ 阵营 {fac} 在我们卡池里**一张防御卡都没有** —— 真值说它有一张 `{rid}`")
        hit = [c for c in cands if c["id"] == rid]
        how = "原版 id 命中"
        if not hit:
            nm = _norm_name(next(v["refName"] for v in raw_map.values() if v["refId"] == rid))
            hit = [c for c in cands if _norm_name(c["name"]) == nm]
            how = "归一化卡名命中"
        if not hit:
            nm = _norm_name(next(v["refName"] for v in raw_map.values() if v["refId"] == rid))
            scored = sorted(((difflib.SequenceMatcher(None, nm, _norm_name(c["name"])).ratio(), c)
                             for c in cands), key=lambda x: -x[0])
            if scored and scored[0][0] >= 0.80:
                hit = [scored[0][1]]
                how = f"模糊匹配 {scored[0][0]:.2f}"
        if len(hit) != 1:
            raise SystemExit(
                f"❌ 真值里的 `{rid}` 在 {fac} 的 {len(cands)} 张防御卡里定位到 {len(hit)} 张 "
                f"（候选：{'、'.join(c['id'] + '/' + c['name'] for c in cands)}）—— 要人判，不猜")
        c = hit[0]
        if fac in ref_of_fac:
            raise SystemExit(f"❌ 阵营 {fac} 被两条真值指到两张卡（`{ref_of_fac[fac]}` 与 `{rid}`）"
                             f"—— 「同阵营督军同卡」这条不变量不成立了，`RuleCore` 那边要改口径")
        ref_of_fac[fac] = c["id"]
        report.append((rid, next(v["refName"] for v in raw_map.values() if v["refId"] == rid),
                       c["id"], c["name"], how, sum(1 for v in raw_map.values() if v["refId"] == rid)))
    return ref_of_fac, report


def load_zh():
    """读中文卡名/效果文字表 → {英文卡名: {"nameZh":..., "descZh":...}}。

    ⚠️ 原表里有 **12 处同名冲突**：大多是 `Normal Conditions` 这类**关键词术语**被各分组
    译得略有出入；真正的卡名冲突只有 4 张已知重名卡（Aggressor / Terminator /
    Terminator Champion / Maulerfiend）。合并时**后写的赢**，冲突原样记在
    zh_cards.json 的 `conflicts` 字段里，要修去那儿看。
    （这也顺带说明「卡名当 id」这个设计迟早要改。）
    """
    if not os.path.exists(ZH_SRC):
        print(f"⚠️ 找不到中文表 {ZH_SRC} —— 生成出来的卡表没有中文（卡面会显示英文）")
        return {}
    doc = json.load(open(ZH_SRC, encoding="utf-8"))
    out = {}
    for name, v in doc.get("cards", {}).items():
        nzh, dzh = (v.get("n") or "").strip(), (v.get("d") or "").strip()
        e = {}
        if nzh:
            e["nameZh"] = nzh
        if dzh:
            e["descZh"] = dzh
        if e:
            out[name] = e
    # 🔴 `A1445`（2026-10-11）：源表 `zh_cards.json` 的顶层 `count` 字段**已删** ——
    #    那是个会烂的死数（实测写着 1196、而 `cards` 里 1198 条）。这里**现算**条目数，
    #    ⛔ 别再回去读源表里任何「总数」字段（生成器只读 `cards` / `conflicts` 两处内容）。
    print(f"中文表       {len(out)} 条（源条目 {len(doc.get('cards') or {})} · 冲突 {len(doc.get('conflicts') or [])} 处）")
    return out


def load_official_cardname():
    """原版**官方中文卡名**（`A1801`）→ `(by_id, by_en, bad, stats)`。见 `OFFICIAL_LOC_SRC` 那段。

    `by_id`  = {`Card_Name/` 的键 id : 中文}`（`AM12` / `ASH83` 这种）
    `by_en`  = {官方英文名 : 中文}` —— **桥**：官方那 **1198** 条 `Card_Name` 里有 **139** 条键是
               32 位 GUID（原版包里的 `m_SubObjectName` 形态；两个数都是 2026-10-19 现读），**拿不到我们的卡 id**
               ⇒ 只能拿**卡的英文名**去官方表里找「`Card_Name/*` 的英文值 == 这个英文名」的那一条。
               ⚠️ **同名卡（Terminator Champion / Maulerfiend / Terminator）会有多条候选** ——
               现读那 5 组候选的**中文值逐组相同**，故取哪条都一样；仍然**按阵营前缀优先**取，
               并把「候选值不一致」报出来（`bad`），⛔ 不许静默挑一条。
    `bad`    = **官方表自身坏掉的条目**（**连英文都没有**）—— **现读 0 条**。
               ⛔ 这一族**绝不能进卡表**，故**整个键丢弃**、让它退回自译并出声。
               🔴 **2026-10-18 就地订正（铁律 5）**：这里原来写着「现读只有 `AM24`（中文 = `"u"`）」——
                   **那是读法错**：`Card_Name/AM24` 的值是**裸字符串**（官方表里那张卡只有英文列），
                   `v[0]`/`v[1]` 取成了字符串第 0/1 个字符 ⇒ 才有 `"B"`/`"u"`。
                   **「官方没有中文」≠「值坏了」**：现读 13 条（`AM24` + `EC65..EC75` + 1 条 GUID，
                   **池内只有 `AM24`**）⇒ 现在按「**保持英文原文**」正式收下（⛔ 不自译）。

    🔴 **为什么不能只按 id 整批换**（铁律 5·c）：官方表里有一批 `Card_Name/*` 键是 32 位 GUID
       （现读 **139** 条）⇒ **对不上我们的卡 id**，那条路只能拿英文名去桥。
       **现读（2026-10-19）：池 1127 张，id 命中 1127/1127、`by_en` 命中 0 次**
       （官方表按 id 键已覆盖全池，英文名桥一次都不触发）。
       ⚠️ **历史读数「现池 1126 张 / 英文名桥补回 137 张 / 最后 152 张两族都查不到」已过期，⛔ 别引用。**
    """
    if not os.path.exists(OFFICIAL_LOC_SRC):
        print(f"⚠️ 找不到官方中文表 {OFFICIAL_LOC_SRC} —— 卡中文名将退回 `zh_cards.json`（我们自译）")
        return {}, {}, [], None
    terms = json.load(open(OFFICIAL_LOC_SRC, encoding="utf-8")).get("terms") or {}
    by_id, by_en, bad, no_cn = {}, {}, [], []
    for k, v in terms.items():
        if not k.startswith("Card_Name/"):
            continue
        _id = k[len("Card_Name/"):]
        if isinstance(v, list):
            en = (v[0] if len(v) > 0 else "") or ""
            zh = (v[1] if len(v) > 1 else "") or ""
        else:
            en = zh = str(v or "")      # 裸字符串 = 快照里只有一列（现读只有 `AM24`）
        en, zh = en.strip(), zh.strip()
        # 🔴 **2026-10-18 就地订正（铁律 5）**：原来这里只有一条谓词「空白 / 单字符 / 没汉字 ⇒ `bad`」，
        #    于是把「**官方没有中文**」和「**官方值坏了**」混成了一档 —— 前者现读 **13 条**
        #    （`AM24` 裸字符串 + `EC65`/`EC66`…`EC75`/一条 GUID 的「只有英文列」数组；**池内只命中 `AM24`**）。
        #    更糟的是裸字符串那条被当成了 `AM24 = "u"`（`v[0]`/`v[1]` 取的是字符串的第 0/1 个**字符**）。
        #    ⇒ 现在分开：**没中文 ⇒ 保持英文原文**（⛔ 不自译，与 `load_official_display_names` 同一条口径）；
        #       **真空值**（连英文都没有）才进 `bad` 并出声。
        _no_cn = not (len(zh) >= 2 and re.search(r"[一-鿿]", zh))
        if _no_cn:
            if en:
                zh = en
                no_cn.append((_id, en))
            else:
                bad.append((_id, en, zh))
                continue
        by_id[_id] = zh
        by_en.setdefault(en, []).append((_id, zh))
    stats = {"terms": sum(1 for k in terms if k.startswith("Card_Name/")),
             "ok": len(by_id), "bad": len(bad), "nocn": len(no_cn),
             "guid": sum(1 for k in by_id if re.fullmatch(r"[0-9a-f]{32}", k))}
    print(f"官方中文表   `Card_Name/*` {stats['terms']} 条（可用 {stats['ok']} · GUID 键 {stats['guid']} ·"
          f" 官方没中文、保持英文 {stats['nocn']} · 真空值丢弃 {stats['bad']}）")
    for _id, _en, _zh in bad:
        print(f"            ⚠️ 官方值不成立、**丢弃**：`Card_Name/{_id}` = ({_en!r}, {_zh!r})"
              f" —— 这张卡退回我们自译（理由见 `load_official_cardname` 的 docstring）")
    return by_id, by_en, bad, stats


def official_name(by_id, by_en, cid, name, faction, conflicts, bridge=None):
    """一张卡的官方中文名（两级查，见 `load_official_cardname`）→ `(值 or None, 命中路, 官方键 or None)`。

    🔴 **第三个返回值（官方键）是 `A1763` 数据层要的**：官方表里 `Card_Name/*` 与
    `Card_Description/*` **共用同一个键空间**（都是卡 id / 32 位 GUID）⇒ 这里解出来的那个键
    就是去 `Card_Description/` 查描述的钥匙（否则那 147 个 GUID 键的卡一张都查不到）。

    🔴 **`by_en` 是英文名桥 —— 不许静默走（`A2050` / `N5`，2026-10-18）**：
      第 ① 级（`cid in by_id`，按 id 直查）是**判据唯一**的那条路；第 ② 级（`by_en`，拿卡名去
      官方表里找英文值相同的那一条）是**启发式** —— 同名卡 / 改名都会让它**静默换一张卡的中文**。
      现读（2026-10-18）池内 **1127/1127 走第 ① 级、`by_en` 命中 0 次**（官方表按 id 建键已覆盖全池）。
      ⇒ 本函数把每一次 `by_en` 命中都**记进 `bridge` 列表**（调用方跑完逐张打出来）；
      没传 `bridge` 时**当场 print 报警**（任何调用点都逃不掉「出声」这一条）。
      ⛔ 别改回「命中就悄悄用」—— 那正是 `A2050` 要治的静默。
    """
    if cid in by_id:
        return by_id[cid], "id", cid
    cands = by_en.get(name)
    if not cands:
        return None, None, None
    # ⚠️ 走到这里 = 英文名桥要出手了 —— 先出声（有 `bridge` 就记账、由调用方汇总打印）
    if bridge is not None:
        bridge.append((cid, name, cands[0][0]))
    else:
        print(f"⚠️ 英文名桥接：我们 id `{cid}`（`{name}`）在官方 `Card_Name/*` 里**没有 id 键**，"
              f"退回按英文名匹配到键 `{cands[0][0]}` —— 该路是启发式，⛔ 别当判据（`A2050`/`N5`）")
    if len(cands) > 1:
        vals = {z for _k, z in cands}
        if len(vals) > 1:
            conflicts.append((cid, name, cands))
        # 同名卡：取**阵营前缀相同**的那条（取不到就按官方表的先后序取第一条）
        pre = (faction or "").upper()
        for _k, _z in cands:
            m = re.match(r"^([A-Za-z]+?)(?:_|[0-9]|$)", _k)
            if m and m.group(1).upper() == pre:
                return _z, "en", _k
    return cands[0][1], "en", cands[0][0]


def load_official_display_names():
    """原版**官方显示名**（`name` 英文 + `nameZh` 中文）→ `(by_id, stats)`。见 `OFFICIAL_LOC_SRC`。

    🔴 **2026-10-18（用户改判）**：此前「**卡名一个字不动**」的口径**作废** —— 那是 PnP 时代定的
    （当时卡名以 PnP 印刷品 / 卡图资产名为准）。现在改为**官方显示名**（原版客户端里真正显示的那串字）。
    判据 = `资料/普查产出_1018/R_卡牌线_完整ID与SO后的调整评估.md` §3 `N3`（「32 张卡名 ≠ 当前官方显示名」）。
    **现读（2026-10-18）= 64 张**：32 张是**词/语义不同**（`Ork Spanner` → `Spanner` 这一类），
    另 32 张只差**大小写 / 撇号 / 连字符 / 句点**（`Fire prism` → `Fire Prism` 这一类）——
    两批**都换**（显示名逐字符照官方，大小写也是显示的一部分）。

    与 `load_official_cardname()` 的分工：那个只管**中文**（`nameZh`），本函数**两列一起**取。
    两者现在对「官方没有中文」**同一口径**：`AM24` 的官方条目是**裸字符串**（en+zh 快照里只有英文列；
    `官方本地化_13lang.json` 的 `zh` 列也是英文）⇒ 按「**保持英文原文**」处理（⛔ 不自译）。
    （⚠️ 2026-10-18 之前 `load_official_cardname` 把这条误判成「值坏了」整条丢弃；已就地订正。）
    """
    if not os.path.exists(OFFICIAL_LOC_SRC):
        print(f"⚠️ 找不到官方表 {OFFICIAL_LOC_SRC} —— 卡名保持源表写法（`name`）/ 自译（`nameZh`）")
        return {}, None
    terms = json.load(open(OFFICIAL_LOC_SRC, encoding="utf-8")).get("terms") or {}
    by_id = {}
    for k, v in terms.items():
        if not k.startswith("Card_Name/"):
            continue
        _id = k[len("Card_Name/"):]
        if isinstance(v, list):
            en = (v[0] if len(v) > 0 else "") or ""
            zh = (v[1] if len(v) > 1 else "") or ""
        else:                      # 裸字符串 = 只有英文列（现读只有 `AM24` 一条）
            en = zh = v or ""
        en, zh = en.strip(), zh.strip()
        if not en:                 # 英文都没有 ⇒ 这条没用（⛔ 不拿中文顶英文）
            continue
        by_id[_id] = (en, zh or en)
    stats = {"keys": sum(1 for k in terms if k.startswith("Card_Name/")), "ok": len(by_id)}
    print(f"官方显示名   `Card_Name/*` {stats['keys']} 条（可用 {stats['ok']} · "
          f"裸字符串 {stats['keys'] - stats['ok'] if stats['keys'] > stats['ok'] else 0}）")
    return by_id, stats


def load_official_desc():
    """原版**官方中文卡面描述**（`A1763` 的数据层）→ `(by_id, bad, stats)`。见 `OFFICIAL_LOC_SRC`。

    键空间 = `Card_Description/*`（`AM12`/`ASH83` 这种短 id，另有 **147 个 32 位 GUID 键**）
    —— 与 `Card_Name/*` **同源**（判据：现读 `Card_Description` 里那 147 个 hex 键
    全部也在 `Card_Name` 里）⇒ 取值的钥匙一律先用 `official_name()` 解出来的**官方键**，
    再拿它查这张表（`build()` 里那一段）。

    🔴 **那 181 条必须挑出来**（与 `load_official_cardname` 里「值不成立就丢弃」同一条纪律 ——
       ⚠️ 那条纪律原来举的 `AM24 = "u"` 是**读法错**，2026-10-18 已订正，见那个函数的 docstring）。
    ⚠️ **2026-10-15（第三十二会话）就地订正（铁律 5）**：本段原来写的是
    「官方表里有一批**被截断的**条目 —— 现读全表 1098 条里 **181** 条的值**就是单个字符**：
    `(` 91 条 · `[` 89 条 · `N`/`o` 1 条」—— 🔴 **那是读错了**：真因 = 原表里 **en == zh**
    的条目被压成了**单个字符串**（出处 = `工具/_loc_official/w4_lib.py:30`），而旧代码对字符串取 `v[1]`
    = **第二个字符** ⇒ `'[[flank]]'` 取出 `'['`、`'((…))'` 取出 `'('` ⇒ 那 181 条**好值**被误判成「被截断」。
    **数据本身是好的、不短**（多数 8–80 字符）。**行为不变**（单值 = 没有中文列可换
    ⇒ 仍保持我们自译），但**理由与文案已改对**。
    判据仍然是「**值不是 `[en, zh]` 两元素表**（或 zh 去空白后短于 3 字符）就丢」：
    实测**恰好**命中这 181 条、一条不多一条不少 ⇒ **可用 = 1098 − 181 = 917**，
    与 `资料/普查产出_第三十一会话/C_远端判据重判.md` §〇 写死的「`Card_Description/*`
    **1098 键 / 917 非空**」**逐字吻合**（那 181 条就是它说的「空」）。

    ⚠️ **不拿「值里没有汉字」当判据**：官方正文是**模板层**，有一大批值整条只有记号
    （例 `ASH6` = `[[waystone]]。[[stealth]]。[[sniper]]`，393 条无汉字）—— 那是**正常值**。
    """
    if not os.path.exists(OFFICIAL_LOC_SRC):
        print(f"⚠️ 找不到官方中文表 {OFFICIAL_LOC_SRC} —— `descZh` 将只有 `zh_cards.json` 那一份自译")
        return {}, [], None
    terms = json.load(open(OFFICIAL_LOC_SRC, encoding="utf-8")).get("terms") or {}
    # 🔴 **2026-10-15（第三十二会话）就地修：同一张卡有「两代」键，「`_new` 那代必须赢」。**
    #   官方表里对同一张卡可以有 `Card_Description/<id>` 与 `Card_Description/<id>_new` **两条**
    #   （现读 16 对）。实测 **`_new` 那代才与 PnP 成品卡图印的一致** —— 逐张开图核过：
    #     · `TL66 Neurogaunt`  卡面印 `Tide 2`（新代），而旧代是 `Tide 4`
    #       （`d:/2/Warpforge部队卡片/Tyranid/3部队/Warpforge_4_Neurogaunt.png`）
    #     · `TL44 Hardened Biology` 卡面印 `+1 Health`（新代），而旧代是 `+2`
    #       （`Tyranid/4计策/Warpforge_55_Hardened-Biology.png`）
    #   PnP = 内容的尺子（铁律 7 / 铁律 10 第 3 条）⇒ **`_new` 赢**。
    #   ⚠️ 改前那 16 条 `_new` 被当成**独立的键**（`SOR30_new`）存进 `by_id`，
    #   而卡 id 是 `SOR30` ⇒ **一条都查不到** ⇒ 那 16 张全部退回旧代。
    #   实测代价：`TL66` / `TL44` / `UM68` / `UM72` 四张的**数值是错的**（见上）。
    #   ℹ️ 不在人工层（`cardface_fixes.descZh`）里的那几张才会被本修改到 ——
    #   人工层仍然**最后生效**（层序不变），例 `Celestian Sacresant` 人工值本来就是 `_new` 语义。
    #
    # ⚠️ **同日订正（铁律 5）**：下面原来写着「官方表里有一批**被截断的**条目 —— 现读全表 1098 条里
    #   **181** 条的值**就是单个字符**：`(` 91 条 · `[` 89 条 · `N`/`o` 1 条」**—— 那是读错了**：
    #   真因 = 原表里 **en == zh** 的条目压成了**单个字符串**（出处 = `工具/_loc_official/w4_lib.py:30`），
    #   而旧代码对字符串取 `v[1]` = **第二个字符** ⇒ `'[[flank]]'` 取出 `'['`、`'((…))'` 取出 `'('`
    #   ⇒ 那 181 条**好值**被误判成「被截断」。**行为不变**（单值 = 没有中文可换，
    #   仍保持我们自译），但**报告文案与推理得改对**。（当日另有该日志落盘。）
    by_id, bad, superseded = {}, [], []
    for k, v in terms.items():
        if not k.startswith("Card_Description/"):
            continue
        _id = k[len("Card_Description/"):]
        _isnew = _id.endswith("_new")
        if _isnew:
            _id = _id[:-4]
        if isinstance(v, str):
            # en == zh 的单值（现读 181 条）⇒ **没有中文可换**，保持我们自译
            bad.append((_id, v, "单值(en==zh)"))
            continue
        en = (v[0] if len(v) > 0 else "") or ""
        zh = ((v[1] if len(v) > 1 else "") or "").strip()
        if len(zh) < 3:
            bad.append((_id, en, zh))
            continue
        if _isnew:
            superseded.append((_id, by_id.get(_id), zh))
            by_id[_id] = zh            # 🔴 `_new` **覆盖**旧代
        elif _id not in by_id:          # 不让旧代盖掉已写入的新代（`terms` 的序不保证）
            by_id[_id] = zh
    stats = {"terms": sum(1 for k in terms if k.startswith("Card_Description/")),
             "ok": len(by_id), "bad": len(bad), "superseded": len(superseded),
             "guid": sum(1 for k in by_id if re.fullmatch(r"[0-9a-f]{32}", k))}
    print(f"官方中文效果 `Card_Description/*` {stats['terms']} 条（可用 {stats['ok']} · GUID 键 {stats['guid']} ·"
          f" 值不成立丢弃 {stats['bad']} · `_new` 新代生效 {stats['superseded']}）")
    if superseded:
        print(f"            ✅ `_new` 新代**覆盖旧代** {len(superseded)} 张："
              + " · ".join(_i for _i, _o, _n in superseded))
    if bad:
        _bc = {}
        for _t in bad:
            _zh = _t[2]
            _k = _zh if _zh in ("(", "[", "单值(en==zh)") else "其它"
            _bc[_k] = _bc.get(_k, 0) + 1
        print(f"            ⚠️ 整条丢弃 {len(bad)} 条（"
              + " · ".join(f"{k!r}×{n}" for k, n in sorted(_bc.items())) + "）"
              " —— 单值那一档是**原表里 en == zh**（没有中文可换），这些卡保持我们自译")
        for _t in bad[:6]:
            print(f"            · `Card_Description/{_t[0]}` = ({_t[1]!r}, {_t[2]!r})")
        if len(bad) > 6:
            print(f"            … 其余 {len(bad) - 6} 条同理")
    return by_id, bad, stats

def norm_int(v):
    """None/非数字 → 0。原始数据里 null 很常见（59 张卡无数值）"""
    if v is None:
        return 0
    if isinstance(v, bool):
        return 0
    if isinstance(v, (int, float)):
        return int(v)
    return 0


def norm_str(v):
    return "" if v is None else str(v)


def norm_list(v):
    if v is None:
        return []
    if isinstance(v, list):
        return [str(x) for x in v if x is not None and str(x).strip()]
    return [str(v)]


# ==========================================================================================
#  🆕 2026-10-25（引擎线 `X20`）：**隐藏附魔模板入池**（判据驱动，现读入池 1 张）
# ==========================================================================================
# 判据 / 能力表 / 逐张缺口 → `资料/普查产出_1025/X20_PrayEnchantment入池_执行报告.md`
#                            ＋ `资料/普查产出_1025/X20_附魔能力表.tsv`。
#
# 🔴 **为什么不能塞进主循环的 `raw`**（`card_stats.json`）：主循环那一整套层是为**可打出的卡**
#    准备的（宝石稀有度表 / 卡面逐张修正 / `armor` 家族判据 / `art` 守卫 / 中文表 / 官方名）。
#    隐藏附魔卡在那些表里**一条判据都没有** —— 塞进去只会得到「空稀有度」「art 为空」这类
#    假缺口，而且 `inventoryOptions` 那一层会按 `card_inventory_options.json` 查不到而落 `-1`
#    （`-1` = 「引擎不拦」⇒ 这张卡会出现在卡组编辑器里！）。⇒ **单独一条窄路，只造 entry**。
#
# 🔴 **普查读数（2026-10-25 现读，`cards_raw.json` 1,351 张）**：
#    · 名字含 `enchant` 的 **120** 张：`cardType == 20` 的附魔模板 **118** 张 ＋ 2 张兄弟卡
#      （`Wurrboy` / `Sergeant Gadriel`，`cardType 0`、SO 名尾也带 `_enchantment`，**本体是部队卡**、
#      两张都不在我们池里）；
#    · `cardType 20` **不是附魔专用型** —— 全表 680 张都是 20（含普通战术卡）；
#    · `buffEffect.enchantment` 的非零 PPtr 引用 **260 条 / 114 张父卡**（其中 **63 张父卡在池内**）；
#      `buffType == 15` 的父卡 **39 张**（36 张在池内）。
#      ⚠️ 「`enchantment` 字段」那个键在**每个 `buffEffect` 对象里都有**（13012 处 / 1351 张卡）——
#        **键存在 ≠ 引用**：要判「引用了哪张附魔」只看 `enchantment.m_PathID != 0`。
#
# 🔴 **为什么现读只入池 1 张**（⛔ 不是「按名字挑的」）：
#    判据 = ① 被**池内卡**以 `buffEffect{buffType 12/15/16, enchantment}` 引用到（否则没人挂它 = 死卡）
#          ∧ ② 它自己 `cardAbilities[].abilityLogic[]` 的 `(trigger, chosenAbility)` 形状
#               **逐条命中**一个**已实现**的附魔执行点（`ENCHANTMENT_EXECUTORS`，逐条给代码位置）。
#    现读：有池内挂载者的模板 **32** 张，其中只有 `Pray Enchantment` 命中执行点
#    （它的两跳在 `RuleCore`/`Enchantments` 里是手接的）⇒ 入池 1 张；
#    31 张**有池内挂载者但形状不支持**（逐张缺口 → 那张 TSV），**87 张没有池内挂载者**。
#    ⛔ **共同缺的那一跳是同一个**：引擎**没有**「被动 `cardAbilities[].abilityLogic[]`」的解释器
#      （`EffectResolver` 只解释 `activeAbility` / `alternativeAbility` 的 `chosenAbility`，v23/v24）。
#      ⇒ 把这 31 张收进来 = 有数据没人用，而且会改掉 `create` / 开包按阵营摇号的确定性结果。
#
# ⚠️ 这一层的条目**故意只落「它需要的列」**：下面列出的那些没落的列（`animTimes` / `abilityTargets` /
#    `abilities` 各类 criteria …）在 `CardDef` 里读出来是「未知」，而这张卡
#    **永远不进手牌/棋盘/卡面**（`showInCollectionOptions = 0`）⇒ 那些列**没有可达的消费者**。
#    不落 ≠ 不知道：真值在 `cards_raw.json` 里，接显示/动画那条链时再补（⛔ 别在这里编造空行）。
# ------------------------------------------------------------------------------------------
# 引擎【已实现】的附魔执行点（**唯一判据**）—— 每一格都指向真代码，⛔ 不许写「机制没接」这种空话。
#
# 一个附魔模板能入池 = ① 它被**池内卡**以 `buffEffect{buffType 12/15/16, enchantment → 它}` 引用到
#                     （否则挂了也没人挂 = 死卡）**且** ② 它自己 `cardAbilities[].abilityLogic[]` 的
#                     `(trigger, chosenAbility)` 形状**逐条**命中某个执行点。
#
# ⚠️ **`ids` 是执行点自己的适用范围，不是「我挑的白名单」**：今天这两跳（受伤即撤 / 自己回合开始
#    +1 信仰、一次性）在 `Core/RuleCore.cs` + `Core/Enchantment.cs` 里是**按这张卡的 id 认的**
#    （`Enchantments.PrayEnchantmentCardId`）。同形状的别的卡**真不会跑** —— 所以 `ids` 必须写上；
#    而 `pairs` 是那个执行点**假设**的卡面形状 ⇒ 数据一漂（模板改了一条 ability）就**当场炸**，
#    ⛔ 别让它静默变成「入池了但什么都不做」。
# 要扩大这个集合 = 先接一个通用执行器（被动 `cardAbilities[].abilityLogic[]` 的解释器），
# 那是独立一件（缺口清单见 `资料/普查产出_1025/X20_附魔能力表.tsv` 与报告 §缺口）。
ENCHANTMENT_EXECUTORS = (
    {
        "name": "Pray Enchantment",
        "ids": ("9218576fd3e26554db5b4c239a36dbc7",),   # Z-NC Sororitas-Pray_enchantment
        # 卡面形状 = ability[0] `UnitDamaged(60) → removeThisEnchantment(340)`；
        #            ability[1] `TurnStartPlayer(31) → buffMana(60)` 然后 `removeThisEnchantment(340)`。
        "pairs": ((60, 340), (31, 60), (31, 340)),
        "why": "挂 = RuleCore.UseAlternativeCore 的 Pray 支 → Enchantments.AttachPray；"
               "结算 = RuleCore.BeginTurn → GrantPrayEnchantmentFaith（+1 信仰后 RemovePray）；"
               "中断 = RuleCore.ApplyDamage / 疲劳支 → Enchantments.RemovePray",
    },
)
ENCHANTMENT_ATTACH_BUFFTYPES = (12, 15, 16)     # `CardEffect__IsEnchantment.c:9-12`


def _enchant_shape(r):
    """模板自己的 `cardAbilities[].abilityLogic[]` → `[(trigger, chosenAbility), …]`（**保序、保留重复**）。"""
    out = []
    for a in (r.get("cardAbilities") or []):
        for lg in (a.get("abilityLogic") or []):
            out.append((a.get("trigger"), lg.get("chosenAbility")))
    return out


def build_enchantment_entries(truth_cards, raw, cards, truth_by_path=None,
                              anim_idx=None, anim_cache=None, anim_trig_names=None,
                              anim_report=None):
    """从真值快照造「隐藏附魔模板」的卡池条目 → `(entries, report)`。

    **判据**（不是名字清单 —— 见 `ENCHANTMENT_EXECUTORS` 顶上那段）：
      ① 被**池内卡**以 `buffEffect{buffType∈{12,15,16}, enchantment → 它}` 引用；
      ② 它自己的 `(trigger, chosenAbility)` 形状**逐条**命中一个已实现执行点（`ENCHANTMENT_EXECUTORS`）。
    `report` = 只记账的读数（普查张数 / 有池内挂载者的 / 入池的 / 未入池的原因），由调用方打印。

    🔴 **2026-10-25（`X20` 第二版 · 主对话 `RuleEngineTest` 6 红后修）**：本层**必须与主循环落同一批列**。
       第一版只手写了「它用得到的列」⇒ 那 9 个「**每张卡都落**」的列在它身上是 `null`，而 `null`
       的语义是「**整列缺席**（旧表 / 调用方没给）」—— 与「没有行 = `[]`」**必须分得开**
       （这是 `CardDef`/`CardDatabase` 的契约，也是测试 A2 那几条判据）。⇒ 本函数现在**逐列调用主循环
       用的同一个 helper**（`ability_triggers_of` / `ability_targets_of` / `active_ability_logic_of` …）：
       没有行的落 `[]`、有真值行的落真值。⛔ 别改回「只写需要的列」。
    """
    report = {"mname_hits": 0, "templates": 0, "siblings": [], "pooled": [],
              "pending": [], "skipped_reason": [], "no_pool_attach": [], "shape_mismatch": [],
              "attach_rows": 0, "attach_cards_in_pool": 0}
    # 真值 `cardArmy`（数值）→ 我们的阵营名：从**源表**那对 `(faction, factionId)` 现建，
    # ⛔ 不写死 80 = Sororitas（那是判据的复制，迟早漂）。
    army2fac = {}
    for c in raw:
        _fid = norm_int(c.get("factionId"))
        _fac = norm_str(c.get("faction"))
        if _fid > 0 and _fac:
            army2fac.setdefault(_fid, _fac)

    # ---- ① 谁把哪个模板挂上去（只看 `buffEffect.enchantment` 的非零 PPtr）----
    pool_ids = {c.get("id") for c in cards}
    attach = {}          # 模板 path_id → [(owner 卡名, owner uid, owner 在池?, owner cardType, buffType, owner trigger)]
    for owner in truth_cards.values():
        for a in (owner.get("cardAbilities") or []):
            for lg in (a.get("abilityLogic") or []):
                be = lg.get("buffEffect") or {}
                if be.get("buffType") not in ENCHANTMENT_ATTACH_BUFFTYPES:
                    continue
                en = be.get("enchantment") or {}
                pid = en.get("m_PathID") if isinstance(en, dict) else None
                if not pid:
                    continue
                attach.setdefault(pid, []).append(
                    (norm_str(owner.get("cardName")), owner.get("uniqueId"),
                     owner.get("uniqueId") in pool_ids, owner.get("cardType"),
                     be.get("buffType"), a.get("trigger")))
    report["attach_rows"] = sum(len(v) for v in attach.values())
    report["attach_cards_in_pool"] = len({r[1] for v in attach.values() for r in v if r[2]})

    # ---- ② 全表普查（只记账）：名字含 `enchant` 的分「`cardType 20` 模板」与「兄弟卡」----
    for r in truth_cards.values():
        if "enchant" not in norm_str(r.get("m_Name")).lower():
            continue
        report["mname_hits"] += 1
        if r.get("cardType") == 20:
            report["templates"] += 1
        else:
            report["siblings"].append((norm_str(r.get("cardName")), r.get("cardType"),
                                       norm_str(r.get("m_Name"))))

    # ---- ③ 按判据挑（+ 形状硬校验；不符 = 数据漂了，当场炸）----
    want = {}            # uid → executor
    for ex in ENCHANTMENT_EXECUTORS:
        for uid in ex["ids"]:
            r = truth_cards.get(uid)
            if r is None:
                report["skipped_reason"].append(f"{uid} —— 真值快照里没有这一行（⛔ 不编造）")
                continue
            shape = tuple(_enchant_shape(r))
            if shape != tuple(ex["pairs"]):
                sys.exit(f"❌ 附魔模板形状漂了：{ex['name']}（{uid}）现在的 ability 形状 = {shape}，"
                         f"而引擎执行点假设的是 {tuple(ex['pairs'])} —— 先核执行点，"
                         "⛔ 别改这里的期望值把红抹平。")
            want[uid] = ex

    entries = []
    have = {c.get("id") for c in cards}
    for r in truth_cards.values():
        if r.get("cardType") != 20 or "enchant" not in norm_str(r.get("m_Name")).lower():
            continue
        uid = r.get("uniqueId")
        owners = attach.get(r.get("path_id"), [])
        owners_pool = [o for o in owners if o[2]]
        if uid not in want:
            report["pending"].append((norm_str(r.get("cardName")), uid, bool(owners_pool), len(owners)))
            if owners_pool:
                report["shape_mismatch"].append(
                    (norm_str(r.get("cardName")), tuple(_enchant_shape(r)), sorted({o[0] for o in owners_pool})))
            else:
                report["no_pool_attach"].append(norm_str(r.get("cardName")))
            continue
        if not owners_pool:
            # 执行点有、但**没有池内卡会挂它** ⇒ 入池 = 死卡（判据 ①）。
            report["skipped_reason"].append(
                f"{uid} —— 形状命中执行点，但没有**池内**卡引用它（挂载者 {len(owners)} 条全在池外）"
                "⇒ 不入池（死卡）")
            continue
        if uid in have:
            report["skipped_reason"].append(f"{uid} —— 主循环已经收过这张卡（id 会撞）⇒ 不重复入池")
            continue
        army = r.get("cardArmy")
        faction = army2fac.get(army)
        if not faction:
            report["skipped_reason"].append(
                f"{uid} —— `cardArmy = {army}` 在源表里找不到对应阵营名（⛔ 不猜）")
            continue
        rarity = TRUTH_RARITY.get(r.get("cardRarity"), "")
        if not rarity:
            report["skipped_reason"].append(
                f"{uid} —— `cardRarity = {r.get('cardRarity')}` 不在 1..5（⛔ 不猜）")
            continue
        # 🔴 隐藏卡的**判据本身**：`inventoryOptions = 10 NotInInventory` ∧ `showInCollectionOptions = 0 Hide`。
        #    这两格一漂，这张卡就会出现在卡组编辑器/收藏/开包里 ⇒ 当场炸，⛔ 不静默放过。
        inv = truth_int(r, "inventoryOptions", -1)
        show = truth_int(r, "showInCollectionOptions", -1)
        if inv != 10 or show != 0:
            sys.exit(f"❌ 附魔模板 {norm_str(r.get('cardName'))}（{uid}）的隐藏位变了："
                     f"inventoryOptions={inv}（期望 10 NotInInventory）· "
                     f"showInCollectionOptions={show}（期望 0 Hide）—— "
                     "它会被卡组编辑器/收藏/开包看见；先核判据再改本行。")
        name = norm_str(r.get("cardName")).strip() or norm_str(r.get("m_Name")).split("_")[0]
        entry = {
            "id": uid,
            "name": name,
            "type": "enchantment",
            # 数值照真值直搬（它**从不被打出**：`cost` 只是把原版 SO 的值如实带过来）
            "cost": norm_int(r.get("manaCost")),
            "attack": norm_int(r.get("meleeAttack")),
            "health": norm_int(r.get("maxHealth")),
            "ranged": norm_int(r.get("rangeAttack")),
            "keywords": [],
            # 原版**没有卡面文字**（隐藏卡、无 PnP 成品）⇒ 空串。⛔ 别编一句英文塞进来。
            "desc": "",
            "faction": faction,
            "rarity": rarity,
            "subtype": "",
            "armour": norm_int(r.get("armour")),
            "cardTraits": norm_list(r.get("CardTraits")),
            "spellType": truth_int(r, "spellType", -1),
            # 🆕 2026-10-25（P3 · 账 3 条 C）：原版 `RawCardScript.specialAttackScoringCriteria`（bool）。
            #   语义 / 判据 / 消费点 → `CardDef.SpecialAttackScoring` 的 doc（⛔ 别在这里抄第二份）。
            #   真值 1,351 张**全 0** ⇒ 本列今天不改变任何行为，只把「原版有这一维」带进卡表。
            #   ⚠️ bool ⇒ 缺键即 0（没有「未知」档），⛔ 不设 -1。
            "specialAttackScoring": truth_int(r, "specialAttackScoringCriteria", 0),
            "inventoryOptions": inv,
            "showInCollectionOptions": show,
            "initiative": truth_int(r, "initiative", -1),
        }
        # ==========================================================================================
        #  🔴 **2026-10-25（`X20` 第二版）：其余列必须与主循环**逐列同形**。
        #     判据 = `CardDef`/`CardDatabase` 的契约 + 测试 A2：「`null`（整列缺席）与 **没有行**（`[]`）
        #     必须分得开」—— 第一版只手写了它用得到的列 ⇒ 它在池里成了唯一的 `null` ⇒ 6 红。
        #     ⚠️ 这里**调用主循环用的同一批 helper**（⛔ 不要另写一份取值逻辑：两处写同一条规则）。
        #     没有行的落 `[]`；真值里**有行**的照落真值（本卡 `abilityTriggers/abilityTargets/
        #     abilityTargetCriteria/abilityLevelCriteria` 就有行 —— 见 §十 的 diff）。
        # ==========================================================================================
        # ---- `A1799` 那 10 个 SO 字段列（缺值 `-1` / `""`，与主循环同一循环）----
        for _col, _tf, _dflt, _doc in A1799_FIELDS:
            entry[_col] = truth_ref_name(r, _tf) if _dflt == "" else truth_int(r, _tf, _dflt)
        # ---- 🆕 2026-10-25：`alternateArts`（与主循环同一 helper；本卡没有异画 ⇒ `[]`，⛔ 不是 `null`）----
        entry["alternateArts"] = alt_ids_of(r)
        # ---- `choicesDefinition` 三列（本卡没有 ⇒ `-1 / -1 / []`，与主循环同一段）----
        _cd = (r.get("choicesDefinition") or {})
        _cd_ta, _cd_mt = _cd.get("targetsAffected"), _cd.get("maxTargets")
        entry["choicesTargetsAffected"] = _cd_ta if isinstance(_cd_ta, int) and not isinstance(_cd_ta, bool) else -1
        entry["choicesMaxTargets"] = _cd_mt if isinstance(_cd_mt, int) and not isinstance(_cd_mt, bool) else -1
        entry["choicesSpecificCardIds"] = [truth_by_path.get((_ref or {}).get("m_PathID"))
                                           for _ref in (_cd.get("filterSpecificCards") or [])
                                           if truth_by_path.get((_ref or {}).get("m_PathID"))]
        # ---- `animTimes`（主循环**每张卡都落**；本卡真值里有 3 行动画 ⇒ 落真值）----
        entry["animTimes"] = anim_times_of(r, anim_idx, anim_cache, anim_trig_names, anim_report)
        # ---- 9 个「每张卡都落」的结构化列（`None` 只会在「真值里没有这张卡」时出现）----
        _atg = ability_triggers_of(uid, name, r, ABILITY_TRIGGER_REPORT)
        if _atg is not None: entry["abilityTriggers"] = _atg
        _atgt = ability_targets_of(uid, name, r, ABILITY_TARGETS_REPORT)
        if _atgt is not None: entry["abilityTargets"] = _atgt
        _amul = ability_multipliers_of(uid, name, r, ABILITY_MULTIPLIERS_REPORT)
        if _amul is not None: entry["abilityMultipliers"] = _amul
        _aal = active_ability_logic_of(uid, name, r, ACTIVE_ABILITY_LOGIC_REPORT)
        if _aal is not None: entry["activeAbilityLogic"] = _aal
        _aat = active_ability_targets_of(uid, name, r, truth_by_path or {},
                                         ACTIVE_ABILITY_TARGETS_REPORT)
        if _aat is not None: entry["activeAbilityTargets"] = _aat
        _atc = ability_target_criteria_of(uid, name, r, truth_by_path or {},
                                          ABILITY_TARGET_CRITERIA_REPORT)
        if _atc is not None: entry["abilityTargetCriteria"] = _atc
        _alc = ability_level_criteria_of(uid, name, r, truth_by_path or {},
                                         ABILITY_LEVEL_CRITERIA_REPORT)
        if _alc is not None: entry["abilityLevelCriteria"] = _alc
        _aac = ability_acting_criteria_of(uid, name, r, truth_by_path or {},
                                          ABILITY_ACTING_CRITERIA_REPORT)
        if _aac is not None: entry["abilityActingCriteria"] = _aac
        _aapc = active_ability_pick_criteria_of(uid, name, r, truth_by_path or {},
                                                ACTIVE_ABILITY_PICK_CRITERIA_REPORT)
        if _aapc is not None: entry["activeAbilityPickCriteria"] = _aapc
        # ---- 主动能力闸六列（只落**非默认**键；本卡全 0 ⇒ 一条都不落 = 与主循环同形）----
        for _gk, _gv in ability_gate_of(uid, name, r, truth_by_path or {}, ABILITY_GATE_REPORT).items():
            entry[_gk] = _gv
        entries.append(entry)
        report["pooled"].append((uid, name, faction, rarity, inv, show,
                                 len(owners_pool), want[uid]["name"]))
    return entries, report


def build():
    with open(SRC, encoding="utf-8") as f:
        raw = json.load(f)["cards"]

    rarity_lookup = make_rarity_lookup(load_rarity())
    zh = load_zh()
    # 🆕 2026-10-14（`A1801`）：**官方中文卡名**（`OFFICIAL_LOC_SRC`）—— 它在下面**盖掉自译**。
    off_id, off_en, off_bad, off_stats = load_official_cardname()
    # 🆕 2026-10-15（P3 相 · `A1763` 数据层）：**官方中文卡面描述**（`Card_Description/*`）——
    #    它灌进 `descZh`，插在 `zh_cards.json`（自译）之后、`cardface_fixes.descZh`（人工）**之前**。
    #    为什么要它：`nameZh` 2026-10-14（`A1801`）已经换成官方了，而 `descZh` 还是我们自译
    #    ⇒ **同一张卡现在中英自相矛盾**（官方中文卡名 + 自译中文效果）⇒ 本笔把 `descZh` 也换成官方的。
    #    ⚠️ 口径（已裁，`资料/任务_卡组卡牌战斗.md:289`）：**只换 `descZh`、`desc` 不动**
    #    （`desc` 是引擎唯一的解析输入；`descZh` 全仓无一处进解析器）。
    off_desc_by_id, off_desc_bad, off_desc_stats = load_official_desc()
    off_desc_hit = []     # (卡id, 英文名, 换前, 换后, 命中路) —— 真换掉的
    off_desc_same = []    # 官方值 == 我们自译（不必动）
    off_desc_miss = []    # 两级都查不到（含「原表里 en == zh」那 181 条）→ 保持自译，**要出声**
    # 🔴 `descZh` 三个写手的**顺序断言**的违例（人工值必须最终生效）——
    #    非空 ⇒ 当场炸、一个字节都不写（与 `_bad` 同一条口径）。
    desc_zh_order_violations = []
    man_desc_total = 0    # 人工 `descZh` 里**真匹配到卡**的条数
    man_desc_ok = 0       # 其中**最终生效**的条数（== total 才算层序对）
    off_hit = []          # (卡id, 英文名, 换前, 换后, 命中路) —— 真改掉的那些
    off_same = []         # 官方值 == 我们自译（不需要动）
    off_miss = []         # 两级都没查到 → 保持自译（**要出声**，见跑完的摘要）
    off_conflict = []     # 同名卡的多条候选值不一致（要出声）
    # 🔴 `A2050` / `N5`（2026-10-18）：**英文名桥**的每一次命中（`official_name()` 的第 ② 级）——
    #    现读 0 次；非 0 ⇒ 跑完**逐张打出来**（⛔ 别让它静默换掉一张卡的中文名）。
    off_en_bridge = []    # (我们 id, 卡名, 官方表命中的键)
    face_fixes, face_fixes_by_fac = load_cardface_fixes()   # 卡面逐张核对修正（2026-09-13），见那张表的长注释
    # ⚠️ 两张表：裸卡名 / 带阵营 —— **同名卡必须用后者**，见 `load_cardface_fixes` 的注释
    face_fixed = []                         # 被修正过的卡（跑完打出来给人看）
    used_face_fix = set()                   # 修正表里**真匹配到卡**的键 —— 抓「改名后失配」，见 `build()` 末尾
    used_stat_fix = set()                   # `STAT_FIXES` 里**真匹配到卡**的键（键是**改名前的卡名**）
                                            # —— 抓「改名后静默失配」，见 `build()` 末尾那条守卫
    # 🆕 2026-10-12（`A1620`）：**同一张卡的「裸卡名」键与「阵营/卡名」键，对同一列给出了不同的值**
    #    —— 合并时阵营键赢、裸键那一份**被静默丢掉**。见下面 `_ff_bare` / `_ff_fac` 那一支的长注释。
    key_conflicts = []
    id_by_name = load_ids()                 # 原版 id（见 `IDS_SRC` 那段长注释）
    raw_def = load_warlord_defence()        # 督军后手那张防御卡（原版真值，见 `DEFENCE_SRC` 那段）
    raw_inv = load_inventory_options()      # 卡能不能进卡组（原版真值，见 `INV_SRC` 那段）
    raw_init = load_initiative()            # 先后手用的先攻值（原版真值，见 `INIT_SRC` 那段）
    # 🆕 2026-10-15（P2 相 `A1773`/`A1776`/`A1799`）：**真值卡 SO 快照本体**（见 `TRUTH_CARDS_SRC` 那段）。
    # 键只认 `uniqueId` —— 与我们卡池 id 是**同一套**（P1 相换过一次），所以 `truth_cards.get(cid)`
    # 就是那份**已裁过的 join**，本相不另开第二条。
    truth_cards, truth_by_name = load_truth_cards()
    # 🆕 2026-10-19（`E12`）：逐候选动画时间的索引 / 缓存 / 枚举名（判据见 `ANIM_GUID_MAP_TSV` 上面那段）
    anim_report = {"unresolved_guid": 0, "not_cardanim": 0, "missing_asset": 0,
                   "no_prefab": 0, "missing_map": 0, "rows": 0, "cards": 0}
    anim_idx = load_anim_guid_index(anim_report)
    anim_cache = {}
    anim_trig_names = load_ability_trigger_names()
    # 🆕 2026-10-19（`E12` · `A2142`）：天赋名 → id 表的跑完读数
    talent_name_report = {"keys": 0, "conflicts": 0, "conflict_rows": []}
    # 🆕 2026-10-21（引擎线 `T03·c` 批 B）：`triggerPriority` 的跑完读数 ——
    #   `cards`/`rows` = 落值的卡数/条数；其余五个口都是「**没落成**」的原因，**一条都不许吞**
    #   （`no_truth` = 真值快照里没有这张卡 ⇒ 不知道它有没有非默认 priority；`bad` = 非法值；
    #    `empty_logic` = 有 priority 但 abilityLogic 为空（GOF97 那一类）；
    #    `unmapped` = 触发时机在我们引擎里**没有关键词**（见 `ABILITY_TRIGGER_KEYWORD` 上面那段）；
    #    `conflict` = 同卡同关键词两个不同 priority，表达不出）。
    trigger_priority_report = {"cards": 0, "rows": 0, "no_truth": 0,
                               "bad": [], "empty_logic": [], "unmapped": [], "conflict": []}
    # 🔴🆕 2026-10-23（引擎线 P1·①）：`abilityTriggerPriority` 的跑完读数 ——
    #   与 `trigger_priority_report` 同形（`unmapped` 换成 `no_name`：本列按**枚举名**落，
    #   「引擎没有这个关键词」不再是缺口，缺的只剩「枚举名查不出来」）。
    ability_trigger_priority_report = {"cards": 0, "rows": 0, "no_truth": 0,
                                       "bad": [], "empty_logic": [], "no_name": [], "conflict": []}
    # 🆕 2026-10-22（引擎线 `W63`）：`abilityContext` 的跑完读数 —— 与 `trigger_priority_report` 同形：
    #   `cards`/`rows` = 落值的卡数/条数；`no_truth` = 真值快照里没有这张卡；`bad` = 非法/缺键；
    #   `no_name` = `AbilityTrigger` 枚举名查不出来（枚举桩缺失 ⇒ 不落）；`dup_same` = 同
    #   `(trigger, context)` 合并掉的重复行；`conflict` = 同 trigger 不同 context（**两行都落**、出声备查）；
    #   后两个是分布读数（「非 0 条数」与「按 trigger 分布」的唯一凭据）。
    ability_context_report = {"cards": 0, "rows": 0, "no_truth": 0, "bad": [], "no_name": [],
                              "dup_same": [], "conflict": [], "by_trigger": {}, "by_context": {}}
    # 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`unitConditions` 的跑完读数 ——
    #   `cards`/`rows` = 落值的卡数/条数；`no_truth` = 真值快照里没有这张卡（≠「没有条件」）；
    #   `bad` = 值不在 `UnitConditions` 枚举里 / `excludeCondition` 非 0-1 / 非整数（**丢弃该条**）；
    #   `by_cond` / `by_role` = 分布读数（审计那条「28 张非 0」的唯一凭据）。
    unit_conditions_report = {"cards": 0, "rows": 0, "no_truth": 0, "bad": [],
                              "by_cond": {}, "by_role": {},
                              # 🆕 v16（`B-16` 缺口）：`count` 行的「被数集合」
                              "set_rows": 0, "set_bad": [], "set_unresolved": [],
                              # 🆕 v22（2026-10-24 · `新账-95` #2）：`mult`/`multi`/`avail`/`choice`
                              #   四格各自落了整条 criteria 的**行数**（分布读数；键 = role）。
                              "role_set": {}}
    # 🆕 2026-10-22：`UnitConditions` 枚举名（`name` 字段的唯一来源；读不到就一条都不落）
    unit_cond_names = load_unit_conditions_names()
    if not unit_cond_names:
        print(f"⚠️ 读不到 `UnitConditions` 枚举桩 {UNIT_CONDITIONS_STUB} ⇒ `unitConditions` 一列"
              f"**整列不落**（⛔ 不猜名字；数值 `cond` 也得靠这张表校验）")
    # 🆕 2026-10-21（引擎线 `T03·b`）：开局加成三列 / `skipCardDraw` 生产列的跑完读数。
    initial_modifier_report = {c: {"cards": 0} for c in INITIAL_MODIFIER_COLS}
    skip_draw_report = {"cards": 0, "rows": 0, "no_truth": 0, "unmapped": [], "bad_uses": []}
    if not anim_trig_names:
        print(f"⚠️ 读不到 `AbilityTrigger` 枚举桩 {ABILITY_TRIGGER_STUB} ⇒ `animTimes[].tn` 将全空"
              f"（数值 `tg` 照落；⛔ 不猜名字）")
    # 🔴 2026-10-19（`A1960`）：`choicesDefinition.filterSpecificCards` 的 **PPtr 解引用表**。
    #   `m_PathID` → 真值卡的 `uniqueId`（= 我们池的 id）。现核 178 条引用**全部**能解（0 未解）。
    #   ⚠️ 解出来的 id **未必在我们卡池里**（实测：`Adaptive Strategy` 的选项指向 `_normal`/`_oath`
    #   两张**变体卡 SO**，它们不在卡池）—— 本层**照实落值**；「这一项能不能用」由引擎侧判，
    #   ⛔ 别在这里替它丢掉（丢掉之后下游不知道原版给了什么，也就无从如实报）。
    truth_by_path = {r.get("path_id"): uid for uid, r in truth_cards.items() if r.get("path_id") is not None}
    # `A1959`/`A1960` 的跑完读数（见主循环末尾与 `main()` 的打印）
    choices_report = {"with_choice": 0, "pool_180": 0, "pool_180_ids": [],
                      "pool_180_no_filter": [], "unresolved_refs": [], "not_in_pool_refs": 0}
    # 🔴 2026-10-18（`W25`）：**按 `uniqueId` 直取**的落位表（见 `load_direct_ids` 那段）。
    #    ⚠️ 它必须在主循环**之前**建好 —— 主循环里 `cid` 的第一来源就是它。
    id_direct_tbl, direct_ambiguous = load_direct_ids(truth_cards, load_id_mapping())
    truth_hit = 0                           # 能在快照里按 id 找到的卡（跑完打出来 —— 它是各列的覆盖率分母）
    kw_truth_filled = []                    # (卡id, 追加了哪些 trait) —— **真值铺底**那一层的读数
    kw_truth_skipped = []                   # (卡id, 卡名, 跳过了哪些) —— 引擎 Prefixes 不认的那三个 trait
    kw_truth_wiped = []                     # (卡id, 卡名, 被更高层换掉的) —— 规则内、但**要看得见**
    # 🔴 `keywords` 权威序（已裁③）的**三条断言**的违例（见 `merge_truth_traits`）——
    #    非空 ⇒ **当场炸、一个字节都不写**（与 `_bad` 同一条口径：结构错不许静默写出去）。
    kw_order_violations = []
    armour_src_pnp = []                     # `armour > 0` 且值来自 **PnP**（我们的 keywords/desc）
    armour_src_truth = []                   # `armour > 0` 且值来自 **真值兜底**（PnP 那一层没读到）
    hero_cost_from_truth = []               # 督军 `cost` 被真值改掉的（`A1773` + 已裁④）
    hero_cost_kept = []                     # 督军 `cost` 没真值可依、保持现状的（要出声）
    # 🆕 2026-10-18（`W` 笔）：**SO 数值真值覆盖层**的两个记账口（见下面那段长注释）
    so_stat_changed = []                    # (cid, 卡名, 类型, 字段, 改前, 改后) —— 本层真改掉的每一处
    so_stat_skipped = []                    # (cid, 卡名, 类型, 字段, 现值, SO值, SO卡名) —— 有 SO 值但**没敢覆盖**的
    # 🔴 2026-10-19：`manaCost = -1`（原版「不可直接打出」哨兵）⇒ `cost = -1` 的那一档。
    #    (cid, 卡名, 类型, 改前值, SO 卡名) —— 4 张 `Dark Pact of *`（附魔模板）现读全在这里。
    unplayable_cost_set = []
    discarded_hasstats = []                 # 被 `hasStats=false` 整批丢的那批 → 跑完按真值归类
    # 🆕 2026-10-18（`W25`）：id 层的三个新记账口（见主循环「稳定 id」那段）。
    id_direct = []                          # 直取命中（`uniqueId` 表唯一落位）—— 现读 = 全池
    id_cascade = []                         # 直取没落位、退回**名级联**的（要出声；现读 0）
    id_cross_mismatch = []                  # (直取, 级联, 级联来源, 卡名, 阵营) —— 交叉校验不一致（要出声）
    id_orig, id_made = [], []               # 名级联/card_ids.json 命中的 / 自造 id 的（跑完打出来给人看）
    # 🆕 2026-10-14（P1 相 · `A1782`/`A1800`）：走【真值索引】那一层拿到 id 的（见 `TRUTH_IDX_SRC` 那段）。
    # 单列一份只为把「这一层救回多少张」打在摘要里 —— 它是 id 置换那批账的验收读数。
    id_truth = []
    truth_idx = load_truth_index()
    inv_unknown = []                        # `inventoryOptions` 真值查不到的卡（`A1798`，跑完打出来）
    init_unknown = []                       # `initiative` 真值查不到的卡（`A1775`，跑完打出来）
    cards, skipped = [], []
    _seen_ids = set()                       # 生成的 id 必须两两不同（下面有断言）
    _seen_names = set()   # (阵营, 归一化卡名) —— 判重只在这个粒度上做
    filled, still_missing = [], []
    # 🔴 **稀有度守卫的记账口**（`A2044`，2026-10-15 `G3`）—— 「最终落成空串」的卡逐张登记
    #    `(阵营, 卡名, ocrName)`，跑完在 `build()` 末尾由那两条谓词消费（见 `rarity_guard_bad` 那段）。
    #    ⚠️ 记账口只能在这儿：`cards` 里的条目**不保留 `ocrName`**，出了这个循环就查不回来了。
    _rarity_empty = []
    # 🔴 **`A2062`（2026-10-16）：稀有度那一列的【来源台账】**—— 第 1 层是**真值 `cardRarity`**
    #    （见模块级 `TRUTH_RARITY` 那段），够不到的卡才走宝石表 / 两张补表。
    #    这四个口子只为**看得见**：跑完逐条打出来（`main()` 的稀有度段）。
    rarity_truth_filled = []    # (id, 卡名, 换前, 换后) —— 真值**改掉**了宝石表/源表的值（现读 0 处）
    rarity_src_truth = []       # (id, 卡名) —— 值来自第 1 层（真值 `cardRarity`）
    rarity_src_table = []       # (id, 卡名, 命中表) —— 值来自宝石表 / `RARITY_BY_FACTION` / `RARITY_UNLISTED`
    rarity_src_source = []      # 🔴 (id, 卡名, 值) —— **既没真值、也没表命中 ⇒ 直接吃了源表原值**
                                #    （`card_stats.json` 的 `rarity` 列）—— **只出声、不进 `_bad`**，
                                #    但它是「这一列还有多少是 OCR 说了算」的读数，⛔ 别静默（见 `main()`）
    stat_fixed = []       # 数值被 `STAT_FIXES` 纠正过的卡（见那张表）
    for c in raw:
        ctype = norm_str(c.get("type"))
        # ⚠️ **`noise=true` 的整条丢掉**（2026-09-12 加）：那批是**根本不是卡**的索引噪音 ——
        #    `Normal Conditions` / `Normal` / `Default Conditions` / `v2` 这类占位条目（无卡面、
        #    无数值、任何卡组都不引用），加上两张重复卡（`HB` = Imotekh 的异画版、
        #    `Threnodic Choir Flawless` = 与另一张同图）。原来它们**混在卡池里**，
        #    卡组编辑器和自动凑牌都可能抽到「一张没有卡面的假卡」。
        #    判定和理由都在源表里（`noise_reason`），这里只负责过滤。
        if c.get("noise"):
            skipped.append((norm_str(c.get("name")), "噪音卡(" + norm_str(c.get("noise_reason"))[:40] + ")"))
            continue
        if not c.get("hasStats") and norm_str(c.get("name")).strip() not in STATS_EXCEPTIONS:
            skipped.append((norm_str(c.get("name")), "无数值(hasStats=false)"))
            # 🆕 2026-10-15（`A1773`）：这批**不再只是一句「丢掉了」** —— 记下来，
            #    跑完用真值逐张归类（判别器 = `spellType` + `inventoryOptions`/`cantBeRandomlyCreated`，
            #    见 `classify_discarded`）。目的是把「真卡被误丢」这件事变成**可复核的读数**。
            discarded_hasstats.append((norm_str(c.get("name")), norm_str(c.get("faction")),
                                       norm_str(c.get("type")), norm_str(c.get("ocrName"))))
            continue
        if ctype not in TYPES:
            skipped.append((norm_str(c.get("name")), "未知类型 " + ctype))
            continue
        name = norm_str(c.get("name")).strip()
        name = re.sub(r"\s+", " ", name)          # 空白归一：源表里有 ` Iron Priest`（前导空格）这种
        name_before_rename = name                  # `STAT_FIXES` 的键用的是**改名前的名字**（见下）
        # ⚠️ **同阵营同名的丢掉后一条**（2026-09-12 加）：源表里有一对
        #    ` Iron Priest` / `Iron Priest`（前导空格造成的重复），两条数值一模一样。
        #    按 **(阵营, 归一化名字)** 判重 —— 不能只按名字，原版本来就有跨阵营同名卡
        #    （Aggressor / Terminator 那几个），那些是**不同的卡**，不能误删。
        if (norm_str(c.get("faction")), name) in _seen_names:
            skipped.append((name, "同阵营重名（前一条已收）"))
            continue
        _seen_names.add((norm_str(c.get("faction")), name))
        # 白名单里的卡：源表没数值，用补的那份盖掉（见 `STATS_EXCEPTIONS` 的收录依据）
        _exc = STATS_EXCEPTIONS.get(name) or STATS_EXCEPTIONS.get(name.strip())
        if _exc:
            c = dict(c)
            c.update(_exc)
        # 🆕 2026-09-13：`STAT_FIXES` 里可以带 `"name"` —— **旧表把卡名抄错**时用。
        # ⚠️ 必须在这里（`_seen_names` 判重**之后**、下面各处引用 `name` **之前**）——
        #    放早了判重会用旧名，放晚了 entry/中文表/立绘都还拿着旧名找。
        #    ⚠️ **判重那一步用的是旧名**：所以如果新旧名在源表里同时存在，
        #       两张都会被收 —— 这个风险由 `STAT_FIXES` 的注释负责说明（不收），代码不替它判。
        _nf = STAT_FIXES.get(name)
        if _nf and "name" in _nf:
            name = _nf["name"]
        rarity = norm_str(c.get("rarity"))
        # ⚠️ 2026-09-12 更正：这里原来写的是 `if rarity not in RARITIES:` —— 只把宝石表当**补丁**用
        #    （仅在原值是空串 / `defence` 这类**非法值**时才查），于是 OCR 写错但**看起来合法**的值
        #    被原样放行。实测：**151 张与宝石表冲突，其中 140 张是我们写成 `legendary`、
        #    宝石实测是 common/rare/epic/special**（Autarch / Night Spinner / Shining Spear / Ursula Creed…）。
        #    拿卡面核过实：Night Spinner 与 Shining Spear 卡面底部那颗菱形宝石都是**浅蓝 = common**，
        #    OCR 记的 `legendary` 是错的 —— 那句「宝石表是唯一的权威来源」原来只写在文档字符串里，
        #    代码没照做。**现在只要宝石表里有这张卡就用它**。
        #    影响面：稀有度 → 卡框取 tier1–4（`CardArt.TierOf`）+ 底部宝石颜色，这 151 张一直挂错档。
        got, how = rarity_lookup(name)
        # 重名卡（同名不同阵营 / 同名但写法几乎一样）优先按「阵营 + 卡名」特判 ——
        # 宝石表是按卡名查的，重名时后写的赢，光靠 normalize 分不开。见 `RARITY_BY_FACTION`。
        forced = RARITY_BY_FACTION.get((norm_str(c.get("faction")), name))
        if forced:
            got, how = forced, "重名按阵营定（卡面实测）"
        elif got is None and name in RARITY_UNLISTED:
            # 三份 0824 表没收录的 9 张（见 `RARITY_UNLISTED`）—— 宝石表里查不到，用卡面实测值
            got, how = RARITY_UNLISTED[name], "表外卡（卡面实测）"
        if got:
            if got != rarity:
                filled.append((name, rarity, got, how))
            rarity = got
            _rar_from = ("table", how)
        elif rarity not in RARITIES:
            # 宝石表里也没有这张（卡名对不上）—— 原值本身也不合法，才算缺失
            still_missing.append(name)
            _rarity_empty.append((norm_str(c.get("faction")), name, norm_str(c.get("ocrName"))))
            rarity = ""
            _rar_from = ("empty", None)
        else:
            # 🔴 **原值「看起来合法」、于是被原样放行** —— 这就是 `A2062` 说的那个口子：
            #    源表 `card_stats.json` 的 `rarity` 列有 239 空 + 102 个 `defence`（类型串进来了）
            #    + **140 个把非传说写成 `legendary`**；而「看起来合法」的那几个（现读 13 张）
            #    **过去是【一个声音都没有】地进了卡表**。⇒ 记下来、跑完打出来（`main()` 里那一段）。
            _rar_from = ("source", None)
        cost = norm_int(c.get("cost"))
        attack = norm_int(c.get("attack"))
        health = norm_int(c.get("health"))
        ranged = norm_int(c.get("ranged_attack"))
        # 数值修正（OCR 读错/漏读的那几张，见 `STAT_FIXES`）—— **每条都对着卡面核过**
        # ⚠️ **必须用「改名前」的名字查**：`STAT_FIXES` 的键就是旧名（改名那一条的 key 也是旧名）——
        #    查 `name` 的话改过名的卡会**查不到自己的数值修正**（**静默**：名字对了、数没改）。
        fix = STAT_FIXES.get(name_before_rename)
        if fix:
            # 🔴 **`A2067`（2026-10-19）：连【改名前】的键一起记** —— `STAT_FIXES` 里
            #    `{"name": …}` 那种是**改名**、不是数值修正；读数要把两栏分开算
            #    （只带 `name` 的进「改名」、带 cost/attack/health/ranged 的进「数值修正」）。
            stat_fixed.append((name, dict(fix), name_before_rename))
            used_stat_fix.add(name_before_rename)   # ← 孤立键守卫的记账口，见 `build()` 末尾
            if "cost" in fix: cost = fix["cost"]
            if "attack" in fix: attack = fix["attack"]
            if "health" in fix: health = fix["health"]
            if "ranged" in fix: ranged = fix["ranged"]

        # ---- 稳定 id（2026-09-13 第三十三轮 · 🔴 2026-10-18 `W25` 改成**按 uniqueId 直取**）----
        # **① 直取**（`load_direct_ids` 那段）：`(阵营码, 归一化卡名) → uniqueId` **唯一落位**表。
        #    这是**唯一的身份判据** —— 落位唯一才认，⛔ 不猜。
        # **② 名级联只作交叉校验**（`pick_truth_id` 真值索引 → `pick_id` 老路）：
        #    它与直取**不一致就必须出声**（`id_cross_mismatch`，跑完在摘要里逐条打出来）。
        #    ⚠️ 为什么会不一致：级联是启发式（官方英文名 → `m_Name` 尾巴 → `cardName`，再按
        #    `base`/短 id 挑）—— 次序一变就可能挑到**另一张卡**的 id，这正是本笔要根除的那类静默。
        # **③ 直取落不了位**（快照之后的新卡 / 同一个名字在同阵营有多条且没有唯一基准卡）⇒ 退回名级联，
        #    记进 `id_cascade` **并出声**；级联也挑不出才**自造**（`id_made`，由跑完的报告列出）。
        #    🔴 现读 = 直取 1127/1127、级联兜底 0、自造 0（`build()` 末尾有硬断言，退回自造直接非零退出）。
        faction = norm_str(c.get("faction"))
        faction_id = norm_int(c.get("factionId"))
        ocr_name = norm_str(c.get("ocrName"))
        cid = pick_direct_id(id_direct_tbl, faction_id, name, ocr_name)
        # 名级联（老路）—— 现在只为**交叉校验**跑；直取没落位时才真的用它。
        cid_cascade, cascade_soft = pick_truth_id(truth_idx, faction, faction_id, name, ocr_name)
        cascade_how = "真值索引"
        _cascade_truth = True          # 这一张的兜底 id 是「真值索引」给的（不是 card_ids.json）
        if not cid_cascade:
            cid_cascade = pick_id(id_by_name.get(_norm_name(name), []), faction)
            cascade_how = "card_ids.json"
            _cascade_truth = False
        elif cascade_soft:
            # 🔴 `A1880`：只有 `by_name` 有候选 ⇒ 这一条 id 是**软匹配**来的 —— 摘要里逐条可见
            cascade_how = "真值索引(by_name 软匹配)"
        if cid:
            id_direct.append((cid, name, faction))
            if cid_cascade and cid_cascade != cid:
                id_cross_mismatch.append((cid, cid_cascade, cascade_how, name, faction))
        elif cid_cascade:
            cid = cid_cascade
            id_cascade.append((cid, name, faction, cascade_how))
            if _cascade_truth:
                id_truth.append((cid, name, faction))
            else:
                id_orig.append((cid, name, faction))
        else:
            cid = make_id(faction, name)
            id_made.append((cid, name, faction))
        if cid in _seen_ids:
            # id 撞了 = 稳定 id 这件事本身就塌了，**当场炸**，别生成一份带重复 id 的卡表
            sys.exit(f"❌ id 撞车：{cid}（{name} / {faction}）—— 自造规则要改，不能把重复 id 写进卡表")
        _seen_ids.add(cid)

        # ---- 🆕 2026-10-15（P2 相）：本卡对应的**真值行**（`TRUTH_CARDS_SRC` 那段）--------------
        # 取法 = **按 id 直查**（我们卡池 id 与真值 `uniqueId` 是同一套，P1 相换过）。
        # 查不到 ⇒ `trow = None` ⇒ 所有真值列走**缺值**（`-1`/`""`），**不许拿 0 顶**。
        trow = truth_cards.get(cid)
        if trow is not None:
            truth_hit += 1

        # ---- 🔴🔴 `A2062`（2026-10-16）：稀有度那一列的【第 1 层】= 真值 `cardRarity` -------------
        # **权威序（从高到低，本处收口）**：
        #   ① **真值 `cardRarity`**（`TRUTH_CARDS_SRC` 那份原版卡 SO 快照，**游戏自己的枚举**）—— 本处；
        #   ② 宝石表 `资料/卡牌数据表/卡牌宝石稀有度_0824.md`（`rarity_lookup`，**人眼看印刷品记的色名**）—— 上面那一支；
        #   ③ `RARITY_BY_FACTION`（`(阵营, 卡名)` 强制覆盖）—— 上面那一支；
        #   ④ `RARITY_UNLISTED`（宝石表里**没有**这张卡）—— 上面那一支；
        #   ⑤ `card_stats.json` 的 `rarity` 原值（**OCR 读出来的**）—— 上面那个 `else` 支（**要出声**）。
        # ⚠️ **为什么放在 `trow` 之后**（不是跟上面那一支并排）：真值行是按 **`cid`** 取的，而 `cid`
        #    是**稳定 id** 那一层算出来的 —— 它在稀有度那一支**之后**。⇒ 本块**改的是局部变量 `rarity`**，
        #    `entry` 要到下面才拼（见 `entry = {…}`），**顺序上是安全的**。
        # ⚠️ **它是「值都不会变」的那一档**（现读 1005/1005 与卡池一致，`rarity_truth_filled` 现读 0 处）——
        #    本笔买的是**根**：宝石表是按**PnP 卡面写法**建的键，我们池里那批「用美术/资产名」的卡
        #    （`Land Rider` / `Morkai Eliminator` / `2nd Company Terminator` / `Bladeguard Lieutenant` /
        #     `Simulacrum Imperialis` …）**按现名必然落空**，只能靠 ③ 逐条特判 —— 那一整类账
        #    （`A2061` / `A2064` / B20 四条 / `SOR24`）**从根上消掉**。
        # ⚠️ **真值够不到的那 122 张**（快照 2025-07-30 之后新增：SW 68 / UM 38 / DA 12 等）
        #    **照旧走 ②③④** —— 它们的值**一个字都不动**（`rarity_src_table` 就是那批的读数）。
        _rtv = trow.get("cardRarity") if trow is not None else None
        _rtru = TRUTH_RARITY.get(_rtv) if isinstance(_rtv, int) and not isinstance(_rtv, bool) else None
        if _rtru is not None:
            if _rtru != rarity:
                rarity_truth_filled.append((cid, name, rarity, _rtru))
            rarity = _rtru
            rarity_src_truth.append((cid, name))
        else:
            # 真值**没有**这张卡 / `cardRarity` 是 `0`（哨兵）⇒ 上面那条链说了算，**逐张登记来源**
            if _rar_from[0] == "table":
                rarity_src_table.append((cid, name, _rar_from[1]))
            elif _rar_from[0] == "source":
                rarity_src_source.append((cid, name, rarity))
            # "empty" 那一档已经记进 `_rarity_empty` 了（守卫 ① 消费它）

        # ---- 🆕 `A1773`：督军 `cost` —— **PnP 不印，所以照真值**（已裁④ + 已裁⑥）--------------
        # 🔬 **PnP 在这一点上是空的、不是矛盾的**：2026-10-15 亲开 `Anvirr Keltoc` 的成品图
        #    （`Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png`）—— 右上角是**阵营徽记**
        #    （深绿圆盘里的 Saim-Hann 蛇纹），**根本没有费用六边形**。当年 OCR 把它读成了 `5`，
        #    `施工设计 §七 已裁⑥` 那句「`Anvirr Keltoc` 却印 5」就是**照这处 OCR 误读写的**（已订正）。
        #    ⇒ 与 `STAT_FIXES` 里 2026-09-14 那条「56/56 无蓝色费用六边形」**一致**。
        # 判据（`项目任务.md:498-516` 的对抗性验证）：`manaCost` 是**序列化字段**（整条继承链
        #    四个环节没有一个把它读成 0/忽略）、`CardScript__UpdateCostText` 对**任何**卡无条件
        #    `SetText(cost)=CurrentCost`、原版只是**显示侧**在 `IsWarlordEquivalent` 时把它藏起来
        #    （`BasicCardUI__SetRawCardData.c:51,81,84` 的 `Toggle(costCtrl,false)`）。
        # ⇒ **`cost=2` 是真值，`0` 是我们当年为了「卡面不显示」而选的表达** —— 而显示侧已经由
        #    `A2006①` 接掉了（`CardView.ShowsCost` = `cost >= 0 && type != "hero"`），
        #    ⇒ 现在改数据**不会**让督军在卡面/卡组行印出费用。
        if ctype == "hero":
            _mc = truth_int(trow, "manaCost") if trow is not None else -1
            if _mc >= 0:
                if _mc != cost:
                    hero_cost_from_truth.append((cid, name, cost, _mc))
                    cost = _mc
            else:
                hero_cost_kept.append((cid, name, faction, cost))

        # ---- 🆕 2026-10-18（`W` 笔）：**SO 数值真值覆盖层**（`health` / `cost` / `attack`）---------
        # 🔴 判据 → `资料/普查产出_1018/R_督军10血成因.md` §1 **结论 A：当前 build 的 SO 就是真值**。
        #    2026 build 把督军生命统一下调（35→25 ×39 · 40→30 ×11 · 30→20 · 35→30），
        #    `allcards_1350/报告.md:101` 原话「**这不是我们抄错，是版本变化**」；
        #    运行时 `warlordLifeChange` 是**每局/每 event 的同一全局增量**（经典 0 / 冲突 −10），
        #    对 56 张督军同加同减，**产生不出** −5（AM3）/ −15（TL35）/ 0（9 张）这种逐卡差异
        #    （反编译证据 → 报告 §2：`BattleManager__SetupWarlordsLifeAndEnchantment.c:29,53,72`）。
        # ⇒ 本层把**这三列**换成 SO 的对应字段（⛔ `ranged` / `desc` / `keywords` / `art` 一律不动 —— 刀口另有人管）。
        #    字段口径（**2026-10-18 现核，不是猜的**）：
        #      · `health` ← `maxHealth`（1127 张里 1078 张本来就相等；差 49 项 = 47 督军 + BL71/SW13）
        #      · `attack` ← `meleeAttack` —— **现表 `attack` 就是近战攻击**：1126/1127 逐张相等，
        #        唯一例外 `TAU12 Darkstrider`（我们 3 / SO 2）**正是本层要改的那 1 项**；
        #        `rangeAttack` 对应现表 `ranged`，**本层不动它**。
        #      · `cost`   ← `manaCost`。**`>= 0` ⇒ 照抄**；**`== -1` ⇒ 落 `cost = -1`**（🔴 2026-10-19
        #        用户裁定，见下）。`-1` = 原版「不可直接打出」的哨兵（全快照 105 张；落进我们池的
        #        只有 4 张 = `Dark Pact of Fate/Blood/Excess/Resilience` —— 它们的 SO 行名是
        #        `Mark of Tzeentch/Khorne/Slaanesh/Nurgle (enchantment)`（`cardType=20` 附魔模板），
        #        `uniqueId` 与我们池里那 4 张**逐一对上**；生成链 = `Enchantments` SO 的 markOfChaos
        #        映射（`bundle_duplicateassetisolationso_assets_all/MonoBehaviour/Enchantments.json`）
        #        + 反编译 `CardScript__AddEffect.c:776-792` case 8 `Enchantments.GetMarkOfChaos`
        #        → `RawCardScript.TriggerEnchantment` —— **只作附魔挂上去，从不直接打出**。
        #        🔴 **2026-10-19 就地订正（铁律 5）**：本行原来写「现表没有『不可打出』这一档表达
        #        ⇒ 保持原值、逐条出声（`so_stat_skipped`）」—— **已作废**：用户当天裁定这 4 张
        #        **没有费用**，且显示侧 `CardView.ShowsCost` 早就是 `cost >= 0 && type != "hero"`
        #        （`A2006①`）⇒ `cost = -1` **天然不印费用格**（底板 + 数字两处都过那一条）。
        #        ⇒ 现在**落 `-1`**、记进 `unplayable_cost_set`（⛔ 不再进 `so_stat_skipped`）。
        #        引擎侧另有「不可直接打出」守卫（`RuleCore.CanPlayCard` / `EffectResolver.CanPlayTactic`
        #        开头那一句 `card.Cost < 0`）—— `CostOf` 会把负值 clamp 成 0，不挡就会被当成**免费**。
        #      · **督军 `cost` 跳过**：上面 `A1773` 那一段已经从同一个 `manaCost` 取过值了，
        #        本层再写一次只会让两个记账口重复计数。
        #    ⚠️ 本层**盖过 `STAT_FIXES`**（那些是 OCR 修正、曾按 PnP 核过）—— 这正是本轮裁定的方向：
        #       **当前 build 的 SO 优先于 PnP 印刷值**（报告 §1）。改前→改后逐条打在 `main()` 摘要里。
        if trow is not None:
            _so_h = truth_int(trow, "maxHealth")
            if _so_h < 0:
                so_stat_skipped.append((cid, name, ctype, "health", health, _so_h, norm_str(trow.get("cardName"))))
            elif _so_h != health:
                so_stat_changed.append((cid, name, ctype, "health", health, _so_h))
                health = _so_h
            _so_a = truth_int(trow, "meleeAttack")
            if _so_a < 0:
                so_stat_skipped.append((cid, name, ctype, "attack", attack, _so_a, norm_str(trow.get("cardName"))))
            elif _so_a != attack:
                so_stat_changed.append((cid, name, ctype, "attack", attack, _so_a))
                attack = _so_a
            if ctype != "hero":
                _so_c = truth_int(trow, "manaCost")
                # 🔴 2026-10-19：`-1` = 原版「不可直接打出」哨兵（**不是缺值**）。
                #   ⚠️ 判「字段真的存在且就是 -1」—— `truth_int` 缺值也回 -1，⛔ 不能靠它区分，
                #   否则快照里**没有 `manaCost` 的行**会被误当成「不可直接打出」。
                _so_c_raw = trow.get("manaCost")
                if isinstance(_so_c_raw, int) and not isinstance(_so_c_raw, bool) and _so_c_raw == -1:
                    # ⇒ 落 `cost = -1`；显示侧 `CardView.ShowsCost` 因此不画费用格。
                    if cost != -1:
                        unplayable_cost_set.append((cid, name, ctype, cost, norm_str(trow.get("cardName"))))
                        cost = -1
                elif _so_c < 0:
                    so_stat_skipped.append((cid, name, ctype, "cost", cost, _so_c, norm_str(trow.get("cardName"))))
                elif _so_c != cost:
                    so_stat_changed.append((cid, name, ctype, "cost", cost, _so_c))
                    cost = _so_c

        # ---- 🆕 `A1776`：`keywords` 那一列的落位（**权威序抄在这里，别再另写一份**）----------
        # 主对话已裁③（`施工设计 §七`）—— **从低到高，后面的赢**：
        #   ① 真值 `CardTraits`（**本处**）        —— 🔴 **只补空**：OCR 那格为空时才填，⛔ 不覆盖已有值
        #   ② OCR 源表（`card_stats.json`）        —— `_kw_ocr`，现状不动
        #   ③ `cardface_fixes.json` 的 `keywords`  —— 下面 `_ff` 那一段（阵营键 > 裸键）
        #   ④ `fix_own_armour` 正则追加            —— 下面 `fix_own_armour(entry)`
        #   ⑤ `_manual_keywords`（人工最终裁定）   —— 由 `gen_cardface_fixes.py` 落进第 ③ 层，**最高**
        # **为什么真值在最低**（三条，都已核）：① 它是 2025-07-30 的老快照，**SW/UM/DA 等后来加的
        #   内容根本不在里面** ⇒ 拿它当高权威会把新卡的 `keywords` 抹空；② 它自己覆盖率就不全
        #   （`CardTraits` 非空 650 · `ShowTraits` 非空只 32）；③ 本仓铁律 7 —— **卡面（PnP 成品图）
        #   的关键词图标才是「内容」层的尺子**，逐张看图抄的第 ③ 层本来就该压过老快照。
        # 配套断言在 `RuleEngineTest`（主对话侧）：①「真值填的值，在 OCR 也有的那张卡上被 OCR 覆盖」
        #   ②「OCR 为空的卡上保留真值」—— 本处的实现形状保证了这两条（逐 trait 只补空）。
        _kw_ocr = norm_list(c.get("keywords"))
        _kw_filled, _kw_added, _kw_skip, _kw_bad = merge_truth_traits(_kw_ocr, trow or {})
        kw_order_violations.extend((cid, name, m) for m in _kw_bad)
        if _kw_added:
            kw_truth_filled.append((cid, name, _kw_added))
        if _kw_skip:
            kw_truth_skipped.append((cid, name, _kw_skip))

        # ---- 🆕 `A1776`：`cardTraits` 列（真值 `CardTraits` 全量，显示写法）-----------------
        # 🔴 与 `keywords` **不是一回事**：这一列**不喂引擎**，是**纯落值**（判「原版挂了什么」用）。
        #    ⚠️ **2026-10-15（`AD` 笔）就地订正（铁律 5）**：本行原来写
        #      「⇒ 它可以带上引擎 `Prefixes` 不认的那三个（`Frenzied`/`Rage`/`Unstoppable`），
        #        而 `keywords` 里没有它们」—— **两句都不再成立**：引擎已登记那三个词
        #      （`AB_接线批.md` §三），`TRAIT_ENGINE_UNREGISTERED` 已清空 ⇒ **两列今天都带得上它们**。
        #    🔴 本列的**取值口径 = `TRAIT_DISPLAY` 的【值】**（与 `keywords` 追加的那条**同一个串**）——
        #      所以 `Frenzied`/`Rage` 现在**写的是卡面字** `Destroyer`/`Penitence`（官方显示名），
        #      ⛔ 不再是真值枚举名。**要判「真值枚举名是什么」看 `CardTraits[].name`，不是看本列。**
        # ⚠️ **读 `CardTraits`、⛔ 不读 `ShowTraits`** —— 理由见 `truth_traits_of` 的 docstring。
        _traits = truth_traits_of(trow) if trow is not None else []

        # ---- 🆕 `A1776`：`armour` 列 —— **护甲值与 trait 解耦** --------------------------
        # 值先取 **PnP 那一层**（`keywords` 里的 `Armour N`，来源 = OCR + `cardface_fixes` +
        # 下面 `fix_own_armour` 从 `desc` 正则补的）；PnP 那一层读不到才退**真值 `armour` 字段**。
        #   ① **P 优先**（已裁⑥）：实测 13/13 逐张开图 —— `Abominant`/`Hulking Aberrant`/
        #      `Rukkatrukk Squigbuggy` 三张 PnP 卡底**银盾都印着 `1`**，而快照 `armour = 0`。
        #   ② 图上有银盾、而 `desc` 又没以 `Armour N` 开头时，`fix_own_armour` 读不到 ⇒ 真值兜底。
        #   ⚠️ 真值 `armour` 只 **62** 张 >0（46×1 · 15×2 · 1×3），而 PnP 那一层读出 **65** 张
        #      ⇒ 实测两条**同一方向**（PnP ⊇ 真值，3 张差额就是上面那三张），所以兜底那支**今天一次都不触发** ——
        #      `armour_src_truth` 的条数会如实打成 **0**（不是没接，是没得兜）。
        _armour_pnp = 0
        for _k in _kw_filled:
            _m = re.search(r"armou?r\s*(\d+)", str(_k), re.I)
            if _m:
                _armour_pnp = int(_m.group(1))
                break

        entry = {
            "id":       cid,
            "name":     name,
            "type":     ctype,
            "cost":     cost,
            "attack":   attack,
            "health":   health,
            "ranged":   ranged,
            "keywords": _kw_filled,
            "desc":     norm_str(c.get("desc")),
            "faction":  norm_str(c.get("faction")),
            "rarity":   rarity,
            # ---- 兵种（Infantry / Vehicle / Drone / Beast / Elixir / Secret …）----
            # **原版数据里有、我们此前一直丢掉了**（2026-09-12 发现）。
            # 用处有两个，都是硬需求：
            #   ① 目标过滤：`a friendly Vehicle` 这类卡面词以前只能当「打得比卡面宽」报出来
            #      （12 张卡挂在这一栏），有了 subtype 就能真正筛
            #   ② 造牌候选池：`Create three Ultramarines Vehicles` / `Create a random Combat Elixir`
            #      —— 规则书附录 C 的骰子查找表就是按 subtype 分组的
            # 覆盖：1212 张里 1117 张有值（95 张没有，多半是 token / 未实装卡）
            "subtype":  norm_str(c.get("subtype")),
            # ---- 🆕 2026-10-15（`A1776`）：`armour` / `cardTraits` -------------------------
            # `armour` = **数值**（`Armour N` 关键词里的那个 N 的**一等公民**版本）——
            #   用途是「护甲值别再去 `keywords` 里正则捞」（`A1776` 的「与 trait 解耦」）。
            #   🔴 **`A2036`（2026-10-18 现读）**：原来这里写「消费者还没切：引擎今天仍读 `KwValue("armour")`
            #   ⇒ 本列现在不改变任何结算」—— **已过期**：`CardDef.cs:297-299` 已接线
            #   （`ArmourFromPool = armour > 0 ? armour : 0`，非 0 时写回 `_keywords[Armour]`）⇒ **本列非 0 就赢**。
            "armour":   _armour_pnp,
            "cardTraits": _traits,
            # ---- 🆕 2026-10-23（v19 · 引擎线 `新账-97` ③/⑤）：`spellType` ---------------------------------
            # 原版 `RawCardScript.spellType`（`dump.cs` 的 `SpellType` 枚举：0 Normal / 60 SecretOrder /
            #   120 Quest / 150 Secret / 230 Sabotage / …）。**真值直搬**，`-1` = 快照里没这张卡（未知）。
            # 为什么现在要它：
            #   ① `CardCriteria` 的 `spellType` 维度（`FilterMethods__CheckIfMeetsCriteria.c:300-303`
            #      `cardType` 那几个 include* 之外，`spellType` 是**独立一格**）：`DA66` 的
            #      `actingCardCriteria.spellType = 150`（"When you create or play a **Secret**"）
            #      —— 正文侧只能把它当卡名猜，认不出就永远不匹配（静默）。
            #   ② `UnitConditions` 的 240 `hasSecretOrders` / 250 `hasQuests`：原版读的是**附魔卡的**
            #      `spellType == 60 / 120`（`EntityScript__HasSecretOrders.c:31` / `HasQuests.c`）。
            #      🔴 现状：当前快照里 **spellType 60 / 120 的卡 0 张**（分布见 main() 的摘要）——
            #      那两类附魔卡不在 `cards_raw.json` 里 ⇒ 这两条条件**仍然判不了**（`ConditionEval`
            #      返回 `null`），本列只是把「附魔卡是什么类型」这一层补上，⛔ 不是凭空造 60/120。
            "spellType": (truth_int(trow, "spellType", -1) if trow is not None else -1),
            # 🆕 2026-10-25（P3 · 账 3 条 C）：原版 `RawCardScript.specialAttackScoringCriteria`（bool）。
            #   语义 / 判据 / 消费点 → `CardDef.SpecialAttackScoring` 的 doc（⛔ 别在这里抄第二份）。
            #   真值 1,351 张**全 0**（`cards_raw.json` 的 `full_raw` 现读；与 `AUD_T06` §二 同结论）
            #   ⇒ 本列今天不改变任何行为，只把「原版有这一维」带进卡表。
            #   ⚠️ bool ⇒ 缺键即 0（没有「未知」档），⛔ 不设 -1。
            "specialAttackScoring": (truth_int(trow, "specialAttackScoringCriteria", 0)
                                     if trow is not None else 0),
            # ---- 🆕 2026-10-25（资源审查 F · 数据侧落地）：`alternateArts` ----------------------------
            # 原版 `RawCardScript.alternateArts`（PPtr 数组）→ 异画 id 列表（`#HB_<卡id>`）。
            # 真值链与判据 → `资料/完全复刻/资源/R6_补核.md` §3；索引 → `数据/索引/altart_index.json`（7 条）。
            # 🔴 **缺值落 `[]`、不落 `null`**：`CardDatabase` 的契约要求「`null` = 整列缺席 / 没有行 = `[]`」
            #    分得开（见 `:3854` 那段）。本 build 只有 7 张有异画。
            # ⚠️ 目前**没有消费者**（`.cs` 里 `alternateArts` 只出现在 `Shell/DeckInfoPopup.cs:2044`
            #    的**原版字段偏移注释**里）⇒ 本列只落数据、不改任何行为。
            "alternateArts": alt_ids_of(trow),
        }
        # ---- 卡能不能进卡组（原版 `inventoryOptions` / `showInCollectionOptions`，`A1798` 2026-10-14）----
        # 🔴 **原版真值直搬**（`工具/gen_card_inventory_options.py` 四级定位出来的），本处**一个字不推**。
        #    `-1` = 真值里查不到这张卡（快照比现池旧：整个 SpaceWolves + DA75..DA74 等）⇒ 引擎**不拦**，
        #    但跑完的摘要会把条数与原因**打出来**（不许静默 —— 见 `main()` 那一段）。
        _inv, _show = raw_inv.get(cid, (-1, -1))
        entry["inventoryOptions"] = -1 if _inv is None else _inv
        entry["showInCollectionOptions"] = -1 if _show is None else _show
        if _inv is None:
            inv_unknown.append((cid, norm_str(c.get("name")), norm_str(c.get("faction"))))
        # ---- 先后手：原版 `initiative`（`A1775` 2026-10-14）----
        # 🔴 **原版真值直搬**（`工具/gen_card_initiative.py`，id 匹配复用 `card_inventory_options.json`），
        #    本处**一个字不推**。`-1` = 真值里查不到这张卡 ⇒ 判先后手时**不比**（退回原版最后一步掷硬币），
        #    跑完的摘要会把条数打出来（不许静默）。取值表见 `INIT_SRC` 那段。
        _init = raw_init.get(cid, -1)
        if _init is None:
            _init = -1
        entry["initiative"] = int(_init)
        if _init < 0:
            init_unknown.append((cid, norm_str(c.get("name")), norm_str(c.get("faction"))))
        # ---- 🆕 `A1799`：其余「有真值、我们在猜」的卡 SO 字段（2026-10-15）------------------------
        # 🔴 **本笔只落值** —— 这 10 列**现在没有任何消费者**（机制/接线另立，见 `A1799_FIELDS` 那段）。
        #    缺值一律 `-1`（整数）/ `""`（字符串）—— ⛔ 不许拿 0 顶（`0` 多半是**合法取值**：
        #    `attackDamageType=0` 是 `Normal`、`useOverDraw=0` 是「不溢出」）。
        for _col, _tf, _dflt, _doc in A1799_FIELDS:
            if _dflt == "":
                entry[_col] = truth_ref_name(trow, _tf)
            else:
                entry[_col] = truth_int(trow, _tf, _dflt)
        # ---- 🆕 2026-10-19（`A1959` 数据管线半边 ＋ `A1960`）：原版 `choicesDefinition` --------------
        #   `TargetCriteria`（`dump.cs:23341` —— `RawCardScript.choicesDefinition; // 0x2A0`）=
        #   「**这张卡要做一次选牌**」的规格。原版消费点 = `BattleManager__GetChoiceFullPool.c:36-49`：
        #   `targetsAffected == 0xb4(180, TargetsAffected.choiceResolution)` ⇒ 候选池 = `filterSpecificCards`
        #   （@0x70）；为空时回落 `BattleManager+0x308`（我们**没有**那一格）。
        #   三列（引擎侧消费者 = `Core/CardDef.cs` 的 `Choices*` 属性）：
        #     · `choicesTargetsAffected` ← `targetsAffected`（`TargetsAffected` 枚举；180 = choiceResolution）
        #     · `choicesMaxTargets`      ← `maxTargets`（面板**摆几张**，`A1959`）
        #     · `choicesSpecificCardIds` ← `filterSpecificCards`（PPtr 列表，按 `path_id` 解成 `uniqueId`）
        #   缺值口径：真值快照里没有这张卡 ⇒ 前两列 `-1`、列表空 —— ⛔ 别当 `0`
        #   （`targetsAffected = 0` 是合法取值 `any`；`maxTargets = 0` 是「不摆」）。
        _cd = (trow.get("choicesDefinition") if trow is not None else None) or {}
        _cd_ta, _cd_mt = _cd.get("targetsAffected"), _cd.get("maxTargets")
        entry["choicesTargetsAffected"] = _cd_ta if isinstance(_cd_ta, int) and not isinstance(_cd_ta, bool) else -1
        entry["choicesMaxTargets"] = _cd_mt if isinstance(_cd_mt, int) and not isinstance(_cd_mt, bool) else -1
        _cd_ids = []
        for _ref in (_cd.get("filterSpecificCards") or []):
            _pid = _ref.get("m_PathID") if isinstance(_ref, dict) else None
            _uid = truth_by_path.get(_pid)
            if _uid is None:
                # ⛔ 不许静默丢 —— 跑完摘要逐条打出来（现核 0 条）
                choices_report["unresolved_refs"].append((cid, _pid))
            else:
                _cd_ids.append(_uid)
        entry["choicesSpecificCardIds"] = _cd_ids
        if _cd_ids or entry["choicesMaxTargets"] > 0:
            choices_report["with_choice"] += 1
        # ---- 🆕 2026-10-19（`E12`）：逐候选动画时间（多候选「时间错开」的数据侧）--------------
        # 判据与字段语义 → `ANIM_GUID_MAP_TSV` 上面那一段 + `anim_times_of()` 的 doc。
        # ⚠️ **顺序 = 原版 `abilityLogic` 索引顺序**（= `card_vfx_by_card.json` 的 `es` 数组顺序，
        #    V2 报告 §3·2 实测 436 行里 435 行逐位相同）⇒ 消费端可以直接按 `a`/`n` join。
        _anim = anim_times_of(trow, anim_idx, anim_cache, anim_trig_names, anim_report)
        entry["animTimes"] = _anim
        if _anim:
            anim_report["cards"] += 1
            anim_report["rows"] += len(_anim)
        # ---- 🆕 2026-10-21（引擎线 `T03·c` 批 B · 第 0 步）：逐触发关键词的 `priority` ----------
        # 判据 / 收词三条 / 未收的那些为什么收不了 → `ABILITY_TRIGGER_KEYWORD` 上面那段长注释。
        # ⚠️ **只落 != 1 的**（缺键 = 原版默认 1 = Instant）；全池今天只有 4 张卡有非默认值。
        # 形状 = `[{"keyword": …, "priority": …}]`（`JsonUtility` 不支持字典，与 `talentNameIds` 同一条口径）。
        _tp = trigger_priorities_of(cid, name, trow, anim_trig_names, trigger_priority_report)
        if _tp:
            entry["triggerPriority"] = _tp
        # ---- 🔴🆕 2026-10-23（引擎线 P1·①）：逐 **原版 trigger 名** 的 `priority` ----------------
        # 判据 / 为什么另开一列 / 与 `triggerPriority` 的分工 → `ABILITY_TRIGGER_PRIORITY_REPORT` 上面那段。
        # 形状 = `[{"trigger": …, "name": <AbilityTrigger 枚举名>, "priority": …}]`（**只落 != 1 的**）。
        _atp = ability_trigger_priorities_of(cid, name, trow, anim_trig_names,
                                             ability_trigger_priority_report)
        if _atp:
            entry["abilityTriggerPriority"] = _atp
        # ---- 🆕 2026-10-22（引擎线 `W63` · `T03·c/d` 数据层）：逐 ability 的 `abilityContext` ----
        # 判据 / 为什么只落非 0 / `-1` 契约 / 为什么 `name` 必须落 → `ABILITY_CONTEXT_VALID` 上面那段。
        # 形状 = `[{"trigger": …, "name": <AbilityTrigger 枚举名>, ["keyword": …,] "context": …}]`
        # （`JsonUtility` 不支持字典，与 `triggerPriority` 同一条口径）。
        _acx = ability_contexts_of(cid, name, trow, anim_trig_names, ability_context_report)
        if _acx:
            entry["abilityContext"] = _acx
        # ---- 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：原版结构化条件 `conditionsFilter` ----
        # 判据 / 落值口径 / 10 个位置 / `exclude` 取反语义 → `CONDITION_ROLES` 上面那段长注释。
        # 形状 = `[{"ab":…, "lg":…, "role":…, "cond":…, "name":…, "ex":…, ["opt","op","refValue"], ["countSet"]}, …]`
        # （`JsonUtility` 不支持字典，与 `triggerPriority` / `abilityContext` 同一条口径）。
        # 🆕 v16：`role == "count"` 的行另带 `countSet`（被数集合）见 `COUNT_SET_*` 那一段。
        # ⚠️ **本列只落数据、零行为变化**（消费端下一波才切；旧文本正则这一波不删）。
        _uc = condition_rows_of(cid, name, trow, unit_cond_names, unit_conditions_report,
                                truth_by_path)
        if _uc:
            entry["unitConditions"] = _uc
        # ---- 🆕 2026-10-23（引擎线 · `B-16` 残余）：`cardAbilities[].trigger` 的全量集合 ----------
        # 判据 / 形状 / 「缺列 ≠ 空数组」的契约 → `ability_triggers_of` 上面那段。
        # ⚠️ **每张卡都落**（没 ability 的卡落 `[]`）—— 只有「真值里没有这张卡」才不落。
        _atg = ability_triggers_of(cid, name, trow, ABILITY_TRIGGER_REPORT)
        if _atg is not None:
            entry["abilityTriggers"] = _atg
        # ---- 🆕 2026-10-23（引擎线 · 战斗审计 `B-17`）：逐 ability / 逐 logic 的 `targetsAffected` ----
        # 判据 / 形状 / 两道闸门 → `ability_targets_of` 上面那段。**每张卡都落**。
        _atgt = ability_targets_of(cid, name, trow, ABILITY_TARGETS_REPORT)
        if _atgt is not None:
            entry["abilityTargets"] = _atgt
        # ---- 🆕 2026-10-24（引擎线 `A12`）：`abilityLogic[].furtherMultiplier` ----------------------
        # 判据 / 形状 / 「缺键 ≠ 0」→ `ability_multipliers_of` 上面那段。**每张卡都落**。
        _amul = ability_multipliers_of(cid, name, trow, ABILITY_MULTIPLIERS_REPORT)
        if _amul is not None:
            entry["abilityMultipliers"] = _amul
        # ---- 🆕 2026-10-24（卡表数据 `D11` · `D10` §6·1）：主动能力整条链（active / alternative）----
        # 判据 / 形状 / `gate` 的三值语义 / 为什么死数据也照落 → `active_ability_logic_of` 上面那段。
        # ⚠️ **每张卡都落**（没有行的卡落 `[]`）；**只落数据、零行为变化**（消费端另批做）。
        _aal = active_ability_logic_of(cid, name, trow, ACTIVE_ABILITY_LOGIC_REPORT)
        if _aal is not None:
            entry["activeAbilityLogic"] = _aal
        # ---- 🆕 v25（执行代理 `E9` · `A2` Q7）：logic / op 级 `targetCriteria`（36 格）--------------
        # 判据 / 形状 / 为什么带 `ab`、为什么 36 格装子对象 → `active_ability_targets_of` 上面那段。
        # ⚠️ **每张卡都落**（真值里没有这张卡才不落列）；**只落数据、零行为变化**（消费端另批做）。
        _aat = active_ability_targets_of(cid, name, trow, truth_by_path, ACTIVE_ABILITY_TARGETS_REPORT)
        if _aat is not None:
            entry["activeAbilityTargets"] = _aat
        _atc = ability_target_criteria_of(cid, name, trow, truth_by_path, ABILITY_TARGET_CRITERIA_REPORT)
        if _atc is not None:
            entry["abilityTargetCriteria"] = _atc
        # ---- 🆕 v26（执行代理 `E2` · `A3-2`）：**ability 级** `targetCriteria`（36 格）------------
        # 判据 / 形状 / 为什么带 `ab`、为什么不落 `lg` → `ability_level_criteria_of` 上面那段长注释。
        # ⚠️ **每张卡都落**（没有行 = `[]`；真值里没有这张卡才不落列）。
        # 🔴 与 v25 的 `abilityTargetCriteria` 是**两层**：那列是 `AbilityLogic.targetCriteria`（lg>=0），
        #    本列是 `CardAbility.targetCriteria`（`+0x30`，lg 恒 -1）。
        _alc = ability_level_criteria_of(cid, name, trow, truth_by_path, ABILITY_LEVEL_CRITERIA_REPORT)
        if _alc is not None:
            entry["abilityLevelCriteria"] = _alc
        # ---- 🆕 v27（执行代理 `E3`）：**ability 级 `actingCardCriteria`（`+0x28`，36 格）** ----------
        # 判据 / 形状 / 为什么不落 `lg`、为什么列名带 `ability` 前缀 → `ability_acting_criteria_of` 上面那段。
        # ⚠️ **每张卡都落**（没有行 = `[]`；真值里没有这张卡才不落列）。
        # 🔴 与 v26 的 `abilityLevelCriteria` 是**同一层的两道闸**：那列筛 `targetCard`（`+0x30`），
        #    本列筛 `actingCard`（`+0x28`）；原版短路次序 = acting → target → board。
        _aac = ability_acting_criteria_of(cid, name, trow, truth_by_path, ABILITY_ACTING_CRITERIA_REPORT)
        if _aac is not None:
            entry["abilityActingCriteria"] = _aac
        # ---- 🆕 v28（执行代理 `E4` · `E4-2`）：**`ActiveAbility.targetCriteria`（`+0x38`）** ----------
        # 判据 / 形状 / 为什么**没有 `lg`**、为什么与 `activeAbilityTargets` 不是同一格
        # → `active_ability_pick_criteria_of` 上面那段。
        # ⚠️ **每张卡都落**（没有行 = `[]`；真值里没有这张卡才不落列）。
        _aapc = active_ability_pick_criteria_of(cid, name, trow, truth_by_path,
                                                ACTIVE_ABILITY_PICK_CRITERIA_REPORT)
        if _aapc is not None:
            entry["activeAbilityPickCriteria"] = _aapc
        # ---- 🆕 2026-10-24（引擎线 `A20` · 第三批 ①/②）：主动能力分支闸 / 装填位 / 任务位 ----------
        # 判据 / 落值口径 / 「缺席 = 恒真的默认 criteria」→ `ability_gate_of` 上面那一段。
        for _gk, _gv in ability_gate_of(cid, name, trow, truth_by_path, ABILITY_GATE_REPORT).items():
            entry[_gk] = _gv
        # ---- 🆕 2026-10-21（引擎线 `T03·b`）：开局加成三列 --------------------------------------
        # 判据 / 为什么「只在非 0 时落列」→ `INITIAL_MODIFIER_COLS` 上面那一段。
        for _col in INITIAL_MODIFIER_COLS:
            _v = truth_int(trow, _col, default=0)
            if _v != 0:
                entry[_col] = _v
                initial_modifier_report[_col]["cards"] += 1
        # ---- 🆕 2026-10-21（引擎线 `T03·b`）：`skipCardDraw` 手牌效果的生产列 -------------------
        # 判据 → `skip_draws_of()` 的 doc。今天 0 张卡（`handBuff.buffType` 没有 7）。
        _sd = skip_draws_of(cid, name, trow, anim_trig_names, skip_draw_report)
        if _sd:
            entry["skipDraws"] = _sd
        # ---- 卡面逐张核对修正（2026-09-13）----
        # 上面那两列 `subtype` / `keywords` 都是**从 OCR 那份源表来的**，实测会串列、会掉数值。
        # 这一层用「1118 张逐张看卡图独立抄」的结果盖掉 —— 见 `CARD_FACE_FIXES_SRC` 的长注释。
        # 同名卡：**带阵营的键优先**（没有才退回裸卡名那一条）
        _ff_bare = face_fixes.get(name) or {}
        _ff_fac = (face_fixes_by_fac.get(entry["faction"]) or {}).get(name) or {}
        if _ff_bare: used_face_fix.add(("bare", name))
        if _ff_fac:  used_face_fix.add(("fac", entry["faction"], name))
        # 🔴 **2026-10-12（`A1620`）：两键对同一列不一致 ⇒ 当场记下来。** 原来这里是【静默】的：
        #    `_ff = dict(_ff_bare); _ff.update(_ff_fac)` ⇒ **阵营键赢**，裸键那一份**无声丢掉**，
        #    而脚本照样打印「卡面核对修正 字段 N 处 · 关键词 M 处」（那行只数「改了几处」，
        #    ⛔ 不数「有谁被盖了」）⇒ 一份**已经查证过的**修正可以一个字都不生效、还没人知道。
        #    🔴 **实测代价**（`A1611`，2026-10-12）：`工具/_apply_kw_misextract.py` 把六路子代理
        #    逐张看 PnP 核过的「误抽」清洗写进**裸卡名**键，而 `Chapter Champion` / `Thermal Weaponry`
        #    另有旧的 `阵营/卡名` 键带着**清洗前**的值 ⇒ 那 3 个词的清洗**一个字都没生效**。
        #    ⇒ 现在有冲突就在跑完的摘要里逐条报出来（见 `main()` 那一支），与本文件末尾那条
        #    「`orphan_face_fix` —— 改名后失配」同一族（都是「表里有、卡表里没生效」），
        #    合起来才是完整的「不许静默失败」。
        #    ⚠️ **判据只看「两键都有这一列且值不同」**——只在一侧出现的列不算冲突（那是正常的列互补，
        #       例：`Ultramarines/Chapter Champion` 有 `keywords`、裸键有 `desc`）。
        #    ⚠️ 值**相同**的两键（`Chapter Champion` / `Thermal Weaponry` 现在就是这样）也不报：那是
        #       **冗余**不是冲突（`A1620` 的 ℹ️；两张卡在卡池里 `name_count = 1` ⇒ 阵营键本就是多余的）。
        for _col in ("subtype", "keywords", "desc", "descZh"):
            if _col in _ff_bare and _col in _ff_fac and _ff_bare[_col] != _ff_fac[_col]:
                key_conflicts.append((entry["faction"], name, _col,
                                      _ff_bare[_col], _ff_fac[_col]))
        _ff = dict(_ff_bare)
        _ff.update(_ff_fac)
        if _ff:
            if "subtype" in _ff and _ff["subtype"] != entry["subtype"]:
                face_fixed.append((name, "subtype", entry["subtype"], _ff["subtype"]))
                entry["subtype"] = _ff["subtype"]
            if "keywords" in _ff and _ff["keywords"] != entry["keywords"]:
                face_fixed.append((name, "keywords",
                                   " ".join(entry["keywords"]), " ".join(_ff["keywords"])))
                entry["keywords"] = _ff["keywords"]
            # 🆕 2026-09-13 第三十三轮：`desc` 也能被卡面修正表盖掉
            # （那批被 OCR 丢掉图标的 `Gain N` —— 见 `load_cardface_fixes` 的注释）
            if "desc" in _ff and _ff["desc"] != entry["desc"]:
                face_fixed.append((name, "desc", entry["desc"], _ff["desc"]))
                entry["desc"] = _ff["desc"]
        fix_own_armour(entry)          # 补「卡自己的护甲」—— 源数据漏了一批，见那个函数
        # ---- 🆕 `A1776`：`armour` 列的**最终值**（在 `fix_own_armour` 之后算，它也会追加 `Armour N`）----
        # 口径 = **P（PnP 那一层）优先，真值兜底**（已裁⑥；理由与实测见上面 `_armour_pnp` 那段注释）。
        _armour_pnp = 0
        for _k in entry["keywords"]:
            _m = re.search(r"armou?r\s*(\d+)", str(_k), re.I)
            if _m:
                _armour_pnp = int(_m.group(1))
                break
        _armour_truth = truth_int(trow, "armour", 0) if trow is not None else 0
        if _armour_pnp > 0:
            entry["armour"] = _armour_pnp
            armour_src_pnp.append((cid, name, _armour_pnp, _armour_truth))
        elif _armour_truth > 0:
            # PnP 那一层没读到（`desc` 不以 `Armour N` 开头、关键词里也没有）⇒ 退真值。
            # ⚠️ 今天**一次都不触发**（PnP ⊇ 真值，见上面那段）—— 留着是因为「PnP 读不到」是
            #    由**我们的读法**造成的（只认段首），真值兜底比留 0 诚实。
            entry["armour"] = _armour_truth
            armour_src_truth.append((cid, name, _armour_truth))
        # ---- 🆕 `A1776`：真值铺底那一层**被更高层盖掉了多少**（**不许静默**）----------------
        # 权威序里 ③④⑤ 都在 ① 上面，所以「① 填了、③ 又整列换掉」是**规则内的**行为 ——
        # 但必须**看得见**：跑完打出来（`_manual_keywords` 是人工最终裁定，它换掉是真值该让位）。
        if _kw_added:
            _have_now = {_norm_name(re.split(r"[:\d]", str(k).strip())[0]) for k in entry["keywords"]}
            _gone = [t for t in _kw_added if _norm_name(re.split(r"[:\d]", t)[0]) not in _have_now]
            if _gone:
                kw_truth_wiped.append((cid, name, _gone))
        # 中文（有才写：没翻译的卡面自动回英文，不写空串进来白占体积）
        # 🔴 先查**改名别名**（见 `ZH_NAME_ALIAS` 的注释）：中文表里还留着**改名前**的键，
        #    按新名查会落空 ⇒ 卡面印英文名（实测 6 张，全是刚改名/补录的那批）。
        for k, v in zh.get(ZH_NAME_ALIAS.get(name, name), {}).items():
            entry[k] = v
        # ⚠️ **推导值放在别名之后**：`ZH_NAME_OVERRIDE` 里的卡名带数字变化，
        #    别名端过来的是旧名字的译文（会印错「第二连」），这一层再盖掉。
        if name in ZH_NAME_OVERRIDE:
            entry["nameZh"] = ZH_NAME_OVERRIDE[name]
        # 🆕 2026-10-14（`A1801`）：**官方中文名放在最后面** —— 它是权威（见 `OFFICIAL_LOC_SRC`），
        #    `zh_cards.json` 那份自译降为**兜底**（官方表里查不到这张卡时才用它）。
        #    ⚠️ 两级查法是**必需**的，⛔ 不能只按 id：官方表里有一批 `Card_Name/*` 键是 32 位 GUID
        #       （**现读 139 条**，原版包 `m_SubObjectName` 形态）、**对不上我们的卡 id**
        #       ⇒ 要走**英文名桥**（拿卡名去官方表里找英文值相同的那一条）。
        #    🔴 **现读（2026-10-19 · 口径 = 逐张拿 `cid` 去 `by_id` 查）**：池 **1127 张，
        #       id 命中 1127/1127、miss 0；`by_en` 命中 0 次**（英文名桥一次都不触发）。
        #       ⚠️ **历史读数 974（id 837 + 英文名补 137）已过期，⛔ 别引用** —— 换代公式：
        #       池张数取 `cards_engine.json` 的 `count`；命中数 = `sum(c['id'] in by_id)`；
        #       `by_en` 命中 = 上面 miss 的那批再按英文名查（同一段代码的 `official_name()` 会逐次记账）。
        _ozh, _ovia, _okey = official_name(off_id, off_en, cid, name, faction, off_conflict, off_en_bridge)
        if _ozh is None:
            off_miss.append((cid, name, entry.get("nameZh") or ""))
        elif _ozh == (entry.get("nameZh") or ""):
            off_same.append((cid, name, _ozh, _ovia))
        else:
            off_hit.append((cid, name, entry.get("nameZh") or "", _ozh, _ovia))
            entry["nameZh"] = _ozh
        # ==========================================================================================
        #  🆕 2026-10-15（P3 相 · `A1763` 数据层）：**官方中文卡面描述**灌进 `descZh`
        # ==========================================================================================
        # 🔴 **`descZh` 一列一共 3 个写手，本层插在【第 2 位】**（从低到高，**后面的赢**）：
        #
        #   | 序 | 写手 | 在哪 | 为什么在这个位置 |
        #   |---|---|---|---|
        #   | ① | `数据/卡牌翻译/zh_cards.json`（**我们自译**） | 上面 `for k, v in zh.get(...)` 那一段 | 兜底：官方表里查不到这张卡时才用它 |
        #   | ② | **官方表 `Card_Description/*`**（**本层**） | 这里 | 权威 —— 与 `A1801` 把 `nameZh` 换官方是**同一把尺子** |
        #   | ③ | `cardface_fixes.json` 的 `descZh`（**逐张 PnP 核过的人工值**） | 紧接本层之后那三行 | **最高**（铁律 7：PnP 成品卡图是尺子） |
        #
        # 🔴 **本层必须排在 ③【之前】** —— 反过来排会把那个人工值**静默盖掉**
        #    （`资料/普查产出_第三十会话/卡表块_施工设计_1014.md` §五·10 点名的坑）。
        #    现读 ③ 有 **279** 条（`cardface_fixes.json` 的 `descZh` 是 **279** 个叶子键 ——
        #    历史读数：账面 248（旧）→ 2026-10-15 现数 251 → **2026-10-18（`A2117`）现数 279**；
        #    ⛔ 别把这个数写死当判据，读 `len(cardface_fixes.json 的 descZh)`）。
        #    下面紧跟一条**结构断言**钉死这件事。
        #
        # ⚠️ **取值钥匙 = `official_name()` 解出来的那个官方键**（`Card_Name/*` 与
        #    `Card_Description/*` **共用同一个键空间**）—— 那 147 个 GUID 键的卡只有走这条路才查得到。
        #    先按我们的卡 id 直查一次（现读能中 1004 张），直查不到再退回官方键。
        _dkey = cid if cid in off_desc_by_id else (_okey if (_okey in off_desc_by_id) else None)
        _ozd = off_desc_by_id.get(_dkey) if _dkey else None
        if _ozd is None:
            off_desc_miss.append((cid, name, entry.get("descZh") or ""))
        elif _ozd == (entry.get("descZh") or ""):
            off_desc_same.append((cid, name))
        else:
            off_desc_hit.append((cid, name, entry.get("descZh") or "", _ozd,
                                 "id" if _dkey == cid else "官方名键"))
            entry["descZh"] = _ozd
        # ⚠️ `descZh` 的卡面修正必须**放在中文表之后** —— 否则刚写的又被上面那两行盖回去
        if _ff and "descZh" in _ff and _ff["descZh"] != entry.get("descZh"):
            face_fixed.append((name, "descZh", entry.get("descZh") or "", _ff["descZh"]))
            entry["descZh"] = _ff["descZh"]
        # 🔴 **结构断言（上面那张序表的判据）**：`cardface_fixes.json` 的 `descZh` 是**最高序**
        #    ⇒ **人工值必须最终生效**。这一条**结构上不可能**在层序排错时还成立：
        #    · 本层若插到人工层**之后**，官方值会盖掉人工值 ⇒ 这里 `entry["descZh"] != _man` ⇒ 红；
        #    · 谁把人工那一层删掉，同样红。
        #    ⛔ 它不是「再断言一遍已经写对的东西」—— 它比的是**表里那条人工原文**，不是本函数的中间产物。
        _man_desc = (_ff or {}).get("descZh")
        if _man_desc is not None:
            man_desc_total += 1
            if entry.get("descZh") == _man_desc:
                man_desc_ok += 1
            else:
                desc_zh_order_violations.append((cid, name, _man_desc, entry.get("descZh") or ""))
        cards.append(entry)

    # ---- 🆕 2026-10-25（引擎线 `X20`）：隐藏附魔模板入池（当前只收 `Pray Enchantment`）--------
    # 🔴 **落在主循环【之后】**：它绕开上面那一整套 OCR 层（理由见 `build_enchantment_entries` 顶上那段）。
    #    这条链的消费端 = `Core/Enchantment.cs` 的附魔层（`Enchantments.AttachPray` / `HasPray`），
    #    卡从 `ctx.CardPool` 取（`BattleDriver` 传 `CardDatabase.Load()` 的全池）。
    _ench_entries, _ench_report = build_enchantment_entries(
        truth_cards, raw, cards, truth_by_path=truth_by_path,
        anim_idx=anim_idx, anim_cache=anim_cache, anim_trig_names=anim_trig_names,
        anim_report=anim_report)
    for _e in _ench_entries:
        cards.append(_e)
    print(f"\n隐藏附魔模板   名字含 `enchant` **{_ench_report['mname_hits']}** 张 —— "
          f"`cardType 20` 模板 **{_ench_report['templates']}** 张 · "
          f"兄弟卡（`cardType != 20`）**{len(_ench_report['siblings'])}** 张"
          f"（{' · '.join(f'{n}({t})' for n, t, _ in _ench_report['siblings'])}）")
    print(f"              挂载引用：`buffEffect{{buffType 12/15/16, enchantment}}` 共 "
          f"**{_ench_report['attach_rows']}** 条 · 落在**池内卡**上的 **{_ench_report['attach_cards_in_pool']}** 张父卡")
    print(f"              入池 **{len(_ench_report['pooled'])}** 张"
          + ("：" + " · ".join(f"{i} {n}({f}/{r}) inv={inv} show={sh} ← 执行点 {ex}"
                              for i, n, f, r, inv, sh, _o, ex in _ench_report["pooled"])
             if _ench_report["pooled"] else "")
          + f"；未入池 **{len(_ench_report['pending'])}** 张模板（判据 = 形状不在已实现执行点）")
    print(f"              其中：**有池内挂载者**但形状不支持 **{len(_ench_report['shape_mismatch'])}** 张 · "
          f"**没有池内挂载者** **{len(_ench_report['no_pool_attach'])}** 张")
    for _nm, _shape, _own in _ench_report["shape_mismatch"][:8]:
        print(f"                · {_nm}：模板形状 {_shape} · 池内挂载者 {', '.join(_own[:3])}"
              + (" …" if len(_own) > 3 else "")
              + " ⇒ 缺「被动 abilityLogic 解释器」（逐张缺口 → 资料/普查产出_1025/X20_附魔能力表.tsv）")
    if len(_ench_report["shape_mismatch"]) > 8:
        print(f"                · … 其余 {len(_ench_report['shape_mismatch']) - 8} 张逐条见那张 TSV")
    for _why in _ench_report["skipped_reason"]:
        print(f"  ⚠️ 附魔入池跳过：{_why}")

    # ---- 督军后手那张防御卡（`A1070` 第 ③ 档，2026-10-14）----
    # 🔴 **落在 `cards` 造完之后**（要按阵营在**我们自己卡池的防御卡**里定位 —— 见那个函数）。
    #    值写在**督军卡**上（原版那个字段本来就长在督军 SO 上），按【阵营】填 ——
    #    真值实测「同阵营督军同卡」55/55，不变量在 `resolve_warlord_defence` 里重新断言过。
    ref_of_fac, def_report = resolve_warlord_defence(raw_def, cards)
    for _c in cards:
        if _c["type"] == "hero" and _c["faction"] in ref_of_fac:
            _c["goesSecond"] = ref_of_fac[_c["faction"]]
    _gs_n = sum(1 for _c in cards if _c.get("goesSecond"))
    _gs_hero = sum(1 for _c in cards if _c["type"] == "hero")
    # ⚠️ **覆盖不到就要出声**：我们卡池里有 13 个阵营，真值现在**也覆盖 13 个**
    #    （🔴 2026-10-23 订正：原来这里写「真值只覆盖 11 个（EC / SW 的督军没有这个字段）」——
    #     那是 2025-07-30 旧快照的缺口；中间表已按当前 build 重生成，见上面 `DEFENCE_SRC` 那段）。
    #    ⇒ 今天 `_gs_missing_fac` 应为**空**；非空就是数据断了（照样出声）。
    _gs_missing_fac = sorted({c["faction"] for c in cards if c["type"] == "hero"} - set(ref_of_fac))

    # 🔴 **「改名后失配」守卫**（2026-09-18 加）—— 修正表是**按卡名做键**的，而 `name` 在上面
    #    `STAT_FIXES` 那一步会**改名** ⇒ 旧键**静默失配**：表里那条修正再也不会生效，而且**没有任何提示**。
    #    实测事故：`stormlord` / `Diviner` 改名成 `Imotekh the Stormlord` / `Orikan the Diviner` 之后，
    #    `keywords: []`（特意清空过）与 `desc: "… Talent: Master Chronomancer"` 两条一起失效 ——
    #    后果是**天赋线丢了**（卡面明写有）＋ 卡表混进认不出的关键词（`RuleEngineTest` 红）。
    #    ⇒ 跑完把「表里有、却一张卡都没匹配上」的键打出来。**这就是本项目「不许静默失败」那条。**
    orphan_face_fix = []
    for _k in face_fixes:
        if ("bare", _k) not in used_face_fix: orphan_face_fix.append(_k)
    for _fac, _m in face_fixes_by_fac.items():
        for _k in _m:
            if ("fac", _fac, _k) not in used_face_fix: orphan_face_fix.append(f"{_fac}/{_k}")
    # 🔴 **`STAT_FIXES` 的孤立键**（2026-10-15 加）—— 与上面 `orphan_face_fix` **同一个形状、
    #    同一套口径**（2026-09-18 那条注释已经说清了共因）。为什么必须有：`STAT_FIXES` 也是
    #    **按卡名做键**、而且查的是**改名前的名字**（上面 `STAT_FIXES.get(name_before_rename)`）
    #    ⇒ 卡名一改，旧键**静默失配**：那条数值修正再也不生效、**没有任何提示**，
    #    在卡表里「看起来就像这张卡本来就是这个数」—— 正是本项目「不许静默失败」要治的东西。
    #    🔴 **实测代价**（2026-10-15 现核、当天删除）：`"Simulacrum Imperialis": {"ranged": 4}`
    #    就是这么失效的（源表已改叫 `Simulacrum Celestian`）。
    #    ⚠️ 记账口 = 上面 `used_stat_fix.add(name_before_rename)`（**有命中的每张卡各记一次**）。
    orphan_stat_fix = [k for k in STAT_FIXES if k not in used_stat_fix]

    # 🔴 **`card_stats.json` 的 `armor` 一族守卫**（`A2043`，2026-10-15 加）——
    #    **形状照抄上面两条孤立键守卫**（`orphan_face_fix` / `orphan_stat_fix`：跑完打出来、逐条列名），
    #    🔴 **但口径不同：这一条进 `_bad`**（那两条只出声、不改退出码，见它们各自那段注释）。
    #    为什么必须挡住落盘：这一族一旦失效是**静默的数值错** —— 那几张卡的 `ranged` 会重新变 0、
    #    **打不到人**，而卡表里「看起来就像这张卡本来就是这个数」（`STAT_FIXES` 键失配时正是这个症状）。
    #    ⚠️ 判据 = 「源表里 `armor` **有值**的行」逐行算出的 **(阵营, 卡名, 家族判据命中?)** 三元组，
    #      与 `OCR_ARMOR_FAMILY` **比集合相等**（**顺序无关**）；读法与主循环一致
    #      （`norm_str` + 卡名空白归一，见上面 `name = re.sub(...)`）。
    #    ⚠️ 匹配口径：**按 (阵营, 卡名)**；`OCR_ARMOR_FAMILY_ALIAS` 里那一对名字视为同一张卡。
    #      ⛔ 不按卡 id / 不按行号 —— 源表行号会随别的写手改动漂移（`R8` 报的那 6 个行号就是这么来的）。
    _armor_got = {}          # (阵营, 卡名) -> 「家族判据命中?」= 该行 `ranged_attack is None`
    for _c in raw:
        if _c.get("armor") is not None:
            _nm = re.sub(r"\s+", " ", norm_str(_c.get("name"))).strip()
            _key = OCR_ARMOR_FAMILY_ALIAS.get((norm_str(_c.get("faction")), _nm),
                                              (norm_str(_c.get("faction")), _nm))
            _armor_got[_key] = _c.get("ranged_attack") is None
    _armor_expect = {OCR_ARMOR_FAMILY_ALIAS.get((_f, _n), (_f, _n)): _p
                     for (_f, _n, _p) in OCR_ARMOR_FAMILY}
    # ⚠️ **先按 (阵营, 卡名) 配对、再比第三格** —— ⛔ 别拿三元组直接做集合差：
    #    那样一张卡的「判据位变了」会**同时**报成一条「少了」+ 一条「多了」，同一个东西报两遍。
    armor_family_bad = []
    for _k in sorted(set(_armor_expect) - set(_armor_got)):
        armor_family_bad.append(f"少了 {_k[0]}/{_k[1]} —— 源表这一行的 `armor` 已经不再有值？"
                                "⇒ 家族判据失效，这张卡的 `ranged` 会被**静默算成 0**")
    for _k in sorted(set(_armor_got) - set(_armor_expect)):
        armor_family_bad.append(f"多了 {_k[0]}/{_k[1]} —— 源表这一行 `armor` 又有值了，"
                                "这族长出了新成员，要重核 `R8` 那份清单")
    for _k in sorted(set(_armor_expect) & set(_armor_got)):
        if _armor_expect[_k] == _armor_got[_k]:
            continue
        if _armor_expect[_k]:
            armor_family_bad.append(
                f"{_k[0]}/{_k[1]} —— 期望源表 `ranged_attack` 是 **null**（这一张是**靠家族判据**"
                "`ranged_attack=null 且 armor≠null` 圈出来的），实际**不是 null** ⇒"
                " 这一行**掉出**了家族判据，`STAT_FIXES` 那条按卡名的修正会**静默失效**、"
                "这张卡的 `ranged` 会被算成它在 `ranged_attack` 里写的值")
        else:
            armor_family_bad.append(
                f"{_k[0]}/{_k[1]} —— 期望源表 `ranged_attack` **不是** null（这一张本来是"
                "「家族判据圈不出来、值直接写在源表里」那一张），实际是 null ⇒"
                " 它**变成**家族判据的成员了，要重核 `R8` 那份清单")

    # 🔴 **`card_stats.json` 的 `art` 一族守卫**（`A2011` + `A2044`，2026-10-18）——
    #    **形状照抄 `armor_family_bad`**（逐条列名打印），🔴 **口径同它：进 `_bad`**
    #    （不符 ⇒ rc=1、**一个字节都不写**）。
    #    **为什么要有它**：`art` 是「这张卡用哪张立绘」的源表列。判据 = 路径里的
    #    `.../art/<阵营目录>/...` 那一段必须与这一行自己的 `faction` 对得上
    #    （13 个 cardassets 包之间**文件名 0 重名** ⇒ 目录段这条判据可靠，`R8_card_stats审计.md` §二·1）。
    #    对不上 = 这张卡在用**别阵营**的美术 —— 今天不影响画面（立绘按卡 id 取名，全仓无一处读 `art`），
    #    但它是**源表里的静默错**，将来谁把 `art` 接进产出链就会直接印错。
    #    🔴 **用户拍板「路 B：加守卫、⛔ 不改源表」**（`准则与做法.md` §七 · `T02` 裁定 1）
    #    ⇒ 现读那 **4 条真错配**进 `ART_KNOWN_BAD`（逐条留痕、不报错），**新增的第 5 条立刻红**。
    #    ⚠️ **不报的两类**：
    #      · `noise:true` 的占位行（`Normal` / `Normal Conditions` 共 10 行，**设计上**共用
    #        UM / Sororitas 的环境占位图，`R8` §二·1 已核）；
    #      · `art` 为空（现读 21 条 —— 本地无立绘原件，**铁律 11 例外①**，不是错配）。
    ART_DIR_OF_FACTION = {
        "AstraMilitarum": "AstraMilitarum", "BlackLegion": "BlackLegion", "DarkAngels": "DarkAngels",
        "EmperorsChildren": "EmperorsChildren", "Genestealers": "GenestealerCults", "Goff": "Orks",
        "Leviathan": "Tyranids", "SaimHann": "Aeldari", "Sautekh": "Necrons", "Sororitas": "Sororitas",
        "SpaceWolves": "SpaceWolves", "TauEmpire": "Tau", "Ultramarines": "Ultramarines",
    }
    # 4 条**已裁定不改**的真错配（`R8_card_stats审计.md` §二·2，逐条开 PnP 核过）。
    # ⛔ 这张表**只许缩小**（等哪天解了「不改源表」那条裁定）：往里加一条 = 把一条新缺陷藏起来。
    ART_KNOWN_BAD = {
        ("DarkAngels", "Aggressor"),
        ("EmperorsChildren", "Terminator"),
        ("BlackLegion", "Terminator Champion"),
        ("BlackLegion", "Maulerfiend"),
    }
    art_guard_bad, art_known_bad = [], []
    for _c in raw:
        _a = _c.get("art") or ""
        if not _a:
            continue                                  # 21 条空 = 本地无原件（铁律 11 例外①）
        _am = re.search(r"/art/([^/]+)/", _a)
        _cf = norm_str(_c.get("faction"))
        _cn = re.sub(r"\s+", " ", norm_str(_c.get("name"))).strip()
        if not _am:
            art_guard_bad.append(f"{_cf}/{_cn} —— `art` 路径里认不出 `/art/<阵营目录>/` 段：`{_a}`")
            continue
        _adir = _am.group(1)
        _aexp = ART_DIR_OF_FACTION.get(_cf)
        if _aexp is None:
            art_guard_bad.append(f"{_cf}/{_cn} —— **未知阵营** `{_cf}`（`ART_DIR_OF_FACTION` 没有这一行）"
                                 f"，没法判 `art` 目录对不对：`{_a}`")
            continue
        if _adir.startswith(_aexp + "_"):
            continue
        if _c.get("noise"):
            continue                                  # 10 条环境占位图（设计，见上）
        if (_cf, _cn) in ART_KNOWN_BAD:
            art_known_bad.append(f"{_cf}/{_cn} —— `{_adir}`（已裁定**不改源表**，路 B）")
            continue
        art_guard_bad.append(
            f"{_cf}/{_cn} —— `art` 指向**别阵营**目录 `{_adir}`（应为 `{_aexp}_*`）：`{_a}`"
            " ⇒ 这张卡在用别的阵营的美术（今天不入产出链，但它是源表里的静默错）")

    # 🔴 **稀有度这一层的守卫**（`A2044`，2026-10-15 `G3`）——
    #    **形状照抄上面三条**（`orphan_face_fix` / `orphan_stat_fix` / `armor_family_bad`：逐条列名打印），
    #    🔴 **口径同 `armor_family_bad`：进 `_bad`**（不符 ⇒ rc=1、**一个字节都不写**）。
    #    **为什么要它**：稀有度是**两级按【卡名】join** 出来的（宝石表 + `RARITY_UNLISTED` /
    #    `RARITY_BY_FACTION` 两张补表），而卡名**会被改名**（`STAT_FIXES` 的 `name`、上游撤改名）
    #    ⇒ 名字一对不上，这张卡的 `rarity` 就**静默掉成空串**：卡框取不到 tier、卡组编辑的筛选
    #    与行底色一起挂 —— 而跑完的报告里**只有一行不带卡名的 ⚠️**（`still_missing` 那行）。
    #    🔴 **实测事故（本守卫要复现的那一次）**：`SOR24` 的卡名从 `Simulacrum Celestian` 撤回成
    #    `Simulacrum Imperialis` ⇒ 宝石表那一行（`:698`）按 **PnP 卡面写法**建键 ⇒ 查不着
    #    ⇒ 池里 `rarity` 变 `""`，**全流程没有任何东西出声**（当天是靠人眼看出来的）。
    #    一条谓词盖不住两个方向，所以这里是**两条**：
    #
    #    ① **症状层 —— 「空串但别名还能查到」**：`rarity` 落成空串的卡，只要宝石表**还能按它的
    #       `ocrName` 查到**，就说明「这张卡确实该有稀有度、只是主键失配」。
    #       ⚠️ **为什么用 `ocrName` 而不是 `name`**：`name` 正是主循环刚查过的那一条，
    #          拿它再查一次是**同义反复、恒为假**（查得到就根本不会是空串）—— 那就成了自证。
    #          `ocrName` 是**独立**的另一个键（源表里「PnP 卡面印的字的 OCR 记录」），
    #          改名的场景下它**往往还指着旧写法**（`SOR24` 就是：`ocrName` 至今是 `Simulacrum Celestian`）。
    #       ⛔ **漏什么**：连 `ocrName` 也对不上的改名 —— 那多半是「宝石表里本来就没这张卡」，
    #          不该报（本谓词**只报「查得到却没查到」**，不报「查不到」）。
    #
    #    ② **权威层 —— 与游戏侧真值 `cardRarity` 逐张比**：`TRUTH_CARDS_SRC` 那份**原版卡 SO 快照**
    #       （`allcards` 包）自带 `cardRarity` —— 那是**游戏自己的枚举**，比宝石表（**人眼看印刷品**
    #       记下来的色名）高一级（铁律 10 第 3 条：PnP 是印刷品；宝石表也只是它的读数）。
    #       ⚠️ 枚举映射 `1..5` 是**现算的、不是猜的**：拿这份快照与本卡池按 `uniqueId` join
    #          （1009 张），混淆矩阵**每个稀有度只对应一个取值、0 例外**
    #          （`common=1` · `rare=2` · `epic=3` · `legendary=4` · `special=5`）；本守卫落地时
    #          **全池 0 处不一致**（所以它不是「补漏」，是**防回归**）。
    #       ⚠️ `cardRarity` 是 `0` / 缺字段 ⇒ **视为「无值」**（快照里那 4 张 GSC「破坏卡」就是 `0`；
    #          它们的稀有度本来就只有卡面一条判据）⇒ 跳过、并打出跳过条数，⛔ **不拿 0 顶**
    #          （同 `truth_int` 的 `-1` 口径）。
    #       ⛔ **漏什么**：快照里没有的卡（自造 id 那 118 张）。
    #    ⇒ 两条合起来才盖住「改名 ⇒ 拿到**空**的稀有度」（①）与「改名 ⇒ 拿到**错**的稀有度」（②）。
    #    🔴 **2026-10-16（账 `A2062`）就地订正（铁律 5）—— ② 的性质变了，必须写清楚**：
    #      ② 从本笔起**不再是一条独立判据**（第 1 层就是它比的那个来源 ⇒ 拿实现证实现，**自证**）。
    #      它现在的身份 = **层序哨兵（fuse）**：**只要第 1 层还在，它恒真**；谁把第 1 层删掉、
    #      或把它挪到别的层下面，这 1005 张**立刻红**（`G3` 三次注入演示抓的就是这个）。
    #      ⇒ 真丢掉的独立判据由 **①（空串）** 与 **③（第 1 层改了值没 —— 见 `rarity_truth_filled`，
    #        那条进 `_bad`）** 承担；⛔ 别删它、也别把它当「今天全绿所以没用」——它管的正是
    #        「明天有人动这一层的**次序**」（把 `RARITY_BY_FACTION` 挪到第 1 层上面 ⇒ 立刻红）。
    rarity_guard_bad = []
    # ① 只在**最终真的落成空串**的卡上判 ——
    #    ⚠️ `_rarity_empty` 是**主循环那一刻**记的（第 1 层还没跑），而第 1 层可能把那几张救回来
    #    ⇒ 不按最终值过滤的话，① 会对「已经被真值填好了」的卡**误报**（假红）。
    _final_empty = {(c["faction"], c["name"]) for c in cards if not c.get("rarity")}
    for _rf, _rn, _roc in _rarity_empty:
        if (_rf, _rn) not in _final_empty:
            continue
        if not _roc:
            continue
        _rg, _rh = rarity_lookup(_roc)
        if _rg:
            rarity_guard_bad.append(
                f"{_rf}/{_rn} —— `rarity` 落成了**空串**，但宝石表**按这张卡的 `ocrName`"
                f"（`{_roc}`）能查到 `{_rg}`［{_rh}］** ⇒ 卡名一改、这一层就**静默丢了**")
    _rarity_truth_n, _rarity_truth_skip_n = 0, 0
    for _rc in cards:
        _rtr = truth_cards.get(_rc.get("id"))
        if _rtr is None:
            continue
        _rtv = _rtr.get("cardRarity")
        _rex = TRUTH_RARITY.get(_rtv) if isinstance(_rtv, int) and not isinstance(_rtv, bool) else None
        if _rex is None:
            _rarity_truth_skip_n += 1
            continue
        _rarity_truth_n += 1
        if _rex != _rc.get("rarity"):
            rarity_guard_bad.append(
                f"{_rc['id']}/{_rc['name']} —— 与**游戏侧真值**不一致：卡池是 `{_rc.get('rarity')}`、"
                f"原版卡 SO 快照是 `{_rex}`（`cardRarity={_rtv}`）⇒ 这一张的档位挂着")

    # 🔴 **`A2064` ②（2026-10-16）：`RARITY_UNLISTED` / `RARITY_BY_FACTION` 的【孤立键】守卫** ——
    #    **形状照抄 `orphan_face_fix` / `orphan_stat_fix`**（逐条列名打印），
    #    🔴 **口径也照抄那两条：只出声、⛔ 不进 `_bad`**（`G3` 当天把这一条留给调度台裁；
    #    本笔裁定 = **做**，理由三条：① 它与 `cardface_fixes` / `STAT_FIXES` 是**同一族的病**
    #    （表按卡名做键、卡名一改就静默变死键）；② 两张表的键现读 **21 / 23 条全部命中池内卡**
    #    ⇒ **今天必绿**；③ 它抓的是「键变成死键**且**那张卡本来有别的判据兜住」这一种 ——
    #    正是 `SOR24` 撤改名那次的形状（`_rarity_empty` + ③ 两条都没抓到它）。
    #    ⚠️ **它不查「宝石表那 68 个池外键」**（`G3` 裁过不加那条）—— 那是「PnP 写法 vs 我们的名」，
    #      本来就会有一批对不上，加它得配一张会烂的白名单。本条只看**我们自己两张补表**的键。
    _pool_keys = {(c["faction"], c["name"]) for c in cards}
    _pool_names = {c["name"] for c in cards}
    orphan_rarity_key = []
    for _k in RARITY_UNLISTED:
        if _k not in _pool_names:
            orphan_rarity_key.append(
                f"`RARITY_UNLISTED` 的键 `{_k}` —— 卡池里**没有任何卡**叫这个名字（改名了？还是删卡了？）")
    for (_rf2, _rn2) in RARITY_BY_FACTION:
        if (_rf2, _rn2) not in _pool_keys:
            orphan_rarity_key.append(
                f"`RARITY_BY_FACTION` 的键 `{_rf2}/{_rn2}` —— 卡池里**没有这张卡**（阵营或卡名对不上了）")

    # ---- 🔴🔴 生成期自检（`W25`，2026-10-18）：**id 层不许静默退回「自造」** -------------------
    # 判据 → `资料/普查产出_1018/R_引擎线_完整ID与SO后的调整评估.md` §4 第 4、5 行 +
    #        `R_卡牌线_…` §4 第 2 行（「加断言：池内 `id_made == 0`，且每个 id ∈ SO 集合」）。
    # 为什么要有它：上面那一整条 id 链（直取 → 名级联 → 自造）里，**「自造」是静默的兜底** ——
    # 一旦 SO/索引换版本、卡名改一个字，某张卡就会悄悄拿回 `XX_Card_Name` 自造 id，
    # 而引擎/存档/回放全都按 id 走 ⇒ 那才是「身份塌了还没人知道」。所以这里**硬失败**：
    # 非零退出 = 一个字节都不写（与 `A1626` 同一条口径），补好判据再重跑。
    _so_ids = set(truth_cards or {})
    _id_bad = []
    if id_made:
        _id_bad.append(f"池内有 **{len(id_made)} 张自造 id**（原版 `uniqueId` 表里落不了位）："
                       + " · ".join(f"{cid}={nm}({fac})" for cid, nm, fac in id_made[:10])
                       + (f" … 其余 {len(id_made) - 10} 张" if len(id_made) > 10 else ""))
    if not _so_ids:
        _id_bad.append("真值卡 SO 快照**为空**（`TRUTH_CARDS_SRC` 读不到）⇒ 无法证明池内 id ∈ 原版 `uniqueId` 集合")
    else:
        _not_in_so = [(c["id"], c["name"], c["faction"]) for c in cards
                      if c.get("id") not in _so_ids]
        if _not_in_so:
            _id_bad.append(f"池内 **{len(_not_in_so)} 张 id 不在真值 SO 的 `uniqueId` 集合里**："
                           + " · ".join(f"{cid}={nm}({fac})" for cid, nm, fac in _not_in_so[:10])
                           + (f" … 其余 {len(_not_in_so) - 10} 张" if len(_not_in_so) > 10 else ""))
    if _id_bad:
        print("\n🔴 id 层生成期自检**失败**：")
        for _line in _id_bad:
            print(f"   · {_line}")
        sys.exit("❌ 卡 id 层自检失败（池内 id_made 必须 = 0、且每个 id 必须 ∈ 真值 SO 的 uniqueId 集合）"
                 " —— 见 `gen_cards_engine.py` 末尾「生成期自检」那段；一个字节都没写。")

    # ---- 🔴 2026-10-18（用户改判）：`name` / `nameZh` 换**官方显示名** ----------------------------
    # 为什么放在【这里】（`cards` 全部算完、所有守卫跑完之后）：
    #   本文件里有一整族表**按源表卡名做键** —— `cardface_fixes.json`（`subtype`/`keywords`/
    #   `desc`/`descZh` 四列）、`STAT_FIXES`、`ZH_NAME_ALIAS`、`RARITY_UNLISTED`、
    #   `RARITY_BY_FACTION`，外加 `official_name()` 的**英文名桥**。
    #   在它们**之前**改名 = 那 40+ 个键**当场变死键**（静默、无报错；2026-09-18 / 09-24 各踩过一次）。
    #   ⇒ 顺序钉死：**所有按名查表的层先跑完（用源表名），最后一步才把显示名换掉**。
    #   ⚠️ 推论：`cardface_fixes.json` 的键**继续用源表名**（= 卡图资产名）—— 那不是缺陷，
    #      它是「上游表的键」；**要按显示名查表的地方必须走 id**（`CardDef.Id` / `CardDatabase.FindById`）。
    _off_names, off_names_stats = load_official_display_names()
    offname_hit, offname_same, offname_miss = [], [], []
    for _c in cards:
        _pair = _off_names.get(_c["id"])
        if not _pair:
            offname_miss.append((_c["id"], _c["name"], _c.get("nameZh") or ""))
            continue
        _en, _zh = _pair
        _old_en, _old_zh = _c["name"], _c.get("nameZh") or ""
        _bits = []
        if _en != _old_en:
            _bits.append(("name", _old_en, _en))
            _c["name"] = _en
        if _zh != _old_zh:
            _bits.append(("nameZh", _old_zh, _zh))
            _c["nameZh"] = _zh
        if _bits:
            offname_hit.append((_c["id"], _bits))
        else:
            offname_same.append(_c["id"])

    # ---- 🆕 2026-10-19（`A1959`/`A1960`）：`choicesDefinition` 的跑完读数 --------------------
    #   `pool_180` = 池内「`targetsAffected == 180` 且 `maxTargets > 0`」的卡（解析层要发 `choosecard` 的那批）。
    #   `pool_180_no_filter` = 其中 `filterSpecificCards` 为空的那几张（原版回落 `BattleManager+0x308`；
    #     我们**没有**那一格 ⇒ 引擎侧如实不动、不猜）。
    #   `not_in_pool_refs` = filter 引用里指向**池外卡**的条数（变体卡 SO 等；数据照实落值，消费侧自己判）。
    choices_report["pool_180_ids"] = [c["id"] for c in cards
                                      if c.get("choicesTargetsAffected") == 180
                                      and (c.get("choicesMaxTargets") or 0) > 0]
    choices_report["pool_180"] = len(choices_report["pool_180_ids"])
    choices_report["pool_180_no_filter"] = [c["id"] for c in cards
                                            if c.get("choicesTargetsAffected") == 180
                                            and (c.get("choicesMaxTargets") or 0) > 0
                                            and not c.get("choicesSpecificCardIds")]
    _pool_ids = {c["id"] for c in cards}
    choices_report["not_in_pool_refs"] = sum(
        1 for c in cards for _i in (c.get("choicesSpecificCardIds") or []) if _i not in _pool_ids)

    # 🆕 2026-10-19（`E12` · `A2142`）：**天赋名 → 天赋卡 id** 的真值表（见 `build_talent_name_ids`）。
    #   仍在卡表里（不另立文件）：`CardDatabase` 把它交给 `TalentCardData` 当**兜底查表**用。
    talent_name_ids = build_talent_name_ids(truth_cards, truth_by_path, cards, talent_name_report)

    return {
        "version": 30,         # v30（2026-10-25 `X20`）= 加卡不加列：池内新增隐藏附魔模板
                               #    `Pray Enchantment`（`type = enchantment`，见下面 v30 那段）。
                               # v6: 每张卡带稳定 id（🔴 2026-10-18 `W25` 起按原版 `uniqueId` 直取，
                               #    名级联降为交叉校验；自造 = 生成期自检硬失败）
                               # v7（2026-10-14 `A1798`）：加 `inventoryOptions` / `showInCollectionOptions` 两列
                               # v8（2026-10-14 `A1775`）：加 `initiative` 一列（先后手；原版真值）
                               # v9（2026-10-15 P2 相 `A1776`/`A1799`）：加 `armour` / `cardTraits` 两列，
                               #    再加 A1799 那 10 个 SO 字段列（attackDamageType · cardShortId ·
                               #    useOverDraw · cantBeRandomlyCreated · expansionPack · refNameId ·
                               #    itemDescriptionKey · fullHeroSpriteReference · eventSpriteName · targeted）
                               # v10（2026-10-19 `A1959`/`A1960`）：加 `choicesTargetsAffected` /
                               #    `choicesMaxTargets` / `choicesSpecificCardIds` 三列 ——
                               #    原版 `RawCardScript.choicesDefinition`（`TargetCriteria` @0x2A0）；
                               #    `filterSpecificCards` 的 PPtr 按 `path_id → uniqueId` 解引用。
                               # v11（2026-10-19 第五十五会话 `E12` · 战斗线 B3/B4 的接口）：加
                               #    **`animTimes`（逐候选动画时间）** 一列 —— 原版
                               #    `cardAbilities[].abilityLogic[].cardAnimations[]` 每条 `CardAnim` SO 的
                               #    `vfxDelayTime / startDelay / timeAtStartPos / timeMoving / timeAtEndPos`
                               #    ＋ 时长（`AnimInfo.GetAnimDuration`）＋ 组内累计起点 `t`
                               #    ＋ `CardAnim`/prefab 名（与 `card_vfx_by_card.json` 的 `es[]` join）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声），
                               #       否则新列会被静默读成 `0`/`null`（`施工设计 §五·3`）
                               # v12（2026-10-19 第五十五会话 `E12` · 账 `A2142`）：顶层加
                               #    **`talentNameIds`（天赋名 → 天赋卡 id 的真值表）** ——
                               #    原版 `cardAbilities[trigger==46].abilityLogic[0].summonCriteria.unitsToSummon`
                               #    的对象引用 → 我们池子的 id，键 = `CreatePool.Norm` 归一后的名字。
                               #    `SpawnTalents` 的「按显示名查卡」兜底先查它（按 id 取），
                               #    ⛔ 不再是「改名即静默失配」。
                               # v13（2026-10-21 第六十一会话 · 引擎线 `T03·c` 批 B · 数据层）：
                               #    加逐卡 **`triggerPriority`**（触发关键词 → 原版 `CardAbility.priority`，
                               #    **只落 != 1 的**，缺键 = 1 = Instant）—— 判据
                               #    `资料/普查产出_1021/W61_引擎线_T03c批B判据.md` §2.1/§2.2。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               #    🔴 今天**还没有消费者**：`CardDef.TriggerPriority(keyword)` 已就位，
                               #       `CardDatabase.CardDto` 那一跳（读列 → 传给 `CardDef` 构造）**另批做**。
                               # v14（2026-10-22 第六十三会话 · 引擎线 `W63` · `T03·c/d` 数据层）：
                               #    加逐卡 **`abilityContext`**（原版 `CardAbility.abilityContext` @+0x14 ——
                               #    「这条 ability 的载体此刻必须待在哪个区域」；**只落 context != 0 的**，
                               #    缺键 = **未知(-1)**，⛔ 不是 Play(0)）—— 判据
                               #    `资料/普查产出_1022/W63_abilityContext_施工简报.md` §1/§2/§3。
                               #    形状 `[{"trigger":…, "name":<AbilityTrigger 名>, ["keyword":…,] "context":…}]`
                               #    （`name` 必须落：只按 keyword 会漏掉 trigger 50 `UnitAttack`）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v15（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16` 根因条）：
                               #    加逐卡 **`unitConditions`**（原版 `TargetCriteria.conditionsFilter`
                               #    ＋ `excludeCondition`；**只落非 none(0) 的行**，缺列 = 未知，
                               #    ⛔ 不是「没有条件」）—— 判据
                               #    `资料/普查产出_1022/W64_兜底审计_战斗.md` 的 `B-16`；
                               #    求值器 `d:/2/tools/decomp_full/FilterMethods__MeetsConditionCriteria.c`
                               #    （末段 `:384-389` = `exclude ? !match : match`）；枚举桩
                               #    `d:/2/Warpforge_code/Scripts/Assembly-CSharp/UnitConditions.cs`。
                               #    形状 `[{"ab":…,"lg":…,"role":…,"cond":…,"name":…,"ex":…[,"opt","op","refValue"]}]`
                               #    （`ab`/`lg` 的位置编码与 `count` 那三个参数 → `CONDITION_ROLES` 上面那段）。
                               #    🔴 本列**只落数据、零行为变化**：消费端（`EffectResolver` /
                               #    `RuleCore`）下一波才切，旧文本正则这一波**不删**。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v16（2026-10-22 · 引擎线 `W64` · 战斗审计 `B-16` 的**明确缺口**）：
                               #    `unitConditions` 的 `count` 行加 **`countSet`（被数集合）** ——
                               #    原版 `BoardCriteria.countCriteria`（36 格 `TargetCriteria` 字段）
                               #    ＋ `BoardCriteria.playerTurn`；`filterSpecificCards` /
                               #    `excludeEnchantments` 按 `m_PathID → uniqueId` 解引用。
                               #    判据 → 本文件 `COUNT_SET_BOOL_FIELDS` 上面那一段
                               #    （`FilterMethods__CheckIfMeetsCriteria.c` 逐字段）。
                               #    🔴 缺省/坏值 = `-1`（引用列表 `null`），⛔ **不是 0/false**
                               #    —— 「不知道」与「是默认值」必须分得开（同 `cond` 的 `-1` 契约）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v17（2026-10-23 · 引擎线 · `B-16` 残余 241/242 ＋ 战斗审计 `B-17`）：
                               #    ① **`abilityTriggers`**（`sorted(set(cardAbilities[].trigger))`，
                               #       **每张卡都落、没 ability 落 `[]`**，缺列 = 真值里没有这张卡 = 未知）——
                               #       判据 `FilterMethods__MeetsConditionCriteria.c:286-293` →
                               #       `RawCardScript__HasAbilityType`（`RawCardScript__HasAbilityType.c:16-38`）；
                               #       消费端 `CardDef.AbilityTriggerPresence` → `ConditionEval` 的 241/242。
                               #    ② **`abilityTargets`**（逐 ability / 逐 logic 的 `targetsAffected`，
                               #       形状 `[{"trigger":…, "ta":[…]}]`，按 `cardAbilities` 顺序；
                               #       非整数落 `-1`）—— 判据 `AbilityLogic__GetTargets.c` 的
                               #       `+0x10` 分发（`self(20)` / `actingCard(80)` / `friendlyHero(120)` /
                               #       `any(0)` / `allFiltered(10)` …）；消费端
                               #       `EffectText.AssignAbilityTargets` → `EffectOp.SubjectTargetsAffected`
                               #       → `EffectResolver` 的「无施放者、无点目标」那一支。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v18（2026-10-23 · 引擎线 P1·①）：
                               #    加逐卡 **`abilityTriggerPriority`**（**原版 `AbilityTrigger` 枚举名** →
                               #    `CardAbility.priority`，**只落 != 1 的**，缺键 = 1 = Instant）——
                               #    补的是 `WhenEvent` / `ResolveAtTurn` / 打出族（**没有引擎关键词**那一半）。
                               #    形状 `[{"trigger":…, "name":<AbilityTrigger 名>, "priority":…}]`
                               #    （与 `abilityContext`（v14）同一套主键 —— 两种主键不是一套词，
                               #      所以 v13 的 `triggerPriority`（键 = 引擎关键词）**不合并**）。
                               #    消费端 = `CardDef.AbilityTriggerPriority(name)` → `ActionPump.ListenerLane`。
                               #    判据 → 资料/普查产出_1023c/引擎深审A_规则语义.md P1·1。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v19（2026-10-23 · 引擎线 `新账-97` ③/⑤）：
                               #    加逐卡 **`spellType`**（原版 `RawCardScript.spellType`，`SpellType` 枚举；
                               #    **真值直搬**，快照里没这张卡 = `-1` 未知）—— 消费端两处：
                               #    ① `CardCriteria.SpellType`（`WhenEvent` 的主语筛，`DA66` 的 Secret=150）；
                               #    ② `Enchantments` 的附魔 `SpellType` 兜底（240 `hasSecretOrders` /
                               #       250 `hasQuests` 读的 `EntityScript__HasSecretOrders.c:31`）。
                               #    🔴 本列**只落数据、零行为变化**：当前快照 `spellType == 60 / 120` 的卡 **0 张**
                               #    （分布见 `main()` 的摘要）⇒ 那两条条件**照旧返回 null（判不了）**。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v20（2026-10-24 · 引擎线 `A12` · 第 3 件）：
                               #    加逐卡 **`abilityMultipliers`**（原版 `AbilityLogic.furtherMultiplier`，
                               #    `+0x38`；`PlayAbility.c:235-236` 的 `local_178 *= furtherMultiplier`）。
                               #    形状与 `abilityTargets` 同形（按 `cardAbilities` / `abilityLogic` 下标）；
                               #    **缺键 = `-1` 未知**，`0` = 原版明确「不乘」。
                               #    🔴 该列**有消费端**（`EffectText.AssignAbilityTargets` →
                               #    `EffectOp.AbilityFurtherMultiplier` → `EffectResolver` 的次数那一格），
                               #    不是死列。现读：>1 的卡 27 张（31 个 logic）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v21（2026-10-24 · 引擎线 `A20` · 第三批 ①/②）：
                               #    加**主动能力分支闸 / 装填位 / 任务位**六列 ——
                               #    `hasActiveAbility`（`+0x220` 由卡数据 `+0x2A9` 灌）·
                               #    `activeNeedsLoading` / `alternativeNeedsLoading`（`ActiveAbility.needsLoading`
                               #    `+0x22`）· `hasAlternativeAbility`（`+0x2AA`）·
                               #    `quest`（`RawCardScript._quest` `+0x240`）· `questTimes`（`+0x7C`）·
                               #    `normalAbilityBoardCriteria`（`BoardCriteria`：`playerTurn` /
                               #    `countOptions` / `triggerIf` / `referenceValue` / `countSet`）。
                               #    判据 → 本文件 `ABILITY_GATE_REPORT` 上面那一段；
                               #    消费端 = `ConditionEval` 的 280 / 320 ＋ `CardDef.UsesAlternativeAbility`。
                               #    🔴 **只落非默认值**（`0` 是合法取值「假」），缺席 = 0/false；
                               #    `-1` = 真值里查不到这张卡（未知，⛔ 不是 0）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v22（2026-10-24 · 引擎线 `新账-95` #2 的残余 8 个 role）：
                               #    `unitConditions` 的 **`mult` / `multi` / `avail` / `choice` 四行
                               #    各加一个「整条 criteria」对象** —— 键名 `multiplierSet` / `multiSet` /
                               #    `availSet` / `choiceSet`，**36 格与 `countSet` 同形**
                               #    （同一个 `criteria_set_of`，⛔ 别各写一份字段表）。
                               #    原版位置与判据：
                               #      · `mult`   = `abilityLogic[].multiplier`（`AbilityLogic__PlayAbility.c:181-236`）；
                               #      · `multi`  = `CardAbility.multiCountCriteria`(`+0x40`) ——
                               #                   `RawCardScript__TriggerAbilities.c:230-250`；
                               #      · `avail`  = `RawCardScript.targetsAvailableCounter`(`+0x298`) ——
                               #                   `BattleCardUI__ShowAvailableTargets.c:42`（**只服务 UI**）；
                               #      · `choice` = `RawCardScript.choicesDefinition`(`+0x2A0`) ——
                               #                   `BattleManager__GetChoiceFullPool.c:28-35`。
                               #    🔴 `playerTurn` **结构上不适用**这四格 ⇒ 落 `-1`、**不进 `set_bad`**
                               #    （与 `countSet` 的「坏值 `-1`」分开，⛔ 别混）。
                               #    消费端 = `CardDef.ConditionSpec.{MultiplierSet,MultiSet,AvailSet,ChoiceSet}`
                               #    → `CountEval` / `EffectResolver`（`mult`）/ 自检（另三格）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v23（2026-10-24 · 卡表数据 `D11` · 战斗审计 `D10` §6·1）：
                               #    加逐卡 **`activeAbilityLogic`** —— 原版**主动能力整条链**：
                               #    `RawCardScript__ResolveActiveAbility.c:18/26-31` 选 active / alternative，
                               #    `:50` 取该 Ability 对象的 `activeAbilityLogic`，
                               #    `:58-67` 逐行 `AbilityLogic__PlayAbility(…, 400, 0)`（400 = `ThisUnitAbilityUsed`）。
                               #    形状 `[{"ab":…,"gate":…,"trigger":400,"needsLoading":…,"ta":[…],"fm":[…]}]`
                               #    （`ab` = -1 active / -2 alternative，与 `unitConditions` 同词；
                               #      `ta`/`fm` 与 `abilityTargets`/`abilityMultipliers` 逐格同契约）。
                               #    🔴 **死数据照落、`gate: 0`**（`hasActiveAbility == 0` 的 45 张 / 52 行
                               #    —— `CardScript__ResolveActiveAbilityPlayed.c:22` 直接 return）：
                               #      「缺席 = 未知」是硬契约，⛔ 别把「已知不消费」写成「没收到」。
                               #    🔴 **只落数据、零行为变化**：消费端（`CardDef` / `EffectText` /
                               #      `EffectResolver`）**另批做** —— 当前 0 个运行期消费者（D10 §3·1）。
                               #    🔴 踩坑：`alternativeAbility` 里的列表键名**也叫 `activeAbilityLogic`**。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v24（2026-10-24 · 执行代理 `E1`）：`activeAbilityLogic` 每行**加三个真值键**
                               #    （⛔ 不是新列）—— `ca`（`chosenAbility` = 原版 `AbilityEffect` 枚举，
                               #    这一行「做什么」的唯一真值）· `pa`（`playersAffected`，0 牌/单位、10 玩家自己）·
                               #    `dmg`（`damageCriteria` 原值 `{min,max,opt,type}`；⚠️ 只有 damage 族 10/12/13/15 才读，
                               #    非伤害行的 `(1,1,0,0)` 是 SO 序列化默认值）。
                               #    🔴 目的 = 让**能力种类**成为「行 ↔ op」的判据（v23 的 38/87 只配了 1:1 那一档）；
                               #    形状仍 `[{"ab","gate","trigger","needsLoading","ta","fm","ca","pa","dmg"}]`，
                               #    逐格对齐 `ta`/`fm`（同长度、同 `-1 = 未知` 契约）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v25（2026-10-24 · 执行代理 `E9` · `A2_引擎战斗判据总表.md` Q7）：
                               #    加**两列**（⛔ 是两个新键，不是给旧列加格）——
                               #    ① **`activeAbilityTargets`** = 主动能力两条支（`activeAbility` /
                               #       `alternativeAbility`）的 `activeAbilityLogic[j].targetCriteria`
                               #       **其余 36 格**：`[{"ab","lg","criteria":{36 格}}]`；
                               #    ② **`abilityTargetCriteria`** = 普通 ability 的
                               #       `cardAbilities[i].abilityLogic[j].targetCriteria` 36 格：
                               #       `[{"ab","trigger","lg","criteria":{36 格}}]`。
                               #    形状里**为什么带 `ab`**：实测 89 张卡有两条 ability 共用同一 `trigger`
                               #    ⇒ `(trigger, lg)` 不唯一（`A2` Q7 的建议形状漏了这一格）。
                               #    **为什么 36 格装子对象 `criteria`**：C# 侧 36 格 DTO 只有一份
                               #    （`CountSetDto`）⇒ 平铺就得再抄一份（铁律「两处写同一条规则」）。
                               #    判据：`AbilityLogic.cs:16-17`（`targetCriteria` 字段）·
                               #    `AbilityLogic__PlayAbility.c:262`（每行 `GetTargets(…, 本行 criteria, …)`）·
                               #    `AbilityLogic__GetTargets.c` + `FilterMethods__CheckIfMeetsCriteria.c`（逐字段）。
                               #    🔴 本批**只落数据、零行为变化**：消费端（`CardDef` 收 DTO →
                               #    `EffectText` 挂 op → `EffectResolver` 按 36 格筛）**另批做**。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v26（2026-10-25 · 执行代理 `E2` · `A3-2`）：
                               #    加**一列** **`abilityLevelCriteria`** —— 原版
                               #    **`CardAbility.targetCriteria`（`+0x30`，ability 级）的其余 36 格**：
                               #    `[{"ab":i, "trigger":…, "criteria":{36 格}}]`（`lg` 不落 = 恒 -1）。
                               #    🔴 与 v25 的 `abilityTargetCriteria` 是**两层同名对象**：
                               #      那列 = `AbilityLogic.targetCriteria`（logic 级，`lg >= 0`）；
                               #      本列 = `CardAbility.targetCriteria`（ability 级，`lg` 恒 -1）。
                               #    判据：`CardAbility__CanTriggerAbility.c:131-135`
                               #    （`CheckIfMeetsCriteria(thisCard, targetCard, targetCard, *(ability+0x30), 0)`；
                               #      形参表 `dump.cs:20360` ⇒ 被筛对象 = **`targetCard`**，⛔ 不是 `actingCard`）。
                               #    `targetsAffected == 0 (any)` ⇒ 原版 `return 1` ⇒ 零信息、**不落**
                               #    （口径 = `criteria_carries_targets`）。
                               #    消费端 = `CardDef.AbilityLevelCriteriaForTrigger`（**唯一入口**）→
                               #    `ActionPump.EnqueueAbility` 的触发前闸（定位不唯一 ⇒ 出声并跳过）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v27（2026-10-25 · 执行代理 `E3` · `E2` 报告 §六）：
                               #    加**一列** **`abilityActingCriteria`** —— 原版
                               #    **`CardAbility.actingCardCriteria`（`+0x28`，第一道闸）的其余 36 格**：
                               #    `[{"ab":i, "trigger":…, "criteria":{36 格}}]`（`lg` 不落 = 恒 -1）。
                               #    🔴 与 v26 的 `abilityLevelCriteria` 是**同一层的两道闸、两个候选**：
                               #      那列筛 `targetCard`（`+0x30`）；本列筛 `actingCard`（`+0x28`）。
                               #    判据：`CardAbility__CanTriggerAbility.c:127-130`
                               #    （`CheckIfMeetsCriteria(thisCard, actingCard, targetCard, *(ability+0x28), 0)`；
                               #      形参表 `dump.cs:20360`）。
                               #    `targetsAffected == 0 (any)` ⇒ 原版 `return 1` ⇒ 零信息、**不落**
                               #    （口径 = `criteria_carries_targets`；93 行「36 格非平凡」里那 10 行
                               #     `ta = 0` 因此落成 **83 行**）。
                               #    消费端 = `CardDef.ActingLevelCriteriaForTrigger`（**唯一入口**）→
                               #    `ActionPump.EnqueueAbility` 的**第一道闸**（Attack / Die 两族）。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v28（2026-10-25 · 执行代理 `E4` · `E4-2`）：
                               #    加**一列** **`activeAbilityPickCriteria`** —— 原版
                               #    **`ActiveAbility.targetCriteria`（`+0x38`）的其余 36 格**：
                               #    `[{"ab": -1|-2, "criteria":{36 格}}]`（**对象级、没有 `lg`**）。
                               #    🔴 与 v25 的 `activeAbilityTargets`（= `activeAbilityLogic[j].targetCriteria`）
                               #      **不是同一格**：那列是「这条 logic 从哪儿取目标」，本列是
                               #      「这个格位能不能被点」（`RawCardScript__IsValidActiveAbilityTarget.c:103-107`）。
                               #    `targetsAffected == 0 (any)` ⇒ 原版 `return 1` ⇒ 零信息、**不落**
                               #    （口径 = `criteria_carries_targets`）。
                               #    ⚠️ `conditionsFilter`/`excludeCondition` **不在本列**（现读只有 `SW9` 1 行非零，
                               #      而 `unitConditions` 里 `ab=-1/-2` 那些行是 **logic 级**的）⇒ 放宽一格，如实记。
                               #    消费端 = `CardDef.ActiveAbilityPickCriteriaFor(ab)`（**唯一入口**）→
                               #    `RuleCore.CanUseAbility` 的目标闸。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v29（2026-10-25 · 执行代理 `X6` · 口径乙 · `资料/待办判据_引擎_1025b.md` §一）：
                               #    `abilityLevelCriteria` 的每一行加 **`ca`** —— 该 ability 的
                               #    **能力种类族名**（`chosenAbility` 的家族；`""` = 判不了）。
                               #    形状变成 `[{"ab":i, "trigger":…, "ca":"damage", "criteria":{36 格}}]`
                               #    （只**新增一格**，键序 `ab`→`trigger`→`ca`→`criteria`）。
                               #    为什么：23 张卡的 `trigger 0` ability 定位不唯一（同一 trigger 两条）
                               #    ⇒ 那 32 行 criteria 一格都没被消费；按家族认回来才能接闸。
                               #    判据 / 三档取值 / 为什么按 ability → 本文件 `ABILITY_KIND_FAMILY` 上面那段；
                               #    族名白名单 = `CardDef.ActiveAbilityKindFamilyNames`（自检有跨语言对账断言）。
                               #    消费端 = `RuleCore.AbilityFamilyTargetAllows` → `EffectResolver` 出牌正文逐 op 闸。
                               #    ⚠️ 加列【必须】bump（`CardDatabase.cs` 的 version 守卫靠它出声）。
                               # v30（🆕 2026-10-25 · 引擎线 `X20` · 用户拍板「允许加入」）：
                               #    **没有加列，加的是「卡」**：把原版隐藏附魔卡 `Pray Enchantment`
                               #    （`uniqueId 9218576fd3e26554db5b4c239a36dbc7`，`cardType 20`）
                               #    按卡数据链入池（`type = "enchantment"`，池 1127 → 1128 张）。
                               #    造法 = `build_enchantment_entries()`（**绕开**主循环那套 OCR 层，
                               #    理由见那段顶上）；其余 117 张附魔模板**只记账、不入池**。
                               #    消费端 = `Core/Enchantment.cs` 的附魔层（`AttachPray` / `HasPray` /
                               #    `RemovePray`，落点 `RuleCore` 的 pray 支 / 回合开始 / 受伤 / 疲劳）。
                               #    ⚠️ 只有 `< N` 的守卫、没有「> N」的守卫 ⇒ 不 bump 也读得进来；
                               #    仍 bump 是因为**卡池内容变了**（`PinnedPoolMd5` / 走查工具要认这一版）。
        "source": "Unity/数据/游戏数据/card_stats.json（稀有度另取 资料/卡牌数据表/卡牌宝石稀有度_0824.md；"
                  "英文名 / 中文名都以**原版官方表** 数据/游戏数据/官方本地化_en+zh-CN.json 的 `Card_Name/*` 为准"
                  "（🔴 2026-10-18 用户改判：**`name` 也换官方显示名** —— 此前「卡名一个字不动」的 PnP 时代口径作废；"
                  "⛔ 只换显示名、**不换 id**；名字只作显示，引擎一律按 id 走）"
                  "（⚠️ 2026-10-19 现读：这份表 = **当前 build（2026）**的 10,854 条；历史上的 2025-07-30 旧快照已不用 ——"
                  "官方表里没有的卡才用 数据/卡牌翻译/zh_cards.json 那份自译；现读 0 张），"
                  "中文效果 2026-10-15（P3 相 `A1763`）起**同样以该表的 `Card_Description/*` 为准**"
                  "（键空间与 `Card_Name/*` 同源；官方表里没有、或原表里 en == zh的那批才用 zh_cards.json 的自译）"
                  "—— 🔴 口径：**只换 `descZh`、`desc` 不动**（`资料/任务_卡组卡牌战斗.md` 的 `A1763` 那一行）；"
                  "subtype/keywords 另按 数据/游戏数据/cardface_fixes.json 修正；"
                  "id 按**原版 `uniqueId` 直取**（`(阵营码, 卡名) → uniqueId` 唯一落位表，名字取自"
                  " 资料/普查产出_1018/allcards_1350/cards_raw.json 的 `cardName`/`m_Name` 尾巴 +"
                  " 数据/游戏数据/card_ids.json 的 `mapping`；名级联只作交叉校验 ——"
                  " 2026-10-18 `W25`，现读池内 1127/1127 真 id、自造 0）；"
                  "督军的 goesSecond 另取 数据/游戏数据/warlord_goes_second.json —— **原版真值**，"
                  "来源 = 抢救出的原版远端包 allcards（见该文件里的 provenance）；"
                  "inventoryOptions / showInCollectionOptions 另取 数据/游戏数据/card_inventory_options.json"
                  "—— 同为**原版真值**（同包），由 工具/gen_card_inventory_options.py 落到我们卡池的 id 上；"
                  "initiative 另取 数据/游戏数据/card_initiative.json"
                  "—— 同为**原版真值**（同包），由 工具/gen_card_initiative.py 落到我们卡池的 id 上；"
                  "cardTraits / armour（兜底那半）/ A1799 那 10 列另取**原版 allcards 快照本体**"
                  " 资料/普查产出_1018/allcards_1350/cards_raw.json（**当前 build** · 1,351 张卡 SO）"
                  "—— 🔴 那些列只在 PnP 成品卡图【不印】的地方生效（闸 = `施工设计 §七 已裁⑥`）；"
                  "🔴 **2026-10-18 起 `health` / `cost` / `attack` 三列改以该快照为真值**"
                  "（`health←maxHealth` · `cost←manaCost`（`>=0` 照抄；🔴 2026-10-19 起 `-1` ="
                  "「不可直接打出」哨兵，**原样落 `cost=-1`** —— 4 张 `Dark Pact of *` 附魔模板，"
                  "`CardView.ShowsCost` 因此不印费用格）· `attack←meleeAttack`；"
                  "判据 = `资料/普查产出_1018/R_督军10血成因.md` §1 结论 A：当前 build 的 SO 就是真值）；"
                  "`ranged` / `desc` / `keywords` / `art` 不动",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。"
                "改数据请改 card_stats.json / 数据/游戏数据/官方本地化_en+zh-CN.json / "
                "数据/卡牌翻译/zh_cards.json / 数据/游戏数据/cardface_fixes.json 后重跑。"
                "name 与 nameZh 都以原版官方表的 `Card_Name/*` 为显示名（2026-10-18 起；`name` 此前取源表写法）；"
                "nameZh 与 descZh 都优先取原版官方中文表（`Card_Name/*` / `Card_Description/*`；"
                "查不到、或原表里 en == zh的才回 数据/卡牌翻译/zh_cards.json 那份自译，见 source）；"
                "🔴 `descZh` 一列有 3 个写手，权威序（低→高）= zh_cards.json（自译）→ 官方 `Card_Description/*` → "
                "`cardface_fixes.json` 的 `descZh`（逐张 PnP 核过的人工值，**最高**）；"
                "⚠️ 官方正文是 **I2 模板层**（带 `[[枚举名]]` / `((Key,arg))` / `$$卡id$$` 记号）——"
                "印之前要过记号渲染层 `CardPresentation/Core/CardIcons.cs` 的 `RewriteMarks`（`A1763` 代码层）；"
                "subtype 是**卡面那行橙字**（部队卡=兵种 Infantry/Vehicle/…；防御卡=Defence；"
                "督军=Warlord；有些计策=Dark Pact/Overlord Power/Combat Elixir…），空串=卡面没这行。"
                "id 是**稳定身份**：原版 id 形如 `AM12`（字母前缀+纯数字）；"
                "🔴 2026-10-18（`W25`）起按原版 `uniqueId` **直取**（现读池内 1127/1127 真 id、**自造 0**）；"
                "`AM_Some_Card`（带下划线）那种**自造形态只在判据缺失时才会出现**，"
                "而生成器末尾有硬断言（自造 ⇒ 非零退出、一个字节都不写）。"
                "goesSecond = 原版 `RawCardScript.goSecondCardInHand`（只写在督军卡上）："
                "该督军后手那一局会拿到的防御卡 id；空串 = 真值里没有这个阵营的督军。"
                "inventoryOptions / showInCollectionOptions = 原版同名字段（`CardInventoryOptions` /"
                "`ShowInCollectionOptions` 枚举），**`-1` = 真值里查不到这张卡**（快照比现池旧）——"
                "⛔ 别把 -1 当成 0（InInventory）；判据消费者 = `RuleEngine/Core/DeckRules.cs` 的"
                "`CannotAddToDeck`（原版 `DeckUtility__ValidateDeckCards.c:68-77`）。"
                "initiative = 原版 `RawCardScript.initiative`（`Initiative` 枚举："
                "Undefined=0/Low=10/Medium=20/High=30/VeryHigh=40），**`-1` = 真值里查不到这张卡**"
                "（同上快照原因）—— 先后手判据（`RuleEngine/Core/FirstSeatRules.cs`）对 `-1` **不比**，"
                "退回原版最后一步掷硬币；⛔ 别拿默认值顶上。"
                "🔴 `armour`（v9）= **护甲值的一等公民**（`Armour N` 关键词里的那个 N），"
                "值与 trait **解耦**（真值 `CardTraits` 里 `name==\"armour\"` 全快照只有 2 条，"
                "而 `armour` 数值 >0 有 62 张 —— `A1748_原版trait真值.md` §7）。"
                "取值 = **PnP 那一层（`keywords` 里的 `Armour N`）优先、真值 `armour` 兜底**。"
                "🔴 **`A2036`（2026-10-18 现读）：消费者【已切换】** —— `RuleEngine/Core/CardDef.cs:297-299`"
                "（`ArmourFromPool = armour > 0 ? armour : 0`，非 0 时写回 `_keywords[KeywordTable.Armour]`）"
                "⇒ **本列非 0 就赢过 `keywords`**；本列 = 0 才回落到 `KwValue(\"armour\")` 的旧口径。"
                "🔴 `cardTraits`（v9）= 真值 `CardTraits` 的**全量**显示写法（**读 `CardTraits`、不读 "
                "`ShowTraits`** —— 后者非空只 32 张，对着它判会把 633 张误判成「没挂 trait」）。"
                "⚠️ 它**不喂引擎**（纯落值）：引擎读的是 `keywords`，不读这一列。"
                "🔴 **`A2036`（2026-10-18 现读）**：原来这里写着「里面包含 `Frenzied`/`Rage`/`Unstoppable`"
                "三个引擎 `CardDef.Prefixes` **未登记**的词，那三个**不进 `keywords`**（写了会被静默丢弃）」"
                "—— **两句都已过期**：① 那三个已于 2026-10-15 登记进 `CardDef.Prefixes`"
                "（`rage`→`Penitence` · `frenzied`→`Destroyer` · `unstoppable`→`Unstoppable`），"
                "生成器的 `TRAIT_ENGINE_UNREGISTERED` 也随之清空 ⇒ 真值那一层**会**把"
                "`Destroyer`/`Penitence`/`Unstoppable` 补进 `keywords`（第 ① 层只补空；"
                "第 ③ 层 `_manual_keywords` 仍可按卡面否决 —— 现读 `keywords` 里 Penitence **5** 张 · "
                "Destroyer **10** 张 · Unstoppable **0** 张，后者是 `SAU7` 的 `_manual_keywords` "
                "按卡面「没印」挡掉的）；② 这一列的字面值也已按卡面换过"
                "（`frenzied`→`Destroyer`、`rage`→`Penitence`，见 `TRAIT_DISPLAY`），不再是那三个枚举名。"
                "🔴 A1799 那 10 列（`attackDamageType` / `cardShortId` / `useOverDraw` / "
                "`cantBeRandomlyCreated` / `expansionPack` / `refNameId` / `itemDescriptionKey` / "
                "`fullHeroSpriteReference` / `eventSpriteName` / `targeted`）= 原版同名字段的**落值**，"
                "**目前没有任何消费者**（机制另立）；缺值 = `-1`（整数）/ `\"\"`（字符串），"
                "**⛔ 别把 `-1` 当成 `0`**（`attackDamageType=0` 是 `Normal`、`useOverDraw=0` 是「不溢出」——"
                "都是合法取值）。`cardShortId` **⛔ 不许当卡 id 换**（与 `A970` 那套 0/763 重合、全库 0 处读取点）。",
        "count": len(cards),
        "cards": cards,
        # 🆕 v12（`E12` · `A2142`）：天赋名 → 天赋卡 id（键已按 `CreatePool.Norm` 归一）。
        #   数组形状（`JsonUtility` 不支持字典）：`[{n, id}, …]`。
        "talentNameIds": talent_name_ids,
    }, skipped, len(raw), filled, still_missing, stat_fixed, face_fixed, id_orig, id_made, id_truth, \
       (id_direct, id_cascade, id_cross_mismatch, direct_ambiguous), \
       orphan_face_fix, orphan_stat_fix, armor_family_bad, art_guard_bad, art_known_bad, \
       key_conflicts, (def_report, _gs_n, _gs_hero, _gs_missing_fac), \
       (off_hit, off_same, off_miss, off_conflict, off_bad, off_stats, off_en_bridge), \
       (off_desc_hit, off_desc_same, off_desc_miss, off_desc_bad, off_desc_stats,
        desc_zh_order_violations, man_desc_ok, man_desc_total), \
       (inv_unknown, raw_inv), \
       (init_unknown, raw_init), \
       (truth_hit, kw_truth_filled, kw_truth_skipped, kw_truth_wiped,
        armour_src_pnp, armour_src_truth, hero_cost_from_truth, hero_cost_kept,
        discarded_hasstats, truth_cards, truth_by_name, kw_order_violations,
        so_stat_changed, so_stat_skipped, unplayable_cost_set), \
       (rarity_guard_bad, _rarity_truth_n, _rarity_truth_skip_n,
        rarity_truth_filled, rarity_src_truth, rarity_src_table, rarity_src_source,
        orphan_rarity_key), \
       (offname_hit, offname_same, offname_miss, off_names_stats), \
       choices_report, anim_report, talent_name_report, trigger_priority_report, \
       ability_trigger_priority_report, \
       ability_context_report, initial_modifier_report, skip_draw_report, \
       unit_conditions_report


def fix_own_armour(entry):
    """补上「这张卡**自己**的护甲」—— 源数据漏了一批。

    卡面右侧那枚盾里的数字、以及战斗里的减伤，都来自 `Armour N` 关键词。
    但源数据（`card_stats.json` 的 `keywords`，OCR 出来的）**漏了将近一半**：
    当时（池 1130 张）desc 写了 `Armour N` 的有 98 张、只有 41 张进了 keywords
    —— ⚠️ **这组是一次性实测、不是现状**（池张数会变，⛔ 别照抄）。
    症状：**卡面上印着「护甲 1」，打起来一点护甲都没有**（`UnitState.Armor` 是 0）。

    判据取**窄的**：只有 desc **以 `Armour N` 开头**才算「卡自己的护甲」。
      · `Armour 1. Codex: Gain +1 melee this turn` → 自己的护甲 1 ✓
      · `Gain Armour 2` / `Give Armour 1 to a friendly troop` → 是**效果给的**，不是自己的 ✗
    这样只补该补的，不会把「给别人加护甲」的卡也变成有护甲。

    🔴 **2026-10-15（`AD` 笔 · `DA66`）：上面那道「已有就不补」的闸原来写宽了，已收窄（铁律 5）。**
      · **原写法** = `any(re.search(r"armou?r", k, ...) for k in keywords)` ⇒ **裸词 `Armour` 也算「已有」**
        ⇒ 直接 `return`。而裸词**恰恰是「OCR 没读到数字」的形态**，不是「已经有护甲值了」。
      · **实测代价**（`AB` 亲开 PnP 成品卡图核过）：`DA66 Ravenwing Ballistus Dreadnought`
        —— `keywords` = `['Armour']`（裸词）· `desc` = `Armour 1. When you create or play a Secret, …`
        ⇒ 被那道闸挡下 ⇒ `armour` 列写 **0**，而卡面关键词行印 `Armour 1.`、右下银盾印 **1**。
        （全池**只此一张**：现扫「关键词里有裸 `Armour` 且 `desc` 以 `Armour N` 开头」= 1 张。）
      · **改法**（两条）：
        ⓐ 闸只看**带数字**的护甲条目（`Armour N`）—— 有数字才是「已经有值」；
        ⓑ 裸词那一格**原地升级**成 `Armour N`（保序、不追加第二条）——
           追加会让卡面印两遍（`护甲。护甲 1。`，`CardText.KeywordSegment` 逐条比字面）。
      ⚠️ **旧口径那一支不受影响**：`CardDef.KwValue(armour)` 对裸词本来就走 `FirstNumber` 兜底 = 1
        ⇒ 改前改后**都是 1**（`AD` 离线逐张量过：全池只有 `DA66` 这一张落到本分支）。
    """
    if not entry.get("desc"):
        return
    m = re.match(r"^\s*Armour (\d+)", entry["desc"], re.I)
    if not m:
        return
    kws = list(entry.get("keywords") or [])
    # ⓐ 已经有「带数字」的护甲条目 ⇒ 不再补（⛔ 裸词不算 —— 那正是本函数要救的形态）
    if any(re.search(r"armou?r\s*\d+", str(k), re.I) for k in kws):
        return
    # ⓑ 有裸词就**原地升级**（保序、不出现第二条）；没有才追加到末尾
    for i, k in enumerate(kws):
        if re.match(r"^\s*armou?r\s*\.?\s*$", str(k), re.I):
            kws[i] = "Armour " + m.group(1)
            entry["keywords"] = kws
            return
    entry["keywords"] = kws + ["Armour " + m.group(1)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    ap.add_argument("--write", action="store_true",
                    help="显式落盘（不带它 = 干跑：照常跑完、只打印，不写任何文件）")
    args = ap.parse_args()
    if args.check and args.write:
        ap.error("--check 与 --write 不能同时给（一个是只对账、一个是显式落盘）")

    (doc, skipped, total, filled, still_missing, stat_fixed, face_fixed,
     id_orig, id_made, id_truth, id_new, orphan_face_fix, orphan_stat_fix, armor_family_bad,
     art_guard_bad, art_known_bad, key_conflicts,
     goes_second, off, off_desc,
     inv, init, truth, rarity_guard, offname, choices_report, anim_report,
     talent_name_report, trigger_priority_report, ability_trigger_priority_report,
     ability_context_report,
     initial_modifier_report, skip_draw_report, unit_conditions_report) = build()
    offname_hit, offname_same, offname_miss, off_names_stats = offname
    id_direct, id_cascade, id_cross_mismatch, direct_ambiguous = id_new
    def_report, _gs_n, _gs_hero, _gs_missing_fac = goes_second
    off_hit, off_same, off_miss, off_conflict, off_bad, off_stats, off_en_bridge = off
    (off_desc_hit, off_desc_same, off_desc_miss, off_desc_bad, off_desc_stats,
     desc_zh_order_violations, _man_desc_ok, _man_desc_total) = off_desc
    inv_unknown, raw_inv = inv
    init_unknown, raw_init = init
    (truth_hit, kw_truth_filled, kw_truth_skipped, kw_truth_wiped, armour_src_pnp, armour_src_truth,
     hero_cost_from_truth, hero_cost_kept, discarded_hasstats, truth_rows, truth_by_name,
     kw_order_violations, so_stat_changed, so_stat_skipped, unplayable_cost_set) = truth
    rarity_guard_bad, _rarity_truth_n, _rarity_truth_skip_n = rarity_guard[:3]
    (rarity_truth_filled, rarity_src_truth, rarity_src_table, rarity_src_source,
     orphan_rarity_key) = rarity_guard[3:]

    by_type = {}
    for c in doc["cards"]:
        by_type[c["type"]] = by_type.get(c["type"], 0) + 1
    by_rarity = {}
    for c in doc["cards"]:
        k = c["rarity"] or "(空)"
        by_rarity[k] = by_rarity.get(k, 0) + 1

    print(f"源卡数      {total}")
    print(f"收录        {doc['count']}    " + "  ".join(f"{k}={v}" for k, v in sorted(by_type.items())))
    print(f"跳过        {len(skipped)}")
    for name, why in skipped[:5]:
        print(f"            · {name}: {why}")
    if len(skipped) > 5:
        print(f"            … 其余 {len(skipped) - 5} 张同理")

    print(f"\n稀有度       " + "  ".join(f"{k}={v}" for k, v in sorted(by_rarity.items())))
    print(f"  用宝石表定档 {len(filled)} 张（宝石表是权威来源，只要它在表里就用它）")
    for name, old, new, how in filled[:4]:
        print(f"            · {name}: {old or '(空)'} → {new}  [{how}]")
    if len(filled) > 4:
        print(f"            … 其余 {len(filled) - 4} 张同理")
    fuzzy = [f for f in filled if f[3].startswith("模糊")]
    if fuzzy:
        print(f"  其中靠**模糊匹配**的 {len(fuzzy)} 张（卡名拼写有出入，建议抽查）：")
        for name, old, new, how in fuzzy:
            print(f"            · {name} → {new}  [{how}]")
    if still_missing:
        print(f"  ⚠️ 宝石表里也没有的 {len(still_missing)} 张：" + "、".join(still_missing[:6]))

    # 🔴 **`A2067`（2026-10-19）：读数分两栏 —— 只有 `name` 的是【改名】，不计入数值修正。**
    #    原来把 `Imotekh the Stormlord` / `Orikan the Diviner` 两条改名并进了「数值修正 29 张」。
    _VALUE_KEYS = ("cost", "attack", "health", "ranged")
    stat_value = [(nm, fx, old) for nm, fx, old in stat_fixed if any(k in fx for k in _VALUE_KEYS)]
    stat_rename = [(nm, fx, old) for nm, fx, old in stat_fixed if not any(k in fx for k in _VALUE_KEYS)]
    print(f"\n数值修正     {len(stat_value)} 张（OCR 读错/漏读，逐张对过卡面 —— 见 `STAT_FIXES`）")
    for nm, fx, _old in stat_value:
        print(f"            · {nm}: " + "、".join(f"{k}={v}" for k, v in fx.items()))
    if stat_rename:
        print(f"\n改名         {len(stat_rename)} 张（`STAT_FIXES` 里只带 `name`、"
              "`cost/attack/health/ranged` **一个都没改** —— ⛔ 不是数值修正）")
        for nm, _fx, _old in stat_rename:
            print(f"            · {_old} → {nm}")

    # 卡面逐张核对（2026-09-13）—— 字段 / 关键词两列，依据是 1118 张卡图的独立抄录
    # 🔴 **2026-10-15（`A2034`）：本行的【文案】改过，`n_sub` / `n_kw` 两个表达式一个字都没动。**
    #    为什么要改：这两列的**读数口径本来就不一样**，而原句把两列一起写成「源：卡图逐张抄录」，
    #    会把 `关键词` 那一列读成「转录改了几处」——
    #      · `字段`（= `subtype`）= 第 ③ 层（`cardface_fixes`）**盖掉 ② OCR** 的处数。
    #        ⚠️ `subtype` 那一列第 ① 层（真值 `CardTraits`）**不参与**（`merge_truth_traits` 只动
    #        `keywords`）⇒ 这一列**就是**「转录改了几处」，口径没变。
    #      · `关键词` = 第 ③ 层与「**① 真值铺底之后的** ② OCR」**不一致的卡数** ——
    #        `build()` 里 `entry["keywords"]` 在进第 ③ 层之前**已经是** `_kw_filled`
    #        （见那处的 `"keywords": _kw_filled`，以及上面那段 `A1776` 权威序注释）⇒
    #        「第 ① 层只补空补进去的那些 trait」**也会被算成一处修正**
    #        ⇒ 它 **≠「转录改了几处」**，会随第 ① 层覆盖率一起变大。
    #        （⛔ **别抄死数**：2026-10-15 那次的读数 **「命中 124 张 / 追加 139 个 trait ⇒ 221 处」已过期**
    #         —— 现读跑一次看本脚本打出的 `关键词 N 处`；该列会随第 ① 层覆盖率一起变。）
    #    **语义本身没变**（还是「权威序第 ③ 层换掉了整列」）—— 变的只是这行字**不许再被读成转录数**。
    #    出处：`资料/普查产出_第三十一会话/X_卡牌P2.md:259-261`（§六·7）· `A2034`。
    n_sub = sum(1 for x in face_fixed if x[1] == "subtype")
    n_kw  = sum(1 for x in face_fixed if x[1] == "keywords")
    print(f"\n卡面核对修正 字段 {n_sub} 处（= 第③层盖掉②OCR 的处数）"
          f" · 关键词 {n_kw} 处")
    print("            ⚠️ 两列**读数口径不同**：`字段` = 转录改了几处；"
          "`关键词` = 第③层与「**① 真值铺底后的**② OCR」**不一致的卡数** ——"
          " ⛔ **不等于「转录改了几处」**（第①层只补空补进去的也算在里面）"
          "。源：卡图逐张抄录 —— 见 `CARD_FACE_FIXES_SRC`")
    for nm, col, old, new in face_fixed[:6]:
        print(f"            · {nm} [{col}]: {old or '(空)'} → {new}")
    if len(face_fixed) > 6:
        print(f"            … 其余 {len(face_fixed) - 6} 处同理")

    # 🔴 改名后失配（见 `build()` 末尾那段）。**不许静默失败** —— 有孤立键就必须看见。
    if orphan_face_fix:
        print(f"\n🔴 卡面修正表里有 {len(orphan_face_fix)} 个键**没匹配到任何卡**：")
        for _k in orphan_face_fix:
            print(f"            · {_k}")
        print("            ⇒ 这几条修正**这次一条都没生效**。多半是卡改名了（见 `STAT_FIXES`）——"
              "修正表的键必须写成**改名后**的卡名（`load_cardface_fixes` 按 `name` 查）。")
    else:
        print("\n卡面修正表：所有键都匹配上了 ✅")

    # 🔴 **`STAT_FIXES` 的孤立键**（2026-10-15 加）—— 与上面那条「卡面修正表里有 N 个键
    #    **没匹配到任何卡**」**同一个形状、同一套口径**（那条管 `cardface_fixes`，这条管本表）。
    #    为什么必须有：`STAT_FIXES` 也是**按卡名做键**的，且查的是**改名前的名字**
    #    （`build()` 里 `STAT_FIXES.get(name_before_rename)`）⇒ 卡名一改，旧键**静默失配**：
    #    那条数值修正再也不会生效、**零提示**，在卡表里「看起来就像这张卡本来就是这个数」。
    #    🔴 **实测代价**（2026-10-15 现核、当天已删）：`"Simulacrum Imperialis": {"ranged": 4}` —
    #    源表已改叫 `Simulacrum Celestian` ⇒ 这条修正一次都没生效过。
    #    ⚠️ **口径与 `orphan_face_fix` 一致：只出声、不进 `_bad`**（那条也不改退出码）——
    #       ⛔ 所以它**不会**让生成器拒绝落盘；要「红到不落盘」得把 `orphan_stat_fix` 加进 `_bad`，
    #       那是改行为、**不由本笔裁**。
    if orphan_stat_fix:
        print(f"\n🔴 `STAT_FIXES` 表（数值修正 ＋ 改名）里有 {len(orphan_stat_fix)} 个键**没匹配到任何卡**：")
        for _k in orphan_stat_fix:
            print(f"            · {_k}")
        print("            ⇒ 这几条修正**这次一条都没生效**（卡还在的话，数值退回源表的 OCR 值）。"
              "多半是卡改名了 —— `STAT_FIXES` 的键必须写成**改名前的卡名**"
              "（`build()` 里 `STAT_FIXES.get(name_before_rename)`）。")
    else:
        print(f"\n`STAT_FIXES` 表（数值修正 ＋ 改名）：所有 {len(STAT_FIXES)} 个键都匹配上了 ✅")

    # 🔴 **`armor` 一族守卫**（`A2043`，见 `build()` 里那段）—— 与上面两条「孤立键」**同一族**
    #    （都是「表里写着的东西，卡表/源表里其实没生效」），🔴 **但这一条进 `_bad`：
    #    不符就 rc=1、一个字节都不写**（理由见 `OCR_ARMOR_FAMILY` 那段：失效是静默的数值错）。
    if armor_family_bad:
        print(f"\n🔴 `card_stats.json` 的 `armor` 一族有 {len(armor_family_bad)} 处**不符预期**"
              f"（期望正好是 `OCR_ARMOR_FAMILY` 那 {len(OCR_ARMOR_FAMILY)} 张）：")
        for _k in armor_family_bad:
            print(f"            · {_k}")
        print("            ⇒ `armor` 列**只有这几行**有值，而且那些值**全是被 OCR 读错的紫圈值**。"
              "「`ranged_attack=null` 且 `armor≠null`」是 `STAT_FIXES` 那几条的锚点判据 ——"
              "它一失效，那几张卡的 `ranged` 会**静默变 0**。见 `OCR_ARMOR_FAMILY` 那段注释。")
    else:
        print(f"\n`armor` 一族：源表里 `armor` 有值的 {len(OCR_ARMOR_FAMILY)} 行与预期一致 ✅")

    # 🔴 **`art` 一族守卫**（`A2011` + `A2044`，2026-10-18，见 `build()` 里那段）——
    #    判据 = `.../art/<阵营目录>/` 与这一行 `faction` 对得上；**路 B：守卫进 `_bad`、源表不改**。
    if art_guard_bad:
        print(f"\n🔴 `card_stats.json` 的 `art` 一族有 {len(art_guard_bad)} 处**指向别阵营**：")
        for _k in art_guard_bad:
            print(f"            · {_k}")
    else:
        print(f"\n`art` 一族：{len(art_known_bad)} 条已知错配（已裁定不改源表）之外无新增 ✅")
    if art_known_bad:
        for _k in art_known_bad:
            print(f"            · 已知（路 B，⛔ 不改源表）：{_k}")

    # 🔴 **稀有度守卫**（`A2044` / `A2062`，见 `build()` 末尾那三条谓词）—— **①②进 `_bad`**：
    #    ① 「`rarity` 空串、但别名还能在宝石表查到」= 改名导致的**静默丢失**（`SOR24` 那次就是这个）；
    #    ② 「与游戏侧真值 `cardRarity` 不一致」= 拿到的是**错**的档位 —— ⚠️ **`A2062` 起它是层序哨兵**。
    #    ⚠️ 这两条**必须红到不落盘** —— 它们治的正是「变了、却没有任何东西出声」（`SOR24` 那天）。
    #    🔴 **`A2062` 新增的「这一列的值从哪来」读数**（**只出声、不进 `_bad`**）——
    #    第 1 层（真值 `cardRarity`）落地之后，**残余**只有「表命中」与「吃源表原值」两种。
    print(f"\n稀有度来源   ①真值 `cardRarity`（第 1 层）**{len(rarity_src_truth)}** 张"
          f" · ②宝石表/两张补表 **{len(rarity_src_table)}** 张"
          f" · ③**吃 `card_stats.json` 原值** **{len(rarity_src_source)}** 张"
          f"（另有 {_rarity_truth_skip_n} 张快照 `cardRarity` 是 `0`/缺字段 ⇒ 按「无值」跳过）")
    if rarity_truth_filled:
        _acc = [x for x in rarity_truth_filled
                if RARITY_TRUTH_ACCEPTED.get((x[0], x[1])) == (x[2], x[3])]
        _unacc = [x for x in rarity_truth_filled if x not in _acc]
        if _acc:
            print(f"            ✅ 第 1 层改掉 {len(_acc)} 张 —— **已核过的快照差异**（放行，见 `RARITY_TRUTH_ACCEPTED`）：")
            for _cid, _nm, _old, _new in _acc[:10]:
                print(f"            · {_cid}/{_nm}：`{_old or '(空)'}` → `{_new}`")
        if _unacc:
            print(f"            ⚠️ 第 1 层**改掉了** {len(_unacc)} 张的值（预期 0 —— "
                  "`A2062` 实测 1005/1005 一致）：")
            for _cid, _nm, _old, _new in _unacc[:10]:
                print(f"            · {_cid}/{_nm}：`{_old or '(空)'}` → `{_new}`")
    if rarity_src_source:
        print(f"            ⚠️ 这 {len(rarity_src_source)} 张**既没真值、也没表命中** ⇒ 稀有度直接吃了"
              " `card_stats.json` 的 `rarity` 列（**OCR 读出来的**）：")
        for _cid, _nm, _v in rarity_src_source:
            print(f"            · {_cid}/{_nm} = `{_v}`")
        print("            ⇒ 要判「这张该是什么档」，得**开 PnP 成品卡图看底部那颗菱形宝石**"
              "（铁律 7）—— 别拿源表那个值当判据。")
    if rarity_guard_bad:
        print(f"\n🔴 稀有度这一层有 {len(rarity_guard_bad)} 处**对不上**：")
        for _k in rarity_guard_bad:
            print(f"            · {_k}")
        print("            ⇒ ①：稀有度是**按卡名 join** 出来的（宝石表 + `RARITY_UNLISTED` /"
              " `RARITY_BY_FACTION`），卡名一改就**静默失配**。")
        print("            ⇒ 修法（①）：在 `RARITY_BY_FACTION` 里补一条 **(阵营, 现名) → 值** 的特判，"
              "值照抄宝石表那一行 —— 写法见那张表末尾 2026-10-15 那条（`SOR24` 就是这么修的）。")
        print("            ⇒ 修法（②）：两边**必有一边过时**，别猜 —— 先核原版卡 SO 快照那一行，"
              "再决定改卡池还是记一条例外。"
              "⚠️ **`A2062` 起第 1 层就是真值** ⇒ ② 报红只可能是「层序被动了」（见 `build()` 那段）。")
    else:
        print(f"\n稀有度这一层：①别名漏 **0** 张 ✅ · ②与游戏侧真值一致 ✅"
              f"（比过 {_rarity_truth_n} 张；另有 {_rarity_truth_skip_n} 张快照 `cardRarity` 是 `0`/缺字段，"
              "按「无值」跳过 —— 见 `build()` 里那段）")
    # 🔴 **`A2064` ② 的孤立键守卫**（只出声、不进 `_bad`，口径同 `orphan_face_fix` / `orphan_stat_fix`）
    if orphan_rarity_key:
        print(f"\n🔴 稀有度的两张补表里有 {len(orphan_rarity_key)} 个键**没匹配到任何卡**：")
        for _k in orphan_rarity_key:
            print(f"            · {_k}")
        print("            ⇒ 与 `cardface_fixes` / `STAT_FIXES` 的孤立键**同一族的病**（表按卡名做键、"
              "卡名一改就变死键）。修法：把键改成**卡池里的现名**（值照抄原值），或删掉这条。")
    else:
        print(f"            ✅ 两张补表的孤立键：0 个"
              f"（`RARITY_UNLISTED` {len(RARITY_UNLISTED)} 键 · `RARITY_BY_FACTION` {len(RARITY_BY_FACTION)} 键，"
              "**全部命中池内卡**）")

    # 🔴 **同卡两键值不一致**（`A1620`，2026-10-12）—— 与上面那条「孤立键」同一族：都是
    #    「**表里写着的修正，卡表里其实没生效**」。区别只在失效方式：
    #      · 孤立键 = 键**没匹配到任何卡**（多半是卡改名了）；
    #      · 这一条 = 键匹配上了、**但同一张卡另一条键把它盖掉了**（阵营键赢）。
    #    ⛔ **不许静默**：两键的写法都对（裸卡名 / `阵营/卡名` 都是本表合法的键），
    #      错的是**它们对同一张卡给了不同的值** —— 这时候**必有一条是过期的**，人来判该留哪条。
    if key_conflicts:
        print(f"\n🔴 卡面修正表里有 {len(key_conflicts)} 处**同卡两键值不一致**"
              f"（阵营键赢、裸键那一份【没生效】）：")
        for _fac, _nm, _col, _bare, _facv in key_conflicts:
            print(f"            · {_fac} / {_nm} 的 `{_col}`：")
            print(f"                裸卡名键 {_nm!r}      → {_bare!r}")
            print(f"                阵营键   {_fac}/{_nm!r} → {_facv!r}   ← 实际生效的是这个")
        print("            ⇒ 这两条**必有一条过期**：卡名的键是给「所有同名卡」用的、阵营键是给这一张用的。")
        print("              判「谁对」只能开 PnP 成品卡图逐张读（铁律 7）—— 定下来之后"
              "**把另一条改成同一个值**（或删掉冗余的那条）。")
    else:
        print("\n同卡两键一致性 ✅（裸卡名键与 `阵营/卡名` 键没有对同一列给出不同的值）")

    # 🔴 **裸卡名键的卡名必须在全池唯一**（`A1633`，2026-10-12）—— 与上面两条**同一族**：
    #    「表里写着的修正，卡表里其实没生效」。那两条管**失效方式**，这条管**错位方式**。
    #    为什么：`load_cardface_fixes` 把**不带 `/` 的键**按**卡名**查 ⇒ 一个裸键是**给所有同名卡**用的。
    #    而卡池里本来就有**同名跨阵营**的卡（`gen_cards_engine.py` 的 `_seen_names` 就是按
    #    `(阵营, 归一化卡名)` 判重的 ⇒ **同名不同阵营是两张不同的卡**）。
    #    ⇒ 一个裸键只可能承载**其中一张**的值，其余同名卡就**静默套上了别人那张的效果**
    #      （卡面不打 `*`、日志不报、脚本照样打印「命中」）。
    #    🔴 **实测代价**（2026-10-12 修前）：`keywords['Terminator Champion']` 是 `BL44` 的
    #    `['Armour 1']`，而 `EC33` 没有自己的阵营键 ⇒ 它**少了卡面印着的 `Cruelty`**。
    #    同一批还有 `desc`/`keywords['Terminator']`（EC22 的值）、`keywords['Maulerfiend']`（BL46 的值）。
    #    ⇒ 判据（**结构上不可能同时满足**）：**裸键的卡名在全池的出现次数必须是 1**；
    #      >1 就报 🔴 并让退出码非 0（然后由人把它拆成 `阵营/卡名` 两条）。
    #    ⚠️ **本判据红不一定是新错** —— `工具/gen_cardface_fixes.py` 的 `keywords`/`subtype` 两列是
    #      **按裸卡名算出来的**（`fixes.setdefault(name, ...)`，先到先得）⇒ 它一重跑就会把同名卡的
    #      裸键写回来。那时这一条变红 = **正确行为**（那两列的权威值必须先落到 `_manual_keywords`）。
    _ff_all = load_cardface_fixes()[0]
    _ncount = {}
    for _c in doc["cards"]:
        _ncount[_c["name"]] = _ncount.get(_c["name"], 0) + 1
    bare_ambiguous = sorted(_k for _k in _ff_all if _ncount.get(_k, 0) > 1)
    if bare_ambiguous:
        print(f"\n🔴 卡面修正表里有 {len(bare_ambiguous)} 个**裸卡名键**用在【同名卡】上"
              f"（一个裸键服务了多张卡 ⇒ 值只可能是其中一张的）：")
        for _k in bare_ambiguous:
            print(f"            · {_k!r}：全池 {_ncount[_k]} 张同名卡，"
                  f"裸键值 = {_ff_all[_k]!r}")
        print("            ⇒ 判据：**裸键的卡名必须在全池唯一**（同名卡一律写 `阵营/卡名`）。"
              "判「谁的值是谁的」只能开 PnP 成品卡图逐张读（铁律 7）。")
        print("            ⚠️ 这两列（`keywords`/`subtype`）是 `gen_cardface_fixes.py` **按裸卡名算**的 ——"
              "权威值要同时写进 `_manual_keywords` / `_manual_subtype`（脚本最后应用、手工赢）。")
    else:
        print("\n裸卡名键唯一性 ✅（每个裸键的卡名在全池只出现一次）")

    zh_n = sum(1 for c in doc["cards"] if c.get("nameZh"))
    zh_d = sum(1 for c in doc["cards"] if c.get("descZh"))
    missing = doc["count"] - zh_n
    print(f"\n中文名       {zh_n}/{doc['count']}   中文效果 {zh_d}/{doc['count']}"
          f"   （缺 {missing} 张，卡面回英文、不静默）")

    # 🆕 2026-10-14（`A1801`）：**中文卡名换官方**（来源见 `OFFICIAL_LOC_SRC`）—— 逐档如实报。
    #    ⛔ 不许把「官方表里没有这张卡」写成「换过了」：查不到的那批（**2026-10-19 现读 0 张**）仍是我们自译，**要看得见**。
    if off_stats:
        print(f"\n官方中文名   `Card_Name/*` {off_stats['terms']} 条（可用 {off_stats['ok']}）"
              f" → 两级命中 {len(off_hit) + len(off_same)}/{doc['count']} 张"
              f"（其中 id 直查 {sum(1 for x in off_hit + off_same if x[-1] == 'id')}"
              f" · 英文名桥 {sum(1 for x in off_hit + off_same if x[-1] == 'en')}）")
        print(f"            与自译**逐字相同**（不必动）{len(off_same)} 张 · "
              f"**换掉** {len(off_hit)} 张 · 官方表查不到、**保持自译** {len(off_miss)} 张")
        for cid, nm, old, new, how in off_hit[:8]:
            print(f"            · {cid:<26} {nm:<30} 「{old or '(空)'}」→「{new}」  [{how}]")
        if len(off_hit) > 8:
            print(f"            … 其余 {len(off_hit) - 8} 张同理")
        if off_miss:
            _mf = {}
            for cid, nm, z in off_miss:
                m = re.match(r"^([A-Za-z]+?)(?:_|[0-9]|$)", cid)
                _mf[m.group(1) if m else cid] = _mf.get(m.group(1) if m else cid, 0) + 1
            print(f"  ⚠️ 官方表（2026 build）里**没有**这 {len(off_miss)} 张卡 ⇒ 卡名仍是我们自译：")
            print("            " + " · ".join(f"{k} {v}" for k, v in sorted(_mf.items(), key=lambda x: -x[1])))
            for cid, nm, z in off_miss[:6]:
                print(f"            · {cid:<26} {nm:<30} 自译「{z or '(空)'}」")
            if len(off_miss) > 6:
                print(f"            … 其余 {len(off_miss) - 6} 张同理")
        for cid, nm, cands in off_conflict:
            print(f"  ⚠️ 同名卡的多条官方候选**值不一致**：{cid} {nm} → "
                  + " · ".join(f"{k}=「{z}」" for k, z in cands))
        for _id, _en, _zh in off_bad:
            print(f"  ⚠️ 官方值不成立已丢弃：`Card_Name/{_id}` = ({_en!r}, {_zh!r}) ⇒ 该卡保持自译")
        # 🔴 `A2050` / `N5`（2026-10-18）：**英文名桥**每一次命中都逐张打出来。
        #    现读 0 次（池内 1127 张全走 id 直查）⇒ 这一块今天不打印；一旦出现，就是
        #    「官方表里这张卡的键不是我们的 id」⇒ 中文名是**按名字猜的**，⛔ 别当判据。
        if off_en_bridge:
            print(f"  🔴 **英文名桥接 {len(off_en_bridge)} 张**（`official_name()` 第 ② 级 = 启发式，"
                  "现读基线 0 ⇒ 出现即为变化，逐张核）：")
            for cid, nm, okey in off_en_bridge[:20]:
                print(f"            · {cid:<34} {nm:<32} → 官方表键 `{okey}`")
            if len(off_en_bridge) > 20:
                print(f"            … 其余 {len(off_en_bridge) - 20} 张同理")
    else:
        print("\n官方中文名   ⚠️ 没读到官方表 ⇒ 卡名仍是我们自译（见 `OFFICIAL_LOC_SRC`）")

    # 🆕 2026-10-18（用户改判）：**英文卡名也换官方显示名**（`name` + `nameZh` 两列一起；
    #    见 `load_official_display_names` 那段的长注释）。⛔ 不许把「官方表里没有这张卡」写成「换过了」。
    if off_names_stats:
        _n_en = sum(1 for _cid, _bits in offname_hit if any(b[0] == "name" for b in _bits))
        _n_zh = sum(1 for _cid, _bits in offname_hit if any(b[0] == "nameZh" for b in _bits))
        print()
        print(f"官方显示名   `Card_Name/*` {off_names_stats['keys']} 条（可用 {off_names_stats['ok']}）"
              f" → 命中 {len(offname_hit) + len(offname_same)}/{doc['count']} 张")
        print(f"            与官方**逐字相同** {len(offname_same)} 张 · **换掉** {len(offname_hit)} 张"
              f"（其中 `name` {_n_en} · `nameZh` {_n_zh}） · 官方表查不到、保持源表名 {len(offname_miss)} 张")
        for _cid, _bits in offname_hit[:10]:
            print(f"            · {_cid:<26} " + " · ".join(f"`{c}`「{o}」→「{n}」" for c, o, n in _bits))
        if len(offname_hit) > 10:
            print(f"            … 其余 {len(offname_hit) - 10} 张同理")
        if offname_miss:
            print("  ⚠️ 官方表里**没有**这些 id ⇒ 保持源表名："
                  + " · ".join(f"{c}={n}" for c, n, _z in offname_miss))
    else:
        print()
        print("官方显示名   ⚠️ 没读到官方表 ⇒ 卡名保持源表写法（见 `OFFICIAL_LOC_SRC`）")

    # 🆕 2026-10-15（P3 相 · `A1763` 数据层）：**官方中文效果文字**逐档如实报（与上面那一段同形）。
    #    ⛔ 不许把「官方表里没有这张卡 / 原表里 en == zh」写成「换过了」—— 那 122 + 181 张仍是我们自译，
    #    **要看得见**（铁律：不许静默失败）。
    if off_desc_stats:
        _dh = off_desc_hit
        print(f"\n官方中文效果 `Card_Description/*` {off_desc_stats['terms']} 条"
              f"（可用 {off_desc_stats['ok']} · 不可用丢弃 {off_desc_stats['bad']} · `_new` 新代生效 {off_desc_stats.get('superseded', 0)} · GUID 键 {off_desc_stats['guid']}）"
              f" → 命中 {len(off_desc_hit) + len(off_desc_same)}/{doc['count']} 张"
              f"（直查我们的 id {sum(1 for x in _dh if x[-1] == 'id')}"
              f" · 走官方名键 {sum(1 for x in _dh if x[-1] == '官方名键')}）")
        print(f"            与自译**逐字相同**（不必动）{len(off_desc_same)} 张 · "
              f"**换掉** {len(off_desc_hit)} 张 · 官方表里查不到 / 原表里 en == zh ⇒ **保持自译** {len(off_desc_miss)} 张")
        for cid, nm, old, new, how in off_desc_hit[:8]:
            print(f"            · {cid:<26} {nm:<30} 「{old or '(空)'}」→「{new}」  [{how}]")
        if len(off_desc_hit) > 8:
            print(f"            … 其余 {len(off_desc_hit) - 8} 张同理")
        if off_desc_miss:
            _mf2 = {}
            for cid, nm, z in off_desc_miss:
                m = re.match(r"^([A-Za-z]+?)(?:_|[0-9]|$)", cid)
                _mf2[m.group(1) if m else cid] = _mf2.get(m.group(1) if m else cid, 0) + 1
            print(f"  ⚠️ 这 {len(off_desc_miss)} 张的 `descZh` 仍是**我们自译**（官方快照比现池旧 / 原表里 en == zh）：")
            print("            " + " · ".join(f"{k} {v}" for k, v in sorted(_mf2.items(), key=lambda x: -x[1])))
            for cid, nm, z in off_desc_miss[:6]:
                print(f"            · {cid:<26} {nm:<30} 自译「{z or '(空)'}」")
            if len(off_desc_miss) > 6:
                print(f"            … 其余 {len(off_desc_miss) - 6} 张同理")
    else:
        print("\n官方中文效果 ⚠️ 没读到官方表 ⇒ `descZh` 全是 `zh_cards.json` 那份自译（见 `OFFICIAL_LOC_SRC`）")

    # 🤖 **`descZh` 三个写手的顺序断言**（人工值必须最终生效）—— 见 `build()` 里那张序表。
    #    违例 ⇒ 退出码 1、一个字节都不写（同 `_bad`）。没违例时把**实际生效的条数**打出来
    #    （它是「那一层真的接上了」的读数：应为人工表里命中到卡的那几条）。
    print(f"\n`descZh` 层序  断言：`cardface_fixes.descZh`（人工、最高序）最终生效 "
          f"**{_man_desc_ok}/{_man_desc_total}** 条 · 违例 **{len(desc_zh_order_violations)}** 处"
          + (" ✅" if not desc_zh_order_violations else " 🔴"))
    if desc_zh_order_violations:
        for cid, nm, want, got in desc_zh_order_violations[:8]:
            print(f"            🔴 {cid} {nm}：人工值「{want}」被盖成「{got}」")
        print("            ⇒ 层序排错了（官方层必须在人工层**之前**）—— 见 `build()` 里那张序表")

    # 督军后手那张防御卡（`A1070` 第 ③ 档）—— 原版真值，来源见 `DEFENCE_SRC` 那段
    print(f"\n督军防御卡   字段 `goesSecond` 落在 **{_gs_n} 张督军**上（全池督军 {_gs_hero} 张）"
          f"· 真值覆盖 {len(def_report)} 个阵营")
    for rid, rname, cid, cname, how, nw in def_report:
        flag = "" if rid == cid else "  ⚠️ 我们的 id 不是原版的"
        print(f"            · {rid:<7} {rname:<22} → {cid:<22} {cname:<22} [{how}·{nw} 个督军]{flag}")
    if _gs_missing_fac:
        print(f"  ⚠️ 真值里没有这些阵营的督军（原版那份快照**根本没有这个字段**）⇒ "
              f"`goesSecond` 空着，引擎第 ③ 档会**出声 + 不给卡**：{'、'.join(_gs_missing_fac)}")

    # 卡能不能进卡组（`A1798`）—— 原版真值，来源见 `INV_SRC` 那段
    _inv_of = {}
    for c in doc["cards"]:
        _inv_of[c["id"]] = (c.get("inventoryOptions", -1), c.get("showInCollectionOptions", -1))
    _n_ok = sum(1 for v in _inv_of.values() if v[0] >= 0)
    _dist = {}
    for v in _inv_of.values():
        _dist[v[0]] = _dist.get(v[0], 0) + 1
    # 原版 `CardDeck.CanAddCard` 把 hero / 防御卡**先路由走**（`CanAddHero` / `CanAddDefensive`）
    # ⇒ 那两个字段对它们不生效 —— 这条路由也写进 `DeckRules.CannotAddToDeck`，两处只此一条判据。
    _blocked = [c["id"] for c in doc["cards"]
                if c["type"] not in ("hero", "defence")
                and _inv_of[c["id"]][0] >= 0 and (_inv_of[c["id"]][0] != 0 or _inv_of[c["id"]][1] != 5)]
    print(f"\n卡组准入     字段 `inventoryOptions`/`showInCollectionOptions` 接上真值 "
          f"**{_n_ok}/{doc['count']}** 张"
          f"· inv 分布 " + " · ".join(f"{k if k >= 0 else '-1(未知)'}={v}" for k, v in sorted(
              _dist.items(), key=lambda kv: -kv[0])))
    print(f"             ⇒ `DeckRules.CannotAddToDeck` 拦下 **{len(_blocked)}** 张"
          f"（已排除 hero / 防御卡 —— 原版把它们路由给 `CanAddHero`/`CanAddDefensive`）")
    if inv_unknown:
        _byfac = {}
        for cid, nm, fac in inv_unknown:
            _byfac.setdefault(fac or "(无阵营)", []).append(nm)
        print(f"  ⚠️ 真值里查不到这 **{len(inv_unknown)}** 张卡（快照 2025-07-30、比现池旧）"
              f"⇒ 两列记 **-1**、引擎**不拦**（⛔ 不猜）："
              + "  ".join(f"{k}={len(v)}" for k, v in sorted(_byfac.items(), key=lambda x: -len(x[1]))))
        print(f"     {INV_SRC} 的 `unknown` 里有逐张清单；真值侧还有 "
              f"{len(json.load(open(INV_SRC, encoding='utf-8')).get('unmatchedTruthRows') or [])} "
              f"行没落进我们卡池（卡池里压根没这些卡）")

    # 先后手（`A1775`）—— 原版真值，**分布要打出来**（实测现池**只有 20 一种**，那正是
    # 「按真值判先后手」在现数据下等价于「平局 ⇒ 掷硬币」的原因，必须让人看见）
    _idist = {}
    for c in doc["cards"]:
        v = c.get("initiative", -1)
        _idist[v] = _idist.get(v, 0) + 1
    print(f"\n先后手       字段 `initiative` 接上真值 **{doc['count'] - len(init_unknown)}/{doc['count']}** 张"
          f"· 分布 " + " · ".join(f"{k if k >= 0 else '-1(未知)'}={v}" for k, v in sorted(
              _idist.items(), key=lambda kv: -kv[0])))
    print("             ⇒ 取值表 `Undefined=0/Low=10/Medium=20/High=30/VeryHigh=40`；"
          "判据 = `RuleEngine/Core/FirstSeatRules.cs`（原版 `SupportMethods.GetPlayerGoesFirstWithInitiative`）")
    if len(_idist) == 1 and -1 not in _idist:
        print("             ⚠️ **只有一种取值** ⇒ 按原版那条链会**永远平局**、落到最后一步"
              "（`MatchData.playerIdGoesFirst` / 掷硬币）—— 这就是现数据下「按真值判先后手」的真实结果，"
              "不是我们没做")
    if init_unknown:
        _byfac = {}
        for cid, nm, fac in init_unknown:
            _byfac.setdefault(fac or "(无阵营)", []).append(nm)
        print(f"  ⚠️ 真值里查不到这 **{len(init_unknown)}** 张卡（快照 2025-07-30、比现池旧）"
              f"⇒ 记 **-1**、判先后手时**不比**（⛔ 不猜）："
              + "  ".join(f"{k}={len(v)}" for k, v in sorted(_byfac.items(), key=lambda x: -len(x[1]))))
        print(f"     {INIT_SRC} 的 `unknown` 段有逐张清单")

    # ================================================================================
    #  🆕 2026-10-15（P2 相 `A1773` / `A1776` / `A1799`）—— **真值落位批的三个读数**
    #  闸（用户 2026-10-15 拍板 · `施工设计 §七 已裁⑥`）= **PnP 印了的取 PnP，PnP 没有的取真值**。
    #  下面每一段都**只报「真值真被用在了哪」**，⛔ 不把「我们保留了自己的值」说成「换了真值」。
    # ================================================================================
    print(f"\n真值快照     卡池 id 能在快照里找到 **{truth_hit}/{doc['count']}** 张"
          f"（其余 {doc['count'] - truth_hit} 张 = 2025-07-30 之后的新卡 ⇒ 所有真值列走缺值，⛔ 不猜）")

    # ---- `A1773`：督军 `cost` —— **唯一一处「真值赢过我们现值」的换值** -----------------
    print(f"\n督军费用     `A1773`：PnP **56/56 不印费用格**（右上角是阵营徽记）⇒ 照真值 `manaCost`"
          f"· 改掉 **{len(hero_cost_from_truth)}** 张（`0` → `2`，与 `项目任务.md:498-516` 的裁决一致）")
    for cid, nm, old, new in hero_cost_from_truth[:5]:
        print(f"            · {cid:<26} {nm:<28} {old} → {new}")
    if len(hero_cost_from_truth) > 5:
        print(f"            … 其余 {len(hero_cost_from_truth) - 5} 张同理")
    if hero_cost_kept:
        _kf = {}
        for cid, nm, fac, cst in hero_cost_kept:
            _kf.setdefault(fac or "(无阵营)", []).append(nm)
        print(f"  ⚠️ 真值里**没有**这 {len(hero_cost_kept)} 张督军（快照比现池旧）⇒ `cost` **保持现值**、⛔ 不猜："
              + "  ".join(f"{k}={len(v)}" for k, v in sorted(_kf.items(), key=lambda x: -len(x[1]))))
        for cid, nm, fac, cst in hero_cost_kept[:4]:
            print(f"            · {cid:<34} {nm:<24} cost={cst}")
        if len(hero_cost_kept) > 4:
            print(f"            … 其余 {len(hero_cost_kept) - 4} 张同理")
    # ---- 🆕 2026-10-18（`W` 笔）：**SO 数值真值覆盖层** --------------------------------------
    #   判据 → `资料/普查产出_1018/R_督军10血成因.md` §1 **结论 A（当前 build 的 SO 就是真值）**。
    #   🔴 与上面 `A1773` **同源、不同列**：`A1773` 管 hero 的 `cost`；本层管**全池** `health` +
    #      **非 hero** 的 `cost` + **全池** `attack`（三段都是同一份 `cards_raw.json`）。
    _so_by = {}
    for _x in so_stat_changed:
        _so_by[_x[3]] = _so_by.get(_x[3], 0) + 1
    print(f"\nSO 覆盖：health **{_so_by.get('health', 0)}** 张 / cost **{_so_by.get('cost', 0)}** 张"
          f" / attack **{_so_by.get('attack', 0)}** 张（合计改 **{len(so_stat_changed)}** 处）")
    print("             源 = `cards_raw.json` 的 `maxHealth` / `manaCost`（`>=0` 照抄；`-1` 见下一段）/ `meleeAttack`；"
          "判据 → `资料/普查产出_1018/R_督军10血成因.md` §1 结论 A")
    for cid, nm, ty, col, old, new in so_stat_changed:
        print(f"            · {cid:<34} {nm:<30} [{col}] {old} → {new}")
    # ---- 🔴 2026-10-19：`manaCost = -1` = 原版「不可直接打出」哨兵 ⇒ `cost = -1` ----------------
    print(f"\n不可直接打出  `manaCost=-1` 的卡 **{len(unplayable_cost_set)}** 张 ⇒ `cost = -1`"
          "（原版哨兵；这不是缺值，⛔ 别拿 0 顶）：")
    for cid, nm, ty, old, sonm in unplayable_cost_set:
        print(f"            · {cid:<34} {nm:<30} [{ty}] {old} → -1   （SO 行名 = `{sonm}`）")
    if not unplayable_cost_set:
        print("            （0 张 —— 🧨 若这里空、而池里确实有 `Dark Pact of *`，说明 SO 真值层没跑到）")
    if so_stat_skipped:
        print(f"  ⚠️ 有 **{len(so_stat_skipped)}** 处**没敢覆盖**（缺值 / 非法负值；`manaCost=-1` 不在此列，见上）⇒ 保持原值：")
        for cid, nm, ty, col, cur, sov, sonm in so_stat_skipped:
            _why = (f"（SO 行名 = `{sonm}`，与我们的卡不是同一张）" if sonm and sonm != nm else "")
            print(f"            · {cid:<34} {nm:<26} [{col}] 我们 {cur} / SO {sov}{_why}")
    print("             ⚠️ `ranged` / `desc` / `keywords` / `art` 一律**没动**（本层只管这三列）")

    # ---- `A1776`：`keywords` 的「真值铺底」那一层 -------------------------------------
    print(f"\n关键词铺底   `A1776`：真值 `CardTraits` **只补空**（已裁③ 权威序第 1 层）"
          f"· 命中 **{len(kw_truth_filled)}** 张 · 追加 **{sum(len(x[2]) for x in kw_truth_filled)}** 个 trait")
    # 已裁③ 点名要的**断言**（三条，全池逐卡跑；非空 ⇒ 退出码 1、一个字节都不写）
    if kw_order_violations:
        print(f"  🔴 **`keywords` 权威序的三条断言有 {len(kw_order_violations)} 处违例**"
              f"（ⓐ 只补空 · ⓑ OCR 条逐字原样 · ⓒ 真值每条有归宿）—— **退出码 1、不落盘**：")
        for cid, nm, m in kw_order_violations[:10]:
            print(f"            · {cid:<28} {nm:<26} {m}")
        if len(kw_order_violations) > 10:
            print(f"            … 其余 {len(kw_order_violations) - 10} 处同理")
    else:
        print("             断言（已裁③ 那三条）：ⓐ 只补空 · ⓑ OCR 条逐字原样 · ⓒ 真值每条有归宿"
              " —— **全池 {n} 张 0 违例 ✅**".format(n=doc["count"]))
    _td = {}
    for cid, nm, added in kw_truth_filled:
        for a in added:
            _td[a] = _td.get(a, 0) + 1
    if _td:
        print("            " + " · ".join(f"{k}={v}" for k, v in sorted(_td.items(), key=lambda x: -x[1])))
    if kw_truth_skipped:
        _sd = {}
        for cid, nm, sk in kw_truth_skipped:
            for s in sk:
                _sd[s] = _sd.get(s, 0) + 1
        print(f"  ⚠️ **{sum(_sd.values())} 处没进 `keywords`**（{len(kw_truth_skipped)} 张卡）："
              + " · ".join(f"{k}={v}" for k, v in sorted(_sd.items(), key=lambda x: -x[1])))
        print("     ⇒ 理由 = `RuleEngine/Core/CardDef.cs` 的 `Prefixes` **没登记**这些词 ——"
              "写进 `keywords` 会被 `KeywordTable.Normalize` 返回 null 后**静默丢掉**"
              "（本工程红线）。它们**已进 `cardTraits` 列**（那列不喂引擎）。要接进引擎得动 `Core/`，"
              "接完再把那个词从 `TRAIT_ENGINE_UNREGISTERED` 里去掉（**现读该集合为空**）。")
    if kw_truth_wiped:
        # 规则内（③④⑤ 都压过 ①），但**必须看得见** —— 否则「填了 134 个、实际只生效几十个」
        # 这句账谁也复核不了。🔬 抽验过一处：`ASH12 Swooping Hawk` 的 `Rally` 被 ③ 换掉，
        # 2026-10-15 亲开它的 PnP 成品图 —— **卡面只印 `Waystone.` + `Flying.`、没有 `Rally`**
        # ⇒ 那是**快照自己的旧数据**，③ 换掉它是对的（PnP 优先，已裁⑥）。
        _wd = {}
        for cid, nm, gone in kw_truth_wiped:
            for g in gone:
                _wd[g] = _wd.get(g, 0) + 1
        print(f"  ℹ️ 另有 **{sum(_wd.values())} 处**（{len(kw_truth_wiped)} 张）被**更高层**换掉"
              f"（权威序 ③/⑤ 压过 ①，**规则内**）：" + " · ".join(f"{k}={v}" for k, v in sorted(_wd.items(), key=lambda x: -x[1])))

    # ---- `A1776`：`armour` 列 --------------------------------------------------------
    print(f"\n护甲值列     `A1776`：`armour` 列 >0 共 **{len(armour_src_pnp) + len(armour_src_truth)}** 张"
          f"· 其中**取 PnP 那一层** {len(armour_src_pnp)} 张 · **真值兜底** {len(armour_src_truth)} 张")
    _zd = sum(1 for c in doc["cards"] if c.get("armour", 0) == 0)
    print(f"             （另 {_zd} 张 = 0，即卡底没有银盾）")
    if armour_src_truth:
        for cid, nm, v in armour_src_truth[:5]:
            print(f"            · {cid:<26} {nm:<28} 真值兜底 armour={v}")

    # ---- `A1773`：被 `hasStats=false` 整批丢掉那批的**真值归类** ----------------------
    print(f"\n丢弃批归类   `A1773`：`hasStats=false` 整批丢了 **{len(discarded_hasstats)}** 张"
          f"（`STATS_EXCEPTIONS` 里那几张另有白名单，不在其中）⇒ 逐张拿真值归类：")
    _cls = {}
    for nm, fac, ty, ocr in discarded_hasstats:
        why, uid, row = classify_discarded(nm, ocr, truth_by_name, truth_rows)
        _cls.setdefault(why, []).append((nm, fac, uid, row))
    for why, lst in sorted(_cls.items(), key=lambda x: -len(x[1])):
        print(f"            · {why} —— {len(lst)} 张")
    _mis = _cls.get("🔴 疑似误丢(真值是可收藏的卡)") or []
    if _mis:
        print("  🔴 **真值说这些是可收藏的卡、却被我们丢了** —— 逐张（要人判）：")
        for nm, fac, uid, row in _mis:
            print(f"            · {nm:<34} {fac:<16} 真值 id={uid} · m_Name={row.get('m_Name')}"
                  f" · inv={row.get('inventoryOptions')} show={row.get('showInCollectionOptions')}"
                  f" spell={row.get('spellType')} cantRand={row.get('cantBeRandomlyCreated')}")
    else:
        print("  ✅ **真值归类里没有「疑似误丢」** —— 即这批丢掉的一张真卡都没有。")
    for why in ("非收藏(生成/附魔/教程副本)", "进攻/防御卡(另一条线)"):
        for nm, fac, uid, row in (_cls.get(why) or [])[:8]:
            print(f"            · [{why}] {nm:<30} {fac:<16} id={uid} · m_Name={row.get('m_Name')}")
    if _cls.get("未在快照") or any(k.startswith("未在快照(同名") for k in _cls):
        _amb = {k: v for k, v in _cls.items() if k.startswith("未在快照(同名")}
        print(f"  ℹ️ 「未在快照」{sum(len(v) for k, v in _cls.items() if k.startswith('未在快照'))} 张里，"
              f"**同名多候选**的 {sum(len(v) for v in _amb.values())} 张（⛔ 不替它挑）：")
        for k, v in sorted(_amb.items()):
            print(f"            · [{k}] " + " · ".join(x[0] for x in v[:10]))

    # ---- `A1799`：10 个 SO 字段列的覆盖率（**本笔只落值，没有消费者**）------------------
    print(f"\nSO 字段列    `A1799`：10 列已落值（**目前没有任何消费者** —— 机制/接线另立）：")
    _n = len(doc["cards"])
    for _col, _tf, _dflt, _doc in A1799_FIELDS:
        if _dflt == "":
            _hit = sum(1 for c in doc["cards"] if c.get(_col))
        else:
            _hit = sum(1 for c in doc["cards"] if c.get(_col, _dflt) != _dflt)
        print(f"            · {_col:<26} 有值 {_hit:>4}/{_n}"
              + ("   ⚠️ **现池全空**（快照里那批卡不在我们池）" if _hit == 0 else ""))
    print("             ⚠️ 缺值 = `-1`（整数）/ `\"\"`（字符串）—— ⛔ **别把 `-1` 当成 `0`**"
          "（`attackDamageType=0` 是 `Normal`、`useOverDraw=0` 是「不溢出」，都是合法取值）")

    # 稳定 id（2026-09-13 第三十三轮 · 🔴 2026-10-18 `W25`：按 `uniqueId` 直取，名级联降为交叉校验）
    # —— **自造的必须报出来**，不许静默。
    print(f"\n卡 id        直取（`uniqueId` 唯一落位表）{len(id_direct)} 张 · "
          f"名级联兜底 {len(id_cascade)} 张 · 自造 {len(id_made)} 张（共 {doc['count']}）")
    print(f"             交叉校验：{len(id_direct)} 张直取里与名级联**不一致 {len(id_cross_mismatch)} 处**"
          + ("  ✅" if not id_cross_mismatch else "  🔴 见下"))
    for cid, alt, how, nm, fac in id_cross_mismatch[:20]:
        print(f"            🔴 {fac} / {nm}：直取 `{cid}` ≠ 名级联（{how}）`{alt}`"
              " —— 两条路的判据打架，先查名字表，⛔ 别直接选一个")
    if len(id_cross_mismatch) > 20:
        print(f"            … 其余 {len(id_cross_mismatch) - 20} 处同理")
    if id_cascade:
        print(f"  ⚠️ **直取没落位、退回名级联** {len(id_cascade)} 张（落位表缺键 ⇒ 这一批的身份不是「唯一判据」定的）：")
        for cid, nm, fac, how in id_cascade[:10]:
            print(f"            · {cid:<34} {fac} / {nm}（{how}）")
        if len(id_cascade) > 10:
            print(f"            … 其余 {len(id_cascade) - 10} 张")
    if direct_ambiguous:
        print(f"  ℹ️ 直取表里**落位不唯一、没有收**的键 {len(direct_ambiguous)} 个"
              "（同名多候选且没有唯一基准卡 —— 池内卡若靠这些键会落到「名级联兜底」那一档）：")
        for army, nm, uids in direct_ambiguous[:8]:
            print(f"            · 阵营码 {army} · `{nm}` → {', '.join(uids[:4])}"
                  + (" …" if len(uids) > 4 else ""))
    if id_made:
        by_fac = {}
        for cid, nm, fac in id_made:
            by_fac.setdefault(fac or "(无阵营)", []).append(nm)
        print("  自造 id 按阵营：" + "  ".join(
            f"{k}={len(v)}" for k, v in sorted(by_fac.items(), key=lambda x: -len(x[1]))))
        for cid, nm, fac in id_made[:8]:
            print(f"            · {cid:<34} {fac} / {nm}")
        if len(id_made) > 8:
            print(f"            … 其余 {len(id_made) - 8} 张见 {MADE_LIST}"
                  "（⚠️ 该清单**只在本次运行成功落盘时**才写 —— `A1980`②）")
        # 🔴 **`A1980`②（2026-10-18）：清单的写出挪到 `--check` 与 `_bad` 两处闸【之后】**
        #    （见 `main()` 末尾写 `DST` 那一段）。原来它写在 `--check` 分支之前
        #    ⇒ 跑 `--check`（「只对账、不写文件」）也会往 `_tmp_view` 落一份清单；
        #    若本次还 `_bad`（非零退出、卡表一个字节都不写），清单却已经写出去 = 口径不一致。

    # 🔴 **退出码先算、再决定落不落盘**（与 `工具/gen_icon_plan.py` 的 `A1626` 同一条口径）——
    #    「非 0」必须等于「没写文件」。`bare_ambiguous` 是**数据侧的错**，不是提示：
    #    这时写出去的卡表里有一张卡带着**别人那张卡的效果**，宁可让它保持旧值、由人来判。
    # 🔴 **`A2062`（2026-10-16）：加 `rarity_truth_filled` —— 这一条是【灭自证】的那半。**
    #    为什么必须有它：第 1 层（真值 `cardRarity`）落地之后，**下面的守卫 ② 与它是同一个来源**
    #    （拿真值比真值 ⇒ **恒真、自证**，`build()` 那段已就地写明）。⇒ 「第 1 层的值对不对」
    #    **再也没有别的东西看着**。本条的判据**与 ② 结构性不同**：它比的是
    #    「**第 1 层的输出** vs **宝石表 + 两张补表那条链的输出**」（两个**互相独立**的来源），
    #    实测基线 = **0 处**（`A2062` 的「一个值都不会变」）。
    #    ⇒ 一旦不为 0：要么**真值映射表被改错**、要么**快照换了**（两边必有一个过时）——
    #      两种都**要人看**，所以进 `_bad`（⛔ 别静默写出去）；核完照下面那句说明处置。
    _rarity_unexpected = [x for x in rarity_truth_filled
                          if RARITY_TRUTH_ACCEPTED.get((x[0], x[1])) != (x[2], x[3])]
    _bad = bool(bare_ambiguous) or bool(kw_order_violations) or bool(desc_zh_order_violations) \
        or bool(armor_family_bad) or bool(rarity_guard_bad) or bool(_rarity_unexpected) \
        or bool(art_guard_bad)

    # 🆕 2026-10-19（`A1959`/`A1960`）—— `choicesDefinition` 三列的跑完读数。
    #   判据 → `dump.cs:23341 choicesDefinition // 0x2A0` · `BattleManager__GetChoiceFullPool.c:36-49`。
    print(f"\n选牌定义     `choicesDefinition` 落值 **{choices_report['with_choice']}/{doc['count']}** 张"
          f"（`maxTargets > 0` 或 `filterSpecificCards` 非空）")
    print(f"             `targetsAffected==180 && maxTargets>0`（解析层要发 `choosecard` 的那批）："
          f"**{choices_report['pool_180']}** 张")
    if choices_report["pool_180_no_filter"]:
        print(f"  ⚠️ 其中 `filterSpecificCards` **为空** {len(choices_report['pool_180_no_filter'])} 张"
              f"（原版回落 `BattleManager+0x308`，我们**没有**那一格 ⇒ 不猜）："
              + "、".join(choices_report["pool_180_no_filter"]))
    if choices_report["unresolved_refs"]:
        print(f"  🔴 filter 的 PPtr **解不出** {len(choices_report['unresolved_refs'])} 条："
              + "、".join(f"{cid}:{pid}" for cid, pid in choices_report["unresolved_refs"][:6]))
    print(f"             filter 引用里指向**池外卡**的 {choices_report['not_in_pool_refs']} 条"
          f"（变体卡 SO 等 —— 照实落值，消费侧自己判）")
    # 🆕 2026-10-19（`E12`）逐候选动画时间：覆盖率 + **每一个解不出的口都出声**
    _an = anim_report
    print(f"动画时间     `animTimes` 落值 **{_an['cards']}/{doc['count']}** 张 · {_an['rows']} 条候选"
          f"（`CardAnim` SO 的 `vfxDelayTime/startDelay/timeAtStartPos/timeMoving/timeAtEndPos`）")
    if _an["missing_map"] or _an["unresolved_guid"] or _an["not_cardanim"] \
            or _an["missing_asset"] or _an["no_prefab"]:
        print(f"  ⚠️ GUID 表缺失 {_an['missing_map']} · GUID 解不出 {_an['unresolved_guid']} · "
              f"不是 CardAnim {_an['not_cardanim']} · 资产文件缺 {_an['missing_asset']} · "
              f"无 prefab（纯计时占位）{_an['no_prefab']}")
    # 🆕 2026-10-19（`E12` · `A2142`）天赋名 → id 表
    print(f"天赋名表     `talentNameIds` **{talent_name_report['keys']}** 个键"
          f"（键 = `CreatePool.Norm` 归一后的天赋卡名 → 我们池子的 id）")
    if talent_name_report["conflicts"]:
        print(f"  ⚠️ 一个名字指向多张卡、**整条丢掉**（⛔ 不猜）：{talent_name_report['conflicts']} 条 —— "
              + "；".join(f"`{k}` → {ids}" for k, ids in talent_name_report["conflict_rows"][:5]))

    # 🆕 2026-10-21（引擎线 `T03·c` 批 B · 第 0 步）：`triggerPriority` 的覆盖率
    #    ＋ **每一个没落成的口都出声**（本工程红线：静默失败比报错更贵）。
    _tp = trigger_priority_report
    print(f"触发优先级   `triggerPriority` 落值 **{_tp['cards']}/{doc['count']}** 张 · {_tp['rows']} 条"
          f"（原版 `CardAbility.priority`，**默认 1 省略** —— 判据 `W61_引擎线_T03c批B判据.md` §2.1）")
    if _tp["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_tp['no_truth']} 张 —— 它们的 priority **未知**"
              "（≠「没有非默认值」；快照比现池旧，见 `TRUTH_CARDS_SRC`）")
    for _cid, _nm, _i, _tg, _pr in _tp["bad"]:
        print(f"  🔴 非法 priority：{_cid} {_nm} 第 {_i} 条 ability（trigger={_tg}）priority={_pr!r}"
              " ⇒ **丢弃**（原版 `AddAutoActionToQueue` 对非法值 LogError 后丢弃，⛔ 不 clamp）")
    for _cid, _nm, _i, _tg, _pr in _tp["empty_logic"]:
        print(f"  ℹ️ 有 priority 但 `abilityLogic` 为空：{_cid} {_nm} 第 {_i} 条"
              f"（trigger={_tg} priority={_pr}）⇒ 没有动作可排队、**不落**（GOF97 那一类）")
    for _cid, _nm, _tn, _tg, _pr in _tp["unmapped"]:
        print(f"  ℹ️ 引擎没有关键词的触发：{_cid} {_nm} · trigger={_tg} {_tn} · priority={_pr}"
              " ⇒ **不落**（等队列接进 `WhenEvent` 那一层；⛔ 不编造键名）")
    for _cid, _nm, _kw, _prs in _tp["conflict"]:
        print(f"  🔴 同卡同关键词两个不同 priority：{_cid} {_nm} · `{_kw}` = {_prs}"
              " ⇒ **整条丢掉**（一格只能一个值，表达不出）")

    # 🔴🆕 2026-10-23（引擎线 P1·①）：`abilityTriggerPriority`（按**原版 trigger 名**）
    #    —— `WhenEvent` / 回合 / 打出族那一半，与上面那张**按引擎关键词**的列互补、不重叠口径。
    _atp = ability_trigger_priority_report
    _atp_names = []
    for _c in doc["cards"]:
        for _r in (_c.get("abilityTriggerPriority") or []):
            _atp_names.append(f"{_c['id']}·{_r['name']}={_r['priority']}")
    print(f"触发优先级②  `abilityTriggerPriority` 落值 **{_atp['cards']}/{doc['count']}** 张 · {_atp['rows']} 条"
          f"（原版 `AbilityTrigger` 名 → `CardAbility.priority`，**默认 1 省略** —— 判据 `引擎深审A_规则语义.md` P1·1）")
    if _atp_names:
        print("             " + " · ".join(_atp_names))
    if _atp["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_atp['no_truth']} 张 —— 它们的 priority **未知**"
              "（≠「没有非默认值」）")
    for _cid, _nm, _i, _tg, _pr in _atp["bad"]:
        print(f"  🔴 非法 priority：{_cid} {_nm} 第 {_i} 条 ability（trigger={_tg}）priority={_pr!r}"
              " ⇒ **丢弃**（原版 `AddAutoActionToQueue` 对非法值 LogError 后丢弃，⛔ 不 clamp）")
    for _cid, _nm, _i, _tg, _pr in _atp["empty_logic"]:
        print(f"  ℹ️ 有 priority 但 `abilityLogic` 为空：{_cid} {_nm} 第 {_i} 条"
              f"（trigger={_tg} priority={_pr}）⇒ **不落**（GOF97 那一类）")
    if _atp["no_name"]:
        print(f"  ⚠️ `AbilityTrigger` 枚举名查不出来 {len(_atp['no_name'])} 条 ⇒ **不落**"
              f"（枚举桩缺失？见 `ABILITY_TRIGGER_STUB`）")
    for _cid, _nm, _tg, _tn, _prs in _atp["conflict"]:
        print(f"  🔴 同卡同 trigger 两个不同 priority：{_cid} {_nm} · {_tg} {_tn} = {_prs}"
              " ⇒ **整条丢掉**（一格只能一个值，表达不出）")

    # 🆕 2026-10-22（引擎线 `W63` · 数据层）：`abilityContext` 的覆盖率 + 按 context / trigger 的分布
    #    ＋ **每一个没落成的口都出声**（本工程红线：静默失败比报错更贵）。
    _acr = ability_context_report
    _ac_bc = " · ".join(f"{_c}:{_n}" for _c, _n in sorted(_acr["by_context"].items())) or "（无）"
    _ac_bt = " · ".join(f"{_t}:{_n}" for _t, _n in sorted(_acr["by_trigger"].items())) or "（无）"
    print(f"能力上下文   `abilityContext` 落值 **{_acr['cards']}/{doc['count']}** 张 · {_acr['rows']} 条"
          f"（原版 `CardAbility.abilityContext`，**Play(0) 省略** —— 判据 `W63_abilityContext_施工简报.md` §1）")
    print(f"             按 context：{_ac_bc}"
          "（10 Hand · 20 Deck · 30 Cemetery · 40 InPlayOrDying〔含待死〕· 50 InPlayOrCemetery〔不含待死〕）")
    print(f"             按 trigger：{_ac_bt}")
    if _acr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_acr['no_truth']} 张 —— 它们的 abilityContext **未知**"
              "（≠「全是 Play」；快照比现池旧，见 `TRUTH_CARDS_SRC`）")
    for _cid, _nm, _i, _tg, _c in _acr["bad"]:
        print(f"  🔴 非法 abilityContext：{_cid} {_nm} 第 {_i} 条 ability（trigger={_tg}）"
              f"context={_c!r} ⇒ **丢弃**（合法值只有 0/10/20/30/40/50；⛔ 不默认成 Play）")
    if _acr["no_name"]:
        print(f"  ⚠️ `AbilityTrigger` 枚举名查不出来 {len(_acr['no_name'])} 条 ⇒ **不落**"
              f"（枚举桩缺失？见 `ABILITY_TRIGGER_STUB`）")
    for _cid, _nm, _tg, _c in _acr["dup_same"]:
        print(f"  ℹ️ 同 (trigger, context) 重复行**合并**：{_cid} {_nm} · trigger={_tg} context={_c}"
              "（不合并会让消费端当成「同 key 重复」整组丢弃）")
    for _cid, _nm, _tg, _cs in _acr["conflict"]:
        print(f"  🔴 同 trigger 两个不同 abilityContext：{_cid} {_nm} · trigger={_tg} = {_cs}"
              " ⇒ **两行都落**（数据是真的），由消费端按它的规则裁")

    # 🆕 2026-10-22（引擎线 `W64` · 战斗审计 `B-16`）：`unitConditions` 的覆盖率 + 按 cond / role 的分布。
    #   审计原话「28 张卡 conditionsFilter != 0」＝ 只在**ability 级**（不含 `abilityLogic`）数出来的口径，
    #   本脚本按**全量位置**落（含 `abilityLogic` 与卡级计分），条数会更多 —— 读数里两个口径都看得到。
    _ucr = unit_conditions_report
    _uc_bc = " · ".join(f"{_c}:{_n}" for _c, _n in sorted(_ucr["by_cond"].items())) or "（无）"
    _uc_br = " · ".join(f"{_r}:{_n}" for _r, _n in sorted(_ucr["by_role"].items(),
                                                          key=lambda kv: CONDITION_ROLE_ORDER[kv[0]])) or "（无）"
    print(f"结构化条件   `unitConditions` 落值 **{_ucr['cards']}/{doc['count']}** 张 · {_ucr['rows']} 条"
          f"（原版 `TargetCriteria.conditionsFilter`，**none(0) 省略** —— 判据 `W64_兜底审计_战斗.md` B-16）")
    print(f"             按 cond：{_uc_bc}")
    print(f"             按 role：{_uc_br}")
    if _ucr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_ucr['no_truth']} 张 —— 它们有没有条件**未知**"
              "（≠「没有条件」；快照比现池旧，见 `TRUTH_CARDS_SRC`）")
    for _cid, _nm, _ab, _lg, _role, _v, _why in _ucr["bad"]:
        print(f"  🔴 非法 conditionsFilter：{_cid} {_nm} · ab={_ab} lg={_lg} role={_role} "
              f"值={_v!r}（{_why}）⇒ **丢弃该条**（⛔ 不猜枚举名、不默认成 none）")
    # 🆕 v16（`B-16` 缺口）：`count` 行的**被数集合**覆盖 + 坏值/解不出的引用（都是「出声」）。
    print(f"             `count` 行的被数集合 `countSet` 落值 {_ucr['set_rows']} 条"
          "（原版 `BoardCriteria.countCriteria` ＋ `playerTurn`；缺省/坏值 = -1，⛔ 不是 0/false）")
    for _cid, _nm, _f, _v, _why in _ucr["set_bad"]:
        print(f"  🔴 被数集合坏值：{_cid} {_nm} · `{_f}` = {_v!r}（{_why}）⇒ 该格落 **-1**"
              "（⛔ 不丢整行：丢行 = 这一格变成「没有条件」= 静默放行）")
    for _cid, _nm, _f, _pid in _ucr["set_unresolved"]:
        print(f"  ⚠️ 被数集合引用解不出：{_cid} {_nm} · `{_f}` 的 `m_PathID`={_pid!r}"
              " ⇒ **不落那一条**（⛔ 不拿 path_id 当 id）")
    # 🆕 v22（2026-10-24 · 引擎线 `新账-95` #2）：`mult` / `multi` / `avail` / `choice`
    #   四格的「整条 criteria」覆盖读数（键 = role；每条计数 = 落下来的行数）。
    _rs = _ucr.get("role_set") or {}
    if _rs:
        print("             " + " · ".join(f"{_r}:{_rs[_r]}" for _r in ("mult", "multi", "avail", "choice")
                                          if _rs.get(_r))
              + " 行的 `multiplierSet` / `multiSet` / `availSet` / `choiceSet` 已落整条 criteria"
              "（36 格；`playerTurn` 结构上不适用 ⇒ -1）")

    # 🆕 2026-10-23（引擎线 · `B-16` 残余 241/242）：`abilityTriggers` 覆盖读数。
    #   判据 → `ability_triggers_of` 上面那段（`HasAbilityType.c:16-38` 只看 trigger、不看 logic 空不空）。
    _atg = ABILITY_TRIGGER_REPORT
    print(f"触发集合     `abilityTriggers` 落值 **{_atg['cards']}/{doc['count']}** 张 · "
          f"{_atg['triggers']} 个 trigger 值（原版 `cardAbilities[].trigger` 全量集合；"
          f"没 ability 的卡落 `[]`、缺列 = 真值里没这张卡）")
    if _atg["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_atg['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _cid, _nm, _i, _tg in _atg["bad"]:
        print(f"  🔴 `trigger` 非整数：{_cid} {_nm} 第 {_i} 条 ability → {_tg!r} ⇒ 跳过该条")

    # 🆕 2026-10-23（引擎线 · 战斗审计 `B-17`）：`abilityTargets` 覆盖读数。
    _atgt = ABILITY_TARGETS_REPORT
    _ta_cnt = {}
    _ta_cards = {}
    for _c in doc["cards"]:
        for _ab in (_c.get("abilityTargets") or []):
            for _v in (_ab.get("ta") or []):
                _ta_cnt[_v] = _ta_cnt.get(_v, 0) + 1
                _ta_cards.setdefault(_v, 0)
            for _v in set(_ab.get("ta") or []):
                _ta_cards[_v] = _ta_cards.get(_v, 0) + 1
    _ta_top = " · ".join(f"{_v}:{_n}" for _v, _n in sorted(_ta_cnt.items(), key=lambda kv: -kv[1])[:10])
    print(f"目标集合     `abilityTargets` 落值 **{_atgt['cards']}/{doc['count']}** 张 · "
          f"{_atgt['abilities']} 条 ability · {_atgt['logics']} 个 logic 的 `targetsAffected`"
          f"（原版 `AbilityLogic.targetCriteria`；每张卡都落、没 ability 落 `[]`）")
    print(f"             按 targetsAffected（条数）：{_ta_top or '（无）'}")
    if _atgt["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_atgt['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _cid, _nm, _i, _f, _v in _atgt["bad"]:
        print(f"  🔴 `targetsAffected` 非整数：{_cid} {_nm} 第 {_i} 条 ability 的 `{_f}` → {_v!r}"
              " ⇒ 该格落 **-1**（⛔ 不猜成 0 = `any`）")

    # 🆕 2026-10-24（引擎线 `A12`）：`abilityMultipliers` 覆盖读数。
    _amr = ABILITY_MULTIPLIERS_REPORT
    _fm_cnt = {}
    _fm_gt1_cards = set()
    for _c in doc["cards"]:
        for _ab in (_c.get("abilityMultipliers") or []):
            for _v in (_ab.get("fm") or []):
                _fm_cnt[_v] = _fm_cnt.get(_v, 0) + 1
                if _v > 1:
                    _fm_gt1_cards.add(_c.get("id"))
    _fm_top = " · ".join(f"{_v}:{_n}" for _v, _n in sorted(_fm_cnt.items(), key=lambda kv: -kv[1])[:8])
    print(f"倍数         `abilityMultipliers` 落值 **{_amr['cards']}/{doc['count']}** 张 · "
          f"{_amr['abilities']} 条 ability · {_amr['logics']} 个 logic 的 `furtherMultiplier`"
          f"（原版 `AbilityLogic.furtherMultiplier`；0 = 不乘、-1 = 缺键）")
    print(f"             按取值（条数）：{_fm_top or '（无）'} · **>1 的卡 {len(_fm_gt1_cards)} 张**"
          f"（这些会被乘 ⇒ 消费端必须接）")
    if _amr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_amr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _cid, _nm, _i, _f, _v in _amr["bad"]:
        print(f"  🔴 `furtherMultiplier` 坏值：{_cid} {_nm} 第 {_i} 条 ability 的 `{_f}` → {_v!r}"
              " ⇒ 该格落 **-1**（⛔ 不猜成 0 = 不乘）")

    # 🆕 2026-10-24（v23 · 卡表数据 `D11`）：`activeAbilityLogic` 列的读数。
    #   🔴 `dead_cards` / `dead_rows` **不是 0 也正常**：`hasActiveAbility == 0` 的卡在本 build
    #     一条都不消费（`CardScript__ResolveActiveAbilityPlayed.c:22`）—— 行照落、带 `gate: 0`。
    _aalr = ACTIVE_ABILITY_LOGIC_REPORT
    print(f"主动能力     `activeAbilityLogic` 落值 **{_aalr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_aalr['with_rows']}** 张 · {_aalr['objs']} 个能力对象 · {_aalr['rows']} 行"
          f"（原版 `ResolveActiveAbility` → `PlayAbility(…, 400, 0)`；每张卡都落、没行落 `[]`）")
    print(f"             其中 `gate=0`（`hasActiveAbility=0` = 本 build 死数据）**{_aalr['dead_cards']}** 张 / "
          f"**{_aalr['dead_rows']}** 行 · `fm>0` 的行 **{_aalr['fm_nonzero_rows']}** 行 · "
          f"`alternativeAbility` 有行 **{_aalr['alt_cards']}** 张")
    print(f"             v24 每行真值：`chosenAbility` 缺/坏 **{_aalr['ca_missing']}** 格 · "
          f"`playersAffected != 0` **{_aalr['pa_nonzero']}** 格 · "
          f"`damageCriteria` 读到 **{_aalr['dmg_read']}** 行")
    print("             🔴 本列**零行为变化**：消费端（`CardDef`/`EffectText`/`EffectResolver`）另批做")
    if _aalr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_aalr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _aalr["bad"]:
        print(f"  🔴 `activeAbilityLogic` 坏值：{_bad} ⇒ 坏格落 **-1**（⛔ 不猜 0）")

    # 🆕 2026-10-24（v25 · 执行代理 `E9` · `A2` Q7）：logic / op 级 `targetCriteria` 两列的读数。
    #   「有信息的行」判据 = 36 格里**任一格不是默认**（默认：整数/布尔 `0`、引用列表空）；
    #   `-1`（缺省/坏值）**不算信息**。全默认的 criteria 在原版里等价于「不过滤」
    #   （`AbilityLogic__GetTargets.c:190-192`：`targetsAffected == 0` ⇒ disregards all other filters）。
    def _crit_rows_readings(_rows):
        _info = 0
        _ta = {}
        _ref = 0
        for _r in _rows:
            _cr = _r.get("criteria") or {}
            _v = _cr.get("targetsAffected")
            _ta[_v] = _ta.get(_v, 0) + 1
            _hit = False
            for _k, _x in _cr.items():
                if _k == "playerTurn":
                    continue
                if isinstance(_x, int) and _x not in (0, -1):
                    _hit = True
                elif isinstance(_x, list) and _x:
                    _hit = True
            if _hit:
                _info += 1
            if _cr.get("filterSpecificCards") or _cr.get("excludeEnchantments"):
                _ref += 1
        return _info, _ta, _ref

    def _ta_top(_ta, _n=6):
        return " · ".join(f"{_v}:{_c}" for _v, _c in sorted(_ta.items(), key=lambda kv: -kv[1])[:_n]) or "（无）"

    _aatr = ACTIVE_ABILITY_TARGETS_REPORT
    _aat_rows = [r for _c in doc["cards"] for r in (_c.get("activeAbilityTargets") or [])]
    _aat_info, _aat_ta, _aat_ref = _crit_rows_readings(_aat_rows)
    print(f"目标判据①    `activeAbilityTargets` 落值 **{_aatr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_aatr['with_rows']}** 张 · **{_aatr['rows']}** 行"
          f"（active {_aatr['active_rows']} / alternative {_aatr['alt_rows']}）"
          f"（原版 `activeAbilityLogic[j].targetCriteria` 的 36 格；每张卡都落、没行落 `[]`）")
    print(f"             36 格「有信息」的行 **{_aat_info}** 行（其余 = 全默认 ⇒ 不过滤）· "
          f"`filterSpecificCards`/`excludeEnchantments` 非空 **{_aat_ref}** 行")
    print(f"             按 targetsAffected（行数）：{_ta_top(_aat_ta)}")
    print("             🔴 本列**零行为变化**：消费端（`CardDef`/`EffectText`/`EffectResolver`）另批做")
    if _aatr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_aatr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _aatr["bad"]:
        print(f"  🔴 `activeAbilityTargets` 坏行：{_bad} ⇒ `criteria` 全 -1（⛔ 不丢行）")

    _atcr = ABILITY_TARGET_CRITERIA_REPORT
    _atc_rows = [r for _c in doc["cards"] for r in (_c.get("abilityTargetCriteria") or [])]
    _atc_info, _atc_ta, _atc_ref = _crit_rows_readings(_atc_rows)
    print(f"目标判据②    `abilityTargetCriteria` 落值 **{_atcr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_atcr['with_rows']}** 张 · **{_atcr['rows']}** 行 / {_atcr['abilities']} 条 ability"
          f"（原版 `cardAbilities[].abilityLogic[].targetCriteria` 的 36 格；每张卡都落、没行落 `[]`）")
    print(f"             36 格「有信息」的行 **{_atc_info}** 行（其余 = 全默认 ⇒ 不过滤）· "
          f"`filterSpecificCards`/`excludeEnchantments` 非空 **{_atc_ref}** 行")
    print(f"             按 targetsAffected（行数）：{_ta_top(_atc_ta)}")
    print("             🔴 本列**零行为变化**：消费端（`CardDef`/`EffectText`/`EffectResolver`）另批做")
    if _atcr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_atcr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _atcr["bad"]:
        print(f"  🔴 `abilityTargetCriteria` 坏行：{_bad} ⇒ `criteria` 全 -1（⛔ 不丢行）")

    # 🆕 v26（执行代理 `E2` · `A3-2`）：ability 级 `targetCriteria`（`CardAbility.targetCriteria` `+0x30`）。
    _alcr = ABILITY_LEVEL_CRITERIA_REPORT
    _alc_rows = [r for _c in doc["cards"] for r in (_c.get("abilityLevelCriteria") or [])]
    _alc_info, _alc_ta, _alc_ref = _crit_rows_readings(_alc_rows)
    print(f"目标判据③    `abilityLevelCriteria` 落值 **{_alcr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_alcr['with_rows']}** 张 · **{_alcr['rows']}** 行"
          f"（原版 `CardAbility.targetCriteria`（`+0x30`）的 36 格；每张卡都落、没行落 `[]`）")
    print(f"             36 格「有信息」的行 **{_alc_info}** 行 · "
          f"`filterSpecificCards`/`excludeEnchantments` 非空 **{_alc_ref}** 行 · "
          f"`targetsAffected == 0 (any)` **不落** {_alcr['skipped_noinfo']} 行（原版直接 return 1 = 零信息）")
    print(f"             按 targetsAffected（行数）：{_ta_top(_alc_ta)}")
    print("             消费端 = `CardDef.AbilityLevelCriteriaForTrigger` → `ActionPump.EnqueueAbility` 的触发前闸")
    # 🆕 v29（执行代理 `X6` · 口径乙）：`ca` 的能力种类族名读数 —— 这是「23 张 trigger 0
    #   定位不唯一的卡，那 32 行 criteria 能不能被认回来」的唯一凭据。
    #   ⚠️ 分两个口径打印，⛔ 别混：
    #     · `按家族可判的卡` = 有 level 行、且**这一列本身**认得出（85 行层面）；
    #     · `闸会遇到的判不了` = **只有**「某个 trigger 在同一张卡上出现 ≥2 次」的那些卡
    #       （= 消费端真要按家族认的那批；现读正好 23 张 / 可判 19 / 判不了 4，
    #       与 `资料/待办判据_引擎_1025b.md` §一 一致）。其余「判不了」卡走的是
    #       既有的按 trigger 唯一定位那条路，**不受 `ca` 影响**。
    _gate_cards, _gate_ok, _gate_amb = 0, [], []
    for _c in doc["cards"]:
        _rows = [r for r in (_c.get("abilityLevelCriteria") or [])]
        if not _rows:
            continue
        _tr = {}
        for _a in (_c.get("abilityTargets") or []):
            _t = _a.get("trigger")
            if isinstance(_t, int) and not isinstance(_t, bool):
                _tr[_t] = _tr.get(_t, 0) + 1
        _ambt = {t for t, n in _tr.items() if n >= 2}
        _gr = [r for r in _rows if r.get("trigger") in _ambt or r.get("trigger") == -1]
        if not _gr:
            continue
        _gate_cards += 1
        _cas = [r.get("ca", "") for r in _gr]
        if any(x == "" for x in _cas) or len(set(_cas)) != len(_cas):
            _gate_amb.append(_c["id"])
        else:
            _gate_ok.append(_c["id"])
    _ca_top = " · ".join(f"{k}:{v}" for k, v in sorted(_alcr["ca_family_counts"].items()))
    print(f"             能力族 `ca`（v29）：可判 **{_alcr['ca_single']}** 行 · "
          f"家族不唯一 **{_alcr['ca_mixed']}** 行 · 无 logic **{_alcr['ca_none']}** 行")
    print(f"             闸会遇到的（trigger 重复的卡）**{_gate_cards}** 张 ⇒ 按家族可判 "
          f"**{len(_gate_ok)}** 张 · 判不了 **{len(_gate_amb)}** 张"
          f"（{', '.join(_gate_amb) or '（无）'}）")
    print(f"             族别分布（行数）：{_ca_top or '（无）'}")
    if _alcr["ca_missing"]:
        print(f"  🔴 `abilityLevelCriteria` 的 `ca` 有 {_alcr['ca_missing']} 处 `chosenAbility` 坏值"
              " ⇒ 该行 `ca` 落 `\"\"`（判不了 ⇒ 消费端出声 + 放行）")
    if _alcr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_alcr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _alcr["bad"]:
        print(f"  🔴 `abilityLevelCriteria` 坏行：{_bad} ⇒ 整行**不落**并出声")
    # 🆕 v27（执行代理 `E3`）：ability 级 `actingCardCriteria`（`CardAbility.actingCardCriteria` `+0x28`）。
    _aacr = ABILITY_ACTING_CRITERIA_REPORT
    _aac_rows = [r for _c in doc["cards"] for r in (_c.get("abilityActingCriteria") or [])]
    _aac_info, _aac_ta, _aac_ref = _crit_rows_readings(_aac_rows)
    print(f"目标判据④    `abilityActingCriteria` 落值 **{_aacr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_aacr['with_rows']}** 张 · **{_aacr['rows']}** 行"
          f"（原版 `CardAbility.actingCardCriteria`（`+0x28`）的 36 格；每张卡都落、没行落 `[]`）")
    print(f"             36 格「有信息」的行 **{_aac_info}** 行 · "
          f"`filterSpecificCards`/`excludeEnchantments` 非空 **{_aac_ref}** 行 · "
          f"`targetsAffected == 0 (any)` **不落** {_aacr['skipped_noinfo']} 行（原版直接 return 1 = 零信息）")
    print(f"             按 targetsAffected（行数）：{_ta_top(_aac_ta)}")
    print("             消费端 = `CardDef.ActingLevelCriteriaForTrigger` → `ActionPump.EnqueueAbility` 的**第一道闸**")
    if _aacr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_aacr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _aacr["bad"]:
        print(f"  🔴 `abilityActingCriteria` 坏行：{_bad} ⇒ 整行**不落**并出声")
    # 🆕 v28（执行代理 `E4`）：`ActiveAbility.targetCriteria`（`+0x38`，对象级目标闸）。
    _aapr = ACTIVE_ABILITY_PICK_CRITERIA_REPORT
    _aap_rows = [r for _c in doc["cards"] for r in (_c.get("activeAbilityPickCriteria") or [])]
    _aap_info, _aap_ta, _aap_ref = _crit_rows_readings(_aap_rows)
    print(f"目标判据⑤    `activeAbilityPickCriteria` 落值 **{_aapr['cards']}/{doc['count']}** 张 · "
          f"有行的 **{_aapr['with_rows']}** 张 · **{_aapr['rows']}** 行"
          f"（原版 `ActiveAbility.targetCriteria`（`+0x38`）的 36 格；每张卡都落、没行落 `[]`）")
    print(f"             36 格「有信息」的行 **{_aap_info}** 行 · "
          f"`filterSpecificCards`/`excludeEnchantments` 非空 **{_aap_ref}** 行 · "
          f"`targetsAffected == 0 (any)` **不落** {_aapr['skipped_noinfo']} 行（原版直接 return 1 = 零信息）")
    print(f"             按 targetsAffected（行数）：{_ta_top(_aap_ta)}")
    print("             消费端 = `CardDef.ActiveAbilityPickCriteriaFor(ab)` → `RuleCore.CanUseAbility` 的目标闸")
    if _aapr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_aapr['no_truth']} 张 —— **不落列**（未知，⛔ 不是空表）")
    for _bad in _aapr["bad"]:
        print(f"  🔴 `activeAbilityPickCriteria` 坏行：{_bad} ⇒ 整行**不落**并出声")
    # 四列共用的 `criteria_set_of` 出声（`set_bad` / `set_unresolved`）—— ⛔ 不许静默吞掉。
    for _name, _rep in (("activeAbilityTargets", _aatr), ("abilityTargetCriteria", _atcr),
                        ("abilityLevelCriteria", _alcr), ("abilityActingCriteria", _aacr),
                        ("activeAbilityPickCriteria", _aapr)):
        for _cid, _nm, _f, _v, _why in _rep["set_bad"]:
            print(f"  🔴 {_name} 的 criteria 坏值：{_cid} {_nm} · `{_f}` = {_v!r}（{_why}）⇒ 该格落 **-1**")
        for _cid, _nm, _f, _pid in _rep["set_unresolved"]:
            print(f"  ⚠️ {_name} 的 criteria 引用解不出：{_cid} {_nm} · `{_f}` 的 `m_PathID`={_pid!r}"
                  " ⇒ **不落那一条**（⛔ 不拿 path_id 当 id）")

    # 🔴 v25 自检：两列的 `criteria` 必须**恰好这 37 个键、且键序固定**
    #   （`playerTurn` + 36 格）。少一个键 ⇒ `JsonUtility` 那边读成 `-99`（缺键哨兵）=
    #   消费端把「未知」当「默认」或反之，而**本条是全仓唯一能在写盘前挡住它的闸**。
    _crit_expected = (["playerTurn"] + list(COUNT_SET_INT_FIELDS)
                      + list(COUNT_SET_BOOL_FIELDS) + list(COUNT_SET_REF_FIELDS))
    _crit_key_bad = 0
    _crit_rows_all = 0
    for _c in doc["cards"]:
        for _k in ("activeAbilityTargets", "abilityTargetCriteria", "abilityLevelCriteria",
                   "abilityActingCriteria", "activeAbilityPickCriteria"):
            for _r in (_c.get(_k) or []):
                _crit_rows_all += 1
                if list((_r.get("criteria") or {}).keys()) != _crit_expected:
                    _crit_key_bad += 1
    if _crit_key_bad:
        print(f"  🔴 **v25 criteria 键集自检失败 {_crit_key_bad}/{_crit_rows_all} 行** —— "
              "键必须是 `playerTurn` + 36 格（`COUNT_SET_*` 的顺序）⇒ **一个字节都不写**")
        _bad = True
    else:
        print(f"v25 自检     criteria 键集：**{_crit_rows_all} 行 × 37 键**（`playerTurn` + 36 格）全部一致 ✅")

    # 🆕 2026-10-23（v19 · 引擎线 `新账-97` ③/⑤）：`spellType` 列的读数。
    #   🔴 消费端要的两档是 **60 SecretOrder** 与 **120 Quest**（附魔层那两条条件）——
    #   它们**不在当前快照里**的话必须当面说清（那样 240/250 只能继续返回 null）。
    _st_cnt = {}
    _st_unknown = 0
    for _c in doc["cards"]:
        _v = _c.get("spellType", -1)
        if _v is None or _v < 0:
            _st_unknown += 1
            continue
        _st_cnt[_v] = _st_cnt.get(_v, 0) + 1
    _st_top = " · ".join(f"{_v}:{_n}" for _v, _n in sorted(_st_cnt.items()) if _v != 0)
    print(f"卡牌类型     `spellType` 落值 **{doc['count'] - _st_unknown}/{doc['count']}** 张 · "
          f"未知 {_st_unknown} 张（原版 `RawCardScript.spellType`；0 = Normal）")
    print(f"             非 0 的取值：{_st_top or '（无）'}")
    for _need, _nm in ((60, "SecretOrder"), (120, "Quest")):
        if _st_cnt.get(_need, 0) == 0:
            print(f"  ⚠️ `spellType == {_need}`（{_nm}）**池内 0 张** ⇒ `UnitConditions` 的 "
                  f"{240 if _need == 60 else 250} 仍判不了（`ConditionEval` 返回 null，⛔ 不拿 false 顶）")

    # 🆕 2026-10-24（v21 · 引擎线 `A20` · 第三批 ①/②）：主动能力分支闸 / 装填位 / 任务位的读数。
    #   🔴 六列全是「只在非默认时才落」⇒ 这条摘要就是「今天有没有卡走到」的唯一凭据：
    #      这几个数**恒为 0 不是 bug**（`needsLoading` / `_quest` / `questTimes` 真值全 0）。
    _agr = ABILITY_GATE_REPORT
    print(f"能力闸       `hasActiveAbility=1` **{_agr['has_active']}** 张 · "
          f"`hasAlternativeAbility=1` **{_agr['alt']}** 张 · "
          f"`needsLoading` 非 0 {_agr['needs_loading']} 处 · "
          f"`quest`/`questTimes` 非 0 {_agr['quest']} 处")
    print(f"             非默认 `normalAbilityBoardCriteria` **{_agr['criteria']}** 张"
          "（原版 `BoardAnalysis.MeetsCriteria`；缺席 = `targetsAffected = any` 的恒真默认值）")
    if _agr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_agr['no_truth']} 张 —— 六列**都不落**（未知，⛔ 不是 0）")
    for _bad in _agr["bad"]:
        print(f"  🔴 能力闸坏值：{_bad}")

    # 🆕 2026-10-21（引擎线 `T03·b`）：开局加成三列 + `skipCardDraw` 生产列的读数。    #   三列/一列都**只在有值时才落** ⇒ 这条摘要就是「今天有没有卡走到」的唯一凭据。
    _im = initial_modifier_report
    print("开局加成     "
          + " · ".join(f"`{c}` {_im[c]['cards']} 张" for c in INITIAL_MODIFIER_COLS)
          + "（原版 `RawCardScript.initial*Modifier`；0 = 不加不减 ⇒ **不落列**）")
    _sdr = skip_draw_report
    print(f"抽牌封锁     `skipDraws` 落值 **{_sdr['cards']}/{doc['count']}** 张 · {_sdr['rows']} 条"
          f"（原版 `handBuff.cardEffect.buffType == skipCardDraw(7)`；0 条 = 休眠机制）")
    if _sdr["no_truth"]:
        print(f"  ⚠️ 真值快照里没有的卡 {_sdr['no_truth']} 张 —— 它们有没有这条效果**未知**")
    for _cid, _nm, _tn, _tg, _i, _j in _sdr["unmapped"]:
        print(f"  ℹ️ 触发时机在我们引擎里没有关键词：{_cid} {_nm} 第 {_i} 条 ability 第 {_j} 条 logic"
              f"（trigger={_tg} {_tn}）⇒ **不落**（⛔ 不编造键名）")
    for _cid, _nm, _u, _i, _j in _sdr["bad_uses"]:
        print(f"  ⚠️ `numberOfUses` 非法：{_cid} {_nm} 第 {_i} 条 ability 第 {_j} 条 logic "
              f"uses={_u!r} ⇒ 按 **1** 收")

    if args.check:
        if os.path.exists(DST):
            old = json.load(open(DST, encoding="utf-8"))
            # 🔴 2026-10-19（`E12`）：原来只比 `count` / `cards` —— 顶层的**新键**（`version`、
            #   `talentNameIds` …）加了之后 `--check` 照样报「一致 ✅」⇒ 新表可以静默不落盘。
            #   ⇒ 顶层这几格一起比（`source`/`note` 是长文案，故意不比：改注释不该让数据表失效）。
            same = (old.get("count") == doc["count"]
                    and old.get("version") == doc.get("version")
                    and old.get("cards") == doc["cards"]
                    and old.get("talentNameIds") == doc.get("talentNameIds"))
            print(f"\n--check：现有 {DST} "
                  f"{'一致 ✅' if (same and not _bad) else '**已过期** ❌（重跑时加 `--write` 落盘）'}")
            return 0 if (same and not _bad) else 1
        print(f"\n--check：{DST} 不存在")
        return 1

    if _bad:
        _why = []
        if bare_ambiguous:
            _why.append("「裸卡名键用在同名卡上」")
        if kw_order_violations:
            _why.append(f"「`keywords` 权威序三条断言 {len(kw_order_violations)} 处违例」")
        if desc_zh_order_violations:
            _why.append(f"「`descZh` 层序断言 {len(desc_zh_order_violations)} 处违例"
                        "（人工值被官方层盖掉）」")
        if armor_family_bad:
            _why.append(f"「`card_stats.json` 的 `armor` 一族 {len(armor_family_bad)} 处不符预期」")
        if art_guard_bad:
            _why.append(f"「`card_stats.json` 的 `art` 一族 {len(art_guard_bad)} 处指向别阵营」")
        if rarity_guard_bad:
            _why.append(f"「稀有度这一层 {len(rarity_guard_bad)} 处对不上"
                        "（别名能查到却落成空串 / 与游戏侧真值不一致）」")
        if _rarity_unexpected:
            _why.append(f"「稀有度第 1 层（真值 `cardRarity`）改掉了 {len(_rarity_unexpected)} 张的值"
                        "（基线 0 —— 见 `_bad` 那段：要么真值映射表被改错、要么快照换了，"
                        "两边必有一个过时，核完再决定改哪边）」")
        print(f"\n🔴 退出码 1、**一个字节都不写**（{' ＋ '.join(_why)} 没解决之前"
              "不落盘 —— 见 `A1626` 那条口径）")
        return 1

    # 🔴 **`A1648`（2026-10-19）：不带 `--write` = 干跑** —— 以前「不带参数就落盘」，
    #    本意只想看读数的一次运行**无条件重写**了 `cards_engine.json`。
    #    现在落盘必须显式 `--write`；干跑照样把上面所有读数打完，然后返回 0、一个字节都不写。
    if not args.write:
        print(f"\n干跑（未写盘）：要落盘请显式加 `--write` —— {DST}")
        return 0

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, "w", encoding="utf-8") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
    print(f"\n写出 {DST}  ({os.path.getsize(DST) / 1024:.0f} KB)")
    # 🔴 **`A1980`②（2026-10-18）：自造 id 清单** —— 写在「`--check` 已返回、`_bad` 也已返回」之后，
    #    与卡表**同生共死**（只对账不落盘 / 出错不落盘，两种情况下都不该留下这份清单）。
    if id_made:
        os.makedirs(os.path.dirname(MADE_LIST), exist_ok=True)
        with open(MADE_LIST, "w", encoding="utf-8") as f:
            f.write("# 自造 id 的卡（原版 id 表里没有 / 同名候选不唯一）—— 由 gen_cards_engine.py 重写\n")
            f.write("# 卡名 | 阵营 | 我们发的 id\n")
            for cid, nm, fac in sorted(id_made, key=lambda x: (x[2], x[1])):
                f.write(f"{nm}\t{fac}\t{cid}\n")
        print(f"写出 {MADE_LIST}（自造 id {len(id_made)} 张）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
