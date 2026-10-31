// B109_E18_RandomCard_Assert.cs — 第一百零九会话 · 执行代理 `B109_E18`
//
// ============================ 它盯的是什么 ============================
//  `Random Card Drawer` 中立档（`cardArmy == Neutral`）的**第二层图**：
//  原版 `Content/Random Image`（父件、圆底 `UI_Button_Round_background`）下面还有一颗子件
//  `Content/Random Image/Icon`（问号 `40k_Rewards_icon__`，62×117）。
//  判据正本 = `资料/完全复刻/判据/B109_R9_资源侧剩余.md` §1；本件按它落成机检，**不重查**。
//
//    A 段（前提 + 资源）：抽屉建得出来、路由到 `RandomCardDrawer`、**图真的进了 `Resources/`**（62×117）
//    B 段（中立档）：`Icon` 建出来 · 是 `ImageQuad` · 贴图名 = `40k_Rewards_icon__` ·
//                    父件是 `Random Image`（原版层级 `Content/Random Image/Icon`）· **画出来的矩形**四项
//    C 段（灭自证）：宽高比 = 62/117 · 若把 `keepAspect` 关掉 ⇒ B4 段必红（见下）
//    D 段（互斥）：`cardArmy != Neutral` ⇒ **不建** `Icon`（原版 `randomArmyIcon.SetActive(false)`）
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · B4/C1：把 `ItemDrawerRandomCard.cs` 的 `keepAspect: true` 改成 `false`
//      ⇒ 画出来的框从 171.69×324 变回 427.2×324 ⇒ B4 的 x1/x2 与 C1 的宽高比**同时**红。
//  · B3：把 `RandomIconSprite` 改成 `40k_general_wildcard_common_small`（= 原版 `cardIcon` 字段那张，
//      ⛔ 原版 `Draw` 根本不读它）⇒ 贴图名断言红（那图也确实不是这一张）。
//  · B2：把 `Icon` 挂回 `content`（不挂 `Random Image` 底下）⇒ 父件名断言红。
//  · D1 与 B1 是一对**结构上不可能同时满足**的断言：把 `Draw` 里的 `army == 0` 判断去掉（两档都画）
//      ⇒ D1 红；改成两档都不画 ⇒ B1 红。
//  · A3：把刚导进来的 `Resources/Art/ui_menu/40k_Rewards_icon__.png` 删掉 ⇒ A3 红（并会同时触发
//      `ItemDrawerRandomCard.DrawRandomIcon` 的那条 `randomcard|icon` 出声）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  唯一入口 = `B109_E18_RandomCard_Assert.Run()`；建议宿主 `Editor/ShellScene.cs`（抽屉全在外壳侧），
//  在该宿主那条聚合行**同一物理行、第一个 `//` 之前**追加：
//      B109_E18_RandomCard_Assert.Run(); pass += B109_E18_RandomCard_Assert.Pass; fail += B109_E18_RandomCard_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · 判据 §1 的「rect = (142.4,540)-(569.6,864)」与 prefab 实测**差两处**（x 少 0.4 px；y 是**向上**的锚点值）。
//    本件按 `menu_dump` 的实测绝对矩形 (142.8,216)-(570.0,540)（本仓 `PxRect` 是左上原点 · y 向下）——
//    差异与本仓口径的关系写在 `Shell/ItemDrawerRandomCard.cs` 的 `RandomIconX1` 那段注释里。
//  · 判据 §1 的「删图 ⇒ 出声计数 +1」**这一条没法机检**：`ItemDrawer.Note` 是 `private static`
//    （进程内去重用的 `HashSet` 也是 private）⇒ 断言侧读不到计数。本件改用 A3「图取得到且尺寸对」
//    做等价前提：图没了 A3 必红，那条 `Note` 同时会响。
//  · 本文件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B109_E18`：`Random Card Drawer` 的 `Content/Random Image/Icon` 问号层。</summary>
public static class B109_E18_RandomCard_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B109E18] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>原版那颗问号图的资产名（prefab `Content/Random Image/Icon.m_Sprite`）。</summary>
    const string IconSprite = "40k_Rewards_icon__";

    // 原版 prefab 实测（`menu_dump` · 绝对设计 px · 左上原点 · y 向下）：
    //   `Content/Random Image` 框 (0.4,108)-(712.4,648)；`Icon` 锚点 (0.2,0.2)-(0.8,0.8) 内接 ⇒
    //   (142.8,216)-(570.0,540)，框 427.2×324。
    //   贴图 62×117 + `m_PreserveAspect 1` ⇒ 画出来 **171.69×324**，居中 ⇒ 下面那四个数。
    const float IconBoxX1 = 142.8f, IconBoxY1 = 216.0f, IconBoxX2 = 570.0f, IconBoxY2 = 540.0f;

    /// <summary>本件唯一入口。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B109_E18：`Random Card Drawer` 的 `Content/Random Image/Icon`（问号 62×117）");

        SectionA_Resource();

        // 两个夹具的框都取 712.8×1080（= 抽屉根的设计尺寸）⇒ `FrameOf` 的 K = 1、OX = OY = 0
        //   ⇒ `fr.Map` 出来的就是**设计 px 本身**，断言的期望值不必再叠一次缩放（少一处自证）。
        var box = new PxRect(0f, 0f, ItemDrawer.DrRootW, ItemDrawer.DrRootH);

        SectionB_Neutral(box);
        SectionD_NotNeutral(box);

        Debug.Log(_sink.P + "B109E18 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 前提 + 资源真的进了 `Resources/`
    static void SectionA_Resource()
    {
        MenuCheck.Section(_sink, "A 前提：图在 `Resources/`，且尺寸/出处对得上");

        Texture2D tex = CardArt.MenuUi(IconSprite);
        Tr(tex != null,
           "A3 ★ `CardArt.MenuUi(\"" + IconSprite + "\")` 取得到"
         + "｜🧨 删掉 `Resources/Art/ui_menu/" + IconSprite + ".png` ⇒ 红（`DrawRandomIcon` 同时会出声一次）");
        if (tex == null) return;

        C(tex.width, 62, "A3a 贴图宽 = 62（prefab `m_Rect` / `m_RD.textureRect` 都是 62×117，导入走 `asis` 不补画布）");
        C(tex.height, 117, "A3b 贴图高 = 117");
        Tr(tex.name == IconSprite,
           "A3c 贴图名 = `" + IconSprite + "`（实际 " + tex.name + "）—— 同名不同图在解包里很常见，这里顺带钉一下");
    }

    // ============================================================ B · 中立档：`Icon` 建出来且几何对
    static void SectionB_Neutral(PxRect box)
    {
        MenuCheck.Section(_sink, "B 中立档（`cardArmy == 0`）：`Content/Random Image/Icon` 建出来 + 画出来的矩形");

        var root = new GameObject("B109E18_RANDOMCARD_NEUTRAL_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;      // 文字层不参与本项（`DrawerArmyName` 也吃 `NamePx` ⇒ 一并跳过）
            var spec = new ItemSpec
            {
                Id = "B109E18_RandomCard",
                Type = "DropTableItem",              // 原版类型键 ⇒ `PickDrawer` 落到 `RandomCardDrawer`
                Kind = ItemKind.Generic,
                Rarity = 0,                          // `cardRarity == 0` ⇒ 画 `Base Background` 那一层
                CardArmy = 0,                        // `CardArmy.Neutral` ⇒ 画 `Random Image` + 它的子件 `Icon`
                Label = "B109E18",
            };
            res = ItemDrawer.Draw(root.transform, box, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "B0 **前提**：随机卡抽屉的根建出来了");
            C(res.Drawer, ItemDrawer.DrawerRandomCard,
              "B0b **前提**：`PickDrawer` 真的落到 `RandomCardDrawer`（`ItemTypeSets` 那张真表那一跳）");
            if (res.Node == null) return;

            Transform randomImage = FindDeep(res.Node, "Random Image");
            Tr(randomImage != null,
               "B1a **前提**：父件 `Random Image` 建出来了（中立档画圆底 `UI_Button_Round_background`）");

            Transform icon = FindDeep(res.Node, "Icon");
            Tr(icon != null,
               "B1 ★ `Icon` 建出来了（原版 `Content/Random Image/Icon`，`m_Sprite = " + IconSprite + "`）"
             + "｜🧨 两档都不画 ⇒ 红");
            if (icon == null) return;

            var iq = icon.GetComponent<ImageQuad>();
            Tr(iq != null, "B2 **前提**：`Icon` 节点上就是 `ImageQuad`（`MenuDraw.Rect` 的落地件）");
            if (iq == null) return;

            C(iq.Texture != null ? iq.Texture.name : "<null>", IconSprite,
              "B3 ★ `Icon` 的贴图名 = `" + IconSprite + "`"
            + "｜🧨 换成原版 `cardIcon` 字段那张 `40k_general_wildcard_common_small` ⇒ 红（原版 `Draw` 不读它）");

            Tr(icon.parent != null && icon.parent.name == "Random Image",
               "B4a ★ `Icon` 的父件名 = `Random Image`（原版层级 `Content/Random Image/Icon`）"
             + "｜🧨 把 `Icon` 挂回 `content` ⇒ 红（实际父件 = " + (icon.parent != null ? icon.parent.name : "<null>") + "）");

            // 画出来的矩形（`keepAspect` 之后那块画心，不是 prefab 的框）。
            //   框 (142.8,216)-(570.0,540) = 427.2×324；贴图 62:117 内接 ⇒ 高定 324、宽 = 324 × 62/117 = 171.69，
            //   居中 ⇒ x 356.4 ± 85.85 = 270.55 / 442.25。
            float w = 324f * 62f / 117f;
            float cx = (IconBoxX1 + IconBoxX2) * 0.5f;
            PxRect qr = MenuDraw.QuadRectPx(iq);
            N(qr.x1, cx - w * 0.5f, 0.5f, "B4b ★ `Icon` 画出来 x1 = " + (cx - w * 0.5f).ToString("F2")
              + "（框 142.8 → `m_PreserveAspect` 内接后居中）｜🧨 关掉 `keepAspect` ⇒ 得 142.8 ⇒ 红");
            N(qr.y1, IconBoxY1, 0.5f, "B4c ★ `Icon` 画出来 y1 = 216.0（框上沿；高定档 ⇒ 竖直方向不缩）");
            N(qr.x2, cx + w * 0.5f, 0.5f, "B4d ★ `Icon` 画出来 x2 = " + (cx + w * 0.5f).ToString("F2"));
            N(qr.y2, IconBoxY2, 0.5f, "B4e ★ `Icon` 画出来 y2 = 540.0（框下沿）");

            // C · 灭自证：宽高比只有「按贴图比例内接」才会是 62/117；直接拉伸会得框比例 1.3185。
            N(iq.WorldW / Mathf.Max(1e-6f, iq.WorldH), 62f / 117f, 0.002f,
              "C1 ★ `Icon` 的宽高比 = 62/117 = 0.5299（`keepAspect` 生效）"
            + "｜🧨 关掉 `keepAspect` ⇒ 变成框比例 1.3185 ⇒ 红");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ D · 互斥：非中立档不建 `Icon`
    static void SectionD_NotNeutral(PxRect box)
    {
        MenuCheck.Section(_sink, "D 互斥（`cardArmy != Neutral`）：`Icon` **不建**（原版 `randomArmyIcon.SetActive(false)`）");

        var root = new GameObject("B109E18_RANDOMCARD_ARMY_ROOT");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);
            st.NamePx = 0f; st.QuantityPx = 0f;
            var spec = new ItemSpec
            {
                Id = "B109E18_RandomCardArmy",
                Type = "DropTableItem",
                Kind = ItemKind.Generic,
                Rarity = 0,
                CardArmy = 10,                       // `CardArmy.Ultramarines` ≠ Neutral
                Label = "B109E18",
            };
            res = ItemDrawer.Draw(root.transform, box, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "D0 **前提**：非中立档的抽屉根也建出来了");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, "Icon") == null,
               "D1 ★ `cardArmy = 10` ⇒ **不建** `Icon`（原版 `RandomImage` 整件 `SetActive(false)`，子件跟着不画）"
             + "｜🧨 去掉 `RandomCard` 里的 `army == 0` 判断（两档都画）⇒ 红");
            Tr(FindDeep(res.Node, "Random Image") == null,
               "D2 `cardArmy = 10` ⇒ 也不建 `Random Image`（互斥的两支：`Army` 徽记那一支接管右半边）");
        }
        finally
        {
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
}
