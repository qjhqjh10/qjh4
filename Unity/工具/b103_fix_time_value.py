#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：订正账 `战斗逻辑驱动:1383` 的阻塞时长 0.6 s → 1.078 s（`B103_R5` 实读常量算出来的）。
只改那一行的「差异」列里的那一句，其它一个字不动。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_time_value.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
METHOD_PREFIX = "DisplayEnemyCardInCenter("
OLD = "**原版那 0.6 s 的阻塞我们没建模**"
NEW = ("**原版那 1.078 s 的阻塞我们没建模**"
       "（🔴 2026-10-30 `B103_R5` 实读订正：原记「0.6 s」是错的，真值 = `DAT_1834b2dc4 0.1` + "
       "`0.3×0.66` + `0.3×2.6` = **1.078 s**，常量全部经 `read_literal.py` 从 DLL 实读；"
       "`DOMove` 那 0.15 是并行补间、不计）")


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines) if l.strip() and len(l.split("\t")) > 7
            and l.split("\t")[2].strip().startswith(METHOD_PREFIX)]
    print("命中 %d 条" % len(hits))
    if len(hits) != 1:
        print("⛔ 命中数不是 1，跳过")
        return
    i = hits[0]
    c = lines[i].split("\t")
    if OLD not in c[7]:
        print("⛔ 差异列里找不到待替换的那句，跳过")
        return
    print("状态 = %s · 列数 = %d" % (c[5], len(c)))
    if apply:
        c[7] = c[7].replace(OLD, NEW)
        lines[i] = "\t".join(c)
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)[i].split("\t")
        assert NEW in back[7] and len(back) == len(c), "往返校验失败"
        print("✅ 已写 · 列数 %d · 行数 %d" % (len(back), len(lines)))
    else:
        print("（预演）")


if __name__ == "__main__":
    main()
