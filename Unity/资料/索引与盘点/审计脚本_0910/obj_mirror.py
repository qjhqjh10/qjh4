# -*- coding: utf-8 -*-
"""审计 9：验证 06_模型/*.obj 是否为 UnityPy 导出的 X 镜像版本（vs 原始 bundle 网格数据）。
方法: 从 scenes_scenes_battlearena1.bundle 取若干 Mesh，取顶点集 A；读对应 OBJ 顶点集 B；
     分别比较 B 与 A、B 与 (-x,y,z)A 的最大偏差。
"""
import os, sys, json

sys.stdout.reconfigure(encoding="utf-8")
import UnityPy

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
OBJ_DIR = r"d:/2/解包整理/06_模型/scenes_scenes_battlearena1"
BUNDLE = os.path.join(BUNDLE_DIR, "scenes_scenes_battlearena1.bundle")

env = UnityPy.load(BUNDLE)
objs = [o for o in env.objects if o.type.name == "Mesh"]
print("bundle Mesh 数:", len(objs))

def read_obj_verts(fp):
    vs = []
    with open(fp, encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("v "):
                p = line.split()
                vs.append((float(p[1]), float(p[2]), float(p[3])))
    return vs

idx = {}
for n in os.listdir(OBJ_DIR):
    if n.lower().endswith(".obj"):
        idx[n[:-4]] = os.path.join(OBJ_DIR, n)

tested = 0
for o in objs:
    try:
        d = o.read()
        name = d.m_Name
    except Exception as e:
        continue
    fp = idx.get(name)
    if not fp:
        continue
    verts = read_obj_verts(fp)
    try:
        m = d.m_Vertices
        bv = [(m[i], m[i+1], m[i+2]) for i in range(0, len(m), 3)]
    except Exception:
        continue
    if not verts or not bv:
        continue
    n = min(len(verts), len(bv))
    same = max(max(abs(verts[i][k] - bv[i][k]) for k in range(3)) for i in range(n))
    mir = max(max(abs(verts[i][0] + bv[i][0]), abs(verts[i][1] - bv[i][1]),
                  abs(verts[i][2] - bv[i][2])) for i in range(n))
    tested += 1
    print(f"  {name!r}: OBJ顶点={len(verts)} bundle顶点={len(bv)} | OBJ vs 原样 maxdiff={same:.4g} | OBJ vs (-x,y,z) maxdiff={mir:.4g}")
    if tested >= 8:
        break
print("测试网格数:", tested)
