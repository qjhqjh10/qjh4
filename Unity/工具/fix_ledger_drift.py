# -*- coding: utf-8 -*-
"""G2 漂移补：对 verify_annot 判「缺注解」（= 落点那一行不再是同名声明）的行，
若首个 <path>:<行>（Name）里的 Name 在该文件中**唯一命中一份声明** ⇒ 把行号挪到那份声明。
只改「我们的落点」列，只改行号，⛔ 不动名字、不动其它列。命中不唯一的一律跳过并报告。"""
import io, os, re, sys

D = r'D:/4/Unity/资料/完全复刻/账/'
ROOTS = [r'D:/4/Unity/', r'D:/4/Unity/MyGame/Assets/', r'D:/4/Unity/MyGame/Assets/CardPresentation/']
APPLY = '--apply' in sys.argv
REF = re.compile(r'([\w\-./]+\.cs):(\d+)')
NAMEP = re.compile(r'^\s*（\s*([A-Za-z_]\w*(?:<[^）]*>)?)[^）]*）')
DECL = re.compile(r'\s*(?:(?:public|private|protected|internal|static|sealed|override|virtual|async|abstract|extern|new|unsafe|partial)\s+)*[\w<>\[\],\.\? ]+?\s(\w+)\s*[\({]')
_C = {}


def find(rel):
    for r in ROOTS:
        p = r + rel
        if os.path.exists(p):
            return p
    return None


def lines(p):
    if p not in _C:
        _C[p] = io.open(p, 'rb').read().decode('utf-8', 'replace').split('\n')
    return _C[p]


def decl_at(p, ln):
    ls = lines(p)
    s = ls[ln - 1].strip() if 0 < ln <= len(ls) else ''
    m = DECL.match(s)
    return m.group(1) if m else None


def decl_line(p, name):
    out = []
    for i, l in enumerate(lines(p), 1):
        s = l.strip()
        if s.startswith('//') or s.startswith('*'):
            continue
        m = re.search(r'\b' + re.escape(name) + r'\s*[\({]', s)
        if not m:
            continue
        if '=' in s[:m.start()] or 'return' in s[:m.start()] or s.startswith('case '):
            continue
        dm = DECL.match(s)
        if dm and dm.group(1) == name:
            out.append(i)
    return out


fixed, skipped = [], []
for n in sorted(x for x in os.listdir(D) if x.endswith('.tsv')):
    b = io.open(D + n, 'rb').read()
    assert b'\r' not in b
    rows = b.decode('utf-8').split('\n')
    dirty = False
    for i, l in enumerate(rows):
        c = l.split('\t')
        if len(c) != 11 or c[5] != 'A':
            continue
        cell = c[6]
        m = REF.search(cell)
        if not m:
            continue
        p = find(m.group(1))
        if not p:
            continue
        orig = re.sub(r'\(.*$', '', c[2])
        if '.' in orig:
            orig = orig.rsplit('.', 1)[-1]
        got = decl_at(p, int(m.group(2)))
        if got == orig:
            continue
        if '（等价实现' in cell or '（实现 =' in cell:
            continue
        nm = NAMEP.match(cell[m.end():])
        if not nm:
            skipped.append((n, i + 1, c[1], c[2][:40], '括号里没名字', got)); continue
        name = nm.group(1)
        hits = decl_line(p, name)
        if len(hits) != 1:
            skipped.append((n, i + 1, c[1], c[2][:40], '声明命中 %d 条' % len(hits), name)); continue
        old = cell
        c[6] = cell[:m.start(2)] + str(hits[0]) + cell[m.end(2):]
        if c[6] == old:
            skipped.append((n, i + 1, c[1], c[2][:40], '挪到 %d 仍是同一行（DECL 认不出该行）' % hits[0], name))
            continue
        rows[i] = '\t'.join(c)
        dirty = True
        fixed.append((n, i + 1, c[1], c[2][:44], m.group(1) + ':' + m.group(2) + ' -> ' + str(hits[0]), name))
    if dirty and APPLY:
        nb = '\n'.join(rows).encode('utf-8')
        assert b'\r' not in nb
        io.open(D + n, 'wb').write(nb)

print('APPLY =', APPLY)
print('--- FIXED', len(fixed))
for x in fixed:
    print('   ', x)
print('--- SKIPPED', len(skipped))
for x in skipped:
    print('   ', x)
