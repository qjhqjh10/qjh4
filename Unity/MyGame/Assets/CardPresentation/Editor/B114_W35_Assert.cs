// B114_W35_Assert.cs — 第一百一十四会话 · 写手 `W-35`（2026-11-01）· 账 `B-275` / `B-276` / `B-277`
//
// ============================ 它盯的是什么 ============================
//  ① **`B-275`：`Title Drawer`（竖卡）的 `Content/Background` 是另一张图。**
//     真包现读（`menu_dump.py bundle_menus_assets_all "Title Drawer"`）= `40k_Rewards_icon_title card`
//     （329×499），而我们 `ItemDrawerTitle.Title(...)` 原来转调共享的 `DrawerBackground` ⇒ 画的是
//     `40k_Rewards_icon_base card`（328×498）。**对照组 = `Avatar Drawer`，它的确是 base card**。
//  ② **`B-276`：`Title Drawer` 的 `Content/Army` 字号是 35.099998，不是 75。**
//     对照 `Avatar Drawer` 的 `Army` **确实是 75.0** ⇒ **75 是通则、Title 是例外**（⛔ 别把通则改掉）。
//     横向档（`Title Drawer Horizontal Variant`）不受影响：它 `Army` 是 `m_IsActive 0`、我们根本不建。
//  ③ **`B-277`：`Alliance Badge Drawer Icon` 的根是固定尺寸。**
//     prefab 根：**无 `AspectRatioFitter`** · 锚 `(0.5,0.5)-(0.5,0.5)` · `m_AnchoredPosition (0, 44.6322)` ·
//     `m_SizeDelta **165.9539 × 155.7357**` ⇒ 原版**永远那么小**，父框多大都不变。
//     我们原来（`B114_WC` 的 `IconAllianceBadge`）用 `frameR = box` ⇒ **框大画大、框小画小**。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · ① 的期望值**不是**读 `ItemDrawerLayout.DrBackgroundSprite` 得来的（那是被测常量）—— 本节自己写死
//    真包里那两个字面量（`40k_Rewards_icon_title card` / `40k_Rewards_icon_base card`，空格→下划线），
//    再与**画出来的 `ImageQuad.Texture.name`** 比。
//    A2/A3 是**一对**：A2 断 Title = title card、A3 断 Avatar = base card
//    🧨 把共享默认值改成 title card ⇒ A3 红；把 Title 改回默认 ⇒ A2 红。**两处同时改**才会“全绿”，
//    所以 A4 再加一条结构性反证：同一个框里画两档，两张**必须是不同的图**。
//  · ② 读的是**标签身上那条「标称字号」**（`Label.NominalPx`，单位 = 画布 px；它**不含自适应结果**，
//    判据 → `Battle/Label.cs` 的 `NominalPx()` doc），⛔ 不是读生产常量。
//    B3/B4 同理是一对 + B5 结构性反证（两档标称**必须不等**）。
//    ⚠️ **为什么不读 `Label.FontSize` / `FontSizeMax`**：前者是自适应**收敛结果**、后者两档**都是 75px**
//    （auto 区间逐份相同）⇒ 那两个口**照不出这条差异**，读它们等于自证。
//  · ③ 用**两个不同大小的框**画同一档：C3 断「两框量出来**逐值相同**」、C4 断「和手算的 155.74 一致」；
//    对照组 C5 用**同一族的另一份 prefab**（`Alliance Badge Drawer` 基础档）在同样两个框里画，
//    它**必须跟着框变** ⇒ 「固定尺寸」是这一档的**例外**，不是全族通则。
//    🧨 把 `AllianceBadgeIconRootOf` 改回 `box` ⇒ C3 红；把**基础档**也改成固定 ⇒ C5 红。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W35_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W35_Assert.Run(); _sink.Pass += B114_W35_Assert.Pass; _sink.Fail += B114_W35_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 本节只建临时 GameObject / 只读，**不写盘**、不跑场景。
//  · ② 的「标称字号」用**反射**读 `Label.NominalPx()`（那是私有方法；同族先例 = `Label.BaseFld` 反射读
//    `m_fontSizeBase`）。拿不到句柄 ⇒ **B0 前提直接红**（⛔ 不许静默跳过）。
//  · ③ 只断「几何与框无关」，**不断**「原版那一格长什么样」（没有实拍）；上移 44.6322 是真包字段原文。
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-35`：`B-275`（Title 底板图）+ `B-276`（Title Army 字号）+ `B-277`（Alliance Badge Icon 固定尺寸根）。</summary>
public static class B114_W35_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W35] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（⛔ 一律从**真包** `menu_dump` 现读，不从生产代码反读）----
    /// <summary>`Title Drawer` 的 `Content/Background.m_Sprite`（329×499）。</summary>
    const string JudgeTitleBg = "40k_Rewards_icon_title_card";
    /// <summary>共享底图（`Avatar Drawer` 等默认档的 `Content/Background.m_Sprite`，328×498）。</summary>
    const string JudgeBaseBg = "40k_Rewards_icon_base_card";
    /// <summary>`Title Drawer` 的 `Content/Army.m_fontSize`（auto [12,75] · base 36）。</summary>
    const float JudgeTitleArmyPx = 35.1f;
    /// <summary>`Avatar Drawer` 的 `Content/Army.m_fontSize`（**通则**）。</summary>
    const float JudgeAvatarArmyPx = 75f;
    /// <summary>`Alliance Badge Drawer Icon` 根的 `m_SizeDelta`。</summary>
    const float JudgeBadgeW = 165.9539f, JudgeBadgeH = 155.7357f;
    /// <summary>根的 `m_AnchoredPosition.y`（Unity 向上为正 ⇒ 本工程 y 向下要**减**）。</summary>
    const float JudgeBadgeAnchorY = 44.6322f;

    // ---- 夹具 ----
    /// <summary>方框（= 基础档根的尺寸，`K = 1` ⇒ 设计 px 与世界 px 1:1，读数不用乘）。</summary>
    static readonly PxRect BoxRoot = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>③ 的小框：恰好等于 Icon 档根的尺寸。</summary>
    static readonly PxRect BoxBadgeSmall = new PxRect(0f, 0f, JudgeBadgeW, JudgeBadgeH);
    /// <summary>③ 的大框：400×400（比根大得多 —— 跟框实现在这里会画成两倍多）。</summary>
    static readonly PxRect BoxBadgeBig = new PxRect(0f, 0f, 400f, 400f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W35（B-275 / B-276 / B-277）：Title 底板图 + Title Army 字号 + Alliance Badge Icon 固定根");

        // ⚠️ 三段各自独立：某一段「前提不成立就 return」不会把别的段吞掉
        SectionTitleBackground();
        SectionTitleArmyFont();
        SectionAllianceBadgeIcon();

        Debug.Log(_sink.P + "B114W35 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① `B-275`：Title 的底板图
    static void SectionTitleBackground()
    {
        MenuCheck.Section(_sink, "① `B-275` `Title Drawer` 的底板 = `40k_Rewards_icon_title card`（⛔ 不是共享的 `..._base card`）");

        // —— 硬前提：两张图都在 `Resources/Art/ui_menu/`（缺图 ⇒ 那一层根本不建，A2/A3 没有对象）——
        Tr(CardArt.MenuUi(JudgeTitleBg) != null,
           "A0 **前提**：Title 专属底板 `" + JudgeTitleBg + "` 取得到（`Resources/Art/ui_menu/`）"
         + "｜🧨 删掉那张 png ⇒ 红（那时 `Content/Background` 整层不建）");
        Tr(CardArt.MenuUi(JudgeBaseBg) != null,
           "A0b **前提**：共享底板 `" + JudgeBaseBg + "` 取得到（对照组要用）");

        var titleRoot = new GameObject("B114W35_TITLE_BG");
        ItemDrawResult resTitle = default(ItemDrawResult);
        try
        {
            resTitle = ItemDrawer.Draw(titleRoot.transform, BoxRoot, SpecTitle(), 1, DrawerOverride.Default,
                                       ItemDrawerStyle.Default(10, 11, 12));
            C(resTitle.Drawer, ItemDrawer.DrawerTitle,
              "A1 **前提**：`CosmeticItemTitle` + `Default`(0) 路由到竖卡 `TitleDrawer`");
            Tr(resTitle.Node != null, "A1b **前提**：称号抽屉根建出来了");
            if (resTitle.Node == null) return;

            var bg = FindDeep(resTitle.Node, ItemDrawer.NodeBackground);
            Tr(bg != null, "A1c **前提**：`Content/Background` 建出来了");
            if (bg == null) return;
            var q = bg.GetComponent<ImageQuad>();
            Tr(q != null && q.Texture != null, "A1d **前提**：底板那一件带 `ImageQuad` 且有贴图");
            if (q == null || q.Texture == null) return;

            C(q.Texture.name, JudgeTitleBg,
              "A2 ★★ `Title Drawer` 的底板贴图 = **`" + JudgeTitleBg + "`**（真包现读 `Content/Background.m_Sprite`）"
            + "｜🧨 改回共享的 `DrawerBackground(...)` 默认值 ⇒ 这里读到 `40k_Rewards_icon_base_card` ⇒ 红");
        }
        finally
        {
            if (resTitle.View != null) resTitle.View.Release();
            Object.DestroyImmediate(titleRoot);
        }

        // —— 对照组：同族的默认档（`Avatar Drawer`）**必须还是** base card ——
        var avatarRoot = new GameObject("B114W35_AVATAR_BG");
        ItemDrawResult resAvatar = default(ItemDrawResult);
        try
        {
            resAvatar = ItemDrawer.Draw(avatarRoot.transform, BoxRoot, SpecAvatar(), 1, DrawerOverride.Default,
                                        ItemDrawerStyle.Default(20, 21, 22));
            C(resAvatar.Drawer, ItemDrawer.DrawerAvatar,
              "A3a **前提（对照）**：`PlayerAvatar` 路由到 `AvatarDrawer`");
            var bg2 = resAvatar.Node == null ? null : FindDeep(resAvatar.Node, ItemDrawer.NodeBackground);
            var q2 = bg2 == null ? null : bg2.GetComponent<ImageQuad>();
            Tr(q2 != null && q2.Texture != null, "A3b **前提（对照）**：Avatar 的底板也建出来了");
            if (q2 == null || q2.Texture == null) return;

            C(q2.Texture.name, JudgeBaseBg,
              "A3 ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的底板仍是 `" + JudgeBaseBg + "`"
            + "（真包现读）⇒「换图」是 `Title Drawer` 的**例外**，⛔ 不是把共享默认值换掉"
            + "｜🧨 有人把 `DrBackgroundSprite` 直接改成 title card ⇒ 这里红");
        }
        finally
        {
            if (resAvatar.View != null) resAvatar.View.Release();
            Object.DestroyImmediate(avatarRoot);
        }
    }

    // ============================================================ ② `B-276`：Title 的 Army 字号
    static void SectionTitleArmyFont()
    {
        MenuCheck.Section(_sink, "② `B-276` `Title Drawer` 的 `Content/Army` 标称字号 = 35.1（通则 75 见 `Avatar Drawer`）");

        // —— 硬前提：反射句柄 + 阵营名/徽记图（`DrawerArmyName` 的早退闸）——
        Tr(_nominalPx != null,
           "B0 **前提**：`Label.NominalPx()` 的反射句柄取得到（读「标称字号」的唯一口，见文件头 ②）"
         + "｜🧨 有人把它改名/删掉 ⇒ 红（本段整段失效，⛔ 不许静默跳过）");
        Tr(ArmyIcons.Texture(10) != null && !string.IsNullOrEmpty(ArmyIcons.LocalizedName(10)),
           "B0b **前提**：阵营 `CardArmy 10` 有徽记图与本地化名（否则 `DrawerArmyName` 早退、不建 `Army`）");
        if (_nominalPx == null) return;

        float titlePx, avatarPx;

        var root1 = new GameObject("B114W35_TITLE_ARMY");
        ItemDrawResult res1 = default(ItemDrawResult);
        try
        {
            res1 = ItemDrawer.Draw(root1.transform, BoxRoot, SpecTitle(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(10, 11, 12));
            var army = res1.Node == null ? null : FindDeep(res1.Node, ItemDrawer.NodeArmy);
            Tr(army != null, "B1 **前提**：`Title Drawer` 建出了 `Content/Army`");
            var lb = army == null ? null : army.GetComponent<Label>();
            Tr(lb != null, "B1b **前提**：`Army` 那一件上挂的是 `Label`（TMP 后端）");
            titlePx = lb == null ? -1f : NominalPxOf(lb);

            N(titlePx, JudgeTitleArmyPx, 0.6f,
              "B3 ★★ `Title Drawer` 的 `Army` 标称字号 = **35.1 px**（真包 `m_fontSize 35.099998`；方框 ⇒ `K = 1`）"
            + "｜🧨 改回通则 75（`DrawerArmyName` 的默认值）⇒ 这里读到 75 ⇒ 红");
        }
        finally
        {
            if (res1.View != null) res1.View.Release();
            Object.DestroyImmediate(root1);
        }

        // —— 对照组：`Avatar Drawer` 的 Army **必须还是 75** ——
        var root2 = new GameObject("B114W35_AVATAR_ARMY");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            res2 = ItemDrawer.Draw(root2.transform, BoxRoot, SpecAvatar(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(20, 21, 22));
            var army = res2.Node == null ? null : FindDeep(res2.Node, ItemDrawer.NodeArmy);
            Tr(army != null, "B2 **前提（对照）**：`Avatar Drawer` 也建出了 `Content/Army`（它建完会 `SetActive(false)`，节点仍在）");
            var lb = army == null ? null : army.GetComponent<Label>();
            avatarPx = lb == null ? -1f : NominalPxOf(lb);

            N(avatarPx, JudgeAvatarArmyPx, 0.6f,
              "B4 ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Army` 标称字号仍是 **75 px**（真包现读 75.0）"
            + "｜🧨 有人把 `DrArmyFontPx` 直接改成 35.1 ⇒ 这里红");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }

        // —— 结构性反证：两档的标称**必须不等**（谁把它们做成同一个值，「例外」就不存在了）——
        Tr(Mathf.Abs(titlePx - avatarPx) > 30f,
           "B5 ★★ 结构反证：同一批调用里，`Title` 的 Army = " + titlePx.ToString("F2") + " px vs `Avatar` 的 Army = "
         + avatarPx.ToString("F2") + " px（差 > 30px）⇒ **这是按档传的例外值，不是全局改了字号**"
         + "｜🧨 两处都读到同一个数 ⇒ 红（说明「按档传」被写成了一刀切）");
    }

    // ============================================================ ③ `B-277`：Alliance Badge Icon 的固定根
    static void SectionAllianceBadgeIcon()
    {
        MenuCheck.Section(_sink, "③ `B-277` `Alliance Badge Drawer Icon` 的根 = **固定** 165.95×155.74（不跟框）、中心上移 44.63");

        // —— 硬前提：徽章图与档位框（`AllianceBadgeArt.Get` 对未知 id 回落 `Default_Badge`）——
        Tr(AllianceBadgeArt.Default != null,
           "C0 **前提**：徽章默认图 `Alliance_Trophies_Default_v2` 取得到（`Resources/Art/ui_menu/`）");

        // 手算期望（⛔ 不读生产常量）：根宽高来自 prefab；徽章/档位框都是 1024² 方图 + `m_PreserveAspect 1`
        // ⇒ 画出来 = min(W,H) 的正方形。
        float px = Mathf.Min(JudgeBadgeW, JudgeBadgeH);

        var small = DrawBadgeIcon(BoxBadgeSmall);
        var big = DrawBadgeIcon(BoxBadgeBig);
        try
        {
            Tr(small.Ok, "C1 **前提**：小框（" + JudgeBadgeW.ToString("F2") + "×" + JudgeBadgeH.ToString("F2")
                       + "，= 根的尺寸）里 `Frame` 画出来了");
            Tr(big.Ok, "C1b **前提**：大框（400×400）里 `Frame` 也画出来了");

            N(small.W, px, 0.6f,
              "C2 ★ 小框下 `Frame` 画成 " + small.W.ToString("F2") + " px（= min(165.9539,155.7357) = 155.74，"
            + "方图按 `m_PreserveAspect 1` 内接）");

            Tr(Mathf.Abs(small.W - big.W) < 0.6f && Mathf.Abs(small.H - big.H) < 0.6f,
               "C3 ★★ **核心**：框从 " + JudgeBadgeW.ToString("F0") + "×" + JudgeBadgeH.ToString("F0")
             + " 换成 400×400，画出来仍是 " + big.W.ToString("F2") + "×" + big.H.ToString("F2")
             + "（小框那份 " + small.W.ToString("F2") + "×" + small.H.ToString("F2") + "）⇒ **原版这一档根本不跟框走**"
             + "｜🧨 用 `frameR = box`（`B114_WC` 的原写法）⇒ 大框画成 400 ⇒ 红");

            N(big.CX, BoxBadgeBig.CX, 0.6f,
              "C4 ★ 水平：根**居中**在框心（锚 `(0.5,0.5)` · `m_AnchoredPosition.x = 0`）");
            N(big.CY, BoxBadgeBig.CY - JudgeBadgeAnchorY, 0.6f,
              "C4b ★ 垂直：根心 = 框心 **减** 44.6322（prefab `m_AnchoredPosition.y = +44.6322` 是 Unity 的向上为正）"
            + "｜🧨 把那个偏移漏掉（或加号用反）⇒ 这里差 44.63 ⇒ 红");
        }
        finally
        {
            small.Release();
            big.Release();
        }

        // —— 对照组：**同一族的另一份 prefab**（基础档 `Alliance Badge Drawer`）**必须跟着框变** ——
        var baseA = DrawBadgeBase(BoxRoot);
        var baseB = DrawBadgeBase(new PxRect(0f, 0f, 356.4f, 540f));
        try
        {
            Tr(baseA.Ok && baseB.Ok, "C5a **前提（对照）**：基础档在两个框里都画出来了");
            Tr(baseA.Ok && baseB.Ok && Mathf.Abs(baseA.W - baseB.W * 2f) < 1.5f,
               "C5 ★★ **灭自证（同族对照）**：基础档 `Alliance Badge Drawer` 在 712.8×1080 与 356.4×540 两个框里"
             + " 画出来是 " + baseA.W.ToString("F2") + " vs " + baseB.W.ToString("F2")
             + "（**恰好 2:1** ⇒ 它跟着框走）⇒「固定尺寸」是 Icon 那一档的**例外**，不是把全族都定死"
             + "｜🧨 有人把 `AllianceBadgeIconRootOf` 那套套到基础档上 ⇒ 这里红");
        }
        finally
        {
            baseA.Release();
            baseB.Release();
        }
    }

    // ============================================================ 夹具 / 量具
    struct QuadProbe
    {
        public bool Ok;                 // 画出来了、量到了
        public float W, H, CX, CY;
        Transform _root; ItemDrawResult _res;
        public void Release()
        {
            if (_res.View != null) _res.View.Release();
            if (_root != null) Object.DestroyImmediate(_root.gameObject);
            _root = null;
        }
        public void Take(float w, float h, float cx, float cy) { Ok = true; W = w; H = h; CX = cx; CY = cy; }
        public void Keep(Transform root, ItemDrawResult res) { _root = root; _res = res; }
    }

    /// <summary>画一次 Icon 档的徽章抽屉（`DrawerOverride.Icon`），量 `Frame` 那一块。</summary>
    static QuadProbe DrawBadgeIcon(PxRect box)
    {
        var p = new QuadProbe();
        var root = new GameObject("B114W35_BADGE_ICON");
        ItemDrawResult res = ItemDrawer.Draw(root.transform, box, SpecBadge(), 1, DrawerOverride.Icon,
                                             ItemDrawerStyle.Default(30, 31, 32));
        if (res.Node != null) Probe(ref p, res.Node, ItemDrawer.NodeTrophyFrame);
        p.Keep(root.transform, res);
        return p;
    }

    /// <summary>画一次**基础档**的徽章抽屉（`DrawerOverride.Default`），量 `Frame`。</summary>
    static QuadProbe DrawBadgeBase(PxRect box)
    {
        var p = new QuadProbe();
        var root = new GameObject("B114W35_BADGE_BASE");
        ItemDrawResult res = ItemDrawer.Draw(root.transform, box, SpecBadge(), 1, DrawerOverride.Default,
                                             ItemDrawerStyle.Default(40, 41, 42));
        if (res.Node != null) Probe(ref p, res.Node, ItemDrawer.NodeTrophyFrame);
        p.Keep(root.transform, res);
        return p;
    }

    /// <summary>量某一层画出来的矩形（⚠️ `QuadProbe` 是 struct ⇒ 必须 `ref`）。</summary>
    static void Probe(ref QuadProbe p, Transform node, string layer)
    {
        var t = FindDeep(node, layer);
        if (t == null) return;
        var q = t.GetComponent<ImageQuad>();
        if (q == null || q.Texture == null) return;
        var r = MenuDraw.QuadRectPx(q);
        p.Take(r.W, r.H, r.CX, r.CY);
    }

    static ItemSpec SpecTitle()
    {
        return new ItemSpec
        {
            Id = "B114W35_Title", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
            CardArmy = 10, Label = "B114W35", Title = "B114W35 Title",
        };
    }

    static ItemSpec SpecAvatar()
    {
        return new ItemSpec
        {
            Id = "B114W35_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
            CardArmy = 10, Art = "Player_Profile_Border", Label = "B114W35",
        };
    }

    static ItemSpec SpecBadge()
    {
        return new ItemSpec
        {
            Id = "B114W35_Badge", Type = "AllianceTrophyData", Kind = ItemKind.Generic,
            BadgeId = "UM_Forge_Badge", Label = "B114W35",
        };
    }

    /// <summary>`Label.NominalPx()`（私有）——「调用方要的那个字号，折算成画布 px，**不含自适应结果**」。
    /// 判据与「为什么只能反射」→ `Battle/Label.cs:1417-1445`；同族先例 = `Label.BaseFld` 反射读 `m_fontSizeBase`。
    /// ⚠️ 取不到 / 类型不符 ⇒ 返回 **-1**（调用方那条 B0 前提会先红，⛔ 不许静默跳过）。</summary>
    static readonly MethodInfo _nominalPx = typeof(Label).GetMethod(
        "NominalPx", BindingFlags.Instance | BindingFlags.NonPublic, null, System.Type.EmptyTypes, null);

    static float NominalPxOf(Label lb)
    {
        if (lb == null || _nominalPx == null) return -1f;
        object v = _nominalPx.Invoke(lb, null);
        return v is float ? (float)v : -1f;
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
}
