#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""生成 B109_E2 的账补丁（`账/_parts/B109_E2.tsv`）。

只改 `锻造.tsv` 的两行（都是本批改动直接造成的）：
  · row 24 `BoosterPackVisualItem.StartCardAnimationEvent()` —— 落点重钉（`:900/:1332` → `:1664/:906/:1365`）
  · row 23 `BoosterPackVisualItem.Initialize(...)` —— [7] 里 ④「开包时间线仍是 Build 后立刻起播」已不成立（铁律 5 就地订正）

⛔ 不写主表；⛔ 不猜：两行都按 (原版类, 方法) 精确定位，找不到就报错退出。
"""
import io
import os
import sys

LEDGER = r"d:/4/Unity/资料/完全复刻/账/锻造.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B109_E2.tsv"
COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]


def read_rows(path):
    b = io.open(path, "rb").read()
    text = b.decode("utf-8-sig")
    assert b.count(b"\r\n") == 0, "主表应是纯 LF（现读不成立，先查行尾）"
    return [l for l in text.split("\n") if l != ""]


def find(rows, cls, method_prefix):
    hits = []
    for i, l in enumerate(rows):
        c = l.split("\t")
        if len(c) < 11:
            continue
        if c[1] == cls and c[2].startswith(method_prefix):
            hits.append(i)
    if len(hits) != 1:
        raise SystemExit("定位失败：%s / %s* 命中 %d 处" % (cls, method_prefix, len(hits)))
    return hits[0]


rows = read_rows(LEDGER)
assert len(rows[0].split("\t")) == 11, "表头不是 11 列"

i24 = find(rows, "BoosterPackVisualItem", "StartCardAnimationEvent(")
i23 = find(rows, "BoosterPackVisualItem", "Initialize(")
r23 = rows[i23].split("\t")
r24 = rows[i24].split("\t")
assert len(r23) == 11 and len(r24) == 11

# ---------------- row 24：落点重钉 ----------------
r24[6] = ("Shell/BoosterPackOpenWindow.cs:1664（StartOpenAnimations —— 开包时间线的**起点**；"
          "= 原版 `windowAnimation.Play()` 那一跳）＋ :906（`OpenTimelineGate()` 闸住的开包 clip 推进 :907-931）"
          "＋ :1365（StepBgShake，同样由 `_openRequested` 闸住）")

r24[7] = (r24[7].rstrip("。")
          + "｜🔴 **2026-10-31（`B109_E2`）就地订正（铁律 5）**：上面「显式时间线 + 两条曲线」实现没变，但"
            "**起点以前是错的** —— 原版这句 `Play()` 只在包实物那条 clip 的 **t=2.0** 打 `StartCardAnimationEvent` 时发生；"
            "我们原来 `Build()` 之后第一拍就起播（**早 2 s**）。现在 `Advance` 由 `OpenTimelineGate()` 闸住："
            "事件到之前一秒都不走（`OpenT` 恒 0、5 张卡停在 state 1／卡节点 scale 恒 1），事件到才从 0 起。"
            "判据 = `decomp_full/BoosterPackOpenWindow__BoosterPackOpen.c`（只有两句 `Animation.Play`、没有自启路径）"
            "＋ `decomp_full/BoosterPackVisualItem__StartCardAnimationEvent.c`（唯一效果 = 转调宿主窗 `BoosterPackOpen()`）"
            "＋ `Resources/misc_clips.json`（两条 `Booster Open *` 的 `events[].t = 2.0`；⚠️ 是**事件时刻**不是 clip 长度，"
            "Standard 末帧 2.3667）＋ `BoosterPackOpenWindow__AnimationEndEvent.c`（播完 ⇒ 逐张 `ChangeState(2)`）。"
            "验收 = `Editor/B109_E2_Booster_Assert.cs`。")

r24[10] = (r24[10].rstrip("｜ ")
           + "｜[B109_E2 2026-10-31] 落点重钉：`Shell/BoosterPackOpenWindow.cs:900（Advance 的开包时间线 :905-922）"
             "＋ :1332（StepBgShake）` → `:1664 StartOpenAnimations（起点）` ＋ `:906 OpenTimelineGate（闸）` ＋ "
             "`:1365 StepBgShake`。⚠️ `:1332` 早在 `B108_A13` §③ 就被记成漂到一句 `return;`（本轮现读 `void StepBgShake` 在 `:1365`）。"
             "开包时间线已改成**事件驱动**（修掉「比原版早 2 s」）。")

# ---------------- row 23：[7] 的 ④ 已不成立（铁律 5 就地订正）----------------
f7 = r23[7]
i = f7.find("④ **开包时间线仍是")
j = f7.find("断言全变）。", i)
if i < 0 or j < 0:
    raise SystemExit("row23 [7] 的 ④ 那句没找到（原文可能已被别人改过）—— 停下来人工看")
NEW = ("④ 🔴 **2026-10-31（`B109_E2`）已修 —— 下面引号里是订正前的原话，⛔ 别当现状**："
       "「**开包时间线仍是『Build 后立刻起播』**，原版由那条 clip 在 **t=2.0** 打的 AnimationEvent 触发 "
       "⇒ 我们比原版**早 2 s**」。现在是**事件驱动**：`Advance` 由 `OpenTimelineGate()` 闸住，"
       "起点 = 那条 clip 的 t=2.0 事件（判据 → 报告 `B109_E2.md`；宿主那条断言的改动单在报告 §4）。")
r23[7] = f7[:i] + NEW + f7[j + len("断言全变）。"):]

r23[10] = (r23[10].rstrip("｜ ")
           + "｜[B109_E2 2026-10-31] 就地订正本行 [7] 的 ④：开包时间线**已经不是「Build 后立刻起播」**"
             "（起点 = 包实物 clip 的 t=2.0 `StartCardAnimationEvent`）。⚠️ 连带：`Editor/ShopScene.cs` 那两条"
             "「推进 0.65 s ⇒ 开包 clip 播完」要**先打这一拍**（可粘贴补丁 → `资料/完全复刻/报告/B109_E2.md` §4），"
             "⛔ 本代理不改宿主。")

out = [rows[0]] + ["\t".join(r23), "\t".join(r24)]

# 硬校验：11 列 / 无 tab / 无换行
for k, l in enumerate(out):
    c = l.split("\t")
    if len(c) != 11:
        raise SystemExit("第 %d 行列数 = %d（要 11）" % (k, len(c)))
    if "\r" in l or "\n" in l:
        raise SystemExit("第 %d 行含换行" % k)

text = "\n".join(out) + "\n"
io.open(OUT, "wb").write(text.encode("utf-8"))
print("写好了：", OUT)
print("行数：", len(out), "（1 表头 + 2 数据）")
for k, l in enumerate(out):
    print("  row%d cols=%d  %s" % (k, len(l.split("\t")), l[:90]))
