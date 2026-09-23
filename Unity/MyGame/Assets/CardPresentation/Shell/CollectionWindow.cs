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
// ============================ 建了什么 / 没建什么（出声） ============================
//   ✅ 外壳 + 四页签 · **Deck 页**（页头 + 卡组列表，6 列 × 225×364.5，可纵向滚）
//   ✅ **Cards 页**：页头 + 万能卡计数条 + 卡池网格（6 列 × 262.5×384）+ **左侧完整筛选栏**
//      （7 行：搜索框 / Owned / Upgradable / Army 13 档 / Rarity 5 档 / Cost 8 档 / Type 3 档）
//   ✅ **Cosmetics 页**（卡背）：页头 + **233 张卡背网格**（列数**按宽度算** = 7 列 · 250×405）+ 左抽屉（起手收起）
//   ⏭ **Styles 页**（异画）：仍只有占位 + `NotifyNotBuilt`
//      · ⚠️ **2026-09-24 更正**：**本地有 7 张督军异画**（`assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/`，
//        文件名是 **`AA_HB_…` / `…_AA_HB.png`**，另有 `DarkAngels_AA_warlord_Azrael_v2`）+ 卡背 6 + `UI_Deck_Warlord` 6 + 头像 4。
//        上一版这里写「远程包、本机零副本、卡表 0 条」—— **错因是普查搜的词**（`alternate`/`variant`/`skin`）
//        **一个都不命中 `AA_HB` 这种命名**，是用户拿文件名来问才发现的。
//      · 仍在远程的：`alternateartstyles` 那个 CCD 包（本机从未下载）。
//        **判据**（自己解的 `catalog_main.json`）：label `alternateArt` 挂 **4 条** entry —— ⚠️ 上一版写的「4022」是转述来的错数，已删。
//        ⇒ **本页可以「先建骨架 + 只填这 7 张」**，但要说清只覆盖 `AA_HB` / `v2` 两种风格。
//        详见 `资料/阶段二_卡组线_原版规格.md` §七 ③b 与 `资料/阶段二_战斗入口_原版规格.md`。
//   ✅ **`Deck info Popup`**（2026-09-23）：点一格卡组 ⇒ 选中 + 开它；窗里 `Edit Deck` 才进编辑
//      ⇒ **`SelectDeck` 里那条「再点一下 = 进编辑」的顶替路已撤**（原版那条路有了）
//   ✅ **`Import Deck Popup`**（2026-09-23）：接上 Deck 页那个 `Import` 钮（此前点了只报「没实现」）
//   ⏭ **`Deck Selection Popup with Tabs`**（三个弹窗里最后一个）
//   ⏭ 卡片详情窗（参数已于 2026-09-23 普查完，够建 80%；见 `项目任务.md` §三 第 15 条）
//
// ---- Cosmetics 页**没做**的（逐条出声）----
//   · `Cardback Shadow SDF`（原版悬停高亮那层）：预设里 `sprite=0`（运行时喂）⇒ **我们不确定该喂哪张，不画**
//     ⚠️ 别写成「本地没有」——`Cardback_*_SDF` 那批**实测是在的**（见 `资料/战场还原度_差距清单_0917.md:66`）；没画是因为**没查清喂法**。
//   · 抽屉里的 `Army Filter`：A4 只给了容器 rect 与「→ Title + Content(HLG) → Toggle×N」，**没给格子尺寸** ⇒ 没建
//   · `Empty Collection Warning`（`act=F`）⇒ 照纪律不建
//
// ---- 筛选栏**没做**的（逐条出声）----
//   · **滑动动画**：原版 `hiddenPosition=(-550,0)` + `animationTime=0.3`；我们是整块显隐
//   · **`Owned only` 恒真**（单机全解锁）· **`Upgradable only` 没有升级系统** —— 与卡组编辑同一口径
//   · **`Type` 只有 3 档**（原版 `CardTypeOptions` 就是这样）⇒ **我们的防御卡筛不到**，照原版不加第 4 档
//   · 原版的 `Viewport` 比屏幕长（1150.94 > 1080）—— 我们按屏幕可见的 924.1 做视口，多出来的靠滚动
//
// ---- 🔴 我们挑的（原版取不到，逐条出声）----
//   · **卡组格里的图**：原版是**玩家选的卡背**（`CollectionManager` 的 cosmetic）。
//     ✅ **233 张卡背 2026-09-23 已导进工程**（`Resources/Art/cardbacks/`，`CardArt.Cosmetic(name)` 取），
//     但**「玩家选哪张」这件事我们还没有数据源/入口**（那正是卡组编辑 `Cosmetics` 页的活，见 `项目任务.md` §三 第 15 条 第 46 行）
//     ⇒ 本轮仍用 `CardArt.CardBack(阵营)`（**该阵营的默认卡背**）顶着。
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
        /// <summary>筛选栏的层**在整页之上**（原版兄弟序里 `Card Filters` 排在 `Collection Display` **之后** ⇒ 压在卡池上）。
        /// ⚠️ 队列要**高过卡池那一整片**（`CardView` 的层走材质默认 3000、页底板 3030）—— 用 3040 段。</summary>
        public const int QFlt = 3040, QFltRow = 3041, QFltText = 3042, QFltHit = 3043;

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

        // ============================================================ Cards 页的筛选栏（A3 §五·1 + 本工程实读）
        //
        // 原版 = `CardsTab` 里的 `Card Filters` 面板（组件 `CollectionFilterController<RawCardScript>`）。
        // 逐条实读（本机 `工具/menu_dump.py` / `menu_rect.py bundle_menus_assets_all -8460121208602172715`）：
        //   · 面板 **0.25,155.9 → 335.56,1080**，图 `40k_main_tab_background`（**Simple**，图本身只有 185×4 ⇒ 拉伸）
        //   · 面板自己的 `Shadow`（`40k_main_tab_shadow`，**Simple**，col α **0.314**）宽 **152.82**，兄弟序**在面板之前**
        //   · 面板里 = `Scroll View`(sens **50**) → `Viewport`(`UIMask` + Mask showGraphic=0)
        //     → `Filters`(**VerticalLayoutGroup** sp0 pad0) → **7 行**
        //   · `filters[6]` 是后 6 行；第 1 行是搜索框（`CardNameFilter` + `EverguildInputField`）
        //   · 收起时整栏滑到 `hiddenPosition = (-550, 0)`，`animationTime = 0.3`
        //
        // 🔴 **选项表是从 MB 实读的，别按枚举直觉编**（`FilterOptions` 列表就在各 `Card*Filter` 的
        //    `options` 字段里 —— 见文件头出处）：
        //      Army **13** 档（alt 空 ⇒ 走 Localize，我们直接印阵营名）
        //      Rarity **5** 档（alt = `Common/Rare/Epic/Legendary/Special`）
        //      Cost  **8** 档（alt = `1-` / `2`…`7` / `8+` —— **是区间，不是「每个费用一格」**）
        //      Type  **3** 档（`Warlord` / `Troops` / `Stratagem`；
        //             `CardTypeOptions { Minion=0, Hero=10, Tactic=20, Whispers=40 }`，`ShowOnlyHero` 比的就是 `== 10`）
        //   ⇒ 30 个选项的 `background` 图**全是运行时赋的**（预设里 `sprite=0`），`checkMark` **30/30 全是 null**
        //   ⇒ **选中态没有对勾图，靠 `EverguildToggle` 的 tint** —— 我们用
        //      `on = 白(1,1,1,1)` / `off = 灰(0.349,0.341,0.341,1)`。
        //      ⚠️ **这两个颜色是「同 bundle 里那对重复出现的 toggle 预制值」，没证明就是采集筛选那一支**
        //         （`togglePrefab` 的 pid 在本 bundle 里找不到根 GO，见文件头）。**如实标成我们的取法。**
        //
        // 🔴 **两处「一个值 ≠ 全部情况」**（铁律 5·c）：
        //   ① 行高/标题位是**布局组跑之后**的值（`Filters` 高 **989.02** = 7 行之和），不是模板位；
        //   ② 面板**比屏幕长**：预设里视口一直画到 y=1150.94，而 `Content Area` 只到 1080
        //      ⇒ 我们按**屏幕可见的 924.1** 做视口，于是内容 989.02 **可滚 64.92**（Type 行靠滚动够得着）。
        //      ⚠️ **这条是我们的判读，不是实读** —— 原版运行时到底是「滚」还是「就那么画到屏幕外」，
        //         **还没跑实况核过**（记在 `项目任务.md` §三 第 15 条）。
        public const float FltL = 0.25f, FltT = 155.9f, FltW = 335.31f;
        public const float FltShadowW = 152.82f;
        /// <summary>面板**可见**高（屏幕底裁掉）—— 与卡组编辑那条筛选栏同一个数（`DeckRuntime.FltH = 924.1`）。</summary>
        public const float FltViewH = 924.1f;
        /// <summary>`Filters`（VLG）内容高 = 7 行之和，**实读**。</summary>
        public const float FltContentH = 989.02f;
        public const float FltHiddenDx = -550f;   // 原版 `hiddenPosition = (-550, 0)`
        public const float FltAnimTime = 0.3f;    // 原版 `animationTime`（**我们没做滑动动画**，见类头「没建」）
        public static readonly PxRect FltView = new PxRect(FltL, FltT, FltL + FltW, FltT + FltViewH);

        // 7 行的**面板内**顶边（`menu_rect` 实读：逐行相加 0 / 79.02 / 129.02 / 179.02 / 329.02 / 609.02 / 839.02）
        const float FR_Name = 0f, FR_Owned = 79.02f, FR_Upgr = 129.02f, FR_Army = 179.02f;
        const float FR_Rarity = 329.02f, FR_Cost = 609.02f, FR_Type = 839.02f;
        // 搜索框内件（面板内坐标）
        const float FR_InputX1 = 27.01f, FR_InputX2 = 308.29f, FR_InputY1 = 19.51f, FR_InputY2 = 59.51f;
        // Owned / Upgradable 两个开关（同一套内件，只差行顶边）
        const float FR_TogIconX1 = 239.71f, FR_TogIconX2 = 310.30f;     // 70.59×50、preserveAspect
        const float FR_TogLabX1 = 25.0f, FR_TogLabX2 = 234.71f;
        // 三档格子的几何（cell / spacing / pad，出处 A3 §3·6 的 GridLayoutGroup 字段原文）
        const float FltGridPadL = 14f, FltGridSpX = 7f;      // Army / Rarity：cell 100×100
        const float FltCostPadL = 15f, FltCostSpX = 15f, FltCostSpY = 20f;   // Cost：cell 65×65

        /// <summary>筛选栏**一格**（原版 `EverguildToggle` + `CollectionFilterToggle`）。
        /// 坐标一律是**面板内**（未减滚动量）—— 见 `RebuildFilterRows`。</summary>
        struct FltCell
        {
            public PxRect R;        // 格 = 点击区
            public PxRect Bg;       // `Background` 那张图（Rarity 比格小：格 100²、图 **50²** 居中）
            public string Icon;
            public PxRect Lab;      // 格内小字（Army 行**没有**）
            public string Label;
            public float LabelPx;   // 小字字号（原版 px）
            public float LabelAutoMin; // 原版 `auto(min-max)` 的 min（0 = 不开自适应）
            public bool LabelRight; // 原版这几行 `Label` 是 hAlign=Right
            public string Key;      // 点了改哪一项
            public bool On;
        }
        readonly List<FltCell> _fltCells = new List<FltCell>();
        Transform _fltPanel;
        bool _fltOpen;
        MenuScroll _fltScroll;
        /// <summary>万能卡计数条那 4 个数字（筛选变了要重算）。⚠️ 语义**与原生不同** —— 见 `项目任务.md` §三 第 15 条 第 29 项。</summary>
        readonly Label[] _wcCount = new Label[4];

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
            // ⚠️ 2026-09-23 顺手修两处（A3 §5·2 原文）：原版这套按钮是
            //    **[圆钮 50×50]** → 里面 `icon detail` 只有 **30×30**（`sd=(-20,-20)` = 四边各内缩 10、preserveAspect）
            //    · `label` 在 **[437.2, y, 150, 50]**；我们原来是「图拉满 50×50」+「label 从 427.2 起」。
            Rect(page, "40k_menu_bt", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Button", QPagePanel);
            Rect(page, "40k_bt_icon_search", new PxRect(FltBtnX + 10f, FltBtnY + 10f, FltBtnX + 40f, FltBtnY + 40f),
                 "Filters Icon", QPageRow, null, true);
            var fltLab = Text(page, "Filters", 437.2f, 587.2f, FltBtnY, FltBtnY + FltBtnS,
                              5, PageInk, "Filters Label", 42f);
            if (fltLab != null) fltLab.SetRenderQueue(QPageText);
            AddHit(page, "FiltersHit", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS + 220f, FltBtnY + FltBtnS), QPageRow,
                   () => ToggleFilters());
            Rect(page, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            var clrLab = Text(page, "Clear filters", ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", 42f);
            if (clrLab != null) clrLab.SetRenderQueue(QPageText);
            AddHit(page, "ClearFiltersHit",
                   new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH), QPageRow,
                   () => ClearCardFilters());

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
                if (t != null) { t.SetRenderQueue(QPageText); _wcCount[i] = t; }
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

            BuildCardFilters(page);        // 左侧筛选栏（**在卡池之后建** ⇒ 兄弟序在原版里也是它靠后 = 画在卡池之上）
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
            var page = PageRoot(1);
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

        // ============================================================ Cosmetics 页（卡背；A4 §二）
        //
        // 原版 = `Cardback Tab`（脚本 `CardbackCollectionTab`）→ `Cardback Display`（`CardbackCollectionDisplay`）。
        // 逐条实读（A4 §二 + §2·1）：
        //   · 页头 `Header` 0,0→1920,85：`Filter Toggle`(200,17.5,50²) `40k_menu_bt` ·
        //     `label`("Filters") 270,17.5→420,67.5 **fs35**(auto 10-35) · `icon detail` 210,27.5,30² ·
        //     `Separator Line` 0,80→1920,90 `40k_main_line` ·
        //     `label`("Your cosmetics collection") 右缘 1821、y 10..70 **fs38** ·
        //     `Clear Filter Button` 1405,12.5→1655,72.5 `UI_Button_Mulligan` + 字 "Clear filters" **fs33**(auto 10-33) hRight
        //     ⚠️ **字号是「每页自己的实例」值**：本页 Filters=**35** / Clear=**33**，异画页同名两处是 **42** —— 不许拿一个数当全局
        //   · `Scroll View` 168.27,85→1920,1080（**1751.73 × 995**）+ `Viewport`(`UIMask`+`Mask showGraphic=0`)
        //   · 网格：`_cellWidth=250` `_cellHeight=405` spacing 0 ⇒
        //     🔴 **列数是【按宽度算】的 = `floor(1751.73 ÷ 250)` = 7**，不是 `_segments=5`
        //     （`_controlSegmentSize=1` ⇒ `ConfigureColumnNumber` 每帧按宽度覆盖 `_segments`；
        //      A4 §2·1 原来写「5 列是定值」**已就地更正** —— 证据同 A3 §3·5）
        //   · 一格 = `Collection Cosmetic` 250×405，**只有两层图**：`Cardback`(铺满) + `Cardback Shadow SDF`
        //     （后者 `sprite=0` 运行时喂）—— **没有卡名/费用/文字**
        //   · 左抽屉 `Cosmetic FIlter`（0.05,85→335.55,1080）**出厂 act=F**
        //
        // ---- 没建的（出声）----
        //   · `Cardback Shadow SDF`（原版悬停高亮那层）：**SDF 图本地没有**（`sprite=0` 运行时喂）⇒ 不画
        //   · 抽屉里的 `Army Filter`：A4 只给了容器 rect（335.5×345）与「→ Title + Content(HLG) → Toggle×N」，
        //     **没给格子的尺寸** ⇒ **没建**（要建得先补一次普查）
        //   · `Empty Collection Warning`（`act=F`）⇒ 照纪律不建
        public const float CosmoL = 168.27f, CosmoT = 85f, CosmoR = 1920f, CosmoB = 1080f;
        public const float CosmoCellW = 250f, CosmoCellH = 405f;
        /// <summary>列数 = `floor(视口宽 ÷ 格宽)`（**算出来的，不是 `_segments`**）。</summary>
        public static int CosmoCols { get { return Mathf.Max(1, Mathf.FloorToInt((CosmoR - CosmoL) / CosmoCellW)); } }
        public static readonly PxRect CosmoView = new PxRect(CosmoL, CosmoT, CosmoR, CosmoB);

        public static PxRect CosmoCellRect(int i)
        {
            int r = i / CosmoCols, c = i % CosmoCols;
            float x = CosmoView.x1 + c * CosmoCellW;
            float y = CosmoView.y1 + r * CosmoCellH;
            return new PxRect(x, y, x + CosmoCellW, y + CosmoCellH);
        }

        /// <summary>卡背那一页的滚动区 / 画出来的格（自检用）。</summary>
        public MenuScroll CosmoScroll;
        public readonly List<Transform> CosmoCells = new List<Transform>();
        /// <summary>卡背总张数（自检用；应是 **233**）。</summary>
        public static int CosmoTotal { get { return CardArt.CosmeticNames().Length; } }

        public void BuildCosmeticsPage(Transform page)
        {
            // ---- 页头（A4 §二 `/Header`）----
            Rect(page, "40k_menu_bt", new PxRect(200f, 17.5f, 250f, 67.5f), "Filters Button", QPagePanel);
            Rect(page, "40k_bt_icon_search", new PxRect(210f, 27.5f, 240f, 57.5f), "Filters Icon", QPageRow, null, true);
            var flt = Text(page, "Filters", 270f, 420f, 17.5f, 67.5f, 5, PageInk, "Filters Label", 35f);
            if (flt != null) flt.SetRenderQueue(QPageText);
            AddHit(page, "FiltersHit", new PxRect(200f, 17.5f, 420f, 67.5f), QPageRow, () => ToggleCosmoFilters());

            Rect(page, "40k_main_line", new PxRect(0f, 80f, 1920f, 90f), "Separator Line", QPageRow);
            // ⚠️ 标题那条 rect 宽是 **0**（实测 `sd=(0,60)`、`m_HorizontalAlignment=4` = **Right**）
            //    ⇒ **右对齐到 1821** 才是它的真值（A4 表里写「hFlush」**是错的**，已就地更正）。
            var title = Text(page, "Your cosmetics collection", 1200f, 1821f, 10f, 70f, 5, PageInk, "Header Label", 38f);
            if (title != null) { title.SetRenderQueue(QPageText); title.AlignRightOn(LayoutSpace.FromPixel(1821f, 0f).x); }

            // 🔴 **`Clear filters` 的 x 不能照抄 dump 的 1405** —— 那是**布局组跑之前的模板位**
            //    （`Header/Filters` 是 **425..1385 宽 960 的容器 + VLG spacing=(5,0) align=3 MiddleLeft**；
            //     子件 `pos.x=20` 是相对容器**右**锚点的，落在容器外）。
            //    ⚠️ 照抄 1405 的代价**实拍一眼可见**：它和右对齐到 1821 的标题**叠在一起**
            //      （截图 `04_收藏_Cosmetics.png` 第一版就是 `Your co…llection` 压在 `Clear filters` 上）。
            //    真值 = **容器左缘 425 起**（align=3），竖直居中 ⇒ y 仍是 12.5（与 dump 的 y 一致，可互证）。
            Rect(page, "UI_Button_Mulligan", new PxRect(425f, 12.5f, 675f, 72.5f), "Clear filters", QPageRow);
            var clr = Text(page, "Clear filters", 425f, 675f, 12.5f, 72.5f, 5, PageInk, "Clear filters Text", 33f);
            if (clr != null) { clr.SetRenderQueue(QPageText); clr.AlignRightOn(LayoutSpace.FromPixel(675f, 0f).x); }
            AddHit(page, "ClearFiltersHit", new PxRect(425f, 12.5f, 675f, 72.5f), QPageRow, () => ClearCosmoFilters());

            // ---- 卡背网格 ----
            var holder = Node(page, "Scroll View", CosmoView);
            Node(holder, "Viewport", CosmoView);
            int n = CosmoTotal;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)CosmoCols));
            CosmoScroll = MenuScroll.TopAligned(CosmoView, rows * CosmoCellH);
            CosmoScroll.Owner = gameObject;
            CosmoScroll.OnChanged = () => RebuildCosmoCells(holder);
            PointerLayer.RegisterScroll(CosmoScroll);
            RebuildCosmoCells(holder);

            BuildCosmoDrawer(page);
        }

        void RebuildCosmoCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            CosmoCells.Clear();

            var names = CardArt.CosmeticNames();
            var prevClip = Clip;
            Clip = CosmoView;
            for (int i = 0; i < names.Length; i++)
            {
                var r = CosmoScroll.Shift(CosmoCellRect(i));
                if (!CosmoScroll.Intersects(r)) continue;
                var cell = Node(parent, "CollectionCosmetic_" + i, r);
                // 一格只有两层：底 = SDF 阴影（**本地没有 SDF 图** ⇒ 不画，出声）、面 = 卡背
                var tex = CardArt.Cosmetic(names[i]);
                if (tex != null)
                {
                    var q = ImageQuad.Create(cell, tex, Local(cell, r.x1, r.y1, r.x2, r.y2),
                                             LayoutSpace.Px(r.H), new Vector2(0.5f, 0.5f), "Cardback");
                    if (q != null) { q.SetAspect(r.W / r.H); q.SetRenderQueue(QPageRow); }
                }
                else
                {
                    Debug.LogWarning("[Collection] 卡背取不到：" + names[i]);
                }
                AddHit(cell, "Hit", r, QPageRow, () => NotifyNotBuilt("卡背详情/装备（原版点它开哪个窗，普查标了「不确定」）"));
                CosmoCells.Add(cell);
            }
            Clip = prevClip;
        }

        // ---- 左抽屉 `Cosmetic FIlter`（**出厂关**）----
        Transform _cosmoDrawer;
        /// <summary>抽屉开着没有。</summary>
        public bool CosmoFiltersOpen { get { return _cosmoDrawer != null && _cosmoDrawer.gameObject.activeSelf; } }

        void BuildCosmoDrawer(Transform page)
        {
            var d = Node(page, "Cosmetic FIlter", new PxRect(0.05f, 85f, 335.55f, 1080f));
            _cosmoDrawer = d;
            Rect(d, "40k_main_tab_shadow", new PxRect(0.05f, 85f, 153.06f, 1080f), "Shadow", QFlt, new Color(0f, 0f, 0f, 0.314f));
            Rect(d, "40k_main_tab_background", new PxRect(0.05f, 85f, 335.55f, 1080f), "Panel", QFlt);
            // `Filters`（**HLG**，不是 VLG）+ 两行：`Spacing`(335.5×15) / `Owned Toggle (1)`(335.5×50)
            Rect(d, "40_main_bt_toggle_on", new PxRect(239.54f, 100f, 310.13f, 150f), "Owned Image", QFltRow, null, true);
            var lb = Text(d, "Owned only", 25f, 234.5f, 100f, 150f, 5, PageInk, "Owned Label", 32f);
            if (lb != null) lb.SetRenderQueue(QFltText);
            AddHit(d, "OwnedHit", new PxRect(0.05f, 100f, 335.55f, 150f), QFltHit,
                   () => Debug.Log("[Collection] 全部卡背均已拥有（单机全解锁）⇒ 这个开关不改变结果"));

            d.gameObject.SetActive(false);        // 实证 act=F
        }

        public void ToggleCosmoFilters()
        {
            if (_cosmoDrawer == null) return;
            _cosmoDrawer.gameObject.SetActive(!_cosmoDrawer.gameObject.activeSelf);
            Debug.Log("[Collection] 卡背页的筛选抽屉 " + (CosmoFiltersOpen ? "打开" : "收起"));
        }

        public void ClearCosmoFilters()
        {
            Debug.Log("[Collection] 卡背页 `Clear filters`（**本轮没建筛选条件** —— 抽屉里只建了 `Owned only`，"
                      + "`Army Filter` 缺格子尺寸、没建，见 `Shell/CollectionWindow.cs` Cosmetics 那段）");
        }

        // ============================================================ 筛选栏：建 / 刷 / 点

        /// <summary>筛选栏开着没有（`Filters` 圆钮开合）。原版默认**收起**。</summary>
        public bool FiltersOpen { get { return _fltOpen; } }
        /// <summary>筛选栏那一列的滚动量（自检用）。</summary>
        public MenuScroll FilterScroll { get { return _fltScroll; } }
        /// <summary>画出来的筛选格数（自检用）。</summary>
        public int FilterCellCount { get { return _fltCells.Count; } }

        /// <summary>建左侧筛选栏（**起手是收起态** —— 原版靠 `hiddenPosition` 滑出去，我们整块显隐）。</summary>
        public void BuildCardFilters(Transform page)
        {
            var panel = Node(page, "Card Filters", FltView);
            _fltPanel = panel;

            // 兄弟序照原版：`Shadow` **先**、面板本体**后** ⇒ 面板压在影子上
            Rect(panel, "40k_main_tab_shadow", new PxRect(FltL, FltT, FltL + FltShadowW, FltT + FltViewH),
                 "Shadow", QFlt, new Color(0f, 0f, 0f, 0.314f));
            Rect(panel, "40k_main_tab_background", FltView, "Panel", QFlt);
            Node(panel, "Scroll View", FltView);
            Node(panel.Find("Scroll View"), "Viewport", FltView);

            // ⚠️ `Owner` 指向**面板**（不是窗口）—— `PointerLayer.HitScroll` 靠它判「这一区还开着没」
            //    （面板收起时整块 SetActive(false)，滚轮就不该再被这一列吃掉）
            _fltScroll = MenuScroll.TopAligned(FltView, FltContentH);
            _fltScroll.Owner = panel.gameObject;
            _fltScroll.OnChanged = () => RebuildFilterRows(panel);
            PointerLayer.RegisterScroll(_fltScroll);

            RebuildFilterRows(panel);                    // **先建**（内部会临时激活 —— TMP 量不到非激活对象）
            panel.gameObject.SetActive(_fltOpen);        // 再按状态显隐
        }

        /// <summary>`Filters` 圆钮 / 自检：开合筛选栏。</summary>
        public void ToggleFilters()
        {
            _fltOpen = !_fltOpen;
            if (_fltPanel != null) _fltPanel.gameObject.SetActive(_fltOpen);
            Debug.Log("[Collection] 筛选栏 " + (_fltOpen ? "打开" : "收起")
                      + "（原版是**滑进滑出**：`hiddenPosition=(-550,0)`、`animationTime=0.3` ——"
                      + " 我们做的是整块显隐，**出声**，见 `Shell/CollectionWindow.cs` 筛选栏那段注释）");
        }

        /// <summary>清空筛选（`Clear filters` 钮）。原版回到「不限」那一套。</summary>
        public void ClearCardFilters()
        {
            if (PointerLayer.Instance != null && PointerLayer.Instance.TextEditing) PointerLayer.Instance.EndText(false);
            CardsState.SetFilter(DeckFilter.None);
            RefreshCardsAfterFilter();
            Debug.Log("[Collection] 已清空筛选");
        }

        /// <summary>点了一格筛选（`$name` / `$fac:x` / `$rar:x` / `$cost:i` / `$type:x`）。</summary>
        public void ApplyCardFilter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (key == "$name")
            {
                var pl = PointerLayer.Instance;
                if (pl == null) return;
                pl.BeginText(CardsState.Filter.Name ?? "", 24,
                             s => { var f0 = CardsState.Filter; f0.Name = (s ?? "").Trim(); CardsState.SetFilter(f0); RefreshCardsAfterFilter(); Debug.Log("[Collection] 卡名筛选：" + (string.IsNullOrEmpty(f0.Name) ? "（清空）" : f0.Name)); },
                             () => RebuildFilterRows(_fltPanel),
                             s => { var t = _fltPanel != null ? _fltPanel.Find("Scroll View/Viewport/Name Filter/Input Text") : null; var lb = t != null ? t.GetComponent<Label>() : null; if (lb != null) lb.SetText(s + "_"); });
                Debug.Log("[Collection] 卡名筛选：输入后回车确认，ESC 取消");
                return;
            }
            if (key == "$owned") { Debug.Log("[Collection] 全部卡牌均已拥有（单机全解锁）⇒ 这个开关不改变结果"); RebuildFilterRows(_fltPanel); return; }
            if (key == "$upgradable") { Debug.Log("[Collection] Upgradable only：单机版没有升级系统（不装作有）"); return; }

            var f = CardsState.Filter;
            if (key.StartsWith("$fac:")) { var v = key.Substring(5); f.Faction = f.Faction == v ? "" : v; }
            else if (key.StartsWith("$rar:")) { var v = key.Substring(5); f.Rarity = f.Rarity == v ? "" : v; }
            else if (key.StartsWith("$cost:"))
            {
                int v = int.Parse(key.Substring(6));
                bool wasOn = f.Cost == v;                        // ⚠️ 先判「原来是不是这一档」再改，别改完再比
                f.Cost = wasOn ? DeckEditorState.AnyCost : v;
                f.CostMax = wasOn ? 0 : CostBucketHi(v);
            }
            else if (key.StartsWith("$type:")) { var v = key.Substring(6); f.Type = f.Type == v ? "" : v; }
            else return;
            CardsState.SetFilter(f);
            RefreshCardsAfterFilter();
        }

        /// <summary>改完筛选：重画卡池（回到顶部）+ 重刷筛选栏选中态 + 重算计数条。</summary>
        void RefreshCardsAfterFilter()
        {
            var page = PageRoot(1);
            if (page == null) return;
            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                if (CardsScroll != null) CardsScroll.SetOffset(0f);
                RebuildCardsCells(holder);
            }
            if (_fltPanel != null) RebuildFilterRows(_fltPanel);
            // ⚠️ 计数条：**原版跟着「指针悬停的那张卡」走**（`CardCollectionDisplay.CheckFocusedArmy`），
            //    我们还没有 hover ⇒ 这里只按当前筛选重算。**已知偏离**，见 `项目任务.md` §三 第 15 条 第 29 项。
            var counts = CardsRarityCounts();
            for (int i = 0; i < _wcCount.Length && i < counts.Length; i++)
                if (_wcCount[i] != null) _wcCount[i].SetText(counts[i].ToString());
        }

        /// <summary>费用那一档的**上界**（原版 8 档：`1-` / 2…7 / `8+`）。</summary>
        static int CostBucketHi(int lo) { return lo == 1 ? 1 : (lo == 8 ? int.MaxValue : lo); }

        /// <summary>按当前筛选条件 + 滚动量，把 7 行摆出来。**每次刷新都重建**（29 格 + 1 个搜索框，量小，省一套脏标记）。
        ///
        /// 🔴 **2026-09-23 修**：进来先把面板**临时激活**，建完再还原。
        ///    起因（实拍 + 断言双查）：本方法是「先 `SetActive(false)` 再建」的，而 **TMP 在非激活对象上量不出尺寸**
        ///    —— `Label.RefreshBounds` 读到的 `textBounds.size.x` 是 0（`WorldW` 返回下限 `1e-4`），
        ///    于是 `AlignRightOn/AlignLeftOn` 里 `worldRight - WorldW*0.5` 退化成「**把锚点放在右边界**」，
        ///    整段字右移半个字宽（`common`/`legendary` 那几个标签就是这么跑到面板边上被切掉的）。
        ///    ⚠️ 这正是 `CLAUDE.md` §三 记的那条：「TMP 在对象没激活时量不出尺寸，`ForceMeshUpdate()` 要在
        ///    `SetActive(true)` **之后**调」—— 那次是 tooltip，这次是筛选栏。**同类坑在同一工程里第三次出现。**
        ///    断言的空转也要记一笔：我第一条「标签不越出左边界」的断言**恒真**（左边缘 = 右边界 − 0/2），
        ///    是「量出来 0.0px」把它暴露出来的 —— 断言光「过」不够，还得**看一眼量出来的数**。</summary>
        public void RebuildFilterRows(Transform panel)
        {
            if (panel == null) return;
            bool wasActive = panel.gameObject.activeSelf;
            if (!wasActive) panel.gameObject.SetActive(true);     // ⇒ 建的时候必须活着（见上）

            var vp = panel.Find("Scroll View/Viewport");
            var parent = vp != null ? vp : panel;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            _fltCells.Clear();

            if (_fltScroll != null)
            {
                var prevClip = Clip;
                Clip = FltView;

                BuildFilterRowModel();
                BuildNameRow(parent);
                BuildFilterTitles(parent);

                foreach (var c in _fltCells)
                {
                    var r = _fltScroll.Shift(c.R);
                    if (!_fltScroll.Intersects(r)) continue;
                    var cell = Node(parent, "Cell_" + KeyToName(c.Key), r);
                    var b = _fltScroll.Shift(c.Bg);
                    Rect(cell, c.Icon, b, "Background", QFltRow, ToggleTint(c.On), true);
                    if (!string.IsNullOrEmpty(c.Label))
                    {
                        var lr = _fltScroll.Shift(c.Lab);
                        TextAligned(cell, c.Label, lr, ToggleTint(c.On), "Label", c.LabelPx, c.LabelRight, c.LabelAutoMin);
                    }
                    AddHit(cell, "Hit", r, QFltHit, () => ApplyCardFilter(c.Key));
                }

                Clip = prevClip;
            }

            if (!wasActive) panel.gameObject.SetActive(false);
        }

        /// <summary>筛选格的**稳定名字**（`$rar:legendary` → `rar_legendary`）—— 自检按它找格，
        /// **别按序号找**（视口外的格不建 ⇒ 序号会错位）。</summary>
        static string KeyToName(string key)
        {
            return (key ?? "").Replace("$", "").Replace(":", "_");
        }

        /// <summary>格子的着色 = **原版那套 tint**（`checkMark` 30/30 全是 null ⇒ 选中态没有对勾图）。
        /// ⚠️ 这两个值是「同 bundle 里成对出现的 toggle 预制值」，**没证明就是采集筛选那一支** —— 如实标。</summary>
        static Color ToggleTint(bool on)
        {
            return on ? new Color(1f, 1f, 1f, 1f) : new Color(0.349f, 0.341f, 0.341f, 1f);
        }

        /// <summary>摆一段**左/右对齐**的字（`Label.Align*On` 吃世界坐标）。
        /// 🔴 **必须在建好之后再对齐** —— TMP 在空串/未激活时量出的是垃圾边界（`项目任务.md` §三 第 15 条 第 8 项）。
        /// ⚠️ `autoMinPx &gt; 0` 时开**自适应字号**（原版那几处 `auto(10-27)` / `auto(25-45)`）——
        ///    不开的话 `Legendary` 在 100px 的格宽里会**冲出去**（实测 105.9px &gt; 100）。</summary>
        void TextAligned(Transform parent, string text, PxRect r, Color col, string name, float fontPx, bool right,
                         float autoMinPx = 0f)
        {
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, col, name, fontPx);
            if (lb == null) return;
            lb.SetRenderQueue(QFltText);
            if (autoMinPx > 0f) lb.SetAutoFitBox(LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), autoMinPx, fontPx);
            float x = right ? r.x2 : r.x1;
            float wx = LayoutSpace.FromPixel(x, 0f).x;
            if (right) lb.AlignRightOn(wx); else lb.AlignLeftOn(wx);
        }

        /// <summary>第 ① 行：搜索框（原版 `CardNameFilter` → `Input Field` 281.28×40 @面板内 (27.01,19.51)）。
        /// ⚠️ 坐标同样要**加 `FltL/FltT` 换成页面绝对**（见 `BuildFilterRowModel` 那条踩坑）。</summary>
        void BuildNameRow(Transform parent)
        {
            var r = _fltScroll.Shift(new PxRect(FltL + FR_InputX1, FltT + FR_InputY1,
                                                FltL + FR_InputX2, FltT + FR_InputY2));
            if (!_fltScroll.Intersects(r)) return;
            var cell = Node(parent, "Name Filter", r);

            // 底：**九宫格**（原版 `InputFieldBackground` 是 **Unity 内置图** 32×32、`m_Border=(10,10,10,10)`，
            //     藏在 `bundle_Warpforge_unitybuiltinassets` ⇒ 得单独导，见 `工具/import_original_art.py`）
            var tex = Art("InputFieldBackground");
            if (tex != null)
            {
                var g = ImageQuad.CreateNineSlice(cell, tex, new Vector4(10f, 10f, 10f, 10f), 32f, 32f,
                                                  Local(cell, r.x1, r.y1, r.x2, r.y2),
                                                  LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), "Input BG");
                if (g != null)
                    foreach (var q in g.GetComponentsInChildren<ImageQuad>())
                    { q.SetTint(new Color(0.0627f, 0f, 0f, 1f)); q.SetRenderQueue(QFltRow); }   // col=(0.0627,0,0,1)
            }

            // 字：`Text Area` [37.4,182.4,231.3,27]（绝对）→ 面板内 (37.15, 26.5)~(268.45, 53.5)；空时是占位符 "Search"
            string cur = CardsState.Filter.Name;
            bool editing = PointerLayer.Instance != null && PointerLayer.Instance.TextEditing;
            string txt = editing ? (PointerLayer.Instance.TextBuffer + "_")
                                 : (string.IsNullOrEmpty(cur) ? "Search" : cur);
            var tr = _fltScroll.Shift(new PxRect(FltL + 37.15f, FltT + 26.5f, FltL + 268.45f, FltT + 53.5f));
            TextAligned(cell, txt, tr, PageInk, "Input Text", 30f, false);

            // 尾图标 `40k_icon_search` 35×30（面板内 268.35,24.5 → 303.35,54.5）
            var ir = _fltScroll.Shift(new PxRect(FltL + 268.35f, FltT + 24.5f, FltL + 303.35f, FltT + 54.5f));
            Rect(cell, "40k_icon_search", ir, "Search Icon", QFltRow, null, true);

            AddHit(cell, "Hit", r, QFltHit, () => ApplyCardFilter("$name"));
        }

        /// <summary>后 6 行的格子表。**坐标一律「面板内」写、出口处加 `FltL/FltT` 换成页面绝对** ——
        /// 🔴 2026-09-23 踩过：最初模型里一半加了 `FltT` 一半没加，而 `RebuildFilterRows` 是**按绝对坐标摆**的
        /// ⇒ **整排偏上 155.9px**，搜索框干脆落到视口外**根本没建**（8 条断言把它抓出来）。
        /// 出处逐条见 `FR_*` 与 A3 §五·1。</summary>
        void BuildFilterRowModel()
        {
            var f = CardsState.Filter;
            // 面板内 → 页面绝对
            System.Func<float, float, float, float, PxRect> A =
                (x1, y1, x2, y2) => new PxRect(FltL + x1, FltT + y1, FltL + x2, FltT + y2);

            // ---- ② Owned only / ③ Upgradable only（原版两个 `EverguildToggle`，50 高）----
            for (int k = 0; k < 2; k++)
            {
                float y = k == 0 ? FR_Owned : FR_Upgr;
                _fltCells.Add(new FltCell {
                    R = A(0f, y, FltW, y + 50f),
                    Bg = A(FR_TogIconX1, y, FR_TogIconX2, y + 50f),
                    Icon = "40_main_bt_toggle_on",
                    Lab = A(FR_TogLabX1, y, FR_TogLabX2, y + 50f),
                    Label = k == 0 ? "Owned only" : "Upgradable only", LabelPx = 32f,
                    Key = k == 0 ? "$owned" : "$upgradable",
                    On = k == 0,          // ⚠️ 单机全解锁 ⇒ Owned 恒真（与卡组编辑同一口径）
                });
            }

            // ---- ④ Army：13 档 · 格 100×100 · sp7/0 · pad L14 ⇒ **3 格/行** ----
            var facs = CardsState.Factions();
            for (int i = 0; i < facs.Count; i++)
            {
                float x = FltGridPadL + (i % 3) * (100f + FltGridSpX);
                float y = FR_Army + 50f + (i / 3) * 100f;                 // Content 从行内 y+50 起
                var rr = A(x, y, x + 100f, y + 100f);                     // Army 行**背景铺满格**（A3 §5·1）
                _fltCells.Add(new FltCell {
                    R = rr, Bg = rr, Icon = DeckRuntime.FactionIcon(facs[i]), Label = null,
                    Key = "$fac:" + facs[i], On = f.Faction == facs[i],
                });
            }

            // ---- ⑤ Rarity：5 档 · 格 100×100、图 50×50 居中、标签在格底 100×22 ----
            for (int i = 0; i < RarityKeys.Length; i++)
            {
                float x = FltGridPadL + (i % 3) * (100f + FltGridSpX);
                float y = FR_Rarity + 65f + (i / 3) * 100f;               // Content 从行内 y+65 起
                _fltCells.Add(new FltCell {
                    R = A(x, y, x + 100f, y + 100f),
                    Bg = A(x + 25f, y + 25f, x + 75f, y + 75f),
                    Icon = RarityArt[i],
                    Lab = A(x, y + 78f, x + 100f, y + 100f),
                    Label = RarityNames[i], LabelPx = 23.2f, LabelAutoMin = 10f, LabelRight = true,
                    Key = "$rar:" + RarityKeys[i], On = string.Equals(f.Rarity, RarityKeys[i], System.StringComparison.OrdinalIgnoreCase),
                });
            }

            // ---- ⑥ Cost：**8 档（`1-`/2…7/`8+`，区间不是每费一格）** · 格 65×65 · sp15/20 · pad L15 ⇒ 4 格/行 ----
            for (int i = 0; i < CostBuckets.Length; i++)
            {
                float x = FltCostPadL + (i % 4) * (65f + FltCostSpX);
                float y = FR_Cost + 65f + (i / 4) * (65f + FltCostSpY);   // Content 从行内 y+65 起
                var rr = A(x, y, x + 65f, y + 65f);
                _fltCells.Add(new FltCell {
                    R = rr, Bg = rr, Icon = "Card_Frame_Cost_Icon",
                    Lab = rr, Label = CostBuckets[i].Label, LabelPx = 45f, LabelAutoMin = 25f,
                    Key = "$cost:" + CostBuckets[i].Lo, On = f.Cost == CostBuckets[i].Lo,
                });
            }

            // ---- ⑦ Type：**3 档**（原版 `CardTypeOptions`，**没有防御卡那一档**）· 格 80×100 · HLG pad15/LowerLeft ----
            for (int i = 0; i < TypeKeys.Length; i++)
            {
                float x = FltCostPadL + i * 80f;
                float y = FR_Type + 50f;                                  // 行高 150、HLG 贴下（align=6）
                _fltCells.Add(new FltCell {
                    R = A(x, y, x + 80f, y + 100f),
                    Bg = A(x + 15f, y + 25f, x + 65f, y + 75f),
                    Icon = TypeArt[i],
                    Lab = A(x, y + 78f, x + 80f, y + 100f),
                    Label = TypeLabels[i], LabelPx = 23.2f, LabelAutoMin = 10f, LabelRight = true,
                    Key = "$type:" + TypeKeys[i], On = f.Type == TypeKeys[i],
                });
            }
        }

        /// <summary>四行的小标题（原版 `Title` TMP，**fs32 · hAlign=Center**）。
        /// ⚠️ 2026-09-23 实拍补的：第一版**漏了这四个**（只建了格子），断言一条都没报 —— 因为它们不是「位置不对」
        /// 而是**根本不在**，而当时没有「标题在不在」的断言（已补在 `CollectionScene`）。
        /// rect 出处：`menu_rect.py … -5393211807834578219` 等（Rarity/Cost/Type 的 Title 从 x=25 起，Army 从 0 起）。</summary>
        void BuildFilterTitles(Transform parent)
        {
            TitleRow(parent, "Army", 0f, FR_Army, 50f);
            TitleRow(parent, "Rarity", 25f, FR_Rarity + 25f, 50f);
            TitleRow(parent, "Energy Cost", 25f, FR_Cost + 15f, 50f);
            TitleRow(parent, "Type", 25f, FR_Type + 5f, 50f);
        }

        void TitleRow(Transform parent, string text, float x, float y, float h)
        {
            var r = _fltScroll.Shift(new PxRect(FltL + x, FltT + y, FltL + FltW, FltT + y + h));
            if (!_fltScroll.Intersects(r)) return;
            // hAlign=Center ⇒ 用 `Text()`（它摆的就是矩形中心），**不要**用 `TextAligned`
            var lb = Text(parent, text, r.x1, r.x2, r.y1, r.y2, 5, PageInk, "Title " + text, 32f);
            if (lb != null) lb.SetRenderQueue(QFltText);
        }

        /// <summary>Rarity 五档（原版 `CardRarityFilter.options` 的 `alternativeText`，顺序照抄）。</summary>
        static readonly string[] RarityKeys = { "common", "rare", "epic", "legendary", "special" };
        /// <summary>**卡面上印的那几个词**（= 原版 `alternativeText` 原文，首字母大写）。</summary>
        static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary", "Special" };
        static readonly string[] RarityArt =
        {
            "1_40k_cardframe_rarity_common", "2_40k_cardframe_rarity_rare", "3_40k_cardframe_rarity_epic",
            "4_40k_cardframe_rarity_legendary", "5_40k_cardframe_rarity_special",
        };
        /// <summary>Cost 八档（原版 `CardCostFilter.options`：`1-` / `2`…`7` / `8+`）。</summary>
        static readonly CostBucket[] CostBuckets =
        {
            new CostBucket("1-", 1), new CostBucket("2", 2), new CostBucket("3", 3), new CostBucket("4", 4),
            new CostBucket("5", 5), new CostBucket("6", 6), new CostBucket("7", 7), new CostBucket("8+", 8),
        };
        struct CostBucket { public readonly string Label; public readonly int Lo;
            public CostBucket(string l, int lo) { Label = l; Lo = lo; } }
        /// <summary>Type 三档（`CardTypeOptions`：Hero=10/Minion=0/Tactic=20）。**防御卡原版没有这一档** —— 出声。</summary>
        static readonly string[] TypeKeys = { "hero", "unit", "tactic" };
        static readonly string[] TypeLabels = { "Warlord", "Troops", "Stratagem" };
        static readonly string[] TypeArt =
        {
            "40k_menu_search_icon_warlord", "40k_menu_search_icon_troop", "40k_menu_search_icon_stratagem",
        };

        // ============================================================ 页头（A2 §三·4）
        public const float CreateX = 1661f, ImportX = 1391f, HdrBtnY = 80.9f, HdrBtnW = 245f, HdrBtnH = 60f;
        public const float FltBtnX = 367.2f, FltBtnY = 88.5f, FltBtnS = 50f;
        /// <summary>🔴 **2026-09-23 更正**：原来这里写 **1218.6**（注释说是「按容器内右对齐实算」）—— **那是错的**。
        /// 真值 = **612.2**（`资料/普查产出_0923/A2_Deck页.md:161` **实测落点**：=「Filters」文字条右缘 **587.2 + 25**）。
        /// **错因**：`Header/Filters` 是个 `GridLayoutGroup`（cell 85×70 / count 1 / MiddleLeft）+ `CSF(H=Preferred)`，
        /// 而唯一子件 `Clear Filter Button` 带 **`LayoutElement.m_IgnoreLayout = 1`** ⇒ **布局根本不排它**、
        /// 容器**宽度算出来是 0** ⇒ 按钮按自己的锚点 `(1,.5) pos(20,0)` 落在**容器右缘 592.2 + 20 = 612.2**。
        /// **代价**：照 1218.6 摆 ⇒ 它和右边的 `Import`（1391..1636）**压掉 77.6px**，实拍一眼可见
        /// （`_tmp_view/collection/06_收藏_ImportDeck弹窗.png`）。</summary>
        public const float ClearFltX = 612.2f, ClearFltY = 83.5f, ClearFltW = 250f, ClearFltH = 60f;

        public override void Open()
        {
            Build();
            if (tabButtons != null) tabButtons.Click(0);       // 默认落在第一页（`GetStartingTab` 的回落）
        }

        /// <summary>点了还没做的件 —— **出声**（红线：不许静默失败）。</summary>
        public override void NotifyNotBuilt(string what)
        {
            Debug.Log("[Collection] `" + what + "` 还没实现（本轮建到「外壳 + 四页签 + **Deck 页** + **Cards 页（含筛选栏）**」，"
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

        // 🔴 **2026-09-23 修一个静默 bug**：原来四处刷新都写 `transform.Find("Tabs/" + 页名)`，
        //    而 **`Tabs` 是 `Content Area` 的子节点**（`MenuWindowBase.BuildShell`：`res.tabs = Node(area, "Tabs", …)`
        //    —— 那句就在 `Node(root, "Content Area", …)` 下面几行）⇒ **那条路径恒为 null**，
        //    于是「重建卡池 / 重建卡组列表」全在**空转**：**状态对、画面不刷新**，
        //    而断言量的都是状态（`DeckCount()` / `CardsVisibleCount`）⇒ **一条都没报**。
        //    实拍才露出来（清空筛选后搜索框还停在 `impe_`）。
        //    ⇒ 不再拼路径，**直接存页节点引用**。
        readonly Transform[] _pages = new Transform[4];
        /// <summary>第 <paramref name="p"/> 页的节点（未建时给 null）。</summary>
        public Transform PageRoot(int p) { return (p >= 0 && p < _pages.Length) ? _pages[p] : null; }

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
                _pages[p] = pg;
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
            // ⚠️ 文案与字号照 A2 §三·4：`Import` 那条的字是 **"Import Deck"**、`Create` 是 **"Create Deck"**，都是 **fs42**(auto 10-42)
            var impLab = Text(page, "Import Deck", ImportX, ImportX + HdrBtnW, HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Import Text", 42f);
            if (impLab != null) impLab.SetRenderQueue(QPageText);
            var newLab = Text(page, "Create Deck", CreateX, CreateX + HdrBtnW, HdrBtnY, HdrBtnY + HdrBtnH, 5, PageInk, "Create Text", 42f);
            if (newLab != null) newLab.SetRenderQueue(QPageText);

            AddHit(page, "ImportHit", new PxRect(ImportX, HdrBtnY, ImportX + HdrBtnW, HdrBtnY + HdrBtnH), QPageRow,
                   () => OpenImportPopup());
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

        /// <summary>点一格卡组：**选中 + 开 `Deck info Popup`**（2026-09-23 起 —— 那扇窗建好了）。
        /// 原版就是这条：点格 ⇒ 选中，由窗里的 `Edit Deck` 才进编辑。</summary>
        public void SelectDeck(int i)
        {
            if (i != CollectionData.CurrentIndex())
            {
                CollectionData.Select(i);
                var pr = PageRoot(0);
                var holder = pr != null ? pr.Find("Deck Scroll View") : null;
                if (holder != null) RebuildDeckCells(holder);
            }
            OpenDeckInfo(i);
        }

        /// <summary>最近一次开出来的 `Deck info Popup`（自检用）。</summary>
        public static DeckInfoPopup LastOpened;

        /// <summary>最近一次开出来的 `Import Deck Popup`（自检用）。</summary>
        public static ImportDeckPopup LastImport;

        /// <summary>开 `Import Deck Popup`（Deck 页那个 `Import` 钮走它；自检也直调）。</summary>
        public ImportDeckPopup OpenImportPopup()
        {
            LastImport = null;
            if (Manager == null) { Debug.Log("[Collection] 没有 `WindowsManager`，开不了 `Import Deck Popup`"); return null; }
            var w = ImportDeckPopup.Create(Manager, RebuildDeckListNow);
            Manager.OpenWindow(w);
            LastImport = w;
            Debug.Log("[Collection] 开 `Import Deck Popup`");
            return w;
        }

        /// <summary>重画 Deck 页的卡组列表（导入成功后调）。</summary>
        public void RebuildDeckListNow()
        {
            var pr = PageRoot(0);
            var holder = pr != null ? pr.Find("Deck Scroll View") : null;
            if (holder != null) RebuildDeckCells(holder);
        }

        /// <summary>开 `Deck info Popup`（自检也直调它）。</summary>
        public DeckInfoPopup OpenDeckInfo(int i)
        {
            LastOpened = null;
            if (Manager == null) { Debug.Log("[Collection] 没有 `WindowsManager`，开不了 `Deck info Popup`"); return null; }
            var w = DeckInfoPopup.Create(Manager, i);
            Manager.OpenWindow(w);
            LastOpened = w;
            Debug.Log("[Collection] 开 `Deck info Popup`：「" + CollectionData.DeckAt(i).Name + "」");
            return w;
        }

        /// <summary>进编辑。⚠️ **这是相对原版的一处偏离**：原版在同一扇窗里换页；我们的编辑器早已建成**独立场景**
        ///    （200 条自检 + 自带的坐标层），嵌进窗里要重写它的 `Pos/ToPx` ⇒ 先用「切场景」把闭环打通。
        ///    ⚠️ **批处理下不切场景**（自检要靠同一个进程跑完）⇒ 自检验的就是「交接下标对不对」。</summary>
        public void EditDeck(int i) { GoEdit(i); }

        /// <summary>「进编辑」的**唯一实现** —— 收藏窗那条路与 `Deck info Popup` 的 `Edit Deck` 钮**都走它**
        /// （CLAUDE.md §三：两处写同一条规则 = 迟早不一致）。</summary>
        public static void GoEdit(int i)
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
            var pr = PageRoot(0);
            var holder = pr != null ? pr.Find("Deck Scroll View") : null;
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
                    _win.BuildCosmeticsPage(_root);        // 233 张卡背 2026-09-23 已导 ⇒ 可建了
                    break;
                default:
                    _win.NotifyNotBuilt("Styles 页（异画，卡面文案 `Styles`）—— **数据在远程包、本机零副本**，"
                                        + "见 `资料/阶段二_卡组线_原版规格.md` §七 ③b");
                    break;
            }
        }
    }
}
