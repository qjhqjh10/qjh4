#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""shader pathID → {name, props[]}（属性表 —— 用来区分「材质值写死在材质里」vs「shader 没这个名字=死值」）。

🔴 **2026-10-06 修（A160 / A152 A6 连带）**：与 `build_shader_index.py` **同一张表、同一个病**
（全局裸 pid → shader，先到先得、零歧义检测）。实测 Shader 同 pid 不同名 **6 条**，
来源全是 `scenes_scenes_battlearena*`（pid 28–33）。而且这里更糟：`idx[str(o.path_id)] = {...}`
是**覆盖**（last-write-wins）⇒ 撞号时取到哪个**取决于扫到哪张文件**。
修法同 `build_shader_index.py`：顶层格式不变（`diff.py:80` 是 `shprops.get(str(pid))`），
值改**首见为准**，另存 `_amb` 撞号表，并**扫完所有文件**（原来 `while need and ...` 会早停）。
"""
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
seen = {}          # pid → [(名字, 包, CAB), …]（判撞号用）
files = [f for f in sorted(os.listdir(AA)) if f.endswith('.bundle')]
for i, f in enumerate(files):
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
            k = str(o.path_id)
            idx.setdefault(k, {'name': nm, 'props': props})   # 🔴 首见为准（原来是无条件覆盖）
            cab = getattr(getattr(o, 'assets_file', None), 'name', None)
            seen.setdefault(k, []).append((nm, f, cab))
            need.discard(k)
        del env
    except Exception as e:
        print('  !! %s %s' % (f, e))
    print('  [%d/%d] %s  有 %d  还缺 %d' % (i + 1, len(files), f, len(idx), len(need)))

amb = {k: [[n, f, c] for n, f, c in v] for k, v in seen.items() if len({x[0] for x in v}) > 1}
out = dict(idx)
out['_amb'] = amb
io.open(OUT, 'w', encoding='utf-8', newline='\n').write(json.dumps(out, ensure_ascii=False, indent=1))
print('OK %d，还缺 %s' % (len(idx), sorted(need)))
if amb:
    print('🔴 **%d 个 pid 有多个 shader 名**（`_amb` 里全列出来了）：' % len(amb))
    for k, v in sorted(amb.items()):
        print('    pid=%-8s %s' % (k, ' ｜ '.join('%s(%s/%s)' % (n, f, c) for n, f, c in v[:4])))
else:
    print('✅ 没扫到同 pid 不同名的 shader')
