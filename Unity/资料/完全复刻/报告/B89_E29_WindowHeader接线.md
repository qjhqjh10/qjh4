# B89-E29 · `WindowHeaderWithBackButton` 接线（原版 8 个 prefab 实例）

> 轮次 `B89-E29` · 2026-10-28
> 前置：`Shell/WindowHeaderWithBackButton.cs` 由前序代理按
> `decomp_full/WindowHeaderWithBackButton__Initialize.c`（49 行五步）建好，但**零接线**。
> 本批把它接到宿主窗上。

## 一句话结论

**原版 8 个 prefab 实例中，本批接上 6 处；剩 2 处接不上**（原因见 §三）。
全仓 `typecheck` **0/0**（`TMPDIR=/tmp/wf_B89E29 bash 工具/typecheck.sh`）。

## 一、实例清单（现读，⛔ 不是抄旧文档）

用 `grep -rln "6d74726afb8bab28201edc2a0ace1fa8"`（组件 `.cs.meta` 的 guid）
扫 `d:/2/tools/assetripper_out/ExportedProject` 的 `*.prefab` / `*.unity`：

| # | 原版 prefab | 原版类（decomp 调用方） | 我们的宿主 | 本批 |
|---|---|---|---|---|
| 1 | `RankedEventWindowV2.prefab` | `RankedEventWindowV2` | `Shell/RankedEventWindow.cs` | ✅ 接上 |
| 2 | `SkirmishModeEventWindow.prefab` | `SkirmishEventWindow` | `Shell/SkirmishEventWindow.cs` | ✅ 接上 |
| 3 | `EnergySinglePlayerOnlyEventWindow.prefab` | `SinglePlayerOnlyEnergyEventWindow` | `Shell/EnergySinglePlayerOnlyEventWindow.cs` | ✅ 接上 |
| 4 | `Daily Streak Popup.prefab` | `DailyStreakWindow` | `Shell/DailyStreakPopup.cs` | ✅ 接上 |
| 5 | `Draft Mode Menu Demo.prefab` | `DraftModeMenuDemo` | `Shell/DraftModeWindow.cs` | ✅ 接上 |
| 6 | `Two Sides Event Window.prefab` | `TwoSidesEvent_OnGoingTab` | `Shell/ParentEventWindow.cs` | ✅ 接上 |
| 7 | `Tutorial Mode Menu.prefab` | `TutorialModePopup` | `Shell/TutorialModePopup.cs` | ⛔ 白名单外 |
| 8 | `Draft Mode Timed Mode Window.prefab` | （限时版，调用方未单列） | 无独立窗类（`DraftModeWindow` 是**非限时版** `DraftModeMenuDemo`） | ⛔ 无宿主 |

> ⚠️ **订正（铁律 5）**：`WindowHeaderWithBackButton.cs` 头部原写「原版有 **7** 处」，
> 现读 = **8 个 prefab**（多出 `Tutorial Mode Menu`）。已在文件头就地改掉。

原版每处都是 `header.Initialize(liveopEvent, onBack)`（`ILiveOps` 重载）：
`RankedEventWindowV2__Open.c:85` · `SkirmishEventWindow__Open.c:28` ·
`SinglePlayerOnlyEnergyEventWindow__Open.c` · `DailyStreakWindow__Open.c` ·
`DraftModeMenuDemo__Open.c` · `TwoSidesEvent_OnGoingTab__OnOpen.c` · `TutorialModePopup__Open.c`。

## 二、逐处：判据 / 我们接在哪 / 观测口 / 是否破坏原返回逻辑

> **统一说明（6 处都一样）**：
> ① **标题**：原版 `Initialize(ILiveOps, onBack)` 内部走
> `LiveOpsAssetUtility.GetLabel(baseData, Title(0), [])`（`Initialize.c` 第 ③ 步）。
> 我们的六扇窗**都没有 `ILiveOps` 实例**（本地等价物是 `LiveOpsLocalEvent` DTO，不是 `ILiveOps`）
> ⇒ 走 `Initialize(string, Action)`，标题用**同一条原版词条键**已解析好的字。
> `TitleTermResolver` 仍然接上（见 §四），给将来真有事件的窗用。
> ② **返回回调**：原版 = `BackButtonPressed()` → `OnBackButtonPressed`（`Initialize.c` 第 ④ 步）。
> 我们 = `WireHeader` **先清掉** `WindowButton.onClick`（`WindowHeader.WithBackButton` 建时已把 `Spec.OnBack`
> 挂上去），再交给组件驱动 ⇒ **点一下只走一条路**（不清则同一次 `Close()` 会走两遍；
> `Daily Streak` 那种「先 `StreakAutoCollect` 再 `Close`」的链会**收两次**）。
> ③ **键位**：`Bind` 里 `onClick += BackButtonPressed`，所以 `WindowButton.Click()`（自检直调）依旧能关窗。
> ④ **重建**：组件挂在**窗头节点**上，`GameWindow.Build()` 每次都重建子件 ⇒ 每次都是新节点新组件，
> 不会叠；`Bind` 内含 `Unhook`，同一节点重复 `Bind` 也幂等。

### 1. `RankedEventWindowV2` → `Shell/RankedEventWindow.cs`
- **标题词条**：`MainMenu/GameMode/Classic`（本窗 `DeckGameMode` 恒经典）—— 现读
  `Shell/LiveOpsEventWindow.cs:1277`（基类 `BuildHeader` 的 `TitleText`）。
- **我们接在**：`Shell/RankedEventWindow.cs:425` `BuildExtras` → `WireHeaderComponent(root)`（helper 在**类尾** `:434`：`WindowHeaderWithBackButton.WireByPath(root, WindowHeader.RootNameGameMode, () => Close(), Loc.T("MainMenu/GameMode/Classic"))`）。
  （表头建在**基类** `LiveOpsEventWindow.Build()` 的私有 `BuildHeader` 里 ⇒ 子类只能在 `BuildExtras`
  —— 同一 `Build()` 里排在 `BuildHeader` 之后 —— 按路径补线。）
- **观测口**：从主菜单进「排位」模式卡 → 窗开 ⇒ 表头标题应为 **「经典」**；点返回钮 ⇒ 关窗、回主菜单。
- **破坏检查**：基类原 `OnBack = () => Close()` 被清掉后由组件回调同一 `Close()` ⇒ 行为不变；
  `MainMenuScene` 里排位窗的返回钮断言（若有）走的仍是 `WindowButton`，仍可点。

### 2. `SkirmishEventWindow` → `Shell/SkirmishEventWindow.cs`
- **标题词条**：`MainMenu/GameMode/Skirmish`（现读 `LiveOpsEventWindow.cs:1277`）。
- **我们接在**：`Shell/SkirmishEventWindow.cs:627` `BuildExtras` → `WireHeaderComponent(root)`（helper 在**类尾** `:913`）。
- **观测口**：进「遭遇/小冲突」模式卡 ⇒ 标题应为 **「小冲突」**；返回钮 ⇒ 关窗。

### 3. `SinglePlayerOnlyEnergyEventWindow` → `Shell/EnergySinglePlayerOnlyEventWindow.cs`
- **标题**：本窗 `Spec.TitleText = "Game Mode"`（`Shell/EnergySinglePlayerOnlyEventWindow.cs:711`，字面量，
  ⛔ 不是词条键 —— 如实记；原版是从事件 `Title` 标签取）。
- **我们接在**：`Shell/EnergySinglePlayerOnlyEventWindow.cs:741` `BuildHeader` 末尾 → `WireHeaderComponent(p, onBack)`（helper 在**类尾** `:1307`：`WireHeader(p.Root, p.BackHit 上的 WindowButton, p.Title, onBack, "Game Mode")`）。
  返回钮档 = `QuadInOwnNode` ⇒ `WindowButton` 在 `BackHit` 命中区节点上。
- **观测口**：开能源活动窗 ⇒ 标题 **「Game Mode」**；返回钮 ⇒ `Close()`；
  另**压暗层**那条出口（`BackgroundHit` → `Close`）**未动**。
- **现存自检**：`Editor/MainMenuScene.cs:16560-16568` 直调 `bw.Click()` 断「点返回钮 ⇒ 窗关」——
  接线后 `onClick = BackButtonPressed`（非空）⇒ **这条仍绿**。

### 4. `DailyStreakWindow` → `Shell/DailyStreakPopup.cs`
- **标题**：`DailyData.StreakWindowTitle()` = **字面量 `"Daily Streak"`**（`Shell/DailyData.cs:1063`，
  ⚠️ 它**不是**本地化词条 —— 这是本窗既有的口径，本批未改，如实登记）。
- **我们接在**：`Shell/DailyStreakPopup.cs:722` `BuildHeader` 末尾 → `WireHeaderComponent(p, onBack)`（helper 在**类尾** `:869`：`WireHeader(p.Root, p.BackQuad 上的 WindowButton, p.Title, onBack, DailyData.StreakWindowTitle())`）。
  返回钮档 = `BoundOnQuad` ⇒ `WindowButton` 挂在图自己身上（`p.BackQuad`），**没有 `BackHit`**。
- **观测口**：日常 → 连登弹窗 ⇒ 标题 **「Daily Streak」**；点返回钮 ⇒ **先 `StreakAutoCollect()` 再 `Close()`**（原版 `Close()` = `TryCollect(() => base.Close())`）。
- **破坏检查**：回调仍是原来那个 `onBack` 闭包（收 + 关），只是驱动口从 `onClick` 直挂改成组件转发 ⇒ **只收一次**。

### 5. `DraftModeMenuDemo` → `Shell/DraftModeWindow.cs`
- **标题词条**：`Demo/DraftMode/Title`（现读 `Shell/DraftModeWindow.cs:1222`）。
- **我们接在**：`Shell/DraftModeWindow.cs:1237` `BuildHeader` 末尾 → `WireHeaderComponent(root, onBack)`（helper 在**类尾** `:4157` → `WireByPath(...)`）。返回钮档 = `QuadOnHeader`。
- **观测口**：进选秀/轮抽窗 ⇒ 标题 **「选秀模式 / Draft Mode」**；返回钮 ⇒ 关窗。

### 6. `TwoSidesEvent_OnGoingTab` → `Shell/ParentEventWindow.cs`
- **标题**：`P2HeaderTitle` = **prefab 出厂 `"Game mode"`**（`Shell/ParentEventWindow.cs:449`）。
  原版运行期由 liveop 的 `Title` 标签覆盖；**本地没有该 `ILiveOps` 实例** ⇒ 仍显示出厂字（**这半仍是缺口**，见下）。
- **我们接在**：`Shell/ParentEventWindow.cs:1239` `BuildOnGoingPage` → `WireHeaderComponent(header, backHit, onBack)`（helper 在**类尾** `:1561` → `WireHeader(header, backHit 上的 WindowButton, _p2HeaderTitle, onBack, P2HeaderTitle)`）。
  本窗是**内联建**表头（没走 `WindowHeader.WithBackButton`）⇒ 返回钮的 `WindowButton` 在 `Back Header Button/Hit` 上。
- **观测口**：活动 → `Two Sides Event Window` 的「进行中」页 ⇒ 标题 **「Game mode」**；返回钮 ⇒ `Close()`。
- **⚠️ 未解决**：标题**数据驱动**仍缺（原版从事件 `Title` 标签取）—— 本窗有 `LocalLiveOpsEventId` /
  页签 `EventId`，但没有 `ILiveOps` 实例，且该事件的 `Title` 标签是否在本地目录里未逐条核。
  ⇒ 账里本行**保持 B（缺数据驱动）**，不因本批接线升 A。

## 三、接不上的 2 处（写清原因）

1. **`Tutorial Mode Menu` → `Shell/TutorialModePopup.cs`**：该文件**不在本代理的文件白名单**
   （`Shell/**` 其它归 E16c/E22/E25）。它的 `BuildHeader`（`Shell/TutorialModePopup.cs:411`）
   同样走 `WindowHeader.WithBackButton` 且注释已自陈「`TutorialModePopup.header` 指的就是它」
   ⇒ **接线形状与本批 6 处完全一致**，建议派给有该文件写权的代理，3 行即可：
   `var p = WindowHeader.WithBackButton(...)` → `var wb = p.BackHit?.GetComponent<WindowButton>()`
   → `WindowHeaderWithBackButton.WireHeader(p.Root, wb, p.Title, onBack, 原标题)`。
2. **`Draft Mode Timed Mode Window`**：我们**没有对应的独立窗类**（`Shell/DraftModeWindow.cs` 文件头
   明写「**不是**限时版 `Draft Mode Timed Mode Window`」）⇒ **无宿主可接**。
   ⚠️ 这不是「判断难」而是「我们没建那扇窗」——按铁律 11 属「复刻缺漏」，已记入报告，
   建议立账到「轮抽线」（先建限时版窗，再接线）。

## 四、`TitleTermResolver` 接法（原口径：组件不直依赖 `LiveOpsLocalCatalog`）

- 组件侧只留静态口：`WindowHeaderWithBackButton.InstallTitleTermResolver(Func<ILiveOps,string>)`
  （`Shell/WindowHeaderWithBackButton.cs:245`）。
- 通用解析器放在 **`Shell/ParentEventWindow.cs` 类尾**（本批追加）：
  `ParentEventWindow.ResolveHeaderTitleTerm(ILiveOps)` = `LiveOpsLocalCatalog.Find(ev.GetID())`
  → `LiveOpsAssetUtility.GetEventLabel(le, LiveOpsLabelRef.Title)` → `.Term`。
  放这扇窗的理由：**它本来就依赖本地活动目录**（`TabLabelTerm` / `BuildEventTabs`），
  而组件按口径**不直依赖**那个文件（前序代理曾因取器搬迁断过编译）。
- 六处接线前各调一次 `ParentEventWindow.EnsureHeaderTitleResolver()`（幂等，装一次全局生效）。

## 五、改动清单

| 文件 | 改动 |
|---|---|
| `Shell/WindowHeaderWithBackButton.cs` | 新增 `WireHeader` / `WireByPath` / `InstallTitleTermResolver`；文件头「7 处」→「8 个 prefab」 |
| `Shell/ParentEventWindow.cs` | 类尾追加 `ResolveHeaderTitleTerm` / `EnsureHeaderTitleResolver` / `WireHeaderComponent`；`BuildOnGoingPage` 内 1 行调用 |
| `Shell/DailyStreakPopup.cs` | `BuildHeader` 接线（`BoundOnQuad`）；helper `WireHeaderComponent(WindowHeader.Parts, Action)` 在类尾 |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | `BuildHeader` 接线（`QuadInOwnNode`）；helper 在类尾 |
| `Shell/DraftModeWindow.cs` | `BuildHeader` 接线（`QuadOnHeader`）；helper 在类尾（`WireByPath`） |
| `Shell/RankedEventWindow.cs` | `BuildExtras` 里 1 行 `WireHeaderComponent(root)`；helper 在类尾（`WireByPath`） |
| `Shell/SkirmishEventWindow.cs` | `BuildExtras` 里 1 行 `WireHeaderComponent(root)`；helper 在类尾（`WireByPath`） |

## 六、验证

- `TMPDIR=/tmp/wf_B89E29 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- **未跑 Unity 自检**（口径：本代理不跑 Unity，由调度台在同步点统一跑）。
  受影响的宿主（若要跑）：`MainMenuScene.Run`（能源窗返回钮 `bw.Click()` 断言）·
  `ShellScene.Run` / `RewardsScene.Run`（连登窗）· `DeckScene`+`DraftAssertD3`（轮抽窗）。
- 逐行 `git diff --numstat` 未出现整篇重写（行尾未翻：7 个文件均 `bareCR = 0`）。

### 六·b 🔴 账点行号漂移（同步点必做 `check_ledger_points --fix`）

接线代码必须落在 `BuildHeader` / `BuildExtras` / `BuildOnGoingPage` 的方法体里（新成员已按纪律追加到**类尾**，
但那 1–2 行调用避不开）⇒ 插入点**之后**的既有账点会整体下移。逐文件的净位移（= 插入点之后的所有账点）：

| 文件 | 插入点 | 之后账点净位移 |
|---|---|---|
| `Shell/DailyStreakPopup.cs` | `:680`(+1) / `:721`(+2) | **+3** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | `:705`(+1) / `:740`(+2) | **+3** |
| `Shell/DraftModeWindow.cs` | `:1213`(+1) / `:1236`(+2) | **+3**（⚠️ 本文件被引用点最多） |
| `Shell/SkirmishEventWindow.cs` | `:626`(+2) | **+2** |
| `Shell/ParentEventWindow.cs` | `:1236`(+3) | **+3** |
| `Shell/RankedEventWindow.cs` | 新增全在文件尾（`:424` 起） | **0**（既有账点 `:419` 在前） |

⇒ **本批之后跑一次 `python -I -X utf8 工具/check_ledger_points.py --all-A --fix`**（只在「无 `.cs` 写手 + 补丁已合并」时跑，
CLAUDE.md 纪律）。另：`Shell/ParentEventWindow.cs` 的 `BuildOnGoingPage` 声明在 **`:1190`**（主表原写 `:1189` 落在分节注释上，
本批补丁已就地钉到 `:1190`）。

## 七、顺手发现（供调度台分流）

1. `WindowHeaderWithBackButton.cs` 在我工作期间**被另一个代理并发修改**过（文件头被补成
   「🔶 自己设计」措辞版）——两边的改动共存、编译 0/0。⚠️ **违反「一个文件一个写手」**，
   建议调度台核一下是谁（疑似自设计打标代理）。
2. `DailyData.StreakWindowTitle()` 返回的是**英文字面量**，不是词条键
   （原版这一格来自事件 `Title` 标签）—— 连登窗标题在中文档会是英文。属既有缺口，本批未改。
3. `WindowHeaderWithBackButton.cs` **没有 `.meta`**（前序代理新建后未提交）——
   Unity 首次导入会现生成 guid，若已有场景按新 guid 引用需注意。
