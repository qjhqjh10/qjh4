// B97W15_Assert.cs —— 执行代理 `B97_W15`（第九十七会话 · 波 1 补位）：商店「售罄格变灰」
//
// ============================ 它盯的是什么 ============================
//   账 `商店:297` 的**最后一条残差**：原版售罄格的价签钮会**变灰**，我们原来只做了「拒绝 + 出声」（`RefuseSoldOut`）。
//   本轮落地 = `Shell/ShopTabPage.cs` 的 `BuildCell` 那句
//       `wb.Bind(...); wb.TintOn(iconQ); if (ShopData.SoldOut(_page, idx)) wb.SetSoftDisabled(true);`
//   判据（⛔ 别在这里抄第二份）→ `资料/完全复刻/判据/B97_R5_零散剩余施工单.md` §一·补 ① + §四·1；
//   本轮报告 → `资料/完全复刻/报告/B97_W15.md`。
//
//   🔴 **旧说法「要取 prefab 的 `m_DisabledColor`」已被推翻**（施工单 §四·1）：全解包 70/70 颗
//   `PriceDisplayButton` 的 `button` 都是 `m_Transition=2`(SpriteSwap) + `customColorTintTransitions=0`
//   + `m_DisabledSprite=null` ⇒ `EverguildButton__DoStateTransition.c` 头一句就把自写颜色段整段跳过
//   ⇒ `m_DisabledColor = (0.7843,…)` 是**死值**。真路 = `SetToStateActiveOrDisabled(false)` → `SwitchMaterial`
//   （闸 `colorTintGreyOnDisable = 1`，70/70）→ **换 `Everguild/UI/Greyscale` 材质**
//   ⇔ 我们已有的 `WindowButton.RefreshGray`。
//
// ============================ 怎么用（宿主）============================
// 宿主 = `Editor/ShopScene.cs`（`ShopScene.Run`）。在 `B97SHOP2Assert.Run();` **那一行之后**加两行：
//   `B97W15Assert.Run(win); _sink.Pass += B97W15Assert.Pass; _sink.Fail += B97W15Assert.Fail;`
//   · 参数 `win` = `RunCore()` 里那个 `var win = Build(out var root);`（`B97SHOP2Assert` 之后仍在作用域）。
//   · ⚠️ **必须排在几何断言之后**（本段会 `page.Setup()` 重建 Cards 页的节点树；收尾会再建回出厂态）。
//   · 写盘全程走 `ShopData.OverrideDir` 临时目录，⛔ 不碰玩家真存档。
//   · 本文件**不建持久资产**，不建 GameObject（只用宿主已有的商店窗）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//   · W15-3 / W15-4 / W15-11：把 `SetSoftDisabled` 改成**无条件**调用（去掉 `if (ShopData.SoldOut…)`）
//     ⇒ 未售罄那两格与复位后那一格都变灰 ⇒ 三条同时红。
//   · W15-7 / W15-8 / W15-9：把 `wb.SetSoftDisabled(true)` 那一句删掉 ⇒ 灰根本没发生 ⇒ 三条红。
//   · W15-8：把实现改成「材质颜色乘 0.7843」（= 旧说法那条**错路**）⇒ `GrayedForTest` 可能仍真、
//     但 `Generic UI Button` 的 shader 不是 `Everguild/UI/Greyscale`、`Tint` 也不再等于 `PriceTint` ⇒ 本条红。
//   · W15-9：把 `wb.TintOn(iconQ)` 删掉 ⇒ 按钮灰了、`Price Icon` 仍是原材质 ⇒ 本条红（原版它在按钮子树里、一起灰）。
//   · W15-6b：把 `DoBuy` 里那句 `Setup()` 删掉（生产路径不再重画）⇒ 本段的重画读口失效（见报告 §三 的说明）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B97_W15`：商店「限购售罄 ⇒ 价签钮变灰」（账 `商店:297` 残差）的断言。
/// <para>⛔ **期望值全是字面量/原版常量**（`GrayShaderName` · `PriceTint` · `1` / `false` / `true`），
/// 不从被测实现里读回「应该灰」这个结论。</para></summary>
public static class B97W15Assert
{
    static readonly CheckSink _sink = new CheckSink("[W15] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // 格节点路径 = `Setup()` 里 `Node(_root,"Packs Scroll View") → "Viewport" → "Content"` + `BuildGrid` 的命名
    const string CellPath = "Packs Scroll View/Viewport/Content/CatalogItemShopContainer_";

    static Transform Cell(ShopTabPage page, int i)
        => page == null ? null : page.transform.Find(CellPath + i);

    /// <summary>一格价签钮 = `CatalogItemShopContainer_i/Hit` 上那颗 `WindowButton`（判据 = `BuildCell` 的建树顺序）。</summary>
    static WindowButton PriceButton(Transform cell)
    {
        if (cell == null) return null;
        var hit = cell.Find("Hit");
        return hit != null ? hit.GetComponent<WindowButton>() : null;
    }

    static ImageQuad Quad(Transform cell, string name)
    {
        if (cell == null) return null;
        var t = cell.Find(name);
        return t != null ? t.GetComponent<ImageQuad>() : null;
    }

    /// <summary>这一颗 quad 现在**画在什么 shader 上**（原版灰化的判据 = 那张 shader 的名字）。</summary>
    static string ShaderOf(ImageQuad q)
    {
        if (q == null) return null;
        var mr = q.GetComponent<MeshRenderer>();
        var m = mr != null ? mr.sharedMaterial : null;
        return m != null && m.shader != null ? m.shader.name : null;
    }

    // ============================================================ 入口
    /// <summary>跑一遍。宿主传**已建好的商店窗**（`ShopWindow`）—— Cards 页从它上面取（`PageOf(0)`）。</summary>
    public static void Run(ShopWindow win)
    {
        MenuCheck.Section(_sink, "W15 售罄格变灰（账 商店:297 残差 —— 原版走**材质**，不是 `m_DisabledColor`）");

        // ---- 前提（硬检查，⛔ 不写 `if (x != null) Check(…)` 那种「前提不成立就静默跳过」）----
        Tr(win != null, "W15-0 前提：宿主传进来的 `ShopWindow` 非 null");
        Tr(WindowButton.GrayShaderAvailable,
           "W15-0b 前提：原版灰化 shader `" + WindowButton.GrayShaderName + "` 取得到"
           + "（取不到时 `RefreshGray` 会**静默不变灰**并记 `MissingGrayArt` ⇒ 后面的「灰了」会假红/假绿）");
        var page = win != null ? win.PageOf(0) : null;
        Tr(page != null, "W15-0c 前提：Cards 页（第 1 页）的 `ShopTabPage` 建出来了");
        if (page == null) return;
        C(page.Type, WindowTabType.ShopCards, "W15-0d 前提：`PageOf(0)` 就是 Cards 页（`WindowTabType.ShopCards`）");

        string prevDir = ShopData.OverrideDir;
        ShopData.OverrideDir = "d:/4/_tmp_view/shop_selftest_soldoutgray";   // ⛔ 不动玩家真存档
        try
        {
            ShopData.ResetForTest();      // 出厂：第 1 件 1/5（还有 1 支）、第 2 件 2/5、第 3/4 件 `0/0`
            page.Setup();                 // 按当前数据重建 Cards 页（= 生产路径「买完之后那一跳」）

            // ---- 前提：三件商品的三态（出厂值全是字面量，⛔ 不从被测实现读）----
            C(ShopData.AvailableOf(0, 0), 1, "W15-1 前提：第 1 件限购剩 **1/5**（还能买一次）");
            C(ShopData.SoldOut(0, 0), false, "W15-1b 前提：第 1 件**没售罄**");
            C(ShopData.SoldOut(0, 2), false, "W15-1c 前提：第 3 件 `AvailableMax = 0`（= **0/0 不是售罄**，原版 `max < 1 ⇒ 可买`）");

            var cell0 = Cell(page, 0);
            Tr(cell0 != null, "W15-2 前提：第 1 格 `CatalogItemShopContainer_0` 建出来了");
            var wb0 = PriceButton(cell0);
            Tr(wb0 != null, "W15-2b 前提：第 1 格的价签钮（`…/Hit` 上的 `WindowButton`）在");
            Tr(Quad(cell0, "Generic UI Button") != null, "W15-2c 前提：那颗价签图（`Generic UI Button`）在");
            Tr(Quad(cell0, "Price Icon") != null, "W15-2d 前提：那颗价格图标（`Price Icon` = 原版 `Price Display/icon`）也在"
               + "（取不到时 `MenuDraw.Rect` 返回 null ⇒ 原版那颗件本来就不存在，下面的「一起灰」无从谈起 —— ⛔ 不许静默跳过）");

            // ---- 两态对照 ①：未售罄 ⇒ 不灰（防「无条件变灰」）----
            Tr(wb0 != null && !wb0.GrayedForTest, "W15-3 ★★ 未售罄（1/5）⇒ 价签钮**没灰**｜🧨 去掉 `if (ShopData.SoldOut…)` ⇒ 本条红");
            var wb2 = PriceButton(Cell(page, 2));
            Tr(wb2 != null && !wb2.GrayedForTest,
               "W15-4 ★★ `0/0`（**没有**限购信息）那一格也**没灰**"
               + "｜🧨 把 `SoldOut` 误写成 `Available <= 0` ⇒ 本条红（原版 `max < 1 ⇒ 可买`）");

            // ---- 买掉最后一件（`ShopData.Buy` = `DoBuy` 里那句原样的数据层调用）----
            int idBefore = cell0.GetInstanceID();
            int owned0 = ShopData.OwnedOf(0, 0);
            string got = ShopData.Buy(0, 0);
            Tr(ShopData.PurchaseSucceeded(got), "W15-5 前提：还剩 1 支 ⇒ `ShopData.Buy` 成功（返回 `" + got + "`）");
            C(ShopData.SoldOut(0, 0), true, "W15-5b 前提：卖完 ⇒ `ShopData.SoldOut` 真");
            C(ShopData.OwnedOf(0, 0), owned0 + 1, "W15-5c 前提：拥有数 +1");

            // ---- 生产路径的「买完之后那一跳」（`ShopTabPage.cs:800` 那句 `Setup()`，一字不差）----
            page.Setup();

            var cell0b = Cell(page, 0);
            Tr(cell0b != null, "W15-6 ★★ 买完之后那一格**还在**（重建成功）");
            if (cell0b == null) return;
            Tr(cell0b.GetInstanceID() != idBefore,
               "W15-6b ★★ …而且**真的重画**了（节点实例 id 变了：旧 " + idBefore + " / 新 " + cell0b.GetInstanceID()
               + "）｜🧨 那一跳不重建节点（原地不动）⇒ 本条红");

            var wb0b = PriceButton(cell0b);
            Tr(wb0b != null && wb0b.GrayedForTest,
               "W15-7 ★★★ 售罄 ⇒ 价签钮**变灰**（`GrayedForTest` 真）｜🧨 删掉 `wb.SetSoftDisabled(true)` ⇒ 本条红");

            // ---- 灰的**是哪条路**（原版 = 换材质；⛔ 不是「颜色乘 0.7843」）----
            C(ShaderOf(Quad(cell0b, "Generic UI Button")), WindowButton.GrayShaderName,
              "W15-8 ★★★ 灰走的是**材质**：价签图 `Generic UI Button` 的 shader = `" + WindowButton.GrayShaderName + "`"
              + "｜🧨 改成「颜色乘 0.7843」那条错路 ⇒ 本条红");
            var pq = Quad(cell0b, "Generic UI Button");
            Tr(pq != null
               && Mathf.Abs(pq.Tint.r - ShopTabPage.PriceTint.r) < 1e-3f
               && Mathf.Abs(pq.Tint.g - ShopTabPage.PriceTint.g) < 1e-3f
               && Mathf.Abs(pq.Tint.b - ShopTabPage.PriceTint.b) < 1e-3f,
               "W15-8b ★★ …而且**颜色没被改**（仍是 `PriceTint` 0.902/0.637/0.18，不是 0.7843 那一档）"
               + (pq == null ? "" : "｜实测 " + pq.Tint.r.ToString("F3") + "/" + pq.Tint.g.ToString("F3") + "/" + pq.Tint.b.ToString("F3")));
            C(ShaderOf(Quad(cell0b, "Price Icon")), WindowButton.GrayShaderName,
              "W15-9 ★★ 价格图标 `Price Icon` **跟着一起灰**（原版它在 `Generic UI Button` 子树里、挂 "
              + "`EverguildButtonMaterialModifier` ⇒ `GetGraphicsInChildren` 会连它一起换灰材质；我们靠 `wb.TintOn(iconQ)`）"
              + "｜🧨 删掉 `TintOn(iconQ)` ⇒ 本条红");

            // ---- 内建审计：每一颗「自称灰了」的钮，图形件材质必须全是那张原版 shader ----
            int nGray;
            string bad = WindowButton.AuditGrayLook(page.transform, out nGray);
            C(bad, "", "W15-10 ★★ `AuditGrayLook` 全过（自称灰的钮，图形件材质全是 `" + WindowButton.GrayShaderName + "`）");
            Tr(nGray >= 1, "W15-10b …而且**至少有一颗真的在灰态**（审计不是 0 条空转）");

            // ---- 两态对照 ②：复位 ⇒ 又变回不灰（⛔ 别把灰做成粘住/一次性）----
            ShopData.ResetForTest();
            page.Setup();
            var wb0c = PriceButton(Cell(page, 0));
            Tr(wb0c != null && !wb0c.GrayedForTest,
               "W15-11 ★★ 两态对照：复位（1/5 可买）⇒ 价签钮**退出灰化**｜🧨 灰做成无条件/不清 ⇒ 本条红");
            Tr(wb0c != null && !wb0c.SoftDisabled,
               "W15-11b …`SoftDisabled` 也回到 **false**（灰的**输入**被清掉，不只是材质还原）");
        }
        finally
        {
            // ⚠️ 顺序要紧：`ResetForTest()` 自己会 `DeleteStoreForTest()` ⇒ **仍在临时目录里**调它，
            //    并且「把宿主那一页恢复成出厂态」也要在关门之前做完（`EnsureSeeded` 会读盘）。
            ShopData.DeleteStoreForTest();
            ShopData.ResetForTest();
            if (win != null && win.PageOf(0) != null) win.PageOf(0).Setup();
            ShopData.DeleteStoreForTest();
            ShopData.OverrideDir = prevDir;      // 关门（后面的节不再碰盘）
        }
    }
}
