import os, io, json, collections, re
BASE = r"d:/2/新解包资源/assets_full"
def load(p):
    with io.open(p,"r",encoding="utf-8-sig") as f: return json.load(f)
def mbfiles(b): return [os.path.join(BASE,f"bundle_{b}_assets_all","MonoBehaviour",f) for f in os.listdir(os.path.join(BASE,f"bundle_{b}_assets_all","MonoBehaviour")) if f.endswith(".json")]

def collect(b, keys):
    out = collections.defaultdict(set)
    nfiles = collections.Counter()
    for p in mbfiles(b):
        try: o = load(p)
        except Exception: continue
        if not isinstance(o, dict): continue
        def walk(o, path=""):
            if isinstance(o, dict):
                for k,v in o.items():
                    if k in keys:
                        if isinstance(v,str) and v: out[k].add(v); nfiles[k]+=1
                        elif isinstance(v,list):
                            for x in v:
                                if isinstance(x,str) and x: out[k].add(x)
                            nfiles[k]+=1
                    elif isinstance(v,(dict,list)): walk(v, path+"/"+k)
            elif isinstance(o,list):
                for x in o: walk(x,path)
        walk(o)
    return out, nfiles

keys = {"targetId","uniqueId","cardIds","cardLibraryIds","id","deckId","cardbackId"}
res = {}
for b in ["draftpacks","prebuiltdecks","cosmeticsso","menus","duplicateassetisolationso"]:
    out,nf = collect(b, keys)
    res[b]=out
    print(f"\n=== {b} ===")
    for k in sorted(out):
        print(f"  {k}: distinct={len(out[k])} files={nf[k]} sample={sorted(out[k])[:5]}")

print("\n=== OVERLAPS ===")
dt = res["cosmeticsso"]["uniqueId"]
pre = res["prebuiltdecks"]["cardLibraryIds"]
draft = res["draftpacks"]["cardIds"]
menu_t = res["menus"]["targetId"]
cos_t = res["cosmeticsso"]["targetId"]
dup_t = res["duplicateassetisolationso"]["targetId"]
print("cosmeticsso uniqueId x prebuiltdecks cardLibraryIds:", len(dt & pre), sorted(dt & pre)[:5])
print("cosmeticsso uniqueId x draftpacks cardIds:", len(dt & draft), sorted(dt & draft)[:5])
print("prebuiltdecks cardLibraryIds x draftpacks cardIds:", len(pre & draft), sorted(pre & draft)[:5])
print("menus targetId x draftpacks cardIds:", len(menu_t & draft), sorted(menu_t & draft)[:5])
print("menus targetId x prebuiltdecks:", len(menu_t & pre), sorted(menu_t & pre)[:5])
print("dup targetId x draftpacks:", len(dup_t & draft), sorted(dup_t & draft)[:5])
print("dup targetId x prebuiltdecks:", len(dup_t & pre), sorted(dup_t & pre)[:5])
print("cosmeticsso targetId x prebuiltdecks:", len(cos_t & pre), sorted(cos_t & pre)[:5])
print("cosmeticsso targetId x draftpacks:", len(cos_t & draft), sorted(cos_t & draft)[:5])
