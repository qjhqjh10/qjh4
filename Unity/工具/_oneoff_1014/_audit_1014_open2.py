# -*- coding: utf-8 -*-
"""收尾检查（修正版）· 两张待办正本里【已做完却还列着】的行
判据收紧：① **首格标记**是 ✅/📌/⚪ 或被 ~~划掉~~ ⇒ 疑似该核销
        ② 或者：正文里有「已收口/已结案/可核销/已核销/已办」**且没有**「仍开着/还欠/未做/仍开/未查/还差」这类尾巴词
⛔ 不看正文里出现的裸 ✅（那是断言文案里的，不是状态）。
用法：python -I -X utf8 d:/4/Unity/工具/_audit_1014_open2.py
"""
import io
import re

FILES = [r'D:\4\Unity\资料\任务_卡组卡牌战斗.md', r'D:\4\项目任务.md']
DONE = ('已收口', '已结案', '可核销', '已核销', '尾巴已补')
TAIL = ('仍开着', '还欠', '未做', '仍开', '未查', '还差', '仍留', '待人工', '未核', '还没')


def rows(path):
    lines = io.open(path, encoding='utf-8').read().split('\n')
    i, out = 0, []
    while i < len(lines):
        l = lines[i]
        if l.startswith('| ') and re.search(r'`A\d+`', l[:60]) and l.count('|') >= 3:
            j = i
            while j < len(lines) and not lines[j].rstrip().endswith(' |'):
                j += 1
            if j < len(lines):
                out.append((i + 1, '\n'.join(lines[i:j + 1])))
            i = j + 1
        else:
            i += 1
    return out


def main():
    for p in FILES:
        print('=' * 72)
        print(p.split('\\')[-1])
        a = b = 0
        for ln, txt in rows(p):
            first = txt.split('|')[1].strip()
            body = txt
            mark = first.split()[0] if first.split() else ''
            struck = first.startswith('~~') or '~~ **' in first or first.endswith('~~')
            by_mark = mark in ('✅', '📌', '⚪') or struck
            by_text = (any(k in body for k in DONE) and not any(k in body for k in TAIL))
            if by_mark or by_text:
                tag = '首格标记' if by_mark else '正文自陈'
                print('  L%-5d [%s] %-38s' % (ln, tag, first[:38]))
                if by_mark:
                    a += 1
                else:
                    b += 1
        print('  ⇒ 首格标记类 %d 行 · 正文自陈类 %d 行 · 合计 %d' % (a, b, a + b))


if __name__ == '__main__':
    main()
