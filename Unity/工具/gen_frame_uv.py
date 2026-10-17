#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_frame_uv.py — 把「卡框贴图的不透明包围盒」离线烘成一张表（T09 第 2 步 · 2026-10-17）。

为什么要有它
------------
`Core/CardView.FrameUv()` 原来在运行时对每张卡框贴图做一次 `GetPixels32()`（1024² = 4 MB）
＋一次 104 万次的全图像素扫描。实测 ≈ **55 ms/格**（按贴图缓存，见
`资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md` §4）。它**不是** 2.6 s/格的主因，
但属于「可以一次烘好、以后零成本」的那一档；顺带为将来关掉贴图 `Read/Write` 铺路。

判据（必须与 C# 逐字同一套，否则 UV 会偏）
------------------------------------------
源 = `Core/CardView.cs` 的 `FrameUv()`：
  · 透明判据 = **`alpha <= 8`**（`Color32.a`，0..255）；
  · 包围盒 = 所有 `alpha > 8` 的像素的 min/max；
  · `UnityEngine.Texture2D.GetPixels32()` 的 **row 0 在图的下边**，UV 的 `v=0` 也在下边；
  · 输出 `Rect((x0+0.5)/W, (y0+0.5)/H, (x1-x0+1)/W, (y1-y0+1)/H)`；
  · 全透明 / 读不到 ⇒ `Rect(0,0,1,1)`。

本脚本用 PIL 读 PNG：PIL 的 row 0 在**上边** ⇒ 扫描后把 y 翻成「从下往上」再套同一公式。

输出
----
`Assets/CardPresentation/Resources/frame_uv.json`（**不放 `Resources/Art/` 下** —— 那个目录在
`.gitignore` 里；这张表很小、要进 git）。`CardView.FrameUv` 按贴图名查它；查不到再退回运行时扫描。

用法
----
    python -I -X utf8 d:/4/Unity/工具/gen_frame_uv.py            # 写表
    python -I -X utf8 d:/4/Unity/工具/gen_frame_uv.py --check    # 只统计、不写

⚠️ 重跑时机：`Resources/Art/cards/frame_*.png` 增删/替换之后。
"""
import io
import json
import os
import sys

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
CARDS = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Resources', 'Art', 'cards')
OUT = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Resources', 'frame_uv.json')
ALPHA_MIN = 8          # 与 CardView.FrameUv 同值：alpha <= 8 视为透明
PREFIX = 'frame_'


def bbox_uv(path):
    im = Image.open(path).convert('RGBA')
    W, H = im.size
    a = im.getchannel('A')
    # PIL row 0 = 上边；先找 bbox（上边为原点），再翻成 Unity 的「下边为原点」。
    mask = a.point(lambda v: 255 if v > ALPHA_MIN else 0)
    box = mask.getbbox()          # (left, upper, right, lower)，right/lower 是开区间
    if box is None:
        return W, H, 0.0, 0.0, 1.0, 1.0
    left, upper, right, lower = box
    x0, x1 = left, right - 1
    # 翻 y：PIL 的 [upper, lower) 对应 Unity 数组行 [H-lower, H-upper)
    y0, y1 = H - lower, H - upper - 1
    return (W, H,
            (x0 + 0.5) / W, (y0 + 0.5) / H,
            (x1 - x0 + 1) / W, (y1 - y0 + 1) / H)


def main():
    check = '--check' in sys.argv
    files = sorted(f for f in os.listdir(CARDS)
                   if f.startswith(PREFIX) and f.lower().endswith('.png'))
    if not files:
        print('找不到卡框图：' + CARDS)
        return 2
    entries = []
    full = 0
    for f in files:
        name = os.path.splitext(f)[0]
        W, H, u, v, w, h = bbox_uv(os.path.join(CARDS, f))
        if (u, v, w, h) == (0.0, 0.0, 1.0, 1.0):
            full += 1
        entries.append({'t': name, 'w': W, 'h': H,
                        'u': round(u, 9), 'v': round(v, 9),
                        'sw': round(w, 9), 'sh': round(h, 9)})
    print(f'卡框图 {len(entries)} 张（整张不透明/读不出 ⇒ 全 UV 的 {full} 张）')
    print('样本：', entries[0])
    if check:
        print('--check：不写盘')
        return 0
    data = {
        '_note': ('卡框贴图的不透明包围盒（离线表）。判据 = Core/CardView.FrameUv：alpha<=8 视为透明，'
                  'UV 公式 (x0+0.5)/W, (y0+0.5)/H, (x1-x0+1)/W, (y1-y0+1)/H。'
                  '生成器 = 工具/gen_frame_uv.py；重跑时机 = Resources/Art/cards/frame_*.png 增删/替换之后。'),
        '_source': 'MyGame/Assets/CardPresentation/Resources/Art/cards/frame_*.png',
        '_alphaMin': ALPHA_MIN,
        'count': len(entries),
        'entries': entries,
    }
    text = json.dumps(data, ensure_ascii=False, indent=1)
    tmp = OUT + '.tmp'
    io.open(tmp, 'w', encoding='utf-8', newline='\n').write(text + '\n')
    os.replace(tmp, OUT)
    print('写出：' + OUT + f'（{len(text.encode("utf-8"))} B）')
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
