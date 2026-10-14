# -*- coding: utf-8 -*-
"""收尾检查 · ① 两张待办正本里【已做完却还列着】的行
判据（沿用第二十九会话分诊那把尺子）：行内明写「已办 / 已收口 / 已结案 / 可核销 / 已核销 / 已做 / 尾巴已补」
**且没有**「⏭ 仍开着 / 还欠 / 未做 / 仍开 / 未查」这类尾巴词 ⇒ 判「疑似该核销」。
用法：python -I -X utf8 d:/4/Unity/工具/_audit_1014_open.py
"""
import io
import re

FILES = [r'D:\4\Unity\资料\任务_卡组卡牌战斗.md', r'D:\4\项目任务.md']
DONE = ('已办', '已收口', '已结案', '可核销', '已核销', '已做', '尾巴已补', '已落地', '✅')
TAIL = ('⏭ 仍开着', '仍开着', '还欠', '未做', '仍开', '未查', '剩 ', '仍留', '还差', '待人工', '未核')


def rows(path):
    lines = io.open(path, encoding='utf-8').read().split('\n')
    i = 0
    out = []
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
        print('=' * 70)
        print(p.split('\\')[-1])
        sus = []
        for ln, txt in rows(p):
            first = txt.split('|')[1].strip()
            has_done = any(k in txt for k in DONE)
            has_tail = any(k in txt for k in TAIL)
            struck = '~~' in first
            if has_done and not has_tail:
                sus.append((ln, first[:34], struck))
            elif struck:
                sus.append((ln, first[:34], True))
        for ln, first, struck in sus:
            print('  L%-5d %-36s %s' % (ln, first, '【已划掉】' if struck else ''))
        print('  ⇒ 疑似已完成但仍在表里：%d 行' % len(sus))


if __name__ == '__main__':
    main()
