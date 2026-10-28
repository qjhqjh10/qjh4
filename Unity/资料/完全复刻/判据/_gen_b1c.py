# -*- coding: utf-8 -*-
"""B92_B1c 账补丁生成器（第三轮施工：actionType 回填 6 处 + WaitingForBlockingAction 查询口）。
只读主表 `账/战斗逻辑驱动.tsv` 取 11 列骨架 → 写 `账/_parts/B92_B1c.tsv`。⛔ 不写主表、不改 .cs。"""
import io, os

BASE = r"d:/4/Unity/资料/完全复刻"
MAIN = os.path.join(BASE, "账", "战斗逻辑驱动.tsv")
OUT = os.path.join(BASE, "账", "_parts", "B92_B1c.tsv")


def read_tsv(p):
    t = io.open(p, "rb").read().decode("utf-8-sig").replace("\r\n", "\n")
    return [l.split("\t") for l in t.split("\n") if l.strip()]


def write_tsv(p, rows):
    body = "\r\n".join("\t".join(r) for r in rows) + "\r\n"
    d = os.path.dirname(p)
    if d and not os.path.isdir(d):
        os.makedirs(d)
    io.open(p, "wb").write(b"\xef\xbb\xbf" + body.encode("utf-8"))


main = read_tsv(MAIN)
hdr, rows = main[0], main[1:]
assert len(hdr) == 11

P = {}

P["ResolveAction(BattleAction battleAction)"] = (
    "B",
    "RuleEngine/Core/ActionPump.cs:1436（PumpActionQueues）· RuleEngine/Core/ActionQueue.cs:292（TryDequeue）· 🆕 本批回填的 8 处 `RuleCore.cs` 的 `EnqueueAbility(…, actionType: N)`",
    "原版 = 91 型 actionType 巨型 switch（`.c` 9,300 行，switch 内 **76 个 case**；`attack(1)`/`doDamage(8)` 等 15 型不在本 switch）。**映射表已产出**（`判据/B92_战斗判据_B1.md` 附录 A）。"
    "🆕 **`B92_B1b` + `B92_B1c` 已回填 `RuleCore.cs` 侧 8 / 15 处**（每处都带 `.c` 行号判据）："
    "`虫群合并 → 79`（`AddExecuteSwarm.c` 的 ctor `0x4f`）· `伏击续攻 → 1`、`落地续攻 → 1`（`AddAttackContinuationAction.c:451` 的 ctor 类型 = 1 = 再发一记 attack）· "
    "`resetDuty → 87`（`AddResetDuty.c:34` 的 ctor `0x57`）· `回合末到期:限时增益 → 61`（`ResolveAction.c:5034-5062` 的 `case 0x3d` = clearEndOfTurnEffects）· "
    "`逐条 removeEffect → 16`（`CardScript__SendRemoveEffect.c:27` 的 `local_208[0] = 0x10`）· `minionDeath → 4`（`CardScript__TriggerUnitDeathActions.c:41` 的 `local_208[0] = 4`）· "
    "`recall-to-deck → 51`（`AddRecallToDeck.c:38` 的 ctor `0x33`；调用点 `UsedActiveAbility.c:65` 传道号 3 = DelayedInstant）。"
    "**剩 7 处仍 0**（原版本来就没有单一常量，⛔ 不猜）：`·attacked` / `death-signal` / `Unstable` / `slay-credit` / `死亡结转` / `FireTriggerAt 通用入队` / `mob-extra`。",
    "【B92_B1c 2026-10-28】剩 7 处为什么不能给常量：① 前五个都是「跑某张卡的 ability 正文」，原版走 `PlayAbility` → **该能力自己的 action 类型**（每张卡不同）；"
    "② `Unstable` 在**全量反编译里 0 命中**该字符串 ⇒ 它是卡数据驱动的能力正文，不是硬编码动作；"
    "③ `slay-credit` 原版**内联在 `ResolveDeadCard`**（没有独立动作）；"
    "④ `mob-extra` 是我们的 `ExtraTriggers` 派生动作，**不是**原版 `triggerMobAdditional(81)`（调度台 2026-10-28 裁定：⛔ 不为了数字对齐硬填 81）。"
    "另：`EffectResolver` 侧调用点（B4）与形参本体（B5，`int actionType = 0` 已加）不在本批。"
    "⚠️ 行为零变化：`ActionType` **不进 `Fingerprint`/`StateHash`**，唯二读它的两条路（`ApplyCancelSideEffects` 的 1/0x17、`CancelEffectsFromDisappearingUnitFromQueue` 的三档豁免）今天**没有调用点**。")

P["CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CanPlayCard（:3260）· 🆕 RuleEngine/Core/RuleCore.cs.WaitingForBlockingAction（类尾，本批新建）",
    "七段闸（`.c:35-60/:226-255/:270-380`）。**③ 侧闸已覆盖**（订正：`+0x244 = isPlayerTurn` / `+0x246 = !isPlayerTurn` 每回合被 `_NextTurn:382-384` 写，合起来 = 「是不是这一侧回合」= `p != ctx.Active`）。"
    "**仍缺 3 条**：① `initiateAction && actionQueue.Count > 0` ⇒ PleaseWait；② 教程闸；④ `WaitingForBlockingAction`。"
    "🆕 **`B92_B1c` 已把 ④ 的查询口建好** = `RuleCore.WaitingForBlockingAction(ctx)`（`MatchType.Replay ⇒ false`；否则 `ActionQueue.Has(a => a.ActionType == 27)`，判据 `WaitingForBlockingAction.c:28/:42/:60/:84`）。",
    "【B92_B1c 2026-10-28】① 与 ④ **接线都只差一个返回码档**：`RuleCodes` 现有 30 个码逐个核过，没有「还有阻塞动作在等」这一档（`ErrNotTurn` / `ErrNotPlayable` 都不是它的意思），而 `RuleCodes.cs` **不在本批白名单** ⇒ 接线 = 「`RuleCodes` 加一档 + `CanPlayCard` 两行」。⛔ 不许拿现有码顶（那是发明口径）。① 另需 `bool initiateAction = false` 可选形参（原版 AI 侧传 `false`，见 `out_cpp2il_dev_ilspy/BattleManager.cs:43298`；加了也只有显式传 `true` 的调用点受影响）。② 上游 = 教程闸口。")

out, missing = [], []
for r in rows:
    if r[2] in P:
        st, land, diff, note = P[r[2]]
        out.append([r[0], r[1], r[2], r[3], r[4], st, land, diff, r[8], r[9], note])
for k in P:
    if not any(r[2] == k for r in rows):
        missing.append(k)
assert not missing, missing
assert len(out) == len(P), (len(out), len(P))
write_tsv(OUT, [hdr] + out)
print("wrote", OUT, len(out) + 1, "lines")
