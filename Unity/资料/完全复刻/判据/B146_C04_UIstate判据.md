# B146 · C-04 UIstate 判据（2026-11-05）

> 只读代理产出。判据源：`d:/2/tools/decomp_full/*.c`（第一权威）· `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/*.cs`（可读）· `d:/2/tools/il2cpp_out/dump.cs`。
> 写法约定：**值在前、时间限定在后**。凡 ILSpy/cpp2il 与 `.c` 冲突，一律以 `.c` 为准。

## 一、原版定义与取值

| 项 | 结论 | 出处 |
|---|---|---|
| 枚举名 | **`UIstateOptions`**（全局命名空间，`TypeDefIndex: 971`） | `il2cpp_out/dump.cs:46314` · `cpp2il/out_cpp2il_dev_ilspy/UIstateOptions.cs:1-18` |
| 底层类型 | **`int32`**（`public int value__`）；字段 `undefined4` @ `+0x290` | `dump.cs:46317` · `BattleManager__get_UIstate.c:5` |
| 载体 | `[SerializeField] private UIstateOptions __field_UIstate; // 0x290`（**BattleManager 的私有序列化字段**） | `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:116` |
| 访问器 | `get_UIstate` / `set_UIstate`（`private set`）+ `GetUIState()` —— 三者共用同一个 0x18096b540 体，**都是直接读/写 `+0x290`** | `BattleManager.cs:537-548` · `dump.cs:30969/31129/31133/32818` · `BattleManager__GetUIState.c:5` |
| 外部写手 | **0**（全树 `grep "\.UIstate = "` 排除 `__field_UIstate` 后 0 命中）⇒ **只有 BattleManager 自己写** | `cpp2il/out_il2csharp/Assembly-CSharp/*.cs` 全扫 |
| 初值 | `0`（`inactive`，序列化缺省）；对局内第一条写是 `SetupMulliganPhase → 11` | `BattleManager.cs:116` |

**15 档取值（逐值照抄，⛔ 别按名字猜）**

```
0 inactive · 1 normalBattle · 2 movingHandCard · 3 playingMinion · 4 playingTargetedSpell
5 playingNonTargetSpell · 6 minionAttacking · 7 finishBattleSequence · 8 postBattle
9 choosingUnitTarget · 10 targetedActiveAbility · 11 mulligan · 12 mulliganFinished
13 choosingAbilityTarget · 14 choosingResolutionCard
```

## 二、写点表（27 处 + setter；`.c` 行号）

| 值 | 位置 | 函数 | 时机 |
|---|---|---|---|
| 11 | `_SetupMulliganPhase_d__341__MoveNext.c:29` | `SetupMulliganPhase` | 开局进入换牌 |
| 12 | `_SetupEnviromentalEffectPhase_d__352__MoveNext.c:129` | `SetupEnviromentalEffectPhase` | 换牌收尾 / 进环境卡阶段 |
| 1 | `_StartBattlePhase_d__334__MoveNext.c:165` | `StartBattlePhase` | 正式开打（同句置 `duringBattleFlag +0x25c`） |
| 2 | `StartMovingCardInHand.c:8` | `StartMovingCardInHand` | 玩家按住手牌开始拖 |
| 2 / 1 | `Update.c:621` · `:828` · `:407` | `Update` | 拖拽起手 / 松手复位各支 |
| **3 / 5 / 4** | `Update.c:264` · `:270` · `:280` | `Update` | 拖过阈值且 `CanPlayCard` 通过：单位⇒3 · 非指向战术/Whispers⇒5 · **指向**战术⇒4（4 那条先 `Play2D(PlayMinionFromHand)`） |
| 1 | `Update.c:1095` · `:1200` | `Update` | 拖拽放弃 / 落在非法处收回 |
| 6 | `StartAttackFrom.c:30` | `StartAttackFrom` | **仅 `param_4`(playerInitiated)!=0** 时；开十字准星 |
| 10 | `StartTargetedActiveAbility.c:31` | `StartTargetedActiveAbility` | 拖主动技能出来瞄准 |
| 13 | `StartChoosingAbilityTargetByClick.c:15` | `StartChoosingAbilityTargetByClick` | 点选主动技能、等点目标（**函数第一句**就写） |
| 9 | `_ResolvePlayCardFromHand_d__447__MoveNext.c:389` | `ResolvePlayCardFromHand` | 单位已落地、等玩家点目标 |
| 1 | `_ResolvePlayCardFromHand…:1333` · `_ChooseUnitTarget_d__780__MoveNext`（il2c `:37052`）· `EndTurnClick.c:66` | 同上 / `ChooseUnitTarget` / `EndTurnClick` | 选目标收尾 · 结束回合（`CancelActionStates()` 为真才置 1） |
| 14 | `ChoiceOfCardPlayer.c:20` | `ChoiceOfCardPlayer` | 开「选牌」模态 |
| 12 / 1 | `ClickChosenCardDone.c:40` · `:25` · `:105` | `ClickChosenCardDone` | 选牌确定：换牌期(11)⇒12；平时⇒1；末段再落 1 |
| 7 | `DeadHero.c:164` · `CancelMatchWithoutResult.c:21` · `FailedToReconnectAfterDisconnect.c:63` | 三处 | 战斗结束演出 / 取消对局 / 掉线 |
| 8 | `FinishedLeavingBattle.c:40` · `_CompleteBattleFinished_d__389__MoveNext.c:39` | 两处 | 离场完成 |

> ⚠️ `set_UIstate` 是 `private set`，**全树 0 外部调用者**。⇒ 移植时**别把它做成公开可写属性**（原版也没有）。

## 三、读点表（尤其 `CanAttackNow`）

### 3.1 `CanAttackNow` 那一处（本单核心）

`CardScript__CanAttackNow.c:56-66`（`cpp2il/out_il2csharp/CardScript.cs:1354-1362` 逐句同形）：

```
if (this.isPlayer) {                       // card+0x40
    if (UIstate != 1 /*normalBattle*/) {
        if (UIstate != 6 /*minionAttacking*/) return false;
    }
}
```

⇒ **`UIstate ∈ {1, 6}` 才放行**；且**只对 `isPlayer`（本机玩家自己的卡）** 判，敌方卡整段跳过。
完整闸序（`CardScript__CanAttackNow.c` 全文）：① 近战≥1 或 远程≥1 → ② `!isRemnant` → ③ `!trait 150 cantAttack` → ④ `!trait 880 nonCombatant` → ⑤ `allowPlayerActionsFlag` / `allowEnemyActionsFlag`（按 `isPlayer` 二选一，读 0 ⇒ false）→ ⑥ `cardState ∈ {2 inPlay, 3 inPlayAttacking, 17 inPlayAminingAttack}` → **⑦ UIstate ∈ {1,6}（仅 isPlayer）** → ⑧ `canAct(+0x230)` → ⑨ `canAttack(+0x23c)`。

### 3.2 同族闸（数值各不相同，⛔ 别共用一张表）

| 读口 | 允许的档 | 出处 |
|---|---|---|
| `CanUseAbilityNow` | **{1, 10, 6, 13}**（仅 isPlayer） | `CardScript__CanUseAbilityNow.c:69-89` |
| `CanAttackCard(card, allowAutoActions)` | `allowAutoActions==false` 且 isPlayer ⇒ **==1**（`param_3!=0` 整段跳过） | `BattleManager__CanAttackCard.c:44` |
| `CanDisplayHandCard` / `CanMoveHandCard` | **==1** | `BattleManager__CanDisplayHandCard.c:10` · `__CanMoveHandCard.c:17` |
| `CanUseActiveAbility` / `CanUseWaystone` / `ReadyToUseActiveAbility` | **==1**（只判 isPlayer 那一支） | `:82` / `:47` / `:52` |
| `IsPlayingCardState` | **∈{2,3,4,5}** | `BattleManager__IsPlayingCardState.c:8-14` |
| `IsDuringBattle(includeMulligan)` | 0⇒false · {7,8}⇒false · **{11,12}⇒`includeMulligan`** · 其余⇒true | `__IsDuringBattle.c:9-20` |
| `IsDuringMulligan` | **∈{11,12}**（`state-11 <= 1` 无符号） | `__IsDuringMulligan.c:7` |
| `DisplayCard` | **禁止 {0,7,8,11,12}**（否则/`param_4` 强制才显示） | `__DisplayCard.c:15` |
| `ClickChat` | **禁止 {0,2,3,4,5,7,8,11,12,14}** ⇒ 只剩 {1,6,9,10,13} | `__ClickChat.c:15-26`（位掩码 `0x402c` = bit{2,3,5,14}，另有 `state!=4`） |
| `EndTurnClick` / `CountdownTrigger` / `ShowDeckSize` / 若干协程 | 特判 `!= 14` / `==14` | `__EndTurnClick.c:43` · `__CountdownTrigger.c:5` · `__ShowDeckSize.c:24` |
| `CancelActionStates` | {2,3,4,5}⇒走"取消出牌" · **{9,10}⇒返回 1** · {6}⇒`CancelAttack` · 其余⇒0 | `__CancelActionStates.c:17-45` |
| `Update` | `switch(state)` 只显式处理 **case 1 和 case 2** | `__Update.c:159-289` |
| `GetTargetInPos` | 10 / 13 两档特判取目标 | `__GetTargetInPos.c:160-165` |
| `TryCardHighlight` / `OnTouchDown` / `OnTouchUpAsButton` / `OnTouchDrag` / `TryDraggingFromBoard` / `CardScript.ActivateMinion` / `CanActNow` | 经 `get_UIstate` 间接读 | 各 `.c` |
| **非 BattleManager 的读者** | `BattleHud.cs:108`（==1）· `ActiveSkillDisplay.cs:465-466`（!=10）· `ClockManager.cs:569-570`（!=postBattle） | cpp2il |

> 派生物：`CardScript.CanActNow()` = **`CanAttackNow() || CanUseAbilityNow()`**（`CardScript__CanActNow.c:8-12`）——
> 我们把这一层收窄成了「近战||远程」（见 §四 缺口 ③）。

## 四、我们的现状与缺口

| # | 现状 | 位置 | 缺口 |
|---|---|---|---|
| ① | 已经有一份**逐档映射** `UiStateNow()`（15 档全齐，注释标明每档等价物） | `BattleDriver.cs:42261` | **它是"现算 getter"，不是存储字段**；住在表现层，**`RuleCore` 看不见** |
| ② | `BattleUiIsNormalBattle` = `UiStateNow()==1`，唯一消费点 = 收灵魂石 | `BattleDriver.cs:34482-34493` · `:34136` | 只覆盖 1 档；原版 `CanUseWaystone` 另有 4 道闸已接 |
| ③ | `CanAttackNow` **无任何 UI 闸** | `RuleCore.cs:5868-5950` | 原版第 ⑦ 条（`UIstate ∈ {1,6}`）整条缺失 |
| ④ | `CanActNow` = 近战||远程 | `RuleCore.cs:5953-5958` | 原版 = `CanAttackNow \|\| CanUseAbilityNow`；缺技能那一半 + 缺 `CanUseAbilityNow` 的 {1,10,6,13} 闸 |
| ⑤ | 手牌拖拽已有等价闸（「`UIstate != 1`」三分支） | `CardInteraction.cs:346-354` | 自建判据，与 `UiStateNow()` 是**第二份** |
| ⑥ | `IsDuringBattle` 已有等价物（吃 `ctx`） | `BattleContext.cs:2329-2352` | 注释自陈「我们没有单一 UIstate 枚举」；**0 消费者** |
| ⑦ | `MatchData`/`ClockManager` 侧 | — | `ClockManager != postBattle` 那条未复用 `UiStateNow()` |

## 五、最小可落地模型（含影响面）

### 5.1 新增（全部**零调用点改动**）

1. `RuleEngine/Core/UiStateOptions.cs`（新文件，全局/`RuleEngine` 命名空间）——
   15 个常量**逐值照抄** §一；注释里写死判据 `dump.cs:46314-46332`。
2. `BattleContext`（**追加到 partial 类尾**，见 `BattleContext.cs:2520` 起那块 `partial class`）：
   ```csharp
   public int UiState = 1;      // UIstateOptions.normalBattle —— 缺省 1 ⇒ headless/AI/断言零影响
   public int LocalSeat = 0;    // 本机的座位（P2P 由入口给；单机恒 0）
   ```
   ⚠️ `BattleContext.cs` 有 40+ 条按行号引用的账落点 ⇒ **只能追加在类尾**（先例 `B96_CA`）。
3. 一个**写口**（判据只一处）：`BattleDriver.SyncUiStateToContext()` =
   `if (Ctx != null) Ctx.UiState = UiStateNow();`
   调用点 = ① 每帧输入驱动开头（`DrivePlayerTurn` 入口）② 每次 UI 档位变化后（拖拽起/止、选择器开/关、面板开/关、`AwaitingPlayTarget` 变、`SyncBoard` 尾部）。

### 5.2 `CanAttackNow` 闸怎么写（插在原第 ⑥ 条之后、`AttackBannedByTraits` 之前）

```csharp
// ⑦ 原版 `CardScript__CanAttackNow.c:56-66`：仅 isPlayer（本机自己的卡）
if (p == ctx.LocalSeat && ctx.UiState != 1 && ctx.UiState != 6) return RuleCodes.ErrWaitingBlockingAction;
```
- 返回码建议 **复用 `ErrWaitingBlockingAction(30)`**（语义就是「现在这一拍不接受输入」，且已有名字/文案，见 `RuleCodes.cs:185-190`）；**不新建码**。
- `allowAutoActions == true` 那一支必须**继续短路在前面**（原版 `CanAttackCard(param_3!=0)` 同样跳过 UI 闸）⇒ 脚本攻击 / `forceAttack` / 回放零影响。

### 5.3 影响面（会不会把今天能攻击的变不能）

| 消费者 | 影响 | 结论 |
|---|---|---|
| 引擎/无头（`ruleprobe play`、`RuleEngineTest` 全部 `CanAttackNow` 断言） | `ctx.UiState` 缺省 **1** | **零变化** |
| `SimpleAI` / `play all dump` | AI 座位 **≠ `LocalSeat`(0)** ⇒ 闸整段跳过 | **零变化**（且与原版敌方卡跳过同构） |
| `DeclareAttack` / `DeclareAttackByCurrentType`（玩家点击路径） | 只在 `UiStateNow()` 落 2/3/4/5/9/10/13/14/11/12/7/8/0 时拒 | **对齐原版**；但见下方风险 |
| 绿光 `CanActNow`（`BattleDriver.cs:26901` / `:27014`） | 同上 | **对齐原版**（原版绿光就是 `SetActive(CanAttackNow())`） |
| `GetWarlordDataString` 的 `ca:` / `GetCardsPlayPlayer` | 无（它们走 `CanAttackNowBool`，但 `LocalSeat` 缺省 0 时玩家侧可能翻转） | ⚠️ **需单独确认**：这两处是「序列化给对方的字符串」，原版读的是**本机 isPlayer**，联机时两端结论不同 —— 属既知差异（`RuleCore.cs:16406` 注释已记） |

### 5.4 两个必须先处理的风险

1. **`UiStateNow()` 是现算的**，而 `WaitingForBlockingAction` 在演出期间为真 ⇒ 若把「每帧开头」当唯一同步点，`SyncBoard` 在演出中重算绿光时会把**整排绿光关掉**，且要等下一次 `SyncBoard` 才回来（原版 `ActivateMinion` 是事件驱动、不会这么闪）。
   ⇒ 缓解：同步点放 **`DrivePlayerTurn` 入口 + 所有 UI 档位变化点**（不是每帧无脑抄），并在 `SyncBoard` **之前**显式刷一次。
2. **判据重复**：`CardInteraction.cs:346` 已有一份「`UIstate != 1`」自建判据。接上 `ctx.UiState` 后必须**改成读同一份**，否则同一条规则两处写（铁律 6）。
3. ⚠️ `LocalSeat` 在**回放局**（`_replaySession`）与 **P2P 客机**下的取值要现读 `BattleDriver._me` 后确认；`LocalSeat` 缺省 0 对客机是错的（客机 `_me == 1`）。

## 六、没查清的

1. `Update.c` 那 40+ 处 `normalBattle` 写点里，有若干是 Ghidra 的 `goto` 合流被摊平的**同一逻辑写点**；cpp2il 版逐个可见但没有强制编号 ⇒ **写点"个数"取 27 是按去重后的落点算的，可能略多**（不影响语义）。
2. `GetWarlordDataString` / `GetCardsPlayPlayer` 读 `CanAttackNow` 的**联机两端一致性**未验（本机 `isPlayer` 在客机上翻转，字符串会不同）—— 需真机或 `NetBattleTest` 才能定。
3. `CanUseAbilityNow` 的 {1,10,6,13} 之外是否还有 `choosingAbilityTarget(13)` 的上游写入条件（`StartChoosingAbilityTargetByClick` 无条件写 13）是否会与 `skillPanel` 关闭时序冲突，**未读 UI 侧**。
4. `UIstate` 的 **`[SerializeField]` 初值在 prefab 里是否被覆盖成非 0** —— 未去 `assetripper_out` 的 `BattleManager` prefab 上读序列化值（若为 0，则"对局开始前"确为 `inactive`，与本报告一致；若为 1 则差一档）。
5. 我们 `UiStateNow()` 的 15 档等价物**逐档**与 §二/§三 的对齐度：本次只核了 `1/2/6/9/10/13/14/11/12/7/8`，**`3/4/5` 三档的等价物（`WaitingForBlockingAction` + `DeployingNonUnit`）未逐条验**。
