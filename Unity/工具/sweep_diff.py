# -*- coding: utf-8 -*-
"""退步定位用的小算尺：从两趟 sweep TSV 里算**目标效果**的「亮度比中位 / |ln|」。

为什么要单独写：`资料/特效还原台账.tsv` 是权威产物，`analyze_sweep.py` 会把**整张台账重写**
（它按全量 sweep 算）。定位退步时我只需要十几个效果的数，**不能拿全量表去覆盖台账**。

口径与 `analyze_sweep.py` 一致：每个时间点 exp/orig 的「亮度和」之比，取**中位**；
`|ln|` 是验收数字（0 = 和原版一样亮）。

用法：`python 工具/sweep_diff.py <orig.tsv> <exp.tsv> [效果名...]`
（不给效果名就用下面 TARGETS）
"""
import io
import math
import statistics
import sys

TARGETS = [
    # 变差的（台账里「偏暗（导出更暗/更稀）」）
    "Cut Wulfen SW", "Cut Wulfen SW Reverse", "Cut Wulfen SW Double",
    "Bore Through", "Bore Through Intense", "StrikeEffect", "VulnerableEffect",
    "CreateCard BlackLegion", "EnvironmentalCondition Sororitas Raging Storm",
    # 对照：第33轮之后**变好**的（Z 组）
    "Stealth_Proc_UM", "Stealth_Proc_Eldar", "SynapseEffect",
    # 对照：一直对得上的
    "InvulnerableEffect",
]


def load(path):
    d = {}
    with io.open(path, encoding="utf-8") as f:
        next(f)
        for ln in f:
            p = ln.rstrip("\r\n").split("\t")        # ⚠️ sweep_*.tsv 是 CRLF，不剥 \r 时间列对不上
            if len(p) < 4:
                continue
            d.setdefault(p[0], {})[p[1]] = (int(p[2]), int(p[3]))
    return d


def main():
    o = load(sys.argv[1])
    e = load(sys.argv[2])
    names = sys.argv[3:] or TARGETS
    print(f"{'效果':<44}{'亮度比中位':>10}{'|ln|':>8}   备注")
    for n in names:
        if n not in o or n not in e:
            print(f"{n:<44}{'—':>10}{'—':>8}   （这份 TSV 里没有这个效果）")
            continue
        ratios = []
        for t, (lit_o, sum_o) in o[n].items():
            if t in e[n]:                                  # ⚠️ 是 `e[n]` 不是 `e`（`e` 的键是**效果名**）
                lit_e, sum_e = e[n][t]
                if sum_o > 0 and sum_e > 0:
                    ratios.append(sum_e / sum_o)
        if not ratios:
            print(f"{n:<44}{'—':>10}{'—':>8}   （两边至少一侧全程空）")
            continue
        med = statistics.median(ratios)
        print(f"{n:<44}{med:>10.3f}{abs(math.log(med)):>8.3f}")


if __name__ == "__main__":
    main()
