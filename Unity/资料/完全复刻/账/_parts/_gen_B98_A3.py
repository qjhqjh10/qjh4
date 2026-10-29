# -*- coding: utf-8 -*-
"""B98_A3：从主表取 日常_奖励.tsv 第 515 / 526 行，只在「备注」列追加标记落地说明，产出补丁。
纪律：裸 split("\\t")、LF 行尾、不改状态列、不改其它列。"""
import io, os

BASE = "d:/4/Unity/资料/完全复刻/账"
src = os.path.join(BASE, "日常_奖励.tsv")
out = os.path.join(BASE, "_parts", "B98_A3.tsv")

raw = io.open(src, "rb").read()
txt = raw.decode("utf-8")
lines = txt.split("\n")
header = lines[0]
assert header.count("\t") == 10, "表头不是 11 列: %r" % header
assert header.split("\t")[0] == "域", header

ADD = {
    515: "｜[B98_A3 2026-10-29] 已按铁律 18 在 `PurchasePremiumWindow.cs:465` 就地补 `🔶 自己设计（非原版）` 标记"
         "（表拿不到时显示 '?'；⛔ 不编点数）—— 零行数变化，落点 `:470` 未变，状态保持 C。",
    526: "｜[B98_A3 2026-10-29] 已按铁律 18 在 `MissionsTab.cs:2611`（`GridMissionLayoutLogic` 类声明处）就地补 "
         "`🔶 自己设计（非原版）` 标记（纯函数替代 `FlexibleGridLayout` 列数规则）—— 零行数变化，"
         "落点 `:2538` 未变，状态保持 B。",
}

picked = []
for ln, add in ADD.items():
    row = lines[ln - 1]
    cols = row.split("\t")
    assert len(cols) == 11, (ln, len(cols), cols[:3])
    assert cols[5] in ("B", "C"), (ln, cols[5])
    assert "(" in cols[2] or cols[2].endswith(")"), cols[2]
    assert add not in cols[10]
    cols[10] = cols[10] + add
    assert len(cols) == 11
    new = "\t".join(cols)
    assert new.count("\t") == 10
    picked.append((ln, cols[5], new))

body = header + "\n" + "\n".join(p[2] for p in picked) + "\n"
io.open(out, "wb").write(body.encode("utf-8"))

# 回读校验：逐列往返
back = io.open(out, "rb").read().decode("utf-8").split("\n")
assert back[0] == header
for i, (ln, st, new) in enumerate(picked):
    got = back[1 + i].split("\t")
    assert len(got) == 11
    assert got[5] == st
    assert got[10] == new.split("\t")[10]
    orig = lines[ln - 1].split("\t")
    for c in range(11):
        if c == 10:
            continue
        assert got[c] == orig[c], (ln, c)
print("OK 写出 %s ：%d 行（行号 %s）" % (out, len(picked), [p[0] for p in picked]))
print("状态列：", [p[1] for p in picked])
