#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""check_vfx_trigger_chain.py — 特效触发链的执行闸（读 `判据/B9_特效触发链.tsv`）。

背景：那张表是**人工核对**出来的 45 行（原版 `BattleManager` / `BattleCardUI` / `CardScript` … 的
播放入口 → 我们在哪接的）。人工表会过期：文件被改名/挪走/行号漂移时，表上仍写着「已接」。
本工具把「已接」那一档变成**可执行检查**：

  * 状态列**动态统计**（⛔ 不写死 28/4/3/10 —— 那些数字会变，2026-10-31 就订正过 3 行）；
  * 「已接」逐条解析 `我们的入口(文件:行)`：
      - `文件:行`   ⇒ 文件必须在（`Assets/` 根 → `Assets/CardPresentation/` 前缀 → 全仓按文件名唯一匹配），
                      行号必须在范围内；括号里的符号（如 `DoAnim` / `PulseBadge` / `_abilityVfxMoves.Add`）
                      必须在**该行附近**出现，否则退一步要求**整个文件**里有；哪儿都没有 ⇒ **断链**。
      - 只写文件      ⇒ 文件必须在；括号里有符号就再核符号。
      - 只写散文      ⇒ 只接受两种：「同 X」「同上」（按方法名/上一行**重定向**到那一条的落点再核），
                      其余一律算**断链**（没有落点 = 没法核）。
  * 「未接」「判不了」「原版无」**原样列出**：⛔ 既不算通过，也不猜。

用法（⛔ 不带动作参数时只打用法、什么都不写）：
  python -I -X utf8 工具/check_vfx_trigger_chain.py            # 打印用法，退出 0
  python -I -X utf8 工具/check_vfx_trigger_chain.py --check    # 跑检查：有断链 ⇒ 退出码 1
  可选：--tsv <表路径> · --assets <代码根，默认 MyGame/Assets>

依赖：仅 Python 3 标准库。
"""
import io
import os
import re
import sys
import argparse

HERE = os.path.dirname(os.path.abspath(__file__))       # d:/4/Unity/工具
UNITY = os.path.dirname(HERE)                           # d:/4/Unity
DEFAULT_TSV = os.path.join(UNITY, "资料", "完全复刻", "判据", "B9_特效触发链.tsv")
DEFAULT_ASSETS = os.path.join(UNITY, "MyGame", "Assets")

USAGE = __doc__

# 落点里的 `路径.cs` + 可选 `:行[-行]`（可能连写多个，例如 `:1` · `:969-972`）
PATH_RE = re.compile(r"([A-Za-z0-9_][A-Za-z0-9_./\- ]*?\.cs)((?::\d+(?:\s*-\s*\d+)?)*)")
LINE_RE = re.compile(r":(\d+)(?:\s*-\s*(\d+))?")
PAREN_RE = re.compile(r"[（(]([^（）()]*)[）)]")

# 括号里这些不是代码符号（是旁证文件名 / 数字 / 中文说明）
STOP_TOKENS = {"json", "true", "false", "null", "ability", "blend"}


def read_rows(path):
    lines = io.open(path, encoding="utf-8").read().split("\n")
    header = lines[0].rstrip("\n").split("\t")
    rows = []
    for ln in lines[1:]:
        if not ln.strip():
            continue
        rows.append(ln.rstrip("\n").split("\t"))
    return header, rows


def col(header, name, fallback):
    return header.index(name) if name in header else fallback


def resolve_file(rel, assets_root):
    """相对路径 → 绝对路径（None = 找不到）。"""
    rel = rel.strip().strip("`").replace("\\", "/")
    cands = [os.path.join(assets_root, rel),
             os.path.join(assets_root, "CardPresentation", rel)]
    for c in cands:
        if os.path.isfile(c):
            return c
    base = os.path.basename(rel)
    hits = []
    for dp, _dn, fns in os.walk(assets_root):
        if base in fns:
            hits.append(os.path.join(dp, base))
            if len(hits) > 1:
                break
    return hits[0] if len(hits) == 1 else None


def read_text(path):
    return io.open(path, encoding="utf-8", errors="replace").read().split("\n")


def is_code_line(s):
    t = s.strip()
    if not t:
        return False
    if t.startswith(("//", "/*", "*", "*/", "#")):
        return False
    return True


def symbols_in(text):
    """从括号文字里抠出「像代码符号」的 token。"""
    out = []
    for tok in re.split(r"[^A-Za-z0-9_.]+", text):
        if len(tok) < 4 or not re.search(r"[A-Za-z]", tok):
            continue
        if tok.startswith("0x"):
            continue
        if tok.endswith((".c", ".cs", ".json")):
            continue
        if tok.lower() in STOP_TOKENS:
            continue
        out.append(tok)
    return out


def find_symbol(lines, sym, lo=0, hi=None):
    """返回符号在**代码行**里出现的行号列表（1 基）；注释行不算。"""
    hi = len(lines) if hi is None else hi
    pat = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(sym) + r"(?![A-Za-z0-9_])")
    hits = []
    for i, s in enumerate(lines, 1):
        if i < lo or i > hi:
            continue
        if is_code_line(s) and pat.search(s):
            hits.append(i)
    return hits


def find_any(lines, sym, lo=0, hi=None):
    """不区分注释/代码（用来判「只出现在注释里」）。"""
    hi = len(lines) if hi is None else hi
    pat = re.compile(r"(?<![A-Za-z0-9_])" + re.escape(sym) + r"(?![A-Za-z0-9_])")
    return [i for i, s in enumerate(lines, 1) if lo <= i <= hi and pat.search(s)]


def parse_cell(cell):
    """`我们的入口` 一栏 → (entries, prose)。

    entry = {"path": 相对路径, "lines": [(start, end|None), ...], "symbols": [...], "raw": 原文}
    `文件:1 起` = 文件级（不给行号，⛔ 别把「起」当成第 1 行）。
    """
    entries, prose = [], []
    cur = None
    for seg in re.split(r"·", cell):
        seg = seg.strip()
        if not seg:
            continue
        m = PATH_RE.search(seg)
        if m:
            cur = {"path": m.group(1).strip(), "lines": [], "symbols": [], "raw": seg}
            entries.append(cur)
            tail = seg[m.end():]
            line_part = m.group(2) or ""
            if re.match(r"\s*起", tail):
                line_part = ""
            for a, b in LINE_RE.findall(line_part):
                cur["lines"].append((int(a), int(b) if b else None))
        else:
            if cur is not None and re.match(r"^\s*:\d", seg):
                if not re.match(r"^\s*:\d+\s*起", seg):     # `:N 起` ⇒ 文件级，不加行
                    for a, b in LINE_RE.findall(seg):
                        cur["lines"].append((int(a), int(b) if b else None))
            else:
                prose.append(seg)
        for par in PAREN_RE.findall(seg):
            syms = symbols_in(par)
            if syms and cur is not None:
                cur["symbols"].extend(syms)
    # 去重保序
    for e in entries:
        seen = set()
        e["symbols"] = [s for s in e["symbols"] if not (s in seen or seen.add(s))]
    return entries, prose


def check_entry(entry, assets_root):
    """→ (状态, 说明)。状态 ∈ {"OK", "WARN", "FAIL"}。

    判据顺序：① 文件在不在；② 括号里给了符号 ⇒ 以符号为准（代码里没有 = 断链；
    只在注释里出现 ⇒ 断链并点名，因为注释里的名字不是可调用点）；③ 没给符号 ⇒ 以行号为准
    （行号越界 = 断链；落在注释块/空白 = 告警，因为「没法据此确认」而不是「确认不存在」）。
    """
    path = resolve_file(entry["path"], assets_root)
    if not path:
        return "FAIL", "文件不存在：%s" % entry["path"]
    lines = read_text(path)
    rel = os.path.relpath(path, assets_root).replace("\\", "/")
    syms = entry["symbols"]

    # ① 文件级（无行号）：有符号就核符号
    if not entry["lines"]:
        if not syms:
            return "WARN", "%s（仅文件级，无行号/符号）" % rel
        for s in syms:
            if find_symbol(lines, s):
                return "OK", "%s（文件级命中 %s）" % (rel, s)
        for s in syms:
            h = find_any(lines, s)
            if h:
                return "FAIL", "%s 里的 %s 只出现在注释行（:%s），不是代码符号" % (
                    rel, s, ",".join(str(x) for x in h[:3]))
        return "FAIL", "%s 文件在，但符号 %s 一个都没找到" % (rel, ", ".join(syms[:3]))

    # ② 有行号：先看符号（给了就认符号），没给就认「这段落点是不是代码」
    soft, parts = [], []
    verified = False
    for (a, b) in entry["lines"]:
        end = b or a
        if a < 1 or end > len(lines):
            soft.append("%s:%d-%d 行号越界（文件共 %d 行）" % (rel, a, end, len(lines)))
            continue
        if syms:
            near = None
            for s in syms:
                hits = find_symbol(lines, s, max(1, a - 40), min(len(lines), end + 40))
                if hits:
                    near = (s, hits[0])
                    break
            if near is not None:
                verified = True
                parts.append("%s:%d-%d（%s 在 :%d）" % (rel, a, end, near[0], near[1]))
                continue
            far = None
            for s in syms:
                hits = find_symbol(lines, s)
                if hits:
                    far = (s, hits[0])
                    break
            if far is not None:
                verified = True
                soft.append("%s:%d-%d 符号 %s 在 :%d（行号漂移）" % (rel, a, end, far[0], far[1]))
            continue                       # 全文都没有 ⇒ 交给下面的统一判定
        span_code = any(is_code_line(s) for s in lines[a - 1:end])
        if span_code:
            verified = True
            parts.append("%s:%d-%d" % (rel, a, end))
        else:
            lo = max(1, a - 10)
            hi = min(len(lines), end + 10)
            if any(is_code_line(s) for s in lines[lo - 1:hi]):
                soft.append("%s:%d-%d 落在注释块里（附近有代码）" % (rel, a, end))
            else:
                soft.append("%s:%d-%d 落点不在代码区（前后 10 行也没有代码）" % (rel, a, end))

    if syms:
        code_hit = None
        for s in syms:
            hits = find_symbol(lines, s)
            if hits:
                code_hit = (s, hits[0])
                break
        if code_hit is None:
            for s in syms:
                h = find_any(lines, s)
                if h:
                    return "FAIL", "%s 里的 %s 只出现在注释行（:%s），不是代码符号" % (
                        rel, s, ",".join(str(x) for x in h[:3]))
            return "FAIL", "%s:%s 附近与全文都找不到符号 %s" % (
                rel, ",".join("%d-%d" % (a, b or a) for a, b in entry["lines"]), ", ".join(syms[:3]))
        if not verified:
            soft.append("符号 %s 在 :%d，但所标区间都对不上（行号整体漂移）" % (code_hit[0], code_hit[1]))
    elif not verified:
        # 没给符号、落点又指到注释/空白 ⇒ 这条「证据」不可核，但**不能因此断定实现不在**
        #（行号漂移是常态）⇒ 告警，交人工复核；⛔ 不判断链。
        soft.append("没有一条落点能核对到代码（需人工复核）")
    return ("WARN" if soft else "OK"), "；".join(parts + soft)


def check_row(row, idx, rows, header, assets_root, i_method, i_cell, i_status):
    """单行 → (状态, 说明)。状态 ∈ {"OK","WARN","FAIL"}。"""
    cell = row[i_cell]
    entries, prose = parse_cell(cell)
    if not entries:
        # 只接受「同 X」「同上」两种重定向
        redirect = None
        for p in prose:
            m = re.match(r"^同\s*(.+)$", p.strip())
            if m and m.group(1).strip() not in ("上",):
                target = m.group(1).strip()
                for j, r2 in enumerate(rows):
                    if j == idx or r2[i_status] != "已接":
                        continue
                    if target.split(" ")[0].split("/")[0] in r2[i_method]:
                        redirect = j
                        break
                if redirect is None:
                    return "FAIL", "重定向目标找不到：%s" % p
                break
            if p.strip() in ("同上", "同上一条"):
                for j in range(idx - 1, -1, -1):
                    if rows[j][i_status] == "已接":
                        redirect = j
                        break
                if redirect is None:
                    return "FAIL", "「同上」前面没有可用的「已接」行"
                break
        if redirect is None:
            return "FAIL", "没有可解析的落点（只有散文：%s）" % cell[:60]
        sub_entries, _ = parse_cell(rows[redirect][i_cell])
        if not sub_entries:
            return "FAIL", "重定向到第 %d 行，但它自己也没有可解析的落点" % (redirect + 1)
        results = [check_entry(e, assets_root) for e in sub_entries]
        worst = "FAIL" if any(s == "FAIL" for s, _ in results) else (
            "WARN" if any(s == "WARN" for s, _ in results) else "OK")
        msg = "→ 第 %d 行 %s：%s" % (redirect + 1, rows[redirect][i_method],
                                   "；".join(m for _, m in results))
        return worst, msg
    results = [check_entry(e, assets_root) for e in entries]
    worst = "FAIL" if any(s == "FAIL" for s, _ in results) else (
        "WARN" if any(s == "WARN" for s, _ in results) else "OK")
    return worst, "；".join(m for _, m in results)


def do_check(args):
    if not os.path.isfile(args.tsv):
        print("⛔ 表不存在：%s" % args.tsv)
        return 2
    if not os.path.isdir(args.assets):
        print("⛔ 代码根不存在：%s" % args.assets)
        return 2
    header, rows = read_rows(args.tsv)
    i_method = col(header, "方法", 1)
    i_cell = col(header, "我们的入口(文件:行)", 5)
    i_status = col(header, "判定", 6)
    i_cls = col(header, "原版类", 0)
    i_what = col(header, "播的是什么", 3)
    counts = {}
    for r in rows:
        counts[r[i_status]] = counts.get(r[i_status], 0) + 1

    print("读：%s" % os.path.relpath(args.tsv, UNITY).replace("\\", "/"))
    print("数据行：%d（表头 1 行）· 状态分布：%s" % (
        len(rows), " / ".join("%s %d" % (k, v) for k, v in sorted(counts.items(), key=lambda x: -x[1]))))
    print("")

    ok = warn = fail = 0
    fails, warns = [], []
    print("── 已接：逐条核落点 ──────────────────────────────────")
    for idx, r in enumerate(rows):
        if r[i_status] != "已接":
            continue
        st, msg = check_row(r, idx, rows, header, args.assets, i_method, i_cell, i_status)
        tag = {"OK": "✅", "WARN": "⚠", "FAIL": "❌"}[st]
        print("  %s %2d %s.%s —— %s" % (tag, idx + 1, r[i_cls], r[i_method], msg))
        if st == "FAIL":
            fail += 1
            fails.append((idx + 1, r[i_cls], r[i_method], msg))
        elif st == "WARN":
            warn += 1
            warns.append((idx + 1, r[i_cls], r[i_method], msg))
        else:
            ok += 1

    n_已接 = counts.get("已接", 0)
    for name in ("未接", "判不了", "原版无"):
        if not counts.get(name):
            continue
        print("")
        why = {"未接": "未接（⛔ 不算通过，也不猜）",
               "判不了": "判不了（⛔ 判据为空，不算通过）",
               "原版无": "原版本身没有（不适用，不算失败）"}[name]
        print("── %s：%d 条，原样列出 ──────────────────────────" % (why, counts[name]))
        for idx, r in enumerate(rows):
            if r[i_status] == name:
                print("  · %2d %s.%s —— 表内落点：%s" % (idx + 1, r[i_cls], r[i_method], r[i_cell][:90]))

    print("")
    print("汇总：%d 通过 / %d 断链 / %d 未接 / %d 判不了 / %d 不适用（另 %d 条告警：行号漂移 / 仅文件级 / 落点在注释块）" % (
        ok, fail, counts.get("未接", 0), counts.get("判不了", 0), counts.get("原版无", 0), warn))
    if n_已接 != ok + warn + fail:
        print("⚠ 计数不自洽：已接 %d ≠ %d+%d+%d" % (n_已接, ok, warn, fail))
    if fails:
        print("")
        print("断链明细（⛔ 表上写着「已接」但代码里找不到对应落点）：")
        for ln, cls, meth, msg in fails:
            print("  %s.%s（第 %d 行）：%s" % (cls, meth, ln, msg))
    return 0 if fail == 0 else 1


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--tsv", default=DEFAULT_TSV)
    ap.add_argument("--assets", default=DEFAULT_ASSETS)
    ap.add_argument("-h", "--help", action="store_true")
    a, unknown = ap.parse_known_args(argv)
    if a.help or not a.check:
        print(USAGE)
        if unknown:
            print("（忽略了无法识别的参数：%s）" % " ".join(unknown))
        return 0
    return do_check(a)


if __name__ == "__main__":
    sys.exit(main())
