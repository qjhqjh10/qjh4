#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""shader pathID → {name, props[]}（属性表 —— 用来区分「材质值写死在材质里」vs「shader 没这个名字=死值」）。"""
import io, json, os
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
OUT = 'd:/4/Unity/_tmp_view/mataudit/shader_props.json'
need = set()
d = json.load(io.open('d:/4/Unity/_tmp_view/mataudit/orig_resolved.json', encoding='utf-8'))
for a, v in d.items():
    for r in v['resolved']:
        p = r['mat'].get('shaderPid')
        if p is not None:
            need.add(str(p))
print('需要 %d 个 shader' % len(need))

idx = {}
files = [f for f in sorted(os.listdir(AA)) if f.endswith('.bundle')]
i = 0
while need and i < len(files):
    f = files[i]
    i += 1
    if os.path.getsize(os.path.join(AA, f)) < 200000 and 'shader' not in f.lower():
        continue
    try:
        env = UnityPy.load(os.path.join(AA, f))
        for o in env.objects:
            if o.type.name != 'Shader':
                continue
            try:
                t = o.read_typetree()
            except Exception:
                continue
            ps = ((t.get('m_ParsedForm') or {}).get('m_PropInfo') or {}).get('m_Props') or []
            props = [p.get('m_Name') for p in ps if p.get('m_Name')]
            nm = (t.get('m_ParsedForm') or {}).get('m_Name') or ''
            idx[str(o.path_id)] = {'name': nm, 'props': props}
            need.discard(str(o.path_id))
    except Exception as e:
        print('  !! %s %s' % (f, e))
    print('  [%d/%d] %s  有 %d  还缺 %d' % (i, len(files), f, len(idx), len(need)))
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False, indent=1))
print('OK %d，还缺 %s' % (len(idx), sorted(need)))
