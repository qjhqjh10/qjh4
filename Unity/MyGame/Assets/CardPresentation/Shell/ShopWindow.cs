// ShopWindow.cs — 阶段二第 3 层第 4 件：商店（原版 `Shop Menu Variant` + 三个页签）
//
// ============================ 出处（唯一正本） ============================
// 本文件里**每一个矩形/字号/颜色/sprite 名都是直读原版资产**，工具与命令：
//   `工具/menu_rect.py bundle_menus_assets_all "<名>" …`  ·  `工具/menu_dump.py bundle_menus_assets_all "<名>" …`
//   ⚠️ 三个页签 prefab（`Card Shop Tab` / `Daily Shop Tab` / `Item Shop Tab`）的 **`m_Father = 0`**
//      —— 它们是**独立 prefab 根**、运行期才被 `ShopWindow.CreateStoreTab` 实例化进 `Tabs`，
//      所以量它们要 `--size 1752.83x1009.06 --relative` 再统一 **+(167.17, 70.94)**（= `Tabs` 的左上角）。
//      交叉验证：这样算出来的 `Packs Scroll View` 与直接量 `Shop Menu Variant` 的同一节点**逐位相同**。
//
// ---- 🔴 窗口参数（`Shop Menu Variant` 的 MB 原文，与奖励窗**不同**，别互推）----
//   `type = 0 (Fullscreen)` · `windowsPlacement = 10 (World)` · `closeOnESC = 1` ·
//   `updateNavPanel = 1` · `extraScaleSmallScreen = 1.0`。
//
// ---- 🔴 三个页签是「同一套树的三份拷贝」----
//   `Card Shop Tab` / `Daily Shop Tab` / `Item Shop Tab` **逐节点同构**（节点数 71、结构一致，
//   但每个节点的 RT/GO pid 各不相同 = 复制粘贴）；**唯一的结构差**在
//   `daily shop header/TimeCounter` 的第一个子件：
//     · `Card Shop Tab` → TMP **`Refreshes in:`**
//     · `Daily Shop Tab` → TMP `Atualiza em:`（⚠️ **葡语占位串**，别当英文抄）
//     · `Item Shop Tab` → **`Clock Icon`**（`WF_icon_clock`，`LayoutElement.preferredWidth = 26`）
//   ⇒ 我们三个页**共用一套 View**，那个子件用一个开关（`ShopData.PageSpec.TimerAsText`）。
//
// ---- 🔴 驱动链（反编译，`d:/2/tools/decomp_full/`）----
//   `ShopWindow__SetupTabs` 按**存档里的 store 列表**逐个建页签 · `__CreateStoreTab` 载 prefab 并建签 ·
//   `__GetStartingTab` 决定默认落在哪一签 · `ShopTab__Setup` 设 `GridLayoutGroup.cellSize`
//   （= 预制默认 × `contentLayoutElementsSizeMultiplierSmallScreens` **1.18**，**只在小屏时乘**）·
//   `ShopTab__SetStore` 总开关计时器 · `ShopTab__OnOpen` → `RefreshOffers` → `CreateOffers`
//   （把报价卡实例化进 **`mountPoint` = `Content`**）· `ItemShopTab__DoOfferReordering` 按 `itemOrder` 排序
//   （`0` = **整个重排跳过**，见 `Item Shop Tab No Automatic Ordering`）。
//   ⚠️ **反编译有撞名丢产物**：`ItemShopTab__.ctor.c` 里装的是 `BoosterShopTab___ctor`；
//      `ShopTab__ToFocus.c` 与 `ShopTab__OnOpen.c` 内容相同 ⇒ **这两处读不出东西，别当结论**。
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **页签表**：原版由**服务端 store 列表**驱动，prefab 里**只序列化了一个母版键**
//     （label `Pacotes`、图标 `40K_shop_bt_boosters`）⇒ 我们的三个键**按三个页 prefab 建**，
//     **图标从那 7 张 `*_shop_bt_*` 里挑**（见 `ShopData.Pages` 的注释）。
//   · **商品数据**（有哪些报价、多少钱、拥有几张、限购几次）**全是我们编的** —— 原版在 PlayFab。
//     按用户边界②（不做真实经济）⇒ **买了不扣钱**，只记账 + 打日志。
//   · 🆕 **2026-10-03（§三第29条 A6）：`Booster Info Popup` 的入口是我们定的** ——
//     点 `Booster Pack` 格子的**商品主图**开那扇窗。原版走 `ShopOfferContainer.OnClick → OpenContainer`
//     （从 offer 取 AssetGroup 0xc 的窗口），**那一族我们没有**；`CatalogItemContainer.OnInitialize`
//     只画抽屉、不开窗 ⇒ 见 `Shell/BoosterInfoPopup.cs` 文件头（那里是判据正本）。
//   · **格里的「名字 / 类型」两行**：原版由 `Card Drawer` 把卡画出来（⚠️ 本行原括注「我们还没有那套抽屉」
//     **已作废** —— 见下面那条更正痕迹；留着它就和相邻两行打架了）
//     ⇒ 这里**我们直接画两行字**顶在那，标明是**我们加的**。
//     ⚠️ **2026-10-03 就地更正（铁律 5）**：这条原来写的是「我们还没有那套抽屉」—— **当天 A8 把那条链接上了**
//     （主图改走 `ItemDrawer.Draw(..., DrawerOverride.Shop, ...)`，见 `BuildCell`）。
//     **但这两行字仍然是我们加的**：原版那两行是 `Card Drawer` 画在**卡面**上的，
//     🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来接着写「而我们工程里没有 `ItemDrawerConfig` 那张
//     『类型 → 抽屉 prefab』表」—— **假的**：那张表（SO）**2026-10-04 已整张解出**
//     （`资料/普查产出_1004/ItemDrawerConfig_映射表.md`），`ItemDrawer.ItemTypeSets` 现在就住着我们这份；
//     我们缺的是**那个抽屉变体本身**（`Card Drawer` 那个变体**没建** ⇒ 抽屉那一侧画出来的是
//     **我们自建的那 4 个抽屉**，见 `ItemDrawer.cs` 文件头）。
//     ⇒ 这一条的后半句仍然成立，前半句（「还没有那套抽屉」）已作废。
//   · **`TimedOffer` / `New` 两个角标不建**：出厂 INACT，而且按原版锚点算出来落在**格外面**
//     （y −151..−101，见 `menu_rect`），运行时位置无从查证 ⇒ **不建**（纪律①）。
//   · **`Line`（出厂 INACT）/ `WebShop Button Square Variant`（出厂 INACT）/ `price-bg`（`m_Enabled=0`）
//     不建**。
//   · **滚动与 `RectMask2D`**：三个页的 `Viewport` 都带 `RectMask2D`（softness y 25）—— 纵向滚动 +
//     硬裁 + **软边**都接了（2026-10-04 补软边，见 `PacksSoft`）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>商店。原版 `ShopWindow : GameWindowWithTabs`。</summary>
    public class ShopWindow : MainMenuSubmenuWindow
    {
        // ============================================================ 页签

        /// <summary>左栏键表：三个页 + **一个母版**（`TabButtons.tabButtonPrefab`，`Initialize` 一进来就关掉）。
        /// 🔴 **这张表是我们定的**（原版由服务端 store 列表驱动）—— 见文件头。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            new TabBtnSpec(ShopData.Pages[0].Icon, ShopData.Pages[0].Label, 36f, 5f, 36f, "ShopMenu_CardsButton"),
            new TabBtnSpec(ShopData.Pages[1].Icon, ShopData.Pages[1].Label, 36f, 5f, 36f, "ShopMenu_DailyButton"),
            new TabBtnSpec(ShopData.Pages[2].Icon, ShopData.Pages[2].Label, 36f, 5f, 36f, "ShopMenu_ItemsButton"),
            // 第 4 个 = 母版（原版 `TabButtons.tabButtonPrefab`）：**照建但关着**。
            new TabBtnSpec(ShopData.MasterKeyIcon, "Booster Packs", 25.65f, 12f, 33f, "", 47.9f),
        };

        public ShopWindow()
        {
            // 左栏的**视觉顺序** = 三个页 + 母版（母版那格是 None ⇒ 点它走 `NotifyNotBuilt`）
            visualTypes.Clear();
            visualTypes.Add(WindowTabType.ShopCards);
            visualTypes.Add(WindowTabType.ShopDaily);
            visualTypes.Add(WindowTabType.ShopItems);
            visualTypes.Add(WindowTabType.None);
        }

        public static ShopWindow Create(WindowsManager mgr)
        {
            // 🔴 **2026-10-11（A218）**：窗口根是 `RectTransform` ＋ 写 `sizeDelta`。
            //    判据 = 原版同名 prefab 实读：`Shop Menu Variant` 的 `RectTransform`
            //    `anchor (0,0)-(1,1)` · `sizeDelta (0,0)` · pivot (0.5,0.5) · **绝对矩形 (0,0)-(1920,1080)**
            //    （`bundle_menus_assets_all`，2026-10-11 现读）⇒ 整屏矩形。
            //    ⚠️ 原版靠 stretch 拿父（Canvas）的尺寸 ⇒ 我们用「重合锚点 + 屏尺寸」表达同一个矩形
            //    （锚点不复刻，见 `MenuDraw.SetPxSize`）。
            //    改坏法：删掉 `SetPxSize` 那句 ⇒ `Editor/ShopScene.cs` §A218「商店窗根 = 整屏矩形」红。
            var go = new GameObject("Shop Menu Variant", typeof(RectTransform));
            MenuDraw.SetPxSize(go.transform, LayoutSpace.DesignPxW, LayoutSpace.DesignPxH);
            var win = go.AddComponent<ShopWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.World;           // 实证 windowsPlacement=10（**奖励窗是 5**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            Build();
            BuildPages();
            if (tabButtons != null) tabButtons.Click(0);      // 默认落在第 1 个可见键（`__GetStartingTab` 的回落分支）
            RefreshHighlights();
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Shop] `" + what + "` 还没实现（原版是 `ShopWindow.CreateStoreTab` 按服务端 store 列表建的页签；" +
                      "本地只有 `Card/Daily/Item Shop Tab` 三页 prefab ⇒ 见 `ShopWindow.cs` 文件头）");
        }

        // ============================================================ 建

        public void Build()
        {
            var res = BuildShell(transform, Buttons, "ShopTabButton_", "Tabs");
            tabButtons = res.buttons;
            _btnRoot = res.roots;
            _btnHighlight = res.highlight;
            _btnRes = res;
            _btnBadge = res.badge;

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Shop] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            // ⚠️ 只画一次、不静默 —— 🆕 **2026-10-03：这一条缺口补上了**（原来这里出声「没做」）。
            //    现在 `Packs Scroll View` 的纵向滚动 + `Viewport` 的等效裁剪（硬边 + 软边）都接了
            //    （`ShopTabPage.BuildGrid` → `MenuScroll.TopAligned` + `Packs Scroll View/Viewport` 那颗 `ViewportClip`；
            //     ⚠️ 2026-10-13（A435 阶段 2 · A26）起走**节点态**，不再是 `MenuWindowBase.Clip/ClipSoftness`）。
            Debug.Log("[Shop] `Packs Scroll View` 纵向滚动 + 裁剪（硬边 + 软边 `m_Softness = (0,25)`）**已接**"
                      + "（2026-10-03 接滚动/硬裁 · 2026-10-04 补软边）");
        }

        /// <summary>自检用：某一页的 `ShopTabPage`（`Buy` / `GridScroll` 那些都在它上面）。</summary>
        public ShopTabPage PageOf(int page)
        {
            return page >= 0 && page < tabs.Count ? tabs[page] as ShopTabPage : null;
        }

        /// <summary>自检用：某一页的栅格滚动区（批处理里没有滚轮 ⇒ 直调它的 `Wheel/SetOffset/Tick`）。</summary>
        public MenuScroll GridScrollOf(int page)
        {
            var t = page >= 0 && page < tabs.Count ? tabs[page] as ShopTabPage : null;
            return t != null ? t.GridScroll : null;
        }

        Transform[] _btnRoot;
        ImageQuad[] _btnHighlight, _btnBadge;
        /// <summary>🆕 2026-10-03：高亮层要**整棵**开关（九宫格）⇒ 留一份 `BarResult`。</summary>
        MainMenuSubmenuWindow.BarResult _btnRes;

        void BuildPages()
        {
            tabs.Clear();
            for (int p = 0; p < ShopData.Pages.Length; p++)
            {
                var pg = Node(tabHolder, ShopData.Pages[p].Prefab, TabsRect);
                var t = pg.gameObject.AddComponent<ShopTabPage>();
                t.SetHost(this, pg, p);
                tabs.Add(t);
            }
            foreach (var t in tabs) t.Setup();
        }

        /// <summary>选中态：**只画选中的那一个**（原版四键出厂都亮、可见性由运行时 `TabButtons` 驱动）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            // 🆕 2026-10-03：**整棵九宫格一起开关**（`Highlight` 现在是 `Sliced`，一棵树 9 个 quad）
            MainMenuSubmenuWindow.SetHighlight(_btnRes, sel);
        }

        public string Dump()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append($"Shop：页签 {CurrentTab} · 页 {tabs.Count} 个 · 左栏 {Buttons.Length} 键");
            sb.Append(" · 取不到的图 ").Append(MissingArt.Count).Append(" 张");
            return sb.ToString();
        }
    }

    // ============================================================ 一页

    /// <summary>商店的一页。原版 `ShopTab : WindowTabBase`（`CardShopTab` / `ItemShopTab` 都继承它）。
    /// **三个页共用这一份 View**（实测三页逐节点同构，只差时间条的第一个子件）。</summary>
    public class ShopTabPage : WindowTabBase
    {
        public override WindowTabType Type
        {
            get
            {
                switch (_page)
                {
                    case 0: return WindowTabType.ShopCards;
                    case 1: return WindowTabType.ShopDaily;
                    default: return WindowTabType.ShopItems;
                }
            }
        }

        MainMenuSubmenuWindow _win;
        Transform _root;
        int _page;

        /// <summary>**没有主图**的商品名（画了占位板 + 出声 —— 红线：不许静默失败）。</summary>
        public readonly System.Collections.Generic.List<string> NoArtOffers =
            new System.Collections.Generic.List<string>();

        /// <summary>🆕 **`Packs Scroll View` 的纵向滚动区**（原版实测：`h=0 v=1 mode=1(**Elastic**)` ·
        /// `inertia=1` · `elasticity=0.1` · `decel=0.135` · `Viewport` 带 `RectMask2D`）。
        /// 2026-10-03 之前**没接** ⇒ 每页第 3 行起（≥5 件商品）**真画到屏外、够不着**
        /// （`Content` 由 `ContentSizeFitter(V=Preferred)` 撑到 `行数×475+7`，视口只有 952.38 高）。</summary>
        MenuScroll _gridScroll;
        /// <summary>自检用：批处理里没有滚轮事件 ⇒ 直调 `MenuScroll` 那几条（**和真滚同一条**）。</summary>
        public MenuScroll GridScroll { get { return _gridScroll; } }

        /// <summary>🆕 **2026-10-13（A435 阶段 2 · 乙 · A26/A465）**：本页 `Packs Scroll View/Viewport`
        /// 那颗 `ViewportClip`（= 原版 `Viewport` 上那个 `RectMask2D`）。
        /// ⚠️ **`Setup()` 会 `DestroyChildren(_root)` 把它整棵清掉** ⇒ 每次 `Setup()` 都要重挂、重赋给
        /// `_gridScroll.ClipNode`（`Setup` 里那两行）；`_gridScroll` 本身**复用**（见 `BuildGrid` 的 `else` 支），
        /// 所以不重赋就会**静默**停在已销毁的那颗组件上（Unity 的 `!= null` 对已销毁对象判 false ⇒ 回落旧路）。
        /// 自检用：`GridScroll.ClipNode` 直接读得到。</summary>
        ViewportClip _gridVp;

        public void SetHost(MainMenuSubmenuWindow win, Transform root, int page)
        { _win = win; _root = root; _page = page; }

        // ============================================================ 矩形（原版实测）

        /// <summary>页根 = `Tabs` 的整矩形（三页的根都是 `aMin(0,0)/aMax(1,1)/pos(0,0)/sizeDelta(0,0)` ⇒ 撑满父）。</summary>
        public static PxRect PageRect { get { return MainMenuSubmenuWindow.TabsRect; } }

        /// <summary>`daily shop header`：实测 **167.17,70.94 → 1920.00,155.94**（高 **85**）。</summary>
        public static readonly PxRect Header = new PxRect(167.17f, 70.94f, 1920.00f, 155.94f);

        /// <summary>`…/TimeCounter`：实测 **367.47,70.94 → 678.87,150.94**（311.40 × 80）。
        /// 它是个 `HorizontalLayoutGroup`（spacing **5** · `ChildControlW/H = 1` · `ForceExpandW = 0`/`H = 1` ·
        /// `MiddleCenter`）⇒ 两个子件的**位置与尺寸都是算出来的**。</summary>
        public static readonly PxRect TimeCounter = new PxRect(367.47f, 70.94f, 678.87f, 150.94f);

        /// <summary>`Packs Scroll View`：实测 **329.76,127.62 → 1920.00,1080.00**。</summary>
        public static readonly PxRect ScrollView = new PxRect(329.76f, 127.62f, 1920.00f, 1080.00f);

        /// <summary>🆕 **2026-10-04：这个 `Viewport` 上 `RectMask2D.m_Softness` 的原版真值 = (0,25)**
        /// —— **纵向** 25px 渐隐带（x 是硬边）。
        /// 🔴 判据（`d:/4/_tmp_view/q1_rm2d.txt` 扫的是**3 个菜单族包 = 150+1+5 = 156** 个 `RectMask2D`；
        /// ⚠️ **全库是 222 个**，多出来的 65 个在 13 个 `battlearena*`、1 个在
        /// `bundle_scenes_scenes_mainmenuwarpforge` —— 逐包数字 / 两条复现命令 / `m_Script` 的 PathID 判据
        /// → `MenuWindowBase.ClipSoftness` 的注释）。**三页各有一条、值都是 (0,25)**：
        /// ⚠️ **2026-10-05 二次订正（铁律 5）**：这里 2026-10-04 那次写「原来那个 222 **没有出处** ⇒
        /// 该表自己的表头加起来是 156」—— **订过头了**（错因 = 把菜单族那三包当成了全库）；
        /// **222 一直是对的**，本条这几处的取值不受影响。
        ///   · `Card Shop Tab/Packs Scroll View/Viewport`（:177-178）
        ///   · `Daily Shop Tab/Packs Scroll View/Viewport`（:295-296）
        ///   · `Item Shop Tab/Packs Scroll View/Viewport`（:59-60）
        /// ⚠️ 同一批里 `Card Shop VIP Tab Variant` / `Shop Menu Variant/…/Shop Tab` / `Item Shop Tab No Automatic
        ///    Ordering` 也都是 (0,25)，而 `Packs Tab` / `Gold Tab` / `Generic Shop Tab` 是 (0,0) —— **我们只用三页**。
        /// ⚠️ 机制与代价 → `MenuDraw.ApplySoftEdges`（几何等效：按渐隐带内沿切开 + 逐顶点 alpha 斜坡）。</summary>
        public static readonly Vector2 PacksSoft = new Vector2(0f, 25f);

        /// <summary>`…/Content` 上的 `GridLayoutGroup`（原文 `MonoBehaviour`）：
        /// **cell 335.6 × 475** · `spacing (0,0)` · `padding (0,0,7,0)` · `Flexible` / `count 2` · `UpperLeft` / `Horizontal`。
        /// ⚠️ `ShopTab.Setup` 会把它乘上 `contentLayoutElementsSizeMultiplierSmallScreens`（**1.18**）——
        ///    那是**小屏 UI** 那档；我们的版面固定 16:9 ⇒ **乘 1.0**（别照抄 1.18）。</summary>
        public const float CellW = 335.6f, CellH = 475f, GridPadT = 7f;
        public const int GridCols = 2;

        /// <summary>`Empty Collection Warning`：实测 **164.76,42.62 → 1970.00,1080.00**（**比父还宽，左右都溢出**）。</summary>
        public static readonly PxRect EmptyWarn = new PxRect(164.76f, 42.62f, 1970.00f, 1080.00f);
        public const string TxtEmpty = "There are no deck in your collection for the selected filters";

        // ---- 格里（相对 `Catalog Item Shop Container` 的 335.6×475；`menu_rect --root-size 335.6x475 --relative`）----
        public static readonly PxRect CellBg = new PxRect(-1f, -1f, 336.60f, 476f);
        /// <summary>`Available Counter`。🔴 **原版的锚点落在 `y = 476`** —— 那是**格子底边之内 1px**
        /// （格子高 475、`background` 是 −1..476）。而它的 TMP 是 **`v = Geometry`（`m_VerticalAlignment 4096`）
        /// + 零高框** ⇒ 垂直方向以锚点为中心 ⇒ **原版自己有半截落在格底之外、和下一行挨着**。
        /// 我们**把整条抬进格子里**（放在价格钮下面），并在自检里钉一条「不许越出格底」——
        /// ⚠️ **这是相对原版的一处偏离，原因写在 `资料/阶段二_商店_原版规格.md` §六**。</summary>
        public static readonly PxRect CellAvail = new PxRect(18.87f, 452f, 315.33f, 474f);
        public static readonly PxRect CellPrice = new PxRect(79.69f, 412.73f, 255.91f, 446.17f);
        public static readonly PxRect CellCount = new PxRect(-1f, 372.23f, 336.60f, 404.78f);
        public static readonly PxRect CellCountText = new PxRect(58.48f, 381.47f, 277.12f, 403.28f);
        public static readonly PxRect CellDrawer = new PxRect(-1f, 52.05f, 336.60f, 381.53f);
        public static readonly Color PriceTint = new Color(0.902f, 0.637f, 0.18f, 1f);       // `40K_button` 的 `m_Color`

        // 🔴 **我们自己划出来的三块**（原版那两行字是 `Card Drawer` 画在卡上的，见文件头）：
        //    名字 / 类型 / 主图**各占一条，谁也不压谁** —— 第一版把两行"顶"在抽屉区底部，
        //    而主图占满整个抽屉区 ⇒ **字压在商品图上**（Cards 页一眼就看出来，而**断言全绿**）。
        public static readonly PxRect CellTypeBand = new PxRect(9f, 282f, 325.6f, 316f);
        public static readonly PxRect CellNameBand = new PxRect(9f, 318f, 325.6f, 366f);
        /// <summary>主图的框：抽屉区**去掉下面那两条文字带**之后的余量。
        /// 🆕 A8：`ItemDrawer.Draw` 的 `box` 就吃它（见 `DrawerStyle` 的注释）。</summary>
        public static readonly PxRect CellArtBox = new PxRect(11f, 64f, 325.6f, 272f);

        /// <summary>🆕 A8：商店格**走抽屉那条路**时用的版式（`ItemDrawer.Draw` 那一份）。三处**我们挑的**：
        /// · `NodeName = "Art"` —— 既有的四条断言按 `FindChild(cell,"Art")` 找主图（`Art` / `ArtPlaceholder` 二选一），
        ///   名字换了那几条会**静默跳过**（`RectOf` 拿不到就 `continue`）⇒ 名字必须留 `Art`。
        /// · `IconFill = 1f` —— `ItemDrawer` 把主图画成 `min(box) × IconFill` 的**居中方块**、
        ///   再按图自身宽高比内接（`ItemDrawer.Square` + `MenuDraw.Rect(keepAspect)`）。
        ///   ⚠️ **它不等于「老路逐像素相同」** —— 2026-10-04（A34-F7）订正：
        ///   老路是**直接内接 `CellArtBox`**（314.6 × 208），而抽屉是**内接一个 208² 的方块**
        ///   ⇒ 只有「图比框宽」（`aspect ≤ 1`，如第 0 格那份）时两者才是同一个矩形；
        ///   **`Sautekh Booster`（959×914，aspect 1.049）会两轴各缩 4.7%**（218.24×208 → 208×198.24）。
        ///   ⇒ 由 <see cref="DrawerBox"/> 把方框按图的宽高比放大，**把两者重新对齐**（那一条现在对 4 件全成立）。
        /// · `QuantityPx / NamePx = 0` —— 数量由这一格自己的 `Counter` 画、名字由 `CellNameBand` 画
        ///   （原版这两个开关来自每条 `ItemDrawerReference.options` 的 `stackable` / `showName`。
        ///   🔴 **2026-10-03 就地更正（铁律 5 · A86）**：原括注「**那张配置表本地没有**」—— **假的**：
        ///   映射表 §② 已把那 20 条记录的 `options` 三字段**逐类型解出**（判据齐）；
        ///   真实原因是**我们还没把它接进来**（见 `ItemDrawer.cs` 文件头）。）</summary>
        public static ItemDrawerStyle DrawerStyle
        {
            get
            {
                var st = ItemDrawerStyle.Default(QCellArt, QCellArt, QCellText);
                st.NodeName = "Art";
                st.IconFill = 1f;
                st.QuantityPx = 0f;
                st.NamePx = 0f;
                return st;
            }
        }

        /// <summary>🆕 **2026-10-04（A34-F7）**：交给 `ItemDrawer.Draw` 的那个「抽屉框」。
        /// <para>抽屉把主图画成 **居中方块**（`min(box) × IconFill`）再按图内接；而这条格子的老路
        /// （`ShopOffer.Art` + `MenuDraw.Rect(..., keepAspect)`）是**直接内接 `CellArtBox`**。
        /// 两者只在 `aspect ≤ 1` 时相等 —— 图比框宽时抽屉那边**两轴各缩 ~4.7%**
        /// （实测 `Sautekh Booster` 959×914：老路 218.24×208，抽屉 208×198.24）。
        /// ⇒ 按图的长宽比把方框放大到刚好装下那个内接矩形（**上限 = 框宽**，再大就会越出 `CellArtBox`）。</para>
        /// <para>判据：原版唯一一份**抽屉几何旁证** —— `Daily Reward Popup Item Drawer` 里那个抽屉实例的
        /// `Content/Image` **与抽屉根同矩形 + `preserveAspect`**（`ItemDrawer.cs` 文件头 §「一条真的几何旁证」）
        /// ⇒ 原版的图是**内接抽屉框**，不是内接一个正方形。⚠️ 方框本身仍**是我们挑的**
        /// （🔴 **2026-10-03 更正（铁律 5 · A86）**：原括注「抽屉 prefab 本地没有」**是假的** ——
        /// 那批 prefab 整棵 dump 得出来；真实原因是我们**还没照 dump 出来的几何改**，见 `ItemDrawer.cs` 文件头 ②），
        /// 但它的目标是「**渲出来的矩形与换路前逐像素相同**」—— 自检对 4 件逐格断这个。</para></summary>
        public static PxRect DrawerBox(PxRect band, float texAspect)
        {
            float a = texAspect > 0f ? texAspect : 1f;
            float side = Mathf.Clamp(band.H * a, band.H, band.W);
            return new PxRect(band.CX - side * 0.5f, band.CY - side * 0.5f,
                              band.CX + side * 0.5f, band.CY + side * 0.5f);
        }

        /// <summary>🆕 A8：本页**走抽屉**的格数（`ItemDrawer`）/ **走兜底**的格数（`ShopOffer.Art` + 占位板）。
        /// 自检拿它断「两条路各走了一次」，**不是**靠数节点反推。</summary>
        public int DrawerCells, FallbackCells;
        /// <summary>没有主图时画的占位板（**中性灰底 + 短名**，不拿别的图冒充）。</summary>
        public static readonly Color ArtPlaceholderTint = new Color(0.16f, 0.16f, 0.18f, 1f);

        public static readonly Color WarnColor = new Color(0f, 0f, 0f, 1f);                  // 原版那行是**纯黑字**

        // 渲染队列：**这一层是「页」**（窗外壳 3005…3014 之下，弹窗 3110+ 之上）
        public const int QHeader = 3020, QHeaderText = 3021, QGrid = 3022,
                         QCellBg = 3023, QCellArt = 3024, QCellText = 3025,
                         QCellPrice = 3026, QCellPriceText = 3027, QCellCount = 3028, QCellCountText = 3029,
                         QEmpty = 3030,
                         /// <summary>🆕 A6：主图上那个「开卡包详情窗」的命中区。
                         /// 比价格钮的命中区（`QCellPrice`）**低** —— 两块矩形本来不重叠，
                         /// 真叠上时让**购买**优先（`PointerLayer` 取队列最高的那个）。</summary>
                         QCellInfoHit = 3031;

        // ============================================================ 建

        public override void Setup()
        {
            _win.DestroyChildren(_root);
            NoArtOffers.Clear();
            var page = PageRect;
            var spec = ShopData.Pages[_page];

            // ---- ① `daily shop header`（`Line` 出厂 INACT ⇒ 不建）----
            var hdr = MainMenuSubmenuWindow.Node(_root, "daily shop header", Header);
            BuildTimeCounter(hdr, spec.TimerAsText);

            // ---- ② `Packs Scroll View` → `Viewport` → `Content`（GridLayoutGroup）----
            var sv = MainMenuSubmenuWindow.Node(_root, "Packs Scroll View", ScrollView);
            // ⚠️ `Scroll View`/`Viewport` 自己的 `Image` 是 `UIMask` 且 **alpha = 0** ⇒ 不画
            var vp = MainMenuSubmenuWindow.Node(sv, "Viewport", ScrollView);
            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A26）**：裁切状态**长在这颗视口节点上** ——
            //   参数 = 原版那两个字段的实读值（判据与逐页出处 → `PacksSoft` 那段注释）：
            //   `m_Padding = (0,0,0,0)` · `m_Softness = (0,25)`（**纵向** 25px 渐隐带、x 是硬边）。
            //   ⚠️ 原来是 `BuildGrid` 里一对「`_win.Clip = ScrollView; _win.ClipSoftness = PacksSoft;`
            //   → 循环 → 两件原样放回」—— **那四行已整对删掉**（留着 = 形参永远非空 ⇒ `Resolve` 第 1 支
            //   ⇒ 节点一个像素都不生效，静默；唯一痕迹 = `ViewportClip.NodeShadowedByParam`）。
            //   ⚠️ **别用 `ViewportClip.Hang`**：那颗走 `MenuDraw.Node`，与这里的
            //   `MainMenuSubmenuWindow.Node` 虽然今天逐句等价，但这一行是既有代码、**只加不换**更稳
            //   （`MenuDraw.Node` / `MenuWindowBase.Node` 等价性 → 两者各自的注释）。
            _gridVp = vp.gameObject.AddComponent<ViewportClip>();
            _gridVp.padding = Vector4.zero;
            _gridVp.softness = new Vector2Int((int)PacksSoft.x, (int)PacksSoft.y);
            var content = MainMenuSubmenuWindow.Node(vp, "Content", ScrollView);
            BuildGrid(content);

            // ---- ③ `Empty Collection Warning`（出厂 INACT；原版在列表为空时显示）----
            var ew = MainMenuSubmenuWindow.Node(_root, "Empty Collection Warning", EmptyWarn);
            var wl = _win.Text(ew, TxtEmpty, EmptyWarn.x1, EmptyWarn.x2, EmptyWarn.y1, EmptyWarn.y2, 5,
                               WarnColor, "Warning", 36f);
            if (wl != null) { wl.SetRenderQueue(QEmpty); MenuDraw.AlignRight(wl, EmptyWarn); }
            // 我们的每一页都有商品 ⇒ **恒不显示**（留着是为了将来真出现空列表时能用）
            ew.gameObject.SetActive(false);

            if (NoArtOffers.Count > 0) WarnNoArt();
        }

        /// <summary>`TimeCounter`（`HorizontalLayoutGroup`：spacing 5 · `ChildControlW/H=1` ·
        /// `ForceExpandW=0` / `H=1` · `MiddleCenter`）。
        /// 🔴 子件的宽是**各自的首选宽**（`ChildControlWidth=1`）—— 文字那种量出来才知道，
        ///    所以只能**先建、量渲染宽、再摆位**（照 UGUI 的语义：整组在容器里居中）。</summary>
        void BuildTimeCounter(Transform hdr, bool asText)
        {
            var tc = MainMenuSubmenuWindow.Node(hdr, "TimeCounter", TimeCounter);
            var parts = new System.Collections.Generic.List<Transform>();
            var widths = new System.Collections.Generic.List<float>();

            if (asText)
            {
                var lb = _win.Text(tc, ShopData.RefreshText, TimeCounter.x1, TimeCounter.x2,
                                   TimeCounter.y1, TimeCounter.y2, 5, Color.white, "RefreshText", 30f);
                if (lb != null) { lb.SetRenderQueue(QHeaderText); parts.Add(lb.transform); widths.Add(lb.WorldW * 108f); }
            }
            else
            {
                // `Clock Icon`：`LayoutElement.preferredWidth = 26`（实测）。容器的 `ChildControlHeight = 1`
                // 会把它的框撑到内容高（80），但原版那张 `Image` 是 **`preserveAspect`** ⇒ 实际画出来是
                // **26×26 居中**。⇒ 这里直接按 26² 画，别拿 26×80 去拉（那会把钟抻长）。
                // 🔴 节点要**摆在与文字同一套矩形上**（= 容器中心），否则后面那步「只改 x」会把 y 留在错的初值上
                //    —— 第一版就是这么错的：图标画在屏外，而**所有断言全绿**（断言只管「节点在不在」）。
                var ic = MainMenuSubmenuWindow.Node(tc, "Clock Icon",
                                                    new PxRect(TimeCounter.x1, TimeCounter.y1, TimeCounter.x2, TimeCounter.y2));
                const float side = 26f;
                var tex = _win.Art("WF_icon_clock");
                if (tex != null)
                {
                    var q = ImageQuad.Create(ic, tex,
                                             MainMenuSubmenuWindow.Local(ic, TimeCounter.CX, TimeCounter.CY),
                                             LayoutSpace.Px(side), new Vector2(0.5f, 0.5f), "Icon");
                    if (q != null) { q.SetAspect((float)tex.width / tex.height); q.SetRenderQueue(QHeaderText); }
                }
                parts.Add(ic); widths.Add(side);
            }

            // `Time`（`LayoutElement.minWidth = 111.31`，实测）
            var tm = _win.Text(tc, ShopData.RefreshTime, TimeCounter.x1, TimeCounter.x2,
                               TimeCounter.y1, TimeCounter.y2, 5, Color.white, "Time", 30f);
            if (tm != null) { tm.SetRenderQueue(QHeaderText); parts.Add(tm.transform); widths.Add(111.31f); }

            // 居中排：总宽 = Σ + spacing×(n−1)，从容器中心往两边分
            float total = 0f;
            for (int i = 0; i < widths.Count; i++) total += widths[i];
            total += 5f * (widths.Count - 1);
            float x = TimeCounter.CX - total * 0.5f;
            for (int i = 0; i < parts.Count; i++)
            {
                float w = widths[i];
                float cx = x + w * 0.5f;
                var lb = parts[i].GetComponent<Label>();
                if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(x, 0f).x);
                // 🔴 **2026-10-11（A306⑥）**：`− tc.position.x` → `− MenuDraw.PosInDesignSpace(tc).x`
                // ——**同一份量纲病（少除一层父级 `lossyScale`）的窄版（只 x 分量）**。
                // ⚠️ **同一个 `if/else` 的另一支（上一行 `AlignLeftOn`）2026-10-11（A228）已经修过了**
                // （`Battle/Label.cs` 的 `ParentXInDesignSpace()` 除的是**标签父件** = `tc` 的 `lossyScale`）
                // ⇒ 这一支原来**一支修了一支没修**（同一处代码两个分支两套口径）。本次把两支对齐：
                // · `AlignLeftOn` 除 `tc.lossyScale.x`；本行除 `tc.parent.lossyScale.x`（`PosInDesignSpace` 的语义）。
                // · **两者在「`tc` 自己不带 `localScale`」时逐位同值** —— 而 `tc` 是
                //   `MainMenuSubmenuWindow.Node(hdr, "TimeCounter", TimeCounter)` 建的（只写 `localPosition`
                //   ⇒ `localScale = 1`）⇒ **本处两支口径现在完全一致**。
                // · ⚠️ 若哪天有人给 `tc` 挂 `localScale`，两支会分家；那时按 `PosInDesignSpace` 这条统一
                //   （`AlignLeftOn` 那边是 `Battle/Label.cs` 的公共件，不在本批白名单）。
                // 📌 `k == 1`（缩放开关出厂关着）时与改前**逐位相同**。
                // 🔴 **改坏法**：换回裸 `tc.position.x` ⇒ **今天一条现有断言都不会红**
                // （`k == 1` 两式逐位相同 ⇒ 这是**潜伏缺陷**）⇒ 要补的两态断言写在
                // `资料/普查产出_1011/W4_子3.md` §四，由调度台安排。
                else parts[i].localPosition = new Vector3(
                        LayoutSpace.FromPixel(cx, 0f).x - MenuDraw.PosInDesignSpace(tc).x,
                        parts[i].localPosition.y, 0f);
                x += w + 5f;
            }
        }

        /// <summary>`GridLayoutGroup`：`cellSize (335.6,475)` · `spacing (0,0)` · `padding.top 7` ·
        /// `constraint Flexible / count 2` · `startCorner UpperLeft` · `startAxis Horizontal`
        /// ⇒ 格位 = `(col, row) × (335.6, 475) + (0, 7)`（相对 `Content` 左上角）。
        /// 🆕 **2026-10-03：接上纵向滚动 + 裁切**（原来第 3 行起真画到屏外）——
        /// 原版这一件是 `ScrollRect(h=0 v=1 mode=1(**Elastic**) · inertia=1 · elasticity=0.1 · decel=0.135)`
        /// + `Viewport` 上的 `RectMask2D`（`menu_dump.py bundle_menus_assets_all "Card Shop Tab"` 实读）。</summary>
        void BuildGrid(Transform content)
        {
            var offers = ShopData.Offers(_page);
            // 🆕 A8：两条路的计数**每次重建都归零**（`Setup` 与滚动回调都会走到这儿）
            DrawerCells = 0; FallbackCells = 0;
            // `ContentSizeFitter(Vertical = Preferred)`：内容高 = 行数 × 475 + 7（原版就是这个式子）
            float contentH = (offers.Length + GridCols - 1) / GridCols * CellH + GridPadT;
            float contentW = GridCols * CellW;

            if (_gridScroll == null)
            {
                _gridScroll = MenuScroll.TopAligned(ScrollView, contentH);
                // 🔴 档位 = 原版 `m_MovementType = 1` ⇒ UGUI **Elastic**（真值 `0 Unrestricted / 1 Elastic / 2 Clamped`；
                //    本文件原注释写的「`mode=1(Clamped)`」是【反的】，2026-10-04 已在
                //    `资料/阶段二_滚动与指针_原版规格.md` §一 订正 —— A28 把行为补上）。
                //    判据（原始 JSON 实读，逐页都读过）：`python 工具/menu_dump.py bundle_menus_assets_all "<页名>"`
                //    ⇒ `Card Shop Tab` / `Daily Shop Tab` / `Item Shop Tab`（= 我们那三页）的 `Packs Scroll View`
                //    **都是 `h=0 v=1 mode=1`**（⚠️ 包里另有 `Generic Shop Tab` 那种**不在我们三页里**的同类件，别拿它当判据）。
                _gridScroll.Elastic = true;
                _gridScroll.Owner = _root.gameObject;
                _gridScroll.OnChanged = RebuildForScroll;
                PointerLayer.RegisterScroll(_gridScroll);
            }
            else
            {
                // 切页/换内容 ⇒ 同一份滚动区只改两端（**别新建** —— 旧注册条目的 `Owner` 是窗口根、不会自己死）
                _gridScroll.ContentX1 = ScrollView.y1;
                _gridScroll.ContentX2 = ScrollView.y1 + contentH;
                _gridScroll.Stop();                 // 旧速度别带到新内容上
            }
            // 内容比视口窄 ⇒ 横向本来就没有可滚的余地（原版 `h=0`）

            // 🔴 **2026-10-13（A435 阶段 2 · 乙 · A465）**：构建循环那一路（「整块在视口外 ⇒ 不建」）
            //   也要吃那颗视口节点。⚠️ **每次 `BuildGrid` 都重赋** —— `Setup()` 会把上一轮那颗节点整棵
            //   销毁，而 `_gridScroll` 是**复用**的（上面那个 `else` 支）⇒ 不重赋就会静默停在已销毁的组件上。
            _gridScroll.ClipNode = _gridVp;

            // 🔴 **`Clip` 与 `ClipSoftness` 那一对（旧写法）已在 2026-10-13 删掉** —— 见 `Setup()` 里
            //    `_gridVp` 那一段：状态现在长在 `Packs Scroll View/Viewport` 那颗 `ViewportClip` 上，
            //    本循环里所有件（`_win.Rect` / `_win.Text` / `_win.AddHit` 建的）都挂在 `Content` 之下
            //    ⇒ 沿父链解析到**同一份**（框 = `ScrollView` 反推、`pad` 全 0、`softness` = (0,25)）。
            //    ⚠️ 原来那一段留着的理由（「谁设 `Clip` 谁顺手把 `ClipSoftness` 设对」）随字段一起作废。
            for (int i = 0; i < offers.Length; i++)
            {
                int col = i % GridCols, row = i / GridCols;
                float x1 = ScrollView.x1 + col * CellW;
                float y1 = ScrollView.y1 + GridPadT + row * CellH;   // 内容坐标（偏移 0 时）
                var rc = new PxRect(x1, y1, x1 + CellW, y1 + CellH);
                var r = _gridScroll.Shift(rc);                        // 内容坐标 → 屏幕坐标（**只做偏移**）
                if (!_gridScroll.Intersects(r)) continue;             // 整格在视口外 ⇒ 不建（点击区也没了）
                BuildCell(MainMenuSubmenuWindow.Node(content, "CatalogItemShopContainer_" + i, r), r, i, offers[i]);
            }
        }

        /// <summary>滚动回调（`MenuScroll.OnChanged`）—— 只重画栅格，不重建整页
        /// （整页重建会把页头、页签、滚动区登记一起搅动）。
        /// 🔴 **幂等**：必须先清 `Content` 的子节点（同 `ForgeTab.BuildArmyItems` 那条教训 —— 不清就越建越多）。</summary>
        void RebuildForScroll()
        {
            var content = _root.Find("Packs Scroll View/Viewport/Content");
            if (content == null) return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Object.DestroyImmediate(content.GetChild(i).gameObject);
            NoArtOffers.Clear();
            BuildGrid(content);
            if (NoArtOffers.Count > 0) WarnNoArt();
        }

        /// <summary>「有商品没有主图」这条**出声**（红线：不许静默失败）。</summary>
        void WarnNoArt()
        {
            Debug.LogWarning("[Shop] ⚠️ 第 " + (_page + 1) + " 页有 " + NoArtOffers.Count
                             + " 件商品**没有主图**（画的是中性灰占位板，**没拿别的图冒充**）："
                             + string.Join("、", NoArtOffers.ToArray())
                             + " —— 原版这一步是服务端给的货图，我们编的商品表里没填 `Art`（正本 §六 第 2 条）");
        }

        /// <summary>一格 `Catalog Item Shop Container`。格里几何照 `menu_rect --root-size 335.6x475 --relative` 原文。
        /// 🔴 **名字 / 类型两行是我们加的**（原版这两行由 `Card Drawer` 把卡画出来）。
        /// 🔴 **2026-10-03 就地更正（铁律 5 · A86）**：本行原来接着写「而我们没有 `ItemDrawerConfig` 那张
        /// 『类型 → 抽屉 prefab』表」—— **假的**：那张表 **2026-10-04 已整张解出**
        /// （`资料/普查产出_1004/ItemDrawerConfig_映射表.md`；我们的那份在 `ItemDrawer.ItemTypeSets`）；
        /// 我们缺的是**`Card Drawer` 那个抽屉变体本身没建** ⇒ 抽屉那一侧画的是**我们自建的 4 个抽屉**。
        /// 🆕 **2026-10-03（A8）**：主图改走 **原版那条链** `ItemDrawer.Draw(..., DrawerOverride.Shop, ...)`
        /// （见下面那段注释），老路（`ShopOffer.Art` + 占位板）**原样留着当兜底**。</summary>
        void BuildCell(Transform cell, PxRect r, int idx, ShopOffer o)
        {
            // `background`：`UI_Deck_Selection_Back_simple`（**Simple**，不是 Sliced）+ `Mask(showGraphic=1)`
            _win.Rect(cell, "UI_Deck_Selection_Back_simple",
                      Rect(r, CellBg), "background", QCellBg);

            // 商品主图（原版 `background` 的 `m_Sprite` 是 0、运行期由服务端赋图）
            // ⚠️ **主图的框是「抽屉区去掉下面两条文字带」** —— 第一版让它占满整个抽屉区，
            //    于是我们加的那两行字**压在商品图上**（Cards 页一眼可见，而**断言全绿**）。
            //
            // 🆕 **2026-10-03（A8）：补上原版那条链** —— `CatalogItemContainer.OnInitialize` 原版干的是
            //   `ItemDrawer.Draw(this.drawerHolder, item, 1, DrawerOverride.Shop(0x14), 0)`
            //   （`d:/2/tools/decomp_full/CatalogItemContainer__OnInitialize.c` 逐步读出来的），
            //   **不是**自己填一张图。我们此前是自己填 `ShopOffer.Art`。
            // 🔴 **这条链是【加法】**：`ItemDrawer.HasArt(spec, Shop)` 为真 ⇒ 走抽屉；
            //    **为假 ⇒ 下面那条老路（`ShopOffer.Art` + 占位板）一字不动**。
            //    ⇒ 今天只有 Cards 页那 4 件卡包走抽屉（它们有图）；Daily/Items 页 6 件 `Art = null`
            //      ⇒ `Spec` 判 `Unknown` ⇒ `HasArt` 假 ⇒ 照旧走占位板 + 出声。
            // ⚠️ `spec` 的 id 用**商品名**当替身 —— 原版那个 id 是服务端 `ObtainableItem.targetId`，
            //    我们的 `ShopOffer` 里没有这一栏（`ShopData.cs` **不在本次白名单**）⇒ **这是我们挑的**。
            var spec = ItemDrawer.Spec(o.Name, o.Art, ShopData.ShortName(o.Name));
            if (ItemDrawer.HasArt(spec, DrawerOverride.Shop))
            {
                DrawerCells++;
                // 🔴 **抽屉框按图的长宽比放大**（A34-F7）—— 否则图比框宽的那几件会两轴各缩 ~4.7%，
                //    而断言只覆盖第 0 格 ⇒ 静默。判据与算式 → `DrawerBox` 的注释。
                var artBand = Rect(r, CellArtBox);
                var artTex = CardArt.MenuUi(o.Art);
                var artBox = artTex != null && artTex.height > 0
                           ? DrawerBox(artBand, (float)artTex.width / artTex.height)
                           : artBand;
                var drew = ItemDrawer.Draw(cell, artBox, spec, 1, DrawerOverride.Shop, DrawerStyle);
                // 红线：不许静默失败 —— 判据说「有图」却没画出来 / 落了占位板 / 用了退档图，都要出声
                if (drew.Node == null)
                    Debug.LogWarning("[Shop] 第 " + (_page + 1) + " 页第 " + (idx + 1) + " 件 `" + o.Name
                                     + "`：`ItemDrawer.HasArt` 说**有图**，可 `Draw` 什么都没画（`Drawer = "
                                     + (drew.Drawer ?? "<null>") + "`）");
                else if (drew.Placeholder)
                    Debug.LogWarning("[Shop] 第 " + (_page + 1) + " 页第 " + (idx + 1) + " 件 `" + o.Name
                                     + "`：抽屉**落了占位板**（图名 `" + o.Art + "` 运行时取不到）");
                else if (drew.FallbackArt)
                    Debug.LogWarning("[Shop] 第 " + (_page + 1) + " 页第 " + (idx + 1) + " 件 `" + o.Name
                                     + "`：抽屉用了**退档图**（`" + drew.Art + "`）");
            }
            else if (!string.IsNullOrEmpty(o.Art))
            {
                FallbackCells++;
                _win.Rect(cell, o.Art, Rect(r, CellArtBox), "Art", QCellArt, null, true);
            }
            else
            {
                FallbackCells++;
                // **没有主图 ⇒ 画一块占位板并出声**（不拿别的图冒充；红线：不许静默失败）
                // 🔴 **不要铺满整个主图框** —— 第一版就是铺满的，实拍出来是**一大块灰板**，
                //    看着像「这一格坏了」。改成**小一号居中的板 + 物品短名**（同战役奖励窗那套）。
                if (!NoArtOffers.Contains(o.Name)) NoArtOffers.Add(o.Name);
                var ar = Rect(r, CellArtBox);
                var pr = new PxRect(ar.CX - 84f, ar.CY - 84f, ar.CX + 84f, ar.CY + 84f);
                _win.Rect(cell, null, pr, "ArtPlaceholder", QCellArt, ArtPlaceholderTint);
                var pl = _win.Text(cell, ShopData.ShortName(o.Name), pr.x1 + 8f, pr.x2 - 8f,
                                   pr.y1 + 8f, pr.y2 - 8f, 5, new Color(0.78f, 0.78f, 0.82f, 1f),
                                   "PlaceholderName", 24f);
                if (pl != null)
                {
                    pl.SetRenderQueue(QCellText);
                    pl.SetAutoFitBox(LayoutSpace.Px(pr.W - 16f), LayoutSpace.Px(pr.H - 16f), 14f, 24f);
                }
            }

            // 🆕 2026-10-03（§三 第 29 条 A6）：`Booster Pack` 那一类商品 —— **点主图开 `Booster Info Popup`**。
            // 🔴 **入口是我们定的、不是复刻**：原版走 `ShopOfferContainer.OnClick → OpenContainer`
            //    （从 offer 取 AssetGroup 0xc 的窗口），而**我们用的这族 `CatalogItemContainer`
            //    只画抽屉、不开窗**（2026-10-03 查实；判据 → `阶段二_商店_原版规格.md` §五·二 末尾）。
            //    只在卡包上接：那扇窗的内容是**卡包保底进度**，非卡包商品套不上。
            if (o.Type == "Booster Pack")
                MenuDraw.Hit(cell, "InfoHit", Rect(r, CellArtBox), QCellInfoHit, () => OpenBoosterInfo(idx));

            // 我们加的两行字（**标明是我们加的**；各自一条带，谁也不压谁）
            // 🔴 **必须开自适应字号**：`Ultramarines Booster` 在 fs30 下实测宽 **328px**，
            //    而名字带只有 **316.6px** ⇒ `AlignRight` 之后**左边越出格子 2.8px**
            //    （同 `资料/已知的坑.md` 那条「AutoFitBox：字号对而溢出，自检照样全绿」）。
            //    ⚠️ 顺序：**先 `SetAutoFitBox` 再 `AlignRight`** —— 对齐是按**当前**文字宽度算的。
            {
                var tyR = Rect(r, CellTypeBand);
                var ty = _win.Text(cell, o.Type, tyR.x1, tyR.x2, tyR.y1, tyR.y2, 5,
                                   new Color(0.717f, 0.717f, 0.717f, 1f), "Type", 26f);
                if (ty != null)
                {
                    ty.SetRenderQueue(QCellText);
                    ty.SetAutoFitBox(LayoutSpace.Px(tyR.W), LayoutSpace.Px(tyR.H), 16f, 26f);
                    MenuDraw.AlignRight(ty, tyR);
                }
                var nmR = Rect(r, CellNameBand);
                var nm = _win.Text(cell, o.Name, nmR.x1, nmR.x2, nmR.y1, nmR.y2, 5, Color.white, "Name", 30f);
                if (nm != null)
                {
                    nm.SetRenderQueue(QCellText);
                    nm.SetAutoFitBox(LayoutSpace.Px(nmR.W), LayoutSpace.Px(nmR.H), 18f, 30f);
                    MenuDraw.AlignRight(nm, nmR);
                }
            }

            // `Available Counter`：**有限购信息才画**（`AvailableMax > 0`）；原版是 `Available: 1/5`，hAlign Right
            if (o.AvailableMax > 0)
            {
                var ar = Rect(r, CellAvail);
                var al = _win.Text(cell, "Available: " + o.Available + "/" + o.AvailableMax,
                                   ar.x1, ar.x2, ar.y1, ar.y2, 5, Color.white, "Available Counter", 16f);
                if (al != null)
                {
                    al.SetRenderQueue(QCellText);
                    // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版这颗 `Available Counter`
                    //   是 `v = Geometry (4096)` = **`Midline`**（判据 = `CellAvail` 那条注释自己逐字写的
                    //   `m_VerticalAlignment 4096`）。⚠️ 原版那颗是**零高框、锚点落在格底之外**，
                    //   我们**把整条抬进格子里**（`CellAvail`，见 `:291-295` 那条如实登记的偏离）
                    //   ⇒ 本笔只落**档位**，框仍是我们挑的那个（同 A712 阶段 2 的口径）。
                    MenuDraw.SetVAlign(al, Label.VAlign.Midline, ar);
                    MenuDraw.AlignRight(al, ar);
                }
            }

            // `Counter`（拥有数角标）+ 它的 `Text (TMP)` `x14`
            {
                var cr = Rect(r, CellCount);
                _win.Rect(cell, "40K_main_deck_card_counter", cr, "Counter", QCellCount, null, true);
                var tr = Rect(r, CellCountText);
                var tl = _win.Text(cell, "x" + ShopData.OwnedOf(_page, idx),
                                   tr.x1, tr.x2, tr.y1, tr.y2, 5, Color.white, "Text (TMP)", 23f);
                if (tl != null) { tl.SetRenderQueue(QCellCountText); MenuDraw.AlignRight(tl, tr); }
            }

            // `Price Display Button` → `Generic UI Button`（`40K_button`，Simple + preserveAspect，金色）
            {
                var pr = Rect(r, CellPrice);
                // 🆕 2026-10-03 A17：原版这一颗是 SpriteSwap —— `40K_button` → `40K_button_hover`
                //（普查 §块 2 第 1 行：`Catalog Item Shop Container>background>price-bg>Price Display Button>Generic UI Button`）
                var priceQ = _win.Rect(cell, "40K_button", pr, "Generic UI Button", QCellPrice, PriceTint, true);
                var lt = _win.Text(cell, o.Price, pr.x1, pr.x2, pr.y1, pr.y2, 5, Color.white, "Button Text", 30f);
                if (lt != null) lt.SetRenderQueue(QCellPriceText);
                var hit = MainMenuSubmenuWindow.New(cell, "Hit");
                var hq = ImageQuad.Create(hit, CardArt.Solid(),
                                          MainMenuSubmenuWindow.Local(hit, pr.x1, pr.y1, pr.x2, pr.y2),
                                          LayoutSpace.Px(pr.H), new Vector2(0.5f, 0.5f), "Hit");
                if (hq != null)
                {
                    hq.SetAspect(pr.W / pr.H);
                    hq.SetTint(new Color(0f, 0f, 0f, 0f));
                    hq.SetRenderQueue(QCellPrice);
                }
                var wb = hit.gameObject.AddComponent<WindowButton>();
                int captured = idx;
                wb.onClick = () => Buy(captured);
                wb.Bind(priceQ, "40K_button");   // A17：悬停/按下换图（同一颗，原版命中就在按钮本身上）
            }
        }

        /// <summary>把「格内局部矩形」换算成绝对矩形（格内几何是相对 `Catalog Item Shop Container` 的）。</summary>
        static PxRect Rect(PxRect cell, PxRect inner)
        {
            return new PxRect(cell.x1 + inner.x1, cell.y1 + inner.y1, cell.x1 + inner.x2, cell.y1 + inner.y2);
        }

        /// <summary>买一件。**照边界②：不扣钱**，只记账 + **出声**（红线：点了必须有反应）。
        /// 🆕 2026-10-03：**先过「传奇重复购买」那道确认**（判据 → `ShopData.NeedsLegendaryConfirm`；
        /// 原版 `CatalogItemContainer__TryPurchase` 就是这个顺序：先弹 `MenuShop/ExtraLegendaryWarning`、
        /// 确认了才走 `ShopItemContainer.TryPurchase`）。⚠️ 取消 = **什么都不做**（原版那条 `Cancel` 分支就是返回）。</summary>
        public string Buy(int idx)
        {
            if (ShopData.NeedsLegendaryConfirm(_page, idx))
            {
                Debug.Log("[Shop] 这一件是**传奇（Rarity 4）且已拥有 1 张** ⇒ 照原版先弹确认框"
                          + "（`MenuShop/ExtraLegendaryWarning`）—— 确认了才买");
                if (_win != null && _win.Manager != null)
                {
                    int captured = idx;
                    _win.Manager.ShowPopUp(ShopData.LegendaryWarnText, "确定", () => DoBuy(captured), "取消");
                    return "";
                }
                Debug.LogWarning("[Shop] 没有 `WindowsManager` ⇒ **弹不出确认框**（这一件按原版不该直接买）");
                return "";
            }
            return DoBuy(idx);
        }

        /// <summary>真正掏钱那一步（确认框点「确定」之后走的也是它）。
        /// <para>🆕 **2026-10-12（A439）：买到手 ⇒ 弹领奖窗**（非容器档那几件）。判据见下面那一段注释。</para></summary>
        string DoBuy(int idx)
        {
            string got = ShopData.Buy(_page, idx);
            Debug.Log("[Shop] 买了 " + got + "（**不做真实经济**：资源固定 9999、不扣钱 —— 用户 2026-09-17 边界②）");
            // 拥有数变了 ⇒ 重建这一页（**卡变了就重建视图**，同卡池那条纪律）
            Setup();
            // ---- 到手之后那一扇窗：**两条路，按原版的档分开**（A439）----
            //
            // 🔴 判据（第一权威 = 反编译方法体）：**原版按「这一件是哪一档 offer」走两条完全不同的路**——
            //   · **容器档**（`ContainerOfferData`，买卡包那种）⇒ `ContainerService.OpenContainer` 的
            //     回包里 `RewardService.Collect(rewards, showAnimation: **0**, …)`
            //     （`ContainerService.__c__DisplayClass2_0___OpenContainer_g__OnComplete_0.c:107`）
            //     ⇒ **不开领奖窗**，紧跟着 `:110-146` 才 `BoosterPackOpenWindow.Initialize(...)`
            //     ⇒ 我们这一侧 = 下面那句 `OpenBoosterPack(idx)`（**已经有了，别动**）。
            //   · **商品档**（`ShopOfferDataV2` = 我们这三页的非卡包货）⇒ `ShopOfferEventV2.OpenOfferContainer`
            //     的回包里 `RewardService__Collect(rewards, **1**, onCollected, 0, 1, 0, 1, 0)`
            //     （`Everguild.LiveOps.ShopOfferEventV2.__c__DisplayClass15_0___OpenOfferContainer_g__HandleSuccess_0.c:23`
            //     —— 第 2 参实读到 `1`）⇒ **开窗**，「窗里画的就是这一件发的那几条」。
            //     ⚠️ **同一族的 `ContainerOfferEvent` 那条是 `0`**（同名列 `…ContainerOfferEvent…:23`）
            //     ⇒ **两条不能一起接**，这里按 `Type` 分开正是为此。
            // 🔴 **还有一个更硬的判据**：原版那个开关**是 per-offer 的数据**，不是写死的 ——
            //   `ContainerService.__c__DisplayClass1_0___OpenContainer_g__OnComplete_0.c:20-22` 里
            //   `Collect(param_2, offer.<虚属性>, …)` 取的就是 `ShopOfferBase.ShowRewardOnPurchase`
            //   （`ContainerOfferData` 那条反编译出来是**常量 false**；本地那 4 个 `ShopOfferDataV2`
            //   SO 逐张实读到 `showRewardOnPurchase: 1`）⇒ **容器档恒不开、商品档开**。
            //   我们这一侧的对位 = `Type == "Booster Pack"`（容器档，本文件既有那条判据）+ `Grants`。
            // 🔴 **窗里装什么都不在窗这边**（数据在 `ShopData.Grants`，判据与出处写在 `ShopOffer.Grants` 上）——
            //   本函数只管「什么时候开、开的哪几条」，⛔ 别在这里再写一份奖励表。
            var offers = ShopData.Offers(_page);
            bool inRange = idx >= 0 && idx < offers.Length;
            if (inRange && offers[idx].Type == "Booster Pack")
            {
                // 🆕 2026-10-03（§三 第 29 条 **A7**）：**买完开包**。
                // 🔴 入口是我们定的那一处：原版这条链的上游在服务端（买成功 → 服务端回执 → 弹开包窗），
                //    本地没有 ⇒ 我们把它接在**购买成功之后**（`§五·三` 只说「规格已备好、没建」，没给入口判据）。
                // ⚠️ `BoosterPackOpenWindow` 是 `type = 0 (Fullscreen)` ⇒ `OpenWindow` 会**把商店关掉**
                //    （原版 `OpenWindowCO` 对全屏窗就是这个行为），这是照原版的，不是我们图省事。
                OpenBoosterPack(idx);
            }
            else
            {
                var grants = ShopData.GrantsOf(_page, idx);
                if (grants == null)
                {
                    // 红线：**不许静默失败** —— 商品档却没有奖励表 = 数据漏填/越界，说出来
                    string nm = inRange ? offers[idx].Name : ("#" + idx + "（越界）");
                    Debug.LogWarning("[Shop] 第 " + (_page + 1) + " 页的 `" + nm + "` **没有奖励表** ⇒ 不弹领奖窗"
                                     + "（A439：原版这一档是 `RewardService.Collect(..., showAnimation: 1, …)`；"
                                     + "我们这一侧的奖励表在 `ShopData` 的 `ShopOffer.Grants`，这一件没填）");
                }
                else
                {
                    RewardWindow.ShowCollected(grants, collected => Setup());
                }
            }
            return got;
        }

        /// <summary>最近一次开出来的 `Booster Pack Open Window`（自检用）。</summary>
        public BoosterPackOpenWindow LastBoosterPack;

        /// <summary>🆕 2026-10-03（A7）：开「开包窗」（买完卡包那条链的落地）。
        /// 🔴 **入口是我们定的** —— 判据与取舍 → `Shell/BoosterPackOpenWindow.cs` 文件头。</summary>
        public BoosterPackOpenWindow OpenBoosterPack(int idx)
        {
            LastBoosterPack = null;
            if (_win == null || _win.Manager == null)
            {
                Debug.LogWarning("[Shop] 没有 `WindowsManager` ⇒ 开不了 `Booster Pack Open Window`");
                return null;
            }
            var w = BoosterPackOpenWindow.Create(_win.Manager);
            _win.Manager.OpenWindow(w);
            w.Show(_page, idx);
            LastBoosterPack = w;
            return w;
        }

        /// <summary>最近一次开出来的 `Booster Info Popup`（自检用）。</summary>
        public BoosterInfoPopup LastBoosterInfo;

        /// <summary>🆕 2026-10-03（A6）：开「卡包详情窗」。
        /// 🔴 **入口是我们定的**（原版那条 `ShopOfferContainer.OnClick → OpenContainer` 我们没有）——
        /// 判据与取舍 → `BoosterInfoPopup.cs` 文件头。</summary>
        public BoosterInfoPopup OpenBoosterInfo(int idx)
        {
            LastBoosterInfo = null;
            if (_win == null || _win.Manager == null)
            {
                Debug.LogWarning("[Shop] 没有 `WindowsManager` ⇒ 开不了 `Booster Info Popup`");
                return null;
            }
            var w = BoosterInfoPopup.Create(_win.Manager);
            w.Host = _win as ShopWindow;                 // 购买那条链要走商店的传奇确认闸门
            _win.Manager.OpenWindow(w);
            w.Show(_page, idx);
            LastBoosterInfo = w;
            return w;
        }

        public string Dump()
        {
            return "ShopPage[" + ShopData.PageLabel(_page) + "] 商品 " + ShopData.Offers(_page).Length + " 件";
        }
    }
}
