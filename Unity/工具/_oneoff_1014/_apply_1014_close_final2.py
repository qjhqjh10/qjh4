# -*- coding: utf-8 -*-
"""收尾核销（第三趟）：补完上一趟没走到的几行 + 订正 3 个错标记。
顺序仍是【先删 → 再改 → 最后插】。⛔ 排除 A1662（正文里有两行同名，留给下个会话判）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_close_final2.py
"""
import io
import re

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'

MOVE = [(TASKS, 'A1696'), (TASKS, 'A1820'), (MAIN, 'A950')]

FIX = {
    'A1088': '行内自陈「**我们没做**」＋「⇒ **要做**（铁律 11）：补这道闸」',
    'A1431': '行内自陈「🟡 …补了一层、**【仍开着】**」＋「⇒ **要做**（铁律 11）」',
    'A1694': '行内自陈「**还差最后一步**」',
}


def load(p):
    b = io.open(p, 'rb').read()
    assert b.count(b'\r\n') == 0, p
    return b.decode('utf-8').split('\n')


def save(p, lines):
    t = '\n'.join(lines)
    assert '\x00' not in t and '\r\n' not in t
    io.open(p, 'wb').write(t.encode('utf-8'))


def find_row(lines, cid):
    pat = re.compile(r'^\| [^\n]{0,30}\*\*`%s`\*\*' % cid)
    hits = [i for i, l in enumerate(lines) if pat.match(l)]
    assert len(hits) == 1, (cid, len(hits))
    i = hits[0]
    j = i
    while not lines[j].rstrip().endswith(' |'):
        j += 1
    return i, j


def main():
    picked = []
    for path, cid in MOVE:
        lines = load(path)
        i, j = find_row(lines, cid)
        picked.append('\n'.join(lines[i:j + 1]))
        del lines[i:j + 1]
        save(path, lines)
        print('  - %s（%s）' % (cid, path.split('\\')[-1]))

    lines = load(TASKS)
    for cid, why in FIX.items():
        i, j = find_row(lines, cid)
        assert lines[i].startswith('| ✅ **`%s`**' % cid), (cid, lines[i][:40])
        lines[i] = lines[i].replace(
            '| ✅ **`%s`** |' % cid,
            '| 🆕 **`%s`** | ⚠️ **2026-10-14 就地订正（铁律 5）**：行首原为 ✅，但%s ⇒ **改回 🆕**'
            '（本会话做过一次全表审计：**✅ 标记不可信** —— 同一趟还查出 `A1590`/`A1662` 一类「🆕 但其实已闭」的反向情况）。' % (cid, why), 1)
        print('  ✎ %s ✅→🆕' % cid)
    save(TASKS, lines)

    at = io.open(ARC, encoding='utf-8').read()
    for cid in ('A1696', 'A1820', 'A950'):
        assert '**`%s`**' % cid not in at, '%s 已在收口档' % cid
    lines2 = at.split('\n')
    sep = [i for i, l in enumerate(lines2) if l.strip() == '|---|']
    assert len(sep) == 1
    k = sep[0] + 1
    lines2[k:k] = ['\n'.join(picked)]
    at2 = '\n'.join(lines2)
    at2 = at2.replace('> ⬇️ 下面 **33 行全部来自**', '> ⬇️ 下面 **36 行全部来自**')
    io.open(ARC, 'wb').write(at2.encode('utf-8'))
    print('  + 收口档追加 %d 行' % len(picked))


if __name__ == '__main__':
    main()
