#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""A236 · 全库扫描：同一容器内两个 CAB 存在同名 pathID（「撞号」）

背景（为什么要有这个脚本）
--------------------------
`d:/2/Warpforge_tools/scripts/{fix_exports,extract_full}.py` 是 `d:/2/新解包资源/assets_full/`
这棵导出树的**两个根因脚本**。它们都按「一个容器 = 一套 pathID 命名空间」写：

  * `fix_exports.py:113-121`  `outdir/…/<m_Name>.<ext>`        —— 不看 CAB，同名/同 pid 都直接写
  * `extract_full.py:195-197` `…/<name>.json`，撞了就 bump 一次 `_<path_id>`

而**同一个 bundle 里可以有两份 CAB**（主 CAB + `<CAB>.sharedAssets`），它们**各有各的
pathID 命名空间**（`PPtr(m_FileID=0, …)` 只在「引用方自己那一份」里解析）。于是
「pid 1」在第一份里是 `PreloadData`、在另一份里是 `GameObject` —— 撞号**本身不是错**，
错的是**任何按裸 pid 去找对象的代码**（A173 就是栽在这里）。

本脚本只做一件事：**把「同一容器内两份 CAB 有共同 pathID」的实例全库列出来**，
并顺带答一句「现有导出树里那一条取的是哪一份 / 有没有互相覆盖」。
只读，不写导出树、不写 `d:/2/`。

用法
----
    PY = "d:/2/Warpforge_tools/py312/python.exe"
    $PY d:/4/Unity/工具/a236_scan_dual_cab_pid_collision.py \
        --json /tmp/a236.json --md /tmp/a236.md --jobs 8

  --jobs N    并行进程数（纯 python，与 Unity 无关；默认 1）
  --limit N   只扫前 N 个容器（调试用）
  --no-names  不算 name / 不查导出树（只数撞号；快很多）

判据口径
--------
* 容器集合 = **与两个根因脚本的 `load_files()` 完全相同**：`Warpforge_Data/` 根下
  `.assets`/`.resource`/`globalgamemanagers*`/`level0`/`level1`/`sharedassets*`
  ＋ `StreamingAssets/aa/StandaloneWindows64/*.bundle`。⛔ 不是按文件名瞎猜。
* 「CAB」= 容器内的一份 `SerializedFile`。bundle 里读 `env.file.files`；
  独立 `.assets` 自己就是一份（UnityPy 里 `env.files.values()` 直接是 `SerializedFile`）。
* 「撞号」= 两份 CAB 的 `sf.objects` 键（= pathID）**交集非空**。
"""
import argparse
import json
import os
import re
import sys
import time
import traceback
from concurrent.futures import ProcessPoolExecutor

GAME = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data"
BUNDLES = os.path.join(GAME, "StreamingAssets", "aa", "StandaloneWindows64")
EXPORT_ROOT = r"d:/2/新解包资源/assets_full"

SAFE = re.compile(r'[\\/:*?"<>|\x00-\x1f]')
# extract_full.py:179 —— 只有这几类会去读 m_Name
NAMED_TYPES = ("MonoBehaviour", "GameObject", "Sprite", "Texture2D", "AudioClip", "Mesh")
# fix_exports.py:110 / :122-185 —— 这些类型在各自主分支里落盘
FIX_EXPORTS_EXT = {
    "Texture2D": ".png",
    "Mesh": ".obj",
    "Font": ".ttf",
    "VideoClip": ".mp4",
    "TextAsset": ".txt",
}
FIX_EXPORTS_TYPES = set(FIX_EXPORTS_EXT) | {"AudioClip"}


# --------------------------------------------------------------------------- #
# 容器枚举（镜像 fix_exports.load_files / extract_full.load_files）
# --------------------------------------------------------------------------- #
def list_containers():
    out = []
    if os.path.isdir(GAME):
        for fn in sorted(os.listdir(GAME)):
            if fn.endswith((".assets", ".resource")) or fn.startswith(
                ("globalgamemanagers", "level0", "level1", "sharedassets")
            ):
                out.append((os.path.join(GAME, fn), fn))
    if os.path.isdir(BUNDLES):
        for fn in sorted(os.listdir(BUNDLES)):
            if fn.endswith(".bundle"):
                out.append((os.path.join(BUNDLES, fn), "bundle_" + fn))
    return out


def iter_sfs(env):
    """Yield (cab_label, SerializedFile) —— bundle 的内层 CAB / 独立文件自身。

    ⚠️ 坑：`SerializedFile.files` 是个 **property**，等价于它自己的 `.objects`
    （`UnityPy/files/SerializedFile.py:225-232`）⇒ 不能靠 `hasattr(f,'files')`
    区分「包」和「独立文件」，必须按类型判。
    """
    from UnityPy.files import BundleFile, SerializedFile

    for top in env.files.values():
        if isinstance(top, SerializedFile):
            yield "<standalone>", top
        elif isinstance(top, BundleFile):
            for cab, inner in top.files.items():
                if isinstance(inner, SerializedFile):
                    yield cab, inner
        else:
            # WebBundle 之类：兜底再找一层
            for cab, inner in (getattr(top, "files", {}) or {}).items():
                if isinstance(inner, SerializedFile):
                    yield cab, inner


def name_of(obj):
    """尽量取 m_Name（失败不抛，返回 None）。"""
    try:
        d = obj.read()
        return getattr(d, "m_Name", None)
    except Exception:
        return None


def safe_name(name, fallback):
    """extract_full.safe_name"""
    name = SAFE.sub("_", (name or "").strip())
    return name[:120] or fallback


def fix_exports_name(name, tn, pid):
    """fix_exports.py:119 的 fname 算法"""
    return re.sub(r'[\\/:*?"<>|\x00-\x1f]', "_", (name or "")).strip()[:120] or f"{tn}_{pid}"


def export_candidates(stem, tn, pid, name):
    """两个脚本会给这个对象算出哪些**落盘路径**（相对 EXPORT_ROOT）。

    返回候选相对路径列表（按两个脚本的先后顺序）。
    """
    cands = []
    if tn in FIX_EXPORTS_TYPES:
        fn = fix_exports_name(name, tn, pid)
        if tn == "AudioClip":
            cands += [f"{stem}/{tn}/{fn}{e}" for e in (".ogg", ".wav", ".vorbis", ".bin")]
        else:
            cands.append(f"{stem}/{tn}/{fn}{FIX_EXPORTS_EXT[tn]}")
    else:
        # extract_full.py:184-197 的 JSON 兜底
        nm = name if tn in NAMED_TYPES else None
        fn = safe_name(nm, f"{tn}_{pid}")
        cands.append(f"{stem}/{tn}/{fn}.json")
        cands.append(f"{stem}/{tn}/{fn}_{pid}.json")
    return cands


# --------------------------------------------------------------------------- #
# 「现有导出取的是哪一份」—— 内容级判定（不是靠 os.path.exists，
# Windows 大小写不敏感会让 Text.json / text.json 这种假阳性满天飞）
# --------------------------------------------------------------------------- #
def _json_text(tt):
    """与 extract_full.py:199 逐字相同的一次序列化。"""
    return json.dumps(tt, ensure_ascii=False, indent=1, default=json_default)


def json_default(o):
    """extract_full.py:149-152"""
    import base64

    if isinstance(o, bytes):
        return {"$bytes": base64.b64encode(o).decode("ascii")}
    return str(o)


def _read_norm(path):
    """读盘 + 归一化行尾（extract_full 用文本模式 `open(...,'w')` ⇒ Windows 上落了 CRLF）"""
    with open(path, "rb") as f:
        return f.read().replace(b"\r\n", b"\n")


def dir_index(root):
    """{目录: {小写文件名: 真实文件名}} —— 用来在大小写不敏感的盘上拿到真名。"""
    idx = {}
    if not os.path.isdir(root):
        return idx
    for dirpath, _dirs, files in os.walk(root):
        idx[os.path.normcase(dirpath)] = {f.lower(): f for f in files}
    return idx


def _md5(b):
    import hashlib

    return hashlib.md5(b).hexdigest()


def _resolve(idx, rel):
    """把「脚本会写的相对路径」解析成盘上的真名文件（找不到 → None）。"""
    p = os.path.join(EXPORT_ROOT, rel.replace("/", os.sep))
    d = idx.get(os.path.normcase(os.path.dirname(p))) or {}
    real = d.get(os.path.basename(p).lower())
    return os.path.join(os.path.dirname(p), real) if real else None


def _content_md5(obj, tn):
    """这个对象**如果被导出**，盘上内容该是什么 md5？

    * JSON 类（extract_full）：与 `json.dump(tt, f, ensure_ascii=False, indent=1,
      default=json_default)` 逐字相同的文本。盘上是 CRLF（文本模式 open 的换行翻译），
      所以**两边都归一到 LF 再算**（`_read_norm`）—— 实测 `Material_1.json` 归一后逐字节相同。
    * 二进制类（fix_exports：png/obj/ttf/mp4/txt/ogg…）：编码过程不可重放
      （PIL / unity 导出器 / 我们另写的音频解码），**返回 None = 本脚本不做内容判定**。
    """
    if tn in FIX_EXPORTS_TYPES:
        return None
    try:
        tt = obj.read_typetree()
        return _md5(_json_text(tt).encode("utf-8"))
    except Exception:
        return None


def replay_names(env, stem):
    """按两个根因脚本的**真实写盘顺序**重放文件名分配，返回「被 ≥2 个对象写到同一路径」的组。

    * 顺序 = `env.objects`（两个脚本都是 `list(env.objects)` / 直接 for 它）。
      UnityPy 的 `Environment.objects` 对包是「按 `bf.files` 序、每份 CAB 内按 pid 序」。
    * 规则逐条镜像（`d:/2/Warpforge_tools/scripts/`）：
        - `fix_exports.py:110` 只处理 6 类；`:119` fname = 净化后的 m_Name（空 → `Type_pid`）；
          `:122-185` 各分支**无条件写**、**没有冲突处理**。
        - `extract_full.py:179-184` 只有 6 类读 m_Name，其余用 `Type_pid`；
          `:195-197` 撞了**只 bump 一次**成 `<fname>_<path_id>.json`，**bump 出来的名字不再检查**。
    * ⚠️ 只重放**文件名**（谁占住了哪条路径），不动内容。
    * 基线 = 空输出目录（即「跑第一遍」）。两个脚本互不干扰：`extract_full:192-193`
      对 `Texture2D/Mesh/Font/TextAsset/VideoClip` 直接 `continue`。
    """
    owner = {}   # 归一化路径 -> [ {order,type,name,path_id,cab,path} ]
    order = 0
    for obj in env.objects:
        tn = obj.type.name
        order += 1
        try:
            nm = getattr(obj.read(), "m_Name", None)
        except Exception:
            nm = None
        if tn in FIX_EXPORTS_TYPES:
            fn = fix_exports_name(nm, tn, obj.path_id)
            cands = [] if tn == "AudioClip" else [f"{stem}/{tn}/{fn}{FIX_EXPORTS_EXT[tn]}"]
            final = cands[0] if cands else None
        else:
            nm2 = nm if tn in NAMED_TYPES else None
            fn = safe_name(nm2, f"{tn}_{obj.path_id}")
            p1 = f"{stem}/{tn}/{fn}.json"
            final = p1 if p1.lower() not in owner else f"{stem}/{tn}/{fn}_{obj.path_id}.json"
        if final is None:
            continue
        owner.setdefault(final.lower(), []).append(
            {"order": order, "type": tn, "name": nm, "path_id": obj.path_id,
             "cab": getattr(obj.assets_file, "name", None), "path": final}
        )
    return [
        {"path": v[0]["path"], "n_writers": len(v), "writers": v}
        for v in owner.values()
        if len(v) >= 2
    ]


def _binary_bytes(obj, tn):
    """这个对象**如果被 fix_exports 导出**，盘上内容该是什么字节（None = 该分支不会落盘）。

    逐条镜像 `fix_exports.py:122-185`：
      * Texture2D: `obj.read().image` → `img.save(base+'.png')`（img 为空 ⇒ 不写）
      * Mesh:      `MeshExporter.export_mesh(d, format='obj')` → 文本模式写盘 ⇒ 盘上是 CRLF
      * Font:      `m_FontData`（整数 list）→ bytes
      * TextAsset: `m_Script`（str）→ utf-8/surrogateescape
    AudioClip/VideoClip 不可重放（我们另写的解码 / 流式读取）⇒ None。
    """
    try:
        if tn == "Mesh":
            from UnityPy.export import MeshExporter

            return MeshExporter.export_mesh(obj.read(), format="obj").encode("utf-8")
        if tn == "Texture2D":
            img = obj.read().image
            if img is None:
                return None
            import io as _io

            buf = _io.BytesIO()
            img.save(buf, format="PNG")
            return buf.getvalue()
        if tn == "Font":
            return bytes(obj.read().m_FontData)
        if tn == "TextAsset":
            sc = obj.read().m_Script
            return sc.encode("utf-8", "surrogateescape") if isinstance(sc, str) else bytes(sc)
    except Exception:
        return None
    return None


def verify_overwrites(env, stem, groups, idx):
    """给重放出来的每组「被 ≥2 个对象写」定论：盘上那一份是谁、谁被覆盖了。

    做法 = 把每个写手**重新导出一次**（与 fix_exports 的调用逐条相同），
    再和盘上文件比（行尾归一）。
    """
    for g in groups:
        real = _resolve(idx, g["path"])
        g["on_disk"] = None
        disk = None
        if real:
            try:
                disk = _read_norm(real)
                g["on_disk"] = {"file": os.path.basename(real), "bytes": len(disk),
                                "md5": _md5(disk)}
            except Exception as e:
                g["on_disk"] = {"error": str(e)}
        winners = []
        for w in g["writers"]:
            w["content_md5"] = None
            if disk is None:
                continue
            try:
                obj = _find_obj(env, w)
                b = _binary_bytes(obj, w["type"]) if obj is not None else None
                if b is None:
                    # JSON 类：用 extract_full 的序列化
                    b = _json_text(obj.read_typetree()).encode("utf-8") if obj is not None else None
                if b is None:
                    continue
                h = _md5(_read_norm_bytes(b))
                w["content_md5"] = h
                if h == g["on_disk"]["md5"]:
                    winners.append(w)
            except Exception as e:
                w["content_error"] = str(e)
        g["winners"] = [{k: w[k] for k in ("order", "cab", "type", "name", "path_id")}
                        for w in winners]
        g["losers"] = [{k: w[k] for k in ("order", "cab", "type", "name", "path_id")}
                       for w in g["writers"] if w not in winners]
    return groups


def _read_norm_bytes(b):
    return b.replace(b"\r\n", b"\n")


def _find_obj(env, w):
    for obj in env.objects:
        if obj.path_id == w["path_id"] and \
                getattr(obj.assets_file, "name", None) == w["cab"]:
            return obj
    return None


def scan_one(args):
    path, short = args
    stem = os.path.splitext(short)[0]
    rec = {
        "container": short,
        "path": path,
        "type": None,
        "cabs": [],
        "n_cab": 0,
        "n_objects_total": 0,
        "collisions": [],
        "error": None,
    }
    try:
        import UnityPy

        t0 = time.time()
        env = UnityPy.load(path)
        rec["type"] = type(env.file).__name__
        sfs = list(iter_sfs(env))
        rec["load_secs"] = round(time.time() - t0, 2)
        rec["cabs"] = [{"cab": cab, "n_objects": len(sf.objects or {})} for cab, sf in sfs]
        rec["n_cab"] = len(sfs)
        rec["n_objects_total"] = sum(len(sf.objects or {}) for _, sf in sfs)
        if _ARGS.get("replay"):
            t1 = time.time()
            ow = replay_names(env, stem)
            if _ARGS.get("verify") and ow:
                ow = verify_overwrites(env, stem, ow,
                                       dir_index(os.path.join(EXPORT_ROOT, stem)))
            rec["overwrites"] = ow
            rec["replay_secs"] = round(time.time() - t1, 2)
        if len(sfs) < 2:
            return rec

        pidsets = [(cab, set((sf.objects or {}).keys())) for cab, sf in sfs]
        for i in range(len(pidsets)):
            for j in range(i + 1, len(pidsets)):
                cab_a, set_a = pidsets[i]
                cab_b, set_b = pidsets[j]
                inter = set_a & set_b
                if not inter:
                    rec["collisions"].append(
                        {"cab_a": cab_a, "cab_b": cab_b, "n_collide": 0, "items": []}
                    )
                    continue
                items = []
                idx = dir_index(os.path.join(EXPORT_ROOT, stem)) if _ARGS["verify"] else {}
                for pid in sorted(inter):
                    oa = sfs[i][1].objects[pid]
                    ob = sfs[j][1].objects[pid]
                    ta, tb = oa.type.name, ob.type.name
                    if _ARGS["no_names"]:
                        items.append({"pid": pid, "a": {"type": ta}, "b": {"type": tb}})
                        continue
                    na, nb = name_of(oa), name_of(ob)
                    ca = export_candidates(stem, ta, pid, na)
                    cb = export_candidates(stem, tb, pid, nb)
                    # 两边的候选路径是否**互相覆盖**（Windows 大小写不敏感 ⇒ 一律 lower 比）
                    la = {c.lower() for c in ca}
                    lb = {c.lower() for c in cb}
                    overlap = sorted(la & lb)

                    side_a = {"cab": cab_a, "type": ta, "name": na, "cands": ca}
                    side_b = {"cab": cab_b, "type": tb, "name": nb, "cands": cb}

                    # ---- 内容级「盘上这一份是谁」 ----
                    if _ARGS["verify"]:
                        ha = _content_md5(oa, ta)
                        hb = _content_md5(ob, tb)
                        side_a["content_md5"] = ha
                        side_b["content_md5"] = hb
                        found = {}
                        for tag, side in (("A", side_a), ("B", side_b)):
                            for c in side["cands"]:
                                real = _resolve(idx, c)
                                if not real:
                                    continue
                                try:
                                    dg = _md5(_read_norm(real))
                                except Exception as e:
                                    found[f"{tag}:{c}"] = f"<read err {e}>"
                                    continue
                                who = []
                                if ha and dg == ha:
                                    who.append("A")
                                if hb and dg == hb:
                                    who.append("B")
                                found[f"{tag}:{c}"] = (
                                    f"{os.path.basename(real)} md5={dg[:12]}"
                                    f" => {'/'.join(who) if who else '既不是 A 也不是 B'}"
                                )
                        for c in ca + cb:
                            p = os.path.join(EXPORT_ROOT, c.replace("/", os.sep))
                            side_a.setdefault("export_exists", {})
                            if c not in side_a["export_exists"]:
                                side_a["export_exists"][c] = os.path.exists(p)
                        side_a["on_disk"] = found

                    items.append(
                        {
                            "pid": pid,
                            "a": side_a,
                            "b": side_b,
                            "same_output_path": overlap,
                        }
                    )
                rec["collisions"].append(
                    {"cab_a": cab_a, "cab_b": cab_b, "n_collide": len(inter), "items": items}
                )
        return rec
    except Exception as e:
        rec["error"] = f"{type(e).__name__}: {e}"
        rec["trace"] = traceback.format_exc()[-1200:]
        return rec


_ARGS = {"no_names": False, "verify": False, "replay": False}


def _init(no_names, verify=False, replay=False):
    _ARGS["no_names"] = no_names
    _ARGS["verify"] = verify
    _ARGS["replay"] = replay


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--json", default=None, help="明细 JSON 输出路径")
    ap.add_argument("--md", default=None, help="人读 Markdown 输出路径")
    ap.add_argument("--jobs", type=int, default=1)
    ap.add_argument("--limit", type=int, default=0)
    ap.add_argument("--no-names", action="store_true")
    ap.add_argument("--verify", action="store_true",
                    help="内容级判「盘上那一份是 A 还是 B」（JSON 类；二进制类不做）")
    ap.add_argument("--replay", action="store_true",
                    help="按两个脚本的真实写盘顺序重放文件名分配，找「被 >=2 个对象写到同一路径」（慢）")
    ap.add_argument("--only", default=None, help="只扫名字里含这个子串的容器")
    a = ap.parse_args()

    conts = list_containers()
    if a.only:
        conts = [c for c in conts if a.only in c[1]]
    if a.limit:
        conts = conts[: a.limit]
    print(f"# 容器数 = {len(conts)} （根目录 {GAME} + 包目录 {BUNDLES}）", flush=True)
    for _p, s in conts:
        print(f"#   {s}", flush=True)
    print("#", flush=True)

    t0 = time.time()
    recs = []
    if a.jobs > 1:
        with ProcessPoolExecutor(max_workers=a.jobs, initializer=_init,
                                 initargs=(a.no_names, a.verify, a.replay)) as ex:
            for k, r in enumerate(ex.map(scan_one, conts)):
                recs.append(r)
                print(f"[{k+1}/{len(conts)}] {r['container']}: cabs={r['n_cab']} "
                      f"objs={r['n_objects_total']} ow={len(r.get('overwrites') or [])} "
                      f"err={r['error']}", flush=True)
    else:
        _init(a.no_names, a.verify, a.replay)
        for k, c in enumerate(conts):
            r = scan_one(c)
            recs.append(r)
            print(f"[{k+1}/{len(conts)}] {r['container']}: cabs={r['n_cab']} "
                  f"objs={r['n_objects_total']} ow={len(r.get('overwrites') or [])} "
                  f"err={r['error']}", flush=True)
    print(f"# 扫描耗时 {time.time()-t0:.1f}s", flush=True)

    # ---------------- 汇总 ----------------
    multi = [r for r in recs if r["n_cab"] >= 2]
    withcoll = [r for r in multi if any(c["n_collide"] for c in r["collisions"])]
    tot_pairs = sum(len(r["collisions"]) for r in multi)
    tot_collide = sum(c["n_collide"] for r in multi for c in r["collisions"])
    tot_overlap = sum(
        len(it["same_output_path"]) > 0
        for r in multi
        for c in r["collisions"]
        for it in c["items"]
    )
    print(f"# 多 CAB 容器 {len(multi)} / {len(recs)}", flush=True)
    print(f"# 有撞号的容器 {len(withcoll)}；CAB 对数 {tot_pairs}；撞号 pid 合计 {tot_collide}", flush=True)
    print(f"# 其中【两边算出的落盘路径相同】的实例 {tot_overlap}", flush=True)
    if a.replay:
        tot_ow = sum(len(r.get("overwrites") or []) for r in recs)
        cont_ow = sum(1 for r in recs if r.get("overwrites"))
        print(f"# 名字级重放：{cont_ow} 个容器里有 {tot_ow} 条路径被 >=2 个对象写", flush=True)

    if a.json:
        os.makedirs(os.path.dirname(a.json), exist_ok=True)
        with open(a.json, "w", encoding="utf-8") as f:
            json.dump(
                {
                    "game": GAME,
                    "bundles": BUNDLES,
                    "export_root": EXPORT_ROOT,
                    "n_containers": len(recs),
                    "records": recs,
                },
                f,
                ensure_ascii=False,
                indent=1,
            )
        print(f"# JSON -> {a.json}", flush=True)

    if a.md:
        os.makedirs(os.path.dirname(a.md), exist_ok=True)
        with open(a.md, "w", encoding="utf-8") as f:
            f.write(f"# A236 全库扫描结果（{time.strftime('%Y-%m-%d %H:%M')}）\n\n")
            f.write(f"* 容器 {len(recs)} 个（{GAME} 根 + {BUNDLES}）\n")
            f.write(f"* 多 CAB 容器 **{len(multi)}** 个；其中**有 pid 撞号**的 **{len(withcoll)}** 个\n")
            f.write(f"* 撞号 pid 实例合计 **{tot_collide}**；两边落盘路径相同的 **{tot_overlap}**\n\n")

            # ---- 🔴 真丢件：同名 + 同 pid ⇒ 两边算出的落盘路径是同一个文件 ----
            f.write("\n## 🔴 真丢件（两边落盘路径相同 ⇒ 后写静默覆盖先写）\n\n")
            bad = []
            for r in recs:
                for c in r.get("collisions", []):
                    for it in c["items"]:
                        if not it.get("same_output_path"):
                            continue
                        A, B = it["a"], it["b"]
                        od = A.get("on_disk") or {}
                        # 盘上那一份到底是谁：取对 A 的候选路径的判定
                        verdicts = {k.split(":", 1)[0]: v for k, v in od.items()}
                        bad.append((r["container"], c, it, verdicts))
            if not bad:
                f.write("（无）\n")
            for cont, c, it, vd in bad:
                f.write(f"\n### `{cont}`  ·  pathID `{it['pid']}`\n\n")
                for tag, side in (("A", it["a"]), ("B", it["b"])):
                    f.write(f"* **{tag}** `{side['cab']}` → {side['type']} `{side.get('name')}`"
                            f"  ⇒ {side['cands']}\n")
                f.write(f"* 共同落盘路径：`{', '.join(it['same_output_path'])}`\n")
                for k, v in (vd or {}).items():
                    f.write(f"* 盘上（{k} 的候选）→ {v}\n")

            # ---- 名字级覆盖重放（--replay）----
            if a.replay:
                f.write("\n\n## 名字级覆盖重放（按两个脚本的真实写盘顺序）\n\n")
                tot_ow = 0
                for r in recs:
                    ow = r.get("overwrites") or []
                    if not ow:
                        continue
                    tot_ow += len(ow)
                    f.write(f"\n### `{r['container']}` —— **{len(ow)}** 条路径被 >=2 个对象写\n\n")
                    for g in ow:
                        f.write(f"* `{g['path']}`（{g['n_writers']} 个写手）")
                        if g.get("on_disk"):
                            f.write(f" —— 盘上 `{g['on_disk'].get('file')}` "
                                    f"{g['on_disk'].get('bytes')} B "
                                    f"md5={str(g['on_disk'].get('md5'))[:12]}")
                        f.write("\n")
                        win = {(w["cab"], w["path_id"]) for w in (g.get("winners") or [])}
                        for w in g["writers"][:8]:
                            if not win:
                                mark = ""
                            else:
                                mark = ("✅ 盘上这一份" if (w["cab"], w["path_id"]) in win
                                        else "🔴 被覆盖（内容已不在盘上）")
                            f.write(f"    * #{w['order']} `{w['cab']}` pid={w['path_id']} "
                                    f"{w['type']} `{w['name']}` {mark}\n")
                        if g["n_writers"] > 8:
                            f.write(f"    * … 另 {g['n_writers']-8} 个\n")
                f.write(f"\n**合计 {tot_ow} 条路径被 >=2 个对象写**\n\n")
                n_w = sum(1 for r in recs for g in (r.get("overwrites") or []) if g.get("winners"))
                n_l = sum(len(g.get("losers") or []) for r in recs for g in (r.get("overwrites") or []))
                f.write(f"其中**已内容定论** {n_w} 条路径；**判定被覆盖丢掉的对象 {n_l} 个**\n\n")

            f.write("\n\n## 逐容器明细\n")
            for r in recs:
                if r["n_cab"] < 2 and not r["error"]:
                    continue
                f.write(f"\n## `{r['container']}`  ({r['type']}, {r['n_cab']} CAB, "
                        f"{r['n_objects_total']} objects)\n")
                if r["error"]:
                    f.write(f"\n🔴 LOAD ERROR: {r['error']}\n")
                    continue
                for c in r["cabs"]:
                    f.write(f"- CAB `{c['cab']}`: {c['n_objects']} objects\n")
                for c in r["collisions"]:
                    f.write(f"\n### `{c['cab_a']}`  ×  `{c['cab_b']}`  —— 撞号 {c['n_collide']}\n\n")
                    if not c["items"]:
                        continue
                    f.write("| pathID | A: 类型 / 名字（CAB） | B: 类型 / 名字（CAB） | 两边落盘路径相同? | 盘上这一份是谁 |\n")
                    f.write("|---|---|---|---|---|\n")
                    for it in c["items"]:
                        A, B = it["a"], it["b"]
                        sa = f"{A['type']} / `{A.get('name')}`"
                        sb = f"{B['type']} / `{B.get('name')}`"
                        ov = "<br>".join(it.get("same_output_path") or []) or "—"
                        od = A.get("on_disk") or {}
                        ex = "<br>".join(f"`{k}` → {v}" for k, v in od.items()) or "—"
                        f.write(f"| {it['pid']} | {sa} | {sb} | {ov} | {ex} |\n")
        print(f"# MD -> {a.md}", flush=True)

    return 0


if __name__ == "__main__":
    sys.exit(main())
