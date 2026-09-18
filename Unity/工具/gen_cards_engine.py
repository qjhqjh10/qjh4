#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_cards_engine.py — 卡牌原始数据 → 规则引擎用的精简卡表

为什么要有这一道：
  1. `card_stats.json` 有 978 KB，大头是 `art` / `voice` / `ocrSrc` / `face` 这些
     **引擎完全用不到的路径字符串**。精简后只剩 273 KB，能进构建、能进仓库。
  2. 原始数据里数值字段**大量为 null**（`"cost": null` 有 124 处）。
     C# 侧 `JsonUtility` 遇到显式 null 不可靠 —— 在这里一次性归一化掉，
     生成的 JSON 里每个字段都有确定类型，`CardDatabase.cs` 就不用写防御代码了。

输出：`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`
（放 `Resources/` 是为了运行时 `Resources.Load<TextAsset>("cards_engine")` 能取到 ——
  `.json` 进 Assets 默认就按 TextAsset 导入，232 KB 进构建可以接受）

用法：
    "D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/gen_cards_engine.py
    ... --check     只对账，不写文件
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

# 🔴 **卡改过名之后，中文表里还留着旧名** ⇒ 按新名查落空 ⇒ 卡面印英文名。
#    这张表 = 「引擎卡名 → 中文表里的键」。**只填「同一张卡改了名」的**，
#    值必须是**中文表里那个旧名**（出处：`Unity/数据/本地化/i18n/zh_CN.csv`），
#    ⚠️ **2026-09-18 更正**：原来这里写「**官方本地化**里那个旧名」—— **那个说法站不住**。
#       实测原版客户端**根本没有中文表**（246,807 个文件扫中文串零命中 · 84 个 bundle 无本地化包 ·
#       I2 表在远端 CCD）⇒ 这张 csv **是我们自己译的**，不是官方产物。
#       它仍然可以当**我们内部的命名基准**用（避免同一个名字两处不一致），但**不是「原版怎么说」的判据** ——
#       要判原版语义得走卡图（铁律 7）或反编译。
#    翻译本身不抄进代码（免得两处各写一份，迟早不一致）。
#    出处：`资料/PnP卡图_逐张对账_0915.md` §四·F 的改名清单（每条都带三处独立证据）。
ZH_NAME_ALIAS = {
    # 引擎名（新）          中文表里的键（旧）        我们的译名（⚠️ 不是「官方中文」，见 :38 的更正）
    "Dogmata":                "Sister Dogmata",        # 教义修女      （zh_CN.csv:2252）
    "Fire Warrior Marksman":  "Fire Warrior Sniper",   # 火氏狙击手    （zh_CN.csv:1307）
    "Sons of Morkai Eliminator": "Morkai Eliminator",  # 莫凯歼击者    （zh_CN.csv:1885）
    "Land Raider":            "Land Rider",            # 兰德骑手      （zh_CN.csv:1774；旧的 `Land Rider` 少个 a）
    # 🆕 2026-09-18：这两张的**旧键是美术文件名尾段、不是卡名**（见 `STAT_FIXES` 里那段）——
    "Imotekh the Stormlord":  "stormlord",             # 伊摩泰克·风暴领主
    "Orikan the Diviner":     "Diviner",               # 占卜师
    # 🆕 2026-09-18：这两张的**旧键是美术文件名尾段、不是卡名**（见 `STAT_FIXES` 里那段）——
    "Imotekh the Stormlord":  "stormlord",             # 伊摩泰克·风暴领主
    "Orikan the Diviner":     "Diviner",               # 占卜师
}

# ⚠️ 这一类**不能靠别名**：卡名里的**数字变了**（`2nd` → `1st`），
#    别名会直接把旧名字的译文端过来（会印成「第二连终结者」，**错**）。
#    这里写**推导值**，并在注释里标清楚是**我们推的、不是官方原文**。
ZH_NAME_OVERRIDE = {
    # ⚠️ 这一条是**官方译文**，只是我们的 `zh_cards.json` 里**没有这个键**（别名接不上）⇒ 直接写值。
    #    出处：`Unity/数据/本地化/i18n/zh_CN.csv:830`（`Chosen of the Four,~,四神宠儿`）。
    "Chosen of the Four": "四神宠儿",

    # ⚠️ 这一条是**我们推的，不是官方原文**：官方对 `2nd Company Terminator` 的译文是
    #    「第二连终结者」（zh_CN.csv:465）；我们的卡按 PnP 卡面改成了 **1st** Company
    #    （`Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png`）
    #    ⇒ 机械换号得「第一连终结者」。**没有官方译文可查。**
    "1st Company Terminator": "第一连终结者",
}


# 引擎认识的稀有度取值（`special` = 橙红宝石，防御/药剂/特殊卡）
RARITIES = ("common", "rare", "epic", "legendary", "special")

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
    ("Sororitas", "Dogmata"): "rare",                  # 卡面绿 = rare（引擎原 legendary；SOR 77 张里唯一一处）
    ("AstraMilitarum", "Leman Russ"): "rare",          # 卡面绿 = rare（引擎原 legendary；卡面印 `Leman Russ Tank`）

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
    # UM 那张 Bladeguard Veteran 旧表叫 `Bladeguard Lieutenant`、`rarity` 是**空串**。
    # 🔴 **2026-09-15 更正**：原来这里写「宝石实读**绿 = rare**（与 DA 那张同名卡一致）」—— **不成立**。
    #    按同一套取样法（底部菱形宝石区 x402-486/y985-1110 取 hue 中位）重测：
    #      · `Ultramarines/3部队/Warpforge_34_Bladeguard-Veteran.png` → **hue 177.7 = 钢蓝 = common**
    #      · `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`   → hue 120.0 = 绿 = **rare**（这条对）
    #    两张**同名但不同色**，旧注把 UM 那张当成跟 DA 一样了（UM/AM 那路子代理独立读数也是 common）。
    ("Ultramarines", "Bladeguard Veteran"): "common",
}

# 三份 0824 表**没收录**的 9 张（`Dark Angels/6秘密` 5 张 + `Genestealer Cult/6破坏卡` 4 张，
# 都是手机翻拍 IMG_*.jpg，没进那套 OCR 流水线）→ 宝石表里查不到，只能另立一表。
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

    # 🆕 2026-09-15 PnP 对账：这三张宝石表里没有、引擎 `rarity` 是空串，卡面实测都是 **绿 = rare**
    "Commissar Elan": "rare",          # Astra Militarum/1督军/Warpforge_01_Commissar-Denkler.png
    "Medic Scion": "rare",             # Astra Militarum/3部队/…（卡面印 `Scion Medic`）
    "1st Company Terminator": "rare",  # Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png

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
    "Simulacrum Imperialis": "epic",                       # Sorotitas/3部队/Warpforge_30_Simulacrum-Celestian.png（紫；卡面印 `Simulacrum Celestian`）
}

# 数值修正 —— **OCR 读错/漏读**的卡。每条都对着卡面核过，出处写在后面。
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

    # ---- 🆕 2026-09-15 PnP 逐张对账批（`资料/PnP卡图_逐张对账_0915.md` §四·F）----
    # **卡名按卡面印的改**。三处独立证据一致：PnP 文件名、源表 `ocrName`、`card_ids.json` 的
    # 原版 ID 表（`SW45 = Land Raider` · `SW36 = Sons of Morkai Eliminator` ·
    # `UM83 = 1st Company Terminator` · `SOR32 = Dogmata`）。
    # ⚠️ 改名会**连带换 id**（id 按新名查原版表）⇒ 立绘也要重导（`import_original_art.py`）。
    "Land Rider":             {"name": "Land Raider"},
    "Morkai Eliminator":      {"name": "Sons of Morkai Eliminator"},
    "Sister Dogmata":         {"name": "Dogmata"},
    "2nd Company Terminator": {"name": "1st Company Terminator", "ranged": 6},
    #     ↑ 紫圆 6（`Ultramarines/3部队/Warpforge_19_1st-Company-Terminator.png`）
    # 9 张督军的「费用」：卡面**费用槽是空的**（子代理逐张开图核过 9/9；对照卡同槽有蓝色六边形）
    # ⇒ 引擎里都该是 0。剩下 6 张（本表上面已有条目的并进去了）。
    "Njal Stormcaller":       {"cost": 0},
    "War Shaper":             {"cost": 0},
    "Terror of Vardenghast":  {"cost": 0},
    "Tervigon":               {"cost": 0},
    "Varro Tigurius":         {"cost": 0},
    # 一处 `name`：**旧表把卡名抄错了**。核过卡图 + 两边的 desc：
    #   · Ultramarines/3部队/Warpforge_34_Bladeguard-Veteran.png 卡面印的是 **`Bladeguard Veteran`**
    #     （`Armour 1. Vanguard / Codex: Heals 3`，6/6/2/1/6，Infantry）
    #   · 旧表里那张叫 `Bladeguard Lieutenant` 的，**desc 与它逐字一致**、费用同为 6
    #   · 而同阵营另有一张 `Bladeguard Ancient`（7/7/5/1/7）**是另一张卡，别混**
    # ⇒ 是**同一个东西被写成了两个名字**（旧表名错），**不是少了一张卡**。
    #   ⚠️ 顺带修三处（同一行里一起给）：
    #     · `ranged` 0 是漏读 → 2
    #     · 卡面兵种行印的是 `Infantry`，旧表那一格**是空的** → 补上
    #     · 稀有度：卡图底部宝石是**绿**（对照 `Dark Angels/3部队/Warpforge_26_Bladeguard-Veteran.png`
    #       那颗也是绿、我们记 `rare`）⇒ `RARITY_BY_FACTION` 里也补一条
    "Bladeguard Lieutenant": {"name": "Bladeguard Veteran", "ranged": 2},
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
    #   ⚠️ **同一类错可能还有**：另有 9 张督军的 cost 非 0（2×4 / 3×3 / 1×2）—— **本轮没核**。
    #      要查就照上面这个法子：**开图看右上角有没有蓝色费用六边形**。
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
    #   `card_ids.json` 的原版 ID 表用的就是我们现在这个名字（`AM1=Commissar Elan`），
    #   PnP 印的是**当年重印的写法**。改名只认「三处证据一致」那一条，见下面。
    "Commissar Elan":        {"ranged": 2},
    "Acolyte Iconward":      {"ranged": 2},
    "Simulacrum Imperialis": {"ranged": 4},
    # ✅ **不改名**（2026-09-16 查清并结案）。原来差点按上面那条「三处证据一致」的规矩改成
    #    `Veteran Stormboy` —— 那三处（PnP 文件名 / `ocrName` / `card_ids.json`）**全是从 PnP 派生的**，
    #    不是三个独立来源。**独立于 PnP 的游戏侧证据全部指向 `Veteran Flyboy`**：
    #      · `数据/本地化/i18n/zh_CN.csv:2565` `Veteran Flyboy,~,飞行老兵`
    #      · 同表 `:3407` **`Talent: Veteran Flyboy`** · `:4596`/`:5790` **`Companion 1/2: Veteran Flyboy`**
    #        —— 这两族是**引擎拿卡名拼出来的模板串**，拼出来的就是 Flyboy；表里**没有** `Veteran Stormboy`
    #      · 同表 `:2656` 把**我们这张卡的效果原文**当本地化串收着：
    #        `"When you deploy a Stormboy, give it Flank."`（那个 Stormboy 是**另一张卡** `GOF9`）
    #      · 抽卡包 `bundle_draftpacks_assets_all/MonoBehaviour/Flyboyz.json`（`packId: GOFF_Flyboyz`）
    #  ⇒ PnP 印的 `Veteran Stormboy` 是**当年另一种写法**，不是原版卡名。**别再翻案。**
    # ⚠️ 推论：上面那条「三处证据一致就改」的规矩**只在有第四个独立来源背书时才安全** ——
    #    已经按 PnP 改掉的那 4 张（`Land Raider` / `1st Company Terminator` / `Dogmata` /
    #    `Sons of Morkai Eliminator`）**没查过游戏侧**，真要较真得照这个法子各查一遍。
    "Veteran Flyboy":        {"ranged": 4},
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
    为什么加：修正表原来**只按卡名查**，而卡池里有 **5 组同名跨阵营**的卡
    （`Terminator` / `Terminator Champion` / `Aggressor` / `Maulerfiend` / `Bladeguard Veteran`），
    于是「改一张会连带改另一张」—— 实测 `Ultramarines/Terminator` 的 desc 被
    `EmperorsChildren/Terminator` 的值串掉了（`资料/PnP卡图_逐张对账_0915.md` §四·B）。
    **同名卡要用带阵营的键**；普通卡照旧用裸卡名。带阵营的**优先**。
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

# 卡牌类型白名单 —— 引擎认识这四种（见 CardDef.Type）
TYPES = ("unit", "tactic", "hero", "defence")


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
# ⇒ 这一层给每张卡发一个**唯一且稳定**的 id。做法是**优先用原版 id**，对不上的自造。
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


def pick_id(cands, faction):
    """从候选里挑**这张阵营**的那一个。挑不出（0 个或多个）返回 None —— **不猜**。"""
    pre = PREFIX_OF.get(faction)
    if not pre:
        return None
    hit = [cid for cid, _ in cands if cid.startswith(pre) and cid[len(pre):].isdigit()]
    return hit[0] if len(hit) == 1 else None


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
    print(f"中文表       {len(out)} 条（源 {doc.get('count')}，冲突 {len(doc.get('conflicts') or [])} 处）")
    return out


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


def build():
    with open(SRC, encoding="utf-8") as f:
        raw = json.load(f)["cards"]

    rarity_lookup = make_rarity_lookup(load_rarity())
    zh = load_zh()
    face_fixes, face_fixes_by_fac = load_cardface_fixes()   # 卡面逐张核对修正（2026-09-13），见那张表的长注释
    # ⚠️ 两张表：裸卡名 / 带阵营 —— **同名卡必须用后者**，见 `load_cardface_fixes` 的注释
    face_fixed = []                         # 被修正过的卡（跑完打出来给人看）
    id_by_name = load_ids()                 # 原版 id（见 `IDS_SRC` 那段长注释）
    id_orig, id_made = [], []               # 用上原版 id 的 / 自造 id 的（跑完打出来给人看）
    cards, skipped = [], []
    _seen_ids = set()                       # 生成的 id 必须两两不同（下面有断言）
    _seen_names = set()   # (阵营, 归一化卡名) —— 判重只在这个粒度上做
    filled, still_missing = [], []
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
        elif rarity not in RARITIES:
            # 宝石表里也没有这张（卡名对不上）—— 原值本身也不合法，才算缺失
            still_missing.append(name)
            rarity = ""
        cost = norm_int(c.get("cost"))
        attack = norm_int(c.get("attack"))
        health = norm_int(c.get("health"))
        ranged = norm_int(c.get("ranged_attack"))
        # 数值修正（OCR 读错/漏读的那几张，见 `STAT_FIXES`）—— **每条都对着卡面核过**
        # ⚠️ **必须用「改名前」的名字查**：`STAT_FIXES` 的键就是旧名（改名那一条的 key 也是旧名）——
        #    查 `name` 的话改过名的卡会**查不到自己的数值修正**（**静默**：名字对了、数没改）。
        fix = STAT_FIXES.get(name_before_rename)
        if fix:
            stat_fixed.append((name, dict(fix)))
            if "cost" in fix: cost = fix["cost"]
            if "attack" in fix: attack = fix["attack"]
            if "health" in fix: health = fix["health"]
            if "ranged" in fix: ranged = fix["ranged"]

        # ---- 稳定 id（2026-09-13 第三十三轮）----
        # **优先用原版 id**：按「归一化卡名」查表，再按**本卡的阵营**在前缀上挑一个。
        # 这一步同时解决了跨阵营重名 —— `Aggressor` / `Terminator` / `Terminator Champion` /
        # `Maulerfiend` 四组同名卡各有各的 id（例：`Terminator Champion` = `BL44` 或 `EC33`）。
        # 挑不出（表里没有这张 / 同名候选 >1 个）就**自造**，并记进 `id_made` 由跑完的报告列出 ——
        # **不许静默**：自造意味着「这张卡没有原版 id」，下游要能一眼看出来。
        faction = norm_str(c.get("faction"))
        cid = pick_id(id_by_name.get(_norm_name(name), []), faction)
        if cid:
            id_orig.append((cid, name, faction))
        else:
            cid = make_id(faction, name)
            id_made.append((cid, name, faction))
        if cid in _seen_ids:
            # id 撞了 = 稳定 id 这件事本身就塌了，**当场炸**，别生成一份带重复 id 的卡表
            sys.exit(f"❌ id 撞车：{cid}（{name} / {faction}）—— 自造规则要改，不能把重复 id 写进卡表")
        _seen_ids.add(cid)

        entry = {
            "id":       cid,
            "name":     name,
            "type":     ctype,
            "cost":     cost,
            "attack":   attack,
            "health":   health,
            "ranged":   ranged,
            "keywords": norm_list(c.get("keywords")),
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
        }
        # ---- 卡面逐张核对修正（2026-09-13）----
        # 上面那两列 `subtype` / `keywords` 都是**从 OCR 那份源表来的**，实测会串列、会掉数值。
        # 这一层用「1118 张逐张看卡图独立抄」的结果盖掉 —— 见 `CARD_FACE_FIXES_SRC` 的长注释。
        # 同名卡：**带阵营的键优先**（没有才退回裸卡名那一条）
        _ff = dict(face_fixes.get(name) or {})
        _ff.update((face_fixes_by_fac.get(entry["faction"]) or {}).get(name) or {})
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
        # 中文（有才写：没翻译的卡面自动回英文，不写空串进来白占体积）
        # 🔴 先查**改名别名**（见 `ZH_NAME_ALIAS` 的注释）：中文表里还留着**改名前**的键，
        #    按新名查会落空 ⇒ 卡面印英文名（实测 6 张，全是刚改名/补录的那批）。
        for k, v in zh.get(ZH_NAME_ALIAS.get(name, name), {}).items():
            entry[k] = v
        # ⚠️ **推导值放在别名之后**：`ZH_NAME_OVERRIDE` 里的卡名带数字变化，
        #    别名端过来的是旧名字的译文（会印错「第二连」），这一层再盖掉。
        if name in ZH_NAME_OVERRIDE:
            entry["nameZh"] = ZH_NAME_OVERRIDE[name]
        # ⚠️ `descZh` 的卡面修正必须**放在中文表之后** —— 否则刚写的又被上面那两行盖回去
        if _ff and "descZh" in _ff and _ff["descZh"] != entry.get("descZh"):
            face_fixed.append((name, "descZh", entry.get("descZh") or "", _ff["descZh"]))
            entry["descZh"] = _ff["descZh"]
        cards.append(entry)

    return {
        "version": 6,          # v6: 每张卡带稳定 id（原版 id 优先，对不上的自造 —— 见 IDS_SRC 那段）
        "source": "Unity/数据/游戏数据/card_stats.json（稀有度另取 资料/卡牌数据表/卡牌宝石稀有度_0824.md；"
                  "中文另取 数据/卡牌翻译/zh_cards.json；"
                  "subtype/keywords 另按 数据/游戏数据/cardface_fixes.json 修正；"
                  "id 另取 数据/游戏数据/card_ids.json，对不上的自造）",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。"
                "改数据请改 card_stats.json / 数据/卡牌翻译/zh_cards.json / 数据/游戏数据/cardface_fixes.json 后重跑。"
                "nameZh / descZh 是可选字段 —— 没有的卡面回英文；"
                "subtype 是**卡面那行橙字**（部队卡=兵种 Infantry/Vehicle/…；防御卡=Defence；"
                "督军=Warlord；有些计策=Dark Pact/Overlord Power/Combat Elixir…），空串=卡面没这行。"
                "id 是**稳定身份**：原版 id 形如 `AM12`（字母前缀+纯数字）；"
                "**自造 id 形如 `AM_Some_Card`（带下划线）** —— 一眼能分出「原版有 id」和「我们补的」。",
        "count": len(cards),
        "cards": cards,
    }, skipped, len(raw), filled, still_missing, stat_fixed, face_fixed, id_orig, id_made


def fix_own_armour(entry):
    """补上「这张卡**自己**的护甲」—— 源数据漏了一批。

    卡面右侧那枚盾里的数字、以及战斗里的减伤，都来自 `Armour N` 关键词。
    但源数据（`card_stats.json` 的 `keywords`，OCR 出来的）**漏了将近一半**：
    1130 张卡里 desc 写了 `Armour N` 的有 98 张，只有 41 张进了 keywords。
    症状：**卡面上印着「护甲 1」，打起来一点护甲都没有**（`UnitState.Armor` 是 0）。

    判据取**窄的**：只有 desc **以 `Armour N` 开头**才算「卡自己的护甲」。
      · `Armour 1. Codex: Gain +1 melee this turn` → 自己的护甲 1 ✓
      · `Gain Armour 2` / `Give Armour 1 to a friendly troop` → 是**效果给的**，不是自己的 ✗
    这样只补该补的，不会把「给别人加护甲」的卡也变成有护甲。
    """
    if not entry.get("desc"):
        return
    m = re.match(r"^\s*Armour (\d+)", entry["desc"], re.I)
    if not m:
        return
    if any(re.search(r"armou?r", k, re.I) for k in entry.get("keywords") or []):
        return
    entry["keywords"] = list(entry.get("keywords") or []) + ["Armour " + m.group(1)]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true", help="只对账，不写文件")
    args = ap.parse_args()

    doc, skipped, total, filled, still_missing, stat_fixed, face_fixed, id_orig, id_made = build()

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

    print(f"\n数值修正     {len(stat_fixed)} 张（OCR 读错/漏读，逐张对过卡面 —— 见 `STAT_FIXES`）")
    for nm, fx in stat_fixed:
        print(f"            · {nm}: " + "、".join(f"{k}={v}" for k, v in fx.items()))

    # 卡面逐张核对（2026-09-13）—— 字段 / 关键词两列，依据是 1118 张卡图的独立抄录
    n_sub = sum(1 for x in face_fixed if x[1] == "subtype")
    n_kw  = sum(1 for x in face_fixed if x[1] == "keywords")
    print(f"\n卡面核对修正 字段 {n_sub} 处 · 关键词 {n_kw} 处"
          f"（源：卡图逐张抄录 —— 见 `CARD_FACE_FIXES_SRC`）")
    for nm, col, old, new in face_fixed[:6]:
        print(f"            · {nm} [{col}]: {old or '(空)'} → {new}")
    if len(face_fixed) > 6:
        print(f"            … 其余 {len(face_fixed) - 6} 处同理")

    zh_n = sum(1 for c in doc["cards"] if c.get("nameZh"))
    zh_d = sum(1 for c in doc["cards"] if c.get("descZh"))
    missing = doc["count"] - zh_n
    print(f"\n中文名       {zh_n}/{doc['count']}   中文效果 {zh_d}/{doc['count']}"
          f"   （缺 {missing} 张，卡面回英文、不静默）")

    # 稳定 id（2026-09-13 第三十三轮）—— **自造的必须报出来**，不许静默
    print(f"\n卡 id        用原版 {len(id_orig)} 张 · 自造 {len(id_made)} 张（共 {doc['count']}）")
    if id_made:
        by_fac = {}
        for cid, nm, fac in id_made:
            by_fac.setdefault(fac or "(无阵营)", []).append(nm)
        print("  自造 id 按阵营：" + "  ".join(
            f"{k}={len(v)}" for k, v in sorted(by_fac.items(), key=lambda x: -len(x[1]))))
        for cid, nm, fac in id_made[:8]:
            print(f"            · {cid:<34} {fac} / {nm}")
        if len(id_made) > 8:
            print(f"            … 其余 {len(id_made) - 8} 张见 {MADE_LIST}")
        os.makedirs(os.path.dirname(MADE_LIST), exist_ok=True)
        with open(MADE_LIST, "w", encoding="utf-8") as f:
            f.write("# 自造 id 的卡（原版 id 表里没有 / 同名候选不唯一）—— 由 gen_cards_engine.py 重写\n")
            f.write("# 卡名 | 阵营 | 我们发的 id\n")
            for cid, nm, fac in sorted(id_made, key=lambda x: (x[2], x[1])):
                f.write(f"{nm}\t{fac}\t{cid}\n")

    if args.check:
        if os.path.exists(DST):
            old = json.load(open(DST, encoding="utf-8"))
            same = old.get("count") == doc["count"] and old.get("cards") == doc["cards"]
            print(f"\n--check：现有 {DST} {'一致 ✅' if same else '**已过期** ❌（重跑不带 --check）'}")
            return 0 if same else 1
        print(f"\n--check：{DST} 不存在")
        return 1

    os.makedirs(os.path.dirname(DST), exist_ok=True)
    with open(DST, "w", encoding="utf-8") as f:
        json.dump(doc, f, ensure_ascii=False, separators=(",", ":"))
    print(f"\n写出 {DST}  ({os.path.getsize(DST) / 1024:.0f} KB)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
