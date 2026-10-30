// B102HOVER_Assert.cs — 执行代理 `B102_HOVER`（2026-10-30）的断言：**卡组编辑窗的悬停不许被派发两次**。
// 账条目 = `卡组编辑` 的 C 档「卡组编辑窗的悬停可能被**双派发**（自身 `UpdateButtonHover` × `PointerLayer`）」；
// 普查出处 = `资料/完全复刻/判据/AUD04_interactions.tsv`；线索 = `Deck/DeckRuntime.cs` 的 `Build()` 末尾那条 A364 订正。
//
// 用法（宿主里加两行，想并进总数就带后一行）：
//
//     B102HOVER_Assert.Run(win);          // `win` = 已经 `Open()` 过、`Runtime` 建好按钮的 `DeckEditingWindow`
//     pass += B102HOVER_Assert.Pass; fail += B102HOVER_Assert.Fail;
//
// 也可以传 `(DeckRuntime rt, Transform winRoot)` 那一档（宿主手上只有 `rt` 时用）。
//
// ==================================================================
//  期望值从哪来（⛔ 不许拿我们自己的常量证明我们自己的实现）
// ------------------------------------------------------------------
//   · **「只有一条派发链」** = 原版只有一个 UGUI `EventSystem`（`EventSystem.cs` 的 `Update` →
//     `StandaloneInputModule.ProcessMouseEvent` → `HandlePointerExitAndEnter`）——
//     `DeckEditingWindow` 那棵树在原版里就是普通 UGUI，**没有任何第二条派发路**。
//     ⇒ 同一颗按钮在**同一帧**被两条链各派一次 = 原版不存在的行为。
//   · **被判的那几颗** = `DeckRuntime.UiHoverKeys()`（`_hoverBtns` 的键）—— 那是**生产代码自己**报的
//     「本类会派发悬停的那几颗」，不是我抄的一份名单。
//   · **命中矩形** = 两条链各自的**生产口**（`DeckRuntime.UiBtnRect` / `PointerLayer.HitBoxForTest`），
//     ⛔ 本文件**不重算**任何矩形（重算 = 改坏实现时断言跟着一起错 = 自证）。
// ==================================================================
//  ⚠️ 「悬停音/描边播两次」这个**具体症状本身不成立**（先证伪，结论写在这里，免得下一个人重查）：
//    `WindowButton.Enter()` / `Exit()` 有 `Hovered` 守卫（`Shell/WindowButton.cs:513` / `:532`）⇒
//    **同一颗同向的第二次调用是空转**，纹理/tint 只换一次；而本工程**没有悬停音**
//    （`onEnter` / `onExit` 在 `DeckRuntime` 里 0 处赋值，`PlayClickSound` 只在 `Click()` 里）。
//  ⇒ 真正的缺陷是**别的三条**（本批修的就是它们）：
//    ① 两条链的**命中判据不同**（`_btns` 区域表 vs quad 矩形）⇒ 边界带里能**同时点亮两颗**；
//    ② 本类无模态/抽屉状态 ⇒ 那边挡住时本类照样点亮；
//    ③ 点击**响两声**（`PointerLayer.Click()` → `WindowButton.Click()` 的 `PlayClickSound()` 排在**空的
//       `onClick` 之前**；`DeckRuntime.HandleButtons` 自己还按 `ClickCueById` 播一次）。
//  ⇒ 所以本断言量的是「**这颗钮到底归哪一条链**」，不是「换了几次图」。
// ==================================================================
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

public static class B102HOVER_Assert
{
    const string P = "[B102HOVER] ";

    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + "   ✓ " + msg); return true; }
        Fail++; Debug.LogError(P + "   ✗ " + msg); return false;
    }

    /// <summary>宿主手上有一扇已经开好的卡组编辑窗时走这一档。</summary>
    public static void Run(DeckEditingWindow win)
    {
        if (win == null) { Pass = 0; Fail = 0; Ok(false, "A0 前提：`DeckEditingWindow` 为空（宿主没开那扇窗）⇒ 后面全部空转"); return; }
        Run(win.Runtime, win.transform);
    }

    /// <summary>跑全部四组。`winRoot` 只用来数「这一窗里有几颗按钮正亮着」。</summary>
    public static void Run(DeckRuntime rt, Transform winRoot)
    {
        Pass = 0; Fail = 0;
        try
        {
            var targets = CollectTargets(rt, winRoot);
            if (targets == null) return;                 // A0 已经红过，别往下跑出一片假红
            RunSingleDispatch(rt, winRoot, targets);     // A1 ★
            RunFalsification(rt, winRoot, targets);      // A2 🧨
        }
        catch (Exception e)
        {
            Ok(false, "断言宿主抛异常（后面的条目没跑完）：" + e);
        }
        finally
        {
            // 收尾：两条链都挪到窗外，别把「亮着」的状态留给后面的宿主
            try { if (rt != null) rt.UiHoverAt(-100000f, -100000f); } catch { }
            try { var pl = PointerLayer.Instance; if (pl != null) pl.HoverAt(-100000f, -100000f); } catch { }
            Debug.Log(P + $"=== B102_HOVER 合计：{Pass} 通过 / {Fail} 失败 ===");
        }
    }

    // ==================================================================
    //  ⓪ 前提（**硬断言** —— 前提不成立就红，⛔ 不许写成 `if (前提) Check(结论)`）
    // ==================================================================
    struct Target { public string key; public WindowButton wb; public Vector2 px; public Vector2 center; }

    static List<Target> CollectTargets(DeckRuntime rt, Transform winRoot)
    {
        if (!Ok(rt != null, "A0 前提：`DeckRuntime` 不为空")) return null;
        if (!Ok(winRoot != null, "A0 前提：窗根不为空")) return null;

        var pl = PointerLayer.Ensure();
        if (!Ok(pl != null, "A0 前提：`PointerLayer` 拿得到实例（`PointerLayer.Ensure()`）")) return null;
        PointerLayer.SetInputEnabled(true);              // 别的宿主可能关过输入层（B94B5 / B97_W2 都有这一句）
        if (!Ok(!PointerLayer.InputBlocked, "A0 前提：输入层没被黑场/转圈件挡着（`InputBlocked == false`）")) return null;

        var keys = rt.UiHoverKeys();
        if (!Ok(keys != null && keys.Count > 0, "A0 前提：本窗登记了悬停钮（`UiHoverKeys().Count > 0`，现数 = " + (keys != null ? keys.Count : -1) + "）")) return null;

        // 每颗 hover 钮：先由 **DeckRuntime 自己的生产口** 认出它（`UiHoverAt` 走 `UpdateButtonHover`）
        var list = new List<Target>();
        var missed = new List<string>();
        for (int i = 0; i < keys.Count; i++)
        {
            float x, y, w, h;
            if (!rt.UiBtnRect(keys[i], out x, out y, out w, out h)) { missed.Add(keys[i] + "(无矩形)"); continue; }
            var wb = rt.UiHoverAt(x + w * 0.5f, y + h * 0.5f);
            if (wb == null) { missed.Add(keys[i] + "(本类没认) "); continue; }

            Vector2 c, half; ImageQuad quad;
            if (!PointerLayer.HitBoxForTest(wb, out c, out half, out quad))
            {
                // 这**不是**缺陷：`HitBoxForTest == false` 只有三个来源（见 `PointerLayer` 那节的类注），
                // 最常见的是「这颗今天不可命中」⇒ 指针层根本走不到它 ⇒ 对 A1 无意义，跳过但**记下来**。
                Debug.Log(P + "   · 跳过 `" + keys[i] + "`：指针层量不到它的命中区（`HitBoxForTest == false`）⇒ 这条链本来也碰不到它");
                missed.Add(keys[i] + "(指针层够不着)");
                continue;
            }
            list.Add(new Target { key = keys[i], wb = wb, px = new Vector2(x + w * 0.5f, y + h * 0.5f), center = c });
        }

        if (missed.Count > 0)
            Debug.Log(P + "   · 没进 A1 的键：" + string.Join("、", missed.ToArray()));

        // 🔴 **这条是「A1 会不会空转」的唯一闸**：一颗都够不着 ⇒ A1 恒绿而毫无意义
        if (!Ok(list.Count > 0, "A0 前提：**至少一颗**悬停钮在指针层的命中范围里（否则 A1 是空转绿）。现数 = " + list.Count))
            return null;

        // 让位表必须**建钮时就登记好了**（`DeckRuntime.Hover` / `HoverTint` 里那一句）
        Ok(PointerLayer.YieldToHostCountForTest >= keys.Count,
           "A0 前提：让位表登记数 (" + PointerLayer.YieldToHostCountForTest + ") ≥ `UiHoverKeys().Count` (" + keys.Count + ")");

        return list;
    }

    // ==================================================================
    //  A1 ★：同一颗钮**只归一条链** —— 指针层不再碰它，而本窗自己认得出
    // ==================================================================
    static void RunSingleDispatch(DeckRuntime rt, Transform winRoot, List<Target> targets)
    {
        var pl = PointerLayer.Ensure();
        // 起手先把两条链都挪到窗外 —— `CollectTargets` 已经把本窗那一颗点亮了，
        // 而指针层的 `_hover` 可能停在上一宿主留下的某颗上（不清就是**两条链各亮一颗**的假红）
        rt.UiHoverAt(-100000f, -100000f);
        pl.HoverAt(-100000f, -100000f);
        int bad = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            var t = targets[i];

            // ① DeckRuntime 那条链：喂**它自己的**设计 px 中心 —— 必须认得出这一颗
            var mine = rt.UiHoverAt(t.px.x, t.px.y);
            bool okMine = Ok(mine == t.wb,
                "A1★ `" + t.key + "`：本窗自己认得出（`UiHoverAt` = " + NameOf(mine) + "，期望 `" + t.key + "`）");

            // ② PointerLayer 那条链：喂**它自己的**命中区中心 —— 必须**不再**返回这一颗
            var theirs = pl.HoverAt(t.center.x, t.center.y);
            bool okTheirs = Ok(theirs != t.wb && pl.HoveredForTest != t.wb,
                "A1★ `" + t.key + "`：指针层**不派发**它（`HoverAt` = " + NameOf(theirs) + "，`HoveredForTest` = " + NameOf(pl.HoveredForTest) + "）");

            // ③ 这一窗里**同时亮着**的钮最多一颗，且就是本窗认出的那一颗
            int lit = CountHovered(winRoot);
            bool okLit = Ok(lit <= 1,
                "A1★ `" + t.key + "`：这一窗里同时亮着的按钮数 = " + lit + "（≤ 1；>1 = 两条链各点亮了一颗）");

            if (!okMine || !okTheirs || !okLit) bad++;
        }

        Ok(bad == 0, "A1★ 合计：被两条链同时派发的悬停钮 = " + bad + " 颗（期望 0）");
    }

    // ==================================================================
    //  A2 🧨 灭自证：把「让位」撤掉 ⇒ 指针层**能**命中它们（证明 A1 不是空转绿，且双派发真实存在过）
    // ==================================================================
    static void RunFalsification(DeckRuntime rt, Transform winRoot, List<Target> targets)
    {
        var pl = PointerLayer.Ensure();

        // ① 撤掉让位 ⇒ 那些钮应当**又**落到指针层的命中里
        PointerLayer.ClearYieldToHostForTest();
        int hits = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            var got = pl.HoverAt(targets[i].center.x, targets[i].center.y);
            if (got == targets[i].wb) hits++;
        }
        // 🧨 **改坏法**：把 `DeckRuntime.Hover` / `HoverTint` 里那句 `PointerLayer.YieldToHost(wb);` 删掉
        //    （或把它挪到 `return wb;` 之后）⇒ 这一条**照样绿**（因为它量的是「撤掉后能命中」），
        //    而 **A1 会红** ⇒ 两条一起才有意义，⛔ 别只留 A1。
        Ok(hits > 0, "A2 🧨 撤掉让位后，指针层能命中 " + hits + "/" + targets.Count + " 颗（>0 = 双派发**真实存在**，A1 不是空转绿）");

        // ② 复原：把每一颗重新登记回去，并验指针层**立刻**不再命中
        pl.HoverAt(-100000f, -100000f);          // 先把 `_hover` 挪开（撤让位那一步可能把它挂在了某颗上）
        for (int i = 0; i < targets.Count; i++) PointerLayer.YieldToHost(targets[i].wb);
        int stillHit = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            var got = pl.HoverAt(targets[i].center.x, targets[i].center.y);
            if (got == targets[i].wb) stillHit++;
        }
        Ok(stillHit == 0, "A2 🧨 复原让位后，指针层命中它们 = " + stillHit + " 颗（期望 0）");
        Ok(PointerLayer.YieldToHostCountForTest >= targets.Count,
           "A2 🧨 让位表复原后登记数 = " + PointerLayer.YieldToHostCountForTest + "（≥ " + targets.Count + "）");
    }

    // ------------------------------------------------------------ 小工具

    static int CountHovered(Transform root)
    {
        if (root == null) return 0;
        int n = 0;
        var all = root.GetComponentsInChildren<WindowButton>(true);
        for (int i = 0; i < all.Length; i++) if (all[i] != null && all[i].Hovered) n++;
        return n;
    }

    static string NameOf(WindowButton b) { return b == null ? "<null>" : ("`" + b.name + "`"); }
}
