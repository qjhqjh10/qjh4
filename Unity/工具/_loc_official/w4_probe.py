# -*- coding: utf-8 -*-
"""W4 探针入口（只读）。用法: python -I -X utf8 工具/_loc_official/w4_probe.py <子命令>"""
import io
import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import w4_lib as L  # noqa: E402
from collections import Counter  # noqa: E402


def fam_counts(t):
    return Counter(L.fam(k) for k in t)


def cmd_fams():
    tab = L.loc_table()
    off = L.official()
    print("Loc Entry 现读 = %d" % len(tab))
    print("官方 terms 现读 = %d" % len(off))
    a, b = fam_counts(tab), fam_counts(off)
    print("\n%-24s %6s %6s" % ("族", "我们", "官方"))
    for k in sorted(set(a) | set(b)):
        print("%-24s %6d %6d" % (k, a.get(k, 0), b.get(k, 0)))


def cmd_trait():
    tab = L.loc_table()
    off = {k.split("/", 1)[1]: v for k, v in L.official("CardTraitDescription").items()}
    ours = {k.split("/", 1)[1]: v for k, v in tab.items() if k.startswith("Card_Trait/")}
    low = {k.lower(): k for k in off}
    print("我们 Card_Trait = %d · 官方 CardTraitDescription = %d" % (len(ours), len(off)))
    rows, miss = [], []
    for k, (zh, en) in sorted(ours.items()):
        ok = low.get(k.lower())
        if ok:
            rows.append((k, ok, zh, off[ok][1]))
        else:
            miss.append(k)
    print("大小写无关命中 = %d · 没有官方条目 = %d" % (len(rows), len(miss)))
    print("没官方条目的: %s" % ", ".join(miss))
    diff = [r for r in rows if L.unesc(r[2]) != r[3]]
    print("值不同 = %d" % len(diff))
    for r in diff:
        print("   %-24s offkey=%-18s 我们「%s」 vs 官方「%s」" % (r[0], r[1], r[2], r[3]))
    hitlow = set(k.lower() for k in ours) & set(low)
    print("\n官方有、我们没有的 %d 个: %s" % (len(off) - len(hitlow), ", ".join(sorted(set(off) - set(low[h] for h in hitlow)))))


def cmd_diff(family):
    tab = L.loc_table()
    off = L.official(family)
    print("族 %s: 我们 %d 条 / 官方 %d 条" % (family, len([k for k in tab if L.fam(k) == family]), len(off)))
    print("\n--- 我们有、官方没有 ---")
    for k in sorted(k for k in tab if L.fam(k) == family):
        if k not in off:
            print("   %-50s 「%s」 / 「%s」" % (k, tab[k][0], tab[k][1]))
    print("\n--- 都有、ZH 不同 ---")
    for k in sorted(k for k in tab if L.fam(k) == family and k in off):
        if L.unesc(tab[k][0]) != off[k][1]:
            print("   %-50s 我们「%s」 -> 官方「%s」" % (k, tab[k][0], off[k][1]))


if __name__ == "__main__":
    arg = sys.argv[1:]
    if not arg:
        cmd_fams()
    elif arg[0] == "fams":
        cmd_fams()
    elif arg[0] == "trait":
        cmd_trait()
    elif arg[0] == "diff":
        cmd_diff(arg[1])
    else:
        raise SystemExit("unknown " + arg[0])
