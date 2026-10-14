# -*- coding: utf-8 -*-
"""_check_table_pipes.py -- 查 markdown 表格行的竖线数分布（判据：本仓铁律「改表别拿行首当锚点」那条）。

用法:
  python -I -X utf8 d:/4/Unity/工具/_check_table_pipes.py <文件> [起始行 [结束行]] [--only 正则]

⚠️ 数竖线时排除 GFM 转义的 \|（`` `a\|b` `` 不分列）。
"""
import io, re, sys, collections

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
