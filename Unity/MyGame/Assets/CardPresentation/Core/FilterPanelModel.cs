// FilterPanelModel.cs — 原版 `Card Filters` 筛选抽屉的**七行模型**（只算几何/文案，不画）。
//
// 为什么要单开一份：**同一个抽屉原版只有一套** —— 卡组编辑与收藏窗用的是**同一批脚本**
// （`CardNameFilter` / `CardArmyFilter` / `CardRarityFilter` / `CardCostFilter` / `CardTypeFilter`
//  + `CollectionFilterController<T>`），行高/行位/格尺寸**逐条相同**，只有**面板自己**的两处原点不同：
//   · 收藏窗 Cards 页  `Card Filters` = `0.25,155.9` **335.31×924.06**
//   · 卡组编辑         `Card Filters` = `2.2,156.0`  **331.7×924.1**（权威坐标 R:174-176）
// ⇒ 两份实现迟早不一致（`CLAUDE.md` 铁律「两处写同一条规则 = 迟早不一致」），所以做成**一份、参数化面板矩形**。
//
// ============================ 判据出处（逐条实读，不是估的） ============================
// · 行结构 / 行顶 / 内件 rect / sprite 名 / 字号：
//   `资料/普查产出_0923/A3_Cards页.md` §5·1（`menu_rect.py` + `menu_dump.py` 实读，绝对 px）
// · 卡组编辑那棵树的对照：`资料/说明书/04_界面UI/菜单全树.md:9432-9472`
//   （模板位，宽 0 ⇒ **不能照抄**；行高与内件尺寸与收藏窗逐条吻合：
//    Name 79 / Owned 50 / Upgradable 50 / Rarity 280 / Cost 230 / Type 150，Input Field **281.3×40**）
// · 选项表（**不是按枚举直觉编的**）：
//   Army **13** 档（原版走 Localize；**我们只印阵营图标、不印字** —— 格 100×100 里那行字原版也没有）· Rarity **5** 档 · Cost **8** 档（区间）· Type **3** 档
//
// ============================ 七行的行顶（面板内坐标，px） ============================
// 0 / 79.02 / 129.02 / 179.02 / 329.02 / 609.02 / 839.02，内容总高 **989.02**（= 七行之和）。
// ⚠️ 面板可见高只有 **924.1** ⇒ **可滚 64.92**（最后一行 `Type` 的底 65px 要滚一下才露出来）。
//    「原版是滚还是就那么画到屏幕外」**没跑到实况**（关服 + 该界面实例化即黑），如实记在 §三 第 15 条。
//
// ⚠️ 三处「一个值 ≠ 全部情况」（铁律 5·c）：
//   ① 行高/行位是**布局组跑之后**的值（`Filters` = VerticalLayoutGroup，spacing 0、pad 0）；
//   ② `Background` 图与 `Checkmark`：**30 个选项的图全是运行时赋的**（预设里 `sprite=0`），
//      `checkMark` **30/30 全是 null** ⇒ **选中态没有对勾图，靠 `EverguildToggle` 的 tint**。
//      🔴 **2026-10-05（A32③）订正：原来这里写「我们用的 on=白 / off=灰 那两个色值是同 bundle 里
//        成对出现的 toggle 预制值，**没证明就是采集筛选那一支**」—— 现在【证明了】，而且【不是一个值】**：
//        逐行的模板节点（父链解过）按行给了三个值 —— Army `(0.5,0.5,0.5,1)` · Rarity `(0.5,0.5,0.5,0.749)` ·
//        Cost/Type `(0.349,0.341,0.341,1)`；两个窗口各读一遍、逐值相同。见 `OffTintFaction/Rarity/CostType`。
//   ③ **开关行与四行小标题的 `Label` / `Title` 对齐**：实读都是 **Left/Middle**（全包 8/8 + 12/12），
//      ⚠️ 小标题我们**仍按 Center 画**（还开着的偏离，见 `TitleFontPx` 那段）。
using System.Collections.Generic;

namespace CardPresentation
{
    public static class FilterPanelModel
    {
        // ------------------------------------------------------------ 行顶（面板内，px）
        public const float RowName = 0f, RowOwned = 79.02f, RowUpgr = 129.02f, RowArmy = 179.02f;
        /// <summary>Name 行高（= `Input Field` 79.02 那一行）。Owned/Upgradable 两行各 <see cref="ToggleRowH"/>。</summary>
        public const float RowNameH = 79.02f;
        /// <summary>Rarity / Cost / Type 三行的**行高**（`菜单全树.md` 与 A3 §5·1 的实读值）。
        /// 逐条对得上自己的内容：Rarity 5 格 3 列 = 2 行 100 + 15 = 215 ⇒ 行高 65+215 = **280** ✓；
        /// Cost 8 格 4 列 = 2 行（65/20）= 150 + 15 ⇒ 行高 65+165 = **230** ✓；Type 3 格 1 行 ⇒ **150** ✓。</summary>
        public const float RowRarityH = 280f, RowCostH = 230f, RowTypeH = 150f;

        /// <summary>🔴 **2026-09-28 修一处真缺陷：Army 那一行的高度必须由【格数】算出来。**
        /// 原来把 Army 行写死 150（A3 §5·1 的 `Army Filter ... 335.3,150` + `Content 335.3,100`），
        /// 而那一行的网格是 **13 格 · 3 格/行 = 5 行 = 500 px**（`cell 100×100` `spacing 7/0` `pad L14`
        /// ⇒ `floor((335.3−14+7)/107) = 3 格/行`，A3 §3·6 实读）⇒ **溢出 350px，压在 Rarity/Cost 行上**
        /// （截图看得很清楚：阵营徽记与稀有度框叠在一起，且「点 Rarity 实际改了阵营」）。
        /// 两处判据**不可能同时成立** —— 卡组编辑自己那棵树给的是 `Army Filter [2,335 332x0]`（高 **0**，
        /// 说明它是**内容驱动**的，同族的 Rarity/Cost 才是写死 280/230），所以按**内容高**算。
        /// ⚠️ 这是一处**偏离 A3 那两个数**的取值，理由与两处证据都记在这里，别再改回去。</summary>
        public static float ArmyRowH(int armyCellCount)
        {
            int rows = armyCellCount <= 0 ? 0 : (armyCellCount + ArmyPerRow - 1) / ArmyPerRow;
            return ArmyContentTop + rows * ArmyCell;
        }
        /// <summary>Army 一行摆几格（原版 `GridLayoutGroup` Flexible：`floor((335.3−14+7)/107)` = 3）。</summary>
        public const int ArmyPerRow = 3;

        /// <summary>七行的**行顶 / 内容高**（面板内 px）。Army 那一行随阵营数变 ⇒ 它下面三行跟着往下挪
        /// （原版就是一个 `VerticalLayoutGroup` 顺着排，Army 高多少，Rarity 就落在哪儿）。</summary>
        public struct Layout
        {
            public float ArmyTop, RarityTop, CostTop, TypeTop, ContentH;
        }

        public static Layout ComputeLayout(int armyCellCount)
        {
            Layout L;
            L.ArmyTop = RowArmy;
            L.RarityTop = RowArmy + ArmyRowH(armyCellCount);
            L.CostTop = L.RarityTop + RowRarityH;
            L.TypeTop = L.CostTop + RowCostH;
            L.ContentH = L.TypeTop + RowTypeH;
            return L;
        }

        /// <summary>同一份布局，直接按状态里的阵营数算（**格子数与行高必须同源**，否则又会对不上）。</summary>
        public static Layout ComputeLayout(DeckEditorState state)
        {
            return ComputeLayout(state != null ? state.Factions().Count : 0);
        }

        /// <summary>抽屉内容总高（给滚动上界用）。</summary>
        public static float ContentHFor(DeckEditorState state) { return ComputeLayout(state).ContentH; }
        /// <summary>开关行（Owned / Upgradable）的行高。</summary>
        public const float ToggleRowH = 50f;

        // ---- ① 搜索框那一行的内件（面板内，px；A3 §5·1 实读）----
        public const float InputTopInRow = 19.51f;   // `Input Field` 顶边（Name 行内）
        public const float InputW = 281.28f, InputH = 40f;
        public const float InputTextInset = 10.14f;  // `Text Area` 左内缩（37.15 − 27.01）
        public const float InputTextTopIn = 7.0f;    // `Text Area` 顶内缩（26.5 − 19.5）
        public const float InputTextW = 231.28f, InputTextH = 27f;
        public const float SearchIconW = 35f, SearchIconH = 30f;
        public const float SearchIconRightIn = 4.94f; // 尾图标右缘到输入框右缘（308.29 − 303.35）
        public const float SearchIconTopIn = 5.0f;    // 尾图标顶内缩（24.5 − 19.5）
        /// <summary>`Input Field` 底图是 **Unity 内置图** `InputFieldBackground`（32×32、`m_Border=(10,10,10,10)`，
        /// 藏在 `bundle_Warpforge_unitybuiltinassets` ⇒ 单独导的）。`m_Color` 实读 = **(0.0627, 0, 0, 1)**。</summary>
        public const string InputSprite = "InputFieldBackground";
        public const float InputBorder = 10f;
        public static readonly UnityEngine.Color InputTint = new UnityEngine.Color(0.0627f, 0f, 0f, 1f);
        /// <summary>占位符文字（原版 `Placeholder` TMP 原文，**没本地化**）。</summary>
        public const string InputPlaceholder = "Search";
        public const float InputFontPx = 30f, InputFontAutoMin = 18f;
        public const string SearchIconSprite = "40k_icon_search";

        // ---- ②③ 两个开关的内件（面板内，px）----
        /// <summary>`Image` = `40_main_bt_toggle_on`，锚 `a(0.7,0)-(1,1) pv(1,.5) pos(-25,0) sd(-30,0)`
        /// ⇒ 宽 = **0.3·面板宽 − 30**、右缘 = **面板宽 − 25**（所以是**按锚点算**，不是固定 70.59 ——
        /// 面板宽一变它就跟着变；收藏窗 335.31 宽时正好是实读的 70.59）。</summary>
        public const float ToggleIconRightIn = 25f, ToggleIconWMinus = 30f, ToggleIconFrac = 0.3f;
        /// <summary>`Label` TMP（"Owned only" / "Upgradable only"）· fs32 auto(18–32) · **hAlign=Left** ·
        /// 🔴 2026-10-05（A32④）订正：原来是 `Center`（照 A3 §5·1 里一个读错的字）—— 全包 8 个 `Owned only` /
        /// `Upgradable only` 的 `m_HorizontalAlignment` 实测**都是 `1`(Left)**，见 `Build` 里那处长注释。
        /// 右缘按锚点算（见 <see cref="ToggleRowRects"/>）。</summary>
        public const float ToggleLabLeft = 25f;
        public const float ToggleFontPx = 32f, ToggleFontAutoMin = 18f;
        public const string ToggleSprite = "40_main_bt_toggle_on";
        /// <summary>🆕 2026-10-04（A24）：**关着**时那张 —— 原版那三颗 `EverguildToggle`
        /// （`Card Filters/…/{Owned Toggle, Upgradable Toggle}` 与 `Cosmetic FIlter/…/Owned Toggle`）
        /// 的 `changeSpriteOnValueChange = 1` · `onSprite = 40_main_bt_toggle_on` ·
        /// `offSprite = 40_main_bt_toggle_off`（逐字段实读 prefab：pid `-7583144681335034689` / `-8582066306391164324`）。
        /// 🔴 我们原来**恒画 on 那张**、只给关掉的多一层色偏 ⇒ **关掉的和打开的长得一模一样，只暗一点**。
        /// ⚠️ 同一批 toggle 的 `colorTintOnValueChange = 0` ⇒ 原版**不按值改色**，状态**只体现在图上**
        /// （所以 `CellTint()` 对这几格给白，见 `DeckRuntime.CellTint`）。</summary>
        public const string ToggleSpriteOff = "40_main_bt_toggle_off";
        /// <summary>选中的那一格打白（`onColor` 全库一律 `(1,1,1,1)`）。
        /// 🔴 **关着那一格的色偏逐行不同** —— 别再写成一份共用值（铁律 5·c）。见下三个常量。</summary>
        public static UnityEngine.Color ToggleTint(bool on, UnityEngine.Color off)
        { return on ? UnityEngine.Color.white : off; }

        // ---- 🔴 2026-10-05（A32③）**关着时的色偏**：原版 `EverguildToggle.offColor`，**四行三个值** ----
        //   判据 = **直接读 prefab 的序列化字段**（不是转抄普查；两扇窗各读一遍，逐值相同）：
        //   `python 工具/q_probe_btn.py` / 读 MB 的 `offColor`，逐行的模板节点（父链已解过，铁律 4）：
        //     Army    `… > Card Filters > … > Army Filter/Content/Toggle    ` `(0.5,0.5,0.5,1)`     pid `1318350590833061668`（卡组编辑）
        //             · 收藏窗同族 `3457606859507047723` / 样式页 `8601981187430933205` —— **三处同值**
        //     Rarity  `… > Rarity FIlter/Content/Toggle`                     `(0.5,0.5,0.5,`**`0.749`**`)` pid `-1902731330081722588`（卡组编辑）
        //             · 收藏窗同族 `8604850209327997653` / 样式页 `509764840299422421` —— **三处同值**
        //     Cost    `… > Cost Filter/Content/Toggle`                       `(0.349,0.341,0.341,1)`
        //     Type    `… > Type Filter/Content/Toggle`                       `(0.349,0.341,0.341,1)`
        //   🔴 **我们原来只有一份 `0.349`** ⇒ Army/Rarity 两行都偏深（Rarity 还丢了一个 alpha）。
        //   ⚠️ **Rarity 那个 0.749 不是笔误**：`191/255 = 0.7490196` —— 四个模板里只有它有 alpha。
        //   ⚠️ 这四颗的 `m_Transition` 全是 **0(None)** ⇒ 原版**悬停什么都不变**（A32② 那条「9 颗」里的
        //     「4 个筛选格 toggle」就在这四行上，逐颗实读 ⇒ **原版本身就没有**，我们也不接）。
        /// <summary>Army 行（13 阵营格）关着时的色偏。**卡组页/卡背页那几个阵营格也是它**（同族同值）。</summary>
        public static readonly UnityEngine.Color OffTintFaction = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 1f);
        /// <summary>Rarity 行关着时的色偏（⚠️ 带 alpha，见上面那段）。</summary>
        public static readonly UnityEngine.Color OffTintRarity = new UnityEngine.Color(0.5f, 0.5f, 0.5f, 0.7490196f);
        /// <summary>Cost / Type 两行关着时的色偏。
        /// ⚠️ **两行的原值是【不同】的两个浮点数 —— 但量化到 8 位后是同一组 `(89,87,87)`**，
        /// 所以这里**故意共用一份**（如实记，别当成「没查」）：
        /// Cost `(0.3490196, 0.3411765, 0.3411765, 1)`（= 89/255, 87/255）·
        /// Type `(0.3490566, 0.3408241, 0.3408241, 1)`（差在第 4 位小数，肉眼/8 位纹理**分不出**）。
        /// 判据 = 逐行读 MB 的 `offColor`（`q_probe_btn.py` / 上面那条扫描），两扇窗各一份、逐值相同。</summary>
        public static readonly UnityEngine.Color OffTintCostType = new UnityEngine.Color(0.349f, 0.341f, 0.341f, 1f);

        // ---- ④ 阵营（Grid 100×100 · sp7/0 · pad L14 ⇒ 3 格/行）----
        public const float ArmyCell = 100f, ArmySpX = 7f, ArmyPadL = 14f;
        public const float ArmyContentTop = 50f;        // `Content` 从行内 y+50 起
        // ---- ⑤ 稀有度（Grid 100×100 · 同 pad/sp；图 50×50 居中；标签 100×22 贴格底）----
        public const float RarityCell = 100f, RarityContentTop = 65f;
        public const float RarityIconInset = 25f, RarityLabTopIn = 78f;
        public const float RarityFontPx = 23.2f, RarityFontAutoMin = 10f;
        // ---- ⑥ 费用（Grid 65×65 · sp15/20 · pad L15 ⇒ 4 格/行）----
        public const float CostCell = 65f, CostSpX = 15f, CostSpY = 20f, CostPadL = 15f;
        public const float CostContentTop = 65f;
        public const float CostFontPx = 45f, CostFontAutoMin = 25f;
        public const string CostSprite = "Card_Frame_Cost_Icon";
        // ---- ⑦ 类型（HLG sp0 pad 15/0/0/0 align 6 LowerLeft ⇒ 贴行底）----
        public const float TypeCellW = 80f, TypeCellH = 100f, TypePadL = 15f;
        public const float TypeContentTop = 50f;
        public const float TypeIconInsetX = 15f, TypeIconInsetY = 25f, TypeLabTopIn = 78f;
        /// <summary>小标题（`Title` TMP **fs32** · 高 50）。x = 面板内左起。
        /// 🔴 **2026-10-05 复核（A32④）：原版这四行的 `Title` 是 **hAlign=Left/Middle**，不是 Center**
        /// （旧注释写的 `Center` 与 A3 §5·1 同源、同样读错了）。**同日已改** —— 见 `Title.Left`
        /// 与两个渲染方（`DeckRuntime.RefreshFilterTitles` / `CollectionWindow.TitleRow`）。
        /// <para>判据（本轮**现读**，两处独立）：① `python 工具/menu_dump.py bundle_menus_assets_all
        /// "Deck Editing Menu" --depth 12` 打出的 `Army` `2.2,335→333.9,385` · `Rarity` `27.2,360→333.9,410` ·
        /// `Energy Cost` `27.2,630→333.9,680` · `Type` `27.2,850→333.9,900`，**四行都是 `对齐=Left/Middle`**；
        /// ② 同命令打在 `Collection Menu Variant` 上（`Cards Tab/Card Filters` 那棵）同样是
        /// `Title 0.3,335→335.6,385` / `25.3,510` / `25.3,780` / `25.3,1000`，**四行 `对齐=Left/Middle`**。
        /// ③ 两处的 x 差都是 **25**（2.2 vs 27.2 · 0.3 vs 25.3）⇒ `TitleArmyX=0` / 其余 `=25` 那组常量成立。</para></summary>
        public const float TitleFontPx = 32f, TitleH = 50f;
        public const float TitleArmyX = 0f, TitleArmyY = RowArmy;
        public const float TitleRarityX = 25f, TitleRarityYIn = 25f;
        public const float TitleCostX = 25f, TitleCostYIn = 15f;
        public const float TitleTypeX = 25f, TitleTypeYIn = 5f;

        // ------------------------------------------------------------ 选项表（照抄原版 `options`，别按枚举直觉编）
        /// <summary>Rarity 五档（`CardRarityFilter.options`，顺序照抄）。</summary>
        public static readonly string[] RarityKeys = { "common", "rare", "epic", "legendary", "special" };
        /// <summary>**卡面上印的那几个词**（= 原版 `alternativeText` 原文）。</summary>
        public static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary", "Special" };
        public static readonly string[] RarityArt =
        {
            "1_40k_cardframe_rarity_common", "2_40k_cardframe_rarity_rare", "3_40k_cardframe_rarity_epic",
            "4_40k_cardframe_rarity_legendary", "5_40k_cardframe_rarity_special",
        };
        /// <summary>Cost 八档（`CardCostFilter.options`：`1-` / `2`…`7` / `8+`）——
        /// 🔴 **是区间不是「每个费用一格」**，`Lo` 那一档还兼「上界」语义（见 `DeckFilter.CostMax`）。</summary>
        public struct CostBucket
        {
            public readonly string Label; public readonly int Lo;
            public CostBucket(string l, int lo) { Label = l; Lo = lo; }
        }
        public static readonly CostBucket[] CostBuckets =
        {
            new CostBucket("1-", 1), new CostBucket("2", 2), new CostBucket("3", 3), new CostBucket("4", 4),
            new CostBucket("5", 5), new CostBucket("6", 6), new CostBucket("7", 7), new CostBucket("8+", 8),
        };
        /// <summary>Type 三档（`CardTypeOptions`：Hero=10 / Minion=0 / Tactic=20）。
        /// 🔴 **原版没有「防御卡」那一档** —— 防御卡在这个筛选里**筛不到**（不是我们漏做）。</summary>
        public static readonly string[] TypeKeys = { "hero", "unit", "tactic" };
        public static readonly string[] TypeLabels = { "Warlord", "Troops", "Stratagem" };
        public static readonly string[] TypeArt =
        {
            "40k_menu_search_icon_warlord", "40k_menu_search_icon_troop", "40k_menu_search_icon_stratagem",
        };

        // ------------------------------------------------------------ 模型
        /// <summary>筛选栏**一格**（原版 `EverguildToggle` + `CollectionFilterToggle`）。
        /// 坐标一律是**面板内**（未减滚动量）—— 出口处再加面板原点。</summary>
        public struct Cell
        {
            public PxRect R;            // 格 = 点击区
            public PxRect Bg;           // `Background` 那张图（Rarity 比格小：格 100²、图 50² 居中）
            public string Icon;
            /// <summary>🆕 2026-10-04（A24）：**关着时换的那张图**（`null` = 这一类不按状态换图）。
            /// 只有三颗开关格有它（原版 `EverguildToggle.onSprite/offSprite`）—— 画的人按 `On` 二选一，
            /// 见 `DeckRuntime.RefreshFilterCells` / `RefreshCosmoFilters`。</summary>
            public string IconOff;
            public PxRect Lab;          // 格内小字（Army 行**没有**）
            public string Label;
            public float LabelPx;
            public float LabelAutoMin;  // 原版 `auto(min-max)` 的 min（0 = 不开自适应）
            public bool LabelRight;     // 原版这几行 `Label` 是 hAlign=Right
            /// <summary>⚠️ **`false`（= 左对齐）才是原版的读数** —— 见 `Build` 里那两处 `LabelCenter = false`
            /// 旁边的长注释（这条字段原来是 `true`，是**照 A3 §5·1 里一个读错的字**写的）。</summary>
            public bool LabelCenter;
            /// <summary>🆕 2026-10-05（A32③）**关着时那一格的色偏** = 原版 `EverguildToggle.offColor`
            /// —— **逐行不同**（`OffTintFaction` / `OffTintRarity` / `OffTintCostType`）。
            /// 开关那一类（`IconOff != null`）**不用它**（那三颗 `colorTintOnValueChange = 0`）。</summary>
            public UnityEngine.Color OffTint;
            public string Key;          // 点了改哪一项
            public bool On;
        }

        /// <summary>四行的小标题（原版 `Title` TMP · **fs32**）。
        /// 对齐**随矩形一起发出去**（见 <see cref="Left"/>）—— 只给矩形、让渲染方摆中心 = 会画成 Center。</summary>
        public struct Title
        {
            public string Text; public PxRect R; public float Px;
            /// <summary>原版 `m_HorizontalAlignment = 1`(Left) + `m_VerticalAlignment = 512`(Middle)。
            /// **四行全是 `true`**（逐行实读，见 `TitleFontPx` 那段）；留成字段（而不是「默认就是左」）
            /// 是为了让「给矩形的那一处」把对齐**一起交代掉** —— `Cell.LabelCenter` 是同一个道理，
            /// 那个字段的教训正是「渲染方自己猜对齐」猜错过一次。</summary>
            public bool Left;
        }

        /// <summary>搜索框那一行的三个矩形（面板内）。</summary>
        public static void NameRowRects(float w, out PxRect input, out PxRect textArea, out PxRect icon)
        {
            float ix = (w - InputW) * 0.5f;                       // 实读：281.28 在 335.31 里居中 ⇒ 27.01
            input = new PxRect(ix, InputTopInRow, ix + InputW, InputTopInRow + InputH);
            textArea = new PxRect(input.x1 + InputTextInset, input.y1 + InputTextTopIn,
                                  input.x1 + InputTextInset + InputTextW, input.y1 + InputTextTopIn + InputTextH);
            icon = new PxRect(input.x2 - SearchIconRightIn - SearchIconW, input.y1 + SearchIconTopIn,
                              input.x2 - SearchIconRightIn, input.y1 + SearchIconTopIn + SearchIconH);
        }

        /// <summary>开关那一行（`Owned` / `Upgradable`）的三个矩形（面板内）。</summary>
        public static void ToggleRowRects(float w, float rowTop, out PxRect row, out PxRect icon, out PxRect lab)
        {
            row = new PxRect(0f, rowTop, w, rowTop + ToggleRowH);
            float iw = ToggleIconFrac * w - ToggleIconWMinus;      // 0.3·w − 30
            float ix2 = w - ToggleIconRightIn;
            icon = new PxRect(ix2 - iw, rowTop, ix2, rowTop + ToggleRowH);
            // `Label` 与 `Image` 同源：锚 `a(0,0)-(0.7,1)` ⇒ 右缘 = **0.7·w**（不是固定的「w − 100.6」——
            // 那个值只在 w=335.31 时相等，面板一窄就差 1px；出处 A3 §5·1 的 Label 209.7 宽 = 0.7×335.31 − 25.3）
            lab = new PxRect(ToggleLabLeft, rowTop, w - ToggleIconFrac * w, rowTop + ToggleRowH);
        }

        /// <summary>七行里**后六行**的格子表（① 搜索框是单独一行，见 <see cref="NameRowRects"/>）。
        /// 坐标 = **面板内**。`state` 提供当前筛选条件（决定每格 `On`）与阵营表。</summary>
        public static void Build(DeckEditorState state, float w, List<Cell> cells)
        {
            var f = state.Filter;
            var facs = state.Factions();
            // 🔴 **行顶随阵营数变**（Army 行高 = 它自己的内容高）—— 为什么这么算，见 `ArmyRowH` 那段。
            var L = ComputeLayout(facs.Count);

            // ---- ②③ Owned only / Upgradable only（原版两个 `EverguildToggle`，50 高）----
            for (int k = 0; k < 2; k++)
            {
                float y = k == 0 ? RowOwned : RowUpgr;
                PxRect row, icon, lab;
                ToggleRowRects(w, y, out row, out icon, out lab);
                cells.Add(new Cell
                {
                    R = row, Bg = icon, Icon = ToggleSprite, IconOff = ToggleSpriteOff, Lab = lab,
                    Label = k == 0 ? "Owned only" : "Upgradable only",
                    // 🔴 **2026-10-05（A32④）：`LabelCenter` 从 `true` 改成 `false` —— 原版是【左对齐】。**
                    //   判据（自己重跑，不是转抄）：`bundle_menus_assets_all` 里**全部 8 个**
                    //   `Owned only` / `Upgradable only` 的 TMP 都是 `m_HorizontalAlignment = 1`（= Left）
                    //   · `m_VerticalAlignment = 512`（= Middle）—— **这一包里一个 Center 都没有**
                    //   （直接扫 MB 的 `m_text` ⇒ `m_HorizontalAlignment`；`menu_dump.py … "Deck Editing Menu"`
                    //     与 `… "Collection Menu Variant"` 两棵树上打出来的也都是 `对齐=Left/Middle`）。
                    //   ⚠️ **旧注释写「原版那两个开关行的 Label 是 hAlign=Center（A3 §5·1 实读：[25.3,y,209.7,50]）」——
                    //     那个 `rect` 是对的（`25.25,234.96→234.96` ✓），`Center` 那个字是错的**
                    //     （A3 §5·1:175 原文；`资料/普查产出_0923/A3_Cards页.md` 那一行的 `hAlign=Center` 按本读数作废）。
                    //   ⚠️ A32 那条待办原来写的是「**按窗分参数**（收藏窗 Center / 卡组编辑 Left）」——
                    //     **两扇窗其实都是 Left**（收藏窗那 5 颗也逐颗读过）⇒ **不需要按窗分参数**，一份 `false` 就对两扇。
                    LabelPx = ToggleFontPx, LabelAutoMin = ToggleFontAutoMin, LabelCenter = false,
                    Key = k == 0 ? "$owned" : "$upgradable",
                    On = k == 0 ? f.Owned : f.Upgradable,
                });
            }

            // ---- ④ Army：13 档 · 格 100×100 · sp7/0 · pad L14 ⇒ **3 格/行**（行高由它撑出来）----
            for (int i = 0; i < facs.Count; i++)
            {
                float x = ArmyPadL + (i % 3) * (ArmyCell + ArmySpX);
                float y = L.ArmyTop + ArmyContentTop + (i / 3) * ArmyCell;
                var rr = new PxRect(x, y, x + ArmyCell, y + ArmyCell);   // Army 行**背景铺满格**
                cells.Add(new Cell
                {
                    R = rr, Bg = rr, Icon = DeckRuntime.FactionIcon(facs[i]), Label = null,
                    Key = "$fac:" + facs[i], On = f.Faction == facs[i],
                    OffTint = OffTintFaction,
                });
            }

            // ---- ⑤ Rarity：5 档 · 格 100×100、图 50×50 居中、标签在格底 100×22 ----
            for (int i = 0; i < RarityKeys.Length; i++)
            {
                float x = ArmyPadL + (i % 3) * (ArmyCell + ArmySpX);
                float y = L.RarityTop + RarityContentTop + (i / 3) * RarityCell;
                cells.Add(new Cell
                {
                    R = new PxRect(x, y, x + RarityCell, y + RarityCell),
                    Bg = new PxRect(x + RarityIconInset, y + RarityIconInset, x + RarityCell - RarityIconInset, y + RarityCell - RarityIconInset),
                    Icon = RarityArt[i],
                    Lab = new PxRect(x, y + RarityLabTopIn, x + RarityCell, y + RarityCell),
                    Label = RarityNames[i], LabelPx = RarityFontPx, LabelAutoMin = RarityFontAutoMin, LabelRight = true,
                    Key = "$rar:" + RarityKeys[i],
                    On = string.Equals(f.Rarity, RarityKeys[i], System.StringComparison.OrdinalIgnoreCase),
                    OffTint = OffTintRarity,            // 🔴 这一行是 `(0.5,0.5,0.5,0.749)`，**不是** Cost/Type 那个 0.349
                });
            }

            // ---- ⑥ Cost：**8 档（`1-`/2…7/`8+`）** · 格 65×65 · sp15/20 · pad L15 ⇒ 4 格/行 ----
            for (int i = 0; i < CostBuckets.Length; i++)
            {
                float x = CostPadL + (i % 4) * (CostCell + CostSpX);
                float y = L.CostTop + CostContentTop + (i / 4) * (CostCell + CostSpY);
                var rr = new PxRect(x, y, x + CostCell, y + CostCell);
                cells.Add(new Cell
                {
                    R = rr, Bg = rr, Icon = CostSprite,
                    Lab = rr, Label = CostBuckets[i].Label, LabelPx = CostFontPx, LabelAutoMin = CostFontAutoMin,
                    Key = "$cost:" + CostBuckets[i].Lo,
                    On = f.Cost == CostBuckets[i].Lo,
                    OffTint = OffTintCostType,
                });
            }

            // ---- ⑦ Type：**3 档** · 格 80×100 · HLG pad15 / align 6(LowerLeft) ----
            for (int i = 0; i < TypeKeys.Length; i++)
            {
                float x = TypePadL + i * TypeCellW;
                float y = L.TypeTop + TypeContentTop;
                cells.Add(new Cell
                {
                    R = new PxRect(x, y, x + TypeCellW, y + TypeCellH),
                    Bg = new PxRect(x + TypeIconInsetX, y + TypeIconInsetY,
                                    x + TypeCellW - TypeIconInsetX, y + TypeCellH - TypeIconInsetY),
                    Icon = TypeArt[i],
                    Lab = new PxRect(x, y + TypeLabTopIn, x + TypeCellW, y + TypeCellH),
                    Label = TypeLabels[i], LabelPx = RarityFontPx, LabelAutoMin = RarityFontAutoMin, LabelRight = true,
                    Key = "$type:" + TypeKeys[i],
                    On = f.Type == TypeKeys[i],
                    OffTint = OffTintCostType,
                });
            }
        }

        /// <summary>点了一格之后**改哪一个筛选条件 —— 全工程只有这一份实现**（卡组编辑与收藏窗共用）。
        /// 返回 `false` = 不认识这个 key（唯一一个是 `$name`：它要开输入框，由各窗自己做）。
        /// 出处：`CollectionFilterController` 各 filter 求交集（`卡组编辑界面_查证_0920.md:35`）；
        /// 费用那档的「再点一次取消」与上界展开（`CostBucketHi`）照收藏窗 2026-09-23 的实读。</summary>
        public static bool Click(DeckEditorState state, string key)
        {
            if (state == null || string.IsNullOrEmpty(key)) return false;
            var f = state.Filter;
            if (key == "$owned") f.Owned = !f.Owned;
            else if (key == "$upgradable") f.Upgradable = !f.Upgradable;
            else if (key.StartsWith("$fac:")) { var v = key.Substring(5); f.Faction = f.Faction == v ? "" : v; }
            else if (key.StartsWith("$rar:")) { var v = key.Substring(5); f.Rarity = f.Rarity == v ? "" : v; }
            else if (key.StartsWith("$cost:"))
            {
                int v = int.Parse(key.Substring(6));
                bool wasOn = f.Cost == v;                     // ⚠️ 先判「原来是不是这一档」再改，别改完再比
                f.Cost = wasOn ? DeckEditorState.AnyCost : v;
                f.CostMax = wasOn ? 0 : CostBucketHi(v);
            }
            else if (key.StartsWith("$type:")) { var v = key.Substring(6); f.Type = f.Type == v ? "" : v; }
            else return false;
            state.SetFilter(f);
            return true;
        }

        /// <summary>八档费用的**上界**：`1-` ⇒ 1 · `2`…`7` ⇒ 同值 · `8+` ⇒ 无上界。
        /// （`1-` 在卡面上读作「1 及以下」，我们这一版取到的行为是 `[1,1]` —— 与收藏窗 2026-09-23 那份一致，
        ///   **没再往下挖**原版的区间语义，如实记。）</summary>
        public static int CostBucketHi(int lo) { return lo == 1 ? 1 : (lo == 8 ? int.MaxValue : lo); }

        /// <summary>四行小标题的位置（面板内）。对齐 = **Left/Middle**（原版实测，见 `TitleFontPx` 那段）
        /// —— 由 <see cref="Title.Left"/> 交给渲染方。
        /// ⚠️ 位置**跟着 <see cref="ComputeLayout(DeckEditorState)"/> 走**（Army 行一高，下面三行就往下挪）。</summary>
        public static void BuildTitles(DeckEditorState state, float w, List<Title> titles)
        {
            var L = ComputeLayout(state);
            titles.Add(new Title { Text = "Army", R = new PxRect(TitleArmyX, L.ArmyTop, w, L.ArmyTop + TitleH), Px = TitleFontPx, Left = true });
            titles.Add(new Title { Text = "Rarity", R = new PxRect(TitleRarityX, L.RarityTop + TitleRarityYIn, w, L.RarityTop + TitleRarityYIn + TitleH), Px = TitleFontPx, Left = true });
            titles.Add(new Title { Text = "Energy Cost", R = new PxRect(TitleCostX, L.CostTop + TitleCostYIn, w, L.CostTop + TitleCostYIn + TitleH), Px = TitleFontPx, Left = true });
            titles.Add(new Title { Text = "Type", R = new PxRect(TitleTypeX, L.TypeTop + TitleTypeYIn, w, L.TypeTop + TitleTypeYIn + TitleH), Px = TitleFontPx, Left = true });
        }

        // ============================================================
        //  卡背页那个抽屉（原版 `Cosmetic FIlter`）—— **另一棵 prefab，只有两行**
        //
        //  实读：`python 工具/menu_rect.py d:/2/新解包资源/assets_full/bundle_menus_assets_all
        //        "Cosmetic FIlter" --depth 5`
        //  （⚠️ 那个包里命中 **两个**同名节点，脚本取的是 `-372539790263455964` 那个 ——
        //    它的父是 `Cosmetic Display` 167.18,70.97 ⇒ **就是卡组编辑这棵树**）
        //    抽屉       2.18,155.97 → 333.90,1080.03（331.73 × 924.06 · **出厂 INACT**）
        //    `Shadow`   同父矩形 · `Filters`（HLG）
        //      `Spacing`       331.73 × 15.00      （155.97 → 170.97）
        //      `Army Filter`   331.73 × **150**     （模板高 = `Title` 50 + `Content` 100；行高随格数）
        //        `Title`   2.18,170.97 → 332.34,220.97
        //        `Content` 2.18,220.97 → 332.35,320.97 · 格 `Toggle` **100×100** @ 16.18,220.97
        //                  ⇒ 与上面 ④ 行**同一套尺子**（pad L14 · 列距 7 · 3 格/行）—— 直接复用
        //      `Spacing (1)`   331.73 × 12.81      （Army 行之后）
        //      `Owned Toggle`  331.73 × 50.00 · `Image` 228.90→308.90 · `Label` 27.18→257.18
        //  🔴 **行序与卡牌那套【不同】**：这里是 **Army 在前、Owned 在后**
        //     （卡牌那套是 Owned/Upgradable 在前、Army 在第三行）。
        // ============================================================
        public const float CosmoSpacing1 = 15f, CosmoSpacing2 = 12.81f;

        /// <summary>`Army Filter` 那一行的高度 —— 与卡牌那套**同一个 `ArmyRowH`**（格数决定行数）。</summary>
        public static float CosmoArmyRowH(int armyCount) { return ArmyRowH(armyCount); }

        /// <summary>`Owned Toggle` 那一行的行顶（相对抽屉顶）—— Army 行一高，它跟着往下走。</summary>
        public static float CosmoOwnedTop(int armyCount)
        { return CosmoSpacing1 + CosmoArmyRowH(armyCount) + CosmoSpacing2; }

        /// <summary>卡背抽屉整块内容的高度（px）—— 判断要不要滚动用它。</summary>
        public static float CosmoContentH(int armyCount) { return CosmoOwnedTop(armyCount) + ToggleRowH; }

        /// <summary>卡背抽屉里那两行的格子 —— **只有「13 个阵营格 + 1 个 Owned 开关」**，
        /// 没有搜索框 / 稀有度 / 费用 / 类型（原版那棵树里就没有）。
        /// 坐标 = **抽屉内 px**；`w` = 抽屉宽。</summary>
        public static void BuildCosmetics(List<string> facs, DeckFilter f, float w, List<Cell> cells)
        {
            float armyTop = CosmoSpacing1;
            for (int i = 0; i < facs.Count; i++)
            {
                float x = ArmyPadL + (i % ArmyPerRow) * (ArmyCell + ArmySpX);
                float y = armyTop + ArmyContentTop + (i / ArmyPerRow) * ArmyCell;
                var rr = new PxRect(x, y, x + ArmyCell, y + ArmyCell);   // 阵营行**背景铺满格**
                cells.Add(new Cell
                {
                    R = rr, Bg = rr, Icon = DeckRuntime.FactionIcon(facs[i]), Label = null,
                    Key = "$fac:" + facs[i], On = f.Faction == facs[i],
                    OffTint = OffTintFaction,
                });
            }

            PxRect row, icon, lab;
            ToggleRowRects(w, CosmoOwnedTop(facs.Count), out row, out icon, out lab);
            cells.Add(new Cell
            {
                R = row, Bg = icon, Icon = ToggleSprite, IconOff = ToggleSpriteOff, Lab = lab,
                Label = "Owned only", LabelPx = ToggleFontPx, LabelAutoMin = ToggleFontAutoMin,
                // 🔴 **2026-10-05（A32④）：`false`（左对齐）** —— 判据同卡牌那两行（这一颗的 TMP 也是
                //   `m_HorizontalAlignment = 1`；`menu_dump.py "Deck Editing Menu"` 打出来的是 `对齐=Left/Middle`）。
                LabelCenter = false, Key = "$owned", On = f.Owned,
            });
        }
    }
}
