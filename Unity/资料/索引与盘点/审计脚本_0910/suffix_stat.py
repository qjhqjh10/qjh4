# -*- coding: utf-8 -*-
"""审计 15：验证「二次运行」假说 —— 逐目录统计 <name>_<pathID> 后缀文件占比。
若为二次运行产物，则「有后缀」文件与「无后缀」文件应一一对应（同 pid）。"""
import os, re, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"
pat = re.compile(r"^(?P<base>.+?)_(?P<pid>-?\d{5,})$")

rows = []
for dp, dn, fn in os.walk(ROOT):
    rel = os.path.relpath(dp, ROOT).replace("\\", "/")
    if rel == ".":
        continue
    suf = 0
    base = 0
    pair_ok = 0
    pair_bad = 0
    stems = set(os.path.splitext(f)[0] for f in fn)
    for f in fn:
        s = os.path.splitext(f)[0]
        m = pat.match(s)
        if m:
            suf += 1
            if m.group("base") in stems:
                pair_ok += 1
            else:
                pair_bad += 1
        else:
            base += 1
    if suf >= 20:
        rows.append((rel, base, suf, pair_ok, pair_bad))

print("%-50s %8s %8s %8s %8s" % ("目录", "无后缀", "有pid后缀", "有基文件", "孤儿"))
tot = [0, 0, 0, 0]
for rel, b, s, ok, bad in sorted(rows, key=lambda r: -r[2]):
    print("%-50s %8d %8d %8d %8d" % (rel, b, s, ok, bad))
    tot[0] += b; tot[1] += s; tot[2] += ok; tot[3] += bad
print("%-50s %8d %8d %8d %8d" % ("合计", *tot))
print()
print("有 pid 后缀文件里，能配对到同名无后缀文件的: %d/%d = %.2f%%" % (tot[2], tot[1], 100 * tot[2] / max(tot[1], 1)))
