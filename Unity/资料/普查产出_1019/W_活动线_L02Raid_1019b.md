# W · 活动线 L02 · Raid 团队活动页 —— 写手报告（2026-10-19）

> 本轮只做 **L02**（⛔ 未做 Expansion Pass / L03）。
> 验证口径：只跑了秒级类型检查（`TMPDIR=/tmp/wf_l02 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**）。
> ⚠️ 中途两次运行报错，**全部在别人正在写的文件**（先是 `Shell/MenuTutorialFlow.cs` 的 `IsInWindowContext` 未定义，
> 后是 `Shell/LeaderboardRow.cs` / `Shell/MatchLogRow.cs` 的 `Texture→Texture2D` + `MainMenuScene.cs` 引用了
> 不存在的 `RebuildBackgroundForTest`）—— 按简报**未改别人的文件**；等它们写完重跑已归零。
> **未跑** Unity 批处理、任何 `*.Run` 自检、player、真机点击；未动 git；未改任何正本。

---

## 一、结论（建了什么 / 原版判据）

### 1·1 建了什么

**新窗口 `RaidEventWindow`（承载窗外壳 = `Parent Event Window` 那一族 + 恰好一个 Raid 页）**：

| 区块 | 内容 |
| --- | --- |
| 承载窗外壳 | `Menu Dark Background`（点暗底关窗）· `Content Area/Background`（沿用 `ParentEventWindow` 的渐变常量）· `Tab Buttons` 左栏（底板四段渐变 + 阴影 + 母版页签[关] + 唯一运行期页签「Raid / 突袭」）· `Tabs` 容器 · `Shadow (1)`（出厂关）· 吸收层 |
| 页根 `Raid Progress Tab` | 根矩形 **x 163.7875..1920.2125 · y 0.1795..1079.8205**（与 `Gacha Tab` 页根**逐位相同**） |
| `Background Black` / `Background Image` | 纯黑满页底 + liveops 背景图节点（**无图不画**；原版取不到 sprite 时把 `Image` 组件 `set_enabled(false)`，GO 仍 active —— 我们同态） |
| `Rewards Scroll View`（= `RaidRewardSelector`） | `Viewport`（`ViewportClip`，`UIMask` alpha=0 不画）+ `Rewards Content` + **奖励容器模板 `Raid Reward Container`（关着）** |
| `Raid Reward Container`（模板） | `Collect Highlight Effect`[出厂关] · `RewardTransform`（`RaidRewardDrawerController` 空容器）· **`Community Progression`**（`Community Raid Progress Bar`：`Bar` 底/填充/端帽 + `Points` `--/--` + `Level` 里程碑座）· **`Personal Progression`**（`Personal Raid Progress Bar` 同上 + `Claim Button Container`：原价/领取钮/`Claimed Text`/折后价/`Level`+`Lock`） |
| `Header`（= `RaidHeaderController`） | `Title`（prefab 出厂原文 `Event Title`）· `Instructions Community`（词条）· `Army Icon`（无图）· `Help Icon`（ⓘ + tooltip）· `Xp Points Icon`[出厂关] + `TotalXp Points`（`--`）· `Time Remaining`（词条 `Ends in:`）→ `Time`（`--`）+ `Help Grace Period`（ⓘ + tooltip） |
| `Footer` | `Help Icon Player`（ⓘ + tooltip）· `Instructions Player`（词条） |
| `Debug Add points` | **出厂 INACT** ⇒ 建出来、关着（照建，不参与显示） |
| 开窗入口 | `WindowsManager.PrefabRefRaidEvent = "Raid Progress Tab"` + `WindowsManager.OpenRaidEvent()`（照 `OpenGachaEvent` 的 `OpenByRef` 形状） |

### 1·2 原版判据（逐条可复跑）

- **类桩**：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/{RaidEventTab,RaidHeaderController,RaidRewardSelector,RaidEventRewardItemContainer,RaidRewardDrawerController,RaidProgressClaimButtonController,EventProgressBarController,RaidProgressBarScaleAndPositionController,ProgressRewardLevelObject,RaidOrArmyReleaseMenuContainer}.cs`（字段表逐条读过）。
- **行为（反编译方法体）**：
  - `BaseParentEventWindow__SetupTabs.c` / `__CreateEventTab.c` —— Raid 是 `Parent Event Window` 里由 liveops 事件列表动态建出来的一个 tab（与 Gacha 同构，不是独立窗口）。
  - `RaidEventTab__OnOpen/__OnClose/__Initialize/__Reset/__SetEventTab.c` —— `OnOpen = OnOpen()` + `Initialize()`；`OnClose = OnClose()` + `Reset()`；`Reset` 里 `CustomDebug.LogWarning` + `Unload` 两批 liveops 资产。
  - `RaidEventTab__Initialize.c` 三句：`rewardSelector.scoringEvent = 事件` → `CreateRewards()`；`backgroundImage.sprite = LoadAsset(事件,1)` 后 `Behaviour.set_enabled(sprite != null)`；`headerController.Initialize(事件)`。
  - `RaidHeaderController__Initialize.c`：`title.text = GetLabel(事件)`；`armyIcon.sprite = LoadAsset(事件,5)`；`UpdateText()`；`StartCoroutine(UpdateTimer())`。
  - `RaidHeaderController__UpdateText.c`：阶段 0 ⇒ `timeTitle` 整件关；否则 `timeTitle` 印 `Ends in:`/`Starts in:`、`timeRemaining` 印 `GetTimeStringFromDate(日期)`、`gracePeriodTooltip.SetActive(日期 < 服务器时间)`。
  - `RaidHeaderController__Awake.c`：给 `eventHelpButton` 挂监听 —— **本 prefab 的 `eventHelpButton` 是空 PPtr（0）** ⇒ 那一跳在本页根本不执行。
  - `RaidRewardSelector__CreateRewards.c`：按 `RaidEvent.Milestones` 逐件 `Instantiate(rewardItemContainer, contentAnchor)`；**里程碑为空 ⇒ 一件都不建**（`Count >= 1` 时先全 Destroy 再重建）。
  - `RaidEventRewardItemContainer__GetState.c`：`collected ⇒ 4`；否则按 `个人分 >= 个人目标` / `社区分 >= 社区目标` 组合出 `0/1/2/3`。
  - `RaidProgressClaimButtonController.stateDefinitions`（MB `6944932261852371964`，6 个 target PPtr 逐个解出节点名）：
    `0 Locked`→`Level`+`Lock`+`Button Price Display` / `1 PlayerUnlocked`→`Level`(+highlight)+`Lock` /
    `2 CommunityUnlocked`→`Lock`+`Collect Button`+`Button Price Display` / `3 TotallyUnlocked` 同 2 /
    `4 Collected`→`Lock`+`Collect Button`+`Claimed Text`+`Old Price Display`。
- **prefab 节点树**：`python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Raid Progress Tab" --depth 14 --relative`
  （落地副本 `d:/4/_tmp_view/raid_tab_dump.txt`，53 行）；同族五个 prefab 的 dump → `d:/4/_tmp_view/raid_related_dump.txt`（157 行）。
  另用自写临时脚本 `d:/4/_tmp_view/raid_rt.py` / `raid_walk.py` 逐节点读了**原始五元组**与 MB 字段（`m_PixelsPerUnitMultiplier` / `m_Color` / `m_Term` / `tooltipAnchor` / `stateDefinitions`）。
  根 `RectTransform` 实读 `RectTransform_-3447017820810090354.json`；根 MB `MonoBehaviour_-3329924186120558450.json`（`rewardSelector` / `backgroundImage` / `headerController` 三个 PPtr）。
- **本地化键**（`官方本地化_en+zh-CN.json` 的 `terms`；运行时走 `Resources/official_loc_13lang.json`，**逐条命中**）：
  `Event_Description/General/Raid`（Raid / 突袭 · 页签兜底）· `Event_Description/Raid/CollectiveScoringHelp` ·
  `Event_Description/Raid/PlayerScoringHelp` · `Event_Description/General/EndsIn` · `Event_Description/General/TimeEndsIn` ·
  `Event_Description/General/TimeStartsIn` · `Event_Description/Raid/CollectiveTooltip` ·
  `Event_Description/Raid/IndividualTooltip` · `Event_Description/General/GracePeriodDescription` · `Rewards Menu/Claimed`。
- **承载窗外壳**：直接复用 `Shell/ParentEventWindow.cs` 的 **public 常量**（矩形 / 渐变 / 页签几何 / 颜色 / art 名），⛔ 没有抄第二套判据。

### 1·3 有意偏离（照实记录，⛔ 不冒充原版）

1. **独立窗口 vs tab 页**：原版 Raid 是 `BaseParentEventWindow` 里由 liveops 事件列表动态建的一个 tab；本地**没有 liveops 事件列表** ⇒ 建「承载窗外壳 + 恰好一个 Raid 页」的等价形态。根名沿用原版**页** prefab 的 `Raid Progress Tab`。
2. **空数据显示**：原版 `RaidRewardSelector.CreateRewards` 在里程碑为 0 时**一件奖励容器都不建**。本任务验收口径 = 「页面能打开 + 空态出声」⇒ 我们**只建一份模板 `Raid Reward Container` 并关着**，数值 `--`/`--/--`，并在 `Open()` 里 `Debug.LogWarning` 逐条出声。⛔ 没有拿 prefab 里的示例值（`300/400` / `99` / `154748` / `19d 15h`）当真实数据。
3. **页签文字**：原版 = `LiveOpsAssetUtility.GetLabel(事件)`（远端）；本件用 `Event_Description/General/Raid`（官方表里的通用「Raid / 突袭」）兜底 —— **这是我们挑的兜底**，已在 `WarnOnce` 与本注释里写明。页签**图标**留空（liveops sprite 本地没有），⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。
4. **`Title`**：原版运行期被 `GetLabel(事件)` 覆盖；本件照 **prefab 出厂原文 `Event Title`** 画（同 `ParentEventWindow.P2HeaderTitle = "Game mode"` 那条先例），并出声。
5. **`Time Remaining` 的框**：原版该节点 **rect 宽 = 0**（`ContentSizeFitter` 横向 PreferredSize、`pivot.x = 1`，运行期从右缘 1638.425 向左撑）。本壳没有 CSF 运行时 ⇒ 用 **300 宽的右对齐框**（右缘逐位钉在 1638.425）。**这是我们挑的框**。
6. **`Rewards Scroll View` 无滚动运行时**：只建结构（视口裁切 + 内容轨）。原版 `ScrollRect`（`m_Horizontal=1` · `m_Vertical=0` · `m_MovementType=1`）在数据接上后需要另接。
7. **`ToBackground` 多关一次物体**：本壳是扁平 quad + 显式队列；本窗 3660–3689 而 `WhereToGetCardsPopup`（3479–3487）更低 ⇒ 只写 `state` 会被本窗盖住。处置照 `GachaEventWindow.ToBackground`（**我们挑的**，不是原版 base 行为）。
8. **`Collect Button` 的 `m_PixelsPerUnitMultiplier = 0.01`**（Unity 对 0 的钳位值）：按 `m_Border ÷ ppuMul` 算出 23400px 角块，但这颗钮只有 221.8×82，`CreateNineSlice` 照 `Image.GetAdjustedBorders` 逐轴夹 ⇒ 与传 `m_Border` 原值**逐位相同**。本轮直接传 `m_Border`（代码里写了推导）。

---

## 二、改动清单（文件:行号）

| 文件 | 行号 | 改动 |
| --- | --- | --- |
| `Unity/MyGame/Assets/CardPresentation/Shell/RaidEventWindow.cs` | **新建 · 1–933** | 整个 Raid 活动页（文件头判据 1–125；队列档 128–150；根/节点名 152–159；窗参 161–169；页几何 171–214；奖励容器几何 216–270；图/色 272–292；本地化键 294–309；状态/自检读口 311–373；`Create` 377–393；`Open`/`ToBackground`/`WarnOnce` 395–460；`BuildShell` 462；`BuildShellTail` 483；`BuildTabBar` 496；`BuildTabButton` 534；`BuildPage` 599；`BuildPageBackground` 613；`BuildRewardsScroll` 629；`BuildHeader` 649；`BuildFooter` 719；`BuildDebugAddPoints` 737；`BuildHelpTip` 755；`BuildRewardContainer` 780；`BuildCommunityProgression` 799；`BuildPersonalProgression` 830；`BuildPoints` 852；`BuildClaimButton` 868；`Note`/`Tex` 913–931） |
| `Unity/MyGame/Assets/CardPresentation/Shell/RaidEventWindow.cs.meta` | 新建 | `guid: 402ed7d51fe4427fafc85d098541a962`（类名 == 文件名，避免 `!u!115` 存根） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:725-734` | 新增 `PrefabRefRaidEvent = "Raid Progress Tab"`（含「原版没有独立窗口」的说明） |
| `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs` | `:971-998` | 新增 `OpenRaidEvent()`（`OpenByRef` + `RaidEventWindow.Create`） |

⛔ 未碰：`ParentEventWindow.cs` / `LiveOpsEventWindow.cs` / `GachaEventWindow.cs` / `MainMenuRuntime.cs` /
`MenuTutorial*.cs` / `TutorialPointerUi.cs` / `Battle/**` / `RuleEngine/**` / `WarpforgeVFX/**` /
`Core/CardView.cs` / `Scenes/*.unity` / 正本 / `d:/2`（只读）/ git。
（`WindowsManager.cs` 的 48 行新增里，19 行是 **L01 那位写手**已经加的 Gacha 段，本轮**没动**它。）

---

## 三、没查清（远端数据缺什么）

以下全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里**没有**（逐条已确认，⛔ 未猜）：

1. `LiveOpsAssetUtility.GetLabel(事件)` —— 页签文字 + 页内 `Title`（两处同一来源）。
2. `LiveOpsAssetUtility.LoadAsset(事件, 1)` —— `Background Image`（整页背景图，`Tiled`）。
3. `LiveOpsAssetUtility.LoadAsset(事件, 5)` —— `Army Icon`（阵营图）。
4. `LiveOpsAssetUtility.LoadAsset(事件, 0)` —— 页签图标（`BaseParentEventWindow.CreateEventTab` 的实参）。
5. `RaidEvent.Milestones` / `GlobalScore` / `PlayerScore` —— 里程碑数量、社区总分、个人分。
6. `RaidMilestone.CollectiveScoreToUnlock` / `CostToClaim` / `DiscountedPrice` / `Rewards`（`RewardInfo` 列表，喂 `RaidRewardDrawerController`）。
7. `RaidRewardSelector.CreateMilestoneInfo` 需要的 `ScoringDataInfo`（`current` / `max`，进度条百分比 `PercentageToNextLevel` 的两端）。
8. 事件起止时刻 —— `RaidHeaderController.UpdateText` 的 `LiveOpsUtility.GetDateForEventStage` + `PlayerDataManager.currentServerTime`（宽限期判定也靠它）。
9. 每档的**货币图标**（`PriceDisplay.icon` / `EventProgressBarController.ScoreObject.iconHolder` 的 `ObtainableItem`）。
10. `RaidHeaderController.UpdateTimer` 协程的刷新间隔（反编译里是 `wait1Second` 字段，值读不到 —— 本件不跑协程，`TimerDisplay.Attach` 走我们自己的刷新）。
11. `Xp Points Icon` 那一块的总 XP 数值（`TotalXp Points`，出厂 `154748` 是示例）。

---

## 四、顺手发现（交调度台分流）

1. 🔴 **`RaidProgressBarScaleAndPositionController` 说明「两条进度条的位置/缩放是运行期按状态改的」**
   （字段 `positionOffsetCurrent/Previous` · `scaleOffsetCurrent/Previous`）—— 而 `Raid Reward Container` 的
   **`Community Raid Progress Bar` 出厂就挂在容器边界外**（x −70.1..239.8，容器只有 0..505.9）。
   ⇒ 数据接上后**不能直接信 prefab 出厂位置**，要跑那条 controller（铁律 5·c「一个值 ≠ 全部情况」）。
   本轮模板是关着的，所以不影响画面；**接数据时必须回头补**。
2. ⚠️ **`Raid Progress Bar` / `Personal Raid Progress Bar` / `Community Raid Progress Bar` 三个 prefab 结构几乎一样，
   只有 `Points` 的 tint 与 `icon` 组件不同**：
   `Community`/`Raid` 两版的 `icon` 是 `Image` **组件被禁用**（`m_Enabled=0`）；`Personal` 版**根本没有 `Image` 组件**（只有 `AspectRatioFitter` + `LayoutElement`）。
   ⛔ 别以为它们是同一个 prefab 的三份拷贝。
3. ⚠️ **`40K_ArmyTrack_bar` 的 tint 三处不同**：`Community` = (1, 0.765, 0.505) 金 / `Personal` = 纯白 / 填充 `Community` = (1, 0.395, 0.118) 橙 /
   `Personal` = (0.613, 0.273, 0) 暗橙。**逐条照抄，别统一**。
4. ⚠️ **`Raid Header` 的 `Help Icon`（ⓘ）三颗 `tooltipAnchor` 各不相同**：20（集体）/ 25（宽限期）/ 30（个人），
   宽限期那颗还有 `offset.x = -10`。三颗 `m_RaycastPadding` 都是 `(-20,-20,-20,-20)` ⇒ 命中区每边外扩 20。
5. ⚠️ **官方表里 `Event_Description/General/StartsIn` 不存在**，真正的两条是
   `TimeEndsIn`（`Ends in:`）/ `TimeStartsIn`（`Starts in:`）—— 原版 `UpdateText` 就是在这两条之间切。
   本件按 prefab 出厂只印 `EndsIn`，运行期切换要等事件数据。
6. ⚠️ **`Raid Progress Tab` 页根与 `Gacha Tab` 页根 RectTransform 逐字段相同**（同 anchor / pos(82,0) / sizeDelta(−163.575,−0.359)）
   ⇒ 两个活动页在同一个 `Tabs` 容器里**完全重叠**，靠 `SetActive` 切页（已按此建）。
7. ⚠️ **`RaidHeaderController.eventHelpButton` 是空 PPtr** —— 反编译里 `Awake` 那一跳会早退。
   所以「活动帮助」按钮**不在这页上**（别照 Gacha 的 `Where get cards` 那样补一颗）。
8. ⚠️ **`Raid Reward Container` 的 `RewardTransform` 上 `RaidRewardDrawerController` 没有任何序列化字段**
   （`ItemDrawer` 在基类里是 protected 非 `[SerializeField]`，运行期自动找）—— 本件只建空节点。
