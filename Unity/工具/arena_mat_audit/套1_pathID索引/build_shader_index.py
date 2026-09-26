#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""重建 shader 名索引（名字在 `m_ParsedForm.m_Name`，顶层 `m_Name` 是空的）。"""
import io, json, os
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
AF = 'd:/2/新解包资源/assets_full'
OUT = 'd:/4/Unity/_tmp_view/mataudit/shader_index.json'

targets = set()
d = json.load(io.open('d:/4/Unity/_tmp_view/mataudit/orig_resolved.json', encoding='utf-8'))
for a, v in d.items():
    for r in v['resolved']:
        p = r['mat'].get('shaderPid')
        if p is not None:
            targets.add(str(p))
print('需要 %d 个 shader' % len(targets))

idx = {}
files = [os.path.join(AA, f) for f in sorted(os.listdir(AA)) if f.endswith('.bundle')]
for f in sorted(os.listdir(AF)):
    p = os.path.join(AF, f)
    if os.path.isfile(p):
        files.append(p)
missing = set(targets)
for i, p in enumerate(files):
    if not missing:
        break
    try:
        env = UnityPy.load(p)
        for o in env.objects:
            if o.type.name == 'Shader':
                try:
                    d2 = o.read_typetree()
                except Exception:
                    continue
                nm = (d2.get('m_ParsedForm') or {}).get('m_Name') or d2.get('m_Name') or ''
                if nm:
                    idx[str(o.path_id)] = nm
                    missing.discard(str(o.path_id))
    except Exception as e:
        print('  !! %s %s' % (os.path.basename(p), e))
    print('  [%d/%d] %s  shader=%d 还缺 %d' % (i + 1, len(files), os.path.basename(p), len(idx), len(missing)))
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(idx, ensure_ascii=False))
print('OK shader=%d 还缺=%s' % (len(idx), sorted(missing)[:20]))
