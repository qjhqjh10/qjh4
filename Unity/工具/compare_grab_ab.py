# -*- coding: utf-8 -*-
"""比较「抓屏 Feature 开 / 关」两组小批 sweep：
   在两侧 lit 都 >=60 的帧上取 sum 比值的中位数（与 `工具/analyze_sweep.py` 同一口径），
   看抓屏这一族在两种尺子下的读数差多少、分档（Z/E/D/W）翻了几条。
"""
import io, os, statistics as st

TMP = 'd:/4/_tmp_view'


def load(path):
    """→ {(效果名, 时刻): (lit, sum)}"""
    d = {}
    if not os.path.exists(path):
        print('缺文件:', path); return d
    for i, l in enumerate(io.open(path, encoding='utf-8').read().splitlines()):
        if i == 0:
            continue
        p = l.split('\t')
        if len(p) < 4:
            continue
        try:
            d[(p[0], p[1])] = (int(float(p[2])), float(p[3]))
        except Exception:
            pass
    return d


def ratios(o, e):
    """→ {效果名: (比值, 用了几帧)}"""
    out = {}
    names = {k[0] for k in o}
    for n in names:
        rs = []
        for (nn, t), (lit_o, sum_o) in o.items():
            if nn != n:
                continue
            ee = e.get((n, t))
            if not ee:
                continue
            lit_e, sum_e = ee
            if lit_o >= 60 and lit_e >= 60 and sum_o > 0:
                rs.append(sum_e / sum_o)
        if rs:
            out[n] = (st.median(rs), len(rs))
    return out


def bucket(r):
    if r is None: return '?'
    import math
    x = abs(math.log(r))
    if x <= 0.336: return 'Z'
    return 'E+' if r > 1 else 'E-'


A_o, A_e = load(f'{TMP}/ab_A_orig.tsv'), load(f'{TMP}/ab_A_exp.tsv')
B_o, B_e = load(f'{TMP}/ab_B_orig.tsv'), load(f'{TMP}/ab_B_exp.tsv')
ra, rb = ratios(A_o, A_e), ratios(B_o, B_e)

print(f'可比的（A 组）{len(ra)} 条 ·（B 组）{len(rb)} 条')
common = sorted(set(ra) & set(rb))
flip = [n for n in common if bucket(ra[n][0]) != bucket(rb[n][0])]
print(f'\n两组都有数 {len(common)} 条；**分档翻转 {len(flip)} 条**')
print(f"{'效果名':44s} {'A(Feature开)':>13s} {'B(关)':>9s}  分档")
for n in common:
    va, vb = ra[n][0], rb[n][0]
    mark = '  ← 翻' if bucket(va) != bucket(vb) else ''
    print(f'{n[:43]:44s} {va:13.3f} {vb:9.3f}  {bucket(va)} → {bucket(vb)}{mark}')
import math
if common:
    d = [abs(math.log(rb[n][0] / ra[n][0])) for n in common if ra[n][0] > 0]
    d.sort()
    print(f'\n|ln(比值_B / 比值_A)| 中位 {st.median(d):.3f} · 90 分位 {d[int(len(d)*0.9)-1]:.3f} · 最大 {d[-1]:.3f}')
