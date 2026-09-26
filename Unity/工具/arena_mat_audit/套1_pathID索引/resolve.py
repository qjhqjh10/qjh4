#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把 4 场的粒子材质（原版）解析齐（本包 + 全局索引），摊成一张表。"""
import io, json, os, collections

D = 'd:/4/Unity/_tmp_view/mataudit'
orig = json.load(io.open(D + '/orig_materials.json', encoding='utf-8'))
idx = json.load(io.open(D + '/mat_index.json', encoding='utf-8'))
shidx = json.load(io.open(D + '/shader_index.json', encoding='utf-8'))
MAN = 'd:/4/Unity/MyGame/Assets/WarpforgeArena1/arenas/%s/%s_manifest.json'


def norm_color(v):
    if isinstance(v, dict):
        return (round(v.get('r', 0), 6), round(v.get('g', 0), 6), round(v.get('b', 0), 6), round(v.get('a', 0), 6))
    return tuple(round(x, 6) for x in v)


def main():
    out = {}
    for arena, rows in orig.items():
        # 去重：同一 (GO 路径, matPid)
        byp = collections.OrderedDict()
        for r in rows:
            byp.setdefault((r['go'], r['matPid']), r)
        res = []
        unresolved = []
        for (go, pid), r in byp.items():
            hit = idx.get(str(pid))
            if r.get('mat'):
                src = '本包'
                m = {'name': r['mat'], 'queue': r['queue'], 'validKw': r['validKw'],
                     'invalidKw': r['invalidKw'], 'floats': r['floats'],
                     'colors': r['colors'], 'texs': r['texs'], 'shaderPid': None}
                # 本包材质的 shader 名在上面 dump 时已解出
                m['shader'] = r['shader']
            elif hit:
                src = hit.get('_b', '?')
                m = {'name': hit['name'], 'queue': hit['queue'], 'validKw': hit['validKw'],
                     'invalidKw': hit['invalidKw'], 'floats': hit['floats'],
                     'colors': hit['colors'], 'texs': hit['texs'],
                     'shader': shidx.get(str(hit.get('shaderPid')), '?'),
                     'shaderPid': hit.get('shaderPid')}
            else:
                unresolved.append((go, pid))
                continue
            res.append({'go': go, 'matPid': pid, 'src': src, 'mat': m})
        out[arena] = {'resolved': res, 'unresolved': unresolved}
        print('[%s] 去重后 %d 条 · 解出 %d · 未解出 %d' % (arena, len(byp), len(res), len(unresolved)))
        for go, pid in unresolved:
            print('    ❌ %s  matPid=%s' % (go, pid))
    io.open(D + '/orig_resolved.json', 'w', encoding='utf-8', newline='\n').write(
        json.dumps(out, ensure_ascii=False, indent=1))


main()
