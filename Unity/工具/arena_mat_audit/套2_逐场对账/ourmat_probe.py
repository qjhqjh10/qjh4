# -*- coding: utf-8 -*-
"""读我们的 .mat（YAML）→ 渲染状态字段；与原版并排比。"""
import io, os, json, re, sys, collections

ROOT = 'd:/4/Unity/MyGame/Assets'
ARENAS = ['battlearena1', 'battlearena2', 'battlearena3', 'battlearenaaeldari']
GUID_NAMES = None


def load_guid_index():
    """guid -> 资产路径（扫工程 + Packages 的 .meta）。"""
    idx = {}
    for base in ('d:/4/Unity/MyGame/Assets', 'd:/4/Unity/MyGame/Packages',
                 'd:/4/Unity/MyGame/Library/PackageCache'):
        if not os.path.isdir(base):
            continue
        for dp, dn, fn in os.walk(base):
            for f in fn:
                if not f.endswith('.meta'):
                    continue
                p = os.path.join(dp, f)
                try:
                    head = io.open(p, encoding='utf-8', errors='replace').read(400)
                except Exception:
                    continue
                m = re.search(r'^guid: ([0-9a-f]{32})', head, re.M)
                if m:
                    idx.setdefault(m.group(1), p[:-5])
    return idx


def parse_mat(path):
    t = io.open(path, encoding='utf-8', errors='replace').read()
    d = {'path': path}
    m = re.search(r'^  m_Name: (.*)$', t, re.M)
    d['name'] = m.group(1).strip() if m else None
    m = re.search(r'^  m_Shader: \{.*guid: ([0-9a-f]{32})', t, re.M)
    d['shader_guid'] = m.group(1) if m else None
    m = re.search(r'^  m_CustomRenderQueue: (-?\d+)', t, re.M)
    d['queue'] = int(m.group(1)) if m else None
    def kwlist(tag):
        m = re.search(r'^  %s:\n((?:  - .*\n)*)' % tag, t, re.M)
        if not m:
            m2 = re.search(r'^  %s: \[\]' % tag, t, re.M)
            return [] if m2 else None
        return [l.strip()[2:] for l in m.group(1).rstrip('\n').split('\n')]
    d['valid'] = kwlist('m_ValidKeywords')
    d['invalid'] = kwlist('m_InvalidKeywords')
    # floats
    fl = {}
    m = re.search(r'^    m_Floats:\n((?:    - .*\n)*)', t, re.M)
    if m:
        for l in m.group(1).rstrip('\n').split('\n'):
            k, _, v = l.strip()[2:].partition(': ')
            fl[k] = float(v)
    co = {}
    m = re.search(r'^    m_Colors:\n((?:    - .*\n)*)', t, re.M)
    if m:
        for l in m.group(1).rstrip('\n').split('\n'):
            k, _, v = l.strip()[2:].partition(': ')
            co[k] = [float(x) for x in re.findall(r'-?[\d.eE+]+', v)]
    d['floats_all'] = fl
    d['colors_all'] = co
    # texenvs: slot -> guid
    tx = {}
    m = re.search(r'^    m_TexEnvs:\n(.*?)^    m_', t, re.M | re.S)
    if m:
        block = m.group(1)
        for sm in re.finditer(r'^    - (\S+):\n      m_Texture: \{fileID: \d+, guid: ([0-9a-f]+)', block, re.M):
            tx[sm.group(1)] = sm.group(2)
    d['texenvs'] = tx
    return d


def main():
    gi = load_guid_index()
    print('guid index: %d' % len(gi))
    rows = {}
    for a in ARENAS:
        dirp = '%s/WarpforgeArena1/arenas/%s/Materials' % (ROOT, a)
        for f in sorted(os.listdir(dirp)):
            if not f.endswith('.mat'):
                continue
            d = parse_mat(os.path.join(dirp, f))
            rows[(a, d['name'])] = d
    print('我们的 PS_ 材质数：', len(rows))
    for (a, n), d in sorted(rows.items()):
        g = d['shader_guid']
        print(json.dumps({'arena': a, 'name': n, 'shader': os.path.basename(gi.get(g, g or '?')),
                          'shader_path': gi.get(g), 'queue': d['queue'], 'valid': d['valid'],
                          'invalid': d['invalid'],
                          'floats': {k: v for k, v in d['floats_all'].items()
                                     if k in ('_SrcBlend', '_DstBlend', '_ZWrite', '_Cull',
                                              '_EmissionEnabled', '_Surface', '_Blend',
                                              '_AlphaClip', '_SrcBlendAlpha', '_DstBlendAlpha',
                                              '_ColorMode', '_SoftParticlesNearFadeDistance')},
                          'colors': {k: v for k, v in d['colors_all'].items()
                                     if k in ('_Color', '_BaseColor', '_TintColor', '_EmissionColor',
                                              '_EmissiveColor', '_HDRColor')},
                          'texenvs': d['texenvs']}, ensure_ascii=False))
    return 0


if __name__ == '__main__':
    sys.exit(main())
