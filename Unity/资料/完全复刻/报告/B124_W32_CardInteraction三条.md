# B124_W32 · `Hand/CardInteraction.cs` 三条（CANDROP / DROPIND / QUEUETOP）

> 执行写手 W32b · 2026-11-03 · 只动 `MyGame/Assets/CardPresentation/Hand/CardInteraction.cs`（本文件 2850 行，**行数零变化** · CRLF 未翻）· 未开 Unity · 未动 git。

## 一、`B124-CANDROP` —— `CanDropAtSlot` 默认值

**判据（亲读）**：原版 `BattleManager__CanPlayCard.c:95-120` 对**单位卡**只问 `MinionManager.IsAvailableSlot(mm,1)`（有没有空位），无候选 ⇒ `NotifyCantDoAction(Battle/Tips/NotEnoughRoom)`（`showTips:1` 支，**出声**）；`GetClosestAvailableSlot` 无候选返回 0/无格（`il2csharp/MinionManager.cs:321-390`）。
⇒ **原版没有「槽 −1 也算能打」的路径**：解不出格 = 拒 + 出声。我们的等价物已在位（`DropReject` → `NoRoomBranch` → `EngineRefused` → `OnIllegalAction` cantdo），**出声这半本来就对**。

**改动**（`:216`）：`(slot, card) => true` → **`(slot, card) => slot >= 0`**。

- ⛔ 不改恒 `false`：本默认值是**表现层自检的免引擎替身**，恒 false 会让一批夹具出不了牌。
- **现状核查（重要）**：`DropReject` 三条 `return null`（`:1307/:1331/:1353`）**全部被 `TryResolveSlot` 成功守住**，而 `BoardLayout.TryResolveSlot:540` 与 `ArenaSlots.TryResolveSlot:362` 都返回 `slot >= 0` ⇒ **「槽 −1 判 Ok」今天是结构上不可达的**；本次是**防御性收紧**（谁哪天拆掉那道门，旧默认会静默放行 −1）。

**靠默认值的夹具（不写 `CanDropAtSlot` 的 10 件）**：`B117F11_Assert` · `B117_G10_06_Assert` · `B117_WGB_Assert` · `B123_C3_Assert` · `B94X2_Assert` · `B97W37_Assert` · `B97_UI2_Assert` · `B99B1_Assert` · `B120_W21_Assert` · `CardBaseDemo`（后两者真跑拖拽/松手）。
**受影响 = 0**：它们全在「已解出槽位」的路上走，`slot >= 0`；而 `TryResolveSlot` 失败时**根本不会去问**这个委托。⛔ 无一需要改。

## 二、`B124-DROPIND` —— 底片挪到提交之后（已改产品侧）

**判据（亲读）**：`MinionManager.ReassembleMinionsWhilePlayingUnit`（`il2csharp/MinionManager.cs:1572-1612`）只在 **`num1 != lastUnitPos`** 时 `SetCardShadow(pos, showY:1, boardPosition:1)`；唯一调用点在 `BattleManager.cs:25963` 的 **`case 3 = playingMinion`（已提交、正在落地）**（`case 4/5` = 两种 `playing*Spell` 同族）；隐藏那一半在 `ReassembleMinions`（`MinionManager.cs:1406/1412`，`showY:0`）。**原版拖拽中（`case 2`）根本不点底片，且 `case 3` 里没有任何 y 闸。**

**改动**：

1. `UpdateDrag`（`:1097`，原 `ShowDropIndicator(litB, …)`）→ 改成说明注释，**不再在拖拽中点亮**（原句本来就是死码：`TryCommitWhileHolding` 在前且充要条件相同）。`litB` 仍喂 `OnDropPreview`（让位预览，另账）。
2. `Release` 的 `ok` 支（`:1439`，同一物理行追加）：`if (!tactic) ShowDropIndicator(which, land, card);` —— **提交之后**才点，落格 = 真落点 `land`，只单位牌（`…WhilePlayingUnit`）。
3. `DeploySequence.Play` 的 `onDone`（`:1485`，lambda 内）与 `CancelDeployFlight`（`:2732`）各加一句 `ShowDropIndicator(null, -1, null)` = 原版 `ReassembleMinions` 的 `showY:0` / 撤销那一档。
⇒ 现在「拖拽中看不到、提交后到落地前短暂可见」是**真可达状态**。

**受影响断言（`Editor/BattleScene.cs` 不归我，以下是可粘贴补丁，均**保持行数不变**）**：

- **A（必改）`BattleScene.cs:6769-6770`**（松手后马上断言全灭，现在会红）→ 换成两行：

```csharp
                Check(pBoard.LitSlotCount == 1 && pBoard.LitSlot == freeSlot,   // 🔴 2026-11-03（`B124-W32`）：底片已按原版挪到**提交之后**（`case 3`）⇒ 提交那一刻是**亮着的**
                      $"★ 提交后落点底片**亮在它会落的那一格**（亮 {pBoard.LitSlotCount} 格、第 {pBoard.LitSlot} 格；应第 {freeSlot} 格）—— 落地/撤销才灭");
```

- **B（建议加，零行数开销）`BattleScene.cs:6878`**：`Step(0.2f);` 同一行追加

```csharp
 Check(pBoard.LitSlotCount == 0, $"★ 落地后落点底片**全灭**（还剩 {pBoard.LitSlotCount} 格）—— 原版 `ReassembleMinions` 的 `SetCardShadow(…, showY:0)`");
```

  （`onDone` 里清除排在 `OnDeployed` **之前**，而该循环以「引擎里那一格填上」为出口 ⇒ 出循环时必已熄灭。）

- **C（文字过期，非必须）`BattleScene.cs:6641-6643 / 6766`**：原注释「产品里结构上不可达 / 见 `B124_W20` 报告」与截图名 `2c_落点指示_拖拽中` 已过期（`:6644` 那句直接 `AttachDropSlotTo` 仍可留作拖拽期探针）。

## 三、`B124-QUEUETOP` —— 起拖档 3000 → 3001（已定案）

**判据链（亲读，四跳）**：

1. `CardScript.StartMovingFromHand`（`il2csharp/CardScript.cs:5162-5186`）= 设 `cardState = inHandMoving(9)` + 音效 + `StartMovingCardInHand` + `DOScale` + `DisplayCreatedByText`，**全篇没有 `SetSortingLayer`**。
2. 进它只有一个口：`TryDraggingFromHand`（`:4595-4660`），而 `OnTouchDrag`（`:4556`）只在 **`cardState == 8`(`inHandShowing`)（`:4581`）** 时调它。
3. state 8 由 `ShowCardInHand`（`:4893`）设，那里第一句级就是 **`SetSortingLayer("HandMoving")`（`:4944`）**。
4. ⇒ **起拖那张必然在 `HandMoving` 层**（高于整条 `Hand`），起拖本身不改层。原版三处 `SetSortingLayer("HandMoving")`（`:4045` `ShowCardInCenter` · `:4092` `ShowCardBeingDrawn` · `:4944` `ShowCardInHand`）没有一处属于拖拽路径。

**改动**（`:965`）：`card.SetQueue(hand.queueTop)`（3000）→ **`card.SetQueue(CardView.QHandMoving)`（3001）**，与 `ApplyOutFromPlayingArea` / `ReenterDragAfterCancel` 统一到一条口径。
额外理由（我们侧）：3000 是 `HandLayout.Refresh:818` 给**悬停那张**的号；起拖期间仍可能逐帧 `Relayout` 且 `HoveredIn` 给出别的卡 ⇒ 两张同号 3000、分层退化成按距离（`CardView.QHandMoving` 的号段注释）。3001 严格高于手牌段顶 3000，无平手。

**顺手发现（不改，交给调度台）**：`ApplyOutFromPlayingArea` 对应的原版 `CardScript.OutFromPlayingAreaAnim`（`:5578-5607`）**同样不设层**（只有 dissolve/`cardState=9`/`LightPlayableHand`）⇒ 我们那里的 `QHandMoving` 是**等价推断**而非逐字；撤出牌后原版那张的层沿用「落地前」的 `HandMoving`（`MoveIntoFieldFromHand:5529` 也没设层），所以 3001 不与之冲突。若以后要更严，需要 `MoveToPositionInPlay` 那侧 `SetSortingLayer("Elements")` 的时序判据。

## 四、验证

- `TMPDIR=/tmp/wf_b124w32b bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**。
- `工具/check_ledger_points.py`：**账里没有任何一行落点在 `CardInteraction.cs`**（`grep CardInteraction` 输出为空）⇒ 本次零账影响。闸现读 `A 5909 · 通过 5838 · 漂移 63 · 失败 8`，漂移/失败**全在 `Shell/CollectionWindow.cs` / `DeckInfoPopup.cs` / `CardFeel.cs` 等别的文件**（与本次无关，未动）。
- 文件 **2850 行**（改前 2850）· `b.count(b'\r\n') = 2850`、裸 CR = 0 · 所有新增代码都在行尾 `//` **之前**。
