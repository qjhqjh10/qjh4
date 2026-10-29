# -*- coding: utf-8 -*-
# _gen_B98_A6.py — 生成账补丁 `B98_A6.tsv`（账 `特效动画:195` · 执行代理 B98_A6 · 2026-10-29）
#
# 做法：**从主表第 195 行原样起手**、只改该改的列 ⇒ 未改列逐字保真（⛔ 不手工重打整行）。
# 规矩：裸 `split("\t")`（与 `工具/merge_ledger_parts.py` 同口径）· LF 行尾 · 11 列。
import io, sys

BASE = r"D:/4/Unity/资料/完全复刻/账"
MAIN = BASE + "/特效动画.tsv"
OUT = BASE + "/_parts/B98_A6.tsv"
ROWNO = 195          # 1-based，含表头

raw = io.open(MAIN, "rb").read()
assert raw.count(b"\r\n") == 0, "主表不是纯 LF"
lines = raw.decode("utf-8").split("\n")
head = lines[0].split("\t")
assert len(head) == 11, head
old = lines[ROWNO - 1].split("\t")
assert len(old) == 11, len(old)
assert old[1] == "CardEffectItem" and old[2].startswith("LoadEffect("), old[1:3]

f = list(old)

# ---- 判据出处：补上本批的判据文件（§一 = 账行 195）------------------------------------------
f[3] = f[3] + " · 判据/B98_R1_特效3条闭合.md §一"

# ---- 状态：B → A ------------------------------------------------------------------------------
assert f[5] == "B", f[5]
f[5] = "A"

# ---- 我们的落点：补类尾两个新函数 + 订正「手牌兜底」那半句 + 补本批断言文件 ---------------------
t = "旧口 :851 `RowsOf`（仍留给旧断言与手牌兜底）"
assert t in f[6], f[6]
f[6] = f[6].replace(t, "旧口 :851 `RowsOf`（无生产调用点，仍留给旧断言；「手牌兜底」那半句已不成立 —— S5 起项源走 `Of()`）")
anchor = "· :2550（环境卡整组闸）"
assert anchor in f[6], f[6]
f[6] = f[6].replace(
    anchor,
    "· :2484 `MarkRow`（mark 行：左专名 + 右契约正文）· :2616 `MarkGodEffectKey` / :2636 `MarkGodEffectText`"
    "（右列取值，追加在类尾）" + anchor)
a2 = "的 A5/A6/A9/B1/B2/C1"
assert a2 in f[6], f[6]
f[6] = f[6].replace(a2, a2 + " ＋ `Editor/B98A6_Assert.cs`（mark 行右列）")

# ---- 差异：追加本批的订正（含 B95_FX 的错因）--------------------------------------------------
f[7] = f[7] + (
    " ｜ 🔴 2026-10-29（B98_A6）**mark 行右列订正 —— `B95_FX` 那条结论是错的**："
    "原版 `CardEffectItem__LoadEffect.c:98-101` 写完左列就 `goto LAB_18060aaa7` ⇒ `.c:148-153` 照样"
    "`effectText = GetEffectDesc(effect)`；`GameStaticData__GetEffectDesc.c:396-413` 的 `case 8: case 0x11:` 拼的是"
    "**`\"Battle/Effect/\" + MarkOfChaosType 枚举名`**（前缀是字面量，`out_il2csharp/GameStaticData.cs:1348-1353`；"
    "⛔ 不是 `CardTraitDescription/` —— 那一族属于另一个方法），四条键 `Battle/Effect/{Khorne,Nurgle,Slaanesh,Tzeentch}`"
    "在官方 10,862 键表里**都在**（`Resources/official_loc_13lang.json` 下标 787-790）⇒ 原版 mark 行**两列都有字、整格显示**。"
    "我们原来 `what` 硬写空串 ⇒ 拉近视图那一行右列**空白**（S5 已接 ⇒ 在线可见）；"
    "本轮改为 `MarkGodEffectText(markOfChaosType)` 并按原版用 `TextRow`（右列空 ⇒ 整行折空，= `IsTextEmpty` 只读右列），"
    "旧口 `RowsOf` 同处一起改 ⇒ 判据对齐、全分支可达，**升 A**。"
    "⚠️ `B95_FX` 的错因：只查了 `CardTraitDescription/*` 那一族（而 `GetEffectDesc` 走的是另一族键），"
    "于是把`施工细则 §1.6` 里**正确**的 `Battle/Effect/Khorne` 反过来改坏了。")

# ---- 轮次：追加本批 ---------------------------------------------------------------------------
assert "B97_W16" in f[9], f[9]
f[9] = f[9] + " · B98_A6"

# ---- 备注：把「没查清」那条结案 + 记下未接的旧断言 ----------------------------------------------
dead = ("⚠️ 由此引出一条**没查清**：mark 行的右列也是空 ⇒ 按同一判据原版 mark 行**整格也会被 Disable**"
        "（= 原版可能根本不显示 mark 行）；这与 `B95_FX` 的 `what=` 结论**冲突**，**未拍板**（报告 §没查清）")
assert dead in f[10], f[10]
f[10] = f[10].replace(dead, (
    "✅ **2026-10-29（B98_A6）已结案**：mark 行右列**不空**（`Battle/Effect/<神名>`，四条键官方表里都在）"
    "⇒ `IsTextEmpty` 为假 ⇒ 原版 mark 行**整格显示**（不是被 Disable）；`B95_FX` 的 `what=` 结论已就地推翻"
    "（判据 → `判据/B98_R1_特效3条闭合.md` §一；报告 → `报告/B98_A6.md`）。"
    "`IsEmptyRow`（判「两列都空」）那一格**仍未改**（槽位模型红线）—— mark 行靠 `TextRow` 折行对齐原版。"
    "⚠️ **未接的旧断言**：`Editor/B97W16_Assert.cs:357` 的 B1-b 仍断「右列 = 空串」⇒ 本批会让它红，"
    "须同步改成新判据（本批白名单不含 `Editor/*.cs`，可粘贴补丁见 `报告/B98_A6.md`）。"))

row = "\t".join(f)
assert "\r" not in row and "\n" not in row
assert len(f) == 11

out = "\t".join(head) + "\n" + row + "\n"
io.open(OUT, "wb").write(out.encode("utf-8"))

# ---- 往返校验：读回来逐列比对（8 列全等才算没污染）--------------------------------------------
back = io.open(OUT, "rb").read()
assert back.count(b"\r\n") == 0, "写出了 CRLF"
bl = back.decode("utf-8").split("\n")
assert len(bl) == 3 and bl[2] == "", bl[-1:]
assert bl[0].split("\t") == head, "表头变了"
got = bl[1].split("\t")
assert len(got) == 11, len(got)
for i in range(11):
    assert got[i] == f[i], ("列 %d 往返不一致" % (i + 1), got[i][:80], f[i][:80])

print("OK ->", OUT)
print("表头逐字照主表：", "\t".join(head))
for i in range(11):
    chg = "改" if got[i] != old[i] else "  "
    print(" %s %-6s | %s" % (chg, head[i], got[i][:120]))
