# -*- coding: utf-8 -*-
"""修 A1993 那一行被 bash 反引号命令替换吃掉的两个标识符。
错因：`bash python -c "…`desc`…"` 里反引号被当成命令替换（日志有 `desc: command not found`）。
⛔ 教训：**改正本一律用文件式脚本**，别用 `bash -c` 内联带反引号的字符串。
用法：python -I -X utf8 d:/4/Unity/工具/_fix_1014_a1993.py
"""
import io

P = r'D:\4\项目任务.md'
BAD = "⇒ 改 （英文）， 是对的**："
GOOD = "⇒ 改 `desc`（英文），`descZh` 是对的**："


def main():
    b = io.open(P, 'rb').read()
    assert b.count(b'\r\n') == 0
    t = b.decode('utf-8')
    n = t.count(BAD)
    assert n == 1, 'anchor hits=%d' % n
    t = t.replace(BAD, GOOD)
    assert '\x00' not in t and '\r\n' not in t
    io.open(P, 'wb').write(t.encode('utf-8'))
    print('OK 修好了（%d 处）' % n)
    # 顺带体检：这一行里不该再出现「（）」这种被吃空后留下的空括号
    for line in t.split('\n'):
        if line.startswith('| 🆕 **`A1993`**') or ('SOR29' in line and 'Ephrael' in line):
            assert '（）' not in line, '仍有被吃空的括号'
            assert '改 （英文）' not in line
    print('体检通过')


if __name__ == '__main__':
    main()
