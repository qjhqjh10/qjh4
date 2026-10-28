#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B92_L1 辅助（紧凑版）：只打 域/类/方法/状态/落点 + 闸的理由。只读。"""
import glob
import io
import os
import sys

sys.path.insert(0, r"d:/4/Unity/工具")
import check_ledger_points as C  # noqa: E402

LEDGER = r"D:/4/Unity/资料/完全复刻/账"
ROOTS = list(C.DEFAULT_ROOTS)


def read_rows(path):
    b = io.open(path, "rb").read()
    return b.decode("utf-8-sig").replace("\r\n", "\n").split("\n")


def main():
    files = sorted(f for f in glob.glob(os.path.join(LEDGER, "*.tsv"))
                   if not os.path.basename(f).startswith("_"))
    n = 0
    for f in files:
        total, fails, drifts, rewrites, getters, warns = C.check_file(f, ROOTS)
        if not fails and not drifts:
            continue
        base = os.path.basename(f)
        rows = read_rows(f)
        hdr = rows[0].split("\t")
        i_cls, i_mtd, i_land = hdr.index("原版类"), hdr.index("方法"), hdr.index("我们的落点")
        idx = {}
        for i, l in enumerate(rows):
            if not l.strip():
                continue
            c = l.split("\t")
            if len(c) <= max(i_cls, i_mtd):
                continue
            idx.setdefault((c[i_cls].strip(), c[i_mtd].strip()), []).append(i)
        for kind, lst in (("FAIL", fails), ("DRIFT", drifts)):
            for (_f, who, land, why) in lst:
                n += 1
                cls, mtd = who.split(".", 1) if "." in who else (who, "")
                hits = idx.get((cls, mtd), [])
                print("#%03d %s | %s | %s" % (n, kind, base, who))
                print("   LAND: " + land)
                print("   WHY : " + why)
                for h in hits:
                    cc = rows[h].split("\t")
                    print("   ROW%d: 域=%s 状态=%s 等级=%s" % (h + 1, cc[0], cc[5], cc[8]))
                print()


main()
