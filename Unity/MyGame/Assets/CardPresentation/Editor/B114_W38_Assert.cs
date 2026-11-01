// B114_W38_Assert.cs — 第一百一十四会话 · 写手 `W-38`（2026-11-01）· 账 `B-280` / `B-281` / `B-282`
//
// ============================ 它盯的是什么 ============================
//  ① **`B-280`：`Title Drawer` 的 `Content/Army` 垂直档 = `Center/Bottom`。**
//     真包现读（`menu_dump.py bundle_menus_assets_all "Title Drawer"`）= 对齐 `Center/**Bottom**`；
//     对照 `Avatar Drawer` 的 `Army` = `Center/Middle`（**Middle 是通则**）。
//     我们原来一颗都不设（出厂 `Middle`）⇒ `TitleDrawer` 那行字比原版低/高一块（108 设计 px 高的框里 ≈36px）。
//  ② **`B-281`：死码 `IconAllianceBadge` 已删。**
//     `B114_W35`（`B-277`）在 `Dispatch` 第一句插了 `IconAllianceBadgeFixed` 分流、`IconVariant` 只从那一行被调
//     ⇒ 旧实现（`frameR = box` = 跟框画）**一个调用点都没有**。删掉之后 `IconVariant` **只服务其余 6 族**。
//  ③ **`B-282①`：`Title Drawer` 的 `Content/Background` 是 `m_PreserveAspect = 1`**（我们原先拉伸）。
//  ④ **`B-282②`：`Icon` 档那颗徽章节点原版叫 `Badge`**（我们原先沿用了基础档的 `Trophy Icon`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · ① 的 A 段读的是**标签身上实际生效的那一档**（`Label.VAlignTier` —— 点阵后端下它如实报 `Middle`），
//    ⛔ 不是读 `ItemDrawerTitle` 里的常量。A/B 是一对：A 断 Title = Bottom、B 断 Avatar = Middle
//    ⇒ 🧨 把垂直档做成「全抽屉一刀切」⇒ B 红；把 `Title` 那两句删回去 ⇒ A 红。
//    A4 再加一条**结构性反证**：两档真正施加的垂直位移（`Label.VOffsetWorld`）必须不等
//    —— ⛔ 不拿它当期望值（W35 那份报告 B 段的纪律），只用它判「两处不是同一个档」。
//  · ② 用**反射查方法名**：`IconAllianceBadge` 必须 0 命中（有人把它加回来 ⇒ 红）；
//    B2 再断「`IconVariant` 里剩下那 6 族仍然按 Icon 几何画」——用 **Avatar 档的 1.57× 过扫**当哨兵
//    （基础档会画成 `Content/Image` 那一条 712×432 的横带）。
//  · ③ 的期望值 = **两张图自己的比例**（Title `329/499` · Avatar `328/498`，都是 prefab sprite 尺寸），⛔ 不是读生产常量。
//    单看几何分不出来（框的比例 712/1080 与 329/499 只差 **0.009%**）⇒ 靠 C1 这条**分辨力前提**
//    证明本测试真有分辨力（`|329/499 − 712/1080| > 2e-5`，而实测容差 = 2e-5）。
//    🔴 **2026-11-01 `B114_W43`（`B-284`）就地订正（铁律 5）**：C5 原来断的是「`Avatar Drawer` 本波没越界（**仍是拉伸**）」——
//    那是**当时**的改动范围哨兵。`B-284` 已把 `DrawerBackground` 的 `keepAspect` 默认值升成 `true`（真包逐份现读：
//    全族 **11 份** `Content/Background` 都是 `m_PreserveAspect 1`）⇒ 现在 C3/C5 **两档都断 sprite 比例**：
//    C3 = `Title`（**显式**传 `true` 那一档）· C5 = `Avatar`（**吃默认值**那一档）
//    ⇒ 成对之后「默认值被改回 `false`」只让 **C5 红、C3 仍绿**（两者一起红/一起绿才说明这条没分辨力）。
//  · ④ D 段：`Icon` 档根下**必须有 `Badge`、必须没有 `Trophy Icon`**；D2 是**对照组**（基础档反过来）。
//    🧨 只改名不改基础档 ⇒ D2 红；只改基础档 ⇒ D1 红。两处同时改成同一个名字才会“全绿”，
//    所以 D3 再加一条结构反证：**同一批调用里两套 prefab 的节点名必须不同**。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W38_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W38_Assert.Run(); _sink.Pass += B114_W38_Assert.Pass; _sink.Fail += B114_W38_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 本节只建临时 GameObject / 只读，**不写盘**、不跑场景。
//  · ① **没有实拍可比**（原版已关服）—— 断的是 prefab 字段那一档，不是「画面上看起来对不对」。
//  · ③ 的两张图比例差 0.009% ⇒ **改 PA 今天不改变画面**；本段断的是「按原版那颗的 PA 走」，
//    也就是「换图/换框时不会再露馅」。⚠️ 实测容差 2e-5 是**故意卡得比两图差值（5.9e-5）紧**的。
using System.Reflection;
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-38`：`B-280`（Title Army 垂直档）+ `B-281`（死码删除）+ `B-282`（Title 底板 PA / Icon 档徽章节点名）。</summary>
public static class B114_W38_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W38] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（⛔ 一律从**真包** / prefab 现读，不从生产代码反读）----
    /// <summary>`Title Drawer` 的 `Content/Background.m_Sprite` 尺寸（真包 329×499）。</summary>
    const float JudgeBgW = 329f, JudgeBgH = 499f;
    /// <summary>抽屉底框的设计比例（`Content` 712×1080 ⇒ `Content/Background` 铺满那一格）。</summary>
    const float BoxBgW = 712f, BoxBgH = 1080f;
    /// <summary>**基础档**共享底图 `40k_Rewards_icon_base card` 的尺寸（真包 prefab 现读 328×498；
    /// `Resources/Art/ui_menu/40k_Rewards_icon_base_card.png` 的 IHDR 同值）。
    /// 2026-11-01 `B114_W43`（`B-284`）：全族 PA 之后 **`Avatar Drawer` 走默认值那条路**也按它画 ⇒ C5 的期望值。</summary>
    const float JudgeBaseBgW = 328f, JudgeBaseBgH = 498f;
    /// <summary>`Title Drawer` 的 `Content/Background.m_Sprite`（落盘名）。</summary>
    const string JudgeTitleBg = "40k_Rewards_icon_title_card";
    /// <summary>Icon 档 `Alliance Badge Drawer Icon` 根的直接子件名（真包 prefab `&224709867586078768`）。</summary>
    const string JudgeIconBadgeNode = "Badge";
    /// <summary>基础档 `Alliance Badge Drawer` 那颗的名字（`Content/Frame/Trophy Icon`）。</summary>
    const string JudgeBaseBadgeNode = "Trophy Icon";

    // ---- 夹具 ----
    /// <summary>基础档根的尺寸（`K = 1` ⇒ 设计 px 与世界 px 1:1，读数不用乘）。</summary>
    static readonly PxRect BoxRoot = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>Icon 档的方框（1080²，`K = 1`）—— ② 的哨兵与 ④ 的夹具都用它。</summary>
    static readonly PxRect Box1080 = new PxRect(0f, 0f, 1080f, 1080f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W38（B-280 / B-281 / B-282）：Title Army 垂直档 + 死码删除 + 底板 PA + Icon 档节点名");

        // ⚠️ 四段各自独立：某一段「前提不成立就 return」不会把别的段吞掉
        SectionArmyVAlign();
        SectionDeadCode();
        SectionTitleBackgroundAspect();
        SectionIconBadgeNodeName();

        Debug.Log(_sink.P + "B114W38 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ ① `B-280`：Title 的 Army 垂直档 = Bottom
    static void SectionArmyVAlign()
    {
        MenuCheck.Section(_sink, "① `B-280` `Title Drawer` 的 `Army` 垂直档 = **Center/Bottom**（通则 `Middle` 见 `Avatar Drawer`）");

        Tr(ArmyIcons.Texture(10) != null && !string.IsNullOrEmpty(ArmyIcons.LocalizedName(10)),
           "A0 **前提**：阵营 `CardArmy 10` 有徽记图与本地化名（否则 `DrawerArmyName` 早退、不建 `Army` 节点）");
        if (ArmyIcons.Texture(10) == null || string.IsNullOrEmpty(ArmyIcons.LocalizedName(10))) return;

        Label titleLb, avatarLb;

        var root1 = new GameObject("B114W38_TITLE_ARMY_VALIGN");
        ItemDrawResult res1 = default(ItemDrawResult);
        try
        {
            res1 = ItemDrawer.Draw(root1.transform, BoxRoot, SpecTitle(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(10, 11, 12));
            C(res1.Drawer, ItemDrawer.DrawerTitle,
              "A1 **前提**：`CosmeticItemTitle` + `Default`(0) 路由到竖卡 `TitleDrawer`");
            var army = res1.Node == null ? null : FindDeep(res1.Node, ItemDrawer.NodeArmy);
            Tr(army != null, "A1b **前提**：`Title Drawer` 建出了 `Content/Army`");
            titleLb = army == null ? null : army.GetComponent<Label>();
            Tr(titleLb != null, "A1c **前提**：`Army` 那一件上挂的是 `Label`（TMP 后端）");
            if (titleLb == null) return;

            C(titleLb.VAlignTier, Label.VAlign.Bottom,
              "A2 ★★ `Title Drawer` 的 `Army` 垂直档 = **Bottom**（真包现读对齐 `Center/Bottom`）"
            + "｜🧨 删掉 `Title(...)` 里那句 `MenuDraw.SetVAlign(lb, Label.VAlign.Bottom, …)` ⇒ 读回出厂 `Middle` ⇒ 红");
        }
        finally
        {
            if (res1.View != null) res1.View.Release();
            Object.DestroyImmediate(root1);
        }

        // —— 对照组：`Avatar Drawer` 的 Army **必须还是 Middle**（2026-11-01 真包现读 `Center/Middle`）——
        var root2 = new GameObject("B114W38_AVATAR_ARMY_VALIGN");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            res2 = ItemDrawer.Draw(root2.transform, BoxRoot, SpecAvatar(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(20, 21, 22));
            var army = res2.Node == null ? null : FindDeep(res2.Node, ItemDrawer.NodeArmy);
            Tr(army != null, "A3 **前提（对照）**：`Avatar Drawer` 也建出了 `Content/Army`（它建完会 `SetActive(false)`，节点仍在）");
            avatarLb = army == null ? null : army.GetComponent<Label>();
            Tr(avatarLb != null, "A3b **前提（对照）**：那一件也是 `Label`");
            if (avatarLb == null) return;

            C(avatarLb.VAlignTier, Label.VAlign.Middle,
              "B1 ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Army` 垂直档仍是 **Middle**（真包现读 `Center/Middle`）"
            + "｜🧨 有人把 `DrawerArmyName` 的出厂档直接改成 Bottom（一刀切）⇒ 这里红");

            // —— 结构性反证：两处**真正施加的垂直位移**必须不等 ——
            // ⚠️ 只判「不等」，⛔ 不拿 `VOffsetWorld` 当期望值（那是拿我们的算式证明我们）。
            float d = Mathf.Abs(titleLb.VOffsetWorld - avatarLb.VOffsetWorld);
            Tr(d > 0.001f,
               "B2 ★★ 结构反证：同一次运行里 `Title` 的 Army 位移 = " + titleLb.VOffsetWorld.ToString("F5")
             + " vs `Avatar` 的 Army 位移 = " + avatarLb.VOffsetWorld.ToString("F5")
             + "（差 " + d.ToString("F5") + " > 0）⇒ **这是按档设的例外，不是全工程一刀切**"
             + "｜🧨 两处读到同一个数 ⇒ 红（说明垂直档根本没按档设）");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(root2);
        }
    }

    // ============================================================ ② `B-281`：死码 `IconAllianceBadge` 已删
    static void SectionDeadCode()
    {
        MenuCheck.Section(_sink, "② `B-281` 死码 `IconAllianceBadge` 已删；`IconVariant` 仍服务其余 6 族");

        var dead = typeof(ItemDrawer).GetMethod("IconAllianceBadge",
                       BindingFlags.NonPublic | BindingFlags.Static);
        Tr(dead == null,
           "B0 ★★ `ItemDrawer.IconAllianceBadge` **已不存在**（`B114_W35` 接上 `IconAllianceBadgeFixed` 分流之后它就没有调用点了）"
         + "｜🧨 有人把它加回来（＝两份几何并存、其中一份是跟框画的错版）⇒ 红");

        var repl = typeof(ItemDrawer).GetMethod("IconAllianceBadgeFixed",
                       BindingFlags.NonPublic | BindingFlags.Static);
        Tr(repl != null,
           "B0b **前提**：替换件 `ItemDrawer.IconAllianceBadgeFixed` 在（`B114_W35` 的固定尺寸根那条路）"
         + "｜🧨 连替换件都没了 ⇒ 这一档整段回落基础档几何 ⇒ 红");

        var named = typeof(ItemDrawer).GetMethod("IconAllianceBadgeFixedNamed",
                       BindingFlags.NonPublic | BindingFlags.Static);
        Tr(named != null,
           "B0c **前提**：`ItemDrawer.IconAllianceBadgeFixedNamed` 在（`Dispatch` 现在分流到它 —— 见 ④ 的节点名）");

        // —— 哨兵：`IconVariant` 剩下那 6 族**仍然按 Icon 几何画** ——
        // 取 `Avatar` 档：Icon 变体里 `avatar` 锚 -0.285..1.285 × -0.195..1.38（1.57× 过扫），
        // 而基础档画的是 `Content/Image` 那条 (0.4,216)-(712.4,648) = 712×432 的横带 ⇒ 两者一眼可分。
        var probe = DrawIconAvatar();
        try
        {
            Tr(probe.Ok, "B1 **前提**：Icon 档的 `Avatar Drawer` 画出了主图");
            if (!probe.Ok) return;
            Tr(probe.R.W > 1000f && probe.R.H > 1000f,
               "B2 ★★ **灭自证（剩余 6 族还在）**：`Icon Avatar Drawer Variant` 画出来 " + probe.R.W.ToString("F1")
             + "×" + probe.R.H.ToString("F1") + "（> 1080² 的过扫）⇒ `IconVariant` 删掉 Alliance 那一支之后**仍然服务其余各族**"
             + "｜🧨 把 `IconVariant` 整个删掉（回到基础档）⇒ 这里变成 712×432 的横带 ⇒ 红");
        }
        finally { probe.Release(); }
    }

    // ============================================================ ③ `B-282①`：Title 的底板 `m_PreserveAspect = 1`
    static void SectionTitleBackgroundAspect()
    {
        MenuCheck.Section(_sink, "③ `B-282①` `Title Drawer` 的 `Background` 按 **`m_PreserveAspect = 1`** 画（我们原先拉伸）");

        Tr(CardArt.MenuUi(JudgeTitleBg) != null,
           "C0 **前提**：Title 专属底板 `" + JudgeTitleBg + "` 取得到（`Resources/Art/ui_menu/`）");
        if (CardArt.MenuUi(JudgeTitleBg) == null) return;

        // —— 结构性反证：本测试**真有分辨力** —— 两个候选比例只差 0.009%，
        //    所以容差必须比那个差值更紧，否则「拉伸」也会绿（自证）。
        float sprA = JudgeBgW / JudgeBgH, boxA = BoxBgW / BoxBgH;
        Tr(Mathf.Abs(sprA - boxA) > 2e-5f,
           "C1 ★★ **分辨力前提**：sprite 比例 " + sprA.ToString("F7") + " 与框比例 " + boxA.ToString("F7")
         + " 相差 " + Mathf.Abs(sprA - boxA).ToString("F7") + " > 2e-5（= 本段的实测容差）"
         + " ⇒ 「PA」与「拉伸」在几何上**可区分**"
         + "｜🧨 换了一张与本框同比例的新图 ⇒ 这一段失去分辨力 ⇒ 红（那时应当重写期望而不是放宽容差）");

        var title = DrawBackgroundAspect(ItemDrawer.DrawerTitle, SpecTitle(), BoxRoot, "B114W38_TITLE_BG");
        var avatar = DrawBackgroundAspect(ItemDrawer.DrawerAvatar, SpecAvatar(), BoxRoot, "B114W38_AVATAR_BG");
        try
        {
            Tr(title.Ok, "C2 **前提**：`Title Drawer` 的 `Content/Background` 建出来且带贴图");
            if (title.Ok)
            {
                N(title.Aspect, sprA, 2e-5f,
                  "C3 ★★ `Title Drawer` 的底板**按 sprite 自己的比例** " + sprA.ToString("F7")
                + " 画（实测 " + title.Aspect.ToString("F7") + "）；框的比例是 " + boxA.ToString("F7")
                + "（差 " + Mathf.Abs(sprA - boxA).ToString("F7") + " ≈ 0.1 px，**肉眼无差**）"
                + "｜🧨 把 `DrawerBackground(..., TitleBgSprite)` 的第 5 个实参去掉（回到拉伸）⇒ 实测变成 "
                + boxA.ToString("F7") + " ⇒ 红");
            }

            Tr(avatar.Ok, "C4 **前提（对照）**：`Avatar Drawer` 的底板也建出来了");
            if (avatar.Ok)
            {
                // `Avatar` 那一档**不显式传 `keepAspect`** ⇒ 它量到的就是 `DrawerBackground` 的**默认值**这一条路。
                float baseA = JudgeBaseBgW / JudgeBaseBgH;
                N(avatar.Aspect, baseA, 2e-5f,
                  "C5 ★★ **全族 PA（`B-284`）**：`Avatar Drawer` 的底板 —— 它**不显式传 `keepAspect`**、吃的是共用默认值 —— "
                + "也按**自己那张 sprite 的比例** " + baseA.ToString("F7") + " 画（实测 " + avatar.Aspect.ToString("F7") + "）；"
                + "框的比例是 " + boxA.ToString("F7") + "（差 " + Mathf.Abs(baseA - boxA).ToString("F7") + " ≈ 0.68 px）"
                + "｜🧨 把 `DrawerBackground` 的 `keepAspect` 默认值改回 `false`（＝回到全族拉伸）⇒ 实测变成 "
                + boxA.ToString("F7") + " ⇒ 红；**此时 C3 仍绿**（`Title` 那一档是显式传 `true` 的）"
                + " ⇒ 两条成对证明本条盯的是**共用默认值那条路**，不是某一档的特例"
                + "｜📌 判据：真包逐份现读，全族 **11 份** `Content/Background` 都是 `m_PreserveAspect 1`"
                + "（Avatar/Container/Premium/ForgePoints/Alliance/XSolla/AvatarBorder/RandomCard/Currency/Title/CampaignPoints）");
            }
        }
        finally
        {
            title.Release();
            avatar.Release();
        }
    }

    // ============================================================ ④ `B-282②`：Icon 档的徽章节点名 = `Badge`
    static void SectionIconBadgeNodeName()
    {
        MenuCheck.Section(_sink, "④ `B-282②` `Alliance Badge Drawer Icon` 的徽章节点名 = **`Badge`**（基础档才是 `Trophy Icon`）");

        Tr(AllianceBadgeArt.Default != null,
           "D0 **前提**：徽章默认图取得到（`Resources/Art/ui_menu/`，未知 id 会回落到它）");
        if (AllianceBadgeArt.Default == null) return;

        bool iconHasBadge; bool iconHasTrophy;
        var r1 = new GameObject("B114W38_BADGE_ICON_NAME");
        ItemDrawResult res1 = default(ItemDrawResult);
        try
        {
            res1 = ItemDrawer.Draw(r1.transform, Box1080, SpecBadge(), 1, DrawerOverride.Icon,
                                   ItemDrawerStyle.Default(30, 31, 32));
            Tr(res1.Node != null, "D1 **前提**：Icon 档徽章抽屉的根建出来了");
            if (res1.Node == null) return;
            iconHasBadge = FindDeep(res1.Node, JudgeIconBadgeNode) != null;
            iconHasTrophy = FindDeep(res1.Node, JudgeBaseBadgeNode) != null;

            Tr(iconHasBadge,
               "D2 ★★ Icon 档根下**有 `" + JudgeIconBadgeNode + "`**（真包 prefab `&224709867586078768`）"
             + "｜🧨 沿用基础档的 `Trophy Icon`（= 改名没做）⇒ 这里找不到 ⇒ 红");
            Tr(!iconHasTrophy,
               "D2b ★★ Icon 档根下**没有 `" + JudgeBaseBadgeNode + "`**（那是基础档的名字；本档只有 `Frame` + `Badge` 两颗）"
             + "｜🧨 改名改出一份副本、或压根没改 ⇒ 红");
        }
        finally
        {
            if (res1.View != null) res1.View.Release();
            Object.DestroyImmediate(r1);
        }

        // —— 对照组：**基础档**反过来（`Content/Frame/Trophy Icon`；根下没有 `Badge`）——
        bool baseHasTrophy; bool baseHasBadge;
        var r2 = new GameObject("B114W38_BADGE_BASE_NAME");
        ItemDrawResult res2 = default(ItemDrawResult);
        try
        {
            res2 = ItemDrawer.Draw(r2.transform, BoxRoot, SpecBadge(), 1, DrawerOverride.Default,
                                   ItemDrawerStyle.Default(40, 41, 42));
            Tr(res2.Node != null, "D3 **前提（对照）**：基础档徽章抽屉的根建出来了");
            if (res2.Node == null) return;
            baseHasTrophy = FindDeep(res2.Node, JudgeBaseBadgeNode) != null;
            baseHasBadge = FindDeep(res2.Node, JudgeIconBadgeNode) != null;

            Tr(baseHasTrophy,
               "D4 ★★ **灭自证（通则那一侧）**：基础档 `Alliance Badge Drawer` 那颗仍叫 `" + JudgeBaseBadgeNode
             + "`（真包 `Content/Frame/Trophy Icon`）"
             + "｜🧨 把 `NodeTrophyIcon` 常量直接改名为 Badge（一刀切）⇒ 这里红");
            Tr(!baseHasBadge,
               "D4b ★ 基础档根下**没有** `" + JudgeIconBadgeNode + "`（两套 prefab 各自一个名字，不是同一个通则）");

            // —— 结构性反证：两档**各带自己那一份名字、且都不带对方那份**（= 交叉缺失）——
            //   🔴 2026-11-01 就地订正（铁律 5 · `B114_R9` 诊断 #6）：原来写的是 `iconHasBadge != baseHasTrophy`，
            //   而这两项**按设计都应为 true**（D2 已断言 Icon 档有 `Badge`、D4 已断言基础档有 `Trophy Icon`）
            //   ⇒ `true != true` **恒 false** —— 它红的永远是它自己，跟「改名做没做」无关。
            //   结构判据要的是**交叉缺失**：Icon 档没有基础档那个名字、基础档也没有 Icon 档那个名字。
            Tr(!iconHasTrophy && !baseHasBadge,
               "D5 ★★ 结构反证：Icon 档**没有** `" + JudgeBaseBadgeNode + "`（实得 " + iconHasTrophy
             + "）· 基础档**没有** `" + JudgeIconBadgeNode + "`（实得 " + baseHasBadge + "）⇒ **两档各自一套名字**"
             + "｜🧨 两档读到同一个名字（改名被做成了一刀切 / 改出一份副本）⇒ 红");
        }
        finally
        {
            if (res2.View != null) res2.View.Release();
            Object.DestroyImmediate(r2);
        }
    }

    // ============================================================ 夹具 / 量具
    struct DrawProbe
    {
        public bool Ok;
        public float Aspect;               // 画出来的宽/高
        public PxRect R;                   // 画出来的矩形
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
                                             ItemDrawerStyle.Default(50, 51, 52));
        p.Keep(root.transform, res);
        if (res.Drawer != wantDrawer || res.Node == null) return p;
        var bg = FindDeep(res.Node, ItemDrawer.NodeBackground);
        var q = bg == null ? null : bg.GetComponent<ImageQuad>();
        if (q == null || q.Texture == null) return p;
        var r = MenuDraw.QuadRectPx(q);
        float d = q.WorldW / Mathf.Max(1e-6f, q.WorldH);
        p.R = r; p.Aspect = (r.W > 1e-6f && r.H > 1e-6f) ? r.W / r.H : d; p.Ok = true;
        return p;
    }

    /// <summary>画一次 **Icon 档**的 `Avatar Drawer`（② 的哨兵）。</summary>
    static DrawProbe DrawIconAvatar()
    {
        var p = new DrawProbe();
        var root = new GameObject("B114W38_ICON_AVATAR");
        ItemDrawResult res = ItemDrawer.Draw(root.transform, Box1080, SpecAvatarIcon(), 1, DrawerOverride.Icon,
                                             ItemDrawerStyle.Default(60, 61, 62));
        p.Keep(root.transform, res);
        if (res.Node == null) return p;
        var img = FindDeep(res.Node, ItemDrawer.NodeImage);
        var q = img == null ? null : img.GetComponent<ImageQuad>();
        if (q == null || q.Texture == null) return p;
        var r = MenuDraw.QuadRectPx(q);
        p.R = r; p.Aspect = r.W / Mathf.Max(1e-6f, r.H); p.Ok = true;
        return p;
    }

    static ItemSpec SpecTitle()
    {
        return new ItemSpec
        {
            Id = "B114W38_Title", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
            CardArmy = 10, Label = "B114W38", Title = "B114W38 Title",
        };
    }

    static ItemSpec SpecAvatar()
    {
        return new ItemSpec
        {
            Id = "B114W38_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
            CardArmy = 10, Art = "Player_Profile_Border", Label = "B114W38",
        };
    }

    /// <summary>`Icon` 档的 Avatar 夹具 —— 图名取一张**确定在 `Resources/Art/avatars/` 里**的
    /// （同族先例 = `B114_C_Assert` 的哨兵，那边写死了同一串）。</summary>
    static ItemSpec SpecAvatarIcon()
    {
        return new ItemSpec
        {
            Id = "B114W38_AvatarIcon", Type = "PlayerAvatar", Kind = ItemKind.Generic,
            CardArmy = 10, Art = "Avatar_AM_Attilan Rough Rider", Label = "B114W38",
        };
    }

    static ItemSpec SpecBadge()
    {
        return new ItemSpec
        {
            Id = "B114W38_Badge", Type = "AllianceTrophyData", Kind = ItemKind.Generic,
            BadgeId = "UM_Forge_Badge", Label = "B114W38",
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
