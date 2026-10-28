// GachaEventWindow.cs — 活动线 `L01`：**Gacha 抽卡活动页**（原版 `GachaEventTab : WindowTabBase<BaseParentEventWindow>, ILiveOpsTab`）
//
// ============================ 出处（唯一判据，全部本地现读 · 2026-10-19）============================
// ① **类桩**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/`）：
//      `GachaEventTab.cs`（`gachaCratePanel` / `headerController` / `rewardsPanel` / `getCardsButton` / `getCardsWindow`）
//      `GachaCratePanel.cs`（`openCrateButton` / `openCrateHighlight` / `buyTicketsButton`(null) / `videoPlayer` /
//                           `videoBackground` / `crateOpenSound` / `crate` / `crateHighlight`）
//      `GachaRewardsPanel.cs`（`specialItemCount` / `timerDisplay` / `containerHolder` / `containerPrefab` /
//                              `allItemsCompletedObject` / `progressBar` / `doesntHaveAllItems`）
//      `GachaRewardContainer.cs`（`itsClaimedObject` / `drawerHolder` / `claimedColor` / `inprogressColor` / `claimedText`）
// ② **行为**（`d:/2/tools/decomp_full/`，方法体完整）：
//      · `BaseParentEventWindow__SetupTabs.c` → 遍历 liveops 事件：`HasComponent(0xd)` ⇒ `TabButtons.AddTabButton(label, icon)`；
//        `HasComponent(0xb)` ⇒ `BaseParentEventWindow__CreateEventTab.c` → `GameWindowWithTabs.CreateTab(label, icon, 组件, …)`
//        ⇒ **Gacha 是 `Parent Event Window` 里的一个 tab 页**（不是独立窗口）。
//      · `GachaEventTab__OnOpen.c` = `WindowTabBase.OnOpen()` + `Initialize()`（`Initialize` 里
//        `GachaEvent.CurrentContainer` 为空 ⇒ `WindowTabBase.CloseTab()`；随后把事件喂给 crate/rewards/header 三块）。
//      · `GachaEventTab__GetCardsClick.c` → `WindowsManager.OpenWindow(getCardsWindow, config)`（**本地有对应窗口** ⇒ 真接）。
//      · `GachaCratePanel__Awake.c`：`videoPlayer.enabled = false`、`videoBackground.SetActive(false)`；
//        `OpenCrateButtonClick` = 开视频 + `ContainerOfferEvent.TryPurchaseOffer`（**服务器**）。
//      · `GachaCratePanel__RefreshVisuals.c`：`crateHighlight` 是否显示由 `CratesSinceLastLegendary` 与
//        `PlayerHasAllSpecialItems` 决定（**远端数据**）。
//      · `GachaRewardsPanel__RefreshFooter.c`：`PlayerHasAllSpecialItems` 切 `Player doesn't have all items` /
//        `Player has all items` 两块；进度 = `ProgressBar.SetProgress(cratesSinceLastLegendary, max)`。
//      · `GachaRewardContainer__Initialize.c`：按 `currentNumberOfItems / maxNumberOfItems` 生成 `"{0}/{1}"`，
//        `current >= max` 时改印本地化 `Rewards Menu/Claimed`。
// ③ **prefab / 节点树**（`bundle_menus_assets_all`）：
//      · 根 `Gacha Tab`（GO `-5969151252855400831`；组件 = `RectTransform` + `CanvasRenderer` + `GachaEventTab`）。
//      · 全树 dump（可复跑；下面每个数都能逐字找到）：
//        `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Gacha Tab" --depth 12 --relative`
//        落地副本 → `d:/4/_tmp_view/gacha_tab_dump.txt`（2026-10-19 现读）。
//      · 根 RectTransform：`anchor (0,0)-(1,1)` · `anchoredPosition (82,0)` · `sizeDelta (-163.575,-0.359)`
//        ⇒ 在 1920 宽的父里落到 **x 163.79..1920.21 · y 0.18..1079.82**（`PageDx/PageDy`）。
//      · 根 MB（`MonoBehaviour_-665041264658295167.json`）的五个 PPtr → 三块子件（Chest / Header / Rewards）。
// ④ **本地化键**（`Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`，逐条命中）：
//      `Event_Description/Gacha/TabTitle` = The Vault / 金库
//      `Event_Description/General/EndsIn` = Ends in: / 结束于：
//      `Event_Description/ExpansionPass/Vault/SpecialItems` = Special Items / 特殊物品
//      `Event_Description/Gacha/GuaranteeRewardInfo`（prefab 的 Footer Text 原文）
//      `Event_Description/Gacha/AllRewards`（Player has all items 那块）
//      `Event_Description/ExpansionPass/Vault/{Completed,CompletedTip}`（Completed 覆盖层）
//      `Rewards Menu/Claimed`（奖励格）
//      `Event_Description/BattlePass/WhereButton`（Where get cards 钮）
// ⑤ **承载窗外壳** = `Shell/ParentEventWindow.cs`（界面线 `T28`，原版 `BaseParentEventWindow` 的树：
//      `Menu Dark Background` + `Content Area/Background` + `Tab Buttons` 左栏 + `Tabs` 容器）。
//      本件**直接复用它的 public 常量**（矩形 / 渐变 / 页签几何 / 色值）—— 判据只留一份，⛔ 别抄第二套。
//
// ============================ 🔴 这一扇为什么是【独立窗口】而不是 ParentEventWindow 的第三页 ============================
// 原版：`BaseParentEventWindow.SetupTabs` 按**远端 liveops 事件列表**动态建页；Gacha 只是其中一种 tab。
// 本地：**没有 liveops 事件列表**（远端 PlayFab/CCD）⇒ 无法知道「Gacha 与 Two Sides 是否同时在线」。
// 处置：建「**承载窗外壳 + 恰好一个 Gacha 页**」= 原版在「只有 Gacha 这一个活动」时的等价形态；
//      ⛔ 不把 Two Sides 那两页拉进来假装同时在开（那是编数据）。
// 根名 `Gacha Tab` = 原版**页** prefab 的根名（原版没有独立窗口 prefab，如实记录）。
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编）============================
// 活动数据全部来自 **LiveOps（远端 PlayFab / CCD）**，本地离线包体里没有：
//   · `GachaEvent.CurrentContainer` / `CurrentOffer` / `Offers`（宝箱价格、票数、掉落表）
//   · `CrateInfo.CratesSinceLastLegendary` / `maxCrates`（进度分母）
//   · `SpecialDropTable` 里的 Special Items 列表（奖励格逐个 `GachaRewardContainer.Initialize` 的数据）
//   · 事件截止时刻（`TimerDisplay` 的目标时间）
//   · 页签图标（`LiveOpsAssetUtility.LoadAsset(事件, 0)` 的 sprite）
// ⇒ 本窗照 **prefab 出厂结构**建满，**数值一律 `--` 占位**（⛔ 不编日期/价格/进度/奖励），
//    并在 `Open()` 里 `Debug.LogWarning` **逐条出声**（本工程红线：不许静默失败）。
//    **开箱**：原版 = `TryPurchaseOffer`（服务器）+ 播 `Gacha Crate Opening.mp4` ⇒ 本地点击**只出声**，不假装开箱。
// 🔴 **一处有意偏离（照实记录）**：原版 `GachaEventTab.Initialize` 在 `GachaEvent.CurrentContainer == null` 时
//    会调 `WindowTabBase.CloseTab`（即 **没数据就没有这个 tab**）。本地**恒无**该数据 ⇒ 若照搬，页面永远建不出来；
//    本任务的验收口径是「**页面能打开、看得到原版结构、空态出声**」⇒ 我们**保留页面**并印 `--` 占位。
//    ⛔ 别把这一条写成「原版空数据也显示页面」—— 原版不是那样。
//
// ============================ 我们侧的对位（逐件写清，不冒充原版）============================
// · **页签栏**：原版 `TabButtons.AddTabButton(label, sprite)` 由 liveops 数据驱动；本窗只有一颗
//   「The Vault」页签（文字走 `Loc`），图标留空（liveops sprite 本地没有）。
// · **`Video Player` / `Video Background`**：建节点；不挂 `VideoPlayer`（clip 本地**有**、只是未导入 `Resources/`，见 `:645`）、`Video Background` 出厂关着。
// · **`Gacha Drawer Holder`**：`containerPrefab`（运行时逐奖励 Instantiate）的**模板**；无数据 ⇒ 建出来、**关着**。
// · **两张图未导入 `Resources/`**（源图在本地，落盘名可查）：
//     `40k_Crate_Tier5_Warp_premium`（`bundle_boosterpacks_assets_all/Texture2D/`）⇒ 回退 `40k_Crate_Tier5_Warp`；
//     `Glow UI sharp W40K`（`Assets/CardPresentation/Art/原版/去重资源/Glow_UI_sharp_W40K.png`）⇒ 回退 `Glow_UI_W40K`。
//   两处都**出声一次**并记进 `MissingArt` 之外的 `ArtNotes`（自检读口）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>Gacha 抽卡活动页（原版 `GachaEventTab`）—— **承载窗外壳照 `Parent Event Window`、内容照 `Gacha Tab` prefab**。
    /// <para>🔴 活动数据全在远端 LiveOps ⇒ 结构照建、数值 `--` + 出声（⛔ 不编；判据 → 文件头「数据缺口」）。</para>
    /// <para>✅ **2026-10-23 起有生产入口了**：左竖导航第 6 颗 `Event Navigation Button` →
    /// `WindowsManager.OpenEventHub(...)`；本窗左栏的「事件钮」（<see cref="ParentEventWindow.SetupEventTabs"/>
    /// 建的）也走同一个口。直接调用 = `WindowsManager.OpenGachaEvent()`。</para></summary>
    public class GachaEventWindow : GameWindow
    {
        // ============================================================ 渲染队列档（3630–3659 · 全工程现读空档）
        // 相邻带：`ParentEventWindow` 3610–3629 · `Tooltip` 4077–4079（2026-10-20 W58 从 3605 搬来）· 下一档 3935（Energy）。
        // 🔴 梯子（每层 +1；同档时 `ImageQuad` 世界 z 恒 0 ⇒ 谁盖谁不可控 —— CLAUDE.md §三）：
        public const int QShade = 3630;        // `Menu Dark Background`（含「点暗底关窗」命中区）
        public const int QContentBg = 3631;    // `Content Area/Background` 渐变（低于所有页内容）
        const int QChestBg = 3632;             // 左页：箱体背后的 `40k_shop_popup_info_bg`
        const int QChestShadow = 3633;         // 左页：两片 `Crate Shadow`
        const int QChestHi = 3634;             // 左页：`Highlight Crate`（箱体金/紫高亮框）
        const int QChestCrate = 3635;          // 左页：`Crate` 本体
        const int QChestButton = 3636;         // 左页：`Price Display Button` 的高亮框
        const int QChestButton1 = 3637;        // 左页：`Open Chest Button` 按钮底（压在框上）
        const int QChestText = 3638;           // 左页：按钮文字 / 表头文字
        // 🔴 右栏 `Rewards Panel` 与左页在 x 1042–1143 那一条**重叠**，而原版兄弟序是 Chest(0) → Rewards(1)
        //   ⇒ 右栏整族必须**严格高于**左页（3639+）。
        const int QRewBg = 3639;               // 右栏：`Background Border`
        const int QRewFill = 3640;             // 右栏：`Background fill`（平铺纹理）
        const int QRewArt = 3641;              // 右栏：滚动区底 / 奖励格 / 进度条
        const int QRewArt1 = 3642;             // 右栏：奖励格上那一层
        const int QRewText = 3643;             // 右栏：全部文字
        const int QRewBar = 3644;              // 右栏：进度条填充 / 描边
        const int QVideo = 3645;               // 整页视频黑场（原版页根最后一件 ⇒ 盖住全部页内容；出厂 INACT）
        // 左侧页签栏（必须高于**页内全部件**：页签栏与页在 x 163.8–332.2 重叠）
        const int QPanel = 3646;               // `Tab Buttons` 底板
        const int QTabShadow = 3647;
        const int QTabHi = 3648;               // 选中高亮
        const int QTabIcon = 3649;
        const int QTabName = 3650;             // 页签名牌底
        const int QTabLabel = 3651;
        const int QBadge = 3652;
        const int QBadgeText = 3653;
        /// <summary>本窗内容命中区那一档（压暗层命中区必须严格低于它；`Absorb` 取 `QHit − 1`）。</summary>
        public const int QHit = 3659;

        // ============================================================ 根 / 节点名
        /// <summary>prefab 根名（= `Create()` 里 `new GameObject(...)` 用的名字，**两处同源**）。
        /// ⚠️ 这是原版**页** prefab 的名字（原版没有独立窗口 prefab，见文件头）。</summary>
        public const string RootName = "Gacha Tab";
        public const string PageNodeName = "Gacha Tab Page";
        public const string TabButtonName = "Gacha Tab Button";

        // ============================================================ 窗参（`Parent Event Window` 根 MB 实读）
        // `type = 0`(Fullscreen) · `windowsPlacement = 5`(Canvas) · `closeOnESC = 1` · `updateNavPanel = 1`
        // · `extraScaleSmallScreen = 1.0` · `useDefaultCloseSoundIfNull = 1` —— 逐字段实读
        //   `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_6349037533193994564.json`。
        // ⚠️ 与 `Two Sides Event Window` 那份（15/0/0）**不同** —— 本件照**承载窗自己**那一份。
        public const bool ParamsCloseOnEsc = true;          // `closeOnESC = 1`
        public const bool ParamsUpdateNavPanel = true;      // `updateNavPanel = 1`
        public const float ParamsExtraScale = 1f;           // `extraScaleSmallScreen = 1.0`
        public const bool ParamsUseDefaultCloseSound = true;

        // ============================================================ 几何：页根偏移（根 RectTransform 实读）
        // `anchor (0,0)-(1,1)` · `pos (82,0)` · `size (-163.575,-0.359)` · 父 1920×1080
        // ⇒ 页根 x **163.7875..1920.2125** · y **0.1795..1079.8205**。
        public const float PageDx = 163.7875f, PageDy = 0.1795f;
        public const float PageW = 1756.425f, PageH = 1079.641f;

        // ============================================================ 几何：左页 `Chest panel`（dump 相对值 + PageDx/PageDy）
        static PxRect R(float x1, float y1, float x2, float y2)
            => new PxRect(x1 + PageDx, y1 + PageDy, x2 + PageDx, y2 + PageDy);

        static readonly PxRect ChestR = R(-107.0f, -0.1f, 979.5f, 1079.6f);
        /// <summary>`Mask` 布局框 831.95×921.93、自带 `localScale = 1.3` ⇒ **视觉框 1081.54×1198.51**。</summary>
        static readonly PxRect MaskR = Center(R(20.3f, 130.8f, 852.2f, 1052.7f), 1081.54f, 1198.51f);
        static readonly PxRect MaskBgR = R(-348.2f, -296.6f, 1360.4f, 1481.5f);   // 已含父链 1.3 缩放（dump 口径）
        static readonly PxRect Shadow1R = R(-1.8f, 590.4f, 751.8f, 844.9f);
        static readonly PxRect Shadow2R = R(-60.8f, 533.5f, 561.3f, 788.0f);
        static readonly PxRect CrateHiR = R(13.3f, 162.9f, 859.2f, 916.6f);
        static readonly PxRect CrateR = R(180.2f, 283.8f, 692.2f, 795.8f);
        static readonly PxRect PriceBtnR = R(221.6f, 794.0f, 650.9f, 903.6f);
        static readonly PxRect PriceBtnHiR = R(177.4f, 761.3f, 692.2f, 936.3f);
        static readonly PxRect OpenBtnR = R(221.6f, 794.0f, 650.9f, 903.6f);
        static readonly PxRect OpenBtnTextR = R(234.6f, 809.6f, 637.9f, 888.0f);   // 出厂 INACT
        static readonly PxRect PriceDispR = R(237.9f, 809.4f, 631.3f, 889.9f);
        static readonly PxRect TicketIconR = Center(R(394.3f, 809.4f, 474.8f, 889.9f), 96.65f, 96.65f);  // 自带 ×1.2
        static readonly PxRect TicketTextR = Center(R(482.9f, 809.4f, 482.9f, 889.9f), 120f, 80.54f); // 原 pivot .5 + CSF
        static readonly PxRect VideoPlayerR = R(386.2f, 489.8f, 486.2f, 589.8f);

        // ---- 左页表头 ----
        static readonly PxRect HeaderR = R(-107.0f, -0.1f, 979.5f, 293.8f);
        static readonly PxRect HeaderBgR = R(-107.0f, 23.1f, 979.5f, 199.0f);      // 出厂 INACT
        static readonly PxRect TitleR = R(186.2f, 41.7f, 686.2f, 179.9f);           // 原 pivot 0.5 + CSF（中心 x 436.2）
        static readonly PxRect HelpIconR = R(456.1f, 84.7f, 508.2f, 136.9f);       // 出厂 INACT
        static readonly PxRect ArmyIconR = R(-102.0f, 56.8f, 18.0f, 176.8f);       // 出厂 INACT / 无图
        static readonly PxRect TimeRemainR = R(236.2f, 161.8f, 436.2f, 211.8f);    // pivot 1.0 ⇒ 右对齐
        static readonly PxRect TimeClockR = R(436.2f, 162.0f, 485.8f, 211.6f);
        static readonly PxRect TimeTextR = R(488.6f, 161.8f, 598.9f, 211.8f);
        static readonly PxRect GetCardsR = R(257.9f, 940.5f, 614.6f, 999.0f);
        static readonly PxRect GetCardsTextR = R(270.4f, 926.6f, 601.7f, 1012.9f);
        static readonly PxRect VideoBgR = R(-3150.1f, -1359.4f, 4896.2f, 2526.1f); // 出厂 INACT

        // ============================================================ 几何：右栏 `Rewards Panel`
        static readonly PxRect RewardsPanelR = R(878.2f, -0.5f, 1756.4f, 1079.6f);
        static readonly PxRect RewardsTopR = R(887.4f, 8.6f, 1748.3f, 1070.5f);    // 出厂 INACT
        static readonly PxRect RewHeaderR = R(887.4f, 9.9f, 1745.9f, 239.3f);
        static readonly PxRect RewTitleR = R(900.9f, 9.9f, 1745.9f, 239.2f);
        static readonly PxRect RewTimerR = R(1066.6f, 161.3f, 1316.6f, 212.3f);    // pivot 1.0 ⇒ 右对齐
        static readonly PxRect RewClockR = R(1316.6f, 162.0f, 1366.2f, 211.5f);
        static readonly PxRect RewTimeR = R(1369.0f, 161.3f, 1479.3f, 211.3f);
        static readonly PxRect RewScrollR = R(878.2f, 239.3f, 1756.6f, 808.9f);
        static readonly PxRect RewContentR = R(1167.4f, 269.3f, 1467.4f, 778.9f);
        static readonly PxRect DrawerR = R(1167.4f, 269.3f, 1467.4f, 778.9f);      // `Gacha Drawer Holder`
        static readonly PxRect ClaimedR = R(1177.3f, 286.1f, 1402.9f, 354.0f);
        static readonly PxRect ClaimedTextR = R(1220.6f, 300.8f, 1333.2f, 339.3f);
        static readonly PxRect FooterR = R(878.2f, 787.8f, 1756.6f, 1080.0f);
        static readonly PxRect RewardCountR = R(892.6f, 814.9f, 1747.4f, 901.9f);
        static readonly PxRect NoAllItemsR = R(1010.4f, 874.8f, 1624.4f, 1061.8f);
        static readonly PxRect GuaranteeTextR = R(1010.4f, 974.3f, 1624.4f, 1046.3f);
        static readonly PxRect ProgressR = R(1060.4f, 904.5f, 1574.4f, 966.9f);
        static readonly PxRect ProgressBgR = R(1060.4f, 917.0f, 1574.4f, 954.4f);
        static readonly PxRect FillAreaR = R(1060.4f, 921.2f, 1574.4f, 950.2f);
        static readonly PxRect FillR = R(1060.4f, 950.2f, 1060.4f, 950.2f);        // 出厂 0 宽
        static readonly PxRect FillEndR = R(1028.4f, 924.9f, 1068.3f, 979.2f);
        static readonly PxRect ProgressiveOutlineR = R(1060.4f, 917.0f, 1574.4f, 954.4f);
        static readonly PxRect CounterR = R(1163.2f, 914.5f, 1471.6f, 961.7f);
        static readonly PxRect AllItemsR = R(888.2f, 874.8f, 1746.6f, 1080.0f);    // 出厂 INACT
        static readonly PxRect AllItemsTextR = R(888.2f, 894.0f, 1746.6f, 1054.4f);
        static readonly PxRect CompletedR = R(878.2f, -0.5f, 1756.4f, 1079.6f);    // 出厂 INACT
        static readonly PxRect CompletedMsgR = R(878.2f, 217.1f, 1756.4f, 862.5f);
        static readonly PxRect CompletedBannerR = R(878.2f, 387.7f, 1756.4f, 691.5f);
        static readonly PxRect CompletedTitleR = R(878.2f, 387.7f, 1756.4f, 689.8f);
        static readonly PxRect CompletedInfoR = R(878.2f, 613.1f, 1756.4f, 846.6f);

        // ============================================================ 图 / 色（Image MB 逐字段实读）
        const string ArtChestBg = "40k_shop_popup_info_bg";     // 565×588
        const string ArtCrate = "40k_Crate_Tier5_Warp";          // 原版 `..._premium` 未导入 ⇒ 回退（出声）
        const string ArtCrateHg = "Crate_Border_Highlight";      // 128×128
        const string ArtGlow = "Glow_UI_W40K";                   // 原版 `Glow UI sharp W40K` 未导入 ⇒ 回退（出声）
        const string ArtBtn = "40K_button";                      // 489×107 九宫 234,46,234,46
        const string ArtOctagon = "OctagonUI_Filled_SDF";        // 128×128 九宫 52,52,52,52
        const string ArtTicket = "40k_topmarquee_currency_ticket"; // 90×90
        const string ArtInfo = "40K_generic_bt_info";            // 128×128
        const string ArtClock = "WF_icon_clock";                 // 64×64（原版 sprite 名带 `for shader`）
        const string ArtDropdownBg = "40K_dropdown_bg";          // 119×102 九宫 23,20,23,20
        const string ArtPopupTex = "40k_popup_texture";          // 128×128（Tiled）
        const string ArtClaim = "WF_Special_offer_Value";        // 324×87 九宫 162,0,162,0
        const string ArtBarBg = "40k_campaign_bar_bg";           // 42×18 九宫 20,0,20,0
        const string ArtBarFill = "40k_campaign_bar_fill";       // 42×18 九宫 10,0,10,0
        const string ArtBarEnd = "40k_campaign_bar_end";         // 22×18
        const string ArtBarOutline = "40k_campaign_bar_outline"; // 46×22 九宫 20,0,20,0
        const string ArtBanner = "40k_UI_Banner_BW";             // 624×190

        static readonly Color ChestBgTint = new Color(0.3254717f, 0.8159437f, 1f, 1f);
        static readonly Color ShadowTint = new Color(0.09433961f, 0.00222499f, 0.00222499f, 0.5960785f);
        static readonly Color CrateHiTint = new Color(0.754717f, 0.1317195f, 0.4443462f, 1f);
        static readonly Color OpenBtnTint = new Color(1f, 0.4103774f, 0.6266298f, 1f);
        static readonly Color GetCardsTint = new Color(0.5970096f, 0.9811321f, 0.6578578f, 1f);
        static readonly Color DropdownTint = new Color(0.1792453f, 0.1792453f, 0.1792453f, 1f);
        static readonly Color ScrollBgTint = new Color(0.05660379f, 0.05660379f, 0.05660379f, 1f);
        static readonly Color ClaimTint = new Color(0.372549f, 0.7529412f, 0.3934071f, 1f);
        static readonly Color BarBgTint = new Color(0.2735849f, 0.06581523f, 0.1231214f, 1f);
        static readonly Color BarFillTint = new Color(1f, 0.740566f, 0.8215345f, 1f);
        static readonly Color BarEndTint = new Color(1f, 0.3820755f, 0.6042997f, 0.6980392f);
        static readonly Color BarOutlineTint = new Color(0.6698113f, 0.6603329f, 0.6640189f, 1f);
        static readonly Color BannerTint = new Color(0.8784314f, 0.7568628f, 0.345098f, 1f);
        static readonly Color CompletedShadeTint = new Color(0f, 0f, 0f, 0.8588235f);
        static readonly Color VideoShadeTint = new Color(0f, 0f, 0f, 0.7803922f);
        static readonly Color HeaderBgTint = new Color(0f, 0f, 0f, 1f);

        // ============================================================ 本地化键（原版 `Localize.mTerm` 实读）
        public const string TermTitle = "Event_Description/Gacha/TabTitle";
        public const string TermEndsIn = "Event_Description/General/EndsIn";
        public const string TermSpecialItems = "Event_Description/ExpansionPass/Vault/SpecialItems";
        public const string TermGuarantee = "Event_Description/Gacha/GuaranteeRewardInfo";
        public const string TermAllRewards = "Event_Description/Gacha/AllRewards";
        public const string TermCompleted = "Event_Description/ExpansionPass/Vault/Completed";
        public const string TermCompletedTip = "Event_Description/ExpansionPass/Vault/CompletedTip";
        public const string TermClaimed = "Rewards Menu/Claimed";
        public const string TermWhereButton = "Event_Description/BattlePass/WhereButton";

        /// <summary>无数据占位（⚠️ **不是原文**：远端数据缺 ⇒ 只印占位，⛔ 不编数字）。</summary>
        public const string NoData = "--";

        // ============================================================ 状态 / 自检读口
        public static GachaEventWindow LastOpened { get; private set; }
        /// <summary>活动数据本地没有（恒 true —— 见文件头「数据缺口」）。</summary>
        public bool DataMissing { get { return true; } }
        public string DataNote
        {
            get
            {
                return "Gacha 活动数据全在远端 LiveOps（宝箱价格/票数/掉落表/进度/截止时刻/页签图标）——"
                     + "本地离线包体里没有 ⇒ 结构照 prefab 建满、数值一律 `--` 占位，⛔ 不编。"
                     + "开箱 = 原版 `ContainerOfferEvent.TryPurchaseOffer`（服务器）⇒ 本地点击只出声。";
            }
        }

        Transform _shade, _contentArea, _background, _tabButtons, _tabShadow, _tabs, _tabMaster;
        Transform _page, _chest, _crate, _openCrateBtn, _getCardsBtn, _rewardsPanel, _header, _videoBg,
                  _rewardsScroll, _drawer, _completed, _progress;
        Label _title, _timeLabel, _rewardTitle, _rewardTime, _rewardCount, _counter, _guarantee, _allItems,
              _completedTitle, _completedInfo, _claimedText, _tabLabel;
        readonly List<Transform> _tabButtonsList = new List<Transform>();
        readonly List<Transform> _tabHi = new List<Transform>();
        readonly List<Label> _tabLabels = new List<Label>();
        // 🆕 2026-10-23（活动线 `L01`）：**事件钮**（原版 `BaseParentEventWindow.SetupTabs` 在任何一扇
        //   活动窗的左栏都列全部 live 事件 ⇒ Gacha 这扇也要有）。**与内容页那三张表分家**
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
        /// <summary>「原版用 A、我们只能回退 B」的图（自检读口；⛔ 与 `MissingArt` 不是一回事）。</summary>
        public List<string> ArtNotes { get { return _artNotes; } }

        public Transform ShadeNode { get { return _shade; } }
        public Transform ContentAreaNode { get { return _contentArea; } }
        public Transform TabButtonsNode { get { return _tabButtons; } }
        public Transform TabsNode { get { return _tabs; } }
        public Transform PageNode { get { return _page; } }
        public Transform ChestNode { get { return _chest; } }
        public Transform CrateNode { get { return _crate; } }
        public Transform OpenCrateButtonNode { get { return _openCrateBtn; } }
        public Transform GetCardsButtonNode { get { return _getCardsBtn; } }
        public Transform RewardsPanelNode { get { return _rewardsPanel; } }
        public Transform HeaderNode { get { return _header; } }
        public Transform VideoBackgroundNode { get { return _videoBg; } }
        public Transform RewardsScrollNode { get { return _rewardsScroll; } }
        public Transform DrawerTemplateNode { get { return _drawer; } }
        public Transform CompletedNode { get { return _completed; } }
        public Transform ProgressBarNode { get { return _progress; } }
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
        public Label TimeLabel { get { return _timeLabel; } }
        public Label RewardTitleLabel { get { return _rewardTitle; } }
        public Label RewardTimeLabel { get { return _rewardTime; } }
        public Label RewardCountLabel { get { return _rewardCount; } }
        public Label CounterLabel { get { return _counter; } }
        public Label GuaranteeLabel { get { return _guarantee; } }
        public Label AllItemsLabel { get { return _allItems; } }
        public Label CompletedTitleLabel { get { return _completedTitle; } }
        public Label CompletedInfoLabel { get { return _completedInfo; } }
        public Label ClaimedLabel { get { return _claimedText; } }

        // ============================================================ 开
        /// <summary>建一扇（`WindowsManager.OpenGachaEvent` 的 `create` 那一跳）。</summary>
        public static GachaEventWindow Create(WindowsManager mgr)
        {
            var go = new GameObject(RootName);                        // 节点名照 prefab（`OpenByRef` 的键同源）
            var win = go.AddComponent<GachaEventWindow>();
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
            else { Debug.LogWarning("[Gacha] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
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
        /// `Canvas.sortingOrder`。本窗占 3630–3659，而能从本窗开出去的 `WhereToGetCardsPopup` 在
        /// 3479–3487 ⇒ 只写 state 的话，**弹窗会被本窗整个盖住**（症状 = 「点 Where get cards 没反应」，
        /// 静默失败）。处置照 `MainMenuWindow.ToBackground` 的形状：**被压背景时把整棵关掉**；
        /// 关掉弹窗时 `WindowsManager.ShowPreviousWindow` 会对列表尾调无参 `TryOpen()`，
        /// 它的 `Background` 支会 `SetActive(true)` + 回 `Open` 态 ⇒ 本窗自动回到台上。
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
            Debug.LogWarning("[Gacha] " + DataNote);
            Debug.LogWarning("[Gacha] ⚠️ **有意偏离**：原版 `GachaEventTab.Initialize` 在 "
                           + "`GachaEvent.CurrentContainer == null` 时会 `WindowTabBase.CloseTab`（没数据就没有这个 tab）；"
                           + "本件的验收口径 = **页面能打开 + 空态出声** ⇒ 保留页面、数值 `--`（见文件头）。");
            Debug.LogWarning("[Gacha] 页签图标留空：原版由 `TabButtons.AddTabButton(label, sprite)` 传入 liveops sprite，"
                           + "本地没有 ⇒ ⛔ 不用母版的 `40K_shop_bt_boosters` 冒充。");
            Debug.LogWarning("[Gacha] 开箱按钮：原版 `GachaCratePanel.OpenCrateButtonClick` → "
                           + "`ContainerOfferEvent.TryPurchaseOffer`（服务器）+ 播 `Gacha Crate Opening.mp4`；"
                           + "本地没有 liveops 后端与视频 ⇒ **点击只出声、不假装开箱**。");
            Debug.LogWarning("[Gacha] 奖励池为空：原版 `GachaRewardsPanel.RefreshContainers` 按 "
                           + "`SpecialDropTable` 逐件 `Instantiate(GachaRewardContainer)`；本地没有掉落表 ⇒ "
                           + "只建模板（`Gacha Drawer Holder`，关着）+ 数值 `--`。");
            if (_artNotes.Count > 0)
                Debug.LogWarning("[Gacha] 有 " + _artNotes.Count + " 条图是**回退**（原版那张未导入 `Resources/`）："
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
            _page = _chest = _crate = _openCrateBtn = _getCardsBtn = _rewardsPanel = _header = _videoBg = null;
            _rewardsScroll = _drawer = _completed = _progress = null;
            _title = _timeLabel = _rewardTitle = _rewardTime = _rewardCount = _counter = _guarantee = null;
            _allItems = _completedTitle = _completedInfo = _claimedText = _tabLabel = null;

            BuildShell();
            BuildTabBar();
            BuildPage();
            BuildShellTail();
            // 🆕 2026-10-23（活动线 `L01`）：左栏的「事件钮」（原版 `BaseParentEventWindow.SetupTabs` 在
            //   **每一扇**活动窗的左栏都列全部 live 事件）。兄弟序 = 内容页在前、事件钮在后。
            SetupTabs(ParentEventWindow.BuildEventTabs(ParentEventWindow.EventWindowKind.Gacha,
                                                       LiveOpsLocalCatalog.NowUtc));

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Gacha] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
        }

        // ============================================================ 承载窗外壳（= `Parent Event Window` 那一族）
        void BuildShell()
        {
            // ---- 0) `Menu Dark Background`（整屏压暗；原版 `backgroundCloseButton` 与表头返回钮同一个 delegate
            //        ⇒ 点暗底 = 关窗；判据 → `Shell/ParentEventWindow.cs` 文件头 ④，同一族口径）----
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
            var lbl = Loc.T(TermTitle);
            var tab = BuildTabButton(TabButtonName, ParentEventWindow.TabTop, lbl, false);
            _tabButtonsList.Add(tab);
            _tabLabel = _tabLabels.Count > 0 ? _tabLabels[_tabLabels.Count - 1] : null;
            // 单页：点页签只重选当前页。原版 `EverguildToggle.allowClickWhenAlreadySelected = 0`
            // ⇒ 已选中时**连按钮音一起跳过**（同 `ParentEventWindow` 页签的口径）。
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

        /// <summary>🆕 2026-10-23（活动线 `L01`）：**建左栏的「事件钮」** —— 与 `ParentEventWindow`
        /// **走同一份判据**（`ParentEventWindow.SetupEventTabs` / `BuildEventTabs`；本类只提供自己的
        /// `BuildTabButton` 与自己的渲染队列档，⛔ 不在这里再抄一份 `SetupTabs` 的实现）。
        /// <para>内容页占 1 颗（`The Vault` 那一页）⇒ 事件钮从第 2 格（`TabTop + TabH`）往下排。</para></summary>
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

        // ============================================================ `Gacha Tab` 页（原版页根：x 163.79..1920.21）
        void BuildPage()
        {
            // `Tabs`（原版 `TabbedWindowComponents.tabHolder`）—— 兄弟序在 `Tab Buttons` 之后、`Shadow (1)` 之前
            _tabs = MenuDraw.Node(_contentArea, "Tabs", ParentEventWindow.TabsR);
            _page = MenuDraw.Node(_tabs, PageNodeName, R(0f, 0f, PageW, PageH));
            BuildChest(_page);
            BuildRewardsPanel(_page);
            // `Video Background`（出厂 INACT · 黑 α.78 · 整屏盖住）
            _videoBg = MenuDraw.Node(_page, "Video Background", VideoBgR);
            MenuDraw.Rect(_videoBg, CardArt.Solid(), VideoBgR, "Image", QVideo, VideoShadeTint);
            _videoBg.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ 左页 `Chest panel`
        void BuildChest(Transform page)
        {
            _chest = MenuDraw.Node(page, "Chest panel", ChestR);

            // `Mask`（RectMask2D）→ 我们走 `ViewportClip`（视觉框 = 布局框 × 自带 1.3）
            var mask = ViewportClip.Hang(_chest, "Mask", MaskR, Vector4.zero, Vector2Int.zero).transform;

            // `Background`（`40k_shop_popup_info_bg` 565×588 Simple · tint 淡蓝；矩形已含父链 1.3）
            MenuDraw.Rect(mask, Tex(ArtChestBg, "箱体背景"), MaskBgR, "Background", QChestBg, ChestBgTint);

            // 两片 `Crate Shadow`（`Glow UI sharp W40K` · tint 暗红 α.596 · 各自绕 z 转；quat 实读 4.7137 / −6.6537）
            BuildCrateShadow("Crate Shadow", Shadow1R, 4.7137f);
            BuildCrateShadow("Crate Shadow", Shadow2R, -6.6537f);

            // `Highlight Crate`（`Crate Border Highlight` · tint 紫 · 在箱体之下）
            MenuDraw.Rect(_chest, Tex(ArtCrateHg, "箱体高亮框"), CrateHiR, "Highlight Crate", QChestHi, CrateHiTint);

            // `Crate`（原版 `40k_Crate_Tier5_Warp_premium` 未导入 ⇒ 回退普通 Tier5，出声）
            _crate = MenuDraw.Node(_chest, "Crate", CrateR);
            MenuDraw.Rect(_crate, TexCrate(), CrateR, "Image", QChestCrate, Color.white);

            BuildPriceButton(_chest);
            BuildHeader(_chest);

            // `Generic Simplified UI Button (1)`（Where get cards · `40K_button` Sliced · tint 绿）
            _getCardsBtn = MenuDraw.Node(_chest, "Generic Simplified UI Button (1)", GetCardsR);
            MenuDraw.Nine(_getCardsBtn, Tex(ArtBtn, "Where get cards 钮底"), GetCardsR,
                          new Vector4(234f, 46f, 234f, 46f), 489f, 107f, QChestButton, GetCardsTint, true, "Image",
                          new Vector4(234f / 1.51f, 46f / 1.51f, 234f / 1.51f, 46f / 1.51f));
            var gcd = MenuDraw.TextBox(_getCardsBtn, GetCardsTextR, Loc.T(TermWhereButton), Color.white, "Button Text",
                                       55f, 10f, QChestText, 55f, 12f, face: TmpFont.Face.LatinCardBody);
            if (gcd != null)
            {
                gcd.SetWrapping(false);
                MenuDraw.SetVAlign(gcd, Label.VAlign.Capline, GetCardsTextR);
            }
            // 原版 `GachaEventTab.GetCardsClick` → `WindowsManager.OpenWindow(getCardsWindow, config)`。
            // 🔴 本地有对应窗口（`WhereToGetCardsPopup`）⇒ 真接，⛔ 不是空按钮。
            MenuDraw.Hit(_getCardsBtn, "Hit", GetCardsR, QHit, () =>
            {
                var wm = WindowsManager.Instance;
                if (wm != null) WindowsManager.OpenWhereToGetCards();
                else Debug.LogWarning("[Gacha] `Where get cards`：没有 `WindowsManager` ⇒ 开不了 "
                                    + "`WhereToGetCardsPopup`（原版那一跳是 `GachaEventTab.GetCardsClick` → "
                                    + "`WindowsManager.OpenWindow`）。");
            }, null, null, null, null, null, clickCue: WindowSounds.PositiveCue);
            _getCardsBtn.gameObject.SetActive(true);

            // `Video Player`（原版挂 `VideoPlayer`；🔴 2026-10-28 B89_E17 铁律 5 订正：这句原写「本地没有 `Gacha Crate Opening.mp4`」= **假** —— clip 在 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/VideoClip/Gacha Crate Opening.mp4`（5,141,522 B），只是**没导入 `Resources/Art/videos/`**（同目录那四支 defeat/draw/intro/victory 就是从同族 mp4 转出来的）⇒ 今天只建空节点。真正的闸不是素材：原版点击 = 播视频 + `ContainerOfferEvent.TryPurchaseOffer`（服务器；掉落表本地没有）⇒ 播了却没有回包 = 假装开箱）
            MenuDraw.Node(_chest, "Video Player", VideoPlayerR);
        }

        void BuildCrateShadow(string name, PxRect r, float angleZ)
        {
            var n = MenuDraw.Node(_chest, name, r);
            n.localRotation = Quaternion.Euler(0f, 0f, angleZ);   // 原版 `m_LocalRotation` 实读
            // 原版 sprite = `Glow UI sharp W40K`（本地只在 `Art/原版/去重资源/` 里、未进 `Resources/`）
            // ⇒ 回退已导入的 `Glow_UI_W40K`（软光晕，形状略钝），出声一次。
            if (CardArt.MenuUi("Glow_UI_sharp_W40K") == null)
                Note("箱体光晕：原版 `Glow UI sharp W40K` 未导入 `Resources/`"
                    + "（源 = `Assets/CardPresentation/Art/原版/去重资源/Glow_UI_sharp_W40K.png`）"
                    + " ⇒ 回退 `Glow_UI_W40K`");
            MenuDraw.Rect(n, Tex(ArtGlow, "箱体阴影光晕"), r, "Image", QChestShadow, ShadowTint);
        }

        void BuildPriceButton(Transform chest)
        {
            _openCrateBtn = MenuDraw.Node(chest, "Price Display Button", PriceBtnR);

            // `Button Higlight border`（`OctagonUI Filled SDF` 九宫 52 · ppuMul .85 · fillCenter=0）
            MenuDraw.Nine(_openCrateBtn, Tex(ArtOctagon, "开箱钮高亮框"), PriceBtnHiR,
                          new Vector4(52f, 52f, 52f, 52f), 128f, 128f, QChestButton, Color.white, false,
                          "Button Higlight border",
                          new Vector4(52f / 0.85f, 52f / 0.85f, 52f / 0.85f, 52f / 0.85f));

            // `Open Chest Button`（`40K_button` Simple + preserveAspect · tint 粉）
            var btn = MenuDraw.Node(_openCrateBtn, "Open Chest Button", OpenBtnR);
            MenuDraw.Rect(btn, Tex(ArtBtn, "开箱钮底"), OpenBtnR, "Image", QChestButton1, OpenBtnTint, true);

            // `Button Text` 出厂 INACT ⇒ 建出来、关着（不印）
            var bt = MenuDraw.Node(btn, "Button Text", OpenBtnTextR);
            var btLb = MenuDraw.TextBox(bt, OpenBtnTextR, "", Color.white, "Button Text", 12f, 12f, QChestText,
                                        38f, 12f, face: TmpFont.Face.LatinCardBody);
            if (btLb != null) MenuDraw.SetVAlign(btLb, Label.VAlign.Midline, OpenBtnTextR);
            bt.gameObject.SetActive(false);

            // `Price Display`（HLG：icon + text；价格 = liveops 数据 ⇒ `--` 占位）
            var pd = MenuDraw.Node(btn, "Price Display", PriceDispR);
            MenuDraw.Rect(pd, Tex(ArtTicket, "票价图标"), TicketIconR, "icon", QChestText, Color.white);
            var pt = MenuDraw.TextBox(pd, TicketTextR, NoData, Color.white, "text", 62.77f, 13.46f, QChestText,
                                      62.77f, 39f, face: TmpFont.Face.LatinCardBody);
            if (pt != null)
            {
                pt.SetWrapping(false);
                MenuDraw.SetVAlign(pt, Label.VAlign.Capline, TicketTextR);
            }

            // 开箱 = `ContainerOfferEvent.TryPurchaseOffer`（服务器）+ 视频 ⇒ 本地只出声
            MenuDraw.Hit(_openCrateBtn, "Hit", PriceBtnR, QHit, () =>
                Debug.LogWarning("[Gacha] **开箱**需要服务器：原版 `GachaCratePanel.OpenCrateButtonClick` → "
                               + "`GachaEvent.CurrentOffer.TryPurchaseOffer(...)`（LiveOps）+ 播 "
                               + "`Gacha Crate Opening.mp4`；本地没有 liveops 后端与视频 ⇒ **不假装开箱**。"),
                null, null, null, null, null, clickCue: WindowSounds.DefaultButtonCue);
        }

        void BuildHeader(Transform chest)
        {
            _header = MenuDraw.Node(chest, "Header", HeaderR);

            // `Background`（出厂 INACT · 纯黑）
            var hb = MenuDraw.Node(_header, "Background", HeaderBgR);
            MenuDraw.Rect(hb, CardArt.Solid(), HeaderBgR, "Image", QChestText, HeaderBgTint);
            hb.gameObject.SetActive(false);

            // `Title`（The Vault / 金库 · fs90 / base36 / auto[40,90] / Center-Midline / wrap）
            var titleNode = MenuDraw.Node(_header, "Title", TitleR);
            _title = MenuDraw.TextBox(titleNode, TitleR, Loc.T(TermTitle), Color.white, "Title", 90f, 40f, QChestText,
                                      90f, 36f, face: TmpFont.Face.LatinUi);
            if (_title != null) MenuDraw.SetVAlign(_title, Label.VAlign.Midline, TitleR);

            // `Help Icon Player`（**挂在 `Title` 下**，与 dump 兄弟序一致；出厂 INACT）
            var help = MenuDraw.Node(titleNode, "Help Icon Player", HelpIconR);
            MenuDraw.Rect(help, Tex(ArtInfo, "活动说明图标"), HelpIconR, "Image", QChestText, Color.white);
            help.gameObject.SetActive(false);

            // `Army Icon`（出厂 INACT · `m_Sprite = null`）
            var army = MenuDraw.Node(_header, "Army Icon", ArmyIconR);
            army.gameObject.SetActive(false);

            // `Time Remaining`（Ends in: · pivot 1.0 ⇒ 右对齐）+ 时钟 + 时间（TimerDisplay，目标时刻本地没有 ⇒ --）
            var tr = MenuDraw.Node(_header, "Time Remaining", TimeRemainR);
            var trLb = MenuDraw.TextBox(tr, TimeRemainR, Loc.T(TermEndsIn), Color.white, "Time Remaining",
                                        40f, 18f, QChestText, 72f, 40f, face: TmpFont.Face.LatinCardBody);
            if (trLb != null)
            {
                MenuDraw.AlignRight(trLb, TimeRemainR);
                MenuDraw.SetVAlign(trLb, Label.VAlign.Midline, TimeRemainR);
            }
            MenuDraw.Rect(tr, Tex(ArtClock, "倒计时时钟"), TimeClockR, "Time image", QChestText, Color.white);
            _timeLabel = MenuDraw.TextBox(tr, TimeTextR, NoData, Color.white, "Time", 40f, 18f, QChestText,
                                          72f, 40f, face: TmpFont.Face.LatinCardBody);
            if (_timeLabel != null)
            {
                _timeLabel.SetWrapping(false);
                MenuDraw.AlignLeft(_timeLabel, TimeTextR);
                MenuDraw.SetVAlign(_timeLabel, Label.VAlign.Midline, TimeTextR);
                TimerDisplay.Attach(_timeLabel, null, "", "Gacha/Header/Timer",
                                    "Gacha 活动截止时刻在远端 LiveOps（`LiveOpsEvent` 事件数据），本地没有 ⇒ `--`");
            }
        }

        // ------------------------------------------------------------ 右栏 `Rewards Panel`
        void BuildRewardsPanel(Transform page)
        {
            _rewardsPanel = MenuDraw.Node(page, "Rewards Panel", RewardsPanelR);

            // `Background Border`（`40K_dropdown_bg` Sliced 23,20,23,20 · ppuMul 2.18）
            MenuDraw.Nine(_rewardsPanel, Tex(ArtDropdownBg, "奖励栏边框"), RewardsPanelR,
                          new Vector4(23f, 20f, 23f, 20f), 119f, 102f, QRewBg, DropdownTint, true, "Background Border",
                          new Vector4(23f / 2.18f, 20f / 2.18f, 23f / 2.18f, 20f / 2.18f));
            // `Background fill`（`40k_popup_texture` Tiled · ppuMul 2 ⇒ 贴片 128×2 = 256px）
            MenuDraw.Tiled(_rewardsPanel, Tex(ArtPopupTex, "奖励栏纹理"), RewardsPanelR, 256f, QRewFill,
                           "Background fill", Color.white);
            // `BackgroundTop`（出厂 INACT · `UICornersGradient` + 无 sprite）
            var top = MenuDraw.Node(_rewardsPanel, "BackgroundTop", RewardsTopR);
            top.gameObject.SetActive(false);

            BuildRewardsHeader(_rewardsPanel);
            BuildRewardsScroll(_rewardsPanel);
            BuildRewardsFooter(_rewardsPanel);
            BuildRewardsCompleted(_rewardsPanel);

            // 末尾那颗空 `Rewards` 容器（原版树里有、无组件）
            MenuDraw.Node(_rewardsPanel, "Rewards", RewardsPanelR);
        }

        void BuildRewardsHeader(Transform panel)
        {
            var header = MenuDraw.Node(panel, "Header", RewHeaderR);
            var t = MenuDraw.Node(header, "Header Title", RewTitleR);
            _rewardTitle = MenuDraw.TextBox(t, RewTitleR, Loc.T(TermSpecialItems), Color.white, "Header Title",
                                            90f, 40f, QRewText, 90f, 36f, face: TmpFont.Face.LatinUi);
            if (_rewardTitle != null) MenuDraw.SetVAlign(_rewardTitle, Label.VAlign.Midline, RewTitleR);

            var rt = MenuDraw.Node(header, "Reward Timer", RewTimerR);
            var rtLb = MenuDraw.TextBox(rt, RewTimerR, Loc.T(TermEndsIn), Color.white, "Reward Timer",
                                        40f, 18f, QRewText, 72f, 40f, face: TmpFont.Face.LatinCardBody);
            if (rtLb != null)
            {
                // pivot 1.0 + `m_HorizontalAlignment = Left` ⇒ 文本块从右枢轴向左长，视觉上右缘贴 1316.6
                MenuDraw.AlignRight(rtLb, RewTimerR);
                MenuDraw.SetVAlign(rtLb, Label.VAlign.Midline, RewTimerR);
            }
            MenuDraw.Rect(rt, Tex(ArtClock, "奖励栏倒计时时钟"), RewClockR, "Time image", QRewText, Color.white);
            _rewardTime = MenuDraw.TextBox(rt, RewTimeR, NoData, Color.white, "Time", 40f, 18f, QRewText,
                                           72f, 40f, face: TmpFont.Face.LatinCardBody);
            if (_rewardTime != null)
            {
                _rewardTime.SetWrapping(false);
                MenuDraw.AlignLeft(_rewardTime, RewTimeR);
                MenuDraw.SetVAlign(_rewardTime, Label.VAlign.Midline, RewTimeR);
                TimerDisplay.Attach(_rewardTime, null, "", "Gacha/Rewards/Timer",
                                    "Gacha 活动截止时刻在远端 LiveOps，本地没有 ⇒ `--`");
            }
        }

        void BuildRewardsScroll(Transform panel)
        {
            _rewardsScroll = MenuDraw.Node(panel, "Rewards", RewScrollR);
            // ScrollRect 自制件：本壳没有滚动运行时，数据也没有 ⇒ 只建结构（框 + 视口裁切 + 内容）
            MenuDraw.Rect(_rewardsScroll, CardArt.Solid(), RewScrollR, "Background", QRewArt, ScrollBgTint);
            var vp = ViewportClip.Hang(_rewardsScroll, "Viewport", RewScrollR, Vector4.zero, Vector2Int.zero).transform;
            // `Viewport` 的 `UIMask` 图 **alpha = 0**（不可见）⇒ 不画；裁切已由 `ViewportClip` 承担（如实记录）。
            _rewardContent = MenuDraw.Node(vp, "Content", RewContentR);

            // `Gacha Drawer Holder` = `GachaRewardsPanel.containerPrefab` 的模板（运行时逐奖励 Instantiate）。
            // ⚠️ 没有掉落表 ⇒ **建出来、关着**（⛔ 不拿 prefab 里那颗示例当真实奖励）。
            _drawer = MenuDraw.Node(_rewardContent, "Gacha Drawer Holder", DrawerR);
            var claimed = MenuDraw.Node(_drawer, "Gacha Reward Claimed (1)", ClaimedR);
            MenuDraw.Nine(claimed, Tex(ArtClaim, "奖励领取条"), ClaimedR, new Vector4(162f, 0f, 162f, 0f),
                          324f, 87f, QRewArt, ClaimTint, true, "Image");
            _claimedText = MenuDraw.TextBox(claimed, ClaimedTextR, Loc.T(TermClaimed), Color.white, "Claimed Tex",
                                            40.55f, 18f, QRewText, 200f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_claimedText != null) MenuDraw.SetVAlign(_claimedText, Label.VAlign.Midline, ClaimedTextR);
            _drawer.gameObject.SetActive(false); ApplyRewardsSpacing();
        }

        void BuildRewardsFooter(Transform panel)
        {
            var footer = MenuDraw.Node(panel, "Footer", FooterR);
            _rewardCount = MenuDraw.TextBox(footer, RewardCountR, NoData + "/" + NoData, Color.white, "Reward count",
                                            56f, 18f, QRewText, 56f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_rewardCount != null) MenuDraw.SetVAlign(_rewardCount, Label.VAlign.Midline, RewardCountR);

            // `Player doesn't have all items`（出厂 ACTIVE；`PlayerHasAllSpecialItems` 为假时的那一支）
            var noAll = MenuDraw.Node(footer, "Player doesn't have all items", NoAllItemsR);
            _guarantee = MenuDraw.TextBox(noAll, GuaranteeTextR, Loc.T(TermGuarantee), Color.white, "Footer Text",
                                          38f, 18f, QRewText, 56f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_guarantee != null) MenuDraw.SetVAlign(_guarantee, Label.VAlign.Midline, GuaranteeTextR);

            // `ProgressBar`（Slider 0..max · 出厂 max 6 / value 4.15）—— 数据没有 ⇒ 填充 0、计数 `--/--`
            _progress = MenuDraw.Node(noAll, "ProgressBar", ProgressR);
            var barBg = MenuDraw.Node(_progress, "Background", ProgressBgR);
            MenuDraw.Nine(barBg, Tex(ArtBarBg, "进度条底"), ProgressBgR, new Vector4(20f, 0f, 20f, 0f),
                          42f, 18f, QRewBar, BarBgTint, true, "Image");
            var fillArea = MenuDraw.Node(barBg, "Fill Area", FillAreaR);
            _progressFill = MenuDraw.Node(fillArea, "Fill", FillR);              // 出厂 0 宽 = 进度 0
            MenuDraw.Nine(_progressFill, Tex(ArtBarFill, "进度条填充"), FillR, new Vector4(10f, 0f, 10f, 0f),
                          42f, 18f, QRewBar, BarFillTint, true, "Image");
            MenuDraw.Rect(_progressFill, Tex(ArtBarEnd, "进度条末端"), FillEndR, "end", QRewBar, BarEndTint, true);
            MenuDraw.Nine(_progress, Tex(ArtBarOutline, "进度条描边"), ProgressiveOutlineR,
                          new Vector4(20f, 0f, 20f, 0f), 46f, 22f, QRewBar, BarOutlineTint, true, "Outline");
            _counter = MenuDraw.TextBox(_progress, CounterR, NoData + "/" + NoData, Color.white, "counter",
                                        49.8f, 12f, QRewText, 60f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_counter != null) MenuDraw.SetVAlign(_counter, Label.VAlign.Middle, CounterR);

            // `Player has all items`（出厂 INACT；`PlayerHasAllSpecialItems` 为真时的那一支）
            var all = MenuDraw.Node(footer, "Player has all items", AllItemsR);
            _allItems = MenuDraw.TextBox(all, AllItemsTextR, Loc.T(TermAllRewards), Color.white, "Footer Text",
                                         56f, 18f, QRewText, 56f, 36f, face: TmpFont.Face.LatinCardBody);
            if (_allItems != null) MenuDraw.SetVAlign(_allItems, Label.VAlign.Middle, AllItemsTextR);
            all.gameObject.SetActive(false); ReinitializeRewardsPanel();   // 原版 `GachaRewardsPanel.Initialize` 的末两拍（同一行 = 不加行号）
        }

        void BuildRewardsCompleted(Transform panel)
        {
            _completed = MenuDraw.Node(panel, "Completed", CompletedR);
            MenuDraw.Rect(_completed, CardArt.Solid(), CompletedR, "Background", QRewArt, CompletedShadeTint);
            var msg = MenuDraw.Node(_completed, "Message container", CompletedMsgR);
            MenuDraw.Rect(msg, Tex(ArtBanner, "完成横幅"), CompletedBannerR, "Background", QRewArt, BannerTint);
            _completedTitle = MenuDraw.TextBox(msg, CompletedTitleR, Loc.T(TermCompleted), Color.white,
                                               "Complete Text Title", 131.9f, 18f, QRewText, 131.9f, 36f,
                                               face: TmpFont.Face.LatinUi);
            if (_completedTitle != null) MenuDraw.SetVAlign(_completedTitle, Label.VAlign.Middle, CompletedTitleR);
            _completedInfo = MenuDraw.TextBox(msg, CompletedInfoR, Loc.T(TermCompletedTip), Color.white,
                                              "Complete Tex Info", 53.2f, 18f, QRewText, 53.2f, 36f,
                                              face: TmpFont.Face.LatinCardBody);
            if (_completedInfo != null) MenuDraw.SetVAlign(_completedInfo, Label.VAlign.Middle, CompletedInfoR);
            _completed.gameObject.SetActive(false);   // 出厂 INACT；无数据时不假装「已收集完」
        }

        // ============================================================ 坐标 / 取图助手
        /// <summary>以 `r` 的中心为心、换成 `w×h`（dump 的「视觉框」列用这个落位）。</summary>
        static PxRect Center(PxRect r, float w, float h)
        {
            float cx = (r.x1 + r.x2) * 0.5f, cy = (r.y1 + r.y2) * 0.5f;
            return new PxRect(cx - w * 0.5f, cy - h * 0.5f, cx + w * 0.5f, cy + h * 0.5f);
        }

        /// <summary>箱子图：原版 `40k_Crate_Tier5_Warp_premium`（本地只在解包目录里）⇒ 回退 `..._Warp`，**出声**。</summary>
        Texture2D TexCrate()
        {
            var t = CardArt.MenuUi("40k_Crate_Tier5_Warp_premium");
            if (t != null) return t;
            Note("箱子图：原版 `40k_Crate_Tier5_Warp_premium` 未导入 `Resources/`"
                + "（源 = `bundle_boosterpacks_assets_all/Texture2D/40k_Crate_Tier5_Warp_premium.png`）"
                + " ⇒ 回退 `40k_Crate_Tier5_Warp`");
            return Tex(ArtCrate, "箱子（回退 Tier5）");
        }

        void Note(string s)
        {
            if (_artNotes.Contains(s)) return;
            _artNotes.Add(s);
            Debug.LogWarning("[Gacha] " + s);
        }

        /// <summary>取图 —— **取不到必须出声**（红线；`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。</summary>
        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[Gacha] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`）。");
            }
            return t;
        }

        // ============================================================ 🆕 2026-10-28（B89-R3b · 账 29）奖励行行距
        // 原版 = `AutoLayoutSpacingByItemSize`：挂在我们这棵树的对应节点 `Rewards/Viewport/Content` 上
        //   （prefab 直读 `GenericPromptWindow` 之外的另一份：`d:/2/tools/assetripper_out/ExportedProject/Assets/Gacha Tab.prefab`
        //    的节点链 `Gacha Tab ▸ Rewards Panel ▸ Rewards ▸ Viewport ▸ Content`，同 GO 还挂着
        //    HorizontalLayoutGroup(fileID `-405508275`) + ContentSizeFitter(H fit, fileID `1741964061`)）。
        // 🔴 判据 = 序列化字段 + 方法体，两样都现读：
        //   · `spacingAt16_9 = -40`（`m_Curve` 在 `1.777` 那一端）· `spacingAt4_3 = 84.76`（`1.33` 那一端）
        //   · `spacingByAspectRatioCurve` 两个键：`(1.33, 0, in 1.983927, out 1.983927, inW 0, outW 0.21344574)` ·
        //     `(1.777, 1, in 2.1977012, out 2.1977012, inW 0.17689756, outW 0.33333334)`
        //   · 方法体 `decomp_full/AutoLayoutSpacingByItemSize__OnEnable.c` 全 24 行：
        //     `t = Clamp01(curve.Evaluate(Screen.width / (float)Screen.height))` ·
        //     `spacing = (max − min) × t + min`（`+0x28` = max = **−40**、`+0x2c` = min = **84.76**）
        // ⚠️ 负值不是笔误：16:9 下奖励格**故意重叠 40px**，⛔ 别 `Mathf.Max(0, …)`。
        /// <summary>16:9 那一端（`spacingAt16_9`）。</summary>
        public const float SpaceAt16_9 = -40f;
        /// <summary>4:3 那一端（`spacingAt4_3`）。</summary>
        public const float SpaceAt4_3 = 84.76f;

        /// <summary>本帧算出来的奖励行行距（设计 px）= 原版 `layoutGroup.spacing` 的等价物。**自检读口**：
        /// 16:9 ⇒ **−40**、4:3 ⇒ **84.76**、两端之外被曲线夹住（ClampForever）。</summary>
        public float RewardsSpacingPx { get; private set; }

        static AnimationCurve _aspectCurve;
        static bool _spacingNoted;

        /// <summary>原版那条 `spacingByAspectRatioCurve`（键/切线/权重逐字段照抄，判据见本节头）。</summary>
        public static AnimationCurve AspectCurve
        {
            get
            {
                if (_aspectCurve == null)
                {
                    _aspectCurve = new AnimationCurve(
                        new Keyframe(1.33f, 0f, 1.983927f, 1.983927f, 0f, 0.21344574f),
                        new Keyframe(1.777f, 1f, 2.1977012f, 2.1977012f, 0.17689756f, 0.33333334f));
                    _aspectCurve.preWrapMode = WrapMode.ClampForever;
                    _aspectCurve.postWrapMode = WrapMode.ClampForever;
                }
                return _aspectCurve;
            }
        }

        /// <summary>= 原版 `OnEnable` 那三句（判据见本节头）。`aspect` = **物理**宽高比 `Screen.width / Screen.height`。</summary>
        public static float SpacingForAspect(float aspect)
        {
            float t = Mathf.Clamp01(AspectCurve.Evaluate(aspect));
            return (SpaceAt16_9 - SpaceAt4_3) * t + SpaceAt4_3;
        }

        /// <summary>算一次并存进 <see cref="RewardsSpacingPx"/>（`BuildRewardsScroll` 末尾调）。
        /// <para>⚠️ **今天没有可观测的画面差异**：奖励格要 `GachaRewardsPanel.RefreshContainers` 按**掉落表**逐条
        /// Instantiate，而掉落表在服务端（本地 0 载荷）⇒ `Content` 下只有那颗关着的模板。
        /// 观测面 = ① 本读口 ② 建树时那条日志（`Note`，每扇窗只打一次）。</para></summary>
        void ApplyRewardsSpacing()
        {
            float aspect = Screen.height > 0 ? Screen.width / (float)Screen.height : 16f / 9f;
            RewardsSpacingPx = SpacingForAspect(aspect);
            if (_spacingNoted) return;
            _spacingNoted = true;
            Note("奖励行行距 = " + RewardsSpacingPx.ToString("0.##") + " px（宽高比 " + aspect.ToString("0.###")
                 + "；原版 `AutoLayoutSpacingByItemSize`：16:9 ⇒ " + SpaceAt16_9 + " · 4:3 ⇒ " + SpaceAt4_3
                 + "。奖励格还没建（掉落表在服务端）⇒ 这个值目前只进读数与日志）");
        }

        // ============================================================ 🆕 2026-10-28（B92_N3）
        // **原版 `GachaRewardsPanel` / `GachaRewardContainer` 的运行时那半边**（本节 = 机制；
        // 数据一律经 `RewardsProvider` 注入，`null` = 今天的真实情况）。
        //
        // 判据（`d:/2/tools/decomp_full/` 四个方法体**现读** · 2026-10-28）：
        //  · `GachaRewardsPanel__Initialize.c`：`gachaEvent == null` ⇒ 崩；`CurrentContainer == null` ⇒
        //    `Debug.LogError` + return；否则 `TimerDisplay.Initialize(+0x28, CurrentOffer.<+0x4c8>(), …)` →
        //    `RefreshContainers()` → `RefreshFooter()`（后两拍在 `.c` 里被内联展开）。
        //  · `GachaRewardsPanel__RefreshContainers.c`：`Σ dropTable[i].maxAmount`（分母）/ `Σ counters[id]`
        //    （只数掉落表里出现的 id，分子）→ `SupportMethods.DestroyAllChildren(+0x30)` →
        //    逐条 `Instantiate(+0x38)` + `SetActive(true)` + `GachaRewardContainer.Initialize(item, 分子_i, 分母_i)` →
        //    顶行计数 = `string.Format(fmt, 分子, 分母)` → `+0x40.SetActive(分子 == 分母)`。
        //  · `GachaRewardsPanel__RefreshFooter.c`：`!PlayerHasAllSpecialItems` ⇒ `+0x50.SetActive(true)` +
        //    `ProgressBar.SetProgress(Math.Max(0, CratesSinceLastLegendary), container.maxCrates, fmt, 1)`；
        //    否则 `+0x50.SetActive(false)`（**只碰 +0x50，不碰 +0x40** ⇒ 我们也不在这一支碰它）。
        //  · `GachaRewardContainer__Initialize.c`：先 `Destroy` 旧抽屉(+0x58) → `ItemDrawer.Draw(+0x28, reward, 1, 0, 0)` →
        //    `current < max` ⇒ 文本 `"{0}/{1}"`，否则 `Loc("Rewards Menu/Claimed")` →
        //    `+0x20.SetActive(current > 0)` + `transform.SetAsLastSibling()`。
        //  · `GachaRewardContainer__Reset.c` = **只有一句** `ItemDrawer.Release(+0x58)`
        //    （`claimedColor` / `inprogressColor` 两个序列化色在这四个方法里**一次都没被读** ⇒ ⛔ 别按名字编用法）。
        //  · `GachaRewardContainer__OnClick(Action)` = 把回调转发给抽屉（虚槽 +0x218）。
        //  · `GachaRewardContainer__Awake.c`（`.c` 首行是 ICF 折叠来的他方法名，**体只碰 `this.gameObject`**）
        //    = `gameObject.SetActive(false)` ⇒ 模板出厂关着（我们 `BuildRewardsScroll` 末尾那一句就是它）。
        //
        // 🔴 **今天一格都建不出来**：`SpecialDropTable` / `droptableCounters` / `CrateInfo` 全在远端 LiveOps
        //    ⇒ 走原版那条「没数据 ⇒ 直接返回」的分支，footer 保持 prefab 出厂态（「Player doesn't have all items」
        //    亮着、进度 0）。⛔ 不编掉落表、不编进度。
        // ⚠️ **不在本节的两件事**（如实记，别以为漏了）：① 截止时刻（`Initialize` 第一拍）—— 我们的
        //    `TimerDisplay.Attach` 在 `BuildHeader` / `BuildRewardsHeader` 建树时就挂了，杆子等价；
        //    ② 格子**排布**（`Content` 上的 `HorizontalLayoutGroup` + `AutoLayoutSpacingByItemSize`）——
        //    那是**另一个组件**的职责（账在 `AutoLayoutSpacingByItemSize` 那一行），本节只照原版做
        //    「Instantiate + Initialize」；框由调用方给（同 `ItemDrawer.Draw` 的约定），缺省 = 模板框。

        Transform _rewardContent;
        Transform _progressFill;
        readonly List<ItemDrawerView> _rewardContainers = new List<ItemDrawerView>();
        readonly List<Transform> _rewardContainerRoots = new List<Transform>();
        static bool _rewardsDataNoted;

        /// <summary>奖励格的一项 —— 原版 `CustomDropTableDefinition.DropTableItem`（`reward.itemId` +
        /// `maxAmount`）＋「当前持有」（原版 `droptableCounters[container.DropTableCounterKey][id]`）。</summary>
        public sealed class SpecialItem
        {
            /// <summary>物品（原版 `ObtainableItem`；我们用 `ItemDrawer.Spec(id, art, label)` 组）。</summary>
            public ItemSpec Item;
            /// <summary>计数器当前值。</summary>
            public int Current;
            /// <summary>这一项的总数（原版 `DropTableItem.maxAmount`）。</summary>
            public int Max;
        }

        /// <summary>一局活动数据的三格（原版 `GachaEvent.CurrentContainer` 里那三样）。</summary>
        public struct RewardsData
        {
            /// <summary>`DropTableContainer.SpecialDropTable.items`（**空表 = 全部集齐**，见 `PlayerHasAllSpecialItems`）。</summary>
            public List<SpecialItem> SpecialItems;
            /// <summary>`CrateInfo.CratesSinceLastLegendary`（进度条分子）。</summary>
            public int CratesSinceLastLegendary;
            /// <summary>`DropTableContainer.maxCrates`（`+0x74`，进度条分母）。</summary>
            public int MaxCrates;
        }

        /// <summary>数据注入口（远端 LiveOps 的替身）。`null` = 没有活动数据（今天）。
        /// ⚠️ 这是**数据来源**的替身，不是「我们编了一套掉落规则」—— 规则（怎么算集齐/怎么印字）逐句照原版。</summary>
        public static System.Func<RewardsData?> RewardsProvider { get; set; }

        /// <summary>建出来的奖励格（自检读口；顺序 = 掉落表顺序）。</summary>
        public List<ItemDrawerView> RewardContainers { get { return _rewardContainers; } }
        /// <summary>最后一次刷新时「没有数据源」= 一格都没建。</summary>
        public bool RewardsDataMissing { get; private set; }
        /// <summary>最后一次 `SetRewardProgress` 的两个实参（自检读口）。</summary>
        public int ProgressValue { get; private set; }
        public int ProgressMax { get; private set; }

        static RewardsData? Snapshot()
        {
            var p = RewardsProvider;
            return p != null ? p() : null;
        }

        /// <summary>= 原版 `GachaEvent.PlayerHasAllSpecialItems()`（`GachaEvent.cs:245` 现读）：
        /// `Enumerable.All(SpecialDropTable.items, x =&gt; counters[x.reward.itemId] &gt;= x.maxAmount)`
        /// ⇒ **空表 = true**（`All` 的真空真），照抄。
        /// <para>没有数据源 ⇒ `false`（= 原版那条链根本进不来时的现状：footer 保持「doesn't have all」出厂态）。</para></summary>
        public bool PlayerHasAllSpecialItems()
        {
            RewardsData? d = Snapshot();
            if (d == null || d.Value.SpecialItems == null) return false;
            var items = d.Value.SpecialItems;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null || it.Current < it.Max) return false;
            }
            return true;
        }

        Transform NoAllItemsNode() { return _guarantee != null ? _guarantee.transform.parent : null; }
        Transform AllItemsNode() { return _allItems != null ? _allItems.transform.parent : null; }

        /// <summary>= 原版 `GachaRewardsPanel.Initialize(gachaEvent)` 的后两拍（`Shell` 建完三块后调，见
        /// `BuildRewardsFooter` 末句）。没有数据 ⇒ 照原版的空数据分支**什么都不建**（只出声一次）。</summary>
        public void ReinitializeRewardsPanel()
        {
            RewardsData? d = Snapshot();
            if (d == null)
            {
                RewardsDataMissing = true;
                if (!_rewardsDataNoted)
                {
                    _rewardsDataNoted = true;
                    Debug.LogWarning("[Gacha] 奖励面板：`CurrentContainer` / `SpecialDropTable` 本地没有（远端 LiveOps）"
                                   + " ⇒ 照原版 `GachaRewardsPanel.Initialize` 的空数据分支**一格都不建**，"
                                   + "footer 保持 prefab 出厂态。⛔ 不编掉落表与进度。");
                }
                return;
            }
            RewardsDataMissing = false;
            RefreshRewardContainers();
            RefreshRewardFooter();
        }

        /// <summary>= 原版 `GachaRewardsPanel.RefreshContainers()`（逐句见本节头）。
        /// <para><paramref name="boxOf"/> = 每一格的框（序号 ⇒ `PxRect`）。原版的格子尺寸在
        /// `containerPrefab` 里、位置由 `Content` 上的 LayoutGroup 排；我们这棵树没有那个 LayoutGroup
        /// ⇒ 框由**调用方**给（同 `ItemDrawer.Draw` 的约定）。缺省 = 模板框 `DrawerR`（多格会重叠，如实记）。</para></summary>
        /// <returns>建出来的格数。</returns>
        public int RefreshRewardContainers(System.Func<int, PxRect> boxOf = null)
        {
            RewardsData? d = Snapshot();
            if (d == null || d.Value.SpecialItems == null) { RewardsDataMissing = true; return 0; }
            RewardsDataMissing = false;
            var items = d.Value.SpecialItems;
            ClearRewardContainers();
            int totalMax = 0, owned = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                totalMax += it.Max;
                int c = it.Current;
                owned += c < 0 ? 0 : (c > it.Max ? it.Max : c);
            }
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null) continue;
                BuildRewardContainer(items[i], boxOf != null ? boxOf(i) : DrawerR);
            }
            if (_rewardCount != null) _rewardCount.SetText(owned + "/" + totalMax);
            var allNode = AllItemsNode();
            if (allNode != null) allNode.gameObject.SetActive(owned == totalMax);
            return _rewardContainers.Count;
        }

        /// <summary>= 原版 `GachaRewardsPanel.RefreshFooter()`（**只碰「doesn't have all items」那一个节点** +
        /// 进度条；`+0x40` 是 `RefreshContainers` 的事，⛔ 别写两处）。</summary>
        public void RefreshRewardFooter()
        {
            bool hasAll = PlayerHasAllSpecialItems();
            var noAll = NoAllItemsNode();
            if (noAll != null) noAll.gameObject.SetActive(!hasAll);
            RewardsData? d = Snapshot();
            if (d == null) return;                       // 没有数据 ⇒ 进度保持出厂 0（⛔ 不编）
            SetRewardProgress(d.Value.CratesSinceLastLegendary, d.Value.MaxCrates);
        }

        /// <summary>= 原版 `ProgressBar.SetProgress(value, max, fmt, 1)` 那一拍（`Math.Max(0, value)` 照抄）。
        /// <para>填充 = `Fill Area` 的宽度 × `value/max`；端帽（`Fill` 的子件 `end`）中心跟填充右端走，
        /// 偏移量照 prefab 出厂关系（`FillEndR` 中心 1048.35 比 `Fill Area` 左缘 1060.4 左 **12.05**，
        /// 端帽自身 39.9×54.3）—— 都是序列化值之间的关系，不是我们挑的数。</para></summary>
        public void SetRewardProgress(int value, int max)
        {
            ProgressValue = value < 0 ? 0 : value;
            ProgressMax = max < 0 ? 0 : max;
            if (_counter != null) _counter.SetText(ProgressValue + "/" + ProgressMax);
            if (_progressFill == null) return;
            float t = ProgressMax <= 0 ? 0f : Mathf.Clamp01(ProgressValue / (float)ProgressMax);
            var fr = new PxRect(FillAreaR.x1, FillAreaR.y1, FillAreaR.x1 + FillAreaR.W * t, FillAreaR.y2);
            MenuDraw.ApplyPxRect(_progressFill, _progressFill.parent, fr);
            var end = _progressFill.Find("end");
            if (end != null)
            {
                float cx = fr.x2 - 12.05f;
                MenuDraw.ApplyPxRect(end, _progressFill,
                                     new PxRect(cx - 19.95f, FillEndR.y1, cx + 19.95f, FillEndR.y2));
            }
        }

        /// <summary>= 原版 `GachaRewardContainer.Initialize(reward, current, max)`（逐句见本节头）。
        /// <paramref name="onClick"/> = 原版 `OnClick(action)` 那一跳（转发给抽屉；`null` = 不挂）。</summary>
        public ItemDrawerView BuildRewardContainer(SpecialItem it, PxRect box, System.Action onClick = null)
        {
            if (_rewardContent == null || it == null) return null;
            var node = MenuDraw.Node(_rewardContent, "Gacha Reward Container", box);
            _rewardContainerRoots.Add(node);
            var holder = MenuDraw.Node(node, "Drawer Holder", box);              // 原版 `drawerHolder`(+0x28)
            var view = ItemDrawer.DrawView(holder, box, it.Item, 1, DrawerOverride.Default,
                                           ItemDrawerStyle.Default(QRewArt, QRewArt1, QRewText));
            if (view != null)
            {
                // 原版 `DrawItemReward` 第三步 `ToggleShowInfoOnClick(false)`（我们那一档是空实现，
                // 理由 → `ItemDrawerView.ToggleShowInfoOnClick`：原版的 `showItemInfoOnClick` 是 prefab 上的静态值）
                view.ToggleShowInfoOnClick(false);
                if (onClick != null) view.OverrideOnClickAction(onClick);
                _rewardContainers.Add(view);
            }
            // `itsClaimedObject`(+0x20)：`current > 0` 才显示，并 `SetAsLastSibling()` 压到最上
            var claimed = MenuDraw.Node(node, "Gacha Reward Claimed (1)", ClaimedR);
            MenuDraw.Nine(claimed, Tex(ArtClaim, "奖励领取条"), ClaimedR, new Vector4(162f, 0f, 162f, 0f),
                          324f, 87f, QRewArt, ClaimTint, true, "Image");
            var txt = MenuDraw.TextBox(claimed, ClaimedTextR,
                                       it.Current < it.Max ? it.Current + "/" + it.Max : Loc.T(TermClaimed),
                                       Color.white, "Claimed Tex", 40.55f, 18f, QRewText, 200f, 36f,
                                       face: TmpFont.Face.LatinCardBody);
            if (txt != null) MenuDraw.SetVAlign(txt, Label.VAlign.Midline, ClaimedTextR);
            claimed.gameObject.SetActive(it.Current > 0);
            claimed.SetAsLastSibling();
            return view;
        }

        /// <summary>= 原版 `GachaRewardContainer.Reset()`（**只有一句** `ItemDrawer.Release(itemDrawer)`）。
        /// 我们的抽屉不池化（→ `ItemDrawer` 追加区头注 ③）⇒ 等价做法 = 销毁整格；节点销毁时
        /// `ItemDrawerLeash.OnDestroy` 会自动把句柄从 `ItemDrawer._handles` 放掉（= 原版 `Release` 那一半）。</summary>
        public void ResetRewardContainer(ItemDrawerView v)
        {
            if (v == null) return;
            _rewardContainers.Remove(v);
            Transform root = null;
            if (v.Node != null)
                root = v.Node.parent != null && v.Node.parent.parent != null ? v.Node.parent.parent : v.Node;
            if (root != null) _rewardContainerRoots.Remove(root);
            Transform kill = root != null ? root : (v.Node != null ? v.Node : null);
            if (kill == null) return;
#if UNITY_EDITOR
            if (!Application.isPlaying) { Object.DestroyImmediate(kill.gameObject); return; }
#endif
            Object.Destroy(kill.gameObject);
        }

        /// <summary>= 原版 `RefreshContainers` 的 `SupportMethods.DestroyAllChildren(containerHolder)` 那一拍。
        /// <para>⚠️ 我们这棵树里 `Gacha Drawer Holder`（模板）**是** `Content` 的子件（原版它是独立 prefab）
        /// ⇒ 清的时候要保它（它出厂关着，等于不存在）。</para></summary>
        public void ClearRewardContainers()
        {
            for (int i = _rewardContainers.Count - 1; i >= 0; i--) ResetRewardContainer(_rewardContainers[i]);
            _rewardContainers.Clear();
            if (_rewardContent == null) { _rewardContainerRoots.Clear(); return; }
            for (int i = _rewardContent.childCount - 1; i >= 0; i--)
            {
                var ch = _rewardContent.GetChild(i);
                if (ch == _drawer) continue;
                _rewardContainerRoots.Remove(ch);
#if UNITY_EDITOR
                if (!Application.isPlaying) { Object.DestroyImmediate(ch.gameObject); continue; }
#endif
                Object.Destroy(ch.gameObject);
            }
            _rewardContainerRoots.Clear();
        }
    }
}
