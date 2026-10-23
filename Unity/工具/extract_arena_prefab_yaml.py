#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""extract_arena_prefab_yaml.py — 战场线 · prefab YAML 级抽取（2026-10-23 试点）

从 AssetRipper 导出的 `<场景>.unity` 里，按 Transform 父链抠出「世界内容根」那一整棵子树，
原样（连同全部 YAML 文档）写成我们 Unity 工程能 import 的 `.prefab`，并把闭包里的
网格 / 材质 / 贴图 / Sprite 资产连同 `.meta` 一起拷过去（guid 不变 ⇒ 外引自动成立）。

判据（唯一来源）：`Unity/资料/普查产出_1023b/战场线_15prefab清单.md` §五 (c)。
形状模板：ExportedProject 里 AssetRipper 自己写的 prefab（`Assets/Battle Arena 3 Baked.prefab`）
—— 没有 `PrefabInstance` 文档、根 `m_Father: {fileID: 0}`、全部 `m_CorrespondingSourceObject: {fileID: 0}`。

用法
----
    # 试点（写进 Assets）
    python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena3 --root Scenario \
        --out-root d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs

    # 🆕 2026-10-23 推广：默认写 staging（⛔ 不再默认写 Assets）
    python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena3 --root Scenario
    python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearena2 --root Scenario --dry-run

    # 🆕 独立 Baked 根合并回 Scenario（世界变换保持；见 `--merge-root`）
    python -I -X utf8 d:/4/Unity/工具/extract_arena_prefab_yaml.py --arena battlearenaastramilitarum \
        --root Scenario --merge-root "Battle Arena Astra Militarum Baked"

⛔ 不要提供 `--all`：铁律 15 —— 战场/场景的活一律一场一场做。

产出（默认）
-----------
    <out-root>/<arena>/<arena>__<root>.prefab           ← 子树全量 YAML
    <out-root>/<arena>/<arena>__<root>.prefab.meta      ← 确定性 guid（重跑不变）
    <out-root>/<arena>/<原 ExportedProject 相对路径>     ← 闭包资产 + 原 .meta（guid 原样）

⚠️ 默认 `<out-root>` = `d:/4/_tmp_view/arena_prefabs_out`（**staging，不碰 `Assets/`**）。
   要写进工程就显式 `--out-root d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs`（试点那次的口径）。

合并多颗根（`--merge-root`，可多次）
------------------------------------
把额外的世界内容根（如 astra / blacklegion / EC 的独立 `Baked` 根）**挂到主根下**，输出一颗 prefab。
**按世界变换保持来挂**：新局部 TRS = inverse(主根世界) × 被挂根世界。
⚠️ 实测 `Scenario` 的根位**不是** 0（8 场是 `(113.238, -8.695, 19.853)`）⇒ 直接「局部置 0」会把
Baked 内容整体平移 ~113 单位。astra / EC 就是这么被纠回来的（工具里有「世界变换保持」自检）。

已知的丢掉项（本工具按设计不做，报告里会逐项列出）
--------------------------------------------------
* `MonoBehaviour`（我们工程没有那 22 个桩脚本）—— 组件连同 GameObject.m_Component 里那一行一起删。
* `.shader`（ExportedProject 里 149/149 都是 Dummy）—— 材质上的 shader guid 原样保留，import 后 Missing/品红。
* `.anim` / `.controller` / `.playable` / `.prefab` —— 不在「网格/材质/贴图」范围内，默认不拷
  （可用 `--extra-ext anim,prefab` 打开）。
"""

from __future__ import annotations

import argparse
import hashlib
import io
import json
import os
import re
import sys
import time
from collections import Counter, OrderedDict

# ---------------------------------------------------------------------------
# 常量
# ---------------------------------------------------------------------------

EXPORTED_ROOT_DEFAULT = r"d:/2/tools/assetripper_out/ExportedProject"
# 🆕 2026-10-23：默认写 staging，⛔ 不默认写 Assets（主对话要先把产物审过再 import）
OUT_ROOT_DEFAULT = r"d:/4/_tmp_view/arena_prefabs_out"
# 试点（battlearena3）那次写进工程的实际目录；要用旧口径就 --out-root 这个
OUT_ROOT_LEGACY_ASSETS = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/ArenaOriginalPrefabs"
OUR_ASSETS_DEFAULT = r"d:/4/Unity/MyGame/Assets"

# arena key → ExportedProject 里的场景相对路径（清单 §二 逐行抄；⛔ 别按名字猜）
ARENA_SCENES = OrderedDict([
    ("battlearena1",              "Assets/Data/Scenes/Battle Scenarios/Battle Arena 1.unity"),
    ("battlearena2",              "Assets/Data/Scenes/Battle Scenarios/Battle Arena 2.unity"),
    ("battlearena3",              "Assets/Data/Scenes/Battle Scenarios/Battle Arena 3.unity"),
    ("battlearenaaeldari",        "Assets/Data/Scenes/Battle Scenarios/Battle Arena Aeldari.unity"),
    ("battlearenaastramilitarum", "Assets/Data/Scenes/Battle Scenarios/Battle Arena Astra Militarum.unity"),
    ("battlearenablacklegion",    "Assets/Data/Scenes/Battle Scenarios/Battle Arena Black Legion.unity"),
    ("battlearenadarkangels",     "Assets/Data/Scenes/Battle Scenarios/Battle Arena Dark Angels.unity"),
    ("battlearenaemperorschildren", "Assets/Data/Scenes/Battle Scenarios/Battle Arena Emperors Children.unity"),
    ("battlearenagenestealers",   "Assets/Data/Scenes/Battle Scenarios/Battle Arena Genestealers.unity"),
    ("battlearenaleviathan",      "Assets/Data/Scenes/Battle Scenarios/Battle Arena Leviathan.unity"),
    ("battlearenasororitas",      "Assets/Data/Scenes/Battle Scenarios/Battle Arena Sororitas.unity"),
    ("battlearenaspacewolves",    "Assets/Data/Scenes/Battle Scenarios/Battle Arena Space Wolves.unity"),
    ("battlearenatauviorla",      "Assets/Data/Scenes/Battle Scenarios/Battle Arena Tau Viorla.unity"),
])

# Unity 内建资源包（不是「解不到」）
BUILTIN_GUIDS = {
    "0000000000000000e000000000000000": "unity default resources",
    "0000000000000000f000000000000000": "unity_builtin_extra",
}

# 默认要拷的扩展名（= 任务的「网格 / 材质 / 贴图」口径 + Sprite）
DEFAULT_COPY_EXTS = {".asset", ".mat", ".png", ".jpg", ".jpeg", ".tga", ".psd", ".exr", ".tif", ".tiff", ".bmp"}

# 默认丢掉的组件类名
DEFAULT_DROP_CLASSES = {"MonoBehaviour"}

# 重编号偏移：Unity prefab 里的 fileID 惯例是大整数。加常数不影响唯一性。
RENUMBER_OFFSET = 1_000_000_000_000_000

DOC_RE = re.compile(r"^--- !u!(\d+) &(-?\d+)(.*)$")
REF_FLOW_RE = re.compile(r"\{fileID: (-?\d+)([^}]*)\}")
GUID_REF_RE = re.compile(r"\{fileID: (-?\d+), guid: ([0-9a-fA-F]{32}), type: (\d+)\}")
GUID_IN_META_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.M)

YAML_HEADER = "%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n"

PREFAB_META_TMPL = """fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META_TMPL = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


# ---------------------------------------------------------------------------
# YAML 文档层
# ---------------------------------------------------------------------------

class Doc(object):
    __slots__ = ("type", "id", "lines", "hdr")

    def __init__(self, type_id, fid, hdr):
        self.type = type_id
        self.id = fid
        self.hdr = hdr
        self.lines = []          # 不含文档头那一行

    @property
    def cls(self):
        if not self.lines:
            return "?"
        first = self.lines[0]
        return first.rstrip().rstrip(":").strip()

    @property
    def text(self):
        return "\n".join(self.lines)


def parse_unity_yaml(path):
    """把 .unity / .prefab 切成文档列表。返回 (docs, byid)。"""
    raw = io.open(path, "rb").read()
    txt = raw.decode("utf-8", "surrogateescape")
    docs = []
    cur = None
    for ln in txt.split("\n"):
        m = DOC_RE.match(ln)
        if m:
            cur = Doc(int(m.group(1)), int(m.group(2)), ln)
            docs.append(cur)
        elif cur is not None:
            cur.lines.append(ln)
    for d in docs:
        while d.lines and d.lines[-1].strip() == "":
            d.lines.pop()
    byid = {d.id: d for d in docs}
    return docs, byid, txt


def field(doc, name):
    """取 `name: value` 的第一行值（顶层或缩进均可，按 strip 后比较）。"""
    for ln in doc.lines:
        s = ln.strip()
        if s.startswith(name + ":"):
            return s[len(name) + 1:].strip()
    return None


def block_list(doc, name):
    """取 `name:` 之后的 `- ...` 列表（每项是去掉前导 '- ' 的字符串）。"""
    out = []
    inlist = False
    for ln in doc.lines:
        s = ln.strip()
        if s.startswith(name + ":"):
            if s.endswith("[]"):
                return []
            inlist = True
            continue
        if inlist:
            if s.startswith("- "):
                out.append(s[2:])
            elif s.startswith("-{"):
                out.append(s[1:])
            elif s == "-":
                out.append("")
            else:
                break
    return out


def fileid_of(ref_text):
    m = REF_FLOW_RE.search(ref_text or "")
    return int(m.group(1)) if m else None


def children_of(transform_doc):
    out = []
    for item in block_list(transform_doc, "m_Children"):
        v = fileid_of(item)
        if v:
            out.append(v)
    return out


def components_of(go_doc):
    out = []
    for item in block_list(go_doc, "m_Component"):
        v = fileid_of(item)
        if v:
            out.append(v)
    return out


def go_of_transform(transform_doc):
    return fileid_of(field(transform_doc, "m_GameObject") or "")


# ---------------------------------------------------------------------------
# 子树 / 闭包
# ---------------------------------------------------------------------------

def find_root(byid, root_name):
    """找 m_Father == 0 且 GameObject.m_Name == root_name 的 Transform/RectTransform。"""
    hits = []
    for d in byid.values():
        if d.type not in (4, 224):
            continue
        father = field(d, "m_Father")
        if not father or fileid_of(father) != 0:
            continue
        gid = go_of_transform(d)
        go = byid.get(gid)
        if go is not None and field(go, "m_Name") == root_name:
            hits.append(d)
    if len(hits) != 1:
        raise SystemExit(
            "FATAL: 根 `%s` 命中 %d 个（期望 1）。现读的根清单：%s"
            % (root_name, len(hits), [x.id for x in hits])
        )
    return hits[0]


def collect_subtree(byid, root_transform_id):
    """按 m_Children 递归，返回 (transform 顺序表, 集合)。"""
    order = []
    seen = set()
    stack = [root_transform_id]
    while stack:
        tid = stack.pop()
        if tid in seen:
            continue
        seen.add(tid)
        order.append(tid)
        stack.extend(reversed(children_of(byid[tid])))
    return order, seen


# ---------------------------------------------------------------------------
# guid 索引
# ---------------------------------------------------------------------------

def build_guid_index(assets_dir, skip_dirs=("Library", "Temp", "obj", "Logs")):
    """扫 assets_dir 下所有 .meta，返回 guid -> 资产绝对路径。"""
    out = {}
    n_meta = 0
    t0 = time.time()
    for dp, dns, fns in os.walk(assets_dir):
        dns[:] = [d for d in dns if d not in skip_dirs and not d.startswith(".")]
        for fn in fns:
            if not fn.endswith(".meta"):
                continue
            p = os.path.join(dp, fn)
            try:
                head = io.open(p, "rb").read(512).decode("utf-8", "replace")
            except OSError:
                continue
            m = GUID_IN_META_RE.search(head)
            n_meta += 1
            if m:
                out[m.group(1).lower()] = p[:-5]
    return out, n_meta, time.time() - t0


def read_meta_guid(meta_path):
    try:
        head = io.open(meta_path, "rb").read(512).decode("utf-8", "replace")
    except OSError:
        return None
    m = GUID_IN_META_RE.search(head)
    return m.group(1).lower() if m else None


def deterministic_guid(salt):
    return hashlib.md5(("wf-arenaoriginalprefabs:" + salt).encode("utf-8")).hexdigest()


# ---------------------------------------------------------------------------
# 变换数学（`--merge-root` 用；只处理 TRS，无 shear）
# ---------------------------------------------------------------------------

VEC3_RE = re.compile(r"\{x: ([^,]+), y: ([^,]+), z: ([^}]+)\}")
QUAT_RE = re.compile(r"\{x: ([^,]+), y: ([^,]+), z: ([^,]+), w: ([^}]+)\}")

IDENTITY_QUAT = (0.0, 0.0, 0.0, 1.0)
ONE_VEC3 = (1.0, 1.0, 1.0)


def parse_vec3(s):
    m = VEC3_RE.search(s or "")
    if not m:
        raise ValueError("不是 vec3：%r" % s)
    return tuple(float(x) for x in m.groups())


def parse_quat(s):
    m = QUAT_RE.search(s or "")
    if not m:
        raise ValueError("不是 quat：%r" % s)
    return tuple(float(x) for x in m.groups())


def fmt_num(v):
    """Unity 的浮点写法：短、可回读。`-0.0` 统一写成 `0`。"""
    if v == 0.0:
        return "0"
    r = repr(float(v))
    if r.endswith(".0"):
        r = r[:-2]
    return r


def fmt_vec3(v):
    return "{x: %s, y: %s, z: %s}" % tuple(fmt_num(x) for x in v)


def fmt_quat(q):
    return "{x: %s, y: %s, z: %s, w: %s}" % tuple(fmt_num(x) for x in q)


def qmul(a, b):
    ax, ay, az, aw = a
    bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by,
            aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw,
            aw * bw - ax * bx - ay * by - az * bz)


def qconj(q):
    return (-q[0], -q[1], -q[2], q[3])


def qrot(q, v):
    """用单位四元数旋转向量：v' = v + 2w(q×v) + 2q×(q×v)。"""
    x, y, z, w = q
    vx, vy, vz = v
    tx = 2.0 * (y * vz - z * vy)
    ty = 2.0 * (z * vx - x * vz)
    tz = 2.0 * (x * vy - y * vx)
    return (vx + w * tx + (y * tz - z * ty),
            vy + w * ty + (z * tx - x * tz),
            vz + w * tz + (x * ty - y * tx))


def world_trs(byid, tid, memo=None):
    """沿 m_Father 累乘出世界 TRS。scale 按分量乘（本场景无 shear；父链 scale 全 1 时精确）。"""
    if memo is None:
        memo = {}
    if tid in memo:
        return memo[tid]
    d = byid[tid]
    pos = parse_vec3(field(d, "m_LocalPosition"))
    rot = parse_quat(field(d, "m_LocalRotation"))
    scl = parse_vec3(field(d, "m_LocalScale"))
    f = fileid_of(field(d, "m_Father") or "")
    if not f:
        w = (pos, rot, scl)
    else:
        ppos, prot, pscl = world_trs(byid, f, memo)
        off = qrot(prot, (pos[0] * pscl[0], pos[1] * pscl[1], pos[2] * pscl[2]))
        w = ((ppos[0] + off[0], ppos[1] + off[1], ppos[2] + off[2]),
             qmul(prot, rot),
             (pscl[0] * scl[0], pscl[1] * scl[1], pscl[2] * scl[2]))
    memo[tid] = w
    return w


def make_local(parent_world, child_world):
    """求「挂到 parent 下、世界变换保持」所需的局部 TRS。"""
    ppos, prot, pscl = parent_world
    cpos, crot, cscl = child_world
    if abs(pscl[0]) < 1e-12 or abs(pscl[1]) < 1e-12 or abs(pscl[2]) < 1e-12:
        raise SystemExit("FATAL: 主根的世界 scale 有 0 分量 %r，无法求逆" % (pscl,))
    inv = qconj(prot)
    delta = (cpos[0] - ppos[0], cpos[1] - ppos[1], cpos[2] - ppos[2])
    r = qrot(inv, delta)
    lpos = (r[0] / pscl[0], r[1] / pscl[1], r[2] / pscl[2])
    lrot = qmul(inv, crot)
    lscl = (cscl[0] / pscl[0], cscl[1] / pscl[1], cscl[2] / pscl[2])
    # 数值清理：近零归零；近单位四元数直接写成单位阵（避免 `6.3e-30` 那种噪声进产物）
    lpos = tuple(0.0 if abs(v) < 1e-12 else v for v in lpos)
    lrot = tuple(0.0 if abs(v) < 1e-12 else v for v in lrot)
    n = (lrot[0] ** 2 + lrot[1] ** 2 + lrot[2] ** 2 + lrot[3] ** 2) ** 0.5
    if n < 1e-9:
        raise SystemExit("FATAL: 合并根的局部四元数长度为 0")
    lrot = tuple(v / n for v in lrot)
    if min(abs(lrot[0]), abs(lrot[1]), abs(lrot[2]), abs(1.0 - abs(lrot[3]))) < 1e-9:
        if abs(abs(lrot[3]) - 1.0) < 1e-9:
            lrot = IDENTITY_QUAT
    lscl = tuple(1.0 if abs(v - 1.0) < 1e-12 else v for v in lscl)
    return lpos, lrot, lscl


def patch_trs_lines(lines, father_id=None, pos=None, rot=None, scl=None):
    """在 Transform 文档的 lines 里换掉 m_Father / m_LocalPosition / m_LocalRotation / m_LocalScale。"""
    out = []
    for ln in lines:
        s = ln.strip()
        if father_id is not None and s.startswith("m_Father:"):
            out.append("  m_Father: {fileID: %d}" % father_id)
            continue
        if pos is not None and s.startswith("m_LocalPosition:"):
            out.append("  m_LocalPosition: " + fmt_vec3(pos))
            continue
        if rot is not None and s.startswith("m_LocalRotation:"):
            out.append("  m_LocalRotation: " + fmt_quat(rot))
            continue
        if scl is not None and s.startswith("m_LocalScale:"):
            out.append("  m_LocalScale: " + fmt_vec3(scl))
            continue
        out.append(ln)
    return out


def append_children_lines(lines, child_ids):
    """把 child_ids 追加到 Transform 的 m_Children 列表尾部（保持原缩进；空列表 `[]` 也能展开）。"""
    if not child_ids:
        return list(lines)
    out = []
    in_children = False
    last_idx = None
    indent = "  - "
    for ln in lines:
        s = ln.strip()
        if in_children:
            if s.startswith("- {fileID:") or s.startswith("- component:"):
                indent = ln[:len(ln) - len(ln.lstrip())] + "- "
                out.append(ln)
                last_idx = len(out) - 1
                continue
            in_children = False
        if s.startswith("m_Children:"):
            if s.endswith("[]"):
                # `m_Children: []` -> 展开成列表
                out.append(ln.replace("[]", "").rstrip())
                in_children = True
                last_idx = len(out) - 1
                indent = "  - "
                continue
            out.append(ln)
            in_children = True
            continue
        out.append(ln)
    if last_idx is None:
        raise SystemExit("FATAL: 该 Transform 里没有 m_Children，无法追加子件")
    for cid in child_ids:
        out.insert(last_idx + 1, "%s{fileID: %d}" % (indent, cid))
        last_idx += 1
    return out


# ---------------------------------------------------------------------------
# 渲染 prefab
# ---------------------------------------------------------------------------

def strip_component_entries(go_doc, drop_ids):
    """从 GameObject 的 m_Component 列表里删掉 drop_ids 那几行。

    返回新的 lines 列表。⚠️ 必须整块重建 —— 只在原行上做字符串替换会留下
    空列表头或者把 `m_Component: []` 和后面的 `m_Layer` 挤在一起。
    """
    if not drop_ids:
        return list(go_doc.lines)
    out = []
    in_comp = False
    for ln in go_doc.lines:
        s = ln.strip()
        if s.startswith("m_Component:"):
            in_comp = True
            out.append("  m_Component:")
            continue
        if in_comp:
            if s.startswith("- component:"):
                v = fileid_of(s)
                if v in drop_ids:
                    continue
                out.append(ln)
                continue
            # 列表结束
            in_comp = False
        out.append(ln)
    # 如果一条都没剩，写成空列表
    if out and out[-1].strip() == "m_Component:":
        out[-1] = "  m_Component: []"
    return out


def renumber_refs(text, idmap):
    """把不带 guid 的 {fileID: N} 里的 N 换成新号（只换 idmap 里的）。"""
    def _sub(m):
        n = int(m.group(1))
        rest = m.group(2)
        if "guid:" in rest:
            return m.group(0)
        new = idmap.get(n)
        if new is None:
            return m.group(0)
        return "{fileID: %d%s}" % (new, rest)
    return REF_FLOW_RE.sub(_sub, text)


def render_prefab(byid, kept_ids, idmap, drop_id_set, root_transform_id,
                  zero_root_pos=False, line_overrides=None):
    """按源场景里的文档顺序输出 prefab 文本。

    `line_overrides`：{源 fileID: 已改写的 lines}（`--merge-root` 的 father / TRS / m_Children 补丁）。
    """
    chunks = [YAML_HEADER]
    for d in byid.values():            # dict 保持插入顺序 = 场景里的文档顺序
        if d.id not in kept_ids:
            continue
        d2 = Doc(d.type, idmap[d.id], d.hdr)
        if d.type == 1 and drop_id_set:
            d2.lines = strip_component_entries(d, drop_id_set)
        elif line_overrides and d.id in line_overrides:
            d2.lines = list(line_overrides[d.id])
        else:
            d2.lines = list(d.lines)
        body = renumber_refs("\n".join(d2.lines), idmap)
        if d.id == root_transform_id:
            body = re.sub(r"^(\s*m_Father:\s*)\{fileID: -?\d+\}", r"\1{fileID: 0}", body, flags=re.M)
            if zero_root_pos:
                body = re.sub(r"^(\s*m_LocalPosition:\s*)\{[^}]*\}", r"\1{x: 0, y: 0, z: 0}", body, flags=re.M)
        chunks.append("--- !u!%d &%d\n%s\n" % (d.type, idmap[d.id], body))
    return "".join(chunks)


# ---------------------------------------------------------------------------
# 自检
# ---------------------------------------------------------------------------

def yaml_parse_check(text):
    """用 PyYAML 逐文档解析。

    🔴 Unity 的 `%TAG !u! ...` 指令**只对紧跟的那一个文档有效**（YAML 规范如此），
    而 Unity 只在文件开头写一次 ⇒ 直接把整个文件喂给 `safe_load_all` 会报
    `found undefined tag handle '!u!'`。正确做法 = **按 `--- !u!` 切块，每块补回文件头再解析**
    （任务书 §6 允许的两条路之一）。
    """
    try:
        import yaml
    except ImportError:
        return False, "PyYAML 未安装（跳过该条）"

    class Loader(yaml.SafeLoader):
        pass

    def _any(loader, tag_suffix, node):
        # 只为了「能解析」，不解释内容
        if isinstance(node, yaml.MappingNode):
            return loader.construct_mapping(node, deep=False)
        if isinstance(node, yaml.SequenceNode):
            return loader.construct_sequence(node, deep=False)
        return loader.construct_scalar(node)

    Loader.add_multi_constructor("tag:unity3d.com,2011:", _any)
    Loader.add_multi_constructor("!u!", _any)
    Loader.add_multi_constructor("!", _any)

    parts = re.split(r"(?m)^(?=--- !u!)", text)
    n = 0
    try:
        for part in parts:
            if not part.startswith("--- !u!"):
                continue          # 文件头
            docs = list(yaml.load_all(YAML_HEADER + part, Loader=Loader))
            if len(docs) != 1:
                return False, "切块后单个 chunk 解析出 %d 个文档（期望 1）" % len(docs)
            n += 1
    except Exception as exc:  # noqa: BLE001  —— 这里就是要抓「解析不了」
        return False, "%s: %s" % (type(exc).__name__, str(exc).replace("\n", " | "))
    return True, "%d 个文档全部解析通过（按文档切块 + 补回 %%TAG 头）" % n


def main(argv=None):
    ap = argparse.ArgumentParser(description="战场线 prefab YAML 级抽取（试点）")
    ap.add_argument("--arena", help="场 key（battlearena1..battlearenatauviorla）；也可用 --scene 直接给路径")
    ap.add_argument("--root", default="Scenario", help="根 GameObject 名（默认 Scenario）")
    ap.add_argument("--scene", help="场景相对路径（相对 ExportedProject）；给了就不用 --arena")
    ap.add_argument("--exported-root", default=EXPORTED_ROOT_DEFAULT)
    ap.add_argument("--out-root", default=None,
                    help="产物根（默认 staging：%s）；"
                         "写进工程用 %s" % (OUT_ROOT_DEFAULT, OUT_ROOT_LEGACY_ASSETS))
    ap.add_argument("--out-dir", help="直接指定产物目录（覆盖 --out-root/<arena>）")
    ap.add_argument("--name", help="prefab 文件名（默认 <arena>__<root>）")
    ap.add_argument("--merge-root", action="append", default=None,
                    help="额外挂到主根下的独立根 GameObject 名（可多次）；按世界变换保持来挂")
    ap.add_argument("--dry-run", action="store_true", help="只统计，不写任何文件")
    ap.add_argument("--no-copy", action="store_true", help="只写 prefab，不拷依赖资产")
    ap.add_argument("--copy-exts", default=None, help="额外要拷的扩展名，逗号分隔（例：anim,prefab）")
    ap.add_argument("--drop-class", action="append", default=None,
                    help="额外要丢的组件类名（可多次）；MonoBehaviour 恒丢")
    ap.add_argument("--keep-mono", action="store_true", help="保留 MonoBehaviour（默认丢）")
    ap.add_argument("--zero-root-position", action="store_true", help="把根 m_LocalPosition 清零")
    ap.add_argument("--json-out", help="把摘要写成 JSON 文件")
    args = ap.parse_args(argv)

    t_start = time.time()

    # --- 定位场景 ---
    if args.scene:
        scene_rel = args.scene.replace("\\", "/")
        arena = args.arena or os.path.splitext(os.path.basename(scene_rel))[0].lower().replace(" ", "_")
    else:
        if not args.arena:
            ap.error("要么给 --arena，要么给 --scene")
        if args.arena not in ARENA_SCENES:
            ap.error("未知场 key %r；可选：%s" % (args.arena, ", ".join(ARENA_SCENES)))
        arena = args.arena
        scene_rel = ARENA_SCENES[arena]

    scene_path = os.path.join(args.exported_root, scene_rel)
    if not os.path.isfile(scene_path):
        raise SystemExit("FATAL: 场景不存在：%s" % scene_path)

    # --- 输出根（默认 staging） ---
    if args.out_root:
        out_root = os.path.abspath(args.out_root)
    elif args.out_dir:
        out_root = os.path.dirname(os.path.abspath(args.out_dir))
    else:
        out_root = os.path.abspath(OUT_ROOT_DEFAULT)
    out_dir = os.path.abspath(args.out_dir) if args.out_dir else os.path.join(out_root, arena)
    prefab_name = args.name or ("%s__%s" % (arena, args.root))
    prefab_path = os.path.join(out_dir, prefab_name + ".prefab")

    # --- 解析场景 ---
    docs, byid, _ = parse_unity_yaml(scene_path)
    if len(byid) != len(docs):
        raise SystemExit("FATAL: 场景里有重复 fileID（%d 文档 / %d 唯一 id）" % (len(docs), len(byid)))

    root_doc = find_root(byid, args.root)
    order, transform_ids = collect_subtree(byid, root_doc.id)
    nodes_main = len(order)

    # --- 合并独立根（--merge-root）：主子树先算好，再把别的根挂进来 ---
    def _subtree_closure(root_id):
        ids = set()
        for t in collect_subtree(byid, root_id)[1]:
            ids.add(t)
            gid = go_of_transform(byid[t])
            if gid is None:
                raise SystemExit("FATAL: Transform &%d 没有 m_GameObject" % t)
            ids.add(gid)
            for cid in components_of(byid[gid]):
                ids.add(cid)
        return ids

    main_closure = _subtree_closure(root_doc.id)
    merge_roots = []
    merge_info = []
    line_overrides = {}
    for nm in (args.merge_root or []):
        mr = find_root(byid, nm)
        if mr.id == root_doc.id:
            raise SystemExit("FATAL: --merge-root %r 就是主根自己" % nm)
        if any(mr.id == x.id for x in merge_roots):
            raise SystemExit("FATAL: --merge-root %r 重复给了两次" % nm)
        mo, mset = collect_subtree(byid, mr.id)
        overlap = mset & transform_ids
        if overlap:
            raise SystemExit("FATAL: 合并根 %r 的子树与主子树有 %d 个重叠节点" % (nm, len(overlap)))
        merge_roots.append(mr)
        order = order + mo
        transform_ids = transform_ids | mset
        merge_info.append({"name": nm, "transform": mr.id, "nodes": len(mo)})

    # 跨根引用（合并前 = 从主子树闭包里指向主子树外的 bare fileID），合并后必须都在产物里
    cross_refs = []
    if merge_roots:
        for i in sorted(main_closure):
            for mm in REF_FLOW_RE.finditer(byid[i].text):
                n = int(mm.group(1))
                if n == 0 or "guid:" in mm.group(2):
                    continue
                if n not in main_closure:
                    cross_refs.append((byid[i].cls, i, n))

    # 合并补丁：被挂根的局部 TRS 按「世界变换保持」反算；主根的 m_Children 追加它们
    memo_w = {}
    parent_world = world_trs(byid, root_doc.id, memo_w)
    for idx, mr in enumerate(merge_roots):
        cw = world_trs(byid, mr.id, memo_w)
        lpos, lrot, lscl = make_local(parent_world, cw)
        line_overrides[mr.id] = patch_trs_lines(mr.lines, father_id=root_doc.id,
                                                pos=lpos, rot=lrot, scl=lscl)
        merge_info[idx]["local_trs"] = {
            "pos": fmt_vec3(lpos), "rot": fmt_quat(lrot), "scale": fmt_vec3(lscl),
            "world": {"pos": fmt_vec3(cw[0]), "rot": fmt_quat(cw[1]), "scale": fmt_vec3(cw[2])},
        }
    if merge_roots:
        line_overrides[root_doc.id] = append_children_lines(
            root_doc.lines, [mr.id for mr in merge_roots])

    # --- 闭包：transform + GameObject + 组件 ---
    closure = set(transform_ids)
    go_ids = set()
    for tid in transform_ids:
        gid = go_of_transform(byid[tid])
        if gid is None:
            raise SystemExit("FATAL: Transform &%d 没有 m_GameObject" % tid)
        go_ids.add(gid)
        closure.add(gid)
        for cid in components_of(byid[gid]):
            closure.add(cid)
    missing_doc = sorted(i for i in closure if i not in byid)
    if missing_doc:
        raise SystemExit("FATAL: 闭包里 %d 个 id 在场景里没有文档：%s" % (len(missing_doc), missing_doc[:10]))

    # --- 丢组件 ---
    drop_classes = set() if args.keep_mono else set(DEFAULT_DROP_CLASSES)
    for c in (args.drop_class or []):
        drop_classes.add(c)
    drop_ids = {i for i in closure if byid[i].cls in drop_classes}
    kept_ids = {i for i in closure if i not in drop_ids}
    dropped_by_class = Counter(byid[i].cls for i in drop_ids)
    # 哪些 GameObject 因此少了组件
    go_with_drops = set()
    for gid in go_ids:
        if any(c in drop_ids for c in components_of(byid[gid])):
            go_with_drops.add(gid)

    # --- 内部引用完整性（丢组件前先查一遍，丢了之后查「有 guid 的」不受影响） ---
    def scan_refs(ids):
        bare_in, bare_out, ext = set(), set(), set()
        for i in ids:
            for m in REF_FLOW_RE.finditer(byid[i].text):
                n = int(m.group(1))
                if n == 0:
                    continue
                if "guid:" in m.group(2):
                    continue
                (bare_in if n in closure else bare_out).add((byid[i].cls, n))
        for i in ids:
            for m in GUID_REF_RE.finditer(byid[i].text):
                ext.add(m.group(2).lower())
        return bare_in, bare_out, ext

    _, bare_out_before, ext_guids_before = scan_refs(closure)
    _, bare_out_after, ext_guids_after = scan_refs(kept_ids)

    # --- 重编号 ---
    idmap = {}
    for n, old in enumerate(sorted(kept_ids), start=1):
        idmap[old] = old + RENUMBER_OFFSET
    if len(set(idmap.values())) != len(idmap):
        raise SystemExit("FATAL: 重编号后 id 不唯一")

    prefab_text = render_prefab(byid, kept_ids, idmap, drop_ids, root_doc.id,
                                args.zero_root_position, line_overrides)

    # --- 自检（写文件之前先把文本查一遍） ---
    checks = []

    # 从内存文本再切一次，自检就查「真的会落盘的那份文本」，不是查中间结构
    def parse_text(txt):
        ds = []
        cur = None
        for ln in txt.split("\n"):
            m = DOC_RE.match(ln)
            if m:
                cur = Doc(int(m.group(1)), int(m.group(2)), ln)
                ds.append(cur)
            elif cur is not None:
                cur.lines.append(ln)
        for d in ds:
            while d.lines and d.lines[-1].strip() == "":
                d.lines.pop()
        return ds, {d.id: d for d in ds}

    out_docs, out_byid = parse_text(prefab_text)
    out_go = [d for d in out_docs if d.type == 1]
    out_tr = [d for d in out_docs if d.type in (4, 224)]

    # 1) 节点数
    _node_detail = "%d 个 GameObject / %d 个 Transform（原版子树 %d" % (len(out_go), len(out_tr), len(transform_ids))
    if merge_roots:
        _node_detail += " = 主根 %d + %s" % (
            nodes_main, " + ".join("%r 的 %d" % (x["name"], x["nodes"]) for x in merge_info))
    _node_detail += "）"
    checks.append(("节点数 = 原版子树节点数",
                   len(out_go) == len(transform_ids) == len(out_tr), _node_detail))

    # 2) PrefabInstance 非零 = 0
    bad_pi = [d.id for d in out_docs if re.search(r"m_PrefabInstance: \{fileID: (?!0\})", d.text)]
    bad_cso = [d.id for d in out_docs if re.search(r"m_CorrespondingSourceObject: \{fileID: (?!0\})", d.text)]
    checks.append(("m_PrefabInstance 非零 = 0", len(bad_pi) == 0, "非零 %d 处" % len(bad_pi)))
    checks.append(("m_CorrespondingSourceObject 非零 = 0", len(bad_cso) == 0, "非零 %d 处" % len(bad_cso)))

    # 3) 根 m_Father = 0，且只有一个 m_Father=0 的 Transform
    root_new = idmap[root_doc.id]
    root_out = out_byid.get(root_new)
    father_zero = []
    for d in out_tr:
        f = field(d, "m_Father")
        if f and fileid_of(f) == 0:
            father_zero.append(d.id)
    ok_root = (root_out is not None
               and fileid_of(field(root_out, "m_Father")) == 0
               and father_zero == [root_new])
    checks.append(("根 m_Father: {fileID: 0}", ok_root,
                   "m_Father=0 的 Transform 共 %d 个%s" % (len(father_zero),
                    "" if father_zero == [root_new] else "（期望只有根 &%d）" % root_new)))

    # 4) 内部 bare 引用全部落在产物内
    out_ids = set(out_byid)
    dangling = set()
    for d in out_docs:
        for m in REF_FLOW_RE.finditer(d.text):
            n = int(m.group(1))
            if n == 0 or "guid:" in m.group(2):
                continue
            if n not in out_ids:
                dangling.add((d.cls, d.id, n))
    checks.append(("无 guid 的 fileID 引用全部落在产物内", len(dangling) == 0,
                   "悬空 %d 处%s" % (len(dangling), ("：" + repr(sorted(dangling)[:6])) if dangling else "")))

    # 5) fileID 唯一 & 都不小于 RENUMBER_OFFSET
    ids_out = [d.id for d in out_docs]
    checks.append(("fileID 唯一", len(ids_out) == len(set(ids_out)), "共 %d 个" % len(ids_out)))
    small = [i for i in ids_out if i < RENUMBER_OFFSET]
    checks.append(("fileID 全是大整数（≥1e15）", not small, "小号 %d 个" % len(small)))

    # 6) YAML 可解析
    ok_yaml, yaml_msg = yaml_parse_check(prefab_text)
    checks.append(("PyYAML 逐文档解析", ok_yaml, yaml_msg))

    # 7) 结构：m_Children ↔ m_Father 一一对应（试点的那三条「额外结构核对」收进工具）
    out_tr_by = {d.id: d for d in out_tr}
    struct_bad = []
    for d in out_tr:
        for c in children_of(d):
            cd = out_tr_by.get(c)
            if cd is None:
                struct_bad.append((d.id, c, "child 不在产物里"))
                continue
            f = fileid_of(field(cd, "m_Father") or "")
            if f != d.id:
                struct_bad.append((d.id, c, "child 的 m_Father=%r" % f))
    for d in out_tr:
        f = fileid_of(field(d, "m_Father") or "")
        if not f:
            continue
        pd = out_tr_by.get(f)
        if pd is None or d.id not in children_of(pd):
            struct_bad.append((f, d.id, "父节点 m_Children 里没有它"))
    checks.append(("m_Children ↔ m_Father 一一对应", not struct_bad,
                   "错配 %d 处%s" % (len(struct_bad), ("：" + repr(struct_bad[:4])) if struct_bad else "")))

    # 8) 组件引用：GameObject.m_Component → 产物；组件.m_GameObject → 产物
    out_go_ids = {d.id for d in out_go}
    comp_bad = []
    for go in out_go:
        for c in components_of(go):
            if c not in out_byid:
                comp_bad.append((go.id, c, "m_Component 指向的文档不在产物里"))
        if not components_of(go):
            comp_bad.append((go.id, None, "m_Component 为空"))
    for d in out_docs:
        if d.type == 1:
            continue
        g = fileid_of(field(d, "m_GameObject") or "")
        if g and g not in out_go_ids:
            comp_bad.append((d.id, g, "%s 的 m_GameObject 不在产物里" % d.cls))
    checks.append(("组件引用双向闭合", not comp_bad,
                   "悬空 %d 处%s" % (len(comp_bad), ("：" + repr(comp_bad[:4])) if comp_bad else "")))

    # 9) 合并：世界变换保持（逐节点比 源场景 vs 产物）
    if merge_roots and not args.zero_root_position:
        memo_src, memo_dst = {}, {}
        worst = (0.0, None)
        worst_s = (0.0, None)
        min_dot = (1.0, None)
        bad_world = 0
        for old in sorted(transform_ids):
            src = world_trs(byid, old, memo_src)
            dst = world_trs(out_byid, idmap[old], memo_dst)
            pe = max(abs(a - b) for a, b in zip(src[0], dst[0]))
            se = max(abs(a - b) for a, b in zip(src[2], dst[2]))
            tol_p = 1e-4 + 1e-6 * max(abs(x) for x in src[0])
            tol_s = 1e-4 + 1e-6 * max(abs(x) for x in src[2])
            if pe > tol_p or se > tol_s:
                bad_world += 1
            if pe > worst[0]:
                worst = (pe, old)
            if se > worst_s[0]:
                worst_s = (se, old)
            a = src[1]
            b = dst[1]
            na = sum(x * x for x in a) ** 0.5 or 1.0
            nb = sum(x * x for x in b) ** 0.5 or 1.0
            dot = abs(sum(x * y for x, y in zip(a, b)) / (na * nb))
            if dot < min_dot[0]:
                min_dot = (dot, old)
        checks.append(("合并后世界变换保持（逐节点）", bad_world == 0,
                       "越限 %d / %d 节点 · 位置最大偏差 %.3g · scale 最大偏差 %.3g · 四元数最差点积 %.9f"
                       % (bad_world, len(transform_ids), worst[0], worst_s[0], min_dot[0])))

    # 10) 合并：跨根 bare 引用的目标都在产物内
    if merge_roots:
        miss = [c for c in cross_refs if c[2] not in kept_ids or idmap.get(c[2]) not in out_byid]
        _xr_detail = "%d 条跨根 bare 引用，目标缺失 %d 个" % (len(cross_refs), len(miss))
        if cross_refs:
            _xr_detail += "（目标新 id：%s）" % [idmap.get(c[2]) for c in cross_refs]
        checks.append(("跨根 bare 引用的目标都在产物内", not miss, _xr_detail))

    # --- 外引解析 ---
    our_index, our_meta_n, our_secs = build_guid_index(OUR_ASSETS_DEFAULT)
    ext_guids = set()
    ext_uses = Counter()
    for d in out_docs:
        for m in GUID_REF_RE.finditer(d.text):
            g = m.group(2).lower()
            ext_guids.add(g)
            ext_uses[g] += 1

    exported_index, exported_meta_n, exported_secs = build_guid_index(
        os.path.join(args.exported_root, "Assets"))

    # --- 拷闭包资产 ---
    copy_exts = set(DEFAULT_COPY_EXTS)
    if args.copy_exts:
        for e in args.copy_exts.split(","):
            e = e.strip().lower()
            if e:
                copy_exts.add(e if e.startswith(".") else "." + e)

    SKIP_ALWAYS = {".shader", ".cs", ".dll", ".controller", ".playable", ".asmdef"}
    copied = []           # (guid, src, dst, bytes)
    skipped = {}          # ext -> [ (guid, path, reason) ]
    not_found = []        # guid
    already_ours = []     # (guid, our_path)
    seen_guids = set()
    queue = list(ext_guids)
    copied_bytes = 0
    new_files = 0

    # --- 目录 .meta（确定性 guid；已存在就不动） ---
    def ensure_folder_meta(folder):
        if not os.path.isdir(folder):
            return
        mp = folder + ".meta"
        if os.path.exists(mp):
            return
        rel = os.path.relpath(folder, out_root).replace("\\", "/")
        with io.open(mp, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(FOLDER_META_TMPL.format(guid=deterministic_guid("folder:" + rel)))

    def ensure_ancestor_metas(path):
        """给 path 的所有上级目录（到 out_root 为止）补 .meta。"""
        root = os.path.normcase(os.path.abspath(out_root))
        head = os.path.abspath(os.path.dirname(path))
        chain = []
        while True:
            if os.path.normcase(head) == root:
                break
            parent = os.path.dirname(head)
            if parent == head:
                return          # 走到盘根还没碰到 out_root ⇒ 不是我们的树，不补
            chain.append(head)
            head = parent
        for p in reversed(chain):
            ensure_folder_meta(p)

    if not (args.dry_run or args.no_copy):
        os.makedirs(out_dir, exist_ok=True)
        ensure_ancestor_metas(os.path.join(out_dir, "x"))       # 含 out_dir 本身

    while queue:
        g = queue.pop(0).lower()
        if g in seen_guids:
            continue
        seen_guids.add(g)
        if g in BUILTIN_GUIDS:
            skipped.setdefault("<builtin>", []).append((g, BUILTIN_GUIDS[g], "Unity 内建，不需要拷"))
            continue
        # 我们工程里已有同 guid ⇒ 跳过（引用自动落到已有资产）
        if g in our_index:
            already_ours.append((g, our_index[g]))
            continue
        src = exported_index.get(g)
        if src is None:
            not_found.append(g)
            continue
        ext = os.path.splitext(src)[1].lower()
        if ext in SKIP_ALWAYS:
            skipped.setdefault(ext, []).append((g, src, "按设计不拷（%s）" % ext))
            continue
        if ext not in copy_exts:
            skipped.setdefault(ext, []).append((g, src, "不在 copy-exts 里"))
            continue
        rel = os.path.relpath(src, os.path.join(args.exported_root, "Assets")).replace("\\", "/")
        dst = os.path.join(out_dir, rel)
        size = os.path.getsize(src)
        copied.append((g, src, dst, size))
        copied_bytes += size
        if not args.dry_run:
            os.makedirs(os.path.dirname(dst), exist_ok=True)
            ensure_ancestor_metas(dst)
            same = os.path.exists(dst) and os.path.getsize(dst) == size \
                   and io.open(dst, "rb").read() == io.open(src, "rb").read()
            if not same:
                with io.open(src, "rb") as fh:
                    data = fh.read()
                with io.open(dst, "wb") as fh:
                    fh.write(data)
                new_files += 1
            # .meta 原样拷（guid 必须保持）
            meta_src = src + ".meta"
            meta_dst = dst + ".meta"
            if os.path.isfile(meta_src) and not os.path.exists(meta_dst):
                with io.open(meta_src, "rb") as fh:
                    mdata = fh.read()
                with io.open(meta_dst, "wb") as fh:
                    fh.write(mdata)
        # 递归：文本资产里还有 guid 引用（材质→贴图、Sprite→Texture2D）
        if ext in (".mat", ".asset", ".controller", ".playable", ".anim", ".prefab", ".spriteatlas"):
            try:
                t = io.open(src, "rb").read().decode("utf-8", "replace")
            except OSError:
                t = ""
            for mm in re.finditer(r"guid: ([0-9a-fA-F]{32})", t):
                ng = mm.group(1).lower()
                if ng not in seen_guids:
                    queue.append(ng)

    # --- 外引解析分类（复制之后） ---
    resol = {"copied": [], "already_ours": [], "exported_only": [], "builtin": [], "UNRESOLVED": []}
    copied_guids = {c[0] for c in copied}
    ours_guids = {a[0] for a in already_ours}
    for g in sorted(ext_guids):
        if g in copied_guids:
            resol["copied"].append(g)
        elif g in ours_guids:
            resol["already_ours"].append(g)
        elif g in BUILTIN_GUIDS:
            resol["builtin"].append(g)
        elif g in exported_index:
            resol["exported_only"].append(g)
        else:
            resol["UNRESOLVED"].append(g)

    resolved_ok = (len(resol["UNRESOLVED"]) == 0) and (len(not_found) == 0)
    checks.append((
        "每个外引 {fileID,guid,type} 都能解析（复制过来的 + 我们工程 + ExportedProject + Unity 内建）",
        resolved_ok,
        "全部 %d 个 guid：拷贝 %d · 我们已有 %d · 仅 ExportedProject %d · 内建 %d · 解不到 %d"
        % (len(ext_guids), len(resol["copied"]), len(resol["already_ours"]),
           len(resol["exported_only"]), len(resol["builtin"]), len(resol["UNRESOLVED"]))))

    # --- 写 prefab ---
    prefab_bytes = 0
    if not args.dry_run:
        os.makedirs(out_dir, exist_ok=True)
        ensure_ancestor_metas(prefab_path)
        data = prefab_text.encode("utf-8")
        with io.open(prefab_path, "wb") as fh:
            fh.write(data)
        prefab_bytes = len(data)
        meta_p = prefab_path + ".meta"
        if not os.path.exists(meta_p):
            rel = os.path.relpath(prefab_path, out_root).replace("\\", "/")
            with io.open(meta_p, "w", encoding="utf-8", newline="\n") as fh:
                fh.write(PREFAB_META_TMPL.format(guid=deterministic_guid("prefab:" + rel)))

    # --- 名称重复检查（我们工程里已有同名资产 —— guid 不同，会变成两份） ---
    our_names = {}
    for g, p in our_index.items():
        stem = os.path.splitext(os.path.basename(p))[0].lower()
        our_names.setdefault(stem, []).append(p)
    name_dups = []
    for g, src, dst, n in copied:
        stem = os.path.splitext(os.path.basename(dst))[0].lower()
        if stem in our_names:
            name_dups.append((os.path.basename(dst), our_names[stem][0]))

    # --- 摘要 ---
    summary = {
        "arena": arena,
        "scene": scene_rel,
        "root": args.root,
        "prefab_path": prefab_path,
        "prefab_bytes": prefab_bytes,
        "prefab_md5": hashlib.md5(prefab_text.encode("utf-8")).hexdigest(),
        "prefab_lines": prefab_text.count("\n"),
        "out_root": out_root,
        "out_dir": out_dir,
        "dry_run": args.dry_run,
        "merge_roots": merge_info,
        "cross_root_bare_refs": [[c[0], c[1], c[2], idmap.get(c[2])] for c in cross_refs],
        "nodes": len(transform_ids),
        "nodes_main_tree": nodes_main,
        "docs_total_source": len(docs),
        "closure_docs": len(closure),
        "kept_docs": len(kept_ids),
        "dropped_docs": len(drop_ids),
        "dropped_by_class": dict(dropped_by_class),
        "go_with_dropped_components": len(go_with_drops),
        "bare_refs_escaping_before": len(bare_out_before),
        "bare_refs_escaping_after": len(bare_out_after),
        "external_guids": len(ext_guids),
        "external_uses": sum(ext_uses.values()),
        "copied_files": len(copied),
        "copied_bytes": copied_bytes,
        "copied_by_ext": dict(Counter(os.path.splitext(c[1])[1].lower() for c in copied)),
        "skipped_by_ext": {k: len(v) for k, v in sorted(skipped.items())},
        "resolution": {k: len(v) for k, v in resol.items()},
        "checks": [{"name": n, "ok": bool(ok), "detail": det} for n, ok, det in checks],
        "all_checks_ok": all(ok for _, ok, _ in checks) and resolved_ok,
        "name_dups_vs_our_project": name_dups,
        "our_meta_scanned": our_meta_n,
        "exported_meta_scanned": exported_meta_n,
        "elapsed_sec": round(time.time() - t_start, 1),
    }

    # --- 打印 ---
    print("=" * 78)
    print("arena=%s root=%s  scene=%s" % (arena, args.root, scene_rel))
    if merge_roots:
        for x in merge_info:
            print("merge: %r -> 挂在 %r 下 · %d 节点 · local TRS=%s"
                  % (x["name"], args.root, x["nodes"], x["local_trs"]["pos"]))
    print("nodes=%d  closure=%d docs  kept=%d  dropped=%d %s"
          % (len(transform_ids), len(closure), len(kept_ids), len(drop_ids), dict(dropped_by_class)))
    print("prefab: %s (%d B)" % (prefab_path, prefab_bytes))
    print("copied: %d files / %.2f MB  %s%s" % (len(copied), copied_bytes / 1048576.0,
                                                dict(Counter(os.path.splitext(c[1])[1].lower() for c in copied)),
                                                ("" if args.dry_run else "   (本次新写 %d 个，其余已存在)" % new_files)))
    print("skipped: %s" % {k: len(v) for k, v in sorted(skipped.items())})
    print("resolution: %s" % {k: len(v) for k, v in resol.items()})
    print("-" * 78)
    for n, ok, det in checks:
        print("[%s] %s — %s" % ("OK " if ok else "FAIL", n, det))
    print("-" * 78)
    print("ALL CHECKS: %s   (%.1fs)" % ("PASS" if summary["all_checks_ok"] else "FAIL", summary["elapsed_sec"]))
    if name_dups:
        print("⚠️ 我们工程里已有同名资产 %d 个（guid 不同 ⇒ 会成为两份）：" % len(name_dups))
        for a, b in name_dups[:10]:
            print("   %-40s ~ %s" % (a, b))
    for ext, lst in sorted(skipped.items()):
        if ext == "<builtin>":
            continue
        print("⏭ 未拷 %s × %d：%s" % (ext, len(lst), ", ".join(sorted({os.path.basename(x[1]) for x in lst}))[:200]))
    if resol["UNRESOLVED"]:
        print("🔴 解不到的 guid：%s" % resol["UNRESOLVED"])
    if not_found:
        print("🔴 not_found：%s" % not_found)
    print("=" * 78)

    if args.json_out:
        with io.open(args.json_out, "w", encoding="utf-8", newline="\n") as fh:
            fh.write(json.dumps(summary, ensure_ascii=False, indent=2))

    return 0 if summary["all_checks_ok"] else 1


if __name__ == "__main__":
    sys.exit(main())
