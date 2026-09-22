#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_unity_arena_manifest.py — 原版战场 → Unity Editor 清单 JSON（**13 个战场共用，用 --arena 选**）

读取 07_场景/<场景>/ 的原始 Unity 序列化 JSON 转储, 生成机器可读清单, 并把
清单实际引用到的模型/贴图拷进 Unity 项目。

产出（<场景> = `--arena` 的值）:
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/<场景>/<场景>_manifest.json
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/<场景>/Models/      (.obj)
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/<场景>/Textures/    (.png)

坐标: 一律 Unity 原始世界变换 (pos/rot/scale = world chain 结算), 不做任何手性/镜像转换。

用法:
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --arena battlearenaspacewolves
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --list-arenas
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --no-copy
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --all-particles
"""
import argparse
import json
import math
import os
import re
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_scene_to_godot import Assembler, q_mul, q_rot_vec, load_pid_dir, win_safe   # noqa: E402


# ------------------------------------------- 后处理（原版战场挂在 BoardCamera 上的全局 Volume）
def _val(v, default=None):
    """Unity `VolumeParameter` 是 {m_OverrideState, m_Value} 的包装。返回 (值, 是否被覆盖)。"""
    if not isinstance(v, dict) or 'm_Value' not in v:
        return default, False
    return v['m_Value'], bool(v.get('m_OverrideState', 0))


def read_postfx(scene_dir):
    """2026-09-20 新增。原版战场的后处理 = `BoardCamera` 上一个**全局 Volume**
    （`m_IsGlobal:1` / priority 0 / weight 1），profile 名 `<场景> PostProcessing`，里面 3 个 override：
      · **Bloom**      threshold 1.15 · intensity 5.0 · scatter 1.0 · skipIterations 6
      · **Vignette**   color 黑 · center (0.5,0.5) · intensity 0.297
      · **ColorLookup** LUT `LUT Normal` —— ⚠️ 实测**严格 identity**（4096 个采样点偏差 0/255，
        见 `资料/普查产出_0920/`）⇒ **不产生任何分级效果**，不接。
    原来我们一个都没接 —— 这是边角亮度对不上的一个来源。"""
    mb = load_pid_dir(os.path.join(scene_dir, 'MonoBehaviour'))
    # 1) 找 Volume 组件（`m_IsGlobal` + `sharedProfile` 两个字段同时有）
    vol = None
    for pid, d in mb.items():
        if isinstance(d, dict) and 'sharedProfile' in d and 'm_IsGlobal' in d:
            vol = d
            break
    if vol is None:
        return None
    prof_ref = vol.get('sharedProfile') or {}
    # ⚠️ 这里**不能**按 `m_FileID == 0` 判「同文件」—— 该 bundle 里 profile 与 Volume 分属两个
    #    serialized file（arena1 的 `m_FileID` 是 3），而 dump 目录已把整个 bundle 摊平
    #    ⇒ 直接按 PathID 在同一个目录里查。
    prof = mb.get(prof_ref.get('m_PathID'))
    if prof is None:
        return None

    out = {'profile': prof.get('m_Name', ''), 'isGlobal': bool(vol.get('m_IsGlobal', 1)),
           'priority': r6(vol.get('priority', 0.0)), 'weight': r6(vol.get('weight', 1.0)),
           'components': []}
    for c in prof.get('components', []):
        cd = mb.get(c.get('m_PathID'))
        if cd is None:
            continue
        nm = cd.get('m_Name', '')
        entry = {'type': nm}
        for k, v in cd.items():
            if k in ('m_GameObject', 'm_Script', 'm_Name', 'm_Enabled', 'components',
                     'm_ExcludedPropertiesInInspector', 'm_LockStageInInspector'):
                continue
            val, ov = _val(v)
            if val is None or isinstance(val, dict) and 'm_FileID' in val:
                continue                   # 资产引用（如 LUT 贴图）不搬
            if not ov:
                continue                   # 没打勾的 override 不生效，不写进清单
            if isinstance(val, dict) and 'r' in val:
                entry[k] = [r6(val['r']), r6(val['g']), r6(val['b']), r6(val.get('a', 1.0))]
            elif isinstance(val, dict) and 'x' in val:
                entry[k] = [r6(val['x']), r6(val['y'])]
            else:
                entry[k] = r6(val) if isinstance(val, float) else val
        # ⚠️ Bloom 特例：URP 的 bloom pass 读的是 **`maxIterations`**（`PostProcessPass.cs`
        #    `Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value)`），而 `skipIterations`
        #    在 URP 里是 `[Obsolete(..., true)]` —— **根本不读**。原版 Bloom 里
        #    `skipIterations` 打了勾=6、`maxIterations` 没打勾=6（= URP 默认，未打勾的
        #    VolumeParameter 用其默认值，也正好是 6）⇒ **生效值 = 6**。
        #    这里把 maxIterations 的「字段值」无论打没打勾都带上，免得下游再去猜。
        if nm == 'Bloom':
            mv, _ = _val(cd.get('maxIterations'))
            if mv is not None:
                entry['maxIterations'] = int(mv)
        out['components'].append(entry)
    return out


# ------------------------------------------------------------------ 路径常量
# 2026-09-20 起 **参数化**：原版 13 个战场共用同一条搬运链，逐场只是这几个路径不同。
#   python gen_unity_arena_manifest.py --arena battlearenaspacewolves
# 输出根从「一个战场一处」改成 `Assets/WarpforgeArena1/arenas/<场景名>/`，
# 免得 13 场的 Models/Textures/Materials 互相覆盖（那些目录都在 .gitignore 里）。
SCENE_ROOT       = 'd:/2/解包整理/07_场景/'
OBJ_ROOT         = 'd:/2/解包整理/06_模型/'
# 贴图兜底搜索目录 (场景自己的 Texture2D 里没有时按顺序往下找; 置空 = 严格只认场景目录,
# 找不到就写 null)。粒子特效贴图全部来自共享 bundle, 不在场景目录里。
# 🔴 2026-09-20 补 `内置资源`：`Default-Particle.png` 这类是 **Unity 内置贴图**，本地有这一份。
FALLBACK_TEX_DIRS = [
    'd:/2/解包整理/08_预制体特效/共享资源/Texture2D',
    'd:/2/解包整理/03_界面UI/去重资源/Texture2D',
    'd:/2/解包整理/12_主程序资源/内置资源/Texture2D',
]
# 🔴 2026-09-20 新增：**网格也有共享包**。`battlesharedresources` 里放着各场共用的网格
# （`Spore_Large1` / `CandleFlame_*` / `Combined Mesh (root_ scene)` 等）。
# 原来只认场景自己的 06_模型/scenes_scenes_<场景>/ ⇒ leviathan 的 `Spore_Large1` 会误报「不存在」。
FALLBACK_OBJ_DIRS = [
    'd:/2/解包整理/06_模型/battlesharedresources',
]

UNITY_ROOT = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1'

# 下面这些由 set_arena() 填；**别在别处再写死场景名**
SCENE         = 'battlearena1'
OBJ_DIR       = ''
SCENE_TEX_DIR = ''
ARENA_DIR     = ''
OUT_JSON      = ''
OUT_MODELS    = ''
OUT_TEXTURES  = ''

ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']

OBJ_SRC = {}     # 网格文件名 → 真实来源目录（由 obj_resolver 填；兜底目录里的不在 OBJ_DIR）


def set_arena(name):
    global SCENE, OBJ_DIR, SCENE_TEX_DIR, ARENA_DIR, OUT_JSON, OUT_MODELS, OUT_TEXTURES
    SCENE         = name
    OBJ_DIR       = OBJ_ROOT + 'scenes_scenes_' + name
    SCENE_TEX_DIR = SCENE_ROOT + name + '/Texture2D'
    ARENA_DIR     = UNITY_ROOT + '/arenas/' + name
    OUT_JSON      = ARENA_DIR + '/' + name + '_manifest.json'
    OUT_MODELS    = ARENA_DIR + '/Models'
    OUT_TEXTURES  = ARENA_DIR + '/Textures'


set_arena('battlearena1')            # 默认值；main() 会用 --arena 覆盖

CAMERA_NAME = 'BoardCamera'
LIGHT_NAME  = 'Directional Light'

# 2D UI 子树根名: 其下的 ParticleSystem 属于卡牌 UI 特效 (能量聚集等, 坐标在 UI 空间
# x≈-0.5/4.7/72/77), 不属于 3D 战场, 默认排除。--all-particles 可全部输出。
UI_PS_ROOTS = ('Energy Accumulation VFX On', 'Energy Accumulation VFX Off')

# 缺失字段的兜底默认值
DEF_DURATION      = 5.0
DEF_LOOPING       = True
DEF_PREWARM       = False
DEF_LIFETIME      = 1.0
DEF_SPEED         = 0.0
DEF_SIZE          = 1.0
DEF_COLOR         = (1.0, 1.0, 1.0, 1.0)
DEF_MAXPARTICLES  = 1000          # Unity ParticleSystem 默认值
DEF_EMISSION_RATE = 0.0
DEF_SHAPE_TYPE    = 0
DEF_SHAPE_RADIUS  = 0.0
DEF_SHAPE_ANGLE   = 0.0
DEF_SHAPE_ARC_DEG = 360.0
DEF_RENDER_MODE   = 0
DEF_SIM_SPACE     = 0

PI = math.pi


# ------------------------------------------------------------------ 数值工具
def r6(x):
    """float32 精度够用的可读化: 保留 6 位小数"""
    try:
        v = float(x)
    except (TypeError, ValueError):
        return 0.0
    if v != v or v in (float('inf'), float('-inf')):
        return 0.0
    return round(v, 6)


def _f(v, default=0.0):
    try:
        if v is None:
            return float(default)
        return float(v)
    except (TypeError, ValueError):
        return float(default)


def v3(d, default=(0.0, 0.0, 0.0)):
    if isinstance(d, dict):
        return [r6(d.get('x', default[0])), r6(d.get('y', default[1])), r6(d.get('z', default[2]))]
    return [r6(default[0]), r6(default[1]), r6(default[2])]


def quat4(d):
    if isinstance(d, dict):
        return [r6(d.get('x', 0.0)), r6(d.get('y', 0.0)), r6(d.get('z', 0.0)), r6(d.get('w', 1.0))]
    return [0.0, 0.0, 0.0, 1.0]


def color3(c, default=(0.0, 0.0, 0.0)):
    if isinstance(c, dict):
        return [r6(c.get('r', default[0])), r6(c.get('g', default[1])), r6(c.get('b', default[2]))]
    if isinstance(c, (list, tuple)) and len(c) >= 3:
        return [r6(c[0]), r6(c[1]), r6(c[2])]
    return [r6(default[0]), r6(default[1]), r6(default[2])]


def color4(c, default=DEF_COLOR):
    if isinstance(c, dict):
        return [r6(c.get('r', default[0])), r6(c.get('g', default[1])),
                r6(c.get('b', default[2])), r6(c.get('a', default[3]))]
    if isinstance(c, (list, tuple)) and len(c) >= 4:
        return [r6(c[0]), r6(c[1]), r6(c[2]), r6(c[3])]
    return [r6(x) for x in default]


# ------------------------------------------------- MinMaxCurve / MinMaxGradient
def _curve_keys(curve_obj):
    """m_Curve 里所有 key 的 value"""
    if not isinstance(curve_obj, dict):
        return []
    out = []
    for k in (curve_obj.get('m_Curve') or []):
        if isinstance(k, dict):
            out.append(_f(k.get('value'), 1.0))
    return out


def curve_minmax(v, default):
    """Unity MinMaxCurve → [min, max]

    🔴 **2026-09-21 更正：`minMaxState` 是 `ParticleSystemCurveMode`，
    枚举是 `0=Constant · 1=Curve · 2=TwoCurves · 3=TwoConstants`。**
    这里原来写的是「1=TwoConstants / 2=Curve / 3=TwoCurves」——**错位了**。
    一手证据（同一份场景 JSON 里自查）：`minMaxState=3` 的条目 `maxCurve.m_Curve` 全是**空数组**、
    而 `minScalar`/`scalar` 都有值（Gas fuming 0.1/0.7、Green Vapours 4.0/6.0）＝TwoConstants；
    `minMaxState=1` 的条目 `maxCurve` 非空、`minCurve` 空 ＝Curve。
    ⚠️ 旧写法在 `startSize` 上**碰巧**是对的（state 3 落到兜底 `[minScalar, scalar]`），
    但 `curve_single` 那侧被它伤到了 —— 见那里的注释。
    曲线态里 scalar/minScalar 是曲线乘数 (curveMultiplier)。
    """
    if not isinstance(v, dict):
        x = _f(v, default)
        return [r6(x), r6(x)]
    st = int(_f(v.get('minMaxState'), 0))
    scalar = _f(v.get('scalar'), default)
    mins = _f(v.get('minScalar'), default)
    if st == 0:                                   # Constant
        return [r6(scalar), r6(scalar)]
    if st == 3:                                   # TwoConstants
        return [r6(mins), r6(scalar)]
    vals = []                                     # Curve / TwoCurves
    vals += [scalar * x for x in _curve_keys(v.get('maxCurve'))]
    vals += [mins * x for x in _curve_keys(v.get('minCurve'))]
    if vals:
        return [r6(min(vals)), r6(max(vals))]
    return [r6(mins), r6(scalar)]        # 曲线为空 → 退回常量域


def curve_single(v, default=0.0):
    """MinMaxCurve → 单值 (gravityModifier / rateOverTime 这类清单里只放一个数)

    ⚠️ 这是**有损**的（曲线被压成一个数）。曲线态的完整数据由 `curve_raw()` 另存，
    构建侧优先用 `curve_raw` 的结果 —— 这个单值只当兜底/可读字段。
    🔴 2026-09-21 修：原来 `if st in (0, 1): return scalar` 把 **state=1（Curve）也当常量**
    返回，结果曲线只剩乘数。实测 `Fire Right.gravityModifier` 原版是 0→−0.662（×0.05），
    我们存成常数 **+0.05**（量级与符号都错）。
    """
    if not isinstance(v, dict):
        return r6(_f(v, default))
    st = int(_f(v.get('minMaxState'), 0))
    scalar = _f(v.get('scalar'), default)
    if st == 0:
        return r6(scalar)
    vals = [scalar * x for x in _curve_keys(v.get('maxCurve'))]
    if st == 2:
        vals += [_f(v.get('minScalar'), default) * x for x in _curve_keys(v.get('minCurve'))]
    if vals:
        return r6(max(vals))
    return r6(scalar)


def curve_raw(v):
    """MinMaxCurve → `{'mult': 乘数, 'keys': [{'t':…, 'v':…}, …]}`（**曲线态**才返回，常量态返回 None）。

    构建侧用 `new MinMaxCurve(mult, AnimationCurve)` 原样还原 —— 这是 2026-09-21 新增的，
    因为「拍成一个数」会改行为：原版 `emissionRate` 多为 (0,1)(0.41,0.35)(0.45,0)(1,0)×15
    （**只在循环前 ~2.5s 发射**），取峰值常数会让存活粒子多 2~3 倍。
    `TwoCurves`（state 2）额外带 `minKeys`/`multMin`。
    ⚠️ **键必须写成对象数组、不能写成 `[[t,v],…]`** —— C# 侧是 `JsonUtility.FromJson`，
    **它不支持交错数组（`float[][]`）**，写成嵌套数组整份清单都会解析失败。
    """
    if not isinstance(v, dict):
        return None
    st = int(_f(v.get('minMaxState'), 0))
    if st not in (1, 2):
        return None
    keys = []
    for c in ((v.get('maxCurve') or {}).get('m_Curve') or []):
        if isinstance(c, dict):
            keys.append({'t': r6(_f(c.get('time'), 0.0)), 'v': r6(_f(c.get('value'), 1.0))})
    if not keys:
        return None
    out = {'mult': r6(_f(v.get('scalar'), 1.0)), 'keys': keys}
    if st == 2:
        mk = []
        for c in ((v.get('minCurve') or {}).get('m_Curve') or []):
            if isinstance(c, dict):
                mk.append({'t': r6(_f(c.get('time'), 0.0)), 'v': r6(_f(c.get('value'), 1.0))})
        if mk:
            out['multMin'] = r6(_f(v.get('minScalar'), 1.0))
            out['minKeys'] = mk
    return out


def mm(v, default=0.0):
    """MinMaxCurve → **清单里的统一形状**（常量态和曲线态都能表达）。

    · 常量：`{'isConst': true, 'c': …, 'cMin': …}`
    · 曲线：`{'isConst': false, 'mult': …, 'keys': […], 'multMin': …, 'minKeys': …}`

    为什么要这样一个统一形状：VFX 那几个模块（velocity / noise / rotation…）的参数
    **大多是常量、少数是曲线**，分两套字段会让 C# 侧到处判空。
    ⚠️ 键一律是**对象数组**（`JsonUtility` 不支持交错数组）。
    """
    if not isinstance(v, dict):
        x = _f(v, default)
        return {'isConst': True, 'c': r6(x), 'cMin': r6(x)}
    st = int(_f(v.get('minMaxState'), 0))
    scalar = _f(v.get('scalar'), default)
    mins = _f(v.get('minScalar'), default)
    if st == 0:
        return {'isConst': True, 'c': r6(scalar), 'cMin': r6(scalar)}
    if st == 3:
        return {'isConst': True, 'c': r6(scalar), 'cMin': r6(mins)}
    keys = []
    for c in ((v.get('maxCurve') or {}).get('m_Curve') or []):
        if isinstance(c, dict):
            keys.append({'t': r6(_f(c.get('time'), 0.0)), 'v': r6(_f(c.get('value'), 1.0))})
    if not keys:
        return {'isConst': True, 'c': r6(scalar), 'cMin': r6(mins)}
    out = {'isConst': False, 'mult': r6(scalar), 'keys': keys}
    if st == 2:
        k2 = []
        for c in ((v.get('minCurve') or {}).get('m_Curve') or []):
            if isinstance(c, dict):
                k2.append({'t': r6(_f(c.get('time'), 0.0)), 'v': r6(_f(c.get('value'), 1.0))})
        if k2:
            out['multMin'] = r6(mins)
            out['minKeys'] = k2
    return out


def color_keys(gradient):
    """`ColorModule.gradient` → `{'colors': [{t,r,g,b}…], 'alphas': [{t,a}…]}`，否则 None。

    🔴 **2026-09-21 修**：原来只抽了 **alpha**（`alpha_keys`），**颜色那一半丢了** ——
    火焰「白→黄→橙→烟」的渐变就是靠颜色键，只建 alpha 的话火焰永远是一坨白。
    一手字段：颜色键 = `m_NumColorKeys` / `ctime{i}` / `key{i}.rgb`；
    alpha 键 = `m_NumAlphaKeys` / `atime{i}` / `key{i}.a`（**两套时间数组是分开的**）。
    """
    if not isinstance(gradient, dict):
        return None
    g = gradient.get('maxGradient')
    if not isinstance(g, dict):
        return None
    ncol = int(_f(g.get('m_NumColorKeys'), 0))
    nal = int(_f(g.get('m_NumAlphaKeys'), 0))
    if ncol < 2 and nal < 2:
        return None
    colors = []
    for i in range(min(ncol, 8)):
        c = g.get('key%d' % i) or {}
        colors.append({'t': round(_f(g.get('ctime%d' % i), 0.0) / 65535.0, 4),
                       'r': round(_f(c.get('r'), 1.0), 4), 'g': round(_f(c.get('g'), 1.0), 4),
                       'b': round(_f(c.get('b'), 1.0), 4)})
    alphas = []
    for i in range(min(nal, 8)):
        c = g.get('key%d' % i) or {}
        alphas.append({'t': round(_f(g.get('atime%d' % i), 0.0) / 65535.0, 4),
                       'a': round(_f(c.get('a'), 1.0), 4)})
    return {'colors': colors, 'alphas': alphas}


def mod_fields(ps, key):
    """取模块（没开就 None）。"""
    m = ps.get(key)
    return m if isinstance(m, dict) and m.get('enabled') else None


def velocity_fields(m):
    """`VelocityModule` —— **生命周期内的位移**（26/71 个对象有）。烟不飘的根就是它没建。"""
    if m is None:
        return None
    return {'inWorldSpace': bool(m.get('inWorldSpace')),
            'x': mm(m.get('x')), 'y': mm(m.get('y')), 'z': mm(m.get('z')),
            'radial': mm(m.get('radial')), 'speedModifier': mm(m.get('speedModifier')),
            'orbitalX': mm(m.get('orbitalX')), 'orbitalY': mm(m.get('orbitalY')),
            'orbitalZ': mm(m.get('orbitalZ')),
            'orbitalOffsetX': mm(m.get('orbitalOffsetX')),
            'orbitalOffsetY': mm(m.get('orbitalOffsetY')),
            'orbitalOffsetZ': mm(m.get('orbitalOffsetZ'))}


def clamp_velocity_fields(m):
    """`ClampVelocityModule`（限速 / 阻尼，10 个对象有）。"""
    if m is None:
        return None
    return {'separateAxis': bool(m.get('separateAxis')), 'inWorldSpace': bool(m.get('inWorldSpace')),
            # ⚠️ `dampen` 在 Unity API 里是 **float**（序列化里也是裸数 1.0），不是 MinMaxCurve
            'dampen': r6(_f(m.get('dampen'), 1.0)), 'drag': mm(m.get('drag')),
            'multiplyDragByParticleSize': bool(m.get('multiplyDragByParticleSize')),
            'multiplyDragByParticleVelocity': bool(m.get('multiplyDragByParticleVelocity')),
            'x': mm(m.get('x'), 1.0), 'y': mm(m.get('y'), 1.0), 'z': mm(m.get('z'), 1.0),
            'magnitude': mm(m.get('magnitude'), 1.0)}


def noise_fields(m):
    """`NoiseModule`（湍流，6 个对象有）—— 烟雾「有机扭动」的来源。"""
    if m is None:
        return None
    return {'separateAxes': bool(m.get('separateAxes')), 'damping': bool(m.get('damping')),
            'remapEnabled': bool(m.get('remapEnabled')),
            # ⚠️ `frequency` 在 Unity API 里是 **float**（序列化里也是裸数 2.0）；`scrollSpeed` 才是曲线
            'frequency': r6(_f(m.get('frequency'), 2.0)),
            'octaves': int(_f(m.get('octaves'), 1)),
            'octaveMultiplier': r6(_f(m.get('octaveMultiplier'), 0.5)),
            'octaveScale': r6(_f(m.get('octaveScale'), 2.0)),
            'quality': int(_f(m.get('quality'), 1)),
            'scrollSpeed': mm(m.get('scrollSpeed')),
            'strength': mm(m.get('strength')), 'strengthY': mm(m.get('strengthY'), 1.0),
            'strengthZ': mm(m.get('strengthZ'), 1.0),
            'positionAmount': mm(m.get('positionAmount'), 1.0),
            'rotationAmount': mm(m.get('rotationAmount')),
            'sizeAmount': mm(m.get('sizeAmount'))}


def rotation_fields(m):
    """`RotationModule`（自转，7 个对象有）。⚠️ `separateAxes=false` 时 Z 轴存在 `curve` 里。"""
    if m is None:
        return None
    sep = bool(m.get('separateAxes'))
    out = {'separateAxes': sep, 'x': mm(m.get('x')), 'y': mm(m.get('y')),
           'z': mm(m.get('curve'))}
    if sep and m.get('z') is not None:
        out['z'] = mm(m.get('z'))
    return out


def sub_emitters(a, m):
    """`SubModule`（子发射器，7 个对象有）—— 「火里蹦火星」就是它。

    引用的是**另一个 ParticleSystem 组件的 PathID** ⇒ 解析成那个对象的 **GameObject 名字**，
    构建侧按名字找（同一份清单里一定有）。
    """
    if m is None:
        return None
    out = []
    for se in (m.get('subEmitters') or []):
        if not isinstance(se, dict):
            continue
        pid = (se.get('emitter') or {}).get('m_PathID')
        ps = a.PS.get(pid) if pid else None
        if not isinstance(ps, dict):
            continue
        gopid = ps.get('m_GameObject', {}).get('m_PathID')
        nm = (a.GO.get(gopid) or {}).get('m_Name') if gopid else None
        if not nm:
            continue
        out.append({'target': nm, 'type': int(_f(se.get('type'), 0)),
                    'emitProbability': r6(_f(se.get('emitProbability'), 1.0))})
    return out or None


def size_over_lifetime(ps):
    """`SizeModule`（sizeOverLifetime）→ `{'separateAxes': bool, 'x'/'y'/'z': curve_raw}`，否则 None。

    原版常用它做「随生命长大」：leviathan 的 `Gas`/`Fume Burst` 是 0.16~0.23 → 1.0
    （**前段只有最终尺寸的 1/5**），我们没建 ⇒ 全程满尺寸，平均大 2~4 倍。
    """
    sm = ps.get('SizeModule')
    if not isinstance(sm, dict) or not sm.get('enabled'):
        return None
    sep = bool(sm.get('separateAxes'))
    out = {'separateAxes': sep}
    if sep:
        # ⚠️ `separateAxes=true` 时 Unity 把 **X 轴**序列化成 `curve`（不是 `x`），
        #    y/z 才是独立字段 —— 实读 `ParticleSystem_1762`：curve 的 scalar=0.5、y/z 各 1.0。
        cx = curve_raw(sm.get('curve')) or curve_raw(sm.get('x'))
        if cx:
            out['x'] = cx
        for ax in ('y', 'z'):
            c = curve_raw(sm.get(ax))
            if c:
                out[ax] = c
    else:
        c = curve_raw(sm.get('curve'))
        if not c:
            return None
        for ax in ('x', 'y', 'z'):
            out[ax] = dict(c)
    if not any(k in out for k in ('x', 'y', 'z')):
        return None
    return out


def gradient_color(v, default=DEF_COLOR):
    """MinMaxGradient → 单个 RGBA

    0/1 = Color/TwoColors → maxColor; 2/3 = Gradient → maxColor (AssetRipper 已把
    ColorMax 落成有意义的代表色, 曲线本体只存在于 ColorModule)。都没有则取 maxGradient.key0。
    """
    if not isinstance(v, dict):
        return color4(v, default)
    c = v.get('maxColor')
    if isinstance(c, dict) and (c.get('a', 0.0) or c.get('r', 0.0) or c.get('g', 0.0) or c.get('b', 0.0)):
        return color4(c, default)
    g = v.get('maxGradient')
    if isinstance(g, dict) and isinstance(g.get('key0'), dict):
        return color4(g['key0'], default)
    if isinstance(c, dict):
        return color4(c, default)
    return [r6(x) for x in default]


def shape_dim(v, default):
    """ShapeModule 的 radius/arc 是 {value, mode, spread, speed} 结构, 不是 MinMaxCurve"""
    if isinstance(v, dict):
        return _f(v.get('value'), default)
    if isinstance(v, (int, float)):
        return float(v)
    return float(default)


def burst_count(v):
    """EmissionModule.m_Bursts[].countCurve (MinMaxCurve) → 单个数; 完全取不到返回 None

    0=Constant(scalar) 1=TwoConstants(minScalar/scalar) 2/3=Curve(key 最大值 × 乘数)
    兜底链: 曲线 → minScalar → scalar → None (调用方补 1.0)
    """
    if isinstance(v, (int, float)):
        return r6(v)
    if not isinstance(v, dict):
        return None
    st = int(_f(v.get('minMaxState'), 0))
    scalar = v.get('scalar')
    mins = v.get('minScalar')
    if st == 0:
        if scalar is not None:
            return r6(_f(scalar, 0.0))
    elif st == 1:
        if mins is not None:
            return r6(_f(mins, 0.0))
        if scalar is not None:
            return r6(_f(scalar, 0.0))
    else:
        vals = [_f(scalar, 1.0) * x for x in _curve_keys(v.get('maxCurve'))]
        if vals:
            return r6(max(vals))
        if scalar is not None:
            return r6(_f(scalar, 0.0))
    for x in (mins, scalar):
        if x is not None:
            return r6(_f(x, 0.0))
    return None


def active_in_hierarchy(a, tpid):
    """沿 `m_Father` 上溯，把每一层 GameObject 的 `m_IsActive` 与起来。

    🔴 **2026-09-21 新增** —— 原来生成器**从不读 `m_IsActive`**，原版关着的对象也照建。
    实测受害：`battlearena3` 的 `Particle Effects/TorchEffectNecron/Fire/Light` 与
    `TorchEffectNecron (1)/Fire/Light`（原版 `activeInHierarchy=False`、淡绿 ×2），
    我们建出来一开场就满屏绿 ⇒ 「arena3 绿光溢出」。
    """
    cur = tpid
    seen = 0
    while cur and seen < 64:
        td = a.TF.get(cur)
        if not td:
            break
        gopid = td.get('m_GameObject', {}).get('m_PathID')
        go = a.GO.get(gopid) if gopid else None
        if isinstance(go, dict) and not go.get('m_IsActive', True):
            return False
        cur = td.get('m_Father', {}).get('m_PathID')
        seen += 1
    return True


def psr_of(a, gopid):
    """取该 GameObject 上的 ParticleSystemRenderer（没有就 None）。"""
    for c in a.go_comps.get(gopid, []):
        r = a.PSR.get(c)
        if r:
            return r
    return None


def burst_list(em, warn, gname):
    """EmissionModule → bursts 数组 [{time, count, cycles, interval, probability}]"""
    out = []
    for be in (em.get('m_Bursts') or []):
        if not isinstance(be, dict):
            continue
        cnt = burst_count(be.get('countCurve'))
        if cnt is None:
            warn['burst_count_missing'].append({'go': gname, 'time': r6(_f(be.get('time'), 0.0))})
            cnt = 1.0
        out.append({
            'time': r6(_f(be.get('time'), 0.0)),
            'count': cnt,
            'cycles': max(1, int(_f(be.get('cycleCount'), 1))),
            'interval': r6(max(0.01, _f(be.get('repeatInterval'), 0.01))),
            'probability': r6(min(1.0, max(0.0, _f(be.get('probability'), 1.0)))),
        })
    return out


def uv_fields(ps):
    """UVModule (Unity 序列化里 TextureSheetAnimation 叫这名) → 翻页图集 8 字段

    uvEnabled 缺省 false; tilesX/tilesY 缺省 1; uvFps 缺省 30; uvCycles 缺省 1;
    uvAnimationType/uvTimeMode/uvRowIndex 缺省 0。
    """
    uv = ps.get('UVModule') or {}
    if not isinstance(uv, dict):
        uv = {}
    return {
        'uvEnabled': bool(uv.get('enabled', False)),
        'tilesX': int(_f(uv.get('tilesX'), 1)),
        'tilesY': int(_f(uv.get('tilesY'), 1)),
        'uvAnimationType': int(_f(uv.get('animationType'), 0)),
        'uvTimeMode': int(_f(uv.get('timeMode'), 0)),
        'uvFps': r6(_f(uv.get('fps'), 30.0)),
        'uvCycles': r6(_f(uv.get('cycles'), 1.0)),
        'uvRowIndex': int(_f(uv.get('rowIndex'), 0)),
    }


# ------------------------------------------------------------------ 世界变换
def world_chain(a, tpid):
    """沿 m_Father 上溯累乘 → (world_pos, world_quat)。抄自 gen_spec_scenes.py"""
    chain = []
    cur = tpid
    while cur is not None and cur in a.TF:
        chain.append(a.local_trans(cur))
        cur = a.TF[cur].get('m_Father', {}).get('m_PathID')
    p = [0.0, 0.0, 0.0]
    q = [0.0, 0.0, 0.0, 1.0]
    for lt in reversed(chain):
        rp = q_rot_vec(tuple(q), lt[0:3])
        p = [p[0] + rp[0], p[1] + rp[1], p[2] + rp[2]]
        q = list(q_mul(tuple(q), lt[3]))
    return p, q


def _mat_mul(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def _local_matrix(lt):
    px, py, pz, (qx, qy, qz, qw), (sx, sy, sz) = lt
    x2, y2, z2 = qx + qx, qy + qy, qz + qz
    xx, xy, xz = qx * x2, qx * y2, qx * z2
    yy, yz, zz = qy * y2, qy * z2, qz * z2
    wx, wy, wz = qw * x2, qw * y2, qw * z2
    r = [[1 - (yy + zz), xy - wz, xz + wy, 0.0],
         [xy + wz, 1 - (xx + zz), yz - wx, 0.0],
         [xz - wy, yz + wx, 1 - (xx + yy), 0.0],
         [0.0, 0.0, 0.0, 1.0]]
    for i in range(3):
        r[i][0] *= sx
        r[i][1] *= sy
        r[i][2] *= sz
    r[0][3], r[1][3], r[2][3] = px, py, pz
    return r


def world_scale(a, tpid):
    """世界矩阵列长 = Unity Transform.lossyScale"""
    M = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
    chain = []
    cur = tpid
    while cur is not None and cur in a.TF:
        chain.append(a.local_trans(cur))
        cur = a.TF[cur].get('m_Father', {}).get('m_PathID')
    for lt in reversed(chain):
        M = _mat_mul(M, _local_matrix(lt))
    return [r6(math.sqrt(sum(M[i][j] ** 2 for i in range(3)))) for j in range(3)]


def world_trs(a, tpid):
    p, q = world_chain(a, tpid)
    return [r6(p[0]), r6(p[1]), r6(p[2])], [r6(x) for x in q], world_scale(a, tpid)


def go_name_of(a, tpid):
    return a.go(a.TF[tpid].get('m_GameObject', {}).get('m_PathID')).get('m_Name', '?') or '?'


def root_name_of(a, tpid):
    """上溯到根, 返回根 GO 名 (判定 UI 子树用)"""
    cur, guard = tpid, 0
    while cur is not None and cur in a.TF and guard < 200:
        guard += 1
        fa = a.TF[cur].get('m_Father', {}).get('m_PathID')
        if fa not in a.TF:
            return go_name_of(a, cur)
        cur = fa
    return ''


# ------------------------------------------------------------------ 资源解析
class Resolver:
    """文件名 → 真实磁盘路径 (含兜底目录); 记录命中来源"""

    def __init__(self, primary, fallbacks):
        self.dirs = []
        for d in [primary] + list(fallbacks):
            if d and os.path.isdir(d):
                self.dirs.append(d)
        self.primary = primary
        self.used = {}          # filename -> 实际来源目录
        self._index = []
        for d in self.dirs:
            try:
                self._index.append((d, {n.lower(): n for n in os.listdir(d)}))
            except OSError:
                pass

    def find(self, name):
        """返回真实存在的文件名 (原名), 找不到 None。
        🔴 2026-09-20：磁盘上的文件名是**消毒过的**（`win_safe` 把 `\\/:*?"<>|` 换成 `_`，
        因为 `:` 在 NTFS 上是 ADS 流），所以查表前要先消毒 —— 不然
        `Combined Mesh (root: scene)` 永远找不到它自己的 `Combined Mesh (root_ scene).png`。"""
        if not name:
            return None
        want = (win_safe(name) + '.png').lower()
        for d, idx in self._index:
            real = idx.get(want)
            if real:
                self.used[real] = d
                return real
        return None

    def path_of(self, filename):
        d = self.used.get(filename)
        return os.path.join(d, filename) if d else None

    def from_primary(self, filename):
        return self.used.get(filename) == self.primary


# ------------------------------------------- Unity 侧材质判据 / OBJ 手性还原

def _tex_alpha_stats(path):
    """返回 (不透明%, 全透明%)；读不到返回 (None, None)"""
    try:
        from PIL import Image
        im = Image.open(path).convert('RGBA')
        px = list(im.getdata())
        n = len(px)
        if not n:
            return None, None
        op = sum(1 for t in px if t[3] > 250)
        cl = sum(1 for t in px if t[3] < 5)
        return round(100.0 * op / n, 1), round(100.0 * cl / n, 1)
    except Exception:
        return None, None


def unity_mat_fields(mi, tex_path):
    """
    Unity 侧的材质渲染判据。

    注意：**不要用 MatInfo.is_transparent()** —— 那个是给 Godot 管线用的，判据是 `_Blend > 0`，
    而 Warpforge 用的是自定义 shader，`_Blend` 恒为 0，会把 126/350 个真 Alpha 混合材质误判成不透明
    （见 解包整理/_审计报告_0910.md A2）。权威判据是 `_SrcBlend`/`_DstBlend` + `_AlphaClip` + 关键字，
    再叠加「贴图全透明像素占比」兜底 —— 有材质标着 Opaque 却用着 26% 全透明的贴图（背景板就是），
    照不透明渲染就会糊成大白板。
    """
    sb = float(mi.src_blend) if (mi and mi.src_blend is not None) else None
    db = float(mi.dst_blend) if (mi and mi.dst_blend is not None) else None
    # 🔴 2026-09-20：**先过「残留值」闸门** —— 材质上的 `_SrcBlend`/`_DstBlend` 不一定说了算。
    #    原版材质留着**内置 Standard shader 的残留值**，而原版 shader 的属性表里可能根本没声明它们
    #    （实测 45 个原版 shader：**15 个有、30 个没有**）。没声明时 Unity 忽略材质值，
    #    **真值在 pass 的 `rtBlend0`（硬编码）**。
    #    踩过的实例 = sororitas 的 `Floor`：`Everguild/FX/Floor Planar Reflections Grainny`
    #    （属性 8 个、无 `_SrcBlend`，pass 硬编码 One/Zero = 不透明）被判成透明 ⇒
    #    那块地板整片透掉、露出默认天空盒（13 场里只有它肉眼可见，因为它的图 91.3% 全透明）。
    #    ⚠️ **不能一刀切全按不透明** —— 有 15 个 shader 是真的在 pass 里间接寻址 `[_SrcBlend]`，
    #       那些照旧采材质值（材质值就是它们说的）。
    #    详见 `资料/普查产出_0920/场景光照与后处理_原版规格.md` §13.1。
    si = getattr(mi, 'shader_info', None) if mi else None
    if si and not si.get('has_src_blend') and si.get('pass_src') is not None:
        sb, db = float(si['pass_src']), float(si['pass_dst'])
    ac = float(mi.alpha_clip) if (mi and mi.alpha_clip is not None) else None
    kw = set(mi.keywords or []) if mi else set()

    transparent = False
    alpha_clip = (ac is not None and ac > 0.5) or ('_ALPHATEST_ON' in kw)
    if sb is not None and db is not None:
        if abs(sb - 5.0) < 0.01 and abs(db - 10.0) < 0.01:
            transparent = True          # SrcAlpha / OneMinusSrcAlpha
        elif abs(sb - 5.0) < 0.01 and abs(db - 1.0) < 0.01:
            transparent = True          # 预乘 Alpha
        elif abs(sb - 1.0) < 0.01 and abs(db - 0.0) < 0.01:
            transparent = False         # 真不透明
    if '_SURFACE_TYPE_TRANSPARENT' in kw or '_ALPHABLEND_ON' in kw:
        transparent = True

    op_pct = cl_pct = None
    if tex_path and os.path.isfile(tex_path):
        op_pct, cl_pct = _tex_alpha_stats(tex_path)

    # 🔴 2026-09-20：**只要读到了 shader 的属性表，就信 shader 这一侧的判据** ——
    #    贴图统计（`forceBlend` / Unity 侧那条 `TextureHasAlpha`）是**猜**，猜不该盖过已知。
    #    ⚠️ 原来只在「属性表里**没有** `_SrcBlend`」时才让位，结果 **材质值明确写 `1/0`（不透明）**
    #    的材质照样被 `forceBlend` 拉回透明 —— 实例：sororitas 的 `Floor#sub1`
    #    （shader = `Everguild/UnlitAmbient`、属性表里有 `_SrcBlend`、材质值 `1/0` ⇒ 原版**不透明**，
    #    而它那张图集 91.3% 全透明 ⇒ 被兜底改判成透明 ⇒ **整块地板透掉、露出天空盒**）。
    #    这是 `holes_*.png` 诊断图（摘掉天空盒刷亮绿）当场量出来的。
    shader_authoritative = bool(si is not None)
    force_blend = (not shader_authoritative and not transparent and not alpha_clip
                   and cl_pct is not None and cl_pct > 15.0)

    return {
        'srcBlend': int(sb) if sb is not None else None,
        'dstBlend': int(db) if db is not None else None,
        'alphaClip': bool(alpha_clip),
        'forceBlend': bool(force_blend),
        'transparent': bool(transparent),
        'blendAuthoritative': shader_authoritative,
        # 🔴 2026-09-20：把**原版 shader 名**带出来 —— Unity 侧要拿它去 `WarpforgeShaderLoader`
        #    取原版编译字节码来建材质（原来一律用 `URP/Unlit`，就是把**环境光那层压暗**整个丢了：
        #    实测暗部偏亮 1.4~1.9×，而亮部只差 1.05×）。取不到就退回 `URP/Unlit` 并报警。
        'shader': (si.get('name') if si else None),
        # 🆕 2026-09-21：**整张属性表原样带出去**（`unity_scene_to_godot.parse_mat` 收的 `raw_props`）。
        #    用途：Unity 侧「**运行时**用原版 shader 重建材质」时把参数按原版灌回去
        #    （`ArenaBuilder.RematerializeWithOriginalShaders`）。
        #    格式 = `[{'k':属性名, 't':'f'|'c', 'f':标量 或 'c':[r,g,b,a]}]`
        #    ⚠️ 用**数组**不用字典 —— 清单是 `JsonUtility` 读的，它不吃字典。
        'props': (list(getattr(mi, 'raw_props', []) or []) if mi else []),
        'texOpaquePct': op_pct,
        'texClearPct': cl_pct,
    }


def restore_obj(src, dst):
    """
    把 UnityPy 导出的 X 镜像 OBJ 还原成 Unity 原始手性，写到 dst（**绝不改 src**）。

    见 解包整理/_审计报告_0910.md A1：UnityPy 的 MeshExporter 做了「顶点 x 取负 + 面绕序反转」
    的不完整左手→右手转换（UV 的 u 没跟着镜像）。直接用会让每个网格左右反、背面剔除和法线也错。
    还原规则：`v`/`vn` 的 x 取负；`f` 的索引倒序；`vt`（UV）**一律不动**。
    """
    out = []
    with open(src, encoding='utf-8', errors='replace') as f:
        for line in f:
            p = line.split()
            if not p:
                out.append(line)
                continue
            if p[0] in ('v', 'vn') and len(p) >= 4:
                out.append('%s %.9G %.9G %.9G\n' % (p[0], -float(p[1]), float(p[2]), float(p[3])))
            elif p[0] == 'f' and len(p) >= 4:
                out.append('f ' + ' '.join(reversed(p[1:])) + '\n')
            else:
                out.append(line)
    with open(dst, 'w', encoding='utf-8') as f:
        f.writelines(out)


def obj_resolver():
    """场景自己的模型目录 + 共享包兜底 → (小写名→(真实文件名, 来源目录), OBJ_DIR)。
    兜底目录里的文件不在 OBJ_DIR 里，所以另建一张 `OBJ_SRC`（文件名 → 真实来源目录）
    给 `copy_assets` 用 —— 不能一律拿 `OBJ_DIR` 去拼。"""
    global OBJ_SRC
    OBJ_SRC = {}
    dirs = [d for d in [OBJ_DIR] + FALLBACK_OBJ_DIRS if os.path.isdir(d)]
    if not dirs:
        return None, {}
    idx = {}
    for d in dirs:
        for n in os.listdir(d):
            if n.lower().endswith('.obj') and n.lower() not in idx:
                idx[n.lower()] = (n, d)          # 先到先得：场景目录优先于共享包
                OBJ_SRC[n] = d
    return idx, OBJ_DIR


# ------------------------------------------- 默认环境条件 SO（运行时环境光真源）
# 2026-09-20 新增。为什么必须读它：
#   静态场景里的 `RenderSettings.m_AmbientSkyColor` **不是运行时用的值** ——
#   实况探针（SceneJumpShot `LogRenderEnvFacts`，2026-09-20 跑 battlearena1 + battlearena3）
#   与全量反编译两条独立证据都指向同一件事：
#     `EnvironmentConditionsController`(m_IsActive=true) / `ScenarioEnvironmentConditionsManager`
#     的 `Awake()` → `ApplyEnvironment(defaultEnvironment, instant:true)` →
#     `EnableEnvironment` → `ApplyAmbientColor` → `RenderSettings.ambientLight = <SO>.ambientColor`
#   实况读数：arena1 得 (1,1,1,α0)（SO 的 α=0；场景是 α=1）、arena3 得 (0.80660,0.95225,1)
#   （SO 值 (0.8066,0.9522,1)；场景值是 (0.6840,0.9229,1)）⇒ **运行时跟 SO，不跟场景**。
#   13 场共 **6 个不同**的默认环境 SO（8 场共用 Ultramarines 那个）。
#   判据出处：`decomp_full/ScenarioEnvironmentConditionSO__ApplyAmbientColor.c` ·
#   `ScenarioEnvironmentConditionsManager__Awake.c` · `…__ApplyEnvironment.c` · `…__EnableEnvironment.c`
AA_DIRS = [
    'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
    'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64',
]
CAB_MAP = 'd:/2/Warpforge_tools/data/cab_bundle_map.json'


def read_default_environment(scene, warn):
    """读 `<scene>` 的 `ScenarioEnvironmentConditionsManager.defaultEnvironment` 那个 SO。
    返回 dict 或 None（失败时往 warn['default_env'] 写原因，**不静默**）。"""
    try:
        import UnityPy
    except Exception as e:
        warn['default_env'].append('UnityPy 不可用: %s' % e)
        return None

    aa = next((d for d in AA_DIRS if os.path.isdir(d)), None)
    if aa is None:
        warn['default_env'].append('找不到 aa 目录（原始 bundle）')
        return None
    bundle = os.path.join(aa, 'scenes_scenes_%s.bundle' % scene)
    if not os.path.isfile(bundle):
        warn['default_env'].append('bundle 不存在: %s' % bundle)
        return None

    # 1) 场景里找挂在管理器上的 defaultEnvironment 引用
    ref = None
    ext_cab = None
    try:
        env = UnityPy.load(bundle)
        for obj in env.objects:
            if obj.type.name != 'MonoBehaviour':
                continue
            try:
                d = obj.read_typetree()
            except Exception:
                continue
            if 'defaultEnvironment' not in d:
                continue
            ref = d['defaultEnvironment']
            exts = obj.assets_file.externals
            fi = int(ref.get('m_FileID', 0))
            if fi <= 0 or fi > len(exts):
                warn['default_env'].append('defaultEnvironment 是本文件内对象（m_FileID=%d）' % fi)
                return None
            ext_cab = exts[fi - 1].path.split('/')[1]      # archive:/CAB-xxxx/CAB-xxxx
            break
    except Exception as e:
        warn['default_env'].append('读场景 bundle 失败: %s' % e)
        return None
    if ref is None:
        warn['default_env'].append('场景里没有 ScenarioEnvironmentConditionsManager')
        return None

    # 2) CAB → bundle 文件名
    host = None
    if os.path.isfile(CAB_MAP):
        with open(CAB_MAP, encoding='utf-8') as fp:
            cmap = json.load(fp)
        v = cmap.get(ext_cab)
        host = (v[0] if isinstance(v, list) else v) if v else None
    if not host:
        warn['default_env'].append('CAB %s 不在 cab_bundle_map.json 里（重新生成该映射表）' % ext_cab)
        return None

    # 3) 到宿主 bundle 里按 PathID 取 SO
    so_path = os.path.join(aa, host)
    if not os.path.isfile(so_path):
        warn['default_env'].append('宿主 bundle 不存在: %s' % so_path)
        return None
    want = int(ref['m_PathID'])
    try:
        env2 = UnityPy.load(so_path)
        for o2 in env2.objects:
            if o2.path_id != want:
                continue
            so = o2.read_typetree()
            return {
                'so': so.get('m_Name', ''),
                'hostBundle': host,
                'pathId': want,
                'blendTime': r6(so.get('blendTime', 2.0)),
                'ambientColor': color4(so.get('ambientColor'), (1.0, 1.0, 1.0, 1.0)),
                'ambientBlend': r6(so.get('ambientBlend', 0.0)),
                'fogColor': color4(so.get('fogColor'), (1.0, 1.0, 1.0, 1.0)),
                'fogDensity': r6(so.get('fogDensity', 0.0)),
            }
    except Exception as e:
        warn['default_env'].append('读宿主 bundle 失败: %s' % e)
        return None
    warn['default_env'].append('宿主 bundle %s 里没有 PathID %d' % (host, want))
    return None


# ------------------------------------------------------------------ 清单构建
def build_manifest(a, include_all_particles=False):
    warn = {'obj_missing': [], 'tex_missing': [], 'tex_fallback': [], 'ps_defaults': {},
            'burst_count_missing': [], 'default_env': []}

    def note_default(field):
        warn['ps_defaults'][field] = warn['ps_defaults'].get(field, 0) + 1

    tex = Resolver(SCENE_TEX_DIR, FALLBACK_TEX_DIRS)
    obj_index, _ = obj_resolver()

    # ---------------- 相机 ----------------
    camera = None
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        for c in a.go_comps.get(gopid, []):
            if c not in a.CAM:
                continue
            d = a.CAM[c]
            nm = go_name_of(a, t)
            if camera is not None and nm != CAMERA_NAME:
                continue
            p, q, _ = world_trs(a, t)
            ls = d.get('m_LensShift') or {}
            camera = {
                'name': nm,
                'pos': p, 'rot': q,
                'fov': r6(d.get('field of view', 46.39718246459961)),
                'near': r6(d.get('near clip plane', 0.3)),
                'far': r6(d.get('far clip plane', 300.0)),
                'lensShiftY': r6(ls.get('y', 0.0)),
            }
            if nm == CAMERA_NAME:
                break
        if camera and camera['name'] == CAMERA_NAME:
            break

    # ---------------- 灯光 ----------------
    # 2026-09-20：判据从「名字 == 'Directional Light'」改成「m_Type == 1（平行光）且 m_Enabled」。
    # 理由（13 场实测）：名字**其实通吃**（13/13 都叫 `Directional Light`，见
    # `d:/4/Unity/资料/普查产出_0920/`），但名字不是语义 —— 真正要的是「那盏平行光」。
    # 原版每场**恰好 1 盏** `m_Type==1`；场景里另有相机组等非光对象，按类型取才稳。
    light = None
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        for c in a.go_comps.get(gopid, []):
            if c not in a.LIG:
                continue
            d = a.LIG[c]
            if int(d.get('m_Type', 1)) != 1:      # 1 = Directional
                continue
            if not int(d.get('m_Enabled', 1)):
                continue
            nm = go_name_of(a, t)
            if light is not None and nm != LIGHT_NAME:
                continue
            sh = d.get('m_Shadows') or {}
            p, q, _ = world_trs(a, t)
            light = {
                'name': nm, 'pos': p, 'rot': q,
                'color': color3(d.get('m_Color'), (1.0, 1.0, 1.0)),
                'intensity': r6(d.get('m_Intensity', 1.0)),
                # 2026-09-20 补：原版 13 场的 m_Shadows 各不相同（soft + 强度 0.591/0.65/0.725/1.0
                # 四档），原来只写死 LightShadows.Soft、强度从没搬过
                'shadowType': int(sh.get('m_Type', 2)),      # 0=None 1=Hard 2=Soft
                'shadowStrength': r6(sh.get('m_Strength', 1.0)),
                'shadowBias': r6(sh.get('m_Bias', 0.05)),
            }
            if nm == LIGHT_NAME:
                break
        if light and light['name'] == LIGHT_NAME:
            break

    # ---------------- 环境光 (RenderSettings) ----------------
    # 2026-09-20 更正：原来只抄 sky/ground/intensity，**丢了 m_AmbientMode**，而原版 13 场
    # 全是 m_AmbientMode = 3（Flat 单色），不是 1（Trilight）。模式错了，抄对颜色也没用。
    # ⚠️ 且**场景里的值不是运行时用的值** —— 实况（SceneJumpShot 探针，2026-09-20）实测
    # `EnvironmentConditionsController/ScenarioEnvironmentConditionsManager.Awake()` 会用
    # `defaultEnvironment.ambientColor` 覆盖 `RenderSettings.ambientLight`（见 default_env()）。
    ambient = {'mode': 3, 'sky': [1.0, 1.0, 1.0], 'equator': [0.3255, 0.5862, 1.0],
               'ground': [0.59, 0.59, 1.0], 'intensity': 0.41, 'fog': False,
               'fogColor': [1.0, 1.0, 1.0], 'fogDensity': 0.0}
    for r in a.REN.values():
        ambient = {
            'mode': int(r.get('m_AmbientMode', 3)),
            'sky': color3(r.get('m_AmbientSkyColor'), (1.0, 1.0, 1.0)),
            'equator': color3(r.get('m_AmbientEquatorColor'), (0.3255, 0.5862, 1.0)),
            'ground': color3(r.get('m_AmbientGroundColor'), (0.59, 0.59, 1.0)),
            'intensity': r6(r.get('m_AmbientIntensity', 0.41)),
            'fog': bool(r.get('m_Fog', False)),
            'fogColor': color3(r.get('m_FogColor'), (1.0, 1.0, 1.0)),
            'fogDensity': r6(r.get('m_FogDensity', 0.0)),
        }
        break

    # ---------------- 默认环境条件 SO（运行时真正的环境光来源）----------------
    default_env = read_default_environment(SCENE, warn)

    # ---------------- 后处理（BoardCamera 上的全局 Volume）----------------
    postfx = read_postfx(os.path.join(SCENE_ROOT, SCENE))

    # ---------------- 网格 ----------------
    meshes = []
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        mesh_name, _mesh_obj = a.mesh_of(gopid)
        if not mesh_name:
            continue
        gname = go_name_of(a, t)
        p, q, s = world_trs(a, t)

        hit = obj_index.get((win_safe(mesh_name) + '.obj').lower())
        obj_file = hit[0] if hit else None
        if not obj_file:
            warn['obj_missing'].append({'go': gname, 'obj': mesh_name})

        mats = a.mats_of(gopid)
        mi = mats[0] if mats else None
        tex_name = mi.tex_name if mi else None
        tex_file = tex.find(tex_name) if tex_name else None
        if tex_name and not tex_file:
            warn['tex_missing'].append({'go': gname, 'tex': tex_name})
        elif tex_file and not tex.from_primary(tex_file):
            warn['tex_fallback'].append({'go': gname, 'tex': tex_file, 'dir': tex.used[tex_file]})
        if len(mats) > 1:
            print('  [注意] %s 有 %d 个材质, 清单只取第 1 个' % (gname, len(mats)))

        matf = unity_mat_fields(mi, tex.path_of(tex_file) if tex_file else None)

        # 🔴 2026-09-20：**多子网格网格要把每一个材质都带上**。
        #    原来只取 `mats[0]`（下面那条 `[注意]` 提示早就在打，只是没人处理），而 OBJ 里
        #    `g <名>_0` / `g <名>_1` … 会被 Unity 导成**多个子网格** ⇒ 第 1..N 个子网格拿不到材质
        #    ⇒ **Unity 用默认灰材质**。实测后果：圣女战场的 `Floor` 整个变成一片均匀灰
        #    （(106,101,96)，而原版是红毯 + 大理石）。
        #    实测面：**13 场里 6 场共 29 个网格**有多子网格（arena2 的 `Combined Mesh (root_ scene)`
        #    有 **31 个**！· leviathan 22 个背景楼 · tauviorla 的 `Floor`/`Energy Bar` ·
        #    blacklegion 的 `Background_Ring_12` · sororitas 的 `Floor`）。**arena1 恰好是 0 个**
        #    —— 这就是为什么只有 arena1 一直看着很好。
        sub_mats = []
        for m in mats:
            tn = m.tex_name
            tf = tex.find(tn) if tn else None
            if tn and not tf:
                warn['tex_missing'].append({'go': gname, 'tex': tn})
            mf2 = unity_mat_fields(m, tex.path_of(tf) if tf else None)
            sub_mats.append({
                'tex': tn, 'texFile': tf,
                'baseColor': color3(m.base_color, (1.0, 1.0, 1.0)),
                'emission': color3(m.emission_color, (0.0, 0.0, 0.0)),
                'cull': int(_f(m.cull, 2)),
                'transparent': mf2['transparent'],
                'srcBlend': mf2['srcBlend'], 'dstBlend': mf2['dstBlend'],
                'alphaClip': mf2['alphaClip'], 'forceBlend': mf2['forceBlend'],
                'blendAuthoritative': mf2['blendAuthoritative'],
                'shader': mf2['shader'],
                'props': mf2['props'],       # 🆕 2026-09-21：整张属性表（运行时重建材质用）
            })

        sub_files = None
        if obj_file and len(sub_mats) > 1:
            src_obj = os.path.join(OBJ_SRC.get(obj_file, OBJ_DIR), obj_file)
            if os.path.isfile(src_obj):
                with open(src_obj, 'r', encoding='utf-8', errors='replace') as fh:
                    parts = split_obj_by_group(fh.read())
                if len(parts) > 1:
                    base = obj_file[:-4] if obj_file.lower().endswith('.obj') else obj_file
                    sub_files = [base + sfx + '.obj' for sfx, _ in parts]

        meshes.append({
            'go': gname,
            'obj': mesh_name,
            'objFile': obj_file,
            'subFiles': sub_files,        # ← 多子网格时按组拆出来的若干 OBJ（见 split_obj_by_group）
            'pos': p, 'rot': q, 'scale': s,
            'tex': tex_name,
            'texFile': tex_file,
            'subMats': sub_mats,          # ← 每个子网格一个（顺序 = MeshRenderer 的 m_Materials 顺序）
            'baseColor': color3(mi.base_color, (1.0, 1.0, 1.0)) if mi else [1.0, 1.0, 1.0],
            'emission': color3(mi.emission_color, (0.0, 0.0, 0.0)) if mi else [0.0, 0.0, 0.0],
            'blend': int(_f(mi.blend, 0)) if mi else 0,
            'cull': int(_f(mi.cull, 2)) if mi else 2,
            'transparent': matf['transparent'],
            # ↓ A2 修复：这组才是权威渲染判据（旧 `blend` 字段在 Warpforge 的自定义 shader 里恒为 0，
            #   会把 126/350 个真 Alpha 混合材质误判成不透明）。见 _审计报告_0910.md A2
            'srcBlend': matf['srcBlend'],
            'dstBlend': matf['dstBlend'],
            'alphaClip': matf['alphaClip'],
            'forceBlend': matf['forceBlend'],
            'blendAuthoritative': matf['blendAuthoritative'],
            'shader': matf['shader'],
            'props': matf['props'],       # 🆕 2026-09-21：整张属性表（运行时重建材质用）
            'texOpaquePct': matf['texOpaquePct'],
            'texClearPct': matf['texClearPct'],
        })

    # ---------------- 粒子 ----------------
    particles = []
    skipped_ui = 0
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        res = a.ps_of(gopid)
        if not res or res[0] is None:
            continue
        ps, render_mode, _mesh_ref, mats = res
        if not include_all_particles and root_name_of(a, t) in UI_PS_ROOTS:
            skipped_ui += 1
            continue
        gname = go_name_of(a, t)
        p, q, s = world_trs(a, t)

        im = ps.get('InitialModule') or {}
        em = ps.get('EmissionModule') or {}
        shp = ps.get('ShapeModule') or {}

        if 'lengthInSec' not in ps:
            note_default('duration')
        if 'looping' not in ps:
            note_default('looping')
        if 'prewarm' not in ps:
            note_default('prewarm')
        if 'startLifetime' not in im:
            note_default('startLifetime')
        if 'startSpeed' not in im:
            note_default('startSpeed')
        if 'startSize' not in im:
            note_default('startSize')
        if 'startColor' not in im:
            note_default('startColor')
        if 'gravityModifier' not in im:
            note_default('gravityModifier')
        if 'maxNumParticles' not in im:
            note_default('maxParticles')
        if 'rateOverTime' not in em:
            note_default('emissionRate')
        if 'type' not in shp:
            note_default('shapeType')
        if 'radius' not in shp:
            note_default('shapeRadius')
        if 'angle' not in shp:
            note_default('shapeAngle')
        if 'arc' not in shp:
            note_default('shapeArc')
        if 'moveWithTransform' not in ps:
            note_default('simulationSpace')

        tex_name = mats[0].tex_name if mats else None
        tex_file = tex.find(tex_name) if tex_name else None
        if tex_name and not tex_file:
            warn['tex_missing'].append({'go': gname, 'tex': tex_name})
        elif tex_file and not tex.from_primary(tex_file):
            warn['tex_fallback'].append({'go': gname, 'tex': tex_file, 'dir': tex.used[tex_file]})
        if not mats:
            note_default('tex')

        arc_deg = shape_dim(shp.get('arc'), DEF_SHAPE_ARC_DEG)
        psr = psr_of(a, gopid) or {}
        col_mod = ps.get('ColorModule') or {}
        # 🔴 2026-09-22：这五个模块先取出来，**「在不在」要一起写进清单** ——
        #    C# 侧任何 `!= null` 判断都失效（`JsonUtility` 把 `null` 整棵子树物化），
        #    判据只能是清单里的 `hasXxx` 布尔量。详见下面 `hasVelocity` 那一大段。
        vel_m   = mod_fields(ps, 'VelocityModule')
        clamp_m = mod_fields(ps, 'ClampVelocityModule')
        noise_m = mod_fields(ps, 'NoiseModule')
        rot_m   = mod_fields(ps, 'RotationModule')
        sub_m   = sub_emitters(a, mod_fields(ps, 'SubModule'))

        particles.append({
            'go': gname,
            # 🔴 2026-09-21 新增：原版关着的对象不要再建（见 active_in_hierarchy 的说明）
            'active': active_in_hierarchy(a, t),
            'pos': p, 'rot': q, 'scale': s,
            'tex': tex_name,
            'texFile': tex_file,
            'duration': r6(_f(ps.get('lengthInSec'), DEF_DURATION)),
            'looping': bool(ps.get('looping', DEF_LOOPING)),
            'prewarm': bool(ps.get('prewarm', DEF_PREWARM)),
            # 🔴 **2026-09-22 新增：`simulationSpeed`（原来根本没抽）**
            #    原版 13 场里 **19/34 ≠ 1.0** —— 烟囱那个 `Scenario/Particles/SmokeEffect`
            #    是 **0.1**（慢 10 倍）、`Bullets Controller` 是 **4.41**、`Dust Floor` 一族 0.5。
            #    不接就一律按 1.0 播 ⇒ 飘移速度、曲线推进、prewarm 之后的相位全不对。
            #    判据 = 原版 JSON 的 `simulationSpeed`（`arena_particle_audit.py` 可复查）。
            'simulationSpeed': r6(_f(ps.get('simulationSpeed'), 1.0)),
            # 🔴 2026-09-22 新增：`startDelay`（原来是漏的；`Generator glows` 有 0.2）
            'startDelay': mm(ps.get('startDelay')),
            'startLifetime': curve_minmax(im.get('startLifetime'), DEF_LIFETIME),
            'startSpeed': curve_minmax(im.get('startSpeed'), DEF_SPEED),
            'startSize': curve_minmax(im.get('startSize'), DEF_SIZE),
            'startColor': gradient_color(im.get('startColor'), DEF_COLOR),
            # 🔴 **2026-09-22 新增：`startRotation` 一族（原来根本没抽）**
            #    单位是**弧度**（原版 `Steam` 是 `TwoConstants(0, 2π)` = 每个粒子随机朝向，
            #    `Droppods/SmokeEffect (1)` 是 `(−2π, +2π)`；`Portal` 的 X 是 3π/2）。
            #    不接 ⇒ **全部粒子朝向相同** ⇒ 烟/蒸汽渲出来是「一坨」而不是「散开的缕」。
            #    `randomizeRotationDirection` 是 0..1 的浮点（原版 `Steam`/`Dust Floor` 是 0.5）。
            #    ⚠️ `startRotationX/Y/Z` 只在 `rotation3D=true` 时生效（原版 3 个对象开了）。
            'startRotation': mm(im.get('startRotation')),
            'rotation3D': bool(im.get('rotation3D')),
            'startRotationX': mm(im.get('startRotationX')),
            'startRotationY': mm(im.get('startRotationY')),
            # ⚠️ `rotation3D=true` 时 Z 轴存在 `startRotation` 里（和 `RotationModule` 同一个坑）
            'startRotationZ': mm(im.get('startRotation')),
            'randomizeRotationDirection': r6(_f(im.get('randomizeRotationDirection'), 0.0)),
            # 🆕 2026-09-22：`size3D` 的三轴尺寸（原版 `Droppods/Glow` 的 Y 是 3.5）
            'size3D': bool(im.get('size3D')),
            'startSizeY': mm(im.get('startSizeY')),
            'startSizeZ': mm(im.get('startSizeZ')),
            'gravityModifier': curve_single(im.get('gravityModifier'), 0.0),
            # 🆕 2026-09-21：曲线态原样带出去（构建侧按 MinMaxCurve 还原，别拍成一个数）
            'gravityCurve': curve_raw(im.get('gravityModifier')),
            'maxParticles': int(_f(im.get('maxNumParticles'), DEF_MAXPARTICLES)),
            'emissionRate': curve_single(em.get('rateOverTime'), DEF_EMISSION_RATE),
            'emissionRateCurve': curve_raw(em.get('rateOverTime')),
            # 🆕 2026-09-21：两个「我们原来完全没建」的模块 —— 粒子大小曲线 + 透明度生命周期
            'sizeOverLifetime': size_over_lifetime(ps),
            # 🆕 2026-09-21 下半场：**改成完整 RGBA 渐变**（原来只抽了 alpha ⇒ 火焰永远一坨白）
            'colorOverLifetime': (color_keys(col_mod.get('gradient'))
                                  if col_mod.get('enabled') else None),
            # 🆕 2026-09-21 下半场：**VFX 那 5 个模块**（原来一个都没建 —— 烟不飘、火没有星、雾不扭）
            'velocity': velocity_fields(mod_fields(ps, 'VelocityModule')),
            'clampVelocity': clamp_velocity_fields(mod_fields(ps, 'ClampVelocityModule')),
            'noise': noise_fields(mod_fields(ps, 'NoiseModule')),
            'rotationOverLifetime': rotation_fields(mod_fields(ps, 'RotationModule')),
            'subEmitters': sub_m,
            # 🔴🔴 **2026-09-22 新增：必须显式带「这个模块到底在不在」的布尔量。**
            #   起因（一个**静默、而且吃掉了全部 13 场**的坑）：生成器对「原版关着的模块」写的是
            #   `"velocity": null`，而 **`JsonUtility.FromJson` 会把 `null` 整棵子树物化** ——
            #   实测 `velocity != null`、**连 `velocity.x != null` 都是 True**（`ParticleSanity` 的探针钉死的）。
            #   ⇒ C# 那边**任何 null 判断都失效**：`ArenaBuilder` 给 **34/34 颗**粒子都打开了
            #   `velocityOverLifetime` 与 `limitVelocityOverLifetime`，后者的 `limit` 落到兜底值 **1**
            #   ⇒ **把所有粒子速度钳到 1 单位/秒**。症状 = arena1 烟囱那颗「活了 5.7 秒却只离发射体
            #   0.83 单位、|v|≈1.13」，把 `startSpeed` 覆盖成 10 也不动。
            #   铁证 = 建场日志「velocity 34 · clampVelocity 34 · noise 34 · rotation 34」
            #   （原版实测 leviathan 是 **26 / 10 / 6 / 7**，34/34 不可能）。
            #   ⚠️ 同一个坑在 `emissionRateCurve` 上早就踩过一次（判 `!= null` ⇒ 断言假绿）。
            'hasVelocity': vel_m is not None,
            'hasClampVelocity': clamp_m is not None,
            'hasNoise': noise_m is not None,
            'hasRotation': rot_m is not None,
            'hasSubEmitters': bool(sub_m),
            'shapeType': int(_f(shp.get('type'), DEF_SHAPE_TYPE)),
            'shapeRadius': r6(shape_dim(shp.get('radius'), DEF_SHAPE_RADIUS)),
            'shapeAngle': r6(_f(shp.get('angle'), DEF_SHAPE_ANGLE)),
            'shapeArc': r6(arc_deg * PI / 180.0),          # 清单里统一存弧度
            # 🔴 **2026-09-22 新增：ShapeModule 的位置 / 旋转 / 缩放 / 厚度（原来只抽了 type/radius/angle/arc）**
            #    · `m_Rotation` —— 原版 `SmokeEffect/Embers (2)`、`TinyFlames/Embers`、
            #      `SmokeEffect/WildFire` 都是 **`(-90, 0, 0)`** ⇒ **发射方向差 90°**（单位：度）；
            #    · `m_Position` —— `WildFire` 是 `(0, -0.53, 0)`；
            #    · `m_Scale` —— `RisingSteam` z=0.5 · `TinyFlames` (0.8, 0.44, 1.0) · `WildFire` z=0.6
            #      （**默认必须是 (1,1,1)**，不是 0）；
            #    · `radiusThickness` —— `WildFire` 是 **0.0**（= 只从表面发射），其余是 1.0。
            'shapePos': v3(shp.get('m_Position')),
            'shapeRot': v3(shp.get('m_Rotation')),
            'shapeScale': v3(shp.get('m_Scale'), (1.0, 1.0, 1.0)),
            'shapeRadiusThickness': r6(_f(shp.get('radiusThickness'), 1.0)),
            'renderMode': int(_f(render_mode, DEF_RENDER_MODE)),
            'scalingMode': int(_f(ps.get('scalingMode'), 1)),
            'renderAlignment': int(_f(psr.get('m_RenderAlignment'), 0)),
            'sortingFudge': r6(_f(psr.get('m_SortingFudge'), 0.0)),
            'lengthScale': r6(_f(psr.get('m_LengthScale'), 2.0)),
            'maxParticleSize': r6(_f(psr.get('m_MaxParticleSize'), 0.5)),
            'minParticleSize': r6(_f(psr.get('m_MinParticleSize'), 0.0)),
            'matColor': (lambda m: ([r6(_f(m.get('r'), 1.0)), r6(_f(m.get('g'), 1.0)),
                                     r6(_f(m.get('b'), 1.0)), r6(_f(m.get('a'), 1.0))]
                                    if isinstance(m, dict) else None))(
                (mats[0].base_color if mats else None)),
            'simulationSpace': int(_f(ps.get('moveWithTransform'), DEF_SIM_SPACE)),
            'bursts': burst_list(em, warn, gname),
        })
        particles[-1].update(uv_fields(ps))

    manifest = {
        'scene': SCENE,
        'camera': camera,
        'light': light,
        'ambient': ambient,
        'defaultEnv': default_env,
        'postFx': postfx,
        'meshes': meshes,
        'particles': particles,
    }
    return manifest, warn, skipped_ui, tex


# ------------------------------------------------------------------ 拷贝资源
_GROUP_RE = re.compile(r'^g\s+(.*)_(\d+)\s*$')


def split_obj_by_group(text):
    """把一个 OBJ 按 `g <名>_<i>` 组拆成多份（每份都带上**同一段顶点块**）。

    🔴 2026-09-20 实测两轮，结论是：**Unity 的 OBJ 导入器不会切子网格** ——
    ① 原文件只有 `g` 组、没有 `usemtl` ⇒ 导入成 **1** 个子网格；
    ② 补上 `usemtl mat_<i>` 之后**仍然是 1 个**（诊断行照旧报「网格 1 个子网格 / 材质 2 个」）。
    而原版有些网格**真的是多个子网格、每个一个材质**（圣女战场的 `Floor`：第 0 个**透明**、
    第 1 个**不透明**；tauviorla 的 `Energy Bar` 同理）。
    ⇒ 唯一稳的办法是**按组拆成多个 OBJ 文件**，Unity 侧各建一个网格对象、各配各的材质
      （几何上无损：两份共用同一段顶点块与同一个 Transform）。

    返回 `[(后缀, 文本), …]`；**只有一组时返回单元素**（调用方照旧整份拷贝）。
    """
    lines = text.splitlines(keepends=True)
    header, groups, cur = [], [], None
    for line in lines:
        s = line.rstrip('\r\n')
        if s[:2] in ('v ', 'vt', 'vn') or s[:3] in ('v\t', 'vt\t', 'vn\t'):
            header.append(line)
            continue
        if _GROUP_RE.match(s):
            cur = [line]
            groups.append(cur)
            continue
        (cur if cur is not None else header).append(line)
    groups = [g for g in groups if any(x.startswith('f ') for x in g)]   # 丢掉没面的空组
    if len(groups) <= 1:
        return [('', text)]
    head = ''.join(header)
    return [('__sub%d' % i, head + ''.join(g)) for i, g in enumerate(groups)]


def copy_assets(manifest, tex_resolver):
    os.makedirs(OUT_MODELS, exist_ok=True)
    os.makedirs(OUT_TEXTURES, exist_ok=True)
    n_obj = n_tex = 0
    copied = set()
    for e in manifest['meshes']:
        f = e['objFile']
        if f and f not in copied:
            # ⚠️ 来源目录**不能一律用 `OBJ_DIR`** —— 共享包兜底命中的那些在别处（见 obj_resolver）
            src = os.path.join(OBJ_SRC.get(f, OBJ_DIR), f)
            if os.path.isfile(src):
                # ⚠️ 这里**必须原样拷贝，不要做任何手性还原**。
                #
                # 审计报告 A1 说「OBJ 是 X 镜像的，进 Unity 会左右反」—— 数据比对没错
                # （obj 顶点确实 == -x × bundle 顶点），但结论对 Unity 是**反的**：
                # UnityPy 导出时做的是 Unity(左手系) → OBJ(右手系) 的转换，而
                # **Unity 的 OBJ 导入器会再做一次反向转换**，两次恰好抵消。
                # 如果这里先还原一次，Unity 再转一次 → 每个网格内部被镜像（多翻了一次）。
                #
                # 2026-09-10 用 battlearena1 做了 A/B 实拍对比验证：
                #   原样拷贝 → 哥特教堂在左上，与原版参照图一致 ✅
                #   先还原   → 教堂跑到右上，28 个网格全部内部镜像 ❌
                # （教堂是最不对称的物体，所以最容易看出；其余物体只是"看着有点怪"）
                #
                # 🔴 2026-09-20：**唯一的例外是补 `usemtl`** —— 那不改顶点、不改手性，
                #    只是把「Unity 导入时要切几个子网格」这件事告诉导入器（见 `add_usemtl`）。
                #    不补的话多材质网格只有第 0 个材质生效。
                with open(src, 'r', encoding='utf-8', errors='replace') as fh:
                    body = fh.read()
                dst = os.path.join(OUT_MODELS, f)
                subs = e.get('subFiles')
                if subs and len(subs) > 1:
                    # 多子网格：**按组拆成多个 OBJ**（Unity 侧一个子网格一个网格对象）。
                    # 见 `split_obj_by_group` —— Unity 的 OBJ 导入器不切子网格（实测两轮）。
                    for (_, txt), name in zip(split_obj_by_group(body), subs):
                        with open(os.path.join(OUT_MODELS, name), 'w', encoding='utf-8', newline='') as fh:
                            fh.write(txt)
                        copied.add(name)
                        n_obj += 1
                else:
                    shutil.copy2(src, dst)
                    copied.add(f)
                    n_obj += 1
    copied_tex = set()
    for e in manifest['meshes'] + manifest['particles']:
        # 网格要把**每个子网格**的贴图都算进来（粒子没有 subMats ⇒ 用 .get）
        names = [e['texFile']] + [s.get('texFile') for s in (e.get('subMats') or [])]
        for f in names:
            if f and f not in copied_tex:
                src = tex_resolver.path_of(f)
                if src and os.path.isfile(src):
                    shutil.copy2(src, os.path.join(OUT_TEXTURES, f))
                    copied_tex.add(f)
                    n_tex += 1
    return n_obj, n_tex


# ------------------------------------------------------------------ 主流程
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default='battlearena1',
                    help='战场场景名（默认 battlearena1）；可用 --list-arenas 看全 13 个')
    ap.add_argument('--list-arenas', action='store_true', help='列出 13 个战场场景名后退出')
    ap.add_argument('--no-copy', action='store_true', help='只写清单, 不拷贝模型/贴图')
    ap.add_argument('--all-particles', action='store_true', help='连 2D UI 粒子一起输出')
    ap.add_argument('--all', action='store_true',
                    help='13 场全跑一遍 —— 🔴 2026-09-20 加：同一进程内 bundle 缓存跨场复用，'
                         '比逐场起进程快一个数量级（实测单场 ~6 分钟，绝大部分花在重复加载共享包）')
    args = ap.parse_args()

    if args.list_arenas:
        for a in ARENAS:
            print(a)
        return 0
    targets = ARENAS if args.all else [args.arena]
    if not args.all and args.arena not in ARENAS:
        print('未知战场 %r；可选：%s' % (args.arena, ', '.join(ARENAS)))
        return 2
    rc = 0
    for name in targets:
        rc = run_one(name, args) or rc
    return rc


def run_one(arena, args):
    """跑单个战场（2026-09-20 从 `main` 抽出来，好让 `--all` 在同一进程里循环 —— 见 `--all` 的说明）。"""
    set_arena(arena)

    a = Assembler(SCENE, 'd:/tmp/wf_manifest_tmp')
    manifest, warn, skipped_ui, tex = build_manifest(a, args.all_particles)

    os.makedirs(ARENA_DIR, exist_ok=True)
    with open(OUT_JSON, 'w', encoding='utf-8') as fp:
        json.dump(manifest, fp, ensure_ascii=False, indent=2)
        fp.write('\n')

    n_obj = n_tex = 0
    if not args.no_copy:
        n_obj, n_tex = copy_assets(manifest, tex)

    # ---------------- 控制台报告 ----------------
    li = ['']
    li.append('=' * 78)
    li.append('%s → %s' % (SCENE, OUT_JSON))
    li.append('  meshes    : %d' % len(manifest['meshes']))
    li.append('  particles : %d%s' % (len(manifest['particles']),
                                      ' (另有 %d 条 2D UI 粒子已排除)' % skipped_ui if skipped_ui else ''))
    if manifest['camera']:
        li.append('  camera    : %s fov=%.3f' % (manifest['camera']['name'], manifest['camera']['fov']))
    if manifest['light']:
        L = manifest['light']
        li.append('  light     : %s 色=(%.4f,%.4f,%.4f) intensity=%.2f shadows=%d 强度=%.3f pos=(%.3f,%.3f,%.3f)' % (
            L['name'], L['color'][0], L['color'][1], L['color'][2], L['intensity'],
            L.get('shadowType', 2), L.get('shadowStrength', 1.0), L['pos'][0], L['pos'][1], L['pos'][2]))
    if manifest['ambient']:
        A = manifest['ambient']
        li.append('  ambient   : mode=%d(3=Flat) sky=(%.4f,%.4f,%.4f) 强度=%.4f fog=%s' % (
            A.get('mode', 3), A['sky'][0], A['sky'][1], A['sky'][2], A['intensity'], A.get('fog')))
    if manifest.get('defaultEnv'):
        E = manifest['defaultEnv']
        li.append('  运行时环境光(SO): %s  ambientColor=(%.4f,%.4f,%.4f,%.4f) blend=%.3f' % (
            E['so'], E['ambientColor'][0], E['ambientColor'][1], E['ambientColor'][2],
            E['ambientColor'][3], E['ambientBlend']))
        li.append('      （来自 %s PathID %d；**这个才是运行时 RenderSettings.ambientLight**）' % (
            E['hostBundle'], E['pathId']))
    if warn['default_env']:
        li.append('  🔴 [默认环境条件读不到] %s' % ' / '.join(warn['default_env']))
    if not args.no_copy:
        li.append('  拷贝      : Models/ %d 个 .obj, Textures/ %d 张 .png' % (n_obj, n_tex))
    if warn['obj_missing']:
        li.append('  [obj 对不上] %d 条:' % len(warn['obj_missing']))
        for m in warn['obj_missing']:
            li.append('      %s -> %s.obj (不存在)' % (m['go'], m['obj']))
    if warn['tex_fallback']:
        dirs = {}
        for m in warn['tex_fallback']:
            dirs.setdefault(m['dir'], set()).add(m['tex'])
        li.append('  [贴图来自兜底目录] %d 条:' % len(warn['tex_fallback']))
        for d, names in dirs.items():
            li.append('      %s : %s' % (d, ', '.join(sorted(names))))
    if warn['tex_missing']:
        li.append('  [贴图完全找不到] %d 条:' % len(warn['tex_missing']))
        for m in warn['tex_missing']:
            li.append('      %s -> %s.png' % (m['go'], m['tex']))
    if warn['ps_defaults']:
        li.append('  [粒子字段缺失→默认值]: %s' % json.dumps(warn['ps_defaults'], ensure_ascii=False))
    n_ps_burst = sum(1 for e in manifest['particles'] if e['bursts'])
    n_burst = sum(len(e['bursts']) for e in manifest['particles'])
    li.append('  bursts    : %d 条粒子带 burst, 共 %d 个 burst' % (n_ps_burst, n_burst))
    uv_on = [e for e in manifest['particles'] if e['uvEnabled']]
    gd = {}
    for e in uv_on:
        gd[(e['tilesX'], e['tilesY'])] = gd.get((e['tilesX'], e['tilesY']), 0) + 1
    li.append('  flipbook  : %d/%d 条 uvEnabled=true, 网格分布 %s' % (
        len(uv_on), len(manifest['particles']),
        ', '.join('%dx%d×%d' % (k[0], k[1], v) for k, v in sorted(gd.items(), key=lambda x: -x[1])) or '-'))
    if warn['burst_count_missing']:
        li.append('  [burst count 取不到→1.0] %d 条:' % len(warn['burst_count_missing']))
        for m in warn['burst_count_missing']:
            li.append('      %s @ t=%s' % (m['go'], m['time']))
    li.append('=' * 78)
    print('\n'.join(li))
    return 0


if __name__ == '__main__':
    sys.exit(main())
