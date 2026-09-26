#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""原版粒子材质 vs 我们工程里的 .mat：逐字段对账。只报有差/有疑的。"""
import io, json, os, re, collections

D = 'd:/4/Unity/_tmp_view/mataudit'
ARENAS = ['battlearenaleviathan', 'battlearenasororitas', 'battlearenaspacewolves',
          'battlearenatauviorla']
AREN = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s'
shidx = json.load(io.open(D + '/shader_index.json', encoding='utf-8'))
shprops = json.load(io.open(D + '/shader_props.json', encoding='utf-8'))
orig = json.load(io.open(D + '/orig_resolved.json', encoding='utf-8'))
ours = json.load(io.open(D + '/our_materials.json', encoding='utf-8'))

# 原版材质里那些「驱动混合的 shader 属性名」（来自 shader pass 的 rtBlend*.name）
BLEND_DRIVEN = {'_SrcBlend', '_DstBlend', '_SrcBlendAlpha', '_DstBlendAlpha'}

GUID2 = {}
for base in ('d:/4/Unity/MyGame/Assets', 'd:/4/Unity/MyGame/Library/PackageCache'):
    for root, _, fs in os.walk(base):
        for f in fs:
            if f.endswith('.shader.meta'):
                try:
                    g = re.search(r'^guid: (\w+)', io.open(os.path.join(root, f), encoding='utf-8', errors='replace').read(), re.M)
                except Exception:
                    continue
                if g:
                    GUID2[g.group(1)] = f[:-len('.shader.meta')]

BLEND_KEYS = ['_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface', '_Blend',
              '_SrcBlendAlpha', '_DstBlendAlpha', '_EmissionEnabled']


def cc(v):
    if isinstance(v, dict):
        return tuple(round(float(v.get(k, 0)), 4) for k in 'rgba')
    if isinstance(v, (list, tuple)) and len(v) >= 4:
        try:
            return tuple(round(float(x), 4) for x in v[:4])
        except Exception:
            return None
    return None


def orig_by_name(a):
    out = collections.defaultdict(list)
    for r in orig[a]['resolved']:
        m = dict(r['mat'])
        m['shader'] = shidx.get(str(m.get('shaderPid')), '?') if m.get('shaderPid') else m.get('shader')
        out[m['name']].append(m)
    return out


def main():
    for a in ARENAS:
        man = json.load(io.open(AREN % a + '/' + a + '_manifest.json', encoding='utf-8'))
        ob = orig_by_name(a)
        groups = collections.OrderedDict()
        for p in man['particles']:
            built = bool(p.get('texFile')) and p.get('active') and p.get('renderMode') != 5
            ident = p.get('matName')
            if not ident:
                continue
            g = groups.setdefault(ident, {'n': 0, 'nbuilt': 0, 'gos': [], 'p': p})
            g['n'] += 1
            if built:
                g['nbuilt'] += 1
                g['gos'].append(p['go'])
        print('=' * 100)
        print('### %s —— %d 个材质名 · 会建的粒子 %d' % (a, len(groups), sum(g['nbuilt'] for g in groups.values())))
        for ident, g in sorted(groups.items()):
            if g['nbuilt'] == 0:
                continue
            p = g['p']
            o = ob.get(ident) or []
            om = o[0] if o else None
            fn = 'PS_' + re.sub(r'[<>:"/\\|?*]', '_', ident).strip()
            mm = next((r['mat'] for r in ours[a]['rows'] if r['file'] == fn and r['mat']), None)
            osh = om['shader'] if om else '?'
            op = (shprops.get(str(om.get('shaderPid')) or '', {}) or {}).get('props', []) if om else []
            myguid = re.search(r'm_Shader: \{fileID: \d+, guid: (\w+)', io.open(mm['path'], encoding='utf-8', errors='replace').read()).group(1) if mm else None
            mysh = GUID2.get(myguid, myguid or '?')
            print('-' * 100)
            print('%-40s 粒子%d(会建%d) | 原版 shader=%s | 我方=%s' % (ident, g['n'], g['nbuilt'], osh, mysh))
            if om is None or mm is None:
                print('   ⚠️ %s' % ('原版材质查不到' if om is None else '我方 .mat 不在盘上'))
                continue
            # 关键字
            okw = set(om['validKw'] or [])
            mkw = set(mm['valid'])
            if okw != mkw:
                print('   🔴 关键字 原版=%s  我方=%s' % (sorted(okw), sorted(mkw)))
            # 队列
            if om['queue'] != mm['queue']:
                print('   🔴 队列   原版=%s  我方=%s' % (om['queue'], mm['queue']))
            # 混合
            for k in BLEND_KEYS:
                ov = om['floats'].get(k)
                mv = mm['floats'].get(k)
                if k not in op:
                    if k in BLEND_DRIVEN and mv is not None:
                        print('   ⚠️ %-16s 原版 shader 的混合是**写死在 pass 里**（材质值 %s 是死值）· 我方写进了 %s'
                              % (k, ov, mv))
                    continue
                if ov is None and mv is None:
                    continue
                if ov is None or mv is None or abs((ov or 0) - (mv or 0)) > 1e-4:
                    print('   🔴 %-16s 原版=%s  我方=%s' % (k, ov, mv))
            # 颜色
            for k in ['_Color', '_BaseColor', '_TintColor', '_EmissionColor',
                      '_Color_Mutliplier', '_EmissiveColor', '_Color2',
                      '_Depth_And_Fallof_XY', '_Depth_And_Fallof']:
                oc, mc = cc(om['colors'].get(k)), cc(mm['colors'].get(k))
                if k not in op:
                    if oc is not None and mc is not None:
                        print('   ⚠️ %-20s 原版 shader 无此属性（材质值 %s 死值）· 我方写进 %s' % (k, oc, mc))
                    elif oc is not None and mc is None:
                        print('   ℹ️ %-20s 原版有(死值) %s · 我方没有' % (k, oc))
                    continue
                if oc is None and mc is None:
                    continue
                if oc != mc:
                    print('   🔴 %-20s 原版=%s  我方=%s' % (k, oc, mc))
            # 我们完全没有的属性（原版 shader 声明了、但我们没设）
            lost = [k for k in ('_Color', '_Color_Mutliplier', '_Color2', '_Depth_And_Fallof_XY',
                                '_Depth_And_Fallof', '_EmissiveColor', '_PrimaryTex_Scale_XY_Speed_ZW',
                                '_SecondaryTex_Scale_XY_Speed_ZW', '_TintColor')
                    if k in op and k not in mm['colors'] and k not in mm['floats']]
            if lost:
                print('   ℹ️ 原版 shader 声明、我方没设：%s  原值=%s' % (
                    lost, {k: cc(om['colors'].get(k)) or om['floats'].get(k) for k in lost}))
            ots = sorted(k for k, v in om['texs'].items() if v)
            if ots:
                print('   ℹ️ 原版贴图槽=%s' % ots)


main()
