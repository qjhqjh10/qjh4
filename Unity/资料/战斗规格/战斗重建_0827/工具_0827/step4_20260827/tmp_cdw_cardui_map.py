# -*- coding: utf-8 -*-
"""5 张 CardUI 逐元素对照: (CardUI 名, 元素名, 深度路径) -> {GO pid, RT pid, MB pids} (2026-08-27)"""
import json, os, sys
sys.stdout.reconfigure(encoding='utf-8')
BASE = 'D:/2/解包整理/07_场景/battlearena1'
INDEX = {}
for t in ['GameObject', 'RectTransform', 'Transform', 'MonoBehaviour']:
    td = os.path.join(BASE, t)
    for f in os.listdir(td):
        if not f.endswith('.json'):
            continue
        base = f[:-5]
        parts = base.rsplit('_', 1)
        if not (len(parts) == 2 and parts[1].lstrip('-').isdigit()):
            continue
        pid = int(parts[1])
        if pid in INDEX:
            continue
        try:
            INDEX[pid] = json.load(open(os.path.join(td, f), encoding='utf-8'))
        except Exception:
            pass

def node_info(gopid):
    go = INDEX.get(gopid)
    if go is None:
        return (gopid, None, [])
    rt = None
    mbs = []
    for c in go.get('m_Component', []):
        cp = c['component']['m_PathID']
        d = INDEX.get(cp)
        if d is None:
            continue
        if 'm_AnchoredPosition' in d:
            rt = cp
        if 'm_Script' in d:
            mbs.append(cp)
    return (gopid, rt, mbs)

def children_of(gopid):
    go = INDEX.get(gopid)
    if go is None:
        return []
    pids = []
    for c in go.get('m_Component', []):
        cp = c['component']['m_PathID']
        d = INDEX.get(cp)
        if d is None:
            continue
        ch = d.get('m_Children')
        if not ch:
            continue
        for cc in ch:
            cid = cc.get('m_PathID')
            crt = INDEX.get(cid)
            if crt is None:
                continue
            cgo = (crt.get('m_GameObject') or {}).get('m_PathID')
            if cgo is not None:
                pids.append((cid, cgo))
    return pids

def walk_map(gopid, prefix, out):
    go = INDEX.get(gopid)
    if go is None:
        return
    name = go.get('m_Name')
    out[prefix + '|' + str(name)] = node_info(gopid)
    # 用 first RT/TR 找 children
    seen = set()
    for c in go.get('m_Component', []):
        cp = c['component']['m_PathID']
        d = INDEX.get(cp)
        if d is None or cp in seen:
            continue
        ch = d.get('m_Children')
        if not ch:
            continue
        for cc in ch:
            cid = cc.get('m_PathID')
            if cid in seen:
                continue
            seen.add(cid)
            crt = INDEX.get(cid)
            if crt is None:
                continue
            cgo = (crt.get('m_GameObject') or {}).get('m_PathID')
            if cgo is not None:
                walk_map(cgo, prefix + '/' + name, out)

cards = [('(4)', 971), ('(3)', 315), ('(2)', 635), ('(1)', 1128), ('base', 705)]
maps = {}
for nm, g in cards:
    m = {}
    walk_map(g, '', m)
    maps[nm] = m

# 打印按元素路径行 = 树末节点名+父链 (简化: 用完整路径)
allkeys = set()
for m in maps.values():
    allkeys.update(m.keys())
def shortkey(k):
    parts = k.split('|')
    return '|'.join(parts[-2:])  # 父名|自身名
rows = {}
for k in sorted(allkeys):
    key = shortkey(k)
    rows.setdefault(key, {})
    for nm, m in maps.items():
        v = m.get(k)
        if v:
            go, rt, mbs = v
            rows[key][nm] = '%s/%s/%s' % (go, rt or '-', ','.join(str(x) for x in mbs))
print('路径 | (4) | (3) | (2) | (1) | base   [GO/RT/MBs]')
for key in sorted(rows):
    r = rows[key]
    print('%s | %s' % (key, ' | '.join(r.get(n, '-') for n in ['(4)', '(3)', '(2)', '(1)', 'base'])))
