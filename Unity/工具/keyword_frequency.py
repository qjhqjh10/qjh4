#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
关键词频次统计 —— 每个关键词在卡池里被多少张卡用到。

用途：给「下一批补哪些关键词机制」排优先级。
⚠️ 本脚本**只统计频次**，**不做「实现没实现」的判断**（那是别的对账的事）。

复刻依据（照铁律 2：先读资源，不猜）：
  · 卡池      d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json（1130 张）
  · 关键词全集 d:/4/Unity/资料/关键词图标/_规则书关键词表.md（规则书 61 个，中英对照）
  · 别名依据   d:/4/Unity/资料/关键词图标/关键词与图标_对照表.md（原版图集 sprite 名 = 英文关键词）

两个口径（差就是数据缺口）：
  ① 只在 `keywords` 数组里的 —— 引擎现在真正读得到的
  ② `keywords` ∪ `desc` 都算的 —— 卡面写了但数据没进数组的也算

归一化（按题目要求）：
  去方括号 → 在第一个数字或冒号处截断 → 去掉所有非字母数字 → 转小写。
  例：'Armour 1' 'Armour 2.' → 'armour'；'Companion 2: Missile Drone' → 'companion'；
      'Talent: Witchfire' → 'talent'；'Blood Thirst' 'Bloodthirst' → 'bloodthirst'。

防误报（题目硬性要求「别把战术卡描述里的普通英文词当成关键词」）：
  · desc 侧**只扫**规则书 61 个关键词（含别名），在 keywords 数组里出现过但不在 61 表里的名字
    **只从数组侧统计**，不扫 desc —— 那些名字（Warlord / Secret / Chaos …）当普通英文词扫会误报
    （实测 `Warlord` 若扫 desc 会从 1 张涨到 53 张，全是卡文里的「敌方督军」）。
    这个口径的盲点已核查：**desc 的「关键词位置」里出现的名字，除这 61 个以外只有
    `Choose one` / `Your Warlord gains` 两个，都是非关键词**。见本文档末尾自检说明。
  · desc 侧**方括号外按原样大小写匹配** —— 实测卡面 100% 写 'Flying' 'Rally' 'Shield'，
    小写的 flying/shield 只出现在 `[shield]` `[faith]` 这类图标方括号里（方括号走单独一遍，
    大小写不敏感）。这一条把「普通英文词」的误报压到 0。
  · 例外：美式拼写 'Armor'/'armor' 两种大小写都有，那一个 pattern 单独开 ignore-case。

用法（幂等、可重跑，不依赖上一次的产物）：
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/keyword_frequency.py
"""

import json
import os
import re
from collections import Counter, defaultdict

# ---------------------------------------------------------------- 路径
ROOT = "d:/4"
CARDS_JSON = ROOT + "/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
RULEBOOK_MD = ROOT + "/Unity/资料/关键词图标/_规则书关键词表.md"
OUT_MD = ROOT + "/Unity/资料/关键词频次表.md"
OUT_TSV = ROOT + "/_tmp_view/keyword_frequency.tsv"
MD_LINE_LIMIT = 150


# ---------------------------------------------------------------- 归一化
def norm(s):
    """取关键词名部分：去方括号 → 截到第一个数字/冒号 → 只留字母数字 → 小写。"""
    if not s:
        return ""
    s = s.strip()
    s = re.sub(r"^\[|\]$", "", s)
    s = re.sub(r"[0-9].*$", "", s)      # 'Armour 2' -> 'Armour'
    s = s.split(":")[0]                 # 'Companion 2: Missile Drone' -> 'Companion '
    s = re.sub(r"[^A-Za-z]", "", s)
    return s.lower()


# 归一化后同名、实际是同一个关键词的（每条都有依据，不是猜的）
ALIASES = {
    # 图集里叫 concussive.png，规则书表格里写 Concussion —— 同一个关键词（对照表 :92 已记）
    "concussive": "concussion",
    # 规则书条目名是复数 Dark Pacts，卡面单复数混用；'of Excess'/'of Resilience' 是 4 种增益的载荷
    "darkpact": "darkpacts",
    "darkpactofexcess": "darkpacts",
    "darkpactofresilience": "darkpacts",
    # 美式拼写（desc 里 '+N Armor' 与 '[Armor]' 图标）
    "armor": "armour",
}

# 同一关键词在卡面的另一种拼法（大小写敏感的 desc 匹配用；'Armor' 那条例外开忽略大小写）
# 词尾变化的那几条是**全池扫出来的**（卡面在别处还会写 Spirit Stones / Remnants / Prays / Praying），
# 不写死就会漏；没有用「通用加 s」的规则，怕把普通英文词扫进来。
EXTRA_SPELLINGS = {
    "armour": ["Armor"],
    "concussion": ["Concussive"],
    "darkpacts": ["Dark Pact"],
    "bloodthirst": ["Bloodthirst"],
    "spiritstone": ["Spirit Stones"],
    "remnant": ["Remnants"],
    "pray": ["Prays", "Praying"],
}
CASE_INSENSITIVE_SPELLINGS = {"Armor"}


# ---------------------------------------------------------------- 读规则书关键词表
def load_rulebook(path):
    """返回 [(归一化键, 英文名, 中文名)]，共 61 条。第 4 格是行号的那张表才是关键词表。"""
    rows = []
    with open(path, encoding="utf-8") as fh:
        for line in fh:
            if not line.strip().startswith("|"):
                continue
            cells = [c.strip() for c in line.strip().strip("|").split("|")]
            if len(cells) != 6 or not re.fullmatch(r"1[6-9]\d|2[0-2]\d", cells[3]):
                continue
            rows.append((norm(cells[1]), cells[1], cells[0]))
    return rows


# ---------------------------------------------------------------- 主流程
def main():
    rulebook = load_rulebook(RULEBOOK_MD)
    canonical = {k: (en, zh) for k, en, zh in rulebook}      # 61 表
    in_book = {k for k, _, _ in rulebook}

    with open(CARDS_JSON, encoding="utf-8") as fh:
        data = json.load(fh)
    cards = data["cards"]

    # ---- 先扫一遍 keywords 数组，收集规则书没收的名字（后面单列一栏）
    extras = {}          # key -> 第一个见到的原始写法
    for card in cards:
        for raw in (card.get("keywords") or []):
            key = ALIASES.get(norm(raw), norm(raw))
            if key and key not in in_book and key not in extras:
                extras[key] = raw

    # ---- 每个键的 desc 匹配 pattern
    def spellings(key):
        if key in canonical:
            base = [canonical[key][0]]
        else:
            base = [extras[key]]
        base = base + EXTRA_SPELLINGS.get(key, [])
        pats = []
        for sp in base:
            flags = re.I if sp in CASE_INSENSITIVE_SPELLINGS else 0
            pats.append(re.compile(r"(?<![A-Za-z])" + re.escape(sp) + r"(?![A-Za-z])", flags))
        return pats

    all_keys = set(canonical) | set(extras)
    # desc 侧只认规则书 61 个（含别名）—— 理由见文件头「防误报」
    DESC_PATS = {k: spellings(k) for k in canonical if k in all_keys}
    DESC_KEYS = set(DESC_PATS)

    def keys_from_array(card):
        out = set()
        for raw in (card.get("keywords") or []):
            k = ALIASES.get(norm(raw), norm(raw))
            if k:
                out.add(k)
        return out

    def keys_from_desc(card):
        text = card.get("desc") or ""
        text = text.replace("\u2019", "'").replace("\u2018", "'")
        out = set()
        for tok in re.findall(r"\[([^\]]*)\]", text):        # 方括号图标形态，大小写不敏感
            k = ALIASES.get(norm(tok), norm(tok))
            if k in DESC_KEYS:
                out.add(k)
        bare = re.sub(r"\[[^\]]*\]", " ", text)
        for k, pats in DESC_PATS.items():
            for pat in pats:
                if pat.search(bare):
                    out.add(k)
                    break
        return out

    # ---- 聚合
    stat = defaultdict(lambda: {"cards": 0, "kw_only": 0, "desc_only": 0, "both": 0})
    factions = defaultdict(Counter)
    ex_card = {}
    ex_raw = {}
    raw_forms = defaultdict(set)

    for card in cards:
        kws = keys_from_array(card)
        dsc = keys_from_desc(card)
        for raw in (card.get("keywords") or []):
            k = ALIASES.get(norm(raw), norm(raw))
            if k:
                raw_forms[k].add(raw)
        for k in kws | dsc:
            s = stat[k]
            s["cards"] += 1
            factions[k][card.get("faction") or "?"] += 1
            if k in kws and k in dsc:
                s["both"] += 1
            elif k in kws:
                s["kw_only"] += 1
            else:
                s["desc_only"] += 1
            # 示例优先取「写在 keywords 数组里」的；数组里没有才用 desc 片段
            if k in kws:
                if k not in ex_raw or ex_raw[k].startswith("desc:"):
                    idx = [ALIASES.get(norm(x), norm(x)) for x in card["keywords"]].index(k)
                    ex_raw[k] = card["keywords"][idx]
                    ex_card[k] = card["name"]
            elif k not in ex_raw:
                m = None
                for pat in DESC_PATS.get(k, []):
                    m = pat.search(card.get("desc") or "")
                    if m:
                        break
                txt = (card.get("desc") or "").replace("\n", " ")
                if m:
                    a = max(0, m.start() - 24)
                    ex_raw[k] = "desc: …" + txt[a:m.end() + 34]
                else:
                    ex_raw[k] = "desc: " + txt[:60]
                ex_card[k] = card["name"]

    rows = []
    for k in all_keys:
        s = stat.get(k)
        if not s or s["cards"] == 0:
            continue
        rows.append({
            "key": k,
            "name": canonical[k][0] if k in canonical else extras[k],
            "in_book": k in in_book,
            "cards": s["cards"],
            "kw_only": s["kw_only"],
            "desc_only": s["desc_only"],
            "both": s["both"],
            "factions": factions[k],
            "ex_card": ex_card.get(k, ""),
            "ex_raw": ex_raw.get(k, ""),
            "forms": sorted(raw_forms.get(k, [])),
        })
    rows.sort(key=lambda r: (-r["cards"], r["key"]))

    zero = sorted(canonical[k][0] for k in canonical if k not in stat)
    desc_only_keys = [r for r in rows if r["kw_only"] == 0 and r["cards"] > 0]
    both_keys = [r for r in rows if r["both"] > 0 and r["kw_only"] == 0]

    # ---- 写 TSV（原始口径，字段全）
    os.makedirs(os.path.dirname(OUT_TSV), exist_ok=True)
    with open(OUT_TSV, "w", encoding="utf-8", newline="") as fh:
        fh.write("key\tname\tin_rulebook61\tcards\tkw_only\tdesc_only\tboth\tfactions_all\texample_card\texample_raw\traw_forms_in_keywords_array\n")
        for r in rows:
            fh.write("\t".join([
                r["key"], r["name"], "1" if r["in_book"] else "0",
                str(r["cards"]), str(r["kw_only"]), str(r["desc_only"]), str(r["both"]),
                ",".join("%s:%d" % (f, n) for f, n in r["factions"].most_common()),
                r["ex_card"], r["ex_raw"], " | ".join(r["forms"]),
            ]) + "\n")

    # ---- 写 md（行数上限 MD_LINE_LIMIT，超了打印警告）
    def wrap(prefix, items, per_line=6):
        """把一串短语按每行 per_line 个折行，省行数。"""
        out, buf = [], []
        for it in items:
            buf.append(it)
            if len(buf) == per_line:
                out.append("　".join(buf))
                buf = []
        if buf:
            out.append("　".join(buf))
        return [prefix + out[0]] + ["　" + x for x in out[1:]] if out else [prefix]

    L = []
    L.append("# 关键词在卡池里的频次（%d 张，2026-09-13）" % len(cards))
    L.append("> 生成脚本 `Unity/工具/keyword_frequency.py`（可重跑）"
             "　⚠️ 只统计频次，**不做「实现没实现」的判断**（那是别的对账的事）")
    L.append("> **两个口径**：① 只在 `keywords` 数组里（引擎现在读得到的）"
             " ② `keywords` ∪ `desc`（卡面写了就算）。**两者的差 = 数据缺口**。")
    L.append("> **归一化**：去方括号 → 在第一个数字或冒号处截断 → 去非字母数字 → 小写。")
    L.append("> **desc 侧口径**：只认规则书 61 个（含别名），方括号外按原样大小写匹配"
             "（实测卡面写法统一）；数组独有、不在 61 表的名字不扫 desc（会误报普通英文词）。")
    L.append("> ⚠️ desc 里含「if it has X」这类**引用**（不是这张卡自己有 X），"
             "所以「只在 desc」这一列是缺口的**上界**。")
    L.append("")
    L.append("| 关键词 | 卡数 | 只在keywords | 只在desc | 两者都有 | 主要阵营 | 示例卡名 | 示例写法 |")
    L.append("|---|---|---|---|---|---|---|---|")
    for r in rows:
        top = r["factions"].most_common(3)
        fac = " · ".join(f for f, _ in top)
        if len(r["factions"]) > 3:
            fac += " +%d" % (len(r["factions"]) - 3)
        name = r["name"] + ("" if r["in_book"] else " *")
        ex = r["ex_raw"].replace("|", "/")
        if len(ex) > 46:
            ex = ex[:45] + "…"
        L.append("| %s | %d | %d | %d | %d | %s | %s | `%s` |" % (
            name, r["cards"], r["kw_only"], r["desc_only"], r["both"], fac,
            r["ex_card"].replace("|", "/"), ex))
    L.append("")
    extra_rows = [r for r in rows if not r["in_book"]]
    L.append("`*` = **只在 `keywords` 数组里出现、不在规则书 61 表**（原版数字版独有，或**不是关键词**"
             "而是被塞进数组的其它标签），共 %d 个：" % len(extra_rows))
    L += wrap("", ["%s(%d)" % (r["name"], r["cards"]) for r in
                   sorted(extra_rows, key=lambda z: -z["cards"])], per_line=7)
    L.append("　其中大多数是**督军天赋名被拆进来了**（`Talent: X` 的 X）或阵营/兵种标签"
             "（`Chaos` / `Ultramarines` / `Warlord` …），**不是机制关键词**，别按它们排机制优先级。")
    if zero:
        L.append("　**规则书 61 个里、池子里一张卡都没用的（%d 个）**：" % len(zero) + "　".join(zero))
    L.append("")
    L.append("## ⚠️ 只在 desc、不在 keywords 的（数据缺口）")
    L.append("")
    gap = sorted([r for r in rows if r["desc_only"] > 0], key=lambda r: -r["desc_only"])
    L.append("卡面写了、引擎 `keywords` 数组里读不到的，共 **%d 个关键词 / %d 张卡次**"
             "（上界，含引用）：" % (len(gap), sum(r["desc_only"] for r in gap)))
    L += wrap("", ["**%s** %d/%d" % (r["name"], r["desc_only"], r["cards"]) for r in gap], per_line=8)
    whole = [r for r in gap if r["kw_only"] == 0]
    if whole:
        L.append("")
        L.append("🔴 **一次都没进过 `keywords` 数组的**（%d 个）：%s" % (
            len(whole), " · ".join("%s(%d)" % (r["name"], r["desc_only"]) for r in whole)))
    L.append("")
    L.append("## ⚠️ 归一化时遇到的怪写法")
    L.append("")
    L.append("同一关键词在 `keywords` 数组里长得不一样、归一化后才同名（各给一个原文）：")
    L.append("")
    weird = [(r["name"], r["forms"]) for r in rows if len(r["forms"]) > 1]
    weird.sort(key=lambda z: -len(z[1]))
    for name, forms in weird:
        tail = " …共%d种" % len(forms) if len(forms) > 4 else ""
        L.append("- **%s**：%s%s" % (name, " / ".join("`%s`" % f for f in forms[:4]), tail))
    L.append("")
    L.append("归一化**没归到一起**、靠脚本里的 `ALIASES` 手动合的（每条都有依据）：")
    L.append("")
    L.append("- `Concussive` → **Concussion**（图集 sprite 就叫 `concussive.png`，规则书写 Concussion）")
    L.append("- `Dark Pact` / `Dark Pact of Excess` / `Dark Pact of Resilience` → **Dark Pacts**"
             "（规则书条目是复数；of X 是 4 种增益的载荷）")
    L.append("- `Armor` / `armor` / `[Armor]` → **Armour**（美式拼写；卡池里没有独立的「护甲属性」字段）")
    L.append("- 尾点：`Armour 2.` `Remnant.` `Stealth.` `Vanguard.` `Destroyer.` —— 抄录时把句号带进了数组")
    L.append("- 冒号后是**载荷**不是新关键词：`Talent: Witchfire` `Rally: Deal 1 damage…` `Mob: Gain +1 armor…`；"
             "数字是参数：`Companion 2: Missile Drone` `Blast 2` `Tide 1`")
    L.append("")
    L.append("> 大小写：任务里提到的 `hunt mark` 小写形态**在这份数据里不存在**（全文件只有 `Hunt Mark`，34 处）；"
             "`desc` 里的关键词写法实测 100% 首字母大写，小写只出现在 `[shield]` `[faith]` `[armor]` 图标方括号内。")
    L.append("> **词尾变化**：`desc` 侧另收了池子里仅有的 4 种变形 —— `Spirit Stones`(5) · `Remnants`(2) · "
             "`Prays`(1) · `Praying`(1)；其余关键词在卡面只用原形。")
    L.append("> **盲点自检**：desc 的「关键词位置」里出现的名字，除这 61 个以外只有 `Choose one` / "
             "`Your Warlord gains` 两个 —— 所以「不扫数组独有名字」不会漏掉真关键词。")

    if len(L) > MD_LINE_LIMIT:
        print("WARN: md 行数 %d 超过上限 %d" % (len(L), MD_LINE_LIMIT))
    with open(OUT_MD, "w", encoding="utf-8", newline="\n") as fh:
        fh.write("\n".join(L) + "\n")

    # ---- 控制台只打 ASCII（Windows 控制台是 GBK）
    print("cards=%d  keywords_rows=%d  desc_gap_keys=%d  md_lines=%d"
          % (len(cards), len(rows), len(gap), len(L)))
    print("top10: " + ", ".join("%s=%d" % (r["name"], r["cards"]) for r in rows[:10]))
    print("out: %s" % OUT_MD)
    print("out: %s" % OUT_TSV)


if __name__ == "__main__":
    main()
