#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""**按区域对账**：给定几个矩形（用户圈出来的地方），逐块比「我们 / 原版」的亮度。

为什么单独一个工具：`arena_compare.py` 只给整图四个数，**整图数字对不上时看不出差在哪一块**；
用户圈图提问题时，「这一块到底差多少」要能一条命令量出来。

（另有一个更有用的判据：把该场的粒子全 `WF_HIDE` 掉再渲一张，喂给 `--nops`，
就能把每一块的差**拆成「粒子」和「非粒子」两部分** —— 见 `资料/战场13场_逐场对账_0920.md` §一 第 1 条。）

用法：
    PYTHONIOENCODING=utf-8 python 工具/arena_regions.py battlearenaleviathan
    PYTHONIOENCODING=utf-8 python 工具/arena_regions.py battlearenaleviathan \
        --nops d:/4/_tmp_view/iso/lv_nops2.png --rect 130,95,300,235 --rect 1040,270,1130,345
"""
import argparse
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
SHOT = {
    "battlearena1": "Battle_Arena_1", "battlearena2": "Battle_Arena_2",
    "battlearena3": "Battle_Arena_3", "battlearenaaeldari": "Battle_Arena_Aeldari",
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
# 默认区域 = 2026-09-21 用户圈出来的那 5 处（leviathan，1280×720 坐标）
DEFAULT_RECTS = [
    ("① 左上绿雾", (130, 95, 300, 235)),
    ("② 左中绿火柱", (320, 75, 540, 360)),
    ("③ 右侧水池", (580, 140, 1130, 470)),
    ("④ 白烟", (1040, 270, 1130, 345)),
    ("⑤ 右侧火", (1180, 275, 1250, 340)),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arena")
    ap.add_argument("--nops", default=None, help="把粒子全关掉再渲的那张（用来拆「粒子 / 非粒子」）")
    ap.add_argument("--rect", action="append", default=None, help="x0,y0,x1,y1（可重复）")
    a = ap.parse_args()

    ours_p = os.path.join(ROOT, f"MyGame/Assets/WarpforgeArena1/arenas/{a.arena}/preview_{a.arena}.png")
    orig_p = os.path.join(ROOT, f"资料/原版实拍/arena_0920/shot_{SHOT[a.arena]}.png")
    ours = Image.open(ours_p).convert("L")
    orig = Image.open(orig_p).convert("L").resize(ours.size, Image.LANCZOS)
    nops = Image.open(a.nops).convert("L") if a.nops else None

    rects = [(f"rect{i+1}", tuple(int(x) for x in r.split(",")))
             for i, r in enumerate(a.rect)] if a.rect else DEFAULT_RECTS

    print(f"{a.arena}：整图 mean 我们={_mean(ours):.2f} 原版={_mean(orig):.2f} "
          f"比={_mean(ours)/_mean(orig):.3f}")
    print()
    head = f"{'区域':16} {'我们':>7} {'原版':>7} {'差':>7} {'比':>6}"
    if nops:
        head += f" | {'关粒子':>7} {'粒子贡献':>8} {'非粒子差':>8}"
    print(head)
    for name, (x0, y0, x1, y1) in rects:
        A, B = _crop(ours, (x0, y0, x1, y1)), _crop(orig, (x0, y0, x1, y1))
        line = f"{name:16} {A:>7.1f} {B:>7.1f} {A-B:>+7.1f} {A/B:>6.3f}"
        if nops:
            N = _crop(nops, (x0, y0, x1, y1))
            line += f" | {N:>7.1f} {A-N:>+8.1f} {N-B:>+8.1f}"
        print(line)
    if nops:
        print("\n读法：**粒子贡献** = 关掉粒子后掉了多少（≈ 这一块有多少是粒子画的）；"
              "**非粒子差** = 就算粒子全对，这一块还会差多少（≈ 几何/材质/整体的锅）。")


def _mean(img):
    import numpy as np
    return float(np.asarray(img, dtype=float).mean())


def _crop(img, box):
    import numpy as np
    return float(np.asarray(img.crop(box), dtype=float).mean())


if __name__ == "__main__":
    main()
