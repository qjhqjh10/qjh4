// B108_R6_Assert.cs — 第一百零八会话 · 波 1 · 执行代理 `B108_A15`
//
// ============================ 它盯的是什么 ============================
//  判据正本 = `资料/完全复刻/判据/B108_AUD01_余项.md` §四（`B108_R6` 逐字回源）。本件按它那七条落成断言，
//  **不重查** ①②③（头像过扫 / 卡背过扫+旋转 / 徽章 DoMaterialRefresh —— 那三条 R6 已复核「已对」）。
//
//    A1 ★ `Avatar Drawer` 的 `image` 槽 = `avatar`（860.21×730.48 过扫），⛔ 不是 `avatarBG`（431.19×479）
//    A2 ★ `Avatar Border Drawer` 的 `image` 槽 = `avatarBG` —— 两档**方向相反**（同一张系数表两处不同）
//    B1 ★ `Cardback Drawer` 的 `Image` = 1.15 倍居中等比过扫（818.8×1242，`Content` 712×1080）
//    C1 ★ `NodeCampaignGlow` 的槽名 + **本轮新落的两层开关**（`PremiumLayersOfSlot` 的 4 条分支）
//    C2 ★ `CardDrawerScale(1)` = 3.5028（原版 `UIScaleToFit` 算式；守卫「`DrCardPadMul` 被改回 1.0」）
//    D1 ★ 徽章档位下标 `TierIndexOf(0) == 0`、`TierFrameName(0) == "Alliance_Trophies_TIER1"`
//    D2 ★ `qty = 0`（未达档）的徽章本体真的置灰（`ImageQuad.Grey == 1`，且**必须 `>= 0`**）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  A1 改回 `CosmeticAvatarFrame`（431.19 宽）⇒ A1 红，A2 仍绿。
//  A2 把两档一起改成同一个框 ⇒ A1 或 A2 必有一条红（两档尺寸不同，不可能同时满足）。
//  B1 改回整格 `fr.Map(0,0,712,1080)` ⇒ B1 红。
//  C1 把 `PremiumLayersOfSlot` 的分支改回「一律 `Original`」⇒ C1-b/c/d 红；
//     把槽名常量改回 `"Background"` ⇒ C1-a 红。
//  C2 把 `ItemDrawerCard.cs` 的 `DrCardPadMul` 从 `1.35` 改成 `1.0` ⇒ C2 红（3.5028 → 2.5946）。
//  D1 把 `Max(0, tier−1)` 改成 `Max(1, tier)` ⇒ D1 红。
//  D2 删掉 `ItemDrawerAlliance.cs` 里那句 `if (qty <= 0) badge.SetGrey(1f);` ⇒ 得 0 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动断言宿主）============================
//  纯算式 + 一段自包含的抽屉夹具 ⇒ 任一宿主的聚合行、`//` 之前同一物理行追加即可（⚠️ 先看那一行第一个 `//`）：
//      B108R6_Assert.Run(); pass += B108R6_Assert.Pass; fail += B108R6_Assert.Fail;
//  建议落点：`Editor/ShellScene.cs`（抽屉全部消费点都在外壳侧；`B101R8_Assert.Run()` 也建议落那里）。
//
// ============================ 边界（如实记）============================
//  · D2 只验「`_GreyScale` 这条通道的值」；**观感**（去饱和 vs 变暗）归真机验收（铁律 12 暂停期）。
//  · C1 只验**开关的取值**；横带的**画面位置**（`fr.Map` 之后）要真机看 —— 几何判据见 R6 §二·2 的注：raw layout dump
//    里这份 prefab 的 `Content` 是 159.44×147.41（ARF 组件 `m_Enabled=0`），与 R6 按 712.8×1080 推的横带**不是同一套空间**，
//    本代理按 R6 给的数落（调度台可复核，见报告 §没查清）。
//  · 本文件只建临时 GameObject / 只读常量，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B108_A15`：AUD01 余项七条（A1/A2/B1/C1/C2/D1/D2）。</summary>
public static class B108R6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[R6] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>矩形四边逐条比（一条不过不影响另外三条跑）。</summary>
    static void Rect(PxRect got, float x1, float y1, float x2, float y2, float tol, string msg)
    {
        N(got.x1, x1, tol, msg + " — x1");
        N(got.y1, y1, tol, msg + " — y1");
        N(got.x2, x2, tol, msg + " — x2");
        N(got.y2, y2, tol, msg + " — y2");
    }

    static void SectionA_Frames()
    {
        MenuCheck.Section(_sink, "A · `avatar` / `avatarBG` 两档（A1 / A2）");

        // 前提：读口本身是通的（返回有限值）；K=1 ⇒ OX=OY=0 ⇒ 直接就是设计 px
        var a1 = ItemDrawer.AvatarImageFrameForTest(1f);
        Tr(float.IsFinite(a1.W) && float.IsFinite(a1.H) && a1.W > 1f,
           "A0 **前提**：`AvatarImageFrameForTest(1)` 读得出有限矩形（= " + a1 + "）");

        // `Avatar Drawer.prefab`：`ItemDrawerComponents.image` → `avatar`；
        // `avatar` RT anchors (−0.475,−0.175)-(1.52,1.35) + sizeDelta≈0，`avatarBG` = (139.81,233.28)-(571.00,712.28)
        // ⇒ 设计 px (−65.0,65.6)-(795.2,796.1) = 860.21×730.48（比 `avatarBG` 宽 1.995× / 高 1.525×）。
        Rect(a1, -65.0f, 65.6f, 795.2f, 796.1f, 0.1f,
             "A1 ★ `Avatar Drawer` 的 `image` = 过扫的 `avatar`（−65.0,65.6)-(795.2,796.1)"
           + "｜🧨 改回 `CosmeticAvatarFrame`（431.19 宽）⇒ 红");

        var a2 = ItemDrawer.CosmeticAvatarFrameForTest(1f);
        Tr(float.IsFinite(a2.W) && float.IsFinite(a2.H) && a2.W > 1f,
           "A2-前提：`CosmeticAvatarFrameForTest(1)` 读得出有限矩形（= " + a2 + "）");
        Rect(a2, 139.8f, 233.3f, 571.0f, 712.3f, 0.1f,
             "A2 ★ `Avatar Border Drawer` 的 `image` = `avatarBG` 那一格（139.8,233.3)-(571.0,712.3）"
           + "｜🧨 两档一起改 ⇒ 与 A1 不可能同时满足（尺寸不同）");
    }

    static void SectionB_Cardback()
    {
        MenuCheck.Section(_sink, "B · 卡背过扫（B1）");

        var b1 = ItemDrawer.CardbackOverdrawFrameForTest(1f);
        Tr(float.IsFinite(b1.W) && float.IsFinite(b1.H) && b1.W > 1f,
           "B1-前提：`CardbackOverdrawFrameForTest(1)` 读得出有限矩形（= " + b1 + "）");
        // anchors `-0.075..1.075` 的 `Image`，相对 `Content` 712×1080 ⇒ 818.8×1242（1.15 倍居中过扫）
        Rect(b1, -53.4f, -81.0f, 765.4f, 1161.0f, 0.1f,
             "B1 ★ `Cardback Drawer` 的 `Image` = 1.15 倍居中等比过扫（−53.4,−81.0)-(765.4,1161.0）"
           + "｜🧨 改回整格 `Map(0,0,712,1080)` ⇒ 红");
    }

    static void SectionC_PremiumAndScale()
    {
        MenuCheck.Section(_sink, "C · `Campaign Glow` 槽名 + 两层开关（C1）· 卡牌缩放（C2）");

        C(ItemDrawer.NodeCampaignGlow, "Campaign Glow",
          "C1-a ★ `PremiumIconDrawer` 的 `background` 槽名 = `Campaign Glow`（`Icon VIP …` 的 PPtr 实读）"
        + "｜🧨 改回 `\"Background\"` ⇒ 红");

        // 🆕 本批落的真缺口：`Icon Premium Campaign Drawer Variant`（根 ARF 0.66）**没有** `Background` 节点，
        //    只有一条 `Campaign Glow` 横带；而 `Premium Drawer` / `Expansion Pass …` 两层都有、横带几何不同。
        var iconCampaign = ItemDrawer.PremiumLayersOfSlot("Icon Premium Campaign Drawer Variant");
        Tr(iconCampaign.HasValue, "C1-b-前提：`PremiumLayersOfSlot(\"Icon Premium Campaign Drawer Variant\")` 认得出这一档");
        if (iconCampaign.HasValue)
        {
            Tr(!iconCampaign.Value.BaseBackground,
               "C1-b ★ `Icon Premium Campaign Drawer Variant` **不画** `Background`（原版那份 prefab 里没有这个节点）"
             + "｜🧨 把分支改回一律 `Original` ⇒ 红");
            Tr(iconCampaign.Value.CampaignGlow,
               "C1-c ★ 同一档要画 `Campaign Glow` 静态横带（prefab 自带层，`PremiumDrawer.Draw` 不碰它）");
            N(iconCampaign.Value.GlowX1, -31.26f, 0.01f, "C1-d 横带左沿 = −31.26（设计 px · R6 §二·2）");
            N(iconCampaign.Value.GlowX2, 744.06f, 0.01f, "C1-d 横带右沿 = 744.06");
            N(iconCampaign.Value.GlowY1, 197.05f, 0.01f, "C1-d 横带上沿 = 197.05");
            N(iconCampaign.Value.GlowY2, 666.95f, 0.01f, "C1-d 横带下沿 = 666.95");
        }

        var premium = ItemDrawer.PremiumLayersOfSlot("Premium Drawer");
        Tr(premium.HasValue && premium.Value.BaseBackground && premium.Value.CampaignGlow,
           "C1-e ★ `Premium Drawer` = 有 `Background` + 有横带（同一组 31.26/197.05/744.06/666.95）");

        var noGlow = ItemDrawer.PremiumLayersOfSlot("Premium Drawer No Glow");
        Tr(noGlow.HasValue && noGlow.Value.BaseBackground && !noGlow.Value.CampaignGlow,
           "C1-f ★ `Premium Drawer No Glow` = 有 `Background`、**没有**横带（⛔ 它整串以 `Premium Drawer` 开头，分支顺序不能反）");

        var expansion = ItemDrawer.PremiumLayersOfSlot("Expansion Pass Premium Drawer");
        Tr(expansion.HasValue && expansion.Value.BaseBackground && expansion.Value.CampaignGlow,
           "C1-g ★ `Expansion Pass Premium Drawer` = 两层都有");
        if (expansion.HasValue)
            N(expansion.Value.GlowX1, -114.66f, 0.01f,
              "C1-h ★ `Expansion Pass Premium Drawer` 横带左沿 = −114.66（sizeDelta 那一组与上面**不同**）");

        Tr(!ItemDrawer.PremiumLayersOfSlot("Card Drawer").HasValue,
           "C1-i 不认识的槽名 ⇒ `null`（= 保持旧形状，⛔ 别默认改成别的档）");

        // C2：`UIScaleToFit` 的 `1.35 × Content.w(712.8) / CardUI.w(2.5437) / 108`
        // ⚠️ 与 `B101R8_Assert.A1` 是**同一条算式**（那边是它的原生落点）；这里保留一条，
        //    是为了「抽屉七条」自成一套、不依赖另一个文件还在不在。
        N(ItemDrawer.CardDrawerScale(1f), 3.5028f, 0.0005f,
          "C2 ★ 整格 `CardDrawerScale(1)` = 3.5028（1.35×712.8/2.5437/108）"
        + "｜🧨 把 `DrCardPadMul` 改回 1.0 ⇒ 2.5946 ⇒ 红");
    }

    static void SectionD_Alliance()
    {
        MenuCheck.Section(_sink, "D · 联盟徽章档位 / 置灰（D1 / D2）");

        C(AllianceTrophyArt.TierIndexOf(0), 0,
          "D1-a ★ `TierIndexOf(0) == 0`（原版 `tierFrames[Math.Max(0, tier−1)]` 的夹法）"
        + "｜🧨 改成 `Max(1, tier)` ⇒ 红");
        C(AllianceTrophyArt.TierFrameName(0), "Alliance_Trophies_TIER1",
          "D1-b ★ `TierFrameName(0) == \"Alliance_Trophies_TIER1\"`（未达档回落第一档）");

        // D2：真建一格徽章抽屉（`AllianceBadgeDrawer`），读 `Trophy Icon` 上那条 `_GreyScale` 通道。
        var root = new GameObject("B108R6_BADGE_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;          // 文字层不参与本项（也免了 TMP 依赖）
            var spec = new ItemSpec
            {
                Id = "B108R6_Badge",
                BadgeId = AllianceBadgeArt.DefaultBadgeId,
                Type = "AllianceTrophyData",             // 原版类型键 ⇒ `PickDrawer` 落到 `AllianceBadgeDrawer`
                Kind = ItemKind.Generic,
                Label = "B108R6",
            };
            res = ItemDrawer.Draw(root.transform, new PxRect(0f, 0f, 200f, 300f), spec, 0,
                                  DrawerOverride.Default, st);
            Tr(res.Node != null, "D2-前提①：徽章抽屉的根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAllianceBadge,
              "D2-前提②：`PickDrawer` 真的落到 `AllianceBadgeDrawer`（`ItemTypeSets` 那张真表那一跳）");

            Transform icon = res.Node != null ? FindDeep(res.Node, ItemDrawer.NodeTrophyIcon) : null;
            Tr(icon != null, "D2-前提③：`Trophy Icon` 那一层建出来了（= 原版 `GetComponentsInChildren` 唯一命中的那颗）");
            var iq = icon != null ? icon.GetComponent<ImageQuad>() : null;
            Tr(iq != null, "D2-前提④：`Trophy Icon` 节点上就是 `ImageQuad`（`AllianceBadgeArt.Draw` 的落地件）");
            if (iq != null)
            {
                float g = iq.Grey;
                Tr(g >= 0f,
                   "D2-a **通道检查**：`Grey >= 0`（−1 = `_GreyScale` 这条通道压根没接上 —— ⛔ 别让它混进「通过」）");
                N(g, 1f, 1e-4f,
                  "D2 ★ `qty = 0`（未达档）⇒ 徽章本体 `SetGrey(1)`（原版 `SetTierFrame` 末尾 "
                + "`DoMaterialRefresh(interactable = tier > 0)`；作用范围只有 `Trophy Icon`，档位框不灰）"
                + "｜🧨 删掉 `badge.SetGrey(1f)` ⇒ 得 0 ⇒ 红");
            }
        }
        finally
        {
            // 抽屉句柄表是**静态**的（`ItemDrawer._handles`）⇒ 这里只撤自己那一格；
            // ⛔ 别用 `ClearHandlesForTest()` —— 那会清掉同一宿主里别的夹具的登记。
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>按名字递归找（`Transform.Find` 只找**直接子件** —— 见 `CLAUDE.md` §三 卡根那层 `2DCard` 的教训）。</summary>
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
        MenuCheck.Section(_sink, "B108_A15 · AUD01 余项七条（判据 → `判据/B108_AUD01_余项.md` §四）");
        try { SectionA_Frames(); }
        catch (System.Exception e) { Tr(false, "「A 头像两档」这一组抛异常（后面的条目没跑完）：" + e.GetType().Name + "：" + e.Message); }
        try { SectionB_Cardback(); }
        catch (System.Exception e) { Tr(false, "「B 卡背过扫」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionC_PremiumAndScale(); }
        catch (System.Exception e) { Tr(false, "「C 高级抽屉两层 / 卡牌缩放」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        try { SectionD_Alliance(); }
        catch (System.Exception e) { Tr(false, "「D 联盟徽章」这一组抛异常：" + e.GetType().Name + "：" + e.Message); }
        Debug.Log(_sink.P + "B108R6 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }
}
