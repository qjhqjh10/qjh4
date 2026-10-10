# -*- coding: utf-8 -*-
import json, io, sys, re
sys.stdout.reconfigure(encoding="utf-8")
P="Unity/数据/游戏数据/card_icon_plan.json"
CARDS="Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json"
plan={}
for c in json.load(io.open(P,encoding="utf-8"))["cards"]:
    plan[c["id"]]={f["name"]:[(i["token"],i["sprite"]) for i in f["items"]] for f in c["fields"]}
cards={c["id"]:c for c in json.load(io.open(CARDS,encoding="utf-8"))["cards"]}

print("=== A) 同字段内 token 互为子串（全池）===")
n=0
for cid,fs in plan.items():
    for fld,items in fs.items():
        toks=[t for t,_ in items]
        for a in toks:
            for b in toks:
                if a!=b and a in b:
                    n+=1
                    print("  ",cid,fld,"|",repr(a),"⊂",repr(b))
print("  合计:",n)

print()
print("=== B) 新 token 在原文里是不是【更长词】的一部分 ===")
for tok in ("Invulnerable","无敌"):
    hits=[]
    for cid,c in cards.items():
        for f in ("desc","descZh"):
            t=c.get(f) or ""
            for m in re.finditer(r"[A-Za-z一-鿿]*"+re.escape(tok)+r"[A-Za-z一-鿿]*", t):
                hits.append((cid,f,m.group(0)))
    longer=[h for h in hits if h[2]!=tok]
    print(f"  {tok!r}: 全池命中 {len(hits)} 处；其中『更长词』(词内包含) {len(longer)} 处")
    for h in longer: print("     ",h)

print()
print("=== C) 大小写不敏感扫 invulnerable（找别种写法/更长词）===")
pat=re.compile(r"[A-Za-z]*invulnerab[A-Za-z]*",re.I)
s=set()
for cid,c in cards.items():
    for f in ("desc","descZh"):
        for m in pat.finditer(c.get(f) or ""): s.add(m.group(0))
print("  实际出现的写法集合:",sorted(s))

print()
print("=== D) `无敌` 周边汉字（看有没有更长词）===")
s2=set()
for cid,c in cards.items():
    t=c.get("descZh") or ""
    for m in re.finditer(r".{0,4}无敌.{0,4}",t): s2.add(m.group(0))
for x in sorted(s2): print("   ",repr(x))

print()
print("=== E) 关键词数组 / 方括号 ===")
for cid,c in cards.items():
    kws=[k for k in (c.get("keywords") or []) if "invulnerab" in k.lower() or "无敌" in k]
    if kws: print("  keywords:",cid,kws)
    for f in ("desc","descZh"):
        for m in re.finditer(r"\[[^\[\]]*\]",c.get(f) or ""):
            if "invulnerab" in m.group(0).lower() or "无敌" in m.group(0):
                print("  bracket:",cid,f,m.group(0))
