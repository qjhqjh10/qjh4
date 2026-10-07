# W-弃牌堆先后 · 写手报告（2026-10-17）

## ① 结论（改了什么、改成什么）

1. 🔴 **`RuleCore.CleanupDeaths`：「进弃牌堆」+「阵亡登记」两笔账下移到 `FireTriggerAt(Backlash)` 之后。**
   - 改前（一句流水）：`BoardSlots.RemoveAt` → `ps.Discard.Add(u.Instance)` → `SwarmUnder` 一起进 →
     `ctx.DeadUnits.Add` → `Emit(Death)` / `BroadcastWhen(Die)` / `FlushDeathWatches` → `UnstableBlast` → **反噬**。
   - 改后：`BoardSlots.RemoveAt`（**只有它留在原地**）→ `DiedThisTurn++` → 死亡广播 → 不稳定 → **反噬** →
     `ps.Discard.Add` + `SwarmUnder` + `ctx.DeadUnits.Add` + 那句日志 → `Auras.Recompose(ctx)`。
   - 实现：加了一个局部标记 **`bool leftPlay`**（只在「**真的离场**」那一支置位；「翻面成残骸」那一支
     **不记账**、行为逐字不变）。残骸另两处 `Add`（残骸被摧毁 / 变成残骸）**没动**。
2. **下移之后补了一道「原版同构」的复核**（`bool refiled`）：那一份实例**已经回到手牌**、或**已经被登记过**
   ⇒ **不再登记**。判据 = 原版进墓地那一跳之前的复核（见下表 ⑥）。没有它会出现「同一张卡既在手又在弃牌堆」。
3. **⚠️ 配套改了 `EffectResolver.DoReturn` 一处**（唯一一处行为改动，理由见 §四-1）：
   `TakeFromGraveyard(owner, ActingUnit.Instance) ?? NewInstance(dead)`
   → `… ?? ctx.ActingUnit.Instance ?? NewInstance(dead)`；同支的日志文案一并改真（旧文案说「从弃牌堆捞的」，
   下移之后不成立）。⛔ **没动** `EffectResolver.cs:1029` 的 `ps.Discard.Add(inst)` —— `grep` 判过：它在
   `PlayTactic` 里，是**战术卡自己**结算完进弃牌堆，**不是**单位死亡这条链。
4. **② 注释订正**：`Core/RuleCore.cs` **16 处** + `Core/EffectResolver.cs` **16 处**（含两个文件头）
   把「**原版** `rule_core.gd:NNNN`」订正成「我们自己的 `rule_core.gd:NNNN`（旁证、非权威）」。
   **只改出处措辞，⛔ 没动任何判据值 / 数值 / 结论**。文件头保留了 2026-10-17 的更正痕迹 + 全文件读法。
5. **③ 断言**：新增 `RuleEngineTest.TestDeathAccountsAfterBacklash()`（紧跟 `TestBacklash`，已注册进 runner）。

## ② 原版逐跳证据（全在 `d:/2/tools/decomp_full/`，均为反编译方法体）

| # | 方法体 `文件:行号` | 一句话 |
|---|---|---|
| ① | `CardScript__CheckIfDead.c:124` | 生命归零 → `state(0x228) = 5`（濒死）。 |
| ② | `CardScript__CheckIfDead.c:135`（「变残骸」支是 `:141`） | **当场 `TriggerUnitBacklashActions`** —— 反噬在这一跳。 |
| ③ | `CardScript__TriggerUnitBacklashActions.c:99` | 反噬被包成 action `BattleManager.AddAutoActionToQueue` **入队** ⇒ 自己一次都不碰墓地。 |
| ④ | `CardScript__ResolveDeadCard.c`（全篇） | 死亡触发那一族（`TriggerOnMinionDeath` / `RemoveExtrinsicEffectsFrom` / `BroadcastUnitRequiem` / `OnTrigger(0x1ae)`）里**一次 `Cemetery` / `GoToCemetery` 都没有**。 |
| ⑤ | `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:55,59,119` | **从棋盘移除**（`MinionManager.RemoveMinion` + `List.Remove`）在**另一条协程**里；同处 `StartCoroutine(CardScript.UnitDeath(...))`。 |
| ⑥ | `CardScript._UnitDeath_d__446__MoveNext.c:63` | 进墓地之前**复核**：`state(0x228) != 5` ⇒ `LogWarning + return 0`，**压根不调 `GoToCemetery`**（中途被捞走的**不进墓地**）。 |
| ⑦ | `…MoveNext.c:80,126,383,152` | 演出三拍：`SetCardStateInCemetery` → `WaitForSeconds`(:126) → 死亡粒子 → `WaitForSeconds`(:383) → **state 2 才 `GoToCemetery`（:152）**。 |
| ⑧ | `CardScript__GoToCemetery.c:8,17` | `state = 6`（= 在墓地里）；`:17` 调 `BattleManager.AddToCemetery`。 |
| ⑨ | `BattleManager__AddToCemetery.c:6` | 全库 grep `AddToCemetery` **只有 2 个文件命中**（另一个是它自己）⇒ **唯一调用者就是 ⑧**；体里转 `CemeteryManager.AddCardToCemetery(bm+0x110, card, 0)`。 |
| ⑩ | `CemeteryManager__AddCardToCemetery.c:17-47` | 真身：`List.Add`（`manager+0x60`）+ 换父 + `SetAsCemetery(idx)` + `SetActive(false)` —— **这才是「进弃牌堆」**。 |

⇒ **进墓地 = `UnitDeath` 协程的最后一跳**（两次 `WaitForSeconds` 之后），**反噬在链头**。
「反噬结算时它还没进弃牌堆 / 还没进阵亡表」成立得不能再成立。

## ③ 改动清单

| 文件 | 改了什么 |
|---|---|
| `Core/RuleCore.cs` | `CleanupDeaths`：两笔账下移 + `leftPlay` 标记 + `refiled` 复核 + 逐跳判据注释；16 处出处措辞订正。 |
| `Core/EffectResolver.cs` | `DoReturn` 的「施放者已离场」支：实例沿用（见 §四-1）+ 日志文案改真；16 处出处措辞订正（含文件头「权威语义来源」自证）。 |
| `Editor/RuleEngineTest.cs` | 新增 `TestDeathAccountsAfterBacklash()`（106 行）+ runner 注册一行。 |

**断言逐条盯什么**（`TestDeathAccountsAfterBacklash`，三块，都是**行为驱动**、不是同义反复）：
- 🚧 **护栏**：`probe.TriggerOps(Backlash) != null` —— 正文没解析出来的话下面两条会**假绿**，先钉住。
- **①「看得到还没登记」**：探针的反噬正文 = `If a friendly troop died this turn, Damage 3 EnemyWarlord`
  —— 它读的正是 `ctx.DeadUnits`（`EffectResolver.ConditionHolds` 的 `case "deaths"`）。
  ⇒ 探针**自己的**反噬**打不出来**（P1 督军血量不变）。⚠️ 两笔账挪回去 ⇒ 这条立刻变红。
- **②「跑完之后进去了」**：`DeadUnits` 里查得到它 + `Discard[0]` 是它；再加一条**跨单位**证据 ——
  同回合打死**第二只**探针时，反噬**打得出来**（3 点），因为第一只已经在阵亡表里。
- **③ 判别式（弃牌堆的次序）**：`outer` → 反噬打死 `Killer` → `Killer` 反噬打死 `inner` ⇒
  P2 弃牌堆 = **`[inner, outer]`**（内层先跑完、先入档）；`DeadUnits` 的次序同样内层在前。
  ⚠️ 两笔账挪回反噬之前 ⇒ 变成 `[outer, inner]`，这条与 ① 一起变红。**这就是「灭自证」那条。**

## ④ 没查清 / 需要调度台裁的部分

1. 🔴 **`EffectResolver.DoReturn` 那一处是本批唯一的「额外」行为改动** —— 但它**是 ① 的必然结果**，
   不是我自选的口径：那一支的注释原来就写着「清账排在反噬**之前**，所以只能从弃牌堆捞」；
   下移之后 `TakeFromGraveyard` 必然落空 ⇒ 原样保留会 `NewInstance` **另发一份**，而旧那一份稍后仍会进
   弃牌堆与阵亡表 ⇒ **同一张卡既在手又在墓地**（静默的重复，现有断言看不见：Makari 那条只按 `CardDef` 认）。
   ⇒ 落空时沿用 `ctx.ActingUnit.Instance`（守住用户 2026-09-18「回手沿用同一份实例」的口径）；
   `CleanupDeaths` 那侧用 `ps.Hand.Contains(u.Instance)` 拦住重复登记。**请复核这一处。**
2. ⚠️ **`ctx.DiedThisTurn++` 我【没有】跟着下移**（刻意的，代码里写了注）：它是 `For each one that dies this turn`
   的**回合计数**、不是那两张登记表，任务也只点名了三样。⇒ 反噬期间 `DiedThisTurn` **算它**、`DeadUnits` **不算它**，
   这个不对称**如实标着**；若调度台判定该一起下移，一行的事。
3. ⚠️ **「反噬结算时单位在不在棋盘上」—— 没查清**：原版 `TriggerUnitBacklashActions` 在 `CheckIfDead`
   （**早于**从棋盘移除）就入队，但**入队 ≠ 结算**；移除在另一条协程 `_ResolveMinionDeath` 里。
   静态读不出「结算那一刻棋盘上有没有它」。⇒ **我们保持现状（先移除、再反噬）**，如实标「没查清」。
   （`BattleManager.ResolveBacklash` / `ResolveMinionDeath` 在 dump 里**都查不到调用点** —— 只有它们自己和
   自己的协程文件命中 grep，**这一格也没查清**。）
4. ⚠️ **嵌套死亡的次序**：我们引擎是同步的 ⇒ **确定性**（内层先跑完 ⇒ 先入档），
   上面 ③ 就把这个次序钉死了。原版是**协程交错**、静态读不出唯一次序；能确证的只有结构不变量：
   **任何一瞬间，「已进墓地」⊆「已触发过反噬」**（墓地只在各自链尾才写）。⇒ 我们「内层先进弃牌堆」与它**相容**。
5. ⚠️ **`Auras.Recompose` 不需要额外补一次**：下移的这几行**不写 `Board[..]`**，而 `CleanupDeaths` 末尾那次
   `Auras.Recompose(ctx)` 仍然排在它们**之后** ⇒ 覆盖不变（`UnstableBlast` / 反噬自己改棋盘的那些路径各自有钩子）。
6. ⚠️ **`SwarmUnder` 在被捞走的那一支不清空**（虫群宿主被 `Backlash: Returns to your hand` 捞回手牌时，压在下面的牌留在 `u.SwarmUnder` 里）。
   原版这一格**没有对应物**（原版没有「压在下面」这个列表）⇒ 如实留痕、未改；`Makari` 是 1 血小兵，实际碰不到。

## ⑤ 类型检查结果（原样贴）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

命令行：`TMPDIR=/tmp/wf_b3 bash d:/4/Unity/工具/typecheck.sh`（改完每个 `.cs` 后跑过三次，末次即上面这份）。⛔ **没跑 Unity**（按简报）。
行尾：`RuleCore.cs` 纯 LF（0 CRLF）· `EffectResolver.cs` / `RuleEngineTest.cs` 纯 CRLF，改完逐次核对过；
`git diff --numstat` = `RuleCore.cs 89/31 · EffectResolver.cs 35/16 · RuleEngineTest.cs 106/0`。

**收口时请跑**：`RuleEngineTest.Run`（引擎侧改动，宿主是它自己）。
⚠️ 与新断言强相关的既有用例：`TestBacklash`（"Death 排在 Backlash 前面" 仍成立）· Makari 那条
`Backlash: Returns to your hand and costs 2 more this turn`（§四-1 的复核靶）。
