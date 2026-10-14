# -*- coding: utf-8 -*-
"""_apply_1014_data6.py 的补跑：A1992 在 MAIN（⛔ 不在 TASKS）+ A2002/A2003 插入。
（A1987/A1993 那两条上一趟已落。）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_data6b.py
"""
import io
import importlib.util

spec = importlib.util.spec_from_file_location('_d6', r'D:\4\Unity\工具\_apply_1014_data6.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)          # 取 A1992_SEG / A2002 / A2003，不跑 main()
MAIN = r'D:\4\项目任务.md'


def block_end(lines, i):
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    return j


def append(head, seg):
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    if seg in '\n'.join(lines):
        print('  = %s 已在，跳过' % head)
        return
    hits = [i for i, l in enumerate(lines) if l.startswith(head)]
    assert len(hits) == 1, (head, len(hits))
    j = block_end(lines, hits[0])
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head, j + 1))


def main():
    append('| 🆕 **`A1992`**', m.A1992_SEG)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    if '**`A2002`**' in b.decode('utf-8'):
        print('  = A2002/A2003 已在，跳过')
        return
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1998`**')]
    assert len(hits) == 1
    j = block_end(lines, hits[0])
    lines[j + 1:j + 1] = ['\n'.join([m.A2002, m.A2003])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A2002/A2003')


if __name__ == '__main__':
    main()
