# -*- coding: utf-8 -*-
"""D26 普查：把全仓 `.cs` 里所有 `rule_core` 的出现逐条列出来并分类。

只读，不写任何生产文件。输出 -> /tmp/d26_census.txt
"""
import io, os, collections

ROOTS = ['RuleEngine', 'CardPresentation']
BASE = 'D:/4/Unity/MyGame/Assets'

WEAK = ['我们自己的', '上一版复刻', '上一版 Godot', '我们上一版', '不是原版',
        '旁证', '非权威', '非原版', '更正', '那份 .gd', '那份 `.gd`', 'gd 只是']
STRONG = ['判据', '权威', '规格书', '照抄', '参考实现', '出处', '语义来源', '基准', '语义照']

rows = []
for root in ROOTS:
    for dp, dn, fn in os.walk(os.path.join(BASE, root)):
        for f in fn:
            if not f.endswith('.cs'):
                continue
            full = os.path.join(dp, f)
            rel = full.replace(BASE + os.sep, '').replace(os.sep, '/')
            try:
                txt = io.open(full, 'r', encoding='utf-8', newline='').read()
            except Exception:
                continue
            for i, l in enumerate(txt.split('\n')):
                if 'rule_core' in l:
                    rows.append((rel, i + 1, l.rstrip()))

out = []
out.append('TOTAL %d' % len(rows))
c = collections.Counter(r[0] for r in rows)
out.append('FILES %d' % len(c))
kind = collections.Counter()
for p, n, l in rows:
    isweak = any(k in l for k in WEAK)
    isstrong = any(k in l for k in STRONG)
    kind[(isweak, isstrong)] += 1
out.append('weak/strong 分布: %s' % dict(kind))
out.append('')
out.append('== 逐文件 ==')
for f, n in sorted(c.items(), key=lambda x: -x[1]):
    out.append('%4d  %s' % (n, f))
out.append('')
out.append('== 仍然【没降级 + 用强词】的行（优先要改的） ==')
for p, n, l in rows:
    isweak = any(k in l for k in WEAK)
    isstrong = any(k in l for k in STRONG)
    if (not isweak) and isstrong:
        out.append('%s:%d | %s' % (p, n, l))
out.append('')
out.append('== 仍然【没降级】但没用强词的行 ==')
for p, n, l in rows:
    isweak = any(k in l for k in WEAK)
    isstrong = any(k in l for k in STRONG)
    if (not isweak) and (not isstrong):
        out.append('%s:%d | %s' % (p, n, l))

io.open('D:/4/Unity/资料/普查产出_第八会话/_b1_d26_census.txt', 'w', encoding='utf-8', newline='\n').write('\n'.join(out))
print('ok', len(rows), len(c))
