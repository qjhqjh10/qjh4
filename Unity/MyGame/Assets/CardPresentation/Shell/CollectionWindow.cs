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
//   ✅ **Cosmetics 页**（卡背）：页头 + **233 张卡背网格**（列数**按宽度算** = **6 列** · 250×405）+ 左抽屉（起手收起）
//      🔴 **2026-09-24 坐标整套订正**：原来那一套（`168.27,85→1920,1080`）是**在 `Content Area` 的局部系里**的
//      ⇒ 整页偏 (167.17, 70.94)、**列数因此多算成 7 列**。真值 = `Scroll View` **335.44,155.94 → 1920.01,1080**
//      （1584.56 × 924.06）⇒ `floor(1584.56 ÷ 250)` = **6 列**。根因是 `menu_rect.py` 的父链口径，坑见 `资料/已知的坑.md`。
//   ✅ **Styles 页**（异画）**2026-09-24 建完**：换风格条（左右两颗圆钮 + 风格名）+ **6 列 × 262.5×384 网格** + 左抽屉（**出厂展开**）
//      · 数据 = **本地 7 张督军异画**（`assets_full/bundle_<阵营>cardassets_assets_all/Texture2D/`，
//        文件名是 **`AA_HB_…` / `…_AA_HB.png`**，另有 `DarkAngels_AA_warlord_Azrael_v2`）
//      · 覆盖 **2 种风格**：`AA_HB`(6 张) + `v2`(1 张)；其余风格在远端 CCD 的 `alternateartstyles` 包。
//        ⚠️ 上一版这里写「远程包、本机零副本」—— **错因是普查搜的词**（`alternate`/`variant`/`skin`）
//        **一个都不命中 `AA_HB` 这种命名**，是用户拿文件名来问才发现的（`阶段二_卡组线_原版规格.md` §七 ③b）。
//   ✅ **`Deck info Popup`**（2026-09-23）：点一格卡组 ⇒ 选中 + 开它；窗里 `Edit Deck` 才进编辑
//      ⇒ **`SelectDeck` 里那条「再点一下 = 进编辑」的顶替路已撤**（原版那条路有了）
//   ✅ **`Import Deck Popup`**（2026-09-23）：接上 Deck 页那个 `Import` 钮（此前点了只报「没实现」）
//   ⏭ **`Deck Selection Popup with Tabs`**（三个弹窗里最后一个）
//   ⏭ 卡片详情窗（参数已于 2026-09-23 普查完，够建 80%；见 `项目任务.md` §三 第 15 条）
//
// ---- Cosmetics 页**没做**的（逐条出声）----
//   · ~~`Cardback Shadow SDF`~~ ✅ **2026-09-26 建完 + 已加 6 条断言**（不再是缺口；原来那段「仍然没画」已删）。
//     现在的样子：卡背格 = **两层**，底 = SDF（比卡背大一圈）、面 = 卡背，**各自一个渲染队列**（`QPageSdf` < `QPageRow`）。
//     逐值与出处（**唯一出处**）→ `项目任务.md` §三 第 8b 条 —— 那里写了这个 rect 是多少、格式是 `x,y,w,h`、
//     以及为什么不能拿战斗牌堆那处的倍数来套（**两处倍数不同**：这里 337.5/250，那里 2.9212/2.1739）。
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
        /// <summary>页内的渲染队列**梯子**。
        /// 🔴 **2026-09-26 插了一层**：卡背格多了一个 **`Cardback Shadow SDF`**（原版那层距离场，
        /// 比卡背本体大一圈、**画在卡背底下**）⇒ 给它单开 `QPageSdf`，其余整体 +1。
        /// ⚠️ **别把 SDF 和卡背放同一个队列** —— 透明物体按「到相机的 3D 距离」排序，
        /// 同一队列里谁盖谁不可控（这工程踩过三次，见 `资料/已知的坑.md`）。</summary>
        public const int QPagePanel = 3030, QPageSdf = 3031, QPageRow = 3032,
                         QPageText = 3033, QPageOverlay = 3034;
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
        /// <summary>一页的「左侧筛选栏 + 它的状态」。**Cards 页与 Styles 页各一份、实现只有一份**
        /// （铁律「两处写同一条规则 = 迟早不一致」）—— 下面那些 `_fltXxx` 不是字段，是**转发到当前这一份**。
        /// 每份自己带 <see cref="State"/>（筛的是哪一批卡）与 <see cref="OnChanged"/>（筛完该重画什么）。
        /// 🔴 **实测两页的抽屉矩形完全相同**：`Card Filters` 与 `Card Filters`（异画页）都是
        /// **0.25, 155.94 → 335.56, 1080**（335.31 × 924.06）⇒ 几何不必参数化。
        /// ⚠️ 出厂态**两页不同**：Cards 页默认收起、**Styles 页 act=T（展开）**。</summary>
        class FilterPanel
        {
            public Transform Node;                       // `Card Filters` 容器
            public MenuScroll Scroll;
            public readonly List<FltCell> Cells = new List<FltCell>();
            public bool Open;
            public DeckEditorState State;                // 这一页筛的是哪一批卡
            public System.Action OnChanged;              // 筛选变了之后重画什么（各页自己给）
        }

        /// <summary>当前动作作用在哪一份筛选栏上（建 / 刷 / 点 / 开合都走它）。</summary>
        FilterPanel _flt;
        FilterPanel _fltCards;      // Cards 页那份
        FilterPanel _fltStyles;     // Styles 页那份（2026-09-24 加）

        // ⚠️ 这三个**是属性不是字段** —— 原来它们是字段、只服务 Cards 页一份。
        //    改成转发之后，下面所有筛选栏方法**一个字都不用改**就同时服务两页。
        List<FltCell> _fltCells { get { return _flt.Cells; } }
        Transform _fltPanel { get { return _flt != null ? _flt.Node : null; } set { if (_flt != null) _flt.Node = value; } }
        MenuScroll _fltScroll { get { return _flt != null ? _flt.Scroll : null; } set { if (_flt != null) _flt.Scroll = value; } }
        bool _fltOpen { get { return _flt != null && _flt.Open; } set { if (_flt != null) _flt.Open = value; } }
        /// <summary>筛选栏代码里**唯一**该用的筛选状态（别在那些方法里直接写 `CardsState` ——
        /// 异画页筛的是异画那批卡，不是卡池）。</summary>
        DeckEditorState FltState { get { return _flt.State; } }

        /// <summary>把 `_flt` 指向某一份，跑完还原（同步调用，不跨帧）。
        /// 🔴 **必须还原** —— 否则「Cards 页建完把指针留在自己身上」，异画页那次就作用错对象了。</summary>
        void Scope(FilterPanel p, System.Action body)
        {
            // ⚠️ 面板还没建就点了（例：页还没切过去）—— **出声**，别静默吞掉
            if (p == null) { Debug.LogWarning("[Collection] 这一页的筛选栏还没建，这次动作忽略"); return; }
            var prev = _flt;
            _flt = p;
            try { body(); } finally { _flt = prev; }
        }
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
            // 页头：Filters 圆钮 + 文案 + Clear filters（**三页共用的那一行** —— 见 `BuildFilterHeader`）
            // ⚠️ 2026-09-23 顺手修两处（A3 §5·2 原文）：原版这套按钮是
            //    **[圆钮 50×50]** → 里面 `icon detail` 只有 **30×30**（`sd=(-20,-20)` = 四边各内缩 10、preserveAspect）
            //    · `label` 在 **[437.2, y, 150, 50]**；我们原来是「图拉满 50×50」+「label 从 427.2 起」。
            // 本页字号 = **42 / 42**（原版逐页不同，见 `BuildFilterHeader` 的注释）
            BuildFilterHeader(page, 42f, 42f, ClearCardFilters, ToggleFilters);

            // 万能卡计数：4 个稀有度图标 + 各自的数字（**复用卡组编辑那条几何**，别抄第二份）
            Rect(page, "40k_topmarquee_currency_display_BW",
                 new PxRect(1550f, 91.5f, 1870f, 135.5f), "Wildcard Bg", QPagePanel);
            string[] wcIc = { "40k_general_wildcard_common_small", "40k_general_wildcard_rare_small",
                              "40k_general_wildcard_epic_small", "40k_general_wildcard_legendary_small" };
            var counts = CardsRarityCounts();
            for (int i = 0; i < 4; i++)
            {
                float x = 1565f + 75f * i;
                // 🔴 **2026-09-27 补 `keepAspect`（PA 普查抓的）**：原版这 4 个图标 `m_PreserveAspect = 1`、
                //   `m_Type=0`（Simple），贴图 42×51 塞进 30×44 的框 ⇒ 原版只画 **30×36.4**（按框居中）。
                //   我们原来拉伸 ⇒ 画满 30×**44**，**高 ×1.21**。同为 PA=1 的另三张（41×51）同理。
                Rect(page, wcIc[i], new PxRect(x, 91.5f, x + 30f, 135.5f), "Wildcard Icon " + i, QPageRow,
                     null, true);
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

            // 左侧筛选栏（**在卡池之后建** ⇒ 兄弟序在原版里也是它靠后 = 画在卡池之上）
            // 起手收起（原版靠 `hiddenPosition` 滑出去；我们整块显隐 —— 出声）
            _fltCards = BuildFilterPanel(page, CardsState, RefreshCardsAfterFilter, false);

            // `Empty Collection Warning`：**原版三页都有**，出厂 `act=F`，
            // 运行期条件 = **过滤后为空**（`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0` ⇒ `SetActive(true)`）。
            // 坐标 = **135.22,70.94 → 1970.01,1080**（**比父还宽、左右都溢出** —— 原版就这样，别"修正"）。
            // ✅ 2026-09-24 补建（Styles 页同日已建过同一条；这是 §三 第 15 条 第 50 行的下半场）。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(135.22f, 70.94f, 1970.01f, 1080f));
                var wt = Text(ew, "There are no cards in your collection for the selected filters",
                              135.22f, 1970.01f, 70.94f, 1080f, 5, PageInk, "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _cardsEmpty = ew;
            }
            RefreshCardsEmpty();
        }

        Transform _deckEmpty;      // Deck 页的「一套卡组都没有」
        Transform _cardsEmpty;     // Cards 页
        Transform _cosmoEmpty;     // Cosmetics 页（卡背）
        /// <summary>Cards 页的「过滤后为空」提示（判据与 Styles 页**同一条**：`VisibleCards().Count <= 0`）。</summary>
        void RefreshCardsEmpty()
        {
            if (_cardsEmpty != null) _cardsEmpty.gameObject.SetActive(CardsState.VisibleCards().Count <= 0);
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
                // ⚠️ 闭包**别捕循环变量 `i`** —— 点击发生在重建之后，那时 `i` 已经是 `list.Count`（越界）
                var def = list[i];
                AddHit(parent, "CardHit_" + i, r, QPageRow, () => OpenCardDetail(def));
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
        //   · `Cardback Shadow SDF`：**图本地有、喂法也查清了**（见文件头），**但没画** —— 这是缺口，别写成「本地没有」
        //   · 抽屉里的 `Army Filter`：A4 只给了容器 rect（335.5×345）与「→ Title + Content(HLG) → Toggle×N」，
        //     **没给格子的尺寸** ⇒ **没建**（要建得先补一次普查）
        //   · `Empty Collection Warning`（`act=F`）⇒ 照纪律不建
        // ---- 🔴 2026-09-24 坐标整套订正（原来那一套在 `Content Area` 的局部系里）----
        // 原来写的是 `CosmoL/CosmoT = 168.27 / 85`。那是把 **`Content Area`(167.17,70.94) 当成原点** 的值
        // ⇒ 整页（网格 + 抽屉 + 页头）**统一偏了 (167.17, 70.94)**，而且**列数因此多算了一列**。
        // 真值（这次是**从 `Collection Menu Variant` 根节点一路走下来**读的，口径见
        // `资料/已知的坑.md` 那条 `menu_rect.py` 的坑）：
        //   · `Cardback Display/Scroll View` = **335.44, 155.94 → 1920.01, 1080**（1584.56 × 924.06）
        //   · `Cardback Display/Cosmetic FIlter` = **0.06, 155.94 → 335.56, 1080**
        //   · `Header/Filter Toggle` = **367.17, 88.44**（与 Deck/Cards/Styles 三页**同值** ⇒ 已收口到 `BuildFilterHeader`）
        // ⚠️ 上一版据 A4 写成 `168.27,85 → 1920,1080`（1751.73 宽）⇒ 列数算出 **7**；
        //    真视口 1584.56 宽 ⇒ **floor(1584.56 ÷ 250) = 6 列**。两个数只差 0.87px 时看不出对错，这次差一列。
        public const float CosmoL = 335.44f, CosmoT = 155.94f, CosmoR = 1920.01f, CosmoB = 1080f;
        public const float CosmoCellW = 250f, CosmoCellH = 405f;
        /// <summary>列数 = `floor(视口宽 ÷ 格宽)`（**算出来的，不是 `_segments`**）。</summary>
        public static int CosmoCols { get { return Mathf.Max(1, Mathf.FloorToInt((CosmoR - CosmoL) / CosmoCellW)); } }
        /// <summary>内容**整体居中**的左边距（`CreateCellPool` 的 `((W − cols·cellW) + cellW)·0.5` 那条）
        /// = (1584.56 − 6×250) × 0.5 = **42.28**。</summary>
        public static float CosmoPadX { get { return (CosmoR - CosmoL - CosmoCols * CosmoCellW) * 0.5f; } }
        public static readonly PxRect CosmoView = new PxRect(CosmoL, CosmoT, CosmoR, CosmoB);

        public static PxRect CosmoCellRect(int i)
        {
            int r = i / CosmoCols, c = i % CosmoCols;
            float x = CosmoView.x1 + CosmoPadX + c * CosmoCellW;
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
            // ---- 页头 = **三页共用的那一行**（2026-09-24 收口；本页字号 35 / 33，逐页不同）----
            BuildFilterHeader(page, 35f, 33f, ClearCosmoFilters, ToggleCosmoFilters);

            // 标题那条 rect **宽是 0**（实测 `sd=(0,60)`、`m_HorizontalAlignment=4` = **Right**）
            // ⇒ **右对齐到 1821.01** 才是它的真值（A4 表里写「hFlush」**是错的**，已就地更正）。
            // y 真值 = **80.94 .. 140.94**（2026-09-24 订正；原来写的 10..70 是 `Content Area` 局部值）。
            var title = Text(page, "Your cosmetics collection", 1200f, 1821.01f, 80.94f, 140.94f, 5, PageInk,
                             "Header Label", 38f);
            if (title != null) { title.SetRenderQueue(QPageText); title.AlignRightOn(LayoutSpace.FromPixel(1821.01f, 0f).x); }

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

            // `Empty Collection Warning`：**本页那一份的矩形与别页不同** —— 直读 = **170.44,70.94 → 1970.00,1080**
            // （1799.56×1009.06；出处同 Deck 页那条注释）。判据与别页同一条：**过滤后为空**。
            // ⚠️ 本页的筛选抽屉（`Army Filter` 13 格）**还没建** ⇒ 实际上永远不空、这行字不会出现。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(170.44f, 70.94f, 1970.00f, 1080f));
                var wt = Text(ew, "There are no cardbacks in your collection for the selected filters",
                              170.44f, 1970.00f, 70.94f, 1080f, 5, PageInk, "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _cosmoEmpty = ew;
                if (_cosmoEmpty != null) _cosmoEmpty.gameObject.SetActive(CardArt.CosmeticNames().Length <= 0);
            }

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
                // 一格两层（**照原版兄弟序**）：底 = `Cardback Shadow SDF`（距离场，比卡背本体大一圈）、
                // 面 = 卡背本体。两层**各自一个渲染队列**（`QPageSdf` < `QPageRow`）—— 见梯子那段的注释。
                var tex = CardArt.Cosmetic(names[i]);
                if (tex != null)
                {
                    // 先画 SDF（它必须**在卡背底下**）。逐值出处：`资料/普查产出_0923/A4_装饰页与驱动链.md:91`
                    //   `/…/Cardback Shadow SDF` = **-42.5,-70.87,337.5,550.8**（相对卡背格 250×405）、
                    //   锚点 (-0.17,-0.185)-(1.18,1.175)（**拉伸**）、pivot (.5,.5)、`Simple + preserveAspect`、`act=T`。
                    //   ⇒ 在格子坐标里就是「左 −42.5、下 −70.87、右 337.5、上 550.8」。
                    var sdfTex = CardArt.CosmeticSdf(names[i]);
                    var sdfBase = CardView.CardbackSdfMaterialBase();
                    if (sdfTex != null && sdfBase != null)
                    {
                        // ⚠️ 表里那串是 **`x, y, w, h`**（不是 x1,y1,x2,y2 —— 同一张表第一行写 `0,0,250,405`
                        //    而那正是格子的尺寸）。y 是**向下**、相对**格左上**。
                        //    两条独立路径核过同一个矩形：① `rect = -42.5,-70.87,337.5,550.8`；
                        //    ② 锚点 (-0.17,-0.185)-(1.18,1.175) + sizeDelta (0,0)（拉伸）⇒
                        //       x: −0.17×250 = **−42.5** ✓ · y: −(1.175−1)×405 = **−70.875** ✓。
                        var sr = new PxRect(r.x1 - 42.5f, r.y1 - 70.875f, r.x1 + 295f, r.y1 + 479.925f);
                        var qs = ImageQuad.Create(cell, sdfTex, Local(cell, sr.x1, sr.y1, sr.x2, sr.y2),
                                                  LayoutSpace.Px(sr.H), new Vector2(0.5f, 0.5f), "Cardback Shadow SDF");
                        if (qs != null)
                        {
                            var mat = new Material(sdfBase);       // ⚠️ 每格一份：共享会让所有格共用最后一张掩码
                            mat.mainTexture = sdfTex;
                            qs.SetMaterial(mat);
                            qs.SetAspect(sr.W / sr.H);             // 原版 `preserveAspect`
                            qs.SetRenderQueue(QPageSdf);
                        }
                    }
                    else if (sdfTex == null)
                        Debug.LogWarning("[Collection] 卡背 SDF 取不到：" + names[i] + "_sdf"
                                       + "（跑 `工具/import_original_art.py --only-cardback-sdf` 补）");

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
            // 抽屉真值（2026-09-24 从根走下来读的；原来那套是 `Content Area` 局部值、y 少 70.94）：
            //   `Cosmetic FIlter` **0.06, 155.94 → 335.56, 1080** · `Shadow` 0.06→153.07
            //   → `Filters`(HLG) → `Spacing`(×15) → **`Owned Toggle (1)` 0.06,170.94 → 335.56,220.94**
            //     → `Image` 239.91→310.56 · `Label` 25.06→234.91
            //   → `Army Filter` 0.06,220.94 → 335.56,565.94（`Title` 25.06,225.94→335.56,275.94 ·
            //      `Content`(LayoutGroup) 0.06,285.94→335.56,565.94）—— **仍然没建**：格子的尺寸还是没有
            var d = Node(page, "Cosmetic FIlter", new PxRect(0.06f, 155.94f, 335.56f, 1080f));
            _cosmoDrawer = d;
            Rect(d, "40k_main_tab_shadow", new PxRect(0.06f, 155.94f, 153.07f, 1080f), "Shadow", QFlt, new Color(0f, 0f, 0f, 0.314f));
            Rect(d, "40k_main_tab_background", new PxRect(0.06f, 155.94f, 335.56f, 1080f), "Panel", QFlt);
            // `Filters`（**HLG**，不是 VLG）+ 两行：`Spacing`(335.5×15) / `Owned Toggle (1)`(335.5×50)
            Rect(d, "40_main_bt_toggle_on", new PxRect(239.91f, 170.94f, 310.56f, 220.94f), "Owned Image", QFltRow, null, true);
            var lb = Text(d, "Owned only", 25.06f, 234.91f, 170.94f, 220.94f, 5, PageInk, "Owned Label", 32f);
            if (lb != null) lb.SetRenderQueue(QFltText);
            AddHit(d, "OwnedHit", new PxRect(0.06f, 170.94f, 335.56f, 220.94f), QFltHit,
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

        // ============================================================ Styles 页（异画；A4 §三）
        //
        // 原版 = `Alternate Art Tab`（脚本 `AlternateArtCardCollectionTab`）→ `Collection Display`（`CardCollectionDisplay`）。
        // 🔴 **本节的坐标是 2026-09-24 从 `Collection Menu Variant` 根节点一路走下来实读的**。
        //    上一轮那批（A4）是**被 `menu_rect.py` 的父链 bug 平移过**的（整页偏 (167.17, 70.94)），
        //    坑与判据见 `资料/已知的坑.md` 那条「`menu_rect.py` 会把 `Content Area` 下的节点整套平移」。
        //
        //   · `Collection Display/Scroll View` **330.22, 287.67 → 1920.01, 1080**（1589.78 × 792.33）
        //   · 格 = `Collection Card`（作者 350×512）→ 槽 **262.5 × 384** · 行距 384
        //     · 列数 = `floor(1589.78 ÷ 262.5)` = **6** · 内容**整体居中**：pad = **7.39**
        //       ⇒ 首格 **337.61, 287.67 → 600.11, 671.67**（中心 **468.86, 479.67**）
        //   · `Header`（**换风格条**，不是标题栏）332.35, 155.94 → 1920.00, 283.94（1587.66×128）
        //     ├ `Select Art Button Right` **1320.40, 190.67 → 1394.78, 266.27**（74.39×75.61）
        //     │   ⚠️ 它的**脚本字段名是 `leftStyleButton`**（挂在 `Select Art Button Right` 上）——
        //     │      **接线按位置、不按字段名**（A4 §五·4；2026-09-24 复核确认）
        //     ├ `Select Art Button Left`  **683.40, 190.67 → 757.78, 266.27**
        //     │   两钮的子件一样：`Background` = `40k_general_bt_yellow` · `Icon` = `40k_general_bt_arrow`；
        //     │   **左钮的 Icon 带 `UIFlippable(m_Horizontal=1)`** ⇒ 同一张图**镜像**，别去找左箭头
        //     ├ `Art Style Logo` **787.59, 155.94 → 1299.59, 283.94**（512×128）
        //     └ `Separator Line (1)` **339.44, 281.86 → 1920.01, 290.44**
        //   · 左抽屉 `Card Filters` **0.25, 155.94 → 335.56, 1080**（与 Cards 页**同矩形**、同 7 行）
        //
        // ---- 我们挑的 / 没建的（逐条出声）----
        //   · **风格图 SO 本地没有**：`Art Style Logo` 原版 `sprite=0`，运行时由 `styleImage` 载
        //     `alternateArtStyles[i]` 的 Addressable，而 `AlternateArtStyleIconsSO` 全库**只有类名没有资产**
        //     ⇒ 我们在那一格里**画风格名文字**（**我们挑的做法，不是原版的**）。
        //   · **风格清单**：本机只覆盖 `AA_HB`(6 张) + `v2`(1 张)，其余风格在远端 CCD 的 `alternateartstyles` 包。
        //     `v2` 的**显示名查不到** ⇒ 直接印 token。
        //   · 点一张卡：原版开 `CardDisplayWindow`（卡片详情窗）—— **那扇窗还没建** ⇒ `NotifyNotBuilt`。
        //   · `Card Filters` 的 `Army Filter` 一格仍然没建（A4 没给格子尺寸）。

        /// <summary>网格视口 **330.22, 287.67 → 1920.01, 1080**（1589.78 × 792.33）。</summary>
        public static readonly PxRect StyleView = new PxRect(330.22f, 287.67f, 1920.01f, 1080f);
        /// <summary>格 **262.5 × 384**（作者 350×512，同 Cards 页）。</summary>
        public const float StyleCellW = 262.5f, StyleCellH = 384f;

        /// <summary>列数 = `floor(1589.78 ÷ 262.5)` = **6**。</summary>
        public static int StyleCols { get { return Mathf.Max(1, Mathf.FloorToInt(StyleView.W / StyleCellW)); } }
        /// <summary>内容**整体居中**的左边距 = (1589.78 − 6×262.5) × 0.5 = **7.39**。</summary>
        public static float StylePadX { get { return (StyleView.W - StyleCols * StyleCellW) * 0.5f; } }

        public static PxRect StyleCellRect(int i)
        {
            int r = i / StyleCols, c = i % StyleCols;
            float x = StyleView.x1 + StylePadX + c * StyleCellW;
            float y = StyleView.y1 + r * StyleCellH;
            return new PxRect(x, y, x + StyleCellW, y + StyleCellH);
        }

        // ---- 换风格条（A4 §三 `Header`）----
        public const float StyleArrowW = 74.39f, StyleArrowH = 75.61f;
        public const float StyleArrowY1 = 190.67f, StyleArrowY2 = 266.27f;
        public const float StyleArrowLx = 683.40f, StyleArrowRx = 1320.40f;
        public const float StyleLogoL = 787.59f, StyleLogoR = 1299.59f;
        public const float StyleBarT = 155.94f, StyleBarB = 283.94f;
        public const float StyleSep1L = 339.44f, StyleSep1T = 281.86f, StyleSep1B = 290.44f;
        /// <summary>圆钮里 `Background`/`Icon` 那两层（原版 `sd=(0.3774,0.3774)` 等比）56.86×58.13，居中。</summary>
        public const float StyleArrowIconW = 56.86f, StyleArrowIconH = 58.13f;

        /// <summary>本机能拿到的督军异画（**7 张 · 2 种风格**）。表与出处 = `工具/import_original_art.py` 的 `ALT_ART`。</summary>
        public struct AltArtCard
        {
            public readonly string CardId, Style;
            public AltArtCard(string id, string style) { CardId = id; Style = style; }
        }

        public static readonly AltArtCard[] AltArtCards =
        {
            new AltArtCard("AM5",                 "AA_HB"),
            new AltArtCard("BL1",                 "AA_HB"),
            new AltArtCard("SAU1",                "AA_HB"),
            new AltArtCard("GOF3",                "AA_HB"),
            new AltArtCard("SW1",                 "AA_HB"),
            new AltArtCard("UM_Lieutenant_Titus", "AA_HB"),
            new AltArtCard("DA3",                 "v2"),
        };

        /// <summary>风格顺序（**表里第一次出现的次序**，稳定）。</summary>
        public static string[] AltStyles
        {
            get
            {
                var list = new List<string>();
                foreach (var a in AltArtCards) if (!list.Contains(a.Style)) list.Add(a.Style);
                return list.ToArray();
            }
        }

        /// <summary>某个风格的**显示名**。`AA_HB` = **Hammer and Bolter**（`AlternateArtStyleIconsSO/HammerAndBolter`，
        /// 出处 `资料/索引与盘点/解包资源使用地图.md:1174`）；其余**查不到**（风格 SO 在服务端）⇒ 原样印 token。</summary>
        public static string StyleLabel(string style)
        {
            if (style == "AA_HB") return "Hammer and Bolter";
            return style;
        }

        /// <summary>当前风格下标（0 起）。**自检也读它**。</summary>
        public int StyleIndex;
        /// <summary>**画面上真会出现的异画张数** = 「属于当前风格」∩「过了筛选」—— 自检用。
        /// ⚠️ 别拿 `StylesState.VisibleCards().Count` 充数：那是**两种风格合起来**的池子
        /// （第一版就这么断的，量出 7 而画面只有 6）。</summary>
        public int StyleVisibleCount { get { return ShownAltArts().Count; } }
        /// <summary>异画页的滚动区（自检用；7 张 = 2 行 ⇒ 本页**滚不动**，如实记）。</summary>
        public MenuScroll StyleScroll;
        public readonly List<Transform> StyleCells = new List<Transform>();

        static DeckEditorState _stylesState;
        /// <summary>异画那一批卡的筛选状态 —— **另一份 `DeckEditorState`**（筛的是异画这批，不是卡池）。
        /// 复用同一套 `DeckFilter` / `VisibleCards()` ⇒ 筛选栏那 7 行**一行代码都不用重写**。</summary>
        public static DeckEditorState StylesState
        {
            get
            {
                if (_stylesState == null)
                {
                    var pool = new List<CardDef>();
                    foreach (var a in AltArtCards)
                    {
                        var c = CollectionData.Card(a.CardId);
                        if (c != null) pool.Add(c);
                        else Debug.LogWarning("[Collection] 异画表里的卡 id 在卡池里查不到：" + a.CardId);
                    }
                    _stylesState = new DeckEditorState(pool);
                }
                return _stylesState;
            }
        }
        /// <summary>自检用：丢掉异画那两份缓存。</summary>
        public static void ResetStylesForTest() { _stylesState = null; }

        /// <summary>当前风格下的异画（按 `AltArtCards` 的次序，**不受筛选影响**）。</summary>
        public List<AltArtCard> CurrentStyleAltArts()
        {
            var list = new List<AltArtCard>();
            string st = AltStyles[Mathf.Clamp(StyleIndex, 0, AltStyles.Length - 1)];
            foreach (var a in AltArtCards) if (a.Style == st) list.Add(a);
            return list;
        }

        /// <summary>**画面上真会出现的那几张** = 「属于当前风格」∩「过了筛选」（原版也是这两个条件叠加）。
        /// `RebuildStyleCells` 与 `StyleVisibleCount` **共用这一份**（两处各写一遍迟早不一致）。</summary>
        public List<AltArtCard> ShownAltArts()
        {
            var visible = StylesState.VisibleCards();
            var show = new List<AltArtCard>();
            foreach (var a in CurrentStyleAltArts())
                foreach (var c in visible)
                    if (c.Id == a.CardId) { show.Add(a); break; }
            return show;
        }

        public void BuildStylesPage(Transform page)
        {
            // ---- 页头 = **共用那一行**（本页字号 42 / 42，逐页不同）----
            BuildFilterHeader(page, 42f, 42f, ClearStyleFilters, ToggleStyleFilters);

            // ---- 换风格条 `Header` ----
            // 🔴 **按位置接线**：左边那颗 = 上一个、右边那颗 = 下一个
            //    （脚本字段名反着：`leftStyleButton` 挂的是**右边**那颗）
            BuildStyleArrow(page, StyleArrowLx, true, () => ChangeStyle(-1));
            BuildStyleArrow(page, StyleArrowRx, false, () => ChangeStyle(+1));
            // `Art Style Logo`：原版运行时喂图（**风格图标 SO 本地没有**）⇒ 我们画风格名文字（**我们挑的**）
            // 🔴 **必须限宽自适应** —— 那一格是 **512×128**，`Hammer and Bolter` 按 56px 画出来宽 ≈1270px，
            //    **直接压到右箭钮上**（第一版实拍一眼可见）。`SetAutoFitBox` 按框宽缩到放得下为止。
            //    ⚠️ 判据要量**渲染宽度**（`Label.WorldW`），不是比字号 —— 见 `CLAUDE.md` §二 的 `AutoFitBox` 教训。
            _styleLogo = Text(page, StyleLabel(CurrentStyleName()), StyleLogoL, StyleLogoR, StyleBarT, StyleBarB,
                              6, PageInk, "Art Style Logo", 56f);
            if (_styleLogo != null)
            {
                _styleLogo.SetRenderQueue(QPageText);
                _styleLogo.SetAutoFitBox(LayoutSpace.Px(StyleLogoR - StyleLogoL),
                                         LayoutSpace.Px(StyleBarB - StyleBarT), 18f, 56f);
            }
            Rect(page, "40k_main_line", new PxRect(StyleSep1L, StyleSep1T, 1920.01f, StyleSep1B),
                 "Separator Line (1)", QPageRow);

            // ---- 网格 ----
            var holder = Node(page, "Scroll View", StyleView);
            Node(holder, "Viewport", StyleView);
            int n = StylesState.VisibleCards().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)StyleCols));
            StyleScroll = MenuScroll.TopAligned(StyleView, rows * StyleCellH);
            StyleScroll.Owner = gameObject;
            StyleScroll.OnChanged = () => RebuildStyleCells(holder);
            PointerLayer.RegisterScroll(StyleScroll);
            RebuildStyleCells(holder);

            // `Empty Collection Warning`：原版 `act=F`，运行期条件 = **过滤后为空**
            // （`CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0 ⇒ SetActive`）
            {
                var ew = Node(page, "Empty Collection Warning", StyleView);
                var wt = Text(ew, "There are no cards in your collection for the selected filters",
                              StyleView.x1, StyleView.x2, StyleView.y1, StyleView.y2, 5, PageInk,
                              "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _styleEmpty = ew;
            }

            // 左抽屉（**出厂 act=T = 展开**）—— 与 Cards 页**同一份实现**，只是状态与刷新对象不同
            _fltStyles = BuildFilterPanel(page, StylesState, RefreshStylesAfterFilter, true);
            RefreshStyleEmpty();
        }

        Transform _styleEmpty;
        /// <summary>`Art Style Logo` 那一格里的字（原版是图；**待替换成风格图标 SO**，本地没有）—— 自检用它量渲染宽度。</summary>
        Label _styleLogo;
        /// <summary>自检用：`Art Style Logo` 那段字的渲染宽度（画布 px）。**判「有没有溢出那一格 512 宽」。**</summary>
        public float StyleLogoWidthPx { get { return _styleLogo != null ? _styleLogo.WorldW * 108f : 0f; } }

        string CurrentStyleName()
        {
            var s = AltStyles;
            return s.Length == 0 ? "" : s[Mathf.Clamp(StyleIndex, 0, s.Length - 1)];
        }

        /// <summary>一个换风格圆钮：`UI_Button_Round_background`（圆底）+ `40k_general_bt_yellow` + `40k_general_bt_arrow`。
        /// 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— 我们翻转 uv 的 x。</summary>
        void BuildStyleArrow(Transform page, float x1, bool mirror, System.Action onClick)
        {
            string side = mirror ? "Left" : "Right";
            var r = new PxRect(x1, StyleArrowY1, x1 + StyleArrowW, StyleArrowY2);
            var node = Node(page, "Select Art Button " + side, r);
            Rect(node, "UI_Button_Round_background", r, "Background Round", QPagePanel, null, true);
            float ix = x1 + (StyleArrowW - StyleArrowIconW) * 0.5f;
            float iy = StyleArrowY1 + (StyleArrowH - StyleArrowIconH) * 0.5f;
            var ir = new PxRect(ix, iy, ix + StyleArrowIconW, iy + StyleArrowIconH);
            Rect(node, "40k_general_bt_yellow", ir, "Background", QPageRow, null, true);
            // 🔴 **Icon 必须比 Background 高一层队列** —— 两层摆在同一个矩形上，同队列时
            //    「谁盖谁不可控」会把箭头盖掉（2026-09-24 实拍：两颗钮里只看得见黄底、箭头没出现，
            //    而**矩形断言全绿**）。同族坑见 `资料/已知的坑.md`「同一个渲染队列的两层」。
            var q = Rect(node, "40k_general_bt_arrow", ir, "Icon", QPageOverlay, null, true);
            // 左钮**镜像**（原版 `UIFlippable(m_Horizontal=1)`）—— uv 的 u 从 1 到 0
            if (q != null && mirror) q.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            // ⚠️ 名字要**带左右**：两颗钮各有一个 `ArrowHit`，同名的话自检按名字找只会拿到第一颗
            //    （`FindChild` 是按名字找的，传路径进去恒 null —— `资料/已知的坑.md`）
            AddHit(node, "ArrowHit " + side, r, QPageRow, onClick);
        }

        /// <summary>切风格（`ChangeStyleButton(bool isLeft)`：**isLeft=true = 上一个(−1)**，越界回绕）。</summary>
        public void ChangeStyle(int dir)
        {
            var s = AltStyles;
            if (s.Length == 0) return;
            StyleIndex = ((StyleIndex + dir) % s.Length + s.Length) % s.Length;
            Debug.Log("[Collection] 切风格 → 「" + StyleLabel(s[StyleIndex]) + "」（第 " + (StyleIndex + 1) + "/" + s.Length + " 种）");
            RebuildStylesPage();
        }

        /// <summary>重建整个 Styles 页（切风格 / 筛选变了调它）。</summary>
        public void RebuildStylesPage()
        {
            var page = PageRoot(3);
            if (page == null) return;
            for (int i = page.childCount - 1; i >= 0; i--) DestroySafe(page.GetChild(i).gameObject);
            _styleEmpty = null;
            _styleLogo = null;
            BuildStylesPage(page);
        }

        /// <summary>异画页的刷新回调（筛选栏变了调它）—— 与 Cards 页那份是**同一个函数指针位置**，只是重画的东西不同。</summary>
        void RefreshStylesAfterFilter()
        {
            var page = PageRoot(3);
            if (page == null) return;
            var holder = page.Find("Scroll View");
            if (holder != null)
            {
                if (StyleScroll != null) StyleScroll.SetOffset(0f);
                RebuildStyleCells(holder);
            }
            if (_fltStyles != null) RebuildFilterRows(_fltStyles);
            RefreshStyleEmpty();
        }

        void RefreshStyleEmpty()
        {
            if (_styleEmpty != null) _styleEmpty.gameObject.SetActive(StylesState.VisibleCards().Count <= 0);
        }

        void RebuildStyleCells(Transform holder)
        {
            var vp = holder.Find("Viewport");
            var parent = vp != null ? vp : holder;
            for (int i = parent.childCount - 1; i >= 0; i--) DestroySafe(parent.GetChild(i).gameObject);
            StyleCells.Clear();

            var show = ShownAltArts();

            var prevClip = Clip;
            Clip = StyleView;
            float scale = StyleCellH / (CardView.Height * 108f);      // 按**卡位高 384** 反解（同 Cards 页）
            for (int i = 0; i < show.Count; i++)
            {
                var a = show[i];
                var card = CollectionData.Card(a.CardId);
                if (card == null) continue;
                var content = StyleCellRect(i);
                var r = StyleScroll.Shift(content);
                if (!StyleScroll.Intersects(r)) continue;

                var d = BattleDriver.ToCardData(card, card.Faction);
                // 🔴 **立绘换成异画**（`CardData.artOverride`）—— 卡框/数值/名字照旧用原卡
                d.artOverride = CardArt.AltArt(a.CardId);
                if (d.artOverride == null)
                    Debug.LogWarning("[Collection] 异画立绘取不到（卡面会退回普通立绘）：" + a.CardId);

                var v = CardView.Create(parent, d, "CollectionAltArt_" + a.CardId);
                if (v == null) continue;
                v.gameObject.SetActive(true);
                v.SetPose(Local(parent, r.x1, r.y1, r.x2, r.y2), 0f, scale);
                v.SetData(d);
                v.SetFace(CardFace.Full);
                v.SetHighlight(CardHighlightState.Normal);
                StyleCells.Add(v.transform);
                AddHit(parent, "AltArtHit_" + a.CardId, r, QPageRow, () => OpenCardDetail(card));
            }
            Clip = prevClip;
        }

        // ============================================================ 筛选栏：建 / 刷 / 点
        //
        // 🔴 **2026-09-24 收口**：这一整套（建 / 开合 / 清空 / 点格 / 重画）**只有一份实现**，
        //    靠模块级的 `_flt` 指针决定作用在 **Cards 页**还是 **Styles 页**那份上。
        //    起因：异画页的左抽屉在原版里是**同一个矩形、同一套 7 行**（实测都是
        //    `Card Filters` **0.25,155.94 → 335.56,1080**）⇒ 再抄一份就是铁律说的「两处写同一条规则」。

        /// <summary>**Cards 页**的筛选栏开着没有（`Filters` 圆钮开合）。原版默认**收起**。</summary>
        public bool FiltersOpen { get { return _fltCards != null && _fltCards.Open; } }
        /// <summary>**Cards 页**筛选栏那一列的滚动量（自检用）。</summary>
        public MenuScroll FilterScroll { get { return _fltCards != null ? _fltCards.Scroll : null; } }
        /// <summary>**Cards 页**画出来的筛选格数（自检用）。</summary>
        public int FilterCellCount { get { return _fltCards != null ? _fltCards.Cells.Count : 0; } }
        /// <summary>**Styles 页**的抽屉开着没有（自检用）。出厂就展开（实证 act=T）。</summary>
        public bool StyleFiltersOpen { get { return _fltStyles != null && _fltStyles.Open; } }

        /// <summary>建一页的左侧筛选栏。**起手收起**（原版靠 `hiddenPosition` 滑出去，我们整块显隐）；
        /// <paramref name="openAtStart"/> = 出厂就展开（**异画页是 act=T**）。
        /// 返回这一页那份 <see cref="FilterPanel"/>。</summary>
        FilterPanel BuildFilterPanel(Transform page, DeckEditorState state, System.Action onChanged,
                                            bool openAtStart)
        {
            var p = new FilterPanel { State = state, OnChanged = onChanged, Open = openAtStart };
            Scope(p, () =>
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
                _fltScroll.OnChanged = () => RebuildFilterRows(p);
                PointerLayer.RegisterScroll(_fltScroll);

                RebuildFilterRows(p);                        // **先建**（内部会临时激活 —— TMP 量不到非激活对象）
                panel.gameObject.SetActive(p.Open);          // 再按状态显隐
            });
            return p;
        }

        /// <summary>`Filters` 圆钮 / 自检：开合筛选栏。</summary>
        public void ToggleFilters() { Scope(_fltCards, ToggleFiltersNow); }
        /// <summary>异画页的左抽屉开合（`Filter Toggle`）。</summary>
        public void ToggleStyleFilters() { Scope(_fltStyles, ToggleFiltersNow); }

        void ToggleFiltersNow()
        {
            _fltOpen = !_fltOpen;
            if (_fltPanel != null) _fltPanel.gameObject.SetActive(_fltOpen);
            Debug.Log("[Collection] 筛选栏 " + (_fltOpen ? "打开" : "收起")
                      + "（原版是**滑进滑出**：`hiddenPosition=(-550,0)`、`animationTime=0.3` ——"
                      + " 我们做的是整块显隐，**出声**，见 `Shell/CollectionWindow.cs` 筛选栏那段注释）");
        }

        /// <summary>清空筛选（`Clear filters` 钮）。原版回到「不限」那一套。</summary>
        public void ClearCardFilters() { Scope(_fltCards, ClearFiltersNow); }
        /// <summary>异画页的 `Clear filters`。</summary>
        public void ClearStyleFilters() { Scope(_fltStyles, ClearFiltersNow); }

        void ClearFiltersNow()
        {
            if (PointerLayer.Instance != null && PointerLayer.Instance.TextEditing) PointerLayer.Instance.EndText(false);
            FltState.SetFilter(DeckFilter.None);
            _flt.OnChanged();                       // 各页自己决定重画什么（Cards：卡池 + 计数条；Styles：异画格）
            Debug.Log("[Collection] 已清空筛选");
        }

        /// <summary>点了一格筛选（`$name` / `$fac:x` / `$rar:x` / `$cost:i` / `$type:x`）—— **Cards 页用**。</summary>
        public void ApplyCardFilter(string key) { Scope(_fltCards, () => ApplyFilter(key)); }
        /// <summary>异画页点了一格筛选。</summary>
        public void ApplyStyleFilter(string key) { Scope(_fltStyles, () => ApplyFilter(key)); }

        /// <summary>自检入口：**直接给 Cards 页设一个筛选**（走的是**同一条**刷新路：`SetFilter` + `OnChanged`）。
        /// 为什么需要：批处理里**敲不出键盘**，而「筛到空结果 ⇒ 出 `Empty Collection Warning`」这条
        /// 只能用空结果去触发（拿 `$name` 那条路要 `PointerLayer` 的文本输入，批处理下没有）。</summary>
        public void UiSetCardFilter(DeckFilter f)
        {
            Scope(_fltCards, () => { FltState.SetFilter(f); _flt.OnChanged(); });
        }

        /// <summary>点了一格筛选（`$name` / `$fac:x` / `$rar:x` / `$cost:i` / `$type:x`）。</summary>
        void ApplyFilter(string key)
        {
            if (string.IsNullOrEmpty(key)) return;
            if (key == "$name")
            {
                var pl = PointerLayer.Instance;
                if (pl == null) return;
                // 🔴 **键盘那三个回调是「后来」才跑的**（每次按键 / 回车 / ESC）—— 那时 `_flt` 早就还原了
                //    ⇒ **必须先把这一份面板捕获下来**，回调里一律走 `owner`，不许再碰 `_flt`。
                var owner = _flt;
                pl.BeginText(FltState.Filter.Name ?? "", 24,
                             s => Scope(owner, () => { var f0 = owner.State.Filter; f0.Name = (s ?? "").Trim(); owner.State.SetFilter(f0); owner.OnChanged(); Debug.Log("[Collection] 卡名筛选：" + (string.IsNullOrEmpty(f0.Name) ? "（清空）" : f0.Name)); }),
                             () => RebuildFilterRows(owner),
                             s => { var t = owner.Node != null ? owner.Node.Find("Scroll View/Viewport/Name Filter/Input Text") : null; var lb = t != null ? t.GetComponent<Label>() : null; if (lb != null) lb.SetText(s + "_"); });
                Debug.Log("[Collection] 卡名筛选：输入后回车确认，ESC 取消");
                return;
            }
            if (key == "$owned") { Debug.Log("[Collection] 全部卡牌均已拥有（单机全解锁）⇒ 这个开关不改变结果"); RebuildFilterRows(_flt); return; }
            if (key == "$upgradable") { Debug.Log("[Collection] Upgradable only：单机版没有升级系统（不装作有）"); return; }

            var f = FltState.Filter;
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
            FltState.SetFilter(f);
            _flt.OnChanged();
        }

        /// <summary>**Cards 页**改完筛选：重画卡池（回到顶部）+ 重刷筛选栏选中态 + 重算计数条。</summary>
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
            if (_fltCards != null) RebuildFilterRows(_fltCards);
            RefreshCardsEmpty();       // 「过滤后为空」那条提示跟着筛选走（与 Styles 页同一条判据）
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
        void RebuildFilterRows(FilterPanel p)
        {
            if (p == null) return;
            var prev = _flt;
            _flt = p;
            try
            {
                RebuildFilterRowsNow();
            }
            finally { _flt = prev; }
        }

        void RebuildFilterRowsNow()
        {
            var panel = _flt.Node;
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
                    var key = c.Key;
                    // 🔴 **点击回调必须带上"这是哪一份面板"** —— `ApplyFilter` 读的是模块级的 `_flt`，
                    //    而点击发生在**建完之后**（那时 `_flt` 已经还原了）。第一版漏了这一步，
                    //    点一格筛选就 `NullReferenceException`（自检当场抓到）。
                    var owner = _flt;
                    AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter(key)));
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
            string cur = FltState.Filter.Name;
            bool editing = PointerLayer.Instance != null && PointerLayer.Instance.TextEditing;
            string txt = editing ? (PointerLayer.Instance.TextBuffer + "_")
                                 : (string.IsNullOrEmpty(cur) ? "Search" : cur);
            var tr = _fltScroll.Shift(new PxRect(FltL + 37.15f, FltT + 26.5f, FltL + 268.45f, FltT + 53.5f));
            TextAligned(cell, txt, tr, PageInk, "Input Text", 30f, false);

            // 尾图标 `40k_icon_search` 35×30（面板内 268.35,24.5 → 303.35,54.5）
            var ir = _fltScroll.Shift(new PxRect(FltL + 268.35f, FltT + 24.5f, FltL + 303.35f, FltT + 54.5f));
            Rect(cell, "40k_icon_search", ir, "Search Icon", QFltRow, null, true);

            // ⚠️ 同 `RebuildFilterRowsNow` 那一条：点击回调要**带上这一份面板**，别用 `ApplyCardFilter`
            //    （那个写死了作用在 Cards 页那份上）
            var owner = _flt;
            AddHit(cell, "Hit", r, QFltHit, () => Scope(owner, () => ApplyFilter("$name")));
        }

        /// <summary>后 6 行的格子表。**坐标一律「面板内」写、出口处加 `FltL/FltT` 换成页面绝对** ——
        /// 🔴 2026-09-23 踩过：最初模型里一半加了 `FltT` 一半没加，而 `RebuildFilterRows` 是**按绝对坐标摆**的
        /// ⇒ **整排偏上 155.9px**，搜索框干脆落到视口外**根本没建**（8 条断言把它抓出来）。
        /// 出处逐条见 `FR_*` 与 A3 §五·1。</summary>
        void BuildFilterRowModel()
        {
            var f = FltState.Filter;
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
            var facs = FltState.Factions();
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

        // ============================================================ 四页共用的那一条页头
        //
        // 🔴 **2026-09-24 抽出来（铁律「两处写同一条规则 = 迟早不一致」）**：Cards / Cosmetics / Styles
        //    三页的页头在**原版里是同一行** —— 实读（从 `Collection Menu Variant` 根一路走下来）：
        //      · `Filter Toggle`  `40k_menu_bt`          **367.17, 88.44 → 417.17, 138.44**（50×50）
        //      · ├ `icon detail` `40k_bt_icon_search`    377.17, 98.44 → 407.17, 128.44（SD −20 ⇒ 30×30）
        //      · ├ `label`       "Filters"              437.17, 88.44 → 587.17, 138.44
        //      · `Separator Line` `40k_main_line`        167.17,150.94 → 1920.01,160.94
        //      · `Filters`（GridLayoutGroup）→ `Clear Filter Button` `UI_Button_Mulligan` **250×60**
        //        ⇒ 真值 **612.17, 83.44 → 862.17, 143.44**（**四页都是这个 x**，理由见下）
        //    ⚠️ **字号逐页不同**（原版每页是自己的实例值）：Deck 42/33 · Cards 42/42 · Cosmetics 35/33 · Styles 42/42
        //       ⇒ **做成参数**，不许当成全局常量（`阶段二_卡组线_原版规格.md` §三 那句「别拿一个数当全部情况」）。
        //
        // 🔴 `Clear filters` 的 x 为什么是 612.17 而不是 Cardback 页序列化里的 **1488.59**：
        //    `Header/Filters` 是 `GridLayoutGroup`(cell 85×70 · FixedRowCount 1 · MiddleLeft) + `CSF(H=Preferred)`，
        //    而唯一子件 `Clear Filter Button` 带 **`LayoutElement.m_IgnoreLayout = 1`** ⇒ **布局根本不排它**，
        //    容器宽度**算出来是 0** ⇒ 按钮按自己的锚 `(1,.5) pos=(20,0)` 落回**容器左缘 592.17 + 20**。
        //    Deck 页的容器宽实测 **−0.00**、Styles 页 **−0.17**、Cards 页同族 —— 都是 0；
        //    Cardback 页序列化里那个 **876.42** 是**布局跑之前的模板值**（同 2026-09-23 第 40 条那次错法）。
        //    判据：照 1488.59 摆，它会和**右对齐到 1821 的标题**压在一起 —— 原版不会这样。

        /// <summary>页头那条分隔线（`Header/Separator Line`，四页同值）。</summary>
        public const float HdrSepT = 150.94f, HdrSepB = 160.94f;

        /// <summary>建一条**共用页头**：`Filters` 圆钮 + `icon detail` + `label` + 分隔线 + `Clear filters`。
        /// <paramref name="filterPx"/> / <paramref name="clearPx"/> = **本页自己的**字号（原版逐页不同）。</summary>
        public void BuildFilterHeader(Transform page, float filterPx, float clearPx, System.Action onClear,
                                      System.Action onToggle)
        {
            Rect(page, "40k_menu_bt", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS),
                 "Filters Button", QPagePanel);
            Rect(page, "40k_bt_icon_search", new PxRect(FltBtnX + 10f, FltBtnY + 10f, FltBtnX + 40f, FltBtnY + 40f),
                 "Filters Icon", QPageRow, null, true);
            var fltLab = Text(page, "Filters", 437.2f, 587.2f, FltBtnY, FltBtnY + FltBtnS, 5, PageInk,
                              "Filters Label", filterPx);
            if (fltLab != null) fltLab.SetRenderQueue(QPageText);
            if (onToggle != null)
                AddHit(page, "FiltersHit", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS + 220f, FltBtnY + FltBtnS),
                       QPageRow, onToggle);

            Rect(page, "40k_main_line", new PxRect(167.17f, HdrSepT, 1920.01f, HdrSepB), "Separator Line", QPageRow);

            Rect(page, "UI_Button_Mulligan", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                 "Clear filters", QPageRow);
            var clrLab = Text(page, "Clear filters", ClearFltX, ClearFltX + ClearFltW, ClearFltY, ClearFltY + ClearFltH,
                              5, PageInk, "Clear filters Text", clearPx);
            if (clrLab != null) clrLab.SetRenderQueue(QPageText);
            if (onClear != null)
                AddHit(page, "ClearFiltersHit",
                       new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH), QPageRow, onClear);
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
            // 🔴 2026-09-24 补：**`Filters` 圆钮原来没有命中区** ⇒ 玩家点它没反应
            //    （§三 第 15 条 第 49 行）。原版它开的就是本页的左抽屉 `Deck Filters`。
            AddHit(page, "FiltersHit", new PxRect(FltBtnX, FltBtnY, FltBtnX + FltBtnS, FltBtnY + FltBtnS), QPageRow,
                   ToggleDeckFilters);
            AddHit(page, "ClearFltHit", new PxRect(ClearFltX, ClearFltY, ClearFltX + ClearFltW, ClearFltY + ClearFltH),
                   QPageRow, ClearDeckFilters);
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

            // `Empty Collection Warning`：**原版四个页各有一份、矩形各不相同**（2026-09-24 直读
            // `menu_rect.py bundle_menus_assets_all "Collection Menu Variant" --depth 8`）：
            //   Deck `165.88,70.94 → 1970.01,1080`（1804.12×1009.06）· Cards `135.22,…`（1834.78）
            //   · Cosmetics `170.44,…`（1799.56）· Styles `330.22,287.67 → 1920,1080`（= 它自己的视口矩形）。
            // ⚠️ 原来那条「Deck 页没有这一件」是**没查到**，不是不存在。
            {
                var ew = Node(page, "Empty Collection Warning", new PxRect(165.88f, 70.94f, 1970.01f, 1080f));
                var wt = Text(ew, "There are no decks in your collection", 165.88f, 1970.01f, 70.94f, 1080f, 5, PageInk,
                              "Warning", 36f);
                if (wt != null) wt.SetRenderQueue(QPageText);
                _deckEmpty = ew;
            }
            RefreshDeckEmpty();
            BuildDeckFilterDrawer(page);
        }

        // ============================================================ Deck 页的左抽屉 `Deck Filters`（2026-09-24）
        //
        // 出处（**直读**，不是推的）：正本 §三 + `menu_rect.py bundle_menus_assets_all "Deck Filters" --depth 6`
        //   · `Deck Filters` **0.06,155.94 → 335.56,1080**（335.50 × 924.06）
        //   · 兄弟 `Shadow` 宽 **153.01**
        //   · 里面 `Filters`(LayoutGroup) = `Deck Name Filter`(335.50×80) + `Army Filter`(335.50×345)
        //     — `Deck Name Filter/Input Field` **281.28×40 @(27.17,175.94)**，尾部一个 35×30 的 `Image`
        //       （Cards 页同族那份是 `40k_icon_search` 放大镜）
        //     — `Army Filter` = `Title`(310.50×50) + `Content`(GridLayoutGroup)，格模板 **100×100**
        // ⚠️ **两个 LayoutGroup 的 spacing/pad 没读**（`Filters` 与 `Content` 都是布局组，
        //    `menu_rect` 给的是**布局跑之前的模板位**）⇒ 这里照 **Cards 页 Army 那一行**（同族、已实读）：
        //    cell 100×100 · spacing 7/0 · pad 14（**3 格一行**）。
        // ⚠️ 原版这一页的 `Deck Filters` **出厂 act=? 没读到** —— 我们起手**收起**（与 Cards 页一致，
        //    而且它的入口就是页头那颗 `Filters`）。**这是我们挑的**。
        // ⚠️ `Deck Name Filter` 那条输入：批处理里没有键盘，走 `PointerLayer.BeginText`（Play 里能敲）。
        const float DfltL = 0.06f, DfltT = 155.94f, DfltR = 335.56f, DfltB = 1080f, DfltShadowW = 153.01f;
        const float DNameT = 155.94f, DNameH = 80f;
        const float DInputL = 27.17f, DInputT = 175.94f, DInputW = 281.28f, DInputH = 40f;
        const float DArmyT = 235.94f, DArmyH = 345f;
        const float DTitleL = 25.06f, DTitleT = 240.94f, DTitleW = 310.50f, DTitleH = 50f;
        const float DCellT = 300.94f, DCellS = 100f, DCellGap = 7f, DCellPad = 14f;

        public static readonly PxRect DeckFltView = new PxRect(DfltL, DfltT, DfltR, DfltB);

        Transform _deckFltPanel;
        Label _deckFltNameTx;
        /// <summary>Deck 页的筛选（**只管卡组列表**，与 Cards 页那套卡牌筛选是两回事）：空串 = 不限。</summary>
        string _deckFacFilter = "", _deckNameFilter = "";

        public bool DeckFiltersOpen { get { return _deckFltPanel != null && _deckFltPanel.gameObject.activeSelf; } }
        public string DeckFacFilter { get { return _deckFacFilter; } }
        public string DeckNameFilter { get { return _deckNameFilter; } }
        /// <summary>自检用：画出来的卡组格数（筛选后）</summary>
        public int DeckCellCount { get { return DeckCells.Count; } }

        void BuildDeckFilterDrawer(Transform page)
        {
            var panel = Node(page, "Deck Filters", DeckFltView);
            // 兄弟序照原版：`Shadow` **先**、面板本体**后** ⇒ 面板压在影子上
            Rect(panel, "40k_main_tab_shadow", new PxRect(DfltL, DfltT, DfltL + DfltShadowW, DfltB), "Shadow", QFlt,
                 new Color(0f, 0f, 0f, 0.314f));
            Rect(panel, "40k_main_tab_background", DeckFltView, "Panel", QFlt);
            var filters = Node(panel, "Filters", DeckFltView);

            // ---- ① `Deck Name Filter` ----
            var nameRow = Node(filters, "Deck Name Filter", new PxRect(DfltL, DNameT, DfltR, DNameT + DNameH));
            var ir = new PxRect(DInputL, DInputT, DInputL + DInputW, DInputT + DInputH);
            var itex = Art("InputFieldBackground");
            if (itex != null)
            {
                var g = ImageQuad.CreateNineSlice(nameRow, itex, new Vector4(10f, 10f, 10f, 10f), 32f, 32f,
                                                  Local(nameRow, ir.x1, ir.y1, ir.x2, ir.y2),
                                                  LayoutSpace.Px(ir.W), LayoutSpace.Px(ir.H), "Input BG");
                if (g != null)
                    foreach (var q in g.GetComponentsInChildren<ImageQuad>())
                    { q.SetTint(new Color(0.0627f, 0f, 0f, 1f)); q.SetRenderQueue(QFltRow); }
            }
            _deckFltNameTx = Text(nameRow, DeckNameFilterText(), DInputL + 10f, DInputL + DInputW - 45f,
                                  DInputT + 6.5f, DInputT + DInputH - 6.5f, 5, PageInk, "Input Text", 30f);
            if (_deckFltNameTx != null) _deckFltNameTx.SetRenderQueue(QFltText);
            Rect(nameRow, "40k_icon_search", new PxRect(268.45f, 180.94f, 303.45f, 210.94f), "Search Icon",
                 QFltRow, null, true);
            AddHit(nameRow, "Hit", ir, QFltHit, BeginDeckNameFilter);

            // ---- ② `Army Filter`：Title + 13 格（3 格一行）----
            var army = Node(filters, "Army Filter", new PxRect(DfltL, DArmyT, DfltR, DArmyT + DArmyH));
            var ttl = Text(army, "Army", DTitleL, DTitleL + DTitleW, DTitleT, DTitleT + DTitleH, 5, PageInk,
                           "Title", 32f);
            if (ttl != null) ttl.SetRenderQueue(QFltText);
            var armies = CampaignData.Armies;
            for (int i = 0; i < armies.Length; i++)
            {
                string fac = armies[i];
                float x = DCellPad + (i % 3) * (DCellS + DCellGap);
                float y = DCellT + (i / 3) * DCellS;
                var cr = new PxRect(x, y, x + DCellS, y + DCellS);
                var cell = Node(army, "Cell_fac_" + fac, cr);
                Rect(cell, DeckRuntime.FactionIcon(fac), cr, "Icon", QFltRow,
                     ToggleTint(fac == _deckFacFilter), true);
                AddHit(cell, "Hit", cr, QFltHit, () => ToggleDeckFacFilter(fac));
            }

            panel.gameObject.SetActive(false);       // 起手收起（**我们挑的**，见上面那条注释）
            _deckFltPanel = panel;
        }

        string DeckNameFilterText()
        {
            var pl = PointerLayer.Instance;
            if (pl != null && pl.TextEditing) return pl.TextBuffer + "_";
            return string.IsNullOrEmpty(_deckNameFilter) ? "Search" : _deckNameFilter;
        }

        /// <summary>`Filters` 圆钮 / 自检：开合 Deck 页的左抽屉（**原版那颗钮开的就是它**）。</summary>
        public void ToggleDeckFilters()
        {
            if (_deckFltPanel == null) return;
            bool on = !_deckFltPanel.gameObject.activeSelf;
            _deckFltPanel.gameObject.SetActive(on);
            if (on && _deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
            Debug.Log("[Collection] Deck 页筛选栏 " + (on ? "打开" : "收起") + "（原版 `Deck Filters`）");
        }

        /// <summary>点一格阵营格：**再点一次取消**（与 Cards 页 `$fac:` 同一条手感）。</summary>
        public void ToggleDeckFacFilter(string fac)
        {
            _deckFacFilter = _deckFacFilter == fac ? "" : (fac ?? "");
            RebuildDeckFilterCells();
            ApplyDeckFilterChanged();
        }

        /// <summary>Deck 页的 `Clear filters`（原版那颗钮：清掉本页的名字 + 阵营筛选）。</summary>
        public void ClearDeckFilters()
        {
            _deckFacFilter = ""; _deckNameFilter = "";
            RebuildDeckFilterCells();
            ApplyDeckFilterChanged();
            if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
        }

        void BeginDeckNameFilter()
        {
            var pl = PointerLayer.Instance;
            if (pl == null) { Debug.Log("[Collection] 批处理里没有 PointerLayer ⇒ 卡组名筛选敲不了字（Play 里可以）"); return; }
            string started = _deckNameFilter ?? "";
            pl.BeginText(started, 24,
                         s => { _deckNameFilter = (s ?? "").Trim(); RebuildDeckFilterCells();
                                if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText());
                                ApplyDeckFilterChanged(); },
                         () => { if (_deckFltNameTx != null) _deckFltNameTx.SetText(DeckNameFilterText()); },
                         s => { if (_deckFltNameTx != null) _deckFltNameTx.SetText(s + "_"); });
            Debug.Log("[Collection] 卡组名筛选：输入后回车确认，ESC 取消");
        }

        /// <summary>按当前筛选重刷阵营格的选中色（**只改颜色，不重建节点** —— 格子是固定的 13 个）。</summary>
        void RebuildDeckFilterCells()
        {
            if (_deckFltPanel == null) return;
            var armies = CampaignData.Armies;
            for (int i = 0; i < armies.Length; i++)
            {
                var cell = FindDeep(_deckFltPanel, "Cell_fac_" + armies[i]);
                var q = cell != null ? cell.GetComponentInChildren<ImageQuad>() : null;
                if (q != null) q.SetTint(ToggleTint(armies[i] == _deckFacFilter));
            }
        }

        /// <summary>Deck 页的卡组在当前筛选下要显示哪些（**原始下标**）。</summary>
        List<int> FilteredDeckIndices()
        {
            var list = new List<int>();
            int n = CollectionData.DeckCount();
            for (int i = 0; i < n; i++)
            {
                var d = CollectionData.DeckAt(i);
                if (!string.IsNullOrEmpty(_deckFacFilter)
                    && !string.Equals(d.Faction, _deckFacFilter, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(_deckNameFilter)
                    && (d.Name ?? "").IndexOf(_deckNameFilter, System.StringComparison.OrdinalIgnoreCase) < 0) continue;
                list.Add(i);
            }
            return list;
        }

        /// <summary>改完筛选：重算滚动区 + 重画格子 + 重判空态（**只此一处**，三样一起动）。</summary>
        void ApplyDeckFilterChanged()
        {
            int cnt = FilteredDeckIndices().Count;
            int rows = Mathf.Max(1, Mathf.CeilToInt(cnt / (float)DeckCols));
            float contentH = rows * (DeckCellH + DeckSpacingX) - DeckSpacingX;
            if (DeckScroll != null)
            {
                // ⚠️ `MenuScroll` 没有「改内容尺寸」的 API ⇒ 直接写它那两个**公开字段**
                //    （纵向时 `ContentX1/X2` 装的是上下两端，见那个字段的注释），再夹一次偏移
                DeckScroll.ContentX2 = DeckScroll.Viewport.y1 + contentH;
                DeckScroll.SetOffset(DeckScroll.Offset);
            }
            var page = PageRoot(0);
            var holder = page != null ? page.Find("Deck Scroll View") : null;
            if (holder != null) RebuildDeckCells(holder);
            RefreshDeckEmpty();
        }

        /// <summary>按名字**深度**找（`Transform.Find` 不递归；本页这几处要找的节点在两层以内）。</summary>
        static Transform FindDeep(Transform root, string name)
        {
            if (root == null) return null;
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }

        /// <summary>Deck 页的「一套卡组都没有」提示。🔴 判据 = **当前筛选下一套都不剩**
        /// （原版 `CollectionDisplay.RefreshCollection`：`filteredCollection.Count <= 0`）。</summary>
        void RefreshDeckEmpty()
        {
            if (_deckEmpty != null) _deckEmpty.gameObject.SetActive(FilteredDeckIndices().Count <= 0);
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
            // 🔴 **摆位按「筛选后的序号」、身份用「原始下标」** —— 两件事分开（2026-09-24 建抽屉时改的）。
            //    筛选为空时这里自然一个都不建，`_deckEmpty` 那条提示由 `RefreshDeckEmpty` 打开。
            var idxs = FilteredDeckIndices();
            for (int p = 0; p < idxs.Count; p++)
            {
                var content = DeckCellRect(p);
                var r = DeckScroll != null ? DeckScroll.Shift(content) : content;
                if (DeckScroll != null && !DeckScroll.Intersects(r)) continue;
                DeckCells.Add(BuildDeckCell(parent, idxs[p], r));
            }
            Clip = prevClip;
        }

        /// <summary>一格卡组。版式照 A2：根 250×405、显示 **0.9 倍**。
        /// 🔴 **2026-09-24 收口到 `MenuDraw.DeckCell`** —— 原版 `Collection Deck` 与
        /// `Deck Selection Popup` 的 `Collection Deck With Highlight` 是**同一份 prefab 几何的两个变体**
        /// （逐字段 diff 过，只差根组件的 `useSelectedHighlight`）⇒ 画法**只能有一份**。</summary>
        Transform BuildDeckCell(Transform parent, int i, PxRect r)
        {
            int idx = i;                                   // ⚠️ 闭包别捕 `i`（循环变量）
            return MenuDraw.DeckCell(parent, "CollectionDeck_" + i, r, CollectionData.DeckAt(i),
                                     i == CollectionData.CurrentIndex(),
                                     QPageRow, QPageText, QPageOverlay, QPageRow,
                                     () => SelectDeck(idx), DeckViewport);
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

        /// <summary>开 `CardDetailPopup`（= 原版菜单版 `CardDisplayWindow`）。
        /// **两处入口共用这一份**（CLAUDE.md §三）：Cards 页点一张卡（`CardCollectionTab.OnItemSelected`
        /// → `CardDisplayWindow.Instance.ShowCard(card,…)`）与 Styles 页点一张异画（`AlternateArtCardCollectionTab` 同一条）。</summary>
        public CardDetailPopup OpenCardDetail(CardDef card)
        {
            if (card == null) { Debug.LogWarning("[Collection] 没有卡可展示"); return null; }
            if (Manager == null)
            {
                Debug.LogWarning("[Collection] 没有 `WindowsManager`，开不了卡片详情窗");
                return null;
            }
            var w = CardDetailPopup.Create(Manager);
            Manager.OpenWindow(w);
            w.ShowCard(card);                 // 原版 `ShowCard` 复用同一个窗（不新建）
            LastCardDetail = w;
            Debug.Log("[Collection] 开卡片详情窗：「" + card.Name + "」");
            return w;
        }

        /// <summary>最近一次开出来的卡片详情窗（自检用）。</summary>
        public static CardDetailPopup LastCardDetail;

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
                    _win.BuildStylesPage(_root);           // 异画页：本地 7 张督军异画（2 种风格）
                    break;
            }
        }
    }
}
