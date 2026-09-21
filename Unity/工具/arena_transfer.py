#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""战场「亮度传递曲线」：**按原版亮度分箱**，看我们在每一档上偏多少。

为什么要有它：`arena_compare.py` 只给整图 mean/dark%/hi%/gray% 四个数 ——
那四个数**分不出机制**。同样是「暗场偏亮 1.19×」，可能是
  ① 加性抬升（我们 = 原版 + c，暗部比值大、亮部比值小）
  ② 乘性（我们 = k × 原版，各档比值都 ≈ k）
  ③ 暗部曲线（我们 = f(原版)，f(x)>x 只在暗端）
三者要改的地方完全不同。**这张表就是用来分它们的。**

口径：和 `arena_imgstats.py` 一致（V = 0.299R+0.587G+0.114B）。
原版图 1920×1080 → 缩到我们预览图的尺寸（默认 1280×720）再比。

用法：
    PYTHONIOENCODING=utf-8 python 工具/arena_transfer.py battlearenaleviathan
    PYTHONIOENCODING=utf-8 python 工具/arena_transfer.py --all
    # 逐区（4×4 网格）版：
    PYTHONIOENCODING=utf-8 python 工具/arena_transfer.py battlearenaleviathan --grid 4
"""
import argparse
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
SHOT_DIR = os.path.join(ROOT, "资料/原版实拍/arena_0920")
ARENA_DIR = os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas")

SHOT_NAME = {   # 目录名 → shot 文件名里的那一段
    "battlearena1": "Battle_Arena_1",
    "battlearena2": "Battle_Arena_2",
    "battlearena3": "Battle_Arena_3",
    "battlearenaaeldari": "Battle_Arena_Aeldari",
    "battlearenaastramilitarum": "Battle_Arena_Astra_Militarum",
    "battlearenablacklegion": "Battle_Arena_Black_Legion",
    "battlearenadarkangels": "Battle_Arena_Dark_Angels",
    "battlearenaemperorschildren": "Battle_Arena_Emperors_Children",
    "battlearenagenestealers": "Battle_Arena_Genestealers",
    "battlearenaleviathan": "Battle_Arena_Leviathan",
    "battlearenasororitas": "Battle_Arena_Sororitas",
    "battlearenaspacewolves": "Battle_Arena_Space_Wolves",
    "battlearenatauviorla": "Battle_Arena_Tau_Viorla",
}

BINS = [0, 16, 32, 48, 64, 80, 96, 112, 128, 144, 160, 176, 192, 208, 224, 240, 256]


def lum(img):
    px = img.convert("RGB").load()
    w, h = img.size
    out = [[0] * w for _ in range(h)]
    for y in range(h):
        row = out[y]
        for x in range(w):
            r, g, b = px[x, y]
            row[x] = 0.299 * r + 0.587 * g + 0.114 * b
    return out


def load_pair(arena):
    pv = os.path.join(ARENA_DIR, arena, f"preview_{arena}.png")
    sh = os.path.join(SHOT_DIR, f"shot_{SHOT_NAME[arena]}.png")
    if not os.path.exists(pv):
        return None
    if not os.path.exists(sh):
        return None
    a = Image.open(pv).convert("RGB")
    b = Image.open(sh).convert("RGB").resize(a.size, Image.LANCZOS)
    return a, b


def report(arena, grid=0):
    pair = load_pair(arena)
    if pair is None:
        print(f"{arena}: 缺图（预览或原版实拍）")
        return
    a, b = pair
    la, lb = lum(a), lum(b)
    w, h = a.size
    print(f"\n=== {arena}  ({w}×{h}) ===")
    print(f"{'原版亮度档':>14} {'像素数':>9} {'原版均值':>9} {'我们均值':>9} {'我们-原版':>10} {'比':>7}")
    for i in range(len(BINS) - 1):
        lo, hi = BINS[i], BINS[i + 1]
        n = 0
        sa = sb = 0.0
        for y in range(h):
            ra, rb = la[y], lb[y]
            for x in range(w):
                v = rb[x]
                if lo <= v < hi:
                    n += 1
                    sa += ra[x]
                    sb += v
        if n == 0:
            continue
        ma, mb = sa / n, sb / n
        d = ma - mb
        print(f"{lo:>6}-{hi - 1:<6} {n:>9} {mb:>9.2f} {ma:>9.2f} {d:>+10.2f} {ma / mb:>7.3f}")

    if grid:
        print(f"\n--- {grid}×{grid} 网格：每格 (我们-原版, 比) ---")
        gw, gh = w // grid, h // grid
        for gy in range(grid):
            cells = []
            for gx in range(grid):
                sa = sb = n = 0.0
                for y in range(gy * gh, (gy + 1) * gh, 2):
                    for x in range(gx * gw, (gx + 1) * gw, 2):
                        sa += la[y][x]
                        sb += lb[y][x]
                        n += 1
                ma, mb = sa / n, sb / n
                cells.append(f"{ma - mb:+6.1f} {ma / mb:5.3f}" if mb > 0.01 else "   --      ")
            print("  | " + " | ".join(cells) + " |")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arena", nargs="?", help="战场目录名，如 battlearenaleviathan")
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--grid", type=int, default=0)
    a = ap.parse_args()
    if a.all:
        for k in SHOT_NAME:
            report(k, a.grid)
    elif a.arena:
        report(a.arena, a.grid)
    else:
        print(__doc__)


if __name__ == "__main__":
    main()
