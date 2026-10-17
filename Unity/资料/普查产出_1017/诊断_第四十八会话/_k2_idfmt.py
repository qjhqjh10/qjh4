import io,json,os,re,collections
BASE=r"d:/2/新解包资源/assets_full"
def load(p):
    with io.open(p,"r",encoding="utf-8-sig") as f: return json.load(f)
def files(b): 
    d=os.path.join(BASE,f"bundle_{b}_assets_all","MonoBehaviour")
    return [os.path.join(d,f) for f in os.listdir(d) if f.endswith(".json")]
hexre=re.compile(r'^[0-9a-f]{32}$')
def classify(ids):
    h=[x for x in ids if hexre.match(x)]
    s=[x for x in ids if not hexre.match(x)]
    return len(h),len(s),s[:6]

# prebuiltdecks cardLibraryIds per-file
pre=collections.defaultdict(set); preall=set()
for p in files("prebuiltdecks"):
    o=load(p)
    for x in o.get("cardLibraryIds",[]):
        pre[p].add(x); preall.add(x)
h,s,ex=classify(preall)
print(f"prebuiltdecks cardLibraryIds distinct={len(preall)} hex32={h} nonhex={s} nonhex_sample={ex}")
non=[x for x in preall if not hexre.match(x)]
print("  nonhex all sample:", sorted(non)[:20])
print("  hex sample:", sorted(x for x in preall if hexre.match(x))[:10])
# per-file mix
mixed=sum(1 for p,v in pre.items() if any(hexre.match(x) for x in v) and any(not hexre.match(x) for x in v))
print("  files with mixed id formats:", mixed, "of", len(pre))
# draftpacks
dr=set()
for p in files("draftpacks"):
    dr.update(load(p).get("cardIds",[]))
print("draftpacks cardIds distinct=",len(dr),"hex32=",sum(1 for x in dr if hexre.match(x)),"sample:",sorted(dr)[:6])
# cosmeticsso targetIds
ct=set(); cu=set(); cn=collections.Counter()
for p in files("cosmeticsso"):
    o=load(p)
    if isinstance(o.get("uniqueId"),str) and o["uniqueId"]: cu.add(o["uniqueId"])
    def walk(o):
        if isinstance(o,dict):
            for k,v in o.items():
                if k=="targetId" and isinstance(v,str) and v: ct.add(v)
                elif isinstance(v,(dict,list)): walk(v)
        elif isinstance(o,list):
            for x in o: walk(x)
    walk(o)
print("cosmeticsso uniqueId distinct=",len(cu),"hex32=",sum(1 for x in cu if hexre.match(x)))
print("cosmeticsso targetId distinct=",len(ct),"hex32=",sum(1 for x in ct if hexre.match(x)),"nonhex=",len(ct)-sum(1 for x in ct if hexre.match(x)))
print("  nonhex sample:",sorted(x for x in ct if not hexre.match(x))[:10])
