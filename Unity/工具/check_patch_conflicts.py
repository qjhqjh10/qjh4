#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""报告 `账/_parts/` 下**跨补丁的重复键**（同一 `(原版类, 方法)` 出现在多份补丁里）。

## 为什么要这个
`merge_ledger_parts.py` 是**按文件名排序、后写的赢**。
而同一条账行可能被**两个代理**都改过（后一批的判据更新），文件名顺序**恰好**决定谁赢 ——
那是**巧合，不是语义**。2026-10-26 G11 抓到一例：
`B5_G11.tsv`（A 版）与 `B5_G5.tsv`（B 版）在 93/95/116 三行冲突，
而 `"B5_G11.tsv" < "B5_G5.tsv"` ⇒ 一次 `--only B5_` 会把 **A 版覆盖回 B 版**。

## 用法
    python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py            # 只看（默认批次 5/6 及以后）
    python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py --all      # 连已归档的 B3 一起看

## 输出
每条冲突：键 · 涉及哪几份补丁（按合并顺序，**最后一个赢**）· 各补丁里的 `状态`。
**怎么处置**：先合「该输的那一份」，后合「该赢的那一份」；或从输的那份里删掉这几行。
⛔ 本脚本只读，不改任何文件。
"""
import io
import os
import sys

PARTS = r"d:/4/Unity/资料/完全复刻/账/_parts"


def read_rows(p):
    b = io.open(p, "rb").read()
    text = b.decode("utf-8-sig")
    for i, l in enumerate(text.split("\n"), 1):
        f = l.split("\t")
        if len(f) == 11 and i > 1:
            yield i, f


def main():
    include_archived = "--all" in sys.argv
    files = []
    files += sorted(f for f in os.listdir(PARTS) if f.startswith("B") and f.endswith(".tsv")
                    and not f.startswith("B_"))
    if include_archived:
        arc = os.path.join(PARTS, "_merged_b3")
        if os.path.isdir(arc):
            files = ["_merged_b3/" + f for f in sorted(os.listdir(arc)) if f.endswith(".tsv")] + files

    occ = {}   # key -> [(file, line, status)]
    for fn in files:
        p = fn if fn.startswith("_merged_b3/") else fn
        path = os.path.join(PARTS, p)
        if not os.path.isfile(path):
            continue
        for ln, f in read_rows(path):
            k = (f[1].strip(), f[2].strip())
            occ.setdefault(k, []).append((fn, ln, f[5].strip()))

    conflicts = {k: v for k, v in occ.items() if len({x[0] for x in v}) > 1}
    if not conflicts:
        print("✅ 没有跨补丁重复键（%d 份补丁）" % len(files))
        return 0

    print("⚠️ 跨补丁重复键 %d 条 —— **合并顺序决定谁赢（文件名排序，最后一个赢）**\n" % len(conflicts))
    for k, v in sorted(conflicts.items(), key=lambda kv: kv[1][0][0]):
        print("  %s . %s" % (k[0], k[1][:70]))
        for fn, ln, st in v:
            mark = "  ← 赢" if fn == sorted(x[0] for x in v)[-1] else ""
            print("      %-22s :%-4d 状态=%s%s" % (fn, ln, st, mark))
    print("\n处置：先合「该输的那一份」，后合「该赢的那一份」；或从输的那份里删掉这几行。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
