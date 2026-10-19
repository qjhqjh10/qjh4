// ExpansionPassEventWindow.cs — 活动线 `L03`：**Expansion Pass 战令页**（原版 `ExpansionPassEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab`）
//
// ============================ 出处（唯一判据，全部本地现读 · 2026-10-19）============================
// ① **类桩**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）：
//      `ExpansionPassEventTab.cs`（`rewardSelector` / `backgroundImage` / `premiumPanel` / `headerController` /
//                                   `getCardsButton` / `getCardsWindow` / `currentEvent`）
//      `ExpansionPassRewardSelector.cs`（`rewardItemContainer` / `contentAnchor` / `rewardClaimedSound` / `viewport`）
//      `ExpansionPassRewardItemContainer.cs`（`culling` / `rewardDrawerController` / `highlightReward` /
//          `highlightEffect` / `soundOnCollect` / `premiumGlow` / `allRewardsInfoButton` / `personalProgressBar` /
//          `personalProgressBarScaleAndPositionController` / `claimButtonController` / `levelUpContainer` /
//          `levelUpPriceDisplay`）
//      `ExpansionPassProgressClaimButtonController.cs`（`actionButton` / `buyPremium` / `stateDefinitions`）
//      `ExpansionPassPurchasePremiumPanel.cs`（`purchasePremiumButton` / `playerIsNotPremiumObjects` /
//          `playerIsPremiumObjects` / `initialPremiumRewardAnchor` / `lastPremiumRewardAnchor` /
//          `extraPointsText` / `expansionPassPointDrawer`）
//      `ExpansionPassRewardDrawerController.cs` / `ExpansionPassPointDrawer.cs` / `ExpansionPassPremiumDrawer.cs`
//      `ExpansionPassMissionsTab.cs`（第二页：`headerController` / `dailyTickets` / `dailyMissionAnchor` /
//          `missionContainerReference` / `warlordImage`）
// ② **行为**（`d:/2/tools/decomp_full/`，方法体完整）：
//      · `BaseParentEventWindow__SetupTabs.c` / `__CreateEventTab.c` → Expansion Pass 是 `Parent Event Window`
//        里由 liveops 事件列表动态建出来的一个 tab（与 Gacha / Raid 同构，**不是独立窗口**）。
//      · `ExpansionPassEventTab__OnOpen.c` = `WindowTabBase.OnOpen()` + `Initialize()`。
//      · `ExpansionPassEventTab__Initialize.c`（逐句）：
//          `rewardSelector.Initialize(currentEvent)` →
//          `backgroundImage.sprite = LiveOpsAssetUtility.LoadAsset(currentEvent, 1)`，
//          随后 `backgroundImage.enabled = (sprite != null)`（⚠️ **组件禁用，不是关 GO**）→
//          `premiumPanel.Initialize(currentEvent, onClosePurchasePopup)` →
//          `headerController.Initialize(currentEvent)` →
//          `getCardsButton.onClick += GetCardsClick` →
//          `getCardsButton.gameObject.SetActive(ConfigManager.GetConfig<...>() 里某个布尔 != 0)`
//          （⚠️ **那个配置项的名字没解出** —— 反编译只到「`config + 0x28 → +0x18 → +0x18` 是 bool」这一层；
//           本件**恒建出按钮**并如实记在这里，⛔ 不拿我们自己的常量冒充原版那个开关）。
//      · `ExpansionPassEventTab__GetCardsClick.c` → `WindowsManager.OpenWindow(getCardsWindow, config)`（**本地有对应窗**）。
//      · `ExpansionPassEventTab__Reset.c`：`CustomDebug.LogWarning` + `Unload(currentEvent, 1)` + 摘 `getCardsButton` 监听。
//      · `ExpansionPassPurchasePremiumPanel__Initialize.c`（逐句）：
//          `playerIsNotPremiumObjects.SetActive(!HasPremium)` · `playerIsPremiumObjects.SetActive(HasPremium)`；
//          `DestroyAllChildren(initialPremiumRewardAnchor/lastPremiumRewardAnchor)`；
//          未买 → 两锚点上各 `ItemDrawer.Draw(Milestones.First/Last 的 premium 奖励)`；
//          已买 → 只画 Last 那件，并把 `ExtraPercentaje` 词条按 `PremiumPercentageBonus` 填进 `extraPointsText`，
//          再 `expansionPassPointDrawer.DrawForArmy(army)`。
//      · `ExpansionPassPurchasePremiumPanel__PurchasePremiumClick.c` →
//          `LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)` 取窗口引用 → `WindowsManager.OpenWindow(...)`
//          （**那个引用在远端 premium 报价数据里，本地没有**）。
//      · `ExpansionPassRewardSelector__CreateRewards.c` → 按 `ExpansionPassEvent.Milestones` 逐件
//          `Instantiate(rewardItemContainer, contentAnchor)`（**里程碑为空 ⇒ 一件都不建**）。
//      · `ExpansionPassRewardItemContainer__Initialize.c`（逐句）：
//          `highlightReward.SetActive(false)`；`premiumGlow.enabled = milestone.IsPremium`；
//          `personalProgressBarScaleAndPositionController.Initialize(...)` →
//          `personalProgressBar.Initialize(scoreInfo, ...)` →
//          `claimButtonController.Initialize(state, normalPrice, onCollect)` →
//          `rewardDrawerController.Initialize(rewards, isPremium, state)`；
//          `levelUpContainer.SetActive(cost > 0)` + `levelUpPriceDisplay.Setup(cost)`；
//          `levelUpPriceDisplay.OnClick += onLevelUp`。
//      · `ExpansionPassProgressClaimButtonController` 的 `stateDefinitions`（prefab MB 实读，4 档，
//          target PPtr 已逐个解成节点名）：
//            state 0 → ClaimButton ✗ · Level ✓ · Lock ✓ · BuyPremium ✗
//            state 1 → ClaimButton ✓ · Level ✗ · Lock ✗ · BuyPremium ✓ · Highlight button claim ✗
//            state 2 → ClaimButton ✓ · Level ✗ · Lock ✗ · BuyPremium ✗
//            state 3 → ClaimButton ✗ · Level ✓ · Lock ✗ · BuyPremium ✗
//          `BuyPremiumClick` = `UIMessageController.ShowError("Event_Description/ExpansionPass/BuyPremiumToClaim")`。
//      · `RaidHeaderController` 复用（`ExpansionPassEventTab.headerController` 字段类型就是它）：
//          `Awake` 给 `eventHelpButton` 挂 `ClickEventHelpButton`（**Pass 页这个 prefab 里它非空** = `Help Icon Player`）→
//          `LiveOpsAssetUtility.HasComponent(事件, 0x1e)` 时打开那个**远端帮助窗**；
//          `Initialize` = `title = GetLabel(事件)` · `armyIcon.sprite = LoadAsset(事件,5)` · `UpdateText()` · 起计时协程；
//          `UpdateText` 阶段 0 ⇒ 关 `timeTitle`；否则按服务器时间在 `TimeEndsIn` / `TimeStartsIn` 之间切、
//          `timeRemaining = GetTimeStringFromDate(日期)`、`gracePeriodTooltip.SetActive(日期 < 服务器时间)`
//          （⚠️ **两个 Expansion Pass prefab 的 `gracePeriodTooltip` 都是 NULL** ⇒ 宽限期那颗 ⓘ **不在这两页上**，
//           ⛔ 别照 Raid 补；Missions 页的 `eventHelpButton` 也是 NULL，那颗 `Help Icon Player (1)` 只是 INACT 的 tooltip）。
//      · **第二页** `ExpansionPassMissionsTab`（`d:/2/tools/decomp_full/ExpansionPassMissionsTab__*.c`）：
//          `SetEventTab` 存 `LiveOps` + 取 `LiveOpsManager.GetHandler<Missions>()` →
//          `headerController.Initialize(事件)` + `warlordImage.sprite = LoadAsset(事件, 2)`；
//          `OnOpen` / `ToFocus` = 基类 `OnOpen()` + `RefreshMissions()`；
//          `RefreshMissions`：取 handler 的**可做任务列表**（`Where(IsActive)`），
//          **一个都没有 ⇒ `WindowTabBase.CloseTab()`**（没数据就没有这个 tab）；有则
//          `dailyTickets.Initialize(第一条)` + `SetChallenge(...)`，再把最多 3 条逐件
//          `Instantiate(missionContainerReference, dailyMissionAnchor)` + `Initialize` + `SetChallenge`，
//          最后把模板 `missionContainerReference.gameObject.SetActive(false)`。
//          `Reset` = `ReleaseEventAssets(事件)`。
// ③ **prefab / 节点树**（`bundle_menus_assets_all`）：
//      · 根 `Expansion Pass Tab`（`ExpansionPassEventTab`，字段 PPtr 5/5 + 1 个 AssetReference 逐个对上）。
//      · 全树 dump（可复跑；下面每个数都能逐字找到）：
//        `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Expansion Pass Tab" --depth 14 --relative`
//        落地副本 → `d:/4/_tmp_view/expansion_pass_tab_dump.txt`（2026-10-19 现读）。
//        奖励容器 / 进度条：
//        `… "Expansion Pass Reward Container" --depth 16 --relative` → `_tmp_view/ep_Expansion_Pass_Reward_Container.txt`
//        `… "Expansion Pass Progress Bar" --depth 16 --relative` → `_tmp_view/ep_Expansion_Pass_Progress_Bar.txt`
//        **第二页**：
//        `… "Expansion Pass Missions Tab" --depth 16 --relative` → `_tmp_view/ep_Expansion_Pass_Missions_Tab.txt`（240 行）
//      · 根 RectTransform：`anchor (0,0)-(1,1)` · `anchoredPosition (82,0)` · `sizeDelta (-163.575,-0.359)`
//        ⇒ 页根 x **163.7875..1920.2125** · y **0.1795..1079.8205**（与 `Gacha Tab` / `Raid Progress Tab` 逐位相同）。
//      · 「像素 → 视觉框」的两处 `UIScaleToFit` 已按 `ratio` 换成视觉框（`Premium Glow` ×0.3562、
//        点数额度图标 `Content` ×0.05848）—— 本壳没有 USF 运行时，⛔ 别把 1080×1080 的布局框照抄。
// ④ **本地化键**（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`；运行时走
//      `Resources/official_loc_13lang.json`，**逐条命中**）：
//        `Event_Description/ExpansionPass/TabTitle` = Pass / 通行证
//        `Event_Description/ExpansionPass/Title` = Expansion Pass / 扩展通行证
//        `Event_Description/ExpansionPass/Subtitle` = Get<color=#E0DC2DFF> Exclusive </color>cards!
//        `Event_Description/ExpansionPass/GetNow` = Get now! / 立即获取！
//        `Event_Description/ExpansionPass/YouArePremium` = You are Premium! / 您是高级用户！
//        `Event_Description/ExpansionPass/ExtraPercentaje` = +{0}% Extra Points（**代码常量键**，不在 prefab 上）
//        `Event_Description/ExpansionPass/LevelUp` = Level up now: / 立即升级：
//        `Event_Description/ExpansionPass/BuyPremiumToClaim`（`BuyPremiumClick` 的报错文案）
//        `Event_Description/General/EndsIn` = Ends in: / 结束于：
//        `Event_Description/BattlePass/WhereButton` = Where to get cards / 如何获取张牌
//        `MainMenu/General/Claim` = Claim / 索取（领取钮的 `Localize.mTerm` 实读）
// ⑤ **承载窗外壳** = `Shell/ParentEventWindow.cs`（界面线 `T28`，原版 `BaseParentEventWindow` 的树：
//      `Menu Dark Background` + `Content Area/Background` + `Tab Buttons` 左栏 + `Tabs` 容器）。
//      本件**直接复用它的 public 常量**（矩形 / 渐变 / 页签几何 / 色值）—— 判据只留一份，⛔ 别抄第二套。
//
// ============================ 🔴 这一扇为什么是【独立窗口】而不是 ParentEventWindow 的第 N 页 ============================
// 原版：`BaseParentEventWindow.SetupTabs` 按**远端 liveops 事件列表**动态建页；Expansion Pass 自己就带**两个**
//  liveops tab：`Expansion Pass Tab`（Pass 页）与 `Expansion Pass Missions Tab`（任务页）。
// 本地：**没有 liveops 事件列表**（远端 PlayFab/CCD）⇒ 无法知道「它们此刻是不是在线」。
// 处置：建「**承载窗外壳 + Pass / Missions 两个页签**」= 原版在「只有 Expansion Pass 这两个 tab」时的等价形态；
//      ⛔ 不把 Raid / Gacha / Two Sides 拉进来假装同时在线（那是编结构）。
// 根名 `Expansion Pass Tab` = 原版**页** prefab 的根名（原版没有独立窗口 prefab，如实记录）。
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编）============================
// 活动数据全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里没有：
//   · `ExpansionPassEvent`：`Milestones`（奖励轨道逐档数据）/ `PlayerScore` / `HasPremium` /
//     `PremiumPercentageBonus` / `PremiumOfferData`（付费档报价）/ 起止时刻
//   · `LiveOpsAssetUtility.LoadAsset(事件, 1)` = 页背景图；`LoadAsset(事件, 5)` = 阵营图；
//     `GetLabel(事件)` = 头衔文案；`GetComponentReference(premiumOfferData, 0xc)` = 买付费档要开的窗口
//   · `ExpansionPassRewardSelector.Initialize` 的 `IScoringEvent`（进度条两端 current/max、里程碑状态）
//   · 每档 `RewardInfo` 列表（`ExpansionPassRewardDrawerController.Initialize` 逐件画）
//   · **Missions 页**：`Missions` handler 的任务列表 / 每日任务挑战 / 周常进度 / 骷髅里程碑 / 每档奖励 / 计时器
//     （`ExpansionPassMissionsTab.RefreshMissions` 逐条读；**一条都没有时原版直接 `CloseTab`**）
//   · `LiveOpsAssetUtility.LoadAsset(事件, 2)` = Missions 页 `Event Image`（= `warlordImage`）
// ⇒ 本窗照 **prefab 出厂结构**建满，**数值一律 `--` 占位**（⛔ 不编等级/分数/奖励/价格/日期），
//    并在 `Open()` 里 `Debug.LogWarning` **逐条出声**（本工程红线：不许静默失败）。
//    **领奖 / 升级 / 买付费档**：原版分别是 `ExpansionPassEvent.OnRewardCollected` / `PurchaseLevelUp` /
//    `PurchasePremiumClick → OpenWindow(远端窗口引用)`（**全是服务器 + 远端引用**）⇒ 本地点击**只出声**。
// 🔴 **有意偏离（照实记录）**：
//    原版 `ExpansionPassRewardSelector.CreateRewards` 按里程碑逐件 Instantiate 奖励容器，里程碑为空时**一件都不建**；
//    本任务的验收口径是「**页面能打开、看得到原版结构、空态出声**」⇒ 我们**只建一份模板 `Expansion Pass Reward Container` 并关着**。
// 🔴 **第二处有意偏离（`HasPremium` 未知时的分支）**：
//    原版 `ExpansionPassPurchasePremiumPanel.Initialize` 按 `ExpansionPassEvent.HasPremium` 二选一显示
//    `IsNotPremium` / `Is Premium`。本地**没有这个布尔**（在远端存档里）⇒ 我们**默认走 `IsNotPremium` 一支**
//    （= 未购买态；这样「付费档入口 `Get now!`」是可点到的，与本次任务目标一致），并把 `Is Premium` 建成**关着**。
//    ⛔ 不写成「原版此刻就是未购买」—— 原版此刻显示哪一支由服务器数据决定。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **页签栏**：原版 `TabButtons.AddTabButton(label, sprite)` 由 liveops 数据驱动；本窗建**两颗**：
//   「Pass」（`Event_Description/ExpansionPass/TabTitle`）与「Missions」（`MainMenu/General/Missions` —— **我们挑的兜底**），
//   图标都留空（liveops sprite 本地没有）。两页互斥 `SetActive`。
// · **Missions 页的周常卡**（`Daily Mission Container Ref`）：原版按任务逐件 Instantiate；本地没有任务
//   ⇒ 只建一份**关着的**模板（原版 `RefreshMissions` 末尾也会关它）+ 数值 `--`。
// · **两张 premium 卡预览**（`Fisrt/Last Premium Reward/Card Drawer`）：原版由 `ItemDrawer.Draw` 按奖励数据
//   装配真实卡面 ⇒ 本地没有奖励数据 ⇒ **只建空容器**（⛔ 不拿 prefab 里的示例卡当真实奖励）。
// · **`Background Image` / `Army Icon`**：运行时从 liveops 灌图；本地取不到 ⇒ 只建节点、不出图
//   （原版在 `Background Image` 取不到 sprite 时也会把 `Image` 组件 `set_enabled(false)`，GO 仍 active）。
// · **两张图在 `Resources/` 里没有**（源图在本地 `Art/原版/0_mainmenu/`，未进导入器 `MENU_IMAGES`）：
//     `40K_general_icon_Expansion Pass XP`（点数图标）· `40k_Rewards_icon_base card SDF`（奖励容器 Premium Glow）
//   ⇒ 这两件**不画**并出声（⛔ 不用语义不同的图冒充；Gacha 那种「同义变体」回退在这里不成立）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>Expansion Pass 活动窗（原版 `ExpansionPassEventTab` + `ExpansionPassMissionsTab` 两个 liveops 页）——
    /// **承载窗外壳照 `Parent Event Window`、内容照 `Expansion Pass Tab` / `Expansion Pass Missions Tab` prefab**。
    /// <para>🔴 活动数据全在远端 LiveOps ⇒ 结构照建、数值 `--` + 出声（⛔ 不编；判据 → 文件头「数据缺口」）。</para>
    /// <para>⚠️ 本地**没有生产入口**：原版由 liveops 事件链 `BaseParentEventWindow.CreateEventTab` 建出来
    /// ⇒ 只提供 `WindowsManager.OpenExpansionPassEvent()`，⛔ 不伪造调用点。</para></summary>
    public class ExpansionPassEventWindow : GameWindow
    {
        // ============================================================ 渲染队列档（3690–3729 · 全工程现读空档）
        // 相邻带：`RaidEventWindow` 3660–3689 · 下一档 3935（`EnergySinglePlayerOnlyEventWindow`）。
        // 🔴 梯子（每层 +1；同档时 `ImageQuad` 世界 z 恒 0 ⇒ 谁盖谁不可控 —— CLAUDE.md §三）：
        public const int QShade = 3690;        // `Menu Dark Background`（含「点暗底关窗」命中区）
        const int QContentBg = 3691;           // `Content Area/Background` 渐变（低于所有页内容）
        const int QPageBg = 3692;              // 页：`Background Black`
        const int QPageBgImage = 3693;         // 页：`Background Image`（liveops 图，本地不画）
        // 付费档面板（`Purchase premium Panel`）
        const int QPanelBg = 3694;             // `Background`
        const int QPanelCard = 3695;           // 两张 premium 卡预览（本地空容器）
        const int QPanelDecor = 3696;          // 点数图标 / INACT 抽屉名牌
        const int QPanelText = 3697;           // Title / Subtitle / Extra points
        const int QPanelBtn = 3698;            // 按钮底 / 高亮框
        const int QPanelBtnText = 3699;        // 按钮文字
        const int QPanelHi = 3700;             // 按钮高亮框（原版兄弟序在文字之后 ⇒ 必须高于按钮底）
        // 奖励容器（`Expansion Pass Reward Container` · 模板关着）
        const int QCardBg = 3701;              // `Level up/Background`
        const int QCardArt = 3702;             // `Arrow` / `Premium Glow` / `Icon Extra rewards`
        const int QCardGlow = 3703;            // `Collect Highlight Effect`
        const int QCardReward = 3704;          // `RewardTransform` 上的奖励件
        const int QCardProgressBg = 3705;      // 进度条底
        const int QCardBar = 3706;             // 进度条填充 / 端帽
        const int QCardBtn = 3707;             // 领取钮 / 升级钮 / 里程碑
        const int QCardHi = 3708;              // 领取钮高亮框 / 里程碑锁（原版兄弟序在底之后）
        const int QCardText = 3709;            // 容器内全部文字
        // 页头 / 页内按钮 / 调试件（兄弟序在滚动区之后 ⇒ 必须高于滚动区内容）
        const int QHeaderIcon = 3710;
        const int QHeaderText = 3711;
        const int QWhereBtn = 3712;
        const int QWhereBtnText = 3713;
        const int QDebug = 3714;
        // 左侧页签栏（必须高于**页内全部件**：页签栏与页在 x 163.8–332.2 重叠）
        const int QPanel = 3716;               // `Tab Buttons` 底板
        const int QTabShadow = 3717;
        const int QTabHi = 3718;               // 选中高亮
        const int QTabIcon = 3719;
        const int QTabName = 3720;             // 页签名牌底
        const int QTabLabel = 3721;
        const int QBadge = 3722;
        const int QBadgeText = 3723;
        /// <summary>本窗内容命中区那一档（压暗层命中区必须严格低于它；`Absorb` 取 `QHit − 1`）。</summary>
        public const int QHit = 3729;

        // ============================================================ 根 / 节点名
        /// <summary>prefab 根名（= `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// ⚠️ 这是原版**页** prefab 的名字（原版没有独立窗口 prefab，见文件头）。</summary>
        public const string RootName = "Expansion Pass Tab";
        public const string PageNodeName = "Expansion Pass Tab Page";
        public const string TabButtonName = "Expansion Pass Tab Button";
        /// <summary>第二个页签 / 第二页（原版 `Expansion Pass Missions Tab`）。</summary>
        public const string MissionsPageNodeName = "Expansion Pass Missions Tab Page";
        public const string MissionsTabButtonName = "Expansion Pass Missions Tab Button";
        /// <summary>`ExpansionPassRewardSelector.rewardItemContainer` 那个 prefab 的根名（运行时逐里程碑 Instantiate）。</summary>
        public const string RewardContainerName = "Expansion Pass Reward Container";

        // ============================================================ 窗参（`Parent Event Window` 根 MB 实读）
        // `type = 0`(Fullscreen) · `windowsPlacement = 5`(Canvas) · `closeOnESC = 1` · `updateNavPanel = 1`
        // · `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` —— 逐字段实读
        //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6349037533193994564.json`。
        // ⚠️ `Expansion Pass Tab` 自己**没有窗口组件**（只有 `ExpansionPassEventTab`）⇒ 窗参只能取承载窗那一份。
        public const bool ParamsCloseOnEsc = true;
        public const bool ParamsUpdateNavPanel = true;
        public const float ParamsExtraScale = 1f;
        public const bool ParamsUseDefaultCloseSound = true;

        // ============================================================ 几何：页根偏移（根 RectTransform 实读）
        // `anchor (0,0)-(1,1)` · `pos (82,0)` · `size (-163.575,-0.359)` · 父 1920×1080
        // ⇒ 页根 x **163.7875..1920.2125** · y **0.1795..1079.8205**（与 `Gacha Tab` / `Raid Progress Tab` 逐位相同）。
        public const float PageDx = 163.7875f, PageDy = 0.1795f;
        public const float PageW = 1756.425f, PageH = 1079.641f;

        static PxRect R(float x1, float y1, float x2, float y2)
            => new PxRect(x1 + PageDx, y1 + PageDy, x2 + PageDx, y2 + PageDy);

        // ---- 页根直接子件（dump 相对值 + `R`；兄弟序 = prefab 的 m_Children）----
        static readonly PxRect PageBgR = R(0f, 0f, PageW, PageH);                    // `Background Black`
        static readonly PxRect PageBgImageR = R(0f, 137.1f, 3767.4f, 1044.9f);       // `Background Image`（Tiled · 本地不画）
        static readonly PxRect PremiumPanelR = R(0f, 167.3f, 594.2f, 353.9f);        // `Purchase premium Panel`
        static readonly PxRect ScrollR = R(0.3f, 232.4f, 1756.1f, 1019.1f);          // `Rewards Scroll View`
        static readonly PxRect ContentR = R(0.3f, 181.1f, 122.3f, 1010.3f);          // `Rewards Content`（HLG 轨道）
        static readonly PxRect HeaderR = R(0f, -0.1f, 1756.4f, 181.9f);              // `Header`
        static readonly PxRect WhereBtnR = R(688.0f, 274.7f, 1044.6f, 333.2f);       // `Generic Simplified UI Button (1)`
        static readonly PxRect WhereBtnTextR = R(700.4f, 260.8f, 1031.7f, 347.1f);
        static readonly PxRect DebugAddPointsR = R(700.3f, 143.0f, 903.9f, 197.0f);  // **出厂 INACT**
        static readonly PxRect DebugTextR = DebugAddPointsR;
        static readonly PxRect DebugInputR = R(914.0f, 143.0f, 998.6f, 197.0f);

        // ---- `Purchase premium Panel`（全部是**页坐标**；prefab 里它自己 pivot 在左边中点）----
        static readonly PxRect PremiumBgR = R(0f, 158.8f, 594.2f, 372.2f);           // `Background`（WF_Campaign_Info_Background）
        static readonly PxRect LastPremiumR = R(415.7f, 136.2f, 609.7f, 330.2f);     // rot -17.50°
        static readonly PxRect LastCardDrawerR = R(448.7f, 136.2f, 576.7f, 330.2f);   // rot  9.94°
        static readonly PxRect FirstPremiumR = R(299.1f, 131.2f, 493.1f, 325.2f);    // rot  4.83°
        static readonly PxRect FirstCardDrawerR = R(332.1f, 131.2f, 460.1f, 325.2f); // rot -1.98°
        static readonly PxRect IsNotPremiumR = R(247.1f, 210.6f, 347.1f, 310.6f);    // **出厂 INACT**（见文件头「有意偏离」）
        static readonly PxRect NpTitleR = R(15.8f, 182.5f, 292.6f, 236.5f);
        static readonly PxRect NpSubtitleR = R(29.0f, 229.6f, 279.4f, 264.6f);
        static readonly PxRect NpButtonR = R(26.4f, 274.7f, 281.7f, 333.2f);
        static readonly PxRect NpButtonTextR = R(35.2f, 273.0f, 272.8f, 334.9f);
        static readonly PxRect NpHighlightR = R(-0.7f, 249.3f, 306.9f, 359.9f);
        static readonly PxRect IsPremiumR = R(0f, 155.0f, 594.2f, 341.6f);
        static readonly PxRect PTitleR = R(21.3f, 197.7f, 324.1f, 251.7f);
        static readonly PxRect ExtraMsgR = R(36.3f, 264.5f, 392.9f, 299.5f);
        static readonly PxRect ExtraIconR = R(22.3f, 250.4f, 85.4f, 313.6f);        // 63.16×63.16（USF 视觉框）
        static readonly PxRect ExtraPointsR = R(89.4f, 264.5f, 321.5f, 299.5f);

        // ---- `Header` ----
        // `Title`：原版 rect 宽 0（`ContentSizeFitterMinMax`）⇒ 本壳没有 CSF 运行时，用 1312 宽的左对齐框
        //   （左缘 = 原版 133、高度 = 原版 63.66）—— **这是我们挑的框**，逐条注在报告里。
        static readonly PxRect TitleR = R(133.0f, 29.1f, 1445.6f, 92.7f);
        static readonly PxRect HelpIconPlayerR = R(144.9f, 34.8f, 197.1f, 87.0f);
        static readonly PxRect ArmyIconR = R(5.0f, 0.9f, 125.0f, 120.9f);           // liveops `LoadAsset(事件,5)`
        // `Time Remaining`：pivot 右缘钉在 x 1582.4；原版宽 0（CSF）⇒ 用 300 宽的右对齐框代替（**我们挑的框**）
        static readonly PxRect TimeTitleR = R(1282.4f, 34.3f, 1582.4f, 84.3f);
        static readonly PxRect TimeClockR = R(1582.4f, 34.5f, 1632.0f, 84.1f);
        static readonly PxRect TimeR = R(1635.8f, 34.1f, 1746.1f, 84.1f);

        // ============================================================ 几何：`Expansion Pass Reward Container`（模板 · 出厂关着）
        // 容器根 505.9 × 719.463；落位 = `Rewards Content` 的 HLG：
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

        // `Level up`（容器兄弟序 0 —— 在最下层）
        static readonly PxRect LevelUpR = K(-211.8f, 619.5f, 304.7f, 719.5f);
        static readonly PxRect LevelUpBgR = K(-182.8f, 642.9f, 281.5f, 723.4f);
        static readonly PxRect LevelUpArrowR = K(63.4f, 625.5f, 147.4f, 646.5f);
        static readonly PxRect LevelUpBtnR = K(-5.3f, 657.8f, 222.0f, 708.6f);
        static readonly PxRect LevelUpBtnTextR = K(7.7f, 663.6f, 209.0f, 702.7f);   // 出厂 INACT
        static readonly PxRect LevelUpPriceR = K(3.3f, 662.2f, 211.6f, 704.9f);
        static readonly PxRect LevelUpPriceIconR = K(86.1f, 662.2f, 128.8f, 704.9f); // 运行时灌货币图
        static readonly PxRect LevelUpPriceTextR = K(133.0f, 662.2f, 211.6f, 704.9f); // 出厂宽 0（CSF）
        static readonly PxRect LevelUpTextR = K(-262.8f, 665.7f, -12.5f, 700.7f);
        // `Premium Glow`（布局框 1148.06×1422.67 · `localScale 0.3562` ⇒ **视觉框 408.90×506.71**，
        //  中心 = 布局框中心；本壳没有 USF/缩放 ⇒ 直接按视觉框建）
        static readonly PxRect PremiumGlowR = Center(K(-321.6f, -409.1f, 826.5f, 1013.5f), 408.90f, 506.71f);
        static readonly PxRect CollectHiR = K(60.1f, 44.4f, 445.8f, 560.9f);         // 图 alpha=0（出厂就不可见）
        static readonly PxRect RewardTransformR = K(104.1f, 110.3f, 401.8f, 495.0f);
        static readonly PxRect ExtraRewardIconR = K(312.3f, 76.1f, 405.6f, 169.4f);
        // `Personal Progression`
        static readonly PxRect PersonalProgR = K(207.9f, 514.7f, 298.0f, 614.7f);
        static readonly PxRect PersonalBarRootR = K(-85.4f, 511.4f, 215.9f, 618.0f);
        static readonly PxRect PersonalBarR = K(-85.4f, 554.7f, 215.9f, 574.7f);
        static readonly PxRect PersonalBarBgR = K(-85.4f, 554.9f, 215.9f, 574.5f);
        static readonly PxRect PersonalBarFillR = K(-85.4f, 574.9f, -85.4f, 574.5f); // 出厂 0 宽 = 进度 0
        static readonly PxRect PersonalBarEndR = K(-97.3f, 546.0f, -44.2f, 603.4f);
        static readonly PxRect PersonalPointsR = K(-20.9f, 591.6f, 151.4f, 628.9f);
        static readonly PxRect PersonalPointsIconR = K(-7.9f, 591.6f, 29.4f, 628.9f); // **没有 Image 组件**（运行时由 ItemDrawer 灌）
        static readonly PxRect PersonalPointsTextR = K(42.4f, 591.6f, 151.4f, 628.9f);
        static readonly PxRect ClaimContainerR = K(210.4f, 520.7f, 295.4f, 608.7f);
        static readonly PxRect ClaimBtnR = K(141.0f, 523.8f, 362.8f, 605.7f);
        static readonly PxRect ClaimBtnTextR = K(154.0f, 545.7f, 349.8f, 583.8f);
        static readonly PxRect ClaimHiR = K(103.3f, 502.8f, 400.8f, 626.3f);
        static readonly PxRect LevelR = K(210.4f, 520.7f, 295.4f, 608.7f);           // 出厂 INACT
        static readonly PxRect LockR = K(235.7f, 540.9f, 270.2f, 585.5f);
        static readonly PxRect CollisionR = K(111.2f, 96.1f, 394.7f, 495.0f);
        static readonly PxRect CullingRefR = K(-211.8f, 0f, 505.9f, 719.5f);

        // ============================================================ 图 / 色（Image MB 逐字段实读）
        const string ArtCampaignInfoBg = "WF_Campaign_Info_Background";  // 740×167 九宫 335,0,395,0
        const string ArtMulliganBtn = "UI_Button_Mulligan";              // 410×124 九宫 333,96,333,96
        const string ArtGlowBorder = "Border_Line_FX";                   // 128×128 九宫 50,50,50,50
        const string ArtBtn = "40K_button";                              // 489×107 九宫 234,46,234,46
        const string ArtOctagon2 = "OctagonUI_Border_SDF_2";             // 128×128 九宫 53,53,53,53
        const string ArtArmyTrackBar = "40K_ArmyTrack_bar";              // 15×21 · ppuMul 0.92
        const string ArtArmyTrackFill = "40K_ArmyTrack_bar_fill";        // 15×21 · ppuMul 0.92
        const string ArtArmyTrackPos = "40K_ArmyTrack_bar_position";     // 64×64 · ppuMul 0.92
        const string ArtMilestone = "40k_ArmyTrack_milestone_off";       // 85×88（`ProgressRewardLevelObject.offSprite`）
        const string ArtLock = "WF_Lock_Icon_Simple";                    // 42×63（原版 sprite 名 = `WF Lock Icon Simple`）
        const string ArtTicket = "40k_topmarquee_currency_ticket";       // 90×90（`Icon Extra rewards`）
        const string ArtClosedChatBg = "Closed-Chat_background";         // 331×61（`Level up/Background`）
        const string ArtArrow = "40K_ArmyTrack_chosen_faction";         // 84×21（切片名里的空格换成下划线）
        const string ArtHelpInfo = "40K_generic_bt_info";                // 128×128
        const string ArtClock = "WF_icon_clock";                         // 64×64（原版 sprite 名带 `for shader`）
        // ---- Missions 页（`Expansion Pass Missions Tab`）----
        const string ArtMissionHorizontal = "40K_missions_display_Daily_horizontal"; // 810×149（周常卡底）
        const string ArtMissionVertical = "40K_missions_display_Daily_vertical";     // 335×556（骷髅卡底，旋转 90°）
        const string ArtSkullIcon = "40K_missions_icon_Daily_skulls";                // 222×198
        const string ArtCheckmark = "40K_settings_icon_checkmark";                   // 66×51
        const string ArtBarEmpty = "40k_generial_bar_empty";                         // 12×12 九宫 4,4,4,4 · ppuMul 2
        const string ArtBarFill = "40k_generial_bar_fill";                           // 12×12 九宫 4,4,4,4 · ppuMul 2
        const string ArtRoundYellow = "40k_general_bt_yellow";                       // 71×71
        const string ArtRoundYellowDelete = "40k_general_bt_yellow_delete";          // 71×71

        static readonly Color LevelUpBgTint = Color.white;
        static readonly Color ArrowTint = new Color(0.8868f, 0.3723f, 0.3723f, 1f);        // MB 实读
        static readonly Color LevelUpBtnTint = new Color(0.9020f, 0.6375f, 0.1804f, 1f);   // MB 实读
        static readonly Color BarFillTint = new Color(1f, 0.6557f, 0.9320f, 1f);           // MB 实读
        static readonly Color ClaimBtnTint = new Color(0.4185f, 0.7709f, 0.9340f, 1f);     // MB 实读
        static readonly Color LockTint = new Color(1f, 1f, 1f, 0.9059f);                   // MB 实读
        static readonly Color WhereBtnTint = new Color(0.5970f, 0.9811f, 0.6579f, 1f);     // MB 实读
        static readonly Color ClockTint = new Color(0.9843f, 0.8275f, 0.5098f, 1f);        // MB 实读
        static readonly Color DebugBtnTint = new Color(1f, 0f, 0.9457f, 1f);               // MB 实读
        static readonly Color CollectHiTint = new Color(1f, 1f, 1f, 0f);                   // 图 alpha 实读 = 0

        // ============================================================ 本地化键（原版 `Localize.mTerm` / 代码常量实读）
        public const string TermTabTitle = "Event_Description/ExpansionPass/TabTitle";
        public const string TermTitle = "Event_Description/ExpansionPass/Title";
        public const string TermSubtitle = "Event_Description/ExpansionPass/Subtitle";
        public const string TermGetNow = "Event_Description/ExpansionPass/GetNow";
        public const string TermYouArePremium = "Event_Description/ExpansionPass/YouArePremium";
        /// <summary>`ExpansionPassPurchasePremiumPanel.PREMIUM_EXTRA_POINTS_KEY` 常量原值（**不在 prefab 组件上**）。</summary>
        public const string TermExtraPercentaje = "Event_Description/ExpansionPass/ExtraPercentaje";
        public const string TermLevelUp = "Event_Description/ExpansionPass/LevelUp";
        public const string TermBuyPremiumToClaim = "Event_Description/ExpansionPass/BuyPremiumToClaim";
        public const string TermEndsIn = "Event_Description/General/EndsIn";
        public const string TermWhereButton = "Event_Description/BattlePass/WhereButton";
        public const string TermClaim = "MainMenu/General/Claim";
        // ---- 第二页（`Expansion Pass Missions Tab`）----
        /// <summary>页签文字兜底。⚠️ **原版取的是 `LiveOpsAssetUtility.GetLabel(事件)`（远端）**；
        /// 本条是官方表里那条通用「Missions / 布道所」—— **我们挑的兜底**，⛔ 不冒充原版。</summary>
        public const string TermMissionsTab = "MainMenu/General/Missions";
        public const string TermWeeklyChallenges = "Missions/Title/WeeklyChallenges";   // 页内 `Mission Header/name`
        public const string TermDailySkulls = "Missions/Title/DailySkulls";            // 骷髅卡头部
        public const string TermDailySkullsInfo = "Missions/Info/DailySkulls";         // 骷髅卡 ⓘ tooltip
        public const string TermCollect = "MainMenu/General/Collect";                  // 领取钮 `Localize.mTerm`
        /// <summary>⚠️ **跨域原文**：Missions 页 `Help Icon Player (1)` 的 `EverguildTooltipTrigger.text` 实读就是
        /// `Event_Description/Raid/IndividualTooltip`（**prefab 自己写的就是 Raid 那一条**，⛔ 别「修正」）。</summary>
        public const string TermRaidIndividualTooltip = "Event_Description/Raid/IndividualTooltip";

        /// <summary>prefab 出厂原文（`Title` 那一条）—— 运行期被 `RaidHeaderController.Initialize` 的
        /// `GetLabel(事件)` 覆盖，本地拿不到 ⇒ 照出厂原文画（同 `RaidEventWindow.PrefabTitleText` 那条先例），并出声。</summary>
        public const string PrefabTitleText = "Event title";
        /// <summary>无数据占位（⚠️ **不是原文**：远端数据缺 ⇒ 只印占位，⛔ 不编数字）。</summary>
        public const string NoData = "--";

        // ============================================================ 状态 / 自检读口
        public static ExpansionPassEventWindow LastOpened { get; private set; }
        /// <summary>活动数据本地没有（恒 true —— 见文件头「数据缺口」）。</summary>
        public bool DataMissing { get { return true; } }
        public string DataNote
        {
            get
            {
                return "Expansion Pass 活动数据全在远端 LiveOps（里程碑奖励轨道/等级与分数/HasPremium/付费档报价/"
                     + "活动起止时刻/背景图/阵营图/页签图标；Missions 页的任务列表/进度/奖励/计时/骷髅里程碑）"
                     + "—— 本地离线包体里没有 ⇒ 结构照 prefab 建满、数值一律 `--` 占位，⛔ 不编。"
                     + "领奖/升级/买付费档/任务领取都是服务器操作 ⇒ 本地点击只出声。";
            }
        }
        /// <summary>`HasPremium` 本地没有 ⇒ 默认展示的那一支（见文件头「有意偏离」）。</summary>
        public const bool DefaultShowsPremiumBranch = false;

        Transform _shade, _contentArea, _background, _tabButtons, _tabShadow, _tabs, _tabMaster;
        Transform _page, _pageBg, _pageBgImage, _premiumPanel, _scroll, _viewport, _content, _header,
                  _whereBtn, _debug, _container, _personalBar;
        Transform _missionsPage;
        int _activePage;
        Label _title, _timeTitle, _time, _npTitle, _npSubtitle, _npButtonText, _pTitle, _extraPoints, _tabLabel;
        Label _mtTitle, _mtTime, _mtWeeklyName, _mtSkullName;

        readonly List<Transform> _tabButtonsList = new List<Transform>();
        readonly List<Transform> _tabHi = new List<Transform>();
        readonly List<Label> _tabLabels = new List<Label>();
        bool _warned;
        readonly List<string> _missArt = new List<string>();
        readonly List<string> _artNotes = new List<string>();

        /// <summary>建不出来的图（非空 = 有件没画；自检读口）。</summary>
        public List<string> MissingArt { get { return _missArt; } }
        /// <summary>「原版有 liveops 图 / 我们画不出」的图（自检读口；⛔ 与 `MissingArt` 不是一回事）。</summary>
        public List<string> ArtNotes { get { return _artNotes; } }

        public Transform ShadeNode { get { return _shade; } }
        public Transform ContentAreaNode { get { return _contentArea; } }
        public Transform TabButtonsNode { get { return _tabButtons; } }
        public Transform TabsNode { get { return _tabs; } }
        public Transform PageNode { get { return _page; } }
        public Transform PageBackgroundNode { get { return _pageBg; } }
        public Transform PageBackgroundImageNode { get { return _pageBgImage; } }
        public Transform PremiumPanelNode { get { return _premiumPanel; } }
        public Transform RewardsScrollNode { get { return _scroll; } }
        public Transform ViewportNode { get { return _viewport; } }
        public Transform RewardsContentNode { get { return _content; } }
        public Transform HeaderNode { get { return _header; } }
        public Transform WhereGetCardsButtonNode { get { return _whereBtn; } }
        public Transform DebugAddPointsNode { get { return _debug; } }
        /// <summary>`ExpansionPassRewardSelector.rewardItemContainer` 那份模板（**出厂关着**，无里程碑数据）。</summary>
        public Transform RewardContainerTemplateNode { get { return _container; } }
        public Transform PersonalProgressBarNode { get { return _personalBar; } }

        public List<Transform> TabButtonNodes { get { return _tabButtonsList; } }
        public List<Transform> TabHighlights { get { return _tabHi; } }
        public List<Label> TabLabels { get { return _tabLabels; } }
        public Label TitleLabel { get { return _title; } }
        public Label TimeTitleLabel { get { return _timeTitle; } }
        public Label TimeLabel { get { return _time; } }
        public Label NotPremiumTitleLabel { get { return _npTitle; } }
        public Label NotPremiumSubtitleLabel { get { return _npSubtitle; } }
        public Label GetNowTextLabel { get { return _npButtonText; } }
        public Label PremiumTitleLabel { get { return _pTitle; } }
        public Label ExtraPointsLabel { get { return _extraPoints; } }
        /// <summary>第二页（`Expansion Pass Missions Tab`）的页节点。</summary>
        public Transform MissionsPageNode { get { return _missionsPage; } }
        /// <summary>当前页（0 = Pass · 1 = Missions）。</summary>
        public int ActivePage { get { return _activePage; } }
        public Label MissionsTitleLabel { get { return _mtTitle; } }
        public Label MissionsTimeLabel { get { return _mtTime; } }
        public Label WeeklyChallengesLabel { get { return _mtWeeklyName; } }
        public Label DailySkullsLabel { get { return _mtSkullName; } }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenExpansionPassEvent` 的 `create` 那一跳）。</summary>
        public static ExpansionPassEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject(RootName);                        // 节点名照 prefab（`OpenByRef` 的键同源）
            var win = go.AddComponent<ExpansionPassEventWindow>();
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
            else { Debug.LogWarning("[ExpansionPass] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
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
        /// `Canvas.sortingOrder`。本窗占 3690–3729，而 `WhereToGetCardsPopup` 在 3479–3487
        /// ⇒ 只写 state 的话，**弹窗会被本窗整个盖住**（症状 = 「点 Where get cards 没反应」，静默失败）。
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
            Debug.LogWarning("[ExpansionPass] " + DataNote);
            Debug.LogWarning("[ExpansionPass] ⚠️ **有意偏离**：原版 `ExpansionPassRewardSelector.CreateRewards` 按 "
                           + "`ExpansionPassEvent.Milestones` 逐件 Instantiate 奖励容器，里程碑为空时**一件都不建**；"
                           + "本件的验收口径 = **页面能打开 + 空态出声** ⇒ 只建一份模板"
                           + "（`Expansion Pass Reward Container`，关着）+ 数值 `--`（见文件头）。");
            Debug.LogWarning("[ExpansionPass] ⚠️ **有意偏离（第二处）**：`HasPremium` 在远端存档里、本地没有 ⇒ "
                           + "默认展示 **`IsNotPremium` 一支**（未购买态，付费档入口可达），`Is Premium` 建成关着。"
                           + "⛔ 原版此刻显示哪一支由服务器数据决定，别把这条当成「原版就是未购买」。");
            Debug.LogWarning("[ExpansionPass] `Title` 照 prefab 出厂原文 `" + PrefabTitleText + "` 画 —— "
                           + "原版运行期被 `RaidHeaderController.Initialize` 的 `GetLabel(事件)` 覆盖，本地没有那份数据。");
            Debug.LogWarning("[ExpansionPass] 页签文字用 `" + TermTabTitle + "`（Pass / 通行证）：原版是 "
                           + "`LiveOpsAssetUtility.GetLabel(事件)`（远端）；页签**图标**留空（liveops sprite 本地没有），"
                           + "⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。");
            Debug.LogWarning("[ExpansionPass] 两张 liveops 图缺失：`Background Image`（`LoadAsset(事件,1)`）与 "
                           + "`Army Icon`（`LoadAsset(事件,5)`）⇒ 只建节点、不出图。"
                           + "⚠️ 原版在 `Background Image` 取不到 sprite 时也会把 `Image` 组件 `set_enabled(false)`。");
            Debug.LogWarning("[ExpansionPass] 领奖 / 升级 / 买付费档都要服务器：原版分别是 "
                           + "`ExpansionPassEvent.OnRewardCollected` / `PurchaseLevelUp` / "
                           + "`PurchasePremiumClick → OpenWindow(远端 premiumOfferData 的窗口引用)`；"
                           + "本地没有后端、也拿不到那个窗口引用 ⇒ **点击只出声、不假装成功**。");
            Debug.LogWarning("[ExpansionPass] 两张 premium 卡预览（`Fisrt/Last Premium Reward/Card Drawer`）"
                           + "由 `ItemDrawer.Draw` 按奖励数据装配 ⇒ 本地没有奖励数据 ⇒ 只建空容器（不拿示例卡冒充）。");
            Debug.LogWarning("[ExpansionPass] `Help Icon Player` = `RaidHeaderController.eventHelpButton`："
                           + "原版 `ClickEventHelpButton` 要 `HasComponent(事件, 0x1e)` 才开远端帮助窗 ⇒ "
                           + "本地没有该事件组件 ⇒ 点击只出声。");
            Debug.LogWarning("[ExpansionPass] 第二页（`Expansion Pass Missions Tab`）：原版 `RefreshMissions` 在"
                           + "**没有任何可做任务时直接 `WindowTabBase.CloseTab`**（没数据就没有这个 tab）；"
                           + "本件的验收口径 = **两页都能打开 + 空态出声** ⇒ 保留 Missions 页、任务卡数值一律 `--`。"
                           + "任务列表/进度/奖励/计时/骷髅里程碑全部来自远端 `Missions` handler ⇒ 本地只有结构。");
            Debug.LogWarning("[ExpansionPass] Missions 页的 `warlordImage` = liveops `LoadAsset(事件,2)`、"
                           + "`Daily Skulls` 等任务卡的 `MissionContainer.SetChallenge` 数据也在远端 ⇒ 只建节点/空态；"
                           + "领取（`MainMenu/General/Collect`）与任务卡跳转都**只出声**。");
            if (_artNotes.Count > 0)
                Debug.LogWarning("[ExpansionPass] 有 " + _artNotes.Count + " 条图/件是**未画或降级**："
                                 + string.Join("、", _artNotes.ToArray()));
        }

        // ============================================================ 建整棵树（顺序 = 原版兄弟序）
        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            ArtNotes.Clear();
            _tabButtonsList.Clear(); _tabHi.Clear(); _tabLabels.Clear();
            _shade = _contentArea = _background = _tabButtons = _tabShadow = _tabs = _tabMaster = null;
            _page = _pageBg = _pageBgImage = _premiumPanel = _scroll = _viewport = _content = _header = null;
            _whereBtn = _debug = _container = _personalBar = null;
            _missionsPage = null; _activePage = 0;
            _title = _timeTitle = _time = _npTitle = _npSubtitle = _npButtonText = _pTitle = null;
            _extraPoints = _tabLabel = null;
            _mtTitle = _mtTime = _mtWeeklyName = _mtSkullName = null;

            BuildShell();
            BuildTabBar();
            BuildPage();
            ShowPage(0);
            BuildShellTail();

            if (MissingArt.Count > 0)
                Debug.LogWarning("[ExpansionPass] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
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
                                         ParentEventWindow.BgGradAngle);
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

            // 运行期页签（原版 `AddTabButton(label, sprite)`；Pass 与 Missions 各一颗）
            // ⚠️ 原版页签文字 = `LiveOpsAssetUtility.GetLabel(事件)`（远端）；Pass 用官方词条 `…/TabTitle`、
            //    Missions 用官方通用词条 `MainMenu/General/Missions` —— **两者都是我们挑的兜底**（见文件头）。
            var labels = new[] { Loc.T(TermTabTitle), Loc.T(TermMissionsTab) };
            var names = new[] { TabButtonName, MissionsTabButtonName };
            for (int i = 0; i < labels.Length; i++)
            {
                float top = ParentEventWindow.TabTop + ParentEventWindow.TabH * i;
                var tab = BuildTabButton(names[i], top, labels[i], false);
                _tabButtonsList.Add(tab);
                if (i == 0) _tabLabel = _tabLabels.Count > 0 ? _tabLabels[_tabLabels.Count - 1] : null;
                // 原版 `EverguildToggle.allowClickWhenAlreadySelected = 0` ⇒ 已选中时**连按钮音一起跳过**。
                int idx = i;
                var tabHit = MenuDraw.Hit(tab, "Hit",
                                          new PxRect(ParentEventWindow.TabButtonsR.x1, top,
                                                     ParentEventWindow.TabButtonsR.x2, top + ParentEventWindow.TabH),
                                          QHit, () => ShowPage(idx), null, null, null, null, null,
                                          clickCue: WindowSounds.DefaultButtonCue);
                var wb = tabHit != null ? tabHit.GetComponent<WindowButton>() : null;
                if (wb != null)
                {
                    wb.allowClickWhenAlreadySelected = false;
                    wb.isOn = () => _activePage == idx;
                }
            }
        }

        /// <summary>切页（原版 `WindowTabBase` 的两页互斥 `SetActive`）。页节点在 `BuildPage` 里建，
        /// 本方法由 `Build()` 在建完之后调一次、之后由页签点击调用。</summary>
        public void ShowPage(int idx)
        {
            _activePage = idx;
            if (_page != null) _page.gameObject.SetActive(idx == 0);
            if (_missionsPage != null) _missionsPage.gameObject.SetActive(idx == 1);
            for (int i = 0; i < _tabHi.Count; i++)
                if (_tabHi[i] != null) _tabHi[i].gameObject.SetActive(i == idx);
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

        // ============================================================ `Expansion Pass Tab` 页（原版页根：x 163.79..1920.21）
        void BuildPage()
        {
            // `Tabs`（原版 `TabbedWindowComponents.tabHolder`）—— 兄弟序在 `Tab Buttons` 之后、`Shadow (1)` 之前
            _tabs = MenuDraw.Node(_contentArea, "Tabs", ParentEventWindow.TabsR);
            _page = MenuDraw.Node(_tabs, PageNodeName, R(0f, 0f, PageW, PageH));

            // 兄弟序（prefab `m_Children`）：Background Black → Background Image → Purchase premium Panel →
            //                            Rewards Scroll View → Header → Generic Simplified UI Button (1) → Debug Add points
            BuildPageBackground(_page);
            BuildPremiumPanel(_page);
            BuildRewardsScroll(_page);
            BuildHeader(_page);
            BuildWhereGetCardsButton(_page);
            BuildDebugAddPoints(_page);

            // ---- 第二页：`Expansion Pass Missions Tab`（原版是本承载窗里的第二个 tab）----
            _missionsPage = MenuDraw.Node(_tabs, MissionsPageNodeName, R(0f, 0f, PageW, PageH));
            BuildMissionsPage(_missionsPage);
        }

        // ------------------------------------------------------------ `Background Black` / `Background Image`
        void BuildPageBackground(Transform page)
        {
            // `Background Black`：`Image`(sprite=null · Sliced · ppuMul 0.01 · 纯黑 α1) —— 没有 sprite 时
            // uGUI 仍按 `m_Color` 画满（原版整页底色就是它）⇒ 我们用实色 quad。
            _pageBg = MenuDraw.Node(page, "Background Black", PageBgR);
            MenuDraw.Rect(_pageBg, CardArt.Solid(), PageBgR, "Image", QPageBg, new Color(0f, 0f, 0f, 1f));

            // `Background Image`：**运行时** `LoadAsset(事件, 1)` 灌图（`Tiled` · ppuMul 1.12），
            // 取不到时原版把 **`Image` 组件** `set_enabled(false)`（⚠️ 不是关 GO —— `m_IsActive` 仍是 true）
            // ⇒ 本件**只建节点、不出图**，节点保持 active（同原版：GO 在、图不画）。
            _pageBgImage = MenuDraw.Node(_pageBg, "Background Image", PageBgImageR);
            Note("页背景图 `Background Image` = liveops `LoadAsset(事件,1)`（`Tiled` · ppuMul 1.12）"
                 + " —— 本地没有 ⇒ 只建节点、不出图（原版取不到 sprite 时把 `Image` 组件 `set_enabled(false)`）");
        }

        // ------------------------------------------------------------ `Purchase premium Panel`（付费档面板）
        void BuildPremiumPanel(Transform page)
        {
            _premiumPanel = MenuDraw.Node(page, "Purchase premium Panel", PremiumPanelR);

            // `Background`（`WF_Campaign_Info_Background` 740×167 九宫 335,0,395,0 · Sliced）
            MenuDraw.Nine(_premiumPanel, Tex(ArtCampaignInfoBg, "付费档面板背景"), PremiumBgR,
                          new Vector4(335f, 0f, 395f, 0f), 740f, 167f, QPanelBg, Color.white, true, "Background");

            // 两张 premium 卡预览（`initialPremiumRewardAnchor` / `lastPremiumRewardAnchor`）。
            // 原版由 `ItemDrawer.Draw` 按 `Milestones.First/Last` 的 premium 奖励装配真实卡面；
            // 本地没有奖励数据 ⇒ **只建空容器**（⛔ 不拿 prefab 里的示例卡当真实奖励）。
            BuildPremiumCardPreview("Last Premium Reward", LastPremiumR, LastCardDrawerR, -17.50f, 9.94f);
            BuildPremiumCardPreview("Fisrt Premium Reward", FirstPremiumR, FirstCardDrawerR, 4.83f, -1.98f);
            Note("两张 premium 卡预览（`Fisrt/Last Premium Reward/Card Drawer`）：原版 `ItemDrawer.Draw` 按"
                 + "奖励数据装配真实卡面 —— 本地没有奖励数据 ⇒ 只建空容器（不拿示例卡冒充）");

            BuildNotPremiumBranch();
            BuildPremiumBranch();
        }

        /// <summary>卡预览组：组 + `Card Drawer`（`CardDrawer` 运行时装配卡面）。`Card Drawer` 的旋转是
        /// prefab `m_LocalRotation` 实读（组与抽屉各自带一个）。</summary>
        void BuildPremiumCardPreview(string name, PxRect groupR, PxRect drawerR, float groupRotZ, float drawerRotZ)
        {
            var g = MenuDraw.Node(_premiumPanel, name, groupR);
            g.localRotation = Quaternion.Euler(0f, 0f, groupRotZ);      // prefab `m_LocalRotation` 实读
            var d = MenuDraw.Node(g, "Card Drawer", drawerR);
            d.localRotation = Quaternion.Euler(0f, 0f, drawerRotZ);     // prefab `m_LocalRotation` 实读
        }

        /// <summary>`IsNotPremium`（`playerIsNotPremiumObjects`）—— 未购买态：Title / Subtitle / `Get now!` 钮。
        /// <para>🔴 出厂 INACT，但本地**没有 `HasPremium`** ⇒ 我们默认展示这一支（见文件头「有意偏离」）。</para></summary>
        void BuildNotPremiumBranch()
        {
            var np = MenuDraw.Node(_premiumPanel, "IsNotPremium", IsNotPremiumR);

            _npTitle = MenuDraw.TextBox(np, NpTitleR, Loc.T(TermTitle), Color.white, "Title",
                                        42f, 10f, QPanelText, 42f, 46f, face: TmpFont.Face.LatinUi);
            if (_npTitle != null)
            {
                _npTitle.SetWrapping(false);
                _npTitle.SetCharSpacing(-3f);
                MenuDraw.SetVAlign(_npTitle, Label.VAlign.Midline, NpTitleR);
            }

            _npSubtitle = MenuDraw.TextBox(np, NpSubtitleR, Loc.T(TermSubtitle), new Color(0.717f, 0.717f, 0.717f, 1f),
                                           "Subtitle", 30f, 10f, QPanelText, 30f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_npSubtitle != null)
            {
                _npSubtitle.SetWrapping(false);
                MenuDraw.SetVAlign(_npSubtitle, Label.VAlign.Midline, NpSubtitleR);
            }

            // `Generic Simplified UI Button`（`UI_Button_Mulligan` 九宫 333,96,333,96 · Sliced · ppuMul 0.01）
            //  ⚠️ ppuMul 0.01 = Unity 对 0 的钳位值；`CreateNineSlice` 会逐轴夹到不超过半宽/半高
            //     ⇒ 与传 `m_Border` 原值画出来逐位相同（同 `RaidEventWindow` 的 `Collect Button` 那条推导）。
            var btn = MenuDraw.Node(np, "Generic Simplified UI Button", NpButtonR);
            MenuDraw.Nine(btn, Tex(ArtMulliganBtn, "付费档入口按钮底"), NpButtonR,
                          new Vector4(333f, 96f, 333f, 96f), 410f, 124f, QPanelBtn, Color.white, true, "Image");
            _npButtonText = MenuDraw.TextBox(btn, NpButtonTextR, Loc.T(TermGetNow), Color.white, "Button Text",
                                             55f, 10f, QPanelBtnText, 55f, 12f, face: TmpFont.Face.LatinCardBody);
            if (_npButtonText != null)
            {
                _npButtonText.SetWrapping(false);
                MenuDraw.SetVAlign(_npButtonText, Label.VAlign.Capline, NpButtonTextR);
            }
            MenuDraw.Nine(btn, Tex(ArtGlowBorder, "付费档入口高亮"), NpHighlightR,
                          new Vector4(50f, 50f, 50f, 50f), 128f, 128f, QPanelHi, Color.white, true, "Highlight");
            // 原版 `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` → `GetComponentReference(premiumOfferData, 0xc)`
            // → `WindowsManager.OpenWindow(...)`。**那个窗口引用在远端 premium 报价数据里**，本地拿不到
            // ⇒ 只出声，⛔ 不猜成 `PurchasePremiumWindow` / 某个 `BaseOfferPopup` 变体。
            MenuDraw.Hit(btn, "Hit", NpButtonR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] **买付费档**需要服务器：原版 "
                               + "`ExpansionPassPurchasePremiumPanel.PurchasePremiumClick` → "
                               + "`LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)` → "
                               + "`WindowsManager.OpenWindow(...)`；本地拿不到 premium 报价与那个窗口引用 ⇒ "
                               + "**不假装打开购买窗**。"),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);
        }

        /// <summary>`Is Premium`（`playerIsPremiumObjects`）—— 已购买态：Title / Extra points message。
        /// <para>本地没有 `HasPremium` ⇒ 建成**关着**（默认展示 `IsNotPremium`，见文件头「有意偏离」）。</para></summary>
        void BuildPremiumBranch()
        {
            var pr = MenuDraw.Node(_premiumPanel, "Is Premium", IsPremiumR);

            _pTitle = MenuDraw.TextBox(pr, PTitleR, Loc.T(TermYouArePremium), Color.white, "Title",
                                       52f, 10f, QPanelText, 52f, 46f, face: TmpFont.Face.LatinUi);
            if (_pTitle != null)
            {
                _pTitle.SetWrapping(false);
                _pTitle.SetCharSpacing(-3f);
                MenuDraw.AlignLeft(_pTitle, PTitleR);
                MenuDraw.SetVAlign(_pTitle, Label.VAlign.Middle, PTitleR);
            }

            var msg = MenuDraw.Node(pr, "Extra points message", ExtraMsgR);

            // `Icon Expansion Pass Points Drawer Variant`（`ExpansionPassPointDrawer`）
            //  prefab 的 `Content` 是 1080×1080 布局框 × `UIScaleToFit 0.05848` ⇒ 视觉 63.16×63.16；
            //  本壳没有 USF ⇒ 直接按视觉框建（中心不变）。
            var drawer = MenuDraw.Node(msg, "Icon Expansion Pass Points Drawer Variant", ExtraIconR);
            var content = MenuDraw.Node(drawer, "Content", Center(ExtraIconR, 63.16f, 63.16f));
            // `Campaign Glow`：原版 `40K_general_icon_Expansion Pass XP`（布局 42.09 × 1.5 = 视觉 63.13）
            //  —— 这张图**没导入 `Resources/`**（源 = `Art/原版/0_mainmenu/40K_general_icon_Expansion_Pass_XP.png`）
            //  ⇒ 不画、出声（⛔ 不用语义不同的点数图标冒充）。
            MenuDraw.Node(content, "Campaign Glow", Center(ExtraIconR, 63.13f, 63.13f));
            Note("`Campaign Glow`：原版 `40K_general_icon_Expansion Pass XP` 未导入 `Resources/`"
                 + "（源 = `Assets/CardPresentation/Art/原版/0_mainmenu/40K_general_icon_Expansion_Pass_XP.png`）"
                 + " ⇒ 这一件没画（⛔ 不用别的点数图标冒充）");
            // `Image`：运行期 `ExpansionPassPointDrawer.DrawForArmy(army)` 按阵营灌图（出厂那张 Orks 是示例）
            MenuDraw.Node(content, "Image", Center(ExtraIconR, 63.16f, 63.16f));
            Note("`ExpansionPassPointDrawer/DrawForArmy(army)` 的阵营图：prefab 出厂是 Orks 示例"
                 + "（`40k_DeckSelection_icon_FactionOrks`），运行期按玩家阵营换 —— 本地没有阵营 ⇒ 不画");
            // 两个 INACT 抽屉（`40k_main_bt_nametag`）：内部价签是数据驱动的 ⇒ 只建节点、关着。
            var conv = MenuDraw.Node(content, "Converted Drawer", Center(ExtraIconR, 49.02f, 5.00f));
            MenuDraw.Rect(conv, Tex("40k_main_bt_nametag", "换算抽屉名牌"), Center(ExtraIconR, 49.02f, 5.00f),
                          "Image", QPanelDecor, Color.white);
            conv.gameObject.SetActive(false);
            var eph = MenuDraw.Node(content, "Ephemeral Drawer", Center(ExtraIconR, 49.02f, 5.00f));
            MenuDraw.Rect(eph, Tex("40k_main_bt_nametag", "临时抽屉名牌"), Center(ExtraIconR, 49.02f, 5.00f),
                          "Image", QPanelDecor, Color.white);
            eph.gameObject.SetActive(false);

            // `Extra points`（`extraPointsText`）：原版 `String.Format(GetTranslation(ExtraPercentaje), PremiumPercentageBonus)`
            //  ⇒ 百分比在远端 ⇒ `--` 占位（⛔ 不编 25%）。
            _extraPoints = MenuDraw.TextBox(msg, ExtraPointsR,
                                            string.Format(Loc.T(TermExtraPercentaje), NoData),
                                            new Color(0.718f, 0.718f, 0.718f, 1f), "Extra points",
                                            36.55f, 18f, QPanelText, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_extraPoints != null)
            {
                MenuDraw.AlignLeft(_extraPoints, ExtraPointsR);
                MenuDraw.SetVAlign(_extraPoints, Label.VAlign.Midline, ExtraPointsR);
            }

            pr.gameObject.SetActive(false);   // 见 doc：本地 `HasPremium` 未知 ⇒ 默认不展示这一支
        }

        // ------------------------------------------------------------ `Rewards Scroll View`（= `ExpansionPassRewardSelector`）
        void BuildRewardsScroll(Transform page)
        {
            _scroll = MenuDraw.Node(page, "Rewards Scroll View", ScrollR);
            Note("`Rewards Scroll View`：原版 `ScrollRect`（`m_Horizontal = 1` · `m_Vertical = 0` · `m_MovementType = 1`）"
                 + " —— 本壳没有滚动运行时、且本地没有里程碑数据 ⇒ 只建结构（视口裁切 + 内容轨）");

            _viewport = ViewportClip.Hang(_scroll, "Viewport", ScrollR, Vector4.zero, Vector2Int.zero).transform;
            // `Viewport` 的 `UIMask` 图 **alpha = 0**（不可见）⇒ 不画；裁切已由 `ViewportClip` 承担（如实记录）。

            _content = MenuDraw.Node(_viewport, "Rewards Content", ContentR);

            // `Expansion Pass Reward Container` = `ExpansionPassRewardSelector.rewardItemContainer` 的模板
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
            // ⚠️ 原版运行期被 `RaidHeaderController.Initialize` 的 `GetLabel(事件)` 覆盖 ⇒ 照 prefab 出厂原文。
            var titleNode = MenuDraw.Node(_header, "Title", TitleR);
            _title = MenuDraw.TextBox(titleNode, TitleR, PrefabTitleText, Color.white, "Title",
                                      50f, 18f, QHeaderText, 50f, 50f, face: TmpFont.Face.LatinUi);
            if (_title != null)
            {
                MenuDraw.AlignLeft(_title, TitleR);
                MenuDraw.SetVAlign(_title, Label.VAlign.Midline, TitleR);
            }

            // `Help Icon Player`（**挂在 `Title` 下**，与 dump 兄弟序一致；= `RaidHeaderController.eventHelpButton`）
            // 原版 `ClickEventHelpButton`：`HasComponent(事件, 0x1e)` 才开那个**远端帮助窗** ⇒ 本地只出声。
            MenuDraw.Rect(titleNode, Tex(ArtHelpInfo, "活动帮助图标"), HelpIconPlayerR, "Help Icon Player",
                          QHeaderIcon, Color.white);
            // `EverguildButton` 那一颗的 `m_RaycastPadding` 未逐字段读 ⇒ 命中区按图标矩形本身（不外扩）。
            MenuDraw.Hit(titleNode, "Help Icon Player Hit", HelpIconPlayerR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] `Help Icon Player` = `RaidHeaderController.eventHelpButton`："
                               + "原版 `ClickEventHelpButton` 要 `LiveOpsAssetUtility.HasComponent(事件, 0x1e)` "
                               + "才打开那个远端帮助窗；本地没有该事件组件 ⇒ **不开窗**。"),
                null, null, null, null, null, clickCue: WindowSounds.DefaultButtonCue);

            // `Army Icon`（`Image` · `m_Sprite = null`，运行期 `LoadAsset(事件,5)` 灌图）
            MenuDraw.Node(_header, "Army Icon", ArmyIconR);
            Note("`Army Icon` = liveops `LoadAsset(事件,5)` —— 本地没有 ⇒ 只建节点、不出图");

            // `Time Remaining`（`Localize` = `Ends in:`；原版宽 0 + CSF ⇒ 用 300 宽的右对齐框，右缘 = 原版 pivot）
            var tr = MenuDraw.Node(_header, "Time Remaining", TimeTitleR);
            _timeTitle = MenuDraw.TextBox(tr, TimeTitleR, Loc.T(TermEndsIn), Color.white, "Time Remaining",
                                          36f, 0f, QHeaderText, 36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_timeTitle != null)
            {
                MenuDraw.AlignRight(_timeTitle, TimeTitleR);
                MenuDraw.SetVAlign(_timeTitle, Label.VAlign.Midline, TimeTitleR);
            }
            MenuDraw.Rect(tr, Tex(ArtClock, "倒计时时钟"), TimeClockR, "Time image", QHeaderIcon, ClockTint);
            // `Time`（`SupportMethods.GetTimeStringFromDate(事件日期)` ⇒ 本地没有日期 ⇒ `--`）
            _time = MenuDraw.TextBox(tr, TimeR, NoData, Color.white, "Time", 36f, 0f, QHeaderText,
                                     36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_time != null)
            {
                _time.SetWrapping(false);
                MenuDraw.AlignLeft(_time, TimeR);
                MenuDraw.SetVAlign(_time, Label.VAlign.Midline, TimeR);
                TimerDisplay.Attach(_time, null, "", "ExpansionPass/Header/Timer",
                                    "Expansion Pass 活动起止时刻在远端 LiveOps（`LiveOpsUtility.GetDateForEventStage`），本地没有 ⇒ `--`");
            }
            // ⚠️ `gracePeriodTooltip` 在**本 prefab 里是 NULL**（`RaidHeaderController` MB 实读）⇒ 宽限期 ⓘ **不存在**，
            //    ⛔ 别照 `RaidEventWindow` 补一颗（那是 Raid 自己的 prefab 才有）。
        }

        // ------------------------------------------------------------ `Generic Simplified UI Button (1)`（Where get cards）
        void BuildWhereGetCardsButton(Transform page)
        {
            _whereBtn = MenuDraw.Node(page, "Generic Simplified UI Button (1)", WhereBtnR);
            MenuDraw.Nine(_whereBtn, Tex(ArtBtn, "Where get cards 钮底"), WhereBtnR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QWhereBtn, WhereBtnTint, true, "Image",
                          new Vector4(234f / 1.51f, 46f / 1.51f, 234f / 1.51f, 46f / 1.51f));
            var txt = MenuDraw.TextBox(_whereBtn, WhereBtnTextR, Loc.T(TermWhereButton), Color.white, "Button Text",
                                       55f, 10f, QWhereBtnText, 55f, 12f, face: TmpFont.Face.LatinCardBody);
            if (txt != null)
            {
                txt.SetWrapping(false);
                MenuDraw.SetVAlign(txt, Label.VAlign.Capline, WhereBtnTextR);
            }
            // 原版 `ExpansionPassEventTab.GetCardsClick` → `WindowsManager.OpenWindow(getCardsWindow, config)`。
            // 🔴 本地有对应窗口（`WhereToGetCardsPopup`）⇒ 真接，⛔ 不是空按钮（同 Gacha / Raid 口径）。
            MenuDraw.Hit(_whereBtn, "Hit", WhereBtnR, QHit, () =>
            {
                var wm = WindowsManager.Instance;
                if (wm != null) WindowsManager.OpenWhereToGetCards();
                else Debug.LogWarning("[ExpansionPass] `Where get cards`：没有 `WindowsManager` ⇒ 开不了 "
                                    + "`WhereToGetCardsPopup`（原版那一跳是 `ExpansionPassEventTab.GetCardsClick` → "
                                    + "`WindowsManager.OpenWindow`）。");
            }, null, null, null, null, null, clickCue: WindowSounds.PositiveCue);
        }

        // ------------------------------------------------------------ `Debug Add points`（**出厂 INACT** · 调试件）
        void BuildDebugAddPoints(Transform page)
        {
            _debug = MenuDraw.Node(page, "Debug Add points", DebugAddPointsR);
            MenuDraw.Nine(_debug, Tex(ArtBtn, "调试按钮底"), DebugAddPointsR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QDebug, DebugBtnTint, true, "Image");
            var dt = MenuDraw.TextBox(_debug, DebugTextR, "Debug add points", Color.white, "Text (TMP)",
                                      24f, 18f, QDebug, 24f, 24f, face: TmpFont.Face.LatinCardBody);
            if (dt != null) MenuDraw.SetVAlign(dt, Label.VAlign.Middle, DebugTextR);
            // `InputField (TMP)`：输入框（出厂字 `5`）—— 调试用，照建、跟着父件一起关着。
            var inp = MenuDraw.Node(_debug, "InputField (TMP)", DebugInputR);
            MenuDraw.Rect(inp, CardArt.Solid(), DebugInputR, "Background", QDebug, new Color(0f, 0f, 0f, 0.5f));
            var it = MenuDraw.TextBox(inp, DebugInputR, "5", Color.white, "Text", 51.4f, 18f, QDebug,
                                      51.4f, 51.4f, face: TmpFont.Face.LatinCardBody);
            if (it != null) MenuDraw.SetVAlign(it, Label.VAlign.Middle, DebugInputR);
            _debug.gameObject.SetActive(false);  // prefab 出厂 INACT
        }

        // ============================================================ `Expansion Pass Reward Container`（模板 · 出厂关着）
        void BuildRewardContainer(Transform root)
        {
            // ---- `Level up`（容器兄弟序 0 · 出厂 ACTIVE）----
            var lv = MenuDraw.Node(root, "Level up", LevelUpR);
            MenuDraw.Rect(lv, Tex(ArtClosedChatBg, "升级条底"), LevelUpBgR, "Background", QCardBg, LevelUpBgTint);
            MenuDraw.Rect(lv, Tex(ArtArrow, "升级箭头"), LevelUpArrowR, "Arrow", QCardArt, ArrowTint);

            var lvBtn = MenuDraw.Node(lv, "Price Display Button (1)", LevelUpBtnR);
            var lvGui = MenuDraw.Node(lvBtn, "Generic UI Button", LevelUpBtnR);
            MenuDraw.Rect(lvGui, Tex(ArtBtn, "升级按钮底"), LevelUpBtnR, "Image", QCardBtn, LevelUpBtnTint);
            // `Button Text` 出厂 INACT（原版是按钮的备用文本）⇒ 建出来、关着
            var lvBt = MenuDraw.TextBox(lvGui, LevelUpBtnTextR, "", Color.white, "Button Text",
                                        12f, 12f, QCardText, 38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (lvBt != null) { MenuDraw.SetVAlign(lvBt, Label.VAlign.Midline, LevelUpBtnTextR); lvBt.gameObject.SetActive(false); }
            // `Price Display`（价格 = 远端 ⇒ `--`；货币图标运行时灌）
            var lvPrice = MenuDraw.Node(lvGui, "Price Display", LevelUpPriceR);
            MenuDraw.Node(lvPrice, "icon", LevelUpPriceIconR);
            var lvPriceText = MenuDraw.TextBox(lvPrice, LevelUpPriceTextR, NoData, Color.white, "text",
                                               40f, 13.46f, QCardText, 40f, 39f, face: TmpFont.Face.LatinCardBody);
            if (lvPriceText != null) MenuDraw.SetVAlign(lvPriceText, Label.VAlign.Capline, LevelUpPriceTextR);
            // `Text level up`（`Localize` = `Event_Description/ExpansionPass/LevelUp`）
            var lvText = MenuDraw.TextBox(lv, LevelUpTextR, Loc.T(TermLevelUp), Color.white, "Text level up",
                                          34f, 10f, QCardText, 34f, 36f, face: TmpFont.Face.LatinCardBody);
            if (lvText != null)
            {
                MenuDraw.AlignRight(lvText, LevelUpTextR);
                MenuDraw.SetVAlign(lvText, Label.VAlign.Middle, LevelUpTextR);
            }
            // 升级 = `ExpansionPassEvent.PurchaseLevelUp`（服务器）⇒ 点击只出声
            MenuDraw.Hit(lvBtn, "Hit", LevelUpBtnR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] **升级**需要服务器：原版 `ExpansionPassEvent.PurchaseLevelUp`"
                               + "（`ExpansionPassRewardItemContainer.levelUpPriceDisplay.OnClick`）；"
                               + "本地没有后端 ⇒ **不假装升级**。"),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // ---- `Premium Glow`（`40k_Rewards_icon_base card SDF` · 视觉框 408.90×506.71）----
            // 这张图**没导入 `Resources/`**（源 = `Art/原版/0_mainmenu/40k_Rewards_icon_base_card.png`，
            // 而 prefab 用的是 `..._card SDF` 变体）⇒ 只建节点、不画。
            MenuDraw.Node(root, "Premium Glow", PremiumGlowR);
            Note("`Premium Glow`：原版 `40k_Rewards_icon_base card SDF` 未导入 `Resources/`"
                 + "（源 = `Assets/CardPresentation/Art/原版/0_mainmenu/40k_Rewards_icon_base_card.png`）⇒ 这一件没画");

            // ---- `Collect Highlight Effect`（`Border_Line_FX` 九宫 50 · ppuMul 0.5 · 图 alpha 实读 = 0）----
            var hi = MenuDraw.Node(root, "Collect Highlight Effect", CollectHiR);
            MenuDraw.Nine(hi, Tex(ArtGlowBorder, "领取高亮框"), CollectHiR,
                          new Vector4(50f, 50f, 50f, 50f), 128f, 128f, QCardGlow, CollectHiTint, true, "Image",
                          new Vector4(50f / 0.5f, 50f / 0.5f, 50f / 0.5f, 50f / 0.5f));

            // ---- `RewardTransform`（`ExpansionPassRewardDrawerController`）----
            var rt = MenuDraw.Node(root, "RewardTransform", RewardTransformR);
            MenuDraw.Rect(rt, Tex(ArtTicket, "额外奖励图标"), ExtraRewardIconR, "Icon Extra rewards",
                          QCardReward, Color.white);
            Note("`RewardTransform`：原版 `ExpansionPassRewardDrawerController`（`ItemDrawer`）按 `RewardInfo` 逐件实例化"
                 + " —— 本地没有里程碑奖励数据 ⇒ 只建空容器 + 那张固定的 `Icon Extra rewards`");

            // ---- `Personal Progression`（进度条 + 领取钮）----
            BuildPersonalProgression(root);

            // ---- `CollisionButton`（`NonDrawingGraphic` + `UIGenericEventCatcher`：奖励卡的整块点击区）----
            MenuDraw.Node(root, "CollisionButton", CollisionR);
            // ---- `Culling Reference Object`（空容器）----
            MenuDraw.Node(root, "Culling Reference Object", CullingRefR);
        }

        void BuildPersonalProgression(Transform root)
        {
            var prog = MenuDraw.Node(root, "Personal Progression", PersonalProgR);
            _personalBar = MenuDraw.Node(prog, "Expansion Pass Progress Bar", PersonalBarRootR);

            // `Bar`（`Slider` · 出厂填充 0 宽）：底 + 填充 + 端帽
            var bar = MenuDraw.Node(_personalBar, "Bar", PersonalBarR);
            MenuDraw.Rect(bar, Tex(ArtArmyTrackBar, "战令进度条底"), PersonalBarBgR, "ProgressionBG",
                          QCardProgressBg, Color.white);
            var fill = MenuDraw.Node(bar, "ProgressionBar", PersonalBarFillR);   // 出厂 0 宽 = 进度 0
            MenuDraw.Rect(fill, Tex(ArtArmyTrackFill, "战令进度条填充"), PersonalBarFillR, "Image",
                          QCardBar, BarFillTint);
            MenuDraw.Rect(fill, Tex(ArtArmyTrackPos, "战令进度条端帽"), PersonalBarEndR, "BarEnd",
                          QCardBar, Color.white);

            // `Points`（HLG）：`icon`（**原版就没有 `Image` 组件**，运行时由 `ItemDrawer` 灌）+ `text`
            var pts = MenuDraw.Node(_personalBar, "Points", PersonalPointsR);
            MenuDraw.Node(pts, "icon", PersonalPointsIconR);
            var t = MenuDraw.TextBox(pts, PersonalPointsTextR, NoData + "/" + NoData, Color.white, "text",
                                     39.3f, 13.46f, QCardText, 40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (t != null)
            {
                t.SetWrapping(false);
                MenuDraw.SetVAlign(t, Label.VAlign.Capline, PersonalPointsTextR);
            }

            // `Claim Button Container`（`ExpansionPassProgressClaimButtonController`）
            // 出厂 = `Claim Button` ACTIVE · `Buy premium` / `Level` INACT（= stateDefinitions 里某一档的形态）；
            // 本地没有里程碑状态 ⇒ **不跑状态机**（⛔ 不假装某一档已解锁）。
            var cbc = MenuDraw.Node(prog, "Claim Button Container", ClaimContainerR);

            // `Claim Button`（`40K_button` · tint 蓝）
            var cb = MenuDraw.Node(cbc, "Claim Button", ClaimBtnR);
            MenuDraw.Rect(cb, Tex(ArtBtn, "领取按钮底"), ClaimBtnR, "Image", QCardBtn, ClaimBtnTint);
            var cbt = MenuDraw.TextBox(cb, ClaimBtnTextR, Loc.T(TermClaim), Color.white, "Button Text",
                                       41.1f, 12f, QCardText, 44f, 12f, face: TmpFont.Face.LatinCardBody);
            if (cbt != null) MenuDraw.SetVAlign(cbt, Label.VAlign.Midline, ClaimBtnTextR);
            MenuDraw.Nine(cb, Tex(ArtOctagon2, "领取钮高亮框"), ClaimHiR,
                          new Vector4(53f, 53f, 53f, 53f), 128f, 128f, QCardHi, Color.white, true,
                          "Highlight button claim");
            // 领奖 = `ExpansionPassEvent.OnRewardCollected`（服务器）⇒ 点击只出声
            MenuDraw.Hit(cb, "Hit", ClaimBtnR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] **领奖**需要服务器：原版 `ExpansionPassProgressClaimButtonController`"
                               + " → `ExpansionPassEvent.OnRewardCollected`（LiveOps）；本地没有后端 ⇒ **不假装领取**。"),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // `Buy premium`（出厂 INACT；`stateDefinitions` 的 state 1 才显示，点击弹 `BuyPremiumToClaim` 报错）
            var bp = MenuDraw.Node(cbc, "Buy premium", ClaimBtnR);
            MenuDraw.Rect(bp, Tex(ArtBtn, "买付费档按钮底"), ClaimBtnR, "Image", QCardBtn, ClaimBtnTint);
            var bpt = MenuDraw.TextBox(bp, ClaimBtnTextR, Loc.T(TermClaim), Color.white, "Button Text",
                                       38f, 12f, QCardText, 38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (bpt != null) MenuDraw.SetVAlign(bpt, Label.VAlign.Midline, ClaimBtnTextR);
            MenuDraw.Hit(bp, "Hit", ClaimBtnR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] " + Loc.T(TermBuyPremiumToClaim)), null, null, null, null, null,
                clickCue: WindowSounds.DefaultButtonCue);
            bp.gameObject.SetActive(false);

            // `Level`（`ProgressRewardLevelObject` · `levelText` 实读 = NULL ⇒ 只有底图与锁）
            var lvl = MenuDraw.Node(cbc, "Level", LevelR);
            MenuDraw.Rect(lvl, Tex(ArtMilestone, "里程碑底座"), LevelR, "Image", QCardBtn, Color.white);
            MenuDraw.Rect(lvl, Tex(ArtLock, "锁图标"), LockR, "Lock", QCardHi, LockTint);
            lvl.gameObject.SetActive(false);
        }

        // ============================================================ 第二页 `Expansion Pass Missions Tab`（原版第二个 liveops tab）
        // 出处：`_tmp_view/ep_Expansion_Pass_Missions_Tab.txt`（menu_dump `--depth 16 --relative` 现读，240 行）
        // + 类桩 `ExpansionPassMissionsTab.cs` + 反编译 `ExpansionPassMissionsTab__{Initialize,OnOpen,ToFocus,Reset,SetEventTab,RefreshMissions}.c`。
        // 🔴 原版 `RefreshMissions` 在**没有可做任务时直接 `WindowTabBase.CloseTab`**（没数据就没有这个 tab）；
        //    本任务口径 = 两页都能打开 + 空态出声 ⇒ 保留页面、任务卡数值一律 `--`。
        void BuildMissionsPage(Transform page)
        {
            // `Event Image`（= `warlordImage` 字段，`LoadAsset(事件, 2)` 灌图）
            MenuDraw.Node(page, "Event Image", R(764.3f, 49.1f, 1843.7f, 1128.5f));
            Note("Missions 页 `Event Image`（= `ExpansionPassMissionsTab.warlordImage`）= liveops `LoadAsset(事件,2)`"
                 + " —— 本地没有 ⇒ 只建节点、不出图");

            BuildMissionsHeader(page);
            BuildDailyMissions(page);
            BuildDailySkulls(page);
        }

        // ------------------------------------------------------------ Missions 页 `Header`（= `RaidHeaderController`）
        void BuildMissionsHeader(Transform page)
        {
            var h = MenuDraw.Node(page, "Header", R(0f, -0.1f, 1756.4f, 181.9f));

            // `Background`（`Image` 无 sprite · m_Color = (0,0,0,1) ⇒ 纯黑条）
            var bg = MenuDraw.Node(h, "Background", R(0.2f, 23.1f, 1756.6f, 87.2f));
            MenuDraw.Rect(bg, CardArt.Solid(), R(0.2f, 23.1f, 1756.6f, 87.2f), "Image", QHeaderIcon,
                          new Color(0f, 0f, 0f, 1f));

            // `Title`（fs50 / base50 / auto[18,50] / Left-Midline；运行期 `GetLabel(事件)`）
            var tr = R(139f, 23.1f, 1451.6f, 86.7f);
            var titleNode = MenuDraw.Node(h, "Title", tr);
            _mtTitle = MenuDraw.TextBox(titleNode, tr, PrefabTitleText, Color.white, "Title",
                                        50f, 18f, QHeaderText, 50f, 50f, face: TmpFont.Face.LatinUi);
            if (_mtTitle != null)
            {
                MenuDraw.AlignLeft(_mtTitle, tr);
                MenuDraw.SetVAlign(_mtTitle, Label.VAlign.Midline, tr);
            }
            // `Help Icon Player (1)`（**出厂 INACT** · `EverguildTooltipTrigger`；⚠️ term 实读 = **Raid 那条**）
            var helpR = R(1399.4f, 28.8f, 1451.6f, 81.0f);
            BuildMissionTooltipIcon(titleNode, "Help Icon Player (1)", helpR, TermRaidIndividualTooltip, 30,
                                    Vector3.zero, false);

            // `Army Icon`（无 sprite，运行期 `LoadAsset(事件,5)`）
            MenuDraw.Node(h, "Army Icon", R(5f, 0.9f, 125f, 120.9f));

            // `Time Remaining`（原版宽 0 + CSF，pivot 右缘 1595.4；本壳用 300 宽右对齐框 —— **我们挑的框**）
            var timeTitleR = R(1295.4f, 29.9f, 1595.4f, 79.9f);
            var tn = MenuDraw.Node(h, "Time Remaining", timeTitleR);
            var tt = MenuDraw.TextBox(tn, timeTitleR, Loc.T(TermEndsIn), Color.white, "Time Remaining",
                                      36f, 0f, QHeaderText, 36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (tt != null)
            {
                MenuDraw.AlignRight(tt, timeTitleR);
                MenuDraw.SetVAlign(tt, Label.VAlign.Midline, timeTitleR);
            }
            MenuDraw.Rect(tn, Tex(ArtClock, "倒计时时钟"), R(1595.4f, 30.1f, 1645f, 79.7f), "Time image",
                          QHeaderIcon, ClockTint);
            var timeR = R(1645f, 29.9f, 1755.3f, 79.9f);
            _mtTime = MenuDraw.TextBox(tn, timeR, NoData, Color.white, "Time", 36f, 0f, QHeaderText,
                                       36f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_mtTime != null)
            {
                _mtTime.SetWrapping(false);
                MenuDraw.AlignLeft(_mtTime, timeR);
                MenuDraw.SetVAlign(_mtTime, Label.VAlign.Midline, timeR);
                TimerDisplay.Attach(_mtTime, null, "", "ExpansionPass/Missions/Header/Timer",
                                    "Expansion Pass 起止时刻在远端 LiveOps，本地没有 ⇒ `--`");
            }
            // ⚠️ 本 prefab 的 `RaidHeaderController.eventHelpButton` 也是 **NULL**（MB 实读）；
            //    那颗 `Help Icon Player (1)` 只是 tooltip trigger 且**出厂 INACT** ⇒ 不接点击。
        }

        // ------------------------------------------------------------ `Daily Missions`（周常卡列表 + 卡片模板）
        void BuildDailyMissions(Transform page)
        {
            var dm = MenuDraw.Node(page, "Daily Missions", R(55.8f, 448.8f, 887.1f, 1020.7f));

            var holder = MenuDraw.Node(dm, "Daily Missions Holder", R(55.8f, 511.3f, 887.1f, 1020.7f));
            // `Daily Mission Container Ref` = `missionContainerReference`（运行时按任务逐件 Instantiate；
            // 无任务数据 ⇒ 建一份模板、**关着**；原版 `RefreshMissions` 末尾也会把模板 SetActive(false)）
            var tpl = MenuDraw.Node(holder, "Daily Mission Container Ref", R(55.8f, 511.3f, 887.1f, 668.7f));
            BuildWeeklyMissionCard(tpl);
            tpl.gameObject.SetActive(false);

            // `Mission Header`（本地只是标题条 + INACT 的 ⓘ；渐变按 `Gradient2` 实读色键）
            var mh = MenuDraw.Node(dm, "Mission Header", R(58.5f, 451.0f, 887.1f, 503.8f));
            MenuDraw.Rect(mh, MissionHeaderGradient(), R(58.5f, 451.0f, 887.1f, 503.8f), "Image",
                          QCardArt, Color.white);
            var nameR = R(83.4f, 452.4f, 754.5f, 502.4f);
            _mtWeeklyName = MenuDraw.TextBox(mh, nameR, Loc.T(TermWeeklyChallenges), Color.white, "name",
                                             30f, 20f, QCardText, 30f, 46f, face: TmpFont.Face.LatinCardBody);
            if (_mtWeeklyName != null)
            {
                _mtWeeklyName.SetWrapping(false);
                MenuDraw.AlignLeft(_mtWeeklyName, nameR);
                MenuDraw.SetVAlign(_mtWeeklyName, Label.VAlign.Capline, nameR);
            }
            // `info`（**出厂 INACT**；prefab 里是 `localize=0` 的**示例英文**，运行期会被任务表覆盖 ⇒ 不画）
            var infoR = R(836.1f, 456.9f, 877.1f, 497.9f);
            var info = MenuDraw.Node(mh, "info", infoR);
            MenuDraw.Rect(info, Tex(ArtHelpInfo, "周常说明图标"), infoR, "Image", QCardArt, Color.white);
            info.gameObject.SetActive(false);
            Note("Missions 页 `Mission Header/info` 出厂 INACT ⇒ 只建节点、关着（prefab 里的示例英文不画）");
        }

        /// <summary>`MissionContainer` 的**周常卡**（`Daily Mission Container Ref`）：全节点按 dump 建，
        /// 数据驱动栏（描述/计时/奖励计数/进度）一律 `--`。**模板关着**，不参与画面。</summary>
        void BuildWeeklyMissionCard(Transform root)
        {
            var cardR = R(55.8f, 511.3f, 887.1f, 668.7f);
            MenuDraw.Rect(root, Tex(ArtMissionHorizontal, "任务卡底"), cardR, "background", QCardBg,
                          new Color(0.4941f, 0.5686f, 0.9176f, 1f));

            // `title`（出厂 INACT）+ `description` + `timer`（三者同一块区域，由 displayRule 互斥）
            var titleR = R(93.2f, 524.0f, 522.1f, 586.2f);
            var title = MenuDraw.TextBox(root, titleR, NoData, Color.white, "title", 30f, 15f, QCardText,
                                         30f, 36f, face: TmpFont.Face.LatinCardBody);
            if (title != null)
            {
                MenuDraw.AlignLeft(title, titleR);
                MenuDraw.SetVAlign(title, Label.VAlign.Midline, titleR);
                title.gameObject.SetActive(false);
            }
            var descR = R(78.2f, 524.0f, 941.4f, 586.2f);
            var desc = MenuDraw.TextBox(root, descR, NoData, Color.white, "description", 40f, 15f, QCardText,
                                        40f, 36f, face: TmpFont.Face.LatinCardBody);
            if (desc != null)
            {
                MenuDraw.AlignLeft(desc, descR);
                MenuDraw.SetVAlign(desc, Label.VAlign.Midline, descR);
            }
            var timerR = R(78.2f, 524.0f, 751.6f, 586.2f);
            var timer = MenuDraw.TextBox(root, timerR, NoData, new Color(1f, 1f, 1f, 0.588f), "timer",
                                         35f, 15f, QCardText, 35f, 36f, face: TmpFont.Face.LatinCardBody);
            if (timer != null)
            {
                MenuDraw.AlignLeft(timer, timerR);
                MenuDraw.SetVAlign(timer, Label.VAlign.Midline, timerR);
            }

            // `Rewards`（`MissionRewardsDisplay` → `MissionRewardItem`）
            var rewR = R(350.2f, 575.6f, 592.7f, 666.7f);
            var rew = MenuDraw.Node(root, "Rewards", rewR);
            var itemR = R(350.2f, 575.6f, 592.7f, 666.7f);
            var item = MenuDraw.Node(rew, "Reward Display Mission", itemR);
            BuildMissionPointsDrawer(item, R(434.6f, 590.2f, 496.6f, 652.2f));
            var countR = R(508.3f, 575.6f, 560f, 666.7f);   // 原版宽 0（CSF）⇒ 我们给了 51.7 宽
            var count = MenuDraw.TextBox(item, countR, NoData, Color.white, "count", 49.68f, 20f, QCardText,
                                         49.68f, 36f, face: TmpFont.Face.LatinCardBody);
            if (count != null)
            {
                count.SetWrapping(false);
                MenuDraw.AlignLeft(count, countR);
                MenuDraw.SetVAlign(count, Label.VAlign.Midline, countR);
            }

            // `Mission Milestones Progress Bar`（`MissionProgressBarDisplay`）
            BuildMissionProgressBar(root, R(78.2f, 596.8f, 338.8f, 648.6f), R(83.7f, 637.0f, 338.8f, 648.6f),
                                    R(83.7f, 637.0f, 328.8f, 648.6f), R(77.9f, 637.4f, 89.5f, 659.8f),
                                    R(83.7f, 596.8f, 338.8f, 637.5f));

            // `Generic UI Button`（`MainMenu/General/Collect`）
            BuildMissionCollectButton(root, R(610.8f, 599.7f, 865.4f, 656.2f), R(617.8f, 604.6f, 858.4f, 651.4f),
                                      new Color(1f, 0.5280f, 0f, 1f), 45f, 12f, 45f);

            // `Generic Round Button`（`MissionReRollButton` · **出厂 INACT**）+ `Button Text`[INACT] + `Image`
            var rrR = R(824.6f, 606.9f, 873.7f, 656.2f);
            var rr = MenuDraw.Node(root, "Generic Round Button", rrR);
            MenuDraw.Rect(rr, Tex(ArtRoundYellow, "重掷按钮底"), rrR, "Image", QCardBtn,
                          new Color(1f, 0.7725f, 0.3333f, 1f), true);
            var rrt = MenuDraw.TextBox(rr, R(832.6f, 606.9f, 865.7f, 656.2f), "X", Color.white, "Button Text",
                                       30f, 12f, QCardText, 30f, 12f, face: TmpFont.Face.LatinCardBody);
            if (rrt != null) { MenuDraw.SetVAlign(rrt, Label.VAlign.Midline, R(832.6f, 606.9f, 865.7f, 656.2f)); rrt.gameObject.SetActive(false); }
            MenuDraw.Rect(rr, Tex(ArtRoundYellowDelete, "重掷删除图标"), R(824.6f, 607.9f, 871.7f, 655.2f),
                          "Image", QCardArt, Color.white, true);
            rr.gameObject.SetActive(false);

            // `Mission Debug Buttons`（调试件；原版 `MissionDebugButtons.Setup` 里 `SetActive(false)`）
            var dbgOn = new[]
            {
                (R(77.5f, 496.6f, 180.7f, 530.3f), "Reset", R(90.5f, 505.9f, 167.7f, 521.0f), "Reset", 16.75f),
                (R(180.7f, 496.6f, 284.0f, 530.3f), "Re-Roll", R(193.7f, 505.9f, 271.0f, 521.0f), "Re-Roll", 16.75f),
                (R(387.3f, 496.6f, 536.2f, 530.3f), "Complete", R(400.3f, 501.5f, 523.2f, 525.4f), "Complete", 26.15f),
            };
            BuildMissionDebugGroup(root, "Mission Debug Buttons", R(77.5f, 496.6f, 865.4f, 530.3f),
                                   R(284.0f, 496.6f, 387.3f, 530.3f), R(284.0f, 496.6f, 335.6f, 530.3f),
                                   R(335.6f, 496.6f, 387.3f, 530.3f),
                                   R(297.0f, 511.0f, 322.6f, 515.9f), R(348.6f, 511.0f, 374.3f, 515.9f),
                                   R(536.2f, 496.6f, 657.3f, 530.3f),
                                   dbgOn, false);
        }

        // ------------------------------------------------------------ `Daily Skulls Mission Container`（固定骷髅卡）
        void BuildDailySkulls(Transform page)
        {
            var root = MenuDraw.Node(page, "Daily Skulls Mission Container", R(55.8f, 150.2f, 887.1f, 420.8f));

            // `raycast target`（**出厂 INACT** · `Image` α=0）
            var ray = MenuDraw.Node(root, "raycast target", R(55.8f, 150.2f, 887.1f, 420.8f));
            ray.gameObject.SetActive(false);

            // `background`（原版 `Image` 组件 **m_Enabled=0** ⇒ 不画；保留节点）
            var bg = MenuDraw.Node(root, "background", R(55.8f, 150.2f, 887.1f, 420.8f));

            // `background Rotated`（`40K_missions_display_Daily vertical` · ppuMul 2 · **绕 z 转 90°**）
            // ⚠️ dump 的 AABB 与 rect 中心不重合（pivot 未重算）；本件按**中心枢轴**转，视觉上盖住整张卡
            //    —— 这是**我们挑的枢轴**（数据/画面都不依赖它，如实记在报告里）。
            var rotR = R(336.4f, -130.9f, 605.7f, 701.8f);
            var rot = MenuDraw.Node(bg, "background Rotated", rotR);
            rot.localRotation = Quaternion.Euler(0f, 0f, 90f);
            MenuDraw.Rect(rot, Tex(ArtMissionVertical, "骷髅卡底（旋转）"), rotR, "Image", QCardBg, Color.white);

            // `header`（`Gradient2` 渐变条）+ `name` + `info`（tooltip = `Missions/Info/DailySkulls`）
            var hdR = R(58.5f, 152.4f, 887.1f, 205.2f);
            var hd = MenuDraw.Node(bg, "header", hdR);
            MenuDraw.Rect(hd, MissionHeaderGradient(), hdR, "Image", QCardArt, Color.white);
            var nameR = R(83.4f, 153.8f, 754.5f, 203.8f);
            _mtSkullName = MenuDraw.TextBox(hd, nameR, Loc.T(TermDailySkulls), Color.white, "name",
                                            30f, 20f, QCardText, 30f, 46f, face: TmpFont.Face.LatinCardBody);
            if (_mtSkullName != null)
            {
                _mtSkullName.SetWrapping(false);
                MenuDraw.AlignLeft(_mtSkullName, nameR);
                MenuDraw.SetVAlign(_mtSkullName, Label.VAlign.Capline, nameR);
            }
            BuildMissionTooltipIcon(hd, "info", R(836.1f, 158.3f, 877.1f, 199.3f), TermDailySkullsInfo, 10,
                                    new Vector3(55f, 0f, 0f), true);

            // `body`：description[INACT] / Image（骷髅图） / counter / image[INACT]
            var body = MenuDraw.Node(bg, "body", R(117.6f, 222.0f, 394.6f, 347.2f));
            var descR = R(117.6f, 222.0f, 394.6f, 347.2f);
            var desc = MenuDraw.TextBox(body, descR, "", Color.white, "description", 30f, 15f, QCardText,
                                        30f, 36f, face: TmpFont.Face.LatinCardBody);
            if (desc != null) { MenuDraw.SetVAlign(desc, Label.VAlign.Midline, descR); desc.gameObject.SetActive(false); }
            MenuDraw.Rect(body, Tex(ArtSkullIcon, "骷髅图标"), R(256.1f, 211.3f, 406.1f, 357.9f), "Image",
                          QCardArt, Color.white, true);
            var counterR = R(129.8f, 227.9f, 280.4f, 341.3f);
            var counter = MenuDraw.TextBox(body, counterR, NoData, Color.white, "counter", 70f, 30f, QCardText,
                                           70f, 36f, face: TmpFont.Face.LatinCardBody);
            if (counter != null) MenuDraw.SetVAlign(counter, Label.VAlign.Midline, counterR);
            var imgInact = MenuDraw.Node(body, "image", R(159.7f, 237.3f, 381.5f, 362.5f));
            MenuDraw.Rect(imgInact, Tex(ArtSkullIcon, "骷髅小图"), R(159.7f, 237.3f, 381.5f, 362.5f), "Image",
                          QCardArt, Color.white, true);
            imgInact.gameObject.SetActive(false);

            // `progress` → `milestones` → `steps`（5 个里程碑点）
            var prog = MenuDraw.Node(bg, "progress", R(96f, 347.2f, 421f, 420.8f));
            var ms = MenuDraw.Node(prog, "milestones", R(96f, 347.2f, 421f, 420.8f));
            var steps = MenuDraw.Node(ms, "steps", R(96f, 347.2f, 421f, 420.8f));
            for (int i = 0; i < 5; i++)
            {
                float x = 118.5f + 60f * i;
                var stepR = R(x, 364f, x + 40f, 404f);
                var step = MenuDraw.Node(steps, "Mission Milestones Step" + (i == 0 ? "" : " (" + i + ")"), stepR);
                var hold = MenuDraw.Node(step, "holder", stepR);
                MenuDraw.Rect(hold, CardArt.Solid(), stepR, "Image", QCardBtn, new Color(0f, 0f, 0f, 1f));
                MenuDraw.Rect(hold, Tex(ArtCheckmark, "里程碑勾"), R(x + 21f, 335.6f, x + 61f, 377.9f),
                              "CheckMark", QCardHi, Color.white, true);
                var num = MenuDraw.TextBox(hold, stepR, NoData, Color.white, "text", 32f, 15f, QCardText,
                                           32f, 36f, face: TmpFont.Face.LatinCardBody);
                if (num != null) MenuDraw.SetVAlign(num, Label.VAlign.Midline, stepR);
            }

            // `footer`：Rewards（`Icon Currency Drawer Variant`）/ Collect / TimerHolder
            var ft = MenuDraw.Node(bg, "footer", R(471.5f, 216.6f, 796.5f, 398.5f));
            var rewR = R(507.9f, 216.6f, 832.9f, 294.2f);
            var rew = MenuDraw.Node(ft, "Rewards", rewR);
            var item = MenuDraw.Node(rew, "Reward Display Mission", rewR);
            BuildMissionCurrencyDrawer(item, R(621.8f, 216.6f, 699.4f, 294.2f));
            var countR = R(707.6f, 216.6f, 760f, 294.2f);   // 原版宽 0（CSF）⇒ 我们给了 52.4 宽
            var count = MenuDraw.TextBox(item, countR, NoData, Color.white, "count", 49.68f, 20f, QCardText,
                                         49.68f, 36f, face: TmpFont.Face.LatinCardBody);
            if (count != null)
            {
                count.SetWrapping(false);
                MenuDraw.AlignLeft(count, countR);
                MenuDraw.SetVAlign(count, Label.VAlign.Midline, countR);
            }
            BuildMissionCollectButton(ft, R(546f, 294.2f, 802f, 368.9f), R(559f, 309.2f, 789f, 353.9f),
                                      new Color(1f, 0.4740f, 0.0991f, 1f), 45f, 12f, 45f);
            var th = MenuDraw.Node(ft, "TimerHolder", R(507.9f, 368.9f, 832.9f, 426f));
            var tmR = R(507.9f, 362.9f, 832.9f, 420f);
            var tm = MenuDraw.TextBox(th, tmR, NoData, new Color(0.569f, 0.573f, 0.588f, 1f), "Timer",
                                      38f, 15f, QCardText, 38f, 36f, face: TmpFont.Face.LatinCardBody);
            if (tm != null) MenuDraw.SetVAlign(tm, Label.VAlign.Midline, tmR);

            // `debug_buttons`（**出厂 INACT** · 竖排）
            var dbgOn = new[]
            {
                (R(883.7f, 152.4f, 1015.3f, 195.2f), "Reset", R(896.7f, 163.5f, 1002.3f, 184.1f), "Reset", 22.6f),
                (R(883.7f, 200.2f, 1015.3f, 242.9f), "Re-Roll", R(896.7f, 211.2f, 1002.3f, 231.8f), "Re-Roll", 22.6f),
                (R(883.7f, 293.0f, 1015.3f, 335.7f), "Complete", R(896.7f, 304.1f, 1002.3f, 324.6f), "Complete", 22.6f),
            };
            var dbg = BuildMissionDebugGroup(bg, "debug_buttons", R(883.7f, 152.4f, 1015.3f, 288.7f),
                                            R(883.7f, 247.9f, 1015.3f, 288f), R(883.7f, 247.9f, 949.5f, 288f),
                                            R(949.5f, 247.9f, 1015.3f, 288f),
                                            R(896.7f, 264.1f, 936.5f, 271.8f), R(962.5f, 264.1f, 1002.3f, 271.8f),
                                            R(883.7f, 340.7f, 1015.3f, 380.8f),
                                            dbgOn, true);
            dbg.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ Missions 页的小件助手
        /// <summary>`Gradient2`（`_gradientType=0` Horizontal · `_blendMode=2`）的等效贴图 ——
        /// 色键 / alpha 键是 **MB 实读**（ctime 0→1；atime 17733/65535=0.2706、59367/65535=0.9059）。
        /// 与 `Shell/MissionsTab.HeaderGradient()` 同源同值（那一份是 private，本件按同一组常量重建）。</summary>
        static Texture2D MissionHeaderGradient()
        {
            return CardArt.GradientKeys(new Color(0.247f, 0.188f, 0.380f), new Color(0.475f, 0.306f, 0.153f),
                                        0f, new float[] { 0f, 0.2706f, 0.9059f, 1f },
                                        new float[] { 1f, 1f, 0.098f, 0.098f });
        }

        /// <summary>Missions 页的 ⓘ（`EverguildTooltipTrigger`）：悬停出 tooltip、点击不发音（与 Raid 那三颗同形）。</summary>
        void BuildMissionTooltipIcon(Transform parent, string name, PxRect r, string term, int anchor,
                                     Vector3 offset, bool active)
        {
            var icon = MenuDraw.Node(parent, name, r);
            MenuDraw.Rect(icon, Tex(ArtHelpInfo, name + "图标"), r, "Image", QCardArt, Color.white);
            var hit = MenuDraw.Hit(icon, "Hit", r, QHit, null);
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            if (wb != null)
            {
                var center = new Vector2(r.CX, r.CY);
                wb.onEnter = () =>
                {
                    var body = Loc.T(term);
                    if (!string.IsNullOrEmpty(body))
                        Tooltip.Show(body, LayoutSpace.FromPixel(center.x, center.y), anchor, offset);
                };
                wb.onExit = Tooltip.Hide;
                wb.playClickSound = false;
            }
            icon.gameObject.SetActive(active);
        }

        /// <summary>`ExpansionPassPointDrawer`（62×62 视觉框）—— 内容全部来自远端任务奖励 / 玩家阵营 ⇒ 只建空架。</summary>
        void BuildMissionPointsDrawer(Transform parent, PxRect r)
        {
            var d = MenuDraw.Node(parent, "Icon Expansion Pass Points Drawer Variant", r);
            var c = MenuDraw.Node(d, "Content", r);              // 原版 USF ×0.0574 ⇒ 视觉框=62×62
            MenuDraw.Node(c, "Campaign Glow", r);                // `40K_general_icon_Expansion Pass XP` 未导入 ⇒ 不画
            MenuDraw.Node(c, "Image", r);                        // `DrawForArmy(army)` 运行期灌图
            var conv = MenuDraw.Node(c, "Converted Drawer", Center(r, 49.02f, 5.00f));
            MenuDraw.Rect(conv, Tex("40k_main_bt_nametag", "换算抽屉名牌"), Center(r, 49.02f, 5.00f), "Image",
                          QCardText, Color.white);
            conv.gameObject.SetActive(false);
            var eph = MenuDraw.Node(c, "Ephemeral Drawer", Center(r, 49.02f, 5.00f));
            MenuDraw.Rect(eph, Tex("40k_main_bt_nametag", "临时抽屉名牌"), Center(r, 49.02f, 5.00f), "Image",
                          QCardText, Color.white);
            eph.gameObject.SetActive(false);
        }

        /// <summary>`CurrencyDrawer`（骷髅卡 footer 的奖励抽屉）：货币图 + 两个 INACT 抽屉；数量 `--` 在兄弟件上。</summary>
        void BuildMissionCurrencyDrawer(Transform parent, PxRect r)
        {
            var d = MenuDraw.Node(parent, "Icon Currency Drawer Variant", r);
            var c = MenuDraw.Node(d, "Content", r);
            MenuDraw.Rect(c, Tex(ArtTicket, "奖励货币图标"), r, "Image", QCardArt, Color.white, true);
            var conv = MenuDraw.Node(c, "Converted Drawer", Center(r, 49.02f, 5.00f));
            MenuDraw.Rect(conv, Tex("40k_main_bt_nametag", "换算抽屉名牌"), Center(r, 49.02f, 5.00f), "Image",
                          QCardText, Color.white);
            conv.gameObject.SetActive(false);
            var eph = MenuDraw.Node(c, "Ephemeral Drawer", Center(r, 49.02f, 5.00f));
            MenuDraw.Rect(eph, Tex("40k_main_bt_nametag", "临时抽屉名牌"), Center(r, 49.02f, 5.00f), "Image",
                          QCardText, Color.white);
            eph.gameObject.SetActive(false);
        }

        /// <summary>任务卡的进度条（`MissionProgressBarDisplay`）：底 + 0 填充 + INACT 手柄 + `--/--`。</summary>
        void BuildMissionProgressBar(Transform parent, PxRect rootR, PxRect barR, PxRect handleAreaR,
                                     PxRect handleR, PxRect progressR)
        {
            var root = MenuDraw.Node(parent, "Mission Milestones Progress Bar", rootR);
            var bar = MenuDraw.Node(root, "Progress Bar", barR);
            MenuDraw.Nine(bar, Tex(ArtBarEmpty, "任务进度条底"), barR,
                          new Vector4(4f, 4f, 4f, 4f), 12f, 12f, QCardProgressBg,
                          new Color(1f, 0.5926f, 0f, 1f), true, "Background",
                          new Vector4(2f, 2f, 2f, 2f));
            var fillR = new PxRect(barR.x1, barR.y2, barR.x1, barR.y2);   // 出厂 0×0 = 进度 0
            MenuDraw.Nine(bar, Tex(ArtBarFill, "任务进度条填充"), fillR,
                          new Vector4(4f, 4f, 4f, 4f), 12f, 12f, QCardBar,
                          new Color(1f, 0.7686f, 0.3333f, 1f), true, "Fill",
                          new Vector4(2f, 2f, 2f, 2f));
            var area = MenuDraw.Node(bar, "Handle Slide Area", handleAreaR);
            MenuDraw.Rect(area, CardArt.Solid(), handleR, "Handle", QCardBar,
                          new Color(0.9412f, 0.7255f, 0.3137f, 1f));
            area.gameObject.SetActive(false);
            var t = MenuDraw.TextBox(root, progressR, NoData + "/" + NoData, new Color(1f, 0.773f, 0.333f, 1f),
                                     "progress", 35f, 25f, QCardText, 35f, 36f, face: TmpFont.Face.LatinCardBody);
            if (t != null)
            {
                t.SetWrapping(false);
                MenuDraw.AlignLeft(t, progressR);
                MenuDraw.SetVAlign(t, Label.VAlign.Midline, progressR);
            }
        }

        /// <summary>任务卡的领取钮（`MainMenu/General/Collect`）；点击 = 服务器领取 ⇒ 只出声。</summary>
        void BuildMissionCollectButton(Transform parent, PxRect btnR, PxRect textR, Color tint,
                                       float fontPx, float minPx, float maxPx)
        {
            var b = MenuDraw.Node(parent, "Generic UI Button", btnR);
            MenuDraw.Nine(b, Tex(ArtBtn, "领取按钮底"), btnR, new Vector4(234f, 46f, 234f, 46f), 489f, 107f,
                          QCardBtn, tint, true, "Image");
            var t = MenuDraw.TextBox(b, textR, Loc.T(TermCollect), Color.white, "Button Text",
                                     fontPx, minPx, QCardText, maxPx, 12f, face: TmpFont.Face.LatinCardBody);
            if (t != null)
            {
                t.SetWrapping(false);
                MenuDraw.SetVAlign(t, Label.VAlign.Midline, textR);
            }
            MenuDraw.Hit(b, "Hit", btnR, QHit, () =>
                Debug.LogWarning("[ExpansionPass] **任务领取**需要服务器：原版 `MissionContainer.collectButton` → "
                               + "任务表 handler（LiveOps）；本地没有任务数据 ⇒ **不假装领取**。"),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);
        }

        /// <summary>`MissionDebugButtons`（Reset / Re-Roll / +1 / +5 / Complete / 倒计时）—— 调试件，
        /// **不接点击**（原版 `Setup()` 里整组关掉；本件照 prefab 建、由调用方决定显隐）。</summary>
        Transform BuildMissionDebugGroup(Transform parent, string name, PxRect groupR, PxRect incGroupR,
                                         PxRect incOneR, PxRect incHalfR, PxRect incOneTextR, PxRect incHalfTextR,
                                         PxRect clockR,
                                         (PxRect r, string node, PxRect textR, string text, float fs)[] rows,
                                         bool vertical)
        {
            var g = MenuDraw.Node(parent, name, groupR);
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i].r;
                var b = MenuDraw.Node(g, rows[i].node, row);
                MenuDraw.Nine(b, Tex(ArtBtn, "调试按钮底"), row, new Vector4(234f, 46f, 234f, 46f), 489f, 107f,
                              QCardBtn, new Color(1f, 0f, 0.8098f, 1f), true, "Image",
                              new Vector4(117f, 23f, 117f, 23f));
                var t = MenuDraw.TextBox(b, rows[i].textR, rows[i].text, Color.white, "Button Text",
                                         rows[i].fs, 12f, QCardText, 38f, 12f, face: TmpFont.Face.LatinCardBody);
                if (t != null) MenuDraw.SetVAlign(t, Label.VAlign.Midline, rows[i].textR);
            }
            var incG = MenuDraw.Node(g, "GameObject", incGroupR);
            var one = MenuDraw.Node(incG, "Increase one", incOneR);
            MenuDraw.Nine(one, Tex(ArtBtn, "调试 +1"), incOneR, new Vector4(234f, 46f, 234f, 46f), 489f, 107f,
                          QCardBtn, new Color(1f, 0f, 0.8098f, 1f), true, "Image",
                          new Vector4(117f, 23f, 117f, 23f));
            var oneT = MenuDraw.TextBox(one, incOneTextR, "+1", Color.white, "Button Text", 12f, 12f, QCardText,
                                        38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (oneT != null) MenuDraw.SetVAlign(oneT, Label.VAlign.Midline, incOneTextR);
            var half = MenuDraw.Node(incG, "Increase half", incHalfR);
            MenuDraw.Nine(half, Tex(ArtBtn, "调试 +5"), incHalfR, new Vector4(234f, 46f, 234f, 46f), 489f, 107f,
                          QCardBtn, new Color(1f, 0f, 0.8098f, 1f), true, "Image",
                          new Vector4(117f, 23f, 117f, 23f));
            var halfT = MenuDraw.TextBox(half, incHalfTextR, "+5", Color.white, "Button Text", 12f, 12f, QCardText,
                                         38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (halfT != null) MenuDraw.SetVAlign(halfT, Label.VAlign.Midline, incHalfTextR);
            var clock = MenuDraw.Node(g, "GameObject (1)", clockR);
            var ck = MenuDraw.TextBox(clock, clockR, "00:00", new Color(1f, 0f, 0.76f, 1f), "Text (TMP)",
                                      vertical ? 42.3f : 35.5f, 18f, QCardText, 72f, 36f,
                                      face: TmpFont.Face.LatinCardBody);
            if (ck != null) MenuDraw.SetVAlign(ck, Label.VAlign.Middle, clockR);
            return g;
        }


        /// <summary>以 `r` 的中心为心、换成 `w×h`（dump 的「视觉框」列用这个落位）。</summary>
        static PxRect Center(PxRect r, float w, float h)
        {
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        }

        void Note(string s)
        {
            if (_artNotes.Contains(s)) return;
            _artNotes.Add(s);
            Debug.Log("[ExpansionPass] " + s);
        }

        /// <summary>取图 —— **取不到必须出声**（红线；`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。</summary>
        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[ExpansionPass] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`）。");
            }
            return t;
        }
    }
}
