# -*- coding: utf-8 -*-
"""审计 20：按 NTFS 4KB 簇估算磁盘占用（解释 README 的 6.2G 从哪来）。"""
import os, sys

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理"
CL = 4096
logical = 0
alloc = 0
n = 0
for dp, dn, fn in os.walk(ROOT):
    for f in fn:
        p = os.path.join(dp, f)
        try:
            s = os.path.getsize(p)
        except OSError:
            continue
        n += 1
        logical += s
        alloc += ((s + CL - 1) // CL) * CL
print("文件数        :", n)
print("逻辑字节      : %d  (%.2f GiB / %.2f GB)" % (logical, logical / 1024**3, logical / 1000**3))
print("4KB 簇分配字节: %d  (%.2f GiB / %.2f GB)" % (alloc, alloc / 1024**3, alloc / 1000**3))
print("簇开销        : %.2f GB" % ((alloc - logical) / 1000**3))
