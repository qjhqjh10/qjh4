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
// ⚠️ 两处「一个值 ≠ 全部情况」（铁律 5·c）：
//   ① 行高/行位是**布局组跑之后**的值（`Filters` = VerticalLayoutGroup，spacing 0、pad 0）；
//   ② `Background` 图与 `Checkmark`：**30 个选项的图全是运行时赋的**（预设里 `sprite=0`），
//      `checkMark` **30/30 全是 null** ⇒ **选中态没有对勾图，靠 `EverguildToggle` 的 tint**
//      （我们用 on=白 / off=灰 —— **这两个色值是「同 bundle 里成对出现的 toggle 预制值」，
//       没证明就是采集筛选那一支**，如实标成我们的取法）。
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
        /// <summary>`Label` TMP（"Owned only" / "Upgradable only"）· fs32 auto(18–32) · **hAlign=Center** ·
        /// 右缘按锚点算（见 <see cref="ToggleRowRects"/>）。</summary>
        public const float ToggleLabLeft = 25f;
        public const float ToggleFontPx = 32f, ToggleFontAutoMin = 18f;
        public const string ToggleSprite = "40_main_bt_toggle_on";
        /// <summary>选中态着色 = 原版那套 tint（`checkMark` 30/30 全是 null ⇒ **选中没有对勾图**）。
        /// ⚠️ 这两个色值是「同 bundle 里成对出现的 toggle 预制值」，**没证明就是采集筛选那一支** —— 如实标。
        /// 🔴 只有这一份（两扇窗都调它，别再各写一套）。</summary>
        public static UnityEngine.Color ToggleTint(bool on)
        {
            return on ? UnityEngine.Color.white : new UnityEngine.Color(0.349f, 0.341f, 0.341f, 1f);
        }

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
        /// <summary>小标题（`Title` TMP **fs32** · hAlign=Center · 高 50）。x = 面板内左起。</summary>
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
            public PxRect Lab;          // 格内小字（Army 行**没有**）
            public string Label;
            public float LabelPx;
            public float LabelAutoMin;  // 原版 `auto(min-max)` 的 min（0 = 不开自适应）
            public bool LabelRight;     // 原版这几行 `Label` 是 hAlign=Right
            /// <summary>原版那两个开关行的 `Label` 是 **hAlign=Center**（A3 §5·1 实读：[25.3,y,209.7,50]）
            /// ⇒ 画的时候**不要**再调 `AlignLeftOn/AlignRightOn`（`Label.Create` 本来就是居中的）。</summary>
            public bool LabelCenter;
            public string Key;          // 点了改哪一项
            public bool On;
        }

        /// <summary>四行的小标题（原版 `Title` TMP · **fs32 · hAlign=Center**）。</summary>
        public struct Title
        {
            public string Text; public PxRect R; public float Px;
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
                    R = row, Bg = icon, Icon = ToggleSprite, Lab = lab,
                    Label = k == 0 ? "Owned only" : "Upgradable only",
                    LabelPx = ToggleFontPx, LabelAutoMin = ToggleFontAutoMin, LabelCenter = true,
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

        /// <summary>四行小标题的位置（面板内）。文字一律 hAlign=Center（`Title` TMP 的实读）。
        /// ⚠️ 位置**跟着 <see cref="ComputeLayout(DeckEditorState)"/> 走**（Army 行一高，下面三行就往下挪）。</summary>
        public static void BuildTitles(DeckEditorState state, float w, List<Title> titles)
        {
            var L = ComputeLayout(state);
            titles.Add(new Title { Text = "Army", R = new PxRect(TitleArmyX, L.ArmyTop, w, L.ArmyTop + TitleH), Px = TitleFontPx });
            titles.Add(new Title { Text = "Rarity", R = new PxRect(TitleRarityX, L.RarityTop + TitleRarityYIn, w, L.RarityTop + TitleRarityYIn + TitleH), Px = TitleFontPx });
            titles.Add(new Title { Text = "Energy Cost", R = new PxRect(TitleCostX, L.CostTop + TitleCostYIn, w, L.CostTop + TitleCostYIn + TitleH), Px = TitleFontPx });
            titles.Add(new Title { Text = "Type", R = new PxRect(TitleTypeX, L.TypeTop + TitleTypeYIn, w, L.TypeTop + TitleTypeYIn + TitleH), Px = TitleFontPx });
        }
    }
}
