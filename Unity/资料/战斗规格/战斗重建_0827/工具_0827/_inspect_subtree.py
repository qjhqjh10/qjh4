#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""临时分析工具: 从原始 Unity JSON 索引 GO/RT, 按 RT PathID dump 子树 (含 raw RT 值 + 组件 PID 列表)"""
import json, os, sys, glob

sys.stdout.reconfigure(encoding='utf-8')
SRC = 'd:/2/解包整理/07_场景/battlearena1'

go_index, rt_index, tr_index, tr_by_go = {}, {}, {}, {}
go_name, name_idx = {}, {}

def load():
    for t in ['GameObject', 'RectTransform', 'Transform']:
        td = os.path.join(SRC, t)
        for f in glob.glob(os.path.join(td, '*.json')):
            base = os.path.basename(f).rsplit('.json', 1)[0]
            m = base.rsplit('_', 1)
            pid = None
            if len(m) == 2 and m[1].lstrip('-').isdigit():
                pid = int(m[1])
            if pid is None:
                continue
            try:
                d = json.load(open(f, encoding='utf-8'))
            except Exception:
                continue
            if t == 'GameObject':
                if pid not in go_index:
                    go_index[pid] = d
                    nm = d.get('m_Name', '?')
                    go_name[pid] = nm
                    name_idx.setdefault(nm, []).append(pid)
            elif t == 'RectTransform':
                if pid not in rt_index:
                    rt_index[pid] = d
            else:
                if pid not in tr_index:
                    tr_index[pid] = d
                    gop = d.get('m_GameObject', {}).get('m_PathID')
                    if gop: tr_by_go.setdefault(gop, []).append(d)

def go_of_rt(rtid):
    rt = rt_index.get(rtid)
    if not rt: return None
    return rt.get('m_GameObject', {}).get('m_PathID')

def fmt(v):
    return '%.4g' % v if isinstance(v, (int, float)) else str(v)

def scale_of_goid(goid):
    sc = (1.0, 1.0)
    for tr in tr_by_go.get(goid, []):
        s = tr.get('m_LocalScale') or {}
        x = s.get('x', 1); y = s.get('y', x)
        if x != 1 or y != 1:
            sc = (x, y)
    return sc

def dump(rtid, depth=0, maxd=99):
    rt = rt_index.get(rtid)
    if rt is None:
        print('  ' * depth + '?? RT %s 不存在' % rtid); return
    goid = go_of_rt(rtid)
    nm = go_name.get(goid, '?')
    act = go_index.get(goid, {}).get('m_IsActive')
    a = rt.get('m_AnchorMin') or {}; b = rt.get('m_AnchorMax') or {}
    ap = rt.get('m_AnchoredPosition') or {}; sd = rt.get('m_SizeDelta') or {}
    pv = rt.get('m_Pivot') or {}
    sc = scale_of_goid(goid)
    comps = [c.get('component', {}).get('m_PathID') for c in go_index.get(goid, {}).get('m_Component', [])]
    print('%s[%s %s] RT %s anchor(%.3f,%.3f)-(%.3f,%.3f) pos(%.2f,%.2f) size(%.2f,%.2f) pivot(%.2f,%.2f)%s comps=%s' % (
        '  ' * depth, nm, 'A' if act else 'i', rtid,
        a.get('x',0),a.get('y',0),b.get('x',0),b.get('y',0),
        ap.get('x',0), ap.get('y',0), sd.get('x',0), sd.get('y',0), pv.get('x',0.5), pv.get('y',0.5),
        (' scale=(%.5f,%.5f)' % sc) if sc != (1.0,1.0) else '', comps))
    if depth >= maxd: return
    for c in rt.get('m_Children', []):
        dump(c.get('m_PathID'), depth + 1, maxd)

if __name__ == '__main__':
    load()
    for arg in sys.argv[1:]:
        print('====== %s ======' % arg)
        dump(int(arg))
