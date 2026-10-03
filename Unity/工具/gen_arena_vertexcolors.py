#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_vertexcolors.py —— 把**原版网格的顶点色**抽成旁挂 JSON（13 场逐场一份）。

为什么要它（判据 → `项目任务.md` §三 第 30 条 散件 C 的 2026-09-30 晚那一段）：
  我们想复刻材质关键字 **`_ALPHAMODULATE_ON`**（URP 内置，语义 = 按顶点色调制 alpha），
  而**我们的 OBJ 网格没有顶点色** —— 不是解不出，是 **`UnityPy/export/MeshExporter.py` 不写**
  （它只写 `v/vt/vn/f`；`helpers/MeshHelper.py` 早就把 channel 3 读进 `m_Colors` 了）。
  实测规模：13 场 + `battlesharedresources` 共 **120 个网格**带顶点色（**24,793 个顶点**）；
  我们这 697 个 OBJ 里对得上 **130 个文件**（一个网格被按组拆成多个 OBJ 时会有多份）。
  🔴 **2026-10-05 补注**：上面这组数**当时就包含 10 个流式网格**（`16,256` 个顶点），
  而**代码那时读不了它们** ⇒ 读数其实只有 **110 个网格 / 8,537 个顶点**（唯一名 101 个）。
  流式那一路补上之后（见下面「流式网格」那条），**120 / 24,793** 才真的成立
  （唯一名 111 个）。**判据**：`--selftest` ① 扫 14 个 bundle 数出来。

**坐标空间**（实测钉死，🔴 **改错过一次，别想当然**）：
  · 原版顶点 → 我们 **OBJ 文件**的 `v` 是 **X 取反**（`Ground Lights.0011` 原版第 1 个顶点 `+0.0146326`
    ↔ 我们 OBJ `−0.0146326`；同一条链还有**绕序翻转** —— 见 `gen_unity_arena_manifest.py:1478-1495`）；
  · 但 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    （实测：按原版空间比 **命中 186/186**，按取反后比 **0/186**；`DumpMiss` 两种手性各试一遍得到的）。
  ⇒ **本脚本产出的 `pos` 一律是原版空间（不取反）**，建场期直接与 Unity 网格顶点比。
    自检那一步要与我们 **OBJ 文件**比，所以**临时取反**（见下面 `vpos` 那行）。

产物：`<工程>/WarpforgeArena1/arenas/<场>/<场>_vcol.json`
  { "arena": "...", "items": [ { "obj": "<我们工程里的 .obj 文件名>", "mesh": "<原版网格名>",
                                "src": "<来自哪个 bundle>", "positions": [[x,y,z],...],
                                "colors": [[r,g,b,a],...] } ], "unmatched": [...] }
  · **`positions` 与 `colors` 一一对应**（都按原版网格的顶点序）。
  · 运行时/建场期按**位置**匹配到 Unity 顶点（**不能按索引**：OBJ 是按 `g` 组拆过文件的、
    而且 Unity 的 OBJ 导入器会 `weldVertices` 重排顶点）。

自检（写盘前跑）：逐个 item 把**我们 OBJ 的 `v` 行**拿去 `positions` 里找（1e-4 容差），
  命中率与漏掉的写进日志；`unmatched` = 有顶点色但我们在工程里找不到对应 OBJ 的网格。

🆕 **静态合批的「孤儿源网格」→ 接到它被烘进去的那一段 OBJ 上**（2026-10-04，见下面
  `static_batch_join` 的头注释）：`battlearena2` 是**唯一**一场有静态合批的
  （13 场逐 bundle 实读 `m_StaticBatchInfo`：这一场 33 个、其余 12 场 0 个 ——
  `资料/战场13场_逐场对账_0920.md` ①-i-2 / ①-i-3），合批后源网格（`Barrels1`）**不再被
  任何 MeshFilter 引用**、工程里也就没有同名 OBJ ⇒ 原来会一直挂在 `unmatched` 里。

🆕 **流式网格（`.resS`）**（2026-10-05，见下面 `RessReader` / `_read_stream` 的头注释）：
  有的网格 `m_VertexData.m_DataSize` **长度是 0**，顶点数据在 bundle 的 **`.resS` 流**里
  （`m_StreamData = {offset, size, path}`）。原来这一路**读不到** ⇒ 这类网格**连 `unmatched`
  都进不去**（静默漏账，实测 `battlearena2` 的 `Rubble1`）。现在从流里按 `(offset,size)` 取。

用法：PYTHONIOENCODING=utf-8 python 工具/gen_arena_vertexcolors.py [--arena <场>] [--check] [--selftest]
  · `--check` 只扫不写；`--selftest` 只跑「流式网格」那一路的三条断言（**不写任何文件** —— 见 `selftest()`）。
"""
import argparse
import collections
import hashlib
import io
import json
import math
import os
import struct
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')
import UnityPy

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
MODELS = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s/Models'
BUNDLES = ['battlesharedresources_assets_all'] + [
    'scenes_scenes_battlearena%s' % a for a in
    ['1', '2', '3', 'aeldari', 'astramilitarum', 'blacklegion', 'darkangels', 'emperorschildren',
     'genestealers', 'leviathan', 'sororitas', 'spacewolves', 'tauviorla']]

FMT_SIZE = {0: 4, 1: 2, 2: 1, 3: 1}      # 0=Float32 1=Float16 2=UNorm8 3=SNorm8（Unity 的 VertexAttributeFormat）

# 出声用（**不许静默漏账**）：流式网格的读数 —— 读到 / 读不到 / 没有读取器
STREAM_STAT = collections.Counter()


def _dim(c):
    """`ChannelInfo.dimension` —— **只有低 4 位是维度**。

    🔴 **2026-10-05 实测钉死**：流式网格的**法线通道**存的字节是 **`0x34`**（= `4 | 0x30`），
    按原值算步长会得到 **116**，真值 **48**（`Rubble1`）。
    判据（**可复算、不靠猜**）：14 个 bundle 的 **165 个流式网格**里，
    `m_StreamData.size ÷ m_VertexCount` **165/165 恰好等于**按 `dimension & 0x0f` 算出的步长；
    **按原值算则有 152 个对不上**。
    ⚠️ **非流式网格里一个都没有** `dimension > 4`（708 个网格全扫过）⇒ 这个掩码对它们**恒等**、
    不改既有行为（这也是为什么老代码一直没暴露这个问题）。"""
    return c.get('dimension', 0) & 0x0F


def item_size(fmt, dim):
    return FMT_SIZE.get(fmt, 4) * dim


def read_channel(raw, stride, ch, n, fmt):
    """按通道取 n 个顶点 → [(v...)]。只支持 fmt 0（Float32）与 2（UNorm8）——实测就这两种。"""
    off, dim = ch['offset'], _dim(ch)      # 🔴 维度要过 `_dim` 掩码（见它上面的实测）
    out = []
    if fmt == 0:
        for i in range(n):
            out.append(struct.unpack_from('<%df' % dim, raw, i * stride + off))
    elif fmt == 2:
        for i in range(n):
            out.append(tuple(b / 255.0 for b in struct.unpack_from('<%dB' % dim, raw, i * stride + off)))
    else:
        return None
    return out


class RessReader:
    """bundle 内层 **`.resS` 流** → 按 `(offset, size)` 取字节（流式网格的顶点数据在这里）。

    🔴 **两个坑（都实测过，别改回去）**：
    1. **必须 seek + `read_bytes`，不能整份 `read()`**：内层文件是 `EndianBinaryReader`，
       `read()` 是**消耗式**的 —— 读第二次返回 **0 字节**（实测 `battlesharedresources` 的
       `.resS` 第一次 `read()` = 122,903,776 B、第二次 = 0）。`gen_skybox_cubemap.py` 那边
       只读一次所以没暴露这个坑。
    2. **不要整份读进内存**：`battlesharedresources` 的 `.resS` 是 **122.9 MB**，
       而我们每个网格只要几 KB ~ 几百 KB。
    自检（一次性做过）：`Position=off; read_bytes(size)` 与「整份 `read()` 再切片」
    **逐字节相同**（`Rubble1` 51216 B · `Floor` 74032 B，md5 一致、且可重复取）。
    ⚠️ `m_StreamData.offset` 是**相对 `.resS` 起点**的（reader 的 `Position` 已经把这层算好了，
    别自己再加 `BaseOffset`）。
    """

    def __init__(self, env):
        f = getattr(env, 'file', None)
        self._files = getattr(f, 'files', None) or {}

    def __call__(self, path, offset, size):
        key = (path or '').split('/')[-1]          # `archive:/CAB-x/CAB-x.resS` ⇒ `CAB-x.resS`
        f = self._files.get(key)
        if f is None:                              # 兜底：有的包用整串当键
            f = self._files.get(path)
        if f is None:
            return None
        f.Position = offset
        return f.read_bytes(size)


def _read_stream(d, res, what):
    """流式网格的顶点数据 → bytes，取不到就**出声**并返回 None。"""
    sd = d.get('m_StreamData') or {}
    if not sd.get('size'):
        return None                                # 既没内联数据、也没流 ⇒ 真的没有顶点数据
    if res is None:
        STREAM_STAT['no_reader'] += 1
        return None
    raw = res(sd.get('path'), sd.get('offset'), sd.get('size'))
    if raw is None or len(raw) != sd['size']:
        STREAM_STAT['fail'] += 1
        print('      ⚠️ 流式网格 `%s`（%s）：`.resS` 里取不到 %s 字节（拿到 %s）—— 这一条读不了'
              % (d.get('m_Name'), what, sd['size'], None if raw is None else len(raw)))
        return None
    STREAM_STAT['ok'] += 1
    return raw


def _vd_channels(d, res=None):
    """`m_VertexData` 的公共部分 → (raw, stride, n, ch)，取不到就 None。
    （`mesh_colors` 与 `mesh_positions` 两个入口共用这一段，别再各写一份。）
    `res` = `RessReader`（流式网格用）；不给就退回「只读内联数据」的老行为。"""
    vd = d.get('m_VertexData')
    if not vd:
        return None
    ch = vd.get('m_Channels') or []
    raw = vd.get('m_DataSize')          # ⚠️ UnityPy 把字节 blob 放在这个名字下（不是长度）
    if not isinstance(raw, (bytes, bytearray)):
        return None
    if not raw:
        # 🔴 **流式网格**：`m_DataSize` 长度 0、真数据在 `.resS` 里（见 `RessReader` 头注释）
        raw = _read_stream(d, res, 'm_VertexData')
        if raw is None:
            return None
    n = vd['m_VertexCount']
    stride = max(c['offset'] + item_size(c['format'], _dim(c)) for c in ch if _dim(c))
    if stride * n > len(raw):
        return None
    return raw, stride, n, ch


def mesh_positions(d, res=None):
    """只要**位置**（`mesh_colors` 的裁掉颜色那一半）。
    合并网格（静态合批的产物）**没有顶点色**，但它的顶点位置是那条 join 的尺子。"""
    ch = ((d.get('m_VertexData') or {}).get('m_Channels')) or []
    if len(ch) < 1 or not _dim(ch[0]):
        return None                     # 先做**便宜的先判**：没有 ch0 就别去动 `.resS`（见 `mesh_colors`）
    r = _vd_channels(d, res)
    if r is None:
        return None
    raw, stride, n, ch = r
    return read_channel(raw, stride, ch[0], n, ch[0]['format'])


def mesh_colors(d, res=None):
    """→ (positions, colors) 或 None（没有顶点色 / 格式不支持）。
    positions **保留原版空间、【不】取反**（⚠️ 2026-10-04 更正：原 docstring 写「已按 X 取反」，与同函数正文那句自相矛盾）。"""
    ch = ((d.get('m_VertexData') or {}).get('m_Channels')) or []
    if len(ch) < 4 or not _dim(ch[3]):
        return None                     # 便宜的先判：没有顶点色 ⇒ **不去动 `.resS`**（省掉没用的读）
    r = _vd_channels(d, res)
    if r is None:
        return None
    raw, stride, n, ch = r
    pos = read_channel(raw, stride, ch[0], n, ch[0]['format'])
    col = read_channel(raw, stride, ch[3], n, ch[3]['format'])
    if pos is None or col is None:
        return None
    if len(col[0]) == 3:                # 理论上是 4；真遇到 3 就补 alpha
        col = [tuple(c) + (1.0,) for c in col]
    # 🔴 **这里【不】取反 —— 保留原版空间**。实测（2026-09-30 晚，`DumpMiss` 两种手性各试一遍）：
    #   · 我们的 OBJ 文件 = **原版 X 取反**（导出器干的）
    #   · 而 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    #   ⇒ 拿**我们 OBJ 文件**的顶点比时（下面的 `vpos` 那一行），要先**临时取反**；而拿 **Unity 网格**比时应**原样**用原版空间：`原样命中 0/186 · X 取反后命中 186/186`
    #     （`Candles 52`）· `0/258 → 258/258`（`Ground Lights.0011`）。
    #   与 OBJ 文件比对（下面的自检）时**临时取反**即可。
    return pos, col


def obj_vertices(path):
    """读我们 OBJ 的 `v` 行（顺序即文件序）。"""
    out = []
    for line in io.open(path, encoding='utf-8', errors='replace'):
        if line.startswith('v '):
            a = line.split()
            out.append((float(a[1]), float(a[2]), float(a[3])))
    return out


# ---------------------------------------------------------------------------
# 几何认领的**匹配原语**（2026-10-05 换过一次，见下面那条改动理由）
#
# 🔴 **为什么不再用 `round(v, 4)` 比 key**：那是我原来的写法，它有一条**半值边界**的病 ——
#   我们 OBJ 的 `v` 是**十进制文本**、网格那边是 **float32**，同一个点两边差 <1e-7，
#   可当它**正好骑在 1e-4 的量化半值上**时，`round()` 会**朝相反方向破** ⇒ 报「对不上」。
#   实测（`Toxic_pool_wall1.obj` / leviathan）：2595 个顶点里 **11 个**是这么丢的，
#   例：`z = 2.67205` → OBJ 侧 round 成 `2.6721`、网格侧 `2.6720499992370605` → `2.6720`。
#   ⇒ 换成**逐分量容差的最近邻**：`|Δx| ≤ TOL 且 |Δy| ≤ TOL 且 |Δz| ≤ TOL`，取最近的那个。
#
# ⚠️ **严格度没有降**（这是本次改动的全部依据，别再放宽）：
#   旧判据「两边第 4 位小数相等」**蕴含** `|Δ| ≤ 1e-4` ⇒ **新判据是旧判据的超集**，
#   任何候选的命中数**只会增、不会减**；上界仍是那 **1e-4**
#   （= 原版网格坐标量级 ~1e2 时 float32 的 eps 的十几倍；与**静态合批那条 join** 的
#   `SB_TOL = 1e-4` 是同一个数，两条口子口径一致）。
#   ⛔ **认领规则三条一个字没动**：本场包优先 → 同名本就没色则跳过 → 几何认领 **100% 命中才写**。
# ---------------------------------------------------------------------------
TOL = 1e-4


def _pos_buckets(pos, tol=TOL):
    """顶点位置 → 边长 `tol` 的网格桶 `{(ix,iy,iz): [顶点下标...]}`。"""
    g = 1.0 / tol
    out = {}
    for i, p in enumerate(pos):
        k = (int(math.floor(p[0] * g)), int(math.floor(p[1] * g)), int(math.floor(p[2] * g)))
        out.setdefault(k, []).append(i)
    return out


def geom_hits(vpos, pos, tol=TOL):
    """OBJ 顶点集 → 候选网格顶点集的**命中数**（逐分量 `|Δ| ≤ tol`，取最近的那个）。

    返回 `(命中数, 最大偏差)`。`最大偏差 ≤ tol` 恒成立（只统计命中的那些）。
    ⚠️ 查的是**本桶 + 26 个邻桶** —— 桶边长就是 `tol`，所以「分量差 ≤ tol」的点**必定**落在这 27 个桶里。"""
    b = _pos_buckets(pos, tol)
    g = 1.0 / tol
    hit, worst = 0, 0.0
    for p in vpos:
        bx = int(math.floor(p[0] * g))
        by = int(math.floor(p[1] * g))
        bz = int(math.floor(p[2] * g))
        best = None
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for i in b.get((bx + dx, by + dy, bz + dz), ()):
                        q = pos[i]
                        d = max(abs(p[0] - q[0]), abs(p[1] - q[1]), abs(p[2] - q[2]))
                        if d <= tol and (best is None or d < best):
                            best = d
        if best is not None:
            hit += 1
            if best > worst:
                worst = best
    return hit, worst


def obj_face_span(path):
    """→ (v 行数, (最小下标, 最大下标, 去重个数) | None)。下标是 **0-based**，被 `f` 行真正引用到的。

    🔴 为什么需要它：静态合批拆出来的 `<合并网格>__smN.obj` 把**整张合并网格的 `v` 全写了**
    （实测 `Combined Mesh (root_ scene)__sm10.obj` = 9965 行 `v`、9965 行 `vt`、9965 行 `vn`），
    只有第 N 段的 `f` 用到其中一段（实测该文件引用的恰好是 2201…2440 = 240 个）
    ⇒ 「这文件是哪一段」只能看 `f` 的下标，**不能看 `v` 的行数**。"""
    nv, lo, hi, cnt = 0, None, None, set()
    for line in io.open(path, encoding='utf-8', errors='replace'):
        if line.startswith('v '):
            nv += 1
        elif line.startswith('f '):
            for tok in line.split()[1:]:
                i = int(tok.split('/')[0]) - 1      # OBJ 下标从 1 起
                cnt.add(i)
                lo = i if lo is None else min(lo, i)
                hi = i if hi is None else max(hi, i)
    return nv, (None if lo is None else (lo, hi, len(cnt)))


# ---------------------------------------------------------------------------
# 静态合批的「孤儿源网格」→ 接到它被烘进去的那一段 OBJ 上（2026-10-04）
#
# 机制（判据**全部读原版 bundle**，不看我们自己的清单）：
#   · Unity 静态合批把一批静态物体的网格合进 `Combined Mesh (root: scene)`，渲染器改挂合并网格，
#     靠 `m_StaticBatchInfo = {firstSubMesh, subMeshCount=1}` 只画第 N 段。
#     ⇒ 源网格（`Barrels1`）**不再被任何 MeshFilter 引用**，但资产还在包里 ⇒ 工程里没有同名 OBJ。
#   · 第 N 段的顶点 = `m_SubMeshes[N].{firstVertex, vertexCount}`；我们工程里对应的文件是
#     `<合并网格名>__smN.obj`（生成器 `split_obj_by_group` 拆的，命名见
#     `工具/scripts快照/gen_unity_arena_manifest.py:1239`）。
#   · 我们的渲染器**不带 transform**（`ArenaBuilder.cs:1850` 对 `worldBaked` 不 ApplyTransform）
#     ⇒ 那一段的顶点是**世界坐标** ⇒ 旁挂的 `pos` 必须写**世界坐标**（= 合并网格里的原值，
#     与 Unity 网格逐位相同；这一点与别的条目不同 —— 那些写的是原版网格的**局部**坐标）。
# 认领规则：**必须几何认领**（拿源网格的顶点乘该物体的世界矩阵，与那一段的顶点**逐个按下标**比，
#   100% 命中才写，否则不写并出声）—— 与上面「按几何认领」同一条纪律。
# 实测（`battlearena2` 的 `Barrels1`）：240/240 命中、最大偏差 3.8e-06；
#   `m_SubMeshes[10]` = firstVertex 2201 × 240 个；`Barrels` 的 `firstSubMesh = 10`。
# ⚠️ **顶点数会对不上，这是形态决定的、不是错**：`__smN.obj` 里写着**整张合并网格的 9965 个 `v`**
#   （`split_obj_by_group` 每份都带同一段顶点块），而这一条只覆盖**那 240 个**（＝该文件 `f` 引用的）。
#   ⇒ 建场期 `MeshVertexColors` 按 `src.vertices` 逐个找色：若 Unity 的 OBJ 导入器**把没被 `f`
#   引用的顶点也留下**（**这一点我们没验过**），那一趟会多报 ~9725 个「按位置找不到色」——
#   那批顶点不在任何面上、且这边的颜色本来就是白 ⇒ **不影响画面，只脏日志**。
#   （要一次钉死：重打 arena2 后读 `battlearena2.unity` 里那条 `<...>__sm10_vcol` 网格的 `m_VertexCount`。）
# ⚠️ 只处理「本场包里带顶点色、且工程里没建出来」的网格；没静态合批的 12 场根本进不来
#   （`m_StaticBatchInfo.subMeshCount == 1` 一个都没有 ⇒ 表为空 ⇒ 一个条目都不加）。
# ---------------------------------------------------------------------------
SB_TOL = 1e-4        # 几何容差：世界坐标 ~1e2 时 float32 的 eps ≈ 7.6e-6 ⇒ 有 ~13 倍余量


def _mat_mul(A, B):
    return [[sum(A[i][k] * B[k][j] for k in range(4)) for j in range(4)] for i in range(4)]


def _local_matrix(lt):
    """4×4 局部矩阵。**抄自 `工具/scripts快照/gen_unity_arena_manifest.py:682`（同一套写法）**。"""
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


def _t_local_trs(t):
    """`Transform` → `_local_matrix` 要的那个三元组（**形状与清单生成器那份一致**，别改成扁平）。"""
    p, q, s = t['m_LocalPosition'], t['m_LocalRotation'], t['m_LocalScale']
    return (p['x'], p['y'], p['z'],
            (q['x'], q['y'], q['z'], q['w']),
            (s['x'], s['y'], s['z']))


def _world_matrix(tf, pid):
    """沿 `m_Father` 上溯累乘 → 4×4 世界矩阵。
    **父链只要有一环 `m_FileID != 0`（跨文件）就返回 None** —— 判不了就出声，别拿半截父链当世界矩阵。"""
    chain, cur = [], pid
    while cur:
        t = tf.get(cur)
        if t is None:
            return None
        fa = t.get('m_Father') or {}
        if fa.get('m_FileID'):
            return None
        chain.append(_t_local_trs(t))
        cur = fa.get('m_PathID')
    M = [[1.0 if i == j else 0.0 for j in range(4)] for i in range(4)]
    for lt in reversed(chain):
        M = _mat_mul(M, _local_matrix(lt))
    return M


def static_batch_sources(bp):
    """读**原版 bundle** → 静态合批表：每个 `m_StaticBatchInfo.subMeshCount == 1` 的渲染器一条
    `{go, mesh, first, fv, vc, mvc, mpos, M}`（`mpos` = 合并网格**整张**的顶点位置）。
    **只认合并网格与渲染器在同一个文件里的（`m_FileID == 0`）** —— 跨文件的路径先不用
    （`资料/已知的坑.md`：「解析引用一律先走 PPtr、别拿 PathID 全库扫小号」）。"""
    env = UnityPy.load(bp)
    res = RessReader(env)           # 合并网格也可能是流式的（本场实测不是，但别默认它不是）
    meshes, gos, tf, mfs, mrs = {}, {}, {}, [], []
    for o in env.objects:
        fn = o.assets_file.name
        if o.type.name == 'Mesh':
            meshes[(fn, o.path_id)] = o.read_typetree()
        elif o.type.name == 'GameObject':
            gos[(fn, o.path_id)] = o.read_typetree().get('m_Name')
        elif o.type.name == 'Transform':
            tf.setdefault(fn, {})[o.path_id] = o.read_typetree()
        elif o.type.name == 'MeshFilter':
            mfs.append((fn, o.read_typetree()))
        elif o.type.name == 'MeshRenderer':
            mrs.append((fn, o.read_typetree()))
    out = []
    for fn, d in mrs:
        sbi = d.get('m_StaticBatchInfo') or {}
        if sbi.get('subMeshCount') != 1:
            continue
        gop = (d.get('m_GameObject') or {}).get('m_PathID')
        first = sbi.get('firstSubMesh')
        # 同一个 GameObject 上的 MeshFilter → 合并网格
        src = None
        for fn2, mf in mfs:
            if fn2 == fn and (mf.get('m_GameObject') or {}).get('m_PathID') == gop:
                src = mf
                break
        if src is None:
            print('      ⚠️ 静态合批：渲染器所在物体上没有 MeshFilter —— 跳过')
            continue
        pp = src.get('m_Mesh') or {}
        if pp.get('m_FileID'):
            print('      ⚠️ 静态合批：合并网格不在本文件里（m_FileID=%s）—— 跳过' % pp.get('m_FileID'))
            continue
        md = meshes.get((fn, pp.get('m_PathID')))
        if md is None:
            print('      ⚠️ 静态合批：本文件里找不到 pathID=%s 的网格 —— 跳过' % pp.get('m_PathID'))
            continue
        subs = md.get('m_SubMeshes') or []
        mpos = mesh_positions(md, res)
        if first is None or first >= len(subs) or mpos is None:
            print('      ⚠️ 静态合批：`%s` 的 firstSubMesh=%s 读不出来（子网格 %d 个）—— 跳过'
                  % (md.get('m_Name'), first, len(subs)))
            continue
        fv = subs[first].get('firstVertex')
        vc = subs[first].get('vertexCount')
        # ⚠️ 顶点数要用**读出来的位置个数**：`m_VertexCount` 在 `m_VertexData` 里面、**不在 Mesh 顶层**
        #    （写成 `md.get('m_VertexCount')` 会恒为 None ⇒ 每一段都被判「读不出来」，实测踩过）
        if fv is None or not vc or fv + vc > len(mpos):
            print('      ⚠️ 静态合批：`%s` 第 %s 段的顶点区间读不出来（顶点 %d 个）—— 跳过'
                  % (md.get('m_Name'), first, len(mpos)))
            continue
        tpid = None
        for pid, t in tf.get(fn, {}).items():
            if (t.get('m_GameObject') or {}).get('m_PathID') == gop:
                tpid = pid
                break
        M = _world_matrix(tf.get(fn, {}), tpid) if tpid else None
        if M is None:
            print('      ⚠️ 静态合批：`%s` 的世界矩阵读不出来（父链跨文件？）—— 跳过'
                  % gos.get((fn, gop), '?'))
            continue
        out.append({'go': gos.get((fn, gop), '?'), 'mesh': md.get('m_Name'), 'first': first,
                    'fv': fv, 'vc': vc, 'mvc': len(mpos), 'mpos': mpos, 'M': M})
    return out


def _batch_dev(src_pos, M, sl):
    """源网格顶点乘世界矩阵 → 与那一段顶点**按下标**逐个比，返回最大分量偏差。"""
    worst = 0.0
    for i, v in enumerate(src_pos):
        w = [M[0][0] * v[0] + M[0][1] * v[1] + M[0][2] * v[2] + M[0][3],
             M[1][0] * v[0] + M[1][1] * v[1] + M[1][2] * v[2] + M[1][3],
             M[2][0] * v[0] + M[2][1] * v[1] + M[2][2] * v[2] + M[2][3]]
        d = max(abs(w[0] - sl[i][0]), abs(w[1] - sl[i][1]), abs(w[2] - sl[i][2]))
        if d > worst:
            worst = d
    return worst


def static_batch_join(own, mdir, names, index):
    """把「本场包里有顶点色、但工程里没建出来」的网格接到它被静态合批烘进去的那一段 OBJ 上。
    → (items, 接上的网格名)。**认不出的一律不写**（交由调用方照实记进 `unmatched`）。"""
    if not names:
        return [], []
    spans = {}                                  # 文件名 → (v 行数, 被 `f` 引用的下标段)
    for f in sorted(os.listdir(mdir)):
        if f.endswith('.obj') and '__sm' in f:
            spans[f] = obj_face_span(os.path.join(mdir, f))
    if not spans:
        print('      ⚠️ 本场有 %d 个带色网格没建出来，但 Models/ 里一个 `__sm*.obj` 都没有 '
              '⇒ 静态合批那条 join 用不上' % len(names))
        return [], []
    bp = os.path.join(AA, own + '.bundle')
    srcs = static_batch_sources(bp)
    items, done = [], []
    for name in names:
        cands = [c for c in (index.get(name) or []) if c[0] == own]
        if len(cands) != 1:
            print('      ⚠️ `%s`：本场包里的同名带色网格有 %d 个（要 1 个）—— 不接' % (name, len(cands)))
            continue
        _, pos, col = cands[0]
        n = len(pos)
        hits = []
        for s in srcs:
            if s['vc'] != n:
                continue
            d = _batch_dev(pos, s['M'], s['mpos'][s['fv']:s['fv'] + s['vc']])
            if d <= SB_TOL:
                hits.append((d, s))
        if len(hits) != 1:
            print('      ⚠️ `%s`（%d 顶点）：静态合批那 %d 个渲染器里命中 %d 个（要 1 个）—— 不接'
                  % (name, n, len(srcs), len(hits)))
            continue
        d, s = hits[0]
        # 那一段落在哪个文件：**v 行数 = 合并网格顶点数** 且 **`f` 引用的下标段 = 该段**（两条都要）
        want = (s['fv'], s['fv'] + s['vc'] - 1, s['vc'])
        f = [fn for fn, (nv, sp) in spans.items() if nv == s['mvc'] and sp == want]
        if len(f) != 1:
            print('      ⚠️ `%s`：静态合批那一段（firstSubMesh=%d · firstVertex %d × %d）在 Models/ 里'
                  '对上 %d 个文件（要 1 个）—— 不接' % (name, s['first'], s['fv'], s['vc'], len(f)))
            continue
        sl = s['mpos'][s['fv']:s['fv'] + s['vc']]
        items.append({
            'obj': f[0], 'mesh': name, 'src': own,
            'n': s['vc'],
            # 🔴 世界坐标（合并网格里的原值）—— 这个物体的渲染器不带 transform，见本节头注释
            'pos': [v for p in sl for v in p],
            'col': [round(v, 5) for c in col for v in c],
            # 自检口径：`_objVerts` 仍记**文件里的 `v` 行数**（9965，与别的条目同口径），
            # `_hitVerts` 记**这一段真正用到的顶点数**（240）—— 两者不等是 `__smN.obj` 的形态决定的
            '_objVerts': spans[f[0]][0], '_hitVerts': spans[f[0]][1][2],
            '_batch': '静态合批：firstSubMesh=%d · m_SubMeshes[%d] = firstVertex %d × %d 个 · '
                      '该文件 `f` 只引用这一段' % (s['first'], s['first'], s['fv'], s['vc']),
            '_batchMatch': '几何认领：`%s` 的顶点 × `%s` 的世界矩阵 = 那一段，%d/%d 全中（最大偏差 %.1e）'
                           % (name, s['go'], s['vc'], s['vc'], d),
        })
        done.append(name)
        print('      ✅ 静态合批接上 `%s` → `%s`（%d 顶点 · firstSubMesh=%d · 几何偏差 %.1e）'
              % (name, f[0], s['vc'], s['first'], d))
    return items, done


def selftest():
    """`--selftest`：**流式那一路的自检**（只读、不写任何文件）。三条断言，逐条打 PASS/FAIL：

    ① **步长不变量**：每个流式网格都满足 `m_StreamData.size == stride × m_VertexCount`
       （`stride` 按 `_dim` 掩码算）。**这条就是当年揪出「`dimension` 高 4 位是垃圾」的那条** ——
       实测 165/165 成立；按未掩码的原值算有 152 个对不上。
    ② **位置对得上 `m_LocalAABB`**：读出来的顶点包围盒必须**落在** `m_LocalAABB` 里（容差见下）。
       AABB 是**序列化里另一个字段**（不是同一段字节算出来的）⇒ 拿它当尺子是**独立证据**，
       不是自己证自己。位置读错（错 stride / 错 offset）这条必红。
    ③ **`.resS` 的取法**：`Position=off; read_bytes(size)` 与「整份 `read()` 再切片」**逐字节相同**。
       这条同时挡住那个**消耗式 `read()`** 的坑（读第二次返回 0 字节）。
       ⚠️ 整份读很贵（`battlesharedresources` 的 `.resS` **122.9 MB**）⇒ 只对**最小的那份**做，
       其余包只验「同一段取两次结果相同」。
    """
    FMT = FMT_SIZE
    ok = [True]

    def chk(cond, msg):
        if not cond:
            ok[0] = False
        print('    %s %s' % ('✅' if cond else '🔴 FAIL', msg))

    print('① 步长不变量（m_StreamData.size == stride × m_VertexCount）')
    n_mesh = n_stream = n_bad = 0
    for b in BUNDLES:
        p = os.path.join(AA, b + '.bundle')
        if not os.path.isfile(p):
            continue
        env = UnityPy.load(p)
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            d = o.read_typetree()
            vd = d.get('m_VertexData') or {}
            sd = d.get('m_StreamData') or {}
            if not sd.get('size'):
                continue
            n_mesh += 1
            ch = vd.get('m_Channels') or []
            n = vd['m_VertexCount']
            stride = max(c['offset'] + item_size(c['format'], _dim(c)) for c in ch if _dim(c))
            if stride * n != sd['size']:
                n_bad += 1
                print('      🔴 %s/%s：stride %d × %d = %d ≠ %d'
                      % (b, d.get('m_Name'), stride, n, stride * n, sd['size']))
        n_stream += 1
    print('    （扫过 %d 个 bundle · 流式网格 %d 个 · 不满足 %d 个）' % (n_stream, n_mesh, n_bad))
    chk(n_bad == 0 and n_mesh > 0, '步长不变量：%d/%d 成立' % (n_mesh - n_bad, n_mesh))

    print('② 位置落在 m_LocalAABB 里（独立字段，拿它当尺子）')
    worst = 0.0
    n_ab = n_ab_bad = 0
    for b in BUNDLES:
        p = os.path.join(AA, b + '.bundle')
        if not os.path.isfile(p):
            continue
        env = UnityPy.load(p)
        res = RessReader(env)
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            d = o.read_typetree()
            if not (d.get('m_StreamData') or {}).get('size'):
                continue
            r = mesh_colors(d, res)
            if r is None:
                continue
            pos = r[0]
            ab = d.get('m_LocalAABB') or {}
            c, e = ab.get('m_Center'), ab.get('m_Extent')
            if not c or not e:
                continue
            n_ab += 1
            lo = [min(q[i] for q in pos) for i in range(3)]
            hi = [max(q[i] for q in pos) for i in range(3)]
            cen = [(lo[i] + hi[i]) / 2.0 for i in range(3)]
            ext = [(hi[i] - lo[i]) / 2.0 for i in range(3)]
            scale = max(abs(c['x']), abs(c['y']), abs(c['z']),
                        abs(e['x']), abs(e['y']), abs(e['z']), 1e-6)
            tol = scale * 1e-4                     # 相对容差：AABB 在有的网格上比顶点集略大
            dev = max([abs(cen[i] - (c['x'], c['y'], c['z'])[i]) for i in range(3)]
                      + [abs(ext[i] - (e['x'], e['y'], e['z'])[i]) for i in range(3)])
            worst = max(worst, dev / scale)
            if dev > tol:
                n_ab_bad += 1
                print('      🔴 %s/%s：算出来 center=%s extent=%s ≠ m_LocalAABB center=%s extent=%s'
                      % (b, d.get('m_Name'), cen, ext, (c['x'], c['y'], c['z']), (e['x'], e['y'], e['z'])))
    print('    （可比的流式带色网格 %d 个 · 超出容差 %d 个 · 最大相对偏差 %.2e）'
          % (n_ab, n_ab_bad, worst))
    chk(n_ab_bad == 0 and n_ab > 0, '位置 vs m_LocalAABB：%d/%d 落在容差内' % (n_ab - n_ab_bad, n_ab))

    print('③ `.resS` 取法（seek+read_bytes == 整份 read() 再切片；且可重复取）')
    best = None                                 # (长度, bundle, 键, offset, size)
    for b in BUNDLES:
        p = os.path.join(AA, b + '.bundle')
        if not os.path.isfile(p):
            continue
        env = UnityPy.load(p)
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            d = o.read_typetree()
            sd = d.get('m_StreamData') or {}
            if not sd.get('size'):
                continue
            key = sd['path'].split('/')[-1]
            f = env.file.files.get(key)
            if f is None:
                continue
            n = getattr(f, 'Length', None)
            if n and (best is None or n < best[0]):
                best = (n, b, key, sd['offset'], sd['size'], d.get('m_Name'))
    if best is None:
        chk(False, '找不到任何 `.resS` —— 这条自检没跑成')
    else:
        n, b, key, off, size, nm = best
        env = UnityPy.load(os.path.join(AA, b + '.bundle'))
        f = env.file.files[key]
        whole = f.read()                        # ⚠️ 消耗式：这一份读完就没了
        sl = whole[off:off + size]
        env2 = UnityPy.load(os.path.join(AA, b + '.bundle'))
        f2 = env2.file.files[key]
        f2.Position = off
        a1 = f2.read_bytes(size)
        f2.Position = off
        a2 = f2.read_bytes(size)
        print('    （拿最小的那份 `%s`（%.1f MB / 网格 `%s`）· offset=%d size=%d）'
              % (key, n / 1048576.0, nm, off, size))
        chk(a1 == sl, 'seek 读 == 整份切片（%d 字节 · md5 %s）'
            % (len(a1), hashlib.md5(a1).hexdigest()[:12]))
        chk(a1 == a2, '同一段取两次结果相同（挡住「消耗式 read()」那个坑）')
        # 其余包：只验可重复取
        n_rep = n_rep_bad = 0
        for bb in BUNDLES:
            if bb == b:
                continue
            pp = os.path.join(AA, bb + '.bundle')
            if not os.path.isfile(pp):
                continue
            e2 = UnityPy.load(pp)
            for o in e2.objects:
                if o.type.name != 'Mesh':
                    continue
                dd = o.read_typetree()
                sd = dd.get('m_StreamData') or {}
                if not sd.get('size'):
                    continue
                kk = sd['path'].split('/')[-1]
                ff = e2.file.files.get(kk)
                if ff is None:
                    continue
                ff.Position = sd['offset']
                x1 = ff.read_bytes(sd['size'])
                ff.Position = sd['offset']
                x2 = ff.read_bytes(sd['size'])
                n_rep += 1
                if x1 != x2 or len(x1) != sd['size']:
                    n_rep_bad += 1
                    print('      🔴 %s/%s 取两次不一致（%d / %d / 期望 %d）'
                          % (bb, dd.get('m_Name'), len(x1), len(x2), sd['size']))
        chk(n_rep_bad == 0, '其余 %d 个流式网格都可重复取（坏 %d 个）' % (n_rep, n_rep_bad))

    print()
    print('自检结果：%s' % ('✅ 全过' if ok[0] else '🔴 有 FAIL'))
    return 0 if ok[0] else 1


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None, help='只处理某一场（默认 13 场全做）')
    ap.add_argument('--check', action='store_true', help='只扫不写')
    ap.add_argument('--selftest', action='store_true',
                    help='只跑「流式网格」那一路的自检（不写文件；见 `selftest()` 的头注释）')
    a = ap.parse_args()

    if a.selftest:
        return selftest()

    # 网格名 → [(bundle, positions, colors)]（**只有带顶点色的**）
    index = collections.defaultdict(list)
    # 网格名 → {bundle}（**所有**网格，用来判「本场那个同名的网格本来就没顶点色」）
    names_all = collections.defaultdict(set)
    colored_pair = set()      # (名字, bundle) 里**带顶点色**的那些 —— 判「本场这个网格有没有色」只能靠它
    for b in BUNDLES:
        p = os.path.join(AA, b + '.bundle')
        if not os.path.isfile(p):
            print('⚠️ 缺 bundle', b)
            continue
        env = UnityPy.load(p)
        res = RessReader(env)               # 流式网格的顶点数据在 `.resS` 里（见 `RessReader`）
        got = 0
        stream_got = 0
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            try:
                d = o.read_typetree()
            except Exception:
                continue
            nm = d.get('m_Name')
            names_all[nm].add(b)
            r = mesh_colors(d, res)
            if r is None:
                continue
            colored_pair.add((nm, b))
            index[nm].append((b, r[0], r[1]))
            got += 1
            if not (d['m_VertexData'].get('m_DataSize') or b''):
                stream_got += 1             # 这一条是**从 `.resS` 流里**读出来的
        if got:
            print('  %-46s 带顶点色的网格 %d 个%s' % (b, got, ('（其中流式 %d 个）' % stream_got) if stream_got else ''))
    print('唯一网格名 %d 个（其中带色 %d 个）' % (len(names_all), len(index)))
    print('流式网格的 `.resS` 读数：读到 %d 个 · 取不到 %d 个 · 没有读取器 %d 个'
          % (STREAM_STAT['ok'], STREAM_STAT['fail'], STREAM_STAT['no_reader']))
    if STREAM_STAT['no_reader']:
        print('  ⚠️ 「没有读取器」非 0 = 有流式网格**没被尝试读** —— 这是静默漏账的前兆，查一下调用处')

    arenas = [a.arena] if a.arena else [b.replace('scenes_scenes_', '') for b in BUNDLES[1:]]
    for arena in arenas:
        mdir = MODELS % arena
        if not os.path.isdir(mdir):
            print('⚠️ 没有 %s' % mdir)
            continue
        objfiles = [f for f in os.listdir(mdir) if f.endswith('.obj')]
        items, unmatched, miss_items, miss_total, hit_total, nocount = [], [], [], 0, 0, 0
        dev_max = 0.0          # 本场认领到的**最差**几何偏差（判据见 `geom_hits`）
        for f in sorted(objfiles):
            mesh_name = f[:-len('.obj')]
            v = obj_vertices(os.path.join(mdir, f))
            if not v:
                continue
            # ⚠️ OBJ 文件是**原版 X 取反**的 ⇒ 拿它比之前先翻回来（见 `mesh_colors` 的说明）
            # 🔴 **这里存的是原值、不是量化后的 key**（2026-10-05 改）：比法交给 `geom_hits`
            #   的逐分量容差（见 `TOL` 那段头注释 —— 量化 key 会栽在半值边界上）。
            vpos = [(-p[0], p[1], p[2]) for p in v]
            # ---- 候选怎么来（🔴 别只按名字精确查，实测会**静默配错**）----
            #   · 我们的 OBJ 有一批是**按 `g` 组拆出来的**，文件名 = `<网格名><序号>`
            #     （`Barrels1.obj` / `Banner 11.obj`）；而**别的包里可能恰好有一个真叫 `Barrels1` 的网格**
            #     ⇒ 精确查名会把两个不同的网格配到一起（实测 4 场、共 529 个顶点因此对不上）。
            #   · 所以：候选 = **精确名** ∪ **去掉尾部数字的名字**，再**按几何认领**
            #     （拿 OBJ 的顶点去候选的顶点集里找，命中率最高的胜）——「内容认领」这条
            #     与 `资料/解包数据…判据` 那套「按内容认、不按文件名认」是同一条纪律。
            own = 'scenes_scenes_' + arena
            # 🔴 **先问一句**：本场包 / 共享包里有没有这个网格？
            #   有、而且**它本来就没顶点色** ⇒ 这个 OBJ 不需要色，**直接跳过**（不是缺口；
            #   实测 `Barrels1` / `Banner 31` 这批就是这种 —— 本场有同名网格但无色，
            #   而别的包里恰好有个**同名的带色网格**，只按名字查就会给它贴上错色）。
            SHARED = 'battlesharedresources_assets_all'
            here = names_all.get(mesh_name, set())
            # 🔴 **本场包优先**：我们的 OBJ 就是这个包里的网格导出来的 ——
            #   ① 本场包里有这个网格 ⇒ 以它为准（有色就取它；**没色就跳过**，别去别包找同名的）
            #   ② 本场包没有 ⇒ 看共享包（`arena3/aeldari/leviathan/sororitas` 的网格都在那里）
            st = None
            for b in (own, SHARED):
                if b in here:
                    st = 'colored' if (mesh_name, b) in colored_pair else 'plain'
                    break
            if st == 'plain':
                nocount += 1
                continue
            # ---- 候选：精确名 ∪ 去掉尾部数字的名字，再**按几何认领**（拿 OBJ 的顶点去候选顶点集里找）----
            base = mesh_name.rstrip('0123456789').strip()
            base1 = mesh_name[:-1].strip() if mesh_name[-1:].isdigit() else base
            cands = [c for c in (index.get(mesh_name) or [])
                     if st == 'colored' and c[0] in (own, SHARED) or st is None]
            if not cands:
                # st == 'colored' 但候选被过滤空了（本场有色却没进 index？）⇒ 出声，别静默
                if st == 'colored':
                    print('      ⚠️ %-34s 本场包说它带色，但索引里没有 —— 不写色' % f[:34])
                    miss_items.append('%s(本场包说带色，索引缺失)' % f)
                continue
            for alt in {base, base1}:
                if alt and alt != mesh_name:
                    for c in (index.get(alt) or []):
                        if c not in cands:
                            cands.append(c)
            best, best_hit, best_b, best_dev = None, -1, None, None
            for (b, pos, col) in cands:
                h, dv = geom_hits(vpos, pos)      # 逐分量容差最近邻（见 `TOL` 那段头注释）
                # 同分时**同场优先**（与「优先与材质同包」同一条纪律）
                rank = (0 if b == own else (2 if 'shared' in b else 1))
                if (h, -rank) > (best_hit, -(0 if best_b == own else (2 if best_b and 'shared' in best_b else 1))):
                    best, best_hit, best_b, best_dev = (b, pos, col), h, b, dv
            miss = len(v) - best_hit
            if miss > 0:
                # 🔴 **宁可认不出，也不贴错色**（项目红线）：对不上就**不写这一条**，并出声
                print('      ⚠️ %-34s 认不出（%d 个候选里最好的只命中 %d/%d，最大偏差 %.1e ≥ 容差 %.0e）—— 这一条不写色'
                      % (f[:34], len(cands), best_hit, len(v), best_dev or 0.0, TOL))
                miss_items.append('%s(候选%d个, 最好命中 %d/%d, 最大偏差 %.1e, 候选源=%s)'
                                  % (f, len(cands), best_hit, len(v), best_dev or 0.0, best_b))
                miss_total += miss
                continue
            b, pos, col = best
            hit_total += len(v)
            dev_max = max(dev_max, best_dev or 0.0)   # 认领的**最差**几何偏差（出声用，不写进旁挂）
            items.append({
                'obj': f, 'mesh': mesh_name, 'src': b,
                # 🔴 **扁平数组，不是 `[[x,y,z],...]`** —— Unity 的 `JsonUtility` **不支持交错数组**
                #   （`float[][]` 会**静默变 null** ⇒ 建场期 NRE；2026-09-30 实测踩过）：
                #   `pos` = xyz 三元组依次摊平、`col` = rgba 四元组依次摊平，配对靠下标。
                'n': len(pos),
                # 🔴 **坐标不四舍五入** —— 有的网格小到坐标 ~2e-4（实测 `Candles 52`），
                #   舍到 5 位小数就等于丢掉 1.6% 的量；而建场侧是按**网格自身尺度**做最近邻匹配的
                #   （步长 = 包围盒对角 × 1e-3）⇒ 这里必须给全精度。
                'pos': [v for q in pos for v in q],
                'col': [round(v, 5) for q in col for v in q],
                '_objVerts': len(v), '_hitVerts': best_hit,
            })
        # 有顶点色但工程里没有对应 OBJ 的（那些网格没被建出来 or 名字不同）
        for name, cands in sorted(index.items()):
            if name.endswith('.obj'):
                continue
            if not any(it['mesh'] == name for it in items):
                if any(c[0] == 'scenes_scenes_' + arena for c in cands):
                    unmatched.append(name)
        # 🆕 静态合批的「孤儿源网格」：源网格被烘进合并网格的第 N 段 ⇒ 接到那一段的 OBJ 上
        #    （认不出的一律不接，照实留在 `unmatched` 里 —— 见上面 `static_batch_join` 的头注释）
        sb_items, sb_done = static_batch_join(own, mdir, unmatched, index)
        if sb_done:
            items.extend(sb_items)
            # 保持「items 按 obj 文件名排序」这条不变量（别的 12 场本来就是按 `sorted(objfiles)`
            # 建出来的 ⇒ 这一步对它们是恒等变换，输出逐字节不变）
            items.sort(key=lambda it: it['obj'])
            unmatched = [n for n in unmatched if n not in sb_done]
        print('%-32s 带色网格 %d 个 · OBJ 顶点命中 %d / 漏 %d · 最差几何偏差 %.1e%s%s%s'
              % (arena, len(items), hit_total, miss_total, dev_max,
                 ('（另有 %d 个 OBJ 的原网格本来就没顶点色，跳过）' % nocount) if nocount else '',
                 ('  ✅ 静态合批接上 %d 个' % len(sb_done)) if sb_done else '',
                 ('  ⚠️ 本场有顶点色但没建出来的网格 %d 个' % len(unmatched)) if unmatched else ''))
        if a.check:
            continue
        out = {'arena': arena,
               '_schema': 'arena_vcol/1 —— 原版网格的顶点色（positions **保留原版空间、没有取反** —— 我们的 OBJ 是取反过的、而 Unity 的 OBJ 导入器会再取反一次 ⇒ **Unity 网格的 X ≈ 原版**，按原样比即可）；'
                          '运行时按**位置**匹配到 Unity 顶点，别按索引（OBJ 被按组拆过、Unity 还会 weldVertices）',
               '_source': 'd:/2/.../aa/StandaloneWindows64/{battlesharedresources,scenes_scenes_<场>}.bundle',
               'items': items,
               # ⚠️ 对不上的那些**照实记着**（obj 顶点在源网格里找不到 ⇒ 颜色不可信，运行时别用）
               '_missItems': miss_items,
               'unmatched': unmatched}
        dst = os.path.join('d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas', arena, arena + '_vcol.json')
        io.open(dst, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False))
        print('   写出 %s（%.1f KB）' % (dst, os.path.getsize(dst) / 1024.0))
    return 0


if __name__ == '__main__':
    sys.exit(main())
