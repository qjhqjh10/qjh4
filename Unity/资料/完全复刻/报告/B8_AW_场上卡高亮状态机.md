# B8_AW · 场上卡高亮状态机（`CardHighlight`）核清与落地

> 执行代理 `B8_AW` · 2026-10-26 · 批次 8
> 白名单：`CardPresentation/Core/CardHighlight.cs` · `CardPresentation/Core/CardView.cs`
> 账补丁：`资料/完全复刻/账/_parts/B8_AW.tsv`（**4 行，全部保持 B** —— 判据/落点/差异/观测量全部补实，缺口缩到「派发未接」）
> 判据源：🥇 `d:/2/tools/decomp_full/*.c`（本轮亲读 **20 份**：`CardHighlight` 全族 10 + `CardScript` 五个入口/`TryCardHighlight` 5 + `BattleManager__ShowDeckSize` 1 + `CardScript__DisplayingActiveAbility`/`__StopDisplayingActiveAbility` 2 + `BattleManager__ShowPotentialTargets`/`__StopShowingPotentialTargets` 2，后两份只读了 `ChangeState` 调用点那几行）· 字段偏移 `d:/2/tools/il2cpp_out/dump.cs`（本轮展开 `CardHighlight` / `CardScript` / `EntityScript` / `RawCardScript` / `BattleManager` 五张字段表 + 4 个枚举）

---

## 一、结论

| 项 | 结果 |
|---|---|
| **缺的是哪一半** | **渲染侧早就全有**（状态环 6 色 · 卡体缩放 ×1.05 · 「会死」图标 · 攻击类型底光 · 残骸环拷色）。缺的是**①那台状态机本身**（谁持有 `State`、`3↔4` 怎么转、`regular` 那道入口守卫）+ **②四个入口的派发**（`BattleDriver` 里**根本没有「悬停棋盘卡」这条输入**，只有 `_pressSlot` 的按下/抬起）+ **③伤害预览的探针**（算式在引擎侧，宿主没接）。 |
| **本轮建了什么** | `CardHighlight.CardBoardHighlight` —— 原版 `CardHighlight` 组件的**整台机器**（6 档 `ChangeState` + `Start` + `SetCombatHighlights` + `HighlightAttackTypes` + `ToggleCombatPreviewHighlight` + **四个入口方法** + 一个静态守卫），外加 `CardView` 的四个渲染侧入口与三个读数口。**零行为变化**（见 §三末）。 |
| 四条账行去向 | **4 条全部保持 B**。`OnTouchDown` / `OnTouchEnter` / `OnTouchExit`（3 条）的缺口**只剩派发**（`BattleDriver` 三处调用点，patch 在 §七）；`OnTouchUpAsButton` 的缺口**不是状态机**（见下面的就地改判）。 |
| 🔴 **就地改判（铁律 5）** | 简报把 4 条说成「同一处卡点」。**本轮逐句核下来只有 3 条是真**：`CardScript__OnTouchUpAsButton.c:1-87` **全文一次都没碰 `cardHighlight`（+0x218）** ⇒ 它跟这台机器**没有关系**（它要的是 `displayingCardFlag` 协议 + 换牌档 + 灵魂石那三支）。它保持 B 的原因是 **①换牌档（13/14）落点在 `Battle/MulliganPanel.cs` 未核 ②`MulliganElementsAreVisible` 那个 `mulliganException` 参数我们这边无落点**，**不是**状态机。 |
| 改了 `.cs` | **+667 行 / 2 个文件 / 全在类尾或文件尾**（`CardHighlight.cs` +602 · `CardView.cs` +65）。**零删改既有逻辑**（唯一一处既有行改动是把一行注释就地扩写，**行数不变**）。 |
| 机检 | `typecheck` **我这 2 个文件 0 错**（最后一次改完 `.cs` 后整袋 **0 / 0**；随后并发写手的 `RuleEngine/Core/CardDef.cs:8634` 冒出一条 `CS0708`，见 §六·8 —— 那是别人的活）· `verify_annot` **缺注解 0** · `check_a_vs_diff` **强 0**（我这 4 行是 B、天然不在扫描范围）· `check_patch_conflicts` **无重复键** · 补丁 11 字段 × 5 行（含表头）· LF、裸 CR 0 |

---

## 二、逐条证据

### 2.1 原版那台机器（`CardHighlight` 组件）

字段表（`dump.cs:34145-34205`，本轮展开）：`+0x20 CardHighlightAnimTime` · `+0x24 state` · `+0x28 CardBodyToScale` · `+0x30 ScaleFactor` · `+0x38 minionWillDieAnimation` · `+0x40 minionWillDieObject` · `+0x48 highlightAttackTypes[]` · `+0x50 cardScript` · `+0x60 initialScale` · `+0x70 FrameHighlight` · `+0x78 FrameHighlightRemnant` · `+0x80 ValidTargetColor` · `+0x90 SelectedColor` · `+0xA0 PlayableColor` · `+0xB0 SelectedTargetColor` · `+0xC0 RegularColor`。

`ChangeState`（`CardHighlight__ChangeState.c:1-77`）**6 档逐档**（这是账的判据，不抄我们的表）：

| 档 | 枚举名 | 色字段（反编译读的偏移） | 补间 | 缩放 |
|---|---|---|---|---|
| 0 | `regular` | `+0xC0` = `RegularColor`（**alpha 0**） | `+0x20` = 0.1 | `ChangeTargetScale(initialScale, 0.1)` ⇒ **回 1.0** |
| 1 | `potentialTargetInHand` | `+0x80` = 绿 `#00FF21` | 0.1 | **不动** |
| 2 | `selected` | `+0x90` = 白 | **0（瞬切）**（`:69 uVar2 = 0`） | **不动** |
| 3 | `potentialTargetInBoard` | `+0x80` = 绿 | 0.1 | `initialScale × +0x30(1.05)` |
| 4 | `selectedTargetInBoard` | `+0xB0` = 橙 `#FF8400` | 0.1 | ×1.05 |
| 5 | `displayingActiveAbility` | `+0xA0` = 黄 `(1,1,0)` | **0（瞬切）** | **不动** |

`ChangeState` 的两句结构：`:14-16` **同态早退**（一个像素都不改）、`:17 ToggleFrames(true)`。
`ChangeFrameColor`（`.c:22-93`）只写两个 `SpriteRenderer.color`（`+0x70`/`+0x78`）+ `DOColor` 补间 + **alpha==0 才在收尾回调里 `ToggleFrames(false)`**（= 淡出而不是瞬灭）。
`ChangeTargetScale`（`.c:14-39`）：`time <= 0` 走瞬切、否则 `DOScale`。
`SetCombatHighlights`（`.c:5-8`）**只做一句** `minionWillDieObject.SetActive(false)`。
`HighlightAttackTypes`（`.c:9-24`）遍历 `highlightAttackTypes[]` 逐个 `HighlightIfNeeded(attackType)` ⇒ **我们的等价物是 `CardView.SetOwnAttackType`（已 A，`CardView.cs:8940-9110`）**。
`SetRemnantHighlight`（`.c:12-24`）：把主环 `color` 原样拷给残骸那具 ⇒ **我们已由 `CardView.SetHighlight → SetRemnantLight` 承担**（`CardView.cs:2620-2650`）。

`ToggleCombatPreviewHighlight`（`.c:1-178`）控制流（照标签走，全部落在新方法的 doc 里）：两台对象都在 ⇒ 算伤害条目 → `hasCurrentTrait(0x4b0)` / `EnoughPendingDamageToDieWithDamageValues` → `willDie` ⇒ 播 `minionWillDieAnimation` + `minionWillDieObject.SetActive(IsAnimatingCombat==0)` → 残骸支 `ShowOriginalRemnantCard(true)` 后 return → 计策卡（`rawCardData.cardType == Tactic(20)`）return → `!changeAttackType || formUnityAbility` return → `HighlightAttackTypes(**other 的** attackType)`；「收」支走 `:145-173`（`ShowOriginalRemnantCard(false)` → `SetActive(false)` → `HighlightAttackTypes(isPlayer ? 自己的 attackType : None)`）。

### 2.2 四个入口（各自承接哪一半）

```
CardScript.OnTouchDown（.c:1-97）
  :25-26 无条件写 pressTime            → CardScript+0x320 timeSinceLastClickOnCard（dump.cs:35506；反汇编里的 `param_1 + 800` 就是它）
  :27-32 UIstate==14 ⇒ LogWarning      → 宿主
  :61-70 cardState==inPlay(2)          → ★ 本机 OnTouchDown：regular 才 ChangeState(selected) + 无条件清 displayingCardFlag
  :35-47 / :49-59 cardState 0 / 1      → ShowDeckSize（→ CardHoldPeek，已 A）
  :71-92 其余各档                       → 早退 / LogWarning（宿主）
CardScript.OnTouchEnter（.c:1-40）
  :12-14 右键/双指 ⇒ 整条早退           → CardInteraction.IsRightButtonOrTwoFingerTouch（public，已 A）
  :21-24 UIstate==14 ⇒ 只 Log          → 本机 TouchInput.ChoosingResolutionCard
  :25-33 cardState==inHand(1)          → 手牌（CardInteraction.UpdateHover，已 A）
  :34-37 cardState==inPlay(2)          → ★ 本机 OnTouchEnter → TryCardHighlight
CardScript.TryCardHighlight（.c:1-99）
  :19-24 UIstate ∉ {0,7,8,11,12}（0x1901 掩码 + ==7）→ TouchInput.HoverAllowed
  :27-48 regular ⇒ ChangeState(selected) + 清 displayingCardFlag +（残骸支）
  :50-93 potentialTargetInBoard(3) ⇒ ChangeState(4) + ToggleCombatPreviewHighlight(BM.cardTracked, isAttacker:false,
         changeAttackType:true, formUnityAbility:(tracked.attackType==Active(4) || UIstate∈{9,10}))
         然后 attackType==Active(4) ⇒ return、UIstate∈{9,10} ⇒ return、+0x419 ⇒ return、
         否则**再给 cardTracked 那张**来一次 ToggleCombatPreviewHighlight(this, isAttacker:true, changeAttackType:false)
CardScript.OnTouchExit（.c:1-57）
  :15-20 displayingCardFlag ⇒ 清零 + DisplayCard(false)   → ★ 本机（等价物已在驱动层：_cardWinSource）
  :21-23 isPlayer ∧ cardState==8 ⇒ StopShowingCardInHand  → 手牌（ApplyShowInHand，已 A）
  :26-27 ToggleCombatPreviewHighlight(null, 0, 0, IsAnimatingCombat==0, 0)  → ★ 本机
  :28-39 BM.cardTracked 那张也收（isAttacker:true, changeAttackType:true） → ★ 本机
  :40-50 state==2 ⇒ ChangeState(0) / state==4 ⇒ ChangeState(3)           → ★ 本机（**通篇无闸**）
CardScript.OnTouchUpAsButton（.c:1-87）
  :17-20 UIstate==14 ⇒ LogWarning + return
  :22-51 {1,8} 手牌档（0.2 s 门 + DisplayCard + state==8 收手牌悬停 + 非我方牌要 Sabotage）→ 手牌侧（已 A）
  :52-61 {13,14} 换牌档（displayingCardFlag=1 + 非我方 return + DisplayCard(1,1,0)）→ MulliganPanel（未核）
  :63-83 {2,3,17} isRemnant ⇒ TryUsingWaystone；否则 displayingCardFlag=1 + DisplayCard(1, !MulliganElementsAreVisible, 0)
  ⚠️ 全文**没有一处**碰 CardHighlight（+0x218）⇒ 与状态机无关
```

### 2.3 本轮展开的字段表（解掉了上一路留下的缺口）

| 反汇编里的偏移 | 真名 | 出处 |
|---|---|---|
| `CardScript +0x28` | **`rawCardData`（`RawCardScript`）** | `dump.cs:21968`（EntityScript 的 backing field） |
| `rawCardData +0x90` | **`spellType`**（`0xE6 = 230 = SpellType.Sabotage`） | `dump.cs:23244+` / `SpellType` 枚举 |
| `rawCardData +0x94` | **`cardType`**（`0x14 = 20 = CardTypeOptions.Tactic`） | 同上（`ToggleCombatPreviewHighlight.c:44/132` 用的就是它） |
| `CardScript +0x320` | **`timeSinceLastClickOnCard`** 及其常量 **`THRESHOLD_FOR_OPENING_CARD_DISPLAY = 0.2`** | `dump.cs:35506-35507` |
| `CardScript +0x248 / +0x24A / +0x290` | `IsAnimatingCombat` / `displayingCardFlag` / `CardUI` | `dump.cs:35461/35464/35486` |
| `EntityScript +0x40 / +0x65 / +0x120` | `isPlayer` / `isRemnant` / `currentAttackType` | `dump.cs:21973/22001/22029` |
| `BattleManager +0x1E8 / +0x290 / +0x330 / +0x419` | `mulliganManager` / `UIstate` / **`cardTracked`** / `waitingToResolveUseWaystoneFlag` | `dump.cs:30709+` |
| `CardHighlight +0x24` | `state`（`get_State` = `return *(+0x24)`） | `dump.cs:34150` / `CardHighlight__get_State.c:5` |

---

## 三、改了什么（+667 行，零删改），以及为什么是**零行为变化**

### `MyGame/Assets/CardPresentation/Core/CardHighlight.cs`（259 → 861 行，**+602**，全部追加在文件末尾、`OutlineOf` 之下）
| 行 | 内容 |
|---|---|
| `:259-321` | 判据头注释（逐方法出处 + 五张字段表对齐 + 「两个枚举别合并」） |
| `:323` | `public static readonly Color PlayableColor` —— 原版 `+0xA0` = `(1,1,0)`（原来只在注释里，没有字段） |
| `:327-338` | `enum CardBoardHighlightState` —— 原版 `HighlightStates` **6 值逐条照抄** |
| `:340-349` | `static class CardStateId` —— 原版 `CardStateOptions` 里本机读到的 8 档 |
| `:359-379` | `struct CombatPreviewInfo` + `delegate CombatPreviewProbe` |
| `:382-428` | `struct TouchInput`（每个字段标了它替原版哪一个字段/表达式） |
| `:433-857` | `sealed class CardBoardHighlight`：`State`(派生) `:447-468` · `ChangeState :538-545` · `MapToView :554-564` · `OnStart :569-574` · `SetCombatHighlights :578` · `HighlightAttackTypes :588-592` · `ToggleCombatPreviewHighlight :612-658` · `TryCardHighlight :688-718` · `OnTouchDown :726-732` · `OnTouchEnter :741-747` · `OnTouchExit :759-779` · `OnTouchUpAsButton :796-827` · `EnemyHandCardClickable :836-841` · `Describe :844-857` |

### `MyGame/Assets/CardPresentation/Core/CardView.cs`（14390 → 14455 行，**+65**，**全部追加在类尾**）
`:14390-14399` 说明 · `:14401` `_boardHighlight` · `:14406-14413` `BoardHighlight`（懒建） · `:14420` `BoardHighlightStateId` · `:14423` `HighlightScaleMulForTest` · `:14436-14444` `ApplyBoardHighlightState`（6 档 → 已有三层） · `:14448-14453` `ForceRegularFrame`。

> 🔴 **为什么一行都不许插在中间**：本文件有 **40+ 条账的落点按 `文件:行号` 指进来**（`卡牌面_渲染.tsv` 最密的地方全在 `:10270-:14278`），中间插一行就整片漂移。实测：本轮只追加类尾 ⇒ 上述行号**一条都没动**（`CardHighlight.cs` 唯一那条锚点 `:229-250` 也没动）。

**零行为变化的理由（可复核）**：
1. 新类是**独立对象**，唯一入口是 `CardView.BoardHighlight`（懒建）；全仓**没有**任何产品代码读它 ⇒ 不改任何既有画面。
2. `CardView.ApplyBoardHighlightState` / `ForceRegularFrame` 只在 `ChangeState` / `OnStart` 里被调，而这两个只从本类的入口方法进 ⇒ 今天没人调。
3. 唯一**今天就有读数**的是 `BoardHighlightStateId` —— 它**派生自** `CardView.State`（不另存），所以即使没人调入口，它也始终等于视图真值：`ValidTarget` → 场上 3 / 手牌 1、`Selected` → 2、`SelectedTargetInBoard` → 4、其余 → 0。
4. 既有 `SetHighlight` 的 20+ 个调用点**一个字没动**（这是刻意的：那 40+ 条锚点 + 大量自检都压在这条路上）。

---

## 四、断言草稿（请转交 `Editor/BattleScene.cs` 的写手，插在 `CheckCardViewState` 之后）

```csharp
// A1 · 6 档逐档（判据 = CardHighlight__ChangeState.c，⛔ 别拿我们自己的表当期望值）
//   probe = CardView.Create(root, data, "HlProbe"); h = probe.BoardHighlight;
//   h.ChangeState(potentialTargetInBoard) ⇒ BoardHighlightStateId==3
//        ∧ RimTargetForTest ≈ (#00FF21 × RimAlphaScale) ∧ RgbNear(…, 0,255,33)
//   推进 0.2 s ⇒ HighlightScaleMulForTest ≈ 1.05
//   h.ChangeState(selectedTargetInBoard) ⇒ ==4 ∧ 橙 #FF8400 ∧ 缩放仍 1.05
//   h.ChangeState(potentialTargetInBoard) ⇒ ==3（4⇒3 回**绿**，⛔ 不是 regular）
//   h.ChangeState(selected) ⇒ ==2 ∧ **同帧** RimTargetForTest 已等于目标（原版 :69 uVar2 = 0 = 瞬切）
//   h.ChangeState(regular) ⇒ ==0；推进 0.2 s ⇒ RimVisible==false ∧ 缩放回 1.0
//   灭自证：把 MapToView(selectedTargetInBoard) 改成 Selected ⇒ 橙环那条必红

// A2 · 入口守卫（判据 = TryCardHighlight.c:19-24 / :27-28 / :50-53）
//   h.ChangeState(regular); h.TryCardHighlight(inp{CardState=2,HoverAllowed=true}) ⇒ true ∧ ==2
//   h.ChangeState(potentialTargetInBoard); h.TryCardHighlight(inp{…,TrackedCard=null}) ⇒ true ∧ ==4 ∧ 不抛
//   h.ChangeState(regular); h.TryCardHighlight(inp{HoverAllowed=false}) ⇒ false ∧ 仍 ==0
//   判别式：把 HoverAllowed 那道闸删掉 ⇒ 第三条红
//   h.ChangeState(regular); h.OnTouchEnter(inp{CardState=2,RightButtonOrTwoFingers=true}) ⇒ false ∧ 仍 ==0

// A3 · OnTouchExit 的 2⇒0 / 4⇒3 + ①关窗协议（判据 = OnTouchExit.c:15-20 / :40-50）
//   h.ChangeState(selected);  h.OnTouchExit(inp) ⇒ ==0
//   h.ChangeState(selectedTargetInBoard); h.OnTouchExit(inp) ⇒ ==3
//   h.DisplayingCardFlag = true; h.DisplayCard = on => shown = on;
//   h.OnTouchExit(inp) ⇒ DisplayingCardFlag==false ∧ shown==false

// A4 · 预览的收/发（判据 = ToggleCombatPreviewHighlight.c:104-142 / :145-173）
//   h.PreviewProbe = (o,a,c,f) => new CombatPreviewInfo{Valid=true, WillDie=true, AttackTypeToHighlight=1};
//   h.ToggleCombatPreviewHighlight(other, false, true, false) ⇒ probe.WillDieVisible==true ∧ HighlightedAttackType==Melee
//   h.PreviewProbe = … {Valid=false} ⇒ WillDieVisible==false（并出声一次）
//   判别式：把 `if (!changeAttackType) return;` 删掉 ⇒ 传 changeAttackType:false 时底光仍会亮 ⇒ 补一条断言抓它

// A5 · 「State 派生、不另存」（钉住设计）
//   probe.SetHighlight(ValidTarget)   （模拟 HighlightTargets）⇒ BoardHighlightStateId == 3（场上）**不经入口**
//   handProbe.SetHighlight(ValidTarget)（手牌形态）        ⇒ == 1
```

---

## 五、没查清的部分

| 项 | 状态 | 卡在哪 |
|---|---|---|
| `EnemyReturnsAttack` / `GetThisCardPlayedDamage` / `GetCombatDamageWithType` / `GetActiveAbilityDamage` 四句的**原版算式** | **没读** | 本轮只读 `CardHighlight` 这一族 + 四个入口；那四句在 `EntityScript` 那一族（本轮白名单与时间都不允许）。⇒ 状态机用 `PreviewProbe` 承接，**⛔ 没有拿我们自己的 `RuleCore.*` 冒充原版算式**。我们现成的等价链（`RuleCore.AttackDamageEntries` / `ActiveAbilityDamageEntries` / `WouldKillByEntries`）已在 `BattleDriver.SetReticleTarget` 里被用过（那条是 A），接探针时直接复用。 |
| `hasCurrentTrait(0x4b0)` 那条 trait | **没锚定名字** | `0x4b0 = 1200`，`DefinedTrait` 枚举里没有这么大的值 ⇒ 它是**数据侧 trait id**（同族 `0x4ce` / `0x4fb` 也都在 `OutlineOf` 的注释里出现过）。没猜。 |
| `BattleManager.camerasShaking`（`+0x419` 同族状态）与「演出中」判据 | **没核** | `+0x419 = waitingToResolveUseWaystoneFlag` 已锚定（用上了）；`+0x248 IsAnimatingCombat` 的原版写入点没查。 |
| `displayingActiveAbility(5)` 的接线方 | **没有** | `CardScript.DisplayingActiveAbility()` → `ChangeState(5)`（`.c:6`）/ `StopDisplayingActiveAbility()` → `ChangeState(0)`（`.c:6`）—— 那两条属「主动技能展示」那一族，**本批没人接**。我们的视图枚举里也没有这一档 ⇒ 机器里用 `_displayingActiveAbility` 布尔记住、渲染走 `CardView.ApplyBoardHighlightState` 的第三支（黄环瞬切）。**如实记：今天够不到。** |
| 换牌档（`cardState 13/14`） | **未核** | 落点在 `Battle/MulliganPanel.cs`（白名单外）。 |

---

## 六、顺手发现（都没改别处，按铁律 13·4⑧ 报上来）

1. 🔴 **`BattleManager.ShowDeckSize(bool isPlayerY)` 的那个参数是死参**（`BattleManager__ShowDeckSize.c:1-47` 全文**一次都没读**它；签名见 `dump.cs:32917`）—— 方法体把**两侧** `DeckManager`（`+0xf0`/`+0x108`）与**两个** `PlayerHand`（`+0xd0`/`+0xd8`）**一起点亮**，并置 `+0x598 = 1`（松手由 `Update.c:140-150` 收起）。⇒ **我们「两边一起显示」是对的**，B8_AL 留的「state==1 敌方侧未核」这一档**已闭合**。
2. 🔴 **但触发条件我们与原版不同**（驱动层缺陷，未改）：原版只在 `cardState 0`（牌库里的卡）或 `cardState 1` **且非我方**时才 `ShowDeckSize`；`CardHoldPeek` 是「按着手牌 或 按着棋盘格」（`BattleDriver.cs:16440-16441`）⇒ **按棋盘单位时原版不亮、按牌库时原版亮、按自己手牌时原版不亮**。改动请求 → §七③。
3. 🔴 **`TryCardHighlight` 的 UIstate 闸不是「只有 normalBattle」**：`0x1901` 掩码 = `{inactive 0, postBattle 8, mulligan 11, mulliganFinished 12}`，再加 `== 7(finishBattleSequence)` ⇒ 封锁集 5 档；`playingMinion 3` / `minionAttacking 6` / `choosingUnitTarget 9` / `targetedActiveAbility 10` / `choosingAbilityTarget 13` **都允许**。⚠️ 若照 `BattleDriver.BattleUiIsNormalBattle`（B8_A 建的）一档接，会**少掉这 5 档** ⇒ 我把判据做成 `TouchInput.HoverAllowed`（映射写在它的 doc 里），没复用那个属性。
4. ⚠️ **`ChangeState` 的 `selected` 档原版不调 `ChangeTargetScale`**（`.c:37-42` 直接落 `LAB_18060af65`）⇒ 原版会把上一档的缩放**留着**。我们 `SetHighlight(Selected)` 会 `SetHighlightScale(false)` 回 1.0。实到路径上（`0→2` 与 `3→4`）前态都是 1.0/1.05 且 2 档只在 `regular` 之后进 ⇒ **无可观察差异**，但已如实写进账的「差异」列。
5. ⚠️ **`OnTouchUpAsButton` 与这台机器无关**（§一 那条就地改判）。
6. ✅ **`HighlightTargets` / `HighlightPlayTargets` 不用改**：原版「合法目标」那两档也是走同一台机器（`BattleManager__ShowPotentialTargets.c:31` / `__StopShowingPotentialTargets.c:24` 那两处 `CardHighlight__ChangeState(...)`），而我们的机器 `State` **派生自视图** ⇒ 驱动层直接 `SetHighlight(ValidTarget)` 之后，机器的 `State` 自动就是 3（场上）/1（手牌），`3⇒4⇒3` 那条路**已经通**。⛔ 别为了「统一入口」去把那两处改成调 `ChangeState`（那会多一层转发、零收益）。
7. ⚠️ `CardView.cs` 工作树里本来就有别人未提交的改动（HEAD 14184 行 → 我改前 14390 行）⇒ **别拿 `git diff --numstat` 判我的行数**，按「HEAD 行数 vs 现读」算：`CardHighlight.cs` 259→861、`CardView.cs` 14390→14455。
8. 🔴 **`typecheck` 现在有 1 条红，不是我的**：`RuleEngine/Core/CardDef.cs:8634` 报 `CS0708: 不能在静态类中声明实例成员（TraitReachability.IsWarlordEquivalent）` —— 那一行是**并发写手刚加进去的属性**（`public bool IsWarlordEquivalent { get { return Type == "hero"; } }` 落在 `static class TraitReachability` 里）。⛔ 我没动那个文件（白名单外）。**证据：本轮最后一次改完 `.cs` 之后、那个属性落盘之前，`typecheck` 是 0 / 0**；现在编译器只报这 1 条、**一条都不是我这 2 个文件的** ⇒ 我的改动干净。请调度台转给 `CardDef.cs` 的写手（把它挪出静态类，或改成 `static` + 收 `type` 形参）。

---

## 七、给调度台的请求

### ① 🔴 **接派发（3 处调用点，`Battle/BattleDriver.cs`，判据齐、可直接抄）**
> 目标：`OnTouchDown` / `OnTouchEnter` / `OnTouchExit` **三条一起翻 A**。前提 = 派发接上 + `BattleScene.Run` 一条读数（§四 A1–A5 里挑两条）。
> ⛔ **不要**接 `OnTouchUpAsButton`（它跟机器无关，见 §一 改判）；也⛔ **不要**把 `DisplayCard` / `CollectWaystone` 两个钩子接到机器上（驱动层 `TapBoardCard` / `_cardWinSource` 已经在做，接两处 = 一次点开两次窗）。

**(a) 每帧悬停（新加，放在 `Update` 里 `if (BoardPress(world, PointerHeldRaw())) return;`（`:14345`）**之前**）**
```csharp
// 原版 CardCollider.OnPointerEnter/Exit → CardScript.OnTouchEnter/OnTouchExit（成对派发）
int hs = HitSlot(_myUnits, world); int hSide = _me;
if (hs < 0) { hs = HitSlot(_foeUnits, world); hSide = 1 - _me; }
CardView hv = null;
if (hs >= 0) (hSide == _me ? _myUnits : _foeUnits).TryGetValue(hs, out hv);
if (hv != _hoverBoardCard)
{
    if (_hoverBoardCard != null) _hoverBoardCard.BoardHighlight.OnTouchExit(BoardTouchInput(_hoverBoardCard, _hoverBoardSide));
    _hoverBoardCard = hv; _hoverBoardSide = hSide;
    if (hv != null) hv.BoardHighlight.OnTouchEnter(BoardTouchInput(hv, hSide));
}
```
**(b) 按下（`:14423` / `:14427` 记 `_pressSlot` 那两句之后）**：`view.BoardHighlight.OnTouchDown(BoardTouchInput(view, side));`
**(c) 字段**：`CardView _hoverBoardCard; int _hoverBoardSide;`
**(d) 输入包**（`TouchInput` 的每个字段都在 `CardHighlight.cs:382-428` 有 doc）：
```csharp
CardHighlight.TouchInput BoardTouchInput(CardView v, int sideHit)
{
    var inp = new CardHighlight.TouchInput
    {
        CardState = CardHighlight.CardStateId.inPlay,
        IsPlayer = sideHit == _me,
        // 原版 TryCardHighlight.c:19-24 的封锁集 {0,7,8,11,12}：我们这边的等价 = 局没终 ∧ 不在换牌期
        HoverAllowed = Ctx != null && !Ctx.IsOver && InMulligan == false && Ctx.Turn > 0,
        ChoosingResolutionCard = Choose != null && Choose.Visible,     // 原版 UIstate==14
        RightButtonOrTwoFingers = interaction != null && interaction.IsRightButtonOrTwoFingerTouch(),
        ChoosingUnitOrAbilityTarget = _selectedSlot >= 0 || _command != AttackKind.None,  // 原版 UIstate ∈ {9,10}
        MulliganElementsVisible = mulligan != null && mulligan.Visible,
        WaystonePending = false,          // ⚠️ 我们无该字段；原版那一支只在收集灵魂石结算中，见改造提示
        TrackedCard = _reticleTarget,     // 原版 BattleManager.cardTracked(+0x330)
        PressSeconds = PressElapsedSeconds,
    };
    return inp;
}
```
⚠️ `InMulligan` / `mulligan` / `interaction` 三个标识符请按文件里**现读的真名**替换（我没在该文件里动手，避免撞车）。

**(e) 伤害预览探针（`PreviewProbe`，紧接上面接线一次）**
```csharp
v.BoardHighlight.PreviewProbe = (other, isAttacker, changeAttackType, formUnityAbility) =>
{
    int os = SlotOfView(other); if (os < 0) return default(CardHighlight.CombatPreviewInfo);
    int os2 = SideOfView(other);
    return new CardHighlight.CombatPreviewInfo {
        Valid = true,
        OtherIsTactic = other.Data != null && other.Data.subtype /*CardType Tactic⇔计策*/ …,
        WillDie = RuleCore.WouldKillByEntries(Ctx.Players[os2].Board[os], DamageEntriesFor(os2, os)),
        AttackTypeToHighlight = (int)other.OwnAttackType,
    };
};
```
⚠️ `OtherIsTactic` 的等价判据 = 我们卡表的**计策**那一维（`RuleCore`/`CardDef` 侧现有唯一判据），⛔ 别新写一张映射表；`SlotOfView` / `SideOfView` 若没有现成口，请复用 `HandIndex` 那一路的写法（同族已有）。

### ② 转派：`Battle/MulliganPanel.cs` —— `OnTouchUpAsButton` 的换牌档（`cardState 13/14`：`displayingCardFlag=1` + 非我方 return + `DisplayCard(1,1,0)`）与 `MoveCardToMulliganPos` 行几何（B8_W 报过的 383.8/502.2 vs 我们 292/414）。这一条核完 + `MulliganElementsAreVisible` 那个参数有落点，**923 才能升 A**。

### ③ 转派：`BattleDriver.CardHoldPeek` 的**触发条件**（§六·2）—— 原版只在 `cardState 0`（牌库里的卡）或 `cardState 1 ∧ !isPlayer` 时 `ShowDeckSize`；待改点：`_pressSlot >= 0` 那一支应改成「按在**牌堆**上」、`interaction.IsDragging`（按自己手牌）那一支应删。判据齐（`OnTouchDown.c:35-59` + `ShowDeckSize.c:25-37`）。

### ④ 同步点请跑一次 `check_ledger_points --all-A --fix`（在无 `.cs` 写手时）：我这 4 行是 B、不在它的工作集里；本轮**没有**动任何既有行号（§三那条红线）。

### ⑤ 断言草稿 5 组（§四 A1–A5）—— 请转给 `Editor/BattleScene.cs` 的写手。
