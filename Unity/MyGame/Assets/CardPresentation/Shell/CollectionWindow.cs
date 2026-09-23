// CollectionWindow.cs — 阶段二第 3 层第 5 件「卡组线」：**收藏窗**（`Collection Menu Variant`）
//
// ============================ 出处（唯一正本） ============================
// `资料/阶段二_卡组线_原版规格.md`（本文件的常量都从它来；细节表在 `资料/普查产出_0923/`
// 的 A1_外壳与弹窗 / A2_Deck页 / A3_Cards页 / A4_装饰页与驱动链 四份）。
//
// 🔴 **这条线是「进卡组编辑的唯一门」** —— 在此之前 `DeckEditor.unity` 能编辑、但**玩家没有路径进去**
//    （`资料/普查产出_0917/菜单盘点_汇总.md` §二·5 原话）。
//
// ============================ 原版实测（照抄，别自己编） ============================
//   · 类名 **`CollectionScreen : GameWindowWithTabs`**；窗口参数 `type=0 Fullscreen` ·
//     `windowsPlacement=5 Canvas` · `closeOnESC=1` · `updateNavPanel=1` · `extraScaleSmallScreen=1.0`
//     （与**奖励窗**只差 `closeOnESC`：它是 1、奖励窗是 0）
//   · 入口 = 主菜单左竖导航的 **COLLECTION 钮**（`OpenWindowButton` GUID `6b5ba7fb…`，`closeOtherMenus=1`）
//   · 外壳与奖励窗/商店**同一个壳**（`Content Area` 167.17,70.94→1920.01,1080 · 左栏 165 宽 ·
//     同一批图 `40k_main_tab_background`/`_shadow`/`40k_main_bt_selected BW`/`40k_main_bt_nametag`）
//     ⇒ 直接继承 `MainMenuSubmenuWindow`。**唯一差别**：左栏第一个键顶边 **188.64**
//     （条本身从 158.64 起 + VLG `padTop=30`）⇒ 覆写 `BarPadTop = 188.64 − 70.94 = **117.7**`。
//   · 四个页签的**卡面文案 = Decks / Cards / Cosmetics / Styles**（不是页节点名），
//     图标 `40k_collection_bt_{decks,cards,cosmetics,style}`，标签 fs **36**（auto 5–36）。
//
// ============================ 本轮建了什么 / 没建什么（出声） ============================
//   ✅ 外壳 + 四页签 · **Deck 页**（页头 + 卡组列表，6 列 × 225×364.5，可纵向滚）
//   ⏭ **Cards / Cosmetics / Styles 三页**：本轮只建占位 + `NotifyNotBuilt`（下一切片；规格已就绪）
//   ⏭ 三个弹窗（`Deck info Popup` / `Deck Selection Popup with Tabs` / `Import Deck Popup`）
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **卡组格里的图**：原版是**玩家选的卡背**（`CollectionManager` 的 cosmetic，233 张**还没导进工程**）
//     ⇒ 本轮用 `CardArt.CardBack(阵营)`（**该阵营的默认卡背**，工程里现成）顶着。
//   · **卡组的稳定标识**用 `Name`（`PlayerDeck` 没有 id 字段）—— 重命名会让选中态丢，如实记。
using System.Collections.Generic;
using UnityEngine;
using RuleEngine;      // `CardDatabase` / `CardDef`（`DeckDef` 那套在 RuleEngine 里）

namespace CardPresentation
{
    /// <summary>收藏窗。原版 `CollectionScreen : GameWindowWithTabs`。</summary>
    public class CollectionWindow : MainMenuSubmenuWindow
    {
        // ============================================================ 页签

        /// <summary>左栏四个键。文案是**卡面文案**（`Cosmetics`/`Styles`），不是页节点名。出处 A1 表 `:17-22`。</summary>
        public static readonly TabBtnSpec[] Buttons =
        {
            new TabBtnSpec("40k_collection_bt_decks",     "Decks",     36f, 5f, 36f, "DecksTabButton",     47.9f),
            new TabBtnSpec("40k_collection_bt_cards",     "Cards",     36f, 5f, 36f, "CardsTabButton",     47.9f),
            new TabBtnSpec("40k_collection_bt_cosmetics", "Cosmetics", 36f, 5f, 36f, "CardBacksTabButton", 47.9f),
            new TabBtnSpec("40k_collection_bt_style",     "Styles",    36f, 5f, 36f, "AltArtTabButton",    47.9f),
        };

        /// <summary>左栏第一个键的顶边（原版 188.64）− `Content Area` 顶边（70.94）= **117.7**。
        /// ⚠️ 别照抄「padTop = 30」—— 那是**条内**布局组的 padTop，条本身还从 158.64 起（比 `Content Area` 低 87.7）。</summary>
        protected override float BarPadTop { get { return 117.7f; } }

        // ============================================================ 页内自己的层（照兄弟序逐层 +1）
        public const int QPagePanel = 3030, QPageRow = 3031, QPageText = 3032, QPageOverlay = 3033;

        /// <summary>页面上文字的主色。原版这几页的 TMP 都是白（`DeckRuntime.Ink` 是**卡组编辑那一边**的
        /// 常量，跨窗别用 —— 那是两个窗各自的调色板）。</summary>
        static readonly Color PageInk = Color.white;

        public static CollectionWindow Create(WindowsManager mgr)
        {
            var go = new GameObject("Collection Menu Variant");
            var win = go.AddComponent<CollectionWindow>();
            win.type = WindowType.Fullscreen;                 // 实证 type=0
            win.placement = WindowsPlacement.Canvas;          // 实证 windowsPlacement=5（**商店是 10**）
            win.closeOnEsc = true;                            // 实证 closeOnESC=1（**奖励窗是 0**）
            win.extraScaleSmallScreen = 1f;                   // 实证 1.0
            win.Manager = mgr;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>卡组列表的滚动区（**纵向**）—— 自检用（批处理里没有滚轮，直调 `Wheel/ScrollBy`）。</summary>
        public MenuScroll DeckScroll;
        /// <summary>卡池（Cards 页）的滚动区 —— 同上。</summary>
        public MenuScroll CardsScroll;
        /// <summary>卡池当前画出来的格（自检比版面用）。</summary>
        public readonly List<Transform> CardsCells = new List<Transform>();

        // ============================================================ Cards 页的几何（A3 实测）
        /// <summary>卡池视口 **330.2, 155.9 → 1919.9, 1079.9**（1589.7 × 924.06）——
        /// ⚠️ 与 Deck 页的视口（330.9,155.9→1920,1080）**差 0.7/0.1**，别互相抄（两份实测如此）。</summary>
        public static readonly PxRect CardsViewport = new PxRect(330.2f, 155.9f, 1919.9f, 1079.9f);
        /// <summary>卡位 **262.5 × 384** · spacing **0** · 行距 **384** · **贴左但整体居中**（A3）。</summary>
        public const float CardsCellW = 262.5f, CardsCellH = 384f;
        /// <summary>列数 = `floor(1589.7 ÷ (262.5 + 0))` = **6**（原版运行时算的，A3 §四·1 说算法读不到）。</summary>
        public const int CardsCols = 6;
        /// <summary>贴左但整体居中：内容宽 6×262.5 = 1575 < 视口 1589.7 ⇒ 两侧各留 **7.35**。</summary>
        public static float CardsPadX { get { return (CardsViewport.W - CardsCols * CardsCellW) * 0.5f; } }

        public static PxRect CardsCellRect(int i)
        {
            int r = i / CardsCols, c = i % CardsCols;
            float x = CardsViewport.x1 + CardsPadX + c * CardsCellW;
            float y = CardsViewport.y1 + r * CardsCellH;
            return new PxRect(x, y, x + CardsCellW, y + CardsCellH);
        }

        /// <summary>Cards 页的筛选状态（**复用卡组编辑那套** `DeckEditorState.Filter` —— 别写第二套）。</summary>
        static DeckEditorState _cardsState;
        public static DeckEditorState CardsState
        {
            get
            {
                if (_cardsState == null) _cardsState = new DeckEditorState(CardDatabase.Load());
                return _cardsState;
            }
        }
        /// <summary>左栏四个键的选中底图（`BuildShell` 给的**数组**；选中态由 `RefreshHighlights` 刷）。</summary>
        ImageQuad[] _btnHighlight;

        /// <summary>切页后刷左栏选中态（基类 `ChangeTab` 会调 —— 2026-09-23 加的那条钩子）。
        /// 🔴 起因：收藏窗第一版漏了这一步，**截图里高亮停在第 2 键上**（DECKS 页却亮着 CARDS）。</summary>
        public override void RefreshHighlights()
        {
            int sel = tabButtons != null ? tabButtons.CurrentVisualIndex : -1;
            if (_btnHighlight == null) return;
            for (int i = 0; i < _btnHighlight.Length; i++)
                if (_btnHighlight[i] != null) _btnHighlight[i].gameObject.SetActive(i == sel);
        }
        /// <summary>卡组格的节点（自检比版面用；顺序 = 卡组顺序）。</summary>
        public readonly List<Transform> DeckCells = new List<Transform>();

        // ============================================================ 页的几何（A2 实测）

        /// <summary>`Deck Scroll View/Viewport`：**330.9, 155.9 → 1920, 1080**（1589.1 × 924.1）。</summary>
        public static readonly PxRect DeckViewport = new PxRect(330.9f, 155.9f, 1920f, 1080f);
        /// <summary>卡组格 **225 × 364.5**（prefab 根 250×405 的 **0.9 倍**）· 横向 spacing **20** · pad **L10** ⇒ **6 列**。</summary>
        public const float DeckCellW = 225f, DeckCellH = 364.5f, DeckSpacingX = 20f, DeckPadL = 10f;
        public const float DeckCellScale = 0.9f;
        /// <summary>列数 = `floor(1589.1 ÷ (225+20))` = **6**（原版是运行时算的，A2 §四·1 说算法读不到）。</summary>
        public const int DeckCols = 6;

        public static PxRect DeckCellRect(int i)
        {
            int r = i / DeckCols, c = i % DeckCols;
            float x = DeckViewport.x1 + DeckPadL + c * (DeckCellW + DeckSpacingX);
            float y = DeckViewport.y1 + r * (DeckCellH + DeckSpacingX);
            return new PxRect(x, y, x + DeckCellW, y + DeckCellH);
        }

        // ------------------------------------------------------------ Cards 页（卡池网格）

        /// <summary>Cards 页：**卡池网格**（6 列 × 262.5×384 · 贴左但整体居中 · 纵向滚）+ 页头 + 万能卡计数。
        /// ⚠️ **筛选面板（13 阵营 3 列 / 稀有度 / 费用那几组格）本轮只做「清空筛选」与计数条**，
        ///    完整筛选格是下一切片（规格在 `资料/普查产出_0923/A3_Cards页.md` §「筛选栏」）。**出声**。</summary>
        public void BuildCardsPage(Transform page)
        {
            // 页头：Filters 圆钮 + 文案 + Clear filters + 万能卡计数（A3：`CardsTab/Header Filters/WIldcard Display`）
            Rect(page, "40k_bt_icon_search", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Icon", QPageRow);
            var fltLab = Text(page, "Filters", FltBtnX + 60f, FltBtnX + 300f, FltBtnY, FltBtnY + FltBtnS,
                              5, PageInk, "Filters Label", 42f);
            if (fltLab != null) fltLab.SetRenderQueue(QPageText);
            Rect(page, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            var clrLab = Text(page, "Clear filters", ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", 33f);
            if (clrLab != null) clrLab.SetRenderQueue(QPageText);
            AddHit(page, "ClearFiltersHit",
                   new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH), QPageRow,
                   () => { CardsState.SetFilter(DeckFilter.None); RebuildCardsPage(); NotifyNotBuilt("完整筛选面板（本轮只做「清空筛选」）"); });

            // 万能卡计数：4 个稀有度图标 + 各自的数字（**复用卡组编辑那条几何**，别抄第二份）
            Rect(page, "40k_topmarquee_currency_display_BW",
                 new PxRect(1550f, 91.5f, 1870f, 135.5f), "Wildcard Bg", QPagePanel);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            var counts = CardsRarityCounts();
            for (int i = 0; i < 4; i++)
            {
                float x = 1565f + 75f * i;
                Rect(page, wcIc[i], new PxRect(x, 91.5f, x + 30f, 135.5f), "Wildcard Icon " + i, QPageRow);
                var t = Text(page, counts[i].ToString(), x + 30f, x + 71f, 91.5f, 135.5f, 5, PageInk,
                             "Wildcard Count " + i, 32.6f);
                if (t != null) t.SetRenderQueue(QPageText);
            }

            var holder = Node(page, "Scroll View", CardsViewport);
            Node(holder, "Viewport", CardsViewport);
            int n = CardsState.VisibleCards().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)CardsCols));
            CardsScroll = MenuScroll.TopAligned(CardsViewport, rows * CardsCellH);
            CardsScroll.Owner = gameObject;
            CardsScroll.OnChanged = () => RebuildCardsCells(holder);
            PointerLayer.RegisterScroll(CardsScroll);
            RebuildCardsCells(holder);
        }

        int[] CardsRarityCounts()
        {
            var rc = new int[4];
            foreach (var c in CardsState.VisibleCards())
            {
                if (c.Rarity == "common") rc[0]++;
                else if (c.Rarity == "rare") rc[1]++;
                else if (c.Rarity == "epic") rc[2]++;
                else if (c.Rarity == "legendary") rc[3]++;
            }
            return rc;
        }

        /// <summary>重建整个 Cards 页（筛选变了调它；自检也用）。</summary>
        public void RebuildCardsPage()
        {
            var page = transform.Find("Tabs/" + PageNode(1));
            if (page == null) return;
            for (int i = page.childCount - 1; i >= 0; i--) DestroySafe(page.GetChild(i).gameObject);
            BuildCardsPage(page);
        }

        void RebuildCardsCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            CardsCells.Clear();

            var prevClip = Clip;
            Clip = CardsViewport;                      // 裁切（`RectMask2D` 等效物）
            var list = CardsState.VisibleCards();
            float scale = CardsCellH / (CardView.Height * 108f);      // 按**卡位高 384** 反解（同卡组编辑）
            for (int i = 0; i < list.Count; i++)
            {
                var content = CardsCellRect(i);
                var r = CardsScroll.Shift(content);
                if (!CardsScroll.Intersects(r)) continue;
                var v = CardView.Create(parent, BattleDriver.ToCardData(list[i], list[i].Faction), "CollectionCard_" + i);
                if (v == null) continue;
                v.gameObject.SetActive(true);
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                v.SetData(BattleDriver.ToCardData(list[i], list[i].Faction));
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                CardsCells.Add(v.transform);
                AddHit(parent, "CardHit_" + i, r, QPageRow, () => NotifyNotBuilt("卡片详情窗（点卡打开）"));
            }
            Clip = prevClip;
        }

        /// <summary>卡池当前可见卡数（自检用；筛选之后会变）。</summary>
        public int CardsVisibleCount { get { return CardsState.VisibleCards().Count; } }

        // ============================================================ 页头（A2 §三·4）
        public const float CreateX = 1661f, ImportX = 1391f, HdrBtnY = 80.9f, HdrBtnW = 245f, HdrBtnH = 60f;
        public const float FltBtnX = 367.2f, FltBtnY = 88.5f, FltBtnS = 50f;
        /// <summary>⚠️ 原版这条 `Clear Filter Button` 是 `IgnoreLayout`、模板位在容器外（A1 §153）⇒
        /// 这个 x 是**按「容器内右对齐」实算**的（容器右边界 1468.6 − 250 = 1218.6），不是抄来的。</summary>
        public const float ClearFltX = 1218.6f, ClearFltY = 83.5f, ClearFltW = 250f, ClearFltH = 60f;

        public override void Open()
        {
            Build();
            if (tabButtons != null) tabButtons.Click(0);       // 默认落在第一页（`GetStartingTab` 的回落）
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Collection] `" + what + "` 还没实现（本轮建到「外壳 + 四页签 + Deck 页」，"
                      + "见 `Shell/CollectionWindow.cs` 文件头与 `资料/阶段二_卡组线_原版规格.md` §七）");
        }

        static string PageNode(int p)
        {
            switch (p)
            {
                case 0: return "Select Deck Tab";          // ⚠️ 页节点名不是 "Deck Tab"
                case 1: return "Card Collection Tab";
                case 2: return "Cardback Tab";
                default: return "Alternate Art Tab";
            }
        }

        public void Build()
        {
            var res = BuildShell(transform, Buttons, "CollectionTabButton_", "Tabs");
            tabButtons = res.buttons;
            _btnHighlight = res.highlight;

            // 🔴 `visualTypes` 在基类里带一个**奖励窗的默认表** ⇒ 本窗必须**整表替换**
            if (visualTypes != null)
            {
                visualTypes.Clear();
                visualTypes.Add(WindowTabType.CollectionDecks);
                visualTypes.Add(WindowTabType.CollectionCards);
                visualTypes.Add(WindowTabType.CollectionCosmetics);
                visualTypes.Add(WindowTabType.CollectionStyles);
            }

            if (MissingArt.Count > 0)
                Debug.LogWarning("[Collection] ⚠️ 有 " + MissingArt.Count + " 张图取不到（**这些件没画**）："
                                 + string.Join("、", MissingArt.ToArray()));

            tabs.Clear();
            for (int p = 0; p < 4; p++)
            {
                var pg = Node(tabHolder, PageNode(p), TabsRect);
                var t = pg.gameObject.AddComponent<CollectionTabPage>();
                t.SetHost(this, pg, p);
                tabs.Add(t);
            }

            // 🔴 **本窗的底图必须压到所有页内容之下**（2026-09-23 实拍抓的）：
            //    `CardView`（卡池用的那个外来组件）**所有层都落在默认队列 3000**
            //    （全工程只给 SDF 那层显式设过 3000，其余靠材质默认）—— 而基类给 `Background` 的是
            //    `QPanel = 3005` ⇒ **整片卡池被窗口底图盖住**：截图里一片空，而**断言全绿**
            //    （断言量的是节点位置与尺寸，量不到「谁盖谁」）。⇒ 把这张底图压到 2000：
            //    页内容（3000+）与左栏（3005）都在它上面；它下面没有别的东西（它本来就是最底层）。
            // ⚠️ 底图在 `Content Area/Background`（不是窗口根的直接子节点 —— 正本 §二）
            var bgNode = transform.Find("Content Area/Background");
            if (bgNode != null)
                foreach (var q in bgNode.GetComponentsInChildren<ImageQuad>(true)) q.SetRenderQueue(2000);

            foreach (var t in tabs) t.Setup();
        }

        // ============================================================ Deck 页的内容（由 `CollectionTabPage` 调）

        /// <summary>页头：Filters 键 + Clear filters + Search + Import/Create（A2 §三·4）。</summary>
        public void BuildDeckHeader(Transform page)
        {
            Rect(page, "40k_bt_icon_search", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Icon", QPageRow);
            var fltLab = Text(page, "Filters", FltBtnX + 60f, FltBtnX + 300f, FltBtnY, FltBtnY + FltBtnS,
                              5, PageInk, "Filters Label", 42f);
            if (fltLab != null) fltLab.SetRenderQueue(QPageText);

            Rect(page, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            var clrLab = Text(page, "Clear filters", ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", 33f);
            if (clrLab != null) clrLab.SetRenderQueue(QPageText);

            Rect(page, "40K_button", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), "Import", QPageRow);
            Rect(page, "40K_button", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), "Create", QPageRow);
            var impLab = Text(page, "Import", ImportX, ImportX + HdrBtnW, HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Import Text", 36f);
            if (impLab != null) impLab.SetRenderQueue(QPageText);
            var newLab = Text(page, "Create", CreateX, CreateX + HdrBtnW, HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Create Text", 36f);
            if (newLab != null) newLab.SetRenderQueue(QPageText);

            AddHit(page, "ImportHit", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => NotifyNotBuilt("Import Deck Popup"));
            AddHit(page, "CreateHit", new PxRect(CreateX, HdrBtnY, CreateX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => CreateDeck());
        }

        /// <summary>卡组列表（**纵向滚**）：6 列 × 225×364.5、spacing (20,0)、pad L10。</summary>
        public void BuildDeckList(Transform page)
        {
            var holder = Node(page, "Deck Scroll View", DeckViewport);
            Node(holder, "Viewport", DeckViewport);
            int n = CollectionData.DeckCount();
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)DeckCols));
            float contentH = rows * (DeckCellH + DeckSpacingX) - DeckSpacingX;
            DeckScroll = MenuScroll.TopAligned(DeckViewport, contentH);
            DeckScroll.Owner = gameObject;
            DeckScroll.OnChanged = () => RebuildDeckCells(holder);
            PointerLayer.RegisterScroll(DeckScroll);
            RebuildDeckCells(holder);
        }

        public void RebuildDeckCells(Transform holder)
        {
            for (int i = holder.childCount - 1; i >= 0; i--)
            {
                var ch = holder.GetChild(i);
                if (ch.name == "Viewport") continue;             // 视口节点留着
                Object.DestroyImmediate(ch.gameObject);
            }
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject);
            DeckCells.Clear();

            var prevClip = Clip;
            Clip = DeckViewport;
            int n = CollectionData.DeckCount();
            for (int i = 0; i < n; i++)
            {
                var content = DeckCellRect(i);
                var r = DeckScroll != null ? DeckScroll.Shift(content) : content;
                if (DeckScroll != null && !DeckScroll.Intersects(r)) continue;
                DeckCells.Add(BuildDeckCell(parent, i, r));
            }
            Clip = prevClip;
        }

        /// <summary>一格卡组。版式照 A2：根 250×405、显示 **0.9 倍** ⇒ 内部每个 rect 都 ×0.9。</summary>
        Transform BuildDeckCell(Transform parent, int i, PxRect r)
        {
            var info = CollectionData.DeckAt(i);
            const float K = DeckCellScale;
            var cell = Node(parent, "CollectionDeck_" + i, r);

            Rect(cell, "40K_bt_deck", new PxRect(r.x1 + 2f * K, r.y1 + 17f * K, r.x1 + 248f * K, r.y1 + 385f * K),
                 "Frame", QPageRow);
            // 🔴 **我们挑的**：原版这里放**玩家选的卡背**（233 张还没导）⇒ 用该阵营的**默认卡背**顶着
            var back = CardArt.CardBack(info.Faction);
            if (back != null)
            {
                var q = ImageQuad.Create(cell, back, Local(cell, r.x1 + 11f * K, r.y1 + 26.1f * K,
                                                           r.x1 + 239f * K, r.y1 + 332.1f * K),
                                         LayoutSpace.Px(306f * K), new Vector2(0.5f, 0.5f), "CardBack");
                if (q != null) { q.SetAspect(228f / 306f); q.SetRenderQueue(QPageRow); }
            }
            var nm = Text(cell, info.Name, r.x1 + 20f * K, r.x1 + 230f * K, r.y1 + 344.2f * K, r.y1 + 380f * K,
                          5, PageInk, "Deck Name", 32f * K);
            if (nm != null) nm.SetRenderQueue(QPageText);
            if (!string.IsNullOrEmpty(info.Faction))
                Rect(cell, DeckRuntime.FactionIcon(info.Faction),
                     new PxRect(r.x2 - 84.5f * K - 8f * K, r.y1 + 8f * K, r.x2 - 8f * K, r.y1 + (8f + 85.7f) * K),
                     "Faction", QPageRow, null, true);
            if (i == CollectionData.CurrentIndex())
                Rect(cell, "Highlight_Rounded_Square",
                     new PxRect(r.x1 - 1.4f, r.y1 - 1.4f, r.x1 + 289.8f * K - 1.4f, r.y1 + 427.3f * K - 1.4f),
                     "Highlight Rounded Square", QPageOverlay, new Color(1f, 0.773f, 0f, 1f));

            AddHit(cell, "Hit", r, QPageRow, () => SelectDeck(i));
            return cell;
        }

        /// <summary>点一格卡组：**第一次点 = 选中；再点一下同一个 = 进编辑**（原版是「点开 `Deck info Popup`
        /// → 里面的 `Editar`」，那扇窗本轮没建 ⇒ 用「再点一下」顶着，**出声**）。</summary>
        public void SelectDeck(int i)
        {
            if (i == CollectionData.CurrentIndex()) { EditDeck(i); return; }
            CollectionData.Select(i);
            var holder = transform.Find("Tabs/" + PageNode(0) + "/Deck Scroll View");
            if (holder != null) RebuildDeckCells(holder);
            Debug.Log("[Collection] 选中卡组「" + CollectionData.DeckAt(i).Name + "」"
                      + "（原版这里还开 `Deck info Popup` —— 本轮没建；**再点一下**就进编辑）");
        }

        /// <summary>进编辑：把下标交给 `DeckRuntime`（`CollectionData.PendingEditDeck`），再切到 `DeckEditor` 场景。
        /// ⚠️ **这是相对原版的一处偏离**：原版在同一扇窗里换页；我们的编辑器早已建成**独立场景**
        ///    （197 条自检 + 自带的坐标层），嵌进窗里要重写它的 `Pos/ToPx` ⇒ 先用「切场景」把闭环打通。
        /// ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完）⇒ 自检验的就是「交接下标对不对」。</summary>
        public void EditDeck(int i)
        {
            CollectionData.PendingEditDeck = i;
            Debug.Log("[Collection] 进编辑：「" + CollectionData.DeckAt(i).Name + "」"
                      + "（把下标 " + i + " 交给 `DeckRuntime`；返回时回主菜单场景）");
            if (Application.isBatchMode) { Debug.Log("[Collection] （批处理：不切场景，只交接）"); return; }
            UnityEngine.SceneManagement.SceneManager.LoadScene("DeckEditor");
        }

        public void CreateDeck()
        {
            string name = CollectionData.CreateDeck();
            var holder = transform.Find("Tabs/" + PageNode(0) + "/Deck Scroll View");
            if (holder != null) RebuildDeckCells(holder);
            Debug.Log("[Collection] 新建卡组「" + name + "」（原版走 `Deck Editing Menu`，本轮只建卡组）");
        }

        public string Dump()
        {
            return "Collection：页 " + CurrentTab + " · 卡组 " + CollectionData.DeckCount() + " 套"
                   + " · 当前第 " + (CollectionData.CurrentIndex() + 1) + " 套"
                   + " · 列表滚动 " + (DeckScroll != null ? DeckScroll.Offset.ToString("F0") : "-") + "px"
                   + " · 画出的格 " + DeckCells.Count;
        }
    }

    /// <summary>收藏窗的一页（四页共用这一个组件，按 `_page` 分）。</summary>
    public class CollectionTabPage : WindowTabBase
    {
        CollectionWindow _win;
        Transform _root;
        int _page;

        public override WindowTabType Type
        {
            get
            {
                switch (_page)
                {
                    case 0: return WindowTabType.CollectionDecks;
                    case 1: return WindowTabType.CollectionCards;
                    case 2: return WindowTabType.CollectionCosmetics;
                    default: return WindowTabType.CollectionStyles;
                }
            }
        }

        public void SetHost(CollectionWindow win, Transform root, int page)
        {
            _win = win; _root = root; _page = page;
        }

        public override void Setup()
        {
            if (_win == null || _root == null) return;
            switch (_page)
            {
                case 0:
                    _win.BuildDeckHeader(_root);
                    _win.BuildDeckList(_root);
                    break;
                case 1:
                    _win.BuildCardsPage(_root);
                    break;
                case 2:
                    // ⏭ 下一切片：需要先把 233 张卡背导进工程（正本 §五）
                    _win.NotifyNotBuilt("Cosmetics 页（卡背，卡面文案 `Cosmetics`）");
                    break;
                default:
                    _win.NotifyNotBuilt("Styles 页（异画，卡面文案 `Styles`）");
                    break;
            }
        }
    }
}
