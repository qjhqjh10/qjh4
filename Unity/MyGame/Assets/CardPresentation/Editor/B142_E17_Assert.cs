// B142_E17_Assert.cs — 执行写手 `B142-E17`（第一百二十九会话 · 2026-11-04）· `B141-BINDNINE-BORDER`
//
// ============================ 它盯的是什么 ============================
//  🟠 **机制缺口**：`Shell/WindowButton.cs` 的 `BindNine` 原来只记 `ImageQuad[] _nine`，
//  `SwapTo → SetOn` 只换纹理、`ImageQuad._uv` 一个字节不动 ⇒ 换到 **border 不同**的那一档时，
//  uv 分界与角块宽仍沿用**常态图**的切分（静默偏一点点，没有任何报错）。
//  实测三族 border 各不相同（`sharedassets0/Sprite/*.json` 2026-11-04 现读）：
//    · `40K_button_square` 103×107 · (36,28,37,29) ↔ `_hover` (32,27,34,28) ↔ `_pressed` (28,27,27,25)   [ppuMul 2.5]
//    · `40K_tab_button` 489×79 · (183,0,128,29) ↔ `…_overwindow` 489×97 · (188,0,99,30)                  [ppuMul 1.5]
//  同族反例（**同 border**，必须早退、一格都不许动）：`40K_button` 三档全 (234,46,234,46) ·
//  `40K_dropdown_field_closed/opened` 全 (60,35,60,35) · `UI_Button_Mulligan` 三档 (333,96,333,96)。
//
//  ✅ **原版怎么做的**（第一权威 = 本机 uGUI 包源码 `Image.cs`）：
//    · `:279-295` `sprite` setter：`m_Sprite != value` ⇒ `SetAllDirty()`（换 sprite = 重建整张网格）；
//    · `:1157` `adjustedBorders = GetAdjustedBorders(border / multipliedPixelsPerUnit, rect)`
//      —— `border` 是**新 sprite 的 `Sprite.border`**（逐次现读）⇒ 每次换图都按**新图**重切；
//    · `:756` `multipliedPixelsPerUnit => pixelsPerUnit * m_PixelsPerUnitMultiplier` ⇒ 画出来的边 = `border ÷ ppuMul`；
//    · `:1479-1500` `GetAdjustedBorders`：某轴「两条角块之和 > 矩形边长」时**逐轴**按比例缩。
//
// ============================ 断言 ============================
//  A 组（源码 / 接线，零前提）：
//    A1  `WindowButton` 有 `NineMeta`（BorderPx / PpuMul / Valid 三格）
//    A2  `BindNine` 追加的是**可选**形参（四条 `NineMeta? xxx = null`）⇒ 14 个既有调用点零改动
//    A3  `SwapTo` 里 `MaybeResliceNine(tex)` 排在**逐块 SetOn 之前**
//    A4  `ImageQuad` 的切分算式**只有一份**：`NineSliceLayout(` 全文件恰 3 处（1 定义 + 2 调用：建件 / 重切）
//    A5  `ResliceNineSlice` 存在且**不销毁**（尾段无 `Destroy`）、用不到旧块走 `SetActive(false)`
//    A6  `MaybeResliceNine` 有「同 border 早退」那一条（`SameBorder(_nineAppliedBorder, meta.BorderPx)`）
//    A7  `OfferContainer` 三档 meta 都传了、且 border = 原版 JSON 的三组值
//    A8  `DeckSelectionPopup` 两颗页签四档 meta 都登记了（Selected = `…_overwindow` 的 `TabBorderOff`）
//  B 组（运行时探针 —— 真实图 / 真实 border / 真实 ppuMul / 120×120 px 的框）：
//    B2  建件那一刻 = 常态 border (36,28,37,29)（角块宽 + uv 分界）
//    B3  ★ `Enter()` ⇒ 按 **hover** 的 (32,27,34,28) 重切（角块宽 / 中段宽 / uv 三样一起变）
//    B3d/B3e ★ **9 块是同一批对象**（原地改几何，⛔ 不是销毁重建 —— 保住 target/染色表/灰材质/队列）
//    B4  ★ `Press()` ⇒ 按 **pressed** 的 (28,27,27,25) 重切
//    B5/B6 `Release()` ⇒ 回 hover；`Exit()` ⇒ 回常态
//    B7  ★ **同 border 早退**（`40K_dropdown_field_closed/opened` 全 60/35/60/35）：把某块 localPosition
//        人为挪 0.01 后 `Enter()`，**挪动仍在**（= 一次都没重切）+ 纹理照换（换图那半没被连坐）
//    B8  ★ **灭自证**：同一个「人为挪动」在不**同** border 的那棵树上 ⇒ 被**纠回**到算式中心
//        （⇒ B7 的「挪动仍在」不是恒真，它真的在区分「切没切」）
//    B9  ★ **默认 null = 今天的行为**：不传 meta 的 `BindNine` ⇒ 挪动仍在（不重切）、纹理照换
//    B10 ★ **`DeckSelectionPopup` 页签那一族**（`40K_tab_button` 183/0/128/29 ↔ `…_overwindow` 188/0/99/30 · ppuMul 1.5）：
//        `SetSelected(true/false)` ⇒ 按 Selected 的 `TabBorderOff` 重切、取消后回常态
//        （⚠️ 原版这两颗 `m_Transition = 0` ⇒ 指针走不到 Selected；本探针只证机制通，不是「画面变好了」）
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `MaybeResliceNine` 整个空转（不调 `ResliceNineSlice`）⇒ **B3 / B4 / B8** 红。
//  · 早退条件写成 `if (meta.Valid) return;`（同/不同 border 都不切）⇒ **B3 / B4 / B8** 红。
//  · 把早退删掉（同 border 也重切）⇒ **B7 / B9** 红（挪动被纠回），且 `B141_W81_Assert` 的 B4–B7b 一起红。
//  · 改成「销毁重建」⇒ **B3d / B3e** 红（块对象换了），`RefreshGray` 的按下标缓存也会错位。
//  · 把算式在 `ResliceNineSlice` 里**再抄一遍**（不共用）⇒ **A4** 红（`NineSliceLayout(` 计数变 2）。
//  · 只改 uv 不改几何（或反过来）⇒ **B3 / B3b** 之一红（两条分别量角块宽与中段宽）。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 份源码文本 + 5 张图 + 三个用完即销毁的探针树）。
//  挂法（由调度台决定）：同一物理行追加
//      B142_E17_Assert.Run(); pass += B142_E17_Assert.Pass; fail += B142_E17_Assert.Fail;
//  判绿红：按行首 `B142E17 ✓` / `B142E17 ✗` 数（⛔ 别 `grep -c ✗`）。
//  宿主建议：`MainMenuScene.Run`（`OfferContainer` / 下拉框都在外壳一侧；⚠️ 本文件不跑 Unity）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B142-E17`：九宫格换 sprite 按**新图 border** 重切（`B141-BINDNINE-BORDER`）的回归断言。</summary>
public static class B142_E17_Assert
{
    const string Tag = "B142E17";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    // ---- 判据里的数（`sharedassets0/Sprite/*.json` 2026-11-04 现读；⚠️ 改了要同步报告）----
    static readonly Vector4 BN = new Vector4(36f, 28f, 37f, 29f);   // 40K_button_square
    static readonly Vector4 BH = new Vector4(32f, 27f, 34f, 28f);   // 40K_button_square_hover
    static readonly Vector4 BP = new Vector4(28f, 27f, 27f, 25f);   // 40K_button_square_pressed
    static readonly Vector4 BF = new Vector4(60f, 35f, 60f, 35f);   // 40K_dropdown_field_closed / _opened（同 border 家族）
    /// <summary>`Button Image` 的 `m_PixelsPerUnitMultiplier`（`OfferContainer.WebShopPpuMul`，实读 2.5）。</summary>
    const float PpuMul = 2.5f;
    /// <summary>探针框（设计 px）—— 比三档的角块和都大 ⇒ 9 块全在、`GetAdjustedBorders` 的逐轴夹不介入。</summary>
    const float BoxPx = 120f;
    /// <summary>世界单位 = 108 设计 px（`ImageQuad.PixelsPerUnit`）。</summary>
    static float PPU { get { return ImageQuad.PixelsPerUnit; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static string Src(string rel)
    {
        try { return File.ReadAllText(Application.dataPath + "/CardPresentation/" + rel); }
        catch (System.Exception e) { Debug.LogError(Tag + " 读不到 `" + rel + "`：" + e.Message); return ""; }
    }

    static int Count(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, at = 0;
        while ((at = s.IndexOf(sub, at, System.StringComparison.Ordinal)) >= 0) { n++; at += sub.Length; }
        return n;
    }

    static ImageQuad Cell(GameObject root, string name)
    {
        if (root == null) return null;
        var t = root.transform.Find(name);
        return t != null ? t.GetComponent<ImageQuad>() : null;
    }

    static bool Near(float a, float b) { return Mathf.Abs(a - b) < 2e-4f; }
    static bool NearTight(float a, float b) { return Mathf.Abs(a - b) < 1e-4f; }

    static WindowButton.NineMeta Meta(Vector4 borderPx, float ppuMul)
    {
        return new WindowButton.NineMeta { BorderPx = borderPx, PpuMul = ppuMul, Valid = true };
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_Runtime();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（三棵探针树：`40K_button_square` 三档 / 同 border 档 / 默认 null 档）");
    }

    // ==================================================================
    //  A · 源码 / 接线
    // ==================================================================
    static void GroupA_Wiring()
    {
        var wb = Src("Shell/WindowButton.cs");
        var iq = Src("Battle/ImageQuad.cs");
        var oc = Src("Shell/OfferContainer.cs");

        Check(wb.Contains("public struct NineMeta")
              && wb.Contains("public Vector4 BorderPx;") && wb.Contains("public float PpuMul;") && wb.Contains("public bool Valid;"),
              "A1 `WindowButton` 有 `NineMeta`（= 原版 `Sprite.border` + `Image.m_PixelsPerUnitMultiplier` + 「有没有判据」三格）");

        Check(wb.Contains("NineMeta? normal = null") && wb.Contains("NineMeta? hover = null")
              && wb.Contains("NineMeta? pressed = null") && wb.Contains("NineMeta? selected = null"),
              "A2 `BindNine` 追加的是**可选**形参（四条 `= null`）⇒ 14 个既有调用点零改动、默认行为逐位不变");

        int iCall = wb.IndexOf("if (_nine != null) MaybeResliceNine(tex);", System.StringComparison.Ordinal);
        int iLoop = wb.IndexOf("for (int i = 0; i < _nine.Length; i++) SetOn(_nine[i], tex);", System.StringComparison.Ordinal);
        Check(iCall >= 0 && iLoop >= 0 && iCall < iLoop,
              "A3 `SwapTo` 里 `MaybeResliceNine(tex)` 排在**逐块换图之前**（先按新 border 重切、再换纹理）");

        Check(Count(iq, "NineSliceLayout(") == 3 && iq.Contains("static void NineSliceLayout(string name"),
              "A4 切分算式**只有一份**：`NineSliceLayout(` 全文件恰 3 处（1 定义 + 建件 1 调用 + 重切 1 调用）｜实得 "
              + Count(iq, "NineSliceLayout("));

        int iRes = iq.IndexOf("public static bool ResliceNineSlice(", System.StringComparison.Ordinal);
        string resTail = iRes >= 0 ? iq.Substring(iRes) : "";
        Check(iRes >= 0 && !resTail.Contains("Destroy") && resTail.Contains("SetActive(false)"),
              "A5 `ResliceNineSlice` 存在、**⛔ 不销毁**（尾段 0 处 `Destroy`）、用不到的旧块走 `SetActive(false)`");

        Check(wb.Contains("SameBorder(_nineAppliedBorder, meta.BorderPx)")
              && wb.Contains("_nineAppliedTexW == tex.width && _nineAppliedTexH == tex.height"),
              "A6 `MaybeResliceNine` 有「同 border + 同贴图尺寸 ⇒ 早退」那一条（同 border 家族靠它一格不动）");

        Check(oc.Contains("new WindowButton.NineMeta { BorderPx = new Vector4(36f, 28f, 37f, 29f)")
              && oc.Contains("new WindowButton.NineMeta { BorderPx = new Vector4(32f, 27f, 34f, 28f)")
              && oc.Contains("new WindowButton.NineMeta { BorderPx = new Vector4(28f, 27f, 27f, 25f)"),
              "A7 `OfferContainer` 的 WebShop 钮三档 meta 都传了、border = 原版 JSON 三组值");

        var ds = Src("Shell/DeckSelectionPopup.cs");
        Check(ds.Contains("BindNine(tabBgNode, norm, norm, norm, TabOffArt, onMeta, onMeta, onMeta, selMeta)")
              && ds.Contains("BorderPx = on ? TabBorderOn : TabBorderOff")
              && ds.Contains("var selMeta = new WindowButton.NineMeta { BorderPx = TabBorderOff"),
              "A8 `DeckSelectionPopup` 两颗页签四档 meta 都登记了（常态/悬停/按下 = 当前那张，Selected = `…_overwindow` 的 `TabBorderOff`）");
    }

    // ==================================================================
    //  B · 运行时探针（真实图 / 真实 border / 真实 ppuMul）
    // ==================================================================
    static void GroupB_Runtime()
    {
        var texN = CardArt.MenuUi("40K_button_square");
        var texH = CardArt.MenuUi("40K_button_square_hover");
        var texP = CardArt.MenuUi("40K_button_square_pressed");
        Check(texN != null, "B0 前提：`40K_button_square` 取得到（`CardArt.MenuUi`）");
        Check(texH != null, "B0b 前提：`40K_button_square_hover` 取得到");
        Check(texP != null, "B0c 前提：`40K_button_square_pressed` 取得到");
        if (texN == null || texH == null || texP == null) return;   // 前提已硬断（上面三条），这里只为不 NRE
        Check(texN.width == 103 && texN.height == 107 && texH.width == 103 && texH.height == 107
              && texP.width == 103 && texP.height == 107,
              "B0d 前提：三张图都是 103×107（`Sprite/*.json` 同值；尺寸不同也会触发重切，别混进来）｜实得 "
              + texN.width + "×" + texN.height + " / " + texH.width + "×" + texH.height + " / " + texP.width + "×" + texP.height);

        var root = new GameObject("B142E17_probe");
        try
        {
            float boxW = LayoutSpace.Px(BoxPx), boxH = LayoutSpace.Px(BoxPx);

            // ---------- 树 1：三档 border 各不同（`40K_button_square` 家族） ----------
            var nine = ImageQuad.CreateNineSlice(root.transform, texN, BN, texN.width, texN.height,
                                                 Vector3.zero, boxW, boxH, "Btn", BN / PpuMul, true);
            Check(nine != null, "B1 前提：九宫格建出来了");
            if (nine == null) return;
            var before = nine.GetComponentsInChildren<ImageQuad>();
            Check(before.Length == 9, "B1b 前提：9 块都在（120×120 px 的框 > 三档角块和 ⇒ 无逐轴夹）｜实得 " + before.Length);
            var c00 = Cell(nine, "Btn_00");
            var m10 = Cell(nine, "Btn_10");
            Check(c00 != null && m10 != null, "B1c 前提：左下角块（`Btn_00`）与中段（`Btn_10`）按名字找得到");
            if (c00 == null || m10 == null) return;

            Check(Near(c00.WorldW, 36f / PpuMul / PPU) && NearTight(c00.UvRect.width, 36f / 103f),
                  "B2 建件那一刻 = 常态图 (36,28,37,29)：角块宽 " + (36f / PpuMul / PPU).ToString("F5")
                  + " · uv 分界 " + (36f / 103f).ToString("F5") + "｜实得 "
                  + c00.WorldW.ToString("F5") + " · " + c00.UvRect.width.ToString("F5"));

            var hit = new GameObject("Hit");
            hit.transform.SetParent(root.transform, false);
            var w = hit.AddComponent<WindowButton>();
            w.BindNine(nine, "40K_button_square", "40K_button_square_hover", "40K_button_square_pressed", null,
                       Meta(BN, PpuMul), Meta(BH, PpuMul), Meta(BP, PpuMul));
            Check(w.HoverTexForTest == texH && w.PressedTexForTest == texP,
                  "B2b 前提：悬停/按下图都接上了（取不到则 B3/B4 恒真）");

            w.Enter();   // = 指针进来 ⇒ SwapTo(_hoverTex) ⇒ 按 hover 的 border 重切
            Check(Near(c00.WorldW, 32f / PpuMul / PPU),
                  "B3 ★ **悬停：角块按 hover 的 border 重切**（32÷2.5 px = " + (32f / PpuMul / PPU).ToString("F5")
                  + "）｜实得 " + c00.WorldW.ToString("F5"));
            Check(Near(m10.WorldW, (BoxPx - 32f / PpuMul - 34f / PpuMul) / PPU),
                  "B3b ★ **中段宽也跟着新 border 变**（" + ((BoxPx - 32f / PpuMul - 34f / PpuMul) / PPU).ToString("F5")
                  + "；只改角块不改中段 ⇒ 这里红）｜实得 " + m10.WorldW.ToString("F5"));
            Check(NearTight(c00.UvRect.width, 32f / 103f),
                  "B3c ★ 角块 uv 分界 = **新图**的 32/103（旧行为会停在 36/103）｜实得 " + c00.UvRect.width.ToString("F5"));

            var after = nine.GetComponentsInChildren<ImageQuad>();
            int same = 0;
            for (int i = 0; i < before.Length && i < after.Length; i++) if (ReferenceEquals(before[i], after[i])) same++;
            Check(after.Length == before.Length && same == before.Length,
                  "B3e ★ 9 块是**同一批对象**（原地改几何/uv，⛔ 不是销毁重建 —— 保住 target/染色表/灰材质/队列）｜实得 "
                  + same + "/" + before.Length);

            w.Press();   // 按下 ⇒ pressed 那一档
            Check(Near(c00.WorldW, 28f / PpuMul / PPU) && NearTight(c00.UvRect.width, 28f / 103f),
                  "B4 ★ **按下：按 pressed 的 (28,27,27,25) 重切**（角块 " + (28f / PpuMul / PPU).ToString("F5")
                  + " · uv " + (28f / 103f).ToString("F5") + "）｜实得 "
                  + c00.WorldW.ToString("F5") + " · " + c00.UvRect.width.ToString("F5"));

            w.Release(); // 松开但仍悬停 ⇒ 回 hover 那一档
            Check(Near(c00.WorldW, 32f / PpuMul / PPU),
                  "B5 松开（仍悬停）⇒ 回到 hover 那档（32÷2.5）｜实得 " + c00.WorldW.ToString("F5"));

            w.Exit();    // 离开 ⇒ 回常态
            Check(Near(c00.WorldW, 36f / PpuMul / PPU) && NearTight(c00.UvRect.width, 36f / 103f),
                  "B6 离开 ⇒ 回常态 (36,28,37,29)（角块 " + c00.WorldW.ToString("F5")
                  + " · uv " + c00.UvRect.width.ToString("F5") + "）");

            // ---------- B8 · 灭自证：不同 border 时，人为挪动会被**纠回** ----------
            var p0 = c00.transform.localPosition;
            c00.transform.localPosition = p0 + new Vector3(0.01f, 0f, 0f);
            w.Enter();
            float wantX = -boxW * 0.5f + (32f / PpuMul / PPU) * 0.5f;
            Check(NearTight(c00.transform.localPosition.x, wantX),
                  "B8 ★ **灭自证**：不同 border ⇒ 人为挪动被**纠回**算式中心（" + wantX.ToString("F5")
                  + "）｜实得 " + c00.transform.localPosition.x.ToString("F5")
                  + " —— 档没被纠回 ⇒ B7/B9 的「挪动仍在」就不可信");
            w.Exit();

            // ---------- 树 2：**同 border** 家族（`40K_dropdown_field_closed/opened` 全 60/35/60/35） ----------
            var texC = CardArt.MenuUi("40K_dropdown_field_closed");
            var texO = CardArt.MenuUi("40K_dropdown_field_opened");
            Check(texC != null && texO != null, "B7 前提：`40K_dropdown_field_closed/opened` 两张都取得到");
            if (texC != null && texO != null)
            {
                var n2 = ImageQuad.CreateNineSlice(root.transform, texC, BF, texC.width, texC.height,
                                                   Vector3.zero, LayoutSpace.Px(250f), LayoutSpace.Px(59.4f), "Dd", BF, true);
                Check(n2 != null, "B7b 前提：同 border 那棵树建出来了");
                if (n2 != null)
                {
                    var d = Cell(n2, "Dd_00");
                    Check(d != null, "B7c 前提：`Dd_00` 在");
                    if (d != null)
                    {
                        var h2 = new GameObject("Hit2");
                        h2.transform.SetParent(root.transform, false);
                        var w2 = h2.AddComponent<WindowButton>();
                        w2.BindNine(n2, "40K_dropdown_field_closed", "40K_dropdown_field_opened",
                                    "40K_dropdown_field_opened", null,
                                    Meta(BF, 1f), Meta(BF, 1f), Meta(BF, 1f));
                        var q0 = d.transform.localPosition;
                        d.transform.localPosition = q0 + new Vector3(0.01f, 0f, 0f);
                        var moved = d.transform.localPosition;
                        w2.Enter();
                        Check(NearTight(d.transform.localPosition.x, moved.x),
                              "B7 ★ **同 border ⇒ 早退**：人为挪动**仍在**（= 一次都没重切；"
                              + "回归面 = `B141_W81_Assert` 的 B4–B7b 那套引用不许被换掉）｜x="
                              + d.transform.localPosition.x.ToString("F5"));
                        Check(d.Texture == texO, "B7d 同 border 早退**不能连坐换图**：纹理照换到 `_opened`");
                        w2.Exit();
                    }
                }
            }

            // ---------- 树 3：**默认 null**（不传 meta）= 2026-11-04 之前的行为 ----------
            if (texC != null && texO != null)
            {
                var n3 = ImageQuad.CreateNineSlice(root.transform, texC, BF, texC.width, texC.height,
                                                   Vector3.zero, LayoutSpace.Px(250f), LayoutSpace.Px(59.4f), "Nl", BF, true);
                var e = Cell(n3, "Nl_00");
                Check(n3 != null && e != null, "B9 前提：默认 null 那棵树建出来了");
                if (n3 != null && e != null)
                {
                    var h3 = new GameObject("Hit3");
                    h3.transform.SetParent(root.transform, false);
                    var w3 = h3.AddComponent<WindowButton>();
                    w3.BindNine(n3, "40K_dropdown_field_closed", "40K_dropdown_field_opened", "40K_dropdown_field_opened", null);
                    var r0 = e.transform.localPosition;
                    e.transform.localPosition = r0 + new Vector3(0.01f, 0f, 0f);
                    var moved3 = e.transform.localPosition;
                    w3.Enter();
                    Check(NearTight(e.transform.localPosition.x, moved3.x),
                          "B9 ★ **默认 null = 今天的行为**：一次都不重切（挪动仍在）｜x="
                          + e.transform.localPosition.x.ToString("F5"));
                    Check(e.Texture == texO, "B9b 默认 null 那棵树纹理照换（换图那半没被连坐）");
                    w3.Exit();
                }
            }

            // ---------- 树 4：`DeckSelectionPopup` 两颗页签那一族（`40K_tab_button` ↔ `…_overwindow` · ppuMul 1.5） ----------
            //   ⚠️ 原版这两颗 `m_Transition = 0` ⇒ **指针走不到 Selected 档**；这里直调态入口 `SetSelected(true)`，
            //   只证明「meta 已登记 + 换档会重切」这条机制是通的（生产上不可观测 —— 如实记，别写成「画面变好了」）。
            var tabOn = CardArt.MenuUi("40K_tab_button");
            var tabOff = CardArt.MenuUi("40K_tab_button_overwindow");
            Check(tabOn != null && tabOff != null, "B10 前提：`40K_tab_button` / `…_overwindow` 两张都取得到");
            if (tabOn != null && tabOff != null)
            {
                var tBon = new Vector4(183f, 0f, 128f, 29f);
                var tBoff = new Vector4(188f, 0f, 99f, 30f);
                const float tabPpu = 1.5f;
                var n4 = ImageQuad.CreateNineSlice(root.transform, tabOn, tBon, tabOn.width, tabOn.height,
                                                   Vector3.zero, LayoutSpace.Px(260f), LayoutSpace.Px(67.6421f), "Tab", tBon / tabPpu, true);
                var f = Cell(n4, "Tab_01");
                Check(n4 != null && f != null, "B10b 前提：页签九宫格建出来了（`Tab_01` 在 —— `Tab_00` **结构性不存在**：border `(183,0,128,29)` 的下边 = 0 ⇒ 底排 `j=0` 的高 = 0，被 `ImageQuad.CreateNineSlice` 的 `if (w <= 0f || h <= 0f) continue;` 跳过，见 `Battle/ImageQuad.cs:585`；`Tab_01` = 左列中间行，宽仍是 `wl`）");
                if (n4 != null && f != null)
                {
                    var h4 = new GameObject("Hit4");
                    h4.transform.SetParent(root.transform, false);
                    var w4 = h4.AddComponent<WindowButton>();
                    var mOn = Meta(tBon, tabPpu);
                    var mSel = Meta(tBoff, tabPpu);   // 原版 `m_SelectedSprite = …_overwindow`
                    w4.BindNine(n4, "40K_tab_button", "40K_tab_button", "40K_tab_button", "40K_tab_button_overwindow",
                                mOn, mOn, mOn, mSel);
                    Check(Near(f.WorldW, 183f / tabPpu / PPU),
                          "B10c 建件那一刻 = 常态 `(183,0,128,29)`（角块 183÷1.5 px = " + (183f / tabPpu / PPU).ToString("F5")
                          + "）｜实得 " + f.WorldW.ToString("F5"));
                    w4.SetSelected(true);   // = Selected 档（指针不可达，这里直调）
                    Check(Near(f.WorldW, 188f / tabPpu / PPU) && NearTight(f.UvRect.width, 188f / 489f),
                          "B10d ★ Selected 档按 `…_overwindow` 的 `(188,0,99,30)` 重切（角块 " + (188f / tabPpu / PPU).ToString("F5")
                          + " · uv " + (188f / 489f).ToString("F5") + "）｜实得 " + f.WorldW.ToString("F5")
                          + " · " + f.UvRect.width.ToString("F5"));
                    w4.SetSelected(false);
                    Check(Near(f.WorldW, 183f / tabPpu / PPU),
                          "B10e 取消选中 ⇒ 回常态那档（183÷1.5）｜实得 " + f.WorldW.ToString("F5"));
                }
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
