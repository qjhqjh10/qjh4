#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把 4 场粒子用到的**全部**原版 shader 的属性表 / SubShader 队列 tag / pass 混合取出来
（含 build_shader_props.py 因为跳小包而漏掉的那个）。"""
import io, json, os
import UnityPy

AA = 'd:/2/unity_run_ref/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64'
OUT = 'd:/4/Unity/_tmp_view/mataudit/shader_full.json'
si = json.load(io.open('d:/4/Unity/_tmp_view/mataudit/shader_index.json', encoding='utf-8'))
want = set()
d = json.load(io.open('d:/4/Unity/_tmp_view/mataudit/orig_resolved.json', encoding='utf-8'))
for a, v in d.items():
    for r in v['resolved']:
        p = r['mat'].get('shaderPid')
        if p is not None:
            want.add(str(p))
# 只取这些 shader 的名字（shader_index 是全量扫出来的）
names = {si[k]: k for k in want if k in si}
print('需要 %d 个 shader: %s' % (len(want), sorted(names)))

out = {}
for f in sorted(os.listdir(AA)):
    if not f.endswith('.bundle') or not want:
        continue
    try:
        env = UnityPy.load(os.path.join(AA, f))
    except Exception:
        continue
    for o in env.objects:
        if o.type.name != 'Shader' or str(o.path_id) not in want:
            continue
        t = o.read_typetree()
        pf = t.get('m_ParsedForm') or {}
        subs = pf.get('m_SubShaders') or []
        tags = []
        blends = []
        for s in subs:
            tags.append({k: v for k, v in (s.get('m_Tags') or {}).items()})
            for p in (s.get('m_Passes') or []):
                st = p.get('m_State') or {}
                b0 = st.get('rtBlend0') or {}
                blends.append({
                    'pass': p.get('m_Name'),
                    'src': (b0.get('srcBlend') or {}).get('name'), 'srcv': (b0.get('srcBlend') or {}).get('val'),
                    'dst': (b0.get('destBlend') or {}).get('name'), 'dstv': (b0.get('destBlend') or {}).get('val'),
                    'srcA': (b0.get('srcBlendAlpha') or {}).get('name'), 'dstA': (b0.get('destBlendAlpha') or {}).get('name'),
                    'zwrite': (st.get('rtZWrite') or {}).get('name') if isinstance(st.get('rtZWrite'), dict) else st.get('rtZWrite'),
                    'zwritev': (st.get('rtZWrite') or {}).get('val') if isinstance(st.get('rtZWrite'), dict) else None,
                })
        out.setdefault(pf.get('m_Name'), {'props': [x.get('m_Name') for x in ((pf.get('m_PropInfo') or {}).get('m_Props') or [])],
                                          'tags': tags, 'blends': blends})
        want.discard(str(o.path_id))
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False, indent=1))
print('缺:', want)
for n, v in sorted(out.items()):
    print('==', n)
    print('   tags=%s' % v['tags'])
    for b in v['blends'][:3]:
        print('   blend[%s] src=%s(%s) dst=%s(%s) alpha=%s/%s  zwrite=%s(%s)' %
              (b['pass'], b['src'], b['srcv'], b['dst'], b['dstv'], b['srcA'], b['dstA'], b['zwrite'], b['zwritev']))
