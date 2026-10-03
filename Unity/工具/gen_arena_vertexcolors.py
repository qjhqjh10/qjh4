#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""gen_arena_vertexcolors.py —— 把**原版网格的顶点色**抽成旁挂 JSON（13 场逐场一份）。

为什么要它（判据 → `项目任务.md` §三 第 30 条 散件 C 的 2026-09-30 晚那一段）：
  我们想复刻材质关键字 **`_ALPHAMODULATE_ON`**（URP 内置，语义 = 按顶点色调制 alpha），
  而**我们的 OBJ 网格没有顶点色** —— 不是解不出，是 **`UnityPy/export/MeshExporter.py` 不写**
  （它只写 `v/vt/vn/f`；`helpers/MeshHelper.py` 早就把 channel 3 读进 `m_Colors` 了）。
  实测规模：13 场 + `battlesharedresources` 共 **120 个网格**带顶点色（**24,793 个顶点**）；
  我们这 697 个 OBJ 里对得上 **130 个文件**（一个网格被按组拆成多个 OBJ 时会有多份）。

**坐标空间**（实测钉死，🔴 **改错过一次，别想当然**）：
  · 原版顶点 → 我们 **OBJ 文件**的 `v` 是 **X 取反**（`Ground Lights.0011` 原版第 1 个顶点 `+0.0146326`
    ↔ 我们 OBJ `−0.0146326`；同一条链还有**绕序翻转** —— 见 `gen_unity_arena_manifest.py:1478-1495`）；
  · 但 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    （实测：按原版空间比 **命中 186/186**，按取反后比 **0/186**；`DumpMiss` 两种手性各试一遍得到的）。
  ⇒ **本脚本产出的 `pos` 一律是原版空间（不取反）**，建场期直接与 Unity 网格顶点比。
    自检那一步要与我们 **OBJ 文件**比，所以**临时取反**（见下面 `vkeys` 那行）。

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

用法：PYTHONIOENCODING=utf-8 python 工具/gen_arena_vertexcolors.py [--arena <场>] [--check]
"""
import argparse
import collections
import io
import json
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


def item_size(fmt, dim):
    return FMT_SIZE.get(fmt, 4) * dim


def read_channel(raw, stride, ch, n, fmt):
    """按通道取 n 个顶点 → [(v...)]。只支持 fmt 0（Float32）与 2（UNorm8）——实测就这两种。"""
    off, dim = ch['offset'], ch['dimension']
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


def _vd_channels(d):
    """`m_VertexData` 的公共部分 → (raw, stride, n, ch)，取不到就 None。
    （`mesh_colors` 与 `mesh_positions` 两个入口共用这一段，别再各写一份。）"""
    vd = d.get('m_VertexData')
    if not vd:
        return None
    ch = vd.get('m_Channels') or []
    raw = vd.get('m_DataSize')          # ⚠️ UnityPy 把字节 blob 放在这个名字下（不是长度）
    if not isinstance(raw, (bytes, bytearray)):
        return None
    n = vd['m_VertexCount']
    stride = max(c['offset'] + item_size(c['format'], c['dimension']) for c in ch if c.get('dimension'))
    if stride * n > len(raw):
        return None
    return raw, stride, n, ch


def mesh_positions(d):
    """只要**位置**（`mesh_colors` 的裁掉颜色那一半）。
    合并网格（静态合批的产物）**没有顶点色**，但它的顶点位置是那条 join 的尺子。"""
    r = _vd_channels(d)
    if r is None:
        return None
    raw, stride, n, ch = r
    if len(ch) < 1 or not ch[0].get('dimension'):
        return None
    return read_channel(raw, stride, ch[0], n, ch[0]['format'])


def mesh_colors(d):
    """→ (positions, colors) 或 None（没有顶点色 / 格式不支持）。
    positions **保留原版空间、【不】取反**（⚠️ 2026-10-04 更正：原 docstring 写「已按 X 取反」，与同函数正文那句自相矛盾）。"""
    r = _vd_channels(d)
    if r is None:
        return None
    raw, stride, n, ch = r
    if len(ch) < 4 or not ch[3].get('dimension'):
        return None
    pos = read_channel(raw, stride, ch[0], n, ch[0]['format'])
    col = read_channel(raw, stride, ch[3], n, ch[3]['format'])
    if pos is None or col is None:
        return None
    if len(col[0]) == 3:                # 理论上是 4；真遇到 3 就补 alpha
        col = [tuple(c) + (1.0,) for c in col]
    # 🔴 **这里【不】取反 —— 保留原版空间**。实测（2026-09-30 晚，`DumpMiss` 两种手性各试一遍）：
    #   · 我们的 OBJ 文件 = **原版 X 取反**（导出器干的）
    #   · 而 **Unity 的 OBJ 导入器自己还会再取反一次** ⇒ **Unity 网格的 X ≈ 原版**
    #   ⇒ 拿**我们 OBJ 文件**的顶点比时（下面的 `vkeys` 自检），要先**临时取反**；而拿 **Unity 网格**比时应**原样**用原版空间：`原样命中 0/186 · X 取反后命中 186/186`
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
        mpos = mesh_positions(md)
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


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--arena', default=None, help='只处理某一场（默认 13 场全做）')
    ap.add_argument('--check', action='store_true', help='只扫不写')
    a = ap.parse_args()

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
        got = 0
        for o in env.objects:
            if o.type.name != 'Mesh':
                continue
            try:
                d = o.read_typetree()
            except Exception:
                continue
            nm = d.get('m_Name')
            names_all[nm].add(b)
            r = mesh_colors(d)
            if r is None:
                continue
            colored_pair.add((nm, b))
            index[nm].append((b, r[0], r[1]))
            got += 1
        if got:
            print('  %-46s 带顶点色的网格 %d 个' % (b, got))
    print('唯一网格名 %d 个（其中带色 %d 个）' % (len(names_all), len(index)))

    arenas = [a.arena] if a.arena else [b.replace('scenes_scenes_', '') for b in BUNDLES[1:]]
    for arena in arenas:
        mdir = MODELS % arena
        if not os.path.isdir(mdir):
            print('⚠️ 没有 %s' % mdir)
            continue
        objfiles = [f for f in os.listdir(mdir) if f.endswith('.obj')]
        items, unmatched, miss_items, miss_total, hit_total, nocount = [], [], [], 0, 0, 0
        for f in sorted(objfiles):
            mesh_name = f[:-len('.obj')]
            v = obj_vertices(os.path.join(mdir, f))
            if not v:
                continue
            # ⚠️ OBJ 文件是**原版 X 取反**的 ⇒ 拿它比之前先翻回来（见 `mesh_colors` 的说明）
            vkeys = [(round(-p[0], 4), round(p[1], 4), round(p[2], 4)) for p in v]
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
            best, best_hit, best_b = None, -1, None
            for (b, pos, col) in cands:
                ks = set((round(p[0], 4), round(p[1], 4), round(p[2], 4)) for p in pos)
                h = sum(1 for k in vkeys if k in ks)
                # 同分时**同场优先**（与「优先与材质同包」同一条纪律）
                rank = (0 if b == own else (2 if 'shared' in b else 1))
                if (h, -rank) > (best_hit, -(0 if best_b == own else (2 if best_b and 'shared' in best_b else 1))):
                    best, best_hit, best_b = (b, pos, col), h, b
            miss = len(v) - best_hit
            if miss > 0:
                # 🔴 **宁可认不出，也不贴错色**（项目红线）：对不上就**不写这一条**，并出声
                print('      ⚠️ %-34s 认不出（%d 个候选里最好的只命中 %d/%d）—— 这一条不写色'
                      % (f[:34], len(cands), best_hit, len(v)))
                miss_items.append('%s(候选%d个, 最好命中 %d/%d, 候选源=%s)'
                                  % (f, len(cands), best_hit, len(v), best_b))
                miss_total += miss
                continue
            b, pos, col = best
            hit_total += len(v)
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
        print('%-32s 带色网格 %d 个 · OBJ 顶点命中 %d / 漏 %d%s%s%s'
              % (arena, len(items), hit_total, miss_total,
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
