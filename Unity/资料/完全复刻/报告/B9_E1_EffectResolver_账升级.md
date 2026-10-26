# B9_E1 — `战斗逻辑驱动` 域 EffectResolver 切片 前 40 行账升级

- 会话：第八十三会话（完全复刻）批次 B9 · 执行代理 `B9_E1`
- 日期：2026-10-26
- 切片命令：`python -I -X utf8 工具/ledger_slice.py --domain 战斗逻辑驱动 --status B --file EffectResolver --limit 0`（共 363 行；本轮做**账行号升序前 40 条** = 物理行 69/74/75/76/80/82 与 128–163）
- 补丁（两批）：
  - `d:/4/Unity/资料/完全复刻/账/_parts/_merged_B9_E1/B9_E1.tsv`（第一批 40 行，**已并入主表**）
  - `d:/4/Unity/资料/完全复刻/账/_parts/B9_E1b.tsv`（第二批 **13 行**，按调度台 2026-10-26 裁定把「自述有顺序差」的 13 条从 A 改回 B；`check_patch_conflicts.py` 通过、`merge_ledger_parts.py --dry` = 13 更新、13 个键在主表唯一命中）

---

## ① 结论（**按调度台 2026-10-26 裁定修正后**的现行值）

| 判定 | 条数 | 为什么 |
|---|---|---|
| **A** | **6** | 判据侧读到方法体，我们侧指到一个**实名函数**，且**净效果与执行顺序都等价**：`GetAverageValue` · `GetTargets`（4 参重载 = 纯转调）· `SetPreviousAbilityTargets` · `AddAutoActionToQueue`（入队原语本身）· `AddCardInPlayToDeck`（**原版也是即时**、不入队）· `AddExecuteSwarm`（**我们真的也入 DelayedInstant 道**、道号照抄） |
| **B** | **28** | 四类：① `Add*` 族里「原版入队 / 我们同步施加」⇒ **已知顺序差**（13 条，见下节）；② 只有**类级命中**或实现散在多点（136/140/144/151）；③ **纯表现层**（口径六：133/138/139/141）；④ 结构不同 + 半个分支没核完（76/80/129/152/159/160/161） |
| **C** | **3** | 找不到对应物、且能说清「差什么」：`GetRefCard`（ref card 通路 + 四档 valueOptions）· `AddFollowResolutionAttackContinuationAction`（结算后追打）· `AddMissAction`（落空计数） |
| **D** | **3** | ① 原版**死代码**（`AddCardToDeck` / `AddInitialCardToDeck` —— 三源 0 调用点）；② 用**别的机制代替**（`AddReassembleMinionsOrder` —— 我们棋盘恒无洞，不需要「重排填空」） |

---

## ①·b Add* 族的 A/B 二分（调度台裁定后）

**裁定口径**（调度台 2026-10-26）：A 要求「行为一致」；`差异` 列里**自述存在先后/顺序不同的**一律 B（⛔ 不许为了保住 A 把「有先后差」改写成「等价」，那是自证）。

### 保持 A —— 3 条，各自为什么等价

| 账行 | 方法 | 为什么「净效果 **与执行顺序** 都等价」 |
|---|---|---|
| 130 | `AddAutoActionToQueue` | 这一行**就是入队原语**（不是「某个动作」）：四条道 + 非法道号 `LogError` 后丢弃 + 道内 FIFO 追加。我们 `Core/ActionQueue.cs:246` 的 `Enqueue` 逐条同形（含非法值出错丢弃、含 `DequeueOrder = 2→1→3→0`）。**它自身不产生「先后」**，先后是消费端的事（那在 `NextActionInQueue` 那一账行）。 |
| 134 | `AddCardInPlayToDeck` | **原版这一行也是即时方法**（不进队列、直接 `List.Insert` + 视图）；我们 `EffectResolver.cs:12884（InsertIntoDeckRandom）` 同样即时，随机位 `GetRandomInt(0, Count+1)` 与 `Count - idx` 的镜像换算逐字对得上 ⇒ 两侧都**没有**排队，不存在先后问题。 |
| 147 | `AddExecuteSwarm` | **我们真的也入队**：`RuleCore.cs:4537` 的 `EnqueueAbility(… ActionPriority.DelayedInstant …)` = 原版 `AddExecuteSwarm(self, right, 3, 1)` 的**同一个道号 3**，且同样在「这张牌打出」那一刻入队 ⇒ **同道、同入队点** ⇒ 与其它入队动作的先后**与模型无关地一致**。 |

### 改判 B —— 13 条，各自差在哪

> 共同差 = 原版**建一条 BattleAction 入队**、由 `Update` 按 `2→1→3→0` 取；我们**在结算途中同步施加** ⇒ 与「其它已入队动作」的先后**不保证**。这 13 条已在 `B9_E1b.tsv` 里把 `差异` 列开头加了一句固定前缀，并各写了自己的额外差。

| 账行 | 方法 | 除了「顺序差」之外，各自还差什么 |
|---|---|---|
| 128 | `AddAttackAction` | 原版入队前有 `SetupAttack`/`ClearPendingDamage(双方)`/`RecordAttackPendingDamage` 三个前置动作 —— 我们**没有待结算伤害池**（伤害逐段现结），那三个动作无对应物；原版 `targetCard == null` 直接抛，我们返错码。 |
| 131 | `AddAutoPlayFromHand` | 原版有一道闸（`!IsInHand && cardState != inDeckDrawing` ⇒ 告警并跳过），我们先把牌放进手牌再打出 ⇒ 前提恒成立、不建这道闸（结构使然）。 |
| 132 | `AddBerzerkAttack` | 原版 `blockPlayerActions = true` 入队；360 那个触发动画是表现层；可达性 = 全池 0 张带 `berzerk`。 |
| 142 | `AddDrawCard` | 原版入队前把**牌库顶那张**（下标 0、`cardState == 0`）标 `SetAsDeckDrawing`（表现态），我们不建；牌序 `G13` 那笔（装载口没做 `Count - idx` 换算）另账。 |
| 143 | `AddDrawSpecificCard` | 同上，只是标记的是**指定那张**。 |
| 145 | `AddEndTurnAction` | 原版入 `globalVars+0xc4` 道；我们 `EndTurn` 直接调用 ⇒ 道号概念不存在。 |
| 148 | `AddFollowAfterResolution` | 原版入队、**等轮到它**才触发 410；我们在「这张战术彻底结算完」那一句直接发 ⇒ 队列里还压着别的动作时次序不同（这一条是**我自己先写出来**的那条顺序差）。 |
| 150 | `AddForceAttack` | 原版 `dontCountAttack` 看实参；我们按卡表 `chosenAbility` 列现算（净效果同、开关来路不同）。 |
| 153 | `AddHandEffect` | 原版 `HandEffect` 多字段，我们只建了「改费用」这一档。 |
| 155 | `AddManaBuff` | 原版入队后由 `ProcessManaBuff` 施加；我们同步施加（三档特殊闸对得上）。 |
| 157 | `AddNewCardToDeck` | 原版「造视图 + 入队」；视图三跳（`CreateCard`/scale/`SetActive`）是表现层，我们没有；`+0x90 == 70` 那个 bool 语义没解清。 |
| 158 | `AddNewCardToHand` | 同上；另原版 `battleMode == 60` 时**不入队**，我们没这道闸（该模式语义没查清）。 |
| 163 | `AddRecallToDeck` | 原版入队后**立即** `SetAsUnitDestroyed`（针对 Minion）；我们 `DoReturn` 摘棋盘时置状态位目的相同但**未逐步对过**。 |

---

## ①·c 「入队层」到底承载了什么（决定它是不是真缺口）

**结论：入队层【已经建好】，不是「原版有、我们没有」的缺口；只差把 `Add*` 那族接进去。**

- `Core/ActionQueue.Enqueue` 的**生产调用点只有一个**：`Core/ActionPump.cs:562`（`EnqueueAbility` 的尾部 `return ctx.ActionQueue.Enqueue(act, lane);`）。另有一处自检夹具 `Editor/RuleEngineTest_Priority.cs:884`。
- 因此「队列里跑什么」完全由 `EnqueueAbility` 的 27 个生产调用点决定，按族分：
  1. **ability / 触发派发**（最大一族，走 `ListenerLane` / `TriggerLane`）：监听者 `EffectResolver.cs:10030` · 常驻 `:10299` · 手牌陷阱 `:10493` · 手牌监听 `:10656` · 部署触发 `:9361` · `attacked` `RuleCore.cs:6628` · `mob-extra` `RuleCore.cs:12460` · `darkpact`（self/ally）`EffectResolver.cs:12140/12164` · 第 3 张牌 `PlayedThirdCardInTurn.cs:81` · `synapse-extra` `EffectResolver.cs:2775`
  2. **回合起止**：单位行 / 手牌行 / 常驻行 `EffectResolver.cs:8662/8690/8697`
  3. **阵营资源**：灵魂石 `EffectResolver.cs:8867/8915` · 信仰 `:9044`
  4. **战术卡正文尾跳**：`EffectResolver.cs:2588`（`tactic-body:`）
  5. **死亡 / 离场结转**：`death-signal` `RuleCore.cs:9630` · `Unstable` `:9737` · `minionDeath` `:9863` · `slay-credit` `:10086` · `死亡结转` `:10769`
  6. **少数真正的「动作」**：限时增益到期 `RuleCore.cs:2321`（DelayedInstant）· **虫群合并 `RuleCore.cs:4537`（DelayedInstant = 原版 `executeSwarm`）** · 回收/复活类 `RuleCore.cs:11818/12370/12460`
- ⇒ **今天队列承载的是「ability/触发 + 5 类结转信号 + 2 个真动作」，不承载 `Add*` 那族的原子动作**（attack / draw / discard / endTurn / recall / createCard / manaBuff / forceAttack …）。

**这条对施工的意义**：`Add*` 那族要升 A，**不是**「先造一个动作队列」（已经有了），而是**逐族把动作接进去**：给 `QueuedAction.Payload` 定结构化载荷 + 一个 `ResolveAction` 式的分派（原版 `ResolveAction` 那一账行就是那张分派表）⇒ 这是**明确的施工项**，不是「原版没有 / 做不到」。

### 🔴 顺手纠正账里两处**已过期**的说法（铁律 5）

1. **账行 82（`SetPreviousAbilityTargets`）**：旧 `差异` 写「我们做的是**并集** ⇒ 真差异」。**已过期** —— 那是 2026-10-24 `A1/E2` 改前「只在出口同步一次」的形态。现读 `Core/EffectResolver.cs:10993（NotePreviousAbilityTargets）` = `Clear()` + `AddRange(并集)`，与原版 `.c:16-31` 的「整表覆盖」**同形** ⇒ 本行 **B → A**。
2. **34 行 Add\* 的旧落点**统一写着「EffectResolver / RuleCore 的同步结算（**我们无动作队列**）」。**这句话是错的** —— `Core/ActionQueue.cs`（四条道 + 非法道号 LogError 丢弃）+ `Core/ActionPump.cs`（`Update` 的泵点等价物）就是那条队列；只是它**今天只承载 ability/触发**（唯一生产入口 `Core/ActionPump.cs:562`）。已在账行 130 的 `备注` 里写死，其余行落点已改。

---

## ② 逐条证据（只列 C 与有疑点的）

### C-1 `AbilityLogic.GetRefCard()`
- 原版：`decomp_full/AbilityLogic__GetRefCard.c:1-38`（🥇 全文）—— 读**静态单例**「上一条能力目标表」（与 `SetPreviousAbilityTargets` 同一个 `DAT_1842c3a28.b8`）：null ⇒ null；`Count < 1` ⇒ null；否则返回 **`[0]`**。
- 调用点（三源）：`grep -rl AbilityLogic__GetRefCard` 全库只有 **2 个文件**（自身 + `AbilityLogic__PlayAbility.c`）⇒ 唯一消费点服务 `ChooseValueOptions` 的 RefCard 四档 **100/101/110/120**。
- 我们：**没有任何 `[0]` 取用**。`ctx.PreviousAbilityTargets` 只被 `Core/CountEval.cs:505-527`（`targetsAffected=150` 分支）**整表遍历**；`grep -rn RefCard` 全仓 2 处**都只是注释**。
- 可达性：`cards_truth.json` 里 `valueOptions=101`（BaseRefCardMAttack）**1 张 = UM118 Indomitus Crusade**。它**已经**在 `UnparsedEffects` 快照里（`RuleEngineTest.cs:35835`）⇒ 这是**出声的缺口，不是静默错**（不违反项目红线）。
- **差什么**：① `PreviousAbilityTargets[0]` 的取用口径（我们那张表只装**场上单位**，原版还能装手牌/牌库卡）；② `valueOptions 100/101/110/120` 四档求值 —— 该落在 `Core/EffectResolver.cs` 的数值求值口 + `Core/EffectText.cs` 的 `equal to its [Attack]` 句型。

### C-2 `BattleManager.AddFollowResolutionAttackContinuationAction`
- 原版：`decomp_full/BattleManager__AddFollowResolutionAttackContinuationAction.c:115-465`（全文 467 行）—— 以 `baseAction` 的 actingCard/targetCard **新建一条 type=1 attack 动作**（带 `+0x40 isPlayer` / `+0x120 attackType` / `local_158 = 1`），再把类型改写成 **85 afterResolutionAttackContinuation**，入 `globalVars+0xb8` 道。
- 我们：**没有「攻击结算完再追打一记」这个动作**（我们的攻击是一次算完 = `RuleCore.DeclareAttack`）。
- **差什么**：见 ③ —— 触发谓词没解清（`.c` 里三段 `baseAction.actionType != 52/67/42` 分支**只做结构体拷贝**，没读出别的语义）。

### C-3 `BattleManager.AddMissAction`
- 原版：`decomp_full/BattleManager__AddMissAction.c:1-80`（全文）—— 建 `type=50 missAction` + 装 targetCard 入队 ⇒ 结算侧记一次「攻击落空」（`MissActions` 计数，与 `FinishAttackActionWithoutMoving` / `UsedActiveAbility` 同族：`Core/RuleCore.cs:6237` 引的 `:13/:20/:26` 三处 +1）。
- 我们：**没有 miss 动作、也没有这个计数器**。攻击打不了只 `return 错误码`（出声在返回值/日志）。
- **差什么**：那笔计数 + 「miss 这个动作本身」。⚠️ 可达性未查（见 ③）。

### 疑点（我**没有**下结论的）

| 账行 | 疑点 | 该谁看 |
|---|---|---|
| 151 `AddForceAttackFriendlyUnits` | 我**没读够** `DoForceAttack` 的目标侧分派（原版 `type=66 forceAttackFriendly` 是独立动作；我们只有一个 `DoForceAttack`）⇒ 「友方」这一档是否覆盖未证 | 下一轮把 `DoForceAttack` 的目标侧读一遍；覆盖了就升 A |
| 152 `AddGoesSecondCardToDeck` | 主路（防御卡 → **后手**手牌）✅ 对得上（`RuleCore.cs:729 GrantDefenceCard` + `:222-224`）；但原版 ② 那一支「`battleMode ∈ {50,80,110,120}` ⇒ 从 `GetEnvEffectCards()` 随机抽一张替换」我们**没建**，且**可达性未查** | 查 `GetEnvEffectCards` 在哪些模式非空 |
| 158 `AddNewCardToHand` | 原版 ④ `battleMode(+0x294) == 60` 时**不入队**（只造视图）；我们**没有这道闸**，`+0x294` 是哪一种玩法未查清 | 查 `+0x294` 的枚举 |
| 157 `AddNewCardToDeck` | 原版 ① `cardType == Tactic && +0x90 == 70` 置一个 bool（面朝下？），**语义没解清** | 查 `+0x90` |
| 149 | 见 C-2 | |
| 156 | 见 C-3 | |
| 136 `AddCardsInHandToList` | 原版有**一个集中的手牌筛选入口**（`GetCardsInHandList`），我们没有 —— 同一套筛（督军排除 / 单位·战术·耳语三档 / 侧别）散在多个 handler 里（`EffectResolver.cs:5791 / 5864 / 8504 / 11070`）⇒ **将来两处口径打架的风险**（未查出今天的可观测差异） | 想收口的话先造 `HandCardsOf(...)` 再逐点替换 |
| 129 `AddAttackContinuationAction` | 「重新入队 ⇒ 与其它已入队动作的先后」我们结构上没有（同步继续）；可达性 = 全池 **0 张**带 `droppod`/`landing` ⇒ 今天零可观测差异 | |
| 160 / 161 `AddPlasmaMana` / `AddPsychicMana` | 净效果（+1 上限）我们有（`DoEnergy`）；但**触发路径**（`ResolveReactor` / `ResolvePsyker` / `OnTurnStart`）我们未建、池内 0 张 ⇒ 今天不可达 | 与 `CardScript.OnTurnStart` 那一账行是同一笔 |

### D 三行的证据（判 D 不许「影响小」当理由，这里都是硬证据）

| 账行 | 判 D 的依据 |
|---|---|
| 135 `AddCardToDeck` | **三源 0 调用点**：`decomp_full` 只有自身文件 / `out_cpp2il_dev_ilspy` 只有定义（`BattleManager.cs:48619`）/ `out_il2csharp` 只有定义（`:27105`）/ `assets_full` **0 处**字符串引用。同形私有副本 `BattleCardManager.AddCardToDeck`（`BattleCardManager.cs:303`）**也 0 调用点**。活的牌库构建走 `BattleCardManager.CreateDeck`（`CreateDeck.c:20-74` 内联同一段 Insert/SetAsDeck/SetActive）⇒ 重构残留。 |
| 154 `AddInitialCardToDeck` | 同上三源 0 调用点（`private`，`BattleManager.cs:48383` 只有定义）。⚠️ **易错点**：它 `deck.Insert(0)`，只看这个文件会得出「初始牌库是倒序的」**错结论**；真值是 `CreateDeck` 的**按序 append**（`CreateDeck.c:60-70`）。 |
| 162 `AddReassembleMinionsOrder` | 原版用它把「每个下标的单位摆回各自的下标」（消洞，9 处调用点）；我们的棋盘**恒无洞**（`Core/BoardSlots.cs` 的 `Insert` 插入推人 / 离场外侧补位，文件头 `:12` 就引着 `MinionManager__ReassembleMinions.c:97,154`）⇒ **用别的机制代替**，净可观测差异 0。 |

---

## ③ 没查清的部分（如实）

1. **`AddFollowResolutionAttackContinuationAction` 的触发谓词**：`.c` 全文读完，但三段 `!= 52/67/42` 分支在 Ghidra 里只剩结构体拷贝（256 字节 ×3 遍），真正的触发条件在调用方 `_ResolveAttack_d__438__MoveNext.c` 那一大段里，本轮没读。
2. **`AddMissAction` 的可达性**：不知道 `PlayAbility` 的哪些分支会调它 ⇒ 这条 C 里有一半是「没查清」（缺口本身是真的：我们没有那个动作也没有计数）。
3. **`PlayAbility` 的 2,878 行主分流**：只读了 `:308/:324`（playersAffected）· `:668`（ShuffleCardList）· 以及「每个效果出口调 `SetPreviousAbilityTargets`」（41 个 case / 48 处 `LAB_18081cc87`）。**整条分流顺序没通读** ⇒ 账行 80 只能留 B。
4. **`GetTargets`（6 参）剩余档**：账行 76 只核过 `ta = 100/105/106/110/113/115` 那几支（B8_P/B8_AE 的成果），其余档没逐条核 ⇒ 仍 B。
5. `AddGoesSecondCardToDeck` 的「环境效果卡随机替换」可达性、`AddNewCardToHand` 的 `battleMode == 60`、`AddNewCardToDeck` 的 `+0x90 == 70` —— 三个字段的语义/可达性都没查（见 ② 疑点表）。
6. `AddForceAttackFriendlyUnits`：`DoForceAttack` 的目标侧分派**没逐句读**（见 ② 疑点表）。

---

## ④ 交接给下一个代理

### 做到哪一行
**物理行 69 → 163（升序前 40 条）全部过了一遍**，第一批补丁 40 行已并入主表（现归档在 `账/_parts/_merged_B9_E1/`），第二批 13 行在 `账/_parts/B9_E1b.tsv`（把「自述有顺序差」的 13 条改回 B）。下一轮从**物理行 164（`AddRecallToHand`）** 起接着做；`--file EffectResolver` 这个切片的剩余 ~323 行仍是同一批。

⚠️ **切片的组成会变**：本轮之后，我改过的行落点里大多已不带 `EffectResolver` 字样（换成 `RuleEngine/Core/RuleCore.cs` / `ActionQueue.cs` / `AiScoring.cs` 等）⇒ 重跑切片命令时**这些行会离开本切片**。想接着做，直接用 `--status B` 不要 `--file`，或按物理行号往下推。

### 已确定的规律（可直接复用，别再重查）
1. **`BattleManager.Add*` 全族形状**：`建 BattleAction(type=<整数> + 字段) → AddAutoActionToQueue(priority)`。**`BattleActionType` 枚举已解**：`tools/cpp2il/out_cpp2il_dev_ilspy/BattleActionType.cs`（0 undefined · 1 attack · 3 drawCard · 7 reorderMinionsInPlay · 11 playAnim · 14 endTurn · 15 addEffect · 23 createHandCard · 25 addHandEffect · 28 discardFromHand · 34 manaBuff · 42 forceAttack · 44 disappearUnit · 46 autoPlayFromHand · 48 createDeckCard · 50 missAction · 51 recallToDeck · 53 attackContinuation · 56 disappearFromHand · 57 displayFromHand · 58 followAfterResolution · 66 forceAttackFriendly · 79 executeSwarm · 85 afterResolutionAttackContinuation）。**先查这张表定类型，再读体**，会快很多。
2. **四道 = 0 Player(+0x350) / 1 Instant(+0x358) / 2 Interrupt(+0x368) / 3 DelayedInstant(+0x360)**；消费序 `2→1→3→0`。道号在 `AddAutoActionToQueue.c` 的 4 个分支里，`globalVars+0xb8` / `+0xc4` 是几个常用的「非默认道」。
3. **`+0x228` = `cardState`（`CardStateOptions`），不是 cardType**；**`+0x40` = isPlayer**；**`+0x120` = 当前攻击型**；**`+0x290` = BattleCardUI**；**`+0x294` = battleMode**。`ManaBuff` 偏移表 → `tools/il2cpp_out/dump.cs:19631-19641`。
4. **牌序铁证链（三条独立证据）**：`BattleCardManager__CreateDeck.c:20-74`（逐张 append + `SetAsDeck(i)` + `SetActive(i == 0)`）· `AddDrawCard.c:62-100`（抽牌前标记**下标 0** 那张）· `AddCardInPlayToDeck.c`（`SetActive(pos == 0)`）⇒ **原版下标 0 = 牌库顶**。我们「列表末尾 = 顶」且在插入处做了 `Count - idx` 换算，但 **`BuildPlayer` 装载口没换算**（`Core/RuleCore.cs:616-618`）⇒ 不洗牌时倒着抽（已知 `G13`）。
5. **三个 0 调用点的 `Add*`**（135 / 154，另 `AddNextReplayAction` 是假 0 —— 见坑 1）：判 D 前**必须三源**（`decomp_full` + `out_cpp2il_dev_ilspy` + `out_il2csharp`，再加 `assets_full` 字符串）。

### 踩到的坑（务必躲）
1. 🔴 **`BattleManager__AddNextReplayAction.c` 那 21 行不是 `AddNextReplayAction`** —— 它只 new 一个匿名类型并返回（「文件名与内容不符」那一类）。真正的体在 **`BattleManager._AddNextReplayAction_d__420__MoveNext.c`**（143 行回放驱动器）。**凡是协程/泛型方法，体永远在 `MoveNext` / `StateMachine` 那份。**
2. 🔴 **`out_cpp2il_dev_ilspy` 对 `BattleManager` 的这一段大量丢体**（`Cpp2ILHelpers.NoteDecompilerIssue` + `Unmanaged memory load` 满屏，结构体拷贝把语义盖住）⇒ 这一族**优先用 `decomp_full` 的 Ghidra `.c`**（那批方法体短、可读，且首行函数名与文件名一致，未发现错位）。
3. ⚠️ **`AbilityLogic__GetTargets.c` 是 6 参重载**（文件头 6 个形参）；4 参重载**没有 Ghidra 产物**，只有 dev_ilspy（`AbilityLogic.cs:712-721`，纯转调）。别把两行账当成同一个体。
4. ⚠️ **Ghidra 有时把结构体字段写成 `*(undefined2 *)(x + 0x14) = 1`** —— 那是**两个 bool 一起写**（`+0x14 = 1`、`+0x15 = 0`），不是「一个 short 字段 = 1」。
5. ⚠️ **`AddPlasmaMana`/`AddPsychicMana` 的 `newMana` 实参只进日志**（`manaAmount` 恒默认 1）⇒ 别照着形参去实现「加 N 点」。
6. ⚠️ **`_ResolveAttack_d__438__MoveNext.c` 里 `AddAttackContinuationAction` 的调用点（:426）在空投舱分支之后** —— 那一段全是 256 字节结构体拷贝，直接 grep 调用点会看不出上下文，要先读 `:288-370` 那段（stealth/ambush/droppod 三支）。

### 本轮的验证
- `python -I -X utf8 工具/check_patch_conflicts.py` ⇒ ✅ 无跨补丁重复键
- `python -I -X utf8 工具/merge_ledger_parts.py --dry` ⇒ 可通过（当时合计 106 更新 = 我的 40 + 同时在飞的另一份补丁）
- 自写校验：第一批 40 行 × 11 列 · 第二批 13 行 × 11 列 · `方法` 列无全角括号 · 两批共 53 个 `(原版类, 方法)` 键在主表**唯一命中** · 第一批当时判 A 的 19 条落点 `文件:行` **逐条回读、行内容都含括号里那个声明名**（改判 B 的 13 条**落点不变**，B 也保留落点，下一轮施工直接用）
- `check_patch_conflicts.py`（1 份在飞补丁）· `merge_ledger_parts.py --dry` ⇒ **13 更新 / 0 新增**
- ⛔ 本轮**未跑 Unity、未改任何 `.cs`、未动主账表、未动 git**
