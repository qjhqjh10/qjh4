# A · 战斗逻辑驱动 —— B 升 A 第 1 轮

> 白名单本轮新增 `CardPresentation/Editor/BattleScene.cs`（落表现半断言）。
> 收尾 `typecheck`（`TMPDIR=/tmp/wf_a`）：**运行时 0 / 编辑器 0**。

---

## ① 结论（数字）

| 项 | 第 2 轮末 | 本轮末 | Δ |
|---|---:|---:|---:|
| `A 已对齐` | 131 | **141** | **+10** |
| `B 已实现未核` | 1,193 | **1,184** | −9 |
| `C 该做没做` | 65 | **64** | −1（`IsThereDefender` 那条补完升 A） |
| `D 不适用` | 146 | 146 | 0 |
| `E 未读` | 0 | 0 | 0 |
| 账总行数 | 1,535 | 1,535 | 0（列结构未动、未删行） |

**本轮升 A 的 10 条，每一条都读了原版方法体并逐条对了我们的实现**（名单见 §③）。
**同时查出并当场修掉 4 处真差异**（其中 2 条主链可见 ⇒ 见 §②）。

> ⚠️ **我没有为了数字好看批量改 A。** 剩下的 **1,184 条 B** 里有 **约 640 条是表现层/HUD**
> （`归B` 口径、B 域已核过一轮，我没有那个域的判据去逐条核 ⇒ 保持 B），
> **约 540 条是引擎逻辑**，每条都要「读原版方法体 + 对我们的实现」两趟 —— 那是多轮的量。
> 本轮按调度台给的顺序**从主链（BattleManager/CardScript）挑判据最硬、风险最高的先做**。

---

## ② 本轮查出的 4 处真差异（全部已修）

### ②·1 【P1 · 主链可见】**`ephemeral` 的牌死掉**不该进墓地

- **判据**：`CardScript.GoToCemetery`（`CardScript.cs:16401-16415`，方法体现读）
  ```csharp
  cardState = inCemetery;
  if (IsUnit()) ResetToValuesInHand();
  if (!HasCurrentTrait(ephemeral)) { BattleManager.AddToCemetery(this); return; }
  gameObject.SetActive(false);          // ← 带 ephemeral：只把卡面关掉
  ```
- **我们原来**：死亡收尾**无条件** `ps.Discard.Add(u.Instance)` + 进 `ctx.DeadUnits`。
- **可达性**：现读卡池 **105 张**带 `ephemeral` ⇒ 墓地张数、`Choose a unit that died …` 的候选、重燃目标全受影响。
- **修法**：`RuleCore.MinionDeathBookkeepingDeferred` / 残骸被摧毁那一处，`ephemeral` ⇒ **两处都不进**、只出声。
  ⚠️ 虫群下面压着的（`SwarmUnder`）**照旧进墓地**（它们没死、是被并进去的）。

### ②·2 【P1】手牌效果的到期判据：缺 `endOfTurn` 门控 + 缺两条「换主」支线 + 缺「单位离场时的清扫」

- **判据**：`PlayerHand.UpdateCardEffects(bool endOfTurn)`（`PlayerHand.cs` 方法体现读）：
  - `if (endOfTurn) { untilEndOfTurn（无条件摘）; if (IsPlayerTurn() != originalIsPlayer) { untilPlayerTurnStart / untilFollowingPlayerTurnStart }; }`
  - **无论 `endOfTurn`** 都判四条 `extrinsic` 支线：① 施放者 `!IsInPlay` ② 施放者带 `jam`
    ③ `playersAffected == Self ∧ 施放者不属这手牌那方` ④ `playersAffected == Enemy ∧ 施放者属这手牌那方`。
  - **调用点**：`DestroyUnitSilent` 末尾对**双方手牌**各调一次 `UpdateCardEffects(false)`。
- **我们原来**：只有一个「回合末」调用点 ⇒ ①② 有、③④ **没有**；因为没有 `endOfTurn` 参数，
  「回合末」那两档被当成恒真（只因唯一调用点正好是回合末才没露馅）。
- **修法**：`ExpireHandBuffs(ctx, endOfTurn)` 补门控；`HandEffectExpired` 补两条换主支线；
  新增 `SweepHandEffectsOnRemoval`（在**三个真正离场点**各调一次：残骸被摧毁 / 翻面成残骸 / 真死离场）。

### ②·3 【P1】`CardEffect.IsSameEffect` 的判重口径：我们原来只比记录号

- **判据**：`CardEffect.IsSameEffect`（`CardEffect.cs:200-240`，方法体现读）比的是**一整串字段**
  （buffType · enchantingCard(+heroCheck) · enchantmentCardId · extrinsic(×2) · until* 五档 ·
  近战/远程/血/费/随机的 Change+ValueOptions · targetCriteria）。
- **我们原来**：`AttachHandEffectCopy` 只比 `RecordId` ⇒ **同字段的两条会叠两层**。
- **修法**：新增 `EffectResolver.IsSameHandEffect`（逐字段；⛔ **没退回** `W4` 判死的「来源+载荷字符串」），
  `AttachHandEffectCopy` 改调它，并留了一张「原版字段 → 我们那一格」的对照表。
  ⚠️ 我们**没有** `enchantmentCardId` / `extrinsicVariable` 这两维 ⇒ 恒等（doc 里已写明）。

### ②·4 【P2】`DeckRules` 缺短档文案（收藏域 `C` 的判据）

- **判据**：官方 `MenuDeck/Error/*` 共 **15 条**，**只有 2 条**带 `/Short`
  （`InvalidDeck/Short` · `InvalidDeckBannedCards/Short`，EN/ZH 两列齐全，现读本地化表）。
- **修法**：`DeckRules.DescribeShort(DeckError)` —— 只有那两档拼 `/Short`，其余**一律回长档**
  （⛔ 没按「每条都补 Short」）。
- 🔴 **还欠一条（不在我白名单）**：`MenuDeck/Error/InvalidDeck/Short` 这条**词条要进 `Core/Loc.cs`**
  （`CardPresentation` 的文件）—— 缺条时 `Loc.T` 会把键名印到屏幕上。**改动请求**：给 E 加这一条
  （文本直接取 `官方本地化_en+zh-CN.json` 的 `MenuDeck/Error/InvalidDeck/Short` 那两列）。

---

## ③ 本轮升 A 的 10 条（每条都读了原版方法体）

| 账行 | 方法 | 原版出处 | 我们的落点 | 备注 |
|---|---|---|---|---|
| 1276 | `MinionManager.IsThereDefender` | `MinionManager__IsThereDefender.c:19-…` + `CardScript.IsDefender` | `RuleCore.IsValidTarget.IsDefenderUnit` | 两道过滤器（隐身 / 将死）+ 2 条**灭自证**断言 |
| 1345 | `PlayerHand.UpdateCardEffects` | `PlayerHand.cs` 方法体 | `HandEffectExpired` + `ExpireHandBuffs` + `SweepHandEffectsOnRemoval` | 见 ②·2 |
| 315 | `BattleManager.DestroyUnitSilent` | `BattleManager.cs:23941-…` | `CleanupDeaths` + `SweepHandEffectsOnRemoval` + `Aura.Recompose` | 七步逐条对上 |
| 314 | `BattleManager.DestroyUnit` | `BattleManager.cs:40914-…` | `EffectResolver.DoDestroy` / `CleanupDeaths` | 入队 vs 同步（净结果同） |
| 869 | `CardScript.GoToCemetery` | `CardScript.cs:16401-16415` | `MinionDeathBookkeepingDeferred` | 见 ②·1 |
| 999 | `CardScript.SendToCemetery` | `CardScript.cs:9742-…` | 死亡收尾 | 前两跳是表现层 |
| 772 | `CardScript.AlreadyContainsEffect` | `CardScript.cs` + `CardEffect.cs:200-240` | `IsSameHandEffect` + `AttachHandEffectCopy` | 见 ②·3 |
| 1113 | `CardScript.UpdateWhileInPlay` | `CardScript.cs` / `RawCardScript.cs:1239` | `EmitWhileInPlayPass` + `Aura.Recompose` | 信号/数值两半分开对上 |
| 1443 | `RawCardScript.UpdateWhileInPlay` | `RawCardScript.cs:1239-1250` | 同上 | 只剩两句 `TriggerAbilities(20)/(240)` |
| 1019 | `CardScript.SetHealthToZero` | `CardScript.cs:17399` | `Hurt` / `CleanupDeaths` | 一句赋值 |

---

## ④ 断言

### ④·1 落盘（`RuleEngine/Editor/RuleEngineTest.cs`）
- **`TestA2AttackGateAndPlayTarget`**（第 2 轮已落，9 条）—— 先锋闸两个过滤器（含 2 条灭自证）+ 两段式选目标的引擎半。
- 🆕 **`TestA3EphemeralSkipsCemetery`**（本轮新落，4 条）：
  - 尺子：**不带** `ephemeral` 的单位死掉 ⇒ 进 `Discard` **且** 进 `DeadUnits`；
  - 正题：**带** `ephemeral` ⇒ **两处都不进**（判据 `CardScript.GoToCemetery`）。

### ④·2 落盘（`CardPresentation/Editor/BattleScene.cs`）—— 两段式手势的**表现半**（4 条）
新增一节「两段式选目标（targeted 单位卡）」，用夹具卡 `Rally: Deal 2 damage to an enemy`：
1. **落位后进「等点目标」态**（`AwaitingPlayTarget == true`），且**牌还没离开手牌**；
2. **点中合法目标**（敌方督军）⇒ 退出该态 + 牌真的打出去（手牌 −1）+ **点定的那个掉 2 点**；
3. **点空白** ⇒ 取消、**牌留在手里**（原版 `:903-970` 的终点一致）；
4. **回放局（`_replaySession`）不进两段式**（原版 `matchType != 0xA0`）。

> 为这 4 条在 `BattleDriver` 加了 3 个**自检专用**入口（`SimulateCardDroppedForTest` /
> `SimulatePlayTargetPickForTest` / `SetReplaySessionForTest`）并把 `TryResolvePlayTarget` 的判定本体
> 拆成 `ResolvePlayTargetByUnit`（**产品与自检走同一段代码**，⛔ 不是给测试另开一条路）。
> 全部标「待自检恢复时跑」。

---

## ⑤ 没查清 / 为什么还剩 1,184 条 B

**按调度台口径，核不动的保持 B + 写清差什么。** 分三档：

### ⑤·1 表现层 / HUD（约 640 条，`备注 = 表现层，B域已核`）
`Battle`/`CardScript` 里的 `*Anim` / `Display*` / `Show*` / `Sorting*` / `Pos*` / `Tween*` 那一族。
原版方法体是 `Transform`/`Tween`/TMP 的逐帧操作，**核它要那个域的判据**（B/D 已核过一轮、我没重复）。
**差什么**：要升 A 得逐条读原版 Tween 参数（时长/缓动/起止位）并与我们表现层对照 —— 那是表现域的活。

### ⑤·2 `Add*` / `Resolve*` 动作队列族（约 300 条）
原版是「建 `BattleAction` → `AddAutoActionToQueue(priority)` → 协程逐拍消费」；我们是**同步结算**。
**净结果已对**（同一条 `EffectResolver` / `RuleCore` 落点），但**真的没法逐句「对齐」**（架构不同）。
**差什么**：要 A 得先定义「同步模型下 A 的判据是什么」—— 建议把这一族判 `A` 的**判据**改成
「同名 `Resolve*` 消费的语义逐条对上」（本轮已经这样核了 `DestroyUnit` 一族），其余仍需一条条过。

### ⑤·3 其余引擎逻辑（约 240 条）
`BattleManager` 的回合/装配/收局/联机分叉、`CardScript` 的 trait 增减、`RawCardScript` 的资产访问器。
**差什么**：每条都要读原版方法体（很多是 `yield` 状态机，得回 `.c` 或 VA 读）——
按本轮的经验，**一个主题簇（3~6 条）要 1 次原版读 + 1 次我们的读**，可以在后续轮次成批推进。
**建议的下一批主题簇**（按主链价值排）：① 回合/阶段切换（`NextTurn`/`ResolveEndTurn`/`OnTurnStart/End` 那一族）
② 收局（`BattleFinished`/`DeadHero`/`CheckWinner`/`ProcessMatchResult`）③ trait 增减与 `AddEffect`
④ 联机分叉（`Receive*`/`Parse*`）⑤ 装配（`Setup*`）。

---

## ⑥ 顺手 / 跨域

1. **`CardDef.Expansion` 的默认值口径**（C 的纠正）：确认已经是 `-1 = 未知`，
   `ExpansionSearchName` 对 `<=0` 一律 `null`（搜索行为同于 `Default`，**语义不同**）—— 我的 doc 里写的就是这句。
   本轮**没有**改动这一处。
2. **`RaceDisplayName` 那一条**：本轮没碰到「按屏上兵种名搜」的代码 ⇒ 无需改；口径已记下。
3. `Shell/` 与 `CardPresentation/Core` 的并行编译错误（`Inventory.cs` / `TutorialPointerUi.cs` / `LeaderboardData.cs`）
   都是别人的在飞文件，我没碰、收尾时已由对方修好。

---

# 第 2 轮（2026-10-25 续 · 按调度台给的三条标准推进 5 个主题簇）

`typecheck`：**运行时 0 / 编辑器 0**。

## ① 数字

| 项 | 上轮末 | 本轮末 | Δ |
|---|---:|---:|---:|
| `A 已对齐` | 141 | **142** | +1 |
| `B 已实现未核` | 1,184 | **1,183** | −1 |
| `C 该做没做` | 64 | **64** | 0（+1 新 C = 回合末手牌清扫的**次序**偏差；同时 `NextTurn` 升 A 抵掉一条同名族） |
| `D` / `E` | 146 / 0 | 146 / 0 | — |

> 本轮**只升了 1 条**，因为我把大部分力气花在「按三条标准把 `Add*/Resolve*` 那一族能不能升 A」上 ——
> 结论是：**能，但只对「已经用可观察量钉住」的行**。本轮按这个口径钉住了 `NextTurn`。
> 其余同族（约 300 条）**仍然保持 B**，因为第 2 条（可观察量）我没有逐条核过 —— ⛔ 不为了凑数升。

## ② 调度台三条标准的落地（`Add*/Resolve*` 这一族）

已升 A 的样本 = `BattleManager.NextTurn`（row 489）：
1. **机制差异写清了**：原版 `_NextTurn_d__395__MoveNext` 是协程逐拍（6 跳）；
   我们 `BeginTurnCore` 是同步一次算完；**净结果等价**，依据写到了我们每一跳的行号。
2. **可观察量钉住了**：回合号 `ctx.Turn` · 双方 `Energy` · 双方手牌张数 ——
   已有断言 `TestNewBattle` / `TestBeginTurn` / `TestEnergyIsPerPlayer`。
3. **没核的不进这一族**：同族其余行仍在 B。

📌 **副产品**：核 `NextTurn` 时查清了一条容易误读的判据 ——
`PlayerManager.TurnStart(manager, param_2)` 的**第一句**就是 `if (param_2 == 0) return;`（`PlayerManager__TurnStart.c:17`），
而 `_NextTurn` 对**双方**都调了一次（`:338/:340`，第二方传的是 `!isPlayerTurn`）
⇒ **实际只有本回合那一方补能量**。我们「只给行动方补」是对的（这条以前只是被当成「大概如此」）。

## ③ 新查出的差异（1 条，本轮**未修**、已入账）

**回合末手牌效果清扫的【次序】**（`BattleManager.ResolveEndTurn` row 561，判为 `C · P2`）：
- 原版 `BattleManager__ResolveEndTurn.c` 的次序（逐跳现读）：
  `:33-137` 狂暴强制攻击 → `:385 BroadcastTurnBeforeEnd` → `:433 BroadcastTurnEnd` →
  `:483 ResolveEndTurnEnchantment` → `:623/:672 PlayerManager.OnTurnEnd`（双方）→
  **`:678/:680` 双方手牌 `UpdateCardEffects(endOfTurn: true)`（最后）**。
- 我们：`RuleCore.EndTurnCore` 把这个清扫排在 **TurnEnd 触发段之前**（`RuleCore.cs:2259`）⇒
  「`this turn` 的手牌增益」在回合末触发跑之前就被摘掉了。
- **可达性低**（要一张手牌效果 × 某个 TurnEnd 触发互相读），但**次序是实的**。
- **为什么本轮不修**：`EndTurnCore` 里 14 处次序注释/断言互相咬合，搬这一步要同一批跑 `RuleEngineTest` 全量；
  自检暂停期间单方面搬 = 没人接得住回归。账里写了「差什么」= 把它挪到 TurnEnd 触发段之后 + 同一批跑全量。

## ④ 两条口径回执

1. **`Loc.T` 印键名那一层**：`DeckRules.Describe` / `DescribeShort` 返回的是**词条键**（`RuleEngine/Core` 不碰 `CardPresentation`），
   **我这边没有直接调 `Loc.T` 的地方** ⇒ 兜底只能加在调用点（`Shell/SkirmishEventWindow.cs`，E 的文件）。
   给 E 的完整写法（一行）：
   ```csharp
   string key = DeckRules.DescribeShort(err);
   string text = Loc.T(key);
   if (text == key) text = Loc.T(DeckRules.Describe(err));   // 短档缺条 ⇒ 回长档，⛔ 别把键名印到屏幕上
   ```
2. **`CardDef.Expansion` 默认 `-1`**：确认（`Expansion { get; private set; } = -1`，
   `ExpansionSearchName` 对 `<=0` 一律 `null`）；`Parse` 缺列时也传 `c.expansionPack` 原值（缺值 = 0?）
   —— ⚠️ **这一条要如实标**：`cards_engine.json` 里这一列是**每张都有值**（0..80，取值为 0 的 897 张 = `Default`），
   所以 `-1` 只在「旧表 / 调用方没给」时出现；两类在搜索行为上同（都不参与匹配），**语义不同**（已在 doc 里写明）。

## ⑤ 下一批（5 个主题簇）的进度

| 簇 | 状态 |
|---|---|
| ① 回合/阶段切换 | **本轮核完 `NextTurn`（升 A）+ `ResolveEndTurn`（查出一处次序偏差 → C/P2）**；`TurnStartCardDraw` 上轮已 A |
| ② 收局（`BattleFinished`/`DeadHero`/`CheckWinner`/`ProcessMatchResult`） | 未开始 |
| ③ trait 增减与 `AddEffect` | 未开始 |
| ④ 联机分叉（`Receive*`/`Parse*`） | 未开始 |
| ⑤ 装配（`Setup*`） | 未开始 |

**下一轮建议**：按 ② → ③ → ⑤ → ④ 推进（④ 依赖联机线的判据，排最后）。

## ⑥ 本段追加的两件（都是别人请求的接线/配合）

### 6·1 `DeckError.InvalidDeck`（解掉 E 的断言编译错）
E 在 `CardPresentation/Editor/CardBaseDemo.cs:5143` 写了 `DeckRules.DescribeShort(DeckError.InvalidDeck)` 的断言，
但枚举里**没有** `InvalidDeck` 这一档（原来只有 `EffectOnlyCard` 走 catch-all）。
- **补法**：在 `DeckError` **末尾**追加 `InvalidDeck`（⛔ 不插中间 —— 枚举序号位移会让按号取值的地方静默错位）。
- **判据**：官方 `MenuDeck/Error/InvalidDeck` 是 15 条具名族之一，且是**唯一带 `/Short` 的两条之一**；
  原版 `DeckUtility.ToRawLocalizationString` 对认不出的错码就落这一档。
- 现在 `Describe(InvalidDeck)` = `MenuDeck/Error/InvalidDeck`、`DescribeShort` = `…/InvalidDeck/Short`（E 那两条断言可直接过）。
- ⚠️ **`Loc.T` 印键名的兜底不在我这边**：`Describe*` 只返回**键**，解析发生在 E 的调用点 ——
  已把「短档缺条 ⇒ 回长档」的一行写法给 E（见 §④·1）。

### 6·2 排位结算接进对局结束那一拍（G 的请求）
- **落点** = `BattleDriver.ShowEndPanelAndFreezeInput()` 的**最前面**（在 `_endPanel == null` 那道早退**之前** ——
  A382 那一档「面板建不出来账照记」同一条口径：结算也不该因为没面板就丢）。
- **只做调用**：`_rankedScore = RankedData.ApplyMatchResult(outcome)`；`RankedMatchScore` 是 **struct** ⇒
  另设 `_rankedSettled` 布尔当「每局只结算一次」的闸。对外读口 = `RankedScore` / `RankedSettled`。
- **三道闸**：① `_replaySession`（回放 ⇒ 不结算，否则白送一局连败）② `_net == null`（联机局的分数在服务端，
  本机算一遍只多一条假连败）③ `RankedData.IsRanked`（G 的开关，默认关 ⇒ 单机练习不受影响）。
- ⛔ **驱动里没有一行结算逻辑**（段位换算/连败/增减全在 `Shell/RankedData.cs`）。

## ⑦ 第 3 段：② 收局簇（按你的顺序推进）

`typecheck`：**运行时 0 / 编辑器 0**。账：**A 142 → 144 · B 1,183 → 1,180 · D 146 → 147 · C 64（不变）**。

| 账行 | 方法 | 判 | 依据（机制差异 + 可观察量） |
|---|---|---|---|
| 211 | `BattleManager.BattleFinished` | **A** | 原版是「停 AI 协程 → 教程战后脚本 → `BasicBattleEndSequence`/`CompleteBattleFinished`」的**演出链**；引擎侧判据只有 `battleWinner − 10 == 0 ⇒ isVictory`（我们 `Ctx.Winner == _me + 1`）。**净结果等价**（`BattleManager__BattleFinished.c`）。**可观察量**：结算面板胜负档 + 对局记录那一行 `_rec.result`。⚠️ 战后教程脚本那支整套没做（FTUE 未建，已单列） |
| 301 | `BattleManager.DeadHero` | **A** | 原版 = 「算 winner → **平局门**（取胜方督军也必须活着：`cardState != waitingToDie` ∧ `Health > 0` ∧ `!= inCemetery` ∧ `battleResult != DrawButton`，否则 `Draw`）→ 教程 `playerAlwaysWins` 覆盖 → 演出」。我们 `RuleCore.CheckWinner` 一个口算完同一套三步（注释逐条引 `DeadHero.c:253-269`）。**可观察量**：`Ctx.Winner`（1/2 胜方 · 3 平局）+ `[BattleResult]` 那行码 |
| 511 | `BattleManager.ProcessMatchResult` | **D** | 方法体全是 `CloudscriptHandler.LogErrorToPlaystream` / `SaveMatchWinner` / PlayFab 排行 ⇒ **服务端上报，本工程没有后端**（离线那半走 `BattleLogData`，另一条账） |
| 431 | `GetWinnerAfterBattleEnd` | **A**（上一轮已升） | 平局门与教程覆盖同 `DeadHero` |

**下一批**：③ trait 增减与 `AddEffect`（⚠️ 这一族正是 `Add*` 那条标准的适用面 —— 逐行补「机制差异 + 可观察量 + 依据」三样，缺一条留 B）→ ⑤ 装配（`Setup*`）→ ④ 联机分叉。

## ⑧ 那条次序 C 的备注已按你要求写死

`ResolveEndTurn`（row 561）的 `备注` 现在就是你要的三样 + 触发条件：
① 原版次序 `ResolveEndTurn.c:678/680`（最后）vs 我们 `RuleCore.cs:2259`（TurnEnd 触发段之前）；
② 可达性低但次序是实的；③ **触发条件 = 自检恢复后的第一批**（搬 `ExpireHandBuffs(ctx)` 到 TurnEnd 触发段之后 + 同一批跑 `RuleEngineTest` 全量；`EndTurnCore` 有 14 处次序注释/断言互相咬合）。⛔ 自检暂停期不单方面搬。

## ⑨ 第 4 段：`overrideByArmies` 的阵营来源（资源侧对账 · 调度台派单）

`typecheck`：**运行时 0 / 编辑器 0**。

### 改了什么（`BattleDriver.cs`，我的文件）
场上卡的 `CardData` 现在用**【卡自己的】阵营**，不再用拥有者的：
```csharp
string cardFaction = (u.Card != null && !string.IsNullOrEmpty(u.Card.Faction))
                     ? u.Card.Faction : faction;      // 卡没有阵营才回落到拥有者
… frame = FactionColor(cardFaction), faction = cardFaction, …
```
（这是 `ToCardData(UnitState, faction, isPlayer)` 那一个重载 —— **只影响场上/展示的 `CardData`**，
手牌那条路 `ToCardData(CardDef, faction)` 本来就是按卡取的。）

### 判据
- 原版读的是卡自己的 `rawCardData.cardArmy`（`ObtainableItem.cardArmy`，偏移 `+0x2C`）；
  消费点 = `TraitFrames.ArmyId` → `TraitEffect.GetEffect(army)` 的**按阵营覆盖**（`overrideByArmies`，
  `TraitEffect__GetEffect.c:9-34`）；同族还有卡框纹理 `CardArt.Frame(d.faction,…)` 与死亡粒子
  `VfxMap.ResolveDeathParticle(d.faction,…)` —— 三处都该按**卡自己的**阵营。
- **可达性**：同阵营卡组逐字不变（卡组构筑规则本来要求同阵营）；只有**卡池外造出来的跨阵营卡**
  （盟军/中立/衍生卡）才变，而那正是原版那两个读点的语义。影响面 = `Shield` 4 处 + `Invulnerable` 6 处覆盖。

### 给 E 的一条
`Core/TraitFrames.cs` 文件头那条「已知偏差：要从 `BattleDriver` 传 `CardDef.Faction`」
**已经不需要再补 override 字段了** —— 它 `GetComponent<CardView>().Data.faction` 里现在拿到的**就是卡自己的**阵营。
（若 E 已经加了 setter，留着也无害，只是**没有任何调用点会去设它**——建议顺手把那句「已知偏差」改成「已解决」并删掉 setter，避免两份口径。）

## ⑩ 第 5 段：转派的 C 项「`CardTrait` 缺 `_hide` 位」—— **结论：没有这一列，也不该建**

`typecheck`：**运行时 0 / 编辑器 0**。账：**B 1,180 → 1,179 · D 147 → 148**（row 1189 `EntityScript.IsShowingTrait` 改判 D 并写满证据）。

### 现读原版后的事实（三条，逐条有出处）
1. `_hide` 是 **`CardTraitData` 上的运行期字段**，由 `CardTrait.GetNewTrait(trait, hideTrait = true, value)` 的**第 2 个实参**初始化（`CardTrait.cs:81-90`）。
2. 它**唯一的生产调用点** = `CardScript.AddTraitSilently(card, trait, hideTrait, value)`（`CardScript.cs:19299-…`）；
   而全量反编译里 **5 个调用点全部传 `hideTrait = 0`**
   （`AddEffect.c:499/947` · `ResolveDamageDealt.c:187` · `Stun.c:48` · `UseSurvivor.c:71`）⇒ **真 trait 一律可见**。
3. `_hide = true` 只出现在两处：① `GetNewTrait` 的**构造默认值**（下一句就被第 2 个实参覆盖）；
   ② `EntityScript.GetCurrentTraitData` 的**「查不到就返回一条隐藏占位」**（`EntityScript.cs:3346-3347`，那是显示 helper）。

### 结论与落点
- **卡表没有 `_hide` 这一列，也不该有** —— 造一列等于编数据（⛔ 你原话：别自己编值）。
- 今天**没有任何可达调用点**会传 `hideTrait = true` ⇒ 那道闸在原版里也是「恒过」。
- 🔑 **将来的落点**（真出现 `hide=true` 的调用时）：`UnitState.AddKeyword` 加一个 `hidden` 变体
  + `Badges.For` 跳过 hidden 的键。**今天不建**（否则就是死字段，正是本仓 `[Obsolete(死列)]` 要防的）。
- 📌 **卡牌面账 95/97 两行请 E 据此核销**（我改的是**战斗逻辑驱动**账 row 1189，写的就是这个结论）。

### 另两条（按你的分工）
- **`GetTooltipPosition = mouseWorld + 10pxY`**：已派给 E，我**没动**。
- **`MouseWorld` 现成口**：`BattleDriver` 里本来只有私有的 `WorldPointer()`。E 要算 tooltip 就得用它 ——
  它里面有一道**「指针在屏幕外 ⇒ 归零、不外推」**的原版守卫（`BattleManager__GetMousePerspectivePos.c:26-27/40-44`），
  ⛔ 下游自己算 `ScreenToWorldPoint` 会漏掉这道守卫、让 tooltip 跟着指针飞到场外。
  ⇒ 我在 `BattleDriver` 加了一个公开只读别名 **`public Vector3 MouseWorld => WorldPointer();`**（带判据注释），**给 E 直接用**。

## ⑪ 第 6 段：`_hide` 改判 A + ③ trait/`AddEffect` 族开工

`typecheck`：**运行时 0 / 编辑器 0**。账：**A 145 → 146 · B 1179 → 1178 · D 147 · C 64（不变）**。

### 11·1 `_hide` 那条按你的口径改判 **A**（不留 C、不是 D）
`row 1189` 现在的 `备注` 就是你要的那段：
> 原版 `CardTraitData._hide` 存在但**零可达**（5 个生产调用点全传 `hideTrait = 0`；`_hide = true` 只在 `GetNewTrait` 的构造默认值与 `EntityScript.GetCurrentTraitData` 的显示占位两处）⇒ 我们无该字段但 `ShowingTrait` 行为**等价**。将来真出现 `hide = true` 调用时：`UnitState.AddKeyword` 加 `hidden` 变体 + `Badges.For` 跳过。

### 11·2 ③ 开工：`CardScript.AddTraitSilently` → **A**
判据（`CardScript.cs:19299-…` + `CardScript__AddTraitSilently.c:1-90` 方法体现读）：**`CurrentTraits` 里已有该 trait ⇒ 直接 return**（不叠第二条），否则造一条并**同时**加进 `CurrentTraits(+0x128)` 与 `CurrentShowingTraits(+0x130)`。
- **机制差异**：原版是「一个 trait 一条 `CardTraitData`，重复被 `Enumerable.Any` 挡掉」；我们是**字典一键一值**（同名天然一条）。
- **净结果等价 + 依据**：`Has(trait)` 同真、重复 add 不会变两条；我们 `UnitState.AddKeyword` 的三条写点。
- **可观察量**：`u.Keywords` 键集合 + `u.KwValue(kw)`（`TestAttackKeywords` / `TestBattleKeywords` / `TestAuras` 都在断言）。
- ⚠️ **结构性差别如实标**：原版 `AddTraitSilently` **不让值累加**（值建条时定），`Armour +1` 那族走 `AddEffect(buffType=addStats/addTrait)` **另一条路**（多条 effect 各带 `traitValue`，由 `GetCurrentTraitValueWithModifiers` 求和）；我们把两条路合成「`AddKeyword(k,+v)` 累加」—— **总数等价**。

### 11·3 `CardScript.AddEffect` → **保持 B**（按标准第 3 条：没核的不许进）
- 它是 **824 行**的 `buffType` 分派器。**可读的那几段**已经核过：① 卡状态闸（`waitingToDie(5)` / `removedFromGame` 不收）；② `extrinsic ∧ 施放者 jam` ⇒ 不收；③ `ContainsExtrinsicEffectFrom(...)` 命中 ⇒ 不收（除 `removeExtrinsic`）；④ 按 `buffType` 分派。这四条我们都在 `ApplyOneGain` 里实现了。
- 🔴 **为什么仍留 B**：反编译在这一段**丢了大量条件**（连续空 `if` 块 + `attackTypes = Melee;` 占位）—— 按 `.c` 读只能读出「看着像」的形状，本仓规矩是**回 VA 反汇编**才认。**差什么**已写进账：
  ① 把 `BuffType` 枚举值逐个对上（`BuffType.cs` 有定义）；② 每个分支回 `CardScript__AddEffect.c` 对应地址段读条件；③ 四条闸做 VA 级逐句核。
- ⛔ **没有为了数字把它升 A** —— 824 行反编译丢条件，升 A 就是把「看着像」当判据。

### 下一步
③ 剩下的：`RemoveTrait` / `RemoveTraitSilently` / `RemoveTraitAndEffects` / `RemoveBuffedTrait` / `AddArmour` / `SetBaseCardTraits` / `RemoveActiveEffect(s)` / `ContainsTrait`
→ 然后 ⑤ 装配（`Setup*`）→ ④ 联机分叉。

## ⑫ 第 7 段：`AddEffect` —— 用第二意见源把「读不动」变成「逐分支覆盖表」（仍判 B，但差什么都点清了）

`typecheck`：**运行时 0 / 编辑器 0**。账 **1,535 行 / 11 列**：A146 · **B1178** · C64 · D147。

### 你给的取证方向有效 ✅
`D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs` 的 `AddEffect`（1083 行）**分支骨架确实保住了** ——
能读出：`switch (buffType - 1)` 覆盖 1..30，外层再按 `<= 35 / == 35 / <= 45 / > 45` 分档。
（`CardScript` 那份 ilspy 产物在这一段是**连续空 `if` 块**，完全读不出条件。）

### 读出来的四条**进门票闸**（都已在我们这边实现）
1. 卡状态 `waitingToDie(5)` / `…(6)` / `removedFromGame` ⇒ 不收（`LogWarning`）。
2. `extrinsic ∧ 施放者带 jam` ⇒ 不收。
3. `ContainsExtrinsicEffectFrom(施放者, buffType, baseEnchantmentCard)` 命中 ⇒ 不收（`removeExtrinsic` 除外）。
4. 🔑 **`doubleAttack(20)` 在分派【之前】被改写成 `addStats`**：`meleeAttackChange = CurrentMeleeAttack`
   （原版日志 `"Convering doubleAttack buff into +N"`）⇒ 「翻倍」不是独立分支，是**先算成 +当前近战**。

### `BuffType` 29 值 × 我们的落点（覆盖表，已写进账 row 768）
| ✅ 有落点且核过 | 🟡 有落点未逐句核 | ⛔ 未建 / 不可达 |
|---|---|---|
| 1 addStats · 3 addTrait · 4 removeTrait · 7 skipCardDraw · 11 removeExtrinsic（= `Aura.Recompose` 整体重算，等价）· 12 enchantment · 15 singleUseEnchantment · 20 doubleAttack（`"double"` verb）· 25 ephemeralCopy · 26 tideCopy | 2 changeCost · 40 replaceAbility · 70 setMeleeAttack · 95 setCost | **5 swapStats · 6 changeRace · 9 setHealth · 10 changeAbilityCost · 16 choiceEnchantment · 30 addRace · 85 setRangeAttack · 100 setCurrentHealth**（8 个真未建）· 8 markOfChaos / 17 lastMarkOfChaos（池内 0 张）· 45 addStatsDropPod / 90 addTraitRemnant（不可达）· **35 daemonHost = 原版自己 `LogError("not implemented")`** ⇒ 不需要 |

### 为什么**仍然 B**（按你的第 3 条标准）
- 还有 **8 个真未建** + **4 个未核** ⇒ 不能算「已确认净结果相同」。
- **差什么（已写进账，可执行）**：
  ① 先用 `cards_raw.json` 的 `abilities[].logic[].buff.buffType` 扫这 8 个的**池内可达性**：
     可达的建、不可达的按「原版有我们没有」入账（铁律 11：仍要做，只是排后）；
  ② 那 4 个 🟡 回 `CardScript__AddEffect.c` 的对应地址段读条件；
  ③ `out_il2csharp` **只当分支骨架**（它枚举映射丢、分支里用未赋值局部变量）⇒ 条件里的枚举/常量一律回 `decomp_full` 或 VA。

### 一个操作事故（已在当轮修好，如实记）
写账时我把带 `\n` 的多行文本塞进了一个单元格 ⇒ TSV 被拆成 3 行、行数变 1537、列数校验失败。
**当场按「首行前 7 列 + 续行 + 尾行后 3 列」重组回一行**，校验回到 **1,535 行 / 全 11 列**。
⇒ 纪律：**TSV 单元格里永远不许有换行**；写完必须跑一次「行数 + 每行 11 列」校验（这次就是被校验抓到的）。

## ⑬ 第 8 段：粒子碰撞平面的 `FloorPositionReference` tag（D 的需求）

`typecheck`：**运行时 0 / 编辑器 0**。

### 🔴 **判据纠正：tag 只打在【一块】平面上，不是 7 块**
派单里写的是「那 7 个粒子碰撞平面要打 tag」，但现读原版后**不成立**，证据两条：
1. `ParticleFindFloorCollider__Start.c:20-45`：`if (floorCollider == null) floorCollider = GameObject.FindGameObjectWithTag(<tag>)?.transform;`
   —— 缓存是**静态**的（全场上**只有一块**地面），随后 `collisionModule.SetPlane(它)`：**9 处挂载各自那套粒子都把碰撞面设成同一块**。
   而 `FindGameObjectWithTag` 返回的是**第一个**匹配 ⇒ **7 块全打 = 碰撞面随机变成某块竖直的墙**（那 6 块的法线是 ±Z）。
2. 场景 JSON 现读（`bundle_scenes_scenes_battlearena1/GameObject/`）：
   | 物体 | `m_Tag` |
   |---|---|
   | **`Floor Position Reference`** | **20001**（Unity 内置 tag 0..19999，自定义从 20000 起） |
   | `Player Minions Particle Collision` / `Player Warlord …` / `Player Warlord From Camera …` / `Enemy Minions …` / `Enemy warlord …` / `Generic Target` | **全 0 = Untagged** |

⇒ **只给 `i == 0`（`Floor Position Reference`）那一块打 tag**，注释里把上面两条证据写死了。

### 改动
1. **`ProjectSettings/TagManager.asset`**：`tags: []` → `tags:\n  - FloorPositionReference`（原工程这个 tag 就在，我们这份是空的）。
2. **`BattleDriver.ParticleCollider`** 建 7 块的那一圈里：`if (i == 0) go.tag = "FloorPositionReference";` + 判据注释（含「⛔ 别 7 块全打」的理由）。
3. **断言（`BattleScene.cs`，接在既有那 7 块位姿断言之后）**：
   - `GameObject.FindGameObjectsWithTag("FloorPositionReference").Length == 1`（**唯一性** —— 这条正好挡住「7 块全打」这个错法）；
   - 带 tag 的那一块**位置 == `ParticleColliderAt(0)`**（就是那块水平的地面，不是某块竖直的墙）。
   ⚠️ 「不该出现『找不到地面』的出声」那一半：那条 `LogError` 只在 `WFEffectExtras.ParticleFindFloorCollider`
   （**已废弃**、产品路径不调用）里；原版等价物就是上面那句 `FindGameObjectWithTag` ⇒
   **只要这两条断言成立，任何调用方都找得到地面**，所以用它当那一条的可验形式（报告里如实标了这层替换）。

## ⑭ 第 9 段：③ `RemoveTrait` 族 —— 5 条升 A，**又查出一处真差异并修掉**

`typecheck`：**运行时 0 / 编辑器 0**。账：**A 146 → 151 · B 1178 → 1173**（C 64 · D 147 不变）。

### 🔴 真差异：**摘掉 `flank` / `fast` 时原来没有重算 act/attack**（已修）
- **判据**：`CardScript.RemoveTrait`（`CardScript.cs:19361-19410`，方法体）：
  ```csharp
  if (cardTraitData._trait == blind)              UpdateFigures();
  if (cardTraitData._trait == flank || == fast)   UpdateSummonSicknessAnims();   // → ActivateMinion
  ```
  `ActivateMinion.c:39-42` 把 `canAttack(+0x23C)` 与 `canAct(+0x230)` **一起**写成 `!displaySummonSickness`，
  而 `displaySummonSickness = +0x58 && !fast && !flank` ⇒ **摘掉这两个词必须立刻把两位写回去**。
- **我们原来**：**加的那半边有**（`AddKeyword` / `AddAuraKeyword`，`AddEffect.c:570-572` 那一跳），
  **摘的半边没有** ⇒「本回合给的侧翼/迅捷到期、或被效果摘掉」之后，单位仍按带 `flank` 算
  （`CanAct`/`CanAttack` 停在上一次的值，**静默**）。
- **修法**：`UnitState.RemoveAll` 里补 `if (keyword == "fast" || keyword == "flank") RecomputeActAndAttack(this);`
  **外加** `ClearAuraGrants` —— 光环那本账是**直接改字典**的（不走 `RemoveAll`），所以光环收回 `fast/flank` 时同样要重算。
- **断言**（新落 `RuleEngineTest.TestA6RemoveFlankRecomputesAct`，3 条）：带召唤病 ⇒ 两位为真 → 加 `flank` ⇒ 两位解除 →
  **摘掉 `flank` ⇒ 两位立刻回到真**｜🧨 删掉 `RemoveAll` 里那句 `RemoveActAndAttack` ⇒ 变红。

### 升 A 的 5 条（每条都读了原版方法体）
| 行 | 方法 | 要点 |
|---|---|---|
| 767 | `AddArmour` | `currentArmour += n` → **结果 < 0 夹成 0** → `UpdateHealthText()`。我们减甲三条路都夹 0（`UnitState.cs:1040/1172/1274`）；可观察量 = `u.Armor`（`TestArmorMinimumOne`） |
| 814 | `ContainsTrait` | 在 trait 列表里找有没有这一个 ↔ 我们 `_keywords` 字典键存在；可观察量 = `u.Keywords` |
| 974 | `RemoveTrait` | 见上面的真差异；另外 `blind ⇒ UpdateFigures`（我们的 `blind` 派生属性天然成立）、`CleanStatusAnims/UpdateTraitIcons`（表现层由集合对差刷） |
| 975 | `RemoveTraitAndEffects` | `activeEffects.RemoveAll((addTrait‖addTraitRemnant) ∧ trait==t)` 再 `RemoveTrait`；我们 `RemoveAll` **同时撤「授予的正文」**（`_grantedOps/_grantedText` 那本账）⇒ 等价 |
| 976 | `RemoveTraitSilently` | 只摘两个列表 + `RemoveStatusAnim`，**显式跳过**数值重算。我们「数值重算 / 图标刷新」本来就分开 ⇒ 图标那半等价 |

### ⚠️ 一处**如实标的不对称**（未修，已入账）
`RemoveTraitSilently` **不重算**，而我们的 `RemoveAll` 现在**无条件**重算 ⇒ 若某条路用 Silently 摘 `fast/flank`，
我们会**立刻恢复疲劳**、原版**停在旧值**。
`RemoveTraitSilently` 有 **6 个真实调用点**（`AddEffect` / `CardSetup` / `RemoveEffect` / `ResetToValuesInHand` /
`ResolveCardPlayed` / `TransformIntoRemnant`），但**没有一条核过「会不会摘 fast/flank」** ⇒ **可达性未核**。
要完全对齐得给 `RemoveAll` 加 `silent` 形参并逐条核 —— 本轮**没做**（没核实就加 = 死参数），已写进账的「差什么」。

### ③ 剩余（仍 B）
`RemoveBuffedTrait`（只摘「被 buff 出来」的那一份，需与 `addTrait` 的账对齐）· `SetBaseCardTraits`（把 `CurrentTraits` 重置回卡面）
· `RemoveActiveEffect(s)` / `RemoveEffect` / `GetCopyOfCardEffects` / `AddEnchantment`。
之后 ⑤ 装配 → ④ 联机。

## ⑮ 第 10 段：联机 `D` 的两件接线（都在 `BattleDriver.cs`）

`typecheck`：**运行时 0 / 编辑器 0**。账数字不变（A151 · B1173 · C64 · D147）。

### ① `INetFoePresentationHost` 实现（3 个表现口）
类声明挂上接口：`BattleDriver : MonoBehaviour, INetBattleHost, INetChatHost, **INetFoePresentationHost**`。

| 接口方法 | 我们做了什么 | 判据/理由 |
|---|---|---|
| `NetRemoteCardSeen(uniqueInGameId)` | 按**局内唯一 id**（`CardInstance.Id`，⛔ 不是卡 id）在**双方手牌 + 双方棋盘**里找到那一份实例，用**现成的卡展示窗**把它摆出来（`_cardDisplay.Show`）；找不到 ⇒ `Ctx.Log` 出声 | 原版 `ShowEnemyCardSeen(uniqueInGameID)`；⚠️ 找不到时**不静默** |
| `NetRemoteCardReorder(id, newIndex)` | **只出声**：我们的对面手牌是**一排卡背**、没有「逐张顺序」的视图模型（`_foeHandViews` 按槽位摆、不记实例顺序）⇒ 不做视觉变化，日志里说明 | 原版 `ShowEnemyHandReorder` 也只影响对面手感、不影响局面（D 的注释同口径） |
| `NetRemoteMatchFinished()` | **只出声**（`Ctx.Log`），⛔ **不判胜负** | 原版它只用来开/确认「等对手最后几条动作」那个窗口；判胜负是 `NetRemoteResign` / 掉线那几档 |

### ② 「等对手最后几条动作」那个窗口（原版 `WaitOpponentLastActions`）
- **判据（D 读透的，我照抄并注明来源）**：条件 = `matchFinished || ActionsLeftInQueue() > 0`；
  循环 `while(cond && remaining>0){ remaining -= 2.0f; yield ≈1s }`；
  `FinishWaitStep = 2.0f`（`GameAssembly.dll` 的 `0x1834b2bbc` 直读 = `00000040`），上限 12.5 s。
- **落点**：`ShowEndPanelAndFreezeInput()` **最前面**（在 ranked 结算与面板之前）：
  `if (!_finishWaitStarted) { _net.SendMatchFinished(); _net.BeginFinishWait(); }` →
  `if (_net.TickFinishWait(dt)) { _endPanelPending = true; return; }`；
  `Update()` 里每帧 `if (_endPanelPending) ShowEndPanelAndFreezeInput();` 重试
  （那个方法是一次性调用，没有这句窗口关掉之后面板就永远不出）。
- 🔑 **`ActionsLeftInQueue()` 沿用 D 的代理量（`Session.Inbox.Count`）** —— ⛔ 没有另造第二份口径。
- 回放局 `_net == null` ⇒ 这条天然不生效。

## ⑯ 第 11 段：四件小接线（含 **1 个真缺陷** + 3 处注释订正）

`typecheck`：**运行时 0 / 编辑器 0**。账 1,535 行 / 11 列（A151 · B1173 · C64 · D147）；其中 row 630/695 两行按本轮订正写满了判据。

### 【1】🔴 `manaAccumulation` 挂错字段（真缺陷，已修）
- **判据**：`BattleManager__SetupBoardPhase.c:181/196` 读的是 `GameplayVariablesData **+0x34** = addedManaSecond`
  （`manaAccumulation` 在 **+0x40**，两者相邻 ⇒ 被读混）。
- **改前的两个毛病**：① 读 `manaAccumulation`（错 3 个字段宽）；② 那个值只在 `BuildHud` 里算一次、**不随局变**
  ⇒ **遭遇模式（`addedManaSecond` 非 0）那盏灯永不亮**。
- **改法**：删掉那个无人赋值的本地量，换成 **`EnergyAccumulationOn`**（`Ctx.Vars.addedManaSecond > 0`）
  + **`RefreshEnergyAccumulation()`**（判据只此一处），建完之后与**每次 `RefreshAll()`** 都走它。
- ⚠️ 顺带订正旧注释里「这个数是 LiveOps（服务端下发）的、本地拿不到」—— `addedManaSecond` 是**规则书口径**
  （经典 0 / 遭遇 1，见 `GameplayVariables`），不是服务端值。

### 【2】Tooltip 跟鼠标（一行）
`Tooltip.Show(body, v.KeywordAnchor)` → **`Tooltip.ShowAtMouse(body, MouseWorld, v.KeywordAnchor.z, 1f / TipPx)`**。
- 判据：原版 `GetTooltipPosition` = `ScreenToWorldPoint(mouse.x, mouse.y + 10, text.transform.position.z)`，
  那 10 = `DAT_1834b2da8`（E 已读成 `Tooltip.MouseTipDyPx`）。
- ⛔ **没有自己写 `ScreenToWorldPoint`** —— 用 `MouseWorld`（带「指针在屏幕外 ⇒ 归零、不外推」的原版守卫）。
- ⚠️ `textWorldZ` 取 `v.KeywordAnchor.z`（卡面这一层没有单独的 TMP 句柄），已写进注释。

### 【3】逐卡 tooltip 开关的消费侧（一行 ×2 + 「先关后开」）
- `TickTraitTip` / `TickCardTip` 各加一句 **`if (!v.TooltipsEnabled) return false;`**
  —— 缺它 `CardView.TooltipsEnabled` 就是**没人读的死字段**（E 已在 `CardDisplayWindow` 两处设它）。
- **「新卡先关、摆好后开」**（原版 `BattleCardManager:128`，我们没有那个类 ⇒ 落在这里）：
  手牌的**唯一建件点**（`SyncHand` 里 `CardView.Create` 那支）建出来先 `TooltipsEnabled = false`，
  `interaction.SetCards(...)`（布局做完）之后统一打开。

### 【4】铁律 5 订正「Skip 钮常驻」那条注释（**3 处，一起改**）
- **错在哪**：原版 `BattleSettingsWindow.cs:225-232` 的**第一句**就是
  `skipTutorialButton.SetActive(matchType - 100 == 0)` ⇒ **只有 `MatchType.Tutorial(100)` 才显示**，不是常驻。
- **改了 3 处**（`grep 常驻` 收出来的全部）：`BattleDriver` 里「点了设置钮但面板没建起来」那条、
  `HitSkipTutorial` 输入那一段、`ApplyTutorialSkip` 的 doc 头。都写成「⚠️ 2026-10-25 订正：原来写 X，实际是 Y（出处）」，
  并**保留**原结论（输入侧照样吃掉这一下）但**换掉理由**。
- 🔴 **由此暴露一条跨域待办**：**「Skip 钮的显隐」我们还没接**（原版那一句在 `BattleSettingsWindow` 的
  `Setup`/`Open` 里）⇒ 归 B/D 域（`Battle/SettingsPanel.cs`）。已写进上面那条注释的「已单列为待办」，
  也在报告这里留痕（⛔ 我没碰那个文件）。

## ⑰ 第 12 段：**A 档落点收紧（131 条全处理）** + E 的四条补丁

`typecheck`：**运行时 0 / 编辑器 0**。账 1,535 行 / 11 列。

### 17·1 A 落点：**148 条 A，全部带 `文件:行`**（改前 20 条合格）
按 `报告/G_A档审计_跨账.md`（战斗逻辑驱动那一段 131 行）逐条处理：

| 处理 | 条数 | 做法 |
|---|---:|---|
| 🟢 抄 G 算好的声明行 | 121 | 落点改成 `文件:行 ｜ <原落点>`（**只改落点、状态与判据要点一个字没动**） |
| 🟡 我现找的方法级落点 | 7 | `AvailableLeftSlot/RightSlot/AvailableSlotsCount/IsAvailableSlotLeft/Right` → `BoardSlots.cs:87`（**共用实现**：原版各算自己那一侧，我们收在 `RoomOnSide(side)` 一个口）+ `OnTrigger` ×2 → `EffectResolver.cs:7148`（三个重载共用同一个分发器） |
| 🔴 找不到 ⇒ **退 B** | 3 | `CardScript.SendToCemetery`（我们模型里没有对应方法）· `RawCardScript.ContainsRelatedCards` / `GetRelatedCards`（**`CardDef.RelatedIds` 不存在**，`grep` 0 命中） |

**校验**：脚本跑完后自检「A 条数 = 带 `file:line` 的 A 条数」⇒ **148 = 148** ✅。
抽查 6 个行号，全部指向真实声明（`RuleCore.cs:5185 IsValidTarget` · `:5499 CanAttackNow` · `EffectResolver.cs:7148 TriggerNameOfWhenKind` · `BoardSlots.cs:87 RoomOnSide` · `CardDef.cs:2439 AbilityTriggerPresence`）。
⚠️ 顺手订正自己两处：`GetBaseCardsInHandCount` / `GetSecondExtraCardsCount` 原本指到 `RuleCore.cs:276/348`（**函数中间**）⇒ 改指 `RuleCore.cs:105 HandMaxFor`（起手张数公式的真身）。

### 17·2 E 的 P0 补丁（四条）
| # | 内容 | 状态 |
|---|---|---|
| ① 倒计时遇演出顺延 | `_clockInCountdownAnimDepth` + `PauseClockForAnim/UnpauseClockForAnim` + `TickClock` 里 `if (_clockInCountdown && (depth>0 ‖ WaitingForBlockingAction)) return;` | ✅ 已落（判据 `ClockManager.cs:340-348/736-742/663-672`；⛔ 没放进回合表分支） |
| ② 结算收窗 | `ShowEndPanelAndFreezeInput()` **第一句** `WindowsManager.Instance.CloseAllWindows()` | ✅ 已落（判据 `EndBattleDoors__SetupDoor.c`；放在「等对手最后动作」闸**之前**——收窗是「这局结束」的即时反应） |
| ③ 抽牌终点锚（194） | `LeftArea/CardDrawPositions/{PlayerDrawnPos,EnemyDrawnPos}` | ⏸ **未做，判据已备**：13 份战场树里都有这两个节点（arena1 现读 `PlayerDrawnPos [1350,1030 100x100]` / `EnemyDrawnPos [1350,1030 100x100]`），但**那份 dump 的坐标系约定（y 朝哪、是设计 px 还是锚点）没核**⇒ 按铁律 3 **不猜参数**（编一个坐标 = 又一个「我们自己挑的数」）。核法：拿 `HudAbs` 已有的 4 件（能量板/骷髅/…）反算同一份树里的坐标，对上了再用。 |
| ④ 设置面板两行 | `SettingsPanel.Create(root, Forfeit, Ctx?.MatchType)` + `OnSkipTutorial = SkipTutorialFromSettings` | ✅ 已落（漏传会出声，不会静默） |

### 17·3 对「自绘光标（448-450）」的评估（你让我判断能不能在 `BattleDriver`/`Battle/**` 里做）
**结论：技术上能，但不该塞进 `BattleDriver`。**
- 原版 `CursorManager` 是 `OnGUI` 自绘 + `Start/Update` 跟随（E 的判据）——`BattleDriver` 里加一个 `OnGUI()` 也能跑，但那个文件已经 **3 万行**、且 `OnGUI` 会在**每一帧的多个事件阶段**回调（Layout/Repaint），与驱动现有的「`Update` 单入口 + 显式时间泵」模型冲突（`Time.deltaTime` 在 `OnGUI` 里不可靠）。
- 规矩上：`CardPresentation/Battle/**` 除 `BattleDriver.cs` 之外**归 D**；新建 `Battle/CursorManager.cs` 等于在别人的所有权里落件。
- ⇒ **建议**：作为**独立小件**派给 `Battle/**` 的所有者（D），或在下一批把 `Battle/` 收回来时由我建。判据 E 已给全（`CursorManager.OnGUI` + `Start/Update`），不需要再查。

## ⑱ 第 13 段：③ 剩余 7 条收口（3 升 A · 1 降 C · 3 留 B）

`typecheck`：**运行时 0 / 编辑器 0**。账 **1,535 行 / 11 列**：**A 151 · B 1,172 · C 65 · D 147**；**A 里带 `文件:行` 的 = 151/151** ✅。

| 行 | 方法 | 判 | 依据 |
|---|---|---|---|
| 955 | `RemoveActiveEffect(trait)` | **A** | 摘掉产生该 trait 的那些 effect ↔ 我们 `UnitState.RemoveAll`（关键词 + 它授予的正文一起撤）；可观察量 `u.Has(trait)` / `u.Keywords` |
| 956 | `RemoveActiveEffects(buffType, trait)` | **A** | 「加 trait 前先清掉所有 `removeTrait` 效果」↔ 我们 `AddKeyword` 里的 `_removedTraits.Clear()`（判据 `CardScript__AddEffect.c` 的 `case 3` 第一句 `RemoveActiveEffects(card, 4)`，**不带 trait 过滤**） |
| 961 | `RemoveBuffedTrait(trait)` | **A** | 倒序摘 `{addTrait, addTraitRemnant} ∧ !extrinsic ∧ trait==参数` 的每一条。⚠️ **它总与 `RemoveTrait` 成对调**（`AddEffect` case 4 / `RemoveDropPod`）⇒ 合起来 = trait 整个消失，与 `RemoveAll` 一致；「只摘 buff、留卡面那份」单独用我们**没有**对应物，但**全量反编译 0 个单独调用点** ⇒ 不可达（已如实写进账） |
| 769 | `AddEnchantment(e)` | **C · P2** | 🔴 **第五步缺**：追加完 `ActiveEnchantment` / `activeEffects` 之后原版调 **`ActivateTraitsOnSummonOrEnchantment(onSummon:false)`**。**为什么缺（不是加一句就行）**：我们没有「附魔卡 → 关键词」那条数据通路 —— 原版是把附魔的 `CardEffect` 放进 `activeEffects`、再由 `GetCurrentTraitValueWithModifiers` 把 `addTrait` 那份并进 trait 表，**所以附魔带的关键词会真挂到载体上**；我们的附魔是**另一层**（`ActiveCardEffect`），载体的 `_keywords` 不并它 ⇒ 得先做「附魔关键词并进载体」，再接那一句调用。判据与两步修法都写进账了 |
| 851 | `GetCopyOfCardEffects` | **B** | 原版是 `activeEffects` 的浅拷贝，调用点 = 自动召唤出一张复制品 / 打出的是一张复制品。**我们没核「复制品是否继承源卡的 `_keywords`/`_grantedOps`」**（也没有 `activeEffects` 那张表）⇒ 差什么写进账 |
| 966 | `RemoveEffect(effect)` | **B** | 原版摘**一条记录**，我们按关键词整份撤 ⇒ 「同一关键词叠了多条不同来源」时结果不同。差什么 = 核 `AddEffect` 里哪些分支只摘一条 |
| 1011 | `SetBaseCardTraits` | **B** | 原版「把 `CurrentTraits` 整个重置回卡面」；我们只有构造期装载、没有重建口。差什么 = 核两个调用点（`CardSetup` / 回手重置），需要时加 `UnitState.ResetTraitsToCard()` |

### ③ 结账
`AddEffect`（B，824 行分派器 + 逐 buffType 覆盖表）是这一族**唯一剩下的硬骨头**；其余要么 A、要么带「差什么」的 B/C。
🔴 **最有价值的一条新发现**（C 那条）：**我们的附魔层不把附魔卡的关键词并进载体** —— 这解释了「附魔带 `Armour 1` 之类的卡在我们这边不生效」的整类现象，比补一句调用重要得多。

**下一步**：按调度台口径回 **B 升 A 主战场**（⑤ 装配 / ④ 联机排在后面，别急着跳）。

## ⑲ 第 14 段：给那条 C 定量判级（**结论：保持 P2，但理由换了**）+ **订正我上一段的一句错话**

### 19·1 现读数据（`cards_raw.json` 1351 张 + `prebuilt_decks*.json` 181 套）
| 问题 | 答案 |
|---|---|
| 附魔类 buff 一共多少 | `buffType 12/15/16` = **132 条** |
| 其中带非零载荷的 | **37 条 / 33 张卡**（载荷字段 = `trait`·`traitValue`·`melee/range/health/armour/cost` Change） |
| 这 33 张在预组里吗 | **14 张出现 50 次**（`BL74` 3 · `UM73` 3 · `SAU53` 1 · `TL47` 2 · 索里塔斯 9 张 40+）⇒ **卡本身每局都会遇到** |

### 19·2 但**缺的那一步不可观察** —— 所以 P2
`ActivateTraitsOnSummonOrEnchantment` 只在附魔授予 **`fast` / `flank` / `ferocity` / `oath`** 时会改变 `canAct/canAttack`。
而全池附魔载荷里的 trait **只有两个**：
- **`1185 = pray`**（15 张索里塔斯）—— 那些卡**卡面本来就印着 `Pray`**（`cardTraits: ['Pray']`，现读）⇒ **重复授予**；
- **`480 = vulnerable`**（`BL74 Spreading Corruption`）—— 卡面文字「Give **Vulnerable 4**」走的是**正常文本 op**（`give`），**不经过附魔层**。

数值类载荷同理：`UM73 +2 Health` / `TL47 -1 cost` 都是**卡面文本**驱动的 op。

⇒ **等级保持 P2**，账里备注已按你的格式写死：
> 「**每局不遇到**，因为：附魔载荷里**没有任何 `fast/flank/ferocity/oath`**；其余载荷要么是**重复授予**（`pray`）、要么由**卡面文本 op** 覆盖。」

### 19·3 🔴 订正我上一段的一句话（铁律 5）
上一段我写：「**我们的附魔层不把附魔卡的关键词并进载体** —— 这解释了「附魔带 `Armour 1` 之类不生效」的整类现象」。
**这句不成立，当场订正**：全池附魔载荷里**没有** `armour` / `melee` / `range` 这类「只靠附魔层才生效」的项（37 条载荷逐条看过）。
⇒ 它是**结构上的缺口**（第五步确实没做），但**不是一个今天能观察到的整类现象** —— 这正是你把「等级按可达性定」这条规矩较真的价值：**我先把「重要」当成了「等级高」**。

### 19·4 顺手发现（新，数据侧）
`SAU53 Undying Legions` 的附魔载荷是 `meleeAttackChange 2 + rangeAttackChange 2`，但**这张卡的卡面**是
「For the rest of this battle, when a friendly troop becomes a Remnant it gains Shield. Give Shield to your Warlord」
—— **对不上**。可能是同一张卡另一条 ability 的载荷被我的扫描按卡归并了（扫描是按卡遍历全部 ability），
也可能是数据本身如此。**未核**，记在这里给数据侧（⛔ 我没有据此改任何东西）。

## ⑳ 第 15 段：按机检工具收紧落点格式（**151/151 通过「声明形态 + 声明名可解析」**）

`typecheck`：**运行时 0 / 编辑器 0**。账 **1,535 行 / 11 列**：**A 151 · B 1,172 · C 65 · D 147**；**A 里 151/151 都写成 `路径:行（声明名）` 且该行是声明**。

### 做了什么
机检工具（新版，比审计那版更严）有两条我原来没满足的规则：
1. **`（声明名）` 必须能在同一文件里按名字找到那份声明** —— 我原来的 `路径:行 ｜ <原散文>` 里散文带 `(...)`，工具会把散文里的名字当声明名去查（`FirstSeatRules` / `necron` …）⇒ 假失败；
2. `)` 之后只能是 `{` / `=>` / `//` / `where` / `:`；`;` 结尾的必须带访问修饰符。

⇒ **全部 151 行统一改写成 `路径:行（声明名） ｜ <原散文（括号换成全角 ［］）>`**，声明名从**那一行的真实声明**里取。
另外**修掉 4 处「行号指向非声明」**：
- `AI.CombinedAttackCanKillWarlord` → `SimpleAI.cs:1237（CanLethal）`（原指向一行式方法，`)` 后接 `; }`）
- `CardScript.ContainsTrait` → `UnitState.cs:938（KwValue）`（原指向一行式属性 `IsStunned`）
- `RawCardScript.OnMinionDeath` → **`EffectResolver.cs:9447（BroadcastWhen）`**（原误写成 `RuleCore.cs`，那份文件里没有这个声明）
- `SupportMethods.IsSpellType` → `WhenEvent.cs:1423（Matches）`（原指到 `RuleCore.DeathTypeOfSource` 里的 `string.Equals` —— **名不对、方法也不对**，已订正）

### ⚠️ 工具本身在改（不是我的账的问题，请转告它的 owner）
收尾那几次运行时，`工具/check_ledger_points.py` **正在被编辑**：
- 一度 `SyntaxError: unterminated string literal (detected at line 254)`；
- 稳定后它报「合计：A 151 · 通过 0 · 漂移 0 · **失败 151**」，但**明细只列 3 条**（汇总与明细不一致）；
- 且它对 `SimpleAI.cs` 的 `public static bool CanLethal(` 报「**文件里找不到这份声明**」（该行确实在 `SimpleAI.cs:1231/1237`）⇒ **它的名字解析当前是坏的**。

⇒ 我用**同一套判法自己实现了一遍**（`.cs` 行不是注释、含 `(`/`=>`/`{`、`)` 后只接 `{`/`=>`/`//`/`where`/`:`、`;` 行须带访问修饰符、`（名）` 必须在该文件里能找到声明）：
**A 151 条 · 失败 0** ✅
工具修好后请再跑一次复核（我不改别人文件；它的 owner 应该已经知道）。

### 20·1 工具修好了之后的复核结果（本条覆盖上面那段「工具坏了」）
工具被我重跑时已经稳定，结果：**A 151 · 通过 137 · 漂移 14 · 失败 0**；
再跑一次 `--fix` 把 14 条漂移行号就地改正（改前已备份到 `/tmp/wf_a/ledger_bak.tsv`），**复跑：A 151 · 通过 151 · 漂移 0 · 失败 0** ✅
账完整性：**1,535 行 / 全 11 列 / A151 · B1,172 · C65 · D147**；`typecheck` 运行时 0 / 编辑器 0。

## ㉑ 第 16 段：**G 审计「建议列」的语义复核**（C 实证的那类坑）

> C 的实证：G 对「落点文字里有多个符号、被多方法共用」的行，是**取落点文字里的第一个符号**当地名
> ⇒ 例：`CardDeck.CanAddCard` 被建议到 `DeckRules.cs:694 = AddCard()`（**加牌** ≠ **能不能加**）。
> ⚠️ 机检只查形态（那一行是不是声明 + 名字能不能解析），**查不出「指到了另一个方法」**。

### 我的做法（两遍筛）
1. **机筛**：拿「原版方法名」和「指到的声明名」做词元比对（去掉 `card/unit/trait/effect/get/set/add/remove/can/is/has` 这类通名词），
   完全没有交集的行 = **83 条**；
2. **人看**：那 83 条逐条判断「这个声明是不是这个行为的实现」。大多数是**合理的换名**（例：`DestroyUnitSilent → CleanupDeaths`、
   `ResolveSummonUnit → DeployFree`、`IsDefender → IsValidTarget` 里的 `IsDefenderUnit`、`MinionManager.*Slot* → RoomOnSide`），
   **不需要改**。
3. 再补一遍**调度台点名的危险前缀族**（`Can*`/`Add*`/`Remove*`/`Get*`/`Set*`/`Has*`/`Is*` 且词根相近）：7 条，
   逐条看过 —— 全部是同一行为的不同命名（`CanUseAbilityNow→CanUseAbility`、`IsInPlay→IsUnitInPlay`、
   `RemoveDropPod→OpenDropPod`（我们的「开舱」就是「摘掉 dropPod」）、`IsTargetedCard→CardDef.Targeted` …）。

### 结果：**建议列复核：83 条进筛 · 5 条改指**（其余确认无问题）
| # | 原版方法 | 改前（错） | 改后（对） | 为什么错 |
|---|---|---|---|---|
| 1 | `BattleManager.NeedToChooseMinionTarget` | `BattleDriver.cs:7657（SetLastPlayRejectCodeForTest）` | **`BattleDriver.cs:7676（NeedsUnitTargetAfterDeploy）`** | 指到了一个**自检钩子**上（名字里的 `Target` 把它骗过去了） |
| 2 | `PlayerHand.GetHeroExtraCardsCount` | `CardInstance.cs:395（CardInstance）` | **`RuleCore.cs:105（HandMaxFor）`** | 指到了**构造函数**（不是「督军正修正 ⇒ 多抽几张」那一支） |
| 3 | `CardScript.ContainsTrait` | `UnitState.cs:392（KwValue）` | **`UnitState.cs:938（KwValue）`** | 392 是**一行式属性 `Survivor`**（它内部**调用** `KwValue`）⇒ 名字骗过机检；938 才是 `KwValue` 的声明 |
| 4 | `RawCardScript.HasPlayedThirdCardInTurn` | `RuleCore.cs:149（NewBattle）` | **`PlayedThirdCardInTurn.cs:55（FirePlayedThirdCardInTurn）`** | 指到了 **`NewBattle`**（开局）—— 与「本回合已打出第三张」毫无关系 |
| 5 | `SupportMethods.IsSpellType` | `WhenEvent.cs:1423（Matches）` | **`WhenEvent.cs:1599（ActingCardTypeMatches）`** | `Matches` 是通用匹配器；**具体的 cardType 判定**在 `ActingCardTypeMatches` |

🔴 **第 1/2/4 条是「指到了另一个方法」的正例**（C 说的那类），第 3 条是**机检自己也认错行**的例子
（它把一行式属性里**调用的方法名**当成了那份声明 ⇒ 报「漂移：实际在 392」—— 那个漂移是**假**的，⛔ 别照它 `--fix`）。

### 最终状态
`check_ledger_points.py`：**A 151 · 通过 150 · 漂移 1 · 失败 0**
（那 1 条漂移 = `ContainsTrait`，就是上面第 3 条那个**假漂移**；保留 938，不按工具跳到 392。）
账完整性：**1,535 行 / 全 11 列 / A151 · B1,172 · C65 · D147**；`typecheck` 运行时 0 / 编辑器 0。
