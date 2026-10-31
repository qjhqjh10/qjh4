// B109_E5_PremiumDrawer_Assert.cs — 第一百零九会话 · 执行代理 `B109_E5`
//
// ============================ 它盯的是什么 ============================
//  判据正本 = `资料/完全复刻/判据/B109_R4_ExpansionPassPremium.md`（`B109_R4` 逐字回源：prefab 直读 + 3 个实例复核）。
//  本件把那 4 处差异落成断言，**不重新查判据**。
//
//    A1 ★ `Icon Expansion Pass Premium Drawer Variant` 那档**不画 `Background`**（原版该 prefab 没有这个节点、
//         `ItemDrawerComponents.background` = {fileID: 0}）
//    B1 ★ 同一档**要画 `Campaign Glow`**（prefab 自带静态层），且在 `Content` 之下、与 `Image` 同框
//    B2 ★ 它的 sprite = `40K_general_icon_Expansion_Pass XP`（prefab `Campaign Glow.m_Sprite` guid 6c77cbd6…）
//         —— **读的是渲染器上真贴图的名字**，⛔ 不读我们自己的常量
//    B3 ★ 它的矩形 = 设计 px `(0,216)-(712,648)`（aMin(0,0.4) aMax(1,0.8) sd≈0），PA=1 ⇒ 画出来 432×432 居中
//    C1 ★ `BaseOfferPopup` 表里 `:473` 那格（`…Variant Variant`）**乘了实例的 `localScale 1.4`**
//    C2 ★（灭自证配对）另两格同槽名 `:565/:573` **scale = 1** ⇒ 整体乘 1.4 不可能同时满足 C1 与 C2
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  A1 把 `NoBaseWithExpansionGlow.BaseBackground` 改回 `true` ⇒ A1 红（`Content/Background` 又出现了）。
//  B1 把 `CampaignGlow` 改回 `false` ⇒ B1 红（那条横带压根不建）。
//  B2 把 `GlowSprite` 换回 `PremiumGlowSprite`（Campaign points big）⇒ B2 红 —— 因为 B2 读的是
//     `ImageQuad.Texture.name`（渲染器上那张贴图），不是 `lay.GlowSprite` 这个字段本身。
//  B3 把横带矩形抄成 `Expansion Pass Premium Drawer` 那组（−114.66/146.46/826.66/717.54）⇒ B3 红。
//  C1 把 `:473` 的 R 改回 ×1 那组（653.86,550.15)-(936.14,977.85) ⇒ C1 红。
//  C2 **与 C1 是一对**：任何「全局把抽屉表乘 1.4」的改法都会让 C2 红（另两格 scale = 1，见 §1 实例表）。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  纯算式 + 一段自包含的抽屉夹具 ⇒ 任一宿主的聚合行、`//` 之前同一物理行追加即可（⚠️ 先看那一行第一个 `//`）：
//      B109_E5_PremiumDrawer_Assert.Run(); pass += B109_E5_PremiumDrawer_Assert.Pass; fail += B109_E5_PremiumDrawer_Assert.Fail;
//  建议落点：`Editor/ShellScene.cs`（抽屉全部消费点在外壳侧；`B108R6_Assert.Run()` 也建议落那里）。
//
// ============================ 边界（如实记）============================
//  · B3 断的是**画出来的那块**（`MenuDraw.QuadRectPx`）。原版 `PA=1` + 256² 贴图 ⇒ 在 712×432 的框里
//    收到 432×432 居中 ⇒ 宽度这一项**不**等于 712（那是 uGUI 的行为，我们照它做）。所以只断中心 + 高度。
//  · C1/C2 断的是**表里的矩形**；实例的 `localScale 1.4` 我们是把它**折进 `R`**（`MenuDraw.Node` 不写 scale），
//    ⇒ 表值与 prefab 的 `m_SizeDelta × m_LocalScale` 必须一致，见 C1 的算式。
//  · ⚠️ **本件依赖 `Resources/Art/ui_menu/40K_general_icon_Expansion_Pass_XP.png` 已导入**
//    （`工具/import_original_art.py` 的 `MENU_IMAGES`）。没导入 ⇒ B1/B2/B3 的**前提**会先红并点名。
//  · 本文件只建临时 GameObject / 只读常量，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E5`：`Icon Expansion Pass Premium Drawer Variant` 的两层（A1/B1/B2/B3/C1/C2）。</summary>
public static class B109_E5_PremiumDrawer_Assert
{
    static readonly CheckSink _sink = new CheckSink("[E5] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>判据里的槽名（`BaseOfferPopup` 的实例名 —— `:473` 那颗多一个 `Variant`）。</summary>
    const string SlotNameSmall = "Icon Expansion Pass Premium Drawer Variant Variant";
    const string SlotNameBig = "Icon Expansion Pass Premium Drawer Variant";

    // ============================================================ A · 两层开关（判据表那一跳）
    static void SectionA_Layers()
    {
        MenuCheck.Section(_sink, "A · `Icon Expansion Pass Premium Drawer Variant` 的两层开关（A1）");

        var lay = ItemDrawer.PremiumLayersOfSlot(SlotNameBig);
        Tr(lay.HasValue, "A0 **前提**：`PremiumLayersOfSlot(\"" + SlotNameBig + "\")` 认得出这一档"
                       + "｜🧨 分支被删 / 前缀改成 `==` 比较 ⇒ 这里就红");
        if (lay.HasValue)
        {
            Tr(!lay.Value.BaseBackground,
               "A1 ★ 该档**不画 `Background`**（原版 prefab 里没有这个节点、`background` 槽 = {fileID: 0}）"
             + "｜🧨 改回 `true` ⇒ 红");
            Tr(lay.Value.CampaignGlow,
               "A1-b ★ 该档**要画** `Campaign Glow`（prefab 自带静态层，`ExpansionPassPremiumDrawer.Draw` 不碰它）"
             + "｜🧨 改回 `false` ⇒ 红");
            N(lay.Value.GlowX1, 0.4f, 0.01f, "A1-c 横带左沿 = 0.4（prefab 0 / 我们 712.8 基准的等价位）");
            N(lay.Value.GlowY1, 216f, 0.01f, "A1-d 横带上沿 = 216（aMax.y 0.8 ⇒ (1−0.8)×1080）");
            N(lay.Value.GlowX2, 712.4f, 0.01f, "A1-e 横带右沿 = 712.4");
            N(lay.Value.GlowY2, 648f, 0.01f, "A1-f 横带下沿 = 648（aMin.y 0.4 ⇒ (1−0.4)×1080）");
        }

        // 实例名多一个 `Variant`（`:473` 那颗）必须**同样**认得出来。
        var lay2 = ItemDrawer.PremiumLayersOfSlot(SlotNameSmall);
        Tr(lay2.HasValue && lay2.Value.CampaignGlow && !lay2.Value.BaseBackground,
           "A2 ★ `\"…Variant Variant\"`（`:473` 那颗实例名多一个 `Variant`）走**同一档**"
         + "｜🧨 用 `==` 而不是 `StartsWith` ⇒ 红");

        // 姊妹档不能被这一条吃掉。
        var wide = ItemDrawer.PremiumLayersOfSlot("Expansion Pass Premium Drawer");
        Tr(wide.HasValue && wide.Value.BaseBackground && wide.Value.CampaignGlow,
           "A3 姊妹档 `Expansion Pass Premium Drawer` 仍是「有底板 + 有横带」");
        if (wide.HasValue)
            C(wide.Value.GlowSprite, "40k_campaign_Expansion_Pass",
              "A3-b ★ 它的横带 sprite = `40k_campaign_Expansion Pass`（该 prefab `Campaign Glow.m_Sprite` guid d2dc932d…）"
            + "｜🧨 使它共用 `PremiumGlowSprite`（Campaign points big）⇒ 红");
    }

    // ============================================================ B · 真建一格抽屉，读渲染器
    static void SectionB_BuiltDrawer()
    {
        MenuCheck.Section(_sink, "B · 真建一格 `ExpansionPassPremiumDrawer`（B1/B2/B3）");

        var root = new GameObject("B109E5_DRAWER_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;                       // 与 `BaseOfferPopup.BuildDrawers` 同口径
            st = ItemDrawer.WithPremiumLayers(st, SlotNameBig);

            var spec = new ItemSpec
            {
                Id = "B109E5_ExpansionPremium",
                Type = "ExpansionPremiumItem",                        // 原版类型键 ⇒ `PickDrawer` 落到这一档
                Kind = ItemKind.Generic,
                Label = "B109E5",
                Art = null,                                           // 主图不参与本项（原版出厂 `m_Sprite` 就是 null）
            };
            // 框取 712.8×1080 ⇒ `FrameOf` 的 K = 1 ⇒ 设计 px 与「我们算出来的数」直接可比。
            res = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, 712.8f, 1080f), spec, 1,
                                  DrawerOverride.OfferPopups, st);

            Tr(res.Node != null, "B0-前提①：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerExpansionPremium,
              "B0-前提②：`PickDrawer` 真的落到 `ExpansionPassPremiumDrawer`（`ItemTypeSets` 那张真表那一跳）");

            Transform content = res.Node != null ? FindDeep(res.Node, ItemDrawer.NodeContent) : null;
            Tr(content != null, "B0-前提③：`Content` 那一层建出来了");

            // A1 的**结构侧**：这一档的 `Content` 下不许有 `Background`（⛔ 不是读我们自己的 bool —— 是看树上有没有这颗）
            Tr(content != null && FindDeep(content, ItemDrawer.NodeBackground) == null,
               "B1 ★ 建出来的 `Content` 下**没有** `Background` 节点（= 原版 prefab 的槽位实读）"
             + "｜🧨 `BaseBackground` 改回 `true` ⇒ 这里立刻多出一颗 ⇒ 红");

            Transform glow = content != null ? FindDeep(content, ItemDrawer.NodeCampaignGlow) : null;
            Tr(glow != null,
               "B2-前提 ★ `Content/Campaign Glow` 建出来了"
             + "｜🧨 开关改回 `false` ⇒ 红；⚠️ 若这里红且日志里有 `premium|glow|…` ⇒ "
             + "`Resources/Art/ui_menu/40K_general_icon_Expansion_Pass_XP.png` 还没导入");

            var q = glow != null ? glow.GetComponent<ImageQuad>() : null;
            Tr(q != null, "B2-前提②：`Campaign Glow` 节点上就是 `ImageQuad`");

            if (q != null)
            {
                // 🔴 B2 的**灭自证**在这里：读的是**渲染器上那张贴图**的名字，不是 `lay.GlowSprite` 字段。
                var tex = q.Texture;
                Tr(tex != null, "B2-前提③：`Campaign Glow` 的 `ImageQuad` 上有真贴图（不是空壳）");
                if (tex != null)
                    C(tex.name, "40K_general_icon_Expansion_Pass_XP",
                      "B2 ★ 横带真贴图 = `40K_general_icon_Expansion Pass XP`（空格→下划线后的落盘名）"
                    + "｜🧨 换回 `PremiumGlowSprite`（Campaign points big）⇒ 红");

                var r = MenuDraw.QuadRectPx(q);
                // 框 (0,216)-(712,648) ⇒ 中心 (356.4, 432)；PA=1 + 256² 贴图 ⇒ 画出来 432×432。
                N(r.CY, 432f, 1f,
                  "B3 ★ 横带**垂直中心** = 432（`(216+648)/2` 的设计 px；`FrameOf` K=1 时与表值同空间）"
                + "｜🧨 抄成 `Expansion Pass Premium Drawer` 那组（146.46/717.54 ⇒ 中心 432 但高 571）⇒ B3-b 红");
                N(r.H, 432f, 1f,
                  "B3-b ★ 横带画出来的高度 = 432（`0.4 × 1080`）"
                + "｜🧨 抄成姊妹档那组 sizeDelta（229.31/139.08 ⇒ 571.08 高）⇒ 红");
                N(r.CX, 356.4f, 1f, "B3-c 横带水平中心 = 356.4（Content 中心）");
                N(r.W, 432f, 1f,
                  "B3-d ★ 横带画出来的宽度 = 432（`PA=1` + 256² 贴图在 712×432 框里内接 —— 原版 uGUI 的同一结果）"
                + "｜🧨 `keepAspect` 传成 `false` ⇒ 变成 712 ⇒ 红");
            }
        }
        finally
        {
            // 抽屉句柄表是**静态**的（`ItemDrawer._handles`）⇒ 只撤自己那一格（同 `B108_R6_Assert` D2）。
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ C · 表里的槽矩形（只 :473 带 ×1.4）
    static void SectionC_SlotRects()
    {
        MenuCheck.Section(_sink, "C · `BaseOfferPopup` 抽屉表：`:473` 那格的 `localScale 1.4`（C1/C2）");

        // prefab 实读（`General Basic Offer Popup Variant Expansion pass.prefab`）：
        //   窗口 RT = 1128.5542 × 663.2962（= 我们 `Geo.Window` 小档，中心 (960,520)）
        //   抽屉实例 aMin(0,0) aMax(1,1) · aPos (−165,−244) · sizeDelta (0, −235.5968) · **m_LocalScale 1.4**
        //   ⇒ ARF(2, 0.66) 把宽压成 高×0.66，再整体 ×1.4、中心不动。
        const float winW = 1128.5542f, winH = 663.2962f;
        const float apX = -165f, apY = -244f, sdY = -235.5968f, scale = 1.4f, arf = 0.66f;
        float h = (winH + sdY) * scale;             // 427.6994 × 1.4 = 598.7792
        float w = h * arf;                          // 395.1942
        float cx = 960f + apX;                      // 795
        float cy = 520f - apY;                      // 764（Unity y 向上 ⇒ 设计 px 里减）

        float ex1 = cx - w * 0.5f, ex2 = cx + w * 0.5f;
        float ey1 = cy - h * 0.5f, ey2 = cy + h * 0.5f;

        Tr(GetSlotRect("General Basic Offer Popup Variant Expansion pass", SlotNameSmall,
                       out float gx1, out float gy1, out float gx2, out float gy2),
           "C0 **前提**：表里找得到 `Expansion pass` 那份的 `" + SlotNameSmall + "`");

        N(gx1, ex1, 0.1f, "C1 ★ `:473` 槽左沿 = " + ex1.ToString("F2") + "（= ARF 后 282.28 × 1.4，中心 795）"
                        + "｜🧨 改回 ×1 那组（653.86…）⇒ 红");
        N(gy1, ey1, 0.1f, "C1 槽上沿 = " + ey1.ToString("F2") + "（中心 764）｜🧨 同上 ⇒ 红");
        N(gx2, ex2, 0.1f, "C1 槽右沿 = " + ex2.ToString("F2"));
        N(gy2, ey2, 0.1f, "C1 槽下沿 = " + ey2.ToString("F2"));
        N(gx2 - gx1, w, 0.1f, "C1 宽 = " + w.ToString("F2") + "（= 高 × 0.66，ARF 之后）");
        N(gy2 - gy1, h, 0.1f, "C1 高 = " + h.ToString("F2") + "（= 427.6994 × **1.4**）");

        // ---- C2：灭自证的配对。另两格**同槽名、scale = 1** ⇒ 任何「整体 ×1.4」都不可能同时满足 ----
        // `Premium_Resource.prefab`：aPos (−530,−226.31) · sd (0, 271.56) · scale 1
        float h2 = 720.84f + 271.56f;               // GeoBig 窗口高 + sizeDelta
        float w2 = h2 * arf;
        Tr(GetSlotRect("General Basic Offer Popup Variant Premium_Resource", SlotNameBig,
                       out float px1, out float py1, out float px2, out float py2),
           "C2-前提：表里找得到 `Premium_Resource` 那份的 `" + SlotNameBig + "`");
        N(py2 - py1, h2, 0.1f,
          "C2 ★ 大档那格高 = " + h2.ToString("F2") + "（**scale = 1**，没乘 1.4）"
        + "｜🧨 全局把抽屉表 ×1.4 ⇒ 这条红 —— 它与 C1 是一对，不可能同时满足");
        N(px1, (960f - 530f) - w2 * 0.5f, 0.1f, "C2 大档那格左沿（中心 430）");
        N(py1, (520f + 226.31f) - h2 * 0.5f, 0.1f, "C2 大档那格上沿（中心 746.31）");
        N(px2, (960f - 530f) + w2 * 0.5f, 0.1f, "C2 大档那格右沿");
        N(py2, (520f + 226.31f) + h2 * 0.5f, 0.1f, "C2 大档那格下沿");
    }

    /// <summary>按（宿主 popup 名, 槽名）在 `BaseOfferPopup.Variants` 里取那一格的矩形；找不到 ⇒ `false`。</summary>
    static bool GetSlotRect(string prefab, string slot, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var vs = BaseOfferPopup.Variants;
        if (vs == null) return false;
        for (int i = 0; i < vs.Length; i++)
        {
            if (vs[i].Prefab != prefab || vs[i].Drawers == null) continue;
            for (int j = 0; j < vs[i].Drawers.Length; j++)
            {
                if (vs[i].Drawers[j].Name != slot) continue;
                var r = vs[i].Drawers[j].R;
                x1 = r.x1; y1 = r.y1; x2 = r.x2; y2 = r.y2;
                return true;
            }
        }
        return false;
    }

    /// <summary>按名字递归找（`Transform.Find` 只找**直接子件**）。</summary>
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

    /// <summary>自包含：只要一个空场景就能跑（夹具自己建临时 GameObject）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "B109_E5 · `Icon Expansion Pass Premium Drawer Variant` 的两层"
                                + "（判据 → `判据/B109_R4_ExpansionPassPremium.md`）");
        try { SectionA_Layers(); }
        catch (System.Exception e) { Tr(false, "「A 两层开关」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_BuiltDrawer(); }
        catch (System.Exception e) { Tr(false, "「B 真建抽屉」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_SlotRects(); }
        catch (System.Exception e) { Tr(false, "「C 槽矩形 ×1.4」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B109E5 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
