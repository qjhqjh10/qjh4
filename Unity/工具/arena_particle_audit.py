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
 vs **我们清单里的键**。映射表在下面 `MAPPED`，`[未接]` 就是「清单里没这一列」。

🔴🔴 **别拿它的「缺口清单」派活**（2026-10-11 三个只读代理 `R1`/`F1`/`A3` 逐条核过；
   2026-10-23 `T06` 又按生成器现状把 `MAPPED` 订正了一遍，并修掉两处把它撑大的工具 bug）：
  · **判据口径（R1/F1/A3 逐条核过）= 真缺口只有 `EmissionModule.rateOverDistance`**
    —— 静止发射体上它本来就不发射 ⇒ 接了也没有画面变化。
  · **数值（`--arena battlearena1 --summary`，2026-10-23 现读）**：
    订正前 **56** 个 → 现在 **28** 个；其中 `rateOverDistance` 只剩 **10 个对象**（另 24 个原版就是 0）。
  · **现在还会报的 28 个 =「清单里没这一列」≠「画面错」**，分四类：
    ① 序列化内部字段（`customEmitterVelocity` / `m_BurstCount`）—— 不是用户参数（`NOT_A_PARAM`）；
    ② **当前形状用不到**的：`ShapeModule` 的 `m_Mesh*` / `m_Texture*` / `m_UseMesh*`
       （只在 Mesh / Sprite 形状下生效；13 场实测 `shapeType` 只有 0/2/4/5/8/10/12/13/18，**没有一颗用它们**）；
    ③ 默认值就等于原版的（`DEF` 表；⚠️ `donutRadius` 的默认已按 Unity 实读改成 **0.2**）；
    ④ **真的是没接的小项**（不影响结论、但不是噪音）：`cullingMode`(28) / `emitterVelocityMode`(26) /
       `randomPositionAmount`(25) / `ShapeModule.length`(9，锥高 0.85 ≠ 默认 5) /
       `UVModule` 的 `mode/frameOverTime/speedRange/uvChannelMask/rowMode/sprites/flipU/flipV/startFrame`
       （`ArenaBuilder` 只消费 `uvEnabled/tilesX/tilesY/uvAnimationType/uvTimeMode/uvFps/uvCycles/uvRowIndex`）/
       `autoRandomSeed`+`randomSeed`（各 1 个）。
       ⇒ 这一档**要补就按这一行补**，别按汇总数派活。
  · ⇒ **派活前看这一节**，⛔ 别看汇总行数。

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
# ⚠️ 系统级字段（`ps` 顶层，不带模块名）的键**不带前缀** —— 见 `mapped()` 的说明。
#
# 🔴 2026-10-23（`T06` / `A1559①`）按 `工具/scripts快照/gen_unity_arena_manifest.py` **现读**订正：
#   这几族**早就接了**，旧表漏登 ⇒ 一直被报成 `[未接]`：
#     · `simulationSpeed`（`:1736`）· `startDelay`（`:1738`）
#     · `startRotation` / `rotation3D` / `startRotationX` / `startRotationY`（`:1749-1754`；
#       `rotation3D=true` 时 Z 轴存在 `startRotation` 里）
#     · `randomizeRotationDirection`（`:1755`）· `size3D` / `startSizeY` / `startSizeZ`（`:1757-1759`）
#     · `ShapeModule.m_Position` / `m_Rotation` / `m_Scale`（`:1804-1806` ⇒ 清单 `shapePos/shapeRot/shapeScale`）
#     · `ShapeModule.radiusThickness`（`:1807`）
MAPPED = {
    'lengthInSec': 'duration',
    'looping': 'looping',
    'prewarm': 'prewarm',
    'moveWithTransform': 'simulationSpace',
    'scalingMode': 'scalingMode',
    'simulationSpeed': 'simulationSpeed',
    'startDelay': 'startDelay',
    'InitialModule.startLifetime': 'startLifetime',
    'InitialModule.startSpeed': 'startSpeed',
    'InitialModule.startSize': 'startSize',
    'InitialModule.startColor': 'startColor',
    'InitialModule.gravityModifier': 'gravityModifier / gravityCurve',
    'InitialModule.maxNumParticles': 'maxParticles',
    # 🔴 2026-10-23：这四个原来漏登（生成器 `:1749-1755` 早就抽了）
    'InitialModule.startRotation': 'startRotation（rotation3D 时 = Z 轴）',
    'InitialModule.rotation3D': 'rotation3D',
    'InitialModule.startRotationX': 'startRotationX',
    'InitialModule.startRotationY': 'startRotationY',
    'InitialModule.randomizeRotationDirection': 'randomizeRotationDirection',
    'InitialModule.size3D': 'size3D',
    'InitialModule.startSizeY': 'startSizeY',
    'InitialModule.startSizeZ': 'startSizeZ',
    'EmissionModule.rateOverTime': 'emissionRate / emissionRateCurve',
    'EmissionModule.m_Bursts': 'bursts',
    'ShapeModule.type': 'shapeType',
    'ShapeModule.radius': 'shapeRadius',
    'ShapeModule.angle': 'shapeAngle',
    'ShapeModule.arc': 'shapeArc',
    # 🔴 2026-10-23：`m_Position/m_Rotation/m_Scale/radiusThickness` 也早就接了（生成器 `:1804-1807`）
    'ShapeModule.m_Position': 'shapePos',
    'ShapeModule.m_Rotation': 'shapeRot',
    'ShapeModule.m_Scale': 'shapeScale',
    'ShapeModule.radiusThickness': 'shapeRadiusThickness',
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

# 🔴 2026-10-23（T06）：**Unity 序列化内部字段，不是用户参数** —— 生成器不抽是对的，
#   汇总里不该把它们算成「缺口」（会让人以为要去补）。
#   · `customEmitterVelocity`：ParticleSystem 的**运行时只读**缓存值（`InitialModule` 里那份），
#     Inspector 上根本没有这一格 —— 判据 = 原版 34/34 颗都有它、值恒等于发射体速度。
#   · `m_BurstCount`：`m_Bursts` 的**计数副本**；我们抽的是 `m_Bursts` 本体（⇒ 清单 `bursts`）。
NOT_A_PARAM = {'InitialModule.customEmitterVelocity', 'EmissionModule.m_BurstCount'}


def mapped(mod, field):
    """`mod` 为空 = 系统级字段（`ps` 顶层，`MAPPED` 里的键**不带前缀**）。
    ⚠️ 2026-10-23 修：原来统一拼成 `f'{mod}.{field}'`，系统级那批变成 `.lengthInSec`
    （带前导点）⇒ **一条都对不上** ⇒ `lengthInSec`/`looping`/`prewarm`/`moveWithTransform`/
    `scalingMode`/`simulationSpeed` 六项在汇总里**永远报「未接」**（arena1 每项 34 个对象）。"""
    if not mod:
        return field in MAPPED
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
            # 🔴 2026-10-23（T06）：**这里改成直接遍历原始值**（`v.items()`），不再走 `flat()`。
            #   原因：`flat()` 把 dict 值**转成 JSON 字符串**（那是给 `dump_obj` 打印用的），
            #   而 `_nondefault()` 拿到字符串只能 `float()` 失败 ⇒ 走「宁多报」⇒
            #   **每一个 Vector3 / PPtr 字段都被报成缺口**（`boxThickness`/`m_Mesh`/`m_Texture`… 25 个对象）。
            for p, val in v.items():
                if p in SKIP_FIELD or p == 'enabled' or mapped(k, p):
                    continue
                if f'{k}.{p}' in NOT_A_PARAM:
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
    # ⚠️ 2026-10-23 修：`donutRadius` 原来写 0 —— Unity 的默认是 **0.2**（新建空 ParticleSystem 实读），
    #    写 0 会让 13 场每一颗用 Donut 之外的形状也报「未接」（实测原版全是 0.2）。
    'radiusThickness': 1, 'length': 5, 'boxThickness': 0, 'donutRadius': 0.2,
    'alignToDirection': False, 'randomDirectionAmount': 0, 'sphericalDirectionAmount': 0,
    'arcMode': 0, 'positionAmount': 0, 'rotationAmount': 0, 'scaleAmount': 1,
    'radialVelocityAmount': 0, 'normalOffset': 0, 'arcSpread': 0, 'arcSpeed': 1,
    'placementMode': 0, 'm_MeshMaterialIndex': 0,
}


def _nondefault(field, v):
    """原版这个字段的值**是否偏离 Unity 默认**（偏离才算缺口）。

    🔴 2026-10-23（T06）修两处（原来这两个 bug 一起把「56 个缺口」里的一大半做成了假的）：
      ① **Vector3**（`boxThickness` / `m_Position` … 序列化成 `{x,y,z}`）原来落到 `float(dict)` 抛异常
         ⇒ 「宁多报」⇒ 25 个对象×每个向量字段各报一条；
      ② **PPtr 引用**（`m_Mesh` / `m_Sprite` / `m_Texture` / `m_MeshRenderer` … 是 `{m_FileID,m_PathID}`）
         不是数值参数 —— 它们只在对应形状分支下才被采（13 场实测 `shapeType` 只有
         0/2/4/5/8/10/12/13/18，**没有一颗用 Mesh / Sprite 形状**）。
      ③ `MinMaxCurve` 的包裹仍然是 `{value,mode,…}` ⇒ 照旧取 `value`。
    """
    d = DEF.get(field)
    if isinstance(v, dict):
        try:
            if 'value' in v:              # MinMaxCurve / MinMaxGradient 包裹（`{value,mode,…}`）
                v = v.get('value', 0)
            elif 'scalar' in v:
                # 🆕 2026-10-23：**Unity 旧序列化的 MinMaxCurve**（`{minMaxState,scalar,minScalar,maxScalar,…}`）
                #   原来认不出结构 ⇒ 「宁多报」⇒ `ShapeModule.randomPositionAmount` 这种**全 0** 的也被报 25 条。
                #   `TwoConstants` 时 `scalar` 不参与、真值在 `minScalar`/`maxScalar` ⇒ 三者取绝对值最大。
                v = max(abs(float(v.get(k) or 0)) for k in ('scalar', 'minScalar', 'maxScalar'))
                d = 0 if d is None else (float(d) if not isinstance(d, bool) else float(d))
            elif any(k in v for k in ('x', 'y', 'z')):     # Vector3 / Vector4
                v = max(abs(float(v.get(k) or 0)) for k in ('x', 'y', 'z'))
                d = 0 if d is None else (float(d) if not isinstance(d, bool) else float(d))
            elif 'm_PathID' in v or 'm_FileID' in v:       # PPtr 引用 —— 不是数值参数
                return False
            else:
                return True               # 认不出的结构：宁多报
        except (TypeError, ValueError):
            return True                   # 结构像但不是纯数值（例：嵌套曲线）⇒ 宁多报
    if d is None:
        return True                       # 没登记的字段：先当有值（宁多报）
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
