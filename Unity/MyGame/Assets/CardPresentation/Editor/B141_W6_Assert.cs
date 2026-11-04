// B141_W6_Assert.cs — 第一百二十八会话 · 写手 `B141-W6`（W129 波次 · 两条 `DailyRewardPopup` UI 参数）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` 的 **W7 包**（条目 `4-08` / `4-16`）·
//         审计 = `资料/完全复刻/报告/B138_审计汇总_问题清单.md` §4。
//
//  ── `4-16` 阵营条视口 softness（**本批真改的那条**）──
//    原版 `Daily Reward Popup ▸ Army Selector ▸ Viewport` 上的 `RectMask2D`：
//      `m_Padding = {0,0,0,0}` · `m_Softness = {42,0}`
//      （判据 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Daily Reward Popup.prefab` 实读；
//       同一 prefab 里**另一颗** `Tracks ▸ Rewards Scroll View ▸ Viewport` 是 `(0,0)` —— 别混）。
//    我们（修前）：`Shell/DailyRewardPopup.cs` 的 `BuildArmySelector` 写死 `padding=zero · softness=zero`
//      —— 是从**奖格轨那颗**照抄过来的，原版阵营条这条**左右各有 42px 线性渐隐**。
//    修法：两个字段收口到 `DailyRewardPopup.ApplyArmyViewportClip(GameObject)`（唯一写口），
//      值来自 `DailyRewardPopup.ArmyViewportSoftX/Y` + `ArmyViewportPad`。
//
//  ── `4-08` 进度图 y 区间（**⛔ 判据本身错，本项目未改**）──
//    审计说「原版 `Personal Progression/Image` 中心 406.05 ⇒ y 362.05..450.05，我们 358.76..446.76（上移 3.29）」。
//    **反证**（两条独立源，2026-11-04 现读）：
//      ① `Daily Reward Popup Entry.prefab` / `Daily Reward Popup.prefab` 的 5 个实例：`Personal Progression`
//         `anchoredPosition=(0,0)` · `anchor y 0.5` · `sizeDelta=(0,100)` ⇒ 中心 = Entry(812.10) 正中 = **406.05**；
//         它的子件 `Image` 是 `anchoredPosition = (−0.000024, **+3.2939**)` · `sizeDelta 68×88` · `pivot 0.5`
//         ⇒ 中心 = `406.05 − 3.2939 = 402.7561` ⇒ y = **358.756..446.756**。
//         `Counter`（85×88）`anchoredPosition=(0,0)` 相对 `Image` ⇒ **同中心同 y**。
//      ② 工程自己的尺子 `python 工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup Entry" --depth 5 --relative`
//         实读：`Image` **123.5, 358.8 → 191.5, 446.8** · `Counter` **115.0, 358.8 → 200.0, 446.8**。
//      ⇒ 审计的「pos 0」是错的（把 `Personal Progression` 的中心当成了 `Image` 的中心，漏掉 +3.2939）。
//        我们现读的 `D_ProgMark (123.51, 358.76, 191.51, 446.76)` / `D_ProgCount (115.01, 358.76, 200.01, 446.76)`
//        **与原版逐值吻合（≤0.01 px）** ⇒ 按审计「+3.29」改反而会**制造**一处 3.29px 的偏离。
//      ⛔ 同族判例：CLAUDE.md「判据与 `.c` 冲突时，以 `.c` 为准、宁可拒做」（`B9_W69ENG4`）。
//
// ============================ 🔴 灭自证（每条怎么才能红）============================
//  · A 段（4-08）：A2/A3 是**字面量**期望值（358.76 / 446.76），A5 是**结构性**判别式
//    「`Image` 中心 **不与** `Personal Progression` 同心、差 = 原版的 3.2939」。
//    ⇒ 谁照审计把两个 y 各 +3.29（改成 362.05..450.05）：A2/A3 红（字面量不符）、A5 红（差值变 0）。
//      两条**不共因** —— 只改一处也只改另一处，都拦得住。
//  · B 段（4-16）：B2 拿**字面量 (42,0)** 比（⛔ 不拿 `ArmyViewportSoftX` 比 —— 那样把常量与实现一起改回 0 就能蒙过）；
//    B5 再独立断「x 分量 > 0」。B7–B9 扫**源码非注释部分**：`ApplyArmyViewportClip(vpNode.gameObject)` 恰 1 处、
//    `vc.softness = Vector2Int.zero` 恰 **1** 处（只允许奖格轨那颗）⇒ 把阵营条改回旧值 ⇒ 变 2 处 ⇒ 红。
//  · 全段都不读被测实现自己的「期望值」—— 期望值全是 prefab / `menu_dump` 的实读字面量。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/RewardsScene.cs`（本窗的自检宿主）。在它的聚合行
//  （**同一物理行**、该行第一个 `//` **之前**）追加：
//      B141W6_Assert.Run(); _pass += B141W6_Assert.Pass; _fail += B141W6_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）
//  ⚠️ 本断言**不建窗**：B 段用一个临时 `RectTransform` 节点走生产写口（`ApplyArmyViewportClip`）后当场销毁。
//     ⚠️ 它会多记 **一次** `ViewportClip.LiveDerivations++`（`CaptureNow()` 内部读一次 `ClipPx` 走「没记过 ⇒ 实时反推」那一支）。
//     全仓没有「`LiveDerivations` 绝对值 == 0」的断言依赖它 —— `Editor/ShellScene.cs:6115-6119` 那条是**差分**
//     （自己当场取 `ld0` 再比）⇒ 与本段先后无关。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_DailyReward.md`
//  · 原版：`Daily Reward Popup.prefab`（`m_Padding` / `m_Softness` / `anchoredPosition` / `sizeDelta`）
//          `Daily Reward Popup Entry.prefab`（同上）· `工具/menu_dump.py … "Daily Reward Popup Entry" --depth 5 --relative`
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B141_W6`：`4-16` 阵营条视口 softness `(42,0)` 落地 + `4-08` 判据本身错的反证（值不动）。</summary>
public static class B141W6_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141W6] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B141_W6：每日奖励窗 —— 4-08 进度图 y 区间（⛔判据本身错·值不动） · 4-16 阵营条视口 softness (42,0)");
        A_ProgressImageY();
        B_ArmyViewportClip();
    }

    // ==================================================================
    //  A 段 · 4-08：`Personal Progression/Image`（+ `Counter`）的 y 区间
    // ==================================================================
    //  原版算术（prefab 实读 → y 向下设计 px）：
    //    Entry 根 315.03 × 812.10（`menu_dump … --relative` 表头实读）
    //    Personal Progression：anchor y 0.5 · anchoredPosition (0,0) · sizeDelta (0,100) ⇒ y 356.05..456.05（中心 406.05）
    //    Image：anchoredPosition (−0.000024, +3.2939) · sizeDelta 68×88 · pivot 0.5
    //      ⇒ 中心 = 406.05 − 3.2939 = 402.7561 ⇒ y 358.756..446.756
    //    Counter：同 Image 心 · sizeDelta 85×88 ⇒ y 同
    static void A_ProgressImageY()
    {
        var prog = DailyRewardPopup.D_Progression;
        var mark = DailyRewardPopup.D_ProgMark;
        var count = DailyRewardPopup.D_ProgCount;

        // ---- 前提（硬）----
        N(prog.CY, 406.05f, 0.01f, "A0（前提）`Personal Progression` 中心 = **406.05**（Entry 812.10 竖向居中 · anchor y 0.5）");
        N(prog.H, 100f, 0.01f, "A0（前提）`Personal Progression` 高 = **100**（prefab `sizeDelta.y`）");
        N(mark.H, 88f, 0.01f, "A0（前提）`Image` 高 = **88**（prefab `sizeDelta = 68 × 88`）");
        N(count.H, 88f, 0.01f, "A0（前提）`Counter` 高 = **88**（prefab `sizeDelta = 85 × 88`）");

        // ---- ★ 被测：y 区间 == 原版 ----
        N(mark.y1, 358.76f, 0.01f,
          "A1 ★ `D_ProgMark`（= `Personal Progression/Image`）**上沿 = 358.76**"
        + "（= 406.05 − 3.2939 − 44；`menu_dump` 实读 358.8）"
        + "｜🧨 照 B138 §4-08 改成 362.05（把 Image 当成与 PP 同心）⇒ 红");
        N(mark.y2, 446.76f, 0.01f,
          "A2 ★ `D_ProgMark` **下沿 = 446.76**（`menu_dump` 实读 446.8）"
        + "｜🧨 同上改成 450.05 ⇒ 红");
        N(mark.CY, 402.76f, 0.01f,
          "A3 ★ `D_ProgMark` **中心 = 402.76**（= PP 中心 406.05 − 原版 `anchoredPosition.y` 3.2939）"
        + "｜🧨 改成 406.05（同心）⇒ 红");

        N(count.y1, 358.76f, 0.01f, "A4 ★ `D_ProgCount`（= `Counter`）**上沿 = 358.76**（`menu_dump` 实读 358.8；与 `Image` 同心）");
        N(count.y2, 446.76f, 0.01f, "A4 ★ `D_ProgCount` **下沿 = 446.76**（`menu_dump` 实读 446.8）");

        // ---- ★ 结构性判别式（与上面两条**不共因**）----
        //  原版 Image 相对 PP 中心**有 +3.2939 的 anchoredPosition**（y 向上）⇒ 折算到 y 向下就是「低 3.2939」。
        //  谁把两者做成同心（= 审计的错修法），这条差值会变 0 ⇒ 红。
        float dy = prog.CY - mark.CY;
        N(dy, 3.2939f, 0.01f,
          "A5 ★ 灭自证：`Image` 中心相对 `Personal Progression` 中心**低 3.2939**（原版 `anchoredPosition.y = +3.2939`）"
        + "｜🧨 把 `D_ProgMark` 改到与 PP 同心（中心 406.05）⇒ 本值变 0 ⇒ 红");
        Tr(dy > 1f,
           "A6 ★ 灭自证：上面那个偏移**必须真的存在**（> 1px）—— 挡「D_ProgMark 与 D_Progression 都改了、但改成了同心」");
        N(count.CY, mark.CY, 0.01f, "A7 `Counter` 与 `Image` **同心**（prefab 里 `Counter.anchoredPosition = (0,0)` 相对 `Image`）");

        // ---- 水平方向一个字都不该动（本次只谈 y）----
        N(mark.x1, 123.51f, 0.01f, "A8 `D_ProgMark` 左沿 = 123.51（原版 `menu_dump` 实读 123.5；本次不动 x）");
        N(mark.x2, 191.51f, 0.01f, "A8 `D_ProgMark` 右沿 = 191.51（实读 191.5）");
        N(count.x1, 115.01f, 0.01f, "A9 `D_ProgCount` 左沿 = 115.01（实读 115.0）");
        N(count.x2, 200.01f, 0.01f, "A9 `D_ProgCount` 右沿 = 200.01（实读 200.0）");
    }

    // ==================================================================
    //  B 段 · 4-16：`Army Selector/Viewport` 的 `RectMask2D` 两个字段
    // ==================================================================
    static void B_ArmyViewportClip()
    {
        // ---- 前提：常量 = prefab 实读（⛔ 期望值下面用**字面量**，不拿这两个常量比）----
        C(DailyRewardPopup.ArmyViewportSoftX, 42, "B0（前提）`ArmyViewportSoftX` = 原版 `m_Softness.x` = **42**");
        C(DailyRewardPopup.ArmyViewportSoftY, 0, "B0（前提）`ArmyViewportSoftY` = 原版 `m_Softness.y` = **0**（上下硬边）");
        C(DailyRewardPopup.ArmyViewportPad, Vector4.zero, "B0（前提）`ArmyViewportPad` = 原版 `m_Padding` = **全 0**");

        // ---- ★ 被测：走**生产写口**（`BuildArmySelector` 就这一句），在临时节点上读回 ----
        var go = new GameObject("B141W6_Viewport", typeof(RectTransform));
        try
        {
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(1378.80f, 187.85f);          // = `ArmyViewportRect` 的宽高（real 形状）
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            var vc = DailyRewardPopup.ApplyArmyViewportClip(go);
            Tr(vc != null, "B1（前提）`ApplyArmyViewportClip` 真的给节点挂上了 `ViewportClip`");
            if (vc != null)
            {
                C(vc.softness, new Vector2Int(42, 0),
                  "B2 ★ 阵营条视口 `softness` == 原版 **字面量 (42,0)**"
                + "｜🧨 修前是 `Vector2Int.zero`；把 `ApplyArmyViewportClip` 改回旧值 ⇒ 红（本条**不读** `ArmyViewportSoftX`，"
                + "所以「常量与实现一起改回 0」也拦得住）");
                C(vc.padding, Vector4.zero,
                  "B3 ★ 阵营条视口 `padding` == 原版 **全 0**（同一颗 `RectMask2D` 实读；别把 42 写到 pad 里）");
                N(vc.SoftnessPx.x, 42f, 0.001f, "B4 `SoftnessPx` 取值视图 x = 42（`Mathf.Max(0,·)` 那一支没把它夹掉）");
                N(vc.SoftnessPx.y, 0f, 0.001f, "B4 `SoftnessPx` y = 0（上下是硬边 —— 原版不是 (42,42)）");
                Tr(vc.softness.x > 0,
                   "B5 ★ 灭自证：`softness.x` **必须 > 0**（旧值 0 = 硬边）—— 与 B2 不共因，只改其中一边照样红");
                Tr(vc.HasBaseRect,
                   "B6（前提）`CaptureNow()` 生效（基准矩形已记）⇒ 这条软边真能解析出框，不是「存了值但没用」");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
        }

        // ---- ★ 源码闸：生产调用点真的换成写口了（⛔ 读**非注释**部分）----
        string src = ReadSrc("CardPresentation/Shell/DailyRewardPopup.cs");
        if (src != null)
        {
            C(CountOfCode(src, "ApplyArmyViewportClip(vpNode.gameObject)"), 1,
              "B7 ★ `BuildArmySelector` 里**调用写口**恰 1 处（`ApplyArmyViewportClip(vpNode.gameObject)`）"
            + "｜🧨 删掉它改回内联 `AddComponent<ViewportClip>()` ⇒ 0 ⇒ 红");
            C(CountOfCode(src, "vpNode.gameObject.AddComponent<ViewportClip>"), 0,
              "B8 ★ 阵营条那条**旧的**内联挂载已消失（`vpNode.gameObject.AddComponent<ViewportClip>` 0 处）");
            C(CountOfCode(src, "vc.softness = Vector2Int.zero"), 1,
              "B9 ★ 全文件**非注释代码**里 `vc.softness = Vector2Int.zero` 恰 **1** 处 —— 只剩奖格轨那颗"
            + "（`Rewards Scroll View/Viewport` 原版就是 (0,0)）"
            + "｜🧨 把阵营条改回旧值 ⇒ 2 处 ⇒ 红；把奖格轨也改成 (42,0) ⇒ 0 处 ⇒ 红");
            C(CountOfCode(src, "public const int ArmyViewportSoftX = 42, ArmyViewportSoftY = 0"), 1,
              "B10 原版两个 softness 分量在代码里**只写一处**（`ArmyViewportSoftX/Y` 常量声明）");
        }
    }

    // ------------------------------------------------------------ 工具
    static string ReadSrc(string rel)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, rel);
            if (!File.Exists(p))
            {
                Tr(false, "S0 读得到源码 `" + rel + "`（" + p + "）—— 读不到时源码闸**等于没验** ⇒ 当场红");
                return null;
            }
            return File.ReadAllText(p);
        }
        catch (Exception e)
        {
            Tr(false, "S0 读源码 `" + rel + "` 抛异常：" + e.GetType().Name + "：" + e.Message);
            return null;
        }
    }

    /// <summary>数**非注释行**里的子串出现次数（整行 `//` 开头的跳过 —— 文档/注释里也会出现这些字面量）。</summary>
    static int CountOfCode(string src, string needle)
    {
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(needle)) return -1;
        int n = 0;
        foreach (var raw in src.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            var t = line.TrimStart();
            if (t.StartsWith("//", StringComparison.Ordinal)) continue;   // 注释行不算
            int at = 0;
            while ((at = line.IndexOf(needle, at, StringComparison.Ordinal)) >= 0) { n++; at += needle.Length; }
        }
        return n;
    }
}
