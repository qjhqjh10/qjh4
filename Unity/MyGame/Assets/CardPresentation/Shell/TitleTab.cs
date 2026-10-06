// TitleTab.cs — 玩家档案窗第 3 页：`Title Tab`（原版类名 `TitleTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Avatar与Title页.md` —— §A2 是**层 × 参数**表（工具逐行），
// §A-补 1/2/3 是工具给不了的那部分（`activeSelf` 条件 / 图参数 / 格子怎么排）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt 6261725956636310066 --depth 8 --md`
//
// ---- 🔴 这一页的反编译判据（都是「看类名才判得出、按名字猜会判反」的）----
// ① **`Toggle borde` 是个死节点**：出厂 `m_IsActive = F`，**全包没有任何 MonoBehaviour 字段指向它**、
//    也没有 Animation，`TitleTab` 的 8 个方法体里也没碰过它 ⇒ **不建**（照「出厂 INACT 的不建」这条纪律）。
//    ⚠️ 同一个名字在 `Avatar Tab` 里**是活的**（`AvatarTab.toggleAvatarBorderIcon`）—— 别因为名字一样就当成一份。
// ② `Selected Item Panel` 挂 **`ProfileItemDisplay`**（字段 `image` / `selectButton` / `text`）——
//    它是「当前选中的称号」那块；`image` 喂的是**称号自带的阵营图标**，`text` 喂称号名。
// ③ 格子是 **`GridLayoutGroup`** 排的（`cellSize 325.9×130` · `spacing (15,50)` · `padding L7 T40`），
//    **预制体里 0 个实例**（`Item Drawer` 的 `m_Children` 为空、`sizeDelta.y = 0.00`）——
//    运行期 `Instantiate` 出来的（`TitleTab.Initialize:195` 的 `ItemDrawer.Draw`）。
//
// ---- 🔴 数据：**没有数据源**（原版在服务器）----
//   称号来自 `PlayerDataManager.get_PlayerTitle`（`TitleTab__Initialize.c:68`），本地没有 ⇒
//   按用户口径「**有什么复刻什么，具体的数据和排名这些可以空着**」⇒ **画空态、不编数字**。
//   ⚠️ 原版预制体里那两个**占位值**记在这里备查（**不画**，免得看着像真数据）：
//     · `Selected Item Panel/Avatar Name` 的 `m_text` = `'Warrior of the Raging Winds'`（字号 43.35 · auto[15~50] · 色 (1,0.693,0.00784,1)）
//     · `Selected Item Panel/Image` 的 sprite = `40k_DeckSelection_icon_FactionUM`（Ultramarines 阵营图 · 221.21×221.21）
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Title Tab`（`TitleTab`）。**出厂打开的就是这一页**（正本 §2·1）。</summary>
    public class TitleTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileTitle; } }
        protected override int PageIndex { get { return 2; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.WideL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 真值（绝对画布像素 · 左上原点）
        // 出处：本文件头的普查文件 §A2（表里逐行都有；下面只写「施工要用的那几列」）。
        // ⚠️ 页根**两侧各溢 16.33**（`318.37..1746.97` 而内容区是 `351.03..1746.97`）—— 真值，别对齐。

        /// <summary>`Selected Item Panel`：`318.37,292.02 → 587.32,758.89`（268.95×466.87）。
        /// 挂 `ProfileItemDisplay`（`image` / `selectButton` / `text`）。</summary>
        const float SelL = 318.37f, SelT = 292.02f, SelR = 587.32f, SelB = 758.89f;
        /// <summary>`Selected Item Panel/Avatar Name`（**这一行装的是「称号名」**，不是玩家名）：
        /// `322.54,516.41 → 587.32,631.55` · TMP 字号 **43.35** · auto **[15~50]** · **折行开** ·
        /// 色 **(1, 0.693, 0.00784)** · 对齐 `Center/Middle`。</summary>
        const float NameL = 322.54f, NameT = 516.41f, NameR = 587.32f, NameB = 631.55f;
        const float NamePx = 43.35f, NameAutoMin = 15f, NameAutoMax = 50f;
        static readonly Color NameColor = new Color(1f, 0.693f, 0.00784f, 1f);
        /// <summary>`Selected Item Panel/Image`（称号自带的阵营图标）：`344.32,295.19 → 565.54,516.41`
        /// （221.21²，`Simple`，无九宫格）。</summary>
        const float IconL = 344.32f, IconT = 295.19f, IconR = 565.54f, IconB = 516.41f;
        /// <summary>`Select Avatar Button`：`343.49,670.43 → 562.21,736.48`（218.73×66.05）。
        /// 图 `UI_Button_Mulligan`（410×124，**九宫格 333,96,333,96 但 `m_Type = Simple` ⇒ 九宫格不生效**，照 Simple 画）。</summary>
        const float BtnL = 343.49f, BtnT = 670.43f, BtnR = 562.21f, BtnB = 736.48f;
        public const string ArtButton = "UI_Button_Mulligan";
        /// <summary>`Select Avatar Button/Button Text`：`354.25,676.90 → 550.74,730.01` ·
        /// 字号 **36** · auto **[10~36]** · **不折行** · 对齐 `Center/Capline` · 白。
        /// 🔴 原版这条 `m_text` 是 **`Selecionar`（葡语占位串）**、**没挂 `Localize`** ⇒ 没人会替换它。
        /// **我们写 `Select`**（与同一页的 `Select your title` / `Select your avatar` 一致）—— **这是我们的选择**，
        /// 原版那串照记在上面备查（同商店页 `Atualiza em:` 那条的处理口径）。</summary>
        const float BtnTxtL = 354.25f, BtnTxtT = 676.90f, BtnTxtR = 550.74f, BtnTxtB = 730.01f;
        const float BtnTxtPx = 36f, BtnTxtAutoMin = 10f;
        public const string BtnLabel = "Select";

        /// <summary>`Item Display Panel`：`632.79,210.69 → 1701.49,868.61`（1068.70×657.92）。</summary>
        const float DisL = 632.79f, DisT = 210.69f, DisR = 1701.49f, DisB = 868.61f;
        /// <summary>`Item Display Panel/Select Item`（那个小标题）：`654.16,147.51 → 1166.61,210.70` ·
        /// 字号 **35** · auto **[18~35]** · **折行开** · 对齐 **左**/Middle · 白。
        /// 🔴 它的 **y 在父容器之上**（父顶 210.69、它顶 147.51）—— **溢出父级**，真值，别「修正」。</summary>
        const float TtlL = 654.16f, TtlT = 147.51f, TtlR = 1166.61f, TtlB = 210.70f;
        const float TtlPx = 35f, TtlAutoMin = 18f;
        public const string TtlLabel = "Select your title";
        /// <summary>`Item Display Panel/Background`：整块 `632.79,210.69 → 1701.49,868.61` ·
        /// `UI_Deck_Information_submenu_Back`（69×63）· **Sliced · 九宫 18,18,18,18** · 白。</summary>
        public const string ArtPanel = "UI_Deck_Information_submenu_Back";
        public static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        /// <summary>`Scroll Rect` 视口：`654.16,210.69 → 1680.12,855.46`（1025.95×644.76）·
        /// `ScrollRect` 纵向（`h=0 v=1`）· `mode=1 Clamped` · `inertia=1` · `elasticity 0.1` · `deceleration 0.135`。
        /// 身上那个 `Image` 是**无图 + 全透明**（`(1,1,1,0)`）⇒ 不画；`RectMask2D` 的等效物是 `Clip`。</summary>
        const float VpL = 654.16f, VpT = 210.69f, VpR = 1680.12f, VpB = 855.46f;
        /// <summary>🆕 **2026-10-04：这个 `Scroll Rect` 上 `RectMask2D.m_Softness` 的原版真值 = (0,50)**
        /// —— **纵向** 50px 渐隐带（x 是硬边）。
        /// 🔴 判据（`d:/4/_tmp_view/q1_rm2d.txt:265-266`）：
        /// `Player Profile Window/Menu Area/Tab  Area/Tab Content/Title Tab/Item Display Panel/Scroll Rect`
        /// soft = **(0,50)**（与 `Avatar Tab` 同值 —— 但那**是实读出来的巧合，不是可推的规律**：
        /// 同窗的 `Trophies Tab` / `Ranking Tab` 两处就是 (0,0)）。
        /// ⚠️ 机制与代价 → `MenuDraw.ApplySoftEdges`。</summary>
        static readonly Vector2 VpSoft = new Vector2(0f, 50f);
        /// <summary>`Item Drawer`（内容容器）：宽 **1044.65**（比视口宽 18.69 ⇒ **两侧各溢 9.35**，真值）、
        /// 预制体里**高 0.00**（0 个子节点）· `GridLayoutGroup`：`cellSize 325.9×130` ·
        /// `spacing (15,50)` · `padding L7 R0 T40 B0` · `UpperLeft` 起、水平优先 · `constraint 0 (Flexible)`。</summary>
        const float GridL = 654.16f, GridT = 210.70f, GridW = 1044.65f;
        const float CellW = 325.9f, CellH = 130f, GapX = 15f, GapY = 50f;
        const float PadL = 7f, PadT = 40f;

        MenuScroll _scroll;
        Label _nameLabel;
        ImageQuad _icon;
        Transform _grid;

        /// <summary>🆕 **2026-10-13（A465 · W-A435己）**：这一页那个**纵向**滚动区（**自检用** ——
        /// 同 `LeaderboardWindow.RowsScroll` / `BattleLogTab.RowsScroll` 那条理由：断言要能读到
        /// `ClipNode` 与 `Viewport` 两态）。⛔ 生产代码不用它。</summary>
        public MenuScroll RowsScroll { get { return _scroll; } }

        /// <summary>格子左上（**内容坐标**，未加滚动偏移）。照 `GridLayoutGroup` 的 UpperLeft + 水平优先。</summary>
        public static void CellXY(int col, int row, out float x, out float y)
        {
            x = GridL + PadL + col * (CellW + GapX);
            y = GridT + PadT + row * (CellH + GapY);
        }

        protected override void Build()
        {
            // ---- ① `Item Display Panel`：底 → 滚动内容 → 小标题（队列**从低到高**＝绘制顺序）----
            var disp = Node("Item Display Panel", new PxRect(DisL, DisT, DisR, DisB));
            Nine(disp, ArtPanel, new PxRect(DisL, DisT, DisR, DisB), PanelBorder, "Background", 0);

            var vp = new PxRect(VpL, VpT, VpR, VpB);
            var scrollNode = Node(disp, "Scroll Rect", vp);
            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：裁切状态**长在视口节点上**（= 原版那个 `RectMask2D`）。
            //    ⚠️ **本页没有 `Viewport` 节点**（与 `AvatarTab` 逐位同形）—— 视口矩形建在 `Scroll Rect`
            //    这一层上，原版树是 `Item Display Panel/Scroll Rect/Viewport/Item Drawer`。
            //    ⇒ 走 A435 迁移表 §二·A 注① 的**【低风险】路**：`ViewportClip` 直接挂在现成的
            //    `Scroll Rect` 上（零结构改动；`FindAbove` 从 `Item Drawer` 走一级就命中）。
            //    ⛔ **别顺手插一层 `Viewport`** —— 那会挪 `Item Drawer` 的父节点，是另立的账（同 `AvatarTab`）。
            //    参数：`padding = (0,0,0,0)` · `softness = VpSoft = (0,50)`（原版 `Scroll Rect` 那个 `RectMask2D` 实读）。
            var vc = scrollNode.gameObject.AddComponent<ViewportClip>();
            vc.padding = Vector4.zero;
            vc.softness = new Vector2Int((int)VpSoft.x, (int)VpSoft.y);
            _scroll = NewScroll(vp, GridW, 0f, true);      // 纵向；内容高度建完再算
            // 🔴 **2026-10-13（A465 · W-A435己）**：构建循环那一行（`BuildRows` 里
            //   `if (!_scroll.Intersects(r)) continue;`）从今天起读**同一颗节点**的状态
            //   （`MenuScroll.Intersects` 走 `ClipNode.State.RenderClip`）—— 那颗节点就是上面
            //   `scrollNode`（本页与 `AvatarTab` 逐位同形：没有 `Viewport` 层，挂在 `Scroll Rect` 上）。
            //   ⚠️ 今天两值同（节点框 = `vp`、`padding` 全 0）⇒ **逐个位不变**。
            _scroll.ClipNode = vc;
            // 🔴 **滚轮要能重画**（同 `AvatarTab` 那条：不接 `OnChanged` = 滚了什么都不动，静默失败）
            _scroll.OnChanged = RebuildRows;
            _grid = Node(scrollNode, "Item Drawer", new PxRect(GridL, GridT, GridL + GridW, GridT));

            // 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里是「`Clip = vp; ClipSoftness = VpSoft;`
            //    → `BuildRows()` → 清掉」那一对 —— **整对删掉**（裁切状态已迁到 `Scroll Rect` 节点上）。
            //    留着它 = 形参永远非空 ⇒ `Resolve` 走第 1 支 ⇒ **节点一个像素都不生效**（静默）。
            BuildRows();

            // 小标题在**面板之外、之上**（y 147.51 < 210.69）⇒ 队列给高一点，免得被面板压住
            // 🔴 **A406（2026-10-12）逐站实读**：`Title Tab/Item Display Panel/Select Item`
            //   原版 `auto[18.0~35.0] 基准=26.1` ⇒ 上限 35（= 标称）· base **26.1**。判据 =
            //   `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`。
            Text(disp, TtlLabel, new PxRect(TtlL, TtlT, TtlR, TtlB), Color.white, "Select Item",
                 TtlPx, 3, autoFit: true, autoMinPx: TtlAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 35f, basePx: 26.1f);

            // ---- ② `Selected Item Panel`：当前选中的称号（图 + 名 + 选择钮）----
            var sel = Node("Selected Item Panel", new PxRect(SelL, SelT, SelR, SelB));
            // 🔴 `Image` 那一层**不建**：它装的是「称号自带的阵营图标」，而**我们没有称号数据**。
            //    ⚠️ 别拿 `art = null` 建个纯色块顶替 —— `CardArt.Solid()` 是**不透明白块**，
            //    会变成面板正中一大块白色（2026-09-27 写这一页时差点这么干）。
            //    有数据时由 `Refresh()` 用 `Art(iconName)` 现建（矩形常量 `IconL/T/R/B` 已备好）。
            _icon = null;
            // 🔴 **A406**：`Title Tab/Selected Item Panel/Avatar Name` 原版
            //   `auto[15.0~**50.0**] 基准=36.0` ⇒ 上限 **50**（= 本文件早就写好的 `NameAutoMax` ——
            //   **它此前是死常量**，本件才第一次接上）· base **36.0**（同一条 dump）。
            _nameLabel = Text(sel, "", new PxRect(NameL, NameT, NameR, NameB), NameColor, "Avatar Name",
                              NamePx, 5, autoFit: true, autoMinPx: NameAutoMin, alignLeft: false, wrap: true,
                              autoMaxPx: NameAutoMax, basePx: 36f);

            // 钮：**图和文字都挂在钮节点【里面】**（照原版树：`Select Avatar Button > Button Text`）
            var btn = Node(sel, "Select Avatar Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            var selQ = Rect(btn, ArtButton, new PxRect(BtnL, BtnT, BtnR, BtnB), "Image", 6);
            // 🔴 **2026-10-08（A212）**：`autoFit: true` 且 **不传 `wrap`** ⇒ `ProfilePage.Text` 会把模式
            //    显式落成 **`折行=0`**（`autoFit` 不再隐含 `wrap`，见那个方法的头）。判据（现读）=
            //    `python 工具/menu_dump.py bundle_menus_assets_all "Player Profile Window" --depth 25 --md`
            //    ⇒ `Title Tab/Selected Item Panel/Select Avatar Button/Button Text`（`'Selecionar'`）
            //    `字号=36.0 auto[10.0~36.0] 对齐=Center/Capline` · **`折行=0`** —— 与本文件头那句「不折行」一致。
            // 🔴 **A406**：`Title Tab/Selected Item Panel/Select Avatar Button/Button Text`
            //   原版 `auto[10.0~36.0] 基准=12.0` ⇒ 上限 36（= 标称）· base **12.0**。
            var lbBtn = Text(btn, BtnLabel, new PxRect(BtnTxtL, BtnTxtT, BtnTxtR, BtnTxtB), Color.white, "Button Text",
                 BtnTxtPx, 7, autoFit: true, autoMinPx: BtnTxtAutoMin, alignLeft: false, wrap: false,
                 autoMaxPx: 36f, basePx: 12f);
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版
            //   `Title Tab/Selected Item Panel/Select Avatar Button/Button Text`（`'Selecionar'`）
            //   = `对齐=Center/**Capline**`（判据 = 上面 :176-179 那条 dump 原文；`--depth 25 --md` 同报
            //   `字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0`）。
            MenuDraw.SetVAlign(lbBtn, Label.VAlign.Capline, new PxRect(BtnTxtL, BtnTxtT, BtnTxtR, BtnTxtB));
            // 🆕 A17：原版 `Title Tab>Selected Item Panel>Select Avatar Button` 是 SpriteSwap（普查 §块 5 第 8 行）
            Hit(sel, "SelectHit", new PxRect(BtnL, BtnT, BtnR, BtnB), 8, OnSelectClicked, selQ, ArtButton);

            Refresh();
        }

        /// <summary>格子。**原版运行期 `Instantiate` 出来的**（预制体里 0 个实例）—— 见文件头 ③。
        /// 内容高度 = 行数 × (cellH + gapY) − gapY + padTop（`ContentSizeFitter` 的 `VerticalFit=PreferredSize`）。</summary>
        void BuildRows()
        {
            var items = Titles;
            int n = items.Count;
            int cols = Mathf.Max(1, Mathf.FloorToInt((GridW - PadL + GapX) / (CellW + GapX)));
            int rows = n == 0 ? 0 : (n + cols - 1) / cols;
            float contentH = rows == 0 ? 0f : PadT + rows * CellH + (rows - 1) * GapY;
            // 内容极值：`MenuScroll.TopAligned` 造出来的是 `[y1, y1+contentH]`，这里按真实内容高改一次
            _scroll.ContentX2 = GridT + contentH;

            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x, y; CellXY(col, row, out x, out y);
                var r = _scroll.Shift(new PxRect(x, y, x + CellW, y + CellH));
                if (!_scroll.Intersects(r)) continue;      // 视口外的**整格不建**（顺带它的点击区也不存在）
                // ⚠️ **格子的长相是我们挑的**：原版那 0 个实例说明它由 `ItemDrawer.Draw` 运行期装，
                //    模板是 `Title Drawer Horizontal Variant*` 那一族，而**哪一份没查到**
                //    （普查文件 §C5：配置是 Addressables 资产不是代码）⇒ 这里只画
                //    「一块暗底 + 居中称号名」，**不是原版的格子版式**。查到了要换掉。
                var cell = Node(_grid, "TitleDrawer_" + i, r);
                Solid(cell, r, "Plate", 1, new Color(1f, 1f, 1f, 0.055f));
                // 🔴 子件从**偏移后**的 `r` 起算（拿未偏移的 x/y = 一滚就「底板走了、字没走」，2026-09-27 修）
                // 🆕 **2026-10-08（A212）折行的判据找到了**（原来这里记的是「模板是哪一份没查到」，见下面那段
                //    「格子的长相是我们挑的」）：模板是 **`Title Drawer Horizontal Variant`** —— `bundle_menus_assets_all`
                //    里**同名实例有多份**（工具在一份 `GameObject/` 目录里就报了 18 处命中、8 个不同 pid），
                //    本行读的是工具取的**第一份**（`-6140934185811029764`，名字就是裸名那一份）：
                //    `Content > Label > Name`（出厂文本 `'TITLE'`）实测 **`折行=0`**（`字号=19.0 auto[12.0~75.0]
                //    对齐=Center/Midline`）—— 判据命令：
                //    `python 工具/menu_dump.py bundle_menus_assets_all "Title Drawer Horizontal Variant" --depth 6 --md`
                //    ⇒ 本行**不传 `wrap`**（= `autoFit` 不再隐含折行）就是原版那一档。
                //    ⚠️ **其余同名实例没逐份核**（如实说；要逐份核就 `--rt <那个 pid>` 一份份来）。
                //    ⚠️ 那只解决**折行**这一格；**版式**（底板 / 字号 30 / 居中）仍然是**我们挑的**，照旧。
                // 🔴 **A406（2026-10-12）**：同一条命令的 `Content > Label > Name` 行实读
                //   `字号=19.0 基准=**36.0** auto[**12.0~75.0**] 对齐=Center/Midline 折行=0`
                //   ⇒ 上限 **75.0**（我们原来拿 `fontPx 30` 当上限 ⇒ 短标题永远画小一档）· base **36.0**。
                //   ⚠️ **`m_fontSize 19.0` 与本行的 `fontPx 30` 不等** —— 那个 30 是**我们挑的版式值**
                //   （见上一段），本件**没动它**；自适配上/下限是绝对 px ⇒ 与标称无关。
                var cellR = new PxRect(r.x1 + 8f, r.y1 + 8f, r.x2 - 8f, r.y2 - 8f);
                var lbCell = Text(cell, items[i].Name, cellR,
                     Color.white, "Name", 30f, 2, autoFit: true, autoMinPx: 12f, alignLeft: false, wrap: false,
                     autoMaxPx: 75f, basePx: 36f);
                // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版模板
                //   `Title Drawer Horizontal Variant/Content > Label > Name` 实读
                //   `对齐=Center/**Midline**`（判据 = 上面 :220-227 那两段引的同一条 dump）。
                //   ⚠️ 本格的字号/居中版式**仍是我们挑的**（见上面那段），**只有档位**来自原版。
                MenuDraw.SetVAlign(lbCell, Label.VAlign.Midline, cellR);
                int captured = i;
                Hit(cell, "Hit", r, 3, () => Select(captured));
            }
        }

        /// <summary>滚轮改了偏移 ⇒ 重画格子（挂在 `MenuScroll.OnChanged` 上；**先清再建**，
        /// 回调会重入 —— 同 `ForgeTab.BuildRewardCells` 那条「幂等」注释）。
        /// 🔴 **2026-10-13（A435 阶段 2 · 丙）**：原来这里重设了一对 `Clip`/`ClipSoftness` —— **删掉了**
        /// （裁切状态已迁到 `Scroll Rect` 节点上，重画时自动继续生效）。</summary>
        void RebuildRows()
        {
            if (_grid == null) return;
            for (int i = _grid.childCount - 1; i >= 0; i--) DestroyNow(_grid.GetChild(i).gameObject);
            BuildRows();
        }

        // ============================================================ 数据
        //
        // 🔴 **清单来自原版资产，不是玩家存档**（判据 → `Shell/ProfileData.cs` 文件头）：
        //    462 个称号 SO 在 `素材/Warpforge原版/装饰品/定义数据/`，由 `工具/gen_profile_cosmetics.py`
        //    抽成 `Resources/profile_cosmetics.json`。
        //    ⚠️ **显示名是我们从资源名反推的**（`Title_UM_Premium_1` → `UM Premium 1`）——
        //    原版真名在远端 I2 语言表，本地只有 key（462 条的 `nameTextReference` **全是空串**）。
        public static System.Collections.Generic.List<ProfileData.Item> Titles
        { get { return ProfileData.Titles; } }

        /// <summary>选中第 index 个称号。原版 = `TitleTab.OnItemClick`
        /// （旧选 `ToggleHighlight(false)` + 新选 `true`，见普查文件 §A-补 1 第 3 条）。</summary>
        public void Select(int index)
        {
            if (index < 0 || index >= Titles.Count) { Debug.Log("[Profile] 称号选中越界：" + index); return; }
            Selected = index;
            Refresh();
        }

        /// <summary>当前选中的下标（-1 = 没选）。</summary>
        public int Selected = -1;

        /// <summary>把「当前选中的称号」刷到 `Selected Item Panel`。出处：原版 `TitleTab.Initialize`
        /// 末尾那段 `ProfileItemDisplay.Initialize(...)`。
        /// ⚠️ **图标那一层不画**：称号的 `imageReference` 在原版里**就是空的**（462 条全空，普查 §2.1）——
        /// 所以 `ProfileItemDisplay.image` 本来就没东西可喂，**不是我们没做**。</summary>
        public void Refresh()
        {
            bool has = Selected >= 0 && Selected < Titles.Count;
            if (_nameLabel != null) _nameLabel.SetText(has ? Titles[Selected].Name : "");
            if (_icon != null) _icon.gameObject.SetActive(has);
        }

        void OnSelectClicked()
        {
            if (Selected < 0)
            {
                Debug.Log("[Profile] `Select`（称号页）：**还没选中任何一个称号** —— 先在上面那张表里点一个");
                return;
            }
            Debug.Log("[Profile] 选中称号「" + Titles[Selected].Name + "」（# " + Selected + "）—— "
                    + "⚠️ **没有存档**：原版这一步会 `PlayerDataManager.set_PlayerTitle` + `UploadOnlineDemoData`，"
                    + "我们只在本局记住（见 `ProfileData.cs` 文件头）");
        }
    }
}
