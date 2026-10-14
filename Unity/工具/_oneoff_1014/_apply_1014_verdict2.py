# -*- coding: utf-8 -*-
"""_apply_1014_verdict.py 的补跑：A1692 在 TASKS（⛔ 不在 MAIN）+ §29·d 那行。
⚠️ 教训：**同一个「文件归属」错误本会话犯了 5 次** ⇒ 以后一律先按 id 在【两个文件】里各找一次。
用法：python -I -X utf8 d:/4/Unity/工具/_apply_1014_verdict2.py
"""
import io
import importlib.util

spec = importlib.util.spec_from_file_location('_v', r'D:\4\Unity\工具\_apply_1014_verdict.py')
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

MAIN, TASKS = m.MAIN, r'D:\4\Unity\资料\任务_卡组卡牌战斗.md'


def block_end(lines, i):
    j = i
    while j < len(lines) and not lines[j].rstrip().endswith(' |'):
        j += 1
    assert j < len(lines)
    return j


def append(path, head, seg):
    b = io.open(path, 'rb').read()
    assert b.count(b'\r\n') == 0, path
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(head)]
    assert len(hits) == 1, (path, head, len(hits))
    j = block_end(lines, hits[0])
    lines[j] = lines[j].rstrip()[:-1].rstrip() + seg + ' |'
    out = '\n'.join(lines)
    assert '\x00' not in out and '\r\n' not in out
    io.open(path, 'wb').write(out.encode('utf-8'))
    print('  +%s (%s)' % (head, path.split('\\')[-1]))


def main():
    append(TASKS, '| 🆕 **`A1692`**', m.A1692_VERDICT)
    b = io.open(MAIN, 'rb').read()
    lines = b.decode('utf-8').split('\n')
    hits = [i for i, l in enumerate(lines) if l.startswith(m.ROW_HEAD)]
    assert len(hits) == 1, ('Card_Description 行', len(hits))
    if '不再是「等远端」' not in lines[hits[0]]:
        j = block_end(lines, hits[0])
        lines[j] = lines[j].rstrip()[:-1].rstrip() + m.ROW_ADD + ' |'
        out = '\n'.join(lines)
        assert '\x00' not in out and '\r\n' not in out
        io.open(MAIN, 'wb').write(out.encode('utf-8'))
        print('  +§29·d 的 Card_Description/* 行')
    else:
        print('  = §29·d 行已改过')


if __name__ == '__main__':
    main()
