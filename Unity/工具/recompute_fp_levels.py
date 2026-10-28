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


def load_ledger():
    m = {}
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
        for l in lines[1:]:
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_st, i_me, i_cl):
                continue
            key = c[i_cl].strip() + "." + c[i_me].strip()
            m.setdefault(key, []).append((c[i_st].strip(), os.path.basename(p)))
    return m


def worst(states):
    """D 不参与竞争（见文头）。"""
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

    ledger = load_ledger()
    print("账行索引 (类.方法) =", len(ledger))

    files = sorted(glob.glob(IDX_GLOB))
    print("功能点索引 part 文件 =", len(files))
    if not files:
        return

    old_cnt, new_cnt = {}, {}
    changes, misses = [], 0
    matrix = {}
    plan = []  # (path, lines, eol, new_levels_by_rownum)

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
            for item in cons:
                if item.endswith(".*"):
                    prefix = item[:-1]
                    for k, v in ledger.items():
                        if k.startswith(prefix):
                            states.extend(s for s, _ in v)
                    continue
                got = ledger.get(item)
                if got:
                    states.extend(s for s, _ in got)
                else:
                    missing.append(item)
            if missing:
                misses += len(missing)
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
    print("\n--- 旧→新 矩阵 ---")
    for (o, n2), c in sorted(matrix.items(), key=lambda x: (-x[1], str(x[0]))):
        print("  %-6s → %-4s %4d" % (o, n2, c))
    still_c_old = sum(c for (o, n2), c in matrix.items() if o.startswith("C") and n2 == "C")
    print("旧 C/C? 中仍为 C 的 FP =", still_c_old, "/", sum(c for (o, n2), c in matrix.items() if o.startswith("C")))

    if verbose:
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
