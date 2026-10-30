#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""B103：把两条「实现已在 + 断言已在宿主里跑绿」的 B 行改为 A。
证据（2026-10-30 本会话复跑 `_tmp_view/b103r2/battle.log`）：
  · B102C506 表现半合计 11 通过 / 0 失败
  · B102_R3 账 特效动画:503 合计 26 通过 / 0 失败
用法：python -I -X utf8 d:/4/Unity/工具/b103_flip_two.py [--apply]
"""
import io, sys

LEDGER = "d:/4/Unity/资料/完全复刻/账/"
JOBS = [
    ("战斗逻辑驱动.tsv", "ResolveCreateHandCard(BattleAction battleAction)",
     "【B103 2026-10-30 判 A】R1+R5 已落、断言已接线并在 `BattleScene.Run` 里**实跑绿**（`[B102C506] 表现半合计：11 通过 / 0 失败`）。"
     "原「actingCard 只能传 null / 池内 0 张可观测」是**判据面**的休眠说明，不是缺口 —— 机制已建、闸在 `SpellType==230` 上。"),
    ("特效动画.tsv", "ActivateTraitParticlesFromCodeInTarget(DefinedTrait trait, CardScript targetCard",
     "【B103 2026-10-30 判 A】消费侧已接、断言已接线并**实跑绿**（`[B102TRAIT] === B102_R3 账 特效动画:503 合计：26 通过 / 0 失败 ===`）。"),
]


def main():
    apply = "--apply" in sys.argv
    for fn, msub, note in JOBS:
        path = LEDGER + fn
        raw = io.open(path, "rb").read()
        nl = "\r\n" if raw.count(b"\r\n") > 0 else "\n"
        lines = raw.decode("utf-8").split(nl)
        hits = [i for i, l in enumerate(lines)
                if l.strip() and len(l.split("\t")) > 10 and msub in l.split("\t")[2]
                and l.split("\t")[5].strip() == "B"]
        print("== %s | %s | 命中 B 行 %d" % (fn, msub[:40], len(hits)))
        if len(hits) != 1:
            print("   ⛔ 命中数不是 1，跳过")
            continue
        i = hits[0]
        c = lines[i].split("\t")
        if apply:
            c[5] = "A"
            c[10] = c[10] + "｜" + note
            lines[i] = "\t".join(c)
            io.open(path, "wb").write(nl.join(lines).encode("utf-8"))
            back = io.open(path, "rb").read().decode("utf-8").split(nl)[i].split("\t")
            assert back[5] == "A" and len(back) == len(c), "往返校验失败"
            print("   ✅ 已写 A · 列数 %d" % len(back))
    print("完成" if apply else "（预演）")


if __name__ == "__main__":
    main()
