# -*- coding: utf-8 -*-
"""收尾核销（第二趟）：
 ① 先【删】：把 19 条确实完成的整行搬出 `任务_卡组卡牌战斗.md`、1 条搬出 `项目任务.md`
 ② 再【订正】：A1088 / A1431 / A1694 的行首 ✅ 是错的（行内自陈仍开着）⇒ 改回 🆕 并留痕
 ③ 最后【插】：把搬走的行追加进 `资料/历史/A表已收口_第三十会话.md`
⚠️ 顺序铁律：**先删、再改、最后插** —— 插入会移位，绝不能和按索引的删除混做（本会话已栽过一次）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_close_final.py
"""
import io
import re

TASKS = r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'
MAIN = r'D:\4\项目任务.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'

MOVE_TASKS = ['A1045', 'A1145', 'A1177', 'A1697', 'A1700', 'A1702', 'A1622', 'A1612', 'A1703',
              'A1605', 'A1506', 'A1508', 'A1654', 'A1707', 'A1698', 'A1699', 'A1662', 'A1696', 'A1820']
MOVE_MAIN = ['A950']

FIX = {  # 行首 ✅ 是错的 ⇒ 改回 🆕，并在首格后留痕
    'A1088': '行内自陈「**我们没做**」＋「⇒ **要做**（铁律 11）」',
    'A1431': '行内自陈「🟡 …补了一层、**【仍开着】**」',
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
    # ① 删（每个都重新定位 ⇒ 不受前一次删除影响）
    for path, ids in ((TASKS, MOVE_TASKS), (MAIN, MOVE_MAIN)):
        for cid in ids:
            lines = load(path)
            i, j = find_row(lines, cid)
            picked.append((path, '\n'.join(lines[i:j + 1])))
            del lines[i:j + 1]
            save(path, lines)
            print('  - %s（%s）' % (cid, path.split('\\')[-1]))

    # ② 订正错标记
    lines = load(TASKS)
    for cid, why in FIX.items():
        i, j = find_row(lines, cid)
        assert lines[i].startswith('| ✅ **`%s`**' % cid), (cid, lines[i][:40])
        lines[i] = lines[i].replace(
            '| ✅ **`%s`** |' % cid,
            '| 🆕 **`%s`** | ⚠️ **2026-10-14 就地订正（铁律 5）**：行首原为 ✅，但%s ⇒ **改回 🆕**（✅ 标记不可信，本会话已就这条做过一次全表审计）。' % (cid, why),
            1)
        print('  ✎ %s 标记已订正 ✅→🆕' % cid)
    save(TASKS, lines)

    # ③ 插进收口档
    at = io.open(ARC, encoding='utf-8').read()
    lines2 = at.split('\n')
    sep = [i for i, l in enumerate(lines2) if l.strip() == '|---|']
    assert len(sep) == 1
    k = sep[0] + 1
    lines2[k:k] = ['\n'.join(x[1] for x in picked)]
    at2 = '\n'.join(lines2)
    at2 = at2.replace('> ⬇️ 下面 **17 行全部来自**', '> ⬇️ 下面 **37 行全部来自**')
    at2 = at2.replace('| 编号（原行原文） |',
                      '> ✅ **2026-10-14（第三十会话）再收 20 条**（收尾核销：标记为 ✅ 且行内自陈已完成、无尾巴的）\n\n| 编号（原行原文） |', 1)
    io.open(ARC, 'wb').write(at2.encode('utf-8'))
    print('  + 收口档追加 %d 行' % len(picked))


if __name__ == '__main__':
    main()
