// B114_W48_Assert.cs — 第一百一十四会话 · 写手 `W-48`（2026-11-01）· 账 `B-292`
//
// ============================ 它盯的是什么 ============================
//  **`ItemDisplayWindow` 的「数量格」是同一条链上的第二个尺度来源。**
//  同一个 `ItemBoxRect()`（429×650）被两处折算：
//    · 抽屉本体 —— 原版 `ItemDrawer<T>.Draw`（`ItemDisplayWindow__Open.c` 第 ② 跳），已按 prefab 的
//      `AspectRatioFitter`（`Cardback Drawer`：`m_AspectMode 2` 高控宽 · `m_AspectRatio 0.66`）走
//      `RootFit.HeightControlsWidth`（`B114_W47` 迁完 26 处）；
//    · 数量格 —— `BuildAmount()` 原来还停在 `FrameOf` 的**默认 `Contain`**（等比内接）⇒ **两条口径**。
//  今天看不出来：429/650 = **0.66** 正好等于根的比例 ⇒ 两支的 `K` 逐值相等（0.6018519）；
//  **框一旦不是 0.66 就分家** —— 窄框下 `Contain` 把整格缩进框里、而原版抽屉根是**横向溢出**
//  ⇒ 数字跑到抽屉外。本批把 `BuildAmount` 收口到 `ItemDisplayWindow.AmountCellOf(box)`（唯一尺度来源），
//  本节断的就是「那条口径 = ARF，不是内接」。
//
//  判据（第一权威 · 全部本地现读）：
//    · `ItemDisplayWindow.cs` 文件头「Item Holder 里的卡背」那一行：`Cardback Drawer` 根
//      `AspectRatioFitter(m_AspectMode = 2 高控宽, m_AspectRatio = 0.66)`；
//    · UGUI `AspectRatioFitter.cs:151-156`：`m_AspectMode 2` 只有一句
//      `SetSizeWithCurrentAnchors(Horizontal, rect.height * m_AspectRatio)` —— **完全不看父框的宽**；
//    · 数量格的设计框 = `ItemDrawerLayout.Metrics()` 的 `QtyX1..QtyX2 × LabelY1..LabelY2`
//      = `(61, 810)-(351.4, 918)`（`B100_E3` 13 份 prefab 逐份实读 · 逐份相同）。
//
// ============================ 🔴 期望值怎么来的（⛔ 不自证）============================
//  本节**手算**期望值（下面 A1~A10 的字面量都能拿纸笔复算），**不调** `ItemDrawer.FitK` /
//  `FrameOf` / `RootRectOf` 去反读，也不用 `ItemDrawer.Metrics()` 去取设计框（常量写死在测试里）。
//  夹具是**非 0.66 的框**（窄 400×650 / 宽 1200×650）—— 方框与 0.66 框**永远发现不了**这条缺陷。
//
// ============================ 🧨 灭自证（怎么才能红）============================
//  · A6 / A7：窄框上「ARF」与「内接」的 `K` 相差 **0.0407**、格宽相差 **11.81 px**
//    ⇒ 🧨 把 `ItemDisplayWindow.AmountCellOf` 里的 `fit: …HeightControlsWidth` 删掉（回默认 `Contain`）
//      ⇒ A1 / A2 / A4 / A6 / A7 / A10 同时红。
//  · A8 / A9：宽框下两支相同 ⇒ 分叉**只**在「框比根更窄」那一侧（🧨 谁改成「永远取框高×某常量」，
//    A10 换高度那条会红）。
//  · A10：换一个更高的框（400×700）⇒ `K` 必须跟着变成 700/1080（挡「恒返回某个固定值」）。
//  · B3：端到端断「建那一格用的 `K`」——它写进了字号（`75 × K`）= **45.1389 px**
//    （🧨 若这一格改走别的根高 1080→712.8 之类，字号立刻变）。
//
// ============================ ⚠️ 这一节能证到哪 / 证不到哪（如实记）============================
//  · **证得到**：`AmountCellOf` 这条口径在**任意框**上都是 ARF（窄框 A1~A7 / 宽框 A8~A9 / 变高 A10）；
//    生产窗真的从它取几何（B3 的字号 = `75 × K`）。
//  · **证不到（结构性限制）**：生产窗的框是**常量** `ItemBoxRect()` = 429×650（比例正好 0.66），
//    两支在这里**逐值相等** ⇒ 「`BuildAmount` 是否还调 `AmountCellOf`」**端到端抓不到**
//    （有人把那一行改回内联 `Contain`，本节的端到端仍绿）。⇒ 唯一护栏是**代码只有这一处口径**
//    （`AmountCellOf` 是 `public` 的单一入口，`BuildAmount` 只从它取 `K` 与 `Rect`）；
//    真要端到端可判别，得给 `ItemDisplayWindow` 加「框可注入」的测试口 —— 本批**没有**加
//    （那会让生产 API 为测试变形，且 `Item Holder` 是 prefab 常量）。
//
// ============================ 怎么接（⚠️ 只由调度台改宿主）============================
//  唯一入口 = `B114_W48_Assert.Run()`；宿主 `Editor/ShellScene.cs` 那条聚合行
//  **同一物理行、第一个 `//` 之前**追加：
//      B114_W48_Assert.Run(); _sink.Pass += B114_W48_Assert.Pass; _sink.Fail += B114_W48_Assert.Fail;
//
// ============================ 边界 ============================
//  · 本文件只读 + 建/拆临时窗口，**不写盘**。
//  · 窗口用 `ItemDisplayWindow.Create(null, ctx)`（与 `C2_Assert` / `B110_M2F_Assert` 同一入口），
//    跑完 `Close()` + `MainMenuSubmenuWindow.DestroySafe`。
using System;
using UnityEngine;
using CardPresentation;

/// <summary>写手 `W-48`：账 `B-292` —— 物品窗「数量格」必须与抽屉本体走同一条 ARF 口径。</summary>
public static class B114_W48_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B114W48] ") { Near = MenuNearStyle.Compact3 };
    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // ---- 判据常量（prefab 现读 · **独立于生产代码**）----
    /// <summary>基础抽屉根宽 = 1080 × ARF `m_AspectRatio 0.66`（`Cardback Drawer` 根）。</summary>
    const float RootW = 712.8f;
    /// <summary>基础抽屉根高。</summary>
    const float RootH = 1080f;
    /// <summary>`Content/Label/Quantity` 的设计框（`ItemDrawerLayout` 的共用尺 · 13 份 prefab 逐份实读 = 相同）。</summary>
    const float QtyX1 = 61f, QtyX2 = 351.4f, LbY1 = 810f, LbY2 = 918f;

    // ---- 夹具（后两个才是关键：方框 / 0.66 框上两支**逐值相等**，永远发现不了这条）----
    /// <summary>窄框 400×650（宽/高 = 0.6154 **小于**根的 0.66）⇒ 原版 ARF 横向溢出、`Contain` 会缩。</summary>
    static readonly PxRect BoxNarrow = new PxRect(100f, 50f, 500f, 700f);
    /// <summary>宽框 1200×650（宽/高 = 1.846 **大于** 0.66）⇒ 高度先满足，两口径相同。</summary>
    static readonly PxRect BoxWide = new PxRect(0f, 0f, 1200f, 650f);
    /// <summary>「更高」的窄框 400×700 —— 挡「恒返回某个固定值」。</summary>
    static readonly PxRect BoxTall = new PxRect(0f, 0f, 400f, 700f);

    public static void Run()
    {
        MenuCheck.Section(_sink, "🆕 B114_W48（B-292）：物品窗「数量格」= 抽屉本体同一条 ARF 口径（非 0.66 的框上必须与内接分家）");

        // 两段各自独立：A 段前提不成立不会把 B 段吞掉
        try { SectionFrame(); }
        catch (Exception e) { Tr(false, "A 段抛异常：" + e.GetType().Name + "：" + e.Message); }

        try { SectionEndToEnd(); }
        catch (Exception e) { Tr(false, "B 段抛异常：" + e.GetType().Name + "：" + e.Message); }

        Debug.Log(_sink.P + "B114W48 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · 口径（非 0.66 的框上两支必须分家）
    static void SectionFrame()
    {
        MenuCheck.Section(_sink, "A 口径：`AmountCellOf(box)` 走 `HeightControlsWidth`（K = 框高/根高，不看框宽）");

        // —— 手算（⛔ 不调生产函数）——
        //   ARF 高控宽：K = 650 / 1080 = 0.6018519
        //   等比内接  ：K = min(650/1080, 400/712.8) = 400/712.8 = 0.5611672
        const float KHeight = 650f / RootH;      // 0.6018519
        const float KContain = 400f / RootW;     // 0.5611672

        Tr(BoxNarrow.W > 0f && BoxNarrow.H > 0f,
           "A0（前提）窄框夹具是有效矩形（" + BoxNarrow.W.ToString("F0") + "×" + BoxNarrow.H.ToString("F0") + "）");

        var a = ItemDisplayWindow.AmountCellOf(BoxNarrow);
        Tr(!float.IsNaN(a.K) && !float.IsInfinity(a.K) && a.K > 0f,
           "A0b（前提）算出来的 `K` 是正常正数（= " + a.K.ToString("F6") + "）");

        N(a.K, KHeight, 1e-5f,
          "A1 ★★★ 窄框（400×650）下 `K` = 框高/根高 = **0.6018519**（= 650/1080 —— 原版 ARF「宽 = 高 × 0.66」，不看父框宽）");

        N(a.Rect.W, (QtyX2 - QtyX1) * KHeight, 0.01f,
          "A2 ★★ 那一格宽 = 290.4 × 0.6018519 = **174.7778**");
        N(a.Rect.H, (LbY2 - LbY1) * KHeight, 0.01f,
          "A3 ★★ 那一格高 = 108 × 0.6018519 = **65**");
        N(a.Rect.x1, 122.212963f, 0.01f,
          "A4 ★★ 左沿 = 85.5 + 61×0.6018519 = **122.213**（根左上 x = 300 − 712.8×K/2 = 85.5）");
        N(a.Rect.y1, 537.5f, 0.01f,
          "A5 ★★ 上沿 = 50 + 810×0.6018519 = **537.5**（根左上 y = 375 − 1080×K/2 = 50）");

        // 🧨 灭自证①：实测 K 必须**远离**「等比内接」那一支
        Tr(Mathf.Abs(a.K - KContain) > 0.03f,
           "A6 ★★★（灭自证）实测 K 与「等比内接」的 400/712.8 = 0.5611672 相差 "
         + Mathf.Abs(a.K - KContain).ToString("F4") + "（> 0.03）⇒ **不是内接**"
         + "｜🧨 把 `AmountCellOf` 里的 `fit:` 删掉（回默认 `Contain`）⇒ 本条与 A1 同时红");

        // 🧨 灭自证②：差值大到不是浮点噪声
        Tr(Mathf.Abs(a.Rect.W - (QtyX2 - QtyX1) * KContain) > 10f,
           "A7 ★★★（灭自证·结构）两口径的格宽差 = **11.81 px**（> 10）⇒ 不是浮点噪声：实测 "
         + a.Rect.W.ToString("F2") + " vs 内接的 " + ((QtyX2 - QtyX1) * KContain).ToString("F2"));

        // 对照：宽框下两支相同 ⇒ 分叉**只**在「框比根更窄」那一侧，不是「一律放大」
        var b = ItemDisplayWindow.AmountCellOf(BoxWide);
        N(b.K, KHeight, 1e-5f,
          "A8 ★ 宽框（1200×650）下 K 仍是 **0.6018519**（框够宽 ⇒ ARF 与内接同值）");
        N(b.Rect.W, a.Rect.W, 0.01f,
          "A9 ★ 宽框与窄框的格宽**相同**（174.7778）—— 格宽只跟**框高**走｜🧨 谁改成「按框宽算」⇒ 本条红");

        // 🧨 灭自证③：换个更高的框 ⇒ K 必须跟着变（挡「恒返回某个固定值」）
        var c = ItemDisplayWindow.AmountCellOf(BoxTall);
        N(c.K, 700f / RootH, 1e-5f,
          "A10 ★★（灭自证）框换成 400×700 ⇒ K = 700/1080 = **0.6481481**（跟着框高走；内接会给 0.5611672）");
    }

    // ============================================================ B · 端到端：生产窗真的从这条口径取几何
    static void SectionEndToEnd()
    {
        MenuCheck.Section(_sink, "B 端到端：`ItemDisplayWindow.Create` ⇒ 数量格的字号 = `75 × K`");

        ItemDisplayWindow w = null;
        try
        {
            w = ItemDisplayWindow.Create(null, new ItemDisplayWindow.Context
            {
                Title = "B114W48", Type = "B114W48", ArtName = "b114w48", Amount = 3, ShowAmount = true,
            });
            Tr(w != null, "B0（前提）`ItemDisplayWindow.Create(null, ctx)` 建得出来");
            if (w == null) return;

            Tr(w.HolderNode != null, "B1（前提）`Item Holder` 节点在");
            Tr(w.AmountLabel != null,
               "B2（前提）`ShowAmount = true` ⇒ 数量那一格建出来了（`AmountLabel != null`；建不出来 `BuildAmount` 会出声）");
            if (w.AmountLabel == null) return;

            Tr(w.AmountLabel.transform.parent == w.HolderNode,
               "B2b ★ 那一格挂在 `Item Holder` 下（与抽屉本体同一格 —— 这正是「第二个尺度来源」的成因）");

            // 生产框 = `ItemBoxRect()` = 429×650（宽/高正好 0.66 ⇒ ARF 与内接同值 0.6018519）。
            // 这一节能证的 = 「建那一格用的是哪条 K」：它写进了字号 `75 × K`。
            const float KBox = 650f / RootH;                      // 0.6018519（手算：429/712.8 与它相等）
            float glyphPx = w.AmountLabel.GlyphHeightWorld * (LayoutSpace.DesignPxH / LayoutSpace.DesignHeight);   // 世界单位 → px（÷108 的逆）
            N(glyphPx, 75f * KBox, 0.05f,
              "B3 ★★ 那一格的字号 = 75 × K = **45.1389 px**（K = 650/1080）⇒ `BuildAmount` 用的就是这条 K"
            + "｜🧨 若换成别的根（如 1080² 那一档）⇒ 字号变 ⇒ 红");

            Tr(w.AmountLabel.FontPxNow > 30f && w.AmountLabel.FontPxNow < 60f,
               "B4 渲出来的字号 " + w.AmountLabel.FontPxNow.ToString("F2") + "px 落在 (30, 60)（≈45.1，自适应上限 = 75×K；粗检，不当精确判据）");

            C(w.AmountLabel.Text, "3", "B5 那一格写的是 `Amount`（= 3）—— 顺带确认本节测的还是那条生产路");
        }
        catch (Exception e) { Tr(false, "B 段抛异常：" + e.GetType().Name + "：" + e.Message); }
        finally { Kill(ref w); }
    }

    static void Kill(ref ItemDisplayWindow w)
    {
        if (w == null) return;
        try
        {
            w.Close();
            MainMenuSubmenuWindow.DestroySafe(w.gameObject);
        }
        catch { /* 已关掉 / 已销毁：批处理下不算缺陷 */ }
        w = null;
    }
}
