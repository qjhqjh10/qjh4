#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""arena_particle_audit.py — 把「原版战场的粒子」逐对象 · 逐模块 · 逐字段摊开，
并标出**哪些字段我们的清单（`<场>_manifest.json`）根本没接**。

为什么要有它（2026-09-22 立项，正本 `资料/战场13场_逐场对账_0920.md` §一 第 1 条 + 第 7 条）：
  · 用户圈出来的最大残余差异**全是粒子** —— 烟囱那儿我们是一**大团黑色实心球**
    （块差 −44.9），地面火我们**又小又暗**（块差 +37.7）。
  · 已经排除过：`maxParticleSize`（`WF_MAXPS=20` 实测无变化）· `renderMode`/`lengthScale`
    · 粒子贴图缺失（arena1 0 个）。
  · 剩下的可能性只有**「生成器没抽某个字段」**这一大类 —— 但**没人把「原版有哪些字段」和
    「我们抽了哪些字段」并排看过**。这个脚本就是那张并排表。

判据（唯一）：**原版 JSON（`d:/2/解包整理/07_场景/<场>/`）里的模块字段**
 vs **我们清单里的键**。映射表在下面 `MAPPED`，`[未接]` 就是缺口。

用法：
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/arena_particle_audit.py --obj SmokeEffect
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/arena_particle_audit.py --arena battlearena1 --summary
  D:/2/Warpforge_tools/py312/python.exe d:/4/Unity/工具/arena_particle_audit.py --all --summary
"""
import argparse
import io
import json
import os
import sys

GEN_DIR = 'd:/2/Warpforge_tools/scripts'
sys.path.insert(0, GEN_DIR)
import gen_unity_arena_manifest as G   # noqa: E402

ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']

# ---------------------------------------------------------------- 已接字段映射
# 键 = 原版 JSON 里的路径（`模块.字段`；`*` = 整段都接了），值 = 清单里的键。
# 🔴 改生成器时**这张表要跟着改** —— 它就是「我们到底抽了哪些字段」的唯一正本。
MAPPED = {
    'lengthInSec': 'duration',
    'looping': 'looping',
    'prewarm': 'prewarm',
    'moveWithTransform': 'simulationSpace',
    'scalingMode': 'scalingMode',
    'InitialModule.startLifetime': 'startLifetime',
    'InitialModule.startSpeed': 'startSpeed',
    'InitialModule.startSize': 'startSize',
    'InitialModule.startColor': 'startColor',
    'InitialModule.gravityModifier': 'gravityModifier / gravityCurve',
    'InitialModule.maxNumParticles': 'maxParticles',
    'EmissionModule.rateOverTime': 'emissionRate / emissionRateCurve',
    'EmissionModule.m_Bursts': 'bursts',
    'ShapeModule.type': 'shapeType',
    'ShapeModule.radius': 'shapeRadius',
    'ShapeModule.angle': 'shapeAngle',
    'ShapeModule.arc': 'shapeArc',
    'SizeModule.curve': 'sizeOverLifetime.x（separateAxes 时 X 存在 curve 里）',
    'SizeModule.x': 'sizeOverLifetime.x',
    'SizeModule.y': 'sizeOverLifetime.y',
    'SizeModule.z': 'sizeOverLifetime.z',
    'SizeModule.separateAxes': 'sizeOverLifetime.separateAxes',
    'ColorModule.gradient': 'colorOverLifetime',
    'UVModule.enabled': 'uvEnabled',
    'UVModule.tilesX': 'tilesX',
    'UVModule.tilesY': 'tilesY',
    'UVModule.animationType': 'uvAnimationType',
    'UVModule.timeMode': 'uvTimeMode',
    'UVModule.fps': 'uvFps',
    'UVModule.cycles': 'uvCycles',
    'UVModule.rowIndex': 'uvRowIndex',
    'VelocityModule.*': 'velocity',
    'ClampVelocityModule.*': 'clampVelocity',
    'NoiseModule.*': 'noise',
    'RotationModule.*': 'rotationOverLifetime',
    'SubModule.subEmitters': 'subEmitters',
}

# ------------------------------------------------------- 这些「看着像字段」其实是噪音
# `m_GameObject` / `m_PathID` 这种是序列化骨架，不是参数；打了星号的是「引用了别的对象」。
SKIP_FIELD = {'m_GameObject', 'm_FileID', 'm_PathID', 'm_CustomPlayableFullTypename',
              'm_ObjectHideFlags', 'm_CorrespondingSourceObject', 'm_PrefabInstance',
              'm_PrefabAsset', 'serializedVersion'}
# 原版 13 场**一个都没开**的模块（正本 §一 第 7 条已核实）—— 汇总时不报，省得刷屏
NEVER_ON = {'ForceModule', 'ExternalForcesModule', 'InheritVelocityModule',
            'LifetimeByEmitterSpeedModule', 'CollisionModule', 'TriggerModule',
            'LightsModule', 'CustomDataModule', 'SizeBySpeedModule',
            'RotationBySpeedModule', 'ColorBySpeedModule', 'TrailModule'}


def mapped(mod, field):
    return (f'{mod}.{field}' in MAPPED) or (f'{mod}.*' in MAPPED)


def flat(d, prefix=''):
    """把一个嵌套 dict 摊成 `路径 → 值`（只下钻一层模块；再深就打印成 JSON 串）。"""
    out = []
    for k, v in d.items():
        if k in SKIP_FIELD:
            continue
        p = f'{prefix}{k}'
        if isinstance(v, dict):
            out.append((p, json.dumps(v, ensure_ascii=False)[:220]))
        elif isinstance(v, list):
            out.append((p, json.dumps(v, ensure_ascii=False)[:220] if not v else
                        f'[{len(v)} 项] ' + json.dumps(v, ensure_ascii=False)[:200]))
        else:
            out.append((p, v))
    return out


def go_path(a, tpid):
    """沿 m_Father 上溯拼出 `A/B/C` 路径 —— **重名对象靠它区分**
    （arena1 里就有两个都叫 `SmokeEffect` 的，`WF_HIDE` 子串匹配分不开）。"""
    names, cur, guard = [], tpid, 0
    while cur is not None and cur in a.TF and guard < 200:
        guard += 1
        names.append(G.go_name_of(a, cur))
        cur = a.TF[cur].get('m_Father', {}).get('m_PathID')
    return '/'.join(reversed(names))


def collect(arena):
    """→ [{'path','name','ps','psr','t'}]（原版场景里全部 ParticleSystem）"""
    G.set_arena(arena)
    a = G.Assembler(G.SCENE, 'd:/tmp/wf_particle_audit')
    out = []
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        res = a.ps_of(gopid)
        if not res or res[0] is None:
            continue
        if G.root_name_of(a, t) in G.UI_PS_ROOTS:
            continue
        out.append({'t': t, 'path': go_path(a, t), 'name': G.go_name_of(a, t),
                    'ps': res[0], 'psr': G.psr_of(a, gopid) or {},
                    'active': G.active_in_hierarchy(a, t), 'a': a})
    return out, a


def load_manifest(arena):
    p = f'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/{arena}/{arena}_manifest.json'
    if not os.path.exists(p):
        return None
    return json.load(io.open(p, encoding='utf-8'))


def dump_obj(rec, mrec):
    """摊开一个对象的原版全字段（打 [已接]/[未接]），并与我们清单的对应值并排。"""
    ps, psr = rec['ps'], rec['psr']
    L = []
    L.append('=' * 96)
    L.append(f"对象 {rec['path']}   active={rec['active']}")
    if mrec is None:
        L.append('  🔴 我们的清单里**没有**这个对象')
    L.append('-' * 96)
    for k, v in ps.items():
        if k in SKIP_FIELD:
            continue
        if not isinstance(v, dict):
            mark = '[已接]' if mapped('', k) else '[未接]'
            L.append(f'  {mark} {k} = {v}'
                     + (f'   → 清单 {MAPPED[k]}' if k in MAPPED else ''))
            continue
        en = v.get('enabled')
        head = f'  ── {k}' + ('' if en is None else f' (enabled={en})')
        L.append(head)
        for p, val in flat(v, prefix=''):
            if p in ('enabled',):
                continue
            mk = '[已接]' if mapped(k, p) else '[未接]'
            L.append(f'      {mk} {p} = {val}')
    if psr:
        L.append('  ── ParticleSystemRenderer')
        for k, v in psr.items():
            if k in SKIP_FIELD:
                continue
            if isinstance(v, (dict, list)):
                continue
            L.append(f'      [参数] {k} = {v}')
    L.append('-' * 96)
    L.append('  我们的清单：' + (json.dumps(mrec, ensure_ascii=False)[:900] if mrec else '（无）'))
    return '\n'.join(L)


def summary(recs, man):
    """逐场汇总：**原版开着、而我们清单里没有任何对应键**的字段各有多少个对象中招。"""
    hits = {}      # (module, field) -> [对象路径…]
    for r in recs:
        ps = r['ps']
        for k, v in ps.items():
            if k in SKIP_FIELD:
                continue
            if not isinstance(v, dict):
                if not mapped('', k) and _nondefault(k, v):
                    hits.setdefault(('(系统级)', k), []).append(r['path'])
                continue
            if k in NEVER_ON:
                continue
            if not v.get('enabled'):
                continue
            for p, val in flat(v):
                if p == 'enabled' or mapped(k, p):
                    continue
                if _nondefault(p, val):
                    hits.setdefault((k, p), []).append(r['path'])
    return hits


# Unity 的「默认值」—— 等于默认的就不算缺口（写了也不会改行为）。
# 值是 Unity 序列化后的默认，出处 = 新建一个空 ParticleSystem 时它写出来的数（本机实测）。
DEF = {
    'simulationSpeed': 1.0, 'startDelay': 0, 'playOnAwake': True, 'cullingMode': 0,
    'ringBufferMode': 0, 'emitterVelocityMode': 0, 'useUnscaledTime': False,
    'autoRandomSeed': True, 'randomSeed': 0, 'stopAction': 0,
    'startRotation': 0, 'startRotationX': 0, 'startRotationY': 0,
    'randomizeRotationDirection': 0, 'flipRotation': 0,
    'startSizeY': 1, 'startSizeZ': 1, 'gravitySource': 0,
    'rateOverDistance': 0,
    'radiusThickness': 1, 'length': 5, 'boxThickness': 0, 'donutRadius': 0,
    'alignToDirection': False, 'randomDirectionAmount': 0, 'sphericalDirectionAmount': 0,
    'arcMode': 0, 'positionAmount': 0, 'rotationAmount': 0, 'scaleAmount': 1,
    'radialVelocityAmount': 0, 'normalOffset': 0, 'arcSpread': 0, 'arcSpeed': 1,
    'placementMode': 0, 'm_MeshMaterialIndex': 0,
}


def _nondefault(field, v):
    d = DEF.get(field)
    if d is None:
        return True                       # 没登记的字段：先当有值（宁多报）
    if isinstance(v, dict):               # {value,mode,spread,speed} 这种包裹
        v = v.get('value', 0)
    if isinstance(d, bool):
        return bool(v) != d
    try:
        return abs(float(v) - float(d)) > 1e-6
    except (TypeError, ValueError):
        return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default='battlearena1')
    ap.add_argument('--all', action='store_true', help='13 场都跑')
    ap.add_argument('--obj', default=None, help='只摊开名字含这个子串的对象（逐字段表）')
    ap.add_argument('--summary', action='store_true', help='只出「原版开着但我们没接」的汇总')
    ap.add_argument('--top', type=int, default=40)
    args = ap.parse_args()

    arenas = ARENAS if args.all else [args.arena]
    grand = {}
    for arena in arenas:
        recs, a = collect(arena)
        man = load_manifest(arena)
        print(f'=== {arena}: 原版 {len(recs)} 个 ParticleSystem，清单 '
              f'{len(man["particles"]) if man else "?"} 个 ===')
        if args.obj:
            for r in recs:
                if args.obj.lower() in r['name'].lower():
                    mrec = None
                    if man:
                        for m in man['particles']:
                            if m['go'] == r['name']:
                                mrec = m
                                break
                    print(dump_obj(r, mrec))
        if args.summary:
            h = summary(recs, man)
            for k, v in h.items():
                grand.setdefault(k, []).extend(f'{arena}:{p}' for p in v)
            print(f'  「原版开着 / 有值，我们没接」的字段：{len(h)} 个')
    if args.summary and grand:
        print('=' * 96)
        print('全部场次汇总（按中招对象数降序）—— **这就是缺口清单**')
        for (mod, f), v in sorted(grand.items(), key=lambda x: -len(x[1]))[:args.top]:
            sample = ', '.join(v[:3])
            print(f'  {len(v):4d} 个对象  {mod}.{f}    例：{sample}')
    return 0


if __name__ == '__main__':
    sys.exit(main())
