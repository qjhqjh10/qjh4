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

🆕 2026-10-18（W27）：**同表再烘一张「阵营徽记」的测量值**（`bx/by/br/bb`，贴图像素、y 从顶部数）——
费用八边形要「上缘贴徽记下缘」，而徽记是**画在卡框贴图里**的、逐 tier 缩放/位置都不同
（150 张实测下缘 177..280），固定值必然逐帧偏。四条：
  · `bx,by` = 徽记圆心：`alpha > 200` 的距离变换（3×3 反复腐蚀 = Chebyshev），
    在**右上窗口**（x 0.58..0.90 W、y 0.02..0.36 H）取「最厚处」= 徽记圆盘内切圆的圆心；
  · `br`    = 该点的内切半径（Chebyshev，作参考/合理性判据）；
  · `bb`    = **徽记下缘**：沿圆心那一列向下，第一个「alpha > 200 连续断 4 行」的位置；
  · 徽记和下方装饰**连成一片**（`bb - by > 2.4 × br`，如织带/挂饰/骷髅串）时，改用
    **徽记核心区里最连贯的那条竖向色差边**（横跨 ±0.75r 的颜色差之和的峰）= 徽记自身下轮廓。
判据与逐帧核对 → `资料/普查产出_1018/W27_费用圈贴右缘.md`。

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

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))   # d:/4/Unity
CARDS = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Resources', 'Art', 'cards')
OUT = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Resources', 'frame_uv.json')
ALPHA_MIN = 8          # 与 CardView.FrameUv 同值：alpha <= 8 视为透明
PREFIX = 'frame_'

# ---- 徽记测量（与 Core/CardView 的费用落点同一套判据；数值只在这里算一次）----
BADGE_ALPHA = 200          # 「实心」判据（纹理边缘的半透明抗锯齿不算）
BADGE_RAY_RUN = 4          # 下缘：连续 4 行都不实心才算「断了」
BADGE_MERGE_K = 2.4        # 下缘 > K × 内切半径 ⇒ 认作「与下方装饰连成一片」，走色差边兜底
BADGE_WIN = (0.58, 0.90, 0.02, 0.36)   # 右上窗口（x 左..右、y 上..下，按贴图比例）


def _dist_cheb(mask):
    """Chebyshev 距离变换：像素到最近背景的 8 邻域步数（反复 3×3 腐蚀）。"""
    cur = Image.fromarray((mask * 255).astype(np.uint8))
    d = np.zeros(mask.shape, np.int16)
    i = 0
    while True:
        i += 1
        nxt = cur.filter(ImageFilter.MinFilter(3))
        a = np.array(nxt) > 0
        if not a.any():
            break
        d[a] = i
        cur = nxt
        if i > 300:
            break
    return d.astype(np.float32)


def measure_badge(path):
    """→ (bx, by, br, bb, method) 贴图像素、y 从顶部数；量不出 ⇒ bb=0。"""
    a = np.array(Image.open(path).convert('RGBA'))
    A = a[:, :, 3]
    rgb = a[:, :, :3].astype(np.float32)
    h, w = A.shape
    solid = A > BADGE_ALPHA
    d = _dist_cheb(solid)

    x0, x1 = int(w * BADGE_WIN[0]), int(w * BADGE_WIN[1])
    y0, y1 = int(h * BADGE_WIN[2]), int(h * BADGE_WIN[3])
    sub = d[y0:y1, x0:x1]
    iy, ix = np.unravel_index(np.argmax(sub), sub.shape)
    cy, cx = int(iy + y0), int(ix + x0)
    rin = float(sub[iy, ix])
    if rin < 20:                       # 窗口里没有「厚块」= 这张框上没有徽记
        return 0, 0, 0, 0, 'none'

    yy = cy
    while yy < h - BADGE_RAY_RUN:
        yy += 1
        if not solid[yy:yy + BADGE_RAY_RUN, cx].any():
            break
    bottom = int(yy)
    method = 'ray'

    if bottom - cy > BADGE_MERGE_K * rin:
        half = int(max(20, rin * 0.75))
        xa, xb = max(0, cx - half), min(w, cx + half)
        ys = np.arange(int(cy + 0.4 * rin), int(cy + 2.6 * rin))
        v = np.array([np.linalg.norm(rgb[y + 2, xa:xb] - rgb[y - 2, xa:xb], axis=1).mean()
                      for y in ys])
        sm = np.convolve(v, np.ones(5) / 5, mode='same')
        sel = ys > cy + 0.55 * rin
        bottom = int(ys[int(np.argmax(sm * sel))])
        method = 'edge'

    # 合理性：圆心要落在右上角、半径要像枚徽记
    if not (30.0 <= rin <= 140.0 and 0.55 * w <= cx <= 0.90 * w and 0.02 * h <= cy <= 0.30 * h
            and cy < bottom < h):
        return float(cx), float(cy), round(rin, 1), 0.0, 'bad'
    # ⚠️ 四个值一律写**浮点**（JSON 里别出整数）：`CardView.FrameUvRow` 里是 `float` 字段，
    #    整数令牌交给 JsonUtility 会不会被转成 float 在不同版本上不一定 —— 写成 739.0 最稳。
    return float(cx), float(cy), round(rin, 1), float(bottom), method


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
    no_badge = 0
    merged = 0
    for f in files:
        name = os.path.splitext(f)[0]
        path = os.path.join(CARDS, f)
        W, H, u, v, w, h = bbox_uv(path)
        if (u, v, w, h) == (0.0, 0.0, 1.0, 1.0):
            full += 1
        bx, by, br, bb, method = measure_badge(path)
        if bb <= 0:
            no_badge += 1
        elif method == 'edge':
            merged += 1
        entries.append({'t': name, 'w': W, 'h': H,
                        'u': round(u, 9), 'v': round(v, 9),
                        'sw': round(w, 9), 'sh': round(h, 9),
                        'bx': bx, 'by': by, 'br': br, 'bb': bb})
    print(f'卡框图 {len(entries)} 张（整张不透明/读不出 ⇒ 全 UV 的 {full} 张）')
    print(f'徽记：量出下缘 {len(entries) - no_badge} 张（其中色差边兜底 {merged} 张）'
          f' · 量不出 {no_badge} 张（bb=0 ⇒ C# 退回旧固定点并告警）')
    print('样本：', entries[0])
    if check:
        print('--check：不写盘')
        return 0
    data = {
        '_note': ('卡框贴图的不透明包围盒（离线表）。判据 = Core/CardView.FrameUv：alpha<=8 视为透明，'
                  'UV 公式 (x0+0.5)/W, (y0+0.5)/H, (x1-x0+1)/W, (y1-y0+1)/H。'
                  '生成器 = 工具/gen_frame_uv.py；重跑时机 = Resources/Art/cards/frame_*.png 增删/替换之后。'
                  'bx/by/br/bb = 阵营徽记（圆心 / 内切半径 / 下缘，贴图像素、y 从顶部数）——'
                  'Core/CardView.FaceCostAt01 用 bb 把费用八边形的可见上缘贴到徽记下缘；'
                  'bb<=0 或缺字段 ⇒ C# 退回旧固定点 (751.7, 305.2) 并 Debug.LogWarning。'),
        '_source': 'MyGame/Assets/CardPresentation/Resources/Art/cards/frame_*.png',
        '_alphaMin': ALPHA_MIN,
        '_badgeAlpha': BADGE_ALPHA,
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
