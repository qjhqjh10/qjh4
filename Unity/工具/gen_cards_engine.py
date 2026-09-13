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
    # UM 那张 Bladeguard Veteran 旧表叫 `Bladeguard Lieutenant`、`rarity` 是**空串**，
    # 落到默认 `common`；宝石实读**绿 = rare**（与 DA 那张同名卡一致）。
    ("Ultramarines", "Bladeguard Veteran"): "rare",
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
    "Haarken Worldclaimer": {"ranged": 2},    # Chaos/1督军/Warpforge_3_Haarken-Worldclaimer.png：紫圆 2
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
    "Lhaska Szenari":       {"ranged": 2},    # Genestealer Cult/1督军/Warpforge_01_Lhaska-Szenari.png：紫圆 2
    "Nemesor Zahndrekh":    {"ranged": 2},    # Necron/1督军/Warpforge_3_Nemesor-Zahndrekh.png：紫圆 2
    "Neurothrope":          {"ranged": 2},    # Tyranid/1督军/Warpforge_1_Neurothrope.png：紫圆 2
    # 一处 `cost`：**这张是我亲自开图复验的**（子代理先报，我另开一次确认）——
    # Aeldari/3部队/Warpforge_44_Wraithknight.png 右上角蓝色六边形清清楚楚是 `10`，
    # 同卡面还写 `Armour 2` / 紫圆 6 / 绿框 12，与表里那三项都对得上。
    "Wraithknight":         {"cost": 10},
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
    """读卡面修正表 → {卡名: {"subtype":…, "keywords":[…]}}。文件不在就返回空表（不静默改数）。"""
    if not os.path.exists(CARD_FACE_FIXES_SRC):
        print("⚠️ 找不到卡面修正表 %s —— 这次**不做**字段/关键词修正" % CARD_FACE_FIXES_SRC)
        return {}
    with open(CARD_FACE_FIXES_SRC, encoding="utf-8") as f:
        raw = json.load(f)
    out = {}
    for k, v in (raw.get("subtype") or {}).items():
        out.setdefault(k, {})["subtype"] = v
    for k, v in (raw.get("keywords") or {}).items():
        out.setdefault(k, {})["keywords"] = v
    return out

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
    face_fixes = load_cardface_fixes()      # 卡面逐张核对修正（2026-09-13），见那张表的长注释
    face_fixed = []                         # 被修正过的卡（跑完打出来给人看）
    cards, skipped = [], []
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

        entry = {
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
        _ff = face_fixes.get(name)
        if _ff:
            if "subtype" in _ff and _ff["subtype"] != entry["subtype"]:
                face_fixed.append((name, "subtype", entry["subtype"], _ff["subtype"]))
                entry["subtype"] = _ff["subtype"]
            if "keywords" in _ff and _ff["keywords"] != entry["keywords"]:
                face_fixed.append((name, "keywords",
                                   " ".join(entry["keywords"]), " ".join(_ff["keywords"])))
                entry["keywords"] = _ff["keywords"]
        fix_own_armour(entry)          # 补「卡自己的护甲」—— 源数据漏了一批，见那个函数
        # 中文（有才写：没翻译的卡面自动回英文，不写空串进来白占体积）
        for k, v in zh.get(name, {}).items():
            entry[k] = v
        cards.append(entry)

    return {
        "version": 5,          # v5: subtype/keywords 过了「卡面逐张核对」修正（见 CARD_FACE_FIXES_SRC）
        "source": "Unity/数据/游戏数据/card_stats.json（稀有度另取 资料/卡牌数据表/卡牌宝石稀有度_0824.md；"
                  "中文另取 数据/卡牌翻译/zh_cards.json；"
                  "subtype/keywords 另按 数据/游戏数据/cardface_fixes.json 修正）",
        "note": "由 工具/gen_cards_engine.py 生成，不要手改。"
                "改数据请改 card_stats.json / 数据/卡牌翻译/zh_cards.json / 数据/游戏数据/cardface_fixes.json 后重跑。"
                "nameZh / descZh 是可选字段 —— 没有的卡面回英文；"
                "subtype 是**卡面那行橙字**（部队卡=兵种 Infantry/Vehicle/…；防御卡=Defence；"
                "督军=Warlord；有些计策=Dark Pact/Overlord Power/Combat Elixir…），空串=卡面没这行。",
        "count": len(cards),
        "cards": cards,
    }, skipped, len(raw), filled, still_missing, stat_fixed, face_fixed


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

    doc, skipped, total, filled, still_missing, stat_fixed, face_fixed = build()

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
