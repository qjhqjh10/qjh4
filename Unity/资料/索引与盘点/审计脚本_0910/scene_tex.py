# -*- coding: utf-8 -*-
"""审计 4：每个场景实际引用的贴图 vs 场景 Texture2D/ 目录里有的贴图。

复用 unity_scene_to_godot.Assembler（走原始 bundle 解析 pathID→对象名）。
输出: 每个场景的 需要/缺失/命中兜底 清单。
"""
import os, sys, json, collections, io

sys.stdout.reconfigure(encoding="utf-8")
SCRIPTS = r"d:/2/Warpforge_tools/scripts"
sys.path.insert(0, SCRIPTS)
os.chdir(SCRIPTS)

import unity_scene_to_godot as U   # noqa

ROOT = r"d:/2/解包整理"
SCENES_DIR = os.path.join(ROOT, "07_场景")
OUT = r"d:/2/Warpforge_tools/tmp/audit_0910"

scenes = sorted(d for d in os.listdir(SCENES_DIR) if os.path.isdir(os.path.join(SCENES_DIR, d)))
result = {}
for sc in scenes:
    buf = io.StringIO()
    old = sys.stdout
    sys.stdout = buf
    try:
        a = U.Assembler(sc, os.path.join(OUT, "_godot_dummy", sc))
    except SystemExit as e:
        sys.stdout = old
        print(f"[SKIP] {sc}: {e}")
        continue
    except Exception as e:
        sys.stdout = old
        print(f"[ERR ] {sc}: {type(e).__name__}: {e}")
        continue
    finally:
        sys.stdout = old

    need = {}     # tex_name -> [go names]
    # 网格
    for t in sorted(a.TF):
        gopid = a.TF[t].get("m_GameObject", {}).get("m_PathID")
        mats = a.mats_of(gopid)
        if not mats:
            continue
        try:
            gname = U.go_name_of(a, t)
        except Exception:
            gname = "?"
        for mi in mats:
            for tn in (mi.tex_name, mi.tex_name2):
                if tn:
                    need.setdefault(tn, set()).add(gname)
    # 粒子
    for t in sorted(a.TF):
        gopid = a.TF[t].get("m_GameObject", {}).get("m_PathID")
        res = a.ps_of(gopid)
        if not res or res[0] is None:
            continue
        mats = res[3]
        try:
            gname = U.go_name_of(a, t)
        except Exception:
            gname = "?"
        for mi in mats:
            for tn in (mi.tex_name, mi.tex_name2):
                if tn:
                    need.setdefault(tn, set()).add(gname)

    local_dir = os.path.join(SCENES_DIR, sc, "Texture2D")
    local = set()
    if os.path.isdir(local_dir):
        local = {n.lower() for n in os.listdir(local_dir)}
    have = {n: (n.lower() + ".png") in local for n in need}
    missing = sorted(n for n, ok in have.items() if not ok)
    present = sorted(n for n, ok in have.items() if ok)

    # 全树兜底搜索
    result[sc] = {"need": len(need), "present": present, "missing": missing,
                  "used_by": {k: sorted(v) for k, v in need.items()}}
    print(f"[{sc}] 引用贴图 {len(need)}  场景内有 {len(present)}  缺 {len(missing)}")
    if missing:
        for m in missing:
            print(f"     缺: {m}   (使用者: {sorted(need[m])[:4]})")

json.dump(result, open(os.path.join(OUT, "scene_tex.json"), "w", encoding="utf-8"),
          ensure_ascii=False, indent=1)
print("\nwrote scene_tex.json")
