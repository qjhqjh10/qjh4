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
//    **领奖 / 升级**：原版分别是 `ExpansionPassEvent.OnRewardCollected` / `PurchaseLevelUp`（服务器事务）⇒ 本地点击**只出声**；
//    **买付费档** = `PurchasePremiumClick → OpenWindow(远端窗口引用)` ⇒ 见 `Shell/ExpansionPassPremiumOffers.cs`（🔶 本地报价表 + 两条关窗回调）。
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
    /// <para>✅ **2026-10-23 起有生产入口了**：左竖导航第 6 颗 `Event Navigation Button` →
    /// `WindowsManager.OpenEventHub(...)`；本窗左栏的「事件钮」（<see cref="ParentEventWindow.SetupEventTabs"/>
    /// 建的）也走同一个口。直接调用 = `WindowsManager.OpenExpansionPassEvent()`。</para></summary>
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
        // 🔶 自己设计（非原版）：活动标题框 `TitleR`（宽 1312、左缘 133、高 63.66） · 原版宽 0 靠 ContentSizeFitter，我们没有运行时 CSF（原版判据拿不到） · TODO(谁定的)｜原注：（左缘 = 原版 133、高度 = 原版 63.66）—— **这是我们挑的框**，逐条注在报告里。
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
        /// 🔶 自己设计（非原版）：Missions 页签兜底词条 `MainMenu/General/Missions` · 原版取远端 `LiveOpsAssetUtility.GetLabel(事件)` · TODO(谁定的)｜原注：本条是官方表里那条通用「Missions / 布道所」—— **我们挑的兜底**，⛔ 不冒充原版。</summary>
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
        // 🆕 2026-10-23（活动线 `L03`）：**事件钮**（原版 `BaseParentEventWindow.SetupTabs` 在任何一扇
        //   活动窗的左栏都列全部 live 事件 ⇒ 战令这扇也要有）。**与内容页那三张表分家**
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
        /// <summary>🆕 2026-10-23：**事件钮**（`SetupTabs` 建的那几颗；**不含**内容页签，兄弟序 = 事件名单序）。</summary>
        public List<Transform> EventTabNodes { get { return _eventTabs; } }
        public List<Transform> EventTabHighlights { get { return _eventHi; } }
        public List<Label> EventTabLabels { get { return _eventLabels; } }
        /// <summary>`SetupTabs` 那一声出声的原文（自检读口）。</summary>
        public string EventTabNote { get; private set; }
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
            if (mgr == null) Debug.LogWarning("[ExpansionPass] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器（由调用方自己开；本方法**不再自开**）。");   // ⛔ B103_R1（AUD03 #120）：原来 `if (mgr != null) mgr.OpenWindow(win); else { …; win.Open(); }` 与唯一调用方 `WindowsManager.OpenByRef` 的 `:940` 撞成**首开双 Build**。原版 `Create` 不自开。
            // （原兜底 `win.Open()` 一并去掉：那条会先建一遍树，与 `OpenByRef` 那条路重复。）
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
            _tabOverflowWarned = false;                      // 🆕 事件钮的「溢出左栏」告警一次一建（同 `ParentEventWindow`）
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
            // 🆕 2026-10-23（活动线 `L03`）：左栏的「事件钮」（原版 `BaseParentEventWindow.SetupTabs` 在
            //   **每一扇**活动窗的左栏都列全部 live 事件）。兄弟序 = 内容页在前、事件钮在后。
            SetupTabs(ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.ExpansionPass,
                                                       LiveOpsLocalCatalog.NowUtc));

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

        /// <summary>🆕 2026-10-23（活动线 `L03`）：**建左栏的「事件钮」** —— 与 `ParentEventWindow`
        /// **走同一份判据**（`ParentEventWindow.SetupEventTabs` / `BuildEventTabs`；本类只提供自己的
        /// `BuildTabButton` 与自己的渲染队列档，⛔ 不在这里再抄一份 `SetupTabs` 的实现）。
        /// <para>内容页占 2 颗（Pass / Missions）⇒ 事件钮从第 3 格（`TabTop + TabH * 2`）往下排。</para></summary>
        public int SetupTabs(IList<ParentEventWindow.EventTab> tabs)
        {
            string note;
            int n = ParentEventWindow.SetupEventTabs(
                tabs, 2,
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
            // → `WindowsManager.OpenWindow(...)`。那个窗口引用在**远端** premium 报价数据里 ⇒ 🔶 用本地报价表顶替，
            // 落在 `ExpansionPassPremiumOffers`（`WindowsManager.OpenPurchasePremiumWindow` 那一跳）。
            // 🆕 2026-10-29（`B97_REMOTE1`）：整段在 `PurchasePremiumClick()`（告警带事件 id + 两条关窗回调），这里只留绑定。
            MenuDraw.Hit(btn, "Hit", NpButtonR, QHit, () => PurchasePremiumClick(),
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
            _extraIconImage = MenuDraw.Rect(content, CardArt.Solid(), Center(ExtraIconR, 63.16f, 63.16f), "Image", QPanelDecor, Color.clear);  // 原版这格 `m_Sprite = null`（画前无图）⇒ 透明占位，`DrawForArmy` 时换图（等价，见该方法 doc）
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
            _container.gameObject.SetActive(false); InitializeEventTab();
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
                if (wm != null) GetCardsClick();
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
            // 原版 `levelUpPriceDisplay.OnClick` → `Initialize` 里挂的 lambda → `onLevelUp(milestoneIndex)`
            // → `ExpansionPassRewardSelector.OnLevelUp` → `scoringEvent.PurchaseLevelUp(...)`。
            // 索引按节点反查（`MilestoneIndexOfTransform`：本容器在 `_containers` 里的下标）；
            // 模板那一份不在 `_containers` 里 ⇒ 下标 -1 ⇒ 出声、不假装升级。
            MenuDraw.Hit(lvBtn, "Hit", LevelUpBtnR, QHit, () => OnLevelUpClick(lvBtn),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // ---- `Premium Glow`（`40k_Rewards_icon_base card SDF` · 视觉框 408.90×506.71）----
            // 🆕 2026-10-30（B101_R3）：图已由 `B100_F3`（B6）导进 `Resources/Art/ui_menu/40k_Rewards_icon_base_card_SDF.png`。
            //   原版（`bundle_menus_assets_all/GameObject/Premium Glow.json` 现读）：Simple(type 0) · 白 · **非 PA** · enabled=1；显隐 = `m.IsPremium`
            MenuDraw.Rect(root, Tex("40k_Rewards_icon_base_card_SDF", "Premium Glow"), PremiumGlowR, "Premium Glow", QCardArt, null, false);
            // （B101_R3 已删）原 `Note("…未导入 Resources/ ⇒ 这一件没画")`：图已在盘上，节点现在**真的画图**了；
            //   显隐仍由下方 `ApplyRewardDrawer` 那句 `FindDeep("Premium Glow").SetActive(m.IsPremium)` 驱动（未改动）；⛔ 别拿 `40k_Rewards_icon_base_card.png`（非 SDF 那张）顶。

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
            // 🔴 **2026-10-25（执行代理 E6 · 账 `未分类_其他` #292）**：矩形不再写死 `PersonalBarRootR` ——
            //   原版这颗节点上挂着 `ExpansionPassProgressBarScaleAndPositionController`（会按里程碑档改位置/缩放），
            //   等价物 = `PersonalBarRect(...)`（判据与算式见那个方法的 doc）。
            //   ⚠️ 本地没有里程碑数据 ⇒ 三个实参恒 `(Locked, Locked, 0)` ⇒ 该函数**返回原矩形**（恒等），
            //   与改之前逐像素相同；真数据到了走 `ApplyMilestoneState` 一条就生效。
            _personalBar = MenuDraw.Node(prog, "Expansion Pass Progress Bar",
                                         PersonalBarRect(PersonalBarRootR, MilestoneStateCurrent,
                                                         MilestoneStatePrevious, MilestoneLevel));

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
            // 原版 `ExpansionPassProgressClaimButtonController.CollectionClick` → base（发 `onCollect` = 容器的 `OnCollect`）
            // → `RewardSelector.CollectRewardClicked(index)` → `PurchaseMilestone`。
            // 索引按节点反查（见类尾 `CollectionClickOn` 的 doc）；无数据时只出声。
            MenuDraw.Hit(cb, "Hit", ClaimBtnR, QHit, () => CollectionClickOn(cb),
                null, null, null, null, null, clickCue: WindowSounds.PositiveCue);

            // `Buy premium`（出厂 INACT；`stateDefinitions` 的 state 1 才显示，点击弹 `BuyPremiumToClaim` 报错）
            var bp = MenuDraw.Node(cbc, "Buy premium", ClaimBtnR);
            MenuDraw.Rect(bp, Tex(ArtBtn, "买付费档按钮底"), ClaimBtnR, "Image", QCardBtn, ClaimBtnTint);
            var bpt = MenuDraw.TextBox(bp, ClaimBtnTextR, Loc.T(TermClaim), Color.white, "Button Text",
                                       38f, 12f, QCardText, 38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (bpt != null) MenuDraw.SetVAlign(bpt, Label.VAlign.Midline, ClaimBtnTextR);
            // 原版 `ExpansionPassProgressClaimButtonController.BuyPremiumClick` = `UIMessageController.ShowError(词条, localize:true)`
            MenuDraw.Hit(bp, "Hit", ClaimBtnR, QHit, BuyPremiumClick, null, null, null, null, null,
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
            BuildDailySkulls(page); RefreshMissionsLocal();
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
            MenuDraw.Rect(mh, MissionHeaderGradient((503.8f - 451.0f) / (887.1f - 58.5f)), R(58.5f, 451.0f, 887.1f, 503.8f), "Image",
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
            MenuDraw.Rect(hd, MissionHeaderGradient(hdR.H / hdR.W), hdR, "Image", QCardArt, Color.white);
            var nameR = R(83.4f, 153.8f, 754.5f, 203.8f);
            _mtSkullName = MenuDraw.TextBox(hd, nameR, Loc.T(TermDailySkulls), Color.white, "name",
                                            30f, 20f, QCardText, 30f, 46f, face: TmpFont.Face.LatinCardBody);
            if (_mtSkullName != null)
            {
                _mtSkullName.SetWrapping(false);
                MenuDraw.AlignLeft(_mtSkullName, nameR);
                MenuDraw.SetVAlign(_mtSkullName, Label.VAlign.Capline, nameR);
            }
            // 🔴 **2026-10-19（`W57` UI-4 · `N73` 顺手发现的那条 `108×` 量纲错，本笔修）**：
            //   原版 `Daily Skulls Mission Container ▸ background ▸ header ▸ info` 的
            //   `EverguildTooltipTrigger` 现读 = `tooltipAnchor **10**` · `offset = **(55, 0, 0) 画布 px**`；
            //   而 `Tooltip.Show` 的 `offset` 与 `worldPos` **同量纲 = 世界单位**（`Core/Tooltip.cs` 的
            //   `_pos = worldPos + offset`）⇒ 画布 px 必须过 `LayoutSpace.Px`（先例 =
            //   `Shell/CampaignTab.AddInfoTooltip` 的 `LayoutSpace.Px(-80f)` · `BattleDriver.TickTooltipAt` 的 `/ TipPx`）。
            //   改前直接传 `55f` ⇒ 面板被推 **55 × 108 = 5940 设计 px**，整块飞出屏幕。
            BuildMissionTooltipIcon(hd, "info", R(836.1f, 158.3f, 877.1f, 199.3f), TermDailySkullsInfo, 10,
                                    new Vector3(LayoutSpace.Px(55f), 0f, 0f), true);

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
        static Texture2D MissionHeaderGradient(float aspectHoverW)
        {
            return CardArt.GradientKeys(new Color(0.247f, 0.188f, 0.380f), new Color(0.475f, 0.306f, 0.153f),
                                        0f, new float[] { 0f, 0.2706f, 0.9059f, 1f },
                                        new float[] { 1f, 1f, 0.098f, 0.098f }, aspectHoverW);
        }

        /// <summary>Missions 页的 ⓘ（`EverguildTooltipTrigger`）：悬停出 tooltip、点击不发音（与 Raid 那三颗同形）。
        /// ⚠️ **`offset` 的单位 = 世界单位**（`Tooltip.Show` 里 `_pos = worldPos + offset`，与 `worldPos` 同量纲）
        /// ⇒ 原版 prefab 的画布 px 要在**调用点**自己过 `LayoutSpace.Px`（见 `BuildDailySkulls` 里那颗的注释）。</summary>
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

        // ============================================================ 个人进度条的「按档改位置/缩放」（账 `未分类_其他` #292）
        //
        // 【判据 · 两侧都到方法级】
        // ① 原版 `ExpansionPassProgressBarScaleAndPositionController.SetPositionAndScale(...)`
        //    （`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ExpansionPassProgressBarScaleAndPositionController.cs:7-26`，
        //     并与 Ghidra 那份交叉：`D:/2/tools/decomp_full/ExpansionPassProgressBarScaleAndPositionController__SetPositionAndScale.c:12-27`）：
        //      **仅当** `state == LockedBecauseNotPremium(1) || state == Unlocked(2)`：
        //          `rectTransform.anchoredPosition = positionToModify + positionOffset`
        //          `rectTransform.sizeDelta      = scaleToModify    + scaleOffset`
        //      （其余档 = 一个字段都不动）。
        // ② 基类 `BaseProgressBarScaleAndPositionController<TState>.Initialize(currentState, previousState, level)`
        //    （同目录 `BaseProgressBarScaleAndPositionController.cs:20-48`；⚠️ 泛型体在 cpp2il 里**实参名全丢**，
        //     这里是按语句顺序 + 子类语义还原的，标「还原」不标「实读」）：
        //      Ⅰ. 首次调用时把出厂矩形存进 `originalScale / originalPosition`（闸 = `originalScale == (0,0)`）；
        //      Ⅱ. **无条件**先按【上一档】摆一次：`SetPositionAndScale(originalPosition, originalScale, previousState, …Previous)`；
        //      Ⅲ. `if (level > 0)` 再按【当前档】在**Ⅱ 的结果上**叠加：`SetPositionAndScale(rt.anchoredPosition, rt.sizeDelta, currentState, …Current)`。
        // ③ 调用点（三个实参从哪来）= `D:/2/tools/decomp_full/ExpansionPassRewardItemContainer__Initialize.c:110`：
        //      `Initialize(GetState(milestone[milestoneIndex]), GetState(milestone[milestoneIndex-1]), milestoneIndex)`
        //      ⇒ `level` 就是**里程碑序号**；第 0 档走不到 Ⅲ（`previous` 取到 null ⇒ `Locked`）。
        // ④ 出厂值 = prefab `D:/2/tools/assetripper_out/ExportedProject/Assets/Expansion Pass Reward Container.prefab`
        //    的 MB `114699617135816954`（`m_Script` guid `8bd238dc…`）逐字段实读：
        //      `rectTransform` → 节点 **`Expansion Pass Progress Bar`**
        //        （`anchoredPosition (-37.067, 0)` · `sizeDelta (301.271, 106.574)` · `m_Pivot (1, 0.5)`）
        //      `positionOffsetCurrent (-74.34, 0)` · `scaleOffsetCurrent (-72.31, 0)`
        //      `positionOffsetPrevious (0, 0)`     · `scaleOffsetPrevious (-72.31, 0)`
        //    ⚠️ 我们 `PersonalBarRootR` 的**尺寸**逐位等于 `(301.271, 106.574)` ⇒ 同一来源，可反证。
        //
        // 【我们的等价物】原版改的是 `RectTransform`；我们这套没有 uGUI 布局，矩形就是设计 px。
        //   ⇒ 用**同一个算式**把矩形反算出来（函数 = `PersonalBarRect`，纯函数、可自检）。
        //   ⚠️ **如实标：非恒等档还差一步** —— 原版改 `sizeDelta` 时**子件按锚点跟着走**（pivot.x = 1 ⇒ 右缘不动、
        //   左缘与左锚子件动）；我们的 `Bar` / `Points` 是**绝对设计 px**摆的（`MenuDraw.Rect` 按绝对框），
        //   父件矩形一变它们**不跟着重排**。本地拿不到任何里程碑状态（`ExpansionPassEvent.Milestones` 是远端
        //   LiveOps 下发）⇒ 这一档**无法用实况验证**，因此**没有**凭空编一套子件重排；真数据到了再按 prefab
        //   子件锚点逐颗核（施工点就写在这儿）。

        /// <summary>`ScoringMilestone.State` 的四个值（`Everguild.LiveOps/ScoringMilestone.cs:9-15`）。
        /// ⚠️ 只有 `LockedBecauseNotPremium` 与 `Unlocked` 会让进度条挪位/改宽（见上面 ①）。</summary>
        public const int MsStateLocked = 0, MsStateLockedBecauseNotPremium = 1, MsStateUnlocked = 2, MsStateCollected = 3;

        /// <summary>原版两张偏移表（prefab 序列化实读，出处见上面 ④）。</summary>
        static readonly Vector2 BarPosOffCurrent = new Vector2(-74.34f, 0f), BarSizeOffCurrent = new Vector2(-72.31f, 0f),
                                BarPosOffPrevious = new Vector2(0f, 0f),    BarSizeOffPrevious = new Vector2(-72.31f, 0f);

        /// <summary>本窗「已知」的三个里程碑实参（= 原版 `Initialize` 的形参）。
        /// 本地没有里程碑数据 ⇒ 恒 `(Locked, Locked, 0)`（`ExpansionPassEvent.Milestones` 在远端 LiveOps）。</summary>
        public int MilestoneStateCurrent = MsStateLocked, MilestoneStatePrevious = MsStateLocked, MilestoneLevel = 0;

        static bool BarMovesFor(int state)
        {
            return state == MsStateLockedBecauseNotPremium || state == MsStateUnlocked;
        }

        /// <summary>把「位置偏移 + 尺寸偏移」落到我们的矩形上（= 原版那两行 `anchoredPosition/sizeDelta = ... + offset`）。
        /// <para>锚点按 prefab 的 **pivot = (1, 0.5)** 反算：`anchoredPosition` 指的是**右缘中心**
        /// ⇒ 位置偏移先挪右缘；`sizeDelta.x` 变而右缘不动 ⇒ 中心跟着挪 `−dw/2`。
        /// ⚠️ Unity 的 `y` 向上、我们的 <see cref="PxRect"/> **y 向下** ⇒ `dy` 取负（本表 dy 恒 0，写成通用式）。</para></summary>
        public static PxRect ShiftBar(PxRect r, Vector2 posOff, Vector2 sizeOff)
        {
            float w = r.W + sizeOff.x, h = r.H + sizeOff.y;
            float right = r.x2 + posOff.x;
            float cy = (r.y1 + r.y2) * 0.5f - posOff.y;
            float cx = right - w * 0.5f;
            return new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        }

        /// <summary>= 原版 `BaseProgressBarScaleAndPositionController<TState>.Initialize` 那三句（见上面 ②）：
        /// 先按**上一档**摆、`level > 0` 时再按**当前档**叠加。
        /// <para>`level == 0` ⇒ 只走「上一档」那一次（原版第 0 颗里程碑就是这样，`previous` 取到 null ⇒ `Locked` ⇒ 恒等）。</para></summary>
        public static PxRect PersonalBarRect(PxRect original, int currentState, int previousState, int level)
        {
            var r = original;
            if (BarMovesFor(previousState)) r = ShiftBar(r, BarPosOffPrevious, BarSizeOffPrevious);
            if (level > 0 && BarMovesFor(currentState)) r = ShiftBar(r, BarPosOffCurrent, BarSizeOffCurrent);
            return r;
        }

        /// <summary>给了里程碑状态之后：重摆「个人进度条」那一块（= 原版 `Initialize` 在 `ExpansionPassRewardItemContainer`
        /// 每次刷新时被调一次）。本地没有数据源 ⇒ 调用方只会在将来接上 LiveOps 时用它；
        /// ⛔ 别拿它传「猜出来的档」—— 状态必须来自真数据（红线：不假装某一档已解锁）。</summary>
        public void ApplyMilestoneState(int currentState, int previousState, int level)
        {
            MilestoneStateCurrent = currentState;
            MilestoneStatePrevious = previousState;
            MilestoneLevel = level;
            if (_container == null) return;
            // 复用的是同一棵模板子树（= 原版那件 `ExpansionPassRewardItemContainer` 的 prefab 实例）
            MenuDraw.ClearChildren(_container);
            BuildRewardContainer(_container);
            Debug.Log("[ExpansionPass] 里程碑档 ⇒ 当前 " + currentState + " · 上一档 " + previousState
                    + " · level " + level + " ⇒ 个人进度条矩形 = " + PersonalBarRect(PersonalBarRootR,
                        currentState, previousState, level));
        }

        // ════════════════════════════════════════════════════════════════════════════════════════════
        //  B15（2026-10-27）：`ExpansionPassEvent` 一族（51 条 C 行）的视图侧接线
        //
        //  【为什么追加在类尾】账里按 `文件:行号` 记落点（`:491` `:509` `:572` `:950` `:1026` `:1070`
        //    `:1134` `:1203` `:1210` `:1271` …）⇒ **新成员一律追加到类尾**，插在中间会让下游行号整体漂移
        //    （CLAUDE.md §三 那条「新成员追加到类尾」的实测代价：+6 行 ⇒ 10 条漂移 + 2 条失败）。
        //    类尾之后没有落点 ⇒ 加多少行都不动老账。
        //  【方法 ↔ 原版】逐条写在各自 doc 上；本块**只做视图/回调**，数据与状态机在 `Shell/ExpansionPassEvent.cs`。
        // ════════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>= 原版 `ExpansionPassRewardSelector.contentAnchor` 的 HLG 参数（prefab `Rewards Content` 实读）：
        /// `m_Padding.left = 122` · `m_Spacing = -130` · `m_ChildAlignment = 3`(MiddleLeft)。
        /// <para>逐档的位置 = `ContainerR.x1 + 122 + i * (ContainerW + ContainerSpacing)`（HLG 的排布式）。</para></summary>
        public const float ContainerSpacing = -130f;

        /// <summary>本窗当前的事件（= 原版 `ExpansionPassEventTab.currentEvent` / `ExpansionPassRewardSelector.scoringEvent`）。
        /// 本地常态是 `null`（里程碑 / 分数 / HasPremium 全在远端 LiveOps）。</summary>
        ExpansionPassEvent _event;

        /// <summary>= 原版 `ExpansionPassRewardSelector.currentRewardItemContainers`（运行时逐档 `Instantiate` 出来的那些）。</summary>
        readonly List<Transform> _containers = new List<Transform>();
        /// <summary>容器 → 档位下标（原版靠闭包里的 `milestoneIndex`；我们的命中回调只能拿到节点 ⇒ 反查这张表）。</summary>
        readonly Dictionary<Transform, int> _containerIndex = new Dictionary<Transform, int>();
        /// <summary>= 原版 `ExpansionPassRewardItemContainer.preparedForDelayedInit` + 那五个 `forLateInit_*` 字段。</summary>
        bool _lateInitPending;
        readonly List<int> _lateInitIndices = new List<int>();

        /// <summary>当前挂上的事件（自检读口；`null` = 没有数据 = 出厂态）。</summary>
        public ExpansionPassEvent CurrentEvent { get { return _event; } }
        /// <summary>当前奖励容器个数（= 原版 `currentRewardItemContainers.Count`；无数据时恒 0）。</summary>
        public int RewardContainerCount { get { return _containers.Count; } }

        /// <summary>Missions 页的任务源。**本地恒 `null`**（原版 `LiveOpsManager.GetHandler&lt;Missions&gt;()`）——
        /// 注意「源不存在」≠「源在但 0 条」：原版 `RefreshMissions` 对前者**整段跳过**（`if (!(handler == null))`），
        /// 只有后者才 `CloseTab()`。所以本地没有源时**不关页签**，那是复刻不是偏离。</summary>
        public MissionSource MissionsSource;

        /// <summary>`Missions` handler 的替身：一列任务（`ParentEventId` / `Active` / `Title`）。
        /// 原版 `IMission` 的其余成员（挑战进度 / 奖励 / 计时）本工程没有数据源。</summary>
        public sealed class MissionSource
        {
            public List<Mission> Missions = new List<Mission>();
        }

        /// <summary>`IMission` 的最小替身（只有 `RefreshMissions` 真读到的那两格 + 显示用的标题）。</summary>
        public sealed class Mission
        {
            public string ParentEventId;
            public bool Active = true;
            public string Title;
        }

        bool _noDataSaid, _missionsNoSourceSaid, _missionsClosed;

        // ------------------------------------------------------------ 数据入口

        /// <summary>= 原版 `ExpansionPassEventTab.SetEventTab(ILiveOps)` ＋ `ExpansionPassMissionsTab.SetEventTab(ILiveOps)`。
        /// <para>原版两句：① `currentEvent = liveOps`（含类型转换）② 空事件 ⇒ 清空。本窗是「一窗一类」硬绑
        /// （`Create` 直接 `AddComponent`）⇒ 这是**唯一**的数据入口。</para>
        /// <para>事件非空且树已建 ⇒ 立刻按新数据重建（= 原版 `SetEventTab` 之后由 `OnOpen` 调的 `Initialize`）。</para></summary>
        public void SetEventTab(ExpansionPassEvent ev)
        {
            _event = ev;
            if (_event == null)
            {
                ResetRewards();
                Note("[ExpansionPass] `SetEventTab(null)`：事件被清空 ⇒ 奖励容器全销毁（原版 `SetEventTab` 的「空事件时清空」那一支）");
                return;
            }
            if (_content != null) { InitializeEventTab(); RefreshMissionsLocal(); }
        }

        /// <summary>= 原版 `ExpansionPassEventTab.Initialize()`（`ExpansionPassEventTab__Initialize.c` 的五跳）：
        /// ① `rewardSelector.Initialize(currentEvent)` → ② `backgroundImage.sprite = LoadAsset(事件,1)` + `enabled = (sprite != null)`
        /// → ③ `premiumPanel.Initialize(currentEvent, onClosePurchasePopup)` → ④ `headerController.Initialize(currentEvent)`
        /// → ⑤ `getCardsButton.onClick += GetCardsClick` + 按远端配置显隐。
        /// <para>本地等价：①= <see cref="CreateRewards"/>；②本地没有 liveops 图（`LoadAsset` 走远端 Addressables）⇒ 节点在、不出图并出声；
        /// ③= <see cref="ApplyPremiumPanel"/>；④header 的 Title / Army Icon / 计时全在远端 ⇒ 只出声（⛔ 不覆盖 prefab 原文）；
        /// ⑤已在 `BuildWhereGetCardsButton` 里挂好（同一跳 `WindowsManager.OpenWhereToGetCards`）。</para>
        /// <para>🔴 `_event == null`（本地常态）⇒ 出声一次并返回，窗口保持「出厂结构 + 数值 `--`」—— 与改这一批之前逐像素相同。</para></summary>
        public void InitializeEventTab()
        {
            if (_content == null) return;                 // 还没建树（`Build()` 未跑）
            if (_event == null)
            {
                if (!_noDataSaid)
                {
                    _noDataSaid = true;
                    Debug.Log("[ExpansionPass] `ExpansionPassEventTab.Initialize`：`currentEvent` 为空（里程碑 / 分数 /"
                            + " `HasPremium` / 背景图全在远端 LiveOps，本机没有）⇒ 结构照建、数值 `--`、奖励容器 0 件。"
                            + "要验机制请先 `SetEventTab(<注入的事件>)`。");
                }
                return;
            }
            CreateRewards(false);
            ApplyPremiumPanel();
        }

        // ------------------------------------------------------------ ① `ExpansionPassRewardSelector`

        /// <summary>= 原版 `ExpansionPassRewardSelector.CreateRewards(bool refreshAllRewards)`（`ExpansionPassRewardSelector.cs:205-300`）：
        /// ① 旧容器非空 ⇒ `Reset()` ② 按 `Milestones` 逐件 `Instantiate(rewardItemContainer, contentAnchor)` 并塞进列表
        /// ③ **第 7 档起**（`index &gt;= 7`）且 `!refreshAllRewards` ⇒ `PrepareForLateInitialization`，否则直接 `Initialize`
        /// （判据 = ILSpy `:246-262` 的 `flag3 = num2 &gt;= 7` 与 `flag4 = refreshAllRewards` 的或）。
        /// <para>⚠️ 里程碑为空 ⇒ **一件都不建**（原版同款）。所以没有数据时容器数 = 0（改之前是「1 份模板」，
        /// 模板 `_container` 现在仍然在树上、仍然关着，见 `BuildRewardsScroll`）。</para></summary>
        public void CreateRewards(bool refreshAllRewards = false)
        {
            if (_content == null) return;
            if (_containers.Count > 0) ResetRewards();
            if (_event == null || _event.Milestones == null || _event.Milestones.Count == 0) return;
            int n = _event.Milestones.Count;
            for (int i = 0; i < n; i++)
            {
                var node = MenuDraw.Node(_content, RewardContainerName, ContainerRectFor(i));
                BuildRewardContainer(node);
                node.gameObject.SetActive(true);
                _containers.Add(node);
                _containerIndex[node] = i;
                bool late = (i >= 7) && !refreshAllRewards;
                if (late) PrepareForLateInitialization(i);
                else ApplyRewardItemContainer(i, node, null);
            }
            // 原版的迟到初始化由 `UICulling` 的可见性回调调 `DelayedInit` 触发；本壳没有 UICulling
            // ⇒ 建完立刻补一次（等价于「这批格子当场就可见」），如实记在这里。
            if (_lateInitIndices.Count > 0) DelayedInit();
        }

        /// <summary>= 原版 `ExpansionPassRewardSelector.RefreshData(bool)`：纯转发 `CreateRewards(refreshAllRewards)`。</summary>
        public void RefreshData(bool refreshAllRewards = false) { CreateRewards(refreshAllRewards); }

        /// <summary>= 原版 `ExpansionPassEventTab.RefreshRewards()` = `rewardSelector.CreateRewards(false)`。</summary>
        public void RefreshRewards() { CreateRewards(false); }

        /// <summary>= 原版 `ExpansionPassEventTab.RefreshTestEditorOnly()`：编辑器专用钩子（`Reset()` + `Initialize()` 重跑）。
        /// 原版**正式版不调用**；对应物是自检宿主 `Editor/ShellScene.cs` 的重建断言 ⇒ 这里只在编辑器下生效。</summary>
        public void RefreshTestEditorOnly()
        {
#if UNITY_EDITOR
            Build();
            InitializeEventTab();
            RefreshMissionsLocal();
#else
            Debug.LogWarning("[ExpansionPass] `RefreshTestEditorOnly` 只给编辑器自检用（原版同样只在编辑器里生效）");
#endif
        }

        /// <summary>= 原版 `ExpansionPassRewardSelector.Reset()`：① `CustomDebug.LogWarning("[TODO] We have to clear the addressables …")`
        /// ② 逐个 `Destroy(container.gameObject)` ③ 清表。
        /// <para>⚠️ 原版自己标了 TODO（地址ables 没释放）；我们这边 `MenuDraw.ClearChildren` 会连带放掉逐项美术
        /// （那是本工程 `AssetReference.Release` 的等价那一跳）⇒ 比原版多做一步，如实记。</para></summary>
        public void ResetRewards()
        {
            Debug.LogWarning("[TODO] We have to clear the addressables when closing this window");
            for (int i = 0; i < _containers.Count; i++)
            {
                var t = _containers[i];
                if (t == null) continue;
                MenuDraw.ClearChildren(t);
                var go = t.gameObject;
#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(go); continue; }
#endif
                UnityEngine.Object.Destroy(go);
            }
            _containers.Clear();
            _containerIndex.Clear();
            _lateInitIndices.Clear();
            _lateInitPending = false;
        }

        /// <summary>= 原版 `ExpansionPassRewardSelector.Initialize(IScoringEvent evt)`：
        /// 存事件（类型不是 `ExpansionPassEvent` ⇒ `CustomDebug.LogError("[ERROR] ExpansionPassRewardSelector is not receiving an ExpansionPassEvent")`）
        /// → 清旧 → 按里程碑建满。本类的 `SetEventTab` 已含这一跳（见它的 doc），这里是同一入口的别名。</summary>
        public void InitializeSelector(ExpansionPassEvent evt)
        {
            if (evt == null)
            {
                Debug.LogError("[ERROR] ExpansionPassRewardSelector is not receiving an ExpansionPassEvent");
                return;
            }
            SetEventTab(evt);
        }

        /// <summary>= 原版 `ExpansionPassRewardSelector.CollectRewardClicked(int rewardToClaim)`（`:163-184`）：
        /// ① `EventSystemController.Toggle(false)` ⇒ 等价物 = `PointerLayer.SetInputEnabled(false)`（🔴 2026-10-28 铁律 5 就地订正：原文写「本壳没有 `EventSystemController` ⇒ 无对应物」**已过期** —— 那个等价物从 `B90-N1` 就有；判据 → `资料/完全复刻/判据/B93_Q12_SetInputEnabled语义对齐.md`）
        /// ② `scoringEvent.PurchaseMilestone(index, onComplete, onError)`
        /// ③ 成功回调 = 恢复事件系统 + 播 `rewardClaimedSound` + `CreateRewards(true)`；失败 = 恢复 + `CreateRewards(true)`。</summary>
        public void CollectRewardClicked(int rewardToClaim)
        {
            if (_event == null)
            {
                Debug.LogWarning("[ExpansionPass] `CollectRewardClicked(" + rewardToClaim + ")`：没有事件数据 ⇒ 不假装领取");
                return;
            }
            _event.PurchaseMilestone(rewardToClaim, OnRewardCollectComplete, OnRewardCollectError);
        }

        void OnRewardCollectComplete(CampaignData.RewardSpec[] rewards)
        {
            WindowSounds.PlayFx(WindowSounds.PositiveCue, "ExpansionPass/Claim");
            CreateRewards(true);
            // 原版这一步是 `RewardService.Collect(rewards, showAnimation:true)` 自己弹领奖窗；本工程没有 `RewardService`
            // ⇒ 等价物 = `RewardWindow.ShowCollected`（同一扇 `Reward Window`，`isPremiumLocked` 按「已领」那档给 false）。
            if (rewards != null && rewards.Length > 0 && WindowsManager.Instance != null)
                RewardWindow.ShowCollected(rewards, null, false, null);
        }

        void OnRewardCollectError(string error)
        {
            MessageToast.ShowError(error);
            CreateRewards(true);
        }

        /// <summary>视图侧的升级入口：从点到的 `Level up` 按钮反查档位（= 原版闭包里的 `milestoneIndex`）。</summary>
        void OnLevelUpClick(Transform levelUpButton)
        {
            int idx = MilestoneIndexOfTransform(levelUpButton);
            if (idx < 0)
            {
                Debug.LogWarning("[ExpansionPass] 升级钮点击：找不到它属于哪一档（模板那一份不在 `_containers` 里，"
                               + "或本地没有里程碑数据）⇒ **不假装升级**。");
                return;
            }
            OnLevelUp(idx);
        }

        /// <summary>= 原版 `ExpansionPassRewardSelector.OnLevelUp(int level)`（`:186-203`）：同样的「关/恢复事件系统 +
        /// `PurchaseLevelUp` + 成功/失败都 `CreateRewards(true)`」三跳。</summary>
        public void OnLevelUp(int level)
        {
            if (_event == null)
            {
                Debug.LogWarning("[ExpansionPass] `OnLevelUp(" + level + ")`：没有事件数据 ⇒ 不假装升级");
                return;
            }
            _event.PurchaseLevelUp(level, OnLevelUpComplete, OnLevelUpError);
        }

        void OnLevelUpComplete() { CreateRewards(true); }
        void OnLevelUpError(string error) { MessageToast.ShowError(error); CreateRewards(true); }

        // ------------------------------------------------------------ ② `ExpansionPassRewardItemContainer`

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.Initialize(liveOps, milestoneIndex, onCollect, onLevelUp, overrideCurrentScore)`
        /// （`d:/2/tools/decomp_full/ExpansionPassRewardItemContainer__Initialize.c`，逐句）：
        /// <list type="number">
        /// <item>进度条：`ScoringDataInfo(current = override ?? PlayerScore, previous = prev?.scoreToUnlock ?? 0, next = cur.scoreToUnlock)`
        ///   → `personalProgressBarScaleAndPositionController.Initialize(state, prevState, index)`
        ///   → `personalProgressBar.Initialize(info, scoringItem, isLast)` → `rectMask2D.enabled = (index == 0)`</item>
        /// <item>领奖钮：`claimButtonController.Initialize(state, CurrencyValue(free, 0), OnCollect)`</item>
        /// <item>抽屉：`rewardDrawerController.Initialize(cur.Rewards, cur.isPremium, state)` + `OverridePremiumBadge(...)`</item>
        /// <item>`premiumGlow.enabled = cur.isPremium` · `highlightReward.SetActive(false)`</item>
        /// <item>升级条：**仅当 `prevState != Locked &amp;&amp; state == Locked`** 才评估；`SetActive(0 &lt; cost.Amount)`
        ///   + `levelUpPriceDisplay.Setup(cost)` + 重挂 `OnClick`（Ghidra `:172-190`：`iVar7 == 0 || iVar6 != 0` 走关掉那支）</item>
        /// </list>
        /// <para>⚠️ 我们逐档容器是**同一份 `BuildRewardContainer` 建出来的**（= 原版 prefab 的 Instantiate），
        /// 这个函数只负责「按这一档的数据把件改成本档该有的样子」。</para></summary>
        void ApplyRewardItemContainer(int index, Transform node, int? overrideCurrentScore)
        {
            if (_event == null || _event.Milestones == null) return;
            if (index < 0 || index >= _event.Milestones.Count || node == null) return;
            var ms = _event.Milestones;
            ExpansionMilestone m = ms[index];
            if (m == null) return;
            ExpansionMilestone prev = index > 0 ? ms[index - 1] : null;
            int curScore = overrideCurrentScore ?? _event.PlayerScore;
            ScoringMilestoneState state = _event.GetState(m);
            ScoringMilestoneState prevState = _event.GetState(prev);
            bool isFirst = index == 0;
            bool isLast = index == ms.Count - 1;

            var bar = FindDeep(node, "Expansion Pass Progress Bar");
            if (bar != null)
            {
                var info = new ScoringDataInfo(curScore, prev != null ? prev.ScoreToUnlock : 0, m.ScoreToUnlock);
                ApplyProgressBar(bar, info, isFirst, isLast, (int)state, (int)prevState, index);
            }

            ApplyClaimButtonState(node, (int)state);
            ApplyRewardDrawer(node, m);

            var glow = FindDeep(node, "Premium Glow");
            if (glow != null) glow.gameObject.SetActive(m.IsPremium);
            var hi = FindDeep(node, "Collect Highlight Effect");
            if (hi != null) hi.gameObject.SetActive(false);

            var lv = FindDeep(node, "Level up");
            if (lv != null)
            {
                bool gate = prevState != ScoringMilestoneState.Locked && state == ScoringMilestoneState.Locked;
                int cost = gate ? _event.GetCostToLevelUp(m) : 0;
                bool show = gate && cost > 0;
                lv.gameObject.SetActive(show);
                if (show)
                {
                    var price = FindDeep(lv, "Price Display");
                    var txt = price != null ? FindDeep(price, "text") : null;
                    var lb = txt != null ? txt.GetComponent<Label>() : null;
                    if (lb != null) lb.SetText(cost.ToString());
                }
            }
        }

        /// <summary>= 原版 `EventProgressBarController.Initialize(ScoringDataInfo, ObtainableItem, bool isLastMilestone)`
        /// + `ScoreObject.Initialize` + `PercentageToNextLevel`（`cpp2il/EventProgressBarController.cs`）：
        /// <code>
        /// gameObject.SetActive(info.NextLevelScore != 0);
        /// show = ((isLast || cur &lt; next) &amp;&amp; cur &gt;= prev) || (cur == 0 &amp;&amp; prev == 0);
        ///   → scoreText.enabled = show · iconHolder.SetActive(show) · text = $"{cur}/{next}"
        /// p = max(cur - prev, 0) / (next - prev);  fill.value = p;  tipSprite.enabled = (0 &lt; p &lt; 1)
        /// ## 调用方那一跳：rectMask2D.enabled = (index == 0)
        /// </code>
        /// <para>⚠️ 本壳的进度条是**绝对设计 px**（不是 uGUI `Slider`）⇒ 填充分 = 把 `Bar/ProgressionBar` 的
        /// **宽**按 `p` 缩，端帽 `BarEnd` 跟着走（它的矩形本来就相对填充右缘）。
        /// ⚠️ `rectMask2D` / `tipSprite` 这两件本壳没有对应组件（裁切走 `ViewportClip`、端帽就是 `BarEnd` 那张图）
        /// ⇒ 前者的等价物是「什么都不做」（建树即裁，没有开关口，账 row 187 已记），后者 = `BarEnd` 的显隐。</para></summary>
        void ApplyProgressBar(Transform barRoot, ScoringDataInfo info, bool isFirst, bool isLast,
                              int state, int prevState, int level)
        {
            // ① 位置/缩放：`BaseProgressBarScaleAndPositionController.Initialize` 那三句的等价（见 `PersonalBarRect` 的 doc）
            MenuDraw.ApplyPxRect(barRoot, barRoot.parent, PersonalBarRect(PersonalBarRootR, state, prevState, level));

            // ② 整条显隐
            barRoot.gameObject.SetActive(info.NextLevelScore != 0);

            // ③ `ScoreObject.Initialize`
            bool show = ((isLast || info.CurrentScore < info.NextLevelScore) && info.CurrentScore >= info.PreviousLevelScore)
                        || (info.CurrentScore == 0 && info.PreviousLevelScore == 0);
            var pts = FindDeep(barRoot, "Points");
            if (pts != null)
            {
                var txt = FindDeep(pts, "text");
                var icon = FindDeep(pts, "icon");
                var lb = txt != null ? txt.GetComponent<Label>() : null;
                if (lb != null) { lb.enabled = show; lb.SetText(info.CurrentScore + "/" + info.NextLevelScore); }
                if (icon != null) icon.gameObject.SetActive(show);
            }

            // ④ 填充比例
            float p = PercentageToNextLevel(info);
            var bar = FindDeep(barRoot, "Bar");
            var fill = bar != null ? FindDeep(bar, "ProgressionBar") : null;
            if (fill != null)
            {
                float w = PersonalBarR.W * Mathf.Clamp01(p);
                MenuDraw.ApplyPxRect(fill, fill.parent,
                    new PxRect(PersonalBarR.x1, PersonalBarR.y1, PersonalBarR.x1 + w, PersonalBarR.y2));
                var end = FindDeep(fill, "BarEnd");
                if (end != null) end.gameObject.SetActive(p > 0f && p < 1f);
            }
        }

        /// <summary>= 原版 `EventProgressBarController.PercentageToNextLevel(ScoringDataInfo)`：
        /// `t = cur - prev; if (t &lt;= 0) t = 0; return t / (next - prev);`
        /// <para>⚠️ 分母 0 ⇒ 原版得 `Infinity`/`NaN`（uGUI `Slider` 会夹住）。本壳返回 0 并出声一次 ——
        /// 那只会发生在 `next == prev` 的坏数据上（正常里程碑 `next &gt; prev`）。</para></summary>
        public float PercentageToNextLevel(ScoringDataInfo info)
        {
            if (info == null) return 0f;
            float t = info.CurrentScore - info.PreviousLevelScore;
            if (t <= 0f) t = 0f;
            float d = info.NextLevelScore - info.PreviousLevelScore;
            if (d == 0f) return 0f;
            return t / d;
        }

        /// <summary>= 原版 `ExpansionPassProgressClaimButtonController.Initialize(currentState, normalPrice, onCollect)` 的**状态那一半**
        /// （`BaseEventProgressClaimButtonController&lt;TState&gt;.Initialize` 只做一件事：挑出 `state == currentState`
        /// 的那条 `stateDefinitions` 并 `ApplyState()`）。
        /// <para>`stateDefinitions` 四档的 target 显隐表 = **prefab MB 实读**（文件头 ③ 那张表），逐档照抄：</para>
        /// <code>
        /// state 0 Locked                  → Claim ✗ · Level ✓(Lock ✓) · BuyPremium ✗
        /// state 1 LockedBecauseNotPremium → Claim ✓(Highlight ✓) · Level ✗ · BuyPremium ✓
        /// state 2 Unlocked                → Claim ✓ · Level ✗ · BuyPremium ✗
        /// state 3 Collected               → Claim ✗ · Level ✓(Lock ✗) · BuyPremium ✗
        /// </code>
        /// <para>⚠️ 那份 `stateDefinitions` 里还有 `enableLevelHighlight` / `forceInteractableState` / 每档的
        /// `priceDisplayButton` 三格 —— **本地没读到它们的值**（MB 里只有 target 列表被逐个解成节点名）
        /// ⇒ 这三件**不实现**并如实记（⛔ 不按「大概」补）。</para></summary>
        public void ApplyClaimButtonState(Transform container, int currentState)
        {
            var cbc = FindDeep(container, "Claim Button Container");
            if (cbc == null) return;
            var claim = FindDeep(cbc, "Claim Button");
            var buyPremium = FindDeep(cbc, "Buy premium");
            var level = FindDeep(cbc, "Level");
            var hl = claim != null ? FindDeep(claim, "Highlight button claim") : null;

            bool claimOn, levelOn, lockOn, buyOn, hlOn;
            switch (currentState)
            {
                case MsStateLocked:
                    claimOn = false; levelOn = true; lockOn = true; buyOn = false; hlOn = false; break;
                case MsStateLockedBecauseNotPremium:
                    claimOn = true; levelOn = false; lockOn = false; buyOn = true; hlOn = true; break;
                case MsStateUnlocked:
                    claimOn = true; levelOn = false; lockOn = false; buyOn = false; hlOn = false; break;
                case MsStateCollected:
                    claimOn = false; levelOn = true; lockOn = false; buyOn = false; hlOn = false; break;
                default:
                    // 原版 `stateDefinitions` 里没有匹配项 ⇒ 一件都不动（`foreach` 落空）。我们按出厂态兜底并出声。
                    Debug.LogWarning("[ExpansionPass] ApplyClaimButtonState：未知档位 " + currentState
                                   + "（原版 `stateDefinitions` 只有 0..3）⇒ 按出厂态（state 0）显示");
                    claimOn = false; levelOn = true; lockOn = true; buyOn = false; hlOn = false; break;
            }
            if (claim != null) claim.gameObject.SetActive(claimOn);
            if (buyPremium != null) buyPremium.gameObject.SetActive(buyOn);
            if (level != null) level.gameObject.SetActive(levelOn);
            var lockNode = level != null ? FindDeep(level, "Lock") : null;
            if (lockNode != null) lockNode.gameObject.SetActive(lockOn);
            if (hl != null) hl.gameObject.SetActive(hlOn);
        }

        /// <summary>= 原版 `ExpansionPassProgressClaimButtonController.ToggleCollectButtonInteractable(bool)`：`actionButton.enabled = option`。
        /// <para>⚠️ 本壳的按钮没有 `Selectable.enabled`（`WindowButton` 只有 `onClick`）⇒ 等价做法 = 整颗
        /// `Claim Button` 节点的 `SetActive`（关掉 = 点不到）。⛔ 不是「删掉回调」那种假禁用。</para></summary>
        public void ToggleCollectButtonInteractable(Transform container, bool option)
        {
            var cbc = FindDeep(container, "Claim Button Container");
            var claim = cbc != null ? FindDeep(cbc, "Claim Button") : null;
            if (claim != null) claim.gameObject.SetActive(option);
        }

        /// <summary>= 原版 `ExpansionPassProgressClaimButtonController.ToggleCollectButtonInteractable(bool)` 的**无参容器版**
        /// （原版靠闭包指向自己那一格；我们对**全部**容器生效 —— 原版这个口也只被单格用过）。</summary>
        public void ToggleCollectButtonInteractable(bool option)
        {
            for (int i = 0; i < _containers.Count; i++) ToggleCollectButtonInteractable(_containers[i], option);
        }

        /// <summary>= 原版 `ExpansionPassProgressClaimButtonController.CollectionClick()`（`:44-49`）：
        /// `base.CollectionClick()`（→ `onCollect?.Invoke()`）→ `actionButton.enabled = false`。
        /// <para>无参版本；原版靠闭包知道「哪一格」，我们只能反查 ⇒ 只有一个容器时用它，多容器请用
        /// <see cref="CollectionClickOn"/>（视图侧挂的就是那个）。</para></summary>
        public void CollectionClick() { CollectionClickOn(_containers.Count == 1 ? _containers[0] : null); }

        /// <summary>视图侧的入口：从点到的那个 `Claim Button` 节点反查档位（= 原版闭包里的 `milestoneIndex`）。</summary>
        public void CollectionClickOn(Transform claimButton)
        {
            int idx = MilestoneIndexOfTransform(claimButton);
            if (idx < 0)
            {
                Debug.LogWarning("[ExpansionPass] 领奖钮点击：找不到它属于哪一档（模板那一份不在 `_containers` 里，"
                               + "或本地没有里程碑数据）⇒ **不假装领取**。");
                return;
            }
            OnCollect(idx);
        }

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.OnCollect()`（`:291-299`）：
        /// `onCollectCallback?.Invoke()`（= `onCollect(milestoneIndex)`）→ `ToggleHighlightEffect(true)`。</summary>
        void OnCollect(int index)
        {
            CollectRewardClicked(index);
            ToggleHighlightEffect(index, true);
        }

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.ToggleHighlightEffect(bool)`（`:306-320`）：
        /// `highlightReward.SetActive(option)`；`option` 时再 `SoundManager.Play2D(soundOnCollect)` + `highlightEffect.Play()`。
        /// <para>⚠️ `soundOnCollect` / `rewardClaimedSound` 那两个 `AudioCue` 是 prefab 引用、本地拿不到具体 cue
        /// ⇒ 用本工程的通用正向 cue 代（`WindowSounds.PositiveCue`，与各窗按钮同一份口径），如实记。</para>
        /// <para>⚠️ `UIBorderGlow` 本壳没挂（`Collect Highlight Effect` 是九宫图、图本身 alpha 实读 = 0）
        /// ⇒ `Play()` 那跳无对应物，出声一次。</para></summary>
        public void ToggleHighlightEffect(int index, bool option)
        {
            var node = (index >= 0 && index < _containers.Count) ? _containers[index] : null;
            if (node == null) return;
            var hi = FindDeep(node, "Collect Highlight Effect");
            if (hi != null) hi.gameObject.SetActive(option);
            if (!option) return;
            WindowSounds.PlayFx(WindowSounds.PositiveCue, "ExpansionPass/Highlight");
            var glow = hi != null ? hi.GetComponent<UIBorderGlow>() : null;
            if (glow != null) glow.Play();
            else Note("`Collect Highlight Effect` 上没有 `UIBorderGlow`（本壳没挂那个组件；九宫图本身 alpha = 0）"
                    + " ⇒ `highlightEffect.Play()` 这一跳没有对应物");
        }

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.TestHighlighOn()`（`:301-304`，调试钩子）= `ToggleHighlightEffect(true)`。
        /// 原版带 `Test` 前缀 ⇒ 正式路不调用；我们的对应物落在第 0 个容器上（原版是「自己那一格」）。</summary>
        public void TestHighlighOn() { ToggleHighlightEffect(0, true); }

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.PrepareForLateInitialization(viewport, liveOps, milestoneIndex,
        /// onCollect, onLevelUp, overrideCurrentScore)`（`:255-280`）：**只暂存六参** + 置 `preparedForDelayedInit`，
        /// 真正 `Initialize` 留到 `DelayedInit()`。原版的触发者是 `UICulling` 的可见性回调（`culling.viewport = viewport`）。
        /// <para>我们暂存的只有档位下标与「当前分 override」（另四参由本类成员直接可取）。</para></summary>
        public void PrepareForLateInitialization(int milestoneIndex)
        {
            _lateInitPending = true;
            _lateInitIndices.Add(milestoneIndex);
            Debug.Log("[ExpansionPass] `PrepareForLateInitialization(#" + milestoneIndex + ")`：等 `DelayedInit()`"
                    + "（原版由 `UICulling` 可见性回调触发；本壳没有 UICulling ⇒ `CreateRewards` 末尾立刻补一次）");
        }

        /// <summary>= 原版 `ExpansionPassRewardItemContainer.DelayedInit()`（`:281-289`）：
        /// `if (preparedForDelayedInit) Initialize(暂存那六个参)`（跑完**不**清标志 —— 原版也是不清）。</summary>
        public void DelayedInit()
        {
            if (!_lateInitPending) return;
            _lateInitPending = false;
            for (int i = 0; i < _lateInitIndices.Count; i++)
            {
                int idx = _lateInitIndices[i];
                if (idx < 0 || idx >= _containers.Count) continue;
                ApplyRewardItemContainer(idx, _containers[idx], null);
            }
            _lateInitIndices.Clear();
        }

        // ------------------------------------------------------------ ③ `ExpansionPassRewardDrawerController`

        /// <summary>= 原版 `ExpansionPassRewardDrawerController.Initialize(rewards, isPremium, currentState)`（`:15-50`）：
        /// `flag = (rewards.Count - 1) &gt;= 1`（= **`rewards.Count &gt;= 2`**，ILSpy `:26-31` 那个 `a - 1 &gt;= 1` 惯用式）
        /// → `extraReward.enabled = flag` → `extraReward.GetComponentInChildren&lt;RectTransform&gt;().SetAsLastSibling()`
        /// → `allRewardsInfoButton.gameObject.SetActive(flag)` → `flag` 时把 `ShowAllRewards` 挂上去。
        /// <para>⚠️ `allRewardsInfoButton` 到底挂在哪个节点，prefab 的字段 PPtr 我们**没逐个解**；本工程里唯一带
        /// `UIGenericEventCatcher` 的兄弟节点是 `CollisionButton` ⇒ 用它，并在这里如实标成**推断**。</para></summary>
        void ApplyRewardDrawer(Transform container, ExpansionMilestone m)
        {
            var rt = FindDeep(container, "RewardTransform");
            if (rt == null) return;
            int count = (m != null && m.RewardArray != null) ? m.RewardArray.Length : 0;
            bool extra = count >= 2;

            var extraIcon = FindDeep(rt, "Icon Extra rewards");
            if (extraIcon != null)
            {
                extraIcon.gameObject.SetActive(extra);
                if (extra) extraIcon.SetAsLastSibling();
            }

            var allBtn = FindDeep(container, "CollisionButton");
            if (allBtn == null) return;
            allBtn.gameObject.SetActive(extra);
            if (extra && FindDeep(allBtn, "AllRewardsHit") == null)
            {
                MenuDraw.Hit(allBtn, "AllRewardsHit", CollisionR, QHit, () => ShowAllRewards(allBtn),
                             null, null, null, null, null, clickCue: WindowSounds.DefaultButtonCue);
            }
        }

        /// <summary>= 原版 `ExpansionPassRewardDrawerController.ShowAllRewards()`（`:52-60`）：
        /// `RewardService.Preview(该档全部奖励, isPremiumLocked: **true**)`。
        /// 等价物 = `RewardWindow.ShowPreview`（同一扇 `Reward Window` 的预览态）。</summary>
        public void ShowAllRewards(Transform container)
        {
            int idx = MilestoneIndexOfTransform(container);
            if (idx < 0 || _event == null)
            {
                Debug.LogWarning("[ExpansionPass] `ShowAllRewards`：找不到档位 / 没有事件数据 ⇒ 不开预览窗");
                return;
            }
            var rewards = _event.Milestones[idx].RewardArray ?? new CampaignData.RewardSpec[0];
            if (WindowsManager.Instance == null)
            {
                Debug.LogWarning("[ExpansionPass] `ShowAllRewards`：没有 `WindowsManager` ⇒ 开不了奖励预览窗");
                return;
            }
            RewardWindow.ShowPreview(rewards, null, true, null);
        }

        // ------------------------------------------------------------ ④ `ExpansionPassPurchasePremiumPanel`

        /// <summary>= 原版 `ExpansionPassPurchasePremiumPanel.Initialize(currentEvent, onClosePurchasePopup)`（`:92-215`）：
        /// <list type="number">
        /// <item>`playerIsNotPremiumObjects.SetActive(!HasPremium)` · `playerIsPremiumObjects.SetActive(HasPremium)`</item>
        /// <item>`DestroyAllChildren(initialPremiumRewardAnchor / lastPremiumRewardAnchor)`</item>
        /// <item>已买 ⇒ 关掉 First 锚点那一组、只在 Last 锚点画 `Last(IsPremium).Rewards.First()`、把
        ///   `ExtraPercentaje` 按 `PremiumPercentageBonus` 填进 `extraPointsText`、再 `expansionPassPointDrawer.DrawForArmy(army)`</item>
        /// <item>未买 ⇒ 两个锚点都开，各画 `First(IsPremium)` / `Last(IsPremium)` 的**第一条**奖励</item>
        /// </list>
        /// <para>⚠️ `LoadAsset` / `DrawForArmy(army)` 要远端数据与玩家阵营 ⇒ 本地没有：图不画、**出声**。
        /// 两条 `ItemDrawer.Draw` 只在真有里程碑数据时才跑（`Rewards.First()` 空表会抛）。</para></summary>
        public void ApplyPremiumPanel(System.Action onClosePurchasePopup = null)
        {
            _onClosePurchasePopup = onClosePurchasePopup; if (_premiumPanel == null || _event == null) return;   // 原版 `Initialize` 第一句 = `onClosePurchasePopup = 入参`（先存、后判）
            bool premium = _event.HasPremium;
            var notPremium = FindDeep(_premiumPanel, "IsNotPremium");
            var isPremium = FindDeep(_premiumPanel, "Is Premium");
            if (notPremium != null) notPremium.gameObject.SetActive(!premium);
            if (isPremium != null) isPremium.gameObject.SetActive(premium);

            var firstAnchor = FindDeep(_premiumPanel, "Fisrt Premium Reward");
            var lastAnchor = FindDeep(_premiumPanel, "Last Premium Reward");
            ResetPremiumCardAnchor(firstAnchor);
            ResetPremiumCardAnchor(lastAnchor);

            if (premium)
            {
                if (firstAnchor != null) firstAnchor.gameObject.SetActive(false);
                if (lastAnchor != null) lastAnchor.gameObject.SetActive(true);
                DrawPremiumReward(lastAnchor, LastPremiumByPremium());
                if (_extraPoints != null)
                    _extraPoints.SetText(string.Format(Loc.T(TermExtraPercentaje), _event.Data != null
                        ? _event.Data.PremiumPercentageBonus.ToString() : NoData));
                DrawForArmy(PremiumArmyOf());   // 原版这一句 = `expansionPassPointDrawer.DrawForArmy(army)`；army 来自**事件数据**（`LiveOpsEventData.filter.army`），不是玩家存档
                // ↑ 两行原位替换（保行数）：army 未知时 `DrawForArmy` 自己出声，⛔ 不拿 prefab 出厂的 Orks 示例冒充
            }
            else
            {
                if (firstAnchor != null) firstAnchor.gameObject.SetActive(true);
                if (lastAnchor != null) lastAnchor.gameObject.SetActive(true);
                DrawPremiumReward(firstAnchor, FirstPremiumByPremium());
                DrawPremiumReward(lastAnchor, LastPremiumByPremium());
            }
        }

        ExpansionMilestone FirstPremiumByPremium()
        {
            for (int i = 0; i < _event.Milestones.Count; i++)
                if (_event.Milestones[i] != null && _event.Milestones[i].IsPremium) return _event.Milestones[i];
            return null;
        }

        ExpansionMilestone LastPremiumByPremium()
        {
            for (int i = _event.Milestones.Count - 1; i >= 0; i--)
                if (_event.Milestones[i] != null && _event.Milestones[i].IsPremium) return _event.Milestones[i];
            return null;
        }

        /// <summary>= 原版 `SupportMethods.DestroyAllChildren(anchor)`：把锚点下已经画过的抽屉清掉
        /// （⚠️ **不是**连锚点本身一起清 —— 原版保留 `Card Drawer` 那层容器）。</summary>
        static void ResetPremiumCardAnchor(Transform anchor)
        {
            if (anchor == null) return;
            MenuDraw.ClearChildren(anchor);
        }

        /// <summary>= 原版 `ItemDrawer.Draw(anchor, milestones.First/Last(IsPremium).Rewards.First())`。
        /// 空表 / 空里程碑 ⇒ **不画**并出声（原版会 `Enumerable.First()` 抛异常，我们出声退回）。</summary>
        void DrawPremiumReward(Transform anchor, ExpansionMilestone m)
        {
            if (anchor == null) return;
            if (m == null || m.RewardArray == null || m.RewardArray.Length == 0)
            {
                Note("premium 卡预览：该侧没有 premium 里程碑奖励（本地没有里程碑数据 / 该侧奖励为空）⇒ 只留空 `Card Drawer`");
                return;
            }
            var spec = ItemDrawer.Spec(m.RewardArray[0].Id, null, m.RewardArray[0].Id);
            ItemDrawer.Draw(anchor, LastCardDrawerR, spec, 1,   // 🔴 数量恒 1：原版三处 `ItemDrawer.Draw(anchor, item, 1, 0, 0)` 都**硬编码 1**（不读 reward.quantity）—— 2026-10-28 B92_N3 现读订正（原来传的是 reward 的数量）
                            DrawerOverride.Default, ItemDrawerStyle.Default(QCardReward, QCardReward, QCardText));
        }

        /// <summary>= 原版 `ExpansionPassPurchasePremiumPanel.Start()`（`:85-90`）：`purchasePremiumButton.onClick += PurchasePremiumClick`。
        /// <para>本壳的按钮回调在建树时就挂好了（`BuildNotPremiumBranch` 的 `MenuDraw.Hit`）⇒ 这里只做等价记账。</para></summary>
        public void StartPurchasePremiumPanel()
        {
            Note("`ExpansionPassPurchasePremiumPanel.Start`：原版给 `purchasePremiumButton` 挂 `PurchasePremiumClick`；"
               + "本壳在 `BuildNotPremiumBranch` 建树时就挂了同一个回调（等价）");
        }

        /// <summary>= 原版 `ExpansionPassProgressClaimButtonController.BuyPremiumClick()`（`:29-37`）：
        /// `UIMessageController.Instance.ShowError("Event_Description/ExpansionPass/BuyPremiumToClaim", localize: true)`
        /// —— **玩家能看见**的报错。我们的等价物 = `MessageToast.ShowError`（`UIMessageController` 本工程没有那一扇）
        /// ⇒ 由原来的「只 `Debug.LogWarning`」改成**可见**提示（这正是账 row 236 指出的缺口）。</summary>
        public void BuyPremiumClick()
        {
            string msg = Loc.T(TermBuyPremiumToClaim);
            if (!MessageToast.ShowError(msg))
                Debug.LogWarning("[ExpansionPass] " + msg);
        }

        // ------------------------------------------------------------ ⑤ 第二页 `ExpansionPassMissionsTab`

        /// <summary>= 原版 `ExpansionPassMissionsTab.OnOpen()` / `ToFocus()`（两者反编译出来是同一段）：
        /// `base.OnOpen()` + `RefreshMissions()`。</summary>
        public void OnMissionsTabOpen() { RefreshMissionsLocal(); }

        /// <summary>= 原版 `ExpansionPassMissionsTab.ToFocus()` —— 与 `OnOpen` **逐句相同**（反编译实证），转发同一个口。</summary>
        public void ToFocus() { RefreshMissionsLocal(); }

        /// <summary>= 原版 `ExpansionPassMissionsTab.RefreshMissions()`（`ExpansionPassMissionsTab.cs:186-530`）：
        /// <list type="number">
        /// <item>`handler = Missions`（`UiContainer&lt;IMission&gt;`）；**`handler == null` ⇒ 整段跳过**（`if (!(handler == null))` 那道闸）</item>
        /// <item>`Where(x =&gt; x.ParentEventId == LiveOps.GetID())` → `ToList()`</item>
        /// <item>一条都没有 ⇒ `WindowTabBase.CloseTab()` 并 return</item>
        /// <item>`dailyTickets.Initialize(第一条)` + `SetChallenge(...)`</item>
        /// <item>再筛一遍（`x =&gt; x.&lt;某旗标&gt; != -1`）、`SelectMany(挑战)` → `ToArray()`</item>
        /// <item>`n = Math.Min(3, array.Length)`；按 `containers` 缓存复用（尺寸变了先 `Destroy` 旧的再重建），
        ///   缺的 `Instantiate(missionContainerReference, dailyMissionAnchor)` → `Initialize(mission)` + `SetChallenge(progress)`</item>
        /// <item>末尾 `missionContainerReference.gameObject.SetActive(false)`（模板关掉）</item>
        /// </list>
        /// <para>🔴 本地常态 `MissionsSource == null`（原版 `GetHandler&lt;Missions&gt;` 返回 null）⇒ **跳过、不关页签** ——
        /// 这与「源在但 0 条」是两种情形，原版也只在后者 `CloseTab()`（这条是本批最容易做错的地方）。</para>
        /// <para>⚠️ 第 5 步那个旗标名反编译没解出（`+0x74` 与 `-1` 比）；`MissionChallengeProgress` 本工程也没有 ⇒
        /// 本地等价物 = **不过滤、只按 `Active` 筛**，并把「每条的挑战进度」略过（出声一次）。</para></summary>
        public void RefreshMissionsLocal()
        {
            if (_missionsPage == null) return;
            if (MissionsSource == null)
            {
                if (!_missionsNoSourceSaid)
                {
                    _missionsNoSourceSaid = true;
                    Debug.Log("[ExpansionPass] `RefreshMissions`：`Missions` handler 为 null（原版 `LiveOpsManager.GetHandler<Missions>()`）"
                            + " ⇒ 原版这一整段**跳过**（不 `CloseTab`）⇒ 本窗也保留 Missions 页与它那颗页签。");
                }
                return;
            }

            var active = new List<Mission>();
            string evId = _event != null ? _event.Id : null;
            for (int i = 0; i < MissionsSource.Missions.Count; i++)
            {
                var mm = MissionsSource.Missions[i];
                if (mm == null || !mm.Active) continue;
                if (!string.IsNullOrEmpty(mm.ParentEventId) && !string.IsNullOrEmpty(evId)
                    && mm.ParentEventId != evId) continue;
                active.Add(mm);
            }
            if (active.Count == 0) { CloseMissionsTab(); return; }

            _missionsClosed = false;
            var tpl = FindDeep(_missionsPage, "Daily Mission Container Ref");
            if (tpl == null)
            {
                Debug.LogWarning("[ExpansionPass] `RefreshMissions`：找不到模板 `Daily Mission Container Ref` ⇒ 不建任务卡");
                return;
            }
            var anchor = tpl.parent;
            // 重跑前先清上一批（原版靠 `containers[]` 缓存复用；我们每次重建 ⇒ 先删同名前缀的旧节点，⛔ 不叠两批）
            for (int i = anchor.childCount - 1; i >= 0; i--)
            {
                var ch = anchor.GetChild(i);
                if (ch == null || !ch.name.StartsWith("Mission Container ")) continue;
                MenuDraw.ClearChildren(ch);
#if UNITY_EDITOR
                if (!Application.isPlaying) { UnityEngine.Object.DestroyImmediate(ch.gameObject); continue; }
#endif
                UnityEngine.Object.Destroy(ch.gameObject);
            }
            int n = Mathf.Min(3, active.Count);
            for (int i = 0; i < n; i++)
            {
                var card = MenuDraw.Node(anchor, "Mission Container " + i, CardRectFor(i));
                BuildWeeklyMissionCard(card);
                card.gameObject.SetActive(true);
                var title = FindDeep(card, "title");
                var lb = title != null ? title.GetComponent<Label>() : null;
                if (lb != null && !string.IsNullOrEmpty(active[i].Title)) lb.SetText(active[i].Title);
            }
            tpl.gameObject.SetActive(false);   // 原版末尾也把模板关掉
            Note("`RefreshMissions`：`MissionChallengeProgress` / `dailyTickets`（每日票）本工程没有数据源 ⇒ 那两跳略过（出声）");
        }

        /// <summary>= 原版 `WindowTabBase.CloseTab()`：把这一页从页签栏摘掉。
        /// <para>本壳的页签是自建扁平列表 ⇒ 等价做法 = 把 Missions 那颗钮与这一页都 `SetActive(false)`。
        /// ⚠️ 已知限制：`ShowPage(1)` 仍会把页面重新打开（那一句在既有方法里、本批不许改行数）⇒ 记在这里，
        /// 真接上任务数据时再补一道「已关闭就不许 ShowPage」的闸。</para></summary>
        public void CloseMissionsTab()
        {
            if (_missionsClosed) return;
            _missionsClosed = true;
            if (_missionsPage != null) _missionsPage.gameObject.SetActive(false);
            if (_tabButtonsList.Count > 1 && _tabButtonsList[1] != null) _tabButtonsList[1].gameObject.SetActive(false);
            if (_activePage == 1) ShowPage(0);
            Debug.Log("[ExpansionPass] `RefreshMissions` 一条可做任务都没有 ⇒ `WindowTabBase.CloseTab()`（本壳等价物：关掉 Missions 页与它的页签）");
        }

        /// <summary>Missions 页是否已被 `CloseMissionsTab` 关掉（自检读口）。</summary>
        public bool MissionsTabClosed { get { return _missionsClosed; } }

        // ------------------------------------------------------------ 小工具

        /// <summary>HLG 排布：第 i 个容器的矩形（判据 = `Rewards Content` 的 `m_Padding.left` 122 / `m_Spacing` −130 / MiddleLeft）。</summary>
        static PxRect ContainerRectFor(int index)
        {
            float dx = 122f + index * (ContainerW + ContainerSpacing);
            return new PxRect(ContainerR.x1 + dx, ContainerR.y1, ContainerR.x2 + dx, ContainerR.y2);
        }

        /// <summary>Missions 卡在 `Daily Missions Holder` 里的排布：首张矩形 = 模板矩形，逐张下移一格高度。
        /// ⚠️ 原版 VLG 的 spacing 我们没逐字段读 ⇒ 这里用「模板高度」当步长（**我们挑的**，⛔ 不写成原版）。</summary>
        static PxRect CardRectFor(int index)
        {
            var r = R(55.8f, 511.3f, 887.1f, 668.7f);
            float dy = index * r.H;
            return new PxRect(r.x1, r.y1 + dy, r.x2, r.y2 + dy);
        }

        /// <summary>按名字深度优先找第一个节点（本壳没有 `transform.Find("a/b")` 之外的寻路工具，
        /// 而容器内层级有同名兄弟风险 ⇒ 自己走一遍，**只取第一个**）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var hit = FindDeep(root.GetChild(i), name);
                if (hit != null) return hit;
            }
            return null;
        }

        /// <summary>从点到的节点往上找它属于哪一档（= 原版闭包里的 `milestoneIndex`）。找不到 ⇒ −1。</summary>
        public int MilestoneIndexOfTransform(Transform t)
        {
            for (var cur = t; cur != null; cur = cur.parent)
            {
                int idx;
                if (_containerIndex.TryGetValue(cur, out idx)) return idx;
                if (cur == transform) break;
            }
            return -1;
        }

        // ============================================================ 🆕 2026-10-28（B92_N3b）
        // `ExpansionPassPointDrawer.DrawForArmy(army)` —— 原版方法体**只有两句**（`ExpansionPassPointDrawer__DrawForArmy.c` 现读）：
        //   ① `if (armyText(+0x58) != null) armyText.text = GameStaticData.CardArmyToString(army)`
        //   ② `if (image == null) return; image.sprite = ArmyUtilities.GetArmyIcon(army)`
        // 🔴 **本窗这一份实例的 `armyText` 是 null**（判据 = prefab 的 MB 现读：
        //   `MonoBehaviour_-2460985498715340003.json` 的 `m_GameObject.m_PathID = 564162408064697117`
        //   = `Icon Expansion Pass Points Drawer Variant` 这一份，而它的 `armyText.m_PathID = 0`；
        //   另两份同族实例（`…-7240202003299004953` / `…3544614726814496501`）里前者的 armyText 才非空）
        //   ⇒ **①这一句在原版这一步是空操作**，⛔ 我们也不用建 `armyText` 节点（建了反而与原版不同）。
        // ⚠️ `army` 从哪来：`expansionPass.Data.<+0x1b8>()` = `LiveOpsEventData.Army` = **`eventFilter.army`**
        //   （`out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEventData.cs:42`）—— 是**事件数据**上的一格，
        //   ⛔ **不是玩家存档/`ProfileData`**（本行旧差异那句「需要玩家阵营（本窗没读 ProfileData）」是错的，已订正）。

        /// <summary>事件数据里那一格 army（`LiveOpsEventData.filter.army`）的注入口 —— 本地数据模型里没有这一格
        /// （`Shell/ExpansionPassEvent.cs` 的 `ExpansionPassData` 只有 `XpPerGold` / `PremiumPercentageBonus` 等，
        /// 且那个文件不在本代理白名单）。`null` = 拿不到（今天）⇒ 传 0。</summary>
        public static System.Func<int> PremiumArmyProvider { get; set; }

        /// <summary>`Image` 那一格（原版 `ItemDrawerComponents.image`）：出厂无图（透明占位），
        /// `DrawForArmy` 时换成阵营徽记。</summary>
        ImageQuad _extraIconImage;

        /// <summary>`Initialize` 的第一句 `onClosePurchasePopup = 入参`（原版 `+0x60`）。
        /// 消费方 = `ExpansionPassPremiumOffers.NotifyParent`（原版 `b__12_1`）；今天没人传 ⇒ 恒 null（见属性 doc）。</summary>
        System.Action _onClosePurchasePopup;
        bool _armyNoteOnce;

        int PremiumArmyOf()
        {
            var p = PremiumArmyProvider;
            return p != null ? p() : 0;
        }

        /// <summary>= 原版 `ExpansionPassPointDrawer.DrawForArmy(CardArmy army)`（判据与边界见本节头）。
        /// <para>`army` 认不出（0 / Neutral / 不在 13 条表里）⇒ **不画** + 出声一次
        /// （原版 `ArmyUtilities.GetArmyIcon(Neutral)` 同样取不到 ⇒ `image.sprite` 保持 null）。</para></summary>
        public void DrawForArmy(int army)
        {
            if (_extraIconImage == null) return;
            var tex = ArmyIcons.Texture(army);
            if (tex == null)
            {
                if (!_armyNoteOnce)
                {
                    _armyNoteOnce = true;
                    Note("`ExpansionPassPointDrawer.DrawForArmy(" + army + ")`：这一格 army 取不到"
                       + "（原版 = 事件数据的 `eventFilter.army`，本地 `ExpansionPassData` 里没有这一格）"
                       + " ⇒ 阵营徽记**不画**（原版那一支 `ArmyUtilities.GetArmyIcon` 同样失败，`sprite` 保持 null）");
                }
                return;
            }
            _extraIconImage.SetTexture(tex);
            _extraIconImage.SetTint(Color.white);
        }

        /// <summary>自检读口：存着的 `onClosePurchasePopup`（原版 `+0x60`，由 `ApplyPremiumPanel` 的入参写进来）。
        /// <para>⚠️ 今天**恒 null**：本窗的父级没有接这个回调（`InitializeEventTab` 调 `ApplyPremiumPanel()` 不传参）
        /// ⇒ 原版 `b__12_1` 那一支落到「什么都不做」；存这一句本身是原版 `Initialize` 的第一句，已逐字对齐。</para></summary>
        public System.Action OnClosePurchasePopup { get { return _onClosePurchasePopup; } }

        // ================================================================================
        // 🆕 2026-10-28（`B93_E13` · 批次 G1「商店族」）—— `ExpansionPassPurchasePremiumPanel` 的两条
        // ================================================================================
        //
        // 判据（第一权威 = 反编译方法体）：
        //   `d:/2/tools/decomp_full/ExpansionPassPurchasePremiumPanel__PurchasePremiumClick.c`
        //   `d:/2/tools/decomp_full/ExpansionPassPurchasePremiumPanel__Start.c`
        //   `d:/2/tools/decomp_full/ExpansionPassPurchasePremiumPanel___PurchasePremiumClick_b__12_0.c`
        //   （⚠️ 本篇一律写 `[0x..]` 而不是 `:行号` —— 那份 `.c` 的行号会随反编译器版本漂，点方法名最稳。）

        /// <summary>= 原版 `ExpansionPassPurchasePremiumPanel.PurchasePremiumClick()`（要点逐句）：
        /// <list type="number">
        /// <item>`premiumOfferData = data.PremiumOfferData`（`data` = 组件 `+0x58` 上的 `ExpansionPassData`）；
        ///      **`data` 或 `premiumOfferData` 为 null ⇒ 整段什么都不做**（`.c` 开头两道 null 闸）</item>
        /// <item>`eventData = LiveOpsManager.GetHandler&lt;…&gt;().GetEvent(premiumOfferData.GetID())`</item>
        /// <item>`prefab = LiveOpsAssetUtility.GetComponentReference(premiumOfferData, 0xc)`</item>
        /// <item>`win = WindowsManager.Instance.OpenWindow(prefab, eventData)`</item>
        /// <item>`win == null` ⇒ **`CustomDebug.LogWarning("…" + premiumOfferData.GetID())`**（那条告警**带事件 id**）</item>
        /// <item>开出来了 ⇒ 往 `win + 0xc` 那个 nullary 委托上**连续 Combine 两条回调**
        ///      （`b__12_0` = 重跑 `Initialize(this.data, this.onClose)`，以及 `b__12_1` = 转出 `onClose`；原版**不去重**）</item>
        /// </list>
        /// <para>🔴 **③④ 的输入在远端**（`ExpansionPassData.PremiumOfferData` 由 LiveOps 下发，本机 0 命中）
        /// ⇒ 🔶 窗口那一格用 `ExpansionPassPremiumOffers` 的本地报价表补（`WindowsManager
        /// .OpenPurchasePremiumWindow` 顶替远端 prefab 引用）+ **告警里带上事件 id**；两条关窗回调在
        /// 同一处 `WireClose` 里挂（对位 = `GameWindow.OnCloseWindow`，判据与不确定度见那个文件的文件头）。</para>
        /// <returns>`true` = 真把付费窗开出来了（今天：本地表命中就 true）。</returns></summary>
        public bool PurchasePremiumClick()
        {
            PurchasePremiumClicks++;
            // ① 原版 `data(+0x58)` / `premiumOfferData` 两道 null 闸 ↔ 🔶 本地报价表（`ExpansionPassPremiumOffers`）
            string evId = _event != null ? _event.Id : null;
            var offer = ExpansionPassPremiumOffers.ForEvent(evId);
            if (!offer.HasValue) return ExpansionPassPremiumOffers.SayCannotOpen(evId, "本地报价表里没有这一档");
            // ②③④ 原版 `GetEvent(id)` → `GetComponentReference(premiumOfferData, 0xc)` → `OpenWindow(prefab, 事件)`
            var win = WindowsManager.OpenPurchasePremiumWindow(offer.Value.FocusArmy);
            if (win == null) return ExpansionPassPremiumOffers.SayCannotOpen(evId, "`OpenPurchasePremiumWindow` 返回 null");
            // ⑥ 原版 `Delegate.Combine(win + 0xc, b__12_0)` 再 Combine `b__12_1`（不 RemoveListener ⇒ 点几次叠几次，照抄）
            return ExpansionPassPremiumOffers.WireClose(win, evId, this);
        }

        /// <summary>自检读口：`PurchasePremiumClick` 被点了几次（= 付费档入口那条绑定通不通的端到端证据）。</summary>
        public int PurchasePremiumClicks { get; private set; }

        // ════════════════════════════════════════════════════════════════════════════════════════
        //  B93-5（2026-10-28 · 文件所有者代理 `D2`）：`ExpansionPassEventTab.GetCardsClick()`
        //  的**同名方法级入口**（原来只内联在 `BuildWhereToGetCardsButton` 的 lambda 里 ⇒ 判「声明级等价」降 B）。
        //  【为什么加在类尾】账按 `文件:行号` 记落点（`:491/:509/:572/:950/:1026/:1070/:1134/:1203/:1210/`
        //    `:1271/:1755/:1771/:1856/:1902/:1925/:1943/:1986/:2054/…`）⇒ 新成员只能加在所有老落点之后。
        // ════════════════════════════════════════════════════════════════════════════════════════

        /// <summary>原版 `ExpansionPassEventTab.GetCardsClick()`（`decomp_full/ExpansionPassEventTab__GetCardsClick.c`
        /// 全文 5 句，逐句）：
        /// <code>
        /// var wm   = SingletonBehaviour&lt;WindowsManager&gt;.Instance;        // ①
        /// var data = 事件;                                                // ②（+0x58）
        /// var cfg  = ConfigManager.GetConfig&lt;…&gt;();                        // ③ ★ 远端 TitleData
        /// if (cfg == null || wm == null) NRE;                             // ④
        /// wm.OpenWindow(cfg.&lt;+0x28&gt;, data, options: 0, 0, 0);              // ⑤ ★ 那扇窗的 prefab 引用在 cfg 里
        /// </code>
        /// <para>本仓等价（⛔ 不是猜）：`WindowsManager.OpenWhereToGetCards()` 开的就是那扇「这张卡从哪拿」窗
        /// （`WhereToGetCardsPopup`，判据见 `WindowsManager.OpenWhereToGetCards` 的头注）。
        /// 第 ③⑤ 句里那个 prefab 引用在远端 TitleData（`CardsSpecialConfig.whereToGetCardsData`）⇒ 拿不到，
        /// 我们走的是**同一扇窗的本仓入口**，不是「另挑一扇窗」。</para></summary>
        public void GetCardsClick()
        {
            if (!_getCardsClickBound) return;                                  // 原版 `RemoveAllListeners()` 之后这颗钮不再响应
            var wm = WindowsManager.Instance;                                  // ①
            if (wm == null)                                                    // ④
            {
                Debug.LogWarning("[ExpansionPass] `GetCardsClick`：没有 `WindowsManager` ⇒ 开不了 `WhereToGetCardsPopup`"
                               + "（原版第 ⑤ 句 `OpenWindow(cfg.<+0x28>, …)`）。");
                return;
            }
            WindowsManager.OpenWhereToGetCards();                              // ⑤（③ 的远端 prefab 引用 → 本仓入口）
        }

        /// <summary>原版 `ExpansionPassEventTab.OnClose()`（`__OnClose.c` 全文 2 句）：`base.OnClose(); Reset();`。
        /// <para>我们这一侧：`base` 的收尾由 `GameWindow.Close` 承担 ⇒ 本方法只跑那一跳 `Reset()`；
        /// 由类尾的 `Close()` 覆写在关窗前调（= 原版的顺序）。</para></summary>
        public void OnClose()
        {
            if (!gameObject.activeSelf) return;      // 原版 `base.OnClose` 那道「没开就不收尾」的闸
            ExpansionPassEventTabLogic.OnClose(_event != null, _whereBtn != null); if (ExpansionPassMissionsTabLogic.ReleasesEventAssetsOnClose(_event != null)) LiveOpsAssetUtility.ReleaseEventAssetsById(_event.Id);   // 🔴 2026-10-29（B95_RD）：账 228 的**执行侧**（原版 `ExpansionPassMissionsTab.OnClose` 第 2 句）
            Reset();
        }

        /// <summary>原版 `ExpansionPassEventTab.Reset()`（`__Reset.c` 全文 3 句，逐句）：
        /// <code>
        /// CustomDebug.LogWarning(字面量没解出);                          // ⛔ 原文没解出，我们不编
        /// LiveOpsAssetUtility.Unload(事件, 1);                           // 卸背景图
        /// getCardsButton.onClick.RemoveAllListeners();                   // 摘「Where get cards」那颗钮的监听
        /// </code>
        /// <para>🔴 **2026-10-29（B95_RD）订正**：第 ② 句**不再是空跳** —— 走 <see cref="LiveOpsAssetUtility.UnloadAssets"/>（按
        /// key 取 `EventAssets` 的 GUID → 引用计数 −1 → 到 0 才释放）；第 ③ 句我们的等价 = 把该钮的命中委托置空。</para></summary>
        public void Reset()
        {
            // 🔴 `Reset()` 是 MonoBehavour 的**编辑器回调**（`AddComponent` 时会自动调一次）——
            //   那时候树还没建（`_content == null`）⇒ 原版这一跳也不可能发生 ⇒ 直接早退，
            //   ⛔ 否则会在每次建窗时白打一条告警、并把 `_getCardsClickBound` 提前置成 false。
            if (_content == null) return;
            var plan = ExpansionPassEventTabLogic.Reset(_event != null, _whereBtn != null);
            Debug.LogWarning("[ExpansionPass] `Reset`：原版那句 `LogWarning` 的**原文没解出**（`DAT_1842afd40` 那个字面量全仓只此一处，⛔ 交叉不出；`DAT_1842926d0` 是 `Unload<Int32Enum>` 的泛型上下文、⛔ 不是字符串 —— 2026-10-29 `B94_B4` 就地订正）⇒ 我们只出声、不编原文。要卸的 liveops 图 = " + string.Join("/", plan.AssetTypes) + "；摘监听 = " + plan.RemovesGetCardsListener + "。");
            for (int i = 0; i < plan.AssetTypes.Length; i++) LiveOpsAssetUtility.UnloadAssetByEventId(_event.Id, plan.AssetTypes[i]);   // 🔴 2026-10-29（B95_RD）：账 225 第 ② 句 `Unload(事件,1)` —— 从「空跳」改成**真释放**
            if (ExpansionPassMissionsTabLogic.ReleasesEventAssetsOnReset(_event != null)) LiveOpsAssetUtility.ReleaseEventAssetsById(_event.Id);   // 🔴 账 231 = `ExpansionPassMissionsTab.Reset` 的执行侧（原版全文 1 句）
            _getCardsClickBound = !plan.RemovesGetCardsListener;
        }

        /// <summary>`Where get cards` 那颗钮的命中委托**还在不在**（= 原版 `getCardsButton.onClick` 的监听者数，自检读口）。</summary>
        public bool GetCardsClickBound { get { return _getCardsClickBound; } }

        /// <summary>`Build()` 会把监听重新挂上（= 原版 `Initialize` 第 ⑤ 句的 `AddListener`）⇒ 出厂为在册。</summary>
        bool _getCardsClickBound = true;

        /// <summary>关窗前先跑原版那一跳 `OnClose`（见 `OnClose` 的 doc）。</summary>
        public override void Close()
        {
            OnClose();
            base.Close();
        }
    }

    // ════════════════════════════════════════════════════════════════════════════════════════════
    //  B93-5（2026-10-28 · `D2`）：原版 `ExpansionPassEventTab` / `ExpansionPassMissionsTab` 的
    //  **生命周期那一半**，投影成「显式入参 → 显式出参」的纯函数（同 `Shell/RaidEvent.cs` 的 `…Logic` 族）。
    //  判据（逐句）：`ExpansionPassEventTab__Initialize.c` / `__OnClose.c` / `__Reset.c` /
    //               `ExpansionPassMissionsTab__Initialize.c` / `__OnClose.c` / `__Reset.c`。
    // ════════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>原版 `ExpansionPassEventTab` 的生命周期。字段对位：`+0x30 rewardSelector` · `+0x38 backgroundImage` ·
    /// `+0x40 premiumPanel` · `+0x48 headerController` · `+0x50 getCardsButton` · `+0x68 liveOps(事件)`。</summary>
    public static class ExpansionPassEventTabLogic
    {
        /// <summary>原版 `Initialize()`（`__Initialize.c` 全文 5 跳，逐句）：
        /// <code>
        /// ① rewardSelector.Initialize(事件);
        /// ② backgroundImage.sprite = LoadAsset(事件, 1); backgroundImage.enabled = (sprite != null);
        /// ③ premiumPanel.Initialize(事件, onClosePurchasePopup);
        /// ④ headerController.Initialize(事件);
        /// ⑤ getCardsButton.onClick.AddListener(GetCardsClick);                            // ★ 接线
        ///    getCardsButton.gameObject.SetActive(cfg.whereToGetCardsData.expansionData != 0);   // ★ 远端显隐
        /// </code></summary>
        public struct InitializePlan
        {
            /// <summary>① 要不要重建奖励容器（实体 = `CreateRewards`）。</summary>
            public bool RebuildRewards;
            /// <summary>② 背景图取不取得到（本地恒 `false` ⇒ `Image.enabled = false`）。</summary>
            public bool BackgroundSpriteAvailable;
            /// <summary>③ 付费档面板要不要初始化。</summary>
            public bool ApplyPremiumPanel;
            /// <summary>④ 页头要不要初始化（`GetLabel` / `LoadAsset(事件,5)` 都在远端 ⇒ 只出声）。</summary>
            public bool HeaderInitialized;
            /// <summary>⑤ `Where get cards` 那颗钮**显不显示**（原版读远端 `whereToGetCardsData.expansionData != 0`）。</summary>
            public bool GetCardsButtonVisible;
        }

        public static InitializePlan Initialize(bool hasEvent, bool backgroundSpriteAvailable, bool remoteGetCardsFlag)
        {
            var p = new InitializePlan
            {
                BackgroundSpriteAvailable = backgroundSpriteAvailable,
                GetCardsButtonVisible = remoteGetCardsFlag,
            };
            if (!hasEvent) return p;                 // 事件为空 ⇒ 五跳**一件都不做**（原版每条都先解引用事件）
            p.RebuildRewards = true;                 // ①
            p.ApplyPremiumPanel = true;              // ③
            p.HeaderInitialized = true;              // ④
            return p;
        }

        /// <summary>原版 `OnClose()`（全文 2 句）：`base.OnClose(); Reset();` —— 我们只投影第 2 句的触发。</summary>
        public static bool OnClose(bool hasEvent, bool hasGetCardsButton) { return true; }

        /// <summary>原版 `Reset()`（全文 3 句）：
        /// `LogWarning(原文没解出)` → `Unload(事件, 1)` → `getCardsButton.onClick.RemoveAllListeners()`。</summary>
        public struct ReleasePlan
        {
            /// <summary>要卸的 `EventAssetReferenceType`（原版只有 `1` 这一个）。</summary>
            public int[] AssetTypes;
            /// <summary>要不要摘 `Where get cards` 那颗钮的监听（原版第三句那道 `!= null` 闸）。</summary>
            public bool RemovesGetCardsListener;
        }

        public static ReleasePlan Reset(bool hasEvent, bool hasGetCardsButton)
        {
            return new ReleasePlan
            {
                AssetTypes = hasEvent ? new[] { 1 } : new int[0],   // 原版 `Unload(事件,1)`（无事件时那一跳 NRE ⇒ 我们不卸）
                RemovesGetCardsListener = hasGetCardsButton,
            };
        }
    }

    /// <summary>原版 `ExpansionPassMissionsTab`（第二页）的生命周期。字段对位：`+0x30 headerController` ·
    /// `+0x50 warlordImage` · `+0x68 liveOps(事件)`。</summary>
    public static class ExpansionPassMissionsTabLogic
    {
        /// <summary>原版 `Initialize()`（`__Initialize.c` 全文 2 句）：
        /// <code>
        /// headerController.Initialize(事件);
        /// warlordImage.sprite = LiveOpsAssetUtility.LoadAsset(事件, 2);      // ★ 远端图（督军立绘）
        /// </code></summary>
        public struct InitializePlan
        {
            /// <summary>`headerController.Initialize(事件)` 是否执行（原版 `+0x30` 那道 `!= null` 闸）。</summary>
            public bool HeaderInitialized;
            /// <summary>`LoadAsset(事件, 2)` 取不取得到（本地恒 `false`）。</summary>
            public bool WarlordSpriteAvailable;
        }

        public static InitializePlan Initialize(bool hasHeaderController, bool hasEvent, bool warlordSpriteAvailable)
        {
            return new InitializePlan
            {
                HeaderInitialized = hasHeaderController && hasEvent,
                WarlordSpriteAvailable = warlordSpriteAvailable,
            };
        }

        /// <summary>原版 `OnClose()`（全文 2 句）：`base.OnClose(); LiveOpsAssetUtility.ReleaseEventAssets(事件);`。</summary>
        public static bool ReleasesEventAssetsOnClose(bool hasEvent) { return hasEvent; }

        /// <summary>原版 `Reset()`（全文 1 句）：`LiveOpsAssetUtility.ReleaseEventAssets(事件);`。</summary>
        public static bool ReleasesEventAssetsOnReset(bool hasEvent) { return hasEvent; }
    }
}
