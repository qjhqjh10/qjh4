#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B92_L1 辅助（只读）：给 (文件, 名字) 列出声明候选行 + 该行文本。"""
import importlib.util
import io
import os
import sys

spec = importlib.util.spec_from_file_location("C", r"d:/4/Unity/工具/check_ledger_points.py")
C = importlib.util.module_from_spec(spec)
spec.loader.exec_module(C)

ROOTS = list(C.DEFAULT_ROOTS)


def read_lines(path):
    return C.read_src_lines(path)


def lookup(rel, name):
    full = C.find_file(rel, ROOTS)
    if full is None:
        print("!! 文件不存在: " + rel)
        return
    lines = read_lines(full)
    cands = C.find_decl_candidates(lines, name)
    print("== %s :: %s  (%s, %d lines)" % (rel, name, full, len(lines)))
    if not cands:
        print("   (no decl candidates)")
    for ln in cands:
        print("   :%d  %s" % (ln, lines[ln - 1].strip()[:150]))


def main():
    pairs = []
    for a in sys.argv[1:]:
        rel, name = a.split("::", 1)
        pairs.append((rel, name))
    for rel, name in pairs:
        lookup(rel, name)
        print()


main()
