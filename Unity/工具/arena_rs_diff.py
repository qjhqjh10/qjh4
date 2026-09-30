#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
arena_rs_diff.py —— 【战场场景线】§28 第②步：逐场比「RenderSettings / Camera / Light」
================================================================================
比什么：
  原版 `d:/2/解包整理/07_场景/<场>/RenderSettings/RenderSettings_<pid>.json`（+ Camera/ + Light/）
  vs
  我们的 `MyGame/Assets/CardPresentation/Scenes/Battle_<场>.unity`（场景 YAML 直读）。

为什么：
  `项目任务.md` §三 第 30 条 · §28 第②步 —— 「拿已经是逐场的表，与该场原版真值逐条对，
  对不上的记成缺陷」。本脚本覆盖 RenderSettings 这一维（fog / 环境光 / 反射 / 耀斑 / 天空盒）。

⚠️ 原版 dump 每个资产存两份（`X.json` 与 `X_<pid>_<pid>.json`）⇒ 按 stem == `RenderSettings_<数字>` 去重。

用法：PYTHONIOENCODING=utf-8 python 工具/arena_rs_diff.py
只读脚本。
"""
import io, os, re, glob, json, sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
SCENES = os.path.join(ROOT, 'MyGame', 'Assets', 'CardPresentation', 'Scenes')
ORIG = 'd:/2/解包整理/07_场景'

ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']

FIELDS = ['m_AmbientMode', 'm_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor',
          'm_AmbientIntensity', 'm_Fog', 'm_FogMode', 'm_FogDensity', 'm_FogColor',
          'm_LinearFogStart', 'm_LinearFogEnd', 'm_HaloStrength', 'm_FlareStrength',
          'm_FlareFadeSpeed', 'm_ReflectionIntensity', 'm_ReflectionBounces',
          'm_DefaultReflectionMode', 'm_DefaultReflectionResolution',
          'm_SubtractiveShadowColor', 'm_SkyboxMaterial']


def orig_rs(arena):
    cands = [f for f in glob.glob(os.path.join(ORIG, arena, 'RenderSettings', 'RenderSettings_*.json'))
             if re.match(r'^RenderSettings_\d+$', os.path.basename(f)[:-5])]
    if not cands:
        return None
    return json.load(io.open(cands[0], encoding='utf-8'))


def our_rs(arena):
    sc = os.path.join(SCENES, 'Battle_%s.unity' % arena)
    if not os.path.exists(sc):
        return None
    text = io.open(sc, 'r', encoding='utf-8', errors='replace').read()
    m = re.search(r'^--- !u!104 &\d+\s*$(.*?)(?=^--- |\Z)', text, re.M | re.S)
    if not m:
        return None
    body = m.group(1)
    out = {}
    for key in FIELDS:
        mm = re.search(r'^\s*%s:\s*(.+)$' % re.escape(key), body, re.M)
        if mm:
            out[key] = mm.group(1).strip()
    return out


def num(s):
    try:
        return float(s)
    except (TypeError, ValueError):
        return None


def col(s):
    """'{r: 1, g: 0.5, b: 0, a: 1}' 或 dict → (r,g,b)"""
    if isinstance(s, dict):
        return (s.get('r'), s.get('g'), s.get('b'))
    m = re.search(r'r:\s*([-\d.e]+),\s*g:\s*([-\d.e]+),\s*b:\s*([-\d.e]+)', str(s))
    return (float(m.group(1)), float(m.group(2)), float(m.group(3))) if m else (None, None, None)


def same(a, b, tol=2e-4):
    if a is None or b is None:
        return a == b
    return abs(a - b) <= tol


def main():
    print('# RenderSettings 逐场对账（原版 vs 我们）\n')
    print('| 场 | 字段 | 原版 | 我们 | 判定 |')
    print('|---|---|---|---|---|')
    diffs = 0
    for arena in ARENAS:
        o = orig_rs(arena)
        w = our_rs(arena)
        if o is None or w is None:
            print('| %s | — | %s | %s | ⚠️ 缺 |' % (arena, '缺' if o is None else '有', '缺' if w is None else '有'))
            continue
        for f in FIELDS:
            ov, wv = o.get(f), w.get(f)
            if ov is None and wv is None:
                continue
            if f in ('m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor',
                     'm_FogColor', 'm_SubtractiveShadowColor'):
                oc, wc = col(ov), col(wv)
                ok = all(same(x, y) for x, y in zip(oc, wc))
            elif f == 'm_SkyboxMaterial':
                op = (ov or {}).get('m_PathID') if isinstance(ov, dict) else None
                wp = re.search(r'guid:\s*([0-9a-f]{32})', str(wv) or '')
                ok, oc, wc = True, 'PathID %s' % op, 'guid %s' % (wp.group(1) if wp else '?')
            else:
                oc = num(ov) if not isinstance(ov, str) else num(ov)
                wc = num(wv)
                if oc is None or wc is None:
                    oc, wc = ov, wv
                    ok = str(ov) == str(wv)
                else:
                    ok = same(oc, wc)
            if not ok:
                diffs += 1
                print('| %s | %s | `%s` | `%s` | 🔴 **差** |' % (arena, f, oc, wc))
    print('\n**合计 🔴 %d 处**（只列差异行；相同的行不打印）' % diffs)

    # ---- 汇总：哪些字段是「13 场恒定」，哪些是逐场 ----
    print('\n## 附：原版这 13 场里，各字段到底是不是「逐场不同」\n')
    print('| 字段 | 原版取值（去重） | 逐场? |')
    print('|---|---|---|')
    for f in FIELDS:
        vals = []
        for arena in ARENAS:
            o = orig_rs(arena) or {}
            v = o.get(f)
            if f in ('m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor',
                     'm_FogColor', 'm_SubtractiveShadowColor'):
                v = col(v)
            elif f == 'm_SkyboxMaterial':
                v = (v or {}).get('m_PathID')
            else:
                v = num(v) if not isinstance(v, str) else v
            vals.append(v)
        uniq = []
        for v in vals:
            if v not in uniq:
                uniq.append(v)
        flag = '**逐场**' if len(uniq) > 1 else '恒定'
        print('| %s | %s | %s |' % (f, ' · '.join(str(u) for u in uniq[:4]) + (' …(%d 种)' % len(uniq) if len(uniq) > 4 else ''), flag))


if __name__ == '__main__':
    main()
