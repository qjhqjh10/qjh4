# -*- coding: utf-8 -*-
"""审计 6：场景缺失贴图 → 在全树中搜索同名 png，报告实际位置 + 全树重名情况。"""
import os, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

data = json.load(open(os.path.join(OUT, "scene_tex.json"), encoding="utf-8"))

# 建全树 png 索引: lower(name) -> [paths]
idx = collections.defaultdict(list)
allpng = 0
for dirpath, dirnames, filenames in os.walk(ROOT):
    for f in filenames:
        if f.lower().endswith(".png"):
            idx[f.lower()].append(os.path.relpath(os.path.join(dirpath, f), ROOT).replace("\\", "/"))
            allpng += 1
print("全树 png 数:", allpng)

# 汇总所有场景缺失贴图
miss = collections.Counter()
for sc, d in data.items():
    for m in d["missing"]:
        miss[m] += 1

print("\n=== 缺失贴图 %d 个（按被引用场景数排序）===" % len(miss))
rows = []
for name, n in miss.most_common():
    hits = idx.get((name + ".png").lower(), [])
    rows.append((name, n, hits))
    tag = "★全树都找不到" if not hits else ("×%d" % len(hits))
    print(f"  {name}  [被 {n} 个场景引用]  {tag}")
    for h in hits[:6]:
        print(f"        {h}")

nofind = [r for r in rows if not r[2]]
print(f"\n=== 全树都找不到的: {len(nofind)} 个 ===")
for name, n, _ in nofind:
    print("  ", name)

json.dump([{"name": r[0], "scenes": r[1], "paths": r[2]} for r in rows],
          open(os.path.join(OUT, "missing_tex.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
