# -*- coding: utf-8 -*-
"""审计 5：全树「类目录」去重前后计数表 + 目录级去重计数。"""
import os, re, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

# 复用 dup_content 的 md5 结果太大, 改用「文件名去重」快速口径:
#   - 若 X 与 X_<数字> 或 X_<数字>_<同数字> 同目录共存 → 只算 1
def dedup_key(stem):
    m = re.match(r"^(?P<b>.+?)_(?P<i>-?\d+)(?:_(?P=i))?$", stem)
    if m:
        return m.group("b"), m.group("i")
    return None

rows = []
for dirpath, dirnames, filenames in os.walk(ROOT):
    rel = os.path.relpath(dirpath, ROOT).replace("\\", "/")
    parts = rel.split("/")
    if len(parts) < 2:
        continue
    cls = parts[-1]
    if not filenames:
        continue
    stems = {}
    for f in filenames:
        s = os.path.splitext(f)[0]
        stems.setdefault(s, 0)
        stems[s] += 1
    total = len(filenames)
    unique = 0
    dup_named = 0
    for s, c in stems.items():
        k = dedup_key(s)
        if k and (k[0] in stems or ("%s_%s_%s" % (k[0], k[1], k[1])) in stems):
            # 该 stem 是某基名的派生
            base = k[0]
            if base in stems:
                if s != base:
                    dup_named += c
                    continue
        unique += c
    top2 = "/".join(parts[:2]) if len(parts) >= 2 else parts[0]
    rows.append({"dir": rel, "top": parts[0], "top2": top2, "cls": cls,
                 "files": total, "unique_by_name": unique, "dup_named": dup_named})

json.dump(rows, open(os.path.join(OUT, "class_counts.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
tot = sum(r["files"] for r in rows)
uniq = sum(r["unique_by_name"] for r in rows)
print(f"类目录总数 {len(rows)}  文件 {tot}  按名去重后 {uniq}  命名重复 {tot-uniq}")
print()
print("=== 顶层: 文件 / 按名去重 ===")
agg = collections.defaultdict(lambda: [0, 0])
for r in rows:
    agg[r["top"]][0] += r["files"]
    agg[r["top"]][1] += r["unique_by_name"]
for k in sorted(agg):
    a, b = agg[k]
    print(f"  {k}: {a} -> {b}  (重复 {a-b}, {100*(a-b)/a:.0f}%)")
print()
print("=== 重复最多的前 40 个 类目录 ===")
for r in sorted(rows, key=lambda r: -(r["files"] - r["unique_by_name"]))[:40]:
    print(f"  {r['dir']}: {r['files']} -> {r['unique_by_name']} (dup {r['files']-r['unique_by_name']})")
print()
print("=== 08_预制体特效 各子目录 ===")
for r in sorted(rows, key=lambda r: r["dir"]):
    if r["top"].startswith("08_") or r["top"].startswith("07_"):
        print(f"  {r['dir']}: {r['files']} -> {r['unique_by_name']}")
