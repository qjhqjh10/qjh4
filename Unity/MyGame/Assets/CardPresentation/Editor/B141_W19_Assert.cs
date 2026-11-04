// B141_W19_Assert.cs — 执行代理 `B141-W19`（第一百二十八会话 · 波次 `W129` · B138 §4 `4-14` / `4-22`：Tooltip 面板几何 + 正文字号）
//
// ============================ 它盯的是什么 ============================
//  两条都是 `Core/Tooltip.cs` 的**参数**，不是新功能。原版判据全部本地现读：
//   · **4-14 面板几何** —— `d:/2/tools/assetripper_out/ExportedProject/Assets/BasicToolTip.prefab`：
//       · 根 `RectTransform`（`m_Father: {fileID: 0}`，`:100-119`）`m_SizeDelta = (500, 100)`；
//       · 根的唯一子件 `Generic Popup Background`（`m_GameObject = 1730242686860875`，`:171-176`）挂
//         `VerticalLayoutGroup`（`m_Padding` 在 `:228` = L0/R0/T10/B10；`m_ChildControlWidth: 0` 在 `:237`）
//         + `ContentSizeFitter`（`m_VerticalFit: 1`，`:241-255`）⇒ **面板宽恒 500、高度由子件撑**；
//       · 正文 `Text (TMP)`（`:406-424`）`m_SizeDelta.x = 468.90002` ⇒ 左右内边距 = (500 − 468.9) / 2 = **15.55**；
//       · 正文那颗自定义 fitter（`:537-544`：`m_VerticalFit: 2` · `clampHeight: 1` · `heightMin: 90` · `heightMax: 200`）⇒
//         面板高 = 正文高 + 20 ∈ **[110, 220]**。
//       ⚠️ **根上的 100 是模板值**（⛔ 别照抄成运行期面板高）：布局组不控宽、高度由 fitter 撑 ⇒ 运行时从不等于 100。
//   · **4-22 正文字号** —— 同 prefab `Text (TMP)` `m_fontSize = 28`（`:481`，`m_enableAutoSizing: 1` / min 10 / max 28）；
//     Pragati 的「大写墨高 ÷ em」实测 **0.656**（`Core/TmpFont.cs` 的 `CapPerFontSize` / `EmPerFontSize`）
//     ⇒ `BodyCapWorld = 0.656 × 28 / 108`。原写 `0.72`（`capLine/pointSize` 的 em 口径推算、非实测）⇒ 字大 **9.7%**。
//
// ============================ 🔴 灭自证（🧨 什么坏法会让哪条红）============================
//  · A2 断「反推出的正文宽 = 468.9」，而旧 `PadX = 20` 给出 **460** ⇒ **把 PadX 改回 20 ⇒ A2/A3 立刻红**
//    （B1 把这条差写成结构式：两档**不可能同时满足**）。
//  · A9 的期望值**从字体资产实测反算**（`TmpFont.CapPerFontSize` × `EmPerFontSize` ⇒ em px），
//    ⛔ **不是把常量再乘一遍**（否则就是自证）；把它改回 0.72 ⇒ em 读回 30.73 ⇒ A9 红。
//  · B5 直接钉「**不是 0.72 那一档**」；B2/B3/B4 钉「不是 PadX 20 / PadY 14 / MinW 120」—— 旧值各有一条独立的红。
//  · A11 读源码文本断 `🔶 自己设计` **真的在**（4-14 的原症状就是「自设计但没打标」，`grep "🔶"` = 0 命中）。
//  · C 段是**实例级**：真 `Show` 一条短文案 ⇒ 读 `PanelSizePx` / `DebugDump` 的 `cap=`，
//    证明这些常量**真进了布局与标签**（⛔ 不只是改了常量）。建不出件 ⇒ C0b **前提先红**（⛔ 不静默跳过）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主里加：
//      B141_W19_Assert.Run(); pass += B141_W19_Assert.Pass; fail += B141_W19_Assert.Fail;
//  推荐宿主 = `Editor/BattleScene.cs`（tooltip 在那里是 uGUI 档、已有 tooltip 一组用例）或 `Editor/ShellScene.cs`（9 s，最便宜）。
//  ⚠️ C 段会建一只 tooltip，**结束前 `DestroyImmediate`** —— 单例是**静态**的，
//     不清的话后面的用例会读到「本段建的、档位可能已过时」的那只（`Tooltip.InstanceForTest` 的注释写着同一件事）。
//  判绿红：按行首 `B141W19 ✓` / `B141W19 ✗` 数（⛔ 别 `grep -c ✗`）。

using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141-W19`：`4-14` Tooltip 面板几何（500×110 / 正文宽 468.9）+ `4-22` 正文字号（fs28 × 0.656）。</summary>
public static class B141_W19_Assert
{
    const string Tag = "B141W19";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void Near(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（" + got.ToString("F4") + " ≈ " + want.ToString("F4") + " ± " + tol.ToString("F4") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        Debug.Log(Tag + " --- 4-14 Tooltip 面板几何 + 4-22 正文字号（判据 = 原版 `BasicToolTip.prefab`）---");
        GroupA_OriginalValues();
        GroupB_Nullified();
        GroupC_Applied();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 原版值（`BasicToolTip.prefab` 逐字段）
    // ==================================================================
    static void GroupA_OriginalValues()
    {
        // ---- 4-14 面板几何 ----
        Near(Tooltip.MinW, 500f, 0.001f,
             "A1 ★ 面板**宽下限 = 500**（原版根 `m_SizeDelta.x = 500`；布局组 `m_ChildControlWidth = 0` ⇒ 宽恒 500）");

        // 由「面板宽 − 两个内边距」反推正文宽 —— 这一条才是「正文宽 468.9」的可执行形式
        float bodyW = Tooltip.MinW - Tooltip.PadX * 2f;
        Near(bodyW, 468.9f, 0.01f,
             "A2 ★ 面板宽 − 2×左右内边距 = **正文宽 468.9**（原版 `Text (TMP)` `m_SizeDelta.x = 468.90002`）");

        Near(Tooltip.PadX, (500f - 468.9f) / 2f, 0.001f,
             "A3 左右内边距 = **(500 − 468.9) / 2 = 15.55**（原版没有 pad 常量，是从两个矩形反推的）");

        Near(Tooltip.PadY, 10f, 0.001f,
             "A4 上下内边距 = **10**（原版 `Generic Popup Background` 的 `VerticalLayoutGroup.m_Padding` Top 10 / Bottom 10）");

        Near(Tooltip.MinH, 110f, 0.001f,
             "A5 ★ 面板**高下限 = 110**（原版正文自定义 fitter `heightMin 90` + 上下各 10）"
           + "｜🧨 去掉 `MinH` ⇒ 单行面板只有 50 px");

        // ⚠️ 根上那个 100 是模板值 —— 把「它为什么不是 110」写进断言文案，免得下个人照抄 100
        Check(Mathf.Abs(Tooltip.MinH - 100f) > 1f,
             "A6 面板高下限**不是根上那个 100**（100 是 prefab 模板值：布局组不控宽、高度由 fitter 撑 ⇒ 运行时从不等于 100）");

        // ---- 4-22 正文字号 ----
        Near(Tooltip.BodyCapWorld * Tooltip.PxPerUnit / 28f, 0.656f, 0.0005f,
             "A7 ★ 正文「大写高 ÷ m_fontSize」= **0.656**（Pragati 实测比值；旧值 0.72 = em 口径推算）");

        // 期望值**从字体资产实测反算**（⛔ 不是把 0.656 再乘一遍）
        float cap = TmpFont.CapPerFontSize(TmpFont.Face.LatinCardBody);
        float em = TmpFont.EmPerFontSize(TmpFont.Face.LatinCardBody);
        Check(cap > 0.05f && cap < 0.09f && em > 0.05f && em < 0.15f,
              "A8（前提）Pragati 的「大写墨高 ÷ fontSize」与「em ÷ fontSize」都量到了"
            + "（cap=" + cap.ToString("F5") + " · em=" + em.ToString("F5") + "；量不到说明字体资产没载入 ⇒ 下面 A9 不算数）");
        float emPx = Tooltip.BodyCapWorld / cap * em * Tooltip.PxPerUnit;
        Near(emPx, 28f, 0.3f,
             "A9 ★ 正文按字体资产实测链路**读回 em = 28 px**（= 原版 `m_fontSize`）｜🧨 把 `BodyCapWorld` 改回 0.72 那一档 ⇒ 读回 30.73 ⇒ 本红");

        // ---- 4-14 的第二半：自设计必须打 🔶 ----
        string p = Application.dataPath + "/CardPresentation/Core/Tooltip.cs";
        string src = File.Exists(p) ? File.ReadAllText(p) : "";
        Check(src.Length > 0, "A10（前提）读得到 `Core/Tooltip.cs` 源码（" + p + "）");
        int marks = CountOf(src, "🔶 自己设计");
        Check(marks >= 1,
              "A11 ★ 4-14 要求的 `🔶 自己设计` 标记**真的在**（改之前 `grep -n \"🔶\" Core/Tooltip.cs` = **0 命中**，只有散文「⚠️ 我们挑的」）｜实得 "
            + marks + " 处｜🧨 删掉那条标记 ⇒ 本红");
    }

    // ==================================================================
    //  B · 灭自证：旧值那一档结构上不可能同时满足
    // ==================================================================
    static void GroupB_Nullified()
    {
        // 旧 PadX = 20 ⇒ 正文宽只有 460。把两个数并列写出来，任何人「两边一起改回去」都会先撞 A2。
        float oldBodyW = 500f - 20f * 2f;
        Check(Mathf.Abs(oldBodyW - 468.9f) > 1f,
              "B1 旧 `PadX = 20` 推出的正文宽 = **460**，原版 468.9（差 " + (468.9f - oldBodyW).ToString("F1")
            + " px）⇒ 改回去 **A2 必红**");

        Check(Mathf.Abs(Tooltip.PadX - 20f) > 0.5f, "B2 `PadX` **不是旧的 20**（实得 " + Tooltip.PadX.ToString("F2") + "）");
        Check(Mathf.Abs(Tooltip.PadY - 14f) > 0.5f, "B3 `PadY` **不是旧的 14**（实得 " + Tooltip.PadY.ToString("F2") + "）");
        Check(Mathf.Abs(Tooltip.MinW - 120f) > 1f, "B4 `MinW` **不是旧的 120**（旧值让短文案的面板只有一百多 px，而原版恒 500；实得 " + Tooltip.MinW.ToString("F1") + "）");

        float ratio = Tooltip.BodyCapWorld * Tooltip.PxPerUnit / 28f;
        Check(Mathf.Abs(ratio - 0.72f) > 0.01f,
              "B5 ★ 正文比值**不是 0.72 那一档**（旧值 ⇒ 字大 9.7%）｜实得 " + ratio.ToString("F4")
            + "｜🧨 改回 `0.72f * 28f / PxPerUnit` ⇒ 本红 + A7/A9 红");

        // 自设计的两个必须留在自设计里：留着生长余量（否则长句字画到底图外）
        Check(Tooltip.MaxW > Tooltip.MinW,
              "B6 `MaxW`（自设计）比 `MinW` 大 ⇒ 长句仍有生长余量（原版宽恒 500，我们的 `Label` 按字符数折行且不裁切）｜实得 "
            + Tooltip.MaxW.ToString("F1") + " > " + Tooltip.MinW.ToString("F1"));
    }

    // ==================================================================
    //  C · 实例级：常量真的进了布局与标签
    // ==================================================================
    static void GroupC_Applied()
    {
        // 单例是静态的：先把上一轮留下的件清掉（档位可能已经变了），并解开别人的抑制
        var stale = Tooltip.InstanceForTest;
        if (stale != null) { Tooltip.Hide(); Tooltip.FinishFade(); UnityEngine.Object.DestroyImmediate(stale); }
        bool wasSuppressed = Tooltip.TooltipsSuppressed;
        if (wasSuppressed)
        {
            Debug.Log(Tag + " ⚠ 进入 C 段时 tooltip 被别的 owner 抑制着（" + Tooltip.SuppressorCountForTest
                          + " 个）⇒ 已按自检口清空（否则 `Show` 直接返回，本段会误红）");
            Tooltip.ResetSuppressionsForTest();
        }
        Check(!Tooltip.TooltipsSuppressed, "C0a（前提）清空后没有 owner 再抑制 tooltip");

        Tooltip.Show("x", Vector3.zero, 0);
        Tooltip.FinishFade();
        bool built = Tooltip.InstanceForTest != null;
        Check(built, "C0b（前提）真建出了一只 tooltip（`InstanceForTest != null`）｜⛔ 建不出就别读尺寸（本行先红，不静默跳过）");
        if (!built) return;

        Vector2 sz = Tooltip.PanelSizePx;
        Near(sz.x, 500f, 0.5f,
             "C1 ★ 短文案的面板**宽 = 500 px**（= 原版根 `m_SizeDelta.x`；`MinW` 真进了布局）｜🧨 把 `MinW` 改回 120 ⇒ 本红");

        Near(sz.y, 110f, 0.5f,
             "C2 ★ 单行短文案的面板**高 = 110 px**（= 原版正文 fitter `heightMin 90` + 上下各 10；`MinH` 真进了布局）"
           + "｜🧨 把 `_h` 改回 `lines*LineH + PadY*2` ⇒ 50 ⇒ 本红");

        Check(Tooltip.BodyTextForTest == "x",
              "C3 正文真的喂进去了（`BodyTextForTest`）｜实得「" + (Tooltip.BodyTextForTest ?? "(null)") + "」");

        string dump = Tooltip.DebugDump() ?? "";
        float capApplied = ParseAfter(dump, "cap=");
        float want = Tooltip.UguiMode ? Tooltip.BodyCapWorld * Tooltip.PxPerUnit : Tooltip.BodyCapWorld;
        Near(capApplied, want, Mathf.Max(0.0015f, Mathf.Abs(want) * 0.002f),
             "C4 ★ 正文标签上**真挂着** 0.656×28 那个大写高（`DebugDump` 的 `cap=`；本档 = " + (Tooltip.UguiMode ? "uGUI（设计 px）" : "世界（世界单位）")
           + "，期望 " + want.ToString("F4") + "）｜🧨 改回 0.72 那一档 ⇒ 本红");

        // ---- 真长文案：面板宽仍必须 = 正文宽 + 2×pad（= **没有被 `MaxW` 截**）----
        // 这一条同时把「我们按字符数折行后的真实正文宽」打进日志 —— 那是「`MaxCharsPerLine = 22` 单位
        // 与原版 468.9 px 还差多少」的唯一实测口径（本代理不跑 Unity ⇒ 只能留给宿主跑出来看）。
        Tooltip.Show(TipText.Health, Vector3.zero, 0);
        Tooltip.FinishFade();
        string body = Tooltip.BodyTextForTest ?? "";
        Check(body.Length > 20,
              "C5a（前提）长文案真的喂进去了（`TipText.Health` ；实得 " + body.Length + " 字）｜⛔ 不足 20 字说明 Loc 没载入，下面 C5 不算数");
        if (body.Length > 20)
        {
            string d2 = Tooltip.DebugDump() ?? "";
            float wMeasured = ParseAfter(d2, "W=");            // 第一个 `W=` 是 `WorldW`（`tmpW=` 在后面）
            float bodyPx = Tooltip.UguiMode ? wMeasured : wMeasured * Tooltip.PxPerUnit;
            Vector2 sz2 = Tooltip.PanelSizePx;
            Check(bodyPx > 0f && !float.IsNaN(bodyPx),
                  "C5b（前提）量到了正文宽（`DebugDump` 的 `W=`）｜实得 " + bodyPx.ToString("F2") + " px");
            Near(sz2.x, Mathf.Max(Tooltip.MinW, Mathf.Min(bodyPx + Tooltip.PadX * 2f, Tooltip.MaxW)), 0.6f,
                 "C5 ★ 长文案的面板宽 = `Clamp(正文宽 + 2×15.55, 500, 640)`（= 文档化的那一条；没有被硬卡成 500）"
               + "｜正文宽实测 " + bodyPx.ToString("F1") + " px（原版正文宽 468.9 ⇒ 差 "
               + (bodyPx - 468.9f).ToString("F1") + " px —— **那是我们按字符数折行的残差**，不是面板参数）");
            Check(sz2.x + 0.6f >= bodyPx + Tooltip.PadX * 2f,
                  "C6 ★ 面板宽**不小于**正文宽 + 2×pad ⇒ 字不会画到底图外（`MaxW` 真的没截它）"
                + "｜面板 " + sz2.x.ToString("F1") + " ≥ 正文 " + bodyPx.ToString("F1") + " + " + (Tooltip.PadX * 2f).ToString("F1")
                + "｜🧨 把 `MaxW` 调到比 " + (bodyPx + Tooltip.PadX * 2f).ToString("F0") + " 小 ⇒ 本红");
        }

        Tooltip.Hide();
        Tooltip.FinishFade();
        var go = Tooltip.InstanceForTest;
        if (go != null) UnityEngine.Object.DestroyImmediate(go);
        Tooltip.ResetSuppressionsForTest();
    }

    // ==================================================================
    //  小工具
    // ==================================================================

    /// <summary>从 `DebugDump()` 那种 `key=数值` 串里取一个 float（取不到 ⇒ `NaN` ⇒ 断言红，⛔ 不当 0）。</summary>
    static float ParseAfter(string s, string key)
    {
        if (string.IsNullOrEmpty(s)) return float.NaN;
        int i = s.IndexOf(key, System.StringComparison.Ordinal);
        if (i < 0) return float.NaN;
        i += key.Length;
        var sb = new System.Text.StringBuilder();
        while (i < s.Length)
        {
            char c = s[i];
            if (char.IsDigit(c) || c == '.' || c == '-' || c == '+' || c == 'e' || c == 'E') { sb.Append(c); i++; }
            else break;
        }
        float v;
        return float.TryParse(sb.ToString(), System.Globalization.NumberStyles.Float,
                              System.Globalization.CultureInfo.InvariantCulture, out v) ? v : float.NaN;
    }

    static int CountOf(string s, string sub)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(sub)) return 0;
        int n = 0, i = 0;
        while ((i = s.IndexOf(sub, i, System.StringComparison.Ordinal)) >= 0) { n++; i += sub.Length; }
        return n;
    }
}
