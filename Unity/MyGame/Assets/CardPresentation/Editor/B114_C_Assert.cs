// B114_C_Assert.cs — 第一百一十四会话 · 第 1 波写手 `W-C`（2026-11-01）· 账 `B-223` 的断言
//
// ============================ 它盯的是什么 ============================
//  **Icon 变体几何 7 档**：原版有 7 族抽屉是「**同一个类 + 两套 prefab**」——
//    基础档 `Avatar Drawer` / Icon 档 `Icon Avatar Drawer Variant` ……（`ItemTypeSets` 的 Icon(10) 列
//    写的**仍是主档类名**）。`Dispatch` 早就把 `DrawerOverride ov` 收进来了，但 26 个 `case` 里
//    原先**只有 `GenericArmy` 把 `ov` 往下传** ⇒ 这 7 档永远按基础档几何画。
//  本件逐档断 **「画出来的尺寸」+「该不建的层有没有多建」**，⛔ 不断「方法被调过」。
//
//  判据（逐张 prefab 现读 · `d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
//    读 `!u!224` RectTransform 的 anchorMin/anchorMax/sizeDelta + `!u!114` 的 `ItemDrawerComponents`
//    （`m_Script.guid = 26d6053ad69df806e09ae9cd86daa71f`）五个槽 + 根上的 `AspectRatioFitter`。
//    逐张数字与「改了什么」→ `资料/完全复刻/报告/B114_WC_B223_Icon几何.md`。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · 每一档的**期望矩形都是独立算的常量**（= prefab 设计 px，⛔ 不是读我们代码里的常量现算）：
//      Avatar   `image` 过扫 (-307.8,-210.6)-(1387.8,1490.4)（锚 -0.285..1.285 × -0.195..1.38 × 根 1080²）
//      Container `image` 满帧 ×1.1 → (-54,-54,1134,1134)
//      Currency  `image` 满帧 (0,0,1080,1080)
//      ExpPassPt `image` 满帧 (0,0,1080,1080)
//      RandomCard `Image` 满帧 (0.4,0)-(712.4,1080)；`Random Image` (71.3,108)-(641.5,972)
//      Wildcard  `image` 满帧 ×0.94 → (32.4,32.4)-(1047.6,1047.6)（⚠️ `B113X10` 报告写的 -32.4..1112.4 是**除反了**：那是「要乘 1/0.94 才对」的框；
//                prefab 是 `m_LocalScale 0.94` 绕 pivot(0.5,0.5)，只能**缩小** ⇒ 1015.2² 居中）
//      Alliance  `Frame` / `Badge` 都 = **固定根** 165.9539×155.7357（无 ARF；框只决定中心，再上移 44.6322）
//                🔴 2026-11-01 `B114_W38` 就地订正：本行原来写「都 = 调用框」—— `B-277`（`B114_W35`）之后不成立。
//    ⇒ 把 `IconVariant` 那一支删掉（回到基础档几何）⇒ 这 7 组**同时**红。
//  · 「显隐」那几条：把 `DrawerNameQuantity(hasName:false, hasQuantity:false)` 改回 true ⇒ `Label` 又建出来 ⇒ 红。
//  · H 段（灭自证）：`DrawerOverride.Default` 下**仍然是基础档几何**（Container 帧 712.8 宽 / Wildcard 居中方形）
//    ⇒ 若有人把 Icon 几何直接写进基础档（不分支），H 段红。它与前面那 7 组是**结构上不可能同时满足**的一对。
//
// ============================ 怎么接（⚠️ 只由调度台/本波写手改宿主）============================
//  唯一入口 = `B114_C_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行**同一物理行、第一个 `//` 之前**追加：
//      B114_C_Assert.Run(); _sink.Pass += B114_C_Assert.Pass; _sink.Fail += B114_C_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 期望值按「根 1080² / 712.8×1080」的**方框**夹具算（`FrameOf` 的 `K = 1`、`OX = OY = 0`
//    ⇒ `fr.Map` 出来的就是设计 px 本身）。非方框调用下双方还会再差一层 —— 原版 ARF 在窄容器里
//    是溢出还是按宽缩**还没读**（`Shell/ItemDrawerLayout.cs` 文件头 ① 那条 TODO），本件不覆盖。
//  · 14 档 `Alliance Badge Drawer Icon` 的夹具框取**非方框** (100,50)-(266,205.74)：那正好能证明
//    「这一档**不做**帧换算」（基础档会在框里内接一个 712.8×1080 的竖版帧）。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-C`：账 `B-223` —— Icon 变体几何 7 档（`Dispatch` 的 `ov == Icon` 分流）。</summary>
public static class B114_C_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114C] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>夹具框：根 1080² 的那几族用方框 ⇒ `FrameOf` 的 `K = 1`、`OX = OY = 0`。
    /// ⚠️ 仓里 `PxRect` 是**左上原点 · y 向下**（与 prefab 的 `anchoredPosition` 反号），下面所有常量都是这一口径。</summary>
    static readonly PxRect Box1080 = new PxRect(0f, 0f, 1080f, 1080f);
    /// <summary>夹具框：`Icon Random Card Drawer Variant` 的根就是 712.8×1080（与基础档同形）。</summary>
    static readonly PxRect BoxRandom = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>夹具框：Alliance 那一档**故意用非方框**（证明它不做帧换算）。</summary>
    static readonly PxRect BoxAlliance = new PxRect(100f, 50f, 266f, 205.74f);
    /// <summary>`Alliance Badge Drawer Icon` 的**根矩形**（实测期望，⛔ 不是读 `ItemDrawer.AllianceBadgeIconRootOf`）。
    /// 三个数是 **prefab 字段原文**：`m_SizeDelta 165.9539 × 155.7357` · `m_AnchoredPosition.y 44.6322`（Unity 向上为正，
    /// 本仓 y 向下 ⇒ **减**）· 锚 `(0.5,0.5)` ⇒ 中心 = `BoxAlliance` 中心 `(183, 127.87)` 再上移 ⇒ `(183, 83.2378)`。
    /// 🔴 **2026-11-01 `B114_W38` 就地订正**：本件原来（`B114_WC` 那一版）写的是「`Frame`/`Badge` = 整个调用框」——
    /// 那在 `B-277`（`B114_W35`）之后**已经不成立**：原版这一档的根**没有 ARF**、锚死 `(0.5,0.5)`、
    /// `sizeDelta` 固定 ⇒ 框只决定它的中心在哪。</summary>
    static readonly PxRect BoxAllianceIconRoot = new PxRect(100.02305f, 5.36995f, 265.97695f, 161.10565f);

    // ---- 每档的图名（都在 `Resources/`，先断「取得到」再断几何 ⇒ 图没了是**红**、不是静默跳过）----
    const string ArtAvatar = "Avatar_AM_Attilan Rough Rider";   // `Resources/Art/avatars/`
    const string ArtBaseCard = "40k_Rewards_icon_base_card";   // `Resources/Art/ui_menu/`
    const string ArtRoundBg = "UI_Button_Round_background";    // `Resources/Art/ui_menu/`
    const string ArtWildcard = "40k_general_wildcard_common";  // `Resources/Art/ui_deck/`
    const string BadgeId = "UM_Forge_Badge";                   // `AllianceBadgeArt.Entries`

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_C（B-223）：Icon 变体几何 7 档（原版「同一类两套 prefab」）");

        SectionAvatar();
        SectionContainer();
        SectionCurrency();
        SectionExpansionPassPoint();
        SectionRandomCard();
        SectionWildcard();
        SectionAllianceBadge();
        SectionBaseStillBase();

        Debug.Log(_sink.P + "B114C 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ #2 Icon Avatar Drawer Variant
    static void SectionAvatar()
    {
        MenuCheck.Section(_sink, "#2 `Icon Avatar Drawer Variant`：头像过扫 1.57× 根宽 · 无背景/名字/数量");

        var tex = CardArt.Cosmetics(ArtAvatar);
        Tr(tex != null, "A0 **前提**：头像图 `" + ArtAvatar + "` 取得到（`CardArt.Cosmetics`）"
                      + "｜🧨 删掉 `Resources/Art/avatars/" + ArtAvatar + ".png` ⇒ 红");
        if (tex == null) return;

        var root = new GameObject("B114C_AVATAR_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic, Art = ArtAvatar, Label = "B114C" };
            res = DrawIcon(root.transform, Box1080, spec);

            Tr(res.Node != null, "A1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAvatar, "A1b **前提**：`ov = Icon` 仍路由到 `AvatarDrawer`（`ItemTypeSets` 的 Icon(10) 列）");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "A2 **前提**：`Content` 建出来了（Icon 变体的 `Content` 锚 (0,0)-(1,1) 铺满 1080² 根；主图挂在它下面）");

            // 主图：`avatar` 锚 -0.285..1.285 × -0.195..1.38 ⇒ 设计 px (-307.8,-210.6)-(1387.8,1490.4)
            var img = FindDeep(res.Node, "Image");
            Tr(img != null, "A3 **前提**：主图节点建出来了");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(-307.8f, -210.6f, 1387.8f, 1490.4f)), 0.6f,
                       "A3 ★★ 主图框 = 过扫 (-307.8,-210.6)-(1387.8,1490.4)（= 1.57× 根宽的 `avatar` 节点）"
                     + "｜🧨 删掉 `IconVariant` 的 Avatar 支 ⇒ 回到基础档 860×730 那一格 ⇒ 红");

            // 显隐：`background`/`label`/`quantity` 槽全 null；`Army` 节点 `m_IsActive = 0`
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null,
               "A4 ★ 不建 `Background`（Icon 变体的 `background` 槽是 null —— 基础档才有那张 `40k_Rewards_icon_base card`）"
             + "｜🧨 让 Icon 支仍调 `DrawerBackground` ⇒ 红");
            Tr(FindDeep(res.Node, "Label") == null,
               "A5 ★ 不建 `Label`（原版 Icon 变体的 `label` 槽 = `{fileID:0}`，15/15 都是 null）"
             + "｜🧨 `DrawerNameQuantity(...,hasName:true,...)` 改回来 ⇒ 红");
            Tr(FindDeep(res.Node, ItemDrawer.NodeArmy) == null,
               "A6 ★ 不画阵营名（`Army` 节点在 Icon 变体里 `m_IsActive = 0`，原版 `AvatarDrawer.Draw` 只写 `text`、不 `SetActive`）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ #4 Icon Container Drawer Variant
    static void SectionContainer()
    {
        MenuCheck.Section(_sink, "#4 `Icon Container Drawer Variant`：1080² 根 · 满帧 ×1.1 · 无徽记/名字/数量");

        var tex = CardArt.MenuUi(ArtBaseCard);
        Tr(tex != null, "B0 **前提**：容器图 `" + ArtBaseCard + "` 取得到");
        if (tex == null) return;

        var root = new GameObject("B114C_CONTAINER_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_Container", Type = "Everguild.LiveOps.ShopContainer", Kind = ItemKind.Generic, Art = ArtBaseCard, CardArmy = 10, Label = "B114C" };
            res = DrawIcon(root.transform, Box1080, spec);

            Tr(res.Node != null, "B1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerContainer, "B1b **前提**：路由到 `ContainerDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "B2 **前提**：`Content` 建出来了（Icon 变体的 `Content` 铺满 1080² 根；基础档是 712×1080 的竖版帧）");

            var img = FindDeep(res.Node, "Image");
            Tr(img != null, "B3 **前提**：主图建出来了");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(-54f, -54f, 1134f, 1134f)), 0.6f,
                       "B3 ★★ 主图框 = 满帧 ×1.1 → (-54,-54)-(1134,1134)（prefab `m_LocalScale = 1.1`）"
                     + "｜🧨 比例改回 1.2 / 帧改回 712.8 宽 ⇒ 红");

            Tr(FindDeep(res.Node, ItemDrawer.NodeArmyIcon) == null,
               "B4 ★ 不建 `Army Icon`（Icon 变体的 `armyIcon` 槽是 null；`cardArmy = 10` 也照旧不建）");
            Tr(FindDeep(res.Node, "Label") == null, "B5 ★ 不建 `Label`（`label`/`quantity` 槽都是 null）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null, "B6 ★ 不建 `Background`（`background` 槽是 null）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ #5 Icon Currency Drawer Variant
    static void SectionCurrency()
    {
        MenuCheck.Section(_sink, "#5 `Icon Currency Drawer Variant`：整格只有一张满帧主图（无底板/名条/小图标/数量）");

        var tex = CardArt.MenuUi(ArtBaseCard);
        Tr(tex != null, "C0 **前提**：货币主图 `" + ArtBaseCard + "` 取得到");
        if (tex == null) return;

        var root = new GameObject("B114C_CURRENCY_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_Currency", Type = "Currency", Kind = ItemKind.Generic, Art = ArtBaseCard, Label = "B114C" };
            res = DrawIcon(root.transform, Box1080, spec);

            Tr(res.Node != null, "C1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerCurrency, "C1b **前提**：路由到 `CurrencyDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "C2 **前提**：`Content` 建出来了（`ItemTypeSets` 的 Currency Icon 档写的就是这个类；Content 铺满 1080² 根）");

            var img = FindDeep(res.Node, "Image");
            Tr(img != null, "C3 **前提**：主图建出来了");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(0f, 0f, 1080f, 1080f)), 0.6f,
                       "C3 ★★ 主图框 = 满帧 (0,0)-(1080,1080)（`m_LocalScale = 1.0`）"
                     + "｜🧨 删掉 Currency 的 Icon 支 ⇒ 回到基础档 (604,0)-(1316,864)×0.9 ⇒ 红");

            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null, "C4 ★ 不建 `Background`（`background` 槽 null）");
            Tr(FindDeep(res.Node, "Label") == null, "C5 ★ 不建 `Label`（名条行 + 小图标 + 数量三层都在它下面）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeCurrencySmallIcon) == null, "C6 ★ 不建小图标（`icon` 槽 null）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeQuantity) == null, "C7 ★ 不建 `Quantity`（`quantity` 槽 null）");

            // 自检读口：Icon 档只有主图那一格
            var lc = ItemDrawer.LastCurrency;
            Tr(lc.MainImage && !lc.Background && !lc.Nametag && !lc.SmallIcon && !lc.Quantity,
               "C8 ★ `LastCurrency` = 只有 `MainImage`（其余四格 false）—— 读口别停在基础档那份");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ #6 Icon Expansion Pass Points
    static void SectionExpansionPassPoint()
    {
        MenuCheck.Section(_sink, "#6 `Icon Expansion Pass Points Drawer Variant`：1080² 根 · 满帧 · 无阵营名/名字/数量");

        var tex = ArmyIcons.Texture(10);
        Tr(tex != null, "D0 **前提**：阵营 10 的图标取得到（`ArmyIcons.Texture(10)` = `"
                      + (ArmyIcons.IconName(10) ?? "<null>") + "`）");
        if (tex == null) return;

        var root = new GameObject("B114C_EXPPASS_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_ExpPass", Type = "Everguild.LiveOps.ExpansionPassPoints", Kind = ItemKind.Generic, CardArmy = 10, Label = "B114C" };
            res = DrawIcon(root.transform, Box1080, spec);

            Tr(res.Node != null, "D1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerExpansionPassPoint, "D1b **前提**：路由到 `ExpansionPassPointDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "D2 **前提**：`Content` 建出来了（这一族 Icon 档**没有独立类**，分流只能挂在本类上；Content 铺满 1080² 根）");

            var img = FindDeep(res.Node, "Image");
            Tr(img != null, "D3 **前提**：主图建出来了");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(0f, 0f, 1080f, 1080f)), 0.6f,
                       "D3 ★★ 主图框 = 满帧 (0,0)-(1080,1080)（原版 Icon 变体铺满；基础档是 (0.4,216)-(712.4,648) 的中间横带）"
                     + "｜🧨 删掉 ExpPassPt 的 Icon 支 ⇒ 回到横带 ⇒ 红");

            Tr(FindDeep(res.Node, ItemDrawer.NodeArmy) == null, "D4 ★ 不建阵营名（Icon 变体 `armyText` 槽 null）");
            Tr(FindDeep(res.Node, "Label") == null, "D5 ★ 不建 `Label`（`label`/`quantity` 槽都是 null）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null, "D6 ★ 不建 `Background`（`background` 槽 null）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ #10 Icon Random Card
    static void SectionRandomCard()
    {
        MenuCheck.Section(_sink, "#10 `Icon Random Card Drawer Variant`：`Image` 满帧 · `Random Image` 570.2×864 · 无阵营名/名字/数量");

        var baseTex = CardArt.MenuUi(ArtBaseCard);
        var roundTex = CardArt.MenuUi(ArtRoundBg);
        Tr(baseTex != null && roundTex != null,
           "E0 **前提**：`" + ArtBaseCard + "` 与 `" + ArtRoundBg + "` 都取得到");
        if (baseTex == null || roundTex == null) return;

        // ---- E1~E5：中立档（`cardArmy == 0`）⇒ `Random Image` ----
        var root = new GameObject("B114C_RANDOMCARD_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_RandomCard", Type = "DropTableItem", Kind = ItemKind.Generic, Rarity = 0, CardArmy = 0, Label = "B114C" };
            res = DrawIcon(root.transform, BoxRandom, spec);

            Tr(res.Node != null, "E1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerRandomCard, "E1b **前提**：路由到 `RandomCardDrawer`");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "E2 **前提**：`Content` 建出来了（这一族根 712.8×1080，Icon 变体与基础档**同形**）");

            var randomImage = FindDeep(res.Node, "Random Image");
            Tr(randomImage != null, "E3 **前提**：中立档的 `Random Image` 建出来了");
            if (randomImage != null)
                RectIs(QuadOf(randomImage), FitBox(roundTex, new PxRect(71.3f, 108f, 641.5f, 972f)), 0.6f,
                       "E3 ★★ `Random Image` 框 = (71.3,108)-(641.5,972) = 570.2×864（prefab 锚 (0.1,0.1)-(0.9,0.9)）"
                     + "｜🧨 用基础档那格 (0.4,108)-(712.4,648) ⇒ 红");

            var qIcon = FindDeep(res.Node, ItemDrawer.NodeIcon);
            if (qIcon != null)
                RectIs(QuadOf(qIcon), FitBox(TexOf(qIcon), InsetAnchors(new PxRect(71.3f, 108f, 641.5f, 972f), 0.2f, 0.3f, 0.8f, 0.7f)), 0.6f,
                       "E4 ★ `Random Image/Icon`（问号）按 **Icon 变体**的锚 (0.2,0.3)-(0.8,0.7) 内接"
                     + "｜🧨 照基础档 (0.2,0.2)-(0.8,0.8) 画 ⇒ 红");

            Tr(FindDeep(res.Node, "Label") == null, "E5 ★ 不建 `Label`（`label`/`quantity` 槽都是 null）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeArmy) == null, "E5b ★ 不建阵营名（Icon 变体没有 `Army` 节点）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }

        // ---- E6~E8：非中立档 ⇒ `Image` 满帧（基础档是中间横带）----
        var root2 = new GameObject("B114C_RANDOMCARD_ICON_ARMY");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_RandomCardArmy", Type = "DropTableItem", Kind = ItemKind.Generic, Rarity = 0, CardArmy = 10, Label = "B114C" };
            res2 = DrawIcon(root2.transform, BoxRandom, spec);
            Tr(res2.Node != null, "E6 **前提**：非中立档的抽屉根建出来了");
            if (res2.Node == null) return;

            var armyTex = ArmyIcons.Texture(10);
            var img = FindDeep(res2.Node, "Image");
            Tr(img != null, "E7 **前提**：非中立档的 `Image`（阵营徽记）建出来了");
            if (img != null && armyTex != null)
                RectIs(QuadOf(img), FitBox(armyTex, new PxRect(0.4f, 0f, 712.4f, 1080f)), 0.6f,
                       "E7 ★★ `Image` 满帧 (0.4,0)-(712.4,1080)（原版 Icon 变体的 `image` 槽 = 满帧；基础档只占中间横带）"
                     + "｜🧨 沿用基础档 `slotR` ⇒ 红");
            Tr(FindDeep(res2.Node, "Random Image") == null, "E8 ★ `cardArmy != 0` ⇒ 不建 `Random Image`（与基础档同一对互斥）");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }
    }

    // ============================================================ #13 Icon Wildcard
    static void SectionWildcard()
    {
        MenuCheck.Section(_sink, "#13 `Icon Wildcard Drawer Variant`：满框 ×0.94（不是居中方形 0.7）");

        var tex = CardArt.MenuUi(ArtWildcard);
        Tr(tex != null, "F0 **前提**：野牌图 `" + ArtWildcard + "` 取得到");
        if (tex == null) return;

        var root = new GameObject("B114C_WILDCARD_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_Wildcard", Type = "Wildcard", Kind = ItemKind.Wildcard, Rarity = 1, Label = "B114C" };
            res = DrawIcon(root.transform, Box1080, spec);

            Tr(res.Node != null, "F1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerWildcardIcon, "F1b **前提**：`ov = Icon` 的野牌路由到 `WildcardIconDrawer`");
            if (res.Node == null) return;

            var img = FindDeep(res.Node, ItemDrawer.NodeIcon);
            Tr(img != null, "F2 **前提**：主图（`Icon` 节点）建出来了");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(32.4f, 32.4f, 1047.6f, 1047.6f)), 0.6f,
                       "F2 ★★ 主图框 = 满框 ×0.94 → (32.4,32.4)-(1047.6,1047.6)（prefab `m_LocalScale = 0.94` 绕 pivot(0.5,0.5) 缩 ⇒ 1015.2² 居中）"
                     + "｜🧨 仍走 `Square(box, IconFill=0.7)` ⇒ 得 (162,162)-(918,918) ⇒ 红"
                     + "；🧨 把 0.94 当「除以 0.94」（-32.4..1112.4，= `B113X10` 报告那组反了的数）⇒ 也红");

            Tr(FindDeep(res.Node, "Label") == null, "F3 ★ 不建 `Label`（`label`/`quantity` 槽都是 null）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null, "F4 ★ 不建 `Background`（`background` 槽 null）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ #14 Alliance Badge Drawer Icon
    static void SectionAllianceBadge()
    {
        MenuCheck.Section(_sink, "#14 `Alliance Badge Drawer Icon`：**无 ARF** ⇒ `Frame`/`Badge` 都 = 调用框 · 无 Content/阵营名/名字/数量");

        var frameTex = AllianceTrophyArt.GetTierFrame(1);
        var badgeTex = AllianceBadgeArt.Get(BadgeId);
        Tr(frameTex != null && badgeTex != null,
           "G0 **前提**：档位框与徽章图都取得到（`" + AllianceTrophyArt.TierFrameName(1) + "` / `" + BadgeId + "`）");
        if (frameTex == null || badgeTex == null) return;

        var root = new GameObject("B114C_ALLIANCE_ICON");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec { Id = "B114C_Alliance", Type = "AllianceTrophyData", Kind = ItemKind.Generic, BadgeId = BadgeId, CardArmy = 10, Label = "B114C" };
            res = DrawIcon(root.transform, BoxAlliance, spec);

            Tr(res.Node != null, "G1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAllianceBadge, "G1b **前提**：路由到 `AllianceBadgeDrawer`");
            if (res.Node == null) return;

            var frame = FindDeep(res.Node, ItemDrawer.NodeTrophyFrame);
            Tr(frame != null, "G2 **前提**：`Frame`（档位框）建出来了");
            if (frame != null)
                RectIs(QuadOf(frame), FitBox(frameTex, BoxAllianceIconRoot), 0.6f,
                       "G2 ★★ `Frame` = **固定根** (100.02,5.37)-(265.98,161.11)（= 框心再上移 44.6322；"
                       + "原版这一档**根上没有 ARF**、`sizeDelta 165.9539×155.7357` ⇒ 框多大都不变）"
                     + "｜🧨 仍按基础档 712.8×1080 的竖版帧内接、或退回 `frameR = box`（跟框画）⇒ 红");

            // 🔴 2026-11-01 `B114_W38`（`B-282②`）：Icon 档那颗徽章**原版就叫 `Badge`**
            //    （prefab `&224709867586078768`）；基础档同位置那颗才叫 `Trophy Icon`。
            var badge = FindDeep(res.Node, ItemDrawer.NodeIconBadge);
            Tr(badge != null, "G3 **前提**：`Badge`（徽章本体，Icon 档的节点名）建出来了"
                             + "｜🧨 沿用基础档的 `Trophy Icon` ⇒ 这里找不到 ⇒ 红");
            if (badge == null) return;
            Tr(FindDeep(res.Node, ItemDrawer.NodeTrophyIcon) == null,
               "G3b ★ Icon 档根下**不该再有** `Trophy Icon`（那是基础档的节点名；两套 prefab 各自一个名字）"
             + "｜🧨 只是「多建一颗」或改名没做干净 ⇒ 红");
            if (badge != null)
                RectIs(QuadOf(badge), FitBox(badgeTex, BoxAllianceIconRoot), 0.6f,
                       "G3c ★★ `Badge` 与 `Frame` **同框**（原版两者都是锚 (0,0)-(1,1) 满根 ⇒ 都按固定根内接）");

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) == null,
               "G4 ★ 不建 `Content`（这一档 prefab 里 `Frame`/`Badge` 是**根的直接子件**，没有 `Content` 层）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) == null, "G5 ★ 不建 `Background`（`background` 槽 null）");
            Tr(FindDeep(res.Node, "Label") == null, "G6 ★ 不建 `Label`（`label`/`quantity` 槽都是 null）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ H · 灭自证：基础档仍是基础档
    static void SectionBaseStillBase()
    {
        MenuCheck.Section(_sink, "H 灭自证：`DrawerOverride.Default` 下**仍是基础档几何**（本波没把 Icon 几何写进基础档）");

        var tex = CardArt.MenuUi(ArtBaseCard);
        if (tex == null) { Tr(false, "H0 **前提**：容器图取得到"); return; }

        var root = new GameObject("B114C_BASE_CONTAINER");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            var spec = new ItemSpec { Id = "B114C_ContainerBase", Type = "Everguild.LiveOps.ShopContainer", Kind = ItemKind.Generic, Art = ArtBaseCard, CardArmy = 10, Label = "B114C" };
            res = ItemDrawer.Draw(root.transform, Box1080, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "H1 **前提**：基础档抽屉根建出来了");
            if (res.Node == null) return;

            // 基础档 `Container Drawer`：根 712.8×1080 ⇒ 在 1080² 的框里按 min 内接（K = 1、OX = 183.6、OY = 0）
            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null, "H2 **前提**：基础档 `Content` 建出来了");

            var img = FindDeep(res.Node, "Image");
            if (img != null)
                RectIs(QuadOf(img), FitBox(tex, new PxRect(112.8f, -108f, 967.2f, 1188f)), 0.6f,
                       "H2 ★★ 基础档主图 = 712×1080 帧铺满再 ×1.2（帧 = (184,0)-(896,1080) ⇒ 视觉 (112.8,-108)-(967.2,1188)）"
                     + "｜🧨 把 Icon 那套（1080² ×1.1）直接写进基础档 / 把比例改成 1.1 ⇒ 红");

            Tr(FindDeep(res.Node, "Label") != null,
               "H4 ★ 基础档**仍然**建 `Label`（`DrawerNameQuantity(hasName:true, hasQuantity:true)`）—— "
             + "「Icon 档不画名字/数量」是**分支**，不是把基础档也关掉");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 夹具 / 量具
    /// <summary>统一入口：**Icon 档** + 默认版式（`QuantityPx = 34 / NamePx = 26` 都非 0
    /// ⇒ 下面那些「不建 Label / 不建 Quantity」才有意义；设成 0 就是**静默跳过**）。</summary>
    static ItemDrawResult DrawIcon(Transform parent, PxRect box, ItemSpec spec)
    {
        var st = ItemDrawerStyle.Default(10, 11, 12);
        return ItemDrawer.Draw(parent, box, spec, 1, DrawerOverride.Icon, st);
    }

    /// <summary>`MenuDraw.Rect(..., keepAspect: true)` 之后**画出来**的矩形（= uGUI `PreserveSpriteAspectRatio`）。
    /// 直接用生产代码那条 `CardbackFace.Fit` ⇒ 期望值只差「喂进去的框」，而那个框是**独立的 prefab 常量**。</summary>
    static PxRect FitBox(Texture2D tex, PxRect box)
    {
        float dw, dh, dx, dyUp;
        if (tex == null || !CardbackFace.Fit(tex, box.W, box.H, out dw, out dh, out dx, out dyUp)) return box;
        float cx = box.CX + dx, cy = box.CY - dyUp;     // `Fit` 的 dy 是**向上为正**；本仓 `PxRect` 是 y 向下
        return new PxRect(cx - dw * 0.5f, cy - dh * 0.5f, cx + dw * 0.5f, cy + dh * 0.5f);
    }

    /// <summary>把父框按 uGUI 锚点内接（`ax1..ax2` 是 0..1 的比例）。</summary>
    static PxRect InsetAnchors(PxRect parent, float ax1, float ay1, float ax2, float ay2)
    {
        return new PxRect(parent.x1 + parent.W * ax1, parent.y1 + parent.H * ay1,
                          parent.x1 + parent.W * ax2, parent.y1 + parent.H * ay2);
    }

    /// <summary>主图那类件是 `ImageQuad` ⇒ 走 `MenuDraw.QuadRectPx` 反推（设计 px · 左上原点 · y 向下）。
    /// ⚠️ `Content` 这类**纯容器**（`MenuDraw.Node`，没有 `ImageQuad`）**不在这里量** —— 本件只量「真画出来的图」，
    /// 帧形一律由主图/子件的矩形反推（少一个量具就少一处与生产代码分家的机会）。</summary>
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

    static void RectIs(PxRect got, PxRect want, float tol, string msg)
    {
        Tr(Mathf.Abs(got.x1 - want.x1) <= tol && Mathf.Abs(got.y1 - want.y1) <= tol
           && Mathf.Abs(got.x2 - want.x2) <= tol && Mathf.Abs(got.y2 - want.y2) <= tol,
           msg + "（实测 " + got.x1.ToString("F1") + "," + got.y1.ToString("F1") + " - "
           + got.x2.ToString("F1") + "," + got.y2.ToString("F1") + "；期望 "
           + want.x1.ToString("F1") + "," + want.y1.ToString("F1") + " - "
           + want.x2.ToString("F1") + "," + want.y2.ToString("F1") + "）");
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
