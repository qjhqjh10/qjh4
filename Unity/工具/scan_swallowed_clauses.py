# -*- coding: utf-8 -*-
"""scan_swallowed_clauses.py —— 从逐句解析探针的输出里挑出「**自称全认了、但子句被吞**」的卡。

为什么要有它（2026-09-16）：
  `EffectParseProbe.Run` 的「卡级 → 不认识 0 · 半懂 0」只说明**没有不认识的段**，
  不代表 desc 里每个子句都变成了 op —— 子句可能被**吞进目标短语或载荷**里，静默消失。
  两个已确认的实例（`资料/阵营推进_清单与交接.md:329-335`）：

    Cadian Honour : `Give +1 Ranged Attack to your troops and reload their Duty abilities`
                    → `give 载荷「+1 ranged attack」 目标[own/troop 「your troops and reload their duty abilities」]`
                      「reload」从来没发生过，而且**不报错**
    Thunderwolf Cavalry Pack Leader :
                    `When an enemy gets a Hunt Mark, give +1 to your units this turn. Rally: …`
                    → `gain 载荷「a hunt mark, give +1 to your units」 目标[enemy/any 「when an enemy」]`
                      条件从句被当成了目标

它们长得一模一样：**探针说「认了」，目标/载荷的引号里却装着一整个从句**。
所以判据就是——把 op dump 里所有 `「…」` 引文抠出来，看它是不是**装进了不该装的东西**：
  · 含 ` and ` / ` or ` / `,` 这类**句子连接符**
  · 以 `when` / `if` / `whenever` / `while` / `until` 开头（**条件从句不许当主语** —— 这条工程里记过）
  · 引文里出现了**第二个动词**（deal/give/gain/heal/draw/…）

⚠️ 这是**候选清单，不是结论**：`your troops and your warlord` 这种正常目标也会被 `and` 命中。
   一条一条判之前，别把它当 bug 列表。

用法：
  # ① 先跑探针（会写 d:/4/_tmp_view/probe_out.txt）
  # ② PYTHONIOENCODING=utf-8 python Unity/工具/scan_swallowed_clauses.py
  # 输出：d:/4/_tmp_view/swallow_candidates.tsv（desc → 卡名 → 被吞的引文 → 触发哪条判据）
"""
import io
import os
import re
import sys

PROBE = "d:/4/_tmp_view/probe_out.txt"
DESC2CARDS = "d:/4/_tmp_view/desc_to_cards.tsv"
OUT = "d:/4/_tmp_view/swallow_candidates.tsv"

# 「引文里不该出现的东西」——命中任意一条就进候选。
# ⚠️ 动词表**只放动作动词**：`attack` / `damage` / `flank` 这类在目标短语里是**名词**
#    （`+2 Ranged Attack`、`a friendly troop with Flank`），放进来会淹掉真信号（第一版就这样，
#    376 处候选里大半是 `载荷「+1 melee attack」` 这种正常引文 —— 2026-09-16 实测）。
VERBS = (
    "deal|give|gain|heal|draw|discard|destroy|reload|summon|create|"
    "move|return|put|sacrifice|reveal|double|reduce|increase|restore|"
    "exhaust|shuffle|choose"
)
COND_HEADS = ("when ", "whenever ", "if ", "while ", "until ", "unless ")
CONNECTORS = (" and ", " or ", ",")
VERB_RE = re.compile(r"(?<![A-Za-z])(" + VERBS + r")(?![A-Za-z])", re.I)

QUOTE_RE = re.compile(r"「([^」]*)」")
CARD_HEAD_RE = re.compile(r"^【(.*)】$")
OPLINE_RE = re.compile(r"^ {6,}(\S.*)$")
# op dump 里目标那一段：`目标[own/troop ×1 「…」]` —— 可能有多个目标
TARGET_RE = re.compile(r"目标\[([^\]]*)\]")
# 载荷那一段：`载荷「…」`
PAYLOAD_RE = re.compile(r"载荷「([^」]*)」")


def load_desc2cards(path):
    m = {}
    if not os.path.exists(path):
        return m
    for line in io.open(path, encoding="utf-8"):
        parts = line.rstrip("\n").split("\t")
        if len(parts) >= 2:
            m[parts[0]] = parts[1]
    return m


def why_target(quote):
    """目标引文 —— 命中的判据。空列表 = 不候选。"""
    hits = []
    low = quote.lower().strip()
    if not low:
        return hits
    for h in COND_HEADS:
        if low.startswith(h):
            hits.append("条件从句当主语")
            break
    vs = {m.group(1).lower() for m in VERB_RE.finditer(quote)}
    if vs:
        # 目标短语本身不该含动作动词 —— 含了就是吞了第二个子句
        hits.append("目标引文含动词 " + "/".join(sorted(vs)))
    for c in CONNECTORS:
        if c in low:
            hits.append("目标引文含连接符 %r" % c.strip())
            break
    return hits


def why_payload(quote):
    """载荷引文 —— 只在「逗号 + 动词」或「条件从句头」时才可疑。

    载荷里出现 `+1 melee attack`、`Flank`、`Hunt Mark` 都是正常的（那是载荷本身）；
    但 `a hunt mark, give +1 to your units` 不是 —— 那是把后面的子句**装进了载荷**。"""
    hits = []
    low = quote.lower().strip()
    for h in COND_HEADS:
        if low.startswith(h):
            hits.append("载荷含条件从句头")
            break
    if ("," in low or " and " in low) and VERB_RE.search(quote):
        vs = {m.group(1).lower() for m in VERB_RE.finditer(quote)}
        hits.append("载荷含子句（连接符+动词 " + "/".join(sorted(vs)) + "）")
    return hits


def main():
    if not os.path.exists(PROBE):
        sys.stderr.write("没有 %s —— 先跑 EffectParseProbe.Run\n" % PROBE)
        return 1
    d2c = load_desc2cards(DESC2CARDS)

    cur = None            # 当前卡的 desc
    total_cards = 0
    clean_cards = 0       # 卡级「不认识 0 · 半懂 0」的
    rows = []
    for raw in io.open(PROBE, encoding="utf-8"):
        line = raw.rstrip("\n")
        m = CARD_HEAD_RE.match(line)
        if m:
            cur = m.group(1)
            total_cards += 1
            continue
        if cur is None:
            continue
        if line.startswith("  卡级"):
            if "不认识 0" in line and "半懂 0" in line:
                clean_cards += 1
            else:
                cur = None      # 这条 card 本来就报着「不认/半懂」，不属本扫描的靶子
            continue
        mo = OPLINE_RE.match(line)
        if not mo:
            continue
        op = mo.group(1)
        for seg in TARGET_RE.findall(op):
            for q in QUOTE_RE.findall(seg):
                hits = why_target(q)
                if hits:
                    rows.append((cur, d2c.get(cur, "?"), "目标:" + q,
                                 "；".join(hits), op.strip()))
        for q in PAYLOAD_RE.findall(op):
            hits = why_payload(q)
            if hits:
                rows.append((cur, d2c.get(cur, "?"), "载荷:" + q,
                             "；".join(hits), op.strip()))

    with io.open(OUT, "w", encoding="utf-8", newline="\n") as f:
        f.write("desc\t卡名\t引文\t命中判据\top行\n")
        for r in rows:
            f.write("\t".join(x.replace("\t", " ") for x in r) + "\n")

    print("扫描：卡 %d 条（其中自称「不认识 0 · 半懂 0」的 %d 条）"
          % (total_cards, clean_cards))
    print("候选 %d 处 → %s" % (len(rows), OUT))
    uniq = []
    for r in rows:
        if r[0] not in uniq:
            uniq.append(r[0])
    print("涉及卡 %d 张" % len(uniq))
    return 0


if __name__ == "__main__":
    sys.exit(main())
