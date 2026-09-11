# -*- coding: utf-8 -*-
"""审计 9b：验证 06_模型/*.obj 顶点相对原始 bundle 是否 X 镜像。"""
import os, sys, struct

sys.stdout.reconfigure(encoding="utf-8")
import UnityPy

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"

def bundle_positions(obj):
    d = obj.read()
    vd = d.m_VertexData
    raw = vd.m_DataSize
    cnt = int(vd.m_VertexCount)
    ch = vd.m_Channels[0]
    stride = None
    # stride = max(offset+dim*size)
    sizes = {0: 4, 1: 4, 2: 4, 3: 4}   # float32 常见
    end = 0
    for c in vd.m_Channels:
        if c.dimension and c.format in (0, 1, 2, 3):
            end = max(end, c.offset + c.dimension * 4)
    stride = end
    if stride * cnt != len(raw):
        # 兜底: 用 32 字节
        stride = len(raw) // cnt if cnt else 32
    out = []
    dim = ch.dimension or 3
    for i in range(cnt):
        o = i * stride + ch.offset
        out.append(struct.unpack_from("<3f", raw, o))
    return out, stride, cnt, len(raw)

def obj_positions(fp):
    vs = []
    with open(fp, encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("v "):
                p = line.split()
                vs.append((float(p[1]), float(p[2]), float(p[3])))
            elif line.startswith("f "):
                break
    return vs

def check(scene):
    b = os.path.join(BUNDLE_DIR, "scenes_scenes_%s.bundle" % scene)
    d = os.path.join(r"d:/2/解包整理/06_模型", "scenes_scenes_%s" % scene)
    if not (os.path.exists(b) and os.path.isdir(d)):
        return
    env = UnityPy.load(b)
    idx = {}
    for n in os.listdir(d):
        if n.lower().endswith(".obj"):
            idx[n[:-4]] = os.path.join(d, n)
    print("===", scene, "bundle meshes:", sum(1 for o in env.objects if o.type.name == "Mesh"),
          " objs:", len(idx))
    done = 0
    for o in env.objects:
        if o.type.name != "Mesh":
            continue
        try:
            name = o.read_typetree().get("m_Name")
        except Exception:
            continue
        fp = idx.get(name)
        if not fp or done >= 4:
            continue
        try:
            bp, stride, cnt, rawlen = bundle_positions(o)
        except Exception as e:
            print("   ", name, "解析失败", e)
            continue
        op = obj_positions(fp)
        n = min(len(bp), len(op))
        if n == 0:
            print("   ", repr(name), "顶点数 0 (bundle=%d obj=%d)" % (len(bp), len(op)))
            continue
        same = max(max(abs(op[i][k] - bp[i][k]) for k in range(3)) for i in range(n))
        mir = max(max(abs(op[i][0] + bp[i][0]), abs(op[i][1] - bp[i][1]),
                      abs(op[i][2] - bp[i][2])) for i in range(n))
        verdict = "X镜像" if mir < 1e-4 and same > 1e-3 else ("一致" if same < 1e-4 else "都不匹配")
        print(f"    {name!r}: stride={stride} bundle顶点={len(bp)} obj顶点={len(op)} "
              f"原样diff={same:.5g} 镜像diff={mir:.5g} -> {verdict}")
        done += 1

for s in ["battlearena1", "battlearena2", "battlearenasororitas"]:
    check(s)
