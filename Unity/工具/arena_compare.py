#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""13 个战场：**我们的预览图 vs 原版真渲图** 逐场量化比对（一张表）。

为什么要脚本：`资料/战场13场_逐场对账_0920.md` 第二节那张亮度表**是手算的**，
没有可复现的脚本 ⇒ 换个人/换一版渲染就对不上。这条线**只认这张表**，谁要报数就调它。

用法：
    python 工具/arena_compare.py                     # 用默认路径
    python 工具/arena_compare.py --preview-dir <目录> --shot-dir <目录>

⚠️ **量之前先确认预览图是「预热过的」那版**（`ArenaBuilder.RenderPreview` 2026-09-21 起
连渲 4 帧只用最后一帧）。**没预热的那批图是首帧垃圾**，数会随机漂
（实测同一场景同一命令：mean = 105.93 / 141.96 / 98.40 三种都有）。
判据：日志里 `[Arena] 预热第 1/4 帧` 与 `预热第 2/4 帧` 的 mean **不一样**、第 2 帧起才收敛；
最终图**跨两次运行**的 mean 差应 < 0.5。

指标定义见 `工具/arena_imgstats.py`（同一个函数，别另写一套）。
原版图是 1920×1080、我们的是 1280×720 ⇒ **把原版缩到我们的尺寸**再比
（缩放会抹掉单像素高光，所以别拿单像素峰值说事，看面积占比）。
"""
import argparse
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402
from arena_imgstats import stats  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity


def norm(s):
    return re.sub(r'[^a-z0-9]', '', s.lower())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview-dir", default=os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas"))
    ap.add_argument("--shot-dir", default=os.path.join(ROOT, "资料/原版实拍/arena_0920"))
    a = ap.parse_args()

    shots = {}
    for f in os.listdir(a.shot_dir):
        m = re.match(r'shot_(.+)\.png$', f)
        if not m:
            continue
        key = norm(m.group(1).replace("Battle_Arena_", ""))
        shots[key] = os.path.join(a.shot_dir, f)

    rows = []
    for arena in sorted(os.listdir(a.preview_dir)):
        d = os.path.join(a.preview_dir, arena)
        if not os.path.isdir(d):
            continue
        pv = os.path.join(d, f"preview_{arena}.png")
        if not os.path.exists(pv):
            continue
        key = norm(arena.replace("battlearena", ""))
        if key not in shots:
            rows.append((arena, None))
            continue
        ours = stats(Image.open(pv))
        im = Image.open(shots[key])
        pv_img = Image.open(pv)
        if im.size != pv_img.size:
            im = im.resize(pv_img.size, Image.LANCZOS)
        orig = stats(im)
        rows.append((arena, (ours, orig)))

    print(f"{'战场':30} {'mean 我':>8} {'原版':>8} {'比':>6} | {'dark 我':>8} {'原版':>8} | "
          f"{'hi 我':>7} {'原版':>7} | {'gray 我':>8} {'原版':>7}")
    bad = []
    for arena, r in rows:
        if r is None:
            print(f"{arena:30} ** 找不到对应的原版图 **")
            continue
        o, g = r
        print(f"{arena:30} {o['mean']:8.2f} {g['mean']:8.2f} {o['mean'] / g['mean']:6.3f} | "
              f"{o['dark']:8.2f} {g['dark']:8.2f} | {o['hi']:7.2f} {g['hi']:7.2f} | "
              f"{o['gray']:8.2f} {g['gray']:7.2f}")
        if abs(o['mean'] / g['mean'] - 1) > 0.10:
            bad.append((arena, o['mean'] / g['mean']))
    print()
    print(f"共 {len(rows)} 场；**亮度比偏离 1.00 超过 ±10%** 的：{bad if bad else '无'}")


if __name__ == "__main__":
    main()
