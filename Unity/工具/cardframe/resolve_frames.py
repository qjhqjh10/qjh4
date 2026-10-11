import UnityPy, json, io, os, sys, struct
sys.stdout.reconfigure(encoding='utf-8')
AA = r'D:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
SO = r'D:/2/新解包资源/assets_full/bundle_staticgeneralassets_assets_all/MonoBehaviour/CardFramesByArmy.json'
so = json.load(io.open(SO, encoding='utf-8'))

# guild -> (bundle, pid)
want = set()
for cf in so['clanFrames']:
    for ft in cf['framesByTier']:
        for k in ('stratagemHighlightFrame','stratagemFrame','troopHighlightFrame','troopFrame'):
            want.add(ft[k]['m_AssetGUID'])
print('guids wanted:', len(want), file=sys.stderr)

bundles = [f for f in os.listdir(AA) if f.endswith('.bundle') and 'cardassets' in f]
g2obj = {}
for bn in bundles:
    env = UnityPy.load(os.path.join(AA, bn))
    for o in env.objects:
        if o.type.name != 'AssetBundle':
            continue
        for entry in o.read().m_Container:
            guid, info = entry[0], entry[1]
            if guid in want:
                g2obj[guid] = (bn, info.asset.m_PathID, info.asset.assetsfile)
print('resolved:', len(g2obj), 'of', len(want), file=sys.stderr)

# per bundle: build pid -> object
cache = {}
def getobj(bn, pid):
    if bn not in cache:
        env = UnityPy.load(os.path.join(AA, bn))
        cache[bn] = env
    env = cache[bn]
    for o in env.objects:
        if o.path_id == pid:
            return o
    return None

tex = {}
def texname(bn, pid):
    if (bn,pid) in tex: return tex[(bn,pid)]
    o = getobj(bn, pid)
    r = None
    if o is not None:
        d = o.read(); r = (d.m_Name, d.m_Width, d.m_Height)
    tex[(bn,pid)] = r
    return r

out = []
for cf in so['clanFrames']:
    for ft in cf['framesByTier']:
        row = {'army': cf['army'], 'tier': ft['cardTier']}
        for k in ('troopFrame','troopHighlightFrame','stratagemFrame','stratagemHighlightFrame'):
            guid = ft[k]['m_AssetGUID']
            got = g2obj.get(guid)
            if not got:
                row[k] = ('UNRESOLVED', guid, '', '', '')
                continue
            bn, pid, af = got
            o = getobj(bn, pid)
            if o is None:
                row[k] = ('NOOBJ', bn, pid, '', ''); continue
            d = o.read()
            t = d.m_RD.texture
            tinfo = None
            if t.assetsfile is not None:
                td = t.read()
                tinfo = (td.m_Name, td.m_Width, td.m_Height)
            row[k] = (d.m_Name, (d.m_Rect.x, d.m_Rect.y, d.m_Rect.width, d.m_Rect.height),
                      (d.m_Border.x, d.m_Border.y, d.m_Border.z, d.m_Border.w),
                      d.m_PixelsToUnits, (d.m_Pivot.x, d.m_Pivot.y), tinfo, bn if 'NO' in '' else '')
        out.append(row)

json.dump(out, io.open(r'C:/Users/qjh36/AppData/Local/Temp/d2/frames_resolved.json','w',encoding='utf-8'), ensure_ascii=False, indent=1, default=str)
for r in out:
    print('army %-4s tier %s' % (r['army'], r['tier']))
    for k in ('troopFrame','troopHighlightFrame','stratagemFrame','stratagemHighlightFrame'):
        v = r[k]
        print('   %-24s %s' % (k, v))
