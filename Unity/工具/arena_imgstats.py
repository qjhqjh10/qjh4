#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""战场图的**量化尺子** —— 读一张（或两张）PNG，打出一组可比数字。

为什么要这个脚本：`资料/战场13场_逐场对账_0920.md` 那张 13 场亮度表是**手算的**，
没有可复现的脚本 ⇒ 换个人/换一版渲染就对不上，也没法说清「改善了多少」。
这个脚本是那条线的**唯一量法**，谁要报数就调它，别自己另写一套。

用法：
    python 工具/arena_imgstats.py <图1.png> [图2.png ...]        # 每张一组指标
    python 工具/arena_imgstats.py --pair <我们.png> <原版.png>    # 再加一列比值 + 分区表

指标（与原典一致，`战场13场_逐场对账_0920.md` 第一节「亮度量法」）：
    mean  = 0.299R + 0.587G + 0.114B 的整图平均（0..255）
    dark% = V < 40 的像素占比（V = 上面那个亮度）
    hi%   = V > 200 的像素占比
    gray% = **中性灰**占比：|R−G|,|G−B|,|R−B| 全 ≤ 6 且 V > 190
            —— 「灰板/灰矩形」那类 bug 的**专用指标**（透明区 RGB 恰好 240,240,240）。
            2026-09-21 的透明 bug 就是靠它量的：arena1 预览 3.45% → 0.79%。

⚠️ **分辨率不同要先对齐**（原版实拍 1920×1080 vs 我们的预览 1280×720）：
    `--pair` 会把第二张缩放到第一张的尺寸再比 —— 缩放会抹掉单像素高光，
    所以**别拿单像素峰值说事**，用面积占比。
"""
import sys
from PIL import Image


def stats(img):
    img = img.convert("RGB")
    w, h = img.size
    px = list(img.getdata())
    n = len(px)
    s = d = hi = gray = 0
    for (r, g, b) in px:
        v = 0.299 * r + 0.587 * g + 0.114 * b
        s += v
        if v < 40:
            d += 1
        if v > 200:
            hi += 1
        if v > 190 and abs(r - g) <= 6 and abs(g - b) <= 6 and abs(r - b) <= 6:
            gray += 1
    return dict(w=w, h=h, mean=s / n, dark=100.0 * d / n, hi=100.0 * hi / n, gray=100.0 * gray / n)


def fmt(name, st):
    return (f"{name}\n"
            f"    {st['w']}x{st['h']}  mean={st['mean']:.2f}  "
            f"dark%={st['dark']:.2f}  hi%={st['hi']:.2f}  gray%={st['gray']:.2f}")


def main(argv):
    if len(argv) >= 3 and argv[0] == "--pair":
        a = Image.open(argv[1])
        b = Image.open(argv[2])
        sa, sb = stats(a), stats(b)
        print(fmt(argv[1] + "  [我们]", sa))
        bb = b.resize(a.size, Image.LANCZOS) if b.size != a.size else b
        sb2 = stats(bb)
        print(fmt(argv[2] + ("" if b.size == a.size else f"  [已缩放到 {a.size[0]}x{a.size[1]}]"), sb2))
        print(f"\n比值（我们/原版）：mean {sa['mean'] / sb2['mean']:.3f}x  "
              f"dark {sa['dark'] / sb2['dark']:.3f}x  hi {sa['hi'] / sb2['hi']:.3f}x  "
              f"gray {sa['gray'] / max(sb2['gray'], 1e-6):.3f}x")
        return 0
    if not argv:
        print(__doc__)
        return 1
    for p in argv:
        print(fmt(p, stats(Image.open(p))))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
