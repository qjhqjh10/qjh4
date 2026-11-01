// B114_W24_Assert.cs — 第一百一十四会话 · 写手 `W-24`（2026-11-01）· 账 `B-256` 的断言
//
// ============================ 它盯的是什么 ============================
//  `CosmeticItemAvatarBorder` 在 `DrawerOverride.Shop`(20) 那一档，原版换了另一套 prefab
//  （`Avatar Border Drawer Shop Variant`，同一个类 `AvatarBorderDrawer` —— 与 `B-249` 的 `PlayerAvatar` 同形兄弟）。
//  两份 prefab 的**唯一行为差异** = `Content/Background` 那颗 GameObject 的 `m_IsActive: 1 → 0`
//  ⇒ 原版商店档**屏幕上没有那张底板**，其余（`avatarBG` / `avatar` / `Army` / `Label` / `Label/Name`）**逐值相同**。
//  本件断的是「底板建没建」+「其余那几格有没有被顺手改坏」，⛔ 不断「某个方法被调过」。
//
//  【判据（现行 build 现读）】
//    · 真包：`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Avatar Border Drawer Shop Variant"
//      --depth 4 --relative --no-sprite --no-ancestor-scale`
//        → `Background` 行 `act = INACT`（`m_IsActive = 0`）· `avatarBG` (139.8,233.3)-(571.0,712.3)
//          · `avatar` (-65.0,65.6)-(795.2,796.1) · `Label` (36.0,810.0)-(676.8,918.0) · `Label/Name` (61,810)-(651.8,918)
//    · 交叉源：两份 `d:/2/tools/assetripper_out/ExportedProject/Assets/Avatar Border Drawer*.prefab` 的 `diff`
//      **整份只有 3 行不同**：`m_Name` · `Background.m_IsActive 1→0` · `Background` 那颗 `Image` 的
//      `m_Enabled 1→0` + `m_Color.a 1→0`（后两条与「整颗 GO 关着」叠加同义，见 `B114_W16` 报告 §八·4）。
//    · 判据链的另一半（「谁会把它打开」）：`ItemDrawer<T>.Draw`（`decomp_generic_trial/out/ItemDrawer_object___Draw.c`）
//      只 SetActive 过 `quantity` / `label` 两格，**`background` 那格一次没碰** ⇒ 序列化态说了算。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · C1（结论）断 Shop 档**没有** `Background` 节点：让 `ShopAvatarBorderVariant` 不生效 / 仍调 `DrawerBackground` ⇒ 红。
//  · **D4 = 结构上不可能同时满足的那一半**：同一个夹具、同一套 `ItemDrawerStyle`，`Default` 档
//    **有底板且激活**、`Shop` 档**没有底板**，而**两档的主图矩形与 `Label` 矩形完全相同**
//    ⇒ 谁把两档画成同一套（Shop 也画底板）⇒ D4 与 C1 一起红；谁把 Shop 画成「什么都不画」（主图也没了）
//    ⇒ P3 直接红（那正是「靠画不出来把 C1 变绿」的那种假绿）。
//  · C2/C3 锁「其余格没被顺手改」：把 Shop 支改成另一套几何（例如照 `B-249` 的 `ShopAvatar` 那批常量）⇒ 红。
//
// ============================ 怎么接（⚠️ 由调度台统一追加，写手不碰宿主）============================
//  唯一入口 = `B114_W24_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行（紧跟 `B114_W16_Assert...` 之后、
//  `Debug.Log(P + $"=== 合计` 之前）**同一物理行、第一个 `//` 之前**追加：
//      B114_W24_Assert.Run(); _sink.Pass += B114_W24_Assert.Pass; _sink.Fail += B114_W24_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · `ShopData.cs` 目前没有 `CosmeticItemAvatarBorder` 商品 ⇒ 生产暂未触发；本件直接调
//    `ItemDrawer.Draw(..., DrawerOverride.Shop, ...)` 把这一档建出来（与 `ShopTabPage` 生产那条调用同形）。
//  · 本件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-24`：账 `B-256` —— `Shop` 档头像边框变体（`Avatar Border Drawer Shop Variant`）的「不画底板」。</summary>
public static class B114_W24_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W24] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>抽屉根 = 712.8×1080（`AspectRatioFitter` 0.66）⇒ 夹具直接给这个框（`FrameOf` 的 `K = 1`）。</summary>
    static readonly PxRect BoxBase = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>边框图（`Resources/Art/ui/Player_Profile_Border.png` 现成可取；= `CardArt.MenuUi` 的第三批目录）。</summary>
    const string ArtBorder = "Player_Profile_Border";
    /// <summary>抽屉共享底图（prefab `Content/Background.m_Sprite`；`Resources/Art/ui_menu/` 里有这张）。</summary>
    const string BgSprite = "40k_Rewards_icon_base_card";

    // ---- 期望值 = **独立的 prefab 常量**（⛔ 不读 `ItemDrawer` 里那批常量，否则就是拿实现证明实现）----
    /// <summary>`Avatar Border Drawer`（基础档与 Shop 变体**逐值相同**）的 `image` 槽 = `avatarBG`：
    /// 锚 (0,0.38)-(1,0.95) of 712×1080 ⇒ 设计 px (139.8,233.3)-(571.0,712.3)。</summary>
    static readonly PxRect ImgRect = new PxRect(139.8f, 233.3f, 571.0f, 712.3f);
    /// <summary>`Label` 一行：锚 (0,0.14)-(1,0.25)？—— 实读 prefab 是 (36.0,810.0)-(676.8,918.0)（两档相同）。</summary>
    static readonly PxRect LabelRect = new PxRect(36.0f, 810.0f, 676.8f, 918.0f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W24（B-256）：`Shop` 档头像边框（`Avatar Border Drawer Shop Variant` 不画底板）");
        SectionShop();
        SectionDefaultControl();
        Debug.Log(_sink.P + "B114W24 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ Shop 档
    static void SectionShop()
    {
        var tex = CardArt.MenuUi(ArtBorder);
        var bgTex = CardArt.MenuUi(BgSprite);
        Tr(bgTex != null, "P0 **前提**：抽屉共享底图 `" + BgSprite + "` 取得到（`CardArt.MenuUi`）"
                         + "｜🧨 删掉 `Resources/Art/ui_menu/" + BgSprite + ".png` ⇒ 红"
                         + "（⛔ 不是 `if (图) Check(...)` 静默跳过 —— 这一条本身就是硬前提；它不在 ⇒ 下面 D3「基础档有底板」会变成空转）");
        Tr(tex != null, "P0b **前提**：边框图 `" + ArtBorder + "` 取得到（`CardArt.MenuUi` 的 `ui/` 那一批）"
                      + "｜🧨 删掉 `Resources/Art/ui/" + ArtBorder + ".png` ⇒ 红");
        if (tex == null || bgTex == null) return;

        var root = new GameObject("B114W24_BORDER_SHOP");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);   // `QuantityPx 34 / NamePx 26` 都非 0 ⇒ 文字层**开着**
            var spec = new ItemSpec
            {
                Id = "B114W24_Border", Type = "CosmeticItemAvatarBorder", Kind = ItemKind.Generic,
                Art = ArtBorder, Label = "B114W24",
            };
            res = ItemDrawer.Draw(root.transform, BoxBase, spec, 1, DrawerOverride.Shop, st);

            Tr(res.Node != null, "P1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAvatarBorder,
              "P1b **前提**：`ov = Shop` 仍路由到 `AvatarBorderDrawer`（`ItemTypeSets`（`ItemDrawer.cs:421`）的 Shop(20) 列写的就是它 —— 同一个类、两套 prefab）");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "P2 **前提**：`Content` 建出来了（这一档的 `Content` 与基础档逐值相同：712×1080）");

            var img = FindDeep(res.Node, ItemDrawer.NodeImage);
            Tr(img != null, "P3 **前提**：主图节点 `Image` 建出来了 —— 这一条是 C1 的反向守卫："
                          + "「没有 `Background`」必须是**只少了底板**，⛔ 不是整格没画出来（那样 C1 会假绿）");
            if (img != null)
                RectIs(QuadRectOf(img), FitBox(tex, ImgRect), 0.6f,
                       "C2 主图框 = Fit((139.8,233.3)-(571.0,712.3)) —— 与基础档**同一格**（两份 prefab 逐值相同）"
                     + "｜🧨 照 `B-249` 的 `ShopAvatar` 那批常量（或任何别的档）给 ⇒ 红");

            // ---- 底板：这一档**没有**（本条就是整条账的结论）----
            //     判据是 prefab 的 `m_IsActive: 0` ⇒ 「**不存在 或 未激活**」两种写法在屏幕上同义，都可接受
            //     （我们的实现走「不建」，见 `ItemDrawerPartial_B114W24.cs`）。
            Tr(!BackgroundShown(res.Node),
               "C1 ★★ **结论**：`Shop` 档**没有** `Background`（节点不存在，或建了但 `activeSelf == false`）——"
             + "`Avatar Border Drawer Shop Variant` 的 `Content/Background` GameObject 是 `m_IsActive: 0`"
             + "（真包 `act = INACT` · 两份 prefab `diff` 只有这一处行为差异），"
             + "而 `ItemDrawer<T>.Draw` / `AvatarBorderDrawer.Draw` 都不碰 `background` 那格 ⇒ 原版屏幕上没有那张底板"
             + "｜🧨 让 Shop 支仍调 `DrawerBackground`（或删掉 `Dispatch` 那条 `ov == Shop` 分流）⇒ 红"
             + "（实得：节点=" + (FindDeep(res.Node, ItemDrawer.NodeBackground) != null) + "）");

            // ---- 其余格：这一档与基础档**逐值相同** ⇒ 顺手核一遍，防「为了不画底板把别的也改坏」----
            var label = FindDeep(res.Node, "Label");
            Tr(label != null, "C3a `Label` 建出来了（这一档 `label` 槽非 null，与基础档同）");
            if (label != null)
                RectIs(NodeRectOf(label), LabelRect, 0.6f,
                       "C3b `Label` 框 = (36.0,810.0)-(676.8,918.0) —— 与基础档**同一格**"
                     + "｜🧨 把 Shop 支改成另一套名字行几何 ⇒ 红");
            Tr(FindDeep(res.Node, ItemDrawer.NodeName) != null,
               "C4 `Label/Name` 建出来了（`hasName: true`；期望框 (61,810)-(651.8,918)，与基础档同）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeQuantity) == null,
               "C5 `Quantity` 不建（这一档 `quantity` 槽 = null，与基础档同 ⇒ 永不画数量）");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ 灭自证：两档只有底板不同
    static void SectionDefaultControl()
    {
        MenuCheck.Section(_sink, "D 灭自证：同一个夹具下，`Shop` 档与 `Default` 档「只有底板不同」");

        var tex = CardArt.MenuUi(ArtBorder);
        var bgTex = CardArt.MenuUi(BgSprite);
        if (tex == null || bgTex == null)
        {
            Tr(false, "D0 **前提**：边框图与底图都取得到（与 P0/P0b 同一对 —— 这里必须在，否则整段是空转）");
            return;
        }

        var rootShop = new GameObject("B114W24_BORDER_SHOP2");
        var rootBase = new GameObject("B114W24_BORDER_DEFAULT");
        ItemDrawResult resShop = default(ItemDrawResult);
        ItemDrawResult resBase = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);        // 与 S 组**同一套** style
            var spec = new ItemSpec
            {
                Id = "B114W24_Border", Type = "CosmeticItemAvatarBorder", Kind = ItemKind.Generic,
                Art = ArtBorder, Label = "B114W24",
            };
            resShop = ItemDrawer.Draw(rootShop.transform, BoxBase, spec, 1, DrawerOverride.Shop, st);
            resBase = ItemDrawer.Draw(rootBase.transform, BoxBase, spec, 1, DrawerOverride.Default, st);

            Tr(resShop.Node != null && resBase.Node != null, "D1 **前提**：两档的根都建出来了");
            if (resShop.Node == null || resBase.Node == null) return;

            C(resBase.Drawer, ItemDrawer.DrawerAvatarBorder,
              "D2 **前提**：`ov = Default` 也路由到同一个抽屉类（两档只差 prefab）");

            var baseBg = FindDeep(resBase.Node, ItemDrawer.NodeBackground);
            bool baseBgOn = baseBg != null && baseBg.gameObject.activeSelf;
            bool shopBg = BackgroundShown(resShop.Node);      // 「不存在 或 未激活」都算「不显示」

            Tr(baseBgOn,
               "D3 ★ 基础档：**有** `Background` 且 **`activeSelf == true`**（`Avatar Border Drawer` 的 `m_IsActive: 1`）"
             + "｜🧨 把 `AvatarBorder` 默认的 `drawBackground` 改成 `false`（基础档也不画）⇒ 红"
             + "（实得：节点=" + (baseBg != null) + " · 激活=" + baseBgOn + "）");

            // 两档的「其余格」必须逐值相同（判据：两份 prefab 只有 `Background` 那一处行为差异）
            var baseImg = FindDeep(resBase.Node, ItemDrawer.NodeImage);
            var shopImg = FindDeep(resShop.Node, ItemDrawer.NodeImage);
            var baseLabel = FindDeep(resBase.Node, "Label");
            var shopLabel = FindDeep(resShop.Node, "Label");
            bool sameImg = baseImg != null && shopImg != null && SameRect(QuadRectOf(baseImg), QuadRectOf(shopImg), 0.01f);
            bool sameLabel = baseLabel != null && shopLabel != null
                          && SameRect(NodeRectOf(baseLabel), NodeRectOf(shopLabel), 0.01f);

            Tr(baseBgOn && !shopBg && sameImg && sameLabel,
               "D4 ★★ **灭自证**：两档的关系 =「**只有底板不同**」—— 基础档 (有底板·激活)，`Shop` 档 (无底板)，"
             + "而**主图矩形与 `Label` 矩形逐值相同**｜🧨 谁把两档画成同一套（Shop 也画底板）⇒ 这条与 C1 一起红；"
             + "谁把 Shop 支改成另一套几何 ⇒ `sameImg`/`sameLabel` 红；谁靠「Shop 整格没画」骗过 C1 ⇒ P3 红"
             + "（实得：基础(底板显示=" + baseBgOn + ", 主图同=" + sameImg + ", Label同=" + sameLabel + ") · Shop(底板显示=" + shopBg + ")）");
        }
        finally
        {
            if (resShop.View != null) resShop.View.Release();
            if (resBase.View != null) resBase.View.Release();
            Object.DestroyImmediate(rootShop);
            Object.DestroyImmediate(rootBase);
        }
    }

    // ============================================================ 量具
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

    /// <summary>`Content/Background` **显示着吗**（= 节点存在 **且** `activeSelf`）。
    /// 原版的判据是序列化态 `m_IsActive: 0` ⇒ 「不建」与「建了但关着」在屏幕上同义，两者都不算显示。</summary>
    static bool BackgroundShown(Transform root)
    {
        var t = FindDeep(root, ItemDrawer.NodeBackground);
        return t != null && t.gameObject.activeSelf;
    }

    /// <summary>主图那类件是 `ImageQuad` ⇒ 走全仓唯一那份 `MenuDraw.QuadRectPx`（设计 px · 左上原点 · y 向下）。</summary>
    static PxRect QuadRectOf(Transform t)
    {
        var q = t != null ? t.GetComponentInChildren<ImageQuad>(true) : null;
        return q != null ? MenuDraw.QuadRectPx(q) : new PxRect();
    }

    /// <summary>文字 / 纯容器那颗自己的矩形（画布 px · 左上原点 · y 向下）。
    /// 尺寸项取 `rect.rect`（拉伸锚点下 `sizeDelta` 表达的是「相对父框的内缩量」）；
    /// 位置项走 `PixelOfDesign(PosInDesignSpace(t))`（= `MenuDraw.Local` 的逆）—— 与 `B114_W16_Assert.cs`
    /// 的 `NodeRectOf` / `Editor/C5_Assert.cs` 的 `NodeRect` 同一条量法（⛔ 不复用第三份公式）。</summary>
    static PxRect NodeRectOf(Transform t)
    {
        var rt = t as RectTransform;
        if (rt == null) return new PxRect();
        var r = rt.rect;
        Vector2 c = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(rt));
        const float K = 108f;                        // = `LayoutSpace.DesignPxH / DesignHeight`
        float hw = r.width * 0.5f * K, hh = r.height * 0.5f * K;
        return new PxRect(c.x - hw, c.y - hh, c.x + hw, c.y + hh);
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

    static bool SameRect(PxRect a, PxRect b, float tol)
    {
        return Mathf.Abs(a.x1 - b.x1) <= tol && Mathf.Abs(a.y1 - b.y1) <= tol
            && Mathf.Abs(a.x2 - b.x2) <= tol && Mathf.Abs(a.y2 - b.y2) <= tol;
    }

    static void RectIs(PxRect got, PxRect want, float tol, string msg)
    {
        bool ok = SameRect(got, want, tol);
        Tr(ok, msg + "｜实得 (" + got.x1.ToString("F2") + "," + got.y1.ToString("F2") + ")-("
             + got.x2.ToString("F2") + "," + got.y2.ToString("F2") + ") " + got.W.ToString("F2") + "×"
             + got.H.ToString("F2"));
    }
}
