#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""compare_sweep.py — A/B 两份导出侧扫描数据，量出「这次改动是改善还是退步」

为什么需要它：`analyze_sweep.py` 出的是**当前台账**，回答不了「改了之后比改之前好多少」。
而验收指标是 `|ln(亮度比)|` 的中位/均值（连续量、不受带宽边界影响，见交接文档 P0-f），
这个脚本就是把两份导出侧数据放在同一份原版侧上比。

用法：
  "D:/2/Warpforge_tools/py312/python.exe" "d:/4/Unity/工具/compare_sweep.py" \
      甲.tsv 乙.tsv [--orig 原版.tsv] [--top 15] [--csv 输出.csv]

  甲 = 改动前的导出侧（sweep_exp.tsv），乙 = 改动后的
  --orig 默认用 资料/比对基线/sweep_orig.tsv

输出：
  · 两侧的判定分布 + |ln| 中位/均值 + 可判定数
  · 改善/退步清单（按 |ln| 变化量排序）
  · 一句话结论（看 |ln| 中位和均值往哪边走）

⚠️ 判据是 |ln| 的中位/均值，**不是「Z 计数」** —— 那个边界挪 0.05 就能摆动 22–54 个。
"""
import argparse
import collections
import io
import math
import os
import statistics
import sys

LIT_MIN = 60
BAND_LO, BAND_HI = 0.7, 1.4
DEFAULT_ORIG = r"d:/4/Unity/资料/比对基线/sweep_orig.tsv"


def load(path):
    """{效果名: {时刻: (lit, sum)}}"""
    d = collections.defaultdict(dict)
    for l in io.open(path, encoding="utf-8"):
        f = l.rstrip("\n").split("\t")
        if len(f) < 4 or f[0] == "effect":
            continue
        d[f[0]][float(f[1])] = (int(f[2]), float(f[3]))
    return d


def verdict(orig, exp):
    """返回 (分组, 亮度比中位)。口径和 analyze_sweep.py 保持一致。"""
    times = sorted(set(orig) | set(exp))
    o_on = [t for t in times if orig.get(t, (0, 0))[0] >= LIT_MIN]
    e_on = [t for t in times if exp.get(t, (0, 0))[0] >= LIT_MIN]
    both = [t for t in times if orig.get(t, (0, 0))[0] >= LIT_MIN and exp.get(t, (0, 0))[0] >= LIT_MIN]
    if not o_on and not e_on:
        return "W", None
    if not o_on:
        return "D", None
    if not e_on:
        return "C", None
    if not both:
        return "T", None
    ratios = [exp[t][1] / orig[t][1] for t in both if orig[t][1] > 0]
    if not ratios:
        return "?", None
    med = statistics.median(ratios)
    # med<=0 也要归到 E（偏暗），和 analyze_sweep.py 的口径对齐 —— 否则两边会差 1 个
    return ("Z" if BAND_LO <= med <= BAND_HI else "E"), med


def absln(med):
    return abs(math.log(med)) if med and med > 0 else None


def summarize(tag, orig, side):
    rows = {}
    for n in set(orig) | set(side):
        g, med = verdict(orig.get(n, {}), side.get(n, {}))
        rows[n] = (g, med, absln(med))
    vals = [a for _, _, a in rows.values() if a is not None]
    cnt = collections.Counter(g for g, _, _ in rows.values())
    print(f"  {tag}: 可判定 {len(vals):4d} 个   |ln| 中位 {statistics.median(vals):.3f} / 均值 {statistics.mean(vals):.3f}"
          f"   |  判定 Z{cnt['Z']} E{cnt['E']} C{cnt['C']} D{cnt['D']} T{cnt['T']} W{cnt['W']}")
    return rows, statistics.median(vals), statistics.mean(vals)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("before", help="改动前的导出侧 tsv")
    ap.add_argument("after", help="改动后的导出侧 tsv")
    ap.add_argument("--orig", default=DEFAULT_ORIG)
    ap.add_argument("--top", type=int, default=15)
    ap.add_argument("--csv", default=None, help="把逐效果对比写成 csv")
    args = ap.parse_args()

    for p in (args.before, args.after, args.orig):
        if not os.path.exists(p):
            print(f"缺文件: {p}")
            return 1

    orig = load(args.orig)
    print(f"原版侧 {len(orig)} 个效果（{os.path.basename(args.orig)}）\n")
    b, bmed, bmean = summarize("改前", orig, load(args.before))
    a, amed, amean = summarize("改后", orig, load(args.after))

    # 逐效果变化
    moved = []
    for n in set(b) | set(a):
        gb, mb, ab = b.get(n, ("W", None, None))
        ga, ma, aa = a.get(n, ("W", None, None))
        if ab is None and aa is None:
            continue
        if ab is None:
            moved.append((n, gb, ga, None, aa, "新可判定"))
        elif aa is None:
            moved.append((n, gb, ga, ab, None, "变成判定不了"))
        else:
            moved.append((n, gb, ga, ab, aa, ""))

    better = [m for m in moved if m[3] is not None and m[4] is not None and m[4] < m[3] - 0.01]
    worse = [m for m in moved if m[3] is not None and m[4] is not None and m[4] > m[3] + 0.01]
    print(f"\n逐效果：变好 {len(better)} 个 / 变差 {len(worse)} 个 / 基本没动 {len(moved) - len(better) - len(worse)} 个")
    print(f"\n  ▼ 改善最多的 {args.top} 个（|ln| 变小 = 更像原版）")
    for n, gb, ga, ab, aa, note in sorted(better, key=lambda m: m[3] - m[4], reverse=True)[:args.top]:
        print(f"    {n[:40]:42s} {gb}→{ga}  |ln| {ab:.3f} → {aa:.3f}")
    print(f"\n  ▲ 退步最多的 {args.top} 个")
    for n, gb, ga, ab, aa, note in sorted(worse, key=lambda m: m[4] - m[3], reverse=True)[:args.top]:
        print(f"    {n[:40]:42s} {gb}→{ga}  |ln| {ab:.3f} → {aa:.3f}")

    dm, dmean = amed - bmed, amean - bmean
    print(f"\n结论：|ln| 中位 {bmed:.3f} → {amed:.3f}（{dm:+.3f}），"
          f"均值 {bmean:.3f} → {amean:.3f}（{dmean:+.3f}）")
    print("  → " + ("**改善**" if dm < -0.005 or dmean < -0.005 else
                    "**退步**" if dm > 0.005 or dmean > 0.005 else "基本没变（改动没起作用，或者只影响了少数效果）"))

    if args.csv:
        with io.open(args.csv, "w", encoding="utf-8-sig", newline="") as f:
            f.write("效果名\t改前判定\t改后判定\t改前|ln|\t改后|ln|\t变化\n")
            for n, gb, ga, ab, aa, note in moved:
                d = "" if (ab is None or aa is None) else f"{aa - ab:+.3f}"
                f.write(f"{n}\t{gb}\t{ga}\t{'' if ab is None else f'{ab:.3f}'}\t"
                        f"{'' if aa is None else f'{aa:.3f}'}\t{d}{note}\n")
        print(f"\n逐效果明细 → {args.csv}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
