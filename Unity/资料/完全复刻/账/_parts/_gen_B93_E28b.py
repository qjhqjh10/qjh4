#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B93_E28b —— 只改账：把 B93_D4 那 3 条「落点是纯散文、没有 文件:行」的 A 档行规范化。

背景：`check_ledger_points.py` 判它们「落点里没有 `文件:行`」= **失败**（日常_奖励.tsv 的 3 条）。
做法：① 现读定位真实声明行；② 落点改成 `文件.cs:行（名字）`；③ 原散文原样移进「差异」列。
⛔ 只改「我们的落点 / 差异」两列；⛔ 不动状态、不动备注、不动主表、不动 .cs。
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账"
OUT = os.path.join(LEDGER, "_parts", "B93_E28b.tsv")
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

# (rn, 期望类, 期望方法, 新落点, 规范化的依据说明)
E = [
    (679, "RewardService", "CollectItemReward(RewardInfo reward)",
     "CardPresentation/Shell/Inventory.cs:2016（CollectItemReward）",
     "真声明 = `public static void CollectItemReward(RewardInfo reward)`"
     "（`public static class RewardService` 起于同文件 :1950）"),
    (680, "RewardService", "CollectItemReward<T>(T reference, string instanceID, int quantity)",
     "CardPresentation/Shell/Inventory.cs:1995（CollectItemReward）",
     "真声明 = `public static void CollectItemReward<T>(T reference, string instanceID, int quantity) where T : class`"
     "（同文件 :1950 `public static class RewardService`）"),
    (748, "TwoSidesEvent", "GetTotalStats()",
     "CardPresentation/Shell/LeaderboardData.cs:324（GetTotalStats）",
     "真声明 = `public static LocalLeaderboardResult GetTotalStats(string key)`"
     "（`public static class LeaderboardData` 起于同文件 :134）"),
]


def main():
    path = os.path.join(LEDGER, "日常_奖励.tsv")
    b = io.open(path, "rb").read()
    assert b.count(b"\r") == b.count(b"\r\n"), "裸 CR"
    lines = b.decode("utf-8").split("\n")

    patch, problems = [], []
    for rn, cls, mtd, land, why in E:
        row = lines[rn - 1].split("\t")
        if len(row) != 11:
            problems.append("rn=%d 列数 %d" % (rn, len(row)))
            continue
        if row[1].strip() != cls or row[2].strip() != mtd:
            problems.append("rn=%d 键不符: %r . %r" % (rn, row[1], row[2]))
            continue
        if "文件:行" in row[6] or ".cs:" in row[6]:
            problems.append("rn=%d 落点里已经有 `文件:行` 了，别覆盖：%r" % (rn, row[6][:80]))
            continue
        old_land = row[6]
        new = list(row)
        new[6] = land
        new[7] = (row[7] + "｜" if row[7].strip() else "") + \
                 "[B93-E28b 2026-10-28] **落点规范化**：原「我们的落点」列那段散文（`" + old_land + \
                 "`）移到此；" + why + "。"
        # 只允许改 6/7 列
        for i in range(11):
            if i in (6, 7):
                continue
            if new[i] != row[i]:
                problems.append("rn=%d 第 %d 列被意外改动" % (rn, i))
        patch.append(new)

    if problems:
        print("!! 先修这些问题（未写盘）：")
        for p in problems:
            print("   " + p)
        return 1

    body = ["\t".join(COLS)] + ["\t".join(r) for r in patch]
    for i, l in enumerate(body):
        assert len(l.split("\t")) == 11, "行 %d 列数不是 11" % (i + 1)
    data = ("\n".join(body) + "\n").encode("utf-8")
    assert b"\r" not in data
    io.open(OUT, "wb").write(data)

    back = [l for l in io.open(OUT, "rb").read().decode("utf-8").split("\n") if l.strip()]
    assert len(back) == len(E) + 1, "回读行数 %d" % len(back)
    for i, l in enumerate(back):
        assert len(l.split("\t")) == 11, "回读行 %d 列数不是 11" % (i + 1)
    print("OK 写出 %s：%d 行补丁（+表头）· 11 列 · LF-only" % (OUT, len(back) - 1))
    return 0


if __name__ == "__main__":
    sys.exit(main())
