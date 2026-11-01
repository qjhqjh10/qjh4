// B114_W32_Assert.cs — 第一百一十四会话 · 写手 `W-32`（2026-11-01）· 账 `B-260`（ARF 非方框）+ `B-263`（两档变体几何）
//
// ============================ 它盯的是什么 ============================
//  ① **`B-260`：原版 `AspectRatioFitter`（`m_AspectMode 2` = HeightControlsWidth）在【框不是根那比例】时怎么算。**
//     UGUI 那一档只有一句 `SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio)`，
//     **完全不看父框的宽**（判据 = `D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/Resources/PackageManager/
//     BuiltInPackages/com.unity.ugui/Runtime/UGUI/UI/Core/Layout/AspectRatioFitter.cs:151-156`）
//     ⇒ 框比 `高×比例` 更窄时，根是**横向溢出**（左右各一半），**不是**按宽缩。
//     ⇒ 原版口径 `K = 框高/根高`（`RootFit.HeightControlsWidth`）；本库默认 `K = min(框高/根高, 框宽/根宽)`
//       （`RootFit.Contain` = 等比内接，`m_AspectMode 3` FitInParent 与「根无 ARF」那几族才是它）。
//     🔴 **两者只在「框宽/框高 < 根宽/根高」时不同** —— 所以**方框夹具（以及比根更宽的框）永远发现不了这条**
//       （这正是 W-11/W-12/W-16/W-20 四份报告都写着「非方框 ARF 未读」却一直全绿的原因）。本节 A4/A5 用**非方框**夹具。
//  ② **`B-263`：`Horizontal`(15) / `OfferPopups`(30) 的 `Title Drawer Horizontal Variant` 是另一套版面。**
//     它的 `Content` = **1028×370**（不是竖卡的 712×1080），根**没有 ARF**（锚 0..1 铺满父框）、
//     `Content` 由 `UIScaleToFit(fitInParent)` **等比内接** ⇒ `FrameOf(box, 1028, 370)`（`Contain`）。
//     底板图 = `40k_Profile_display_title`（不是共享的 `40k_Rewards_icon_base card`）；
//     `Army` / `Label` 两颗 `m_IsActive = 0` ⇒ 原版屏幕上**没有**阵营名与名字行。
//  ③ **`OfferPopups`(30) 的 `CosmeticItemAvatarBorder` 指的也是 `Avatar Border Drawer Shop Variant`**
//     （`ItemDrawerConfig` 映射表 §② 那一行，与 `Shop`(20) **同一份 prefab**）⇒ 这一档同样**不画底板**。
//     我们原来只在 `ov == Shop` 时走那条分流 ⇒ `OfferPopups` 多画了一张底板。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · ① 的期望值**不是**调 `FrameOf`/`FitK` 算出来的：本节自己按 UGUI 的算式与 prefab 常量重算一遍
//    （`K_contain = min(H/1080, W/712.8)` / `K_height = H/1080` 写在测试里），再跟生产的读数比。
//  · A2/A3 是**一对**：A2 断言方框下两口径**相等**（解释历史断言为何全绿）、A3 断言窄框下**不等**。
//    🧨 谁把 `FitK` 写成「永远取 min」⇒ A3 红；谁把它写成「永远取高」⇒ A2b（宽框那一条）红。
//  · B4 是**结构性反证**：同一个框里，竖卡档与横条档的 `Content` 宽**必然不同**（712·K vs 1028·K）
//    ⇒ 「两档共用一套几何」这条修法不可能对（🧨 把 `TitleHorizontal` 改回 `Title` ⇒ B4 与 B6 同时红）。
//  · C3 是**反向对照**：`Default` 档仍然画底板 —— 它红说明「不画底板」被改宽成了「这一族永远不画」。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W32_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W32_Assert.Run(); _sink.Pass += B114_W32_Assert.Pass; _sink.Fail += B114_W32_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · **本波没有把 `FrameOf` 的默认值改成 `HeightControlsWidth`** —— 那会让 `m_AspectMode 3` 与「根无 ARF」
//    的那四族（`Icon Campaign Points Drawer Variant` / `Icon Expansion Pass Points Drawer Variant` /
//    `Title Drawer Horizontal Variant` / `Alliance Badge Drawer Icon`）反过来溢出。
//    迁移要**按 prefab 逐个显式传**，清单见 `资料/完全复刻/报告/B114_W32_ARF与两档几何.md` §一·4。
//    ⇒ 本节 A2/A3 断的是**两条口径各自的正确性**（`FitK` 的两支），不是「生产已经迁完了」。
//  · ② 的字号/高度取自**真包**（`menu_dump`），不是 AssetRipper 导出 —— 后者这份 prefab variant 的
//    TMP override 没合进去（`m_fontSize 90` vs 真包 `120`；反证 → `ItemDrawerPartial_B114W32.cs` 文件头 ①）。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-32`：账 `B-260`（ARF 非方框口径）+ `B-263`（`Horizontal`/`OfferPopups` 的横条称号档 + `OfferPopups` 的头像边框档）。</summary>
public static class B114_W32_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W32] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（prefab / UGUI 源码现读 · 独立于生产代码，⛔ 不从 `Metrics()` 或 `FrameOf` 反读）----
    /// <summary>基础抽屉根宽 = 1080 × ARF `m_AspectRatio 0.66`。</summary>
    const float RootW = 712.8f;
    /// <summary>基础抽屉根高。</summary>
    const float RootH = 1080f;
    /// <summary>`Title Drawer Horizontal Variant` 的 `Content.m_SizeDelta`（真包现读）。</summary>
    const float HTitleW = 1028f, HTitleH = 370f;
    /// <summary>`Content/Image` 的设计 px（Content 空间 · 左上原点 · y 向下）。</summary>
    const float HImgX1 = 786.7f, HImgY1 = 141.5f, HImgX2 = 1004.0f, HImgY2 = 353.0f;
    /// <summary>`Content/Title` 的设计 px（同上）。</summary>
    const float HTxtX1 = 51.4f, HTxtY1 = 64.4f, HTxtX2 = 976.6f, HTxtY2 = 306.6f;
    /// <summary>横条档专属底板图（⛔ 不是 `40k_Rewards_icon_base card`）。</summary>
    const string HTitleBg = "40k_Profile_display_title";

    // ---- 夹具：一个方框、一个**窄框**、一个**宽框**（后两个才是 ① 的关键）----
    /// <summary>方框夹具：正好是根的尺寸（`K = 1`）—— 历史断言全用这一档。</summary>
    static readonly PxRect BoxRoot = new PxRect(0f, 0f, RootW, RootH);
    /// <summary>窄框：600×1080（宽/高 = 0.5556 **小于** 根的 0.66）⇒ 原版会横向溢出。</summary>
    static readonly PxRect BoxNarrow = new PxRect(0f, 0f, 600f, RootH);
    /// <summary>宽框：1200×1080（宽/高 = 1.111 **大于** 根的 0.66）⇒ 高度先满足，两口径相同。</summary>
    static readonly PxRect BoxWide = new PxRect(0f, 0f, 1200f, RootH);
    /// <summary>横条档夹具：600×300（与 `Content` 的 1028×370 也不是同一比例 ⇒ 走内接的那一支）。</summary>
    static readonly PxRect BoxStrip = new PxRect(0f, 0f, 600f, 300f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W32（B-260 / B-263）：ARF 非方框口径 + `Horizontal`/`OfferPopups` 两档变体几何");

        // ⚠️ 四段各自独立调用：某一段「前提不成立就 return」不会把别的段吞掉
        SectionArf();
        SectionHorizontalTitle();
        SectionOfferPopupBorder();
        SectionArfProduction();

        Debug.Log(_sink.P + "B114W32 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① ARF 口径（`RootFit` 两支）
    static void SectionArf()
    {
        MenuCheck.Section(_sink, "① `AspectRatioFitter` 口径：`m_AspectMode 2` ⇒ `K = 框高/根高`（宽不看父框）· FitInParent/无 ARF ⇒ `min`");

        // —— 判据自己算一遍（⛔ 不调生产函数）——
        float kRootC = Mathf.Min(BoxRoot.H / RootH, BoxRoot.W / RootW);          // 方框：1
        float kRootH = BoxRoot.H / RootH;
        float kNarrowC = Mathf.Min(BoxNarrow.H / RootH, BoxNarrow.W / RootW);    // = 600/712.8
        float kNarrowH = BoxNarrow.H / RootH;                                     // = 1
        float kWideC = Mathf.Min(BoxWide.H / RootH, BoxWide.W / RootW);          // = 1
        float kWideH = BoxWide.H / RootH;                                         // = 1

        N(kRootC, kRootH, 1e-5f,
          "A1 方框（712.8×1080）下两条口径**逐值相等**（`K = 1`）—— 这就是为什么四份报告的方框断言全绿、却没人发现非方框那一路");

        N(ItemDrawer.FitK(BoxRoot, RootW, RootH, ItemDrawer.RootFit.Contain), kRootC, 1e-5f,
          "A2b 生产的 `Contain` 口径在方框上与手算一致（`min(1080/1080, 712.8/712.8) = 1`）");

        // ★★ 非方框（窄）—— 原版 `HeightControlsWidth`
        N(ItemDrawer.FitK(BoxNarrow, RootW, RootH, ItemDrawer.RootFit.HeightControlsWidth), kNarrowH, 1e-5f,
          "A3 ★★ 窄框（600×1080）下原版口径 `K = 框高/根高 = 1`（**不看框宽**）"
        + "｜🧨 把 `HeightControlsWidth` 也写成 `min` ⇒ 红（那正是我们现在的默认口径）");
        N(ItemDrawer.FitK(BoxNarrow, RootW, RootH, ItemDrawer.RootFit.Contain), kNarrowC, 1e-5f,
          "A4 ★★ 窄框下 **等比内接**口径 = `600/712.8 = 0.84175`（比原版小 15.8%）"
        + "｜🧨 两条口径在这里必须**不等** —— 谁让它们相等，A3 或 A4 必有一条红");

        Tr(Mathf.Abs(kNarrowH - kNarrowC) > 0.1f,
           "A5 ★★ 差异**只**在窄侧：窄框上 `K_height − K_contain = " + (kNarrowH - kNarrowC).ToString("F4")
         + "`（> 0.1）｜🧨 若有人把 `FitK` 的 `fit` 形参吞掉（恒走一支）⇒ A5 或 A6 红");

        // —— 溢出方向：原版根比框宽 112.8 ⇒ 左右各溢 56.4 ——
        var r = ItemDrawer.RootRectOf(BoxNarrow, RootW, RootH, ItemDrawer.RootFit.HeightControlsWidth);
        N(r.W, RootW, 0.01f, "A6 ★★ 窄框下的**根宽** = 712.8（= 1080×0.66，与框宽 600 **无关**）⇒ 原版是**横向溢出**");
        N(r.x1, BoxNarrow.CX - RootW * 0.5f, 0.01f,
          "A6b ★ 溢出量：左沿 = 300 − 356.4 = **−56.4**（在框外）｜🧨 按宽缩的实现会给 0（贴框左沿）⇒ 红");
        N(r.x2, BoxNarrow.CX + RootW * 0.5f, 0.01f, "A6c ★ 右沿 = 300 + 356.4 = **656.4**（同样在框外）");
        Tr(r.x1 < BoxNarrow.x1 - 1f && r.x2 > BoxNarrow.x2 + 1f,
           "A6d ★★ 结构反证：根**真的**比框宽（左右各越界 56.4）—— 不是浮点噪声、不是「刚好贴边」");

        var rc = ItemDrawer.RootRectOf(BoxNarrow, RootW, RootH, ItemDrawer.RootFit.Contain);
        Tr(rc.x1 >= BoxNarrow.x1 - 1e-3f && rc.x2 <= BoxNarrow.x2 + 1e-3f,
           "A7 对照：`Contain` 口径下根**不越界**（x1 = " + rc.x1.ToString("F2") + " · x2 = " + rc.x2.ToString("F2")
         + "）—— 两种口径在窄框上给出**两个不同的根矩形**");

        // —— 宽框：两支相同（这就是「只有窄侧才分叉」的另一半）——
        N(ItemDrawer.FitK(BoxWide, RootW, RootH, ItemDrawer.RootFit.HeightControlsWidth), kWideH, 1e-5f,
          "A8 宽框（1200×1080）下原版口径 `K = 1`（高先满足）");
        N(ItemDrawer.FitK(BoxWide, RootW, RootH, ItemDrawer.RootFit.Contain), kWideC, 1e-5f,
          "A8b 宽框下两条口径**相同**（都是 1）⇒ 分叉只在「框比根更窄」那一侧");
        Tr(Mathf.Abs(kWideH - kWideC) < 1e-5f,
           "A8c ★ 不变式：`框宽/框高 ≥ 根宽/根高` 时两条口径**必然相等**（这一条不复制任何一次读数，只比两支之差）");

        // —— 边界：恰好等于根比例 ——
        var exact = new PxRect(0f, 0f, 500f * 0.66f, 500f);
        N(ItemDrawer.FitK(exact, RootW, RootH, ItemDrawer.RootFit.Contain),
          ItemDrawer.FitK(exact, RootW, RootH, ItemDrawer.RootFit.HeightControlsWidth), 1e-5f,
          "A9 边界：框比例**恰好等于** 0.66 时两支逐值相等（分叉的判据是「严格更窄」，不是「不等于」）");

        // —— 比例守恒（两条口径都必须保住根的比例）——
        var rh = ItemDrawer.RootRectOf(BoxNarrow, RootW, RootH, ItemDrawer.RootFit.HeightControlsWidth);
        N(rh.W / rh.H, RootW / RootH, 1e-4f,
          "A10 ★ 不变式：原版口径下根比例守恒（712.8/1080 = 0.66）—— 变形的只有「占多大」，不是「长宽比」");
    }

    // ============================================================ ② `Title Drawer Horizontal Variant`（B-263 a）
    static void SectionHorizontalTitle()
    {
        MenuCheck.Section(_sink, "② `Horizontal`(15)/`OfferPopups`(30) 的 `Title Drawer Horizontal Variant`：`Content` 1028×370 · 无 Army/Label");

        Tr(CardArt.MenuUi(HTitleBg) != null,
           "B0 **前提**：横条档专属底板 `" + HTitleBg + "` 在 `Resources/Art/ui_menu/` 取得到"
         + "｜🧨 删掉那张图 ⇒ 红（那时 `Background` 整层不建，B2 没对象）");
        Tr(ArmyIcons.Texture(10) != null, "B0b **前提**：阵营徽记 `CardArmy 10` 取得到（横条档主图就是它）");

        // 手算期望（⛔ 不调生产函数）：根无 ARF ⇒ 根 = 框；Content 1028×370 由 UIScaleToFit 内接
        float k = Mathf.Min(BoxStrip.H / HTitleH, BoxStrip.W / HTitleW);           // min(0.8108, 0.5837)
        float ox = BoxStrip.CX - HTitleW * k * 0.5f;
        float oy = BoxStrip.CY - HTitleH * k * 0.5f;

        var root = new GameObject("B114W32_HTITLE");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var spec = new ItemSpec
            {
                Id = "B114W32_TitleH", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
                CardArmy = 10, Label = "B114W32", Title = "B114W32 Title",
            };
            res = ItemDrawer.Draw(root.transform, BoxStrip, spec, 1, DrawerOverride.OfferPopups,
                                  ItemDrawerStyle.Default(10, 11, 12));

            Tr(res.Node != null, "B1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerTitleHorizontal,
              "B1b **前提**：`CosmeticItemTitle` + `OfferPopups`(30) 路由到 `TitleDrawerHorizontal`"
            + "（`ItemTypeSets` 那一格；映射表 §② GUID `b9799d0d…`）");
            if (res.Node == null) return;

            var content = FindDeep(res.Node, ItemDrawer.NodeContent);
            Tr(content != null, "B2 **前提**：`Content` 建出来了");
            if (content != null)
            {
                var rt = content as RectTransform;
                N(PxW(rt), HTitleW * k, 1.5f,
                  "B3 ★★ `Content` 宽 = 1028 × K = " + (HTitleW * k).ToString("F1") + "（**不是**竖卡的 712×K）"
                + "｜🧨 把这一档改回 `Title(...)`（竖卡 `FrameOf(box)` + 712×1080）⇒ 红");
                N(PxH(rt), HTitleH * k, 1.5f,
                  "B3b ★ `Content` 高 = 370 × K = " + (HTitleH * k).ToString("F1"));
            }

            var bg = FindDeep(res.Node, ItemDrawer.NodeBackground);
            Tr(bg != null, "B4 **前提**：`Content/Background` 建出来了");
            if (bg != null)
            {
                var q = QuadOf(bg);
                N(q.x1, ox + 0f * k, 1.5f, "B4b ★ 底板左沿 = 根左沿（这一档的 `Content/Background` 铺满 1028×370）");
                N(q.W, HTitleW * k, 1.5f, "B4c ★ 底板宽 = 1028 × K");
            }

            var img = FindDeep(res.Node, ItemDrawer.NodeImage);
            Tr(img != null, "B5 **前提**：主图 `Content/Image` 建出来了");
            if (img != null)
            {
                // 🔴 2026-11-01 就地订正（铁律 5 · `B114_R9` 诊断 #4/#5）：这一颗 `Content/Image` 在原版是
                //   **`m_PreserveAspect = 1`**（`Title Drawer Horizontal Variant.prefab` 现读；基础档 `Title Drawer.prefab`
                //   同一路径 `Content/Image` 也是 1；实现 `ItemDrawerLayout.DrawerImage` 恒传 `keepAspect: true` —— **本批未改**）
                //   ⇒ 画出来是**按图比例内接 + 居中**，⛔ 不是拉伸到框沿。
                //   原来四条期望写的是「框沿」：只有**受限那一维**（这里 = 高）恰好与框沿同值，所以 B5d/B5e 是**碰巧绿**；
                //   换成非方图/换比例就会给出误导性的绿。
                //   ⛔ 期望值不调 `FrameOf`/`FitK`（那是自证）：喂给 `Fit` 的框是**独立的 prefab 常量**（`HImgX1..Y2`），
                //   而内接那一段走与生产**同一条** `CardbackFace.Fit`（`MenuDraw.Rect` 的 `keepAspect` 支，
                //   `Shell/MenuDraw.cs:1641-1650`）—— 这正是同一断言族 `B114_W16_Assert.cs` 的 S6 用的口径。
                var imgR = new PxRect(ox + HImgX1 * k, oy + HImgY1 * k, ox + HImgX2 * k, oy + HImgY2 * k);
                var wantImg = FitBox(ArmyIcons.Texture(10), imgR);
                var q = QuadOf(img);
                N(q.x1, wantImg.x1, 1.5f,
                  "B5b ★★ 主图左沿 = 框沿 " + (ox + HImgX1 * k).ToString("F2")
                + " 按 **PA=1** 内接后的 " + wantImg.x1.ToString("F2") + "（**不是**框沿）"
                + "｜🧨 用竖卡的 (191.5,76.5)-(520.5,422.3) ⇒ 红；改成拉伸到框沿 ⇒ 红");
                N(q.x2, wantImg.x2, 1.5f,
                  "B5c ★ 主图右沿（同上，PA=1 内接）= " + wantImg.x2.ToString("F2") + "（框沿是 "
                + (ox + HImgX2 * k).ToString("F2") + "）");
                N(q.y1, wantImg.y1, 1.5f,
                  "B5d ★ 主图上沿 = " + wantImg.y1.ToString("F2")
                + "（高是受限维 ⇒ 与框沿 " + (oy + HImgY1 * k).ToString("F2") + " 同值；算式仍走同一条内接）");
                N(q.y2, wantImg.y2, 1.5f,
                  "B5e ★ 主图下沿 = " + wantImg.y2.ToString("F2") + "（同 B5d）");
            }

            Tr(FindDeep(res.Node, ItemDrawer.NodeTitleText) != null,
               "B6 ★★ 称号正文 `Content/Title` 建出来了（这一档的 `title` 槽；字号 120 / 自动 [15,120]）"
             + "｜🧨 这一支改用竖卡的 `Title` 那格（64.7,432.1)-(648.2,680.5)/字号 27.05 ⇒ 落在框外、看不见 ⇒ 红");

            Tr(FindDeep(res.Node, ItemDrawer.NodeArmy) == null,
               "B7 ★★ **不建** `Army`：这一份 prefab 的 `Content/Army` 是 `m_IsActive = 0`（真包现读）"
             + "｜🧨 把 `DrawerArmyName` 顺手接进来 ⇒ 红（原版屏幕上没有阵营名那一行）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeName) == null,
               "B7b ★★ **不建** `Name`：`Content/Label` 整颗 `m_IsActive = 0`（名字与数量都不出现）"
             + "｜🧨 把 `DrawerNameQuantity(hasName:true)` 接进来 ⇒ 红");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }

        // —— 结构性反证：同一个框里，竖卡档与横条档的 `Content` **不可能**同宽 ——
        var root2 = new GameObject("B114W32_TITLE_BASE");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            var spec2 = new ItemSpec
            {
                Id = "B114W32_Title", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
                CardArmy = 10, Label = "B114W32", Title = "B114W32 Title",
            };
            res2 = ItemDrawer.Draw(root2.transform, BoxStrip, spec2, 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(20, 21, 22));
            C(res2.Drawer, ItemDrawer.DrawerTitle, "B8 **前提**：`Default` 档路由到竖卡 `TitleDrawer`");
            var c2 = res2.Node == null ? null : FindDeep(res2.Node, ItemDrawer.NodeContent);
            float wBase = PxW(c2 as RectTransform);
            float wH = HTitleW * k;
            Tr(wBase > 0f && Mathf.Abs(wBase - wH) > 50f,
               "B9 ★★ 结构反证：同一个 600×300 的框里，竖卡 `Content` 宽 = " + wBase.ToString("F1")
             + " vs 横条 `Content` 宽 = " + wH.ToString("F1") + " ⇒ **两档不可能共用一套几何**"
             + "｜🧨 有人把 `TitleHorizontal` 删掉、让两条 case 合回一支 ⇒ B3 与 B9 同时红");
            Tr(FindDeep(res2.Node, ItemDrawer.NodeArmy) != null,
               "B9b 反向对照：竖卡档**建了** `Army`（它的 prefab 里是 `m_IsActive = 1`）⇒「不建 Army」是横条档专属，不是两档一起砍");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }
    }

    // ============================================================ ③ `OfferPopups`(30) 的头像边框档（B-263 b）
    static void SectionOfferPopupBorder()
    {
        MenuCheck.Section(_sink, "③ `CosmeticItemAvatarBorder` 的 `OfferPopups`(30) 也走 `Avatar Border Drawer Shop Variant`（不画底板）");

        Tr(DrawHasBackground(DrawerOverride.Default),
           "C1 **前提/对照**：`Default`(0) 档 = `Avatar Border Drawer` ⇒ **有** `Content/Background`（底板 `40k_Rewards_icon_base card`）");
        Tr(!DrawHasBackground(DrawerOverride.Shop),
           "C2 `Shop`(20) 档 = `Avatar Border Drawer Shop Variant` ⇒ **没有** `Content/Background`（`m_IsActive = 0`，`B-256` 已接）");
        Tr(!DrawHasBackground(DrawerOverride.OfferPopups),
           "C3 ★★ `OfferPopups`(30) 档指的**也是** `Avatar Border Drawer Shop Variant`（映射表 §② 那两行是同一个 GUID）"
         + " ⇒ 同样**不画底板**｜🧨 分流只认 `ov == Shop` ⇒ 这里红（原来就是这样）");
        C(ItemDrawer.PickDrawer(SpecBorder(), DrawerOverride.OfferPopups), ItemDrawer.DrawerAvatarBorder,
          "C4 **前提**：`OfferPopups` 档仍路由到 `AvatarBorderDrawer` 这个类（变体只是换了 prefab，类名不变）");
    }

    /// <summary>画一次 `CosmeticItemAvatarBorder`，回答「这一档有没有 `Content/Background`」。</summary>
    static bool DrawHasBackground(DrawerOverride ov)
    {
        var root = new GameObject("B114W32_BORDER_" + ov);
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            res = ItemDrawer.Draw(root.transform, BoxRoot, SpecBorder(), 1, ov, ItemDrawerStyle.Default(30, 31, 32));
            if (res.Node == null) return false;
            return FindDeep(res.Node, ItemDrawer.NodeBackground) != null;
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ ④ 生产迁移：窄框下根**横向溢出**（`B-260` 的迁移验收）
    //  ⚠️ 前三段断的是**两条口径各自的算式**（`FitK` 的两支）；这一段断的是**生产真的走了 ARF 2 那一支** ——
    //     判据只认「画出来那颗 `Content` 有多宽」，⛔ 不调 `FitK`/`FrameOf` 拿期望值（那是自证）。
    //  🧨 灭自证：把 `ItemDrawerCosmetic.AvatarBorder` 那处的 `fit: RootFit.HeightControlsWidth` 删掉（回 `Contain`）
    //     ⇒ `Content` 宽从 712 变 `712 × 600/712.8 = 599.3` ⇒ P1/P2/P4/P5 同时红。
    static void SectionArfProduction()
    {
        MenuCheck.Section(_sink, "④ 生产：`m_AspectMode 2` 的抽屉在**窄框**（宽 600 < 高×0.66 = 712.8）里**横向溢出**，⛔ 不按宽缩");

        // 手算期望（⛔ 不调生产函数）：ARF 2 ⇒ K = 框高/根高 = 1；Content 宽 = 712 × K = 712（比框宽 600 还宽）
        float kH = BoxNarrow.H / RootH;                                              // 1
        float kContain = Mathf.Min(BoxNarrow.H / RootH, BoxNarrow.W / RootW);         // 0.84175（本波**不该**走这一支）

        var got = ProbeContent(BoxNarrow);
        Tr(got.Has, "P0 **前提**：窄框里抽屉根与 `Content` 都建出来了");
        if (!got.Has) return;

        N(got.W, 712f * kH, 1.5f,
          "P1 ★★ 生产：窄框（600×1080）下 `Content` 宽 = 712 × (1080/1080) = **712** —— 比框宽 600 还宽"
        + "｜🧨 把那一处的 `fit: RootFit.HeightControlsWidth` 删掉（回 `Contain`）⇒ 变 " + (712f * kContain).ToString("F1") + " ⇒ 红");
        N(got.BgX1, BoxNarrow.CX - RootW * 0.5f + 0.4f, 2.5f,
          "P2 ★★ 溢出方向：背景左沿 ≈ 300 − 356.4 + 0.4 = **−56.0**（落在框外；`Contain` 会给 ≈ 0.7）");
        Tr(got.W > BoxNarrow.W + 100f,
          "P3 ★★ 结构反证：`Content` 比框宽 " + (got.W - BoxNarrow.W).ToString("F1") + " px（> 100）"
        + " —— 原版这一刻是**溢出**，不是「刚好贴边」、也不是浮点噪声");
        Tr(Mathf.Abs(got.W - 712f * kContain) > 100f,
          "P4 ★★ 差值反证：实测宽与本波之前那条口径（`Contain` = " + (712f * kContain).ToString("F1") + "）差 "
        + Mathf.Abs(got.W - 712f * kContain).ToString("F1") + " px ⇒ 生产真的换了支（🧨 迁回去 ⇒ 差变 0 ⇒ 红）");

        // —— 宽框对照：1200×1080 时两支**逐值相同**（K 都是 1）⇒ 证明 P1 不是「一律放大」——
        var wide = ProbeContent(BoxWide);
        Tr(wide.Has, "P5 **前提**：宽框里抽屉根与 `Content` 都建出来了");
        N(wide.W, 712f * (BoxWide.H / RootH), 1.5f,
          "P6 宽框（1200×1080）对照：`Content` 宽仍是 **712**（两支相同）⇒ 分叉只在「框比根更窄」那一侧");
    }

    /// <summary>在给定框里画一次 `Avatar Border Drawer`（ARF `m_AspectMode 2` · 0.66 · 已迁 `HeightControlsWidth`），
    /// 量 `Content` 的宽与 `Content/Background` 的左沿（设计 px · 左上原点 · y 向下）。</summary>
    static ContentProbe ProbeContent(PxRect box)
    {
        var root = new GameObject("B114W32_ARF_" + box.W.ToString("F0"));
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            res = ItemDrawer.Draw(root.transform, box, SpecBorder(), 1, DrawerOverride.Default,
                                  ItemDrawerStyle.Default(40, 41, 42));
            var content = res.Node == null ? null : FindDeep(res.Node, ItemDrawer.NodeContent);
            var bg = res.Node == null ? null : FindDeep(res.Node, ItemDrawer.NodeBackground);
            var probe = new ContentProbe { Has = content != null };
            if (content != null) probe.W = PxW(content as RectTransform);
            probe.BgX1 = bg == null ? float.NaN : QuadOf(bg).x1;
            return probe;
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>`ProbeContent` 的读数（`Has` = `Content` 建出来了）。</summary>
    struct ContentProbe
    {
        /// <summary>`Content` 节点在。</summary>
        public bool Has;
        /// <summary>`Content` 的宽（设计 px）。</summary>
        public float W;
        /// <summary>`Content/Background` 的左沿（设计 px；没画成 = `NaN`）。</summary>
        public float BgX1;
    }

    static ItemSpec SpecBorder()
    {
        return new ItemSpec
        {
            Id = "B114W32_Border", Type = "CosmeticItemAvatarBorder", Kind = ItemKind.Generic,
            Art = "Player_Profile_Border", Label = "B114W32",
        };
    }

    // ============================================================ 夹具 / 量具
    /// <summary>设计 px ↔ 世界单位（`MenuDraw.SetPxSize` 写的是 `LayoutSpace.Px(px)` = px/108）——
    /// 量**容器节点**的 `rect` 时必须乘回来，⛔ 别拿 `rect.width` 直接跟设计 px 比（会差 108 倍）。</summary>
    static readonly float PxPerUnit = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

    /// <summary>容器节点的宽（**设计 px**）。</summary>
    static float PxW(RectTransform rt) { return rt == null ? -1f : rt.rect.width * PxPerUnit; }
    /// <summary>容器节点的高（**设计 px**）。</summary>
    static float PxH(RectTransform rt) { return rt == null ? -1f : rt.rect.height * PxPerUnit; }

    /// <summary>图上的件是 `ImageQuad` ⇒ 走 `MenuDraw.QuadRectPx` 反推（设计 px · 左上原点 · y 向下）。</summary>
    static PxRect QuadOf(Transform t)
    {
        var q = t.GetComponent<ImageQuad>();
        return q == null ? new PxRect() : MenuDraw.QuadRectPx(q);
    }

    /// <summary>`MenuDraw.Rect(..., keepAspect: true)` 之后**画出来**的矩形（= uGUI `PreserveSpriteAspectRatio`）。
    /// 🔴 2026-11-01（`B114_W51` · 诊断 #4/#5）：生产 `MenuDraw.Rect` 的 `keepAspect` 支就是调
    /// `CardbackFace.Fit`（`Shell/MenuDraw.cs:1641-1650`）⇒ 这里直接复用那一条，只喂**独立的 prefab 常量**框。
    /// 与 `B114_W16_Assert.cs` 的 `FitBox` 逐字同形（那一份已被 S6 用绿）。</summary>
    static PxRect FitBox(Texture2D tex, PxRect box)
    {
        float dw, dh, dx, dyUp;
        if (tex == null || !CardbackFace.Fit(tex, box.W, box.H, out dw, out dh, out dx, out dyUp)) return box;
        float cx = box.CX + dx, cy = box.CY - dyUp;     // `Fit` 的 dy 是**向上为正**；本仓 `PxRect` 是 y 向下
        return new PxRect(cx - dw * 0.5f, cy - dh * 0.5f, cx + dw * 0.5f, cy + dh * 0.5f);
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
