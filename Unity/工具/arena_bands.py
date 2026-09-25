#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""**按横带对比**（我们 − 原版）—— 判「差异在屏幕的哪一带」。

为什么单独一个工具：`arena_compare.py` 只给整图四个数、`arena_blocks.py` 给**块**（40px）图；
但**战场线的差往往是一条横带**（例：darkangels/emperorschildren = 只有顶部亮 +10~12；arena2 = 中带 +12）。
先把「差在哪一带」定下来，才能选对 `WF_HIDE` / `WF_ONLY` 去点名对象。

口径与 `arena_blocks.py` / `arena_imgstats.py` 一致：`im.convert("L")`，两边都缩到 **1280×720**，
带 = 6 条各 120 px（y0-120 · 120-240 · … · 600-720）。原版图取自 `资料/原版实拍/arena_0920/`。

用法：
    PYTHONIOENCODING=utf-8 python 工具/arena_bands.py battlearenadarkangels
    PYTHONIOENCODING=utf-8 python 工具/arena_bands.py battlearenadarkangels \
        --ours _tmp_view/abl/battlearenadarkangels_noamb.png=NOAMB \
        --ours _tmp_view/abl/battlearenadarkangels_noemit.png=NOEMIT
"""
import argparse
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
BANDS = [(0, 120), (120, 240), (240, 360), (360, 480), (480, 600), (600, 720)]


def norm(s):
    return re.sub(r'[^a-z0-9]', '', s.lower())


def orig_path(arena):
    key = norm(arena.replace("battlearena", ""))
    d = os.path.join(ROOT, "资料/原版实拍/arena_0920")
    if not os.path.isdir(d):
        return None
    for f in os.listdir(d):
        if not f.lower().endswith(".png"):
            continue
        n = norm(f)
        if "arena" + key in n or (key and n.endswith("arena" + key + "png")) or n == f"shotbattlearena{key}png":
            return os.path.join(d, f)
    return None


def load(p):
    return Image.open(p).convert("L").resize((1280, 720), Image.LANCZOS)


def stats(im):
    px = list(im.getdata())
    mean = sum(px) / len(px)
    blocks, within = [], 0
    for by in range(0, 720, 40):
        for bx in range(0, 1280, 40):
            b = im.crop((bx, by, bx + 40, by + 40)).getdata()
            blocks.append(sum(b) / len(b))
    return mean, blocks


def bands(im):
    out = []
    for y0, y1 in BANDS:
        c = im.crop((0, y0, 1280, y1))
        d = c.getdata()
        out.append(sum(d) / len(d))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arena")
    ap.add_argument("--ours", action="append", default=[],
                    help="另比一张图，`路径` 或 `路径=标签`（可重复）")
    a = ap.parse_args()

    op = orig_path(a.arena)
    if not op:
        print(f"❌ 找不到 {a.arena} 的原版真渲图（资料/原版实拍/arena_0920/）")
        return 1
    ours_list = [(os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas", a.arena,
                               f"preview_{a.arena}.png"), "基线")]
    for spec in a.ours:
        p, _, lab = spec.partition("=")
        ours_list.append((p if os.path.isabs(p) else os.path.join(ROOT, p), lab or os.path.basename(p)))

    oimg = load(op)
    omit, oblk = stats(oimg)
    oband = bands(oimg)
    print(f"=== {a.arena}  原版 = {os.path.basename(op)}（{omit:.2f}）===")
    print("图".ljust(22) + "mean".rjust(8) + "比".rjust(7) + "".join(f"y{y0}-{y1}".rjust(9) for y0, y1 in BANDS)
          + "  |差|≤5块")
    for p, lab in ours_list:
        if not os.path.exists(p):
            print(f"{lab:<22} 缺文件 {p}")
            continue
        im = load(p)
        m, blk = stats(im)
        bd = bands(im)
        within = sum(1 for x, y in zip(blk, oblk) if abs(x - y) <= 5) / len(blk) * 100
        print(f"{lab:<22}{m:8.2f}{m / omit:7.3f}"
              + "".join(f"{d - o:+9.1f}" for d, o in zip(bd, oband))
              + f"  {within:5.1f}%")
    print("（带差 = 我们这一带的 mean − 原版同一带的 mean；比 = 整图 mean 之比）")
    return 0


if __name__ == "__main__":
    sys.exit(main())
