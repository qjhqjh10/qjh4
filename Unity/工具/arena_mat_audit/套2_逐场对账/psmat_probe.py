# -*- coding: utf-8 -*-
"""原版 vs 我们：战场粒子材质渲染状态并排比（只读）。"""
import io, os, json, re, glob, sys, collections

AF = 'd:/2/新解包资源/assets_full'
ROOT = 'd:/4/Unity/MyGame/Assets'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari']

WANT_F = ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_EmissionEnabled', '_Surface',
          '_Blend', '_AlphaClip', '_SrcBlendAlpha', '_DstBlendAlpha', '_ColorMode', '_QueueOffset']
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


# ---------------------------------------------------------------- 原版索引
def build_index():
    mi, si = {}, {}
    for d in sorted(glob.glob(AF + '/bundle_*')):
        b = os.path.basename(d)
        for kind, ix in (('Material', mi), ('Shader', si)):
            sub = os.path.join(d, kind)
            if not os.path.isdir(sub):
                continue
            for f in os.listdir(sub):
                if f.endswith('.json') and '_' in f:
                    ix[f[:-5].rsplit('_', 1)[1]] = (b, os.path.join(sub, f))
    return mi, si


def jload(p):
    return json.load(io.open(p, encoding='utf-8'))


def orig_mat(p):
    mj = jload(p)
    sp = pairlist(mj.get('m_SavedProperties'))
    tx = pairlist(sp.get('m_TexEnvs'))
    fl = pairlist(sp.get('m_Floats'))
    co = pairlist(sp.get('m_Colors'))
    prim = next((k for k in ('_BaseMap', '_MainTex') if k in tx), None)
    d = {'name': mj.get('m_Name'), 'queue': mj.get('m_CustomRenderQueue'),
         'valid': sorted(mj.get('m_ValidKeywords') or []),
         'invalid': sorted(mj.get('m_InvalidKeywords') or []),
         'shader_pid': str((mj.get('m_Shader') or {}).get('m_PathID')),
         'floats': {k: (v.get('x') if isinstance(v, dict) else v) for k, v in fl.items() if k in WANT_F},
         'colors': {k: [v.get('r'), v.get('g'), v.get('b'), v.get('a')] for k, v in co.items() if k in WANT_C},
         'texslot': prim,
         'texpid': str(((tx.get(prim) or {}).get('m_Texture') or {}).get('m_PathID')) if prim else None,
         'texenvs': sorted(tx.keys())}
    return d


# ---------------------------------------------------------------- 我们的 .mat
def parse_our(path):
    t = io.open(path, encoding='utf-8', errors='replace').read()
    d = {'path': path}
    names = re.findall(r'^  m_Name: (.*)$', t, re.M)
    d['name'] = names[-1].strip() if names else None
    m = re.search(r'^  m_Shader: \{.*guid: ([0-9a-f]{32})', t, re.M)
    d['shader_guid'] = m.group(1) if m else None
    m = re.search(r'^  m_CustomRenderQueue: (-?\d+)', t, re.M)
    d['queue'] = int(m.group(1)) if m else None

    def kw(tag):
        m = re.match(r'  %s: \[\]' % tag, t, re.M)
        if m:
            return []
        m = re.search(r'^  %s:\n((?:  - .*\n)*)' % tag, t, re.M)
        return [l.strip()[2:] for l in m.group(1).rstrip('\n').split('\n')] if m else None
    d['valid'] = kw('m_ValidKeywords')
    d['invalid'] = kw('m_InvalidKeywords')
    fl, co = {}, {}
    m = re.search(r'^    m_Floats:\n((?:    - .*\n)*)', t, re.M)
    if m:
        for l in m.group(1).rstrip('\n').split('\n'):
            k, _, v = l.strip()[2:].partition(': ')
            try:
                fl[k] = float(v)
            except ValueError:
                pass
    m = re.search(r'^    m_Colors:\n((?:    - .*\n)*)', t, re.M)
    if m:
        for l in m.group(1).rstrip('\n').split('\n'):
            k, _, v = l.strip()[2:].partition(': ')
            co[k] = [float(x) for x in re.findall(r'-?[\d.]+(?:e-?\d+)?', v)]
    d['floats'] = {k: v for k, v in fl.items() if k in WANT_F}
    d['colors'] = {k: v for k, v in co.items() if k in WANT_C}
    tx = {}
    m = re.search(r'^    m_TexEnvs:\n(.*?)^    m_\w', t, re.M | re.S)
    if m:
        for sm in re.finditer(r'^    - (\S+):\n        m_Texture: \{fileID: \d+, guid: ([0-9a-f]+)',
                              m.group(1), re.M):
            tx[sm.group(1)] = sm.group(2)
    d['texenvs'] = sorted(tx.keys())
    d['tex_guid'] = tx.get('_BaseMap') or tx.get('_MainTex')
    return d


# ---------------------------------------------------------------- 收集原版粒子材质
def collect(arena, mi, si):
    d = os.path.join(AF, 'bundle_scenes_scenes_%s' % arena)
    go, psr, pss = {}, {}, {}
    for kind, tgt in (('GameObject', go), ('ParticleSystemRenderer', psr), ('ParticleSystem', pss)):
        sub = os.path.join(d, kind)
        for f in (os.listdir(sub) if os.path.isdir(sub) else []):
            if f.endswith('.json') and '_' in f:
                tgt[f[:-5].rsplit('_', 1)[1]] = jload(os.path.join(sub, f))
    out = []
    for pid, r in sorted(psr.items(), key=lambda kv: int(kv[0])):
        gopid = str((r.get('m_GameObject') or {}).get('m_PathID'))
        g = go.get(gopid, {})
        out.append({'psr': pid, 'go': g.get('m_Name'), 'active': g.get('m_IsActive'),
                    'matpid': str(((r.get('m_Materials') or [{}])[0] or {}).get('m_PathID'))})
    return out


def main():
    mi, si = build_index()
    mans = {}
    for a in ARENAS:
        manif = jload('%s/WarpforgeArena1/arenas/%s/%s_manifest.json' % (ROOT, a, a))
        # go name -> list of manifest entries (含 matName)
        d = collections.defaultdict(list)
        for p in manif['particles']:
            d[p['go']].append(p)
        mans[a] = d
    for a in ARENAS:
        print('\n######## %s' % a)
        recs = collect(a, mi, si)
        seen = {}
        for r in recs:
            if r['matpid'] in ('0', 'None'):
                continue
            hit = mi.get(r['matpid'])
            if not hit:
                print('  %-32s ❌ 找不到材质 pid=%s' % (r['go'], r['matpid']))
                continue
            o = orig_mat(hit[1])
            o['host'] = hit[0]
            sj = si.get(o['shader_pid'])
            o['shader'] = (jload(sj[1]).get('m_ParsedForm') or {}).get('m_Name') if sj else None
            o['shader_host'] = sj[0] if sj else None
            o['go'] = r['go']
            o['active'] = r['active']
            seen.setdefault(r['matpid'], o)
        for pid, o in sorted(seen.items(), key=lambda kv: kv[1]['name'] or ''):
            inman = sum(len(v) for k, v in mans[a].items() if k == o['go'])
            print(json.dumps({'arena': a, 'go': o['go'], 'active': o['active'], 'inmanifest': inman,
                              'mat': o['name'], 'pid': pid, 'host': o['host'],
                              'shader': o['shader'], 'shader_host': o['shader_host'],
                              'queue': o['queue'], 'valid': o['valid'],
                              'floats': o['floats'], 'colors': o['colors'],
                              'texslot': o['texslot'], 'texpid': o['texpid'],
                              'texenvs': o['texenvs']}, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
