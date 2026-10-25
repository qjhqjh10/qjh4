# -*- coding: utf-8 -*-
"""校验：每一条 A 行，落点那一行声明的名字若与原版方法名不同，落点里**必须**有「（等价实现/（实现 =」注解。"""
import io, re

D = r'D:/4/Unity/资料/完全复刻/账/'
ROOTS = [r'D:/4/Unity/', r'D:/4/Unity/MyGame/Assets/', r'D:/4/Unity/MyGame/Assets/CardPresentation/']
REF = re.compile(r'([\w\-./\\]+\.cs):(\d+)')


def find(rel):
    for r in ROOTS:
        try:
            p = r + rel.replace('\\', '/')
            io.open(p, encoding='utf-8').read(1)
            return p
        except Exception:
            pass
    return None


def dn_at(p, ln):
    ls = io.open(p, encoding='utf-8', errors='replace').read().split('\n')
    s = ls[ln - 1].strip() if ln - 1 < len(ls) else ''
    m = re.match(r'\s*(?:(?:public|private|protected|internal|static|sealed|override|virtual|async|abstract|extern|new|unsafe|partial)\s+)*[\w<>\[\],\.\? ]+?\s(\w+)\s*[\({]', s)
    return m.group(1) if m else s[:30]


bad = []; same = 0; ann = 0
import os
for n in sorted(x[:-4] for x in os.listdir(D) if x.endswith('.tsv')):
    raw = io.open(D + n + '.tsv', 'rb').read()
    for l in raw.decode('utf-8').split('\n'):
        c = l.split('\t')
        if len(c) != 11 or c[5] != 'A':
            continue
        lm = REF.search(c[6])
        if not lm:
            bad.append((n, c[1], c[2][:30], 'NO_LINE')); continue
        p = find(lm.group(1)); dn = dn_at(p, int(lm.group(2))) if p else '?'
        orig = re.sub(r'\(.*$', '', c[2])
        if dn == orig:
            same += 1
        elif '（等价实现' in c[6] or '（实现 =' in c[6]:
            ann += 1
        else:
            bad.append((n, c[1], c[2][:34], dn))
print('同名 %d · 异名已注解 %d · 缺注解 %d' % (same, ann, len(bad)))
for x in bad[:15]:
    print('  ', x)
