# -*- coding: utf-8 -*-
"""check_pipes.py —— 表格「竖线数」普查（Markdown 表格列数一致性）

用法：
    python d:/4/Unity/工具/check_pipes.py <某个 .md> [<另一个 .md> ...]

为什么需要它（`CLAUDE.md` 那两条纪律的判据）：
  🔴 **改 `项目任务.md` §三 那张大表时，别拿「行首」当锚点。**
     想在某一行**前面插一行**，若 `old_string` 取的是 `| **A26** | 🔴 **…**` 这种**行首片段**、
     `new_string` 写成一整行 ⇒ **原来那一行丢了行首、尾巴被挂到新行后面**，
     表格列数变成 5 —— **而内容读起来还「像是对的」**（只有 linter 的 `MD056` 会报）。
  ✅ **改完立刻验那一行的竖线数**（3 列 = **4 条**竖线），或跑本脚本。

判据：**同一个连续表块里，所有行的竖线数必须相同**（块 = 行号连续的那些表行）。
  ⚠️ 数竖线时要**排除 GFM 转义的 `\\|`**（`` `a\\|b` `` 不分列）—— 否则会得到假警报。

输出：只打印**列数不一致的块**（块范围 + 各列数计数）。全一致则只打一行「竖线普查完成」。
退出码：0 = 全一致；1 = 有不一致（可直接接进别的脚本）。
"""
import io
import sys
from collections import Counter

BS = chr(92)


def scan(path):
    L = io.open(path, encoding='utf-8').read().split('\n')
    bars = []
    for i, s in enumerate(L):
        t = s.strip()
        if not t.startswith('|'):
            continue
        n = 0
        j = 0
        while j < len(t):
            if t[j] == BS:          # 跳过被转义的字符（含 \|）
                j += 2
                continue
            if t[j] == '|':
                n += 1
            j += 1
        bars.append((i + 1, n))

    bad = []
    prev = None
    run = []

    def flush(run):
        if not run:
            return
        c = Counter(x[1] for x in run)
        if len(c) > 1:
            bad.append((run[0][0], run[-1][0], dict(c)))

    for ln, n in bars:
        if prev is not None and ln != prev + 1:
            flush(run)
            run = []
        run.append((ln, n))
        prev = ln
    flush(run)
    return len(bars), bad


def main(argv):
    if not argv:
        print(__doc__)
        return 2
    rc = 0
    for p in argv:
        n, bad = scan(p)
        if bad:
            rc = 1
            print('🔴 %s：共 %d 行表行，列数不一致的块：' % (p, n))
            for a, b, c in bad:
                print('    行 %d-%d  各列数计数 = %s' % (a, b, c))
        else:
            print('✅ %s：竖线普查完成，共 %d 行表行，全一致' % (p, n))
    return rc


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
