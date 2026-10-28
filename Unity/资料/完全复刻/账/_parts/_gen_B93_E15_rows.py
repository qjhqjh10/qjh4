# -*- coding: utf-8 -*-
"""Dump current main-ledger rows for the B93_E15 change set (read-only)."""
import io, os, json

LEDGER = "d:/4/Unity/资料/完全复刻/账"
OUT = "d:/4/_tmp_view/e15"

def rd(p):
    b = io.open(p, 'rb').read()
    if b[:3] == b'\xef\xbb\xbf':
        b = b[3:]
    return b.decode('utf-8').replace('\r\n', '\n').split('\n')

diffs = json.load(io.open(OUT + '/diffs.json', encoding='utf-8'))
cache = {}
out = []
for r in diffs:
    if r['cur'] == 'A':
        continue  # A rows are protected
    dom, idx = r['dom_act'], int(r['idx'])
    if dom not in cache:
        cache[dom] = rd(os.path.join(LEDGER, dom + '.tsv'))
    c = cache[dom][idx].split('\t')
    out.append((dom, idx, r['tier'], r['cur'], c, r['key'], r['ev'], r['gap']))

# the 4 architectural D rows (currently D -> B)
ARCH = [('战斗逻辑驱动', 1277), ('日常_奖励', 309), ('日常_奖励', 485), ('日常_奖励', 522)]
for dom, idx in ARCH:
    if dom not in cache:
        cache[dom] = rd(os.path.join(LEDGER, dom + '.tsv'))
    c = cache[dom][idx].split('\t')
    out.append((dom, idx, 'B', c[5], c, c[1] + '.' + c[2], '(architectural)', '(arch)'))

with io.open(OUT + '/rows.txt', 'w', encoding='utf-8', newline='\n') as f:
    for dom, idx, tier, cur, c, key, ev, gap in out:
        f.write("=" * 100 + "\n")
        f.write(f"[{cur} -> {tier}] {dom}:{idx}  KEY={key}\n")
        f.write(f"  判据出处: {c[3]}\n")
        f.write(f"  判据要点: {c[4]}\n")
        f.write(f"  我们的落点: {c[6]}\n")
        f.write(f"  差异: {c[7]}\n")
        f.write(f"  等级: {c[8]} | 轮次: {c[9]}\n")
        f.write(f"  备注: {c[10]}\n")
        f.write(f"  Q3EV: {ev[:400]}\n")
        f.write(f"  Q3GAP: {gap[:300]}\n")
print("wrote rows.txt", len(out))
