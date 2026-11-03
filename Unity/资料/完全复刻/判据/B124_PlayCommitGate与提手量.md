# B124 · PlayCommitGate 与提手量 · 2026-11-03

只读诊断代理 R2。判据全部现读（`Assets/CardPresentation/**`），未跑 Unity / typecheck。

## 〇 结论（A/B/C）

**（B）只在批处理夹具里成立，真机不受影响。** 风险等级：**低**（不是「玩家拖不出牌」）。

一句话：`PlayCommitGate` 比的是**指针按下点 ↔ 指针当前点**，**代码里没有任何一处拿卡的位置当指针**；
`CardBaseDemo:201` 把**悬停抬起后的卡 transform** 当成「指针」喂进去 —— 那是夹具把两个量混了（与 `B124_ASSERTDEBT_1` §15 判 δ 一致）。

残留边角（与原版同构，⛔ 不是复刻偏离）：玩家若**主动把光标追到抬起后的卡上**再按下（按下点 y 可能 ≥440 px），
又**纯水平**拖到棋盘行（444 px）松手，则抬高 < 4.45 px ⇒ 这一帧不提交。原版同一套语义（见 §三），**不建议改产品**；
真机顺手确认一次即可（探针见 §五）。

## 一 PlayCommitGate（定义 / 调用点 / 用的坐标）

- 常量：`Hand/CardInteraction.cs:2345` `PlayLiftWorld = 0.3f * HandLayout.OurUnitsPerWorldUnit` = **0.04120 我们的单位 = 4.45 px**
  （原版 `GlobalVariables+0x7C` `minMovementToPlayCard = 0.3` 原版世界单位；判据 `decomp_full/BattleManager__Update.c:249-288`）。
- 定义：`Hand/CardInteraction.cs:2349-2353`
  ```
  if (nowWorld.y - pressWorld.y < PlayLiftWorld) return false;              // ① 比按下点抬高 0.3 原版世界单位
  return LayoutSpace.ToNormalized(nowWorld).y >= BoardLayout.HandLimitY;    // ② 越手牌限位线（300/1080）
  ```
  **两个实参都是「指针」的世界坐标**（`LayoutSpace.ScreenToWorld`）：`pressWorld` = 按下/起拖那一刻的指针，
  `nowWorld` = 当前帧指针。**没有卡的位置、没有屏幕坐标、没有设计坐标。**
- 调用点（产品，全 2 处 + 1 个壳）：
  - `:1084` `UpdateDrag` ⇒ `ResolveDrop(world, …, allowNearest:true)` 后调 `TryCommitWhileHolding(world, ok)`
    （原版「还按着就 commit」：越过高度那一帧就提交，⛔ 不等松手）。
  - `:2399-2406` `TryCommitWhileHolding` ⇒ `:2403` `if (!PlayCommitGate(_pressWorld, world)) return false;` ⇒ `:2404 Release(world, true)`。
  - `:1395` `Release`（真松手那一趟）：`if (ok && !PlayCommitGate(_pressWorld, world)) { … MissedSlot … }`。
- `_pressWorld` 的写入（都是**指针**）：`TickInput` 收 `world = PointerWorldSafe()`（`:623`/`:626`，`:1765-1792`）
  → PC 档按下即抓 `:706 PickUp(hc, world)` → `:891 _pressWorld = world`；触屏候选档 `:765 BeginPressCandidate`，过阈值后 `:828 PickUp` 再覆盖一次。
  与原版对得上：`CardScript__TryDraggingFromHand.c:89` 把**当前指针** `ScreenToWorldPoint(param_3)` 传给
  `StartMovingFromHand` → `BattleManager__StartMovingCardInHand.c:4` 写 `mgr+0x320`（= `pointerCursorStart`）。
- 自检读口（纯函数，可离线喂点）：`Editor/B120_W1_Assert.cs:173-190` 三态；`Editor/B120_W24_Assert.cs:252-279` 灭自证。

## 二 提手量 2.91（定义 / 施加点 / 时机）

- 常量：`Hand/HandLayout.cs:473` `HoverLiftWorld = 30f`（**原版世界单位**，判据 `VarsDesktop.cardInHandShownYOffset: 30`、
  `CardScript__ShowCardInHand.c:69-79` 是加在**世界 y** 上）⇒ `:265 hoverLift = 30 × OurUnitsPerWorldUnit = 4.1208 我们的单位 = 445 px`。
  （会话 119 `47a4ccee` 的动作是 **30 px → 445 px，14.835×**；`2.91`/314 px 是**本局夹具量到的差值**，不是常量。）
- 施加点：`HandLayout.cs:798`（`Refresh` 里悬停那一支）
  `pos.y = (areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + hoverLift;` —— **绝对 y**（手牌区底边 + 445 px），
  写的是**卡的 `localPosition`**（`:834 Place → :845 SetPose`），同时 `scale = hoverScale(1.3) * LayoutSpace.Scale`（`:799`）。
- 时机：`CardInteraction.swift UpdateHover`（`:723-747`）→ `SetHovered`（`:1956-1961`）→ `Relayout(false)` → `Refresh(hoveredIndex)`；
  悬停那张 `snap = true`（`:834`）⇒ **瞬切**，不走 0.2 s 补间。
- 结果（CBD 实测，`_tmp_view/CardBaseDemo_b123.log:616241`）：手牌中心 y −3.79 → −0.88（+2.91 我们的单位 = 314 px），
  **悬停卡心 = 屏底 + 445 px = 归一化 0.412，而棋盘行 `BoardLayout.lineY = 0.4110`（444 px）** —— 两者几乎等高，这就是「看着像要出事」的来源。

## 三 链路上的坐标来源对照

- 「点牌 → 抬起 → 拖动 → 松手」全程：**判定量 = 指针**。`_pressWorld`（按下/起拖指针）、`world`（当前指针）、
  `ResolveDrop(world, …)`、`ShowDropIndicator`、`PlayCommitGate`、日志 —— 全部同一个来源 `PointerWorldSafe()`。
- **视觉量 = 卡的 `transform`**（`HandLayout` 写 pos/scale）。两者**只在卡跟手时有滞后关系**，判据从不互借（`:1013`/`:1073-1079` 两条长注释就是当年为此订正的）。
- 关键的反证 —— **原版有「卡被抬走后指针留在原处」的接住区，我们有**：
  `Core/CardView.cs:14106-14113` 第二颗 collider `1.25 × 2.53 @ y −2.62`（注释 `:14111` 原文：「它是『卡被抬走后指针留在原处』那一段的接住区」）
  · `:14166-14191 SetCollidersShowBig` · `:14193-14197 BigRectContains` · `:9704` 命中并集 → 接线在 `CardInteraction.cs:1978 now.SetCollidersShowBig(true)`（`ApplyShowInHand`）。
  算一下：悬停卡心 445 px、缩放 1.3 ⇒ 接住区在屏高 **≈0…255 px**，正好盖住手牌带。
  ⇒ 真机上玩家光标停在手牌带里，卡抬起后**仍判这张被悬停**，按下点就在手牌带（低），
  再到棋盘行（444 px）⇒ 抬高 **≥189 px ≫ 4.45 px** ⇒ 闸过。
- 夹具与真机的差别（为什么是 B）：`Editor/CardBaseDemo.cs:201 it.SimulatePress(hoverCard.transform.position)` ——
  喂进去的是**抬起后卡的位置**（−0.88），随后 `:206-208` 拖到 `board.SlotPosition(3)`（−0.89）⇒ 抬高 **−0.01** ⇒ 必挡。
  `SimulatePress` 本身不读指针（`:1815` 只取 `HoveredCard` 再 `PickUp(h, world)`），所以「指针」完全由调用方给错。
  对照其它夹具（`B120_W16_Assert.cs:170-197`、`B120_W24_Assert.cs:194-197`）喂的是 **`home` = 悬停前的卡位**，闸都过。

## 四 修法或反证

**不改产品代码**（改了才是偏离原版：闸的语义、`pointerCursorStart` 的取值、445 px 的抬起量三者都与原版逐句对上）。

夹具修法（离线可做，归 W7/断言债那一路）：
1. `Editor/CardBaseDemo.cs:201` —— 按下点用**悬停前**的卡位（`:189` 已存了 `var homeBefore = hoverCard.transform.position;`）：
   `it.SimulatePress(homeBefore);`，并在注释写「press 的是**指针**，⛔ 不是卡；卡已被悬停抬起 2.91 单位」。
2. 补两条**前提**断言（照 `B120_W16_Assert.cs:161-170` 的形状，先断前提再断结论）：
   - `Check(LayoutSpace.ToNormalized(homeBefore).y < BoardLayout.HandLimitY, "（前提）按下点在限位线以下")`
   - `Check(slotPos.y - homeBefore.y >= CardInteraction.PlayLiftWorld, "（前提）这一拖抬高 ≥ 4.45 px")`
3. 灭自证：把「旧的错喂法」记成一条**必须为假**的断言
   `Check(!CardInteraction.PlayCommitGate(hoverCard.transform.position, slotPos), "抬起后的卡位当指针 ⇒ 闸必假（本条的来由）")`。

（可选、零风险）产品侧**只加日志**：`Release` 的 `_lastReleaseLog`（`:1424-1428`）同一物理行追加 `_pressWorld`，
真机一次拖拽就能读出「按下点 / 松手点 / 抬高」三个数，用来顺手验收 §〇 那条边角。**不改行为、零行号漂移。**

## 五 建议探针 / 断言

- 若要在真机上把 §〇 边角判死：读 `CardInteraction` 的 `_pressWorld` 与 `Release(world)` 的 `world` 两个 y（现已能只加日志拿到），
  外加 `HoveredCard` 的名字与它的 `transform.position.y`。**要看到的**：按下点 y 落在手牌带（≈0…255 px）⇒ B 成立。
- 离线断言（一条纯函数，无需 Unity）：`PlayCommitGate(press=(x, −0.88), now=(x, −0.89)) == false` —— 即「从抬起位水平拖 1 px 不出牌」是**规格**，不是 bug。
- 真实 hover 稳定性（顺手发现）：悬停那张瞬切到 445 px 后，靠**放大后的接住区**（§三）保持命中；
  `Editor/*Scene.cs` 是 edit mode、`Update` 不跑 ⇒ 这条**从没在 Unity 里跑过**，真机第一眼请顺带确认「悬停不闪、不跳牌」。

## 六 顺带核：CBD 那 4 条拖拽红的分工

- **2 条 = 本条（δ 夹具把卡位当指针）**：`log:616319` 落在槽 3（距离 3.527）+ `log:616331` `Placed[3]` 是空。
- **2 条 = `allowNearest:true` 判据变更（归 W7）**：`log:616373` 非法落点没被算成合法 + `log:616385` 弹回手牌第 0 位。
  W7 已把 `CardBaseDemo.cs:241` 的 `nowhere` 从 (0.12, 0.88) 改成 (0.12, **0.15**)（停在手牌带里 ⇒ 第二道闸挡），修法正确。
- 结论：4 条**不是同一个原因**，各 2 条；本条只负责前 2 条，且**判定为夹具问题**。
