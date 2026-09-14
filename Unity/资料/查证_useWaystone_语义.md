# 查证：原版 `useWaystone` 一族的语义

**出处**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out{,2}/`（Ghidra 反编译）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（⚠️ 签名桩，方法体空的，只当「有这个字段/方法」用）· 规则书 `d:/4/Unity/资料/规则书/` · OCR 卡表 `d:/4/Unity/数据/游戏数据/card_stats.json`。 2026-09-14。

## 一、结论

1. 【**两件不同的事**】`useWaystone` 是**独立的玩家动作**：`PlayerActions.clickWaystone = 6`（`GetAvailableActions` 会逐卡列出）→ `TryUsingWaystone` → `BattleActionType.useWaystone = 76` 入队 → **协程** `ResolveUseWaystone`，**回合内随时可点**，不是「从手牌打出时一次性决定」。
2. 而 `CanUseSpiritStone`（在 `_ResolvePlayCardFromHand` 里被调的）**管的是另一件事**：它只是「打出牌流程里那个*从池中选一个*（`BattleManager.GetChoiceOptions/GetChoiceFullPool` 那个 choice 池）」步骤的闸门之一（与 `DefinedTrait.rally = 10` 并列 → `NeedsToChooseFromPool` → `ChooseCardMethod` 协程）；**两条链互不调用**（全量 grep：`CanUseSpiritStone` 只出现在打出牌协程里）。
3. 【**作用对象**】场上那张**属于你自己、已翻面成「灵族残骸／灵魂石」的原卡**（`cardState == inPlay(2)`、`isRemnant(+0x65) = true`）。`actingCard` 来自 `CardScript.OnTouchUpAsButton`（点这张卡）或 `GetAvailableActions` 的逐卡扫描 —— **就是这张卡本身**，不是「上一张被打出的卡」。卡面侧＝SaimHann 12 张 `Waystone.` 单位（Dire Avenger／Fire Dragon／Ranger…）。
4. 【**前置条件**】①`card.isPlayer(+0x40) == isPlayerAsking`（只能点自己那侧）；②提问方此刻允许行动（`isPlayerAsking ? this+0x244 allowPlayerActionsFlag : this+0x246 allowEnemyActionsFlag`）；③自己那侧的卡还要求 `this+0x290 UIstate == normalBattle(1)`；④`cardState == inPlay(2)`；⑤教程闸门 `CheckIfPlayerActionPermittedInTutorial(this, 0x4c /*=76*/, card, …)`。
5. ⚠️ **`CanUseWaystone` 既不查灵魂石余额、也不查这卡有没有 600 能力** —— 点击链上的这两条判定不在这函数里（余额那条读不到，见 §四）。
6. 【**三步**】Try：先带提示查 `(card,1,1)`，失败记日志 `return false`；再静默复检 `(card,1,0)`，**失败或 `waitingToResolveUseWaystoneFlag` 已置位** → 只警告、flag=1、`return true`；通过 → `BattleAction{76, isPlayer=card.isPlayer, actingCard=card}` 用 `AddAutoActionToQueue(action, VarsGlobal+0xb4 优先级)` 入队、flag=1、`return true`。
7. Execute：同一套静默复检＋入队，**失败时把 flag 清 0**。
8. Resolve：**协程**（`ResolveAction` 的 `case 0x4c` 只做「建状态机＋`StartCoroutine`」；**协程体 MoveNext 没被 dump**）。`WaitingForResolveUseWaystone`＝`waitingToResolveUseWaystoneFlag`(this+0x419)，给 UI 当「正在结算路标石、别再收输入」（`TryCardHighlight` 一见到就早退）—— **是异步流程这点是读到的**。
9. 【**代价**】`CanUseSpiritStone` 读**卡自己** 600 能力的 `triggerValue`（`CardAbility` +0x20，trigger 在 +0x10），余额读 `PlayerManager.GetCurrentSpiritStone()`，要求 `余额 >= triggerValue` 且 `余额 >= 1`。
10. 【**触发哪一族**】`AbilityTrigger.UseSpiritStone = 600` → 既有入口 `CardScript.TriggerSpiritStone(actingCard)` 里的 `RawCardScript.TriggerAbilities(600, bm, thisCard=卡, cardPlayed=actingCard, targetCard=卡, null)`；随后 `BroadcastUnitSpiritStone` → 每张在场卡＋双方手牌逐张 `TriggerOtherCardSpiritStone`（= `OtherCardSpiritStone = 455` 的听众）。另有 `BattleActionType.triggerSpiritStone = 77`：`AddTriggerSpiritStone(targetCard, actingCard, priority)` → `ResolveTriggerSpiritStone` → 同一个 `TriggerSpiritStone`。

## 二、证据（`结论 ← 文件:行号（读到的那行）`）

- 独立玩家动作 ← `Assembly-CSharp/PlayerActions.cs:9`（`clickWaystone = 6`，同表 `activeAbility = 5`）← 会被主动列出（含 AI）：`decomp_out/BattleManager__GetAvailableActions.c:321`（`CanUseWaystone(this, 卡, param_2, 0, 0)`）`:330`（`AvailableAction.SetParameters(act, 6=clickWaystone, 卡, 0, 分数, 0)`）；同形对照 `:162`+`:182`（activeAbility=5）
- 点卡入口 ← `decomp_out/CardScript__OnTouchUpAsButton.c:72`（`SupportMethods__IsAeldariRemnant(card)`）`:83`（`TryUsingWaystone(bm, card, …)`）；状态闸 `:23-30`（1/8 走手牌分支，2/3/17 落到这里）
- 动作号与载荷 ← `decomp_out/BattleManager__TryUsingWaystone.c:56`（`uStack_308._0_4_ = 0x4c`=76）`:58`（`= *(byte*)(card+0x40)`）`:59-62`（actingCard=card，写在 +0x8）
- 入队 ← `TryUsingWaystone.c:111,159`（`get_globalVars` → `AddAutoActionToQueue(this, action, *(int*)(globalVars+0xb4), 0)`）；桩 `BattleManager.cs:5469 AddAutoActionToQueue(BattleAction, int priority)`
- 权限闸 ← `decomp_out/BattleManager__CanUseWaystone.c:26`（`card+0x40 == param_3`）`:27-32`（`this+0x246` / `this+0x244`）；`decomp_out/BattleManager__get_allowEnemyActionsFlag.c:5`（读 `this+0x246`）；桩 `BattleManager.cs:4687/4691`
- UI/卡状态闸 ← `CanUseWaystone.c:48`（`*(int*)(card+0x228) == 2`）`:50`（`*(int*)(this+0x290) == 1`）；`decomp_out/BattleManager__get_UIstate.c:5`；`CardStateOptions.cs`（inPlay=2）；`UIstateOptions.cs`（normalBattle=1、choosingResolutionCard=14）
- 教程闸 ← `CanUseWaystone.c:54`（`CheckIfPlayerActionPermittedInTutorial(this, 0x4c, card, …)`，0x4c 就是 76）
- 三次调用的形状 ← 桩 `BattleManager.cs:7266 CanUseWaystone(CardScript parentCard, bool isPlayerAsking, bool showTips)`；`TryUsingWaystone.c:34`（`(card,1,1,0)`）`:43`（`(card,1,0,0)`）；提示本体 `CanUseWaystone.c:40-44`（`GetTermTranslation` → `BattleTipController.NotifyCantDoAction`）
- flag ← `decomp_out/BattleManager__get_WaitingForResolveUseWaystone.c:5`（`return *(byte*)(this+0x419)`）；桩 `BattleManager.cs:4561 waitingToResolveUseWaystoneFlag`、`:4770`；读它 `decomp_out/CardScript__TryCardHighlight.c:82`
- Resolve 是协程 ← 桩 `BattleManager.cs:5715-5716`（`[IteratorStateMachine(typeof(_003CResolveUseWaystone_003Ed__480))] private IEnumerator ResolveUseWaystone(BattleAction)`）+ `:3699-3705`（状态机只有 state/current/`<>4__this`/battleAction）；`decomp_out/BattleManager__ResolveAction.c:6621`（`case 0x4c:`）`:6680`（`StartCoroutine_Auto`）；77 号在 `:6682`，`:6919` 调 `CardScript__TriggerSpiritStone`
- 代价与余额 ← `decomp_out/CardScript__CanUseSpiritStone.c:18`（`HasAbilityType(rawCard,600)`）`:30-32`（`GetPlayerManager(bm, card+0x40)` → `GetCurrentSpiritStone`）`:33,36`（`<1` / `< iVar4` → 0）`:52`（`iVar4 = *(int*)(ability+0x20)`）；桩 `CardAbility.cs:16 triggerValue`（前面排 trigger/abilityContext/priority/playIcon ⇒ +0x10/+0x14/+0x18/+0x1c/+0x20）
- 600 触发 ← `decomp_out/CardScript__TriggerSpiritStone.c:18`（`FUN_18094a920(rawCard, 600, bm, card, actingCard, card, 0)` = 桩 `RawCardScript.cs:350 TriggerAbilities(AbilityTrigger, BattleManager, thisCard, cardPlayed, targetCard, …)`）`:21-24`（`HasAbilityType(600)` → `BroadcastUnitSpiritStone`）
- 455 听众 ← `decomp_out/BattleManagerSupport__BroadcastUnitSpiritStone.c`（遍历 `bm+0x470` 在场卡、再双方手牌 → `CardScript.TriggerOtherCardSpiritStone`）
- 77 号 ← `decomp_out/BattleManager__AddTriggerSpiritStone.c`（`BattleAction.__ctor(…, 0x4d, …, casterCard=actingCard, …)`；`targetCard` 写在 +0x10；`AddAutoActionToQueue(action, priority)`）；`decomp_out/BattleManager__ResolveTriggerSpiritStone.c:133,269,316`
- 打出牌流程里的 CanUseSpiritStone ← `decomp_out2/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:719-731`（`HasDefaultTrait(raw,10)` 不成立再看 `CanUseSpiritStone(card)`；都假就 `goto` 正常结算，否则 `NeedsToChooseFromPool`）`:830`（`ChooseCardMethod` 协程）；`decomp_out/CardScript__NeedsToChooseFromPool.c:31-37`（池对象 `rawCard+0x2a0` 是 List）、`:118` 起（`HasDefaultTrait(raw, 0x4fb=oath)` vs `BoardAnalysis.GetManaLeft`）；池＝choice 池：`decomp_out/BattleManager__GetChoiceOptions.c:43`、`decomp_out/BattleManager__GetChoiceFullPool.c:30,38,65`（读/填 `rawCard+0x2a0`）
- 残骸＝同一张卡翻面 ← `decomp_out/CardScript__SetFutureRemnant.c`（体是 `EntityScript__set_isRemnant`，写 +0x65）；`decomp_out/CardScript__SetLeavingRemnant.c`（+0x66=1）；桩 `CardScript.cs:2580/2584 TransformIntoRemnant/FromRemnant`、`BattleManager.cs:6694 GetUnitsInPlayPooledList(… inclRemnants …)`、`BattleAction.cs:77 toRemnant`、`BattleCardUI.cs:416/607 ChangeCardToRemnant/ShowOriginalRemnantCard`
- 规则书 ← PDF 打印页 10/11 右栏：「**Waystone**: When this unit dies: flip it face down to represent the creation of a Spirit Stone」·「**Spirit Stone**: Token created when Aeldari units with Waystone are destroyed. Destroyed when dealt damage. Collectible during controlling player's turn. Number collected is reduced by amount required to trigger ability.」；中文翻译 `:225/:210/:203`
- 数据侧关键词 ← `DefinedTrait.cs:112 waystone=1140 / :103 remnant=1050 / :132 oath=1275 / :80 chooseOne=820`；`RawCardScript.cs:668 UsesSpiritStone()`；`UnitConditions.cs:17 hasUseSpiritStone=241`

## 三、「读到的」与「推的」分开

**读到的**：上面 §二 全部；核心是「点击＝一个独立玩家动作 76」「对象是场上自己那张卡」「`CanUseWaystone` 只查归属／权限／UI 状态／卡状态／教程，**不查余额**」「Resolve 是协程」。

**推的（没直接读到）**：
- 「点一下＝**收集**这颗灵魂石（而不是『花 N 颗激活』）」 —— 推的（规则书「控制者回合可收集」＋`clickWaystone` 命名＋`CanUseWaystone` 没有余额检查，三处同向）。**这一下是往池子里加还是扣**：`AddSpiritStoneMana` 在 dump 里只有 `BattleManager__ProcessManaBuff.c:97` 一个调用点（「Gain N Spirit Stones」那类），`UseSpiritStoneEnergy` **一个调用点都没有**（只能由 `PlayerManager.UseMana(n, ManaType.SpiritStone=5)` 进，`PlayerManager.cs:145/200`、`ManaType.cs:4`）⇒ 加／扣哪边都读不到。
- 「`CanUseSpiritStone` 的余额检查就是『点路标石要花 N 颗』的判定」 —— 推的（它在 dump 里唯一调用点在打出牌流程）。
- 「`IsAeldariRemnant(card)` = 该卡是灵族阵营的翻面残骸」 —— 推的（`SupportMethods` 体没 dump；同文件还有同形的 `IsNecronRemnant`，`SupportMethods.cs:32/37`，调用点全在 UI 三处：点卡／可用动作／高亮）。
- 「`TryUsingWaystone`／`ExecuteUseWaystone` 里 `isPlayerAsking` 硬编码成 1（玩家侧）」 —— **半读半推**：实参读到的是 `1,1,0`／`1,0,0`，但 Ghidra 在该函数上的原型本身不稳（同一函数被写成 3 参和 5 参两种）。
- 「`ExecuteUseWaystone` 由 UI/AI 确认后调用」 —— 推的：全量 grep 里它**没有任何调用点**（只有定义本身）。
- 「`VarsGlobal+0xb4` 是某项入队优先级」 —— 偏移是读到的，**字段名查不到**（桩里没有字段偏移）。
- 600 能力的**效果**（触发后这张卡具体得到什么）dump 里读不到；`AbilityEffect.cs:74 triggerSpiritStone=440` 可能是另一条链。

## 四、没读明白的

- ⚠️ **与 `查证_裸写触发点_SaimHann.md:20,35` 的张力（留给下一个会话裁）**：那份把 `MoveNext:726` 的 `CanUseSpiritStone` 读成「付费窗口开在从手牌打出结算时」；本次读到的同一行是**choice 池那一步的闸门**（`:719-731` 的落点是 `NeedsToChooseFromPool`→`ChooseCardMethod`，池＝`GetChoiceOptions/GetChoiceFullPool` 用的 `rawCard+0x2a0`），而 `CanUseWaystone` 要求 `cardState == inPlay(2)`（手牌是 1 ⇒ 路标石动作打的是**已在场**的卡）。两份不必然互斥（也可能两处都能付费），但**谁都没跑实况坐实**。
- 🔴 **`_003CResolveUseWaystone_003Ed__480.MoveNext` 没被 dump**（`ls decomp_out{,2}|grep -i waystone` 只有那 5 个文件；全量 grep `d__480` 零命中）⇒ 协程体、以及「谁支付灵魂石」「点完到底发生什么」「0x419 何时被清 0」都读不到。
- `0x419` 的写入点只有 `TryUsingWaystone.c:49-50`（连着 `=0; =1;`）`:160` 与 `ExecuteUseWaystone.c:41`（`=0`）；**哪一条是「取消等待」判不出**（大概是源码两处赋值被合并，不能当两个独立语义读）。
- `CanUseWaystone.c:35` 的 `if ((param_3 != 0) && (*(char*)(param_1+0x244) == 0))` 在 `cVar2 != 0` 分支里恒假 ⇒ 该段 Ghidra 有重复／丢失；后果是 **`showTips`(param_4) 在恢复出的函数体里一次都没出现**，提示调用看着是无条件的。
- **`NeedsToChooseFromPool` 那个「池」到底是什么**（`rawCard+0x2a0`，与 oath(1275)／`GetManaLeft` 有关）与 `ChooseCardMethod` 给出什么选项，没读明白；只能确定它**不是** waystone 链的一部分。
- `bm+0x128`（`CanUseWaystone` 里喂给 `NotifyCantDoAction` 的对象）没坐实类型，按其他文件同名偏移应为 BattleTipController。
- 77 号动作（`AddTriggerSpiritStone`）的**上游调用点**在 dump 里也没有（「Trigger the abilities requiring Spirit Stones of your troops」那类执行器没被 dump）。
