# -*- coding: utf-8 -*-
r"""_check_table_pipes.py -- 查 markdown 表格行的竖线数分布（判据：本仓铁律「改表别拿行首当锚点」那条）。

## A25 2026-10-24: 本 docstring 首行加上 raw 前缀（字母 r + 三引号）—— 原文里的 `\|` 是无效转义，Python 3.12+ 每次运行都会
## 往 stderr 吐一条 SyntaxWarning（`--help` 也带）。raw 之后字面量一字不改（`__doc__` 内容逐字节相同），
## 只是不再触发转义解析。⛔ 不要改回普通 docstring，也不要把 `\|` 改成 `\\|`（那会让 docstring 里多一个反斜杠）。

用法:
  python -I -X utf8 d:/4/Unity/工具/_check_table_pipes.py <文件> [起始行 [结束行]] [--only 正则]

⚠️ 数竖线时排除 GFM 转义的 \|（`` `a\|b` `` 不分列）。
"""
import io, re, sys, collections

## A17 2026-10-24: -h/--help early exit (print usage only, exit 0; never run the real work).
import sys as _wf_help_sys
if any(_wf_help_sys.argv[_i] in ("-h", "--help") for _i in range(1, len(_wf_help_sys.argv))):
    try:
        _wf_help_sys.stdout.reconfigure(encoding="utf-8")
    except Exception:
        pass
    print('用法:\n  python -I -X utf8 d:/4/Unity/工具/_check_table_pipes.py <文件> [起始行 [结束行]] [--only 正则]\n  -h, --help  只打印本用法并退出（退出码 0），不执行任何真活。')
    _wf_help_sys.exit(0)

BAR = re.compile(r'(?<!\\)\|')


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    only = None
    for a in sys.argv[1:]:
        if a.startswith("--only"):
            only = a.split("=", 1)[1] if "=" in a else None
    path = args[0]
    lo = int(args[1]) if len(args) > 1 else 1
    hi = int(args[2]) if len(args) > 2 else 10 ** 9
    t = io.open(path, encoding="utf-8").read().split("\n")
    c = collections.Counter()
    rows = []
    for i, l in enumerate(t, 1):
        if i < lo or i > hi:
            continue
        if not l.startswith("|"):
            continue
        if only and not re.search(only, l):
            continue
        n = len(BAR.findall(l))
        c[n] += 1
        rows.append((i, n, l))
    print("文件: %s  行区间 [%d, %d]  表行数=%d" % (path, lo, hi if hi < 10 ** 9 else len(t), len(rows)))
    print("竖线数分布 =", dict(sorted(c.items())))
    for i, n, l in rows[:12]:
        print("  :%-6d pipes=%-3d %s" % (i, n, l[:110]))
    return 0


if __name__ == "__main__":
    sys.exit(main())
