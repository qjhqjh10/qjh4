import io,json,os,collections
BASE=r"d:/2/新解包资源/assets_full"
cmap=json.load(io.open(r"d:/2/Warpforge_tools/data/cab_bundle_map.json","r",encoding="utf-8"))
local={b for v in cmap.values() for b in v}
for b in ["prebuiltdecks","cosmeticsso","menus","draftpacks","duplicateassetisolationso"]:
    ab=os.path.join(BASE,f"bundle_{b}_assets_all","AssetBundle")
    f=os.path.join(ab,os.listdir(ab)[0])
    o=json.load(io.open(f,"r",encoding="utf-8-sig"))
    deps=o.get("m_Dependencies",[])
    print(f"\n=== {b}  ({len(deps)} deps) ===")
    for i,d in enumerate(deps,1):
        key="CAB-"+d[4:]
        names=cmap.get(key)
        status="LOCAL" if names else "*** NOT LOCAL ***"
        print(f"  m_FileID={i}: {d} -> {names[0] if names else '<absent>'}  {status}")
    # fileIDs used
    c=collections.Counter(x.get("m_FileID") for x in o.get("m_PreloadTable",[]))
    print("  preload fileIDs:",dict(c))
