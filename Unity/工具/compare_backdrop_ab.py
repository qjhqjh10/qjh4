#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""compare_backdrop_ab.py —— 「换尺子」A/B 的比较：**旧口径 vs 棋盘底图口径**。

读 `d:/4/_tmp_view/ab_{A,B}_{orig,exp}.tsv`（由 `工具/run_backdrop_ab.sh` 产出），按效果逐时刻比：
  · A 组 = `WFSWEEP_BACKDROP=0`（旧口径：空场景 + 纯色背景，判据基准 = 常量 `Bg`）
  · B 组 = 棋盘底图（新口径，判据基准 = 同一场景不播效果渲出来的 `plate`）

判读用它回答两个问题：
  1. **抓屏族在旧口径里是不是「读数恒 0」**（空场景没有可扭曲的内容 ⇒ 扭曲前后一样）
     —— 是则新口径的核心目的达到；
  2. 新口径下这一族的 **exp/orig 比值**有没有落在可比区间（`|ln|` 小），
     以及**对照效果**（本来就对得上的）有没有被新口径弄坏。

⚠️ **两个口径的数字不可直接比大小**（基准不同）—— 要比的是「**可读性**」（lit 是不是 0、
两侧有没有同时亮），以及同一口径内的 exp/orig。
"""
import io
import math
import os
import sys
from collections import defaultdict

TMP = r"d:/4/_tmp_view"
GROUPS = ["A", "B"]


def load(tag):
    """→ {(effect, time): (lit, sum)}"""
    d = {}
    for side in ("orig", "exp"):
        p = os.path.join(TMP, f"ab_{tag}_{side}.tsv")
        if not os.path.exists(p):
            print(f"!! 缺 {p}")
            continue
        with io.open(p, encoding="utf-8") as f:
            f.readline()
            for line in f:
                c = line.rstrip("\n").split("\t")
                if len(c) < 4:
                    continue
                d[(c[0], float(c[1]), side)] = (int(c[2]), float(c[3]))
    return d


def main():
    # 🆕 2026-10-02：两组标签可以由命令行给（原来写死 A/B）——
    #    `compare_backdrop_ab.py <组1> <组2> [效果名子串]`，例如 `… D E`（Feature 开/关那次实验）。
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    if len(args) >= 2:
        GROUPS[:] = [args[0], args[1]]
        want = args[2] if len(args) > 2 else None
    else:
        want = args[0] if args else None
    data = {g: load(g) for g in GROUPS}

    effects = sorted({k[0] for g in GROUPS for k in data[g]})
    if want:
        effects = [e for e in effects if want.lower() in e.lower()]
    if not effects:
        print("没有任何效果 —— 先跑 工具/run_backdrop_ab.sh")
        return 1

    for e in effects:
        print(f"\n■ {e}")
        print(f"    时刻     {GROUPS[0]}: orig_lit/exp_lit  比值 |  {GROUPS[1]}: orig_lit/exp_lit  比值")
        for t in sorted({k[1] for g in GROUPS for k in data[g] if k[0] == e}):
            cells = []
            for g in GROUPS:
                o = data[g].get((e, t, "orig"))
                x = data[g].get((e, t, "exp"))
                if not o or not x:
                    cells.append("        -            ")
                    continue
                ratio = (x[1] / o[1]) if o[1] > 0 else float("nan")
                rs = f"{ratio:.3f}" if o[1] > 0 else "orig=0"
                cells.append(f"{o[0]:6d}/{x[0]:6d} {rs:>7}")
            print(f"    {t:4.2f}   {cells[0]} | {cells[1]}")

    # ---- 汇总：可读性 ----
    print("\n===== 汇总（「可读」= 两侧 lit 都 > 0 的时刻数 / 总时刻数）=====")
    print(f"{'效果':<45} {'A 可读':>10} {'B 可读':>10}   {'A 中位|ln|':>12} {'B 中位|ln|':>12}")
    for e in effects:
        row = [e]
        for g in GROUPS:
            n = tot = 0
            lns = []
            for k, (lit, s) in data[g].items():
                if k[0] != e or k[2] != "orig":
                    continue
                x = data[g].get((e, k[1], "exp"))
                if not x:
                    continue
                tot += 1
                if lit > 0 and x[0] > 0:
                    n += 1
                    if s > 0 and x[1] > 0:
                        lns.append(abs(math.log(x[1] / s)))
            med = sorted(lns)[len(lns) // 2] if lns else float("nan")
            row.append(f"{n}/{tot}")
            row.append(f"{med:.3f}" if lns else "-")
        print(f"{row[0]:<45} {row[1]:>10} {row[2]:>10}   {row[3]:>12} {row[4]:>12}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
