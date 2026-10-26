# B8_F · 表现层 B→A 报告（2026-10-26）

> 代理：B8_F（执行型）· 白名单：`CardPresentation/Hand/**` · `CardPresentation/Board/**` · `CardPresentation/Core/CardFeel.cs`
> 账补丁：`账/_parts/B8_F.tsv`（**21 行**：14 条升 A · 3 条升 C · 4 条保持 B 但订正判据）
> 口径正本：`判据/战斗逻辑驱动_B2A_口径.md`（G0–G8 逐格打勾）

---

## ① 结论

| 结果 | 条数 | 说明 |
|---|---|---|
| **真升 A** | **14** | 每一条都：判据 `类__方法.c:行号`（标反编译来源）+ 我们侧**行号范围**逐句读过 + 一句机制差异 + 一个可观察量（两侧数值 + 读法）|
| **升 C（明确差异待修）** | 3 | `FillMinionPositions`（敌方镜像）· `DissolveMinionCard`（缺「溶回来」方向）· `MoveBackToHand`（缺 3 句） |
| **保持 B，但订正判据** | 4 | `GetLeftMostPos`（LogError 那句是错的）· `ToggleCombatState`（原落点配错）· `OutFromPlayingAreaAnim`（`LightPlayableHand` 无入口）· `SetAsCemetery`（缺 3 句，等墓地列表） |

**没有改一行行为代码** —— 这 14 条的共同原因是「实现早就在、只是没人逐句对过」，所以「落地」的产物是**账 + 可观察量读法**，不是新代码。
唯一的代码改动是 `ArenaSlots.cs` 的**注释订正**（见 §③），因为原注释把一个错判据写在了判据正主的位置上。

---

## ② 逐条证据

> 格式：`方法 | 判据（原版，带反编译来源） | 我们落点 | 机制差异 | 可观察量（原版期望 ↔ 我们）`

### A 档 14 条

| # | 方法 | 判据（🥇 decomp_full 优先） | 我们落点 | 机制差异（一句） | 可观察量 |
|---|---|---|---|---|---|
| 1 | `CardScript.TurnCardAround` | `CardScript__TurnCardAround.c:1-43`；`.rdata 0x1834b2e04=90` · `0x1834b2e7c=0.25`（`read_literal` 4B float） | `CardFeel.cs:3249-3256` · `:3259-3262` · 常量 `:3237/:3241` | 原版「一段 0→90 + OnComplete 起第二段」，我们一条 Sequence 两段 | 卡根 `localEulerAngles.y`：t=0 → 0° · t=0.25×draw → **90.0°** · t=0.5×draw → 0° |
| 2 | `CardScript.FinishCardDrawRotation` | `CardScript__FinishCardDrawRotation.c:1-30` | `CardFeel.cs:3254`（第二段）· `:3347-3349`（换面两条回调） | 原版 `SetObjectVisibility` 换子件，我们开/关 `cardBack` 层 | 同 #1 的第二段；同一刻 `cardBack.activeSelf` true→false |
| 3 | `CardScript.DoPushBack` | `CardScript__DoPushBack.c:1-32`；`0x1834b3158=0.4` · `0x1834b2dc8=0.3` · vibrato 硬编码 8 | `CardFeel.cs:1266-1284`（PunchBack）· `:62-68` · `:1217-1240` | 幅度来源改用「攻击方近战档 0.5/1.0/1.5」（原版那两个方法体本机读不到） | `DOPunchPosition` 四实参（punch=d×幅 / 0.4 / 8 / 0.3） |
| 4 | `CardScript.CompleteReassembleAnim` | `CardScript__CompleteReassembleAnim.c:1-21`；`+0x2C4 = originalLocalPosInPlay`（`dump.cs` 字段表） | `CardFeel.cs:3289-3300` · 目标位 = `HandLayout.cs:838-883` | 同一个「静止位」：原版靠缓存字段 `+0x2C4`，我们靠形参 | 补位补间终点 = 格位 `localPosition`、时长 0.1 s、OutQuad |
| 5 | `CardScript.FinishAttackActionWithoutMoving` | `CardScript__FinishAttackActionWithoutMoving.c:1-48`；`0x1834b315c=0.65`；`+0x1A0=attackStepTime` | `CardFeel.cs:2279-2281` · `:2287` · `:2290-2298` | 我们把它与 `AfterAttack` 合成一段（原版两者只差 `+0x48++` 与 replay ÷3） | 回程 0.1 s + 前置 0.65 s（整条 0.75 s）；`BattleDriver.cs:32358` 有现成断言 |
| 6 | `CardScript.GetCardInHandScale` | `CardScript__GetCardInHandScale.c:1-61`；`+0x148` = `m_cardsHorizontalLayout`；`m_scale @ +0x78`（`dump.cs:112246`） | `HandLayout.cs:96` · `:227` · `:770` | **原版 = `Vector3.one × m_scale`**（原账写 baseScale×m_scale，已订正）；载体由「卡反查布局」变「布局自持」 | 手牌卡 `localScale.x` = **0.730**（原版 m_scale 0.73，MB 5271 实读） |
| 7 | `CardScript.GetCardInHandMovingScale` | `CardScript__GetCardInHandMovingScale.c:1-32`；`VarsGlobal +0x44 = 1.1` | `CardInteraction.cs:37` · `:940-943` | 同一乘数、同一位置；原版现查 VarsGlobal，我们是序列化字段 | 拿起后 `localScale.x` = 0.730×1.1 = **0.803**，松手回 0.730 |
| 8 | `CardScript.StartMovingFromHand` | `CardScript__StartMovingFromHand.c:1-99`；`+0x44=1.1` · `+0x78=0.1` | `CardInteraction.cs:871-943` · `CardTween.cs:26` | 拿起补间 0.1 s / 终点 0.803 / OutQuad 逐句同；音效接在出牌那一拍 | 见 #7 |
| 9 | `CardScript.UpdateMinionInHandPosition` | `CardScript__UpdateMinionInHandPosition.c:1-58` | `HandLayout.cs:838-883` · `CardTween.cs:38` | 原版三条独立补间（各自 Kill），我们一条 `TweenPose` ⇒ 逐帧等值 | 三量：`SlotPosition` / `RotationAt` / `cardScale`，0.2 s OutQuad（`TryLastPose` 可读） |
| 10 | `PlayerHand.PositionCardInHand` | `PlayerHand__PositionCardInHand.c:1-53` | `HandLayout.cs:762-800` · `:838-883` | 选中让位的两道门换成 `useExtraSpaceOnSelectedCard` + `IsCompressed` | 我方 ≥8 张（压缩态）选中时两侧各外移 **0.2747 世界 / 29.7 px** |
| 11 | `PlayerHand.DisplayHandWithCardSpace` | `PlayerHand__DisplayHandWithCardSpace.c:1-78` | `HandLayout.cs:683-696` · `:708-836` · `CardInteraction.cs:1537-1542` | 三段式新序号推导逐支对齐；我们每帧重算（无早退缓存） | 槽位 = 可见张数 + 1；分界在**两张之中点** |
| 12 | `PlayerHand.IsDoingAnim` | `PlayerHand__IsDoingAnim.c:5-7`；`+0x61` | `CardInteraction.cs:436` · `:456-462` · `:1537-1542` | 载体：bool 字段 → `DelayedCall(0.2)` 计时器（批处理可推进） | 闸开 0.2 s，期间 `PickUp` 早退；`IsDoingAnimForTest` / `AnimGateRemainingForTest` |
| 13 | `PlayerHand.MoveCardsInHandToPosition` | `out_cpp2il_dev_ilspy/PlayerHand.cs:1243-1530`（🥈 状态机真体；`decomp_full` 版只有 kickoff） | `HandLayout.cs:708-836` · `CardInteraction.cs:1537-1542/:456-462` · 激活半边 `BattleDriver.cs:26819-26840` | 层序载体：`Canvas.sortingOrder`+`SetSiblingIndex` → 渲染队列 `queueTop−(slot+1)`，方向逐字同 | 队号序列（最左 3000、逐张 −1）+ 闸 0.2 s + `Unplayable` 描边 |
| 14 | `CardScript.ShowCardBack` | `CardScript__ShowCardBack.c:1-12` → `BasicCardUI__ToggleCardBack.c:1-12` → `Card2DController__SetCardVisibility.c:1-38`（三层全读） | `CardView.cs:7364-7414` · `:7360` · `CardFeel.cs:3347-3349` | 原版**两个 GameObject 互斥**（关卡面/开卡背），我们只把卡背**盖上**（z=−0.20） | `cardBack.activeSelf`：t=0 true、t=0.075 s false（我方抽牌才有） |

### C 档 3 条（明确差异待修）

| 方法 | 差异（可复算） | 修复要动哪 |
|---|---|---|
| `MinionManager.FillMinionPositions` | 我们对敌方整行取负（`ArenaSlots.cs:109` `enemy ? -x : x`），**原版两侧同号**（见 §⑤-1 的完整判据链） | `ArenaSlots.cs:109` + `BoardLayout.mirror` + `BoardLayout.NxOf` + 重跑 `BattleScene.BuildAndSaveScene`（`mirror`/`lineY` 烘进了场景） |
| `CardScript.DissolveMinionCard` | 只有「溶没（0→1）」那一半；原版 `dissolveY=false` 是「**溶回来（1→0）**」+ `ActivateCard(true)` | `CardFeel.Dissolve` 旁边补一支 un-dissolve；调用点 = `MoveBackToHand` / `OutFromPlayingAreaAnim` |
| `CardScript.MoveBackToHand` | 缺 `SetSortingLayer(手牌层)` · `StopAnimsInPlay` · `UnDissolveWholeCard`（全仓 0 命中） · `SetStateInHand` 的 cardState 枚举 | 回手链（`CardFeel.ReturnToHand` 的调用点） |

### B 档 4 条（保持，但判据已订正）

- `GetLeftMostPos`：原账写「数组空 ⇒ LogError」—— **错**，数组空走的是「回 `Vector3.zero`」那一支、一声不出（LogError 只在 `GetLeft/RightSlotPos` 的越界支）。索引语义（最外一格）与我们 slot 0 / 8 一致，但坐标符号受 §⑤-1 影响 ⇒ 保持 B。
- `ToggleCombatState`：原落点写成「MeleeAttack 段2 的 25° 旋转」—— **配错**。真身 = `+0x248 = state` + `CardHighlight.SetCombatHighlights(+0x218, false)`，与攻击姿态无关 ⇒ 保持 B（我们两样都无）。
- `OutFromPlayingAreaAnim`：4 步中第 4 步 `LightPlayableHand` 在我们手牌链上**无等价入口**（手牌可打高亮由 `BattleDriver.RefreshHandPlayable:26819` 在回合/状态变化时统一刷，不由回手触发）⇒ 保持 B。
- `SetAsCemetery`：位置 + 缩放两半逐句同（(−8.14, −0.2, 0) · 0.65 绝对值 · 0.3 s · OutCubic）；缺 `SetSortingLayer` / `SetSortingOrder(墓地序号)` / `SetColliderNotInHand` 三句 —— 我们**没有墓地列表**、卡在同帧 `SetActive(false)` ⇒ 那三句没有观察面，等墓地列表建成再落。

---

## ③ 改了哪些文件哪些行

| 文件 | 改动 | 净增行数 |
|---|---|---|
| `Assets/CardPresentation/Board/ArenaSlots.cs` | **只改注释** 106-108 三行（原地改写，3 行 → 3 行）：把「原版敌方 `leftSlotPosNormal` 就是 +x」订正为「那是 4373 的序列化死值，运行时 `Awake` 同式重算 ⇒ 原版不镜像」 | **0** |

**没有其它代码改动。** 手牌 / 卡牌 / 手感三层的行为代码（`HandLayout.cs` · `CardInteraction.cs` · `CardFeel.cs` · `BoardLayout.cs`）本轮一个字节没动 —— 这 14 条的实现本来就对，缺的是核对。

逐卡行号按 `check_ledger_points.py` 的预期测过：`ArenaSlots.cs` 行数不变 ⇒ **0 漂移**。

---

## ④ 没查清的部分（如实记，别当已核）

1. **`CardScript.ShowCardBeingDrawn` 与 `DrawCard` 的翻面条件相反**：`DrawCard.c:52-59` 是 `+0x40 != 0 ⇒ TurnCardAround`；`ShowCardBeingDrawn.c:7-16` 是 `+0x40 == 0 ⇒ TurnCardAround`。同一字段、相反判断 ⇒ **需裁定**（是两个不同用途的入口，还是原版自己的 bug）。我们按 `DrawCard` 实现，故这一条**保持 B**。
2. **`PlayerHand.DisplayHandMinusCard` 的真体**在 `out_cpp2il_dev_ilspy/PlayerHand.cs:819-970`（`_003CDisplayHandMinusCard_003Ed__80`），**没读** ⇒ 保持 B。
3. **`CardScript.GetEffectAnchor`** 的 `CardUI+0x30`（Transform）与 `+0x1a0`（Transform）两个锚点在 prefab 里的具体位置没解出来；我们的 `EffectsAnchorHeight` 只覆盖「锚点高度」这一个量 ⇒ 保持 B。
4. **`CardScript.AttackMeleeAnim`** 的 400+ 行状态机**没有逐句读完**（只对过段2 的旋转基，见 `CardFeel.cs:1113-1131` 的判据块）⇒ 保持 B。
5. **`PlayerHand` 其余 12 条表现层方法**（`HideHand` / `ShowHandSize` / `PlaySentinelAnims` / `SetupCardInMulligan` / `MoveCardToMulliganPos` / `HandCardMovedToNewIndex` / `RemoveCard` / `SetParentOfCard` / `LightPlayableHand` / `StopLightPlayableHand` / `HandCardReleased` / `StartMovingHandCard` / `FinishMulligan` / `FinishMulliganFinalPhase`）**没逐条核** ⇒ 全部保持 B。
   - 其中 **`HandCardMovedToNewIndex`** 最值得下一批做：它改的是**手牌列表顺序（数据）**，而全仓 `Hand.Insert` 0 处 ⇒ 手牌顺序恒等于抽牌顺序；原版能拖拽换位（玩家可观察）。
6. **`MinionManager` 几何族剩下 12 条**（`GetSlots` / `GetLeftSlotPos` / `GetRightSlotPos` / `GetRightMostPos` / `AdjustMinionBoardPos` / `SlotPosToBoardPos` / `GetDummyDistance` / `AreMinionsOutOfPos` / `SetCardShadow` / `HideCardShadow` / `ReassembleMinions` / `GetNextSlotPos`）保持 B —— 它们都落在同一个 `ArenaSlots.Position` 上，且都被 §⑤-1 那条镜像缺陷罩住；**先把镜像定了再一次性升**，别分两次动 `ArenaSlots`。
7. **13 场里 `DeckReferenceObject` / 墓地参照物的取值**没有逐场核（影响 `SetInDeckPosition` / `SetAsCemetery`）。

---

## ⑤ 顺手发现（⛔ 我没自己修，报上来）

### 5-1 🔴🔴 最高价值：**敌方槽位「镜像」这个问题可以结案了 —— 原版不镜像**

- 现状：`ArenaSlots.cs:109` 对敌方整行取负；`BoardLayout.mirror` 同样。依据是「原版敌方 `leftSlotPosNormal` 就是 +x」。
- `资料/普查产出_1015战斗/B6_逐格对账_棋盘与手牌我们vs原版.md:55` 已经把这条标成「**两条硬判据互相打架、判不了**，要进原版 dump 一个敌方 left 槽的 x 符号才能结」（= `A2103④`）。
- **本轮查清（三步，全部可复算）**：
  1. `leftSlotPosNormal` 在**全部 ILSpy 产物里只有两处赋值**：`MinionManager.cs:164`（在 **`Awake()` 内联**）与 `:399`（在 `FillMinionPositions()`）。而 **`FillMinionPositions()` 全库 0 调用者**（`grep -rn "FillMinionPositions"` 只命中它自己）⇒ **活写者只有 `Awake`**。
  2. `Awake` 用的是**同一套式子**（`left[i] = −((i+1)·Sep) − extra`、`right[i] = +((i+1)·Sep) + extra`、`y=0`），两个 `MinionManager`（玩家 4372 / 敌方 4373）各跑一遍 ⇒ **两侧 left 恒 −x**。
  3. 两个 `MinionArea` 的 `Transform_1309` / `Transform_1314`：`m_LocalRotation = (0,0,0,1)`、`m_LocalScale = 1` ⇒ 没有 180° 旋转把符号翻回去。
- **序列化里的「敌方 left = +x」是死值**：4373 的 `leftSlotPosNormal` 只有 **3 项**、`y = 5.0`，与运行时式子（`slotsPerSide=4` 项、`y=0`）自相矛盾 ⇒ 它就是 B4 §2.4 说的「序列化 ≠ 运行时」。
- ⇒ **结论：原版不镜像。我们镜像了 ⇒ 敌方「格号→世界 x」与原版相反。** 已落成 `FillMinionPositions` 那条 **C** 账行。
- **影响面**：敌方每一张场上卡的世界 x 符号；「我打你的槽 k」两边指的不是同一个物理格；`ArenaSlots.TryResolveSlot` 的命中判定；联机/录像会放大。
- **修复要点（跨白名单，需主对话拍）**：`ArenaSlots.Position` 去掉 `enemy ? -x : x` → 同步 `BoardLayout.mirror`（`BattleScene` 建场时置）与 `BoardLayout.NxOf`（2D 兜底）→ **必须重跑 `BattleScene.BuildAndSaveScene`**（`mirror` 与 `lineY` 烘在 `Scenes/Battle.unity` 里，不重跑 player 读的是旧值）。

### 5-2 `GetCardInHandScale` 的判据要点记错了（已订正）

原账写「`cardsHorizontalLayout` 非空 ⇒ **baseScale × m_scale**」。原文 `CardScript__GetCardInHandScale.c:22-32` 是 `Vector3.one × *(layout+0x78)`，而 `dump.cs:112246` 实读 `m_scale // 0x78` ⇒ **`Vector3.one × m_scale`，与卡自己的 baseScale 无关**。差别很实：少乘一个 baseScale。已写进 #6 的账行。

### 5-3 `FillMinionPositions` 是死代码，但被多处当判据引用

`ArenaSlots.cs` 文件头（:13、:17）· `BoardLayout.cs` · 多份 `资料/*.md` 都写「落点公式见 `MinionManager__FillMinionPositions.c:33-35`」。**真身是 `MinionManager.Awake`**（ILSpy `MinionManager.cs:131-200`）。数值一模一样，所以历次结论没受影响，但**判据出处该改**（铁律 5：翻错源不报警，只会在下次「按名字找实现」时找不到）。

### 5-4 `MoveCardIntoBattle` 音效挂错时间点

原版 `StartMovingFromHand`（拿起那一刻）就 `Play2D(MoveCardIntoBattle)`；我们挂在**出牌/部署**那一拍（`BattleDriver.cs:20591` / `:24614`）⇒ 拿起一张牌时没有声音。属音频/战斗界面线，行号见 #8 的账行。

### 5-5 `OutFromPlayingAreaAnim` 的 `LightPlayableHand` 在手牌链上没有入口

手牌「打得出」的亮/不亮由 `BattleDriver.RefreshHandPlayable():26819` 统一刷（回合变化 / 出牌后），**不由「回手」触发**。原版是「离场回手 → 立刻点灯」。症状会是「回手之后那张牌的高亮滞后到下一次刷新」——属低可见、但是真差异。与 5-6 同族。

### 5-6 `UnDissolveWholeCard` 全仓 0 命中

`MoveBackToHand` / `OutFromPlayingAreaAnim` 都在「战术卡（`cardType 0x14/0x28`）且 `targeted`」时调它（= 把溶了一半的卡补回来）。我们只有单向 `Dissolve`。

### 5-7 `SetAsCemetery` 的三句缺的是「观察面」而不是「实现难度」

`SetSortingLayer` / `SetSortingOrder(墓地序号)` / `SetColliderNotInHand` 都依赖「墓地列表」（原版的 `CemeteryManager` 存着一个有序列表，序号 = 列表长度）。我们**没有墓地列表**。要落这三句，得先建一个有序墓地容器。

---

## ⑥ 给调度台的请求

### 要跑的自检（我不跑 Unity）

1. **`BattleScene.Run`** —— 本批 14 条的可观察量全部落在它的发牌 / 格位 / 攻击三组断言上。**特别请求一条读数**（不是新断言，是打印现有量）：
   ```
   ArenaSlots.Position(0,false)   // 期望 −3.37（我方最外左）
   ArenaSlots.Position(0,true)    // 当前 +3.37（镜像）；按 §5-1 应改到 −3.37
   ArenaSlots.Position(8,false) / Position(8,true)
   ```
2. **`CardBaseDemo.Run`** —— 手牌侧 5 条（#9–#12、#7）的可观察量在那边的重排 / 拖拽断言里。
3. **`RuleEngineTest.Run`** —— 本批**没动引擎**，跑它只是防「别人半成品」；可以按 §16·8 的重同步点一起跑。

### 要看的东西

- `_tmp_view/shots/<日期>_B8F/` 下：(a) 我方抽牌三档时刻（0 / 0.075 / 0.15 s）的旋转角；(b) 拿起手牌前后缩放（0.730 → 0.803）；(c) 拖动带洞时的槽位分布。

### 账漂移预期

- `ArenaSlots.cs`：**只改注释、行数不变**（106-108 → 106-108）⇒ 预期 **0 漂移**。
- `HandLayout.cs` / `CardInteraction.cs` / `CardFeel.cs` / `BoardLayout.cs` / `CardView.cs`：**一个字节没动** ⇒ 0 漂移。
- **风险点**：账行 #13 引了 `BattleDriver.cs:26819-26840`、#5 引了 `:32358`。`BattleDriver.cs` 是**别人正在改的文件** ⇒ 合并后跑 `check_ledger_points.py` 时**这两处可能报漂移**，属预期（不是我改歪了）。若已挪行，按符号名 `RefreshHandPlayable` / `AttackStepTime + AfterAttackDelay` 重定位即可。
- 合并命令：`python -I -X utf8 工具/merge_ledger_parts.py --only B8_F --dry` → 确认 21 条全部 `恰好 1 处命中` → 去掉 `--dry` 真合并 → 把 `B8_F.tsv` 移进 `_parts/_merged_*/`。

### 需要拍板的一件（跨白名单）

**§5-1 的镜像**：改 `ArenaSlots.cs:109` 一行是廉价的，但要同步 `BoardLayout.mirror` / `NxOf` 并**重跑 `BattleScene.BuildAndSaveScene`**（场景里烘了 `mirror`）—— 属「Unity 批处理 + 跨文件」，得由主对话或专人做。**定之前，`MinionManager` 那 12 条几何行不要升 A。**
