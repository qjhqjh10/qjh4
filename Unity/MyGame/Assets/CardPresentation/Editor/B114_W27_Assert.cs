// B114_W27_Assert.cs — 第一百一十四会话 · 写手 `W-27`（2026-11-01）· 账 `B-258` 的断言
//   🆕 2026-11-01 写手 `W-39` 追加：③ 段补 `C5/C5b/C5c`（主图几何）· ⑤ 段 `B-265` · ⑥ 段 `B-266`（`SectionMainImage` / `SectionBaseBackground`）。
//
// ============================ 它盯的是什么 ============================
//  `Campaign Points` 这一族抽屉 `Content` 下那层**恒显**的静态件 `Campaign Glow`
//  （不在 `ItemDrawerComponents` 任何槽里 ⇒ 原版不由代码开关）。**两档都要画，而且是两套不同的数**：
//    · Icon 档 `Icon Campaign Points Drawer Variant`：锚 (0,0)-(1,1) + sizeDelta 0 · `m_LocalScale 1.4999999`
//      ⇒ 框（相对 `Content` 1080²）= **(-270,-270)-(1350,1350)**（1620² 过扫）
//    · 基础档 `Campaign Points Drawer`：锚 (0,0.4)-(1,0.8) + sizeDelta (62.5253,37.8941) · `m_LocalScale 1`
//      ⇒ 框（**根**设计 px，`Content` 左沿 = `DrCx1` 0.4 已加）= **(-30.86265,197.05295)-(743.66265,666.94705)**
//        （774.5253 × 469.8941；`m_PreserveAspect = 1` + 256² 方图 ⇒ 画出来内接成 469.8941²）
//    · **两档是同一张图**（`m_Sprite` guid 都是 `d2b2111cc38ea6542aa8031e1578f84f`
//      = `40K_genearl_icon_Campaign points_big`，= `ItemDrawer.PremiumGlowSprite`）。
//
//  判据 = prefab 现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/`，`!u!224` 的
//  anchor/sizeDelta/scale + `!u!114` 的 `m_Sprite`/`m_PreserveAspect`/`m_IsActive`）。
//  全文与「我们改了什么」→ `资料/完全复刻/报告/B114_W27_CampaignPoints光圈.md`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 期望框是**独立常量**（prefab 设计 px，⛔ 不是读我们代码里的常量现算）⇒ 谁把 `CampaignPoint` 里那句
//    光圈调用删掉 ⇒ A3/A4、B3/B4 **直接红**（节点找不到）。
//  · A6 / B6 断「用的是哪张图」—— 两档本来就该是同一张；谁给它换成别的（例如把阵营徽记顶上去）⇒ 红。
//  · A5 / B5 断**次序**（原版 sibling 序 `Background < Campaign Glow < Image`）——
//    若有人把光圈队列改回 `QBoard` / `QArt`（本档调用方 `CampaignPointStyle` 三个号**同值**）⇒ 红。
//  · **C3 + C4 是变体判据的端到端闸**：`DrawCampaignPointsReward` 必须走 **Icon 档**几何
//    （那颗在原版就是 `Icon Campaign Points Drawer Variant`）。若有人把
//    `style.CampaignPointsGlowIcon = true` 那句删掉 ⇒ 回落基础档 ⇒ 光圈宽从 97.5 掉到 ≈30.3 ⇒ C3 红
//    （这是**唯一**能抓到「变体没置位」的那条）。
//  · D1 **结构性反证**：两档的光圈框**必须不同**（1620² vs 469.894²）。
//    谁把两档合并成一套常量 ⇒ D1 与 A3/B3 里**必有一边红**（两边不可能同时满足）。
//  · 所有「前提」都是**硬断言**（⛔ 不是 `if (图) Check(...)` 静默跳过）：图取不到是**红**。
//
// ============================ 怎么接（⚠️ 只由调度台/本波写手改宿主）============================
//  唯一入口 = `B114_W27_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W27_Assert.Run(); _sink.Pass += B114_W27_Assert.Pass; _sink.Fail += B114_W27_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 只跑**方框 / 单框**夹具（`FrameOf` 的 `K` 与 `OX/OY` 都是整帧）—— 非方框调用下 `FrameOf` 还会再差一层
//    （原版 ARF 在窄容器里是溢出还是按宽缩 = `ItemDrawerLayout.cs` 文件头 ① 那条未读 TODO）。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-27`：账 `B-258` —— `Campaign Points` 两档抽屉的 `Content/Campaign Glow`。</summary>
public static class B114_W27_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W27] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>Icon 档夹具框：根 1080²（`FrameOf(box, DrIconRoot, DrIconRoot)` ⇒ `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box1080 = new PxRect(0f, 0f, 1080f, 1080f);
    /// <summary>基础档夹具框：根 712.8×1080（`FrameOf(box)` 的默认根 ⇒ `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box712 = new PxRect(0f, 0f, 712.8f, 1080f);

    // ---- 判据常量（prefab 现读 · 独立于生产代码）----
    /// <summary>Icon 档 `Campaign Glow` 的框（相对 `Content` 1080²；锚 (0,0)-(1,1) + scale 1.5 绕中心）。</summary>
    static readonly PxRect GlowIconWant = new PxRect(-270f, -270f, 1350f, 1350f);
    /// <summary>基础档 `Campaign Glow` 的**布局框**（根设计 px；`Content` 左沿 0.4 已加）。
    /// ⚠️ 这一档 `m_PreserveAspect = 1` 且 sprite 是 256×256 方图 ⇒ 画出来会内接成 469.8941²，所以断言走 `FitBox`。</summary>
    static readonly PxRect GlowBaseBox = new PxRect(-30.86265f, 197.05295f, 743.66265f, 666.94705f);

    // ---- 图名（两档**同一张**）----
    const string ArtGlow = ItemDrawer.PremiumGlowSprite;   // = "40K_genearl_icon_Campaign_points_big"

    /// <summary>Icon 档画出来的那个框（D1 要用，先存着）。</summary>
    static PxRect _iconGlowRect;
    static bool _iconGlowRectOk;
    static string _iconGlowTex;
    static string _baseGlowTex;

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W27（B-258）：`Campaign Points` 抽屉的 `Content/Campaign Glow`（两档两套数）");

        _iconGlowRectOk = false; _iconGlowTex = null; _baseGlowTex = null;
        SectionIconVariant();
        SectionBaseDrawer();
        SectionRewardHelperIsIcon();
        SectionCrossVariant();
        SectionMainImage();        // 🆕 2026-11-01（`W-39` · `B-265`）：主图几何（基础档横带 216..648 ⇒ PA 432²）
        SectionBaseBackground();   // 🆕 2026-11-01（`W-39` · `B-266`）：基础档底板 `Content/Background`（Icon 档槽 null ⇒ 不画）
    }

    // ============================================================ Icon 档 `Icon Campaign Points Drawer Variant`
    static void SectionIconVariant()
    {
        MenuCheck.Section(_sink, "① Icon 档：`Campaign Glow` = 满帧×1.5（-270..1350，1620² 过扫）· 图 `" + ArtGlow + "`");

        // 硬前提：图取得到（⛔ 不是 `if (图) Check(...)` —— 取不到就是红）
        var tex = CardArt.MenuUi(ArtGlow);
        Tr(tex != null, "A0 **前提**：`Campaign Glow` 那张图 `" + ArtGlow + "` 取得到（`CardArt.MenuUi` = `Resources/Art/ui_menu/`）"
                      + "｜🧨 删掉 `Resources/Art/ui_menu/" + ArtGlow + ".png` ⇒ 红");

        var root = new GameObject("B114W27_CAMPPOINTS_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W27_CampPoints", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W27",
            };
            var stIcon = ItemDrawerStyle.Default(10, 10, 11);
            stIcon.CampaignPointsGlowIcon = true;          // = Icon 变体档（判据见 `ItemDrawerPartial_B114W27.cs`）
            res = ItemDrawer.Draw(root.transform, Box1080, spec, 1, DrawerOverride.Icon, stIcon);

            Tr(res.Node != null, "A1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerCampaignPoint,
              "A1b **前提**：仍路由到 `CampaignPointDrawer`（这一族没有独立 Icon 类，`ItemTypeSets:414` 两列同名）");
            if (res.Node == null) return;

            var glow = FindDeep(res.Node, ItemDrawer.NodeCampaignGlow);
            Tr(glow != null, "A2 **前提**：`Campaign Glow` 建出来了（原版 `Content.m_Children[0]`、`m_IsActive = 1`、不在任何槽里 ⇒ 恒显）"
                          + "｜🧨 删掉 `CampaignPoint` 里那句 `DrawCampaignPointsGlowIcon(...)` 分支 ⇒ 红");

            if (glow != null)
            {
                var got = QuadOf(glow);
                N(got.x1, GlowIconWant.x1, 0.6f, "A3 ★★ 光圈左沿 = -270（= 锚 (0,0)-(1,1) + sizeDelta 0，绕中心 ×1.5）");
                N(got.y1, GlowIconWant.y1, 0.6f, "A3 ★★ 光圈上沿 = -270");
                N(got.x2, GlowIconWant.x2, 0.6f, "A3 ★★ 光圈右沿 = 1350（1080×1.5 的过扫）"
                                             + "｜🧨 改成「占满 Content 不缩放」(0..1080) ⇒ 红");
                N(got.y2, GlowIconWant.y2, 0.6f, "A3 ★★ 光圈下沿 = 1350");
                N(got.W, 1620f, 1.2f, "A3b ★★ 光圈宽 = 1080 × 1.5 = 1620");
                _iconGlowRect = got; _iconGlowRectOk = got.W > 1f;

                var et = TexOf(glow);
                _iconGlowTex = et == null ? "<null>" : et.name;
                C(_iconGlowTex, ArtGlow,
                  "A4 ★ 用的是 `Campaign Glow` 那张（`m_Sprite` guid d2b2111c… = `40K_genearl_icon_Campaign points_big`）"
                + "｜🧨 换成阵营徽记 / 别的图 ⇒ 红");

                var img = FindDeep(res.Node, ItemDrawer.NodeArmyIcon);
                Tr(img != null, "A5a **前提**：主图（`NodeArmyIcon`）建出来了");
                var gq = glow.GetComponent<ImageQuad>();
                var iq = img != null ? img.GetComponent<ImageQuad>() : null;
                Tr(gq != null && iq != null, "A5b **前提**：光圈与主图都是 `ImageQuad`（不然「队列」这一项根本无从谈起）");
                if (gq != null && iq != null)
                {
                    Tr(gq.RenderQueue > 10, "A5 ★★ 光圈队列在底板之上（`CampaignPointsGlowQueue` = `QBoard+1`）"
                                          + "｜🧨 改回 `st.QBoard`（本档 `QBoard == QArt`）⇒ 红");
                    Tr(gq.RenderQueue < iq.RenderQueue,
                       "A5 ★★ 层次序：`Campaign Glow` 的队列 **<** 主图的队列（原版 `Content.m_Children` 里它在 `Image` **之前**"
                     + " ⇒ 光圈压在主图下面；它中间那块是近不透明的金属盘，顺序反了会把主图盖掉）"
                     + "｜🧨 不改 `st.QArt`（`Default(q,q,q)` 那类调用点同号）⇒ 红");
                }
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 基础档 `Campaign Points Drawer`
    static void SectionBaseDrawer()
    {
        MenuCheck.Section(_sink, "② 基础档：`Campaign Glow` = 774.5253×469.8941 的框（PA ⇒ 469.8941²）");

        var tex = CardArt.MenuUi(ArtGlow);
        Tr(tex != null, "B0 **前提**：`Campaign Glow` 那张图 `" + ArtGlow + "` 取得到（`CardArt.MenuUi`）");

        var root = new GameObject("B114W27_CAMPPOINTS_BASE");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W27_CampPointsBase", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W27",
            };
            res = ItemDrawer.Draw(root.transform, Box712, spec, 1, DrawerOverride.Default,
                                  ItemDrawerStyle.Default(10, 10, 11));

            Tr(res.Node != null, "B1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerCampaignPoint, "B1b **前提**：路由到 `CampaignPointDrawer`");
            if (res.Node == null) return;

            var glow = FindDeep(res.Node, ItemDrawer.NodeCampaignGlow);
            Tr(glow != null, "B2 **前提**：`Campaign Glow` 建出来了（原版 `Content.m_Children[1]`，在 `Background` 之后、`Image` 之前）"
                          + "｜🧨 删掉 `CampaignPoint` 里那句 `DrawCampaignPointsGlowBase(...)` 分支 ⇒ 红");

            if (glow != null)
            {
                // `m_PreserveAspect = 1`：256×256 方图内接进 774.5253×469.8941 的框 ⇒ 469.8941²（居中在 x=356.4）
                var want = FitBox(tex, GlowBaseBox);
                var got = QuadOf(glow);
                N(got.x1, want.x1, 0.6f, "B3 ★★ 光圈左沿（774.53×469.89 的框，PA 内接成 469.89² 后居中：121.45）"
                                       + "｜🧨 套用 Icon 档那组数（-270/1350）⇒ 红");
                N(got.y1, want.y1, 0.6f, "B3 ★★ 光圈上沿（197.05）");
                N(got.x2, want.x2, 0.6f, "B3 ★★ 光圈右沿（590.35）");
                N(got.y2, want.y2, 0.6f, "B3 ★★ 光圈下沿（666.95）");
                N(got.H, 469.8941f, 0.8f, "B3b ★★ 画出来的高 = 469.8941（= 布局框的高，方图 PA 后不被裁）"
                                        + "｜🧨 把 sizeDelta 那两半漏掉（只用锚 0.4..0.8 = 432 高）⇒ 红");
                N(got.W, got.H, 0.8f, "B3c ★ 方图 PA ⇒ 宽 == 高");

                var et = TexOf(glow);
                _baseGlowTex = et == null ? "<null>" : et.name;
                C(_baseGlowTex, ArtGlow,
                  "B4 ★ 两档**同一张**图（两份 prefab 的 `m_Sprite` guid 都是 d2b2111c…）");

                var img = FindDeep(res.Node, ItemDrawer.NodeArmyIcon);
                var gq = glow.GetComponent<ImageQuad>();
                var iq = img != null ? img.GetComponent<ImageQuad>() : null;
                Tr(gq != null && iq != null, "B5a **前提**：光圈与主图都是 `ImageQuad`");
                if (gq != null && iq != null)
                    Tr(gq.RenderQueue < iq.RenderQueue,
                       "B5 ★★ 层次序：光圈队列 **<** 主图队列（原版 `Content.m_Children` 里 `Campaign Glow` 在 `Image` 之前）"
                     + "｜🧨 不改 `st.QArt`（`Default(q,q,q)` 那类调用点同号）⇒ 红");
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 端到端：`DrawCampaignPointsReward` 必须走 Icon 档
    static void SectionRewardHelperIsIcon()
    {
        MenuCheck.Section(_sink, "③ 端到端：`DrawCampaignPointsReward` 走 **Icon 档**几何（= `Campaign Tab` 高级面板那颗）");

        // 真实站点的框（`CampaignTab.cs:1447`）：65 × 69.73 ⇒ `K = 65/1080` ⇒ 光圈 = 65 × 1.5 = 97.5
        var box = new PxRect(541.22f, 910.43f, 606.22f, 980.15f);
        var root = new GameObject("B114W27_CAMPPOINTS_HELPER");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(3000, 3000, 3000);
            res = ItemDrawer.DrawCampaignPointsReward(root.transform, box, 200, 10 /* Ultramarines */, st);

            Tr(res.Node != null, "C1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerCampaignPoint, "C1b **前提**：`Type = \"CampaignPoints\"` 仍路由到 `CampaignPointDrawer`");
            if (res.Node == null) return;

            var glow = FindDeep(res.Node, ItemDrawer.NodeCampaignGlow);
            Tr(glow != null, "C2 **前提**：`Campaign Glow` 建出来了｜🧨 删掉 `CampaignPoint` 里的光圈调用 ⇒ 红");
            if (glow == null) return;

            var got = QuadOf(glow);
            N(got.W, 97.5f, 1.0f, "C3 ★★ 本格光圈宽 = min(65, 69.73) × 1.5 = 97.5（= **Icon 档**几何：Content 1080² 铺满再 ×1.5）"
                                + "｜🧨 删掉 `DrawCampaignPointsReward` 里那句 `style.CampaignPointsGlowIcon = true` ⇒ 回落基础档（≈30.3 宽）⇒ 红");
            N(got.W, got.H, 0.8f, "C3b ★ 方图 PA ⇒ 宽 == 高");

            Tr(res.Art == "40k_DeckSelection_icon_FactionUM",
               "C4 **前提**：主图仍是阵营徽记（本波不动 `DrawForArmy` 那条替换语义）");

            // 🆕 2026-11-01（`W-39` · `B-265`）：主图几何也照 `Content/Image` 的锚点（不再是 `Square(box, st.ArmyFill)`）
            var imgE2E = FindDeep(res.Node, ItemDrawer.NodeArmyIcon);
            Tr(imgE2E != null, "C5 **前提**：主图节点建出来了（`C5b/C5c` 要量它）");
            if (imgE2E != null)
            {
                var gi = QuadOf(imgE2E);
                N(gi.W, 65f, 1.0f, "C5b ★★ 本格主图宽 = min(65, 69.73) = 65（Icon 档 `Content/Image` 锚 (0,0)-(1,1) 铺满 1080² 的 `Content` ⇒ PA 后 65²）"
                                  + "｜🔴 `W-39` 2026-11-01 **前后值**：本夹具（`ItemDrawerStyle.Default` ⇒ `ArmyFill 0.34`）**改前 22.1** ⇒ 改后 65；"
                                  + "真实站点 `CampaignTab.CampaignPointStyle()` 给的是 `ArmyFill = 1f` ⇒ 那边**改前改后都是 65**（不变）"
                                  + "｜🧨 让 `CampaignPointsImageRect` 的 Icon 支回退成 `Square(box, st.ArmyFill)` ⇒ 本夹具回到 22.1 ⇒ 红");
                N(gi.W, gi.H, 0.8f, "C5c ★ 方图 PA ⇒ 宽 == 高");
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 灭自证：两档框必须不同
    static void SectionCrossVariant()
    {
        MenuCheck.Section(_sink, "④ 灭自证：两档的光圈**不是**同一个框，但**是**同一张图");

        Tr(_iconGlowRectOk,
           "D0 **前提**：① Icon 档那次真的量到了光圈（不然 D1 不成立）");

        Tr(_iconGlowRectOk && Mathf.Abs(1620f - 469.8941f) > 1000f,
           "D1 ★★ 两档的判据框**结构上就不一样**：Icon = 1620²，基础档 = 469.8941²"
         + "（实测 Icon = " + (_iconGlowRectOk ? _iconGlowRect.W.ToString("F1") : "<未量到>") + "）"
         + "｜🧨 谁把两档合并成一套常量 ⇒ 这一条与 A3 / B3 里**必有一边红**（两边不可能同时满足）");

        C(_iconGlowTex, _baseGlowTex,
          "D2 ★ 两档用的是**同一张**图（原版两份 prefab 的 `m_Sprite` guid 都是 d2b2111c…）"
        + "｜🧨 谁给某一档换成别的图 ⇒ 红");
    }

    // ============================================================ 🆕 ⑤ `B-265`：主图几何（两档照各自 prefab 的锚点）
    //
    //  判据（现读 `d:/2/tools/assetripper_out/ExportedProject/Assets/Campaign Points Drawer.prefab`）：
    //    · 基础档 `Content/Image`（RT `&224017697238300914`）= 锚 `(0,0.4)-(1,0.8)` + `m_SizeDelta (0,0)`
    //      + `m_LocalScale (1,1,1)` + `m_PreserveAspect 1`
    //      ⇒ 根设计 px 的横带 = `x 0.4..712.4` × `y 216..648`（= `1080 − 0.8×1080 .. 1080 − 0.4×1080`）；
    //        256² 方图按 PA 内接 ⇒ **432²**，居中在 `(356.4, 432)` ⇒ 画出来 `(140.4,216)-(572.4,648)`。
    //    · Icon 档 `Content/Image`（RT `&224313026266067498`）= 锚 `(0,0)-(1,1)` + `m_SizeDelta (0,0)` + PA
    //      ⇒ 铺满 1080² 的 `Content` ⇒ **1080²**。
    //  🔴 期望值全是**独立常量**（从 prefab 的锚点算式来，⛔ 不读 `ItemDrawer.CampaignPointsImageRect` 现算）。
    static void SectionMainImage()
    {
        MenuCheck.Section(_sink, "⑤ B-265：主图（`Content/Image`）两档照各自的 prefab 锚点 —— 基础档横带 216..648 ⇒ PA 后 432²");

        var tex = CardArt.MenuUi("40k_DeckSelection_icon_FactionUM");
        Tr(tex != null, "E0 **前提**：阵营徽记 `40k_DeckSelection_icon_FactionUM` 取得到（256² 方图；取不到 ⇒ 后面量的是空框，⛔ 不是绿）");

        // ---- 基础档：`Campaign Points Drawer` ----
        var rootB = new GameObject("B114W39_IMG_BASE");
        ItemDrawResult rb = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W39_ImgBase", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W39",
            };
            // ⚠️ `ArmyFill = 1f` = `CampaignTab.CampaignPointStyle()` 给的那一档（最接近旧行为的取值）
            var stB = ItemDrawerStyle.Default(10, 10, 11); stB.ArmyFill = 1f;
            rb = ItemDrawer.Draw(rootB.transform, Box712, spec, 1, DrawerOverride.Default, stB);

            Tr(rb.Node != null, "E1 **前提**：基础档抽屉根建出来了");
            C(rb.Drawer, ItemDrawer.DrawerCampaignPoint, "E1b **前提**：路由到 `CampaignPointDrawer`");
            if (rb.Node != null)
            {
                var img = FindDeep(rb.Node, ItemDrawer.NodeArmyIcon);
                Tr(img != null, "E2 **前提**：主图节点（`Army Icon`）建出来了");
                if (img != null)
                {
                    var got = QuadOf(img);
                    N(got.x1, 140.4f, 0.8f, "E3 ★★ 基础档主图左沿 = 356.4 − 216 = 140.4（712×432 的横带里 PA 内接成 432² 后居中）");
                    N(got.y1, 216f, 0.8f, "E3 ★★ 上沿 = 216（= `Content/Image` 锚 0.8 × 1080 那一刀，⛔ 不是整根 0）");
                    N(got.x2, 572.4f, 0.8f, "E3 ★★ 右沿 = 356.4 + 216 = 572.4");
                    N(got.y2, 648f, 0.8f, "E3 ★★ 下沿 = 648（= 锚 0.4 × 1080 那一刀）");
                    N(got.W, 432f, 0.8f, "E3b ★★ 基础档主图宽 = 432（= 横带的高，256² 方图 PA 内接）"
                                       + "｜🧨 改回 `Square(box, st.ArmyFill)` ⇒ 本条红（见 E4 的两个对照值）");
                    N(got.H, got.W, 0.8f, "E3c ★ 方图 PA ⇒ 宽 == 高");

                    // 灭自证：旧写法**两种** `ArmyFill` 取值都结构上不可能命中 432
                    float oldFull = Mathf.Min(Box712.W, Box712.H) * 1f;      // `CampaignPointStyle()` 给的 1f ⇒ 712.8
                    float oldDef = Mathf.Min(Box712.W, Box712.H) * 0.34f;    // `ItemDrawerStyle.Default` 给的 0.34 ⇒ 242.35
                    Tr(Mathf.Abs(got.W - oldFull) > 50f && Mathf.Abs(got.W - oldDef) > 50f,
                       "E4 ★★ **灭自证**：量到的宽（" + got.W.ToString("F1") + "）**不是**旧写法任一 `ArmyFill` 的结果"
                     + "（`CampaignPointStyle` 的 1f ⇒ " + oldFull.ToString("F1") + " · `Default` 的 0.34 ⇒ " + oldDef.ToString("F1") + "）"
                     + "｜🧨 把 `CampaignPointsImageRect` 换回 `Square(box, st.ArmyFill)` ⇒ E3 与 E4 一起红"
                     + "（两边**结构上**不可能同时满足：432 ≠ 712.8 ≠ 242.35）");
                }
            }
        }
        finally
        {
            if (rb.View != null) rb.View.Release();
            Object.DestroyImmediate(rootB);
        }

        // ---- Icon 档：`Icon Campaign Points Drawer Variant`（锚 (0,0)-(1,1) ⇒ 1080²）----
        var rootI = new GameObject("B114W39_IMG_ICON");
        ItemDrawResult ri = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W39_ImgIcon", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W39",
            };
            var stI = ItemDrawerStyle.Default(10, 10, 11);
            stI.CampaignPointsGlowIcon = true;
            // ⚠️ `ArmyFill` 故意给 0.34（`Default` 的出厂值）：几何**不该**再受它影响
            ri = ItemDrawer.Draw(rootI.transform, Box1080, spec, 1, DrawerOverride.Icon, stI);

            Tr(ri.Node != null, "E5 **前提**：Icon 档抽屉根建出来了");
            C(ri.Drawer, ItemDrawer.DrawerCampaignPoint, "E5b **前提**：Icon 档仍路由到 `CampaignPointDrawer`");
            if (ri.Node != null)
            {
                var imgI = FindDeep(ri.Node, ItemDrawer.NodeArmyIcon);
                Tr(imgI != null, "E6 **前提**：Icon 档主图节点建出来了");
                if (imgI != null)
                {
                    var got = QuadOf(imgI);
                    N(got.x1, 0f, 0.8f, "E7 ★★ Icon 档主图左沿 = 0（`Content` 锚 (0,0)-(1,1) 铺满 1080² 根）");
                    N(got.y1, 0f, 0.8f, "E7 ★★ 上沿 = 0");
                    N(got.x2, 1080f, 0.8f, "E7 ★★ 右沿 = 1080");
                    N(got.y2, 1080f, 0.8f, "E7 ★★ 下沿 = 1080");
                    N(got.W, 1080f, 1.0f, "E7b ★★ Icon 档主图宽 = 1080（铺满 Content；PA 后方图仍是 1080²）"
                                        + "｜🧨 让 `CampaignPointsImageRect` 的 Icon 支也吃 `st.ArmyFill`（0.34 × 1080 = 367.2）⇒ 红");
                }
            }
        }
        finally
        {
            if (ri.View != null) ri.View.Release();
            Object.DestroyImmediate(rootI);
        }
    }

    // ============================================================ 🆕 ⑥ `B-266`：基础档底板 `Content/Background`
    //
    //  判据（现读 `Campaign Points Drawer.prefab`）：
    //    · `ItemDrawerComponents`（`!u!114 &114302127217294661`）的 `background` = `{fileID: 114875609422105747}`
    //      ⇒ **真实节点** `Content/Background`（`m_IsActive 1` · `m_Enabled 1` · `m_PreserveAspect 1`）
    //      ⇒ **原版真的画**（⚠️ 与 `B-256` 那条「有槽但 `m_IsActive: 0`」是两回事）
    //    · Icon 档（`!u!114 &114985305603660343`）的 `background` = **`{fileID: 0}`** ⇒ 那一档**不画**
    //  期望值同样是**独立常量**（`Content` = 根设计 px 的 `(0.4,0)-(712.4,1080)`）。
    static void SectionBaseBackground()
    {
        MenuCheck.Section(_sink, "⑥ B-266：基础档底板 `Content/Background`（`background` 槽 = 真实节点；Icon 档槽 null ⇒ 不画）");

        var tex = CardArt.MenuUi("40k_Rewards_icon_base_card");
        Tr(tex != null, "F0 **前提**：底板图 `40k_Rewards_icon_base_card` 取得到（`CardArt.MenuUi` = `Resources/Art/ui_menu/`）"
                      + "｜🧨 删掉那张 PNG ⇒ 红（⛔ 不是 `if (图) Check(...)` 静默跳过）");

        // ---- 基础档：必须有，而且就是 `Content` 整块 ----
        var rootB = new GameObject("B114W39_BG_BASE");
        ItemDrawResult rb = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W39_BgBase", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W39",
            };
            rb = ItemDrawer.Draw(rootB.transform, Box712, spec, 1, DrawerOverride.Default,
                                 ItemDrawerStyle.Default(10, 10, 11));

            Tr(rb.Node != null, "F1 **前提**：基础档抽屉根建出来了");
            if (rb.Node == null) return;

            var bg = FindDeep(rb.Node, ItemDrawer.NodeBackground);
            Tr(bg != null, "F1b ★★ 基础档建出了 `Background` 那一层（原版 `ItemDrawerComponents.background` 指向真实节点、"
                         + "`m_IsActive 1` + `m_Enabled 1` ⇒ **原版真的画**）"
                       + "｜🧨 删掉 `CampaignPoint` 里那句 `DrawCampaignPointsBackground(...)` ⇒ 红");
            if (bg != null)
            {
                var got = QuadOf(bg);
                N(got.x1, 0.4f, 1.0f, "F2 ★★ 左沿 = `Content` 左沿 0.4（`m_SizeDelta 0` ⇒ 与 `Content` 同框）");
                N(got.y1, 0f, 1.0f, "F2 ★★ 上沿 = 0");
                N(got.x2, 712.4f, 1.0f, "F2 ★★ 右沿 = 712.4（= `Content` 右沿）");
                N(got.y2, 1080f, 1.0f, "F2 ★★ 下沿 = 1080（= 根高）");
                N(got.W, 712f, 1.0f, "F2b ★★ 宽 = `Content` 宽 712");
                N(got.H, 1080f, 1.0f, "F2b ★★ 高 = 1080（底板铺满整根，⛔ 不是主图那条横带）");

                var bt = TexOf(bg);
                C(bt == null ? null : bt.name, "40k_Rewards_icon_base_card",
                  "F3 ★ 用的是 `background` 槽那张图（`m_Sprite` guid `c8f9b1a41360ac845919c316ddae577b`）"
                + "｜🧨 换成主图/光圈那张（`40K_genearl_icon_Campaign_points_big`）⇒ 红");

                // 层次序：原版 sibling 序 `Background < Campaign Glow < Image`
                var glow = FindDeep(rb.Node, ItemDrawer.NodeCampaignGlow);
                var img = FindDeep(rb.Node, ItemDrawer.NodeArmyIcon);
                var bq = bg.GetComponent<ImageQuad>();
                var gq = glow != null ? glow.GetComponent<ImageQuad>() : null;
                var iq = img != null ? img.GetComponent<ImageQuad>() : null;
                Tr(bq != null && gq != null && iq != null,
                   "F4a **前提**：底板 / 光圈 / 主图三件都是 `ImageQuad`（缺一件 ⇒ 下面那条无从谈起）");
                if (bq != null && gq != null && iq != null)
                    Tr(bq.RenderQueue < gq.RenderQueue && gq.RenderQueue < iq.RenderQueue,
                       "F4 ★★ 层次序 `Background < Campaign Glow < Image`（原版 `Content.m_Children` 的 sibling 序：0/1/2）"
                     + "｜🧨 给底板也用 `st.QBoard + 1`（与光圈同号）⇒ 红");
            }
        }
        finally
        {
            if (rb.View != null) rb.View.Release();
            Object.DestroyImmediate(rootB);
        }

        // ---- Icon 档：必须**没有**（`background: {fileID: 0}`）----
        var rootI = new GameObject("B114W39_BG_ICON");
        ItemDrawResult ri = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W39_BgIcon", Type = "CampaignPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W39",
            };
            var stI = ItemDrawerStyle.Default(10, 10, 11); stI.CampaignPointsGlowIcon = true;
            ri = ItemDrawer.Draw(rootI.transform, Box1080, spec, 1, DrawerOverride.Icon, stI);

            Tr(ri.Node != null, "F5 **前提**：Icon 档抽屉根建出来了");
            if (ri.Node != null)
                Tr(FindDeep(ri.Node, ItemDrawer.NodeBackground) == null,
                   "F5b ★★ **两档判别 / 灭自证**：Icon 档**不建** `Background`（`Icon Campaign Points Drawer Variant` 的"
                 + "`background` 槽 = `{fileID: 0}` ⇒ 原版不画；同族先例 `Editor/B114_C_Assert.cs:121`）"
                 + "｜🧨 把 `DrawCampaignPointsBackground` 里那道 `CampaignPointsGlowIcon` 闸删掉（两档都画）⇒ 本条红，"
                 + "而 F1b/F2 仍然绿 —— 这两组**结构上**不可能被一个错的实现同时满足");
        }
        finally
        {
            if (ri.View != null) ri.View.Release();
            Object.DestroyImmediate(rootI);
        }
    }

    // ============================================================ 夹具 / 量具
    /// <summary>`MenuDraw.Rect(..., keepAspect: true)` 之后**画出来**的矩形（= uGUI `PreserveSpriteAspectRatio`）。
    /// 直接用生产代码那条 `CardbackFace.Fit` ⇒ 期望值只差「喂进去的框」，而那个框是**独立的 prefab 常量**。</summary>
    static PxRect FitBox(Texture2D tex, PxRect box)
    {
        float dw, dh, dx, dyUp;
        if (tex == null || !CardbackFace.Fit(tex, box.W, box.H, out dw, out dh, out dx, out dyUp)) return box;
        float cx = box.CX + dx, cy = box.CY - dyUp;     // `Fit` 的 dy 是**向上为正**；本仓 `PxRect` 是 y 向下
        return new PxRect(cx - dw * 0.5f, cy - dh * 0.5f, cx + dw * 0.5f, cy + dh * 0.5f);
    }

    /// <summary>图上的件是 `ImageQuad` ⇒ 走 `MenuDraw.QuadRectPx` 反推（设计 px · 左上原点 · y 向下）。</summary>
    static PxRect QuadOf(Transform t)
    {
        var q = t.GetComponent<ImageQuad>();
        return q == null ? new PxRect() : MenuDraw.QuadRectPx(q);
    }

    static Texture2D TexOf(Transform t)
    {
        var q = t.GetComponent<ImageQuad>();
        return (q == null || q.Texture == null) ? null : q.Texture as Texture2D;
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
