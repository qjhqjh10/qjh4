#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：把账里不合规的「方法」格改成单方法签名，并把原注记挪进「备注」。
理由：`工具/merge_ledger_parts.py` 的 `looks_like_one_method` 会把
`OnCardPlayed(RawCard card) (MissionChallenge 子类)` 判成「一格塞多个方法」⇒ 整份补丁被拒（B103_W1 实测）。
用法：python -I -X utf8 d:/4/Unity/工具/b103_fix_method_cell.py [--apply]
"""
import io, sys

PATH = "d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv"
OLD_METHOD = "OnCardPlayed(RawCard card) (MissionChallenge 子类)"
NEW_METHOD = "OnCardPlayed(RawCard card)"
NOTE = "｜【B103 2026-10-30 订正】原「方法」格写作 `OnCardPlayed(RawCard card) (MissionChallenge 子类)`，" \
       "括号注记让 `merge_ledger_parts.looks_like_one_method` 判成「一格塞多个方法」⇒ 整份补丁被拒。" \
       "注记挪到这里，方法格只留单签名（原版类列已经是 `CardPlayedCount`，子类关系不缺信息）。"


def main():
    apply = "--apply" in sys.argv
    raw = io.open(PATH, "rb").read()
    nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
    lines = raw.decode("utf-8").split(nl)
    hits = [i for i, l in enumerate(lines) if l.strip() and len(l.split("\t")) > 10
            and l.split("\t")[2].strip() == OLD_METHOD]
    print("命中 %d 条" % len(hits))
    if len(hits) != 1:
        print("⛔ 命中数不是 1，跳过")
        return
    i = hits[0]
    c = lines[i].split("\t")
    print("原方法格 = %s" % c[2])
    print("原状态 = %s · 列数 = %d" % (c[5], len(c)))
    if apply:
        c[2] = NEW_METHOD
        c[10] = c[10] + NOTE
        lines[i] = "\t".join(c)
        io.open(PATH, "wb").write(nl.join(lines).encode("utf-8"))
        back = io.open(PATH, "rb").read().decode("utf-8").split(nl)[i].split("\t")
        assert back[2] == NEW_METHOD and len(back) == len(c), "往返校验失败"
        print("✅ 已写 · 方法格 = %s · 列数 %d · 行数 %d" % (back[2], len(back), len(lines)))
    else:
        print("（预演）")


if __name__ == "__main__":
    main()
