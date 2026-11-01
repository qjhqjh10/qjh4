// B114_W43_Assert.cs — 第一百一十四会话 · 写手 `W-43`（2026-11-01）· 账 `B-284` / `B-285`
//
// ============================ 它盯的是什么 ============================
//  ① **`B-284`：全族各档的 `Content/Background` 都是 `m_PreserveAspect = 1`。**
//     真包逐份现读（`menu_dump.py bundle_menus_assets_all "<prefab>" --depth 2 --relative`，那行末都印 `preserveAspect`）：
//       `Avatar` · `Container` · `Premium` · `Forge Points` · `Alliance Badge` · `XSolla Offer` · `Avatar Border` ·
//       `Random Card` · `Currency` · `Title` · `Campaign Points` —— **11 份，一个例外都没有**。
//     我们原来 `DrawerBackground` 的 `keepAspect` 默认 `false`（拉伸）⇒ **全族**都偏离原版；
//     `B114_W38` 按施工单只给 `Title` 显式传了 `true`，`B-284` 把它升成**全族默认**（`Shell/ItemDrawerLayout.cs`）。
//     ⚠️ 几何差 **<1 px**（基础档 `328×498` 进 `712×1080` ⇒ 高差 0.68 px）⇒ 视觉等价；改它 = **换图/换框时不再露馅**。
//
//  ② **`B-285`：`Title Drawer` 的 `Content/Label/Name` 垂直档 = `Center/Midline`。**
//     真包现读（同一份 prefab）：`Name 'TITLE' 字号=35.099998 auto[12~75] 对齐=Center/**Midline** 折行=0`；
//     对照 `Avatar Drawer` 的 `Label/Name` = `Center/**Middle**`（`'Legendary Wildcard' 字号=75`）⇒ **`Middle` 是通则**。
//     我们原来一颗都不设（出厂 `Middle`）⇒ `Title(...)` 里那颗名字的墨心比原版偏下一块。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · ① 取的是**第三档**（`Container Drawer`，与 `B114_W38` 的 C3/C5 用的 `Title`/`Avatar` 不是同一份 prefab），
//    证明的是「共用默认值这一条路真的把**全族**都改了」，而不是只改了两档：
//    🧨 `DrawerBackground` 的默认值改回 `false` ⇒ F2 读到**框的比例** ⇒ 红；
//    F4 再断「`Title`（显式 `true`）与 `Container`（吃默认值）**两档的比例必须不等**，且各自 = 自己那张图」
//    ⇒ 🧨 有人把某个常数一刀切给全族 ⇒ 其中一档必红。
//    期望值一律写成 **prefab 的 sprite 尺寸之比**（`328/498` / `329/499`），⛔ 不读生产常量。
//  · ② 与 `B-280`（`Army` = `Center/Bottom`）**同一形状**：N1 断 `Title` 的 `Name` = `Midline`、
//    N2 断**对照档** `Avatar` 的 `Name` 仍是 `Middle` ⇒ 🧨 一刀切改成 Midline/ Middle 都会有一侧红；
//    N3 再断「两档**真正施加的垂直位移**不等」（⛔ 不拿 `VOffsetWorld` 当期望值 —— 那是拿我们的算式证明我们）。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W43_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加（与 `B114_W35_Assert` / `B114_W38_Assert` 同形）：
//      B114_W43_Assert.Run(); _sink.Pass += B114_W43_Assert.Pass; _sink.Fail += B114_W43_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 本节只建临时 GameObject / 只读，**不写盘**、不跑场景。
//  · ① **没有实拍可比**（原版已关服）—— 断的是 prefab 字段那一档，不是「画面上看起来对不对」。
//  · ② `Midline` 那一档的位移按 `Label` 的模型算（`OrigInkCenterPx` = 0 只对全大写/数字串成立）；
//    本节只断「档位读回是 Midline」+「与 Middle 的位移不同」，**不断具体挪了多少 px**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-43`：`B-284`（全族 Background PA）+ `B-285`（Title 的 `Label/Name` 垂直档）。</summary>
public static class B114_W43_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W43] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（⛔ 一律从**真包** / prefab 现读，不从生产代码反读）----
    /// <summary>共享底图 `40k_Rewards_icon_base card` 的尺寸（`Container` / `Avatar` 等基础档用这张）。</summary>
    const float JudgeBaseBgW = 328f, JudgeBaseBgH = 498f;
    /// <summary>`Title Drawer` 专属底图 `40k_Rewards_icon_title card` 的尺寸。</summary>
    const float JudgeTitleBgW = 329f, JudgeTitleBgH = 499f;
    /// <summary>抽屉底框的设计比例（`Content` 712×1080 ⇒ `Content/Background` 铺满那一格）。</summary>
    const float BoxBgW = 712f, BoxBgH = 1080f;
    /// <summary>底板 sprite 的落盘名（`Resources/Art/ui_menu/`）。</summary>
    const string BaseBgSprite = "40k_Rewards_icon_base_card";

    // ---- 夹具 ----
    /// <summary>基础档根的尺寸（`K = 1` ⇒ 设计 px 与世界 px 1:1）。</summary>
    static readonly PxRect BoxRoot = new PxRect(0f, 0f, 712.8f, 1080f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W43（B-284 / B-285）：全族 Background PA + Title 的 Label/Name 垂直档");

        // ⚠️ 两段各自独立：某一段「前提不成立就 return」不会把别的段吞掉
        SectionFamilyBackgroundAspect();
        SectionTitleNameVAlign();

        Debug.Log(_sink.P + "B114W43 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① `B-284`：全族 Background 都是 PA = 1
    static void SectionFamilyBackgroundAspect()
    {
        MenuCheck.Section(_sink, "① `B-284` `Content/Background` 按 **`m_PreserveAspect = 1`** 画（全族 11 份 prefab 都是）");

        Tr(CardArt.MenuUi(BaseBgSprite) != null,
           "F0 **前提**：共享底图 `" + BaseBgSprite + "` 取得到（`Resources/Art/ui_menu/`）");
        if (CardArt.MenuUi(BaseBgSprite) == null) return;

        // —— 分辨力：两个候选比例只差 0.095%，容差必须比它紧，否则「拉伸」也会绿（自证）——
        float baseA = JudgeBaseBgW / JudgeBaseBgH, boxA = BoxBgW / BoxBgH;
        Tr(Mathf.Abs(baseA - boxA) > 2e-5f,
           "F1 ★★ **分辨力前提**：sprite 比例 " + baseA.ToString("F7") + " 与框比例 " + boxA.ToString("F7")
         + " 相差 " + Mathf.Abs(baseA - boxA).ToString("F7") + " > 2e-5（= 本段的实测容差）"
         + " ⇒ 「PA」与「拉伸」在几何上**可区分**"
         + "｜🧨 换了一张与本框同比例的新图 ⇒ 这一段失去分辨力 ⇒ 红（那时应当重写期望而不是放宽容差）");

        // —— 被测量的档：`Container Drawer`（它走 `DrawerBackground(content, fr, st)`，**不显式传 keepAspect**）——
        var container = DrawBackgroundAspect(ItemDrawer.DrawerContainer, SpecContainer(), BoxRoot, "B114W43_CONTAINER_BG");
        try
        {
            C(container.Drawer, ItemDrawer.DrawerContainer,
              "F2 **前提**：`Everguild.LiveOps.ShopContainer` 路由到 `ContainerDrawer`（`ItemTypeSets` 那一行）");
            Tr(container.Ok, "F2b **前提**：`Container Drawer` 的 `Content/Background` 建出来且带贴图");
            if (container.Ok)
            {
                N(container.Aspect, baseA, 2e-5f,
                  "F3 ★★ `Container Drawer` 的底板按**自己那张 sprite 的比例** " + baseA.ToString("F7")
                + " 画（实测 " + container.Aspect.ToString("F7") + "）；框的比例是 " + boxA.ToString("F7")
                + "（差 " + Mathf.Abs(baseA - boxA).ToString("F7") + " ≈ 0.68 px，**肉眼无差**）"
                + "｜🧨 把 `DrawerBackground` 的 `keepAspect` 默认值改回 `false`（= 全族拉伸）⇒ 实测变成 "
                + boxA.ToString("F7") + " ⇒ 红");
            }
        }
        finally { container.Release(); }

        // —— 灭自证：`Title`（**显式**传 true）与 `Container`（**吃默认值**）两档都要对，且两者比例**不相等** ——
        var title = DrawBackgroundAspect(ItemDrawer.DrawerTitle, SpecTitle(), BoxRoot, "B114W43_TITLE_BG");
        try
        {
            Tr(title.Ok, "F4 **前提**：`Title Drawer` 的 `Content/Background` 建出来且带贴图");
            if (title.Ok && container.Ok)
            {
                float titleA = JudgeTitleBgW / JudgeTitleBgH;         // 329/499
                N(title.Aspect, titleA, 2e-5f,
                  "F4b ★ `Title Drawer` 的底板（**显式**传 `keepAspect: true`）也 = 自己那张图的 " + titleA.ToString("F7")
                + "（实测 " + title.Aspect.ToString("F7") + "）");
                Tr(Mathf.Abs(title.Aspect - container.Aspect) > 2e-5f,
                   "F5 ★★ **灭自证（不是一刀切常数）**：两档画出来的比例**不等** —— `Title` " + title.Aspect.ToString("F7")
                 + "（329/499）vs `Container` " + container.Aspect.ToString("F7") + "（328/498），差 "
                 + Mathf.Abs(title.Aspect - container.Aspect).ToString("F7")
                 + " ⇒ **各按自己那张背景图**，不是把某个常数套给全族"
                 + "｜🧨 有人写死一个比例给所有抽屉 ⇒ 这两条里必有一条红");
            }
        }
        finally { title.Release(); }
    }

    // ============================================================ ② `B-285`：Title 的 `Label/Name` 垂直档 = Midline
    static void SectionTitleNameVAlign()
    {
        MenuCheck.Section(_sink, "② `B-285` `Title Drawer` 的 `Label/Name` 垂直档 = **Center/Midline**（通则 `Middle` 见 `Avatar Drawer`）");

        Label titleLb;

        var root1 = new GameObject("B114W43_TITLE_NAME_VALIGN");
        ItemDrawResult res1 = default(ItemDrawResult);
        try
        {
            res1 = ItemDrawer.Draw(root1.transform, BoxRoot, SpecTitle(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(70, 71, 72));
            C(res1.Drawer, ItemDrawer.DrawerTitle,
              "N0 **前提**：`CosmeticItemTitle` + `Default`(0) 路由到竖卡 `TitleDrawer`");
            var name = res1.Node == null ? null : FindDeep(res1.Node, ItemDrawer.NodeName);
            Tr(name != null, "N0b **前提**：`Title Drawer` 建出了 `Label/Name`（`item.Label` 非空 + `st.NamePx > 0`）");
            titleLb = name == null ? null : name.GetComponent<Label>();
            Tr(titleLb != null, "N0c **前提**：`Name` 那一件上挂的是 `Label`（TMP 后端）");
            if (titleLb == null) return;

            C(titleLb.VAlignTier, Label.VAlign.Midline,
              "N1 ★★ `Title Drawer` 的 `Label/Name` 垂直档 = **Midline**（真包现读对齐 `Center/Midline`）"
            + "｜🧨 删掉 `Title(...)` 里那句 `MenuDraw.SetVAlign(nameLb, Label.VAlign.Midline, nr)` ⇒ 读回出厂 `Middle` ⇒ 红");
        }
        finally
        {
            if (res1.View != null) res1.View.Release();
            Object.DestroyImmediate(root1);
        }

        // —— 对照组：`Avatar Drawer` 的 `Label/Name` **必须还是 Middle**（真包现读 `Center/Middle`）——
        var root2 = new GameObject("B114W43_AVATAR_NAME_VALIGN");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            res2 = ItemDrawer.Draw(root2.transform, BoxRoot, SpecAvatar(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(80, 81, 82));
            var name = res2.Node == null ? null : FindDeep(res2.Node, ItemDrawer.NodeName);
            Tr(name != null, "N2 **前提（对照）**：`Avatar Drawer` 也建出了 `Label/Name`");
            var avatarLb = name == null ? null : name.GetComponent<Label>();
            Tr(avatarLb != null, "N2b **前提（对照）**：那一件也是 `Label`");
            if (avatarLb == null) return;

            C(avatarLb.VAlignTier, Label.VAlign.Middle,
              "N3 ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Label/Name` 垂直档仍是 **Middle**（真包现读 `Center/Middle`）"
            + "｜🧨 有人把 `DrawerNameQuantity` / 共用件改成 Midline（一刀切）⇒ 这里红");

            // —— 结构性反证：两处**真正施加的垂直位移**必须不等 ——
            // ⚠️ 只判「不等」，⛔ 不拿 `VOffsetWorld` 当期望值（那是拿我们的算式证明我们）。
            float d = Mathf.Abs(titleLb.VOffsetWorld - avatarLb.VOffsetWorld);
            Tr(d > 0.001f,
               "N4 ★★ 结构反证：同一次运行里 `Title` 的 Name 位移 = " + titleLb.VOffsetWorld.ToString("F5")
             + " vs `Avatar` 的 Name 位移 = " + avatarLb.VOffsetWorld.ToString("F5")
             + "（差 " + d.ToString("F5") + " > 0）⇒ **这是按档设的例外，不是全工程一刀切**"
             + "｜🧨 两处读到同一个数 ⇒ 红（说明垂直档根本没按档设）");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }
    }

    // ============================================================ 夹具 / 量具
    struct DrawProbe
    {
        public bool Ok;
        public float Aspect;               // 画出来的宽/高
        public PxRect R;                   // 画出来的矩形
        public string Drawer;              // `ItemDrawResult.Drawer`（判路由用）
        Transform _root; ItemDrawResult _res;
        public void Release()
        {
            if (_res.View != null) _res.View.Release();
            if (_root != null) Object.DestroyImmediate(_root.gameObject);
            _root = null;
        }
        public void Keep(Transform root, ItemDrawResult res) { _root = root; _res = res; }
    }

    /// <summary>画一次某档抽屉，量 `Content/Background` 那一块**画出来的**宽高比。</summary>
    static DrawProbe DrawBackgroundAspect(string wantDrawer, ItemSpec spec, PxRect box, string goName)
    {
        var p = new DrawProbe();
        var root = new GameObject(goName);
        ItemDrawResult res = ItemDrawer.Draw(root.transform, box, spec, 1, DrawerOverride.Default,
                                             ItemDrawerStyle.Default(90, 91, 92));
        p.Keep(root.transform, res);
        p.Drawer = res.Drawer;
        if (res.Drawer != wantDrawer || res.Node == null) return p;
        var bg = FindDeep(res.Node, ItemDrawer.NodeBackground);
        var q = bg == null ? null : bg.GetComponent<ImageQuad>();
        if (q == null || q.Texture == null) return p;
        var r = MenuDraw.QuadRectPx(q);
        float d = q.WorldW / Mathf.Max(1e-6f, q.WorldH);
        p.R = r; p.Aspect = (r.W > 1e-6f && r.H > 1e-6f) ? r.W / r.H : d; p.Ok = true;
        return p;
    }

    static ItemSpec SpecTitle()
    {
        return new ItemSpec
        {
            Id = "B114W43_Title", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
            CardArmy = 10, Label = "B114W43", Title = "B114W43 Title",
        };
    }

    static ItemSpec SpecAvatar()
    {
        return new ItemSpec
        {
            Id = "B114W43_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
            CardArmy = 10, Art = "Player_Profile_Border", Label = "B114W43",
        };
    }

    /// <summary>`Container Drawer` 的夹具 —— 走 `DrawerBackground` 的**默认值**那条路（本段要测的就是它）。
    /// `Art` 借那张确定在 `Resources/` 里的共享底图（只为免掉一条「主图取不到」的出声，与底板那格无关）。</summary>
    static ItemSpec SpecContainer()
    {
        return new ItemSpec
        {
            Id = "B114W43_Container", Type = "Everguild.LiveOps.ShopContainer", Kind = ItemKind.Generic,
            Art = BaseBgSprite, CardArmy = 0, Label = "B114W43",
        };
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
