#!/usr/bin/env python
# -*- coding: utf-8 -*-
import io, json, collections
D = 'd:/4/Unity/_tmp_view/mataudit'
d = json.load(io.open(D + '/orig_resolved.json', encoding='utf-8'))
KEYS = ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Color', '_BaseColor',
        '_Surface', '_Blend', '_SrcBlendAlpha', '_DstBlendAlpha', '_EmissionEnabled',
        '_AlphaClip', '_Mode', '_FlipbookBlending', '_SoftParticlesEnabled']


def cc(v):
    if isinstance(v, dict):
        return tuple(round(float(v.get(k, 0)), 4) for k in 'rgba')
    if isinstance(v, (list, tuple)):
        return tuple(round(float(x), 4) for x in v[:4])
    return v


for a in d:
    print('=' * 10, a)
    seen = collections.OrderedDict()
    for r in d[a]['resolved']:
        seen.setdefault(r['mat']['name'], []).append(r)
    for nm, rs in seen.items():
        m = rs[0]['mat']
        fl = m['floats']
        bl = {k: fl[k] for k in KEYS if k in fl}
        co = {k: cc(v) for k, v in m['colors'].items()}
        print('  %-44s | %-44s | q=%-5s' % (nm, (m['shader'] or '?')[:44], m['queue']))
        print('      kw=%s' % (','.join(m['validKw'] or [])))
        print('      blend=%s' % bl)
        print('      col=%s' % co)
        if len(rs) > 1:
            print('      ⚠️ 该名字有 %d 条引用（同源？）' % len(rs))
            # 检查同名是否属性不同
            sig = {json.dumps({'f': m2['mat']['floats'], 'c': m2['mat']['colors'],
                               'k': m2['mat']['validKw'], 's': m2['mat']['shader']},
                              sort_keys=True) for m2 in rs}
            if len(sig) > 1:
                print('      🔴 同名但属性不同（%d 种）' % len(sig))
