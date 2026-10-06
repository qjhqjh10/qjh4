// PurchasePremiumWindow.cs — **购买高级战役**（原版 `PurchasePremiumWindow : GameWindow`）
//
// ============================ 出处（唯一正本）============================
// prefab：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/` 根 GO `Purchase Premium Window`
//   · 窗口参数 MB = `MonoBehaviour_6814103817760397369`（**34 个节点**，逐节点现读）；
//   · 逐节点几何 = **`menu_dump.py` 现读**（本文件里那套是 **`--relative`** 那一档，见下）：
//     `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Purchase Premium Window" --depth 12 --relative --md`
//   · 字段 → 节点（pid 反查，5/5 + 4/4 逐个对上）：
//     `PurchasePremiumWindow`：`armyInfo → Army Info` · `armyInfoContainerReference → Army Container`（**模板**）·
//       `container → Content` · `scrollRect → Scroll View` · `closeButton → Generic Close Button Orange` ·
//       `canvasGroup → 根上那颗 CanvasGroup` · `layoutGroup → Content 上那颗 VerticalLayoutGroup` ·
//       `premiumStoreReference → null`（**空引用**，数据源在远端 LiveOps 配置）。
//     `PuchasePremiumArmyInfo`（原版就这么拼）：`armyIcon → Army Icon` · `description → Description Content` ·
//       `purchasedText → Purchased Text` · `purchaseButton → Generic UI Button` ·
//       `priceDisplayButton → Price Display Button 2`。
//     `PuchasePremiumArmyContainer`：`toggleButton → Army Container` · `backgroundArmyImage → Army Image Background` ·
//       `armyName → Army Name` · `premiumObjects → Premium Unlocked`。
// 行为 = 反编译 `d:/2/tools/decomp_full/PurchasePremiumWindow__{OnEnable,TryOpen,Initialize,FetchData,
//   ClearCurrentArmyContainers,FocusOnArmy,SelectArmy,CloseButtonClick}.c` ＋
//   `PuchasePremiumArmyInfo__Initialize.c` ＋ `SimpleArmyImage`（= `Reward Event Faction Bonus Container`，
//   那一族归 `Shell/RankedRewardEventWindow.cs`）。
//
// ============================ 窗参（MB 逐字段实读）============================
// `type = 1`(Popup) · `windowsPlacement = 15`(Popup) · `closeOnESC = 1` · `updateNavPanel = 0` ·
// `extraScaleSmallScreen = 1.0`。根上**只有两颗组件**（`PurchasePremiumWindow` + `CanvasGroup(1,true,true)`）
// —— ⛔ **没有 `TransformScalerBySmallScreenUI`**（「窗口根上带成品的 3 扇」不含它，判据 → `Shell/TrophyInfoPopup.cs`）。
//
// ============================ 🔴 它是「带参数的窗口」那一族 ============================
// 全库 **7 个**覆写了 `TryOpen(data, options)` 的窗（`DeckEditingWindow` / `DeckSelectionPopup` /
// `PurchasePremiumWindow` / `RankedEventWindow{,V2}` / `SinglePlayerOnlyEnergyWindow` / `SkirmishEventWindow`，
// 形状一律「**先调基类、再补自己的刷新**」）—— `Shell/WindowsManager.cs` 的 `GameWindow.TryOpen(data, options)` 那句注释指的就是这张表。
// 本窗那一支（`PurchasePremiumWindow__TryOpen.c` 逐句）：
//   `base.TryOpen(data, options)` → `Initialize()` → `FocusOnArmy(data as CardArmy? ?? Ultramarines(10))`。
//   ⚠️ **`data` 是 `Nullable&lt;CardArmy&gt;`**、为空时取 **`10` = `CardArmy.Ultramarines`**（枚举实读）。
//   我们同形：`OpenEx(...)` 先走基类那条路、再 `Initialize()` + `FocusOnArmy(...)`，**⛔ 不另立一套参数表**。
//
// ============================ 🔴 三处「照原版自己的分支」 ============================
// ① **`SubTitle` 出厂 `act = F`**（prefab 实测）⇒ 建成关着的（同 `TrophyInfoPopup` 的 `Category` 口径）。
// ② **`Price Display Button 2/Generic UI Button/Button Text` 出厂 `act = F`**（那是价签那颗钮的备用文本）
//    ⇒ 建成关着的。
// ③ **`Scrollbar Collection` 出厂 `act = F`** ⇒ 它的两个子件（`Sliding Area` / `Handle`）在 original 里
//    是 `ANC✗`（**祖先 inactive ⇒ 原版根本不画它们**，dump 逐行标了）⇒ 我们**建节点、但不建那两颗子件**？
//    ⛔ **不** —— 判据是「照 prefab 的节点表建、照 `activeSelf` 摆」：三颗都建，`Scrollbar Collection` 关着
//    （关着 ⇒ 子树整个不画，与 `ANC✗` 的可观测结果一致，而节点表仍然是完整的 34 个）。
// ④ **模板在 `Initialize()` 用完就被关掉**：`armyInfoContainerReference.gameObject.SetActive(false)`
//    （`PurchasePremiumWindow__Initialize.c` 尾段）⇒ 我们同样把 `Army Container` 建成**关着**的。
//
// ============================ 🔴 没数据时怎么办（本地恒无数据）============================
// `FetchData()` 读的是 `premiumStoreReference`（`LoadableReference&lt;ShopStoreData&gt;`）→ LiveOps 处理器
// → **远端 CCD**。而 `premiumStoreReference` 在 prefab 里**就是空引用**（pid 0）。
// ⇒ 原版这一档：列表为空 ⇒ 一个容器都不实例化 ⇒ 紧接着 `_armies[0]` **越界抛**
//   （`Initialize()` 尾段就是 `get_Item(list, 0)`）—— **运行时它是一扇「有数据才开得出来」的窗**。
// ⇒ **我们的做法**（红线：不许编内容、也不许假装有数据）：
//   · 节点表**全建**（34 个）、出厂显隐逐条照 prefab；
//   · `Content` 下**一个容器都不建**、`Army Container` 模板**关着**（= `Initialize()` 尾段那一句）；
//   · `Army Info` 那几栏用 **prefab 出厂原文**（`BUY ONCE, PROFIT ENDLESSLY` / 那三条 bullet / `300,00` /
//     `Purchased!` / `Ultramarines`）—— 原版这几格分别是 I2 词条与**逐军数据**，本地都没有；
//   · **出声**说明（`Open()` 里那一条 `Debug.Log`）。
// ============================ 🔴 开场动画（**已实现**）============================
// `OnEnable()` 逐句：`canvasGroup.alpha = 0` → `interactable = false` → `DOFade(1, 0.3)` →
//   `transform.localScale = (0.8,0.8,0.8)` → `DOScale((1,1,1), 0.3)`；`OnComplete` 里再把 `interactable` 打开。
//   `0.3` 那个时长是**从二进制里读出来的**（`GameAssembly.dll` RVA `0x34B2DC8` = `0.3f`）；
//   `0.8` 是 `0x3f4ccccd`（= 0.8f，指令流里的立即数）。
//   ⚠️ `DOScale` 的**目标值**来自一个静态 `Vector3`（`DAT_1842da2a8` 的 `+0xc`）—— 那是 DOTween 的
//   缓存 `Vector3`。**我没有把它读成具体值**（要再追一层静态字段链）⇒ 按「根 `m_LocalScale = (1,1,1)`」
//   取 **`Vector3.one`**，并在报告 §九 如实标出这一步。缓动 = DOTween 默认 `Ease.OutQuad`。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `PurchasePremiumWindow`（`GameWindow` 子类）—— 左栏一列阵营、右栏该阵营的高级战役说明与价签。
    /// <para>🔴 **它不在任何一页的层带里**：自成一档 **3521–3546**（排行榜 `LeaderboardWindow` 3500–3520
    /// 之上、通用弹窗 `PopUpGameWindow` 3560–3565 之下 —— 层带不许重叠）。</para>
    /// <para>⚠️ **本地走不到这条路**：数据源在远端 LiveOps；全量反编译里也没有第二个引用它的地方
    /// （只命中它自己那 20 个 `.c`）⇒ **入口由调用方自己定**（⛔ 我们不编一个）。</para></summary>
    public class PurchasePremiumWindow : GameWindow
    {
        // ============================================================ 队列档（本窗自成一档 · 3521–3546）
        //   逐层顺序 = **原版兄弟序**（uGUI 按兄弟序画 ⇒ 后面的压前面的；判据 = prefab 的 `m_Children`）：
        //   根 = [Menu Dark Background, Generic Window Red Background Big, Title, SubTitle, Premium image,
        //         Army Info, Scroll View, Scrollbar Collection, Generic Close Button Orange]。
        public const int QShade = 3521,          // `Menu Dark Background`
                         QShadeHit = 3522,       // └ 它的命中区（点窗外关窗）
                         QWinBg = 3523,          // `Generic Window Red Background Big`
                         QTitle = 3524,          // `Title`
                         QSubTitle = 3525,       // `SubTitle`（出厂关）
                         QPremIcon = 3526,       // `Premium image`
                         QInfoBg = 3527,         // `Army Info/Background`
                         QIcon = 3528,           // `Army Info/Army Icon`
                         QInfoTitle = 3529,      // `Description title`
                         QInfoText = 3530,       // `Description Content`
                         QPriceBtn = 3531,       // `Price Display Button 2/Generic UI Button`
                         QPriceBtnText = 3532,   // └ `Button Text`（出厂关）
                         QPriceBox = 3533,       // └ `Price Display`
                         QPriceIcon = 3534,      //    └ `icon`
                         QPriceText = 3535,      //    └ `text`
                         QPurchased = 3536,      // `Purchased Text`
                         QScroll = 3537,         // `Scroll View`
                         QContainer = 3538,      // `Content/Army Container`（**模板**）
                         QContainerName = 3539,  // └ `Army Name`
                         QContainerHi = 3540,    // └ `Hightlight`
                         QPremiumUnlocked = 3541,//    └ `Premium Unlocked`
                         QPremiumIcon = 3542,    //       └ `Premium image`
                         QPremiumText = 3543;    //          └ `Premium Text`
        /// <summary>`Scrollbar Collection`（出厂关，整棵不画）那一档。</summary>
        public const int QScrollbar = 3544;
        public const int QClose = 3545, QCloseIcon = 3546;

        // ============================================================ 几何（**全部 `--relative` 现读**）
        //   🔴 **本窗的坐标是「相对根左上角」那一档**（`--relative`）—— 根的绝对框另有换算，见 `RootRect()`。
        //   两档都现读核过（`--relative` 与不带参数各跑一次），逐节点 `绝对 = 根原点 + 相对` 成立。
        /// <summary>根尺寸（`m_SizeDelta`）：1585.65 × 938.12。锚点 `(0.5,0.5)`、pivot `(0.5,0.5)`、
        /// `anchoredPosition = (0,0)` ⇒ 根**居中在屏幕上**（绝对框 = 167.17,70.94→1752.83,1009.06，现读 ✓）。</summary>
        public const float RootW = 1585.65f, RootH = 938.12f;
        /// <summary>根左上角在画布（y 向下）里的位置 = `(960 − W/2, 540 − H/2)`（ap 两个分量都是 0）。</summary>
        public const float RootX1 = 960f - RootW * 0.5f, RootY1 = 540f - RootH * 0.5f;

        // 根的直接子件（**相对框**，逐条 = dump 现读）
        static readonly PxRect ShadeR       = new PxRect(-1494.48f, -817.12f, 3080.13f, 1755.24f);  // 4574.60×2572.36，**以根中心为中心**
        static readonly PxRect WinBgR       = new PxRect(15.09f, -15.30f, 1605.76f, 973.95f);      // 1590.67×989.256
        static readonly PxRect TitleR       = new PxRect(756.33f, 57.49f, 1497.67f, 141.71f);
        static readonly PxRect SubTitleR    = new PxRect(651.02f, 213.19f, 1280.98f, 269.01f);     // 出厂 **F**
        static readonly PxRect PremImgR     = new PxRect(651.17f, 47.52f, 754.08f, 151.80f);
        static readonly PxRect ArmyInfoR    = new PxRect(642.75f, 183.46f, 1547.17f, 871.66f);
        static readonly PxRect ScrollViewR  = new PxRect(32.04f, 4.63f, 616.95f, 923.07f);
        static readonly PxRect ScrollbarR   = new PxRect(26.92f, 4.63f, 46.92f, 893.01f);          // 出厂 **F**
        static readonly PxRect CloseR       = new PxRect(1547.16f, -34.24f, 1621.55f, 41.36f);

        // `Army Info` 子树（相对**根**）
        static readonly PxRect InfoBgR      = new PxRect(642.53f, 183.89f, 1547.37f, 871.22f);
        static readonly PxRect ArmyIconR    = new PxRect(655.13f, 190.38f, 754.08f, 289.34f);
        static readonly PxRect DescTitleR   = new PxRect(754.00f, 219.12f, 1522.10f, 274.94f);
        static readonly PxRect DescBodyR    = new PxRect(679.90f, 310.96f, 1521.40f, 705.56f);
        static readonly PxRect PriceBtnR    = new PxRect(930.60f, 760.07f, 1259.31f, 833.45f);
        static readonly PxRect PriceBtnTxtR = new PxRect(943.60f, 767.32f, 1246.31f, 826.20f);     // 出厂 **F**
        static readonly PxRect PriceBoxR    = new PxRect(947.36f, 766.72f, 1240.57f, 826.74f);
        static readonly PxRect PriceIconR   = new PxRect(1063.96f, 766.72f, 1123.98f, 826.74f);    // ×1.2 ⇒ 视觉 72.03²
        /// <summary>⚠️ **这一格是「我们摆的」，不是原版量出来的**（全文件**唯一**一处非逐字照抄）：
        /// dump 量出来 `text` 的宽 = **0.00**，因为它带 `ContentSizeFitter(h:PreferredSize)`、而文字宽要 Unity 的字体度量。
        /// **能反推的那一半**（逐位可验）：`icon.x1 = PriceBox.x1 + (293.21 − 总首选宽)/2 = 947.36 + 116.6`
        /// ⇒ **总首选宽 = 60.01 = icon 的宽**（60.025）⇒ 证明 dump 那一刻 **`text` 的宽确实是 0**（不是被截掉）。
        /// **推不出来的那一半** = `text` 真正的宽（要字体度量）。
        /// ⇒ 取 **`[icon 右沿, PriceBox 右沿]` = 116.59**（= 框的右半），并**照样把 `text` 画出来**
        /// （判据旁的旁证：同族的 `BaseOfferPopup` 里那颗 `Price Display/text` 是 **124.50** 宽，同一数量级）。
        /// ⛔ **改版式时从这一格看起**。</summary>
        static readonly PxRect PriceTextR   = new PxRect(1123.98f, 766.72f, 1240.57f, 826.74f);
        static readonly PxRect PurchasedR   = new PxRect(779.98f, 753.82f, 1409.94f, 839.10f);

        // `Scroll View` 子树（相对**根**）
        static readonly PxRect ViewportR    = new PxRect(40.54f, 4.63f, 608.45f, 923.07f);
        static readonly PxRect ContentR     = new PxRect(40.54f, 4.63f, 608.44f, 192.95f);         // 空列表那一档（CSF 现读）
        static readonly PxRect BarSlideR    = new PxRect(36.92f, 14.63f, 36.92f, 883.01f);         // `ANC✗`（祖先关）
        static readonly PxRect BarHandleR   = new PxRect(26.92f, 14.63f, 46.92f, 883.01f);         // 同上（dump 的 y 是 nan ⇒ 用父框）

        // `Army Container`（**模板**）子树（相对**根** —— 这一份是「只有 1 个容器时」那一档的现读值，
        //   多容器时它自己那份由 `ArmyContainerRect(i)` 按 `Content` 的 VLG 算式重算）
        static readonly PxRect ContainerR   = new PxRect(58.88f, 29.63f, 590.11f, 192.95f);
        static readonly PxRect ContNameR    = new PxRect(88.72f, 51.89f, 459.93f, 101.89f);
        static readonly PxRect ContHiR      = new PxRect(58.88f, 29.63f, 590.11f, 192.95f);
        static readonly PxRect PremUnlockR  = new PxRect(65.89f, 92.95f, 165.89f, 192.95f);
        static readonly PxRect PremIconR    = new PxRect(74.98f, 98.44f, 164.00f, 187.46f);
        static readonly PxRect PremTextR    = new PxRect(172.39f, 115.15f, 484.10f, 165.15f);
        /// <summary>容器高（`m_SizeDelta.y`）与容器间距（`Content` 的 VLG `m_Spacing`）。</summary>
        public const float ContainerH = 163.326f, ContainerStep = 168.326f;

        // `Generic Close Button Orange` 子树（相对根）
        static readonly PxRect CloseBgR     = new PxRect(1555.31f, -26.26f, 1612.18f, 31.86f);

        // ============================================================ 图 / 色 / 九宫（逐条 MB 实读）
        public const string ArtWinBg = "UI_Deck_Information_Back";       // 1100×701 · border 42,363,655,81 · Sliced
        public const string ArtInfoBg = "UI_Deck_Information_submenu_Back";  // 69×63 · border 18,18,18,18 · Sliced
        public const string ArtPremIcon = "40k_campaign_Premium-icon";   // 301×305 · Simple
        public const string ArtBtn = "UI_Button_Mulligan",               // 410×124 · border 333,96,333,96 · Sliced · PA · ppuMul 2
                            ArtBtnHover = "UI_Button_Mulligan_hover",
                            ArtBtnPressed = "UI_Button_Mulligan_Pressed";
        /// <summary>`Army Container/Hightlight` 的图 —— 🔴 **工程里没有**（源在
        /// `bundle_atlasindividual_assets_0_mainmenu/Sprite/UI_HIghlight Internal.json`，只 staged 在
        /// `Assets/CardPresentation/Art/原版/0_mainmenu/UI_HIghlight_Internal.png`、**没进 `Resources/`**）
        /// ⇒ 节点照建、**这一格不画** + 出声。</summary>
        public const string ArtHightlight = "UI_HIghlight_Internal";
        public const string ArtBarBg = "40k_menu_scroll_bar_bg";         // 20×50 · border 0,12,0,12 · Sliced
        public const string ArtBarFill = "40k_menu_scroll_bar_fill";     // 同上
        public const string ArtClose = "UI_Button_Round_background";     // 237² · Simple · PA
        public const string ArtCloseBg = "40k_general_bt_yellow";        // 71² · Simple · PA
        public const string ArtCloseIcon = "40k_general_bt_yellow_close";// 同上
        /// <summary>`60×60` 的那颗价签图标 —— 原版 `m_Sprite = &lt;无图&gt;`（运行期 `PriceDisplayButton.Setup` 灌）
        /// ⇒ 只建节点、不画。</summary>
        public static readonly Color TitleTint = new Color(0.915f, 0.541f, 0f, 1f);        // `Title` 实读
        public static readonly Color PremTextTint = new Color(0.996f, 0.596f, 0.118f, 1f); // `Purchased Text` / `Premium Text` 实读
        public static readonly Color HiTint = new Color(0.46698f, 0.88011f, 1f, 1f);       // `Hightlight` 实读
        public static readonly Color ShadeTint = new Color(0f, 0f, 0f, 0.772549f);         // 实读（同族统一那个值）

        // ============================================================ 出厂文本（prefab 原文，逐字）
        public const string TxtTitle = "Premium campaigns";
        public const string TxtSubTitle = "Buy premium campaigns to get more rewards!";      // `SubTitle`（出厂关）
        public const string TxtInfoTitle = "BUY ONCE, PROFIT ENDLESSLY";
        public const string TxtInfoBody =
            "• Unlock the Premium rewards in this faction's Campaign, including a Legendary Wildcard in the first node.\n" +
            "• {0} Campaign Points for this faction in the Daily Login Bonus, every day, just for logging in. " +
            "If you have several Premium Campaigns, you get this bonus for ALL of them!\n" +
            "• This one-time purchase will provide benefits for this faction forever, as the last Campaign node can be claimed repeatedly!";
        public const string TxtPrice = "300,00";
        public const string TxtPurchased = "Purchased!";
        public const string TxtArmyName = "Ultramarines";
        public const string TxtPremium = "Premium";
        /// <summary>`Description Content` 里那个 `{0}` 对应的 I2 词条键 —— 原版在
        /// `PuchasePremiumArmyInfo__Initialize.c` 里 `string.Format(GetTranslation("MainMenu/PurchasePremium/Description"), points)`，
        /// `points = Missions.GetPointsPerArmyOnDailyLogin(army)`。**本地没有词条表、也没有那个 points** ⇒ 留 `{0}` 原样。</summary>
        public const string TermInfoBody = "MainMenu/PurchasePremium/Description";

        // ============================================================ 字号（逐颗 MB 实读）
        public const float TitleFont = 45f, TitleBase = 36f, TitleMin = 10f, TitleMax = 45f;
        public const float SubTitleFont = 36f, SubTitleBase = 36f, SubTitleMin = 18f, SubTitleMax = 36f;
        public const float InfoTitleFont = 40f, InfoTitleBase = 36f, InfoTitleMin = 10f, InfoTitleMax = 40f;
        public const float InfoBodyFont = 40f, InfoBodyBase = 36f, InfoBodyMin = 10f, InfoBodyMax = 40f;
        public const float PriceBtnTxtFont = 12f, PriceBtnTxtBase = 12f, PriceBtnTxtMin = 12f, PriceBtnTxtMax = 38f;
        public const float PriceTextFont = 40f, PriceTextBase = 39f, PriceTextMin = 13.46f, PriceTextMax = 40f;
        public const float PurchasedFont = 61.89f, PurchasedBase = 36f, PurchasedMin = 18f, PurchasedMax = 61.89f;
        public const float ArmyNameFont = 36f, ArmyNameBase = 36f;
        public const float PremiumFont = 30.7f, PremiumBase = 30.7f, PremiumMin = 18f, PremiumMax = 30.7f;

        // ============================================================ 开场动画（`OnEnable` 那一段）
        /// <summary>`CanvasGroup` 淡入时长（二进制实读 `GameAssembly.dll` RVA `0x34B2DC8` = **0.3f**）。</summary>
        public const float PopDuration = 0.3f;
        /// <summary>起始缩放（指令流里的立即数 `0x3f4ccccd` = **0.8f**）。</summary>
        public const float PopFromScale = 0.8f;

        // ============================================================ 数据（原版 `PremiumArmyPurchaseData`）
        /// <summary>一条「高级战役」报价（= 原版 `PurchasePremiumWindow.PremiumArmyPurchaseData` 的公开面）。
        /// 原版字段（`dump.cs`）：`CardArmy` / `IapData` / `Price` / `Purchased` / `EventId`。
        /// 🔴 **本地一条都没有**（`premiumStoreReference` 是空引用、数据在远端 LiveOps）
        /// ⇒ `OpenEx` 不传就是**空列表**（= 原版「列表为空」那个局面）。</summary>
        public struct ArmyOffer
        {
            /// <summary>`CardArmy` 枚举值（`Ultramarines = 10` … `SpaceWolves = 130`，枚举实读）。</summary>
            public int Army;
            /// <summary>显示名（原版从阵营 SO 拿；本地有 13 个阵营名字表 ⇒ 调用方可传）。</summary>
            public string ArmyName;
            /// <summary>原版 `Purchased`：真 ⇒ 开 `Purchased Text`、关价签；假 ⇒ 反过来。</summary>
            public bool Purchased;
            /// <summary>价签上那串字（原版 `IapData.Price`）。</summary>
            public string PriceText;
            /// <summary>原版还有一位「这个阵营是不是已经解锁（`premiumObjects`）」——
            /// `PuchasePremiumArmyContainer.Initialize` 用它开关 `Premium Unlocked`。我们同义收一格。</summary>
            public bool PremiumUnlocked;
        }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static PurchasePremiumWindow LastOpened { get; private set; }

        ArmyOffer[] _offers = new ArmyOffer[0];
        /// <summary>当前那份报价（自检读口）。**长度 0 = 没数据**（本地恒真）。</summary>
        public ArmyOffer[] Offers { get { return _offers; } }
        /// <summary>原版 `TryOpen` 里那个「聚焦到哪个阵营」（`CardArmy`，缺省 `Ultramarines = 10`）。</summary>
        public int FocusArmy { get; private set; }

        Label _title, _subTitle, _infoTitle, _infoBody, _priceText, _purchased;
        Transform _content, _armyInfo, _containerTemplate;
        GameObject _priceBtn;
        readonly System.Collections.Generic.List<Transform> _containers = new System.Collections.Generic.List<Transform>();

        // 开场动画（`OnEnable` 那一段；`_popT < 0` = 没在跑）
        float _popT = -1f;
        CanvasGroup _cg;

        // ============================================================ 开

        /// <summary>建一扇。`offers == null` ⇒ **空列表**（本地恒如此，见文件头那一段）。</summary>
        public static PurchasePremiumWindow Create(WindowsManager mgr, ArmyOffer[] offers = null, int? focusArmy = null)
        {
            var go = new GameObject("Purchase Premium Window");   // 节点名照原版（`WindowsManager` 复用的键同源）
            var win = go.AddComponent<PurchasePremiumWindow>();
            win.type = WindowType.Popup;                  // 实读 `type = 1`
            win.placement = WindowsPlacement.Popup;       // 实读 `windowsPlacement = 15`
            win.closeOnEsc = true;                        // 实读 `closeOnESC = 1`
            win.extraScaleSmallScreen = 1f;               // 实读 1.0
            win.Manager = mgr;
            win._offers = offers ?? new ArmyOffer[0];
            win.FocusArmy = focusArmy ?? 10;              // 实读：`data` 为空时取 `10` = `CardArmy.Ultramarines`
            WindowsManager.AttachToAnchor(win);
            if (mgr != null) mgr.OpenWindow(win);
            else { Debug.LogWarning("[Premium] 没有 `WindowsManager` ⇒ 只建出来、没进窗口管理器。"); win.Open(); }
            return win;
        }

        /// <summary>原版那条**带参数**的路（`TryOpen(data, options)` 覆写的等价物 —— 见文件头那一段）。
        /// `data` = `CardArmy?`、`options` 我们这一侧没有对应物（`GameWindowOptions` 全仓未建模）⇒ 只保留 `army`。
        /// ⚠️ 复用那一支也要能把新数据喂进来（`GameWindow.TryOpen` 在 `CurrentState == Open` 时**早退**，
        /// 照原版 A217②）⇒ 这个方法**每次都重建内容**（`Initialize()` 是原版明写的重建入口）。</summary>
        public PurchasePremiumWindow OpenEx(int? army = null, ArmyOffer[] offers = null)
        {
            if (army.HasValue) FocusArmy = army.Value;
            if (offers != null) _offers = offers;
            Initialize();
            return this;
        }

        public override void Open()
        {
            LastOpened = this;
            // 🔴 **2026-10-13（A803）就地订正（铁律 5）**：这里原来**只有 `Initialize()`、从不 `Build()`**
            // —— 而 `Build()` 才是铺整棵树那一跳（`Build()` 全仓**零调用点**）⇒ 窗对象在、六条窗参全绿，
            // 但 `transform.childCount == 0`：**开出来是空树**（约 23 条红跨 `ShellScene` + `ShopScene`）。
            // 判据：**同族另五扇 `Open()` 全都 `Build()` 打头**（`GenericOptionsPanel:243` ·
            // `AllianceMemberOptionsPopup:229` · `BaseOfferPopup:643/663` · `ReferralPopupWindow:307` ·
            // `RankedRewardEventWindow:255`）—— 只这一扇漏。`Build()` 尾段自带 `Initialize()` + `StartPop()`
            // ⇒ 下面那两句保持原样、顺序不变（⛔ 别把 `Initialize()` 挪到 `Build()` 前）。
            Build();
            Initialize();
            Debug.Log("[Premium] 开了 `Purchase Premium Window`（原版 `PurchasePremiumWindow`）。"
                    + (FocusArmy >= 0 ? "聚焦阵营 = " + FocusArmy : "")
                    + "。⚠️ **本地没有任何报价**（`premiumStoreReference` 在 prefab 里就是空引用、数据在远端 "
                    + "LiveOps）⇒ `Content` 下一个容器都不建、`Army Container` 模板关着"
                    + "（= 原版 `Initialize()` 尾段那一句 `armyInfoContainerReference.SetActive(false)`）；"
                    + "`Army Info` 那几栏用 **prefab 出厂原文**（原版那几格是 I2 词条 + 逐军数据）。");
        }

        /// <summary>原版 `Initialize()`：`_armies = FetchData()` → `ClearCurrentArmyContainers()` →
        /// 逐条 `Instantiate` → `armyInfo.Initialize(_armies[0])` → **模板 `SetActive(false)`**。
        /// <para>🔴 原版在 `_armies` **非空**时才会走到 `_armies[0]`（空列表它直接越界抛）
        /// ⇒ 我们空列表那一支**只做前两跳 + 关模板**，语义与原版「有数据才开得出来」自洽。</para></summary>
        public void Initialize()
        {
            ClearArmyContainers();
            RebuildContainers();
            // 模板：原版**用完就关**（`Initialize.c` 尾段）⇒ 我们建出来那一颗也关掉。
            // ⚠️ 它在 `Build()` 里已经建成关着的了；这里再关一次是**幂等**的（复用路径上不会复活）。
            if (_containerTemplate != null) _containerTemplate.gameObject.SetActive(false);
            ApplyArmyInfo(_offers.Length > 0 ? (ArmyOffer?)_offers[0] : null);
            FocusOnArmy(FocusArmy);
        }

        /// <summary>本窗内容命中区那一档（**压暗层命中区严格低于它** —— `MenuDraw.ShadeHit` 现场核）。</summary>
        public const int QHit = QCloseIcon + 1;      // 3547

        /// <summary>`Content`（`VerticalLayoutGroup` + `ContentSizeFitter(v:PreferredSize)`）的高度。
        /// `m_Padding(0,0,25,0)` · `spacing = 5` · `childControlHeight = 0` ⇒ 每格 = 自己的 `sizeDelta.y`
        /// = 163.326。n = 0 时 uGUI 直接返回 0（现读那一档是 n = 1：`4.63 + 188.326 = 192.95` ✓）。</summary>
        public static float ContentH(int n)
        {
            return n <= 0 ? 0f : 25f + ContainerStep * n - 5f;
        }

        /// <summary>原版 `ClearCurrentArmyContainers()`：逐个 `DestroyImmediate` 之后清空列表。</summary>
        void ClearArmyContainers()
        {
            foreach (var c in _containers) if (c != null) MainMenuSubmenuWindow.DestroySafe(c.gameObject);
            _containers.Clear();
        }

        /// <summary>原版 `FocusOnArmy(CardArmy)`：在容器列表里找到 `data.CardArmy == 目标` 那颗、
        /// `toggle.isOn = true` 并 `ScrollViewFocusFunctions.FocusOnItem(...)`。
        /// <para>⚠️ 我们**没有 uGUI 的 `Toggle`**、也**没有 `ScrollViewFocusFunctions`** ⇒ 我们能做的那一半：
        /// 把滚动区**移到那一颗**（`MenuScroll.SetOffset`）；聚焦不到就**出声**（红线：不许静默）。</para></summary>
        public void FocusOnArmy(int army)
        {
            if (_offers.Length == 0)
            {
                Debug.Log("[Premium] `FocusOnArmy(" + army + ")`：**一条报价都没有** ⇒ 列表是空的、没有可聚焦的格子"
                        + "（原版这一支在 `foreach` 之后直接 `throw` —— 它就是「有数据才开得出来」那件事）。");
                return;
            }
            for (int i = 0; i < _offers.Length; i++)
            {
                if (_offers[i].Army != army) continue;
                SelectArmy(i);
                if (_scroll != null)
                {
                    // 原版那一跳是 `ScrollViewFocusFunctions.FocusOnItem(scrollRect, 那一格)`
                    // —— 等价物 = 把内容滚到让那一格的**中心**落在视口中心（同一份偏移算法）。
                    // 🔴 **2026-10-13（A806 同族）**：`_scroll.Viewport` 是**绝对**档（`MenuScroll.TopAligned(Abs(ViewportR), …)`）
                    // ⇒ 这一格也要 `Abs(...)`，否则偏移会差一个根原点 `RootY1`（同 `RebuildContainers` 那条）。
                    var r = Abs(ContainerRect(i));
                    _scroll.SetOffset(_scroll.Viewport.CY - (r.y1 + r.y2) * 0.5f);
                }
                return;
            }
            Debug.Log("[Premium] `FocusOnArmy(" + army + ")`：列表里没有这个阵营 ⇒ 没有可聚焦的格子"
                    + "（原版这一支是 `foreach` 里直接 `throw`，我们如实出声、不做动作）。");
        }

        /// <summary>原版 `SelectArmy(data)`：`armyInfo.Initialize(data, this)` —— 右栏换成这一档。</summary>
        public void SelectArmy(int index)
        {
            if (index < 0 || index >= _offers.Length) return;
            ApplyArmyInfo(_offers[index]);
        }

        /// <summary>把一条报价铺到 `Army Info` 上（= 原版 `PuchasePremiumArmyInfo.Initialize` 的可见那一半）：
        /// `armyIcon.sprite = GetArmyIcon(army)` → `description = Format(词条, points)` →
        /// `Purchased` 决定「价签」与「`Purchased!`」谁开。
        /// <para>⚠️ `armyIcon` 的图与那句 `points` 本地都取不到（前者要 `ArmyUtilities.GetArmyIcon`，
        /// 后者要 LiveOps 的 `Missions.GetPointsPerArmyOnDailyLogin`）⇒ 图标**只建节点、不画**并出声。</para></summary>
        void ApplyArmyInfo(ArmyOffer? o)
        {
            if (o.HasValue)
            {
                if (_infoTitle != null) _infoTitle.SetText(TxtInfoTitle);
                if (_infoBody != null) _infoBody.SetText(TxtInfoBody.Replace("{0}", "?"));   // 原版是 string.Format(词条, points)
                if (_priceText != null) _priceText.SetText(o.Value.PriceText ?? TxtPrice);
                if (_priceBtn != null) _priceBtn.SetActive(!o.Value.Purchased);
                if (_purchased != null) _purchased.gameObject.SetActive(o.Value.Purchased);
                Debug.Log("[Premium] `Army Icon` 的图取不到（原版 `ArmyUtilities.GetArmyIcon(army)`）⇒ 只建节点；"
                        + "`Description Content` 里那个 `{0}`（原版 `string.Format(词条, "
                        + "Missions.GetPointsPerArmyOnDailyLogin(army))`）本地也没有 ⇒ 打 `?`。");
            }
            else
            {
                // 没数据那一支：照 **prefab 出厂态**（两栏都开、文字是出厂原文）
                if (_infoTitle != null) _infoTitle.SetText(TxtInfoTitle);
                if (_infoBody != null) _infoBody.SetText(TxtInfoBody);
                if (_priceText != null) _priceText.SetText(TxtPrice);
                if (_priceBtn != null) _priceBtn.SetActive(true);
                if (_purchased != null) _purchased.gameObject.SetActive(true);
            }
        }

        // ============================================================ 几何

        /// <summary>根矩形（绝对画布 px）。锚/pivot 都是 `(0.5,0.5)`、`anchoredPosition = (0,0)` ⇒ 屏幕上居中
        /// （现读绝对框 167.17,70.94→1752.83,1009.06 ✓ 与本式逐位相同）。</summary>
        public static PxRect RootRect()
        {
            return new PxRect(RootX1, RootY1, RootX1 + RootW, RootY1 + RootH);
        }

        /// <summary>把「相对根的框」搬到绝对画布坐标（**全窗唯一一处换算**，同 `MenuDraw.Local` 那条纪律）。</summary>
        public static PxRect Abs(PxRect rel)
        {
            return new PxRect(RootX1 + rel.x1, RootY1 + rel.y1, RootX1 + rel.x2, RootY1 + rel.y2);
        }

        /// <summary>第 `i` 个容器的框（= `Content` 的 `VerticalLayoutGroup` 跑出来的那一档）。
        /// `Content`：`m_Padding(0,0,25,0)` · `align = 1 (UpperCenter)` · `spacing = 5` · `ctrlH = 0`
        /// ⇒ 每格高 = 自己的 `sizeDelta.y = 163.326`、步进 = `163.326 + 5 = 168.326`。
        /// 现读那一档（i = 0）= `58.88,29.63→590.11,192.95`：`Content.y1 + 25 = 4.63 + 25 = 29.63` ✓
        /// （相对根）。⚠️ `.x` 是 `Content` 的内宽 567.9 − 两侧各 0 ⇒ 容器宽 531.23、水平居中。</summary>
        public PxRect ContainerRect(int i)
        {
            float top = ContentR.y1 + 25f + ContainerStep * i;
            return new PxRect(58.88f, top, 590.11f, top + ContainerH);
        }

        // ============================================================ 建

        public void Build()
        {
            var root = transform;
            MenuDraw.ClearChildren(root);
            MissingArt.Clear();
            _containers.Clear();
            _title = _subTitle = _infoTitle = _infoBody = _priceText = _purchased = null;
            _content = null; _armyInfo = null; _containerTemplate = null; _priceBtn = null;

            // ---- 根上那颗 `CanvasGroup`（原版有、动画要它）----
            _cg = GetComponent<CanvasGroup>();
            if (_cg == null) _cg = gameObject.AddComponent<CanvasGroup>();
            _cg.alpha = 1f; _cg.interactable = true; _cg.blocksRaycasts = true;
            _popT = -1f;

            // ---- 1) `Menu Dark Background` ----
            var S = Abs(ShadeR);
            MenuDraw.Rect(root, CardArt.Solid(), S, "Menu Dark Background", QShade, ShadeTint);
            MenuDraw.ShadeHit(root, S, QShade, QHit, () => Close(), "BackgroundHit");

            // ---- 2) `Generic Window Red Background Big`（九宫 `UI_Deck_Information_Back`）----
            var winBgTex = Tex(ArtWinBg, "窗体底");
            if (winBgTex != null)
                MenuDraw.Nine(root, winBgTex, Abs(WinBgR), new Vector4(42f, 363f, 655f, 81f), 1100f, 701f,
                              QWinBg, null, true, "Generic Window Red Background Big");
            else MenuDraw.Node(root, "Generic Window Red Background Big", Abs(WinBgR));

            // ---- 3) `Title`（hAlign = Left ⇒ `AlignLeft`；色 (0.915,0.541,0,1)）----
            _title = MenuDraw.TextBox(root, Abs(TitleR), TxtTitle, TitleTint, "Title",
                                      TitleFont, TitleMin, QTitle, TitleMax, TitleBase);
            if (_title != null) MenuDraw.AlignLeft(_title, Abs(TitleR));

            // ---- 4) `SubTitle`（**出厂 act = F** ⇒ 建成关着的）----
            _subTitle = MenuDraw.TextBox(root, Abs(SubTitleR), TxtSubTitle, Color.white, "SubTitle",
                                         SubTitleFont, SubTitleMin, QSubTitle, SubTitleMax, SubTitleBase);
            if (_subTitle != null)
            {
                MenuDraw.AlignLeft(_subTitle, Abs(SubTitleR));
                _subTitle.gameObject.SetActive(false);
            }

            // ---- 5) `Premium image`（Simple，不保比）----
            var premTex = Tex(ArtPremIcon, "`Premium image` 的图标");
            if (premTex != null)
                MenuDraw.Rect(root, premTex, Abs(PremImgR), "Premium image", QPremIcon);
            else MenuDraw.Node(root, "Premium image", Abs(PremImgR));

            // ---- 6) `Army Info`（`PuchasePremiumArmyInfo`）----
            BuildArmyInfo(root);

            // ---- 7) `Scroll View` → `Viewport` → `Content` ----
            //   ⚠️ `Scroll View` 自己那张 `Background` 与 `Viewport` 的 `UIMask` 两颗 `Image`
            //   在 prefab 里都是 **`m_Enabled = 0`**（组件级禁用）⇒ 照「不可见的件不画」的纪律**两颗都不建**
            //   （同 `RewardWindow` / `CampaignRewardWindow` 那条；节点照建）。
            //   ✅ **`Viewport` 的 `RectMask2D` 参数**（实读）：`m_Padding = (0,0,0,0)`、`m_Softness = (0,0)`
            //   —— 硬边、无内缩 ⇒ 我们这边**不需要设 `ClipPad`/软边**（全 0 就是同一件事）。
            var sv = MenuDraw.Node(root, "Scroll View", Abs(ScrollViewR));
            // 🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A768②）**：这颗 `Viewport` 从今天起就是**本视口的裁切状态载体**
            //   （= 上面第 7 步记的那个 `RectMask2D`）。**参数回原版逐字复核过（⛔ 不是照抄上面的注释）**：
            //    · `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8892924525201795015.json`
            //      = `m_Padding (0,0,0,0)` · `m_Softness (0,0)` · `m_Enabled 1` ·
            //      `m_Script.m_PathID = 536591447201701790`（= `UnityEngine.UI.RectMask2D`）；
            //      它的 `m_GameObject`（PathID `-514462124471887815`）按 `m_Father` 父链上行 =
            //      `Purchase Premium Window/Scroll View/Viewport`（本件现走一遍）。
            //   ⇒ **硬边、无内缩** —— 与迁移前「不裁」相比唯一的差别就是「按视口框裁」那一条（原版本来就是裁的）。
            //   ⛔ 别再退回 `MenuDraw.Node`（那样这一棵子树里所有 `clip == null` 的件又变成「不裁画出去」）。
            var vpVc = ViewportClip.Hang(sv, "Viewport", Abs(ViewportR), Vector4.zero, Vector2Int.zero);
            var vp = vpVc.transform;
            // ⚠️ **A465（W-A435己）在本文件留下的那段「这处没接、只报不改」的说明已被本件取代**
            //   （铁律 5 就地订正，原文见 git）：它当时列的三条理由里，②「不能跑 Unity 去验」仍然成立
            //   （本件也没跑），但调度台已按 A754 那条同族先例裁定「**算 A435 站点 ⇒ 要做**」。
            _content = MenuDraw.Node(vp, "Content", Abs(ContentR));
            // 纵向滚动（原版 `m_Horizontal 0 / m_Vertical 1`）⇒ 走公共件的 `TopAligned`（它顺手置 `Vertical = true`）。
            _scroll = MenuScroll.TopAligned(Abs(ViewportR), ContentH(0));
            // 🔴 **A465（W-A435己）那一行，本件补上**：构建循环（`RebuildContainers` 里
            //   `if (_scroll != null && !_scroll.Intersects(on)) continue;`）从今天起读**同一颗节点**的状态。
            //   ⚠️ **赋值必须紧跟「建这颗节点 / 建这个滚动区」的那一处**：`_content` / `_scroll` / 这颗
            //      `Viewport` 都是**每次 `Build()` 新建**的（`MenuDraw.ClearChildren(root)` 起手）⇒ 两者同频、
            //      写在建它的那一行旁边即可（⛔ 别写成 `if (_scroll == null)` 那种跨构建复用——那会指向已销毁的组件）。
            _scroll.ClipNode = vpVc;
            _scroll.Elastic = true;
            _scroll.Inertia = true;
            _scroll.Owner = gameObject;
            _scroll.OnChanged = RebuildContainers;
            PointerLayer.RegisterScroll(_scroll);
            // ⚠️ 原版 `m_ScrollSensitivity = 30`（逐处实读）—— 我们这条滚轮走的是**公共件那一份**系数
            //    （`MenuScroll.NotchK = 0.4`，全壳唯一一份）⇒ 手感与原版的 30 **不是同一个数**。
            //    如实记（报告 §九②），⛔ 不为这一扇再立第二份滚动实现。

            // 模板（`armyInfoContainerReference`，**出厂 act = T、`Initialize()` 用完关掉**）
            _containerTemplate = MenuDraw.Node(_content, "Army Container", Abs(ContainerR));
            BuildContainer(_containerTemplate, Abs(ContainerR), null);
            _containerTemplate.gameObject.SetActive(false);

            // ---- 8) `Scrollbar Collection`（**出厂 act = F** ⇒ 整棵不画；三颗节点照建）----
            //   🔴 **它是 `Scroll View` 的子件**（`menu_dump --relative` 的缩进 = `··2`；⛔ 不是根的直系子件
            //   —— 这一条是**对账脚本抓出来的**，我原来建成了根的兄弟）。层级错了会**静默**
            //   （同 `TrophyInfoPopup` 那三颗「节点按名字找得到、层级却是错的」的教训）。
            var sb = MenuDraw.Node(sv, "Scrollbar Collection", Abs(ScrollbarR));
            var sbTex = Tex(ArtBarBg, "滚动条槽");
            if (sbTex != null)
                MenuDraw.Nine(sb, sbTex, Abs(ScrollbarR), new Vector4(0f, 12f, 0f, 12f), 20f, 50f, QScrollbar,
                              null, true, "Image");
            // ⚠️ `Handle` 原版是 **`Sliding Area` 的子件**（`Scrollbar Collection > Sliding Area > Handle`），
            //    ⛔ 别建成兄弟（同 `TrophyInfoPopup` 那三颗「层级错了会静默」的教训）。
            var slide = MenuDraw.Node(sb, "Sliding Area", Abs(BarSlideR));
            var hdTex = Tex(ArtBarFill, "滚动条把手");
            if (hdTex != null)
                MenuDraw.Nine(slide, hdTex, Abs(BarHandleR), new Vector4(0f, 12f, 0f, 12f), 20f, 50f, QScrollbar,
                              null, true, "Handle");
            sb.gameObject.SetActive(false);

            // ---- 9) `Generic Close Button Orange` ----
            var close = MenuDraw.Node(root, "Generic Close Button Orange", Abs(CloseR));
            var closeTex = Tex(ArtClose, "关窗钮底");
            ImageQuad closeBg = null;
            if (closeTex != null) closeBg = MenuDraw.Rect(close, closeTex, Abs(CloseR), "Image", QClose, null, true);
            var cbTex = Tex(ArtCloseBg, "关窗钮内底");
            if (cbTex != null) MenuDraw.Rect(close, cbTex, Abs(CloseBgR), "Background", QClose, null, true);
            var ciTex = Tex(ArtCloseIcon, "关窗钮叉");
            if (ciTex != null) MenuDraw.Rect(close, ciTex, Abs(CloseBgR), "Icon", QCloseIcon, null, true);
            // `m_SpriteState` = dump 那一列实读的 `HL=40k_general_bt_yellow_hover P=40k_general_bt_yellow_pressed`
            var closeHit = MenuDraw.Hit(close, "Hit", Abs(CloseR), QHit, () => Close(), closeBg, ArtClose,
                                        "40k_general_bt_yellow_hover", "40k_general_bt_yellow_pressed");
            if (closeHit == null)
                Debug.LogWarning("[Premium] 关窗钮的命中区没建出来 ⇒ **点它关不了窗**（还能点窗外或 ESC）。");

            Initialize();     // = 原版 `TryOpen` 尾段那一跳（建容器 + 关闭模板 + 铺 `Army Info`）
            StartPop();       // = 原版 `OnEnable` 那一段（淡入 + 0.8→1 放大）
        }

        MenuScroll _scroll;
        /// <summary>`Scroll View` 那条滚动（自检读口）。</summary>
        public MenuScroll Scroll { get { return _scroll; } }

        /// <summary>滚动偏移变了 ⇒ 整列容器按新偏移重建（同 `RewardWindow.BuildItems` 那条规矩：
        /// 「滚动 ⇒ 内容按新偏移重建」）。
        /// <para>🔴 **`Viewport` 的 `RectMask2D` 是硬边、`padding` 全 0**（实读，见第 7 步那段）⇒ 裁切框 = 视口框本身。
        /// 走公共件的那条路：**整块在视口外 ⇒ 连节点都不建**（`MenuScroll.Intersects`），
        /// 部分越界的件由 `MenuDraw.Nine` / `Rect` / `Hit` **沿父链解析到那颗视口节点**后裁到视口内
        /// （A768② 起 —— 那颗 `Viewport` 上挂了 `ViewportClip`）。</para>
        /// <para>🔴 **2026-10-13（A435 阶段 2 · 庚 · W-A435庚 / A770）就地订正（铁律 5）** —— 本段原来写的是
        /// 「部分越界的件把 `clip` **显式**喂给 `MenuDraw.Nine/Rect/Hit`」，**那句话与代码不符**：
        /// 现读 `RebuildContainers` / `BuildContainer` **一个 `clip` 实参都没传**（`MenuDraw.Node` / `TextBox` /
        /// `Nine` / `Rect` / `Hit` 全是缺省）⇒ **滚到一半的容器是【不裁】画出来的**：`Hit` 命中区照旧铺满整格
        /// （视口外的点也点得到），`Hightlight` / `Premium image` 那两张图会溢出视口、压到窗体边上
        /// —— 与它下面那句自陈的原版 `RectMask2D` 不符。这**正是 A768② 那件事的可见后果**，
        /// 现已由「挂节点 + 父链解析」修掉（`MenuDraw.Hit` 的命中区也一起被截到框内 = 原版
        /// `RectMask2D.IsRaycastLocationValid` 那一面）。</para>
        /// <para>🔴 **2026-10-14（A797）就地订正（铁律 5）—— 本段原来记的那处「缺口」已经闭合。**
        /// 原文：「⚠️ **仍然如实标一处缺口**（报告 §九③，**本件没修**）：`MenuDraw.Text` / `MenuDraw.TextBox`
        /// **没有 `clip` 形参、也不走 `ViewportClip.Resolve`** ⇒ 视口里那条文字（`Army Name` / `Premium Text`）
        /// **不吃裁切**（容器滚到一半时，那一行字仍会溢出视口画出来）…… 要真修得动 `Shell/MenuDraw.cs`
        /// 那两个共用件（另立账）」。
        /// **错因 = 它写于 A781 之前，那一刻是真的**；**A781（2026-10-13）当天就把那两个共用件修了**
        /// ——「另立的那笔账」正是 A781 —— 但**没人回来销这一句**（铁律 5 那个形状）。
        /// **现在的事实**：`MenuDraw.Text` / `TextBox` 各带 `clip` / `clipSoftness` **两个可选形参**、
        /// 走**同一份** `ViewportClip.Resolve`（`Shell/MenuDraw.cs` 那两个入口里各一句 `var _st = …`），
        /// 并且 **A798（2026-10-14）起还多一道「整块在框外 ⇒ 连节点一起不建」的闸**。
        /// ⇒ 上面那两个类里文字（`Army Name`(`BuildContainer`) / `Premium Text`(`BuildContainer`)）的父链
        /// 经 `_content` 上行到 `Build()` 挂的那颗 `ViewportClip`（本文件那句 `Hang`）⇒ **它们已经吃裁切**。
        /// ⚠️ **别把这一句读成「本窗的文字全在视口里」**：`Army Info` 那一棵（`Build()` 里的 `BuildArmyInfo(root)`）
        /// 直接挂在**窗根**下、**不在**任何视口里 ⇒ 它不裁（照旧，那是对的）。</para></summary>
        void RebuildContainers()
        {
            if (_content == null) return;
            foreach (var c in _containers) if (c != null) MainMenuSubmenuWindow.DestroySafe(c.gameObject);
            _containers.Clear();
            if (_scroll != null)
            {
                // `Content` 的高度随容器数变 ⇒ 极值也要跟着改（否则滚不到底）。
                _scroll.ContentX1 = Abs(ViewportR).y1;
                _scroll.ContentX2 = Abs(ViewportR).y1 + ContentH(_offers.Length);
            }
            for (int i = 0; i < _offers.Length; i++)
            {
                // 🔴 **2026-10-13（A806）就地订正（铁律 5）**：`r` 原来是**相对根**的 `ContainerRect(i)`，
                // 而 `MenuDraw.Node/Rect/Hit` 的矩形实参是**绝对画布框**（`MenuDraw.Node → ApplyPxRect →
                // Local(parent,…)`）⇒ 容器整棵子树差一个根原点 `(167.175, 70.94)`（实测容器节点落在
                // `(324.495, 111.29)`、应该在 `(491.67, 182.23)`）。**与 `Build()` 里模板那一份
                // （`:526-527` 走 `Abs(ContainerR)`）对齐**；`Sub` 的基准同步改成 `Abs(ContainerR)` ——
                // **两半必须一起改**，只改一半会把根原点再加一次（见 `BuildContainer` 那段注释）。
                // ⚠️ 滚动那一档本来就是绝对档（`:606-607` 用的就是 `Abs(ViewportR).y1`）⇒ `Shift` 直接吃绝对框。
                var r = Abs(ContainerRect(i));
                var on = _scroll != null ? _scroll.Shift(r) : r;
                if (_scroll != null && !_scroll.Intersects(on)) continue;   // 整块在视口外 ⇒ 不建（同全壳口径）
                var cn = MenuDraw.Node(_content, i == 0 ? "Army Container" : "Army Container (" + i + ")", on);
                BuildContainer(cn, on, _offers[i]);
                int idx = i;
                MenuDraw.Hit(cn, "Hit", on, QHit, () => SelectArmy(idx));
                _containers.Add(cn);
            }
        }

        /// <summary>`Army Info` 那一棵（原版 `PuchasePremiumArmyInfo` 的 11 个节点）。</summary>
        void BuildArmyInfo(Transform root)
        {
            _armyInfo = MenuDraw.Node(root, "Army Info", Abs(ArmyInfoR));
            var bgTex = Tex(ArtInfoBg, "`Army Info/Background`");
            if (bgTex != null)
                MenuDraw.Nine(_armyInfo, bgTex, Abs(InfoBgR), new Vector4(18f, 18f, 18f, 18f), 69f, 63f,
                              QInfoBg, null, true, "Background");
            else MenuDraw.Node(_armyInfo, "Background", Abs(InfoBgR));
            // `Army Icon`：原版 `m_Sprite = <无图>`（运行期 `ArmyUtilities.GetArmyIcon`）⇒ 只建节点。
            MenuDraw.Node(_armyInfo, "Army Icon", Abs(ArmyIconR));
            _infoTitle = MenuDraw.TextBox(_armyInfo, Abs(DescTitleR), TxtInfoTitle, Color.white, "Description title",
                                          InfoTitleFont, InfoTitleMin, QInfoTitle, InfoTitleMax, InfoTitleBase);
            if (_infoTitle != null) MenuDraw.AlignLeft(_infoTitle, Abs(DescTitleR));
            _infoBody = MenuDraw.TextBox(_armyInfo, Abs(DescBodyR), TxtInfoBody, Color.white, "Description Content",
                                         InfoBodyFont, InfoBodyMin, QInfoText, InfoBodyMax, InfoBodyBase);
            if (_infoBody != null) MenuDraw.AlignLeft(_infoBody, Abs(DescBodyR));

            // `Price Display Button 2`（`PriceDisplayButton`）
            var pb = MenuDraw.Node(_armyInfo, "Price Display Button 2", Abs(PriceBtnR));
            var gb = MenuDraw.Node(pb, "Generic UI Button", Abs(PriceBtnR));
            var btnTex = Tex(ArtBtn, "价签钮底");
            GameObject btnNine = null;
            if (btnTex != null)
                btnNine = MenuDraw.Nine(gb, btnTex, Abs(PriceBtnR), new Vector4(333f, 96f, 333f, 96f), 410f, 124f,
                                        QPriceBtn, null, true, "Image",
                                        new Vector4(333f / 2f, 96f / 2f, 333f / 2f, 96f / 2f));
            // 备用文本（**出厂 act = F** ⇒ 新建出来就是关的）
            var pbt = MenuDraw.TextBox(gb, Abs(PriceBtnTxtR), "", Color.white, "Button Text",
                                       PriceBtnTxtFont, PriceBtnTxtMin, QPriceBtnText, PriceBtnTxtMax, PriceBtnTxtBase);
            if (pbt != null) pbt.gameObject.SetActive(false);
            // `Price Display`（HLG + `PriceDisplay`；那颗 icon 的 `localScale = 1.2` ⇒ 按**视觉框** 72.03² 摆，
            // 同 `BaseOfferPopup` 那条纪律）
            var pbox = MenuDraw.Node(gb, "Price Display", Abs(PriceBoxR));
            MenuDraw.Node(pbox, "icon", Abs(new PxRect(PriceIconR.x1 - 6f, PriceIconR.y1 - 6f,
                                                       PriceIconR.x2 + 6f, PriceIconR.y2 + 6f)));
            _priceText = MenuDraw.TextBox(pbox, Abs(PriceTextR), TxtPrice, Color.white, "text",
                                          PriceTextFont, PriceTextMin, QPriceText, PriceTextMax, PriceTextBase);
            // 换图那一跳走 `BindNine`（九宫格切成 9 张 ⇒ 只换中心那格 = 边框不跟着亮）。
            var priceHit = MenuDraw.Hit(gb, "Hit", Abs(PriceBtnR), QHit, () => PurchaseButtonClick());
            if (priceHit != null && btnNine != null)
            {
                var wb = priceHit.GetComponent<WindowButton>();
                if (wb != null) wb.BindNine(btnNine, ArtBtn, ArtBtnHover, ArtBtnPressed);
                else Debug.LogWarning("[Premium] 价签钮的命中区上没有 `WindowButton` ⇒ 悬停/按下**不换图**。");
            }
            _priceBtn = pb.gameObject;

            // `Purchased Text`
            _purchased = MenuDraw.TextBox(_armyInfo, Abs(PurchasedR), TxtPurchased, PremTextTint, "Purchased Text",
                                          PurchasedFont, PurchasedMin, QPurchased, PurchasedMax, PurchasedBase);
        }

        /// <summary>一个 `Army Container`（`PuchasePremiumArmyContainer` 的 6 个节点）。
        /// `o == null` ⇒ 模板那一份（用实例化的出厂态）。</summary>
        void BuildContainer(Transform cn, PxRect r, ArmyOffer? o)
        {
            // 子件的框是**相对容器**的（容器的框一变，子件跟着走）—— 用同一份「容器内偏移」。
            // 🔴 **2026-10-13（A806）就地订正（铁律 5）**：基准 `ContainerR` 原来是**相对根**的，而
            // `abs`（形参）是**绝对**框（调用点一律 `Sub(Abs(…))`）⇒ 两者相减正好把根原点**加回去**、
            // 容器节点在相对档而它的子件在绝对档（A780 那条「混档」）。基准改用 `Abs(ContainerR)`
            // —— ⚠️ **与 `RebuildContainers` 那一半同生共死**：那边搬绝对档、这边换基准，缺一半就再加一次根原点。
            PxRect Sub(PxRect abs) { return new PxRect(r.x1 + (abs.x1 - Abs(ContainerR).x1), r.y1 + (abs.y1 - Abs(ContainerR).y1),
                                                        r.x1 + (abs.x2 - Abs(ContainerR).x1), r.y1 + (abs.y2 - Abs(ContainerR).y1)); }
            // `Army Image Background`：原版 `m_Sprite = <无图>`（运行期灌阵营底图）⇒ 只建节点。
            MenuDraw.Node(cn, "Army Image Background", Sub(Abs(ContainerR)));
            var nm = o.HasValue && !string.IsNullOrEmpty(o.Value.ArmyName) ? o.Value.ArmyName : TxtArmyName;
            // 🔴 **2026-10-16（A799 · 生产 1/9）：这一格【会新裁】= 目的，⛔ 别「修」。**
            //   父链：`cn` ← `_content`（`:517` `Node(vp, "Content", …)`）← `vp`（`:513`）← `vpVc`
            //   （`:512` `ViewportClip.Hang(sv, "Viewport", …)`）；容器就是 `:633` 那句 `BuildContainer` 建的。
            //   ⇒ 本处没传 `clip`（恒 `null`）⇒ A781 起 `MenuDraw.TextBox` 的末句
            //   （`if (_st.RenderClip.HasValue) ClipText(lb, clip, clipSoftness);`）会自己解析到那颗节点
            //   ⇒ 压在视口边上的「Army Name」**第一次**被夹到视口沿（此前这一段字一个顶点都不裁）。
            //   ✅ 为什么可以：判据 = **原版 `RectMask2D` 对文字与图片一视同仁**（同容器的 `Nine`/`Rect`
            //   早在裁）⇒ 这一刀正是 A781 要的；返回值有守卫（下一句 `if (lb != null)`）。
            //   ⛔ 别在这儿补 `ClipText`（同框第二刀 = A821 那 7 个包装器的形状，诊断计数会虚高）。
            //   判据全文 / 全量 199 处 → `资料/普查产出_1015/R2_A799全量表.md` §一① #1。
            var lb = MenuDraw.TextBox(cn, Sub(Abs(ContNameR)), nm, Color.white, "Army Name",
                                      ArmyNameFont, 0f, QContainerName, ArmyNameFont, ArmyNameBase);
            if (lb != null) MenuDraw.AlignLeft(lb, Sub(Abs(ContNameR)));
            var hiTex = Tex(ArtHightlight, "`Hightlight` 的选中底");
            if (hiTex != null)
                MenuDraw.Nine(cn, hiTex, Sub(Abs(ContHiR)), new Vector4(31f, 28f, 31f, 28f), 69f, 63f,
                              QContainerHi, HiTint, true, "Hightlight",
                              new Vector4(31f / 2f, 28f / 2f, 31f / 2f, 28f / 2f));
            else MenuDraw.Node(cn, "Hightlight", Sub(Abs(ContHiR)));
            var pu = MenuDraw.Node(cn, "Premium Unlocked", Sub(Abs(PremUnlockR)));
            var pi = MenuDraw.Node(pu, "Premium image", Sub(Abs(PremIconR)));
            var ptex = Tex(ArtPremIcon, "`Premium Unlocked/Premium image`");
            if (ptex != null) MenuDraw.Rect(pi, ptex, Sub(Abs(PremIconR)), "Image", QPremiumIcon);
            // 🔴 **2026-10-16（A799 · 生产 2/9）：这一格【会新裁】= 目的，⛔ 别「修」。**
            //   父链：`pi` ← `pu` ← `cn` ← `_content`（`:517`）← `vp`（`:513`）← `vpVc`（`:512` 那颗 `ViewportClip`）。
            //   ⇒ `clip` 恒 `null` ⇒ A781 起由那颗节点接管 ⇒ 压在视口边上的「Premium Text」第一次被夹到视口沿。
            //   ✅ 为什么可以：同上一处（`:707`）—— 原版 `RectMask2D` 文字/图片一视同仁，**新裁才是对的**；
            //   且本句**丢弃返回值**（不接 `Label`）⇒ A798 那条「整块在框外 ⇒ 返回 `null`」在这句上无副作用。
            //   ⛔ 别补 `ClipText`。判据全文 → `资料/普查产出_1015/R2_A799全量表.md` §一① #2。
            MenuDraw.TextBox(pi, Sub(Abs(PremTextR)), TxtPremium, PremTextTint, "Premium Text",
                             PremiumFont, PremiumMin, QPremiumText, PremiumMax, PremiumBase);
            // 原版 `premiumObjects` 的开关（`PuchasePremiumArmyContainer.Initialize`）
            if (o.HasValue) pu.gameObject.SetActive(o.Value.PremiumUnlocked);
        }

        /// <summary>原版 `PurchaseButtonClick` → `TryPurchase(data)` → IAP 那条链。**本地没有 IAP**
        /// ⇒ 只出声、不改状态（红线）。</summary>
        void PurchaseButtonClick()
        {
            Debug.Log("[Premium] 点了价签钮（原版 `PuchasePremiumArmyInfo.PurchaseButtonClick` → "
                    + "`PurchasePremiumWindow.TryPurchase(data)` → `PremiumArmyPurchaseData.TryPurchase()` 那条 IAP 链）。"
                    + "**本地没有 IAP，也没有报价** ⇒ 只出声、状态没有变。");
        }

        // ============================================================ 开场动画（`OnEnable` 那一段）

        void StartPop()
        {
            if (_cg != null) { _cg.alpha = 0f; _cg.interactable = false; }
            transform.localScale = new Vector3(PopFromScale, PopFromScale, PopFromScale);
            _popT = 0f;
        }

        void Update()
        {
            if (_popT < 0f) return;
            _popT += Time.deltaTime;
            float k = Mathf.Clamp01(_popT / PopDuration);
            k = 1f - (1f - k) * (1f - k);                 // DOTween 默认缓动 `Ease.OutQuad`
            if (_cg != null) _cg.alpha = k;
            float s = Mathf.Lerp(PopFromScale, 1f, k);
            transform.localScale = new Vector3(s, s, s);
            if (_popT >= PopDuration)                      // = 原版 `OnComplete`（那一跳把 `interactable` 打开）
            {
                _popT = -1f;
                if (_cg != null) { _cg.alpha = 1f; _cg.interactable = true; }
                transform.localScale = Vector3.one;
            }
        }

        /// <summary>自检用：动画跑完了吗（`false` = 还在淡入）。</summary>
        public bool PopDone { get { return _popT < 0f; } }
        /// <summary>自检用：把动画**跳到终点**（批处理没有帧循环 ⇒ 只能手动推）。
        /// ⚠️ **别在断言里拿它当「动画做过了」的证据** —— 它证明的只是「终点那一档对不对」。</summary>
        public void FinishPopForTest()
        {
            _popT = PopDuration;
            Update();
        }

        // ============================================================ 取图（取不到必须出声）
        readonly System.Collections.Generic.List<string> _missArt = new System.Collections.Generic.List<string>();
        /// <summary>本窗**取不到的图**（自检读口）。</summary>
        public System.Collections.Generic.List<string> MissingArt { get { return _missArt; } }

        Texture2D Tex(string art, string what)
        {
            var t = CardArt.MenuUi(art);
            if (t == null && !_missArt.Contains(art))
            {
                _missArt.Add(art);
                Debug.LogWarning("[Premium] 图取不到：`" + art + "`（" + what + "）⇒ **这一件没画**"
                               + "（`MenuDraw.Rect/Nine` 对 `tex == null` 是静默返回 null）。"
                               + "导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            }
            return t;
        }

        // ============================================================ 读口（自检专用）
        /// <summary>`Content` 容器（自检读口）。</summary>
        public Transform ContentNode { get { return _content; } }
        /// <summary>`Army Container` 模板（**应当是关着的**）。</summary>
        public Transform ContainerTemplate { get { return _containerTemplate; } }
        /// <summary>真的实例化出来的容器（自检读口）。</summary>
        public System.Collections.Generic.List<Transform> Containers { get { return _containers; } }
        /// <summary>`Army Info` 那一棵（自检读口）。</summary>
        public Transform ArmyInfoNode { get { return _armyInfo; } }
        public Label TitleLabel { get { return _title; } }
        public Label SubTitleLabel { get { return _subTitle; } }
        public Label InfoTitleLabel { get { return _infoTitle; } }
        public Label InfoBodyLabel { get { return _infoBody; } }
        public GameObject PriceButton { get { return _priceBtn; } }
        public Label PriceTextLabel { get { return _priceText; } }
        public Label PurchasedLabel { get { return _purchased; } }

        /// <summary>自检用：把当前状态摊开。</summary>
        public string DebugDump()
        {
            return "offers=" + _offers.Length + " containers=" + _containers.Count
                 + " templateActive=" + (_containerTemplate != null && _containerTemplate.gameObject.activeSelf)
                 + " priceBtnActive=" + (_priceBtn != null && _priceBtn.activeSelf)
                 + " popDone=" + PopDone
                 + " missingArt=" + _missArt.Count
                 + (MissingArt.Count > 0 ? "（" + string.Join("、", MissingArt.ToArray()) + "）" : "");
        }

        // ============================================================ 对账表（**纯数据 · 不参与渲染**）
        /// <summary>一个节点的**冻结事实**（路径 / **相对根左上角的框** / 出厂 `activeSelf`）——
        /// 逐格对着 `python 工具/menu_dump.py bundle_menus_assets_all "Purchase Premium Window" --depth 12 --relative --md`
        /// 的现读值抄，供 `_tmp_view/wl2/check_table.py` 逐格对账。
        /// <para>⚠️ **两条如实标注**（都在下面那几行旁边的注释里）：
        ///   ① `Price Display/text` 这一行照抄 dump 的 **0 宽**（那是真读数），而我们**建出来**的那一格
        ///      是 116.59 宽（见 `PriceTextR` 那条）⇒ **这一行的框与建出来的不一致、是故意的**；
        ///   ② `Scrollbar Collection/Sliding Area/Handle` **不在表里** —— 原版那一格的
        ///      `m_AnchoredPosition.y` 是 **NaN**（dump 印 `nan`），**没有可对账的值**（⛔ 不编号码顶替）。
        ///       ⇒ 表里 **33 条**，真实节点 **34 个**（差的那一个就是它）。</para></summary>
        public struct ReconRow
        {
            public string Path;
            public PxRect R;
            public bool On;
            public ReconRow(string p, PxRect r, bool on = true) { Path = p; R = r; On = on; }
        }

        /// <summary>逐节点对账表（33 条，见上面那两条标注）。</summary>
        public static readonly ReconRow[] Recon =
        {
            new ReconRow("Purchase Premium Window",                        new PxRect(0f,        0f,       1585.65f, 938.12f)),
            new ReconRow("Menu Dark Background",                           new PxRect(-1494.48f, -817.12f, 3080.13f, 1755.24f)),
            new ReconRow("Generic Window Red Background Big",              new PxRect(15.09f,   -15.30f,  1605.76f, 973.95f)),
            new ReconRow("Title",                                          new PxRect(756.33f,   57.49f,   1497.67f, 141.71f)),
            new ReconRow("SubTitle",                                       new PxRect(651.02f,   213.19f,  1280.98f, 269.01f), false),
            new ReconRow("Premium image",                                  new PxRect(651.17f,   47.52f,   754.08f,  151.80f)),
            new ReconRow("Army Info",                                      new PxRect(642.75f,   183.46f,  1547.17f, 871.66f)),
            new ReconRow("Army Info/Background",                           new PxRect(642.53f,   183.89f,  1547.37f, 871.22f)),
            new ReconRow("Army Info/Army Icon",                            new PxRect(655.13f,   190.38f,  754.08f,  289.34f)),
            new ReconRow("Army Info/Description title",                    new PxRect(754.00f,   219.12f,  1522.10f, 274.94f)),
            new ReconRow("Army Info/Description Content",                  new PxRect(679.90f,   310.96f,  1521.40f, 705.56f)),
            new ReconRow("Army Info/Price Display Button 2",               new PxRect(930.60f,   760.07f,  1259.31f, 833.45f)),
            new ReconRow("Army Info/Price Display Button 2/Generic UI Button",
                                                                           new PxRect(930.60f,   760.07f,  1259.31f, 833.45f)),
            new ReconRow("Army Info/Price Display Button 2/Generic UI Button/Button Text",
                                                                           new PxRect(943.60f,   767.32f,  1246.31f, 826.20f), false),
            new ReconRow("Army Info/Price Display Button 2/Generic UI Button/Price Display",
                                                                           new PxRect(947.36f,   766.72f,  1240.57f, 826.74f)),
            new ReconRow("Army Info/Price Display Button 2/Generic UI Button/Price Display/icon",
                                                                           new PxRect(1063.96f,  766.72f,  1123.98f, 826.74f)),
            // ① 这一行照抄 dump 的 0 宽（真读数）；我们**建出来**的是 116.59 宽（见 `PriceTextR`）。
            new ReconRow("Army Info/Price Display Button 2/Generic UI Button/Price Display/text",
                                                                           new PxRect(1093.97f,  766.72f,  1093.97f, 826.74f)),
            new ReconRow("Army Info/Purchased Text",                       new PxRect(779.98f,   753.82f,  1409.94f, 839.10f)),
            new ReconRow("Scroll View",                                    new PxRect(32.04f,    4.63f,    616.95f,  923.07f)),
            new ReconRow("Scroll View/Viewport",                           new PxRect(40.54f,    4.63f,    608.45f,  923.07f)),
            new ReconRow("Scroll View/Viewport/Content",                   new PxRect(40.54f,    4.63f,    608.44f,  192.95f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container",    new PxRect(58.88f,    29.63f,   590.11f,  192.95f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Army Image Background",
                                                                           new PxRect(58.88f,    29.63f,   590.11f,  192.95f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Army Name",
                                                                           new PxRect(88.72f,    51.89f,   459.93f,  101.89f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Hightlight",
                                                                           new PxRect(58.88f,    29.63f,   590.11f,  192.95f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Premium Unlocked",
                                                                           new PxRect(65.89f,    92.95f,   165.89f,  192.95f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Premium Unlocked/Premium image",
                                                                           new PxRect(74.98f,    98.44f,   164.00f,  187.46f)),
            new ReconRow("Scroll View/Viewport/Content/Army Container/Premium Unlocked/Premium image/Premium Text",
                                                                           new PxRect(172.39f,   115.15f,  484.10f,  165.15f)),
            new ReconRow("Scroll View/Scrollbar Collection",                new PxRect(26.92f,    4.63f,    46.92f,   893.01f), false),
            new ReconRow("Scroll View/Scrollbar Collection/Sliding Area",   new PxRect(36.92f,    14.63f,   36.92f,   883.01f)),
            // ② `…/Sliding Area/Handle` **不在表里** —— 原版那一格的 `m_AnchoredPosition.y` 是 NaN。
            new ReconRow("Generic Close Button Orange",                    new PxRect(1547.16f, -34.24f,  1621.55f, 41.36f)),
            new ReconRow("Generic Close Button Orange/Background",         new PxRect(1555.31f, -26.26f,  1612.18f, 31.86f)),
            new ReconRow("Generic Close Button Orange/Icon",               new PxRect(1555.31f, -26.26f,  1612.18f, 31.86f)),
        };
    }
}
