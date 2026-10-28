# -*- coding: utf-8 -*-
"""B93_E15 helper: dump / verify Q3 220 rows against main ledger. READ-ONLY on main tables."""
import io, os, sys, json

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

def match_row(r, L):
    qc, qm, qp = splitkey(r['key'])
    qmb, qcb = methname(qm), classbase(qc)
    exact, fuzzy, weak = [], [], []
    for i in range(1, len(L)):
        c = L[i].split('\t')
        if len(c) < 3:
            continue
        if c[1] + '.' + c[2] == r['key']:
            exact.append(i); continue
        lc, lm = c[1], c[2]
        if classbase(lc) == qcb and methname(lm) == qmb:
            fuzzy.append(i); continue
        if methname(lm) == qmb and (qcb in lc or lc in qcb):
            weak.append(i)
    for cand in ([r['n'] - 1] if 0 <= r['n'] - 1 < len(L) else []):
        if cand in exact:
            return cand, 'exact@n-1'
    if len(exact) == 1:
        return exact[0], 'exact'
    if len(exact) > 1:
        # prefer closest to n
        exact.sort(key=lambda x: abs(x - (r['n'] - 1)))
        return exact[0], 'exact-multi'
    if len(fuzzy) == 1:
        return fuzzy[0], 'fuzzy'
    if len(fuzzy) > 1:
        fuzzy.sort(key=lambda x: abs(x - (r['n'] - 1)))
        return fuzzy[0], 'fuzzy-multi'
    if len(weak) == 1:
        return weak[0], 'weak'
    if len(weak) > 1:
        weak.sort(key=lambda x: abs(x - (r['n'] - 1)))
        return weak[0], 'weak-multi'
    return None, 'none'

def main():
    q = rd(Q3)
    hdr = q[0].split('\t')
    assert hdr[0] == '账域', hdr
    rows = []
    for ln in q[1:]:
        if not ln.strip():
            continue
        c = ln.split('\t')
        assert len(c) == 7, (len(c), ln[:200])
        rows.append(dict(dom=c[0], n=int(c[1]), key=c[2], tier=c[3], land=c[4], ev=c[5], gap=c[6]))
    print("Q3 rows:", len(rows))

    # load main ledgers
    tables = {}
    for fn in sorted(os.listdir(LEDGER)):
        if not fn.endswith('.tsv'):
            continue
        dom = fn[:-4]
        L = rd(os.path.join(LEDGER, fn))
        h = L[0].split('\t')
        tables[dom] = (h, L)
    print("domains:", len(tables))

    # check header consistency
    base = None
    for dom, (h, L) in tables.items():
        if base is None:
            base = h
        elif h != base:
            print("HDR DIFF", dom, h)

    # verify (dom, n) mapping: does table row n exist and does 原版类.方法 match?
    mism = []
    status = {}
    for r in rows:
        t = tables.get(r['dom'])
        if t is None:
            mism.append((r['dom'], r['n'], 'NO DOMAIN FILE'))
            continue
        h, L = t
        found, how = match_row(r, L)
        if found is None:
            mism.append((r['dom'], r['n'], r['key'], how))
            continue
        status[(r['dom'], r['n'])] = (found, L[found].split('\t'), how)

    print("mismatch:", len(mism))
    for m in mism[:40]:
        print("  ", m)

    # current status distribution of matched rows
    from collections import Counter
    c1 = Counter(); c2 = Counter(); how_c = Counter()
    for k, (i, cs, how) in status.items():
        c1[cs[5]] += 1
        how_c[how] += 1
    print("current status:", dict(c1))
    print("match method:", dict(how_c))
    # tier vs current status cross
    cross = Counter()
    for r in rows:
        k = (r['dom'], r['n'])
        if k in status:
            cross[(r['tier'], status[k][1][5])] += 1
        else:
            cross[(r['tier'], 'MISS')] += 1
    print("tier->cur:", dict(cross))

    json.dump({k[0] + '|' + str(k[1]): [v[0], v[1], v[2]] for k, v in status.items()}, io.open(OUT + "/map.json", "w", encoding="utf-8"), ensure_ascii=False)
    json.dump(rows, io.open(OUT + "/q3.json", "w", encoding="utf-8"), ensure_ascii=False)

main()
