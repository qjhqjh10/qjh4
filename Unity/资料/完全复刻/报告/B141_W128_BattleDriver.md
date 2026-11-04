# B141-W8 · `BattleDriver` 独占包（战-T09 S2 + B124-TACTDROP）

> 执行写手 `B141-W8` · 第一百二十八会话 · 2026-11-04
> 白名单内只改：`Battle/BattleDriver.cs` · `Hand/CardInteraction.cs` · 新建 `Editor/B141_W8_Assert.cs` · `Editor/BattleScene.cs`（一行接线）。
> ⛔ 未动 `BoardLayout.cs`（不需要）· `B112_R3C_Assert.cs` / `B120_W16_Assert.cs`（经核**不需要改**，理由见 §B·4）·
> ⛔ 未跑 Unity / 未动 git / 未改 `d:/2` / 未改账主表 / 未改 `EffectResolver.cs`·`RuleCore.cs`。
> 类型检查：`cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w8 bash 工具/typecheck.sh` ⇒
> **运行时 0**（每次都是 0，含全部改动之后）；**编辑器**：本包自己那次是 **0**，最后一次复跑变成 **27 条、全部在
> `Assets/RuleEngine/Editor/B141_W15_Assert.cs`**（`DeckLibrary.SaveCurrentDeckText` 等 7 个成员不存在）——
> **那是别的写手正在写的文件**，`typecheck.sh` 输出里 `B141_W8` / `BattleDriver.cs` / `CardInteraction.cs` / `BattleScene.cs`
> **一条都没有**。⇒ 本包文件编译干净；27 条不是本包的。
> （中途另有一次编辑器报错在 `Assets/RuleEngine/Editor/B141_W9_Assert.cs`，同样是别的写手在飞的文件，随后自行归零。）

---

## 〇 · 一句话

**A（战-T09 S2）已落地**：新增两条唯一判据 `UiTopWindow()`（窗口层）/ `UiStateNow()`（战斗阶段，补齐 4/5/12/13 四档），
`Update` 的窗口模态分支全部改由 `UiTopWindow()` 把门，`ChatHotkeyUiState()` 退化成一行转发。
**B（B124-TACTDROP）已落地**：非单位 `targeted` 卡的目标改取**松手时指针下的那张卡**（`DropPointerUnit` → 原版 `GetTargetInPos`），
敌方督军格命中 `pool.Contains` 即结算，不再解到邻格、不再多要一次点击。

---

## 一 · 战-T09 S2（两条轴收口）

### 1·1 新增（类尾追加，`BattleDriver` 类内、原 `:42122` 之前）

| 现读行号（改后） | 成员 | 作用 |
|---|---|---|
| `BattleDriver.cs:42135-42174` | `UIWinNone … UIWinAlliance` 九个常量 | 通道 A 的窗口序号（**数值顺序 = `Update` 的分支先后**） |
| `BattleDriver.cs:42151` | `public int UiTopWindow()` | 通道 A 唯一判据：按优先序返回最上面那扇可见窗 |
| `BattleDriver.cs:42172` | `public int UiStateNow()` | 通道 B 唯一判据：原版 `UIstate(+0x290)` 15 档 |
| `BattleDriver.cs:42188` | `bool DeployingNonUnit` | 把 `playingMinion(3)` 与 `playingNonTargetSpell(5)` 分开 |
| `BattleDriver.cs:42217` | `UnitState DropPointerUnit(out int owner, out int slot)` | 原版 `GetTargetInPos` 的等价物（指针下那张卡） |
| `BattleDriver.cs:42246` | `public UnitState LastPlayTargetForTest` | 自检读口（`_chosenTargetForNextPlay` 会被 `DoPlay` 消费掉） |

### 1·2 `UiStateNow()` 的 15 档（原版值 vs 我们的等价物）

原版枚举 = `dump.cs:46314-46332`；逐档表 → `T09b_uGUI拦路判据.md` §B·1。

| 档 | 原版名 | 我们的等价物（现读） | 改前 |
|---|---|---|---|
| 0 | inactive | `Ctx == null` | 有 |
| 1 | normalBattle | 兜底 | 有 |
| 2 | movingHandCard | `interaction.IsDragging` | 有 |
| 3 | playingMinion | `WaitingForBlockingAction` 且演的是**单位** | 有（不分单位/非单位） |
| **4** | **playingTargetedSpell** | `AwaitingPlayTarget` 且 `_awaitPlayTargetDef` **非单位** | ❌ **缺**（原统一返回 9） |
| **5** | **playingNonTargetSpell** | `WaitingForBlockingAction` 且 `DeployingNonUnit` | ❌ **缺** |
| 6 | minionAttacking | `selector.Visible` | 有 |
| 7 | finishBattleSequence | `_endPanel.Visible` | 有 |
| 8 | postBattle | `Ctx.IsOver` | 有 |
| 9 | choosingUnitTarget | `AwaitingPlayTarget`（单位）或 `_selectedSlot >= 0 && _command != Ability` | 有 |
| 10 | targetedActiveAbility | `skillPanel.Visible` | 有 |
| 11 | mulligan | `InMulligan` | 有 |
| **12** | **mulliganFinished** | `_mulliganSettleViews != null`（`B117-T07` 的逐张回库收尾泵） | ❌ **缺** |
| **13** | **choosingAbilityTarget** | `_selectedSlot >= 0 && _command == Ability` | ❌ **缺**（原来也返回 10） |
| 14 | choosingResolutionCard | `_choosePanel.Visible` | 有 |

⚠️ 两处**有意的等价改写**（行为面在 `HandleChatHotkeyEsc` 里同档，所以不改行为）：
`_command == Ability && _selectedSlot >= 0` 由 **10 → 13**（`HandleChatHotkeyEsc` 对 10/13 都是 `ClickChat`）；
`WaitingForBlockingAction` 拆 3/5（两者都在「什么都不做」那一组）。
`AwaitingPlayTarget` 由 9 拆 4/9 则是**真修正**：原版 `Update.c:1303-1335` 里 `2/3/5/14` 与 `4` 都是「什么都不做」，`9` 才 `ClickChat`。

### 1·3 `Update` 的 17 条 if-return 怎么收的口

现读 `Update()` 起于 `BattleDriver.cs:14285`。**没有重排**（重排会动 `AdvanceTimeline` / `HandleReplayBar` / `Ctx.IsOver` 的相对次序），
而是把每条分支的**门**换成唯一判据（逐行就地改，**零行数变化**）：

| 现读行 | 改后 | 通道 |
|---|---|---|
| `14355` | `if (UiTopWindow() == UIWinMultiCards && TickMultiCards()) return;` | A |
| `14360` | `if (UiTopWindow() == UIWinCardDisplay && TickCardWinPointerExit(WorldPointer())) return;` | A |
| `14363` | `if (UiTopWindow() == UIWinCardDisplay && TickCardDisplayClick()) return;` | A |
| `14408` | `if (UiTopWindow() == UIWinMulligan && TickMulligan(Time.deltaTime)) { UpdateHud(); return; }` | A |
| `14409` | `if (UiTopWindow() == UIWinMulligan && HandleMulligan()) …` | A |
| `14410` | `if (UiTopWindow() == UIWinChoose && HandleChoose()) …` | A |
| `14436` | `if (UiTopWindow() == UIWinSettings && HandleSettings()) …` | A |
| `14449` | `if (UiTopWindow() == UIWinChatPopup && HandleChatPopup()) …` | A |
| `14457` | `if (UiTopWindow() == UIWinBattleLog && HandleBattleLog()) …` | A |
| `14461` | `if (UiTopWindow() == UIWinAlliance && HandleAlliancePanel()) …` | A |
| `14421`（调用点）/ `12786`（本体） | `HandleEndBattleExit()` 内部：`if (!ExitReady \|\| (!_replaySession && UiStateNow() != 8)) return false;` | B |
| `14439`（调用点）/ `6119`（本体） | `HandleSettingsEsc()` 内部：`stEsc = UiStateNow(); if (stEsc == 6 \|\| 9 \|\| 10 \|\| 13) return false;` | B |
| `14445`（调用点）/ `36271`（本体） | `HandleChatHotkeyEsc()` 内部：`int st = UiStateNow();` | B |
| `14448` + `14396`（调用点）/ `8761`（本体） | `HandleTimeControlKeys()` 内部：`if (UiStateNow() == 0) return false;` | B |
| — | `HandleCameraResetButton()`：**保持不动**（`=> false`，手搓路径已退役，没有窗口/状态可问） | — |
| — | 回放支那两条（`HandleTimeControlKeys` / `HandleReplaySessionExit`）**不动**（是回放帧循环的出口，不属两条轴） | — |

⚠️ **`HandleSettingsEsc` 没有改成 `UiStateNow() != 1`**（施工图原句）。理由：那样会连带把 `3/5`（演出中）也挡掉，
而批处理里 `_blocking` 会一直挂着（`B118W23_Assert` 的 N2 已经实录过）⇒ **会把 `BattleScene` ③「ESC ⇒ 开设置窗」打成假红**。
现写成 `IsTargeting ∨ selector.Visible` 的**等价状态集** `{6,9,10,13}`（仍是唯一判据取数，⛔ 不重判那两个字段）。

### 1·4 `DrivePlayerTurn`（`:14938` 起）

`int uiSt = UiStateNow();` 取一次；两条守卫改成 `uiSt == 2 || (uiSt == 6 && interaction.IsDragging)` 与 `uiSt == 6`。
⚠️ **不是简单的 `st == 2`**：映射里 6 档排在 2 前面 ⇒ 「一边拖手牌、一边选择器开着」时 `uiSt == 6`，
而**原行为是拖拽优先**（那段注释写着「两边读同一个鼠标」）⇒ 补了这一格，保证逐位等价。

---

## 二 · B124-TACTDROP（战术卡落点含敌方督军格）

### 2·1 原版判据（第一权威，现读）

- `decomp_full/BattleManager__GetTargetInPos.c:104` / `:248`：`Physics2D.GetRayIntersectionNonAlloc` / `Physics.RaycastNonAlloc` 取**指针下的 `CardScript`**。
- `BattleManager__Update.c:136-139`：松手那一刻把它存进 `+0x440`。
- `BattleManager__Update.c:676-684`：case 4 松手支拿它过 `IsValidSpellTarget`。
- `BattleManager__IsValidSpellTarget.c:28-66`：只要求 `IsInPlay` + targeted + 非隐藏/护盾/伪装 + criteria，**没有任何格号 / 候选集** ⇒ **敌方督军是合法落点**。

### 2·2 我们原来的链（病根）

`CardInteraction.DropReject`（`:1301` 战术卡分支）→ `BoardLayout.TryResolveSlot`（`:535` 过滤）→
`ArenaSlots.IsCandidate`（`:297` **督军位恒 false**）⇒ 落点被解成**邻格 3/5** →
`TryBeginPlayTargetPick` 非单位分支拿 `Ctx.Players[owner4].Board[slot]` ⇒ **永远取不到督军格**：

- 邻格空 ⇒ 进「等目标」态 ⇒ **比原版多一次点击**；
- 邻格有人且也在池里 ⇒ **静默打错目标**（平手取更早下标 = 3 号格）。

### 2·3 改法（原版全量的「取指针下那张卡」）

| 文件 | 现读行号 | 改了什么 |
|---|---|---|
| `Battle/BattleDriver.cs` | `8147-8188`（`TryBeginPlayTargetPick` 非单位分支） | 先问 `DropPointerUnit(out ptrOwner, out ptrSlot)`；`pool.Contains(ptrUnit)` ⇒ `landed = ptrUnit`（当场结算）。**格号那条降为兜底**（只在「没有指针命中信息」时走，= 自检 / `-wfdrive`，行为逐位不变） |
| 同上 | `8064` | `_chosenTargetForNextPlay = null; LastPlayTargetForTest = null;`（自检读口每问一次先清） |
| 同上 | `8187` | 结算那一支多写一句 `LastPlayTargetForTest = landed;` |
| 同上 | `42217` | 新 `DropPointerUnit`：`interaction.HasLastDropWorld ? LastDropWorld : WorldPointer()` → `HitSlot(_myUnits/_foeUnits, world)`（**复用** `HitSlot`，⛔ 不另写命中） |
| 同上 | `8424` | `SimulateCardDroppedForTest` 先 `interaction.ClearLastDropWorld()` —— 这条口**不经 `Release`**，不清就会拿上一手真实拖拽的松手点去解目标 |
| `Hand/CardInteraction.cs` | `2866-2877`（类尾） | 新 `LastDropWorld` / `HasLastDropWorld` / `SetLastDropWorldForTest` / `ClearLastDropWorld` |
| 同上 | `1382`（`Release` 第一句之后） | `LastDropWorld = world; HasLastDropWorld = true;` |

**为什么必须记 `LastDropWorld`**：我们的结算延后到 `DeploySequence` 走完（0.65 s），
那时 `WorldPointer()` 早不是松手那一点——直接读指针会把「压在督军上」解成别处（静默）。

### 2·4 夹具同步：经核**不需要改**

| 夹具 | 现读 | 为什么不用改 |
|---|---|---|
| `Editor/B112_R3C_Assert.cs:147` | `SimulateCardDroppedForTest(handIdx, emptySlot)` | 该口不设 `LastDropWorld` 且现在**主动清掉** ⇒ `DropPointerUnit` 退回 `WorldPointer()`（批处理无指针）+ 格号兜底 ⇒ **逐位不变**，仍进等目标态（A4 依旧成立） |
| `Editor/BattleScene.cs:29632` | `SimulateCardDroppedForTest(bhi, 2)`（**单位卡**） | 走的是 `unitCard && n == 0` 那一支，**根本不进非单位分支** |
| `Editor/B120_W16_Assert.cs:158/231/271` | `SimulateDrag` / `Release`（**单位卡** + 提交闸） | 同上；`Release` 多写一个字段，不改任何判定 |

### 2·5 顺带订正（铁律 5）

`BattleDriver.cs:8146` 那条 `🔶 自己设计` 的前提「原版 `FUN_180617800` 未导出、两种读法相反」**已站不住**：
现读 `BattleManager__Update.c:635-673` 明确 = 松手时指针下没有合法目标 ⇒ **牌回手 + `DragToTarget`**，**原版没有「再点一次目标」这一态**。
**标记保留**（我们仍比原版多给一次点选），但理由换成现读事实；改成原版全量（回手）会动 196 张非单位 `targeted` 卡的既有链，**未做**。

---

## 三 · 断言（新文件 `Editor/B141_W8_Assert.cs`，337 行）

接线：`Editor/BattleScene.cs:45405` 那条**活 driver 聚合行**，`driver.DetachStaticHooks();` **之前**、
全文件最末（同一物理行追加 ⇒ **BattleScene 行数零变化**）。

| # | 断言 | 🧨 改坏法 |
|---|---|---|
| A1 | `UiStateNow()` ∈ 0…14 | —— |
| A2 | `ChatHotkeyUiState()` 与 `UiStateNow()` **同值**（转发壳） | 在 `ChatHotkeyUiState` 里另写一份映射 ⇒ 红 |
| A4/A5/A6 | `UiTopWindow()`：全关=`None` · 设置开着=`Settings` · 设置+日志同时开仍是 `Settings`（优先序） | 改 `UiTopWindow()` 的 if 顺序 / 改常量数值 ⇒ 红 |
| A7 | 非单位卡在等目标 ⇒ `UiStateNow() == 4` | 删 4 档退回 9 ⇒ 红 |
| B5 | **灭自证**：指针下什么都没有 ⇒ 进「等目标」态 | 把「指针下的卡」写成恒真 ⇒ 红 |
| B8 | **★★** 指针压在敌方督军格 ⇒ `LastPlayTargetForTest == 敌方督军` | 退回 `Board[slot]` ⇒ null/邻格 ⇒ 红 |
| B9 | ……且 `!AwaitingPlayTarget`（当场结算，不多要一次点击） | 同上 |
| C1 | 源码闸：`UiStateNow()` 里 `? 4 : 9` · `return 12;` · `? 13 : 9` · `DeployingNonUnit ? 5 : 3` **四档都在** | 删任何一档 ⇒ 红 |
| C2/C2b | `ChatHotkeyUiState()` 只剩转发；全文件只有**一处** `if (Ctx == null) return 0;` | 第二份映射回来 ⇒ 红 |
| C3 | `Update()` 里 ≥9 条窗口分支由 `UiTopWindow()` 把门（现读 = **10**）；三条裸写法已无 | 改回裸 `Visible` ⇒ 红 |
| C4 | `HandleSettingsEsc()` 用 `UiStateNow()`，且**不再**出现 `IsTargeting` / `selector.Visible` | 写回来 ⇒ 红 |
| C5 | `HandleEndBattleExit()` 带 `postBattle(8)` 闸 | —— |
| C6/C6b/C7/C7b | `TryBeginPlayTargetPick` 先问 `DropPointerUnit(out …)`、格号只在它之后；`DropPointerUnit` 复用 `HitSlot` | 退回格号优先 ⇒ 红 |
| C8 | `CardInteraction.Release` 里 `LastDropWorld = world;` | 删掉 ⇒ 真机又会解到邻格 |

**⚠️ 覆盖边界（如实记）**：
- **批处理没有相机 / 指针** ⇒ `HitSlot` 在产品路径上恒不命中。B 组用 `SetDropPointerForTest` / `SetDropPointerNoneForTest`
  （= `EscapePressedForTest` / `PointerHeldForTest` 同族的钉死口）喂「指针下是哪一格」；
  **产品路径与断言走的是同一条 `DropPointerUnit`**（钉死口就在它第一句，其后是同一段 `HitSlot` 逻辑）。
- **12 / 5 两档只有源码闸**：`mulliganFinished(12)` 要真跑换牌收尾、`playingNonTargetSpell(5)` 要真机上非单位法术在演 ——
  批处理构造不出来，**不硬凑**。
- B10（手牌数）**只记录不计分**：引擎收不收这一手取决于费用/回合，不在本条要验的范围。
- 真机手感（ESC / 拖动 / 点击）仍归 `资料/真Play待验清单.md`。

---

## 四 · 账闸影响（给调度台重钉）

**文件行数变化**（本包全部改动）：

| 文件 | 改前 | 改后 | 变化 |
|---|---|---|---|
| `Battle/BattleDriver.cs` | 42,205 | **42,336** | **+131** |
| `Hand/CardInteraction.cs` | 2,850 | **2,865** | **+15** |
| `Editor/BattleScene.cs` | 51,505 | 51,505 | **0**（同一物理行追加） |
| `Editor/B112_R3C_Assert.cs` | 253 | 253 | 0 |
| `Editor/B120_W16_Assert.cs` | 336 | 336 | 0 |
| `Editor/B141_W8_Assert.cs` | —— | **337** | 新建 |

**漂移从哪里开始**：`BattleDriver.cs:8146` 之后**不再逐行匀速**（`TryBeginPlayTargetPick` 那段 +6、`HandleSettingsEsc` −1、
`ChatHotkeyUiState` −14），到类尾再 +131。⇒ **必须由调度台在「没有 `.cs` 写手、补丁已合并完」时跑一次
`check_ledger_points --all-A --fix`**，然后**复跑闸看真实数字**（`--fix` 第一遍印的是修前快照）。

**只读跑了一次闸（2026-11-04，本包改动之后）**：`A 5966 · 通过 5550 · 漂移 386 · 失败 32`。
- `漂移 386` 里 `战斗界面_HUD` 160 + `战斗逻辑驱动` 108 与本次行号位移一致（其余散在别的写手在飞的文件）。
- **32 条失败里有几条是本包位移造成的**（例：`战斗界面_HUD · BattleManager.GetUIState → BattleDriver.cs:36264（ChatHotkeyUiState）`、
  `战斗逻辑驱动 · GetTargetInPos → BattleDriver.cs:33318（HitSlot）` —— 两条的落点现在都指到注释行）。
  ⛔ **别当成「本包改错了语义」**；重钉后再看还剩几条。
- 其余失败（`个人资料_成就_排行` / `其他_基础设施` / `卡牌面_渲染` / `本地化` / `日常_奖励` / `特效动画` / `菜单_外壳`）
  **不在本包文件内**（`个人资料_成就_排行` / `Shell/*` / `RuleEngine/EffectsToShow.cs` 等）⇒ 是别的写手或既有账。

---

## 五 · 还差什么（如实记）

1. **真机**：`Update` 的窗口层只在批处理里被「开/关窗」验过；「同一屏幕点一次只做一件事」要真 Play（`资料/真Play待验清单.md`）。
2. **`UiTopWindow()` 的窗口清单是 8 扇**（多卡 / 放大 / 换牌 / 选牌 / 设置 / 聊天气泡 / 日志 / 联盟）。
   原版还有 `BattleDoors` / `CemeteryLogPanel` 那一层 —— 我们这两件与日志窗同层，**未单独建 id**（今天行为等价：都走日志窗那一支）。
3. **2 档 / 3 档的 `UiStateNow()` 只在「帧循环里」有意义**：自检里 `_blocking` 要 `SetBlockingForTest(false)` 才能落到 1
   （`B118W23_Assert` 已记，本包没有改它的前提）。
4. **`🔶 自己设计` 那条（战术卡落点无合法目标 ⇒ 进点选态）仍是放宽版**：改成原版全量（回手 + `DragToTarget`）会动
   196 张非单位 `targeted` 卡的既有链，属**另一件活**（判据已现读清楚，见 §2·5）。
5. **未验**：`DeployingNonUnit` 的 `IsDeployFlowing` 判定在真机 0.65 s 飞行窗口里是否稳定（批处理里飞行为同步完成 ⇒ 覆盖不到 5 档）。

---

## 六 · 出处索引

- 原版窗口层：`WindowsManager.cs:995-1006` · `T09b_uGUI拦路判据.md` §A·2
- 原版战斗阶段：`dump.cs:46314-46332`（15 档）· `T09b_uGUI拦路判据.md` §B·1
- 原版 ESC 两条消费者：`BattleManager__Update.c:1303-1335` · `BattleHud__Update.c:20-35`
- 原版战术卡落点：`BattleManager__GetTargetInPos.c:104/:248` · `BattleManager__Update.c:136-139,635-673,676-684` ·
  `BattleManager__IsValidSpellTarget.c:28-66`
- 施工图：`资料/完全复刻/报告/B128_执行_战斗表现.md` §一·A（S2）/ §一·B（S3-S4）
