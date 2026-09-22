#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""**逐块有符号差**（ours − original）—— 判「画面差异是不是全图均匀的偏移」的**第一步**。

🔴 **为什么必须有它**（正本 `资料/战场13场_逐场对账_0920.md` 坑 ⑨）：
   我一度**按「原版亮度」分箱**得出「我们 = 0.877×原版 + 13.2，一条 12% 罩纱」，
   还据此追了雾 / 粒子 / 天空盒 / 透明网格一大圈 —— **全是白追的**。
   分箱比较天然有**回归均值**效应：形状看着像「压对比度」，其实图像本身是**零均值**的
   （实测逐块有符号差：全图均值 −0.68、中位 −1.0，绝大多数块在 ±5 以内）。
   **正确顺序**：① **先看逐块的有符号差**判「有没有全图均匀的偏移」；
   ② 确认有系统性偏移之后，**再**做分箱看形状。**反过来做会白追一整轮。**

它给出的第二样东西同样重要：**哪几块最极端** —— 「烟囱那块 −44.9」「地面火那块 +37.7」
就是这么被点名的（2026-09-22 定案：那几块**全是粒子**）。

用法：
    python 工具/arena_blocks.py battlearena1                # 默认 40px 块、列最极端 12 块
    python 工具/arena_blocks.py --all --top 6
    python 工具/arena_blocks.py battlearena1 --bs 20 --csv _tmp_view/blocks_arena1.csv
    python 工具/arena_blocks.py battlearena1 --ours <另一张图.png>   # 比 A/B（比如 WF_HIDE 那版）
"""
import argparse
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity


def norm(s):
    return re.sub(r'[^a-z0-9]', '', s.lower())


def orig_path(arena):
    key = norm(arena.replace("battlearena", ""))
    d = os.path.join(ROOT, "资料/原版实拍/arena_0920")
    for f in os.listdir(d):
        m = re.match(r'shot_(.+)\.png$', f)
        if m and norm(m.group(1).replace("Battle_Arena_", "")) == key:
            return os.path.join(d, f)
    return None


def luma(im):
    """与 `arena_imgstats.py` 同一个亮度口径（0.299R+0.587G+0.114B），别另写一套。"""
    return im.convert("L")


def blocks(im, bs):
    """按 `bs`×`bs` 分块求均值。用 numpy 切块（1280×720 = 92 万像素，纯 Python 太慢）。"""
    import numpy as np
    a = np.asarray(im, dtype=np.float32)
    h, w = a.shape
    ny, nx = h // bs, w // bs
    a = a[:ny * bs, :nx * bs].reshape(ny, bs, nx, bs).mean(axis=(1, 3))
    return [(bx * bs, by * bs, float(a[by, bx])) for by in range(ny) for bx in range(nx)]


def run(arena, bs, top, ours=None, csv=None):
    pv = ours or os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas", arena,
                              f"preview_{arena}.png")
    op = orig_path(arena)
    if not os.path.exists(pv):
        print(f"{arena}: 没有 {pv}")
        return None
    if not op:
        print(f"{arena}: 找不到对应的原版图")
        return None
    a = luma(Image.open(pv))
    b = luma(Image.open(op))
    if a.size != b.size:
        b = b.resize(a.size, Image.LANCZOS)
    A, B = blocks(a, bs), blocks(b, bs)
    dif = [(bx, by, av - bv) for (bx, by, av), (_, _, bv) in zip(A, B)]
    dif_sorted = sorted(dif, key=lambda t: -abs(t[2]))
    vals = sorted(d[2] for d in dif)
    n = len(vals)
    mean = sum(vals) / n
    med = vals[n // 2]
    same = sum(1 for v in vals if abs(v) <= 5.0)
    print(f"=== {arena}  ({bs}px 块 × {n})  ours={os.path.basename(pv)} ===")
    print(f"  逐块有符号差（我们 − 原版）：均值 {mean:+.2f} · 中位 {med:+.2f} · "
          f"|差|≤5 的块 {same}/{n} = {100.0 * same / n:.1f}%")
    print(f"  ⇒ {'**零均值、按物体局部**（不是全图均匀偏移，别去追「全局罩纱」）' if abs(mean) < 2.0 else '⚠️ 均值偏得不小，按「全图均匀偏移」查'}")
    print(f"  最极端 {top} 块：")
    for bx, by, d in dif_sorted[:top]:
        print(f"     ({bx:4d},{by:4d})  {d:+7.2f}")
    if csv:
        os.makedirs(os.path.dirname(csv), exist_ok=True)
        with open(csv, "w", encoding="utf-8") as fp:
            fp.write("x,y,diff\n")
            for bx, by, d in sorted(dif, key=lambda t: (t[1], t[0])):
                fp.write(f"{bx},{by},{d:.3f}\n")
        print(f"  逐块表 → {csv}")
    return dif


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("arena", nargs="?", default=None)
    ap.add_argument("--all", action="store_true")
    ap.add_argument("--bs", type=int, default=40)
    ap.add_argument("--top", type=int, default=12)
    ap.add_argument("--ours", default=None, help="用另一张图代替 preview_<场>.png（A/B 用）")
    ap.add_argument("--csv", default=None)
    a = ap.parse_args()
    arenas = ([d for d in sorted(os.listdir(os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas")))
               if os.path.isdir(os.path.join(ROOT, "MyGame/Assets/WarpforgeArena1/arenas", d))]
              if a.all else [a.arena or "battlearena1"])
    for x in arenas:
        run(x, a.bs, a.top, a.ours, a.csv)
        print()


if __name__ == "__main__":
    main()
