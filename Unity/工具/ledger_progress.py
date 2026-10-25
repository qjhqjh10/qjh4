#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""账进度速览：每个域 TSV 里各状态多少条（A/B/C/D/E），以及 P0/P1/P2 计数。

用法：python -I -X utf8 d:/4/Unity/工具/ledger_progress.py [目录]

只读，不改任何文件。调度台用它看批次进度，别去读代理的 transcript。
"""
import collections
import io
import os
import sys

D = sys.argv[1] if len(sys.argv) > 1 else r"D:/4/Unity/资料/完全复刻/账"
STATES = ["A", "B", "C", "D", "E"]


def main():
    # 只统计「域账」；代理的补读文件（*_补读.tsv）不算域账，单独列在最后
    files = sorted(x for x in os.listdir(D) if x.endswith(".tsv") and "_补读" not in x)
    print("%-24s %6s %5s %5s %5s %5s %5s  %s" % ("域", "总数", "A", "B", "C", "D", "E", "P0/P1/P2"))
    tot = collections.Counter()
    shape_bad = []
    for fn in files:
        st = collections.Counter()
        pr = collections.Counter()
        n = 0
        with io.open(os.path.join(D, fn), "r", encoding="utf-8") as f:
            next(f, None)
            for lineno, line in enumerate(f, 2):
                cells = line.rstrip("\n").split("\t")
                if len(cells) != 11:
                    # 🔴 最常见原因：某个单元格里塞了换行 ⇒ 一行被拆成两行
                    shape_bad.append((fn, lineno, len(cells)))
                    continue
                n += 1
                st[cells[5].strip()[:1] or "E"] += 1
                if cells[5].strip()[:1] == "C":
                    pr[cells[8].strip().upper()[:2]] += 1
        for s in STATES:
            tot[s] += st[s]
        tot["n"] += n
        mark = "" if st["E"] == n else "  ←"
        print("%-24s %6d %5d %5d %5d %5d %5d  %d/%d/%d%s" % (
            fn[:-4], n, st["A"], st["B"], st["C"], st["D"], st["E"],
            pr["P0"], pr["P1"], pr["P2"], mark))
    print("-" * 78)
    print("%-24s %6d %5d %5d %5d %5d %5d" % ("合计", tot["n"], tot["A"], tot["B"], tot["C"], tot["D"], tot["E"]))
    done = tot["A"] + tot["D"]
    print("已定案(A+D) = %d / %d = %.1f%%" % (done, tot["n"], 100.0 * done / max(1, tot["n"])))
    if shape_bad:
        print("\n🔴 形状异常 %d 行（列数 != 11，最常见 = 单元格里塞了换行）：" % len(shape_bad))
        for fn, lineno, nc in shape_bad[:20]:
            print("   %s:%d  列数=%d" % (fn, lineno, nc))
        print("   修法：把被拆开的行重组成一行（**TSV 单元格永远不许有换行**）。")


if __name__ == "__main__":
    main()
