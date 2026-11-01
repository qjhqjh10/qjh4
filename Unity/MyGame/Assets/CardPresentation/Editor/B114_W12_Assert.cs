// B114_W12_Assert.cs — 第一百一十四会话 · 写手 `W-12`（2026-11-01）· 账 `B-243` 的断言
//
// ============================ 它盯的是什么 ============================
//  `Expansion Pass Points` 这一族抽屉 `Content` 下那层**恒显**的静态件 `Campaign Glow`
//  （不在 `ItemDrawerComponents` 任何槽里 ⇒ 原版不由代码开关）。**两档都要画，而且是两套不同的数**：
//    · Icon 档 `Icon Expansion Pass Points Drawer Variant`：图 `40K_general_icon_Expansion_Pass_XP`
//      · 框（相对 `Content` 1080²）= (0.243225,0.243225)-(1079.756775,1079.756775)
//      （= 锚 (0,0)-(1,1) + sizeDelta −360.3243 ⇒ 719.6757²，绕中心 ×1.5）
//    · 基础档 `Expansion Pass Points Drawer`：图 `40k_campaign_Expansion_Pass`
//      · 框（相对 `Content` 712×1080）= (-114.6555,146.4597)-(826.6552,717.5403)
//      （锚 (0,0.4)-(1,0.8) + sizeDelta (229.31067,139.08057)，`m_LocalScale = 1`）
//
//  判据 = prefab 现读（`d:/2/tools/assetripper_out/ExportedProject/Assets/`，`!u!224` 的
//  anchor/sizeDelta/scale + `!u!114` 的 `m_Sprite`/`m_PreserveAspect`/`m_IsActive`）。
//  全文与「我们改了什么」→ `资料/完全复刻/报告/B114_W12_CampaignGlow.md`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 期望框是**独立常量**（prefab 设计 px，⛔ 不是读我们代码里的常量现算）⇒ 谁把 `DrawCampaignGlowIcon`
//    那一句删掉 ⇒ A3/A4 **直接红**（节点找不到）。
//  · A5/B6 断「用的是哪张图」—— 若有人拿 `40K_general_icon_Expansion_Pass_XP` 顶替基础档那张（或反过来）⇒ 红。
//  · A6/B5 断**次序**（原版 sibling 序 `Background < Campaign Glow < Image`）——
//    若有人把光圈队列改回 `QBoard` / `QArt`（同号）⇒ 红。这是 `CLAUDE.md` §三「分层用渲染队列」那一条的落地闸。
//  · C1 **结构性反证**：两档各画一次，两个光圈框**必须不同**（≈1079.5² vs 571.08²）。
//    谁把两档合并成一套常量 ⇒ C1 与 A4/B4 里**必有一边红**（两边不可能同时满足）。
//  · 所有「前提」都是**硬断言**（⛔ 不是 `if (图) Check(...)` 静默跳过）：图取不到是**红**。
//
// ============================ 怎么接（⚠️ 只由调度台/本波写手改宿主）============================
//  唯一入口 = `B114_W12_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W12_Assert.Run(); _sink.Pass += B114_W12_Assert.Pass; _sink.Fail += B114_W12_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 只跑**方框**夹具（`K = 1`、`OX = OY = 0`）⇒ `fr.Map` 出来的就是设计 px 本身。
//    非方框调用下 `FrameOf` 还会再差一层（原版 ARF 在窄容器里是溢出还是按宽缩= `ItemDrawerLayout.cs` 文件头 ① 那条未读 TODO）。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-12`：账 `B-243` —— `Expansion Pass Points` 两档抽屉的 `Content/Campaign Glow`。</summary>
public static class B114_W12_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W12] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>Icon 档夹具框：根 1080²（`FrameOf(box, 1080, 1080)` 的 `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box1080 = new PxRect(0f, 0f, 1080f, 1080f);
    /// <summary>基础档夹具框：根 712.8×1080（`FrameOf(box)` 的默认根 ⇒ `K = 1`、`OX = OY = 0`）。</summary>
    static readonly PxRect Box712 = new PxRect(0f, 0f, 712.8f, 1080f);

    // ---- 判据常量（prefab 现读 · 独立于生产代码）----
    /// <summary>Icon 档 `Campaign Glow` 的视觉框（相对 `Content` 1080²）。</summary>
    static readonly PxRect GlowIconWant = new PxRect(0.243225f, 0.243225f, 1079.756775f, 1079.756775f);
    /// <summary>基础档 `Campaign Glow` 的**框**（相对 `Content` 712×1080；绝对设计 px = 左沿 0.4 + 本地框）。
    /// ⚠️ 这一档 `m_PreserveAspect = 1` 且 sprite 是 126×126 方图 ⇒ 画出来会内接成 571.08²，所以断言走 `FitBox`。</summary>
    static readonly PxRect GlowBaseBox = new PxRect(-114.255457f, 146.459715f, 827.055212f, 717.540285f);

    // ---- 图名（两档**不是同一张**）----
    const string ArtIcon = "40K_general_icon_Expansion_Pass_XP";   // `Resources/Art/ui_menu/`
    const string ArtBase = "40k_campaign_Expansion_Pass";          // `Resources/Art/ui_menu/`

    /// <summary>Icon 档画出来的那个框（C1 要用，先存着）。</summary>
    static PxRect _iconGlowRect;
    static bool _iconGlowRectOk;

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W12（B-243）：`Expansion Pass Points` 抽屉的 `Content/Campaign Glow`（两档两套数）");

        _iconGlowRectOk = false;
        SectionIconVariant();
        SectionBaseDrawer();

        Debug.Log(_sink.P + "B114W12 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ Icon 档 `Icon Expansion Pass Points Drawer Variant`
    static void SectionIconVariant()
    {
        MenuCheck.Section(_sink, "① Icon 档：`Campaign Glow` = 满帧×1.5（0.243..-1079.757..）· 图 `" + ArtIcon + "`");

        // 硬前提：图取得到（⛔ 不是 `if (图) Check(...)` —— 取不到就是红）
        var tex = CardArt.MenuUi(ArtIcon);
        Tr(tex != null, "A0 **前提**：`Campaign Glow` 那张图 `" + ArtIcon + "` 取得到（`CardArt.MenuUi` = `Resources/Art/ui_menu/`）"
                      + "｜🧨 删掉 `Resources/Art/ui_menu/" + ArtIcon + ".png` ⇒ 红");

        var root = new GameObject("B114W12_EXPPASS_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W12_ExpPass", Type = "Everguild.LiveOps.ExpansionPassPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W12",
            };
            res = ItemDrawer.Draw(root.transform, Box1080, spec, 1, DrawerOverride.Icon,
                                  ItemDrawerStyle.Default(10, 11, 12));

            Tr(res.Node != null, "A1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerExpansionPassPoint, "A1b **前提**：`ov = Icon` 仍路由到 `ExpansionPassPointDrawer`（这一族没有独立 Icon 类）");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "A2 **前提**：`Content` 建出来了（Icon 变体铺满 1080² 根；`Campaign Glow` 挂在它下面）");

            var glow = FindDeep(res.Node, ItemDrawer.NodeCampaignGlow);
            Tr(glow != null, "A3 **前提**：`Campaign Glow` 建出来了（原版 `Content.m_Children[0]`、`m_IsActive = 1`、不在任何槽里 ⇒ 恒显）"
                          + "｜🧨 删掉 `IconExpansionPassPoint` 里那句 `DrawCampaignGlowIcon(...)` ⇒ 红");

            if (glow != null)
            {
                var got = QuadOf(glow);
                N(got.x1, GlowIconWant.x1, 0.6f, "A4 ★★ 光圈左沿 = 0.243（= 锚 (0,0)-(1,1) + sizeDelta −360.3243 ⇒ 719.6757²，绕中心 ×1.5）");
                N(got.y1, GlowIconWant.y1, 0.6f, "A4 ★★ 光圈上沿 = 0.243");
                N(got.x2, GlowIconWant.x2, 0.6f, "A4 ★★ 光圈右沿 = 1079.757（≈ 满帧）"
                                             + "｜🧨 改成「占满子框不缩放」(180.16..899.84) ⇒ 红");
                N(got.y2, GlowIconWant.y2, 0.6f, "A4 ★★ 光圈下沿 = 1079.757");
                _iconGlowRect = got; _iconGlowRectOk = got.W > 1f;

                var et = TexOf(glow);
                C(et == null ? "<null>" : et.name, ArtIcon,
                  "A5 ★ 用的是 Icon 档那张图（`m_Sprite` guid 6c77cbd6… = `40K_general_icon_Expansion Pass XP`）"
                + "｜🧨 换成基础档那张（或拿阵营徽记顶替）⇒ 红");

                var img = FindDeep(res.Node, ItemDrawer.NodeImage);
                Tr(img != null, "A6 **前提**：主图 `Image` 建出来了");
                var gq = glow.GetComponent<ImageQuad>();
                var iq = img != null ? img.GetComponent<ImageQuad>() : null;
                Tr(gq != null && iq != null, "A6b **前提**：光圈与主图都是 `ImageQuad`（不然「队列」这一项根本无从谈起）");
                if (gq != null && iq != null)
                    Tr(gq.RenderQueue < iq.RenderQueue,
                       "A6 ★★ 层次序：`Campaign Glow` 的队列 **<** 主图的队列（原版 `Content.m_Children` 里它在 `Image` **之前**"
                     + " ⇒ 光圈压在主图下面；它中间那块是近不透明的金属盘，顺序反了会把主图盖掉）"
                     + "｜🧨 把光圈队列改回 `st.QArt` / `st.QBoard`（同号）⇒ 红");
            }

            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null,
               "A7 ★ Icon 档**仍然不建** `Background`（`background` 槽是 `{fileID:0}`）—— 新加的光圈不是「把底板偷偷画回来了」");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 基础档 `Expansion Pass Points Drawer`
    static void SectionBaseDrawer()
    {
        MenuCheck.Section(_sink, "② 基础档：`Campaign Glow` = 941.31×571.08 的框（PA ⇒ 571.08²）· 图 `" + ArtBase + "`");

        var tex = CardArt.MenuUi(ArtBase);
        Tr(tex != null, "B0 **前提**：`Campaign Glow` 那张图 `" + ArtBase + "` 取得到（`CardArt.MenuUi`）"
                      + "｜🧨 删掉 `Resources/Art/ui_menu/" + ArtBase + ".png` ⇒ 红");

        var root = new GameObject("B114W12_EXPPASS_BASE");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W12_ExpPassBase", Type = "Everguild.LiveOps.ExpansionPassPoints",
                Kind = ItemKind.Generic, CardArmy = 10, Label = "B114W12",
            };
            res = ItemDrawer.Draw(root.transform, Box712, spec, 1, DrawerOverride.Default,
                                  ItemDrawerStyle.Default(10, 11, 12));

            Tr(res.Node != null, "B1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerExpansionPassPoint, "B1b **前提**：路由到 `ExpansionPassPointDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null, "B2 **前提**：`Content` 建出来了（基础档 = `fr.Map(0.4,0,712.4,1080)`）");
            var bg = FindDeep(res.Node, ItemDrawer.NodeBackground);
            var img = FindDeep(res.Node, ItemDrawer.NodeImage);
            Tr(img != null, "B2b **前提**：主图 `Image` 建出来了");

            var glow = FindDeep(res.Node, ItemDrawer.NodeCampaignGlow);
            Tr(glow != null, "B3 **前提**：`Campaign Glow` 建出来了（原版 `Content.m_Children[1]`，在 `Background` 之后、`Image` 之前）"
                          + "｜🧨 删掉 `ExpansionPassPoint` 里的 `DrawCampaignGlowBase(...)` ⇒ 红");

            if (glow != null)
            {
                // `m_PreserveAspect = 1`：126×126 方图内接进 941.31×571.08 的框 ⇒ 571.08²
                var want = FitBox(tex, GlowBaseBox);
                var got = QuadOf(glow);
                N(got.x1, want.x1, 0.6f, "B4 ★★ 光圈左沿（941.31×571.08 的框，PA 内接成 571.08² 后居中：70.86）"
                                       + "｜🧨 套用 Icon 档那组数（0.243/1079.757）⇒ 红");
                N(got.y1, want.y1, 0.6f, "B4 ★★ 光圈上沿（146.46）");
                N(got.x2, want.x2, 0.6f, "B4 ★★ 光圈右沿（641.94）");
                N(got.y2, want.y2, 0.6f, "B4 ★★ 光圈下沿（717.54）");

                Tr(!_iconGlowRectOk || Mathf.Abs(got.W - _iconGlowRect.W) > 100f,
                   "C1 ★★ 灭自证：**两档的光圈不是同一个框**（Icon ≈ 1079.5² / 基础档 ≈ 571.1²，实测 "
                 + got.W.ToString("F1") + " vs " + _iconGlowRect.W.ToString("F1") + "）"
                 + "｜🧨 谁把两档合并成一套常量 ⇒ 这一条与 A4 / B4 里**必有一边红**（两边不可能同时满足）");

                var et = TexOf(glow);
                C(et == null ? "<null>" : et.name, ArtBase,
                  "B5 ★ 基础档用的是**它自己那张**（`m_Sprite` guid d2dc932d… = `40k_campaign_Expansion Pass`，126×126）"
                + "｜🧨 两档共用 Icon 档那张 ⇒ 红（两档 prefab 的 sprite guid 本来就不同）");

                var gq = glow.GetComponent<ImageQuad>();
                var iq = img != null ? img.GetComponent<ImageQuad>() : null;
                var bq = bg != null ? bg.GetComponent<ImageQuad>() : null;
                Tr(gq != null && iq != null, "B6a **前提**：光圈与主图都是 `ImageQuad`");
                if (gq != null && iq != null)
                    Tr(gq.RenderQueue < iq.RenderQueue,
                       "B6 ★★ 层次序：光圈队列 **<** 主图队列（原版 `Content.m_Children` 里 `Campaign Glow` 在 `Image` 之前）"
                     + "｜🧨 不改 `st.QArt`（`Default(q,q,q)` 那类调用点同号）⇒ 红");
                if (gq != null && bq != null)
                    Tr(bq.RenderQueue < gq.RenderQueue,
                       "B6b ★★ 层次序：底板队列 **<** 光圈队列（原版 `Content.m_Children` 里 `Campaign Glow` 在 `Background` **之后**）"
                     + "｜🧨 光圈改到 `QBoard` 同号（会被底板盖死）⇒ 红");
            }
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
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
