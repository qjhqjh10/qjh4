#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""重算「功能点级」那把尺子：按【现账】逐 FP 取最差档。

## 口径（沿用 E9 的读法，⛔ 别自创）
- **FP 等级 = 该 FP 构成行里【最差的那一条】**，但 **D 不参与竞争** ——
  D = 原版不可达 / 不复刻，它不阻塞玩家用上这个功能点。
  顺序：任一 C ⇒ C · 否则任一 B ⇒ B · 否则 A。**所有构成行都是 D ⇒ D**。
- ⚠️ 这与「行级真复刻率 A/(A+B+C)」是两把尺子，⛔ 别混（行级测「读懂了多少」）。

## 用法（默认只打印，⛔ 不写盘）
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py            # 只看统计与变化
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --verbose  # 逐 FP 打旧→新
    python -I -X utf8 d:/4/Unity/工具/recompute_fp_levels.py --apply    # 写回 part.tsv 的「等级」列（显式动作）

## 三条硬规矩（本仓踩过）
1. **二进制读、二进制写**：照抄每份文件自己的行尾；⛔ 别用文本模式（会把 CRLF 翻成 LF）。
2. **先算进变量、确认无误再写盘**（`write(<表达式>)` 会先截断再求值，异常 = 文件空掉）。
3. `--apply` 只改**第 9 列（等级）**，其余列逐字不动。

## 🔧 2026-10-30 B101_R6 修的三处（详见 `报告/B101_R6_重复账与FP.md` §六）
- **`类.方法`（无括号）**：设计文 §1.1 允许「同名重载全算」，`feature_index_check.py` 已实现，
  本脚本原来只做精确匹配 ⇒ 按 `类.方法(` 前缀展开（商店域 45 条 missing 归零）。
- **`类<…,...>.*` 截断前缀**：原来按字符串前缀比，永远 0 命中且不报 missing ⇒ 三档查
  （逐字类名 → 泛型族 → 同名全族）；0 命中会打印，⛔ 不静默。
- **`E` 档**：`worst()` 原来没有 E 分支 ⇒ E 与 A 混在一起时会被静默丢掉。现在 E/未知档
  **不进 worst**、逐条打警告；⛔ 本脚本不替 E 定语义（账里现读 0 行带 E）。
"""
import glob
import io
import os
import sys

LEDGER_DIR = r"d:/4/Unity/资料/完全复刻/账"
IDX_GLOB = os.path.join(r"d:/4/Unity/资料/完全复刻/判据", "功能点索引_*_part.tsv")


def read_raw(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    if b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n"):
        return text.split("\r\n"), "\r\n"
    return text.split("\n"), "\n"


def strip_generics(s):
    """🔧 2026-10-30 B101_R6：剥掉成对的 `<...>`；**容忍未闭合**（FP 里 `RankedEventBase<TSave,.*`
    这种被截断的前缀，右括号不存在）⇒ 未闭合的尾巴连内容一起丢掉。
    ⚠️ 只用于**类名**比较，⛔ 不用来改账键。"""
    out = []
    depth = 0
    for ch in s:
        if ch == "<":
            depth += 1
        elif ch == ">":
            if depth > 0:
                depth -= 1
            else:
                out.append(ch)
        else:
            if depth == 0:
                out.append(ch)
    return "".join(out)


def load_ledger():
    """返回 (精确键表, 按【剥泛型后的类名】分组表, 账数据行总数)。
    - 键表：`原版类.方法` → [(状态, 文件, 行号), ...]（行号用于按行去重计数）
    - 分组表：`strip_generics(原版类)` → [(状态, 文件, 行号), ...]（供 `类.*` 通配用）
    """
    m = {}
    by_cls = {}
    by_cls_generic = {}
    by_exact_cls = {}
    total = 0
    for p in sorted(glob.glob(os.path.join(LEDGER_DIR, "*.tsv"))):
        if os.path.basename(p).startswith("_"):
            continue
        lines, _ = read_raw(p)
        if not lines:
            continue
        hdr = lines[0].split("\t")
        if "状态" not in hdr or "方法" not in hdr or "原版类" not in hdr:
            continue
        i_st, i_me, i_cl = hdr.index("状态"), hdr.index("方法"), hdr.index("原版类")
        base = os.path.basename(p)
        for ln, l in enumerate(lines[1:], 2):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_st, i_me, i_cl):
                continue
            total += 1
            cls, meth = c[i_cl].strip(), c[i_me].strip()
            row = (c[i_st].strip(), base, ln)
            m.setdefault(cls + "." + meth, []).append(row)
            by_exact_cls.setdefault(cls, []).append(row)                 # 原版类【逐字】分组
            by_cls.setdefault(strip_generics(cls).strip(), []).append(row)  # 剥泛型后的类名分组
            if "<" in cls:
                # 只收「泛型写法」的那些类 —— 修 `LiveOpsEvent<TSave,.*` 这类被截断的前缀时
                # 只认泛型族，⛔ 不把非泛型 `LiveOpsEvent` 也算进来（否则与 `LiveOpsEvent.*` 重复）
                by_cls_generic.setdefault(strip_generics(cls).strip(), []).append(row)
    return m, by_exact_cls, by_cls, by_cls_generic, total


def expand_no_paren(item, ledger):
    """🔧 2026-10-30 B101_R6：FP 的「原版构成」允许写 `类.方法`（同名的所有重载都算进来，
    见 `资料/完全复刻/判据/功能点验收索引_设计与样板.md` §1.1 第 6 列）。
    旧版只做 `ledger.get(item)` 精确匹配 ⇒ 这种写法永远找不到（商店域 45 条全栽在这）。
    这里按 `类.方法(` 前缀展开；`(` 作边界，⛔ 不会把 `SetupX` 算进 `Setup`。
    返回 [(状态, 文件), ...]，没命中返回 None。"""
    prefix = item + "("
    hits = []
    for k, v in ledger.items():
        if k.startswith(prefix):
            hits.extend(v)
    return hits or None


KNOWN_STATES = ("A", "B", "C", "D")


def split_unknown(states):
    """🔧 2026-10-30 B101_R6：`E`（及一切非 ABCD）**不进 worst**。
    ⛔ 本脚本不替 E 定语义 —— 设计文 §1.1 第 9 列虽然写了 `E > C > B > A`，
    但账里现读 0 行带 E，这条规则**还没落成可执行的东西**。
    ⇒ 这里把它当【未知】：出现就打警告（⛔ 不许静默），既不升档也不降档。"""
    known, unknown = [], []
    for s in states:
        if not s:
            continue
        if s in KNOWN_STATES:
            known.append(s)
        else:
            unknown.append(s)
    return known, unknown


def worst(states):
    """D 不参与竞争（见文头）。⚠️ 只接受 ABCD（E/未知请先过 split_unknown）。"""
    ss = [s for s in states if s]
    live = [s for s in ss if s != "D"]
    if not live:
        return "D" if ss else "?"
    if "C" in live:
        return "C"
    if "B" in live:
        return "B"
    if "A" in live:
        return "A"
    return live[0]


def main():
    verbose = "--verbose" in sys.argv
    do_apply = "--apply" in sys.argv

    ledger, ledger_by_exact_cls, ledger_by_cls, ledger_by_cls_generic, ledger_rows = load_ledger()
    print("账行索引 (类.方法) =", len(ledger), "· 账数据行总数 =", ledger_rows)

    files = sorted(glob.glob(IDX_GLOB))
    print("功能点索引 part 文件 =", len(files))
    if not files:
        return

    old_cnt, new_cnt = {}, {}
    changes, misses = [], 0
    matrix = {}
    plan = []  # (path, lines, eol, new_levels_by_rownum)
    empty_expansions = []   # 通配条目展开 0 行（⛔ 不许静默）
    unknown_hits = []       # (FP id, [未知档...])
    fp_sizes = []           # (FP id, 构成行数-按 文件:行 去重)

    for p in files:
        lines, eol = read_raw(p)
        if not lines:
            continue
        hdr = lines[0].split("\t")
        if "等级" not in hdr or "原版构成" not in hdr:
            print("SKIP (no 等级/原版构成):", os.path.basename(p))
            continue
        i_lv, i_cs = hdr.index("等级"), hdr.index("原版构成")
        newlv = {}
        for n, l in enumerate(lines):
            if n == 0 or not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_lv, i_cs):
                continue
            old = c[i_lv].strip()
            old_cnt[old] = old_cnt.get(old, 0) + 1
            cons = [x.strip() for x in c[i_cs].split(";") if x.strip()]
            states, missing = [], []
            matched_rows = set()   # 构成条数按 (文件, 行号) 去重 —— 只用于自检读数，⛔ 不是写回值
            for item in cons:
                if item.endswith(".*"):
                    # 🔧 通配前缀：
                    #   ① 先按【逐字类名】比 —— 与权威 `feature_index_check.py::expand` 口径一致；
                    #   ② 逐字找不到时，再剥 `<...>` 按【类族】比 —— 修 `RankedEventBase<TSave,.*`
                    #      这类被截断的泛型前缀（原来永远 0 命中，且静默）。
                    #   ⚠️ 比的是「整类」，⛔ 不是字符串前缀 ⇒ 不会把 `FooBar` 算进 `Foo`。
                    cls_tok = item[:-1].rstrip(".").strip()
                    got = ledger_by_exact_cls.get(cls_tok)
                    if not got:
                        base = strip_generics(cls_tok).strip()
                        # 截断泛型前缀（带 `<`）先只认泛型族；再不行才退到全族
                        # （`ContainerBuilder<TCont>.*` 就是这种：原版/账里只有非泛型写法）
                        got = (ledger_by_cls_generic if "<" in cls_tok else ledger_by_cls).get(base)
                        if not got:
                            got = ledger_by_cls.get(base)
                    if not got:
                        empty_expansions.append((os.path.basename(p), item))
                        continue
                    for st, f, ln in got:
                        states.append(st)
                        matched_rows.add((f, ln))
                    continue
                got = ledger.get(item)
                if not got and "(" not in item:
                    got = expand_no_paren(item, ledger)  # 🔧 无括号 `类.方法` ⇒ 同名重载全展开
                if got:
                    for st, f, ln in got:
                        states.append(st)
                        matched_rows.add((f, ln))
                else:
                    missing.append(item)
            fp_sizes.append((c[0].strip() if c else "?", len(matched_rows)))
            if missing:
                misses += len(missing)
            known, unknown = split_unknown(states)
            if unknown:
                unknown_hits.append((c[0].strip() if c else "?", sorted(set(unknown))))
            states = known
            nv = worst(states)
            newlv[n] = nv
            new_cnt[nv] = new_cnt.get(nv, 0) + 1
            norm_old = old.rstrip("?！!").strip()
            matrix[(norm_old, nv)] = matrix.get((norm_old, nv), 0) + 1
            if norm_old != nv:
                changes.append((os.path.basename(p), c[0] if c else "?", old, nv, len(cons), len(missing)))
        plan.append((p, lines, eol, newlv, i_lv))

    print("\n--- 旧等级分布 ---", dict(sorted(old_cnt.items(), key=lambda x: -x[1])))
    print("--- 新等级分布 ---", dict(sorted(new_cnt.items(), key=lambda x: -x[1])))
    fp_total = sum(new_cnt.values())
    c_cnt = new_cnt.get("C", 0)
    print("\nFP 总数 %d · 仍有 C 行的 = %d · （旧口径里 C/存疑 合计 %d）"
          % (fp_total, c_cnt, sum(v for k, v in old_cnt.items() if k.startswith("C"))))
    print("构成行在账里找不到的条目数 =", misses)

    print("\n--- E / 未知档警告 ---")
    print("⚠ E 档仍然没有规则：设计文 §1.1 第 9 列写「E > C > B > A」，但账里现读 0 行带 E，"
          "该顺序没有任何工具/账实现 ⇒ 本脚本把 E（及一切非 ABCD）当【未知】："
          "⛔ 不计入 worst（既不升档也不降档），但**不静默** —— 命中就逐条打警告。")
    print("出现 E/未知档的 FP = %d 个" % len(unknown_hits))
    for fid, ss in unknown_hits[:20]:
        print("  ⚠ %s 含未知档 %s" % (fid, ",".join(ss)))

    print("\n--- 展开 0 行的通配条目（⛔ 不许静默）---")
    print("展开 0 行的通配条目 =", len(empty_expansions))
    for f, it in empty_expansions[:20]:
        print("  ⚠ %s · %s" % (f, it))

    print("\n--- 自检 ---")
    print("账数据行总数 =", ledger_rows)
    if fp_sizes:
        mx_fid, mx_n = max(fp_sizes, key=lambda x: x[1])
        print("构成条数（按 文件:行 去重，仅自检读数）最大的 FP = %s (%d)" % (mx_fid, mx_n))
        over = [(fid, nn) for fid, nn in fp_sizes if nn > ledger_rows]
        print("构成条数 > 账总行数 的 FP = %d" % len(over))
        for fid, nn in over[:20]:
            print("  ✗ %s 构成 %d > 账 %d" % (fid, nn, ledger_rows))
        ok = (misses == 0 and not over and not empty_expansions)
        print("自检结论 =", "PASS ✓（missing==0 且 无 FP 超上限 且 无 0 行展开）" if ok
              else "FAIL ✗（详见上面各项）")

    print("\n--- 旧→新 矩阵 ---")
    for (o, n2), c in sorted(matrix.items(), key=lambda x: (-x[1], str(x[0]))):
        print("  %-6s → %-4s %4d" % (o, n2, c))
    still_c_old = sum(c for (o, n2), c in matrix.items() if o.startswith("C") and n2 == "C")
    print("旧 C/C? 中仍为 C 的 FP =", still_c_old, "/", sum(c for (o, n2), c in matrix.items() if o.startswith("C")))

    if verbose:
        print("\n--- 逐 FP 构成条数（按 文件:行 去重，只打印不写盘）---")
        for fid, nn in fp_sizes:
            print("  %-18s %d" % (fid, nn))
        print("\n--- 等级有变化的 FP（旧 → 新）---")
        for r in changes:
            print("  %-38s %-14s %-8s → %-4s  构成%d条(缺%d)" % r)
    else:
        print("（加 --verbose 看逐 FP 变化）")

    if not do_apply:
        print("\n未写盘（要看写盘请加 --apply）。")
        return

    for p, lines, eol, newlv, i_lv in plan:
        out = []
        for n, l in enumerate(lines):
            if n in newlv and l.strip():
                c = l.split("\t")
                if len(c) > i_lv:
                    c[i_lv] = newlv[n]
                    l = "\t".join(c)
            out.append(l)
        data = eol.join(out)
        if not data.endswith(eol):
            data += eol
        payload = data.encode("utf-8")
        io.open(p, "wb").write(payload)
        print("WROTE", os.path.basename(p))


if __name__ == "__main__":
    main()
