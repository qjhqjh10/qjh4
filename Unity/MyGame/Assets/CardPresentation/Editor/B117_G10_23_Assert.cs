// B117_G10_23_Assert.cs — 执行代理 `B117-G10-23`（战斗域第 23 部分：教程 · 战役脚本）
//
// ============================ 它盯的是哪一条 ============================
//  `Battle/TutorialOverlay.SetHighlight` 在 2026-11-02 之前把高亮贴图按**锚点元素尺寸 + `HighlightPad`**
//  摆（`HighlightPad = 0.06f` 自己标着「🔶 我们挑的 / 无判据」）。按铁律 18·0（自设计只允许「服务器相关」）
//  ⇒ 判据拿得到就不许自设计。判据拿到了：
//
//   ① **载体**（`bundle_scenes_scenes_battlearena1/GameObject/Tutorial highlight.json` →
//      `MonoBehaviour_5145.json`）：一颗 **stretch 全屏** `Image`，
//      `m_Sprite` = `Tutorial Highlight`（pid `-2216962027063784652`）、
//      `m_Material` = `Tutorial Highlight`（pid `509935442048338405`）、
//      `m_Color` = **白 α 0.8235294（= 210/255）**、`m_Type 0`(Simple)、`m_PreserveAspect 0`；
//      13 个战场场景各一份、同值。
//   ② **材质**（`bundle_duplicateassetisolation_assets_all/Material/Material_509935442048338405.json`）：
//      shader `Everguild/FX/Tutorial Highlight`；`_UVScale = (1.48, 2.0)`、`_ViewportPoint = (0.5, 0.5)`。
//   ③ **算式**（`工具/disasm_dxbc.py "Tutorial Highlight" --stage vs`，VS 逐句）：
//        `uv.x = _UVScale.x*(screenUV.x - _ViewportPoint.x)*(w/h) + 0.5`
//        `uv.y = _UVScale.y*(screenUV.y - _ViewportPoint.y) + 0.5`
//      ⇒ 贴图恒占 **(屏高/1.48) × (屏高/2.0)** 像素（1080p = **730×540**）、**与锚点元素多大无关**。
//   ④ **位置**：`AiScripted__EnableTutorialHighlight.c` → `ScreenHighlightPosition__HighlightPosition.c`
//      （`Camera.WorldToViewportPoint(世界点)` → `Material.SetVector(shaderViewportID, v)`）。
//      `ScreenHighlightPosition` 自带 `fadeTime = 0.1`（`__.ctor.c` 的 `0x3dcccccd`）。
//
//  ⇒ 我们落成：世界高 = `LayoutSpace.DesignHeight / 2.0` = **5.0**、世界宽 = `DesignHeight / 1.48` = **6.7568**
//    （可见高恒 10 世界单位 ⇒ 与 1080p 下的 540 / 730 px 同一件事）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `B117_G10_23_Assert.Run();`
//  宿主里两行（同族先例 `B108_A8_TutorialArrow_Assert` / `B94Y5_Assert`）：
//    `B117_G10_23_Assert.Run(); pass += B117_G10_23_Assert.Pass; fail += B117_G10_23_Assert.Fail;`
//  · 世界档建件（`TutorialOverlay.Create(root.transform)` 传 `uguiHost: null`）—— 生产就是这么建的
//    （`BattleDriver.cs:16967`）。⚠️ 世界档下 `UseHost(null)` 会**故意** `Debug.LogError` 一句
//    （工程红线：不许静默失败）—— 那是**预期日志**，不计入失败（失败只按 `CheckSink` 的 `✗` 数）。
//  · 临时根在 `finally` 里 `DestroyImmediate`；不建持久资产、不切场景、不改静态状态。
//
// ============================ 🔴 灭自证（每条都写清「改哪一处会红」）============================
//  · C1：把尺寸改回「锚点元素尺寸 + pad」⇒ 两次不同 `size` 的测量值**不再相等** ⇒ 红。
//  · C2：把宽高比改回「按贴图比例（1:1）」⇒ `w/h = 1.0` ⇒ 红（期望 1.3514）。
//  · C3：把 `h` 改回 `size.y + 0.12` ⇒ 恰好吃到 `size.y + 0.12` 那一格 ⇒ 红。
//  · D1：把 `HighlightPad` **写回活代码**（哪怕改了名字但公式回到元素尺寸）⇒ D1 红；
//        D2 盯「常量 `HlUvScaleX/HlUvScaleY` 还在」—— 两者一起断，防「删了旧公式但没接新公式」。
//  · E1/E2：染色改回 0.85 / 只改 alpha 不改 rgb ⇒ 红。
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B117-G10-23`：把「教程高亮 = 固定屏幕尺寸的发光贴图」这条判据钉死。
/// <para>⛔ 期望值全是**常量/字面量**，⛔ 不从被测实现里读回来（否则就是自证）。</para></summary>
public static class B117_G10_23_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B117G1023] ") { Near = MenuNearStyle.Labeled4 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void Near(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 期望值：原版材质两个字面量（`Material_509935442048338405.json`）----
    const float UvScaleX = 1.48f, UvScaleY = 2.0f;
    /// <summary>原版 `Image.m_Color.a`（`MonoBehaviour_5145.json` 的 `0.8235294222831726`）。</summary>
    const float OriginalTintAlpha = 210f / 255f;

    const string SrcOverlay = "Assets/CardPresentation/Battle/TutorialOverlay.cs";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink,
            "B117-G10-23 · 教程高亮 = 全屏 Image + `Everguild/FX/Tutorial Highlight`（贴图恒占 (屏高/1.48)×(屏高/2.0)，与锚点尺寸无关）");
        var root = new GameObject("B117G1023_Root");
        try { RunAll(root.transform); }
        finally { UnityEngine.Object.DestroyImmediate(root); }
        Debug.Log(_sink.P + "B117_G10_23 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    static void RunAll(Transform parent)
    {
        var ov = TutorialOverlay.Create(parent);      // 世界档（= `BattleDriver.cs:16967` 的生产形状）
        SectionA_Precondition(ov);
        SectionB_Size(ov);
        SectionC_AntiSelfProof(ov);
        SectionD_SourceGuard();
        SectionE_Tint(ov);
    }

    // ============================================================ A · 前提（先断前提，再断结论）
    static void SectionA_Precondition(TutorialOverlay ov)
    {
        MenuCheck.Section(_sink, "A · 前提：高亮层真的建出来了（读口 -1 = 没建 ⇒ 后面全是空转绿）");
        // 🔴 读口按约定「没建出来 ⇒ -1」⇒ 这里必须同时断 > 0（0 也是可疑值：世界高不可能是 0）
        Tr(ov.HighlightWorldHForTest > 0f,
           "A1 前提：`HighlightWorldHForTest > 0`（建出来了才谈尺寸）");
        Tr(ov.HighlightWorldWForTest > 0f,
           "A2 前提：`HighlightWorldWForTest > 0`");
        ov.SetHighlight(true, Vector3.zero, new Vector2(0.4f, 0.4f));
        Tr(ov.HighlightVisible, "A3 前提：`SetHighlight(true, …)` 之后那一层 activeSelf = true");
        ov.SetHighlight(false, Vector3.zero, new Vector2(0.4f, 0.4f));
        Tr(!ov.HighlightVisible, "A4 开/关两态都能切换（关掉之后 activeSelf = false）");
        ov.SetHighlight(true, Vector3.zero, new Vector2(0.4f, 0.4f));   // 复原成「亮着」，给 B/C 读尺寸
    }

    // ============================================================ B · 尺寸 = 原版那两个系数
    static void SectionB_Size(TutorialOverlay ov)
    {
        MenuCheck.Section(_sink, "B · 尺寸：世界高 = `DesignHeight/2.0`（5.0）· 世界宽 = `DesignHeight/1.48`（6.7568）");

        // 可见高恒 = `LayoutSpace.DesignHeight`（10 世界单位）⇒ 屏高的 1/2.0 与 1/1.48 就是下面两个数
        float wantH = LayoutSpace.DesignHeight / UvScaleY;     // 5.0
        float wantW = LayoutSpace.DesignHeight / UvScaleX;     // 6.756756…
        Near(ov.HighlightWorldHForTest, wantH, 1e-3f,
             "B1 世界高 = 可见高/2.0（原版 `_UVScale.y = 2.0`；1080p = 540 px）");
        Near(ov.HighlightWorldWForTest, wantW, 1e-3f,
             "B2 世界宽 = 可见高/1.48（原版 `_UVScale.x = 1.48`；1080p = 730 px）");
        // 宽高比 = 2.0/1.48 = 1.35135 —— 这是「不是按贴图 1:1」的判别式
        Near(ov.HighlightWorldWForTest / ov.HighlightWorldHForTest, UvScaleY / UvScaleX, 1e-4f,
             "B3 宽高比 = `_UVScale.y/_UVScale.x` = 1.3514（⛔ 不是按贴图比例的 1.0）");
    }

    // ============================================================ C · 灭自证：尺寸与锚点无关
    static void SectionC_AntiSelfProof(TutorialOverlay ov)
    {
        MenuCheck.Section(_sink, "C · 灭自证：尺寸**与锚点元素多大无关**（旧写法 `size + HighlightPad` 会在这三条里全红）");

        float h1, w1, h2, w2;
        ov.SetHighlight(true, Vector3.zero, new Vector2(0.4f, 0.4f));
        h1 = ov.HighlightWorldHForTest; w1 = ov.HighlightWorldWForTest;
        ov.SetHighlight(true, Vector3.zero, new Vector2(4.0f, 3.0f));
        h2 = ov.HighlightWorldHForTest; w2 = ov.HighlightWorldWForTest;

        // C1 ★ 结构性：换一个锚点尺寸，量出来的**逐位相同**。旧写法（元素尺寸 + pad）必然不同 ⇒ 红。
        Tr(h1 == h2 && w1 == w2,
           $"C1 ★ 换锚点尺寸（0.4×0.4 → 4.0×3.0）后高亮**逐位不变**（{w1:F4}×{h1:F4} vs {w2:F4}×{h2:F4}）"
           + " —— 旧写法 `size + HighlightPad` 会在这里红");

        // C2 ★ 结构性：旧写法 h = size.y + 0.12 ⇒ 恰好吃到那一格（这里 size.y = 3.0 ⇒ 3.12）
        Tr(Mathf.Abs(h2 - (3.0f + 0.12f)) > 1e-3f,
           $"C2 ★ 世界高 ≠ `锚点高 + 2×0.06`（旧 `HighlightPad` 公式给 3.12，实测 {h2:F4}）");

        // C3 ★ 结构性：旧写法 w ≈ 锚点宽 + 0.12（≈ 4.12）；原版恒 6.7568
        Tr(w2 > 4.0f * 1.5f,
           $"C3 ★ 世界宽远大于锚点宽（4.0 → 实测 {w2:F4}；旧写法只会给 ≈4.12）");

        // C4 前提回填：把尺寸改回小锚点，确认 C1 不是「两次都读到缓存的同一个值」而空转
        ov.SetHighlight(true, Vector3.zero, new Vector2(0.4f, 0.4f));
        Near(ov.HighlightWorldWForTest, w2, 1e-3f,
             "C4 再切回小锚点，宽仍是同一个 6.7568（⇒ C1 不是缓存）");
    }

    // ============================================================ D · 源码闸：旧公式不许回来
    static void SectionD_SourceGuard()
    {
        MenuCheck.Section(_sink, "D · 源码闸：`HighlightPad` 不许在**活代码**里，`HlUvScaleX/Y` 必须在");

        string src = null;
        try { if (File.Exists(SrcOverlay)) src = File.ReadAllText(SrcOverlay); }
        catch (Exception e) { Debug.LogWarning("[B117G1023] 读不到源码（" + e.GetType().Name + "）：" + SrcOverlay); }

        Tr(src != null, "D0 前提：源码读得到（读不到 ⇒ D1/D2 是空转，⛔ 不许当绿）");
        if (src == null) return;

        // 剥掉行注释再查（历史留痕里那些词还在注释里 —— ⛔ 那不是活代码）
        var live = StripLineComments(src);

        Tr(live.IndexOf("HighlightPad", StringComparison.Ordinal) < 0,
           "D1 ★ `HighlightPad`（旧的自设计边距）**不在活代码里**（把旧公式写回去 ⇒ 红）");
        Tr(live.IndexOf("HlUvScaleX", StringComparison.Ordinal) >= 0
           && live.IndexOf("HlUvScaleY", StringComparison.Ordinal) >= 0,
           "D2 ★ 新公式的两个系数 `HlUvScaleX/HlUvScaleY` 在活代码里（删了没接新的 ⇒ 红）");
    }

    /// <summary>逐行剥掉 `//` 之后的内容（**够用就行**：本文件里没有含 `//` 的字符串字面量）。</summary>
    static string StripLineComments(string s)
    {
        var sb = new System.Text.StringBuilder(s.Length);
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/')
            {
                while (i < s.Length && s[i] != '\n') i++;
            }
            else sb.Append(s[i++]);
        }
        return sb.ToString();
    }

    // ============================================================ E · 染色（原版 `Image.m_Color`）
    static void SectionE_Tint(TutorialOverlay ov)
    {
        MenuCheck.Section(_sink, "E · 染色 = 原版 `Image.m_Color`（白，α 210/255 = 0.8235）");
        var c = ov.HighlightTintForTest;
        Near(c.a, OriginalTintAlpha, 0.002f,
             "E1 α = 210/255（`MonoBehaviour_5145.json` 的 `0.8235294`；我们原来挑的是 0.85）");
        Tr(Mathf.Approximately(c.r, 1f) && Mathf.Approximately(c.g, 1f) && Mathf.Approximately(c.b, 1f),
           $"E2 rgb 全 = 1（实测 {c.r:F3}/{c.g:F3}/{c.b:F3}；只改 α 不改 rgb 才与原版同形）");
    }
}
