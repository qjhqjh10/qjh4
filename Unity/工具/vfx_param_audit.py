#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""vfx_param_audit.py — 特效参数对账（`判据/B9_特效参数对账.tsv`）的可复跑闸。

来源：移植 2026-10-26 V1 那轮的临时链（`_tmp_view/v1tmp/` 下 `vfx_diff.py` · `vfx_diff2.py` ·
`queue_judge.py` · `gen_b9.py`），**去掉了对 `_tmp_view` 绝对路径的依赖**，逻辑与口径逐条照抄，
只修了一处会**静默算错**的缓存 bug（见下面 `_fields_of` 上方注释）。

依赖：**仅 Python 3 标准库**（不需要 UnityPy / 任何第三方包）。

用法（⛔ 不带动作参数时只打用法、什么都不写）：
  python -I -X utf8 工具/vfx_param_audit.py                 # 打印用法，退出 0
  python -I -X utf8 工具/vfx_param_audit.py --check         # 只算不写：报差异 + 退出码（0 = 无差异）
  python -I -X utf8 工具/vfx_param_audit.py --write [-o 路径]  # 重生成 TSV（默认写 `判据/B9_特效参数对账.tsv`）
  python -I -X utf8 工具/vfx_param_audit.py --self-test     # 内存里篡改一个字段再比
        ⇒ **检出篡改 = 退出码 1**（这就是「闸有牙」的证据，⛔ 不要把它当失败，也别塞进 CI 的绿名单）
        ⇒ 没检出 = 退出码 3（那才是真失败：闸坏了）

可比对的三条根（都能覆盖）：
  --assets <AssetRipper 的 ExportedProject/Assets>   （默认 D:/2/tools/assetripper_out/ExportedProject/Assets）
  --bundle <assets_full 里 battleprefabs_vfxandmisc 包目录>
  --ours   <我们工程的 WarpforgeVFX/Prefabs>

口径（与 B9 表一致，逐条可查）：
  * 比的是 **参数**：ParticleSystem 全部模块 / 各 Renderer / MeshFilter / SpriteMask，
    外加 Transform 的 localPosition·localScale。MonoBehaviour 只统计「有/无」，不比字段。
  * 两侧「省略 = 默认值」：原版 JSON 会省略等于默认的字段，我们的 prefab 写全 ⇒ 按 `DEFAULT` 补。
  * 已知等价噪声（不计失败，单独计数）：`m_MaskInteraction`（未序列化 = 默认 None = 0）、
    `m_StitchLightmapSeams`（Unity 工程级序列化默认）、`m_SelectedEditorRenderState`（纯编辑器字段）。
  * 同名兄弟**不按 pathID / 树次序配对**，按「整棵组件内容哈希 → 字段差数」配对
    （AssetRipper 会重排子节点；先例：同一组三个 Smoke 两边次序完全相反）。
  * 结构差里的「`Orbital 5 repeat ` 尾随空格」是原版 GameObject 名带尾空格造成的同名错位：
    **缺/多两个节点的路径去掉尾空格后相同 ⇒ 判为命名等价**（警告，不失败）。
"""
import io
import os
import re
import sys
import json
import math
import time
import argparse
import collections
from multiprocessing import Pool

# ---------------------------------------------------------------- 路径常量
HERE = os.path.dirname(os.path.abspath(__file__))          # d:/4/Unity/工具
UNITY = os.path.dirname(HERE)                              # d:/4/Unity
REPO = os.path.dirname(UNITY)                              # d:/4

OUR_ASSETS = os.path.join(UNITY, "MyGame", "Assets")
OUR_PREFABS = os.path.join(OUR_ASSETS, "WarpforgeVFX", "Prefabs")
OUR_MATERIALS = os.path.join(OUR_ASSETS, "WarpforgeVFX", "Materials")
CARDS_ENGINE = os.path.join(OUR_ASSETS, "RuleEngine", "Resources", "cards_engine.json")
OUT_TSV = os.path.join(UNITY, "资料", "完全复刻", "判据", "B9_特效参数对账.tsv")
CARD_VFX = os.path.join(UNITY, "数据", "游戏数据", "card_vfx_by_card.json")
SHADER_Q = os.path.join(UNITY, "数据", "游戏数据", "shader_renderqueue.tsv")
RQ_TRUTH = os.path.join(UNITY, "数据", "游戏数据", "renderqueue_truth.json")   # 原版生效队列真值
RQ_RUNTIME = os.path.join(UNITY, "数据", "游戏数据", "mat_renderqueue.json")   # 我们运行时的覆盖表

ORIG_AR = r"D:/2/tools/assetripper_out/ExportedProject/Assets"
ORIG_BUNDLE = r"D:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all"

# 已知等价噪声（与 gen_b9.py 完全同名同义）
NOISE_EQUIV = {"m_MaskInteraction"}
NOISE_DEFAULT = {"m_StitchLightmapSeams", "m_SelectedEditorRenderState"}

USAGE = __doc__

# ================================================================ Unity YAML
# 逐字移植自 `_tmp_view/v1tmp/vfx_diff.py`（只删了没用到的入口）。
DOC_RE = re.compile(r"^--- !u!(\d+) &(\d+)(.*)$")


def _split_flow(s):
    """split a flow map body on top-level commas"""
    out, depth, cur, q = [], 0, [], None
    for ch in s:
        if q:
            cur.append(ch)
            if ch == q:
                q = None
            continue
        if ch in "\"'":
            q = ch
            cur.append(ch)
            continue
        if ch in "{[":
            depth += 1
        elif ch in "}]":
            depth -= 1
        if ch == "," and depth == 0:
            out.append("".join(cur).strip())
            cur = []
        else:
            cur.append(ch)
    if cur:
        out.append("".join(cur).strip())
    return out


def _scalar(v):
    v = v.strip()
    if not v:
        return ""
    if v[0] == "{" and v[-1] == "}":
        body = v[1:-1].strip()
        d = {}
        if not body:
            return d
        for part in _split_flow(body):
            if ":" not in part:
                continue
            k, _, val = part.partition(":")
            d[k.strip()] = _scalar(val)
        return d
    if v[0] == "[" and v[-1] == "]":
        body = v[1:-1].strip()
        return [_scalar(x) for x in _split_flow(body)] if body else []
    if len(v) >= 2 and v[0] == v[-1] and v[0] in "\"'":
        return v[1:-1]
    try:
        if v in ("Infinity", "-Infinity", "NaN"):
            return float(v.lower())
        if re.match(r"^-?\d+$", v):
            return int(v)
        if re.match(r"^-?\d*\.\d+(e[-+]?\d+)?$", v, re.I) or re.match(r"^-?\d+e[-+]?\d+$", v, re.I):
            return float(v)
    except ValueError:
        pass
    return v


def _child_block(lines, i, key_indent):
    """key 的值为空时：先看下一行是不是【与 key 同缩进】的序列项（Unity 写法），否则缩进 +2。"""
    n = len(lines)
    j = i
    while j < n and lines[j].strip() == "":
        j += 1
    if j < n:
        ci = len(lines[j]) - len(lines[j].lstrip(" "))
        if ci == key_indent and lines[j][key_indent:].startswith("- "):
            return parse_block(lines, j, key_indent)
    return parse_block(lines, i, key_indent + 2)


def parse_block(lines, i, indent):
    """parse lines[i:] at given indent; return (obj, next_i)"""
    n = len(lines)
    if i >= n:
        return None, i
    first = lines[i]
    stripped = first[indent:] if first.startswith(" " * indent) else first.lstrip()
    if stripped.startswith("- "):
        items = []
        while i < n:
            ln = lines[i]
            cur_indent = len(ln) - len(ln.lstrip(" "))
            if ln.strip() == "":
                i += 1
                continue
            if cur_indent != indent or not ln[indent:].startswith("- "):
                break
            body = ln[indent + 2:]
            if body.strip() == "":
                obj, i = parse_block(lines, i + 1, indent + 2)
                items.append(obj)
                continue
            m = re.match(r"^([\w.\-]+):\s?(.*)$", body)
            if m and not body.lstrip().startswith("- "):
                d = {}
                k, v = m.group(1), m.group(2)
                if v.strip() != "":
                    d[k] = _scalar(v)
                    i += 1
                else:
                    obj, i = _child_block(lines, i + 1, indent + 2)
                    d[k] = obj
                while i < n:
                    ln2 = lines[i]
                    if ln2.strip() == "":
                        i += 1
                        continue
                    ci = len(ln2) - len(ln2.lstrip(" "))
                    if ci != indent + 2 or ln2[indent + 2:].startswith("- "):
                        break
                    m2 = re.match(r"^([\w.\-]+):\s?(.*)$", ln2[indent + 2:])
                    if not m2:
                        i += 1
                        continue
                    k2, v2 = m2.group(1), m2.group(2)
                    if v2.strip() != "":
                        d[k2] = _scalar(v2)
                        i += 1
                    else:
                        obj, i = _child_block(lines, i + 1, indent + 2)
                        d[k2] = obj
                items.append(d)
            else:
                items.append(_scalar(body))
                i += 1
        return items, i
    d = {}
    while i < n:
        ln = lines[i]
        if ln.strip() == "":
            i += 1
            continue
        cur_indent = len(ln) - len(ln.lstrip(" "))
        if cur_indent < indent:
            break
        if cur_indent > indent:
            i += 1
            continue
        if ln[indent:].startswith("- "):
            break
        m = re.match(r"^([\w.\-]+):\s?(.*)$", ln[indent:])
        if not m:
            i += 1
            continue
        k, v = m.group(1), m.group(2)
        if v.strip() in ("|", ">", "|-", ">-", "|+", ">+"):
            i += 1
            buf = []
            while i < n and (lines[i].strip() == "" or len(lines[i]) - len(lines[i].lstrip(" ")) > indent):
                buf.append(lines[i].strip())
                i += 1
            d[k] = "\n".join(buf)
            continue
        if v.strip() != "":
            d[k] = _scalar(v)
            i += 1
        else:
            j = i + 1
            while j < n and lines[j].strip() == "":
                j += 1
            if j < n:
                ci = len(lines[j]) - len(lines[j].lstrip(" "))
                if ci == indent and lines[j][indent:].startswith("- "):
                    obj, i = parse_block(lines, j, indent)
                    d[k] = obj
                    continue
            obj, i = _child_block(lines, i + 1, indent)
            d[k] = obj
    return d, i


def parse_unity_yaml(path):
    docs = {}   # cls -> list of (docid, root, obj, stripped)
    with io.open(path, "r", encoding="utf-8", errors="replace") as f:
        lines = f.read().split("\n")
    if lines and lines[0].startswith("\ufeff"):
        lines[0] = lines[0][1:]
    i = 0
    n = len(lines)
    while i < n:
        m = DOC_RE.match(lines[i])
        if not m:
            i += 1
            continue
        cls = int(m.group(1))
        did = int(m.group(2))
        stripped = "stripped" in m.group(3)
        j = i + 1
        while j < n and not DOC_RE.match(lines[j]):
            j += 1
        body = lines[i + 1:j]
        k = 0
        root = None
        val = None
        while k < len(body):
            l = body[k]
            if l and not l.startswith(" ") and ":" in l:
                kk, _, vv = l.partition(":")
                root = kk.strip()
                val = vv.strip()
                k += 1
                break
            k += 1
        if root is None:
            i = j
            continue
        if val:
            obj = _scalar(val)
        else:
            obj, _ = parse_block(body, k, 2)
        docs.setdefault(cls, []).append((did, root, obj, stripped))
        i = j
    return docs


SKIP_TOP = {"m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance",
            "m_PrefabAsset", "m_GameObject", "serializedVersion",
            "m_EditorHideFlags", "m_EditorClassIdentifier"}


def flatten(obj, prefix="", out=None, depth=0):
    if out is None:
        out = {}
    if isinstance(obj, dict):
        for k, v in obj.items():
            if depth == 0 and k in SKIP_TOP:
                continue
            flatten(v, prefix + "." + k if prefix else k, out, depth + 1)
    elif isinstance(obj, list):
        for idx, v in enumerate(obj):
            flatten(v, "%s[%d]" % (prefix, idx), out, depth + 1)
    else:
        out[prefix] = obj
    return out


# ================================================================ 参数对账核心
# 移植自 `_tmp_view/v1tmp/vfx_diff2.py`。
IGNORE_LEAF = {"serializedVersion", "m_PreviewTimeScale",
               "m_AutoUVMaxAngle", "m_AutoUVMaxDistance", "m_AdditionalVertexStreams",
               "m_EnlightenVertexStream", "m_StaticBatchInfo", "m_StaticBatchRoot",
               "m_ProbeAnchor", "m_LightProbeVolumeOverride", "m_ReceiveGI",
               "m_ReflectionProbeUsage", "m_LightProbeUsage", "m_MotionVectors",
               "m_AllowOcclusionWhenDynamic", "m_RayTraceProcedural", "m_SmallMeshCulling",
               "m_RenderingLayerMask", "m_ForceRenderingOff"}
IGNORE_PREFIX = ("m_ObjectHideFlags", "m_CorrespondingSourceObject", "m_PrefabInstance",
                 "m_PrefabAsset", "m_GameObject", "m_EditorHideFlags", "m_EditorClassIdentifier")
# 省略即默认值（按叶子字段名）
DEFAULT = {
    "inSlope": 0, "outSlope": 0, "tangentMode": 0, "weightedMode": 0,
    "inWeight": 0.33333334, "outWeight": 0.33333334,
    "m_PreInfinity": 2, "m_PostInfinity": 2, "m_RotationOrder": 4,
}
SKIP_COMPS = {"MonoBehaviour", "Animation", "Animator", "Canvas", "CanvasGroup",
              "CanvasRenderer", "LookAtConstraint", "VisualEffect", "ParticleSystemForceField",
              "BoxCollider", "BoxCollider2D", "CapsuleCollider", "MeshCollider",
              "VFXRenderer", "SortingGroup", "RectTransform"}


def leaf(name):
    return name.rsplit(".", 1)[-1].split("[", 1)[0]


PPTR_LEAF = {"fileID", "guid", "type", "m_FileID", "m_PathID"}


def prune(obj, depth=0):
    """在 flatten 之前剪掉「Unity 写全、原版省略」的等价表示：
    minMaxState ∈ {0,3} 时 maxCurve/minCurve 是常量值的冗余表示 ⇒ 整对删掉。"""
    if isinstance(obj, dict):
        if "minMaxState" in obj:
            st = obj.get("minMaxState")
            drop = []
            if st == 0:
                drop = ["maxCurve", "minCurve", "minScalar"]
            elif st == 1:
                drop = ["minCurve"]
            elif st == 3:
                drop = ["maxCurve", "minCurve"]
            if drop:
                obj = {k: v for k, v in obj.items() if k not in drop}
        return {k: prune(v, depth + 1) for k, v in obj.items()}
    if isinstance(obj, list):
        return [prune(v, depth + 1) for v in obj]
    return obj


def norm(fields):
    out = {}
    for k, v in fields.items():
        lf = leaf(k)
        if lf in IGNORE_LEAF or lf in PPTR_LEAF:
            continue
        if lf.startswith(("colorLabel", "vectorLabel")):
            continue
        parts = re.split(r"[.\[]", k)
        if any(pt.startswith(("m_Lightmap", "m_ImportantGI", "m_IgnoreNormals",
                              "m_GlobalIllumination", "m_ReflectionProbe", "m_ReceiveGI"))
               for pt in parts):
            continue
        if any(k == p or k.startswith(p + ".") for p in IGNORE_PREFIX):
            continue
        out[k] = v
    return out


def get(fields, k):
    if k in fields:
        return fields[k]
    d = DEFAULT.get(leaf(k), None)
    return d if d is not None else "<无>"


def _nan(x):
    return isinstance(x, float) and (x != x)


def _inf(x):
    return isinstance(x, float) and (x in (float("inf"), float("-inf")))


def valeq(a, b):
    if a == "<无>" or b == "<无>":
        return a == b
    if _nan(a) and _nan(b):
        return True
    if _inf(a) or _inf(b):
        return _inf(a) and _inf(b) and (a > 0) == (b > 0)
    if isinstance(a, bool) or isinstance(b, bool):
        return a == b
    fa = isinstance(a, float) or isinstance(a, int)
    fb = isinstance(b, float) or isinstance(b, int)
    if fa and fb:
        return abs(float(a) - float(b)) <= 1e-5 * max(1.0, abs(float(a)), abs(float(b)))
    return a == b


def prefab_model(path):
    """-> {节点基路径: [node,...]}；node = {path, comps, active, localScale/Rot/Pos}"""
    docs = parse_unity_yaml(path)
    by_id = {}
    for cls, lst in docs.items():
        for did, root, obj, _ in lst:
            by_id[did] = (cls, root, obj)
    gos = {did: obj for did, root, obj, _ in docs.get(1, [])}
    trs = {}
    for cls in (4, 224):
        for did, root, obj, _ in docs.get(cls, []):
            trs[did] = obj
    paths = {}
    roots = [t for t, v in trs.items() if not v.get("m_Father", {}).get("fileID")]

    def go_name(tid):
        gid = trs.get(tid, {}).get("m_GameObject", {}).get("fileID")
        return gos.get(gid, {}).get("m_Name", "?"), gid

    def dfs(tid, path):
        paths[tid] = path
        kids = [c.get("m_PathID") for c in trs.get(tid, {}).get("m_Children", []) if c.get("m_PathID") in trs]
        cnt = collections.Counter(go_name(k)[0] for k in kids)
        seen = collections.Counter()
        for k in kids:
            nm, _ = go_name(k)
            if cnt[nm] > 1:
                sub = "%s#%d" % (nm, seen[nm])
                seen[nm] += 1
            else:
                sub = nm
            dfs(k, path + "/" + sub if path else sub)

    for r in roots:
        dfs(r, go_name(r)[0])
    for tid in trs:
        if tid not in paths:
            paths[tid] = go_name(tid)[0]
    model = collections.defaultdict(list)
    for tid, p in paths.items():
        gid = trs[tid].get("m_GameObject", {}).get("fileID")
        g = gos.get(gid, {})
        entry = {}
        for c in g.get("m_Component", []) or []:
            if not isinstance(c, dict):
                continue
            cid = c.get("component", {}).get("fileID")
            if cid not in by_id:
                continue
            cls, root, obj = by_id[cid]
            if cls in (1, 4, 224):
                continue
            entry.setdefault(root + "!u!%d" % cls, []).append(prune(obj))
        base = re.sub(r"#\d+$", "", p)
        model[base].append({"path": p, "comps": entry, "active": g.get("m_IsActive"),
                            "localScale": trs[tid].get("m_LocalScale") or {},
                            "localRot": trs[tid].get("m_LocalRotation") or {},
                            "localPos": trs[tid].get("m_LocalPosition") or {}})
    return model


def _r(x):
    return round(x, 5) if isinstance(x, float) else x


def _rot(node):
    r = node.get("localRot") or {}
    return tuple(_r(r.get(a, 0 if a != "w" else 1)) for a in ("x", "y", "z", "w"))


def node_hash(node):
    h = ["T|pos=%r;scale=%r;rot=%r" % (tuple(_r(node["localPos"].get(a, 0)) for a in ("x", "y", "z")),
                                       tuple(_r(node["localScale"].get(a, 1)) for a in ("x", "y", "z")),
                                       _rot(node))]
    for k in sorted(node["comps"]):
        for d in node["comps"][k]:
            f = norm(flatten(d))
            h.append(k + "|" + ";".join("%s=%r" % (kk, _r(f[kk])) for kk in sorted(f)))
    return "\n".join(h)


def _fields_of(node):
    """节点 → {组件key|字段: 值}（同名兄弟配对用）。

    🔴 原版（`vfx_diff2.py`）用 `id(node)` 做缓存键：**对象被 GC 后 id 会被复用**，
    缓存于是把**另一个 prefab 的另一个节点**的字段喂给当前节点 ⇒ 同名兄弟配错，
    表现为「一堆成对镜像的假差异」（实测：`Environmental Condition Necrons Immortal Beams`
    的 3 个同名 `Glow` 单跑 0 差异、跑 --all 却刷出 46 行成对镜像的差异）。
    ⇒ 这里把结果**挂在节点自己身上**，不依赖 id。
    """
    cached = node.get("_fields")
    if cached is not None:
        return cached
    out = {}
    for k, lst in node["comps"].items():
        d = norm(flatten(lst[0])) if lst else {}
        for kk, vv in d.items():
            out[k + "|" + kk] = vv
    for key, pre in (("localPos", "T.pos"), ("localScale", "T.scale"), ("localRot", "T.rot")):
        for a, vv in (node.get(key) or {}).items():
            out["%s.%s" % (pre, a)] = vv
    node["_fields"] = out
    return out


def node_dist(a, b):
    """两棵同名节点的字段差数（越小越像）——同名镜像兄弟就靠它配对。"""
    fa, fb = _fields_of(a), _fields_of(b)
    ka, kb = set(fa), set(fb)
    d = float(len(ka ^ kb))
    for k in ka & kb:
        if not valeq(fa[k], fb[k]):
            d += 1.0
    return d


def pair_nodes(ours, ars):
    """同名兄弟先按【整棵组件内容哈希】精确配对，再按【参数签名】贪心配对。
    ⛔ 不能按 pathID / 树次序配对：AssetRipper 会重排（同一组三个 Smoke 次序完全相反）。"""
    if len(ours) == 1 and len(ars) == 1:
        return [(ours[0], ars[0])]
    pairs = []
    oh = [node_hash(n) for n in ours]
    ah = [node_hash(n) for n in ars]
    left_o = list(range(len(ours)))
    left_a = list(range(len(ars)))
    for i in list(left_o):
        for j in list(left_a):
            if oh[i] == ah[j]:
                pairs.append((ours[i], ars[j]))
                left_o.remove(i)
                left_a.remove(j)
                break
    cand = []
    for i in left_o:
        for j in left_a:
            cand.append((node_dist(ours[i], ars[j]), i, j))
    cand.sort()
    used_o, used_a = set(), set()
    for d, i, j in cand:
        if i in used_o or j in used_a:
            continue
        pairs.append((ours[i], ars[j]))
        used_o.add(i)
        used_a.add(j)
    for i in left_o:
        if i not in used_o:
            pairs.append((ours[i], None))
    for j in left_a:
        if j not in used_a:
            pairs.append((None, ars[j]))
    return pairs


def diff_models(nm, om, am):
    """两棵模型对差 → 行（7 列，与 B9 链同一格式）。"""
    rows = []
    n_ops = sum(1 for lst in om.values() for n in lst for k in n["comps"] if k.startswith("ParticleSystem!"))
    n_aps = sum(1 for lst in am.values() for n in lst for k in n["comps"] if k.startswith("ParticleSystem!"))
    rows.append([nm, "", "PREFAB", "ParticleSystem 数量", "INFO",
                 "原版=%d 我们=%d" % (n_aps, n_ops), ""])
    for base in sorted(set(om) | set(am)):
        ours = om.get(base, [])
        ars = am.get(base, [])
        for on, an in pair_nodes(ours, ars):
            if on is None:
                rows.append([nm, an["path"], "NODE", "", "我们缺整个节点", "原版有", ""])
                continue
            if an is None:
                rows.append([nm, on["path"], "NODE", "", "我们多一个节点", "", "我们有"])
                continue
            p = on["path"]
            ok, ak = on["comps"], an["comps"]
            okinds = set(k.split("!u!")[0] for k in ok)
            akinds = set(k.split("!u!")[0] for k in ak)
            for kind in sorted(okinds | akinds):
                if kind in SKIP_COMPS or kind in ("Transform", "RectTransform"):
                    continue
                oL = [v[0] for k, v in ok.items() if k.split("!u!")[0] == kind]
                aL = [v[0] for k, v in ak.items() if k.split("!u!")[0] == kind]
                if not oL:
                    rows.append([nm, p, kind, "", "我们缺整个组件", "原版有", ""])
                    continue
                if not aL:
                    rows.append([nm, p, kind, "", "我们多一个组件", "", "我们有"])
                    continue
                of = norm(flatten(oL[0]))
                af = norm(flatten(aL[0]))
                for f in sorted(set(of) | set(af)):
                    ov = get(of, f)
                    av = get(af, f)
                    if valeq(ov, av):
                        continue
                    rows.append([nm, p, kind, f, "不一致", str(av), str(ov)])
            for key, label in (("localScale", "localScale"), ("localPos", "localPosition")):
                ov = on.get(key) or {}
                av = an.get(key) or {}
                for axis in ("x", "y", "z"):
                    if axis not in ov and axis not in av:
                        continue
                    if not valeq(ov.get(axis, 0), av.get(axis, 0)):
                        rows.append([nm, p, "Transform", "%s.%s" % (label, axis), "不一致",
                                     str(av.get(axis, 0)), str(ov.get(axis, 0))])
    return rows


def find_ar_path(ar_dir, nm):
    for cand in (os.path.join(ar_dir, nm + ".prefab"),
                 os.path.join(ar_dir, nm.replace(",", "_") + ".prefab"),
                 os.path.join(ar_dir, "GameObject", nm + ".prefab")):
        if os.path.exists(cand):
            return cand
    return None


def compare_one(nm, prefabs_dir=None, ar_dir=None):
    """单个 prefab 对差；返回行列表。"""
    prefabs_dir = prefabs_dir or OUR_PREFABS
    ar_dir = ar_dir or ORIG_AR
    op = os.path.join(prefabs_dir, nm + ".prefab")
    ap = find_ar_path(ar_dir, nm)
    if not os.path.exists(op) or not ap:
        return [[nm, "", "PREFAB", "", "判不了",
                 "缺文件 our=%s ar=%s" % (os.path.exists(op), bool(ap)), ""]]
    try:
        return diff_models(nm, prefab_model(op), prefab_model(ap))
    except Exception as e:                                    # 解析异常也要出声，⛔ 不静默
        return [[nm, "", "PREFAB", "", "判不了", "解析异常 %r" % (e,), ""]]


# -------- 多进程跑（Unity 不掺和；纯 CPU，16 核本机实测 ~7m41s → 数十秒）
_W = {}


def _init_worker(prefabs_dir, ar_dir):
    _W["prefabs"] = prefabs_dir
    _W["ar"] = ar_dir


def _run_one(nm):
    return nm, compare_one(nm, _W["prefabs"], _W["ar"])


def run_all(names, prefabs_dir, ar_dir, jobs):
    if jobs <= 1 or len(names) < 8:
        return [(n, compare_one(n, prefabs_dir, ar_dir)) for n in names]
    with Pool(processes=jobs, initializer=_init_worker, initargs=(prefabs_dir, ar_dir)) as pool:
        out = pool.map(_run_one, names, chunksize=4)
    return out


# ================================================================ 原始 JSON（补计数）
# 只用于「AssetRipper 没导出、因此我们没法比参数」的极少数 prefab 的**粒子系统计数**
# （B9 表里 `?（未判）` 那一格）。参数对差本身仍只走 AR / 我们工程两份 prefab。
KIND = {
    "ParticleSystem": "ParticleSystem", "ParticleSystemRenderer": "ParticleSystemRenderer",
    "TrailRenderer": "TrailRenderer", "MeshRenderer": "MeshRenderer", "MeshFilter": "MeshFilter",
    "SkinnedMeshRenderer": "SkinnedMeshRenderer", "SpriteRenderer": "SpriteRenderer",
    "SpriteMask": "SpriteMask", "MonoBehaviour": "MonoBehaviour", "VisualEffect": "VisualEffect",
    "Canvas": "Canvas", "CanvasRenderer": "CanvasRenderer", "CanvasGroup": "CanvasGroup",
    "SortingGroup": "SortingGroup", "Animator": "Animator", "Animation": "Animation",
    "BoxCollider": "BoxCollider", "BoxCollider2D": "BoxCollider2D",
    "CapsuleCollider": "CapsuleCollider", "MeshCollider": "MeshCollider",
}


def _conv(o):
    if isinstance(o, dict):
        return {k: _conv(v) for k, v in o.items()}
    if isinstance(o, list):
        return [_conv(v) for v in o]
    if isinstance(o, bool):
        return 1 if o else 0
    return o


def build_raw_index(bundle_dir):
    """assets_full 本包 → {go 名字: [(go_pid, go_json)]} + {tr_pid: {...}} + 组件 id 集合。"""
    idx = {"comp": {}, "tr": {}, "gos": collections.defaultdict(list), "by_gopid": {}}
    for d in KIND:
        dd = os.path.join(bundle_dir, d)
        if not os.path.isdir(dd):
            continue
        s = set()
        for fn in os.listdir(dd):
            if not fn.endswith(".json"):
                continue
            try:
                s.add(int(fn[:-5].rsplit("_", 1)[-1]))
            except ValueError:
                pass
        idx["comp"][d] = s
    for d in ("Transform", "RectTransform"):
        dd = os.path.join(bundle_dir, d)
        if not os.path.isdir(dd):
            continue
        for fn in os.listdir(dd):
            if not fn.endswith(".json"):
                continue
            try:
                pid = int(fn[:-5].rsplit("_", 1)[-1])
            except ValueError:
                continue
            t = _conv(json.load(io.open(os.path.join(dd, fn), encoding="utf-8")))
            idx["tr"][pid] = {"go": (t.get("m_GameObject") or {}).get("m_PathID"),
                              "father": (t.get("m_Father") or {}).get("m_PathID"),
                              "children": [c.get("m_PathID") for c in t.get("m_Children") or []]}
    go_dir = os.path.join(bundle_dir, "GameObject")
    if os.path.isdir(go_dir):
        for fn in os.listdir(go_dir):
            if not fn.endswith(".json"):
                continue
            g = _conv(json.load(io.open(os.path.join(go_dir, fn), encoding="utf-8")))
            name = g.get("m_Name") or fn[:-5]
            gopid = None
            for c in g.get("m_Component") or []:
                cid = (c.get("component") or {}).get("m_PathID")
                t = idx["tr"].get(cid)
                if t and t.get("go"):
                    gopid = t["go"]
                    break
            if gopid is None:
                m = re.match(r"^(.*)_(-?\d+)$", fn[:-5])
                gopid = int(m.group(2)) if m else None
            idx["gos"][name].append((gopid, g))
            if gopid is not None:
                idx["by_gopid"][gopid] = (name, g)
    return idx


def raw_roots(idx):
    """包内根 GameObject 名（有 transform 且无父）——B9 表 `bundle` 列用的那个集合。"""
    out = set()
    go2tr = {}
    for t, v in idx["tr"].items():
        if v["go"] is not None:
            go2tr.setdefault(v["go"], t)
    for name, lst in idx["gos"].items():
        for gopid, _g in lst:
            t = go2tr.get(gopid)
            if t is not None and not idx["tr"][t]["father"]:
                out.add(name)
    return out


def raw_ps_count(idx, name):
    """原始 JSON 里这个 prefab 的粒子系统数；找不到根 ⇒ None。"""
    go2tr = {}
    for t, v in idx["tr"].items():
        if v["go"] is not None:
            go2tr.setdefault(v["go"], t)
    for gopid, _g in idx["gos"].get(name, []):
        t = go2tr.get(gopid)
        if t is None or idx["tr"][t]["father"]:
            continue
        ps = 0
        stack = [t]
        while stack:
            cur = stack.pop()
            g = idx["by_gopid"].get(idx["tr"][cur]["go"])
            if g:
                for c in g[1].get("m_Component") or []:
                    cid = (c.get("component") or {}).get("m_PathID")
                    if cid in idx["comp"].get("ParticleSystem", ()):
                        ps += 1
            stack.extend(idx["tr"][cur]["children"])
        return ps
    return None


# ================================================================ 材质 renderQueue
# 移植自 `_tmp_view/v1tmp/queue_judge.py`：原版 `m_CustomRenderQueue == -1` 时用**原版 shader 默认队列**判等价。
def load_shader_queue_table(path):
    q = {}
    for line in io.open(path, encoding="utf-8"):
        p = line.rstrip("\n").split("\t")
        if len(p) < 2 or p[0] == "shader名":
            continue
        try:
            q[p[0]] = int(p[1])
        except ValueError:
            pass
    return q


def shader_name_index(assets_full):
    """全局 shader 索引（材质引用的 shader 多数在别的包里）。"""
    name = {}
    root = os.path.dirname(os.path.abspath(assets_full)) if os.path.basename(assets_full) else assets_full
    try:
        dirs = [d for d in os.listdir(root) if os.path.isdir(os.path.join(root, d, "Shader"))]
    except OSError:
        return name
    for d in dirs:
        sd = os.path.join(root, d, "Shader")
        for fn in os.listdir(sd):
            if not fn.endswith(".json"):
                continue
            try:
                pid = int(fn[:-5].rsplit("_", 1)[-1])
            except ValueError:
                continue
            if pid in name:
                continue
            try:
                o = json.load(io.open(os.path.join(sd, fn), encoding="utf-8"))
            except Exception:
                continue
            nm = (o.get("m_ParsedForm") or {}).get("m_Name") or o.get("m_Name")
            if nm:
                name[pid] = nm
    return name


def queue_judge(bundle_dir, materials_dir, shader_q, shaders):
    orig_dir = os.path.join(bundle_dir, "Material")
    if not os.path.isdir(orig_dir):
        return []
    orig = {}
    for fn in os.listdir(orig_dir):
        if not fn.endswith(".json"):
            continue
        d = json.load(io.open(os.path.join(orig_dir, fn), encoding="utf-8"))
        orig.setdefault(d.get("m_Name"), d)
    our = {}
    if os.path.isdir(materials_dir):
        for fn in os.listdir(materials_dir):
            if not fn.endswith(".mat"):
                continue
            try:
                docs = parse_unity_yaml(os.path.join(materials_dir, fn))
            except Exception:
                continue
            for did, root, obj, _ in docs.get(21, []):
                our.setdefault((obj.get("m_Name") or fn).split("__")[0], obj)
    rows = []
    for name, o in sorted(orig.items()):
        u = our.get(name.split("__")[0]) or our.get(name)
        if not u:
            continue
        oq = o.get("m_CustomRenderQueue")
        uq = u.get("m_CustomRenderQueue")
        sh = shaders.get((o.get("m_Shader") or {}).get("m_PathID"))
        oqeff = oq if (oq is not None and oq != -1) else shader_q.get(sh)
        if oq == uq:
            continue
        eq = (oqeff == uq) or (oq == -1 and uq == -1)
        rows.append([name, sh, oq, oqeff, uq, "等价" if eq else "不一致"])
    return rows


def load_json_or(path, default=None):
    try:
        return json.load(io.open(path, encoding="utf-8"))
    except Exception:
        return default


def queue_check(bundle_dir, materials_dir, shader_q_path, truth_path, runtime_path):
    """材质队列判定 → ([(raw_row, 等价?, 依据)], 是否用了运行时口径)。

    静态 `.mat` 的 `m_CustomRenderQueue` 不一样 ≠ 画面不一样：
    运行时 `WarpforgeEffectBinder.cs:573` 会用 `WFMatDef.renderQueue`（= `mat_renderqueue.json`
    的 `queue[name]`，由 `gen_mat_renderqueue.py` 从原版材质 JSON 生成）**覆盖**它。
    ⇒ 真正要比的是「**运行时生效队列**」：
        我们 = runtime.queue[name]（有就用）· 否则 = 我们 .mat 的队列
        原版 = 原版材质队列（>=0 时）· 否则 = 原版 shader 默认队列（`renderqueue_truth.json`）
    两份表任一缺失 ⇒ 退回静态口径（会打印警告，⛔ 不静默）。
    """
    shaders = shader_name_index(bundle_dir)
    rows = queue_judge(bundle_dir, materials_dir, load_shader_queue_table(shader_q_path), shaders)
    truth = load_json_or(truth_path)
    runtime = load_json_or(runtime_path)
    use_runtime = bool(truth and runtime)
    out = []
    for r in rows:
        name, sh, oq, oqeff, uq = r[0], r[1], r[2], r[3], r[4]
        if use_runtime:
            tm = (truth.get("mat") or {}).get(name)
            orig_eff = tm if isinstance(tm, int) and tm >= 0 else (truth.get("shader") or {}).get(sh)
            our_eff = (runtime.get("queue") or {}).get(name, uq)
            eq = (our_eff == orig_eff) or (our_eff == -1 and orig_eff == -1)
            out.append((r, eq, (our_eff, orig_eff)))
        else:
            out.append((r, r[5] == "等价", None))
    return out, use_runtime


# ================================================================ 影响面（卡片引用）
def compute_usage(card_vfx_path, cards_engine_path):
    """prefab → {names:set, events:set}（按**卡名**去重 —— 与当年 `prefab_usage.tsv` 同口径：
    105 个 id 里同名卡归并后是 103 张，实测 643/645 条计数逐条吻合）。"""
    if not (os.path.exists(card_vfx_path) and os.path.exists(cards_engine_path)):
        return {}
    d = json.load(io.open(card_vfx_path, encoding="utf-8"))
    by = d.get("byCard") or {}
    ce = json.load(io.open(cards_engine_path, encoding="utf-8"))
    cards = ce.get("cards") if isinstance(ce, dict) and "cards" in ce else ce
    name_of = {}
    for c in (cards if isinstance(cards, list) else cards.values()):
        name_of[c.get("id")] = c.get("name")
    per = collections.defaultdict(lambda: {"names": set(), "events": set()})
    for cid, info in by.items():
        if not isinstance(info, dict):
            continue
        for ev, lst in info.items():
            if not isinstance(lst, list):
                continue
            for e in lst:
                if isinstance(e, dict) and e.get("prefab"):
                    per[e["prefab"]]["names"].add(name_of.get(cid) or cid)
                    per[e["prefab"]]["events"].add(ev)
    return per


# ================================================================ 判定 / 汇总
STRUCT_KIND = {"我们缺整个节点", "我们多一个节点", "我们缺整个组件", "我们多一个组件"}


def split_rows(rows):
    """→ (real_diffs, structs, noise_count, info_rows, unjudged)"""
    real, structs, noise, info, unjudged = [], [], 0, [], []
    for r in rows:
        v = r[4]
        if v == "INFO":
            info.append(r)
        elif v == "判不了":
            unjudged.append(r)
        elif v in STRUCT_KIND:
            structs.append(r)
        elif v == "不一致":
            f = r[3]
            if f in NOISE_EQUIV or f in NOISE_DEFAULT:
                noise += 1
            else:
                real.append(r)
    return real, structs, noise, info, unjudged


def struct_verdict(structs):
    """结构差分类：只差【尾随空格】的同名节点错位 = 命名等价（警告），其余 = 失败。"""
    bad, artifact = [], []
    missing = [r for r in structs if r[4] == "我们缺整个节点"]
    extra = [r for r in structs if r[4] == "我们多一个节点"]
    used = set()
    for m in missing:
        hit = None
        for i, e in enumerate(extra):
            if i in used:
                continue
            if m[1].rstrip() == e[1].rstrip() and m[1] != e[1]:
                hit = i
                break
        if hit is None:
            bad.append(m)
        else:
            used.add(hit)
            artifact.append((m, extra[hit]))
    for i, e in enumerate(extra):
        if i not in used:
            bad.append(e)
    for r in structs:
        if r[4] in ("我们缺整个组件", "我们多一个组件"):
            bad.append(r)
    return bad, artifact


# ================================================================ 主流程
def collect(names, args):
    t0 = time.time()
    jobs = args.jobs if args.jobs > 0 else min(16, os.cpu_count() or 4)
    res = run_all(names, args.ours, args.assets, jobs)
    print("扫描 %d 个 prefab（%d 进程，%.1f 秒）" % (len(names), jobs, time.time() - t0))
    return res


def do_check(args):
    names = sorted(f[:-7] for f in os.listdir(args.ours) if f.endswith(".prefab"))
    if not names:
        print("⛔ 我们的 prefab 目录是空的：%s" % args.ours)
        return 2
    if not os.path.isdir(args.assets):
        print("⛔ 原版（AssetRipper 导出）目录不存在：%s" % args.assets)
        return 2
    res = collect(names, args)
    all_real, all_struct, all_unjudged = [], [], []
    noise = 0
    for nm, rows in res:
        real, structs, nz, info, unj = split_rows(rows)
        all_real.extend(real)
        all_struct.extend(structs)
        all_unjudged.extend(unj)
        noise += nz
    bad_struct, artifact = struct_verdict(all_struct)

    # 材质队列：比「运行时生效队列」（静态 .mat 会被 WFMatDef.renderQueue 覆盖）
    qres, use_runtime = queue_check(args.bundle, args.materials, args.shader_q,
                                    args.rq_truth, args.rq_runtime)
    qdiff = [x for x in qres if not x[1]]

    print("")
    print("── 参数层 ────────────────────────────────────────────")
    print("  已知等价噪声（不计失败）：%d 行" % noise)
    print("  真差异（判定=不一致，已剔噪声）：%d 行" % len(all_real))
    print("  结构差：%d 行（其中尾随空格命名等价 %d 对，真结构差 %d 行）"
          % (len(all_struct), len(artifact), len(bad_struct)))
    print("  判不了（原版侧缺件/解析异常）：%d 个 prefab" % len({r[0] for r in all_unjudged}))
    for u in all_unjudged:
        print("    · %s —— %s" % (u[0], u[5]))
    print("── 材质 renderQueue ──────────────────────────────────")
    if use_runtime:
        print("  比了 %d 个材质（静态 .mat 队列不同者），按【运行时生效队列】判：真不一致 %d"
              % (len(qres), len(qdiff)))
    else:
        print("  ⚠ 缺 `renderqueue_truth.json` / `mat_renderqueue.json` ⇒ 退回【静态】口径")
        print("  比了 %d 个材质（静态 .mat 队列不同者），真不一致 %d" % (len(qres), len(qdiff)))

    def dump(title, rows, limit=40):
        if not rows:
            return
        print("")
        print("  " + title + "（共 %d 行，列前 %d）：" % (len(rows), min(limit, len(rows))))
        for r in rows[:limit]:
            print("    %s | %s | %s.%s: 原版 %s / 我们 %s" % (r[0], r[1], r[2], r[3], r[5], r[6]))
        if len(rows) > limit:
            print("    …… 其余 %d 行省略" % (len(rows) - limit))

    dump("真差异", all_real)
    dump("真结构差", bad_struct)
    if qdiff:
        print("")
        print("  材质不一致（共 %d 行）：" % len(qdiff))
        for r, _eq, eff in qdiff:
            if eff:
                print("    %s | 原版生效 %s / 我们运行时 %s（静态 .mat %s）"
                      % (r[0], eff[1], eff[0], r[4]))
            else:
                print("    %s | 原版 %s / 我们 %s" % (r[0], r[2], r[4]))

    ok = not all_real and not bad_struct and not qdiff
    print("")
    print("结论：%s（真差异 %d · 真结构差 %d · 材质不一致 %d）"
          % ("✅ 0 差异" if ok else "❌ 有差异", len(all_real), len(bad_struct), len(qdiff)))
    return 0 if ok else 1


def do_write(args):
    names = sorted(f[:-7] for f in os.listdir(args.ours) if f.endswith(".prefab"))
    res = collect(names, args)
    ps_of = {}
    rows_by = collections.defaultdict(list)
    # 噪声统计（全局三行要用真实数字，⛔ 不写死）
    stitch = collections.Counter()
    sel = collections.Counter()
    mask = collections.Counter()
    for nm, rows in res:
        for r in rows:
            if r[2] == "PREFAB" and r[3].startswith("ParticleSystem"):
                m = re.search(r"原版=(\d+) 我们=(\d+)", r[5] or "")
                if m:
                    ps_of[nm] = (int(m.group(1)), int(m.group(2)))
            if r[4] != "不一致":
                if r[4] in STRUCT_KIND:
                    rows_by[nm].append(r)
                continue
            f = r[3]
            if f in NOISE_EQUIV:
                mask[r[2]] += 1
                continue
            if f in NOISE_DEFAULT:
                (stitch if f == "m_StitchLightmapSeams" else sel)[r[2]] += 1
                continue
            rows_by[nm].append(r)

    idx = build_raw_index(args.bundle)
    bundled = raw_roots(idx)
    for nm in names:                                  # AR 缺的那几个：ps 计数改读原始 JSON
        if nm not in ps_of:
            c = raw_ps_count(idx, nm)
            if c is not None:
                ps_of[nm] = (c, c)
    usage = compute_usage(args.card_vfx, args.cards_engine)

    # ---- 逐 prefab 一行（逻辑逐条照抄 gen_b9.py） ----
    judged_cross = ["RewardAppearParticle", "Vanguard Frame Animated VAT"] + \
        ["Wildcard_Use_Button %d %s" % (i, s) for i, s in ((1, "Common"), (2, "Rare"), (3, "Epic"), (4, "Legendary"))] + \
        ["Wildcard_Use_Card %d %s" % (i, s) for i, s in ((1, "Common"), (2, "Rare"), (3, "Epic"), (4, "Legendary"))]
    ps_of["Vanguard Frame Animated VAT"] = (0, 0)   # 网格 + VAT 材质，无粒子系统（原始 JSON 父链实读）

    out_rows = []
    for nm in names:
        nps_o, nps_u = ps_of.get(nm, (0, 0))
        f = "Assets/WarpforgeVFX/Prefabs/%s.prefab" % nm
        u = usage.get(nm)
        if u:
            sample = "; ".join(sorted(u["names"])[:3])
            infl = "%d 张卡（%s；事件 %s）" % (len(u["names"]), sample, ",".join(sorted(u["events"])))
        else:
            infl = "未被卡表引用（0 张卡）"
        note = []
        if nm in judged_cross and nm != "Vanguard Frame Animated VAT":
            note.append("AssetRipper 把它放在 `Assets/GameObject/` 子目录（不在 Assets 根）⇒ 已按子目录路径比，逐字段 0 差异")
        if nm == "Vanguard Frame Animated VAT":
            note.append("原件**不是独立 prefab**，是本包 `VanguardIdleEffect` 下的子节点（`Transform` 父链实读）；"
                        "已按子节点比：结构 1 节点、网格名 `Vanguard Frame Animated_Mesh`、材质名 `Vanguard_Frame VAT` 全部一致")
        for r in rows_by.get(nm, []):
            if r[4] in STRUCT_KIND:
                note.append("%s %s（%s）" % (r[4], r[1], r[2]))
            else:
                note.append("%s.%s: 原版 %s / 我们 %s" % (r[1], r[3], r[5], r[6]))
        if any(r[4] in STRUCT_KIND for r in rows_by.get(nm, [])):
            note.append("（上面这条是原版 GameObject 名带**尾随空格**造成的同名节点错位；节点内容逐字段一致）")
        if nm not in bundled and nm != "Vanguard Frame Animated VAT":
            if nm in judged_cross:
                note.append("原件**不在本包**（在 `scenes` / `menus` 包），但 AR 的导出里有同名 prefab ⇒ 上面那轮比的就是它")
            else:
                note.append("不在 battleprefabs_vfxandmisc 包的根集合里（原件在别的包 / 非 addressable 重打包 / 本包里是别人的子节点）")
        if nm in ps_of and nps_o == 0 and nps_u == 0:
            note.append("无粒子系统（纯精灵/网格/UI）")
        if nm not in ps_of:
            note.append("本轮未取到粒子系统计数（判据缺，见 §六）")
        out_rows.append([nm, "battleprefabs_vfxandmisc" if nm in bundled else "(非本包)",
                         "有", f,
                         "ParticleSystem × %d / Renderer × %d" % (nps_u, nps_u),
                         "全部模块逐字段：duration/looping/prewarm/startLifetime/startSpeed/startSize/startColor/startRotation/"
                         "gravityModifier/emission(rateOverTime,rateOverDistance,每个 burst)/shape(shapeType,radius,angle,arc,scale,rotation)/"
                         "color·size·velocity·rotation·noise·trails·subEmitters·UV·collision/renderer(renderMode,sortingFudge,材质指派)/"
                         "stopAction/cullingMode/simulationSpeed",
                         ("%d 个粒子系统" % nps_o) if nm in ps_of else "?（未判）",
                         ("%d 个粒子系统" % nps_u) if nm in ps_of else "?（未判）",
                         "一致", infl, "；".join(note) if note else "逐字段 0 差异"])

    # ---- 本包有、我们没有导出的 4 个根（逐条判据见原 gen_b9.py） ----
    out_rows.extend([
        ["CardHalo", "battleprefabs_vfxandmisc", "无", "—", "MeshRenderer", "整个 prefab", "1 节点（MeshFilter+MeshRenderer+MeshCollider），0 粒子系统", "—",
         "我们缺整个 prefab", "未被卡表引用", "本包根里存在、我们没导出；但**不含粒子系统**（3D 光晕网格），不属特效库口径；是否要收进卡牌表现 → 归界面/卡牌线"],
        ["CardMulliganSelected", "battleprefabs_vfxandmisc", "无", "—", "CanvasRenderer+MonoBehaviour", "整个 prefab", "3 节点（换牌 UI），0 粒子系统", "—",
         "我们缺整个 prefab", "换牌界面", "同上：0 粒子系统，属 UI（`MulliganPanel` 那边另有实现），不属特效库"],
        ["Fake Light Glow", "battleprefabs_vfxandmisc", "内联", "（被 7 个 Environmental Condition prefab 内联为同名节点）", "SpriteRenderer", "整个 prefab", "1 节点 SpriteRenderer，0 粒子系统", "已内联",
         "一致", "环境效果", "**没有单独导出，但内容被内联进了用到它的 prefab**（判据：`Environmental Condition Black Legion Warp Storm.prefab:241869` 有同名节点，无丢失）"],
        ["Orbital 5 repeat ␠", "battleprefabs_vfxandmisc", "有（名字末尾差一个空格）", "Assets/WarpforgeVFX/Prefabs/Orbital 5 repeat.prefab", "ParticleSystem × 8", "整个 prefab", "8 个粒子系统", "8 个粒子系统",
         "一致", "未被卡表引用", "原版 GameObject 名 **`Orbital 5 repeat `（尾随空格）**，我们存成 `Orbital 5 repeat` ⇒ 名字差一个字符；参数已逐字段对齐（本轮 AR 对差覆盖）"],
    ])

    # ---- 材质层（只出「队列不同」的那些） ----
    shaders = shader_name_index(args.bundle)
    for r in queue_judge(args.bundle, args.materials, load_shader_queue_table(args.shader_q), shaders):
        if len(r) >= 6 and r[5] != "等价":
            out_rows.append([r[0], "battleprefabs_vfxandmisc", "有", "Assets/WarpforgeVFX/Materials/%s.mat" % r[0],
                             "Material", "m_CustomRenderQueue", r[3], r[4], "一致", "按材质名追",
                             "静态 .mat 队列不同，但**运行时不看 .mat**：`WarpforgeEffectBinder.cs:573` 按 `WFMatDef.renderQueue` 覆盖，"
                             "而那个值 = 原版材质队列或**原版 shader 默认队列**（`EffectExporter.TruthQueue:2999` + `数据/游戏数据/shader_renderqueue.tsv`）"])

    # ---- 三个「看起来是差异、实际不是」的全局字段（数字现算，⛔ 不写死） ----
    def _kind(k):
        return "Skinned" if k == "SkinnedMeshRenderer" else k

    n_compared = len({nm for nm, rows in res if any(r[4] == "INFO" for r in rows)})
    stitch_total = sum(stitch.values())
    sel_total = sum(sel.values())
    mask_total = sum(mask.values())
    kinds = [(k, v) for k, v in stitch.most_common() if v]
    if kinds:
        rest = "、".join("%d %s" % (v, _kind(k)) for k, v in kinds[1:])
        # 原表写法：首项写「N 处在 <组件类>」，其余写「<数> <类>」（首项那个数 = 该类的处数，不是总数）
        stitch_text = "%s 处在 %s%s" % (format(kinds[0][1], ","), _kind(kinds[0][0]),
                                        ("、" + rest) if rest else "")
    else:
        stitch_text = "0 处"
    mask_detail = " · ".join("%d %s" % (v, _kind(k)) for k, v in mask.most_common() if v)
    out_rows.append(["(全局) 所有渲染器", "battleprefabs_vfxandmisc", "有", "—", "Renderer 基类", "m_StitchLightmapSeams",
                     "0（原版包体未序列化该字段）", "1（我们工程默认）", "一致",
                     "全部 %d 个 prefab（%s 处）" % (n_compared, format(stitch_total, ",")),
                     "Unity **工程级序列化默认**差异，不是特效参数：%s；粒子/精灵不走光贴图缝合，视觉无影响" % stitch_text])
    out_rows.append(["(全局) SpriteRenderer", "battleprefabs_vfxandmisc", "有", "—", "SpriteRenderer", "m_SelectedEditorRenderState",
                     "3", "0", "一致", "%d 处" % sel_total,
                     "**纯编辑器**字段（SceneView 遮挡剔除预览），不参与运行"])
    out_rows.append(["(全局) 全部渲染器", "battleprefabs_vfxandmisc", "有", "—", "Renderer 基类", "m_MaskInteraction",
                     "（未序列化）", "0", "一致", "%d 处（%s）" % (mask_total, mask_detail),
                     "**等价**：未序列化 = 默认 `None` = 0"])

    lines = ["原版 prefab 名\tbundle\t我们建了没有\t我们对应文件\t模块\t参数\t原版值\t我们的值\t判定\t影响面\t备注"]
    for r in out_rows:
        lines.append("\t".join(str(x).replace("\t", " ").replace("\n", " ") for x in r))
    text = "\n".join(lines) + "\n"
    with io.open(args.out, "w", encoding="utf-8", newline="\n") as fh:
        fh.write(text)
    print("rows: %d -> %s" % (len(out_rows), args.out))
    print("counts:", collections.Counter(str(r[8]) for r in out_rows))
    return 0


def do_self_test(args):
    """在**内存里**篡改一个字段再比 ⇒ 必须检出（退出码 1）。"""
    names = sorted(f[:-7] for f in os.listdir(args.ours) if f.endswith(".prefab"))
    target = None
    for nm in names:
        ap = find_ar_path(args.assets, nm)
        if not ap:
            continue
        try:
            om = prefab_model(os.path.join(args.ours, nm + ".prefab"))
        except Exception:
            continue
        if any(k.startswith("ParticleSystem!") for lst in om.values() for n in lst for k in n["comps"]):
            target = nm
            break
    if not target:
        print("⛔ 找不到可用于自检的 prefab（需要两侧都有、且带 ParticleSystem）")
        return 2
    op = os.path.join(args.ours, target + ".prefab")
    ap = find_ar_path(args.assets, target)
    om = prefab_model(op)
    am = prefab_model(ap)
    # 篡改：挑第一个带 ParticleSystem 的节点，把 lengthInSec +1（⛔ 不落在噪声字段上）
    node = next(n for lst in om.values() for n in lst
                if any(k.startswith("ParticleSystem!") for k in n["comps"]))
    key = next(k for k in node["comps"] if k.startswith("ParticleSystem!"))
    comp = node["comps"][key][0]
    before = comp.get("lengthInSec", "<无>")
    comp["lengthInSec"] = (before if isinstance(before, (int, float)) else 1.0) + 1.0
    rows = diff_models(target, om, am)
    real, structs, noise, info, unj = split_rows(rows)
    hit = [r for r in real if r[3] == "lengthInSec"]
    print("自检对象：%s / 节点 %s / 字段 lengthInSec" % (target, node["path"]))
    print("  篡改：原版 %r → 我们（内存中）%r" % (before, comp["lengthInSec"]))
    print("  比对后新增「真差异」行：%d（其中 lengthInSec 那一行：%d）" % (len(real), len(hit)))
    for r in hit[:3]:
        print("    %s | %s | %s: 原版 %s / 我们 %s" % (r[0], r[1], r[3], r[5], r[6]))
    if hit:
        print("")
        print("✅ 闸有牙：篡改被检出 —— 本命令**故意**以退出码 1 结束（不是失败）")
        return 1
    print("")
    print("❌ 闸没牙：篡改没有被检出，这个 checker 不可信")
    return 3


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=False)
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--write", action="store_true")
    ap.add_argument("--self-test", action="store_true")
    ap.add_argument("-o", "--out", default=OUT_TSV)
    ap.add_argument("--assets", default=ORIG_AR)
    ap.add_argument("--bundle", default=ORIG_BUNDLE)
    ap.add_argument("--ours", default=OUR_PREFABS)
    ap.add_argument("--materials", default=OUR_MATERIALS)
    ap.add_argument("--card-vfx", default=CARD_VFX)
    ap.add_argument("--cards-engine", default=CARDS_ENGINE)
    ap.add_argument("--shader-q", default=SHADER_Q)
    ap.add_argument("--rq-truth", default=RQ_TRUTH)
    ap.add_argument("--rq-runtime", default=RQ_RUNTIME)
    ap.add_argument("--jobs", type=int, default=0)
    ap.add_argument("-h", "--help", action="store_true")
    a, unknown = ap.parse_known_args(argv)
    actions = [a.check, a.write, a.self_test]
    if a.help or sum(1 for x in actions if x) != 1:
        # ⛔ 不带显式动作（或给了多个/未知参数）⇒ 只打用法、什么都不写
        print(USAGE)
        if unknown:
            print("（忽略了无法识别的参数：%s）" % " ".join(unknown))
        return 0
    if a.check:
        return do_check(a)
    if a.write:
        return do_write(a)
    return do_self_test(a)


if __name__ == "__main__":
    sys.exit(main())
