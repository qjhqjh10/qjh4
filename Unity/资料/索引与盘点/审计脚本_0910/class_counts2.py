# -*- coding: utf-8 -*-
"""审计 5b：基于 md5 重复组，给出每个类目录的「文件数 / 去重后唯一对象数」。
口径 = 内容 md5（权威）。组内冗余文件计入组内第一个文件所在目录。
"""
import os, re, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

# 读取 dup_groups.txt（按顶层分组）
groups = []
cur = None
for line in open(os.path.join(OUT, "dup_groups.txt"), encoding="utf-8"):
    line = line.rstrip("\n")
    if line.startswith("# "):
        if cur is not None:
            groups.append(cur)
        cur = []
    elif line.startswith("  "):
        cur.append(line.strip())
if cur is not None:
    groups.append(cur)

redundant_by_dir = collections.Counter()   # dir -> 冗余文件数
group_by_dir = collections.defaultdict(set)
for g in groups:
    dirs = collections.Counter(os.path.dirname(p) for p in g)
    for d, n in dirs.items():
        redundant_by_dir[d] += n - 1
    group_by_dir[os.path.dirname(g[0])].add(tuple(g))

# 全量文件数
total_by_dir = collections.Counter()
bytes_by_dir = collections.Counter()
for dirpath, dirnames, filenames in os.walk(ROOT):
    rel = os.path.relpath(dirpath, ROOT).replace("\\", "/")
    for f in filenames:
        p = os.path.join(dirpath, f)
        try:
            bytes_by_dir[rel] += os.path.getsize(p)
        except OSError:
            pass
        total_by_dir[rel] += 1

rows = []
for d, tot in total_by_dir.items():
    if d == ".":
        continue
    red = redundant_by_dir.get(d, 0)
    rows.append({"dir": d, "files": tot, "redundant": red, "unique": tot - red,
                 "MB": round(bytes_by_dir[d] / 1024**2, 2)})

tot = sum(r["files"] for r in rows)
red = sum(r["redundant"] for r in rows)
print(f"合计 文件={tot}  冗余={red}  唯一={tot-red}")
print()
print("=== 顶层 ===")
agg = collections.defaultdict(lambda: [0, 0, 0.0])
for r in rows:
    top = r["dir"].split("/")[0]
    if r["dir"].count("/") == 0:
        agg[top][0] += r["files"]; agg[top][1] += r["unique"]; agg[top][2] += r["MB"]
for k in sorted(agg):
    a, b, m = agg[k]
    print(f"  {k}: {a} -> {b}  (冗余 {a-b}, {100*(a-b)/max(a,1):.0f}%)  {m:.1f}MB")
print()
print("=== 冗余最多的前 30 个类目录 ===")
for r in sorted(rows, key=lambda r: -r["redundant"])[:30]:
    print(f"  {r['dir']}: {r['files']} -> {r['unique']}  (冗余 {r['redundant']})")
print()
print("=== 08/07/03/01/02 主要类目录 ===")
for r in sorted(rows, key=lambda r: r["dir"]):
    if r["redundant"] > 0 and (r["dir"].split("/")[0] in
                               ("08_预制体特效", "07_场景", "01_卡牌", "02_装饰品")):
        print(f"  {r['dir']}: {r['files']} -> {r['unique']}  (冗余 {r['redundant']})")
json.dump(rows, open(os.path.join(OUT, "class_counts.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
print("\nwrote class_counts.json")
