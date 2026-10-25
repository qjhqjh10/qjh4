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
// ---- 🔴 数据：**清单在本地资产里，已装备的那一条在本地存档里** ----
//   清单 = `ProfileData.Titles`（462 个 `CosmeticItemTitle` SO，装配见 `Shell/ProfileData.cs` 文件头）。
//   当前装备的那一条 = `ProfileData.TitleIndex`（原版 `PlayerDataManager.get_PlayerTitle` 从服务器读；
//   我们的等价物 = `persistentDataPath/WarpforgeProfile/profile.json`，见 `ProfileData` 末尾「本地存档」）。
//   🆕 **2026-10-22（P1 · 账户界面批次）**：本页补上原版的**「预览 ≠ 装备」**两段式
//   （判据 = `TitleTab__OnItemClick.c` 只调 `ProfileItemDisplay.Initialize`（换大图预览）+
//    `ProfileItemDisplay__ToggleButton`（换钮的状态）；**装备**发生在 `TitleTab__OnSelect.c`
//    —— 点那颗钮才 `PlayerDataManager.set_PlayerTitle` + `UploadOnlineDemoData`）。
//   ⇒ 点格子 = **只预览**；点 `Select Avatar Button` = **装备**；
//      预览的那条 == 已装备的那条 ⇒ 钮文案换成 `PlayerProfile/InUse`（`In use` / `正在使用`）且**不可点**
//      （`ProfileItemDisplay__ToggleButton.c` 的三分支：相等 ⇒ InUse · 未解锁 ⇒ Locked · 否则 ⇒ Select）。
//   ⚠️ 原版那两条占位串（`Selected Item Panel/Avatar Name` 的 `'Warrior of the Raging Winds'`、
//      `Image` 的 `40k_DeckSelection_icon_FactionUM`）**只在 prefab 里、运行期会被覆盖** ⇒ 我们**不画占位**。
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
        /// 🔴 原版这条 `m_text` 是 **`Selecionar`（葡语占位串）**、**没挂 `Localize`** ⇒ 没人会替换它
        /// （2026-10-18 `W22` 按 pid 逐颗复查：`Title Tab` 这颗 = `MonoBehaviour_-1034895004042421383.json`
        /// / `MonoBehaviour_6233035307625970226.json` 两份实例，组件表里**都没有 `Localize`**
        /// —— 同 `Avatar Tab`/`Profile Cosmetic Tab` 那两颗，三颗 TMP 全是 `Selecionar`）。
        /// ✅ **2026-10-19（`W57`）全包普查复核（独立重做）**：`bundle_menus_assets_all` 里
        /// `m_text = 'Selecionar'` 的 TMP 恰好 **3 颗**，三颗的 GameObject 上 `Localize` 组件数 = **0**
        /// （与 `N37①` / `N47②` 的结论一致；⛔ 不是「只翻了一个包」）。
        /// **我们上屏 `Select`**（与同一页的 `Select your title` / `Select your avatar` 一致）—— **这是我们的选择**，
        /// 原版那串照记在上面备查（同商店页 `Atualiza em:` 那条的处理口径）。</summary>
        const float BtnTxtL = 354.25f, BtnTxtT = 676.90f, BtnTxtR = 550.74f, BtnTxtB = 730.01f;
        const float BtnTxtPx = 36f, BtnTxtAutoMin = 10f;
        /// <summary>🔴 **2026-10-18（`W22`）**：本颗的上屏词条键。
        /// <para>**prefab 侧判据是空的**（上面那条：原版这颗没有 `Localize`/`mTerm`，铁律 11 例外①）。
        /// 用**同族同一颗节点**的键：`Avatar Tab/Selected Item Panel/Select Avatar Button/Button Text`
        /// 与本页这颗**节点名逐字相同**（原版是同一份 prefab 的两种实例），而头像页那颗当前就按
        /// `Demo/AvatarMenu/SelectAvatarButton` 上屏（`Shell/AvatarTab.cs` 的 `BtnLabelTerm`）——
        /// 官方表（索引 **5906**）= `Select` / **`选择`**（13 列全非空）⇒ 两页这颗钮**同一个词**，
        /// 也是用户点名的口径（「头像页那颗已经是中文『选择』，称号页这颗还是英文」）。</para>
        /// <para>⚠️ **「原版用的是这条键」是【推断】**：本页 prefab 无 `mTerm` 可依 —— ⛔ 别写成原版证据
        /// （判据只能记到「节点同一族 + 官方表有这条同值键」）。</para></summary>
        public const string BtnLabelTerm = "Demo/AvatarMenu/SelectAvatarButton";
        /// 🔶 自己设计（非原版）：称号页钮的英文原文 `Select`（不上屏） · 原版本页 prefab 无 mTerm，只能按节点同族推断 · TODO(谁定的)｜原注：<summary>我们挑的英文原文（= 官方英文列）—— ⛔ 不上屏，只留作判据（上屏走 `Loc.T(BtnLabelTerm)`）。</summary>
        public const string BtnLabel = "Select";

        /// <summary>`Item Display Panel`：`632.79,210.69 → 1701.49,868.61`（1068.70×657.92）。</summary>
        const float DisL = 632.79f, DisT = 210.69f, DisR = 1701.49f, DisB = 868.61f;
        /// <summary>`Item Display Panel/Select Item`（那个小标题）：`654.16,147.51 → 1166.61,210.70` ·
        /// 字号 **35** · auto **[18~35]** · **折行开** · 对齐 **左**/Middle · 白。
        /// 🔴 它的 **y 在父容器之上**（父顶 210.69、它顶 147.51）—— **溢出父级**，真值，别「修正」。</summary>
        const float TtlL = 654.16f, TtlT = 147.51f, TtlR = 1166.61f, TtlB = 210.70f;
        const float TtlPx = 35f, TtlAutoMin = 18f;
        /// <summary>原版 prefab 这颗粒子 TMP 的 `m_text`（出厂英文原文）—— ⛔ 不上屏，只留作判据。</summary>
        public const string TtlLabel = "Select your title";
        /// <summary>🔴 **2026-10-18（W18）**：这颗 `Select Item` 的官方词条键 —— **判据是 prefab 自己**
        /// （不是猜的）：`Title Tab/Item Display Panel/Select Item` 那件 GameObject 上挂的
        /// `Localize` 组件 `mTerm = "PlayerProfile/SelectTitleTip"`
        /// （读法 = `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`
        ///  里 `m_text = 'Select your title'` 的那颗 MB ⇒ 取它的 `m_GameObject.m_PathID` ⇒
        ///  在同一包里找 `mTerm` 非空、`m_GameObject` 同 pid 的那条 = `MonoBehaviour_6068214396443195954.json`）。
        /// 官方两列 = `Select your title` / **`选择你的头衔`**（`Resources/official_loc_13lang.json`）。
        /// ⚠️ 改前这里直接印 `TtlLabel` 字面量 ⇒ **中文档下整页标题是英文**（用户 2026-10-18 真机点名）。</summary>
        public const string TtlLabelTerm = "PlayerProfile/SelectTitleTip";
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

        // ============================================================ 格子模板（`Title Drawer Horizontal Variant`）
        //
        // 🔴 **2026-10-22（P1）**：格子的版式**不再是「我们挑的」** —— 真模板已定位并逐层读出：
        //   根 prefab = GO pid `3945556096783145118` / RT `-2705666888391468898`（身上 `TitleDrawerHorizontal`），
        //   表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -2705666888391468898 --depth 8 --md`。
        //   · `Content` 设计框 **1028×370**，`UIScaleToFit` 按 `min(父宽/1028, 父高/370)` 缩放（反汇编 `Fit()`，
        //     `fitInParent=1` ⇒ 目标 = 本格 325.9×130）⇒ **视觉 325.9×117.3，垂直居中**。
        //   · `Background` = **`40k_Profile_display_title`**（322×116 石板 + 金边），锚点 (0,0)-(1,1) ⇒ 铺满 Content。
        //   · `Title` = 称号名（TMP `Asar-Regular Title Reward` 材质 ⇒ **字色黑**、`auto[15~90]`、基准 36、折行、Center/Middle）。
        //   · `Image` + `Image Top` = **阵营徽记**（两层叠印，见下）。
        //   · `Army`（阵营名 TMP）出厂 **`m_IsActive = 0`** 且 `ItemDrawer.Draw` 只 `set_text`、**从不 SetActive`
        //     ⇒ 原版**不画** ⇒ 我们**不建**（参考图里那格也只有名字和徽记，没有阵营名）。
        //   · `Lock`：原版 `ToggleLock` 按「有没有解锁」切；本项目**全解锁**（用户边界）⇒ 不建。
        const float TplContentW = 1028f, TplContentH = 370f;
        /// <summary>石板底（原版 `Background`）。</summary>
        public const string ArtPlate = "40k_Profile_display_title";
        /// <summary>`Title` 在 Content 里的锚点（比例）：`a=(0.05,0.171297)-(0.95,0.826)`、`sizeDelta=(0,-9.5e-7)`。</summary>
        static readonly Vector2 TplTtlA = new Vector2(0.05f, 0.171297f), TplTtlB = new Vector2(0.95f, 0.826f);
        /// <summary>字色：**黑**（原版那颗 TMP 的 `m_sharedMaterial` = pid `1930305694072827706`
        /// ⇒ `bundle_fonts_assets_all/Material/Material_1930305694072827706.json`，`m_Name = Asar-Regular Title Reward`、
        /// `_FaceColor = (0,0,0,1)`、`_UnderlayColor = (0.988,0.988,0.988,0.639)`（浅色衬底 = 参考图上那圈浮印）。
        /// ⚠️ prefab 里 `m_fontColor` 是白 —— 那是**顶点色乘数**，材质 face 是黑 ⇒ 实测上屏就是黑。</summary>
        static readonly Color TitleCellColor = new Color(0f, 0f, 0f, 1f);
        /// <summary>字号：`m_fontSize 90` / `auto[15~90]` / 基准 36（**都是 Content 设计单位**）
        /// ⇒ 落到本格要乘缩放 `s = min(325.9/1028, 130/370)`。</summary>
        const float TtlPxDesign = 90f, TtlAutoMinDesign = 15f, TtlBaseDesign = 36f;
        /// <summary>`Image`（阵营徽记）锚点：`a=(0.765251,0.046)-(0.976665,0.617596)`；两层色见下。</summary>
        static readonly Vector2 FacA = new Vector2(0.765251f, 0.046f), FacB = new Vector2(0.976665f, 0.617596f);
        static readonly Color FacTint = new Color(0.519f, 0.519f, 0.519f, 0.282f);
        static readonly Color FacTopTint = new Color(1f, 1f, 1f, 0.416f);
        /// <summary>`highlight`（选中态）：`Highlight Rounded Square`（九宫 73,75,73,75）· 色 (0.981,0.589,0.31,1)。
        /// 模板里它比 Content **大一圈**：Content 视觉 1920×691 时 highlight 是 2028.68×775.39
        /// ⇒ 每边 +54.7/+54.0（x）、+42.9/+41.4（y）；折算到本格（325.9×117.3，比例 0.16974）
        /// = **每边 +9.2 / +7.2**（⚠️ 别直接抄 54/42 —— 那是 1920 帧的数，不是格子帧）。</summary>
        public const string ArtHighlight = "Highlight_Rounded_Square";
        static readonly Color HlTint = new Color(0.981f, 0.589f, 0.31f, 1f);
        static readonly Vector4 HlBorder = new Vector4(73f, 75f, 73f, 75f);
        const float HlPadX = 9.2f, HlPadY = 7.2f;
        /// <summary>「正在使用」词条（原版 `ProfileItemDisplay.ToggleButton` 里那个分支的官方键 ——
        /// 官方表 `PlayerProfile/InUse` = `In use` / `正在使用`；参考图两颗钮都印这个）。</summary>
        public const string InUseTerm = "PlayerProfile/InUse";

        MenuScroll _scroll;
        Label _nameLabel;
        ImageQuad _icon;
        Transform _grid;
        Label _btnLabel;
        WindowButton _btnHit;
        Transform _selPanel;
        readonly System.Collections.Generic.List<GameObject> _cellHl = new System.Collections.Generic.List<GameObject>();
        readonly System.Collections.Generic.List<int> _cellIdx = new System.Collections.Generic.List<int>();
        /// <summary>🆕 2026-10-23（P4）：上一次为「预览没有阵营图」出声的那条下标（同一条**不重复刷日志**）。</summary>
        int _noIconLogged = -2;

        /// <summary>🆕 2026-10-22（P1）：**预览**的那一条（点格子只改它；`Select` 钮才写 `ProfileData.TitleIndex`）。
        /// 原版 = `TitleTab.currentItem`（`OnItemClick` 写）；`OnOpen` 用已装备的那条重设
        /// （`TitleTab__Initialize.c` 的 `OnItemClick(PlayerDataManager.PlayerTitle)` 那一路）。</summary>
        public int PreviewIndex = -1;
        /// <summary>已装备的那一条（= `ProfileData.TitleIndex`，全工程唯一一份）。</summary>
        public int Equipped { get { return ProfileData.TitleIndex; } }

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
            // 🆕 2026-10-22（P1）：**先**定预览项，再建「当前预览」那块（原版 `OnOpen` 也是先 `OnItemClick(已装备那条)`）
            PreviewIndex = ProfileData.TitleIndex;
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
            // 🔴 **2026-10-18（A840 · S1）**：补一句 `CaptureNow()` —— 把**刚写进这个节点的那个矩形**记成基准
            //   （= 框的中心那一帧，口径 → `ViewportClip` 文件头 §①）。⛔ 少了它，这个视口**逐位回落到旧写法**
            //   （实时反推 + `LiveDerivations`）：本页整块面板被挪过之后，框与被比矩形就**不在同一帧**。
            //   ⚠️ **必须在这里调**（`AddComponent` 之后、`BuildRows` 之前）：`MenuDraw.ApplyPxRect` 写矩形时
            //   组件还不存在（那一句的穿透写在 `ApplyPxRect` 尾）—— 同 `ViewportClip.Hang` 里那句
            //   `vc.SetBaseRect(r)` 的位置理由（`CardPresentation/Shell/ViewportClip.cs`）。⛔ 别改成 `OnEnable` 里自动抓（文件头 `CardPresentation/Shell/ViewportClip.cs` 明令）。
            vc.CaptureNow();
            _scroll = NewScroll(vp, GridW, 0f, true, 50f);      // 纵向；内容高度建完再算；原版 `Title Tab/Item Display Panel/Scroll Rect` = 50（D_N29）
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
            // 🔴 **A1024③（2026-10-17）换面**：本页只有**选中项那颗** `Avatar Name` 是 `Asar-Regular SDF`
            //    （判据 = `menu_dump … "Player Profile Window"` 的 `Title Tab ▸ Selected Item Panel ▸ Avatar Name`，
            //    样本 `'Warrior of the Raging Winds'`）；`Select Item` / `Button Text` / 格子里的 `Name` 都是 **Pragati**。
            Text(disp, Loc.T(TtlLabelTerm), new PxRect(TtlL, TtlT, TtlR, TtlB), Color.white, "Select Item",
                 TtlPx, 3, autoFit: true, autoMinPx: TtlAutoMin, alignLeft: true, wrap: true,
                 autoMaxPx: 35f, basePx: 26.1f, face: TmpFont.Face.LatinCardBody);

            // ---- ② `Selected Item Panel`：当前预览的那条称号（阵营徽记 + 名 + 选择钮）----
            var sel = Node("Selected Item Panel", new PxRect(SelL, SelT, SelR, SelB));
            _selPanel = sel;
            // 🔴 **2026-10-22（P1）就地订正（铁律 5）**：原来这里写「`Image` 那一层**不建** —— 我们没有称号数据」。
            //   **判据早就有了**：原版 `ProfileItemDisplay__Initialize.c` 第一段就是
            //   `image.sprite = ArmyUtilities.GetArmyIcon(item.cardArmy)`（称号**自带阵营**，不是逐条图标）——
            //   参考图上那块大号盾徽就是它。`Item.Army` 在本地齐全 ⇒ 照 `Core/ArmyIcons.cs` 的 13 阵营表画。
            //   🔴 **2026-10-23（P4）就地订正（铁律 5）**：那句「取不到图时**不建**」**改成「建、但 `SetActive(false)`」** ——
            //   判据 = `ProfileItemDisplay__Initialize.c`：`image` 是 **prefab 里那颗常驻件**，`Initialize` 只做
            //   `set_sprite` + `Behaviour.set_enabled(item != null)`，**从不销毁它**。而 `ProfilePage.Rect(art: null)`
            //   走的是 `CardArt.Solid()`（1×1 白）那条纯色路 ⇒ **不关掉就是面板正中一大块白**（旧注释担心的正是这个）。
            //   ⇒ 节点永远建（结构稳定、预览换到有阵营的条目时**直接换图**），取不到图就关掉。
            //   ⚠️ 原版 prefab 这颗 `Image` 是 `Simple` + **`m_PreserveAspect = 0`**（dump 那一行末尾是「否」）⇒ `keepAspect: false`。
            string previewIcon = PreviewArmyIcon();
            _icon = Rect(sel, previewIcon, new PxRect(IconL, IconT, IconR, IconB), "Image", 4, null, false);
            if (_icon != null) _icon.gameObject.SetActive(!string.IsNullOrEmpty(previewIcon));
            // 🔴 **A406**：`Title Tab/Selected Item Panel/Avatar Name` 原版
            //   `auto[15.0~**50.0**] 基准=36.0` ⇒ 上限 **50**（= 本文件早就写好的 `NameAutoMax` ——
            //   **它此前是死常量**，本件才第一次接上）· base **36.0**（同一条 dump）。
            _nameLabel = Text(sel, "", new PxRect(NameL, NameT, NameR, NameB), NameColor, "Avatar Name",
                              NamePx, 5, autoFit: true, autoMinPx: NameAutoMin, alignLeft: false, wrap: true,
                              autoMaxPx: NameAutoMax, basePx: 36f, face: TmpFont.Face.LatinUi);

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
            // 🔴 **2026-10-18（`W22`）**：文案**走官方词条**（`Demo/AvatarMenu/SelectAvatarButton`
            //   = `Select` / `选择`，与头像页那颗同键 —— 见 `BtnLabelTerm` 那段）。改前写死 `BtnLabel` 英文
            //   ⇒ **中文档下这颗仍是英文**（用户 2026-10-18 第六轮真机截图点名）。⛔ 别再直接印常量。
            var lbBtn = Text(btn, ButtonLabel(), new PxRect(BtnTxtL, BtnTxtT, BtnTxtR, BtnTxtB), Color.white, "Button Text",
                 BtnTxtPx, 7, autoFit: true, autoMinPx: BtnTxtAutoMin, alignLeft: false, wrap: false,
                 autoMaxPx: 36f, basePx: 12f, face: TmpFont.Face.LatinCardBody);
            _btnLabel = lbBtn;
            // 🆕 **2026-10-16（A712 阶段 2）**：纵向那一半 —— 原版
            //   `Title Tab/Selected Item Panel/Select Avatar Button/Button Text`（`'Selecionar'`）
            //   = `对齐=Center/**Capline**`（判据 = 上面 :176-179 那条 dump 原文；`--depth 25 --md` 同报
            //   `字号=36.0 auto[10.0~36.0] 对齐=Center/Capline 折行=0`）。
            MenuDraw.SetVAlign(lbBtn, Label.VAlign.Capline, new PxRect(BtnTxtL, BtnTxtT, BtnTxtR, BtnTxtB));
            // 🆕 A17：原版 `Title Tab>Selected Item Panel>Select Avatar Button` 是 SpriteSwap（普查 §块 5 第 8 行）
            var hit = Hit(sel, "SelectHit", new PxRect(BtnL, BtnT, BtnR, BtnB), 8, OnSelectClicked, selQ, ArtButton,
                clickCue: WindowSounds.SelectAvatarCue);
            _btnHit = hit != null ? hit.GetComponent<WindowButton>() : null;

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

            _cellHl.Clear(); _cellIdx.Clear();
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x, y; CellXY(col, row, out x, out y);
                var r = _scroll.Shift(new PxRect(x, y, x + CellW, y + CellH));
                if (!_scroll.Intersects(r)) continue;      // 视口外的**整格不建**（顺带它的点击区也不存在）
                var cell = Node(_grid, "TitleDrawer_" + i, r);

                // ---- `Content`：`UIScaleToFit` 的等价物 ----
                // 反汇编 `Fit()`：`ratio = min(目标框.w / 1028, 目标框.h / 370)`，`fitInParent=1` ⇒ 目标 = 本格
                // ⇒ 视觉 **325.9×117.3，垂直居中**（格子 130 高，上下各留 6.35）。
                float s = Mathf.Min(CellW / TplContentW, CellH / TplContentH);
                float cw = TplContentW * s, ch = TplContentH * s;
                var cr = new PxRect(r.CX - cw * 0.5f, r.CY - ch * 0.5f, r.CX + cw * 0.5f, r.CY + ch * 0.5f);
                var content = Node(cell, "Content", cr);

                // `highlight`：模板第一颗子件（**画在石板下面**），比 Content 每边大一圈 —— 选中（= 预览的那条）才亮
                var hlGo = Nine(content, ArtHighlight,
                                new PxRect(cr.x1 - HlPadX, cr.y1 - HlPadY, cr.x2 + HlPadX, cr.y2 + HlPadY),
                                HlBorder, "highlight", 1, HlTint);
                if (hlGo != null) hlGo.SetActive(i == PreviewIndex);
                _cellHl.Add(hlGo);
                _cellIdx.Add(i);

                // `Background`：石板（`40k_Profile_display_title`，锚点 (0,0)-(1,1) ⇒ 铺满 Content）
                Rect(content, ArtPlate, cr, "Background", 2, null, true);

                // `Image` + `Image Top`：**阵营徽记**（= `ArmyUtilities.GetArmyIcon(item.cardArmy)`，两层叠印）。
                // 结构（判据 = `工具/menu_dump.py bundle_menus_assets_all --rt -2705666888391468898 --depth 8 --md`）：
                //   模板里**每一格都建这两颗**，且 `Image Top` 是 **`Image` 的孩子**（锚点 (0,0)-(1,1)、铺满父件）
                //   ⇒ 树是 `Content > Image > Image Top`。
                // 🔴 **2026-10-23（P4）就地订正（铁律 5）**：上一位（P2，同日）把「无图那一档」判成
                //   **uGUI 的纯色 quad**（一层淡灰 `FacTint` + 一层淡白 `FacTopTint`），理由链是
                //   `Image.activeSprite == null ⇒ Graphic.OnPopulateMesh`。**那条链漏了一环 —— 运行期调用点自己关掉了**：
                //   判据 = 反编译 `d:/2/tools/decomp_full/TitleDrawer__Draw.c`：
                //     `image.sprite = ArmyUtilities.GetArmyIcon(item.cardArmy);`
                //     `imageTop.sprite = image.sprite;`
                //     `if (image.sprite == null) { image.gameObject.SetActive(false); imageTop.gameObject.SetActive(false); }`
                //   ⇒ **`Neutral(0)`（前 19 条 `All Generic`）那两格是 `SetActive(false)`**，原版**什么都不画**。
                //   而照 P2 那条写出来的实际观感 = 每格右缘**一块白方块**（两层半透明白叠在浅色石板上），
                //   用户 2026-10-23 对着参考图点名了这个（参考图上那一处是**深色阵营小圆章**或干脆没有）。
                //   ⛔ **不是删掉这两颗**：模板结构要保留（结构断言按 `Content > Image > Image Top` 查），
                //   只是 `SetActive(false)` —— 同族先例 `Shell/DailyData.cs:323`（`GetArmyIcon` 取不到 ⇒ 整格隐藏）。
                string facArt = ArmyIcons.IconName(items[i].Army);
                bool hasFac = !string.IsNullOrEmpty(facArt);
                var fr = UguiRect.Child(cr, FacA, FacB, UguiRect.P50c, Vector2.zero, Vector2.zero);
                var facBase = Rect(content, hasFac ? facArt : null, fr, "Image", 3, FacTint, hasFac);
                if (facBase != null)
                {
                    var facTop = Rect(facBase.transform, hasFac ? facArt : null, fr, "Image Top", 4, FacTopTint, hasFac);
                    // ⚠️ 显式给两态（原版只在 null 那一支 `SetActive(false)`、没有回 true 的那一支 ——
                    //    因为它每格都是新 `Instantiate` 的；我们这里同样是**新建**，显式写清更稳）。
                    facBase.gameObject.SetActive(hasFac);
                    if (facTop != null) facTop.gameObject.SetActive(hasFac);
                }

                // `Title`：称号名（黑字 · `Center/Middle` · 折行 · auto[15~90]/基准 36，全按 Content 设计单位 × s）
                var tr = UguiRect.Child(cr, TplTtlA, TplTtlB, UguiRect.P50c, Vector2.zero, Vector2.zero);
                var lbCell = Text(content, items[i].Name, tr, TitleCellColor, "Title", TtlPxDesign * s, 3,
                                  autoFit: true, autoMinPx: TtlAutoMinDesign * s, alignLeft: false, wrap: true,
                                  autoMaxPx: TtlPxDesign * s, basePx: TtlBaseDesign * s, face: TmpFont.Face.LatinUi);
                MenuDraw.SetVAlign(lbCell, Label.VAlign.Midline, tr);

                int captured = i;
                // 点格子 = **只预览**（原版 `TitleTab.OnItemClick` → `ProfileItemDisplay.Initialize` + `ToggleButton`，
                // **不**写 `PlayerDataManager.PlayerTitle`；装备在 `SelectHit` 那颗钮上）。
                Hit(cell, "Hit", r, 3, () => Select(captured), clickCue: WindowSounds.SelectAvatarCue);
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
        /// （旧选 `ToggleHighlight(false)` + 新选 `true`，见普查文件 §A-补 1 第 3 条）。
        /// 🔴 **2026-10-17（`T20` / `A2118`）**：选中结果写进 `ProfileData.TitleIndex`（**全工程唯一一份**）——
        ///   原来只写本页自己的字段（`Selected`），关掉档案窗就丢，**战斗 HUD 名牌读不到**。
        ///   ⚠️ 原版这一步是 `PlayerDataManager.set_PlayerTitle` + 上传服务器；我们**没有存档**
        ///   ⇒ 只在本次会话里有效（同 `ProfileData.AvatarIndex` 那条边界）。</summary>
        public void Select(int index)
        {
            // 越界**不改**（预览值也得守住，别让它变成脏值）
            if (index < 0 || index >= Titles.Count) { Debug.Log("[Profile] 称号预览越界：" + index); return; }
            PreviewIndex = index;
            Refresh();
            RefreshGridHighlight();
        }

        /// <summary>装备当前预览的那条 —— 原版 `TitleTab.OnSelect`（`ProfileItemDisplay.GetItem` →
        /// `PlayerDataManager.set_PlayerTitle` + `UploadOnlineDemoData`）。
        /// 我们的等价物 = `ProfileData.TitleIndex`（全工程唯一一份，**会落盘**）。</summary>
        public void ConfirmSelect()
        {
            if (PreviewIndex < 0 || PreviewIndex >= Titles.Count)
            {
                Debug.Log("[Profile] `Select`（称号页）：**还没预览任何一条称号** —— 先在右边格子里点一个");
                return;
            }
            ProfileData.TitleIndex = PreviewIndex;
            Refresh();
            RefreshGridHighlight();
            Debug.Log("[Profile] 已装备称号「" + Titles[PreviewIndex].Name + "」（# " + PreviewIndex
                    + "）—— 写进 `ProfileData.TitleIndex`（战斗 HUD 名牌读它），并落盘到 "
                    + "`persistentDataPath/WarpforgeProfile/profile.json`");
        }

        /// <summary>当前**预览**的下标（**−1 = 没预览**）。旧的 `Selected` 名字保留（外部只读它）；
        /// ⚠️ 语义已从「已装备」改成「预览」—— 已装备那条读 <see cref="Equipped"/> / `ProfileData.TitleIndex`。</summary>
        public int Selected
        {
            get { return PreviewIndex; }
            set { PreviewIndex = value; }
        }

        /// <summary>每次切到本页：预览回到**已装备的那条**（原版 `OnOpen` → `OnItemClick(PlayerTitle)`）。</summary>
        public override void OnOpen()
        {
            PreviewIndex = ProfileData.TitleIndex;
            Refresh();
            RefreshGridHighlight();
        }

        /// <summary>把「当前预览的称号」刷到 `Selected Item Panel` + 那颗钮。出处：原版
        /// `ProfileItemDisplay.Initialize`（图 + 名）+ `ProfileItemDisplay.ToggleButton`（钮文案/可点性）。
        /// ⚠️ 图标 = **称号自带的阵营徽记**（`ArmyUtilities.GetArmyIcon(item.cardArmy)`；逐条图标在原版里就是空的）。</summary>
        public void Refresh()
        {
            bool has = PreviewIndex >= 0 && PreviewIndex < Titles.Count;
            if (_nameLabel != null) _nameLabel.SetText(has ? Titles[PreviewIndex].Name : "");
            string art = has ? PreviewArmyIcon() : null;
            var tex = string.IsNullOrEmpty(art) ? null : Art(art);
            // 兜底：正常 `Build` 已经把节点建好了（2026-10-23 P4 起**永远建**）；这里只在它意外为空时补一颗
            if (_icon == null && _selPanel != null)
                _icon = Rect(_selPanel, art, new PxRect(IconL, IconT, IconR, IconB), "Image", 4, null, false);
            if (_icon != null)
            {
                if (tex != null) { _icon.SetTexture(tex); _icon.gameObject.SetActive(true); }
                else _icon.gameObject.SetActive(false);
            }
            // 🔴 **2026-10-23（P4）：不许静默** —— 预览那条没有盾徽时**说一句**（用户 2026-10-23 点名「头衔那里预览的是
            //    该头像的阵营图标，现在是否有？」）。两条成因分开说：`Army = 0 (Neutral)` 是**原版就没有这张图**
            //    （`ArmyIconsSO` 里只有 13 条、没有 Neutral），图名取不到 / 贴图缺文件是**我们的问题**。
            if (has && tex == null && _noIconLogged != PreviewIndex)
            {
                _noIconLogged = PreviewIndex;
                if (string.IsNullOrEmpty(art))
                    Debug.Log("[Profile] 称号「" + Titles[PreviewIndex].Name + "」的 `Army` = " + Titles[PreviewIndex].Army
                            + "（`0` = Neutral）—— 原版 `ArmyIconsSO` 里**没有**这一条，`ArmyUtilities.GetArmyIcon` 返回 null；"
                            + "原版对 null 的处置是 **`SetActive(false)`**（判据 `d:/2/tools/decomp_full/TitleDrawer__Draw.c`）"
                            + " ⇒ 预览盾徽同样关掉，只留橙色的称号名。");
                else
                    Debug.LogWarning("[Profile] 称号「" + Titles[PreviewIndex].Name + "」的阵营图 `" + art
                                   + "` **取不到**（`MissingArt` 会汇总）⇒ 预览盾徽先关掉（⛔ 这不是原版的空态）");
            }
            // 钮：预览 == 已装备 ⇒ `PlayerProfile/InUse`（In use / 正在使用）**且不可点**；
            //     否则 ⇒ `Demo/AvatarMenu/SelectAvatarButton`（Select / 选择）、可点。
            //     判据 = `ProfileItemDisplay__ToggleButton.c` 的三分支（相等 / 未解锁 / 其他 ⇒ InUse / Locked / Select）。
            if (_btnLabel != null) _btnLabel.SetText(ButtonLabel());
            if (_btnHit != null) _btnHit.Interactable = has && PreviewIndex != ProfileData.TitleIndex;
        }

        /// <summary>当前预览那条的**阵营徽记**图名（`Army = 0` / 认不出 ⇒ `null`）。
        /// = 原版 `ArmyUtilities.GetArmyIcon(item.cardArmy)`；映射表唯一一份在 `Core/ArmyIcons.cs`。</summary>
        string PreviewArmyIcon()
        {
            return PreviewIndex >= 0 && PreviewIndex < Titles.Count ? ArmyIcons.IconName(Titles[PreviewIndex].Army) : null;
        }

        /// <summary>那颗钮当前该印什么（自检也读它）。</summary>
        public string ButtonLabel()
        {
            bool has = PreviewIndex >= 0 && PreviewIndex < Titles.Count;
            if (has && PreviewIndex == ProfileData.TitleIndex)
            {
                string v = Loc.T(InUseTerm);
                // 词条取不到 ⇒ 回官方英文原文（⛔ 不许把键名印上屏）
                return string.IsNullOrEmpty(v) || v == InUseTerm ? "In use" : v;
            }
            string sel = Loc.T(BtnLabelTerm);
            return string.IsNullOrEmpty(sel) || sel == BtnLabelTerm ? BtnLabel : sel;
        }

        void RefreshGridHighlight()
        {
            for (int k = 0; k < _cellHl.Count; k++)
                if (_cellHl[k] != null)
                    _cellHl[k].SetActive(k < _cellIdx.Count && _cellIdx[k] == PreviewIndex);
        }

        void OnSelectClicked()
        {
            ConfirmSelect();
        }
    }
}
