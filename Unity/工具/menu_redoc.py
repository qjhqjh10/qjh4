#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""把一份普查文档里的**工具表区段**全部换掉，**散文原样保留**。

用法：python 工具/menu_redoc.py <文档> <RT pid1>[:depth1] [<RT pid2>[:depth2] ...]

什么时候用它：**`menu_dump.py` 改了之后**，已经落盘的普查表就与工具不一致了
（文档里写着「表 = 某条命令的输出」，这句话得成立）。
例：2026-09-27 修了「布局组算子节点没滤 `m_IsActive=0`」，四个页的表全部重出过一遍。

⚠️ **别用「第 N 个围栏块」那种办法** —— 有的文档里表**根本没套围栏**
（`档案窗_Profile页.md` 当初是把工具输出直接 `>` 重定向进文件的）⇒ 会换错块、把散文写坏。
本脚本按**内容签名**识别（一行以 `# MonoScript 类名索引` 开头），与套不套围栏无关。
这一版改成**按内容签名识别**（一行以 `# MonoScript 类名索引` 开头 = 工具表区的起点），
与套不套围栏无关。

区段界定：
  起点 = 某行以 `# MonoScript 类名索引` 开头（**若它上一行是围栏则连围栏一起算**）
  终点 = 从起点往下，**最后一个**「像表区的行」（空行 / `|` 开头 / `#` 开头 / `⚠️` 开头 / 缩进列表）
         之后紧跟一个**不像表区的行**为止（**尾部的空行要吐回去**，否则会吃掉段间空行）
"""
import io
import subprocess
import sys

TOOL = 'd:/4/Unity/工具/menu_dump.py'
FENCE = chr(96) * 3
SIG = '# MonoScript 类名索引'

try:                                   # Windows 控制台默认 GBK ⇒ 打印 ⚠️ 会 UnicodeEncodeError
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
except Exception:
    pass


def table_shape(line):
    s = line.strip()
    return (s == '' or s.startswith('|') or s.startswith('#') or s.startswith('⚠️')
            or s.startswith('🔴') or s.startswith('·') or line.startswith('    '))


def regions(lines):
    out = []
    i = 0
    while i < len(lines):
        if lines[i].startswith(SIG):
            a = i
            if a > 0 and lines[a - 1].startswith(FENCE):
                a -= 1
            b = i
            while b + 1 < len(lines) and table_shape(lines[b + 1]):
                b += 1
            while b > i and lines[b].strip() == '':      # 尾部空行吐回去
                b -= 1
            if b + 1 < len(lines) and lines[b + 1].startswith(FENCE):
                b += 1
            out.append((a, b))
            i = b + 1
        else:
            i += 1
    return out


def fresh(spec):
    rt, _, d = spec.partition(':')
    depth = d or '8'
    r = subprocess.run([sys.executable, TOOL, 'bundle_menus_assets_all', '--rt', rt,
                        '--depth', depth, '--md'], cwd='d:/4/Unity', capture_output=True)
    return FENCE + '\n' + r.stdout.decode('utf-8', 'replace').rstrip('\n') + '\n' + FENCE


def main():
    doc = sys.argv[1]
    specs = sys.argv[2:]
    lines = io.open(doc, encoding='utf-8').read().split('\n')
    regs = regions(lines)
    if len(regs) != len(specs):
        print('⚠️ 找到 %d 个表区，但给了 %d 张新表 —— 中止，别乱改' % (len(regs), len(specs)))
        return 1
    out, prev = [], 0
    for (a, b), spec in zip(regs, specs):
        out.extend(lines[prev:a])
        out.extend(fresh(spec).split('\n'))
        prev = b + 1
    out.extend(lines[prev:])
    io.open(doc, 'wb').write('\n'.join(out).encode('utf-8'))
    print('%s：换了 %d 个表区' % (doc, len(regs)))
    return 0


if __name__ == '__main__':
    sys.exit(main())
