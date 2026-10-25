// RaidEventWindow.cs — 活动线 `L02`：**Raid 团队活动页**（原版 `RaidEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab`）
//
// ============================ 出处（唯一判据，全部本地现读 · 2026-10-19）============================
// ① **类桩**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）：
//      `RaidEventTab.cs`（`rewardSelector` / `backgroundImage` / `headerController` / `currentEvent`）
//      `RaidHeaderController.cs`（字段声明序 = `title` · `armyIcon` · `timeRemaining` · `timeTitle` ·
//                                 `gracePeriodTooltip` · `eventHelpButton`）
//      `RaidRewardSelector.cs`（`rewardItemContainer` / `contentAnchor` / `rewardClaimedSound`）
//      `RaidEventRewardItemContainer.cs`（`State` 5 档 · `GetState` · 各子件 PPtr）
//      `RaidRewardDrawerController` / `RaidProgressClaimButtonController` /
//      `RaidProgressBarScaleAndPositionController` / `EventProgressBarController` /
//      `ProgressRewardLevelObject` / `RaidOrArmyReleaseMenuContainer`
// ② **行为**（`d:/2/tools/decomp_full/`，方法体完整）：
//      · `BaseParentEventWindow__SetupTabs.c` → 遍历 liveops 事件：`HasComponent(0xd)` ⇒ `TabButtons.AddTabButton(label, icon)`；
//        `HasComponent(0xb)` ⇒ `BaseParentEventWindow__CreateEventTab.c` → `GameWindowWithTabs.CreateTab(...)`
//        ⇒ **Raid 和 Gacha 一样，是 `Parent Event Window` 里的一个 tab 页**（不是独立窗口）。
//      · `RaidEventTab__OnOpen.c` = `WindowTabBase.OnOpen()` + `RaidEventTab__Initialize()`；
//        `RaidEventTab__OnClose.c` = `WindowTabBase.OnClose()` + `RaidEventTab__Reset()`。
//      · `RaidEventTab__Initialize.c` 逐句：
//          ① `rewardSelector.scoringEvent = 当前事件` → `RaidRewardSelector__CreateRewards()`（**按里程碑逐件 Instantiate**）；
//          ② `backgroundImage.sprite = LiveOpsAssetUtility.LoadAsset(事件, 1)`，随后
//             `Behaviour.set_enabled(image, sprite != null)` ⇒ **没有 liveops 素材时整件关掉**（不是「画个空」）；
//          ③ `headerController.Initialize(当前事件)`。
//      · `RaidHeaderController__Initialize.c` 逐句：
//          `title.text = LiveOpsAssetUtility.GetLabel(事件)`；`armyIcon.sprite = LoadAsset(事件, 5)`；
//          `UpdateText()`；`StartCoroutine(UpdateTimer())`（每秒刷倒计时）。
//      · `RaidHeaderController__UpdateText.c` 逐句：
//          `timeTitle.text` = 事件阶段 > 0 ? （已过期 ? `Ends in:` : `Starts in:`）：（阶段 = 0 时 timeTitle 整件 SetActive(false)）；
//          `timeRemaining.text = SupportMethods.GetTimeStringFromDate(事件日期)`；
//          `gracePeriodTooltip.SetActive(事件日期 < 服务器时间)`（= 宽限期才显示那个 ⓘ）。
//      · `RaidHeaderController__Awake.c`：给 `eventHelpButton` 挂 `ClickEventHelpButton` ——
//          **本 prefab 的 `eventHelpButton` 是空 PPtr（0）** ⇒ 那一跳在本页**根本不执行**（别伪造一颗钮）。
//      · `RaidRewardSelector__CreateRewards.c`：`RaidEvent.Milestones` 为空 ⇒ **一件奖励容器都不建**（循环长度 = 0）；
//          `currentRewardItemContainers.Count >= 1` 时先全 Destroy 再重建。
//      · `RaidEventRewardItemContainer__GetState.c`（判据 = 4 个 int 比较）：
//          `collected` ⇒ 4(Collected)；否则 `社区分 >= 社区目标` ? (`个人分 >= 个人目标` ? 3 : 1) : (`个人分 >= 个人目标` ? 2 : 0)。
//          ⚠️ 注意参数顺序：`playerScoringInfo`（param_3）/ `communityScoringInfo`（param_4）——上面按此读。
//      · `RaidProgressClaimButtonController.stateDefinitions`（prefab MB `6944932261852371964` 实读，
//        6 个 target PPtr 逐个解出节点名）：
//          state 0 Locked            → ON `Level` `Lock` `Button Price Display`
//          state 1 PlayerUnlocked    → ON `Level`(+highlight) `Lock`
//          state 2 CommunityUnlocked → ON `Lock` `Collect Button` `Button Price Display`
//          state 3 TotallyUnlocked   → ON `Lock` `Collect Button` `Button Price Display`
//          state 4 Collected         → ON `Lock` `Collect Button` `Claimed Text` `Old Price Display`
// ③ **prefab / 节点树**（`bundle_menus_assets_all`）：
//      · 页根 `Raid Progress Tab`（GO pid `8034768051126757518`；组件 = `RectTransform` + `CanvasRenderer` + `RaidEventTab`）。
//        全树 dump（可复跑）：
//        `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Raid Progress Tab" --depth 14 --relative`
//        落地副本 → `d:/4/_tmp_view/raid_tab_dump.txt`；相关 prefab（`Raid Reward Container` 等）→
//        `d:/4/_tmp_view/raid_related_dump.txt`（2026-10-19 现读）。
//      · **页根 RectTransform 逐字段实读**（`RectTransform_-3447017820810090354.json`）：
//        `anchor (0,0)-(1,1)` · `anchoredPosition (82,0)` · `sizeDelta (-163.575,-0.359)` · `pivot (0.5,0.5)`
//        ⇒ 在 1920×1080 的父里落到 **x 163.7875..1920.2125 · y 0.1795..1079.8205**（= `PageDx/PageDy/PageW/PageH`）。
//        🔴 **与 `Gacha Tab` 的页根逐位相同**（同一个父、同一套锚点）—— 两个活动页在同一个 Tabs 里叠放。
//      · 页根 MB（`MonoBehaviour_-3329924186120558450.json`）三个 PPtr：
//          `rewardSelector` → `Rewards Scroll View` 上的 `RaidRewardSelector`
//          `backgroundImage` → `Background Black/Background Image`
//          `headerController` → `Header`
//      · `RaidRewardSelector` 的 `rewardItemContainer` PPtr **= `Raid Reward Container` 这个独立 prefab**
//        （运行时 `Instantiate` 到 `contentAnchor` = `Rewards Content`）。
// ④ **本地化键**（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`，逐条命中；
//     运行时走的资源是 `Resources/official_loc_13lang.json`，**13 条键全部在里面**）：
//      `Event_Description/General/Raid` = Raid / 突袭（**页签文字的兜底**，见下「有意偏离」）
//      `Event_Description/Raid/CollectiveScoringHelp` = Reach collective milestones to unlock new content / 达到集体里程碑以解锁新内容
//      `Event_Description/Raid/PlayerScoringHelp` = Reach individual milestones to unlock massive discounts / 达到个人里程碑以解锁大额折扣
//      `Event_Description/General/EndsIn` = Ends in: / 结束于：
//      `Event_Description/General/TimeEndsIn` = Ends in: · `Event_Description/General/TimeStartsIn` = Starts in:
//      `Event_Description/Raid/CollectiveTooltip` / `Event_Description/Raid/IndividualTooltip` /
//      `Event_Description/General/GracePeriodDescription`（三颗 ⓘ 的 tooltip 正文，prefab 里是 `Localize=1` 的词条键）
//      `Rewards Menu/Claimed` = Claimed / 已认领
// ⑤ **承载窗外壳** = `Shell/ParentEventWindow.cs`（界面线 `T28`，原版 `BaseParentEventWindow` 的树：
//      `Menu Dark Background` + `Content Area/Background` + `Tab Buttons` 左栏 + `Tabs` 容器）。
//      本件**直接复用它的 public 常量**（矩形 / 渐变 / 页签几何 / 色值）—— 判据只留一份，⛔ 别抄第二套。
//
// ============================ 🔴 这一扇为什么是【独立窗口】而不是 ParentEventWindow 的第三页 ============================
// 原版：`BaseParentEventWindow.SetupTabs` 按**远端 liveops 事件列表**动态建页；Raid 只是其中一种 tab。
// 本地：**没有 liveops 事件列表**（远端 PlayFab/CCD）⇒ 无法知道「Raid 与其他活动是否同时在线」。
// 处置（与 `GachaEventWindow` 同一条口径）：建「**承载窗外壳 + 恰好一个 Raid 页**」=
//      原版在「只有 Raid 这一个活动」时的等价形态；⛔ 不把别的活动页拉进来假装同时在开（那是编数据）。
// 根名 `Raid Progress Tab` = 原版**页** prefab 的根名（原版没有独立窗口 prefab，如实记录）。
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编）============================
// 活动数据全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里没有：
//   · `LiveOpsAssetUtility.GetLabel(事件)` —— 页签文字与页内 `Title`（两处同一个来源）
//   · `LiveOpsAssetUtility.LoadAsset(事件, 1)` —— `Background Image`（整页背景图）
//   · `LiveOpsAssetUtility.LoadAsset(事件, 5)` —— `Army Icon`（阵营图）
//   · `RaidEvent.Milestones` / `GlobalScore` / `PlayerScore` —— 里程碑数量、社区分、个人分、每档奖励
//   · `RaidMilestone.CollectiveScoreToUnlock` / `CostToClaim` / `DiscountedPrice` / `Rewards`
//   · 事件起止时刻（`RaidHeaderController.UpdateText` 的日期 + 宽限期判定）
//   · 页签图标（`BaseParentEventWindow.CreateEventTab` 里 `LoadAsset(事件, 0)`）
// ⇒ 本窗照 **prefab 出厂结构**建满，**数值一律 `--` 占位**（⛔ 不编日期/分数/价格/奖励），
//    并在 `Open()` 里 `Debug.LogWarning` **逐条出声**（本工程红线：不许静默失败）。
// 🔴 **一处有意偏离（照实记录）**：原版 `RaidRewardSelector.CreateRewards` 是**按里程碑逐件 Instantiate**，
//    里程碑为 0 时**一件都不建**。本地恒无里程碑数据 ⇒ 本窗**只建模板 `Raid Reward Container`（关着）**
//    + 数值 `--/--`，⛔ 不拿 prefab 里的示例值（`300/400` / `99` / `154748` / `19d 15h`）当真实数据。
//    验收口径 = 「页面能打开、看得到原版结构、空态出声」。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **页签栏**：原版 `TabButtons.AddTabButton(label, sprite)` 由 liveops 数据驱动；本窗只有一颗页签
//   （文字走 `Event_Description/General/Raid` = 「Raid / 突袭」——**这是我们挑的兜底**，
//   原版取的是 `LiveOpsAssetUtility.GetLabel(事件)`），图标留空（liveops sprite 本地没有）。
// · **`Title`**：原版运行期被 `GetLabel(事件)` 覆盖；prefab 序列化原文是 `Event Title` ⇒ 照出厂原文画
//   （同 `ParentEventWindow` 的 `Game mode` 表头那条先例），并出声。
// · **`Background Image` / `Army Icon`**：两张 liveops 图本地都没有 ⇒ **只建节点、不出图**。
//   原版 `Background Image` 在取不到 sprite 时会被 `set_enabled(false)`（见上 ②）—— 我们同态。
// · **`Xp Points Icon` / `Debug Add points`**：**prefab 出厂 INACT** ⇒ 建出来、关着（⛔ 别因为「看见了节点」就打开）。
// · **`Rewards Scroll View`**：本壳没有滚动运行时（同 `GachaEventWindow` 的口径），数据也没有
//   ⇒ 只建 `Viewport`（`ViewportClip`）+ `Rewards Content` 结构；`UIMask` 图 alpha = 0（不可见）⇒ 不画。
// · **`Raid Reward Container` 里那两条进度条**（社区 / 个人）**不在页根上**，它们在**每档奖励容器**内部
//   （= `Community Raid Progress Bar` / `Personal Raid Progress Bar`）⇒ 本件按 prefab 建进模板里。
//   ⚠️ 模板**出厂就是关着的**（无里程碑数据，见上「有意偏离」）。
// · **`Points` 的 `icon`**：prefab 里 `Image` 组件是**禁用**的（`m_Enabled = 0`），运行时由
//   `EventProgressBarController.ScoreObject` 的 `ItemDrawer` 灌物品图 ⇒ 只建节点、不出图。
// · **`RewardTransform`**：`RaidRewardDrawerController`（`ItemDrawer`）运行时逐件实例化奖励 ⇒ 只建空容器。
// · **`Credit`（领取）**：原版 `RaidProgressClaimButtonController.CollectionClick` → `RaidEvent.OnRewardCollected`
//   （服务器）⇒ 本地点击**只出声**，⛔ 不假装领取。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>Raid 团队活动页（原版 `RaidEventTab`）—— **承载窗外壳照 `Parent Event Window`、内容照 `Raid Progress Tab` prefab**。
    /// <para>🔴 活动数据全在远端 LiveOps ⇒ 结构照建、数值 `--` + 出声（⛔ 不编；判据 → 文件头「数据缺口」）。</para>
    /// <para>✅ **2026-10-23 起有生产入口了**：左竖导航第 6 颗 `Event Navigation Button` →
    /// `WindowsManager.OpenEventHub(...)`；本窗左栏的「事件钮」（<see cref="ParentEventWindow.SetupEventTabs"/>
    /// 建的）也走同一个口。直接调用 = `WindowsManager.OpenRaidEvent()`。</para></summary>
    public class RaidEventWindow : GameWindow
    {
        // ============================================================ 渲染队列档（3660–3689 · 全工程现读空档）
        // 相邻带：`GachaEventWindow` 3630–3659 · 下一档 3935（`EnergySinglePlayerOnlyEventWindow`）。
        // 🔴 梯子（每层 +1；同档时 `ImageQuad` 世界 z 恒 0 ⇒ 谁盖谁不可控 —— CLAUDE.md §三）：
        public const int QShade = 3660;        // `Menu Dark Background`（含「点暗底关窗」命中区）
        const int QContentBg = 3661;           // `Content Area/Background` 渐变（低于所有页内容）
        const int QPageBg = 3662;              // 页：`Background Black` / `Background Image`
        const int QPageArt = 3663;             // 页：滚动区底 / 卡片底
        const int QCardBg = 3664;              // 奖励容器：卡片底板
        const int QCardArt = 3665;             // 奖励容器：进度条底 / 里程碑图标
        const int QCardArt1 = 3666;            // 奖励容器：压在底上的那一层（按钮底 / 锁）
        const int QCardText = 3667;            // 奖励容器：全部文字
        const int QBar = 3668;                 // 奖励容器：进度条填充 / 端帽
        // 左侧页签栏（必须高于**页内全部件**：页签栏与页在 x 163.8–332.2 重叠）
        const int QPanel = 3670;               // `Tab Buttons` 底板
        const int QTabShadow = 3671;
        const int QTabHi = 3672;               // 选中高亮
        const int QTabIcon = 3673;
        const int QTabName = 3674;             // 页签名牌底
        const int QTabLabel = 3675;
        const int QBadge = 3676;
        const int QBadgeText = 3677;
        /// <summary>本窗内容命中区那一档（压暗层命中区必须严格低于它；`Absorb` 取 `QHit − 1`）。</summary>
        public const int QHit = 3689;

        // ============================================================ 根 / 节点名
        /// <summary>prefab 根名（= `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// ⚠️ 这是原版**页** prefab 的名字（原版没有独立窗口 prefab，见文件头）。</summary>
        public const string RootName = "Raid Progress Tab";
        public const string PageNodeName = "Raid Progress Tab Page";
        public const string TabButtonName = "Raid Tab Button";
        /// <summary>`RaidRewardSelector.rewardItemContainer` 那个 prefab 的根名（运行时逐里程碑 Instantiate）。</summary>
        public const string RewardContainerName = "Raid Reward Container";

        // ============================================================ 窗参（`Parent Event Window` 根 MB 实读）
        // `type = 0`(Fullscreen) · `windowsPlacement = 5`(Canvas) · `closeOnESC = 1` · `updateNavPanel = 1`
        // · `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` —— 逐字段实读
        //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6349037533193994564.json`。
        // ⚠️ `Raid Progress Tab` 自己**没有窗口组件**（只有 `RaidEventTab`）⇒ 窗参只能取承载窗那一份。
        public const bool ParamsCloseOnEsc = true;
        public const bool ParamsUpdateNavPanel = true;
        public const float ParamsExtraScale = 1f;
        public const bool ParamsUseDefaultCloseSound = true;

        // ============================================================ 几何：页根偏移（根 RectTransform 实读）
        // `anchor (0,0)-(1,1)` · `pos (82,0)` · `size (-163.575,-0.359)` · 父 1920×1080
        // ⇒ 页根 x **163.7875..1920.2125** · y **0.1795..1079.8205**（与 `Gacha Tab` 逐位相同）。
        public const float PageDx = 163.7875f, PageDy = 0.1795f;
        public const float PageW = 1756.425f, PageH = 1079.641f;

        static PxRect R(float x1, float y1, float x2, float y2)
            => new PxRect(x1 + PageDx, y1 + PageDy, x2 + PageDx, y2 + PageDy);

        // ---- 页根下四块（dump 相对值 + `R`）----
        // `Background Black`：anchor(0,0)-(1,1) · pos(0,0) · size(0,0) · pivot(0,0.5) ⇒ 与页根同框
        static readonly PxRect PageBgR = R(0f, 0f, PageW, PageH);
        // `Background Image`：anchor(0,0.5)-(0,0.5) · pos(-0.0001,-0.3529) · size(3767.388, 605.579) · pivot(0,0.5)
        static readonly PxRect PageBgImageR = R(-0.0f, 237.4f, 3767.4f, 843.0f);
        // `Rewards Scroll View`：anchor(0,0.5)-(1,0.5) · pos(0.2778,-43.169) · size(-0.555,922.721) · pivot(0,0.5)
        static readonly PxRect ScrollR = R(0.28f, 121.63f, 1756.15f, 1044.35f);
        // `Rewards Content`：anchor(0,0.5)-(0,0.5) · pos(0,-23.247) · size(0, 829.149) · pivot(0,0.5)
        // 🔴 原版宽 **0**（`ContentSizeFitter` 横向 PreferredSize，运行时按子件撑开）⇒ 这里只留轨道框。
        static readonly PxRect ContentR = R(0.28f, 191.66f, 122.28f, 1020.81f);

        // ---- `Header`（anchor(0,1)-(1,1) · pos(0,-90.89) · size(0.0000249,182) · pivot(.5,.5)）----
        static readonly PxRect HeaderR = R(0f, -0.11f, PageW, 181.89f);
        static readonly PxRect TitleR = R(127.56f, 23.06f, 1448.46f, 86.72f);
        static readonly PxRect InstructionsCommunityR = R(190f, 94.89f, 1510f, 144.89f);
        static readonly PxRect ArmyIconR = R(5f, 0.89f, 125f, 120.89f);
        static readonly PxRect HelpIconR = R(121.91f, 93.80f, 174.09f, 145.99f);
        static readonly PxRect XpPointsIconR = R(152.07f, 118.82f, 205.03f, 171.78f);   // 出厂 INACT
        static readonly PxRect TotalXpPointsR = R(205.03f, 120.30f, 437.36f, 170.30f);
        // `Time Remaining`：pivot **1.0** ⇒ 右缘钉在 x 1638.425；原版**宽 0**（CSF 横向 PreferredSize）
        // ⇒ 见 `TimeTitleR` 的说明（本壳没有 CSF 运行时，用 300 宽的右对齐框代替）。
        static readonly PxRect TimeRemainingR = R(1638.425f, 6.03f, 1638.425f, 56.03f);
        static readonly PxRect TimeTitleR = R(1338.425f, 6.03f, 1638.425f, 56.03f);     // ← 我们挑的框（右缘 = 原版 pivot）
        static readonly PxRect TimeR = R(1645.25f, 6.03f, 1755.56f, 56.03f);
        static readonly PxRect HelpGracePeriodR = R(1581.64f, 4.94f, 1633.82f, 57.12f);

        // ---- `Footer`（anchor(0,0)-(1,0) · pos(0,90) · size(0,182) · pivot(.5,.5)）----
        static readonly PxRect FooterR = R(0f, 898.64f, PageW, 1080.64f);
        static readonly PxRect HelpIconPlayerR = R(121.91f, 954.54f, 174.09f, 1006.73f);
        static readonly PxRect InstructionsPlayerR = R(190f, 955.64f, 1510f, 1005.64f);

        // ---- `Debug Add points`（**出厂 INACT** · 调试用）----
        static readonly PxRect DebugAddPointsR = R(700.30f, 142.85f, 903.94f, 196.78f);
        static readonly PxRect DebugTextR = DebugAddPointsR;
        static readonly PxRect DebugInputR = R(902.21f, 142.85f, 986.77f, 196.78f);

        // ============================================================ 几何：`Raid Reward Container`（模板 · 出厂关着）
        // 容器根 505.9 × 719.463（`RectTransform` 实读）。落位 = `Rewards Content` 的 HLG：
        //   `m_Padding.left = 122` · `m_Spacing = -130` · `m_ChildAlignment = 3`(MiddleLeft)
        public const float ContainerW = 505.90f, ContainerH = 719.46f;
        static readonly PxRect ContainerR =
            new PxRect(ContentR.x1 + 122f,
                       ContentR.y1 + (ContentR.H - ContainerH) * 0.5f,
                       ContentR.x1 + 122f + ContainerW,
                       ContentR.y1 + (ContentR.H - ContainerH) * 0.5f + ContainerH);

        /// <summary>奖励容器**内部**坐标（原版它自己的 `RectTransform` 左上原点）。</summary>
        static PxRect K(float x1, float y1, float x2, float y2)
            => new PxRect(ContainerR.x1 + x1, ContainerR.y1 + y1, ContainerR.x1 + x2, ContainerR.y1 + y2);

        static readonly PxRect CollectHighlightR = K(60.40f, -5.00f, 446.10f, 511.40f);        // 出厂 INACT
        static readonly PxRect RewardTransformR = K(104.10f, 58.40f, 401.80f, 443.10f);
        // 社区这条（`Community Progression`）：整块向左挂出容器外（原版就是这样，运行期由
        // `RaidRewardSelector` / `RaidProgressBarScaleAndPositionController` 再摆位）——照抄。
        static readonly PxRect CommunityProgressionR = K(98.00f, -87.60f, 407.90f, 19.00f);
        static readonly PxRect CommunityBarRootR = K(-70.10f, -87.60f, 239.80f, 19.00f);
        static readonly PxRect CommunityBarR = K(-83.80f, -47.60f, 217.20f, -27.60f);
        static readonly PxRect CommunityBarBgR = K(-83.80f, -47.40f, 217.20f, -27.80f);
        static readonly PxRect CommunityBarFillR = K(-83.80f, -27.40f, -83.80f, -27.80f);     // 出厂 0 宽
        static readonly PxRect CommunityBarEndR = K(-95.80f, -56.20f, -42.70f, 1.10f);
        static readonly PxRect CommunityPointsR = K(-10.20f, -18.20f, 143.50f, 19.00f);
        static readonly PxRect CommunityPointsIconR = K(-6.40f, -18.20f, 30.80f, 19.00f);
        static readonly PxRect CommunityPointsTextR = K(34.50f, -18.20f, 143.50f, 19.00f);
        static readonly PxRect CommunityLevelR = K(210.40f, -78.30f, 295.40f, 9.70f);
        static readonly PxRect CommunityLevelLabelR = K(210.50f, -78.30f, 295.50f, 9.70f);

        // 个人这条（`Personal Progression`）
        static readonly PxRect PersonalProgressionR = K(207.90f, 514.70f, 298.00f, 614.70f);
        static readonly PxRect PersonalBarRootR = K(-85.40f, 511.40f, 215.90f, 618.00f);
        static readonly PxRect PersonalBarR = K(-85.40f, 554.70f, 215.90f, 574.70f);
        static readonly PxRect PersonalBarBgR = K(-85.40f, 554.90f, 215.90f, 574.50f);
        static readonly PxRect PersonalBarFillR = K(-85.40f, 574.90f, -85.40f, 574.50f);      // 出厂 0 宽
        static readonly PxRect PersonalBarEndR = K(-97.30f, 546.00f, -44.20f, 603.40f);
        static readonly PxRect PersonalPointsR = K(-11.60f, 581.00f, 142.10f, 618.20f);
        static readonly PxRect PersonalPointsIconR = K(-7.90f, 581.00f, 29.40f, 618.20f);
        static readonly PxRect PersonalPointsTextR = K(33.10f, 581.00f, 142.10f, 618.20f);
        static readonly PxRect ClaimContainerR = K(210.40f, 520.70f, 295.40f, 608.70f);
        static readonly PxRect OldPriceDisplayR = K(154.00f, 475.10f, 349.80f, 515.20f);
        static readonly PxRect OldPriceIconR = K(169.60f, 475.10f, 209.70f, 515.20f);
        static readonly PxRect OldPriceTextR = K(213.70f, 475.10f, 338.20f, 515.20f);
        static readonly PxRect CollectButtonR = K(141.00f, 523.80f, 362.80f, 605.70f);
        static readonly PxRect ClaimedTextR = K(154.00f, 545.70f, 349.80f, 583.80f);
        static readonly PxRect ButtonPriceDisplayR = K(149.40f, 544.50f, 352.70f, 586.20f);
        static readonly PxRect ButtonPriceIconR = K(168.00f, 544.50f, 209.60f, 586.20f);
        static readonly PxRect ButtonPriceTextR = K(213.70f, 544.50f, 338.20f, 586.20f);
        static readonly PxRect ClaimLevelR = K(210.40f, 520.70f, 295.40f, 608.70f);
        static readonly PxRect ClaimLockR = K(235.70f, 540.90f, 270.20f, 585.60f);

        // ============================================================ 图 / 色（Image MB 逐字段实读）
        const string ArtHelpInfo = "40K_generic_bt_info";            // 128×128 Simple（三颗 ⓘ 共用）
        const string ArtForgePoints = "40K_general_icon_Forge_points"; // 88×85（`Xp Points Icon`，出厂 INACT）
        const string ArtArmyTrackBar = "40K_ArmyTrack_bar";           // 15×21 Simple · ppuMul 0.92
        const string ArtArmyTrackFill = "40K_ArmyTrack_bar_fill";     // 15×21 Simple · ppuMul 0.92
        const string ArtArmyTrackPos = "40K_ArmyTrack_bar_position";  // 64×64 Simple · ppuMul 0.92
        const string ArtMilestone = "40k_ArmyTrack_milestone_off";    // 85×88 Simple
        const string ArtMilestoneOn = "40k_ArmyTrack_milestone_on";
        const string ArtLock = "WF_Lock_Icon_Simple";                 // 42×63 Simple（原版 sprite 名 = `WF Lock Icon Simple`）
        const string ArtGlowBorder = "Border_Line_FX";                // 128×128 九宫 50,50,50,50 · ppuMul 0.5
        const string ArtBtn = "40K_button";                           // 489×107 九宫 234,46,234,46

        static readonly Color CommunityBarBgTint = new Color(1f, 0.765f, 0.505f, 1f);      // MB 实读 (1,0.765,0.505,1)
        static readonly Color CommunityBarFillTint = new Color(1f, 0.395f, 0.118f, 1f);    // MB 实读
        static readonly Color PersonalBarFillTint = new Color(0.613f, 0.273f, 0f, 1f);     // MB 实读
        static readonly Color LockTint = new Color(0f, 0f, 0f, 1f);                        // MB 实读 (0,0,0,1)
        static readonly Color CollectBtnTint = new Color(0.902f, 0.637f, 0.18f, 1f);       // MB 实读
        static readonly Color DebugBtnTint = new Color(1f, 0f, 0.9456892f, 1f);            // MB 实读（调试件）

        // ============================================================ 本地化键（原版 `Localize.mTerm` / `EverguildTooltipTrigger.text` 实读）
        /// <summary>页签文字兜底。⚠️ **原版取的是 `LiveOpsAssetUtility.GetLabel(事件)`（远端）**；
        /// 本条是 `Event_Description/General/Raid`（官方表里那条通用「Raid / 突袭」）—— **我们挑的兜底**，⛔ 不冒充原版。</summary>
        public const string TermTabTitle = "Event_Description/General/Raid";
        public const string TermInstructionsCommunity = "Event_Description/Raid/CollectiveScoringHelp";
        public const string TermInstructionsPlayer = "Event_Description/Raid/PlayerScoringHelp";
        public const string TermEndsIn = "Event_Description/General/EndsIn";
        public const string TermClaimed = "Rewards Menu/Claimed";
        public const string TermTipCommunity = "Event_Description/Raid/CollectiveTooltip";
        public const string TermTipIndividual = "Event_Description/Raid/IndividualTooltip";
        public const string TermTipGracePeriod = "Event_Description/General/GracePeriodDescription";

        /// <summary>prefab 出厂原文（`Title` 那一条）—— 运行期被 `GetLabel(事件)` 覆盖，本地拿不到
        /// ⇒ 照出厂原文画（同 `ParentEventWindow.P2HeaderTitle = "Game mode"` 那一条先例），并出声。</summary>
        public const string PrefabTitleText = "Event Title";
        /// <summary>无数据占位（⚠️ **不是原文**：远端数据缺 ⇒ 只印占位，⛔ 不编数字）。</summary>
        public const string NoData = "--";

        /// <summary>三颗 ⓘ 的 `EverguildTooltipTrigger` 参数（MB 逐字段实读）。
        /// `tooltipAnchor`：20 = 集体说明 / 25 = 宽限期 / 30 = 个人说明（**三颗各不相同，⛔ 别互抄**）。</summary>
        public const int TipAnchorCommunity = 20, TipAnchorGrace = 25, TipAnchorIndividual = 30;
        /// <summary>三颗 ⓘ 的 `Graphic.m_RaycastPadding` = **(-20,-20,-20,-20)** ⇒ 命中区每边**外扩 20**。</summary>
        public static readonly Vector4 HelpIconPad = new Vector4(-20f, -20f, -20f, -20f);

        // ============================================================ 状态 / 自检读口
        public static RaidEventWindow LastOpened { get; private set; }
        /// <summary>活动数据本地没有（恒 true —— 见文件头「数据缺口」）。</summary>
        public bool DataMissing { get { return true; } }
        public string DataNote
        {
            get
            {
                return "Raid 活动数据全在远端 LiveOps（事件标题/背景图/阵营图、里程碑列表、社区分与个人分、"
                     + "每档奖励与价格、活动起止时刻、页签图标）—— 本地离线包体里没有 ⇒ "
                     + "结构照 prefab 建满、数值一律 `--` 占位，⛔ 不编。"
                     + "领奖 = 原版 `RaidProgressClaimButtonController.CollectionClick` → `RaidEvent.OnRewardCollected`"
                     + "（服务器）⇒ 本地点击只出声。";
            }
        }

        Transform _shade, _contentArea, _background, _tabButtons, _tabShadow, _tabs, _tabMaster;
        Transform _page, _pageBg, _pageBgImage, _scroll, _viewport, _content, _header, _footer, _debug,
                  _container, _collectHighlight, _armyIcon;
        Label _title, _instructionsCommunity, _instructionsPlayer, _timeTitle, _time, _totalXp, _tabLabel;
        readonly List<Transform> _tabButtonsList = new List<Transform>();
        readonly List<Transform> _tabHi = new List<Transform>();
        readonly List<Label> _tabLabels = new List<Label>();
        // 🆕 2026-10-23（活动线 `L02`）：**事件钮**（原版 `BaseParentEventWindow.SetupTabs` 在任何一扇
        //   活动窗的左栏都列全部 live 事件 ⇒ Raid 这扇也要有）。**与内容页那三张表分家**
        //   （理由 → `Shell/ParentEventWindow.cs` 的 `SetupEventTabs`）。
        readonly List<Transform> _eventTabs = new List<Transform>();
        readonly List<Transform> _eventHi = new List<Transform>();
        readonly List<Label> _eventLabels = new List<Label>();
        bool _tabOverflowWarned;
        bool _warned;
        readonly List<string> _missArt = new List<string>();
        readonly List<string> _artNotes = new List<string>();

        /// <summary>建不出来的图（非空 = 有件没画；自检读口）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>「原版用 A、我们只能回退 B」/「原版有 liveops 图、我们画不出」的图（自检读口；⛔ 与 `MissingArt` 不是一回事）。</summary>
        public List<string> ArtNotes { get { return _artNotes; } }

        public Transform ShadeNode { get { return _shade; } }
        public Transform ContentAreaNode { get { return _contentArea; } }
        public Transform TabButtonsNode { get { return _tabButtons; } }
        public Transform TabsNode { get { return _tabs; } }
        public Transform PageNode { get { return _page; } }
        public Transform PageBackgroundNode { get { return _pageBg; } }
        public Transform PageBackgroundImageNode { get { return _pageBgImage; } }
        public Transform RewardsScrollNode { get { return _scroll; } }
        public Transform ViewportNode { get { return _viewport; } }
        public Transform RewardsContentNode { get { return _content; } }
        public Transform HeaderNode { get { return _header; } }
        public Transform FooterNode { get { return _footer; } }
        public Transform ArmyIconNode { get { return _armyIcon; } }
        public Transform DebugAddPointsNode { get { return _debug; } }
        /// <summary>`RaidRewardSelector.rewardItemContainer` 那份模板（**出厂关着**，无里程碑数据）。</summary>
        public Transform RewardContainerTemplateNode { get { return _container; } }
        public Transform CollectHighlightNode { get { return _collectHighlight; } }
        /// <summary>`Raid Reward Container` 内部的两条进度条（自检读口；模板关着时它们也不画）。</summary>
        public Transform CommunityProgressBarNode { get { return _communityBar; } }
        public Transform PersonalProgressBarNode { get { return _personalBar; } }
        Transform _communityBar, _personalBar;

        public List<Transform> TabButtonNodes { get { return _tabButtonsList; } }
        public List<Transform> TabHighlights { get { return _tabHi; } }
        public List<Label> TabLabels { get { return _tabLabels; } }
        /// <summary>🆕 2026-10-23：**事件钮**（`SetupTabs` 建的那几颗；**不含**内容页签，兄弟序 = 事件名单序）。</summary>
        public List<Transform> EventTabNodes { get { return _eventTabs; } }
        public List<Transform> EventTabHighlights { get { return _eventHi; } }
        public List<Label> EventTabLabels { get { return _eventLabels; } }
        /// <summary>`SetupTabs` 那一声出声的原文（自检读口）。</summary>
        public string EventTabNote { get; private set; }
        public Label TitleLabel { get { return _title; } }
        public Label InstructionsCommunityLabel { get { return _instructionsCommunity; } }
        public Label InstructionsPlayerLabel { get { return _instructionsPlayer; } }
        public Label TimeTitleLabel { get { return _timeTitle; } }
        public Label TimeLabel { get { return _time; } }
        public Label TotalXpLabel { get { return _totalXp; } }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenRaidEvent` 的 `create` 那一跳）。</summary>
        public static RaidEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject(RootName);                        // 节点名照 prefab（`OpenByRef` 的键同源）
            var win = go.AddComponent<RaidEventWindow>();
            win.type = WindowType.Fullscreen;                         // MB 实读 `type = 0`
            win.placement = WindowsPlacement.Canvas;                  // MB 实读 `windowsPlacement = 5`
            win.closeOnEsc = ParamsCloseOnEsc;                        // MB 实读 `closeOnESC = 1`
            win.updateNavPanel = ParamsUpdateNavPanel;                // MB 实读 `updateNavPanel = 1`
            win.extraScaleSmallScreen = ParamsExtraScale;             // MB 实读 1.0（= 不覆盖）
            win.useDefaultCloseSoundIfNull = ParamsUseDefaultCloseSound;
            // `openSound` / `closeSound` 实读都是空 PPtr ⇒ 一个 clip 都不赋，走家族默认。
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Raid] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
            WarnOnce();
        }

        /// <summary>被更高优先级的**弹窗**压到背景（原版 `GameWindow.ToBackground` = 只写 `state = Background`）。
        /// <para>🔴 **我们为什么多关一次物体**：本壳是**扁平 quad + 显式渲染队列**，没有原版那种独立
        /// `Canvas.sortingOrder`。本窗占 3660–3689，而能从本窗开出去的弹窗（`WhereToGetCardsPopup` 3479–3487
        /// 那一档）都比本窗低 ⇒ 只写 state 的话，**弹窗会被本窗整个盖住**（症状 = 「点了没反应」，静默失败）。
        /// 处置照 `GachaEventWindow.ToBackground` 的形状：**被压背景时把整棵关掉**；关掉弹窗时
        /// `WindowsManager.ShowPreviousWindow` 会对列表尾调无参 `TryOpen()`，它的 `Background` 支会
        /// `SetActive(true)` + 回 `Open` 态 ⇒ 本窗自动回到台上。
        /// ⚠️ **这是【我们挑的】处置，不是原版 base 的行为**（原版 base 只写 `0x68 = 2`）。</para></summary>
        public override void ToBackground()
        {
            base.ToBackground();          // 原版那一句：`state = Background`
            gameObject.SetActive(false);  // 扁平队列下必须物理让位（见 doc）
        }

        void WarnOnce()
        {
            if (_warned) return;
            _warned = true;
            Debug.LogWarning("[Raid] " + DataNote);
            Debug.LogWarning("[Raid] ⚠️ **有意偏离**：原版 `RaidRewardSelector.CreateRewards` 按 `RaidEvent.Milestones` "
                           + "逐件 Instantiate 奖励容器，里程碑为空时**一件都不建**；本件的验收口径 = "
                           + "**页面能打开 + 空态出声** ⇒ 只建模板（`Raid Reward Container`，关着）+ 数值 `--`（见文件头）。");
            Debug.LogWarning("[Raid] 页签文字用 `" + TermTabTitle + "` 兜底（Raid / 突袭）：原版是 "
                           + "`LiveOpsAssetUtility.GetLabel(事件)`（远端）⇒ 本地拿不到；页签**图标**留空（liveops sprite 本地没有），"
                           + "⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。");
            Debug.LogWarning("[Raid] 页内 `Title` 照 prefab 出厂原文 `" + PrefabTitleText + "` 画 —— "
                           + "原版运行期被 `RaidHeaderController.Initialize` 的 `GetLabel(事件)` 覆盖，本地没有那份数据。");
            Debug.LogWarning("[Raid] 两张 liveops 图缺失：`Background Image`（`LoadAsset(事件,1)`）与 "
                           + "`Army Icon`（`LoadAsset(事件,5)`）⇒ 只建节点、不出图。"
                           + "⚠️ 原版在 `Background Image` 取不到 sprite 时也会 `set_enabled(false)`（`RaidEventTab.Initialize`）。");
            Debug.LogWarning("[Raid] 领奖（Collect）需要服务器：原版 `RaidProgressClaimButtonController.CollectionClick` → "
                           + "`RaidEvent.OnRewardCollected`（LiveOps）；本地没有后端 ⇒ **点击只出声、不假装领取**。");
            if (_artNotes.Count > 0)
                Debug.LogWarning("[Raid] 有 " + _artNotes.Count + " 条图/件是**降级或未画**："
                                 + string.Join("、", _artNotes.ToArray()));
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            ArtNotes.Clear();
            _tabButtonsList.Clear(); _tabHi.Clear(); _tabLabels.Clear();
            _tabOverflowWarned = false;                      // 🆕 事件钮的「溢出左栏」告警一次一建（同 `ParentEventWindow`）
            _shade = _contentArea = _background = _tabButtons = _tabShadow = _tabs = _tabMaster = null;
            _page = _pageBg = _pageBgImage = _scroll = _viewport = _content = _header = _footer = _debug = null;
            _container = _collectHighlight = _communityBar = _personalBar = _armyIcon = null;
            _title = _instructionsCommunity = _instructionsPlayer = _timeTitle = _time = _totalXp = _tabLabel = null;

            BuildShell();
            BuildTabBar();
            BuildPage();
            BuildShellTail();
            // 🆕 2026-10-23（活动线 `L02`）：左栏的「事件钮」（原版 `BaseParentEventWindow.SetupTabs` 在
            //   **每一扇**活动窗的左栏都列全部 live 事件）。兄弟序 = 内容页在前、事件钮在后。
            SetupTabs(ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Raid,
                                                       LiveOpsLocalCatalog.NowUtc));

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Raid] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 承载窗外壳（= `Parent Event Window` 那一族）
        void BuildShell()
        {
            // ---- 0) `Menu Dark Background`（整屏压暗；程序集族口径 = 点暗底关窗）----
            _shade = MenuDraw.Node(transform, "Menu Dark Background", ParentEventWindow.ShadeR);
            MenuDraw.Rect(_shade, CardArt.Solid(), ParentEventWindow.ShadeR, "Image", QShade,
                          ParentEventWindow.ShadeTint);
            MenuDraw.ShadeHit(_shade, ParentEventWindow.ShadeR, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 1) `Content Area`（167.2,70.9 → 1920,1080）----
            _contentArea = MenuDraw.Node(transform, "Content Area", ParentEventWindow.ContentAreaR);

            // ---- 2) `Background`：`Image`(sprite=null) + `UIGradient`（纯代码渐变；色值/角度判据只留
            //        `ParentEventWindow.BgGrad*` 那一份）----
            _background = MenuDraw.Node(_contentArea, "Background", ParentEventWindow.ContentAreaR);
            var bgTex = CardArt.Gradient(ParentEventWindow.BgGradC1, ParentEventWindow.BgGradC2,
                                         ParentEventWindow.BgGradAngle,
                                         ParentEventWindow.ContentAreaR.H / ParentEventWindow.ContentAreaR.W);   // 原版 CompensateAspectRatio 的 h/w
            MenuDraw.Rect(_background, bgTex, ParentEventWindow.ContentAreaR, "Image", QContentBg, Color.white);
        }

        /// <summary>`Tabs` 之后的两件（`Shadow (1)` + 吸收层）—— 兄弟序照原版：
        /// `Background` → `Tab Buttons` → `Tabs` → `Shadow (1)`。</summary>
        void BuildShellTail()
        {
            // ---- `Shadow (1)`（**出厂 INACT**；与 ParentEventWindow 同值）----
            var shadow1 = MenuDraw.Node(_contentArea, "Shadow (1)", ParentEventWindow.Shadow1R);
            MenuDraw.Rect(shadow1, Tex(ParentEventWindow.ArtTabShadow, "页签栏阴影（右侧那条）"),
                          ParentEventWindow.Shadow1R, "Image", QTabShadow, ParentEventWindow.TabShadowTint);
            shadow1.gameObject.SetActive(false);

            // 页签栏底板 / 压暗层之间的吸收：原版点这里**什么都不做**（⛔ 别让它冒到压暗层去关窗）。
            MenuDraw.Absorb(_contentArea, "AbsorbHit", ParentEventWindow.ContentAreaR, QShade, QHit);
        }

        // ============================================================ 页签栏（`Tab Buttons`；原版运行期 AddTabButton）
        void BuildTabBar()
        {
            _tabButtons = MenuDraw.Node(_contentArea, "Tab Buttons", ParentEventWindow.TabButtonsR);
            MenuDraw.GradientV(_tabButtons, Tex(ParentEventWindow.ArtTabBg, "页签栏底板"),
                               ParentEventWindow.TabButtonsR, QPanel, "Image",
                               ParentEventWindow.TabGradT, ParentEventWindow.TabGradC);

            _tabShadow = MenuDraw.Node(_tabButtons, "Shadow", ParentEventWindow.TabShadowR);
            MenuDraw.Rect(_tabShadow, Tex(ParentEventWindow.ArtTabShadow, "页签栏阴影"),
                          ParentEventWindow.TabShadowR, "Image", QTabShadow, ParentEventWindow.TabShadowTint);

            // 母版（原版 `TabButtons.Initialize` 先把 `tabButtonPrefab.SetActive(false)`）
            _tabMaster = BuildTabButton("Menu Navigation Panel Button", ParentEventWindow.TabTop,
                                        ParentEventWindow.MasterLabel, true);
            _tabMaster.gameObject.SetActive(false);

            // 唯一运行期页签（原版 = `AddTabButton(label, sprite)`；文字走 Loc、图标留空）
            var lbl = Loc.T(TermTabTitle);
            var tab = BuildTabButton(TabButtonName, ParentEventWindow.TabTop, lbl, false);
            _tabButtonsList.Add(tab);
            _tabLabel = _tabLabels.Count > 0 ? _tabLabels[_tabLabels.Count - 1] : null;
            // 单页：点页签只重选当前页。原版 `EverguildToggle.allowClickWhenAlreadySelected = 0`
            // ⇒ 已选中时**连按钮音一起跳过**（同 `ParentEventWindow` / `GachaEventWindow` 的口径）。
            var tabHit = MenuDraw.Hit(tab, "Hit",
                                      new PxRect(ParentEventWindow.TabButtonsR.x1, ParentEventWindow.TabTop,
                                                 ParentEventWindow.TabButtonsR.x2,
                                                 ParentEventWindow.TabTop + ParentEventWindow.TabH),
                                      QHit, () => { }, null, null, null, null, null,
                                      clickCue: WindowSounds.DefaultButtonCue);
            var wb = tabHit != null ? tabHit.GetComponent<WindowButton>() : null;
            if (wb != null)
            {
                wb.allowClickWhenAlreadySelected = false;
                wb.isOn = () => true;
            }
            if (_tabHi.Count > 0) _tabHi[0].gameObject.SetActive(true);   // 唯一页 = 恒选中
        }

        /// <summary>🆕 2026-10-23（活动线 `L02`）：**建左栏的「事件钮」** —— 与 `ParentEventWindow`
        /// **走同一份判据**（`ParentEventWindow.SetupEventTabs` / `BuildEventTabs`；本类只提供自己的
        /// `BuildTabButton` 与自己的渲染队列档，⛔ 不在这里再抄一份 `SetupTabs` 的实现）。
        /// <para>内容页占 1 颗 ⇒ 事件钮从第 2 格（`TabTop + TabH`）往下排。</para></summary>
        public int SetupTabs(IList<ParentEventWindow.EventTab> tabs)
        {
            string note;
            int n = ParentEventWindow.SetupEventTabs(
                tabs, 1,
                (name, top, label) => BuildTabButton(name, top, label, false),
                _eventTabs, _eventHi, _eventLabels, _tabHi, _tabLabels,
                QTabIcon, QHit, ref _tabOverflowWarned, out note);
            EventTabNote = note;
            return n;
        }

        Transform BuildTabButton(string name, float top, string label, bool isMaster)
        {
            var btn = MenuDraw.Node(_tabButtons, name,
                                    new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                               ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH));

            // `Highlight`：整颗按钮同矩形 · `40k_main_bt_selected_BW`（九宫 30 · ppuMul 0.92 · tint 红）
            var hi = MenuDraw.Node(btn, "Highlight",
                                   new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                              ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH));
            MenuDraw.Nine(hi, Tex(ParentEventWindow.ArtTabHi, "页签选中高亮"),
                          new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                     ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH),
                          new Vector4(30f, 30f, 30f, 30f), 71f, 71f, QTabHi,
                          ParentEventWindow.TabHiTint, true, "Image",
                          new Vector4(ParentEventWindow.TabHiCorner, ParentEventWindow.TabHiCorner,
                                      ParentEventWindow.TabHiCorner, ParentEventWindow.TabHiCorner));
            if (!isMaster) _tabHi.Add(hi);
            hi.gameObject.SetActive(false);   // 由 `BuildTabBar` 打开（原版 `m_IsOn` 的等价物）

            // `Icon`：运行期图标 = liveops sprite，本地没有 ⇒ 只建空节点（母版那一颗照出厂图）
            var icon = MenuDraw.Node(btn, "Icon",
                                     new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                                ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH));
            if (isMaster)
                MenuDraw.Rect(icon, Tex(ParentEventWindow.ArtTabIcon, "母版页签图标"),
                              new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                         ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH),
                              "Image", QTabIcon, Color.white, true);

            // `Label`（名牌底板 155×37.86）+ `TabButtonLabel`（fs25.65 / base23 / auto[12,33] / Midline / 字距 -3.5）
            float dy = top - ParentEventWindow.TabTop;
            var nameR = new PxRect(ParentEventWindow.TabNameR.x1, ParentEventWindow.TabNameR.y1 + dy,
                                   ParentEventWindow.TabNameR.x2, ParentEventWindow.TabNameR.y2 + dy);
            var labelNode = MenuDraw.Node(btn, "Label", nameR);
            MenuDraw.Rect(labelNode, Tex(ParentEventWindow.ArtTabName, "页签名牌底板"), nameR, "Image",
                          QTabName, Color.white);
            var lb = MenuDraw.TextBox(labelNode, nameR, label, ParentEventWindow.TabLabelInk, "TabButtonLabel",
                                      ParentEventWindow.TabLabelFont, ParentEventWindow.TabLabelMin, QTabLabel,
                                      ParentEventWindow.TabLabelMax, ParentEventWindow.TabLabelBase,
                                      face: TmpFont.Face.LatinCardBody);
            if (lb != null)
            {
                lb.SetWrapping(false);
                MenuDraw.SetVAlign(lb, Label.VAlign.Midline, nameR);
                lb.SetCharSpacing(ParentEventWindow.TabLabelSpacing);
                if (!isMaster) _tabLabels.Add(lb);
            }

            // `Badge Highlight`（35×35 · `40K_notification_number` · 计数来自通知系统 ⇒ 出厂空字）
            var badgeR = new PxRect(ParentEventWindow.TabBadgeR.x1, ParentEventWindow.TabBadgeR.y1 + dy,
                                    ParentEventWindow.TabBadgeR.x2, ParentEventWindow.TabBadgeR.y2 + dy);
            var badgeTextR = new PxRect(ParentEventWindow.TabBadgeTextR.x1, ParentEventWindow.TabBadgeTextR.y1 + dy,
                                        ParentEventWindow.TabBadgeTextR.x2, ParentEventWindow.TabBadgeTextR.y2 + dy);
            var badge = MenuDraw.Node(btn, "Badge Highlight", badgeR);
            MenuDraw.Rect(badge, Tex(ParentEventWindow.ArtBadge, "页签红点"), badgeR, "Image",
                          QBadge, ParentEventWindow.TabBadgeTint, true);
            var one = MenuDraw.TextBox(badge, badgeTextR, "", Color.white, "OneText", 34f, 18f, QBadgeText,
                                       34f, 36f, face: TmpFont.Face.LatinCardBody);
            if (one != null) MenuDraw.SetVAlign(one, Label.VAlign.Middle, badgeTextR);

            return btn;
        }

        // ============================================================ `Raid Progress Tab` 页（原版页根：x 163.79..1920.21）
        void BuildPage()
        {
            // `Tabs`（原版 `TabbedWindowComponents.tabHolder`）—— 兄弟序在 `Tab Buttons` 之后、`Shadow (1)` 之前
            _tabs = MenuDraw.Node(_contentArea, "Tabs", ParentEventWindow.TabsR);
            _page = MenuDraw.Node(_tabs, PageNodeName, R(0f, 0f, PageW, PageH));

            BuildPageBackground(_page);
            BuildRewardsScroll(_page);
            BuildHeader(_page);
            BuildFooter(_page);
            BuildDebugAddPoints(_page);
        }

        // ------------------------------------------------------------ `Background Black` / `Background Image`
        void BuildPageBackground(Transform page)
        {
            // `Background Black`：`Image`(sprite=null · Sliced · ppuMul 0.01 · 纯黑 α1) —— 没有 sprite 时
            // uGUI 仍按 `m_Color` 画满（原版整页底色就是它）⇒ 我们用实色 quad。
            _pageBg = MenuDraw.Node(page, "Background Black", PageBgR);
            MenuDraw.Rect(_pageBg, CardArt.Solid(), PageBgR, "Image", QPageBg, new Color(0f, 0f, 0f, 1f));

            // `Background Image`：**运行时** `LoadAsset(事件, 1)` 灌图（`Tiled` · ppuMul 1.68），
            // 取不到时原版把 **`Image` 组件** `set_enabled(false)`（⚠️ 不是关 GO —— `m_IsActive` 仍是 true）
            // ⇒ 本件**只建节点、不出图**，节点保持 active（同原版：GO 在、图不画）。
            _pageBgImage = MenuDraw.Node(_pageBg, "Background Image", PageBgImageR);
            Note("页背景图 `Background Image` = liveops `LoadAsset(事件,1)`（`Tiled` · ppuMul 1.68）"
                 + " —— 本地没有 ⇒ 只建节点、不出图（原版取不到 sprite 时把 `Image` 组件 `set_enabled(false)`）");
        }

        // ------------------------------------------------------------ `Rewards Scroll View`（= `RaidRewardSelector`）
        void BuildRewardsScroll(Transform page)
        {
            _scroll = MenuDraw.Node(page, "Rewards Scroll View", ScrollR);
            Note("`Rewards Scroll View`：原版 `ScrollRect`（`m_Horizontal = 1` · `m_Vertical = 0` · `m_MovementType = 1`）"
                 + " —— 本壳没有滚动运行时、且本地没有里程碑数据 ⇒ 只建结构（视口裁切 + 内容轨）");

            _viewport = ViewportClip.Hang(_scroll, "Viewport", ScrollR, Vector4.zero, Vector2Int.zero).transform;
            // `Viewport` 的 `UIMask` 图 **alpha = 0**（不可见）⇒ 不画；裁切已由 `ViewportClip` 承担（如实记录）。

            _content = MenuDraw.Node(_viewport, "Rewards Content", ContentR);

            // `Raid Reward Container` = `RaidRewardSelector.rewardItemContainer` 的模板
            //（原版运行时按里程碑逐件 `Instantiate` 到 `contentAnchor` = 这个 `Rewards Content`）。
            // ⚠️ 没有里程碑数据 ⇒ **建出来、关着**（⛔ 不拿 prefab 里的示例值当真实奖励）。
            _container = MenuDraw.Node(_content, RewardContainerName, ContainerR);
            BuildRewardContainer(_container);
            _container.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ `Header`（= `RaidHeaderController`）
        void BuildHeader(Transform page)
        {
            _header = MenuDraw.Node(page, "Header", HeaderR);

            // `Title`（fs50 / base50 / auto[18,50] / Left-Midline / 折行）
            // ⚠️ 原版运行期被 `RaidHeaderController.Initialize` 的 `GetLabel(事件)` 覆盖 ⇒ 这里照 prefab 出厂原文。
            _title = MenuDraw.TextBox(_header, TitleR, PrefabTitleText, Color.white, "Title",
                                      50f, 18f, QCardText, 50f, 50f, face: TmpFont.Face.LatinUi);
            if (_title != null)
            {
                MenuDraw.AlignLeft(_title, TitleR);
                MenuDraw.SetVAlign(_title, Label.VAlign.Midline, TitleR);
            }

            // `Instructions Community`（`Localize` = `Event_Description/Raid/CollectiveScoringHelp`）
            _instructionsCommunity = MenuDraw.TextBox(_header, InstructionsCommunityR, Loc.T(TermInstructionsCommunity),
                                                       Color.white, "Instructions Community", 36f, 0f, QCardText,
                                                       36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_instructionsCommunity != null)
            {
                MenuDraw.AlignLeft(_instructionsCommunity, InstructionsCommunityR);
                MenuDraw.SetVAlign(_instructionsCommunity, Label.VAlign.Midline, InstructionsCommunityR);
            }

            // `Army Icon`（`Image` · `m_Sprite = null`，运行期 `LoadAsset(事件,5)` 灌图）
            _armyIcon = MenuDraw.Node(_header, "Army Icon", ArmyIconR);
            Note("`Army Icon` = liveops `LoadAsset(事件,5)` —— 本地没有 ⇒ 只建节点、不出图");

            // `Help Icon`（集体说明 ⓘ · 词条 + tooltip）
            BuildHelpTip(_header, "Help Icon", HelpIconR, QCardText, TermTipCommunity, TipAnchorCommunity,
                         Vector3.zero, "集体说明");

            // `Xp Points Icon`（**出厂 INACT**）+ `TotalXp Points`（数值 = 远端 ⇒ `--`）
            var xp = MenuDraw.Node(_header, "Xp Points Icon", XpPointsIconR);
            MenuDraw.Rect(xp, Tex(ArtForgePoints, "Xp 点数图标"), XpPointsIconR, "Image", QCardText, Color.white);
            _totalXp = MenuDraw.TextBox(xp, TotalXpPointsR, NoData, Color.white, "TotalXp Points",
                                        32.7f, 18f, QCardText, 32.7f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_totalXp != null)
            {
                MenuDraw.AlignLeft(_totalXp, TotalXpPointsR);
                MenuDraw.SetVAlign(_totalXp, Label.VAlign.Middle, TotalXpPointsR);
            }
            xp.gameObject.SetActive(false);      // prefab 出厂 INACT

            // `Time Remaining`（`Localize` = `Ends in:`；原版宽 0 + CSF ⇒ 用 300 宽的右对齐框，右缘 = 原版 pivot）
            var tr = MenuDraw.Node(_header, "Time Remaining", TimeRemainingR);
            _timeTitle = MenuDraw.TextBox(tr, TimeTitleR, Loc.T(TermEndsIn), Color.white, "Time Remaining",
                                          36f, 0f, QCardText, 36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_timeTitle != null)
            {
                MenuDraw.AlignRight(_timeTitle, TimeTitleR);
                MenuDraw.SetVAlign(_timeTitle, Label.VAlign.Midline, TimeTitleR);
            }
            // `Time`（`SupportMethods.GetTimeStringFromDate(事件日期)` ⇒ 本地没有日期 ⇒ `--`）
            _time = MenuDraw.TextBox(tr, TimeR, NoData, Color.white, "Time", 36f, 0f, QCardText,
                                     36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_time != null)
            {
                _time.SetWrapping(false);
                MenuDraw.AlignLeft(_time, TimeR);
                MenuDraw.SetVAlign(_time, Label.VAlign.Midline, TimeR);
                TimerDisplay.Attach(_time, null, "", "Raid/Header/Timer",
                                    "Raid 活动起止时刻在远端 LiveOps（`LiveOpsUtility.GetDateForEventStage` + 事件数据），本地没有 ⇒ `--`");
            }
            // `Help Grace Period`（宽限期说明 ⓘ；原版 `gracePeriodTooltip.SetActive(事件日期 < 服务器时间)`）
            BuildHelpTip(tr, "Help Grace Period", HelpGracePeriodR, QCardText, TermTipGracePeriod, TipAnchorGrace,
                         new Vector3(-10f, 0f, 0f), "宽限期");
        }

        // ------------------------------------------------------------ `Footer`
        void BuildFooter(Transform page)
        {
            _footer = MenuDraw.Node(page, "Footer", FooterR);

            BuildHelpTip(_footer, "Help Icon Player", HelpIconPlayerR, QCardText, TermTipIndividual,
                         TipAnchorIndividual, Vector3.zero, "个人说明");

            _instructionsPlayer = MenuDraw.TextBox(_footer, InstructionsPlayerR, Loc.T(TermInstructionsPlayer),
                                                   Color.white, "Instructions Player", 36f, 0f, QCardText,
                                                   36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_instructionsPlayer != null)
            {
                MenuDraw.AlignLeft(_instructionsPlayer, InstructionsPlayerR);
                MenuDraw.SetVAlign(_instructionsPlayer, Label.VAlign.Midline, InstructionsPlayerR);
            }
        }

        // ------------------------------------------------------------ `Debug Add points`（**出厂 INACT** · 调试件）
        void BuildDebugAddPoints(Transform page)
        {
            _debug = MenuDraw.Node(page, "Debug Add points", DebugAddPointsR);
            MenuDraw.Nine(_debug, Tex(ArtBtn, "调试按钮底"), DebugAddPointsR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QCardArt1, DebugBtnTint, true, "Image");
            var dt = MenuDraw.TextBox(_debug, DebugTextR, "Debug add points", Color.white, "Text (TMP)",
                                      24f, 18f, QCardText, 24f, 24f, face: TmpFont.Face.LatinCardBody);
            if (dt != null) MenuDraw.SetVAlign(dt, Label.VAlign.Middle, DebugTextR);
            // `InputField (TMP)`：输入框（出厂字 `5`）—— 调试用，照建、跟着父件一起关着。
            var inp = MenuDraw.Node(_debug, "InputField (TMP)", DebugInputR);
            MenuDraw.Rect(inp, CardArt.Solid(), DebugInputR, "Background", QCardArt1, new Color(0f, 0f, 0f, 0.5f));
            var it = MenuDraw.TextBox(inp, DebugInputR, "5", Color.white, "Text", 51.4f, 18f, QCardText,
                                      51.4f, 51.4f, face: TmpFont.Face.LatinCardBody);
            if (it != null) MenuDraw.SetVAlign(it, Label.VAlign.Middle, DebugInputR);
            _debug.gameObject.SetActive(false);  // prefab 出厂 INACT
        }

        // ============================================================ `Help Icon` 一族（三颗 ⓘ · 各自 anchor/pad）
        void BuildHelpTip(Transform parent, string name, PxRect iconR, int q, string term, int anchor,
                          Vector3 offset, string what)
        {
            MenuDraw.Rect(parent, Tex(ArtHelpInfo, what + "图标"), iconR, "Image", q, Color.white);
            // 原版 `Graphic.m_RaycastPadding = (-20,-20,-20,-20)` ⇒ 命中区每边**外扩 20**
            //（判据/先例 → `资料/待办判据_卡组与卡面.md` 的可点区域那条；外扩只走 `MenuDraw.PaddedHitRect`）。
            var hitR = MenuDraw.PaddedHitRect(iconR, HelpIconPad);
            var hit = MenuDraw.Hit(parent, name + " Hit", hitR, QHit, null);
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return;
            var center = new Vector2(iconR.CX, iconR.CY);
            wb.onEnter = () =>
            {
                var body = Loc.T(term);
                if (!string.IsNullOrEmpty(body))
                    Tooltip.Show(body, LayoutSpace.FromPixel(center.x, center.y), anchor, offset);
            };
            wb.onExit = Tooltip.Hide;
            // 原版这一件**不是按钮**（`EverguildTooltipTrigger` 只挂悬停，父链上没有点击处理器）
            // ⇒ 点它不发音（同 `ParentEventWindow` 那颗 P1 返回钮的 `playClickSound = false` 处置）。
            wb.playClickSound = false;
        }

        // ============================================================ `Raid Reward Container`（模板 · 出厂关着）
        void BuildRewardContainer(Transform root)
        {
            // `Collect Highlight Effect`（**出厂 INACT** · `Border Line FX` 九宫 50 · ppuMul 0.5 · `UIBorderGlow`）
            _collectHighlight = MenuDraw.Node(root, "Collect Highlight Effect", CollectHighlightR);
            MenuDraw.Nine(_collectHighlight, Tex(ArtGlowBorder, "领取高亮框"), CollectHighlightR,
                          new Vector4(50f, 50f, 50f, 50f), 128f, 128f, QCardArt, Color.white, true, "Image",
                          new Vector4(50f / 0.5f, 50f / 0.5f, 50f / 0.5f, 50f / 0.5f));
            _collectHighlight.gameObject.SetActive(false);

            // `RewardTransform`（`UITransformTweenController` + `RaidRewardDrawerController`）
            // —— 奖励物品由 `ItemDrawer` 运行时逐件实例化 ⇒ 只建空容器。
            MenuDraw.Node(root, "RewardTransform", RewardTransformR);
            Note("`RewardTransform`：原版 `RaidRewardDrawerController`（`ItemDrawer`）按 `RewardInfo` 逐件实例化"
                 + " —— 本地没有里程碑奖励数据 ⇒ 只建空容器");

            BuildCommunityProgression(root);
            BuildPersonalProgression(root);
        }

        void BuildCommunityProgression(Transform root)
        {
            // `Community Progression` → `Community Raid Progress Bar`（`EventProgressBarController`）
            var prog = MenuDraw.Node(root, "Community Progression", CommunityProgressionR);
            _communityBar = MenuDraw.Node(prog, "Community Raid Progress Bar", CommunityBarRootR);

            // `Bar`（`Slider` · 出厂 `m_MaxValue` 默认 · 填充 0 宽）：底 + 填充 + 端帽
            var bar = MenuDraw.Node(_communityBar, "Bar", CommunityBarR);
            MenuDraw.Rect(bar, Tex(ArtArmyTrackBar, "社区进度条底"), CommunityBarBgR, "ProgressionBG",
                          QCardArt, CommunityBarBgTint);
            var fill = MenuDraw.Node(bar, "ProgressionBar", CommunityBarFillR);   // 出厂 0 宽 = 进度 0
            MenuDraw.Rect(fill, Tex(ArtArmyTrackFill, "社区进度条填充"), CommunityBarFillR, "Image",
                          QBar, CommunityBarFillTint);
            MenuDraw.Rect(fill, Tex(ArtArmyTrackPos, "社区进度条端帽"), CommunityBarEndR, "BarEnd",
                          QBar, Color.white);

            BuildPoints(_communityBar, CommunityPointsR, CommunityPointsIconR, CommunityPointsTextR);

            // `Level`（`ProgressRewardLevelObject`：`40k_ArmyTrack_milestone_off` → 满足时换 `_on`）
            var lv = MenuDraw.Node(prog, "Level", CommunityLevelR);
            MenuDraw.Rect(lv, Tex(ArtMilestone, "里程碑底座"), CommunityLevelR, "Image", QCardArt, Color.white);
            var ll = MenuDraw.TextBox(lv, CommunityLevelLabelR, NoData, Color.white, "LevelLabel",
                                      45f, 18f, QCardText, 35f, 45f, face: TmpFont.Face.LatinCardBody);
            if (ll != null)
            {
                ll.SetWrapping(false);
                ll.SetCharSpacing(-3.5f);
                MenuDraw.SetVAlign(ll, Label.VAlign.Midline, CommunityLevelLabelR);
            }
        }

        void BuildPersonalProgression(Transform root)
        {
            var prog = MenuDraw.Node(root, "Personal Progression", PersonalProgressionR);
            _personalBar = MenuDraw.Node(prog, "Personal Raid Progress Bar", PersonalBarRootR);

            var bar = MenuDraw.Node(_personalBar, "Bar", PersonalBarR);
            MenuDraw.Rect(bar, Tex(ArtArmyTrackBar, "个人进度条底"), PersonalBarBgR, "ProgressionBG",
                          QCardArt, Color.white);
            var fill = MenuDraw.Node(bar, "ProgressionBar", PersonalBarFillR);   // 出厂 0 宽 = 进度 0
            MenuDraw.Rect(fill, Tex(ArtArmyTrackFill, "个人进度条填充"), PersonalBarFillR, "Image",
                          QBar, PersonalBarFillTint);
            MenuDraw.Rect(fill, Tex(ArtArmyTrackPos, "个人进度条端帽"), PersonalBarEndR, "BarEnd",
                          QBar, Color.white);

            BuildPoints(_personalBar, PersonalPointsR, PersonalPointsIconR, PersonalPointsTextR);

            BuildClaimButton(prog);
        }

        /// <summary>`Points`（`HorizontalLayoutGroup` + `CSFMinMax`）：`icon` + `text`。
        /// ⚠️ `icon` 在 prefab 里 **`Image` 组件被禁用**（运行时由 `EventProgressBarController.ScoreObject`
        /// 的 `ItemDrawer` 灌物品图）⇒ 只建节点、不出图。`text` 的出厂字 `300/400` 是示例值 ⇒ `--/--`。</summary>
        void BuildPoints(Transform barRoot, PxRect pointsR, PxRect iconR, PxRect textR)
        {
            var pts = MenuDraw.Node(barRoot, "Points", pointsR);
            MenuDraw.Node(pts, "icon", iconR);      // 运行时灌图（`Image` 组件在原版里本就是禁用的）
            var t = MenuDraw.TextBox(pts, textR, NoData + "/" + NoData, Color.white, "text",
                                     39.3f, 13.46f, QCardText, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (t != null)
            {
                t.SetWrapping(false);
                MenuDraw.SetVAlign(t, Label.VAlign.Capline, textR);
            }
        }

        /// <summary>`Claim Button Container`（`RaidProgressClaimButtonController`）。
        /// <para>四个 `stateDefinitions` 的显隐在文件头 ② 最后一段逐条列出；本件照 **prefab 出厂 active** 建，
        /// 没有里程碑状态 ⇒ 不跑状态机（⛔ 不假装某一档已解锁）。</para></summary>
        void BuildClaimButton(Transform personal)
        {
            var cbc = MenuDraw.Node(personal, "Claim Button Container", ClaimContainerR);

            // `Old Price Display`（划线原价 · `PriceDisplay`）：价格 = 远端 ⇒ `--`
            var old = MenuDraw.Node(cbc, "Old Price Display", OldPriceDisplayR);
            MenuDraw.Node(old, "icon", OldPriceIconR);       // 货币图标 = `PriceDisplay.icon`，运行时灌
            var oldTx = MenuDraw.TextBox(old, OldPriceTextR, NoData, Color.white, "text", 42.25f, 12f,
                                         QCardText, 54f, 39f, face: TmpFont.Face.LatinCardBody);
            if (oldTx != null) MenuDraw.SetVAlign(oldTx, Label.VAlign.Capline, OldPriceTextR);

            // `Collect Button` → `Generic UI Button`（`40K_button` 九宫 234,46 · tint 金）
            // ⚠️ prefab 的 `m_PixelsPerUnitMultiplier` 实读 = **0.01**（= Unity 对 0 的钳位值）。
            //    按「角块 = `m_Border ÷ ppuMul`」会算出 23400px 的角块 —— 但这颗钮只有 221.8×82，
            //    而 `CreateNineSlice` 照原版 `Image.GetAdjustedBorders` **逐轴按比例夹**，
            //    23400 与 234 夹出来的都是 **110.5 / 41** ⇒ **画出来逐位相同**
            //    （判据 → `Battle/ImageQuad.cs` 的 `scX/scY` 那两行）。⇒ 这里直接传 `m_Border` 原值。
            var cb = MenuDraw.Node(cbc, "Collect Button", CollectButtonR);
            var gu = MenuDraw.Node(cb, "Generic UI Button", CollectButtonR);
            MenuDraw.Nine(gu, Tex(ArtBtn, "领取按钮底"), CollectButtonR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QCardArt1, CollectBtnTint, true, "Image");
            // `Claimed Text`（`Localize` = `Rewards Menu/Claimed`）
            var claimed = MenuDraw.TextBox(gu, ClaimedTextR, Loc.T(TermClaimed), Color.white, "Claimed Text",
                                           41.1f, 12f, QCardText, 44f, 12f, face: TmpFont.Face.LatinCardBody);
            if (claimed != null) MenuDraw.SetVAlign(claimed, Label.VAlign.Midline, ClaimedTextR);
            // `Button Price Display`（折后价）：价格 = 远端 ⇒ `--`
            var bp = MenuDraw.Node(gu, "Button Price Display", ButtonPriceDisplayR);
            MenuDraw.Node(bp, "icon", ButtonPriceIconR);     // 货币图标 = 运行时灌
            var bpTx = MenuDraw.TextBox(bp, ButtonPriceTextR, NoData, Color.white, "text", 43.9f, 12f,
                                        QCardText, 54f, 39f, face: TmpFont.Face.LatinCardBody);
            if (bpTx != null) MenuDraw.SetVAlign(bpTx, Label.VAlign.Capline, ButtonPriceTextR);

            // 领奖 = `RaidEvent.OnRewardCollected`（服务器）⇒ 本地只出声
            MenuDraw.Hit(cb, "Hit", CollectButtonR, QHit, () =>
                Debug.LogWarning("[Raid] **领奖**需要服务器：原版 `RaidProgressClaimButtonController.CollectionClick` → "
                               + "`RaidEvent.OnRewardCollected`（LiveOps）；本地没有后端 ⇒ **不假装领取**。"),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // `Level`（`ProgressRewardLevelObject`）+ `Lock`（`WF Lock Icon` · 纯黑）
            var lv = MenuDraw.Node(cbc, "Level", ClaimLevelR);
            MenuDraw.Rect(lv, Tex(ArtMilestone, "里程碑底座（领取列）"), ClaimLevelR, "Image", QCardArt, Color.white);
            MenuDraw.Rect(lv, Tex(ArtLock, "锁图标"), ClaimLockR, "Lock", QCardArt1, LockTint);
        }

        // ============================================================ 坐标 / 取图助手
        void Note(string s)
        {
            if (_artNotes.Contains(s)) return;
            _artNotes.Add(s);
            Debug.Log("[Raid] " + s);
        }

        /// <summary>取图 —— **取不到必须出声**（红线；`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。</summary>
        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[Raid] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`）。");
            }
            return t;
        }
    }
}
