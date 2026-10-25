# -*- coding: utf-8 -*-
"""按域列出「语义闸（verify_annot.py）」判出来的【缺注解】A 行 —— 只读，不改任何账。

用法：
    python -I -X utf8 工具/list_missing_annot.py                 # 全部域，只给计数
    python -I -X utf8 工具/list_missing_annot.py 战斗界面_HUD      # 指定域，逐行列出（TSV）
    python -I -X utf8 工具/list_missing_annot.py 战斗界面_HUD --limit 20
    python -I -X utf8 工具/list_missing_annot.py --all-domains    # 每个域都逐行列出

输出列（TAB 分隔，无表头）：
    类 \t 方法 \t 落点原文 \t 实际声明名 \t 判据要点 \t 判据出处

口径与 verify_annot.py 完全一致：
  A 行 + 落点能解析出 `文件.cs:行` + 该行声明的名字 != 原版方法名 + 落点里没有「（等价实现」/「（实现 =」注解
  ⇒ 计入「缺注解」。
（另有一类 NO_LINE：落点压根没有 `.cs:行` —— 那类本工具也列出来，第 4 列写 `NO_LINE`。）
"""
import io
import os
import re
import sys

D = r'D:/4/Unity/资料/完全复刻/账/'
ROOTS = [r'D:/4/Unity/', r'D:/4/Unity/MyGame/Assets/', r'D:/4/Unity/MyGame/Assets/CardPresentation/']
REF = re.compile(r'([\w\-./\\]+\.cs):(\d+)')
DECL = re.compile(
    r'\s*(?:(?:public|private|protected|internal|static|sealed|override|virtual|async|abstract|extern|new|unsafe|partial)\s+)*'
    r'[\w<>\[\],\.\? ]+?\s(\w+)\s*[\({]')


def find(rel):
    for r in ROOTS:
        p = r + rel.replace('\\', '/')
        try:
            io.open(p, encoding='utf-8').read(1)
            return p
        except Exception:
            pass
    return None


def dn_at(p, ln):
    ls = io.open(p, encoding='utf-8', errors='replace').read().split('\n')
    s = ls[ln - 1].strip() if 0 < ln <= len(ls) else ''
    m = DECL.match(s)
    return m.group(1) if m else s[:40]


def scan(domain):
    path = D + domain + '.tsv'
    rows = []
    raw = io.open(path, 'rb').read().decode('utf-8-sig')
    for i, line in enumerate(raw.split('\n')):
        c = line.split('\t')
        if len(c) != 11 or c[5] != 'A':
            continue
        lm = REF.search(c[6])
        if not lm:
            rows.append((c[1], c[2], c[6], 'NO_LINE', c[4], c[3]))
            continue
        p = find(lm.group(1))
        dn = dn_at(p, int(lm.group(2))) if p else '?'
        orig = re.sub(r'\(.*$', '', c[2])
        if dn == orig:
            continue
        if '（等价实现' in c[6] or '（实现 =' in c[6]:
            continue
        rows.append((c[1], c[2], c[6], dn, c[4], c[3]))
    return rows


def main():
    args = [a for a in sys.argv[1:]]
    limit = None
    if '--limit' in args:
        i = args.index('--limit')
        limit = int(args[i + 1])
        del args[i:i + 2]
    all_domains = '--all-domains' in args
    args = [a for a in args if not a.startswith('--')]

    if not args:
        total = 0
        out = []
        for n in sorted(x[:-4] for x in os.listdir(D) if x.endswith('.tsv')):
            r = scan(n)
            if r:
                out.append((n, len(r)))
                total += len(r)
        for n, k in sorted(out, key=lambda t: -t[1]):
            print('%-24s %6d' % (n, k))
        print('%-24s %6d' % ('合计', total))
        return

    domains = args if args else sorted(x[:-4] for x in os.listdir(D) if x.endswith('.tsv'))
    for n in domains:
        if not os.path.exists(D + n + '.tsv'):
            print('!! 没有这份账: ' + n, file=sys.stderr)
            continue
        rows = scan(n)
        print('# %s : %d 条缺注解' % (n, len(rows)), file=sys.stderr)
        if not all_domains and len(args) == 0:
            continue
        for r in (rows[:limit] if limit else rows):
            print('\t'.join(r))


if __name__ == '__main__':
    main()
