# -*- coding: utf-8 -*-
"""全仓扫：还剩哪些【带引号的旧 id 字面量】（.cs / .py / .json）。只读。"""
import io, json, os, sys

ROOT = 'd:/4/'
IDMAP = json.load(io.open(ROOT + '_tmp_view/p1_1014/idmap.json', encoding='utf-8'))
ROOTS = ['Unity/MyGame/Assets', 'Unity/工具', 'Unity/数据']
SKIP = ('Resources/Art', 'Resources/Art/')

hits = []
for r in ROOTS:
    for dp, dn, fn in os.walk(ROOT + r):
        rel = os.path.relpath(dp, ROOT).replace('\\', '/')
        if rel.startswith('Unity/MyGame/Assets/CardPresentation/Resources/Art'):
            continue
        for f in fn:
            if not f.endswith(('.cs', '.py', '.json')):
                continue
            p = os.path.join(dp, f)
            try:
                t = io.open(p, encoding='utf-8').read()
            except Exception as e:
                print('  (unreadable)', p, e)
                continue
            for old, new in IDMAP.items():
                n = t.count('"' + old + '"')
                if n:
                    hits.append((os.path.relpath(p, ROOT).replace('\\', '/'), old, new, n))

byfile = {}
for p, old, new, n in hits:
    byfile.setdefault(p, []).append((old, new, n))
for p, v in sorted(byfile.items()):
    print(p)
    for old, new, n in v:
        print('    %-40s -> %s  x%d' % (old, new, n))
print('files: %d  occurrences: %d' % (len(byfile), sum(n for _, _, _, n in hits)))
