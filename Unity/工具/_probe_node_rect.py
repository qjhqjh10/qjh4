#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""_probe_node_rect.py — 把战斗场景里某个节点的矩形**相对某个祖先**算出来
（输出 = 面板内 px：原点 = 祖先的中心，**y 向上**；与本工程 `.cs` 里那套 `U(px)` 同口径）。

为什么不用 `menu_rect.py`：那条链从**根**往下走，而战斗场景的根 `BattleHud` 是**纯 `Transform`**
（RT 1471 没有 `RectTransform`）⇒ 工具在那一级断链、整表退化（两个工具都会印那行警告）。

用法：python -I -X utf8 工具/_probe_node_rect.py <bundle目录> <节点名> <祖先名>
"""
import io
import json
import os
import sys

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法：python -I -X utf8 工具/_probe_node_rect.py <bundle目录> <节点名> <祖先名>\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

sys.stdout.reconfigure(encoding="utf-8")
BUNDLES = "d:/2/新解包资源/assets_full"


def main():
    bundle, name, stop = sys.argv[1], sys.argv[2], sys.argv[3]
    root = os.path.join(BUNDLES, bundle)
    rt_dir = os.path.join(root, "RectTransform")
    go_dir = os.path.join(root, "GameObject")

    # ---- 索引：RectTransform pathID → (文件名, 对象)；GameObject 的组件表（GO → 它的 RT/T）----
    rts = {}
    for f in os.listdir(rt_dir):
        o = json.load(io.open(os.path.join(rt_dir, f), encoding="utf-8"))
        rts[f[:-5].split("_")[-1]] = (f, o)

    go_of_rt = {}          # RT pathID → GameObject 名字
    first_comp = {}        # GameObject 文件名（不含 .json）→ 第一颗组件的 pathID（= RectTransform/Transform）
    for f in os.listdir(go_dir):
        g = json.load(io.open(os.path.join(go_dir, f), encoding="utf-8"))
        nm = g.get("m_Name")
        comps = g.get("m_Component") or []
        for c in comps:
            t = (c.get("component") or c)
            pid = t.get("m_PathID")
            if t.get("m_FileID") in (0, None) and pid:
                go_of_rt.setdefault(str(pid), nm)
        if comps:
            t0 = (comps[0].get("component") or comps[0])
            first_comp[f[:-5]] = (str(t0.get("m_PathID")), nm)

    # ---- 目标节点的 RectTransform ----
    cand = [k for k, (f, o) in rts.items() if go_of_rt.get(k) == name]
    print(f"名字 = {name!r} 的 RectTransform 候选 = {len(cand)} 个（{cand}）")
    if not cand:
        raise SystemExit("❌ 找不到")
    pid = cand[0]

    chain = []
    seen = set()
    for _ in range(40):
        if pid not in rts:
            print(f"⚠️ 父 RT {pid} 不在本包 ⇒ 断链")
            break
        f, o = rts[pid]
        chain.append((pid, go_of_rt.get(pid), o))
        if go_of_rt.get(pid) == stop:
            break
        par = (o.get("m_Father") or {}).get("m_PathID")
        if not par or par in seen:
            break
        seen.add(par)
        pid = str(par)

    print("\n链（自下而上）：")
    for p, nm, o in chain:
        print(f"  {nm!r} (RT {p})  aMin={o['m_AnchorMin']} aMax={o['m_AnchorMax']} "
              f"piv={o['m_Pivot']} pos={o['m_AnchoredPosition']} sd={o['m_SizeDelta']} "
              f"scale={o.get('m_LocalScale')}")

    if go_of_rt.get(chain[-1][0]) != stop:
        print(f"\n⚠️ 没走到 {stop!r}（停在 {go_of_rt.get(chain[-1][0])!r}）⇒ 下面的换算**不可用**")
        return

    chain = list(reversed(chain))          # [祖先, …, 目标]
    anc = chain[0][2]
    cur_w = abs(anc["m_SizeDelta"]["x"])
    cur_h = abs(anc["m_SizeDelta"]["y"])
    cur_tl = (-cur_w / 2.0, -cur_h / 2.0)   # 祖先的左上角（相对祖先中心，y 向下）
    print(f"\n祖先 {stop!r}：尺寸 {cur_w} × {cur_h}（用它的 sd 当矩形 —— 若它自己被锚拉伸，这个值不对）")

    x1 = x2 = y1 = y2 = w = h = 0.0
    for p, nm, o in chain[1:]:
        a_min, a_max, piv, pos = o["m_AnchorMin"], o["m_AnchorMax"], o["m_Pivot"], o["m_AnchoredPosition"]
        sd = o["m_SizeDelta"]
        rx = a_min["x"] + (a_max["x"] - a_min["x"]) * piv["x"]
        ry = a_min["y"] + (a_max["y"] - a_min["y"]) * piv["y"]
        px = cur_tl[0] + rx * cur_w + pos["x"]
        py = cur_tl[1] + (1.0 - ry) * cur_h - pos["y"]
        w = abs(a_max["x"] - a_min["x"]) * cur_w + sd["x"]
        h = abs(a_max["y"] - a_min["y"]) * cur_h + sd["y"]
        x1, x2 = px - piv["x"] * w, px + (1 - piv["x"]) * w
        y1, y2 = py - (1 - piv["y"]) * h, py + piv["y"] * h
        cur_tl, cur_w, cur_h = (x1, y1), w, h

    cx, cy = (x1 + x2) / 2.0, (y1 + y2) / 2.0
    print(f"\n目标中心（相对 {stop!r} 中心，y 向下）= ({cx:.3f}, {cy:.3f})")
    print(f"⇒ 面板内（y 向上，本工程口径）= ({cx:.3f}, {-cy:.3f})；尺寸 = {w:.3f} × {h:.3f}")
    print(f"⇒ 矩形（y 向上）：x [{x1:.3f}, {x2:.3f}] · y [{-y2:.3f}, {-y1:.3f}]")


if __name__ == "__main__":
    main()
