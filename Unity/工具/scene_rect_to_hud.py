#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""scene_rect_to_hud.py — 把**原版场景里的 RectTransform** 换算成我们 `HudAbs(...)` 那套坐标（只读）。

为什么要它
----------
我们的 HUD 是拿 `HudAbs(root, art, x, y, w, h, name, z)`（`BattleDriver.cs:28179`）一个个摆的；
原版那些锚点的真值在**场景 RectTransform** 里。`资料/说明书/01_战斗_对战/2D层_<场>全树.md` 只印
`[x, y wxh]`（= `m_AnchoredPosition` + `m_SizeDelta`），**没有 anchor/pivot**，而 anchor/pivot 不同
换算就不同 —— 所以**不能**拿树的 y 直接加减（踩过：两个节点的「和」分别是 1676.17 与 1433.86）。

换算公式（**已用 4 个节点验证**，见 `资料/完全复刻/判据/HUD光标与抽牌锚.md`）
----------------------------------------------------------------------
1) 画布局部矩形（原点 = 画布左下角、y 向上；画布 = 1920×1080）：
   从根往下逐级：
     size  = sizeDelta + (anchorMax − anchorMin) * parentSize
     rectMin = parentRectMin + anchorMin*parentSize + anchoredPosition − pivot*size
2) 换成 `HudAbs` 口径：
     x = rectMin.x                       （从画布左边量）
     y = 540 − (rectMin.y + size.y)      （540 = 画布半高；等价于「从画布中线往下量到矩形顶边」）
     w/h = size

用法
----
    python -I -X utf8 d:/4/Unity/工具/scene_rect_to_hud.py battlearena1 CardDrawPositions PlayerDrawnPos EnemyDrawnPos
    python -I -X utf8 d:/4/Unity/工具/scene_rect_to_hud.py --verify      # 跑内置的 4 个验证样本
"""
import io
import json
import os
import re
import sys

ASSETS = r"D:/2/新解包资源/assets_full"
CANVAS_W, CANVAS_H = 1920.0, 1080.0


def load(arena):
    b = os.path.join(ASSETS, "bundle_scenes_scenes_%s" % arena)
    if not os.path.isdir(b):
        cands = [d for d in os.listdir(ASSETS)
                 if d.startswith("bundle_scenes_scenes_") and arena in d]
        if not cands:
            raise SystemExit("找不到场景 bundle：" + arena)
        b = os.path.join(ASSETS, cands[0])
    rt = {}
    d = os.path.join(b, "RectTransform")
    for fn in os.listdir(d):
        if fn.endswith(".json"):
            pid = re.search(r"_(-?\d+)\.json$", fn).group(1)
            rt[pid] = json.load(io.open(os.path.join(d, fn), encoding="utf-8", errors="ignore"))
    comp2go = {}
    d = os.path.join(b, "GameObject")
    for fn in os.listdir(d):
        if not fn.endswith(".json"):
            continue
        t = json.load(io.open(os.path.join(d, fn), encoding="utf-8", errors="ignore"))
        for c in t.get("m_Component", []):
            comp2go[c["component"]["m_PathID"]] = fn
    gpid2name = {}
    for pid, r in rt.items():
        fn = comp2go.get(int(pid))
        if fn:
            gpid2name.setdefault(r["m_GameObject"]["m_PathID"], fn[:-5])
    return rt, gpid2name


def name_of(rt, gpid2name, pid):
    r = rt.get(str(pid))
    return gpid2name.get(r["m_GameObject"]["m_PathID"], "?") if r else None


def rect(rt, pid):
    """返回该节点在画布坐标（左下原点、y 向上）的 (xMin, yMin, sizeX, sizeY)。"""
    chain, cur, guard = [], str(pid), 0
    while cur != "0" and cur in rt and guard < 40:
        guard += 1
        chain.append(cur)
        cur = str(rt[cur]["m_Father"]["m_PathID"])
    px, py, pw, ph = 0.0, 0.0, CANVAS_W, CANVAS_H
    for cid in reversed(chain):
        r = rt[cid]
        amin, amax = r["m_AnchorMin"], r["m_AnchorMax"]
        piv, ap, sd = r["m_Pivot"], r["m_AnchoredPosition"], r["m_SizeDelta"]
        sx = sd["x"] + (amax["x"] - amin["x"]) * pw
        sy = sd["y"] + (amax["y"] - amin["y"]) * ph
        px = px + amin["x"] * pw + ap["x"] - piv["x"] * sx
        py = py + amin["y"] * ph + ap["y"] - piv["y"] * sy
        pw, ph = sx, sy
    return px, py, pw, ph


def hud(rt, pid):
    x, y, w, h = rect(rt, pid)
    return x, (CANVAS_H / 2.0) - (y + h), w, h


SAMPLES = [
    # (节点名, 我们 HudAbs 里写死的值 x,y,w,h)
    ("CenterCameraButton", (17.929, 568.174, 64.443, 61.846)),
    ("OffensiveButton", (0.0, 446.857, 109.008, 106.943)),
    ("OvertimeIndicator", (1718.882, 341.504, 68.625, 70.991)),
]


def main():
    if "--verify" in sys.argv:
        rt, gpn = load("battlearena1")
        print("节点 / 公式 / HudAbs 实测（x 允许 0.6 误差；w/h 同）")
        for nm, exp in SAMPLES:
            pid = next((p for p in rt if name_of(rt, gpn, p) == nm), None)
            if not pid:
                print("  %-20s 未找到" % nm)
                continue
            g = hud(rt, pid)
            dxok = abs(g[0] - exp[0]) < 0.6 or abs((CANVAS_W + g[0]) - exp[0]) < 0.6  # 右锚件我们代码里写的是 1920+x
            ok = dxok and abs(g[1] - exp[1]) < 0.6 and abs(g[2] - exp[2]) < 0.6 and abs(g[3] - exp[3]) < 0.6
            print("  %-20s 公式=(%9.3f,%9.3f,%8.3f,%8.3f) 实测=(%9.3f,%9.3f,%8.3f,%8.3f) %s%s"
                  % (nm, g[0], g[1], g[2], g[3], exp[0], exp[1], exp[2], exp[3],
                     "OK" if ok else "XX",
                     "" if abs(g[0] - exp[0]) < 0.6 else "   ← 这一件我们 HudAbs 写的是 1920+x（从右边量的）"))
        return 0

    if len(sys.argv) < 3:
        print(__doc__)
        return 2
    arena = sys.argv[1]
    rt, gpn = load(arena)
    print("场景 %s（画布 %.0fx%.0f）" % (arena, CANVAS_W, CANVAS_H))
    for want in sys.argv[2:]:
        hits = [p for p in rt if name_of(rt, gpn, p) == want]
        if not hits:
            print("  %-24s 找不到" % want)
            continue
        for p in hits:
            x, y, w, h = hud(rt, p)
            rx, ry, rw, rh = rect(rt, p)
            print("  %-24s pid=%-6s rectMin=(%9.3f,%9.3f) size=(%8.3f,%8.3f)  =>  HudAbs(x=%.3f, y=%.3f, w=%.3f, h=%.3f)"
                  % (want, p, rx, ry, rw, rh, x, y, w, h))
    return 0


if __name__ == "__main__":
    sys.exit(main())
