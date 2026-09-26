# -*- coding: utf-8 -*-
"""原版战场粒子材质渲染状态：UnityPy 读原始 bundle（GO 名可靠）+ assets_full 兜底解外链材质。"""
import io, os, json, glob, sys, collections

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
AF = 'd:/2/新解包资源/assets_full'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari']

WANT_F = ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface', '_Blend', '_AlphaClip',
          '_SrcBlendAlpha', '_DstBlendAlpha', '_ColorMode', '_QueueOffset', '_EmissionEnabled']
WANT_C = ['_Color', '_BaseColor', '_TintColor', '_EmissionColor', '_EmissiveColor', '_HDRColor']


def pairlist(v):
    out = {}
    if isinstance(v, dict):
        return v
    for it in (v or []):
        try:
            out[str(it[0])] = it[1]
        except Exception:
            pass
    return out


def build_index():
    mi, si = {}, {}
    for d in sorted(glob.glob(AF + '/bundle_*')):
        b = os.path.basename(d)
        for kind, ix in (('Material', mi), ('Shader', si)):
            sub = os.path.join(d, kind)
            if not os.path.isdir(sub):
                continue
            for f in os.listdir(sub):
                if f.endswith('.json'):
                    ix[f[:-5].rsplit('_', 1)[1]] = (b, os.path.join(sub, f))
    return mi, si


def tt_mat(mt):
    sp = pairlist(mt.get('m_SavedProperties'))
    tx = pairlist(sp.get('m_TexEnvs'))
    fl = pairlist(sp.get('m_Floats'))
    co = pairlist(sp.get('m_Colors'))
    prims = [k for k in ('_BaseMap', '_MainTex') if k in tx]
    prim = prims[0] if prims else None
    return {'name': mt.get('m_Name'), 'queue': mt.get('m_CustomRenderQueue'),
            'valid': sorted(mt.get('m_ValidKeywords') or []),
            'invalid': sorted(mt.get('m_InvalidKeywords') or []),
            'shader_pid': str((mt.get('m_Shader') or {}).get('m_PathID')),
            'floats': {k: (v.get('x') if isinstance(v, dict) else v) for k, v in fl.items() if k in WANT_F},
            'colors': {k: [v.get('r'), v.get('g'), v.get('v') if False else v.get('b'), v.get('a')]
                       for k, v in co.items() if k in WANT_C},
            'texslot': prim,
            'texpid': str(((tx.get(prim) or {}).get('m_Texture') or {}).get('m_PathID')) if prim else None,
            'allslots': sorted(tx.keys())}


def main():
    import UnityPy
    mi, si = build_index()
    mans = {}
    for a in ARENAS:
        m = json.load(io.open('d:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s/%s_manifest.json'
                              % (a, a), encoding='utf-8'))
        mans[a] = collections.defaultdict(list)
        for p in m['particles']:
            mans[a][p['go']].append(p)

    for a in ARENAS:
        print('\n######## %s' % a)
        env = UnityPy.load(os.path.join(AA, 'scenes_scenes_%s.bundle' % a))
        inbundle = {}
        for o in env.objects:
            if o.type.name == 'Material':
                try:
                    inbundle[o.path_id] = o.read_typetree()
                except Exception:
                    pass
        rows = []
        for o in env.objects:
            if o.type.name != 'ParticleSystemRenderer':
                continue
            try:
                d = o.read()
                gname = d.m_GameObject.read().m_Name
            except Exception:
                gname = None
            mats = [getattr(m, 'm_PathID', None) for m in (d.m_Materials or [])]
            mp = str(mats[0]) if mats else None
            mt = inbundle.get(mats[0]) if mats else None
            host = 'arena bundle'
            if mt is None and mp and mp in mi:
                mt = json.load(io.open(mi[mp][1], encoding='utf-8'))
                host = mi[mp][0]
            if mt is None:
                rows.append({'go': gname, 'mat': None, 'pid': mp, 'host': None})
                continue
            r = tt_mat(mt)
            r['mat'] = r['name']
            r['arena'] = a
            sh = si.get(r['shader_pid'])
            r['shader'] = (json.load(io.open(sh[1], encoding='utf-8')).get('m_ParsedForm') or {}).get('m_Name') if sh else None
            r['shader_host'] = sh[0] if sh else None
            r['go'] = gname
            r['pid'] = mp
            r['host'] = host
            r['active'] = None
            r['inman'] = sum(1 for p in mans[a].get(gname, []))
            rows.append(r)
        # 去重（同一材质可能被多个 go 用）
        seen = {}
        for r in sorted(rows, key=lambda r: (r['mat'] or '', r['go'] or '')):
            seen.setdefault(r['pid'], r)
        for pid, r in sorted(seen.items(), key=lambda kv: kv[1]['mat'] or ''):
            print(json.dumps(r, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
