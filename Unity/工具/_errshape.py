#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""_errshape.py —— 把 Unity 编译错误按「被调用的函数」归类，用来判断值不值得批量修。

用法：python _errshape.py <编译器日志> <工程根>
"""
import collections
import io
import re
import sys

PAT = re.compile(r"^(Assets[^(]*)\((\d+),(\d+)\): error (CS\d+)")


def main(log, proj):
    seen = set()
    by_code = collections.Counter()
    by_fn = collections.Counter()
    by_file = collections.Counter()
    for raw in io.open(log, encoding="utf-8", errors="ignore"):
        line = raw.replace("\\", "/")
        m = PAT.match(line)
        if not m:
            continue
        f, ln, col, code = m.group(1), int(m.group(2)), int(m.group(3)), m.group(4)
        if (f, ln, col, code) in seen:
            continue
        seen.add((f, ln, col, code))
        by_code[code] += 1
        by_file[f] += 1
        try:
            src = io.open(proj + "/" + f, encoding="utf-8", errors="ignore").read().split("\n")[ln - 1]
        except Exception:
            continue
        pre = src[:col - 1]
        # 找错误位置左边最近的一个「名字(」，就是被调用的那个函数
        m2 = None
        for mm in re.finditer(r"([A-Za-z_][A-Za-z0-9_.]*)\s*\(", pre):
            m2 = mm
        if m2:
            by_fn[m2.group(1)] += 1
        else:
            by_fn["<不是调用：赋值/返回等>"] += 1

    print("去重后的错误点 %d 个" % len(seen))
    print("\n按错误码：")
    for k, v in by_code.most_common():
        print("   %-8s %d" % (k, v))
    print("\n按文件：")
    for k, v in by_file.most_common(12):
        print("   %-52s %d" % (k, v))
    print("\n按「被调用的函数」（前 22）：")
    for k, v in by_fn.most_common(22):
        print("   %-40s %d" % (k, v))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
