#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B118_W4 的账补丁（`账/_parts/B118_W4.tsv`）。

一件事：
  · `战斗逻辑驱动.tsv` row 536 `BattleManager.ResolveResetDuty(CardScript target)` —— `B117-46` 落地留痕
    （落点不变：`EffectResolver.cs:15492` 的 `DoReloadDuty`；改的是**本体语义**：不再按 `duty` 筛、失败档已删）。

⛔ 不写主表；⛔ 不猜：按 (原版类, 方法) 精确定位，命中不是 1 处就报错退出。
⚠️ `B117-41`（`EventTiming.DeathHold`）是**我们自己的常量**、原版无对应方法 ⇒ 没有账行可改（状态记在 `活状态表`）。
"""
import io

BASE = r"d:/4/Unity/资料/完全复刻/账"
OUT = BASE + r"/_parts/B118_W4.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    assert b.count(b"\r\n") == 0, "主表应是纯 LF（现读不成立，先查行尾）"
    return [l for l in text.split("\n") if l != ""]


def find(rows, cls, method):
    hits = [i for i, l in enumerate(rows)
            if len(l.split("\t")) >= 11 and l.split("\t")[1] == cls and l.split("\t")[2] == method]
    if len(hits) != 1:
        raise SystemExit("定位失败：%s / %s 命中 %d 处" % (cls, method, len(hits)))
    return hits[0]


rows = read_rows(BASE + "/战斗逻辑驱动.tsv")
i = find(rows, "BattleManager", "ResolveResetDuty(CardScript target)")
r = rows[i].split("\t")
assert len(r) == 11, "列数不是 11（%d）" % len(r)
assert "EffectResolver.cs:15492" in r[6], "落点列与预期不符：%s" % r[6]

r[7] = r[7] + (
    "｜[B118_W4 2026-11-02] `B117-46` 落地（`EffectResolver.cs:15492` 的 `DoReloadDuty` 本体改判）："
    "**去掉 `Has(KeywordTable.Duty)` 筛选** ⇒ 目标 = 己方场上**全部存活单位（含督军 `Board[4]`）** / 单目标档 = `ctx.LastTarget`，"
    "与 `BattleManager__ResolveResetDuty.c:5-9`「对传进来的那张**无条件**清 `+0x4c`」＋ `AddResetDuty.c:34/98`「无 trait 判断」"
    "＋ 池内 `ca==475` 三张卡（`AM47`/`Cadian Honour`/`Lead by Example`）的 `traitsFilter`/`keywordFilter` 全 = 0 一致；"
    "并**删掉「目标表为空 ⇒ 这条没生效 / `unresolved.Add` / `return false`」这个失败档**（原版没有这条分支）。"
    "净行为对**带 duty 的单位**不变（`+0x4c` 原本就是假再写假）。断言 = `RuleEngine/Editor/B118W4_Assert.cs` A1–A5（未接线）。")

r[9] = r[9] + " · B118_W4"
r[10] = r[10] + (
    "｜[B118_W4 2026-11-02] `B117-46` 已落（见「差异」列）。判据 → `资料/完全复刻/报告/B117_F13_阻塞断言与剩余trait.md` §三 · "
    "`资料/完全复刻/报告/B118_W4_引擎两件.md`。零行数变化（改既有物理行，行号不漂）。")

out = ["\t".join(COLS), "\t".join(r)]
io.open(OUT, "wb").write(("\n".join(out) + "\n").encode("utf-8"))
print("WROTE", OUT, "行数 =", len(out))
