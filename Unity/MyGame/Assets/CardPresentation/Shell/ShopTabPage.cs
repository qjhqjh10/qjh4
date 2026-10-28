// ShopTabPage.cs —— 从 `Shell/ShopWindow.cs` 里拆出来的（A1506：MonoBehaviour 的类名【必须】与文件名相同，
// 否则 Unity 建不出 MonoScript ⇒ 存场景时被烘成内联 `!u!115` 存根 ⇒ player 加载即崩）。零行为改动。
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 一页

    /// <summary>商店的一页。原版 `ShopTab : WindowTabBase`（`CardShopTab` / `ItemShopTab` 都继承它）。
    /// **四个页共用这一份 View**（🆕 2026-10-27：第 4 页 = VIP，原版 `VIPCardShopTab : CardShopTab`；前三页逐节点同构、只差时间条的第一个子件，VIP 页多一行 `VIP Status` 与一块非 VIP 遮罩 —— 见类尾）。</summary>
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
                    default: return _page == VipPage ? WindowTabType.ShopVip : (_page == CratesPage ? WindowTabType.ShopCrates : WindowTabType.ShopItems);   // 第 4 页 = VIP（见类尾那一段）· 🆕 2026-10-28（B91_W5）第 5 页 = Crates/Boosters（枚举门槛在 `RewardsWindow.cs`）
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

        /// <summary>🆕 **2026-10-18（N63）**：`TimeCounter` 的两颗字（`RefreshText` / `Time`）——
        /// 换语言时由 <see cref="RefreshLanguageTexts"/> 重设（`Loc` 不发静态事件，见 `Loc.SetLanguage`）。</summary>
        Label _refreshText, _refreshTime;

        /// <summary>🆕 **2026-10-18（N63）**：换语言后重设页内文案。页签文字由 `ShopWindow` 的左栏负责；
        /// 这里只处理本页 `TimeCounter` 那两颗（截图 `10_settings_english.png` 里「可用的」不随语档变）。</summary>
        public void RefreshLanguageTexts()
        {
            if (_refreshText != null) _refreshText.SetText(Loc.T("MenuShop/RefreshCounter"));
            if (_refreshTime != null) _refreshTime.SetText(ShopData.RefreshTime);
        }

        public void SetHost(MainMenuSubmenuWindow win, Transform root, int page)
        { _win = win; _root = root; _page = page; }

        /// <summary>🆕 **2026-10-18**：商品「类型」行（原版 `type`，例 `Booster Pack`）的显示文案。
        /// 官方表有 `MenuShop/ShopItemType/&lt;去空格的类型&gt;` 这一族 ⇒ 有键走词条，没键原样
        /// （⛔ 先 `HasEntry` 守一道：`Loc.T` 缺键会把键名印出来）。</summary>
        static string TypeText(string type)
        {
            if (string.IsNullOrEmpty(type)) return type;
            string key = "MenuShop/ShopItemType/" + type.Replace(" ", "");
            return Loc.HasEntry(key) ? Loc.T(key) : type;
        }

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
        /// <summary>🔴 **2026-10-18（A891 的续 · 第四轮）：那句空态文案走词条**（原来写死英文 —— 见下）。
        /// <para>键 = 原版那颗 `Warning` 的 `Localize.mTerm` 原文 **`MenuCollection/NoDecksFound`**
        /// （原版 `Collection Menu Variant > Decks Tab … > Empty Collection Warning/Warning` 那一份，
        /// **与本窗这一份同一条 mTerm**）。英文列 = 那颗 TMP 的 `m_text` 原文、**含原版自己的语病**
        /// （`There are no **deck** …` 单数，见 <see cref="TxtEmptyEn"/>）；中文列 = 「在您选择的过滤器中，您的收藏里没有卡组」（2026-10-14 官方 zh-CN 表；原「没有符合当前筛选的卡组」）。</para>
        /// <para>⛔ **别改回 `const`** —— 常量跟不了语言（这正是它原来「中文档印英文」的原因）。
        /// 渲染只读本属性；消费点 = `Build()` 那一处 `_win.Text(ew, TxtEmpty, …)`。</para>
        /// <para>⚠️ 本窗每一页都有商品 ⇒ 它**恒不显示**（`ew.gameObject.SetActive(false)`），
        /// 但文案仍按词条走（将来真出现空列表时直接就是对的）。</para></summary>
        public static string TxtEmpty { get { return Loc.T(TxtEmptyTerm); } }
        /// <summary>上面那条词条的**键**（原版 `Localize.mTerm` 原文；自检/诊断口与
        /// <see cref="TxtEmpty"/> 共用这一份）。</summary>
        public const string TxtEmptyTerm = "MenuCollection/NoDecksFound";
        /// <summary>无卡组那句的**英文兜底**。
        /// 🔴 **2026-10-14 就地订正（铁律 5 · `W4`/`A1803`）**：原来这里写「原版那颗 TMP 的 `m_text` **原文**，
        /// **含原版自己的语病**（`no deck` 单数）—— ⛔ 别顺手修正」—— **两句都错**：
        /// 官方 I2 表 `MenuCollection/NoDecksFound` 的 EN 是 **复数 `decks`**（`There are no decks in your
        /// collection for the selected filters`）⇒ **单数 `deck` 是我们自己写错的、不是原版语病**。
        /// （那串「原版 TMP 原文」是 **prefab 的静态默认值**，不是运行时真值 —— 运行时喂的是 I2 表。）
        /// ⇒ 按用户 2026-10-14 口径「**照抄官方**」取表里的值。</summary>
        public const string TxtEmptyEn = "There are no decks in your collection for the selected filters";

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

        /// <summary>🆕 **2026-10-17（界面线 `T07` · `A1753` #10）**：价签 `Button Text` 的**屏上颜色**。
        /// 原版那一颗挂 `Pragati-Regular Light Cream`，`_FaceColor = (0.992, 0.868, 0.733)`、顶点色白
        /// （屏上 = 顶点色 × 材质色 ⇒ 就是材质本色）。判据 = `资料/普查产出_1017/UI/R_T07_剩余四项.md` §4·2 #10。</summary>
        public static readonly Color PriceInk = new Color(0.992f, 0.868f, 0.733f, 1f);
        /// <summary>🆕 **2026-10-17（`A1753` #5）**：`Available Counter`（`Available: 1/5`）的屏上颜色 ——
        /// 原版材质 `Pragati-Regular Off White Warm`，`_FaceColor = (0.972, 0.937, 0.866)`、顶点白。</summary>
        public static readonly Color AvailableInk = new Color(0.972f, 0.937f, 0.866f, 1f);

        // 🔴 **我们自己划出来的三块**（原版那两行字是 `Card Drawer` 画在卡上的，见文件头）：
        //    名字 / 类型 / 主图**各占一条，谁也不压谁** —— 第一版把两行"顶"在抽屉区底部，
        //    而主图占满整个抽屉区 ⇒ **字压在商品图上**（Cards 页一眼就看出来，而**断言全绿**）。
        public static readonly PxRect CellTypeBand = new PxRect(9f, 282f, 325.6f, 316f);
        public static readonly PxRect CellNameBand = new PxRect(9f, 318f, 325.6f, 366f);
        /// <summary>主图的框：抽屉区**去掉下面那两条文字带**之后的余量。
        /// 🆕 A8：`ItemDrawer.Draw` 的 `box` 就吃它（见 `DrawerStyle` 的注释）。</summary>
        public static readonly PxRect CellArtBox = new PxRect(11f, 64f, 325.6f, 272f);

        /// 🔶 自己设计（非原版）：商店格走抽屉路线时的主图框 / 类型带 / 名字带版式 · 原版走 `ItemDrawer`，这套版式是我们挑的（原版判据拿不到） · TODO(谁定的)｜原注：<summary>🆕 A8：商店格**走抽屉那条路**时用的版式（`ItemDrawer.Draw` 那一份）。三处**我们挑的**：
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
            BuildTimeCounter(hdr, spec.TimerAsText); if (_page == VipPage && ShopData.IsVip) BuildVipStatus(hdr);   // VIP 页多一行（原版 `daily shop header/VIP Status`；非 VIP 时整行 `SetActive(false)` ⇒ 不建，见类尾那一段）

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
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()` —— 把**刚写进这个节点的那个矩形**记成基准
            //   （= 框的中心那一帧，口径 → `ViewportClip` 文件头 §①）。⛔ 少了它，这个视口**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：节点或它的**祖先**被挪过之后，框与被比矩形就**不在同一帧**。
            //   ⚠️ **必须在这里调**（`AddComponent` 之后、`BuildGrid` 之前）：`MenuDraw.ApplyPxRect` 写矩形时
            //   组件还不存在（那一句的穿透写在 `ApplyPxRect` 尾），所以只能由本行补 —— 同 `ViewportClip.Hang`
            //   里那句 `vc.SetBaseRect(r)` 的位置理由（`CardPresentation/Shell/ViewportClip.cs`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `CardPresentation/Shell/ViewportClip.cs` 明令）。
            _gridVp.CaptureNow();
            var content = MainMenuSubmenuWindow.Node(vp, "Content", ScrollView);
            BuildGrid(content); if (_page == VipPage && !ShopData.IsVip) BuildVipBlock(sv);   // 非 VIP ⇒ 原版把 `playerIsNotVIPObject`（`Packs Scroll View/No VIP` 整块遮罩）打开，见类尾那一段

            // ---- ③ `Empty Collection Warning`（出厂 INACT；原版在列表为空时显示）----
            var ew = MainMenuSubmenuWindow.Node(_root, "Empty Collection Warning", EmptyWarn);
            var wl = _win.Text(ew, TxtEmpty, EmptyWarn.x1, EmptyWarn.x2, EmptyWarn.y1, EmptyWarn.y2, 5,
                               WarnColor, "Warning", 36f, face: TmpFont.Face.LatinCardBody);
            if (wl != null) { wl.SetRenderQueue(QEmpty); MenuDraw.AlignRight(wl, EmptyWarn); }
            // 我们的每一页都有商品 ⇒ **恒不显示**（留着是为了将来真出现空列表时能用）
            ew.gameObject.SetActive(false);

            if (NoArtOffers.Count > 0) WarnNoArt(); if (_page == CratesPage && _win != null && _win.CurrentTab == WindowTabType.ShopCrates) TryOpenContainerArmy();   // 🆕 2026-10-28（B91_W5）crates 支 —— 判据/时机/为什么带 `CurrentTab` 闸，见 `TryOpenContainerArmy` 的 doc（⛔ 别把这两句拆成两行：本文件多处账按 `文件:行号` 钉着）
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
                // 🔴 **2026-10-18（第四轮）：这一颗的字走词条** —— 键 = 原版那颗 `RefreshText` 的
                //   `Localize.mTerm` 原文 **`MenuShop/RefreshCounter`**（4 颗同键；`Card Shop Tab` 那颗
                //   TMP = `Refreshes in:`、`Daily Shop Tab` 那颗 = **葡语占位串** `Atualiza em:`
                //   —— 但**同一条 mTerm** ⇒ 原版两页共用一条键，我们也共用）。
                //   ⚠️ 原来读的是 `Shell/ShopData.cs` 里那个**写死的英文常量** `ShopData.RefreshText`
                //   —— 它留着当「原版英文原文」的留档（⛔ 那份文件不在本批白名单里，一个字没动它）。
                //   ⛔ 节点名 `"RefreshText"` 不动（`Editor/ShopScene.cs` 按名找）。
                var lb = _win.Text(tc, Loc.T("MenuShop/RefreshCounter"), TimeCounter.x1, TimeCounter.x2,
                                   TimeCounter.y1, TimeCounter.y2, 5, Color.white, "RefreshText", 30f,
                                   TimeCounter.W, 10f, 30f, 25f, face: TmpFont.Face.LatinCardBody);
                // 🆕 **2026-10-10（A1212 块 6）接上自适应**：原版那一颗四格 = `10 / 30 / 25 / 0`，
                //   出处 = `资料/普查产出_第十会话/R6_包装层战斗与其余.md` §3（块 6 逐处表 =
                //   `资料/普查产出_第十一会话/RA1212_切块表.md` 块 6 #15）。闸（`MenuDraw.TextCore`）三条：
                //   `wrapPx 311.40 > 0` ∧ `autoMinPx 10 > 0` ∧ `fontPx 30 > 10` ⇒ 全真，真生效。
                //   ⚠️ 原版 `折行 = 0` ⇒ `SetAutoFitBox` 顺带打开的 `Normal(1)` 要**显式还原**
                //   （两条路的帮助器都**没有**任何 wrap 还原，见块表那段注）。
                if (lb != null) { lb.SetWrapping(false); lb.SetRenderQueue(QHeaderText); parts.Add(lb.transform); widths.Add(lb.WorldW * 108f); _refreshText = lb; }
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
            // 🆕 **2026-10-10（A1212 块 6）接上自适应**：原版那一颗四格 = `15 / 30 / 25 / 0`
            //   （出处 = `RA1212_切块表.md` 块 6 #16）。闸三条全真（`311.40 > 0` ∧ `15 > 0` ∧ `30 > 15`）。
            //   ⚠️ `折行 = 0` ⇒ 显式还原（同上）。
            var tm = _win.Text(tc, ShopData.RefreshTime, TimeCounter.x1, TimeCounter.x2,
                               TimeCounter.y1, TimeCounter.y2, 5, Color.white, "Time", 30f,
                               TimeCounter.W, 15f, 30f, 25f, face: TmpFont.Face.LatinCardBody);
            if (tm != null)
            {
                tm.SetWrapping(false); tm.SetRenderQueue(QHeaderText); parts.Add(tm.transform); widths.Add(111.31f);
                _refreshTime = tm;
                // 🆕 **2026-10-17（W_A_TimerDisplay）**：真倒计时 —— 原版这一格就是 `TimerDisplay`
                //   （`ShopTab.Setup` → `TimerDisplay.Initialize(CheckTimer(...), "")`）。
                //   🔴 **2026-10-27（B88_J1）就地订正（铁律 5）**：原来写「三页统一按 9 点规则（我们挑的）」——
                //     原版 `SetStore` 第 ① 步是**按页问 `CheckTimer`**、假就**整块隐藏**，只有
                //     `CardShopTab`/`CosmeticsShopTab` 返回 true ⇒ 现在按页分档（见方法 `PageShowsTimer`）。
                //   · 本机时钟（无服务器 —— 本项目允许的偏离）。
                //   · 原版这一颗 `useLastMinutesText = 1` ⇒ 最后 5 分钟描述串换成 `MainMenu/LastMinutes`（`MonoBehaviour_-5363483528564307958.json` 实读；常量 `DAT_1834b3380` = 5.0）。
                if (PageShowsTimer(out System.DateTime _timerDue))
                    TimerDisplay.Attach(tm, () => TimerDisplay.ShopRefreshAt(System.DateTime.Now), "", "商店 TimeCounter", null, TimerDisplay.LastMinutesText());
                else tc.gameObject.SetActive(false);   // 原版 `SetStore()` ②：`CheckTimer` 假 ⇒ 整块 `SetActive(false)`
            }

            // 居中排：总宽 = Σ + spacing×(n−1)，从容器中心往两边分
            float total = 0f;
            for (int i = 0; i < widths.Count; i++) total += widths[i];
            total += 5f * (widths.Count - 1);
            float x = TimeCounter.CX - total * 0.5f;
            for (int i = 0; i < parts.Count && tc.gameObject.activeSelf; i++)   // 整块被 `SetStore` 藏掉时不再量/摆（TMP 在未激活件上量不出尺寸）
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
                _gridScroll = MenuScroll.TopAligned(ScrollView, contentH, 1f);   // 原版各 `… Shop Tab/Packs Scroll View` = 1.0（D_N29）
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
                // 🔴 **2026-10-09（A1173）**：autosize 收进 `Text` 的形参（原来是先 `Text`、再手工补一次）。
                //    ⚠️ 框 = **内缩 8 之后那一格**（`pr.W−16` × `pr.H−16`）—— 与上面那对 `x1+8/x2−8` 同一个框。
                var pl = _win.Text(cell, ShopData.ShortName(o.Name), pr.x1 + 8f, pr.x2 - 8f,
                                   pr.y1 + 8f, pr.y2 - 8f, 5, new Color(0.78f, 0.78f, 0.82f, 1f),
                                   "PlaceholderName", 24f,
                                   wrapPx: pr.W - 16f, autoMinPx: 14f, autoMaxPx: 24f, face: TmpFont.Face.LatinCardBody);
                if (pl != null) pl.SetRenderQueue(QCellText);
            }

            // 🆕 2026-10-03（§三 第 29 条 A6）：`Booster Pack` 那一类商品 —— **点主图开 `Booster Info Popup`**。
            // 🔴 **入口是我们定的、不是复刻**：原版走 `ShopOfferContainer.OnClick → OpenContainer`
            //    （从 offer 取 AssetGroup 0xc 的窗口），而**我们用的这族 `CatalogItemContainer`
            //    只画抽屉、不开窗**（2026-10-03 查实；判据 → `阶段二_商店_原版规格.md` §五·二 末尾）。
            //    只在卡包上接：那扇窗的内容是**卡包保底进度**，非卡包商品套不上。
            if (o.Type == "Booster Pack")
                // 🆕 **2026-10-18（音效线缺口 `AUD` §三 B12）**：原版
                //   `Shop Menu Variant/…/Comsetic Item Shop Container/raycast target` = **`Purchase Click`**
                //   ⇒ cue 落 `PurchaseCue`。⚠️ 语义差异（我们这颗是「开 `BoosterInfoPopup`」、原版是
                //   `OpenContainer`）属界面线既有账，本次只归音。
                MenuDraw.Hit(cell, "InfoHit", Rect(r, CellArtBox), QCellInfoHit, () => OpenBoosterInfo(idx),
                             clickCue: WindowSounds.PurchaseCue);

            // 🆕 **2026-10-18（第五十一会话 · 真机「中英混排」）**：类型行原来直接印 `o.Type`（英文）。
            //   官方表有 `MenuShop/ShopItemType/<去空格的类型>` 那一族 ⇒ 有键就走词条，没键原样。
            //   ⚠️ 先 `HasEntry` 守一道：`Loc.T` 缺键会把**键名**印出来（本会话商店页签就栽过一次）。

            // 我们加的两行字（**标明是我们加的**；各自一条带，谁也不压谁）
            // 🔴 **必须开自适应字号**：`Ultramarines Booster` 在 fs30 下实测宽 **328px**，
            //    而名字带只有 **316.6px** ⇒ `AlignRight` 之后**左边越出格子 2.8px**
            //    （同 `资料/已知的坑.md` 那条「AutoFitBox：字号对而溢出，自检照样全绿」）。
            //    ⚠️ 顺序：**先 `SetAutoFitBox` 再 `AlignRight`** —— 对齐是按**当前**文字宽度算的。
            //    🔴 **2026-10-09（A1173）**：那一刀 `SetAutoFitBox` 收进 `Text` 的形参；**次序不变**
            //      （漏斗里那两步排在返回之前 ⇒ 仍早于下面这句 `AlignRight`）。
            {
                var tyR = Rect(r, CellTypeBand);
                var ty = _win.Text(cell, TypeText(o.Type), tyR.x1, tyR.x2, tyR.y1, tyR.y2, 5,
                                   new Color(0.717f, 0.717f, 0.717f, 1f), "Type", 26f,
                                   wrapPx: tyR.W, autoMinPx: 16f, autoMaxPx: 26f, face: TmpFont.Face.LatinCardBody);
                if (ty != null)
                {
                    ty.SetRenderQueue(QCellText);
                    MenuDraw.AlignRight(ty, tyR);
                }
                var nmR = Rect(r, CellNameBand);
                var nm = _win.Text(cell, o.Name, nmR.x1, nmR.x2, nmR.y1, nmR.y2, 5, Color.white, "Name", 30f,
                                   wrapPx: nmR.W, autoMinPx: 18f, autoMaxPx: 30f, face: TmpFont.Face.LatinCardBody);
                if (nm != null)
                {
                    nm.SetRenderQueue(QCellText);
                    MenuDraw.AlignRight(nm, nmR);
                }
            }

            // `Available Counter`：**有限购信息才画**（`AvailableMax > 0`）；原版是 `Available: 1/5`，hAlign **Center**。
            // 🔴 **2026-10-19（N72 现核 · 铁律 5 就地订正）**：本行原文写「hAlign Right」——**不成立**。
            //   现跑 `menu_dump.py bundle_menus_assets_all "Catalog Item Shop Container"`，**6 份同名变体逐个实读**
            //   全是 `对齐=Center/Midline`（`m_HorizontalAlignment = 2` + `m_VerticalAlignment = 4096`）；
            //   错因 = `资料/阶段二_商店_原版规格.md:143` 那格 `Right` 是**旧工具 hAlign 错位**留下的值
            //   （同 `阶段二_战斗入口_原版规格.md` 文件头那条横幅）。⇒ 横向**不调 `Align*`**（`Label` 出厂就是
            //   按框中心摆整块，见 `Battle/Label.cs` 的 `RefreshBounds`），对齐由框本身（`CellAvail`）给。
            if (o.AvailableMax > 0)
            {
                var ar = Rect(r, CellAvail);
                // 🆕 **2026-10-17（A1236）：判据已查实 ⇒ 改【值】，⛔ 不动共用闸。**
                //   按 `A1208` 的既定口径「先看原版那一颗的 `m_fontSizeBase`」：
                //   全包 50 颗 `m_text` 含 `'Available:'` 的 TMP 里，本处用的是 **5 颗那一档**，
                //   逐颗实读 = **`fs 16 / min 16 / max 31.5 / base 39 / 折行 0`**
                //   （本次全包扫 `bundle_menus_assets_all/MonoBehaviour/*.json`）。
                //   `base(39) > min(16)` ⇒ **原版有可缩余量**、我们原来把 `fontPx` 抄成了运行时字号 16
                //   ⇒ 现在标称改 **39**（= `m_fontSizeBase`，TMP 自适应的起点）+ 三格照实传。
                //   ⚠️ 闸 `fontPx > autoMinPx` 现在是 `39 > 16` ✓ ⇒ `SetAutoFitBox` 真会执行。
                //   ⚠️ 原版**折行 = 0**：`SetAutoFitBox` 内部那句 `SetWrapWidth` 会**无条件**把模式开成
                //   `Normal` ⇒ 返回后必须 `SetWrapping(false)` 还原（同 `EnergySinglePlayerOnly…:453` 的成对写法）。
                var al = _win.Text(cell, "Available: " + o.Available + "/" + o.AvailableMax,
                                   ar.x1, ar.x2, ar.y1, ar.y2, 5, AvailableInk, "Available Counter", 39f,
                                   wrapPx: ar.W, autoMinPx: 16f, autoMaxPx: 31.5f, autoBasePx: 39f, face: TmpFont.Face.LatinCardBody);
                if (al != null)
                {
                    al.SetWrapping(false);        // 原版 `m_TextWrappingMode = 0`
                    al.SetRenderQueue(QCellText);
                    // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版这颗 `Available Counter`
                    //   是 `v = Geometry (4096)` = **`Midline`**（判据 = `CellAvail` 那条注释自己逐字写的
                    //   `m_VerticalAlignment 4096`）。⚠️ 原版那颗是**零高框、锚点落在格底之外**，
                    //   我们**把整条抬进格子里**（`CellAvail`，见 `:291-295` 那条如实登记的偏离）
                    //   ⇒ 本笔只落**档位**，框仍是我们挑的那个（同 A712 阶段 2 的口径）。
                    MenuDraw.SetVAlign(al, Label.VAlign.Midline, ar);
                    // 🔴 **2026-10-19（N72）**：横向 **Center** —— 原版 6 份变体实读 `Center/Midline`，
                    //   我们原来 `AlignRight` 把整串往右推 ~55px。⛔ **别改回 `AlignRight`**：那条是旧施工图的错值。
                    //   不调 `Align*` = 整块按 `ar` 的中心摆（`Label.RefreshBounds` 的出厂口径）。
                }
            }

            // `Counter`（拥有数角标）+ 它的 `Text (TMP)` `x14`
            {
                var cr = Rect(r, CellCount);
                _win.Rect(cell, "40K_main_deck_card_counter", cr, "Counter", QCellCount, null, true);
                var tr = Rect(r, CellCountText);
                // 🆕 **2026-10-10（A1212 块 6）接上自适应**：原版那一颗（`x{Owned}` 角标）四格 =
                //   `18 / 72 / 32 / 1`（出处 = `RA1212_切块表.md` 块 6 #18）。闸三条全真（`23 > 18`）。
                //   ⚠️ 折行 = **1** ⇒ 不用还原（`SetAutoFitBox` 顺带开的就是 `Normal(1)`）。
                var tl = _win.Text(cell, "x" + ShopData.OwnedOf(_page, idx),
                                   tr.x1, tr.x2, tr.y1, tr.y2, 5, Color.white, "Text (TMP)", 23f,
                                   tr.W, 18f, 72f, 32f, face: TmpFont.Face.LatinCardBody);
                if (tl != null)
                {
                    tl.SetRenderQueue(QCellCountText);
                    // 🔴 **2026-10-18（N59）**：原版 `Counter/Text (TMP)` 是 **`Center/Middle`**（判据 =
                    //   `Catalog Item Shop Container` dump：框 58.48..277.12、对齐 Center/Middle）。
                    //   我们原来 `AlignRight` ⇒ `x3` 被推到计数药丸（居中只画 ~105px 宽）外面。
                    MenuDraw.SetVAlign(tl, Label.VAlign.Midline, tr);
                }
            }

            // `Price Display Button` → `Generic UI Button`（`40K_button`，Simple + preserveAspect，金色）
            {
                var pr = Rect(r, CellPrice);
                // 🆕 2026-10-03 A17：原版这一颗是 SpriteSwap —— `40K_button` → `40K_button_hover`
                //（普查 §块 2 第 1 行：`Catalog Item Shop Container>background>price-bg>Price Display Button>Generic UI Button`）
                var priceQ = _win.Rect(cell, "40K_button", pr, "Generic UI Button", QCellPrice, PriceTint, true);
                // 🔴 **2026-10-18（N59）**：原版 `Price Display` = **金币图标 + 数字**
                //   （`Catalog Item Shop Container` dump：`Price Display` 86.36,416.86 → 247.86,442.54，
                //   子件 `icon` = `40k_topmarquee_currency_gold`、ARF 高控宽 1:1；我们原来只画数字）。
                var iconR = Rect(r, new PxRect(86.36f, 416.86f, 112.04f, 442.54f));
                _win.Rect(cell, "40k_topmarquee_currency_gold", iconR, "Price Icon", QCellPrice, null, true);
                var textR = Rect(r, new PxRect(112.04f, 416.86f, 247.86f, 442.54f));
                // 🆕 **2026-10-10（A1212 块 6）接上自适应**：原版那一颗（价格）四格 = `13.46 / 40 / 39 / 0`
                //   （出处 = `RA1212_切块表.md` 块 6 #19）。闸三条全真（`30 > 13.46`）。
                //   🔴 **同层另有【一颗也叫 `Button Text`】**（`INACT`/空串/`fs12·base12·auto[12~38]`）
                //   —— 切块表点名「**别抄它**」；本处只填这一颗自己的四格，**节点名一个字不动**
                //   （`Editor/ShopScene.cs` 按名找）。
                //   ⚠️ 折行 = **0** ⇒ 显式还原（同 `RefreshText`）。
                // 🔴 **2026-10-25（`C` 商店域）**：价格文本的三档照原版 `PriceDisplay`（判据 `PriceDisplay.cs:20-100`）：
                //   · **免费** ⇒ `Loc.T("MainMenu/Free")`（`SetupFree` 的 `PRICE_FREE`）；
                //   · **空** ⇒ **`--`**（`Setup(string)` 里 `buttonText == null ⇒ "--"`）；
                //   · **`useStrikeThrough`** ⇒ 文本**前插 `<s>`**（TMP 删除线；原版**只插开头那一个、不闭合**，
                //     `TryInsertStrikeThrough` 逐字如此）。⚠️ 今天商品表里没有一件打开后两档（组件补齐、数据可开）。
                string ptxt = o.Free ? Loc.T("MainMenu/Free")
                            : string.IsNullOrEmpty(o.Price) ? "--"
                            : o.Price;
                if (o.StrikeThrough) ptxt = "<s>" + ptxt;
                var lt = _win.Text(cell, ptxt, textR.x1, textR.x2, textR.y1, textR.y2, 5, PriceInk,
                                   "Button Text", 30f, textR.W, 13.46f, 40f, 39f,
                                   face: TmpFont.Face.LatinCardBody);
                if (lt != null) { lt.SetWrapping(false); lt.SetRenderQueue(QCellPriceText); }
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
                    ShowLegendaryConfirm(_win.Manager, () => DoBuy(captured));
                    return "";
                }
                Debug.LogWarning("[Shop] 没有 `WindowsManager` ⇒ **弹不出确认框**（这一件按原版不该直接买）");
                return "";
            }
            return DoBuy(idx);
        }

        /// <summary>把「买传奇」那扇确认框开出来 —— **左 = `Cancel`（不买）· 右 = `OK`（买）**。
        /// 生产路径 <see cref="Buy"/> 与自检都**只走这一处**（⛔ 别在调用点另拼一遍两颗钮的顺序）。
        ///
        /// <para>🔴 **2026-10-24（`A11` · 铁律 5 就地订正：这里原来左右是反的）**。
        /// 原版 `d:/2/tools/decomp_full/CatalogItemContainer__TryPurchase.c:30-50` 与
        /// `CraftingPanel__TryCrafting.c:26-49` 是**同一形状**：`buttons[0] = Cancel`（左）、
        /// `buttons[1] = OK`（右）；而 `PopUpGameWindow.ConfigurePopUp` 的 `LiveButtons[0]` = `ButtonLeft`
        /// ⇒ `ShowPopUp(…, primaryButton, secondButton)` 的 **primary = 左**。
        /// 改前我们把 `OK` 当 primary 传 ⇒ 商店那扇**左 OK / 右 Cancel**，与隔壁锻造确认窗
        /// （左 Cancel / 右 OK）自相矛盾。⇒ 现在按原版：**primary = `Cancel`（回调 null ⇒ 只关窗）·
        /// secondary = `OK`（回调 = 真正的购买）**。
        /// ⚠️ 两颗钮**都**要「点下去关窗」：原版 `ConfigurePopUp` 无条件挂
        /// `PopUpGameWindow.PopUpWindowButtonPressed`（第二个监听专门关窗）。我们这里由
        /// `WindowsManager.ShowPopUp` 那层包装负责（先 `Close()` 再跑调用方回调，⛔ 别绕开它直调
        /// `ShowMessagePopUp`，那样 Cancel 那颗就关不掉）。</para>
        /// <para>⚠️ 文案走词条（`MainMenu/General/Cancel` = 「取消」· `MainMenu/General/OK` = **「好的」**）——
        /// 改前这里写着「中文列『确定』」，那是**过期**的：官方本地化表 `MainMenu/General/OK` 的 ZH 列
        /// 实读 = 「好的」（`数据/游戏数据/官方本地化_en+zh-CN.json`，2026-10-24 现读）。
        /// 键名出处 = 原版 `CatalogItemContainer__TryPurchase.c:37,41,43` 传的那两个地址
        /// （`0x42BE718` / `0x42BE120`）的地址表实读。</para>
        /// <returns>开出来的那扇窗（`WindowsManager` 为 null / 建失败时为 null）—— 自检读口。</returns>
        public static PopUpGameWindow ShowLegendaryConfirm(WindowsManager mgr, System.Action onConfirm)
        {
            if (mgr == null) return null;
            mgr.ShowPopUp(ShopData.LegendaryWarnText,
                          Loc.T("MainMenu/General/Cancel"), null,          // 左：Cancel（不买，只关窗）
                          Loc.T("MainMenu/General/OK"), onConfirm);        // 右：OK（买）
            return mgr.popUpWindow as PopUpGameWindow;
        }

        /// <summary>真正掏钱那一步（确认框点**右钮 OK** 之后走的也是它）。
        /// <para>🆕 **2026-10-12（A439）：买到手 ⇒ 弹领奖窗**（非容器档那几件）。判据见下面那一段注释。</para></summary>
        string DoBuy(int idx)
        {
            // ---- 🆕 2026-10-28（B88_B5）：**原版那一扇「正在处理您的购买…」等待框** ----
            // 判据（🥇 VA 级反汇编 + 源码级还原 + 官方词条，三源一致）→
            //   `资料/完全复刻/判据/B88_R6_ShopStore购买链_VA判定.md` §一/§二。
            // 原版 `ShopStore.TryPurchase`（泛型实例 RVA `0x1253FA0`）的**第一句、早于任何分支**：
            //   `WindowsManager.Instance.ShowPopUp("MenuShop/WaitingPopup", localizeTexts: true,
            //                                     closeOnEsc: false, primaryButton: null, secondButton: null)`
            // ⇒ **0 颗钮 · Esc 关不掉 · 只能等回包后由回调收掉**。
            // 文案键现成（`official_loc_13lang.json`：EN `Processing your purchase...` · ZH `正在处理您的购买...`）
            // ⇒ ⛔ 不用编文案。
            // 🔴 **照原版结构原样接、⛔ 不加延迟**（调度台 2026-10-28 裁定）：我们的购买是**同步**的
            //   ⇒ 这两句会在**同一帧内**开窗又关窗、玩家看不见 —— 那是**原版结构落在同步购买链上的副产品，
            //   不是缺陷**（原版看得见是因为中间隔着一次网络往返）。⇒ 自检**只断结构**
            //   （弹过 / 0 颗钮 / 随后被收掉），⛔ **不断「可见时长」**。
            var wm = _win != null ? _win.Manager : null;
            if (wm != null)
            {
                LastWaitingPopup = wm.ShowMessagePopUp(WaitingPopupTerm, null, null, null, null, closeOnEsc: false);
                // 自检读口：弹起那一刻主钮是否已被藏掉（= 原版 0 颗钮那一档真的落地了）
                LastWaitingPopupHadNoButton = LastWaitingPopup != null && !LastWaitingPopup.PrimaryVisible;
            }
            else
            {
                LastWaitingPopup = null;
                LastWaitingPopupHadNoButton = false;
                Debug.LogWarning("[Shop] 没有 `WindowsManager` ⇒ 弹不出原版那句「正在处理您的购买…」"
                               + "（原版 `ShopStore.TryPurchase` 第一句：0 颗钮、Esc 关不掉）");
            }
            string got = ShopData.Buy(_page, idx);
            // = 原版三条回调的**共用第一句** `WindowsManager.HidePopUp(forceHide: true)`：
            //   `ShopStore…TryPurchase_b__0.c:17` · `…HandlePurchaseComplete_1.c:13` · `…HandlePurchaseError_2.c:13`。
            // ⚠️ 时序照原版：**先关等待框，再 `Setup()` / 开下一扇窗**（⛔ 别反过来 —— 两扇窗会抢栈顶）。
            if (wm != null) wm.HidePopUp(true);
            Debug.Log("[Shop] 买了 " + got + "（**不做真实经济**：资源固定 2000（用户 2026-10-24 从 9999 改）、不扣钱 —— 用户 2026-09-17 边界②）");
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
                // 🔶 自己设计（非原版）：「买完开包」的入口挂在购买成功之后 · 原版这条链的上游在服务端（买成功 → 服务端回执 → 弹开包窗），本地没有 · TODO(谁定的)｜原注：🔴 入口是我们定的那一处：原版这条链的上游在服务端（买成功 → 服务端回执 → 弹开包窗），
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

        // ============================================================ 原版 `ShopTab.SetStore()` 的第 ① 步
        // 🔶 自己设计（非原版）：三页 ↔ 原版类的**对应关系**本地没有判据（prefab 只给节点名），
        //   而 `ItemShopTab.CheckTimer` 要的是 LiveOps store 事件数据（本机没有）；`DailyShopTab` 这个类
        //   **原版根本不存在**（`grep "class .*ShopTab" dump.cs`：ShopTab / ItemShopTab / CardShopTab /
        //   CosmeticsShopTab / GoldShopTab / BoosterShopTab / VIPCardShopTab）。⇒ **只有 Cards 页**
        //   按 `CardShopTab` 的 9 点规则返回 true（🆕 VIP 页同理：`VIPCardShopTab : CardShopTab` **不 override `CheckTimer`** ⇒ 也是 true）；**Daily / Items 返回 false（整块隐藏）**。
        //   依据 / 反证 / 还差什么 → `资料/完全复刻/报告/B88_J1_两条悬案裁定.md`「第二批」。调度台 2026-10-27 裁定。

        /// <summary>原版 `ShopTab.SetStore()` 的第 ① 步：**先问本页「要不要显示刷新计时器」**
        /// （虚方法 `CheckTimer(out DateTime timer)`；`decomp_full/ShopTab__SetStore.c:22-40`：
        /// 假 ⇒ 整个 `TimeCounter` 块 `SetActive(false)`；真 ⇒ `SetActive(true)` ＋ `TimerDisplay.Initialize(timer, "")`）。
        /// <para>四个 override（`dump.cs:91035 / 91086 / 91193 / 91733` 现读 ＋ 各自 `.c`）：
        /// · 基类 `ShopTab.CheckTimer` = **恒 false**（`ShopTab__CheckTimer.c` 全文 = `out = DateTime.Now; return false;`）；
        /// · `CardShopTab` / `CosmeticsShopTab` = **9 点规则**（`<9 点取当天、否则次日` ＋ 9h），返回 `0 &lt; (target−now).TotalMinutes`；
        /// · `ItemShopTab` = `LiveOpsUtility.IsTimedEvent(store, out millis)`；
        /// · `GoldShopTab` / `BoosterShopTab` **不 override** ⇒ 走基类 = false。</para></summary>
        public bool PageShowsTimer(out System.DateTime due)
        {
            if (_page == 0 || _page == VipPage)   // Cards = 原版 `CardShopTab`（9 点规则）；VIP = 原版 `VIPCardShopTab : CardShopTab`，**它不 override `CheckTimer`** ⇒ 同一条 9 点规则
            {
                due = TimerDisplay.ShopRefreshAt(System.DateTime.Now);
                return (due - System.DateTime.Now).TotalMinutes > 0.0;
            }
            due = System.DateTime.Now;   // 基类 `ShopTab.CheckTimer` 就是 `out = Now; return false;`
            return false;
        }

        // ============================================================ VIP 页（第 4 页 · 原版 `VIPCardShopTab : CardShopTab`）
        //
        // ---- 判据（全部从解包资源 / 反编译字段读，⛔ 不是截图估的）----
        // 几何：`工具/menu_dump.py bundle_menus_assets_all "Card Shop VIP Tab Variant"`
        //   ＋ `工具/menu_rect.py bundle_menus_assets_all "Card Shop VIP Tab Variant" --size 1752.83x1009.06 --relative --cs`
        //   （本页 prefab 的 `m_Father = 0` ⇒ 量法与另外三页**同一套**：相对 `Tabs` 左上角量，再统一 +(167.17, 70.94)）
        // 生命周期（`d:/2/tools/decomp_full/VIPCardShopTab__{OnOpen,OnClose,ToFocus,VipTimerOnOnRunOutOfTime,CheckVIPState}.c` 逐方法）：
        //   · `OnOpen`   = `RefreshOffers(); CheckVIPState();` ＋ 调试钮闸 ＋ `vipTimer.OnRunOutOfTime += VipTimerOnOnRunOutOfTime`
        //   · `OnClose`  = `vipTimer.OnRunOutOfTime -= VipTimerOnOnRunOutOfTime;`（就这一句）
        //   · `ToFocus`  = `RefreshOffers(); CheckVIPState();`
        //   · `VipTimerOnOnRunOutOfTime` = `CheckVIPState();`
        //   · `CheckVIPState` = `bool can = Store.CanPurchase;`（**本地逻辑**：`DailyStore.get_CanPurchase` RVA `0x8CC740` = `requiredItem == null || 背包里有它`；⛔ 不是服务端。铁律 5 订正 2026-10-28 B88_B5）⇒ `playerIsVIPObject.SetActive(can);`
        //                       `playerIsNotVIPObject.SetActive(!can);` ＋（`can` 时）`vipTimer.Initialize(GetExpirationDate(RequiredItem))`
        // 🔴 **那两个 GameObject 是谁，是现解 prefab 的 PPtr 定的**（⛔ 不是按名字猜 —— 树里有两个同名 `No VIP`）：
        //   `playerIsVIPObject`    = PPtr `-8306817185179266151` = **`daily shop header/VIP Status/Yes VIP`**
        //   `playerIsNotVIPObject` = PPtr `-252537654527552615` = **`Packs Scroll View/No VIP` 那整块遮罩**
        //   ⛔ 头部那个同名 `No VIP`（`VIP Status/No VIP`）**不是它**：那个出厂 `m_IsActive = 0`，
        //      而且全包**没有任何 MonoBehaviour 引用它** ⇒ 原版自己也不画 ⇒ 我们不建。
        // ⚠️ 页头那颗 `Reset`（文案 `Grant VIP (1 day)`）就是 `DebugGrantVIPButton` 所在件
        //   （PPtr `-5385996312866024551` → GO `Reset`）：`OnOpen` 里那道闸
        //   `!EnvironmentHelper.IsPlayerFacingEnv && SettingsDebugButtons.DebugUIEnabledUI && Store.RequiredItem != null`
        //   在**玩家面恒假** ⇒ 原版玩家看不到它 ⇒ **不建**（账 330 已按 debug-only 判 D）。

        /// <summary>VIP 页在 `ShopData.Pages` 里的下标（第 4 页）。</summary>
        public const int VipPage = 3;

        /// <summary>页头那三颗字的官方词条（= 原版 prefab 里 `Localize.mTerm` 实读；EN/ZH 两列都在
        /// `CardPresentation/Resources/official_loc_13lang.json`：`You are VIP. Time Remaining:` / `您是 VIP。剩余时间：`、
        /// `You have to become VIP to access this store` / `您必须成为 VIP 才能访问此商店`、
        /// `Stay consistent with your daily streak and you’ll gain VIP status.` / `坚持每日连胜，您将获得 VIP 身份。`）。</summary>
        public const string VipHeaderTerm = "MenuShop/VIPCardStore/YouAreVIPHeader",
                            VipNotTerm = "MenuShop/VIPCardStore/YouAreNotVIP",
                            VipTooltipTerm = "MenuShop/VIPCardStore/YouAreNotVIPTooltip";

        /// <summary>`VIP Status`（原版 `daily shop header/VIP Status`）：相对值 **720.72,12.10 → 1032.11,60.99**
        /// （311.40 × 48.89 · 锚 `(0.5,1)-(0.5,1)` · `pos (0,−12.1)` ⇒ 挂在页头**顶边**、水平居中）
        /// ＋ `Tabs` 左上角 **(167.17, 70.94)** ⇒ 绝对 **887.89,83.04 → 1199.28,131.93**。</summary>
        public static readonly PxRect VipStatus = new PxRect(887.89f, 83.04f, 1199.28f, 131.93f);
        /// <summary>`VIP Icon`（`40K_icon_card_VIP` 512²、`Simple + preserveAspect` ⇒ 画出来 68.353²）：
        /// 锚 `(0,0.5)` of `Yes VIP` · `pos (−34.176,0)` ⇒ **右边缘贴着 `Yes VIP` 的左边缘**（= `VIP Status` 的左边）
        /// ⇒ 绝对 **819.54,73.31 → 887.89,141.66**。</summary>
        public static readonly PxRect VipIcon = new PxRect(819.54f, 73.31f, 887.89f, 141.66f);
        /// <summary>`VIP Timer`（221.56 × 48.889 · 锚 `(1,0.5)` of `Yes VIP` · `pos (+6.8999,0)`）
        /// ⇒ 位置**随 `Yes VIP` 的宽度走**，而那颗字的宽在原版是 `ContentSizeFitter(h = PreferredSize)` 量出来的
        /// ⇒ 我们**先建字、量渲染宽、再摆**（与 `BuildTimeCounter` 那条同一口径）。</summary>
        public const float VipTimerGap = 6.9f, VipTimerW = 221.56f, VipTimerH = 48.889f;
        /// <summary>非 VIP 遮罩：整块 = `Packs Scroll View` 同矩形（`No VIP` 与 `Block Input` 都是 `a(0,0)-(1,1)`）。</summary>
        public static readonly PxRect VipBlock = ScrollView;
        /// <summary>`Shadow drop`：`a(0,0)-(1,1)` ＋ `sizeDelta (−716.733,−798.19)` ⇒ 居中 873.51 × 154.19；
        /// 绝对 = **688.12,526.72 → 1561.63,680.90**（图 `40k_Smooth shadow background` 102² · 九宫 50,50,50,50 ·
        /// `m_Type = 1 Sliced` · 色 `(0,0,0,0.7647)` · `m_PixelsPerUnitMultiplier = 1`）。</summary>
        public static readonly PxRect VipShadow = new PxRect(688.12f, 526.72f, 1561.63f, 680.90f);
        /// <summary>遮罩上那行字（原版 `Packs Scroll View/No VIP/No VIP`：锚与轴心都居中 · `sizeDelta (0,112.064)`
        /// ⇒ **宽也是 CSF 量出来的**）。⚠️ 宽度本地量不出 ⇒ 我们给一个**以遮罩中心为中心**的 800 宽框
        /// （`Label` 出厂按框中心摆 ⇒ 视觉等价）；**高 112.064 与 y 是原版值**：547.78 → 659.84。</summary>
        public static readonly PxRect VipBlockText = new PxRect(724.88f, 547.78f, 1524.88f, 659.84f);
        /// <summary>遮罩上那颗 `tooltip trigger`（`40k_generic_bt_info` · 51.014 × 48.48 ·
        /// `m_RaycastPadding = (−15,−15,−15,−15)`（**负 = 外扩**））。绝对 = **1136.98,579.57 → 1187.99,628.05**。</summary>
        public static readonly PxRect VipTipIcon = new PxRect(1136.98f, 579.57f, 1187.99f, 628.05f);

        /// <summary>VIP 页的渲染队列：**整块压在格子里最高的那一档（`QCellInfoHit = 3031`）之上**
        /// —— 遮罩要同时挡住**视线与点击**（原版 `Block Input` 的 `m_RaycastTarget = 1`）。</summary>
        public const int QVipBlock = 3033, QVipShadow = 3034, QVipText = 3035,
                         QVipAbsorb = 3036, QVipTipHit = 3037;

        /// <summary>VIP 页的到期倒计时（`OnClose` 要退订 ⇒ 留一份；节点被 `Setup()` 重建后 Unity 的 `!= null` 判假）。</summary>
        TimerDisplay _vipTimer;
        Transform _vipStatusNode, _vipBlockNode;

        /// <summary>自检/诊断读口：VIP 状态行建了没有（= 原版 `playerIsVIPObject` 开着）。</summary>
        public bool VipStatusShown { get { return _vipStatusNode != null; } }
        /// <summary>自检/诊断读口：非 VIP 遮罩建了没有（= 原版 `playerIsNotVIPObject` 开着）。</summary>
        public bool VipBlockShown { get { return _vipBlockNode != null; } }
        /// <summary>自检/诊断读口：遮罩那颗「吃点击」的命中区（`PointerLayer` 取队列最高那颗 ⇒ 点价签不该买得到）。</summary>
        public WindowButton VipBlockHit { get; private set; }

        /// <summary>原版 `VIPCardShopTab.OnOpen` = `RefreshOffers(); CheckVIPState();`
        /// ＋ 调试钮闸（玩家面恒假 ⇒ 不建，见本节头）＋ `vipTimer.OnRunOutOfTime += VipTimerOnOnRunOutOfTime`。
        /// ⚠️ 我们这两步都落在 `Setup()` 里（`RefreshOffers` = 重建货架；`CheckVIPState` = 按 VIP 态建状态行 / 遮罩），
        /// 订阅在 <see cref="BuildVipStatus"/> 里挂 —— ⛔ 别在别处再造一套（两处写同一条规则 = 迟早不一致）。🆕 2026-10-28（`B91_W5`）：**crates 页**同 VIP —— 这一跳是原版 `BoosterShopTab.OnOpen` 的 `RefreshOffers()`；它那第二句 `TryOpenContainerArmy()` 挂在 `Setup()` 末尾。</summary>
        public override void OnOpen()
        {
            if (_page == VipPage || _page == CratesPage) Setup();
        }

        /// <summary>原版 `VIPCardShopTab.OnClose` 全文一句：`vipTimer.OnRunOutOfTime -= VipTimerOnOnRunOutOfTime;`</summary>
        public override void OnClose()
        {
            if (_vipTimer != null) _vipTimer.OnRunOutOfTime -= OnVipTimerRunOutOfTime;
        }

        /// <summary>原版 `VIPCardShopTab.ToFocus` = `RefreshOffers(); CheckVIPState();`</summary>
        public override void ToFocus()
        {
            if (_page == VipPage) Setup();
        }

        /// <summary>原版 `VIPCardShopTab.VipTimerOnOnRunOutOfTime()` = `CheckVIPState();`
        /// ⚠️ 本机的 VIP 到期时刻**机制在本地**（`InventoryManager.GetExpirationDate(Store.RequiredItem)` = 背包里那条实例的 `EphemeralEndTime`），缺的只是**输入** `RequiredItem`（那份 store SO 未导出）⇒ 取不到
        /// ⇒ 计时器恒 `--`、这个回调**今天不会被触发**（如实记；⛔ 别为了「看起来会动」编一个到期时刻）。</summary>
        void OnVipTimerRunOutOfTime()
        {
            if (_page == VipPage) Setup();
        }

        /// <summary>VIP 状态行（原版 `daily shop header/VIP Status`）—— **只有 VIP 态才建**
        /// （原版 `playerIsVIPObject.SetActive(canPurchase)`；非 VIP 时整行不画）。</summary>
        void BuildVipStatus(Transform hdr)
        {
            var status = MainMenuSubmenuWindow.Node(hdr, "VIP Status", VipStatus);
            _vipStatusNode = status;

            // `Yes VIP`：原版这颗 TMP `m_HorizontalAlignment = 1 (Left)` · `m_VerticalAlignment = 8192 (Capline)` ·
            //   `fs 36` · 宽由 `ContentSizeFitter(h = PreferredSize)` 量（prefab 里 `sizeDelta.x = 0`）。
            var yes = _win.Text(status, Loc.T(VipHeaderTerm), VipStatus.x1, VipStatus.x2,
                                VipStatus.y1, VipStatus.y2, 5, Color.white, "Yes VIP", 36f,
                                wrapPx: VipStatus.W, face: TmpFont.Face.LatinCardBody);
            float textW = 0f;
            if (yes != null)
            {
                // 原版那颗的宽 = 内容宽 ⇒ 不折行（`Text` 里那句 `SetWrapWidth` 会开 `Normal` ⇒ 这里显式还原，同 `RefreshText`）
                yes.SetWrapping(false);
                yes.SetRenderQueue(QVipText);
                MenuDraw.AlignLeft(yes, VipStatus);                       // 原版 h = Left：字从 `VIP Status` 左边起
                MenuDraw.SetVAlign(yes, Label.VAlign.Capline, VipStatus); // 原版 v = Capline (8192)
                textW = yes.WorldW * 108f;
            }

            // `VIP Icon`：右边缘贴着 `Yes VIP` 的左边缘（见 `VipIcon` 的注释）
            _win.Rect(status, "40K_icon_card_VIP", VipIcon, "VIP Icon", QVipText, null, true);

            // `VIP Timer`：锚 `(1,0.5)` of `Yes VIP` + `pos (+6.8999,0)` ⇒ 起点 = 字宽之右
            float tx1 = VipStatus.x1 + textW + VipTimerGap;
            var timerR = new PxRect(tx1, VipStatus.y1, tx1 + VipTimerW, VipStatus.y1 + VipTimerH);
            var timerNode = MainMenuSubmenuWindow.Node(status, "VIP Timer", timerR);
            {
                // `Icon`：`sizeDelta.x = 30` · `preserveAspect` ⇒ 画出来 30×30，竖向居中（同 `BuildTimeCounter` 那颗钟）
                const float side = 30f;
                var tex = _win.Art("WF_icon_clock");
                if (tex != null)
                {
                    var ic = MainMenuSubmenuWindow.Node(timerNode, "Icon",
                        new PxRect(timerR.x1, timerR.CY - side * 0.5f, timerR.x1 + side, timerR.CY + side * 0.5f));
                    var q = ImageQuad.Create(ic, tex,
                                             MainMenuSubmenuWindow.Local(ic, timerR.x1 + side * 0.5f, timerR.CY),
                                             LayoutSpace.Px(side), new Vector2(0.5f, 0.5f), "Icon");
                    if (q != null) { q.SetAspect((float)tex.width / tex.height); q.SetRenderQueue(QVipText); }
                }
            }
            // `Timer Text`：`fs 32` · h = Left · v = Capline（8192）；从图标右边 30 起
            var tt = _win.Text(timerNode, "--", timerR.x1 + 30f, timerR.x2, timerR.y1, timerR.y2, 5,
                               Color.white, "Timer Text", 32f, wrapPx: timerR.W - 30f,
                               face: TmpFont.Face.LatinCardBody);
            if (tt != null)
            {
                tt.SetWrapping(false);
                tt.SetRenderQueue(QVipText);
                MenuDraw.SetVAlign(tt, Label.VAlign.Capline, timerR);
                // 原版 `CheckVIPState`（can 那一支）：`vipTimer.Initialize(InventoryManager.GetExpirationDate(RequiredItem))`
                // 🔴 那个到期时刻**机制在本地**（`InventoryManager__GetExpirationDate_object_.c` 逐句：背包里那条实例的 `EphemeralState.EphemeralEndTime`），缺的只是输入 `RequiredItem`（来自 LiveOps 的 `DailyStoreData`，那份 store SO 本地没有）
                //    ⇒ 取不到 ⇒ 显示 `--` 并**出声一次**（`TimerDisplay` 的 `unknownReason`；⛔ 不编日期）。
                _vipTimer = TimerDisplay.Attach(tt, () => ShopData.VipExpiresAt, "", "VIP 到期时间",
                                               ShopData.VipExpiryUnknownReason, TimerDisplay.LastMinutesText());
                // 原版 `OnOpen` 那一句 `vipTimer.OnRunOutOfTime += VipTimerOnOnRunOutOfTime`
                if (_vipTimer != null) _vipTimer.OnRunOutOfTime += OnVipTimerRunOutOfTime;
            }
        }

        /// <summary>非 VIP 时那块遮罩（原版 `Packs Scroll View/No VIP`）：
        /// `Block Input`（黑 α 0.2196 整块吃点击）＋ `Shadow drop`（`40k_Smooth shadow background`）
        /// ＋ 一行提示字 ＋ 一颗信息 `tooltip trigger`。</summary>
        void BuildVipBlock(Transform sv)
        {
            var blk = MainMenuSubmenuWindow.Node(sv, "No VIP", VipBlock);
            _vipBlockNode = blk;

            // `Block Input`：prefab 里**没有 sprite**、色 `(0,0,0,0.2196)`、`m_RaycastTarget = 1`
            //   ⇒ 原版就是「一整块黑 + 吃掉这一下」（面板上没有任何 `IPointerClickHandler` ⇒ 点了什么都不发生）。
            _win.Rect(blk, null, VipBlock, "Block Input", QVipBlock, new Color(0f, 0f, 0f, 0.2196f));
            // 命中区：**档必须高于格子里所有命中区**（`QCellInfoHit = 3031`），否则遮罩只挡视线、点价签照样买得到。
            //   走 `MenuDraw.Absorb`（= 原版「面板自己吃射线、什么都不做」的等价物：`absorbOnly` 不悬停变色、
            //   点击记录里也不会被误报成「忘了绑动作」）。⚠️ 它内部算 `q = qContentMin − 1` ⇒ 传 `QVipAbsorb + 1`。
            var hit = MenuDraw.Absorb(blk, "BlockInputHit", VipBlock, QCellInfoHit, QVipAbsorb + 1);
            VipBlockHit = hit != null ? hit.GetComponent<WindowButton>() : null;

            // `Shadow drop`：`m_Type = 1 (Sliced)` · 九宫 50,50,50,50（贴图 102²）· 色 `(0,0,0,0.7647)`
            _win.Nine(blk, "40k_Smooth_shadow_background", VipShadow, new Vector4(50f, 50f, 50f, 50f),
                      QVipShadow, new Color(0f, 0f, 0f, 0.7647f), true, "Shadow drop", null, 102f, 102f);

            // 那行字：色白 · `fs 51.11` · 折行 1 · h = Left / v = Midline (4096)（判据 = prefab 字段）；
            //   框是**以遮罩中心为中心**的 800 宽（原版宽是 CSF 量的，见 `VipBlockText`），`Label` 出厂按框中心摆。
            var txt = _win.Text(blk, Loc.T(VipNotTerm), VipBlockText.x1, VipBlockText.x2,
                                VipBlockText.y1, VipBlockText.y2, 5, Color.white, "No VIP", 51.11f,
                                wrapPx: VipBlockText.W, face: TmpFont.Face.LatinCardBody);
            if (txt != null)
            {
                txt.SetRenderQueue(QVipText);
                MenuDraw.SetVAlign(txt, Label.VAlign.Midline, VipBlockText);
            }

            // `tooltip trigger`（`40k_generic_bt_info`）：触发器字段原样 —— `text` 见 `VipTooltipTerm` · `localize = 1` ·
            //   `title = ""` · `tooltipAnchor = 17 (TopCenter)` · `offset = (0, −52.06, 0)` · `registerEvents = 1` ·
            //   `preventPassingClickEventToParent = 0`（⇒ 点它什么都不做）· `m_RaycastPadding = (−15,−15,−15,−15)`。
            _win.Rect(blk, "40k_generic_bt_info", VipTipIcon, "tooltip trigger", QVipText, null, true);
            var tipHit = MenuDraw.Hit(blk, "Hit",
                                      new PxRect(VipTipIcon.x1 - 15f, VipTipIcon.y1 - 15f,
                                                 VipTipIcon.x2 + 15f, VipTipIcon.y2 + 15f),
                                      QVipTipHit, null);
            var wb = tipHit != null ? tipHit.GetComponent<WindowButton>() : null;
            if (wb != null)
            {
                // ⚠️ 用 `tipTextFunc`（不是 `tipText`）：`Loc.T` 是每次现取，静态字段会把建窗那一刻的语言冻住
                wb.tipTextFunc = () => Loc.T(VipTooltipTerm);
                wb.tipAnchor = 17;                          // 原版 `tooltipAnchor = 17`（TopCenter）
                wb.tipOffset = new Vector3(0f, -52.06f, 0f); // 原版 `offset = (0, −52.06, 0)`
            }
        }

        // ============================================================ 购买等待框（类尾 · 原版 `ShopStore.TryPurchase` 第一句）
        // ⚠️ 一律追加在**类尾**：本文件有 3 条 A 档落点钉在 `:827`（`OpenBoosterPack`），
        //    往它前面插成员会整段漂移（2026-10-25 N9 那次的原形）。
        /// <summary>原版购买开始那一刻那扇等待框的**文案键**（不是明文 —— 原版传的
        /// `localizeTexts: true`，`PopUpGameWindow.SetText` 会拿它过 `LocalizationManager.GetTranslation`）。
        /// <para>出处：① 反汇编 `0x1812540BB` → `rdx = [rip+0x307EB1E]` → VA `0x1842D2BE0`；
        /// `script.json` 的 `ScriptString` 表在该地址的值 = **`MenuShop/WaitingPopup`**；
        /// ② `out_cpp2il_dev_ilspy/Everguild.LiveOps/ShopStore.cs:518` 同句；
        /// ③ 词条真值在 `Resources/official_loc_13lang.json`：EN `Processing your purchase...` ·
        /// ZH `正在处理您的购买...`（其余 11 语也在）。⇒ **⛔ 不用编文案**。</para></summary>
        public const string WaitingPopupTerm = "MenuShop/WaitingPopup";

        /// <summary>最近一次购买弹出来的那扇等待框（自检读口）。
        /// ⚠️ 购买返回时它**已经被收掉**（`HidePopUp(true)` = 原版三条回调的共用第一句）⇒
        /// 想看「当时弹没弹、几颗钮」要读这个引用 + <see cref="LastWaitingPopupHadNoButton"/>。</summary>
        public static PopUpGameWindow LastWaitingPopup;

        /// <summary>那次等待框弹起时，**主钮是否已被藏掉**（= 原版「0 颗钮」那一档真的落地）。
        /// 判据 = `PopUpGameWindow__ConfigurePopUp(3 参)` 对每颗 `LiveButtons` 调 `SetActive(false)`；
        /// 详见 <see cref="PopUpGameWindow.PrimaryVisible"/>。</summary>
        public static bool LastWaitingPopupHadNoButton;

        // ============================================================ 🆕 2026-10-28（B91_W5）：Crates/Boosters 页（原版 `BoosterShopTab : ShopTab`）
        // ⚠️ 一律追加在类尾（本文件有十几条账按 `文件:行号` 钉着）。

        /// <summary>Crates/Boosters 页在 `ShopData.Pages` 里的下标（**第 5 页**，原版 `ShopTabType.CratesShop = 0`）。</summary>
        public const int CratesPage = 4;

        /// <summary>自检读口：本页是不是 Crates/Boosters 页。</summary>
        public bool IsCratesPageForTest { get { return _page == CratesPage; } }

        /// <summary>自检读口：最近一次「按 `OpenData.army` 自动开容器」命中的商品下标（−1 = 没命中 / 没跑）。</summary>
        public int LastAutoOpenedOffer { get; private set; } = -1;

        /// <summary>原版 `BoosterShopTab.TryOpenContainerArmy(ShopWindow.OpenData context)` 的等价物。
        /// <para>判据（第一权威 = 反汇编方法体 `decomp_full/BoosterShopTab__TryOpenContainerArmy.c`；
        /// 源码级旁证 `cpp2il/out_cpp2il_dev_ilspy/BoosterShopTab.cs:89-140`）逐句：</para>
        /// <list type="number">
        /// <item>`context.army == CardArmy.Neutral` ⇒ **直接 return**（`.c` 第一句 `if (*(int *)(param_2 + 0x10) != 0)` 的反面）；</item>
        /// <item>否则遍历本页的报价容器，取每件的**阵营**与 `context.army` **逐件比**（`.c` 里 `(**(vtable+0x1b8))(…)` 那个虚调用 = `ObtainableItem.cardArmy`）；</item>
        /// <item>命中的那一件调 `ShopOfferContainer.OpenContainer()`（**开那件报价的窗口**，不是「买 + 拆包」——
        ///   `ShopOfferContainer__OpenContainer.c` 全文 = `GetComponentReference(…, 0xc)` → `WindowsManager.OpenWindow(…)`，
        ///   **只有**那条 `CanXxx` 为假时才落 `base.OnClick()` 那条购买路）；</item>
        /// <item>随后 `*(int *)(param_2 + 0x10) = 0;` —— **把这次提示消费掉**（原文如此），⇒ 再切回本页不会重复开。</item>
        /// </list>
        /// <para>🔴 **我们这一侧的落点（如实标）**：
        /// · 「开那件报价的窗口」= <see cref="OpenBoosterInfo"/>（`Shell/BoosterInfoPopup.cs` 文件头已认定：**它就是
        ///   `ShopOfferContainer.OpenContainer` 那一跳的等价物**）；⛔ **不是** <see cref="OpenBoosterPack"/>（那是**买完拆包**，原版在服务端回执之后）。
        /// · 「每件的阵营」= **主图名反查**（`BoosterPackOpenWindow.ArmyOfArt`，全工程唯一那张 商品主图 → 阵营 的表）；
        ///   原版比的是 `ObtainableItem.cardArmy`（服务端 SO 给），本机没有那批 SO ⇒ 这是我们挑的替代，⛔ 别在这里再写第二份表。</para>
        /// <para>⚠️ **没命中时原版不出声**；我们补一条 `LogWarning`（红线：不许静默失败），且**不**清掉 `army`
        /// （原版只在命中那一支写 0 —— 照抄，⛔ 别"顺手清掉"）。</para></summary>
        void TryOpenContainerArmy()
        {
            LastAutoOpenedOffer = -1;
            var sw = _win as ShopWindow;
            var ctx = sw != null ? sw.Context : null;
            if (ctx == null) return;                                   // 原版：`Context` 为 null ⇒ 整段跳过
            if (ctx.army == ArmyIcons.Neutral) return;                  // ① 原版 `.c` 第一句：Neutral ⇒ return
            var offers = ShopData.Offers(_page);
            for (int i = 0; i < offers.Length; i++)                     // ② 逐件比阵营
            {
                string fac = BoosterPackOpenWindow.ArmyOfArt(offers[i].Art);
                if (string.IsNullOrEmpty(fac)) continue;                // 非「按阵营分」的卡包（如 generic）⇒ 不匹配
                if (ArmyIcons.Id(fac) != ctx.army) continue;
                LastAutoOpenedOffer = i;
                OpenBoosterInfo(i);                                     // ③ 原版 `ShopOfferContainer.OpenContainer()`
                Debug.Log("[Shop] crates 页：按 `OpenData.army = " + ctx.army + "`（" + (ArmyIcons.Faction(ctx.army) ?? "认不出")
                        + "）自动开了第 " + (i + 1) + " 件（`" + offers[i].Name + "`）的详情窗；随后把这次提示消费掉"
                        + "（原版 `BoosterShopTab__TryOpenContainerArmy.c` 末句 `context.army = 0`）");
                ctx.army = ArmyIcons.Neutral;                           // ④ 原版消费掉这次提示（⛔ 别挪到 Log 之前，否则日志印 0）
                return;
            }
            Debug.LogWarning("[Shop] crates 页：`OpenData.army = " + ctx.army + "`（" + (ArmyIcons.Faction(ctx.army) ?? "认不出")
                           + "）在本页 " + offers.Length + " 件里**没有对应的卡包** ⇒ 不弹窗（原版这一支也不出声）");
        }
    }
}
