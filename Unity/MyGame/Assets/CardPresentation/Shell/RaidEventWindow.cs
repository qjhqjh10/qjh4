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
//          state 3 TotallyUnlocked   → ON `Lock` `Collect Button` `Button Price Display` **`Old Price Display`**
//          state 4 Collected         → ON `Lock` `Collect Button` `Claimed Text` `Old Price Display`
//        🔴 **2026-10-27（B14）就地订正（铁律 5）**：state 3 那一行原来**漏了 `Old Price Display`** ——
//          JSON 里 state 3 的第 5 个 target `isEnabled = 1`（同 state 4）。六个 PPtr → 节点名的对照、以及
//          逐档 `isEnabled` 全表 → `Shell/RaidEvent.cs` 的 `RaidClaimButtonLogic`（⛔ 判据只留那一份）。
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
// 🔴 **2026-10-27（B14）就地订正（铁律 5）—— 下面这段「一处有意偏离」已经不成立，原文留给追溯**：
//    原文：「原版 `RaidRewardSelector.CreateRewards` 是按里程碑逐件 Instantiate，里程碑为 0 时一件都不建。
//    本地恒无里程碑数据 ⇒ 本窗**只建模板 `Raid Reward Container`（关着）** + 数值 `--/--`」。
//    **现在**：`CreateRewards` 的视图侧**已照原版实现**（`RebuildRewards()`：先全 Destroy + 清空，再按
//    `Milestones` 逐档建一张；里程碑为 0 ⇒ **0 张**）。`Raid Reward Container` 那件当**母版**留着
//    （= 原版 `RaidRewardSelector.rewardItemContainer` 那个 prefab 引用），出厂关着、不参与显示。
//    ⇒ 本地数据下画面与从前**一致**（一张都没有），差别是「现在是因为里程碑为 0，而不是因为我们没实现」。
//    ⛔ 不拿 prefab 里的示例值（`300/400` / `99` / `154748` / `19d 15h`）当真实数据。
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
//   ⚠️ 母版**出厂就是关着的**；实例由 `RebuildRewards()` 按里程碑逐档建（无里程碑 ⇒ 0 张，见上「就地订正」）。
// · **`Points` 的 `icon`**：prefab 里 `Image` 组件是**禁用**的（`m_Enabled = 0`），运行时由
//   `EventProgressBarController.ScoreObject` 的 `ItemDrawer` 灌物品图 ⇒ 只建节点、不出图。
// · **`RewardTransform`**：`RaidRewardDrawerController`（`ItemDrawer`）运行时逐件实例化奖励 ⇒ 只建空容器。
// · **`Credit`（领取）**：原版 `RaidProgressClaimButtonController.CollectionClick` → `RaidEvent.OnRewardCollected`
//   （服务器）⇒ 本地走**等价模拟**（`PurchaseMilestone` 的解锁判据照抄 + 进程内「已领」+ 出声），⛔ 不假装领到真货。
using System;
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
        static readonly PxRect TimeTitleR = R(1338.425f, 6.03f, 1638.425f, 56.03f);   // 🔶 自己设计（非原版）：时间标题框 `TimeTitleR`（300 宽右对齐） · 原版宽 0 靠 CSF，我们没有运行时 CSF（原版判据拿不到） · TODO(谁定的)｜原注：← 我们挑的框（右缘 = 原版 pivot）
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
        /// 🔶 自己设计（非原版）：Raid 页签兜底词条 `Event_Description/General/Raid` · 原版取远端 `LiveOpsAssetUtility.GetLabel(事件)` · TODO(谁定的)｜原注：本条是 `Event_Description/General/Raid`（官方表里那条通用「Raid / 突袭」）—— **我们挑的兜底**，⛔ 不冒充原版。</summary>
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
                     + "（服务器）⇒ 本地走等价模拟 + 出声。";
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
            if (mgr == null) Debug.LogWarning("[Raid] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器（由调用方自己开；本方法**不再自开**）。");   // ⛔ B103_R1（AUD03 #120）：原来 `if (mgr != null) mgr.OpenWindow(win); else { …; win.Open(); }` 与唯一调用方 `WindowsManager.OpenByRef` 的 `:940` 撞成**首开双 Build**（`Raid.SyncLiveOps` 也跟着跑两遍）。原版 `Create` 不自开。
            // （原兜底 `win.Open()` 一并去掉：那条会先建一遍树，与 `OpenByRef` 那条路重复。）
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            SyncLiveOps();          // 🆕 2026-10-27（B14）：原版 `RaidEventTab.OnOpen` = base + `SetEventTab` 之后 `Initialize`
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
            Debug.LogWarning("[Raid] 关于奖励格：原版 `RaidRewardSelector.CreateRewards` 按 `RaidEvent.Milestones` "
                           + "逐件 Instantiate 奖励容器，里程碑为空时**一件都不建**；本件的验收口径 = "
                           + "**0 张** ⇒ 本窗同样 0 张（母版 `Raid Reward Container` 关着，不参与显示；见文件头「就地订正」）。");
            Debug.LogWarning("[Raid] 页签文字用 `" + TermTabTitle + "` 兜底（Raid / 突袭）：原版是 "
                           + "`LiveOpsAssetUtility.GetLabel(事件)`（远端）⇒ 本地拿不到；页签**图标**留空（liveops sprite 本地没有），"
                           + "⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。");
            Debug.LogWarning("[Raid] 页内 `Title` 照 prefab 出厂原文 `" + PrefabTitleText + "` 画 —— "
                           + "原版运行期被 `RaidHeaderController.Initialize` 的 `GetLabel(事件)` 覆盖，本地没有那份数据。");
            Debug.LogWarning("[Raid] 两张 liveops 图缺失：`Background Image`（`LoadAsset(事件,1)`）与 "
                           + "`Army Icon`（`LoadAsset(事件,5)`）⇒ 只建节点、不出图。"
                           + "⚠️ 原版在 `Background Image` 取不到 sprite 时也会 `set_enabled(false)`（`RaidEventTab.Initialize`）。");
            Debug.LogWarning("[Raid] 领奖（Collect）需要服务器：原版 `RaidProgressClaimButtonController.CollectionClick` → "
                           + "`RaidEvent.OnRewardCollected`（LiveOps）；本地没有后端 ⇒ 走等价模拟（解锁判据照抄 + 出声），⛔ 不假装领到真货。");
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
            // 🆕 B14：整棵树要重挂 ⇒ 奖励格句柄与档位信息一起清（旧节点已被 `MenuDraw.ClearChildren` 拆掉）
            _cards.Clear(); _cardInfos.Clear();
            _timeTitleNode = _graceNode = null;
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
            var master = BuildRewardContainer(_container);
            _container.gameObject.SetActive(false);
            // 自检读口保持指向**母版**（这三条是「结构照 prefab 建到了」的读数，⛔ 别让它指向某一张实例）。
            _collectHighlight = master.Highlight;
            _communityBar = master.CommunityBarRoot;
            _personalBar = master.PersonalBarRoot;

            // 🆕 2026-10-27（B14）：原版 `RaidEventTab.Initialize` 的第二跳 = `rewardSelector.CreateRewards()`
            //   ——**按里程碑逐件 Instantiate**（里程碑为空 ⇒ 一件都不建）。上面那件永远只当**母版**
            //   （= 原版 `RaidRewardSelector.rewardItemContainer` 那个 prefab 引用），实例在下面这一跳里建。
            if (RaidEventTabLogic.Initialize(true, _raid != null, false).RebuildRewards) RebuildRewards();
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
            _timeTitleNode = tr;                 // 🆕 B14：原版 `RaidHeaderController.timeTitle`（`UpdateText` 会整件关）
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
            _graceNode = BuildHelpTip(tr, "Help Grace Period", HelpGracePeriodR, QCardText, TermTipGracePeriod,
                                      TipAnchorGrace, new Vector3(-10f, 0f, 0f), "宽限期");

            // 🆕 2026-10-27（B14）：原版 `RaidHeaderController.Initialize` 的最后一跳 `UpdateText()`
            //   （`__UpdateText.c` —— **整段被「事件不为空」包着** ⇒ 没有事件时一个字段都不写，保持 prefab 出厂态）。
            if (RaidHeaderControllerLogic.Initialize(_raid != null).WritesText) ApplyHeaderText();
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
        /// <summary>建一颗 ⓘ。🆕 **返回那颗图标节点**（B14：原版 `gracePeriodTooltip` 是 `RaidHeaderController`
        /// 的一个 `GameObject` 字段，`UpdateText` 会对它 `SetActive` ⇒ 我们必须拿得到它）。</summary>
        Transform BuildHelpTip(Transform parent, string name, PxRect iconR, int q, string term, int anchor,
                               Vector3 offset, string what)
        {
            var icon = MenuDraw.Rect(parent, Tex(ArtHelpInfo, what + "图标"), iconR, "Image", q, Color.white);
            var iconNode = icon != null ? icon.transform : null;
            // 原版 `Graphic.m_RaycastPadding = (-20,-20,-20,-20)` ⇒ 命中区每边**外扩 20**
            //（判据/先例 → `资料/待办判据_卡组与卡面.md` 的可点区域那条；外扩只走 `MenuDraw.PaddedHitRect`）。
            var hitR = MenuDraw.PaddedHitRect(iconR, HelpIconPad);
            var hit = MenuDraw.Hit(parent, name + " Hit", hitR, QHit, null);
            if (hit == null) return iconNode;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return iconNode;
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
            return iconNode;
        }

        // ============================================================ `Raid Reward Container`（母版 + 每档一张）
        /// <summary>建一整张 `Raid Reward Container`，把**后续要改的节点句柄**还回去。
        /// <para>母版（`_container`，出厂关着 = 原版 `RaidRewardSelector.rewardItemContainer` 那个 prefab 引用）与
        /// 每一张实例**走同一份代码** —— 判据只留一份（CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</para>
        /// <para>🔴 节点名 / 矩形 / 渲染队列**一个都没改**（本批只加句柄与状态落点）。</para></summary>
        RewardCard BuildRewardContainer(Transform root)
        {
            var card = new RewardCard { Root = root };

            // `Collect Highlight Effect`（**出厂 INACT** · `Border Line FX` 九宫 50 · ppuMul 0.5 · `UIBorderGlow`）
            card.Highlight = MenuDraw.Node(root, "Collect Highlight Effect", CollectHighlightR);
            MenuDraw.Nine(card.Highlight, Tex(ArtGlowBorder, "领取高亮框"), CollectHighlightR,
                          new Vector4(50f, 50f, 50f, 50f), 128f, 128f, QCardArt, Color.white, true, "Image",
                          new Vector4(50f / 0.5f, 50f / 0.5f, 50f / 0.5f, 50f / 0.5f));
            card.Highlight.gameObject.SetActive(false);

            // `RewardTransform`（`UITransformTweenController` + `RaidRewardDrawerController`）
            // —— 奖励物品由 `ItemDrawer` 运行时逐件实例化 ⇒ 只建空容器。
            card.RewardTransform = MenuDraw.Node(root, "RewardTransform", RewardTransformR);
            Note("`RewardTransform`：原版 `RaidRewardDrawerController`（`ItemDrawer`）按 `RewardInfo` 逐件实例化"
                 + " —— 本地没有里程碑奖励数据 ⇒ 只建空容器");

            BuildCommunityProgression(root, card);
            BuildPersonalProgression(root, card);
            return card;
        }

        void BuildCommunityProgression(Transform root, RewardCard card)
        {
            // `Community Progression` → `Community Raid Progress Bar`（`EventProgressBarController`）
            var prog = MenuDraw.Node(root, "Community Progression", CommunityProgressionR);
            var barRoot = MenuDraw.Node(prog, "Community Raid Progress Bar", CommunityBarRootR);
            card.CommunityBarRoot = barRoot;

            // `Bar`（`Slider` · 出厂 `m_MaxValue` 默认 · 填充 0 宽）：底 + 填充 + 端帽
            var bar = MenuDraw.Node(barRoot, "Bar", CommunityBarR);
            MenuDraw.Rect(bar, Tex(ArtArmyTrackBar, "社区进度条底"), CommunityBarBgR, "ProgressionBG",
                          QCardArt, CommunityBarBgTint);
            var fill = MenuDraw.Node(bar, "ProgressionBar", CommunityBarFillR);   // 出厂 0 宽 = 进度 0
            card.CommunityFill = fill;
            card.CommunityFillTrack = CommunityBarR;
            var cap = DrawBarFill(fill, CommunityBarR, CommunityBarFillTint, 0f, ArtArmyTrackFill, ArtArmyTrackPos,
                                  CommunityBarEndR, "社区");

            // `Points`（分数牌：`{当前}/{下一档}`）
            var pts = BuildPoints(barRoot, CommunityPointsR, CommunityPointsIconR, CommunityPointsTextR);
            card.CommunityPoints = pts.Label;

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
            card.CommunityLevel = ll;

            // 🆕 B14：**直接用 U04 的 `EventProgressBarController`**（⛔ 不另写一套进度条）
            card.CommunityBar = new EventProgressBarController
            {
                Root = barRoot.gameObject,
                BarSetter = p => { card.CommunityBar.TipSprite = DrawBarFill(fill, CommunityBarR, CommunityBarFillTint,
                                                                             p, ArtArmyTrackFill, ArtArmyTrackPos,
                                                                             CommunityBarEndR, "社区"); },
                TipSprite = cap,
                ScoreObject = new EventProgressBarScoreObject
                {
                    ScoreText = pts.Label,
                    IconHolder = pts.Icon,
                    OnInitialize = (info, itemId, isLast) => ApplyScoreObject(pts, info, isLast),
                },
            };
        }

        void BuildPersonalProgression(Transform root, RewardCard card)
        {
            var prog = MenuDraw.Node(root, "Personal Progression", PersonalProgressionR);
            var barRoot = MenuDraw.Node(prog, "Personal Raid Progress Bar", RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(PersonalBarRootR, RaidRewardState.Locked, RaidBarPlacementLogic.PositionOffsetCurrent, RaidBarPlacementLogic.ScaleOffsetCurrent));
            card.PersonalBarRoot = barRoot;

            var bar = MenuDraw.Node(barRoot, "Bar", PersonalBarR);
            MenuDraw.Rect(bar, Tex(ArtArmyTrackBar, "个人进度条底"), PersonalBarBgR, "ProgressionBG",
                          QCardArt, Color.white);
            var fill = MenuDraw.Node(bar, "ProgressionBar", PersonalBarFillR);   // 出厂 0 宽 = 进度 0
            card.PersonalFill = fill;
            card.PersonalFillTrack = PersonalBarR;
            var cap = DrawBarFill(fill, PersonalBarR, PersonalBarFillTint, 0f, ArtArmyTrackFill, ArtArmyTrackPos,
                                  PersonalBarEndR, "个人");

            var pts = BuildPoints(barRoot, PersonalPointsR, PersonalPointsIconR, PersonalPointsTextR);
            card.PersonalPoints = pts.Label;

            card.PersonalBar = new EventProgressBarController
            {
                Root = barRoot.gameObject,
                BarSetter = p => { card.PersonalBar.TipSprite = DrawBarFill(fill, PersonalBarR, PersonalBarFillTint,
                                                                           p, ArtArmyTrackFill, ArtArmyTrackPos,
                                                                           PersonalBarEndR, "个人"); },
                TipSprite = cap,
                ScoreObject = new EventProgressBarScoreObject
                {
                    ScoreText = pts.Label,
                    IconHolder = pts.Icon,
                    OnInitialize = (info, itemId, isLast) => ApplyScoreObject(pts, info, isLast),
                },
            };

            BuildClaimButton(prog, card);
        }

        /// <summary>`Points` 那一块的三个句柄（`Label` / `icon` 节点 / 父节点）。</summary>
        struct PointsRefs { public Label Label; public Transform Icon; public Transform Root; }

        /// <summary>`Points`（`HorizontalLayoutGroup` + `CSFMinMax`）：`icon` + `text`。
        /// ⚠️ `icon` 在 prefab 里 **`Image` 组件被禁用**（运行时由 `EventProgressBarController.ScoreObject`
        /// 的 `ItemDrawer` 灌物品图）⇒ 只建节点、不出图。`text` 的出厂字 `300/400` 是示例值 ⇒ 出厂画 `--/--`。</summary>
        PointsRefs BuildPoints(Transform barRoot, PxRect pointsR, PxRect iconR, PxRect textR)
        {
            var pts = MenuDraw.Node(barRoot, "Points", pointsR);
            var icon = MenuDraw.Node(pts, "icon", iconR);   // 运行时灌图（`Image` 组件在原版里本就是禁用的）
            var t = MenuDraw.TextBox(pts, textR, NoData + "/" + NoData, Color.white, "text",
                                     39.3f, 13.46f, QCardText, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (t != null)
            {
                t.SetWrapping(false);
                MenuDraw.SetVAlign(t, Label.VAlign.Capline, textR);
            }
            return new PointsRefs { Label = t, Icon = icon, Root = pts };
        }

        /// <summary>`Claim Button Container`（`RaidProgressClaimButtonController`）。
        /// <para>6 档 `stateDefinitions` = `RaidClaimButtonLogic.BuildDefinitions(...)`（逐格抄自 prefab MB 实读）；
        /// 本件照 **prefab 出厂 active** 建，状态由 <see cref="ApplyCardState"/> 落。
        /// ⛔ 没有里程碑数据时**一次都不应用**（= 原版 `Initialize` 根本没被调）。</para></summary>
        void BuildClaimButton(Transform personal, RewardCard card)
        {
            var cbc = MenuDraw.Node(personal, "Claim Button Container", ClaimContainerR);

            // `Old Price Display`（划线原价 · `PriceDisplay`）：价格 = 远端 ⇒ 出厂 `--`
            var old = MenuDraw.Node(cbc, "Old Price Display", OldPriceDisplayR);
            card.StateNodes["Old Price Display"] = old;
            MenuDraw.Node(old, "icon", OldPriceIconR);       // 货币图标 = `PriceDisplay.icon`，运行时灌
            var oldTx = MenuDraw.TextBox(old, OldPriceTextR, NoData, Color.white, "text", 42.25f, 12f,
                                         QCardText, 54f, 39f, face: TmpFont.Face.LatinCardBody);
            if (oldTx != null) MenuDraw.SetVAlign(oldTx, Label.VAlign.Capline, OldPriceTextR);
            card.OldPriceText = oldTx;

            // `Collect Button` → `Generic UI Button`（`40K_button` 九宫 234,46 · tint 金）
            // ⚠️ prefab 的 `m_PixelsPerUnitMultiplier` 实读 = **0.01**（= Unity 对 0 的钳位值）。
            //    按「角块 = `m_Border ÷ ppuMul`」会算出 23400px 的角块 —— 但这颗钮只有 221.8×82，
            //    而 `CreateNineSlice` 照原版 `Image.GetAdjustedBorders` **逐轴按比例夹**，
            //    23400 与 234 夹出来的都是 **110.5 / 41** ⇒ **画出来逐位相同**
            //    （判据 → `Battle/ImageQuad.cs` 的 `scX/scY` 那两行）。⇒ 这里直接传 `m_Border` 原值。
            var cb = MenuDraw.Node(cbc, "Collect Button", CollectButtonR);
            card.StateNodes["Collect Button"] = cb;
            card.CollectButton = cb;
            var gu = MenuDraw.Node(cb, "Generic UI Button", CollectButtonR);
            MenuDraw.Nine(gu, Tex(ArtBtn, "领取按钮底"), CollectButtonR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QCardArt1, CollectBtnTint, true, "Image");
            // `Claimed Text`（`Localize` = `Rewards Menu/Claimed`）
            var claimed = MenuDraw.TextBox(gu, ClaimedTextR, Loc.T(TermClaimed), Color.white, "Claimed Text",
                                           41.1f, 12f, QCardText, 44f, 12f, face: TmpFont.Face.LatinCardBody);
            if (claimed != null) MenuDraw.SetVAlign(claimed, Label.VAlign.Midline, ClaimedTextR);
            card.StateNodes["Claimed Text"] = claimed != null ? claimed.transform : null;
            // `Button Price Display`（折后价）：价格 = 远端 ⇒ 出厂 `--`
            var bp = MenuDraw.Node(gu, "Button Price Display", ButtonPriceDisplayR);
            card.StateNodes["Button Price Display"] = bp;
            MenuDraw.Node(bp, "icon", ButtonPriceIconR);     // 货币图标 = 运行时灌
            var bpTx = MenuDraw.TextBox(bp, ButtonPriceTextR, NoData, Color.white, "text", 43.9f, 12f,
                                        QCardText, 54f, 39f, face: TmpFont.Face.LatinCardBody);
            if (bpTx != null) MenuDraw.SetVAlign(bpTx, Label.VAlign.Capline, ButtonPriceTextR);
            card.ButtonPriceText = bpTx;
            // ⚠️ 缺口（已记账）：`PriceDisplay.useStrikeThrough`（prefab 实读：`Old Price Display` 那条**是划线价**）
            //    我们的 `Label` 没有划线渲染（`TMP.fontStyle.Strikethrough` 没有接出口）⇒ 本件画不出那条横线。
            //    ⛔ 不拿「换颜色」冒充划线（视觉不等价）。

            // 🆕 B14：领奖点击 = 原版 `RaidProgressClaimButtonController.CollectionClick`
            //   → 基类 `onCollect`（= `RaidEventRewardItemContainer.OnCollect`）
            //   → `RaidRewardSelector.CollectRewardClicked(档号)` → `RaidEvent.PurchaseMilestone(...)`。
            var clickCard = card;
            var hit = MenuDraw.Hit(cb, "Hit", CollectButtonR, QHit,
                                   () => {
                                       // 原版 `RaidProgressClaimButtonController.CollectionClick`：
                                       // 先走基类 `onCollect`（= `RaidEventRewardItemContainer.OnCollect`），再**关钮**。
                                       if (clickCard.Claim != null) clickCard.Claim.CollectionClick();
                                       else OnCollect(clickCard);
                                       // ⚠️ 本地 `PurchaseMilestone` 是**同步**的 ⇒ 上面那一跳可能已经把整批卡重建掉
                                       //    （原版是异步：回调晚于这一句）⇒ 只在「这张卡还活着」时关钮，否则会
                                       //    `MissingReferenceException`。
                                       if (clickCard.CollectButtonButton != null && _cards.Contains(clickCard))
                                           clickCard.CollectButtonButton.Interactable =
                                               RaidClaimButtonLogic.ToggleCollectButtonInteractable(false);
                                   },
                                   null, null, null, null, null, clickCue: WindowSounds.PositiveCue);
            card.CollectButtonButton = hit != null ? hit.GetComponent<WindowButton>() : null;

            // `Level`（`ProgressRewardLevelObject`）+ `Lock`（`WF Lock Icon` · 纯黑）
            var lv = MenuDraw.Node(cbc, "Level", ClaimLevelR);
            card.StateNodes["Level"] = lv;
            MenuDraw.Rect(lv, Tex(ArtMilestone, "里程碑底座（领取列）"), ClaimLevelR, "Image", QCardArt, Color.white);
            var lockNode = MenuDraw.Rect(lv, Tex(ArtLock, "锁图标"), ClaimLockR, "Lock", QCardArt1, LockTint);
            card.StateNodes["Lock"] = lockNode != null ? lockNode.transform : null;

            // 🆕 B14：六档 stateDefinitions + 基类控制器（U04 那一份；⛔ 不在这里重写 `ApplyState`）。
            //   `onCollect` 由 `Initialize(...)` 那一个形参传（原版就这一个入口）。
            card.Claim = new BaseEventProgressClaimButtonController<RaidRewardState>
            {
                stateDefinitions = RaidClaimButtonLogic.BuildDefinitions(name =>
                {
                    Transform n;
                    return (card.StateNodes.TryGetValue(name, out n) && n != null) ? n.gameObject : null;
                }),
            };
        }

        // ============================================================ 进度条填充（`EventProgressBarController.SetBarFill` 的视图侧）
        /// <summary>把一条进度条画到 `p`（= 原版 `Slider.value = p`）。
        /// <para>🔴 判据 = `EventProgressBarController__SetBarFill.c`：`slider.value = p` +
        /// `tipSprite.enabled = 0 &lt; p &amp;&amp; p &lt; 1`（<see cref="EventProgressBarController.TipSpriteMaxRatio"/>，U04 那一份）。
        /// ⚠️ 我们的 `Slider` 是**几个绝对 px 的 quad**（没有 uGUI 的 FillRect 归一化）⇒ 按 `barR` 的宽度乘 `p`
        /// 重画一块（先例 = `Shell/EnergySinglePlayerOnlyEventWindow.cs` 的 `DrawFill`，同一个口径）。
        /// ⚠️ **端帽**按 prefab 出厂矩形摆、显隐交给 U04 的 `EventProgressBarController.SetBarFill`
        /// （`tipSprite.enabled = 0 &lt; p &amp;&amp; p &lt; 1`，常数 `TipSpriteMaxRatio`）；⛔ 这里不自己判，
        /// 只**把新建的那块端帽交回去**（`BarSetter` 里赋给 `TipSprite`）。
        /// ⚠️ 端帽**不随进度平移** —— 原版 uGUI 里它挂在 `Fill` 下会随 `sizeDelta` 走，本壳没有那套布局（已记账）。</para></summary>
        ImageQuad DrawBarFill(Transform fill, PxRect barR, Color tint, float p,
                              string fillArt, string capArt, PxRect capR, string what)
        {
            if (fill == null) return null;
            MenuDraw.ClearChildren(fill);
            float w = barR.W * Mathf.Clamp01(p);
            if (w > 0.01f)
                MenuDraw.Rect(fill, Tex(fillArt, what + "进度条填充"),
                              new PxRect(barR.x1, barR.y1, barR.x1 + w, barR.y2), "Image", QBar, tint);
            return MenuDraw.Rect(fill, Tex(capArt, what + "进度条端帽"), capR, "BarEnd", QBar, Color.white);
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

        // ============================================================================================
        //  🆕 2026-10-27（活动线 `B14` · 执行代理 C3）：**事件实例 + 逐档状态**这一层
        //  --------------------------------------------------------------------------------------------
        //  原版对位：`RaidEventTab`(+0x48 `LiveOps` / +0x50 `raidEvent`) · `RaidRewardSelector`
        //  (`Initialize` / `RefreshData` / `Reset` / `CreateRewards` 的**视图侧**) ·
        //  `RaidEventRewardItemContainer.Initialize`（一档 → 状态 → 六个节点 + 两条进度条 + 奖励 + 高亮）。
        //  纯算式**一份都不在这里**：全在 `Shell/RaidEvent.cs`（`RaidScoreObjectLogic` /
        //  `RaidRewardContainerLogic` / `RaidClaimButtonLogic` / `RaidBarPlacementLogic` / `RaidHeaderLogic`），
        //  进度条本体与领奖钮基类**用 U04 的共用件**（`EventProgressBarController` / `BaseEventProgressClaimButtonController`）。
        //  🔴 **本段全部追加在类尾**（⛔ 不插进已有成员之间 —— 账里本窗有 6 处 A 行落点，插行会让它们整体漂移）。
        // ============================================================================================

        // ---- 本段自己的字段（同样追加在类尾；C# 允许「先用后声明」）
        ILiveOps _liveOps;                       // 原版 `RaidEventTab.LiveOps`(+0x48)
        RaidEvent _raid;                         // 原版 `RaidEventTab.raidEvent`(+0x50)
        int _eventStage;                         // 原版 `UpdateText` 读的「阶段」（来源未查清，见 `RaidHeaderLogic.UpdateText`）
        readonly List<RewardCard> _cards = new List<RewardCard>();                              // `currentRewardItemContainers`
        readonly List<RaidRewardSelectorLogic.RaidEventMilestoneInfo> _cardInfos =
            new List<RaidRewardSelectorLogic.RaidEventMilestoneInfo>();                          // `milestoneInfos`
        Transform _timeTitleNode, _graceNode;     // `RaidHeaderController.timeTitle` / `.gracePeriodTooltip`
        bool _noRaidEventWarned, _noMilestoneWarned, _noRewardArtWarned, _layoutWarned, _maskWarned;

        /// <summary>原版 `Event_Description/General/TimeStartsIn`（= `Starts in:`）——
        /// `RaidHeaderController.UpdateText` 在「日期还没到」那一支用的词条
        /// （文件头 ④ 已核对它在官方表里：`TimeEndsIn` 与它成对）。</summary>
        public const string TermStartsIn = "Event_Description/General/TimeStartsIn";

        /// <summary>一张 `Raid Reward Container` 的**句柄**（原版那个 MonoBehaviour 实例的字段投影）。
        /// <para>只留「状态机会改到的」那些节点，别的（`RewardTransform` 等）建完就够。</para></summary>
        public sealed class RewardCard
        {
            public Transform Root;
            public Transform Highlight;                 // `Collect Highlight Effect`（出厂 INACT）
            public Transform RewardTransform;           // `ItemDrawer` 运行时逐件实例化 ⇒ 空容器
            public Transform CommunityBarRoot, PersonalBarRoot;
            public Transform CommunityFill, PersonalFill;
            public PxRect CommunityFillTrack, PersonalFillTrack;
            public Transform CommunityEndCap, PersonalEndCap;
            public Label CommunityPoints, PersonalPoints, CommunityLevel;
            /// <summary>两条进度条的**共用件**（U04 的 `EventProgressBarController`，判据只留那一份）。</summary>
            public EventProgressBarController CommunityBar, PersonalBar;
            /// <summary>领奖钮（U04 的 `BaseEventProgressClaimButtonController`，六档表由 `RaidClaimButtonLogic` 给）。</summary>
            public BaseEventProgressClaimButtonController<RaidRewardState> Claim;
            public Transform CollectButton;
            /// <summary>`Collect Button` 上那颗钮（原版 `PriceDisplayButton`；`Interactable` 那两跳改的就是它）。</summary>
            public WindowButton CollectButtonButton;
            public Label OldPriceText, ButtonPriceText;
            /// <summary>6 个 `stateDefinitions` 的 target（节点名 = 原版 target 名，见 `RaidClaimButtonLogic`）。</summary>
            public readonly Dictionary<string, Transform> StateNodes = new Dictionary<string, Transform>();
            /// <summary>这一张已经落到哪一档（`null` = 还没应用过任何档 = 原版 `Initialize` 没被调）。</summary>
            public RaidRewardState? Applied;
            /// <summary>`stateDefinitions[...].enableLevelHighlight`（原版 `ProgressRewardLevelObject.ToggleHighlight`）。</summary>
            public bool LevelHighlight;
            /// <summary>`stateDefinitions[...].forceInteractableState`（原版 `priceDisplayButton.Interactable = false`，**只关不开**）。</summary>
            public bool ForceInteractableFalse;
            public RaidRewardSelectorLogic.RaidEventMilestoneInfo Info;
        }

        /// <summary>当前事件（原版 `RaidEventTab.LiveOps`，`ILiveOps`）。生产路径恒 `null`（本地没有 Raid 事件 SO）。</summary>
        public ILiveOps LiveOps { get { return _liveOps; } }
        /// <summary>当前事件转成 `RaidEvent` 的那一份（原版 `RaidEventTab.raidEvent`，类型不对 ⇒ null）。</summary>
        public RaidEvent RaidEventInstance { get { return _raid; } }

        /// <summary>原版 `RaidEventTab.SetEventTab(ILiveOps liveOps)`（`RaidEventTab__SetEventTab.c`，**逐句**）：
        /// <code>
        /// LiveOps = liveOps;
        /// raidEvent = liveOps as RaidEvent;      // 类型不对 ⇒ null（不是异常）
        /// </code>
        /// <para>⚠️ 本工程**一窗一类硬绑、没有事件参数入口**（账里那一行的差异列就是这句）⇒ 生产路径由
        /// <see cref="SyncLiveOps"/> 传本地活动（今天恒 null）；自检/将来接 LiveOps 时直接调本方法。</para></summary>
        public void SetEventTab(ILiveOps liveOps)
        {
            _liveOps = liveOps;
            _raid = liveOps as RaidEvent;
        }

        /// <summary>开窗时这一步（原版在 `BaseParentEventWindow.CreateEventTab` 里把事件传进来）。
        /// <para>本地 13 份活动 SO 里**一条 Raid 都没有**（全是过期的 Expansion Pass）⇒ 恒 `SetEventTab(null)`，
        /// **出声一次**（红线：不许静默）。真数据到了只改这一个方法。</para></summary>
        void SyncLiveOps()
        {
            SetEventTab(null);
            if (!_noRaidEventWarned)
            {
                _noRaidEventWarned = true;
                Debug.LogWarning("[Raid] 没有可用的突袭事件实例：原版 `BaseParentEventWindow.CreateEventTab` 按**远端 liveops 列表**"
                               + "把事件传给 `RaidEventTab.SetEventTab`；本地 13 份活动 SO 全是过期的 Expansion Pass、"
                               + "没有 Raid ⇒ `SetEventTab(null)`。⇒ 奖励格 **0 张**（= 原版「里程碑为空就一件不建」）、"
                               + "页头保持 prefab 出厂态（原版 `UpdateText` 整段被「事件不为空」包着）。");
            }
        }

        /// <summary>原版 `RaidEventTab.SetEventTab` 之后 `Initialize` 用的「阶段」实参（⚠️ 来源未查清，
        /// 见 `RaidHeaderLogic.UpdateText` 的说明：原版读 `(事件 as ???)+0x24`，cpp2il 丢了类型身份）。
        /// <para>默认 **0** ⇒ 原版「阶段 0 ⇒ 藏 `timeTitle` + 藏宽限期 ⓘ」那一支；没有事件时这一支**不执行**。</para></summary>
        public int EventStage { get { return _eventStage; } set { _eventStage = value; ApplyHeaderText(); } }

        /// <summary>本窗当前建出来的奖励格（原版 `RaidRewardSelector.currentRewardItemContainers`）。</summary>
        public List<RewardCard> RewardCards { get { return _cards; } }
        /// <summary>每张格子的档位信息（原版 `RaidRewardSelector.milestoneInfos`）。</summary>
        public List<RaidRewardSelectorLogic.RaidEventMilestoneInfo> MilestoneInfos { get { return _cardInfos; } }
        /// <summary>奖励格张数（**自检读口**：原版 `CreateRewards` 的循环长度 = 里程碑数）。</summary>
        public int RewardCardCount { get { return _cards.Count; } }

        /// <summary>自检/将来接 LiveOps 的入口：换一份事件并**整批重建**（= 原版 `RefreshData` → `CreateRewards`）。
        /// <para>⛔ 生产路径**不调它**（真数据只能来自 LiveOps 工厂）。</para></summary>
        public void SetEventForTest(ILiveOps liveOps)
        {
            SetEventTab(liveOps);
            ApplyHeaderText();
            RebuildRewards();
        }

        /// <summary>原版 `RaidRewardSelector.RefreshData()` = `CreateRewards()` 的**视图侧**。
        /// <para>判据（`RaidRewardSelector__CreateRewards.c`）：
        /// ① `currentRewardItemContainers.Count &gt;= 1` ⇒ 先 `LogWarning(…)` + **全 Destroy + 清空**；
        /// ② 按 `Milestones` 逐档 `Instantiate` → `Add` → `CreateMilestoneInfo` → `container.Initialize(...)`；
        /// ③ 里程碑为空 ⇒ **一件都不建**（循环长度 0，⛔ 不是「建一张然后关掉」）。</para>
        /// <para>⚠️ 「全 Destroy 前先 LogWarning」那一声是原版的：`CustomDebug.LogWarning(字面量没解出)` ⇒
        /// 我们出声但**不编原文**。</para></summary>
        public void RebuildRewards()
        {
            if (_content == null) return;      // 还没建树（`Build` 之前调）⇒ 什么都不做
            ResetRewards();

            var infos = RaidRewardSelectorLogic.CreateMilestoneInfos(_raid);
            for (int i = 0; i < infos.Count; i++)
            {
                var info = infos[i];
                var root = MenuDraw.Node(_content, RewardContainerName + " " + i, ContainerR);
                // ⚠️ 落位：原版是 `Rewards Content` 的 HLG（padding.left 122 · spacing -130 · MiddleLeft）逐件排；
                //     本壳没有 HLG 运行时 ⇒ 按同一算式**手工推**第 i 张的 x（⛔ 不是「都叠在 0」）。
                float x = ContentR.x1 + 122f + i * (ContainerW - 130f);
                MenuDraw.ApplyPxRect(root, _content, new PxRect(x, ContainerR.y1, x + ContainerW, ContainerR.y2));
                var card = BuildRewardContainer(root);
                card.Info = info;
                ApplyCardState(card, info, i == infos.Count - 1);
                _cards.Add(card);
                _cardInfos.Add(info);
            }
            if (infos.Count == 0 && !_noMilestoneWarned)
            {
                _noMilestoneWarned = true;
                Debug.Log("[Raid] 里程碑 0 档 ⇒ **一张奖励格都不建**（原版 `CreateRewards` 的循环长度就是 0）。"
                        + "本地没有 Raid 里程碑数据（远端 LiveOps）⇒ 该空态是**原版的空态**，不是我们少做。");
            }
        }

        /// <summary>原版 `RaidRewardSelector.Reset()`（`RaidRewardSelector__Reset.c`，**逐句**）：
        /// `CustomDebug.LogWarning(字面量没解出)` → 把 `currentRewardItemContainers` 里**逐件 Destroy**
        /// → **清空列表**（`List.Clear()` 那三句：size 置 0、version++、`Array.Clear`）。
        /// <para>⚠️ 那一声 `LogWarning` 的**原文没解出**（`DAT_1842add48` 是字符串对象）⇒ 我们出一声但**不编原文**。</para></summary>
        public void ResetRewards()
        {
            if (_cards.Count >= 1)
                Debug.Log("[Raid] 重置奖励格：销毁已有的 " + _cards.Count + " 张（原版 `RaidRewardSelector.Reset` 会先 LogWarning，原文没解出）");
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null && _cards[i].Root != null) DestroyNode(_cards[i].Root.gameObject);
            _cards.Clear();
            _cardInfos.Clear();
        }

        /// <summary>原版 `RaidEventTab.RefreshTestEditorOnly()`（`RaidEventTab__RefreshTestEditorOnly.c`）：
        /// 编辑器上的 `[Button]`，体内 = `Reset()` + `Initialize()` 重跑一遍（正式版**不调用**）。
        /// <para>我们的等价：`ResetRewards()`（= Reset 的视图侧）+ `RebuildRewards()`（= Initialize 的视图侧）。
        /// 只给自检/编辑器用。</para></summary>
        public void RefreshTestEditorOnly()
        {
            ResetRewards();
            ApplyHeaderText();
            RebuildRewards();
        }


        /// <summary>原版 `RaidEventRewardItemContainer.Initialize(liveOps, selector, current, previous, isLast)`
        /// 的**视图侧**（`RaidEventRewardItemContainer__Initialize.c`）—— 落地顺序逐跳：
        /// <code>
        /// state = GetState(current.Collected, current.PlayerScoringInfo, current.CommunityScoringInfo)
        /// item  = liveOps.ScoringItem ?? (current.NormalPrice 若是货币 ⇒ 那个货币)        // 本地 = 里程碑的奖章 id
        /// ① personalScaleController.Initialize(state, prevState, current.Level)
        /// ② personalBar.Initialize(current.PlayerScoringInfo, item, isLast); mask.enabled = (Level == 0)
        /// ③ claimButton.Initialize(state, current.NormalPrice, onCollect, current.DiscountedPrice)
        /// ④ communityBar.Initialize(current.CommunityScoringInfo, item, isLast); mask.enabled = (Level == 0)
        /// ⑤ communityLevelObject.Initialize(current.Level + 1)
        /// ⑥ drawer.ChangeState(state) / 逐件画奖励
        /// ⑦ highlightReward.SetActive(false)
        /// </code>
        /// <para>⚠️ 本地没有 `ItemDrawer`（奖励物品图）⇒ ⑥ 只做 `RewardTransform` 上的**空容器** + 出声（缺口已记账）。</para></summary>
        void ApplyCardState(RewardCard card, RaidRewardSelectorLogic.RaidEventMilestoneInfo info, bool isLastMilestone)
        {
            if (card == null || info == null) return;
            var state = RaidRewardContainerLogic.GetState(info);
            card.Applied = state;

            // ---- ① 个人进度条：按档改位置/尺寸（原版 `BaseProgressBarScaleAndPositionController.Initialize`）
            //      ⚠️ 我们不做「按档位移」（本地无数据、也不改版面 —— 见 `RaidBarPlacementLogic` 的说明）；
            //      仍按原版的**真值**把规则算出来写进口志，免得将来接数据时再猜一次。
            bool applyPrev, applyCur; RaidBarPlacementLogic.PlanSteps(info.Level, out applyPrev, out applyCur);
            if (RaidBarPlacementLogic.Moves(state))
                ApplyPersonalBarShift(card, info.Level, state, applyPrev, applyCur);
                Note("奖励格 " + info.Level + "：状态 " + state + " ⇒ 个人进度条按档位偏移**已落地**（level>0=" + applyCur
                     + "；prefab 偏移表 current(-74.34 / -72.31) · previous(0 / -72.31)）");

            // ---- ② 个人进度条 + 分数牌（**U04 的** `EventProgressBarController.Initialize`）
            //      它自己三跳：`Root.SetActive(Next != 0)` → `ScoreObject.Initialize` → `SetBarFill(p)`
            //      （`SetBarFill` 里再管端帽显隐，常数 `TipSpriteMaxRatio`）。我们只负责「p 怎么画成 quad」。
            if (card.PersonalBar != null)
                card.PersonalBar.Initialize(info.PlayerScoringInfo, ItemIdOf(info), isLastMilestone);

            // ---- ③ 领奖钮 6 档（**U04 的** `BaseEventProgressClaimButtonController.Initialize`）
            if (card.Claim != null)
            {
                card.Claim.Initialize(state, info.NormalPrice, () => OnCollect(card));
                card.LevelHighlight = RaidClaimButtonLogic.LevelHighlightFor(state);
                card.ForceInteractableFalse = RaidClaimButtonLogic.ForceInteractableFor(state);
            }
            // 原版 `Raid…Initialize` 的 Interactable 那半（**只在原本可点时写**，⛔ 只关不开）：
            // `if (collectButton.Interactable) collectButton.Interactable = (state != Collected && state != Locked);`
            if (card.CollectButtonButton != null && card.CollectButtonButton.Interactable)
                card.CollectButtonButton.Interactable = RaidClaimButtonLogic.InteractableFor(state);
            // 两价格：`Old Price Display` 恒 `normalPrice`；`Collect Button` 的 `PriceDisplay` = `PriceToShow(state,…)`
            if (card.OldPriceText != null) card.OldPriceText.SetText(PriceText(info.NormalPrice));
            if (card.ButtonPriceText != null)
                card.ButtonPriceText.SetText(PriceText(RaidClaimButtonLogic.PriceToShow(state, info.NormalPrice, info.DiscountedPrice)));

            // ---- ④ 社区进度条 + ⑤ 社区档号
            if (card.CommunityBar != null)
                card.CommunityBar.Initialize(info.CommunityScoringInfo, ItemIdOf(info), isLastMilestone);
            if (card.CommunityLevel != null) card.CommunityLevel.SetText((info.Level + 1).ToString());
            // 原版 `RectMask2D.enabled = (Level == 0)`（`EventProgressBarController.ToggleRectMask` 那一跳）：
            // 本壳没有 `RectMask2D`（裁切走 `ViewportClip`）⇒ `ToggleRectMask` 是空操作，出声一次。
            if (!_maskWarned)
            {
                _maskWarned = true;
                Debug.Log("[Raid] 进度条的 `RectMask2D.enabled = (档号==0)` 这一跳：本壳没有 `RectMask2D`"
                        + "（裁切走 `ViewportClip`，建树即裁）⇒ `EventProgressBarController.ToggleRectMask` 是空操作（已记账）。");
            }

            // ---- ⑥ 奖励（原版 `RaidRewardDrawerController` / `ItemDrawer` 逐件实例化）—— 本地只出声一次
            if (info.CurrentRewards != null && info.CurrentRewards.Count > 0 && !_noRewardArtWarned)
            {
                _noRewardArtWarned = true;
                Debug.LogWarning("[Raid] 奖励物品（" + info.CurrentRewards.Count + " 件）：原版 `RaidRewardDrawerController` 用 "
                               + "`ItemDrawer` 逐件实例化图标；我们**没有物品图标运行时** ⇒ `RewardTransform` 是空容器（缺口已记账）。");
            }

            // ---- ⑦ 高亮框出厂关着
            if (card.Highlight != null) card.Highlight.gameObject.SetActive(false);
        }

        /// <summary>`ScoreObject.Initialize` 的**钩子体**（U04 把这一跳留给了窗体，见
        /// `EventProgressBarController.cs` 文件头差异 ②）：算显隐 + 拼文本，然后走原版那两句
        /// `scoreText.enabled / iconHolder.SetActive`（由 `EventProgressBarScoreObject.Toggle` 承担 ——
        /// ⛔ 不在这里另写一遍开关）。</summary>
        void ApplyScoreObject(PointsRefs pts, ScoringDataInfo info, bool isLastMilestone)
        {
            if (info == null) return;
            bool show = RaidScoreObjectLogic.Visible(info, isLastMilestone);
            if (pts.Label != null)
            {
                if (show) pts.Label.SetText(RaidScoreObjectLogic.Text(info));
                pts.Label.enabled = show;      // = 原版 `scoreText.enabled`
            }
            if (pts.Icon != null) pts.Icon.gameObject.SetActive(show);
        }

        /// <summary>`ScoringEventData.scoringItemId`（= 原版 `ObtainableItem`，`EventProgressBarController.Initialize`
        /// 的第二参）。本包只有里程碑奖励 id，取**第一条奖励**；没有就用 `ScoringEventData` 上那个 id。</summary>
        string ItemIdOf(RaidRewardSelectorLogic.RaidEventMilestoneInfo info)
        {
            if (info != null && info.CurrentRewards != null && info.CurrentRewards.Count > 0
                && !string.IsNullOrEmpty(info.CurrentRewards[0].Id))
                return info.CurrentRewards[0].Id;
            var d = _raid != null ? _raid.Data : null;
            return d != null ? d.scoringItemId : null;
        }

        /// <summary>销毁一个节点 —— **照 `MenuDraw.ClearChildren` 的同一处口径**
        /// （编辑器非播放态 `DestroyImmediate`、运行时 `Destroy`；批处理下 `Destroy` 不生效，见 CLAUDE.md §三）。
        /// ⛔ 别在运行路径直接 `DestroyImmediate`（会打断同一帧里还在用的件）。</summary>
        static void DestroyNode(GameObject go)
        {
            if (go == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); return; }
#endif
            UnityEngine.Object.Destroy(go);
        }

        /// <summary>价格文本：原版 `PriceDisplay` 画的是「图标 + 数字」；本地货币图没有对应件 ⇒ 只印数字。
        /// <para>⚠️ 缺口：`PriceDisplay.icon`（货币图标）我们没接 —— 与 `RewardTransform` 同一类（已记账）。</para></summary>
        string PriceText(RaidCurrencyValue v)
        {
            if (v == null) return NoData;
            return v.Amount.ToString();
        }

        /// <summary>原版 `RaidHeaderController.UpdateText()` 的**视图侧**（纯算式在 <see cref="RaidHeaderLogic"/>）。
        /// <para>🔴 没有事件 ⇒ **一个字段都不写**（原版整段被「事件不为空」包着）⇒ 保持 prefab 出厂态。</para></summary>
        void ApplyHeaderText()
        {
            if (_raid == null) return;
            var data = _raid.GetBaseData();
            if (data == null) return;
            long nowMs = LiveOpsEventTimeData.ToMs(LiveOpsClock.NowUtc);
            var plan = RaidHeaderLogic.UpdateText(data, _eventStage, nowMs, TermEndsIn, TermStartsIn);
            if (!plan.HasEvent) return;

            if (_timeTitleNode != null) _timeTitleNode.gameObject.SetActive(plan.TimeTitleVisible);
            if (_timeTitle != null && plan.TimeTitleVisible) _timeTitle.SetText(Loc.T(plan.TimeTitleTerm));
            if (_graceNode != null) _graceNode.gameObject.SetActive(plan.GraceVisible);
            // ⚠️ 倒计时**交给 `TimerDisplay`**（每秒自刷）—— ⛔ 不在这里硬写一次文字：
            //    `TimerDisplay` 每秒会把 `_time` 刷回它的 target 算出来的值，硬写会被下一秒盖掉（静默）。
            //    ⇒ 有事件时**换 target**（原版 `RaidHeaderController.UpdateTimer` 也是每秒刷 `UpdateText`）。
            if (_time != null && plan.TimeTitleVisible && plan.CountdownToMs > 0)
            {
                long to = plan.CountdownToMs;
                TimerDisplay.Attach(_time, () => LiveOpsEventTimeData.FromMs(to), "", "Raid/Header/Timer",
                                    "Raid 活动倒计时：原版每秒 `UpdateText`（`GetTimeStringFromDate`），"
                                  + "我们交给全工程唯一的倒计时件 `TimerDisplay`");
            }
        }

        /// <summary>原版 `RaidEventRewardItemContainer.OnCollect()`（`__OnCollect.c`，**逐句**）：
        /// <code>
        /// selector.CollectRewardClicked(currentEventMilestoneInfo.Level);   // ① 关聊天预览 → PurchaseMilestone
        /// ToggleHighlightEffect(true);                                      // ② 点亮高亮 + 放音 + glow
        /// </code>
        /// <para>① 的里面（`RaidRewardSelector.CollectRewardClicked`）：关聊天预览 →
        /// `raidEvent.PurchaseMilestone(level, onSuccess, onError)`；
        /// 成功回调 = 恢复聊天预览 + 放 `rewardClaimedSound` + **CreateRewards()**；失败回调 = 恢复预览 + CreateRewards()。</para>
        /// <para>⚠️ 顺序差异（如实记）：原版的 `PurchaseMilestone` 是**异步**（服务器），所以那一下高亮打在**旧的**那张卡上、
        /// 玩家看得见；我们本地是**同步**（重建在 `PurchaseMilestone` 里就发生了）⇒ 为了「看得见」这同一下，
        /// 我们把高亮打在**重建后同一档的新卡**上。⛔ 这不是新行为，是同一跳在同步时序下的等价落地。</para></summary>
        void OnCollect(RewardCard card)
        {
            if (card == null || card.Info == null)
            {
                Debug.LogWarning("[Raid] 领奖：这张格子没有档位信息（原版 `currentEventMilestoneInfo` 为 null 会 NRE）⇒ 不动作");
                return;
            }
            int level = card.Info.Level;
            CollectRewardClicked(level);
            // ② 高亮那一下 —— 落到「同档」的那张卡上（见上面顺序差异）
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null && _cards[i].Info != null && _cards[i].Info.Level == level)
                { SetHighlight(_cards[i], true); break; }
        }

        /// <summary>原版 `RaidRewardSelector.CollectRewardClicked(int rewardToClaim)`（`__CollectRewardClicked.c`）：
        /// `EventSystemController.Toggle(false)` → `raidEvent.PurchaseMilestone(rewardToClaim, onSuccess, onError)`。
        /// <para>🔴 2026-10-28（B93-5）铁律 5 就地订正：原文写的 `ChatPreviewMessage.Instance?.Enable(false)` 是 **ICF 折叠出来的假符号**，
        /// 真身 = `EventSystemController.Toggle`（同族判据 → `Shell/GachaEventWindow.cs:1258`）；本仓等价物 = `PointerLayer.SetInputEnabled`。</para></summary>
        void CollectRewardClicked(int rewardToClaim)
        {
            // 原版这一跳 = `EventSystemController.Toggle(false)`（cpp2il 的 ICF 折叠把它印成 `ChatPreviewMessage.Enable`）
            PointerLayer.SetInputEnabled(false);
            if (_raid == null)
            {
                Debug.LogWarning("[Raid] 领奖：没有事件实例 ⇒ 不做任何事（原版这一跳 NRE）");
                return;
            }
            _raid.PurchaseMilestone(rewardToClaim,
                rewards =>
                {
                    // 成功回调（`b__7_0`）：恢复事件系统 → 放 `rewardClaimedSound` → `CreateRewards()`
                    PointerLayer.SetInputEnabled(true); WindowSounds.PlayFx(WindowSounds.PurchaseCue, "Raid/Collect");
                    RebuildRewards();
                },
                error =>
                {
                    // 失败回调（`b__7_1`）：恢复事件系统 → `CreateRewards()`
                    PointerLayer.SetInputEnabled(true); Debug.LogWarning("[Raid] 领奖失败（原版走 PlayFab 的 onError）：" + error);
                    RebuildRewards();
                });
        }

        /// <summary>原版 `RaidEventRewardItemContainer.ToggleHighlightEffect(bool option)` 的视图侧：
        /// `highlight.SetActive(option)`；`option == true` 时再放音 + 起 glow（三跳顺序照抄）。</summary>
        void SetHighlight(RewardCard card, bool option)
        {
            if (card == null || card.Highlight == null) return;
            var plan = RaidRewardContainerLogic.ToggleHighlightEffect(option);
            card.Highlight.gameObject.SetActive(plan.Active);
            if (!plan.Active) return;
            if (plan.PlaySound) WindowSounds.PlayFx(WindowSounds.PositiveCue, "Raid/Highlight");
            if (plan.PlayGlow) Note("`Collect Highlight Effect` 的 `UIBorderGlow.Play()` 本壳没有等价运行时 ⇒ 只亮框、不起 glow（已记账）");
        }

        /// <summary>自检入口：把「点亮高亮」那一跳直接跑一次（= 原版 `TestHighlighOn` = `ToggleHighlightEffect(true)`）。</summary>
        public void TestHighlighOn(int index)
        {
            if (index < 0 || index >= _cards.Count) { Debug.LogWarning("[Raid] TestHighlighOn：下标越界 " + index); return; }
            SetHighlight(_cards[index], true);
        }

        // ════════════════════════════════════════════════════════════════════════════════════════
        //  B93-5（2026-10-28 · 文件所有者代理 `D2`）：`RaidEventTab` / `RaidHeaderController` /
        //  `RaidProgressBarScaleAndPositionController` 三者的**接线**（算式在 `Shell/RaidEvent.cs` 的 `…Logic` 族）。
        //  【为什么加在类尾】账按 `文件:行号` 记落点（`:417/:463/:647/:706/:850/:1029/:1177/:1227/:1259/:1273/`
        //    `:1295/:1406/:1441/:1460/:1486/:1497`）⇒ 新成员只能加在所有老落点之后；中间那五处改动
        //    都是**同一物理行的就地改写**，行数不变。
        // ════════════════════════════════════════════════════════════════════════════════════════

        // ------------------------------------------------------------ `RaidEventTab.OnClose` / `Reset`
        /// <summary>原版 `RaidEventTab.OnClose()`（`__OnClose.c` 全文 2 句）：`base.OnClose(); Reset();`。
        /// <para>本仓：`base` 的收尾在 `GameWindow.Close` ⇒ 本方法只跑那一跳 `Reset()`，由类尾 `Close()` 覆写在关窗前调。</para></summary>
        public void OnClose()
        {
            if (!gameObject.activeSelf) return;      // 原版 `base.OnClose` 那道「没开就不收尾」的闸
            Reset();
        }

        /// <summary>原版 `RaidEventTab.Reset()`（`__Reset.c` 全文 3 句，逐句）：
        /// <code>
        /// CustomDebug.LogWarning(字面量没解出);                       // ⛔ 原文没解出，不编
        /// LiveOpsAssetUtility.Unload(事件, 1);                        // 卸背景图
        /// LiveOpsAssetUtility.Unload(事件, 5);                        // 卸阵营图（走 headerController 那一跳）
        /// </code>
        /// <para>本地一张 liveops 图都不加载 ⇒ 两句都是空；**照原版出声**并把要卸的两个类型打出来。</para></summary>
        public void Reset()
        {
            // 🔴 `Reset()` 是 MonoBehaviour 的**编辑器回调**（`AddComponent` 时会自动调一次）——那时树还没建（`_page == null`）
            //   ⇒ 原版这一跳也不可能发生 ⇒ 早退（⛔ 否则每次建窗都会白打一条告警）。
            if (_page == null) return;
            var plan = RaidEventTabLogic.Reset(_header != null);
            Debug.LogWarning("[Raid] `Reset`：原版那句 `LogWarning` 的**原文没解出**（`DAT_1842926d0` 是字符串对象）⇒ 只出声、不编原文。要卸的 liveops 图 = " + string.Join("/", plan.AssetTypes) + "；headerController 在不在 = " + plan.HeaderUnloads + "。");
            for (int i = 0; i < plan.AssetTypes.Length; i++) LiveOpsAssetUtility.UnloadAssets(_raid, plan.AssetTypes[i]);   // 🔴 2026-10-29（B95_RD）：原版那两句 `Unload(事件,1)` / `Unload(事件,5)` 从「空跳」改成**真释放**（按 key 取 `EventAssets` 的 GUID → 引用计数 −1）
            // ⛔ `plan.AssetTypes` 的顺序就是原版那两句的顺序（先 1 背景图，有 headerController 才再有 5 阵营图）。
        }

        /// <summary>关窗前先跑原版那一跳 `OnClose`（= 原版 `OnClose` 在 `base.OnClose` 前后各占一半，顺序照抄）。</summary>
        public override void Close()
        {
            OnClose();
            base.Close();
        }

        // ------------------------------------------------------------ `RaidHeaderController` 生命周期
        /// <summary>原版 `RaidHeaderController.Awake()` 的接线结果（= 那颗 `eventHelpButton.onClick` 挂没挂上）。
        /// <para>🔴 **本 prefab 的 `eventHelpButton` 是空 PPtr（= null）** ⇒ 原版这一跳**不执行** ⇒ 返回 `false`，
        /// 我们也就**不伪造一颗钮**（判据 → 本文件头 ②「`RaidHeaderController__Awake.c`」那一条）。</para></summary>
        public bool SubscribeHelpButton()
        {
            bool ok = RaidHeaderControllerLogic.SubscribeHelpButton(false);
            if (!ok) Note("`RaidHeaderController.Awake`：本 prefab 的 `eventHelpButton` 是空 PPtr ⇒ 原版那一跳不执行（不伪造钮）");
            return ok;
        }

        /// <summary>原版 `RaidHeaderController.ClickEventHelpButton()`（`__ClickEventHelpButton.c` 全文 4 句）：
        /// 三道闸 `HasComponent(事件,0x1e)` → 组件自述可开 → `WindowsManager` 可用，全过才 `OpenWindow`。
        /// <para>本地：事件为空 ⇒ 第 1 道闸就停 ⇒ 只出声（⛔ 不去猜那个帮助窗是哪一扇）。</para></summary>
        public void ClickEventHelpButton()
        {
            var plan = RaidHeaderControllerLogic.ClickEventHelpButton(
                hasHelpComponent: _raid != null,          // `HasComponent(事件, 0x1e)` —— 本地无事件 ⇒ false
                componentSaysOpenable: false,             // 那个组件本身拿不到 ⇒ 第 2 道闸也不会过
                hasWindowsManager: WindowsManager.Instance != null);
            Debug.Log("[Raid] `ClickEventHelpButton`：停在第 " + plan.StoppedAt + " 道闸（0 = 全过 ⇒ 会开帮助窗）。"
                    + (plan.Opens ? "已开。" : "未开（原版三道闸：`HasComponent(事件,0x1e)` / 组件自述 / `WindowsManager`）。"));
        }

        /// <summary>原版 `RaidHeaderController.UpdateTimer()` 那个协程的**判据谓词**（原版读
        /// `LiveOpsUtility.GetDateForEventStage(事件, 0xf)` 返回对象的 `+0x24`）。
        /// <para>⚠️ 那一格**语义没解出**（类型身份在 cpp2il 里丢了）⇒ 做成可注入谓词，**默认恒 `false`**
        /// （= 不自己造一个会无限跑的循环；倒计时文本本来就由 `TimerDisplay` 每秒刷）。</para></summary>
        public System.Func<bool> HeaderTimerShouldContinue = () => false;

        /// <summary>原版 `RaidHeaderController.UpdateTimer()`（`__UpdateTimer.c` + `_d__12__MoveNext.c`）：
        /// `while (谓词) { yield return WaitForSeconds(1); UpdateText(); }`。</summary>
        public System.Collections.IEnumerator UpdateTimer()
        {
            return RaidHeaderControllerLogic.UpdateTimer(HeaderTimerShouldContinue, ApplyHeaderText).GetEnumerator();
        }

        /// <summary>起那个每秒协程（= 原版 `Initialize` 的最后一跳 `StartCoroutine(UpdateTimer())`）。
        /// <para>⚠️ 批处理没有帧循环 ⇒ 只会在 Play 模式里真跑；`HeaderTimerShouldContinue` 默认 `false`
        /// ⇒ 今天它**一起就结束**（如实记录，不假装在跑）。</para></summary>
        public void StartHeaderTimer()
        {
            if (!RaidHeaderControllerLogic.Initialize(_raid != null).StartsTimer) return;
            StartCoroutine(UpdateTimer());
        }

        /// <summary>原版 `RaidHeaderController.OnDestroy()`（`__OnDestroy.c` 全文 1 句）：`Unload(事件, 5)`（卸阵营图）。</summary>
        public void OnDestroy()
        {
            if (!RaidHeaderControllerLogic.ReleasesArmyIcon(_raid != null)) return;
            LiveOpsAssetUtility.UnloadAssets(_raid, (int)LiveOpsAssetRef.ArmyImage);   // 🔴 2026-10-29（B95_RD）：原版 `Unload(事件, 5)`（5 = `EventAssetReferenceType.ArmyImage`）—— 从「空跳」改成**真释放**
        }

        // ------------------------------------------------------------ `RaidProgressBarScaleAndPositionController`
        /// <summary>原版基类 `BaseProgressBarScaleAndPositionController&lt;TState&gt;.Initialize` 的两步
        /// （判据 → `Shell/ExpansionPassEventWindow.PersonalBarRect`，同一份结论）：**先按上一档摆，`level > 0` 时再按当前档叠加**。
        /// <para>落地 = 改那颗 `Personal Raid Progress Bar` 的**父框**；子件（`Bar` / `ProgressionBar` / 端帽）
        /// 的矩形是**父局部坐标** ⇒ 父框一动它们跟着走（这一档与 Expansion Pass 那一边不同，那边子件是绝对设计 px）。</para></summary>
        void ApplyPersonalBarShift(RewardCard card, int level, RaidRewardState state, bool applyPrev, bool applyCur)
        {
            if (card == null || card.PersonalBarRoot == null) return;
            var r = PersonalBarRootR;
            if (applyPrev)
                r = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(
                        r, RaidRewardState.CommunityUnlocked,          // 上一档恒按「会挪」那一档算（原版先摆这一遍）
                        RaidBarPlacementLogic.PositionOffsetPrevious, RaidBarPlacementLogic.ScaleOffsetPrevious);
            if (applyCur)
                r = RaidProgressBarScaleAndPositionControllerLogic.SetPositionAndScale(
                        r, state, RaidBarPlacementLogic.PositionOffsetCurrent, RaidBarPlacementLogic.ScaleOffsetCurrent);
            MenuDraw.ApplyPxRect(card.PersonalBarRoot, card.PersonalBarRoot.parent, r);
        }
    }
}
