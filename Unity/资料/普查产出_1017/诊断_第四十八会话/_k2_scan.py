import os, io, json, collections

BASE = r"d:/2/新解包资源/assets_full"
BUNDLES = ["draftpacks","prebuiltdecks","cosmeticsso","menus","duplicateassetisolationso","duplicateassetisolation"]

def load(p):
    with io.open(p, "r", encoding="utf-8-sig") as f:
        return json.load(f)

summary = {}
for b in BUNDLES:
    d = os.path.join(BASE, f"bundle_{b}_assets_all", "MonoBehaviour")
    if not os.path.isdir(d):
        print(f"=== {b}: DIR MISSING ==="); continue
    files = [f for f in os.listdir(d) if f.endswith(".json")]
    sig = collections.Counter()
    script_counts = collections.Counter()
    idlists = collections.defaultdict(list)
    uid_values = []
    cardname_str = 0
    cardname_pptr = 0
    parsed = 0
    for fn in files:
        p = os.path.join(d, fn)
        try:
            o = load(p)
        except Exception as e:
            sig[("PARSE_FAIL", str(e)[:40])] += 1; continue
        if not isinstance(o, dict): continue
        parsed += 1
        s = o.get("m_Script", {})
        spid = s.get("m_PathID") if isinstance(s, dict) else None
        script_counts[spid] += 1
        sig[tuple(sorted(o.keys()))] += 1
        for k in ("cardIds","cardLibraryIds","cardIdsSkirmish","ids","deckIds"):
            if isinstance(o.get(k), list):
                idlists[k].extend([x for x in o[k] if isinstance(x, str)])
        if isinstance(o.get("uniqueId"), str) and o["uniqueId"]:
            uid_values.append(o["uniqueId"])
        cn = o.get("cardName")
        if isinstance(cn, str) and cn: cardname_str += 1
        elif isinstance(cn, dict): cardname_pptr += 1
    print(f"\n=== {b}  files={len(files)} parsed={parsed}  distinct_signatures={len(sig)}  scripts={len(script_counts)}  uniqueId_nonempty={len(uid_values)}  cardName_str={cardname_str} cardName_pptr={cardname_pptr} ===")
    print("  top signatures:")
    for k,v in sig.most_common(8):
        print(f"    {v:6d}  {list(k)[:16]}")
    print("  top script pathIDs:", script_counts.most_common(8))
    for k, vals in idlists.items():
        print(f"  {k}: entries={len(vals)} distinct={len(set(vals))} sample={sorted(set(vals))[:8]}")
    if uid_values:
        print(f"  uniqueId distinct={len(set(uid_values))} sample={sorted(set(uid_values))[:10]}")
