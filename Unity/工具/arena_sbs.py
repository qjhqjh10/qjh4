#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""战场**并排图**：我们 | 原版 | 放大差异热图（三格横排），给「认真且严格的对比」用。

为什么要有它：`arena_compare.py` 只出**一张数字表**（mean / dark% / hi% / gray%），
`arena_regions.py` 只出**分块数字** —— 两个都答不了「**内容上**差在哪」（哪块东西没画出来、
哪张贴图不对、哪一层少了一级）。数字看不出「这里本该有个物件」，看图能。

用法：
    python 工具/arena_sbs.py battlearena1            # 单场
    python 工具/arena_sbs.py --all                   # 13 场全出
    python 工具/arena_sbs.py battlearena1 --rect 0.5,0.2,0.8,0.75   # 只看某一块（比例坐标）
    python 工具/arena_sbs.py battlearena1 --gain 6   # 差异放大倍数（默认 4）

产出：`_tmp_view/sbs/<场景>_sbs.png`（三格横排；`--rect` 时会另存 `_rect` 版本）

口径与 `arena_compare.py` **共用一处**：原版是 1920×1080，我们的预览是 1280×720
⇒ **把原版缩到我们的尺寸**再比（缩放会抹掉单像素高光，**别拿单像素峰值说事**）。
"""
import argparse
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageChops, ImageDraw  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
PREVIEW_DIR = os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas")
SHOT_DIR = os.path.join(ROOT, "资料/原版实拍/arena_0920")
OUT_DIR = os.path.join(ROOT, "_tmp_view/sbs")


def norm(s):
    return re.sub(r'[^a-z0-9]', '', s.lower())


def shots_by_key():
    out = {}
    for f in os.listdir(SHOT_DIR):
        m = re.match(r'shot_(.+)\.png$', f)
        if m:
            out[norm(m.group(1).replace("Battle_Arena_", ""))] = os.path.join(SHOT_DIR, f)
    return out


def label(im, text):
    d = ImageDraw.Draw(im)
    d.rectangle([0, 0, 8 + 6 * len(text), 16], fill=(0, 0, 0))
    d.text((4, 4), text, fill=(255, 255, 0))
    return im


def build(arena, shots, gain, rect, quiet=False):
    pv_path = os.path.join(PREVIEW_DIR, arena, f"preview_{arena}.png")
    if not os.path.exists(pv_path):
        print(f"  跳过 {arena}：没有预览图 {pv_path}")
        return None
    key = norm(arena.replace("battlearena", ""))
    if key not in shots:
        print(f"  跳过 {arena}：找不到对应的原版图（键 '{key}'）")
        return None

    ours = Image.open(pv_path).convert("RGB")
    orig = Image.open(shots[key]).convert("RGB")
    if orig.size != ours.size:
        orig = orig.resize(ours.size, Image.LANCZOS)

    if rect:
        w, h = ours.size
        x0, y0, x1, y1 = [int(v * (w if i % 2 == 0 else h)) for i, v in enumerate(rect)]
        x0, y0 = max(0, x0), max(0, y0)
        x1, y1 = min(w, x1), min(h, y1)
        ours = ours.crop((x0, y0, x1, y1))
        orig = orig.crop((x0, y0, x1, y1))

    diff = ImageChops.difference(ours, orig)
    diff = diff.point(lambda v: min(255, v * gain))

    w, h = ours.size
    canvas = Image.new("RGB", (w * 3 + 8, h), (24, 24, 24))
    canvas.paste(label(ours.copy(), "OURS"), (0, 0))
    canvas.paste(label(orig.copy(), "ORIGINAL"), (w + 4, 0))
    canvas.paste(label(diff, f"DIFF x{gain}"), (w * 2 + 8, 0))

    os.makedirs(OUT_DIR, exist_ok=True)
    suffix = "_rect" if rect else ""
    out = os.path.join(OUT_DIR, f"{arena}{suffix}_sbs.png")
    canvas.save(out)
    if not quiet:
        # 只在**有内容的差异**上算分：整图逐像素差的均值（放大前）与「差 >8 的像素占比」
        raw = ImageChops.difference(ours, orig).convert("L")
        px = raw.tobytes()          # `getdata()` 在 Pillow 14 要删，用 tobytes（更快）
        n = len(px)
        big = sum(1 for v in px if v > 8)
        print(f"  {arena:32} 差异均值 {sum(px) / n:6.2f} · 差>8 的像素 {100.0 * big / n:5.1f}%  → {out}")
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arena", nargs="?", default=None)
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--gain", type=int, default=4)
    ap.add_argument("--rect", default=None, help="x0,y0,x1,y1 —— **比例**（0~1）")
    a = ap.parse_args()

    rect = None
    if a.rect:
        rect = [float(v) for v in a.rect.split(",")]
        if len(rect) != 4:
            print("--rect 要四个数：x0,y0,x1,y1")
            return 1

    shots = shots_by_key()
    if a.all:
        for arena in sorted(os.listdir(PREVIEW_DIR)):
            if os.path.isdir(os.path.join(PREVIEW_DIR, arena)):
                build(arena, shots, a.gain, rect)
    else:
        if not a.arena:
            print("给一个场景名，或 --all")
            return 1
        build(a.arena, shots, a.gain, rect)
    return 0


if __name__ == "__main__":
    sys.exit(main())
