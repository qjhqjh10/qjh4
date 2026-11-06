// B142_E31_Assert.cs — 执行写手 `B142-E31`（2026-11-04 · 第一百四十二会话 · 波次 `B142`）
//
// ============================ 它盯的是什么（三条活）============================
//  ① `B142-SETTINGSPANEL-CLIP` —— `Battle/SettingsPanel.cs` 的语言下拉 `Viewport` 建了**却没有裁切组件**
//     （`Node()` 出来的 `sizeDelta` 还是 0 ⇒ 连「挂上就能裁」都不成立）。判据 = 原版 `Social Submenu Variant.prefab`
//     的 `Viewport`：`!u!224 &224737469884136080`（`m_SizeDelta.x = −17` ⇒ 宽 = Template − 17；
//     `m_SizeDelta.y = 0` + 四边拉伸锚 ⇒ 高 = Template 高）+ `!u!114 &114189131814739177`
//     （`Mask`、`m_ShowMaskGraphic: 0` ⇒ 只写模板、不画自己）。
//     🔴 本文件是 **uGUI 子画布**档（件全走 `HudCanvas.MakeSlicedPiece`）⇒ 等价物是 `RectMask2D`
//     （同 `Battle/UnitChatPanel.PartB118W3.cs:42` 的 uGUI 先例）；`ViewportClip` 只在 `MenuDraw.*`
//     建件那一刻被 `ViewportClip.Resolve` 解析 ⇒ **挂在 uGUI 树上不裁**（写在这里是为了让下一个人别去挂它）。
//  ② `B142-DROPDOWN-CONTENT-H` —— 原版 `Content` 高 = `n × 40.8707 + 0.8519`、首行顶 = `Content` 顶 + 0.426
//     （`TMP_Dropdown.cs:926-932` 的 `sizeDelta.y = itemSize.y·n + offsetMin.y − offsetMax.y`，
//      `:842-843` 的 `offsetMin.y = 0.426` / `offsetMax.y = −0.426`；`Content` 模板 `m_SizeDelta.y = 41.7226`）。
//     落在 `Shell/AllianceMemberTab.cs` 的 `AllianceDropdownBox.BuildList`。
//  ③ `B141-DRAFT-MATERIAL` —— 轮抽「To Battle」按钮的底图材质 = `Material/UI Orange Button To Green`
//     （`Draft Mode Timed Mode Window.prefab` 那颗 `Image` 的 `m_Material` guid `6ef1a6acedd47434d89c8087abfbcff3`；
//      `_Hue_Offset 0.303` · shader = `Shader Graphs/Sprite HUE Color change`）。
//
// ============================ 灭自证（🧨 每条怎么才能红）============================
//  · A2/A2b/A2c 拿**字面量**比 `RectMask2D` 的两个字段（⛔ 不拿被测常量比自己）——删组件 / 改 `padding`
//    或 `softness` ⇒ 红。A3/A4 拿**字面量宽高**比实测尺寸 ⇒ 把尺寸写回 0（= 今天那个 bug）⇒ 红。
//  · A6 结构性判别式：**列表根上不许有掩码**（把掩码从 `Viewport` 挪到 `Dropdown List` 上 ⇒ 底板/箭头一起被裁）。
//  · B1/B2 拿 `14 × 40.8707 + 0.8519` 这个**算式**比 `Content` / `Viewport` 的实高 ⇒ 只改行偏移不改高 ⇒ 红。
//  · B3 拿 `0.426`（= `0.8519/2`）比「`Content` 顶 → 首行顶」的距离 ⇒ 把首行改回 `top + i·ItemH` ⇒ 距离变 0 ⇒ 红。
//    ⚠️ B1 与 B3 **不共因**：B1 只认「高变了没有」、B3 只认「行往下让了没有」。
//  · C3/C4 拿字面量 `0.303` / shader 名比材质 ⇒ 抄成 `UI Orange Button To Purple` 的 `0.679` ⇒ 红
//    （C5 再把 `0.679` 显式排除一次）。C6 拿**另一颗钮**（`Reset Event Button`）当反例 ⇒ 一键全局套材质 ⇒ 红。
//  · D 段扫**源码非注释部分**：删/改任一处落点 ⇒ 红（防「树是碰巧长对的」）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/MainMenuScene.cs`（同波 `B141W27_Assert` / `B141W6_Assert` 的宿主；
//  轮抽窗那几条断言也在那儿）。在该宿主**合计行之前、同一物理行**（该行第一个 `//` **之前**）追加：
//      B142_E31_Assert.Run(); _sink.Pass += B142_E31_Assert.Pass; _sink.Fail += B142_E31_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）
//  ⚠️ 本件会**建一扇 `SettingsPanel` + 一扇 `SocialWindow` + 一扇 `DraftModeWindow`** 再销毁（三处 `finally`），
//     并自备相机（`LayoutSpace.Cam` 用完原样还回去）。它不需要场景里已有这些窗。
//  ⚠️ `SettingsPanel` 建的那块 `PopUps` 子画布是**幂等复用**的（同 `B117_G10_20_Assert` / `B120_W31_Assert`）。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B142_E31.md`
//  · 判据：`资料/完全复刻/判据/B142_R7_下拉与九宫_施工单.md` §一 · `资料/完全复刻/判据/B142_核销清单.md`
//  · 原版：`Social Submenu Variant.prefab`（social 下拉）· `Draft Mode Timed Mode Window.prefab`（轮抽钮）
//          · `Material/UI Orange Button To Green.mat` · uGUI `TMP_Dropdown.cs:842-843 / :926-932 / :964`。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142-E31`：设置面板下拉裁切 · 下拉 `Content` 高与首行偏移 · 轮抽 `To Battle` 材质。</summary>
public static class B142_E31_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B142E31] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Nr(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 原版字面量（⛔ 不从被测类里读回来，否则变成自证）----
    const float ItemH = 40.8707f;      // 原版 `Item.m_SizeDelta.y`
    const float ItemGap = 0.8519f;     // 41.7226（`Content` 模板高）− 40.8707
    const int Rows14 = 14;             // `AllianceDropdownBox.MaxVisibleRows`
    const string HueShader = "Shader Graphs/Sprite HUE Color change";
    const float HueGreen = 0.303f;     // `UI Orange Button To Green.mat` 的 `_Hue_Offset`
    const float HuePurple = 0.679f;    // `UI Orange Button To Purple.mat`（同族反例：⛔ 别抄错这一支）

    /// <summary>设计 px / 世界单位（= 108，与 `MenuDraw.QuadRectPx` / `ViewportClip.ClipPx` 同一份）。</summary>
    const float K = LayoutSpace.DesignPxH / LayoutSpace.DesignHeight;

    static readonly PxRect Field14 = new PxRect(1130.90f, 210.41f, 1380.90f, 269.81f);   // = 真站点 `LanguagesDropdown` 那组几何
    static readonly PxRect Field3 = new PxRect(200f, 200f, 900f, 260f);

    static float PxW(Transform t) { var rt = t as RectTransform; return rt == null ? -1f : rt.rect.width * K; }
    static float PxH(Transform t) { var rt = t as RectTransform; return rt == null ? -1f : rt.rect.height * K; }

    /// <summary>跑全部四段。<paramref name="sw"/> 可传宿主已经开着的那扇社交窗（不传就自己建）。</summary>
    public static void Run(SocialWindow sw = null)
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "B142-E31 · 下拉裁切 / `Content` 高与首行偏移 / 轮抽 `To Battle` 材质");
        try
        {
            A_SettingsPanelClip();
            B_DropdownContentH(sw);
            C_DraftMaterial();
            D_SourceGate();
        }
        catch (Exception e)
        {
            Tr(false, "断言段抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            Debug.Log(_sink.P + "=== B142E31 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败 ===");
        }
    }

    // ============================================================ A 段：`B142-SETTINGSPANEL-CLIP`

    static void A_SettingsPanelClip()
    {
        MenuCheck.Section(_sink, "A 段 · 战斗内设置面板：下拉 `Viewport` 真裁切（原版 `Template ▸ Viewport(Mask, showGraphic 0) ▸ Content`）");

        Camera camWas = LayoutSpace.Cam;
        GameObject camGo = null;
        SettingsPanel sp = null;
        try
        {
            // ---- 前提：uGUI 层要一块相机（`HudCanvas.Ensure` 没有相机就**不建**）----
            if (LayoutSpace.Cam == null)
            {
                var found = UnityEngine.Object.FindObjectOfType<Camera>();
                if (found != null) LayoutSpace.Cam = found;
                else
                {
                    camGo = new GameObject("B142_E31_Cam");
                    var cam = camGo.AddComponent<Camera>();
                    cam.orthographic = true;
                    LayoutSpace.Cam = cam;
                }
            }
            Tr(LayoutSpace.Cam != null, "（前提）`LayoutSpace.Cam` 有着落（否则 `HudCanvas` 建不出来，下面全白测）");
            if (LayoutSpace.Cam == null) return;

            sp = SettingsPanel.Create(null, null);
            Tr(sp != null && sp.PanelRect != null, "（前提）`SettingsPanel.Create` 建出面板帧");
            if (sp == null || sp.PanelRect == null) return;

            sp.Show();
            sp.ShowLangList();
            Tr(sp.LangListOpen, "（前提）语言下拉列表开出来了（`ShowLangList` ⇒ 那份 `Dropdown List` 才会建）");
            var list = sp.LangListNode;
            Tr(list != null, "A0（前提）`Dropdown List` 节点在");
            if (list == null) return;

            var vp = list.Find("Viewport");
            Tr(vp != null, "A1 ★ `Dropdown List/Viewport` 在（原版 `Template ▸ Viewport`）");
            if (vp == null) return;

            var mask = vp.GetComponent<UnityEngine.UI.RectMask2D>();
            Tr(mask != null,
               "A2 ★★ `Viewport` 上挂着 **`RectMask2D`**（= 原版那颗 `Mask`(`m_ShowMaskGraphic: 0`) 在 **uGUI 档**的等价物；"
             + "`ViewportClip` 只在 `MenuDraw.*` 建件时被解析 ⇒ 挂在这棵 uGUI 树上**不裁**）｜🧨 删掉组件 ⇒ 红");
            if (mask != null)
            {
                C(mask.padding, Vector4.zero, "A2b ★ `m_Padding` = **(0,0,0,0)**（原版 11/11 颗下拉 `Viewport` 都是硬边）");
                C(mask.softness, Vector2Int.zero, "A2c ★ `m_Softness` = **(0,0)**（同上）｜🧨 写成非 0 ⇒ 边缘出现渐变 ⇒ 红");
            }

            var vpRt = vp as RectTransform;
            Tr(vpRt != null, "A3（前提）`Viewport` 是 `RectTransform`");
            if (vpRt == null) return;
            Vector2 size = HudCanvas.SizePx(vpRt);
            // 语言框宽 250（`LangFieldX1Px..X2Px`）− `LstInsetX 2.4999×2` − `LstVpInsetR 17` = 228.0002
            Nr(size.x, 228.0002f, 0.01f,
               "A3 ★★ `Viewport` 实测宽 = 245.0002 − 17 = **228.0002**（原版 `m_SizeDelta.x = −17` ⇒ 宽 = Template − 17）"
             + "｜🧨 不先给尺寸（`Node()` 出来是 0）⇒ 掩码把整棵子树裁没、这里也当场红");
            float wantH = Mathf.Min(sp.LangRowCount * ItemH + ItemGap, 573.96f);   // 原版 `Template` 序列化满高 = 573.96（超出就被它卡住）
            Nr(size.y, wantH, 0.02f,
               "A4 ★★ `Viewport` 实测高 = 面板高 = " + sp.LangRowCount + " × 40.8707 + 0.8519 = **" + wantH.ToString("F3")
             + "**（原版 `m_SizeDelta.y = 0` + 四边拉伸锚 ⇒ 高 = Template 高）");

            // 每一行都整块在框内 ⇒ 加裁切**逐像素零变化**
            float vpCy = sp.LangFieldNode != null ? sp.LangFieldNode.InverseTransformPoint(vp.position).y : float.NaN;
            int outside = 0, measurable = 0;
            for (int i = 0; i < sp.LangRowCount; i++)
            {
                float cx, cy, w, h;
                if (!sp.LangRowRectPx(i, out cx, out cy, out w, out h)) continue;
                measurable++;
                if (cy + h * 0.5f > vpCy + size.y * 0.5f + 0.01f) outside++;
                else if (cy - h * 0.5f < vpCy - size.y * 0.5f - 0.01f) outside++;
            }
            C(measurable, sp.LangRowCount, "A5（前提）每一行都量得到矩形（量不到就判不了「有没有被裁」）");
            C(outside, 0,
              "A5b ★ " + sp.LangRowCount + " 行**每一行都整块在 `Viewport` 框内**（今天不溢出 ⇒ 加裁切前后可见内容不变）"
            + "｜🧨 框高算小 ⇒ 末行被切 ⇒ 红");

            Tr(list.GetComponent<UnityEngine.UI.RectMask2D>() == null,
               "A6 🔴 灭自证：**列表根**（`Dropdown List`，原版 `Template`）上**不挂**掩码 —— 挪上去会把底板/滚动条一起裁，"
             + "而原版那颗 `Mask` 在 `Viewport` 那一级");
        }
        catch (Exception e)
        {
            Tr(false, "A 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (sp != null) { try { UnityEngine.Object.DestroyImmediate(sp.gameObject); } catch { } }
            if (camGo != null) { try { UnityEngine.Object.DestroyImmediate(camGo); } catch { } }
            LayoutSpace.Cam = camWas;
        }
    }

    // ============================================================ B 段：`B142-DROPDOWN-CONTENT-H`

    static void B_DropdownContentH(SocialWindow sw)
    {
        MenuCheck.Section(_sink, "B 段 · 社交下拉：`Content` 高 = `n × 40.8707 + 0.8519`、首行顶再让 0.426（两者必须成对）");

        SocialWindow own = null;
        AllianceDropdownBox box = null;
        try
        {
            if (sw == null)
            {
                own = SocialWindow.Create(null);
                Tr(own != null, "（前提）`SocialWindow.Create(null)` 建得出来（同 `B113X2_Assert` 的夹具写法）");
                if (own != null) own.Open();
                sw = own;
            }
            Tr(sw != null, "（前提）社交窗在");
            var tab = (sw != null && sw.PageAlliances != null) ? sw.PageAlliances.Member : null;
            Tr(tab != null && tab.Page != null,
               "（前提）`Alliances` 页的 `AllianceMemberTab` 与它的 `Page` 都在（取图才走得通 `Page.ArtOf`）");
            if (tab == null || tab.Page == null) return;

            var opts = new string[Rows14];
            for (int i = 0; i < opts.Length; i++) opts[i] = "E31-" + i;
            box = AllianceDropdownBox.Create(tab, sw.transform, "E31 Probe Dropdown 14", Field14, opts);
            Tr(box != null, "（前提）14 项测试下拉建出来了（几何 = 真站点 `LanguagesDropdown` 那组）");
            if (box == null) return;
            box.Open();
            Tr(box.IsOpen, "（前提）`Open()` 之后 14 行已建（`IsOpen`）");

            var list = box.ListRoot;
            var vp = box.ViewportRoot;
            var content = box.ContentRoot;
            Tr(list != null && vp != null && content != null, "B0（前提）`Template / Viewport / Content` 三层都在");
            if (list == null || vp == null || content == null) return;

            float wantH = Rows14 * ItemH + ItemGap;      // 573.0437

            // 这一棵是 **`MenuDraw` 世界档** ⇒ 裁切载体就是 `ViewportClip`（与 A 段那棵 uGUI 树的 `RectMask2D` 分工明确）
            var vclip = vp.GetComponent<ViewportClip>();
            Tr(vclip != null, "B0b ★ `Viewport` 上挂着 **`ViewportClip`**（社交这棵走 `MenuDraw.*` 建件 ⇒ 它才是会真裁的那一支）");
            if (vclip != null)
            {
                C(vclip.padding, Vector4.zero, "B0c ★ `padding` = **(0,0,0,0)**（原版 `Viewport` 的 `m_Padding` 全 0）");
                C(vclip.softness, Vector2Int.zero, "B0d ★ `softness` = **(0,0)**（硬边）｜🧨 写成非 0 ⇒ 上下边缘出现渐变 ⇒ 红");
            }
            Nr(PxH(content), wantH, 0.02f,
               "B1 ★★ `Content` 高 = 14 × 40.8707 + 0.8519 = **" + wantH.ToString("F3") + "**（原版 `TMP_Dropdown.Show():926-932`："
             + "`sizeDelta.y = itemSize.y·n + offsetMin.y − offsetMax.y`，本 prefab 的 `offsetMin.y = 0.426` / `offsetMax.y = −0.426`）"
             + "｜🧨 只把首行往下挪、不把高改过来 ⇒ 红");
            Nr(PxH(vp), wantH, 0.02f,
               "B2 ★★ `Viewport` 高 = 内容高（原版 `Viewport.m_SizeDelta.y = 0` + 四边拉伸锚 ⇒ 跟着 `Template` 缩到内容高）");

            var item0 = content.Find("Item 0");
            Tr(item0 != null, "B3（前提）`Content/Item 0` 在");
            if (item0 != null)
            {
                var cRt = (RectTransform)content;
                var iRt = (RectTransform)item0;
                // 首行顶 距 `Content` 顶 多少 px —— 两个量都在 `Content` 自己的局部框里，量纲 = 设计 px
                float rowCenterFromContentCenter = iRt.localPosition.y * K;      // `MenuDraw.Node` 写的是世界单位 ⇒ ×K
                float contentTop = PxH(content) * 0.5f;                          // 中心锚 + 居中 pivot
                float rowTop = rowCenterFromContentCenter + ItemH * 0.5f;
                Nr(contentTop - rowTop, ItemGap * 0.5f, 0.02f,
                   "B3 ★★ 首行顶 = `Content` 顶**往下 0.426**（= `0.8519 / 2`；原版 `TMP_Dropdown.cs:842-843` 的 `offsetMin.y`）"
                 + "｜🧨 把 `float y1 = top + ItemGap * 0.5f + i * ItemH;` 改回 `top + i * ItemH` ⇒ 这一格变 0 ⇒ 红");
                Tr(cRt != null, "B3b（前提）`Content` 的 `RectTransform` 在");
            }

            // 末行整块仍在框内（首行让 0.426 之后，末行底也必须留出对称的 0.426）
            var last = content.Find("Item " + (Rows14 - 1));
            if (last != null)
            {
                float vpBottom = -PxH(vp) * 0.5f;                                        // 相对 `Viewport` 中心、px
                float contentCenter = ((RectTransform)content).localPosition.y * K;      // 相对 `Viewport` 中心、px
                float lastCenter = contentCenter + ((RectTransform)last).localPosition.y * K;
                float lastBottom = lastCenter - ItemH * 0.5f;
                Nr(lastBottom - vpBottom, ItemGap * 0.5f, 0.05f,
                   "B4 ★ 末行底 = `Viewport` 底**往上 0.426**（与首行对称；原版 `Show()` 把面板缩到内容高时两头的留白相等）"
                 + "｜🧨 行高/内容高只改一边 ⇒ 末行被框切掉 ⇒ 红");
            }
        }
        catch (Exception e)
        {
            Tr(false, "B 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (box != null && box.Node != null) { try { MainMenuSubmenuWindow.DestroySafe(box.Node.gameObject); } catch { } }
            if (own != null) { try { SocialWindow.DestroySafe(own.gameObject); } catch { } }
        }
    }

    // ============================================================ C 段：`B141-DRAFT-MATERIAL`

    static void C_DraftMaterial()
    {
        MenuCheck.Section(_sink, "C 段 · 轮抽「To Battle」按钮材质 = `UI Orange Button To Green`（`_Hue_Offset 0.303`）");

        DraftModeWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            Tr(wm != null, "C0（前提）拿得到 `WindowsManager`");
            if (wm == null) return;
            win = DraftModeWindow.Create(wm, 20261104);
            Tr(win != null, "C0（前提）`DraftModeWindow.Create` 建出窗来");
            if (win == null) return;

            var toBattle = win.ToBattleNode;
            Tr(toBattle != null, "C1（前提）`To Battle Button` 节点在（Ongoing 页建出来了）");
            if (toBattle == null) return;
            var bgT = toBattle.Find("Bg");
            Tr(bgT != null, "C1b（前提）`To Battle Button/Bg` 在");
            if (bgT == null) return;
            var q = bgT.GetComponent<ImageQuad>();
            Tr(q != null, "C1c（前提）`Bg` 是 `ImageQuad`");
            if (q == null) return;

            var mr = q.GetComponent<MeshRenderer>();
            var mat = mr != null ? mr.sharedMaterial : null;
            Tr(mat != null, "C2 ★ `To Battle` 的底图有材质（不是 null）");
            if (mat == null) return;

            Tr(mat.shader != null && mat.shader.name == HueShader,
               "C3 ★★ 材质 shader = **`" + HueShader + "`**（原版 `UI Orange Button To Green.mat` 的 `m_Shader` guid "
             + "`0c65e4f5b9c047847acffd215703def4`）｜实得 `" + (mat.shader != null ? mat.shader.name : "<null>") + "`"
             + "｜🧨 不挂材质 / 挂成别的 shader ⇒ 红");
            Tr(mat.HasProperty("_Hue_Offset"), "C3b ★ 这支 shader 有 `_Hue_Offset`（原版属性表 = `_Brightness/_Contrast/_Hue_Offset/_MainTex/_Saturation`）");
            if (mat.HasProperty("_Hue_Offset"))
            {
                Nr(mat.GetFloat("_Hue_Offset"), HueGreen, 1e-4f,
                   "C4 ★★ `_Hue_Offset` = **0.303**（原版 `m_SavedProperties.m_Floats`；底图是**橙色**的 `UI_Button_Mulligan`，"
                 + "靠这一刀转绿）｜实得 " + mat.GetFloat("_Hue_Offset").ToString("F4"));
                Tr(Mathf.Abs(mat.GetFloat("_Hue_Offset") - HuePurple) > 1e-3f,
                   "C5 🔴 灭自证：`_Hue_Offset` **不是 0.679** —— 那是同族 `UI Orange Button To Purple`（`Shell/MenuFxMaterials`"
                 + " 的 `ApplyOrangeButtonToPurple`）的参数，抄错那一支 ⇒ 绿变紫 ⇒ 这一条必红");
            }
            Tr(!mat.HasProperty("_Color"),
               "C6 ★ 这支 shader **没有 `_Color`** ⇒ 染色必须走顶点色（`ImageQuad.SetVertexTint`）；"
             + "若有 `_Color` 说明挂错了 shader");

            // 反例：另一颗钮**不该**被套上这颗材质（一键全局套 ⇒ 红）
            var reset = win.ResetNode;
            var resetBg = reset != null ? reset.Find("Bg") : null;
            var rq = resetBg != null ? resetBg.GetComponent<ImageQuad>() : null;
            var rmr = rq != null ? rq.GetComponent<MeshRenderer>() : null;
            var rmat = rmr != null ? rmr.sharedMaterial : null;
            Tr(rmat == null || rmat.shader == null || rmat.shader.name != HueShader,
               "C7 🔴 灭自证：`Reset Event Button` 的底图**没有**被套上这颗转色材质（原版只给 `To Battle` 挂；"
             + "把 `ApplyOrangeButtonToGreen` 挂成全局/挂在父节点上 ⇒ 红）");
        }
        catch (Exception e)
        {
            Tr(false, "C 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null)
            {
                try { PointerLayer.UnregisterOwnedBy(win.gameObject); } catch { }
                try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { }
            }
            DraftModeWindow.ClearRunForTest();
            WindowsManager.ClearReuseCacheForTest();
        }
    }

    // ============================================================ D 段：源码结构

    static void D_SourceGate()
    {
        MenuCheck.Section(_sink, "D 段 · 源码结构（防「树是碰巧长对的」；★ 灭自证）");

        string sp = ReadSrc("CardPresentation/Battle/SettingsPanel.cs", "D1");
        if (sp != null)
        {
            C(CountOfCode(sp, "AddComponent<UnityEngine.UI.RectMask2D>()"), 1,
              "D1 ★ `SettingsPanel.cs` 里 `AddComponent<UnityEngine.UI.RectMask2D>()` 恰 **1** 处（下拉 `Viewport` 那颗）"
            + "｜🧨 删掉 ⇒ 0 ⇒ 红");
            C(CountOfCode(sp, "vpMask.padding = Vector4.zero"), 1,
              "D1 ★ `vpMask.padding = Vector4.zero` 恰 **1** 处（pad 全 0）");
            C(CountOfCode(sp, "vpMask.softness = Vector2Int.zero"), 1,
              "D1 ★ `vpMask.softness = Vector2Int.zero` 恰 **1** 处（softness 全 0）");
            C(CountOfCode(sp, "HudCanvas.SetPxSize((RectTransform)vp"), 1,
              "D1 ★ `Viewport` 的尺寸那一句恰 **1** 处｜🧨 删掉（回到 `sizeDelta = 0`）⇒ 掩码把子树裁没 ⇒ 红");
            C(CountOfCode(sp, "ViewportClip.Hang"), 0,
              "D2 🔴 灭自证：`SettingsPanel.cs` 里 **没有** `ViewportClip.Hang`（这棵 uGUI 树挂 `ViewportClip` 是**空操作**，"
            + "真裁切只能是 `RectMask2D`）");
        }

        string am = ReadSrc("CardPresentation/Shell/AllianceMemberTab.cs", "D3");
        if (am != null)
        {
            C(CountOfCode(am, "public const float ItemGap = 0.8519f;"), 1,
              "D3 ★ `AllianceDropdownBox.ItemGap` = 0.8519f 恰 **1** 处");
            C(CountOfCode(am, "float y1 = top + ItemGap * 0.5f + i * ItemH;"), 1,
              "D3 ★ 首行让 `ItemGap / 2` 那一句恰 **1** 处｜🧨 改回 `top + i * ItemH` ⇒ 0 ⇒ 红");
            C(CountOfCode(am, "rows * ItemH + ItemGap"), 3,
              "D3 ★ `listR` / `vpR` / `Content` 三处的高**都**带 `+ ItemGap`（恰 3 处）｜🧨 漏一处 ⇒ 2 ⇒ 红");
        }

        string dw = ReadSrc("CardPresentation/Shell/DraftModeWindow.cs", "D4");
        if (dw != null)
        {
            C(CountOfCode(dw, "ApplyOrangeButtonToGreen(battleBg)"), 1,
              "D4 ★ `ApplyOrangeButtonToGreen(battleBg)` 恰 **1** 处（只挂在 `To Battle` 那颗上）｜🧨 删掉 ⇒ 0 ⇒ 红");
            C(CountOfCode(dw, "F(\"_Hue_Offset\", 0.303f)"), 1,
              "D4 ★ `F(\"_Hue_Offset\", 0.303f)` 恰 **1** 处（原版 `m_Floats`）｜🧨 抄成 0.679 ⇒ 0 ⇒ 红");
            C(CountOfCode(dw, "0.679f"), 0,
              "D5 🔴 灭自证：`DraftModeWindow.cs` 里**没有** `0.679f`（那是 `UI Orange Button To Purple`；这里必须是 0.303）");
            C(CountOfCode(dw, "MenuFxMaterials.ShaderSpriteHueColorChange"), 2,
              "D5 ★ shader 名走的是 `MenuFxMaterials.ShaderSpriteHueColorChange` 那份唯一常量（现读**恰 2 处**：`:4927` 解析 + `:4929` 告警插值串，**两处引的是同一份常量**，⛔ 没有第二份字面量 —— 见下一条）");
            C(CountOfCode(dw, "Shader Graphs/Sprite HUE Color change"), 0,
              "D5 ★ 灭自证：`DraftModeWindow.cs` 的**代码**里 0 处那串字面量（抄一份硬编码 ⇒ 1 ⇒ 红；⚠️ 唯一那处在 `:4913` 的 `///` 文档注释里，`CountOfCode` 跳过注释 ⇒ 不算）");
        }
    }

    // ============================================================ 文件读口（口径同 `B141_W27_Assert`）

    static string ReadSrc(string rel, string tag)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p)) { Tr(false, tag + "0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红"); return null; }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            Tr(false, tag + "0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>数**非注释**片段里 needle 的出现次数（口径同 `B141_W25_Assert.CountOfCode`）。</summary>
    static int CountOfCode(string src, string needle)
    {
        int n = 0, i = 0;
        while (i < src.Length)
        {
            int eol = src.IndexOf('\n', i);
            if (eol < 0) eol = src.Length;
            string line = src.Substring(i, eol - i);
            i = eol + 1;
            int k = line.IndexOf("//", StringComparison.Ordinal);
            string code = k >= 0 ? line.Substring(0, k) : line;
            int from = 0, j;
            while ((j = code.IndexOf(needle, from, StringComparison.Ordinal)) >= 0) { n++; from = j + needle.Length; }
        }
        return n;
    }
}
