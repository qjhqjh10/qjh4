# -*- coding: utf-8 -*-
"""补救：上一趟脚本删了 16 行、却没走到「追加进收口档」那一步 ⇒ 从 `git show HEAD` 取回原行补进收口档。
⚠️ 这 16 行本会话【没有改过】⇒ HEAD 版就是原样（脚本里加断言核首格标记）。
用法：python -I -X utf8 d:/4/Unity/工具/_fix_1014_lost16.py
"""
import io
import re
import subprocess

TASKS_REL = 'Unity/资料/任务_卡组卡牌战斗.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'
IDS = ['A1045', 'A1145', 'A1177', 'A1697', 'A1700', 'A1702', 'A1622', 'A1612', 'A1703',
       'A1605', 'A1506', 'A1508', 'A1654', 'A1707', 'A1698', 'A1699']


def main():
    out = subprocess.run(['git', '-C', r'D:\4', 'show', 'HEAD:%s' % TASKS_REL],
                         capture_output=True, check=True)
    hl = out.stdout.decode('utf-8').split('\n')
    rows = []
    for cid in IDS:
        pat = re.compile(r'^\| ✅ \*\*`%s`\*\*' % cid)
        hits = [i for i, l in enumerate(hl) if pat.match(l)]
        assert len(hits) == 1, (cid, len(hits))
        i = hits[0]
        j = i
        while not hl[j].rstrip().endswith(' |'):
            j += 1
        rows.append('\n'.join(hl[i:j + 1]))
        print('  取回 %s（%d 字符）' % (cid, len(rows[-1])))

    at = io.open(ARC, encoding='utf-8').read()
    for cid in IDS:
        assert '**`%s`**' % cid not in at, '%s 已在收口档里，别重复追加' % cid
    lines2 = at.split('\n')
    sep = [i for i, l in enumerate(lines2) if l.strip() == '|---|']
    assert len(sep) == 1
    k = sep[0] + 1
    lines2[k:k] = ['\n'.join(rows)]
    at2 = '\n'.join(lines2)
    at2 = at2.replace('> ⬇️ 下面 **17 行全部来自**', '> ⬇️ 下面 **33 行全部来自**')
    at2 = at2.replace('| 编号（原行原文） |',
                      '> ✅ **2026-10-14（第三十会话）再收 16 条**（收尾核销：首格 ✅ 且行内自陈已完成、无尾巴）\n\n'
                      '| 编号（原行原文） |', 1)
    io.open(ARC, 'wb').write(at2.encode('utf-8'))
    print('  ✅ 收口档已补 16 行')


if __name__ == '__main__':
    main()
