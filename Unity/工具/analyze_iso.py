#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""analyze_iso.py —— 汇总 EffectIso 两趟产出的逐槽位数字（iso_stats_{orig,exp}.tsv）。

用法（在 d:/4/Unity 下）：
    PYTHONIOENCODING=utf-8 python 工具/analyze_iso.py            # 全部目标
    PYTHONIOENCODING=utf-8 python 工具/analyze_iso.py Buff_DA_Forest_Self

产出：每个目标一张逐槽位表（按 |ln(exp/orig)| 降序）——**第一行就是主导槽**。
口径说明：
  · contrib = Σ|像素 − 背景| / 255（**相对空背景的贡献**，判读用它）
  · lit     = 与纯色背景相差 >2/255 的像素数
  · ratio   = exp_contrib / orig_contrib（原版那侧为 0 时打印「orig=0」，比值无意义）
⚠️ **噪声底**：台账那条「|ln| ≲ 0.6 分不出真假」是对空场景 sweep 说的；逐槽隔离的取景不同，
   但同一族的告警仍然成立 —— **先看绝对值（orig/exp 各自多少），再按比值排**。
⚠️ 第一版 C# 把这一列写成「全图 r+g+b 之和」—— 背景占 99%+ ⇒ 比值恒 1.000（2026-10-02 踩过），已改 contrib。
"""
import io
import math
import os
import sys
from collections import defaultdict

BASE = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "资料", "比对基线")


def load(path):
    """→ {(target, slot): row}，row = dict(lit=int, sum=float, image=str)"""
    d = {}
    if not os.path.exists(path):
        return d
    with io.open(path, encoding="utf-8") as f:
        header = f.readline()
        for line in f:
            c = line.rstrip("\n").split("\t")
            if len(c) < 6:
                continue
            t, slot, image, side, lit, s = c[0], int(c[1]), c[2], c[3], int(c[4]), float(c[5])
            d[(t, slot)] = {"lit": lit, "sum": s, "image": image, "side": side}
    return d


def main():
    want = sys.argv[1] if len(sys.argv) > 1 else None
    o = load(os.path.join(BASE, "iso_stats_orig.tsv"))
    e = load(os.path.join(BASE, "iso_stats_exp.tsv"))
    if not o and not e:
        print("两份 iso_stats 都没有 —— 先跑 bash 工具/run_iso.sh")
        return
    targets = sorted({k[0] for k in list(o) + list(e)})
    for t in targets:
        if want and want not in t:
            continue
        rows = []
        slots = sorted({k[1] for k in list(o) + list(e) if k[0] == t})
        for s in slots:
            ro = o.get((t, s))
            re_ = e.get((t, s))
            if ro is None or re_ is None:
                rows.append((s, ro, re_, None))
                continue
            ratio = (re_["sum"] / ro["sum"]) if ro["sum"] > 0 else None
            rows.append((s, ro, re_, ratio))
        rows.sort(key=lambda r: (-(abs(math.log(r[3])) if r[3] and r[3] > 0 else 99)))
        img = (o.get((t, slots[0]), {}) or {}).get("image") or (e.get((t, slots[0]), {}) or {}).get("image") or ""
        print("=" * 100)
        print(f"■ {t}    （图：{img}）")
        print(f"  {'槽':>3}  {'orig lit':>8} {'orig contr':>11}  {'exp lit':>8} {'exp contr':>11}  {'exp/orig':>9}  {'|ln|':>6}  槽位")
        for s, ro, re_, ratio in rows:
            ol = f"{ro['lit']:>8}" if ro else "       -"
            os_ = f"{ro['sum']:>11.2f}" if ro else "          -"
            el = f"{re_['lit']:>8}" if re_ else "       -"
            es_ = f"{re_['sum']:>11.2f}" if re_ else "          -"
            rt = f"{ratio:>9.3f}" if ratio else "        -"
            ln = f"{abs(math.log(ratio)):>6.3f}" if ratio and ratio > 0 else "     -"
            note = ""
            if ro and re_ and ro["sum"] > 0 and re_["sum"] > 0:
                pass
            if ro is None:
                note = "（原版没有这个槽位）"
            elif re_ is None:
                note = "（导出没有这个槽位）"
            print(f"  {s:>3}  {ol} {os_}  {el} {es_}  {rt}  {ln}  {note}")
        if rows and rows[0][3] and rows[0][3] > 0:
            r0 = rows[0]
            print(f"  ⇒ 主导槽 = [#{r0[0]}] exp/orig = {r0[3]:.3f}（|ln|={abs(math.log(r0[3])):.3f}）")
        print()


if __name__ == "__main__":
    main()
