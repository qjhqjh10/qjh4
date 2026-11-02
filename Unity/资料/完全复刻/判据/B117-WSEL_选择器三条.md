# `B117-WSEL` · `B117-R9`（「战斗选择器有问题」）三条补丁落地报告

执行代理 `B117-WSEL` · 2026-11-03 · 写者白名单 = `Battle/BattleDriver.cs`（本报告除外）
判据正本 = `判据/B117-G10-08_目标选择.md` **§〇（结论）+ §三（三条补丁）**

> **一句话**：三条补丁全部落地，`typecheck` 运行时 0 / 编辑器 0；落地时**多做了两处「配套」**——
> 补丁①必须同时改 `DrivePlayerTurn` ① 段的早退，否则「删掉 `Hide()`」= **攻击整条失灵**；
> 补丁③的落点从 `AttackSelector.Selectable` 改到 `CommitCommand`，否则会**把整条指令取消掉**。理由都写在下面。

---

## 一 · 三条补丁到底改了什么（`文件:行号` · 旧 → 新）

### 补丁①（选择器活到 `StopTracking`）—— **两处，缺一不可**

**(1a) `Battle/BattleDriver.cs:15693`**（`CommitCommand` 里）
```csharp
// 旧
if (selector != null) { selector.SetSelected(kind); selector.Hide(); }
// 新
if (selector != null) selector.SetSelected(kind);   // ⛔ 不在这里 Hide（原版 AttackButtonClick 全篇无 Toggle(false)）
```
收尾仍由 `ClearSelection()` 里的 `selector.Hide()`（`:16631`）负责：结算路 `DoResolve` 末句 `ClearSelection()`（`:16435`）、
取消路 `CancelTargetChoosingByInvalidClick`（`:14905`）/ 槽外取消（`:15006`）⇒ 等价原版 `StopTracking`。

**(1b) `Battle/BattleDriver.cs:15011`**（`DrivePlayerTurn` 的 ① 段末尾）—— ⚠️ **判据 §三·补丁① 没写这一处，但不改就是 P0 回归**
```csharp
// 旧
return;
// 新
if (_command == AttackKind.None) return;
```
**为什么必须改**：① 段是 `if (selector != null && selector.Visible) { … return; }`，**整段把后面的流派一帧吃光**。
改前「三颗钮一选定就 `Hide()`」⇒ 下一帧 `Visible == false` ⇒ 松手那一拍才轮得到 ④（`ReleasedThisFrame()` → `Resolve`）。
**删掉 `Hide()` 之后若这里仍无条件 `return`，选择器一直开着 ⇒ 松手永远被吞 ⇒ 近战/远程/技能攻击全部打不出去**（且不报错）。
新写法只让「还没定打法」那一段独占帧；定了打法就往下走，由 ④ 结算 = 原版 `BattleManager__Update.c` 的 `case 6`
（`__Update.c:1021-1091`：`attackType == 4` ⇒ `ExecuteActiveAbility`，否则 `IsValidAttackTarget` ⇒ `AddAttackAction`，两条都 `StopTracking`）。

**(1c) 同段 `BattleDriver.cs:15004`**（`1b` 的必然配套）
```csharp
// 旧
if (ClickedThisFrame() && !selector.ContainsBar(world))
// 新
if (_command == AttackKind.None && ClickedThisFrame() && !selector.ContainsBar(world))
```
**为什么**：这条「**按下**在选择器外 ⇒ 取消」是**我们自造的**（原版没有；判据 §三·另外两条 #2）。
改前它被 `Hide()` 掩盖（选完打法选择器就没了，这一支行不到）。选择器留下之后，**在目标身上按一下就会被当成「槽外取消」** ⇒
改成只在「还没定打法」时取消；定了打法以后按下/松手都归 ④（原版：**抬起** ⇒ 非法目标 ⇒ `CancelAttack`）。

### 补丁②（起手就带 `card.attackType`）—— `BattleDriver.cs:15325`（`OpenCommand`）
```csharp
// 旧
_selectedSlot = slot;
// 新
_selectedSlot = slot; _command = u.LastAttackType == UnitState.AttackTypeSpell
                                 || u.LastAttackType < 0
                                 || u.LastAttackType > UnitState.AttackTypeActive
                                 ? AttackKind.None : (AttackKind)u.LastAttackType;
```
- 值域**逐值等于** `AttackSelector.KindOf`（`0→None / 1→Melee / 2→Ranged / 4→Ability`，`3(Spell)` 与认不出 ⇒ `None`）。
  `AttackKind` 与 `UnitState.AttackType*` 同号（`AttackSelector.cs:95-101` · `UnitState.cs:663-672`），所以是纯映射、不是新口径。
- 判据：`BattleManager__StartAttackFrom.c:20-40`（`uVar1 = *(int*)(param_2 + 0x120)` 直接喂 `PrepareMovement`）+
  `BattleManager__Update.c:1071-1078`（抬起那一帧 `AddAttackAction(…, *(int*)(tracked+0x120), …)`）。
- **落点为什么在 `_selectedSlot = slot` 那一行之后**：视图没建起来时那一支（`:15318-15324`）**提前 return**，
  先写 `_command` 会让「没有视图」那一档留下半个指挥态（与该处注释「留着它会让下一次点击被当成选目标」同一条理由）。
- ⚠️ `ShowJustOne` 那一支（`:15361-15366`）也吃这一句 —— 原版 `StartAttackFrom` 同样不问「显示了几颗钮」，
  起手就用 `card.attackType` 点亮潜在目标（`StartAttackFrom.c:42-46` → `ShowPotentialTargets`）⇒ 一致。
- 「点亮潜在目标 / 亮准星」这半**本来就有**（`Show()` → `RequestPreviewTargets` → `PreviewTargetsFromDrag`，`:15406`/`:15945`），
  **⛔ 我没有另加一份 `HighlightTargets()`**（两处写同一条规则 = 迟早不一致）。

### 补丁③（同一档不重复下单）—— `BattleDriver.cs:15646`（`CommitCommand` 第一句）
```csharp
// 旧
if (_replaySession)
// 新
if (kind == _command) return; else if (_replaySession)
```
- 判据：`UnitOnBoardAttackTypeSelector__AttackButtonClick.c:26-30` —— **第一句**就是 `if (*(int*)(card + 0x120) == param_2) return;`
  （在空判之后、`HasCurrentTrait(pindown)` 与全部教程闸之前）⇒ 放在 `CommitCommand` 最前**才是原版的次序**。
- 🔴 **落点【不】在 `AttackSelector.Selectable`（判据 §三·补丁③ 的写法），理由两条**：
  1. `Selectable` 同时是 **`KindAtPoint` 的受体判据**（`AttackSelector.cs:1663`）。把「已选那一格」判成不可选 ⇒
     它的钮 / 黄圈**不再算受体** ⇒ `ContainsBar` 判「在选择器外」⇒ **按下即 `ClearSelection()`，整条指令被取消**（比原缺陷更糟）。
  2. 白名单：本代理只能写 `BattleDriver.cs`。
- 依赖关系照判据：`_command` 由补丁②在起手时就写成 `card.attackType`，所以「同一档」不再需要玩家点一次也打得出去；
  ⛔ **单上 ③ 会把「上一手用近战 ⇒ 这一手再也选不了近战」锁死**（判据 §三 原文）。

---

## 二 · 行数 / 行尾核对

| 项 | 读数 |
|---|---|
| 本次 5 处改动 | **全部 1 行 → 1 行**（就地改写已有行，⛔ 一行未加、一行未删） |
| 改完立刻 `wc -l` | **40718**（= 改前 40718，**净 0**） |
| 现读 `wc -l` | **40779** —— ⚠️ 差额 **+61 不是我加的**：`B117-WBD` 正在同一文件**类尾追加**（`:40386 / :40426 / :40501 / :40577` 都是它的 `2026-11-03 B117-WBD` 标记）。详见 §五·1 |
| 行尾 | `LF 40779 · CRLF 40779 · 裸 CR 0`（`io.open(p,'rb')` 读；**整篇仍是纯 CRLF**，没被翻） |
| 账落点 | 我改的 5 行附近**没有 A 档落点**（最近的 = `BattleDriver.cs:15638` 的 `void CommitCommand(...)` 声明行，**未动**）；见 §三 |

---

## 三 · 三道闸读数

| 闸 | 命令 | 结果 |
|---|---|---|
| **秒级类型检查** | `TMPDIR=/tmp/wf_wsel bash Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0**（第一次跑时编辑器侧有 2 条 `WarpforgeArena1/Editor/ArenaBuilder.cs:4634` 的 `CS1002/CS1513` —— **别人的在飞活**，60 s 后复跑归零） |
| **聚合行闸** | `python -I -X utf8 Unity/工具/check_aggregate_lines.py Battle/BattleDriver.cs Editor/BattleScene.cs` | **PASS**：合计 文件 2 · 聚合行 1 · 调用 55 · 违规 0（`BattleScene.cs:46910` 那条聚合行 55 个调用全在 `//` 之前；`BattleDriver.cs` 无聚合行） |
| **账闸** | `python -I -X utf8 Unity/工具/check_ledger_points.py` | 全仓 **A 5831 · 通过 5548 · 漂移 261 · 失败 22** —— ⚠️ **不是我的**：输出里**没有任何一行落在 `Battle/BattleDriver.cs`**（`grep -c "Battle/BattleDriver.cs"` = **0**）。漂移集中在 `卡牌面_渲染.tsv`(64) / `战斗界面_HUD.tsv`(109) / `战斗逻辑驱动.tsv`(33) 等，是**别的写手当刻未提交的改动**（两次跑之间 `卡牌面_渲染` 从 58 → 64 还在变） |

> `WBD` 那句「0 漂移 / 0 失败」是**它当时的快照**；现读已经不是 0，但**一条都不在我这个文件上**。

---

## 四 · 需要调度台跑的宿主 · 以及**会被这三条改动打红的四处既有断言**

### 4.1 必须跑 `BattleScene.Run`（我无权跑批处理）
验的是：
1. **①+②**：拖起单位（`OpenCommand`）→ 选择器**保持开着** → 直接松开在敌人身上 ⇒ **打得出去**（`Command == card.attackType` 那一档）；
2. **①**：选完打法后 `SelectorOpen` 仍为 **真**，黄圈 / 1.3 倍补间真的补完（`ButtonScale > 1.2`）；
3. **③**：悬停「已选那一格」不再重发（`LastAttackType` 不被重写、不响 `changeTo*AttackCUE`）；
4. 回归面（判据 §三·② 点名的）：`IsTargeting` / `IsScreenPointOverCommandTarget` / `SetReticleTarget` 的 `_command != None` 闸 / `DamageEntriesFor` —— 这四处现在**起手就为真**。

### 4.2 🔴 **断言过期（它们钉的正是本次要改掉的旧行为）—— 我不能改 `Editor/*.cs`，请派给该文件写手**

| 位置 | 现文 | 应改成 | 为什么 |
|---|---|---|---|
| `Editor/BattleScene.cs:9417` | `Check(!driver.SelectorOpen, "选完打法，选择器收起");` | `Check(driver.SelectorOpen, "选完打法，选择器**仍开着**（原版活到 StopTracking）");` | 补丁① |
| `Editor/BattleScene.cs:10158` | `Check(!driver.SelectorOpen, "定下打法 → 选择器收起");` | 同上 | 补丁① |
| `Editor/BattleScene.cs:34520` | `Check(!wb4.SelectorOpen && wb4.Command == AttackKind.Ranged, …)` | 只把 `!wb4.SelectorOpen` 那半改成 `wb4.SelectorOpen` | 补丁①（`Command == Ranged` 那半仍然成立：探针 `Ballista` 的 `LastAttackType = 0`） |
| `Editor/BattleScene.cs:31429` | `if (driver.SelectorOpen) driver.SimulateCommand(AttackKind.Melee);   // 收起，别留给下一节` | `if (driver.SelectorOpen) driver.SimulateDeselect();` | 这行靠「commit 会关窗」收尾；现在 **commit 不关窗**（且补丁③还会让同档 commit 直接 return）⇒ 选择器会**留给下一节** |

> 另外 `BattleScene.cs:9547`（`点槽外 → 选择器收起`）**不受影响**（走 `ClearSelection`）；`9717`（轻点不弹选择器）也不受影响。

---

## 五 · 顺手发现（未改，交调度台分流）

1. 🔴 **`Battle/BattleDriver.cs` 现在有两个写手**：我的 5 处是**就地 1:1 改写**（净 0 行，改完实测 40718），
   而 `B117-WBD` 在同一文件**类尾追加**（`:40386 / :40426 / :40501 / :40577`），我这一轮期间文件从 40718 长到 40779。
   ⇒ 两条纪律建议：① 该类活**以后按「区间」分所有权**（我在 `:15004-15693`，它在 `:40386+`），别只写「这个文件归谁」；
   ② 本批合并时**先 `git diff` 看一眼有没有互相覆盖**（Edit 工具是整文件读改写，理论上可互相吞掉）。
2. **`HoverPickReady` 那一帧恰好也是松手帧**时，`BattleDriver.cs:15001` 的 `return;` 会把这一次松手吞掉 ⇒
   指令留在「已武装」态、选择器不关（原版这一档是 `CancelAttack` + `StopTracking`）。
   触发条件是「0.1 s 安全窗到期的同一帧松手」，概率极低 ⇒ **本次未改**，如实记账；要修就是把那一句 `return;` 改成落穿。
3. 判据 §三·补丁③ 的写法（落在 `AttackSelector.Selectable`）**会引入新缺陷**（见 §一·补丁③ 理由 1）——
   建议把判据那一行**就地订正**（铁律 5），否则下一个执行代理会照着做。
