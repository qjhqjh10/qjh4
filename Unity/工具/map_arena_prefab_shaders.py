#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""map_arena_prefab_shaders.py — 战场线 · 把 staging 材质咬的 Dummy shader guid 换成本工程可解析的 shader

背景
----
`工具/extract_arena_prefab_yaml.py` 按设计**不拷 `.shader`**：ExportedProject 里的 shader
全是 AssetRipper 的 Dummy（`//DummyShaderTextExporter`，只剩 Properties + 空 pass）——
照拷进工程等于「占个位、画不出东西」。所以 staging 里的 `.mat` 仍指着
**ExportedProject 那套 guid**（`m_Shader: {fileID: 4800000, guid: <64hex>, type: 3}`），
直接 import 会全变 Missing / 品红。

本工具做的**只有一件事**：把那些 guid 换成我们工程里**真存在、能解析**的 shader guid，
**保留 fileID / type 不动**，其余字节逐字节照拷。

映射判据（唯一来源 = 工程自己的解析链，⛔ 不另发明）
---------------------------------------------------
按 `Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs` 的 `TryResolve` 顺序，为「静态 import」
这个场景做的等价裁剪：

  ① `Replacements`（原版名 → 自建 shader）
     —— 表里有的名字**优先**用它的目标。⚠️ 这些名字里很多同时在 `UseOriginal` 白名单里
        （正常态走 bundle 原件）；**静态 import 拿不到 bundle 里的 shader 对象**
        （它不是工程资产），所以只能落到注释里写明的「**兜底路**」= `Replacements`。
        出处：`WarpforgeShaderMap.cs:574-594` 那段「白名单取件失败才落 Replacements」。
  ② 工程自带同名 —— `Shader.Find(originalName)` 的等价物：先扫 `Assets/**/*.shader`，
     再扫 `Library/PackageCache/**/*.shader`（URP 包就住这儿，guid 与 ExportedProject
     里那些**以 guid 命名的 .shader 文件**逐字符相同，可反证是同一条）。
  ③ 都没有 ⇒ **unresolved**（⛔ 不猜、不拿「看起来像」的顶）。

Unity 内建引用（`guid: 0000000000000000f000000000000000`）
---------------------------------------------------------
它**不是 Dummy**（任何 Unity 工程都解析得到），所以默认**原样保留**，只在 `_shader_map.tsv`
里把 fileID 解析成 shader 名写出来（10720 = `Mobile/Particles/Additive` · 10721 =
`Mobile/Particles/Alpha Blended`；这两个名字是 2026-10-23 用 UnityPy 读
`<Unity>/Editor/Data/Resources/unity_builtin_extra` 的 `m_ParsedForm.m_Name` 现查的，
此前 `战场线_prefabYAML试点_battlearena3.md` §六·5 记的是「没查到」）。
`--remap-builtin` 可把它们按 `Replacements` 一起换掉（默认**关**：见报告 §没查清）。

用法
----
    # 出副本 + 映射表（默认路径）
    python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py

    # 干跑：只打印解析结果，不写盘
    python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py --dry-run

    # 把 unresolved 也兜底到 URP/Unlit（战场线对「取不到原版 shader」的既有做法，
    # 见 资料/已知的坑.md:427 —— ⚠️ 会改变观感，默认关）
    python -I -X utf8 d:/4/Unity/工具/map_arena_prefab_shaders.py --fallback-urp-unlit

产出
----
    <out>/                                  ← 副本（结构与 src 相同；只有 .mat 的 m_Shader guid 变了）
    <out>/_shader_map.tsv                   ← 原 guid / shader 名 / 我们的 guid / 解析来源 / unresolved
    <out>/_unresolved.md                    ← unresolved 逐条 + 影响的材质
    <out>/_mat_changes.tsv                  ← 每个被改的 .mat：旧 guid → 新 guid

红线
----
* ⛔ 不写 `Assets/**`、不写 `d:/2`、不动 git、不跑 Unity。
* ⛔ 不改 prefab / 网格 / 贴图：`--check-only`（自检）会逐文件比哈希。
* ⛔ 不发明映射：判据只有 `WarpforgeShaderMap` 的两张表 + 工程里真有的同名 shader。

退出码
------
0 = 成功（可以带 unresolved）；2 = 自检失败（会打印哪一条）。
"""

from __future__ import annotations

import argparse
import hashlib
import os
import re
import shutil
import sys
import time
from collections import Counter, OrderedDict, defaultdict

# ---------------------------------------------------------------------------
# 常量
# ---------------------------------------------------------------------------

SRC_DEFAULT = r"d:/4/_tmp_view/arena_prefabs_out"
OUT_DEFAULT = r"d:/4/_tmp_view/arena_prefabs_out_shaded"
EXPORTED_DEFAULT = r"d:/2/tools/assetripper_out/ExportedProject"
ASSETS_DEFAULT = r"d:/4/Unity/MyGame/Assets"
PACKAGE_CACHE_DEFAULT = r"d:/4/Unity/MyGame/Library/PackageCache"
SHADERMAP_CS_DEFAULT = r"d:/4/Unity/MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs"
UNITY_RESOURCES_DEFAULT = r"D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources"

BUILTIN_GUID = "0000000000000000f000000000000000"

# `m_Shader: {fileID: 4800000, guid: <64hex>, type: 3}` —— 只替换 guid，fileID/type 原样保留
SHADER_REF_RE = re.compile(
    rb"m_Shader:\s*\{fileID:\s*(-?\d+),\s*guid:\s*([0-9a-fA-F]{32}),\s*type:\s*(-?\d+)\}"
)
DECL_RE = re.compile(r'^\s*Shader\s+"([^"]+)"')
GUID_LINE_RE = re.compile(r"^guid:\s*([0-9a-f]{32})", re.M)

# `--remap-builtin` / `--fallback-urp-unlit` 用；两者都要求工程里有这个 shader
URP_UNLIT_NAME = "Universal Render Pipeline/Unlit"

# Unity 内建 fileID 兜底表（动态读取失败时用；2026-10-23 实读 unity_builtin_extra /
# unity default resources 的 m_ParsedForm.m_Name 得来 —— 只收实际见过的几条）
BUILTIN_FALLBACK = {
    10720: "Mobile/Particles/Additive",
    10721: "Mobile/Particles/Alpha Blended",
    10722: "Mobile/Particles/VertexLit Blended",
    10723: "Mobile/Particles/Multiply",
    10753: "Sprites/Default",
    10757: "Sprites/Mask",
    10755: "Hidden/FrameDebuggerRenderTargetDisplay",
}

# 解析来源的枚举（写进 tsv，别写成自由文本 —— 下游要能筛）
SRC_REPLACEMENTS = "Replacements(自建)"
SRC_SAME_NAME = "工程自带同名(Assets)"
SRC_SAME_NAME_PKG = "工程自带同名(Package)"
SRC_BUILTIN_KEPT = "Unity内建(保留)"
SRC_FALLBACK = "fallback-urp-unlit(⚠️近似)"
SRC_UNRESOLVED = "UNRESOLVED"


# ---------------------------------------------------------------------------
# 小工具
# ---------------------------------------------------------------------------

def read_bytes(path: str) -> bytes:
    with open(path, "rb") as f:
        return f.read()


def read_text(path: str) -> str:
    return read_bytes(path).decode("utf-8", "replace")


def sha1_bytes(b: bytes) -> str:
    return hashlib.sha1(b).hexdigest()


def parse_meta_guid(meta_path: str) -> str | None:
    m = GUID_LINE_RE.search(read_text(meta_path))
    return m.group(1) if m else None


def shader_decl_name(shader_path: str) -> str | None:
    """只认**非注释行**的 `Shader "名字"` —— 我们的 .shader 文件头注释里常引用原版名字，
    早期用整篇正则会把注释里的名字当成真名（本仓踩过）。"""
    for line in read_text(shader_path).splitlines():
        s = line.strip().lstrip("﻿")      # ⚠️ 有文件名带 BOM（`TMP_SDF SSD.shader`），不剥就漏
        if s.startswith("//") or s.startswith("*") or s.startswith("/*"):
            continue
        m = DECL_RE.match(s)
        if m:
            return m.group(1)
    return None


# ---------------------------------------------------------------------------
# 三张表
# ---------------------------------------------------------------------------

def scan_exported_shaders(exported_root: str):
    """ExportedProject: guid → (shader 名, 相对路径)。
    ⚠️ AssetRipper 的 `.shader` 全是 Dummy，名字只能从文件内容第一行 `Shader "…"` 取。"""
    guid2name, guid2path = {}, {}
    if not os.path.isdir(exported_root):
        raise SystemExit(f"[FATAL] ExportedProject 不存在：{exported_root}")
    for dp, _dn, fn in os.walk(exported_root):
        for f in fn:
            if not f.endswith(".shader.meta"):
                continue
            mp = os.path.join(dp, f)
            g = parse_meta_guid(mp)
            if not g:
                continue
            sp = mp[: -len(".meta")]
            nm = shader_decl_name(sp) if os.path.isfile(sp) else None
            guid2name[g] = nm
            guid2path[g] = os.path.relpath(sp, exported_root)
    return guid2name, guid2path


def scan_project_shaders(root: str, label: str):
    """Assets/ 或 PackageCache/: 返回 (name→(guid,path), guid→(name,path))。
    同名多份时保留第一份并记警告（不静默）。"""
    name2, guid2, dup = {}, {}, []
    if not os.path.isdir(root):
        return name2, guid2, dup
    for dp, _dn, fn in os.walk(root):
        for f in fn:
            if not f.endswith(".shader"):
                continue
            sp = os.path.join(dp, f)
            mp = sp + ".meta"
            if not os.path.isfile(mp):
                continue
            g = parse_meta_guid(mp)
            nm = shader_decl_name(sp)
            if not g or not nm:
                continue
            guid2[g] = (nm, sp)
            if nm in name2 and name2[nm][0] != g:
                dup.append((nm, name2[nm][1], sp, label))
                continue
            name2[nm] = (g, sp)
    return name2, guid2, dup


def parse_shader_map_cs(cs_path: str):
    """从 WarpforgeShaderMap.cs 里读 `Replacements` 与 `UseOriginal` 两张表。
    ⛔ 不在本脚本里另抄一份 —— 抄了就是第二个真相来源（铁律 6 的同族问题）。"""
    t = read_text(cs_path)

    def block(start_marker: str) -> str:
        i = t.index(start_marker)
        j = t.index("\n        };", i)
        return t[i:j]

    def quoted(s: str):
        out = []
        for line in s.splitlines():
            line = line.split("//")[0]          # 去掉行尾注释（名字里没有 '//'）
            out += re.findall(r'"([^"]*)"', line)
        return out

    repl_strs = quoted(block("Replacements = new Dictionary<string, string>"))
    pairs = list(zip(repl_strs[0::2], repl_strs[1::2]))
    replacements = OrderedDict(pairs)
    use_original = set(quoted(block("UseOriginal = new HashSet<string>")))
    return replacements, use_original


def scan_builtin_names(unity_resources: str):
    """读 Unity 编辑器自带的两份内建资源，返回 fileID → shader 名。
    UnityPy 不可用 / 编辑器路径不对 ⇒ 退回 BUILTIN_FALLBACK（并打印用了哪条路）。"""
    names = dict(BUILTIN_FALLBACK)
    mode = "fallback-table"
    try:
        import UnityPy  # type: ignore

        got = 0
        for fn in ("unity_builtin_extra", "unity default resources"):
            p = os.path.join(unity_resources, fn)
            if not os.path.isfile(p):
                continue
            env = UnityPy.load(p)
            for o in env.objects:
                if o.type.name != "Shader":
                    continue
                try:
                    d = o.read()
                except Exception:
                    continue
                pf = getattr(d, "m_ParsedForm", None)
                nm = getattr(pf, "m_Name", None) if pf else None
                if nm:
                    names[o.path_id] = nm
                    got += 1
        if got:
            mode = f"UnityPy({got} 条)"
    except Exception as e:  # noqa: BLE001 —— 读不到就不读，退回表
        mode = f"fallback-table({type(e).__name__})"
    return names, mode


# ---------------------------------------------------------------------------
# 解析
# ---------------------------------------------------------------------------

def resolve_all(mat_guids, export_guid2name, asset_name2, asset_guid2,
                pkg_name2, pkg_guid2, replacements, use_original,
                builtin_names, remap_builtin, fallback_urp_unlit):
    """返回 OrderedDict(guid → 解析记录)。每条记录都带**出处**，方便报告逐行核。"""
    rows = OrderedDict()
    urp_unlit = None
    if fallback_urp_unlit:
        if URP_UNLIT_NAME in pkg_name2:
            urp_unlit = pkg_name2[URP_UNLIT_NAME][0]
        elif URP_UNLIT_NAME in asset_name2:
            urp_unlit = asset_name2[URP_UNLIT_NAME][0]

    for guid, file_ids in sorted(mat_guids.items()):
        rec = {
            "guid": guid,
            "name": None,
            "our_guid": None,
            "source": SRC_UNRESOLVED,
            "unresolved": True,
            "in_use_original": False,
            "note": "",
            "file_ids": file_ids,
        }

        # ---- Unity 内建 ----
        if guid == BUILTIN_GUID:
            names = sorted({builtin_names.get(fid, f"<未知内建 fileID {fid}>") for fid in file_ids})
            rec["name"] = " / ".join(names)
            if remap_builtin:
                target_name = names[0]
                if target_name in replacements:
                    tgt = replacements[target_name]
                    if tgt in asset_name2:
                        rec.update(our_guid=asset_name2[tgt][0], source=SRC_REPLACEMENTS,
                                   unresolved=False,
                                   note=f"内建 fileID→名({target_name})，再按 Replacements")
                    else:
                        rec["note"] = f"Replacements 目标 `{tgt}` 工程里没有"
                elif target_name in asset_name2:
                    rec.update(our_guid=asset_name2[target_name][0], source=SRC_SAME_NAME,
                               unresolved=False, note="内建名在 Assets 里同名")
                elif target_name in pkg_name2:
                    rec.update(our_guid=pkg_name2[target_name][0], source=SRC_SAME_NAME_PKG,
                               unresolved=False, note="内建名在包里有同名")
                else:
                    rec["note"] = "内建名两张表都没有"
            else:
                rec.update(source=SRC_BUILTIN_KEPT, unresolved=False,
                           note="非 Dummy；Unity 内建资源，任何工程都解析得到 ⇒ 默认原样保留")
            rows[guid] = rec
            continue

        # ---- Dummy（AssetRipper 导出）----
        name = export_guid2name.get(guid)
        rec["name"] = name
        if name is None:
            rec["note"] = "ExportedProject 的 .shader.meta 里没有这个 guid"
            rows[guid] = rec
            continue

        rec["in_use_original"] = name in use_original

        if name in replacements:
            tgt = replacements[name]
            if tgt in asset_name2:
                rec.update(our_guid=asset_name2[tgt][0], source=SRC_REPLACEMENTS,
                           unresolved=False,
                           note="Replacements" + ("；原名在 UseOriginal，静态 import 走兜底路"
                                                  if rec["in_use_original"] else ""))
                rows[guid] = rec
                continue
            rec["note"] = f"Replacements 目标 `{tgt}` 工程里没有"

        if name in asset_name2:
            rec.update(our_guid=asset_name2[name][0], source=SRC_SAME_NAME, unresolved=False,
                       note="工程自带同名（Assets）")
        elif name in pkg_name2:
            rec.update(our_guid=pkg_name2[name][0], source=SRC_SAME_NAME_PKG, unresolved=False,
                       note="工程自带同名（Library/PackageCache）")
        elif fallback_urp_unlit and urp_unlit:
            rec.update(our_guid=urp_unlit, source=SRC_FALLBACK, unresolved=True,
                       note="兜底：URP/Unlit（⚠️近似，见 资料/已知的坑.md:427）")
        else:
            if not rec["note"]:
                rec["note"] = ("两张表都没有、工程也没有同名 —— 原件只在随包 bundle 里"
                               "（资料/普查产出_1022/J-A_战场素材与材质诊断.md:50）")
        rows[guid] = rec
    return rows


# ---------------------------------------------------------------------------
# 扫描 staging 的材质引用
# ---------------------------------------------------------------------------

def scan_mats(src_root: str):
    """返回 (guid → set(fileID), [(mat 路径, [(fid,guid,type), …]), …])"""
    per_mat, guid_fids = [], defaultdict(set)
    for dp, _dn, fn in os.walk(src_root):
        for f in sorted(fn):
            if not f.endswith(".mat"):
                continue
            p = os.path.join(dp, f)
            refs = []
            for m in SHADER_REF_RE.finditer(read_bytes(p)):
                refs.append((m.group(1).decode(), m.group(2).decode().lower(), m.group(3).decode()))
                guid_fids[m.group(2).decode().lower()].add(int(m.group(1)))
            per_mat.append((p, refs))
    return guid_fids, per_mat


# ---------------------------------------------------------------------------
# 写盘
# ---------------------------------------------------------------------------

def rewrite_mat(data: bytes, rows) -> tuple[bytes, list[tuple[str, str]]]:
    """只替换 **guid 那 32 个字符**（fileID / type / 任何空白原样保留）。
    返回新字节 + [(旧 guid, 新 guid), …]。"""
    changes = []

    def sub(m):
        guid_b = m.group(2)
        guid = guid_b.decode().lower()
        rec = rows.get(guid)
        if rec and rec["our_guid"]:
            changes.append((guid, rec["our_guid"]))
            # 在**原始匹配串**里换掉 guid —— 不重建整行，避免顺手动到空白（铁律：只有 guid 该变）
            return m.group(0).replace(guid_b, rec["our_guid"].encode())
        return m.group(0)

    return SHADER_REF_RE.sub(sub, data), changes


def tree_hash(root: str, skip=()) -> str:
    h = hashlib.sha1()
    for dp, dn, fn in os.walk(root):
        dn.sort()
        for f in sorted(fn):
            p = os.path.join(dp, f)
            rel = os.path.relpath(p, root).replace("\\", "/")
            if rel in skip:
                continue
            h.update(rel.encode("utf-8", "replace"))
            h.update(b"\0")
            h.update(hashlib.sha1(read_bytes(p)).digest())
    return h.hexdigest()


def do_copy(src_root: str, out_root: str, rows):
    """逐文件复制；.mat 走 rewrite。返回统计。"""
    stats = Counter()
    mat_changes = []          # (相对路径, 旧 guid, 新 guid, shader 名)
    for dp, dn, fn in os.walk(src_root):
        dn.sort()
        rel_dir = os.path.relpath(dp, src_root)
        dst_dir = out_root if rel_dir == "." else os.path.join(out_root, rel_dir)
        os.makedirs(dst_dir, exist_ok=True)
        for f in sorted(fn):
            sp = os.path.join(dp, f)
            dpth = os.path.join(dst_dir, f)
            data = read_bytes(sp)
            if f.endswith(".mat"):
                new, ch = rewrite_mat(data, rows)
                if ch:
                    stats["mat_rewritten"] += 1
                    for old, newg in ch:
                        mat_changes.append((os.path.relpath(dpth, out_root), old, newg,
                                            rows[old]["name"] or "?"))
                else:
                    stats["mat_untouched"] += 1
                with open(dpth, "wb") as fo:
                    fo.write(new)
            else:
                shutil.copyfile(sp, dpth)
                stats["copied"] += 1
    return stats, mat_changes


# ---------------------------------------------------------------------------
# 自检
# ---------------------------------------------------------------------------

def self_check(src_root, out_root, rows, asset_guid2, pkg_guid2, builtin_names,
               fallback_urp_unlit):
    """返回 (ok, lines[])。四条：清点 / 非 Dummy leftover / 非 .mat 字节一致 / .mat 只差 shader。"""
    lines, ok = [], True

    # ① 副本里每个 m_Shader guid 的归属
    cat = Counter()
    leftover = []
    for dp, _dn, fn in os.walk(out_root):
        for f in fn:
            if not f.endswith(".mat"):
                continue
            p = os.path.join(dp, f)
            for m in SHADER_REF_RE.finditer(read_bytes(p)):
                g = m.group(2).decode().lower()
                if g in asset_guid2:
                    cat["Assets .meta（我们的 shader）"] += 1
                elif g in pkg_guid2:
                    cat["Packages .meta（URP 等包内 shader）"] += 1
                elif g == BUILTIN_GUID:
                    cat["Unity 内建（fileID 解析，无 .meta）"] += 1
                else:
                    cat["❌ 解析不到（Dummy 残留）"] += 1
                    leftover.append((os.path.relpath(p, out_root), g))
    lines.append("① 副本 shader 引用归属：" + " · ".join(f"{k} {v}" for k, v in cat.items()))
    # ⚠️ 「解析不到」= 本工具**明知** unresolved 的还是那些（清单在 _unresolved.md）；
    #    只有「本该换掉却仍是 Dummy」才是 FAIL（否则 unresolved 会被当成工具坏了）。
    unexpected = [(p, g) for p, g in leftover if not (g in rows and rows[g]["unresolved"])]
    if leftover:
        lines.append(f"   预期内 unresolved 引用 {len(leftover) - len(unexpected)} 处（全部在 _unresolved.md 里）")
    if unexpected:
        ok = False
        lines.append(f"   ❌ 意外残留（本该换掉却没换）{len(unexpected)} 处；前 10：{unexpected[:10]}")

    # ② unresolved 清点
    unr = [r for r in rows.values() if r["unresolved"]]
    lines.append(f"② unresolved：{len(unr)} / {len(rows)} 个 guid")
    for r in unr:
        lines.append(f"   · {r['guid']}  {r['name']}  ({r['source']})")

    # ③ 非 .mat 字节一致（prefab / 网格 / 贴图 / .meta 全在这个集合里）
    diff = []
    for dp, _dn, fn in os.walk(src_root):
        for f in fn:
            if f.endswith(".mat"):
                continue
            sp = os.path.join(dp, f)
            rel = os.path.relpath(sp, src_root)
            dpth = os.path.join(out_root, rel)
            if not os.path.isfile(dpth) or sha1_bytes(read_bytes(sp)) != sha1_bytes(read_bytes(dpth)):
                diff.append(rel)
    lines.append(f"③ 非 .mat 文件逐字节一致：{'PASS' if not diff else 'FAIL'}（{len(diff)} 处不同）")
    if diff:
        ok = False
        lines.append(f"   前 10：{diff[:10]}")

    # ④ .mat **只有 shader guid 那 32 个字符**能变（抹掉 guid 后逐字节相等）
    mask_re = re.compile(rb"(m_Shader:\s*\{fileID:\s*-?\d+,\s*guid:\s*)[0-9a-fA-F]{32}")
    md = []
    for dp, _dn, fn in os.walk(src_root):
        for f in fn:
            if not f.endswith(".mat"):
                continue
            sp = os.path.join(dp, f)
            rel = os.path.relpath(sp, src_root)
            dpth = os.path.join(out_root, rel)
            a = mask_re.sub(rb"\1<MASK>", read_bytes(sp))
            b = mask_re.sub(rb"\1<MASK>", read_bytes(dpth)) if os.path.isfile(dpth) else b""
            if a != b:
                md.append(rel)
    lines.append(f"④ .mat 除 m_Shader 外逐字节一致：{'PASS' if not md else 'FAIL'}（{len(md)} 处不同）")
    if md:
        ok = False
        lines.append(f"   前 10：{md[:10]}")

    # ⑤ 副本里 prefab 的 md5 与 manifest 对照（防「以为拷对了」）
    return ok, lines


# ---------------------------------------------------------------------------
# main
# ---------------------------------------------------------------------------

def main(argv=None):
    ap = argparse.ArgumentParser(description="把战场 prefab staging 的 Dummy shader guid 换成本工程可解析的 shader")
    ap.add_argument("--src", default=SRC_DEFAULT)
    ap.add_argument("--out", default=OUT_DEFAULT)
    ap.add_argument("--exported", default=EXPORTED_DEFAULT)
    ap.add_argument("--assets", default=ASSETS_DEFAULT)
    ap.add_argument("--package-cache", default=PACKAGE_CACHE_DEFAULT)
    ap.add_argument("--shader-map", default=SHADERMAP_CS_DEFAULT)
    ap.add_argument("--unity-resources", default=UNITY_RESOURCES_DEFAULT)
    ap.add_argument("--remap-builtin", action="store_true",
                    help="把 Unity 内建引用也按 Replacements 换掉（默认保留）")
    ap.add_argument("--fallback-urp-unlit", action="store_true",
                    help="unresolved 兜底到 URP/Unlit（⚠️近似，会让观感偏离；默认关）")
    ap.add_argument("--dry-run", action="store_true", help="只解析、不写盘")
    args = ap.parse_args(argv)

    t0 = time.time()
    src = os.path.abspath(args.src)
    out = os.path.abspath(args.out)
    if not os.path.isdir(src):
        raise SystemExit(f"[FATAL] src 不存在：{src}")
    if out == src or out.startswith(src + os.sep) or src.startswith(out + os.sep):
        raise SystemExit(f"[FATAL] out 与 src 不能互相包含：src={src} out={out}")
    if out.lower().replace("\\", "/").startswith(os.path.abspath(args.assets).lower().replace("\\", "/")):
        raise SystemExit("[FATAL] out 落在 Assets/ 里 —— 本工具只写 staging（红线）")

    print(f"[1/6] 读 ExportedProject shader 表：{args.exported}")
    export_guid2name, export_guid2path = scan_exported_shaders(args.exported)
    print(f"      {len(export_guid2name)} 个 .shader.meta")

    print(f"[2/6] 读工程 shader 表：{args.assets}")
    asset_name2, asset_guid2, dup_a = scan_project_shaders(args.assets, "Assets")
    print(f"      Assets {len(asset_guid2)} 个 shader")
    pkg_name2, pkg_guid2, dup_p = scan_project_shaders(args.package_cache, "PackageCache")
    print(f"      PackageCache {len(pkg_guid2)} 个 shader")
    for nm, p1, p2, lab in (dup_a + dup_p):
        print(f"      [WARN] 同名 shader 多份（{lab}）：{nm} —— 用 {p1}，忽略 {p2}")

    print(f"[3/6] 读 WarpforgeShaderMap 判据：{args.shader_map}")
    replacements, use_original = parse_shader_map_cs(args.shader_map)
    print(f"      Replacements {len(replacements)} 条 · UseOriginal {len(use_original)} 条")
    missing_targets = sorted({t for t in replacements.values() if t not in asset_name2})
    if missing_targets:
        print(f"      [WARN] Replacements 目标里工程没有的：{missing_targets}")

    builtin_names, builtin_mode = scan_builtin_names(args.unity_resources)
    print(f"[4/6] Unity 内建 fileID 表：{builtin_mode}")

    print(f"[5/6] 扫描 staging 材质：{src}")
    guid_fids, per_mat = scan_mats(src)
    print(f"      {len(per_mat)} 个 .mat · {len(guid_fids)} 个不同 shader guid")

    rows = resolve_all(guid_fids, export_guid2name, asset_name2, asset_guid2,
                       pkg_name2, pkg_guid2, replacements, use_original,
                       builtin_names, args.remap_builtin, args.fallback_urp_unlit)

    # 控制台小表
    print("\n  原 guid                             shader 名                                      → 我们的 guid                    来源")
    for g, r in rows.items():
        print(f"  {g}  {(r['name'] or '?')[:44]:<44}  {(r['our_guid'] or '-')[:32]:<32}  {r['source']}")
    n_unres = sum(1 for r in rows.values() if r["unresolved"])
    n_mats_unres = 0
    for _p, refs in per_mat:
        if any(rows[g]["unresolved"] for _f, g, _t in refs):
            n_mats_unres += 1
    print(f"\n  解析：{len(rows) - n_unres}/{len(rows)} 个 guid · unresolved {n_unres} 个"
          f"（影响 {n_mats_unres}/{len(per_mat)} 个 .mat）")

    if args.dry_run:
        print("\n--dry-run：不写盘。")
        return 0

    print(f"[6/6] 复制 + 改写 → {out}")
    if os.path.isdir(out):
        # 🔴 只在「确实是我们上一轮的产物」时才删 —— 防手滑把 --out 指到别的目录（如 _tmp_view 本身）
        if not os.path.isfile(os.path.join(out, "_shader_map.tsv")):
            raise SystemExit(f"[FATAL] out 已存在且不像本工具的产物（没有 _shader_map.tsv）⇒ 拒绝覆盖：{out}")
        shutil.rmtree(out)
    os.makedirs(out)
    stats, mat_changes = do_copy(src, out, rows)
    print(f"      改写 {stats['mat_rewritten']} 个 .mat · 未改 {stats['mat_untouched']} · 原样复制 {stats['copied']} 个文件")

    # ---- _shader_map.tsv ----
    tsv = os.path.join(out, "_shader_map.tsv")
    with open(tsv, "w", encoding="utf-8", newline="\n") as f:
        f.write("orig_guid\tshader_name\tour_guid\tsource\tunresolved\tuse_original_whitelist\tfile_ids\tmats\tnote\n")
        mat_count = Counter()
        for _p, refs in per_mat:
            for _f, g, _t in refs:
                mat_count[g] += 1
        for g, r in rows.items():
            f.write("\t".join([
                g,
                r["name"] or "",
                r["our_guid"] or "",
                r["source"],
                "1" if r["unresolved"] else "0",
                "1" if r["in_use_original"] else "0",
                ",".join(str(x) for x in sorted(r["file_ids"])),
                str(mat_count[g]),
                r["note"].replace("\t", " "),
            ]) + "\n")

    # ---- _mat_changes.tsv ----
    with open(os.path.join(out, "_mat_changes.tsv"), "w", encoding="utf-8", newline="\n") as f:
        f.write("mat\told_guid\tnew_guid\tshader_name\n")
        for p, old, new, nm in mat_changes:
            f.write(f"{p}\t{old}\t{new}\t{nm}\n")

    # ---- _unresolved.md ----
    unr = [r for r in rows.values() if r["unresolved"]]
    by_guid_mats = defaultdict(list)
    for p, refs in per_mat:
        for _f, g, _t in refs:
            if rows[g]["unresolved"]:
                by_guid_mats[g].append(os.path.relpath(p, src))
    with open(os.path.join(out, "_unresolved.md"), "w", encoding="utf-8", newline="\n") as f:
        f.write("# unresolved shader（副本里仍指向 ExportedProject 的 guid ⇒ import 后 Missing / 品红）\n\n")
        # ⚠️ 故意**不写生成时间** —— 写了就不是字节幂等（重跑一次 tree_hash 就变，判「产物没变」会假红）
        f.write(f"源：`{src}` **（本文件由 `工具/map_arena_prefab_shaders.py` 生成；重跑逐字节相同）**\n\n")
        f.write(f"**{len(unr)} / {len(rows)} 个 guid unresolved，影响 {n_mats_unres} / {len(per_mat)} 个 .mat。**\n\n")
        f.write("| # | 原 guid | 原版 shader 名 | 用它的材质数 | ExportedProject 文件 | 说明 |\n")
        f.write("|---|---|---|---|---|---|\n")
        for i, r in enumerate(unr, 1):
            mats = by_guid_mats.get(r["guid"], [])
            ex = sorted({os.path.basename(os.path.dirname(m)) for m in mats})
            f.write(f"| {i} | `{r['guid']}` | `{r['name']}` | {len(mats)} | "
                    f"`{export_guid2path.get(r['guid'], '?')}` | {r['note']} |\n")
        f.write("\n## 每个 unresolved 用在哪\n\n")
        for r in unr:
            mats = by_guid_mats.get(r["guid"], [])
            f.write(f"### `{r['name']}`（{r['guid']}）\n\n")
            for m in mats:
                f.write(f"- `{m}`\n")
            f.write("\n")
        f.write("\n## 怎么办（不要在这里瞎补映射）\n\n")
        f.write("1. **原件在随包 bundle 里**（`资料/普查产出_1022/J-A_战场素材与材质诊断.md:50` 实读 20/20 全在）"
                "——但 bundle 里的 shader **不是工程资产**，静态材质指不过去。\n")
        f.write("2. 要消掉这些品红，只有两条路：① 运行时用 `WarpforgeShaderLoader` 换 shader（战场线已有那套）；"
                "② 重跑本工具加 `--fallback-urp-unlit`（兜底 URP/Unlit，⚠️ 观感会变，是本仓对"
                "「取不到原版 shader」的既有做法，见 `资料/已知的坑.md:427`）。\n")

    # ---- 自检 ----
    print("\n== 自检 ==")
    ok, lines = self_check(src, out, rows, asset_guid2, pkg_guid2, builtin_names,
                           args.fallback_urp_unlit)
    for ln in lines:
        print("  " + ln)
    print(f"\n完成：{time.time() - t0:.1f}s · 产物 {out}")
    print(f"  tree_hash = {tree_hash(out)}")
    return 0 if ok else 2


if __name__ == "__main__":
    sys.exit(main())
