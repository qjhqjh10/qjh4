# -*- coding: utf-8 -*-
"""B92_B1d 账补丁生成器（第四轮施工：CanPlayCard ①④ 接线 + 新 RuleCodes 档 + 断言；actionType 7 处定性）。
只读主表 `账/战斗逻辑驱动.tsv` 取 11 列骨架 → 写 `账/_parts/B92_B1d.tsv`。⛔ 不写主表。"""
import io, os

BASE = r"d:/4/Unity/资料/完全复刻"
MAIN = os.path.join(BASE, "账", "战斗逻辑驱动.tsv")
OUT = os.path.join(BASE, "账", "_parts", "B92_B1d.tsv")


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

P["CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CanPlayCard（:3260，①④ 已接线）· RuleEngine/Core/RuleCore.cs.WaitingForBlockingAction（类尾）· RuleEngine/Core/RuleCodes.cs（新开 `ErrWaitingBlockingAction = 30`）· Editor/RuleEngineTest_Priority.cs（断言 `W61T33CanPlayCardWaitingBlocking`）",
    "七段闸（`.c:35-60/:226-255/:270-380`）。**③ 侧闸已覆盖**（订正：`+0x244 = isPlayerTurn` / `+0x246 = !isPlayerTurn` 每回合被 `_NextTurn:382-384` 写，合起来 = 「是不是这一侧回合」= `p != ctx.Active`）。"
    "🆕 **`B92_B1d` 已接线 ① 与 ④**：① `initiateAction && ActionQueue.Count > 0 ⇒ ErrWaitingBlockingAction`（`.c:35-60`，提示 `Battle/Tips/PleaseWait` 属表现层）；"
    "④ `WaitingForBlockingAction(ctx) ⇒ ErrWaitingBlockingAction`（`.c:255`；`matchType == 0xa0(160) ⇒ false`，否则四条道找 `actionType == 0x1b(27)`，判据 `WaitingForBlockingAction.c:28/:42/:60/:84`）。"
    "签名补 `bool showTips = false, bool initiateAction = false, bool actionAlreadyStarted = false`（可选 ⇒ 既有调用点零改动）。",
    "【B92_B1d 2026-10-28】配了断言 `RuleEngineTest_Priority.W61T33CanPlayCardWaitingBlocking`（6 条，每条带 🧨：阻塞在时不可出牌 / 不在时可出牌 / 查询式调用不受 ① 影响 / `transformUnit(27)` 连查询也挡 / Replay 不挡）。"
    "⚠️ **行为零变化**：① 只在显式传 `initiateAction: true` 时生效（现有调用点都不传；原版 AI/查询侧传 `false`，见 `out_cpp2il_dev_ilspy/BattleManager.cs:43298`）；④ 今天恒 false（全仓无入队点写 `ActionType == 27`，`transformUnit` 机制 = 账 G02 未做）。"
    "⚠️ **未做**：`showTips` 的提示气泡（表现层）；`actionAlreadyStarted` 只为签名对齐、体内没读；② 教程闸（上游 = 教程闸口 `CheckIfPlayerActionPermittedInTutorial`，我们没有）。"
    "⚠️ **断言已编译未执行**（零 Unity 批处理窗口由调度台跑 `RuleEngineTest.Run` 首验）。")

P["ResolveAction(BattleAction battleAction)"] = (
    "B",
    "RuleEngine/Core/ActionPump.cs:1436（PumpActionQueues）· RuleEngine/Core/ActionQueue.cs:292（TryDequeue）· RuleCore.cs 侧 8 处 `EnqueueAbility(…, actionType: N)`",
    "原版 = 91 型 actionType 巨型 switch（`.c` 9,300 行，switch 内 **76 个 case**；`attack(1)`/`doDamage(8)` 等 15 型不在本 switch）。**映射表已产出**（`判据/B92_战斗判据_B1.md` 附录 A）。"
    "🆕 **`RuleCore.cs` 侧已回填 8 / 15 处**（每处带 `.c` 行号判据）：虫群合并 **79**（`AddExecuteSwarm.c` ctor `0x4f`）· 伏击续攻 **1** / 落地续攻 **1**（`AddAttackContinuationAction.c:451` ctor 类型 1）· resetDuty **87**（`AddResetDuty.c:34` ctor `0x57`）· 回合末到期:限时增益 **61**（`ResolveAction.c:5034-5062` 的 `case 0x3d`）· 逐条 removeEffect **16**（`CardScript__SendRemoveEffect.c:27` 的 `local_208[0] = 0x10`）· minionDeath **4**（`CardScript__TriggerUnitDeathActions.c:41` 的 `local_208[0] = 4`）· recall-to-deck **51**（`AddRecallToDeck.c:38` ctor `0x33`）。"
    "**剩 7 处保持 0**（`·attacked` / `death-signal` / `Unstable` / `slay-credit` / `死亡结转` / `FireTriggerAt 通用入队` / `mob-extra`）。",
    "【B92_B1d 2026-10-28】剩 7 处 = **能力自带 actionType 的结构缺口，非缺常量**（调度台 2026-10-28 裁定：保持 0、⛔ 不许编枚举语义）。理由：前六处都是「跑某张卡的 ability 正文」，原版 `PlayAbility` 之后 `actionType` 由**那条能力自己**运行时决定（每卡不同）；`mob-extra` 是我们的 `ExtraTriggers` 派生动作、**不是**原版 `triggerMobAdditional(81)`。按 18·0 该补的是机制（能力自带类型），不是常量。另：`EffectResolver` 侧调用点属 B4。"
    "⚠️ 行为零变化：`ActionType` 不进 `Fingerprint`/`StateHash`；唯二读它的两条路（`ApplyCancelSideEffects` 的 1/0x17、`CancelEffectsFromDisappearingUnitFromQueue` 的三档豁免）今天无调用点。")

P["AllowResolveAttack(BattleAction battleAction)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CanAttackNow（:5693）· :5267-5510（IsValidTarget）",
    "七段闸（`.c:92/:134/:176/:226/:236-248/:333/:440/:561`）。**两个短路 + `hasAvailableActions` 都已覆盖**（后者 = `+0x48==0 || (HasTrait(0xdc) && +0x48==1)`，`0xdc` = 220 = bloodThirst ⇒ 与 `ActionQuota` + `AttacksThisTurn` 逐值同构）。**仍缺 4 条**：① `cardState(+0x228)==2 || IsAttacking()` ② 教程闸 ③ ⑦（目标 `isPlayer` 时 `!IsAttacking() && matchType != 0xA0(160)`）④ `errorMessage(+0x528)` 回写（我们用 `RuleCodes`）。",
    "【B92_B1d 2026-10-28】四条**逐条写明「还差 `cardState` 的哪一格」**（调度台要求，全文见 `判据/B92_战斗判据_B1.md` **附录 C-④** 那张表）：① 要 `cardState ∈ {2 inPlay, 3 inPlayAttacking, 0x11 inPlayAminingAttack}`；③ 要的是**目标那张**的 `{3, 0x11}`（`IsAttacking()` 的体就是这两个值）⇒ **⛔ 绝不拿 `IsAttackingOnBoard`（在场且活着）顶，那是错映射**（调度台 2026-10-28 已认可）；② 不需要 `cardState`、缺的是教程闸口本身；④ 不需要 `cardState`、缺的是「错误消息字符串」通道（要动 `BattleContext`，B7）。⇒ **2 条等 B6 卡态机、1 条等教程闸口、1 条等错误消息通道**。")

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
