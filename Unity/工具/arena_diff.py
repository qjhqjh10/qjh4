#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""arena_diff.py —— **逐场自动找出「我们与原版实拍不一样的地方」**（2026-10-11 第二十会话造）。

为什么要有它（用户 2026-10-11 的话）：
  > 「**最好是一个场景一个场景的对比进行复刻**……**很多地方需要我提出你才能发现问题**。」
  ⇒ 光靠人眼看并排图，只能发现「特别扎眼」的那几处；**要机器把候选逐个排出来**。

做法（四步，**每一步都为「少报假警」服务**）：
  1. **色调归一** —— 原版是**游戏内实拍**（带它自己的后处理/曝光），我们是 preview 渲染。
     不做归一 ⇒ 每一格都「有差异」（实测全图平均差 5.7%）。⇒ **按通道做 mean/std 匹配**，
     把整体曝光/对比差压掉，**留下的是「结构性差异」**。
  2. **高斯平滑 σ=6** —— 两个管线不可能逐像素对齐；不平滑的话**边缘错位**会刷出一片假热点。
  3. **连通域**（不是网格）—— 网格只能告诉你「这一格里不一样」，连通域能给你
     「**这里有一团东西，中心在 (0.14, 0.52)，面积 320 px，我们比原版亮 28**」—— 那才是能查的线索。
  4. **逐条出放大图**（原版 | 我们 并排），**每条一个文件** ⇒ 直接拿图去对。

用法：
  python -I -X utf8 d:/4/Unity/工具/arena_diff.py battlearena1
  python -I -X utf8 d:/4/Unity/工具/arena_diff.py all --top 6
  python -I -X utf8 d:/4/Unity/工具/arena_diff.py battlearena1 --out d:/4/_tmp_view/diff20 --ledger d:/4/Unity/资料/比对基线/arena_diff_ledger.tsv

⚠️ **它给的是「候选」，不是「缺陷」** —— 后处理（bloom/tonemap）、粒子随机相位、我们还没做的效果
   **都会进来**。判据永远是「拿这一条的放大图去比 + 回原版找那个对象」。
"""
import argparse
import glob
import io
import json
import os
import sys
from collections import deque

from PIL import Image, ImageFilter
import numpy as np

sys.stdout.reconfigure(encoding='utf-8')

ORIG_DIR = 'd:/4/Unity/资料/原版实拍/arena_0920'
OURS_FMT = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/{a}/preview_{a}.png'

# 场名 → 原版实拍文件名（**不是机械映射**，按 `arena_0920/` 里实际存在的 13 个名字写死）
NAME_MAP = {
    'battlearena1': 'Battle_Arena_1',
    'battlearena2': 'Battle_Arena_2',
    'battlearena3': 'Battle_Arena_3',
    'battlearenaaeldari': 'Battle_Arena_Aeldari',
    'battlearenaastramilitarum': 'Battle_Arena_Astra_Militarum',
    'battlearenablacklegion': 'Battle_Arena_Black_Legion',
    'battlearenadarkangels': 'Battle_Arena_Dark_Angels',
    'battlearenaemperorschildren': 'Battle_Arena_Emperors_Children',
    'battlearenagenestealers': 'Battle_Arena_Genestealers',
    'battlearenaleviathan': 'Battle_Arena_Leviathan',
    'battlearenasororitas': 'Battle_Arena_Sororitas',
    'battlearenaspacewolves': 'Battle_Arena_Space_Wolves',
    'battlearenatauviorla': 'Battle_Arena_Tau_Viorla',
}
ARENAS = list(NAME_MAP)

WORK = (640, 360)        # 分析用小图（连通域在这个尺度上找，够用且快）
SMOOTH = 6               # 平滑半径（px @ WORK 尺度）
PAD = 0.06               # 放大图往外留白（占全图比例）


def align(a, b, rng=9):
    """求整数平移 `(dx, dy)` 让 `b` 最贴 `a`；返回平移后的 `b`。

    🔴 **为什么必须有这一步**（2026-10-11 实测，差点让一整轮读数作废）：
    `battlearenatauviorla` 的两张图**整体竖直错位 ~2px**（640 宽尺度）= **1080p 下约 6px**，
    而且**全图 12 个分块各自的最佳平移完全一致**（都是 `(+0,+2)`）⇒ **纯平移，不是形变**。
    对齐前后：全图平均绝对差 **19.73 → 6.75**（**差 3 倍**）。
    ⚠️ 不做这一步，「每一处几何/亮度的细微差」都会被错位放大成**假候选** ——
    先前的 σ=6 高斯平滑**吃不下** 2px 的系统偏移（它治的是随机抖动，不是整体位移）。
    ⚠️ 找到的非零平移**本身也是情报**（我们的预览与实拍有系统性偏移）⇒ 由 `one()` 打出来。
    """
    ga, gb = a.mean(axis=2), b.mean(axis=2)
    H, W = ga.shape
    m = 12
    best = (1e18, 0, 0)
    for dy in range(-rng, rng + 1):
        for dx in range(-rng, rng + 1):
            gs = np.roll(np.roll(gb, dy, axis=0), dx, axis=1)
            d = np.abs(gs[m:H - m, m:W - m] - ga[m:H - m, m:W - m]).mean()
            if d < best[0]:
                best = (d, dx, dy)
    _, dx, dy = best
    return np.roll(np.roll(b, dy, axis=0), dx, axis=1), dx, dy


def tone_match(a, b):
    """把 `b`（我们）按通道 mean/std 匹配到 `a`（原版）。返回匹配后的 b。"""
    out = np.empty_like(b)
    for c in range(3):
        x, y = a[..., c], b[..., c]
        sx, sy = x.std(), y.std()
        out[..., c] = (y - y.mean()) * (sx / sy if sy > 1e-6 else 1.0) + x.mean()
    return np.clip(out, 0, 255)


def rank_match(a, b):
    """按**分位**把 `b` 匹配到 `a`（直方图匹配）—— 抹掉**任何单调色调差**（曝光 / 对比 / gamma / tonemap）。

    为什么要有它（2026-10-11 实测）：`battlearenatauviorla` 全图均值几乎相同（101.3 vs 100.9），
    但**对比度被压缩**（暗部我们 +26.6、亮部我们 −24）⇒ 线性 mean/std 归一化**压不掉它**，
    于是「整片地面」全被标成候选 —— **那是噪声，不是内容缺陷**。
    ⚠️ 代价：**真的色调缺陷（比如「环境光太高」）也会被一起抹掉** ⇒ 色调差要**单独报**（见 `tone_report`），
    ⛔ 不能只看 rank 匹配后的差异。
    """
    out = np.empty_like(b)
    n = b.shape[0] * b.shape[1]
    for c in range(3):
        x, y = a[..., c].ravel(), b[..., c].ravel()
        xs = np.sort(x)
        order = np.argsort(y, kind='stable')
        lut = np.empty(n, dtype=float)
        lut[order] = xs
        out[..., c] = lut.reshape(b.shape[:2])
    return out


def tone_report(a, b):
    """按**原版亮度**分箱报我们的偏差 —— **单独**的色调诊断（⛔ 不混进结构候选）。"""
    go, gu = a.mean(axis=2), b.mean(axis=2)
    rows = []
    for lo in range(0, 250, 25):
        m = (go >= lo) & (go < lo + 25)
        if m.sum() < 200:
            continue
        rows.append((lo, int(m.sum()), float((gu - go)[m].mean())))
    dev = [r[2] for r in rows]
    span = (max(dev) - min(dev)) if dev else 0.0
    if span < 8:
        verdict = '色调接近（未压缩）'
    elif dev and dev[0] > 0 and dev[-1] < 0:
        verdict = '🔴 **对比度被压缩**（暗部我们偏亮、亮部我们偏暗）'
    elif sum(dev) / max(1, len(dev)) > 4:
        verdict = '我们整体偏亮'
    elif sum(dev) / max(1, len(dev)) < -4:
        verdict = '我们整体偏暗'
    else:
        verdict = '色调偏差不大'
    return rows, verdict


def label_blobs(mask, min_px=12):
    """8 邻域连通域 → [(cx, cy, npix, bbox)]（坐标 = WORK 尺度像素）。"""
    h, w = mask.shape
    seen = np.zeros_like(mask, dtype=bool)
    out = []
    for y0 in range(h):
        xs = np.nonzero(mask[y0] & ~seen[y0])[0]
        for x0 in xs:
            if seen[y0, x0]:
                continue
            q = deque([(y0, x0)])
            seen[y0, x0] = True
            pts = []
            while q:
                y, x = q.popleft()
                pts.append((y, x))
                for dy in (-1, 0, 1):
                    for dx in (-1, 0, 1):
                        ny, nx = y + dy, x + dx
                        if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not seen[ny, nx]:
                            seen[ny, nx] = True
                            q.append((ny, nx))
            if len(pts) >= min_px:
                ys = [p[0] for p in pts]
                xs2 = [p[1] for p in pts]
                out.append((sum(xs2) / len(pts), sum(ys) / len(pts), len(pts),
                            (min(xs2), min(ys), max(xs2), max(ys))))
    return out


def one(arena, outdir, top, ledger, thr_hi, thr_lo, mode='rank'):
    orig_p = os.path.join(ORIG_DIR, 'shot_%s.png' % NAME_MAP[arena])
    ours_p = OURS_FMT.format(a=arena)
    if not os.path.exists(orig_p) or not os.path.exists(ours_p):
        print('⏭ %-28s 缺底图（原版 %s / 我们 %s）' % (arena, os.path.exists(orig_p), os.path.exists(ours_p)))
        return None
    orig = Image.open(orig_p).convert('RGB')
    ours = Image.open(ours_p).convert('RGB')
    a = np.asarray(orig.resize(WORK, Image.LANCZOS)).astype(float)

    # 🔴 **相位稳健**（账 `A1615`）：粒子区的逐像素差**主要是相位噪声** ——
    #   同一场扫 `WF_PSSEC` = 0.5/2/6/8/10/12/16，那块区域与原版的差在 **2.31 ↔ 8.70 之间【振荡】、不单调**
    #   ⇒ 拿**单张**预览去比，蒸汽/烟雾区的每一条候选都不可信（「我们多画了一大团白烟」有一大半是尺子造的）。
    #   做法：同一场渲**多个相位**（`preview_<场>__p*.png`，见 `工具/arena_diff_phases.sh`），**逐像素取中位数**
    #   ⇒ 只在**所有相位都不一致**的地方才算结构差。
    d = os.path.dirname(ours_p)
    alts = sorted(glob.glob(os.path.join(d, 'preview_%s__p*.png' % arena)))
    diffs = []
    adx = ady = 0
    for p in [ours_p] + alts:
        bi = np.asarray(Image.open(p).convert('RGB').resize(WORK, Image.LANCZOS)).astype(float)
        bi, x_, y_ = align(a, bi)
        if not diffs:
            adx, ady = x_, y_
        bn = rank_match(a, bi) if mode == 'rank' else tone_match(a, bi)
        diffs.append(np.abs(bn - a).mean(axis=2))
    n_phase = len(diffs)
    b = np.asarray(Image.open(ours_p).convert('RGB').resize(WORK, Image.LANCZOS)).astype(float)
    b, _, _ = align(a, b)
    b_n = rank_match(a, b) if mode == 'rank' else tone_match(a, b)
    raw = np.median(np.stack(diffs), axis=0) if n_phase > 1 else diffs[0]
    sm = np.asarray(Image.fromarray(np.clip(raw, 0, 255).astype(np.uint8))
                    .filter(ImageFilter.GaussianBlur(SMOOTH))).astype(float)
    # 🔴 `align()` 用的是 `np.roll`（**循环移位**）⇒ 被卷进来的那几条边带（顶部/底部 |dy| 行、左右 |dx| 列）
    #    内容其实是**对边**的像素 —— 不清掉的话，每场的「头号候选」都会是 **y≈0.01 的一条假带**
    #    （2026-10-11 实测：13 场里有 6 场第一名就是它）。⇒ 置 0，并往外多留 2 行防高斯拖尾。
    m = abs(ady) + 2
    if m:
        sm[:m, :] = 0.0; sm[-m:, :] = 0.0
    m = abs(adx) + 2
    if m:
        sm[:, :m] = 0.0; sm[:, -m:] = 0.0

    # 双阈值：`thr_hi` 先找「硬热点」；不够 top 就用 `thr_lo` 放宽
    blobs = []
    for thr in (thr_hi, thr_lo, 8.0):
        blobs = label_blobs(sm > thr)
        if len(blobs) >= top:
            break
    # 打分 = 面积 × 平均强度（大而亮 > 小而亮）
    scored = []
    for cx, cy, n, bb in blobs:
        x0, y0, x1, y1 = bb
        sub = sm[y0:y1 + 1, x0:x1 + 1]
        mean = float(sub.mean())
        # 方向：这一团里我们比原版【亮】还是【暗】
        d = (b_n - a).mean(axis=2)[y0:y1 + 1, x0:x1 + 1]
        scored.append({'cx': cx / WORK[0], 'cy': cy / WORK[1], 'px': n,
                       'box': [x0 / WORK[0], y0 / WORK[1], x1 / WORK[0], y1 / WORK[1]],
                       'mean': mean, 'score': n * mean, 'dir': '+' if d.mean() > 0 else '-'})
    scored.sort(key=lambda z: -z['score'])
    scored = scored[:top]

    os.makedirs(outdir, exist_ok=True)
    # 并排（原版 | 我们）+ 遮罩
    W, H = 720, 405
    o2, u2 = orig.resize((W, H), Image.LANCZOS), ours.resize((W, H), Image.LANCZOS)
    sbs = Image.new('RGB', (W * 2 + 8, H), (15, 15, 15))
    sbs.paste(o2, (0, 0)); sbs.paste(u2, (W + 8, 0))
    sbs.save(os.path.join(outdir, '%s_sbs.png' % arena))
    ov = np.asarray(u2).copy()
    nm = np.asarray(Image.fromarray(np.clip(sm, 0, 255).astype(np.uint8))
                    .resize((W, H), Image.BILINEAR)).astype(float) > (thr_lo * 0.9)
    ov[nm] = [255, 0, 255]
    Image.fromarray(ov).save(os.path.join(outdir, '%s_mask.png' % arena))

    # 逐条放大图（原版 | 我们，等比例、同输出宽）
    for i, z in enumerate(scored):
        x0, y0, x1, y1 = z['box']
        x0, y0 = max(0.0, x0 - PAD), max(0.0, y0 - PAD)
        x1, y1 = min(1.0, x1 + PAD), min(1.0, y1 + PAD)

        def crop(im):
            Wp, Hp = im.size
            c = im.crop((int(x0 * Wp), int(y0 * Hp), int(x1 * Wp), int(y1 * Hp)))
            return c.resize((300, max(1, int(300 * c.height / c.width))), Image.LANCZOS)
        ca, cb = crop(orig), crop(ours)
        h = max(ca.height, cb.height)
        c = Image.new('RGB', (610, h), (15, 15, 15))
        c.paste(ca, (0, 0)); c.paste(cb, (310, 0))
        c.save(os.path.join(outdir, '%s_z%02d.png' % (arena, i + 1)))

    print('\n=== %s ===' % arena)
    if adx or ady:
        print('  🔴 **对位**：我们的图相对原版整体平移 `(dx=%+d, dy=%+d)`（%d×%d 尺度；1080p 下 ≈ %+d px）'
              % (adx, ady, WORK[0], WORK[1], int(round(ady * 1080.0 / WORK[1]))))
        print('     ⚠️ 非零就说明**预览与实拍有系统性偏移** —— 它本身是个缺陷，别只当成对位参数。')
    else:
        print('  ✅ 对位：无需平移（两图已对齐）')
    rows, verdict = tone_report(a, b)
    print('  ── 色调（**单独看，⛔ 不混进下面的结构候选**）──')
    for lo, n, d in rows:
        bar = ('+' if d > 0 else '-') * min(20, int(abs(d) / 2))
        print('     原版亮度 %3d-%3d  n=%6d  偏差 %+6.1f  %s' % (lo, lo + 25, n, d, bar))
    print('     ⇒ **%s**' % verdict)
    print('  ── 结构（%s · **%d 个相位取中位**）──' % ('rank 分位匹配后' if mode == 'rank' else '线性 mean/std 后', n_phase))
    if n_phase == 1:
        print('     ⚠️ **只有 1 个相位** ⇒ 蒸汽/烟雾区的候选是**相位噪声**（账 `A1615`），'
              '先跑 `bash d:/4/Unity/工具/arena_diff_phases.sh <场>` 再判。')
    print('  全图平均差 %5.1f / 255 · 阈值 %.1f 下 %d 团' % (sm.mean(), thr_lo, len(blobs)))
    if not scored:
        print('  ✅ 没有超过阈值的连通域')
    for i, z in enumerate(scored):
        print('  #%d  中心(%.2f, %.2f)  面积%5d px  强度%5.1f  得分%6.0f  我们偏%s  放大图 → %s_z%02d.png'
              % (i + 1, z['cx'], z['cy'], z['px'], z['mean'], z['score'],
                 '亮' if z['dir'] == '+' else '暗', arena, i + 1))
    if ledger:
        os.makedirs(os.path.dirname(ledger), exist_ok=True)
        new = not os.path.exists(ledger)
        with io.open(ledger, 'a', encoding='utf-8', newline='') as fp:
            if new:
                fp.write('arena\tmean_diff\tn_blobs\ttop1_score\ttop1_center\ttop1_dir\n')
            t1 = scored[0] if scored else {'score': 0, 'cx': 0, 'cy': 0, 'dir': '?'}
            fp.write('%s\t%.2f\t%d\t%.0f\t(%.2f,%.2f)\t%s\n'
                     % (arena, sm.mean(), len(scored), t1['score'], t1['cx'], t1['cy'], t1['dir']))
    return sm.mean()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('arena', help='场名，或 all')
    ap.add_argument('--top', type=int, default=8, help='每场报几条（默认 8）')
    ap.add_argument('--out', default='d:/4/_tmp_view/arena_diff')
    ap.add_argument('--ledger', default=None, help='追加一行台账 TSV（跨轮次看分数降没降）')
    ap.add_argument('--thr-hi', type=float, default=30.0)
    ap.add_argument('--thr-lo', type=float, default=18.0)
    ap.add_argument('--mode', choices=('rank', 'linear'), default='rank',
                    help='rank（默认）= 分位匹配，抹掉任何单调色调差、只看结构；linear = 老的 mean/std')
    args = ap.parse_args()
    arenas = ARENAS if args.arena == 'all' else [args.arena]
    res = {}
    for a in arenas:
        m = one(a, args.out, args.top, args.ledger, args.thr_hi, args.thr_lo, args.mode)
        if m is not None:
            res[a] = m
    if len(res) > 1:
        print('\n=== 13 场排名（平均差，越小越像）===')
        for a, m in sorted(res.items(), key=lambda kv: kv[1]):
            print('  %-30s %5.1f' % (a, m))
    print('\n⚠️ 这些是【候选】不是【缺陷】—— 后处理/粒子相位/我们还没做的效果都会进来。'
          '\n   判据 = 拿放大图去比 + 回原版找那个对象。')


if __name__ == '__main__':
    main()
