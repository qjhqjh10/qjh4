// B117_WLABEL2_Assert.cs — 执行代理 `B117-WLABEL2`（2026-11-02）· 账 `B117-29`
//   主题：**uGUI 标签「左对齐 + 量不到尺寸（空文本 / 未激活）」时整块从框左沿外半个框宽起笔**
//
// ============================ 它盯的是什么 ============================
//  ① 根因（判据链见 `Battle/Label.AlignTmpPivotToAnchor` 的方法头 + `Battle/UnitChatPanel.cs` 的 pivot 注）：
//     `TmpFont.NewTextUgui` 给 TMP 子节点的枢轴是出厂 `(0.5, 0.5)`；`Label.RefreshBounds` 只在**量到**
//     `textBounds` 时才把子节点摆到 `-anchor.x*W - b.min.x`，空文本 / 未激活（`GetTextBounds` 只统计
//     `isVisible` 的字 ⇒ 空集哨兵 ±4.29e9）时它早退 ⇒ 子节点留在 `(0,0)`、矩形按「枢轴 + 对齐」几何排
//     ⇒ 左锚（`anchor.x = 0`）的标签整块**从框左沿外半个框宽**起笔。
//  ② 修法 = 新入口 `Label.AlignTmpPivotToAnchor()`：把 **TMP 子节点**的枢轴钉成 `Label.anchor`，
//     并把 TMP 的横向对齐落到同侧（左锚 ⇒ `SetAlignLeft()`）。
//  ③ 接线面（只 7 处）：`Battle/SettingsPanel.cs` ×6 + `Shell/AlliancePanelWindow.cs` 的 `Txt()` ×1。
//     那 3 处「先建在 0、后由调用方摆位」的站点（`ChatPopupPanel:471` / `UnitChatPanel:706` / `BattleLogPanel:923`）
//     **一律不接** —— 接了会把它们的既有摆位挪走。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · `A4`（改造前的左缘 = **−框宽/2**）是**两态判别式**：把入口改成空实现 / 删掉接线 ⇒ `A4` 绿而 `A6` 红
//    （实得 −框宽/2、期望 0）。**只断「改造后 = 0」拿不出鉴别力**（谁把框宽写成 0 也能过）。
//  · `A5`（枢轴 = anchor）与 `A6`（左缘 = 0）**必须分开断**：只改枢轴、不改对齐 ⇒ 文字回到矩形内居中，
//    `A6` 会偏半个**字**宽；只改对齐、不改枢轴 ⇒ 矩形仍在框心，`A6` 偏半个**框**宽。
//  · `B2`（有文本时落点不变）是**回归闸**：把 `RefreshBounds` 的 `-b.min.x` 删掉 ⇒ `B1/B2` 同时红。
//  · `C1..C3`（居中锚零变化）保护既有调用点：把入口改成「无条件 SetAlignLeft」⇒ `C3` 红。
//  · `D1..D5`（接线面计数）：把入口接到那 3 个「先建后摆」站点 ⇒ `D3/D4/D5` 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  接进任一宿主的聚合行（**同一物理行**、放在该行 `//` **之前**）：
//      B117WLABEL2_Assert.Run(); _sink.Pass += B117WLABEL2_Assert.Pass; _sink.Fail += B117WLABEL2_Assert.Fail;
//  ⚠️ 本件会建一个临时 `Canvas` + 若干 `Label`，跑完 `DestroyImmediate` 掉；**不需要场景、不碰存档**。
//  ⚠️ 依赖 `TmpFont.Available`（字体资产）；取不到时 `Label.CreateUgui` 返回 null ⇒ `A0` 硬前提先红，
//     **不会**静默跳过（`CLAUDE.md` §三：`if (前提) Check(结论)` 那种写法一律改成两条断言）。
//
// ============================ 已知边界 ============================
//  · `D*` 是**源码级**接线检查（数 `.AlignTmpPivotToAnchor(` 的调用点数），不是行为断言 —— 它只能证明
//    「没接到那 3 处」，证明不了「那 3 处的摆位真的没变」（那要跑各自宿主看坐标）。
//  · 点阵后端（`_tmp == null`）那一支**没建夹具**：它只出声、不摆位（`NoteDotBackendLacks`），
//    对它的断言会落进 `SettingsScene` 的 A491 那一族，本件不重复。
//
// ============================ 判据 / 报告 ============================
//  · 判据：`资料/完全复刻/判据/B117-WLABEL2_Label左对齐.md`
//  · 先例：`Battle/UnitChatPanel.cs:711`（2026-11-02 手写修法：TMP 子节点 pivat = (0,0.5)）
//           + `:1097`（`SetAlignLeft()` 挪到 `SetText()` 之前）
using System;
using System.IO;
using CardPresentation;
using TMPro;
using UnityEngine;

/// <summary>`B117-29` 的断言集：TMP 子节点枢轴 = `Label.anchor` + 横向对齐同侧。</summary>
public static class B117WLABEL2_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B117WLABEL2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ============================================================ 夹具

    /// <summary>画布 + 一件 uGUI 标签（**每次跑完就销毁**，不污染同一宿主后面的断言）。</summary>
    sealed class Fixture : IDisposable
    {
        public readonly GameObject Canvas;
        public readonly Label Lb;
        public readonly TMP_Text Tmp;

        public Fixture(Vector2 anchor, string text)
        {
            Canvas = new GameObject("b117wlabel2_canvas", typeof(RectTransform), typeof(Canvas));
            var cv = Canvas.GetComponent<Canvas>();
            if (cv != null) cv.renderMode = RenderMode.ScreenSpaceOverlay;
            Lb = Label.CreateUgui(Canvas.transform, text ?? "", Vector3.zero, 4, Color.white, anchor,
                                     "b117wlabel2_lb");
            Tmp = Lb != null ? Lb.GetComponentInChildren<TMP_Text>(true) : null;
        }

        /// <summary>TMP 子节点（没有 = null）。</summary>
        public RectTransform Rect { get { return Tmp != null ? Tmp.rectTransform : null; } }

        /// <summary>那**一块矩形**的宽（设计 px）—— 量不到时 −1（⛔ 别拿 0 冒充）。</summary>
        public float FrameW { get { return Rect != null ? Rect.rect.width : -1f; } }

        /// <summary>矩形的**左沿**在 `Label` 节点坐标系里的 x。
        /// 算法只用一个恒等式：`RectTransform.rect.xMin == -pivot.x * width`，而 `localPosition` 就是
        /// 枢轴点在父坐标系里的位置 ⇒ **与锚点/拉伸方式无关**（⛔ 别改成「按 sizeDelta 猜」）。</summary>
        public float LeftEdge
        {
            get { return Rect != null ? Rect.localPosition.x + Rect.rect.xMin : float.NaN; }
        }

        /// <summary>文字**字墨左沿**在 `Label` 节点坐标系里的 x（`textBounds` 是相对枢轴的 ⇒ 先转世界再转回来）。</summary>
        public float TextLeftInLabel()
        {
            if (Tmp == null) return float.NaN;
            var b = Tmp.textBounds;
            Vector3 world = Tmp.rectTransform.TransformPoint(new Vector3(b.min.x, b.center.y, 0f));
            return Lb.transform.InverseTransformPoint(world).x;
        }

        public void Dispose()
        {
            if (Canvas != null) UnityEngine.Object.DestroyImmediate(Canvas);
        }
    }

    // ============================================================ 入口

    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0; _sink.Failures.Clear();

        MenuCheck.Section(_sink, "B117-29 uGUI 标签左对齐 + 量不到尺寸（TMP 子节点枢轴 / 左沿）");

        SectionA_EmptyText();
        SectionB_MeasuredText();
        SectionC_CenterAnchor();
        SectionD_CallSites();

        Debug.Log(_sink.P + "B117WLABEL2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A 空文本（量不到尺寸）

    static void SectionA_EmptyText()
    {
        MenuCheck.Section(_sink, "A 空文本 + 左锚（`anchor=(0,0.5)`）：改造前左缘 = −框宽/2，改造后 = 0");

        var f = new Fixture(new Vector2(0f, 0.5f), "");
        try
        {
            // A0 硬前提：字体资产在、TMP 建起来了（否则下面全部等于没查）
            Tr(f.Lb != null && f.Tmp != null,
               "A0 ★ 前提：`Label.CreateUgui` 建起来了（`TmpFont.Available` 为真）—— 取不到 TMP 时本节的每一条都无从谈起");

            if (f.Lb == null || f.Tmp == null) return;

            // 框：200 × 40（设计 px）。`SetWrapWidth` 会把 sizeDelta.x 写成 200 ⇒ 矩形的宽 = 200。
            f.Lb.SetAutoFitBox(200f, 40f, 18f, 42f, 36f);

            // A1 出厂枢轴居中（改造前的起点）
            N(f.Rect.pivot.x, 0.5f, 1e-6f, "A1 前提：TMP 子节点出厂枢轴 x = 0.5（`TmpFont.NewTextUgui` 没有设过它）");
            N(f.Rect.pivot.y, 0.5f, 1e-6f, "A1b 同上 y = 0.5");

            // A2 框宽真的写进去了
            N(f.FrameW, 200f, 0.5f,
              "A2 ★ 前提：矩形宽 = 200（`SetAutoFitBox(200,40,…)` ⇒ `SetWrapWidth` 写 `sizeDelta.x`）｜"
            + "🧨 框宽没写进去 ⇒ 下面 A4/A6 的「半个框宽」没有意义，先红这里");

            // A3/A4 改造前：量不到尺寸 ⇒ 子节点停在 (0,0)，矩形按枢轴居中 ⇒ 左沿 = −框宽/2
            N(f.Rect.localPosition.x, 0f, 1e-3f,
              "A3 前提：空文本时 `RefreshBounds` 早退 ⇒ TMP 子节点**没被挪过**（localPosition.x = 0）");
            N(f.LeftEdge, -f.FrameW * 0.5f, 0.5f,
              "A4 ★★★ **缺陷本体**：未接入口时矩形左沿 = −框宽/2（= −100）—— 左锚标签整块从框左沿外半个框宽起笔"
            + "｜🧨 这条与 A6 合起来才是两态判别式：把入口写成空实现 ⇒ A4 绿、A6 红");

            // A5/A6/A7 接上入口之后
            f.Lb.AlignTmpPivotToAnchor();
            N(f.Rect.pivot.x, 0f, 1e-6f,
              "A5 ★★ 接上入口 ⇒ TMP 子节点枢轴 x = `anchor.x`（0）｜🧨 入口改成只设对齐不设枢轴 ⇒ A5 红");
            N(f.Rect.pivot.y, 0.5f, 1e-6f, "A5b 枢轴 y = `anchor.y`（0.5，本档不变）");
            N(f.LeftEdge, 0f, 0.5f,
              "A6 ★★★ 矩形左沿 = **框左沿**（0 = `Label` 节点 x）—— 这就是「文字起点 = 框左沿」"
            + "｜🧨 入口改成空实现 / 删掉接线 ⇒ 实得 −100；只改枢轴不改对齐 ⇒ 仍偏半个字宽");
            C(f.Lb.AlignmentForTest, (int)TextAlignmentOptions.Left,
              "A7 ★★ TMP 横向对齐 = `Left`（与枢轴同侧；⛔ 缺了它，量不到尺寸时文字在矩形内居中、仍偏半个字宽）");
        }
        finally { f.Dispose(); }
    }

    // ============================================================ B 有文本（量得到尺寸）→ 落点不变

    static void SectionB_MeasuredText()
    {
        MenuCheck.Section(_sink, "B 有文本 + 左锚：新入口**不改变**量得到尺寸时的落点（回归闸）");

        var oldWay = new Fixture(new Vector2(0f, 0.5f), "Hello");
        var newWay = new Fixture(new Vector2(0f, 0.5f), "Hello");
        try
        {
            // 硬前提（⛔ 不写 `if (前提) Check(结论)` 那种静默跳过 —— `CLAUDE.md` §三）
            Tr(oldWay.Lb != null && newWay.Lb != null,
               "B01 ★ 前提：两件标签都建起来了（字体资产在）");
            if (oldWay.Lb == null || newWay.Lb == null) return;

            oldWay.Lb.SetAutoFitBox(200f, 40f, 18f, 42f, 36f);          // 旧写法：不接入口
            newWay.Lb.SetAlignLeft();
            newWay.Lb.AlignTmpPivotToAnchor();                          // 新写法：先对齐、再钉枢轴
            newWay.Lb.SetAutoFitBox(200f, 40f, 18f, 42f, 36f);

            oldWay.Lb.SetText("Hello");
            newWay.Lb.SetText("Hello");

            float wOld = oldWay.Lb.WorldW, wNew = newWay.Lb.WorldW;
            Tr(wOld > 1f && wOld < 400f && wNew > 1f && wNew < 400f,
               "B0 ★ 前提：两件都**量到了**宽度（old=" + wOld.ToString("F2") + " / new=" + wNew.ToString("F2")
             + "）—— 量不到时 B1/B2 无意义（哨兵是 ±4.29e9）");

            float lOld = oldWay.TextLeftInLabel(), lNew = newWay.TextLeftInLabel();
            N(lOld, 0f, 0.5f,
              "B1 旧写法（枢轴居中）量得到尺寸时，字墨左沿 = `−anchor.x*W` = **0**（`RefreshBounds` 的 `−b.min.x` 自己摆正）");
            N(lNew, 0f, 0.5f,
              "B2 ★★ 新写法同一条落点 = 0（枢轴换档**逐位不变**：`textBounds` 相对枢轴、`−b.min.x` 整项抵消）"
            + "｜🧨 把 `RefreshBounds` 的 `−b.min.x` 删掉 ⇒ B1/B2 同时红");
            N(lNew - lOld, 0f, 0.5f,
              "B3 ★★ 两种写法**落在同一处**（差 = 0）—— 这就是「有文本时行为不变」的证据，⛔ 不是「看起来差不多」");
        }
        finally { oldWay.Dispose(); newWay.Dispose(); }
    }

    // ============================================================ C 居中锚：入口是零变化

    static void SectionC_CenterAnchor()
    {
        MenuCheck.Section(_sink, "C 居中锚（`anchor=(0.5,0.5)`）：入口必须**什么都不改**（保护既有调用点）");

        var f = new Fixture(new Vector2(0.5f, 0.5f), "");
        try
        {
            // 硬前提（⛔ 不写静默跳过）
            Tr(f.Lb != null && f.Tmp != null, "C01 ★ 前提：标签与 TMP 都建起来了（字体资产在）");
            if (f.Lb == null || f.Tmp == null) return;

            C(f.Lb.AlignmentForTest, (int)TextAlignmentOptions.Center,
              "C0 前提：出厂对齐 = `Center`（`TmpFont.NewTextUgui` 写死的那一档）");

            f.Lb.AlignTmpPivotToAnchor();

            N(f.Rect.pivot.x, 0.5f, 1e-6f,
              "C1 ★ 居中锚 ⇒ 枢轴**仍是 0.5**（入口只对左/右锚改枢轴；⛔ 无条件钉成 anchor 会顺手挪动居中站）");
            N(f.Rect.pivot.y, 0.5f, 1e-6f, "C2 同上 y = 0.5");
            C(f.Lb.AlignmentForTest, (int)TextAlignmentOptions.Center,
              "C3 ★★ 居中锚 ⇒ 对齐**仍是 Center**｜🧨 把入口写成「无条件 SetAlignLeft」⇒ C3 红（居中站会被推歪）");
        }
        finally { f.Dispose(); }
    }

    // ============================================================ D 接线面（源码级）

    static void SectionD_CallSites()
    {
        MenuCheck.Section(_sink, "D 接线面：只接 7 处（SettingsPanel ×6 + AlliancePanelWindow ×1），那 3 处「先建后摆」一律 0");

        C(CountCalls("CardPresentation/Battle/SettingsPanel.cs"), 6,
          "D1 `Battle/SettingsPanel.cs` 的 6 处左锚标签（AutoZoom / LangCaption / LangLabel / Item Label / Mute / SliderLabel）都接了");
        C(CountCalls("CardPresentation/Shell/AlliancePanelWindow.cs"), 1,
          "D2 `Shell/AlliancePanelWindow.cs` 的 `Txt()`（画布档）接了 1 处");
        C(CountCalls("CardPresentation/Battle/ChatPopupPanel.cs"), 0,
          "D3 ★ `ChatPopupPanel`（先建在 0、后摆）**一处都没接**｜🧨 接了会把它既有的摆位挪走");
        C(CountCalls("CardPresentation/Battle/UnitChatPanel.cs"), 0,
          "D4 ★ `UnitChatPanel`（同上，它有自己的手写修法）**一处都没接**");
        C(CountCalls("CardPresentation/Battle/BattleLogPanel.cs"), 0,
          "D5 ★ `BattleLogPanel`（同上）**一处都没接**");
    }

    /// <summary>数一个源文件里 `.AlignTmpPivotToAnchor(` 的调用点数。
    /// ⚠️ 认的是**带括号的调用形式** —— 注释里写的是 `Label.AlignTmpPivotToAnchor`（不带括号），不会混进来。
    /// 文件不在 ⇒ −1（⛔ 别拿 0 冒充「没接」，那会让 D3/D4/D5 假绿）。</summary>
    static int CountCalls(string relPath)
    {
        string p = Path.Combine(Application.dataPath, relPath);
        if (!File.Exists(p)) return -1;
        string src = File.ReadAllText(p);
        int n = 0, i = 0;
        const string key = ".AlignTmpPivotToAnchor(";
        while ((i = src.IndexOf(key, i, StringComparison.Ordinal)) >= 0) { n++; i += key.Length; }
        return n;
    }
}
