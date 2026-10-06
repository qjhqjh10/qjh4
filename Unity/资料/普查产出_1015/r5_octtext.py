#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""R5 只读探针：在所有「菜单族」bundle 里找【OctagonUI 图 + 子树里有 TMP 文字】的节点。
用法: python r5_octtext.py
"""
import sys, os, io, json
sys.stdout.reconfigure(encoding='utf-8')
sys.path.insert(0, 'd:/4/Unity/工具')
os.chdir('d:/2/新解包资源/assets_full')
import menu_dump as MD
import menu_rect as MR

TARGETS = ['bundle_menus_assets_all', 'bundle_menusharedresources_assets_all',
           'bundle_mainmenualwaysloaded_assets_all', 'bundle_generalgamewindows_assets_all',
           'bundle_liveopsmenuimages_assets_all', 'bundle_liveopsicons_assets_all',
           'bundle_duplicateassetisolation_assets_all', 'bundle_staticgeneralassets_assets_all']

smap = MD.sprite_pid_map(quiet=True)
octpids = {str(p): nm for p, nm in smap.items() if 'octagon' in (nm or '').lower()}
print('# octagon sprite pids:', octpids)

def find_tmp(b, rtpid, out, depth=4, guard=0):
    if guard > 6:
        return
    for c in (b.children(rtpid) or []):
        go = b.go_of_rt(c)
        if go is not None:
            g = b.go.get(str(go))
            if g:
                for comp in g.get('m_Component', []):
                    cp = str(comp['component']['m_PathID'])
                    mb = MR.load(os.path.join(MR.BUNDLES, cur_b, 'MonoBehaviour'), 'MonoBehaviour', cp)
                    if mb and 'm_text' in mb:
                        out.append('        TMP@%s: %r' % (b.go_name_of_rt(c), mb.get('m_text')))
        if depth > 0:
            find_tmp(b, c, out, depth - 1, guard + 1)

for cur_b in TARGETS:
    p = os.path.join(MR.BUNDLES, cur_b)
    if not os.path.isdir(p):
        continue
    b = MR.Bundle(p)
    d = os.path.join(p, 'MonoBehaviour')
    if not os.path.isdir(d):
        continue
    hits = []
    for fn in os.listdir(d):
        try:
            j = json.load(io.open(os.path.join(d, fn), encoding='utf-8'))
        except Exception:
            continue
        s = j.get('m_Sprite')
        if isinstance(s, dict) and str(s.get('m_PathID')) in octpids:
            go = (j.get('m_GameObject') or {}).get('m_PathID')
            rt = b.rt_of_go(go)
            if rt is None:
                continue
            texts = []
            find_tmp(b, rt, texts)
            hits.append((b.go_name_of_rt(rt), octpids[str(s['m_PathID'])], rt, texts))
    print('==== %s : %d 个 octagon 节点' % (cur_b, len(hits)))
    for nm, sp, rt, texts in hits:
        path = []
        cur = rt
        g = 0
        while cur and g < 12:
            path.append(b.go_name_of_rt(cur) or '?')
            cur = b.parent(cur)
            g += 1
        print('  %-28s %-26s rt=%s' % (nm, sp, rt))
        print('     父链:', ' < '.join(path[1:]))
        for t in texts:
            print(t)
