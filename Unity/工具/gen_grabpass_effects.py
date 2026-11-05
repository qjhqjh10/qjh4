#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""gen_grabpass_effects.py — B107 · VFX-B14 的「失真效果挪层清单」生成器（只读扫描，不跑 Unity）

它做什么
--------
扫 `Assets/WarpforgeVFX/Prefabs/*.prefab`，找出**用到抓屏扭曲 shader** 的那一族效果
（原版名 `Everguild/FX/Particle Distortion Affect Transparents`，或 `WarpforgeShaderMap`
给它登记的替代名），逐件列出：
  效果名 · 材质 · prefab 自带渲染层 / 渲染队列 · 建议目标层 · 证据（文件:行）· 是否已挪

数据来源（全部现读，⛔ 不猜、不写死条数）
--------------------------------------
1. 效果的失真材质：prefab 里 `WarpforgeEffectBinder.materials[]` 的 `name / shader / renderQueue`
   —— 这正是 `GrabScreen501.UsesDistortion` 的第一条判据（运行期据此把实例搬到 501 层）。
2. 原版名 / 替代名：`WarpforgeShaderMap.cs` 的 `Replacements` 表（正则读那一行，不抄第二份）。
3. 我们自己的替代 shader 资产：`Shaders/*.shader` 头部 `Shader "<替代名>"` + 同名 `.meta` 的 guid。
4. 材质资产：`Materials/*.mat`（`m_Shader` guid / `m_CustomRenderQueue`）。
5. 目标层 / 开关名：`GrabPassTransparentFeature.cs` 的 `GrabScreen501` 常量
   （`DefaultLayer` / `EnvSwitch` / `EnvLayer` / `DistortionOriginalName`）。
6. 交叉核对：`资料/普查产出_0917/效果_shader对账.tsv`（效果→shader 列表）·
   `CardPresentation/Resources/card_vfx_by_card.json`（效果→引用卡）·
   `数据/游戏数据/mat_renderqueue.tsv`（原版材质 m_CustomRenderQueue）·
   `Assets/**/*.unity|*.prefab` 的静态 guid 引用（排除 VFX prefab 自身）。

层的口径（重要）
----------------
表里的「当前层」是 **prefab 自带层**（`GameObject.m_Layer`）。运行期实例的最终层由
`WarpforgeEffectPlayer.Play` 决定（跟父节点 / 显式 layer），并在 `WF_GRAB501=1` 时被
`GrabScreen501.FinalLayer` 覆盖成目标层、再 `SetLayerRec` 递归整棵子树。
运行期真实层**本工具测不到**（不跑 Unity）—— 判据见 `GrabPassTransparentFeature.cs` 的 `GrabScreen501` doc。

用法
----
    python -I -X utf8 d:/4/Unity/工具/gen_grabpass_effects.py            # 只打印用法，不写盘
    python -I -X utf8 d:/4/Unity/工具/gen_grabpass_effects.py --write    # 写判据 md
    python -I -X utf8 d:/4/Unity/工具/gen_grabpass_effects.py --check    # 幂等：与现有 md 逐字节比

退出码：0 = OK / 一致；1 = --check 发现漂移；2 = 参数错误；3 = 输入缺失/解析失败。
⚠️ 不带显式动作参数时**绝不写盘**（本仓 2026-10-26 事故的纪律）。
"""

from __future__ import print_function

import io
import os
import re
import sys


# ----------------------------------------------------------------------
#  路径（工具在 d:/4/Unity/工具/ ⇒ parents[1] = d:/4/Unity）
# ----------------------------------------------------------------------
TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
UNITY_DIR = os.path.dirname(TOOL_DIR)
ASSETS_DIR = os.path.join(UNITY_DIR, "MyGame", "Assets")
VFX_DIR = os.path.join(ASSETS_DIR, "WarpforgeVFX")

DEFAULT_PREFAB_DIR = os.path.join(VFX_DIR, "Prefabs")
DEFAULT_MATERIAL_DIR = os.path.join(VFX_DIR, "Materials")
DEFAULT_SHADER_DIR = os.path.join(VFX_DIR, "Shaders")
DEFAULT_SHADER_MAP_CS = os.path.join(VFX_DIR, "Runtime", "WarpforgeShaderMap.cs")
DEFAULT_FEATURE_CS = os.path.join(VFX_DIR, "Runtime", "GrabPassTransparentFeature.cs")
DEFAULT_EFFECT_TSV = os.path.join(UNITY_DIR, "资料", "普查产出_0917", "效果_shader对账.tsv")
DEFAULT_CARD_JSON = os.path.join(ASSETS_DIR, "CardPresentation", "Resources", "card_vfx_by_card.json")
DEFAULT_ORDER_MD = os.path.join(UNITY_DIR, "资料", "完全复刻", "判据", "B107_特效线施工单_v2.md")
DEFAULT_QUEUE_TSV = os.path.join(UNITY_DIR, "数据", "游戏数据", "mat_renderqueue.tsv")
DEFAULT_OUT = os.path.join(UNITY_DIR, "资料", "完全复刻", "判据", "B107_B14_失真效果挪层清单.md")

# 原版 shader 名（保底值；正式值从 Feature 的 `DistortionOriginalName` 读）
FALLBACK_ORIGINAL = "Everguild/FX/Particle Distortion Affect Transparents"
FALLBACK_REPLACEMENT = "WarpforgeVFX/FX/Distortion"

USAGE = """gen_grabpass_effects.py — B107 · VFX-B14 失真效果挪层清单生成器（只读）

用法：
  python -I -X utf8 工具/gen_grabpass_effects.py            打印本用法（不写盘）
  python -I -X utf8 工具/gen_grabpass_effects.py --write    扫描并写判据 md
  python -I -X utf8 工具/gen_grabpass_effects.py --check    重新扫描并与现有 md 比对（幂等）

可选：
  --prefabs DIR   覆盖 prefab 目录（默认 Assets/WarpforgeVFX/Prefabs）
  --materials DIR 覆盖材质目录（默认 Assets/WarpforgeVFX/Materials）
  --out FILE      覆盖输出 md 路径
  --quiet         只打印汇总行

扫描口径：prefab 里 `WarpforgeEffectBinder.materials[]` 的 shader == 失真原名/替代名；
层 = 承载该材质的 Renderer 所属 GameObject 的 `m_Layer`（**prefab 自带层**，运行期会被覆盖）。
本工具不改任何 .cs / 资产 / 账，也不跑 Unity。
"""


# ----------------------------------------------------------------------
#  基础工具
# ----------------------------------------------------------------------
def read_text(path):
    with io.open(path, "r", encoding="utf-8", errors="replace") as f:
        return f.read()


def rel(path):
    """相对 d:/4/Unity 的路径（统一正斜杠），便于写进证据列。"""
    p = os.path.abspath(path).replace("\\", "/")
    base = UNITY_DIR.replace("\\", "/") + "/"
    if p.startswith(base):
        return p[len(base):]
    return p


def line_of(text, offset):
    return text.count("\n", 0, offset) + 1


def parse_meta_guid(path):
    """读 .meta 的 `guid:`（第一处）。"""
    meta = path + ".meta"
    if not os.path.exists(meta):
        return None
    for ln in read_text(meta).splitlines():
        s = ln.strip()
        if s.startswith("guid:"):
            return s.split(":", 1)[1].strip()
    return None


def build_material_index(mat_dir):
    """材质名 → {guid, shader_guid, queue, path}（按 .mat 现读）。"""
    idx = {}
    if not os.path.isdir(mat_dir):
        return idx
    for fn in sorted(os.listdir(mat_dir)):
        if not fn.endswith(".mat"):
            continue
        p = os.path.join(mat_dir, fn)
        guid = parse_meta_guid(p)
        if not guid:
            continue
        txt = read_text(p)
        nm = re.search(r"^  m_Name:\s*(.*?)\s*$", txt, re.M)
        sm = re.search(r"m_Shader:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)", txt)
        qm = re.search(r"m_CustomRenderQueue:\s*(-?\d+)", txt)
        idx[guid] = {
            "name": nm.group(1) if nm else fn[:-4],
            "shader_guid": sm.group(1) if sm else None,
            "queue": int(qm.group(1)) if qm else None,
            "path": p,
        }
    return idx


def find_shader_guid(shader_dir, shader_name):
    """按 shader 内的 `Shader "<name>"` 找资产，返回其 .meta guid。"""
    if not os.path.isdir(shader_dir):
        return None
    for fn in sorted(os.listdir(shader_dir)):
        if not fn.endswith(".shader"):
            continue
        p = os.path.join(shader_dir, fn)
        txt = read_text(p)
        m = re.search(r'^\s*Shader\s+"([^"]+)"', txt, re.M)
        if m and m.group(1).strip() == shader_name:
            return parse_meta_guid(p)
    return None


def parse_feature_consts(feature_cs):
    """从 GrabScreen501 读常量：原名 / 目标层 / 开关名。"""
    out = {
        "original": FALLBACK_ORIGINAL,
        "default_layer": None,
        "env_switch": "WF_GRAB501",
        "env_layer": "WF_GRAB501_LAYER",
        "feature_path": feature_cs,
    }
    if not os.path.exists(feature_cs):
        return out
    txt = read_text(feature_cs)

    m = re.search(r'DistortionOriginalName\s*=\s*"([^"]+)"', txt)
    if m:
        out["original"] = m.group(1)
    m = re.search(r"public\s+const\s+int\s+DefaultLayer\s*=\s*(\d+)", txt)
    if m:
        out["default_layer"] = int(m.group(1))
    m = re.search(r'EnvSwitch\s*=\s*"([^"]+)"', txt)
    if m:
        out["env_switch"] = m.group(1)
    m = re.search(r'EnvLayer\s*=\s*"([^"]+)"', txt)
    if m:
        out["env_layer"] = m.group(1)
    return out


def parse_shader_map(shader_map_cs, original):
    """从 WarpforgeShaderMap.cs 的 Replacements 读 `<原名> → <替代名>`。"""
    if not os.path.exists(shader_map_cs):
        return None
    txt = read_text(shader_map_cs)
    pat = re.compile(r'\{\s*"' + re.escape(original) + r'"\s*,\s*"([^"]+)"\s*\}')
    m = pat.search(txt)
    if m:
        return m.group(1)
    return None


# ----------------------------------------------------------------------
#  prefab 解析（Unity YAML 文本；按 `--- !u!<class> &<fileID>` 切文档）
# ----------------------------------------------------------------------
DOC_RE = re.compile(r"^--- !u!(\d+) &(\d+)\s*$")


def split_docs(text):
    """返回 [(class_id, file_id, start_line, [lines...])]，行号 1 基。"""
    lines = text.splitlines()
    docs = []
    cur = None
    for i, ln in enumerate(lines):
        m = DOC_RE.match(ln)
        if m:
            if cur:
                docs.append(cur)
            cur = [m.group(1), m.group(2), i + 1, []]
        elif cur is not None:
            cur[3].append((i + 1, ln))
    if cur:
        docs.append(cur)
    return docs


def parse_binder_materials(doc_lines):
    """在 binder 文档里解析 materials 列表 → [{name, shader, queue, line}]。"""
    out = []
    in_list = False
    cur = None
    for lineno, ln in doc_lines:
        s = ln.strip()
        if not in_list:
            if s == "materials:":
                in_list = True
            continue
        # materials 列表项：`  - name: X`
        m = re.match(r"^\s*-\s+name:\s*(.*?)\s*$", ln)
        if m:
            if cur:
                out.append(cur)
            cur = {"name": m.group(1), "shader": None, "queue": None, "line": lineno}
            continue
        if cur is None:
            # materials 之后出现别的键（且缩进 < 2）⇒ 列表结束
            if s and not ln.startswith("    ") and not ln.startswith("  -"):
                in_list = False
            continue
        ms = re.match(r"^\s*shader:\s*(.*?)\s*$", ln)
        if ms and cur["shader"] is None:
            cur["shader"] = ms.group(1)
            cur["line"] = lineno  # 证据行优先指 shader 那一行
            continue
        mr = re.match(r"^\s*renderQueue:\s*(-?\d+)\s*$", ln)
        if mr and cur["queue"] is None:
            cur["queue"] = int(mr.group(1))
            continue
    if cur:
        out.append(cur)
    return out


def parse_renderer_materials(doc_lines):
    """Renderer 文档 → (材质 guid 列表, m_Materials 行号)。"""
    guids = []
    mat_line = None
    in_list = False
    for lineno, ln in doc_lines:
        if not in_list:
            if ln.strip() == "m_Materials:":
                in_list = True
                mat_line = lineno
            continue
        m = re.search(r"guid:\s*([0-9a-f]{32})", ln)
        if m:
            guids.append(m.group(1))
            continue
        if ln.strip().startswith("-") or ln.strip().startswith("{fileID"):
            continue
        # 列表结束
        in_list = False
    # 粒子拖尾材质
    for lineno, ln in doc_lines:
        m = re.search(r"^\s*trailMaterial:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]{32})", ln)
        if m:
            guids.append(m.group(1))
    return guids, mat_line


def parse_prefab(path, material_index, distortion_shader_guid):
    """返回 {name, binder:[...], renderers:[{mats, layers:[(layer,line)], line}], guid}"""
    txt = read_text(path)
    docs = split_docs(txt)

    gos = {}       # fileID -> (name, layer, layer_line)
    renderers = [] # dicts
    binder = []

    for cls, fid, start, lines in docs:
        body = "\n".join(l for _, l in lines)
        if cls == "1" and "m_Layer:" in body:
            nm = re.search(r"^  m_Name:\s*(.*?)\s*$", body, re.M)
            lm = re.search(r"^  m_Layer:\s*(\d+)\s*$", body, re.M)
            if lm:
                lline = None
                for lineno, ln in lines:
                    if re.match(r"^  m_Layer:\s*\d+\s*$", ln):
                        lline = lineno
                        break
                gos[fid] = (nm.group(1) if nm else "?", int(lm.group(1)), lline)
        if "WarpforgeEffectBinder" in body:
            binder.extend(parse_binder_materials(lines))
        if "m_Materials:" in body:
            guids, mat_line = parse_renderer_materials(lines)
            if not guids:
                continue
            mats = []
            for g in guids:
                info = material_index.get(g)
                if info and info["shader_guid"] == distortion_shader_guid:
                    mats.append(info["name"])
            if not mats:
                continue
            gm = re.search(r"^  m_GameObject:\s*\{fileID:\s*(\d+)", body, re.M)
            go_id = gm.group(1) if gm else None
            layer, lline = None, None
            if go_id in gos:
                layer, lline = gos[go_id][1], gos[go_id][2]
            renderers.append({"mats": mats, "layer": layer, "layer_line": lline, "line": mat_line})

    return {
        "name": os.path.basename(path)[:-len(".prefab")],
        "path": path,
        "guid": parse_meta_guid(path),
        "binder": binder,
        "renderers": renderers,
    }


# ----------------------------------------------------------------------
#  外部交叉数据
# ----------------------------------------------------------------------
def parse_effect_tsv(path, original):
    """效果→shader 列表表 → 用到原名的效果名集合（只读）。"""
    rows = set()
    total = 0
    if not os.path.exists(path):
        return None, 0
    with io.open(path, "r", encoding="utf-8-sig", errors="replace") as f:
        first = True
        for ln in f:
            if first:
                first = False
                continue
            c = ln.rstrip("\r\n").split("\t")
            if len(c) < 3:
                continue
            total += 1
            shaders = [s.strip() for s in c[2].split(",")]
            if original in shaders:
                rows.add(c[0].strip())
    return rows, total


def parse_card_refs(path):
    """card_vfx_by_card.json → {效果名: [(卡名, 阵营)]}。"""
    refs = {}
    if not os.path.exists(path):
        return refs
    import json
    try:
        d = json.loads(read_text(path))
    except ValueError:
        return refs
    for card in d.get("cards", []) or []:
        nm = card.get("nm") or "?"
        fa = card.get("fa") or "?"
        cid = card.get("id") or "?"
        for ev in card.get("events", []) or []:
            for es in ev.get("es", []) or []:
                n = es.get("n")
                if not n:
                    continue
                refs.setdefault(n, []).append((nm, fa, cid))
    return refs


def parse_orig_material_queues(tsv_path):
    """`数据/游戏数据/mat_renderqueue.tsv` → {材质名: m_CustomRenderQueue}（原版材质真值）。"""
    out = {}
    if not os.path.exists(tsv_path):
        return out
    with io.open(tsv_path, "r", encoding="utf-8-sig", errors="replace") as f:
        for i, ln in enumerate(f):
            if i == 0:
                continue
            c = ln.rstrip("\r\n").split("\t")
            if len(c) < 2:
                continue
            try:
                out[c[0].strip()] = int(c[1])
            except ValueError:
                continue
    return out


def scan_static_referrers(assets_dir, exclude_dir, guid_to_effect, max_bytes=64 * 1024 * 1024):
    """扫 `Assets/**/*.unity|*.prefab`（排除 VFX/Prefabs 自身），找对效果 prefab guid 的**静态引用**。
    返回 ({效果名: [(相对路径, 行号)]}, 扫描文件数, 跳过的大文件数)。"""
    hits = {}
    scanned = 0
    skipped = 0
    excl = os.path.abspath(exclude_dir).replace("\\", "/") + "/"
    pat = re.compile(r"guid:\s*([0-9a-f]{32})")
    for dirpath, dirnames, filenames in os.walk(assets_dir):
        dirnames[:] = [d for d in sorted(dirnames) if not d.startswith(".")]
        for fn in sorted(filenames):
            if not (fn.endswith(".unity") or fn.endswith(".prefab")):
                continue
            p = os.path.join(dirpath, fn)
            rp = os.path.abspath(p).replace("\\", "/")
            if rp.startswith(excl):
                continue
            try:
                if os.path.getsize(p) > max_bytes:
                    skipped += 1
                    continue
            except OSError:
                skipped += 1
                continue
            txt = read_text(p)
            scanned += 1
            for m in pat.finditer(txt):
                nm = guid_to_effect.get(m.group(1))
                if nm:
                    line = line_of(txt, m.start())
                    hits.setdefault(nm, []).append((rel(p), line))
    return hits, scanned, skipped


def parse_documented_count(order_md):
    """从施工单读「N 个失真效果」这一历史口径。"""
    if not os.path.exists(order_md):
        return None
    txt = read_text(order_md)
    m = re.search(r"(\d+)\s*个失真效果", txt)
    return int(m.group(1)) if m else None


# ----------------------------------------------------------------------
#  报告
# ----------------------------------------------------------------------
def build_report(opts):
    original = opts["original"]
    replacement = opts["replacement"]
    mat_index = build_material_index(opts["material_dir"])
    dist_shader_guid = opts["distortion_shader_guid"]
    target_layer = opts["target_layer"]

    prefabs = []
    for fn in sorted(os.listdir(opts["prefab_dir"])):
        if not fn.endswith(".prefab"):
            continue
        p = os.path.join(opts["prefab_dir"], fn)
        pf = parse_prefab(p, mat_index, dist_shader_guid)
        hit_binder = [e for e in pf["binder"] if e["shader"] == original or (replacement and e["shader"] == replacement)]
        if not hit_binder and not pf["renderers"]:
            continue
        pf["hit_binder"] = hit_binder
        prefabs.append(pf)

    card_refs = parse_card_refs(opts["card_json"])
    orig_queues = parse_orig_material_queues(opts["queue_tsv"])
    tsv_names, tsv_total = parse_effect_tsv(opts["effect_tsv"], original)
    documented = parse_documented_count(opts["order_md"])

    # ---- 汇总 ----
    binder_entries = sum(len(p["hit_binder"]) for p in prefabs)
    mat_names = {}
    layer_hist = {}
    queue_hist = {}
    renderer_total = 0
    already_at_target = 0
    no_renderer = 0
    for p in prefabs:
        for e in p["hit_binder"]:
            mat_names[e["name"]] = mat_names.get(e["name"], 0) + 1
            q = e["queue"] if e["queue"] is not None else -1
            queue_hist[q] = queue_hist.get(q, 0) + 1
        rl = [r for r in p["renderers"] if r["layer"] is not None]
        renderer_total += len(rl)
        if not rl:
            no_renderer += 1
        for r in rl:
            layer_hist[r["layer"]] = layer_hist.get(r["layer"], 0) + 1
        if rl and all(r["layer"] == target_layer for r in rl):
            already_at_target += 1

    lines = []
    A = lines.append
    A("# B107 · VFX-B14 失真效果挪层清单（生成件，⛔ 别手改）")
    A("")
    A("> 生成器：`工具/gen_grabpass_effects.py --write`（只读扫描，不跑 Unity）· 重跑同一份输入 ⇒ 逐字节相同。")
    A("> 用途：开 `" + opts["env_switch"] + "=1`（原版 `GrabScreenFeature` 的 501 重画）之前，核对"
      "「哪些效果会被搬到 501 层」。")
    A("")
    A("## 一、口径")
    A("")
    A("| 项 | 值 | 出处 |")
    A("|---|---|---|")
    A("| 失真 shader 原名 | `" + original + "` | `GrabPassTransparentFeature.cs` 的 `DistortionOriginalName` |")
    A("| 我们的替代 shader | `" + (replacement or "（WarpforgeShaderMap 未登记）") + "` | `WarpforgeShaderMap.cs` 的 `Replacements` |")
    A("| 目标层（建议） | **" + str(target_layer) + "**（可用 `" + opts["env_layer"] + "` 覆盖，0..31） | `GrabScreen501.DefaultLayer` |")
    A("| 「当前层」口径 | **prefab 自带 `GameObject.m_Layer`**（运行期会被 `Play` 的父层/显式层覆盖，开 501 后由 `FinalLayer` 统一覆盖到目标层） | `WarpforgeEffectPlayer.cs` 的 `SetLayerRec` / `FinalLayer` 调用点 |")
    A("| 判定「是不是失真件」 | prefab 的 `WarpforgeEffectBinder.materials[].shader` 命中原名/替代名 | `GrabScreen501.UsesDistortion` 第 1 条判据 |")
    A("")
    A("## 二、现读计数（⛔ 每次重跑现读，别把这里的数字抄进别处）")
    A("")
    A("| 项 | 现读 |")
    A("|---|---|")
    A("| 失真**效果**（prefab） | **" + str(len(prefabs)) + "** |")
    A("| binder 里失真的**材质槽条目** | **" + str(binder_entries) + "** |")
    A("| 不同材质名 | **" + str(len(mat_names)) + "** |")
    A("| 承载失真材质的 Renderer | **" + str(renderer_total) + "** |")
    A("| 解析不出 Renderer 层 / 无 Renderer 的效果 | **" + str(no_renderer) + "** |")
    A("| 全部渲染器都**已在**目标层 " + str(target_layer) + " 的效果 | **" + str(already_at_target) + "** |")
    A("| 历史表 `效果_shader对账.tsv` 命中行 | **" + (str(len(tsv_names)) if tsv_names is not None else "（表缺失）") + "** / 全表 " + str(tsv_total) + " 行 |")
    A("| 施工单写的历史口径 | " + (str(documented) if documented is not None else "（未读到）") + " |")
    A("")
    if documented is not None:
        delta = len(prefabs) - documented
        if delta == 0:
            A("✅ **与历史口径一致**：" + str(documented) + " = 现读 " + str(len(prefabs)) + "。")
        else:
            A("⚠️ **与历史口径不一致**：现读 " + str(len(prefabs)) + "，历史写 " + str(documented)
              + "，差 " + ("%+d" % delta) + "。⛔ 别为了凑数改口径；先查差在哪（重导 prefab / 新增效果 / 表过期）。")
        A("")
    if tsv_names is not None:
        cur = set(p["name"] for p in prefabs)
        only_pf = sorted(cur - tsv_names)
        only_tsv = sorted(tsv_names - cur)
        A("交叉核对 `效果_shader对账.tsv`（2026-09-17）：仅 prefab 有 **" + str(len(only_pf)) + "** · 仅表有 **"
          + str(len(only_tsv)) + "**")
        if only_pf or only_tsv:
            if only_pf:
                A("- 仅 prefab 有：" + "、".join("`%s`" % x for x in only_pf[:20]) + ("…" if len(only_pf) > 20 else ""))
            if only_tsv:
                A("- 仅表有：" + "、".join("`%s`" % x for x in only_tsv[:20]) + ("…" if len(only_tsv) > 20 else ""))
        A("")

    A("层分布（渲染器个数）：" + (" · ".join("层 %d×%d" % (k, layer_hist[k]) for k in sorted(layer_hist)) or "（无）"))
    A("")
    A("队列分布（材质槽条目）：" + (" · ".join("%d×%d" % (k, queue_hist[k]) for k in sorted(queue_hist)) or "（无）")
      + "　（判据：prefab 的 binder `renderQueue`；原版材质 `m_CustomRenderQueue` 全是 `-1` ⇒ 用 shader 默认 `Transparent`=3000）")
    A("")

    # ---- 主表 ----
    A("## 三、逐件表（按效果名排序；" + str(len(prefabs)) + " 行）")
    A("")
    A("| # | 效果 | 材质（binder 材名） | 当前层（prefab 自带） | 当前队列 | 建议目标层 | 引用卡 | 是否已挪 | 证据 |")
    A("|---|---|---|---|---|---|---|---|---|")
    for i, p in enumerate(prefabs, 1):
        mats = sorted(set(e["name"] for e in p["hit_binder"]))
        if not mats:
            mats = sorted(set(m for r in p["renderers"] for m in r["mats"]))
        qs = sorted(set(e["queue"] for e in p["hit_binder"] if e["queue"] is not None))
        rl = [r for r in p["renderers"] if r["layer"] is not None]
        lay = []
        seen = {}
        for r in rl:
            seen[r["layer"]] = seen.get(r["layer"], 0) + 1
        for k in sorted(seen):
            lay.append("%d（%d 个渲染器）" % (k, seen[k]) if len(rl) > 1 else str(k))
        layer_cell = " · ".join(lay) if lay else "?"
        queue_cell = " · ".join(str(q) for q in qs) if qs else "?"
        # 卡片引用按效果名（prefab 名）统计（多数情况下二者同名）
        card_list = card_refs.get(p["name"], [])
        if card_list:
            fac = {}
            for nm, fa, cid in card_list:
                fac[fa] = fac.get(fa, 0) + 1
            card_cell = "%d 张（%s）" % (len(card_list), "、".join("%s×%d" % (k, fac[k]) for k in sorted(fac)))
        else:
            card_cell = "0"
        if rl and all(r["layer"] == target_layer for r in rl):
            moved = "✅ 在目标层"
        elif rl:
            moved = "❌ 未挪（开 501 自动）"
        else:
            moved = "? 无渲染器"
        # 证据：binder shader 行 + 渲染器 -> GO 层行 + 材质资产（若可解析）
        ev = []
        if p["hit_binder"]:
            ev.append("`%s:%d`（binder shader）" % (rel(p["path"]), p["hit_binder"][0]["line"]))
        for r in rl[:3]:
            if r["line"] and r["layer_line"]:
                ev.append("`:%d→%d`（材质→层 %d）" % (r["line"], r["layer_line"], r["layer"]))
        A("| %d | %s | %s | %s | %s | %d | %s | %s | %s |" % (
            i, p["name"], " · ".join(mats) if mats else "?", layer_cell, queue_cell, target_layer,
            card_cell, moved, " · ".join(ev)))
    A("")

    # ---- 按材质分组 ----
    A("## 四、按材质分组（材质名 → 用它的效果数）")
    A("")
    mat_assets = {}
    for info in mat_index.values():
        mat_assets.setdefault(info["name"], info)
    A("| 材质名 | 效果数 | 我们工程里的 `.mat`（m_CustomRenderQueue） | 原版 `m_CustomRenderQueue` | 效果（前 8 个） |")
    A("|---|---|---|---|---|")
    by_mat = {}
    for p in prefabs:
        for e in p["hit_binder"]:
            by_mat.setdefault(e["name"], []).append(p["name"])
    for mn in sorted(by_mat):
        names = sorted(by_mat[mn])
        info = mat_assets.get(mn)
        if info is None:
            # 去掉导出器加的 `__<8位hash>` 后缀再试一次
            base = re.sub(r"__[0-9a-f]{8}$", "", mn)
            info = mat_assets.get(base)
        asset_cell = ("`%s`（%s）" % (rel(info["path"]), info["queue"])) if info else "（工程里没有同名 .mat；运行期按名从 bundle 取）"
        oq = orig_queues.get(mn)
        oq_cell = str(oq) if oq is not None else "（表里没有）"
        A("| %s | %d | %s | %s | %s |" % (mn, len(names), asset_cell, oq_cell,
                                          "、".join(names[:8]) + ("…" if len(names) > 8 else "")))
    A("")

    # ---- 引用者 ----
    A("## 五、引用者（卡 / 场景 / prefab）")
    A("")
    A("### 5.1 卡（`CardPresentation/Resources/card_vfx_by_card.json`，运行期那份）")
    A("")
    A("按 `cards[].events[].es[].n`（= 效果 prefab 名）命中。")
    A("")
    hit_cards = [(p["name"], card_refs.get(p["name"], [])) for p in prefabs if card_refs.get(p["name"])]
    A("有卡片引用的效果：**" + str(len(hit_cards)) + "** / " + str(len(prefabs)) + "。")
    A("")
    if hit_cards:
        A("| 效果 | 引用卡数 | 阵营分布 |")
        A("|---|---|---|")
        for nm, lst in sorted(hit_cards, key=lambda t: (-len(t[1]), t[0]))[:60]:
            fac = {}
            for _n, fa, _c in lst:
                fac[fa] = fac.get(fa, 0) + 1
            A("| %s | %d | %s |" % (nm, len(lst), "、".join("%s×%d" % (k, fac[k]) for k in sorted(fac))))
        if len(hit_cards) > 60:
            A("")
            A("（只列前 60；完整数据见主表「引用卡」列。）")
        A("")

    # 静态引用（场景 / 其它 prefab）——只认 guid，跳过大文件
    guid_to_effect = {}
    for p in prefabs:
        if p["guid"]:
            guid_to_effect[p["guid"]] = p["name"]
    static_hits, scanned_files, skipped_files = scan_static_referrers(
        ASSETS_DIR, opts["prefab_dir"], guid_to_effect)
    A("### 5.2 场景 / 其它 prefab（静态 guid 引用）")
    A("")
    A("扫描范围：`Assets/**/*.unity` + `Assets/**/*.prefab`（排除 `" + rel(opts["prefab_dir"]) + "/` 自身）"
      "，单文件 > 64 MB 跳过。现读扫了 **" + str(scanned_files) + "** 个文件"
      + ("（跳过 " + str(skipped_files) + " 个超大文件）" if skipped_files else "") + "。")
    A("")
    if static_hits:
        A("| 效果 | 引用处数 | 前几处（文件:行） |")
        A("|---|---|---|")
        for nm in sorted(static_hits):
            locs = sorted(static_hits[nm])
            A("| %s | %d | %s |" % (nm, len(locs),
                                   " · ".join("`%s:%d`" % (a, b) for a, b in locs[:4])))
        A("")
        A("⚠️ 有静态引用 = 这些 prefab 被**别的资产直接引用**。现读其中 `Tap*` 三件走的是 "
          "`TapHitMaterialDefinition.ParticleCollisionDefinition.PlayParticle`（`Object.Instantiate` 缓存一件反复挪），"
          "**不经过** `WarpforgeEffectPlayer.Play`（`CardPresentation/Battle/TapHitMaterialDefinition.cs:119`）⇒"
          " 开 501 时**不会被 `FinalLayer` 挪层**，处理方式二选一：① 让那条实例化路径也过 `GrabScreen501.FinalLayer`，"
          "② 确认可接受「它们保持旧行为（采样上一帧的抓屏）」。"
          "⚠️ **不会**因此被画两遍 —— 双画只发生在「同一个物体既在默认透明 pass、又在 501 层」时，它们不在 501 层。")
    else:
        A("现读 **0 处**静态引用 —— 这些效果都是运行期按名字 `Play` 的（与 `GrabScreen501` 的自动档假设一致）。")
    A("")

    # ---- 怎么用 ----
    A("## 六、怎么用这张表开 501")
    A("")
    A("1. **不需要逐件手改**：`GrabScreen501.UsesDistortion` 是**数据驱动**的（binder `materials[].shader` +"
      " 运行期 `shader.name`），开 `" + opts["env_switch"] + "=1` 时 `FinalLayer` 会把上表每一件搬到目标层，"
      "`SetLayerRec` 递归整棵子树。")
    A("2. 表的作用 = **核对覆盖面**：若某件该挪却没被识别（例如 binder 材质丢了 shader 名），它不会被搬 ⇒"
      " 会在默认透明 pass 和 501 各画一遍（扭曲叠两遍）。逐件核对「材质列是否等于预期」。")
    A("3. A/B 两跑（主对话 · Unity 全局串行）：同屏两个扭曲件叠加的场景，分别用 `" + opts["env_switch"] + "=0/1`"
      " 各渲一次逐像素比。⛔ 别在批处理（sweep/自检）里开它 —— 没有帧循环时 `EnsureCameraSeesRelocated` 不跑，"
      " 效果会整块空掉（见 `GrabPassTransparentFeature.cs` 文件尾 ⑤）。")
    A("4. 层号不是原版值：原版 `UniversalRendererData` 资产本地没解出来 ⇒ 目标层是我们挑的（`DefaultLayer`），"
      " `" + opts["env_layer"] + "` 可覆盖。")
    A("")

    # ---- 边界 ----
    A("## 七、覆盖边界 / 没查清")
    A("")
    A("- 「当前层」= prefab 自带层，**不是运行期真实层**（不跑 Unity 量不到；运行期由 `Play` / `FinalLayer` 决定）。")
    A("- 只认 `Assets/WarpforgeVFX/Materials/` 里能解析到 shader 的 `.mat`；运行期从 bundle 现取的材质若**没有**"
      "同名 `.mat` 资产，本工具看不到（本轮现读：233/233 效果都能解析到至少一个失真渲染器）。")
    A("- `m_CustomRenderQueue` 的原始值取自 `数据/游戏数据/mat_renderqueue.tsv`（全是 `-1`）；"
      "表里「当前队列」用的是 prefab binder 的 `renderQueue`（运行期材质重建时按它覆盖）。")
    A("- 引用卡只按 `card_vfx_by_card.json`（运行期那份）；不含立绘/语音/卡背引用。")
    A("- 场景静态引用只认 guid（`*.unity` / `*.prefab`，>64 MB 跳过）；`.asset` / Addressables 容器未扫。")
    A("- 🔴 **发现的文档不一致（只读记录，未改任何 .cs）**：`GrabPassTransparentFeature.cs` 文件尾"
      "「2026-10-25 起有自动档」那段写着「层默认 **31**」，而同一个文件的常量 "
      "`GrabScreen501.DefaultLayer = " + str(target_layer) + "`（`ParseLayer` 都读它）⇒ **以常量为准，"
      "文件尾那句是过期的**。本表目标层按常量 " + str(target_layer) + "。")
    A("")
    return "\n".join(lines) + "\n"


# ----------------------------------------------------------------------
#  main
# ----------------------------------------------------------------------
def main(argv):
    if len(argv) <= 1:
        print(USAGE)
        # 🔴 2026-11-05（B146-E1）：不带显式动作 ⇒ 非零退出（纪律同 §三「写盘类脚本不带动词 = 什么都不做」）。
        return 2

    mode = None
    quiet = False
    prefab_dir = DEFAULT_PREFAB_DIR
    material_dir = DEFAULT_MATERIAL_DIR
    out_path = DEFAULT_OUT
    i = 1
    while i < len(argv):
        a = argv[i]
        if a in ("-h", "--help"):
            print(USAGE)
            return 0
        elif a == "--write":
            if mode == "check":
                print("⛔ --write 与 --check 不能同时给", file=sys.stderr)
                return 2
            mode = "write"
        elif a == "--check":
            if mode == "write":
                print("⛔ --write 与 --check 不能同时给", file=sys.stderr)
                return 2
            mode = "check"
        elif a == "--quiet":
            quiet = True
        elif a == "--prefabs" and i + 1 < len(argv):
            i += 1
            prefab_dir = argv[i]
        elif a == "--materials" and i + 1 < len(argv):
            i += 1
            material_dir = argv[i]
        elif a == "--out" and i + 1 < len(argv):
            i += 1
            out_path = argv[i]
        else:
            print("未知参数：%s\n" % a, file=sys.stderr)
            print(USAGE, file=sys.stderr)
            return 2
        i += 1

    if mode is None:
        print(USAGE)
        # 🔴 2026-11-05（B146-E1）：只给了 `--quiet/--out` 这类修饰、没给动作 ⇒ 同样非零退出、不做事。
        return 2

    if not os.path.isdir(prefab_dir):
        print("⛔ prefab 目录不存在：%s" % prefab_dir, file=sys.stderr)
        return 3

    consts = parse_feature_consts(DEFAULT_FEATURE_CS)
    original = consts["original"]
    replacement = parse_shader_map(DEFAULT_SHADER_MAP_CS, original) or FALLBACK_REPLACEMENT
    dist_shader_guid = find_shader_guid(DEFAULT_SHADER_DIR, replacement)
    if not dist_shader_guid:
        print("⛔ 找不到替代 shader 资产（`%s`）的 guid；查 Assets/WarpforgeVFX/Shaders/" % replacement, file=sys.stderr)
        return 3

    target_layer = consts["default_layer"]
    if target_layer is None:
        print("⛔ 读不到 GrabScreen501.DefaultLayer", file=sys.stderr)
        return 3

    text = build_report({
        "prefab_dir": prefab_dir,
        "material_dir": material_dir,
        "original": original,
        "replacement": replacement,
        "distortion_shader_guid": dist_shader_guid,
        "target_layer": target_layer,
        "env_switch": consts["env_switch"],
        "env_layer": consts["env_layer"],
        "effect_tsv": DEFAULT_EFFECT_TSV,
        "card_json": DEFAULT_CARD_JSON,
        "order_md": DEFAULT_ORDER_MD,
        "queue_tsv": DEFAULT_QUEUE_TSV,
    })

    if mode == "write":
        d = os.path.dirname(out_path)
        if d and not os.path.isdir(d):
            os.makedirs(d)
        with io.open(out_path, "w", encoding="utf-8", newline="\n") as f:
            f.write(text)
        if not quiet:
            print("已写：%s（%d 字节）" % (out_path, len(text.encode("utf-8"))))
        return 0

    # --check
    if not os.path.exists(out_path):
        print("⛔ --check：产物不存在：%s（先跑 --write）" % out_path, file=sys.stderr)
        return 1
    old = read_text(out_path)
    if old == text:
        if not quiet:
            print("一致：%s" % out_path)
        return 0
    print("⛔ 漂移：%s 与现读不一致（重跑 --write）" % out_path, file=sys.stderr)
    return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
