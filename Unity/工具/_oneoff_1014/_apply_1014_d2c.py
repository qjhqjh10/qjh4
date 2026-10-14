# -*- coding: utf-8 -*-
"""_apply_1014_d2b.py 的修正版：多行【表格行】要追加到该块的【最后一行】。
（A1987 正文里有 \\n ⇒ 它占多行；A1975/A1986 上一趟已落，本脚本用「已含该段」判据跳过。）
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_d2c.py
"""
import io
import importlib.util

spec = importlib.util.spec_from_file_location('_d2b', r'D:\4\Unity\工具\_apply_1014_d2b.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)            # 只取 SEG / A1990-92，不跑 main()
SEG = m.SEG
MAIN = r'D:\4\项目任务.md'


def block_end(lines, i):
    """从表格行块的首行 i 往后，找到该块【最后一行】（第一个以 ' |' 收尾的行）"""
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines), '没有找到行尾'
    return j


def append_seg(head_prefix, seg):
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    if seg in '\n'.join(lines):
        print('  = %s 已在，跳过' % head_prefix)
        return
    hits = [i for i, l in enumerate(lines) if l.startswith(head_prefix)]
    assert len(hits) == 1, (head_prefix, len(hits))
    j = block_end(lines, hits[0])
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +%s (尾行 L%d)' % (head_prefix, j + 1))


def main():
    for cid in ('A1975', 'A1986', 'A1987', 'A1988', 'A1989'):
        append_seg('| 🆕 **`%s`**' % cid, SEG[cid])
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    if '**`A1990`**' in b.decode('utf-8'):
        print('  = A1990-92 已在，跳过')
        return
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1989`**')]
    assert len(hits) == 1, hits
    j = block_end(lines, hits[0])
    lines[j + 1:j + 1] = ['\n'.join([m.A1990, m.A1991, m.A1992])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1990/A1991/A1992 (插在 L%d 之后)' % (j + 1))


if __name__ == '__main__':
    main()
