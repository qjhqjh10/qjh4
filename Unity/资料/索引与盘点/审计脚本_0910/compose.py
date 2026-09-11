# -*- coding: utf-8 -*-
"""审计 8：
 (a) 以「组内规范文件 = 名字最短者」为准，逐目录统计冗余（唯一口径，不重复计）
 (b) 08_预制体特效/战斗预制体 的 GameObject 组成分析（哪些是卡牌/UI 混入）
 (c) 0 字节 / 空目录 复核
"""
import os, re, json, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

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

red = collections.Counter()
for g in groups:
    canon = min(g, key=lambda p: (len(os.path.basename(p)), p))
    for p in g:
        if p != canon:
            red[os.path.dirname(p)] += 1

total_by_dir = collections.Counter()
for dp, dn, fn in os.walk(ROOT):
    rel = os.path.relpath(dp, ROOT).replace("\\", "/")
    if rel == ".":
        continue
    total_by_dir[rel] = len(fn)

tot = sum(total_by_dir.values())
r = sum(red.values())
print(f"【去重口径修正】文件 {tot}  冗余 {r}  唯一对象 {tot-r}")
print()
print("=== 顶层 ===")
agg = collections.defaultdict(lambda: [0, 0])
for d, n in total_by_dir.items():
    top = d.split("/")[0]
    if "/" not in d:
        agg[top][0] += n
        agg[top][1] += n - red.get(d, 0)
for k in sorted(agg):
    a, b = agg[k]
    print(f"  {k}: {a} -> {b}  (冗余 {a-b}, {100*(a-b)/max(a,1):.0f}%)")
print()
print("=== 前 25 冗余目录 ===")
for d, v in red.most_common(25):
    print(f"  {d}: {total_by_dir[d]} -> {total_by_dir[d]-v}  (冗余 {v})")

json.dump({d: [total_by_dir[d], total_by_dir[d] - red.get(d, 0)] for d in total_by_dir},
          open(os.path.join(OUT, "dir_counts.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)

# ---------------- (b) 战斗预制体 GameObject 组成 ----------------
print()
print("=== 08_预制体特效/战斗预制体 GameObject 组成 ===")
GO = os.path.join(ROOT, "08_预制体特效/战斗预制体/GameObject")
names = []
for f in os.listdir(GO):
    if not f.endswith(".json"):
        continue
    try:
        d = json.load(open(os.path.join(GO, f), encoding="utf-8"))
    except Exception:
        continue
    names.append(d.get("m_Name", "") or "")
print("GO 文件:", len(names), " 唯一名:", len(set(names)))

# 前缀统计（Unity 复制产生的 "Name (1)" 等）
base = collections.Counter()
for n in names:
    b = re.sub(r"\s*\(\d+\)$", "", n)
    base[b] += 1
print("去序号后唯一名:", len(base))
print()
print("--- 出现次数最多的 40 个名字 ---")
for k, v in base.most_common(40):
    print(f"   {v:5d}  {k}")

# 疑似非特效：卡牌 / UI 关键词
kw_card = re.compile(r"(Card|card|Deck|deck|Hand|hand|Warlord|warlord|Minion|minion|Trait|trait|Energy|energy|Avatar|avatar)")
kw_ui = re.compile(r"(Button|button|Panel|panel|Text|text|Icon|icon|Menu|menu|Popup|popup|HUD|hud|Bar|bar|Slot|slot|Frame|frame|Background|background)")
c_card = [n for n in base if kw_card.search(n)]
c_ui = [n for n in base if kw_ui.search(n)]
print()
print("命中卡牌关键词的不同名:", len(c_card), " 命中UI关键词:", len(c_ui))
print("卡牌类样例:", sorted(c_card)[:40])
print()
print("UI类样例:", sorted(c_ui)[:40])

json.dump({"n_go_files": len(names), "n_unique": len(set(names)), "n_base": len(base),
           "card_like": sorted(c_card), "ui_like": sorted(c_ui),
           "top": base.most_common(300)},
          open(os.path.join(OUT, "battleprefab_go.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
