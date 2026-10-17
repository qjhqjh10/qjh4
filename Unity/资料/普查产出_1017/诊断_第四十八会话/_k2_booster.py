import io,json,os,collections
BASE=r"d:/2/新解包资源/assets_full"
d=os.path.join(BASE,"bundle_boosterpacks_assets_all","MonoBehaviour")
files=[f for f in os.listdir(d) if f.endswith(".json")]
sig=collections.Counter(); scripts=collections.Counter()
for f in files:
    o=json.load(io.open(os.path.join(d,f),"r",encoding="utf-8-sig"))
    sig[tuple(sorted(o.keys()))]+=1
    s=o.get("m_Script",{}); scripts[s.get("m_PathID")]+=1
print("boosterpacks files:",len(files))
for k,v in sig.most_common(5): print(f"  {v:5d} {list(k)[:18]}")
print("scripts:",scripts.most_common(5))
# any list-of-string fields?
lists=collections.Counter()
for f in files:
    o=json.load(io.open(os.path.join(d,f),"r",encoding="utf-8-sig"))
    for k,v in o.items():
        if isinstance(v,list) and v and all(isinstance(x,str) for x in v): lists[k]+=1
print("string-list fields:",lists.most_common(10))
