// B116W4_Assert.cs — 执行代理 `B116_W4`（2026-11-02）落地账 `B-320` 的断言。
//
// ============================ 它盯的是什么 ============================
//  `B-320` = `UIFlippable` 的两半：
//    ① **垂直翻转我们一支都没有**（全仓负**高** `SetUvRect` 调用 = 0 处；负**宽**镜像有 5 处先例）；
//    ② **节点级未闭合** —— 我们商店页里没有 `Counter` / `Arrow Left 1` 同名件。
//  本文件三段：A = **负高 UV 真的上下翻**（机制，零先例 ⇒ 先钉住）；B = `Counter` chip；C = `Arrow Left 1` ×2。
//  （D 段 = `B116_W15` 的 `B-325`；E 段 = `B116_W25` 的 `B-330`：`GenericRoundedSquare` 导入 +
//   `Handle` 换真九宫格 + `Scrollbar Horizontal` 自身红底 —— 判据见下面 E 段与 `ShopTabPage.BuildScrollbarBack`。）
//
// ============================ 判据（原版，2026-11-02 本代理现读 AssetRipper 导出工程）============================
//  A（机制）`Battle/ImageQuad.cs:497`：`m.uv = { (xMin,yMin), (xMax,yMin), (xMax,yMax), (xMin,yMax) }`，
//    顶点序 = 左下 / 右下 / 右上 / 左上（`:486`）⇒ `Rect(0,1,1,-1)` ⇒ uv[0] 取**图的上沿** = 上下翻。
//    期望值全部是**字面量**（⛔ 不从 `UvRect` 回读），见下面 A 段每条的注释。
//  B（`Counter`）`D:/2/tools/assetripper_out/ExportedProject/Assets/Card Shop Tab.prefab`：
//    `…/Catalog Item Shop Container/background/Counter`（GO `&1621510681728303` / RT `&224429401810989285`）
//    · 锚点 `(0, 0.14931637) – (1, 0.21753846)` · `sizeDelta (0,0)` · `pos (0,0)`，父 `background` = 337.6×477
//      ⇒ 框 **337.6 × 32.5419**，相对格顶 **372.234 … 404.776**（= 我们的 `ShopTabPage.CellCount`，逐位对上）；
//    · `Image`：sprite `40K_main_deck_card_counter`（116×36 PNG）· `m_Type 0`(Simple) · **`m_PreserveAspect 1`**
//      ⇒ 画心 **高 32.5419 · 宽 32.5419 × 116/36 = 104.857**；
//    · `UIFlippable`（`&114947810564538834`）：**`m_Horizontal: 0` / `m_Veritical: 1`**
//      ⚠️ **字段名在 prefab 里就拼成 `m_Veritical`**（按 `m_Vertical` 搜 = 0 命中）。
//    `Daily Shop Tab.prefab` / `Item Shop Tab.prefab` 各一份同值（三份各 1 处 `UIFlippable`）。
//  C（`Arrow Left 1`）`Packs Tab.prefab` / `Generic Shop Tab.prefab`（同 fileID 同值）：
//    `Packs Scroll View/Scrollbar Horizontal`（RT `&224313026266067498`）**`m_IsActive: 0`**
//      锚点 `(0,0)–(1,0)` · `pos (173.65668, 15.14)` · `sizeDelta (-509.1857, 5.469)` · `pivot (0,0)`
//      父 `Packs Scroll View` 1589.30×1009.06 ⇒ 框 **1080.114 × 5.469**，像素域 `504.33,1059.39 → 1584.44,1064.86`；
//    `Arrow Left 1`（RT `&224320256170494724`）：锚点 `(0,0.5)` · `pos (3.8147E-06,0)` · `pivot (1,0.5)`
//      · `sizeDelta 36.57007×36.692` ⇒ `467.76,1043.78 → 504.33,1080.47`；
//    `Arrow Left 1 2`（RT `&224089843903986396`）：只差 `pos.x = 10` ⇒ `477.76…514.33`；
//    两颗 `Image`：sprite `40k_general_bt_arrow`（71×71 PNG）· `m_Type 0` · **`m_PreserveAspect 0`** ⇒ 拉伸到框；
//    两颗 `UIFlippable`：**`m_Horizontal: 1` / `m_Veritical: 0`**（`&114217368724061974` / `&114060945828524476`）。
//    ⚠️ 这整棵在原版**恒不显示**（`ScrollRect.m_HorizontalScrollbarVisibility = 1` = AutoHide，uGUI
//      `ScrollRect.UpdateOneScrollbarVisibility` 对非 `Permanent` 档**无条件 `SetActive(false)`**；
//      `Assembly-CSharp` 全仓 0 处按名引用）⇒ **死装饰**，本文件只断「在不在 / 摆得对不对 / 翻没翻」，
//      ⛔ **不断任何交互**。
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 A：把 `CounterFlipV` 写回 `Rect(0,0,1,1)` ⇒ A 段负高那 4 条 + `UvRect` 真值那条 + mesh 那条**当场红**
//     （期望是字面量；而且「基线与负高的 uv[0].y 必须不同」那条会红）。
//  🧨 A：把 `ImageQuad.RebuildMesh` 的 uv 顶点序改成别的 ⇒ 基线那 4 条先红（不是只红负高那几条）⇒ 抓得到。
//  🧨 B：把这颗改成**水平镜像**（`Rect(1,0,-1,1)`）⇒ `UvRect.width == 1 && height == -1` 那条红
//     （镜像写法的 `width` 是 −1）—— 只断「调过 `SetUvRect`」是抓不到这一点的。
//  🧨 C：把 `ArrowFlipH` 去掉（不镜像）⇒ C 段 `UvRect.width == -1` 与 mesh uv[0].x 两条红。
//  🧨 C：把 `BuildScrollbarHorizontal` 删掉 ⇒ C 段存在性那 3 条红（**不是**静默跳过：每条都先断前提）。
//  🧨 E（`B116_W25`）：把 `Resources/Art/ui_menu/GenericRoundedSquare.png` 改名/挪走 ⇒
//     「`Resources.Load` 非空」那条先红（**先断存在、再断名字**，两条都硬）；
//     把 `BuildHandle` 退回 `CardArt.Solid()` ⇒ `Handle` 那 4 条（9 块 / 底图名 / tint / uv）一起红；
//     把红底那颗删掉 ⇒ E 段存在性 + 尺寸 + 三值那几条红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主其它部分）============================
//  `Editor/ShopScene.cs` 的聚合行里、`Debug.Log(... 合计 ...)` **之前**、同一物理行追加：
//      B116W4_Assert.RunShop(); _sink.Pass += B116W4_Assert.Pass; _sink.Fail += B116W4_Assert.Fail;
//  ⚠️ 本文件**自己开一扇商店窗**并在收尾销毁（同 `:6148` 与 `B102_D6` 那两处先例）——
//     ⛔ 不依赖宿主手里那扇 `win`（`:4602` 之后它已被收割，Unity 假 null）。
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B116_W4`：账 `B-320`（`UIFlippable` 垂直翻转 + 商店 `Counter` / `Arrow Left 1`）。</summary>
public static class B116W4_Assert
{
    const string Prefix = "[B116W4] ";
    static readonly CheckSink _sink = new CheckSink(Prefix) { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>一颗 quad 的**网格 uv**（`null` = 没建出来 / 没有 mesh）。</summary>
    static Vector2[] MeshUv(ImageQuad q)
    {
        if (q == null) return null;
        var mf = q.GetComponent<MeshFilter>();
        var m = mf != null ? mf.sharedMesh : null;
        return m != null ? m.uv : null;
    }

    public static void RunShop()
    {
        MenuCheck.Section(_sink, "B116_W4 · B-320：负高 UV（垂直翻）机制 + 商店 `Counter` chip + `Arrow Left 1`×2");
        RunUvMechanism();
        RunCounterAndArrows();
        Debug.Log(Prefix + "B116W4 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 段：负高 UV 机制（零先例 ⇒ 先钉住）

    static void RunUvMechanism()
    {
        MenuCheck.Section(_sink, "B116_W4 · A：`SetUvRect` 负高 = 上下翻（零先例，先钉机制）");
        var root = new GameObject("B116W4_UvProbe");
        try
        {
            var tex = CardArt.Solid();
            // ---- 硬前提（⛔ 别写 `if (前提) Check(结论)`：前提不成立时下面是**空转绿**）----
            Tr(tex != null, "（前提 · A）`CardArt.Solid()` 取到图（取不到 ⇒ A 段全部无意义，本条先红）");
            var q = ImageQuad.Create(root.transform, tex, Vector3.zero, 100f, new Vector2(0.5f, 0.5f), "uvProbe");
            Tr(q != null, "（前提 · A）探针 quad 建出来了");
            var uv0 = MeshUv(q);
            Tr(uv0 != null && uv0.Length == 4, "（前提 · A）探针 quad 的 mesh 有 4 个 uv（拿不到 ⇒ 后面每一条都是空的）");
            if (q == null || uv0 == null || uv0.Length != 4) return;

            // ---- 基线：整张图（期望值写死 = `ImageQuad.RebuildMesh` 的顶点序口径）----
            q.SetUvRect(new Rect(0f, 0f, 1f, 1f));
            var b = MeshUv(q);
            Tr(b != null && b.Length == 4, "（前提 · A）改 uv 之后 mesh 仍是 4 个 uv");
            if (b == null || b.Length != 4) return;
            C(b[0], new Vector2(0f, 0f), "基线 `Rect(0,0,1,1)` · 顶点 0（左下）uv = (0, 0)");
            C(b[1], new Vector2(1f, 0f), "基线 … 顶点 1（右下）uv = (1, 0)");
            C(b[2], new Vector2(1f, 1f), "基线 … 顶点 2（右上）uv = (1, 1)");
            C(b[3], new Vector2(0f, 1f), "基线 … 顶点 3（左上）uv = (0, 1)");

            // ---- 负高：上下翻 ----
            q.SetUvRect(new Rect(0f, 1f, 1f, -1f));
            var f = MeshUv(q);
            Tr(f != null && f.Length == 4, "（前提 · A）负高态 mesh 仍是 4 个 uv");
            if (f == null || f.Length != 4) return;
            C(f[0], new Vector2(0f, 1f), "★ 负高 `Rect(0,1,1,-1)` · **左下**顶点 uv = (0, **1**)（取图的上沿 ⇒ 上下翻）");
            C(f[1], new Vector2(1f, 1f), "★ 负高 … 右下 uv = (1, **1**)");
            C(f[2], new Vector2(1f, 0f), "★ 负高 … 右上 uv = (1, **0**)");
            C(f[3], new Vector2(0f, 0f), "★ 负高 … 左上 uv = (0, **0**)");
            C(q.UvRect, new Rect(0f, 1f, 1f, -1f), "★ 负高态 `ImageQuad.UvRect` 真值 = `(0, 1, 1, -1)`（高 = **−1**）");
            // 🧨 灭自证：两种写法的 uv[0].y 必须不同（基线 0 / 负高 1）
            Tr(!Mathf.Approximately(b[0].y, f[0].y),
               $"🧨 灭自证：基线与负高的**左下顶点 uv.y 必须不同**（实测 {b[0].y:F2} vs {f[0].y:F2}）"
             + "｜把 `SetUvRect` 的负高写成正高（或让 `RebuildMesh` 忽略负号）⇒ 上一条与本条一起红");
            // 🧨 再补一条：**只改 uv，不改几何**（顶点必须逐值相同）—— 证明翻转不是靠“把 quad 倒过来”
            var mfA = q.GetComponent<MeshFilter>();
            var verts = mfA != null && mfA.sharedMesh != null ? mfA.sharedMesh.vertices : null;
            Tr(verts != null && verts.Length == 4, "（前提 · A）负高态 mesh 有 4 个顶点");
            if (verts != null && verts.Length == 4)
                Tr(verts[0].y < 0f && verts[3].y > 0f,
                   $"🧨 负高**没有**倒过来摆顶点（左下 y={verts[0].y:F4} < 0、左上 y={verts[3].y:F4} > 0）"
                 + " ⇒ 翻转是在 uv 上做的，几何仍是正立的");

            // ---- 负宽：水平镜像（工程里已有 5 处先例，这里一并钉住，免得「负高对了、负宽坏了」没人发现）----
            q.SetUvRect(new Rect(1f, 0f, -1f, 1f));
            var hz = MeshUv(q);
            Tr(hz != null && hz.Length == 4, "（前提 · A）负宽态 mesh 仍是 4 个 uv");
            if (hz != null && hz.Length == 4)
            {
                C(hz[0], new Vector2(1f, 0f), "负宽 `Rect(1,0,-1,1)` · 左下顶点 uv = (**1**, 0)（水平镜像，5 处先例同一招）");
                C(hz[1], new Vector2(0f, 0f), "负宽 … 右下 uv = (**0**, 0)");
                C(q.UvRect.width, -1f, "负宽态 `UvRect.width` = **−1**（⛔ 与「负高」互斥：负高时 width 仍是 +1）");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ============================================================ B/C 段：两件静态件（活窗）

    static void RunCounterAndArrows()
    {
        var mgr = WindowsManager.EnsureHost();
        Tr(mgr != null, "（前提 · BC）`WindowsManager.EnsureHost()` 拿到管理器（拿不到 ⇒ B/C 段全部无意义）");
        if (mgr == null) return;
        ShopWindow shop = null;
        try
        {
            shop = ShopWindow.Create(mgr);
            Tr(shop != null, "（前提 · BC）`ShopWindow.Create` 建出窗");
            if (shop == null) return;
            mgr.OpenWindow(shop);
            Tr(shop.tabButtons != null, "（前提 · BC）窗的页签条在（否则切不了页）");
            if (shop.tabButtons == null) return;

            // ---------------- B：`Counter` chip（第 1 页 = 原版 `Card Shop Tab`）----------------
            //   ⚠️ **不点页签**：`BuildPages()` 里 `foreach (var t in tabs) t.Setup();` ⇒ 五页的树在 `OpenWindow` 时
            //   就**全都建好**了（`Shell/ShopWindow.cs:249-260`）⇒ 按 `PageOf` 直读，⛔ 别用 `Click` 制造副作用。
            MenuCheck.Section(_sink, "B116_W4 · B：`Counter` chip（`background/Counter`，`UIFlippable` H=0 / V=1）");
            var pg0 = shop.PageOf(0);
            Tr(pg0 != null, "（前提 · B）商店第 1 页（`Card Shop Tab`）拿到");
            if (pg0 != null)
            {
                var cell = pg0.transform.Find("Packs Scroll View/Viewport/Content/CatalogItemShopContainer_0");
                Tr(cell != null, "（前提 · B）第 1 格 `CatalogItemShopContainer_0` 在");
                var counter = cell != null ? cell.Find("Counter") : null;
                Tr(counter != null, "★ B320-①：格里有 **`Counter`** 节点（原版 `…/background/Counter`）—— 节点级闭合的那一条");
                var q = counter != null ? counter.GetComponent<ImageQuad>() : null;
                Tr(q != null, "（前提 · B）`Counter` 上挂的是 `ImageQuad`（拿不到 ⇒ 下面几条尺寸/uv 都是空的）");
                if (q != null)
                {
                    C(q.Texture != null ? q.Texture.name : "<null>", "40K_main_deck_card_counter",
                      "★ `Counter` 的底图 = `40K_main_deck_card_counter`（原版 `m_Sprite` guid `98160e4e…`）");
                    // 框 337.6 × 32.5419（prefab 锚点算出）；`m_PreserveAspect = 1` ⇒ **高定、宽按贴图比例**（116×36）
                    N(q.WorldH * 108f, 32.5419f, 0.30f, "★ `Counter` 画心**高** = 32.5419（prefab `0.06822209 × background 477`）");
                    N(q.WorldW * 108f, 104.857f, 0.60f, "★ `Counter` 画心**宽** = 104.857（= 32.5419 × 116/36；`m_PreserveAspect = 1` ⇒ 不是拉满 337.6）");
                    // --- 这一条是 `B-320` 的正文：**垂直翻** ---
                    var uv = q.UvRect;
                    C(uv.height, -1f, "★★ `Counter` 的 uv **高 = −1**（`UIFlippable` `m_Veritical: 1` ⇒ **上下翻**）");
                    N(uv.y, 1f, 1e-4f, "★★ …且 uv 起点 `v = 1`（负高必须从 1 起，写 0 就是「翻到图外」）");
                    C(uv.width, 1f, "★★ …且 **宽 = +1**（🧨 若把它写成**水平镜像** `Rect(1,0,-1,1)`，这里会是 −1 ⇒ 红）");
                    var muv = MeshUv(q);
                    Tr(muv != null && muv.Length == 4, "（前提 · B）`Counter` 的 mesh 有 4 个 uv");
                    if (muv != null && muv.Length == 4)
                        Tr(muv[0].y > 0.99f && muv[3].y < 0.01f,
                           $"★★ …而且**真翻到网格上了**（左下 uv.y={muv[0].y:F2} ≈ 1 · 左上 uv.y={muv[3].y:F2} ≈ 0）"
                         + "｜只改字段不改网格 ⇒ 本条红");
                }
            }

            // ---------------- C：`Arrow Left 1` ×2（第 5 页 = 原版 `Packs Tab` / `Generic Shop Tab`）----------------
            MenuCheck.Section(_sink, "B116_W4 · C：`Packs Scroll View/Scrollbar Horizontal` + `Arrow Left 1`×2（`UIFlippable` H=1）");
            var pg4 = shop.PageOf(ShopTabPage.CratesPage);
            Tr(pg4 != null, "（前提 · C）第 5 页（Crates ⇒ 原版 `BoosterShopTab` ⇔ prefab `Packs Tab`）拿到");
            if (pg4 != null)
            {
                var sb = pg4.transform.Find("Packs Scroll View/Scrollbar Horizontal");
                Tr(sb != null, "★ B320-②：`Packs Scroll View/Scrollbar Horizontal` 节点在（原版 `m_IsActive: 0` 的那颗壳）");
                if (sb != null)
                {
                    Tr(sb.parent != null && sb.parent.name == "Packs Scroll View",
                       "★ …父节点 = `Packs Scroll View`（原版父链）");
                    Tr(!sb.gameObject.activeSelf,
                       "★ …而且**关着**（原版 `m_IsActive: 0`；`ScrollRect` 的 AutoHide 运行时还会再关一次 ⇒ 恒不显示）");
                    Tr(sb.childCount >= 5, "★ …它下面至少挂了 5 颗子件（`B116_W15`/`B-325` 补齐后 = `Sliding Area` + 右箭头×2 + 左箭头×2）");
                }
                var a1 = pg4.transform.Find("Packs Scroll View/Scrollbar Horizontal/Arrow Left 1");
                var a2 = pg4.transform.Find("Packs Scroll View/Scrollbar Horizontal/Arrow Left 1 2");
                Tr(a1 != null, "★ B320-③：`Arrow Left 1` 节点在");
                Tr(a2 != null, "★ B320-④：`Arrow Left 1 2` 节点在（原版第二颗，`pos.x = 10`）");
                CheckArrow(a1, "Arrow Left 1");
                CheckArrow(a2, "Arrow Left 1 2");
                RunB325(pg4);
            }
        }
        finally
        {
            if (shop != null) Object.DestroyImmediate(shop.gameObject);
        }
    }

    /// <summary>一颗 `Arrow Left 1*` 的「尺寸 + uv 符号 + 贴图」。</summary>
    static void CheckArrow(Transform t, string what)
    {
        var q = t != null ? t.GetComponent<ImageQuad>() : null;
        Tr(q != null, $"（前提 · C）`{what}` 上挂的是 `ImageQuad`（拿不到 ⇒ 下面几条是空的）");
        if (q == null) return;
        C(q.Texture != null ? q.Texture.name : "<null>", "40k_general_bt_arrow",
          $"★ `{what}` 的底图 = `40k_general_bt_arrow`（原版 `m_Sprite` guid `35e30868…`）");
        // 原版 `sizeDelta 36.57007 × 36.692` · `m_PreserveAspect = 0` ⇒ 拉伸到框（画心 = 框）
        N(q.WorldW * 108f, 36.57007f, 0.30f, $"★ `{what}` 宽 = 36.57007（prefab `m_SizeDelta.x`）");
        N(q.WorldH * 108f, 36.692f, 0.30f, $"★ `{what}` 高 = 36.692（prefab `m_SizeDelta.y`；`m_PreserveAspect = 0` ⇒ 画心 = 框）");
        var uv = q.UvRect;
        C(uv.width, -1f, $"★★ `{what}` 的 uv **宽 = −1**（`UIFlippable` `m_Horizontal: 1` ⇒ 水平镜像）");
        N(uv.x, 1f, 1e-4f, $"★★ …且 uv 起点 `u = 1`（负宽必须从 1 起）");
        C(uv.height, 1f, $"★★ …且 **高 = +1**（🧨 写成垂直翻 `Rect(0,1,1,-1)` ⇒ 这里会是 −1 ⇒ 红：两颗的 `UIFlippable` 是 **H=1 / V=0**）");
        var muv = MeshUv(q);
        Tr(muv != null && muv.Length == 4, $"（前提 · C）`{what}` 的 mesh 有 4 个 uv");
        if (muv != null && muv.Length == 4)
            Tr(muv[0].x > 0.99f && muv[1].x < 0.01f,
               $"★★ …而且**真镜像到网格上了**（左下 uv.x={muv[0].x:F2} ≈ 1 · 右下 uv.x={muv[1].x:F2} ≈ 0）");
    }

    // ============================================================ D 段（`B116_W15` · 账 `B-325`）
    //  `Scrollbar Horizontal` 子树**剩下的三颗**：`Sliding Area` / `Handle` / `Arrow Right 1` / `Arrow Right 2`。
    //
    //  判据（2026-11-02 本代理现读 `D:/2/tools/assetripper_out/ExportedProject/Assets/Packs Tab.prefab`；
    //  `Generic Shop Tab.prefab` 与之**逐字节同构**，`diff` 只差 `m_Name` 与页脚本 ⇒ 两族同值）：
    //   · `Sliding Area`（`RT &224302127217294661`）锚点 `(0,0.5)–(1,0.5)` · `pos (0.006164551, 0)` ·
    //     `sizeDelta (0.2, 21.6125)` · `pivot (.5,.5)` ⇒ 宽 `1080.114 + 0.2 = 1080.314`、高 `21.6125`；
    //     **没有 Image**（只有 RectTransform）。
    //   · `Handle`（`RT &224969106005120503`）四向拉伸 `sizeDelta (0,0)` ⇒ **与 `Sliding Area` 同矩形**；
    //     `Image` 米白 `(0.90588236, 0.83137256, 0.63529414, 1)` · sprite `GenericRoundedSquare` · `m_Type 1`(Sliced)。
    //   · `Arrow Right 1`（`RT &224711914853252429`）锚点 `(1,0.5)` · `pivot (0,.5)` · `pos (-0.00012207031, 0)`
    //     · `sizeDelta 36.570007 × 36.692`；`Arrow Right 2`（`RT &224875609422105747`）只差 `pos.x = -9.400024`。
    //   · **四件的 `m_IsActive` 都是 1**（关的是父件 `Scrollbar Horizontal` 的 `m_IsActive: 0`）
    //     ⇒ 所以 D 段断的是「自己 active / 整棵不 active」两条，⛔ 别把子件也断成 `activeSelf == false`。
    //
    //  灭自证（🧨）：① 子件**次序**逐位比名字（把 `Sliding Area` 建到箭头后面 ⇒ 红）；
    //  ② 每件读回**父节点名**（挂错父件 ⇒ 红）；③ 尺寸断的是 `rect.rect × 108`（拉伸锚点下把
    //  `sizeDelta` 当设计尺寸 ⇒ 红）；④ 给右箭头套了 `ArrowFlipH` ⇒ `UvRect.width` 那条红。

    /// <summary>一颗节点的**设计尺寸（px）** —— 两档读法，按节点类型分（⛔ 别一律读 `RectTransform`）。
    /// <para>① **容器节点**（`MainMenuSubmenuWindow.Node` 建的）：有 `RectTransform` ⇒ 量 `rect.rect × 108`。
    /// ⚠️ 拉伸锚点（`anchorMin ≠ anchorMax`）下 `sizeDelta` 是「相对父框的内缩量」、**不是**设计尺寸 ⇒ 必须走 `rect.rect`。</para>
    /// <para>② **图节点**（`MenuDraw.Rect` 建的）：`ImageQuad` 是**裸 `Transform` + `MeshFilter`/`MeshRenderer`**
    /// （`ImageQuad.Create` 只 `AddComponent` 了 `ImageQuad`，**没有 `RectTransform`**）⇒ `rect.rect` 根本读不到，
    /// 量的是 `ImageQuad.WorldW/WorldH × 108`（= `_worldH × _aspect`，由 `MenuDraw.Rect` 的
    /// `Create(..., LayoutSpace.Px(y2−y1), ...)` + `SetAspect((x2−x1)/(y2−y1))` 定出来的，逐值等于设计 px）。</para>
    /// <para>两档都读不到 ⇒ 返回负值（下一句的尺寸断言当场红，⛔ 不是静默通过）。</para></summary>
    static Vector2 SizePx(Transform t, out string how)
    {
        how = "<读不到>";
        if (t == null) return new Vector2(-1f, -1f);
        var rt = t as RectTransform;
        if (rt != null)
        {
            var r = rt.rect;
            how = "rect.rect";
            return new Vector2(r.width * 108f, r.height * 108f);
        }
        var q = t.GetComponent<ImageQuad>();
        if (q != null)
        {
            how = "ImageQuad.WorldW/H";
            return new Vector2(q.WorldW * 108f, q.WorldH * 108f);
        }
        // 🔴 🆕 2026-11-02（`B116_W25` · `B-330`）：**九宫格的根**既不是 `RectTransform`、自己也不是
        //    `ImageQuad`（`MenuDraw.Nine` 建的是裸 `Transform` 根 + 9 个子 quad）⇒ 前两档都读不到。
        //    第三档 = **子 quad 的包围盒**（九块无论怎么切都铺满目标矩形，所以包围盒逐值 = 设计尺寸）。
        //    ⛔ 别把它写成「读不到 ⇒ 跳过」：那样 `Handle` 换成九宫格之后尺寸断言会**静默失效**。
        var kids = t.GetComponentsInChildren<ImageQuad>(true);
        if (kids != null && kids.Length > 0)
        {
            float x0 = float.MaxValue, x1 = float.MinValue, y0 = float.MaxValue, y1 = float.MinValue;
            int n = 0;
            foreach (var k in kids)
            {
                if (k == null) continue;
                float cx = k.transform.localPosition.x, cy = k.transform.localPosition.y;
                float hw = k.WorldW * 0.5f, hh = k.WorldH * 0.5f;
                x0 = Mathf.Min(x0, cx - hw); x1 = Mathf.Max(x1, cx + hw);
                y0 = Mathf.Min(y0, cy - hh); y1 = Mathf.Max(y1, cy + hh);
                n++;
            }
            if (n > 0)
            {
                how = "九宫格子块包围盒";
                return new Vector2((x1 - x0) * 108f, (y1 - y0) * 108f);
            }
        }
        return new Vector2(-1f, -1f);
    }

    /// <summary>一颗静态子件的「在不在 / 挂谁下面 / 设计尺寸 / 开关态」。</summary>
    static void CheckSbChild(Transform sb, string path, string name, string parentName,
                             float wPx, float hPx, string judge)
    {
        var t = sb != null ? sb.Find(path) : null;
        Tr(t != null, $"★ D·`{name}` 节点在（{judge}）");
        if (t == null) return;
        var pn = t.parent != null ? t.parent.name : "<null>";
        Tr(pn == parentName,
           $"★ D·`{name}` 的**父节点名 = `{parentName}`**（读回 `{pn}`）"
         + "｜🧨 挂到别的父件下（例如直接挂 `Packs Scroll View`）⇒ 本条红");
        string how;
        var sz = SizePx(t, out how);
        N(sz.x, wPx, 0.10f,
          $"★ D·`{name}` **设计宽 = {wPx}**（读法 `{how} × 108`；🧨 拉伸件拿 `sizeDelta` 当设计尺寸 ⇒ 红）");
        N(sz.y, hPx, 0.10f, $"★ D·`{name}` **设计高 = {hPx}**（读法 `{how} × 108`）");
        Tr(t.gameObject.activeSelf,
           $"★ D·`{name}` 自己 **`m_IsActive: 1`**（prefab 实读 —— ⛔ 不是 0：关掉的是父件 `Scrollbar Horizontal`）");
        Tr(!t.gameObject.activeInHierarchy,
           $"★ D·`{name}` 挂在**整棵关着**的 `Scrollbar Horizontal` 下 ⇒ `activeInHierarchy = false`（原版 AutoHide 恒不显示）");
    }

    static void RunB325(ShopTabPage pg4)
    {
        MenuCheck.Section(_sink, "B116_W15 · B-325：`Scrollbar Horizontal` 剩下的三颗（`Sliding Area` / `Handle` / `Arrow Right 1` / `Arrow Right 2`）");
        var root = pg4 != null ? pg4.transform : null;
        Tr(root != null, "（前提 · D）第 5 页的根在（拿不到 ⇒ D 段整段无意义）");
        if (root == null) return;
        var sb = root.Find("Packs Scroll View/Scrollbar Horizontal");
        Tr(sb != null, "（前提 · D）`Packs Scroll View/Scrollbar Horizontal` 在（拿不到 ⇒ 下面每条都是空的）");
        if (sb == null) return;

        // ---- ⓪ 子件个数与次序（原版 `RT &224313026266067498` 的 `m_Children` 有序 5 项）----
        // 🔴 🆕 2026-11-02（`B116_W25` · `B-330`）：期望值从 **5 → 6**。第 6 颗 `Background` 是**我们的**节点，
        //   代表原版挂在 `Scrollbar Horizontal` **自身**上的那颗 `Image`（`&114632457200066912`）——
        //   原版是父件的组件、本工程的图像只能落在节点上 ⇒ 多一颗。它**排在最后**（`index 5`），
        //   ⛔ 前五颗的次序判据一个字没放宽（原版 `m_Children` 仍逐位比）。
        C(sb.childCount, 6, "★ D-⓪ `Scrollbar Horizontal` 子件数 = **6**（原版 `m_Children` 5 项 + 我们那颗 `Background`）");
        int nOrig = Mathf.Min(5, sb.childCount);
        var got = new string[nOrig];
        for (int i = 0; i < nOrig; i++) got[i] = sb.GetChild(i).name;
        string[] want = { "Sliding Area", "Arrow Right 1", "Arrow Right 2", "Arrow Left 1", "Arrow Left 1 2" };
        C(string.Join(" | ", got), string.Join(" | ", want),
          "★ D-① **前五颗**次序逐位对上原版 `m_Children`（🧨 把 `Sliding Area` 建到箭头后面 ⇒ 本条红）");
        C(sb.childCount > 5 ? sb.GetChild(5).name : "<缺>", "Background",
          "★ D-①b 第 6 颗（`index 5`）= 我们补的 `Background`（原版父件自己的红底 `Image`）——"
        + "🧨 把它挪到 `index 0` ⇒ 本条与 D-① 同时红（前五颗次序被顶掉）");

        // ---- ① Sliding Area（只有 RectTransform，无 Image）----
        CheckSbChild(sb, "Sliding Area", "Sliding Area", "Scrollbar Horizontal",
                     1080.31f, 21.61f,
                     "原版 `RT &224302127217294661`：宽 `1080.114 + sizeDelta.x 0.2` · 高 `sizeDelta.y 21.6125`");

        // ---- ② Handle（四向拉伸填满 Sliding Area；米白那块）----
        CheckSbChild(sb, "Sliding Area/Handle", "Handle", "Sliding Area",
                     1080.31f, 21.61f,
                     "原版 `RT &224969106005120503`：四向拉伸 + `sizeDelta (0,0)` ⇒ 与 `Sliding Area` 同矩形");
        // 🔴 🆕 2026-11-02（`B116_W25` · `B-330`）：`Handle` 已从「同色纯色块」换成**真九宫格**
        //    （`BuildHandle` 走 `_win.Nine`）⇒ 它自己不再是 `ImageQuad`，是「裸 `Transform` 根 + 9 个子 quad」。
        //    所以下面按**子件**除 uv / tint / 底图名（⛔ 别再写 `GetComponent<ImageQuad>()`：恒 null ⇒ 整段静默跳过）。
        var hq = sb.Find("Sliding Area/Handle");
        var hKids = hq != null ? hq.GetComponentsInChildren<ImageQuad>(true) : null;
        Tr(hKids != null && hKids.Length > 0,
           "（前提 · D）`Handle` 底下有九宫格子件（拿不到 ⇒ 下面三条都是空的；⛔ 不是「跳过即通过」）");
        if (hKids != null && hKids.Length > 0)
        {
            // 🧨 灭自证（两条都硬）：① **先断 `Resources.Load` 非空**（把图改名/挪走 ⇒ 这条立刻红，
            //    而不是「退回纯色块也算过」）；② 再断真取到的那张**名字**。
            var resTex = Resources.Load<Texture2D>("Art/ui_menu/GenericRoundedSquare");
            Tr(resTex != null,
               "★ D·`Resources/Art/ui_menu/GenericRoundedSquare.png` **真存在且能 Load**"
             + "（🧨 改名 / 挪走 / 没导入 ⇒ 本条红 —— 这条挡的是「退回纯色块也算过」）");
            C(resTex != null ? resTex.name : "<null>", "GenericRoundedSquare",
              "★ D·`Resources.Load` 取到的资产**名 = `GenericRoundedSquare`**（不是同名的别的东西）");
            int okName = 0, okTint = 0, okUv = 0;
            foreach (var k in hKids)
            {
                if (k.Texture != null && k.Texture.name == "GenericRoundedSquare") okName++;
                if (k.Tint.r == 0.90588236f && k.Tint.g == 0.83137256f && k.Tint.b == 0.63529414f
                    && k.Tint.a == 1f) okTint++;
                if (k.UvRect.width > 0f && k.UvRect.height > 0f) okUv++;
            }
            C(okName, hKids.Length,
              "★ D·`Handle` 九宫格的**每一块**底图都是 `GenericRoundedSquare`（原版 `m_Sprite` guid `d185926f…`）");
            C(okTint, hKids.Length,
              "★ D·`Handle` 的 tint = 原版 `Image.m_Color` 米白 `(0.90588236, 0.83137256, 0.63529414, 1)`（逐块核）");
            C(okUv, hKids.Length,
              "★ D·`Handle` **没有翻转**（每块的 uv 宽高都 > 0；原版这颗没挂 `UIFlippable`）");
            C(hKids.Length, 9,
              "★ D·`Handle` 的九宫格 = **9 块**（`m_Type 1` Sliced · `m_FillCenter 1` · `m_Border 9`；"
            + "🧨 退回 `CardArt.Solid()` 单块 ⇒ 本条与上面三条一起红）");
        }

        // ---- ③ 两颗右箭头（`Simple` · `PreserveAspect 0` · **无 `UIFlippable`** ⇒ 不镜像）----
        CheckSbChild(sb, "Arrow Right 1", "Arrow Right 1", "Scrollbar Horizontal",
                     36.57f, 36.69f,
                     "原版 `RT &224711914853252429`：`sizeDelta 36.570007 × 36.692` · `pivot (0,.5)` ⇒ 向右伸出父右沿");
        CheckSbChild(sb, "Arrow Right 2", "Arrow Right 2", "Scrollbar Horizontal",
                     36.57f, 36.69f,
                     "原版 `RT &224875609422105747`：与 `Arrow Right 1` 只差 `pos.x = -9.400024`");
        foreach (var nm in new[] { "Arrow Right 1", "Arrow Right 2" })
        {
            var t = sb.Find(nm);
            var q = t != null ? t.GetComponent<ImageQuad>() : null;
            Tr(q != null, $"（前提 · D）`{nm}` 上挂的是 `ImageQuad`");
            if (q == null) continue;
            C(q.Texture != null ? q.Texture.name : "<null>", "40k_general_bt_arrow",
              $"★ D·`{nm}` 的底图 = `40k_general_bt_arrow`（原版 `m_Sprite` guid `35e30868…`，与左箭头同一张）");
            C(q.UvRect.width, 1f,
              $"★★ D·`{nm}` 的 uv **没镜像**（`宽 = +1`，原版**没有** `UIFlippable`；"
            + "🧨 顺手给它套 `ArrowFlipH` ⇒ 宽变 −1 ⇒ 本条红）");
            Tr(Mathf.Approximately(q.UvRect.height, 1f),
               $"★ D·`{nm}` 的 uv **高 = +1**（没上下翻 —— 右箭头连 `m_Veritical` 都没有）");
        }

        // ---- ④ 🆕 `B116_W25`（账 `B-330`）：`Scrollbar Horizontal` **自身**那颗红底 ----
        //   判据（`Packs Tab.prefab` 现读）：GO `&1724804207875695` 上的 `Image &114632457200066912` ——
        //   `m_Sprite` = `GenericRoundedSquare` · `m_Color (0.5471698, 0.11594599, 0.080010675, 1)` ·
        //   `m_Type 1`(Sliced) · **`m_PixelsPerUnitMultiplier: 2`**（角块 4.5px，不是 9）。
        MenuCheck.Section(_sink, "B116_W25 · B-330：`Scrollbar Horizontal` 自身的红底（`GenericRoundedSquare`）");
        var bg = sb.Find("Background");
        Tr(bg != null, "★ E·红底节点 `Background` 在（原版挂在 `Scrollbar Horizontal` **自身**上的 Image）");
        if (bg != null)
        {
            string how2;
            var sz2 = SizePx(bg, out how2);
            N(sz2.x, 1080.11f, 0.20f,
              $"★ E·红底**设计宽 = 1080.114**（= 父件 `ScrollbarH` 的宽；读法 `{how2} × 108`）");
            N(sz2.y, 5.47f, 0.20f,
              $"★ E·红底**设计高 = 5.469**（同上；读法 `{how2} × 108`）");
            var bKids = bg.GetComponentsInChildren<ImageQuad>(true);
            Tr(bKids != null && bKids.Length > 0,
               "（前提 · E）红底底下有九宫格子件（拿不到 ⇒ 下面三条都是空的）");
            if (bKids != null && bKids.Length > 0)
            {
                int nOk = 0, tOk = 0;
                foreach (var k in bKids)
                {
                    if (k.Texture != null && k.Texture.name == "GenericRoundedSquare") nOk++;
                    if (k.Tint.r == 0.5471698f && k.Tint.g == 0.11594599f && k.Tint.b == 0.080010675f
                        && k.Tint.a == 1f) tOk++;
                }
                C(nOk, bKids.Length,
                  "★ E·红底每一块的底图都是 `GenericRoundedSquare`（原版 `m_Sprite` guid `d185926f…`）");
                C(tOk, bKids.Length,
                  "★ E·红底的三值 = 原版 `Image.m_Color (0.5471698, 0.11594599, 0.080010675)`（逐块核）");
                // 竖向被挤：4.5 + 4.5 = 9 > 5.469 ⇒ uGUI `GetAdjustedBorders` 压扁角块、中段归零
                //   ⇒ 只出 6 块（上 3 + 下 3）。⛔ 别断言 9 块（那是 `Handle` 的档）。
                Tr(bKids.Length <= 9,
                  $"★ E·红底块数 ≤ 9（实测 {bKids.Length}；竖向角被压扁时中行归零 ⇒ 正常是 6 块）");
            }
        }
    }
}
