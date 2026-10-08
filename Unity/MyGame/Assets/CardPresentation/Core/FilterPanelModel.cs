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
        /// <param name="spY">🔴 **2026-10-17（G2 · D39）：`Content` 的 `m_Spacing.y`** —— 列距 <see cref="ArmySpX"/>
        /// 的**纵向那一半**。**缺省 0 = 收藏窗**（`Collection Menu Variant > Cardback Tab > … > Army Filter/Content`
        /// 实读 `spacing={'x':7.0,'y':0.0}`）；**卡组编辑窗的卡背抽屉是 20**（→ <see cref="CosmoArmySpYDeckEdit"/>）。
        /// ⛔ 别把缺省改成 20（会把收藏窗那几个阵营格往下推）。
        /// ⚠️ 单行（`rows ≤ 1`）没有「行与行之间的缝」⇒ 那一项不加。</param>
        public static float ArmyRowH(int armyCellCount, float spY = 0f)
        {
            int rows = armyCellCount <= 0 ? 0 : (armyCellCount + ArmyPerRow - 1) / ArmyPerRow;
            return ArmyContentTop + rows * ArmyCell + (rows > 1 ? (rows - 1) * spY : 0f);
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
        /// <summary>🔴 **2026-10-18（A891 的续）就地换口**：占位符文字**不再写死英文**，改走**词条**。
        /// <para>键 = 原版那颗 `Placeholder` 上 `Localize.mTerm` 的**原文** `MenuDeck/HUD/SearchFilter`
        /// （同键 11 颗 = `Placeholder`×6 + `Button Text`×5；本批按 pid 亲读）；英文列 = 那颗 TMP 的
        /// `m_text` 原文 `Search`（见 <see cref="InputPlaceholderEn"/>）；中文列 = 「搜索」
        /// （源 `数据/本地化/i18n/zh_CN.csv:10`，⚠️ 是我们译的）。</para>
        /// <para>⛔ **别改回 `const`** —— 常量没法跟着语言走（这正是它原来「中文档印英文」的原因）。
        /// 两个消费点都只**读**它（`Deck/DeckRuntime.cs` 的搜索框刷新 与 `Shell/CollectionWindow.cs`
        /// 的三页搜索框），改成属性对它们是**源码兼容**的。</para></summary>
        public static string InputPlaceholder { get { return Loc.T(InputPlaceholderTerm); } }
        /// <summary>上面那条词条的**键**（原版 `Localize.mTerm` 原文；自检/诊断口与
        /// <see cref="InputPlaceholder"/> 共用这一份，⛔ 别再抄一遍字面量）。</summary>
        public const string InputPlaceholderTerm = "MenuDeck/HUD/SearchFilter";
        /// <summary>原版那颗 `Placeholder` 的 TMP `m_text` **原文**（本地唯一那份英文 —— 留档用；
        /// 渲染**不**读它，渲染一律走 <see cref="InputPlaceholder"/>）。</summary>
        public const string InputPlaceholderEn = "Search";
        /// <summary>🔴 **收藏窗**那一份搜索框的字号 / 自适应下界（`Collection Menu Variant` 的
        /// `Placeholder`/`Text` = **`auto[18~30]`**，现读见 `menu_dump … "Collection Menu Variant" --depth 18 --md`）。
        /// ⛔ **别改这一对去迁就卡组编辑窗** —— 那是**另一份**（见 <see cref="InputFontPxDeckEdit"/>，铁律 5·c）。</summary>
        public const float InputFontPx = 30f, InputFontAutoMin = 18f;
        /// <summary>🔴 **2026-10-12（A333 + A336③）：搜索框那一对的【上限】与 `m_fontSizeBase`**
        /// （画布 px，与 <see cref="InputFontPx"/>/<see cref="InputFontAutoMin"/> 同量纲）。
        /// 判据 = 现读 `bundle_menus_assets_all` 的 `Collection Menu Variant` 那两棵
        /// （`工具/menu_dump.py` **不印 `m_fontSizeBase`** ⇒ 本件另扫 `MonoBehaviour/*.json`）：
        /// **卡牌页** `Tabs/CardsTab/Collection Display/Card Filters/Scroll View/Viewport/Filters/Name FIlter/Input Field/Text Area/{Placeholder,Text}`
        /// = `fs 30.0` · **`auto[18.0~30.0]`** · **`m_fontSizeBase = 26.0`** · 折行 `Placeholder 0 / Text 3`；
        /// **异画页** 同路径（`Tabs/Alternate Art Tab/…`）= `fs 35.0` · `auto[10.0~35.0]` · `m_fontSizeBase = 26.0`。
        /// ⚠️ base **两页同值 26**（`26 ≠ 30 ≠ 35`，也 ≠ TMP 出厂默认 `36`）⇒ 原版显式设过、必须照抄。</summary>
        public const float InputFontAutoMax = 30f, InputFontBase = 26f;
        /// <summary>🔴 **2026-10-07（A77⑩ 裁定「按窗分参数」）**：**卡组编辑窗**那一份搜索框的字号
        /// （`Deck Editing Menu > … > Card Filters > … > Input Field > Text Area` 的 `{Placeholder, Text}`）。
        /// <para>判据 = **现读的 dump 原始行**（`python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14 --md`）：
        /// `Placeholder` `'Search' 字号=26.0 对齐=Left/Middle 折行=0` · `Text` `'​'（零宽空格） 字号=26.0 … 折行=3`
        /// —— ⚠️ **两行都没有 `auto[…]` 段** ⇒ 原版 `m_enableAutoSizing = 0`
        /// （MB 侧独立扫描也一致：11 份 `m_text=="Search"` 的 TMP 里只有 2 份 auto 关，`Deck Editing Menu` 是其中之一，
        /// 另一份是它自己的 `Deck Name`，那份是 `fs28 auto[10~28]` —— **另一件**）。
        /// ⇒ 卡组编辑窗：**标称字号 26 · 不开自适应 · 折行按原版**（`Placeholder` 0 / `Text` 3，见
        /// <see cref="DeckEditInputWrap"/>）。</para>
        /// <para>⛔ **不许改 <see cref="InputFontPx"/>/<see cref="InputFontAutoMin"/>**（那两个是收藏窗的实测值，
        /// 两窗共用一份会把收藏窗一起改歪）。</para></summary>
        public const float InputFontPxDeckEdit = 26f;
        /// <summary>卡组编辑窗搜索框那个 `Label` 的换行模式（原版 `m_TextWrappingMode` **原文**）。
        /// = **3**（`PreserveWhitespaceNoWrap`）：同一个 `Label` 兼作 `Placeholder`（原版 **0**）与 `Text`（原版 **3**）
        /// ⇒ 取 **3**（两者在「折不折行」上同档，`TMP_Text.cs:4485`/`:4731`；而 `3` 多保住的「空白保留」正是
        /// **输入文本**要的那一半，见 `Battle/Label.cs` 的 `WrappingMode` 头）。
        /// ⛔ **别写 0 顶替**（那会把原版的 `3` 静默降级）。</summary>
        public const int DeckEditInputWrap = 3;
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
        /// <summary>🔴 **2026-10-12（A333 + A336③）：两个开关标签的【上限】与 `m_fontSizeBase`**（画布 px）。
        /// 判据 = 现读 `Collection Menu Variant`（`menu_dump.py` 不印 base ⇒ 另扫 `MonoBehaviour/*.json`）：
        /// **卡牌页** `Tabs/CardsTab/…/Card Filters/…/{Owned Toggle,Upgradable Toggle}/Label`
        /// = `fs 32.0` · **`auto[18.0~32.0]`** · **`base 32.0`** · 折行 0；
        /// **异画页** 同路径（`Alternate Art Tab/…`）= `fs 36.0` · `auto[10.0~36.0]` · **`base 32.0`**；
        /// **卡背页** `Tabs/Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Owned Toggle (1)/Label`
        /// = `fs 32.0` · `auto[18.0~32.0]` · `base 32.0`。
        /// ⚠️ 本页面的上限**恰好等于标称**（`max == fs`）⇒ 这一族 A333 上「本来就对」，
        /// **变的只有 base**（32 —— 三页同值）。</summary>
        public const float ToggleFontAutoMax = 32f, ToggleFontBase = 32f;
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
        // ---- ⑤ 稀有度（Grid 100×100 · pad L14；**列距按窗分** —— 见下面那段；图 50×50 居中；标签 100×22 贴格底）----
        //
        // 🔴🔴 **2026-10-16（WB）：这三行的「列距 / 左内缩」【两扇窗不是一个值】—— 铁律 5·c。**
        //   本模型被**两扇窗**共用（卡组编辑 `DeckRuntime.FltW = 331.7` · 收藏窗 `CollectionWindow.FltW = 335.31`），
        //   而原版那两棵树里 `Rarity/Cost/Type` 的 `Content` 是**两份不同的 prefab 实例、序列化值不同**：
        //     处          | 卡组编辑 `Deck Editing Menu`（2.2,156.0 331.7×924.1）| 收藏窗 `Collection Menu Variant`（0.25,155.9 335.31×924.06）
        //     Rarity 列距 | `m_Spacing.x = 5`  ⇒ 步进 **105**              | `m_Spacing.x = 7`  ⇒ 步进 107（= Army 那套）
        //     Cost   列距 | `m_Spacing.x = 7`  ⇒ 步进 **72**               | `m_Spacing.x = 15` ⇒ 步进 80
        //     Type  内缩  | `m_Padding.Left = 40`                        | `m_Padding.Left = 15`
        //   （`cell` 100×100 / 65×65 · `pad L` 14 / 15 · `Cost` 的 `m_Spacing.y = 20` · HLG `align=6` —— 这些**两窗相同**。）
        // 🔴 **判据（2026-10-16 现读，可复跑）—— 三条各自独立、逐条吻合：**
        //   ① `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 16 --no-sprite`
        //      ⇒ `…/Rarity FIlter/Content` = `**【GridLayoutGroup】** cellSize=100×100 spacing={'x': 5.0,'y': 0.0} pad=14,0,0,0`；
        //      同命令打在 `Collection Menu Variant` 上（`Cards Tab/Card Filters` 与 `Alternate Art Tab/…` 两棵）
        //      ⇒ 同一颗是 `spacing={'x': 7.0,'y': 0.0}`。
        //   ② 全包**逐实例枚举**（这两个目录 `MonoBehaviour/` 里带 `m_CellSize` 的实例）：
        //      · `100×100` 的 **9** 颗里 `spacing.x = 5` **只有 1 颗**（其余 8 颗 7）；
        //      · `65×65` 的 **3** 颗里 `spacing.x = 7` **只有 1 颗**（其余 2 颗 15）；
        //      · `align=6` 的 HLG **3** 颗里 `m_Padding.Left = 40` **只有 1 颗**（其余 2 颗 15）。
        //      ⇒ 「唯一的那一个」正是卡组编辑窗那一棵（收藏窗两页 + 卡背页都取多的那一档）。
        //   ③ 独立旁证：`资料/普查产出_0923/A3_Cards页.md:118-119`（**收藏窗** Cards 页的实读）早就写着
        //      `Rarity Filter/Content` = `cell 100×100 spacing 7/0 pad 14/0/0/0` —— **收藏窗本来就是 7**。
        //   ⚠️ 这三条在 2026-10-16 之前**只读了卡组编辑那一棵**，于是把卡组编辑那条线索当成两窗共用的值去改；
        //      真要那么改，卡组编辑对上了、**收藏窗反而歪掉**（2/8/16/24/25px）。
        //      卡组编辑侧的差异账 = `资料/普查产出_1017/对账_卡组编辑部分_差异与待办.md` D36–D38。
        // ⛔ **缺省那三个常量 = 收藏窗**（已对上，A3 有实读）—— 别改成卡组编辑的值；
        //    卡组编辑走**显式传参**（<see cref="Build"/> 的 `raritySpX/costSpX/typePadL`）。
        //    形状照本文件 A247 那次的先例（`InputFontPx` vs `InputFontPxDeckEdit`）：**新增专用常量 + 可选形参，共用常量一个字不动**。
        public const float RarityCell = 100f, RarityContentTop = 65f;
        /// <summary>🔴 **卡组编辑窗**那一份的 Rarity 列距（`m_Spacing.x = 5` ⇒ 步进 **105**）。
        /// 收藏窗**不用**它（缺省走 <see cref="ArmySpX"/> = 7 ⇒ 107）。判据 → 上面 ⑤ 那段那三条。</summary>
        public const float RaritySpXDeckEdit = 5f;
        public const float RarityIconInset = 25f, RarityLabTopIn = 78f;
        public const float RarityFontPx = 23.2f, RarityFontAutoMin = 10f;
        /// <summary>🔴 **2026-10-12（A333 + A336③）：稀有度 / 类型两族 `Label` 的【上限】与 `m_fontSizeBase`**
        /// （画布 px）。判据 = 现读（`menu_dump.py` 不印 base ⇒ 另扫 `MonoBehaviour/*.json`）：
        /// **两页逐值相同** —— `…/Rarity FIlter/Content/Toggle/Label`（`'Legendary'`）
        /// 与 `…/Type Filter/Content/Toggle/Label`（`'Warlord'`）都是
        /// `fs 23.2` · **`auto[10.0~27.0]`** · **`base 36.0`** · 折行 0（`Card Filters` / `Alternate Art Tab` 各一份，四颗全同）。
        /// 🔴 **这一族正是 A333 的实测错处**：我们原来把 **`fontPx`(23.2) 当成了上限**
        /// ⇒ 天花板矮 **3.8px**，`'Legendary'` 那种短文案永远画小一档。
        /// （原版全库**没有** `(10, 23.2)` 这个自适应档 —— 那正是「把 `m_fontSize` 当上限」的指纹，
        /// 见 `资料/普查产出_1011/V7_A305_A304_普查.md` §三·1。）</summary>
        public const float RarityFontAutoMax = 27f, RarityFontBase = 36f;
        // ---- ⑥ 费用（Grid 65×65 · pad L15 · spY 20 ⇒ 4 格/行；**列距按窗分** —— 见 ⑤ 那段）----
        public const float CostCell = 65f, CostSpX = 15f, CostSpY = 20f, CostPadL = 15f;
        /// <summary>🔴 **卡组编辑窗**那一份的 Cost 列距（`m_Spacing.x = 7` ⇒ 步进 **72**）。
        /// 收藏窗**不用**它（缺省 15 ⇒ 步进 80）。判据 → 上面 ⑤ 那段那三条。
        /// ⚠️ 旧注释把这里写成「疑似把 `padL` 抄进了 spacing」—— **不是笔误**：15 就是**收藏窗**
        /// `Cost Filter/Content` 序列化里的 `m_Spacing.x`（判据②的 3 颗里那 2 颗），只是卡组编辑不是它。</summary>
        public const float CostSpXDeckEdit = 7f;
        public const float CostContentTop = 65f;
        public const float CostFontPx = 45f, CostFontAutoMin = 25f;
        /// <summary>🔴 **2026-10-12（A333 + A336③）：费用桶那族 `Label` 的【上限】与 `m_fontSizeBase`**（画布 px）。
        /// 判据 = 现读：`…/Cost Filter/Content/Toggle/Background/Label`（`'0'`）= `fs 45.0` ·
        /// **`auto[25.0~45.0]`** · **`base 36.0`** · **折行 1**（四族里唯一折行的那一族）；
        /// `Card Filters` 与 `Alternate Art Tab` 两页逐值相同（八颗全同）。
        /// ⚠️ 本族上限**恰好等于标称**（45 = 45）⇒ A333 上「本来就对」，变的只有 base（36）。</summary>
        public const float CostFontAutoMax = 45f, CostFontBase = 36f;
        public const string CostSprite = "Card_Frame_Cost_Icon";
        // ---- ⑦ 类型（HLG sp0 pad 15/0/0/0 align 6 LowerLeft ⇒ 贴行底；**左内缩按窗分** —— 见 ⑤ 那段）----
        public const float TypeCellW = 80f, TypeCellH = 100f, TypePadL = 15f;
        /// <summary>🔴 **卡组编辑窗**那一份的 Type 左内缩（`m_Padding.Left = 40`；HLG `align=6` LowerLeft
        /// ⇒ 三格整体右移 25px）。收藏窗**不用**它（缺省 15）。判据 → 上面 ⑤ 那段那三条。</summary>
        public const float TypePadLDeckEdit = 40f;
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

        // ============================================================ 🆕 2026-10-11（A248）：**异画页那一套字号**
        //
        // 🔴 **「一个值 ≠ 全部情况」（铁律 5·c）**：上面那三对常量是 **卡牌页**（`CardsTab/Card Filters`）的实测值，
        //   而**异画页**（`Alternate Art Tab/Collection Display/Card Filters`）是**另一套** —— 同一个模型、
        //   同一个矩形、**只有字号不同**。两页的矩形逐条相同（`Name FIlter` `0.3,155.9→335.6,235.0` ·
        //   `Text Area` `37.3,182.4→268.5,209.4` · `Title` 四行 x/y 全同）⇒ **本条只分字号，不动几何**。
        //
        // 判据 = **现读的 dump 原始行**（可复跑）：
        //   `python 工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 22 --no-sprite`
        //   · 异画页 `Name FIlter/…/Text Area/{Placeholder,Text}` = **字号 35.0 · `auto[10.0~35.0]`**
        //     （`Text` 折行 **3** / `Placeholder` 折行 0 —— 与卡牌页那对**同档**，折行不分页）
        //   · 异画页 `Owned Toggle/Label`（`'Owned only'`）与 `Upgradable only/Label` = **字号 36.0 · `auto[10.0~36.0]`**
        //   · 异画页四个 `Title`（Army/Rarity/Energy Cost/Type）= **字号 36.0**（**无 `auto[…]` 段** ⇒ 原版没开自适应）
        //   · 对照：卡牌页那一份 = `30 · auto[18~30]` / `32 · auto[18~32]` / `32`（= 上面那三对常量，本来就对）
        //   ⚠️ 稀有度 / 费用 / 类型那三族**两页相同**（`23.2 auto[10~27]` / `45 auto[25~45]`）⇒ 归共用常量，别按页分。
        //
        // ⛔ **别改上面那三对共用常量**（那是卡牌页的实测值，改了会把卡牌页一起改歪）——
        //   形状照 **A247** 那次的先例：**新增专用常量 + 给模型函数加【可选】形参**，共用常量一个字不动。

        /// <summary>异画页搜索框（`Placeholder`/`Text`）的字号 / 自适应下界（原版 `35 · auto[10~35]`）。</summary>
        public const float InputFontPxStyles = 35f, InputFontAutoMinStyles = 10f;
        /// <summary>🔴 **2026-10-12（A333 + A336③）：异画页搜索框的【上限】与 `m_fontSizeBase`**（画布 px）。
        /// 判据 = 现读 `Collection Menu Variant/Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/…/Name FIlter/Input Field/Text Area/{Placeholder,Text}`
        /// = `fs 35.0` · **`auto[10.0~35.0]`** · **`base 26.0`** · 折行 `Placeholder 0 / Text 3`。
        /// ⚠️ 上限**恰好等于标称**（35 = 35）⇒ A333 上「本来就对」，变的只有 base（26，与卡牌页同值）。</summary>
        public const float InputFontAutoMaxStyles = 35f, InputFontBaseStyles = 26f;
        /// <summary>异画页那两个开关标签（`Owned only`/`Upgradable only`）的字号 / 自适应下界
        /// （原版 `36 · auto[10~36]`）。</summary>
        public const float ToggleFontPxStyles = 36f, ToggleFontAutoMinStyles = 10f;
        /// <summary>🔴 **2026-10-12（A333 + A336③）：异画页两个开关标签的【上限】与 `m_fontSizeBase`**（画布 px）。
        /// 判据 = 现读 `Collection Menu Variant/Content Area/Tabs/Alternate Art Tab/Collection Display/Card Filters/Scroll View/Viewport/Filters/{Owned,Upgradable} Toggle/Label`
        /// = `fs 36.0` · **`auto[10.0~36.0]`** · **`base 32.0`** · 折行 0（两颗同值）。
        /// ⚠️ 上限**恰好等于标称**（36 = 36）⇒ A333 上「本来就对」；base **32** 与卡牌页同值（三页同值）。</summary>
        public const float ToggleFontAutoMaxStyles = 36f, ToggleFontBaseStyles = 32f;
        /// <summary>异画页四个小标题的字号（原版 **36**，**没有 `auto[…]`** ⇒ 不开自适应）。</summary>
        public const float TitleFontPxStyles = 36f;
        public const float TitleArmyX = 0f, TitleArmyY = RowArmy;
        public const float TitleRarityX = 25f, TitleRarityYIn = 25f;
        public const float TitleCostX = 25f, TitleCostYIn = 15f;
        public const float TitleTypeX = 25f, TitleTypeYIn = 5f;

        // ------------------------------------------------------------ 选项表（照抄原版 `options`，别按枚举直觉编）
        //
        // 🔴🔴 **2026-10-18（双语线 · 波 1 · P1）：这一族的【显示字】照原版的 `locKey` 走语言表** ——
        //   判据不是字面串猜的，是**原版方法体**（第一权威）＋ **prefab 序列化字段**两条：
        //   ① `CollectionFilterToggle__Initialize`（`d:/2/tools/decomp_full/CollectionFilterToggle__Initialize.c`）
        //      逐句实读：`if (*(char*)(options + 0x28) == '\0') text = *(string*)(options + 0x48);`
        //      （= `useLocalization == 0` ⇒ 印 `alternativeText`）
        //      `else { LocalizedString ls = *(options+0x30 .. +0x48); text = I2.Loc.LocalizedString.op_Implicit(ls); }`
        //      （= `useLocalization != 0` ⇒ 印 **`locKey` 那条 I2 词条**）；
        //      最后 `if (!IsNullOrWhiteSpace(text)) Label.text = text`。
        //      （字段偏移与本文件 `FilterOptions` 的声明序逐一吻合：`toggle`0x10 · `background`0x18 ·
        //        `checkMark`0x20 · `useLocalization`0x28 · `locKey`0x30(24B) · `alternativeText`0x48。）
        //   ② `bundle_menus_assets_all/MonoBehaviour/` 里 `Deck Editing Menu/…/Card Filters/…` 那三颗
        //      过滤器组件本体（本批按 pid 亲读，路径由 `m_GameObject → RectTransform.m_Father` 解出）：
        //        · `CardRarityFilter` pid **627867653050920740**（`… /Rarity FIlter`）：5 档
        //          `useLocalization=1` + `locKey.mTerm = Card_Rarity/{Common,Rare,Epic,Legendary,Special}`
        //          （顺序 = options 数组序），`alternativeText` = `Common`…`Special`。
        //        · `CardTypeFilter` pid **-3311978293714882780**（`… /Type Filter`）：3 档
        //          `useLocalization=1` + `locKey.mTerm` = **`Card_Race/Warlord` / `MenuDeck/HUD/DeckDescription/Minions`
        //          / `MenuDeck/HUD/DeckDescription/Spells`**（option = 10 / 0 / 20），`alternativeText` **三档全空**。
        //          🔴 所以类型那三格**不是** `Card_Race/{Warlord,Infantry,Stratagem}`（本批差点按字面串猜错）——
        //             Minion/Tactic 两档原版复用的是**卡组计数抽屉**那两条词条。
        //        · `CardCostFilter` pid **1802889686455676708**（`… /Cost Filter`）：8 档
        //          `useLocalization = 0` ⇒ **原版就不走词条**（印 `alternativeText` = `1-`/`2`…/`8+`）
        //          ⇒ `CostBuckets[].Label` **不进表**（施工单 §③「原版不是 mTerm ⇒ 不进」）。
        //   ⇒ 两列英文取哪一份、ZH 自拟与否，逐条写在 `Loc.cs` 那几条键的注释里（本文件不另存一份）。
        //   🔴 **2026-10-18 更正（铁律 5）**：这一行原来写「**表里的键还没补齐**（波 0 未含本族）
        //      ⇒ 取词一律经 `TermOr` 闸门：键在表里就用词条、**不在就退回原来那串**」——
        //      **已过期**：本族那 **10** 条键**早已全部进表**（`Core/Loc.cs` 实读：`Card_Rarity/*` 5 条 ·
        //      `MenuDeck/HUD/DeckDescription/{Minions,Spells}` · `Card_Race/Warlord` ·
        //      `MenuDeck/Filters/{ShowOwnedOnly,ShowUpgradableOnly}`）⇒ 闸门**已删除**（它是**静默兜底**，
        //      与铁律「不许静默失败」冲突）⇒ 取词一律是**裸 `Loc.T(键)`**（`Loc.T` 对缺键回**键名本身**，
        //      这就够了：缺键时屏上会明明白白印出 `Card_Rarity/Common`，而不是悄悄退回一串英文）。
        /// <summary>Rarity 五档（`CardRarityFilter.options`，顺序照抄）。</summary>
        public static readonly string[] RarityKeys = { "common", "rare", "epic", "legendary", "special" };
        /// <summary>**卡面上印的那几个词**（= 原版 `alternativeText` 原文）。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：原来这里写「它现在只是 <see cref="RarityTermKeys"/> 的**兜底**
        /// （键没进表时印它）—— 显示走词条」—— **两半都已过期**：闸门已删（键全在表里），
        /// 显示**只**走 `Loc.T(RarityTermKeys[i])`（见 `Build` 里那一格）。本常量**现在只剩一个用处**：
        /// 它是**原版 prefab 的 `alternativeText` 原文**这份事实的落点（原版那 8 档 Cost 走的就是它，
        /// 见 `CostBuckets`）—— 本族这份**不再参与显示**，留着当判据。</summary>
        public static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Legendary", "Special" };
        /// <summary>🔴 **Rarity 五档的原版词条键**（`CardRarityFilter.options[].locKey.mTerm` 原文，
        /// `useLocalization = 1`；出处见本段开头那两条判据）。顺序与 <see cref="RarityKeys"/> 逐格对齐。</summary>
        public static readonly string[] RarityTermKeys =
        {
            "Card_Rarity/Common", "Card_Rarity/Rare", "Card_Rarity/Epic",
            "Card_Rarity/Legendary", "Card_Rarity/Special",
        };
        public static readonly string[] RarityArt =
        {
            "1_40k_cardframe_rarity_common", "2_40k_cardframe_rarity_rare", "3_40k_cardframe_rarity_epic",
            "4_40k_cardframe_rarity_legendary", "5_40k_cardframe_rarity_special",
        };
        /// <summary>Cost 八档（`CardCostFilter.options`：`1-` / `2`…`7` / `8+`）——
        /// 🔴 **是区间不是「每个费用一格」**，`Lo` 那一档还兼「上界」语义（见 `DeckFilter.CostMax`）。</summary>
        /// <summary>🔴 **Cost 八档不在语言表里** —— 判据 = 那 8 条 `options` 的 `useLocalization` **全是 0**
        /// （`CardCostFilter` pid `1802889686455676708` 实读）⇒ 原版**直印** `alternativeText`（`1-`/`2`…/`8+`），
        /// 不走 I2。照施工单 §③「原版不是 `mTerm` ⇒ 不进表」⇒ 这一族**保持原样**（⛔ 别替它编键）。</summary>
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
        /// <summary>类型三格的**英文串**（= 原版那三条词条的英文列，也是键没进表时**原来**印的那串）。
        /// 🔴 它**不是** `alternativeText` —— 原版那三档的 `alternativeText` **全是空串**
        /// （`CardTypeFilter` pid `-3311978293714882780` 实读），显示字一律来自 `locKey` 那条词条。
        /// ⚠️ **2026-10-18 更正（铁律 5）**：原来这里写「键没进表时印它的兜底」—— 闸门已删、键全在表，
        /// ⇒ **显示不再吃它**（显示只走 `Loc.T`，见 `TypeLabelAt`）。
        /// 它**现在只剩一个用处**：`Deck/DeckEditorState.cs` 按类型名搜卡时**额外**比一次这串**原版英文**
        /// （那条路本来就要「英文档也能搜到」，见 `NameSearchMatch`）。
        /// ⚠️ 第 3 档原版词条 `MenuDeck/HUD/DeckDescription/Spells` 的本地 TMP 原文是 **`Stratagems`（复数）**
        /// （卡组计数抽屉那颗，见本段开头的判据 ②）—— 而这里存的是 `Stratagem`（**单数**，我们旧的近似），
        /// ⇒ 两者**本来就不相等**（英文档显示 `Stratagems`、搜索那一串是 `Stratagem`）。</summary>
        public static readonly string[] TypeLabels = { "Warlord", "Troops", "Stratagem" };
        /// <summary>🔴 **Type 三档的原版词条键**（`CardTypeFilter.options[].locKey.mTerm` 原文，
        /// `useLocalization = 1`）。⚠️ 这三条**不是同一族**：第 1 档复用卡面兵种行那条 `Card_Race/Warlord`，
        /// 第 2/3 档复用**卡组计数抽屉**那两条（原版就是这么复用的）。顺序与 <see cref="TypeKeys"/> 逐格对齐。</summary>
        public static readonly string[] TypeTermKeys =
        {
            "Card_Race/Warlord", "MenuDeck/HUD/DeckDescription/Minions", "MenuDeck/HUD/DeckDescription/Spells",
        };
        /// <summary>🔴 **两个开关标签的原版词条键**（`EverguildToggle` 上那颗 `Localize.mTerm` 原文）：
        /// 卡牌页 `… /Card Filters/…/{Owned,Upgradable} Toggle/Label` 与卡背抽屉
        /// `… /Cosmetic FIlter/Filters/Owned Toggle/Label` —— **两份共用同一条键**（本批按 pid 亲读）。</summary>
        public const string OwnedOnlyTerm = "MenuDeck/Filters/ShowOwnedOnly";
        public const string UpgradableOnlyTerm = "MenuDeck/Filters/ShowUpgradableOnly";
        /// <summary>第 <paramref name="i"/> 档**类型**格印在屏上那串字（**随语档**）—— 显示点与
        /// 「按类型名搜索」共用这一个口（表只此一份；⛔ 别在别处再拼一次）。
        /// 🔴 **2026-10-18（双语线 · P1 收口 · 铁律 5）**：本方法**原来经 `TermOr` 闸门**；闸门已删
        /// ⇒ 现在是**裸 `Loc.T(键)`**（键 `Card_Race/Warlord` / `MenuDeck/HUD/DeckDescription/{Minions,Spells}`
        /// 三条**都已在表**，`Core/Loc.cs` 实读）。缺键时 `Loc.T` 回**键名本身**（不会静默退成英文）。</summary>
        public static string TypeLabelAt(int i)
        {
            return (i >= 0 && i < TypeTermKeys.Length) ? Loc.T(TypeTermKeys[i]) : null;
        }
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
            /// <summary>🔴 **2026-10-12（A333）：原版那一格 `Label` 的 `m_fontSizeMax`**（画布 px，
            /// `0` = 不指定 ⇒ 退回「上限 = `LabelPx`」= 旧行为）。
            /// **本模型四族里只有两族真的需要它**：稀有度 / 类型（原版 `auto[10~27]`、标称 `23.2`
            /// ⇒ 旧写法矮 3.8px）；开关与费用两族的上限恰好等于标称。
            /// 逐族实读值 → 上面那三对常量的注释。</summary>
            public float LabelAutoMax;
            /// <summary>🔴 **2026-10-12（A336④）：原版那一格 `Label` 的 `m_fontSizeBase`**（画布 px，
            /// `0` = 不指定 ⇒ 旧行为「base = 调用方那一档」）。只影响自适应的**二分起点**。
            /// 逐族实读：开关族 **32** · 稀有度/费用/类型三族 **36**。判据 → 上面那三对常量的注释。
            /// ⚠️ 本字段**只有 `Shell/CollectionWindow.cs` 这一半在用**（`Deck/DeckRuntime.cs` 那半还没接，
            /// 见 A336④ 的账）。</summary>
            public float LabelBase;
            public bool LabelRight;     // 原版这几行 `Label` 是 hAlign=Right
            /// <summary>⚠️ **`false`（= 左对齐）才是原版的读数** —— 见 `Build` 里那两处 `LabelCenter = false`
            /// 旁边的长注释（这条字段原来是 `true`，是**照 A3 §5·1 里一个读错的字**写的）。</summary>
            public bool LabelCenter;
            /// <summary>🔴 **2026-10-07（A62 主表 #5/#6）**：原版那一格 `Label` 的 **`m_TextWrappingMode` 原文**
            /// （`0` = 不折行 · `1` = 限宽换行）。**逐族实读、写在下面每一个 `new Cell` 里** ——
            /// ⛔ **别按 `LabelCenter`/`LabelRight` 反推**：那是**静默漏改**（稀有度与类型是 `LabelRight = true`、
            /// 费用桶两样都不设 ⇒ 照 `LabelCenter` 取反会把「原版是 0、该关折行」的稀有度/类型一起跳过）。
            /// 判据 = `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 14 --md` 的 `折行=` 列：
            /// 开关 **0**（`'Owned only'`）· 稀有度 **0**（`'Legendary'`）· 类型 **0**（`'Troops'` 那一族）·
            /// **费用桶 1**（`'0'`，四族里唯一折行的那一族）；卡背页那颗 `'Owned only'` 也是 **0**
            /// （`… "Deck Editing Menu" --depth 18 --md`，`auto[26~32]`）。
            /// ⚠️ **渲染方必须显式设它**（`Label.SetWrapping(LabelWrap == 1)`）—— `Label` 那边自己带的是
            /// 「`SetAutoFitBox` 会**无条件打开折行**」那个副作用（A62 的根因），不显式设就是**碰巧对**。</summary>
            public int LabelWrap;
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

        /// <summary>开关那一行（`Owned` / `Upgradable`）的三个矩形（面板内）。
        /// <param name="iconW">🔴 **2026-10-17（G2 · D41）**：`Image` 的**固定宽**（> 0 时才用）。
        /// **缺省 0 = 按锚点算**（`0.3·w − 30` = **收藏窗**那份）；**卡组编辑窗的卡背抽屉传
        /// <see cref="CosmoIconWDeckEdit"/> = 80**（原版那棵树是 `sd(80,0)`，**锚法不同**）。
        /// 两种算法**右缘都是 `w − 25`**（原版两棵树的实测值，见 Cosmo 那段）。</param>
        /// <param name="labW">🔴 **同上（D42）**：`Label` 的**固定宽**（> 0 时才用）。
        /// **缺省 0 = 按锚点算**（右缘 `0.7·w` = **收藏窗**那份）；**卡组编辑窗的卡背抽屉传
        /// <see cref="CosmoLabWDeckEdit"/> = 230**（⇒ 25..**255**，原版 `sd(230,0)`）。左缘两种算法都是
        /// <see cref="ToggleLabLeft"/> = 25。</param></summary>
        public static void ToggleRowRects(float w, float rowTop, out PxRect row, out PxRect icon, out PxRect lab,
                                          float iconW = 0f, float labW = 0f)
        {
            row = new PxRect(0f, rowTop, w, rowTop + ToggleRowH);
            float ix2 = w - ToggleIconRightIn;
            float iw = iconW > 0f ? iconW : ToggleIconFrac * w - ToggleIconWMinus;   // 0.3·w − 30
            icon = new PxRect(ix2 - iw, rowTop, ix2, rowTop + ToggleRowH);
            // `Label` 与 `Image` 同源：锚 `a(0,0)-(0.7,1)` ⇒ 右缘 = **0.7·w**（不是固定的「w − 100.6」——
            // 那个值只在 w=335.31 时相等，面板一窄就差 1px；出处 A3 §5·1 的 Label 209.7 宽 = 0.7×335.31 − 25.3）
            float lx2 = labW > 0f ? ToggleLabLeft + labW : w - ToggleIconFrac * w;
            lab = new PxRect(ToggleLabLeft, rowTop, lx2, rowTop + ToggleRowH);
        }

        /// <summary>七行里**后六行**的格子表（① 搜索框是单独一行，见 <see cref="NameRowRects"/>）。
        /// 坐标 = **面板内**。`state` 提供当前筛选条件（决定每格 `On`）与阵营表。</summary>
        /// <param name="togglePx">两个开关标签（`Owned only`/`Upgradable only`）的字号。
        /// 🔴 **缺省 = 共用常量 <see cref="ToggleFontPx"/> = 32（= 卡牌页的原版值）**；
        /// **异画页必须显式传 <see cref="ToggleFontPxStyles"/> = 36**（A248 的偏离就在这一格）。</param>
        /// <param name="toggleAutoMin">同上那颗的自适应下界。缺省 18（卡牌页）；异画页传 10。</param>
        /// <param name="toggleAutoMax">🔴 **2026-10-12（A333）**：同上那颗的自适应**上限**（原版 `m_fontSizeMax`）。
        /// 缺省 32（卡牌页）；异画页传 <see cref="ToggleFontAutoMaxStyles"/> = 36。
        /// ⚠️ 本族两页的上限**都恰好等于标称** ⇒ 这一格今天不改变任何渲染，接上只是为了「字段不许是错的」。</param>
        /// <param name="toggleBase">🔴 **2026-10-12（A336④）**：同上那颗的 `m_fontSizeBase`。
        /// **三页同值 32** ⇒ 缺省即对；异画页**不用**另传（<see cref="ToggleFontBaseStyles"/> 也是 32）。</param>
        /// <param name="raritySpX">🔴 **2026-10-16（WB）按窗分的 Rarity 列距**。缺省 <see cref="ArmySpX"/> = 7（**收藏窗**）；
        /// **卡组编辑必须传 <see cref="RaritySpXDeckEdit"/> = 5**（⇒ 步进 105 而不是 107）。
        /// 取值与三条判据 → 上面 ⑤ 那段（铁律 5·c：两扇窗的 `Content` 序列化值本来就不同）。</param>
        /// <param name="costSpX">🔴 **同上，Cost 列距**。缺省 <see cref="CostSpX"/> = 15（收藏窗）；
        /// 卡组编辑传 <see cref="CostSpXDeckEdit"/> = 7（⇒ 步进 72 而不是 80）。</param>
        /// <param name="typePadL">🔴 **同上，Type 左内缩**。缺省 <see cref="TypePadL"/> = 15（收藏窗）；
        /// 卡组编辑传 <see cref="TypePadLDeckEdit"/> = 40（HLG 的 `m_Padding.Left`）。</param>
        public static void Build(DeckEditorState state, float w, List<Cell> cells,
                                 float togglePx = ToggleFontPx, float toggleAutoMin = ToggleFontAutoMin,
                                 float toggleAutoMax = ToggleFontAutoMax, float toggleBase = ToggleFontBase,
                                 float raritySpX = ArmySpX, float costSpX = CostSpX, float typePadL = TypePadL)
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
                    // 🔴 **2026-10-18（双语线 · P1）**：显示字走原版词条（`MenuDeck/Filters/ShowOwnedOnly` /
                    //   `ShowUpgradableOnly`，键名 = prefab 那颗 `Localize.mTerm` 原文，见上面那两条判据）。
                    //   ⚠️ **同日收口（铁律 5）**：这里原来是 `TermOr(…, "Owned only")` 闸门 ⇒ 已收成**裸 `Loc.T`**
                    //   （两条键都在表里；闸门是**静默兜底**，与「不许静默失败」冲突）。
                    Label = Loc.T(k == 0 ? OwnedOnlyTerm : UpgradableOnlyTerm),
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
                    LabelPx = togglePx, LabelAutoMin = toggleAutoMin, LabelCenter = false,
                    LabelAutoMax = toggleAutoMax, LabelBase = toggleBase,   // 🆕 A333/A336④（上限 32 / base 32）
                    LabelWrap = 0,                       // 🔴 dump：`'Owned only' 折行=0`（A62 #5）
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
                float x = ArmyPadL + (i % 3) * (ArmyCell + raritySpX);   // pad L14 **两窗同值**；列距按窗分（5/7）
                float y = L.RarityTop + RarityContentTop + (i / 3) * RarityCell;
                cells.Add(new Cell
                {
                    R = new PxRect(x, y, x + RarityCell, y + RarityCell),
                    Bg = new PxRect(x + RarityIconInset, y + RarityIconInset, x + RarityCell - RarityIconInset, y + RarityCell - RarityIconInset),
                    Icon = RarityArt[i],
                    Lab = new PxRect(x, y + RarityLabTopIn, x + RarityCell, y + RarityCell),
                    // 🔴 **2026-10-18（双语线 · P1）**：显示字走原版词条 `Card_Rarity/*`（键名 = prefab 里
                    //   那 5 条 `options[].locKey.mTerm` 原文，见本区段开头判据 ②）；`RarityNames[i]` 是兜底。
                    //   ⚠️ **同日收口（铁律 5）**：那个「兜底」已随闸门一起删掉 —— 现在是**裸 `Loc.T`**
                    //   （5 条键都在表里）。
                    Label = Loc.T(RarityTermKeys[i]),
                    LabelPx = RarityFontPx, LabelAutoMin = RarityFontAutoMin, LabelRight = true,
                    // 🔴 **A333 的实测错处**：原版上限是 **27**，旧写法拿 `fontPx(23.2)` 当上限 ⇒ 矮 3.8px。
                    //    base = **36**（原版显式设过，≠ 标称 23.2、也 ≠ TMP 出厂默认 36 —— 这里**恰好**是 36，但那是读出来的，不是推的）。
                    LabelAutoMax = RarityFontAutoMax, LabelBase = RarityFontBase,
                    LabelWrap = 0,                       // 🔴 dump：`'Legendary' 折行=0`（A62 #5；⛔ 别按 LabelRight 反推）
                    Key = "$rar:" + RarityKeys[i],
                    On = string.Equals(f.Rarity, RarityKeys[i], System.StringComparison.OrdinalIgnoreCase),
                    OffTint = OffTintRarity,            // 🔴 这一行是 `(0.5,0.5,0.5,0.749)`，**不是** Cost/Type 那个 0.349
                });
            }

            // ---- ⑥ Cost：**8 档（`1-`/2…7/`8+`）** · 格 65×65 · sp15/20 · pad L15 ⇒ 4 格/行 ----
            for (int i = 0; i < CostBuckets.Length; i++)
            {
                float x = CostPadL + (i % 4) * (CostCell + costSpX);
                float y = L.CostTop + CostContentTop + (i / 4) * (CostCell + CostSpY);
                var rr = new PxRect(x, y, x + CostCell, y + CostCell);
                cells.Add(new Cell
                {
                    R = rr, Bg = rr, Icon = CostSprite,
                    Lab = rr, Label = CostBuckets[i].Label, LabelPx = CostFontPx, LabelAutoMin = CostFontAutoMin,
                    // 🆕 A333/A336④：原版上限 45（= 标称，本来就对，接上让字段不再是猜的）· base **36**
                    LabelAutoMax = CostFontAutoMax, LabelBase = CostFontBase,
                    LabelWrap = 1,                       // 🔴 dump：费用桶 `'0' 折行=1`（四族里**唯一**折行的那一族，A62 #5）
                    Key = "$cost:" + CostBuckets[i].Lo,
                    On = f.Cost == CostBuckets[i].Lo,
                    OffTint = OffTintCostType,
                });
            }

            // ---- ⑦ Type：**3 档** · 格 80×100 · HLG pad15 / align 6(LowerLeft) ----
            for (int i = 0; i < TypeKeys.Length; i++)
            {
                float x = typePadL + i * TypeCellW;
                float y = L.TypeTop + TypeContentTop;
                cells.Add(new Cell
                {
                    R = new PxRect(x, y, x + TypeCellW, y + TypeCellH),
                    Bg = new PxRect(x + TypeIconInsetX, y + TypeIconInsetY,
                                    x + TypeCellW - TypeIconInsetX, y + TypeCellH - TypeIconInsetY),
                    Icon = TypeArt[i],
                    Lab = new PxRect(x, y + TypeLabTopIn, x + TypeCellW, y + TypeCellH),
                    // 🔴 **2026-10-18（双语线 · P1）**：显示字走原版词条 `TypeTermKeys[i]`（那三条
                    //   `CardTypeFilter.options[].locKey.mTerm` 原文，见本区段开头判据 ②）；`TypeLabels[i]` 是兜底。
                    //   ⚠️ **同日收口（铁律 5）**：「兜底」已随闸门一起删掉（现在是 `TypeLabelAt` 里的**裸 `Loc.T`**）
                    //   —— `TypeLabels` 现在只给「按类型名搜卡」那条路当**原版英文串**（见它的字段注释）。
                    //   ⚠️ 取词只此一处（`TypeLabelAt`）—— 按类型名搜卡那条路也吃它（表别拼第二份）。
                    Label = TypeLabelAt(i),
                    LabelPx = RarityFontPx, LabelAutoMin = RarityFontAutoMin, LabelRight = true,
                    // 🔴 **A333 的实测错处**（同稀有度那一族）：原版 `'Warlord'/'Troops'/'Stratagem'` 是
                    //    `fs 23.2 · auto[10~27] · base 36` ⇒ 上限 27（旧写法 23.2）、base 36。
                    LabelAutoMax = RarityFontAutoMax, LabelBase = RarityFontBase,
                    LabelWrap = 0,                       // 🔴 dump：类型族 `折行=0`（A62 #5；⛔ 别按 LabelRight 反推）
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
        /// <param name="titlePx">四个小标题的字号。🔴 **缺省 = 共用常量 <see cref="TitleFontPx"/> = 32（卡牌页原版值）**；
        /// **异画页必须显式传 <see cref="TitleFontPxStyles"/> = 36**（A248）。原版两页都**没开自适应**（无 `auto[…]` 段）⇒
        /// 这里只给标称字号、不给下界（与卡牌页同一形状）。</param>
        /// <summary>🔴 **2026-10-18（A891 的续 · 续做 A）**：一行小标题在 `Loc` 表里的**键**
        /// （= 原版那颗 `Localize.mTerm` 的**原文**，按 pid 亲读）；
        /// **`null` = 这一行的名字我们没查过词条**（⛔ 目前**四行一条都不落在这里** —— 见下）。
        /// <para>判据（逐颗读 `bundle_menus_assets_all/MonoBehaviour/*.json` 的 `mTerm` + 同 GameObject 上 TMP 的 `m_text`）：
        /// `Army` → `MenuDeck/Filters/Army`（6 颗）· `Rarity` → `MenuDeck/HUD/Rarity`（3 颗）·
        /// `Type` → `MenuDeck/Filters/Type`（3 颗）—— 父链一律 `… > Card Filters > Filters > {Army,Rarity,Type} FIlter/Title`。</para>
        /// <para>🔴🔴 **2026-10-18（第三轮整改 · 审查 P1）就地订正（铁律 5）**：本行原来写着
        /// 「**`Energy Cost` 那一行没有键，而且那是对的**（全库 3 颗 TMP 一颗 `Localize` 都没挂）」——
        /// **那是错的**。错因 = **「全库」实际只扫了 `bundle_menus_assets_all` 一个包**。
        /// 复跑（`d:/2/新解包资源/assets_full` 下**全部 80 个 bundle**）：`m_text == "Energy Cost"` 的 TMP
        /// 共 **17 颗 / 15 个 bundle**（menus 3 + 13 个 `battlearena*` 各 1 + `mainmenuwarpforge` 1），
        /// **每一颗所在 GO 上都挂着 `Localize`，`mTerm` 一律 = `Battle/Tips/EnergyCost`**
        /// —— 其中 3 颗**就是**这三行 `Cost Filter/Title` 自己。
        /// ⇒ 它是**正经词条**（铁律 11 没有例外①），已按上表接上。
        /// ⚠️ 教训：**报「原版没有」之前先打出「搜过哪几个包」**（`资料/已知的坑.md`）。</para>
        /// <para>`default: null` 这个口**保留** —— 将来（或原版别的行）名字对不上任何键时，它照旧返回英文，
        /// 不会静默编一条。</para></summary>
        public static string TitleTerm(string titleText)
        {
            switch (titleText)
            {
                case "Army":        return "MenuDeck/Filters/Army";
                case "Rarity":      return "MenuDeck/HUD/Rarity";
                case "Type":        return "MenuDeck/Filters/Type";
                case "Energy Cost": return "Battle/Tips/EnergyCost";   // 🔴 第三轮整改（审查 P1）：原来误判成「原版无词条」
                default:            return null;                       // 名字对不上任何键 ⇒ 照原样印英文（⛔ 不静默编词条）
            }
        }

        /// <summary>一行小标题**该印什么字** = <see cref="TitleTerm"/> 有键就 `Loc.T(键)`、没键就**照原样**（英文）。
        /// <para>⚠️ **只用来当「显示文案」**，⛔ **别拿它去当节点名** —— 节点名（`flt_title_*` / `Title *`）
        /// 一律用 <see cref="Title.Text"/> 那份**英文**，`Editor/DeckScene.cs` 与 `Editor/CollectionScene.cs`
        /// 都按名找（把节点名一起换掉 = 整族断言连带红，见本方法调用点的注释）。</para></summary>
        public static string TitleText(string titleText)
        {
            var term = TitleTerm(titleText);
            return term == null ? titleText : Loc.T(term);
        }

        public static void BuildTitles(DeckEditorState state, float w, List<Title> titles,
                                       float titlePx = TitleFontPx)
        {
            var L = ComputeLayout(state);
            // 🔴 **`Title.Text` 是【英文原名】，一行两用**：① 渲染方拿它当**显示文案的输入**
            //   （要过 `TitleText` 换成词条文案）；② **节点名**就直接由它拼出来
            //   （`DeckRuntime` 的 `"flt_title_" + Text.Replace(" ","_")`、`CollectionWindow` 的 `"Title " + Text`）
            //   ⇒ ⛔ **这一列一个字都别改成中文**（改了 = 两个宿主的按名查找全断）。
            titles.Add(new Title { Text = "Army", R = new PxRect(TitleArmyX, L.ArmyTop, w, L.ArmyTop + TitleH), Px = titlePx, Left = true });
            titles.Add(new Title { Text = "Rarity", R = new PxRect(TitleRarityX, L.RarityTop + TitleRarityYIn, w, L.RarityTop + TitleRarityYIn + TitleH), Px = titlePx, Left = true });
            titles.Add(new Title { Text = "Energy Cost", R = new PxRect(TitleCostX, L.CostTop + TitleCostYIn, w, L.CostTop + TitleCostYIn + TitleH), Px = titlePx, Left = true });
            titles.Add(new Title { Text = "Type", R = new PxRect(TitleTypeX, L.TypeTop + TitleTypeYIn, w, L.TypeTop + TitleTypeYIn + TitleH), Px = titlePx, Left = true });
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

        // 🔴🔴 **2026-10-17（G2 · D39/D40/D41/D42）：卡背抽屉【两扇窗又是两份值】—— 铁律 5·c。**
        //   本段原来只照抄了**收藏窗**那一份（`Collection Menu Variant > Cardback Tab > … > Cosmetic FIlter`），
        //   而**卡组编辑窗**那一棵（`Deck Editing Menu > Content Area > Cosmetic Display > Cosmetic FIlter`）
        //   有三处序列化值不同：
        //     处                    | 卡组编辑 `Deck Editing Menu`          | 收藏窗 `Collection Menu Variant`
        //     Army 行 `m_Spacing.y` | **20**（⇒ 行高 **630**，不是 550）    | 0（⇒ 550）
        //     `Owned Toggle/Image`  | `sd(**80**,0)` ⇒ 宽 **80**            | 锚 `a(0.7,0)-(1,1) sd(-30,0)` ⇒ `0.3w−30`（= 69.52）
        //     `Owned Toggle/Label`  | `sd(**230**,0)` ⇒ 宽 **230**（25..255）| 锚 `a(0,0)-(0.7,1) sd(-25,0)` ⇒ 25..`0.7w`（= 232.21）
        //   （`Spacing` 15 / `Spacing (1)` 12.81 · `Image` 右缘 = `w−25` · `Label` 左缘 = 25 —— 这些**两窗相同**。）
        // 🔴 **判据（2026-10-17 现读，可复跑）—— 「找唯一的那一颗」：**
        //   🔴🔴 **①【首选】原始 RT 序列化字段**（`assets_full/bundle_menus_assets_all/RectTransform/*.json`，
        //      **2026-10-18（A890）现读** · 读 RT 才有 `m_AnchorMin/Max` `m_Pivot`）：
        //      · 卡组编辑 `…/Cosmetic FIlter/Filters/Owned Toggle/Image` = `RectTransform_-5843448329795342556.json`：
        //        `m_AnchorMin (1,0)` · **`m_AnchorMax (1,1)`** · `m_Pivot (1,0.5)` · `m_AnchoredPosition (-25,0)` ·
        //        **`m_SizeDelta (80,0)`** ⇒ **右锚**：右缘恒 = 父级右缘 − 25（**面板一宽它跟着走**），
        //        **固定的那个量是【宽 80】、不是右缘** —— `308.90` 只是 `w = 333.90` 那一刻的取值
        //        ⇒ 「固定 308.9 还是 `w−25`」**答 `w − 25`**（= `ToggleRowRects` 的 `ix2 = w − ToggleIconRightIn`）。
        //      · `…/Owned Toggle/Label` = `RectTransform_-6194547466839724252.json`：`m_AnchorMin (0,0)` ·
        //        `m_AnchorMax (0,1)` · `m_Pivot (0,0.5)` · `apos (25,0)` · `sd (230,0)`
        //        ⇒ **左锚 + 固定宽** ⇒ 25..**255**、**不随 `w` 变**（⚠️ 同一行两半规则**不同**，别一刀切）。
        //      · 收藏窗那棵（`…Cardback Tab/Cardback Display/Cosmetic FIlter/Filters/Owned Toggle (1)`）那颗
        //        `Image` = `RectTransform_1643317952186213077.json`：`AnchorMin (0.7,0)` · **`AnchorMax (1,1)`** ·
        //        `Pivot (1,0.5)` · `apos (-25,0)` · `sd (-30,0)` ⇒ **同样右锚**、宽 `0.3w−30`
        //        ⇒ **两棵树差的只是【宽】，右缘规则相同**。
        //      · `EverguildToggle__*.c`（`d:/2/tools/decomp_full/`）**19 个方法体里 0 处**写锚/尺寸 ⇒ 这两颗锚**静态**。
        //      ⚠️ **2026-10-18（A890）订正（铁律 5）**：这一段原来**只写 ①·b 那一行**（`menu_dump` 的 `锚(…)` 三列），
        //      没落到 `m_AnchorMin/Max`/`m_Pivot` 上 ⇒ 判不出「面板宽可变时那颗 `Image` 的右缘是什么」。
        //      ⛔ 抄判据时还要记住 `menu_dump` 自己的读数口径②：**被布局组管的节点**那三列是**布局后的模拟值**
        //      （`Image`/`Label` 不是布局组子件 ⇒ 两条路径本来就该逐字段一致，现读也**确实一致**）。
        //   ⚠️ **①·b（复核路径）** `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 20 --md`
        //      ⇒ `…/Cosmetic FIlter/Filters/Army Filter/Content` = `**【GridLayoutGroup】** cellSize=100×100
        //      spacing={'x': 7.0, 'y': **20.0**} pad=14,0,0,0`；
        //      `…/Cosmetic FIlter/Filters/Owned Toggle/Image` = `锚(1,0)-(1,1) apos(-25,0) sd(**80**,0)`；
        //      `…/Cosmetic FIlter/Filters/Owned Toggle/Label` = `锚(0,0)-(0,1) apos(25,0) sd(**230**,0)`。
        //      同命令打在 `Collection Menu Variant` 上（`Cardback Tab/…`）= `spacing.y **0**` · 那两颗走**锚点**。
        //   ② **全包逐实例枚举**（`bundle_menus_assets_all` 的 `MonoBehaviour/` 与 `RectTransform/`，两个目录都数过）：
        //      · `m_CellSize` 100×100 的 **9** 颗里 `spacing.y = 20` **只有 1 颗**
        //        = `MonoBehaviour_7231577424604229412`，它的父链 = `Content < Army Filter < Filters <
        //        Cosmetic FIlter < Cosmetic Display < Content Area < **Deck Editing Menu**`；其余 8 颗 = 7 颗 `(7,0)` + 1 颗 `(5,0)`。
        //      · `m_SizeDelta = (230,0)` 的 RectTransform **全包只有 1 颗**，父链同上 ⇒ 卡组编辑卡背抽屉那颗 `Label`。
        //      · `(80,0)` 有 **3** 颗：卡组编辑卡背抽屉那颗 `Image` + 两棵 `Header` 的 `Army Icon`
        //        ⇒ **这一条不能只按 `sd` 认，要连父链一起看**。
        //   ③ 修正量自洽：`Owned` 行顶 = 面板顶 156.0 + `CosmoSpacing1` 15 + Army 行 **630** + `CosmoSpacing2` 12.81
        //      = **813.81**；我们原来是 `15 + 550 + 12.81 + 156 = **733.81**`，差 **80** = 4×20（第 2..5 行各 −20k）。
        // ⛔ **缺省一个字不许动**（= 收藏窗那份，已对上）—— 卡组编辑走**显式传参**
        //    （`BuildCosmetics` 的三个可选形参），形状照本文件 A247 的先例。
        /// <summary>🔴 **卡组编辑窗**卡背抽屉 Army 行 `Content` 的 `m_Spacing.y`。收藏窗**不用**它（缺省 0）。
        /// 13 个阵营 = 5 行 ⇒ 行高 550 → **630**。判据 → 上面那段那三条。</summary>
        public const float CosmoArmySpYDeckEdit = 20f;
        /// <summary>🔴 **卡组编辑窗**卡背抽屉 `Owned Toggle/Image` 的**宽**（原版 `sd(80,0)`；右缘仍是 `w−25`）。
        /// 收藏窗**不用**它（缺省 0 ⇒ 走锚点式 `0.3w−30`）。判据 → 上面那段那三条。</summary>
        public const float CosmoIconWDeckEdit = 80f;
        /// <summary>🔴 **卡组编辑窗**卡背抽屉 `Owned Toggle/Label` 的**宽**（原版 `sd(230,0)` ⇒ 25..255）。
        /// 收藏窗**不用**它（缺省 0 ⇒ 走锚点式 25..`0.7w`）。判据 → 上面那段那三条。</summary>
        public const float CosmoLabWDeckEdit = 230f;

        /// <summary>`Army Filter` 那一行的高度 —— 与卡牌那套**同一个 `ArmyRowH`**（格数决定行数）。
        /// <param name="armySpY">`Content` 的 `m_Spacing.y`：缺省 **0 = 收藏窗**；卡组编辑传
        /// <see cref="CosmoArmySpYDeckEdit"/>（见上面那段）。</param></summary>
        public static float CosmoArmyRowH(int armyCount, float armySpY = 0f) { return ArmyRowH(armyCount, armySpY); }

        /// <summary>`Owned Toggle` 那一行的行顶（相对抽屉顶）—— Army 行一高，它跟着往下走。</summary>
        public static float CosmoOwnedTop(int armyCount, float armySpY = 0f)
        { return CosmoSpacing1 + CosmoArmyRowH(armyCount, armySpY) + CosmoSpacing2; }

        /// <summary>卡背抽屉整块内容的高度（px）—— 判断要不要滚动用它。</summary>
        public static float CosmoContentH(int armyCount, float armySpY = 0f)
        { return CosmoOwnedTop(armyCount, armySpY) + ToggleRowH; }

        /// <summary>🔴 **2026-10-09（A247）**：卡背抽屉那颗 `'Owned only'` 的**自适应下界**（画布 px）。
        /// **两扇窗不同**（按 A77⑩「按窗分参数」裁定）：
        ///   · **卡组编辑窗**（本常量）= 原版 **26** ——
        ///     `Deck Editing Menu > … > Cosmetic Display > Cosmetic FIlter > Filters > Owned Toggle > Label`
        ///     = `字号 32 · auto[26~32] · 折行 0`
        ///     （现读 `python 工具/menu_dump.py bundle_menus_assets_all "Deck Editing Menu" --depth 18 --md`）；
        ///   · **收藏窗**卡背页那颗 = 原版 `auto[18~32]`，= 共用常量 <see cref="ToggleFontAutoMin"/>（本来就对）。
        /// ⛔ **别改共用常量 <see cref="ToggleFontAutoMin"/>** —— 那是收藏窗的实测值，改它会把收藏窗一起改歪。</summary>
        public const float CosmoOwnedFontAutoMinDeckEdit = 26f;

        /// <summary>卡背抽屉里那两行的格子 —— **只有「13 个阵营格 + 1 个 Owned 开关」**，
        /// 没有搜索框 / 稀有度 / 费用 / 类型（原版那棵树里就没有）。
        /// 坐标 = **抽屉内 px**；`w` = 抽屉宽。</summary>
        /// <param name="labelAutoMin">`'Owned only'` 那颗 `Label` 的**自适应下界**（原版 `auto(min~max)` 的 min）。
        /// 🔴 **缺省 = 共用常量 <see cref="ToggleFontAutoMin"/> = 18（= 收藏窗的原版值）**；
        /// **卡组编辑窗必须显式传 <see cref="CosmoOwnedFontAutoMinDeckEdit"/> = 26**（A247 的真偏离就在这一格）。</param>
        /// <param name="armySpY">🔴 **2026-10-17（G2 · D39/D40）**：Army 行 `Content` 的 `m_Spacing.y`。
        /// **缺省 0 = 收藏窗**；**卡组编辑窗显式传 <see cref="CosmoArmySpYDeckEdit"/> = 20**
        /// （⇒ Army 行 550 → **630**、`Owned` 行跟着下移 **80**）。</param>
        /// <param name="iconW">🔴 **同上（D41）**：`Owned Toggle/Image` 的固定宽。**缺省 0 = 锚点式（收藏窗）**；
        /// **卡组编辑窗传 <see cref="CosmoIconWDeckEdit"/> = 80**。</param>
        /// <param name="labW">🔴 **同上（D42）**：`Owned Toggle/Label` 的固定宽。**缺省 0 = 锚点式（收藏窗）**；
        /// **卡组编辑窗传 <see cref="CosmoLabWDeckEdit"/> = 230**（⇒ 右缘 232.21 → **255**）。</param>
        public static void BuildCosmetics(List<string> facs, DeckFilter f, float w, List<Cell> cells,
                                          float labelAutoMin = ToggleFontAutoMin, float armySpY = 0f,
                                          float iconW = 0f, float labW = 0f)
        {
            float armyTop = CosmoSpacing1;
            for (int i = 0; i < facs.Count; i++)
            {
                float x = ArmyPadL + (i % ArmyPerRow) * (ArmyCell + ArmySpX);
                // 🔴 **G2/D39**：行距 = `cell` + `m_Spacing.y`（缺省 0 ⇒ 与原来逐位相同；卡组编辑 +20）
                float y = armyTop + ArmyContentTop + (i / ArmyPerRow) * (ArmyCell + armySpY);
                var rr = new PxRect(x, y, x + ArmyCell, y + ArmyCell);   // 阵营行**背景铺满格**
                cells.Add(new Cell
                {
                    R = rr, Bg = rr, Icon = DeckRuntime.FactionIcon(facs[i]), Label = null,
                    Key = "$fac:" + facs[i], On = f.Faction == facs[i],
                    OffTint = OffTintFaction,
                });
            }

            PxRect row, icon, lab;
            ToggleRowRects(w, CosmoOwnedTop(facs.Count, armySpY), out row, out icon, out lab, iconW, labW);
            cells.Add(new Cell
            {
                R = row, Bg = icon, Icon = ToggleSprite, IconOff = ToggleSpriteOff, Lab = lab,
                // 🔴 **2026-10-18（双语线 · P1）**：与卡牌页那颗**共用同一条键**
                //   （`Deck Editing Menu/…/Cosmetic FIlter/Filters/Owned Toggle/Label` 实读 = `MenuDeck/Filters/ShowOwnedOnly`）。
                //   ⚠️ **同日收口（铁律 5）**：原来是 `TermOr(OwnedOnlyTerm, "Owned only")` 闸门 ⇒ 已收成裸 `Loc.T`。
                Label = Loc.T(OwnedOnlyTerm), LabelPx = ToggleFontPx, LabelAutoMin = labelAutoMin,
                // 🆕 A333/A336④：上限 **32**（= 标称，卡背页那颗原版就是 `auto[18~32]`）· base **32**
                LabelAutoMax = ToggleFontAutoMax, LabelBase = ToggleFontBase,
                // 🔴 **2026-10-05（A32④）：`false`（左对齐）** —— 判据同卡牌那两行（这一颗的 TMP 也是
                //   `m_HorizontalAlignment = 1`；`menu_dump.py "Deck Editing Menu"` 打出来的是 `对齐=Left/Middle`）。
                LabelCenter = false, Key = "$owned", On = f.Owned,
                LabelWrap = 0,          // 🔴 dump（卡背页那份）：`'Owned only' 折行=0`（A62 #6，`auto[26~32]`）
                                        // ⚠️ **2026-10-08 订正（铁律 5）：`auto[26~32]` 那份 dump 是【卡组编辑窗】的卡背抽屉**
                                        //   （`Deck Editing Menu/…/Owned Toggle/Label`）；**收藏窗**卡背页那颗原版是 `auto[18~32]`
                                        //   （= 与本行 `ToggleFontAutoMin` 本就一致、不是偏离）。真偏离在 `Deck/DeckRuntime.cs`
                                        //   那一路（按窗分参数传 26）⇒ 另账 **A247**。
            });
        }
    }
}
