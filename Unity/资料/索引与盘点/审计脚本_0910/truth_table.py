# -*- coding: utf-8 -*-
"""审计 18：顶层真值表（文件数 / md5 去重后唯一对象数 / 字节 / 冗余率）。"""
import os, sys, json, collections

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

dc = json.load(open(os.path.join(OUT, "dir_counts.json"), encoding="utf-8"))
agg = collections.defaultdict(lambda: [0, 0])
for d, (tot, uniq) in dc.items():
    top = d.split("/")[0]
    agg[top][0] += tot
    agg[top][1] += uniq

bytes_ = collections.Counter()
for dp, dn, fn in os.walk(ROOT):
    rel = os.path.relpath(dp, ROOT).replace("\\", "/")
    top = rel.split("/")[0] if rel != "." else "(根)"
    for f in fn:
        try:
            bytes_[top] += os.path.getsize(os.path.join(dp, f))
        except OSError:
            pass

print("%-16s %9s %9s %8s %10s" % ("顶层", "文件数", "唯一对象", "冗余率", "MiB"))
T = [0, 0, 0]
for k in sorted(agg):
    a, b = agg[k]
    if a == 0:
        continue
    T[0] += a; T[1] += b
    print("%-16s %9d %9d %7.0f%% %10.1f" % (k, a, b, 100 * (a - b) / a, bytes_[k] / 1024**2))
print("%-16s %9d %9d %7.0f%% %10.1f" % ("合计", T[0], T[1], 100 * (T[0] - T[1]) / T[0],
                                          sum(bytes_.values()) / 1024**2))
print()
print("顶层文件数（含根目录）:", sum(bytes_.values()) and "", dict(agg).keys())
