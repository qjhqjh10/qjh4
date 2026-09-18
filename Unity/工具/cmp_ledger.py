# -*- coding: utf-8 -*-
"""比较两份特效台账（重扫前后），只打「分组计数 + E 组进出名单」。

用法：python 工具/cmp_ledger.py <旧台账.tsv> <新台账.tsv>
"""
import collections
import csv
import io
import sys


def load(p):
    return {r["效果名"]: r for r in csv.DictReader(io.open(p, encoding="utf-8-sig"), delimiter="\t")}


def main():
    old, new = load(sys.argv[1]), load(sys.argv[2])
    print(f"旧 {len(old)} 条 · 新 {len(new)} 条")
    print()
    print(f"{'判定':<28}{'旧':>6}{'新':>6}{'变化':>7}")
    keys = sorted(set([r["判定"] for r in old.values()] + [r["判定"] for r in new.values()]),
                  key=lambda k: -sum(1 for r in new.values() if r["判定"] == k))
    for k in keys:
        a = sum(1 for r in old.values() if r["判定"] == k)
        b = sum(1 for r in new.values() if r["判定"] == k)
        print(f"  {k[:26]:<28}{a:>6}{b:>6}{b-a:>+7}")

    oe = {n for n, r in old.items() if r["分组"] == "E"}
    ne = {n for n, r in new.items() if r["分组"] == "E"}
    print()
    print(f"E 组：旧 {len(oe)} → 新 {len(ne)}（{len(ne)-len(oe):+d}）")
    print(f"  ✅ 修好（旧 E → 新不在 E）：{len(oe - ne)} 条")
    print(f"  ❌ 新进 E（旧不在 E）：{len(ne - oe)} 条")
    if ne - oe:
        print("     新进的：")
        for n in sorted(ne - oe, key=lambda x: -float(new[x]["|ln|"] or 0))[:15]:
            print(f"       {new[n]['判定'][:2]} {new[n]['亮度比中位']:>7} {n}")
    print()
    print("  修好的（按 |ln| 旧值降序前 15）：")
    for n in sorted(oe - ne, key=lambda x: -float(old[x]["|ln|"] or 0))[:15]:
        print(f"       {old[n]['判定'][:2]} {old[n]['亮度比中位']:>7} → {new[n]['判定'][:2]} "
              f"{new[n]['亮度比中位']:>7}  {n}")

    # 全样本 |ln| 中位
    def med(rows):
        v = sorted(float(r["|ln|"]) for r in rows if r["|ln|"].strip())
        return v[len(v) // 2] if v else float("nan")
    print()
    print(f"全样本 |ln| 中位：旧 {med(list(old.values())):.3f} → 新 {med(list(new.values())):.3f}")


if __name__ == "__main__":
    main()
