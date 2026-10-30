// B103F5_Assert.cs — 执行代理 `B103_F5`（2026-10-30）落地 **AUD05 独立复验**结论的断言。
//
// ============================ 覆盖 ============================
//  · `Run()`        —— **收藏窗四页网格**的「两档列数 + 两档格尺寸 + 居中量」。逐个强制 `SmallScreenUI` 两档：
//                      桌面 **6 / 6 / 6 / 6**（卡池 / 异画 / 卡背 / 卡组）· 小屏 **4 / 4 / 5 / 5**。
//  · `RunShopGrid()` —— **商店栅格列数 = 4**（原版 `GridLayoutGroup.m_Constraint = 0` = Flexible）。
//
// ============================ 判据（⛔ 期望值一律**不**从被断的属性读回来）============================
//  收藏窗四页 —— `资料/完全复刻/判据/AUD05_state_matrix_收藏窗.md` §1·2 / §2·2（prefab **逐字段**实读）：
//    | 页     | `_cellWidth × _cellHeight` | `_spacingX` | `_mobileSizeScale` | 小屏格          | prefab 小屏列 |
//    | 卡池   | 262.5 × 384                | 0           | **1.5**            | 393.75 × 576    | 4 |
//    | 异画   | 262.5 × 384（与卡池同值）   | 0           | **1.5**            | 393.75 × 576    | 4 |
//    | 卡背   | 250 × 405                  | 0           | **1.2**            | 300 × 486       | 5 |
//    | 卡组   | 225 × 364.5                | **20**（**不乘**系数） | **1.25**  | 281.25 × 455.625 | 5 |
//  ⇒ 列数 = `floor(视口宽 ÷ (_cellWidth + _spacingX))`（`decomp_full/PolyAndCode.UI.VerticalRecyclingSystem__ConfigureColumnNumber.c`）。
//    视口（我方实测量 1589.7 / 1589.79 / 1584.57 / 1589.1，与 prefab 差 ≤0.25px、**不翻列数**）：
//      桌面 6 / 6 / 6 / 6 · 小屏 4 / 4 / 5 / 5。
//  商店 —— `资料/完全复刻/判据/AUD05_state_matrix_商店窗.md` §1·1 / §1·2：Content 拉伸（`ContentSizeFitter` 只控高）
//    ⇒ 宽 ≡ 视口 1590.24；`cellSize.x = 335.6` · `spacing 0` ⇒ `floor(1590.24 ÷ 335.6)` = **4**。
//    ⚠️ 序列化的 `m_ConstraintCount: 2` 只有在 `FixedColumnCount`/`FixedRowCount` 两档才被读（UGUI `GridLayoutGroup.cs:245,253`）
//       ⇒ 在 `m_Constraint = 0`(Flexible) 下它是**死值**。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  ① 把四页系数改回「统一 ×1.5」= 2026-10-30 `B103_R2` 那一版 ⇒ 卡背小屏变 `375×607.5`（5→**4** 列）、
//     卡组小屏变 `337.5×546.75`（5→**4** 列）⇒ `S_Cosmo*` / `S_Deck*` 那几条**当场红**。
//  ② 反过来只把这里的期望改回 4 而不改实现 ⇒ 同样红（期望是**字面量**，不是从 `CosmoCols`/`DeckCols` 读回来的）。
//  ③ 商店：`RunShopGrid()` 除了断 `GridCols == 4`，还用**可行性**独立复算一遍
//     （`4×335.6 ≤ 视口 < 5×335.6`）—— 把 `GridCols` 改回 2 或改成 5，**两条都红**。
//  ④ 卡组页 `DeckGridOffsetX` 的期望（69.55 / 51.43）同时钉住「`_spacingX 20` **不乘**系数」：
//     若把 spacing 也乘了 1.25（=25）⇒ 小屏偏移算成 `((1589.1 − 306.25×5) + 25)×0.5 = 54.7` ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不碰宿主的聚合行 / 不碰 Step()）============================
//  `Editor/CollectionScene.cs` 的聚合行里加（**同一物理行、`//` 之前**）：
//      B103F5_Assert.Run(); _sink.Pass += B103F5_Assert.Pass; _sink.Fail += B103F5_Assert.Fail;
//  `Editor/ShopScene.cs` 同样加：
//      B103F5_Assert.RunShopGrid(); _sink.Pass += B103F5_Assert.Pass; _sink.Fail += B103F5_Assert.Fail;
//  ⚠️ 两个入口都**只读静态属性**（不建窗、不重排页、不写盘），可在 `Run()` 任意位置调。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B103_F5`：AUD05 复验后的「收藏窗四页网格两档」与「商店栅格 4 列」。</summary>
public static class B103F5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B103F5] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 收藏窗四页（两档）

    /// <summary>收藏窗四页的「桌面 6/6/6/6 ↔ 小屏 4/4/5/5」+ 两档格尺寸 + 居中量。
    /// <para>⚠️ 进去先记下 `SmallScreenUI` 的**进场档**、收尾放回（⛔ 不许硬写 false —— 同 `A1391` 的教训）。</para></summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B103F5 · 收藏窗四页网格：桌面 6/6/6/6 ↔ 小屏 4/4/5/5（四页 `_mobileSizeScale` 各不相同）");

        bool wasPersist = SmallScreenUI.PersistOverride;
        bool wasEnabled = SmallScreenUI.Enabled;
        SmallScreenUI.PersistOverride = true;          // ⛔ 自检不许动玩家的真设置
        try
        {
            // ---------------------------------------------------------------- 态一：桌面（出厂档）
            SmallScreenUI.Set(false);
            Tr(!SmallScreenUI.Enabled, "（前提）态一真的落在**桌面档**（`SmallScreenUI.Enabled == false`）—— 不成立则下面 12 条读数无意义");

            C(CollectionWindow.CardsCols, 6, "桌面 · 卡池 `Cards`：`floor(1589.7 ÷ 262.5)` = **6** 列");
            C(CollectionWindow.StyleCols, 6, "桌面 · 异画 `Styles`：`floor(1589.79 ÷ 262.5)` = **6** 列");
            C(CollectionWindow.CosmoCols, 6, "桌面 · 卡背 `Cosmo`：`floor(1584.57 ÷ 250)` = **6** 列");
            C(CollectionWindow.DeckCols, 6, "桌面 · 卡组 `Deck`：`floor(1589.1 ÷ (225+20))` = **6** 列");

            N(CollectionWindow.CardsCellW, 262.5f, 0.01f, "桌面 · 卡池格宽 = **262.5**（prefab `_cellWidth`）");
            N(CollectionWindow.CardsCellH, 384f, 0.01f, "桌面 · 卡池格高 = **384**（prefab `_cellHeight`）");
            N(CollectionWindow.StyleCellW, 262.5f, 0.01f, "桌面 · 异画格宽 = **262.5**（与卡池同值）");
            N(CollectionWindow.StyleCellH, 384f, 0.01f, "桌面 · 异画格高 = **384**");
            N(CollectionWindow.CosmoCellW, 250f, 0.01f, "桌面 · 卡背格宽 = **250**");
            N(CollectionWindow.CosmoCellH, 405f, 0.01f, "桌面 · 卡背格高 = **405**");
            N(CollectionWindow.DeckCellW, 225f, 0.01f, "桌面 · 卡组格宽 = **225**");
            N(CollectionWindow.DeckCellH, 364.5f, 0.01f, "桌面 · 卡组格高 = **364.5**");
            N(CollectionWindow.DeckGridOffsetX, 69.55f, 0.3f,
              "桌面 · 卡组居中偏移 = `((1589.1 − 245×6) + 20)/2` = **69.55**（`CreateCellPool` 那条 `((W − (sp+cellW)·cols) + sp)·0.5`）");

            // ---------------------------------------------------------------- 态二：小屏（四页四个系数）
            SmallScreenUI.Set(true);
            Tr(SmallScreenUI.Enabled, "（前提）态二真的落在**小屏档**（`SmallScreenUI.Enabled == true`）");

            // 🧨 列数：卡池/异画 ×1.5 ⇒ 4；卡背 ×1.2 ⇒ 5；卡组 ×1.25 ⇒ 5。改回「统一 ×1.5」⇒ 后两条红。
            C(CollectionWindow.CardsCols, 4, "小屏 · 卡池 `Cards`（`_mobileSizeScale` **1.5**）：`floor(1589.7 ÷ 393.75)` = **4** 列");
            C(CollectionWindow.StyleCols, 4, "小屏 · 异画 `Styles`（**1.5**）：`floor(1589.79 ÷ 393.75)` = **4** 列");
            C(CollectionWindow.CosmoCols, 5, "小屏 · 卡背 `Cosmo`（**1.2**，⛔ 不是 1.5）：`floor(1584.57 ÷ 300)` = **5** 列");
            C(CollectionWindow.DeckCols, 5, "小屏 · 卡组 `Deck`（**1.25**，⛔ 不是 1.5）：`floor(1589.1 ÷ (281.25+20))` = **5** 列");

            N(CollectionWindow.CardsCellW, 393.75f, 0.01f, "小屏 · 卡池格宽 = `262.5 × 1.5` = **393.75**");
            N(CollectionWindow.CardsCellH, 576f, 0.01f, "小屏 · 卡池格高 = `384 × 1.5` = **576**");
            N(CollectionWindow.StyleCellW, 393.75f, 0.01f, "小屏 · 异画格宽 = **393.75**");
            N(CollectionWindow.StyleCellH, 576f, 0.01f, "小屏 · 异画格高 = **576**");
            N(CollectionWindow.CosmoCellW, 300f, 0.01f, "小屏 · 卡背格宽 = `250 × 1.2` = **300**");
            N(CollectionWindow.CosmoCellH, 486f, 0.01f, "小屏 · 卡背格高 = `405 × 1.2` = **486**");
            N(CollectionWindow.DeckCellW, 281.25f, 0.01f, "小屏 · 卡组格宽 = `225 × 1.25` = **281.25**");
            N(CollectionWindow.DeckCellH, 455.625f, 0.01f, "小屏 · 卡组格高 = `364.5 × 1.25` = **455.625**");

            // 🧨 居中量：同时钉住「`_spacingX 20` **不乘**系数」（把 20 也乘 1.25 ⇒ 54.7 ⇒ 红）
            N(CollectionWindow.DeckGridOffsetX, 51.43f, 0.3f,
              "小屏 · 卡组居中偏移 = `((1589.1 − (20+281.25)×5) + 20)/2` = **51.43**（`_spacingX 20` 原值、**不乘** 1.25）");
            N(CollectionWindow.CosmoPadX, 42.285f, 0.3f,
              "小屏 · 卡背居中偏移 = `(1584.57 − 5×300)/2` = **42.285**（与桌面 6×250 恰好同值 —— 6×250 = 5×300 = 1500，**巧合**、⛔ 别当公式）");
        }
        finally
        {
            SmallScreenUI.Set(wasEnabled);              // `A1391`：放回**进场档**（⛔ 不硬写 false）
            SmallScreenUI.PersistOverride = wasPersist;
        }
        Tr(SmallScreenUI.Enabled == wasEnabled, "（收尾）`SmallScreenUI` 已放回入场档");
        Tr(SmallScreenUI.PersistOverride == wasPersist, "（收尾）`PersistOverride` 写盘闸已放回入场档");

        Debug.Log(_sink.P + "B103F5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 商店栅格

    /// <summary>商店货架栅格 = **4 列**（原版 `GridLayoutGroup.m_Constraint = 0` Flexible）。
    /// <para>🧨 除了直接断 `GridCols`，还用「4 个放得下、5 个放不下」**独立复算**一遍 —— 期望值不来自 `GridCols`。</para></summary>
    public static void RunShopGrid()
    {
        MenuCheck.Section(_sink, "B103F5 · 商店货架栅格列数（原版 `GridLayoutGroup.m_Constraint = 0` Flexible ⇒ 4 列）");

        C(ShopTabPage.GridCols, 4,
          "商店栅格 = **4** 列（`floor(Content 宽 1590.24 ÷ cellSize.x 335.6)`；`m_ConstraintCount: 2` 在 Flexible 下是**死值**）");

        // 🧨 可行性复算（只用 prefab 值：cellSize 335.6 + Content 拉伸到视口宽 —— ⛔ 不读 `GridCols`）
        float vpW = ShopTabPage.ScrollView.W;
        Tr(4f * ShopTabPage.CellW <= vpW + 0.01f,
           $"…可见域下界：4 × 335.6 = **{4f * ShopTabPage.CellW:F2}** ≤ 视口 **{vpW:F2}**（第 4 个放得下）");
        Tr(5f * ShopTabPage.CellW > vpW,
           $"…可见域上界：5 × 335.6 = **{5f * ShopTabPage.CellW:F2}** > 视口 **{vpW:F2}**（第 5 个放不下）");

        // 行数 / 内容高走同一条式子：内容高 = ceil(件数 ÷ 列数) × 475 + 7（`ContentSizeFitter` 只控高）
        Tr(ShopTabPage.ScrollView.W > ShopTabPage.CellW,
           "（前提）视口宽 > 一格宽（否则下面「至少 1 列」那条退化成恒真）");

        Debug.Log(_sink.P + "B103F5(商店) 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
