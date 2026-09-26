#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把 4 个战场的【原版粒子材质】逐颗摊开（原文照抄，不做判断）。
解析一律按 pathID（这批包的 m_FileID 不可信 —— gen_arena_texslots.py 已实证）。
"""
import io, json, os, sys
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
ARENAS = ['battlearenaleviathan', 'battlearenasororitas', 'battlearenaspacewolves',
          'battlearenatauviorla']
OUT = 'd:/4/Unity/_tmp_view/mataudit/orig_materials.json'
SCRATCH = 'd:/4/Unity/_tmp_view/mataudit'


def main():
    os.makedirs(SCRATCH, exist_ok=True)
    allout = {}
    for arena in ARENAS:
        bundle = os.path.join(AA, 'scenes_scenes_%s.bundle' % arena)
        env = UnityPy.load(bundle)
        gos, mats, shaders, trs = {}, {}, {}, {}
        for o in env.objects:
            t = o.type.name
            try:
                if t == 'GameObject':
                    gos[o.path_id] = o.read_typetree().get('m_Name', '')
                elif t == 'Material':
                    mats[o.path_id] = o.read_typetree()
                elif t == 'Shader':
                    shaders[o.path_id] = o.read_typetree().get('m_Name', '')
                elif t in ('Transform', 'RectTransform'):
                    trs[o.path_id] = o.read_typetree()
            except Exception:
                continue

        def path_of(tpid):
            names, cur, g = [], tpid, 0
            while cur and cur in trs and g < 100:
                g += 1
                names.append(gos.get((trs[cur].get('m_GameObject') or {}).get('m_PathID'), '?'))
                cur = (trs[cur].get('m_Father') or {}).get('m_PathID')
            return '/'.join(reversed(names))

        rows = []
        for o in env.objects:
            if o.type.name != 'ParticleSystemRenderer':
                continue
            try:
                pr = o.read_typetree()
            except Exception:
                continue
            goid = (pr.get('m_GameObject') or {}).get('m_PathID')
            go = gos.get(goid, '?')
            for mref in (pr.get('m_Materials') or []):
                pid = (mref or {}).get('m_PathID')
                m = mats.get(pid)
                if m is None:
                    rows.append({'go': go, 'path': path_of(None), 'matPid': pid,
                                 'mat': None, 'note': '材质不在本包'})
                    continue
                sp = m.get('m_SavedProperties') or {}
                floats = {k: v for k, v in (sp.get('m_Floats') or [])}
                colors = {k: v for k, v in (sp.get('m_Colors') or [])}
                texs = {}
                for k, v in (sp.get('m_TexEnvs') or []):
                    t2 = (v or {}).get('m_Texture') or {}
                    texs[k] = t2.get('m_PathID', 0)
                rows.append({
                    'go': go, 'matPid': pid,
                    'mat': m.get('m_Name', ''),
                    'shader': shaders.get((m.get('m_Shader') or {}).get('m_PathID'), '?'),
                    'queue': m.get('m_CustomRenderQueue'),
                    'validKw': m.get('m_ValidKeywords'),
                    'invalidKw': m.get('m_InvalidKeywords'),
                    'legacyKw': m.get('m_ShaderKeywords'),
                    'floats': floats, 'colors': colors, 'texs': texs,
                    'texKeys': list(texs.keys()),
                })
        # GO 路径（按 GameObject 的 Transform 找）
        go2tr = {}
        for tpid, tr in trs.items():
            g = (tr.get('m_GameObject') or {}).get('m_PathID')
            if g in gos:
                go2tr[g] = tpid
        for r in rows:
            for gid, tr in go2tr.items():
                if gos.get(gid) == r['go']:
                    r['path'] = path_of(tr)
                    break
        allout[arena] = rows
        print('[%s] ParticleSystemRenderer 材质引用 %d 条' % (arena, len(rows)))
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(
        json.dumps(allout, ensure_ascii=False, indent=1))
    print('→ ' + OUT)


main()
