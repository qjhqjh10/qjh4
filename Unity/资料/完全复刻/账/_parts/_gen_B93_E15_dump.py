# -*- coding: utf-8 -*-
"""B93_E15 review dump: join Q3 <-> main ledger, report match quality. Read-only."""
import io, os, json, re

LEDGER = "d:/4/Unity/资料/完全复刻/账"
Q3 = "d:/4/Unity/资料/完全复刻/判据/B93_Q3_无落点220.tsv"
OUT = "d:/4/_tmp_view/e15"
os.makedirs(OUT, exist_ok=True)

def rd(p):
    b = io.open(p, 'rb').read()
    if b[:3] == b'\xef\xbb\xbf':
        b = b[3:]
    return b.decode('utf-8').replace('\r\n', '\n').split('\n')

def splitkey(k):
    i = k.rfind('(')
    head, params = (k[:i], k[i:]) if i > 0 else (k, '')
    j = head.rfind('.')
    return (head[:j], head[j + 1:], params)

def methname(s):
    i = s.find('(')
    return s[:i] if i > 0 else s

def classbase(s):
    i = s.find('<')
    return s[:i] if i > 0 else s

def nparams(params):
    s = params.strip()
    if s in ('', '()'):
        return 0
    return s.count(',') + 1

def keycore(cls, meth, params):
    return (classbase(cls), methname(meth), nparams(params))

def match_row(r, L):
    qc, qm, qp = splitkey(r['key'])
    qcore = keycore(qc, qm, qp)
    exact, fuzzy, weak = [], [], []
    for i in range(1, len(L)):
        c = L[i].split('\t')
        if len(c) < 3:
            continue
        if c[1] + '.' + c[2] == r['key']:
            exact.append(i); continue
        lc, lm = c[1], c[2]
        ip = lm.rfind('(')
        lparams = lm[ip:] if ip > 0 else ''
        if keycore(lc, lm, lparams) == qcore:
            fuzzy.append(i); continue
        if methname(lm) == methname(qm) and (keycore(lc, lm, lparams)[0].endswith(keycore(qc, qm, qp)[0]) or keycore(qc, qm, qp)[0].endswith(keycore(lc, lm, lparams)[0])):
            weak.append(i)
    cand0 = r['n'] - 1
    if 0 <= cand0 < len(L) and cand0 in exact:
        return cand0, 'exact@n-1'
    for lst, tag in ((exact, 'exact'), (fuzzy, 'fuzzy'), (weak, 'weak')):
        if len(lst) == 1:
            return lst[0], tag
        if len(lst) > 1:
            lst.sort(key=lambda x: abs(x - cand0))
            return lst[0], tag + '-multi'
    return None, 'none'

rows = []
q = rd(Q3)
for ln in q[1:]:
    if not ln.strip():
        continue
    c = ln.split('\t')
    assert len(c) == 7, (len(c), ln[:120])
    rows.append(dict(dom=c[0], n=int(c[1]), key=c[2], tier=c[3], land=c[4], ev=c[5], gap=c[6]))

tables = {}
for fn in sorted(os.listdir(LEDGER)):
    if not fn.endswith('.tsv'):
        continue
    dom = fn[:-4]
    L = rd(os.path.join(LEDGER, fn))
    tables[dom] = L

out = []
used = {}
dups = []
OVERRIDE = {
    ('日常_奖励', 424): ('日常_奖励', 423),
    ('泛型与启动链_补读', 609): ('未分类_其他', 180),
    ('泛型与启动链_补读', 610): ('未分类_其他', 179),
    ('泛型与启动链_补读', 611): ('未分类_其他', 181),
}
for r in rows:
    ov = OVERRIDE.get((r['dom'], r['n']))
    if ov:
        r['dom_actual'] = ov[0]
        L = tables[ov[0]]
        idx, how = ov[1], 'override'
    else:
        L = tables.get(r['dom'])
        idx, how = match_row(r, L) if L else (None, 'nodom')
    if False:
        pass
    if idx is None:
        # cross-domain fallback: the row may have moved between tables (缩表)
        for dom2, L2 in tables.items():
            i2, h2 = match_row(r, L2)
            if i2 is not None and h2.startswith('exact'):
                L, idx, how = L2, i2, h2 + '@' + dom2
                r['dom_actual'] = dom2
                break
    cur = '?'
    dom_act = r.get('dom_actual', r['dom'])
    if idx is not None:
        cs = L[idx].split('\t')
        cur = cs[5] if len(cs) > 5 else '?'
        k = (dom_act, idx)
        if k in used:
            dups.append((r['dom'], r['n'], r['key'], used[k]))
        used[k] = r['key']
    out.append((r['dom'], r['n'], dom_act + ':' + str(idx), how, cur, r['tier'], r['key'], r['land'], r['gap']))
    r['dom_actual'] = dom_act
    r['idx_actual'] = idx

print("dups(same ledger row hit twice):", dups)
print("unmatched:", [(o[0], o[1], o[6]) for o in out if o[2] is None])

def cell(s):
    return (s or '').replace('\t', ' ').replace('\n', ' ')

with io.open(OUT + "/review.tsv", 'w', encoding='utf-8', newline='\n') as f:
    f.write("域\tQ3行\t真行idx\t匹配\t现状态\tQ3档\t原版类.方法\t等价物\t还差什么\n")
    for o in out:
        f.write('\t'.join(cell(str(x)) for x in o) + '\n')
print("wrote", OUT + "/review.tsv", len(out))
