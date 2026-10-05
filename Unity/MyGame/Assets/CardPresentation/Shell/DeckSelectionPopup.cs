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
// 原版有**四个**调用点，**全在别处**（`RankedDeckSelector.OnSelectDeckButtonClick` ·
// `RankedEventWindow.ChangeDeckButtonClick` · `DeckGeneralInfoDemo.ChangePlayerDeckButton` ·
// **`DeckInfoPopup.SelectPracticeOpponentDeck`**），
// 收藏窗那条链**不走它**（收藏窗点格 = 开 `Deck info Popup`，由那里的 `Edit Deck` 进编辑 —— 已核实）。
//   🔴 **2026-10-03 订正**：这里原来写「**三个**调用点」—— 漏了第 4 个 `DeckInfoPopup.SelectPracticeOpponentDeck`
//      （**练习对手**那条链），判据 = `资料/预组卡组_原版规格.md` §五之二 那张 4 行的表。
//      补齐它的意义不只是数数：**那一个入口选的是【对手】卡组**（其余三个都是「换我自己的卡组」）——
//      同一个窗、同一套数据，**语义由调用方定**（`DeckSelectionContext.Callback`）。
// ✅ **我们接的第一条是原版的第三个**：练习窗 → `Show Deck Content`（翻抽屉）→ **`Change Deck`** ⇒ 开本窗。
// ✅ **2026-10-03 接上第二条 = 原版的第四个**：`Deck info Popup` 点 `Practice Deck` ⇒
//    **这一副当中途的「我的卡组」、本窗选出来的那副当【对手】** ⇒ 回调 `DeckInfoPopup.StartPracticeMatch`
//    （判据与订正 → `Shell/DeckInfoPopup.cs` 文件头那段）。本窗**不用改**：它本来就只负责
//    「列出来 → 选一个 → 把选中那个交给 `OnPicked`」。
//
// ============================ 没建的 / 我们挑的（逐条出声）============================
//   · ✅ **2026-09-26：「预组卡组」那一页接上了**（此前恒空）。数据 = `Resources/prebuilt_decks.json`，
//     由 `工具/gen_prebuilt_decks.py` 生成：取 `isPractice==1` 的 **103 副**，卡表与督军按**同一套**
//     t1~t5 匹配规则解成**我们的卡 id**（与 `工具/check_prebuilt_decks.py` 同源，判据只一份）。
//     🔴 **只列经典（30 张）那 29 副**：那一池里 **54 副是遭遇模式（12 张）**，而**我们的对战不支持 12 张那套** ——
//     2026-09-26 用户拍板「**先只列经典（b）**，把补 Skirmish 对战记进计划（a）」。逐条判据
//     → `资料/预组卡组_原版规格.md` §五之四；规格 → `资料/加时与冲突模式_原版规格.md` §二。
//     ⚠️ **难度角标**（原版预组页会画 `easyMark/normalMark/hardMark`）与 **`gameMode` 图标**都**还没画**，
//     原因写在 `PrebuiltDecks.cs` 文件头 —— **是缺口，不是「原版没有」**。
//   · **两个页签的文案是我们定的**：原版两个 `m_text` 都是**空串**、只有 `mTerm`
//     （`MenuDeck/Button/PresetDeck` / `MenuDeck/Button/OwnDeck`），而**本地没有任何术语表/翻译源**
//     （2026-09-24 在 `assets_full` / `extract` / 反编译 / `资料/` 四处搜过，0 命中）⇒ 写 `Prebuilt Decks` / `My Decks`。
//   · `Instructions 2`(act=Y) 的文案**取不到**（它只带 `mTerm`，`m_text` 是空串）；
//     同窗的 `Instructions`(act=N) 有明文 **"Select deck"** ⇒ **我们用它那句**填在 `Instructions 2` 的框里。
//   · ✅ **搜索框不建**（2026-09-26 用户拍板「**按照原版设计**」）：原版出厂 `act=N`，且**四条证据都指向「没有任何代码打开它」**——
//     ① `DeckSelectionPopup` 的 6 个序列化字段里没有它；② `DeckSelectionTabController` **6 个方法里零 `SetActive`**，
//     而且它拿的是 `EverguildInputField` **组件**引用、**够不到容器 GameObject**；
//     ③ `EverguildInputField` 已反编译的 3 个方法也不动 `SetActive`；④ 预制体 `Collection Menu Input Field` 出厂 `act=N`。
//     ⇒ **原版那 4 个节点（底板 / `Search Text` / 点击区）我们一个都不建**，也不再做「按名字过滤」
//     （原版 `Filter` 那条路我们同样够不到 —— 那个输入框是做了但没启用的件）。
//     ⚠️ 反编译覆盖不全 ⇒ 「原版确实一直不显示」是**推断**、不是铁证；但取舍是用户拍的。
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
        // ✅ **2026-10-11（A252）可见性收窄**：这行原是 2026-10-04（A47 接线批）**整行**放宽成 `public` 的；
        //   留 `public` 的两个**各有实测引用**（脚本扫全工程 301 个 `.cs`、剔注释、剔本文件）：
        //   `QDs` 3 处（`Editor/MainMenuScene.cs` 的「弹窗压住整条顶栏」比较 · `CheckAbsorbRule` 的档参）、
        //   `QDsHit` 2 处（`CheckShadeRule` · `CheckAbsorbRule`）；`QDsRow`/`QDsText` **外部引用 = 0** ⇒ 回 `const`。
        //   ⚠️ 另核过：同文件里的另一个类 `DeckPick` 不用它们；`class X : DeckSelectionPopup` 全 0。
        public const int QDs = 3125;        // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（3 处）
        const int QDsRow = 3126;            // 3126 卡组行
        const int QDsText = 3127;           // 3127 行上的字
        public const int QDsHit = 3128;     // ✅ 留 `public`：`Editor/MainMenuScene.cs` 引用（2 处）

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
        /// <summary>`false` = 预组卡组那一页（**原版出厂 `m_IsOn=1` 的就是它**）；`true` = 我的卡组。</summary>
        public bool OwnDecks;
        /// <summary>选中的「我的卡组」下标（−1 = 没选）。</summary>
        public int DeckIndex = -1;
        /// <summary>选中的预组卡组 `deckId`（空 = 没选）。与 `DeckIndex` **互不干扰**（两页各记各的）。</summary>
        public string PrebuiltId = "";
        // 🔴 **2026-09-26 删掉了搜索**（原来有个 `Search` 字段 + 输入框 + 过滤）。
        //    原因：**原版那个搜索框根本没有任何代码打开它** ——
        //    · `DeckSelectionPopup` 的 6 个序列化字段里没有它；
        //    · `DeckSelectionTabController` 对 `searchBar`(+0x40) 只做两件事（`Awake` 挂回调 · `Filter` 读文本），
        //      它 **6 个方法里一处 `SetActive` 都没有**，而且它拿的是 `EverguildInputField` **组件**引用、**够不到容器 GameObject**；
        //    · `EverguildInputField` 已反编译的 3 个方法也不动 `SetActive`；
        //    · 预制体里 `Collection Menu Input Field` 出厂 `act=N`。
        //    ⇒ 用户 2026-09-26 拍板「**按照原版设计**」⇒ 我们不再建它（原版那 4 个节点：输入框 + `Search Text` + 点击区都不建）。
        //    ⚠️ 反编译覆盖不全 ⇒ 「原版确实一直不显示」是**推断**，不是铁证；但四条证据都指向它。

        /// <summary>
        /// 选中一套之后交给外面的东西。
        /// 🔴 **为什么不是一个 `int`**：原版的回调是 `Action&lt;CardDeck>`，而 `CardDeck` **同时覆盖
        /// 「预组」与「玩家自己的卡组」**（`DeckSelectionContext.cs:12`）；我们原来只回传
        /// `CollectionData.DeckInfo`，外面再拿**名字**回查自己的卡组库 —— 预组不在库里
        /// ⇒ `idx = −1`、**静默什么都不发生**（撞「不许静默失败」）。
        /// 见 `资料/预组卡组_原版规格.md` §五之二 末。
        /// </summary>
        public struct DeckPick
        {
            /// <summary>true = 预组卡组（原版资产）· false = 玩家自己的卡组。</summary>
            public bool Prebuilt;
            /// <summary>显示用的那一份（两种都有；预组那份按 `PrebuiltDecks.Deck` 现造）。</summary>
            public CollectionData.DeckInfo Info;
            /// <summary>`Prebuilt == false` 时有效：在 `CollectionData` 里的下标。</summary>
            public int OwnIndex;
            /// <summary>`Prebuilt == true` 时有效：产物里的那一副。</summary>
            public PrebuiltDecks.Deck PrebuiltDeck;
        }

        /// <summary>点一套之后干什么（**关窗 + 回调** —— 原版 `DeckSelectionPopup.Select` 就是这两步）。</summary>
        public System.Action<DeckPick> OnPicked;

        public MenuScroll Scroll;
        /// <summary>画出来的格（自检用）。</summary>
        public readonly List<Transform> Cells = new List<Transform>();

        /// <summary>最近一次开出来的（自检用）。</summary>
        public static DeckSelectionPopup LastOpened;

        public Transform TabHit(bool own) { return Find(transform, "TabHit_" + (own ? "Own" : "Pre")); }
        public Transform CloseHit { get { return Find(transform, "CloseHit"); } }
        public Transform ShadeHit { get { return Find(transform, "BackgroundHit"); } }
        public Transform RandomHit { get { return Find(transform, "RandomHit"); } }
        /// <summary>搜索点击区 —— **恒为 null**（按原版不建搜索框；自检用它盯这一点）。</summary>
        public Transform SearchHit { get { return Find(transform, "SearchHit"); } }
        /// <summary>空态那行字现在显示什么（自检用）。</summary>
        public string EmptyText { get { return _empty; } }
        string _empty = "";
        /// <summary>红底板下沿那行小字现在显示什么（自检用）。**原版没有这一行**，见 `RefreshScopeNote`。</summary>
        public string ScopeText { get { return _scope; } }
        string _scope = "";

        static Transform Find(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        public static DeckSelectionPopup Create(WindowsManager mgr, System.Action<DeckPick> onPicked = null,
                                                int? modeFilter = null)
        {
            var go = new GameObject("Deck Selection Popup with Tabs");
            var win = go.AddComponent<DeckSelectionPopup>();
            win.type = WindowType.Popup;                 // 实证 type=1
            win.placement = WindowsPlacement.Popup;      // 实证 windowsPlacement=15
            win.closeOnEsc = true;                       // 实证 closeOnESC=1
            win.extraScaleSmallScreen = 1f;              // 实证 1.0
            win.Manager = mgr;
            win.OnPicked = onPicked;
            win.ModeFilter = modeFilter;
            WindowsManager.AttachToAnchor(win);
            return win;
        }

        /// <summary>「我的卡组」那一页按**哪个模式**筛（`null` = 不筛）。
        ///
        /// 🆕 2026-09-26：原版就是筛的 —— `DeckSelectionPopup` 的 `TryOpen` 收一个 `DeckSelectionContext`，
        /// 它的筛选 lambda 是「**候选卡组的模式 == 当前卡组的模式** 或 == `context.GameMode`」
        /// （`DeckSelectionPopup___TryOpen_b__10_0.c:10-17`，出处汇总 → `资料/加时与冲突模式_原版规格.md` §2.7）。
        /// ⚠️ **预组那一页不筛** —— 照原版预组页就是**两种模式混在一页**列出来的（66 副），
        ///    点错模式那一副由 `LiveOpsEventWindow.SelectedDeckFitsMode` 在开战前挡（原版同款校验）。</summary>
        public int? ModeFilter;

        public override void Open()
        {
            // ✅ **2026-09-26 归位**：原版 `DeckSelectionTabController.Start()` 唯一一句就是
            // `ShowPrebuiltDecks(true)`（`DeckSelectionTabController__Start.c:5`）⇒ **起手落在「预组卡组」页**。
            // 我们原来写死 `OwnDecks = true`，那是因为预组那一页当时没数据（文件头那条偏离）—— **现在数据接上了，回退偏离**。
            OwnDecks = false;
            DeckIndex = FirstOwnDeckInMode(CollectionData.CurrentIndex());
            PrebuiltId = "";
            Build();
            LastOpened = this;
        }

        /// <summary>「我的卡组」里**符合本窗模式**的第一套（从 <paramref name="prefer"/> 起绕一圈）。
        /// 一套都没有 ⇒ 保持原样（那一页会显示空态）。</summary>
        public int FirstOwnDeckInMode(int prefer)
        {
            int n = CollectionData.DeckCount();
            if (n <= 0) return 0;
            for (int k = 0; k < n; k++)
            {
                int i = ((prefer < 0 ? 0 : prefer) + k) % n;
                var d = CollectionData.Raw(i);
                if (d != null && (!ModeFilter.HasValue || d.GameMode == ModeFilter.Value)) return i;
            }
            return (prefer >= 0 && prefer < n) ? prefer : 0;
        }

        // ============================================================ 列表数据

        /// <summary>当前页签要列出来的条目。
        /// ⚠️ **没有搜索过滤**了 —— 原版那个搜索框没有任何代码打开它，我们照原版不建（见 `Search` 那段的注释）。</summary>
        public List<DeckPick> Shown()
        {
            var list = new List<DeckPick>();
            if (OwnDecks)
            {
                for (int i = 0; i < CollectionData.DeckCount(); i++)
                {
                    // 🆕 2026-09-26：**按模式筛**（原版 `DeckSelectionPopup.__TryOpen_b__10_0` 那条 lambda）
                    if (ModeFilter.HasValue && CollectionData.DeckAt(i).GameMode != ModeFilter.Value) continue;
                    list.Add(new DeckPick { Prebuilt = false, Info = CollectionData.DeckAt(i), OwnIndex = i, PrebuiltDeck = null });
                }
            }
            else
            {
                foreach (var d in PrebuiltDecks.Tab)
                    list.Add(new DeckPick { Prebuilt = true, Info = InfoOf(d), OwnIndex = -1, PrebuiltDeck = d });
            }
            return list;
        }

        /// <summary>把一副预组卡组包成格子要的 `DeckInfo`（`MenuDraw.DeckCell` 只吃 Name/Faction/CardbackId 三个字段）。</summary>
        public static CollectionData.DeckInfo InfoOf(PrebuiltDecks.Deck d)
        {
            return new CollectionData.DeckInfo
            {
                Name = d.DisplayName,
                WarlordId = d.heroId,
                Faction = d.faction,
                Count = d.cardIds != null ? d.cardIds.Length : 0,
                CardbackId = d.cardback,
            };
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
            // 🔴 **2026-10-04（A47 接线批）订正档号：`QDsHit − 1`(3127) → `QDs`(3125)** ——
            //   规矩 = 「压暗层的命中区落在**压暗层自己那一档**，且严格低于本窗任何内容命中区档」
            //   （判据 → `MenuDraw.ShadeHit` 的注释 · `资料/待办判据_阶段二与联机.md` §A25·补（一））。
            //   ⚠️ 3127 恰好是 `QDsText`（文字那一档）。
            MenuDraw.ShadeHit(root, new PxRect(0f, 0f, 1920f, 1080f), QDs, QDsHit, () => Close(), "BackgroundHit");

            // 2) 两个页签（HLG spacing 12.45 ⇒ 第 2 个从 196.30 + 260 + 12.45 起）
            //    容器高 **68.50**、键高 **67.6421** ⇒ 竖直居中（`align=MiddleLeft`）
            float ty1 = TabT + (68.50f - TabH) * 0.5f, ty2 = ty1 + TabH;
            BuildTab(root, "Prebuilt Decks", "40K_tab_button", TabL, ty1, ty2, false);
            BuildTab(root, "My Decks", "40K_tab_button_overwindow", TabL + TabW + TabGap, ty1, ty2, true);

            // 3) 红底板
            MenuDraw.Nine(root, CardArt.MenuUi("UI_Deck_Information_Back"), new PxRect(RedL, RedT, RedR, RedB),
                          RedBorder, RedTexW, RedTexH, QDs, null, true);
            // 🆕 **2026-10-06（A94）：红底板吸收点击**。判据 = 原版 prefab
            //   `Deck Selection Popup with Tabs > Generic Window Red Background Big` 那颗 `Image` 的
            //   **`m_RaycastTarget = 1`**（2026-10-06 `rayscan` 实读）—— 射线打到它自己、
            //   父链上没有点击处理器（关窗那颗 `BackgroundCloseButton` 在压暗层上）⇒ 原版**什么都不做**。
            MenuDraw.Absorb(root, "AbsorbHit", new PxRect(RedL, RedT, RedR, RedB), QDs, QDsHit);

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
                var randomBg = MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Mulligan"), rr, "Random Bg", QDsRow);
                MenuDraw.Text(root, rr, "Random", Color.white, "Random Text", 45f, QDsText);
                // A17：原版 `…>Practice buttons>Generic Simplified UI Button` 是 SpriteSwap（普查 §块 3 第 9 行）；
                // 底下那颗 `EverguildButton`(trans=1) 不换图 —— 换图的是 `Button` 那颗
                Hit(root, "RandomHit", rr, PickRandom, QDsHit, randomBg, "UI_Button_Mulligan");
            }

            // 5) ❌ **搜索框不建**（按原版：预制体里 `Collection Menu Input Field` 出厂 `act=N`，
            //    而且**没有任何代码打开它** —— 四条证据见 `Search` 那段注释）。
            //    原版那 4 个节点（输入框底板 / `Search Text` / 点击区）我们**一个都不建**。

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

            // 7b) 我们自己加的一行小字：**如实说明这一页列了多少、藏了多少**（原版没有这一行）。
            //     放在红底板下沿那条空档（视口底 986.69 → 底板底 1032），不占原版任何件的位置。
            RefreshScopeNote();
            MenuDraw.Text(root, new PxRect(DdL, SvB + 6f, DdR, RedB - 6f), _scope,
                          new Color(0.62f, 0.62f, 0.62f, 1f), "Scope Note", 22f, QDsText);

            // 8) 关闭圆钮
            // 🔴 **2026-10-03 补一层（A17 顺带查出的真偏离）**：原版 `Generic Close Button Orange` 是
            //    **三层**（圆底 → **黄面 `40k_general_bt_yellow`** → 关闭图标），我们这里**漏了中间那层**
            //    （同族 `DeckInfoPopup` / `PlayerProfileWindow` 都有）。⚠️ **换图的目标是【圆底那一层】**（见下面 `Hit` 那条），
            //    ⚠️ 同时把图标提到更高一档队列 —— 原来底色与图标**同一个 `QDsRow`**（谁盖谁不可控，
            //    见 `资料/已知的坑.md`「同一个渲染队列的两层」）。
            var closeBase = MenuDraw.Rect(root, CardArt.MenuUi("UI_Button_Round_background"),
                          new PxRect(CloseL, CloseT, CloseR, CloseB), "Close Bg", QDsRow, null, true);
            float ix = CloseL + (CloseR - CloseL - CloseIw) * 0.5f;
            float iy = CloseT + (CloseB - CloseT - CloseIh) * 0.5f;
            var closeFace = MenuDraw.Rect(root, CardArt.MenuUi("40k_general_bt_yellow"),
                          new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), "Close Face", QDsRow + 1, null, true);
            MenuDraw.Rect(root, CardArt.MenuUi("40k_general_bt_yellow_close"),
                          new PxRect(ix, iy, ix + CloseIw, iy + CloseIh), "Close Icon", QDsRow + 2, null, true);
            // 🔴 换图落在**圆底那一层**（原版 `Deck Selection Popup with Tabs>Generic Close Button Orange` 三层 =
            //    圆底 `UI_Button_Round_background` + `Background` 黄面 + `Icon`；`trans=2` 换的是它自己的 Image，
            //    HL = `40k_general_bt_yellow_hover`。2026-10-03 直接读 prefab 核过）
            Hit(root, "CloseHit", new PxRect(CloseL, CloseT, CloseR, CloseB), () => Close(), QDsHit,
                closeBase, null, "40k_general_bt_yellow_hover");

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

        // ❌ **`BeginTyping` / `RefreshSearchText` 已删**（2026-09-26）—— 搜索框不建了，见 `Search` 那段注释与 `Build()` 第 5 步。
        //    原版的 `Filter`（按卡组名过滤）那条路因此在我们这儿不会触发；**这是照原版的**（原版也够不到那个输入框）。

        /// <summary>抽一套（原版 `RandomizeDeck`：从当前列表随机挑 ⇒ **选中并关窗**）。</summary>
        public void PickRandom()
        {
            var shown = Shown();
            if (shown.Count == 0) { Debug.Log("[DeckSel] 当前列表是空的，没得抽"); return; }
            Pick(shown[Random.Range(0, shown.Count)]);
        }

        /// <summary>选一套：**关窗 + 回调**（= 原版 `DeckSelectionPopup.Select` 的两步）。</summary>
        public void Pick(DeckPick pick)
        {
            if (pick.Prebuilt)
            {
                PrebuiltId = pick.PrebuiltDeck != null ? pick.PrebuiltDeck.deckId : "";
                Debug.Log("[DeckSel] 选中预组：「" + pick.Info.Name + "」(" + PrebuiltId + ") ⇒ 关窗 + 回调");
            }
            else
            {
                DeckIndex = pick.OwnIndex;
                Debug.Log("[DeckSel] 选中：「" + pick.Info.Name + "」⇒ 关窗 + 回调");
            }
            if (OnPicked != null) OnPicked(pick);
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
            // 🔴 **2026-10-11（A198③）**：本窗此前**从不设 `Clip`**（格子走裸重载、自己把 `SvRect` 当 `clip` 传）。
            //   改走 `MenuDraw.DeckCell(GameWindow, …)` 之后，「裁哪一块」由**本窗的 `Clip`** 说了算
            //   （= 原版模型：mask 挂在**视口节点**上）⇒ 这里按 `CollectionWindow.RebuildDeckCells` 的同一形状
            //   把**视口**放进去、循环结束**立刻还原**（后面 `RefreshEmptyNote` 的 `MenuDraw.Text` 不该被裁）。
            //   ⚠️ **零行为变化**：本窗 `ClipPad` 从未被设过（= 0），旧写法那个 `maskPad` 缺省也是 0 ⇒ 四个输入两两相同。
            var prevClip = Clip;
            Clip = SvRect;
            for (int k = 0; k < shown.Count; k++)
            {
                var r = Scroll.Shift(CellRect(k));
                if (!Scroll.Intersects(r)) continue;
                var pick = shown[k];   // ⚠️ 在循环体内声明 ⇒ 闭包每轮各拿一份（别提到循环外）
                bool sel = pick.Prebuilt
                    ? (pick.PrebuiltDeck != null && pick.PrebuiltDeck.deckId == PrebuiltId)
                    : (pick.OwnIndex == DeckIndex);
                string cellName = pick.Prebuilt
                    ? "DeckSel_Pre_" + (pick.PrebuiltDeck != null ? pick.PrebuiltDeck.deckId : "?")
                    : "DeckSel_" + pick.OwnIndex;
                // 🆕 **预组格多两层**（原版 `<Deck>` 下本来就有）：
                //   ① **模式图标** —— 喂 `PrebuiltDeck.gameMode`（`DeckDrawer.Draw` 那条）
                //   ② **难度角标** —— 只在**预组页**显示：原版 `DeckCollectionDisplay.displayDifficultyLabel`
                //      在 `ShowPrebuiltDecks` 里置 1、`ShowOwnDecks` 里置 0（`DeckSelectionTabController__*.c`）
                bool pre = pick.Prebuilt && pick.PrebuiltDeck != null;
                Cells.Add(MenuDraw.DeckCell(this, parent, cellName, r, pick.Info, sel,
                                            QDsRow, QDsText, QDsText + 1, QDsHit,
                                            () => Pick(pick),
                                            pre ? (int?)pick.PrebuiltDeck.gameMode : null,
                                            pre ? (int?)pick.PrebuiltDeck.difficulty : null,
                                            showDifficulty: !OwnDecks));
            }
            Clip = prevClip;
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
            if (Shown().Count > 0) { _empty = ""; return; }
            if (!PrebuiltDecks.Available)
                _empty = "预组卡组的数据读不到（Resources/prebuilt_decks.json）⇒ 先如实留空；"
                       + "跑 `python 工具/gen_prebuilt_decks.py` 重新生成";
            else if (!OwnDecks && PrebuiltDecks.Tab.Count == 0)
                _empty = "这一页一副可用的都没有（**拼不齐的按原版口径整副不显示**）";
            else
                _empty = "没有可选的卡组";   // ⚠️ 原来这里会说「搜索串：…」—— 搜索框已按原版去掉（见 `Search` 那段注释）
        }

        /// <summary>
        /// 红底板下沿那一行小字：**这一页列了什么、藏了什么**。
        /// 🔴 **这是原版没有的一行**（原版不筛模式、也照原版那样不解释）。加它是因为
        /// 我们**只列经典**、而且**拼不齐的整副不显示** —— 两件都会让玩家觉得「牌少了」，
        /// 所以如实出声（见文件头与 `资料/预组卡组_原版规格.md` §五之四）。
        /// </summary>
        void RefreshScopeNote()
        {
            if (OwnDecks) { _scope = ""; return; }
            if (!PrebuiltDecks.Available) { _scope = "预组数据读不到（重跑 工具/gen_prebuilt_decks.py）"; return; }
            int classic, skirm; PrebuiltDecks.CountByMode(out classic, out skirm);
            int dropped = PrebuiltDecks.NotListed;
            var sb = new System.Text.StringBuilder();
            // 🔴 这行字要**如实**：列出来的是两类模式**混在一页**（2026-09-26 起照原版全列），
            //    经典 30 张 / 遭遇 12 张 —— 靠每格右下角的模式图标区分，这里先说清各有多少。
            sb.Append("预组共 ").Append(PrebuiltDecks.All.Count).Append(" 副 · 本页列 ")
              .Append(PrebuiltDecks.Tab.Count).Append(" 副（经典 ").Append(classic)
              .Append(" · 遭遇 ").Append(skirm).Append("，按原版难度序）");
            if (dropped > 0) sb.Append(" · ").Append(dropped).Append(" 副我们卡池拼不齐（不显示）");
            _scope = sb.ToString();
        }

        // ============================================================ 小工具

        Transform Hit(Transform parent, string name, PxRect r, System.Action onClick, int q = QDsHit,
                      ImageQuad target = null, string art = null, string hoverArt = null)
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
            if (target != null) wb.Bind(target, art, hoverArt);
            return hit;
        }
    }
}
