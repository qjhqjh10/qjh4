#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_probe_prefab_deps.py — 只读探针：摸清一个原版 bundle 里「Unity 枚举不到」的 prefab 的依赖树。

背景（别重复查）：2026-09-29/30 Unity 侧实测 —— `battleprefabs_vfxandmisc_assets_all.bundle`
的 `AssetBundle.GetAllAssetNames()` 返回 983 条、`LoadAllAssets<GameObject>()` 返回 965 个，
**两条都不含** `Card 3D Death Explosion` 与 `Vanguard Frame Animated VAT`。
本脚本从**原始包**这一侧回答：这两件在不在包里 / 为什么枚举不到 / 拖出来要带多少东西。

结论正本：`d:/4/Unity/资料/普查产出_0930/非addressable_prefab_依赖树.md`

用法（⚠️ 只读：不写 d:/2/、不写工程目录，缓存只落 %TEMP%）：
    python _probe_prefab_deps.py --stage index          # 只在含名字的表 + AssetBundle 两张表
    python _probe_prefab_deps.py --stage cab            # 扫全目录，建 CAB-xxx → 磁盘包 的映射（缓存）
    python _probe_prefab_deps.py --stage tree  --json out.json   # 依赖树（会用上面的缓存）
    python _probe_prefab_deps.py --stage all   --json out.json

⚠️ 性能纪律：**绝不 `o.read()` 整包**。名字走 `read_typetree()`（字典）或前 4+n 字节裸解析。
"""
from __future__ import annotations

import argparse
import collections
import json
import os
import tempfile
import traceback

import UnityPy

BUNDLE = (r"D:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/"
          r"StandaloneWindows64/battleprefabs_vfxandmisc_assets_all.bundle")
BUNDLE_DIR = os.path.dirname(BUNDLE)
GAME_ROOT = r"d:/2/unity_run_ref"
CAB_CACHE = os.path.join(tempfile.gettempdir(), "wf_cab_map.json")

WANTED = ["Card 3D Death Explosion", "Vanguard Frame Animated VAT"]

# 这些类型**整棵树都在自己身上**（没有出边），或者出边很少 —— 名字走裸解析，
# 免得把顶点/像素数据当 list 解出来。
LEAF_NO_TT = {
    "Mesh", "Texture2D", "Cubemap", "Texture2DArray", "RenderTexture",
    "TextAsset", "AudioClip", "Font", "Shader", "VideoClip",
}
BIG_READ_OK = 6_000_000      # 超过这个字节数就不整篇读 typetree（只裸解析名字）


# ----------------------------------------------------------------------------- helpers

def get_sf(path):
    env = UnityPy.load(path)
    bf = list(env.files.values())[0]
    sf = next(v for v in bf.files.values() if type(v).__name__ == "SerializedFile")
    return env, bf, sf


def raw_name(o):
    """对象序列化数据的**第一个字段如果是 string**，直接裸解析。"""
    try:
        data = o.get_raw_data()
    except Exception:
        return None
    if len(data) < 4:
        return None
    n = int.from_bytes(data[0:4], "little")
    if n <= 0 or n > 512 or 4 + n > len(data):
        return None
    try:
        return data[4:4 + n].decode("utf-8")
    except Exception:
        return None


def read_tt(o, check=True):
    try:
        return o.read_typetree(check_read=check)
    except Exception:
        if check:
            return read_tt(o, check=False)
        raise


def type_name(o):
    try:
        return o.type.name
    except Exception:
        return f"class_id={o.class_id}"


def obj_name(o, tname=None):
    tname = tname or type_name(o)
    if tname in LEAF_NO_TT or o.byte_size > BIG_READ_OK:
        n = raw_name(o)
        if n:
            return n
    try:
        return read_tt(o).get("m_Name") or ""
    except Exception:
        return raw_name(o) or ""


def collect_pptrs(value, path, out):
    """从 typetree 字典里递归收集 PPtr（形如 {'m_FileID':int,'m_PathID':int} 的 dict）。"""
    if isinstance(value, dict):
        if set(value.keys()) == {"m_FileID", "m_PathID"}:
            out.append((path, int(value["m_FileID"]), int(value["m_PathID"])))
        else:
            for k, v in value.items():
                collect_pptrs(v, f"{path}.{k}" if path else k, out)
    elif isinstance(value, (list, tuple)):
        for i, v in enumerate(value):
            collect_pptrs(v, f"{path}[{i}]", out)
    return out


# ---- CAB → 磁盘包 的映射 -------------------------------------------------------

def build_cab_map(force=False, verbose=True):
    """扫 AA 目录下每个 `.bundle` 的**内部文件名**（`CAB-xxx`），建反向表。

    Unity 的 externals 里写的是 `archive:/CAB-<hash>/CAB-<hash>`，
    **不是磁盘文件名** —— 要答「指向哪个外部文件」必须做这一步。
    """
    if not force and os.path.exists(CAB_CACHE):
        try:
            with open(CAB_CACHE, encoding="utf-8") as f:
                m = json.load(f)
            if m:
                return m
        except Exception:
            pass
    files = sorted(f for f in os.listdir(BUNDLE_DIR) if f.endswith(".bundle"))
    m, errs = {}, {}
    for i, fn in enumerate(files):
        p = os.path.join(BUNDLE_DIR, fn)
        try:
            env = UnityPy.load(p)
            bf = list(env.files.values())[0]
            for k in bf.files.keys():
                m[k.lower()] = fn
            del env, bf
        except Exception as e:
            errs[fn] = f"{type(e).__name__}: {e}"
        if verbose and i % 20 == 0:
            print(f"    cab 扫描 {i+1}/{len(files)} …", flush=True)
    m = {"cabs": m, "errors": errs, "n_bundles": len(files)}
    try:
        with open(CAB_CACHE, "w", encoding="utf-8") as f:
            json.dump(m, f)
    except Exception:
        pass
    return m


class ExtResolver:
    """按 m_FileID → externals[file_id-1].path → CAB → 磁盘包，惰性加载并读名字。

    ⚠️ 只加载**用到的那几个**包（用完就放），不整包 read。
    """

    def __init__(self, sf):
        self.sf = sf
        self.cab2file = build_cab_map(verbose=False).get("cabs", {})
        self.envs = {}
        self.memo = {}

    def ext_raw_path(self, file_id):
        if file_id <= 0 or file_id > len(self.sf.externals):
            return f"?<file_id={file_id} 越界>"
        return getattr(self.sf.externals[file_id - 1], "path", "")

    def ext_cab(self, file_id):
        raw = self.ext_raw_path(file_id)
        if raw.startswith("archive:/"):
            return raw.split("/")[2] if len(raw.split("/")) > 2 else raw
        return raw

    def ext_disk(self, file_id):
        cab = self.ext_cab(file_id)
        return self.cab2file.get(cab.lower())

    def _load(self, file_id):
        if file_id in self.envs:
            return self.envs[file_id]
        disk = self.ext_disk(file_id)
        ent = {"disk": disk, "sf": None, "obj_by_pid": None}
        if disk:
            try:
                _env, _bf, sf = get_sf(os.path.join(BUNDLE_DIR, disk))
                ent["env"], ent["sf"] = _env, sf
            except Exception as e:
                ent["err"] = f"{type(e).__name__}: {e}"
        self.envs[file_id] = ent
        return ent

    def info(self, file_id, path_id):
        key = (file_id, path_id)
        if key in self.memo:
            return self.memo[key]
        ent = self._load(file_id)
        rec = {"file_id": file_id, "pathid": path_id,
               "cab": self.ext_cab(file_id), "disk_bundle": ent["disk"],
               "type": None, "name": None, "byte_size": None,
               "extra": None, "note": "本机无对应包" if not ent["disk"] else None}
        sf = ent.get("sf")
        if sf is not None:
            o = sf.objects.get(path_id)
            if o is None:
                rec["note"] = "目标 PathID 不在该包里"
            else:
                rec["type"] = type_name(o)
                rec["byte_size"] = o.byte_size
                rec["name"] = obj_name(o, rec["type"])
                if rec["type"] == "MonoScript":
                    try:
                        d = read_tt(o)
                        rec["extra"] = {"m_ClassName": d.get("m_ClassName"),
                                        "m_Namespace": d.get("m_Namespace"),
                                        "m_AssemblyName": d.get("m_AssemblyName")}
                    except Exception as e:
                        rec["extra"] = f"读不到: {type(e).__name__}"
        self.memo[key] = rec
        return rec


# ----------------------------------------------------------------------------- stage: index

def stage_index(sf, out):
    hist = collections.Counter()
    for o in sf.objects.values():
        hist[type_name(o)] += 1
    out["total_objects"] = sum(hist.values())
    out["type_histogram"] = dict(hist.most_common())
    out["counts"] = {k: hist.get(k, 0) for k in
                     ("GameObject", "Transform", "RectTransform", "MonoBehaviour",
                      "Material", "Texture2D", "Mesh")}

    ab = None
    for o in sf.objects.values():
        if type_name(o) == "AssetBundle":
            ab = o
            break
    if ab is None:
        out["assetbundle"] = None
        return out
    d = read_tt(ab)
    container = d.get("m_Container") or []
    preload = d.get("m_PreloadTable") or []
    names = [c[0] for c in container]
    tgt = [c[1]["asset"]["m_PathID"] for c in container]
    tgt_ok = [p for p in tgt if p in sf.objects]
    tgt_type = collections.Counter(type_name(sf.objects[p]) for p in tgt_ok)
    pre_pids = [p["m_PathID"] for p in preload]
    pre_uniq = set(pre_pids)
    out["assetbundle"] = {
        "path_id": ab.path_id,
        "m_Name": d.get("m_Name", ""),
        "m_Container": len(container),
        "m_Container_unique_names": len(set(names)),
        "m_Container_targets_type": dict(tgt_type.most_common()),
        "m_Container_targets_exist": len(tgt_ok),
        "m_Container_targets_missing": len(tgt) - len(tgt_ok),
        "m_Container_dup_names": [k for k, v in collections.Counter(names).items() if v > 1],
        "m_Container_all": [[c[0], c[1]["asset"]["m_FileID"], c[1]["asset"]["m_PathID"]]
                            for c in container],
        "m_PreloadTable": len(preload),
        "m_PreloadTable_unique": len(pre_uniq),
        "m_PreloadTable_unique_existing": len([p for p in pre_uniq if p in sf.objects]),
        "m_IsStreamedSceneAssetBundle": d.get("m_IsStreamedSceneAssetBundle"),
    }

    found = {}
    scanned = 0
    for o in sf.objects.values():
        if type_name(o) != "GameObject":
            continue
        scanned += 1
        nm = obj_name(o, "GameObject")
        if nm in WANTED:
            found.setdefault(nm, []).append(o.path_id)
    out["gameobjects_total_scanned"] = scanned
    out["found"] = found

    memb = {}
    for nm in WANTED:
        pids = set(found.get(nm, []))
        memb[nm] = {
            "pathids": sorted(pids),
            "in_container": [[c[0], c[1]["asset"]["m_PathID"]] for c in container
                             if c[1]["asset"]["m_PathID"] in pids],
            "in_preload": sorted(p for p in pids if p in pre_uniq),
            "in_preload_times": sum(1 for p in pre_pids if p in pids),
            "container_name_substr": [c[0] for c in container if nm.lower() in c[0].lower()],
            "go_type_hist_ok": True,
        }
    out["membership"] = memb
    return out


# ----------------------------------------------------------------------------- stage: tree

def node_extra(d, tname, o, ext):
    ex = {}
    if tname == "Material":
        ex["shader"] = d.get("m_Shader")
        tex = (d.get("m_SavedProperties") or {}).get("m_TexEnvs") or []
        ex["tex_envs"] = [[t[0], t[1]["m_Texture"]["m_FileID"], t[1]["m_Texture"]["m_PathID"]]
                          for t in tex]
    elif tname == "Mesh":
        vd = d.get("m_VertexData") or {}
        ex["vertex_count"] = vd.get("m_VertexCount")
        ex["submesh_count"] = len(d.get("m_SubMeshes") or [])
        ex["index_count"] = len(d.get("m_IndexBuffer") or [])
    elif tname in ("Texture2D", "Cubemap", "Texture2DArray"):
        ex["width"] = d.get("m_Width")
        ex["height"] = d.get("m_Height")
        ex["format"] = d.get("m_TextureFormat")
        sd = d.get("m_StreamData") or {}
        if sd.get("size"):
            ex["stream"] = [sd.get("path"), sd.get("offset"), sd.get("size")]
    elif tname == "MonoBehaviour":
        ex["script"] = d.get("m_Script")
        ex["script_fields"] = [k for k in d.keys() if k not in ("m_GameObject", "m_Script")]
    elif tname == "Animator":
        ex["controller"] = d.get("m_Controller")
        ex["avatar"] = d.get("m_Avatar")
    return ex


def stage_tree(sf, roots, ext, out, cap=200000):
    nodes, edges, ext_raw = {}, [], []
    stack = [int(r) for r in roots]
    while stack:
        pid = stack.pop()
        if pid in nodes or pid == 0:
            continue
        if len(nodes) >= cap:
            out["truncated"] = True
            break
        o = sf.objects.get(pid)
        if o is None:
            nodes[pid] = {"type": "?不在本文件", "name": "", "byte_size": 0}
            continue
        tname = type_name(o)
        d, ex = None, {}
        if tname in LEAF_NO_TT or o.byte_size > BIG_READ_OK:
            nm = raw_name(o) or ""
            if tname != "Shader" and o.byte_size <= BIG_READ_OK:
                try:
                    d = read_tt(o)
                    nm = nm or (d.get("m_Name") or "")
                except Exception as e:
                    ex["tt_err"] = type(e).__name__
        else:
            try:
                d = read_tt(o)
                nm = d.get("m_Name") or ""
            except Exception as e:
                nm = raw_name(o) or ""
                ex["tt_err"] = type(e).__name__
        if d is not None:
            try:
                ex.update(node_extra(d, tname, o, ext))
            except Exception as e:
                ex["extra_err"] = f"{type(e).__name__}: {e}"
            for path, fid, tpid in collect_pptrs(d, "", []):
                if tpid == 0:
                    continue
                edges.append((pid, tname, nm, path, fid, tpid))
                if fid == 0:
                    if tpid not in nodes:
                        stack.append(tpid)
                else:
                    ext_raw.append((pid, tname, nm, path, fid, tpid))
        nodes[pid] = {"type": tname, "name": nm, "byte_size": o.byte_size, **ex}

    ext_by_target = collections.OrderedDict()
    for src_pid, stype, sname, path, fid, tpid in ext_raw:
        key = (fid, tpid)
        e = ext_by_target.setdefault(key, {"file_id": fid, "pathid": tpid, "refs": []})
        e["refs"].append({"from_pathid": src_pid, "from_type": stype,
                          "from_name": sname, "field": path})
    for key, e in ext_by_target.items():
        e.update(ext.info(key[0], key[1]))

    out["tree"] = {
        "nodes": {str(k): v for k, v in nodes.items()},
        "type_counts": dict(collections.Counter(v["type"] for v in nodes.values())),
        "bytes_total": sum(v.get("byte_size") or 0 for v in nodes.values()),
        "bytes_stream": sum(v["stream"][2] for v in nodes.values()
                            if isinstance(v.get("stream"), list) and v["stream"][2]),
        "node_count": len(nodes),
        "edge_count": len(edges),
        "edges": [{"from": e[0], "from_type": e[1], "from_name": e[2], "field": e[3],
                   "file_id": e[4], "pathid": e[5]} for e in edges],
        "external_refs": list(ext_by_target.values()),
    }
    return out


# ----------------------------------------------------------------------------- main

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--stage", default="index", choices=["index", "cab", "tree", "all"])
    ap.add_argument("--bundle", default=BUNDLE)
    ap.add_argument("--json", default=None)
    ap.add_argument("--roots", default="")
    a = ap.parse_args()

    if a.stage == "cab":
        m = build_cab_map(force=True)
        print(f"[cab] 扫了 {m['n_bundles']} 个包，登记 {len(m['cabs'])} 个 CAB 名，"
              f"失败 {len(m['errors'])}；缓存 → {CAB_CACHE}")
        for k, v in list(m["errors"].items())[:5]:
            print("   ERR", k, v)
        return

    env, bf, sf = get_sf(a.bundle)
    print(f"[0] 包 = {a.bundle}")
    print(f"[0] 大小 = {os.path.getsize(a.bundle):,} 字节；Unity {bf.version_engine}；"
          f"内部文件 = {list(bf.files.keys())}")
    out = {"bundle": a.bundle, "bundle_bytes": os.path.getsize(a.bundle),
           "unity_version": bf.version_engine, "serialized_file": list(bf.files.keys())}

    if a.stage in ("index", "all"):
        stage_index(sf, out)
        print(f"[1] 对象总数 = {out['total_objects']:,}")
        print(f"[1] 类型前 15：{json.dumps(dict(list(out['type_histogram'].items())[:15]), ensure_ascii=False)}")
        ab = out["assetbundle"]
        print(f"[1] AssetBundle m_Name={ab['m_Name']!r} PathID={ab['path_id']}")
        print(f"[1]   m_Container = {ab['m_Container']} 条（唯一名 {ab['m_Container_unique_names']}）")
        print(f"[1]   容器目标类型 = {ab['m_Container_targets_type']}；目标缺失 {ab['m_Container_targets_missing']}")
        print(f"[1]   重复名 {len(ab['m_Container_dup_names'])} 个 = {ab['m_Container_dup_names']}")
        print(f"[1]   m_PreloadTable = {ab['m_PreloadTable']:,} 条（唯一 {ab['m_PreloadTable_unique']:,}，"
              f"其中存在于本文件 {ab['m_PreloadTable_unique_existing']:,}）")
        print(f"[1]   m_IsStreamedSceneAssetBundle = {ab['m_IsStreamedSceneAssetBundle']}")
        print(f"[1] GameObject 扫描 {out['gameobjects_total_scanned']:,} 个")
        for nm in WANTED:
            m = out["membership"][nm]
            print(f"[2] {nm!r} → PathID {m['pathids']}")
            print(f"      在容器里: {m['in_container']}   在预加载表里: {m['in_preload']}"
                  f"（被预加载 {m['in_preload_times']} 次）")
            print(f"      容器里含该名的条目: {m['container_name_substr']}")

    if a.stage in ("tree", "all"):
        roots = [int(x) for x in a.roots.split(",") if x.strip()]
        if not roots:
            if "found" not in out:
                stage_index(sf, out)
            for nm in WANTED:
                roots += out["found"].get(nm, [])
        print(f"[3] 根 = {roots}")
        ext = ExtResolver(sf)
        try:
            from UnityPy.helpers.TypeTreeGenerator import TypeTreeGenerator
            gen = TypeTreeGenerator(getattr(bf, "version_engine", "6000.0.0f2"))
            gen.load_local_game(GAME_ROOT)
            env.typetree_generator = gen
            print("[3] TypeTreeGenerator 就绪（load_local_game）")
        except Exception as e:
            print(f"[3] TypeTreeGenerator 失败（MonoBehaviour 会读不全）：{type(e).__name__}: {e}")
        for r in roots:
            sub = stage_tree(sf, [r], ext, {})
            t = sub["tree"]
            nm = t["nodes"][str(r)]["name"]
            print(f"\n[4] 根 {r} ({nm!r})")
            print(f"    节点 {t['node_count']}  边 {t['edge_count']}  "
                  f"内部字节 {t['bytes_total']:,}  外部引用 {len(t['external_refs'])} 处")
            print(f"    类型分布 = {json.dumps(t['type_counts'], ensure_ascii=False)}")
            for e in t["external_refs"]:
                r0 = e["refs"][0]
                print(f"    EXT fid={e['file_id']:<3} pid={e['pathid']:<22} "
                      f"{str(e.get('type')):<14} {str(e.get('name'))[:34]:<34} "
                      f"{str(e.get('disk_bundle'))[:44]:<44} 引 {len(e['refs'])} 次 "
                      f"[{r0['field']} @ {r0['from_name'] or r0['from_type']}]")
            out.setdefault("trees", {})[str(r)] = t

    if a.json:
        with open(a.json, "w", encoding="utf-8") as f:
            json.dump(out, f, ensure_ascii=False, indent=1)
        print(f"\n[9] JSON → {a.json}")


if __name__ == "__main__":
    main()
