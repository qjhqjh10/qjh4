# -*- coding: utf-8 -*-
"""审计 11：battlearena1 网格 → 材质 → 贴图 全表（含 blend 字段与贴图 alpha 对照）。"""
import os, sys, json

sys.stdout.reconfigure(encoding="utf-8")
from PIL import Image
SCRIPTS = r"d:/2/Warpforge_tools/scripts"
sys.path.insert(0, SCRIPTS)
os.chdir(SCRIPTS)
import unity_scene_to_godot as U   # noqa

ROOT = r"d:/2/解包整理"
FALLBACK = [os.path.join(ROOT, "08_预制体特效/共享资源/Texture2D"),
            os.path.join(ROOT, "03_界面UI/去重资源/Texture2D")]


def find_tex(name):
    for d in [os.path.join(ROOT, "07_场景/battlearena1/Texture2D")] + FALLBACK:
        p = os.path.join(d, name + ".png")
        if os.path.exists(p):
            return p
    return None


a = U.Assembler("battlearena1", r"d:/2/Warpforge_tools/tmp/audit_0910/_dummy")
rows = []
for t in sorted(a.TF):
    gopid = a.TF[t].get("m_GameObject", {}).get("m_PathID")
    mesh_name, _ = a.mesh_of(gopid)
    if not mesh_name:
        continue
    mats = a.mats_of(gopid)
    for mi in mats:
        rows.append((mesh_name, mi.name if hasattr(mi, "name") else "", mi.tex_name, mi))
print("%-34s %-36s %-40s %-6s %-6s %-6s %s" % ("MESH", "MATERIAL", "TEXTURE", "blend", "src", "dst", "tex_alpha"))
for mesh, mname, tname, mi in rows:
    alpha = "?"
    if tname:
        p = find_tex(tname)
        if p:
            try:
                im = Image.open(p).convert("RGBA")
                alpha = str(im.getchannel("A").getextrema())
            except Exception:
                alpha = "err"
        else:
            alpha = "NOTFOUND"
    print("%-34s %-36s %-40s %-6s %-6s %-6s %s" % (
        mesh[:34], str(mname)[:36], str(tname)[:40],
        getattr(mi, "blend", "?"), getattr(mi, "src_blend", "-"),
        getattr(mi, "dst_blend", "-"), alpha))
