#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B92_L1 验证（只读主表）：把 B92_L1.tsv 应用到一个**临时副本**，再跑落点闸看真实数字。"""
import glob
import io
import os
import shutil
import subprocess
import sys

LEDGER = r"D:/4/Unity/资料/完全复刻/账"
SIM = r"D:/4/_tmp_view/b92_l1_sim"
PATCH = os.path.join(LEDGER, "_parts", "B92_L1.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_raw(p):
    b = io.open(p, "rb").read()
    text = b.decode("utf-8-sig")
    eol = "\r\n" if (b.count(b"\r\n") and b.count(b"\r\n") == b.count(b"\n")) else "\n"
    return text.replace("\r\n", "\n").split("\n"), eol


def main():
    # 1) 复制主表
    if os.path.isdir(SIM):
        shutil.rmtree(SIM)
    os.makedirs(SIM)
    for p in glob.glob(os.path.join(LEDGER, "*.tsv")):
        fn = os.path.basename(p)
        if fn.startswith("_"):
            continue
        data = io.open(p, "rb").read()
        io.open(os.path.join(SIM, fn), "wb").write(data)

    # 2) 建 (类,方法) -> (文件, 行下标) 索引
    tables = {}
    for fn in os.listdir(SIM):
        lines, eol = read_raw(os.path.join(SIM, fn))
        tables[fn] = [lines, eol]
    idx = {}
    for fn, (lines, _eol) in tables.items():
        for i, l in enumerate(lines):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) != 11:
                continue
            idx.setdefault((c[1].strip(), c[2].strip()), []).append((fn, i))

    # 3) 应用补丁
    plines, _peol = read_raw(PATCH)
    assert plines[0].split("\t") == COLS
    n = 0
    for l in plines[1:]:
        if not l.strip():
            continue
        f = l.split("\t")
        hits = idx.get((f[1].strip(), f[2].strip()), [])
        if len(hits) > 1:
            # 与 merge_ledger_parts 同口径：按补丁自己的 `域` 消歧（域归一后命中恰好一份主表）
            dom_norm = f[0].strip().replace("/", "_")
            cand = [h for h in hits if h[0][:-4] == dom_norm]
            if len(cand) == 1:
                hits = cand
        if len(hits) != 1:
            print("!! 定位不唯一：%r -> %r" % ((f[1], f[2]), hits))
            continue
        fn, i = hits[0]
        tables[fn][0][i] = "\t".join(f)
        n += 1
    print("应用 %d 行到副本 %s" % (n, SIM))

    # 4) 写副本
    for fn, (lines, eol) in tables.items():
        while lines and not lines[-1].strip():
            lines.pop()
        out = eol.join(lines) + eol
        io.open(os.path.join(SIM, fn), "wb").write(out.encode("utf-8"))

    # 5) 跑闸
    files = [os.path.join(SIM, fn) for fn in sorted(os.listdir(SIM)) if fn.endswith(".tsv")]
    cmd = [sys.executable, "-I", "-X", "utf8", r"d:/4/Unity/工具/check_ledger_points.py"] + files + ["--all-A"]
    r = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    tail = r.stdout.strip().split("\n")
    print("\n".join(tail[-6:]))
    if r.returncode not in (0, 1):
        print(r.stderr[-2000:])


main()
