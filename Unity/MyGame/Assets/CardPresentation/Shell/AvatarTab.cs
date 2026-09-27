// AvatarTab.cs — 玩家档案窗第 2 页：`Avatar Tab`（原版类名 `AvatarTab`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0927/档案窗_Avatar与Title页.md` —— §A1 是**层 × 参数**表（工具逐行），
// §A-补 1/2/3 是工具给不了的那部分（`activeSelf` 条件 / 图参数 / **格子怎么排**）。
// 表 = `python 工具/menu_dump.py bundle_menus_assets_all --rt -4150415914498688462 --depth 8 --md`
//
// ---- 🔴 这一页与 `Title Tab` 的差别（同构的那一半**共用一套画法**）----
//   · `Selected Item Panel` 里**不是空的**：装的是 `Avatar Menu Item`（大图预览 + 名字），
//     而 `Title Tab` 那份装的是 `ProfileItemDisplay`（只有图 + 字）。
//   · 多了 `Toggle borde` 这个**活**节点（`AvatarTab.toggleAvatarBorderIcon`）—— 切「显不显示头像边框」。
//     ⚠️ 同一个名字在 `Title Tab` 里是**死节点**（出厂 F、全包无引用）⇒ 别当成一份。
//   · 网格格子 = **`Avatar Item Small`（180×180）**，而 `Title Tab` 用的是 325.9×130 的横条。
//
// ---- 🔴 格子的版式**是真值**（不像 Title 那页得自己挑）----
//   预制体里 `Item Drawer` 底下有**1 个实例** `Avatar Item Small_Ref`（180×180），**它就是 `itemPrefab`**
//   —— 运行期先 `SetActive(false)` 再逐格 `Instantiate`（`AvatarTab__InitializeGameObjects.c:60/152/206`）
//   ⇒ 它的内部结构可以直接照抄（下面 `CellOffsets` 那几行）。出处：普查文件 §A-补 3。
//
// ---- 数据 ----
//   清单来自**原版资产**（469 张可选头像 SO → `Resources/profile_cosmetics.json`），**不是玩家存档**
//   （判据 → `Shell/ProfileData.cs` 文件头）。**显示名是我们从资源名反推的**。
//   ⚠️ 原版预制体里那两个**占位值**记在这里备查（**不画**）：`Avatar Name` 的 `m_text = 'TEST NAME'`。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `Avatar Tab`（`AvatarTab`）。</summary>
    public class AvatarTab : ProfilePage
    {
        public override WindowTabType Type { get { return WindowTabType.ProfileAvatar; } }
        protected override int PageIndex { get { return 1; } }
        public override PxRect PageRect
        {
            get { return new PxRect(PlayerProfileWindow.WideL, PlayerProfileWindow.RedT,
                                    PlayerProfileWindow.ContentR, PlayerProfileWindow.AreaB); }
        }

        // ============================================================ 真值（绝对画布像素 · 左上原点）
        // ⚠️ 页根**两侧各溢 16.33**（真值，正本 §2·1）。

        /// <summary>`Selected Item Panel`：`318.37,292.02 → 587.32,758.89`。</summary>
        const float SelL = 318.37f, SelT = 292.02f, SelR = 587.32f, SelB = 758.89f;
        /// <summary>`Selected Item Panel/Avatar Menu Item`：`318.37,360.15 → 585.45,633.73`。
        /// 挂 `EverguildButton + AvatarDisplay + ItemDrawerComponents`（`AvatarDisplay` 字段
        /// `avatarHolder/avatarImage/avatarName/button/highlight/...`）。</summary>
        const float MiL = 318.37f, MiT = 360.15f, MiR = 585.45f, MiB = 633.73f;
        /// <summary>`Avatar Menu Item/Image Container`：`318.37,360.15 → 585.45,573.73`（267.08×213.58）。</summary>
        const float IcL = 318.37f, IcT = 360.15f, IcR = 585.45f, IcB = 573.73f;
        /// <summary>`Image Container/Highlight`（**选中高亮**）：`318.37,355.60 → 588.79,571.03`。
        /// 图 `Player_Avatar_selected`（256×256）· `preserveAspect`。
        /// 🔴 原版**这一份（大图那份）实际不会亮** —— 两个调用点都写死 `highlight = false`
        /// （`AvatarTab__RefreshDisplays.c:47` 传 `0,0`、`AvatarTab__OnAvatarClick.c:38` 第 4 参 `0`）
        /// ⇒ 我们照实：**只给网格格子用**，大图那份恒关。</summary>
        const float HlL = 318.37f, HlT = 355.60f, HlR = 588.79f, HlB = 571.03f;
        public const string ArtHighlight = "Player_Avatar_selected";
        /// <summary>`Image Container/Border`（**头像边框**）：`318.37,381.51 → 585.45,595.09`。
        /// 图 `Player Profile Border`（256×286）· `preserveAspect`。
        /// ⚠️ **传的必须是导入后的文件名**（下划线版）—— `CardArt.MenuUi` **不做「空格→下划线」转换**，
        /// 写成精灵名 `Player Profile Border` 会**静默取不到**（2026-09-27 就这么红过一条）。
        /// 它由 `Toggle borde` 那个钮控制显隐（`AvatarTab.ToggleAvatarBorder` / `CheckEnableAvatarBorder`）。</summary>
        const float BdL = 318.37f, BdT = 381.51f, BdR = 585.45f, BdB = 595.09f;
        public const string ArtBorder = "Player_Profile_Border";
        /// <summary>`Image Container/Image`（**头像立绘那一层**）：`318.37,357.45 → 585.45,571.03`。
        /// ⚠️ 预制体里这件的 `m_Sprite = 0`（原版运行期由 `AvatarDisplay.avatarImage` 喂）。</summary>
        const float ImL = 318.37f, ImT = 357.45f, ImR = 585.45f, ImB = 571.03f;
        /// <summary>`Avatar Menu Item/Avatar Name`：`303.37,253.13 → 600.45,321.03` ·
        /// TMP 字号 **35** · auto **[15~35]** · 色 **(1, 0.693, 0.00784)**。
        /// ⚠️ 它比父件宽（303.37..600.45 vs 318.37..585.45）—— 真值。</summary>
        const float AnL = 303.37f, AnT = 253.13f, AnR = 600.45f, AnB = 321.03f;
        const float AnPx = 35f, AnAutoMin = 15f;
        static readonly Color NameColor = new Color(1f, 0.693f, 0.00784f, 1f);

        /// <summary>`Select Avatar Button`：`343.49,670.43 → 562.21,736.48` · `UI_Button_Mulligan`（Simple）。</summary>
        const float BtnL = 343.49f, BtnT = 670.43f, BtnR = 562.21f, BtnB = 736.48f;
        const float BtL = 354.25f, BtT = 677.86f, BtR = 550.74f, BtB = 729.05f;
        /// <summary>`Toggle borde`：`343.49,771.43 → 562.21,837.48`（**这一份是活的**）。
        /// ⚠️ 节点名原版就拼成 `borde`（不是 `border`）。文案 `'Toggle Border'`（36 · auto[10~36]，**有 Localize**）。</summary>
        const float TbL = 343.49f, TbT = 771.43f, TbR = 562.21f, TbB = 837.48f;
        const float TbtL = 354.25f, TbtT = 778.86f, TbtR = 550.74f, TbtB = 830.05f;
        public const string ArtButton = "UI_Button_Mulligan";
        public const string BtnLabel = "Select";          // ⚠️ 我们挑的：原版那条是葡语占位串 `Selecionar`（同 Title 页）
        public const string ToggleBorderLabel = "Toggle Border";

        /// <summary>`Item Display Panel` + 它的三件（与 `Title Tab` 逐位相同 —— 同一套壳）。</summary>
        const float DisL = 632.79f, DisT = 210.69f, DisR = 1701.49f, DisB = 868.61f;
        const float TtlL = 654.16f, TtlT = 147.51f, TtlR = 1166.61f, TtlB = 210.70f;
        const float TtlPx = 35f, TtlAutoMin = 18f;
        public const string TtlLabel = "Select your avatar";
        public const string ArtPanel = "UI_Deck_Information_submenu_Back";
        public static readonly Vector4 PanelBorder = new Vector4(18f, 18f, 18f, 18f);
        const float VpL = 654.16f, VpT = 210.69f, VpR = 1680.12f, VpB = 855.46f;
        /// <summary>`Item Drawer`（内容容器）：宽 **1044.65**（比视口宽 18.69 ⇒ 两侧各溢 9.35）。
        /// `GridLayoutGroup`：`cellSize **180×180**` · `spacing **(25,50)**` · `padding L13 T40` ·
        /// `UpperLeft` 起、水平优先。**5 列**（`floor((1044.65−13+25)÷205)`）。</summary>
        const float GridL = 654.16f, GridT = 210.70f, GridW = 1044.65f;
        const float CellW = 180f, CellH = 180f, GapX = 25f, GapY = 50f, PadL = 13f, PadT = 40f;

        // ---- 格子内部（**相对格子左上角**的偏移；出处：普查文件 §A-补 3 的 `Avatar Item Small_Ref` 那几行）----
        // 🔴 `Avatar Name` 落在**格子之外**（y 180..221.31）—— 正好插进 `GridLayoutGroup` 留的 **50px 行距**里。
        //    别把它「修正」回格子内。
        const float CImgL = 0f, CImgT = 0f, CImgR = 180f, CImgB = 142.63f;          // Image Container
        const float CHlL = 0f, CHlT = -3.53f, CHlR = 183.34f, CHlB = 144.49f;       // Highlight（**出厂 T**）
        const float CBdL = 0f, CBdT = 14.26f, CBdR = 180f, CBdB = 156.89f;          // Border
        const float CArtL = 0f, CArtT = -2.70f, CArtR = 180f, CArtB = 139.93f;      // Image（立绘）
        const float CNmL = 0f, CNmT = 180f, CNmR = 180f, CNmB = 221.31f;            // Avatar Name（**出厂 F**）
        const float CNmPx = 36f, CNmAutoMin = 12f;

        MenuScroll _scroll;
        Transform _grid;
        Label _bigName;          // 大图那份的 `Avatar Name`
        ImageQuad _bigArt, _bigHighlight;
        readonly List<ImageQuad> _cellArt = new List<ImageQuad>();
        readonly List<ImageQuad> _cellHl = new List<ImageQuad>();
        /// <summary>每个建出来的格子对应**清单里的第几条**（换选中时要按它点亮对的那一格）。</summary>
        readonly List<int> _cellIdx = new List<int>();

        /// <summary>格子左上（**内容坐标**，未加滚动偏移）。`GridLayoutGroup` 的 UpperLeft + 水平优先。</summary>
        public static void CellXY(int col, int row, out float x, out float y)
        {
            x = GridL + PadL + col * (CellW + GapX);
            y = GridT + PadT + row * (CellH + GapY);
        }

        /// <summary>一屏列数（5）—— 自检要断它。</summary>
        public static int Columns
        {
            get { return Mathf.Max(1, Mathf.FloorToInt((GridW - PadL + GapX) / (CellW + GapX))); }
        }

        protected override void Build()
        {
            // ---- ① `Item Display Panel`（与 `Title Tab` 逐位相同的那套壳）----
            var disp = Node("Item Display Panel", new PxRect(DisL, DisT, DisR, DisB));
            Nine(disp, ArtPanel, new PxRect(DisL, DisT, DisR, DisB), PanelBorder, "Background", 0);

            var vp = new PxRect(VpL, VpT, VpR, VpB);
            var scrollNode = Node(disp, "Scroll Rect", vp);
            _scroll = NewScroll(vp, GridW, 0f, true);
            _grid = Node(scrollNode, "Item Drawer", new PxRect(GridL, GridT, GridL + GridW, GridT));

            Clip = vp;
            BuildRows();
            Clip = null;

            Text(disp, TtlLabel, new PxRect(TtlL, TtlT, TtlR, TtlB), Color.white, "Select Item",
                 TtlPx, 3, autoFit: true, autoMinPx: TtlAutoMin, alignLeft: true, wrap: true);

            // ---- ② `Selected Item Panel`：当前选中的头像（大图 + 名 + 两个钮）----
            var sel = Node("Selected Item Panel", new PxRect(SelL, SelT, SelR, SelB));
            var big = Node(sel, "Avatar Menu Item", new PxRect(MiL, MiT, MiR, MiB));

            var ic = Node(big, "Image Container", new PxRect(IcL, IcT, IcR, IcB));
            // 🔴 `Highlight` 挂在 **`Image Container`** 下（不是 `Avatar Menu Item` 下）—— 照原版树。
            //    大图这份**恒关**（见常量注释）。
            _bigHighlight = Rect(ic, ArtHighlight, new PxRect(HlL, HlT, HlR, HlB), "Highlight", 4, null, true);
            if (_bigHighlight != null) _bigHighlight.gameObject.SetActive(false);
            // ⚠️ **别用 `Rect(ic, null, …)`** —— `CardArt.Solid()` 是**不透明白块**，会变成面板正中一大块白。
            //    没有图就**不建**（`CosmeticRect` 取不到时返回 null）。
            _bigArt = CosmeticRect(ic, Avatars.Count > 0 ? Avatars[Selected].Art : null,
                                   new PxRect(ImL, ImT, ImR, ImB), "Image", 4);
            Rect(ic, ArtBorder, new PxRect(BdL, BdT, BdR, BdB), "Border", 5, null, true);
            _bigName = Text(big, "", new PxRect(AnL, AnT, AnR, AnB), NameColor, "Avatar Name",
                            AnPx, 6, autoFit: true, autoMinPx: AnAutoMin);

            // 两个钮：**图和文字都挂在钮节点【里面】**（照原版树：`Select Avatar Button > Button Text`）
            var btn = Node(sel, "Select Avatar Button", new PxRect(BtnL, BtnT, BtnR, BtnB));
            Rect(btn, ArtButton, new PxRect(BtnL, BtnT, BtnR, BtnB), "Image", 7);
            Text(btn, BtnLabel, new PxRect(BtL, BtT, BtR, BtB), Color.white, "Button Text",
                 CNmPx, 7, autoFit: true, autoMinPx: 10f);
            Hit(sel, "SelectHit", new PxRect(BtnL, BtnT, BtnR, BtnB), 8, OnSelectClicked);

            var tbn = Node(sel, "Toggle borde", new PxRect(TbL, TbT, TbR, TbB));
            Rect(tbn, ArtButton, new PxRect(TbL, TbT, TbR, TbB), "Image", 7);
            Text(tbn, ToggleBorderLabel, new PxRect(TbtL, TbtT, TbtR, TbtB), Color.white, "Button Text",
                 CNmPx, 7, autoFit: true, autoMinPx: 10f);
            Hit(sel, "BordeHit", new PxRect(TbL, TbT, TbR, TbB), 8, ToggleBorder);

            Refresh();
        }

        /// <summary>格子。原版 = `Instantiate(itemPrefab, contentHolder)` 逐张生成
        /// （`AvatarTab__InitializeGameObjects.c:152`），**一屏只建看得见的那些**（视口外的整格不建）。</summary>
        void BuildRows()
        {
            var items = Avatars;
            int n = items.Count, cols = Columns;
            int rows = n == 0 ? 0 : (n + cols - 1) / cols;
            float contentH = rows == 0 ? 0f : PadT + rows * CellH + (rows - 1) * GapY;
            _scroll.ContentX2 = GridT + contentH;

            _cellArt.Clear(); _cellHl.Clear(); _cellIdx.Clear();
            for (int i = 0; i < n; i++)
            {
                int col = i % cols, row = i / cols;
                float x, y; CellXY(col, row, out x, out y);
                var r = _scroll.Shift(new PxRect(x, y, x + CellW, y + CellH));
                if (!_scroll.Intersects(r)) continue;

                var cell = Node(_grid, "Avatar Item Small_" + i, r);
                // 子件按**格子内偏移**摆（`Shift` 已经把整个格子搬到屏幕上了）
                var ic = Node(cell, "Image Container",
                              new PxRect(x + CImgL, y + CImgT, x + CImgR, y + CImgB));
                var hl = Rect(ic, ArtHighlight, new PxRect(x + CHlL, y + CHlT, x + CHlR, y + CHlB),
                              "Highlight", 1, null, true);
                if (hl != null) hl.gameObject.SetActive(i == Selected);   // 出厂 F，选中的那一格才亮
                var art = CosmeticRect(ic, items[i].Art,
                                       new PxRect(x + CArtL, y + CArtT, x + CArtR, y + CArtB), "Image", 2);
                Rect(ic, ArtBorder, new PxRect(x + CBdL, y + CBdT, x + CBdR, y + CBdB), "Border", 3, null, true);
                // `Avatar Name` 在**格子外**（y+CNmT..y+CNmB），落在行距里 —— 真值
                Text(cell, items[i].Name, new PxRect(x + CNmL, y + CNmT, x + CNmR, y + CNmB),
                     Color.white, "Avatar Name", CNmPx, 2, autoFit: true, autoMinPx: CNmAutoMin);
                int captured = i;
                Hit(cell, "Hit", r, 4, () => Select(captured));
                _cellArt.Add(art); _cellHl.Add(hl); _cellIdx.Add(i);
            }
        }

        /// <summary>这一屏建出来的格子数（自检用：469 条全建会卡，必须只建看得见的）。</summary>
        public int BuiltCells { get { return _cellArt.Count; } }

        // ============================================================ 数据
        //
        // 🔴 清单来自**原版资产**（469 张可选头像 SO），不是玩家存档 —— 判据 → `Shell/ProfileData.cs` 文件头。
        //    ⚠️ 显示名是我们从资源名反推的（`Avatar_UM_Attack Bike` → `Attack Bike`）。
        public static List<ProfileData.Item> Avatars { get { return ProfileData.Avatars; } }

        /// <summary>当前选中的下标。**默认 0 是我们挑的** —— 原版开局显示的是 `PlayerAvatarDataManager`
        /// 里**玩家自己的**那张（服务器），我们没有存档 ⇒ 挑第一张，**别当成原版行为**。</summary>
        public int Selected = 0;
        /// <summary>要不要画头像边框（`Toggle borde` 那个钮）。原版由 `PlayerAvatarDataManager` 的边框字段驱动。</summary>
        public bool BorderOn = true;

        public void Select(int index)
        {
            if (index < 0 || index >= Avatars.Count) { Debug.Log("[Profile] 头像选中越界：" + index); return; }
            Selected = index;
            Refresh();
        }

        /// <summary>切「显不显示头像边框」。原版 `AvatarTab.ToggleAvatarBorder` → `SetCurrentItem(border, cb)`。</summary>
        public void ToggleBorder()
        {
            BorderOn = !BorderOn;
            Refresh();
            Debug.Log("[Profile] 头像边框 = " + (BorderOn ? "显示" : "隐藏")
                    + "（⚠️ 没有存档：原版这一步会 `PlayerAvatarDataManager.SaveAvatarBorder`）");
        }

        /// <summary>把「当前选中的头像」刷一遍：大图那块（立绘 + 名）+ 格子上的选中高亮 + 边框显隐。
        /// 出处：原版 `AvatarTab.RefreshDisplays` → `AvatarDisplay.Initialize(...)`；
        /// 「旧选 `ToggleHighlight(false)` + 新选 `true`」那条链在 `AvatarTab__OnAvatarClick.c:25/28`。
        /// ⚠️ **不重建格子**（原版也只切 `Highlight` 的 active）—— 所以换选中**不需要重跑 `BuildRows`**。</summary>
        public void Refresh()
        {
            bool has = Selected >= 0 && Selected < Avatars.Count;
            if (_bigName != null) _bigName.SetText(has ? Avatars[Selected].Name : "");
            if (_bigArt != null)
            {
                var tex = has ? CardArt.Cosmetics(Avatars[Selected].Art) : null;
                if (tex != null) { _bigArt.SetTexture(tex); _bigArt.gameObject.SetActive(true); }
                else _bigArt.gameObject.SetActive(false);
            }
            if (_bigHighlight != null) _bigHighlight.gameObject.SetActive(false);   // 大图那份恒关（见常量注释）
            for (int k = 0; k < _cellHl.Count; k++)
                if (_cellHl[k] != null) _cellHl[k].gameObject.SetActive(k < _cellIdx.Count && _cellIdx[k] == Selected);
            // 边框：`Toggle borde` 那个钮管的（`AvatarTab.CheckEnableAvatarBorder`）。
            // ⚠️ 原版是按 `FindActiveAvatarBorder() != null` 决定**这个钮能不能点**，不是直接藏边框图；
            //    我们这里**简化为「开关边框那一层」** —— 这是**我们的做法**，如实标着。
            for (int k = 0; k < _grid.childCount; k++)
            {
                var bd = _grid.GetChild(k).Find("Image Container/Border");
                if (bd != null) bd.gameObject.SetActive(BorderOn);
            }
        }

        void OnSelectClicked()
        {
            if (Selected < 0) { Debug.Log("[Profile] `Select`（头像页）：**还没选中任何一个**"); return; }
            Debug.Log("[Profile] 选中头像「" + Avatars[Selected].Name + "」（# " + Selected + "）—— "
                    + "⚠️ **没有存档**：原版会 `PlayerAvatarDataManager.SetCurrentItem` + 上传，我们只在本局记住");
        }
    }
}
