# W · 战斗线：串行与阻塞（10 条系统性偏差 · 第 1、2 条）· 2026-10-19

> 写手：战斗线 · 范围 = `R_战斗表现链路对拍.md` 的**偏差 1「结算永远领先表现」** 与
> **偏差 2「没有阻塞/串行」**。
> 状态：代码 + 断言已落地；`typecheck` **0 / 0**。⛔ 未跑 Unity / `*.Run` / player（按现行口径）。
> 文件所有权：只动 `Battle/**` 与 `Editor/BattleScene.cs`；`RuleEngine/**` / `Core/**` / `Hand/**` 一个字节没改。

---

## 一、改了什么（文件:行）

### 1·1 第 2 条：`WaitingForBlockingAction` 等价闸（新的一等状态）

`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs`

| 位置 | 内容 |
|---|---|
| `:12283-12317` | 字段与只读口：`_blocking` / `_blockingQuietAt` / `_blockingSince` / `BlockingMaxSeconds = 20f` · `public bool WaitingForBlockingAction` · `PendingHpForTest` / `PendingHpCountForTest` |
| `:12447` | `BeginBlockingWindow(float quietAt)` —— 由 `PlaySignals` 在排完一批事件后调用；`quietAt` = 这一批**最后一条 cue 的收尾时刻**（`t` 的终值）；每批都重置安全阀起点（它量「卡住多久」，不是「动作链多长」） |
| `:12459` | `TickBlockingWindow()` —— 由 `AdvanceTimeline` 每帧末尾调用；**队列空 ∧ `_clock ≥ quietAt`** 才放行；超 20 s 安全阀**出声并放行**（不静默锁死） |
| `:12083` | `PlaySignals` 末尾：`if (_signalBuf.Count > 0) BeginBlockingWindow(t);`（必须赶在 `AdvanceTimeline(0f)` 之前） |
| `:12342` + `:12424` | `AdvanceTimeline` 末尾：`TickBlockingWindow();` |
| `:12554` | `DropSignals()` 一并清闸、清账 |
| `:3283` 区 | `Begin()` 跨局清闸、清账（新一局开局不会被上一局的闸锁住 / 血账串局） |

**五个消费点（按原版逐处接线）**：

| 原版判据 | 我们对应 | 位置 |
|---|---|---|
| `BattleManager__CanPlayCard.c:72` | `interaction.CanDropAtSlot`（= 唯一的落点合法性委托）→ `if (WaitingForBlockingAction) return false;` | `BattleDriver.cs:3481` |
| `BattleManager__EndTurnClick.c:47`（闸在播 cue **之前**，挡下时**静默**） | `EndPlayerTurn(bool fromUi = false)` 顶上 `if (fromUi && WaitingForBlockingAction) return;`；**玩家点击**与**超时**两条路都传 `fromUi: true` | `:9850`（方法头 `:9846`）· 点击 `:8945` · 超时 `:8816` |
| `BattleManager__ExecuteActiveAbility.c:39` | `CommitCommand(AttackKind, bool fromUi = false)` 顶上同一条闸；玩家在选择器上悬停选中那一处传 `fromUi: true` | `:9217` · 调用点 `:8890` |
| `BattleManager__Update.c:1033/1060` | `Resolve(AttackKind, int, bool fromUi = false)` 顶上同一条闸；玩家点目标那一处传 `fromUi: true` | `:9580` · 调用点 `:8956` |
| 原版动作队列「一条动作演完才轮到下一条」（AI 同一条队列） | `DriveAiTurn` 里两处（教程脚本那支 + 常规那支）`if (WaitingForBlockingAction) return;` | `:11618` · `:11647` |

**被闸挡下的那一下不出声**（照原版 `CanPlayCard` 的 `return 0`）：
`OnIllegalAction`（`SpeakCantDo`）与 `OnCardReturned`（提示条「拖到目标上」）都在 `:4578` / `:8232` 处
`if (WaitingForBlockingAction) return;` —— 免得把「演出没演完」误报成「拖错了」。

### 1·2 第 1 条：血量「可见时刻」（等价的可等待流水线）

仍走原来的事件表，但**状态不再在同一帧跳到终值**：

| 位置 | 内容 |
|---|---|
| `:12287-12299` | `_pendingHp`（键 = `(owner<<8)|slot`）：引擎**已扣、表现还没演到**的血量差 |
| `:12497` | `PlaySignals` 排每一条事件时 `NotePendingHp(e)`（调用点 `:12067`）：`Hit` 且 `Amount ≠ 0` ⇒ 账上 `+Amount`（`Amount` 是 `ApplyDamage` **扣血前**发的 `actual`，见 `RuleCore.EmitHit`） |
| `:13749` / `:13759` | `SyncBoard` 两条建/刷视图的路都改走 `ToBoardCardData(u, owner, slot)` = `ToCardData` + 账上的差值 ⇒ **显示旧血** |
| `:12541` | `ToBoardCardData`（场上卡唯一的一份；⛔ 别在别处再拼） |
| `:12514` | `CompletePendingHpBeat(e)`：事件**轮到播的那一刻**从账上减掉并重刷那一格 |
| `PlaySignal` 顶部（`beat` 分支之后，`:12929`） | `CompletePendingHpBeat(e)` —— 在 `PlayFeel` / 飘字**之前**（原版也是先改 HP 再播受击动效） |
| `:12528` | `RefreshUnitData(owner, slot)` —— 只刷「引擎里还站着的那个人」的卡面，不新建视图 |
| `:13711` | `SyncBoard` 的「这一格空了」分支顺手 `_pendingHp.Remove(...)`（防旧账留给下一个主人） |

**边界（如实记）**：
- **阵亡单位不打账**（`Board[slot]` 已是 null；消散走 `PendingExitFor`，血条随消散走）。
- **身份守卫**：`e.CardId`（= `UnitState.Name`，与 `EmitUnit` 同源）与「排时间线那一刻棋盘上那个人」不符 ⇒ **不记账**
  （宁可早跳，也不把别人的血算错）。**已知漏网** = 同名单位互换格号（本账没有实例 id 可用）→ §五。
- 治疗（`Amount < 0`）走同一条账，方向自动正确。

### 1·3 测试口（只新增，不改既有语义）

- `SimulateEndTurnFromUi()`（`BattleDriver.cs:18375`）= 玩家那条路（过闸）；`SimulateEndTurn()` 原样（不过闸）。
- `WaitingForBlockingAction` / `PendingHpForTest` / `PendingHpCountForTest`（只读）。

### 1·4 断言

`MyGame/Assets/CardPresentation/Editor/BattleScene.cs:23637-23786`（新段，在 `DestroyImmediate(driver)` 之前；
自己 `Begin` 一局新对局 = 同 `A515` 的做法）。断言清单与灭自证见 §三。

### 1·5 既有自检夹具的两处**补推**（不是改判据，必须记账）

闸落地后，**批处理里不推帧**的地方会把闸一直挂着 ⇒ 两处「走玩家 UI 那条路」的既有用例会假红。
两处都只加「把开局/上一批演出排空」，**判据一个没动**：

| 位置 | 为什么 | 加了什么 |
|---|---|---|
| `BattleScene.cs:19786` 一带（A462 段开局） | `driver.Begin` 之后 `DrivePlayerTurnForTest` 会被 `fromUi` 闸挡下（该段不推帧） | `StepThrough(driver)` + 推到 `!WaitingForBlockingAction`（400 帧上限） |
| `BattleScene.cs:11211` 一带（§13 回合时钟） | 超时结束回合走 `EndTurnClick(true)`，同样过闸；该节只 `TickClockForTest`、不推事件时间线 | 同上（**不推** `TickClock` ⇒ `ClockLeft` 那一串断言不受影响） |
| `BattleScene.cs:11029`（§10 战术卡内联用例） | 拖拽前不推帧 ⇒ `CanDropAtSlot` 被闸判「引擎拒绝」，卡根本没打出去 | 同上（复跑红 #2–5 的修） |
| `BattleScene.cs:26482`（`DriveTacticAndCheck`，造牌/部署两节共用） | 同一根因（复跑红 #6–9 的修） | 同上 |
| `BattleScene.cs:21329` 一带（教程 ⑬「脚本接管 AI 回合」） | `DriveAiTurn` 之前不推帧 ⇒ 脚本一条都不推（复跑红 #10–12） | 局部 `AiStep()`：每步先 `StepThrough` + 推到放行，再 `DriveAiTurnForTest()` |

---

## 二、与原版判据的对应

| 原版（`d:/2/tools/decomp_full/`） | 原版行为 | 我们的等价物 | 与原件的关系 |
|---|---|---|---|
| `BattleManager__WaitingForBlockingAction.c` | 扫 `+0x350/+0x358/+0x368` 三张动作表，**还有阻塞型动作 ⇒ 真** | 事件时间线没排空 **或** 最后一条 cue 的时长没走完 ⇒ 真 | **等价物**：我们没有动作队列（引擎同步结算），把「队列里还有动作」换成「时间线上还有演出」。⚠️ 只看 `_timeline.Count == 0` 不够（弹出来只是起播，补间还在跑）⇒ 再等 `EventTiming.DurationOf` |
| `BattleManager__CanPlayCard.c:72` | `WaitingForBlockingAction` ⇒ `return 0`（**无提示**） | `CanDropAtSlot` 拒绝 + 两个回调静默 | 逐处照抄；`RuleCore.CanPlayCard` 本身**没改**（不在所有权内，也不需要） |
| `BattleManager__EndTurnClick.c:45-48` | 闸在 `0x245==0 && 0x244!=0 && !WaitingForBlockingAction` 的 `if` 里；**cue 在闸内** ⇒ 挡下时不出声 | `EndPlayerTurn(fromUi)` 在**任何副作用之前** return | 逐处照抄；超时支（原版 `EndTurnClick(true)`）同样过闸 ⇒ 我们的超时现在是「每帧重试、放行时才播 `TimerExpired`、才结束回合」 |
| `BattleManager__ExecuteActiveAbility.c:39` | 同一条闸 | `CommitCommand(fromUi)` | 逐处照抄 |
| `BattleManager__Update.c:1033/1060` | 「确认主动技目标」「确认攻击目标」两道 | `Resolve(fromUi)` | 逐处照抄；被挡时返回 `RuleCodes.ErrNoAction`（原版无返回码，玩家那条调用点不读它） |
| `_ResolveMultiDamage_d__456__MoveNext.c:136-168` | 逐目标 `ReceiveDamage` 时才改 HP | 受击事件轮到播的那一刻 `CompletePendingHpBeat` 重刷卡面 | **等价物**：引擎仍同步算完（引擎侧「挂起/恢复结算」不在本线），但**可见状态**等事件表。⛔ 时长常量一个没改 |
| `_ResolvePlayCardFromHand_d__447:673` 等 | 出牌/攻击/技能都是协程「一步一步」 | 时间线串行 + 五处输入闸 + AI 逐步 | 偏差 3（新事件插队）/ 4（三条 cue 提前播）/ 8（AI 无落位演出）仍开着 —— 在串行骨架之上逐条对齐 |

---

## 三、断言（含**灭自证**）

段名：`★ W0`（前提）/ `★ W1`（血量可见时刻）/ `★ W2`（闸）。全部在 `BattleScene.Run` 的 W 段（**约 23 条 `Check`**）。

**W0 前提**（不摆对就不让后面的断言空跑）：
- 进段时 `!driver.WaitingForBlockingAction`，且时间线为空；
- 现在是我方回合；我方督军视图在场上；视图血量 = 引擎值。

**W1 血量可见时刻**（夹具 = 先 `Emit(Trigger)` 再把 Hit 推后 `TriggerLen = 0.2 s`，然后 `RuleCore.ApplyDamage`）：
1. 引擎已扣血（`wdmg > 0`）；
2. **同一帧**卡面仍是旧血 `whp0`；
3. 账上 = `wdmg`；
4. **灭自证**：卡面 `whp0` ≠ 引擎终值 `whp1` —— 两个候选**真的分得开**（否则 dmg = 0 时「旧血」与「终值」长得一样）；
5. `Step(0.05)`：仍是旧血、账还在；
6. `Step(0.2)`：卡面跳到 `whp1`、账清 0、时间线空；
7. 放行之后卡面仍 = `whp1`（没有被二次刷回去）。
- 🧨 改坏法都写在断言文案里：`SyncBoard` 换回 `ToCardData` ⇒ 2 红；删 `CompletePendingHpBeat` ⇒ 6 红。

**W2 闸**：
1. 结算一落地 `WaitingForBlockingAction == 真`，`TimelinePending ≥ 1`；
2. **队列空但受击动画还在跑 ⇒ 仍拦着**（这条钉死「只看队列空」那个错实现）；
3. **出牌闸**：`interaction.CanDropAtSlot(slot, card)` 在拦着时 **false**；
   **灭自证**：**同一个调用、同一组实参**在放行之后 **true**（闸是活的，不是恒拒）；
4. **结束回合闸**：`SimulateEndTurnFromUi()` 在拦着时**不改** `Active` / `Turn`；
   **灭自证**：放行之后**同一个口**真的结束回合（`Active` 翻转）；
5. 自然放行 = 循环 `Step` 到 `!WaitingForBlockingAction`（400 帧上限 ≈ 13 s < 安全阀 20 s）——
   🧨 删掉 `TickBlockingWindow` 的放行那行 ⇒ 本条红。

---

## 四、需要主对话复跑的宿主

| 宿主 | 为什么 | 备注 |
|---|---|---|
| **`BattleScene.Run`** | **必修**：新断言全在这里；且 `CanDropAtSlot` 是生产拖拽路径（自检里有两处**真拖**：`BattleScene.cs:4031` / `:9117`） | 本会话没跑（按现行口径）。若那两处真拖恰好在某段动作演出未收尾时执行，会被闸挡下 ⇒ 报「拖上场失败」。真撞上就把那两处前加 `StepThrough(driver)` / 推闸放行，**⛔ 不要拆闸** |
| `NetBattleTest.Run` / `NetSelfTest.Run` | `BattleDriver.cs` 是共用件，两个宿主里有**裸 `BattleDriver` 探针**（`NetBattleTest.cs:516` / `:716`） | 各 15 s；建议顺手跑（改了 `Begin` 的复位段） |
| 其余宿主 | 不涉及 `BattleDriver` 的战斗路径（`CardBaseDemo` 只用 `CardView`） | 可跑可不跑 |

`typecheck`（本会话已跑，`TMPDIR=/tmp/wf_battle`）：**运行时 0 / 编辑器 0**。

---

## 五、没查到 / 风险 / 下一步

### 5·1 判据没查到的（⛔ 没猜）
- **`WaitingForBlockingAction` 的逐动作表判据**（`+0x350` 里哪些 kind 算「阻塞」）：伪码只给了「动作表里有没有 kind == 0x1b」「某个 bool」。我们按**等价物**（时间线未演完）落地，**没有**逐 kind 复刻。若将来要 1:1，需要回读 `BattleAction` 的 kind 枚举与那三张表的写入点。
- **超时支在阻塞期间的重试语义**：原版 `ClockManager.EndCountdown` 播 `TimerExpired` → `EndTurnClick(1)`，被闸拒之后**下一帧还会不会重试**，伪码没直接给出（`ClockManager.Update` 的状态位没读全）。我们的实现 = 每帧重试、**放行那一刻才播 cue**（避免 cue 每帧响）—— 这是**我们挑的**，已如实记在 `TickClock` 的注释里。

### 5·2 已知边界（本步没做，账要留）
1. **出牌前的 0.55 s 飞行动画不在闸内**：`DeploySequence` 在 `CardInteraction`（`Hand/**`，不在所有权）里跑，落位回调 `OnDeployed` 才进引擎 ⇒ 那 0.55 s 内玩家仍能再抓起一张牌。原版从起手就串行。**接口需求**：`CardInteraction` 需要在 `DeploySequence.Play` 开始时给驱动一个「起手」回调（或 `Frozen` 的第二种语义）。
2. **抓牌本身没挡**（只挡落点）：`CardInteraction.Update` 的 `Frozen` 仍只接 `PlayerInputFrozen`（`A915`）；把阻塞并进去会打印**错误文案**（「对手掉线/等重连」），而那个文件不在所有权内。当前做法 = 落点拒绝 + `OnIllegalAction` 静默。
   ⚠️ **2026-10-19 复跑红 #1 订正**：`OnCardReturned`（「拖到目标上」那句提示）**不能**按闸吞掉 —— 它是「这一拖没落成」的**通用**出口（真·拖空也走它），吞了会把合法提示清掉（自检当场红）。现在的代价 = **被闸挡下的那一拖可能顺带显示这句提示**；根治同样需要 `CardInteraction` 把 `DropRejectKind` 带出来。
3. **同名单位换格**：`Hit` 事件只带 `Slot` + `CardId`（= 卡名），不带实例 id ⇒ 同名互换格号时账可能挂在错的人身上（会显示「旧血」的那个是别人）。**接口需求**：引擎事件补一个 `InstanceId`（`UnitState.Instance` 已有，只是没进 `BattleEvent`）。
4. **触发产物先出现在棋盘**（偏差 1 的另一半）：召唤物/残骸是 `SyncBoard` 当场建视图的，没等 `Deploy`/`Summon` 那一拍。这是第 1 条**剩下的那一半**，不在本步（会牵动 `SyncBoard` 的建视图路径与 `PendingExitFor` 同族逻辑）。
5. **HUD 上的骷髅分 / BGM 档**仍读引擎终值（`_foeWarlordMinHp` 等），比卡面早一拍。不是血量数字，没动。
6. **偏差 3/4/5/6/7/8/9/10 未动**（按任务只做第 1、2 条）。其中偏差 3（新事件插队）在闸落地后**自动减轻**（新动作在闸放行前进不来），但 `PlaySignals` 用 `t = _clock` 这一点没改。
7. **AI 的闸没有自检**：批处理里 `DriveAiTurn` 需要真帧循环（`Time.deltaTime`），本次只做了代码 + 注释，没有测试口。若要钉死，需要给 `BattleDriver` 加 `DriveAiTurnForTest()` + 可控 `_aiTimer`（**另开一小笔**）。

### 5·3 风险（给复跑的人）

- **`BattleScene.Run` 里两处「真拖」**（`:4031` / `:9117`）如果落在某段演出未收尾的时刻，会被新闸挡下 —— 优先怀疑这里，处置见 §四。
- **`A462` 的 `DrivePlayerTurnForTest()`**（`:20094` / `:20108` / `:20151` / `:20157`）会经过 `fromUi: true` 的闸 —— **已在那一节的 `Begin` 之后补了「推到放行」**（见 §1·5），所以预期不会拦；若仍红，先查是不是那一段前面又有动作没演完，⛔ 别把闸从 `fromUi` 那条路上摘掉。
- 闸的**安全阀 20 s** 是工程决定（原版无此阀），目的是「回放长时间暂停 / 事件表 bug 时不许锁死玩家」；超时会 `LogWarning`（不静默）。

### 5·4 下一步建议（按原版偏差顺序）

1. 把「出牌起手」接进闸（需要 `Hand/CardInteraction` 一个起手回调 —— 见 5·2 第 1 条）。
2. 事件补 `InstanceId`（引擎线 · 小改 `BattleEvent` + `EmitUnit`）⇒ 关掉 5·2 第 3 条。
3. 第 1 条的「触发产物可见时刻」（5·2 第 4 条）。
4. 偏差 3/4（`PlaySignals` 的 `t = _clock` 与三条提前播的 cue）—— 有了闸之后，这两条是**下一层**的节奏问题。

---

## 六、2026-10-19 复跑 13 红 · 逐条定性（主对话跑 `BattleScene.Run`：3290 过 / 13 败）

> 结论：**12 条是夹具（批处理不推帧）+ 1 条是我加的守卫过宽（实现缺陷）**；
> **闸本身没拆、第 1/2 条没回滚**。13 条红已从 `d:/4/_tmp_view/battle.log` **逐条现核**（`✗` 行名单位）。

| # | 红（日志行） | 定性 | 处置 |
|---|---|---|---|
| 1 | `:47207` 拖空松手提示条为空（期望「拖动到有效目标」） | 🔴 **实现缺陷（我加宽了守卫）**：`OnCardReturned` 是「这一拖没落成」的**通用**出口（真·拖空也走它），按 `WaitingForBlockingAction` 吞掉会把合法提示一起清掉 | **删掉** `BattleDriver.OnCardReturned` 里的闸守卫（保留 `OnIllegalAction` 的）；提示条恢复。残余 = 被闸挡下的那一拖**可能**顺带显示这句提示（记 §5·2 第 2 条） |
| 2–5 | `:115169/:115181/:115193/:115205` 战术卡离开手牌 7→7 · 扣费 4→4 · 弃牌堆 · 目标挨打 21→21 | 🟡 **夹具**：§10 内联战术用例在拖拽前不推帧 ⇒ `CanDropAtSlot` 被闸判「引擎拒绝」，卡根本没打出去（四条是同一根因）。🔴 **日志实锤**：`落点不合法（命中不了格位）[MissedSlot]` —— 因为 `DropReject` 对战术卡先试敌方半场、`CanDropAtSlot` 被闸挡后**落到己方半场的 `TryResolveSlot`**、那边当然命中不了 ⇒ 表面现象是「拖空」，根因仍是闸 | `BattleScene.cs:11029` 拖拽前 `StepThrough(driver)` + 推到 `!WaitingForBlockingAction` |
| 6–7 | `:121577/:121591` 造牌手牌数被上限夹住 · 3 张战斗药剂都造出来 | 🟡 **夹具**：同一个缺口，出在共用助手 `DriveTacticAndCheck` | `BattleScene.cs:26482` 同一段补推（造牌/部署两节共用） |
| 8–9 | `:128173/:128187` 部署 2 个墓刃战机 · 真占 2 个新格位 | 🟡 **夹具**：同 #6–7（`DriveTacticAndCheck`） | 同上 |
| 10–12 | `:277062/:277074/:277086` 脚本接管 AI 回合 · 推到第 3 条回合没变 · 4 条跑完仍在 AI 回合 | 🟡 **夹具**：`DriveAiTurnForTest` 之前不推帧 ⇒ 闸让 `DriveAiTurn` 一条脚本都不推（生产里 `Update` 每帧先推 `AdvanceTimeline`） | `BattleScene.cs:21329` 一带加局部 `AiStep()`：**每步**先 `StepThrough` + 推到放行，再 `DriveAiTurnForTest()` |
| 13 | `:277098` 脚本跑完 ⇒ 那一支自己交回合并推进（回合 2、指针 0 归零） | 🟡 **夹具**：同 #10–12（最后那一步 `DriveAiTurn` 之前没推帧） | 由 #10–12 的 `AiStep()` 一并覆盖 |

**改动文件**（本轮回执）：`Battle/BattleDriver.cs`（只删 `OnCardReturned` 的守卫 + 注释订正）·
`Editor/BattleScene.cs`（三处测试夹具补推）· 本报告。
`typecheck`（`TMPDIR=/tmp/wf_battle bash d:/4/Unity/工具/typecheck.sh`）：**运行时 0 / 编辑器 0**。
⛔ 未跑 Unity / `*.Run`（由主对话统一复跑）。
