// DeckSelectionPopup.cs — 卡组线最后一个弹窗：**`Deck Selection Popup with Tabs`**（原版 `DeckSelectionPopup`）
//
// ============================ 出处（唯一正本） ============================
// `资料/普查产出_0923/A1_外壳与弹窗.md` **§3**（逐节点表 + 根脚本字段）。
// 🔴 **2026-09-24 复核过坐标系**：这扇窗的根是**整屏 popup**（父链上没有 `Content Area`）
//    ⇒ A1 §3 那批坐标**是绝对的、可以直接施工**（不像卡背/异画两页被父链平移过；坑见 `资料/已知的坑.md`）。
//    实读命令：`py 工具/menu_rect.py bundle_menus_assets_all -3585457330712889985 --depth 3`。
//
// 窗口参数（A1 §3 表头）：`type=1 Popup` · `windowsPlacement=15 Popup` · `closeOnESC=1` · `autoInitialize=0`。
//
// ============================ 结构（照 A1 §3 抄）============================
//   · `Menu Dark Background` sprite=0 col **(0,0,0,.773)**（rect 比屏幕大，为了盖住任何画幅）→ **点它关窗**
//   · `Alliance Header Buttons` 167.50,35.07→1756.50,107.25 → **`Tab buttons`**（HLG spacing **12.45** align MiddleLeft）
//     → 两个页签 **260×67.6421**：`Generic Tab UI Button`（`40K_tab_button`，**出厂 m_IsOn=1**）
//     与 `Generic Tab UI Button 1`（`40K_tab_button_overwindow`，出厂 0）
//   · `Generic Window Red Background Big` = **`UI_Deck_Information_Back`** Sliced（134.50,82→1839.50,1032）
//   · `Deck Display` 144.50,124.95→1809.50,973.19
//     ├ `Header` 184.50,124.95→1769.50,209.95（`Separator Line` 在 **204.95→214.95**）
//     │  ├ `Instructions`(act=**N**, 618.72→1335.28) · `Instructions 2`(act=**Y**, 1236.12→1770.24)
//     │  ├ `Practice buttons`（HLG spacing 24.5 align MiddleLeft）→ `Generic Simplified UI Button`
//     │  │   （`UI_Button_Mulligan`，作者 **212.24×63.92**）—— 原版是**「随机挑一套」**（术语 `MenuDeck/Button/Random`）
//     │  ├ `Filter Buttons`(act=**N**) · `Collection Menu Input Field`(act=**N**, 727.00,142.91→1227.00,191.98)
//     └ `Collection Display` 144.50,208.63→1809.50,973.19 → **`Deck Scroll View`** 194.50,208.63→1759.50,986.69
//       （**1565 × 778.06**）· `RecyclableScrollRect`：`_cellWidth/_cellHeight` **225/364.5** · `_spacingX` **20**
//       · `_controlSegmentSize=1` ⇒ **列数按宽度算** = `floor(1565 ÷ (225+20))` = **6** · 原型格
//       = **`Collection Deck With Highlight`**（`useSelectedHighlight=1`，作者 250×405）
//   · `Generic Close Button Orange` 1783.11,62.90→1857.49,138.50（圆底 + `40k_general_bt_yellow_close` 56.86×58.13）
//
// ============================ 格子的画法 ============================
// 🔴 **不重写** —— 走 `MenuDraw.DeckCell`（2026-09-24 收口）：原版 `Collection Deck`（收藏窗 Deck 页）
//    与 `Collection Deck With Highlight`（本窗）**是同一份 prefab 几何的两个变体**
//    （逐字段 diff 过，唯一差别是根组件的 `useSelectedHighlight`）。本窗传 `selected:true` 画金框。
//
// ============================ 入口（我们这条链）============================
// 原版的三个调用点**全在别处**（`RankedDeckSelector.OnSelectDeckButtonClick` ·
// `RankedEventWindow.ChangeDeckButtonClick` · `DeckGeneralInfoDemo.ChangePlayerDeckButton`），
// 收藏窗那条链**不走它**（收藏窗点格 = 开 `Deck info Popup`，由那里的 `Edit Deck` 进编辑 —— 已核实）。
// ✅ **我们接的那条是原版的第三个**：练习窗 → `Show Deck Content`（翻抽屉）→ **`Change Deck`** ⇒ 开本窗。
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · 🔴 **「预组卡组」那一页是空的**：数据在 `Unity/数据/游戏数据/decklists.json`（**236 副**，真原版 id），
//     但**全仓 C# 0 引用**，而且 `资料/原版预组牌_核对.md` 实测**只有 152/236 能用我们的卡池完整拼出**
//     （匹配要 t1~t5 五级规则，那套逻辑现在只在 `工具/check_prebuilt_decks.py` 里）。
//     ⇒ 本页**如实显示空态 + 说清原因**，**不编数据**。见 `项目任务.md` §三 第 15 条。
//   · **两个页签的文案是我们定的**：原版两个 `m_text` 都是**空串**、只有 `mTerm`
//     （`MenuDeck/Button/PresetDeck` / `MenuDeck/Button/OwnDeck`），而**本地没有任何术语表/翻译源**
//     （2026-09-24 在 `assets_full` / `extract` / 反编译 / `资料/` 四处搜过，0 命中）⇒ 写 `Prebuilt Decks` / `My Decks`。
//   · `Instructions 2`(act=Y) 的文案**取不到**（它只带 `mTerm`，`m_text` 是空串）；
//     同窗的 `Instructions`(act=N) 有明文 **"Select deck"** ⇒ **我们用它那句**填在 `Instructions 2` 的框里。
//   · **搜索框照常显示**：原版出厂 `act=N`，**运行期什么时候出现读不到**（脚本里查不到）
//     ⇒ 我们让它一直显示（否则搜索不可用）。**这是一处偏离**，如实记。
//   · `Filter Buttons`（act=N，HLG + `forceExpand` 会把 60×60 撑成 84.56×80）**不建** —— 照「出厂关的件不建」。
//   · `Instructions`(act=N) 那一格**也不建**。
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>`DeckSelectionPopup : GameWindow` —— 选一套卡组（原版从战斗入口那几扇窗开）。</summary>
    public class DeckSelectionPopup : GameWindow
    {
        // 层：**高于收藏窗（最高 3043）与 `Deck info Popup`(3120+) · `Import Deck Popup`(3130+) 之下**，
        //    但**必须低于** `PromptPopup`（3140+，提示窗要能压在所有窗之上）。
        public const int QDs = 3125, QDsRow = 3126, QDsText = 3127, QDsHit = 3128;

        // ---- 几何（A1 §3 逐条；2026-09-24 从根节点复核过，见文件头）----
        public static readonly Color ShadeColor = new Color(0f, 0f, 0f, 0.773f);
        /// <summary>`Tab buttons` 容器（HLG spacing 12.45 align MiddleLeft）；两个页签各 **260×67.6421**（模板位是重合的）。</summary>
        public const float TabL = 196.30f, TabT = 35.07f, TabW = 260f, TabH = 67.6421f, TabGap = 12.45f;
        /// <summary>红底板 `Generic Window Red Background Big`（`UI_Deck_Information_Back`，border 42/363/655/81）。</summary>
        public const float RedL = 134.50f, RedT = 82f, RedR = 1839.50f, RedB = 1032f;
        /// <summary>`Deck Display`（`Header` + `Collection Display` 的父）。</summary>
        public const float DdL = 144.50f, DdT = 124.95f, DdR = 1809.50f, DdB = 973.19f;
        /// <summary>`Deck Scroll View`（RSR 的视口）**194.50,208.63 → 1759.50,986.69**（1565 × 778.06）。</summary>
        public const float SvL = 194.50f, SvT = 208.63f, SvR = 1759.50f, SvB = 986.69f;
        public static readonly PxRect SvRect = new PxRect(SvL, SvT, SvR, SvB);
        /// <summary>`Header` 那一行（`Separator Line` 在 204.95→214.95）。</summary>
        public const float HdrT = 124.95f, HdrB = 209.95f, SepT = 204.95f, SepB = 214.95f;
        /// <summary>提示语：`Instructions`(act=N) 618.72→1335.28 · `Instructions 2`(act=Y) 1236.12→1770.24。</summary>
        public const float Ins2L = 1236.12f, Ins2R = 1770.24f;
        /// <summary>`Practice buttons`(HLG spacing 24.5 align MiddleLeft) 184.50,117.50→443.61,217.39；
        /// 子件作者 **212.24×63.92**、`childControl=0/forceExpand=0` ⇒ 保持作者尺寸、垂直居中。</summary>
        public const float RndL = 184.50f, RndW = 212.24f, RndH = 63.92f, RndBoxT = 117.50f, RndBoxB = 217.39f;
        /// <summary>搜索框 `Collection Menu Input Field`（`InputFieldBackground` 500×49.078）。</summary>
        public const float InL = 727.00f, InT = 142.91f, InR = 1227.00f, InB = 191.98f;
        /// <summary>关闭圆钮 `Generic Close Button Orange`（74.39×75.60）+ 图标 56.86×58.13。</summary>
        public const float CloseL = 1783.11f, CloseT = 62.90f, CloseR = 1857.49f, CloseB = 138.50f;
        public const float CloseIw = 56.86f, CloseIh = 58.13f;

        /// <summary>格 **225×364.5**、横向步进 **245**（= 225 + spacing 20）。</summary>
        public const float CellW = 225f, CellH = 364.5f, CellGapX = 20f, CellStep = CellW + CellGapX;
        /// <summary>列数 = `floor(视口宽 ÷ (格宽 + spacingX))`（`_controlSegmentSize=1`）。</summary>
        public static int Cols { get { return Mathf.Max(1, Mathf.FloorToInt((SvR - SvL) / CellStep)); } }
        /// <summary>内容**整体居中**的左边距 = `(视口宽 − (列数×格宽 + (列数−1)×spacing)) / 2`
        /// = (1565 − 1450) / 2 = **57.5**（`CreateCellPool` 的算法；与收藏窗 Cards/Styles 两页同一口径）。</summary>
        public static float PadX
        {
            get { return (SvR - SvL - (Cols * CellW + (Cols - 1) * CellGapX)) * 0.5f; }
        }

        public static PxRect CellRect(int i)
        {
            int r = i / Cols, c = i % Cols;
            float x = SvL + PadX + c * CellStep;
            float y = SvT + r * CellH;
            return new PxRect(x, y, x + CellW, y + CellH);
        }

        public static readonly Vector4 RedBorder = new Vector4(42f, 363f, 655f, 81f);
        const float RedTexW = 1100f, RedTexH = 701f;
        static readonly Vector4 TabBorder = new Vector4(0f, 0f, 0f, 0f);

        // ---- 状态 ----
        /// <summary>`false` = 预组卡组那一页（原版出厂 `m_IsOn=1` 的是它）；`true` = 我的卡组。</summary>
        public bool OwnDecks;
        /// <summary>选中那一套在当前列表里的下标（−1 = 没选）。</summary>
        public int DeckIndex = -1;
        /// <summary>搜索串（原版 `searchBar` → `Filter`：**只按卡组名**过滤）。</summary>
        public string Search = "";
        /// <summary>点一套之后干什么（**关窗 + 回调** —— 原版 `DeckSelectionPopup.Select` 就是这两步）。</summary>
        public System.Action<CollectionData.DeckInfo> OnPicked;

        public MenuScroll Scroll;
        /// <summary>画出来的格（自检用）。</summary>
        public readonly List<Transform> Cells = new List<Transform>();

        /// <summary>最近一次开出来的（自检用）。</summary>
        public static DeckSelectionPopup LastOpened;

        public Transform TabHit(bool own) { return Find(transform, "TabHit_" + (own ? "Own" : "Pre")); }
        public Transform CloseHit { get { return Find(transform, "CloseHit"); } }
        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform RandomHit { get { return Find(transform, "RandomHit"); } }
        public Transform SearchHit { get { return Find(transform, "SearchHit"); } }
        /// <summary>空态那行字现在显示什么（自检用）。</summary>
        public string EmptyText { get { return _empty; } }
        string _empty = "";

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public static DeckSelectionPopup Create(WindowsManager mgr, System.Action<CollectionData.DeckInfo> onPicked = null)
        {
            var go = new GameObject("Deck Selection Popup with Tabs");
            var win = go.AddComponent<DeckSelectionPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;              // 实证 1.0
            win.Manager = mgr;
            win.OnPicked = onPicked;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        public override void Open()
        {
            // 原版出厂选中是「预组卡组」；但那一页我们**没接数据**（见文件头）⇒ 起手落在「我的卡组」，
            // 并在页面上如实写清预组那一页为什么是空的。**这是一处偏离**，出声。
            OwnDecks = true;
            DeckIndex = CollectionData.CurrentIndex();
            Search = "";
            Build();
            LastOpened = this;
        }

        // ============================================================ 列表数据

        /// <summary>当前页签 + 搜索串过滤之后的卡组下标（**原版 `Filter` 只按名字**）。</summary>
        public List<int> Shown()
        {
            var list = new List<int>();
            if (OwnDecks)
            {
                string q = (Search ?? "").Trim().ToLowerInvariant();
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    var info = CollectionData.DeckAt(i);
                    if (q.Length > 0 && (info.Name ?? "").ToLowerInvariant().IndexOf(q, System.StringComparison.Ordinal) < 0)
                        continue;
                    list.Add(i);
                }
            }
            return list;      // 预组那一页：**空**（数据没接，见文件头）
        }

        /// <summary>当前列表条数（自检用）。</summary>
        public int ShownCount { get { return Shown().Count; } }

        // ============================================================ 建

        void Build()
        {
            var root = transform;
            for (int i = root.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(root.GetChild(i).gameObject);

            // 1) 压暗 + 点背景关窗（原版 `backgroundCloseButton`）
            var shade = MenuDraw.Rect(root, CardArt.Solid(),
                                      new PxRect(-1327.30f, -746.18f, 3247.30f, 1826.18f), "Menu Dark Background",
                                      QDs, ShadeColor);
            if (shade == null)
                MenuDraw.Rect(root, CardArt.Solid(), new PxRect(0f, 0f, 1920f, 1080f), "Menu Dark Background", QDs, ShadeColor);
            Hit(root, "BackgroundHit", new PxRect(0f, 0f, 1920f, 1080f), () => Close(), QDsHit - 1);

            // 2) 两个页签（HLG spacing 12.45 ⇒ 第 2 个从 196.30 + 260 + 12.45 起）
            //    容器高 **68.50**、键高 **67.6421** ⇒ 竖直居中（`align=MiddleLeft`）
            float ty1 = TabT + (68.50f - TabH) * 0.5f, ty2 = ty1 + TabH;
            BuildTab(root, "Prebuilt Decks", "40K_tab_button", TabL, ty1, ty2, false);
            BuildTab(root, "My Decks", "40K_tab_button_overwindow", TabL + TabW + TabGap, ty1, ty2, true);

            // 3) 红底板
            MenuDraw.Nine(root, CardArt.MenuUi("UI_Deck_Information_Back"), new PxRect(RedL, RedT, RedR, RedB),
                          RedBorder, RedTexW, RedTexH, QDs, null, true);

            // 4) `Header` 那一行：分隔线 + 提示语 + 「随机挑一套」
            MenuDraw.Rect(root, CardArt.MenuUi("40k_main_line"), new PxRect(DdL, SepT, DdR, SepB), "Separator Line", QDsRow);
            // ⚠️ `Instructions 2`(act=Y) 的文案取不到；同窗 `Instructions`(act=N) 有明文 "Select deck" ⇒ 用它那句
            MenuDraw.Text(root, new PxRect(Ins2L, HdrT, Ins2R, HdrB), "Select deck", Color.white, "Instructions 2",
                          36f, QDsText);
            {
                float ry1 = RndBoxT + (RndBoxB - RndBoxT - RndH) * 0.5f;
                var rr = new PxRect(RndL, ry1, RndL + RndW, ry1 + RndH);
                // `UI_Button_Mulligan` **没有 border**（实读 `Sprite/UI_Button_Mulligan.json` 的 `m_Border = None`）
                // ⇒ 拉伸。走 `Nine` 会吐「border 比图还大，退回单块」（§三 第 15 条 第 57 行；同 `DeckInfoPopup`）。
                MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Mulligan"), rr, "Random Bg", QDsRow);
                MenuDraw.Text(root, rr, "Random", Color.white, "Random Text", 45f, QDsText);
                Hit(root, "RandomHit", rr, PickRandom, QDsHit);
            }

            // 5) 搜索框（**出厂 act=N，我们照常显示** —— 见文件头那条偏离）
            MenuDraw.Nine(root, CardArt.MenuUi("InputFieldBackground"), new PxRect(InL, InT, InR, InB),
                          new Vector4(10f, 10f, 10f, 10f), 32f, 32f, QDsRow,
                          new Color(0.0627f, 0f, 0f, 1f));
            RefreshSearchText();
            Hit(root, "SearchHit", new PxRect(InL, InT, InR, InB), BeginTyping, QDsHit);

            // 6) 卡组列表（RSR 视口 + 6 列格）
            var holder = MenuDraw.Node(root, "Deck Scroll View", SvRect);
            MenuDraw.Node(holder, "Viewport", SvRect);
            var shown = Shown();
            int rows = Mathf.Max(1, Mathf.CeilToInt(shown.Count / (float)Cols));
            Scroll = MenuScroll.TopAligned(SvRect, rows * CellH);
            Scroll.Owner = gameObject;
            Scroll.OnChanged = () => RebuildCells(holder);
            PointerLayer.RegisterScroll(Scroll);
            RebuildCells(holder);

            // 7) 空态那一行（我们自己加的 —— 原版没有；用来**如实说明**为什么这一页是空的）
            RefreshEmptyText();
            MenuDraw.Text(root, new PxRect(SvL, SvT + 120f, SvR, SvT + 190f), _empty, new Color(0.66f, 0.66f, 0.66f, 1f),
                          "Empty Note", 32f, QDsText);

            // 8) 关闭圆钮
            MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Round_background"),
                          new PxRect(CloseL, CloseT, CloseR, CloseB), "Close Bg", QDsRow, null, true);
            float ix = CloseL + (CloseR - CloseL - CloseIw) * 0.5f;
            float iy = CloseT + (CloseB - CloseT - CloseIh) * 0.5f;
            MenuDraw.Rect(root, CardArt.MenuUi("40k_general_bt_yellow_close"),
                          new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), "Close Icon", QDsRow, null, true);
            Hit(root, "CloseHit", new PxRect(CloseL, CloseT, CloseR, CloseB), () => Close());

            if (MissingArtCount() > 0)
                Debug.LogWarning("[DeckSel] ⚠️ 有 " + MissingArtCount() + " 张图取不到（**这几件没画**）：" + MissingArtList());
        }

        readonly List<string> _missing = new List<string>();
        Texture2D Art(string n)
        {
            var t = CardArt.MenuUi(n);
            if (t == null && !_missing.Contains(n)) _missing.Add(n);
            return t;
        }
        int MissingArtCount() { return _missing.Count; }
        string MissingArtList() { return string.Join("、", _missing.ToArray()); }

        void BuildTab(Transform root, string label, string art, float x1, float y1, float y2, bool own)
        {
            var r = new PxRect(x1, y1, x1 + TabW, y2);
            MenuDraw.Rect(root, Art(art), r, own ? "Generic Tab UI Button 1" : "Generic Tab UI Button", QDsRow);
            // 选中态：原版 `EverguildToggle` 的 `m_IsOn` —— 我们用**文字色**区分（选中亮、未选中灰）
            MenuDraw.Text(root, r, label, OwnDecks == own ? Color.white : new Color(0.6f, 0.6f, 0.6f, 1f),
                          "Tab Text " + (own ? "Own" : "Pre"), 34f, QDsText);
            Hit(root, "TabHit_" + (own ? "Own" : "Pre"), r, () => SwitchTab(own), QDsHit);
        }

        /// <summary>切页签（照原版：重建列表 + `Instructions` 那类提示不变）。</summary>
        public void SwitchTab(bool own)
        {
            if (OwnDecks == own) return;
            OwnDecks = own;
            DeckIndex = -1;
            Debug.Log("[DeckSel] 切到「" + (own ? "我的卡组" : "预组卡组") + "」那一页");
            RebuildAll();
        }

        /// <summary>搜索（原版 `searchBar` → `Filter`：只按卡组名 + 重画）。</summary>
        void BeginTyping()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) return;
            pl.BeginText(Search, 32,
                         s => { Search = s ?? ""; RefreshSearchText(); RebuildListNow(); },
                         () => RefreshSearchText(),
                         s => { Search = s ?? ""; RefreshSearchText(); RebuildListNow(); });
            Debug.Log("[DeckSel] 搜索卡组名：输入后回车确认，ESC 取消");
        }

        void RefreshSearchText()
        {
            var old = Find(transform, "Search Text");
            if (old != null) CollectionWindow.DestroySafe(old.gameObject);
            bool editing = PointerLayer.Instance != null && PointerLayer.Instance.TextEditing;
            string txt = editing ? (PointerLayer.Instance.TextBuffer + "_")
                                 : (string.IsNullOrEmpty(Search) ? "Search" : Search);
            var lb = MenuDraw.Text(transform,
                                   new PxRect(InL + 25f, InT + 8f, InR - 50f, InB - 8f), txt,
                                   string.IsNullOrEmpty(Search) && !editing
                                       ? new Color(0.67f, 0.67f, 0.67f, 0.5f) : Color.white,
                                   "Search Text", 32f, QDsText);
            if (lb != null) lb.AlignLeftOn(LayoutSpace.FromPixel(InL + 25f, 0f).x);
        }

        /// <summary>抽一套（原版 `RandomizeDeck`：从当前列表随机挑 ⇒ **选中并关窗**）。</summary>
        public void PickRandom()
        {
            var shown = Shown();
            if (shown.Count == 0) { Debug.Log("[DeckSel] 当前列表是空的，没得抽"); return; }
            Pick(shown[Random.Range(0, shown.Count)]);
        }

        /// <summary>选一套：**关窗 + 回调**（= 原版 `DeckSelectionPopup.Select` 的两步）。</summary>
        public void Pick(int deckIdx)
        {
            DeckIndex = deckIdx;
            var info = CollectionData.DeckAt(deckIdx);
            Debug.Log("[DeckSel] 选中：「" + info.Name + "」⇒ 关窗 + 回调");
            if (OnPicked != null) OnPicked(info);
            Close();
        }

        /// <summary>重建整个窗（切页签时调）。</summary>
        public void RebuildAll()
        {
            for (int i = transform.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(transform.GetChild(i).gameObject);
            _missing.Clear();
            Build();
        }

        /// <summary>只重建列表（搜索时调 —— 别整窗重建，不然键盘焦点会跟着没了）。</summary>
        public void RebuildListNow()
        {
            var h = Find(transform, "Deck Scroll View");
            if (h != null) RebuildCells(h);
            RefreshEmptyNote();
        }

        void RebuildCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) CollectionWindow.DestroySafe(parent.GetChild(i).gameObject);
            Cells.Clear();

            var shown = Shown();
            if (Scroll == null) return;
            for (int k = 0; k < shown.Count; k++)
            {
                var r = Scroll.Shift(CellRect(k));
                if (!Scroll.Intersects(r)) continue;
                int idx = shown[k];
                Cells.Add(MenuDraw.DeckCell(parent, "DeckSel_" + idx, r, CollectionData.DeckAt(idx),
                                            idx == DeckIndex, QDsRow, QDsText, QDsText + 1, QDsHit,
                                            () => Pick(idx), SvRect));
            }
            RefreshEmptyNote();
        }

        void RefreshEmptyNote()
        {
            RefreshEmptyText();
            var lb = Find(transform, "Empty Note");
            if (lb == null) return;
            var l = lb.GetComponent<Label>();
            if (l != null) l.SetText(_empty);
            lb.gameObject.SetActive(Shown().Count == 0);
        }

        /// <summary>空态那句话。**如实说明原因**，不编数据（红线：不许静默失败）。</summary>
        void RefreshEmptyText()
        {
            if (!OwnDecks)
                _empty = "预组卡组的数据还没接：本地有 236 副（Unity/数据/游戏数据/decklists.json，真原版 id），"
                       + "但全仓 C# 0 引用，而且实测只有 152 副能用我们的卡池完整拼出 ⇒ 先如实留空";
            else if (Shown().Count == 0)
                _empty = "没有匹配的卡组（搜索串：「" + Search + "」）";
            else
                _empty = "";
        }

        // ============================================================ 小工具

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QDsHit)
        {
            var hit = MenuDraw.Node(parent, name, r);
            var quad = ImageQuad.Create(hit, CardArt.Solid(), Vector3.zero, LayoutSpace.Px(r.H),
                                        new Vector2(0.5f, 0.5f), "Hit");
            if (quad != null)
            {
                quad.SetAspect(r.W / Mathf.Max(1e-6f, r.H));
                quad.SetTint(new Color(0f, 0f, 0f, 0f));
                quad.SetRenderQueue(q);
            }
            var wb = hit.gameObject.AddComponent<WindowButton>();
            wb.onClick = onClick;
            return hit;
        }
    }
}
