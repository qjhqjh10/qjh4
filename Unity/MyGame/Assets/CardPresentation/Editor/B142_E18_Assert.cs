// B142_E18_Assert.cs — 执行写手 `E18`（2026-11-04 · 第一百四十二会话 · `B142-E18`）
//
// ============================ 它盯的是什么 ============================
//  `B141-DROPDOWN-VIEWPORT`：`Shell/AllianceMemberTab.cs` 的 `AllianceDropdownBox.BuildList`
//  补上原版那两层 —— `Template ▸ Viewport ▸ Content ▸ Item n`
//  （判据 = `Social Submenu Variant.prefab`：`!u!224 &224441746388359948`(Template) /
//    `!u!224 &224737469884136080`(Viewport) / `!u!224 &224868640879826322`(Content)）。
//
// ============================ 逐支为什么必须这么断 ============================
//  A 段（结构）：
//   · A1 层级逐字对原版：`Template → Viewport → Content`（少一层 = 原版缺件，`Dropdown.Show()` 的
//     `content` 就没有挂点）；
//   · A2 `Viewport` 上挂的是 `ViewportClip` 且 `padding/softness` 全 0 —— 原版那颗是 `Mask`
//     （stencil，`m_ShowMaskGraphic: 0`；**⛔ 不是 `RectMask2D`**），我们只有矩形裁切 ⇒ 这是**效果等价物**；
//     同时断「`Template` 上**不挂**」—— 底板要整块画，挂了会把底板裁成条目宽；
//   · A3 `Viewport` 宽 = `Template` 宽 − 17（= 原版 `m_SizeDelta.x = −17`）、高 = 列表高 `rows × ItemH + ItemGap`（`m_SizeDelta.y = 0`；`ItemGap` = `AllianceDropdownBox.ItemGap`，`B142-E31` 起）；
//   · A4 条目的**父链**逐级对原版，并**灭自证**：`Template` 下**没有** `Item 0`
//     （否则就是「两边各建一份」，路径断得出来但实际没挪家）；
//   · A5 `Blocker` **仍在 `Template`**、且仍是整屏 —— 挪进 `Viewport` 会被裁成 `itemW × listH`
//     ⇒ 「点别处收起」坏掉（原版 `Dropdown.CreateBlocker()` 也是造在**根 Canvas** 上）。
//  B 段（不裁 / 不溢出，用**真站点那组几何** 1130.90/210.41/1380.90/269.81 + 14 项）：
//   · B0 从 `Content` 往上解析到的裁切框**逐字段 = 我们算的 `vpR`**（`MenuDraw.SameRect`，**无容差**）；
//   · B1 每一条 `Item` 的矩形都**整块在框内**且 `ClipRect` 走「原样返回」那条（`SameRect(out, r)`）——
//     `ClipRect` 的「整块在框内」是**无容差**逐字段比（`MenuDraw.cs:277`）⇒ 这一条同时钉住
//     「`vpR` 四条边必须与 `Item` 用同一算式」那条纪律（差 1 ULP 就会被判部分越界、去截几何）；
//   · B2/B3 「补结构前后可见内容不变」的**静态表达**：14 行的 `Item Background` quad 尺寸**仍是整块**
//     （`MenuDraw.Rect` 若判了部分越界，建出来的 quad 会变窄/变矮 ⇒ 这里一条都查不出来）；
//   · B4 `Viewport` 实测高 = 14 × `ItemH` + `ItemGap` ⇒ 「14 行装满、首末各让 `ItemGap/2`、不溢出」。
//   ⚠️ **不断「与原版逐像素无差」**：原版掩码 = 内置 `Background` 图（**圆角**矩形），我们只能做矩形
//     ⇒ 首/末行底图四角差几像素**要渲图核对**，断言表达不了（报告里如实写了）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `CardPresentation/Editor/ShellScene.cs` 的 `Run()` 尾巴那一行（`B113X2_Assert.Run()` 就在那儿，
//  见 `ShellScene.cs:11301`）**同一物理行内追加**：
//      B142_E18_Assert.Run(); _sink.Pass += B142_E18_Assert.Pass; _sink.Fail += B142_E18_Assert.Fail;
//  ⚠️ 两段都**自建夹具下拉**（挂在社交窗根上，同 `B113X2_Assert` 的写法）并自拆 ⇒ 任何宿主都能跑；
//     不碰真站点（`Config fields` 那两格是编辑态才亮的）。
//  ⚠️ 夹具挂在**窗根**（1920×1080 设计帧）—— `MenuDraw.Hit` 见「整块在视口外」会连节点一起不建，
//     挂到别的裁切区里会让 `Item i/Hit` 消失（同 `B113X2_Assert:103-105` 那条坑）。
//
// ============================ 报告 / 出处 ============================
//  · 报告：`资料/完全复刻/报告/B142_E18.md`
//  · 判据：`资料/完全复刻/判据/B142_R7_下拉与九宫_施工单.md` §一
using System;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `E18`：下拉伸缩列表的 `Viewport ▸ Content` 两层（原版 `Template ▸ Viewport ▸ Content ▸ Item n`）。</summary>
public static class B142_E18_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E18] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 几何常量：**全部从 `AllianceDropdownBox` 现读**（⛔ 别抄第二份）----
    const float ItemH = AllianceDropdownBox.ItemH;
    const float TopGap = AllianceDropdownBox.ListTopGap;
    const float WidthLoss = AllianceDropdownBox.ListWidthLoss;
    const float ScrollW = AllianceDropdownBox.ScrollbarW;
    const int MaxRows = AllianceDropdownBox.MaxVisibleRows;

    /// <summary>设计 px / 世界单位（= 108，与 `MenuDraw.QuadRectPx` / `ViewportClip.ClipPx` 同一份）。</summary>
    const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

    /// <summary>3 项夹具（`A` 段：结构，取一个不压在真控件上的坐标）。</summary>
    static readonly PxRect Field3 = new PxRect(200f, 200f, 900f, 260f);

    /// <summary>14 项夹具 = **真站点那组几何**（`AllianceMemberTab.cs` 的 `LanguagesDropdown`：`:1454` 的
    /// `new PxRect(1130.90, 210.41, 1380.90, 269.81)` + `SocialData.LanguageOptionLabels()` 的 14 项）。
    /// ⇒ `Viewport` 宽必须实测 = **228**（= 250 − 5 − 17，原版 `m_SizeDelta.x = −17`）。</summary>
    static readonly PxRect Field14 = new PxRect(1130.90f, 210.41f, 1380.90f, 269.81f);

    /// <summary>照 `BuildList` 的算式算**第 i 行**的矩形。
    /// ⚠️ 必须与生产那两行**同一算式**（`listR.x1 + itemW` / `(top + ItemGap*0.5 + i*ItemH) + ItemH`）——
    /// `MenuDraw.ClipRect` 判「整块在框内」是**无容差**逐字段比，算式不同就等于换了被测对象。</summary>
    static PxRect RowRect(PxRect fieldR, int i)
    {
        float w = fieldR.W - WidthLoss;
        float top = fieldR.CY + TopGap;
        float itemW = w - ScrollW;
        float y1 = top + AllianceDropdownBox.ItemGap * 0.5f + i * ItemH;   // ⚠️ 直接引用生产常量 `ItemGap`（⛔ 别在这里写第二份 0.8519）
        return new PxRect(fieldR.x1, y1, fieldR.x1 + itemW, y1 + ItemH);
    }

    /// <summary>照 `BuildList` 的算式算 `Viewport` 的矩形（四条边与 `RowRect` 同源；`y2 = top + rows*ItemH + ItemGap`，`ItemGap` 直接取生产常量）。</summary>
    static PxRect ViewportRect(PxRect fieldR, int rows)
    {
        float w = fieldR.W - WidthLoss;
        float top = fieldR.CY + TopGap;
        float itemW = w - ScrollW;
        return new PxRect(fieldR.x1, top, fieldR.x1 + itemW, top + rows * ItemH + AllianceDropdownBox.ItemGap);
    }

    static float PxW(Transform t) { var rt = t as RectTransform; return rt == null ? -1f : rt.rect.width * K; }
    static float PxH(Transform t) { var rt = t as RectTransform; return rt == null ? -1f : rt.rect.height * K; }

    /// <summary>跑全部两段。<paramref name="sw"/> 可传宿主**已经开着**的那扇社交窗；传 null 就自己建一扇。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B142-E18 · 下拉 `Viewport ▸ Content`（原版 `Template ▸ Viewport ▸ Content ▸ Item n`）");

        SocialWindow own = null;
        AllianceDropdownBox b3 = null, b14 = null;
        try
        {
            if (sw == null)
            {
                own = SocialWindow.Create(null);
                Tr(own != null, "（前提）`SocialWindow` 建得出来（`Create(null)`，同 `B113X2_Assert` 的夹具写法）");
                if (own != null) own.Open();
                sw = own;
            }
            Tr(sw != null, "（前提）社交窗在");
            var tab = (sw != null && sw.PageAlliances != null) ? sw.PageAlliances.Member : null;
            Tr(tab != null && tab.Page != null,
               "（前提）`Alliances` 页的 `AllianceMemberTab` 与它的 `Page` 都在（取图才走得通 `Page.ArtOf`）");
            if (tab != null && tab.Page != null)
            {
                SectionA(ref b3, tab, sw);
                SectionB(ref b14, tab, sw);
            }
        }
        catch (Exception e)
        {
            Tr(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            if (b3 != null && b3.Node != null) MainMenuSubmenuWindow.DestroySafe(b3.Node.gameObject);
            if (b14 != null && b14.Node != null) MainMenuSubmenuWindow.DestroySafe(b14.Node.gameObject);
            if (own != null) SocialWindow.DestroySafe(own.gameObject);
            Debug.Log(_sink.P + "=== B142E18 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A 段：结构

    static void SectionA(ref AllianceDropdownBox box, AllianceMemberTab tab, SocialWindow sw)
    {
        MenuCheck.Section(_sink, "A 段 · 结构（`Template ▸ Viewport ▸ Content ▸ Item n`）");

        box = AllianceDropdownBox.Create(tab, sw.transform, "E18 Probe Dropdown", Field3,
                                         new[] { "E18-A", "E18-B", "E18-C" });
        Tr(box != null, "（前提）测试下拉建出来了（`AllianceDropdownBox.Create`）");
        if (box == null) return;
        box.Open();
        Tr(box.IsOpen, "（前提）`Open()` 之后列表已建（`IsOpen`）");

        var list = box.ListRoot;
        var vp = box.ViewportRoot;
        var content = box.ContentRoot;
        Tr(list != null, "A0① 列表根（原版 `Template`）建出来了（`ListRoot`）");
        Tr(vp != null, "A0② `Viewport` 节点在（`ViewportRoot`；原版 `!u!224 &224737469884136080`）");
        Tr(content != null, "A0③ `Content` 节点在（`ContentRoot`；原版 `!u!224 &224868640879826322`）");
        if (list == null || vp == null || content == null) return;

        // A1：层级 + 名字（按路径取得到 = 自检/别人照原版路径找件不会落空）
        Tr(vp.name == "Viewport" && list.Find("Viewport") == vp,
           "A1① `Viewport` 是 `Template` 的**直接子件**、名字逐字 `Viewport`（`Template/Viewport` 取得到）");
        Tr(content.name == "Content" && vp.Find("Content") == content,
           "A1② `Content` 是 `Viewport` 的**直接子件**、名字逐字 `Content`（`Template/Viewport/Content` 取得到）");

        // A2：裁切载体 + 两个「不该有」
        var vclip = vp.GetComponent<ViewportClip>();
        Tr(vclip != null, "A2① `Viewport` 上挂着 `ViewportClip`（= 原版那颗 `Mask`(`m_ShowMaskGraphic:0`) 的裁切等价物）");
        if (vclip != null)
        {
            Tr(vclip.padding == Vector4.zero, "A2② `padding == (0,0,0,0)`（原版 11 颗下拉 `Viewport` 都没有非零 `m_Padding`）");
            Tr(vclip.softness == Vector2Int.zero, "A2③ `softness == (0,0)`（同上）");
            Tr(vclip.HasBaseRect, "A2④ 记过基准矩形（走的是 `ViewportClip.Hang` 那条唯一入口，不是裸 `AddComponent`）");
        }
        Tr(list.GetComponent<ViewportClip>() == null,
           "A2⑤ 🔴 `Template` 上**不挂** `ViewportClip`（底板要整块画；挂了会被裁成条目宽 ⇒ 列表底少一圈）");

        // A3：宽高（原版 `Viewport` 的 `m_SizeDelta = (−17, 0)` + 四边拉伸锚点）
        var vpR = ViewportRect(Field3, 3);
        Nr(PxW(vp), PxW(list) - ScrollW, 0.01f, "A3① `Viewport` 实测宽 = `Template` 实测宽 − 17（原版 `m_SizeDelta.x = −17`）");
        Nr(PxW(vp), vpR.W, 0.01f, "A3② 同上，按 `BuildList` 的算式核（= `fieldR.W − 5 − 17`）");
        Nr(PxH(vp), vpR.H, 0.01f, "A3③ `Viewport` 实测高 = 列表高 `rows × ItemH + ItemGap`（原版 `m_SizeDelta.y = 0`，靠锚点撑满；`B142-E31` 起含 `ItemGap`）");

        // A4：条目父链
        var item0 = content.Find("Item 0");
        Tr(item0 != null, "A4① `Content` 下有 `Item 0`");
        Tr(item0 != null && item0.parent == content, "A4② `Item 0` 的父件是 `Content`（⛔ 不是 `Template`）");
        Tr(content.parent == vp, "A4③ `Content` 的父件是 `Viewport`");
        Tr(vp.parent == list, "A4④ `Viewport` 的父件是 `Template`");
        Tr(list.Find("Item 0") == null,
           "A4⑤ 🔴 灭自证：`Template` 下**没有** `Item 0`（条目真挪了家，不是两边各建一份）");

        // A5：Blocker 必须留在 Template
        var blocker = list.Find("Blocker");
        Tr(blocker != null, "A5① 全屏挡板 `Blocker` 在");
        Tr(blocker != null && blocker.parent == list,
           "A5② 🔴 `Blocker` 的父件仍是 `Template`（挪进 `Viewport` 会被裁成 itemW×listH ⇒ 点别处收不起来）");
        Tr(vp.Find("Blocker") == null, "A5③ `Viewport` 下没有 `Blocker`");
        Nr(PxW(blocker), 1920f, 0.01f, "A5④ `Blocker` 宽仍是整屏 1920（没被裁）");
        Nr(PxH(blocker), 1080f, 0.01f, "A5⑤ `Blocker` 高仍是整屏 1080（没被裁）");
    }

    // ============================================================ B 段：不裁 / 不溢出

    static void SectionB(ref AllianceDropdownBox box, AllianceMemberTab tab, SocialWindow sw)
    {
        MenuCheck.Section(_sink, "B 段 · 14 行不溢出 / 一行都不被裁（真站点那组几何）");

        var opts = new string[MaxRows];
        for (int i = 0; i < opts.Length; i++) opts[i] = "E18-" + i;
        box = AllianceDropdownBox.Create(tab, sw.transform, "E18 Probe Dropdown 14", Field14, opts);
        Tr(box != null, "（前提）14 项测试下拉建出来了（几何 = 真站点 `LanguagesDropdown` 那组）");
        if (box == null) return;
        box.Open();
        Tr(box.IsOpen, "（前提）`Open()` 之后 14 行已建（`IsOpen`）");

        var list = box.ListRoot;
        var vp = box.ViewportRoot;
        var content = box.ContentRoot;
        Tr(list != null && vp != null && content != null, "B0① （前提）`Template / Viewport / Content` 三层都在");
        if (list == null || vp == null || content == null) return;

        // B0：解析到的裁切框
        var st = ViewportClip.Resolve(content, null, default(Vector2), default(Vector4));
        var clip = st.RenderClip;
        Tr(clip.HasValue, "B0② 从 `Content` 往上解析到了裁切框（父链上的 `Viewport` 那颗 `ViewportClip` 真带电）");
        if (!clip.HasValue) return;
        var vpR = ViewportRect(Field14, opts.Length);
        // ⚠️ 这一条用**带容差**的写法：`ClipPx` 的尺寸那一半要过一趟 `sizeDelta = px/108` → `rect.width × 108`
        //    的 float32 往返（`ViewportClip.ClipPx` 文件头自陈实测残差量级 −6.1e-5 px，同 `Editor/ShellScene.cs:2622`
        //    那条 A497 注释）⇒ 逐位 `SameRect` 有假阴风险。**真正的牙口在 B1**（拿解析出来的 `clip` 逐行判
        //    `ClipRect` 走哪条路 —— 那正是生产建件那一刻走的同一条），这里只挡「公式写错/差 0.4px 以上」。
        Nr(clip.Value.x1, vpR.x1, 0.01f, "B0③ 解析到的裁切框左边 = `BuildList` 算的 `vpR.x1`");
        Nr(clip.Value.y1, vpR.y1, 0.01f, "B0③ 解析到的裁切框上边 = `vpR.y1`");
        Nr(clip.Value.x2, vpR.x2, 0.01f, "B0③ 解析到的裁切框右边 = `vpR.x2`");
        Nr(clip.Value.y2, vpR.y2, 0.01f, "B0③ 解析到的裁切框下边 = `vpR.y2`（差 ≥0.05px 就说明公式改了）");
        Nr(PxW(vp), 228f, 0.01f, "B0④ `Viewport` 实测宽 = **228**（= 250 − 5 − 17；原版 `m_SizeDelta.x = −17`）");

        // B1–B3：逐行不裁 + quad 仍是整块
        int clipBad = 0, quadMissing = 0, quadBad = 0;
        for (int i = 0; i < opts.Length; i++)
        {
            var r = RowRect(Field14, i);
            PxRect outR;
            bool vis = MenuDraw.ClipRect(r, clip, out outR);
            if (!vis || !MenuDraw.SameRect(outR, r)) clipBad++;

            var itemT = content.Find("Item " + i);
            var bgT = itemT != null ? itemT.Find("Item Background") : null;
            var q = bgT != null ? bgT.GetComponent<ImageQuad>() : null;
            if (q == null) { quadMissing++; continue; }
            if (Mathf.Abs(q.WorldW * K - r.W) > 0.05f || Mathf.Abs(q.WorldH * K - r.H) > 0.05f) quadBad++;
        }
        C(clipBad, 0,
          "B1 14 行**每一行**都整块在 Viewport 框内，且 `ClipRect` 走的是「整块在框内 ⇒ 原样返回」那条（`SameRect(out, r)`）");
        C(quadMissing, 0, "B2 14 行的 `Item Background`（`ImageQuad`）都在（前提：取不到就量不了 B3）");
        C(quadBad, 0,
          "B3 ★「补结构前后可见内容不变」★ 14 行底图的 quad 尺寸**仍是整块**（被截过就会变窄/变矮，这里一条都查不出来）");

        // B4：14 行装满内容高（首末各让 `ItemGap/2`、不溢出、也不留超过半格的缝）
        var last = RowRect(Field14, opts.Length - 1);
        Nr(PxH(vp), opts.Length * ItemH + AllianceDropdownBox.ItemGap, 0.01f, "B4① `Viewport` 实测高 = 14 × 40.8707 + `ItemGap`（`AllianceDropdownBox.ItemGap`；`MaxVisibleRows` 装满内容高，不溢出）");
        Nr(PxH(vp) - (last.y2 - vpR.y1), AllianceDropdownBox.ItemGap * 0.5f, 0.01f, "B4② 末行底距 `Viewport` 底 = `ItemGap/2`（首行让半格、末行让半格必须成对；只改高或只改行偏移任一半 ⇒ 本条红）");
    }
}
