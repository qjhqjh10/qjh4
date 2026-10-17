import io,json,os,re,collections
BASE=r"d:/2/新解包资源/assets_full"
def load(p):
    with io.open(p,"r",encoding="utf-8-sig") as f: return json.load(f)
def mbfiles(b):
    d=os.path.join(BASE,f"bundle_{b}_assets_all","MonoBehaviour")
    return [os.path.join(d,f) for f in os.listdir(d) if f.endswith(".json")]
hexre=re.compile(r'^[0-9a-f]{32}$')
short=set(); hexs=set(); bysrc=collections.defaultdict(set)
for b,keys in [("draftpacks",["cardIds"]),("prebuiltdecks",["cardLibraryIds"]),("cosmeticsso",["targetId"]),("menus",["targetId"]),("duplicateassetisolationso",["targetId"])]:
    for p in mbfiles(b):
        try: o=load(p)
        except Exception: continue
        found=set()
        def walk(x,parent=None):
            if isinstance(x,dict):
                for k,v in x.items():
                    if k in keys and isinstance(v,str): found.add(v)
                    elif k in keys and isinstance(v,list):
                        for y in v:
                            if isinstance(y,str): found.add(y)
                    walk(v,k)
            elif isinstance(x,list):
                for y in x: walk(y,parent)
        walk(o)
        bysrc[b]|=found
for b,s in bysrc.items():
    sh={x for x in s if not hexre.match(x)}; hx={x for x in s if hexre.match(x)}
    short|=sh; hexs|=hx
    print(f"{b}: total={len(s)} short={len(sh)} hex32={len(hx)}")
print("\nUNION short-form card ids =",len(short))
print("  by army prefix:",collections.Counter(re.match(r'^([A-Za-z]+)',x).group(1) if re.match(r'^[A-Za-z]+',x) else '?' for x in short).most_common())
print("  sample:",sorted(short)[:12])
print("UNION hex32 ids =",len(hexs))
# check hex ids against cosmeticsso uniqueId
cu=set()
for p in mbfiles("cosmeticsso"):
    o=load(p)
    u=o.get("uniqueId")
    if isinstance(u,str) and u: cu.add(u)
print("hex ids also in cosmeticsso uniqueId:",len(hexs&cu))
