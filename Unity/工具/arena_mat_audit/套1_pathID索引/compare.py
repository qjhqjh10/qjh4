#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""原版 vs 我们：逐颗粒子材质对渲染状态。"""
import io, json, os, re, collections

D = 'd:/4/Unity/_tmp_view/mataudit'
ARENAS = ['battlearenaleviathan', 'battlearenasororitas', 'battlearenaspacewolves',
          'battlearenatauviorla']
AREN = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s'
shidx = json.load(io.open(D + '/shader_index.json', encoding='utf-8'))
orig = json.load(io.open(D + '/orig_resolved.json', encoding='utf-8'))
ours = json.load(io.open(D + '/our_materials.json', encoding='utf-8'))

# 我们的 .mat 里 m_Shader 的 guid → 名字（靠在工程里找 shader 文件）
GUID2NAME = {}


def build_guid_map():
    for root, _, fs in os.walk('d:/4/Unity/MyGame/Assets'):
        for f in fs:
            if not f.endswith('.shader.meta'):
                continue
            p = os.path.join(root, f)
            try:
                m = re.search(r'^guid: (\w+)', io.open(p, encoding='utf-8', errors='replace').read(), re.M)
            except Exception:
                continue
            if m:
                GUID2NAME[m.group(1)] = f[:-len('.shader.meta')]


# 原版材质名 → 该名字下所有 (shader, 属性指纹)
def orig_by_name(arena):
    out = collections.defaultdict(list)
    for r in orig[arena]['resolved']:
        m = r['mat']
        out[m['name']].append({
            'shader': shidx.get(str(m.get('shaderPid')), '?') if m.get('shaderPid') else m.get('shader'),
            'queue': m['queue'], 'kw': m['validKw'], 'floats': m['floats'],
            'colors': m['colors'], 'texs': m['texs'], 'go': r['go'], 'src': r['src']})
    return out


for a in ARENAS:
    print('#' * 70)
    print('#', a)
    ob = orig_by_name(a)
    rows = ours[a]['rows']
    # 按落盘文件归并（这就是「一个原版材质 → 一份我们的材质」的粒度）
    byfile = collections.OrderedDict()
    for r in rows:
        byfile.setdefault(r['file'], []).append(r)
    for fn, rs in sorted(byfile.items()):
        idents = sorted({r['ident'] for r in rs})
        manShaders = sorted({r['manShader'] for r in rs if r['manShader']})
        manKws = sorted({tuple(r['manKw'] or []) for r in rs})
        exists = rs[0]['exists']
        m = rs[0]['mat']
        built = [r for r in rs if r.get('_built', True)]
        print('--- %s  原版材质名=%s  粒子数=%d  落盘=%s' % (fn, idents, len(rs), '✓' if exists else '❌缺'))
        print('    原版 shader=%s | 清单 matShader=%s' % (sorted({o['shader'] for i in idents if i in ob for o in ob[i]}), manShaders))
        if exists and m:
            print('    我方 shader guid=%s (%s)' % (m['texSlots'][0][3] if False else re.search(r'm_Shader: \{fileID: \d+, guid: (\w+)', io.open(m['path'], encoding='utf-8', errors='replace').read()).group(1),
                                                    ''))
            print('    我方 queue=%s | valid=%s | invalid=%s' % (m['queue'], m['valid'], m['invalid']))
            print('    我方 floats=%s' % {k: m['floats'].get(k) for k in
                  ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface', '_Blend', '_SrcBlendAlpha', '_DstBlendAlpha', '_EmissionEnabled']})
            print('    我方 colors=%s' % {k: m['colors'].get(k) for k in
                  ['_Color', '_BaseColor', '_EmissionColor', '_TintColor']})
