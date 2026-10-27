# C3 · B14「活动 · RaidEventWindow」33 行 —— 施工报告（2026-10-27）

执行代理 `C3` · 批次 `B87-C3` · 白名单 = `CardPresentation/Shell/RaidEventWindow.cs` ＋ 新建 `CardPresentation/Shell/RaidEvent.cs`

---

## ① 逐条做了什么

### 1·1 批次口径核对（先把「33」这个数对上）

`判据/C档施工批次表.tsv` 的 B14 写「涉及账行数 33 · 日常_奖励(33)」。主表里 **Raid 族 + `EventProgressBarController`** 的 C 行一共 **43** 条：

| 原版类 | C 行数 | 本批做不做 |
|---|---:|---|
| `RaidEvent` | 13 | ✅ |
| `RaidRewardSelector` | 8 | ✅ |
| `RaidEventRewardItemContainer` | 5 | ✅ |
| `RaidProgressClaimButtonController` | 3 | ✅ |
| `RaidEventTab` | 2 | ✅ |
| `RaidHeaderController` | 1 | ✅ |
| `RaidProgressBarScaleAndPositionController` | 1 | ✅ |
| **小计** | **33** | = B14 的口径 ✔ |
| `EventProgressBarController` | 6 | ⛔ **不归本批** —— 它们是 **U04** 的（施工单账行 181–186），且 U04 已于今天落进 `Shell/EventProgressBarController.cs` |
| `RaidOrArmyReleaseMenuContainer` | 4 | ⛔ **不归本批** —— 那是**主菜单活动格**（`MainMenuRuntime/TopBar` 是跨域共享文件，施工单自己给的风险 = 「高」） |

⇒ 43 − 6 − 4 = **33**，与批次表逐字吻合。**⛔ 本批没有替 U04 写第二份进度条，也没有去动主菜单。**

### 1·2 新建 `Shell/RaidEvent.cs`（921 行）—— 数据层 + 纯算式

按「原版一个组件方法 = 本地一个纯函数」拆，**全部逐句对齐 `.c`**（判据逐条写在方法头，格式 = `文件:行号` + 逐句代码块）：

| 原版 | 本地 | 关键判据 |
|---|---|---|
| `RaidMilestone : ScoringMilestone` | `RaidMilestone`（`CostToClaim(+0x18)` / `DiscountedPrice(+0x20)` / `Reward(+0x28)` / `CollectiveScoreToUnlock(+0x30)`） | `RaidMilestone.cs` 字段表 + `RaidRewardSelector__CreateMilestoneInfo.c` 的偏移读数 |
| `RaidEventData : ScoringEventData` | `RaidEventData`（`leadboardName` 原文少个 r + `scoringMilestones`，实现抽象 `ScoringMilestones`） | `RaidEventData.cs` |
| `RaidEvent` 13 方法 | `RaidEvent : ScoringEvent<ScoringEventPersistence, RaidEventData>` | 见 1·3 |
| `RaidRewardSelector.RaidEventMilestoneInfo` | `RaidRewardSelectorLogic.RaidEventMilestoneInfo`（七格顺序照 ctor） | `RaidEventMilestoneInfo__.ctor.c` 逐字段 |
| `RaidEventRewardItemContainer.GetState` | `RaidRewardContainerLogic.GetState` | `__GetState.c` 逐支 |
| `RaidProgressClaimButtonController` 的 Raid 那半 | `RaidClaimButtonLogic`（六档表 + 两价格 + `Interactable` 两跳） | prefab MB `6944932261852371964` 逐格 |
| `RaidProgressBarScaleAndPositionController` | `RaidBarPlacementLogic`（`state>=2` 闸 + 四格偏移） | `__SetPositionAndScale.c` + MB `3159737194665565180` |
| `RaidHeaderController.UpdateText` | `RaidHeaderLogic.UpdateText`（两档 + `GetDateForEventStage` + 倒计时） | `__UpdateText.c` / `LiveOpsUtility__GetDateForEventStage.c` |
| `ScoreObject.Initialize` 的「算显隐/拼文本」 | `RaidScoreObjectLogic`（= U04 留的那一个**钩子体**） | `ScoreObject__Initialize.c:26-88` |

**一个数都不自己挑**：端帽上界 `DAT_1834b2bb8` 用 `工具/read_literal.py` 读出来 = **4B float 1.0f**（与 U04 那一份读数一致）。

### 1·3 `RaidEvent` 的 13 条

- `GetCurrentMilestoneIndex()` = `LastOrDefault(PlayerScore >= ScoreToUnlock)` → `GetIndexOf+1` → **夹到 `[0, Count-1]`**（`.c:11-50` 逐句）。
- `GetCurrentMilestone()` = `ElementAtOrDefault(idx)`；`GetScoringInfoFromCurrentMilestone()` = `(PlayerScore, prev?.scoreToUnlock ?? 0, **本档** scoreToUnlock)` —— 🔴 cpp2il 那份把第三参印成 `0`，以 `.c` 为准（已订正）。
- `Initialize()`：`base` → 注册奖励信号 → `MenuActions.BlockGroups["ScoringEvent"]` → 排一条 `BlockActionsFromSameGroup` 的刷新动作。
- `Dispose()`：`base` → 反注册 → `MenuActions.ReleaseAction`。
- `Expire()`：原版先让服务器领掉未领奖励再 `base.Expire` ⇒ 本地**出声跳过**领奖那一跳、`base` 照走（§四 P3 b 档）。
- `OnRewardCollected()`：`Where(i => i.ItemId == "C4")` → `LeaderboardData.LocalScore(榜名, i.Quantity, null)`（增量取 `Quantity` 标**还原**：cpp2il 在这一格丢了字段名）。
- `PurchaseMilestone()`：解锁判据 `PlayerScore >= 0 && GlobalScore >= 社区门槛` 照抄；服务器事务 → 本地等价（标「已领」+ 广播信号 + 回调）+ 出声。
- 两个 lambda（`b__15_0` / `b__11_0`）+ 转发桩（`<>n__0`）→ 等价实现（内联 / `RefreshScoreValues` / `base.Expire` 那一句）。

### 1·4 改 `Shell/RaidEventWindow.cs`（969 → 1503 行）

- **`CreateRewards` 视图侧真的实现了**：`RebuildRewards()` = 先 `ResetRewards()`（出声 + 逐件 Destroy + 清空）→ 按 `Milestones` 逐档建一张 → 逐档 `ApplyCardState`；里程碑为 0 ⇒ **0 张**。
  ⇒ 文件头那条「**一处有意偏离**：只建模板」**已按铁律 5 就地订正**（原文留着追溯），`WarnOnce` 里那三句旧口径同步改掉。
  ⇒ 母版 `Raid Reward Container` 保留且关着 = 原版 `rewardItemContainer` 那个 **prefab 引用**。
- **`ApplyCardState`** 按 `__Initialize.c` 的七跳顺序落：个人条 → 领奖钮 → 社区条 → 社区档号 → 奖励（缺口出声）→ 高亮关。
- **进度条 / 领奖钮直接用 U04 的共用件**：`EventProgressBarController`（`Root.SetActive` / `ScoreObject` / `SetBarFill` / 端帽）、
  `BaseEventProgressClaimButtonController<RaidRewardState>.Initialize`（`Equals(state)` 闸 + 逐 `StateOption` + force-interactable）。
  ⛔ **一行 `ApplyState` 都没重写**（施工单那条风险指的就是这个）。
- **页头两档**：`ApplyHeaderText()`；🔴 没有事件时**一个字段都不写**（原版整段被「事件不为空」包着）⇒ **今天画面零变化**。
- **`SetEventTab(ILiveOps)`** 落地（逐句 `LiveOps = …; raidEvent = … as RaidEvent;`）+ `SyncLiveOps()` 本地传 `null` 并出声。
- 自检/编辑器入口：`SetEventForTest` / `ResetRewards` / `RefreshTestEditorOnly` / `TestHighlighOn(i)` / `RewardCardCount` / `RewardCards` / `MilestoneInfos`。

### 1·5 账

补丁 `账/_parts/B87_C3_B14.tsv`：**33 行 · 11 列 · 表头逐字照主表 · `方法` 全带 `(...)` · `轮次` = `B87-C3`**，
状态 = **A × 32 · B × 1**。已跑 `工具/check_patch_conflicts.py` ⇒ **✅ 没有跨补丁重复键（6 份补丁）**。

---

## ② 跳过 / 没做全的（全部已进账的差异列或缺口，⛔ 没有静默）

| 项 | 为什么 | 在哪记着 |
|---|---|---|
| `SetPositionAndScale` 的**真位移** | 本壳子件是**绝对 px**，父框一动子件不跟随 ⇒ 动了反而更错；`ExpansionPassEventWindow.ShiftBar` 已收口同一结论。**闸 + 四格偏移值已搬齐** ⇒ 记 **B** | 补丁那一行 + `RaidBarPlacementLogic` 文件头 |
| 奖励物品图标（`ItemDrawer`） | 属 **B38**，本机没有物品图标运行时 ⇒ `RewardTransform` 空容器 + 出声一次 | 窗口 ⑥ + 报告 ④ |
| `UIBorderGlow.Play()` | 本壳没有 glow 运行时 ⇒ 只亮框 | `RaidRewardContainerLogic.ToggleHighlightEffect` |
| `RectMask2D.enabled = (Level == 0)` | 本壳裁切走 `ViewportClip`（建树即裁）⇒ `ToggleRectMask` 空操作 | 窗口 `ApplyCardState` + `EventProgressBarController`（U04） |
| `ChatPreviewMessage.Enable(false/true)` | 属**社交/聊天**线（`Shell/ChatPanel.cs`），**不在本批白名单** | 窗口 `CollectRewardClicked` |
| `PriceDisplay.useStrikeThrough`（划线原价） | 我们的 `Label` 没有划线渲染出口 ⇒ 画不出那条横线，**⛔ 没有拿颜色冒充** | 窗口 `BuildClaimButton` 注释 |
| `PriceDisplay.icon`（货币图标） | 与 `ItemDrawer` 同一类缺口 ⇒ 只印数字 | 窗口 `PriceText` |
| `MenuAction.MenuFilter` | 原版那段 `Type[]` 实际装的是字符串（cpp2il 丢了 `typeof`）；我们的 `MenuFilter` 是 `Type[]` ⇒ 不传 | `RaidEvent.Initialize` doc |
| 「阶段」实参的来源 | 原版读 `(事件 as ???)+0x24`，**类型身份被 cpp2il 丢了**，本机不能确证是哪一格 ⇒ 做成显式入参（默认 0），**⛔ 没猜** | `RaidHeaderLogic.UpdateText` doc |
| `RaidOrArmyReleaseMenuContainer` 4 行 | 主菜单活动格（跨域共享文件），**不归 B14** | 报告 ① · 建议另派 |
| `EventProgressBarController` 6 行 | U04 已落（今天同一时刻），本批**复用不重写** | `Shell/EventProgressBarController.cs` |

---

## ③ 验收读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_c3z bash d:/4/Unity/工具/typecheck.sh` | **我的两个文件 0 错**；`运行时错误数: 3`，**3 条全在别人正在写的文件上**（`ChatTab.cs:156/176`、`MainMenuRuntime.cs:3700`，全是 `SocialData.ChatMessage.Channel`）⇒ 按 CLAUDE.md 13·3「错误全部集中在不是你负责的文件上 ⇒ 那不是你的问题」。⚠️ 期间还多次看到 `ScoringEvent.cs` / `ExpansionPassEventWindow.cs` / `CardView.cs` 的**瞬时**错误（U04 与 B15 正在写），每次都**不含我的文件**。 |
| 账补丁格式 | 34 行（1 表头 + 33 数据）· 每行 **11 列** · 状态 `A/B` · `check_patch_conflicts` ✅ |
| 行尾 | `RaidEventWindow.cs` 原 **LF 969** → 现 **LF 1503 · CRLF 0 · 裸 CR 0**；`git diff --numstat` = **593/59**（不是整篇重写） |
| Unity 批处理 | **没跑**（纪律：子代理不跑 Unity）⇒ `ShellScene.Run` 那几条断言**未执行**，本报告不声称「自检绿」 |
| 指纹 / 主表 / git | 都没动（没重钉基线、没写主表、没 commit） |

**能直接用的自检入口**（本批新增，下一轮给 `Editor/ShellScene.cs` 加断言时照着写）：
`RaidEventWindow.SetEventForTest(RaidEvent.CreateForTest("RaidTest", m0, m1))` → 断 `RewardCardCount == 2`；
`RaidRewardContainerLogic.GetState(...)` 五档用合成 `ScoringDataInfo` 各造一次；
`RaidHeaderLogic.UpdateText(data, stage:0, now, …)` ⇒ `TimeTitleVisible == false`（灭自证：恒 true ⇒ 红）。

---

## ④ 顺手发现（⛔ 都没擅自改，交调度台分流）

1. 🔴 **`stateDefinitions` 的 2026-10-19 记录错了一格（已按铁律 5 改掉）**：本窗文件头写「state 3 TotallyUnlocked → ON `Lock` `Collect Button` `Button Price Display`」，
   而 prefab JSON 里 **state 3 的第 5 个 target（= `Old Price Display`）`isEnabled = 1`**。语义上也对得上（解锁到能买时，原价划线价与折后价一起显示）。
   已把窗口文件头 + `RaidClaimButtonLogic` 两处改成一致，并注明「六 PPtr → 节点名」的对照是**按语义反解**的。
2. 🔴 **`decomp_full` 里没有 `RaidEvent__*.c`**（68 个 Raid 文件里一条都没有）—— `RaidEvent` 的方法体只存在于
   `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/RaidEvent.cs`（有真体）。⚠️ 那份的 `ScoringDataInfo` 第三参被印成 `0`，
   与 `.c` 冲突，已按 `.c` 落地。⇒ 建议：把 `RaidEvent__*` 补进泛型/漏名单（同 CLAUDE.md 那条「按 RVA 非负筛名单会静默丢方法」）。
3. **U04 与本批是同一时刻落的**：`Shell/ScoringEvent.cs`、`EventProgressBarController.cs`、`BaseEventProgressClaimButtonController.cs`、
   `BaseEventProgressRewardDrawerController.cs` 四件在我开工后被创建 ⇒ 我**改成了复用**（`ScoringDataInfo` / `ScoringMilestone` /
   `ScoringEvent<TS,TD>` / `EventProgressBarController` / 领奖钮基类）。⚠️ 期间 `ScoringEvent.cs` 一度出现过
   `CS0101 ScoringDataInfo 重复定义`（瞬时，同一文件内），看起来是写入过程中的中间态 —— 若下一轮再见到，先看那份文件是不是被两个写手同时改过。
4. **`LeaderboardData` 缺 `total` 一格**：原版 `RaidEvent.GlobalScore` 读 `LeaderboardResult.total`（服务端预聚合的榜总分），
   我们的 `LocalLeaderboardResult` 只有 `Totals`（按阵营/队伍的表）⇒ 只能按榜名去那张表取、取不到 = 0 并出声。
   建议在 `Shell/LeaderboardData.cs` 补一个 `public long Total;`（那是别人线的文件，本批没动）。
5. **`RaidOrArmyReleaseMenuContainer`（4 行）落在主菜单**：它的 `RefreshProgressBar` 要读 `GetCurrentMilestoneIndex` + `GetScoringInfoFromCurrentMilestone`
   —— 这两件本批已经齐了，**接的时候只差 `MainMenuRuntime/TopBar` 那一侧**（跨域共享文件，施工单风险列写的是「高」）。
6. **`RaidRewardSelector.Reset()` 的那句 `LogWarning` 原文读不出来**（`DAT_1842add48` 是字符串对象，`read_literal` 只能读数值）
   ⇒ 我们出声但**没有编原文**，账里已注明。
7. **`GetBaseData` / `GetBaseSave` 两条的等价实现落在 U04 的基类上**（本批没有在 Raid 侧再写一遍）。
   如果 U04 之后改了 `ScoringEvent.GetBaseData` 的返回类型，这两行的落点要跟着重钉。
