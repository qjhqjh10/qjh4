// B114_W11_Assert.cs — 第一百一十四会话 · 第 1 波写手 `W-11`（2026-11-01）· 账 `B-242` 的断言
//
// ============================ 它盯的是什么 ============================
//  基础档 `Avatar Drawer` 原先在画**原版看不见的阵营名**：
//    原版 `AvatarDrawer.Draw`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarDrawer.cs:15-24`）只有三句 ——
//      `image.sprite = item.Image;`
//      `if (armyText) { armyText.text = ArmyUtilities.GetLocalizedArmyName(item.cardArmy); }`
//    **一次 `SetActive` 都没调**；而 prefab 里 `Content/Army` 这颗 GameObject 是 **`m_IsActive: 0`**
//    （`Avatar Drawer.prefab` 的 `!u!1 &1618682377417806` · `Icon Avatar Drawer Variant.prefab` 的
//     `!u!1 &1724804207875695`，两份逐字现读）⇒ 文本写进去了，屏幕上**不出现**。
//  本件断的是「基础档这一层的显隐」，⛔ 不断「某个方法被调过」。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A4（结论）断 `Army.activeSelf == false`：把 `Shell/ItemDrawerAvatar.cs` 里那句
//    `armyNode.gameObject.SetActive(false);` **删掉** ⇒ 红（那正好是改动前的老行为）。
//  · A3（前提）断 `Army` 节点**在**：把整层删掉（连节点都不建）也红 —— 原版结构里那颗节点是存在的。
//  · A6（结构上不可能同时满足的那一半）：**同一个** `DrawerArmyName` 在 `TitleDrawer` 那一档会**真建出并激活**
//    `Army` 节点（同夹具 / 同 `cardArmy` / 同 `ItemDrawerStyle`）⇒ 证明 A3/A4 不是「这个版式压根建不出文字层」的空转。
//  · A5 组：底板 / 主图 / 名字行**都还在** ⇒ 若有人顺手把整层都关掉（不只阵营名）⇒ 红。
//
// ============================ 怎么接（⚠️ 只由调度台/本波写手改宿主）============================
//  唯一入口 = `B114_W11_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行**同一物理行、第一个 `//` 之前**追加：
//      B114_W11_Assert.Run(); _sink.Pass += B114_W11_Assert.Pass; _sink.Fail += B114_W11_Assert.Fail;
//
// ============================ 边界（如实记）============================
//  · **`Avatar Drawer Shop Variant` 不在本件覆盖内**：那份 `Army` 是 `m_IsActive: 1`（现读）⇒ 原版在商店
//    那一档**会**显示阵营名。照做要按 `DrawerOverride.Shop` 分流，而 `Avatar(...)` 拿不到 `ov`
//    （调用点 `ItemDrawer.cs:1925` 那条聚合行不传）⇒ 本波未接，已写进报告「没查清 / 未接的部分」。
//  · 本件只建临时 GameObject / 只读，**不写盘**。
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-11`：账 `B-242` —— 基础档 `Avatar Drawer` 不显示阵营名（`Content/Army` 的 `m_IsActive = 0`）。</summary>
public static class B114_W11_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W11] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>基础档 `Avatar Drawer` 的根 = 712.8×1080（prefab 现读）⇒ 夹具直接给这个框（`FrameOf` 的 `K = 1`）。</summary>
    static readonly PxRect BoxBase = new PxRect(0f, 0f, 712.8f, 1080f);
    /// <summary>头像图（`Resources/Art/avatars/`，与 `B114_C` 同一张 —— 那张是现成可取的）。</summary>
    const string ArtAvatar = "Avatar_AM_Attilan Rough Rider";
    /// <summary>夹具阵营 = `Ultramarines`(10)（`ArmyIcons.LocalizedName` 会给出非空串 ⇒ 「不画」才有意义）。</summary>
    const int Army = 10;

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W11（B-242）：基础档 `Avatar Drawer` 不显示阵营名（`Content/Army` 的 `m_IsActive = 0`）");

        SectionBaseAvatar();
        SectionWitness();

        Debug.Log(_sink.P + "B114W11 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ 基础档 Avatar Drawer
    static void SectionBaseAvatar()
    {
        var tex = CardArt.Cosmetics(ArtAvatar);
        Tr(tex != null, "A0 **前提**：头像图 `" + ArtAvatar + "` 取得到（`CardArt.Cosmetics`）"
                      + "｜🧨 删掉 `Resources/Art/avatars/" + ArtAvatar + ".png` ⇒ 红");
        string armyName = ArmyIcons.LocalizedName(Army);
        Tr(!string.IsNullOrEmpty(armyName),
           "A0b **前提**：`ArmyIcons.LocalizedName(" + Army + ")` 非空（实得「" + (armyName ?? "<空>") + "」）"
         + "｜🧨 空串 ⇒ 老的 `DrawerArmyName` 本来也建不出节点 ⇒ 下面「不可见」是空转");
        if (tex == null || string.IsNullOrEmpty(armyName)) return;

        var root = new GameObject("B114W11_AVATAR_BASE");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);      // `QuantityPx 34 / NamePx 26` 都非 0 ⇒ 文字层是**开着**的
            var spec = new ItemSpec
            {
                Id = "B114W11_Avatar", Type = "PlayerAvatar", Kind = ItemKind.Generic,
                Art = ArtAvatar, CardArmy = Army, Label = "B114W11",
            };
            res = ItemDrawer.Draw(root.transform, BoxBase, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "A1 **前提**：抽屉根建出来了");
            C(res.Drawer, ItemDrawer.DrawerAvatar, "A1b **前提**：`ov = Default` 路由到 `AvatarDrawer`（= 基础档 `Avatar Drawer`）");
            if (res.Node == null) return;

            Tr(FindDeep(res.Node, ItemDrawer.NodeContent) != null,
               "A2 **前提**：`Content` 建出来了（原版阵营名与名字行都挂在这颗下面）");

            var army = FindDeep(res.Node, ItemDrawer.NodeArmy);
            Tr(army != null,
               "A3 **前提**：`Army` 节点**建了**（原版 prefab 里这颗节点存在、`Draw` 照写 `.text`；"
             + "我们照做 ⇒ ⛔ 不是「把整层删掉」）");
            Tr(army != null && !army.gameObject.activeSelf,
               "A4 ★★ **结论**：`Army` 的 `activeSelf == false` —— 与原版 `m_IsActive: 0` 同义"
             + " ⇒ 文本写进去了、屏幕上**不出现**阵营名"
             + "｜🧨 删掉 `ItemDrawerAvatar.cs` 里那句 `armyNode.gameObject.SetActive(false);`（= 改动前的老行为）⇒ 红");

            // ---- 本活只动阵营名这一层：其余三层必须原样 ----
            Tr(FindDeep(res.Node, ItemDrawer.NodeBackground) != null, "A5 底板 `Background` 仍在（本活只动阵营名这一层）");
            Tr(FindDeep(res.Node, ItemDrawer.NodeImage) != null, "A5b 主图 `Image` 仍在");
            Tr(FindDeep(res.Node, "Label") != null, "A5c 名字行 `Label` 仍在");
            Tr(FindDeep(res.Node, ItemDrawer.NodeName) != null, "A5d `Label/Name` 仍在");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
        }
    }

    // ============================================================ A6 · 灭自证：换一个抽屉，同一句 `DrawerArmyName` 会建出**激活**的节点
    static void SectionWitness()
    {
        MenuCheck.Section(_sink, "A6 灭自证：同一个 `DrawerArmyName` 在 `TitleDrawer` 那一档会真建出并激活 `Army`");

        var root = new GameObject("B114W11_TITLE_WITNESS");
        ItemDrawResult res = default(ItemDrawResult);
        try
        {
            var st = ItemDrawerStyle.Default(10, 11, 12);          // 与上一节**同一套** style
            var spec = new ItemSpec
            {
                Id = "B114W11_Title", Type = "CosmeticItemTitle", Kind = ItemKind.Generic,
                CardArmy = Army, Label = "B114W11",
            };
            res = ItemDrawer.Draw(root.transform, BoxBase, spec, 1, DrawerOverride.Default, st);

            Tr(res.Node != null, "A6a **前提**：`TitleDrawer` 的根建出来了");
            C(res.Drawer, ItemDrawer.DrawerTitle, "A6b **前提**：路由到 `TitleDrawer`（它的 `Draw` 无条件调 `DrawerArmyName`）");
            if (res.Node == null) return;

            var army = FindDeep(res.Node, ItemDrawer.NodeArmy);
            Tr(army != null && army.gameObject.activeSelf,
               "A6 ★★ 灭自证：**同一个夹具 / 同一个 `cardArmy` / 同一个 `ItemDrawerStyle`**，`TitleDrawer` 那档的 `Army`"
             + " 节点**建出来且 `activeSelf == true`** ⇒ 证明上面 A3/A4 断的不是「这个版式压根建不出文字层」"
             + "｜🧨 把 `DrawerArmyName` 整条弄空（或 `st.NamePx` 改成 0）⇒ 红");
        }
        finally
        {
            if (res.View != null) res.View.Release();
            Object.DestroyImmediate(root);
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
}
