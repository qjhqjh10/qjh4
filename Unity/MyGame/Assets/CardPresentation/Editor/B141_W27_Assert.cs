// B141_W27_Assert.cs — 执行写手 `B141-W27`（第一百二十八会话 · 波次 `W129` · 条目 `4-24` / `4-16`）
//
// ============================ 它盯的是什么 ============================
//  判据 = `资料/完全复刻/判据/B141_W129_对账_UI动画.md` 的 **W12 包**
//         （`4-24` 一轮 · `4-16` 三处）· 审计 = `资料/完全复刻/报告/B138_审计汇总_问题清单.md` §4
//         （`4-24` ⛔ 未建 · `4-16` 视口 pad/softness 全 0）。
//  原版逐字段读数（`d:/2/tools/assetripper_out/ExportedProject/Assets/` 现读，2026-11-04）：
//
//  ── `4-24`：`Border Glow Up/Down`（`Draft Mode Menu Demo.prefab`）──
//    `Draft Mode Ongoing State/MainContent/Scaled Content/Glows/{Border Glow Up,Border Glow Down}`
//      · `Glows`：拉满锚点 + `sizeDelta (1820,980)` + `anchoredPosition (−20.999878, 9.547968)` + `localScale.x 1.1`
//      · `Border Glow Up`：`apos (28,109.5)` · `size (700,67.08)`；`Border Glow Down`：`apos (28,−118.3)` · 同 size
//      · 两颗 `RectMask2D`：`m_Padding (0,0,0,0)` · `m_Softness (0,174)`
//      · 子件 `Glow`：`apos (0,26.89)` / `(0,−27.5)` · `size (700,53.78)` · `Image` = `40k_Separator Fade Sides Horizontal`
//        （`m_Type=Sliced` · `m_Border 63,0,63,0` · `m_PixelsPerUnitMultiplier 0.34` · `m_Color (0.7019608,0.46274513,0.17254902,1)`）
//    ⚠️ 四份 prefab（`Draft Mode {Menu Demo,Timed Mode Window,Ongoing Content,Ongoing State}`）这两颗**逐字段同值**；
//       我们建的这一扇 = `Menu Demo`，`Glows` 取它在 `Menu Demo` 里的**实例覆盖值**（另两份独立 prefab 的
//       `Glows` 是 `apos (235,−21.452) · size (532.8,356.12)` —— ⛔ 别混）。
//    绝对 px = prefab 值 × `MainContent.localScale 1.06`（× `Glows.localScale.x 1.1`，只对 x）。
//
//  ── `4-16` 三处（都在本写手白名单内）──
//    ① `DraftModeWindow` 牌表视口 = `Draft Mode Menu Demo ▸ Draft Mode Deck Info Panel ▸ … ▸ Scroll View ▸ Viewport`
//       `m_Padding (0,0,0,−10)` · `m_Softness (0,54)`
//       （⚠️ 独立 `Draft Mode Deck Info Panel.prefab` 那份是 `(0,0)/(0,0,0,0)` —— **以 Menu Demo 实例覆盖为准**）
//    ② `RankedTab` 视口 = `Player Profile Window ▸ … ▸ Ranking Tab/AllFactions/scroll rect/viewport`
//       `m_Padding (0,0,0,−10)`（Top = −10 ⇒ 顶上外扩 10px）· `m_Softness (0,0)`
//       🔴 **订正派活单**：任务里写的「RankedTab 原版 soft 42」**是错的** —— `soft (42,0)` 属于
//          `Daily Reward Popup ▸ Army Selector ▸ Viewport`（`4-16` 的第一处、`B141-W6` 的活）。
//          本处按 prefab 实读的 `pad (0,0,0,−10)` 复刻。
//    ③ `EnergySinglePlayerOnlyEventWindow` 的 `Image Mask`：`m_Padding (0,0,0,0)` · `m_Softness (375,0)`
//       （左右各 375px 渐隐）。🔴 **订正派活单**：任务里写的「pad 25」**不是这颗的 padding** ——
//          本 prefab 的 `Army Selector Panel/Army Selector/Viewport` 是**第二颗** `RectMask2D`
//          （`m_Softness (25,0)`），它由共享件 `Shell/ShowEventArmiesPanel.cs:110` 建（**2026-11-04 订正：已由同波 `B141-W56`/`W62` 落成传参 `(25,0)`**；原写「不在本批白名单 ⇒ 只记不修」已过期）
//          ⇒ 本写手当初只记不修（报告 `报告/B141_W129_GlowClip3.md` §三）。
//
// ============================ 灭自证（🧨 每条怎么才能红）============================
//  · A2/A4/B2/C2/D2 拿**字面量**比 `ViewportClip.softness` / `padding`（⛔ 不拿被测常量比自己）：
//    把 `(0,174)` / `(0,54)` / `(0,0,0,−10)` / `(375,0)` 任一改回 0 ⇒ 当场红。
//  · A6/B4/C4 再拿**字面量矩形**比 `BaseRect`（= prefab 折算出来的画布 px）⇒ 谁把节点摆错/漏折 1.06 或 1.1 ⇒ 红。
//  · A8（结构性判别式，与 A4 **不共因**）：`Glow` 那颗 `ImageQuad` 的**四角 alpha 不全 1** ——
//    软边是通过 `MenuDraw.ApplySoftEdges` 落到顶点色上的；把 `softness` 改回 `(0,0)` ⇒ 没有斜坡 ⇒ 红。
//  · B3/C3（结构性判别式，与 B2/C2 不共因）：`State.RenderClip` 相对 `BaseRect` 的**外扩量**必须逐边等于
//    pad 的负值（Draft 只顶上 +10 · Ranked 只顶上 +10）；改成正 pad / 0 ⇒ 外扩量变 0 ⇒ 红。
//  · E 段扫**源码非注释部分**（口径同 `B141_W25_Assert.CountOfCode`）：把三处调用改回 `Vector4.zero, Vector2Int.zero`
//    ⇒ `ViewportClip.Hang(...)` 那一行的 pad/soft 字面量找不到 ⇒ 红；把 `BuildOngoingGlows(scaled)` 删掉 ⇒ 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  宿主 = `CardPresentation/Editor/MainMenuScene.cs`（同波 `B141W6_Assert` / `B141W25_Assert` 的宿主；
//  轮抽窗的 `DraftWindowAssertD2` 也在那儿 —— 本件与它同域、互不依赖）。
//  在该宿主**合计行之前、同一物理行**（该行第一个 `//` **之前**）追加：
//      B141W27_Assert.Run(); _sink.Pass += B141W27_Assert.Pass; _sink.Fail += B141W27_Assert.Fail;
//  （计数器变量名以宿主当时那一对为准。⚠️ 追加必须落在第一个 `//` 之前 —— 本仓已 6 次踩
//   「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」。）
//  ⚠️ 本件**会建两扇窗再销毁**（`DraftModeWindow` / `EnergySinglePlayerOnlyEventWindow`）+ 一格裸 `RankedTab`；
//     三个 `finally` 里都 `DestroyImmediate`，并清 `DraftModeWindow` 的静态一局（`ClearRunForTest`）。
//     它不需要场景已有这些窗（`WindowsManager.Get()` 会现建宿主）。
//
// ============================ 判据 / 报告 ============================
//  · 报告：`资料/完全复刻/报告/B141_W129_GlowClip3.md`
//  · 原版：`Draft Mode Menu Demo.prefab` / `Draft Mode Timed Mode Window.prefab` / `Player Profile Window.prefab` /
//          `EnergySinglePlayerOnlyEventWindow.prefab` 的 `RectMask2D.m_Padding` / `m_Softness` 与 `RectTransform`
using System;
using System.IO;
using CardPresentation;
using UnityEngine;

/// <summary>执行写手 `B141-W27`：`4-24`（`Border Glow Up/Down`）+ `4-16` 三处视口裁切的落地回归断言。
/// <para>⛔ 期望值全是 prefab 实读的字面量，不从被测实现里读回来。</para></summary>
public static class B141W27_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B141W27] ") { Near = MenuNearStyle.Compact3 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void N(float got, float want, float tol, string msg) { MenuCheck.Near(_sink, got, want, tol, msg); }

    // 原版 `Glows` 两颗掩码 / 图片（画布 px，已折 1.06 × 1.1；推导见报告 §二）
    const float GlowL = 685.99f, GlowR = 1502.19f;
    const float UpT = 389.66f, UpB = 460.76f;
    const float DnT = 631.13f, DnB = 702.23f;

    // 原版 `Draft Mode Deck Info Panel ▸ … ▸ Scroll View ▸ Viewport`（= 我们 `DeckScrollR`，330.97×588.88 @ (1530,270)）
    const float DeckL = 1530f, DeckT = 270f, DeckR = 1860.97f, DeckB = 858.88f;

    // 原版 `Ranking Tab/AllFactions/scroll rect/viewport`（441.68×575.76 @ (1305.29,288.62)-(1746.97,864.38)）
    const float RankL = 1305.29f, RankT = 288.62f, RankR = 1746.97f, RankB = 864.38f;

    /// <summary>唯一的入口（宿主聚合行里调）。</summary>
    public static void Run()
    {
        _sink.Pass = 0; _sink.Fail = 0;
        MenuCheck.Section(_sink, "🆕 B141_W27：4-24 `Border Glow Up/Down` + 4-16 三处视口裁切（Draft 牌表 pad −10/soft 54 · Ranked pad −10 · Energy soft 375）");
        try { WindowsManager.Get(); } catch (Exception e) { Tr(false, "（前提）`WindowsManager.Get()` 抛异常：" + e.GetType().Name + "：" + e.Message); }
        A_DraftWindow();
        C_RankedViewport();
        D_EnergyMask();
        E_SourceStructure();
        Debug.Log(_sink.P + "合计：✓ " + _sink.Pass + " · ✗ " + _sink.Fail);
    }

    // ==================================================================
    //  A · 草稿窗（一扇窗跑完 `4-24` 两条 Border Glow + `4-16①` 牌表视口）
    // ==================================================================
    static void A_DraftWindow()
    {
        DraftModeWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            Tr(wm != null, "A0（前提）拿得到 `WindowsManager`（`Get()` 缺了就现建宿主）");
            if (wm == null) return;
            win = DraftModeWindow.Create(wm, 20261104);
            Tr(win != null, "A0（前提）`DraftModeWindow.Create` 建出窗来");
            if (win == null) return;

            var glows = win.transform.Find("Draft Mode Ongoing State/MainContent/Scaled Content/Glows");
            Tr(glows != null, "A1（前提）`…/Scaled Content/Glows` 节点建出来了");
            if (glows != null)
            {
                CheckGlow(glows, "Border Glow Up", UpT, UpB, "A");
                CheckGlow(glows, "Border Glow Down", DnT, DnB, "B");
            }

            CheckDeckViewport(win, "B");
        }
        catch (Exception e)
        {
            Tr(false, "A 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null)
            {
                try { PointerLayer.UnregisterOwnedBy(win.gameObject); } catch { }   // 编辑模式不派 `OnDestroy` ⇒ 手动摘滚动区
                try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { }
            }
            DraftModeWindow.ClearRunForTest();
            WindowsManager.ClearReuseCacheForTest();
        }
    }

    static void CheckGlow(Transform glows, string name, float top, float bot, string p)
    {
        var t = glows.Find(name);
        Tr(t != null, p + "1 ★ `" + name + "` 建出来了｜🧨 退回「不建」⇒ 红");
        if (t == null) return;

        var vc = t.GetComponent<ViewportClip>();
        Tr(vc != null, p + "1b ★ `" + name + "` 上挂着 `ViewportClip`（= 原版那颗 `RectMask2D` 的等价物）");
        if (vc == null) return;

        C(vc.softness.x, 0, p + "2 ★ `m_Softness.x` = **0**｜实得 " + vc.softness.x);
        C(vc.softness.y, 174, p + "2 ★ `m_Softness.y` = **174**（四份 prefab 逐值同）｜🧨 改回 0 ⇒ 红");
        C(vc.padding.x, 0f, p + "3 ★ `m_Padding.x`（Left）= **0**");
        C(vc.padding.y, 0f, p + "3 ★ `m_Padding.y`（Bottom）= **0**");
        C(vc.padding.z, 0f, p + "3 ★ `m_Padding.z`（Right）= **0**");
        C(vc.padding.w, 0f, p + "3 ★ `m_Padding.w`（Top）= **0**");

        Tr(vc.HasBaseRect, p + "5（前提）掩码节点记下了设计矩形");
        if (vc.HasBaseRect)
        {
            var b = vc.BaseRect;
            N(b.x1, GlowL, 0.05f, p + "6 ★ 掩码左沿 = **685.99**（apos 28 × 1.1 × 1.06 折算）");
            N(b.x2, GlowR, 0.05f, p + "6 ★ 掩码右沿 = **1502.19**（700 × 1.1 × 1.06）");
            N(b.y1, top, 0.05f, p + "6 ★ 掩码上沿 = **" + top + "**｜🧨 漏乘 1.06 或 y 取反 ⇒ 红");
            N(b.y2, bot, 0.05f, p + "6 ★ 掩码下沿 = **" + bot + "**（67.08 × 1.06 = 71.10 高）");
            var rc = vc.State.RenderClip;
            Tr(rc.HasValue, p + "6b ★ `State.RenderClip` 非空（这颗掩码真的会裁）");
            if (rc.HasValue)
            {
                N(rc.Value.y1, top, 0.05f, p + "6b ★ padding 0 ⇒ 裁切框顶上沿 = 掩码上沿（不外扩）");
                N(rc.Value.H, bot - top, 0.05f, p + "6b ★ 裁切框高 = 71.10");
            }
        }

        var glow = t.Find("Glow");
        Tr(glow != null, p + "7 ★ 子件 `Glow` 建出来了（九宫图挂在掩码节点下）");
        if (glow == null) return;
        // ⚠️ `MenuDraw.Nine` 的根是**裸 `GameObject`**（没有 `ImageQuad`、也没有 `RectTransform`），
        //    九块子块才各带一个 `ImageQuad` ⇒ 这里必须 `GetComponentsInChildren`（`Tr(q != null)` 那种写法会假红）。
        var quads = glow.GetComponentsInChildren<ImageQuad>(true);
        Tr(quads != null && quads.Length > 0, p + "7b ★ `Glow` 下建出了九宫子块（`ImageQuad` × " + (quads == null ? 0 : quads.Length) + "）");
        if (quads == null || quads.Length == 0) return;

        ImageQuad first = null;
        for (int i = 0; i < quads.Length; i++)
            if (quads[i] != null && quads[i].gameObject.activeSelf && quads[i].Texture != null) { first = quads[i]; break; }
        Tr(first != null, p + "7c ★ 至少一块**可见**子块挂着贴图");
        if (first != null)
            C(first.Texture.name, "40k_Separator_Fade_Sides_Horizontal",
              p + "7d ★ 图 = `40k_Separator Fade Sides Horizontal`（原版 `m_Sprite`）");

        // 软边：`MenuDraw.ApplySoftEdges` 把渐隐带落到**子块的四角 alpha** 上 ⇒ 必有 < 1 的角
        bool ramp = false; float minA = 1f;
        for (int i = 0; i < quads.Length; i++)
        {
            var q = quads[i];
            if (q == null || !q.gameObject.activeSelf) continue;
            var cc = q.CornerColors;
            if (cc == null) continue;
            for (int j = 0; j < cc.Length; j++)
            { if (cc[j].a < minA) minA = cc[j].a; if (cc[j].a < 0.99f) ramp = true; }
        }
        Tr(ramp, p + "8 ★ 软边真的落到顶点色上（最小角 alpha " + minA.ToString("F3")
                  + " < 1）｜🧨 `softness` 改回 (0,0) ⇒ 没有斜坡 ⇒ 红");
    }

    // ==================================================================
    //  `4-16①` Draft 牌表视口：pad (0,0,0,−10) + soft (0,54)
    //  （宿主窗由 A 段建好传进来 —— ⛔ 别为它另建一扇，静态状态容易互相踩）
    // ==================================================================
    static void CheckDeckViewport(DraftModeWindow win, string p)
    {
        try
        {
            var vp = win.transform.Find("Draft Mode Deck Info Panel/Cards in deck panel/Scroll View/Viewport");
            Tr(vp != null, p + "1（前提）牌表 `Scroll View/Viewport` 建出来了");
            if (vp == null) return;
            var vc = vp.GetComponent<ViewportClip>();
            Tr(vc != null, p + "1 ★ 牌表视口上挂着 `ViewportClip`");
            if (vc == null) return;

            C(vc.softness.x, 0, p + "2 ★ `m_Softness.x` = **0**");
            C(vc.softness.y, 54, p + "2 ★ `m_Softness.y` = **54**（上下各 54px 渐隐）｜🧨 改回 0 ⇒ 红");
            C(vc.padding.x, 0f, p + "2 ★ `m_Padding.x` = **0**");
            C(vc.padding.y, 0f, p + "2 ★ `m_Padding.y` = **0**");
            C(vc.padding.z, 0f, p + "2 ★ `m_Padding.z` = **0**");
            C(vc.padding.w, -10f, p + "2 ★ `m_Padding.w`（Top）= **−10**（负 = 顶上外扩 10px）｜🧨 改回 0 ⇒ 红");

            Tr(vc.HasBaseRect, p + "4（前提）记下了设计矩形");
            if (vc.HasBaseRect)
            {
                var b = vc.BaseRect;
                N(b.x1, DeckL, 0.05f, p + "4 ★ 视口左沿 = **1530**");
                N(b.y1, DeckT, 0.05f, p + "4 ★ 视口上沿 = **270**");
                N(b.x2, DeckR, 0.05f, p + "4 ★ 视口右沿 = **1860.97**");
                N(b.y2, DeckB, 0.05f, p + "4 ★ 视口下沿 = **858.88**");
                var rc = vc.State.RenderClip;
                Tr(rc.HasValue, p + "3 ★ `State.RenderClip` 非空");
                if (rc.HasValue)
                {
                    var c = rc.Value;
                    N(b.y1 - c.y1, 10f, 0.05f, p + "3 ★ 渲染框比设计框**顶上多 10px**（负 pad 的必然结果）｜🧨 pad 改 0 ⇒ 0 ⇒ 红");
                    N(c.y2, DeckB, 0.05f, p + "3 ★ 底沿不动（只顶上外扩）");
                    N(c.W, b.W, 0.05f, p + "3 ★ 宽不变（左右 pad 都是 0）");
                }
            }
        }
        catch (Exception e)
        {
            Tr(false, p + " 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
    }

    // ==================================================================
    //  C · `4-16` RankedTab：pad (0,0,0,−10) · soft (0,0)
    // ==================================================================
    static void C_RankedViewport()
    {
        GameObject go = null;
        try
        {
            WindowsManager.Get();
            go = new GameObject("B141W27RankedTab", typeof(RectTransform));
            var tab = go.AddComponent<RankedTab>();
            Tr(tab != null, "C0（前提）`AddComponent<RankedTab>()` 成功");
            if (tab == null) return;
            tab.Setup();                                   // `ProfilePage.Setup()` = 建本页内容（public sealed override）
            var vp = go.transform.Find("AllFactions/scroll rect/viewport");
            Tr(vp != null, "C1（前提）`AllFactions/scroll rect/viewport` 建出来了");
            if (vp == null) return;
            var vc = vp.GetComponent<ViewportClip>();
            Tr(vc != null, "C1 ★ 排行视口上挂着 `ViewportClip`");
            if (vc == null) return;

            C(vc.softness.x, 0, "C2 ★ `m_Softness.x` = **0**（硬边；`soft 42` 是 `Daily Reward Popup ▸ Army Selector`，不是这里）");
            C(vc.softness.y, 0, "C2 ★ `m_Softness.y` = **0**");
            C(vc.padding.x, 0f, "C2 ★ `m_Padding.x` = **0**");
            C(vc.padding.y, 0f, "C2 ★ `m_Padding.y` = **0**");
            C(vc.padding.z, 0f, "C2 ★ `m_Padding.z` = **0**");
            C(vc.padding.w, -10f, "C2 ★ `m_Padding.w`（Top）= **−10**（顶上外扩 10px）｜🧨 改回 0 ⇒ 红");

            Tr(vc.HasBaseRect, "C4（前提）记下了设计矩形");
            if (vc.HasBaseRect)
            {
                var b = vc.BaseRect;
                N(b.x1, RankL, 0.05f, "C4 ★ 视口左沿 = **1305.29**");
                N(b.y1, RankT, 0.05f, "C4 ★ 视口上沿 = **288.62**");
                N(b.x2, RankR, 0.05f, "C4 ★ 视口右沿 = **1746.97**");
                N(b.y2, RankB, 0.05f, "C4 ★ 视口下沿 = **864.38**");
                var rc = vc.State.RenderClip;
                Tr(rc.HasValue, "C3 ★ `State.RenderClip` 非空");
                if (rc.HasValue)
                {
                    var c = rc.Value;
                    N(b.y1 - c.y1, 10f, 0.05f, "C3 ★ 渲染框比设计框**顶上多 10px**｜🧨 pad 改 0 ⇒ 红");
                    N(c.y2, RankB, 0.05f, "C3 ★ 底沿不动");
                    N(c.W, b.W, 0.05f, "C3 ★ 宽不变");
                }
            }
        }
        catch (Exception e)
        {
            Tr(false, "C 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (go != null)
            {
                try { PointerLayer.UnregisterOwnedBy(go); } catch { }   // 编辑模式 `DestroyImmediate` 不派 `OnDestroy` ⇒ 手动摘滚动区
                try { UnityEngine.Object.DestroyImmediate(go); } catch { }
            }
        }
    }

    // ==================================================================
    //  D · `4-16` Energy：`Image Mask` soft (375,0)
    // ==================================================================
    static void D_EnergyMask()
    {
        EnergySinglePlayerOnlyEventWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (wm == null) { Tr(false, "D0（前提）拿不到 `WindowsManager`"); return; }
            win = EnergySinglePlayerOnlyEventWindow.Create(wm);
            Tr(win != null, "D0（前提）`EnergySinglePlayerOnlyEventWindow.Create` 建出窗来");
            if (win == null) return;

            var mask = win.transform.Find("Image Mask");
            Tr(mask != null, "D1（前提）`Image Mask` 建出来了");
            if (mask == null) return;
            var vc = mask.GetComponent<ViewportClip>();
            Tr(vc != null, "D1 ★ `Image Mask` 上挂着 `ViewportClip`");
            if (vc == null) return;

            C(vc.softness.x, 375, "D2 ★ `m_Softness.x` = **375**（左右各 375px 渐隐）｜🧨 改回 0 ⇒ 红");
            C(vc.softness.y, 0, "D2 ★ `m_Softness.y` = **0**");
            C(vc.padding.x, 0f, "D2 ★ `m_Padding.x` = **0**（派活单写的「pad 25」不是这颗的 padding，是 Army Selector 视口的 softness）");
            C(vc.padding.y, 0f, "D2 ★ `m_Padding.y` = **0**");
            C(vc.padding.z, 0f, "D2 ★ `m_Padding.z` = **0**");
            C(vc.padding.w, 0f, "D2 ★ `m_Padding.w` = **0**");

            var rc = vc.State.RenderClip;
            Tr(rc.HasValue, "D3 ★ `State.RenderClip` 非空（真会裁）");
            Debug.Log(_sink.P + " ℹ️ `Army Selector Panel/Army Selector/Viewport` 是本 prefab 的**第二颗**掩码"
                    + "（原版 `m_Softness (25,0)`），由共享件 `Shell/ShowEventArmiesPanel.cs:110` 建（(25,0)，2026-11-04 W56）"
                    + " ⇒ ⛔ 本写手当时不改（2026-11-04 W56 已落成传参）；逐窗值见 `报告/B141_W129_GlowClip3.md` §三"
                    + "（Energy (25,0) · Skirmish/Ranked (0,52) · Campaign (0,0) ⇒ 得给它加参数，不能写常量）。");
        }
        catch (Exception e)
        {
            Tr(false, "D 段抛异常（不当成通过）：" + e.GetType().Name + "：" + e.Message);
        }
        finally
        {
            if (win != null)
            {
                try { PointerLayer.UnregisterOwnedBy(win.gameObject); } catch { }
                try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { }
            }
        }
    }

    // ==================================================================
    //  E · 源码结构（★ 灭自证：改回 0 / 删掉建节点都会红）
    // ==================================================================
    static void E_SourceStructure()
    {
        // ① 轮抽文件：`BuildOngoingGlows(scaled)` 恰 1 处 + 两处掩码参数行
        string draft = ReadSrc("CardPresentation/Shell/DraftModeWindow.cs", "E1");
        if (draft != null)
        {
            C(CountOfCode(draft, "BuildOngoingGlows(scaled)"), 1,
              "E1 ★ 源码里 `BuildOngoingGlows(scaled)` 恰 **1** 处（Ongoing 页真的接上了）｜🧨 删掉那一句 ⇒ 0 ⇒ 红");
            C(CountOfCode(draft, "\"Border Glow Up\""), 1, "E2 ★ `\"Border Glow Up\"` 恰 **1** 处");
            C(CountOfCode(draft, "\"Border Glow Down\""), 1, "E2 ★ `\"Border Glow Down\"` 恰 **1** 处");
            C(CountOfCode(draft, "new Vector2Int(0, 174)"), 1,
              "E3 ★ 掩码 softness 字面量 `new Vector2Int(0, 174)` 恰 **1** 处｜🧨 改回 `Vector2Int.zero` ⇒ 0 ⇒ 红");
            C(CountOfCode(draft, "new Vector2Int(0, 54)"), 1,
              "E3 ★ 牌表视口 softness 字面量 `new Vector2Int(0, 54)` 恰 **1** 处｜🧨 改回 `Vector2Int.zero` ⇒ 0 ⇒ 红");
            C(CountOfCode(draft, "new Vector4(0f, 0f, 0f, -10f)"), 1,
              "E3 ★ 牌表视口 pad 字面量 `new Vector4(0f, 0f, 0f, -10f)` 恰 **1** 处｜🧨 改回 `Vector4.zero` ⇒ 0 ⇒ 红");
        }

        // ② 排行页：视口那一行必须带 pad 字面量、且不再传 `Vector4.zero`
        string ranked = ReadSrc("CardPresentation/Shell/RankedTab.cs", "E4");
        if (ranked != null)
        {
            string line = CodeLineWith(ranked, "ViewportClip.Hang(sc, \"viewport\"");
            Tr(line != null, "E4（前提）找得到排行页那颗 `ViewportClip.Hang(sc, \"viewport\"…)` 的调用行");
            if (line != null)
            {
                Tr(line.Contains("new Vector4(0f, 0f, 0f, -10f)"),
                   "E5 ★ 排行视口那一行带着 `new Vector4(0f, 0f, 0f, -10f)`｜🧨 改回 `Vector4.zero` ⇒ 红");
                Tr(!line.Contains("Vector4.zero"),
                   "E5 ★ 排行视口那一行**不再**传 `Vector4.zero`（pad 全 0）");
            }
        }

        // ③ 能源窗：`Image Mask` 那一行必须带 softness 375
        string energy = ReadSrc("CardPresentation/Shell/EnergySinglePlayerOnlyEventWindow.cs", "E6");
        if (energy != null)
        {
            string line = CodeLineWith(energy, "ViewportClip.Hang(root, \"Image Mask\"");
            Tr(line != null, "E6（前提）找得到 `ViewportClip.Hang(root, \"Image Mask\"…)` 的调用行");
            if (line != null)
            {
                Tr(line.Contains("new Vector2Int(375, 0)"),
                   "E7 ★ `Image Mask` 那一行带着 `new Vector2Int(375, 0)`｜🧨 改回 `Vector2Int.zero` ⇒ 红");
                Tr(!line.Contains("Vector2Int.zero"),
                   "E7 ★ `Image Mask` 那一行**不再**传 `Vector2Int.zero`（softness 全 0）");
            }
        }
    }

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

    /// <summary>返回**第一条非注释**且包含 needle 的代码行（去掉行尾注释）。</summary>
    static string CodeLineWith(string src, string needle)
    {
        int i = 0;
        while (i < src.Length)
        {
            int eol = src.IndexOf('\n', i);
            if (eol < 0) eol = src.Length;
            string line = src.Substring(i, eol - i);
            i = eol + 1;
            if (line.TrimStart().StartsWith("//", StringComparison.Ordinal)) continue;
            if (line.IndexOf(needle, StringComparison.Ordinal) < 0) continue;
            int k = line.IndexOf("//", StringComparison.Ordinal);
            return k >= 0 ? line.Substring(0, k) : line;
        }
        return null;
    }
}
