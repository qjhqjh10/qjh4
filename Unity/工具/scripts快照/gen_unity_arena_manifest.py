#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
gen_unity_arena_manifest.py — battlearena1 战场 → Unity Editor 清单 JSON

读取 07_场景/battlearena1/ 的原始 Unity 序列化 JSON 转储, 生成机器可读清单, 并把
清单实际引用到的模型/贴图拷进 Unity 项目。

产出:
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/arena1_manifest.json
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/Models/      (28 个 .obj)
  d:/4/Unity/MyGame/Assets/WarpforgeArena1/Textures/    (清单引用到的 .png)

坐标: 一律 Unity 原始世界变换 (pos/rot/scale = world chain 结算), 不做任何手性/镜像转换。

用法:
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --no-copy
  D:/2/Warpforge_tools/py312/python.exe gen_unity_arena_manifest.py --all-particles
"""
import argparse
import json
import math
import os
import shutil
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from unity_scene_to_godot import Assembler, q_mul, q_rot_vec   # noqa: E402

# ------------------------------------------------------------------ 路径常量
SCENE            = 'battlearena1'
SCENE_ROOT       = 'd:/2/解包整理/07_场景/'
OBJ_DIR          = 'd:/2/解包整理/06_模型/scenes_scenes_battlearena1'
SCENE_TEX_DIR    = 'd:/2/解包整理/07_场景/battlearena1/Texture2D'
# 贴图兜底搜索目录 (场景自己的 Texture2D 里没有时按顺序往下找; 置空 = 严格只认场景目录,
# 找不到就写 null)。粒子特效贴图全部来自共享 bundle, 不在场景目录里。
FALLBACK_TEX_DIRS = [
    'd:/2/解包整理/08_预制体特效/共享资源/Texture2D',
    'd:/2/解包整理/03_界面UI/去重资源/Texture2D',
]

UNITY_ROOT   = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1'
OUT_JSON     = UNITY_ROOT + '/arena1_manifest.json'
OUT_MODELS   = UNITY_ROOT + '/Models'
OUT_TEXTURES = UNITY_ROOT + '/Textures'

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

    minMaxState: 0=Constant(scalar) 1=TwoConstants(minScalar/scalar)
                 2=Curve(maxCurve)  3=TwoCurves(maxCurve/minCurve)
    曲线态里 scalar/minScalar 是曲线乘数 (curveMultiplier)。
    """
    if not isinstance(v, dict):
        x = _f(v, default)
        return [r6(x), r6(x)]
    st = int(_f(v.get('minMaxState'), 0))
    scalar = _f(v.get('scalar'), default)
    mins = _f(v.get('minScalar'), default)
    if st == 0:
        return [r6(scalar), r6(scalar)]
    if st == 1:
        return [r6(mins), r6(scalar)]
    vals = []
    vals += [scalar * x for x in _curve_keys(v.get('maxCurve'))]
    vals += [mins * x for x in _curve_keys(v.get('minCurve'))]
    if vals:
        return [r6(min(vals)), r6(max(vals))]
    return [r6(mins), r6(scalar)]        # 曲线为空 → 退回常量域


def curve_single(v, default=0.0):
    """MinMaxCurve → 单值 (gravityModifier / rateOverTime 这类清单里只放一个数)"""
    if not isinstance(v, dict):
        return r6(_f(v, default))
    st = int(_f(v.get('minMaxState'), 0))
    scalar = _f(v.get('scalar'), default)
    if st in (0, 1):
        return r6(scalar)
    vals = [scalar * x for x in _curve_keys(v.get('maxCurve'))]
    if vals:
        return r6(max(vals))
    return r6(scalar)


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
        """返回真实存在的文件名 (原名), 找不到 None"""
        if not name:
            return None
        want = (name + '.png').lower()
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

    force_blend = (not transparent and not alpha_clip
                   and cl_pct is not None and cl_pct > 15.0)

    return {
        'srcBlend': int(sb) if sb is not None else None,
        'dstBlend': int(db) if db is not None else None,
        'alphaClip': bool(alpha_clip),
        'forceBlend': bool(force_blend),
        'transparent': bool(transparent),
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
    if not os.path.isdir(OBJ_DIR):
        return None, {}
    idx = {}
    for n in os.listdir(OBJ_DIR):
        if n.lower().endswith('.obj'):
            idx[n.lower()] = n
    return idx, OBJ_DIR


# ------------------------------------------------------------------ 清单构建
def build_manifest(a, include_all_particles=False):
    warn = {'obj_missing': [], 'tex_missing': [], 'tex_fallback': [], 'ps_defaults': {},
            'burst_count_missing': []}

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
    light = None
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        for c in a.go_comps.get(gopid, []):
            if c not in a.LIG:
                continue
            d = a.LIG[c]
            nm = go_name_of(a, t)
            if light is not None and nm != LIGHT_NAME:
                continue
            p, q, _ = world_trs(a, t)
            light = {
                'name': nm, 'pos': p, 'rot': q,
                'color': color3(d.get('m_Color'), (1.0, 1.0, 1.0)),
                'intensity': r6(d.get('m_Intensity', 1.0)),
            }
            if nm == LIGHT_NAME:
                break
        if light and light['name'] == LIGHT_NAME:
            break

    # ---------------- 环境光 (RenderSettings) ----------------
    ambient = {'sky': [1.0, 1.0, 1.0], 'ground': [0.59, 0.59, 1.0], 'intensity': 0.41}
    for r in a.REN.values():
        ambient = {
            'sky': color3(r.get('m_AmbientSkyColor'), (1.0, 1.0, 1.0)),
            'ground': color3(r.get('m_AmbientGroundColor'), (0.59, 0.59, 1.0)),
            'intensity': r6(r.get('m_AmbientIntensity', 0.41)),
        }
        break

    # ---------------- 网格 ----------------
    meshes = []
    for t in sorted(a.TF):
        gopid = a.TF[t].get('m_GameObject', {}).get('m_PathID')
        mesh_name, _mesh_obj = a.mesh_of(gopid)
        if not mesh_name:
            continue
        gname = go_name_of(a, t)
        p, q, s = world_trs(a, t)

        obj_file = obj_index.get((mesh_name + '.obj').lower())
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

        meshes.append({
            'go': gname,
            'obj': mesh_name,
            'objFile': obj_file,
            'pos': p, 'rot': q, 'scale': s,
            'tex': tex_name,
            'texFile': tex_file,
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

        particles.append({
            'go': gname,
            'pos': p, 'rot': q, 'scale': s,
            'tex': tex_name,
            'texFile': tex_file,
            'duration': r6(_f(ps.get('lengthInSec'), DEF_DURATION)),
            'looping': bool(ps.get('looping', DEF_LOOPING)),
            'prewarm': bool(ps.get('prewarm', DEF_PREWARM)),
            'startLifetime': curve_minmax(im.get('startLifetime'), DEF_LIFETIME),
            'startSpeed': curve_minmax(im.get('startSpeed'), DEF_SPEED),
            'startSize': curve_minmax(im.get('startSize'), DEF_SIZE),
            'startColor': gradient_color(im.get('startColor'), DEF_COLOR),
            'gravityModifier': curve_single(im.get('gravityModifier'), 0.0),
            'maxParticles': int(_f(im.get('maxNumParticles'), DEF_MAXPARTICLES)),
            'emissionRate': curve_single(em.get('rateOverTime'), DEF_EMISSION_RATE),
            'shapeType': int(_f(shp.get('type'), DEF_SHAPE_TYPE)),
            'shapeRadius': r6(shape_dim(shp.get('radius'), DEF_SHAPE_RADIUS)),
            'shapeAngle': r6(_f(shp.get('angle'), DEF_SHAPE_ANGLE)),
            'shapeArc': r6(arc_deg * PI / 180.0),          # 清单里统一存弧度
            'renderMode': int(_f(render_mode, DEF_RENDER_MODE)),
            'simulationSpace': int(_f(ps.get('moveWithTransform'), DEF_SIM_SPACE)),
            'bursts': burst_list(em, warn, gname),
        })
        particles[-1].update(uv_fields(ps))

    manifest = {
        'scene': SCENE,
        'camera': camera,
        'light': light,
        'ambient': ambient,
        'meshes': meshes,
        'particles': particles,
    }
    return manifest, warn, skipped_ui, tex


# ------------------------------------------------------------------ 拷贝资源
def copy_assets(manifest, tex_resolver):
    os.makedirs(OUT_MODELS, exist_ok=True)
    os.makedirs(OUT_TEXTURES, exist_ok=True)
    n_obj = n_tex = 0
    copied = set()
    for e in manifest['meshes']:
        f = e['objFile']
        if f and f not in copied:
            src = os.path.join(OBJ_DIR, f)
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
                shutil.copy2(src, os.path.join(OUT_MODELS, f))
                copied.add(f)
                n_obj += 1
    copied_tex = set()
    for e in manifest['meshes'] + manifest['particles']:
        f = e['texFile']
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
    ap.add_argument('--no-copy', action='store_true', help='只写清单, 不拷贝模型/贴图')
    ap.add_argument('--all-particles', action='store_true', help='连 2D UI 粒子一起输出')
    args = ap.parse_args()

    a = Assembler(SCENE, 'd:/tmp/wf_manifest_tmp')
    manifest, warn, skipped_ui, tex = build_manifest(a, args.all_particles)

    os.makedirs(UNITY_ROOT, exist_ok=True)
    with open(OUT_JSON, 'w', encoding='utf-8') as fp:
        json.dump(manifest, fp, ensure_ascii=False, indent=2)
        fp.write('\n')

    n_obj = n_tex = 0
    if not args.no_copy:
        n_obj, n_tex = copy_assets(manifest, tex)

    # ---------------- 控制台报告 ----------------
    li = ['']
    li.append('=' * 78)
    li.append('battlearena1 → %s' % OUT_JSON)
    li.append('  meshes    : %d' % len(manifest['meshes']))
    li.append('  particles : %d%s' % (len(manifest['particles']),
                                      ' (另有 %d 条 2D UI 粒子已排除)' % skipped_ui if skipped_ui else ''))
    if manifest['camera']:
        li.append('  camera    : %s fov=%.3f' % (manifest['camera']['name'], manifest['camera']['fov']))
    if manifest['light']:
        li.append('  light     : %s intensity=%.2f' % (manifest['light']['name'], manifest['light']['intensity']))
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
