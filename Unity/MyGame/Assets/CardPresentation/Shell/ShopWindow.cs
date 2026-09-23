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
//   · **格里的「名字 / 类型」两行**：原版由 `Card Drawer` 把卡画出来（我们还没有那套抽屉）
//     ⇒ 这里**我们直接画两行字**顶在那，标明是**我们加的**。
//   · **`TimedOffer` / `New` 两个角标不建**：出厂 INACT，而且按原版锚点算出来落在**格外面**
//     （y −151..−101，见 `menu_rect`），运行时位置无从查证 ⇒ **不建**（纪律①）。
//   · **`Line`（出厂 INACT）/ `WebShop Button Square Variant`（出厂 INACT）/ `price-bg`（`m_Enabled=0`）
//     不建**。
//   · **滚动与 `RectMask2D`**：三个页的 `Viewport` 都带 `RectMask2D`（softness y 25），我们没做裁剪
//     ⇒ 内容超出视口的部分**会画到外面**（同锻造厂页那个缺口）。**出声**。
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
            var go = new GameObject("Shop Menu Variant");
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
            _btnBadge = res.badge;

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Shop] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray())
                                 + " —— 导入器：`工具/import_original_art.py` 的 `MENU_IMAGES`");
            // ⚠️ 只画一次、不静默：裁剪与滚动**没实现**
            Debug.Log("[Shop] `Viewport` 的 `RectMask2D` 裁剪与 `ScrollRect` 滚动**本轮没做**"
                      + "（原版 `RectMask2D` softness y=25；内容超出视口的部分会画到外面 —— 同锻造厂页那个缺口）");
        }

        Transform[] _btnRoot;
        ImageQuad[] _btnHighlight, _btnBadge;

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
        public void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            for (int i = 0; i < _btnHighlight.Length; i++)
                if (_btnHighlight[i] != null) _btnHighlight[i].gameObject.SetActive(i == sel);
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
        /// <summary>主图的框：抽屉区**去掉下面那两条文字带**之后的余量。</summary>
        public static readonly PxRect CellArtBox = new PxRect(11f, 64f, 325.6f, 272f);
        /// <summary>没有主图时画的占位板（**中性灰底 + 短名**，不拿别的图冒充）。</summary>
        public static readonly Color ArtPlaceholderTint = new Color(0.16f, 0.16f, 0.18f, 1f);

        public static readonly Color WarnColor = new Color(0f, 0f, 0f, 1f);                  // 原版那行是**纯黑字**

        // 渲染队列：**这一层是「页」**（窗外壳 3005…3014 之下，弹窗 3110+ 之上）
        public const int QHeader = 3020, QHeaderText = 3021, QGrid = 3022,
                         QCellBg = 3023, QCellArt = 3024, QCellText = 3025,
                         QCellPrice = 3026, QCellPriceText = 3027, QCellCount = 3028, QCellCountText = 3029,
                         QEmpty = 3030;

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
            // ⚠️ `Scroll View`/`Viewport` 自己的 `Image` 是 `UIMask` 且 **alpha = 0** ⇒ 不画；也没做裁剪
            var vp = MainMenuSubmenuWindow.Node(sv, "Viewport", ScrollView);
            var content = MainMenuSubmenuWindow.Node(vp, "Content", ScrollView);
            BuildGrid(content);

            // ---- ③ `Empty Collection Warning`（出厂 INACT；原版在列表为空时显示）----
            var ew = MainMenuSubmenuWindow.Node(_root, "Empty Collection Warning", EmptyWarn);
            var wl = _win.Text(ew, TxtEmpty, EmptyWarn.x1, EmptyWarn.x2, EmptyWarn.y1, EmptyWarn.y2, 5,
                               WarnColor, "Warning", 36f);
            if (wl != null) { wl.SetRenderQueue(QEmpty); MenuDraw.AlignRight(wl, EmptyWarn); }
            // 我们的每一页都有商品 ⇒ **恒不显示**（留着是为了将来真出现空列表时能用）
            ew.gameObject.SetActive(false);

            if (NoArtOffers.Count > 0)
                Debug.LogWarning("[Shop] ⚠️ 第 " + (_page + 1) + " 页有 " + NoArtOffers.Count
                                 + " 件商品**没有主图**（画的是中性灰占位板，**没拿别的图冒充**）："
                                 + string.Join("、", NoArtOffers.ToArray())
                                 + " —— 原版这一步是服务端给的货图，我们编的商品表里没填 `Art`（正本 §六 第 2 条）");
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
                else parts[i].localPosition = new Vector3(
                        LayoutSpace.FromPixel(cx, 0f).x - tc.position.x, parts[i].localPosition.y, 0f);
                x += w + 5f;
            }
        }

        /// <summary>`GridLayoutGroup`：`cellSize (335.6,475)` · `spacing (0,0)` · `padding.top 7` ·
        /// `constraint Flexible / count 2` · `startCorner UpperLeft` · `startAxis Horizontal`
        /// ⇒ 格位 = `(col, row) × (335.6, 475) + (0, 7)`（相对 `Content` 左上角）。</summary>
        void BuildGrid(Transform content)
        {
            var offers = ShopData.Offers(_page);
            for (int i = 0; i < offers.Length; i++)
            {
                int col = i % GridCols, row = i / GridCols;
                float x1 = ScrollView.x1 + col * CellW;
                float y1 = ScrollView.y1 + GridPadT + row * CellH;
                var r = new PxRect(x1, y1, x1 + CellW, y1 + CellH);
                BuildCell(MainMenuSubmenuWindow.Node(content, "CatalogItemShopContainer_" + i, r), r, i, offers[i]);
            }
            // ⚠️ **`ContentSizeFitter(Vertical=Preferred)`** 会把 `Content` 撑到 `行数 × 475 + 7`
            //    —— 我们没做滚动 ⇒ 只记录一句，不改矩形
            if (offers.Length > 0)
                Debug.Log("[Shop] 第 " + (_page + 1) + " 页 `Content` 的原版实高应为 "
                          + ((offers.Length + GridCols - 1) / GridCols * CellH + GridPadT).ToString("F1")
                          + "（`ContentSizeFitter(V=Preferred)`；我们没做滚动 ⇒ 不撑）");
        }

        /// <summary>一格 `Catalog Item Shop Container`。格里几何照 `menu_rect --root-size 335.6x475 --relative` 原文。
        /// 🔴 **名字 / 类型两行是我们加的**（原版这两行由 `Card Drawer` 把卡画出来，我们还没有那套抽屉）。</summary>
        void BuildCell(Transform cell, PxRect r, int idx, ShopOffer o)
        {
            // `background`：`UI_Deck_Selection_Back_simple`（**Simple**，不是 Sliced）+ `Mask(showGraphic=1)`
            _win.Rect(cell, "UI_Deck_Selection_Back_simple",
                      Rect(r, CellBg), "background", QCellBg);

            // 商品主图（原版 `background` 的 `m_Sprite` 是 0、运行期由服务端赋图）
            // ⚠️ **主图的框是「抽屉区去掉下面两条文字带」** —— 第一版让它占满整个抽屉区，
            //    于是我们加的那两行字**压在商品图上**（Cards 页一眼可见，而**断言全绿**）。
            if (!string.IsNullOrEmpty(o.Art))
            {
                _win.Rect(cell, o.Art, Rect(r, CellArtBox), "Art", QCellArt, null, true);
            }
            else
            {
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

            // 我们加的两行字（**标明是我们加的**；各自一条带，谁也不压谁）
            //
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
                if (al != null) { al.SetRenderQueue(QCellText); MenuDraw.AlignRight(al, ar); }
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
                _win.Rect(cell, "40K_button", pr, "Generic UI Button", QCellPrice, PriceTint, true);
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
            }
        }

        /// <summary>把「格内局部矩形」换算成绝对矩形（格内几何是相对 `Catalog Item Shop Container` 的）。</summary>
        static PxRect Rect(PxRect cell, PxRect inner)
        {
            return new PxRect(cell.x1 + inner.x1, cell.y1 + inner.y1, cell.x1 + inner.x2, cell.y1 + inner.y2);
        }

        /// <summary>买一件。**照边界②：不扣钱**，只记账 + **出声**（红线：点了必须有反应）。</summary>
        public string Buy(int idx)
        {
            string got = ShopData.Buy(_page, idx);
            Debug.Log("[Shop] 买了 " + got + "（**不做真实经济**：资源固定 9999、不扣钱 —— 用户 2026-09-17 边界②）");
            // 拥有数变了 ⇒ 重建这一页（**卡变了就重建视图**，同卡池那条纪律）
            Setup();
            return got;
        }

        public string Dump()
        {
            return "ShopPage[" + ShopData.PageLabel(_page) + "] 商品 " + ShopData.Offers(_page).Length + " 件";
        }
    }
}
