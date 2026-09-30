#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
arena_lc_diff.py —— 【战场场景线】§28 第②步：逐场比「灯光 / 相机」
================================================================================
原版（`d:/2/解包整理/07_场景/<场>/{Light,Camera,GameObject,Transform}/`）
  vs
我们的 manifest（`MyGame/Assets/WarpforgeArena1/arenas/<场>/<场>_manifest.json`）

比什么：
  · 平行光（`m_Type==1`）：颜色 / 强度 / 阴影强度 / 阴影 bias / 世界位置
  · BoardCamera：fov / near / far / lensShift / 世界位置 / 旋转

⚠️ 原版 dump 每个资产两份（`X.json` 与 `X_<pid>_<pid>.json`）⇒ 按 stem 去重。
⚠️ 引用解析走 `m_PathID` **在本文件内**查（`m_FileID==0`）；跨文件的记「未解析」，不猜。
用法：PYTHONIOENCODING=utf-8 python 工具/arena_lc_diff.py
只读脚本。
"""
import io, os, re, glob, json

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ORIG = 'd:/2/解包整理/07_场景'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']


def load_dir(arena, kind):
    """返回 {pid: json}（按 stem 去重，只留 `Kind_<pid>.json`）"""
    out = {}
    for f in glob.glob(os.path.join(ORIG, arena, kind, '%s_*.json' % kind)):
        stem = os.path.basename(f)[:-5]
        m = re.match(r'^%s_(\d+)$' % kind, stem)
        if not m:
            continue
        out[int(m.group(1))] = json.load(io.open(f, encoding='utf-8'))
    return out


def go_name(arena, pid, gos):
    g = gos.get(pid)
    return g.get('m_Name') if g else None


def transform_of(arena, go_pid, gos, trs):
    g = gos.get(go_pid)
    if not g:
        return None
    for c in g.get('m_Component', []) or []:
        cid = c.get('component', c)
        cp = cid.get('m_PathID') if isinstance(cid, dict) else cid
        if cp in trs:
            t = trs[cp]
            lp = t.get('m_LocalPosition', {})
            lr = t.get('m_LocalRotation', {})
            father = (t.get('m_Father') or {}).get('m_PathID', 0)
            return (lp.get('x'), lp.get('y'), lp.get('z'), lr.get('x'), lr.get('y'), lr.get('z'), lr.get('w'), father)
    return None


def close(a, b, tol=1e-3):
    if a is None or b is None:
        return False
    try:
        return abs(float(a) - float(b)) <= tol
    except (TypeError, ValueError):
        return False


def v3(seq):
    return tuple(round(float(x), 4) for x in seq) if seq else None


def main():
    print('# 灯光 / 相机 逐场对账（原版 vs 我们 manifest）\n')
    print('| 场 | 项 | 原版 | 我们 | 判定 |')
    print('|---|---|---|---|---|')
    bad = 0
    unresolved = []
    for arena in ARENAS:
        mf = os.path.join(ROOT, 'MyGame', 'Assets', 'WarpforgeArena1', 'arenas', arena, '%s_manifest.json' % arena)
        if not os.path.exists(mf):
            print('| %s | manifest | — | **缺文件** | ⚠️ |' % arena)
            continue
        man = json.load(io.open(mf, encoding='utf-8'))
        gos = load_dir(arena, 'GameObject')
        trs = load_dir(arena, 'Transform')
        lights, cams = load_dir(arena, 'Light'), load_dir(arena, 'Camera')

        # ---- 平行光 ----
        dl = [(pid, l) for pid, l in lights.items() if l.get('m_Type') == 1 and l.get('m_Enabled')]
        if not dl:
            unresolved.append('%s: 找不到启用的平行光' % arena)
        else:
            pid, l = dl[0]
            ol = (l.get('m_Color', {}).get('r'), l.get('m_Color', {}).get('g'), l.get('m_Color', {}).get('b'))
            ml = man.get('light', {})
            wl = tuple(ml.get('color') or ())
            for name, o, w in (('灯色', ol, wl),
                               ('灯强度', l.get('m_Intensity'), ml.get('intensity')),
                               ('阴影强度', (l.get('m_Shadows') or {}).get('m_Strength'), ml.get('shadowStrength')),
                               ('阴影bias', (l.get('m_Shadows') or {}).get('m_Bias'), ml.get('shadowBias'))):
                ok = all(close(a, b) for a, b in zip(o, w)) if isinstance(o, tuple) else close(o, w)
                if not ok:
                    bad += 1
                    print('| %s | %s | `%s` | `%s` | 🔴 **差** |' % (arena, name, o, w))
            t = transform_of(arena, (l.get('m_GameObject') or {}).get('m_PathID'), gos, trs)
            if t and t[7] in (0, None):
                op = (round(t[0], 5), round(t[1], 5), round(t[2], 5))
                wp = v3(ml.get('pos'))
                if not all(close(a, b, 1e-4) for a, b in zip(op, wp or ())):
                    bad += 1
                    print('| %s | 灯位置 | `%s` | `%s` | 🔴 **差** |' % (arena, op, wp))
            else:
                unresolved.append('%s: 灯不是根节点 / Transform 没解到（father=%s）' % (arena, t[7] if t else '?'))

        # ---- BoardCamera ----
        found = None
        for pid, c in cams.items():
            gp = (c.get('m_GameObject') or {}).get('m_PathID')
            if go_name(arena, gp, gos) == 'BoardCamera':
                found = (pid, c, gp)
                break
        if not found:
            unresolved.append('%s: 找不到 BoardCamera' % arena)
        else:
            _pid, c, gp = found
            mc = man.get('camera', {})
            for name, o, w in (('相机fov', c.get('field of view'), mc.get('fov')),
                               ('相机near', c.get('near clip plane'), mc.get('near')),
                               ('相机far', c.get('far clip plane'), mc.get('far')),
                               ('lensShiftY', (c.get('m_LensShift') or {}).get('y'), mc.get('lensShiftY'))):
                if not close(o, w):
                    bad += 1
                    print('| %s | %s | `%s` | `%s` | 🔴 **差** |' % (arena, name, o, w))
            t = transform_of(arena, gp, gos, trs)
            if t and t[7] in (0, None):
                op = (round(t[0], 5), round(t[1], 5), round(t[2], 5))
                wp = v3(mc.get('pos'))
                if not all(close(a, b, 1e-4) for a, b in zip(op, wp or ())):
                    bad += 1
                    print('| %s | 相机位置 | `%s` | `%s` | 🔴 **差** |' % (arena, op, wp))
            else:
                unresolved.append('%s: 相机不是根节点 / Transform 没解到' % arena)

    print('\n**合计 🔴 %d 处**（只列差异行）' % bad)
    if unresolved:
        print('\n**未能判定（不猜）**：')
        for u in unresolved:
            print('- ' + u)


if __name__ == '__main__':
    main()
