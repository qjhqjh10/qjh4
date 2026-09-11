# -*- coding: utf-8 -*-
"""验证 OBJ 的 UV(vt) 相对 bundle 是否被镜像 —— 审计只验了顶点，没验 UV。"""
import os, sys, struct
sys.stdout.reconfigure(encoding="utf-8")
import UnityPy

BUNDLE_DIR = r"d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
D = r"d:/2/解包整理/06_模型/scenes_scenes_battlearena1"

def channels(vd):
    """返回 (stride, chan_list) —— 每个 channel: (offset, dim, fmt)"""
    end = 0
    for c in vd.m_Channels:
        if c.dimension and c.format in (0, 1, 2, 3):
            end = max(end, c.offset + c.dimension * 4)
    cnt = int(vd.m_VertexCount)
    stride = end if (cnt and end * cnt == len(vd.m_DataSize)) else (len(vd.m_DataSize)//cnt if cnt else 32)
    return stride, cnt, list(vd.m_Channels)

def chan_val(raw, stride, cnt, ch, dim):
    out = []
    for i in range(cnt):
        o = i * stride + ch.offset
        out.append(struct.unpack_from("<%df" % dim, raw, o))
    return out

def obj_vt(fp):
    vs = []
    with open(fp, encoding="utf-8", errors="replace") as f:
        for line in f:
            if line.startswith("vt "):
                p = line.split()
                vs.append((float(p[1]), float(p[2])))
            elif line.startswith("f "):
                break
    return vs

env = UnityPy.load(os.path.join(BUNDLE_DIR, "scenes_scenes_battlearena1.bundle"))
idx = {n[:-4]: os.path.join(D, n) for n in os.listdir(D) if n.lower().endswith(".obj")}
done = 0
for o in env.objects:
    if o.type.name != "Mesh": continue
    nm = o.read_typetree().get("m_Name")
    fp = idx.get(nm)
    if not fp or done >= 4: continue
    raw = o.read().m_VertexData.m_DataSize
    stride, cnt, chs = channels(o.read().m_VertexData)
    # UV0 = attribute index 4
    uvch = chs[4] if len(chs) > 4 else None
    if uvch is None or not uvch.dimension:
        print(f"  {nm!r}: 无 UV0 channel"); continue
    bu = chan_val(raw, stride, cnt, uvch, uvch.dimension)
    ov = obj_vt(fp)
    n = min(len(bu), len(ov))
    if n == 0:
        print(f"  {nm!r}: uv 数 bundle={len(bu)} obj={len(ov)}"); continue
    same = max(max(abs(ov[i][0]-bu[i][0]), abs(ov[i][1]-bu[i][1])) for i in range(n))
    miru = max(abs((1.0-ov[i][0]) - bu[i][0]) if False else abs(ov[i][0] - (1.0-bu[i][0])) for i in range(n))
    print(f"  {nm!r}: stride={stride} uv ch.off={uvch.offset} dim={uvch.dimension} "
          f"bundle={len(bu)} obj={len(ov)}  原样diff={same:.5g}  u镜像diff={miru:.5g}")
    done += 1
