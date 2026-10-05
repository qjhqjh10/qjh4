#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""A236 丢件回收 —— 把「同名撞车被覆盖掉、内容已不在盘上」的对象**按 pid 补回**导出树。

为什么要有这个脚本
------------------
`d:/2/新解包资源/assets_full/` 那棵树由两个根因脚本写：
  * `fix_exports.py:123-146`   `…/<Type>/<m_Name>.<ext>` —— **没有冲突处理**
  * `extract_full.py:195-197`  `…/<Type>/<m_Name>.json`，撞了只 bump 一次 `_<path_id>`，**bump 出来的名字不再检查**
⇒ 同一容器里两个对象同名时，**后写的静默盖掉先写的**，先写那一份**内容从盘上消失**。
（全库扫出来的 21 条路径 / 17 个对象见 `资料/普查产出_1011/W2_子6_工具与d2.md` 的核算表。）

本脚本**不改那两个根因脚本**（那是 A236 的账，得调度台裁），只做**局部、可回滚的数据回收**：
把丢掉的每一个对象用**与 fix_exports / extract_full 逐条相同的调用**重新导出一次，写到
**旁边一个新名字** `<m_Name>_<path_id>.<ext>`（**只新增，永不覆盖任何既有文件**）。

🔴 命名口径：带上 `path_id` 是**这棵树里已经在用的口径**（`extract_full` 的 bump 产物就叫
`Material_1.json` / `Font_52.json`），不是新发明；带 pid 之后「一个文件 = 一个对象」重新成立。

回滚
----
本脚本只创建文件、从不删改。回滚 = 按本脚本打印/写出的清单把那些新文件删掉：
    dir /b /s <清单里的 path>   ← 或直接读 `--json` 产物的 `created[].dst`。
`--json` 产物里逐条记着 `dst / bytes / md5 / container / cab / path_id`。

用法
----
    PY="d:/2/Warpforge_tools/py312/python.exe"
    $PY d:/4/Unity/工具/a236_restore_lost_exports.py                    # 干跑（默认，不写盘）
    $PY d:/4/Unity/工具/a236_restore_lost_exports.py --apply \
        --json d:/4/_tmp_view/a236_restore.json

    --apply        真写（默认只算不写）
    --only <子串>  只处理容器名里含这个子串的（调试用）
    --json <路径>  把逐条结果写成 JSON
"""
import argparse
import hashlib
import importlib.util
import io
import json
import os
import sys
import time

HERE = os.path.dirname(os.path.abspath(__file__))
SCANNER = os.path.join(HERE, "a236_scan_dual_cab_pid_collision.py")


def load_scanner():
    """按路径 import 那个扫描器（复用它的容器枚举 / 重放 / 内容重导出，口径只此一处）。"""
    spec = importlib.util.spec_from_file_location("a236_scanner", SCANNER)
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


SC = load_scanner()
EXPORT_ROOT = SC.EXPORT_ROOT

# 两个根因脚本里「哪个对象由哪个脚本落盘」—— 逐条照抄（fix_exports.py:120 / extract_full.py:192-193）
FIX_EXPORTS_TYPES = set(SC.FIX_EXPORTS_TYPES)          # Texture2D / Mesh / Font / TextAsset / VideoClip / AudioClip


def payload_of(obj):
    """这一个对象**如果被原脚本导出**，盘上该长成什么（返回 (bytes, ext) 或 (None, 原因)）。

    逐条镜像两个根因脚本的真实写法 —— 差别只在**写出方式**：
      * Texture2D  `fix_exports.py:133-135`  `img.save(base+'.png')`
      * Mesh       `fix_exports.py:141-143`  `open(base+'.obj','w',encoding='utf-8')` + `write(s)`  ⇒ 文本模式 = CRLF
      * GameObject 等 `extract_full.py:196-198` `open(jpath,'w',encoding='utf-8')` + `json.dump(tt, f, ensure_ascii=False, indent=1, default=json_default)`
    ⇒ 这里把「文本」和「二进制」分开返回，写盘时用**同样的 open 模式**，字节天然一致。
    """
    tn = obj.type.name
    if tn == "Texture2D":
        img = obj.read().image
        if img is None:
            return None, "Texture2D.image 为空（0×0 空纹理那类）"
        buf = io.BytesIO()
        img.save(buf, format="PNG")
        return ("bin", buf.getvalue(), ".png"), None
    if tn == "Mesh":
        from UnityPy.export import MeshExporter
        s = MeshExporter.export_mesh(obj.read(), format="obj")
        if not s:
            return None, "MeshExporter 返回空"
        return ("text", s, ".obj"), None
    if tn in FIX_EXPORTS_TYPES:                    # Font / TextAsset / VideoClip / AudioClip
        return None, "属 fix_exports 的 %s 分支，本脚本不重放（A234 已修的是 Font/TextAsset）" % tn
    # 其余一律按 `extract_full.py:194-198` 的兜底：typetree JSON
    try:
        tt = obj.read_typetree()
    except Exception as e:
        return None, "read_typetree 失败：%s" % str(e)[:60]
    s = json.dumps(tt, ensure_ascii=False, indent=1, default=SC.json_default)
    return ("text", s, ".json"), None


def free_name(cls_dir, fname, path_id, ext):
    """`<fname>_<pid><ext>`；被占（`os.path.exists` 在 Windows 上大小写不敏感）就再挂一层 pid，直到空位。"""
    cand = os.path.join(cls_dir, "%s_%d%s" % (fname, path_id, ext))
    while os.path.exists(cand):
        cand = cand[:-len(ext)] + "_%d%s" % (path_id, ext)
    return cand


def write_and_verify(payload, dst):
    """先算后写（纪律：`open(...,'wb')` 会先截断再求值）、写完**回读逐字节比**。"""
    kind, data, _ext = payload
    if os.path.exists(dst):
        return False, "目标已存在（拒绝覆盖）"
    if kind == "bin":
        with open(dst, "wb") as f:
            f.write(data)
        back = io.open(dst, "rb").read()
        if back != data:
            return False, "回读不一致"
        return True, hashlib.md5(back).hexdigest()[:12]
    else:
        # 与原脚本**同一个 open 模式**（文本模式 ⇒ Windows 上 \n 变 \r\n，与既有 .obj/.json 一致）
        with open(dst, "w", encoding="utf-8") as f:
            f.write(data)
        back = io.open(dst, "rb").read()
        if back != data.encode("utf-8").replace(b"\n", b"\r\n") and back != data.encode("utf-8"):
            return False, "回读不一致"
        return True, hashlib.md5(back).hexdigest()[:12]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--apply", action="store_true", help="真写（默认只算不写）")
    ap.add_argument("--only", default=None, help="只处理容器名含这个子串的")
    ap.add_argument("--json", default=None, help="逐条结果写这里")
    ap.add_argument("--max", type=int, default=0, help="最多写 N 个（0=不限）")
    a = ap.parse_args()

    result = {"export_root": EXPORT_ROOT, "apply": bool(a.apply), "created": [],
              "skipped": [], "no_disk_file": [], "scanned": 0}
    t0 = time.time()
    n_write = 0
    for path, short in SC.list_containers():
        stem = os.path.splitext(short)[0]
        if a.only and a.only not in stem:
            continue
        try:
            import UnityPy
            env = UnityPy.load(path)
            groups = SC.replay_names(env, stem)
            if groups:
                SC.verify_overwrites(env, stem, groups, SC.dir_index(os.path.join(EXPORT_ROOT, stem)))
        except Exception as e:
            result["skipped"].append({"container": short, "why": "load/replay 失败: %s" % str(e)[:80]})
            continue
        result["scanned"] += 1
        for g in groups:
            if g.get("on_disk") is None:
                # 盘上根本没有这个文件（=「3 个写手一个都没产出」那类，根因不同：见 A174/庚1 §④·5）
                result["no_disk_file"].append(
                    {"path": g["path"], "n_writers": g["n_writers"],
                     "writers": [[w["type"], w["name"], w["path_id"]] for w in g["writers"]]})
                continue
            won = {(w["cab"], w["path_id"]) for w in (g.get("winners") or [])}
            for w in g["writers"]:
                if (w["cab"], w["path_id"]) in won:
                    continue
                obj = SC._find_obj(env, w)
                if obj is None:
                    result["skipped"].append({"path": g["path"], "path_id": w["path_id"],
                                              "why": "找不到对象"})
                    continue
                payload, why = payload_of(obj)
                if payload is None:
                    result["skipped"].append({"path": g["path"], "path_id": w["path_id"],
                                              "type": w["type"], "name": w["name"], "why": why})
                    continue
                _kind, data, ext = payload
                cls_dir = os.path.join(EXPORT_ROOT, stem, w["type"])
                fname = SC.safe_name(w["name"], "%s_%d" % (w["type"], w["path_id"]))
                dst = free_name(cls_dir, fname, w["path_id"], ext)
                size = len(data) if isinstance(data, bytes) else len(data.encode("utf-8"))
                rec = {"container": short, "cab": w["cab"], "type": w["type"], "name": w["name"],
                       "path_id": w["path_id"], "dst": dst.replace("\\", "/"), "bytes": size}
                if a.apply and (not a.max or n_write < a.max):
                    os.makedirs(cls_dir, exist_ok=True)
                    ok, info = write_and_verify(payload, dst)
                    rec["ok"] = ok
                    rec["md5"] = info if ok else None
                    if not ok:
                        rec["why"] = info
                    n_write += 1
                result["created"].append(rec)

    dt = time.time() - t0
    print("扫过容器 %d 个；可回收对象 %d 个；容器级跳过 %d；盘上无文件的组 %d  [%.1fs]"
          % (result["scanned"], len(result["created"]), len(result["skipped"]),
             len(result["no_disk_file"]), dt))
    for r in result["created"]:
        print("  %-8s %-52s pid=%-22s %8d B -> %s"
              % ("写入" if a.apply else "干跑", r["name"], r["path_id"], r["bytes"],
                 os.path.relpath(r["dst"], EXPORT_ROOT).replace("\\", "/")))
    for r in result["skipped"]:
        print("  ⏭ 跳过 %s pid=%s：%s" % (r.get("name"), r.get("path_id"), r["why"]))
    for r in result["no_disk_file"]:
        print("  ⚠️ 盘上无文件（根因不同，本脚本不碰）：%s（%d 个写手）" % (r["path"], r["n_writers"]))
    if a.json:
        with io.open(a.json, "w", encoding="utf-8") as f:
            json.dump(result, f, ensure_ascii=False, indent=1)
        print("JSON -> %s" % a.json)
    if not a.apply:
        print("⚠️ 这是**干跑**，一个字节都没写。要真写加 `--apply`。")
    return 0


if __name__ == "__main__":
    sys.exit(main())
