# -*- coding: utf-8 -*-
"""B92_B1b 账补丁生成器（转岗施工批）。
只读主表 `账/战斗逻辑驱动.tsv`（取 11 列骨架）→ 写 `账/_parts/B92_B1b.tsv`。
⛔ 不写主表、不改任何 .cs。可复跑、幂等。"""
import io, os

BASE = r"d:/4/Unity/资料/完全复刻"
MAIN = os.path.join(BASE, "账", "战斗逻辑驱动.tsv")
OUT = os.path.join(BASE, "账", "_parts", "B92_B1b.tsv")


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
assert len(hdr) == 11, hdr

# key = 主表那一行自己的 `方法` 字符串（逐字）
P = {}

P["UpdateManaLeftAccumulation(bool isPlayer)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.EndTurnCore（那一句 `p.SetSavedMana(...)`）",
    "✅ 2026-10-28 `B92_B1b` 已按原版补齐两处：① 写 `min(manaAccumulation(+0x40), GetUnusedMana())`（`GetUnusedMana` 等价物 = `p.Energy`）② 加 `ctx.Turn > 1` 闸（`ctx.Turn` ≡ `turnCounter(+0x3F8)`，见 `RuleCore.cs:1212` 那行注释；原版调用点 `BattleManager._NextTurn_d__395__MoveNext.c:288-292` 只在 `> 1` 时调）。改后 `EndTurnCore` 那一行 = `p.SetSavedMana((ctx.Turn > 1 && ctx.Vars.manaAccumulation > 0 && p.Energy > 0) ? (p.Energy < ctx.Vars.manaAccumulation ? p.Energy : ctx.Vars.manaAccumulation) : 0);` **行数不变**（下游账落点零漂移）。",
    "【B92_B1b 2026-10-28】行为影响面：只有 `GameplayVariables.manaAccumulation > 0` 的档（遭遇 Skirmish，值 1）**第 1 回合结束不再结转 1 点**；经典档 `= 0` ⇒ 逐字节不变 ⇒ `play check` / `play all dump`（PlayProbe 用 Classic）**不变**，`battle` 探针（Skirmish）会变。")

P["CheckUnitsCancellingAttack(CardScript actingCard, CardScript targetCard)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CheckUnitsCancellingAttack（🆕 本批新建，类尾）+ 私有 RuleCore.HasCancelAttackAbility",
    "🆕 2026-10-28 `B92_B1b` 已建**查询口**（原版 `.c:36-72` 的三条：逐张问 `HasCancelAttackAbility` / 排除 `trait 0x82(130 = jam)` / 去重；扫双方棋盘 + `IsUnitInPlayOrDying` 闸）。**0 调用点 by design、不打一行日志**（`ctx.Log` 会进 `Events` ⇒ 动 `Fingerprint`）。原有 = `TryCancelAttackByOwnAboutToAttack`（攻方自己那条 `CheckIfAbilityCancelsAttack` 通道）+ `CardDef.Bodyguard` + `TryRedirectAttackToBodyguard`（取槽号最小直改目标）。",
    "【B92_B1b 2026-10-28】接线四件仍缺（全在别的文件或待裁）：① `CancelAttack`/双方 `ClearPendingDamage`（后者我们无待结算缓冲）② `RegisterPlayerActionCancelled`（联机/回放账层）③ `redirectedAttack(74)` 出队体不存在（要 `ActionType` 回填 + 「改目标重发」结算口）④ **口径待裁**：替身已走 Bodyguard 直改目标，若再接「取消+跑正文」会双触发。另口径差：原版 `unitsInPlay` 是双方共用追加序表，我们 P0/P1 按格号。")

P["BroadcastActivateMinions(bool playerTurn)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.BroadcastActivateMinions（:16403）· :3518（RecomputeActAndAttack）",
    "原版按实参选侧（`false` ⇒ 敌 MM + 敌督军、**不点手牌**；`true` ⇒ 先 `LightPlayableHand` + 我 MM + 我督军，`.c:9/:12/:18/:20/:23`）。🔴 **我方第一版判据的两条口径错已订正**：① 「实参座位被忽略」**不成立** —— `_NextTurn_d__395__MoveNext.c:382-384` 是 `+0x244 = isPlayerTurn` / `+0x246 = !isPlayerTurn` / 传 `isPlayerTurn`，实参就是「是不是这一侧的回合」⇒ 恒取 `ctx.Active` **是对的**；② `+0xdc` 已坐实 = **220 = bloodThirst**（`DefinedTrait.cs:21`）⇒ `ActionQuota`（`bloodThirst ? 2 : 1`）与 `ActivateMinion.c:19-22` 的 `+0x48` 闸**逐值同构**。",
    "【B92_B1b 2026-10-28】只剩一处刻意收窄：原版「已行动」时 `canAct`/`canAttack` **两位同写 false**，我们只压 `Exhausted`（已标在 `RecomputeActAndAttack` 的 doc）。")

P["AllowResolveAttack(BattleAction battleAction)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CanAttackNow（:5693）· :5267-5510（IsValidTarget）",
    "七段闸（`.c:92/:134/:176/:226/:236-248/:333/:440/:561`）。🔴 **订正**：`get_hasAvailableActions()` **不是缺口** —— 它的体（`CardScript__get_hasAvailableActions.c`）= `+0x48 == 0 || (HasCurrentTrait(0xdc) && +0x48 == 1)`，`0xdc` = 220 = bloodThirst ⇒ 与我们 `AttacksThisTurn >= ActionQuota ⇒ ErrExhausted` **逐值同构**。**仍缺 4 条**：`cardState(+0x228)==2 || IsAttacking()` · 教程闸 `CheckIfPlayerActionPermittedInTutorial` · ⑦（目标 `isPlayer` 时要求 `!IsAttacking() && matchType != 0xA0(160 Replay)`）· `errorMessage(+0x528)` 回写（我们用 `RuleCodes`）。",
    "【B92_B1b 2026-10-28】三条依赖 `cardState`（附录 C·①）或教程闸口（我们没有）；⑦ 里的 `IsAttacking()` 原版指 `cardState ∈ {3,0x11}`，我们的 `IsAttackingOnBoard` 是「在场且活着」⇒ **不是同一个量，⛔ 不许拿它顶**。")

P["CanPlayCard(CardScript card, bool showTips, bool initiateAction, bool actionAlreadyStarted)"] = (
    "B",
    "RuleEngine/Core/RuleCore.cs.CanPlayCard（:3260）",
    "七段闸（`.c:35-60/:226-255/:270-380`）。🔴 **订正**：「两个侧闸字段」**不是缺口** —— `_NextTurn_d__395__MoveNext.c:382-384` 每回合写 `+0x244 = isPlayerTurn` / `+0x246 = !isPlayerTurn` / `+0x418 = 0`，而 ③ 就是读这两格 ⇒ 合起来等价于「是不是这一侧的回合」= 我们 `p != ctx.Active`；`+0x245`（`resolvingTurnEnd`）在同步模型里无对象。**仍缺 3 条**：① 阻塞队列 `PleaseWait`（`initiateAction && ActionQueue.Count > 0`，**不需要** `BlockPlayerActions`）② 教程闸 ④ `WaitingForBlockingAction` 的调用（要 `actionType == 27` ⇒ 依赖 ActionType 回填）。",
    "【B92_B1b 2026-10-28】① 可顺手做（加 `bool initiateAction = false` 可选形参 + 一行闸，只有显式传 true 的调用点受影响；原版 AI 侧传 false，见 `out_cpp2il_dev_ilspy/BattleManager.cs:43298`）；④ 上游 = `ActionType` 回填。")

P["ResolveAction(BattleAction battleAction)"] = (
    "B",
    "RuleEngine/Core/ActionPump.cs:1436（PumpActionQueues）· RuleEngine/Core/ActionQueue.cs:292（TryDequeue）",
    "原版 = 91 型 actionType 巨型 switch（`.c` 9,300 行，**switch 里 76 个 case**；`attack(1)`/`doDamage(8)`/`doMultiDamage(9)` 等 15 型不在本 switch）。**映射表已产出**（`判据/B92_战斗判据_B1.md` 附录 A：型/枚举名/case 行/主调用 76 行）。🆕 `B92_B1b` **已回填 `RuleCore.cs` 侧 4 处** `EnqueueAbility(…, actionType: N)`：虫群合并 = **79 (executeSwarm)**（判据 `AddExecuteSwarm.c` 的 ctor 0x4f）· 伏击续攻 = **1 (attack)** · 落地续攻 = **1 (attack)**（判据 `AddAttackContinuationAction.c:451` 的 ctor 类型 = 1）· resetDuty = **87**（判据 `AddResetDuty.c:34` 的 ctor 0x57）。",
    "【B92_B1b 2026-10-28】**还差**：`RuleCore.cs` 内另 **11 处** `EnqueueAbility(` 未回填（判据不足，仍 0：通用「跑某张卡的 ability 正文」那几口在原版本来就没有单一常量；两处 `removeEffect` 与 `minionDeath`/`slay`/`Unstable` 需要各自的 `Add*` ctor 判据）＋ `EffectResolver` 侧调用点（B4）＋ 形参本体（B5，已加 `int actionType = 0`）。")

out = []
missing = []
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
