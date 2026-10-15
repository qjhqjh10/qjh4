# -*- coding: utf-8 -*-
"""补三笔账（A2051/A2052/A2053）。先按行打印竖线数再插，便于定位列数问题。"""
import io

TASK = r"d:/4/Unity/资料/任务_卡组卡牌战斗.md"
BAR = "\\u007c"      # 表里「卡id|sprite」那种竖线要转义，否则会把 markdown 表格切坏

ROWS = [
"| **`A2051`** | ✅ **2026-10-15 已修**：**`gen_icon_plan.py` 的中文段首扫描不认 `<br>`** —— `ASH34` 的 `天赋` / `UM23` 的 `典籍` 两处声明**一次都抽不出来** ⇒ **那两张的中文卡面压根没画那一枚图标**。判据 = `IconSetup.Verify` 的 ④a `ZhGaps` 报的两条（**实测是真缺口**；同池对照：真换行那几处 `GOF76`/`SW24` 抽得出、只有 `<br>` 抽不出）。修法 = `KEYWORD_SCAN_ZH` 的前导分隔符收 `<br>` 系列 + 重跑 `--write`（现读两处都进表了）。出处 → `资料/普查产出_第三十二会话/D3_IconSetup五条红.md` §一·4 |",
"| **`A2052`** | ⚠️ **池里 113 张卡的 `id` 仍是 32 位 GUID**（`Skyborne Deployment` = `e59ac9e4…`，光 SaimHann 就 8 张）—— **上一会话那轮真值落位没覆盖这一档**；与 `A970`/`card_ids.json` 同源，但**规模 113 值得单记**。⚠️ 连带症状：`BattleScene` 那条「Skyborne Deployment 试了 8 个种子」的夹具因此被 `DeckRules.CannotAddToDeck` 摘掉。出处 → `D2_18条红诊断.md` 顺手发现 #1 |",
"| **`A2053`** | ⚠️ **`GOF20` 名字对不上**：`card_ids.json` 写 `Veteran Stormboy`，池里是 **`Veteran Flyboy`**；PnP 只有 `Orks/3部队/Warpforge_31_Veteran-Stormboy.png`、全池没有 `Veteran Stormboy` ⇒ 这是预组 `OrksDeck2` `size:28/30`（`complete=false`）的直接原因。出处 → `D2_18条红诊断.md` 顺手发现 #3 |",
]

for r in ROWS:
    print("竖线 %d  %s" % (r.count("|"), r[:50]))
    assert r.count("|") == 3, "竖线 %d" % r.count("|")

s = io.open(TASK, encoding="utf-8", newline="").read()
lines = s.split("\n")
hit = [i for i, L in enumerate(lines) if L.startswith("| **`A2050`** |")]
assert len(hit) == 1, hit
lines[hit[0] + 1:hit[0] + 1] = ROWS
io.open(TASK, "wb").write("\n".join(lines).encode("utf-8"))
b = io.open(TASK, "rb").read()
print("CRLF", b.count(b"\r\n"), "LF", b.count(b"\n"))
