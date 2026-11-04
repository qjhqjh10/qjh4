// B113X2_Assert.cs — 执行代理 `X2`（2026-11-01 · 第一百一十三会话 · 第 2 波）
//
// ============================ 它盯的是什么 ============================
//  两件：
//   · `B-229` `Shell/AllianceMemberTab.cs` 的 `AllianceDropdownBox` **补建 `Item Checkmark`**
//     （原版 `Item` 的第三颗子件，= `Toggle.graphic`，显隐跟 `_value` 而不是 `_selRow`）；
//   · `B-225` `EverguildButtonMaterialModifier` 在 `ImageQuad` 载体上的等价口
//     = 新建的 `Battle/ImageQuadMaterialModifier.cs`（`SetGreyScale(bool)` 换 / 还 `Everguild/UI/Greyscale` 材质）。
//
// ============================ 逐支为什么必须这么断 ============================
//  A 段（`B-229`）：
//   · A1 结构性 —— 三行**每一行**都有 `Item Checkmark` 这个子节点（少建 = 原版缺一颗件）；
//   · A2 语义 —— 只有 `_value` 那一行 `activeSelf == true`，其余 false；
//   · A3 反例 —— `SetValue` 换一行 ⇒ 勾跟着换（证明它**真读 `_value`**，不是一条写死的常量）；
//   · A4 🔴 **灭自证（这一条才是判别式）** —— 手动调**那一行的 `WindowButton.onDown`**
//     （= `MoveSelectionTo(idx)`，把 `_selRow` 挪走而 `_value` 不动）⇒ **勾必须留在原行**。
//     🧨 谁把实现写成 `i == _selRow`（或 `i == hover`），A4 当场红；只断 A2/A3 是断不出这个的。
//     （判据 = 原版 `Toggle.graphic` 由 `Toggle.isOn` 决定，而 `Dropdown.Show()` 设的是
//       `isOn = (i == value)`；`Selectable.Select()` 只换 `m_SelectedSprite` 那一档底图。）
//
//  B 段（`B-225`）：
//   · B1 前提 —— 原版灰化 shader 取得到（取不到时实现会**保持原样并出声**，那时 B2/B3 无意义）；
//   · B2 换 —— `SetGreyScale(true)` 之后材质**实例**变了（`ReferenceEquals` 前后不同），
//     且新材质的 shader 名 = `Everguild/UI/Greyscale`；
//   · B3 还原 —— `SetGreyScale(false)` 之后**原样还回**换之前那一份实例（不是又新建一份）；
//   · B4 幂等 —— 连点两次 `SetGreyScale(true)` 不重建材质（同一实例）。
//   🧨 谁把实现写成「只置一个布尔、不换材质」（= 原账说的「直接 `AddComponent` 一个 `IMaterialModifier` 充数」那类），
//      B2 当场红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/ShellScene.cs` 或 `RewardsScene.cs` 的社交/锻造那一段，**同一物理行内追加**：
//      B113X2_Assert.Run(); pass += B113X2_Assert.Pass; fail += B113X2_Assert.Fail;
//  ⚠️ A 段会**自建一扇 `SocialWindow`**（同 `B102ALLY_Assert` 的夹具写法）并自拆 ⇒ 任何宿主都能跑；
//     B 段只用 `ImageQuad` + 一张纯色图，不建场景。
//
// ============================ 报告 / 出处 ============================
//  · 报告：`资料/完全复刻/报告/B113X2_勾选与材质修饰器.md`
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `X2`：`B-229`（下拉「当前值」那颗勾）+ `B-225`（`ImageQuadMaterialModifier`）。</summary>
public static class B113X2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B113X2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    /// <summary>跑全部两段。<paramref name="sw"/> 可传宿主**已经开着**的那扇社交窗；传 null 就自己建一扇。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B113X2 · B-229 下拉勾 + B-225 ImageQuad 灰化等价口");

        SocialWindow own = null;
        AllianceDropdownBox box = null;
        ImageQuad probeQuad = null;
        try
        {
            if (sw == null)
            {
                own = SocialWindow.Create(null);
                Tr(own != null, "（前提）`SocialWindow` 建得出来（`Create(null)`，同 `B102ALLY_Assert` 的夹具写法）");
                if (own != null) own.Open();
                sw = own;
            }
            Tr(sw != null, "（前提）社交窗在");
            if (sw != null) { SectionA(ref box, sw); }
            SectionB(ref probeQuad);
        }
        catch (Exception e)
        {
            Tr(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            if (box != null && box.Node != null) MainMenuSubmenuWindow.DestroySafe(box.Node.gameObject);
            if (probeQuad != null) MainMenuSubmenuWindow.DestroySafe(probeQuad.gameObject);
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B113X2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A 段：`B-229`

    static void SectionA(ref AllianceDropdownBox box, SocialWindow sw)
    {
        MenuCheck.Section(_sink, "A 段 · B-229 `Item Checkmark`（原版 `Item` 的第三颗子件）");

        var tab = sw.PageAlliances != null ? sw.PageAlliances.Member : null;
        Tr(tab != null, "（前提）`Alliances` 页的 `AllianceMemberTab` 在（`sw.PageAlliances.Member`）");
        Tr(tab != null && tab.Page != null, "（前提）那支视图的 `Page` 已接上（取图才走得通 `Page.ArtOf`）");
        if (tab == null || tab.Page == null) return;

        // 夹具：**自建一个**三项下拉（不碰真站点 —— 那两格是编辑态才亮的 `Config fields`）。
        // ⚠️ 父件取**窗根**、不取视图根：视图那一侧挂在 `ViewportClip` 下，`MenuDraw.Hit` 会按裁切框
        //    把「整块落在视口外」的命中区**连节点一起不建**（`MenuDraw.Hit` 的 `ClipRect` 早退）⇒
        //    `Item i/Hit` 就取不到、A4 无从下手。窗根本身是 1920×1080 的设计帧，没有那层裁切。
        box = AllianceDropdownBox.Create(tab, sw.transform, "X2 Probe Dropdown",
                                         new PxRect(200f, 200f, 900f, 260f),
                                         new[] { "X2-A", "X2-B", "X2-C" });
        Tr(box != null, "（前提）测试下拉建出来了（`AllianceDropdownBox.Create`）");
        if (box == null) return;
        Tr(box.Node != null, "（前提）它的合上面节点在（`box.Node`）");

        // A0：先有值，再开（原版 `Dropdown.Show()` 就是在开的那一刻按 `value` 设 `isOn`）。
        box.SetValue(1);
        C(box.Value, 1, "A0 夹具：`SetValue(1)` 之后 `Value == 1`");
        box.Open();
        Tr(box.IsOpen, "A0 夹具：`Open()` 之后 `IsOpen`（列表已建）");
        var list = box.ListRoot;
        // 🔴 **2026-11-04（`B142-E18`）**：条目**不再直接挂 `Template`** —— 原版层级是
        //   `Template ▸ Viewport ▸ Content ▸ Item n`（`Social Submenu Variant.prefab`），
        //   `ListRoot` 仍是 `Template`，条目在那下面两层 ⇒ 按路径找件一律走 `ContentRoot`。
        var content = box.ContentRoot;
        Tr(list != null && content != null, "（前提）列表根（原版 `Template`）与条目层（原版 `Template ▸ Viewport ▸ Content`）都建出来了");

        // A1：结构性 —— 三行**每一行**都有一颗 `Item Checkmark` 子件。
        bool allThree = true, anyNull = false;
        for (int i = 0; i < 3; i++)
        {
            var q = box.CheckAt(i);
            if (q == null) { anyNull = true; allThree = false; continue; }
            if (content != null && content.Find("Item " + i + "/Item Checkmark") == null) allThree = false;
        }
        Tr(!anyNull, "A1① 三行的 `CheckAt(i)` 都不是 `null`（勾的渲染件建出来了）");
        Tr(allThree, "A1② 三行各自都有名为 `Item Checkmark` 的子节点（原版 `Item` 的第三颗子件，⛔ 不是只建当前那一行）");

        // A2：语义 —— 只有 `_value` 那一行亮。
        C(box.CheckShownRow, 1, "A2① 当前值 = 1 ⇒ **勾在第 1 行**（`CheckShownRow`）");
        if (box.CheckAt(0) != null) Tr(!box.CheckAt(0).gameObject.activeSelf, "A2② 第 0 行**没有**勾");
        if (box.CheckAt(1) != null) Tr(box.CheckAt(1).gameObject.activeSelf, "A2③ 第 1 行**有**勾");
        if (box.CheckAt(2) != null) Tr(!box.CheckAt(2).gameObject.activeSelf, "A2④ 第 2 行**没有**勾");

        // A3：反例 —— 换一行值，勾跟着换。
        box.SetValue(2);
        C(box.Value, 2, "A3 夹具：`SetValue(2)` 之后 `Value == 2`");
        C(box.CheckShownRow, 2, "A3 反例① 值换到 2 ⇒ **勾跟着换到第 2 行**");
        if (box.CheckAt(1) != null) Tr(!box.CheckAt(1).gameObject.activeSelf, "A3 反例② 第 1 行那颗勾**熄了**（不是两行都亮）");

        // A4：🔴 灭自证 —— 把「选中行」挪走而**值不动**，勾必须钉在原行。
        //   驱动口 = 第 0 行命中区上的 `WindowButton.onDown`（`BuildList` 里接的就是 `MoveSelectionTo(0)`）。
        var hit0 = content != null ? content.Find("Item 0/Hit") : null;
        Tr(hit0 != null, "（前提）第 0 行的命中节点 `Item 0/Hit` 在（A4 要借它的 `onDown`）");
        var wb0 = hit0 != null ? hit0.GetComponent<WindowButton>() : null;
        Tr(wb0 != null, "（前提）那颗命中区上挂着 `WindowButton`（`MoveSelectionTo` 的接线点）");
        if (wb0 != null && wb0.onDown != null) wb0.onDown();
        C(box.Value, 2, "A4 夹具：按下第 0 行之后 `Value` **仍是 2**（`onDown` 只动选中行，不改值）");
        C(box.CheckShownRow, 2,
          "A4 ★灭自证★ 选中行挪到 0、值仍是 2 ⇒ **勾必须留在第 2 行**"
        + "（🧨 把实现写成 `i == _selRow` 这一条当场红）");

        // A5：值 -1（原版 `Dropdown.value` 的初值）⇒ 一个勾都不亮。
        box.SetValue(-1);
        C(box.Value, -1, "A5 夹具：`SetValue(-1)`（= 原版「还没选过」）");
        C(box.CheckShownRow, -1, "A5 反例③ 值 = -1 ⇒ **一个勾都不亮**（不是「永远亮第一行」）");
    }

    // ============================================================ B 段：`B-225`

    static void SectionB(ref ImageQuad probeQuad)
    {
        MenuCheck.Section(_sink, "B 段 · B-225 `ImageQuadMaterialModifier`（原版 `EverguildButtonMaterialModifier` 的等价口）");

        Tr(WindowButton.GrayShaderAvailable,
           "（前提）原版灰化 shader `" + ImageQuadMaterialModifier.GrayShaderName + "` 取得到"
         + "（取不到时代码会**保持原样并出声**，B2/B3 就没有意义）");

        var tex = CardArt.Solid();
        Tr(tex != null, "（前提）`CardArt.Solid()` 取得到（纯色图，B 段只要一颗能画的 quad）");
        if (tex == null) return;

        probeQuad = ImageQuad.Create(null, tex, Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "X2 Probe Quad");
        Tr(probeQuad != null, "（前提）探针 `ImageQuad` 建出来了");
        if (probeQuad == null) return;

        var mod = ImageQuadMaterialModifier.Attach(probeQuad);
        Tr(mod != null, "（前提）`ImageQuadMaterialModifier.Attach` 回了一件（组件挂在 quad 的 GO 上）");
        if (mod == null) return;

        var before = mod.CurrentMaterial;
        Tr(before != null, "（前提）探针 quad 初始有一份材质（`ImageQuad.Create` 给的是 `Sprites/Default`）");

        // B2：换 —— 材质**实例**必须真的换了，且 shader 名是原版那一张。
        mod.SetGreyScale(true);
        Tr(mod.GreyScale, "B2① `SetGreyScale(true)` 之后 `GreyScale == true`");
        var after = mod.CurrentMaterial;
        Tr(after != null && !ReferenceEquals(before, after),
           "B2② ★材质实例真的换了★（`ReferenceEquals` 前后不同 —— 🧨 只置布尔不换材质的实现这一条当场红）");
        Tr(after != null && after.shader != null
           && after.shader.name == ImageQuadMaterialModifier.GrayShaderName,
           "B2③ 新材质的 shader 名 = `" + ImageQuadMaterialModifier.GrayShaderName + "`（原版那张）");
        Tr(before == null || before.shader == null || before.shader.name != ImageQuadMaterialModifier.GrayShaderName,
           "B2④ ★灭自证★ 换之前那份材质**本来不是**灰化 shader（否则 B2③ 恒真，断不出什么）");

        // B4：幂等 —— 再点一次不重建材质。
        mod.SetGreyScale(true);
        var again = mod.CurrentMaterial;
        Tr(ReferenceEquals(after, again), "B4 连点两次 `SetGreyScale(true)` ⇒ **同一份材质实例**（幂等，不重建）");

        // B3：还原 —— 原样还回换之前那一份**实例**（不是新建一份同 shader 的）。
        mod.SetGreyScale(false);
        Tr(!mod.GreyScale, "B3① `SetGreyScale(false)` 之后 `GreyScale == false`");
        Tr(ReferenceEquals(mod.CurrentMaterial, before),
           "B3② ★原样还回★ 还原后的材质**就是**换之前那一份实例（🧨 写成「新建一份」这一条当场红）");

        // B5：再灰一次仍然正常（证明还原没把内部状态弄坏）。
        mod.SetGreyScale(true);
        Tr(mod.GreyScale && !ReferenceEquals(mod.CurrentMaterial, before),
           "B5 还原之后再灰一次仍能换（不是「只能灰一次」）");
    }
}
