# -*- coding: utf-8 -*-
"""审计 1：重复 dump 模式 (_N vs _N_N) 全量统计 + 内容级 md5 去重。"""
import os, re, sys, json, hashlib, collections

ROOT = r"d:/2/解包整理"
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

pat_dup = re.compile(r"^(?P<cls>[A-Za-z_][A-Za-z0-9_]*)_(?P<id>\d+)_(?P=id)$")

def walk_all():
    for dirpath, dirnames, filenames in os.walk(ROOT):
        for f in filenames:
            yield os.path.join(dirpath, f)

def main():
    # --- 1. 命名模式 _X_X ---
    dup_pattern = collections.Counter()   # top/class -> count
    dup_with_base = collections.Counter()
    dup_no_base = collections.Counter()
    dup_examples = collections.defaultdict(list)
    n_dup = 0
    for p in walk_all():
        base = os.path.basename(p)
        stem, ext = os.path.splitext(base)
        m = pat_dup.match(stem)
        if not m:
            continue
        n_dup += 1
        rel = os.path.relpath(p, ROOT).replace("\\", "/")
        top = rel.split("/")[0]
        d = os.path.dirname(p)
        cls = os.path.basename(d)
        key = f"{top}/{cls}"
        dup_pattern[key] += 1
        basefile = os.path.join(d, f"{m.group('cls')}_{m.group('id')}{ext}")
        if os.path.exists(basefile):
            dup_with_base[key] += 1
        else:
            dup_no_base[key] += 1
            if len(dup_examples[key]) < 5:
                dup_examples[key].append(rel)
    print("=== _X_X 命名重复文件总数:", n_dup)
    tot_with = sum(dup_with_base.values()); tot_without = sum(dup_no_base.values())
    print("  其中有同名基文件(_X) 的:", tot_with, " 无基文件的孤儿:", tot_without)
    print()
    print("--- 按 目录/类 分布 (前 60) ---")
    for k, v in dup_pattern.most_common(60):
        print(f"  {k}\tdup={v}\t有基={dup_with_base.get(k,0)}\t孤儿={dup_no_base.get(k,0)}")
    print()
    if tot_without:
        print("--- 孤儿 _X_X（无对应 _X）示例 ---")
        for k, v in list(dup_examples.items())[:40]:
            print("  ", k, v)

    json.dump({
        "n_dup_named": n_dup,
        "with_base": dict(dup_with_base),
        "orphan": dict(dup_no_base),
        "orphan_examples": {k: v for k, v in dup_examples.items()},
        "pattern_counts": dict(dup_pattern),
    }, open(os.path.join(OUT, "dup_pattern.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    print("\nwrote dup_pattern.json")

main()
