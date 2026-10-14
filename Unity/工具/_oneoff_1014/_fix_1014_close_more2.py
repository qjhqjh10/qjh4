# -*- coding: utf-8 -*-
"""修复 _apply_1014_close_more2.py 的误删：
 ① 从 `git show HEAD:项目任务.md` 取回 §29·c 里被吃掉的 `A577`·`A586`·`A367` 那一行，插回原位
 ② 把 `✅✅ ~~A650~~` 那一行从正文删掉（它已经被搬进收口档了）
 ③ 核对收口档里 A650 只出现一次
用法：python -I -X utf8 d:/4/Unity/工具/_fix_1014_close_more2.py
"""
import io
import subprocess

MAIN = r'D:\4\项目任务.md'
ARC = r'D:\4\Unity\资料\历史\A表已收口_第三十会话.md'


def main():
    # ① 从 git 取原行
    out = subprocess.run(['git', '-C', r'D:\4', 'show', 'HEAD:项目任务.md'],
                         capture_output=True, check=True)
    head = out.stdout.decode('utf-8')
    hl = head.split('\n')
    hits = [i for i, l in enumerate(hl) if l.startswith('| `A577` · `A586`')]
    assert len(hits) == 1, ('HEAD 里 A577 行', len(hits))
    i0 = hits[0]
    j0 = i0
    while not hl[j0].rstrip().endswith(' |'):
        j0 += 1
    row = '\n'.join(hl[i0:j0 + 1])
    print('  取回原行 %d 行（%d 字符）' % (j0 - i0 + 1, len(row)))

    b = io.open(MAIN, 'rb').read()
    assert b.count(b'\r\n') == 0
    lines = b.decode('utf-8').split('\n')

    # 幂等：已经修过就不再来
    if [l for l in lines if l.startswith('| `A577` · `A586`')]:
        print('  = A577 行已在，跳过恢复')
    else:
        anchor = [i for i, l in enumerate(lines) if l.startswith('| `A985⑤`')]
        assert len(anchor) == 1, ('A985⑤ 锚点', len(anchor))
        lines[anchor[0]:anchor[0]] = row.split('\n')
        print('  ✅ A577/A586/A367 行已插回（L%d 之前）' % (anchor[0] + 1))

    # ② 删 A650（它已进收口档）
    h = [i for i, l in enumerate(lines) if l.startswith('| ✅✅ ~~`A650`~~')]
    if h:
        assert len(h) == 1, h
        j = h[0]
        while not lines[j].rstrip().endswith(' |'):
            j += 1
        print('  ✅ 从正文删掉 A650（L%d-%d，共 %d 行）' % (h[0] + 1, j + 1, j - h[0] + 1))
        del lines[h[0]:j + 1]
    else:
        print('  = A650 已不在正文')

    out_t = '\n'.join(lines)
    assert '\x00' not in out_t and '\r\n' not in out_t
    io.open(MAIN, 'wb').write(out_t.encode('utf-8'))
    print('MAIN 总行数 %d' % len(lines))

    # ③ 收口档核对
    at = io.open(ARC, encoding='utf-8').read()
    n = at.count('`A650`')
    print('  收口档里 `A650` 出现 %d 次（应为 1）' % n)
    assert n == 1, '收口档里 A650 重复或缺失'


if __name__ == '__main__':
    main()
