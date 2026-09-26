# -*- coding: utf-8 -*-
import json, io, os, sys, collections
sys.path.insert(0, 'd:/tmp')
from psmat_probe import parse_our, ROOT, ARENAS
our = {}
for a in ARENAS:
    dp = '%s/WarpforgeArena1/arenas/%s/Materials' % (ROOT, a)
    for f in os.listdir(dp):
        if f.endswith('.mat'):
            our[(a, f[:-4])] = parse_our(os.path.join(dp, f))
rows = [json.loads(l) for l in io.open('d:/tmp/orig2_out.jsonl', encoding='utf-8') if l.startswith('{')]
orig = {}
for r in rows:
    orig.setdefault((r['arena'], r['mat']), r)
mans = {}
for a in ARENAS:
    m = json.load(io.open('%s/WarpforgeArena1/arenas/%s/%s_manifest.json' % (ROOT, a, a), encoding='utf-8'))
    d = collections.defaultdict(list)
    for p in m['particles']:
        if p.get('matName'):
            d[p['matName']].append(p)
    mans[a] = d
print('=== 清单用到的材质 ===')
for a in ARENAS:
    for mat, ps in sorted(mans[a].items()):
        act = sum(1 for p in ps if p.get('active') and p.get('renderMode') != 5 and p.get('texFile'))
        o = orig.get((a, mat)); u = our.get((a, 'PS_' + mat))
        if u is None and act == 0:
            continue
        sh = o['shader'] if o else '?'
        bits = []
        if u is None:
            bits.append('未落盘(建%d颗)' % act)
        else:
            if o:
                if o['queue'] != u['queue']:
                    bits.append('q=%s→%s' % (o['queue'], u['queue']))
                for k in ('_SrcBlend', '_DstBlend', '_ZWrite', '_Cull'):
                    ov = o['floats'].get(k); uv = u['floats'].get(k)
                    if ov is not None and uv is not None and abs(float(ov) - float(uv)) > 1e-4:
                        bits.append('%s=%s→%s' % (k, ov, uv))
                for k in ('_Color', '_BaseColor', '_EmissionColor', '_EmissiveColor'):
                    ov = o['colors'].get(k); uv = u['colors'].get(k)
                    if ov and uv and any(abs(x - y) > 1e-3 for x, y in zip(ov, uv)):
                        bits.append('%s=%s→%s' % (k, [round(x, 3) for x in ov], [round(x, 3) for x in uv]))
            if 'Particles/Unlit' not in (sh or ''):
                bits.append('sh=原版`%s`' % sh)
        print('%s %-19s %-42s 建%2d颗 | %s %s' % ('OK ' if not bits else 'DIFF', a, mat, act,
                                                  str(sh)[:48], ' '.join(bits)))
