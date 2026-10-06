// BaseOfferPopup.cs —— A251-L1：原版 `BaseOfferPopup` 那一族（**21 个 prefab = 1 母版 + 20 变体**）
//
// ============================ 出处（唯一正本）============================
//  · 普查（本件的切块来源）：`资料/普查产出_1013/A表现核_块5.md` §A251 —— 逐窗准名字 / prefab / bundle / 节点数表。
//  · 逐节点几何（**全部现读**，本文件每一格都出自这条命令，**没有一格是推的**）：
//      python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<prefab 名>" --depth 12 --relative --md
//    （`--relative` = **相对根左上角**；21 份逐个跑过。⚠️ 不带 `--relative` 时那几个根带偏移的
//      （`Premium_*` 那 5 份，实测根在 `(-13.40,-16.90)`）会**整棵平移** —— 别拿绝对值当设计值。）
//  · 窗口字段（**21 份逐份实读** `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_*.json`，
//      筛法 = 同时含 `availableCount` + `previewButton` + `badgeText` ⇒ 命中 21 份，与 prefab 数一致）：
//      `type = 1 (Popup)` · `windowsPlacement = **15 (Popup)**` · `closeOnESC = 1` ·
//      `updateNavPanel = 0` · `useDefaultCloseSoundIfNull = 1` · `extraScaleSmallScreen = **1.2**`（21/21 同值）。
//    （`extraScaleSmallScreen = 1.2` 与 `Editor/SettingsScene.cs:488` 的旁证一致 —— 那一行把它列进
//      「1.2 家族：`BaseOfferPopup`×21」。）
//  · 出厂显隐（**21 份逐个核过**，只有两处不是 21/21 同值，见 `Variant.ArtworkOn` / `Variant.ForegroundOn`）：
//      `Menu Dark Background` / `window` / `Generic Window Red Background Big` / `Generic Close Button Orange` /
//      `background` / `Text` / `Title` / `Category` / `Descripton` / `Available Counter` / `Timer` /
//      `Offer Badge` / `Purchase buttons` / `Preview` … **全是 T (21/21)**；
//      **价签里那颗 `Generic UI Button/Button Text` 是 `F` (21/21)**（见 `Q` 段那段注释）；
//      **价签的 `Price Display/icon` 的 sprite 是 `<无图>` (21/21)**。
//  · 行为（第一权威 = 反编译，`d:/2/tools/decomp_full/BaseOfferPopup__*.c` 共 17 个方法体，**逐份读过**）：
//      `Open` / `SetEventData` / `Refresh` / `SetAvailableText` / `SetBadgeText` / `SetTimer` /
//      `Close` / `ProcessPurchase` / `OverrideVisualRewards` / `OnEnable` / `OnDisable` / `OnOfferTimerEnded`
//  · 字段偏移（`d:/2/tools/il2cpp_out/dump.cs:90840-90900` 的 `BaseOfferPopup` 一节）——
//      下面「哪个偏移是哪一栏」就是照它认的：
//      `nameText 0x70` · `subTitleText 0x78` · `descriptionText 0x80` · `foregroundImage 0x88` ·
//      `closeButton 0x90` · `backgroundCloseButton 0x98` · `priceDisplayButton 0xA0` · `timer 0xA8` ·
//      `badgeText 0xB0` · `availableCount 0xB8` · `previewButton 0xC0` · `webShopOpenButton 0xC8` ·
//      `previousPriceGO 0xD0` · `previousPrice 0xD8`
//  · 🔴 **`Close()` 我们只做了它的第 1 句**（老账 A123 / A634，2026-10-13 **本处现读复核**）——
//      `BaseOfferPopup__Close.c` 逐句 5 步：① `GameWindow.Close(this, 0)` ②
//      `ActionScheduler.Release(Instance, this[0x1c])` ③ `LiveOpsAssetUtility.Unload<int>(…, **2**, …)`
//      （= 卸载报价那条链的**资源组 2**）④ 从 `closeButton`(0x90) 与 `backgroundCloseButton`(0x98) 的
//      `UnityEvent` 上各 `RemoveListener` 一次（委托目标 = 两张表**同一个**虚表槽 0x1c0）⑤
//      `GeneralOfferPopupDrawerBase.ReleaseAssets()`（抽屉若在场）。
//      ⇒ **②③⑤ 我们这一层【没有对应物】**：没有调度器/资源组/按窗卸载那一层（美术走 `CardArt` 的
//      `Resources.Load` 全局缓存，`Core/CardArt.cs:20`）—— ⛔ **不硬造**，如实记在这里。
//      **④ 也【没有对应物】**：全仓 `Shell/` **零 `AddListener`**（我们的动作是建树时交给
//      `MenuDraw.Hit` / `ShadeHit` 的闭包，不是 `UnityEvent`），而 `Show()` 在「变体与内容都没变」时
//      **不重建**（照 `GameWindow.TryOpen` 的 `Open` 支）⇒ **不会累积监听** ⇒ 没有要摘的东西。
//      ⚠️ 第 1 句我们做的是 `WindowsManager.GameWindow.Close()`（`Shell/WindowsManager.cs:590`：
//      state + 摘表 + 物体 inactive）。⛔ 别把②③④⑤ 当成「漏抄了」——它们是**宿主机制不同**。
//  · 抽屉那一族（**不重写**）：`Shell/ItemDrawer.cs`（`Draw` / `SetPremium` / `SetEphemeral` / `SetConverted`）
//      与 `Shell/OfferContainer.cs`（`SlotTypes` / `DrawerClassOf` / `Content` / `ItemDrawerStyle` 用法）。
//  · 兄弟窗（同一棵树形，可对照）：`Shell/BoosterInfoPopup.cs`（`Booster Info Popup` —— 它的
//      `window` / 窗底 / 关闭钮 / `Artwork` / `Title` / `Category` / `Descripton` / `Purchase buttons`
//      **与本窗母版的矩形几乎逐位相同**，是这一族的近亲；本窗照它的建筑风格写）。
//
// ============================ 🔴 我们这条链【没有数据源】（与 `OfferContainer` 同一条口径）============================
//  原版这一族是**服务端 LiveOps 报价**的载体：`BaseOfferPopup.Open` 第一句就是
//  `SetEventData(Data-as-IShopOffer)`，`Data` 由商店/容器那一族在报价到达时塞进来。
//  本地**一个报价数据都没有**（同 `OfferContainer.cs` 文件头那条）⇒ 本窗：
//    · **骨架照原版逐节点建**（37 个母版节点 + 每份变体自己的抽屉槽）；
//    · **数据驱动的栏位**走**原版自己的「没有数据」那一支**（下一条），⛔ **不编假内容**；
//    · **不编假入口**（不挂到任何菜单上）；`Create` 由调用方开，点了**如实出声**。
//
// ============================ 🔴 「没有数据时长什么样」—— 判据在【原版自己】那里 ============================
//  这是本窗能既「照原版」又「不编内容」的关键：**原版的 no-data 分支就是「把这几栏关掉」**。
//  逐条列（每条都给了方法体出处）：
//   · **`Timer` 整件关掉** —— `SetTimer.c`：`LiveOpsUtility.IsTimedEvent(eventData, out endTime)` 为假
//     ⇒ `timer.gameObject.SetActive(false)`（**不是**留在那儿显示旧字）。没有报价 ⇒ 不是计时活动 ⇒ 关。
//     ⚠️ 顺带一条**能看出「整件关」是对的**：`Timer` 是 `HorizontalLayoutGroup`，**图标的位置由
//     「倒计时文字有多宽」决定**（母版 dump：`Timer Text` 宽量出来是 **0.00**）⇒ 单画一个图标
//     只能自己编一个位置（同 `SkirmishEventWindow.BuildTimer` 那条判据）。
//   · **`Offer Badge` 整件关掉** —— `SetBadgeText.c`（= `SetEventData` 尾段同一段）：
//     `GetLabel(offer, 3)` 为空 ⇒ 把它那颗 TMP 的**父件** `SetActive(false)`。
//     `badgeText`（0xB0）那颗 TMP 的父件就是 **`Offer Badge`** ⇒ 关的是它。
//   · **`Available Counter` 关掉** —— `SetAvailableText.c`：`availableCount`（0xB8）那颗 TMP 的
//     **自己** `SetActive(param_2)`，`param_2` = `Refresh` 从报价里算出来的「有没有可买的东西」。
//     没有报价 ⇒ 关。⚠️ 它是**一颗 TMP 自己关**（父件 `Text` 不动）⇒ 我们直接关那颗 TMP 的节点。
//   · **`foreground` / `Artwork` 的显隐 = prefab 自带** —— `SetEventData` 里那只在
//     `offer != null` 时才 `SetActive(true)` + `set_sprite(LoadAsset(offer, 2))`；**没有报价时它一个字都不写**
//     ⇒ 出厂态生效。实测 21 份里这两件的出厂 `act` **各有 5 份是 `F`、而且不是同一 5 份**
//     （见 `Variants[]` 的那两格）—— 这正是「状态 → 参数」那一栏要记的东西（铁律 5·c）。
//   · **`Button Text`（价签里那颗）出厂就是 INACT** —— 21/21 实读 `act=**F**`、字串出厂是**空串**。
//   · **`previousPriceGO`（= `Offer Price Discount Badge`）出厂 ACTIVE** —— `Refresh.c` 里那一句
//     `SetActive(previousPriceGO, offerBool)` 只在 `offer != null` 的支里跑到；没有报价 ⇒ 出厂态（**开**）生效。
//   · **`WebShop Button` 出厂 ACTIVE** —— 原版由 `Open()` 尾段
//     `webShopOpenButton.OfferAllowsShow(offer 的那一位)` 决定；没有报价 ⇒ 那一跳不跑 ⇒ 出厂态生效。
//     ⚠️ 我们**不做真实经济**（用户 2026-09-17 边界②）⇒ 点它**只出声、不跳转**（同 `BoosterInfoPopup`）。
//
// ============================ 🔴 三个「我们挑的 / 缺口」如实标注（铁律 3）============================
//   ① **旋转的符号方向**：带 `m_LocalRotation`（绕 z）的件按 `Quaternion.Euler(0,0,rot)` 原样落地。
//      **推导（不是猜）**：原版 `RectTransform` 的局部系是 Unity 标准的 y 向上、绕 z **逆时针为正**；
//      本工程 `MenuDraw.Node` 建出来的节点也没有负 `localScale`（`MenuDraw.SetPxSize` 只写
//      `sizeDelta` 与 `anchorMin/Max/pivot`，**一个字都不动 `localScale`**），局部系同样是
//      Unity 标准的 y 向上 ⇒ **两边同一个 `rot` 就是同一个朝向**。（`LayoutSpace.FromPixel` 那个 y 取反
//      只作用于**位置**，不作用于节点自己的坐标系。）
//      ⚠️ **本件没跑 Unity ⇒ 没有实拍复核**，结论**只到「静态推导成立」这一层**（真 Play 时重点看这一条，
//      已记进报告 §八）。
//   ② **两张图工程里没有** —— `40k_shop_popup_info_bg`（`Artwork/background` 的 prefab sprite）与
//      `40k_OfferBadge`：`Resources/Art/{ui_menu,ui_deck,ui}/` 三个目录逐个 `ls` 过，**两张都 MISSING**
//      （`40k_OfferBadge.png` 只躺在 `Art/原版/0_mainmenu/` 下、**没进 `Resources/`**）。
//      ⇒ 那两处**只建节点、不画**（节点照建，保证树的形状对），并记进 `MissingArt` + **出声**。
//      ⛔ **别拿别的图顶上**（红线：不许静默用别的东西冒充）。
//   ③ **`Descripton` 那一栏恒用 prefab 自带的出厂文本** —— 原版由 `GetLabel(offer, 1)` 运行期覆盖；
//      本地没有报价 ⇒ 用出厂原文（同 `BoosterInfoPopup.DescFor` 那条先例：**不自己编词**）。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BaseOfferPopup`（`bundle_menus_assets_all` · `Base Offer Popup` 那一族，**21 个 prefab**）。
    /// 逐节点几何 / 窗口字段 / 行为判据 → 文件头。**入口是我们定的**（原版那条链没有；见文件头）。</summary>
    public class BaseOfferPopup : GameWindow
    {
        // ============================================================ 队列档
        // 🔴 **选档的依据（现读，不是挑的）**：全工程这一段是**空档** ——
        //   `CampaignTab` 用到 3064 · `BoosterInfoPopup` = 3070..3079 · `MissionRerollPopup` = 3080..3088 ·
        //   练习窗从 **3100** 起 · tooltip 是 **3199+**。
        //   ⇒ **3089..3099 这一段是空的**，本窗整段占用（11 个号，一个不多一个不少）。
        public const int QBase = 3089;
        /// <summary>压暗整屏（`Menu Dark Background`）—— 也是「点窗外关窗」那个命中区的档（见 `QShadeHit`）。</summary>
        public const int QShade = QBase;         // 3089
        /// <summary>窗内命中区的最低档（关闭钮 / `Preview` / 价签 / `WebShop`）。</summary>
        public const int QHit = QBase + 10;      // 3099
        const int QBg = QBase + 1;               // 3090 窗底九宫（`UI_Deck_Information_Back`）
        const int QClose = QBase + 2;            // 3091 关闭钮（圆底 / `Background` / `Icon`）
        const int QArt = QBase + 3;              // 3092 立绘（`Artwork` 两件 + `Preview`）
        const int QDrawer = QBase + 4;           // 3093 抽屉（`ItemDrawer` 的底板 / 图层）
        const int QDrawerDeco = QBase + 5;       // 3094 抽屉的装饰层（premium/converted/ephemeral 三跳）
        const int QText = QBase + 6;             // 3095 右半边文字（`Title`/`Category`/`Descripton`/`Available`）
        const int QBadge = QBase + 7;            // 3096 `Offer Badge` 的底
        const int QBadgeText = QBase + 8;        // 3097 `Offer Badge` 的字
        const int QBtn = QBase + 9;              // 3098 底部三颗钮的底与图标 + 两颗钮上的字

        /// <summary>「点窗外关窗」那颗命中区的档 = **压暗层自己那一档**（不是 `QHit`）。
        /// 🔴 判据 = `CLAUDE.md` §三「分层要用渲染队列，不能用 z」：`ImageQuad` 的世界 z 恒 0
        /// ⇒ 同档时谁吃到命中退化成 `FindObjectsByType` 的枚举顺序（`BoosterInfoPopup` 那一扇
        /// 实测就是这么丢掉 `Tooltip`/价签/`WebShop` 三个命中区的）。
        /// 走公共件 `MenuDraw.ShadeHit`（它会当场核「压暗档 < 内容档」并告警）。</summary>
        const int QShadeHit = QShade;

        // ============================================================ 出厂文本（prefab 原文，**不自己编词**）
        /// <summary>`Title` 的出厂文本（原版由 `GetLabel(offer, 0)` 覆盖）。</summary>
        public const string DefTitleText = "Necron Sautekh";
        /// <summary>`Category` 的出厂文本（原版由 `GetLabel(offer, 2)` 覆盖）。</summary>
        public const string DefCategoryText = "Legendary Booster Pack";
        /// <summary>`Descripton` 的出厂文本（原版由 `GetLabel(offer, 1)` 覆盖）。⚠️ 它点名 `Sautekh` —— 见文件头 ③。</summary>
        public const string DefDescText =
            "Contains 5 cards for the Sautekh army.\n\nAt least one of the cards is guaranteed to be Rare or better" +
            "\n\nCheck FAQ for more information on Drop Rates.";
        /// <summary>`Available Counter` 的出厂文本（原版由 `SetAvailableText` 拼）。</summary>
        public const string DefAvailText = "Available: 1/5";
        /// <summary>`Offer Badge/Text (TMP)` 的出厂文本（原版由 `GetLabel(offer, 3)` 覆盖）。</summary>
        public const string DefBadgeText = "+60% value";
        /// <summary>`Timer/Timer Text` 的出厂文本（原版是 liveop 事件的结束时间）。</summary>
        public const string DefTimerText = "23h 34m";
        /// <summary>`Purchase buttons/Price Display/…/text` 的出厂文本（原版是格式化后的价钱）。</summary>
        public const string DefPriceText = "300,00";
        /// <summary>`Offer Price Discount Badge` 两行的出厂文本。</summary>
        public const string DefPrevPriceTitle = "Previous Price";
        public const string DefPrevPriceValue = "$19.99";
        /// <summary>`WebShop Button/Button Text` 的出厂文本。</summary>
        public const string DefWebShopText = "Save More!";
        /// <summary>`Preview/Button Text` 的出厂文本。</summary>
        public const string DefPreviewText = "Preview";

        /// <summary>`Offer Price Discount Badge` 的 `m_LocalRotation`（绕 z · 度）。
        /// **逐份实读：21/21 都是 17.17°**（含没有抽屉的母版那两份 —— 它在 `Purchase buttons` 里，
        /// 不是抽屉槽，所以母版也有）。见文件头 ① 那条推导。</summary>
        public const float DiscountRotDeg = 17.17f;

        // ============================================================ 图名（工程里导入后的名字；空格→下划线）
        const string ArtPaneBg = "UI_Deck_Information_Back";
        const string ArtCloseBase = "UI_Button_Round_background";
        const string ArtCloseYellow = "40k_general_bt_yellow";
        const string ArtCloseYellowHi = "40k_general_bt_yellow_hover";
        const string ArtCloseIcon = "40k_general_bt_yellow_close";
        const string ArtArtworkBg = "40k_shop_popup_info_bg";          // 🔴 **没进 Resources/** —— 见文件头 ②
        const string ArtOfferBadge = "40k_OfferBadge";                  // 🔴 **没进 Resources/** —— 见文件头 ②
        const string ArtBtn = "40K_button";
        const string ArtBtnHi = "40K_button_hover";
        const string ArtGlow = "OctagonUI_Filled_Fade_SDF";             // 原版名 `OctagonUI Filled Fade SDF`
        const string ArtDiscountIcon = "40K_Icon_Discount_Gold";
        const string ArtClock = "WF_icon_clock";

        // 九宫格 / 贴图尺寸（**逐颗实读 prefab 的 `m_Border` 与 sprite 原尺寸**）
        static readonly Vector4 PaneBorder = new Vector4(42f, 363f, 655f, 81f);
        const float PaneTexW = 1100f, PaneTexH = 701f;
        static readonly Vector4 BadgeBorder = new Vector4(162f, 0f, 162f, 0f);
        const float BadgeTexW = 324f, BadgeTexH = 87f;
        static readonly Vector4 BtnBorder = new Vector4(234f, 46f, 234f, 46f);
        const float BtnTexW = 489f, BtnTexH = 107f;
        static readonly Vector4 GlowBorder = new Vector4(52f, 52f, 52f, 52f);
        const float GlowTexW = 128f, GlowTexH = 128f;

        // 颜色（**逐颗实读 `Image.m_Color`**）
        static readonly Color DarkTint = new Color(0f, 0f, 0f, 0.773f);          // `Menu Dark Background`
        static readonly Color BadgeTint = new Color(0.651f, 0f, 0f, 1f);         // `Offer Badge`
        static readonly Color PriceTint = new Color(0.902f, 0.637f, 0.18f, 1f);  // 价签的 `40K_button`
        static readonly Color WebTint = new Color(0.333f, 0.878f, 0.336f, 1f);   // `WebShop Button Image`
        static readonly Color PreviewTint = new Color(0.902f, 0.18f, 0.19f, 1f); // `Preview` 的 `40K_button`
        static readonly Color GlowTint = new Color(1f, 1f, 1f, 0.8f);            // `Highlight`

        // ============================================================ 几何：两档（**逐份实读后只有这两档**）
        //
        // 🔴 **21 份按 `window` 的矩形分成两档**（逐个 `--relative` 现读，没有第三档）：
        //   · `GeoSmall` = **1128.55×663.30**（x1 395.72）—— **16 份**
        //   · `GeoBig`   = **1426.11×720.84**（x1 246.94）—— **5 份**（`Variant 2 Currencies` ·
        //     `Variant Booster + 2 Currencies` · `Variant Premium_Booster_avatar_cardback_title{,resource}` ·
        //     `Variant Premium_Resource`）
        //   ⚠️ **其余节点不是「跟着 window 平移」**（逐条核过）：同一档内 `Purchase buttons` 子树在
        //   `GeoBig` 里 **恰好 = `GeoSmall` 平移 (+144, 0)**（六个节点逐个对上 —— `Price Display` /
        //   `Generic UI Button` / `Offer Price Discount Badge` / `Discount Title` / `Discount Price` /
        //   `WebShop Button` 及其三件 ⇒ Δx **恒 +144.00**、Δy **恒 0**）；但 `Available Counter` /
        //   `Offer Badge` 是 Δ(−146.50, −28.77)、`Timer` 是 Δ(+152.01, −2.02)、`Preview` 是 Δ(−131, +24)
        //   —— **各有各的锚点** ⇒ 只能**逐档存实读值**（别想着用一个平移量糊过去）。
        //
        // 🔴 **两处「量出来的宽是 0.00」是 dump 的失真、不是原版值**（本件唯一两处**我们算的**）：
        //   `GeoBig` 的 `Price Display/text` 与 `WebShop Button/Button Text` —— 它们带
        //   `ContentSizeFitter(h:PreferredSize)` / `AspectRatioFitter`，而**文字宽要 Unity 的字体度量**；
        //   `GeoSmall` 那两份量得出（124.50 / 132.00），`GeoBig` 量成 0（同一个字符串、同一档字号、
        //   同一个框宽 219.62 / 241.24 ⇒ **宽度本来就该相同**）。
        //   **判据（能反推出来，不是猜）**：`GeoBig` 里价签 `icon` 的 x1 恰好 = 「**只按图标自己**
        //   （text 宽 = 0）在 219.62 的框里居中」的结果（1130.75 + (219.62−44.96)/2 = **1218.08** ✓
        //   与 dump 逐位相同）⇒ 证实 dump 那一刻文字宽 = 0；而 `GeoSmall` 同一格 = 「**按图标 + 124.50**
        //   居中」（986.75 + (219.62−169.46)/2 = **1011.83** ✓）。
        //   ⇒ `GeoBig` 那两格**取 `GeoSmall` 的宽**（同一串字、同一档号 ⇒ 宽相同）。
        //   ⛔ 这两格是**全文件唯一两处非逐字照抄**，改版式时**从这两格看起**。
        /// <summary>一档几何。**每个字段都是 `--relative` 现读的矩形**（左上原点 · 画布像素 · y 向下）。</summary>
        public struct Geo
        {
            /// <summary>档名（自检按它认档）。</summary>
            public string Name;
            public PxRect Window, PaneBg, Close, CloseArt, Artwork, ArtBg, ArtFg, Text;
            public PxRect Title, Category, Desc, Avail, Timer, TimerIcon, TimerTextBox;
            public PxRect Badge, BadgeText;
            /// <summary>`Purchase buttons`（原版是 `HorizontalLayoutGroup` spacing 15 ⇒ 存**布局跑完**的实测矩形）。</summary>
            public PxRect Purchase, Price, PriceBtn, PriceBtnText, PriceBox, PriceIcon, PriceText;
            /// <summary>`Offer Price Discount Badge` 整件（**带 rotation**，见 `DiscountRotDeg`）。</summary>
            public PxRect Discount, DiscTitle, DiscPrice;
            public PxRect WebShop, WebGlow, WebImg, WebIcon, WebText;
            public PxRect Preview, PreviewText;
        }

        /// <summary>母版那一档（16/21 份用它）。</summary>
        public static readonly Geo GeoSmall = new Geo
        {
            Name = "small",
            Window = new PxRect(395.72f, 188.35f, 1524.28f, 851.65f),
            PaneBg = new PxRect(395.70f, 178.35f, 1547.30f, 895.80f),
            Close = new PxRect(1487.08f, 159.85f, 1561.47f, 235.45f),
            CloseArt = new PxRect(1495.24f, 167.83f, 1552.10f, 225.96f),
            Artwork = new PxRect(395.72f, 188.35f, 960.00f, 851.65f),
            ArtBg = new PxRect(405.72f, 198.35f, 950.00f, 841.65f),
            ArtFg = new PxRect(395.72f, 218.35f, 960.00f, 861.65f),
            Text = new PxRect(960.00f, 204.35f, 1508.28f, 835.65f),
            Title = new PxRect(976.00f, 260.56f, 1492.72f, 312.56f),
            Category = new PxRect(976.00f, 307.85f, 1492.72f, 352.85f),
            Desc = new PxRect(976.00f, 356.64f, 1477.24f, 672.07f),
            Avail = new PxRect(422.00f, 282.75f, 730.05f, 314.35f),
            Timer = new PxRect(1102.97f, 766.48f, 1300.64f, 793.24f),
            TimerIcon = new PxRect(1185.32f, 764.86f, 1218.28f, 794.86f),
            TimerTextBox = new PxRect(1218.28f, 767.36f, 1218.28f, 792.36f),
            Badge = new PxRect(403.50f, 217.35f, 809.49f, 296.20f),
            BadgeText = new PxRect(419.50f, 233.35f, 798.73f, 280.20f),
            Purchase = new PxRect(975.00f, 701.91f, 1476.24f, 762.29f),
            Price = new PxRect(977.68f, 695.35f, 1217.32f, 768.86f),
            PriceBtn = new PxRect(977.68f, 695.35f, 1217.32f, 768.86f),
            PriceBtnText = new PxRect(990.68f, 711.32f, 1204.32f, 752.88f),
            PriceBox = new PxRect(986.75f, 710.17f, 1206.37f, 755.13f),
            PriceIcon = new PxRect(1011.83f, 710.17f, 1056.79f, 755.13f),
            PriceText = new PxRect(1034.31f, 710.17f, 1158.81f, 755.13f),
            Discount = new PxRect(840.51f, 596.16f, 1019.48f, 775.14f),
            DiscTitle = new PxRect(883.17f, 638.25f, 975.41f, 691.62f),
            DiscPrice = new PxRect(883.56f, 691.62f, 976.13f, 728.63f),
            WebShop = new PxRect(1232.32f, 706.16f, 1473.56f, 758.04f),
            WebGlow = new PxRect(1195.32f, 667.55f, 1510.56f, 796.65f),
            WebImg = new PxRect(1232.32f, 706.16f, 1473.56f, 758.04f),
            WebIcon = new PxRect(1261.00f, 706.16f, 1312.88f, 758.04f),
            WebText = new PxRect(1286.94f, 706.16f, 1418.94f, 758.04f),
            Preview = new PxRect(422.00f, 795.00f, 592.00f, 845.00f),
            PreviewText = new PxRect(431.04f, 800.29f, 582.41f, 839.73f),
        };

        /// <summary>大窗那一档（5/21 份）。⚠️ 见 <see cref="Geo"/> 上面那两段：`PriceIcon` / `PriceText` /
        /// `WebIcon` / `WebText` 四格是**按 `GeoSmall` 的相对关系 + 本档的框重算的**（dump 量成 0）。</summary>
        public static readonly Geo GeoBig = new Geo
        {
            Name = "big",
            Window = new PxRect(246.94f, 159.58f, 1673.06f, 880.42f),
            PaneBg = new PxRect(246.65f, 149.93f, 1696.36f, 924.22f),
            Close = new PxRect(1635.86f, 131.08f, 1710.25f, 206.68f),
            CloseArt = new PxRect(1644.01f, 139.05f, 1700.88f, 197.18f),
            Artwork = new PxRect(246.94f, 159.58f, 960.00f, 880.42f),
            ArtBg = new PxRect(256.94f, 169.58f, 950.00f, 870.42f),
            ArtFg = new PxRect(246.94f, 189.58f, 960.00f, 890.42f),
            Text = new PxRect(1104.00f, 175.58f, 1801.06f, 864.42f),
            Title = new PxRect(1120.00f, 231.79f, 1636.72f, 283.79f),
            Category = new PxRect(1120.00f, 279.08f, 1636.72f, 324.08f),
            Desc = new PxRect(1120.00f, 327.86f, 1621.24f, 643.29f),
            Avail = new PxRect(275.50f, 253.98f, 583.55f, 285.58f),
            Timer = new PxRect(1254.98f, 764.46f, 1452.65f, 791.21f),
            TimerIcon = new PxRect(1337.33f, 762.84f, 1370.29f, 792.84f),
            TimerTextBox = new PxRect(1370.29f, 765.34f, 1370.29f, 790.34f),
            Badge = new PxRect(257.00f, 188.58f, 811.77f, 267.43f),
            BadgeText = new PxRect(273.00f, 204.58f, 801.00f, 251.43f),
            Purchase = new PxRect(1119.00f, 701.91f, 1620.24f, 762.29f),
            Price = new PxRect(1121.68f, 695.35f, 1361.32f, 768.86f),
            PriceBtn = new PxRect(1121.68f, 695.35f, 1361.32f, 768.86f),
            PriceBtnText = new PxRect(1134.68f, 711.32f, 1348.32f, 752.88f),
            PriceBox = new PxRect(1130.75f, 710.17f, 1350.37f, 755.13f),
            PriceIcon = new PxRect(1155.83f, 710.17f, 1200.79f, 755.13f),      // = 小档 + 144（见 Geo 那段）
            PriceText = new PxRect(1178.31f, 710.17f, 1302.81f, 755.13f),      // = 小档 + 144
            Discount = new PxRect(984.51f, 596.16f, 1163.48f, 775.14f),
            DiscTitle = new PxRect(1027.17f, 638.25f, 1119.41f, 691.62f),
            DiscPrice = new PxRect(1027.56f, 691.62f, 1120.13f, 728.63f),
            WebShop = new PxRect(1376.32f, 706.16f, 1617.56f, 758.04f),
            WebGlow = new PxRect(1339.32f, 667.55f, 1654.56f, 796.65f),
            WebImg = new PxRect(1376.32f, 706.16f, 1617.56f, 758.04f),
            WebIcon = new PxRect(1405.00f, 706.16f, 1456.88f, 758.04f),        // = 小档 + 144
            WebText = new PxRect(1430.94f, 706.16f, 1562.94f, 758.04f),        // = 小档 + 144
            Preview = new PxRect(291.00f, 819.00f, 461.00f, 869.00f),
            PreviewText = new PxRect(300.04f, 824.29f, 451.38f, 863.73f),
        };

        // ============================================================ 变体表（**21 条，逐份实读**）
        /// <summary>一个抽屉槽（= 原版 `window` 下那个 `ItemDrawer&lt;T&gt;` 实例节点）。</summary>
        public struct VariantDrawer
        {
            /// <summary>节点名（**逐字照抄**，含 `" (1)"` / `" 2"` 这类重名后缀）。</summary>
            public string Name;
            /// <summary>矩形（**相对根左上角** · 画布像素 · y 向下）。</summary>
            public PxRect R;
            /// <summary>`m_LocalRotation` 绕 z 的角度（度；`0` = 没转）。见文件头 ① 那条推导。</summary>
            public float Rot;
            /// <summary>出厂 `activeSelf`（`false` = 建成后 `SetActive(false)`）。</summary>
            public bool On;
            /// <summary>抽屉类名（**实读自 prefab 那颗组件自己的 `m_Script`**，逐字）。
            /// ⚠️ `OfferContainer.SlotTypes` 只覆盖 21 个槽名里的 18 个（本族独有
            /// `Card Drawer (1)` / `Icon Currency Drawer Variant (2)` / `Icon Avatar Drawer Variant (1)` /
            /// `Icon Premium Campaign Drawer Variant 2` / `Avatar Border Drawer Shop Variant` /
            /// `Icon Expansion Pass Premium Drawer Variant Variant` 这 6 个）⇒ **本表自带类名**，
            /// 并由 `Editor/ShopScene.cs` 那条断言核「与 `OfferContainer` 共有的名字两边一致」。</summary>
            public string Cls;
        }

        /// <summary>一条变体。</summary>
        public struct Variant
        {
            /// <summary>**原 prefab 名**（逐字；= 我们建出来的**根节点名**，也是 `WindowsManager` 复用的键）。</summary>
            public string Prefab;
            public Geo G;
            /// <summary>`Artwork` 的出厂 `activeSelf`（21 份里 **5 份是 `false`**，见 `Variants` 的逐条注释）。
            /// ⚠️ 它**不是**「有没有立绘」—— 那是运行期 `SetEventData` 写的；这里记的是 **prefab 出厂态**
            /// （铁律 5·c：同一个组件逐份不同）。</summary>
            public bool ArtworkOn;
            /// <summary>`Artwork/foreground` 的出厂 `activeSelf`（**另外 5 份是 `false`，与 `ArtworkOn` 不是同一批**）。
            /// 原版那颗 `Image` 的 `m_Sprite` 出厂是**空**（运行期 `LoadAsset(offer, 2)` 灌）⇒ 我们只建节点。</summary>
            public bool ForegroundOn;
            /// <summary>`window` 下的抽屉槽（**兄弟序**）。`null` = 这一份一个抽屉都没有。</summary>
            public VariantDrawer[] Drawers;
            /// <summary>本族独有的**根级** `AvatarHelpText`（只有 `Variant Expansion pass` 一份有）。</summary>
            public bool AvatarHelpText;
        }

        /// <summary>`AvatarHelpText`（只有 `Variant Expansion pass` 有）的矩形：现读
        /// `(608,795)→(976,845)` · fs24 base24 auto[12~24] · `Left/Midline` · 折行=1 · 出厂字串是**空串**
        /// （原版是 I2 词条，`EverguildTextMeshPro` + `Localize`）⇒ 我们**只建空节点 + 出声**。</summary>
        public static readonly PxRect AvatarHelpR = new PxRect(608f, 795f, 976f, 845f);

        /// <summary>**21 条变体表**。每条上面那行注释是它自己的读数（`Artwork`/`foreground` 出厂态 ·
        /// 抽屉数 · 有几件出厂关着）。复现命令 =
        /// `python 工具/menu_dump.py bundle_menus_assets_all "&lt;下面那条的名字&gt;" --depth 12 --relative --md`。</summary>
        public static readonly Variant[] Variants =
        {
            // ---- 母版 + 只有立绘那一份（**37 节点，一个抽屉都没有**）----
            // `Artwork` T · `foreground` T · 0 槽
            new Variant { Prefab = "Base Offer Popup", G = GeoSmall, ArtworkOn = true, ForegroundOn = true },
            // `Artwork` T · `foreground` T · 0 槽（与母版**逐节点同构** —— 「Just Foreground」= 只画 `foreground` 那一档）
            new Variant { Prefab = "General Basic Offer Popup Just Foreground", G = GeoSmall, ArtworkOn = true, ForegroundOn = true },

            // ---- 小档 16 份里带抽屉的 ----
            // `Artwork` **F** · `foreground` T · 3 槽（`Card Drawer` 出厂关）
            new Variant { Prefab = "General Basic Offer Popup Booster_CardOrAltArt", G = GeoSmall, ArtworkOn = false, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(544.45f, 226.45f, 993.55f, 675.55f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Card Drawer", R = new PxRect(418.72f, 336.46f, 702.83f, 766.93f), Rot = 6.25f, On = false, Cls = "CardDrawer" },
                    new VariantDrawer { Name = "Card Alternate Art Drawer", R = new PxRect(415.04f, 327.46f, 692.96f, 748.54f), Rot = 6.61f, On = true, Cls = "CardAlternateArtDrawer" },
                } },
            // `Artwork` **F** · `foreground` T · 6 槽（`Card Drawer` 出厂关）
            new Variant { Prefab = "General Basic Offer Popup Booster_CardOrAltArt_AvatarORTitle", G = GeoSmall, ArtworkOn = false, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(698.46f, 244.91f, 933.54f, 601.09f), Rot = -7.27f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(591.64f, 204.45f, 1028.74f, 641.55f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Card Drawer", R = new PxRect(418.72f, 336.46f, 702.83f, 766.93f), Rot = 6.25f, On = false, Cls = "CardDrawer" },
                    new VariantDrawer { Name = "Card Alternate Art Drawer", R = new PxRect(415.04f, 327.46f, 692.96f, 748.54f), Rot = 6.61f, On = true, Cls = "CardAlternateArtDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(684.30f, 667.50f, 999.70f, 778.50f), Rot = -8.99f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(736.81f, 613.81f, 931.19f, 808.19f), Rot = -1.87f, On = true, Cls = "AvatarDrawer" },
                } },
            // `Artwork` **F** · `foreground` T · 6 槽（`Card Drawer` 出厂关）
            new Variant { Prefab = "General Basic Offer Popup Booster_CardOrAltArt_Cardback_Avatar_Title", G = GeoSmall, ArtworkOn = false, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(698.46f, 244.91f, 933.54f, 601.09f), Rot = -7.27f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(550.87f, 291.87f, 887.13f, 628.13f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Card Drawer", R = new PxRect(418.72f, 336.46f, 702.83f, 766.93f), Rot = 6.25f, On = false, Cls = "CardDrawer" },
                    new VariantDrawer { Name = "Card Alternate Art Drawer", R = new PxRect(415.04f, 327.46f, 692.96f, 748.54f), Rot = 6.61f, On = true, Cls = "CardAlternateArtDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(436.09f, 704.39f, 751.50f, 815.40f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(775.81f, 597.81f, 970.19f, 792.19f), Rot = -1.87f, On = true, Cls = "AvatarDrawer" },
                } },
            // `Artwork` T · `foreground` T · 4 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Booster_avatar_cardback_title", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(450.44f, 244.44f, 1005.56f, 799.56f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(370.12f, 319.52f, 655.88f, 752.48f), Rot = 4.99f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(738.81f, 634.81f, 933.19f, 829.19f), Rot = -0.58f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(397.30f, 692.50f, 712.70f, 803.50f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                } },
            // `Artwork` T · `foreground` T · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Booster_avatar_resource", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(446.44f, 225.44f, 1001.56f, 780.56f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(712.13f, 605.13f, 951.87f, 844.87f), Rot = -4.24f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(422.20f, 556.20f, 655.80f, 789.80f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                } },
            // `Artwork` T · `foreground` T · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Booster_cardback_resource", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(450.44f, 244.44f, 1005.56f, 799.56f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(370.12f, 319.52f, 655.88f, 752.48f), Rot = 4.99f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(682.20f, 591.20f, 935.80f, 844.80f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                } },
            // `Artwork` T · `foreground` T · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Booster_title_resource", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(446.44f, 225.44f, 1001.56f, 780.56f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(713.20f, 527.20f, 946.80f, 760.80f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant (1)", R = new PxRect(383.90f, 675.60f, 699.30f, 786.60f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                } },
            // `Artwork` T · `foreground` T · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Deck_cardback_avatar", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Deck Drawer", R = new PxRect(455.62f, 254.97f, 780.38f, 747.03f), Rot = 8.71f, On = true, Cls = "DeckAndCardbackDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(678.79f, 397.68f, 943.21f, 798.32f), Rot = -9.05f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(403.81f, 587.81f, 598.19f, 782.19f), Rot = 3.05f, On = true, Cls = "AvatarDrawer" },
                } },
            // `Artwork` T · `foreground` **F** · 7 槽（`Icon Avatar Drawer Variant` 出厂关）· **另有根级 `AvatarHelpText`**
            new Variant { Prefab = "General Basic Offer Popup Variant Expansion pass", G = GeoSmall, ArtworkOn = true, ForegroundOn = false,
                AvatarHelpText = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(446.31f, 538.71f, 640.69f, 733.09f), Rot = 3.05f, On = false, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(733.73f, 621.83f, 905.47f, 793.57f), Rot = 2.58f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Card Drawer", R = new PxRect(432.36f, 295.84f, 663.84f, 646.56f), Rot = 3.57f, On = true, Cls = "CardDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(702.01f, 262.81f, 926.79f, 603.39f), Rot = -5.04f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Avatar Border Drawer Shop Variant", R = new PxRect(400.84f, 497.60f, 685.16f, 928.40f), Rot = 5.04f, On = true, Cls = "AvatarBorderDrawer" },
                    new VariantDrawer { Name = "Card Drawer (1)", R = new PxRect(692.74f, 267.70f, 924.26f, 618.50f), Rot = -4.73f, On = true, Cls = "CardDrawer" },
                    new VariantDrawer { Name = "Icon Expansion Pass Premium Drawer Variant Variant", R = new PxRect(653.86f, 550.15f, 936.14f, 977.85f), Rot = 0f, On = true, Cls = "ExpansionPassPremiumDrawer" },
                } },
            // `Artwork` **F** · `foreground` T · 4 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Premium_Premium_cardback_avatar", G = GeoSmall, ArtworkOn = false, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(382.70f, 260.06f, 711.30f, 757.94f), Rot = 5.52f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(566.81f, 110.71f, 1029.19f, 811.29f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant 2", R = new PxRect(598.49f, 405.23f, 1061.51f, 1106.77f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(482.81f, 621.81f, 677.19f, 816.19f), Rot = -3.32f, On = true, Cls = "AvatarDrawer" },
                } },
            // `Artwork` T · `foreground` T · 5 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Premium_booster_title_avatarOrResource", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(429.74f, 269.64f, 984.86f, 824.76f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(422.30f, 402.50f, 737.70f, 513.50f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(698.81f, 578.81f, 893.19f, 773.19f), Rot = -1.32f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(695.51f, 582.51f, 880.49f, 767.49f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(318.11f, 389.35f, 755.89f, 1052.65f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                } },
            // `Artwork` **F** · `foreground` T · 10 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Single Item Type", G = GeoSmall, ArtworkOn = false, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(416.34f, 252.74f, 971.46f, 807.86f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(403.80f, 609.20f, 719.20f, 720.20f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant (1)", R = new PxRect(403.80f, 364.20f, 719.20f, 475.20f), Rot = 0.82f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(432.91f, 521.81f, 627.29f, 716.19f), Rot = 3.05f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant (1)", R = new PxRect(711.34f, 568.74f, 893.66f, 751.06f), Rot = -4.48f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(425.93f, 566.03f, 597.67f, 737.77f), Rot = 2.58f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (1)", R = new PxRect(716.98f, 577.18f, 899.42f, 759.62f), Rot = -4.40f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (2)", R = new PxRect(426.58f, 359.88f, 609.02f, 542.32f), Rot = -0.25f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(385.02f, 325.02f, 670.78f, 757.98f), Rot = 4.99f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(597.67f, 466.43f, 767.19f, 649.77f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                } },
            // `Artwork` T · `foreground` **F** · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant avatarOrTitle_resource", G = GeoSmall, ArtworkOn = true, ForegroundOn = false,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(580.18f, 268.18f, 937.82f, 625.82f), Rot = -2.96f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant (1)", R = new PxRect(409.24f, 604.95f, 816.76f, 769.05f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(422.51f, 489.01f, 711.49f, 777.99f), Rot = 3.05f, On = true, Cls = "AvatarDrawer" },
                } },
            // `Artwork` T · `foreground` T · 6 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant cardback_premiumOrAvatarOrResource_titleOrResource", G = GeoSmall, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(617.11f, 299.23f, 938.89f, 786.77f), Rot = -3.17f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(424.30f, 387.50f, 739.70f, 498.50f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(446.31f, 538.71f, 640.69f, 733.09f), Rot = 3.05f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(464.93f, 545.93f, 673.07f, 754.07f), Rot = 2.58f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (1)", R = new PxRect(459.79f, 313.79f, 670.21f, 524.21f), Rot = -4.40f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(351.11f, 385.35f, 788.89f, 1048.65f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                } },

            // ---- 大档 5 份（`GeoBig`）----
            // `Artwork` T · `foreground` **F** · 2 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant 2 Currencies", G = GeoBig, ArtworkOn = true, ForegroundOn = false,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (1)", R = new PxRect(273.80f, 407.80f, 700.20f, 834.20f), Rot = 11.85f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(548.80f, 255.80f, 975.20f, 682.20f), Rot = -2.84f, On = true, Cls = "CurrencyDrawer" },
                } },
            // `Artwork` T · `foreground` **F** · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Booster + 2 Currencies", G = GeoBig, ArtworkOn = true, ForegroundOn = false,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(372.43f, 227.96f, 834.51f, 690.04f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (1)", R = new PxRect(282.68f, 511.68f, 613.32f, 842.32f), Rot = -10.38f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(603.94f, 519.94f, 942.06f, 858.06f), Rot = 5.30f, On = true, Cls = "CurrencyDrawer" },
                } },
            // `Artwork` T · `foreground` T · 5 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title", G = GeoBig, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(535.04f, 175.58f, 1082.96f, 723.49f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(279.88f, 265.46f, 598.70f, 748.54f), Rot = 4.99f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(799.04f, 628.50f, 1050.96f, 880.42f), Rot = -6.48f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(283.30f, 723.50f, 598.70f, 834.50f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(440.49f, 355.02f, 931.51f, 1098.98f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                } },
            // `Artwork` T · `foreground` T · 7 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Premium_Booster_avatar_cardback_title_resource", G = GeoBig, ArtworkOn = true, ForegroundOn = true,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Container Drawer Variant", R = new PxRect(515.04f, 159.58f, 1062.96f, 707.49f), Rot = 0f, On = true, Cls = "ContainerDrawer" },
                    new VariantDrawer { Name = "Cardback Drawer", R = new PxRect(279.88f, 265.46f, 598.70f, 748.54f), Rot = 4.99f, On = true, Cls = "CardbackDrawer" },
                    new VariantDrawer { Name = "Icon Avatar Drawer Variant", R = new PxRect(847.04f, 491.04f, 1098.96f, 742.96f), Rot = 1.99f, On = true, Cls = "AvatarDrawer" },
                    new VariantDrawer { Name = "Title Drawer Horizontal Variant", R = new PxRect(283.30f, 723.50f, 598.70f, 834.50f), Rot = 2.45f, On = true, Cls = "TitleDrawerHorizontal" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(440.49f, 355.02f, 931.51f, 1098.98f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                    new VariantDrawer { Name = "Icon Currency Drawer Variant (1)", R = new PxRect(726.40f, 654.20f, 960.00f, 887.80f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Expansion Pass Premium Drawer Variant", R = new PxRect(440.75f, 355.92f, 929.25f, 1096.08f), Rot = 0f, On = true, Cls = "ExpansionPassPremiumDrawer" },
                } },
            // `Artwork` T · `foreground` **F** · 3 槽（全开）
            new Variant { Prefab = "General Basic Offer Popup Variant Premium_Resource", G = GeoBig, ArtworkOn = true, ForegroundOn = false,
                Drawers = new[]
                {
                    new VariantDrawer { Name = "Icon Currency Drawer Variant", R = new PxRect(470.80f, 214.80f, 897.20f, 641.20f), Rot = -4.64f, On = true, Cls = "CurrencyDrawer" },
                    new VariantDrawer { Name = "Icon Premium Campaign Drawer Variant", R = new PxRect(107.33f, 250.10f, 752.67f, 1227.90f), Rot = 0f, On = true, Cls = "PremiumDrawer" },
                    new VariantDrawer { Name = "Icon Expansion Pass Premium Drawer Variant", R = new PxRect(102.51f, 250.11f, 757.49f, 1242.51f), Rot = 0f, On = true, Cls = "ExpansionPassPremiumDrawer" },
                } },
        };

        /// <summary>按 prefab 名找一条变体（找不到 ⇒ `false`，调用方**必须出声**）。</summary>
        public static bool Find(string prefab, out Variant v)
        {
            for (int i = 0; i < Variants.Length; i++)
                if (Variants[i].Prefab == prefab) { v = Variants[i]; return true; }
            v = default(Variant);
            return false;
        }

        /// <summary>母版那一份（= `<see cref="Variants"/>[0]`）。</summary>
        public static Variant Master { get { return Variants[0]; } }

        // ============================================================ 状态
        /// <summary>这一扇吃的是哪一条变体（`null` = 还没建）。</summary>
        public string VariantName { get; private set; }
        /// <summary>几何档（`GeoSmall` / `GeoBig`）。</summary>
        public Geo G { get; private set; }
        /// <summary>喂进来的报价内容（默认 = 出厂值，见 <see cref="DefContent"/>）。</summary>
        public OfferContainer.Content C { get; private set; }

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly List<string> MissingArt = new List<string>();
        /// <summary>建出来的抽屉槽（**兄弟序**；自检按它对账）。</summary>
        public readonly List<Transform> DrawerNodes = new List<Transform>();
        /// <summary>每个槽的抽屉类名（与 <see cref="DrawerNodes"/> **同序**）。</summary>
        public readonly List<string> DrawerClasses = new List<string>();
        /// <summary>`ItemDrawer.Draw` 真填上了几个槽（没有报价 ⇒ 0）。</summary>
        public int DrawerFilled { get; private set; }

        /// <summary>自检要按名字找的几个节点。</summary>
        public Transform DarkNode { get; private set; }
        public Transform WindowNode { get; private set; }
        public Transform ArtworkNode { get; private set; }
        public Transform ArtBgNode { get; private set; }
        public Transform ArtFgNode { get; private set; }
        public Transform TextNode { get; private set; }
        public Transform BadgeNode { get; private set; }
        public Transform TimerNode { get; private set; }
        public Transform AvailNode { get; private set; }
        public Transform PreviewNode { get; private set; }
        public Transform AvatarHelpNode { get; private set; }
        /// <summary>价签里那颗出厂 INACT 的 `Button Text`（21/21 实读 `act=F`，见文件头）。</summary>
        public Transform PriceBtnTextNode { get; private set; }

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static BaseOfferPopup LastOpened { get; private set; }

        // 「上一次建出来的是哪一套」—— 用来实现「同窗再开、内容没变 ⇒ **不重建**」
        // 🔴 这一条**不是优化，是照原版**：`GameWindow.TryOpen` 在 `CurrentState == Open` 时**一个字段都不写**
        //    （`GameWindow__TryOpen` 的第三支，A217② 已按原版落地）⇒ 原版「同窗再开」不重建内容。
        //    我们的开窗入口（`WindowsManager.OpenBaseOfferPopup`）在复用那一支会显式调 `Show(...)`，
        //    靠这个守卫把「没变化」挡在 `Build()` 之前。
        string _builtVariant;
        OfferContainer.Content _builtContent;
        bool _built;

        // ============================================================ 开

        /// <summary>建一扇**母版**。窗口字段照 21/21 份实读值（见文件头）。</summary>
        public static BaseOfferPopup Create(WindowsManager mgr) { return Create(mgr, null, null); }

        /// <summary>建一扇指定变体 + 指定内容。`prefab` 不在 <see cref="Variants"/> 里（含 `null`）⇒
        /// **出声并退回母版**；`content == null` ⇒ 出厂值。</summary>
        public static BaseOfferPopup Create(WindowsManager mgr, string prefab, OfferContainer.Content? content = null)
        {
            var v = Resolve(prefab);
            var go = new GameObject(v.Prefab);            // 名字 = prefab 名（`WindowsManager` 复用的键同源）
            var win = go.AddComponent<BaseOfferPopup>();
            win.type = WindowType.Popup;                  // 21/21：`type = 1`
            win.placement = WindowsPlacement.Popup;       // 21/21：`windowsPlacement = 15`（**不是 10**）
            win.closeOnEsc = true;                        // 21/21：`closeOnESC = 1`
            win.extraScaleSmallScreen = 1.2f;             // 21/21：`extraScaleSmallScreen = 1.2`
            win.Manager = mgr;
            win.VariantName = v.Prefab;
            win.C = FillDef(content);
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>`prefab` → 一条变体。**不在表里（含 `null`）⇒ 出声 + 退回母版**（⛔ 不静默换一个）。</summary>
        public static Variant Resolve(string prefab)
        {
            if (prefab == null) return Master;
            Variant v;
            if (Find(prefab, out v)) return v;
            Debug.LogWarning("[BaseOffer] 变体 `" + prefab + "` **不在变体表里**（表里 21 条）⇒ 退回母版"
                             + "`" + Master.Prefab + "`。表 = `Shell/BaseOfferPopup.cs` 的 `Variants`，"
                             + "来源 = 本文件头部那条 `menu_dump` 命令逐份现读。");
            return Master;
        }

        public override void Open()
        {
            LastOpened = this;
            if (VariantName == null) VariantName = Master.Prefab;
            Build();
        }

        /// <summary>换一份变体 / 换一份内容（同 `BoosterInfoPopup.Show` 那条先例：先 `OpenWindow` 再喂数据）。
        /// `prefab == null` ⇒ 保持当前变体；`c == null` ⇒ 用**出厂值**（= 原版「没有报价」那一支，见文件头）。
        /// <para>🔴 **变体与内容都没变时不重建** —— 照原版 `TryOpen` 的 `Open` 支（「同窗再开不刷内容」，
        /// A217② 已落地；判据 → `WindowsManager.GameWindow.TryOpen` 那段）。见上面那组字段的注释。</para></summary>
        public void Show(string prefab, OfferContainer.Content? c = null)
        {
            var v = prefab == null ? Resolve(VariantName) : Resolve(prefab);
            var cc = FillDef(c);
            if (_built && v.Prefab == _builtVariant && cc.Equals(_builtContent))
            {
                Debug.Log("[BaseOffer] `" + v.Prefab + "` 已经开着、**变体与内容都没变** ⇒ **不重建**"
                          + "（照原版 `GameWindow.TryOpen` 的 `Open` 支：同窗再开一个字段都不写）");
                return;
            }
            VariantName = v.Prefab;
            gameObject.name = v.Prefab;
            C = cc;
            Build();
        }

        /// <summary>出厂内容（**只有那几条数据栏**；`Item` 留空 ⇒ 抽屉不画，同原版 `ItemDrawer.Draw` 第 ②步）。
        /// 🔴 类型直接复用 `OfferContainer.Content` —— 两族是**同一条「服务端报价」链**，
        /// 那边已经把「出厂值 / 空 = 不画」的口径定好了；再立一个同形结构 = 两处写同一条规则。</summary>
        public static OfferContainer.Content DefContent() { return FillDef(null); }

        /// <summary>逐格补出厂值：传进来的哪一格是 `null` 就拿出厂值顶上（**已赋的值一个字不动**）。</summary>
        public static OfferContainer.Content FillDef(OfferContainer.Content? src)
        {
            var c = src ?? new OfferContainer.Content();
            if (c.Name == null) c.Name = DefTitleText;
            if (c.Type == null) c.Type = DefCategoryText;
            if (c.Price == null) c.Price = DefPriceText;
            if (c.BadgeText == null) c.BadgeText = DefBadgeText;
            if (c.TimerText == null) c.TimerText = DefTimerText;
            if (c.Available == null) c.Available = DefAvailText;
            return c;
        }

        // ============================================================ 建

        /// <summary>照变体表建一棵树（**先清空重来** —— 换变体/换内容都是重建，同 `BoosterInfoPopup.Build`）。</summary>
        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();
            DrawerNodes.Clear();
            DrawerClasses.Clear();
            DrawerFilled = 0;
            BadgeNode = TimerNode = AvailNode = AvatarHelpNode = null;

            Variant v;
            if (!Find(VariantName, out v)) { v = Master; VariantName = v.Prefab; }
            G = v.G;
            if (C.Name == null) C = FillDef(C);
            _built = true; _builtVariant = VariantName; _builtContent = C;   // 见那组字段的注释（`Show` 靠它判「没变」）

            // ① 压暗整屏（**点它关窗** = 原版 `BackgroundCloseButton`，21 份都挂在它身上）
            var darkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
            var darkQ = MenuDraw.Rect(transform, CardArt.Solid(), darkR, "Menu Dark Background", QShade, DarkTint);
            DarkNode = darkQ != null ? darkQ.transform : MenuDraw.Node(transform, "Menu Dark Background", darkR);
            MenuDraw.ShadeHit(DarkNode, darkR, QShadeHit, QHit, () => Close(), "CloseHit");

            // ② `window`（**尺寸逐档不同**，见 `Geo`）
            WindowNode = MenuDraw.Node(transform, "window", G.Window);

            // 窗底（`UI_Deck_Information_Back` 九宫 · 比 `window` 四周各外扩一点）
            var paneTex = Art(ArtPaneBg);
            if (paneTex != null)
                MenuDraw.Nine(WindowNode, paneTex, G.PaneBg, PaneBorder, PaneTexW, PaneTexH, QBg, null, true,
                              "Generic Window Red Background Big");
            else
                MenuDraw.Node(WindowNode, "Generic Window Red Background Big", G.PaneBg);
            // 🆕 面板吸收点击（判据 = 原版那颗 `Image` 的 `m_RaycastTarget = 1`；父链上没有点击处理器
            //    ⇒ 原版点面板**什么都不做**）—— 同 `BoosterInfoPopup` 那一处（A94 那条口径）。
            MenuDraw.Absorb(transform, "AbsorbHit", G.PaneBg, QShadeHit, QHit);

            // 关闭钮：**一颗节点带 Image + 两个孩子**（原版就是三层：底图在它自己身上）
            var closeQ = Rect(WindowNode, ArtCloseBase, G.Close, "Generic Close Button Orange", QClose, null, true);
            var close = closeQ != null ? closeQ.transform : MenuDraw.Node(WindowNode, "Generic Close Button Orange", G.Close);
            Rect(close, ArtCloseYellow, G.CloseArt, "Background", QClose, null, true);
            Rect(close, ArtCloseIcon, G.CloseArt, "Icon", QClose, null, true);
            // = 原版 `Open()` 里 `closeButton`(0x90) / `backgroundCloseButton`(0x98) 两颗绑**同一个槽 0x1c0**
            //   ⇒ **一颗的动作就是 `Close()`**（`BaseOfferPopup__Close.c` 也是从这两颗上摘同一个监听）。
            MenuDraw.Hit(close, "Hit", G.Close, QHit, () => Close(), closeQ, null, ArtCloseYellowHi);

            // ③ `Artwork`（左半边）—— 两件的出厂显隐**逐份不同**（`Variant.ArtworkOn` / `ForegroundOn`）
            ArtworkNode = MenuDraw.Node(WindowNode, "Artwork", G.Artwork);
            var bgTex = Art(ArtArtworkBg);
            var artQ = bgTex != null ? MenuDraw.Rect(ArtworkNode, bgTex, G.ArtBg, "background", QArt) : null;
            ArtBgNode = artQ != null ? artQ.transform : MenuDraw.Node(ArtworkNode, "background", G.ArtBg);
            if (artQ == null)
                Debug.Log("[BaseOffer] `Artwork/background` 的 sprite `" + ArtArtworkBg + "` **工程里没有**"
                          + "（`Resources/Art/{ui_menu,ui_deck,ui}/` 三个目录逐个 ls 过）⇒ 节点照样建、**这一格空着不画**"
                          + "（⛔ 不拿别的图顶上）。导入入口 = `工具/import_original_art.py` 的 `MENU_IMAGES`。");
            BindBlink(artQ);   // 原版这一格挂 `BlinkGraphic`（21 份的组件列里都有它）
            ArtFgNode = MenuDraw.Node(ArtworkNode, "foreground", G.ArtFg);
            // 那颗 `Image` 的 `m_Sprite` 出厂是**空**（运行期 `LoadAsset(offer, 2)` 灌）⇒ 只建节点、不画。
            ArtworkNode.gameObject.SetActive(v.ArtworkOn);
            ArtFgNode.gameObject.SetActive(v.ForegroundOn);

            // ④ 抽屉槽 —— **`window` 下、`Artwork` 与 `Text` 之间**（兄弟序照 prefab）
            BuildDrawers(v);

            // ⑤ 右半边 `Text`
            TextNode = MenuDraw.Node(WindowNode, "Text", G.Text);
            BuildTexts();
            BuildBadge();
            BuildTimer();
            BuildButtons();

            // ⑥ 根级的 `Preview`（**在 `window` 之外**）+ 只有一份变体有的 `AvatarHelpText`
            BuildPreview();
            BuildAvatarHelp(v);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[BaseOffer] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));
            HintGaps();
        }

        // ---------------------------------------------------------- Artwork 的 `BlinkGraphic`

        /// <summary>`Artwork/background` 那颗 `BlinkGraphic`（原版挂在这一格上；公共件 = `Shell/BlinkGraphic.cs`）。
        /// 参数与 `BoosterInfoPopup` 那一处同源（`DefaultSpeed` / `DefaultVariation`）。
        /// `null` = 主图取不到 ⇒ 没接（那种情况上面刚报过一条缺图，这里不再刷屏）。</summary>
        public BlinkGraphic Blink { get { return _blink; } }
        BlinkGraphic _blink;

        void Update() { Tick(Time.deltaTime); }

        /// <summary>推一拍闪烁（🔴 **本窗就这一条时钟** —— ⛔ 别在别处再推一次）。
        /// ⚠️ 批处理下**没有帧循环** ⇒ 自检**直调**它（同 `BoosterInfoPopup.Tick` 那条约定）。</summary>
        public void Tick(float dt) { if (_blink != null) _blink.Tick(dt); }

        void BindBlink(ImageQuad quad)
        {
            _blink = GetComponent<BlinkGraphic>();
            if (_blink == null) _blink = gameObject.AddComponent<BlinkGraphic>();
            _blink.LogTag = "[BaseOffer]";
            _blink.blinkSpeed = BlinkGraphic.DefaultSpeed;
            _blink.colorVariation = BlinkGraphic.DefaultVariation;
            if (quad == null) { _blink.Silent = true; _blink.Bind(null, null, null); return; }
            var q = quad;                                    // ⛔ 别直接捕形参（两个委托要活到下一次 Build）
            _blink.Silent = false;
            _blink.Bind(q.gameObject, () => q.Tint, c => q.SetTint(c));
            _blink.Restart();
        }

        // ---------------------------------------------------------- 右半边四段字

        /// <summary>`Title` / `Category` / `Descripton` / `Available Counter` 四段。
        /// 字号 / 自适应范围 / `m_fontSizeBase` / 对齐 **逐个照 prefab 实读**（母版 dump 的「文字」那一列）。</summary>
        void BuildTexts()
        {
            // `Title`：fs40 · base **45.2** · auto[3~40] · `Left/Middle` · 折行=1
            var title = MenuDraw.Text(TextNode, G.Title, C.Name, Color.white, "Title", 40f, QText);
            if (title != null)
            {
                // 🔴 **2026-10-13（A805）就地订正（铁律 5）**：这两句原来**反着写**（`AlignLeft` 在前、
                // `SetAutoFitBox` 在后）—— 而 `SetAutoFitBox` 的末句就是 `RefreshBounds()`
                // （`Battle/Label.cs`），它按**自适应之后**的宽度重算摆位 ⇒ 先做的那次对齐被抹掉、
                // 整块落回框心（实测左沿期望 976.00、实得 999.20 = 框心解）。
                // 纪律：**先自适应、后摆位**（同文件 `Descripton` / `MenuDraw.cs` 的那句「裁切/对齐
                // 必须落在那两步之后」）。
                title.SetAutoFitBox(LayoutSpace.Px(G.Title.W), LayoutSpace.Px(G.Title.H), 3f, 40f, 45.2f);
                MenuDraw.AlignLeft(title, G.Title);
            }
            // `Category`：fs39 · base 39（= 标称 ⇒ 不用显式传）· auto[3~39] · `Left/Midline` · 字距 **−1.8**
            var cat = MenuDraw.Text(TextNode, G.Category, C.Type, Color.white, "Category", 39f, QText);
            if (cat != null)
            {
                // 🔴 **2026-10-13（A805）**：`AlignLeft` 挪到 `SetAutoFitBox` **之后** —— 同 `Title` 那条。
                // 判据：`SetAutoFitBox` 末句 `RefreshBounds()` 会重摆位置（实测左沿期望 976.00、实得 1036.49）。
                // `SetCharSpacing` 只重排 mesh、不挪节点 ⇒ 位置在哪一句前后都行（照原版次序留在前面）。
                cat.SetCharSpacing(-1.8f);
                cat.SetAutoFitBox(LayoutSpace.Px(G.Category.W), LayoutSpace.Px(G.Category.H), 3f, 39f);
                MenuDraw.AlignLeft(cat, G.Category);
            }
            // `Descripton`：fs35 · base 39 · auto[3~35] · `Left/Middle` · 折行=1（文本恒用出厂原文，见文件头 ③）
            var desc = MenuDraw.TextBox(TextNode, G.Desc, DefDescText, Color.white, "Descripton", 35f, 3f,
                                        QText, 35f, 39f);
            if (desc != null) MenuDraw.AlignLeft(desc, G.Desc);

            // `Available Counter`：**它自己就是那颗 TMP**（原版那颗在 `Text` 下、带 `LayoutElement`）
            // —— fs30 · base 39 · auto[10~30] · `Left/Bottom` · **折行=0**
            // 🔴 **原版的 no-data 分支 = 关掉它自己**（`SetAvailableText.c` 对那颗 TMP 自己 `SetActive`）
            //    ⇒ 没有报价 ⇒ 关。见文件头。
            var av = MenuDraw.Text(TextNode, G.Avail, C.Available, Color.white, "Available Counter", 30f, QText);
            AvailNode = av != null ? av.transform : MenuDraw.Node(TextNode, "Available Counter", G.Avail);
            if (av != null)
            {
                // 🔴 **2026-10-13（A805 同族）**：`AlignLeft` 挪到 `SetAutoFitBox` **之后**（先自适应、后摆位）。
                av.SetAutoFitBox(LayoutSpace.Px(G.Avail.W), LayoutSpace.Px(G.Avail.H), 10f, 30f, 39f);
                MenuDraw.AlignLeft(av, G.Avail);
                av.SetWrapping(false);
            }
            AvailNode.gameObject.SetActive(false);
            Debug.Log("[BaseOffer] `Available Counter` **建成关着** —— 原版 `SetAvailableText(bool, int)` 的第一跳就是"
                      + "「那颗 TMP 自己 `SetActive(param_2)`」，`param_2` 由报价算出来；本地没有报价 ⇒ 走原版那一支。");
        }

        // ---------------------------------------------------------- `Offer Badge` + `Timer`

        /// <summary>`Offer Badge`（`WF_Special offer_Value` 九宫）与它下面那颗字。
        /// 🔴 **出厂显隐** = **关** —— 判据 = 原版 `SetBadgeText`：标签为空 ⇒ 把 `badgeText`（0xB0）的**父件**
        /// 关掉，而它就是 `Offer Badge` 下那颗 TMP ⇒ 关的是 `Offer Badge`。</summary>
        void BuildBadge()
        {
            var t = Art(ArtOfferBadge);
            if (t != null)
            {
                var bg = MenuDraw.Nine(TextNode, t, G.Badge, BadgeBorder, t.width, t.height, QBadge, BadgeTint,
                                       true, "Offer Badge");
                BadgeNode = bg != null ? bg.transform : MenuDraw.Node(TextNode, "Offer Badge", G.Badge);
            }
            else
            {
                BadgeNode = MenuDraw.Node(TextNode, "Offer Badge", G.Badge);
                Debug.Log("[BaseOffer] `Offer Badge` 的 sprite `" + ArtOfferBadge + "` **没进 `Resources/`**"
                          + "（只在 `Art/原版/0_mainmenu/` 下躺着）⇒ 节点照样建、**这一格空着不画**"
                          + "（⛔ 不拿别的图顶上）。");
            }
            var bt = MenuDraw.Text(BadgeNode, G.BadgeText, C.BadgeText, Color.white, "Text (TMP)", 38f, QBadgeText);
            if (bt != null)
            {
                // 🔴 **2026-10-13（A805 同族）**：同 `Title` —— 先自适应、后摆位。
                bt.SetAutoFitBox(LayoutSpace.Px(G.BadgeText.W), LayoutSpace.Px(G.BadgeText.H), 3f, 38f);
                MenuDraw.AlignLeft(bt, G.BadgeText);
            }
            BadgeNode.gameObject.SetActive(false);
            Debug.Log("[BaseOffer] `Offer Badge` **建成关着** —— 原版 `SetBadgeText` 那一跳"
                      + "（`GetLabel(offer, 3)` 空 ⇒ 关父件）；本地没有报价 ⇒ 走原版那一支。");
        }

        /// <summary>`Timer`（`TimerDisplay` + `HorizontalLayoutGroup`）。
        /// 🔴 **整件建成关着**（连图标都不画）—— 判据有两条，都在原版自己身上：
        /// ① `SetTimer.c`：`IsTimedEvent` 假 ⇒ `timer.gameObject.SetActive(false)`；
        /// ② `Timer` 是 HLG 而**倒计时文字的宽量出来是 0.00**（母版 dump）⇒ 图标的位置由那句话的宽度决定
        ///    ⇒ 单画一个图标只能**自己编一个位置**（同 `SkirmishEventWindow.BuildTimer` 那条判据）。</summary>
        void BuildTimer()
        {
            TimerNode = MenuDraw.Node(TextNode, "Timer", G.Timer);
            Rect(TimerNode, ArtClock, G.TimerIcon, "Icon", QText, null, true);
            MenuDraw.Text(TimerNode, G.TimerTextBox, C.TimerText, Color.white, "Timer Text", 30f, QText);
            TimerNode.gameObject.SetActive(false);
            Debug.Log("[BaseOffer] `Timer` **整件建成关着**（连图标）—— 原版 `SetTimer`：不是计时活动就"
                      + "`SetActive(false)`；而图标的位置由倒计时文字的宽度决定（HLG，量出来 0.00）⇒ 不编位置。");
        }

        // ---------------------------------------------------------- 底部三颗（价签 / 折扣徽标 / WebShop）

        /// <summary>`Purchase buttons`（原版 `HorizontalLayoutGroup` spacing 15 + `TransformScalerBySmallScreenUI`）
        /// ⇒ **直接照布局跑完的实测矩形摆**，不自己算布局（同 `BoosterInfoPopup.BuildButtons`）。
        /// 三件：`Price Display`（价签）· `Offer Price Discount Badge`（划线价）· `WebShop Button`。</summary>
        void BuildButtons()
        {
            var row = MenuDraw.Node(TextNode, "Purchase buttons", G.Purchase);

            // ---- 价签：`Price Display`(脚本节点) → `Generic UI Button`(带 Image) → { `Button Text`, `Price Display` } ----
            var pd = MenuDraw.Node(row, "Price Display", G.Price);
            var pq = Rect(pd, ArtBtn, G.PriceBtn, "Generic UI Button", QBtn, PriceTint, true);
            var gb = pq != null ? pq.transform : MenuDraw.Node(pd, "Generic UI Button", G.PriceBtn);
            // 🔴 `Button Text` **出厂 INACT**（21/21 实读 `act=F`、字串出厂是空串）⇒ 照 prefab 建成关的。
            var pbt = MenuDraw.Text(gb, G.PriceBtnText, "", Color.white, "Button Text", 12f, QBtn);
            if (pbt != null)
            {
                pbt.SetAutoFitBox(LayoutSpace.Px(G.PriceBtnText.W), LayoutSpace.Px(G.PriceBtnText.H), 12f, 38f);
                pbt.SetWrapping(false);
            }
            PriceBtnTextNode = pbt != null ? pbt.transform : null;
            if (PriceBtnTextNode != null) PriceBtnTextNode.gameObject.SetActive(false);
            // `Price Display`（里面 `icon` + `text`）：原版那颗 `icon` 的 sprite **出厂是空**
            // （21/21 实读 `<无图>`，运行期由 `PriceDisplayButton.Setup(offer)` 灌货币图标）⇒ 只建节点。
            var box = MenuDraw.Node(gb, "Price Display", G.PriceBox);
            MenuDraw.Node(box, "icon", G.PriceIcon);
            Debug.Log("[BaseOffer] 价签的 `icon` **只建节点不画** —— 原版那颗 `Image` 的 `m_Sprite` 出厂是空"
                      + "（21/21 实读 `<无图>`，运行期由 `PriceDisplayButton.Setup(offer)` 灌货币图标）。");
            var pt = MenuDraw.Text(box, G.PriceText, C.Price, Color.white, "text", 47.45f, QBtn);
            if (pt != null)
                pt.SetAutoFitBox(LayoutSpace.Px(G.PriceText.W), LayoutSpace.Px(G.PriceText.H), 12f, 54f, 39f);
            MenuDraw.Hit(pd, "Hit", G.Price, QHit, () => ProcessPurchase(), pq, ArtBtn, ArtBtnHi);

            // ---- `Offer Price Discount Badge`（**带 rotation 17.17°**，见 `DiscountRotDeg`）----
            var dq = Rect(row, ArtOfferBadge, G.Discount, "Offer Price Discount Badge", QBtn, null, false);
            var disc = dq != null ? dq.transform : MenuDraw.Node(row, "Offer Price Discount Badge", G.Discount);
            if (dq == null)
                Debug.Log("[BaseOffer] `Offer Price Discount Badge` 的 sprite `" + ArtOfferBadge
                          + "` **没进 `Resources/`** ⇒ 节点照样建、**这一格空着不画**（同上，⛔ 不拿别的图顶上）。");
            disc.localRotation = Quaternion.Euler(0f, 0f, DiscountRotDeg);
            var dt = MenuDraw.Text(disc, G.DiscTitle, DefPrevPriceTitle, Color.white, "Discount Title", 28.15f, QBtn);
            if (dt != null) dt.SetAutoFitBox(LayoutSpace.Px(G.DiscTitle.W), LayoutSpace.Px(G.DiscTitle.H), 18f, 72f, 36f);
            var dp = MenuDraw.Text(disc, G.DiscPrice, DefPrevPriceValue, Color.white, "Discount Price", 39.05f, QBtn);
            if (dp != null) dp.SetAutoFitBox(LayoutSpace.Px(G.DiscPrice.W), LayoutSpace.Px(G.DiscPrice.H), 18f, 72f, 36f);

            // ---- `WebShop Button`（自己**没有** Graphic，四个孩子）----
            var ws = MenuDraw.Node(row, "WebShop Button", G.WebShop);
            var gt = Art(ArtGlow);
            if (gt != null)
                MenuDraw.Nine(ws, gt, G.WebGlow, GlowBorder, GlowTexW, GlowTexH, QBtn, GlowTint, true, "Highlight");
            else
                MenuDraw.Node(ws, "Highlight", G.WebGlow);
            var wq = Rect(ws, ArtBtn, G.WebImg, "Button Image", QBtn, WebTint, true);
            // `Icon`：矩形 51.88² **× `localScale 1.2`** ⇒ 画出来 62.26²（同中心放大，同 `BoosterInfoPopup`）
            const float iconScl = 1.2f, iconSide = 51.88f * iconScl;
            var iconR = new PxRect(G.WebIcon.CX - iconSide * 0.5f, G.WebIcon.CY - iconSide * 0.5f,
                                   G.WebIcon.CX + iconSide * 0.5f, G.WebIcon.CY + iconSide * 0.5f);
            Rect(ws, ArtDiscountIcon, iconR, "Icon", QBtn);
            var wt = MenuDraw.Text(ws, G.WebText, DefWebShopText, Color.white, "Button Text", 34.2f, QBtn);
            if (wt != null)
            {
                wt.SetAutoFitBox(LayoutSpace.Px(G.WebText.W), LayoutSpace.Px(G.WebText.H), 12f, 44f, 12f);
                wt.SetWrapping(false);                    // prefab 那一行 `折行=0`（同 `BoosterInfoPopup` 那处）
            }
            MenuDraw.Hit(ws, "Hit", G.WebShop, QHit, WebShopClick, wq, ArtBtn, ArtBtnHi);
        }

        /// <summary>买（原版 `ProcessPurchase`）。**我们这条链没有购买后端**（用户边界②：不做真实经济）
        /// ⇒ **只出声**（红线：点了必须有反应、不许静默）。</summary>
        public void ProcessPurchase()
        {
            Debug.Log("[BaseOffer] 点了价签 —— 原版走 `BaseOfferPopup.ProcessPurchase` → `PurchasesManager` 真扣款；"
                      + "**我们不做真实经济**（用户 2026-09-17 边界②）⇒ **只出声、不扣任何东西**。");
        }

        /// <summary>`WebShop Button`（原版 `WebShopOpenButton` 开**外部 WebShop 真钱页**）⇒ 同上，只出声。</summary>
        void WebShopClick()
        {
            Debug.Log("[BaseOffer] 点了 `WebShop Button` —— 原版是**打开外部 WebShop（真钱）**；"
                      + "**我们不做真实经济**（用户 2026-09-17 边界②）⇒ **只出声、不跳转**。");
        }

        // ---------------------------------------------------------- 根级两件

        /// <summary>`Preview`（**在 `window` 之外**、根级）。⚠️ 原版那个字段的旧名是
        /// `previewDebugButton`（`dump.cs` 的 `[FormerlySerializedAs]`）⇒ 是个**调试预览**钮；
        /// 它的 `onClick` 挂在 prefab 的 UnityEvent 上（`Open()` 里只拿它做非空判定）
        /// ⇒ 我们**建出来、点了如实出声**。</summary>
        void BuildPreview()
        {
            var q = Rect(transform, ArtBtn, G.Preview, "Preview", QArt, PreviewTint, true);
            PreviewNode = q != null ? q.transform : MenuDraw.Node(transform, "Preview", G.Preview);
            var t = MenuDraw.Text(PreviewNode, G.PreviewText, DefPreviewText, Color.white, "Button Text", 40f, QArt);
            if (t != null)
            {
                t.SetAutoFitBox(LayoutSpace.Px(G.PreviewText.W), LayoutSpace.Px(G.PreviewText.H), 10f, 40f, 12f);
                t.SetWrapping(false);
            }
            MenuDraw.Hit(PreviewNode, "Hit", G.Preview, QHit, () =>
                Debug.Log("[BaseOffer] 点了 `Preview` —— 原版这颗字段的旧名是 `previewDebugButton`（调试预览），"
                          + "它的动作挂在 prefab 的 UnityEvent 上、要报价里那一项；本地没有报价 ⇒ **只出声、不做别的**。"),
                q, ArtBtn, ArtBtnHi);
        }

        /// <summary>根级 `AvatarHelpText`（**只有 `Variant Expansion pass` 有**）。
        /// 原版那件是 `EverguildTextMeshPro` + `Localize`、**出厂字串是空串**（正文是 I2 词条，
        /// 词条表在远端 CCD）⇒ 我们**只建空节点 + 出声**（同 `BoosterInfoPopup` 的 `Tooltip` 那条口径）。</summary>
        void BuildAvatarHelp(Variant v)
        {
            if (!v.AvatarHelpText) return;
            var t = MenuDraw.Text(transform, AvatarHelpR, "", Color.white, "AvatarHelpText", 24f, QArt);
            AvatarHelpNode = t != null ? t.transform : MenuDraw.Node(transform, "AvatarHelpText", AvatarHelpR);
            if (t != null) t.SetAutoFitBox(LayoutSpace.Px(AvatarHelpR.W), LayoutSpace.Px(AvatarHelpR.H), 12f, 24f);
            Debug.Log("[BaseOffer] `AvatarHelpText` 建出来了但**正文是空的** —— 原版那件出厂字串就是空串"
                      + "（正文是 I2 词条、词条表在远端 CCD、本地一个 value 都没有）⇒ 不自己编词。");
        }

        // ---------------------------------------------------------- 抽屉槽

        /// <summary>建这一份变体的抽屉槽（**`window` 的孩子、兄弟序照 prefab**）。
        /// <para>🔴 **槽 = 原版那颗 `ItemDrawer&lt;T&gt;` 实例**：按**它自己的矩形与 rotation** 建一个节点，
        /// 再转调 `ItemDrawer.Draw(...)` 往里画 —— **判据只此一份**，本文件不另写一套画法
        /// （同 `OfferContainer.FillSlot` 那条纪律）。画的档 = `DrawerOverride.OfferPopups`(30)
        /// （原版 `GeneralOfferPopupDrawer.DrawRewards.c:319` 传的就是 `0x1e`）。</para>
        /// <para>⚠️ **没有报价 ⇒ 抽屉里什么都不画**（= 原版 `ItemDrawer.Draw` 第 ②步：`PickDrawer` 判空就不画）
        /// —— 但**槽节点本身照建、显隐照 prefab 的 `act`**，因为「哪几个槽在这一份里是开的」本身是 prefab 值
        /// （铁律 5·c）。</para></summary>
        void BuildDrawers(Variant v)
        {
            if (v.Drawers == null) return;
            var st = ItemDrawerStyle.Default(QDrawer, QDrawer, QText);
            st.NodeName = "Item Drawer";
            st.QDecor = QDrawerDeco;
            st.IconFill = 1f;        // 原版这些抽屉的主图**铺满抽屉框**（`Content/Image` 与抽屉根同矩形）
            st.QuantityPx = 0f;      // 本族抽屉出厂**不画数量**
            st.NamePx = 0f;          // 也不画名字（那是别的链按奖励状态叠的）

            for (int i = 0; i < v.Drawers.Length; i++)
            {
                var d = v.Drawers[i];
                var slot = MenuDraw.Node(WindowNode, d.Name, d.R);
                slot.localRotation = Quaternion.Euler(0f, 0f, d.Rot);
                slot.gameObject.SetActive(d.On);
                DrawerNodes.Add(slot);
                DrawerClasses.Add(d.Cls);

                var drew = ItemDrawer.Draw(slot, d.R, C.Item, 1, DrawerOverride.OfferPopups, st);
                if (drew.Node == null)
                {
                    // 原版第 ②步（`drawer == null` ⇒ 不画）。没有报价 ⇒ 这一支是**正常的**，不刷屏。
                    continue;
                }
                DrawerFilled++;
                if (drew.Placeholder)
                    Debug.LogWarning("[BaseOffer] 槽 `" + d.Name + "`：抽屉**落了占位板**（图取不到）");
                else if (drew.FallbackArt)
                    Debug.LogWarning("[BaseOffer] 槽 `" + d.Name + "`：抽屉用了**退档图**（`" + drew.Art + "`）");
            }
            if (C.Item.Kind == ItemKind.None)
                Debug.Log("[BaseOffer] 变体 `" + v.Prefab + "` 建了 " + DrawerNodes.Count
                          + " 个抽屉槽，**一个都没填** —— 原版这些槽由服务端报价驱动"
                          + "（`GeneralOfferPopupDrawer.DrawRewards`），本地没有报价（同 `OfferContainer` 那条口径）。"
                          + "喂数据走 `Show(prefab, content)`（`content.Item` = `ItemDrawer.Spec(...)`）。");
        }

        // ---------------------------------------------------------- 取图 / 出声

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }

        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        {
            // ⚠️ `MenuDraw.Rect` 在 `tex == null` 时直接返回 null ⇒ 纯色件要传 `CardArt.Solid()`
            //    （同 `BoosterInfoPopup.Rect` 那条注释）。
            var t = string.IsNullOrEmpty(art) ? CardArt.Solid() : Art(art);
            return MenuDraw.Rect(p, t, r, n, q, tint, keepAspect);
        }

        /// <summary>把「原版有、我们没做 / 没数据」的几处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[BaseOffer] `" + VariantName + "`（几何档 `" + G.Name + "`）建好了 —— "
                      + "**骨架照 21 份实读**；数据栏（标题/类别/描述/角标/倒计时/价钱/立绘）原版由服务端报价灌，"
                      + "本地没有 ⇒ 走**原版自己的 no-data 分支**（关掉 `Timer` / `Offer Badge` / `Available Counter`），"
                      + "其余用 **prefab 出厂文本**。详见 `Shell/BaseOfferPopup.cs` 文件头。");
        }

        public string Dump()
        {
            return "BaseOffer[" + VariantName + "] geo=" + G.Name
                   + " · 抽屉槽 " + DrawerNodes.Count + " 个（填了 " + DrawerFilled + "）"
                   + " · 取不到的图 " + MissingArt.Count + " 张"
                   + (MissingArt.Count > 0 ? "（" + string.Join("、", MissingArt.ToArray()) + "）" : "");
        }
    }
}
