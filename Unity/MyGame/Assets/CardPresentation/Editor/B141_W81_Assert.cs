// B141_W81_Assert.cs — 执行写手 `B141-W81`（第一百二十八会话 · 2026-11-04）· W129 **`B141-BINDNINE`（P1）**
//
// ============================ 它盯的是什么 ============================
//  🔴 **真 bug**：`Shell/WindowButton.cs` 的 `BindNine` 记的 `_targetAspect` = `qs[0]`（**左下角块**）的比，
//  而 `SwapTo → SetOn` 原来对**9 块全** `SetAspect(_targetAspect)` ⇒ 中段/边块被改成角块的比、
//  `ImageQuad.WorldW = _worldH × _aspect` ⇒ **九宫格中段被压扁**（社交下拉框 1.204 → 0.556，边框中间断开）。
//  影响面 = **全部 14 个生产 `BindNine` 站点**（＋1 处自检探针；其中 `DeckSelectionPopup` 那 2 颗是 `B141-W44` 刚接的选中图）。
//
//  ✅ **修法**（判据 = UGUI `Image.GenerateSlicedSprite`：`Sliced` 换 sprite 时角块按 `sprite.m_Border`
//  **不缩放**、只有中段/边段拉伸）：九宫格子块换图**只换纹理、比例一块都不动**
//  （`ImageQuad.SetTexture(t, keepAspect: true)`）；单 quad 站点那套「换图后拉回绑定时的比」**原样保留**。
//  ✅ 修好后接上 W70 因该 bug 暂缓的 **社交 6 个下拉框**的 `selectedArt`（`Shell/AllianceMemberTab.cs` 的
//  `AllianceDropdownBox.Create` —— 那一处就是 6 个 prefab 实例的唯一站点）。
//
// ============================ 断言 ============================
//  A 组（源码 / 接线，零前提）：
//    A1  `SetOn` 里九宫格子块走 `keepAspect: true` 那条分支
//    A2  `SetOn` 里**单 quad** 仍然 `SetAspect(_targetAspect)`（⛔ 别为了修九宫格把单 quad 也放掉：几百处站点靠它）
//    A3  `InNine` 存在且按数组逐项比（不是「按名字/按 index 0」这种脆判据）
//    A4  `BindNine` 的 doc 写明「比例用每块自己那一格」（防止下个会话又改成统一 aspect）
//    A5  社交 6 个下拉框**真的接上了**：`BindNine(bgClosed, ArtFieldClosed, ArtFieldOpened ×3)`
//    A6  两个常量 = 原版逐颗实读的图名（常态 `_closed`；悬停/按下/选中三档同值 `_opened`）
//    A7  `AllianceMemberTab` 里 `.BindNine(` **只有 1 处**（= 6 个实例共用的那个站点；多一处就意味着有人另开了判据）
//    A8  两扇社交窗的构造点**都**在调 `AllianceDropdownBox.Create`（⇒ 那 1 处真的覆盖 6 个实例）
//  B 组（运行时探针 —— 用**真实图 + 真实 border + 真实框尺寸**建一份九宫格）：
//    B1 前提：九宫格建出来、块数 = 6（250×59.4 + border (60,35,60,35) ⇒ 竖中段高为 0、那两块不画）
//    B2 换图**前**中段（`Bg_10`）世界宽 = **1.2037**（±0.002）
//    B3 换图**前**左下角块（`Bg_00`）比例 = **2.02**（±0.01）—— 它就是 `_targetAspect` 那份值
//    B4 **悬停换图后中段宽不变**（±0.002）—— 本条就是这次修的主断言
//    B5 **灭自证**：中段宽 ≠ 角块宽（差 > 0.1）—— 改回「9 块统一 `_targetAspect`」时它变成 0.556 ⇒ B4/B5 一起红
//    B6 **灭自证**：悬停后 **6 块纹理全换成 `_opened`** —— 挡「什么都不换也算过」
//    B7 松开后中段仍 1.2037、纹理回 `_closed`
//    B8 **单 quad 站点不许回归**：比例 2.5 的 quad 悬停换图后仍是 2.5
//
// ============================ 灭自证（🧨 什么坏法会让哪条红）============================
//  · `SetOn` 里删掉 `InNine` 那条分支（回到「9 块全 `SetAspect`」）⇒ **A1 / B4 / B5** 红。
//  · 把 `SetOn` 改成「九宫格子块什么都不做」⇒ **B6** 红（纹理没换）。
//  · 把单 quad 那句 `SetAspect(_targetAspect)` 一起删掉（顺手改坏）⇒ **A2 / B8** 红。
//  · 删掉 `AllianceDropdownBox.Create` 里的 `BindNine` ⇒ **A5 / A7** 红。
//  · 把 `ArtFieldOpened` 写成 `_closed`（或三档里任一处传错）⇒ **A6** 红。
//  · 把 `InNine` 改成「只认 `_nine[0]`」⇒ **B6**（其余 5 块不换）／**B4** 之一红。
//
// ============================ 怎么接（本文件不碰宿主聚合）============================
//  `public static void Run();` —— **零前提**（只读 3 份源码文本 + 2 张图 + 一个用完即销毁的探针树）。
//  挂法（由调度台决定）：同一物理行追加
//      B141_W81_Assert.Run(); pass += B141_W81_Assert.Pass; fail += B141_W81_Assert.Fail;
//  判绿红：按行首 `B141W81 ✓` / `B141W81 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141-W81`：`BindNine` 压扁九宫格的修复 + 社交 6 个下拉框 `selectedArt` 接线的回归断言。</summary>
public static class B141_W81_Assert
{
    const string Tag = "B141W81";
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数。</summary>
    public static int Fail { get { return _fail; } }

    // ---- 判据里的那几个数（报告 §一 的实算；⚠️ 任何一条改了都要同步报告）----
    /// <summary>探针用的下拉框宽（设计 px）= 社交 6 个下拉框的实宽。</summary>
    public const float FieldPx = 250f;
    /// <summary>探针用的下拉框高（设计 px）。</summary>
    public const float FieldPxH = 59.4f;
    /// <summary>`40K_dropdown_field_*` 的九宫 border（左,下,右,上 · 贴图像素）。</summary>
    public static readonly Vector4 FieldBorder = new Vector4(60f, 35f, 60f, 35f);
    /// <summary>换图**前/后**中段都该是这个宽度（世界单位）= (250 − 60 − 60) ÷ 108 = **1.2037**。</summary>
    public const float MidWorldW = 1.2037037f;
    /// <summary>角块宽（世界单位）= 60 ÷ 108 = 0.5556 —— 旧 bug 会把中段压成这个值。</summary>
    public const float CornerWorldW = 0.5555556f;
    /// <summary>本修法覆盖的 `BindNine` **生产**站点数（2026-11-04 现读 14 处；另 1 处是 `Editor/ShellScene.cs:4608` 的自检探针
    /// ⇒ 调用点合计 15 —— `grep -rn "\.BindNine(" --include=*.cs` 可复现；⚠️ `ShellScene` 注释里那句「全库 10 处」已过期）。</summary>
    public const int BindNineSites = 14;

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

    public static void Run()
    {
        _pass = 0; _fail = 0;
        GroupA_Wiring();
        GroupB_Runtime();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail
                  + "（九宫格站点 " + BindNineSites + " 处 · 社交下拉框 6 颗）");
    }

    // ==================================================================
    //  A · 源码 / 接线
    // ==================================================================
    static void GroupA_Wiring()
    {
        var wb = Src("Shell/WindowButton.cs");
        var mt = Src("Shell/AllianceMemberTab.cs");

        Check(wb.Contains("if (InNine(q)) { q.SetTexture(tex, keepAspect: true); return; }"),
              "A1 `SetOn`：九宫格子块走 `keepAspect: true`（只换图、比例不动）—— 本次修复的落点");
        Check(wb.Contains("if (_targetAspect > 0f) q.SetAspect(_targetAspect);"),
              "A2 `SetOn`：**单 quad** 站点仍然「换图后拉回绑定时的比」（全工程几百处 `Bind` 靠这一句，⛔ 别顺手删）");
        Check(wb.Contains("bool InNine(ImageQuad q)") && wb.Contains("for (int i = 0; i < _nine.Length; i++) if (_nine[i] == q) return true;"),
              "A3 `InNine` 按 `_nine` 数组逐项认（⛔ 不是「只认 `_nine[0]`」那种脆判据）");
        Check(wb.Contains("但**比例用每块自己那一格**"),
              "A4 `BindNine` 的 doc 写明「比例用每块自己那一格」（下个会话别又改回统一 aspect）");

        Check(mt.Contains("fieldWb.BindNine(bgClosed, ArtFieldClosed, ArtFieldOpened, ArtFieldOpened, ArtFieldOpened);"),
              "A5 社交下拉框合上面接上了 `BindNine`：常态 `_closed`，悬停/按下/选中**三档同值** `_opened`");
        Check(mt.Contains("const string ArtFieldClosed = \"40K_dropdown_field_closed\", ArtFieldOpened = \"40K_dropdown_field_opened\";"),
              "A6 两张图名 = 原版 6 个下拉框逐颗实读（`40K_dropdown_field_closed` / `…_opened`）");

        int n = 0, at = 0;
        while ((at = mt.IndexOf(".BindNine(", at, System.StringComparison.Ordinal)) >= 0) { n++; at += 10; }
        Check(n == 1,
              "A7 `AllianceMemberTab` 里 `.BindNine(` 只有 **1** 处（= 6 个 prefab 实例共用的那个站点）｜实得 " + n);

        // ---- A8 · 6 个下拉框**全部**走那一个站点（两个宿主的构造点都在调 `AllianceDropdownBox.Create`）----
        var st = Src("Shell/AllianceSearchTab.cs");
        Check(st.Contains("AllianceDropdownBox.Create(") && mt.Contains("AllianceDropdownBox.Create("),
              "A8 两扇社交窗的下拉框都经 `AllianceDropdownBox.Create` 建 ⇒ 那**一处** `BindNine` 覆盖全部 6 个实例"
              + "（Search 页 `Select Language` / `Select Privacy` 2 个 + 成员页两个变体的 `Config fields` 4 个）");
    }

    // ==================================================================
    //  B · 运行时探针（真实图 / 真实 border / 真实框尺寸）
    // ==================================================================
    static void GroupB_Runtime()
    {
        var tex = CardArt.MenuUi("40K_dropdown_field_closed");
        var texOpen = CardArt.MenuUi("40K_dropdown_field_opened");
        Check(tex != null, "B0 前提：`40K_dropdown_field_closed` 取得到（`CardArt.MenuUi`）");
        Check(texOpen != null, "B0b 前提：`40K_dropdown_field_opened` 取得到");
        if (tex == null || texOpen == null) return;

        var root = new GameObject("B141W81_probe");
        try
        {
            // 与 `AllianceDropdownBox.Create` 同参数：727×102 的图、border (60,35,60,35)、250×59.4 的框。
            var nine = ImageQuad.CreateNineSlice(root.transform, tex, FieldBorder, tex.width, tex.height,
                                                 Vector3.zero, LayoutSpace.Px(FieldPx), LayoutSpace.Px(FieldPxH), "Bg");
            var qs = nine != null ? nine.GetComponentsInChildren<ImageQuad>() : new ImageQuad[0];
            // 250×59.4 的框 + 35+35 的上下 border ⇒ 竖中段高被夹成 0（`CreateNineSlice` 跳过那两块）⇒ 正常 6 块；
            //   ⚠️ 浮点残差可能让 `hm` 拿到 +1e-8（于是那两块以 0 高建出来）⇒ 9 块也算通过（下面只认名字，不认块数）。
            Check(qs.Length == 6 || qs.Length == 9,
                  "B1 前提：九宫格建出 6 块（竖中段 = 0；浮点残差下可能 9 块）｜实得 " + qs.Length);

            ImageQuad mid = null, corner = null;
            for (int i = 0; i < qs.Length; i++)
            {
                if (qs[i] == null) continue;
                if (qs[i].gameObject.name == "Bg_10") mid = qs[i];       // i=1（中列）· j=0（下排）
                if (qs[i].gameObject.name == "Bg_00") corner = qs[i];    // 左下角块 = `BindNine` 取的那一块
            }
            Check(mid != null, "B1b 前提：中段那块（`Bg_10`）在");
            Check(corner != null, "B1c 前提：左下角块（`Bg_00`）在");
            if (mid == null || corner == null) return;

            float midW0 = mid.WorldW;
            float cornerAspect = corner.WorldH > 0f ? corner.WorldW / corner.WorldH : 0f;
            Check(Mathf.Abs(midW0 - MidWorldW) < 0.002f,
                  "B2 换图**前**中段世界宽 = " + MidWorldW.ToString("F4") + "（实得 " + midW0.ToString("F4") + "）");
            Check(Mathf.Abs(cornerAspect - 2.0202f) < 0.01f,
                  "B3 角块比例 = 2.02（= 旧代码套给 9 块的那个 `_targetAspect`）｜实得 " + cornerAspect.ToString("F4"));

            var hit = new GameObject("Hit");
            hit.transform.SetParent(root.transform, false);
            var w = hit.AddComponent<WindowButton>();
            // 与接线处同一组实参（常态 `_closed`；悬停/按下/选中 = `_opened`）。
            w.BindNine(nine, "40K_dropdown_field_closed", "40K_dropdown_field_opened",
                       "40K_dropdown_field_opened", "40K_dropdown_field_opened");
            Check(w.HoverTexForTest != null, "B3b 前提：`40K_dropdown_field_opened` 作为悬停图接上了");

            w.Enter();   // = 指针进来 ⇒ `ApplyState → SwapTo(_hoverTex)` ⇒ 9 块一起换图
            Check(Mathf.Abs(mid.WorldW - MidWorldW) < 0.002f,
                  "B4 ★ **悬停换图后中段宽不变**（" + mid.WorldW.ToString("F4") + "，期望 " + MidWorldW.ToString("F4")
                  + "）—— 本次修的主断言");
            Check(Mathf.Abs(mid.WorldW - CornerWorldW) > 0.1f,
                  "B5 ★ 灭自证：中段宽 **≠ 角块宽**（" + CornerWorldW.ToString("F4") + "）—— 改回「9 块统一 `_targetAspect`」时这里变成 0.556");
            Check(Mathf.Abs(corner.WorldW - CornerWorldW) < 0.002f,
                  "B5b 角块自己的宽也没被改（" + corner.WorldW.ToString("F4") + "）");

            int swapped = 0;
            for (int i = 0; i < qs.Length; i++) if (qs[i] != null && qs[i].Texture == texOpen) swapped++;
            Check(swapped == qs.Length,
                  "B6 ★ 灭自证：悬停后 **" + qs.Length + " 块纹理全换成 `_opened`**（挡「什么都不换也算过」）｜实得 " + swapped);

            w.Exit();    // 松开 ⇒ 换回常态图
            Check(Mathf.Abs(mid.WorldW - MidWorldW) < 0.002f,
                  "B7 松开后中段仍 = " + MidWorldW.ToString("F4") + "（实得 " + mid.WorldW.ToString("F4") + "）");
            int back = 0;
            for (int i = 0; i < qs.Length; i++) if (qs[i] != null && qs[i].Texture == tex) back++;
            Check(back == qs.Length, "B7b 松开后 " + qs.Length + " 块纹理都回到 `_closed`｜实得 " + back);

            // ---- B8 · 单 quad 站点不许回归（那条 `_targetAspect` 分支还在工作）----
            var single = new GameObject("single");
            single.transform.SetParent(root.transform, false);
            var sq = ImageQuad.Create(single.transform, CardArt.Solid(), Vector3.zero, 1f, new Vector2(0.5f, 0.5f), "sq");
            Check(sq != null, "B8 前提：单 quad 建出来了（`CardArt.Solid()` 非空）");
            if (sq != null)
            {
                sq.SetAspect(2.5f);
                var sw = single.AddComponent<WindowButton>();
                sw.Bind(sq, "40K_button");       // 悬停图 = `40K_button_hover`（489×107，比例 ≠ 2.5）
                Check(sw.HoverTexForTest != null, "B8b 前提：`40K_button_hover` 取得到（取不到则 B8c 恒真）");
                sw.Enter();
                float afterAspect = sq.WorldH > 0f ? sq.WorldW / sq.WorldH : 0f;
                Check(Mathf.Abs(afterAspect - 2.5f) < 0.01f,
                      "B8c 单 quad 悬停换图后比例仍是 2.5（实得 " + afterAspect.ToString("F4")
                      + "）—— `_targetAspect` 那条分支没被顺手删掉");
                sw.Exit();
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}
