# -*- coding: utf-8 -*-
"""去掉原图卡框底部中央的盾牌徽章（含尖刺环 + 辉光）。

为什么用「横向插值 + 大羽化」而不是别的：
  - 徽章所在区域的背景是横向连续的：上面是半透明窗口雾气，下面是横向轨道，
    所以对掩码内每一行，用左右两侧掩码外的取样做线性插值，能把 alpha 的
    大尺度趋势（雾气的拱形、轨道的上下沿）正确还原。
  - 掩码放大到包住徽章会让插值把辉光延续成一块平顶亮斑，反而更糟；保持
    实测尺寸 + 大羽化（30px）时，边界过渡足够软，看不出接缝。
  - 试过并放弃的两条路：① 镜像高频纹理 —— 掩码宽 180px 时镜像源落到 90px
    外，取到的是卡框侧边装饰而非本地纹理，出彩色噪点；② 多图联合反解水印层
    —— 正向模型残差 RMS 25~44，而反解要除以 (1-a)≈0.36，误差放大 2.8 倍，
    必然留残影。

透明区不修（alpha=0 反正看不见），RGB 最后统一做边缘扩散，避免渲染白边。
"""
import os
import numpy as np
from PIL import Image

ORIG = r"C:\Users\qjh36\Desktop\新建文件夹 (5)\原本卡框"
OUT = r"C:\Users\qjh36\Desktop\新建文件夹 (5)\去盾牌"

# (圆心x, 圆心y, 半轴a, 半轴b) —— 逐张实测
GEO = {
    "人类帝国": (514, 1413, 82, 106),
    "混沌勇士": (512, 1408, 68, 90),
    "诺斯卡":   (518, 1422, 78, 85),
    "高等精灵": (516, 1401, 92, 103),
}
FEATHER = 30


def soft_ellipse(w, h, cx, cy, a, b, feather):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float64)
    d = np.sqrt(((xx - cx) / a) ** 2 + ((yy - cy) / b) ** 2)
    t = (d - 1.0) * min(a, b) / max(feather, 1e-6)
    return np.clip(1.0 - t, 0.0, 1.0)


def row_bounds(mask, thresh=0.002):
    hit = mask > thresh
    H, W = hit.shape
    lo = np.full(H, -1, np.int32); hi = np.full(H, -1, np.int32)
    for y in range(H):
        xs = np.nonzero(hit[y])[0]
        if xs.size:
            lo[y] = xs[0]; hi[y] = xs[-1]
    return lo, hi


def h_lerp(arr, lo, hi, sample=5):
    """按行在 [lo,hi] 之间线性插值，两端取样取自掩码外"""
    H, W = arr.shape[:2]
    out = arr.astype(np.float64).copy()
    for y in range(H):
        if lo[y] < 0:
            continue
        x0, x1 = int(lo[y]), int(hi[y])
        li0, li1 = max(0, x0 - sample), max(0, x0 - 1)
        ri0, ri1 = min(W - 1, x1 + 1), min(W - 1, x1 + sample)
        if li1 < li0 or ri1 < ri0:
            continue
        lv = np.median(arr[y, li0:li1 + 1], axis=0)
        rv = np.median(arr[y, ri0:ri1 + 1], axis=0)
        n = x1 - x0 + 1
        t = np.linspace(1.0 / (n + 1), n / (n + 1), n)
        t = t[:, None] if out.ndim == 3 else t
        out[y, x0:x1 + 1] = lv * (1 - t) + rv * t
    return out


def alpha_extend(rgb, alpha, iters=96):
    """把 RGB 从有 alpha 处向全透明区扩散，消除渲染白边"""
    out = rgb.copy()
    known = alpha > 0.02
    for _ in range(iters):
        if known.all():
            break
        acc = np.zeros_like(out); cnt = np.zeros(out.shape[:2])
        for dy, dx in ((0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            s = np.roll(np.roll(out, dy, 0), dx, 1)
            k = np.roll(np.roll(known, dy, 0), dx, 1)
            acc += np.where(k[..., None], s, 0.0); cnt += k
        new = (cnt > 0) & (~known)
        if not new.any():
            break
        out[new] = acc[new] / cnt[new][..., None]
        known = known | new
    return out


def main():
    os.makedirs(OUT, exist_ok=True)
    for name, (cx, cy, a, b) in GEO.items():
        o = Image.open(os.path.join(ORIG, name + "单位卡框.png")).convert("RGBA")
        W, H = o.size
        arr = np.asarray(o).astype(np.float64)

        m = soft_ellipse(W, H, cx, cy, a, b, FEATHER)
        lo, hi = row_bounds(m)
        filled = h_lerp(arr, lo, hi)
        out = arr * (1 - m[..., None]) + filled * m[..., None]

        alpha = out[:, :, 3]
        rgb = alpha_extend(np.clip(out[:, :, :3], 0, 255), alpha / 255.0)
        res = np.dstack([rgb, np.clip(alpha, 0, 255)]).astype(np.uint8)
        p = os.path.join(OUT, name + "单位卡框.png")
        Image.fromarray(res, "RGBA").save(p)
        print("  %s -> %s" % (name, p))


if __name__ == "__main__":
    main()
