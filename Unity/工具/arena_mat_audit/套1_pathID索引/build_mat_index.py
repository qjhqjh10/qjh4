#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""全局索引：pathID → Material 的瘦身 typetree（84 个 aa bundle）。
为什么按 pathID：这批包 m_FileID 不可信（gen_arena_texslots.py 已实证）。
"""
import io, json, os, sys
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
OUT = 'd:/4/Unity/_tmp_view/mataudit/mat_index.json'
OUTSH = 'd:/4/Unity/_tmp_view/mataudit/shader_index.json'


def slim(m):
    sp = m.get('m_SavedProperties') or {}
    return {
        'name': m.get('m_Name'),
        'shaderPid': (m.get('m_Shader') or {}).get('m_PathID'),
        'queue': m.get('m_CustomRenderQueue'),
        'validKw': m.get('m_ValidKeywords'),
        'invalidKw': m.get('m_InvalidKeywords'),
        'floats': {k: v for k, v in (sp.get('m_Floats') or [])},
        'colors': {k: v for k, v in (sp.get('m_Colors') or [])},
        'texs': {k: ((v or {}).get('m_Texture') or {}).get('m_PathID', 0)
                 for k, v in (sp.get('m_TexEnvs') or [])},
    }


def main():
    idx, shidx = {}, {}
    files = sorted(f for f in os.listdir(AA) if f.endswith('.bundle'))
    for i, f in enumerate(files):
        try:
            env = UnityPy.load(os.path.join(AA, f))
            for o in env.objects:
                if o.type.name == 'Material':
                    d = o.read_typetree()
                    idx.setdefault(str(o.path_id), slim(d))
                    idx[str(o.path_id)]['_b'] = f
                elif o.type.name == 'Shader':
                    shidx.setdefault(str(o.path_id), o.read_typetree().get('m_Name', ''))
        except Exception as e:
            print('  !! %s %s' % (f, e))
        print('  [%d/%d] %s (mat=%d sh=%d)' % (i + 1, len(files), f, len(idx), len(shidx)))
    io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False))
    io.open(OUTSH, 'w', encoding='utf-8', newline='\n').write(json.dumps(shidx, ensure_ascii=False))
    print('OK mat=%d shader=%d' % (len(idx), len(shidx)))


main()
