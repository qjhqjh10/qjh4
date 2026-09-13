# -*- coding: utf-8 -*-
"""audit_effect_shaders.py — 效果 -> 原版 shader 对账（纯 Python，不碰 Unity）

数据源: Assets/WarpforgeVFX/导出报告.tsv 第 3 格，内部三段:
  ① 材质定义N个   ② 原 shader: A, B, C   ③ 近似替代 N 处（可缺）
判据链: WarpforgeShaderMap.Replacements(运行时) -> EffectExporter.ShaderMap(导出期)
        -> 工程/包内同名 shader(Assets+PackageCache 的 .shader 声明 ∪ Unity 内置旧 shader 名单)
        -> wf_shaders.bundle / wf_shaders_extra.bundle (UnityPy 实读) -> 未解析
用法: "D:/2/Warpforge_tools/py312/python.exe" -X utf8 d:/4/Unity/工具/audit_effect_shaders.py
产物(完整版): d:/4/Unity/资料/普查产出_0913/效果_shader_对账.{md,tsv}
"""
import os, collections
A = r"d:/4/Unity/MyGame/Assets"
REPORT = A + "/WarpforgeVFX/导出报告.tsv"
NL = chr(10); TAB = chr(9)

def map_keys(path, anchor):
    blk = open(path, encoding="utf-8").read().split(anchor)[1].split("};")[0]
    out = {}
    for line in blk.split(NL):
        line = line.split("//")[0].strip()
        if not line.startswith("{"): continue
        p = [x.strip().strip(chr(34)) for x in line.rstrip(",").strip("{}").split(",")]
        if len(p) == 2: out[p[0]] = p[1]
    return out

RT = map_keys(A + "/WarpforgeVFX/Runtime/WarpforgeShaderMap.cs", "Replacements = new Dictionary<string, string>")
EM = map_keys(A + "/WarpforgeArena1/Editor/EffectExporter.cs", "static readonly Dictionary<string, string> ShaderMap = new Dictionary<string, string>")

def declared(root):
    s = set()
    for dp, _, fs in os.walk(root):
        for f in fs:
            if not f.endswith(".shader"): continue
            t = open(os.path.join(dp, f), encoding="utf-8", errors="ignore").read()
            i = t.find('Shader "')
            if i >= 0: s.add(t[i + 8:].split(chr(34))[0])
    return s

PROJ = declared(A) | declared(r"d:/4/Unity/MyGame/Library/PackageCache")
UNITY_BUILTIN = {"Sprites/Default", "Sprites/Mask", "UI/Default", "UI/Additive",
                "Mobile/Particles/Additive", "Mobile/Particles/Alpha Blended", "Mobile/Particles/Multiply",
                "Particles/Additive", "Particles/Standard Unlit", "Legacy Shaders/Particles/Additive",
                "Legacy Shaders/Particles/Alpha Blended", "Legacy Shaders/Particles/Alpha Blended Premultiply",
                "Legacy Shaders/Particles/Anim Alpha Blended"}


def bundle(path):
    import UnityPy
    s = set()
    for o in UnityPy.load(path).objects:
        if o.type.name != "Shader": continue
        d = o.read(); pf = getattr(d, "m_ParsedForm", None)
        n = (getattr(pf, "m_Name", "") if pf is not None else "") or getattr(d, "m_Name", "")
        if n: s.add(n)
    return s

BB = A + "/StreamingAssets/WarpforgeVFX/"
MAIN = bundle(BB + "wf_shaders.bundle")
EXTRA = bundle(BB + "wf_shaders_extra.bundle")

def cat(n):
    if n in RT: return "A自建替代"
    if n in EM: return "B标准自带"
    if n in PROJ or n in UNITY_BUILTIN: return "C工程同名"
    if n in MAIN: return "D主包兜底"
    if n in EXTRA: return "E补充包兜底"
    return "F未解析"

rows = []
for l in open(REPORT, "rb").read().decode("utf-8-sig").split(NL)[1:]:
    if not l.strip(): continue
    f = l.rstrip().split(TAB); sg = f[2].split("；")
    sh = [x.strip() for x in sg[1].split("原 shader:")[-1].split(",") if x.strip()]
    ap = None
    for s in sg[2:]:
        if "近似替代" in s: ap = int(s.split("近似替代")[1].split("处")[0].strip())
    un = [x for x in sh if x not in RT and x not in EM]
    rows.append(dict(name=f[0], mats=int(sg[0].split("材质定义")[1].split("个")[0]), sh=sh, ap=ap, un=un,
                     flag="无" if not un else ("是 · 含未解析" if any(cat(x) == "F未解析" for x in un) else "是 · 仅 bundle 兜底")))

cnt = collections.Counter(x for r in rows for x in r["sh"])
print("效果行", len(rows), "| shader 名去重", len(cnt),
      "| Everguild", sum(1 for n in cnt if n.startswith("Everguild/")),
      "| 自建", sum(1 for n in cnt if n.startswith("WarpforgeVFX/")))
print("分类:", collections.Counter(cat(n) for n in cnt))
print("效果级:", collections.Counter(r["flag"] for r in rows))
print("未解析:", [n for n in cnt if cat(n) == "F未解析"])
print("主包", len(MAIN), "补充包", len(EXTRA), "| 映射表", len(RT), "/", len(EM))
for n, c in sorted(cnt.items(), key=lambda kv: -kv[1])[:15]:
    print("  %4d  %-6s %s" % (c, cat(n), n))
