# -*- coding: utf-8 -*-
"""zh_crosscheck.py —— 用**中文卡面文字**当尺子，核**英文解析结果**有没有读错句子。

🔴 **2026-09-16 已知限制（用之前先读这一段）**：
  1. **本脚本读不了新探针格式** —— 它按 **8 空格缩进**认 op，而 `EffectParseProbe` 现在打嵌套 op 用
     **10 空格**（`↳回合内层` / `↳重复内层` / `↳修饰指向`）⇒ **嵌套层它看不见**；
     探针新加的 `**随机抽**` / `死后转给[…]` / `选牌[…]` 三个标记它**一个都没用上**。
  2. **探针快照必须与数据同源** —— `probe_in.txt` / `probe_out.txt` / `cards_engine.json`
     是三次不同时间生成的；数据改过之后旧探针里的那几行**已经不在池里**，
     会掉进 `?` 桶（实测 51 行不可判）。**跑之前先重跑一次探针**。
  3. **它的 A 类启发式噪声很大** —— 2026-09-16 全池 15~17 条 A 类**逐条核完、全是假阳性**
     （成因：探针不带 `CardDef` ⇒ 看不见事件层/天赋/静态改费；探针是**逐句**的 ⇒ 跨句回填不可见；
     「每个」⇒`计数=` 那条判据**本身就是错的**）。
  ⇒ **真正在跑的是自检里那 6 条零假阳性的判据**：`RuleEngineTest.TestZhAgreement`（**J1–J6**）。
     本脚本留着当**人工普查的入口**，**别把它当尺子**（正本见 `资料/普查产出_0916/静默桩家族_0916.md` §四）。

为什么要有它（2026-09-16）：
  解析器**只吃英文 `desc`**（`EffectText` 的所有调用点传的都是 `card.Desc`；`DescZh` 只走显示）。
  英文有歧义而中文没有 —— 同一个 `or` 能表示「**二选一**」（`A or B`）与
  「**条件换数值**」（`A, or B if C`，中文写「…若…则…」），两条英文长得**一模一样**。
  本工程为此吃过亏（`资料/卡牌效果or句_审计.md`），但一直是**逐条的、没人系统性对过账**。

  本脚本把「中文里的结构标记」与「探针里 spec 实际做成了什么」**逐张并排比**，
  把差异分成三类：
    A 类 = **真差异**（中文有 X 结构、spec 里完全没有；或中文是二选一、spec 做成了条件，反向也算）
    B 类 = **可能差异**（中文有标记、spec 里表现形式不同，要人判）
    C 类 = 对上了（只计数）

⚠️ **中文不是独立尺子**：`数据/卡牌翻译/zh_cards.json` 是**我们自己译的**
  （源 = 2026-08-27 那 7 份 `_tmp_zhcards_g*.json`，由手写 T 表产出；术语取自粉丝译规则书；
   官方中文在 remote-only 的 `localization_assets_all.bundle` 里，**本地从来没有过**）。
  它**从英文 desc 译出来** ⇒ 英文读错时它会跟着错；它能抓的是
  「**翻译时读懂了、解析器没读懂**」这一类（这一批恰好不少）。用法见报告 §结论。

用法：
  PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/zh_crosscheck.py
输入：`d:/4/_tmp_view/probe_out.txt`（逐句探针快照，`Editor/EffectParseProbe.cs` 产出）
      `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`desc` / `descZh`）
输出：`d:/4/_tmp_view/zh_crosscheck_0916.md`（正文报告）
      `d:/4/_tmp_view/zh_crosscheck_full.tsv`（全量 A/B 明细，供逐条复查）
"""
import io
import json
import os
import re
import sys

PROBE = "d:/4/_tmp_view/probe_out.txt"
CARDS = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
D2C = "d:/4/_tmp_view/desc_to_cards.tsv"      # **子句 → 卡** 的映射（探针输入是**子句级**的）
OUT_MD = "d:/4/_tmp_view/zh_crosscheck_0916.md"
OUT_TSV = "d:/4/_tmp_view/zh_crosscheck_full.tsv"

# ---------------------------------------------------------------- 探针解析

HEAD_RE = re.compile(r"^【(.*)】$")
CARDLEVEL_RE = re.compile(r"^  卡级 → op (\d+) 条 · 不认识 (\d+) · 半懂 (\d+)")
UNPARSED_RE = re.compile(r"^ {8}✗ 不认识: (.*)$")
PARTIAL_RE = re.compile(r"^ {8}⚠ 半懂: (.*)$")
OPLINE_RE = re.compile(r"^ {8}\S")
TAIL_RE = re.compile(r"^ {16}↳尾句")

# 探针里 op 行上出现的结构特征（出处：`EffectParseProbe.Dump`）
COND_KNOWN_RE = re.compile(r" 条件=(?!⚠|«)([a-z0-9|]+)")
COND_UNKNOWN_RE = re.compile(r" 条件=⚠「([^」]*)」判不了")
ALT_RE = re.compile(r"⇒条件成立时换成 (-?\d+)")
ALT_COND_RE = re.compile(r"｜条件=«([^»]*)»(→([a-z0-9]+)| ⚠)")
INSTEAD_RE = re.compile(r"【替换】")
COUNT_RE = re.compile(r"计数=([a-z0-9]+)")
REPEAT_RE = re.compile(r"×(\d+)次")
TARGET_SEG_RE = re.compile(r"目标\[([^\]]*)\]")
VERB_RE = re.compile(r"^([a-z]+)")
TARGET_FLAG_EACH = " 每个"


def parse_probe(path):
    """→ {desc: {...}}。同一个 desc 出现多次时取并集（探针按 desc 走，池里允许同 desc 多卡）。"""
    blocks = {}
    cur = None
    cur_ops = []
    cur_line = 0

    def flush():
        if cur is None:
            return
        b = blocks.setdefault(cur, {"desc": cur, "ops": [], "unparsed": [], "partial": [],
                                    "opcount": 0, "nblocks": 0, "line": cur_line})
        b["ops"].extend(cur_ops)
        b["nblocks"] += 1

    def newblock():
        nonlocal cur_ops
        cur_ops = []

    for lineno, raw in enumerate(io.open(path, encoding="utf-8"), 1):
        line = raw.rstrip("\n")
        m = HEAD_RE.match(line)
        if m:
            flush()
            cur = m.group(1)
            cur_line = lineno
            newblock()
            continue
        if cur is None:
            continue
        m = CARDLEVEL_RE.match(line)
        if m:
            blocks.setdefault(cur, {"desc": cur, "ops": [], "unparsed": [], "partial": [],
                                    "opcount": 0, "nblocks": 0})["opcount"] = int(m.group(1))
            continue
        m = UNPARSED_RE.match(line)
        if m:
            blocks.setdefault(cur, {"desc": cur, "ops": [], "unparsed": [], "partial": [],
                                    "opcount": 0, "nblocks": 0})["unparsed"] += [
                x.strip() for x in m.group(1).split(" | ")]
            continue
        m = PARTIAL_RE.match(line)
        if m:
            blocks.setdefault(cur, {"desc": cur, "ops": [], "unparsed": [], "partial": [],
                                    "opcount": 0, "nblocks": 0})["partial"] += [
                x.strip() for x in m.group(1).split(" | ")]
            continue
        if TAIL_RE.match(line) and cur_ops:
            cur_ops[-1] += " " + line.strip()
            continue
        if OPLINE_RE.match(line):
            cur_ops.append(line.strip())
            continue
    flush()
    out = {}
    for k, v in blocks.items():
        v["optext"] = "\n".join(v["ops"])
        out[k] = v
    return out


def spec_features(blk):
    """把一张卡的 op 集合摊成结构特征。"""
    txt = blk["optext"]
    verbs = []
    for o in blk["ops"]:
        m = VERB_RE.match(o)
        if m:
            verbs.append(m.group(1))
    # 目标段里去掉「引文」，剩下的才是**标记**（`每个` 这种 flag 不在引号里）
    tflags = []
    for seg in TARGET_SEG_RE.findall(txt):
        tflags.append(re.sub(r"「[^」]*」", "", seg))
    tflags = " ".join(tflags)
    return {
        "optext": txt,
        "verbs": verbs,
        "cond_known": sorted(set(COND_KNOWN_RE.findall(txt))),
        "cond_unknown": COND_UNKNOWN_RE.findall(txt),
        "alt_amt": ALT_RE.findall(txt),
        "alt_cond": ALT_COND_RE.findall(txt),
        "instead": bool(INSTEAD_RE.search(txt)),
        "count": COUNT_RE.findall(txt),
        "each_flag": TARGET_FLAG_EACH in tflags,
        "repeat": REPEAT_RE.findall(txt),
        "choose_one": any(v in ("chooseone", "chooseeffect") for v in verbs),
        "choose_card": "choosecard" in verbs,
        "draw": any(v in ("draw", "drawtype", "drawref") for v in verbs),
        "unparsed_n": len(blk["unparsed"]),
        "partial_n": len(blk["partial"]),
    }


# ---------------------------------------------------------------- 中文标记

ZH_COND = re.compile(r"若|如果|倘若")
ZH_THEN = re.compile(r"则")
# ⚠️ 「每回合 / 每局」是**频率**、「每当」是**事件触发**（= `whenever`），都不是**按数量缩放**，
#    必须排除（实测踩过：`At the end of each turn, deal 1 damage`、`每当你部署一个载具时，
#    此牌费用减少 1 点` 都被误判成 for-each）
ZH_EACH = re.compile(r"每(?![回合局当])[有张点个一名只位]?")
ZH_CHOOSE = re.compile(r"选择|挑选|择一|选一|选其")
ZH_DRAW = re.compile(r"抽")
ZH_REPLACE = re.compile(r"改为|改成")

# 「或」后面跟这些词 = 又一个**效果**（⇒二选一）；否则多半是**名字/类别枚举**（⇒筛选）
# ⚠️ `标记` / `护盾` / `生命` 这类**名词**绝不能放进来（`标记无人机` 会被误判成效果 —— 实测踩过）
ZH_EFFECT_HEAD = re.compile(
    r"^(给予|造成|对|使|获得|抽|部署|治疗|消灭|摧毁|返回|生成|创建|复制|移除|弃|减|增加|"
    r"为|将|让|本回合|复活|复生|召唤|翻倍|眩晕|填满|刷新|弃掉|弃置|攻击|损失|牺牲)")

# 中文有「若/则」而 spec 里没有条件时：spec 的**引文**里若出现英文条件从句头，
# 就说明那句话**被吞进了目标或载荷**（`scan_swallowed_clauses.py` 那条判据的窄化版）
QUOTE_RE = re.compile(r"「([^」]*)」")
EN_COND_HEAD = re.compile(r"(?i)(^|[,;]\s*|\s)(if|when|whenever)\s")

# ---- 「中文说的效果」↔「英文 desc 里到底有没有这个词」----
# 两边**至少有一边错**（Shrineworld：中文「抽 3 张牌」/ 英文「Destroy an enemy unit」，
# 卡面是「Draw 3 cards」⇒ **英文那一侧被手改错了**）。这一族是**数据**差异，不是解析差异。
ZH_EN_PAIRS = [
    ("抽牌", r"抽", r"\b(draw|drew|drawn)\b"),
    ("伤害", r"伤害|造成", r"\bdamage\b"),
    ("部署", r"部署", r"\b(deploy|put in play)"),
    ("治疗", r"治疗", r"\bheal|\brestore"),
    ("摧毁/消灭", r"摧毁|消灭", r"\bdestroy"),
    ("眩晕", r"眩晕", r"\bstun"),
    ("弃牌", r"弃", r"\bdiscard"),
    ("复制", r"复制", r"\bcopy\b"),
    ("护甲", r"护甲|装甲", r"armou?r"),
]
# ⚠️ **不能**进来的一族（实测全是假阳性，2026-09-16）：
#   · `攻击` —— 英文的图标被剥掉后只剩裸 `+1` / 写成 `Ranged` / `[melee]`，都不含 "attack"
#   · `生命` —— 英文写 `heal 1`（不含 health）
#   · `费用` / `侧翼` / `手牌` / `牌库` —— 与中文措辞差得太远
# 这一族**只是「两边至少有一边错」的候选**，判它要去看 PnP 卡面（铁律 7）。


def zh_markers(zh):
    """从中文里抽结构标记。`或` 逐处判后面接的是**效果**还是**名词**。"""
    or_effect, or_noun = [], []
    for m in re.finditer(r"或", zh):
        tail = zh[m.end():m.end() + 10]
        if ZH_EFFECT_HEAD.match(tail):
            or_effect.append(zh[max(0, m.start() - 12):m.end() + 14])
        else:
            or_noun.append(zh[max(0, m.start() - 12):m.end() + 14])
    return {
        "raw": zh,
        "cond": bool(ZH_COND.search(zh)),
        "cond_word": (ZH_COND.search(zh).group(0) if ZH_COND.search(zh) else ""),
        "then": bool(ZH_THEN.search(zh)),
        "rep": bool(ZH_REPLACE.search(zh)),
        "rep_word": (ZH_REPLACE.search(zh).group(0) if ZH_REPLACE.search(zh) else ""),
        "each": [zh[max(0, m.start() - 8):m.start() + 10] for m in ZH_EACH.finditer(zh)],
        "choose": [zh[max(0, m.start() - 8):m.start() + 12] for m in ZH_CHOOSE.finditer(zh)],
        "draw": bool(ZH_DRAW.search(zh)),
        "or_effect": or_effect,
        "or_noun": or_noun,
    }


def name_lines(path, keypat):
    """→ {卡名: 行号}（`keypat` 抓卡名的那一行）。给报告里当**可复查的坐标**用。"""
    m = {}
    try:
        for i, line in enumerate(io.open(path, encoding="utf-8"), 1):
            for mm in keypat.finditer(line):
                m.setdefault(mm.group(1), i)
    except Exception:
        pass
    return m


STATS_LINES = None
FIXES_LINES = None


def ev(name):
    """给出这张卡在各数据文件里的行号（`cards_engine.json` 是**单行**文件，只能给 :1）。"""
    global STATS_LINES, FIXES_LINES
    if STATS_LINES is None:
        STATS_LINES = name_lines("d:/4/Unity/数据/游戏数据/card_stats.json",
                                 re.compile(r'"name":\s*"([^"]+)"'))
        FIXES_LINES = name_lines("d:/4/Unity/数据/游戏数据/cardface_fixes.json",
                                 re.compile(r'"([^"]+)":'))
    out = []
    n = name.split("/")[0]
    if n in FIXES_LINES:
        out.append("cardface_fixes.json:%d" % FIXES_LINES[n])
    if n in STATS_LINES:
        out.append("card_stats.json:%d" % STATS_LINES[n])
    return " · ".join(out) if out else "（没找到行号）"


# ---------------------------------------------------------------- 比对

def classify(zh, sp, en):
    """→ (类, 检查项[], 说明[], 严重度)。A=真差异 B=可能差异 C=对上。

    ⚠️ 每一条判据都踩过假阳性，注释里记着**为什么这么写** —— 改之前先读。
    """
    low_en = en.lower()
    txt = sp["optext"]
    hits = []

    has_cond = bool(sp["cond_known"] or sp["cond_unknown"])
    has_alt = bool(sp["alt_amt"] or sp["alt_cond"])
    cost_cond = any(v in ("costifcontrol", "costwhen") for v in sp["verbs"])
    # `atturn` / `persist` 的正文**存在载荷里、结算期才再解析一遍** ⇒ 探针看不见里面有没有条件
    deferred = any(v in ("atturn", "persist") for v in sp["verbs"])
    # `choosecard` 的 `ChooseAct`（draw/hand/deploy）**探针不打印** —— 但
    # `EffectResolver.DoChooseCard` 的 `case "draw"` 确实会抽上手 ⇒ 「选牌并抽取」是对的
    # `chooseone` 的选项**结算期回解析器再过一遍**（`DoChooseOne`）⇒ 载荷里那串原文是对的
    # 同理：`become` / `eachunitdeal` / `forceattack` 都是**专用动词**，别当没有
    q_swallowed = [q for q in QUOTE_RE.findall(txt) if EN_COND_HEAD.search(q)]

    # --- ① 条件 ---------------------------------------------------------
    if zh["cond"] and not has_cond and not has_alt and not cost_cond:
        if q_swallowed:
            hits.append(("A", "条件被吞", "中文有「%s」，条件从句被吞进引文：「%s」"
                         % (zh["cond_word"], q_swallowed[0]), 1))
        elif deferred:
            hits.append(("B", "条件", "中文有「%s」；条件写在 atturn/persist 载荷里，探针看不见"
                         % zh["cond_word"], 3))
        elif "when" in low_en and " if " not in low_en:
            hits.append(("B", "条件", "中文有「%s」、英文写的是 when（多半是触发不是条件）"
                         % zh["cond_word"], 3))
        else:
            hits.append(("A", "条件缺失", "中文有「%s」但 spec 里没有条件" % zh["cond_word"], 1))
    if has_alt and not zh["cond"] and not zh["then"]:
        hits.append(("A", "条件↔二选一", "spec 做成了「条件换数值」，但中文**没有**「若/则」", 1))

    # --- ② 二选一 -------------------------------------------------------
    if zh["or_effect"] and not (sp["choose_one"] or has_alt or sp["choose_card"]
                                or "become" in sp["verbs"]):
        hits.append(("A", "二选一", "中文有效果的「或」：「%s」，spec 里既没有 choose 也没有 alt"
                     % zh["or_effect"][0], 2))
    if sp["choose_one"] and not zh["or_effect"] and not zh["choose"]:
        hits.append(("B", "二选一", "spec 做成了 chooseone，中文里找不到「或/选择」", 3))

    # --- ③ 按数量缩放 ---------------------------------------------------
    if zh["each"] and not sp["count"] and not sp["each_flag"] and not deferred:
        hits.append(("A", "每个/每有", "中文有「%s」但 spec 里没有 计数= 也没有「每个」标记"
                     % zh["each"][0], 2))

    # --- ④ choose ------------------------------------------------------
    if zh["choose"] and not (sp["choose_one"] or sp["choose_card"]):
        if not re.search(r"随机选|随机挑", zh["choose"][0]):
            hits.append(("B", "选择", "中文有「%s」，spec 里没有 choose*" % zh["choose"][0], 3))

    # --- ⑤ 抽牌 ---------------------------------------------------------
    if zh["draw"] and not (sp["draw"] or sp["choose_card"] or sp["choose_one"]):
        hits.append(("A", "抽牌", "中文有「抽」但 spec 里没有 draw / choosecard / chooseone，"
                     "也没有替换标记", 1))

    # --- ⑥ 替换 ---------------------------------------------------------
    if zh["rep"] and not (sp["instead"] or has_alt):
        hits.append(("B", "替换", "中文有「%s」但 spec 里没有替换标记" % zh["rep_word"], 3))

    # --- ⑦ 中英**数据**本身不一致（desc vs descZh）-----------------------
    # 这一族不是解析差异，是**两边至少有一边错**（Shrineworld 就是它抓出来的：
    # 中文「抽 3 张牌」/ 英文「Destroy an enemy unit」/ 卡面「Draw 3 cards」）
    for label, zhpat, enpat in ZH_EN_PAIRS:
        z = re.search(zhpat, zh["raw"])
        if z and not re.search(enpat, low_en):
            hits.append(("B", "中英不一致", "中文有「%s」（%s），英文 desc 里没有对应词"
                         % (label, z.group(0)), 2))
            break

    if not hits:
        return "C", [], [], 0
    cls = "A" if any(h[0] == "A" for h in hits) else "B"
    sev = min(h[3] for h in hits)
    return cls, [h[1] for h in hits], [h[2] for h in hits], sev


# ---------------------------------------------------------------- main

def main():
    if not os.path.exists(PROBE):
        sys.stderr.write("没有 %s —— 先跑 EffectParseProbe.Run\n" % PROBE)
        return 1
    probe = parse_probe(PROBE)

    doc = json.load(io.open(CARDS, encoding="utf-8"))
    cards = doc["cards"]

    # 探针输入是**子句级**的（`probe_in.txt` 1097 行 ≈ 各卡的 `Split(desc)` 段），
    # 所以除了「整条 desc 直接对」之外，还要用 `desc_to_cards.tsv` 兜底（子句 → 卡）。
    d2c = {}
    try:
        for line in io.open(D2C, encoding="utf-8"):
            p = line.rstrip("\n").split("\t")
            if len(p) >= 2:
                d2c[p[0]] = [x.split("|")[-1] for x in p[1].split(";") if x.strip()]
    except Exception:
        pass
    byname = {}
    for c in cards:
        byname.setdefault(c.get("name") or "?", []).append(c)

    # desc → 卡名（同 desc 多卡：合并成一条，卡名用 `/` 连）
    d2cards = {}
    for c in cards:
        d = (c.get("desc") or "").strip()
        if d:
            d2cards.setdefault(d, []).append(c.get("name") or "?")

    rows = []
    seen_desc = set()
    for desc, blk in probe.items():
        names = d2cards.get(desc.strip())
        clause = False
        if names is None:                       # 探针里有、卡池对不上（多半是行尾空格）
            key = desc.strip()
            for d, ns in d2cards.items():
                if d.strip() == key:
                    names = ns
                    break
        if names is None:                       # 兜底：子句 → 卡（探针输入是子句级的）
            got = d2c.get(desc) or d2c.get(desc.strip())
            if got:
                names = got
                clause = True
        if names is None:
            rows.append({"desc": desc, "name": "（卡池里找不到这个 desc）", "zh": "",
                         "cls": "?", "checks": [], "notes": [], "sev": 9, "sp": spec_features(blk)})
            seen_desc.add(desc)
            continue
        seen_desc.add(desc)
        # 同 desc 多卡的中文应当一致；不一致时并起来看
        zhs, zhname = [], ""
        for c in cards:
            if (c.get("desc") or "").strip() == desc.strip():
                if c.get("descZh"):
                    zhs.append(c["descZh"])
                if c.get("nameZh") and not zhname:
                    zhname = c["nameZh"]
        zh = zhs[0] if zhs else ""
        sp = spec_features(blk)
        if not zh:
            rows.append({"desc": desc, "name": "/".join(names), "zh": "", "cls": "?",
                         "checks": [], "notes": ["中文缺（descZh 空）"], "sev": 9, "sp": sp})
            continue
        cls, checks, notes, sev = classify(zh_markers(zh), sp, desc)
        if clause and cls == "A":
            # ⚠️ 这一行是**子句**、中文拿的是**整条 desc** ⇒ 中文里别处的「若/或」会把子句比下去。
            #    降成 B、并写清原因，别让它污染 A 类（A 类只留「整条 desc 对得上」的那些）。
            cls = "B"
            notes = ["（子句级：中文按整张卡比对，**需人判**）" + " / ".join(notes)]
        rows.append({"desc": desc, "name": "/".join(names), "zh": zh, "cls": cls,
                     "checks": checks, "notes": notes, "sev": sev, "sp": sp,
                     "unparsed": blk["unparsed"], "partial": blk["partial"],
                     "pline": blk.get("line", 0), "ev": ev("/".join(names))})

    # ---- 统计
    zhm = [zh_markers(r["zh"]) for r in rows if r["zh"]]
    n_cond = sum(1 for m in zhm if m["cond"])
    n_or = sum(1 for m in zhm if m["or_effect"])
    n_or_noun = sum(1 for m in zhm if m["or_noun"] and not m["or_effect"])
    n_each = sum(1 for m in zhm if m["each"])
    n_choose = sum(1 for m in zhm if m["choose"])
    n_draw = sum(1 for m in zhm if m["draw"])
    n_rep = sum(1 for m in zhm if m["rep"])
    A = [r for r in rows if r["cls"] == "A"]
    B = [r for r in rows if r["cls"] == "B"]
    C = [r for r in rows if r["cls"] == "C"]
    Q = [r for r in rows if r["cls"] == "?"]

    A.sort(key=lambda r: (r["sev"], r["name"]))

    # ---- 全量 TSV
    with io.open(OUT_TSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("类\t卡名\t检查项\t说明\t中文\t英文desc\tspec(op行)\n")
        for r in sorted(rows, key=lambda x: (x["cls"], x["sev"], x["name"])):
            f.write("\t".join([
                r["cls"], r["name"], "|".join(r["checks"]), "|".join(r["notes"]),
                r["zh"], r["desc"], r["sp"]["optext"].replace("\t", " ").replace("\n", " ⏎ "),
            ]).replace("\n", " ") + "\n")

    print("卡（探针 desc 块）：%d" % len(rows))
    print("中文标记：若/如果 %d · 效果的「或」 %d · 名词的「或」 %d · 每 %d · 选择 %d · 抽 %d · 替换词 %d"
          % (n_cond, n_or, n_or_noun, n_each, n_choose, n_draw, n_rep))
    print("A 类 %d · B 类 %d · C 类 %d · 其它 %d" % (len(A), len(B), len(C), len(Q)))
    bycheck = {}
    for r in A:
        for c in r["checks"]:
            bycheck[c] = bycheck.get(c, 0) + 1
    print("A 类按检查项：", bycheck)
    print("→ %s" % OUT_TSV)

    # ---- 中间产物：给报告用（stdout 也算，方便我复核）
    with io.open("d:/4/_tmp_view/zh_crosscheck_detail.txt", "w", encoding="utf-8", newline="\n") as f:
        for r in A + B:
            f.write("[%s] %s  :: %s\n    中：%s\n    英：%s\n    spec：%s\n    不认/半懂：%s\n    出处：probe_out.txt:%d · %s\n\n"
                    % (r["cls"], r["name"], " / ".join(r["notes"]), r["zh"], r["desc"],
                       r["sp"]["optext"].replace("\n", " ⏎ "),
                       " | ".join((r.get("unparsed") or []) + (r.get("partial") or [])),
                       r.get("pline", 0), r.get("ev", "")))
    return 0


if __name__ == "__main__":
    sys.exit(main())
