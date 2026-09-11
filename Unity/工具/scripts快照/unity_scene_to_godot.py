#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
unity_scene_to_godot.py — 阵营战场 Unity 场景 → Godot 场景脚本 (完全复刻路线)

读 07_场景/<arena>/ 的 GameObject/Transform/MeshFilter/MeshRenderer/SkinnedMeshRenderer/
Material/ParticleSystem/ParticleSystemRenderer/Camera/Light/RenderSettings JSON,
按说明书链路组装出 Godot 场景 (原版层级 + 局部变换 + 材质链 + 粒子):

  - 层级: Transform m_Father/m_Children 原版嵌套, m_LocalPosition/m_LocalRotation/m_LocalScale
          (解包 OBJ 为原始 Unity Y-up 顶点, 与场景局部变换同轴; 输出用 node.quaternion 直赋,
          避免 tscn 矩阵序列化的轴序歧义; **OBJ 的 vt v 勿手动翻转——Godot 导入器自动 1-v**)
  - 网格: MeshFilter.m_Mesh {m_FileID, m_PathID} → Mesh 名 → OBJ
          (fid=3 场景 bundle; fid=N 按场景 externals CAB→bundle 解析)
  - 材质: MeshRenderer.m_Materials → Material JSON/typetree →
          m_SavedProperties.m_TexEnvs(_BaseMap/_MainTex) → 贴图名 → PNG
          _Blend 0=alpha(透明看 _SURFACE_TYPE_TRANSPARENT) 1=LUT(跳过) 2=ADD
          _Cull 2=back(默认) 0=both 1=front; _ZWrite; _BaseColor/_EmissionColor; _UVSpeed
  - 粒子: InitialModule/EmissionModule/ShapeModule/SizeModule/ColorModule/VelocityModule/
          RotationModule → GPUParticles3D (billboard/mesh, 贴图混合, 大小/颜色曲线)
  - 相机/灯: Camera fov/near/far + 相机 Y+180 (Unity +Z 朝前 / Godot -Z);
            DirectionalLight 同 Y+180 (方向 = -R·Z)
  - 环境: RenderSettings ambient(SkyColor/Intensity)/fog(color/density)/背景色

输出 (--out 为 Godot 项目目录):
  assets/meshes/<Mesh名>.obj / assets/textures/<贴图名>.png   缺失才导出
  scenes/unity_arena_<arena>.gd + <arena>.tscn               自动生成场景 (根 x-100)

用法:
  py312/python.exe unity_scene_to_godot.py --arena battlearenablacklegion
  py312/python.exe unity_scene_to_godot.py --arena battlearena2 --out d:/2/战场演示
  (--roots 留空 = 按子树含量自动识别 3D 根: 各变体根名不统一, 名字过滤会漏 battlearena2/leviathan)
多变体要点 (2026-08-22):
  - 资源同名异内容: 'Combined Mesh (root: scene)'/Flame03/Atlas 等跨变体常见 → 内容哈希去重, 不同则 <名>__<arena> 后缀
  - 环境逐变体按说明书直读: 环境光=RenderSettings m_AmbientProbe SH DC (不是 Trilight 平均!), 灯=m_Color×m_Intensity 直通
  - 相机 m_Enabled=0 (reflection camera) → enabled=false; 背景色取 3D 相机 m_BackGroundColor
"""
import argparse
import glob
import json
import math
import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gltf_export import export_morph_gltf

SCENE_ROOT = 'd:/2/解包整理/07_场景/'
BUNDLE_DIR = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64/'
DATA_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', 'data')
CAB_MAP_CACHE = os.path.join(DATA_DIR, 'cab_bundle_map.json')
PID_RE = re.compile(r'^(.+?)_(\d+)\.json$')
# 外部资源固定回退搜查包 (粒子材质/贴图/网格主要在共享包)
SHARED_PACKS = ['battlesharedresources_assets_all.bundle',
                'duplicateassetisolation_assets_all.bundle',
                'battleprefabs_vfxandmisc_assets_all.bundle',
                'atlasindividual_assets_battleatlasui.bundle',
                'boosterpacks_assets_all.bundle']
# Trilight 三色/暖色校正因子已废弃 (2026-08-22 二十轮删除 — 19 轮 LUT 纠错后按说明书直通:
# 环境光=SH probe DC 直读 / 灯=m_Color×m_Intensity 直通, 不再乘任何自创颜色因子)

# ---------------------------------------------------------------- JSON 读取
def load_pid_dir(d):
    out = {}
    if not os.path.isdir(d):
        return out
    for fn in os.listdir(d):
        m = PID_RE.match(fn)
        if not m or not fn.endswith('.json'):
            continue
        try:
            out[int(m.group(2))] = json.load(open(os.path.join(d, fn), encoding='utf-8'))
        except Exception:
            continue
    return out


def load_go_dir(d):
    known, plain = {}, {}
    if not os.path.isdir(d):
        return known, plain
    for fn in os.listdir(d):
        if not fn.endswith('.json'):
            continue
        m = PID_RE.match(fn)
        try:
            data = json.load(open(os.path.join(d, fn), encoding='utf-8'))
        except Exception:
            continue
        if m:
            known[int(m.group(2))] = data
        else:
            plain.setdefault(data.get('m_Name', ''), []).append(data)
    return known, plain


# ---------------------------------------------------------------- 数学
def q_mul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (
        aw * bx + ax * bw + ay * bz - az * by,
        aw * by - ax * bz + ay * bw + az * bx,
        aw * bz + ax * by - ay * bx + az * bw,
        aw * bw - ax * bx - ay * by - az * bz,
    )


def q_rot_vec(q, v):
    """四元数 (x,y,z,w) 旋转向量 v"""
    x, y, z, w = q
    vx, vy, vz = v
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (vx + w * tx + y * tz - z * ty,
            vy + w * ty + z * tx - x * tz,
            vz + w * tz + x * ty - y * tx)


def reflect_z_q(q):
    """Z 反射共轭: R' = M·R·M (M=diag(1,1,-1)) → 四元数 (x,y,z,w)→(-x,-y,z,w).
    ⚠️ 备用理论, 未被验证 (2026-08-23 多探针: 正确转换=不做世界反射, 相机 Y180 直挂 —
    "只转相机=镜像"为 16 轮错误论断; 见 reflect_x_q 注释)。"""
    return (-q[0], -q[1], q[2], q[3])


def reflect_x_q(q):
    """X 反射共轭: M=diag(-1,1,1), R' = M·R·M → 四元数 (x,y,z,w)→(x,-y,-z,w).
    ⚠️ 已证伪 (2026-08-23): 16 轮曾断言"Unity 左手系→Godot 必须整世界 X 反射否则水平镜像",
    多探针 (贴图 u→屏幕 x 映射) 实证相反 — MirrorX(内容 X 镜像+相机 conjX⊗Y180) = 屏幕镜像 ✗;
    默认 no-mirror (内容直挂 Unity 世界坐标+相机仅 Y180) = 正像 ✓ (参考图太阳/王座/齿轮/横幅全对齐)。
    此函数仅 MirrorX 试验分支 (--mirror) 使用, 勿再据此改动默认路径。"""
    return (q[0], -q[1], -q[2], q[3])


def q_light_godot(q):
    """Unity 方向光四元数 → Godot 灯光四元数 (坑85, 2026-08-25; 全部 (x,y,z,w) 序)
    ⚠️ 2026-08-25 已实测证伪并停用 (见 light 分支注释): 本函数结果为背光方向 (全场 35.0 lum vs
    已校准 97.5/95.5=原版 RT) — 保留仅作历史记录, 勿接入。
    Unity 光方向 = q·(0,-1,0) (默认垂直向下); Z 反射世界 → (dx,dy,-dz);
    Godot: 灯照射 = B·(0,0,-1) → B = rot_from_to((0,0,-1), 目标) 同 glTF 分支"""
    d = q_rot_vec(q, (0.0, -1.0, 0.0))
    g = (d[0], d[1], -d[2])
    n = math.sqrt(g[0] * g[0] + g[1] * g[1] + g[2] * g[2])
    if n == 0.0:
        return (0.0, 0.0, 0.0, 1.0)
    g = (g[0] / n, g[1] / n, g[2] / n)
    return rot_from_to((0.0, 0.0, -1.0), g)


def curve_min_max(v, default=1.0):
    if isinstance(v, dict):
        if v.get('minMaxState', 0) == 0:
            return v.get('scalar', default), v.get('scalar', default)
        return v.get('minScalar', default), v.get('maxScalar', v.get('scalar', default))
    return v, v


def curve_scalar(v, default=0.0):
    if isinstance(v, dict):
        return v.get('scalar', default)
    return v


def curve_keys(v):
    if not isinstance(v, dict):
        return []
    mc = v.get('maxCurve', {}) or {}
    return [(k.get('time', 0.0), k.get('value', 1.0))
            for k in (mc.get('m_Curve', []) or []) if isinstance(k, dict)]


def grad_keys(v):
    """Unity Gradient → 8 点梯度: color keys 按 ctime, alpha keys 按 atime 独立插值
    (旧实现按 key 序号均匀分布, 会把 a=1 的早淡出段错位到中段 → 烟半生即透明)"""
    if not isinstance(v, dict):
        return []
    g = v.get('maxGradient', {}) or {}
    ncol = int(g.get('m_NumColorKeys', 2) or 2)
    nalpha = int(g.get('m_NumAlphaKeys', 2) or 2)

    def pairs(kind, n, key):
        out = []
        for i in range(n):
            k = g.get('key%d' % i)
            if not isinstance(k, dict):
                continue
            t = (g.get('%s%d' % (kind, i), 0) or 0) / 65535.0
            out.append((t, key(k)))
        return out

    cols = pairs('ctime', ncol, lambda k: (float(k.get('r', 0.0)), float(k.get('g', 0.0)), float(k.get('b', 0.0))))
    alphas = pairs('atime', nalpha, lambda k: float(k.get('a', 0.0)))

    def interp(ps, t):
        if not ps:
            return None
        if t <= ps[0][0]:
            return ps[0][1]
        if t >= ps[-1][0]:
            return ps[-1][1]
        for i in range(len(ps) - 1):
            t0, v0 = ps[i]
            t1, v1 = ps[i + 1]
            if t0 <= t <= t1:
                f = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
                if isinstance(v0, tuple):
                    return tuple(a + (b - a) * f for a, b in zip(v0, v1))
                return v0 + (v1 - v0) * f
        return ps[-1][1]

    # 联合时间轴 (color 键+alpha 键的并集): 每个时间点 color/alpha 各自插值 —
    # 比 8 点均匀重采样精确 (原版键位如 Light Shaft 呼吸脉动 α .2265/.447/.659 会保留)
    ts = sorted(set([t for t, _ in cols] + [t for t, _ in alphas]))
    out = []
    for t in ts:
        c = interp(cols, t) or (1.0, 1.0, 1.0)
        a = float(interp(alphas, t) if alphas else 1.0)
        out.append((t, (c[0], c[1], c[2], a)))
    return out


def col_str(c):
    return 'Color(%.4f, %.4f, %.4f, %.4f)' % (
        c.get('r', 1.0), c.get('g', 1.0), c.get('b', 1.0), c.get('a', 1.0))


def col_str_hdr(c):
    """粒子材质 HDR 色 (原版 m_Color 可 >1 如 Embers 2.828) → Godot 下 glow 爆白,
    引擎等效校准: >1 分量 clamp 到 1.0 (保留色相/alpha, 观感=原版微弱光斑)"""
    return 'Color(%.4f, %.4f, %.4f, %.4f)' % (
        min(1.0, c.get('r', 1.0)), min(1.0, c.get('g', 1.0)),
        min(1.0, c.get('b', 1.0)), c.get('a', 1.0))


def safe_name(nm):
    s = re.sub(r'[^0-9A-Za-z_]', '_', nm).strip('_') or 'Node'
    if s[0].isdigit():
        s = '_' + s
    return s


# ---------------------------------------------------------------- Bundle 解析


def _saved_props_dict(sp):
    """UnityPy typed m_SavedProperties → {'m_TexEnvs': [[slot, {m_Texture {...}, ...}], ...],
    'm_Floats': [[k, v], ...], 'm_Colors': [[k, {...}], ...]} (和本地 JSON 同构)"""
    out = {'m_TexEnvs': [], 'm_Floats': [], 'm_Colors': []}
    for sect, key in [('m_TexEnvs', 'm_Texture'), ('m_Floats', None), ('m_Colors', None)]:
        items = getattr(sp, sect, None) or []
        for it in items:
            try:
                name = str(it[0])
            except Exception:
                continue
            val = it[1]
            if sect == 'm_TexEnvs':
                ent = {'m_Texture': {}, 'm_Scale': {'x': 1, 'y': 1}, 'm_Offset': {'x': 0, 'y': 0}}
                try:
                    t = val.m_Texture
                    ent['m_Texture'] = {'m_FileID': getattr(t, 'm_FileID', 0), 'm_PathID': getattr(t, 'm_PathID', 0)}
                    sc = val.m_Scale
                    ent['m_Scale'] = {'x': getattr(sc, 'x', 1.0), 'y': getattr(sc, 'y', 1.0)}
                except Exception:
                    pass
                out['m_TexEnvs'].append([name, ent])
            elif sect == 'm_Floats':
                out['m_Floats'].append([name, float(val) if not isinstance(val, tuple) else float(val[0])])
            else:
                try:
                    c = val
                    out['m_Colors'].append([name, {'r': float(getattr(c, 'r', 1.0)),
                                                   'g': float(getattr(c, 'g', 1.0)),
                                                   'b': float(getattr(c, 'b', 1.0)),
                                                   'a': float(getattr(c, 'a', 1.0))}])
                except Exception:
                    pass
    return out

class BundleResolver:
    """场景 bundle + externals(CAB→bundle) 引用解析; 索引 Mesh/Texture2D/Material"""

    def __init__(self, arena):
        self.arena = arena
        self.path = os.path.join(BUNDLE_DIR, 'scenes_scenes_%s.bundle' % arena)
        if not os.path.exists(self.path):
            raise SystemExit('[错误] 场景 bundle 不存在: %s' % self.path)
        import UnityPy
        self.UnityPy = UnityPy
        self.env = UnityPy.load(self.path)
        self.local = {}
        self._index_env(self.env, self.local)
        sf = self.env.objects[0].assets_file
        self.ext = {}
        for i, e in enumerate(sf.externals):
            m = re.search(r'CAB-[0-9a-f]+', str(e))
            if m:
                self.ext[i + 1] = m.group(0)
        self.cab_map = self._cab_map()
        self.ext_index = {}

    def _index_env(self, env, target):
        for o in env.objects:
            t = o.type.name
            if t in ('Mesh', 'Texture2D', 'Material'):
                target.setdefault(t, {})[o.path_id] = o

    def obj_name(self, o):
        """对象名: dict(本地MAT JSON) 或 UnityPy ObjectReader (read_typetree 元数据, 不解码数据)"""
        if o is None:
            return ''
        if isinstance(o, dict):
            return str(o.get('m_Name', ''))
        try:
            return str(o.read_typetree().get('m_Name', ''))
        except Exception:
            try:
                return str(o.read().m_Name)
            except Exception:
                return ''

    def _cab_map(self):
        if os.path.exists(CAB_MAP_CACHE):
            try:
                return json.load(open(CAB_MAP_CACHE, encoding='utf-8'))
            except Exception:
                pass
        m = {}
        for fn in os.listdir(BUNDLE_DIR):
            if not fn.endswith('.bundle'):
                continue
            with open(os.path.join(BUNDLE_DIR, fn), 'rb') as f:
                head = f.read(1 << 20)
            if not head.startswith(b'UnityFS'):
                continue
            for c in re.findall(rb'CAB-[0-9a-f]{32}', head):
                m.setdefault(c.decode(), []).append(fn)
        os.makedirs(DATA_DIR, exist_ok=True)
        with open(CAB_MAP_CACHE, 'w', encoding='utf-8') as f:
            json.dump(m, f, ensure_ascii=False, indent=1)
        return m

    def read_obj(self, ref, want=None):
        fid = ref.get('m_FileID', 0)
        pid = ref.get('m_PathID')
        if pid in (None, 0):
            return None
        # 本地优先: 各变体 dripped JSON 的 fid 语义不一 (0/3=本地; 也有 2/5/6=本地),
        # 本地能命中就直接返回 (darkangels fid2 pid34='Background space' 等); want=期望类型防跨类型撞号
        if want:
            d = self.local.get(want)
            if d and pid in d:
                return d[pid]
        else:
            for d in self.local.values():
                if pid in d:
                    return d[pid]
        if fid in (0, 3):
            found = self._find_ext(pid, want)
            if found is not None:
                return found
        # 外部引用: 先按 externals CAB→bundle 候选扫, 再扫固定共享包 (头部扫描映射不可靠)
        # (fid 0/3 也会走到这里 — dripped JSON 的 fid 语义不一, 贴图常在 battlesharedresources)
        cab = self.ext.get(fid)
        bundles = list(self.cab_map.get(cab, [])) if cab else []
        for bname in SHARED_PACKS:
            if bname not in bundles:
                bundles.append(bname)
        found = self._scan_bundles(pid, bundles, want)
        if found is not None:
            return found
        return self._find_ext(pid, want)

    def _scan_bundles(self, pid, bundles, want=None):
        for bname in bundles:
            if bname not in self.ext_index:
                try:
                    env = self.UnityPy.load(os.path.join(BUNDLE_DIR, bname))
                    d = {}
                    self._index_env(env, d)
                    self.ext_index[bname] = d
                except Exception:
                    self.ext_index[bname] = None
            idx = self.ext_index.get(bname)
            if not idx:
                continue
            if want:
                d = idx.get(want)
                if d and pid in d:
                    return d[pid]
            else:
                for d in idx.values():
                    if pid in d:
                        return d[pid]
        return None

    def _find_ext(self, pid, want=None):
        for idx in self.ext_index.values():
            if not idx:
                continue
            if want:
                d = idx.get(want)
                if d and pid in d:
                    return d[pid]
            else:
                for d in idx.values():
                    if pid in d:
                        return d[pid]
        return None

    def mat_data(self, ref):
        """external Material → dict (UnityPy typed 对象直接读 m_SavedProperties)"""
        obj = self.read_obj(ref, 'Material')
        if obj is None:
            return None
        if isinstance(obj, dict):
            return obj
        # typed Material 对象 → read(): m_SavedProperties 转 dict
        try:
            obj = obj.read()
            sp = getattr(obj, 'm_SavedProperties', None)
            if sp is None:
                return None
            d = {'m_Name': getattr(obj, 'm_Name', '') or 'ext_mat',
                 'm_ValidKeywords': list(getattr(obj, 'm_ValidKeywords', []) or []),
                 'm_SavedProperties': _saved_props_dict(sp)}
            return d
        except Exception:
            return None


# ---------------------------------------------------------------- 材质
class MatInfo:
    def __init__(self, name, tex_ref, blend, keywords, cull, zwrite,
                 base_color, emission_color, uv_speed, mat_color=None,
                 tex_ref2=None, vector4_1=None, cutoff=None, smoothness=None,
                 metallic=None, surface=None, src_blend=None, dst_blend=None,
                 alpha_clip=None, tex_scale=None, tex_offset=None,
                 queue_offset=None, emission_tex_ref=None):
        self.name = name
        self.tex_ref = tex_ref
        self.tex_name = None
        self.tex_obj = None
        self.tex_ref2 = tex_ref2       # _SecondaryTex (UV Scroll Runes 第二贴图)
        self.tex_name2 = None
        self.tex_obj2 = None
        self.blend = blend
        self.keywords = keywords or []
        self.cull = cull
        self.zwrite = zwrite
        self.base_color = base_color
        self.emission_color = emission_color
        self.mat_color = mat_color
        self.uv_speed = uv_speed
        self.vector4_1 = vector4_1     # Runes 滚动参数 (r,g,b=tex2平铺, 滚动速率) 疑似 (1,1,0.02,0)
        # 2026-08-26 B 映射表: 全参数字段 (原值; 缺失=None → emit 侧用 Godot 默认/Unity 默认)
        self.cutoff = cutoff           # _Cutoff (URP) / _ClipThreshold (Everguild)
        self.smoothness = smoothness   # _Smoothness → roughness=1-它
        self.metallic = metallic       # _Metallic (21 个非0 金属)
        self.surface = surface         # _Surface 0=Opaque 1=Transparent (B3 四元组主判据)
        self.src_blend = src_blend     # _SrcBlend
        self.dst_blend = dst_blend     # _DstBlend
        self.alpha_clip = alpha_clip   # _AlphaClip
        self.tex_scale = tex_scale     # _BaseMap/_MainTex m_Scale (uv1_scale, 25 个非单位)
        self.tex_offset = tex_offset   # m_Offset (uv1_offset)
        self.queue_offset = queue_offset  # _QueueOffset/m_CustomRenderQueue → render_priority
        self.emission_tex_ref = emission_tex_ref  # _EmissionMap (84 个)

    def is_transparent(self):
        return '_SURFACE_TYPE_TRANSPARENT' in self.keywords or \
            '_ALPHABLEND_ON' in self.keywords or self.blend > 0

    def emission_energy(self):
        c = self.emission_color or {}
        return max(abs(c.get('r', 0.0)), abs(c.get('g', 0.0)), abs(c.get('b', 0.0)), 1.0)

    def roughness(self):
        # B 映射: roughness = 1 - _Smoothness (800 个默认 0.5 → 0.5; 引擎默认 0.9 保留为兜底)
        if self.smoothness is not None:
            return max(0.0, min(1.0, 1.0 - float(self.smoothness)))
        return None


def parse_mat(raw, name_hint=''):
    if not isinstance(raw, dict):
        return None
    sp = raw.get('m_SavedProperties', {}) or {}
    texenvs = sp.get('m_TexEnvs', []) or []
    floats = {k: v for k, v in (sp.get('m_Floats', []) or [])}
    colors = {k: v for k, v in (sp.get('m_Colors', []) or [])}
    tex_ref = None
    tex_ref2 = None
    for pair in texenvs:
        if not isinstance(pair, (list, tuple)) or len(pair) != 2:
            continue
        slot = pair[0]
        t = pair[1].get('m_Texture', {}) if isinstance(pair[1], dict) else {}
        if not t.get('m_PathID'):
            continue
        if slot == '_SecondaryTex' and tex_ref2 is None:
            tex_ref2 = t
        elif slot in ('_BaseMap', '_MainTex') and tex_ref is None:
            tex_ref = t
    uv = floats.get('_UVSpeed')
    uv_speed = (float(uv.get('x', 1.0)), float(uv.get('y', 1.0))) if isinstance(uv, dict) else None
    # 2026-08-24 坑83: _Cull=0(双面)/_ZWrite=0 被 "or 默认值" 吞掉 (0.0 or 2.0 → 2.0) —
    # 全场景 Fence/Floor(双面)定向为单面、透明材质 zwrite 错置 → 必须 None 判空
    cull_v = floats.get('_Cull')
    zw_v = floats.get('_ZWrite')
    blend_v = floats.get('_Blend')
    # 2026-08-26 B 映射表: 全参数读取 (None 判空延续)
    cutoff_v = floats.get('_ClipThreshold')
    if cutoff_v is None:
        cutoff_v = floats.get('_Cutoff')
    smooth_v = floats.get('_Smoothness')
    metal_v = floats.get('_Metallic')
    surf_v = floats.get('_Surface')
    src_v = floats.get('_SrcBlend')
    dst_v = floats.get('_DstBlend')
    aclip_v = floats.get('_AlphaClip')
    qoff_v = floats.get('_QueueOffset')
    if qoff_v is None:
        cq = raw.get('m_CustomRenderQueue')
        if isinstance(cq, (int, float)) and cq > -1:
            qoff_v = cq - 3000
    # 贴图平铺/偏移 (m_TexEnvs 槽内 m_Scale/m_Offset)
    ts = toff = None
    for pair in texenvs:
        if not isinstance(pair, (list, tuple)) or len(pair) != 2:
            continue
        if pair[0] in ('_BaseMap', '_MainTex'):
            v = pair[1] if isinstance(pair[1], dict) else {}
            try:
                sc = v.get('m_Scale', {})
                ts = (float(sc.get('x', 1.0)), float(sc.get('y', 1.0)))
                of = v.get('m_Offset', {})
                toff = (float(of.get('x', 0.0)), float(of.get('y', 0.0)))
            except Exception:
                ts = toff = None
            break
    # _EmissionMap 引用
    emis_tex = None
    for pair in texenvs:
        if isinstance(pair, (list, tuple)) and len(pair) == 2 and pair[0] == '_EmissionMap':
            t = pair[1].get('m_Texture', {}) if isinstance(pair[1], dict) else {}
            if t.get('m_PathID'):
                emis_tex = t
    return MatInfo(str(raw.get('m_Name') or name_hint), tex_ref,
                   float(blend_v if blend_v is not None else 0.0),
                   list(raw.get('m_ValidKeywords', []) or []),
                   float(cull_v if cull_v is not None else 2.0),
                   float(zw_v if zw_v is not None else 1.0),
                   colors.get('_BaseColor') or colors.get('_Color'),
                   colors.get('_EmissionColor'), uv_speed,
                   raw.get('m_Color') or colors.get('m_Color'),
                   tex_ref2, colors.get('Vector4_1'),
                   float(cutoff_v) if cutoff_v is not None else None,
                   float(smooth_v) if smooth_v is not None else None,
                   float(metal_v) if metal_v is not None else None,
                   float(surf_v) if surf_v is not None else None,
                   float(src_v) if src_v is not None else None,
                   float(dst_v) if dst_v is not None else None,
                   float(aclip_v) if aclip_v is not None else None,
                   ts, toff,
                   float(qoff_v) if qoff_v is not None else None,
                   emis_tex)


# ---------------------------------------------------------------- 汇编
class Assembler:
    def __init__(self, arena, out_dir):
        self.arena = arena
        self.out = out_dir
        self.root_dir = os.path.join(SCENE_ROOT, arena)
        self.b = BundleResolver(arena)
        self.TF = load_pid_dir(os.path.join(self.root_dir, 'Transform'))
        self.GO, _ = load_go_dir(os.path.join(self.root_dir, 'GameObject'))
        self.MF = load_pid_dir(os.path.join(self.root_dir, 'MeshFilter'))
        self.MR = load_pid_dir(os.path.join(self.root_dir, 'MeshRenderer'))
        self.SMR = load_pid_dir(os.path.join(self.root_dir, 'SkinnedMeshRenderer'))
        self.LNR = load_pid_dir(os.path.join(self.root_dir, 'LineRenderer'))
        self.MAT = load_pid_dir(os.path.join(self.root_dir, 'Material'))
        self.PS = load_pid_dir(os.path.join(self.root_dir, 'ParticleSystem'))
        self.PSR = load_pid_dir(os.path.join(self.root_dir, 'ParticleSystemRenderer'))
        self.CAM = load_pid_dir(os.path.join(self.root_dir, 'Camera'))
        self.LIG = load_pid_dir(os.path.join(self.root_dir, 'Light'))
        self.REN = load_pid_dir(os.path.join(self.root_dir, 'RenderSettings'))
        self.TEXDIR = os.path.join(self.root_dir, 'Texture2D')
        # 后处理 ColorLookup 的 LUT 贴图引用 (PostProcessing Profile: Bloom/Vignette/ColorLookup)
        self.lut_ref_val = None
        for f in glob.glob(os.path.join(self.root_dir, 'MonoBehaviour', '*.json')):
            try:
                d = json.load(open(f, encoding='utf-8'))
            except Exception:
                continue
            if d.get('m_Name') == 'ColorLookup':
                t = d.get('texture', {}).get('m_Value', {})
                if t.get('m_PathID'):
                    self.lut_ref_val = t
                    break
        self.go_comps = {}
        for pid, g in self.GO.items():
            for c in g.get('m_Component', []):
                self.go_comps.setdefault(pid, []).append(c.get('component', {}).get('m_PathID'))
        self.mat_cache = {}

    def go(self, pid):
        return self.GO.get(pid, {'m_Name': 'GO%d' % pid, 'm_Component': []})

    def root_transforms(self):
        return [tp for tp in self.TF if self.TF[tp].get('m_Father', {}).get('m_PathID') not in self.TF]

    def children_of(self, tpid):
        return [c.get('m_PathID') for c in self.TF[tpid].get('m_Children', [])
                if c.get('m_PathID') in self.TF]

    def local_trans(self, tpid):
        td = self.TF[tpid]
        lp = td.get('m_LocalPosition', {})
        lr = td.get('m_LocalRotation', {})
        ls = td.get('m_LocalScale', {})
        return (lp.get('x', 0.0), lp.get('y', 0.0), lp.get('z', 0.0),
                (lr.get('x', 0.0), lr.get('y', 0.0), lr.get('z', 0.0), lr.get('w', 1.0)),
                (ls.get('x', 1.0), ls.get('y', 1.0), ls.get('z', 1.0)))

    def mesh_of(self, gopid):
        for c in self.go_comps.get(gopid, []):
            mf = self.MF.get(c)
            if mf:
                ref = mf.get('m_Mesh', {})
                obj = self.b.read_obj(ref, 'Mesh')
                if obj is None:
                    return None, None
                return (self.b.obj_name(obj) or 'mesh%d' % ref.get('m_PathID')), obj
        # SkinnedMeshRenderer (黑军团 Chain1/Chain2 铁链: 单骨骼=自身, 绑定位=GO 变换; 静态呈现即可,
        # 形态键甩鞭动画见 clip17 接入) — 没有 SMR 就返回 None
        for c in self.go_comps.get(gopid, []):
            smr = self.SMR.get(c)
            if smr:
                ref = smr.get('m_Mesh', {})
                obj = self.b.read_obj(ref, 'Mesh')
                if obj is None:
                    return None, None
                return (self.b.obj_name(obj) or 'mesh%d' % ref.get('m_PathID')), obj
        return None, None

    def mats_of(self, gopid):
        out = []
        for c in self.go_comps.get(gopid, []):
            mr = self.MR.get(c) or self.SMR.get(c)
            if not mr:
                continue
            for ref in mr.get('m_Materials', []):
                mi = self.mat_of(ref)
                if mi:
                    out.append(mi)
        return out

    def mat_of(self, ref):
        pid, fid = ref.get('m_PathID'), ref.get('m_FileID', 0)
        key = (fid, pid)
        if key in self.mat_cache:
            return self.mat_cache[key]
        raw = self.MAT.get(pid)
        if raw is None:
            raw = self.b.mat_data(ref)
        mi = parse_mat(raw, 'mat_%s' % pid) if raw else None
        if mi and mi.tex_ref:
            obj = self.b.read_obj(mi.tex_ref, 'Texture2D')
            if obj is not None:
                mi.tex_name = self.b.obj_name(obj) or 'tex_%s' % mi.tex_ref.get('m_PathID')
                mi.tex_obj = obj
        if mi and mi.tex_ref2:
            obj2 = self.b.read_obj(mi.tex_ref2, 'Texture2D')
            if obj2 is not None:
                mi.tex_name2 = self.b.obj_name(obj2) or 'tex2_%s' % mi.tex_ref2.get('m_PathID')
                mi.tex_obj2 = obj2
        self.mat_cache[key] = mi
        return mi

    def ps_of(self, gopid):
        for c in self.go_comps.get(gopid, []):
            if c in self.PS:
                ps = self.PS[c]
                render_mode, mesh_ref, mats = 0, None, []
                for c2 in self.go_comps.get(gopid, []):
                    psr = self.PSR.get(c2)
                    if psr:
                        render_mode = psr.get('m_RenderMode', 0)
                        mesh_ref = psr.get('m_Mesh', {}) or None
                        for ref in psr.get('m_Materials', []):
                            mats.append(self.mat_of(ref))
                        break
                return ps, render_mode, mesh_ref, [m for m in mats if m]
        return None, 0, None, []

    def has_ps(self, gopid):
        return any(c in self.PS for c in self.go_comps.get(gopid, []))

    def has_comp(self, gopid, comps):
        return any(c in comps for c in self.go_comps.get(gopid, []))

    def has_smr(self, gopid):
        return any(c in self.SMR for c in self.go_comps.get(gopid, []))

    def renderer_sbi(self, gopid):
        """MeshRenderer m_StaticBatchInfo {firstSubMesh, subMeshCount} (2026-08-26 F 映射)"""
        for c in self.go_comps.get(gopid, []):
            mr = self.MR.get(c) or self.SMR.get(c)
            if mr:
                sbi = mr.get('m_StaticBatchInfo')
                if sbi:
                    return sbi
        return None

    def subtree_has_3d(self, tpid, seen=frozenset()):
        """子树内是否含可见 3D (PS/相机/灯/带材质网格) — 自动识别 3D 根用
        (各变体根名不统一: Scrap_4/Battle Arena X Baked/Particle Effects/Scenario/BattlePrefab)"""
        if tpid in seen:
            return False
        seen = seen | {tpid}
        gopid = self.TF[tpid].get('m_GameObject', {}).get('m_PathID')
        comps = self.go_comps.get(gopid, [])
        for c in comps:
            if c in self.PS or c in self.CAM or c in self.LIG:
                return True
            if c in self.MF and self.MF[c].get('m_Mesh', {}).get('m_PathID'):
                return True
            if c in self.MR and self.MR[c].get('m_Materials'):
                return True
        for ch in self.TF[tpid].get('m_Children', []):
            cid = ch.get('m_PathID')
            if cid in self.TF:
                if self.subtree_has_3d(cid, seen):
                    return True
        return False

    def auto_roots(self):
        return [tp for tp in self.root_transforms() if self.subtree_has_3d(tp)]


# ---------------------------------------------------------------- UV 区域 alpha 修复
UV_RECTS = {}   # tex_name -> [(u0,u1,v0,v1), ...] 网格 UV 引用区 (rect 仅记录)
UV_OBJS = {}    # tex_name -> [OBJ 磁盘路径] (三角光栅 alpha 修复用)


def uv_rect_of_obj(obj_path):
    """读 OBJ vt 范围 (Godot 导入自动 1-v, 这里记录原始 OBJ v 用于区域判定)"""
    us, vs = [], []
    try:
        for line in open(obj_path, encoding='utf-8', errors='ignore'):
            if line.startswith('vt '):
                p = line.split()
                us.append(float(p[1])); vs.append(float(p[2]))
    except OSError:
        return None
    return (min(us), max(us), min(vs), max(vs)) if us else None


def triangle_uv_mask(obj_path, w, h):
    """OBJ 面片 UV 三角 → 像素掩码 (numpy): 三角形内=真实渲染内容 (包围盒 rect 含 padding 是弧线根因)"""
    import numpy as np
    try:
        from gltf_export import obj_parse
        pos, uv, nrm, fi, tfi = obj_parse(obj_path)
    except Exception:
        return None
    if not uv or len(fi) != len(tfi):
        return None
    maxv = max(fi) + 1 if fi else 0
    if not maxv:
        return None
    mask = np.zeros((h, w), dtype=bool)
    for k in range(0, len(fi), 3):
        if k + 2 >= len(tfi):
            break
        p1 = uv[tfi[k]]
        p2 = uv[tfi[k + 1]]
        p3 = uv[tfi[k + 2]]
        xs = [p1[0], p2[0], p3[0]]
        ys = [p1[1], p2[1], p3[1]]
        # OBJ vt v 自下而上 (Unity 导出); Godot OBJ 导入自动 1-v, 磁盘 PNG 的采样行=(1-v)*h
        # → 光栅化用翻转后 v' (2026-08-23: 先前用 v·h 无翻转=修了错误侧, 子代理 uv 方向双重验证)
        ys = [1.0 - y for y in ys]
        p1s, p2s, p3s = (p1[0], ys[0]), (p2[0], ys[1]), (p3[0], ys[2])
        if max(xs) < 0 or min(xs) >= 1 or max(ys) < 0 or min(ys) >= 1:
            continue
        x0 = max(0, int(min(xs) * w) - 1)
        x1 = min(w, int(max(xs) * w) + 2)
        y0 = max(0, int(min(ys) * h) - 1)
        y1 = min(h, int(max(ys) * h) + 2)
        if x1 <= x0 or y1 <= y0:
            continue
        X, Y = np.meshgrid(np.arange(x0, x1), np.arange(y0, y1))
        U = (X + 0.5) / w
        V = (Y + 0.5) / h
        def edge(ax, ay, bx, by):
            return (bx - ax) * (V - ay) - (by - ay) * (U - ax)
        e1 = edge(*p1s, *p2s)
        e2 = edge(*p2s, *p3s)
        e3 = edge(*p3s, *p1s)
        inside = ((e1 >= 0) & (e2 >= 0) & (e3 >= 0)) | ((e1 <= 0) & (e2 <= 0) & (e3 <= 0))
        mask[y0:y1, x0:x1] |= inside
    return mask


def repair_uv_alpha(args, tex_paths):
    """预乘/无 alpha 图集修复 (三角光栅版, 2026-08-22 二十轮续2):
    UnityPy 解码场景大图集 alpha 通道多为 0 但 RGB=内容 (黑军团主图集内容区 75% alpha0,
    链/环/横幅/顶饰 → Godot ALPHA 材质下近透明=元素消失+透光弧线);
    修复=网格 UV **三角形内**像素 alpha→255 (三角形内=真实内容; 包围盒含 padding, 全修会白块爆炸,
    按亮度阈值修会"内容挖洞"=透光白弧 — 三角形掩码是精确区分)"""
    from PIL import Image
    import numpy as np
    if not UV_RECTS:
        return 0
    n_total = 0
    for tn, rects in UV_RECTS.items():
        print(f"[repair-debug] tn={tn!r} objs={len(UV_OBJS.get(tn, []))} rects={len(rects)}")
        # 三档策略 (2026-08-23 用户目视"上部灰白矩形"→ 子代理原版贴图逐像素核查):
        # Banners/Ground 原版 alpha 即设计值 (参考图证实横幅=镂空透光花纹, Ground 88.9% 基本完好,
        # 船影带 39-46% 半透明=原样) — 曾按"解码丢 alpha"误修→白占位/天空透光区被实体化=灰白矩形;
        # 不修 (保持原始 alpha)。仅 Atlas2 等"内容区 75%+ alpha=0"的真解码损坏图集按三角+保守判据修复。
        if 'Banners' in tn or 'Ground' in tn:
            print(f"[repair] skip (原版 alpha=设计值): {tn}")
            continue
        # 2026-08-26 正交实验A: Baked 恢复 skip (实体化后遮挡骷髅区=-83 无改善; Baked alpha-cut 多层透出=真语义)
        # Back Background/Background 仍实体化 (82% 白 alpha0=按 RGB 渲染); Baked 保持原版 alpha
        if 'Baked' in tn:
            print(f"[repair] skip (Baked alpha-cut 多层语义, 实体化实验回退): {tn}")
            continue
        # 2026-08-26 用户全景缺失检查: 三角内实体化 (原版按 RGB 渲染=Cutoff≈0)
        res = tex_paths.get(tn)
        p = res_to_fs(args.out, res) if res else None
        if not p or not os.path.exists(p):
            continue
        im = Image.open(p).convert('RGBA')
        arr = np.array(im)
        w, h = arr.shape[1], arr.shape[0]
        mask = None
        for (u0, u1, v0, v1) in rects:
            pass  # rect 保留仅作 tex 引用记录; 三角掩码在下方重建
        # 重新收集三角: UV_RECTS 存的是 obj 路径列表转 rect — 改为直接存 OBJ 路径
        for obj_p in UV_OBJS.get(tn, []):
            m = triangle_uv_mask(obj_p, w, h)
            if m is not None:
                mask = m if mask is None else (mask | m)
        if mask is None:
            continue
        alpha = arr[:, :, 3]
        mx = arr[:, :, :3].max(axis=2)
        # 8×8 块均值/方差: "均匀亮块"=占位填充/天空透光区(低饱和近白 ~180,165,157), 保持透明;
        # 非均匀亮块(金/橙/红内容)+暗色内容=真实图集内容, 实体化
        g = arr[:, :, :3].mean(axis=2)
        hh, ww = h - h % 8, w - w % 8
        blk_mean = g[:hh, :ww].reshape(hh // 8, 8, ww // 8, 8).mean(axis=(1, 3))
        blk_std = np.sqrt(((g[:hh, :ww] - blk_mean.repeat(8, 0).repeat(8, 1)) ** 2)
                          .reshape(hh // 8, 8, ww // 8, 8).mean(axis=(1, 3)))
        flat_avg = np.repeat(np.repeat(blk_mean, 8, 0), 8, 1)
        flat_std = np.repeat(np.repeat(blk_std, 8, 0), 8, 1)
        keep_flat = np.zeros((h, w), dtype=bool)
        # 2026-08-26: Floor/Ground 类贴图=不透明材质 (原版 blend=0 kw=[] → alpha 被忽略, 按 RGB 渲染)
        # → keep_flat 误保护地面条带 (亮灰 RGB189 alpha<10 全宽带=原版设计值) = 坑 90 中带暗差来源; 地板贴图全实体化
        # 2026-08-26 全景: Baked 天空/远景/骷髅区=alpha0+RGB亮=按 RGB 渲染 (原版 Cutoff≈0), 同判据全实体化
        # 2026-08-26 C 映射(⑧): 'Back  Background' 82% 白全景同判据 (此前名单漏它=注释与实现脱节=全景缺失剩余点);
        #   注意 'Battle Arena 1 Background' (无 Back)=真 alpha 设计值(天空透光 26.1%a0) → 保留 keep_flat 不豁免
        _flat_exempt = ('Floor' in p) or ('Ground' in p) or ('Baked' in p) \
            or ('Back  Background' in p) or ('Back Background' in p)
        keep_flat[:hh, :ww] = ((flat_avg > 140.0) & (flat_std < 8.0)) \
            if not _flat_exempt \
            else np.zeros((hh, ww), dtype=bool)
        # 内容像素=三角内 alpha0 → 实体化 (25<max<=245); 均匀亮块保持透明
        # 2026-08-26 全景缺失: mx<=245 上界把 RGB 254/255 的纯白内容(天空/骷髅区 82%)排除=元凶
        # (25 轮防 padding 白块=三角外, 三角 mask 已精确区分) → 三角内 alpha<10 无条件实体化
        sel = mask & (alpha < 10) & ~keep_flat
        arr[sel, 3] = 255
        Image.fromarray(arr).save(p)
        n_total += int(sel.sum())
    return n_total


# ---------------------------------------------------------------- 形态键/动画 (20轮续)
BS_FILES = {}   # mesh 名 -> blendshapes json 路径 (含形态键的网格)


def export_blend_shapes(name, obj, out_dir):
    """Unity Mesh m_Shapes → <mesh>.blendshapes.json:
    {channels: [形态键名按通道序], shapes: [[[vert_idx, dx,dy,dz], ...] 每 shape 增量]}"""
    try:
        d = obj.read()
        sh = getattr(d, 'm_Shapes', None)
        if not sh or not len(getattr(sh, 'channels', []) or []):
            return None
        channels = [c.name for c in sh.channels]
        shapes = []
        for s in sh.shapes:
            ds = []
            v0, n = s.firstVertex, s.vertexCount
            for i in range(v0, min(v0 + n, len(sh.vertices))):
                v = sh.vertices[i]
                pp = v.vertex
                ds.append([int(v.index), round(float(pp.x), 6), round(float(pp.y), 6), round(float(pp.z), 6)])
            shapes.append(ds)
        fp = os.path.join(out_dir, 'assets', 'meshes', win_safe(name) + '.blendshapes.json')
        json.dump({'channels': channels, 'shapes': shapes}, open(fp, 'w', encoding='utf-8'))
        return fp
    except Exception:
        return None


def pad_blacken(args, tex_paths):
    """padding 翻黑 (2026-08-25 坑89②白斑根治): 稀疏图集 padding=RGB 白+alpha0
    (原版 URP _ALPHATEST_ON 裁掉/白板材质 RGB0.2 黑不可见) — 导出后直出材质 (UNSHADED) 下
    若 UV 越界采样到 padding 会白块爆炸 → 全贴图 alpha<10 且 RGB 近白 texel 置黑 (内容区不受影响)"""
    from PIL import Image
    import numpy as np
    n = 0
    for tn, res in tex_paths.items():
        p = res_to_fs(args.out, res) if res else None
        if not p or not os.path.exists(p):
            continue
        try:
            im = Image.open(p).convert('RGBA')
        except Exception:
            continue
        arr = np.array(im)
        if arr.shape[2] != 4:
            continue
        al = arr[:, :, 3]
        mx = arr[:, :, :3].max(axis=2)
        sel = (al < 10) & (mx > 200)
        if not sel.any():
            continue
        arr[sel, 0] = 0
        arr[sel, 1] = 0
        arr[sel, 2] = 0
        Image.fromarray(arr).save(p)
        n += int(sel.sum())
        print('[pad_black] %s: %d px' % (tn, int(sel.sum())))
    print('✓ padding 翻黑: %d 像素' % n)
    return n


def clip_curves(arena_dir, clip):
    """AnimationClip JSON → 动画曲线 (clip17: blendShape 权重)"""
    fp = os.path.join(arena_dir, 'AnimationClip', 'AnimationClip_%s.json' % clip)
    if not os.path.exists(fp):
        return None
    d = json.load(open(fp, encoding='utf-8'))
    fcs = d.get('m_FloatCurves', []) or []
    curves = []
    for fc in fcs:
        m = re.search(r'blendShape\.Key (\d+)-', str(fc.get('attribute', '')))
        ks = ((fc.get('curve') or {}).get('m_Curve', [])) or []
        if m and ks:
            curves.append({'node': fc.get('path', ''), 'key': int(m.group(1)),
                           'keys': [(float(k.get('time', 0)), float(k.get('value', 0))) for k in ks]})
    return {'curves': curves}


def clip16_curves(arena_dir):
    """Camera Intro: m_EulerCurves + m_PositionCurves (局部坐标, 2.5s Once)
    2026-08-26 E 映射: clip 编号各场不同 (battlearena1=AnimationClip_19/黑军团=16) → 按 m_Name 含 'Camera Intro' 匹配;
    注意 battlearena1 无 AnimationClip_16.json (阈值检查以目录扫描为准)"""
    adir = os.path.join(arena_dir, 'AnimationClip')
    if not os.path.isdir(adir):
        return None
    files = [f for f in os.listdir(adir) if f.endswith('.json')
             and len(f[:-5].split('_')) <= 2]  # 排除副本 (=<名>_<pid>.json 三段)
    fname = None
    for f in sorted(files):
        if not f.endswith('.json') or '.json.' in f or f.endswith('_16.json'):
            continue
        try:
            dd = json.load(open(os.path.join(arena_dir, 'AnimationClip', f), encoding='utf-8'))
        except Exception:
            continue
        if not isinstance(dd, dict):
            continue
        nm = str(dd.get('m_Name', ''))
        if 'Camera Intro' in nm and 'Intro' in nm:
            fname = f
            break
    if fname is None:
        # 变体导出无 m_Name? (UnityPy bundle 直读路径) — 尝试顺序: 19/20 (b1), 16 (黑军团), 任意含 intro 名
        for f in sorted(files):
            if f.endswith('.json') and not f.endswith('_16.json') and f.endswith(('_18.json', '_19.json', '_20.json')):
                fname = f
                break
    if fname is None:
        return None
    d = json.load(open(os.path.join(arena_dir, 'AnimationClip', fname), encoding='utf-8'))

    def pick(sec):
        for item in d.get(sec, []) or []:
            ks = ((item.get('curve') or {}).get('m_Curve', [])) or []
            if ks:
                return [{'t': float(k.get('time', 0)), 'v': k.get('value')} for k in ks]
        return None

    def pick_axes(sec):
        """Euler/Position 分通道曲线 (attribute=localEulerAnglesRaw.x 等) → 按轴合并到统一时间轴"""
        out = {}
        for item in d.get(sec, []) or []:
            at = str(item.get('attribute', ''))
            ks = ((item.get('curve') or {}).get('m_Curve', [])) or []
            if not ks:
                continue
            axis = None
            for a in ('x', 'y', 'z'):
                if at.endswith('.' + a):
                    axis = a
                    break
            if axis is None:
                continue
            out.setdefault(axis, []).extend(
                {'t': float(k.get('time', 0)), 'v': float(k.get('value', 0) or 0)} for k in ks)
        for axis in out:
            out[axis].sort(key=lambda kv: kv['t'])
        return out if out else None

    pos_axes = pick_axes('m_PositionCurves')
    rot = pick_axes('m_EulerCurves')
    if pos_axes is None and rot is None:
        return {'pos': pick('m_PositionCurves'), 'rot': pick('m_EulerCurves')}
    return {'pos': pos_axes, 'rot': rot}


def clip16_samples(c16, n=25, dur=2.5):
    """c16 (clip16_curves 输出, pos/rot 均可为 分轴dict 或 list) → (pos_samples[(x,y,z)...], rot_samples[(x,y,z)...])
    以 n 等分线性插值 (Unity 曲线近似; slope/Hermite 保形后补)。rot 值=度 (m_EulerCurves) → 直接 rotation_degrees"""
    def axes_of(sec):
        if sec is None:
            return None
        if isinstance(sec, dict):
            ax = {}
            for axis in ('x', 'y', 'z'):
                keys = sec.get(axis) or []
                if keys:
                    ax[axis] = [(float(k['t']), float(k.get('v', 0) or 0)) for k in keys]
            return ax or None
        # 旧格式 list: [{t, v:{x,y,z}}] 或 [{t, v:标量}]
        ax = {}
        for k in sec:
            v = k.get('v')
            if isinstance(v, dict):
                for axis in ('x', 'y', 'z'):
                    ax.setdefault(axis, []).append((float(k['t']), float(v.get(axis, 0) or 0)))
            else:
                ax.setdefault('x', []).append((float(k['t']), float(v or 0)))
        return ax or None

    def sample(keys, t):
        """单轴 keys [(t,v)...] 线性插值 (keys=None→0.0)"""
        if not keys:
            return 0.0
        if t <= keys[0][0]:
            return keys[0][1]
        if t >= keys[-1][0]:
            return keys[-1][1]
        for i in range(len(keys) - 1):
            t0, v0 = keys[i]
            t1, v1 = keys[i + 1]
            if t0 <= t <= t1:
                if t1 == t0:
                    return v1
                return v0 + (v1 - v0) * (t - t0) / (t1 - t0)
        return keys[-1][1]

    pos_ax = axes_of(c16.get('pos'))
    rot_ax = axes_of(c16.get('rot'))
    pos_s = []
    rot_s = []
    for i in range(n + 1):
        t = dur * i / n
        pos_s.append(tuple(
            sample(pos_ax.get(a) if pos_ax and isinstance(pos_ax, dict) and a in pos_ax else None, t)
            for a in ('x', 'y', 'z')))
        rot_s.append(tuple(
            sample(rot_ax.get(a) if rot_ax and isinstance(rot_ax, dict) and a in rot_ax else None, t)
            for a in ('x', 'y', 'z')))
    return pos_s, rot_s


# ---------------------------------------------------------------- GDScript 写器


class GdWriter:
    def __init__(self, f):
        self.f = f
        self._var = 0

    def var(self, base='n'):
        self._var += 1
        return '%s%d' % (base, self._var)

    def line(self, s=''):
        self.f.write(s + '\n')

    def next_id(self, prefix):
        self._var += 1
        return '%s_%d' % (prefix, self._var)


# 原版 shader "Unlit UV scroll" (11_着色器 shaders/Shader_5091587426579848444): _MainTex+_SecondaryTex 双层,
# "UV Scale (XY) Speed (ZW)" 属性 → 黑军团 Vector4_1=(1,1,0.02,0) 副贴图 U 向 0.02/s 慢滚动 (Runes 符文);
# _Layers_Blend_Opacity 1.0 / unlit 无光照。Godot: unshaded spatial 双层 mix + TIME 滚动。
UV_SCROLL_SHADER = '''shader_type spatial;
render_mode unshaded, blend_mix;
uniform sampler2D main_tex : filter_linear, repeat_enable;
uniform sampler2D secondary_tex : filter_linear, repeat_enable;
uniform float layers_blend_opacity : hint_range(0.0, 1.0) = 1.0;
uniform vec4 uv_scroll = vec4(1.0, 1.0, 0.02, 0.0);

void fragment() {
	vec4 c1 = texture(main_tex, UV);
	vec4 c2 = texture(secondary_tex, UV * uv_scroll.xy + vec2(uv_scroll.z, uv_scroll.w) * TIME);
	vec4 c = mix(c1, c2, layers_blend_opacity);
	ALBEDO = c.rgb;
	ALPHA = c.a;
}
'''


# mat_gd() 死函数已删 (2026-08-26: 无调用者+同款 ShaderMaterial cull 错误)


# 原版 Battle Arena 4 PostProcessing 按说明文件 (原始 Unity JSON) 复刻:
# Bloom(threshold 1.15/intensity 5.0/scatter 1.0/skipIterations 6) + Vignette(0.297, smoothness 0.2 默认)
# + ColorLookup(LUT 图, contribution 1.0) + LUTBlender 全屏 pass(_LUT2=同图, _Blend 1.0 纯上屏)
# → 黑军团 ColorLookup=LUT Normal(共享资源, 标准 16^3 identity LUT: 行=G 层/格=B 层/格内=R 渐变, 直采=本色=
#   无颜色分级; 其余阵营各引用自家 LUT Battle Arena <X>.png)。LUT 布局采样: 半像素偏移 + bilinear。
LUT_SHADER = '''shader_type canvas_item;
uniform sampler2D screen_tex : hint_screen_texture, filter_linear_mipmap;
uniform sampler2D lut : filter_linear, repeat_disable;
uniform float lut_contribution = 1.0;
uniform float vignette_intensity = 0.297;
uniform float vignette_smoothness = 0.2;
uniform vec2 vignette_center = vec2(0.5, 0.5);

// 标准 Unity 3D LUT 256x16: 16 格 x 16 行。格内=R 渐变(每格 16 px), 行=G 层, 格=B 层。
vec3 lut_sample(vec3 c) {
	vec3 v = clamp(c, vec3(0.0), vec3(1.0));
	float g_step = v.g * 15.0;
	float b_step = v.b * 15.0;
	ivec2 g_row = ivec2(int(floor(g_step + 0.5)));
	ivec2 b_col = ivec2(int(floor(b_step + 0.5)));
	vec2 uv = vec2(
		(float(b_col.x) * 16.0 + v.r * 15.0 + 0.5) / 256.0,
		(15.0 - float(g_row.x) + 0.5) / 16.0);
	return texture(lut, uv).rgb;
}

void fragment() {
	vec4 c = texture(screen_tex, SCREEN_UV);
	vec3 lut_out = lut_sample(c.rgb);
	c.rgb = mix(c.rgb, lut_out, lut_contribution);
	float d = distance(SCREEN_UV, vignette_center);
	float falloff = smoothstep(1.0, 1.0 - vignette_intensity - vignette_smoothness, d);
	c.rgb *= mix(1.0, falloff, vignette_intensity);
	COLOR = c;
}
'''


def win_safe(s):
    """Windows 文件名消毒: ':' 会被 NTFS 当 ADS 流 ('Combined Mesh (root: scene)' → 数据写进隐藏流=不可见)
    *?\"<>| 同样非法; '/'→'_' 防止目录穿越"""
    return re.sub(r'[\/:*?"<>|]', '_', s)[:80]


def res_to_fs(out, res):
    return os.path.join(out, res[len('res://'):].replace('/', os.sep))


def dedupe_file(final_p, tmp_p, arena):
    """同名资源内容去重: 内容相同→复用已有; 内容不同→落 <name>__<arena> 后缀
    (各变体贴图/网格同名异内容很常见: 'Combined Mesh (root: scene)'/'Flame03'/'Atlas' 等)"""
    import hashlib
    if not os.path.exists(final_p):
        os.replace(tmp_p, final_p)
        return final_p
    h1 = hashlib.md5(open(final_p, 'rb').read()).hexdigest()
    h2 = hashlib.md5(open(tmp_p, 'rb').read()).hexdigest()
    if h1 == h2:
        os.remove(tmp_p)
        return final_p
    d = os.path.dirname(final_p)
    stem, ext = os.path.splitext(os.path.basename(final_p))
    alt = os.path.join(d, '%s__%s%s' % (stem, arena, ext))
    if os.path.exists(alt):
        os.remove(alt)
    os.replace(tmp_p, alt)
    return alt


def make_white_tex(fp, wp):
    """烟材质的白版贴图: RGB→255 保留 alpha (原贴图 RGB≈0.2 灰 × 暗棕 albedo 双重乘≈黑不可见;
    原版做法=发光白烟由 albedo 定色)"""
    if os.path.exists(wp):
        return
    try:
        from PIL import Image
    except Exception:
        return
    try:
        img = Image.open(fp).convert('RGBA')
    except Exception:
        return
    px = img.load()
    w, h = img.size
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            px[x, y] = (255, 255, 255, a)
    img.save(wp)


def world_chain_of(a, tpid):
    """沿 m_Father 链连乘局部 pos/quat (链上 scale 均为 1) → Unity 世界变换"""
    chain = []
    cur = tpid
    while cur is not None and cur in a.TF:
        chain.append(a.local_trans(cur))
        cur = a.TF[cur].get('m_Father', {}).get('m_PathID')
    p = (0.0, 0.0, 0.0)
    q = (0.0, 0.0, 0.0, 1.0)
    for lt in reversed(chain):
        lp, lq = lt[0:3], lt[3]
        rp = q_rot_vec(q, lp)
        p = (p[0] + rp[0], p[1] + rp[1], p[2] + rp[2])
        q = q_mul(q, lq)
    return p, q


def conj_z_trs(lt):
    """Z 反射共轭 TRS: T' = M_z·T·M_z (位置 z 取反 + 四元数 reflect_z_q + scale 不变) —
    与顶点数据 z 镜像配合 = 免运行时负 scale 的整场 Z 反射 (方案A glTF 静态折入)"""
    return (lt[0], lt[1], -lt[2]), reflect_z_q(lt[3]), lt[4]


def resolve_lut_res(args, a):
    """ColorLookup LUT 贴图 (场景引用 → 导出 PNG → res 路径; 无则回退共享 LUT Normal)"""
    lref = a.lut_ref_val
    if lref:
        try:
            lobj = a.b.read_obj(lref, 'Texture2D')
            if lobj is not None:
                limg = getattr(lobj.read(), 'image', None)
                if limg is not None:
                    ln = a.b.obj_name(lobj) or 'lut_%s' % lref.get('m_PathID')
                    lp = os.path.join(args.out, 'assets', 'textures', win_safe(ln) + '.png')
                    if not os.path.exists(lp):
                        os.makedirs(os.path.dirname(lp), exist_ok=True)
                        limg.save(lp + '.tmp', format='PNG')
                        lp = dedupe_file(lp, lp + '.tmp', args.arena)
                    return RESP + 'textures/' + os.path.basename(lp)
        except Exception:
            pass
    if os.path.exists(os.path.join(args.out, 'assets', 'textures', 'LUT Normal.png')):
        return RESP + 'textures/LUT Normal.png'
    return None


# ---------------------------------------------------------------- 方案A: 场景级 glTF 导出 (2026-08-23)
def rot_from_to(a, b):
    """最短弧旋转 (a→b, 单位向量) → 四元数; 用于反射后视线/灯方向定轴
    (反射下 'B·(0,0,-1)=方向' 约定翻转, 共轭四元数得 -M·d → 必须按向量直接构造)"""
    ax, ay, az = a
    bx, by, bz = b
    dot = ax * bx + ay * by + az * bz
    if dot > 0.999999:
        return (0.0, 0.0, 0.0, 1.0)
    if dot < -0.999999:
        return (1.0, 0.0, 0.0, 0.0) if abs(az) < 0.99 else (0.0, 1.0, 0.0, 0.0)
    cx, cy, cz = ay * bz - az * by, az * bx - ax * bz, ax * by - ay * bx
    s = math.sqrt(0.5 * (1.0 + dot))
    inv = 1.0 / (2.0 * s)
    return (cx * inv, cy * inv, cz * inv, s)


def emit_gltf(args, a, all_nodes, mesh_paths, tex_paths, white_paths):
    """fmt=gltf: Mesh+材质链+贴图+相机/灯+手性转换 (Z 反射折入顶点/变换, 无负 scale) 一次导出
    → Godot 原生导入 → 贴图/骨架/动画(glTF)/粒子(代码) 各取所长。
    与 fmt=gd (MirrorZ 根) 渲染等价: 顶点 v→(x,y,-z)+绕序翻转, 每节点 TRS 共轭 (见 conj_z_trs)"""
    from gltf_export import GltfDoc, merge_obj_gltf, _f32
    gd = GltfDoc()
    out = args.out
    gd.doc['root_nodes'] = [gd.add_node(name='ArenaContent')]
    gd.doc.setdefault('extensionsUsed', [])

    def use_ext(e):
        if e not in gd.doc['extensionsUsed']:
            gd.doc['extensionsUsed'].append(e)

    img_uri, tex_uri = {}, {}
    mat_cache = {}
    node_idx = {}
    emitted = []          # (gopid, node_i, parent_key) parent_key: tpid 或 'ROOT'
    cam_info = {'pitch': {}, 'bg': None, 'name': None}
    light_info = []       # (nm, energy, shadow)
    morph_nodes = []      # (node_i, nm, curves)
    patches = {}          # nm -> pact
    patch_mi = {}         # nm -> MatInfo (uvscroll 修正重建)
    curves17 = clip_curves(os.path.join(SCENE_ROOT, args.arena), '17')

    def gltf_material(mi, smr=False):
        key = id(mi)
        if key in mat_cache:
            return mat_cache[key]
        base = {'pbrMetallicRoughness': {'metallicFactor': 0.0, 'roughnessFactor': 0.9}}
        if mi.tex_name and mi.tex_name in tex_paths:
            uri = '../textures/' + os.path.basename(tex_paths[mi.tex_name])
            if uri not in img_uri:
                gd.doc.setdefault('images', []).append({'uri': uri})
                img_uri[uri] = len(gd.doc['images']) - 1
                gd.doc.setdefault('textures', []).append({'source': img_uri[uri]})
                tex_uri[uri] = len(gd.doc['textures']) - 1
            base['pbrMetallicRoughness']['baseColorTexture'] = {'index': tex_uri[uri]}
            if mi.base_color:
                c = mi.base_color
                base['pbrMetallicRoughness']['baseColorFactor'] = [
                    float(c.get('r', 1.0)), float(c.get('g', 1.0)), float(c.get('b', 1.0)),
                    float(c.get('a', 1.0))]
        if mi.emission_color:
            c = mi.emission_color
            base['emissiveFactor'] = [max(0.0, min(1.0, float(c.get('r', 0.0)))),
                                      max(0.0, min(1.0, float(c.get('g', 0.0)))),
                                      max(0.0, min(1.0, float(c.get('b', 0.0))))]
            e = mi.emission_energy()
            if e > 1.0:
                base.setdefault('extensions', {})['KHR_materials_emissive_strength'] = {
                    'emissiveStrength': e}
                use_ext('KHR_materials_emissive_strength')
        if mi.is_transparent():
            base['alphaMode'] = 'BLEND'
        if mi.cull == 0 or smr:
            base['doubleSided'] = True
        pact = {'add': mi.blend >= 2,
                'depth_off': mi.is_transparent() and mi.zwrite <= 0,
                'cull_front': mi.cull == 1,
                'uvscroll': bool(mi.tex_name2 and mi.tex_name2 in tex_paths)}
        m_i = gd.add_material(mi.name or 'mat%d' % len(mat_cache), mat=base)
        mat_cache[key] = (m_i, pact)
        return m_i, pact

    def gltf_parent(tpid):
        cur = tpid
        while cur is not None and cur in a.TF:
            par = a.TF[cur].get('m_Father', {}).get('m_PathID')
            if par in node_idx:
                return node_idx[par]
            cur = par
        return gd.doc['root_nodes'][0]

    for kind, tpid, gopid, nm, payload, parent_tpid in all_nodes:
        if kind == 'skip-mesh':
            continue
        if kind == 'container':
            n_i = gd.add_node(name=nm, trs=conj_z_trs(a.local_trans(tpid)))
            node_idx[gopid] = n_i
            emitted.append((gopid, n_i, parent_tpid))
            continue
        if kind == 'mesh':
            mats = a.mats_of(gopid)
            mi = mats[0] if mats else None
            if mi is None or not ((mi.tex_name and mi.tex_name in tex_paths) or
                                  (mi.tex_name2 and mi.tex_name2 in tex_paths)):
                continue  # 无可见材质网格 = 碰撞/逻辑 (同 gd 路径)
            smr = a.has_smr(gopid)
            m_i, pact = gltf_material(mi, smr)
            obj_path = mesh_paths[payload].replace('res://', out + '/')
            # 2026-08-26 F 映射 firstSubMesh: 共享多 submesh 网格的渲染器按 StaticBatchInfo 选段
            sbi = a.renderer_sbi(gopid)
            if sbi:
                fsm = int(sbi.get('firstSubMesh', 0) or 0)
                if fsm > 0 or int(sbi.get('subMeshCount', 0) or 0) > 1:
                    key = '%s__s%d' % (payload, fsm)
                    if key in SUB_MESH_PATHS:
                        obj_path = SUB_MESH_PATHS[key].replace('res://', out + '/')
            geo = merge_obj_gltf(obj_path, mirror_z=True)
            morph_deltas = None
            weights = None
            if payload in BS_FILES:
                bsdata = json.load(open(BS_FILES[payload], encoding='utf-8'))
                morph_deltas = [[(int(e[0]), e[1], e[2], -e[3]) for e in s] for s in bsdata['shapes']]
                weights = [0.0] * len(morph_deltas)
            prim, targets = gd.add_mesh_primitive(geo, m_i, name=win_safe(payload),
                                                  morph_deltas=morph_deltas)
            mesh_i = gd.add_mesh([prim], name=win_safe(payload), weights=weights if targets else None)
            n_i = gd.add_node(name=nm, mesh=mesh_i, trs=conj_z_trs(a.local_trans(tpid)))
            node_idx[gopid] = n_i
            emitted.append((gopid, n_i, parent_tpid))
            if targets:
                morph_nodes.append((n_i, nm, [c for c in (curves17 or {}).get('curves', [])
                                              if c.get('node') == nm]))
            if any(pact.values()):
                patches[nm] = pact
                patch_mi[nm] = mi
            continue
        if kind == 'camera':
            cd = next(a.CAM[c] for c in a.go_comps[gopid] if c in a.CAM)
            if not cd.get('m_Enabled', 1):
                print('  [跳过禁用相机] %s' % nm)
                continue
            wp, wq = world_chain_of(a, tpid)
            # 视线: Unity 看 +Z (q·ẑ) → Z 反射后 = M_z·d; Godot 相机看 -Z_local →
            # 直接取 短弧旋转 (0,0,-1)→M_z·d (反射下共轭四元数会得 -M·d 符号反转, 勿用)
            du = q_rot_vec(wq, (0.0, 0.0, 1.0))
            qq = rot_from_to((0.0, 0.0, -1.0), (du[0], du[1], -du[2]))
            fov_v = float(cd.get('field of view') or 46.397)
            lsy = float(cd.get('m_LensShift', {}).get('y') or 0.0)
            if lsy and not args.no_lensshift:
                theta = math.atan(lsy * math.tan(math.radians(fov_v) / 2.0))
                qq = q_mul(qq, (math.sin(theta / 2.0), 0.0, 0.0, math.cos(theta / 2.0)))
                cam_info['pitch'][nm] = math.degrees(theta)
                print('  [camera] LensShift y=%.3f → pitch %+.2f°' % (lsy, cam_info['pitch'][nm]))
            cam_info['name'] = nm
            gd.doc.setdefault('cameras', []).append({
                'type': 'perspective',
                'perspective': {'yfov': math.radians(fov_v),
                                'znear': float(cd.get('near clip plane', 0.3)),
                                'zfar': float(cd.get('far clip plane', 300.0))}})
            n_i = gd.add_node(name=nm, cam=len(gd.doc['cameras']) - 1,
                              trs=((wp[0], wp[1], -wp[2]), qq, (1.0, 1.0, 1.0)))
            node_idx[gopid] = n_i
            emitted.append((gopid, n_i, 'ROOT'))
            if cam_info['bg'] is None and 'UI' not in nm:
                bc = cd.get('m_BackGroundColor') or {}
                cam_info['bg'] = (float(bc.get('r', 0.0288)), float(bc.get('g', 0.0288)),
                                  float(bc.get('b', 0.0294)))
            continue
        if kind == 'light':
            ld = next(a.LIG[c] for c in a.go_comps[gopid] if c in a.LIG)
            lt = a.local_trans(tpid)
            wp, wq = world_chain_of(a, tpid)
            # 旧 gd 实现: 局部 q⊗RotX(-90) 挂链下, 方向=链q·q·RX(-90)·(0,0,-1);
            # 反射后方向 = M_z·d → 短弧旋转构造 (同相机, 共轭符号反转勿用)
            d_old = q_rot_vec(q_mul(wq, q_mul(lt[3], (-0.70710678, 0.0, 0.0, 0.70710678))),
                              (0.0, 0.0, -1.0))
            rot_l = rot_from_to((0.0, 0.0, -1.0), (d_old[0], d_old[1], -d_old[2]))
            lc = ld.get('m_Color', {}) or {}
            use_ext('KHR_lights_punctual')
            gd.doc.setdefault('extensions', {}).setdefault(
                'KHR_lights_punctual', {'lights': []})
            gd.doc['extensions']['KHR_lights_punctual']['lights'].append({
                'type': 'directional', 'intensity': 1.0,
                'color': [float(lc.get('r', 1.0) or 1.0), float(lc.get('g', 1.0) or 1.0),
                          float(lc.get('b', 1.0) or 1.0)]})
            li = len(gd.doc['extensions']['KHR_lights_punctual']['lights']) - 1
            n_i = gd.add_node(name=nm, light=li,
                              trs=((wp[0], wp[1], -wp[2]), rot_l, (1.0, 1.0, 1.0)))
            node_idx[gopid] = n_i
            emitted.append((gopid, n_i, 'ROOT'))
            sh = ld.get('m_Shadows', {})
            light_info.append((nm, float(ld.get('m_Intensity', 1.0) or 1.0) * args.light_energy,
                               sh.get('m_Type', 0) != 0 and ld.get('m_Type') == 1))
            continue
    # 父子挂接 (nearest emitted ancestor; 'ROOT' → 场景根)
    for gopid, n_i, parent_key in emitted:
        parent_i = gd.doc['root_nodes'][0] if parent_key == 'ROOT' else gltf_parent(parent_key)
        gd.doc['nodes'][parent_i].setdefault('children', []).append(n_i)
    # clip17 形态键权重动画 (单动画, 每 morph 节点一条 weights 通道)
    if morph_nodes:
        n_samples = 61
        length = 15.98
        times = [length * i / (n_samples - 1) for i in range(n_samples)]

        def ev(keys, t):
            if not keys:
                return 0.0
            if t <= keys[0][0]:
                return keys[0][1]
            if t >= keys[-1][0]:
                return keys[-1][1]
            for i in range(len(keys) - 1):
                t0, v0 = keys[i]
                t1, v1 = keys[i + 1]
                if t0 <= t <= t1:
                    f = 0.0 if t1 == t0 else (t - t0) / (t1 - t0)
                    return v0 + (v1 - v0) * f
            return keys[-1][1]

        t_acc = gd.add_accessor(gd.add_view(_f32(times), 34962), 5126, n_samples, 'SCALAR',
                                'anim_time', ([0.0], [length]))
        channels, samplers = [], []
        for si, (n_i, nm2, curves) in enumerate(morph_nodes):
            n_shape = len(gd.doc['meshes'][gd.doc['nodes'][n_i]['mesh']].get('weights', []))
            if not n_shape:
                continue
            weights = []
            for t in times:
                wv = [0.0] * n_shape
                for c in curves:
                    k = int(c.get('key', 1)) - 1
                    if 0 <= k < n_shape:
                        wv[k] = ev(c['keys'], t) / 100.0
                weights.append(wv)
            w_acc = gd.add_accessor(gd.add_view(_f32([x for w in weights for x in w]), 34962),
                                    5126, len(weights) * n_shape, 'SCALAR', nm2 + '_weights')
            samplers.append({'input': t_acc, 'interpolation': 'LINEAR', 'output': w_acc})
            channels.append({'sampler': si, 'target': {'node': n_i, 'path': 'weights'}})
        gd.add_animation('SceneAnim', channels, samplers)
    # 写盘
    gltf_p = os.path.join(out, 'assets', 'meshes', 'unity_arena_%s.gltf' % args.arena)
    bin_p = os.path.join(out, 'assets', 'meshes', 'unity_arena_%s.bin' % args.arena)
    gd.write(gltf_p, bin_p)
    print('✓ 场景 glTF: %s (%d 节点 / %d 网格 / %d 材质)' % (
        gltf_p, len(gd.doc['nodes']), len(gd.doc.get('meshes', [])),
        len(gd.doc.get('materials', []))))

    # ---- 薄 .gd: 实例化 glTF + 引擎校准 + 命名材质修正 + 灯/相机 + 粒子 + 环境/LUT + intro ----
    out_gd = os.path.join(out, 'scenes', 'unity_arena_%s.gd' % args.arena)
    with open(out_gd, 'w', encoding='utf-8') as f:
        w = GdWriter(f)
        w.line('# 由 unity_scene_to_godot.py 自动生成 (fmt=gltf) — 勿手改, 重跑脚本即可')
        w.line('# 来源: 解包整理/07_场景/%s (原版 Unity 序列化 JSON 说明书)' % args.arena)
        w.line('# 静态场景=scene.gltf (手性已在导出阶段折入, 无负 scale); 粒子/环境/LUT/intro 代码侧')
        w.line('extends Node3D')
        w.line('')
        w.line('func _ready() -> void:')
        w.line('\t_build()')
        w.line('')
        w.line('func _build() -> void:')
        w.line('\tvar scene: Node3D = load(%r).instantiate()' % (RESP + 'meshes/' +
              os.path.basename(gltf_p)))
        # clip17 autoplay 必须在 add_child 之前设置 (AnimationPlayer 入树时读取 autoplay)
        w.line('\tvar _ap: AnimationPlayer = scene.find_child("AnimationPlayer", true, false)')
        w.line('\tif _ap: _ap.autoplay = "SceneAnim"')
        w.line('\tscene.name = "Content"')
        w.line('\tadd_child(scene)')
        w.line('')
        w.line('\t# 引擎等效校准: 原版 URP 无镜面高光 (暗场景锐边白弧) → 全部 SPECULAR_DISABLED')
        w.line('\tfor _mi in scene.find_children("*", "MeshInstance3D", true, false):')
        w.line('\t\tvar _mbase: StandardMaterial3D = null')
        w.line('\t\tif _mi.mesh != null:')
        w.line('\t\t\t_mbase = _mi.mesh.surface_get_material(0)')
        w.line('\t\tif _mbase is StandardMaterial3D:')
        w.line('\t\t\tvar _md := _mbase.duplicate() as StandardMaterial3D')
        w.line('\t\t\t_md.specular_mode = BaseMaterial3D.SPECULAR_DISABLED')
        w.line('\t\t\t_mi.set_surface_override_material(0, _md)')
        # 命名材质修正 (glTF 表达不了的混合/滚动/裁剪)
        for pi, (nm, pact) in enumerate(sorted(patches.items())):
            pvn = '_pn%d' % pi
            w.line('')
            w.line('\t# 材质修正: %s (%s)' % (nm, ','.join(k for k, v in pact.items() if v)))
            w.line('\tvar %s := scene.find_child(%r, true, false) as MeshInstance3D' % (pvn, nm))
            w.line('\tif %s and %s.mesh != null:' % (pvn, pvn))
            w.line('\t\tvar _pm: StandardMaterial3D = %s.get_surface_override_material(0)' % pvn)
            w.line('\t\tif _pm == null: _pm = %s.mesh.surface_get_material(0)' % pvn)
            w.line('\t\tif _pm is StandardMaterial3D:')
            if pact['uvscroll']:
                # 双贴图 (Runes "Unlit UV scroll"): shader 双层 + TIME 滚动
                mi2 = patch_mi[nm]
                w.line('\t\t\tvar _sus := ShaderMaterial.new()')
                w.line('\t\t\t_sus.shader = load("res://assets/uv_scroll.gdshader")')
                w.line('\t\t\t_sus.set_shader_parameter("main_tex", load(%r))' % tex_paths[mi2.tex_name])
                w.line('\t\t\t_sus.set_shader_parameter("secondary_tex", load(%r))' % tex_paths[mi2.tex_name2])
                v4 = mi2.vector4_1 or {}
                w.line('\t\t\t_sus.set_shader_parameter("uv_scroll", Vector4(%.3f, %.3f, %.3f, %.3f))' % (
                    float(v4.get('r', 1.0) or 1.0), float(v4.get('g', 1.0) or 1.0),
                    float(v4.get('b', 0.0) or 0.0), float(v4.get('a', 0.0) or 0.0)))
                w.line('\t\t\t_sus.set_shader_parameter("layers_blend_opacity", 1.00)')
                w.line('\t\t\t%s.set_surface_override_material(0, _sus)' % pvn)
            else:
                w.line('\t\t\tvar _pd := _pm.duplicate() as StandardMaterial3D')
                if pact['add']:
                    w.line('\t\t\t\t_pd.blend_mode = BaseMaterial3D.BLEND_MODE_ADD')
                if pact['depth_off']:
                    w.line('\t\t\t\t_pd.depth_draw_mode = BaseMaterial3D.DEPTH_DRAW_DISABLED')
                if pact['cull_front']:
                    w.line('\t\t\t\t_pd.cull_mode = BaseMaterial3D.CULL_FRONT')
                w.line('\t\t\t%s.set_surface_override_material(0, _pd)' % pvn)
        # 灯能量校准 (glTF intensity 语义 ≠ Unity cc/m²; 直通=过曝, 发光因子等效校准)
        for li2, (nm, energy, shadow) in enumerate(light_info):
            lvn = '_pl%d' % li2
            w.line('')
            w.line('\tvar %s := scene.find_child(%r, true, false) as DirectionalLight3D' % (lvn, nm))
            w.line('\tif %s:' % lvn)
            w.line('\t\t%s.light_energy = %.3f' % (lvn, energy))
            if shadow:
                w.line('\t\t%s.shadow_enabled = false  # RT 实证: 原版烘焙无实时投影 (2026-08-25)' % lvn)
        # 相机: 唯一 3D 相机 → current
        cam_name = cam_info['name'] or 'BoardCamera'
        w.line('')
        w.line('\tvar _cam := scene.find_child(%r, true, false) as Camera3D' % cam_name)
        w.line('\tif _cam: _cam.make_current()')
        w.line('')
        # 粒子 (MirrorZ 包装: 与 glTF 内容同手性; 世界坐标直挂 — 父 scale.z=-1 自动镜像方向)
        w.line('\tvar mirror := Node3D.new()')
        w.line("\tmirror.name = 'MirrorZ'")
        w.line('\tmirror.position = Vector3(0.0, 0.0, 0.0)')
        w.line('\tmirror.scale = Vector3(1.0, 1.0, -1.0)')
        w.line('\tself.add_child(mirror)')
        w.line('')
        for kind, tpid, gopid, nm, payload, parent_tpid in all_nodes:
            if kind == 'particle':
                emit_particle_gd(w, a, ('pn_%d' % gopid), tpid, nm, payload, tex_paths,
                                 parent_expr='mirror', white_paths=white_paths, world_rel=True)
        # 环境 + LUT (同 gd 路径)
        for r in a.REN.values():
            w.line('\tvar env := WorldEnvironment.new()')
            w.line('\tvar e := Environment.new()')
            probe = r.get('m_AmbientProbe') or {}

            def _sh(i):
                v = probe.get('sh[ %d]' % i) or probe.get('sh[%d]' % i)
                return float(v) if v is not None else 1.0
            sh_c = (_sh(0), _sh(1), _sh(2))
            w.line('\te.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR')
            w.line('\te.ambient_light_color = Color(%.4f, %.4f, %.4f, 1.0000)' %
                   tuple(max(0.0, c) for c in sh_c))
            w.line('\te.ambient_light_energy = %.3f' % args.ambient_energy)
            if r.get('m_Fog'):
                w.line('\te.fog_enabled = true')
                w.line('\te.fog_light_color = ' + col_str(r.get('m_FogColor', {})))
                w.line('\te.fog_density = %.5f' % float(r.get('m_FogDensity', 0.01)))
            w.line('\te.tonemap_mode = Environment.TONE_MAPPER_LINEAR')
            w.line('\te.glow_enabled = true')
            w.line('\te.glow_intensity = 5.0')
            w.line('\te.glow_hdr_threshold = 1.15')
            w.line('\te.set("glow_levels/6", 1.0)')
            w.line('\te.set("glow_levels/7", 1.0)')
            for i in range(1, 6):
                w.line('\te.set("glow_levels/%d", 0.0)' % i)
            w.line('\te.background_mode = Environment.BG_COLOR')
            bg = cam_info['bg'] or (0.0288, 0.0288, 0.0294)
            w.line('\te.background_color = Color(%.4f, %.4f, %.4f, 1)' % bg)
            w.line('\tenv.environment = e')
            w.line('\tenv.name = "Env"')
            w.line('\tadd_child(env)')
            w.line('\tvar pp := CanvasLayer.new()')
            w.line('\tpp.layer = -1')
            w.line('\tvar cr := ColorRect.new()')
            w.line('\tcr.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)')
            w.line('\tcr.mouse_filter = Control.MOUSE_FILTER_IGNORE')
            w.line('\tvar pm := ShaderMaterial.new()')
            w.line('\tpm.shader = load("res://assets/lut_vignette.gdshader")')
            lut_res = resolve_lut_res(args, a)
            if lut_res:
                w.line('\tpm.set_shader_parameter("lut", load(%r))' % lut_res)
                w.line('\tpm.set_shader_parameter("lut_contribution", 1.0)')
            w.line('\tpm.set_shader_parameter("vignette_intensity", 0.297)')
            w.line('\tpm.set_shader_parameter("vignette_smoothness", 0.2)')
            w.line('\tcr.material = pm')
            w.line('\tpp.add_child(cr)')
            w.line('\tadd_child(pp)')
            break
        # ---- Camera Intro (clip16) ----
        c16 = clip16_curves(os.path.join(SCENE_ROOT, args.arena))
        if c16 and c16.get('pos'):
            w.line('')
            pos = c16['pos']
            p0, p1 = pos[0]['v'], pos[-1]['v']
            p0x, p0y, p0z = 100.0 + p0['x'], p0['y'], -p0['z']
            p1x, p1y, p1z = 100.0 + p1['x'], p1['y'], -p1['z']
            rot = c16.get('rot') or []
            r0x = rot[0]['v']['x'] if rot else 0.0
            r1x = -cam_info['pitch'].get(cam_name, 0.0) if cam_info['pitch'] else (
                rot[-1]['v']['x'] if rot else 0.0)
            w.line('\t_tgt_cam = _cam')
            w.line('')
            w.line('var _tgt_cam: Camera3D')
            w.line('var _anim_t := 0.0')
            w.line('')
            w.line('func _process(delta: float) -> void:')
            w.line('\t_anim_t += delta')
            w.line('\tif _tgt_cam:')
            w.line('\t\tvar _it := clampf(_anim_t / 2.5, 0.0, 1.0)')
            w.line('\t\t_tgt_cam.position = Vector3(%.4f, %.4f, %.4f).lerp(Vector3(%.4f, %.4f, %.4f), _it)' % (
                p0x, p0y, p0z, p1x, p1y, p1z))
            w.line('\t\t_tgt_cam.rotation_degrees.x = lerpf(%.4f, %.4f, _it)' % (r0x, r1x))
            w.line('\t\tif _it >= 1.0:')
            w.line('\t\t\t_tgt_cam = null  # intro 结束: 停止每帧写相机')
    # 包装 tscn (挂脚本)
    out_tscn = os.path.join(out, 'scenes', 'unity_arena_%s.tscn' % args.arena)
    with open(out_tscn, 'w', encoding='utf-8') as f:
        f.write('[gd_scene load_steps=2 format=3]\n\n')
        f.write('[ext_resource type="Script" path="res://scenes/unity_arena_%s.gd" id="1"]\n\n' % args.arena)
        f.write('[node name="Arena" type="Node3D"]\n')
        f.write('script = ExtResource("1")\n')
    print('✓ 输出:', out_gd)


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default='battlearenablacklegion')
    ap.add_argument('--out', default='d:/2/战场演示')
    ap.add_argument('--roots', default='',
                    help='只保留根名包含这些子串的 (逗号分隔); 留空=按子树含量自动识别 3D 根 (推荐)')
    ap.add_argument('--skip', default=('Scene Initializer,BattleManager,EnvironmentConditions,'
                                       'Battle Events,BattleHud,BattleTipController,Cinemachine,'
                                       'PlayerBoardArea,EnemyBoardArea,Particle colliders,'
                                       'Board Center,Friend Hand,Enemy Hand,TapParticleController,'
                                       'BattleManagerPositions,BattleErrors,NoCanvas2D,'
                                       'TouchInputManager,Battle Music,AudioMixer,Cache [,'
                                       'CardMaterialHelper,TutorialController,BattleCacheManager,'
                                       'Shadow Receiver,CardLowBoardLimit,TMP SubMesh,CardBack,'
                                       'Card Info,Textbackgrounds,ChooseCardMenuAnchor,'
                                       'MulliganAnchor,CemeteryGroup,EffectList,EffectAnchor,'
                                       'HandArea,PlayerArea,EnemyAssetArea,'
                                       'MinionOrWarlord,Tactic Container,Bg,arrow1,TutorialArrows'),
                    help='跳过名字包含这些子串的整棵子树 (逗号分隔); '
                         '注意: Energy Accumulation 不再跳过 (2026-08-22 二十轮: 原版粒子树 active=True,'
                         ' 原排除=违背完全复刻; 仅 2D UI 根由 HandArea/PlayerArea 等排除)')
    ap.add_argument('--all-roots', action='store_true', help='不过滤根')
    ap.add_argument('--light-energy', type=float, default=1.1,
                    help='方向光能量校准系数 (引擎差异: Unity SH 全阶+URP 直通 vs Godot 平场; 黑军团 1.1=参考亮度匹配 2026-08-23)')
    ap.add_argument('--ambient-energy', type=float, default=1.15,
                    help='环境光能量校准 (Unity 直通过曝, 引擎物理等效)')
    ap.add_argument('--unshaded-bake', action='store_true',
                    help='坑86b 实验: Baked 烘焙纹理材质用 SHADING_MODE_UNSHADED (贴图直出)')
    ap.add_argument('--pad-black', action='store_true',
                    help='坑89②: padding 翻黑 (alpha<10 且 RGB 近白 → 黑; 直出材质白斑根治)')
    ap.add_argument('--res-prefix', default='',
                    help='资源 res:// 前缀子目录 (12 变体用 assets/arenas/<arena>)')
    ap.add_argument('--unshaded-gain', type=float, default=1.0,
                    help='UNSHADED 材质全局 albedo 增益 (原版≈贴图×URP后处理≈0.82)')
    ap.add_argument('--no-lensshift', action='store_true',
                    help='跳过 LensShift→pitch 补偿 (pitch 方向实验未定, 回退用)')
    ap.add_argument('--lensshift-deg', type=float, default=None,
                    help='直接指定 LensShift 补偿俯角 (度, 负数=俯视; 覆盖公式 — 坑89 矩阵定案用)')
    ap.add_argument('--no-mirror', action='store_true',
                    help='试验: 内容直挂 Unity 世界系+相机仅 Y180 (2026-08-23 弧线朝外=错, 仅试验)')
    ap.add_argument('--mirror', action='store_true',
                    help='试验: MirrorX X 镜像 (2026-08-23 弧线朝外=错, 仅试验; 默认=Z 反射=定案)')
    ap.add_argument('--flip-tex', action='store_true', default=False,
                    help='场景 mesh 贴图水平翻转 (2026-08-26 曾为修复; 根因实锤=UnityPy OBJ 导出 x 取负'
                         '→ fix_obj_xmirror 正解已修, 默认 False; 保留作对照)')
    ap.add_argument('--fmt', default='gd', choices=['gd', 'gltf'],
                    help="gd=GDScript 场景脚本 (默认); gltf=场景级 glTF 原生导入 (方案A: "
                         "Mesh+材质+贴图+相机/灯+手性一次性导出, 无负 scale)")
    args = ap.parse_args()
    RESP = 'res://assets/' + (args.res_prefix + '/' if args.res_prefix else '')
    skips = [s.strip() for s in args.skip.split(',') if s.strip()]

    a = Assembler(args.arena, args.out)
    print('=== %s | GO %d, TF %d, PS %d, MAT %d' % (
        args.arena, len(a.GO), len(a.TF), len(a.PS), len(a.MAT)))
    roots = a.root_transforms()
    if args.all_roots:
        pass
    elif args.roots:
        roots = [t for t in roots if any(
            k in a.go(a.TF[t].get('m_GameObject', {}).get('m_PathID')).get('m_Name', '')
            for k in args.roots.split(','))]
    else:
        roots = a.auto_roots()
    print('根: %s' % sorted(a.go(a.TF[t].get('m_GameObject', {}).get('m_PathID')).get('m_Name', '?') for t in roots))

    # ---- 资源落地 ----
    mesh_paths, tex_paths = {}, {}
    white_paths = {}  # mi.tex_name → 白版烟贴图 res (RGB 白保留 alpha)

    def ensure_lut_shader():
        p = os.path.join(args.out, 'assets', 'lut_vignette.gdshader')
        os.makedirs(os.path.dirname(p), exist_ok=True)
        if not os.path.exists(p) or open(p, encoding='utf-8').read() != LUT_SHADER:
            with open(p, 'w', encoding='utf-8') as f:
                f.write(LUT_SHADER)
        p2 = os.path.join(args.out, 'assets', 'uv_scroll.gdshader')
        if not os.path.exists(p2) or open(p2, encoding='utf-8').read() != UV_SCROLL_SHADER:
            with open(p2, 'w', encoding='utf-8') as f:
                f.write(UV_SCROLL_SHADER)

    ensure_lut_shader()

    def ensure_mesh(name, obj):
        safe = win_safe(name)
        p = os.path.join(args.out, 'assets', 'meshes', safe + '.obj')
        if not os.path.exists(p):
            raw = None
            try:
                raw = obj.export()
            except Exception:
                raw = None
            # UnityPy 版本差异: export() 可能返回 None 或抛 TypeError — 分开 try,
            # 保证 read().export() 回退总能执行 (踩坑 2026-08-22: 同一 try 内回退被 except 吞掉)
            if raw is None and not isinstance(obj, dict):
                try:
                    raw = obj.read().export()
                except Exception:
                    raw = None
            if raw:
                os.makedirs(os.path.dirname(p), exist_ok=True)
                tmp = p + '.tmp'
                with open(tmp, 'w', encoding='utf-8', errors='replace') as f:
                    f.write(raw)
                fix_obj_normals(tmp)
                fix_obj_xmirror(tmp)
                p = dedupe_file(p, tmp, args.arena)
                imp = p + '.import'
                if os.path.exists(imp):
                    os.remove(imp)
            else:
                print('  [网格导出失败] %s' % name)
                return None
        elif not obj_valid(p):
            fix_obj_normals(p)
            imp = p + '.import'
            if os.path.exists(imp):
                os.remove(imp)
        res = RESP + 'meshes/' + os.path.basename(p)
        mesh_paths[name] = res
        # 多 submesh 共享网格 (battlearena2 'Combined Mesh (root: scene)' 31 submesh):
        # 按 m_SubMeshes 区段切分, 供 firstSubMesh 渲染器选段 (2026-08-26 F 映射)
        # 场景引用=res-prefix 目录 (assets/arenas/<arena>/meshes), 生成器基础文件=assets/meshes →
        # __s 双目录落地, 注册前缀路径
        if obj is not None:
            try:
                sms = obj.read_typetree().get('m_SubMeshes') or []
                if len(sms) > 1:
                    base_dir = os.path.dirname(p)
                    split = split_obj_submeshes(p, name, base_dir, sms)
                    pref_dir = None
                    if args.res_prefix:
                        pref_dir = os.path.join(args.out, 'assets', args.res_prefix.rstrip('/'), 'meshes')
                    for i, sp in split.items():
                        if pref_dir and os.path.abspath(pref_dir) != os.path.abspath(base_dir):
                            os.makedirs(pref_dir, exist_ok=True)
                            sp2 = os.path.join(pref_dir, os.path.basename(sp))
                            if not os.path.exists(sp2) or open(sp2, 'rb').read() != open(sp, 'rb').read():
                                with open(sp2, 'w', encoding='utf-8', errors='replace') as f:
                                    f.write(open(sp, encoding='utf-8', errors='replace').read())
                            imp2 = sp2 + '.import'
                            if os.path.exists(imp2):
                                os.remove(imp2)
                            sp = sp2
                        SUB_MESH_PATHS['%s__s%d' % (name, i)] = RESP + 'meshes/' + os.path.basename(sp)
                    print('  [submesh-split] %s → %d 段' % (name, len(split)))
            except Exception as e:
                print('  [submesh-split FAIL] %s: %r' % (name, e))
        # 形态键网格 → blendshapes.json (clip17 链甩鞭动画数据源)
        if obj is not None and name not in BS_FILES:
            bsf = export_blend_shapes(name, obj, args.out)
            if bsf:
                BS_FILES[name] = bsf
        return res

    def ensure_tex(mi, which=1):
        tn, to = (mi.tex_name, mi.tex_obj) if which == 1 else (mi.tex_name2, mi.tex_obj2)
        if not tn:
            return None
        safe = win_safe(tn)
        p = os.path.join(args.out, 'assets', 'textures', safe + '.png')
        if not os.path.exists(p):
            os.makedirs(os.path.dirname(p), exist_ok=True)
            img = None
            try:
                img = getattr(to.read(), 'image', None)
            except Exception:
                img = None
            if img is not None:
                try:
                    img.save(p + '.tmp', format='PNG')  # PIL 按扩展名判格式, .tmp 需显式
                except Exception:
                    pass
            if not os.path.exists(p + '.tmp'):
                src = os.path.join(a.TEXDIR, tn + '.png')
                if os.path.exists(src):
                    import shutil
                    shutil.copy(src, p + '.tmp')
            if os.path.exists(p + '.tmp'):
                p = dedupe_file(p, p + '.tmp', args.arena)
            else:
                print('  [贴图导出失败] %s' % tn)
                return None
        res = RESP + 'textures/' + os.path.basename(p)
        tex_paths[tn] = res
        return res

    # 收集用到的材质/粒子, 先落地贴图网格
    all_nodes = []
    SUB_MESH_PATHS = {}   # '<mesh名>__s{i}' → res://assets/meshes/<...>__s{i}.obj (2026-08-26 F 映射)

    def collect(tpid, parent_tpid=None):
        td = a.TF[tpid]
        gopid = td.get('m_GameObject', {}).get('m_PathID')
        g = a.go(gopid)
        nm = str(g.get('m_Name', 'GO%d' % gopid))
        # m_IsActive=0: 原版禁用 (如黑军团 CrosshairLine 3D/Cache Stealth) → 整树跳过
        if g.get('m_IsActive', 1) not in (1, True, None):
            return
        # layer 5=UI 世界 (UI Camera cullingMask=32 专属层): 能量积累/Glow 等是 UI 世界粒子
        # (黑军团实测: Energy/Glow/Accumulated 全在 layer5, 世界 x∈[0,80] 战场区外),
        # 不属于 BoardCamera 3D 战场层 → 跳 (2026-08-22 二十轮, 机制=layer 而非名字排除)
        if int(g.get('m_Layer', 0) or 0) == 5:
            return
        for sk in skips:
            if sk in nm:
                return
        kind = 'container'
        payload = None
        mesh_name, mesh_obj = a.mesh_of(gopid)
        if mesh_name:
            mats = a.mats_of(gopid)
            if not mats:
                kind = 'skip-mesh'  # 无材质 = 碰撞/逻辑网格
            else:
                kind = 'mesh'
                for mi in mats:
                    if mi.tex_name and mi.tex_name not in tex_paths:
                        ensure_tex(mi)
                    if mi.tex_name2 and mi.tex_name2 not in tex_paths:
                        ensure_tex(mi, 2)
                mesh_res = ensure_mesh(mesh_name, mesh_obj)
                if mesh_res is None:
                    return  # 导出失败 → 跳过该网格节点及子树
                # 记录该网格 UV 三角阴影 OBJ 路径 → 贴图 alpha 修复 (三角光栅=内容精确)
                _op = mesh_res.replace('res://', args.out + '/')
                for mi in mats:
                    if mi.tex_name:
                        UV_RECTS.setdefault(mi.tex_name, []).append(uv_rect_of_obj(_op))
                        UV_OBJS.setdefault(mi.tex_name, []).append(_op)
                payload = mesh_name
        elif a.has_ps(gopid):
            ps, render_mode, mref, mats = a.ps_of(gopid)
            if ps is not None:
                kind = 'particle'
                for mi in mats:
                    if mi.tex_name and mi.tex_name not in tex_paths:
                        ensure_tex(mi)
                        if mi.tex_name not in tex_paths:
                            print('  [粒子贴图缺失] %s' % mi.tex_name)
                    if mi.tex_name and mi.tex_name in tex_paths and smoke_lit(mi):
                        # 白版烟贴图 (RGB→白保留 alpha): 原贴图灰 0.2×暗棕 albedo≈黑不可见
                        fp = res_to_fs(args.out, tex_paths[mi.tex_name])
                        wp = os.path.join(os.path.dirname(fp), 'white_' + os.path.basename(fp))
                        make_white_tex(fp, wp)
                        if os.path.exists(wp):
                            white_paths[mi.tex_name] = RESP + 'textures/' + os.path.basename(wp)
                payload = (ps, render_mode, mref, mats)
                # renderMode=4 (Mesh): 粒子用网格 draw pass (Light Shaft Plane1x1 等)
                if render_mode == 4 and mref and mref.get('m_PathID'):
                    mo = a.b.read_obj(mref, 'Mesh')
                    if mo is not None:
                        mn = a.b.obj_name(mo) or 'psmesh%d' % mref.get('m_PathID')
                        mesh_res = ensure_mesh(mn, mo)
                        if mesh_res is None:
                            return  # draw mesh 导出失败 → 跳过该粒子
                        payload = (ps, render_mode, (mn, mesh_res), mats)
                        print('  [粒子 draw mesh] %s' % mn)
        elif a.has_comp(gopid, a.CAM):
            kind = 'camera'
        elif a.has_comp(gopid, a.LIG):
            kind = 'light'
        elif a.has_comp(gopid, a.LNR):
            # 2026-08-26 全盘对账 E-1: LineRenderer (Smoke Column×6 烟柱/CrosshairLine) → ribbon mesh
            kind = 'line'
            payload = next((a.LNR[c] for c in a.go_comps[gopid] if c in a.LNR), None)
        all_nodes.append((kind, tpid, gopid, nm, payload, parent_tpid))
        for c in a.children_of(tpid):
            collect(c, tpid)

    for t in roots:
        collect(t)

    counts = {}
    for k, *_ in all_nodes:
        counts[k] = counts.get(k, 0) + 1
    print('节点统计:', counts, '| 网格 %d 贴图 %d' % (len(mesh_paths), len(tex_paths)))

    if args.fmt == 'gltf':
        emit_gltf(args, a, all_nodes, mesh_paths, tex_paths, white_paths)
        nfix = repair_uv_alpha(args, tex_paths)
        if args.pad_black:
            pad_blacken(args, tex_paths)
        if args.pad_black:
            pad_blacken(args, tex_paths)
        if nfix:
            print('✓ alpha 修复: %d 像素 (网格 UV 区内暗内容实体化)' % nfix)
        return 0

    # ---- 生成 .gd ----
    out_gd = os.path.join(args.out, 'scenes', 'unity_arena_%s.gd' % args.arena)
    os.makedirs(os.path.dirname(out_gd), exist_ok=True)
    with open(out_gd, 'w', encoding='utf-8') as f:
        w = GdWriter(f)
        w.line('# 由 unity_scene_to_godot.py 自动生成 — 勿手改, 重跑脚本即可')
        w.line('# 来源: 解包整理/07_场景/%s (原版 Unity 序列化 JSON 说明书)' % args.arena)
        # @tool: 编辑器 3D 视图中直接构建显示 (用户需在 Godot 编辑器打开场景点 3D 标签查看;
        # 无 @tool 时内容仅运行时生成, 编辑器视口为空)
        w.line('@tool')
        w.line('extends Node3D')
        w.line('')
        w.line('## 网格/贴图落地清单 (调试统计用)')
        w.line('const MESH_PATHS: Array[String] = [%s]' % ', '.join(
            repr(mesh_paths[k]) for k in sorted(mesh_paths)))
        w.line('const TEX_PATHS: Array[String] = [%s]' % ', '.join(
            repr(tex_paths[k]) for k in sorted(tex_paths)))
        w.line('')
        # 2026-08-26 用户贴图朝向翻转实锤: 贴图内容水平镜像(图案箭头西南→东南, 位置不变) —
        # 翻转源在贴图→渲染链路 → 加载后水平翻转 = 修复 (与用户对照实验 C 版一致)
        # 注意: ShaderMaterial(uv_scroll/LUT) 不走此函数 (其内部纹理坐标由 shader 处理)
        w.line('var _flip_tex_cache: Dictionary = {}')
        w.line('')
        w.line('func _flip_tex(src: Texture2D) -> Texture2D:')
        w.line('\tif src == null:')
        w.line('\t\treturn null')
        w.line('\tif _flip_tex_cache.has(src):')
        w.line('\t\treturn _flip_tex_cache[src]')
        w.line('\tvar img := src.get_image()')
        w.line('\tif img == null:')
        w.line('\t\treturn src')
        w.line('\timg.flip_x()')
        w.line('\tvar tex := ImageTexture.create_from_image(img)')
        w.line('\t_flip_tex_cache[src] = tex')
        w.line('\treturn tex')
        w.line('')
        w.line('func _ready() -> void:')
        w.line('\t_build()')
        w.line('')
        w.line('func _build() -> void:')
        if args.mirror:
            w.line('\t# --mirror 试验: MirrorX (X 镜像, 2026-08-23 已证弧朝外=错; 仅试验)')
            w.line('\tposition = Vector3(-100, 0, 0)  # 原版世界 x 基准 100 → 场景原点')
        elif args.no_mirror:
            w.line('\t# --no-mirror 试验: 内容直挂 Unity 世界坐标 (2026-08-23 已证弧朝外=错; 仅试验)')
        else:
            w.line('\t# 默认 Z 反射方案 (2026-08-23 定案: 弧线左(右)朝内+太阳右+地面40% 全命中 = Unity 原样; X 镜像两方案均弧朝外)')
        w.line('')
        w.line('\t# Unity(左手系)→Godot(右手系) 手性: 默认 Z 镜像根 scale.z=-1 (不动 x, 非对称形状保持 Unity 原样);')
        w.line('\t# --mirror 时 X 镜像根 (绕相机 x=100 平面); 相机移出镜像根见 camera 分支')
        if args.mirror:
            w.line('\tvar mirror := Node3D.new()')
            w.line("\tmirror.name = 'MirrorX'")
            w.line('\tmirror.position = Vector3(200.0, 0.0, 0.0)')
            w.line('\tmirror.scale = Vector3(-1.0, 1.0, 1.0)')
            w.line('\tself.add_child(mirror)')
        elif args.no_mirror:
            w.line('\tpass  # no_mirror: 无镜像根')
        else:
            w.line('\tvar mirror := Node3D.new()')
            w.line("\tmirror.name = 'MirrorZ'")
            w.line('\tmirror.position = Vector3(0.0, 0.0, 0.0)')
            w.line('\tmirror.scale = Vector3(1.0, 1.0, -1.0)')
            w.line('\tself.add_child(mirror)')
        w.line('')

        mat_line_buf = []  # 材质创建行 (每网格内联)
        used_names = {}
        _mat_n = [0]

        def node_var(gopid):
            return 'n_%d' % gopid

        def parent_ref(pt):
            """父变换节点变量名 (根/父被跳过时挂镜像根 mirror; 相机单独挂 self, 见 camera 分支)"""
            if args.no_mirror:
                return 'self'
            if pt is None:
                return 'mirror'
            for k, tp2, gp2, nm2, py2, pp2 in all_nodes:
                if tp2 == pt:
                    if k == 'skip-mesh':
                        return 'mirror'
                    return node_var(gp2)
            return 'mirror'

        def world_chain(tpid):
            """沿 m_Father 链连乘局部 pos/quat (链上 scale 均为 1) → 场景根系世界变换"""
            chain = []
            cur = tpid
            while cur is not None and cur in a.TF:
                chain.append(a.local_trans(cur))
                cur = a.TF[cur].get('m_Father', {}).get('m_PathID')
            p = (0.0, 0.0, 0.0)
            q = (0.0, 0.0, 0.0, 1.0)
            for lt in reversed(chain):
                lp, lq = lt[0:3], lt[3]
                rp = q_rot_vec(q, lp)
                p = (p[0] + rp[0], p[1] + rp[1], p[2] + rp[2])
                q = q_mul(q, lq)
            return p, q

        def emit_local(tvar, tpid):
            lt = a.local_trans(tpid)
            pos, q, sc = (lt[0], lt[1], lt[2]), lt[3], lt[4]
            w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (tvar, pos[0], pos[1], pos[2]))
            w.line('\t%s.quaternion = Quaternion(%.6f, %.6f, %.6f, %.6f)' % (tvar, q[0], q[1], q[2], q[3]))
            w.line('\t%s.scale = Vector3(%.6f, %.6f, %.6f)' % (tvar, sc[0], sc[1], sc[2]))

        def emit_mat(mi, mv):
            """材质创建行列表 (mv=材质变量名), 无贴图返回 None"""
            if mi.tex_name2 and mi.tex_name2 in tex_paths:
                # 双贴图 (Runes "Unlit UV scroll"): shader 双层 + TIME 滚动 (Vector4_1=(1,1,0.02,0))
                lines = ['var %s := ShaderMaterial.new()' % mv]
                lines.append('%s.shader = load("res://assets/uv_scroll.gdshader")' % mv)
                lines.append('%s.set_shader_parameter("main_tex", load(%r))' % (mv, tex_paths[mi.tex_name]))
                lines.append('%s.set_shader_parameter("secondary_tex", load(%r))' % (mv, tex_paths[mi.tex_name2]))
                v4 = mi.vector4_1 or {}
                sc = (float(v4.get('r', 1.0) or 1.0), float(v4.get('g', 1.0) or 1.0),
                      float(v4.get('b', 0.0) or 0.0), float(v4.get('a', 0.0) or 0.0))
                lines.append('%s.set_shader_parameter("uv_scroll", Vector4(%.3f, %.3f, %.3f, %.3f))' % (mv, *sc))
                lines.append('%s.set_shader_parameter("layers_blend_opacity", 1.00)' % mv)
                # 2026-08-26 删: ShaderMaterial 无 cull_mode (BaseMaterial3D 专属) — b3 'Plane' Texture Baked UV scroll 2
                # (cull=0) 曾致 _ready 报错相机未建=黑屏; 双面由 uv_scroll shader 内 render_mode cull_disabled 处理
                return lines
            if not (mi.tex_name and mi.tex_name in tex_paths):
                return None
            lines = ['var %s := StandardMaterial3D.new()' % mv]
            if args.flip_tex:
                # 2026-08-26 用户贴图朝向翻转实锤: 贴图内容水平镜像(箭头西南→东南), 位置不变
                # → 场景 mesh 贴图加载后水平翻转 = 修复 (粒子/精灵贴图方向性弱, 不翻)
                lines.append('%s.albedo_texture = _flip_tex(load(%r))' % (mv, tex_paths[mi.tex_name]))
            else:
                lines.append('%s.albedo_texture = load(%r)' % (mv, tex_paths[mi.tex_name]))
            if mi.base_color:
                lines.append('%s.albedo_color = ' % mv + col_str(mi.base_color))
            if "_ALPHATEST_ON" in mi.keywords or (mi.alpha_clip is not None and mi.alpha_clip > 0.0 and (mi.surface or 0) == 0):
                # 原版 URP TransparentCutout: 烘焙图集=多层 alpha-cut (裁 padding → 底地板透出)
                # 2026-08-25 坑89③ 构图定案: scissor 是构图必要项 (_Blend=0 → 门控独立于 is_transparent)
                # 2026-08-26 B 映射: cutoff 用 _ClipThreshold/_Cutoff 真实值 (38 个非 0.5 不再硬编码)
                lines.append('%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA_SCISSOR' % mv)
                lines.append('%s.alpha_scissor_threshold = %.4f' % (mv, mi.cutoff if mi.cutoff is not None else 0.5))
            elif mi.is_transparent():
                lines.append('%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mv)
                if mi.zwrite <= 0:
                    lines.append('%s.depth_draw_mode = BaseMaterial3D.DEPTH_DRAW_DISABLED' % mv)
            # 2026-08-26 B 映射表: blend 四元组 (_Surface,_SrcBlend,_DstBlend,_AlphaClip):
            # URP _Blend 0=Alpha 1=Premultiply 2=Additive 3=Multiply — 与 (src,dst) 交叉验证
            if mi.blend == 3 or (mi.dst_blend == 0 and mi.src_blend == 2):
                lines.append('%s.blend_mode = BaseMaterial3D.BLEND_MODE_MUL' % mv)
            elif mi.blend == 1 or (mi.src_blend == 1 and mi.dst_blend == 10):
                lines.append('%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mv)
                lines.append('%s.blend_mode = BaseMaterial3D.BLEND_MODE_PREMULT_ALPHA' % mv)
            elif mi.blend == 2 or (mi.src_blend == 5 and mi.dst_blend == 1):
                lines.append('%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mv)
                lines.append('%s.blend_mode = BaseMaterial3D.BLEND_MODE_ADD' % mv)
            if mi.cull == 0:
                lines.append('%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % mv)
            elif mi.cull == 1:
                lines.append('%s.cull_mode = BaseMaterial3D.CULL_FRONT' % mv)
            if mi.emission_color and '_EMISSION' in mi.keywords:
                # 2026-08-26 B 映射: 补 emission 颜色 (此前只设 enabled+energy=黑发射) + _EmissionMap 贴图
                # 门控=ValidKeywords 含 _EMISSION (Fence/Floor 的 _EmissionColor 在 m_InvalidKeywords=原版不发光)
                lines.append('%s.emission_enabled = true' % mv)
                lines.append('%s.emission = ' % mv + col_str(mi.emission_color))
                lines.append('%s.emission_energy_multiplier = %.3f' % (mv, mi.emission_energy()))
            if mi.tex_scale is not None and mi.tex_offset is not None:
                lines.append('%s.uv1_scale = Vector3(%.4f, %.4f, 1.0)' % (mv, mi.tex_scale[0], mi.tex_scale[1]))
                lines.append('%s.uv1_offset = Vector3(%.4f, %.4f, 0.0)' % (mv, mi.tex_offset[0], mi.tex_offset[1]))
            if mi.queue_offset is not None and abs(mi.queue_offset) > 0.001:
                lines.append('%s.render_priority = %d' % (mv, int(round(mi.queue_offset))))
            if args.unshaded_bake and mi.tex_name and not mi.is_transparent():
                # 坑86b 实验 (2026-08-25): 场景贴图=烘焙产物自带光照, UNSHADED=直出 (对照原版 RT 亮度; 透明 FX 除外);
                # 原版=贴图×URP 后处理(LUT identity+bloom)≈×0.82 → --unshaded-gain 全局压暗
                lines.append('%s.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED' % mv)
                if abs(args.unshaded_gain - 1.0) > 1e-6:
                    lines.append('%s.albedo_color *= %.4f' % (mv, args.unshaded_gain))
            # 2026-08-26 B 映射: roughness=1-_Smoothness (真实值替代硬编码 0.9); 金属/非烘焙材质保留高光
            var_rough = mi.roughness()
            if var_rough is not None:
                lines.append('%s.roughness = %.4f' % (mv, var_rough))
            else:
                lines.append('%s.roughness = 0.9' % mv)
            lit = not (args.unshaded_bake and mi.tex_name and not mi.is_transparent())
            if lit:
                # 金属材质 (_Metallic>0, 21 个: Card 3d WH40K/ShellCasing/Water_Splatter) 保留 PBR
                lines.append('%s.specular_mode = BaseMaterial3D.SPECULAR_SCHLICK_GGX' % mv)
                if mi.metallic is not None and mi.metallic > 0.0:
                    lines.append('%s.metallic = %.4f' % (mv, float(mi.metallic)))
            else:
                lines.append('%s.specular_mode = BaseMaterial3D.SPECULAR_DISABLED' % mv)
            return lines

        bg_color = None  # 3D 相机 (BoardCamera) 的 m_BackGroundColor, 供环境背景用
        BS_DEST = {}   # GO 名 -> 节点变量 (clip17 形态键目标)
        BS_DEST_MESH = {}  # GO 名 -> mesh 名 (形态键网格)
        CAM_VARS = {}  # 相机名 -> 节点变量 (clip16 Camera Intro 目标)
        CAM_PITCH = {}  # 相机名 -> LensShift pitch 补偿角(度) (clip16 intro 终点用, 2026-08-23)
        curves17c = clip_curves(os.path.join(SCENE_ROOT, args.arena), '17')
        for kind, tpid, gopid, nm, payload, parent_tpid in all_nodes:
            nv = node_var(gopid)
            cam_added = False
            if kind == 'skip-mesh':
                continue
            if kind == 'mesh':
                mats = a.mats_of(gopid)
                mi = mats[0] if mats else None
                _mat_n[0] += 1
                mv = 'm_%d' % _mat_n[0]
                ml = emit_mat(mi, mv) if mi is not None else None
                if ml is None:
                    w.line('\t# 跳过不可见网格: %s' % nm)
                    continue
                if payload in BS_FILES:
                    # 形态键网格 (黑军团 Chain 甩鞭): GLTF (morph targets+weights 动画, Godot 原生导入)
                    bsdata = json.load(open(BS_FILES[payload], encoding='utf-8'))
                    BS_DEST[nm] = nv
                    BS_DEST_MESH[nm] = payload
                    _tex_name = win_safe(mi.tex_name) if mi and mi.tex_name else ''
                    rel = '../textures/%s.png' % _tex_name if _tex_name else ''
                    anim = None
                    if curves17c and curves17c['curves']:
                        anim = {'curves': [c for c in curves17c['curves'] if c['node'] == nm],
                                'length': 15.98, 'name': 'SceneAnim'}
                    gltf_p = os.path.join(args.out, 'assets', 'meshes', win_safe(payload) + '.gltf')
                    export_morph_gltf(gltf_p, mesh_paths[payload].replace('res://', args.out + '/'), bsdata, rel,
                                      alpha_blend=mi.is_transparent() if mi else True,
                                      double_sided=a.has_smr(gopid), anim=anim)
                    gltf_res = RESP + 'meshes/' + os.path.basename(gltf_p)
                    w.line('\tvar %s: Node3D' % nv)
                    w.line('\t%s = load(%r).instantiate()' % (nv, gltf_res))
                    w.line('\t%s.name = %r' % (nv, nm))
                    w.line('\tvar _ap%s: AnimationPlayer = %s.find_child("AnimationPlayer", true, false)' % (gopid, nv))
                    w.line('\tif _ap%s: _ap%s.autoplay = "SceneAnim"' % (gopid, gopid))
                else:
                    # 2026-08-26 F 映射 firstSubMesh: 共享多 submesh 网格渲染器按 StaticBatchInfo 选段
                    mesh_res = mesh_paths[payload]
                    sbi = a.renderer_sbi(gopid)
                    if sbi:
                        fsm = int(sbi.get('firstSubMesh', 0) or 0)
                        if fsm > 0 or int(sbi.get('subMeshCount', 0) or 0) > 1:
                            key = '%s__s%d' % (payload, fsm)
                            if key in SUB_MESH_PATHS:
                                mesh_res = SUB_MESH_PATHS[key]
                    w.line('\tvar %s := MeshInstance3D.new()' % nv)
                    w.line('\t%s.name = %r' % (nv, nm))
                    w.line('\t%s.mesh = load(%r)' % (nv, mesh_res))
                    for l in ml:
                        w.line('\t' + l)
                    # SkinnedMeshRenderer 薄片 (黑军团 Chain 链平面): 镜像 scale.x=-1 + yaw180 背面朝相机,
                    # cull_back 会被剔除 (原版单面可见) → SMR 网格强制双面
                    if a.has_smr(gopid):
                        w.line('\t%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % mv)
                    w.line('\t%s.material_override = %s' % (nv, mv))
                emit_local(nv, tpid)
                w.line('\t%s.add_child(%s)' % (parent_ref(parent_tpid), nv))
                w.line('')
                continue
            elif kind == 'particle':
                emit_particle_gd(w, a, nv, tpid, nm, payload, tex_paths, parent_ref(parent_tpid), white_paths)
                continue
            elif kind == 'line':
                # 2026-08-26 全盘对账 E-1: LineRenderer → ribbon mesh (Smoke Column×6 烟柱 z=0 局部折线,
                # widthMultiplier×widthCurve(归一); 面向相机的 ±Z 纸带; CrosshairLine 同类)
                lnr = payload or {}
                params = lnr.get('m_Parameters', {}) or {}
                wm = float(params.get('widthMultiplier', 1.0) or 1.0)
                wkeys = ((params.get('widthCurve', {}) or {}).get('m_Curve', [])) or []
                pts = lnr.get('m_Positions', []) or []
                if not pts:
                    continue
                n_pts = len(pts)
                seg = [0.0] * n_pts
                total = 0.0
                for i in range(1, n_pts):
                    dx = float(pts[i]['x']) - float(pts[i - 1]['x'])
                    dy = float(pts[i]['y']) - float(pts[i - 1]['y'])
                    dz = float(pts[i]['z']) - float(pts[i - 1]['z'])
                    total += (dx * dx + dy * dy + dz * dz) ** 0.5
                    seg[i] = total
                tw = max(total, 1e-6)

                def lw_at(tn):
                    if not wkeys:
                        return wm
                    if tn <= wkeys[0].get('time', 0.0):
                        v = wkeys[0].get('value', 1.0)
                    elif tn >= wkeys[-1].get('time', 1.0):
                        v = wkeys[-1].get('value', 1.0)
                    else:
                        v = wkeys[-1].get('value', 1.0)
                        for i in range(len(wkeys) - 1):
                            t0 = float(wkeys[i].get('time', 0.0) or 0.0)
                            v0 = float(wkeys[i].get('value', 1.0) or 1.0)
                            t1 = float(wkeys[i + 1].get('time', 1.0) or 1.0)
                            v1 = float(wkeys[i + 1].get('value', 1.0) or 1.0)
                            if t0 <= tn <= t1:
                                v = v0 + (v1 - v0) * (tn - t0) / (t1 - t0) if t1 > t0 else v1
                                break
                    return max(0.0, min(1.0, float(v))) * wm

                w.line('\tvar %s := MeshInstance3D.new()' % nv)
                w.line('\t%s.name = %r' % (nv, nm))
                imv = w.next_id('im')
                w.line('\tvar %s := ImmediateMesh.new()' % imv)
                w.line('\t%s.surface_begin(Mesh.PRIMITIVE_TRIANGLES)' % imv)
                for i in range(n_pts - 1):
                    p0 = pts[i]
                    p1 = pts[i + 1]
                    w0 = lw_at(seg[i] / tw) / 2.0
                    w1 = lw_at(seg[i + 1] / tw) / 2.0
                    x0, y0, z0 = float(p0['x']), float(p0['y']), float(p0['z'])
                    x1, y1, z1 = float(p1['x']), float(p1['y']), float(p1['z'])
                    # 两个三角 (0-,0+,1+) (0-,1+,1-); 法线 ±z
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x0, y0, z0 - w0))
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x0, y0, z0 + w0))
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x1, y1, z1 + w1))
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x0, y0, z0 - w0))
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x1, y1, z1 + w1))
                    w.line('\t%s.surface_add_vertex(Vector3(%.4f, %.4f, %.4f))' % (imv, x1, y1, z1 - w1))
                w.line('\t%s.surface_end()' % imv)
                w.line('\t%s.mesh = %s' % (nv, imv))
                mats = a.mats_of(gopid)
                mi = mats[0] if mats else None
                if mi is not None and mi.tex_name and mi.tex_name not in tex_paths:
                    ensure_tex(mi)
                nv2 = w.next_id('m')
                ml = emit_mat(mi, nv2) if mi is not None else None
                if ml:
                    for l in ml:
                        w.line('\t' + l)
                    w.line('\t%s.material_override = %s' % (nv, nv2))
                else:
                    # 2026-08-26: 烟柱材质贴图在解包缺失 (别 bundle) → Smoke5.png 近似 (半透明灰烟)
                    w.line('\tvar %s := StandardMaterial3D.new()' % nv2)
                    w.line('\t%s.albedo_texture = load("res://assets/textures/Smoke5.png")' % nv2)
                    w.line('\t%s.albedo_color = Color(0.9, 0.9, 0.92, 0.55)' % nv2)
                    w.line('\t%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % nv2)
                    w.line('\t%s.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED' % nv2)
                    w.line('\t%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % nv2)
                    w.line('\t%s.material_override = %s' % (nv, nv2))
                emit_local(nv, tpid)
                w.line('\t%s.add_child(%s)' % (parent_ref(parent_tpid), nv))
                w.line('')
                continue
            elif kind == 'camera':
                cd = next(a.CAM[c] for c in a.go_comps[gopid] if c in a.CAM)
                if not cd.get('m_Enabled', 1):
                    print('  [跳过禁用相机] %s' % nm)
                    continue  # reflection camera (原版 enabled=0) 不生成 — 否则先入树成 current 抢渲染
                w.line('\tvar %s := Camera3D.new()' % nv)
                w.line('\t%s.name = %r' % (nv, nm))
                CAM_VARS[nm] = nv
                cd = next(a.CAM[c] for c in a.go_comps[gopid] if c in a.CAM)
                # 相机必须移出镜像根 (mirror 是反射, 相机带反射基 = 画面再次镜像);
                # 位置 = 镜像世界坐标 (100 - p_u.x), 旋转 = conjX(q_u) ⊗ Y180
                # (推导: R_g = M·R_u·D, M=reflect_x, D=RotX(180°) 手性修正)
                wp, wq = world_chain(tpid)
                if args.mirror:
                    # MirrorX: 位置 = 镜像世界坐标 (200 - p_u.x), 旋转 = conjX(q_u) ⊗ Y180
                    w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (nv, 200.0 - wp[0], wp[1], wp[2]))
                    # conjX(q) = (x, -y, -z, w)  (M·R·M 反射共轭)
                    qq = q_mul((wq[0], -wq[1], -wq[2], wq[3]), (0.0, 1.0, 0.0, 0.0))
                elif args.no_mirror:
                    # --no-mirror: 内容直挂 Unity 世界系, 相机仅 Y180 左乘 (15轮前方案)
                    w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (nv, wp[0], wp[1], wp[2]))
                    qq = q_mul((0.0, 1.0, 0.0, 0.0), wq)
                else:
                    # 默认 Z 反射: 相机移出 z 镜像根, 位置 z 取反 + 四元数 Z 反射共轭 (无 Y180, 2026-08-23 定案)
                    w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (nv, wp[0], wp[1], -wp[2]))
                    qq = (-wq[0], -wq[1], wq[2], wq[3])
                # Cinemachine m_Lens.LensShift.y → 相机 pitch 补偿 (2026-08-23 修正:
                # 旧实现 frustum_offset 仅 PROJECTION_FRUSTUM 生效 → 默认 PERSPECTIVE 下 LensShift 从未生效
                # = 画面 gate 偏移缺失 → 地平面偏低/"近了/看不全" 根因之一。
                # LensShift=视口归一化 gate 偏移 (y-0.205=gate 下移) → 等效相机俯角 atan(y*tan(fov/2))
                # 2026-08-25 夜投影校准 (坑89): 地面地标序列 0°→484 / +5.02°→592(目标 586) 单调 →
                # 真值 = **+4.74° (上仰)**; 08-23 原公式符号即正确 (lsy=-0.205 → +5.02);
                # 上午"翻转"两次均为误 (测量受光照态干扰) — 勿再动符号
                _fov_v = cd.get('field of view') or 46.397
                lsy = cd.get('m_LensShift', {}).get('y') or 0.0
                if lsy and not args.no_lensshift:
                    import math as _m
                    if args.lensshift_deg is not None:
                        # 矩阵定案用: 直接指定俯角度数 (覆盖公式; 负=俯视, 正=上仰)
                        _theta = _m.radians(args.lensshift_deg)
                    else:
                        _theta = _m.atan(lsy * _m.tan(_m.radians(_fov_v) / 2.0))
                    _rt = _m.cos(_theta / 2.0)
                    _rs = _m.sin(_theta / 2.0)
                    # 绕本地 X 旋转后乘
                    qq = q_mul(qq, (_rs, 0.0, 0.0, _rt))
                    CAM_PITCH[nm] = _m.degrees(_theta)
                    print('  [camera] LensShift y=%.3f → pitch %+.2f°' % (lsy, CAM_PITCH[nm]))
                w.line('\t%s.quaternion = Quaternion(%.6f, %.6f, %.6f, %.6f)' % (nv, qq[0], qq[1], qq[2], qq[3]))
                w.line('\t%s.scale = Vector3(%.6f, %.6f, %.6f)' % (nv, 1.0, 1.0, 1.0))
                fov = _fov_v
                if fov:
                    w.line('\t%s.fov = %.3f' % (nv, float(fov)))
                w.line('\t%s.near = %.4f' % (nv, float(cd.get('near clip plane', 0.3))))
                w.line('\t%s.far = %.4f' % (nv, float(cd.get('far clip plane', 300.0))))
                if bg_color is None and 'UI' not in nm:
                    bc = cd.get('m_BackGroundColor') or {}
                    bg_color = (float(bc.get('r', 0.0288)), float(bc.get('g', 0.0288)), float(bc.get('b', 0.0294)))
                w.line('\tself.add_child(%s)  # 相机挂在镜像根之外' % nv)
                w.line('')
                cam_added = True
            elif kind == 'light':
                w.line('\tvar %s := DirectionalLight3D.new()' % nv)
                w.line('\t%s.name = %r' % (nv, nm))
                ld = next(a.LIG[c] for c in a.go_comps[gopid] if c in a.LIG)
                lt = a.local_trans(tpid)
                pos, q, sc = (lt[0], lt[1], lt[2]), lt[3], lt[4]
                w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (nv, pos[0], pos[1], pos[2]))
                # 坑85 (2026-08-25): 曾断言旧公式 B = R ⊗ Rx(-90) "结果=光向上(照天)" 并把 q_light_godot 定为正解 —
                # 2026-08-25 重生成 A/B 实测推翻: 旧公式=已校准 98.1/97.5 vs 原版 RT 95.5 ✓;
                # q_light_godot 新版=全场 35.0 (背光). 恢复旧公式, 勿再改 (校准基准=Unity参照管线 RT 亮度)
                qg = q_mul(q, (-math.sqrt(0.5), 0.0, 0.0, math.sqrt(0.5)))
                w.line('\t%s.quaternion = Quaternion(%.6f, %.6f, %.6f, %.6f)' % (nv, qg[0], qg[1], qg[2], qg[3]))
                # 按说明书: Light m_Color(1,1,1) 白光 + m_Intensity 1.0 直通 (此前暖色修正的依据
                # "原版白光→含 LUT 暖调" 已被推翻: ColorLookup=LUT Normal identity → 无暖调, 颜色零自造)
                lc = ld.get('m_Color', {}) or {}
                w.line('\t%s.light_color = Color(%.4f, %.4f, %.4f)' % (nv,
                    float(lc.get('r', 1.0) or 1.0),
                    float(lc.get('g', 1.0) or 1.0),
                    float(lc.get('b', 1.0) or 1.0)))
                # (黑军团验算: Unity 光强直通=过曝 2-3 倍, energy 作引擎物理等效校准, 颜色保持说明书值)
                w.line('\t%s.light_energy = %.3f' % (nv, float(ld.get('m_Intensity', 1.0) or 1.0) * args.light_energy))
                sh = ld.get('m_Shadows', {})
                if sh.get('m_Type', 0) != 0 and ld.get('m_Type') == 1:
                    w.line('\t%s.shadow_enabled = false  # RT 实证: 原版烘焙无实时投影 (2026-08-25)' % nv)
            else:  # container
                w.line('\tvar %s := Node3D.new()' % nv)
                w.line('\t%s.name = %r' % (nv, nm))
                emit_local(nv, tpid)
                w.line('\t%s.add_child(%s)' % (parent_ref(parent_tpid), nv))
                w.line('')
                continue
            if cam_added:
                continue
            w.line('\t%s.add_child(%s)' % (parent_ref(parent_tpid), nv))
            w.line('')

        # 环境 (按说明书 RenderSettings: m_AmbientMode=3 Trilight 三色 + m_AmbientIntensity 原值;
        # URP Volume 无 Tonemapping 组件 → Linear 直出; Bloom(1.15/5.0) + Vignette(0.297/0.2) + ColorLookup(LUT))
        for r in a.REN.values():
            w.line('\tvar env := WorldEnvironment.new()')
            w.line('\tvar e := Environment.new()')
            # 环境间接光 = RenderSettings m_AmbientProbe SH DC 项 (sh[0..2]=平均间接光 RGB;
            # 黑军团 (0.289,0.183,-0.080) 暖橙! Trilight 三色仅天空盒配置, 单色平均会偏蓝 —
            # 说明书完整读取: 环境光按 SH probe, 不是 Trilight 平均)。B<0 夹 0。
            sk, eq, gd = (r.get('m_AmbientSkyColor') or {}), (r.get('m_AmbientEquatorColor') or {}), (r.get('m_AmbientGroundColor') or {})
            probe = r.get('m_AmbientProbe') or {}
            def _sh(i):
                v = probe.get('sh[ %d]' % i) or probe.get('sh[%d]' % i)
                return float(v) if v is not None else 1.0
            sh_c = (_sh(0), _sh(1), _sh(2))
            w.line('\te.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR')
            w.line('\te.ambient_light_color = Color(%.4f, %.4f, %.4f, 1.0000)' %
                   tuple(max(0.0, c) for c in sh_c))
            # SH DC 已含强度 → energy 直通; --ambient-energy 做引擎等效校准
            w.line('\te.ambient_light_energy = %.3f' % args.ambient_energy)
            if r.get('m_Fog'):
                w.line('\te.fog_enabled = true')
                w.line('\te.fog_light_color = ' + col_str(r.get('m_FogColor', {})))
                w.line('\te.fog_density = %.5f' % float(r.get('m_FogDensity', 0.01)))
            # URP: Volume 无 Tonemapping 组件 → m_RenderPostProcessing 直出 = Linear (勿用 Filmic/ACES)
            w.line('\te.tonemap_mode = Environment.TONE_MAPPER_LINEAR')
            w.line('\te.glow_enabled = true')
            w.line('\te.glow_intensity = 5.0')
            w.line('\te.glow_hdr_threshold = 1.15')
            # URP Bloom skipIterations 6+maxIterations 6 ≈ 大扩散层; Godot 4.7 glow_levels/N 层强度
            # (默认 L2/L3 近距叠加会泛白) → 只开最糊两层 L6+L7 (大范围柔光, 发光源内核仍亮)
            w.line('\te.set("glow_levels/6", 1.0)')
            w.line('\te.set("glow_levels/7", 1.0)')
            w.line('\te.set("glow_levels/1", 0.0)')
            w.line('\te.set("glow_levels/2", 0.0)')
            w.line('\te.set("glow_levels/3", 0.0)')
            w.line('\te.set("glow_levels/4", 0.0)')
            w.line('\te.set("glow_levels/5", 0.0)')
            w.line('\te.background_mode = Environment.BG_COLOR')
            bg = bg_color or (0.0288, 0.0288, 0.0294)
            w.line('\te.background_color = Color(%.4f, %.4f, %.4f, 1)' % bg)
            w.line('\tenv.environment = e')
            w.line('\tenv.name = "Env"')
            w.line('\tadd_child(env)')
            # 原版 Battle Arena 4 PostProcessing: ColorLookup(场景引用 LUT, contribution 1.0) + Vignette(0.297)
            # layer=-1: LUT 只作用于 3D 画面, 不遮 HUD (主项目 HUD 在默认 canvas layer 0 之上)
            w.line('\tvar pp := CanvasLayer.new()')
            w.line('\tpp.layer = -1')
            w.line('\tvar cr := ColorRect.new()')
            w.line('\tcr.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)')
            w.line('\tcr.mouse_filter = Control.MOUSE_FILTER_IGNORE')
            w.line('\tvar pm := ShaderMaterial.new()')
            w.line('\tpm.shader = load("res://assets/lut_vignette.gdshader")')
            lut_res = None
            lref = a.lut_ref_val
            if lref:
                try:
                    lobj = a.b.read_obj(lref, 'Texture2D')
                    if lobj is not None:
                        limg = getattr(lobj.read(), 'image', None)
                        if limg is not None:
                            ln = a.b.obj_name(lobj) or 'lut_%s' % lref.get('m_PathID')
                            lp = os.path.join(args.out, 'assets', 'textures', win_safe(ln) + '.png')
                            if not os.path.exists(lp):
                                os.makedirs(os.path.dirname(lp), exist_ok=True)
                                limg.save(lp + '.tmp', format='PNG')
                                lp = dedupe_file(lp, lp + '.tmp', args.arena)
                            lut_res = RESP + 'textures/' + os.path.basename(lp)
                except Exception:
                    lut_res = None
            if lut_res is None and os.path.exists(os.path.join(args.out, 'assets', 'textures', 'LUT Normal.png')):
                lut_res = RESP + 'textures/LUT Normal.png'  # 回退: LUT Blender 共享默认(黑军团)
            if lut_res:
                w.line('\tpm.set_shader_parameter("lut", load(%r))' % lut_res)
                w.line('\tpm.set_shader_parameter("lut_contribution", 1.0)')
            w.line('\tpm.set_shader_parameter("vignette_intensity", 0.297)')
            w.line('\tpm.set_shader_parameter("vignette_smoothness", 0.2)')
            w.line('\tcr.material = pm')
            w.line('\tpp.add_child(cr)')
            w.line('\tadd_child(pp)')
            break

        # ---- Camera Intro 接入 (clip16 Once 2.5s 相机入场; 2026-08-26 E 映射: 全键采样+三轴插值) ----
        c16 = clip16_curves(os.path.join(SCENE_ROOT, args.arena))
        cam_nv = CAM_VARS.get('BoardCamera') or CAM_VARS.get('boardcamera')
        if c16 and c16.get('pos') and cam_nv:
            w.line('')
            pos_s, rot_s = clip16_samples(c16)
            gd_pos = []
            for (px, py, pz) in pos_s:
                if args.mirror:
                    gd_pos.append((100.0 - px, py, pz))
                elif args.no_mirror:
                    gd_pos.append((100.0 + px, py, pz))
                else:
                    gd_pos.append((100.0 + px, py, -pz))
            w.line('	# ==== Camera Intro (原版 2.5s Once: 镜头推镜+三轴 Euler, 25 采样点=全键保形) ====')
            w.line('	_tgt_cam = %s' % cam_nv)
            w.line('')
            w.line('var _tgt_cam: Camera3D')
            w.line('var _anim_t := 0.0')
            w.line('var _pos_s: Array = [%s]' % ', '.join('Vector3(%.4f, %.4f, %.4f)' % p for p in gd_pos))
            w.line('var _rot_s: Array = [%s]' % ', '.join('Vector3(%.4f, %.4f, %.4f)' % p for p in rot_s))
            w.line('')
            w.line('func _process(delta: float) -> void:')
            w.line('	_anim_t += delta')
            w.line('	if _tgt_cam:')
            w.line('		var _it := clampf(_anim_t / 2.5, 0.0, 1.0)')
            w.line('		var _fi := _it * float(_pos_s.size() - 1)')
            w.line('		var _a := int(_fi)')
            w.line('		var _b := mini(_a + 1, _pos_s.size() - 1)')
            w.line('		var _f := _fi - float(_a)')
            w.line('		_tgt_cam.position = (_pos_s[_a] as Vector3).lerp(_pos_s[_b] as Vector3, _f)')
            w.line('		_tgt_cam.rotation_degrees = (_rot_s[_a] as Vector3).lerp(_rot_s[_b] as Vector3, _f)')
            w.line('		if _it >= 1.0:')
            w.line('			_tgt_cam = null  # intro 结束: 停止每帧写相机')
    # 包装 tscn (挂脚本)
    out_tscn = os.path.join(args.out, 'scenes', 'unity_arena_%s.tscn' % args.arena)
    with open(out_tscn, 'w', encoding='utf-8') as f:
        f.write('[gd_scene load_steps=2 format=3]\n\n')
        f.write('[ext_resource type="Script" path="res://scenes/unity_arena_%s.gd" id="1"]\n\n' % args.arena)
        f.write('[node name="Arena" type="Node3D"]\n')
        f.write('script = ExtResource("1")\n')

    print('✓ 输出:', out_gd)
    nfix = repair_uv_alpha(args, tex_paths)
    if nfix:
        print('✓ alpha 修复: %d 像素 (网格 UV 区内暗内容实体化)' % nfix)
    return 0


COLOR_LIT_TINT = 'Color(0.0380, 0.0090, 0.0060, 1.0000)'  # 烟底标定色 ≈ 参考图 (55,25,20)


def smoke_lit(mi):
    """烟/蒸汽类: alpha混合 且 材质名含 smoke|steam|wispy (排除 add/glow/ember/fire 发光类)"""
    if mi is None or mi.blend >= 2:
        return False
    n = mi.name or ''
    return bool(re.search(r'(?i)smoke|steam|wispy', n)) and not re.search(r'(?i)additive|glow|ember|fire', n)


def emit_particle_gd(w, a, nv, tpid, nm, payload, tex_paths, parent_expr='self', white_paths=None,
                     world_rel=False):
    """world_rel=True: 定位用 Unity 世界链 (粒子直挂 MirrorZ 包装根, 与 glTF 折入内容同手性)"""
    ps, render_mode, mref, mats = payload
    white_paths = white_paths or {}
    mi = mats[0] if mats else None
    im = ps.get('InitialModule', {}) or {}
    em = ps.get('EmissionModule', {}) or {}
    shp = ps.get('ShapeModule', {}) or {}
    sm = ps.get('SizeModule', {}) or {}
    cm = ps.get('ColorModule', {}) or {}
    vm = ps.get('VelocityModule', {}) or {}
    rm = ps.get('RotationModule', {}) or {}
    # UVModule 翻页分片 (Black Smoke 8x8/Floor Grill 6x5/FirePit 7x7/Smoke 6x5 等)
    uvm = ps.get('UVModule', {}) or {}
    uv_en = bool(uvm.get('enabled'))
    tiles_x = int(uvm.get('tilesX', 1) or 1)
    tiles_y = int(uvm.get('tilesY', 1) or 1)
    uv_fps = float(uvm.get('fps', 30.0) or 30.0)
    # Godot GPUParticles3D 无 rate 属性: 池大小 = 平均存活粒子数 = rate × lifetime
    # (Unity maxNumParticles 是上限, 直接用作池会全池同时存活 → 粒子爆炸)
    rate_over_time = float(
        (em.get('rateOverTime', {}) or {}).get('scalar', 0.0) if isinstance(em.get('rateOverTime', {}), dict) else 0.0)
    _life_min, _life_max = curve_min_max(im.get('startLifetime', {}), 1.0)
    _pool = int(rate_over_time * max(_life_min, _life_max))
    if _pool > 0 or rate_over_time > 0:
        # rate>0 但池<1 (如 0.2x4=0.8): 取整到 1 发粒 — 防子节点已收集但本粒子早退=引用未声明变量 (b2 RocketTrail, 2026-08-26)
        amount = max(1, min(max(_pool, 1), 800))
        _burst_only = False
    else:
        # rate=0 的 burst 类: 用爆发总量, 上限 200 (Skull Eyes 等一次性粒子)
        _burst_total = 0
        for b in (em.get('m_Bursts', []) or []):
            if isinstance(b, dict):
                _burst_total += int(curve_scalar(b.get('countCurve', {}), 10) or 10)
        if _burst_total <= 0:
            return  # rate=0 且无 burst: 原版不发粒子 (无需生成)
        amount = max(1, min(_burst_total, 200))
        _burst_only = True
    life_min, life_max = curve_min_max(im.get('startLifetime', {}), 1.0)
    lifetime = max(life_min, life_max)
    speed_min, speed_max = curve_min_max(im.get('startSpeed', {}), 0.0)
    size_min, size_max = curve_min_max(im.get('startSize', {}), 1.0)
    size_keys = curve_keys(sm.get('curve', {})) if sm.get('enabled') else []
    color_keys = grad_keys(cm.get('gradient', {})) if cm.get('enabled') else []
    sc0 = im.get('startColor', {}) or {}
    cmin, cmax = sc0.get('minColor') or {}, sc0.get('maxColor') or {}
    stype = shp.get('type', 0)
    radius = curve_scalar(shp.get('radius', {}), 1.0)
    radius_th = float(shp.get('radiusThickness', 1.0) or 1.0)
    sangle = float(shp.get('angle', 0) or 0.0)

    w.line('\tvar %s := GPUParticles3D.new()' % nv)
    w.line('\t%s.name = %r' % (nv, nm))
    lt = a.local_trans(tpid)
    if world_rel:
        wp, wq = world_chain_of(a, tpid)
        lt = (wp[0], wp[1], wp[2], wq, a.local_trans(tpid)[4])
    pos, q, sc = (lt[0], lt[1], lt[2]), lt[3], lt[4]
    w.line('\t%s.position = Vector3(%.4f, %.4f, %.4f)' % (nv, pos[0], pos[1], pos[2]))
    w.line('\t%s.quaternion = Quaternion(%.6f, %.6f, %.6f, %.6f)' % (nv, q[0], q[1], q[2], q[3]))
    w.line('\t%s.scale = Vector3(%.6f, %.6f, %.6f)' % (nv, sc[0], sc[1], sc[2]))
    w.line('\t%s.amount = %d' % (nv, max(amount, 1)))
    w.line('\t%s.lifetime = %.3f' % (nv, max(lifetime, 0.01)))
    # burst 类 (rate=0+bursts): 原版 cycleCount 一次性 → one_shot (Skull Eyes 闪一次)
    w.line('\t%s.one_shot = %s' % (nv, 'true' if (_burst_only or not ps.get('looping', True)) else 'false'))
    w.line('\t%s.emitting = %s' % (nv, 'true' if ps.get('playOnAwake', True) else 'false'))
    # simulationSpeed 直通 (光柱 0.3/毒气 0.4/烟 0.75/气体 1.5 — 此前丢失全按 1.0)
    w.line('\t%s.speed_scale = %.3f' % (nv, float(ps.get('simulationSpeed', 1.0) or 1.0)))
    if ps.get('prewarm', False):
        w.line('\t%s.preprocess = %.2f' % (nv, float(ps.get('lengthInSec', 5.0) or 5.0) *
                                           float(ps.get('simulationSpeed', 1.0) or 1.0)))
    else:
        w.line('\t%s.preprocess = 0.0' % nv)
    pmv = w.next_id('pm')
    w.line('\tvar %s := ParticleProcessMaterial.new()' % pmv)
    # Unity ShapeType: 0 Sphere/1 SphereShell/2 Hemisphere/3 HemisphereShell/4 Cone/5 Box/6 Mesh/
    # 7 Circle/8 CircleEdge/9 SingleSidedEdge/10 MeshRenderer/11 SkinnedMeshRenderer(均按 Box 近似)/
    # 12 BoxShell/13 BoxEdge/14 Donut/15 Rectangle/18 Cylinder(按 Box 近似)
    # Godot 4.7 无 CONE 枚举: 锥形发射源用小球面近似 (粒子沿 direction 喷出, 视觉一致)
    if stype in (2, 3):
        w.line('\t%s.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE_SURFACE' % pmv)
        w.line('\t%s.emission_sphere_radius = %.4f' % (pmv, radius))
    elif stype == 4:
        w.line('\t%s.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE' % pmv)
        w.line('\t%s.emission_sphere_radius = %.4f' % (pmv, min(radius, 0.35)))
    elif stype == 7:
        w.line('\t%s.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_RING' % pmv)
        w.line('\t%s.emission_ring_radius = %.4f' % (pmv, radius))
        w.line('\t%s.emission_ring_inner_radius = %.4f' % (pmv, radius * max(0.0, 1.0 - radius_th)))
    elif stype in (5, 6, 10, 11, 12, 13, 15, 18):
        w.line('\t%s.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_BOX' % pmv)
        scn = shp.get('m_Scale', {}) or shp.get('scale', {}) or {}

        def scv(k, d=1.0):
            v = scn.get(k, {})
            if isinstance(v, dict):
                return curve_scalar(v, d)
            return float(v if v is not None else d)

        ex, ey, ez = scv('x') / 2.0, scv('y') / 2.0, scv('z') / 2.0
        if abs(ex) < 1e-6 and abs(ey) < 1e-6 and abs(ez) < 1e-6:
            ex = ey = ez = max(radius, 0.5) / 2.0  # scale 全 0 时退化为半径球盒
        w.line('\t%s.emission_box_extents = Vector3(%.4f, %.4f, %.4f)' % (pmv, ex, ey, ez))
    else:
        w.line('\t%s.emission_shape = ParticleProcessMaterial.EMISSION_SHAPE_SPHERE' % pmv)
        w.line('\t%s.emission_sphere_radius = %.4f' % (pmv, radius))
    if speed_max > 0:
        w.line('\t%s.initial_velocity_min = %.3f' % (pmv, speed_min))
        w.line('\t%s.initial_velocity_max = %.3f' % (pmv, speed_max))
    if vm.get('enabled'):
        def vof(x):
            if isinstance(x, dict):
                mc = x.get('minMaxCurve', x)
                return curve_scalar(mc, 0.0) if isinstance(mc, dict) else 0.0
            return 0.0
        vx_, vy_, vz_ = vof(vm.get('x')), vof(vm.get('y')), vof(vm.get('z'))
        # Unity velocity-over-lifetime 恒速 (world) → Godot: direction=归一化方向 +
        # initial_velocity=速率 (仅 direction 无初速 → 粒子钉死不动, 原版热浪/绿气恒漂)
        vlen = math.sqrt(vx_ * vx_ + vy_ * vy_ + vz_ * vz_)
        if vlen > 0.001:
            w.line('\t%s.direction = Vector3(%.4f, %.4f, %.4f)' % (pmv, vx_ / vlen, vy_ / vlen, vz_ / vlen))
            w.line('\t%s.initial_velocity_min = %.3f' % (pmv, vlen))
            w.line('\t%s.initial_velocity_max = %.3f' % (pmv, vlen))
    else:
        w.line('\t%s.direction = Vector3(0, 1, 0)' % pmv)
    g = curve_scalar(im.get('gravityModifier', {}), 0.0)
    w.line('\t%s.gravity = Vector3(0, %.3f, 0)' % (pmv, -9.81 * g))
    if uv_en and tiles_x > 1 and tiles_y > 1:
        w.line('\t%s.anim_speed = Vector2(%.1f, %.1f)' % (pmv, uv_fps, uv_fps))
    # Godot 粒子 scale 直接乘 draw-pass 尺寸: quad.size=1, scale=Unity 直径 (startSize)
    w.line('\t%s.scale_min = %.4f' % (pmv, size_min))
    w.line('\t%s.scale_max = %.4f' % (pmv, size_max))
    if size_keys and len(size_keys) >= 2 and abs(size_keys[0][1] - size_keys[-1][1]) > 0.01:
        cv_ = w.next_id('cv')
        w.line('\tvar %s := Curve.new()' % cv_)
        for t, v in size_keys:
            w.line('\t%s.add_point(Vector2(%.4f, %.4f))' % (cv_, t, v))
        ct_ = w.next_id('ct')
        w.line('\tvar %s := CurveTexture.new()' % ct_)
        w.line('\t%s.curve = %s' % (ct_, cv_))
        w.line('\t%s.scale_curve = %s' % (pmv, ct_))
    # startColor 是全局乘子, 与 colorOverLifetime 相乘 (原版两者都生效)
    sc_mode = sc0.get('minMaxState', 0)
    if sc_mode == 2 and cmin and cmax:
        smc = {k: (cmin.get(k, 0.0) + cmax.get(k, 0.0)) / 2.0 for k in ('r', 'g', 'b', 'a')}
        w.line('\t%s.color = %s' % (pmv, col_str(smc)))
    elif cmax:
        w.line('\t%s.color = %s' % (pmv, col_str((cmax if sc_mode in (0, 2) else cmin) or cmax)))
    if color_keys:
        gr_ = w.next_id('gr')
        gt_ = w.next_id('gt')
        w.line('\tvar %s := Gradient.new()' % gr_)
        w.line('\t%s.offsets = PackedFloat32Array([%s])' % (gr_, ', '.join('%.4f' % t for t, _ in color_keys)))
        w.line('\t%s.colors = PackedColorArray([%s])' % (gr_, ', '.join(
            'Color(%.4f, %.4f, %.4f, %.4f)' % c for _, c in color_keys)))
        w.line('\tvar %s := GradientTexture1D.new()' % gt_)
        w.line('\t%s.gradient = %s' % (gt_, gr_))
        w.line('\t%s.color_ramp = %s' % (pmv, gt_))
    if rm.get('enabled'):
        rz = rm.get('curve', rm.get('z', {}))  # 2026-08-26 D映射: separateAxes=false→curve 键(子代理D实锤)
        rv = None
        if isinstance(rz, dict):
            mc = rz.get('minMaxCurve', rz)
            rv = curve_scalar(mc, 0.0) if isinstance(mc, dict) else None
        if rv:
            w.line('\t%s.angular_velocity_min = %.2f' % (pmv, math.degrees(rv)))
            w.line('\t%s.angular_velocity_max = %.2f' % (pmv, math.degrees(rv)))
    w.line('\t%s.process_material = %s' % (nv, pmv))
    # draw pass: renderMode=4 (Mesh) 用导出的网格, 否则 quad (边长=1, scale=直径)
    mat_name = str(mi.name) if mi else ''
    if render_mode == 4 and isinstance(mref, tuple):
        mesh_res = mref[1]
        mmv = w.next_id('pm2')
        w.line('\tvar %s := StandardMaterial3D.new()' % mmv)
        w.line('\t%s.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED' % mmv)
        w.line('\t%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mmv)
        w.line('\t%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % mmv)
        w.line('\t%s.vertex_color_use_as_albedo = true' % mmv)
        if mi and mi.blend >= 2:
            w.line('\t%s.blend_mode = BaseMaterial3D.BLEND_MODE_ADD' % mmv)
        if mi and mi.tex_name and mi.tex_name in tex_paths:
            w.line('\t%s.albedo_texture = load(%r)' % (mmv, tex_paths[mi.tex_name]))
        if mi and (mi.base_color or mi.mat_color):
            w.line('\t%s.albedo_color = %s' % (mmv, col_str_hdr(mi.base_color or mi.mat_color)))
        qdv = w.next_id('qd')
        w.line('\tvar %s := load(%r).duplicate()' % (qdv, mesh_res))
        w.line('\t%s.draw_pass_1 = %s' % (nv, qdv))
        w.line('\t%s.draw_pass_1.surface_set_material(0, %s)' % (nv, mmv))
    else:
        qmv = w.next_id('q')
        w.line('\tvar %s := QuadMesh.new()' % qmv)
        w.line('\t%s.size = Vector2(1.000, 1.000)' % qmv)
        if mi is not None:
            mmv = w.next_id('pm2')
            w.line('\tvar %s := StandardMaterial3D.new()' % mmv)
            w.line('\t%s.billboard_mode = BaseMaterial3D.BILLBOARD_PARTICLES' % mmv)
            # 粒子顶点色(color × color_ramp) 必须显式启用, 否则粒子色/渐变全部不生效
            w.line('\t%s.vertex_color_use_as_albedo = true' % mmv)
            # 烟/蒸汽类材质按原版被环境光染暗棕 (参考图实测 ~55,25,20), 用 unshaded+暗色近似
            if 'Heat Distortion' in mat_name:
                # 原版=屏幕空间折射材质 (Godot StandardMaterial 无法还原) → 用 mask 贴图低透明度近似, 防白块
                if 'Glow' in tex_paths:
                    w.line('\t%s.albedo_texture = load(%r)' % (mmv, tex_paths['Glow']))
                w.line('\t%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mmv)
                w.line('\t%s.albedo_color = Color(1.0, 0.85, 0.7, 0.16)' % mmv)
                w.line('\t%s.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED' % mmv)
                w.line('\t%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % mmv)
                w.line('\t%s.blend_mode = BaseMaterial3D.BLEND_MODE_ADD' % mmv)
            else:
                use_lit = smoke_lit(mi)
                # 说明书: 烟/气粒子材质为 Unlit 粒子着色器, 颜色=startColor×colorMod×贴图RGB(0.2灰)
                # → unshaded + 原版贴图 + albedo 白 (受光会洗色; 暗棕 albedo 会双重乘黑)
                if mi.tex_name and mi.tex_name in tex_paths:
                    w.line('\t%s.albedo_texture = load(%r)' % (mmv, tex_paths[mi.tex_name]))
                w.line('\t%s.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED' % mmv)
                w.line('\t%s.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA' % mmv)
                w.line('\t%s.cull_mode = BaseMaterial3D.CULL_DISABLED' % mmv)
                if mi.blend >= 2:
                    w.line('\t%s.blend_mode = BaseMaterial3D.BLEND_MODE_ADD' % mmv)
                if mi.base_color or mi.mat_color:
                    w.line('\t%s.albedo_color = %s' % (mmv, col_str_hdr(mi.base_color or mi.mat_color)))
            # UVModule 翻页分片: 材质分帧 + loop (anim_speed 已在 pmv 设置)
            if uv_en and tiles_x > 1 and tiles_y > 1:
                w.line('\t%s.particles_anim_h_frames = %d' % (mmv, tiles_x))
                w.line('\t%s.particles_anim_v_frames = %d' % (mmv, tiles_y))
                w.line('\t%s.particles_anim_loop = true' % mmv)
            w.line('\t%s.material = %s' % (qmv, mmv))
        w.line('\t%s.draw_pass_1 = %s' % (nv, qmv))
    w.line('\t%s.add_child(%s)' % (parent_expr, nv))
    w.line('')


def flip_obj_vt(fp):
    """Unity OBJ 导出的 vt v 轴是自下而上, Godot 纹理 v=0 在顶部 → 写 1-v"""
    lines = open(fp, encoding='utf-8', errors='replace').readlines()
    out = []
    n = 0
    for l in lines:
        if l.startswith('vt '):
            p = l.split()
            try:
                p[2] = '%.6f' % (1.0 - float(p[2]))
                out.append(' '.join(p) + '\\n')
                n += 1
            except Exception:
                out.append(l)
        else:
            out.append(l)
    if n:
        open(fp, 'w', encoding='utf-8').write(''.join(out))
    return n


def obj_valid(fp):
    try:
        with open(fp, encoding='utf-8', errors='replace') as f:
            head = f.read(1 << 20)
    except Exception:
        return False
    return ('\nv ' in head or head.startswith('v ')) and '\nvn ' in head and '\nvt ' in head


def fix_obj_xmirror(fp):
    """UnityPy MeshExporter 导出 OBJ: 顶点 x 取负 + 面绕序反转, 但 UV 不镜像 (2026-08-26 根因实锤,
    子代理 A 级证据=MeshExporter.py:33/-pos[0]+L54 绕序反) — "Unity 左手→OBJ 右手"不完整转换:
    几何镜像+绕序反≠外观不变 (缺 UV u 镜像), GO 链又全是 RotX(保持 x̂) → 每个网格绕枢轴水平镜像
    (用户"位置不变、图案开口翻转"精确匹配); 还原=顶点 x'=-x + 面引用倒序 + 法线 x 还原,
    与 MirrorZ z 反射组合后=任意 GO 旋转(含旗/炮复合)精确=原版; 修复后无需 --flip-tex"""
    if os.path.exists(fp + '.xfixed'):
        return
    with open(fp, encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    out = []
    for l in lines:
        p = l.strip().split()
        if not p:
            out.append(l)
            continue
        if p[0] == 'v' and len(p) >= 4:
            try:
                out.append('v %.9G %.9G %.9G\n' % (-float(p[1]), float(p[2]), float(p[3])))
            except Exception:
                out.append(l)
        elif p[0] == 'vn' and len(p) >= 4:
            try:
                out.append('vn %.9G %.9G %.9G\n' % (-float(p[1]), float(p[2]), float(p[3])))
            except Exception:
                out.append(l)
        elif p[0] == 'f' and len(p) >= 4:
            # UnityPy 写入顺序=f c b a (绕序已反) — 还原=引用倒序排列
            out.append('f ' + ' '.join(p[3:0:-1]) + '\n')
        else:
            out.append(l)
    with open(fp, 'w', encoding='utf-8', errors='replace') as f:
        f.writelines(out)
    with open(fp + '.xfixed', 'w', encoding='utf-8') as f:
        f.write('1')


def fix_obj_normals(fp):
    """OBJ 缺 vn/vt 的会被导入器判 invalid → 补法线并重写索引"""
    with open(fp, encoding='utf-8', errors='replace') as f:
        lines = f.readlines()
    has_vn = any(l.startswith('vn ') for l in lines)
    has_vt = any(l.startswith('vt ') for l in lines)
    if has_vn and has_vt:
        return
    vs, vts, faces = [], [], []
    for l in lines:
        p = l.strip().split()
        if not p:
            continue
        if p[0] == 'v':
            # 0826 健壮性: 个别 OBJ v 行缺 z (2D 顶点) → 补 0 (原版偶发, darkangels 崩例)
            if len(p) >= 4:
                vs.append((float(p[1]), float(p[2]), float(p[3])))
            else:
                vs.append((float(p[1]), float(p[2]), 0.0))
        elif p[0] == 'vt':
            vts.append((float(p[1]), float(p[2])))
        elif p[0] == 'f':
            faces.append(l)
    if not vs:
        return
    vns = [(0.0, 0.0, 0.0)] * len(vs)
    for l in faces:
        idx = []
        for tok in l.strip().split()[1:]:
            try:
                idx.append(int(tok.split('/')[0]))
            except Exception:
                idx = []
                break
        if len(idx) < 3:
            continue
        a0, b0, c0 = vs[(idx[0] - 1) % len(vs)], vs[(idx[1] - 1) % len(vs)], vs[(idx[2] - 1) % len(vs)]
        ux, uy, uz = b0[0] - a0[0], b0[1] - a0[1], b0[2] - a0[2]
        vx, vy, vz = c0[0] - a0[0], c0[1] - a0[1], c0[2] - a0[2]
        n = (uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx)
        ln = math.sqrt(n[0] ** 2 + n[1] ** 2 + n[2] ** 2) or 1.0
        for i in idx:
            vi = (i - 1) % len(vns)
            vns[vi] = (vns[vi][0] + n[0] / ln, vns[vi][1] + n[1] / ln, vns[vi][2] + n[2] / ln)
    for i, v in enumerate(vns):
        ln = math.sqrt(v[0] ** 2 + v[1] ** 2 + v[2] ** 2)
        vns[i] = (v[0] / ln, v[1] / ln, v[2] / ln) if ln > 1e-6 else (0.0, 0.0, 1.0)
    out = ['# regenerated by unity_scene_to_godot (missing normals)\n']
    for v in vs:
        out.append('v %.6f %.6f %.6f\n' % v)
    for t in vts:
        out.append('vt %.6f %.6f\n' % t)
    for v in vns:
        out.append('vn %.6f %.6f %.6f\n' % v)
    for l in faces:
        parts = l.strip().split()
        new = [parts[0]]
        for tok in parts[1:]:
            ids = tok.split('/')
            vi = ids[0]
            if len(ids) > 1 and ids[1] and has_vt:
                new.append('%s/%s/%s' % (vi, ids[1], vi))
            else:
                new.append('%s//%s' % (vi, vi))
        out.append(' '.join(new) + '\n')
    with open(fp, 'w', encoding='utf-8') as f:
        f.writelines(out)


def split_obj_submeshes(base_obj_path, name, out_dir, submeshes):
    """按 Unity Mesh m_SubMeshes 区段把已修整 OBJ 切分为 <name>__s{i}.obj (F 映射 firstSubMesh 施工,
    2026-08-26): Face 顺序=索引缓冲顺序 (实测 battlearena2 Combined Mesh 31 submesh: 6004面×3=ΣindexCount;
    firstVertex/vertexCount 齐备); 每 submesh 顶点/uv/法线独立重映射 (按实际使用, v/vt/vn 各自 id 空间)"""
    vs, vts, vns, faces = [], [], [], []
    for l in open(base_obj_path, encoding='utf-8', errors='replace'):
        p = l.strip().split()
        if not p:
            continue
        if p[0] == 'v':
            vs.append(l)
        elif p[0] == 'vt':
            vts.append(l)
        elif p[0] == 'vn':
            vns.append(l)
        elif p[0] == 'f':
            faces.append(l)

    def _emit(sub_faces, tag):
        seen_v, seen_t, seen_n = {}, {}, {}
        v_l, t_l, n_l = [], [], []
        new_faces = []
        for l in sub_faces:
            toks = l.strip().split()[1:]
            new_toks = []
            ok = True
            for tok in toks:
                parts = tok.split('/')
                try:
                    vi = int(parts[0]) - 1
                except Exception:
                    ok = False
                    break
                if vi not in seen_v:
                    seen_v[vi] = len(v_l) + 1
                    v_l.append(vs[vi])
                out_tok = str(seen_v[vi])
                if len(parts) >= 2 and parts[1] != '' and vts:
                    try:
                        ti = int(parts[1]) - 1
                    except Exception:
                        ti = -1
                    if ti >= 0:
                        if ti not in seen_t:
                            seen_t[ti] = len(t_l) + 1
                            t_l.append(vts[ti])
                        out_tok += '/%d' % seen_t[ti]
                    else:
                        out_tok += '/'
                elif len(parts) >= 2 and parts[1] == '':
                    out_tok += '/'
                if len(parts) >= 3 and parts[2] != '' and vns:
                    try:
                        ni = int(parts[2]) - 1
                    except Exception:
                        ni = -1
                    if ni >= 0:
                        if ni not in seen_n:
                            seen_n[ni] = len(n_l) + 1
                            n_l.append(vns[ni])
                        out_tok += '/%d' % seen_n[ni]
                    else:
                        out_tok += '/'
                elif len(parts) >= 3 and parts[2] == '':
                    out_tok += '/'
                new_toks.append(out_tok)
            if ok:
                new_faces.append('f ' + ' '.join(new_toks) + '\n')   # 缺 \n 会导致所有面连成一行=OBJ 失效
        lines_out = v_l + t_l + n_l + new_faces
        p2 = os.path.join(out_dir, win_safe(name) + '__s%d.obj' % tag)
        with open(p2, 'w', encoding='utf-8', errors='replace') as f:
            f.writelines(lines_out)
        imp = p2 + '.import'
        if os.path.exists(imp):
            os.remove(imp)
        return p2

    out = {}
    cum = 0
    for i, s in enumerate(submeshes):
        nf = int(s.get('indexCount', 0)) // 3
        part = faces[cum:cum + nf]
        cum += nf
        if not part:
            continue
        out[i] = _emit(part, i)
    return out


if __name__ == '__main__':
    sys.exit(main())
