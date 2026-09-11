# -*- coding: utf-8 -*-
"""battlearena1 Card Display Window (GO 419) 树 → JSON 中间文件 + 关键字段清单 (2026-08-27)"""
import json, os, sys
sys.stdout.reconfigure(encoding='utf-8')
BASE = 'D:/2/解包整理/07_场景/battlearena1'
OUT = 'D:/2/Warpforge_tools/scripts/tmp_cdw_tree.json'

INDEX = {}
NAME_BY_PID = {}
for t in ['GameObject', 'RectTransform', 'Transform', 'MonoBehaviour']:
    td = os.path.join(BASE, t)
    if not os.path.isdir(td):
        continue
    files = [os.path.join(td, f) for f in os.listdir(td) if f.endswith('.json')]
    def keyf(f):
        base = os.path.basename(f)[:-5]
        parts = base.rsplit('_', 1)
        if len(parts) == 2 and parts[1].lstrip('-').isdigit():
            return (0, 0) if base.count('_') == 1 else (1, int(parts[1]))
        return (2, 0)
    files.sort(key=keyf)
    for f in files:
        base = os.path.basename(f)[:-5]
        parts = base.rsplit('_', 1)
        if not (len(parts) == 2 and parts[1].lstrip('-').isdigit()):
            continue
        pid = int(parts[1])
        if pid in INDEX:
            continue
        try:
            d = json.load(open(f, encoding='utf-8'))
        except Exception:
            continue
        INDEX[pid] = d
        if t == 'GameObject':
            nm = d.get('m_Name')
            if isinstance(nm, str) and nm:
                NAME_BY_PID[pid] = nm

def get(pid):
    return INDEX.get(pid)

def build(gopid):
    go = get(gopid)
    if go is None:
        return {'go': gopid, 'missing': True}
    rtpid = None
    comps = []
    for c in go.get('m_Component', []):
        cp = c['component']['m_PathID']
        d = get(cp)
        if d is None:
            continue
        ks = set(d.keys())
        if rtpid is None and 'm_AnchoredPosition' in ks:
            rtpid = cp
        comps.append(cp)
    node = {'go': gopid, 'name': go.get('m_Name'), 'active': go.get('m_IsActive'),
            'comps': comps, 'child_gos': []}
    if rtpid is not None:
        rt = get(rtpid)
        node['rt'] = rtpid
        for c in rt.get('m_Children', []):
            cid = c.get('m_PathID')
            crt = get(cid)
            if crt is None:
                node['child_gos'].append({'rt': cid, 'missing': True})
                continue
            cgo = (crt.get('m_GameObject') or {}).get('m_PathID')
            if cgo is None:
                node['child_gos'].append({'rt': cid, 'go_missing': True})
                continue
            node['child_gos'].append(build(cgo))
    else:
        d = None
        for cp in comps:
            dd = get(cp)
            if dd is not None and 'm_LocalRotation' in dd and 'm_Father' in dd and 'm_AnchoredPosition' not in dd:
                d = dd
        if d:
            node['tr'] = [cp for cp in comps if 'm_AnchoredPosition' not in (get(cp) or {}).keys() and (get(cp) or {}).get('m_LocalRotation') is not None]
            for c in d.get('m_Children', []):
                cid = c.get('m_PathID')
                ctr = get(cid)
                if ctr is None:
                    node['child_gos'].append({'tr': cid, 'missing': True})
                    continue
                cgo = (ctr.get('m_GameObject') or {}).get('m_PathID')
                if cgo is None:
                    node['child_gos'].append({'tr': cid, 'go_missing': True})
                    continue
                node['child_gos'].append(build(cgo))
    return node

tree = build(419)
json.dump({'tree': tree,
           'pid_index': {str(k): (NAME_BY_PID.get(k), None) for k in INDEX}},
          open(OUT, 'w', encoding='utf-8'), ensure_ascii=False, default=str)

# ---- 关键字段清单 ----
def fnum(v, nd=5):
    if v is None:
        return '?'
    if isinstance(v, float):
        r = round(v, nd)
        return int(r) if r == int(r) and abs(r) < 1e15 else r
    return v

def col(c):
    if c is None:
        return '?'
    return '(%s,%s,%s,%s)' % (fnum(c.get('r'), 4), fnum(c.get('g'), 4), fnum(c.get('b'), 4), fnum(c.get('a'), 4))

def MBline(cp):
    d = get(cp)
    if d is None:
        return 'MB%s <missing>' % cp
    scr = (d.get('m_Script') or {}).get('m_PathID')
    parts = []
    if d.get('m_Enabled') == 0:
        parts.append('DIS')
    if d.get('m_IsActive') is False:
        parts.append('MBINACTIVE')
    if 'm_fontSize' in d:
        parts.append("TXT %r" % str(d.get('m_text', ''))[:60])
        parts.append('fs=%s/%s' % (fnum(d.get('m_fontSize')), fnum(d.get('m_fontSizeBase'))))
        if d.get('m_enableAutoSizing'):
            parts.append('auto[%s,%s]' % (fnum(d.get('m_fontSizeMin')), fnum(d.get('m_fontSizeMax'))))
        parts.append('fc=%s' % col(d.get('m_fontColor')))
        parts.append('H%s V%s sty%s' % (d.get('m_HorizontalAlignment'), d.get('m_VerticalAlignment'), d.get('m_fontStyle')))
        parts.append('fa=%s ovf=%s' % ((d.get('m_fontAsset') or {}).get('m_PathID'), d.get('m_overflowMode')))
    elif 'm_Sprite' in d and 'm_Color' in d:
        parts.append('IMG spr=%s %s ty=%s ray=%s' % (fnum((d.get('m_Sprite') or {}).get('m_PathID')), col(d.get('m_Color')), d.get('m_Type'), d.get('m_RaycastTarget')))
    if 'm_Interactable' in d and 'm_Transition' in d:
        cols = d.get('m_Colors') or {}
        parts.append('BTN tr=%s int=%s' % (d.get('m_Transition'), d.get('m_Interactable')))
    return 'MB%s(scr=%s) %s' % (cp, fnum(scr), ' | '.join(parts))

def flat(node, depth, out):
    out.append('- [GO %s] %s act=%s' % (node['go'], node['name'], node['active']))
    rt = get(node.get('rt')) if node.get('rt') else None
    if rt is not None:
        mn = rt.get('m_AnchorMin') or {}
        mx = rt.get('m_AnchorMax') or {}
        ap = rt.get('m_AnchoredPosition') or {}
        sd = rt.get('m_SizeDelta') or {}
        pv = rt.get('m_Pivot') or {}
        out.append('  RT%s an(%s,%s,%s,%s) p(%s,%s) sz(%s,%s) piv(%s,%s) fa=%s' % (
            node['rt'], fnum(mn.get('x')), fnum(mn.get('y')), fnum(mx.get('x')), fnum(mx.get('y')),
            fnum(ap.get('x')), fnum(ap.get('y')), fnum(sd.get('x')), fnum(sd.get('y')),
            fnum(pv.get('x')), fnum(pv.get('y')), (rt.get('m_Father') or {}).get('m_PathID')))
    for cp in node['comps']:
        d = get(cp)
        if d is None:
            out.append('  ?%s <missing>' % cp)
            continue
        if 'm_AnchoredPosition' in d:
            continue
        if 'm_LocalRotation' in d and 'm_Father' in d:
            lp = d.get('m_LocalPosition') or {}
            ls = d.get('m_LocalScale') or {}
            out.append('  TR%s pos(%s,%s,%s) scale(%s,%s,%s)' % (cp, fnum(lp.get('x')), fnum(lp.get('y')), fnum(lp.get('z')), fnum(ls.get('x')), fnum(ls.get('y')), fnum(ls.get('z'))))
            continue
        if 'm_Script' in d:
            out.append('  ' + MBline(cp))
    for ch in node['child_gos']:
        if 'missing' in ch or 'go_missing' in ch:
            out.append('  !child-rt %s broken' % ch)
            continue
        flat(ch, depth + 1, out)

out = []
flat(tree, 0, out)
print('\n'.join(out))
