# -*- coding: utf-8 -*-
"""审计 12：全场景/预制体材质 blend 字段可靠性统计。
判据: _Blend(工具用的字段) vs _SrcBlend/_DstBlend(真实混合态) vs RenderType 标签。
"""
import os, sys, json, collections

sys.stdout.reconfigure(encoding="utf-8")
ROOT = r"d:/2/解包整理"

def scan(rel):
    base = os.path.join(ROOT, rel)
    if not os.path.isdir(base):
        return None
    st = collections.Counter()
    examples = collections.defaultdict(list)
    seen = set()
    for dp, dn, fn in os.walk(base):
        for f in fn:
            if not f.endswith(".json") or "Material" not in dp:
                continue
            p = os.path.join(dp, f)
            try:
                d = json.load(open(p, encoding="utf-8"))
            except Exception:
                continue
            if "m_SavedProperties" not in d:
                continue
            sp = d["m_SavedProperties"]
            fl = {k: v for k, v in sp.get("m_Floats", [])}
            b = fl.get("_Blend", 0.0)
            sb = fl.get("_SrcBlend")
            db = fl.get("_DstBlend")
            tag = dict(d.get("stringTagMap", []) or []).get("RenderType", "")
            kw = set(d.get("m_ValidKeywords", []) or [])
            key = (b, sb, db, tag)
            st[key] += 1
            if len(examples[key]) < 3:
                examples[key].append(os.path.relpath(p, ROOT).replace("\\", "/"))
    return st, examples

for rel in ["07_场景", "08_预制体特效/战斗预制体"]:
    r = scan(rel)
    if not r:
        continue
    st, ex = r
    print("===", rel, " 材质数:", sum(st.values()))
    print("%-8s %-9s %-9s %-22s %s" % ("_Blend", "_SrcBlend", "_DstBlend", "RenderType", "数量"))
    for (b, sb, db, tag), n in st.most_common(20):
        print("%-8s %-9s %-9s %-22s %d" % (b, sb, db, tag, n))
    print()
    # 关键: _Blend=0 但实际是 alpha 混合 (Src=5 SrcAlpha, Dst=10 OneMinusSrcAlpha)
    n_alpha = sum(n for (b, sb, db, t), n in st.items() if b == 0 and sb == 5.0 and db == 10.0)
    n_opaque = sum(n for (b, sb, db, t), n in st.items() if b == 0 and sb == 1.0 and db == 0.0)
    print(f"  _Blend=0 但 _SrcBlend=5/_DstBlend=10 (真·Alpha混合): {n_alpha}  ← 工具 blend 字段会误判为不透明")
    print(f"  _Blend=0 且 _SrcBlend=1/_DstBlend=0 (真·不透明): {n_opaque}")
    print()
