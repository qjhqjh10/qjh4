# -*- coding: utf-8 -*-
"""zh_crosscheck.py —— 用**中文卡面文字**当尺子，核**英文解析结果**有没有读错句子。

🆕 **2026-09-18（本脚本第 2 版）：能读当前探针格式了。** 这一版修的是**读法**，
   顺带把 2026-09-16 那次普查里逐条核出来是**假阳性**的几条启发式**收窄/豁免**掉。
   想知道「改了什么、为什么改」，看 `资料/普查产出_0918/11③_中文对账脚本_修好.md`
   （每个改动都带 **文件:行号** 出处）。

  跑法：
    PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/zh_crosscheck.py            # 当前口径
    PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/zh_crosscheck.py --legacy   # 关掉 0918 新加的判据豁免
                                                                              # （读法修复保留，用来把
                                                                              #  「脚本没读对」与「判据本身错了」
                                                                              #   分开量）
  输入：`d:/4/_tmp_view/probe_out.txt`（逐句探针快照，`Editor/EffectParseProbe.cs` 产出）
        `d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`desc` / `descZh`）
  输出：`d:/4/_tmp_view/zh_crosscheck_0918.md`（正文报告）
        `d:/4/_tmp_view/zh_crosscheck_full.tsv`（全量明细，供逐条复查）
        `d:/4/_tmp_view/zh_crosscheck_detail.txt`（A/B 的逐条展开）

🔴 **本脚本的定位：人工普查的入口，不是尺子。** 真正在跑的是自检里那 6 条零假阳性的判据
   `RuleEngineTest.TestZhAgreement`（**J1–J6**）。本脚本的启发式**噪声比信号多**，用法是
   「筛候选给人看」，**别把它的 A 类当结论**（2026-09-16 全池 A 类 15 条逐条核完、**全是假阳性**）。

为什么要有它（2026-09-16）：
  解析器**只吃英文 `desc`**（`EffectText` 的所有调用点传的都是 `card.Desc`；`DescZh` 只走显示）。
  英文有歧义而中文没有 —— 同一个 `or` 能表示「**二选一**」（`A or B`）与
  「**条件换数值**」（`A, or B if C`，中文写「…若…则…」，见 `资料/卡牌效果or句_审计.md`），
  两条英文长得**一模一样**。本脚本把「中文里的结构标记」与「探针里 spec 实际做成了什么」
  **逐张并排比**，把差异分成三类：
    A 类 = **真差异候选**（中文有 X 结构、spec 里完全没有）
    B 类 = **可能差异**（中文有标记、spec 里表现形式不同，要人判）
    C 类 = 对上了（只计数）
    ? 类 = 卡池里找不到 / 中文缺（**2026-09-18 起应当只剩极少数**，见下）

⚠️ **中文不是独立尺子**：`数据/卡牌翻译/zh_cards.json` 是**我们自己译的**
  （官方中文在 remote-only 的 `localization_assets_all.bundle` 里，**本地从来没有过**）。
  它**从英文 desc 译出来** ⇒ 英文读错时它会跟着错；它能抓的是
  「**翻译时读懂了、解析器没读懂**」这一类。

⚠️ **探针快照必须与数据同源**（这条仍然成立，**跑之前先看一眼两个文件的 mtime**）：
  `probe_out.txt` 是某一次跑探针的**快照**，`cards_engine.json` 改过之后旧探针里的那几行
  **已经不在池里**。2026-09-18 这一版加了「子句按**子串**找卡」的兜底（原来只认
  `desc_to_cards.tsv` 那张**更旧**的映射表 ⇒ 26 行掉进 `?` 桶），但**英文句子本身改过**的
  那几条仍然只能靠重跑探针治 —— 跑不了探针时，结论要**如实写成「按旧快照」**。
"""
import io
import json
import os
import re
import sys

PROBE = "d:/4/_tmp_view/probe_out.txt"
CARDS = "d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
D2C = "d:/4/_tmp_view/desc_to_cards.tsv"      # **子句 → 卡** 的映射（探针输入是**子句级**的）
OUT_MD = "d:/4/_tmp_view/zh_crosscheck_0918.md"
OUT_TSV = "d:/4/_tmp_view/zh_crosscheck_full.tsv"
OUT_DETAIL = "d:/4/_tmp_view/zh_crosscheck_detail.txt"

# `--legacy` = **关掉 2026-09-18 新加的那些「判据豁免」**（探针新格式的读法修复**保留**）。
# 用途：把「脚本没读对」与「判据本身错了」两件事**分开量**（0918 那份报告就是这么分开写的）。
# ⚠️ 它**不是**「完整回到 0916」——「怎么读中文/怎么读一行」那三处修正两边都生效：
#    ① 探针新格式（嵌套 op / 尾句 / 粘尾组头）② `或 ±数字` 算效果（`Ancient Reliquary`）
#    ③ 中英词表 `cop(y|ies)`（对齐自检 J2）④ **子句级的行不比中英词表**（英文一句 vs 中文整卡）
LEGACY = "--legacy" in sys.argv

# ---------------------------------------------------------------- 探针解析
# 🔴 **2026-09-18 重写**。旧版按「op 行 = 8 空格起」认行，于是：
#   · **看不见嵌套 op** —— `EffectParseProbe.Nested`（`EffectParseProbe.cs:165`）打内层 op 用
#     **10 空格**（`{8 空格}  · `），而 `^ {8}\\S` 在第 9 个字符上是空格 ⇒ 整层丢掉。
#     实测代价：`Sororitas Rhino` 的 `deploy ... 条件=anypraying` **就藏在 `↳回合内层` 里**，
#     旧版看不见 ⇒ 报成 A 类「条件被吞」（**假阳性**）。
#   · **尾句认不出来** —— `↳尾句` 与 op 行**同一个缩进**（都是 8 空格，见 `EffectParseProbe.cs:160`），
#     旧版的 `TAIL_RE` 却写死 16 空格 ⇒ 永不匹配，尾巴被当成**另一条 op**（verb 解不出、只是脏文本）。
#   · **嵌套组头可能粘在父 op 行尾** —— `Nested` 是先 `sb.Append($"{pad}↳…")` 再补 `\n`（:169），
#     所以「这条 op 有几条内层」这件事**混在父行末尾**。
HEAD_RE = re.compile(r"^【(.*)】$")
CARDLEVEL_RE = re.compile(r"^  卡级 → op (\d+) 条 · 不认识 (\d+) · 半懂 (\d+)")
UNPARSED_RE = re.compile(r"^ {8}✗ 不认识: (.*)$")
PARTIAL_RE = re.compile(r"^ {8}⚠ 半懂: (.*)$")
SEGLINE_RE = re.compile(r"^   · \[(.*?)\] 「(.*)」$")           # `   · [认了] 「<句子>」`
OPLINE_RE = re.compile(r"^ {8}(\S.*)$")                        # op 起行（8 空格 + 非空）
NEST_HEAD_RE = re.compile(r"↳(回合内层|重复内层|修饰指向)(（[^）]*）)? (\d+) 条：")
NEST_OP_RE = re.compile(r"^ {10}· (.*)$")                      # 内层 op（10 空格 + `· `）
TAIL_RE = re.compile(r"^ {8}↳尾句 「(.*)」$")

# 探针里 op 行上出现的结构特征（出处：`EffectParseProbe.Dump`，`EffectParseProbe.cs:87`）
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
# ---- 🆕 2026-09-18：**探针 0916 新加、旧版一个都没用上**的三个标记 ----
# 出处：`EffectParseProbe.Dump` 的 `o.RandomPick` / `o.DeathWatchTarget` / `o.ChooseWhat`（:119-150）
RANDOMPICK_RE = re.compile(r"\*\*随机抽\*\*")
DEATHWATCH_RE = re.compile(r"死后转给\[([^\]]*)\]")
CHOOSEPICK_RE = re.compile(r"选牌\[([^\]]*)\]")
CHOOSE_FIELD_RE = re.compile(r"(来源|选什么|动作|阵亡范围|份数)=([^\s\]]+)")
WHEN_EVENT_RE = re.compile(r"事件=«([^»]*)»")
PAY_RE = re.compile(r"付费=(\d+)([a-z]+)")


def _newblock(cur, line):
    return {"desc": cur, "line": line, "ops": [], "segs": [], "unparsed": [], "partial": [],
            "opcount": 0, "nblocks": 0}


def parse_probe(path):
    """→ {desc: blk}。同一个 desc 出现多次时**取并集**（探针按 desc 走，池里允许同 desc 多卡）。

    `blk` 结构（**0918 新**）：
      · `ops`   —— 摊平后的 op 列表，每项 `{"text", "inner": [...], "tail", "groups": [...]}`
      · `segs`  —— `[{"kind", "seg", "ops"}]`（探针**逐句**打，句子的类别在这里）
      · `line`  —— 这个块在 `probe_out.txt` 里的**行号**（报告里当可复查坐标用）
    """
    blocks = {}
    cur = None
    cur_line = 0
    seg = None
    for lineno, raw in enumerate(io.open(path, encoding="utf-8"), 1):
        line = raw.rstrip("\n").rstrip("\r")
        m = HEAD_RE.match(line)
        if m:
            cur = m.group(1)
            cur_line = lineno
            b = blocks.setdefault(cur, _newblock(cur, cur_line))
            b["nblocks"] += 1
            seg = None
            continue
        if cur is None:
            continue
        b = blocks.setdefault(cur, _newblock(cur, cur_line))
        m = CARDLEVEL_RE.match(line)
        if m:
            b["opcount"] = int(m.group(1))
            continue
        m = UNPARSED_RE.match(line)
        if m:
            b["unparsed"] += [x.strip() for x in m.group(1).split(" | ") if x.strip()]
            continue
        m = PARTIAL_RE.match(line)
        if m:
            b["partial"] += [x.strip() for x in m.group(1).split(" | ") if x.strip()]
            continue
        m = SEGLINE_RE.match(line)
        if m:
            seg = {"kind": m.group(1), "seg": m.group(2), "ops": []}
            b["segs"].append(seg)
            continue
        if not line.strip():
            continue
        # ---- op 区：**先认 10 空格的内层 op**，再认 8 空格起的外层行
        #      ⚠️ 顺序不能反：内层行第 9 个字符是空格，`^ {8}\S` **不匹配**，
        #         拿 OPLINE_RE 当门就会把整层悄悄滤掉（这正是旧版犯的错）。
        m_inner = NEST_OP_RE.match(line)
        if not m_inner and not OPLINE_RE.match(line):
            continue
        if seg is None:                       # 兜底：没有段行时也别丢 op（当前探针不会这样）
            seg = {"kind": "（无段行）", "seg": "", "ops": []}
            b["segs"].append(seg)
        if m_inner:
            if seg["ops"]:
                body = m_inner.group(1)
                mh0 = NEST_HEAD_RE.search(body)      # 内层 op 自己也可能拖着嵌套组头
                if mh0:
                    body = body[:mh0.start()].rstrip()
                seg["ops"][-1]["inner"].append(body)
            continue
        body = line[8:]
        if body.startswith("↳尾句"):
            m = TAIL_RE.match(line)
            if m and seg["ops"]:
                seg["ops"][-1]["tail"] = m.group(1)
            continue
        # 嵌套组头：独立一行（`{8 空格}↳回合内层…`）或**粘在父行尾**
        mh = NEST_HEAD_RE.search(body)
        if mh and body.lstrip().startswith("↳"):
            if seg["ops"]:
                seg["ops"][-1]["groups"].append(mh.group(1))
            continue
        inner = False
        if mh:                                 # 粘在行尾的那种
            body = body[:mh.start()].rstrip()
        op = {"text": body, "inner": [], "tail": "", "groups": []}
        if mh:
            op["groups"].append(mh.group(1))
        seg["ops"].append(op)
    for b in blocks.values():
        for s in b["segs"]:
            b["ops"].extend(s["ops"])
        b["optext"] = "\n".join(op_flat(o) for o in b["ops"])
    return blocks


def op_flat(o):
    """一条 op 的**全部**文本（外层 + 内层 + 尾句）—— 特征提取一律在这个并集上做。"""
    parts = [o["text"]]
    parts.extend(o["inner"])
    if o["tail"]:
        parts.append("尾句 " + o["tail"])
    return " ⏎ ".join(parts)


def spec_features(blk):
    """把一张卡的 op 集合摊成结构特征（**外层 + 嵌套内层都算**）。"""
    txt = blk["optext"]
    verbs = []
    for o in blk["ops"]:
        for piece in [o["text"]] + o["inner"]:
            m = VERB_RE.match(piece)
            if m:
                verbs.append(m.group(1))
    # 目标段里去掉「引文」，剩下的才是**标记**（`每个` 这种 flag 不在引号里）
    tflags = []
    for seg in TARGET_SEG_RE.findall(txt):
        tflags.append(re.sub(r"「[^」]*」", "", seg))
    tflags = " ".join(tflags)
    chooses = {}
    for raw in CHOOSEPICK_RE.findall(txt):
        for k, v in CHOOSE_FIELD_RE.findall(raw):
            chooses.setdefault(k, []).append(v)
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
        # ---- 🆕 2026-09-18：原来一个都没用的三个标记 ----
        "random_pick": bool(RANDOMPICK_RE.search(txt)),        # `**随机抽**`（`EffectOp.RandomPick`）
        "death_watch": DEATHWATCH_RE.findall(txt),             # `死后转给[…]`（`EffectOp.DeathWatchTarget`）
        "choose_src": chooses.get("来源", []),
        "choose_what": chooses.get("选什么", []),
        "choose_act": chooses.get("动作", []),
        "choose_copies": chooses.get("份数", []),
        "when_event": WHEN_EVENT_RE.findall(txt),              # `事件=«…»`（`EffectOp.When`）
        "each_player": "每方各一次" in txt,
        "pay_kind": PAY_RE.findall(txt),                       # 行首付费前缀（`[Spirit Stone]` 那类）
        "nested_n": sum(len(o["inner"]) for o in blk["ops"]),
        "tails": [o["tail"] for o in blk["ops"] if o["tail"]],
        # `costwhen` / `costifcontrol` = **降费那条路**的标记 op（条件载体，见 J4 的豁免）
        "cost_op": any(v in ("costwhen", "costifcontrol") for v in verbs),
        # `atturn` / `persist` 的正文**存在载荷里、结算期才再解析一遍** ⇒ 探针看不见里面有什么
        "deferred": any(v in ("atturn", "persist") for v in verbs),
    }


# ---------------------------------------------------------------- 别的层（判据的「家」在 C#）
# 🔴 这几族句子**机制一直在跑**，只是**不在 op 树上** —— `CardDef.HandledByOtherLayer`
#    （`Core/CardDef.cs:1562`）就是干这个的（它自己也只**转调**那几个抽取函数，一行新文法都没有）。
#    ⚠️ **这里是一份镜像**（C# 的判据没法在 python 里调）—— 改 `CardDef.cs` 那边**要同步改这里**，
#       和 `工具/aura_dryrun.py` 的处境一样（那份文件头也写着同一句话）。
#    ⚠️ 镜像**只认实测见过的写法**，认不出就**别豁免**（宁可误报，不可静默放过）。
RE_OTHER_WHEN = re.compile(r"^(when|whenever|after receiving a dark pact)\b", re.I)
RE_OTHER_TALENT = re.compile(r"^talent\s*:", re.I)
RE_OTHER_COMPANION = re.compile(r"^companion\s+\d+\s*:", re.I)
RE_OTHER_STARTWITH = re.compile(r"^start the game with\b.*\bin hand\b", re.I)
# 光环：`Auras.TryParse` 的形状，抄自 `工具/aura_dryrun.py:27-34`（那份与 `Core/Aura.cs` 同源）
RE_OTHER_AURA = re.compile(
    r"^(?:adjacent|your other|other friendly|friendly|your|enemy|enemies)\s*(.*?)\s*(?:have|has)\s+(.+)$",
    re.I)
RE_OTHER_AURA_SPECIAL = re.compile(
    r"^(?:(?:has\s+)?flying\s+during\s+your\s+turn"
    r"|adjacent\s+remnants?\s+do\s+not\s+disappear\s+at\s+the\s+end\s+of\s+your\s+turn)$", re.I)
# 静态改战斗规则：`CardDef.MatchStaticBattleRule`（`Core/CardDef.cs:632`）那 7 句，逐字
STATIC_RULES = (
    "this troop's melee is always equal to its health",
    "targets this troop instead",                      # `Any attack against your Warlord …`（替身）
    "this troop can ignore enemy units with vanguard when attacking",
    "less for each card in enemy hand",                # `Costs N less for each card in enemy hand`
    "friendly [oath] abilities apply an additional time",
    "oath abilities of friendly troops can be activated up to 3 times each turn",
    "oath abilities of friendly troops may be activated on later turns",
)
# 🆕 2026-09-18 **跨句回填**（不是「别的层」，但同样是**探针逐句看不见**的那一类）：
# `If it dies this turn, apply this effect to <目标>`（`BL77 Spreading Corruption`，全池一句）——
# 判据 `EffectText.ReApplyOnDeath`（`Core/EffectText.cs:5088`），落点是**上一条 op** 的
# `DeathWatchTarget`（`TryApplyOnDeath` / `TryFillDeathWatch`，`EffectText.cs:5070`/`:5108`）。
# 探针是**逐句**跑的（`EffectParseProbe.cs:61-74`），`ParseSegment` 那一路 `r.Ops` 是空的 ⇒
# 这一句在探针里永远显示 `[不认]`，而**整条 desc 走 `Parse` 时它接得上**。
RE_APPLY_ON_DEATH = re.compile(r"^if it dies this turn,?\s*apply this effect to ", re.I)


def other_layer(seg, ctype):
    """这一句**是不是已经由另一个层接手了** → 层名 / None。镜像 `CardDef.HandledByOtherLayer`。

    ⚠️ **事件那一族只对单位/督军成立**（`CardDef.CanListenForEvents => IsUnit`，见 `CardDef.cs:1567`）——
       非单位卡的 `When …` 是**手牌陷阱**，走 `EffectText.SplitHandTrapWhen`，**不能**按这一族豁免。
    """
    if not seg:
        return None
    s = seg.strip()
    if RE_APPLY_ON_DEATH.match(s):
        return "跨句回填（DeathWatchTarget）"
    if ctype in ("unit", "hero") and RE_OTHER_WHEN.match(s):
        return "事件层（WhenTriggers）"
    if RE_OTHER_TALENT.match(s):
        return "天赋（TalentName）"
    if RE_OTHER_COMPANION.match(s):
        return "伴生（CompanionName）"
    if RE_OTHER_STARTWITH.match(s):
        return "开局上手（StartWithInHand）"
    if RE_OTHER_AURA.match(s) or RE_OTHER_AURA_SPECIAL.match(s):
        return "光环（AuraSpecs）"
    low = s.lower()
    for pat in STATIC_RULES:
        if pat in low:
            return "静态改战斗规则"
    return None


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
# 🆕 2026-09-18：补 **`±数字`**（`…、+3 远程攻击或 +3 生命` 里的「或」也是二选一，
#    见 `Ancient Reliquary`）—— 旧版把它归成「名词的或」⇒ 报成 B 类假阳性。
ZH_EFFECT_HEAD = re.compile(
    r"^(给予|造成|对|使|获得|抽|部署|治疗|消灭|摧毁|返回|生成|创建|复制|移除|弃|减|增加|"
    r"为|将|让|本回合|复活|复生|召唤|翻倍|眩晕|填满|刷新|弃掉|弃置|攻击|损失|牺牲|[+\-−]?\d)")

# 中文有「若/则」而 spec 里没有条件时：spec 的**引文**里若出现英文条件从句头，
# 就说明那句话**被吞进了目标或载荷**（`scan_swallowed_clauses.py` 那条判据的窄化版）
QUOTE_RE = re.compile(r"「([^」]*)」")
EN_COND_HEAD = re.compile(r"(?i)(^|[,;]\s*|\s)(if|when|whenever)\s")

# ---- 「中文说的效果」↔「英文 desc 里到底有没有这个词」----
# 两边**至少有一边错**（Shrineworld：中文「抽 3 张牌」/ 英文「Destroy an enemy unit」，
# 卡面是「Draw 3 cards」⇒ **英文那一侧被手改错了**）。这一族是**数据**差异，不是解析差异。
# ⚠️ 2026-09-18 修：原来写 `\bcopy\b`，**匹配不上 `copies`** ⇒ `Metamorph Iconbearer` /
#    `Lead From the Front` 两条假阳性。自检 J2 那处一直写的是 `cop(y|ies)`（`RuleEngineTest.cs:6079`），
#    这里跟它对齐 —— **同一条判据不要写两份**。
ZH_EN_PAIRS = [
    ("抽牌", r"抽", r"\b(draw|drew|drawn)\b"),
    ("伤害", r"伤害|造成", r"\bdamage\b"),
    ("部署", r"部署", r"\b(deploy|put in play)"),
    ("治疗", r"治疗", r"\bheal|\brestore"),
    ("摧毁/消灭", r"摧毁|消灭", r"\bdestroy"),
    ("眩晕", r"眩晕", r"\bstun"),
    ("弃牌", r"弃", r"\bdiscard"),
    ("复制", r"复制", r"\bcop(y|ies)\b"),
    ("护甲", r"护甲|装甲", r"armou?r"),
]
# ⚠️ **不能**进来的一族（实测全是假阳性，2026-09-16）：
#   · `攻击` —— 英文的图标被剥掉后只剩裸 `+1` / 写成 `Ranged` / `[melee]`，都不含 "attack"
#   · `生命` —— 英文写 `heal 1`（不含 health）
#   · `费用` / `侧翼` / `手牌` / `牌库` —— 与中文措辞差得太远
# 这一族**只是「两边至少有一边错」的候选**，判它要去看 PnP 卡面（铁律 7）。

# 中文里的引号（`「」` / `“”` / `""`）—— 落在引号里的词是**被授予的能力**，不是本卡自己的效果
ZH_QUOTED = re.compile(r"「[^」]*」|“[^”]*”|\"[^\"]*\"")


def zh_markers(zh):
    """从中文里抽结构标记。`或` 逐处判后面接的是**效果**还是**名词**。"""
    or_effect, or_noun = [], []
    for m in re.finditer(r"或", zh):
        tail = zh[m.end():m.end() + 12].lstrip("　 　、，,")   # 🆕 先剥前导空白/顿号（`或 +3 生命`）
        if ZH_EFFECT_HEAD.match(tail):
            or_effect.append(zh[max(0, m.start() - 12):m.end() + 14])
        else:
            or_noun.append(zh[max(0, m.start() - 12):m.end() + 14])
    quoted = []
    for m in ZH_QUOTED.finditer(zh):
        quoted.append((m.start(), m.end()))
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
        # 🆕 引号里的命中（用来判「这个标记其实是被授予的能力，不是本卡的效果」）
        "quoted": quoted,
    }


def in_quotes(mk, word):
    """中文里 `word` 的每一次出现**是不是都在引号内**（引号内的 = 授予出去的能力）。"""
    hits = list(re.finditer(re.escape(word), mk["raw"]))
    if not hits:
        return False
    for m in hits:
        for a, b in mk["quoted"]:
            if a <= m.start() < b:
                break
        else:
            return False
    return True


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

def classify(zh, sp, en, ctx):
    """→ (类, 检查项[], 说明[], 严重度, 豁免说明[])。A=真差异候选 B=可能差异 C=对上。

    ⚠️ 每一条判据都踩过假阳性，注释里记着**为什么这么写** —— 改之前先读。
    🆕 2026-09-18：新增第 5 个返回值 `exempted`（**本来会报、但被判据豁免掉的**）——
       豁免**必须写出来**，不许静默丢掉（本工程的规矩：不许静默失败）。
    """
    low_en = en.lower()
    txt = sp["optext"]
    hits = []
    exempted = []

    has_cond = bool(sp["cond_known"] or sp["cond_unknown"])
    has_alt = bool(sp["alt_amt"] or sp["alt_cond"])
    cost_cond = sp["cost_op"]
    deferred = sp["deferred"]
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
            exempted.append("条件：中文有「%s」；条件写在 atturn/persist 载荷里，探针看不见"
                            % zh["cond_word"])
        elif "when" in low_en and " if " not in low_en:
            if LEGACY:      # 0916 原样：降成 B（那时还判不了「触发 vs 条件」）
                hits.append(("B", "条件", "中文有「%s」、英文写的是 when（多半是触发不是条件）"
                             % zh["cond_word"], 3))
            else:
                exempted.append("条件：中文有「%s」、英文写的是 when（**触发**不是条件 ⇒ 事件层）"
                                % zh["cond_word"])
        elif ctx["other"] and not LEGACY and not ctx["clause"]:
            # 别的层里已经有条件的（`If it dies this turn, apply this effect to …` 那种
            # 走**跨句回填** `DeathWatchTarget`，`Core/EffectText.cs:5063`）。
            exempted.append("条件：中文有「%s」，但条件在**别的层**（%s）里"
                            % (zh["cond_word"], "／".join(ctx["other"])))
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

    # --- ③ 按数量缩放（🆕 2026-09-18 收窄）--------------------------------
    # 中文的「每」有**两种含义**：
    #   ① 按数量缩放（`每有 1 个敌人死亡，费用 -1`）⇒ 对应 op 上的 `计数=`（`ForEach`）
    #   ② 对集合中每一个（`给予每个我方部队…`）⇒ 对应**目标短语**里的 `each <名词>`
    # 旧判据把②也当成 bug ⇒ **2026-09-16 全池 9 条逐条核完、全是假阳性**
    # （8 条的英文写的是 `each <目标>`、第 9 条 `Burgeoning Empire` 走的是 `costwhen` 降费）——
    # 见 `资料/普查产出_0916/静默桩家族_0916.md` §四。
    # 现在只在「**英文 desc 里连 each/every 都没有**」时才报 —— 那时才真可疑。
    each_scaling = bool(zh["each"]) and not re.search(r"(?i)\b(each|every)\b", en)
    # 🆕 2026-09-18：**先查英文是不是缩放写法**（`for each` / `for every`）—— 那才是「每」的缩放义。
    # 实测（**全池跑过**，命令见 0918 那份报告）：英文写 `for each|for every` 的 **45 张**里，
    # **44 张** spec 里有 `计数=`，唯一例外 `Patriarch` 走的是**静态降费**那条路
    # （`CardDef.CostPerEnemyHandCard`，`Core/CardDef.cs:641` + `RuleCore.CostOf`）⇒ 也在豁免里。
    # ⇒ 收窄之后**一条真差异都没漏**（这是收窄的正当性证据，不是「看着像就放过」）。
    en_for_each = re.search(r"(?i)\bfor (each|every)\b", en)
    if LEGACY:
        if zh["each"] and not sp["count"] and not sp["each_flag"] and not deferred:
            hits.append(("A", "每个/每有", "中文有「%s」但 spec 里没有 计数= 也没有「每个」标记"
                         % zh["each"][0], 2))
    elif zh["each"]:
        if sp["count"] or sp["each_flag"]:
            exempted.append("按数量缩放：中文有「%s」，spec 里有 计数=/「每个」标记，对上了"
                            % zh["each"][0])
        elif cost_cond or deferred or ctx["other"]:
            exempted.append("按数量缩放：中文有「%s」，走的是**另一条路**（降费 costwhen/costifcontrol "
                            "或别的层），不在 op 树上" % zh["each"][0])
        elif each_scaling:
            hits.append(("B", "按数量缩放", "中文有「%s」，英文里**连 each/every 都没有** ⇒ 需人判"
                         % zh["each"][0], 2))
        elif en_for_each:
            # 英文是**缩放**写法却没有 `计数=` —— 这一条**真可疑**（收窄后全池 0 命中）
            hits.append(("B", "按数量缩放",
                         "英文写的是 `for each/for every`（缩放），但 spec 里没有 计数=", 2))
        else:
            exempted.append("按数量缩放：中文有「%s」，英文的 each/every 是**目标短语**那半"
                            "（`each friendly troop` = 对集合中每一个，不是缩放）" % zh["each"][0])

    # --- ④ choose ------------------------------------------------------
    if zh["choose"] and not (sp["choose_one"] or sp["choose_card"]):
        if re.search(r"随机选|随机挑", zh["choose"][0]):
            exempted.append("选择：中文写的是「随机选」（不是让玩家选）")
        elif "of your choice" in low_en and not LEGACY:
            # `Give a Kustom Job of your choice to a friendly Vehicle`（`Mekaniak`）——
            # 选择在**载荷**里，解析入口 `EffectText.TryChooseEffect` ③
            # （`Core/EffectText.cs:4100-4128`）+ `EffectResolver.cs:2351`。
            exempted.append("选择：英文写的是 `of your choice`（选择在**载荷**里，另一条路接手）")
        elif ctx["other"] and not LEGACY and not ctx["clause"]:
            exempted.append("选择：中文有「%s」，但这一句在**别的层**（%s）里"
                            % (zh["choose"][0], "／".join(ctx["other"])))
        else:
            hits.append(("B", "选择", "中文有「%s」，spec 里没有 choose*" % zh["choose"][0], 3))

    # --- ⑤ 抽牌 ---------------------------------------------------------
    if zh["draw"] and not (sp["draw"] or sp["choose_card"] or sp["choose_one"]):
        if not LEGACY and re.search(r"(?i)\bwhen(ever)?\b[^.]{0,40}\bdraw", en):
            # `When you draw a card, …` —— 走**事件层**（`RuleCore.cs:767` 的
            # `WhenEventKind.Draw` 广播 + `WhenEvent.cs:134`）。中文的「抽」在**从句**里。
            exempted.append("抽牌：中文的「抽」在**触发从句**里（`When you draw a card` ⇒ 事件层）")
        elif not LEGACY and (in_quotes(zh, "抽") or re.search(r"(?i)['\"][^'\"]*\bdraw", en)):
            # `Give to a friendly troop Flank and 'Strike: Draw a card'` —— 引号里是**授予出去的能力**
            exempted.append("抽牌：中文的「抽」在**引号内的授予能力**里（不是本卡自己的效果）")
        elif ctx["other"] and not LEGACY and not ctx["clause"]:
            exempted.append("抽牌：这一句在**别的层**（%s）里" % "／".join(ctx["other"]))
        else:
            hits.append(("A", "抽牌", "中文有「抽」但 spec 里没有 draw / choosecard / chooseone，"
                         "也没有替换标记", 1))

    # --- ⑥ 替换 ---------------------------------------------------------
    if zh["rep"] and not (sp["instead"] or has_alt):
        if ctx["other"] and not LEGACY and not ctx["clause"]:
            # `Armour 2. Any attack against your Warlord targets this troop instead.`（`Vargard Obyron`）
            # —— **静态改战斗规则**那一族：`CardDef.MatchStaticBattleRule` 的「替身」
            # （`Core/CardDef.cs:637`），读点在 `RuleCore.DeclareAttack`（`RuleCore.cs:1346`）。
            exempted.append("替换：这一句在**别的层**（%s）里" % "／".join(ctx["other"]))
        else:
            hits.append(("B", "替换", "中文有「%s」但 spec 里没有替换标记" % zh["rep_word"], 3))

    # --- ⑦ 中英**数据**本身不一致（desc vs descZh）-----------------------
    # 这一族不是解析差异，是**两边至少有一边错**（Shrineworld 就是它抓出来的：
    # 中文「抽 3 张牌」/ 英文「Destroy an enemy unit」/ 卡面「Draw 3 cards」）。
    # ⚠️ 2026-09-18：**子句级的行上一律不报** —— 那一边英文是**一句子句**、中文是**整张卡**，
    #    两者本来就不该一致 ⇒ 实测 8 条全是这么来的（`Paragon Warsuit` 的
    #    `6 [Faith Icon]: Gain Vanguard` 那行 vs 卡面上别处的「造成 4 点伤害」）。
    pair_hits = []
    for label, zhpat, enpat in ZH_EN_PAIRS:
        z = re.search(zhpat, zh["raw"])
        if z and not re.search(enpat, low_en):
            pair_hits.append((label, z.group(0)))
    if pair_hits and ctx["clause"]:
        exempted.append("中英不一致：**子句级**的行（英文只一句、中文是整张卡）⇒ 这条判据不成立")
    elif pair_hits:
        label, w = pair_hits[0]
        hits.append(("B", "中英不一致", "中文有「%s」（%s），英文 desc 里没有对应词"
                     % (label, w), 2))

    if not hits:
        return "C", [], [], 0, exempted
    cls = "A" if any(h[0] == "A" for h in hits) else "B"
    sev = min(h[3] for h in hits)
    return cls, [h[1] for h in hits], [h[2] for h in hits], sev, exempted


# ---------------------------------------------------------------- 探针 desc → 卡
# **判据只此一处**：`zh_crosscheck.py` 与 `scan_fixedcount_targets.py` 共用这一份
# （探针的 desc 是**子句级**的，映射规则写两份迟早不一致）。

def _norm_desc(s):
    return re.sub(r"\s+", " ", (s or "").strip().rstrip(".").lower())


def make_cards_for(cards, d2c_path=D2C):
    """→ `cards_for(text) → (卡对象列表|None, 是不是子句级)`。

    三级依次试：
      ① **整条 desc 相等**（探针输入就是整条 desc 时走这条）
      ② **归一化后按子串找**（子句级的行；归一化 = 压空白 + 去尾句点 + 小写）
      ③ `desc_to_cards.tsv`（那张**更旧**的映射表，最后兜底）

    🆕 2026-09-18：返回的是**卡对象**、不是卡名。旧版先落成卡名、再「按名字取中文」——
       于是**同名跨阵营的卡串了**（实测 `Maulerfiend` 两张同名：一张
       `Ecstasy 5: Double this troop's melee and ranged`、另一张
       `Deal 1 damage to all enemies for each Dark Pact on your troops`，
       第一张的中文显示成第二张的「每有一个我方部队身上的黑暗契约…」⇒ 报出一条**凭空的 A 类**）。
       自检 **J1** 盯的正是「同名跨阵营串中文」这件事。
    ⚠️ 子串那级的**代价**：很短的句子（`Ephemeral.` / `Stomp.`）会挂到**几十张卡**上 ——
       那时的卡名列表只表示「谁的 desc 里有这句」，不是「几十张都有问题」。
    """
    d2c = {}
    try:
        for line in io.open(d2c_path, encoding="utf-8"):
            p = line.rstrip("\n").split("\t")
            if len(p) >= 2:
                d2c[p[0]] = [x.split("|")[-1] for x in p[1].split(";") if x.strip()]
    except Exception:
        pass
    byname = {}
    for c in cards:
        byname.setdefault(c.get("name") or "?", []).append(c)
    d2cards = {}
    for c in cards:
        d = (c.get("desc") or "").strip()
        if d:
            d2cards.setdefault(d, []).append(c)
    pool_norm = [(_norm_desc(c.get("desc")), c) for c in cards if (c.get("desc") or "").strip()]

    def cards_for(text):
        key = text.strip()
        got = d2cards.get(key)
        if got:
            return got, False
        for d, cs in d2cards.items():
            if d.strip() == key:
                return cs, False
        nk = _norm_desc(key)
        if nk:
            hit = [c for pd, c in pool_norm if nk in pd]
            if hit:
                return hit, True
        names = d2c.get(text) or d2c.get(key)
        if names:                       # 最后才用那张**更旧**的映射表
            out = []
            for n in names:
                out.extend(byname.get(n, []))
            if out:
                return out, True
        return None, False

    return cards_for


# ---------------------------------------------------------------- main

def main():
    if not os.path.exists(PROBE):
        sys.stderr.write("没有 %s —— 先跑 EffectParseProbe.Run\n" % PROBE)
        return 1
    probe = parse_probe(PROBE)

    doc = json.load(io.open(CARDS, encoding="utf-8"))
    cards = doc["cards"]

    # 探针 desc → 卡：**判据只此一处**（`make_cards_for`，与 `scan_fixedcount_targets.py` 共用）
    cards_for = make_cards_for(cards)

    rows = []
    for desc, blk in probe.items():
        cs, clause = cards_for(desc)
        sp = spec_features(blk)
        if cs is None:
            rows.append({"desc": desc, "name": "（卡池里找不到这个 desc）", "zh": "",
                         "cls": "?", "checks": [], "notes": [], "exempt": [], "sev": 9, "sp": sp,
                         "pline": blk.get("line", 0), "types": []})
            continue
        names = sorted({c.get("name") or "?" for c in cs})
        # 同 desc 多卡的中文应当一致；不一致时取**第一个**（并记下有几个同名不同 desc 的）
        # 🆕 2026-09-18：中文**按卡对象**取，不再「按名字再查一遍」——
        #    旧版那样查会把**同名跨阵营**的卡串起来（见 `cards_for` 的注释）。
        zhs, zhname, types = [], "", set()
        for c in cs:
            if c.get("descZh"):
                zhs.append(c["descZh"])
            if c.get("nameZh") and not zhname:
                zhname = c["nameZh"]
            types.add(c.get("type"))
        zh = zhs[0] if zhs else ""
        if not zh:
            rows.append({"desc": desc, "name": "/".join(names), "zh": "", "cls": "?",
                         "checks": [], "notes": ["中文缺（descZh 空）"], "exempt": [], "sev": 9,
                         "sp": sp, "pline": blk.get("line", 0), "types": sorted(types)})
            continue
        # 🆕 别的层：只对**探针没认下来的那些句子**问 `other_layer`（认下来的当然是 op 树接了）
        other = []
        ctype = sorted(types)[0] if len(types) == 1 else None
        for s in blk["segs"]:
            if s["kind"] in ("不认", "半懂") and not s["ops"]:
                nm = other_layer(s["seg"], ctype)
                if nm and nm not in other:
                    other.append(nm)
        ctx = {"en": desc, "clause": clause, "other": other, "types": sorted(types), "names": names}
        cls, checks, notes, sev, exempt = classify(zh_markers(zh), sp, desc, ctx)
        if clause:
            # ⚠️ 这一行是**子句**、中文拿的是**整条 desc** ⇒ 中文里别处的「若/或」会把子句比下去。
            #    A 降成 B 并写清原因，别让它污染 A 类（A 类只留「整条 desc 对得上」的那些）。
            #    🆕 0918：**B/C 也要标出来** —— 不然读者分不清「这条 B 是整卡对整卡、还是子句对整卡」，
            #    实测那 9 条「中英不一致」B 全是这么来的。
            if cls == "A":
                cls = "B"
            notes = ["（子句级：英文只一句、中文是**整张卡**，需人判）"] + notes
        rows.append({"desc": desc, "name": "/".join(names), "zh": zh, "cls": cls,
                     "checks": checks, "notes": notes, "exempt": exempt, "sev": sev, "sp": sp,
                     "unparsed": blk["unparsed"], "partial": blk["partial"],
                     "pline": blk.get("line", 0), "ev": ev("/".join(names)),
                     "types": sorted(types), "other": other,
                     "zhname": zhname, "nested_n": sp["nested_n"], "opcount": blk["opcount"]})

    n_nested = sum(1 for r in rows if r.get("nested_n"))
    n_other = sum(1 for r in rows if r.get("other"))

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

    # 豁免按「检查项」分组（豁免说明里「：」之前那一段就是检查项名）
    exemptions = {}
    for r in rows:
        for e in (r.get("exempt") or []):
            exemptions.setdefault(e.split("：")[0], []).append(r["name"])

    # ---- 全量 TSV
    with io.open(OUT_TSV, "w", encoding="utf-8", newline="\n") as f:
        f.write("类\t卡名\t检查项\t说明\t豁免\t探针行\t中文\t英文desc\tspec(op行)\n")
        for r in sorted(rows, key=lambda x: (x["cls"], x["sev"], x["name"])):
            f.write("\t".join([
                r["cls"], r["name"], "|".join(r["checks"]), "|".join(r["notes"]),
                "|".join(r.get("exempt") or []), "probe_out.txt:%d" % r.get("pline", 0),
                r["zh"], r["desc"], r["sp"]["optext"].replace("\t", " ").replace("\n", " ⏎ "),
            ]).replace("\n", " ") + "\n")

    print("模式：%s" % ("--legacy（只修读法、判据保持 0916）" if LEGACY else "当前（读法 + 判据都已修）"))
    print("卡（探针 desc 块）：%d · 其中**有嵌套 op** 的 %d 张 · 有「别的层」句子的 %d 张"
          % (len(rows), n_nested, n_other))
    print("中文标记：若/如果 %d · 效果的「或」 %d · 名词的「或」 %d · 每 %d · 选择 %d · 抽 %d · 替换词 %d"
          % (n_cond, n_or, n_or_noun, n_each, n_choose, n_draw, n_rep))
    print("A 类 %d · B 类 %d · C 类 %d · 其它 %d" % (len(A), len(B), len(C), len(Q)))
    bycheck = {}
    for r in A:
        for c in r["checks"]:
            bycheck[c] = bycheck.get(c, 0) + 1
    print("A 类按检查项：", bycheck)
    print("判据豁免（分组）：", {k: len(v) for k, v in sorted(exemptions.items(), key=lambda kv: -len(kv[1]))})
    print("→ %s" % OUT_TSV)

    # ---- 中间产物：给报告用（stdout 也算，方便我复核）
    with io.open(OUT_DETAIL, "w", encoding="utf-8", newline="\n") as f:
        for r in A + B:
            f.write("[%s] %s  :: %s\n    中：%s\n    英：%s\n    spec：%s\n    不认/半懂：%s\n"
                    "    判据豁免：%s\n    出处：probe_out.txt:%d · %s\n\n"
                    % (r["cls"], r["name"], " / ".join(r["notes"]), r["zh"], r["desc"],
                       r["sp"]["optext"].replace("\n", " ⏎ "),
                       " | ".join((r.get("unparsed") or []) + (r.get("partial") or [])),
                       " / ".join(r.get("exempt") or []) or "（无）",
                       r.get("pline", 0), r.get("ev", "")))

    # ---- 正文报告（旧版声明了 OUT_MD 却**从来没写过** —— 顺手补上）
    with io.open(OUT_MD, "w", encoding="utf-8", newline="\n") as f:
        f.write("# 中文对账（`zh_crosscheck.py`）—— %s\n\n"
                % ("--legacy 模式（只修读法）" if LEGACY else "当前口径"))
        f.write("输入：`%s` · `%s`\n\n" % (PROBE, CARDS))
        f.write("- 探针 desc 块 **%d** · 有嵌套 op 的 **%d** 张 · 有「别的层」句子的 **%d** 张\n"
                % (len(rows), n_nested, n_other))
        f.write("- 中文标记：若/如果 %d · 效果的「或」 %d · 名词的「或」 %d · 每 %d · 选择 %d · 抽 %d · 替换词 %d\n"
                % (n_cond, n_or, n_or_noun, n_each, n_choose, n_draw, n_rep))
        f.write("- **A 类 %d · B 类 %d · C 类 %d · 其它 %d**\n\n" % (len(A), len(B), len(C), len(Q)))
        f.write("## A 类（真差异候选）\n\n")
        for r in A:
            f.write("- **%s**（probe_out.txt:%d）%s\n" % (r["name"], r.get("pline", 0),
                                                         " / ".join(r["notes"])))
        f.write("\n## B 类（可能差异，要人判）\n\n")
        for r in B:
            f.write("- %s（probe_out.txt:%d）%s\n" % (r["name"], r.get("pline", 0),
                                                      " / ".join(r["notes"])))
        f.write("\n## 其它（卡池对不上 / 中文缺）\n\n")
        for r in Q:
            f.write("- %s :: %s\n" % (r["name"], r["desc"][:80]))
        # 🆕 2026-09-18：**豁免也要看得见**（不许静默丢掉）—— 按「检查项」分组，
        #    每组给条数 + 头几个卡名（全量在 TSV 的「豁免」列里，逐行可查）。
        f.write("\n## 判据豁免（本来会报、被判据挡下来的）\n\n")
        if not exemptions:
            f.write("（无）\n")
        for reason, names in sorted(exemptions.items(), key=lambda kv: -len(kv[1])):
            f.write("- **%s**：%d 条 —— %s%s\n"
                    % (reason, len(names), " · ".join(names[:6]),
                       " …" if len(names) > 6 else ""))
        f.write("\n> 判据、豁免理由与全部改动见 `资料/普查产出_0918/11③_中文对账脚本_修好.md`。\n")
    return 0


if __name__ == "__main__":
    sys.exit(main())
