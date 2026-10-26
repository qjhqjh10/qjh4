# B9 · P-01 · `RuleCore.cs` 18 条（执行代理 `E11RC` 报告）

> 执行代理 **E11RC** · 2026-10-26（第八十四会话「完全复刻」）
> 活：派活包 `判据/B9_剩余C_派活包.md` §2「P-01 `RuleCore.cs` · 18 条」剩余部分
> 独占文件：`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（**只改这一个 `.cs`**）
> 纪律：⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改正本 · ⛔ 未改主账表（只写 `账/_parts/B9_E11RC.tsv`）· ⛔ 未碰任何白名单外文件
> 前置事实：`RuleCore.cs` 已被 `E1P01`（`+161/−56`）与 `E9ER`（`+9`）改过且已合并 ⇒ 本笔**只加不移**，新增成员一律**追加在类尾**

---

## ① 结论

### 1·1 一句话

18 条**逐条定位**后：**3 条早已是 A**（716/724/840 里 716 已 A、724/840 已 B）· **2 条按调度台裁定判 D 不做**（203/217）· **本笔落 9 条代码/机制**（841·850·818·590·354·440·1373·805·157 复判）· **2 条复核后不动**（845·619）· **2 条单文件内做不了**（637·805 的入队点）。

### 1·2 逐条清单（18 条全覆盖）

| # | 原版类 · 方法 | 账里原状态 | 本笔结论 | 证据 / 落点 |
|---|---|---|---|---|
| 157 | `BattleManager.AddResetActions` | C | **→ B（复核后不改代码）** | 原版这条排进 `VarsGlobal.instantPriority`（`+0xB8`）道、与它前面的 `AddScriptedAttack` **同道**；我们的攻击是**同步**的 ⇒ 现形状（攻击结算完当场退还）正好落在「退还先于攻击自己排的触发」那一格。**改成入队反而更远**（见 §2·157） |
| 203 | `BattleManager.BroadcastCardCreated` | D | **D（不做）** | 调度台日志 `:4930-4931` 裁定：池内 0 张可达，只加发送口 = 死代码，须三处同批 ⇒ 本批不做。**复核**：`cards_engine.json` 1,128 张 `abilityTriggers` 含 210 = **0 张**（与裁定一致） |
| 217 | `BattleManager.BroadcastTrapResolved` | D | **D（不做）** | 同上（270 = 0 张） |
| 354 | `BattleManager.GetGameState` | C | **→ B** | 新建 `RuleCore.GetGameState`（8 参成对状态串，纯函数）；格式串二选一未定、子串形状自定、今天零调用方 ⇒ 见 §2·354 |
| 440 | `BattleManager.LogActivatedTrigger` | C | **→ B（引擎侧早已在）** | 2026-10-25（`X2`）把触发计数收在 `BattleContext.Emit` **单口**（`BattleContext.cs:1866` → `:1900 Tally.AddTrigger`）；缺的只是 challenge 日志那层**实体**（**P2**） |
| 590 | `BattleManager.SetupWarlordsLifeAndEnchantment` | C | **→ B（② 半建了、休眠）** | `ApplyWarlordLifeOverride`（`RuleCore.cs:14675`）+ 注入口 `SetWarlordLifeOverride`（`:14666`）；①② 顺序照原版；数值来源 = 远端 match data（P3）⇒ 今天零调用方 |
| 619 | `BattleManager.TryUsingWaystone` | C | **→ B（引擎侧已齐）** | `CanCollectWaystone`（`:12215`）的 ② 回合闸 + ④ `IsAeldariRemnant`（阵营口径）；③ 在 `BattleDriver`；**唯一剩件 = 夹具阵营**（`RuleEngineTest.cs:4062` 写死 `"Test"` ⇒ 要改 `SaimHann`，不在本笔） |
| 637 | `BattleManagerSupport.BroadcastRemoveFromGame` | C | **C（单文件做不了）** | 需要「移出游戏」状态（`UnitState`/`CardInstance`）+ 完整 extrinsic 账 ⇒ 见 §4·③ |
| 716 | `CardScript.CheckForSkullEarned` | A | **A（无需动）** | 今日 `E9` 已在血量写点挂骷髅检查（`EffectResolver` 的 `SkullAux`）；本笔只复核，未改 |
| 724 | `CardScript.ClearPendingDamage` | B | **B（跳过）** | 派活包 §2 已写明跳过 |
| 738 | `CardScript.EnoughPendingHealingToFull` | B | **→ B（谓词建出来了）** | `RuleCore.cs:14533`；**消费者还差一行**（`ConditionEval.cs:231` 的 `case 40`，属 F3） |
| 805 | `CardScript.MissActions` | C | **→ B（动作体建了，入队点未接）** | `RuleCore.cs:14853`；入队点在 `EffectResolver`（不在本笔） |
| 818 | `CardScript.OnTurnStarted` | C | **→ B（三跳只落棋盘那一跳）** | `BroadcastTurnStarted`（`:14625`）+ 闸 `HasTurnStartedTrigger`（`:14615`）；手牌两跳未建模，池内 0 张 |
| 840 | `CardScript.RecordPendingDamage` | B | **B（跳过）** | 派活包已判 B |
| 841 | `CardScript.RecordPendingHealing` | C | **→ B（粘性累计表已建）** | `RecordPendingHealing`（`:14514`）+ 登记点 `HealUnit`（`:9181`）；**永不清理**照原版 |
| 845 | `CardScript.RemoveAttackThisTurn` | C（等级「中」） | **→ B（复核：逐字同形）** | `RuleCore.cs:14367` 与 `.c:5-7` 一字不差（`> 0` 才减、不穿零）；剩余是**观测缺口**（见 §2·845） |
| 850 | `CardScript.RemoveCostEffects` | C | **→ B（落了一处调用点 + 摘记录口）** | `RemoveCostEffects`（`:14571`）+ 调用点 `SweepEphemeral`（`:2877`）；弃牌那条在 `EffectResolver`（见 §4·②） |
| 1373 | `EnergySpentSignal.Raise` | C | **→ B（累计 + 取数口）** | `RecordEnergySpent`（`:14803`）· `SpendNormalMana`（`:12925`，维护费传 `false`）· `EnergySpentTotal`（`:14827`） |

> 合计 **18**：**A 1**（716）· **B 6**（724/738/840 原判 + 845/440/619 复核后）· **本笔 C→B 9**（157·354·590·805·818·841·850·1373 + 738/841 一对其算两条已含在内，见下）· **D 2**（203/217）· **仍 C 1**（637）。
> ⚠️ 更精确按「本笔是否动了代码」分：**动代码 7 条**（354·590·805·818·841·850·1373）· **只复核/只改账 9 条**（157·440·619·845·716·724·738·840 + 637 的复核）· **判 D 2 条**（203/217）。

### 1·3 零回归（A/B 隔离实测，见 §5）

**把本笔新增的 5 个调用点全部摘掉再跑 `play check`，结算指纹与带改动时【逐字节相同】**（`1b40f42ab010e623e908fcd4a236205f`）⇒ **本笔对全池 1,128 张的结算零影响**。当前 `play check` 报的 **1 张主档 + 3 张电池**差异**全部不是本笔的**（归因见 §5·3）。

---

## ② 关键裁定（每条都带判据）

### 2·157 `AddResetActions`：**⛔ 别照施工单①改成入队**

- 原版把这条动作排进 **`globalVars + 0xb8`** 那道：`_ResolveForceAttack_d__504__MoveNext.c:69` 的 `AddScriptedAttack(…, *(undefined4 *)(globalVars + 0xb8), …)` 与 `:77` 的 `AddResetActions(…, *(undefined4 *)(globalVars + 0xb8), …)` **同一格**；
- 而 `globalVars + 0xb8` = **`VarsGlobal.instantPriority`**（`d:/2/tools/il2cpp_out/dump.cs` 的 `VarsGlobal` 字段表：`instantPriority; // 0xB8`），`AddAutoActionToQueue` 的分道表 `0→Player / 1→Instant / 2→Interrupt / 3→DelayedInstant` ⇒ 这条落在 **Instant(1)** 道，**紧随攻击之后**（同道 FIFO）。
- ⇒ 原版出队序 = **攻击 → 退还(ResetActions) → 攻击结算里自己排进去的那批触发**（只有 Interrupt(2) 道的触发例外，它们排在退还前）。
- 我们的引擎**攻击是同步的**（触发在攻击体内 `EnqueueAbility` 入队，泵点在外层 `ActionScope` 退出时才跑）⇒ 现在「攻击 `DeclareAttackCore` 返回 OK 后**当场**退还」**正好**落在「退还先于那些触发」那一格。
- 若照施工单改成 `EnqueueAbility(…, Instant, …)`：退还**排在那批触发之后**（因为触发先入队）⇒ **离原版更远**。
- 剩余两条真差异（都不可观测，已写进账的「差异」列）：① 原版即使攻击在出队时才失败、退还照跑，我们要求 `code == OK`；② Interrupt(2) 道触发的相对次序相反。

### 2·845 `RemoveAttackThisTurn`：逐字同形，缺口在**观测**不在实现

`RuleCore.RemoveAttackThisTurn`（`:14367`）= `if (u.AttacksThisTurn > 0) u.AttacksThisTurn--;`，与 `CardScript__RemoveAttackThisTurn.c:5-7`（全文 10 行）**逐字一致**；零/负边界天然对（我们这边 `AttacksThisTurn` 是 `int`、`ResetActions` 那半走 `RecomputeActAndAttack` 只写 `canAct/canAttack` 两位、不碰计数）。剩余 = `AttacksThisTurn`/`Exhausted` **不进任何探针指纹**、探针又不装载 `ForcedAttackRefundMap` ⇒ 升 A 缺读数口，不是缺实现。

### 2·805 `MissActions`：**只 +1、不重算**

`CardScript__MissActions.c` 全文 33 行逐句：① 无条件 `CustomDebug.Log`；② `IsUnit` 时 `cardState ∉ {2,3,15,17}` 先 `LogWarning`，然后 **`attacksThisTurn += 1`（无条件，不在场也加）**；③ 非单位只有日志。
🔴 **它不调 `ActivateMinion`** ⇒ 我们的 `MissActions` 同样**只写计数**，⛔ 没顺手加 `RecomputeActAndAttack`（加了会让「空放」当场改变能动/能攻击两位，原版不会）。

### 2·354 `GetGameState`：格式串**二选一未定**（如实标着）

`.c` 全文 8 个 slot 逐对写（`plVar1[4..0xb]`，长度 8）：`GetWarlordDataString(真/假)` · `GetCardsPlayPlayer` · `GetCardsHandPlayer` · `GetCardsDeckPlayer`。字面量落在 `DAT_184281140`（**元数据使用槽**，解不到具体那条）；`stringliteral.json` 里「8 参且形如 `{0}: {1}\n {2}: {3}\n {4}: {5}\n {6}: {7}`」只有 **RVA 0x426EAF8（无尾随）** 与 **0x426EBF0（尾随 `\n `）** ⇒ 取前者，并在账里写明「二选一未定」。四个子串（督军数据 / 出过的牌 / 手牌 / 牌库）的**原版格式没复核** ⇒ 我们的是自定形状（已写进账的「差异」列）。

---

## ③ 改了什么（`RuleCore.cs` 单文件 · `+~435` 行、全部追加在类尾或 1~5 行的调用点）

| 位置 | 改动 | 判据 |
|---|---|---|
| `:9181` `HealUnit` | `+5` 行：加血前 `RecordPendingHealing(ctx, u, amount)` | `BattleManager__HealOneCharacter.c:123`（`AddAutoActionToQueue` **之后**登记）+ `__HealMultipleCharacters.c` |
| `:2877` `SweepEphemeral` | `+3` 行：离手后 `RemoveCostEffects(ctx, c)` | `_ResolveDisappearFromHand_d__545__MoveNext.c:134`（`DestroyCard` 前） |
| `:1684` `BeginTurnCore` | `+5` 行：`ResolveAtTurn("turn_start")` 之后 `BroadcastTurnStarted(ctx)` | `_NextTurn_d__395__MoveNext.c:385`（`BroadcastTurnStart` `:371` 与 `BroadcastActivateMinions` 之后） |
| `:605` `NewBattle` | `+4` 行：两位督军建好后 `ApplyWarlordLifeOverride(ctx)` | `SetupWarlordsLifeAndEnchantment.c:74-140`（①先②后） |
| `:12925` `SpendNormalMana` | `+5` 行签名（`bool energySpentSignal = true`）+ `+4` 行调用；维护费那处传 `false` | `PayCardCost.c` / `PayActiveAbilityCostOath.c`（**只这两处**） |
| 类尾 `:14454-14872` | 新增成员 7 组：待治疗预测表 3 件 · `RemoveCostEffects` · `BroadcastTurnStarted` + 闸 · 督军上限覆盖 3 件 + `GetGameState` 5 件 · 能量消耗 3 件 · `MissActions` | 逐条出处写在每个成员的 doc 里 |

**新增的 public 口（给别处接线用，⛔ 都不改行为）**：
`RuleCore.RecordPendingHealing` / `EnoughPendingHealingToFull` / `RemoveCostEffects` / `SetWarlordLifeOverride` / `GetGameState` / `EnergySpentTotal` / `MissActions`。

**🔶 自己设计标记**：只有两处、都打在新成员 doc 里 —— ① `SetWarlordLifeOverride` 的数值来源（原版在服务端 `MatchData`，P3）；② `EnergySpent` 落成「累计 + 取数口」而非 `EvtKind`（我们无信号总线；加 EvtKind 没消费口 = 死代码，且表现层会按关键词去播）。两处都写了「谁定的 + 日期」。

**已知坑（本笔自己踩的，写下来防复发）**：
- 🔴 `ConditionalWeakTable.GetOrCreateValue` **不能用于 `int[]`** —— 它要求 `TValue : new()`，`int[]` 没有无参构造 ⇒ **运行期**抛 `MissingMethodException: Cannot dynamically create an instance of type 'System.Int32[]'`，而**类型检查 0/0、只有 `ruleprobe play` 才现形**（实测把全池 1,128 张一起刷红，`playRc 0 → -1`）。已改成 `TryGetValue` + 显式 `new int[2]` + `Remove`/`Add`。

---

## ④ 需要【跨文件一行】的接线（都不在本笔白名单，请调度台派）

1. **`RuleEngine/Core/ConditionEval.cs:231`（`case 40` · `damaged` 过滤器）** —— 属 `F3`。加一句：
   `&& !RuleCore.EnoughPendingHealingToFull(ctx, u)`（原版 `FilterMethods.cs:1121` = `GetCurrentDamage() > 0 && !EnoughPendingHealingToFull()`）。
   接上之前池内 **27 行** `conditionsFilter = 40` 的卡行为不变。
2. **`RuleEngine/Core/EffectResolver.cs` 的手牌弃置那条路** —— 原版 `_ResolveDiscardFromHand_d__544__MoveNext.c`（状态 4）在 `DestroyCard` 前调一次 `CardScript.RemoveCostEffects`。加一行 `RuleCore.RemoveCostEffects(ctx, inst)`（`inst` = 那一份 `CardInstance`）。
3. **`RuleEngine/Core/EffectResolver.cs` 的「没有合法目标」那一支** —— 原版在那里入队 `missAction` ⇒ 调 `RuleCore.MissActions(ctx, 该单位)`（非单位传 `UnitState` 为 null 时只记日志，我们的实现里 `u == null` 直接返回 —— 若要覆盖战术卡，请给它一张「战术卡日志」的轻量口，⛔ 别无脑传一个假单位）。
4. **`RuleEngine/Core/UnitState.cs`（P-05 所有者）** —— `BroadcastRemoveFromGame`（637）要的「移出游戏」状态（`cardState 11`）与完整 extrinsic 账。池内 0 入口，建议随 P-05 那批一起做。
5. **`Editor/RuleEngineTest.cs:4062` 夹具** —— 残骸阵营写死 `"Test"` ⇒ 阵营口径下会被 `ErrNotWaystone` 拒；改成 `SaimHann`（判据 → `报告/B8_AX_残骸trait与卡定义.md` §五）。
6. **⛔ 别照施工单 P-01 ① 改 157**（理由见 §2·157）。

---

## ⑤ 读数与验证

### 5·1 类型检查（L0）

`TMPDIR=/tmp/wf_E11RC bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
（中途出现过 3 条 `UnitState.cs` 的 `CS0103 _remnantTraits` —— 是**别人正在写**的文件，等它写完重跑即 0/0。）

### 5·2 `ruleprobe play check`（结算层）

- 当次现算结算指纹 = **`1b40f42ab010e623e908fcd4a236205f`**；基线文件 = `af8e817839fa1dd199d33320f6bf1b2e`（不同）。
- 逐卡：**主档 1 张**（BL47）· **事件电池 3 张**（BL18 / SW25 / UM18）。

### 5·3 🔴 指纹归因（A/B 隔离做过，**不是猜的**）

| 步骤 | 结算指纹 |
|---|---|
| 带本笔改动 | `1b40f42a…` |
| **把本笔 5 个调用点全部摘掉**（`RecordPendingHealing` / `RemoveCostEffects` / `BroadcastTurnStarted` / `ApplyWarlordLifeOverride` / `RecordEnergySpent`） | **`1b40f42a…`（逐字节相同）** |
| 恢复 | `1b40f42a…` |

⇒ **本笔零指纹影响**。那 4 张卡逐条归因：

| 卡 | 差异 | 归谁 |
|---|---|---|
| **BL47** | 主档：`other 0 → 1`，`✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/1/2 {armour1}` | **`E10TB`（修 BL47）** —— 派活包里已点名「本批会有别路 E10TB 修 BL47」 |
| **BL18** | 电池：`netFull` 里一串（净差 +4 量级） | **P-06（`669 BroadcastWhileInPlay` 挂 `Aura.RefreshInPlayNumericAndKeywords`）** —— 施工单 P-06 原话「挂上后 `BL18`/`UM18` 事件电池各 +4 ⇒ **重钉基线必须调度台做**」 |
| **UM18** | 电池：`raw 46→50` / `net 32→36`（**+4**） | 同上（P-06） |
| **SW25** | 电池：`net 26→27`，且 `FIX_WAY` 的关键词从 `{pack1, waystone1}` 变成 `{pack1}` | **P-05（`UnitState` 的 `Has` 残骸闸）** —— 关键词列表少一个「残骸身上不该有的 trait」正是那条口径的产物（`UnitState.cs` 本会话正在被改，`_remnantTraits` 就是它的新字段） |

### 5·4 功能读数（本笔自己的两个口，`ruleprobe play <id>`）

- **841 待治疗表**（`play AM48` = `Heal 2 to all your units and trigger their Regiment abilities`）：
  引擎日志出现 **4 条** `X 记一条待治疗 2（累计 2 / 1 条）`（FIX_A / 督军 / FIX_B / FIX_C），**排在「治疗 4 个目标各 2 点」之前** ⇒ 登记时点 = 原版「治疗已排进队列」那一刻。✅
- **1373 能量消耗**（`play UM6`）：日志出现一次 `[EnergySpent] 开始累计能量消耗（原版 EnergySpentSignal —— 消费者 = 任务挑战…）`，且只出声**一次**（每张牌一条会淹掉 `LogTail`，而它不进指纹）。✅

### 5·5 本笔**没**验证到的部分（如实标着）

- **850 / 805 / 818 / 590 的代码路径今天在池内不可达**（`ephemeral + 实例级费用修正`、`无合法目标入队点`、`trigger 160 = 0 张`、`EventAI/PracticeOffline 且两格非 0`）⇒ 只有「编译过 + 读代码 + 判据逐句对」这一层，**没有行为读数**。它们的验收口写在账的「差异」列里。
- **354 `GetGameState` 零调用方** ⇒ 只有「纯函数、编译过」这一层。
- ⛔ 未跑任何 Unity 自检 / 真机（沿用「暂停自检」口径）。

### 5·6 账的覆盖边界（按 `资料/完全审查与完全复刻_方案.md` §2.4，数字现读）

- 代码账：`战斗逻辑驱动.tsv` 现读 **1,375 行**；分母 **13,182**（全量签名）。
- 资源账：**247,612** 个对象（`assets_full`）；功能点索引已建（`功能点索引.md`）。
- 四类已知未覆盖：① 「读过」≠「复刻过」② 724 条无方法体 ③ 少数只能靠 VA 级反汇编 ④ 已抓到 6 例文件名与内容不符。
- ⇒ **本报告只能证明「这 18 行」的处置**，⛔ 不能当成「P-01 那一域全覆盖」。

---

## ⑥ 交付物

- 代码：`Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs`（唯一改动的 `.cs`）
- 账补丁：`资料/完全复刻/账/_parts/B9_E11RC.tsv`（**13 行**：157·354·440·590·619·637·738·805·818·841·845·850·1373；首行 11 列表头；`merge_ledger_parts.py --dry` 已过）
- 本报告：`资料/完全复刻/报告/B9_E11RC_RuleCore_P01.md`
