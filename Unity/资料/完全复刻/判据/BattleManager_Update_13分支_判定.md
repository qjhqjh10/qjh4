# `BattleManager.Update` 逐分支判定（R8 · 2026-10-27 第八十七会话）

> **作业来源**：`R6` 核 `战斗界面_HUD` 时点名「最贵的一条没人做」——原版 `BattleManager.Update` 无人逐条对过。
> **本文是那一格的判据正本**。账行 → `账/_parts/B87_R8_Update分支.tsv` · 报告 → `报告/R8_Update13分支.md`。

---

## 一、判据来源与取证方法（含一处旧结论的订正）

### 1.1 方法体用了哪三份

| 用途 | 文件 | 行数 | 说明 |
|---|---|---|---|
| **控制流主源**（switch 已解、行号以此为准） | `d:/2/tools/decomp_full/BattleManager__Update.c` | **1416** | 首行 = `void BattleManager__Update(longlong param_1)`，**是它自己**（本仓抓到过「文件名与内容不符」，已核） |
| **字段名 / 条件化简** | `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs` | `25427-26736` | ⚠️ 这一份**比 ILSpy 版完整得多**；ILSpy 版（`out_cpp2il_dev_ilspy/BattleManager.cs:47988`）**把主 switch 整段丢了**（`Indirect jump … should have been resolved before IL gen`）⇒ 只拿它当字段名旁证 |
| 字段偏移 → 名字 | `d:/2/tools/il2cpp_out/dump.cs` | `BattleManager` 起 `:30709` | `+0x290` = `UIstate` · `+0x330` = `cardTracked` · `+0x410` = `cardSelected` · `+0x440` = `targetCard` · `+0x338` = `targetedUnitTarget` · `+0x25c/0x268` = `duringBattleFlag / duringScriptedActionsFlag` · `+0x274` = `playerTurnFlag` · `+0x280` = `resolvingActionFlag` · `+0x510` = `matchFinishedAndWaitingToLeave` · `+0x598` = `showingDeckSize` |

### 1.2 两个 `FUN_` 已经查实（**旧文档说「判不死」的，就地订正**）

`Update` 里每一档都在读 `FUN_180617800()`，之前一直被记成「没有反编译产物、判不死」
（`完全复刻/报告/A2_手牌目标型卡点选目标.md:199` · `普查产出_1025/AU1:266` · `AU2:200`）。

**本轮用机器码把它坐实了**（工具：`d:/4/_tmp_view/r8_20261027/pe_probe.py`，按 PE 节表把 VA 换成文件偏移后读字节）：

```
VA 0x180617800                     ; FUN_180617800
  48 83 ec 28            sub  rsp, 0x28
  80 3d .. 00 75 13      cmp  byte[init_guard], 0 / jne
  48 8d 0d ..            lea  rcx, [class_slot @ 0x1842C22E8]
  e8 ..                  call il2cpp_class_init
  48 8b 05 ..            mov  rax, [class_slot]        ; Il2CppClass*
  48 8b 88 b8 00 00 00   mov  rcx, [rax+0xB8]          ; static_fields blob
  0f b6 41 08            movzx eax, byte [rcx+0x08]    ; ← 静态字段 +0x8（bool）
```

`VA 0x180789A30`（同一份 Update 里 `GetClosestAvailableSlot` 的实参）读的是**同一个 class_slot**、
偏移 `+0x0 / +0x4`（一个 `Vector2`）：

```
  48 8b 05 ..            mov  rax, [class_slot @ 0x1842C22E8]   ; 与上面**同一格**
  48 8b 88 b8 00 00 00   mov  rcx, [rax+0xB8]
  f3 0f 10 41 04         movss xmm0, [rcx+0x04]
  f3 0f 10 09            movss xmm1, [rcx+0x00]
```

对照 `dump.cs` 的 `TouchInputManager`（`:89641`）——**静态字段布局逐格吻合**：

| 静态字段 | 声明偏移 | 上面两条 thunk 读的 |
|---|---|---|
| `TouchPosition`（`Vector2`） | **0x0** | `+0x0 / +0x4` |
| `TouchPressed`（`bool`） | **0x8** | `+0x8` |
| `TouchPressedSecondary`（`bool`） | 0x9 | （没人读） |

再取**写入侧**一锤定音 —— `TouchInputManager__Update.c:128-136`：

```
uVar6 = UnityEngine_Input__GetMouseButton(0);
*(undefined1 *)(*(longlong *)(DAT_1842bd2e8 + 0xb8) + 8) = uVar6;   ; TouchPressed = Input.GetMouseButton(0)
```

（`DAT_1842bd2e8` 就是上面那个 `0x1842C22E8`。）

> **结论（可直接引用）**
> - `FUN_180617800` = **`TouchInputManager.get_TouchPressed`** = 「**左键此刻按着**」（每帧由 `Input.GetMouseButton(0)` 快照）。
> - `FUN_180789a30` = **`TouchInputManager.get_TouchPosition`**（静态 `Vector2`）。
> - ⚠️ `dump.cs` 里 `get_TouchPressed` 的正身写作 `RVA 0x79C8E0`（`get_TouchPosition` 写作 `0x79C850`），
>   **与这两个 thunk 的地址不同** ⇒ 这两个是 IL2CPP 另外发出来的**同一属性的短体/重复体**。
>   「地址在 dump.cs 里查不到」**不等于**「没有判据」——本仓 `普查产出_1015战斗/B3_战斗输入交互矩阵.md:29` 早就是这么判的（结论正确），
>   而 `A2` / `AU1` / `AU2` 三处**没看到 B3**、各自记成「判不死」⇒ **本条按 B3 口径订正，那三处应同步删掉「判不死」的说法**（不改 `.cs`，见报告 §四）。

⇒ 于是本文所有「按住 / 松手」的判别式都是**已定判据**，不再是推定：
`if (TouchPressed())` = 按住；`if (!TouchPressed())` = 松手。

### 1.3 用到的常量（都是现读，不是估的）

| 符号 | 值 | 用在哪 | 出处 |
|---|---|---|---|
| `DAT_1834b2da8` | **10.0f** | `:199` 与 `minMovementToPlayCard` 相乘 | `工具/read_literal.py` |
| `DAT_1834b2bc8` | **-1.0f** | 拖拽中卡的 z | 同上 |
| `DAT_1834b2e98` | **57.2958**（Rad2Deg） | 相机欧拉角换算 | 同上 |
| `DAT_1834b3164` | **15.0f** | 回放快进 `Time.timeScale` | 同上 |
| `VarsDesktop.targetYOffset` | **0.0** | `GetTargetInPos` 前给指针 y 的补偿 | `assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/VarsDesktop.json` |
| `VarsDesktop.moveCardYOffset` | **0.2** | 拖拽中卡/准星的目标 y 补偿 | 同上 |
| `VarsMobile.targetYOffset` / `moveCardYOffset` | **0.2 / 0.7** | 移动档 | `…/VarsMobile.json` |
| `VarsGlobal.minMovementToPlayCard` | `+0x7C` | 判「推上线够不够」 | `dump.cs` `VarsGlobal` |
| `VarsGlobal.maxHeightOfUnplayableCard` | `+0x80` | 判「太高了、打不出」 | 同上 |
| `VarsGlobal.playerActionPriority` | `+0xB4` | `AddAutoActionToQueue` 的 priority | 同上 |

### 1.4 分支条数：**18 段，不是 13 段**（就地订正 R6 的说法）

原版 `Update` 的 `switch` 只有 **9 个 `case` 标签**，覆盖 **10 个** `UIstate` 值
（1 normalBattle · 2 movingHandCard · 3 playingMinion · 4 playingTargetedSpell · 5 playingNonTargetSpell ·
6 minionAttacking · 8 postBattle · 9 choosingUnitTarget · 13 choosingAbilityTarget · 10 targetedActiveAbility）。
`UIstate ∈ {0 inactive, 7 finishBattleSequence, 11 mulligan, 12 mulliganFinished, 14 choosingResolutionCard}`
**没有 case**，直接落到 switch 之后的动作队列泵。
但 `switch` **之外**还有 8 段各自独立的每帧逻辑（初始化闸 / 连接闸 / 鼠标取位 / 牌数收起 / 音效泵 / 手牌上限线 / 队列泵 / ESC / 回放键位）。
⇒ **可核的单位是 18 段**（下表 B01–B18）。「13 条分支」这个数字以后别再抄。

---

## 二、18 段总表

判定口径：**A** = 已复刻且一致 · **B** = 待核（写清差什么）· **C** = 真缺口。
「我们的落点」全部是**本批现读**的行号（`d:/4/Unity/MyGame/Assets/`）。

| # | 原版行号 | 做什么（逐句压缩） | 我们的落点 | 判定 | 差什么 / 备注 |
|---|---|---|---|---|---|
| **B01** | `:97-103` | `status != Initialized(2)` ⇒ 整帧返回；`gameTime(+0x4E0) += Time.deltaTime` | `BattleDriver.cs:14129-14130`（`PollInputEdges()` 在闸**之前**、`Ctx == null` 返回）；**`gameTime` 无** | **A**（行为） | 我们**没有 `gameTime` 累加器**。全库反编译里 `get_gameTime` 在 `BattleManager` 之外**0 命中** ⇒ 无外部消费者 ⇒ 无行为差异（低优先，见报告 §三） |
| **B02** | `:104-115` | `currentConnectionStatus ∉ {Connected(0), Disconnected(3)}` ⇒ 若 `!matchFinishedAndWaitingToLeave` **整帧返回**；否则 `OnConnectionStatusChanged(status)` 回调 + 状态写回 0 + 返回 | `BattleDriver.cs:14189`（`PlayerInputFrozen && !Ctx.IsOver ⇒ UpdateHud(); return;`） | **A** | 判据与映射（`NetClockPaused` ← `NetBattle.ClockPaused`）已写在 `:14163-14188` 的大注释里，逐句对得上；对手掉线期弹窗走 `NetRuntime`，与这条闸**并行**、不是同一件 |
| **B03** | `:116-122` | `matchFinishedAndWaitingToLeave && (左键按下沿 ‖ ESC)` ⇒ `Log("Clicked after match finished. Leaving scene")` + `LeaveBattle()` + 返回 | `BattleDriver.cs:14259 HandleEndBattleExit()`（`:12620`） | **A** | 这条是「终局后点一下就离开」；与 case 8 是**同一动作的两个入口**，原版也各写一份 |
| **B04** | `:123-139` | ① `mouseCanvasPos(+0x434/0x43C) = GetMousePosInWorldSpaceCanvas()` ② `targetCard(+0x440) = GetTargetInPos(mouse.x, mouse.y + desktopVariables.targetYOffset, mouse.z)`（**桌面档 targetYOffset = 0**） | `BattleDriver.WorldPointer()` + `TickBoardHover(:33990)` / `HitSlot` / `HitMyDeckPile`；**不存 `targetCard` 字段** | **A**（桌面档）· **B**（移动档） | ① 我们**没有单一 hover 目标字段**，每个消费者各查一次（结构差异、行为等价）② 移动档 `targetYOffset = 0.2` 未做（我们恒按桌面档 0 处理） |
| **B05** | `:140-151` | `showingDeckSize(+0x598) && !TouchPressed()` ⇒ `Log("StopShowingDeckSize")` + 两侧 `DeckManager.DisplayDeckSize(false)` + 两侧 `PlayerHand.ShowHandSize(false)` + `showingDeckSize = false` | `BattleDriver.cs:17063-17116`（`CardHoldPeek` → `peek` → 三组 `SetVis`） | **A** | 收起的另半个条件**现已坐实 = `!TouchPressed`**（§1.2）⇒ 现存实现（`PointerHeldRaw()`）与判据**同义**；之前记的「另半个条件没反编译出来」可以划掉 |
| **B06** | `:152` | `PlayNextSoundInQueue()`（每帧推进一条音效） | `BattleSfx.cs:290 Tick(dt)` / `:295 DrainOne()` + `Install()` 的 PlayerLoop 泵 | **A** | 串行档、`timeToPlayAgain` 节流、队列上限 256 都是我们自己的兜底（已出声） |
| **B07** | `:153-158` | 取 `playerHandPosLimitObj(+0x180).transform.position` 存进 `local_5a8` —— 后面各档的「手牌上限线 y」 | `CardInteraction.cs:1025`：`LayoutSpace.ToNormalized(world).y < BoardLayout.HandLimitY` | **A** | 原版取场景节点 `HandLimitArea` 的 transform；我们用常量 `HandLimitY`（= 300/1080，`A2104①` 已核 ⇒ 同一根线）。⚠️ 原版 `:341-347` 的让位是**无条件**跑的，我们那道 y 守卫是**自己加的**（`CardInteraction.cs:1010-1020` 已如实标） |
| **B08** | `:160-190` **case 1** normalBattle | `if (TouchPressed())` �� `targetCard != cardSelected ‖ targetCard == null` · `!waitingToResolveActiveAbilityFlag(+0x418)` · `IsPlayerTurn()` · `(IsTutorialStageAvailable ‖ IsCampaignStageAvailable)` · `cardSelected != null` ⇒ `AiScripted.CheckIfWaitingToUseAbility(this, GetCurrentTurnScriptedData(), cardSelected)`；返回真 ⇒ **只打一条 `LogWarning`** | `BattleDriver.TutorialFrameGuard(:16582)` 只覆盖了 `CheckIfWaitingToChangeAttack` 那一半；**`CheckIfWaitingToUseAbility` 无对应** | **C**（低危） | 原版这一支**通篇只有一条 LogWarning，零局面影响**（`AiScripted__CheckIfWaitingToUseAbility.c:118-131`：解析出的卡为 null 或 == `cardSelected` ⇒ return 1 ⇒ 上层 LogWarning）。⇒ 缺的是**一条诊断**，不是行为。施工建议见 §三 C-1 |
| **B09** | `:191-349` **case 2** movingHandCard | 松手：`线y < 指针y` 且够高 ⇒ `CanPlayCard(showTips=1)` 失败则 Log；然后**一律** `RemoveSwarm` + `cardTracked.MoveBackToHand()` + `GetClosestInHandSlot(指针)` + `HandCardMovedToNewIndex`(+`SendCardReorder`) + `ReassembleMinions` + `HandCardReleased` ⇒ 状态 1。按住：① 过线且 `CanPlayCard(showTips=0, initiate=1)` ⇒ `MoveIntoFieldFromHand` + 状态改 3/5/4 + `PlayMinionFromHand` 音 ⇒ `StartMovingHandCard` ② 仍在状态 2 且过高 ⇒ `CanPlayCard(showTips=1)` ⇒ 回手 ③ 拖拽视觉：`SetCardMovement(指针 x, 指针 y + moveCardYOffset(0.2), -1)` + `DisplayHandWithCardSpace` | `CardInteraction.cs:612 Update` / `:973 UpdateDrag` / `:1355 Release`；`BattleDriver.cs:5612 CanDropAtSlot`（→ `RuleCore.CanPlayCard`）；`CardInteraction.cs:1112 DropReject` | **B** | ① **`moveCardYOffset = 0.2`（桌面档）没做** —— 我们用「抓取偏移 `_grabOffset`」（`CardInteraction.cs:528/889/984`，🔶 自己设计）② 原版是**拖动过程中**用 `showTips=1` 的两段式 `CanPlayCard` 出声（`:195-206` / `:289-308`），我们**只在松手**时经 `DropReject` 出词条 ⇒ 出声时机不同 |
| **B10** | `:350-625` **case 3** playingMinion | 松手：① 教程局且 `cardTracked+0x40 != 0` ⇒ `CheckIfPlayerActionPermittedInTutorial(5, …)`，不合格 ⇒ `MoveBackToHand` + 回手位 + `ReassembleMinions` + 状态写 1 + return ② `RemoveSwarm` + 组 `BattleAction{card, kind 5, 1, …, 指针}` + `AddAutoActionToQueue(action, globalVars.playerActionPriority)` + `CardWaitingToBePlayed`。按住：`SetCardMovement(指针 + moveCardYOffset)` + `ReassembleMinionsWhilePlayingUnit(指针)`；`HasCurrentTrait(0x28A)` 时做蜂群预览（`GetClosestAvailableSlot` → `GetCardInPos` → `DisplaySwarm` → `RemoveSwarm`） | `CardInteraction.Release(:1355)` → `BattleDriver` 的落点提交（`CanDropAtSlot(:5612)` → `LocalAct`） | **B** | ① 同 B09-① （`moveCardYOffset`）② **蜂群（swarm）预览**这一支（`:581-613`，trait id `0x28A = 650`）我们**没有核对过**有没有等价物 —— 待核，见报告 §三 |
| **B11** | `:626-830` **case 4** playingTargetedSpell | 松手：① `cardTracked != null` 且 `targetCard != null` 且 `IsValidSpellTarget(cardTracked, targetCard)` ⇒ 组 action + `AddAutoActionToQueue` + `CardWaitingToBePlayed`；否则 ⇒ Log 目标名 + `cardTracked.MoveBackToHand()` + 按 `isPlayer` 选手牌区 `HandCardReleased` + `NotifyCantDoAction("Battle/Tips/DragToTarget")` ② 之后一律 `StopTargetedSpellAnim` + `StopTracking(true)`。按住：`线y < 指针y` ⇒ `MoveCrosshair(playerManager.heroCard.transform.position, 指针 + moveCardYOffset)`；否则 `OutFromPlayingAreaAnim` + `StopTracking` + `StopTargetedSpellAnim` + `StopShowingPotentialTargets` + 状态写 **2** | 落点判定 + 快捷路：`BattleDriver.cs:8081-8103`（`IsValidSpellTarget` 等价 = `pool.Contains` + `SpellTargetBoardOwner`）；取消词条：`CardInteraction.DropReject` → `Battle/Tips/DragToTarget`（`BattleDriver.cs:7793`） | **C** | **拖拽期中版每帧画一根「督军 → 指针」的准星（`:800-820`），我们完全没有**：`UpdateReticle(:15867)` 要求 `_selectedSlot >= 0` 且锚点是**选中单位**，而 `DrivePlayerTurn` 在 `interaction.IsDragging` 时**先 return**（`:14774`）⇒ 拖目标型法术时屏上一根准星都没有。同时 `moveCardYOffset` 也缺（锚点的终点要用它） |
| **B12** | `:831-985` **case 5** playingNonTargetSpell | 松手：`cardTracked != null` ⇒ 组 action（`指针`）+ `AddAutoActionToQueue(…, playerActionPriority)` + `CardWaitingToBePlayed`；否则 `StopShowingPotentialTargets`。按住：`指针y > 线y` ⇒ `SetCardMovement(指针 + moveCardYOffset)`；否则 `OutFromPlayingAreaAnim` + 状态写 2 | `CardInteraction.Release(:1355)` → 提交（非目标法术直接结算） | **B** | 同 B09-①（`moveCardYOffset`） |
| **B13** | `:986-1096` **case 6** minionAttacking | 闸 `playerTurnFlag(+0x274)`，假 ⇒ 状态写 1 收工。按住 ⇒ `MoveCrosshair(cardTracked.transform.position, 指针 + moveCardYOffset)`。松手 ⇒ ① `cardTracked.attackType(+0x120) == 4 (Active)`：`IsValidActiveAbilityTarget` + `!WaitingForBlockingAction(true)` + `allowPlayerActionsFlag(+0x244)` ⇒ `ExecuteActiveAbility` + `StopTracking(true)` ② 否则 `IsValidAttackTarget` + `!WaitingForBlockingAction` + `allowPlayerActionsFlag` + `!AiScripted.CheckIfWaitingToChangeAttack` ⇒ `AddAttackAction(…, playerActionPriority)` + `StopTracking(true)` ③ 都不成 ⇒ `CancelAttack(true)` + `StopTracking(false)`；末尾 `StopShowingPotentialTargets(isAttack)` + 状态写 1 | `BattleDriver.cs:14849-14854`（`_selectedSlot >= 0` ⇒ 每帧 `UpdateReticle`）/ `:14886-14905`（④ 松手选目标 ⇒ `Resolve(_command, t)` 或 `CancelTargetChoosingByInvalidClick()`）/ `:15473 CommitCommand` / `:16582 TutorialFrameGuard`（= `CheckIfWaitingToChangeAttack` 那一半） | **A** | 三处**结构性差异**（不是缺陷）：① 我们的「选打法」是**抬起触发**（`selector.HoverPickReady`，`:14807-14813`）而原版是 `UnitOnBoardAttackTypeSelector` 的按钮回调 ② 我们的「点槽外取消」是自造的（`AttackSelector` 文件头 `A462 §四·3` 已记）③ `StopShowingPotentialTargets(isAttack)` 的 `isAttack` 实参我们没区分（恒走 `ClearTargetHighlights`） |
| **B14** | `:1097-1102` **case 8** postBattle | `左键按下沿 ‖ ESC` ⇒ `LeaveBattle()`（**无其它闸**） | `HandleEndBattleExit(:12620)`：`ExitReady` ⇒ 读边沿 ⇒ `LeaveBattle()` | **A** | 我们多一道 `ExitReady`（= 开门演出闸）。原版这一 case 只会在状态 8 执行，而状态 8 **是开门演出之后**才进的（状态 7 = finishBattleSequence）⇒ 语义等价 |
| **B15** | `:1103-1185` **case 9 + 13** choosingUnitTarget / choosingAbilityTarget | 按住：`targetCard != null` ⇒ `cardJustClickedAsTarget(+0x588) = targetCard`；**状态 13** ⇒ `FinishedChoosingAbilityTargetByClick(targetCard)`；**状态 9** ⇒ 若 `targetCard != targetedUnitTarget(+0x338)` 且 `targetedUnitTarget.rawCard(+0x28).CanBeTargetedBy(targetCard)` ⇒ `targetedUnitTarget = targetCard`（+Log）。松开：`MoveCrosshair(targetedUnitTarget.transform.position, 指针 + moveCardYOffset)` | `BattleDriver.cs:14872`（`AwaitingPlayTarget ⇒ TryResolvePlayTarget`）/ `:8205 TryResolvePlayTarget` / `:8218 ResolvePlayTargetByUnit`；技能目标 = `CancelTargetChoosingByInvalidClick(:14715)` + `SkillPanelConfirmRelease` | **B** | ① **状态 9 的「点一下只是筛 `targetedUnitTarget`」这一层我们没有**：我们是「点谁 = 目标是谁」直接结算；原版那一支带 `CanBeTargetedBy` 谓词（本仓**无该谓词**，grep `CanBeTargetedBy` 0 命中）—— 待核它到底影响哪张卡 ② 该 case 的准星锚点 = `targetedUnitTarget`，我们 = 选中单位 ③ `moveCardYOffset` 缺 |
| **B16** | `:1186-1248` **case 10** targetedActiveAbility | 松手：先 `状态 = 1`；`targetCard == null` ⇒ `NotifyCantDoAction("Battle/Tips/InvalidTarget")` + `CancelAbilityTargeting`；否则 `cardSelected.IsValidActiveAbilityTarget(target)` 且 `IsValidActiveAbilityTargetTutorial` ⇒ `ExecuteActiveAbility`，不合法 ⇒ 同上失败支；之后 `StopTracking(true)` + `StopShowingPotentialTargets(false)`。按住：`MoveCrosshair(cardSelected.transform.position, 指针 + moveCardYOffset)` | `CancelTargetChoosingByInvalidClick(:14715-14738)`（逐句照 `FinishedChoosingAbilityTargetByClick.c:20-34`）+ 技能面板 uGUI 松手（`SkillPanelConfirmRelease`） | **A** | 只有 `moveCardYOffset` 一处缺（准星终点） |
| **B17** | `:1250-1301` 四条动作队列泵 | `(duringBattleFlag(+0x25C) ‖ duringScriptedActionsFlag(+0x268)) && !resolvingActionFlag(+0x280)` ⇒ 依次看 `interruptQueue(+0x368)` → `instantQueue(+0x358)` → `delayedInstantQueue(+0x360)` → `actionQueue(+0x350)`，取**第一个非空**的 `ResolveActionFromQueue` ⇒ **每帧最多一条** | `RuleEngine/Core/ActionPump.cs:75-193`（`ActionScope`：进入先泵、退出**泵到空**）+ `RuleEngine/Core/ActionQueue.cs:200-215`（消费序 2→1→3→0） | **A**（等价） | ① 「每帧一条」vs「动作边界排空」是**已记的设计等价**（`ActionPump.cs:20-40`：帧间隔在纯规则引擎里不可观测）② **`duringBattle / duringScriptedActions` 那道外层闸我们没有**（我们的等价物 = 开局前没人入队）。⚠️ 不要为它加闸：加了会让换牌期的入队不结算 |
| **B18** | `:1303-1335` ESC ⇒ `voiceLines.ClickChat()` | `ESC 按下沿 && UIstate != postBattle` 且 `!blockingOverlay.isSpinning(+0x4C)` 且 `voiceLinesController.IsPopUpActiveAndEnabled(+0x58.isActiveAndEnabled)`：状态 ∈ {0,7,8,11,12} ⇒ 只 `Log("ClickChat but not during battle " + 枚举名)`；状态 ∈ {1,6,9,10,13} ⇒ `VoiceLinesController.ClickChat()`（= 聊天按钮那一下：`Hide()` 后回调 `+0x30`） | ESC 的两个现有消费者：`HandleSettingsEsc(:6056)`（`BattleHud.Update` 那一条，**另一颗组件**）· `HandleChatPopup`（ESC **关**气泡） | **C** | **「ESC = 按一下聊天按钮（开语音弹窗）」这一整条没有**，含它那道状态掩码（`0x402C` ⇒ 屏蔽 {2,3,5,14}，再排除 4）。施工建议 §三 C-2 |
| **B19** | `:1336-1413` 回放局键位（`matchType == 0xA0`） | `A`(0x61)/`←`(0x114) ⇒ 重载当前场景（**重播同一份**，且 `PlayerDataManager.isAltTraitConfig = replaymatchData.GameplayData.alternativeTraitLogic`）；`D`(0x64)/`→`(0x113) ⇒ Time.timeScale = 15` + `replayActionsInQueue(+0x51C)++` +（`≤1` 才 `shouldNotResetTimeScale(+0x520)=true` 且 `StartCoroutine("AddNextReplayAction")`）；`Space(0x20)` ⇒ 切 `isReplayPlayActive(+0x521)` + `Start/StopCoroutine("ReplayPlayAction")` | `HandleTimeControlKeys(:8673)`：`Space` = 暂停/继续 · `.` = 单步 · `R` = 重开（`Restart()`，`:14269`）；回放条四颗钮 `ReplayBar`（`Replay/Play/Pause/Step`） | **C** | ① **`A`/`←` 与 `D`/`→` 都没有**（全仓 `Time.timeScale` **0 处**；`D` 的 15× 快进整条缺失）② `Space` 语义不同：原版 = 自动播放开关，我们 = 暂停/继续 ③ `.` 与 `R` 是 🔶 自设计键（`HandleTimeControlKeys` 的 doc 已自认「不是复刻」）。施工建议 §三 C-3 |

判据统计（18 段，互不重叠）：**A 11 段 · B 4 段 · C 4 段**。

- **A（11）**：B01 · B02 · B03 · **B04（桌面档）** · B05 · B06 · B07 · B13 · B14 · B16 · B17
- **B（4）**：B09 · B10 · B12 · B15　（另：B04 的**移动档**那一半也是 B，见 §四 B-5）
- **C（4）**：**B08**（教程诊断，最低危）· **B11**（拖目标型法术没准星）· **B18**（ESC⇒ClickChat）· **B19**（回放局键位）

---

## 三、C 清单与施工建议（按严重度）

### C-1 `B08` 教程「等玩家用主动技」的诊断 —— **最低危**，原版零行为

- **缺的行为**：`case 1`（normalBattle）里，当玩家按着鼠标、悬停的卡 ≠ 已选的卡、且不在等主动技结算、轮到我方、且当前是教程/战役关：
  调 `AiScripted.CheckIfWaitingToUseAbility(this, GetCurrentTurnScriptedData(), cardSelected)`；
  返回真 ⇒ `CustomDebug.LogWarning(...)`。
- **原版证据**：`BattleManager__Update.c:160-190`（调用点）· `AiScripted__CheckIfWaitingToUseAbility.c:118-131`（返回 1 的两种情形：解析出的卡为 `null`，或 == `cardSelected`）。
- **性质**：**通篇只有一条 LogWarning，不改任何状态** ⇒ 玩家看不到差异。按铁律 11 仍要补（只是排在最后）。
- **建议落点**：`BattleDriver.TutorialFrameGuard`（`BattleDriver.cs:16582`）旁边新开 `TutorialWarnWaitingUseAbility()`；
  谓词照 `TutorialRules.IsWaitingChangeAttack` 的形状新建 `IsWaitingUseAbility(turn, actionCounter)`（`RuleEngine/Core/TutorialScript.cs:1828` 是它的邻居，照着写）。
- **验收口**：`BattleScene.Run` 里加一条断言 —— 把 `EscapePressedForTest` 那套「钉死口」照搬成 `TouchPressedForTest`，钉成「按住」+ 教程第 N 关 + `_selectedSlot`/`_command` 摆好后调一次，断言**日志出现过那条 warning**（断言文案搜 `[Tutorial] 等待使用主动技`）。

### C-2 `B18` ESC = 按聊天按钮（语音弹窗）

- **缺的行为**：`ESC` 在 `UIstate ∈ {1,6,9,10,13}` 且 `!blockingOverlay.isSpinning` 且 `voiceLinesController.IsPopUpActiveAndEnabled` 时，
  **等价于点了聊天按钮**（`VoiceLinesPopupSelector.ClickChat` = `Hide()` + 回调）。`UIstate ∈ {0,7,8,11,12}` 时**只打一条日志**。
- **原版证据**：`BattleManager__Update.c:1303-1335`（掩码 `0x402C` 现读：位 2/3/5/14 ⇒ 屏蔽状态 2/3/5/14，再 `!= 4`）。
- **为什么容易漏**：ESC 在原版有**两个**消费者 —— `BattleHud.Update:20-35`（开设置窗，我们已经做了）与本条（开聊天）。
- **建议落点**：`BattleDriver.Update` 里紧挨 `HandleSettingsEsc(:14277)` 之后新开 `HandleChatHotkeyEsc()`；
  动作直接复用 `ClickChatHud()`（`BattleDriver.cs:5891`，那已经是聊天按钮的唯一落点 ⇒ 不要另写一份）。
  ⚠️ 顺序：`HandleSettingsEsc` 先判（它自己带 `HudClickBlocked`），这条在后 —— 否则一次 ESC 开两扇窗。
- **验收口**：`BattleScene.Run` 新增断言 —— `EscapePressedForTest = true` + 正常战斗状态 ⇒ 断言 `_chatPopup.Visible` 翻转一次；再摆成 `postBattle` ⇒ 断言**没有**翻转（等价原版那道 `!= postBattle`）。

### C-3 `B19` 回放局键位（`D`/`→` 15× 快进、`A`/`←` 重播、`Space` 语义）

- **缺的行为**：
  1. `D`/`→` ⇒ `Time.timeScale = 15` + 排一条「下一条回放动作」；已排过（`> 1`）就只加计数。
  2. `A`/`←` ⇒ 重播同一份录像（原版是 `LoadScene(当前场景名)`，并先把 `isAltTraitConfig` 从录像数据里恢复）。
  3. `Space` ⇒ **自动播放开关**（`isReplayPlayActive`），不是暂停。
- **原版证据**：`BattleManager__Update.c:1336-1413`；`Time.timeScale = DAT_1834b3164 = 15f`（§1.3 现读）。
- **我们现状**：`HandleTimeControlKeys(:8673)` 只有 `Space`(暂停) / `.`(单步) / `R`(重开) —— 三个都是 🔶 自设计键，
  回放条四颗钮（`ReplayBar`）覆盖了 Replay/Play/Pause/Step，但**速度档（15×）完全不存在**。
- **建议落点**：
  - `D`/`→`：`BattleDriver.HandleTimeControlKeys` 里加一档 ⇒ `SetReplaySpeed(15f)`（新方法，管 `Time.timeScale` 的**唯一**读写口，
    ⛔ 别散写 —— 现在全仓 0 处写 `timeScale`，所以这个口是干净的）；回放退出时必须复位 1。
  - `A`/`←`：复用 `Restart()`（`ApplyPendingReplay` 那条，回放局里它已经「重播同一份」）。
  - `Space`：**与现有语义冲突** ⇒ 需要用户/调度台裁一句「键盘 Space 按哪个」；本报告的建议是**按原版**（自动播放开关），
    把「暂停/继续」留在回放条的 `Pause/Play` 钮上（原版本来就是钮）。
- **验收口**：`BattleScene.Run` —— ① 回放局按 `D` ⇒ 断言 `Time.timeScale == 15f`；② 退出回放 ⇒ 断言回 1；
  ③ 按 `A` ⇒ 断言 `_replayRec` 的起始条件指纹与按之前**逐字相同**（「重播同一份」的判别式）。

---

## 四、B 清单（待核，写清差什么）

| # | 待核的问题 | 已知 | 还差什么 |
|---|---|---|---|
| B-1 | `moveCardYOffset = 0.2`（桌面档）在 **5 条分支**（case 2/3/4/5/6/9/10/13 的拖卡与准星终点）都没做 | 值已现读（`VarsDesktop.json`）；`0.2` 世界单位 ≈ 108 px/单位 × 0.2 = **21.6 设计 px** | ① 我们的 `_grabOffset`（🔶 自设计）与原版「指针 + 固定 y 偏移」的**手感差**要不要保留 ② 一次改一处还是统一改（建议统一成一个 `PointerAimY()` 口） |
| B-2 | case 3 的**蜂群预览**（`HasCurrentTrait(0x28A=650)` → `GetClosestAvailableSlot` → `GetCardInPos` → `DisplaySwarm`/`RemoveSwarm`） | 原版 `:581-613` 逐句已读 | 我们有没有等价物（grep `DisplaySwarm` / trait 650 **未做**）⇒ 下一步查 `CardView`/`BoardLayout` 有没有「把同格叠着的单位铺开」这东西 |
| B-3 | case 9 的 `CanBeTargetedBy` 筛选层 | 原版 `:1144-1159`；本仓 grep `CanBeTargetedBy` **0 命中** | ① 原版 `RawCardScript.CanBeTargetedBy` 的方法体（`decomp_full/RawCardScript__CanBeTargetedBy.c` 有没有）② 它到底影响哪些卡（是不是「点一下只是换 `targetedUnitTarget`」那条） |
| B-4 | case 2 的两段式 `CanPlayCard(showTips)`（拖动中出声 vs 松手出声） | 原版 `:195-206` / `:289-308`；我们的 = `CardInteraction.DropReject` 松手时才出词条 | 词条时机是否要提前到「拖动中」；提前的话 `DropReject` 要能每帧跑（现在只跑 `UpdateDrag` 一次） |
| B-5 | `B04` 移动档 `targetYOffset = 0.2` | 桌面档 0 ⇒ 今天零差异 | 我们到底跑不跑移动档；跑的话 `VarsDevice` 这个数据件要立（`B9_剩余C_施工单` 第 443/102 行早就记了这条，属同一条欠账） |

---

## 五、我们这一侧的**结构差异**（不是缺陷，但读本文的人必须知道）

原版的 `Update` 是**一台状态机**（`UIstate` 15 档 switch），我们的是**一条扁平管线**
（`Update` 里按序 `Handle*` / `Tick*`，各自「处理了就 return」）。所以「逐分支对表」在两边不是同一形状：

| 原版 | 我们 |
|---|---|
| `UIstate` 一个字段决定「这一帧跑哪一支」 | **没有这个字段**；等价物是分散的 `_selectedSlot` / `_command` / `_awaitPlayTargetCard` / `interaction.IsDragging` / `selector.Visible` … |
| 唯一一处读口 `GetUIState()` | `BattleUiIsNormalBattle`（`BattleDriver.cs:33952`）只回答「是不是 normalBattle 那一档」，**不是**完整枚举 |
| 命中 = uGUI collider 事件 | 命中 = 每帧 `HitSlot` 轮询（棋盘无 uGUI collder） |

⇒ 「我们是 A」的意思是**行为等价**，不是「有一段同名代码」。反过来说：任何人想加一条「原版 UIstate == N 才做」的闸，
**必须照 `BattleUiIsNormalBattle` 那份清单的写法**逐档映射，不能只写一个布尔。

---

## 六、顺手发现（只报不改）

1. **`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs` 是目前最好读的那一份反编译** ——
   它有完整标签、有字段名、有化简后的条件；而 ILSpy 版（`out_cpp2il_dev_ilspy`）**把整个 `Update` 的主 switch 丢成
   `Indirect jump … should have been resolved before IL gen`**。这与人字旁那条「四份产物只有 dev_ilspy 有真方法体」**不矛盾**
   但**要补一句**：`out_il2csharp` 在**已恢复方法体**的那些方法上，控制流比 ILSpy **更完整**（本方法就是反例）。
   ⇒ 建议以后「读控制流」首选 `out_il2csharp`，「读字段名/常量」查 `dump.cs`，「读真常量」回 `.c`。
2. **`A2` / `AU1` / `AU2` 三份文档里「`FUN_180617800` 没有反编译产物 ⇒ 判不死」的说法已过期**（本文 §1.2 已给出机器码证据）。
   连带 `BattleDriver.cs:8084` 那条 `🔶 自己设计` 标记的理由也过期了 —— **但本批不改 `.cs`**，请调度台另派一条「零行数变化」的注释订正。
3. `BattleDriver.cs:17062` 的 doc 写「收起那支另半个条件没反编译出来（`FUN_180617800`）⇒ 只做『松手』这一半」——
   实际实现（`PointerHeldRaw()`）**已经是对的**（= `!TouchPressed`），只是注释里的话过期。同上，等 `.cs` 写手空出来再改。
4. **`VarsDevice` 这个数据件本机有原版值**（`VarsDesktop.json` / `VarsMobile.json`，在
   `assets_full/bundle_duplicateassetisolationso_assets_all/MonoBehaviour/` 下），
   而 `B9_剩余C_施工单` 第 39/102/443 行还写着「`VarsDesktop/VarsMobile` 两份 SO 未导入 / 取不到 ⇒ 值只能 🔶 自定」。
   ⇒ **那条「取不到」的判断是错的**（同「翻过一个镜像目录就写『本地没有』」那一族）。字段值与路径见本文 §1.3。
5. 原版 `Update` 里 `playerHandPosLimitObj`（`+0x180`）**只在这一处**用到，且它取的是**场景节点 `HandLimitArea` 的 transform**；
   我们把它换成了常量 `BoardLayout.HandLimitY`。⚠️ 若将来 `HandLimitArea` 在某些战场档位不同高（13 场是否全 300 未逐场核），
   常量就会漂 —— 本批未核，建议记一条。
