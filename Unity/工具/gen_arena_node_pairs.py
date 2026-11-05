# -*- coding: utf-8 -*-
"""gen_arena_node_pairs.py —— 13 场「原版 arena prefab 节点 ↔ 我方 prefab 节点」配对表生成器。

判据方案：`资料/完全复刻/判据/B146_A31_A32_审计.md` §五（C-11 / C-13）。
数据源（全是本地现成件，⛔ 不开 Unity）：
  · 原版 = `MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs/<arena>/<arena>__Scenario.prefab`
  · 我方 = `MyGame/Assets/Resources/ArenaPrefabs/<arena>.prefab`

配对口径（§五 A.3，三级键）：
  ① **逻辑名路径**：折叠 `mesh` / `Mesh (n)` 这层「渲染载体」子节点 —— 我方把 MeshFilter/MeshRenderer
     挂在名为 `mesh` 的子件上，原版挂在逻辑节点（`Cannon 1` / `Fences` / `Vehicle`…）上。
  ② **组件类别向量**：P 粒子(198/199) · M 网格(33/23) · L 光(108) · A Animator(95) · R LineRenderer(120) ·
     C 碰撞体(64/65)。⚠️ **不计 MonoBehaviour(114)** —— 我方每个节点都额外挂了自家脚本
     （`WarpforgeEffectBinder` 等），把它算进签名会让「原版有的节点」全部配不上。
  ③ **字段向量**：Transform 的 pos/rot/scale + GameObject 的 layer/active + 各类组件的关键字段。
  匹配 = 「叶子名 + 类别签名」→ 不唯一时取 **字段差数最小**；⛔ **不按 pathID**、⛔ **不按子节点次序**
  （AssetRipper 会重排子节点，见 `判据/B146_A31_A32_审计.md` §五 A.3 与 `V1`）。

产出（`--write` 才落盘）：
  · `数据/游戏数据/arena_node_pairs.tsv`       —— 逐节点配对（C-11）
  · `数据/游戏数据/arena_scenario_pairs.tsv`   —— 场次级汇总（每场「原版 GO 数 / 我方 GO 数」两列可解释全部行）

用法：
    python -I -X utf8 d:/4/Unity/工具/gen_arena_node_pairs.py --dry     # 只分析、打印统计，不写盘
    python -I -X utf8 d:/4/Unity/工具/gen_arena_node_pairs.py --write   # 真写两份 TSV
    python -I -X utf8 d:/4/Unity/工具/gen_arena_node_pairs.py --check   # 重算并与盘上 TSV 逐字节比
    + 可选 `--arena=battlearena1`（只处理这一场；⚠️ **只许配 `--dry`** —— `--write/--check` 是整表语义，
      单场写盘会把其余 12 场从盘上冲掉，脚本会直接拒绝）

退出码：0 = OK；1 = --check 漂移；2 = 参数错误（含裸跑）；3 = 输入缺失/解析失败。
"""
import io
import hashlib
import os
import re
import sys
from collections import defaultdict, Counter

ASSETS = 'D:/4/Unity/MyGame/Assets'
ORIG_DIR = os.path.join(ASSETS, 'WarpforgeVFX', 'ArenaOriginalPrefabs')
OURS_DIR = os.path.join(ASSETS, 'Resources', 'ArenaPrefabs')
OUT_DIR = 'D:/4/Unity/数据/游戏数据'
NODE_TSV = os.path.join(OUT_DIR, 'arena_node_pairs.tsv')
SCENE_TSV = os.path.join(OUT_DIR, 'arena_scenario_pairs.tsv')

ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari',
          'battlearenaastramilitarum', 'battlearenablacklegion', 'battlearenadarkangels',
          'battlearenaemperorschildren', 'battlearenagenestealers', 'battlearenaleviathan',
          'battlearenasororitas', 'battlearenaspacewolves', 'battlearenatauviorla']

MESH_RE = re.compile(r'^(mesh|Mesh)( \(\d+\))?$')
BLOCK = re.compile(r'^--- !u!(\d+) &(-?\d+)(?: stripped)?\s*$', re.M)

# 组件类别：class id → 单字母（114/MonoBehaviour 刻意不在表里，理由见文件头 ②）
CATS = {198: 'P', 199: 'P', 33: 'M', 23: 'M', 108: 'L', 95: 'A', 120: 'R', 64: 'C', 65: 'C'}
# 参与「字段差数」的 class id（114 不计：我方自家脚本与原版脚本天然不同）
FIELD_CLASSES = (1, 4, 23, 33, 64, 65, 95, 108, 120, 198, 199)

# 「仅原版」的判定规则（§五 A.5② 要求每一条都必须落到这两类之一）。
# 判据出处逐个写在这里，表里只抄短标签。
RUNTIME_LINERENDERER = '运行时建（LineRenderer）'
RUNTIME_ANIMATOR = '运行时建（Animator）'
EDITOR_ONLY = '编辑器专用（空节点/编组）'
DUP_ORIG = '多对一（原版同路径同名重复）'
TRUE_MISSING = '真缺'


# ---------------------------------------------------------------- 解析

def read_text(path):
    with io.open(path, 'rb') as f:
        raw = f.read()
    bump = raw[:3]
    if bump == b'\xef\xbb\xbf':
        raw = raw[3:]
    return raw.decode('utf-8', 'replace').replace('\r\n', '\n')


def _decode_yaml_str(s):
    s = re.sub(r'\\u([0-9a-fA-F]{4})', lambda m: chr(int(m.group(1), 16)), s)
    s = s.replace('\\"', '"').replace("\\'", "'")
    return s


def _one(blk, key):
    m = re.search(r'^\s*' + re.escape(key) + r':\s*(.+?)\s*$', blk, re.M)
    return m.group(1) if m else None


def _nested(blk, parent, key):
    """取 `parent:` 下一层的 `key:`（只认两级；用于 ParticleSystem 的 InitialModule 等）。"""
    m = re.search(r'^\s*' + re.escape(parent) + r':\s*$', blk, re.M)
    if not m:
        return None
    tail = blk[m.end():]
    end = re.search(r'^\s{2}\w', tail, re.M)  # 回到与 parent 同级
    if end:
        tail = tail[:end.start()]
    return _one(tail, key)


def _ref(v):
    m = re.search(r'fileID:\s*(-?\d+)', v or '')
    return m.group(1) if m else None


def _vec(v):
    """`{x: 1, y: 2, z: 3}` → ('1.0000','2.0000','3.0000')；取不到给 None（= 该键不算）。"""
    if not v:
        return None
    xs = re.findall(r'([xyzw]):\s*(-?[\d.eE+-]+)', v)
    if not xs:
        return None
    return tuple('%s=%s' % (k, _num(n)) for k, n in xs)


def _num(s):
    try:
        return '%.4f' % float(s)
    except (TypeError, ValueError):
        return str(s)


def _materials(blk):
    m = re.search(r'm_Materials:\s*\n((?:\s*-\s*\{fileID:[^\n]*\n)*)', blk)
    if not m:
        return None
    return tuple(_asset_ref(v) for v in re.findall(r'\{fileID:\s*-?\d+,\s*guid:\s*[0-9a-f]{32}[^}]*\}', m.group(1)))


# ---------------------------------------------------------------- guid → 资产名
# 原版 prefab 里的材质/网格引用指向本仓 `_shared/` 下的副本（guid 与原 bundle 不同），
# 我方指向 `WarpforgeArena1/arenas/**` 的再导入件 ⇒ **同名不同 guid**。
# 直接比 guid 会把「同一件资产」全记成差异 ⇒ 一律归一成**资产文件名**再比（判据：两边 .meta 指向同名 .mat/.asset）。
GUID_NAMES = {}


def build_guid_index(root):
    for dirpath, dirnames, filenames in os.walk(root):
        for fn in filenames:
            if not fn.endswith('.meta'):
                continue
            p = os.path.join(dirpath, fn)
            try:
                with io.open(p, 'rb') as f:
                    head = f.read(200).decode('utf-8', 'replace')
            except OSError:
                continue
            m = re.search(r'^guid:\s*([0-9a-f]{32})\s*$', head, re.M)
            if m:
                GUID_NAMES[m.group(1)] = os.path.splitext(fn[:-5])[0]


def _asset_ref(ref):
    """`{fileID: …, guid: …}` → 资产名（解析不到就退回 guid 前 8 位）。"""
    g = re.search(r'guid:\s*([0-9a-f]{32})', ref or '')
    if not g:
        return None
    gid = g.group(1)
    if gid == '0' * 32:
        return None
    return GUID_NAMES.get(gid, '<guid:%s>' % gid[:8])


def _parse(path):
    txt = read_text(path)
    ms = list(BLOCK.finditer(txt))
    blocks = defaultdict(dict)
    for i, m in enumerate(ms):
        s = m.end()
        e = ms[i + 1].start() if i + 1 < len(ms) else len(txt)
        blocks[int(m.group(1))][m.group(2)] = txt[s:e]

    comp_of = {}
    for cid, d in blocks.items():
        for fid, blk in d.items():
            comp_of[fid] = (cid, blk)

    gos = {}
    for fid, blk in blocks[1].items():
        name = (_one(blk, 'm_Name') or '').strip()
        if len(name) >= 2 and name[0] == name[-1] and name[0] in '\'"':
            name = name[1:-1]
        gos[fid] = dict(name=_decode_yaml_str(name), layer=_one(blk, 'm_Layer'),
                        active=_one(blk, 'm_IsActive'),
                        comps=re.findall(r'component:\s*\{fileID:\s*(-?\d+)\}', blk))
    trs = {}
    for fid, blk in blocks[4].items():
        children = []
        m = re.search(r'm_Children:\s*\n((?:\s*-\s*\{fileID:\s*-?\d+\}\s*\n)*)', blk)
        if m:
            children = re.findall(r'fileID:\s*(-?\d+)', m.group(1))
        trs[fid] = dict(go=_ref(_one(blk, 'm_GameObject')), father=_ref(_one(blk, 'm_Father')),
                        children=children,
                        pos=_vec(_one(blk, 'm_LocalPosition')),
                        rot=_vec(_one(blk, 'm_LocalRotation')),
                        scale=_vec(_one(blk, 'm_LocalScale')))
    return gos, trs, comp_of


def _comp_fields(cid, blk):
    """一个组件的「可比较字段」。键名形如 `<class>.<字段>`。"""
    out = {}
    if cid in (23, 199, 120):
        out['mat'] = _materials(blk)
        out['cast'] = _one(blk, 'm_CastShadows')
        out['recv'] = _one(blk, 'm_ReceiveShadows')
        out['lpu'] = _one(blk, 'm_LightProbeUsage')
        out['en'] = _one(blk, 'm_Enabled')
    elif cid == 33:
        out['mesh'] = _asset_ref(_one(blk, 'm_Mesh'))
    elif cid == 198:
        for k in ('lengthInSec', 'simulationSpeed', 'looping', 'playOnAwake', 'prewarm',
                  'scalingMode', 'cullingMode'):
            out[k] = _one(blk, k)
        for k in ('enabled', 'startLifetime', 'startSpeed', 'startSize'):
            if k == 'enabled':
                out['init.enabled'] = _one(_nested_block(blk, 'InitialModule'), k)
            else:
                out['init.' + k + '.scalar'] = _one(_nested_block(blk, 'InitialModule'), 'scalar')
                out['init.' + k + '.min'] = _one(_nested_block(blk, 'InitialModule'), 'minScalar')
        out['emission.rate'] = _one(_nested_block(blk, 'EmissionModule'), 'scalar')
    elif cid == 108:
        for k in ('m_Type', 'm_Intensity', 'm_Range', 'm_Color', 'm_Lightmapping'):
            out[k] = _one(blk, k)
    elif cid == 95:
        out['m_Controller'] = _asset_ref(_one(blk, 'm_Controller'))
        for k in ('m_ApplyRootMotion', 'm_CullingMode', 'm_UpdateMode'):
            out[k] = _one(blk, k)
    elif cid in (64, 65):
        out['m_Material'] = _asset_ref(_one(blk, 'm_Material'))
        for k in ('m_Enabled', 'm_IsTrigger', 'm_Convex'):
            out[k] = _one(blk, k)
    return {k: v for k, v in out.items() if v is not None}


def _nested_block(blk, parent):
    """返回 `parent:` 那一节的文本（缩进 +2 的内容）。取不到返回 ''。"""
    m = re.search(r'^\s*' + re.escape(parent) + r':\s*$', blk, re.M)
    if not m:
        return ''
    tail = blk[m.end():]
    end = re.search(r'^\s{2}\w', tail, re.M)
    return tail[:end.start()] if end else tail


# ---------------------------------------------------------------- 建「虚拟节点」树

class Node(object):
    __slots__ = ('fid', 'name', 'path', 'sig', 'ngos', 'fields', 'meshes', 'go_names', 'hash')

    def __init__(self):
        self.fid = ''
        self.name = ''
        self.path = ''
        self.sig = ''
        self.ngos = 0
        self.fields = {}
        self.meshes = 0
        self.go_names = []
        self.hash = ''


def build_nodes(path):
    if not GUID_NAMES:
        build_guid_index(ASSETS)
    gos, trs, comp_of = _parse(path)
    go_tr = {t['go']: fid for fid, t in trs.items() if t['go']}
    if not gos:
        return []

    def parent_of(gofid):
        t = go_tr.get(gofid)
        f = trs[t]['father'] if t else None
        return trs[f]['go'] if f in trs else None

    def owner(gofid):
        cur, seen = gofid, 0
        while cur and seen < 128:
            if not MESH_RE.match(gos[cur]['name']):
                return cur
            cur = parent_of(cur)
            seen += 1
        return gofid

    virt = {}
    for gofid in gos:
        o = owner(gofid)
        v = virt.setdefault(o, Node())
        v.fid = o
        v.ngos += 1
        v.go_names.append(gos[gofid]['name'])
        if o != gofid:
            v.meshes += 1
        for cfid in gos[gofid]['comps']:
            cid, blk = comp_of.get(cfid, (None, ''))
            if cid is None:
                continue
            if cid in CATS:
                v.sig = ''.join(sorted(set(v.sig) | {CATS[cid]}))
            if cid in FIELD_CLASSES:
                for k, val in _comp_fields(cid, blk).items():
                    v.fields['%d.%s' % (cid, k)] = val
    # 名字 / 路径 / Transform 级字段
    for gofid, v in virt.items():
        v.name = gos[gofid]['name']
        parts, cur, seen = [], gofid, 0
        while cur and seen < 128:
            if not MESH_RE.match(gos[cur]['name']):
                parts.append(gos[cur]['name'])
            cur = parent_of(cur)
            seen += 1
        v.path = '/'.join(reversed(parts))
        t = trs.get(go_tr.get(gofid))
        if t:
            for k in ('pos', 'rot', 'scale'):
                if t[k] is not None:
                    v.fields['xf.' + k] = t[k]
        v.fields['go.layer'] = gos[gofid]['layer']
        v.fields['go.active'] = gos[gofid]['active']
        v.fields = {k: x for k, x in v.fields.items() if x is not None}
        # 节点内容哈希（= §五 A.3「整棵节点内容哈希」）：字段向量（**不含 `go.layer`** —— 那是分层方案差异）
        v.hash = hashlib.md5(('\x1f'.join('%s=%r' % (k, x) for k, x in
                                          sorted(v.fields.items()) if k != 'go.layer'))
                             .encode('utf-8')).hexdigest()
    out = list(virt.values())
    out.sort(key=lambda n: (n.path, n.fid))
    return out


# ---------------------------------------------------------------- 配对

def field_diff(a, b):
    keys = set(a.fields) | set(b.fields)
    diffs = []
    for k in sorted(keys):
        if a.fields.get(k) != b.fields.get(k):
            diffs.append(k)
    return diffs


def _suffix_len(x, y):
    ax, ay = x.split('/'), y.split('/')
    n = 0
    while n < min(len(ax), len(ay)) and ax[-1 - n] == ay[-1 - n]:
        n += 1
    return n


def match_nodes(orig, ours):
    """贪心全局配对：候选按 (名字相同, 签名相同, 路径后缀长度, 字段差数) 排序后逐个认领。
    ⛔ 不按 pathID、⛔ 不按子节点次序。"""
    cands = []
    for i, o in enumerate(orig):
        for j, u in enumerate(ours):
            name_eq = (o.name == u.name)
            sig_eq = (o.sig == u.sig)
            suf = _suffix_len(o.path, u.path)
            # 候选门槛：名字相同，或路径后缀 ≥2 段且签名相同（容忍我方多套一层容器）
            if not name_eq and not (sig_eq and suf >= 2):
                continue
            diffs = field_diff(o, u)
            penalty = 0 if name_eq else 1
            # 优先级：① 名字相同 ② **节点内容哈希相同**（§五 A.3 一级键）③ 类别签名相同
            #         ④ 路径后缀越长越优先 ⑤ 字段差数最小（⛔ 全程不看 pathID、不看子节点次序）
            cands.append(((penalty, 0 if o.hash == u.hash else 1, 0 if sig_eq else 1,
                           -suf, len(diffs), o.path, u.path), i, j, diffs))
    cands.sort(key=lambda x: x[0])
    used_o, used_u, pairs = set(), set(), []
    for _, i, j, diffs in cands:
        if i in used_o or j in used_u:
            continue
        used_o.add(i)
        used_u.add(j)
        pairs.append((orig[i], ours[j], diffs))
    left_o = [n for i, n in enumerate(orig) if i not in used_o]
    left_u = [n for j, n in enumerate(ours) if j not in used_u]
    return pairs, left_o, left_u


def classify_orig_only(n):
    """§五 A.5②：每条「仅原版」必须落到「运行时建」或「编辑器专用」。
    判据：
      · LineRenderer(120) —— 我们不在 prefab 里烘线，`CardPresentation/Battle/TargetReticle.cs:378`
        记录这批宿主只当 Transform 用；ArenaBuilder 不建 LineRenderer 子件。
      · Animator(95) —— `WarpforgeArena1/Editor/ArenaBuilder.cs:6692` 在运行时 `AddComponent<Animator>()`。
      · 无任何可视组件（纯 Transform）—— 原版用它做编组/骨骼/遮罩锚点，渲染语义为零。
    """
    if 'R' in n.sig:
        return RUNTIME_LINERENDERER
    if 'A' in n.sig:
        return RUNTIME_ANIMATOR
    if not n.sig:
        return EDITOR_ONLY
    return TRUE_MISSING


def count_classes(path):
    """各类 `!u!<class>` 的条数（用于场次表里给「原版 PS / 我方 PS」这类计数）。"""
    return Counter(int(m.group(1)) for m in re.finditer(r'^--- !u!(\d+) &', read_text(path), re.M))


def split_layer(diffs):
    """`go.layer` 单独一列：我方 prefab 统一给节点设了自家层（`BoardElements` / `BoardCardLimit` / …，
    见 `ProjectSettings/TagManager.asset`），原版是 0 ⇒ **它不是「参数复刻错了」，是分层方案不同**，
    混进「字段差数」会把每一行都顶成「差字段」。"""
    rest = [k for k in diffs if k != 'go.layer']
    return rest, ('1' if len(rest) != len(diffs) else '0')


def classify_ours_only(n):
    if not n.sig:
        return '我方容器（无组件）'
    return '我方额外节点（有组件）'


def main(argv):
    args = list(argv[1:])
    dry = '--dry' in args
    write = '--write' in args
    check = '--check' in args
    only_arena = None
    good = [a for a in args if not a.startswith('--')]
    for a in args:
        if a.startswith('--arena='):
            only_arena = a.split('=', 1)[1]
        elif a.startswith('--arena'):
            print('--arena 要带值：`--arena=<场名>`', file=sys.stderr)
            return 2
    if good or (dry + write + check) != 1:
        print_usage()
        return 2
    if only_arena and only_arena not in ARENAS:
        print('⛔ --arena=%r 不在 13 场里' % only_arena, file=sys.stderr)
        return 2
    if only_arena and not dry:
        print('⛔ --arena 只能配 --dry（--write/--check 是整表语义，单场写盘会冲掉其余 12 场）',
              file=sys.stderr)
        return 2

    arenas = [only_arena] if only_arena else ARENAS
    node_rows, scene_rows = [], []
    for arena in arenas:
        op = os.path.join(ORIG_DIR, arena, arena + '__Scenario.prefab')
        up = os.path.join(OURS_DIR, arena + '.prefab')
        if not (os.path.exists(op) and os.path.exists(up)):
            print('⛔ 输入缺失：%s / %s' % (op, up), file=sys.stderr)
            return 3
        o = build_nodes(op)
        u = build_nodes(up)
        pairs, left_o, left_u = match_nodes(o, u)
        # 原版「同路径同名」的重复节点（原版自己就有两个同名 GO）⇒ 挂到已配上的那一行，
        # 判「多对一」，⛔ 不当成「真缺」（它和已配的那颗是同名兄弟，差异已在配对行的字段差里）。
        dup_index = {}
        for a, b, _ in pairs:
            dup_index.setdefault((a.path, a.name), b)
        dups_o = []
        rest_o = []
        for n in left_o:
            b = dup_index.get((n.path, n.name))
            if b is None:
                rest_o.append(n)
            else:
                dups_o.append((n, b))
        left_o = rest_o
        # ---- 节点行
        for a, b, diffs in pairs:
            rest, layer = split_layer(diffs)
            lack = ''.join(sorted(set(a.sig) - set(b.sig)))
            verdict = '差字段' if rest else ('仅层差' if layer == '1' else '等价')
            node_rows.append([arena, a.path, b.path, a.sig or '-', str(a.ngos), str(b.ngos),
                              str(len(rest)),
                              ','.join(_short(k) for k in rest[:6]) + ('…' if len(rest) > 6 else ''),
                              lack or '-', layer, verdict, ''])
        for a, b in dups_o:
            rest, layer = split_layer(field_diff(a, b))
            lack = ''.join(sorted(set(a.sig) - set(b.sig)))
            node_rows.append([arena, a.path, b.path, a.sig or '-', str(a.ngos), '0',
                              str(len(rest)),
                              ','.join(_short(k) for k in rest[:6]) + ('…' if len(rest) > 6 else ''),
                              lack or '-', layer, DUP_ORIG, '同路径同名'])
        for a in left_o:
            node_rows.append([arena, a.path, '', a.sig or '-', str(a.ngos), '0', '',
                              '', a.sig or '-', '', '仅原版', classify_orig_only(a)])
        for b in left_u:
            node_rows.append([arena, '', b.path, b.sig or '-', '0', str(b.ngos), '',
                              '', '-', '', '仅我方', classify_ours_only(b)])
        # ---- 场次行
        ok = len(pairs) + len(dups_o)
        exact = sum(1 for _, _, d in pairs if not split_layer(d)[0])
        oc, uc = count_classes(op), count_classes(up)
        scene_rows.append([arena, arena + '__Scenario.prefab', str(sum(n.ngos for n in o)),
                           str(oc.get(198, 0)), arena + '.prefab', str(sum(n.ngos for n in u)),
                           str(uc.get(198, 0)), str(ok), str(exact),
                           str(len(left_o)), str(len(left_u)),
                           ','.join('%s=%d' % (k, v) for k, v in
                                    sorted(Counter(classify_orig_only(n) for n in left_o).items())) or '-'])
    # 排序：场次按 ARENAS 序；节点行按 (arena 序, 仅原版在前? 不 —— 按路径稳定排序)
    order = {a: i for i, a in enumerate(ARENAS)}
    node_rows.sort(key=lambda r: (order.get(r[0], 99), r[1] or r[2]))
    scene_rows.sort(key=lambda r: order.get(r[0], 99))

    # 🔴 验收①：每场「原版GO / 我方GO」两列必须能解释**全部行**（== 该场 prefab 里 GameObject 的条数）。
    for s in scene_rows:
        a = s[0]
        n_o = sum(int(r[4]) for r in node_rows if r[0] == a)
        n_u = sum(int(r[5]) for r in node_rows if r[0] == a)
        if n_o != int(s[2]) or n_u != int(s[5]):
            print('⛔ 验收失败：%s 行合计 原版%d/%d 我方%d/%d' % (a, n_o, int(s[2]), n_u, int(s[5])),
                  file=sys.stderr)
            return 3

    node_txt = render(NODE_HEADER, node_rows)
    scene_txt = render(SCENE_HEADER, scene_rows)

    if dry:
        for r in scene_rows:
            print(' | '.join(r))
        tot_o = sum(int(r[2]) for r in scene_rows)
        tot_u = sum(int(r[5]) for r in scene_rows)
        print('合计：原版 GO %d · 我方 GO %d · 配对 %d · 仅原版 %d · 仅我方 %d' % (
            tot_o, tot_u, sum(int(r[7]) for r in scene_rows),
            sum(int(r[9]) for r in scene_rows), sum(int(r[10]) for r in scene_rows)))
        miss = Counter(r[11] for r in node_rows if r[10] == '仅原版')
        print('仅原版原因分布：', dict(miss))
        for r in node_rows:
            if r[10] == '仅原版' and r[11] == TRUE_MISSING:
                print('  真缺：', r[0], r[1], r[3])
        print('判定分布：', dict(Counter(r[10] for r in node_rows)))
        print('缺组件分布（配对行）：', dict(Counter(r[8] for r in node_rows
                                                 if r[8] not in ('-', ''))))
        return 0

    if check:
        bad = []
        for path, expect in ((NODE_TSV, node_txt), (SCENE_TSV, scene_txt)):
            if not os.path.exists(path):
                bad.append(path + '（不存在，先 --write）')
            elif read_text(path) != expect:
                bad.append(path + '（漂移）')
        if bad:
            print('⛔ 不一致：' + ' · '.join(bad), file=sys.stderr)
            return 1
        print('一致：%s · %s' % (NODE_TSV, SCENE_TSV))
        return 0

    # --write
    if not os.path.isdir(OUT_DIR):
        os.makedirs(OUT_DIR)
    for path, text in ((NODE_TSV, node_txt), (SCENE_TSV, scene_txt)):
        with io.open(path, 'wb') as f:
            f.write(text.encode('utf-8'))
    print('已写：%s（%d 行）· %s（%d 行）' % (NODE_TSV, len(node_rows), SCENE_TSV, len(scene_rows)))
    return 0


def _short(k):
    return k.split('.', 1)[1] if '.' in k else k


NODE_HEADER = ['arena', 'orig_path', 'ours_path', '类别(PM+LARC)', '原版GO', '我方GO',
               '字段差数', '差异字段', '缺组件', '层差', '判定', '原因']
SCENE_HEADER = ['arena', 'orig_prefab', '原版GO', '原版PS', 'ours_prefab', '我方GO', '我方PS',
                '配对数', '无字段差', '仅原版', '仅我方', '仅原版原因']


def render(header, rows):
    lines = ['\t'.join(header)]
    for r in rows:
        lines.append('\t'.join('' if c is None else str(c) for c in r))
    return '\n'.join(lines) + '\n'


def print_usage():
    print(__doc__.strip())


if __name__ == '__main__':
    sys.exit(main(sys.argv))
