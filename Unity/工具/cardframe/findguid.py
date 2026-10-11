import UnityPy, os, sys
sys.stdout.reconfigure(encoding='utf-8')
AA=r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
want={'7163291bc26180d4da83473e98c2a0ba','19e56d3903f60da438faf912d48b2752','7d41790c20d7ed344ab2dd9e0c22fc66','bb7e1025fa6aed6449b904dddedf5b59'}
for fn in sorted(os.listdir(AA)):
    if not fn.endswith('.bundle'): continue
    try: env=UnityPy.load(os.path.join(AA,fn))
    except Exception: continue
    for o in env.objects:
        if o.type.name!='AssetBundle': continue
        try: c=o.read().m_Container
        except Exception: continue
        for e in c:
            if e[0] in want:
                a=e[1].asset
                try: nm=a.read().m_Name
                except Exception: nm='?'
                print(fn, e[0], '->', nm)
