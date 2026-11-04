// B141_W25_Assert.cs — 执行写手 `B141-W25`（第一百二十八会话 · 波次 `W129` · 条目 `4-23`）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` 的 **W11 包**（条目 `4-23`）·
//         审计 = `资料/完全复刻/报告/B138_审计汇总_问题清单.md` §4 `4-23`「`/Text Area` 族 38 处未按 pad 裁」。
//
//  ── 原版判据（prefab YAML 现读）──
//    `d:/2/tools/assetripper_out/ExportedProject/Assets/GenericPromptWindow.prefab:1337`
//      `RectMask2D`（`m_Script: {fileID: -146154839, guid: d3e719b59ab71ba3f6b398058c866280}`）
//      挂在 `GenericPromptWindow/Window/inputs/template/Text Area` 上：
//        · `m_Padding: {x: -8, y: -5, z: -8, w: -5}`   ← (Left, Bottom, Right, Top)，**负 = 往外扩**
//        · `m_Softness: {x: 0, y: 0}`                  ← 硬边
//    全库扫描（`d:/4/Unity/资料/完全复刻/报告/B141_W129_TextInputPad.md` §1）：
//      **19 个 prefab / 38 处** `m_Padding = (-8,-5,-8,-5)`，**38/38 都挂在名为 `Text Area` 的节点上**、
//      **38/38 的 `m_Softness` 都是 `(0,0)`** ⇒ 这一族参数是同一个值，不是「多套配置」（铁律 5·c 已核）。
//
//  ── 我们（修前 · 修后）──
//    修前：`Shell/TextInputField.cs:157` 只有 `MenuDraw.Node(go, "Text Area", area)` —— **只建节点、没有掩码**
//          ⇒ 打进字去会溢出输入框（`Label` 没有裁切，原版 `RectMask2D` 会按像素裁）。
//    修后：节点改走 `ViewportClip.Hang(go, AreaNodeName, area, AreaPad, AreaSoftness)`（= 原版那颗 `RectMask2D`
//          的等价物，框 = 本节点自己的 rect）＋ 两颗 `Label` 各调一次 `MenuDraw.ClipText(..., null, ...)`
//          （`null` ⇒ 由父链上的 `ViewportClip` 解析；`ClippedTextGuard` 每次重排重新解析）。
//
// ============================ 灭自证（🧨 每条怎么才能红）============================
//  · A2–A5 **逐分量拿字面量** (-8,-5,-8,-5) 比（⛔ 不拿 `TextInputField.AreaPad` 比自己 ——
//    那样把常量与断言一起改回 `Vector4.zero` 就能蒙过）。把 pad 改成 0 ⇒ 四条全红。
//  · A8 拿**字面量** `(102,101.5)-(998,188.5)` 比 `State.RenderClip`（= 880×77 框按负 pad 外扩后的那一份）
//    ⇒ 改回不裁（`MenuDraw.Node`）时 `RenderClip == null` ⇒ 红；把 pad 改成正/零 ⇒ 红。
//  · A9 结构性判别式：`RenderClip` 的 W/H **必须比** `BaseRect` 大 **16 / 10** —— 这条与 A2–A5 **不共因**
//    （只看「有没有 ViewportClip」拦不住「挂上了但 pad 是 0」）。
//  · A10/A11 断两颗 `Label` 身上有 `ClippedTextGuard` —— 那是 `MenuDraw.ClipText` 唯一挂点，
//    且它只在 `_st.RenderClip.HasValue` 为真时才挂 ⇒ 把 `ClipText` 那句删掉、或让父链上没有节点 ⇒ 红。
//  · B1–B6 扫**源码非注释部分**（`grep` 口径同 `B141_W6_Assert.CountOfCode`）：
//    `ViewportClip.Hang(go, AreaNodeName` 恰 1 处 · `MenuDraw.Node(go, "Text Area"` 恰 **0** 处 ·
//    两句 `ClipText` 各恰 1 · pad 字面量恰 1 · 节点名常量恰 1。
//    ⇒ **退回不裁**（把 Hang 改回 Node）⇒ B1/B2 红；**删掉裁切**（删 ClipText）⇒ B3/B4 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/MainMenuScene.cs`（本窗的自检宿主；同族 `B102ALLY_Assert` 接在
//  `MainMenuScene.cs:18538` 那一行，`TextInputField.Create` 的生产站点也都在 SocialWindow 那条链上）。
//  在它的**合计行之前、同一物理行**（该行第一个 `//` **之前**）追加：
//      B141W25_Assert.Run(); _sink.Pass += B141W25_Assert.Pass; _sink.Fail += B141W25_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在该行第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）
//  ⚠️ 本断言**自建一个临时根节点 + 一格输入框**（走生产入口 `TextInputField.Create`）后当场销毁；
//     **零前提**（不需要场景、不写盘、不碰玩家存档）。它会多记若干次 `ViewportClip.NodeResolutions++`
//     （解析走节点态）—— 全仓没有「`NodeResolutions` 绝对值 == 0」的断言依赖它（那几条都是差分），
//     与 `B141_W6_Assert` 对 `LiveDerivations` 的处理同一条纪律。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_TextInputPad.md`
//  · 原版：`GenericPromptWindow.prefab:1337`（`m_Padding` / `m_Softness`）＋ 19 个 prefab 的 38 处扫描

using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141-W25`：`4-23`「`/Text Area` 族按 `m_Padding = (−8,−5,−8,−5)` 建视口裁切」的回归断言。</summary>
public static class B141W25_Assert
{
    const string Tag = "B141W25";
    static int _pass, _fail;

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _fail; } }

    static void C(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log(Tag + " ✓ " + msg); }
        else { _fail++; Debug.LogError(Tag + " ✗ " + msg); }
    }

    static void N(float got, float want, float tol, string msg) { C(Mathf.Abs(got - want) <= tol, msg + "（实得 " + got.ToString("F4") + "）"); }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _pass = 0; _fail = 0;
        A_PadOnBuiltField();
        B_SourceStructure();
        Debug.Log(Tag + " 合计：✓ " + _pass + " · ✗ " + _fail);
    }

    // ==================================================================
    //  A · 建一格输入框 ⇒ `Text Area` 上真有那颗等价物、参数逐值对
    // ==================================================================
    //  夹具矩形：整行 = (100,100)-(1000,190)（900 × 90，= 原版 `inputs/template`）
    //    ⇒ `Text Area` = 四边内缩 10 / 6.5 = (110, 106.5)-(990, 183.5)（880 × 77）
    //    ⇒ 按 pad (-8,-5,-8,-5) **外扩**：PaddedRect = (x1−8, y1−5)-(x2+8, y2+5)
    //         = (102, 101.5)-(998, 188.5)（896 × 87 = 880+16 × 77+10）
    const float RowL = 100f, RowT = 100f, RowR = 1000f, RowB = 190f;
    const float AreaL = 110f, AreaT = 106.5f, AreaR = 990f, AreaB = 183.5f;
    const float ClipL = 102f, ClipT = 101.5f, ClipR = 998f, ClipB = 188.5f;

    static void A_PadOnBuiltField()
    {
        GameObject root = null;
        try
        {
            root = new GameObject("B141W25Fixture", typeof(RectTransform));
            var data = new PromptContext.InputData("B141W25", "Probe");
            var f = TextInputField.Create(root.transform, new PxRect(RowL, RowT, RowR, RowB), data);

            // ---- 前提（硬；⛔ 不许写成 `if (f != null) C(...)` —— 那会静默跳过）----
            C(f != null, "A0（前提）`TextInputField.Create` 在临时根节点上建得出输入框（仓库自带 TMP 资产可用）");
            if (f == null) return;
            C(f.TextLabel != null, "A0（前提）正文 `Label` 建出来了");
            C(f.PlaceholderLabel != null, "A0（前提）占位 `Label` 建出来了");

            var tmpl = root.transform.Find("template");
            C(tmpl != null, "A0（前提）`template` 节点建出来了（`TextInputField.Create` 的第一层）");
            if (tmpl == null) return;
            var area = tmpl.Find(TextInputField.AreaNodeName);
            C(area != null, "A1（前提）`Text Area` 节点建出来了（名字 = `TextInputField.AreaNodeName`）");
            if (area == null) return;

            var vc = area.GetComponent<ViewportClip>();
            C(vc != null, "A1 ★ `Text Area` 上挂着 `ViewportClip`（= 原版那颗 `RectMask2D` 的等价物）"
                        + "｜🧨 退回 `MenuDraw.Node`（只建节点）⇒ 红");

            // ---- ★ pad 逐分量 = 原版字面量 ----
            if (vc == null) return;
            N(vc.padding.x, -8f, 0.0001f, "A2 ★ `m_Padding.x`（Left）= **−8**（负 = 往外扩）｜🧨 改成 0 ⇒ 红");
            N(vc.padding.y, -5f, 0.0001f, "A3 ★ `m_Padding.y`（Bottom）= **−5**｜🧨 改成 0 ⇒ 红");
            N(vc.padding.z, -8f, 0.0001f, "A4 ★ `m_Padding.z`（Right）= **−8**｜🧨 改成 0 ⇒ 红");
            N(vc.padding.w, -5f, 0.0001f, "A5 ★ `m_Padding.w`（Top）= **−5**｜🧨 改成 0 ⇒ 红");

            // ---- ★ softness = 原版 (0,0) ----
            C(vc.softness.x == 0 && vc.softness.y == 0,
              "A6 ★ `m_Softness` = **(0,0)**（原版 38/38 都是硬边）｜实得 (" + vc.softness.x + "," + vc.softness.y + ")");

            // ---- ★ 框 = 本节点自己的设计矩形；渲染框 = 按 pad 外扩 ----
            C(vc.HasBaseRect, "A7 ★ 这个视口记下了设计矩形（`HasBaseRect` = true ⇒ 框与被比矩形同一帧）");
            if (!vc.HasBaseRect) return;
            var b = vc.BaseRect;
            N(b.x1, AreaL, 0.01f, "A7a ★ `BaseRect.x1` = **110**（= 整行左 + 10 内缩）");
            N(b.y1, AreaT, 0.01f, "A7b ★ `BaseRect.y1` = **106.5**（= 整行上 + 6.5 内缩）");
            N(b.x2, AreaR, 0.01f, "A7c ★ `BaseRect.x2` = **990**");
            N(b.y2, AreaB, 0.01f, "A7d ★ `BaseRect.y2` = **183.5**");

            var st = vc.State;
            C(st.Clip.HasValue, "A8（前提）`State.Clip` 非空（节点给得出框）");
            var rc = st.RenderClip;
            C(rc.HasValue, "A8 ★ `State.RenderClip` 非空（= 这颗掩码真的会裁；没有节点时它是 null）"
                         + "｜🧨 退回 `MenuDraw.Node` ⇒ 红");
            if (rc.HasValue)
            {
                var c = rc.Value;
                N(c.x1, ClipL, 0.01f, "A8a ★ 渲染框左沿 = **102**（= 110 − 8，负 pad 往外扩）｜🧨 pad 改 0 ⇒ 110 ⇒ 红");
                N(c.y1, ClipT, 0.01f, "A8b ★ 渲染框上沿 = **101.5**（= 106.5 − 5）｜🧨 pad 改 0 ⇒ 106.5 ⇒ 红");
                N(c.x2, ClipR, 0.01f, "A8c ★ 渲染框右沿 = **998**（= 990 + 8）｜🧨 pad 改 0 ⇒ 990 ⇒ 红");
                N(c.y2, ClipB, 0.01f, "A8d ★ 渲染框下沿 = **188.5**（= 183.5 + 5）｜🧨 pad 改 0 ⇒ 183.5 ⇒ 红");

                // 结构性判别式（与 A2–A5 不共因）：外扩量 = 左+右 16、上+下 10
                N(c.W - b.W, 16f, 0.01f, "A9 ★ 渲染框宽 − 节点宽 = **16**（左右各 8 —— 负 pad 的必然结果）");
                N(c.H - b.H, 10f, 0.01f, "A9 ★ 渲染框高 − 节点高 = **10**（上下各 5）");
            }

            // ---- ★ 两颗 Label 都真的被 `MenuDraw.ClipText` 接手过 ----
            C(f.PlaceholderLabel != null && f.PlaceholderLabel.GetComponent<ClippedTextGuard>() != null,
              "A10 ★ `Placeholder` 身上挂着 `ClippedTextGuard`（= `MenuDraw.ClipText` 只在解析出非空框时才挂）"
            + "｜🧨 删掉 `MenuDraw.ClipText(f._ph, …)` ⇒ 红");
            C(f.TextLabel != null && f.TextLabel.GetComponent<ClippedTextGuard>() != null,
              "A11 ★ `Text` 身上挂着 `ClippedTextGuard`"
            + "｜🧨 删掉 `MenuDraw.ClipText(f._text, …)` ⇒ 红");
        }
        catch (Exception e)
        {
            C(false, "A 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    // ==================================================================
    //  B · 源码结构（★ 灭自证：退回不裁 / 删掉裁切都会红）
    // ==================================================================
    static void B_SourceStructure()
    {
        const string rel = "CardPresentation/Shell/TextInputField.cs";
        string src = null;
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p))
            {
                C(false, "B0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红");
                return;
            }
            src = File.ReadAllText(p);
        }
        catch (Exception e)
        {
            C(false, "B0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return;
        }

        C(CountOfCode(src, "ViewportClip.Hang(go, AreaNodeName") == 1,
          "B1 ★ `Text Area` 节点走 `ViewportClip.Hang`（恰 1 处）"
        + "｜🧨 改回 `MenuDraw.Node(go, \"Text Area\", area)` ⇒ 0 处 ⇒ 红（实得 "
        + CountOfCode(src, "ViewportClip.Hang(go, AreaNodeName") + "）");
        C(CountOfCode(src, "MenuDraw.Node(go, \"Text Area\"") == 0,
          "B2 ★ 旧的「只建节点」写法已消失（`MenuDraw.Node(go, \"Text Area\"` 0 处）"
        + "｜🧨 改回去 ⇒ 1 处 ⇒ 红（实得 " + CountOfCode(src, "MenuDraw.Node(go, \"Text Area\"") + "）");
        C(CountOfCode(src, "MenuDraw.ClipText(f._ph, null, Vector2.zero)") == 1,
          "B3 ★ `Placeholder` 的裁切入口恰 1 处｜🧨 删掉 ⇒ 红");
        C(CountOfCode(src, "MenuDraw.ClipText(f._text, null, Vector2.zero)") == 1,
          "B4 ★ `Text` 的裁切入口恰 1 处｜🧨 删掉 ⇒ 红");
        C(CountOfCode(src, "new Vector4(-8f, -5f, -8f, -5f)") == 1,
          "B5 ★ 原版 pad 的四个分量在代码里**只写一处**（`AreaPad` 常量声明）"
        + "｜🧨 改成 `Vector4.zero` ⇒ 0 处 ⇒ 红");
        C(CountOfCode(src, "public const string AreaNodeName = \"Text Area\"") == 1,
          "B6 ★ 节点名常量恰 1 处（= 原版 prefab 里那 38 处的同一个名字）");
    }

    // ------------------------------------------------------------ 工具
    /// <summary>数**非注释行**里的子串出现次数（整行 `//` 开头的跳过 —— 文档/注释里也会出现这些字面量）。
    /// 口径逐字照 `B141_W6_Assert.CountOfCode`（同一个工程、同一条纪律，⛔ 别另写一套）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return -1;
        int n = 0;
        foreach (var raw in src.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;
            int at = 0;
            while ((at = line.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        }
        return n;
    }
}
