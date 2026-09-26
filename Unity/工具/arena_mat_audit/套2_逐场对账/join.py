# -*- coding: utf-8 -*-
"""并排比：原版粒子材质 vs 我们的 PS_*.mat（只读）。"""
import io, os, json, sys, collections
sys.path.insert(0, 'd:/tmp')
from psmat_probe import parse_our, ROOT, ARENAS

our_cache = {}
for a in ARENAS:
    dirp = '%s/WarpforgeArena1/arenas/%s/Materials' % (ROOT, a)
    for f in sorted(os.listdir(dirp)):
        if f.endswith('.mat'):
            d = parse_our(os.path.join(dirp, f))
            d['file'] = f
            our_cache[(a, f[:-4])] = d

rows = [json.loads(l) for l in io.open('d:/tmp/psmat_out.jsonl', encoding='utf-8') if l.startswith('{')]

# 每场：清单里真正用到的粒子材质（按 go 名匹配）
mans = {}
for a in ARENAS:
    m = json.load(io.open('%s/WarpforgeArena1/arenas/%s/%s_manifest.json' % (ROOT, a, a), encoding='utf-8'))
    mans[a] = collections.defaultdict(list)
    for p in m['particles']:
        mans[a][p['go']].append(p)

print('=== 逐材料并排比（只列 -> 差的字段）===')
summary = []
for r in sorted(rows, key=lambda r: (r['arena'], r['mat'] or '')):
    a = r['arena']
    # 该 go 名在清单里的粒子（可能就是它）
    plist = mans[a].get(r['go']) or []
    inm = bool(plist)
    want_files = set('PS_' + p['matName'] for p in plist if p.get('matName'))
    ourname = 'PS_' + (r['mat'] or '')
    ours = our_cache.get((a, ourname))
    diffs = []
    if not inm:
        diffs.append('※ 该 PSR 在清单里没有（原版关着/UI/其它场遗留）')
    if ours is None:
        diffs.append('🔴 我们没有这份材质（%s 不存在）' % ourname)
    else:
        if r['queue'] != ours['queue']:
            diffs.append('queue 原版%s vs 我们%s' % (r['queue'], ours['queue']))
        ov, uv = set(r['valid']), set(ours['valid'] or [])
        if ov - uv:
            diffs.append('关键字缺: ' + ','.join(sorted(ov - uv)))
        if uv - ov:
            diffs.append('关键字多: ' + ','.join(sorted(uv - ov)))
        for k in ('_SrcBlend', '_DstBlend', '_ZWrite', '_Cull', '_Surface'):
            if k in r['floats']:
                o = r['floats'][k]
                u = ours['floats'].get(k)
                if u is None:
                    diffs.append('%s 原版%s → 我们缺这个键' % (k, o))
                elif abs(float(o) - float(u)) > 1e-4:
                    diffs.append('%s 原版%s vs 我们%s' % (k, o, u))
            elif k in ours['floats']:
                diffs.append('%s 原版没有 → 我们有%s' % (k, ours['floats'][k]))
        for k in ('_BaseColor', '_Color', '_EmissionColor', '_EmissiveColor'):
            if k in r['colors'] and k in ours['colors']:
                o, u = r['colors'][k], ours['colors'][k]
                if any(abs(x - y) > 1e-3 for x, y in zip(o, u)):
                    diffs.append('%s 原版%s vs 我们%s' % (k, [round(x, 4) for x in o], [round(x, 4) for x in u]))
            elif k in r['colors']:
                diffs.append('%s 原版有%s → 我们没有这个键' % (k, [round(x, 4) for x in r['colors'][k]]))
        if r['shader'] and 'Particles/Unlit' not in (r['shader'] or ''):
            diffs.append('🔴 shader 原版 `%s` → 我们一律 URP/Particles/Unlit（粒子路**没有**运行时重建）' % r['shader'])
        if sorted(ours['texenvs'] or []) == []:
            diffs.append('⚠️ 我们的 .mat 里 m_TexEnvs 为空')
    summary.append((a, r['go'], r['mat'], r['shader'], inm, diffs, ours, r))

print('共 %d 颗材质（去重后按场）' % len(summary))
for a, go, mat, sh, inm, diffs, ours, r in summary:
    tag = '清单内' if inm else '清单外'
    print('\n[%s] %s ‖ go=%s ‖ %s ‖ shader=%s' % (a, mat, go, tag, sh))
    for d in diffs:
        print('     - ' + d)
