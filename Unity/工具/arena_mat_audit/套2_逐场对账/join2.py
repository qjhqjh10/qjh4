# -*- coding: utf-8 -*-
"""最终并排表：原版粒子材质 vs 我们的 PS_*.mat。"""
import io, os, json, sys, collections
sys.path.insert(0, 'd:/tmp')
from psmat_probe import parse_our, ROOT, ARENAS

our_cache = {}
for a in ARENAS:
    dirp = '%s/WarpforgeArena1/arenas/%s/Materials' % (ROOT, a)
    for f in sorted(os.listdir(dirp)):
        if f.endswith('.mat'):
            d = parse_our(os.path.join(dirp, f))
            our_cache[(a, f[:-4])] = d

mans = {}
for a in ARENAS:
    m = json.load(io.open('%s/WarpforgeArena1/arenas/%s/%s_manifest.json' % (ROOT, a, a), encoding='utf-8'))
    mans[a] = collections.defaultdict(list)
    for p in m['particles']:
        mans[a][p['matName']].append(p)

rows = [json.loads(l) for l in io.open('d:/tmp/orig2_out.jsonl', encoding='utf-8') if l.startswith('{')]

print('清单材质名（含粒子数 / 有无原版关着的）:')
for a in ARENAS:
    for mn, ps in sorted(mans[a].items(), key=lambda kv: -len(kv[1])):
        n_off = sum(1 for p in ps if not p.get('active'))
        print('   [%s] %-45s x%d%s' % (a, mn, len(ps), ' (%d 颗原版关着)' % n_off if n_off else ''))

print('\n=== 原版 vs 我们（只列有差/有疑的字段）===')
nbad = 0
for r in sorted(rows, key=lambda r: (r['arena'], r['mat'] or '')):
    a, mat = r['arena'], r['mat']
    if mat is None:
        print('[%s] ❌ 材质解不出 pid=%s go=%s' % (a, r['pid'], r['go']))
        continue
    inman = mans[a].get(mat)
    ours = our_cache.get((a, 'PS_' + mat))
    notes = []
    if not inman:
        notes.append('※ 清单里没这颗材质（原版关着/UI/别场遗留）—— 不比对')
    else:
        if ours is None:
            notes.append('🔴 我们的 PS_%s.mat **不存在**' % mat)
        else:
            oq, uq = r['queue'], ours['queue']
            if oq != uq:
                notes.append('queue 原版=%s 我们=%s' % (oq, uq))
            ov, uv = set(r['valid']), set(ours['valid'] or [])
            if ov - uv:
                notes.append('关键字缺: ' + ' '.join(sorted(ov - uv)))
            if uv - ov:
                notes.append('关键字多: ' + ' '.join(sorted(uv - ov)))
            for k in ('_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface'):
                if k in r['floats']:
                    o, u = r['floats'][k], ours['floats'].get(k)
                    if u is None:
                        notes.append('%s 原版=%s 我们缺键' % (k, o))
                    elif abs(float(o) - float(u)) > 1e-4:
                        notes.append('%s 原版=%s 我们=%s' % (k, o, u))
            for k in ('_BaseColor', '_Color', '_EmissionColor', '_EmissiveColor'):
                o = r['colors'].get(k)
                if o is None:
                    continue
                u = ours['colors'].get(k)
                if u is None:
                    notes.append('%s 原版=%s 我们缺键' % (k, [round(x, 4) for x in o]))
                elif any(abs(x - y) > 1e-3 for x, y in zip(o, u)):
                    notes.append('%s 原版=%s 我们=%s' % (k, [round(x, 4) for x in o], [round(x, 4) for x in u]))
            if r['shader'] and 'Particles/Unlit' not in r['shader']:
                notes.append('🔴shader 原版=`%s` → 我们一律 URP/Particles/Unlit' % r['shader'])
            if not ours['texenvs']:
                notes.append('⚠️ 我们的 .mat 里贴图槽为空（连 _BaseMap 都没有）')
    tag = ('清单内 %d 颗' % len(inman)) if inman else '清单外'
    print('[%-20s] %-42s go=%-26s %s | sh=%s | q=%s | kw=%s' % (
        a, mat[:42], (r['go'] or '?')[:26], tag, (r['shader'] or '?')[:44], r['queue'],
        ','.join(r['valid'])[:60]))
    print('        贴图槽=%s pid=%s host=%s' % (r['texslot'], r['texpid'], r['host']))
    for x in notes:
        print('        - ' + x)
    if notes and not inman:
        pass
