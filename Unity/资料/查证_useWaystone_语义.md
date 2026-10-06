# 查证：原版 `useWaystone` 一族的语义

**出处**：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out{,2}/`（Ghidra 反编译）· `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（⚠️ 签名桩，方法体空的，只当「有这个字段/方法」用）· 规则书 `d:/4/Unity/资料/规则书/` · OCR 卡表 `d:/4/Unity/数据/游戏数据/card_stats.json`。 2026-09-14。

---

> 🔴 **2026-09-19 用户给定的玩法语义 —— 这是权威，动手做「收集」之前先读它**
>
> **用户原话**：「灵族部队死了留了灵魂石在场上，这个时候**灵魂石也视为单位（一血）**，
> **我方回合需要点击它才能收集**。一些卡牌的效果需要灵魂石时进行扣除，
> **达到卡牌要求的灵魂石数量（右方收集的灵魂石数量）就强制扣除**灵魂石且发动卡牌的增强效果
> （需要花费灵魂石的效果），**没有则不扣除**（卡牌的增强效果也不发动）。」
>
> **三条落点**：
> 1. **收集 = 玩家主动动作** —— 与本文 §一 结论 1 一致（`clickWaystone=6` → `useWaystone=76` 那条链）。
> 2. 🆕 **灵魂石在场时是一个「1 血单位」** —— 本文通篇**没记过这条**。意味着它上棋盘、能被攻击/被选中，
>    棋盘状态与「单位」相关的判定都要把它算进去。**1 血有硬出处**：`CardScript__TransformIntoRemnant.c:33`
>    读 `GameStaticData.remnantMaxHealth`（`GameStaticData.cs:321`），`__.cctor.c:326` 该槽 = **1**。
>    🔴 **2026-09-19 更正（用户指出后查实）：不是「翻面」、更不是卡背。**
>    本文原来写它是一张「**已翻面**成灵族残骸／灵魂石的原卡」—— 「翻面」这个词**误导**（听着像显示卡背）。
>    **原版卡牌族里根本没有 flip / faceDown 字段**（全量签名桩 grep 只命中 UI 的顶点翻转与 DOTween `DOFlip`），
>    `ShowCardBack` 的唯一调用点是 `PlayerHand.SetupCardInHand` —— **只有手牌才用卡背**。
>    实际发生的是「**原卡变残骸体**」：① 在卡的位置**实例化一个 3D 残骸 prefab**
>    （`BattleCardUI__CreateRemnantBody.c:47,73-90`）② **把原卡的 3D 卡身关掉**
>    （`RemnantBody__BodyVisibilityToggle.c` → `ToggleBody3D`）③ 播 `To Remnant Aeldari` 那条动画。
>    **灵族**最终留在场上的是**一枚漂浮的灵魂石**（`RemnantBody3D Aeldari` 下的 `Spirit Stone Idle`，
>    mesh `Spirt Stone.obj`、材质 `Spirit Stone`）；**死灵**是一张**碎裂的卡**
>    （`RemnantBody3D Necrons` 下的 `Card Remnant`）。资源在
>    `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/{GameObject,Mesh,Material}/`。
>    ④ 收集时 = `RemnantAeldari.DoDestroy(collectWaystone=50)` → `CollectWaystoneEffect()`：
>    `AudioCue.Play3D(waystoneCollectSound)` + 实例化 `Remnant Aeldari Collect particles` + 立刻销毁残骸本体。
> 3. **扣石是强制的、不是「玩家可选」** —— **达到要求就扣、就发动增强效果；不够就不扣、也不发动。**
>
> ⚠️ **与旧记录的冲突，已按本口径更正**：`资料/阵营推进_清单与交接.md` §一之三 原来记
> 「**比原版少一步『选择』**（原版那一步是从池中选一个的闸门、**玩家可以不付**），我们够就自动付」
> ⇒ 按用户口径，**我们现在的「够就自动付」是对的**，那句「少一步选择」里**「玩家可以不付」那半句作废**。
> ⚠️ 但**别顺手把本文 §一 结论 2 也推翻**：`CanUseSpiritStone` 确实挂在「从池中选一个」那一步上
> （反编译读到的）—— 那一步与「玩家要不要付」**是不是同一件事，仍未证**，
> **不许拿它当「玩家可以不付」的判据**（这正是上面那条旧记录的错因）。

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
  🔴 **2026-10-07 更正（A120）：这半句已作废 —— 两边都查到了，而且不是同一条链。**
  · 「**加**」= 就在**收集**协程里：`BattleManager._ResolveUseWaystone_d__480__MoveNext.c:129`
    调 `PlayerManager.AddSpiritStoneMana`（本文 §四 那条 2026-09-25 已读通；数值 `waystoneGiveMana = 1`）。
  · 「**扣**」= 原版**全库唯一**一处扣灵魂石：`RawCardScript__TriggerAbility.c:42-53`（600 分支：
    `thisCard == cardPlayed` 时 `UseMana(pm, ability+0x20, 5 /*ManaType.SpiritStone*/, 0, 0)`；
    全库 `UseMana(…, 5, …)` 只此一处）。⚠️ 那是**激活 600 能力**那条链上的扣，
    **不是** `clickWaystone` 这条链 —— 别混。
  · 另：`PlayerManager__UseSpiritStoneEnergy.c` **有 93 行真方法体**（「签名桩」那句说的是
    `d:/2/Warpforge_code/` 那份**空体**桩集）、**0 调用者** ⇒ 内联残留 / 死函数。
  判据全文 → `资料/普查产出_1007/波7判据核查.md` §A120。
- 「`CanUseSpiritStone` 的余额检查就是『点路标石要花 N 颗』的判定」 —— 推的（它在 dump 里唯一调用点在打出牌流程）。
- 「`IsAeldariRemnant(card)` = 该卡是灵族阵营的翻面残骸」 —— 推的（`SupportMethods` 体没 dump；同文件还有同形的 `IsNecronRemnant`，`SupportMethods.cs:32/37`，调用点全在 UI 三处：点卡／可用动作／高亮）。
- 「`TryUsingWaystone`／`ExecuteUseWaystone` 里 `isPlayerAsking` 硬编码成 1（玩家侧）」 —— **半读半推**：实参读到的是 `1,1,0`／`1,0,0`，但 Ghidra 在该函数上的原型本身不稳（同一函数被写成 3 参和 5 参两种）。
- 「`ExecuteUseWaystone` 由 UI/AI 确认后调用」 —— 推的：全量 grep 里它**没有任何调用点**（只有定义本身）。
- 「`VarsGlobal+0xb4` 是某项入队优先级」 —— 偏移是读到的，**字段名查不到**（桩里没有字段偏移）。
- 600 能力的**效果**（触发后这张卡具体得到什么）dump 里读不到；`AbilityEffect.cs:74 triggerSpiritStone=440` 可能是另一条链。

## 四、没读明白的

- ⚠️ **与 `查证_裸写触发点_SaimHann.md:28,43` 的张力（⚠️ **行号平移过两次**：原写 `:20,35` → 2026-10-10 并 `资料/灵魂石卡_逐张核.md` 时 +7 变 `:27,42` → **2026-10-16 头部又插 1 行「已并入 `资料/查证_裸写触发点_四批.md`」横幅** 变 `:28,43`）（留给下一个会话裁）**：那份把 `MoveNext:726` 的 `CanUseSpiritStone` 读成「付费窗口开在从手牌打出结算时」；本次读到的同一行是**choice 池那一步的闸门**（`:719-731` 的落点是 `NeedsToChooseFromPool`→`ChooseCardMethod`，池＝`GetChoiceOptions/GetChoiceFullPool` 用的 `rawCard+0x2a0`），而 `CanUseWaystone` 要求 `cardState == inPlay(2)`（手牌是 1 ⇒ 路标石动作打的是**已在场**的卡）。两份不必然互斥（也可能两处都能付费），但**谁都没跑实况坐实**。
  🔴 **2026-10-07 补（A121）：「实况未核」这半句照旧成立**（原版已关服 + 手牌注入 `inj FAIL: empty Data`，
  本地核不了），**但判据那一档已从「未证」升级为「反编译有据」** —— 原版里 **600 的发起源只有两处**
  （`RawCardScript__OnCardPlayedWithTarget.c:66`「打出一张带目标的牌」· `CardScript__TriggerSpiritStone.c:16-18`
  那条 77 号动作），**而扣石就在那个 600 分支里**（`RawCardScript__TriggerAbility.c:42-53`）
  ⇒ **正面支持「打出时」**（与用户 2026-09-19 的口径一致；我们实现 = `RuleCore.cs:1142`）。
  判据全文 → `资料/普查产出_1007/波7判据核查.md` §A120 / §A121。
- ✅ **2026-09-25 更正：这一条原来写「`_003CResolveUseWaystone_003Ed__480.MoveNext` 没被 dump」—— 已经不对了。**
  2026-09-17 重建的全量反编译 `d:/2/tools/decomp_full/` 里**有**：`BattleManager._ResolveUseWaystone_d__480__MoveNext.c`（197 行）。
  **原错因**：当时只在 `d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out{,2}/` 这个小 dump 里找，
  而那份只覆盖 71 个类、不含这条协程（`ls decomp_out{,2}|grep -i waystone` 当然只有那 5 个文件）。
  ⇒ **「在小 dump 里 grep 不到」不等于「没被 dump」** —— 查方法体一律先去 `decomp_full/`（`资料/全量反编译_入口与用法.md`）。
  协程体现已读通，结论见下面这条：
- ✅ **【读】收集链全文**（`BattleManager._ResolveUseWaystone_d__480__MoveNext.c`）：
  `PlayerManager.AddSpiritStoneMana(收集方, GameStaticData.waystoneGiveMana)` → `BroadcastCollectedSpiritStone`
  → `CollectSpiritStoneSignal(isPlayer, N, fromWaystone: true)` + `Signal.Raise` →
  `BattleManager.DestroyUnit(…, UnitDeathType.collectWaystone = 50, …)` → `CemeteryManager.AddWaystoneActionToCemetery`。
  🔴 **数值【读】`waystoneGiveMana = 1`**（`GameStaticData.cs:323` 桩 + `GameStaticData__.cctor.c` 该槽 `+0x29c = 1`）
  ⇒ **点一下 = 收集方「+1 颗」，不是扣** —— 本文 §三 原来把这条列在「推的」，**现在升成「读到的」**。
  战斗日志侧另有 `CemeteryActionType.useWaystone = 35`。
- ⚠️ **仍未坐实的两点**：协程里 `0x419`（`waitingToResolveUseWaystoneFlag`）何时被清 0 ·
  收集时**要不要玩家确认/弹提示**（协程主体读到了，这两点没有独立证据）。
- `0x419` 的写入点只有 `TryUsingWaystone.c:49-50`（连着 `=0; =1;`）`:160` 与 `ExecuteUseWaystone.c:41`（`=0`）；**哪一条是「取消等待」判不出**（大概是源码两处赋值被合并，不能当两个独立语义读）。
- `CanUseWaystone.c:35` 的 `if ((param_3 != 0) && (*(char*)(param_1+0x244) == 0))` 在 `cVar2 != 0` 分支里恒假 ⇒ 该段 Ghidra 有重复／丢失；后果是 **`showTips`(param_4) 在恢复出的函数体里一次都没出现**，提示调用看着是无条件的。
- **`NeedsToChooseFromPool` 那个「池」到底是什么**（`rawCard+0x2a0`，与 oath(1275)／`GetManaLeft` 有关）与 `ChooseCardMethod` 给出什么选项，没读明白；只能确定它**不是** waystone 链的一部分。
- `bm+0x128`（`CanUseWaystone` 里喂给 `NotifyCantDoAction` 的对象）没坐实类型，按其他文件同名偏移应为 BattleTipController。
- 77 号动作（`AddTriggerSpiritStone`）的**上游调用点**在 dump 里也没有（「Trigger the abilities requiring Spirit Stones of your troops」那类执行器没被 dump）。

---

## 五、死灵那一半：残骸 → 复生（2026-09-25 补）

> 用户当天问「死灵会复活『残骸』状态的部队吗」。**结论：会，而且和灵族共用同一套底座。**

**底座【读】** —— `SupportMethods__HasToTransformIntoRemnant.c`：

```c
HasCurrentTrait(card, 0x41a = 1050 = remnant)  ||  HasCurrentTrait(card, 0x474 = 1140 = waystone)
```

⇒ **「灵族的灵魂石」和「死灵的残骸」是同一个机制的两个阵营皮肤**：带这两个 trait 之一的部队死亡时，
`AddTransformIntoRemnant`（`BattleActionType.transformToRemnant = 72`）→ `ResolveTransformIntoRemnant` →
`BattleCardUI.CreateRemnantBody` 在原位生成残骸体。区别**只在残骸体长什么样、以及谁能把它变回去**。

**「残骸」是横跨两个阵营的同一个棋盘状态**【读】：
`EntityScript.isRemnant // 0x65`（bool）= true；卡仍是**同一张 `CardScript`**、仍在原格位（`cardState == inPlay(2)`）、
**1 血 / 攻 0**，算「在场上」（`BattleManager.GetEnemyMinionsAndRemnantInPlay`）。
`remnantMaxHealth = 1`（`GameStaticData.cs:321` + `__.cctor.c` 该槽 `+0x298 = 1`）。

**死灵的复生【读】**：不是点击，是**卡的效果动词** —— `AbilityEffect.reanimate = 430` →
`AbilityLogic.PlayAbility` 的 `0x1ae` 分支 → `BattleManager.AddTransformFromRemnant`
（`BattleActionType.reanimate = 73`）→ `ResolveTransformFromRemnant` → `CardScript.TransformFromRemnant`；
残骸清 `isRemnant`、变回部队，并发 `AbilityTrigger.Reanimated = 570` 广播
（`BattleManagerSupport.BroadcastUnitReanimated` → 每张在场卡 `CardScript.ReactToUnitReanimated`）。
⚠️ **死灵没有第二套「复活」系统**：`Resurrect` / `LivingMetal` / `Rebirth` / `Repair` / `Revive` 在**全量签名桩
`d:/2/Warpforge_code/Scripts/Assembly-CSharp/` 里 0 命中**（唯一命中是 `RemnantNecrons.AnimationResurrectionEnd`）——
「复活」就是 `reanimate = 430` 这一条。

**残骸寿命【读】**：`SupportMethods.ShouldRemnantDestroyOnTurnEnd` = 死灵（`faction == 0x28`）**且** `isRemnant`
**且没有** `DefinedTrait.notDestroyRemnant = 1190` ⇒ **控制者回合结束时摧毁**
（`Nemesor Zahndrekh` 的「相邻的残骸不会在你的回合结束时消失」就是这个）。

**两边的卡池**（`MyGame/Assets/RuleEngine/Resources/cards_engine.json`，2026-09-25 实跑）：
`Waystone` 24 张 · **全 SaimHann**；`Remnant` 36 张 · 全 Sautekh；`reanimate` 15 张 · 全 Sautekh。

## 六、我方实现现状（2026-09-25 实测）

| 项 | 状态 | 落点 |
|---|---|---|
| 残骸状态位 | ✅ | `RuleEngine/Core/UnitState.cs:103 public bool IsRemnant` |
| 死亡 → 残骸（留格位、攻 0、1 血） | ✅ | `RuleEngine/Core/RuleCore.cs:2083-2095` |
| 残骸被摧毁才进弃牌堆 | ✅ | `RuleCore.cs:2050-2068` |
| 回合末摧毁残骸 | ✅ | `RuleCore.cs:1954 DestroyRemnants` ← `:641`（`EndTurn`） |
| 灵魂石货币 / 路标石阵亡 +1 | ✅ | `RuleEngine/Core/PlayerState.cs:61 SpiritStones` · `RuleCore.cs:2162`（`GainSpirit, Amount=1`） |
| `reanimate` 动词（从**场上**残骸翻回） | ✅ | `RuleEngine/Core/EffectResolver.cs:5788 DoReanimate`（候选①`:5795-5801`、墓地是退路） |
| `When Reanimated` 广播 | ✅ | `EffectResolver.cs:5830` |
| 灵魂石 HUD 计数条 + 收集闪光特效 | ✅ | `CardPresentation/Battle/BattleDriver.cs:4013/4017/4024` · `Core/VfxMap.cs:113 → "Waystone UI Gain"` |
| 卡面关键词图标（`waystone`/`remnant`/`SpiritStone_1..5`） | ✅ | `CardPresentation/Resources/Art/traits/` · `Core/Badges.cs:148` |
| 棋盘上的残骸体 / 灵魂石形态 | ✅ **2026-09-25 做了** | `CardView.SetRemnantBody`（盖一具 `RemnantBody3D <阵营>` + 关掉原卡卡身，照原版 `RemnantAeldari.Toggle` 那两步）+ `BattleDriver.SyncBoard` 驱动 + `RemnantPrefabOf` 按**关键词**挑哪一具；断言 12 条（`BattleScene` `29_残骸体_灵族与死灵` 那张实拍） |
| `useWaystone` 主动收集交互 | ✅ **2026-09-25 做了** | `RuleCore.CanCollectWaystone` / `CollectWaystone`（+1 颗 → 销毁残骸，走既有的「残骸被摧毁」那条路）+ `BattleDriver.OpenCommand` 拦截点击 + AI 那一支（`AiActionKind.CollectWaystone`，固定 50 分）；收集特效走新事件 `EvtKind.CollectWaystone` → `WaystoneCollect` |

**素材已备齐（只差接线）**：`WarpforgeVFX/Materials/{Spirit Stone, Spirit Stone Explosion, Spirit Stones Energy, Remnant Shatter, Necrons Remnant Up Glow}.mat` ·
`WarpforgeVFX/Meshes/{Spirt Stone, Spirt Stone 1, Card_Remnant_HO Optimization 2}.asset` ·
`WarpforgeVFX/Textures/Matcap Mod Spirit Stone.png` · `Resources/Art/audio/sfx/Waystone Trigger.wav`。

**表现层照抄的形状【读】**（`BattleCardUI__CreateRemnantBody.c` / `RemnantBody__BodyVisibilityToggle.c`）：
① 在卡的 transform 下实例化 `RemnantBody3D <阵营>`（Addressable，`rawCard +0xe0 → +0xe0`），**复制原卡卡身的 localScale**；
② `Initialize(card)` + `CardHighlight.SetRemnantHighlight`；
③ `BodyVisibilityToggle → BattleCardUI.ToggleBody3D(false)` **把原卡的 3D 卡身关掉**。
⇒ **既不是翻面、也不是卡背**（与 §文首 2026-09-19 那条更正一致）。

**✅ 2026-10-07 更正：这一条「代码/注释打架」已不成立（代码注释早就改过了，本文没跟）。**
原文：**「一处代码/注释打架（该按铁律 5 改）」**—— `EffectResolver.cs` 的 `DoReanimate` 头上那段注记仍写
「我们的引擎**没有「翻面」这个棋盘状态** ⇒ 这里拿**墓地**里死掉的单位当残骸」。
**实际**：那句注记**已经不在了**（`EffectResolver.cs:5800-5814` 现在是 🔴 2026-09-25 更正
「我方的做法就是原版的做法」+ ✅ 2026-10-04 更正「`IsRemnant` 0 命中 / 主动收集没做」两条都已销）。
`墓地` 这两个字现在只出现在**候选 ② 那条显式标注的退路**里（`:5876-5903`「**旧口径的退路**…
场上没有残骸时**退回墓地**，但**日志会说清**」）⇒ **代码与注释自洽**，无需再改。
🔑 **教训**：文档记「某处注释是旧的」之后，那句注释被改掉了 —— **本文这类指针也要跟着销账**。

### 六之三 · 🔴 **批处理下 Animator/模块不跑 ⇒ 残骸体的「静止长相」没验过**（2026-09-25 如实记）

> ⚠️ **2026-10-04 更正：本节标题与结论【已过期】—— 静止长相【批处理里就能并排核】，不必等真 Play。**
> 判据（2026-09-29 查实、2026-10-04 收口，全文 → `资料/待办判据_战场与战斗视图.md` 的 **Q1** 那条）：
> **静止态 = 那条 legacy clip 的 `t=0`**（灵族 `Idle/Spirit Stone Idle`，8s 循环、`PlayAutomatically=1`），
> 而 **`t=0` 与 prefab 里手写的 `localPosition`/`localScale` 逐位一致** ⇒ 批处理直接量 t=0 即可；
> 死灵那具 `Card Remnant` 在 prefab 里**本来就是 `m_IsActive: 0`**（连"跑不跑动画"都不涉及）。
> 出处：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/RemnantBody3D {Aeldari,Necrons}.json`
> + `AnimationClip/AnimationClip_-6016683441228044944.json` + `Mesh/Spirt Stone.obj`。
> **错因**：当时把「粒子/动画跑不起来」直接推成「长相核不了」，没注意到**静止态本来就有静态判据**。
> 下面这段原文保留作痕迹（「缩放差 ~13%」那条也在 Q1 里作废：原版抄的就是 `0.88586` 那一级，我们同口径）。

残骸体那两具 prefab **几乎全是粒子 + 动画**，而自检/截图是**批处理**（没有帧循环）：
- 我加了 `Step(0.35f)`（`BattleScene.Step` 里统一 `Simulate` 了粒子）⇒ **粒子那半看得到了**；
- **但 `Animator` 与 `AnimFX` 模块不跑** ⇒ 两处**只露出「第 0 帧」的样子**：
  · 死灵那具的 **`Card Remnant`（子物体）在 prefab 里就是 `m_IsActive: 0`**，运行时由 prefab 自己的
    动画/组件打开 —— 批处理里它是关的，所以截图里看到的是它的 **`Card 3D` 底**；
  · 灵族那具有一枚 `To remnant` 的**卡形网格**（material `Card 3d WH40K Explosion Green`、
    shader `Everguild/Cards/3D Card Explosion`）—— 名字和 shader 都像**过渡**用的，
    但**它是常显还是被动画收掉，没验**。
⇒ **要说的是**：这一族「**摆位/开关/缩放/材质绑定**」已经钉住了（那 16 条断言），
   「**静止时长什么样**」**没有**跟原版并排核过 —— 而原版已关服，**这台机器上核不了**。
  要核只能等真 Play 里肉眼对一遍（或将来拿到原版录像）。

### 六之四 · 改成两段式**有没有连带副作用**（2026-09-25 核过，结论：没有）

改动的实质：带 `Waystone.` 的单位死亡时**不再立刻进弃牌堆、也不再立刻进 `DeadUnits`/`DiedThisTurn`**，
而是留一具残骸；**那张卡要到残骸被摧毁（或收集）时才进弃牌堆**。
（⚠️ 死灵那半**本来就是**这个语义，这次只是把灵族并进同一条路。）

🔴 **唯一要担心的是那些「按死过谁计数」的卡** —— 全池 15 张（`died this game / this battle /
this turn / since your last turn`）。**逐张查过：没有一张是 SaimHann**（分属 AM/BL/DA/EC/GSC/**Sautekh**/Goff/Sororitas）
⇒ **灵族这 24 张卡的改动不波及任何一张**。
（`SAU47` / `SAU64` 那两张是死灵的，走的是**原本就有**的语义，这次没动它们。）

### 六之五 · 「引擎说它死了、可那一格还站着人」—— 与阵亡表现的接缝（2026-09-25 踩，已修）

`CleanupDeaths` 在**两条分支上都会**发 `EvtKind.Death`（「进弃牌堆」和「变成残骸」），
而 `BattleDriver.PlayDeathFeel` 收到 `Death` 就**把视图从 `_myUnits` 里摘掉、溶解、销毁**。
⇒ 残骸那一格：`SyncBoard` 刚给**同一张视图**盖上残骸体，0.85 s 后那条阵亡事件轮到播放，
又把它溶掉 ⇒ 残骸**闪一下没了、再新建一张视图**。

**修法**：`PlayDeathFeel` 开头加守卫 —— **棋盘那一格还有东西就不播消散**。
⚠️ 判据用「格子上还有没有东西」，**不用 `IsRemnant`**（那条事件排在延迟队列里，轮到播时棋盘可能又变过）。
**回归断言**：`BattleScene` 里注入一条 `Death` 事件，钉住「格子还有人 ⇒ `DyingCount` 不动」+ **反例**（格子真空了 ⇒ 照常消散）。
🔴 **写这两条时踩了两个「量法」坑，都记在 `已知的坑.md` 同条**：
① **`animateFeel` 默认 `false`** ⇒ `PlayFeel` 整段不跑、`PlayDeathFeel` 从没被调 **⇒ 正向那条是假绿**
（是**反例**把它戳出来的；写这一族的断言前**先临时打开 `animateFeel`**，跑完关回去）；
② **必须在 `Step` 之前量**、且判据要取**视图对象身份**（`ReferenceEquals(旧视图, BoardViewAt(slot))`）——
「残骸体可见吗」在「被溶掉又重建」时**照样是 true**。
完整版 → `资料/已知的坑.md`「自检里阵亡/命中那一下的手感类断言默认是空跑」那条。

### 六之二 · 改成两段式**有没有连带副作用**（2026-09-25 核过，结论：没有）

改动的实质：带 `Waystone.` 的单位死亡时**不再立刻进弃牌堆、也不再立刻进 `DeadUnits`/`DiedThisTurn`**，
而是留一具残骸；**那张卡要到残骸被摧毁（或收集）时才进弃牌堆**。
（⚠️ 死灵那半**本来就是**这个语义，这次只是把灵族并进同一条路。）

🔴 **唯一要担心的是那些「按死过谁计数」的卡** —— 全池 15 张（`died this game / this battle /
this turn / since your last turn`）。**逐张查过：没有一张是 SaimHann**（分属 AM/BL/DA/EC/GSC/**Sautekh**/Goff/Sororitas）
⇒ **灵族这 24 张卡的改动不波及任何一张**。
（`SAU47` / `SAU64` 那两张是死灵的，走的是**原本就有**的语义，这次没动它们。）
