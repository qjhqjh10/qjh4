// B98K5_Assert.cs — 执行代理 `B98_K5`（第九十八会话 · 2026-10-29）轮抽线三条 B 的断言
//
// ============================ 它盯的是什么 ============================
// 逐条对应 `资料/完全复刻/账/轮抽.tsv` 的那三行（本轮结论 → `资料/完全复刻/报告/B98_K5.md`）：
//
//   A `轮抽:6`   `DraftEventBase.Expire(Action onComplete)` —— 缺的**不是方法体**，是「活动到期」这个服务端事件时刻。
//      本轮的本地模拟（`Shell/DraftLocalEvent.cs`）**造出了那一格**：活动实例 + 起止时刻 + `expireDate`。
//      A 段验这份数据在（`endMs > startMs`、`expireMs ≥ endMs`）+ 六档状态机真的被它驱动。
//   B 同 A，验 `LiveOpsEventData.GetState` 的**四档**（Planned / Active / Finished / Expired）逐档对得上注入的时刻
//      —— 期望值全是枚举字面量，**不从被测实现里读**。
//   C `轮抽:6`   `Expire` 链**真的跑**：`Tick()` 在 `Finished` 档触发 → `ExpireCount` +1 → `LiveOpsSignals.EventExpired` 广播。
//      （判据 = `LiveOpsHandler.ScheduleEventPopups` 的过期支那句 `GetState(now) == Finished`；
//        回调形状 = `LiveOpsHandler.ExpireEvent` 的 `handleOnComplete`。）
//   D `轮抽:114` `TimedDraftLocal.EnableForDemo` **有生产调用点**了：装模拟 ⇒ `TimedDraftLocal.Enabled` 被置真、
//      `Armies` 来自模拟事件的 `specificArmies`（= 原版 `TimedDraftData.specificArmies[2]` 的替身）。
//   E `轮抽:147` 标题幅面：`DraftLocalEvent.TitleText()` 走的是原版那一跳
//      `LiveOpsAssetUtility.GetEventLabel(eventData, Title)`，拿到的**词条键**与**显示文字**都要对得上。
//   F 边界：⛔ 模拟数据**没有污染**原版卡包表（`Resources/draft_packs.json` 里仍然没有任何到期时刻键）。
//
// ============================ 怎么用（宿主）============================
// 由调度台接线（本代理 ⛔ 不改 `Editor/RuleEngineTest.cs` / `Editor/MainMenuScene.cs`）：
//   · 在 `Editor/MainMenuScene.cs`（轮抽 / 菜单域）的 `Run()` 里，紧接其它 `*_Assert.Run();` 之后加：
//       `B98K5_Assert.Run();`
//       `_sink.Pass += B98K5_Assert.Pass; _sink.Fail += B98K5_Assert.Fail;`
//   · 本文件**不跑 Unity 批处理**、不建持久资产。
//   · 🔴 全局状态：本文件**存/还原** `TimedDraftLocal` 的 7 格 + `LiveOpsClock.NowProvider` + `DraftLocalEvent` 的静态开关，
//     收尾后编辑器里的 `TimedDraftLocal` 与跑本断言之前**完全一致**（否则会点亮别人的窗口断言）。
//     ⛔ 不碰 `DraftModeWindow.RunForTest`（`CurrentDraftState` 的反推**永远不会**给 15 ⇒ `Expire` 的
//     `onComplete` 一定被调到，不需要造存档）。
//
// ============================ 🔴 灭自证（改哪一处会让它红）============================
//   · B1–B4：把 `DraftLocalEvent` 里 `Data.dates` 的 `endDate` / `expireDate` 塞回 0（或把 `activationType`
//         改成 `Permanent` 短路）⇒ 四档全塌 ⇒ B 段红。
//   · C2/C3：把 `Tick()` 里的 `== LiveOpState.Finished` 换成 `== Active`（或不判）⇒ `ExpireCount` 不涨 ⇒ C 段红。
//   · D2/D3：把 `ApplyToTimedLocal()` 里的 `TimedDraftLocal.EnableForDemo(a[0], a[1])` 撤掉 ⇒ D 段红。
//   · D4 / E3：`SimulationEnabled = false` 后重装 ⇒ `Enabled` 不该被点亮、`TitleText()` 该为空 ⇒ 灭自证成立。
//   · E2：把 `Spec.titleTerm` 换成不存在的键 ⇒ `TitleText()` 变空 / 走兜底 ⇒ E2 红。
using System;
using CardPresentation;
using UnityEditor;
using UnityEngine;

/// <summary>执行代理 `B98_K5`：轮抽三条 B（`轮抽:6` / `轮抽:114` / `轮抽:147`）的断言。
/// <para>⛔ 期望值全是字面量（`"Ultramarines"` / `"Demo/DraftMode/SelectWarlord"` / 枚举档位），**不从被测实现里读**。</para></summary>
public static class B98K5_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B98_K5] ") { Near = MenuNearStyle.Compact2 };

    public static int Pass { get { return _sink.Pass; } }
    public static int Fail { get { return _sink.Fail; } }

    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }
    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B98_K5：轮抽:6 到期链 · 轮抽:114 Timed 族生产入口 · 轮抽:147 标题幅面（🔶 本地模拟）");

        // ---- 存档全局状态（收尾必还原）----
        bool sEnabled = TimedDraftLocal.Enabled;
        string[] sArmies = TimedDraftLocal.Armies;
        int[] sTotals = TimedDraftLocal.Totals;
        int sCost = TimedDraftLocal.PremiumCost, sCost10 = TimedDraftLocal.PremiumTenCost;
        string sCur = TimedDraftLocal.PremiumCurrency, sImg = TimedDraftLocal.EventImage;

        try
        {
            SectionA_前提与数据();
            SectionB_六档状态机();
            SectionC_到期链();
            SectionD_Timed族生产入口();
            SectionE_标题幅面();
            SectionF_边界_没污染原版数据();
        }
        finally
        {
            // ---- 还原：先回到「正常装好的状态」，再把我们的副作用抹掉（编辑器里不该点亮 Timed 族）----
            LiveOpsClock.ResetForTest();
            DraftLocalEvent.SimulationEnabled = true;
            DraftLocalEvent.InvalidateCache();
            DraftLocalEvent.EnsureLoaded();
            TimedDraftLocal.Enabled = sEnabled;
            TimedDraftLocal.Armies = sArmies;
            TimedDraftLocal.Totals = sTotals;
            TimedDraftLocal.PremiumCost = sCost; TimedDraftLocal.PremiumTenCost = sCost10;
            TimedDraftLocal.PremiumCurrency = sCur; TimedDraftLocal.EventImage = sImg;
        }
    }

    // ============================================================ A 前提与数据（`轮抽:6` 缺的就是这一格）
    static void SectionA_前提与数据()
    {
        MenuCheck.Section(_sink, "A `轮抽:6` 活动实例 + 到期时刻（本地模拟；原版由服务端下发）");

        DraftLocalEvent.ResetForTest();
        var spec = DraftLocalEvent.Current;
        Tr(spec != null, "A1 前提：模拟数据装得出来（`Resources/draft_local_event.json`）");
        if (spec == null) return;
        C(DraftLocalEvent.SimulationEnabled, true, "A1 前提：模拟开关默认打开（否则三行都回到 B）");
        Tr(!string.IsNullOrEmpty(spec.eventId), "A2 ★ 活动实例有 `eventId` = '" + (spec.eventId ?? "<null>") + "'（原版 `GetID()` 读它）");
        Tr(DraftLocalEvent.Event != null, "A3 ★ 活动实例建出来了（`DraftEventBase<DraftEventSave, DraftEventData>` 的具体子类）");
        C(DraftLocalEvent.Event != null ? DraftLocalEvent.Event.GetID() : null, spec.eventId,
          "A3 ★ `GetID()` = 数据的 `eventId`（原版 `LiveOpsEvent.GetID()` → `Data.eventId`）");
        Tr(DraftLocalEvent.EndMs > DraftLocalEvent.StartMs,
           "A4 ★ **有到期时刻**：endMs(" + DraftLocalEvent.EndMs + ") > startMs(" + DraftLocalEvent.StartMs + ")"
           + " —— 这就是 `轮抽:6` 一直缺的那一格（原版 `Resources/draft_packs.json` 里 0 命中）");
        Tr(DraftLocalEvent.ExpireMs >= DraftLocalEvent.EndMs,
           "A4 `expireDate` ≥ `endDate`（`FinalDate = expirable ? expireDate : endDate` 的前提）");
        C(DraftLocalEvent.Event != null ? DraftLocalEvent.Event.EventPlayMode : PlayModes.Classic, PlayModes.ClosedDeck,
          "A5 ★ `EventPlayMode` = `ClosedDeck`(7)（判据 = `DraftEventBase.get_EventPlayMode` 真体全文 `return 7`）");
    }

    // ============================================================ B 六档状态机（数据驱动）
    static void SectionB_六档状态机()
    {
        MenuCheck.Section(_sink, "B `LiveOpsEventData.GetState` 四档（Planned / Active / Finished / Expired）逐档对得上注入的时刻");

        var boot = DraftLocalEvent.BootUtc;
        try
        {
            // ① 还没开始（start 在 -60 分钟 ⇒ 取 -90 分钟就是「未开始」）
            LiveOpsClock.NowProvider = () => boot.AddMinutes(-90);
            C(DraftLocalEvent.State, LiveOpState.Planned, "B1 ★ 早于 `startDate` ⇒ `Planned`（起手最早也是 Planned，不是 Active）");

            // ② 在期
            LiveOpsClock.NowProvider = () => boot;
            C(DraftLocalEvent.State, LiveOpState.Active, "B2 ★ 在 `[startDate, endDate)` 内 ⇒ `Active`");

            // ③ 过了 endDate、未到 FinalDate（= 原版的「活动到期」那一刻）
            LiveOpsClock.NowProvider = () => boot.AddMinutes(200);
            C(DraftLocalEvent.State, LiveOpState.Finished, "B3 ★ 过了 `endDate`、未到 `FinalDate` ⇒ `Finished`（`Expire` 链的触发档）");

            // ④ 过了 FinalDate
            LiveOpsClock.NowProvider = () => boot.AddMinutes(300);
            C(DraftLocalEvent.State, LiveOpState.Expired, "B4 ★ 过了 `FinalDate` ⇒ `Expired`（原版此时事件已从列表摘掉）");
        }
        finally
        {
            LiveOpsClock.NowProvider = null;      // 还原成真实时间（⛔ 不留注入）
        }
    }

    // ============================================================ C 到期链（`轮抽:6`）
    static void SectionC_到期链()
    {
        MenuCheck.Section(_sink, "C `轮抽:6 DraftEventBase.Expire` —— 到期那一刻真的被人敲了（`Tick` → `Expire` → 广播）");

        DraftLocalEvent.ResetForTest();
        Tr(!DraftLocalEvent.ExpireFired, "C1 前提：复位后还没过期过");
        C(DraftLocalEvent.ExpireCount, 0, "C1 前提：`Expire` 还没被调过");

        string gotId = null;
        Action<string> h = id => { gotId = id; };
        LiveOpsSignals.EventExpired += h;
        try
        {
            // 未到期时 `Tick` 什么都不该做（否则「每秒都过一遍期」——那是另一种静默错）
            LiveOpsClock.NowProvider = () => DraftLocalEvent.BootUtc;
            DraftLocalEvent.Tick();
            C(DraftLocalEvent.ExpireCount, 0, "C2 ★ 没到期时 `Tick()` **不触发**（原版也不会）");

            // 到期档：`Finished` ⇒ 走 `Expire` 链
            LiveOpsClock.NowProvider = () => DraftLocalEvent.BootUtc.AddMinutes(200);
            DraftLocalEvent.Tick();
            C(DraftLocalEvent.ExpireCount, 1, "C3 ★ `Finished` 档 `Tick()` ⇒ `Expire` 被调 1 次");
            Tr(DraftLocalEvent.ExpireFired, "C3 ★ `ExpireFired` 置真（同一场活动不再重复过期）");
            C(gotId, DraftLocalEvent.Current != null ? DraftLocalEvent.Current.eventId : null,
              "C4 ★ `LiveOpsSignals.EventExpired` 广播了本次 eventId（= `ExpireEvent` 的 `handleOnComplete` 形状）");

            // 幂等：再敲两次不该再多调
            DraftLocalEvent.Tick();
            DraftLocalEvent.Tick();
            C(DraftLocalEvent.ExpireCount, 1, "C5 ★ 重复 `Tick()` 不再重复过期（原版事件摘掉后也不会）");
        }
        finally
        {
            LiveOpsSignals.EventExpired -= h;
            LiveOpsClock.NowProvider = null;
        }
    }

    // ============================================================ D Timed 族生产入口（`轮抽:114`）
    static void SectionD_Timed族生产入口()
    {
        MenuCheck.Section(_sink, "D `轮抽:114` `TimedDraftLocal.EnableForDemo` 有生产调用点了（模拟事件装载时点亮）");

        // 先把 Timed 关掉，再装一次模拟 ⇒ 只有「模拟真的调了 EnableForDemo」才会重新点亮
        TimedDraftLocal.Enabled = false;
        TimedDraftLocal.Armies = new[] { "", "" };
        DraftLocalEvent.ResetForTest();

        Tr(DraftLocalEvent.TimedFamilyActive, "D1 前提：模拟事件声明自己是 Timed 变体（`spec.timed`）");
        C(TimedDraftLocal.Enabled, true, "D2 ★ 装载模拟 ⇒ `TimedDraftLocal.Enabled` 被置真（原 `EnableForDemo` 生产 0 调用点）");
        var armies = TimedDraftLocal.Armies;
        C(armies != null ? armies.Length : -1, 2, "D3 ★ `Armies` = 2 格（原版 `TimedDraftData.specificArmies[2]` 的替身）");
        C(armies != null && armies.Length > 0 ? armies[0] : null, "Ultramarines", "D3 ★ 第 1 个阵营来自模拟事件");
        C(armies != null && armies.Length > 1 ? armies[1] : null, "Goff", "D3 ★ 第 2 个阵营来自模拟事件");

        // ---- 灭自证：关掉模拟 ⇒ 不该再点亮 ----
        DraftLocalEvent.SimulationEnabled = false;
        TimedDraftLocal.Enabled = false;
        DraftLocalEvent.InvalidateCache();
        DraftLocalEvent.EnsureLoaded();
        C(TimedDraftLocal.Enabled, false,
          "D4 ★ 灭自证：`SimulationEnabled = false` ⇒ `Enabled` 仍是 false（= player 里 Timed 族永不建的旧状态）");

        // 复位，给 E 段用
        DraftLocalEvent.SimulationEnabled = true;
        DraftLocalEvent.ResetForTest();
        Tr(DraftLocalEvent.Event != null, "D5 前提：模拟重新装回来了（E 段要用）");
    }

    // ============================================================ E 标题幅面（`轮抽:147`）
    static void SectionE_标题幅面()
    {
        MenuCheck.Section(_sink, "E `轮抽:147` 选将页标题 = `LiveOpsAssetUtility.GetEventLabel(eventData, Title)`");

        string text = DraftLocalEvent.TitleText();
        Tr(!string.IsNullOrEmpty(text), "E1 ★ 标题文字取到了：'" + (text ?? "<null>") + "'");
        C(DraftLocalEvent.LastTitleTerm, "Demo/DraftMode/SelectWarlord",
          "E2 ★ 走的是**事件数据里的 Title 词条**（不是页面硬编码）—— 词条键 = 模拟事件给的那一个");
        C(text, Loc.T("Demo/DraftMode/SelectWarlord"),
          "E2 ★ 显示文字 = 该词条在本地表里的值（画面文字未被编）");

        // ---- 灭自证：关掉模拟 ⇒ 取不到词条、也没有兜底 ⇒ 空串（保留页面既有标题）----
        DraftLocalEvent.SimulationEnabled = false;
        DraftLocalEvent.InvalidateCache();
        DraftLocalEvent.EnsureLoaded();
        C(DraftLocalEvent.TitleText(), "",
          "E3 ★ 灭自证：模拟关掉 ⇒ `TitleText()` 为空（⛔ 不编词条、⛔ 不拿别的键顶）");

        DraftLocalEvent.SimulationEnabled = true;
        DraftLocalEvent.ResetForTest();
    }

    // ============================================================ F 边界
    static void SectionF_边界_没污染原版数据()
    {
        MenuCheck.Section(_sink, "F 边界：模拟数据没碰原版卡包表（`Resources/draft_packs.json` 仍无任何到期时刻键）");

        string path = System.IO.Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_packs.json");
        Tr(System.IO.File.Exists(path), "F1 前提：`Resources/draft_packs.json` 在盘上");
        if (!System.IO.File.Exists(path)) return;
        string text;
        using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
        using (var sr = new System.IO.StreamReader(fs, System.Text.Encoding.UTF8))
            text = sr.ReadToEnd();
        Tr(text.Length > 1000, "F1 前提：读得出来（现读 " + text.Length + " 字符）");
        Tr(text.IndexOf("\"endAt\"", StringComparison.Ordinal) < 0, "F2 ★ 原版卡包表里仍**没有** `endAt`（模拟数据写在自己的文件里）");
        Tr(text.IndexOf("\"expireDate\"", StringComparison.Ordinal) < 0, "F2 ★ 原版卡包表里仍**没有** `expireDate`");

        string own = System.IO.Path.Combine(Application.dataPath, "CardPresentation/Resources/draft_local_event.json");
        Tr(System.IO.File.Exists(own), "F3 ★ 模拟数据在自己的文件里：`Resources/draft_local_event.json`");
        Tr(typeof(DraftLocalEventPump).IsSubclassOf(typeof(MonoBehaviour)), "F4 ★ 到期泵是 MonoBehaviour（顶层类 + 类名==文件名）");
        C(DraftLocalEventPump.TickSeconds, 1f, "F4 ★ 泵的 tick 间隔 = 1s");
    }
}
