#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""把 4 场粒子用到的**全部**原版 shader 的属性表 / SubShader 队列 tag / pass 混合取出来
（含 build_shader_props.py 因为跳小包而漏掉的那个）。

🔴 **2026-10-06 修（A160 / A152 A6 连带）**：原来这里有
`names = {si[k]: k for k in want if k in si}` —— **把 `shader_index.json` 那张撞号的表反过来用**
（`名字 → pid`）。实测 Shader 同 pid 不同名 **6 条**（来源全是 `scenes_scenes_battlearena*`，
pid 28–33），反查时**后面的名字会把前面的覆盖掉**，于是「需要哪几个 shader」这句。
修法 = **不用反查**：这一趟本来就在逐对象读 `pf.get('m_Name')` ⇒ 名字**直接取对象自己的**；
索引只当「pid 在不在」的存在性检查，撞号的 pid 点名（`_amb`）而不是替它挑一个名字。
"""
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
# 🔴 名字只在**扫到对象那一刻**从对象自己身上取（上面那条注释说的就是这件事）。
amb = si.get('_amb') or {}
print('需要 %d 个 shader（按 pathID 找；名字扫到再印，**不从索引反查**）' % len(want))
for k in sorted(want):
    if k in amb:
        print('    ⚠️ pid=%s 在索引里**撞号**（%s）—— 下面按各包自己的对象印，不替它选'
              % (k, ' ｜ '.join('%s(%s/%s)' % (n, f, c) for n, f, c in amb[k][:3])))

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
        rec = {'props': [x.get('m_Name') for x in ((pf.get('m_PropInfo') or {}).get('m_Props') or [])],
               'tags': tags, 'blends': blends,
               # A160：把「它在哪一份 CAB」也记下来 —— 同名不同包同类的情况能看出来源
               'bundle': f, 'cab': getattr(getattr(o, 'assets_file', None), 'name', None),
               'pathId': str(o.path_id)}
        out.setdefault(pf.get('m_Name'), rec)
        want.discard(str(o.path_id))
    del env
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False, indent=1))
print('缺:', want)
for n, v in sorted(out.items()):
    print('==', n)
    print('   tags=%s' % v['tags'])
    for b in v['blends'][:3]:
        print('   blend[%s] src=%s(%s) dst=%s(%s) alpha=%s/%s  zwrite=%s(%s)' %
              (b['pass'], b['src'], b['srcv'], b['dst'], b['dstv'], b['srcA'], b['dstA'], b['zwrite'], b['zwritev']))
