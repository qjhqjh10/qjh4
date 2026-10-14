# -*- coding: utf-8 -*-
"""_apply_1014_yi6.py 的后半（前半 5 笔已落盘；A1800 的标记撞上了 A1782 行里提到它的那处，故加严匹配）。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_yi6b.py
"""
import io
import importlib.util

spec = importlib.util.spec_from_file_location('_yi6', r'D:\4\Unity\工具\_apply_1014_yi6.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)          # 只取 SEG / A1983-85，不跑 main()
SEG, DOC = m.SEG, m.DOC
TASKS, MAIN = m.TASKS, m.MAIN


def append_row(path, cid, seg):
    """只在【行首那格】就是该编号时才认（⛔ 别用「行内出现」——别的行会提到它）"""
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    head = '| 🆕 **`%s`**' % cid
    hits = [i for i, l in enumerate(lines) if l.startswith(head)]
    assert len(hits) == 1, (cid, len(hits))
    i = hits[0]
    assert lines[i].rstrip().endswith(' |'), lines[i][:70]
    lines[i] = lines[i].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s' % cid)


def main():
    append_row(TASKS, 'A1800', SEG['A1800'])
    append_row(TASKS, 'A1962',
               " ⚠️ **2026-10-14 再添一例（只读查证顺手撞见）**：`d:/2/tools/decomp_full/ActiveEnchantment__.ctor.c` 的**内容是匿名类型 ctor**、"
               "**不是 `ActiveEnchantment` 的构造函数** ⇒ 那一族**要以 `d:/2/tools/il2cpp_out/dump.cs` 为准**（本轮第 3 次撞到「文件名与内容对不上」）。")
    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith('| 🆕 **`A1982`**')]
    assert len(hits) == 1
    lines[hits[0] + 1:hits[0] + 1] = ['\n'.join([m.A1983, m.A1984, m.A1985])]
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(MAIN, 'wb').write(out.encode('utf-8'))
    print('  +A1983/A1984/A1985')


if __name__ == '__main__':
    main()
