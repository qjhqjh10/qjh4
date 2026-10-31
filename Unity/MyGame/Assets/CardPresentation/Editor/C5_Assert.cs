// C5_Assert.cs — 第一百零九会话（C5）两件的断言（2026-10-31）
//
// ============================ ① Gacha 进度条 `Fill` 宽度变了不会长 ============================
// 病灶：`BuildRewardsFooter` 里 `Fill` 的框是 `FillR`（x1 == x2 ⇒ 出厂 0 宽），而 `ImageQuad.CreateNineSlice`
//   在 `worldW == 0` 时把三列全算成 `w <= 0` **跳过** ⇒ 树上一个子块都没有；`SetRewardProgress` 原来只调
//   `MenuDraw.ApplyPxRect`（改 `sizeDelta`）**不重建** ⇒ 进度一变画面仍然空白。
// 判据（原版怎么变宽）：`ProgressBar` 是 uGUI `Slider`，`m_FillRect` → `Fill`
//   （`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_2768653388367320705.json` 现读：`m_Direction = 0`
//   (LeftToRight) · `m_MaxValue = 6` · `m_Value = 4.15`）；`Fill` 那颗 `Image` 的 `m_Type = 1`(Sliced，不是 Filled)
//   ⇒ `Slider.UpdateVisuals()` 走**改锚点**那支（`Library/PackageCache/com.unity.ugui@27635d171b1a/
//   Runtime/UGUI/UI/Core/Slider.cs:585-613`：`anchorMin = (0,0)` / `anchorMax[0] = value` / `anchorMax[1] = 1`）
//   ⇒ **填充矩形 = `Fill Area` 左下起「宽 × value/max」、满高**。
// 本件断言的是**画面结果**（九宫格子块真的建出来了、并集矩形 = 那条口径），⛔ 不断言我们的实现细节。
//
// ============================ ② Draft 结算页内层整体小 5.7% ============================
// 病灶：五个节点取的是**设计值**（`MainContent.localScale = 1.06` 之前的帧，÷1.06），而外层容器
//   （`MainContR` / `OnStagesR` / `OnLossesR` / `Win Marks`）与同页其余坐标都是**视觉值** ⇒ 里面每一件小一号、
//   并且逐件向左上漂。5.7% = 1 − 1/1.06。
// 判据 = `python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Draft Mode Ongoing State" --depth 6 --relative`
//   现读的**视觉值**（下面每条都写成原版字面量）。
//
// ============================ 边界 ============================
//   · 只读生产代码的节点树 / `ImageQuad` 的矩形与贴图，**不写任何游戏状态**（只调 `SetRewardProgress` 这个纯表现口）；
//   · 建出来的窗在 `finally` 里 `DestroyImmediate` + 还原子检静态口（`RewardsProvider` / `ClearRunForTest`）；
//   · 两组**各自 try/catch**（一组抛异常不许把另一组吞掉）。
using System;
using CardPresentation;
using UnityEngine;

public static class C5_Assert
{
    const string P = "[C5] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            try { RunGachaFill(); }   catch (Exception e) { Ok(false, "① Gacha 进度条 段异常（算红，⛔ 不是跳过）: " + e); }
            try { RunDraftLayout(); } catch (Exception e) { Ok(false, "② Draft 结算页版面 段异常（算红）: " + e); }
        }
        finally
        {
            Debug.Log(P + $"=== C5 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ============================================================ ① Gacha 进度条 `Fill`
    // 原版字面量（`menu_dump "Player doesn't have all items" --depth 4 --relative` 现读）：
    //   `ProgressBar` 514.00×62.46 · `Background` 514.00×37.48 ·
    //   `Fill Area` (50.0,46.4)–(564.0,75.5) ⇒ 宽 **514.0** · 高 **29.05**（相对 `Player doesn't have all items` 左上角）
    //   换成整窗坐标 = 我们 `FillAreaR`（1224.19, 921.2 → 1738.19, 950.2），宽 514.0 / 高 29.0 —— 同一条。
    //   ⚠️ 2026-10-31（`D2` 诊断）：dump 给的是**页内相对坐标**；整窗坐标要 + `Gacha Tab` 的 `PageDx 163.7875`
    //     （原版 `Gacha Tab` RectTransform：anchor(0,0)-(1,1)/pos(82,0)/size(−163.575,−0.359)）。
    const float WantAreaX1 = 1224.19f, WantAreaY1 = 921.2f, WantAreaY2 = 950.2f, WantAreaW = 514.0f;
    const string WantFillSprite = "40k_campaign_bar_fill_color";   // 原版 `m_Sprite.m_Name`（空格→下划线）

    static void RunGachaFill()
    {
        Debug.Log(P + "--- ① `GachaEventWindow` 进度条 `Fill`：宽度一变就重建九宫格 ---");

        var go = new GameObject("C5_Gacha");
        GachaEventWindow win = null;
        // 🔴 与 `B109_E27` 同一条口径：先把「奖励数据源」显式置空再建（别的自检装过 provider 的话，`Build` 会
        //    提前 `SetRewardProgress`，出厂 0 宽就测不到了）。收尾还原。
        var prevProvider = GachaEventWindow.RewardsProvider;
        GachaEventWindow.RewardsProvider = null;
        try
        {
            win = go.AddComponent<GachaEventWindow>();
            bool built = true;
            try { win.Build(); }
            catch (Exception e) { built = false; Ok(false, "①-前提：`GachaEventWindow.Build()` 不抛（实抛 " + e.GetType().Name + "：" + e.Message + "）"); }
            if (!built) return;

            var footer = Deep(win.transform, "Footer");
            var noAll = footer != null ? Child(footer, "Player doesn't have all items") : null;
            var bar = noAll != null ? Child(noAll, "ProgressBar") : null;
            var bg = bar != null ? Child(bar, "Background") : null;
            var area = bg != null ? Child(bg, "Fill Area") : null;
            var fill = area != null ? Child(area, "Fill") : null;
            Ok(fill != null, "①a 前提：`Footer ▸ Player doesn't have all items ▸ ProgressBar ▸ Background ▸ Fill Area ▸ Fill` 都在");
            if (fill == null) return;

            // ---- 出厂：0 宽 ⇒ 九宫格**一个子块都没有**（原版 `Slider.value = 0` 时也是 0 宽）----
            var nine = Child(fill, "Image");
            Ok(nine != null, "①b 前提：`Fill ▸ Image` 九宫格根在（名字 = `MenuDraw.Nine` 的 `name`）");
            int kids0 = nine != null ? nine.GetComponentsInChildren<ImageQuad>(true).Length : -1;
            Ok(kids0 == 0, "①c 出厂（进度 0）九宫格子块 = **0**（实得 " + kids0 + "）—— 判据：`CreateNineSlice` 在 `worldW == 0` 时三列全跳过");
            Ok(Mathf.Abs(NodeW(fill) - 0f) < 0.01f, "①d 出厂 `Fill` 宽 = 0.00 px（实得 " + NodeW(fill).ToString("F2") + "）");

            // ---- 3/6：节点宽 + ★★ 九宫格真的长出来了（改前这一条必红）----
            win.SetRewardProgress(3, 6);
            float wHalf = NodeW(fill);
            Ok(Mathf.Abs(wHalf - 257.0f) < 0.5f,
               "①e ★ 进度 3/6 ⇒ `Fill` 宽 = 514.0 × 3/6 = **257.0 px**（实得 " + wHalf.ToString("F2") + "）");
            int kids1 = ImageQuadCount(fill, "end");
            Ok(kids1 > 0,
               "①f ★★★ 进度非 0 时九宫格**真建出了子块**（实得 " + kids1 + " 块）—— 🧨 只改 `sizeDelta` 不重建 ⇒ 0 块、画面永远空白（本件修的缺陷）");
            PxRect fillRect;
            bool got = FillUnion(fill, out fillRect);
            Ok(got && Mathf.Abs(fillRect.W - 257.0f) < 0.6f,
               "①g ★★ 九宫格并集宽 = **257.0 px**（实得 " + (got ? fillRect.W.ToString("F2") : "<量不到>") + "）");
            Ok(got && Mathf.Abs(fillRect.x1 - WantAreaX1) < 0.5f,
               "①h ★★ 填充**左缘钉在 `Fill Area` 左缘** 1224.19（实得 " + (got ? fillRect.x1.ToString("F2") : "<量不到>") + "）—— 原版走 `anchorMin = (0,0)` 那一支（页内 1060.4 + `PageDx` 163.7875）");
            Ok(got && Mathf.Abs(fillRect.y1 - WantAreaY1) < 0.5f && Mathf.Abs(fillRect.y2 - WantAreaY2) < 0.5f,
               "①i ★ 填充**满高** 921.2–950.2（实得 " + (got ? fillRect.y1.ToString("F2") + "–" + fillRect.y2.ToString("F2") : "<量不到>") + "）—— 原版 `anchorMax.y = 1`");
            var endNode = Child(fill, "end");
            var endQ = endNode != null ? endNode.GetComponent<ImageQuad>() : null;
            float endCx = 0f;
            if (endQ != null) { float a, b, c, d; MenuDraw.QuadRectPx(endQ, out a, out b, out c, out d); endCx = (a + c) * 0.5f; }
            Ok(endQ != null && Mathf.Abs(endCx - 1469.14f) < 0.6f,
               "①j 端帽 `end` 中心 x = 填充右端 − 12.05 = **1469.14**（实得 " + endCx.ToString("F2") + "）—— 原版 `FillEndR` 与 `Fill` 的出厂关系（页内 1305.35 + `PageDx` 163.7875）");

            // ---- 满格 / 夹紧 / 归零：三种宽度各走一遍（重建不能越走越脏）----
            win.SetRewardProgress(6, 6);
            Ok(Mathf.Abs(NodeW(fill) - WantAreaW) < 0.6f,
               "①k 进度 6/6 ⇒ `Fill` 宽 = **514.0 px**（实得 " + NodeW(fill).ToString("F2") + "）");
            win.SetRewardProgress(9, 6);
            Ok(Mathf.Abs(NodeW(fill) - WantAreaW) < 0.6f,
               "①l 进度 9/6 ⇒ 夹到 max（514.0 px，实得 " + NodeW(fill).ToString("F2") + "）—— `Mathf.Clamp01` 照原版");
            win.SetRewardProgress(0, 6);
            Ok(Mathf.Abs(NodeW(fill)) < 0.01f && ImageQuadCount(fill, "end") == 0,
               "①m 进度回 0 ⇒ 宽 0、九宫格 0 块（实得宽 " + NodeW(fill).ToString("F2")
             + " / " + ImageQuadCount(fill, "end") + " 块）");

            // ---- 重建不许留旧节点 / 不许丢端帽 ----
            win.SetRewardProgress(3, 6);
            int childCount = fill.childCount;
            Ok(childCount == 2,
               "①n ★★ `Fill` 的子件恒 = **2**（`Image` 九宫格 + `end` 端帽；实得 " + childCount
             + "）—— 🧨 重建时忘了销毁旧的 ⇒ 越刷越多；丢了端帽 ⇒ 变 1");
            var last = fill.childCount > 0 ? fill.GetChild(fill.childCount - 1) : null;
            Ok(last != null && last.name == "end",
               "①o ★ `end` 永远是**最后一个子件**（画在填充之上；实得 `" + (last != null ? last.name : "<无>") + "`）");

            // ---- 重建之后贴图 / tint 不许被换掉（重建走的是同一条 `Tex(ArtBarFill, …)`）----
            var q0 = FirstQuad(fill, "end");
            Ok(q0 != null && q0.Texture != null && q0.Texture.name == WantFillSprite,
               "①p ★★ 重建后填充用的是 `" + WantFillSprite + "`（实得 `"
             + (q0 != null && q0.Texture != null ? q0.Texture.name : "<null>") + "`）");
        }
        finally
        {
            GachaEventWindow.RewardsProvider = prevProvider;
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            else { try { UnityEngine.Object.DestroyImmediate(go); } catch { } }
        }
    }

    // ============================================================ ② Draft 结算页版面
    // 原版字面量（`menu_dump bundle_menus_assets_all "Draft Mode Ongoing State" --depth 6 --relative` 现读，
    //   ⇲ls=1.06 那一列 = **视觉值**，即画出来的那一份）：
    //   Win Marks                (697.9,373.8)–(1471.6,708.8)   773.73×335.03
    //   └ Background Back        (722.7,373.8)–(1456.7,719.2)   **734.01×345.42**
    //     └ Background           (732.6,393.1)–(1447.1,699.2)   **714.47×306.09**
    //   Stages Container         (746.6,408.8)–(1425.1,683.5)   678.49×274.67 · Grid cell **84.80²**
    //   Losses container         (825.4,728.1)–(1072.5,812.4)   247.11×84.35  · HLG 格 **71.80²** · 首格 x1 = 825.4
    //   └ Fail                   (827.4,736.3)–(895.2,804.2)   **67.84²**
    //   Stage Info UI_ref > Completed  (1042.3,493.4)–(1129.4,607.0) **87.13×113.60** · 中心比格心低 **4.0**
    //   ⛔ 这些数字**不许**从 `StageCell` / `LossCell` / `BackgroundBackR` 反算 —— 那正是改前那个错（÷1.06）。
    static void RunDraftLayout()
    {
        Debug.Log(P + "--- ② `DraftModeWindow` 结算页：内层 5.7% 偏差（设计值 ÷1.06 混进视觉值）---");

        DraftModeWindow win = null;
        try
        {
            var wm = WindowsManager.Get();
            if (!Ok(wm != null, "②-前提：拿得到 `WindowsManager`")) return;
            win = DraftModeWindow.Create(wm, 20261031);
            if (!Ok(win != null, "②-前提：`DraftModeWindow.Create` 建出窗")) return;

            var marks = Deep(win.transform, "Win Marks");
            Ok(marks != null, "②a 前提：`Win Marks` 在（结算页那棵树建出来了）");
            if (marks == null) return;

            // ---- 背景两层 ----
            var bgBack = Child(marks, "Background Back");
            var bgQuad = bgBack != null ? Child(bgBack, "Background") : null;
            float x1, y1, x2, y2;
            bool r1 = NodeRect(bgBack, out x1, out y1, out x2, out y2);
            Ok(r1 && Near(x2 - x1, 734.01f) && Near(y2 - y1, 345.42f),
               "②b ★★ `Background Back` = **734.01×345.42**（实得 " + (r1 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 🧨 改回 ÷1.06 那组（692.46×325.86）⇒ 红");
            Ok(r1 && Near(x1, 722.70f) && Near(y1, 373.80f),
               "②c ★ `Background Back` 左上角 = (722.70, 373.80)（实得 " + (r1 ? "(" + x1.ToString("F2") + ", " + y1.ToString("F2") + ")" : "<量不到>")
             + "）—— 尺寸对而位置仍偏 = 只改了一半");
            var bq = bgQuad != null ? bgQuad.GetComponent<ImageQuad>() : null;
            bool r2 = QuadRect(bq, out x1, out y1, out x2, out y2);
            Ok(r2 && Near(x2 - x1, 714.47f) && Near(y2 - y1, 306.09f),
               "②d ★★ `Background Back > Background` = **714.47×306.09**（实得 " + (r2 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 🧨 改回 ÷1.06（674.03×288.76）⇒ 红");
            Ok(r2 && Near(x1, 732.60f) && Near(y1, 393.10f),
               "②e ★ `Background` 左上角 = (732.60, 393.10)（实得 " + (r2 ? "(" + x1.ToString("F2") + ", " + y1.ToString("F2") + ")" : "<量不到>") + "）");

            // ---- 胜场格 84.80²（原版 Grid cellSize 80 × 1.06）----
            var winCell = Child(marks, "Stage Info UI_ref");
            bool r3 = NodeRect(winCell, out x1, out y1, out x2, out y2);
            Ok(r3 && Near(x2 - x1, 84.80f) && Near(y2 - y1, 84.80f),
               "②f ★★ 胜场格 `Stage Info UI_ref` = **84.80²**（实得 " + (r3 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 原版 `GridLayoutGroup.cellSize = 80` × 1.06；🧨 用回 80 ⇒ 红");

            // ---- 胜场亮格 87.13×113.60 · 中心比格心低 4.0 ----
            var completed = winCell != null ? Child(winCell, "Mark") : null;
            var cq = completed != null ? completed.GetComponent<ImageQuad>() : null;
            bool r4 = QuadRect(cq, out x1, out y1, out x2, out y2);
            float cellCx = r3 ? (NodeRectCx(winCell)) : 0f, cellCy = r3 ? (NodeRectCy(winCell)) : 0f;
            Ok(r4 && Near(x2 - x1, 87.13f) && Near(y2 - y1, 87.13f),
               "②g ★ 胜场亮格 `Completed` **画心** = 87.13×87.13（实得 " + (r4 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 原版那颗 `m_PreserveAspect=1` + `Rank Skull` 128² ⇒ **画心必为方**；**RT 框**才是 87.13×113.60"
             + "（D2 订正：原断言把「框」当成了 `QuadRectPx` 的期望）；🧨 改回 ÷1.06（82.2×82.2）⇒ 红");
            Ok(r4 && r3 && Near((y1 + y2) * 0.5f - cellCy, 4.0f) && Near((x1 + x2) * 0.5f, cellCx),
               "②h ★ `Completed` 中心比格心低 **4.0**、x 与格心齐（实得 dy = "
             + (r4 && r3 ? ((y1 + y2) * 0.5f - cellCy).ToString("F2") : "<量不到>") + "）");

            // ---- 败场三格 71.80² · 首格 x1 = 825.4 · 亮格 67.84² ----
            var losses = Deep(win.transform, "Losses container");
            var lossCell = losses != null ? Child(losses, "Losses Info UI_ref") : null;
            bool r5 = NodeRect(lossCell, out x1, out y1, out x2, out y2);
            Ok(r5 && Near(x2 - x1, 71.80f) && Near(y2 - y1, 71.80f),
               "②i ★★ 败场格 `Losses Info UI_ref` = **71.80²**（实得 " + (r5 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 🧨 用回 67.74 ⇒ 红");
            Ok(r5 && Near(x1, 825.40f),
               "②j ★ 首格左缘 = **825.40**（实得 " + (r5 ? x1.ToString("F2") : "<量不到>") + "）—— 原版 HLG `align = MiddleLeft`，格宽一变起点就漂");
            var fail = lossCell != null ? Child(lossCell, "Mark") : null;
            var fq = fail != null ? fail.GetComponent<ImageQuad>() : null;
            bool r6 = QuadRect(fq, out x1, out y1, out x2, out y2);
            Ok(r6 && Near(x2 - x1, 67.84f) && Near(y2 - y1, 67.84f),
               "②k ★ 败场亮格 `Fail` = **67.84²**（实得 " + (r6 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 每边内缩 2.0；🧨 用回 1.89 ⇒ 红");

            // ---- 灭自证：这一页**不许**再混进 ÷1.06 那一套（拿设计值当期望会同时「绿」两条）----
            bool r7 = NodeRect(marks, out x1, out y1, out x2, out y2);
            Ok(r7 && Near(x2 - x1, 773.73f) && Near(y2 - y1, 335.03f),
               "②l ★ 容器 `Win Marks` 自身 = **773.73×335.03**（实得 " + (r7 ? Fmt(x2 - x1, y2 - y1) : "<量不到>")
             + "）—— 它一直是视觉值；本条钉住「外层没被顺手改小」");
        }
        finally
        {
            if (win != null) { try { UnityEngine.Object.DestroyImmediate(win.gameObject); } catch { } }
            try { DraftModeWindow.ClearRunForTest(); } catch { }
            try { WindowsManager.ClearReuseCacheForTest(); } catch { }
        }
    }

    // ============================================================ 量法 / 小工具
    static bool Near(float a, float b) { return Mathf.Abs(a - b) < 0.6f; }
    static string Fmt(float w, float h) { return w.ToString("F2") + "×" + h.ToString("F2"); }

    static Transform Child(Transform t, string name) { return t != null ? t.Find(name) : null; }

    static Transform Deep(Transform t, string name)
    {
        if (t == null) return null;
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++)
        {
            var r = Deep(t.GetChild(i), name);
            if (r != null) return r;
        }
        return null;
    }

    /// <summary>节点自己的设计宽（px）= `RectTransform.sizeDelta × 108`（`MenuDraw.SetPxSize` 的反换算 ——
    /// 本仓的节点锚点恒 `(0.5,0.5)` 重合 ⇒ `rect` 只由 `sizeDelta` 决定）。</summary>
    static float NodeW(Transform t)
    {
        var rt = t as RectTransform;
        return rt != null ? rt.sizeDelta.x * 108f : 0f;
    }

    /// <summary>节点自己的矩形（px，左上原点）。**尺寸项取 `rect.rect`、不取 `sizeDelta`**
    /// （拉伸锚点下 `sizeDelta` 表达的是「相对父框的内缩量」）；位置项走全仓唯一那一份
    /// `PixelOfDesign(PosInDesignSpace(t))`（= `MenuDraw.Local` 的逆）。
    /// ⚠️ 不乘节点自己的 `localScale` —— 本组这几个容器/格子都没有 `localScale`（原版也没有）。</summary>
    static bool NodeRect(Transform t, out float x1, out float y1, out float x2, out float y2)
    {
        x1 = y1 = x2 = y2 = 0f;
        var rt = t as RectTransform;
        if (rt == null) return false;
        var r = rt.rect;
        Vector2 c = MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(rt));
        const float K = 108f;      // = `LayoutSpace.DesignPxH / DesignHeight`
        float hw = r.width * 0.5f * K, hh = r.height * 0.5f * K;
        x1 = c.x - hw; y1 = c.y - hh; x2 = c.x + hw; y2 = c.y + hh;
        return true;
    }

    static float NodeRectCx(Transform t) { float a, b, c, d; NodeRect(t, out a, out b, out c, out d); return (a + c) * 0.5f; }
    static float NodeRectCy(Transform t) { float a, b, c, d; NodeRect(t, out a, out b, out c, out d); return (b + d) * 0.5f; }

    /// <summary>一块 `ImageQuad` 的画面矩形（px，左上原点）= `MenuDraw.QuadRectPx`（全仓唯一那一份量法）。</summary>
    static bool QuadRect(ImageQuad q, out float x1, out float y1, out float x2, out float y2)
    {
        return MenuDraw.QuadRectPx(q, out x1, out y1, out x2, out y2);
    }

    /// <summary>`Fill` 下**除端帽 `end` 之外**全部 `ImageQuad` 的并集（九宫格必须用并集量 —— 只取第一块会量成某个角块）。</summary>
    static bool FillUnion(Transform fill, out PxRect r)
    {
        float x1, y1, x2, y2;
        bool ok = MenuDraw.UnionQuadRectPx(fill, MenuDraw.QuadGate.InHierarchy, true,
                                           q => q.transform.name != "end",
                                           out x1, out y1, out x2, out y2);
        r = new PxRect(x1, y1, x2, y2);
        return ok;
    }

    static int ImageQuadCount(Transform t, string excludeName)
    {
        if (t == null) return 0;
        int n = 0;
        foreach (var q in t.GetComponentsInChildren<ImageQuad>(true))
            if (q != null && q.transform.name != excludeName) n++;
        return n;
    }

    static ImageQuad FirstQuad(Transform t, string excludeName)
    {
        if (t == null) return null;
        foreach (var q in t.GetComponentsInChildren<ImageQuad>(true))
            if (q != null && q.transform.name != excludeName) return q;
        return null;
    }
}
