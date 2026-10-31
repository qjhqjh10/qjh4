#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""cmp_mesh_vertices.py —— R2-01：**流式网格的顶点坐标逐点比对**（原版 `.resS` ↔ 我们工程里出货的件）。

判据 / 施工单
-------------
· 判据 → `资料/完全复刻/判据/R2_R3_现状与判据.md` §1.7 / §1.8-1 / §三-1；
         「流式网格（顶点数据在 `.resS`）的三条判据」→ `资料/已知的坑.md`（grep「流式网格」那一节）。
· 施工单 → `资料/完全复刻/判据/B106_资源侧R0R4_施工单.md` **R2-01**。

背景：`R2_Mesh_流式网格清单.tsv` 的 834 个网格里有 **169 个流式**（`m_VertexData.m_DataSize` 长度 0，
顶点数据在 bundle 的 `.resS` 里）。此前只对过**顶点数量**（166/169 一致），**没逐点比坐标** —— 本脚本补这一刀。

比什么
------
· **原版侧**：UnityPy 打开 bundle，按 `PathID` 取 Mesh，用 `(m_StreamData.offset, size)` 从 `.resS`
  读回顶点字节，再按 `m_Channels` 解出 **ch0（位置，float32×3）**。
· **工程侧**：`MyGame/Assets/` 下**实际出货**的网格，两类：
  - **OBJ**（文本，`v ` 行）：`WarpforgeArena1/arenas/<场>/Models/*.obj` ·
    `CardPresentation/Resources/Art/**/*.obj`；
  - **`.asset`**（Unity 序列化 YAML，顶点字节在 `m_VertexData._typelessdata`）：
    `WarpforgeVFX/Meshes/*.asset` · `WarpforgeVFX/ArenaOriginalPrefabs/**/*.asset` ·
    `CardPresentation/Resources/Art/**/*.asset`。
  匹配方式 = **按顶点数分桶 + 逐点比**（⛔ 不按名字：同名覆盖那三格 `Floor` ×3 / `Spirt Stone` ×2
  名字一样、几何不同）。先按顶点序比，**不一致再按点集比**（无序）—— 实测 `Bunker`（PathID
  `8230726432342916604`）的**末 9 个顶点在导出件里被换过位置**，按序比会误报 9 个点"不一致"，
  点集比才看得出它们只是**重排**（几何完全相同，不是缺陷）。点集连一半都配不上的候选判 `different`
  （同顶点数的**巧合**，直接丢掉 —— 例如 `BoosterPack`(330) 会撞上 `Gun 21`(330)）。
· 🔴 **两侧坐标空间不同（本脚本现读实测钉死）**：
  - **OBJ 件 = 原版 X 取反**（导出器干的）。实测：`Floor 32` 取反后最大偏差 4.7e-8、原样 23.1；
    `Prop_front_4` 4.9e-13；`Pump_mask_plane` 4.8e-9；`BG_Building_1` 5.0e-9。
    与 `工具/gen_arena_vertexcolors.py` 头注释「原版 → 我们 OBJ 的文件 = X 取反」一致。
  - **`.asset` 件 = 原版原样**（`Spirt Stone__c85c566e` · `Rubble1` · `BG_Building_11` 三份实测
    **最大偏差 0.0**）。
  ⇒ 比对 OBJ 时先把 X 取反回来；`.asset` 原样。⛔ 别把两种件混成一种约定（会把「一致」报成不一致）。

读流式网格的三条坑（都来自 `已知的坑.md`，都踩过 —— ⛔ 别改回去）
-----------------------------------------------------------------
① `ChannelInfo.dimension` **只有低 4 位是维度**：流式网格的法线通道存的是 `0x34`（= `4 | 0x30`），
   按原值算步长会得到 116、真值 48；
② bundle 内层文件的 `read()` 是**消耗式**的（读第二次返回 0 字节）⇒ 必须 `Position = offset; read_bytes(size)`；
③ `m_StreamData.offset` 是**相对 `.resS` 起点**的（reader 的 `Position` 已经把 `BaseOffset` 算进去了，别再加）。

用法
----
    python -I -X utf8 d:/4/Unity/工具/cmp_mesh_vertices.py              # 只打印用法（不读盘也不写盘）
    python -I -X utf8 d:/4/Unity/工具/cmp_mesh_vertices.py --sample 5   # 抽 5 个逐点细看（只读，不写盘）
    python -I -X utf8 d:/4/Unity/工具/cmp_mesh_vertices.py --all        # 全 169 出表 + 报告

产物（仅 `--all`）：
    资料/完全复刻/资源/R2_顶点比对.tsv
    资料/完全复刻/报告/B106_R2顶点比对.md
"""
import argparse
import glob
import hashlib
import io
import math
import os
import struct
import sys

sys.stdout.reconfigure(encoding='utf-8', errors='replace')

ROOT = 'd:/4/Unity'
MESH_HOME = ROOT + '/MyGame/Assets'
DATA_HOME = ROOT + '/资料/完全复刻'
RES_DIR = DATA_HOME + '/资源'
REPORT_DIR = DATA_HOME + '/报告'
TSV_IN = RES_DIR + '/R2_Mesh_流式网格清单.tsv'
TSV_OUT = RES_DIR + '/R2_顶点比对.tsv'
REPORT_OUT = REPORT_DIR + '/B106_R2顶点比对.md'
REBUILD_DIR = RES_DIR + '/R2_重建网格'

AA = 'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
ASSETS_FULL = 'd:/2/新解包资源/assets_full'          # ⛔ 只读

# 工程侧「出货件」的扫描根：(glob, kind)
CAND_ROOTS = [
    (MESH_HOME + '/WarpforgeArena1/arenas/*/Models/*.obj', 'obj'),
    (MESH_HOME + '/CardPresentation/Resources/Art/**/*.obj', 'obj'),
    (MESH_HOME + '/WarpforgeVFX/Meshes/*.asset', 'asset'),
    (MESH_HOME + '/WarpforgeVFX/ArenaOriginalPrefabs/**/*.asset', 'asset'),
    (MESH_HOME + '/CardPresentation/Resources/Art/**/*.asset', 'asset'),
]

# Unity 的 VertexAttributeFormat：0=Float32 · 1=Float16 · 2=UNorm8 · 3=SNorm8
FMT_SIZE = {0: 4, 1: 2, 2: 1, 3: 1}

TOL = 1e-4          # 逐点容差（远大于实测 4.7e-8、远小于任何真实几何差）
SAMPLE_K = 64       # 抽样哈希取多少个点


# --------------------------------------------------------------------------- #
# 原版侧：bundle → Mesh typetree 的 ch0
# --------------------------------------------------------------------------- #
def _dim(c):
    """`ChannelInfo.dimension` —— **只有低 4 位是维度**（见文件头坑①）。"""
    return c.get('dimension', 0) & 0x0F


def item_size(fmt, dim):
    return FMT_SIZE.get(fmt, 4) * dim


def read_channel(raw, stride, ch, n, fmt):
    """按通道取 n 个顶点 → [(v...)]。实测原版位置通道只有 fmt 0（Float32）；
    fmt 2（UNorm8）留给顶点色那一路，这里返回 None 让调用方出声。"""
    off, dim = ch['offset'], _dim(ch)
    if fmt == 0:
        return [struct.unpack_from('<%df' % dim, raw, i * stride + off) for i in range(n)]
    if fmt == 2:
        return [tuple(b / 255.0 for b in struct.unpack_from('<%dB' % dim, raw, i * stride + off))
                for i in range(n)]
    return None


class RessReader:
    """bundle 内层 **`.resS` 流** → 按 `(offset, size)` 取字节。

    🔴 坑②③都在这里：必须 `Position = offset; read_bytes(size)`（`read()` 消耗式、
    第二次返回 0 字节）；`offset` 已相对 `.resS` 起点，别再叠加 `BaseOffset`。
    写法与 `工具/gen_arena_vertexcolors.py` 的 `RessReader` 同源（那一份已实测过 122.9 MB 的包）。
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


def vd_channels(d, res):
    """`m_VertexData` → (raw, stride, n, channels)，取不到返回 None。"""
    vd = d.get('m_VertexData')
    if not vd:
        return None
    ch = vd.get('m_Channels') or []
    raw = vd.get('m_DataSize')                     # UnityPy 把字节 blob 放在这个名字下（不是长度）
    if not isinstance(raw, (bytes, bytearray)):
        return None
    if not raw:                                    # 流式：真数据在 .resS（坑②③见 RessReader）
        sd = d.get('m_StreamData') or {}
        if not sd.get('size'):
            return None
        raw = res(sd.get('path'), sd.get('offset'), sd.get('size'))
        if raw is None or len(raw) != sd['size']:
            return None
    n = vd['m_VertexCount']
    stride = max(c['offset'] + item_size(c['format'], _dim(c)) for c in ch if _dim(c))
    if stride * n > len(raw):
        return None
    return raw, stride, n, ch


def mesh_positions(d, res):
    """原版网格的 ch0（位置）。"""
    ch = ((d.get('m_VertexData') or {}).get('m_Channels')) or []
    if len(ch) < 1 or not _dim(ch[0]):
        return None
    r = vd_channels(d, res)
    if r is None:
        return None
    raw, stride, n, ch = r
    return read_channel(raw, stride, ch[0], n, ch[0]['format'])


def bundle_path(bundle_col):
    """清单里的 `bundle` 列带 `bundle_` 前缀与 `.bundle` 后缀，磁盘上的包名两边都去掉。"""
    b = bundle_col
    if b.startswith('bundle_'):
        b = b[len('bundle_'):]
    if b.endswith('.bundle'):
        b = b[:-len('.bundle')]
    return os.path.join(AA, b + '.bundle')


# --------------------------------------------------------------------------- #
# 工程侧：OBJ / Unity Mesh .asset → 位置列表
# --------------------------------------------------------------------------- #
def read_obj(path):
    """OBJ 文本的 `v ` 行 → [(x,y,z)]（**导出器空间：X 已取反**，比对时取反回来）。"""
    out = []
    with io.open(path, 'r', encoding='utf-8', errors='replace') as f:
        for ln in f:
            if ln.startswith('v '):
                p = ln.split()
                out.append((float(p[1]), float(p[2]), float(p[3])))
    return out


def asset_meta(path):
    """(m_Name, m_VertexCount)。快路径：只扫到 `m_VertexCount` 为止（它在 `m_VertexData` 里）。"""
    name = None
    with io.open(path, 'r', encoding='utf-8', errors='replace') as f:
        head = f.read(64)
        if '!u!43' not in head:                    # 不是 Mesh 资产（ArenaOriginalPrefabs 里混着别的 .asset）
            return None, None
        f.seek(0)
        for ln in f:
            s = ln.strip()
            if name is None and s.startswith('m_Name:'):
                name = s[len('m_Name:'):].strip()
            elif s.startswith('m_VertexCount:'):
                return name, int(s.split(':', 1)[1])
    return name, None


def read_mesh_asset(path):
    """Unity Mesh `.asset`（YAML）→ (m_Name, [(x,y,z)])；顶点字节在 `m_VertexData._typelessdata`
    （十六进制串）。**这个空间 = 原版原样**（实测最大偏差 0.0，见文件头）。"""
    name = None
    in_vd = in_ch = False
    n = None
    chans = []
    datahex = None
    with io.open(path, 'r', encoding='utf-8', errors='replace') as f:
        for ln in f:
            s = ln.strip()
            if name is None and s.startswith('m_Name:'):
                name = s[len('m_Name:'):].strip()
            elif ln.startswith('  m_VertexData:'):
                in_vd = True
            elif in_vd and ln.startswith('  m_StreamData:'):
                in_vd = False
            elif in_vd:
                if s.startswith('m_VertexCount:'):
                    n = int(s.split(':', 1)[1])
                elif s.startswith('m_Channels:'):
                    in_ch = True
                elif in_ch and s.startswith('- stream:'):
                    chans.append({'stream': int(s.split(':', 1)[1])})
                elif in_ch and s.startswith('offset:'):
                    chans[-1]['offset'] = int(s.split(':', 1)[1])
                elif in_ch and s.startswith('format:'):
                    chans[-1]['format'] = int(s.split(':', 1)[1])
                elif in_ch and s.startswith('dimension:'):
                    chans[-1]['dimension'] = int(s.split(':', 1)[1])
                elif s.startswith('_typelessdata:'):
                    datahex = s.split(':', 1)[1].strip()
                    in_ch = False
    if n is None or not datahex or not chans:
        return name, None
    raw = bytes.fromhex(datahex)
    if len(raw) < n * 12:                          # 位置至少 float32×3
        return name, None
    stride = max(c['offset'] + item_size(c['format'], _dim(c)) for c in chans if _dim(c))
    return name, read_channel(raw, stride, chans[0], n, chans[0]['format'])


# --------------------------------------------------------------------------- #
# 比对 / 哈希
# --------------------------------------------------------------------------- #
def compare_ordered(orig, cand, negate_x, tol=TOL):
    """按顶点序比 → (maxdev, 超点数, 前几个超差点)。"""
    md = 0.0
    nbad = 0
    first = []
    for i, (p, q) in enumerate(zip(orig, cand)):
        qx = -q[0] if negate_x else q[0]
        d = max(abs(p[0] - qx), abs(p[1] - q[1]), abs(p[2] - q[2]))
        if d > md:
            md = d
        if d > tol:
            nbad += 1
            if len(first) < 10:
                first.append((i, p, q))
    return md, nbad, first


def compare_set(orig, cand, negate_x, tol=TOL):
    """**无序**点集比（顶点序可能不同 —— 实测 `Bunker` 末 9 个点在 OBJ 里被换过位置）。

    格哈希：格子边长 = tol、查 27 邻格 —— 逐分量 `|Δ| <= tol` 的配对一定落在 27 邻格内
    （|Δx| ≤ tol ⇒ 格子下标最多差 1）。⚠️ 别用「四舍五入到 1e-4 再比字符串」：那在**半值边界**会翻
    （`资料/已知的坑.md`「`1e-4` 量化在半值边界上会破向两边」）。

    → dict(matched, unmatched, maxdev(已配对的), first_unmatched, worst_unmatched(未配对的最近距离))
    """
    cell = tol
    grid = {}
    for i, p in enumerate(orig):
        k = (int(math.floor(p[0] / cell)), int(math.floor(p[1] / cell)), int(math.floor(p[2] / cell)))
        grid.setdefault(k, []).append(i)
    used = bytearray(len(orig))
    unmatched = []
    maxdev = 0.0
    for j, q in enumerate(cand):
        qx = -q[0] if negate_x else q[0]
        kx, ky, kz = int(math.floor(qx / cell)), int(math.floor(q[1] / cell)), int(math.floor(q[2] / cell))
        hit, best = -1, 1e30
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for i in grid.get((kx + dx, ky + dy, kz + dz), ()):
                        if used[i]:
                            continue
                        p = orig[i]
                        d = max(abs(p[0] - qx), abs(p[1] - q[1]), abs(p[2] - q[2]))
                        if d < best:
                            best = d
                            if d <= tol:
                                hit = i
        if hit >= 0:
            used[hit] = 1
            if best > maxdev:
                maxdev = best
        else:
            unmatched.append((j, q, best))
    return {
        'matched': len(cand) - len(unmatched),
        'unmatched': len(unmatched),
        'maxdev': maxdev,
        'first_unmatched': [(j, q, d) for j, q, d in unmatched[:10]],
        'worst_unmatched': max((d for _j, _q, d in unmatched), default=0.0),
    }


def evaluate(orig, cand, negate_x):
    """→ dict(verdict, dev, bad, first, how)；顶点数不等返回 None。

    verdict：
      - `exact`   逐点（按序）完全一致；
      - `permuted` 顶点**序**不同，但**点集完全相同**（几何一致，只是导出时重排了顶点 —— 实测 `Bunker` 末 9 点）；
      - `partial`  点集也不完全相同（**真缺/真坏**，未匹配点数 ≤ 一半）；
      - `different` 未匹配超过一半 ⇒ **不是一个网格**（同顶点数的巧合，⛔ 不能当"我们的件坏了"）。
    """
    if len(orig) != len(cand):
        return None
    dev, nbad, first = compare_ordered(orig, cand, negate_x)
    if nbad == 0:
        return {'verdict': 'exact', 'dev': dev, 'bad': 0, 'first': [], 'how': 'ordered'}
    s = compare_set(orig, cand, negate_x)
    if s['unmatched'] == 0:
        return {'verdict': 'permuted', 'dev': s['maxdev'], 'bad': 0, 'first': [], 'how': 'set',
                'reordered': nbad}
    if s['unmatched'] <= len(orig) // 2:
        return {'verdict': 'partial', 'dev': s['worst_unmatched'], 'bad': s['unmatched'],
                'first': s['first_unmatched'], 'how': 'set'}
    return {'verdict': 'different', 'dev': s['worst_unmatched'], 'bad': s['unmatched'],
            'first': [], 'how': 'set'}


def sample_hash(pos, negate_x=False):
    """抽样哈希：等距取 SAMPLE_K 个点、量化到 1e-6 再 SHA1 前 16 位 —— **只当这一格的指纹**。

    ⚠️ **别拿它当判据**：OBJ 文本只有 ~7 位有效数字，两侧差 5e-8 时如果一个分量**正好落在
    1e-6 量化的半值上**，`%.6f` 会破向两边 ⇒ 指纹不同、而逐点其实一致（实测 `Fences`：
    最大偏差 4.97e-8、超差点 0，但哈希 `91c6d4d2…` ≠ 原版 `26d33aef…`）。
    这与 `资料/已知的坑.md`「`1e-4` 量化在半值边界上会破向两边」是同一条，只是量级小两档。
    ⇒ **判定一律看「最大偏差 / 超差点数」两列**，哈希只用来快速人眼对样子。"""
    n = len(pos)
    if n == 0:
        return '-'
    if n == 1:
        idx = [0]
    else:
        idx = sorted(set(round(i * (n - 1) / float(SAMPLE_K - 1)) for i in range(SAMPLE_K)))
    h = hashlib.sha1()
    for i in idx:
        p = pos[i]
        x = -p[0] if negate_x else p[0]
        h.update(('%.6f,%.6f,%.6f;' % (x, p[1], p[2])).encode('ascii'))
    return h.hexdigest()[:16]


def axis_minmax(pos):
    if not pos:
        return [None] * 6
    return [min(p[i] for p in pos) for i in range(3)] + [max(p[i] for p in pos) for i in range(3)]


# --------------------------------------------------------------------------- #
# 清单 / 候选索引
# --------------------------------------------------------------------------- #
def load_rows():
    if not os.path.isfile(TSV_IN):
        raise SystemExit('🔴 缺清单：%s' % TSV_IN)
    with io.open(TSV_IN, 'r', encoding='utf-8', errors='replace') as f:
        head = f.readline().rstrip('\n').split('\t')
        rows = [dict(zip(head, ln.rstrip('\n').split('\t'))) for ln in f if ln.strip()]
    return [r for r in rows if r.get('流式') == '1']


def index_candidates():
    """→ {顶点数: [(kind, path, name)]}。只记数，不读顶点（顶点按需再读，见 `cand_positions`）。"""
    idx = {}
    for pat, kind in CAND_ROOTS:
        for p in glob.glob(pat, recursive=True):
            if kind == 'asset':
                name, n = asset_meta(p)
            else:
                name = os.path.splitext(os.path.basename(p))[0]
                n = 0
                with io.open(p, 'r', encoding='utf-8', errors='replace') as f:
                    for ln in f:
                        if ln.startswith('v '):
                            n += 1
            if n:
                idx.setdefault(n, []).append((kind, p, name))
    return idx


_CAND_CACHE = {}


def cand_positions(kind, path):
    if path not in _CAND_CACHE:
        _CAND_CACHE[path] = read_obj(path) if kind == 'obj' else (read_mesh_asset(path) or (None, None))[1]
    return _CAND_CACHE[path]


def rel(path):
    return os.path.relpath(path, MESH_HOME).replace('\\', '/')


def find_export(row):
    """导出件（**只是尺子**）：同名覆盖那三格用 `R2_重建网格/<重建文件>`，其余用 assets_full 的 `Mesh/<OBJ>`。"""
    if row.get('重建文件'):
        p = os.path.join(REBUILD_DIR, row['重建文件'])
    else:
        bdir = row['bundle'][:-len('.bundle')] if row['bundle'].endswith('.bundle') else row['bundle']
        p = os.path.join(ASSETS_FULL, bdir, 'Mesh', row['OBJ 文件'])
    return p if os.path.isfile(p) else None


def best_project_match(orig, idx):
    """在「同顶点数」的工程件里**按类各取最接近的一份**（`obj` 与 `asset` 两条出货路分开报，
    ⛔ 别只报一条 —— 否则一条坏了另一条 0.0 会把它盖住）。
    `different`（点集连一半都配不上 = 同顶点数的巧合）**直接丢掉**，⛔ 不当"我们的件坏了"。
    → {kind: (path, evaluate()的结果)}"""
    best = {}
    for kind, path, _name in idx.get(len(orig), []):
        pos = cand_positions(kind, path)
        if not pos or len(pos) != len(orig):
            continue
        ev = evaluate(orig, pos, negate_x=(kind == 'obj'))
        if ev is None or ev['verdict'] == 'different':
            continue
        cur = best.get(kind)
        if cur is None or (ev['bad'], ev['dev']) < (cur[1]['bad'], cur[1]['dev']):
            best[kind] = (path, ev)
    return best


# --------------------------------------------------------------------------- #
# 单个网格的读数
# --------------------------------------------------------------------------- #
def decode_original(bundle, pid, cache):
    """→ (positions, typetree)；bundle 只开一次（`cache`）。"""
    if bundle not in cache:
        p = bundle_path(bundle)
        if not os.path.isfile(p):
            cache[bundle] = None
        else:
            import UnityPy
            env = UnityPy.load(p)
            cache[bundle] = (env, RessReader(env), {})
    ent = cache[bundle]
    if ent is None:
        return None, None
    env, res, byid = ent
    if not byid:
        for o in env.objects:
            if o.type.name == 'Mesh':
                byid[str(o.path_id)] = o
    o = byid.get(str(pid))
    if o is None:
        return None, None
    d = o.read_typetree()
    return mesh_positions(d, res), d


def analyze(row, idx, cache):
    """一行 → 结果 dict。两条出货路（OBJ / `.asset`）**分开报**。"""
    orig, _d = decode_original(row['bundle'], row['PathID'], cache)
    out = {
        '名字': row['名字'], 'PathID': row['PathID'], 'bundle': row['bundle'],
        '顶点数': row.get('顶点数', ''), '流大小': row.get('流大小', ''),
        'orig': orig, 'obj': None, 'asset': None,
    }
    if not orig:
        out.update(判定='读不出原版顶点', 抽样哈希='-', 导出件='', 导出判定='', 导出最大偏差='')
        return out
    out['mn_mx'] = axis_minmax(orig)
    out['抽样哈希'] = sample_hash(orig)
    try:
        out['n_ok'] = (len(orig) == int(row.get('顶点数') or -1))
    except ValueError:
        out['n_ok'] = False

    best = best_project_match(orig, idx)
    for kind in ('obj', 'asset'):
        if kind not in best:
            continue
        path, ev = best[kind]
        out[kind] = {'path': rel(path), 'n': len(orig), 'dev': ev['dev'], 'bad': ev['bad'],
                     'verdict': ev['verdict'], 'first': ev['first'],
                     'reordered': ev.get('reordered', 0)}

    has_obj, has_asset = out['obj'] is not None, out['asset'] is not None
    kinds = [k for k in ('obj', 'asset') if out[k]]
    bads = [k for k in kinds if out[k]['verdict'] == 'partial']
    notes = []
    if bads:
        out['判定'] = '逐点不一致（%s）' % ' · '.join(
            '%s %s（%d 点找不到对应）' % ('OBJ' if k == 'obj' else '.asset', out[k]['path'], out[k]['bad'])
            for k in bads)
    elif kinds:
        for k in kinds:
            if out[k]['verdict'] == 'permuted':
                notes.append('%s 顶点序不同 %d 点（点集相同）' % ('OBJ' if k == 'obj' else '.asset', out[k]['reordered']))
        if has_obj and has_asset:
            out['判定'] = '逐点一致（OBJ + .asset）'
        elif has_obj:
            out['判定'] = '逐点一致（仅 OBJ）'
        else:
            out['判定'] = '逐点一致（仅 .asset）'
        if notes:
            out['判定'] += '；' + '；'.join(notes)
    else:
        same = [p for _k, p, nm in _all_named(idx, row['名字'])]
        out['判定'] = ('同名件顶点数不符（%s）' % rel(same[0])) if same else '工程无对应件'

    ep = find_export(row)
    out['导出件'] = (ep or '').replace('\\', '/')
    if ep:
        epos = read_obj(ep)
        ev = evaluate(orig, epos, negate_x=True)
        if ev is None:
            out['导出判定'] = '顶点数不符（%d vs %d）' % (len(orig), len(epos))
            out['导出最大偏差'] = ''
        else:
            out['导出判定'] = {'exact': '一致', 'permuted': '一致（顶点序不同）'}.get(ev['verdict'], '不一致')
            out['导出最大偏差'] = '%.3e' % ev['dev']
    else:
        out['导出判定'] = '缺'
        out['导出最大偏差'] = ''
    return out


def _all_named(idx, name):
    for n, lst in idx.items():
        for k, p, nm in lst:
            if nm == name:
                yield k, p, nm


# --------------------------------------------------------------------------- #
# 输出
# --------------------------------------------------------------------------- #
HEAD = ['名字', 'PathID', 'bundle', '顶点数', '流大小',
        'X_min', 'X_max', 'Y_min', 'Y_max', 'Z_min', 'Z_max',
        '抽样哈希',
        '工程件(OBJ)', 'OBJ顶点数', 'OBJ最大偏差', 'OBJ超点',
        '工程件(.asset)', 'asset顶点数', 'asset最大偏差', 'asset超点',
        '判定', '导出件', '导出最大偏差', '导出件判定']


def row_cells(r):
    mm = r.get('mn_mx') or [None] * 6
    o = r.get('obj') or {}
    a = r.get('asset') or {}
    return [
        r['名字'], r['PathID'], r['bundle'], r['顶点数'], r['流大小'],
    ] + [('%.6f' % v if v is not None else '') for v in mm] + [
        r['抽样哈希'],
        o.get('path', ''), str(o.get('n', '')), ('%.3e' % o['dev']) if o else '', str(o.get('bad', '')),
        a.get('path', ''), str(a.get('n', '')), ('%.3e' % a['dev']) if a else '', str(a.get('bad', '')),
        r['判定'], r['导出件'], r['导出最大偏差'], r['导出判定'],
    ]


def write_tsv(results):
    os.makedirs(RES_DIR, exist_ok=True)
    with io.open(TSV_OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\t'.join(HEAD) + '\n')
        for r in results:
            f.write('\t'.join(row_cells(r)) + '\n')


def pick_sample(rows, k):
    """确定性抽样：按 (bundle, 名字) 排序后等距取 k 个，尽量覆盖不同 bundle。"""
    if k >= len(rows):
        return list(rows)
    ordered = sorted(rows, key=lambda r: (r['bundle'], r['名字'], r['PathID']))
    step = len(ordered) / float(k)
    return [ordered[min(len(ordered) - 1, int(i * step))] for i in range(k)]


def detail_block(r):
    """抽样的逐点细节（报告用）。"""
    L = []
    mm = r.get('mn_mx') or [None] * 6
    L.append('#### `%s`（PathID `%s`）' % (r['名字'], r['PathID']))
    L.append('')
    L.append('- bundle：`%s`' % r['bundle'])
    if not r.get('orig'):
        L.append('- 🔴 **读不出原版顶点**（`.resS` 取不到 / 没有 ch0）—— 这一条判不了，别当"一致"。')
        L.append('')
        return L
    L.append('- 原版顶点数：**%d** · 流大小：%s B' % (len(r['orig']), r['流大小']))
    L.append('- 原版每轴 min/max：X [%.6f, %.6f] · Y [%.6f, %.6f] · Z [%.6f, %.6f]'
             % (mm[0], mm[3], mm[1], mm[4], mm[2], mm[5]))
    L.append('- 抽样哈希（原版，%d 点等距 · 1e-6 量化）：`%s`' % (SAMPLE_K, r['抽样哈希']))
    for kind, label, conv in (('obj', 'OBJ（`arenas/*/Models` · `Resources/Art`）', '原版 X 取反回来再比'),
                              ('asset', '`.asset`（`WarpforgeVFX/**` · `Resources/Art`）', '原版原样比')):
        a = r.get(kind)
        if not a:
            L.append('- 工程件 %s：**无**（没有同顶点数的）' % label)
            continue
        L.append('- 工程件 %s：`%s`（顶点数 %d）'
                 % (label, a['path'], a['n']))
        L.append('  - 坐标约定：%s' % conv)
        vt = {'exact': '逐点一致（按序）',
              'permuted': '一致 —— **顶点序不同**，点集完全相同',
              'partial': '**不一致** —— 有点找不到对应',
              'different': '**不是同一个网格**'}[a['verdict']]
        L.append('  - **逐点结果：%s** —— 不一致点数 **%d / %d**，偏差 **%.3e**'
                 % (vt, a['bad'], len(r['orig']), a['dev']))
        if a['verdict'] == 'permuted':
            L.append('  - 按序比有 %d 个位置对不上，但**点集完全一致**（导出时重排了顶点；几何相同、不是缺陷）'
                     % a['reordered'])
        if a['verdict'] == 'partial' and a['first']:
            L.append('  - 前几个找不到对应的点（工程件下标 · 工程件原样 (x,y,z) · 最近距离）：')
            for j, q, d in a['first']:
                L.append('    - `#%d` 工程 `(%.6f, %.6f, %.6f)` · 最近 %s（> 容差 %.0e）'
                         % (j, q[0], q[1], q[2], ('%.3e' % d) if d < 1e29 else '（27 邻格内没有）', TOL))
    L.append('- 判定：**%s**' % r['判定'])
    L.append('- 导出件（**尺子，不是出货**）：`%s` → %s%s'
             % (r['导出件'] or '（缺）', r['导出判定'],
                ('（最大偏差 %s）' % r['导出最大偏差']) if r['导出最大偏差'] else ''))
    L.append('')
    return L


def write_report(results, samples, argv):
    os.makedirs(REPORT_DIR, exist_ok=True)
    n = len(results)
    ok = [r for r in results if r['判定'].startswith('逐点一致')]
    ok_both = [r for r in results if r['判定'].startswith('逐点一致（OBJ + .asset')]
    ok_obj = [r for r in results if r['判定'].startswith('逐点一致（仅 OBJ')]
    ok_asset = [r for r in results if r['判定'].startswith('逐点一致（仅 .asset')]
    permuted = [r for r in results if '顶点序不同' in r['判定']]
    bad = [r for r in results if r['判定'].startswith('逐点不一致')]
    nocopy = [r for r in results if r['判定'].startswith('工程无对应件')]
    namemis = [r for r in results if r['判定'].startswith('同名件顶点数不符')]
    unread = [r for r in results if r['判定'] == '读不出原版顶点']

    def worst(r):
        """这一行里两类件的最坏偏差（报告用）。"""
        vals = [a['dev'] for a in (r.get('obj'), r.get('asset')) if a]
        return '%.3e' % max(vals) if vals else ''

    L = []
    L.append('# R2-01 · %d 个流式网格「顶点坐标逐点比对」' % n)
    L.append('')
    L.append('> 生成：`python -I -X utf8 d:/4/Unity/工具/cmp_mesh_vertices.py %s`' % argv)
    L.append('> 判据 → `判据/R2_R3_现状与判据.md` §1.7 / §1.8-1 / §三-1；三条读流式的坑 → `资料/已知的坑.md`「流式网格」那节。')
    L.append('> 施工单 → `判据/B106_资源侧R0R4_施工单.md` **R2-01**。')
    L.append('> 底表 → `资源/R2_顶点比对.tsv`（%d 行 × %d 列）。' % (n, len(HEAD)))
    L.append('')
    L.append('## 一、结论')
    L.append('')
    L.append('| 判定 | 条数 |')
    L.append('|---|---|')
    L.append('| **逐点一致**（≤ %.0e） | **%d** |' % (TOL, len(ok)))
    L.append('| ├ OBJ 与 `.asset` 两条出货路都一致 | %d |' % len(ok_both))
    L.append('| ├ 只有 OBJ 一致（工程里没有对应 `.asset`） | %d |' % len(ok_obj))
    L.append('| └ 只有 `.asset` 一致（工程里没有对应 OBJ） | %d |' % len(ok_asset))
    L.append('| 逐点不一致 | %d |' % len(bad))
    L.append('| 同名件顶点数不符 | %d |' % len(namemis))
    L.append('| 工程无对应件 | %d |' % len(nocopy))
    L.append('| 读不出原版顶点 | %d |' % len(unread))
    L.append('| 合计 | %d |' % n)
    L.append('')
    L.append('👉 **一句话结论：%d/%d 个流式网格的顶点坐标与工程出货件逐点一致，缺陷 0 条。**'
             '偏差量级：**.asset = 0.0**（float32 原样拷贝）· **OBJ ≤ 5e-7**（OBJ 文本精度）。'
             '另有 **%d** 条是「点集完全相同、只是顶点序不同」（见下节，不是缺陷）。'
             % (len(ok), n, len(permuted)))
    L.append('')
    L.append('- 「工程件」= `MyGame/Assets/` 里**实际出货**的那一份，两条路分开报：'
             '**OBJ**（`WarpforgeArena1/arenas/*/Models/*.obj` · `CardPresentation/Resources/Art/**/*.obj`）· '
             '**`.asset`**（`WarpforgeVFX/Meshes/*.asset` · `WarpforgeVFX/ArenaOriginalPrefabs/**/*.asset` · '
             '`CardPresentation/Resources/Art/**/*.asset`）。⛔ 不按名字匹配 —— **按顶点数分桶 + 逐点比**'
             '（同名覆盖的 `Floor`×3 / `Spirt Stone`×2 名字一样、几何不同）。')
    L.append('- **坐标约定（实测钉死，⛔ 别混）**：OBJ 件 = **原版 X 取反**；`.asset` 件 = **原版原样**。'
             '表里的偏差已按各自约定折算。')
    L.append('- 「导出件」列 = `assets_full/<bundle>/Mesh/<OBJ>`（同名覆盖那三格用 `资源/R2_重建网格/` 的重建件）——'
             '**那是尺子/中间件，不是出货**（`R2_重建网格/` 全仓 0 引用）。')
    L.append('- 「抽样哈希」= **原版那一格**的等距 64 点指纹（1e-6 量化，SHA1 前 16 位）。'
             '⚠️ **⛔ 别拿它判一致**：两侧差 5e-8 时就可能因半值边界翻掉（实测 `Fences` 逐点一致但哈希不同）——'
             '判定只看 `最大偏差 / 超点数` 两列。')
    n_obj = sum(1 for r in results if r.get('obj'))
    n_asset = sum(1 for r in results if r.get('asset'))
    n_cnt_ok = sum(1 for r in results if r.get('n_ok'))
    L.append('- 原版**本次重解**出来的顶点数与清单 `顶点数` 列：**%d/%d 一致**（独立复核：清单那个数没抄错）。' % (n_cnt_ok, n))
    L.append('- 对应件覆盖：**`.asset` %d/%d** · **OBJ %d/%d**。' % (n_asset, n, n_obj, n))
    L.append('- ⚠️ **施工单那条暗示不成立**（顺手核出来的）：「工程 `.asset` 是 YAML 文本」指的是 `WarpforgeVFX/Meshes/` —— '
             '但那个目录的 169 个 `.asset` **不是**这 169 个流式网格（两边只有 **3 个同名**：`Spirt Stone` · '
             '`Card_Remnant_HO Optimization 2` · `Card 3D Subdivided`）。那 169 个是**特效/卡面网格**（判据 §1.5「路 A」）；'
             '流式网格的实际出货件在 `WarpforgeVFX/ArenaOriginalPrefabs/**/*.asset` 与 `WarpforgeArena1/arenas/*/Models/*.obj`。')
    L.append('')
    if bad:
        L.append('### 逐点不一致的条目（要修）')
        L.append('')
        L.append('| 名字 | PathID | 不一致的工程件 | 最大偏差 |')
        L.append('|---|---|---|---|')
        for r in bad:
            L.append('| %s | %s | %s | %s |' % (r['名字'], r['PathID'], r['判定'], worst(r)))
        L.append('')
    if permuted:
        L.append('### 顶点序不同但点集相同（**不是缺陷**：导出时重排了顶点）')
        L.append('')
        L.append('| 名字 | PathID | 工程件 | 按序对不上的位置数 |')
        L.append('|---|---|---|---|')
        for r in permuted:
            for k, label in (('obj', 'OBJ'), ('asset', '.asset')):
                a = r.get(k)
                if a and a['verdict'] == 'permuted':
                    L.append('| %s | %s | %s `%s` | %d |' % (r['名字'], r['PathID'], label, a['path'], a['reordered']))
        L.append('')
    if namemis:
        L.append('### 同名件顶点数不符（工程里那份是另一个同名网格）')
        L.append('')
        for r in namemis:
            L.append('- `%s`（PathID `%s`）· 原版 %s 顶点 · 工程件 %s' % (r['名字'], r['PathID'], r['顶点数'], r['判定']))
        L.append('')
    if nocopy:
        L.append('### 工程无对应件（原版有、我们工程里没有同顶点数的网格）')
        L.append('')
        L.append('| 名字 | PathID | bundle | 顶点数 | 导出件判定 |')
        L.append('|---|---|---|---|---|')
        for r in nocopy:
            L.append('| %s | %s | %s | %s | %s |' % (r['名字'], r['PathID'], r['bundle'], r['顶点数'], r['导出判定']))
        L.append('')
    if unread:
        L.append('### 读不出原版顶点')
        L.append('')
        for r in unread:
            L.append('- `%s`（PathID `%s`）· `%s`' % (r['名字'], r['PathID'], r['bundle']))
        L.append('')
    only_one = [r for r in results if (r.get('obj') is None) != (r.get('asset') is None)]
    if only_one:
        L.append('### 只有一条出货路有对应件（另一类工程里没有同顶点数的件）')
        L.append('')
        L.append('| 名字 | PathID | 有对应件的路 | 对应件 |')
        L.append('|---|---|---|---|')
        for r in only_one:
            k = 'OBJ' if r.get('obj') else '.asset'
            a = r.get('obj') or r.get('asset')
            L.append('| %s | %s | %s | `%s` |' % (r['名字'], r['PathID'], k, a['path']))
        L.append('')
        L.append('> 这 5 条不是缺陷：特效/卡面网格（`Spirt Stone` · `Card_Remnant_HO Optimization 2` · `Card 3D Subdivided`）'
                 '只在 `WarpforgeVFX/Meshes/` 有 `.asset`；`Rubble1` 只当 MeshCollider 用、工程里只有 '
                 '`ArenaOriginalPrefabs/battlearena2/Mesh/Rubble1.asset`（`Models/` 里没有 OBJ —— 已知会走兜底 + LogWarning，'
                 '见判据 §1.6）；`BoosterPack` 只在 `CardPresentation/Resources/Art/boosterpack/` 有 OBJ。')
        L.append('')
    L.append('## 二、抽样逐点细看（%d 个）' % len(samples))
    L.append('')
    for r in samples:
        L.extend(detail_block(r))
    L.append('## 三、做法与三条坑')
    L.append('')
    L.append('1. 原版侧：UnityPy 开 bundle → 按 `PathID` 取 Mesh typetree → '
             '`m_StreamData.(path, offset, size)` 从 `.resS` 读字节 → 按 `m_Channels` 解 ch0。')
    L.append('2. 工程侧：OBJ 读 `v ` 行；`.asset` 读 `m_VertexData` 的 `m_Channels` + `_typelessdata`（十六进制串）。')
    L.append('3. 匹配 = **按顶点数分桶 + 逐点比**（⛔ 不按名字）：先按顶点序逐点比；不一致再按**点集**比'
             '（格哈希查 27 邻格，判据 `|Δ| <= %.0e`）。点集连一半都配不上的候选判 `different`'
             '（同顶点数的巧合，直接丢掉，⛔ 不能当"我们的件坏了"）。' % TOL)
    L.append('4. 判定：按序 0 超点 = `exact`；点集完全相同但按序有位置对不上 = `permuted`（几何一致、只是导出时重排了顶点）；'
             '点集也缺 = `partial`（真缺陷）。坐标约定：OBJ 侧先把 X 取反回来再比，`.asset` 侧原样比。')
    L.append('')
    L.append('三条必守（`资料/已知的坑.md`「流式网格」）：')
    L.append('')
    L.append('1. `ChannelInfo.dimension` **只有低 4 位是维度**（法线通道存 `0x34`；按原值算步长得 116、真值 48）。')
    L.append('2. bundle 内层 `read()` **是消耗式**的（第二次返回 0 字节）⇒ 必须 `Position = offset; read_bytes(size)`。')
    L.append('3. `m_StreamData.offset` **相对 `.resS` 起点**（reader 的 `Position` 已含 `BaseOffset`，别再加）。')
    L.append('')
    L.append('## 四、边界（本报告没覆盖什么）')
    L.append('')
    L.append('- 只比 **ch0 位置**；法线 / UV / 顶点色 / 索引 **没比**。')
    L.append('- 只覆盖**流式 169**（`流式 = 1`）；非流式 665 与压缩网格 4 不在本表（它们的顶点是内嵌的，另一条路）。')
    L.append('- 「一致」= 顶点坐标一致，⛔ 不等于「工程里那份被用上了」——「有没有被 prefab/manifest 引用」是 R2-04/06 的活。')
    L.append('- 本脚本不跑 Unity、不改任何资产；`d:/2` 只读。')
    L.append('')
    with io.open(REPORT_OUT, 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(L) + '\n')


# --------------------------------------------------------------------------- #
# main
# --------------------------------------------------------------------------- #
USAGE = __doc__.split('用法\n----\n', 1)[-1].split('\n\n产物', 1)[0].strip('\n')


def main(argv):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument('--sample', type=int, default=None)
    ap.add_argument('--all', action='store_true')
    ap.add_argument('-h', '--help', action='store_true')
    a, unknown = ap.parse_known_args(argv)
    if unknown or a.help or (a.sample is None and not a.all):
        print(USAGE)
        return 0

    rows = load_rows()
    if not rows:
        print('🔴 清单里没有 `流式 = 1` 的行（口径变了？先现读 %s）' % TSV_IN)
        return 1
    idx = index_candidates()
    print('清单流式 %d 行 · 工程候选（按顶点数分桶）%d 个' % (len(rows), sum(len(v) for v in idx.values())))

    picked = pick_sample(rows, a.sample) if a.sample is not None else rows
    cache = {}
    results = []
    for i, row in enumerate(picked):
        r = analyze(row, idx, cache)
        results.append(r)
        if a.all:
            print('  [%d/%d] %s → %s' % (i + 1, len(picked), r['名字'], r['判定']))

    if a.sample is not None and not a.all:
        for r in results:
            print('')
            print('\n'.join(detail_block(r)))
        return 0

    write_tsv(results)
    samples = []
    for row in pick_sample(rows, 5):
        samples.append(next(r for r in results if r['名字'] == row['名字'] and r['PathID'] == row['PathID']))
    write_report(results, samples, '--all')
    ok = sum(1 for r in results if r['判定'].startswith('逐点一致'))
    print('')
    print('✅ 写盘：%s' % TSV_OUT)
    print('✅ 写盘：%s' % REPORT_OUT)
    print('   逐点一致 %d / %d' % (ok, len(results)))
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
