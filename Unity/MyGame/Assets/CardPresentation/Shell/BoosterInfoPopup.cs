// BoosterInfoPopup.cs — §三 第 29 条 A6：商店的「Booster Info Popup」（卡包详情窗）
//
// ============================ 出处（唯一正本） ============================
// **几何**：`资料/阶段二_商店_原版规格.md` **§五·二 / §五·二·一**（19 行逐节点几何表）。
//   复现命令：`python 工具/menu_dump.py bundle_menus_assets_all "Booster Info Popup" --depth 7`
//             （绝对矩形 1920×1080 · 左上原点 · y 向下）
// **窗口字段**：MB `assets_full/bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_7872967592023106223.json`
//   —— 原样抄进 `Create()`：`type = 1 (Popup)` · `windowsPlacement = 10 (World)` · `closeOnESC = 1` ·
//   `updateNavPanel = 0` · `extraScaleSmallScreen = 1.2`。
//   该 MB 的绑定字段（**这扇窗的数据入口，全部是运行期赋的**）：
//     `nameText` / `categoryText` / `descriptionText` / `createCounterSlider`(+`createCounterSliderText`) /
//     `foreground` / `background` / `priceDisplayButton` / `closeButton` / `backgroundCloseButton`。
//
// ---- 🔴 入口：**是我们定的，不是复刻**（原版那条链我们没有）----
//   原版开它的是 `ShopOfferContainer.OnClick → OpenContainer`（从 offer 取 AssetGroup 0xc 的窗口 → `OpenWindow`；
//   `阶段二_商店_原版规格.md` §四「点商品」那行 + §五·二 末尾那条 2026-10-03 查实）。
//   而**我们用的那族格子** `CatalogItemContainer.OnInitialize` **只画抽屉、不开窗** ⇒
//   **我们把入口定成「点商店格里那张商品主图」**，而且**只在 `Type == "Booster Pack"` 的格子上接**
//   （这扇窗的内容是卡包保底进度，非卡包商品套不上）。
//   口径同「战斗入口四扇窗」：**入口由我们定、在这里如实标**。
//
// ---- 🔴 我们挑的 / 缺口（逐条出声，红线：不许静默失败）----
//   · **`Artwork/background` 与 `foreground` 两处原版 `m_Sprite` 都是空**（运行期赋卡包图）
//     ⇒ 我们把商品主图放 `background`（它挂着 `BlinkGraphic` = 会闪的那张），
//       `foreground` **只建节点、不画**（哪一张该放前景层，判据不足 —— 留白不猜）。
//   · **`Title` / `Category` / `Descripton` 三栏原版来自服务端 item**，我们的商品表没有后两栏 ⇒
//     · `Title` = 商品名（**我们编的**）
//     · `Category` = 按稀有度拼（`Rarity == 4` ⇒ `Legendary Booster Pack`，否则 `Booster Pack`）—— **我们拼的**
//     · `Descripton` = **prefab 自带的出厂文本原文**（`Contains 5 cards for the Sautekh army.…`）
//       ⚠️ **它点名 Sautekh** —— 原版运行期会被 item 文案整个覆盖，我们拿不到那份 ⇒
//       **原样用出厂文本、不自己编词**（同 `ShopTabPage.TxtEmpty` 那条先例）。要换真文案只改 `DescFor()` 一处。
//   · **保底进度 `100/200`** 同理：**出厂占位值**，我们**没有**「距上次传奇开了几包」这个计数。
//   · **`WebShop Button`** 原版是外部 WebShop（真钱）⇒ 我们**只出声、不跳转**（用户边界②：不做真实经济）。
//   · **`Highlight`**（`OctagonUI_Filled_Fade_SDF` + `UIBorderGlow`：`animDelay/animTime/playOnEnable`）
//     —— 我们**静态画**；那套辉光动画**没做**（出声）。
//   · **`Tooltip`**（`40k_generic_bt_info`，原版挂 `EverguildTooltipTrigger`）—— ✅ **2026-10-03（A12-P1）已接线**：
//     悬停进全工程的 `Tooltip` 层（`Core/Tooltip.cs`）、离开收；触发器的参数**逐字段照原文**（见 `BuildTooltipHit`）。
//     🔴 **正文是 I2 词条 `MenuShop/BoosterInfo/LegendaryTooltip`**（触发器实测 `text` + `localize = 1`），
//     **词条表在远端 CCD、本地一个 value 都没有** ⇒ **正文留空、不自己编**（见 `TipBody`，与 `ForgeTab.HelpTipBody` 同一条做法）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BoosterInfoPopup`（商店格的「卡包详情」窗）。入口见文件头 —— **我们定的**。</summary>
    public class BoosterInfoPopup : GameWindow
    {
        // 队列档：**在商店页（3020–3030）之上、tooltip（3199–3201）之下** ——
        // 🔴 3065–3099 是全工程的一段空档（`CampaignTab` 到 3064、练习窗从 3100 起），
        //    放这里才能让**窗里那个 `Tooltip` 图标**的 tooltip 还看得见（tooltip 队列是常量 3199+）。
        public const int QBase = 3070;
        // ⚠️ `public`（2026-10-04 A47 接线批）：自检宿主要拿 `QShade`/`QHit` 核「压暗命中区档 = 压暗层那一档
        //    且严格 < 本窗内容命中区最低档」这条不变量（`MenuDraw.ShadeRuleOk`）。
        // 🔴 **2026-10-08（A220）收窄**：脚本逐常量扫全工程 `.cs` 的**外部代码引用**（限定名
        //    `BoosterInfoPopup.<档>`，已剔注释）—— `QShade` **1 处** · `QHit` **2 处**
        //    （都在 `Editor/ShopScene.cs`「卡包详情窗」那一段：`MenuDraw.CheckShadeRule` 一条 +
        //    `CheckAbsorbRule` 一条；`QShade` 只出现在后者）⇒ 这两个留 `public`；
        //    同批一起放宽的另外 8 个 **外部 0 处** ⇒ 回 `const`（只有本类自用）。
        //    整段理由见 `BoosterPackOpenWindow.cs:93-103`。
        //    ⚠️ **`QBase` 不在本次收窄范围**（自建窗起就是 `public`，本批没放宽它）。
        public const int QShade = QBase;       // 3070 压暗整屏（`Menu Dark Background`）
        public const int QHit = QBase + 9;     // 3079 窗内命中区（关闭钮 + 价签 + `WebShop` + `Tooltip`）
        const int QBg = QBase + 1;             // 3071 窗底九宫（`UI_Deck_Information_Back`）
        const int QClose = QBase + 2;          // 3072 关闭钮（底 / `Background` / 图标）
        const int QArt = QBase + 3;            // 3073 卡包立绘（`Image`）
        const int QText = QBase + 4;           // 3074 文字（`Title` / `Category` / `Descripton` / `CrateCounter`）
        const int QBar = QBase + 5;            // 3075 保证进度条（bg / fill / end / outline）+ `Tooltip` 图标
        const int QBarText = QBase + 6;        // 3076 条上那个计数（`counter`）
        const int QBtn = QBase + 7;            // 3077 两颗钮（价签 / `WebShop`）的底与图标
        const int QBtnText = QBase + 8;        // 3078 钮上的字（`Button Text`）

        /// <summary>「点窗外关窗」那个命中区（`Menu Dark Background/CloseHit`）的档。
        ///
        /// 🔴 **判据 = `CLAUDE.md` §三「分层要用渲染队列，不能用 z」**：`ImageQuad.Create` 造出来的 quad
        /// **世界 z 恒为 0**（`LayoutSpace.ToWorld` 就给 0）⇒ 两条命中区**同队列**时，
        /// `PointerLayer.HitButton` 那句「同队列再比 z」就**退化成 `FindObjectsByType` 的枚举顺序** ⇒
        /// 谁吃到命中**不可控**。要唯一，只能靠**队列分档**。
        ///
        /// 🔴 **2026-10-03 实测（原先这里写的是 `QHit`，与窗内四个命中区同档）**：压暗层把
        /// `Tooltip` 图标、`Price Display`（**价签**）、`WebShop Button` 三个命中区**全抢走了**
        /// （`_tmp_view/shop.log:11896` 实得 `CloseHit`）—— 那两颗钮**点不动、点下去只会关窗**
        /// （关闭钮被抢看不出差别，因为两边的动作都是 `Close()`）。唯一性靠的就是这条分档。
        ///
        /// ⇒ 压暗层的命中区要**严格低于窗内所有命中区**（`QHit`），
        /// 同时**高于它下面那一层**（商店页命中区最高 `ShopWindow.QCellInfoHit = 3031` ⇒
        /// 点窗外仍然关窗、不会穿透到商品格上）。
        /// 同族既有做法（都把背景命中区压在自家内容命中区**之下**，只这一扇原来写错）：
        /// `BattleLogPopup:105` / `LeaderboardWindow:305` / `DuelPopupWindow:117` 用 `QPanel`（= 压暗层自己那一档）、
        /// `DeckSelectionPopup:294` / `ImportDeckPopup:99` 用 `QDsHit−1` / `QImpHit−1`、
        /// `PlayerProfileWindow:245` 用 `QShade+1`、`BoosterPackOpenWindow:95` 用 `QBase−1`
        /// （那儿写明「在卡命中区之下 ⇒ 卡还能点」）。
        /// ✅ **2026-10-04 订正：公共件已经做了** —— `MenuDraw.ShadeHit`（`Shell/MenuDraw.cs`，本批时在 `:1068`，
        /// 内部就是 `MenuDraw.Hit(..., qShade, ...)`，并带一条「`qShade >= qContentMin` 就当场告警」的
        /// 不变量），自检模板 = `MenuDraw.CheckShadeRule(CheckTrue, …, darkHit, darkVisual, qContentMin)`
        /// （⚠️ **2026-10-08（A221③）签名订正**：这里原写 `MenuDraw.ShadeRuleOk(…)` —— 那个 `(darkHit, qShade, qContentMin, out why)`
        /// 的旧签名第二参已由**调用方传的常量** `qShade` 换成**视觉压暗层** `darkVisual`，A77⑬③）。
        /// 本件这一处**编码本来就与之一致**
        /// （`QShadeHit = QShade`），接线改走 `ShadeHit` 归后面那批 —— ⛔ 别再说「没有公共件」。</summary>
        const int QShadeHit = QShade;

        // ============================================================ 真值（§五·二·一 那张表）

        static readonly PxRect DarkR = new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f);
        static readonly Color DarkTint = new Color(0f, 0f, 0f, 0.773f);   // `Menu Dark Background` 的色

        static readonly PxRect BgR = new PxRect(395.72f, 178.35f, 1547.28f, 895.80f);
        static readonly Vector4 BgBorder = new Vector4(42f, 363f, 655f, 81f);   // 九宫 42,363,655,81

        static readonly PxRect CloseR = new PxRect(1487.06f, 159.83f, 1561.45f, 235.44f);
        /// <summary>`Generic Close Button Orange` 的**两个子件**（底图/图标）同矩形、都 `preserveAspect`。</summary>
        static readonly PxRect CloseArtR = new PxRect(1495.20f, 167.80f, 1552.06f, 225.94f);

        /// <summary>`Artwork`（左半边整块）。`background` 比它内缩 10px、`foreground` 只在上边内缩 10px。</summary>
        static readonly PxRect ArtBgR = new PxRect(405.72f, 198.35f, 950.00f, 841.65f);
        static readonly PxRect ArtFgR = new PxRect(395.72f, 198.35f, 960.00f, 851.65f);

        static readonly PxRect TitleR = new PxRect(976.00f, 260.58f, 1492.72f, 312.58f);
        static readonly PxRect CategoryR = new PxRect(976.00f, 307.92f, 1492.72f, 352.92f);
        static readonly PxRect DescR = new PxRect(976.00f, 356.54f, 1476.00f, 611.24f);
        static readonly PxRect CrateCounterR = new PxRect(976.00f, 619.67f, 1461.28f, 664.67f);

        // 保底进度条：`Slider`(1004.4,664.6→1411.0,714.6) 下四件 + `counter` + `Tooltip`
        static readonly PxRect SliderBgR = new PxRect(1004.40f, 674.60f, 1411.00f, 704.60f);
        static readonly PxRect SliderFillAreaR = new PxRect(1004.40f, 677.40f, 1411.00f, 701.90f);
        static readonly PxRect SliderCounterR = new PxRect(1085.70f, 679.58f, 1329.66f, 704.58f);
        static readonly PxRect TooltipR = new PxRect(1411.00f, 667.18f, 1455.88f, 712.09f);
        static readonly Color BarBgTint = new Color(0.708f, 0.468f, 0.35f, 1f);     // `40k_campaign_bar_bg` 的色
        static readonly Color BarOutlineTint = new Color(1f, 0.626f, 0f, 1f);       // `40k_campaign_bar_outline` 的色
        static readonly Color BarFillTint = new Color(1f, 0.509f, 0f, 1f);          // `40k_campaign_bar_fill` 的色
        static readonly Vector4 BarBgBorder = new Vector4(20f, 0f, 20f, 0f);
        static readonly Vector4 BarFillBorder = new Vector4(10f, 0f, 10f, 0f);

        // 底部两个钮（`Purchase buttons` 是 HLG spacing 15 ⇒ 下面两个矩形是**布局跑完的实测值**）
        static readonly PxRect PriceR = new PxRect(963.50f, 716.30f, 1195.67f, 787.30f);
        static readonly PxRect WebShopR = new PxRect(1210.70f, 725.90f, 1451.94f, 777.75f);
        static readonly PxRect WebGlowR = new PxRect(1173.70f, 687.30f, 1488.93f, 816.40f);
        static readonly PxRect WebIconR = new PxRect(1239.34f, 725.90f, 1291.22f, 777.75f);
        /// <summary>🔴 **2026-10-07（A77⑫④）就地重算**：旧值 `1291.22 → 1423.22`（= `WebIconR` 的右沿起算）**左右两端都偏**。
        /// 重算命令（现读）= `python 工具/menu_dump.py bundle_menus_assets_all "Booster Info Popup" --depth 16 --md`
        /// ⇒ `WebShop Button > Button Text` = **1296.42,725.86→1428.42,777.74**（宽 132.00 不变）。
        /// **为什么变了**：`WebShop Button` 是 `HorizontalLayoutGroup`（`scaleW=1`）而前一件 `Icon` 的
        /// `m_LocalScale = 1.2`（dump 那行标着 `×1.2 → 视觉 62.26×62.26`）⇒ 推进量 = `51.88 × 1.2 = 62.256`
        /// （不是 `51.88`），而**组内居中**的起始偏移按**乘过缩放**的 requiredSpace 折半 ⇒ 净位移
        /// `51.88 × (1.2 − 1) ÷ 2` = **+5.19**（`1291.22 + 5.19 ≈ 1296.42` ✓；
        /// 也对得上「文字左沿 = 图标**视觉**框右沿 1239.34 + 62.26 − 5.19 = 1296.41」）。
        /// 旧值是**旧工具**的读数（`rect_of` 的 `scale` 还是死参）。</summary>
        static readonly PxRect WebTextR = new PxRect(1296.42f, 725.86f, 1428.42f, 777.74f);
        static readonly Color PriceTint = new Color(0.902f, 0.637f, 0.18f, 1f);     // `40K_button` 的色
        static readonly Color WebBtnTint = new Color(0.333f, 0.878f, 0.336f, 1f);   // `40K_button` 的色
        static readonly Color GlowTint = new Color(1f, 1f, 1f, 0.8f);

        // ---- prefab 自带的出厂文本（**原样用，不自己编词** —— 见文件头）----
        /// <summary>`Descripton` 的出厂文本。⚠️ 它点名 `Sautekh`（原版运行期由 item 文案覆盖）。</summary>
        public const string DescSample =
            "Contains 5 cards for the Sautekh army.\n\nAt least one of the cards is guaranteed to be Rare or better" +
            "\n\nCheck FAQ for more information on Drop Rates.";
        /// <summary>`CrateCounter` 的出厂文本（**末尾那个空格是原文就有的**）。</summary>
        public const string CrateCounterText = "Boosters opened since last Legendary ";
        /// <summary>保底进度条的出厂占位值（原版由服务端给）。</summary>
        public const string CounterSample = "100/200";
        /// <summary>`WebShop Button/Button Text` 的出厂文本。</summary>
        public const string WebShopText = "Save More!";

        // ============================================================ 状态

        public int Page { get; private set; }
        public int Index { get; private set; }

        /// <summary>开这一扇的宿主商店窗（`Buy` 要走它身上那条传奇确认闸门）。由入口在 `OpenWindow` 之前塞。</summary>
        public ShopWindow Host;

        /// <summary>最近一次开出来的那一扇（自检用）。</summary>
        public static BoosterInfoPopup LastOpened { get; private set; }

        /// <summary>自检用：这一扇窗现在显示的三段字。</summary>
        public string ShownTitle { get; private set; }
        public string ShownCategory { get; private set; }
        public string ShownPrice { get; private set; }

        /// <summary>取不到的图（出声用 —— 红线：不许静默失败）。</summary>
        public readonly System.Collections.Generic.List<string> MissingArt =
            new System.Collections.Generic.List<string>();

        // 自检要按名字找的几个节点
        public Transform DarkNode { get; private set; }
        public Transform ArtBgNode { get; private set; }
        public Transform ArtFgNode { get; private set; }
        public GameObject SliderBgGo { get; private set; }
        public GameObject SliderFillGo { get; private set; }
        public Transform WebShopNode { get; private set; }
        public Transform TooltipNode { get; private set; }
        /// <summary>保底条画出来的**填充宽度**（px）—— 自检比它比 `counter` 的比例。</summary>
        public float SliderFillW { get; private set; }

        Texture2D Art(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            var t = CardArt.MenuUi(n);
            if (t == null && !MissingArt.Contains(n)) MissingArt.Add(n);
            return t;
        }
        static Transform Node(Transform p, string n, PxRect r) { return MenuDraw.Node(p, n, r); }
        ImageQuad Rect(Transform p, string art, PxRect r, string n, int q, Color? tint = null, bool keepAspect = false)
        {
            // ⚠️ `MenuDraw.Rect` **tex == null 就直接 return null** ⇒ 纯色件（原版那种「没 sprite、只有 m_Color」）
            //    必须传 `CardArt.Solid()`（同 `BattleLogPopup.Rect` 那个写法）。
            //    🔴 **2026-10-03 踩过**：这里原来传的是 `null` ⇒ **压暗层根本没画出来**，
            //       而自检那条「压暗层 α = 0.773」把它抓了出来（实得 0.00）。
            var t = string.IsNullOrEmpty(art) ? CardArt.Solid() : Art(art);
            return MenuDraw.Rect(p, t, r, n, q, tint, keepAspect);
        }
        GameObject Nine(Transform p, string art, PxRect r, Vector4 b, string n, int q, Color? tint = null)
        {
            var t = Art(art);
            return t == null ? null : MenuDraw.Nine(p, t, r, b, t.width, t.height, q, tint, true, n);
        }

        // ---------------------------------------------------------- 开

        public static BoosterInfoPopup Create(WindowsManager mgr)
        {
            var go = new GameObject("Booster Info Popup");
            var win = go.AddComponent<BoosterInfoPopup>();
            win.type = WindowType.Popup;                        // 实证 type = 1
            win.placement = WindowsPlacement.World;             // 实证 windowsPlacement = 10（**不是 15**）
            win.closeOnEsc = true;                              // 实证 closeOnESC = 1
            win.extraScaleSmallScreen = 1.2f;                   // 实证 1.2
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            LastOpened = this;
            Build();
        }

        /// <summary>开哪一件（照 `CardDetailPopup.ShowCard` 那条先例：先 `OpenWindow` 再喂数据）。</summary>
        public void Show(int page, int index)
        {
            Page = page; Index = index;
            Build();
        }

        // ---------------------------------------------------------- 建

        public void Build()
        {
            MenuDraw.ClearChildren(transform);
            MissingArt.Clear();

            var offers = ShopData.Offers(Page);
            if (offers == null || Index < 0 || Index >= offers.Length)
            {
                Debug.LogWarning("[BoosterInfo] 商品下标越界（page=" + Page + " idx=" + Index + "）⇒ 不铺内容");
                return;
            }
            var o = offers[Index];

            // ① 压暗层（**点它关窗** = 原版 `BackgroundCloseButton`）
            DarkNode = Node(transform, "Menu Dark Background", DarkR);
            Rect(DarkNode, null, DarkR, "Image", QShade, DarkTint);
            // 🔴 队列用 **`QShadeHit`（压暗层自己那一档）**，**不是 `QHit`** —— 见 `QShadeHit` 的注释：
            //    同档 ⇒ `ImageQuad` 的世界 z 恒为 0 ⇒ `PointerLayer` 只能靠枚举顺序挑赢家，
            //    压暗层会把窗内四个命中区（关闭钮/价签/`WebShop`/`Tooltip` 图标）全抢走。
            // 🆕 **2026-10-04（A47 接线批）**：改走公共件 `MenuDraw.ShadeHit`（档与内容档现场核，
            //    不一样就当场告警）。**行为一字未改** —— `QShadeHit` 本来就 = `QShade`。
            MenuDraw.ShadeHit(DarkNode, DarkR, QShadeHit, QHit, () => Close(), "CloseHit");

            // ② 窗底（`Generic Window Red Background Big` = `UI_Deck_Information_Back` 九宫）
            var window = Node(transform, "window", new PxRect(395.72f, 188.35f, 1524.28f, 851.65f));
            Nine(window, "UI_Deck_Information_Back", BgR, BgBorder, "Generic Window Red Background Big", QBg);
            // 🆕 **2026-10-06（A94）：窗底那块面板吸收点击**。判据 = 原版 prefab
            //   `Booster Info Popup > window > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到面板自己，
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 挂在压暗层上）⇒ **什么都不做**。
            MenuDraw.Absorb(transform, "AbsorbHit", BgR, QShadeHit, QHit);

            // ③ 关闭钮：原版这一件的**底图本身 `m_Enabled=1`**（与 `Battle Log Popup` 那条不同！），
            //    它自己是 `UI_Button_Round_background`，下面再叠两个子件。
            var close = Node(window, "Generic Close Button Orange", CloseR);
            // 🔴 **换图落在【圆底那一层】** —— 实测原版该件三层的结构是
            //    `Image`(=`UI_Button_Round_background`, 237²) + `Background`(=`40k_general_bt_yellow`) + `Icon`(=`…_close`)，
            //    而 `trans=2` 换的是**它自己那个 Image** ⇒ 悬停把圆底换成 `40k_general_bt_yellow_hover`
            //（2026-10-03 直接读 prefab 核过：`menu_dump.py bundle_menus_assets_all "Booster Info Popup" --depth 5`）。
            var baseQ = Rect(close, "UI_Button_Round_background", CloseArtR, "Base", QClose, null, true);
            var cb = Node(close, "Background", CloseArtR);
            Rect(cb, "40k_general_bt_yellow", CloseArtR, "Background", QClose, null, true);
            var ci = Node(close, "Icon", CloseArtR);
            Rect(ci, "40k_general_bt_yellow_close", CloseArtR, "Icon", QClose, null, true);
            MenuDraw.Hit(close, "Hit", CloseR, QHit, () => Close(), baseQ, null, "40k_general_bt_yellow_hover");

            // ④ `Artwork` —— 主图放 `background`（原版两处都空、运行期赋图；见文件头）
            var artwork = Node(window, "Artwork", new PxRect(395.72f, 188.35f, 960.00f, 851.65f));
            ArtBgNode = Node(artwork, "background", ArtBgR);
            var bgQ = Rect(ArtBgNode, o.Art, ArtBgR, "Image", QArt, null, true);
            if (bgQ == null)
                Debug.LogWarning("[BoosterInfo] ⚠️ 商品「" + o.Name + "」的主图 `" + (o.Art ?? "<空>")
                                 + "` 取不到 ⇒ 这一块**空着**（不拿别的图冒充）");
            ArtFgNode = Node(artwork, "foreground", ArtFgR);          // 原版也是空图 ⇒ 只建节点

            // ⑤ 右半边四段字 + 进度条 + 两个钮
            var text = Node(window, "Text", new PxRect(960.00f, 204.40f, 1508.28f, 835.60f));
            BuildTexts(text, o);
            BuildGuaranteeBar(text);
            BuildButtons(text, o);

            if (MissingArt.Count > 0)
                Debug.LogWarning("[BoosterInfo] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            Debug.Log("[BoosterInfo] 开了「" + o.Name + "」的卡包详情窗"
                      + "（⚠️ 入口是**我们定的**：点商店格的商品主图；`Category`/`Descripton`/保底计数"
                      + " 原版来自服务端 item ⇒ 见 `BoosterInfoPopup.cs` 文件头）");
            HintGaps();
        }

        /// <summary>`Title` / `Category` / `Descripton` 三段（字号/对齐/字距照 §五·二·一）。</summary>
        void BuildTexts(Transform text, ShopOffer o)
        {
            ShownTitle = o.Name;
            ShownCategory = CategoryFor(o);

            var title = MenuDraw.Text(text, TitleR, ShownTitle, Color.white, "Title", 40f, QText);
            if (title != null)
            {
                MenuDraw.AlignLeft(title, TitleR);
                title.SetAutoFitBox(LayoutSpace.Px(TitleR.W), LayoutSpace.Px(TitleR.H), 3f, 40f);
            }

            // ⚠️ 字距 **−1.8**（原版 `m_characterSpacing`，原样传）
            var cat = MenuDraw.Text(text, CategoryR, ShownCategory, Color.white, "Category", 39f, QText);
            if (cat != null)
            {
                MenuDraw.AlignLeft(cat, CategoryR);
                cat.SetCharSpacing(-1.8f);
                cat.SetAutoFitBox(LayoutSpace.Px(CategoryR.W), LayoutSpace.Px(CategoryR.H), 3f, 39f);
            }

            var desc = MenuDraw.Text(text, DescR, DescFor(o), Color.white, "Descripton", 35f, QText);
            if (desc != null)
            {
                MenuDraw.AlignLeft(desc, DescR);
                desc.SetAutoFitBox(LayoutSpace.Px(DescR.W), LayoutSpace.Px(DescR.H), 3f, 35f);
            }

            // ⚠️ 字距 **−2**；对齐是 **Center**（原版这一条与上面三条都不同）
            var cc = MenuDraw.Text(text, CrateCounterR, CrateCounterText, Color.white, "CrateCounter", 35f, QText);
            if (cc != null)
            {
                MenuDraw.AlignLeft(cc, CrateCounterR);
                cc.SetCharSpacing(-2f);
                cc.SetAutoFitBox(LayoutSpace.Px(CrateCounterR.W), LayoutSpace.Px(CrateCounterR.H), 3f, 35f);
            }
        }

        /// <summary>保底进度条（`Slider` + `ProgressBar`）：底 / 填充 / 描边 / 计数 / 说明图标，五件。
        /// 🔴 **原版的填充宽 = 值比例 × 406.58**（`m_FillRect` = `Fill`，`Fill Area` 是它的锚框）；
        ///    `end` 那枚端帽挂在 `Fill` 下、跟着填充右端走（实测：Fill 宽 0 时端帽中心在 x = 1004.4 − 9.0）。</summary>
        void BuildGuaranteeBar(Transform text)
        {
            var slider = Node(text, "Booster pack guarantee Slider", new PxRect(1004.40f, 664.60f, 1411.00f, 714.60f));

            var barBg = Node(slider, "Background", SliderBgR);
            SliderBgGo = Nine(barBg, "40k_campaign_bar_bg", SliderBgR, BarBgBorder, "Image", QBar, BarBgTint);

            float frac = ValueFraction(CounterSample);
            var fillArea = Node(slider, "Fill Area", SliderFillAreaR);
            float fx2 = SliderFillAreaR.x1 + SliderFillAreaR.W * frac;
            var fillR = new PxRect(SliderFillAreaR.x1, SliderFillAreaR.y1, fx2, SliderFillAreaR.y2);
            var fill = Node(fillArea, "Fill", fillR);
            SliderFillW = fillR.W;
            SliderFillGo = Nine(fill, "40k_campaign_bar_fill", fillR, BarFillBorder, "Image", QBar, BarFillTint);
            // `end`：实测尺寸 29.44 × 31.86、中心相对填充右端偏移 (−9.0, +13.3)
            var endR = new PxRect(fx2 - 9.0f - 29.44f * 0.5f, SliderFillAreaR.CY + 13.3f - 31.86f * 0.5f,
                                  fx2 - 9.0f + 29.44f * 0.5f, SliderFillAreaR.CY + 13.3f + 31.86f * 0.5f);
            var end = Node(fill, "end", endR);
            Rect(end, "40k_campaign_bar_end", endR, "Image", QBar, null, true);

            var cnt = MenuDraw.Text(slider, SliderCounterR, CounterSample, Color.white, "counter", 26.35f, QBarText);
            if (cnt != null)
            {
                MenuDraw.AlignLeft(cnt, SliderCounterR);
                cnt.SetAutoFitBox(LayoutSpace.Px(SliderCounterR.W), LayoutSpace.Px(SliderCounterR.H), 12f, 35f);
            }

            var outline = Node(slider, "Outline", SliderBgR);
            Nine(outline, "40k_campaign_bar_outline", SliderBgR, BarBgBorder, "Image", QBar, BarOutlineTint);

            // `Tooltip`（`40k_generic_bt_info`）：图标 + **悬停出 tooltip**（2026-10-03 A12-P1 接线；见 `BuildTooltipHit`）
            TooltipNode = Node(slider, "Tooltip", TooltipR);
            Rect(TooltipNode, "40k_generic_bt_info", TooltipR, "Image", QBar);
            BuildTooltipHit();
        }

        // ============================================================ tooltip（原版 `EverguildTooltipTrigger`）

        /// <summary>正文取值 = 原版那个触发器的 I2 词条（`MenuShop/BoosterInfo/LegendaryTooltip`）；
        /// **词条表在远端 CCD ⇒ 本地没有** ⇒ **留空**（同 `ForgeTab.HelpTipBody` 的做法：版式照做、文案留空并说一声）。
        /// ⚠️ **一个空格不是文案**：`Tooltip.Show("")` 的契约是「空串 = 不显示」（`Core/Tooltip.cs:120`）
        /// ⇒ 用空格让**面板照原版的时机弹出来、里面是空的**；断言 `ShownBody.Trim() == ""` 照样成立。
        /// 🔴 两块（本窗 + 锻造厂）的这条做法是**同一句口径写了两遍**（两个文件都不许碰公共件）——
        ///    要收口就收进 `MenuWindowBase`/`Tooltip`，**别在第三处再写一遍**。</summary>
        public const string TipBody = " ";

        /// <summary>自检用：这个图标被悬停过几次。</summary>
        public static int TipHovers { get; private set; }
        /// <summary>自检用：这个图标的命中区（`PointerLayer.HoverAt` 打到它才算接线通）。</summary>
        public static WindowButton TipHit { get; private set; }

        /// <summary>接线。**原版触发器的实测字段**（GameObject `Tooltip`，父链
        /// `Tooltip < Booster pack guarantee Slider < Text < window < Booster Info Popup`）：
        /// `text = "MenuShop/BoosterInfo/LegendaryTooltip"` · `localize = 1` · `title = ""` ·
        /// `tooltipAnchor = **0**（= None，居中）` · `offset = (0,0,0)` · `registerEvents = 1` ·
        /// `preventPassingClickEventToParent = 0`（⇒ 点它什么都不做）·
        /// **`m_RaycastPadding = (−15,−15,−15,−15)`** · `preventPassingClickEventToParent = 0`（⇒ 点它什么都不做）。
        /// 🔴 最后那条**照做**（这一件只有 44.88²，外扩 15 后才是 74.88²）。
        /// **「负值 = 外扩」怎么证的**（`RectTransformUtility` 的源码不在本地包里 ⇒ 不能直接读）：
        ///   ① 全包 11880 个带 `m_RaycastPadding` 的件里 11672 个是 (0,0,0,0)，**非零的几乎全是负值**
        ///      （−10/−11/−12/−15/−20/−25/−30/−40；唯一一个正的在一件 246.8,84.4,338.6,132.4 的怪件上）；
        ///   ② **反证**：`Mission Progress Bar/Handle Slide Area/Handle` 的 size 是 **4.141 × 50.597**，
        ///      它的 padding 是 **−25** —— 若负值是**内缩**，那个可拖拽的滚动条把手会变成
        ///      **−45.86 × 0.597**（判不中）⇒ 原版自己的滚动条就废了 ⇒ **只能是外扩**。
        ///   ③ 同一族的旁证：`Generic Close Button Orange/Icon` 的 size 是 0.377²（退化子件）、padding −20 ——
        ///      外扩到 40.38² 才等于那颗关闭钮真正的可点范围。</summary>
        void BuildTooltipHit()
        {
            var hr = new PxRect(TooltipR.x1 - 15f, TooltipR.y1 - 15f, TooltipR.x2 + 15f, TooltipR.y2 + 15f);
            var hit = MenuDraw.Hit(TooltipNode, "Hit", hr, QHit, null);
            if (hit == null) return;
            var wb = hit.GetComponent<WindowButton>();
            if (wb == null) return;
            wb.onEnter = ShowTip;
            wb.onExit = Tooltip.Hide;
            TipHit = wb;
        }

        void ShowTip()
        {
            TipHovers++;
            if (TipHovers == 1)
                Debug.Log("[BoosterInfo] `Tooltip` 悬停：原版正文是 I2 词条 `MenuShop/BoosterInfo/LegendaryTooltip`"
                          + "（本地没有，见 `TipBody`）⇒ **面板照弹、正文是空的**。要真文案只需把 `TipBody` 换成那份词条值。");
            // 原版 `tooltipAnchor = 0`（居中）· `offset = (0,0,0)` —— 原样传
            Tooltip.Show(TipBody, LayoutSpace.FromPixel(TooltipR.CX, TooltipR.CY), 0, Vector3.zero);
        }

        /// <summary>底部两个钮（原版 `Purchase buttons` 是 `HorizontalLayoutGroup` spacing 15 ⇒
        /// 我们**直接照布局跑完的实测矩形摆**，不自己算布局）。</summary>
        void BuildButtons(Transform text, ShopOffer o)
        {
            var row = Node(text, "Purchase buttons", new PxRect(947.30f, 721.60f, 1468.10f, 782.00f));

            // `Price Display` → `Generic UI Button`（`40K_button` · PA · 金色）
            var pd = Node(row, "Price Display", PriceR);
            var gb = Node(pd, "Generic UI Button", PriceR);
            var priceQ = Rect(gb, "40K_button", PriceR, "Image", QBtn, PriceTint, true);
            ShownPrice = o.Price;
            var pt = MenuDraw.Text(gb, PriceR, ShownPrice, Color.white, "Button Text", 40f, QBtnText);
            if (pt != null) MenuDraw.AlignRight(pt, PriceR);          // 原版 `Button Text` 是 Right（格内那份）
            MenuDraw.Hit(pd, "Hit", PriceR, QHit, () => Buy(), priceQ, "40K_button");

            // `WebShop Button`：自己的 HLG（spacing 0）⇒ 三个子件摆放照实测
            var ws = Node(row, "WebShop Button", WebShopR);
            WebShopNode = ws;
            var glow = Node(ws, "Highlight", WebGlowR);
            Nine(glow, "OctagonUI_Filled_Fade_SDF", WebGlowR, new Vector4(52f, 52f, 52f, 52f), "Image", QBtn, GlowTint);
            var bi = Node(ws, "Button Image", WebShopR);
            // A17：`m_TargetGraphic` 指**子件 `Button Image`**（普查 §块 2 第 5 行）⇒ 换图落在这张上
            var wsQ = Rect(bi, "40K_button", WebShopR, "Image", QBtn, WebBtnTint, true);
            // `Icon`：矩形 51.88² **× `localScale 1.2`** ⇒ 实际画出来是 62.26²（同中心放大）
            const float iconScl = 1.2f, iconSide = 51.88f * iconScl;
            var iconR = new PxRect(WebIconR.CX - iconSide * 0.5f, WebIconR.CY - iconSide * 0.5f,
                                   WebIconR.CX + iconSide * 0.5f, WebIconR.CY + iconSide * 0.5f);
            var ic = Node(ws, "Icon", iconR);
            Rect(ic, "40K_Icon_Discount_Gold", iconR, "Image", QBtn);
            var wt = MenuDraw.Text(ws, WebTextR, WebShopText, Color.white, "Button Text", 34.2f, QBtnText);
            if (wt != null) wt.SetAutoFitBox(LayoutSpace.Px(WebTextR.W), LayoutSpace.Px(WebTextR.H), 12f, 38f);
            // 🔴 **2026-10-07（A62 主表 #14）**：原版 `Booster Info Popup > … > WebShop Button > Button Text`
            //   （`'Save More!'`）是 **`折行=0 auto[12~38]`**（判据 = `python 工具/menu_dump.py bundle_menus_assets_all
            //   "Booster Info Popup" --depth 14 --md`，该行 `折行=0`）—— `SetAutoFitBox` 内部会**无条件开折行** ⇒ 显式关掉
            //   （A205：关这一下顺带把版面推下去，否则字段变了、画面没变）。
            if (wt != null) wt.SetWrapping(false);
            MenuDraw.Hit(ws, "Hit", WebShopR, QHit, () =>
                Debug.Log("[BoosterInfo] `WebShop Button`：原版是**打开外部 WebShop（真钱）** —— "
                          + "我们**不做真实经济**（用户 2026-09-17 边界②）⇒ **只出声、不跳转**"), wsQ, "40K_button");
        }

        /// <summary>买（与商店格同一条路：先过传奇确认 → `ShopData.Buy`）。
        /// 🔴 **走开我们那扇 `ShopWindow`**（传奇确认那条闸门在它身上）—— 所以入口把宿主存进 `Host`。</summary>
        public void Buy()
        {
            // ⚠️ `Buy` 在**页**身上（`ShopTabPage.Buy`，传奇确认那道闸门也在那儿）—— 不是窗口身上
            var pg = Host != null ? Host.PageOf(Page) : null;
            if (pg != null)
            {
                string got = pg.Buy(Index);
                Debug.Log("[BoosterInfo] 走商店那条购买链：`" + got + "`");
                return;
            }
            Debug.Log("[BoosterInfo] 没有宿主 `ShopWindow`/那一页 ⇒ 直调 `ShopData.Buy`"
                      + "（**不做真实经济**：资源固定 9999、不扣钱）");
            Debug.Log("[BoosterInfo] " + ShopData.Buy(Page, Index));
        }

        // ---------------------------------------------------------- 数据

        /// <summary>`Category` 那一栏 —— **我们拼的**（原版来自服务端 item）。
        /// `Rarity == 4` ⇒ 原文那档 `Legendary Booster Pack`，否则用商品表的 `Type`。</summary>
        public static string CategoryFor(ShopOffer o)
        {
            return o.Rarity == 4 ? "Legendary Booster Pack" : o.Type;
        }

        /// <summary>`Descripton` 那一栏 —— **prefab 的出厂文本**（见文件头；要换真文案只改这一处）。</summary>
        public static string DescFor(ShopOffer o) { return DescSample; }

        /// <summary>`"100/200"` → `0.5`（`ProgressBar` 的填充比例；原版由 `m_Value` 给）。</summary>
        public static float ValueFraction(string counter)
        {
            if (string.IsNullOrEmpty(counter)) return 0f;
            int k = counter.IndexOf('/');
            if (k <= 0) return 0f;
            float a, b;
            if (!float.TryParse(counter.Substring(0, k), out a)) return 0f;
            if (!float.TryParse(counter.Substring(k + 1), out b) || b <= 0f) return 0f;
            return Mathf.Clamp01(a / b);
        }

        /// <summary>把「原版有、我们没做」的三处**当场出声**（红线：不许静默失败）。</summary>
        void HintGaps()
        {
            Debug.Log("[BoosterInfo] ⚠️ 三处如实标注：① `Category`/`Descripton`/保底计数**原版来自服务端 item**"
                      + "（我们用的是**拼的 / prefab 出厂文本 / 出厂占位**）；② `foreground` 与 `Highlight` 的"
                      + "**辉光动画没做**（前者原版空图、后者是 `UIBorderGlow`）；"
                      + "③ `Tooltip` 的 tooltip **接线做了（悬停出面板）、正文是空的** ——"
                      + "原版那件挂 `EverguildTooltipTrigger`、正文取 I2 词条 `MenuShop/BoosterInfo/LegendaryTooltip`，"
                      + "**词条表在远端 CCD、本地一个 value 都没有** ⇒ 不自己编（2026-10-03 A12-P1）");
        }

        public string Dump()        {
            return "BoosterInfo[" + Page + "/" + Index + "] 「" + ShownTitle + "」· " + ShownCategory
                   + " · " + ShownPrice + " · 保底条填充 " + SliderFillW.ToString("0.0") + "px"
                   + " · 取不到的图 " + MissingArt.Count + " 张";
        }
    }
}
