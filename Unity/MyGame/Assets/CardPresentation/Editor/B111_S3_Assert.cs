// B111_S3_Assert.cs —— 执行代理 `B111_S3`（2026-11-01）· 账 `B-67` / `B-68`
//
// ============================ 它盯的是什么 ============================
//  B-67：原版 `ShopOfferContainer.OnInitialize` 第 ④ 句 `ActionScheduler.Instance.Schedule(endDate, OnOfferTimerEnded, null)`
//        —— 商品条目「到点」那条链。本仓原来整条没有（`OfferContainer.Build` 只建节点、不排期）。
//        落地 = `Shell/OfferContainerPartial_B111S3.cs` + `Shell/OfferContainer.cs` 的 `Timer` 那一块（逐字同一处）。
//  B-68：原版 `LiveOpsUtility` 的两个成员被拆到两个类（`IsTimedEvent` 在新 `LiveOpsUtility`、`IsTriggeredEvent` 在
//        `LiveOpsManager`）⇒ 门面 = `Shell/LiveOpsUtilityPartial_B111S3.cs`，两个成员现在都在 `LiveOpsUtility.` 上。
//
// ============================ 逐条为什么必须这么断 ============================
//  · `A` 段**真的调 `OfferContainer.Build`**（不是只调 `ScheduleTimerEnd`）—— 只有这样才证明「接线接上了」，
//    只测 `ScheduleTimerEnd` 的话，`Build` 里那一句被删掉照样全绿（本工程 2026-10-30 已经栽过一次
//    「断言文件建了但没人接线」的教训）。
//  · `B` 段断的是**没到期时刻时「不排期 + 出声」两半**：只断「队里没多」会让「静默跳过」也算对
//    （红线：不许静默失败）。
//  · `C4` 是 `C3` 的**灭自证配对**：只断「持有量 0 ⇒ 置灰」的话，把 `if (n != 0) return;` 删掉照样绿；
//    两条一起才分得开。
//  · `D4` 是 `D3` 的**灭自证配对**：只断「门面与原实现逐值一致」的话，两边都恒 `false` 也绿；
//    必须有一条**真的返 true** 的输入。
//
// ============================ 🔴 灭自证（🧨 每条文案里都写了「改哪一处会让它红」）============================
//  · 🧨 删掉 `Shell/OfferContainer.cs` 里 `Build` 那句 `ScheduleTimerEnd(TimerDeadline.Value)` ⇒ `A1` / `A1b` 红。
//  · 🧨 把 `else` 支的 `NoteNoTimerDeadline(v.Prefab)` 删掉 ⇒ `B2` 红。
//  · 🧨 把 `OfferContainer.ScheduleTimerEnd` 里的「已过点」结果当成正常排期（例如自己塞一条立刻执行）⇒ `B3` 红。
//  · 🧨 把 `OnOfferTimerEnded` 里的 `if (OwnedQuantitySource() != 0) return;` 删掉 ⇒ `C4` 红（`C3` 仍绿）。
//  · 🧨 把「没有持有量数据源」那条 `LogWarning` 删掉 ⇒ `C5` 红。
//  · 🧨 把门面写成 `return false;`（或删掉 `Shell/LiveOpsUtilityPartial_B111S3.cs`）⇒ `D2` / `D4` 红。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  与 `B110W4_Assert` 同族 —— 在 `CardPresentation/Editor/ShellScene.cs` 的活动那一段**同一物理行内追加**：
//      B111S3_Assert.Run(); pass += B111S3_Assert.Pass; fail += B111S3_Assert.Fail;
//  本件**不建资产、不存场景**；只 `new GameObject` 当父件（`Editor/ShopScene.cs:3768` 同形）。

using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

/// <summary>执行代理 `B111_S3`：B-67 商品条目到期排期 · B-68 `LiveOpsUtility` 门面。</summary>
public static class B111S3_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B111S3] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool cond, string msg) { MenuCheck.True(_sink, cond, msg); }

    // ============================================================ 入口

    public static void Run()
    {
        MenuCheck.Section(_sink, "B111_S3：B-67 商品条目到期排期（`ActionScheduler`）+ B-68 `LiveOpsUtility` 门面");
        SectionA_有到期时刻();
        SectionB_无到期时刻();
        SectionC_到期回调();
        SectionD_门面();
    }

    // ============================================================ A：有到期时刻 ⇒ Build 真的排上了

    static void SectionA_有到期时刻()
    {
        MenuCheck.Section(_sink, "A B-67：有到期时刻 ⇒ `Build` 真的排上了 `ActionScheduler`");
        LiveOpsScheduler.ResetForTest();
        OfferContainer.ResetTimerForTest();

        var go = new GameObject("B111S3 A");
        try
        {
            var deadline = DateTime.UtcNow.AddHours(1);
            OfferContainer.TimerDeadline = deadline;
            OfferContainer.Content c = OfferContainer.Content.Def();

            OfferContainer.Built b = default(OfferContainer.Built);
            bool built = false;
            string err = null;
            try
            {
                b = OfferContainer.Build(go.transform, OfferContainer.Variants[0], -9000f, -9000f, c, 3000, null, false);
                built = true;
            }
            catch (Exception e) { err = e.GetType().Name + "：" + e.Message; }

            Tr(built, "A0 前提：`OfferContainer.Build` 不抛（" + (err ?? "ok") + "）");
            if (!built) return;

            Tr(b.Root != null, "A0b 前提：容器根节点建出来了");
            Tr(OfferContainer.TimerSchedule != null,
               "A1 ★ 有到期时刻 ⇒ **排期了**（`TimerSchedule` 非 null = 原版 `schedule` +0x88 那一格）"
             + "｜🧨 删掉 `Shell/OfferContainer.cs` 里 `Build` 那句 `ScheduleTimerEnd(TimerDeadline.Value)` ⇒ 本条红");
            C(LiveOpsScheduler.PendingCount, 1,
               "A1b ★ …而且 `ActionScheduler` 的队里**真的多了 1 条**（不是只赋了个句柄）｜🧨 同上");

            var got = OfferContainer.TimerSchedule != null ? OfferContainer.TimerSchedule.DueUtc : DateTime.MinValue;
            double driftMs = Math.Abs((got - deadline.ToUniversalTime()).TotalMilliseconds);
            Tr(OfferContainer.TimerSchedule != null && driftMs < 2.0,
               "A1c ★ 排的时刻 = 传进去的 `endDate`（差 " + driftMs.ToString("F1")
             + " ms，`MillisSinceEpochToDateTime` 的毫秒截断）｜🧨 把 `ScheduleTimerEnd` 里的 `ms` 换成 `DateTime.UtcNow` ⇒ 本条红");
        }
        finally
        {
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ B：无到期时刻 ⇒ 不排期 + 出声

    static void SectionB_无到期时刻()
    {
        MenuCheck.Section(_sink, "B B-67：**没有**到期时刻 ⇒ 明确不排期，而且**出声**");
        LiveOpsScheduler.ResetForTest();
        OfferContainer.ResetTimerForTest();

        var go = new GameObject("B111S3 B");
        try
        {
            OfferContainer.TimerDeadline = null;                 // ← 本机的常态（没有服务端报价）
            OfferContainer.Content c = OfferContainer.Content.Def();

            var logs = Capture(() =>
                OfferContainer.Build(go.transform, OfferContainer.Variants[0], -9000f, -9000f, c, 3000, null, false));

            C(LiveOpsScheduler.PendingCount, 0,
               "B1 ★ 无到期时刻 ⇒ **不排期**（`ActionScheduler` 队里 0 条）"
             + "｜🧨 把 `Build` 的 else 支改成也调 `ScheduleTimerEnd(DateTime.UtcNow)` ⇒ 本条红");
            Tr(OfferContainer.TimerSchedule == null,
               "B1b …句柄仍是 `null`（不是「排了一个永远不会触发的」）");
            Tr(Has(logs, "本轮没有到期时刻"),
               "B2 ★ …而且**出声**了（`NoteNoTimerDeadline`）｜🧨 删掉 `else` 支的 `NoteNoTimerDeadline(v.Prefab)` ⇒ 本条红");

            // B3：到期时刻**已过** —— 原版 `ActionScheduler__Schedule.c:26-30` 那条 `return 0`
            OfferContainer.ResetTimerForTest();
            var logs2 = Capture(() => OfferContainer.ScheduleTimerEnd(DateTime.UtcNow.AddMinutes(-5)));
            Tr(OfferContainer.TimerSchedule == null,
               "B3 ★ 到期时刻**已过** ⇒ 原版 `if (dateTime < ServerTime) return 0;` ⇒ **不排期**"
             + "｜🧨 在 `ScheduleTimerEnd` 里把过点那一支改成照样入队 ⇒ 本条红");
            Tr(Has(logs2, "已过"),
               "B3b …而且**出声**了（⛔ 不静默吞掉）");
        }
        finally
        {
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // ============================================================ C：到期回调 `OnOfferTimerEnded`

    static void SectionC_到期回调()
    {
        MenuCheck.Section(_sink, "C B-67：到点 ⇒ 回调真的被调到（`OnOfferTimerEnded` 原版全文两句）");
        LiveOpsScheduler.ResetForTest();
        OfferContainer.ResetTimerForTest();

        bool? captured = null;
        try
        {
            // ---- C1–C3：持有量 0 ⇒ 置灰 ----
            OfferContainer.OwnedQuantitySource = () => 0;
            OfferContainer.PriceButtonInteractable = v => captured = v;
            var h = OfferContainer.ScheduleTimerEnd(DateTime.UtcNow.AddMinutes(30));
            Tr(h != null, "C1 前提：30 分钟后那一刻排上了");
            if (h != null)
            {
                C(LiveOpsScheduler.Tick(DateTime.UtcNow.AddHours(2)), 1,
                   "C2 ★ 到点 ⇒ `ActionScheduler.Update` 弹了一条（每拍最多一条，原版 `ActionScheduler__Update.c`）");
                Tr(captured.HasValue, "C2b ★ …回调 `OnOfferTimerEnded` 真的跑到了");
                C(captured.HasValue ? captured.Value : true, false,
                   "C3 ★ 持有量 0 ⇒ 价签被置 **false**（原版 `priceDisplayButton.Interactable = false`）"
                 + "｜🧨 把 `PriceButtonInteractable(false)` 改成 `(true)` ⇒ 本条红");
            }

            // ---- C4：灭自证配对 —— 持有量非 0 ⇒ 什么都不做（原版第一句 `if (n != 0) return;`）----
            captured = null;
            OfferContainer.ResetTimerForTest();
            OfferContainer.OwnedQuantitySource = () => 3;
            OfferContainer.PriceButtonInteractable = v => captured = v;
            var h2 = OfferContainer.ScheduleTimerEnd(DateTime.UtcNow.AddMinutes(30));
            Tr(h2 != null, "C4 前提：第二次排上了");
            if (h2 != null)
            {
                LiveOpsScheduler.Tick(DateTime.UtcNow.AddHours(2));
                Tr(!captured.HasValue,
                   "C4 ★ 持有量 3 ⇒ **不置灰**（原版 `if (n != 0) return;`）"
                 + "｜🧨 删掉 `OnOfferTimerEnded` 里 `if (OwnedQuantitySource() != 0) return;` ⇒ 本条红（C3 仍绿 —— 正说明两条分得开）");
            }

            // ---- C5：没有持有量数据源 ⇒ 出声、不假装 ----
            captured = null;
            OfferContainer.ResetTimerForTest();
            var logs = Capture(() => OfferContainer.OnOfferTimerEnded());
            Tr(Has(logs, "没有持有量数据源"),
               "C5 ★ 没有持有量数据源（远端）⇒ **出声**"
             + "｜🧨 删掉 `OnOfferTimerEnded` 里那条 `LogWarning` ⇒ 本条红");
            Tr(!captured.HasValue, "C5b …而且**不假装置灰**（⛔ 不猜一个 0）");
        }
        finally
        {
            OfferContainer.ResetTimerForTest();
            LiveOpsScheduler.ResetForTest();
        }
    }

    // ============================================================ D：`LiveOpsUtility` 门面（B-68）

    static void SectionD_门面()
    {
        MenuCheck.Section(_sink, "D B-68：`LiveOpsUtility` 门面 —— 两个成员都能从**同一个类**访问到");

        Tr(typeof(LiveOpsUtility).GetMethod("IsTimedEvent", new[] { typeof(ILiveOps), typeof(long).MakeByRefType() }) != null,
           "D1 ★ `LiveOpsUtility.IsTimedEvent(ILiveOps, out long)` 在门面上（`Shell/LiveOpsUtilityPartial_B110W4.cs`）");
        Tr(typeof(LiveOpsUtility).GetMethod("IsTriggeredEvent", new[] { typeof(ILiveOps) }) != null,
           "D2 ★ `LiveOpsUtility.IsTriggeredEvent(ILiveOps)` **也在同一个类上**"
         + "｜🧨 删掉 `Shell/LiveOpsUtilityPartial_B111S3.cs` ⇒ 本条红");

        // 行为面（⛔ 别只靠反射：反射只能证明「有个同名方法」，证明不了它转发到了实现）
        var e0 = MakeEvent(0);
        var e1 = MakeEvent(1);
        C(LiveOpsUtility.IsTriggeredEvent(e0), LiveOpsManager.IsTriggeredEvent(e0),
           "D3 …与 `LiveOpsManager.IsTriggeredEvent` 逐值一致（0 条触发器 ⇒ 两边都是 false）");
        C(LiveOpsUtility.IsTriggeredEvent(e1), true,
           "D4 ★ 挂了 1 条触发器 ⇒ 门面返回 **true**（证明它真的转到了实现，不是恒 false）"
         + "｜🧨 把门面写成 `return false;` ⇒ 本条红（D3 仍绿 —— 这正说明 D3 单独不够）");
        C(LiveOpsUtility.IsTriggeredEvent(null), false,
           "D5 `null` ⇒ false（原版那一格 NRE；本仓既有口径 = 出声 + false，见 `LiveOpsManager.IsTriggeredEvent`）");

        // `IsTimedEvent` 也过一遍（同一个门面上的另一个成员，⛔ 别只断反射）
        var data = new LiveOpsEventData
        {
            eventId = "b111s3_probe",
            activationType = LiveOpsActivationType.OnDate,
            dates = new LiveOpsEventTimeData { endDate = 1700000000000L },
            filter = new LiveOpsEventFilter { hasDuration = false },
        };
        long endDate;
        bool timed = LiveOpsUtility.IsTimedEvent(new ProbeEvent(data), out endDate);
        C(timed, true, "D6 ★ 同一个门面上的 `IsTimedEvent` 也能调到（`OnDate` ⇒ 计时活动）");
        C(endDate, 1700000000000L, "D6b …而且读到的就是 `dates.endDate`");
    }

    // ============================================================ 夹具 / 小工具

    /// <summary>最小的 `ILiveOps` 载体（同 `B110_W4_Assert` 的 `ProbeEvent`；走原版 `LoadEventData` 那一跳）。</summary>
    sealed class ProbeEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
    {
        public ProbeEvent(LiveOpsEventData d) { LoadEventData(d, new LiveOpsSave(), null); }
    }

    /// <summary>触发器桩：本段只关心 `Triggers.Count`，不挂总线。</summary>
    sealed class AnyTrigger : LiveOpsEventTrigger
    {
        public override void Register(Action callback) { }
        public override void Unregister() { }
    }

    /// <summary>造一份「触发器 N 条」的事件数据。</summary>
    static ILiveOps MakeEvent(int triggerCount)
    {
        var d = new LiveOpsEventData
        {
            eventId = "b111s3_trigger_" + triggerCount,
            activationType = LiveOpsActivationType.OnDate,
            dates = new LiveOpsEventTimeData { endDate = 1700000000000L },
            filter = new LiveOpsEventFilter(),
        };
        for (int i = 0; i < triggerCount; i++) d.filter.Triggers.Add(new AnyTrigger());
        return new ProbeEvent(d);
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
