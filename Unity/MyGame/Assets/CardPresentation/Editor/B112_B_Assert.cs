// B112_B_Assert.cs —— 执行代理 `B112_B`（2026-11-01）：`B-81` / `B-82` / `B-83` 三条的断言
//
// ============================ 每段断的是什么 / 灭自证在哪 ============================
//  A `B-81` **可交互价签真的建出来了**（走真 `OfferContainer.Build`，不是只调 `AttachPriceButton`）
//     · A1 ★  价签命中区在（`…/Price Display Button/Generic UI Button/Hit` 上挂着 `WindowButton`）
//     · A1b★  它的 `Interactable` 初值 = `true`（原版 69 份 `PriceDisplayButton.button.m_Interactable` 67/69 = 1）
//     · A1c★★ **命中区渲染队列严格大于整卡 `raycast target`** —— `PointerLayer` 取队列最高者 ⇒
//             价签若 ≤ 整卡，就**永远点不到**（结构性：把 `QoHit` 抬到价签之上 ⇒ 本条红）
//     · A1d   命中区矩形 = 原版 `g.Price`（判据 = `Generic UI Button` 那块矩形，19 份逐份实读进 `Variants`)
//  B `B-81` **到期回调真的把那容器自己的那一颗置灰**（端到端：`Build` → `LiveOpsScheduler.Tick` → 读节点）
//     · B1 ★★ 到期 ⇒ 那一颗 `WindowButton.Interactable == false`
//     · B2 ★  持有量 = 3 ⇒ **不置灰**（灭自证配对：两条一起才分得开「无脑置灰」）
//     · B3 ★  再 `Interactable = true` 能还原（否则「只会变灰」也能骗过 B1）
//  C `B-82` **`LiveOpsUtility` 的两个成员在同一个类名上**（反射 + 行为）
//     · C5/C6/C7 三条咬死原版那三格：严格大于现在 · 取最小那一档 · 一条都没有 ⇒ **0**（⛔ 不回落 endDate）
//  D `B-83` **`ActionScheduler` 收口到 `LiveOpsScheduler`**
//     · D1 ★★ 句柄类型 = `LiveOpsScheduledRefresh`（旧替身返回 int ⇒ 改回去本条红）
//     · D2 ★★ `ResetEventTab` ⇒ 全局队列**真的少一条**（旧替身 `Release` 只出声 ⇒ 改回去本条红）
//     · D3 ★★ 到点 ⇒ **回调真的执行**（旧替身「只排不跑」⇒ 改回去本条红）
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  在 `CardPresentation/Editor/ShopScene.cs`（宿主 = `ShopScene.Run`）里**同一物理行内追加**
//  （与 `B111S3_Assert.Run(); …` 同族 —— ⛔ 别新起一行，那一行本身就是既有账落点）：
//      B112B_Assert.Run(); pass += B112B_Assert.Pass; fail += B112B_Assert.Fail;
//  本件**不建资产、不存场景**；只 `new GameObject` 当父件（`Editor/B111_S3_Assert.cs` 同形）。

using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B112_B`：B-81 可交互价签 · B-82 `LiveOpsUtility` 另两个成员 · B-83 `ActionScheduler` 收口。</summary>
public static class B112B_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B112B] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    const int QBASE = 3000;

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B112_B：B-81 可交互价签 · B-82 `LiveOpsUtility` 另两个成员 · B-83 `ActionScheduler` 收口");
        SectionA_价签建出来了();
        SectionB_到期置灰();
        SectionC_LiveOpsUtility两个成员();
        SectionD_收口();
    }

    // ============================================================ A：B-81 命中区 / 组件 / 队列

    static void SectionA_价签建出来了()
    {
        MenuCheck.Section(_sink, "A B-81：`OfferContainer.Build` 真的建出可交互价签（组件 + 命中区 + 队列压过整卡）");
        OfferContainer.ResetTimerForTest();
        LiveOpsScheduler.ResetForTest();
        var go = new GameObject("B112B_OfferA");
        try
        {
            OfferContainer.TimerDeadline = null;                       // ← 本机的常态（没有服务端报价）
            var built = default(OfferContainer.Built);
            bool ok = false;
            try { built = OfferContainer.Build(go.transform, OfferContainer.Variants[0], -9000f, -9000f,
                                               OfferContainer.Content.Def(), QBASE, null, false); ok = true; }
            catch (Exception e) { Tr(false, "A0 前提：`OfferContainer.Build` 不能抛（实际抛了 " + e.GetType().Name + "：" + e.Message + "）"); }
            Tr(ok && built.Root != null, "A0b 前提：根节点建出来了");
            if (!ok || built.Root == null) return;

            var box = Path(built.Root, "background/name-bg/Price Display Button/Generic UI Button");
            Tr(box != null, "A1a 前提：`…/Price Display Button/Generic UI Button` 在（`BuildPrice` 建的）");
            var hit = box != null ? box.Find("Hit") : null;
            Tr(hit != null, "A1 ★ 价签命中区在（`Generic UI Button/Hit` —— 原版那颗 `EverguildButton` 是可射线件）");
            var wb = hit != null ? hit.GetComponent<WindowButton>() : null;
            Tr(wb != null, "A1b ★ 命中区上挂着 `WindowButton`（= 原版 `EverguildButton` 的等价物）");
            Tr(wb != null && wb.Interactable, "A1c ★ 初值 `Interactable = true`（原版 70 颗 `PriceDisplayButton.button.m_Interactable` = 68/70 为 1）");

            // ---- 队列：价签必须压过整卡那颗 ----
            var rayHit = built.Root.Find("raycast target");
            var priceQ = QuadOf(hit);
            var cardQ = QuadOf(rayHit);
            Tr(priceQ != null && cardQ != null, "A1d 前提：两张命中 quad 都读得到（`Hit` 子件）");
            Tr(priceQ != null && cardQ != null && priceQ.RenderQueue > cardQ.RenderQueue,
               "A1e ★★ 价签命中队列 **严格大于** 整卡 `raycast target`（实际 " + (priceQ != null ? priceQ.RenderQueue : -1)
             + " vs " + (cardQ != null ? cardQ.RenderQueue : -1) + "）—— `PointerLayer` 取最高者，反过来就永远点不到"
             + "｜🧨 把 `QoPriceHit` 降到 ≤ `QoHit` ⇒ 本条红");

            // ---- 矩形 = 原版 `g.Price`（判据来自 19 份 prefab 逐份实读，不是我们编的）----
            // ⚠️ `WorldW/H` 是**世界单位**，`pr.W/H` 是**设计 px**（`LayoutSpace.Px` = /108）⇒ 比之前必须乘回 108。
            var pr = OfferContainer.Variants[0].G.Price;
            float pw = priceQ != null ? priceQ.WorldW * DeckRuntime.PxPerUnit : -1f;
            float ph = priceQ != null ? priceQ.WorldH * DeckRuntime.PxPerUnit : -1f;
            Tr(priceQ != null && Mathf.Abs(pw - pr.W) < 0.5f && Mathf.Abs(ph - pr.H) < 0.5f,
               "A1f 命中区矩形 = 原版 `g.Price`（" + pr.W + "×" + pr.H + "；命中区实际 " + pw + "×" + ph + " px）");
        }
        finally
        {
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ B：B-81 到期回调（端到端）

    static void SectionB_到期置灰()
    {
        MenuCheck.Section(_sink, "B B-81：到期回调真的把**这一容器自己的**那一颗置灰（`Build` → `Tick` → 读节点）");
        var go = new GameObject("B112B_OfferB");
        var pinned = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        try
        {
            // ---------- B1：持有量 0 ⇒ 置灰 ----------
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            LiveOpsClock.NowProvider = () => pinned;
            OfferContainer.TimerDeadline = pinned.AddMinutes(30);
            OfferContainer.OwnedQuantitySource = () => 0;
            var built1 = OfferContainer.Build(go.transform, OfferContainer.Variants[0], -9000f, -9000f,
                                              OfferContainer.Content.Def(), QBASE, null, false);
            var wb1 = PriceButton(built1.Root);
            Tr(wb1 != null, "B1a 前提：这一颗容器建出了价签命中区");
            Tr(wb1 != null && wb1.Interactable, "B1b 前提：起始仍是可交互");
            var fired = LiveOpsScheduler.Tick(pinned.AddHours(2));
            C(fired, 1, "B1c 前提：排上的那一条真的弹了（`LiveOpsScheduler.Tick` 返回 1）");
            Tr(wb1 != null && !wb1.Interactable,
               "B1 ★★ 到期 ⇒ **这一颗**价签被置灰（原版 `ShopOfferContainer.OnOfferTimerEnded` 最后一句）"
             + "｜🧨 删掉 `Build` 里 `ScheduleTimerEnd(…, pb)` 的第二个实参 ⇒ 本条红（只剩静态注入口）");

            // ---------- B4：灰必须落在**可见的** `Button Gfx` 上（防「只建了组件、没绑图」）----------
            var gfx1t = Path(built1.Root, "background/name-bg/Price Display Button/Generic UI Button/Button Gfx");
            var gfx1 = gfx1t != null ? gfx1t.GetComponent<ImageQuad>() : null;
            C(ShaderOf(gfx1), WindowButton.GrayShaderName,
               "B4 ★★ 变灰真的作用在**可见的 `Button Gfx`** 上（原版那颗 `EverguildButton` 的 `colorTintGreyOnDisable = 1`）"
             + "｜🧨 删掉 `AttachPriceButton` 里的 `wb.Bind(…)` ⇒ 本条红（命中区是另建的一颗，子树里只有全透明的命中 quad）");

            // ---------- B3：能还原（防「只会变灰」）----------
            if (wb1 != null) wb1.Interactable = true;
            Tr(wb1 != null && wb1.Interactable, "B3 ★ `Interactable = true` 能还原（否则「只会变灰」也能骗过 B1）");

            // ---------- B2：持有量 3 ⇒ 不置灰（灭自证配对）----------
            UnityEngine.Object.DestroyImmediate(go);
            go = new GameObject("B112B_OfferB2");
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            OfferContainer.TimerDeadline = pinned.AddMinutes(30);
            OfferContainer.OwnedQuantitySource = () => 3;
            var built2 = OfferContainer.Build(go.transform, OfferContainer.Variants[0], -9000f, -9000f,
                                              OfferContainer.Content.Def(), QBASE, null, false);
            var wb2 = PriceButton(built2.Root);
            Tr(wb2 != null && wb2.Interactable, "B2a 前提：第二颗起始可交互");
            LiveOpsScheduler.Tick(pinned.AddHours(2));
            Tr(wb2 != null && wb2.Interactable,
               "B2 ★ 持有量 3 ⇒ **不置灰**（原版 `if (n != 0) return;`）"
             + "｜🧨 删掉 `OnOfferTimerEnded` 里那句 `if (OwnedQuantitySource() != 0) return;` ⇒ 本条红（B1 仍绿 —— 两条一起才分得开）");
        }
        finally
        {
            LiveOpsClock.ResetForTest();
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ C：B-82 `LiveOpsUtility` 的两个成员

    static void SectionC_LiveOpsUtility两个成员()
    {
        MenuCheck.Section(_sink, "C B-82：`LiveOpsUtility.GetDateForEventStage` / `GetDateForNextEventStage` 都在同一个类名上");
        var pinned = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);
        try
        {
            Tr(typeof(LiveOpsUtility).GetMethod("GetDateForEventStage", new[] { typeof(ILiveOps), typeof(int) }) != null,
               "C1 ★ `LiveOpsUtility.GetDateForEventStage(ILiveOps, int)` 在门面上"
             + "｜🧨 删掉 `Shell/LiveOpsUtilityPartial_B112B.cs` ⇒ 本条红");
            Tr(typeof(LiveOpsUtility).GetMethod("GetDateForNextEventStage", new[] { typeof(ILiveOps) }) != null,
               "C2 ★ `LiveOpsUtility.GetDateForNextEventStage(ILiveOps)` 也在同一个类上（此前全仓 0 命中）");

            // ---- 行为面：`GetDateForEventStage` ----
            var d = new LiveOpsEventData
            {
                eventId = "b112b_stage",
                activationType = LiveOpsActivationType.OnDate,
                dates = new LiveOpsEventTimeData { endDate = 999L, eventStages = new List<long> { 0L, 111L, 222L } },
                filter = new LiveOpsEventFilter(),
            };
            var ev = new ProbeEvent(d);
            C(LiveOpsUtility.GetDateForEventStage(ev, 1), 111L, "C3 ★ 阶段 1 ⇒ 该档时刻（111）");
            C(LiveOpsUtility.GetDateForEventStage(ev, 0), 999L,
               "C4 ★ 该档值 = **0** ⇒ 视作「没有这一档」、落到 `dates.endDate`（原版那句 `if (num1 == 0)`）");
            C(LiveOpsUtility.GetDateForEventStage(ev, 9), 999L, "C4b 越界 ⇒ 同样落到 `dates.endDate`");

            // ---- 行为面：`GetDateForNextEventStage` ----
            LiveOpsClock.NowProvider = () => pinned;
            long now = LiveOpsEventTimeData.ToMs(pinned);
            var d2 = new LiveOpsEventData
            {
                eventId = "b112b_next",
                activationType = LiveOpsActivationType.OnDate,
                dates = new LiveOpsEventTimeData
                {
                    endDate = 777777L,                                    // ← 故意给一个「若回落就会露馅」的值
                    eventStages = new List<long> { now - 5000L, now, now + 5000L, now + 1000L, now + 2000L },
                },
                filter = new LiveOpsEventFilter(),
            };
            var ev2 = new ProbeEvent(d2);
            C(LiveOpsUtility.GetDateForNextEventStage(ev2), now + 1000L,
               "C5 ★★ 下一档 = **严格大于现在**的那几档里**最小**的一个（+1000；原版 `Where(s > now).OrderBy(s => s).FirstOrDefault()`）");
            Tr(LiveOpsUtility.GetDateForNextEventStage(ev2) != now,
               "C6 ★ 正好等于「现在」的那一档**不算**下一档（原版是 `>` 不是 `>=`）"
             + "｜🧨 把 `if (s <= now) continue;` 改成 `<` ⇒ 本条红");

            var d3 = new LiveOpsEventData
            {
                eventId = "b112b_next_none",
                activationType = LiveOpsActivationType.OnDate,
                dates = new LiveOpsEventTimeData { endDate = 777777L, eventStages = new List<long> { now - 5000L, now - 1000L } },
                filter = new LiveOpsEventFilter(),
            };
            C(LiveOpsUtility.GetDateForNextEventStage(new ProbeEvent(d3)), 0L,
               "C7 ★★ 一档都还没到 ⇒ **0**（`FirstOrDefault` 的 `default(long)`），⛔ **不回落 `dates.endDate`**"
             + "｜🧨 给它补一个 `?: endDate` 兜底 ⇒ 本条红（这条专门挡「照着 GetDateForEventStage 那一条去『照应』」）");
        }
        finally
        {
            LiveOpsClock.ResetForTest();
        }
    }

    // ============================================================ D：B-83 收口

    static void SectionD_收口()
    {
        MenuCheck.Section(_sink, "D B-83：`ActionScheduler` 收口到 `LiveOpsScheduler`（旧替身「只排不跑 + Release 空操作」都改掉了）");
        LiveOpsScheduler.ResetForTest();
        var go = new GameObject("B112B_GachaSched");
        var win = go.AddComponent<GachaEventWindow>();
        var at = DateTime.UtcNow.AddHours(1);
        try
        {
            C(LiveOpsScheduler.PendingCount, 0, "D0 前提：全局队列是空的");
            win.SetEventTab(new GachaEventWindow.EventTabSource
            {
                LiveOpsId = "b112b-gacha", IsGachaEvent = true,
                CurrentOffer = new object(), ScheduledServerTime = at,
            });
            Tr(win.EventTabScheduledHandle is LiveOpsScheduledRefresh,
               "D1 ★★ 句柄类型 = `LiveOpsScheduledRefresh`（= 真的走了 `LiveOpsScheduler`；旧替身回的是 int）"
             + "｜🧨 把 `SetEventTab` 改回 `LocalActionScheduler.Schedule` ⇒ 本条红");
            C(LiveOpsScheduler.PendingCount, 1, "D1b 全局队列 1 条");

            var logs = Capture(() => LiveOpsScheduler.Tick(at.AddMinutes(1)));
            Tr(Has(logs, "到点"),
               "D3 ★★ 到点 ⇒ **回调真的被执行**（`EventTabTimerEnded` 出声）—— 旧替身「只排、不执行任何动作」"
             + "｜🧨 改回 `LocalActionScheduler` ⇒ 本条红");

            LiveOpsScheduler.ResetForTest();
            win.SetEventTab(new GachaEventWindow.EventTabSource
            {
                LiveOpsId = "b112b-gacha2", IsGachaEvent = true,
                CurrentOffer = new object(), ScheduledServerTime = at,
            });
            C(LiveOpsScheduler.PendingCount, 1, "D2a 前提：又排上 1 条");
            win.ResetEventTab();
            C(LiveOpsScheduler.PendingCount, 0, "D2 ★★ `ResetEventTab` ⇒ 队列**真的少一条**（原版 `ActionScheduler.Release` = `List.Remove`）｜🧨 把 `Release` 改回旧替身那种「只出声、不删」⇒ 本条红");
        }
        finally
        {
            LiveOpsScheduler.ResetForTest();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ 夹具 / 小工具

    /// <summary>按 `/` 分段找**直系**孩子（`Transform.Find` 只找直接子件 —— 本工程踩过）。</summary>
    static Transform Path(Transform root, string p)
    {
        if (root == null) return null;
        var parts = p.Split('/');
        var t = root;
        for (int i = 0; i < parts.Length && t != null; i++) t = t.Find(parts[i]);
        return t;
    }

    /// <summary>`MenuDraw.Hit` 建的命中区里那颗 quad（它是命中节点下**第一个**子件，名字 `Hit`）。</summary>
    static ImageQuad QuadOf(Transform hitNode)
    {
        if (hitNode == null) return null;
        var q = hitNode.GetComponent<ImageQuad>();
        if (q != null) return q;
        var c = hitNode.Find("Hit");
        return c != null ? c.GetComponent<ImageQuad>() : null;
    }

    /// <summary>从一棵容器根读「价签那颗 `WindowButton`」（`…/Price Display Button/Generic UI Button/Hit`）。</summary>
    static WindowButton PriceButton(Transform root)
    {
        var hit = Path(root, "background/name-bg/Price Display Button/Generic UI Button/Hit");
        return hit != null ? hit.GetComponent<WindowButton>() : null;
    }

    /// <summary>这一颗 quad 现在**画在什么 shader 上**（原版灰化的判据 = 那张 shader 的名字；同 `Editor/B97W15_Assert.cs`）。</summary>
    static string ShaderOf(ImageQuad q)
    {
        if (q == null) return null;
        var mr = q.GetComponent<MeshRenderer>();
        var m = mr != null ? mr.sharedMaterial : null;
        return m != null && m.shader != null ? m.shader.name : null;
    }

    /// <summary>最小的 `ILiveOps` 载体（同 `Editor/B111_S3_Assert.cs` 的 `ProbeEvent`；走原版 `LoadEventData` 那一跳）。</summary>
    sealed class ProbeEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
    {
        public ProbeEvent(LiveOpsEventData d) { LoadEventData(d, new LiveOpsSave(), null); }
    }

    /// <summary>收一段 `Debug` 输出（⛔ 不用 `Debug.unityLogger.logEnabled` 关日志 —— 别的断言在等着看）。</summary>
    static List<string> Capture(Action act)
    {
        var logs = new List<string>();
        Application.LogCallback h = (msg, stack, type) => { logs.Add(msg ?? ""); };
        Application.logMessageReceived += h;
        try { act(); }
        finally { Application.logMessageReceived -= h; }
        return logs;
    }

    static bool Has(List<string> logs, string needle)
    {
        for (int i = 0; i < logs.Count; i++)
            if (logs[i] != null && logs[i].IndexOf(needle, StringComparison.Ordinal) >= 0) return true;
        return false;
    }
}
