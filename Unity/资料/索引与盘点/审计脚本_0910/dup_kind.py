# -*- coding: utf-8 -*-
"""审计 3：把内容重复组细分成「同一对象重复 dump」vs「不同对象内容恰巧相同」。"""
import os, re, json, collections

OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"
ROOT = r"d:/2/解包整理"

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
print("组数", len(groups))

# 判断是否为「同对象重复」：组内存在一个名字是另一个名字的 stem 前缀（后跟 _ 或 - 数字）
def is_redump(ps):
    stems = [os.path.splitext(os.path.basename(p))[0] for p in ps]
    for i, s in enumerate(stems):
        for j, t in enumerate(stems):
            if i == j:
                continue
            if t.startswith(s) and len(t) > len(s) and t[len(s)] in "_-" :
                rest = t[len(s):].lstrip("_-")
                if rest.isdigit() or (rest.startswith("-") and rest[1:].isdigit()):
                    return True
            # _X_X 形式
            m = re.match(r"^(.*)_(-?\d+)_\1_?\2?$", t)
            if m and m.group(1) + "_" + m.group(2) == s:
                return True
    return False

redump_groups = []
coincident_groups = []
for g in groups:
    (redump_groups if is_redump(g) else coincident_groups).append(g)

def files(gs): return sum(len(g) for g in gs)
def redundant(gs): return sum(len(g) - 1 for g in gs)
def byts(gs):
    t = 0
    for g in gs:
        try:
            t += os.path.getsize(os.path.join(ROOT, g[0].replace("/", os.sep))) * (len(g) - 1)
        except OSError:
            pass
    return t

print(f"『同对象重复 dump』组={len(redump_groups)} 文件={files(redump_groups)} 冗余={redundant(redump_groups)} 字节={byts(redump_groups)/1024**2:.1f}MB")
print(f"『不同对象内容相同』组={len(coincident_groups)} 文件={files(coincident_groups)} 冗余={redundant(coincident_groups)} 字节={byts(coincident_groups)/1024**2:.1f}MB")

print()
print("=== 内容恰好相同的组：最大 40 组 ===")
for g in sorted(coincident_groups, key=lambda g: -len(g))[:40]:
    print(f"  n={len(g)}: {g[:4]}")

print()
print("=== 内容恰好相同的组：按顶层目录统计 ===")
c = collections.Counter()
for g in coincident_groups:
    tops = collections.Counter(p.split("/")[0] for p in g)
    for t in tops:
        c[t] += tops[t]
for k, v in c.most_common():
    print("  ", k, v)

print()
print("=== 同对象重复 dump：按顶层目录统计 ===")
c2 = collections.Counter()
for g in redump_groups:
    tops = collections.Counter(p.split("/")[0] for p in g)
    for t in tops:
        c2[t] += tops[t]
for k, v in c2.most_common():
    print("  ", k, v)

json.dump({
    "redump_groups": len(redump_groups), "redump_files": files(redump_groups),
    "coincident_groups": len(coincident_groups), "coincident_files": files(coincident_groups),
}, open(os.path.join(OUT,"dup_kind.json"),"w",encoding="utf-8"), ensure_ascii=False, indent=1)
with open(os.path.join(OUT,"coincident_groups.txt"),"w",encoding="utf-8") as fh:
    for g in sorted(coincident_groups, key=lambda g:-len(g)):
        fh.write(f"# n={len(g)}\n")
        for p in g: fh.write("  "+p+"\n")
with open(os.path.join(OUT,"redump_groups.txt"),"w",encoding="utf-8") as fh:
    for g in sorted(redump_groups, key=lambda g:-len(g)):
        fh.write(f"# n={len(g)}\n")
        for p in g: fh.write("  "+p+"\n")
