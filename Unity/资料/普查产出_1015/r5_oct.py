#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R5 只读探针：在 bundle 里按 sprite 名找「谁在用它」（Image 的 m_Sprite.m_PathID）。
用法: python r5_oct.py <bundle> <sprite名子串> [--paths]
"""
import sys, os, io, json
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'd:/4/Unity/工具')
os.chdir('d:/2/新解包资源/assets_full')
import menu_dump as MD
import menu_rect as MR

bundle = sys.argv[1]
needle = sys.argv[2].lower()
want_paths = '--paths' in sys.argv

smap = MD.sprite_pid_map(quiet=True)
hits = {pid: nm for pid, nm in smap.items() if needle in (nm or '').lower()}
print('# sprite 名命中 %d 条:' % len(hits))
for pid, nm in sorted(hits.items(), key=lambda kv: kv[1]):
    print('   %s  pid=%s' % (nm, pid))
pids = set(str(p) for p in hits)

b = MR.Bundle(os.path.join(MR.BUNDLES, bundle))
d = os.path.join(MR.BUNDLES, bundle, 'MonoBehaviour')
res = []
for fn in os.listdir(d):
    try:
        j = json.load(io.open(os.path.join(d, fn), encoding='utf-8'))
    except Exception:
        continue
    s = j.get('m_Sprite')
    if isinstance(s, dict) and str(s.get('m_PathID')) in pids:
        go = (j.get('m_GameObject') or {}).get('m_PathID')
        res.append((fn, go, str(s.get('m_PathID'))))

print('# 用它的 MB: %d 个' % len(res))
for fn, go, sp in res:
    rt = b.rt_of_go(go)
    path = []
    cur = rt
    guard = 0
    while cur and guard < 40:
        path.append(b.go_name_of_rt(cur) or '?')
        cur = b.parent(cur)
        guard += 1
    print('%-46s sprite=%s  %s' % (fn, sp, ' < '.join(path)))
