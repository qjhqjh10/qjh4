# -*- coding: utf-8 -*-
"""
_probe_envcard_panel.py — 把 decomp_full 的 .c 里出现的 DAT_0x184XXXXXX 常量解析成字符串字面量。

用法:
  PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/_probe_envcard_panel.py DAT_1842a5eb0 DAT_184283060
  PYTHONIOENCODING=utf-8 python d:/4/Unity/工具/_probe_envcard_panel.py --file <某个.c>

判据: DAT_0x184XXXXXX 的地址 = 0x180000000 + stringliteral.json 里那条的 address
      (本文件只做查表，不做推导；查不到就打印 NOT_FOUND)
只读: 只读 d:/2/tools/il2cpp_out/stringliteral.json，不写任何东西。
"""
import io
import json
import os
import re
import sys

SL = 'd:/2/tools/il2cpp_out/stringliteral.json'
BASE = 0x180000000


def load():
    with io.open(SL, encoding='utf-8') as f:
        arr = json.load(f)
    m = {}
    for e in arr:
        m[int(e['address'], 16)] = e['value']
    return m


def resolve(m, dat_name):
    """DAT_1842a5eb0 -> VA 0x1842a5eb0 -> 表内 address 0x42a5eb0"""
    va = int(dat_name.replace('DAT_', ''), 16)
    key = va - BASE
    if key in m:
        return m[key], key
    # 容错: 有些 DAT_ 是类静态块指针(非字符串), 找最近的下界
    return None, key


def main():
    args = sys.argv[1:]
    m = load()
    if args and args[0] == '--file':
        with io.open(args[1], encoding='utf-8', errors='replace') as f:
            txt = f.read()
        names = sorted(set(re.findall(r'DAT_[0-9a-fA-F]{9,}', txt)))
        for n in names:
            v, k = resolve(m, n)
            print('%-16s  0x%x  %s' % (n, k, repr(v) if v is not None else 'NOT_A_STRINGLIT'))
        return
    if not args:
        args = ['DAT_1842a5eb0', 'DAT_184283060']
    for n in args:
        v, k = resolve(m, n)
        print('%-16s  0x%x  %s' % (n, k, repr(v) if v is not None else 'NOT_A_STRINGLIT'))


if __name__ == '__main__':
    main()
