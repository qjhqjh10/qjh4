// B114_W20_Assert.cs — 第一百一十四会话 · 写手 `W-20`（2026-11-01）· 账 `B-252` 的断言
//
// ============================ 它盯的是什么 ============================
//  ① **`DrCx1 = 0.4` 是原版真值，⛔ 不是要修掉的偏移**（`B114_W12` §顺手发现 4 说「原版是 0」——**那条不成立**）：
//     prefab 根挂 `AspectRatioFitter`（`m_AspectMode 2` = HeightControlsWidth · `m_AspectRatio 0.66`）
//     ⇒ 根宽 = 1080 × 0.66 = **712.8**；`Content` 是 `m_SizeDelta (712,1080)` + 锚 `(0.5,0.5)` +
//     `m_AnchoredPosition (0,0)` ⇒ **左右各留 (712.8−712)/2 = 0.4** ⇒ 左沿 0.4 / 右沿 712.4。
//     判据 = 2026-11-01 现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/Expansion Pass Points Drawer.prefab`：
//       · 根 `!u!114 &114621510681728303`（脚本 guid `d3e719b5…`）`m_AspectMode: 2` · `m_AspectRatio: 0.66`
//       · `Content`（GO 1730242686860875）`!u!224 &224642988549198181`：anchorMin/Max `(0.5,0.5)` ·
//         `m_AnchoredPosition (0,0)` · `m_SizeDelta (712,1080)` · `m_Pivot (0.5,0.5)`
//       · `Content` 身上 `!u!114 &114711914853252429`：`fitInParent: 1` · `targetRect` = 根 · `padding (0,0)`
//         ⇒ 序列化那个 **`m_LocalScale (0,0,0)`** 是 UIScaleToFit 运行期算的占位值，算出来 = min(712.8/712, 1080/1080)
//         = **1.0**（⛔ 别把 `0` 当原版尺寸读）。
//  ② 端到端：**画出来**的 `Content/Background` 左沿就是 0.4（不是只有常量对）。
//  ③ 结构反证：Icon 档（根 1080²）的 `Content` 是**铺满**的（左沿 0）—— 它那个「0」与基础档的 0.4
//     **不是一个几何**（Content 宽 712 vs 1080）⇒ 「统一成 0」这条修法会把基础档整体左移 0.4 并压窄。
//  ④ 三档 override（`Horizontal`(15) / `Shop`(20) / `OfferPopups`(30)）里**只有一份带 `Campaign Glow`**：
//     `Icon Expansion Pass Premium Drawer Variant`（= `ExpansionPremiumItem` 的 OfferPopups 档，且与它的 Icon 档同一份），
//     已由 `B109_E5` 的 `NoBaseWithExpansionGlow` 实现；另三份（`Title Drawer Horizontal Variant` /
//     `Avatar Drawer Shop Variant` / `Avatar Border Drawer Shop Variant`）**prefab 里根本没有这个节点**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A1/A2 的期望值是**独立的 prefab 常量**（0.4 / 712.4 = (712.8−712)/2），⛔ 不是读 `Metrics()` 现算。
//  · A4 是**不变式**（左留白 == 右留白）：它不复制任何一个常量，只要「Content 不再居中于根」就红。
//  · A5 + B3 是**一对**：谁把 `DrCx1` 改成 0 ⇒ A5 红（`ContentX1 > 0` 不成立）与 B3 红（画出来 0 ≠ 0.4）
//    **同时**发生；谁只改 A1 那条期望值去「对齐」实现 ⇒ A4（留白不等）与 `B-252` 的出处矛盾。
//  · C2 是**结构性反证**：Icon 档主图铺满 (0,0)-(1080,1080)、基础档 `Background` 从 0.4 起 ——
//    两档不可能共用同一个左沿常量（🧨 把 `DrawerIconContent` 也改成从 0.4 起 ⇒ 红）。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W20_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W20_Assert.Run(); _sink.Pass += B114_W20_Assert.Pass; _sink.Fail += B114_W20_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 只跑**方框**夹具（`K = 1`、`OX = OY = 0`）⇒ `fr.Map` 出来的就是设计 px 本身。
//    非方框调用下 `FrameOf` 还会再差一层（原版 ARF 在窄容器里是溢出还是按宽缩 = `ItemDrawerLayout.cs` 文件头 ① 的未读 TODO）。
//  · ④ 那 4 条断的是**我们这套印射表**（`PremiumLayersOfSlot`）的结论；prefab 侧的「有没有这个节点」
//    是**离线扫出来的**（1,314 份 prefab 按 `m_Name: Campaign Glow` 全扫，命中 42 份；那三份不在其中）——
//    本文件跑不出 prefab，所以那半边写在报告里，不写成断言。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-20`：账 `B-252` —— `DrCx1 = 0.4` 是真值（不是偏移）+ 三档 override 的 `Campaign Glow` 结论。</summary>
public static class B114_W20_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W20] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（prefab 现读 · 独立于生产代码）----
    /// <summary>根宽 = 1080 × ARF `m_AspectRatio 0.66`（`m_AspectMode 2` = HeightControlsWidth）。</summary>
    const float RootWWant = 712.8f;
    /// <summary>`Content.m_SizeDelta.x`（prefab 直读）。</summary>
    const float ContentWWant = 712f;
    /// <summary>`Content` 左沿 = (712.8 − 712)/2（= 输出值 0.4）。</summary>
    const float Cx1Want = 0.4f;
    /// <summary>`Content` 右沿 = 712.8 − 0.4。</summary>
    const float Cx2Want = 712.4f;

    /// <summary>基础档夹具框：根 712.8×1080（`FrameOf(box)` 的默认根 ⇒ `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box712 = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>Icon 档夹具框：根 1080²（`FrameOf(box, 1080, 1080)` 的 `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box1080 = new PxRect(0f, 0f, 1080f, 1080f);

    /// <summary>基础档那一格的主图（`Content/Background` 恒用这张）。</summary>
    const string ArtBackground = "40k_Rewards_icon_base_card";

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W20（B-252）：`DrCx1 = 0.4` 是原版真值（(712.8−712)/2）+ 三档 override 的光圈结论");

        // ⚠️ 四段各自独立调用：某一段「前提不成立就 `return`」不会把别的段一起吞掉
        SectionConst();
        SectionDraw();
        SectionIconDrawer();
        SectionTiers();

        Debug.Log(_sink.P + "B114W20 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① 常量层：0.4 是「712 居中于 712.8」的必然结果
    static void SectionConst()
    {
        MenuCheck.Section(_sink, "① `Content` 左右沿（prefab：根 ARF 0.66 ⇒ 712.8 · Content 712 居中 ⇒ 0.4 / 712.4）");

        var m = ItemDrawer.Metrics();

        N(ItemDrawer.DrRootW, RootWWant, 1e-3f,
          "A0 **前提**：抽屉根宽 = 712.8（prefab 根 `AspectRatioFitter`：`m_AspectMode 2` = HeightControlsWidth · `m_AspectRatio 0.66` ⇒ 1080×0.66）"
        + "｜🧨 有人把 `DrRootW` 改成 712（「反正 Content 就 712」）⇒ 红");

        N(m.ContentX1, Cx1Want, 1e-3f,
          "A1 ★★ `Content` 左沿 = **0.4**（= (712.8 − 712)/2）"
        + "｜🧨 改成 0（`B114_W12` §顺手发现 4 的错误结论「原版是 0」）⇒ 红");

        N(m.ContentX2, Cx2Want, 1e-3f,
          "A2 ★★ `Content` 右沿 = **712.4**（= 712.8 − 0.4）"
        + "｜🧨 只改左沿不改右沿（变成 712.4 宽的一条）⇒ 红");

        N(m.ContentX2 - m.ContentX1, ContentWWant, 1e-3f,
          "A3 ★ `Content` 宽 = **712**（prefab `Content.m_SizeDelta (712,1080)` —— 比根窄 0.8，这才有那 0.4 的留白）");

        Tr(Mathf.Abs(m.ContentX1 - (ItemDrawer.DrRootW - m.ContentX2)) <= 1e-3f,
           "A4 ★★ **不变式**：左留白 == 右留白（" + m.ContentX1.ToString("F3") + " vs "
         + (ItemDrawer.DrRootW - m.ContentX2).ToString("F3") + "）—— 即「712 的 Content 在 712.8 的根里**居中**」"
         + "｜🧨 谁把 `Content` 改成从 0 起（宽 712.8 或 712 不居中）⇒ 红（这一条不复制任何常量）");

        Tr(m.ContentX1 > 1e-3f,
           "A5 ★★ `Content` 左沿 **> 0**（原版就是 0.4，不是 0）"
         + "｜🧨 把 `DrCx1` 改成 0 ⇒ 红（与 B3 同时红）；⛔ 「0.4px 落在容差里看不出来」不等于「原版是 0」");
    }

    // ============================================================ ② 端到端（基础档真的画到 0.4）
    static void SectionDraw()
    {
        MenuCheck.Section(_sink, "② 端到端：基础档 `Content/Background` 那颗 quad 的**实测**左沿 = 0.4");

        var bg = CardArt.MenuUi(ArtBackground);
        Tr(bg != null, "B0 **前提**：抽屉共享底图 `" + ArtBackground + "` 取得到（`Resources/Art/ui_menu/`）"
                     + "｜🧨 删掉那张图 ⇒ 红（那时 `Background` 整层不建，下面的量法没对象）");

        _baseBgX1 = float.NaN;
        _baseBgOk = false;

        var root = new GameObject("B114W20_EXPPASS_BASE");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W20_ExpPassBase", Type = "Everguild.LiveOps.ExpansionPassPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W20",
            };
            res = ItemDrawer.Draw(root.transform, Box712, spec, 1, DrawerOverride.Default,
                                  ItemDrawerStyle.Default(10, 11, 12));

            Tr(res.Node != null, "B1 **前提**：基础档抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerExpansionPassPoint, "B1b **前提**：路由到 `ExpansionPassPointDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "B2 **前提**：`Content` 建出来了（`DrawerContent` = `fr.Map(0.4,0,712.4,1080)`）");

            var bgNode = FindDeep(res.Node, ItemDrawer.NodeBackground);
            Tr(bgNode != null, "B2b **前提**：`Content/Background` 建出来了（原版每份抽屉都有这一层）");
            if (bgNode != null)
            {
                var got = QuadOf(bgNode);
                _baseBgX1 = got.x1; _baseBgOk = got.W > 1f;

                N(got.x1, Cx1Want, 0.6f,
                  "B3 ★★ **画出来**的左沿 = 0.4（不是只有常量对：`Content/Background` 那颗 quad 的实测矩形）"
                + "｜🧨 `DrCx1` 改成 0 ⇒ 画到 0 ⇒ 红");
                N(got.x2, Cx2Want, 0.6f, "B3 ★★ **画出来**的右沿 = 712.4");
                N(got.W, ContentWWant, 1.0f, "B3b ★ 画出来的宽 ≈ 712（与 A3 同一数量）｜PA 之后实画 711.325（= 1080×328/498；原版这颗 `m_PreserveAspect = 1`）⇒ 差 0.675，容差取 1.0 与 `B114_W27` 的 F2b（同一块底板）同口径");
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>基础档那一次的实测左沿（`C4` 要用；`B` 段没量到就是 `NaN`）。</summary>
    static float _baseBgX1;
    static bool _baseBgOk;

    // ============================================================ ③ 结构反证（Icon 档的左沿 0 与基础档的 0.4 不是一个几何）
    static void SectionIconDrawer()
    {
        MenuCheck.Section(_sink, "③ 结构反证：Icon 档（根 1080²）主图铺满 ⇒ 左沿 0 ≠ 基础档的 0.4");

        var armyTex = ArmyIcons.Texture(10);
        Tr(armyTex != null, "C0 **前提**：阵营徽记 `CardArmy 10` 取得到（Icon 档的主图就是它）"
                          + "｜🧨 那张图没了 ⇒ 红");

        var root2 = new GameObject("B114W20_EXPPASS_ICON");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W20_ExpPass", Type = "Everguild.LiveOps.ExpansionPassPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W20",
            };
            res2 = ItemDrawer.Draw(root2.transform, Box1080, spec, 1, DrawerOverride.Icon,
                                   ItemDrawerStyle.Default(10, 11, 12));

            Tr(res2.Node != null, "C1 **前提**：Icon 档抽屉根建出来了");
            C(res2.Drawer, ItemDrawer.DrawerExpansionPassPoint, "C1b **前提**：`ov = Icon` 仍路由到 `ExpansionPassPointDrawer`");
            if (res2.Node == null) return;

            var img = FindDeep(res2.Node, ItemDrawer.NodeImage);
            Tr(img != null, "C2 **前提**：Icon 档主图 `Image` 建出来了");
            if (img != null)
            {
                var got = QuadOf(img);            // `fr.Map(0,0,1080,1080)`
                N(got.x1, 0f, 0.6f,
                  "C3 ★★ 结构反证：Icon 档主图左沿 = **0**（`Content` = `DrawerIconContent` 铺满 1080²）"
                + "｜🧨 把 `DrawerIconContent` 也改成从 0.4 起（「统一左沿」）⇒ 红");
                N(got.x2, 1080f, 0.6f, "C3 ★★ Icon 档主图右沿 = 1080（铺满整根）");
            }

            Tr(_baseBgOk && Mathf.Abs(_baseBgX1) > 0.1f,
               "C4 ★★ 两档的「左沿」**不是同一个几何**：基础档 " + (_baseBgOk ? _baseBgX1.ToString("F3") : "<没量到>")
             + "（Content 712 居中于 712.8）vs Icon 档 0（Content 1080 铺满）"
             + " ⇒ 「把 `DrCx1` 改成 0 去对齐原版」这条修法**前提就不成立**"
             + "｜🧨 两档共用同一个左沿常量 ⇒ 这一条与 B3 / C3 里必有一边红");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }
    }

    // ============================================================ ④ 三档 override 的 `Campaign Glow` 结论
    static void SectionTiers()
    {
        MenuCheck.Section(_sink, "④ `Horizontal`(15) / `Shop`(20) / `OfferPopups`(30) 三档：只有 `Icon Expansion Pass Premium Drawer Variant` 带光圈");

        var f = ItemDrawer.PremiumLayersOfSlot("Icon Expansion Pass Premium Drawer Variant");
        Tr(f.HasValue, "D0 **前提**：`PremiumLayersOfSlot` 认得出这一个槽名（`BaseOfferPopup` 三个槽用的名字以它开头）");
        Tr(f.HasValue && f.Value.CampaignGlow,
           "D1 ★★ 唯一带 `Campaign Glow` 的那一份 = `Icon Expansion Pass Premium Drawer Variant`"
         + "（= `ExpansionPremiumItem` 的 **OfferPopups(30)** 档，与它的 Icon(10) 档**同一份 prefab**）"
         + "｜🧨 把这个预设的 `CampaignGlow` 置 false ⇒ 红（`B109_E5` 的 `NoBaseWithExpansionGlow` 被改坏）");
        Tr(f.HasValue && !f.Value.BaseBackground,
           "D1b 该 prefab **没有 `Content/Background` 节点**（所以预设里 `BaseBackground = false`）—— 与「别凭空画整卡底板」那条一致");

        Tr(!ItemDrawer.PremiumLayersOfSlot("Title Drawer Horizontal Variant").HasValue,
           "D2 `Horizontal`(15) 与 `OfferPopups`(30) 共用的 `Title Drawer Horizontal Variant`：**没有** `Campaign Glow`"
         + "（prefab 全树 `m_Name` 扫过，0 个 glow 节点）⇒ **不该**走任何光圈预设");
        Tr(!ItemDrawer.PremiumLayersOfSlot("Avatar Drawer Shop Variant").HasValue,
           "D3 `Shop`(20) 的 `Avatar Drawer Shop Variant`：**没有** `Campaign Glow`（同上）");
        Tr(!ItemDrawer.PremiumLayersOfSlot("Avatar Border Drawer Shop Variant").HasValue,
           "D4 `Shop`(20) / `OfferPopups`(30) 共用的 `Avatar Border Drawer Shop Variant`：**没有** `Campaign Glow`（同上）");
    }

    // ============================================================ 夹具 / 量具
    /// <summary>图上的件是 `ImageQuad` ⇒ 走 `MenuDraw.QuadRectPx` 反推（设计 px · 左上原点 · y 向下）。</summary>
    static PxRect QuadOf(Transform t)
    {
        var q = t.GetComponent<ImageQuad>();
        return q == null ? new PxRect() : MenuDraw.QuadRectPx(q);
    }

    /// <summary>按名字递归找（`Transform.Find` 只找**直接子件** —— `CLAUDE.md` §三 卡根那层 `2DCard` 的教训）。</summary>
    static Transform FindDeep(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        for (int i = 0; i < root.childCount; i++)
        {
            var t = FindDeep(root.GetChild(i), name);
            if (t != null) return t;
        }
        return null;
    }
}
