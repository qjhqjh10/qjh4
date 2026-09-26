#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""读我们工程里的粒子材质（.mat YAML），抽出关键字 / 关键浮点 / 颜色。
判据：清单的 matName → 文件名 `PS_<Sanitize(matName)>.mat`（ArenaBuilder.GetOrCreateParticleMaterial）。"""
import io, json, os, re, sys, collections

ARENAS = ['battlearenaleviathan', 'battlearenasororitas', 'battlearenaspacewolves',
          'battlearenatauviorla']
AREN = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s'
OUT = 'd:/4/Unity/_tmp_view/mataudit/our_materials.json'
FLOAT_KEYS = ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface', '_Blend',
              '_SrcBlendAlpha', '_DstBlendAlpha', '_EmissionEnabled', '_AlphaClip',
              '_Mode', '_FlipbookBlending', '_SoftParticlesEnabled', '_QueueOffset']
COLOR_KEYS = ['_Color', '_BaseColor', '_EmissionColor', '_TintColor', '_EmissiveColor']


def sanitize(s):
    bad = set('<>:"/\\|?*') | {chr(c) for c in range(32)}
    for c in bad:
        s = s.replace(c, '_')
    return s.strip()


def parse_mat(path):
    txt = io.open(path, encoding='utf-8', errors='replace').read()
    d = {'path': path, 'name': None, 'guid': None, 'queue': None,
         'valid': [], 'invalid': [], 'floats': {}, 'colors': {}}
    m = re.search(r'^  m_Name: (.*)$', txt, re.M)
    if m: d['name'] = m.group(1).strip()
    m = re.search(r'^  m_CustomRenderQueue: (-?\d+)', txt, re.M)
    if m: d['queue'] = int(m.group(1))
    m = re.search(r'^  m_ValidKeywords:\n((?:  - .*\n)*)', txt, re.M)
    if m: d['valid'] = [x.strip()[2:].strip() for x in m.group(1).splitlines()]
    m = re.search(r'^  m_InvalidKeywords:\n((?:  - .*\n)*)', txt, re.M)
    if m: d['invalid'] = [x.strip()[2:].strip() for x in m.group(1).splitlines()]
    m = re.search(r'^    m_Floats:\n((?:    - .*\n)*)', txt, re.M)
    if m:
        for ln in m.group(1).splitlines():
            mm = re.match(r'    - (\S+): ([-0-9.eE+]+)', ln)
            if mm: d['floats'][mm.group(1)] = float(mm.group(2))
    m = re.search(r'^    m_Colors:\n((?:    - .*\n)*)', txt, re.M)
    if m:
        for ln in m.group(1).splitlines():
            mm = re.match(r'    - (\S+): \{r: ([-0-9.eE+Infinity]+), g: ([-0-9.eE+Infinity]+), b: ([-0-9.eE+Infinity]+), a: ([-0-9.eE+Infinity]+)\}', ln)
            if mm:
                def fv(x):
                    return float('inf') if 'Inf' in x else float(x)
                d['colors'][mm.group(1)] = tuple(fv(mm.group(i)) for i in range(2, 6))
    # 主贴图槽
    d['texSlots'] = re.findall(r'^    - (\S+):\n        m_Texture: \{fileID: (\d+), guid: (\w+), type: (\d+)\}', txt, re.M)
    return d


def main():
    out = {}
    for a in ARENAS:
        mdir = AREN % a
        man = json.load(io.open(os.path.join(mdir, a + '_manifest.json'), encoding='utf-8'))
        mats = {}
        for f in os.listdir(os.path.join(mdir, 'Materials')):
            if f.endswith('.mat'):
                p = os.path.join(mdir, 'Materials', f)
                mats[f[:-4]] = parse_mat(p)
        rows = []
        for p in man['particles']:
            ident = p.get('matName')
            fallback = False
            if not ident:
                ident = (p.get('texFile') or '') + '_' + (
                    ('%.3f' % p['matColor'][0]) if p.get('matColor') and len(p['matColor']) >= 3 else '-')
                fallback = True
            fn = 'PS_' + sanitize(ident)
            rows.append({'go': p['go'], 'ident': ident, 'fallback': fallback, 'file': fn,
                         'exists': fn in mats, 'mat': mats.get(fn),
                         'manShader': p.get('matShader'), 'manKw': p.get('matKeywords'),
                         'manProps': {q['k']: q for q in (p.get('matProps') or [])},
                         'manQueue': p.get('matQueue')})
        out[a] = {'rows': rows, 'allMats': sorted(mats.keys())}
        miss = [r['file'] for r in rows if not r['exists']]
        print('[%s] 粒子 %d · 材质文件 %d · 落盘缺 %d' % (a, len(rows), len(mats), len(miss)))
        if miss: print('   缺:', sorted(set(miss)))
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False, indent=1))
    print('→ ' + OUT)


main()
