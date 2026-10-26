# -*- coding: utf-8 -*-
"""把指定账行的落点重定位到**最近的祖先方法声明**（⛔ 只改落点，不改状态）。

用法（必须显式给键文件，否则什么都不做）：
    python -I -X utf8 d:/4/Unity/工具/fix_landing_decl.py --keys keys.txt [--dry]

keys.txt 每行：`<域文件.tsv>\t<原版类>\t<方法签名前缀>`（tab 分隔）。
行为：
  · 读该行的 `我们的落点`，取第一个 `文件.cs:起`
  · 从 `起` 往前最多 300 行找**方法声明**（排除 if/for/while/switch/catch/else/using/return/lock/fixed）
  · 命中 ⇒ 落点改成 `文件.cs:声明行-起（等价实现 = 声明名）`
  · 未命中 ⇒ 打印，不改
⚠️ 只改 `状态 == A` 的行。主表以外不碰。
"""
import io
import os
import re
import sys

LEDGER = r"D:/4/Unity/资料/完全复刻/账/"
A = r"D:/4/Unity/MyGame/Assets/"
ROOTS = [A, A + "CardPresentation/", "D:/4/Unity/"]

CTRL = ('if', 'for', 'foreach', 'while', 'switch', 'catch', 'else', 'using',
        'return', 'lock', 'fixed', 'do', 'try', 'finally', 'case', 'default')
# 修饰符 + 返回类型 + 名字 + (
DECL = re.compile(
    r'^(?:(?:public|private|protected|internal|static|sealed|override|virtual|async|abstract|extern|new|unsafe|partial|readonly)\s+)*'
    r'[\w<>\[\],\.\? ]+?\s(\w+)\s*[\({]')
REF = re.compile(r'([A-Za-z0-9_]+(?:/[A-Za-z0-9_.-]+)*)[.]cs:(\d+)')


def resolve(rel):
    for r in ROOTS:
        if os.path.exists(r + rel + '.cs'):
            return r + rel + '.cs'
    return None


def decl_at(src, ln):
    """ln 是从 1 开始的行号；返回 (行号, 名字) 或 None"""
    if not (1 <= ln <= len(src)):
        return None
    t = src[ln - 1].strip()
    if not t or t.startswith('//') or t.startswith('*') or t.startswith('/*'):
        return None
    m = DECL.match(t)
    if not m:
        return None
    if m.group(1).lower() in CTRL:
        return None
    return ln, m.group(1)


def main():
    a = sys.argv[1:]
    if '--keys' not in a:
        print(__doc__)
        print('（本次什么都没做。）')
        return 2
    keyfile = a[a.index('--keys') + 1]
    dry = '--dry' in a
    keys = []
    for l in io.open(keyfile, encoding='utf-8'):
        l = l.rstrip('\n')
        if not l.strip():
            continue
        parts = l.split('\t')
        if len(parts) != 3:
            print('!! 键行格式不对：%r' % l)
            continue
        keys.append(tuple(x.strip() for x in parts))

    changed = 0
    for domfile, klass, meth in keys:
        p = LEDGER + domfile
        if not os.path.exists(p):
            print('!! 找不到 %s' % p)
            continue
        lines = io.open(p, 'rb').read().decode('utf-8').split('\n')
        for li, l in enumerate(lines):
            c = l.split('\t')
            if len(c) != 11 or c[5] != 'A':
                continue
            if c[1] != klass or not c[2].startswith(meth):
                continue
            m = REF.search(c[6])
            if not m:
                print('  ? 无落点文件: %s.%s' % (klass, meth[:30]))
                break
            path = resolve(m.group(1))
            if not path:
                print('  ? 路径解不出: %s' % m.group(1))
                break
            src = io.open(path, 'rb').read().decode('utf-8', errors='replace').split('\n')
            start = int(m.group(2))
            found = None
            for k in range(min(start, len(src)), max(0, start - 300), -1):
                d = decl_at(src, k)
                if d:
                    found = d
                    break
            if not found:
                print('  ! 回溯 300 行没找到声明: %s.%s @%s:%d'
                      % (klass, meth[:24], m.group(1), start))
                break
            newland = '%s.cs:%d-%d（等价实现 = %s）' % (m.group(1), found[0], start, found[1])
            print('  ✓ %-22s %-28s → %s:%d-%d（%s）'
                  % (klass, meth[:28], m.group(1), found[0], start, found[1]))
            if not dry:
                c[6] = newland
                lines[li] = '\t'.join(c)
                io.open(p, 'wb').write('\n'.join(lines).encode('utf-8'))
            changed += 1
            break
    print('\n改了 %d 行%s' % (changed, '（dry，未写盘）' if dry else ''))
    return 0


if __name__ == '__main__':
    sys.exit(main())
