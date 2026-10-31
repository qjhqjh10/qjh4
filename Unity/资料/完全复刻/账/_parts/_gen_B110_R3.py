# -*- coding: utf-8 -*-
# B110_R3 · 生成账补丁（只读主表 → 写 _parts/B110_R3.tsv；⛔ 不动主表）
# 规矩：裸 split("\t")（⛔ 不用 csv）、二进制读、先算进变量再写、写前做往返校验。
import io, sys

MAIN = r"d:/4/Unity/资料/完全复刻/账/战斗逻辑驱动.tsv"
OUT = r"d:/4/Unity/资料/完全复刻/账/_parts/B110_R3.tsv"

COLS = ["域", "原版类", "方法", "判据出处", "判据要点", "状态",
        "我们的落点", "差异", "等级", "轮次", "备注"]

NOTE = ("｜[B110_R3 2026-10-31] **答案的传输/持久化链已补（账 N07）**：`AiAction.OathChoiceRecorded/OathChoice`"
        "（`RuleEngine/Data/AiActionPartial_B110R3.cs`，`SimpleAI.cs` 的类改 `partial`、零行数变化）→ "
        "`MsgAction.oathRecorded/oathChoice` + `ToWire`/`FromWire`（`CardPresentation/Net/NetProtocol.cs`）→ "
        "`BattleDriver.LocalAct` 落地前 `TryPeekOathAnswer` 抓进动作（`BattleContextPartial_OathPeek.cs`）→ "
        "`BattleDriver.RecMsg` 逐字段抄（⚠️ 漏抄 = 联机录像里对手那一半丢光）→ "
        "`NetApply.Apply` 的 `PlayCard` 支落地前 `SetOathAnswer`（**回放/重连/对端同一条路**）。"
        "在场哨兵 `oathRecorded=false` = 没记（老档 / AI / 玩家没答）⇒ 兜底「付得起就付」逐字不变；"
        "`NetProtocol.Version` 与 `ReplayRecord.version` **都不 +1**（纯新增字段，同族先例 `MsgDeck.draftPropsVer` / `ReplayVars.varsRecorded`）。"
        "⚠️ 落点行号已随本批改动整体下移（`BattleDriver.cs` +35 · `NetProtocol.cs` +20 · `NetApply.cs` +15），"
        "需在同步点跑 `check_ledger_points --all-A --fix` 重钉。"
        "断言 = `NetSelfTest` I⑩~I⑫ · `NetBattleTest` N1~N6c · `BattleScene` B110_R3-4/4a/5/5a。"
        "｜🔶 仍自设计：布尔表示（原版是变体 SO 的下标 `filterSpecificCards`，我们池里只有一张合并卡，见 `B88_R3` §5·0 口径 A）")

raw = io.open(MAIN, "rb").read()
assert b"\r" not in raw, "主表里有 CR（本脚本按 LF 假定）"
lines = raw.decode("utf-8-sig").split("\n")

head = lines[0].split("\t")
assert head == COLS, "表头不是 11 列标准表头: %r" % (head,)

hits = []
for i, l in enumerate(lines):
    c = l.split("\t")
    if len(c) == 11 and c[1] == "BattleManager" and c[2].startswith("ChoiceOfCardEnemy("):
        hits.append((i, c))
assert len(hits) == 1, "命中 %d 处（要求恰好 1）" % len(hits)
idx, row = hits[0]
assert row[8] == "P0" and row[5] == "A", "该行状态/等级与预期不符: %r %r" % (row[5], row[8])

row = list(row)
row[9] = row[9] + "·B110_R3"
row[10] = row[10] + NOTE
assert len(row) == 11

out_text = "\t".join(COLS) + "\n" + "\t".join(row) + "\n"
data = out_text.encode("utf-8")

# 写前校验打出来（先算进变量，最后一步才写盘）
print("source line:", idx + 1)
print("row cols:", len(row))
print("output bytes:", len(data))

io.open(OUT, "wb").write(data)

# 往返校验：读回来逐列比
back = io.open(OUT, "rb").read().decode("utf-8-sig").split("\n")
assert back[0].split("\t") == COLS, "回读表头不一致"
assert len(back[1].split("\t")) == 11, "回读列数不一致: %d" % len(back[1].split("\t"))
assert back[1].split("\t") == row, "回读逐列不一致"
print("OK ->", OUT)
