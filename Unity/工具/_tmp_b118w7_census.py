#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B118-W7 一次性普查：CardPresentation/Editor 里 `new CardDef(` 处，
同函数是否有 AttachCardType，以及该夹具是否会走到 cardType 的 6 个消费点。只读。"""
import io, os, re, sys

ASSETS = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', 'MyGame', 'Assets'))
ROOT = os.path.join(ASSETS, 'CardPresentation', 'Editor')

CTRL = {'if','for','foreach','while','switch','catch','using','lock','fixed','do','else','try','finally'}

def mask(text):
    """把字符串/字符/注释替换成空格（保长度），避免花括号干扰块解析。"""
    out = list(text)
    i, n = 0, len(text)
    while i < n:
        c = text[i]
        if c == '/' and i + 1 < n and text[i+1] == '/':
            while i < n and text[i] != '\n':
                out[i] = ' '; i += 1
        elif c == '/' and i + 1 < n and text[i+1] == '*':
            out[i] = ' '; out[i+1] = ' '; i += 2
            while i + 1 < n and not (text[i] == '*' and text[i+1] == '/'):
                out[i] = ' '; i += 1
            if i + 1 < n:
                out[i] = ' '; out[i+1] = ' '; i += 2
        elif c == '@' and i + 1 < n and text[i+1] == '"':
            out[i] = ' '; out[i+1] = ' '; i += 2
            while i < n:
                if text[i] == '"' and (i + 1 >= n or text[i+1] != '"'):
                    out[i] = ' '; i += 1; break
                if text[i] == '"' and i + 1 < n and text[i+1] == '"':
                    out[i] = ' '; out[i+1] = ' '; i += 2; continue
                out[i] = ' '; i += 1
        elif c == '"' or (c == '$' and i + 1 < n and text[i+1] == '"'):
            if c == '$':
                out[i] = ' '; i += 1
            out[i] = ' '; i += 1
            while i < n:
                if text[i] == '\\':
                    out[i] = ' '; i += 1
                    if i < n: out[i] = ' '; i += 1
                    continue
                if text[i] == '"':
                    out[i] = ' '; i += 1; break
                out[i] = ' '; i += 1
        elif c == "'":
            out[i] = ' '; i += 1
            while i < n:
                if text[i] == '\\':
                    out[i] = ' '; i += 1
                    if i < n: out[i] = ' '; i += 1
                    continue
                if text[i] == "'":
                    out[i] = ' '; i += 1; break
                out[i] = ' '; i += 1
        else:
            i += 1
    return ''.join(out)

def find_method_blocks(masked):
    blocks = []
    stack = []
    last_break = 0
    for i, c in enumerate(masked):
        if c == '{':
            hdr = masked[last_break:i].strip()
            stack.append((i, hdr))
            last_break = i + 1
        elif c == '}':
            if stack:
                op, hdr = stack.pop()
                blocks.append((op, i, hdr))
                last_break = i + 1
        elif c == ';':
            if stack:
                last_break = i + 1
    return blocks

def is_method_header(h):
    h = h.strip()
    h2 = re.sub(r'^\s*\[[^\]]*\]\s*', '', h)
    if '=>' in h2 or '(' not in h2 or ')' not in h2:
        return False
    first = re.match(r'([A-Za-z_][A-Za-z0-9_]*)', h2)
    if first and first.group(1) in CTRL:
        return False
    if re.search(r'\b(class|struct|interface|enum|namespace|record)\b', h2):
        return False
    return True

CONSUMERS = [
    ('C1_CardPlayedMask', re.compile(r'\bCardPlayedMask\b')),
    ('C2_GetCardsInCemetery', re.compile(r'\bGetCardsInCemetery\b')),
    ('C3_UnitState_ctor', re.compile(r'\bnew\s+UnitState\s*\(')),
    ('C4_FTUELowLife', re.compile(r'\bSetupFTUELowLifeInvulnerability\b')),
    ('C5_DiscardHand', re.compile(r'DiscardHand[A-Za-z]*\s*\(')),
    ('C6_QuestEnchant', re.compile(r'\bFilterByQuestCounter\b')),
]

VARDECL = re.compile(r'([A-Za-z_][A-Za-z0-9_]*)\s*=\s*$')

def scan():
    rows = []
    for fn in sorted(os.listdir(ROOT)):
        if not fn.endswith('.cs'):
            continue
        p = os.path.join(ROOT, fn)
        text = io.open(p, 'rb').read().decode('utf-8', 'replace').replace('\r\n', '\n')
        mk = mask(text)
        blocks = find_method_blocks(mk)
        methods = [b for b in blocks if is_method_header(b[2])]
        for m in re.finditer(r'new\s+CardDef\s*\(', mk):
            pos = m.start()
            line = text.count('\n', 0, pos) + 1
            ls = text.rfind('\n', 0, pos) + 1
            vm = VARDECL.search(text[ls:pos])
            var = vm.group(1) if vm else '?'
            enc = sorted([b for b in methods if b[0] < pos < b[1]], key=lambda b: b[1]-b[0])
            if enc:
                op, cl, hdr = enc[0]
                mtext = text[op:cl]; mline = text.count('\n', 0, op) + 1
            else:
                mtext = text; mline = 0
            hits = [name for name, rx in CONSUMERS if rx.search(mtext)]
            rows.append(dict(file=fn, line=line, var=var, mline=mline,
                             has_attach='AttachCardType' in mtext, hits=hits))
    return rows

def main():
    rows = scan()
    print(f"总 new CardDef 处 = {len(rows)}，文件 = {len(set(r['file'] for r in rows))}")
    noatt = [r for r in rows if not r['has_attach']]
    print(f"同函数无 AttachCardType = {len(noatt)}，文件 = {len(set(r['file'] for r in noatt))}")
    risky = [r for r in noatt if r['hits']]
    print(f"无 AttachCardType 且该函数触及消费点 = {len(risky)}")
    print()
    for r in risky:
        print(f"{r['file']}:{r['line']} | {r['var']} | fn@{r['mline']} | {','.join(r['hits'])}")
    print()
    print("=== 按消费点汇总（会触及的行数）===")
    from collections import Counter
    c = Counter()
    for r in noatt:
        for h in r['hits']:
            c[h] += 1
    for k, v in sorted(c.items()):
        print(f"  {k}: {v}")

if __name__ == '__main__':
    main()
