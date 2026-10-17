import os, io, json
MS = r"d:/2/新解包资源/assets_full/bundle_Waprforge_monoscripts/MonoScript"
m = {}
for fn in os.listdir(MS):
    if not fn.endswith(".json"): continue
    pid = fn[len("MonoScript_"):-len(".json")]
    try:
        with io.open(os.path.join(MS,fn), "r", encoding="utf-8-sig") as f:
            o = json.load(f)
        m[pid] = o.get("m_ClassName","?")
    except Exception: pass
ids = ["-6624639368848939580","3298024840710435243","146420486734122931","562972769022917651",
       "-3133590969968446585","5795978134593289333","-2313241570638646064","8712395647440592121",
       "-5965435178007098505","1207469734600494903","3885813256827862744","-5845469671781466849",
       "-8258122028049581671","-3478648252019303344","2854252058111336667","-4185286980102898918",
       "5388960122666258753","1487353074671658540"]
for i in ids:
    print(f"{i:>22} -> {m.get(i,'<not found>')}")
print("\ntotal monoscripts:", len(m))
for cls in ["RawCardScript","CardDeck","PlayerItem","BoosterPack","DropTable","DeckInfo","CardScript"]:
    hits=[k for k,v in m.items() if v==cls]
    print(cls, hits)
