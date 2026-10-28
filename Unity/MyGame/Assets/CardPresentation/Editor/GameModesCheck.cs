// GameModesCheck.cs —— 执行代理 `B89-E11`（2026-10-28）的断言宿主（只放断言）
//
// 本文件**只放断言**（⛔ 不改产品行为、不建场景、不碰 `Editor/BattleScene.cs`）。
// 调用方式（主对话在宿主收尾处加两行，想并进总数就带后一行）：
//
//     GameModesCheck.Run();
//     pass += GameModesCheck.Pass; fail += GameModesCheck.Fail;
//
// 入口名：`-executeMethod GameModesCheck.Run`
//
// ==================================================================
//  覆盖的四节（判据逐条写在断言消息里，判据件 = `判据/B88_R10_基础设施58条施工单.md` §三）
// ------------------------------------------------------------------
//  A. `GameModesHandler.DeserializeSharedData`：Classic → Ranked → new 三条回落；
//     **FastMode 没有 Ranked 级回落**（灭自证）；第三条取的是**整份** sharedData（灭自证）。
//  B. `SetResultCount`：胜/负/平各 +1；**未知 winner 不许往表里塞新记录**（灭自证 = 原版 `default` 支不读表）。
//  C. `SetIsHuman` / `SetLossStreak` / `SetAsCurrentlyPlayingEvent` / `CombatFinished`：
//     写进 `RankedSharedData`、结算分派一次、**末尾清空 `CurrentPlayingEvent`**（原版 `.c` 那一句）。
//  D. `GetActiveEvent(PlayModes)` / `GetActiveEvent(Type)` / `GetActiveEvent<T>()`：
//     三道闸（模式 ∧ Active ∧ Priority 降序）+ 排位接口那条递归支。
//
// ⚠️ 夹具：`FakePlayEvent : PlayEvent<PlayEventSaveGame, FakePlayEventData>` —— 只是为了给
//    `EventPlayMode` / `MatchFinished` 两个成员一个真身（原版那两个是 `IPlayEvent` 的接口成员，
//    本机接口未建，见 `GameModesHandler.cs` 文件头差异 ①）。
// ==================================================================
using System;
using System.Collections.Generic;
using CardPresentation;
using UnityEngine;

public static class GameModesCheck
{
    const string P = "[GameModes] ";

    /// <summary>本类这一趟的通过 / 失败数（主对话想并进宿主的合计数就加这两格）。</summary>
    public static int Pass { get; private set; }
    public static int Fail { get; private set; }

    public static void Run()
    {
        Pass = 0; Fail = 0;
        try
        {
            RunDeserializeSharedData();
            RunSetResultCount();
            RunSettersAndCombatFinished();
            RunGetActiveEvent();
            RunRegisteredHandler();
            RunDraftGate();
            RunBootWiring();
        }
        catch (Exception e)
        {
            Fail++;
            Debug.LogError(P + "★ 未捕获异常：" + e);
        }
        finally
        {
            GameModesHandler.ResetForTest();
        }
        Debug.Log(P + "=== 断言合计：" + Pass + " 通过 / " + Fail + " 失败 ===");
    }

    // ==================================================================
    //  A · DeserializeSharedData（账 216）
    // ==================================================================
    static void RunDeserializeSharedData()
    {
        Debug.Log(P + "--- A · DeserializeSharedData 三条回落 ---");

        // ---- A1 Classic 在 ⇒ 直接用（其余键全不读）----
        var h = MakeHandler(
            "{\"Classic\":{\"previousLeaderboard\":\"L-classic\",\"lastPlayedSeason\":7,"
          + "\"lastPlayedEvent\":\"EV-7\",\"isHuman\":true,\"lossStreak\":2,"
          + "\"highestScoreSeason\":11,\"SeasonPositions\":{\"3\":15},\"HighestPosition\":{\"Position\":4,\"Rating\":1600,\"Season\":3}},"
          + "\"Ranked\":{\"previousLeaderboard\":\"L-ranked\"},"
          + "\"FastMode\":{\"previousLeaderboard\":\"L-fast\"}}");
        Eq(h.RankedSharedData == null ? null : h.RankedSharedData.previousLeaderboard, "L-classic",
           "A1 `Classic` 在 ⇒ RankedSharedData 取 Classic（⛔ 不是 Ranked）");
        Eq(h.RankedSharedData.lastPlayedSeason, 7, "A1 `lastPlayedSeason` 读进来");
        Eq(h.RankedSharedData.lastPlayedEvent, "EV-7", "A1 `lastPlayedEvent` 读进来");
        Ok(h.RankedSharedData.isHuman, "A1 `isHuman:true` 读进来");
        Eq(h.RankedSharedData.lossStreak, 2, "A1 `lossStreak` 读进来");
        Eq(h.RankedSharedData.highestScoreSeason, 11, "A1 `highestScoreSeason` 读进来");
        int season3;
        Ok(h.RankedSharedData.SeasonPositions != null && h.RankedSharedData.SeasonPositions.TryGetValue(3, out season3) && season3 == 15,
           "A1 `SeasonPositions` 的 `{\"3\":15}` 解成字典 `<3,15>`");
        Eq(h.RankedSharedData.HighestPosition == null ? -1 : h.RankedSharedData.HighestPosition.Position, 4,
           "A1 `HighestPosition.Position` 读进来");
        Eq(h.RankedSharedData.HighestPosition.Rating, 1600, "A1 `HighestPosition.Rating` 读进来");
        Eq(h.FastSharedData == null ? null : h.FastSharedData.previousLeaderboard, "L-fast",
           "A1 `FastMode` 在 ⇒ FastSharedData 取 FastMode");

        // ---- A2 没有 Classic ⇒ 回落 Ranked；**FastMode 不回落**（灭自证）----
        var h2 = MakeHandler("{\"Ranked\":{\"previousLeaderboard\":\"L-ranked\"}}");
        Eq(h2.RankedSharedData == null ? null : h2.RankedSharedData.previousLeaderboard, "L-ranked",
           "A2 只有 `Ranked` ⇒ 回落到它");
        Ok(h2.FastSharedData != null, "A2（前提）FastSharedData 仍然被建出来（不是 null）");
        Ok(h2.FastSharedData != null && h2.FastSharedData.previousLeaderboard == null,
           "A2 ★灭自证：`FastMode` 缺失时**不会**退回 `Ranked` 那个值（原版没有 Ranked 级回落）");

        // ---- A3 两个键都不在 ⇒ new（ctor 默认值：Position = int.MaxValue）----
        var h3 = MakeHandler("{}");
        Ok(h3.RankedSharedData != null, "A3 两个键都不在 ⇒ `new RankedSharedData()`（不是 null）");
        Ok(h3.RankedSharedData != null && h3.RankedSharedData.previousLeaderboard == null, "A3 新实例的 `previousLeaderboard` 是 null");
        Eq(h3.RankedSharedData == null ? -1 : h3.RankedSharedData.HighestPosition.Position, int.MaxValue,
           "A3 原版 ctor 的默认值：`HighestPosition.Position = int.MaxValue`（= 还没有名次）");
        Eq(h3.RankedSharedData == null ? -1 : h3.RankedSharedData.SeasonPositions.Count, 0, "A3 原版 ctor：`SeasonPositions` 是空字典（不是 null）");

        // ---- A4 第三条：取**整份** sharedData（灭自证：埋在 Ranked 子对象里的 ResultCounter 不算）----
        var h4 = MakeHandler("{\"ResultCounter\":{\"Classic\":{\"Won\":1,\"Loss\":2,\"Draw\":3}}}");
        Ok(h4.ResultCounterData != null, "A4 `ResultCounterData` 被建出来");
        ResultValues v4;
        Ok(h4.ResultCounterData != null && h4.ResultCounterData.ResultCounter.TryGetValue("Classic", out v4) && v4.Won == 1 && v4.Loss == 2 && v4.Draw == 3,
           "A4 顶层 `ResultCounter.Classic` 的 `Won/Loss/Draw` = 1/2/3 解进来");

        var h5 = MakeHandler("{\"Ranked\":{\"ResultCounter\":{\"Classic\":{\"Won\":9}}}}");
        Eq(h5.ResultCounterData == null ? -1 : h5.ResultCounterData.ResultCounter.Count, 0,
           "A5 ★灭自证：`ResultCounter` 埋在 `Ranked` 子对象里 ⇒ **读不到**（原版取的就是整份 sharedData 的顶层）");
    }

    // ==================================================================
    //  B · SetResultCount（账 224）
    // ==================================================================
    static void RunSetResultCount()
    {
        Debug.Log(P + "--- B · SetResultCount 三支 +1 ---");
        var h = MakeHandler("{\"ResultCounter\":{\"Classic\":{\"Won\":1,\"Loss\":2,\"Draw\":3}}}");
        var data = h.ResultCounterData;
        if (!Ok(data != null, "B0（前提）ResultCounterData 已装好")) return;
        ResultValues v;
        if (!Ok(data.ResultCounter.TryGetValue("Classic", out v) && v != null, "B0（前提）`Classic` 那一格在")) return;

        h.SetResultCount("Classic", BattleWinner.Player);
        Eq(v.Won, 2, "B1 `BattleWinner.Player(10)` ⇒ `Won` +1");
        Eq(v.Loss, 2, "B1 其它两格不动（Loss）");
        h.SetResultCount("Classic", BattleWinner.Enemy);
        Eq(v.Loss, 3, "B2 `BattleWinner.Enemy(20)` ⇒ `Loss` +1");
        h.SetResultCount("Classic", BattleWinner.Draw);
        Eq(v.Draw, 4, "B3 `BattleWinner.Draw(30)` ⇒ `Draw` +1");

        // ---- B4 未知 winner：**连表都不许读**（原版 `default` 支直接 return）----
        int before = data.ResultCounter.Count;
        h.SetResultCount("Duel", BattleWinner.Undefined);
        Eq(data.ResultCounter.Count, before,
           "B4 ★灭自证：`Undefined(0)` 不许往 `ResultCounter` 里新建 'Duel' 这一格（原版 `default` 支不碰表）");
        Ok(!data.ResultCounter.ContainsKey("Duel"), "B4 表里确实没有 'Duel'（GetValuesForMode 的建表副作用没被触发）");

        // ---- B5 一个全新的 mode（合法 winner）⇒ 自动建格并 +1（原版 GetValuesForMode 的语义）----
        h.SetResultCount("Skirmish", BattleWinner.Player);
        ResultValues vs;
        Ok(data.ResultCounter.TryGetValue("Skirmish", out vs) && vs != null && vs.Won == 1,
           "B5 新 mode 合法 winner ⇒ 建格并把 `Won` 记 1（原版 `GetValuesForMode` 会 new 一条）");
    }

    // ==================================================================
    //  C · SetIsHuman / SetLossStreak / CombatFinished（账 222 / 223 / 215 / 221）
    // ==================================================================
    static void RunSettersAndCombatFinished()
    {
        Debug.Log(P + "--- C · 两个 setter + CombatFinished ---");
        var h = MakeHandler("{}");
        Ok(h.RankedSharedData != null, "C0（前提）空 sharedData 也有 RankedSharedData（ctor 默认）");

        h.SetIsHuman();
        Ok(h.IsHuman, "C1 `SetIsHuman()` ⇒ `RankedSharedData.isHuman = true`（读口 `IsHuman` 也变了）");
        h.SetLossStreak(4);
        Eq(h.LossStreak, 4, "C2 `SetLossStreak(4)` ⇒ 读口 `LossStreak` = 4");

        // ---- C3 CombatFinished：分派一次 + 末尾清空 ----
        var ev = (FakePlayEvent)MakeEvent("ev-combat", PlayModes.Classic, 1, false);
        h.SetAsCurrentlyPlayingEvent(ev);
        Ok(ReferenceEquals(h.CurrentPlayingEvent, ev), "C3 `SetAsCurrentlyPlayingEvent` 写进了 `CurrentPlayingEvent`");

        var result = new RankedMatchResult(1000, 50) { battleWinner = BattleWinner.Player };
        bool cbCalled = false;
        h.CombatFinished(result, r => { cbCalled = true; });
        Eq(ev.finishedCalls, 1, "C3 `CombatFinished` ⇒ 分派给事件的 `MatchFinished` **一次**");
        Ok(ReferenceEquals(ev.lastResult, result), "C3 分派的就是传进来的那份 `RankedMatchResult`（不是重新 new 的）");
        Ok(cbCalled, "C3 回调被转发到 `MatchFinished` 的 `onCompleted`（没被吞掉）");
        Ok(h.CurrentPlayingEvent == null, "C3 ★原版末尾那句清空：`CurrentPlayingEvent` 变 null");

        h.CombatFinished(result, null);
        Eq(ev.finishedCalls, 1, "C4 ★灭自证：清空之后再来一次 ⇒ **不再分派**（同一局只结算一次）");
    }

    // ==================================================================
    //  D · GetActiveEvent 三道闸（账 217 / 218 / 219）
    // ==================================================================
    static void RunGetActiveEvent()
    {
        Debug.Log(P + "--- D · GetActiveEvent(PlayModes) / (Type) / <T>() ---");

        var e1 = MakeEvent("e1-classic-p5", PlayModes.Classic, 5, false);
        var e2 = MakeEvent("e2-classic-p9", PlayModes.Classic, 9, false);
        var e3 = MakeEvent("e3-classic-p99-dead", PlayModes.Classic, 99, true);   // 优先级最高但**不在生效期**
        var e4 = MakeEvent("e4-duel-p50", PlayModes.Duel, 50, false);
        var e5 = MakeOtherEvent("e5-other-p1", PlayModes.Classic, 1);
        var h = new GameModesHandler();
        var t = h.SetupEvents(new List<ILiveOps> { e3, e4, e1, e2, e5 }, new List<ILiveOps>(), LiveOpsJson.Parse("{}"));
        if (!t.IsCompleted) t.Wait();

        // ---- D1 模式 + Active + 优先级 ----
        Ok(ReferenceEquals(h.GetActiveEvent(PlayModes.Classic), e2),
           "D1 `GetActiveEvent(Classic)` = 优先级 9 那条（p99 那条**不在生效期**被刷掉、p5 那条输了）");
        Ok(ReferenceEquals(h.GetActiveEvent(PlayModes.Duel), e4), "D2 模式不同的不会被串（Duel ⇒ 那一条）");
        Ok(h.GetActiveEvent(PlayModes.Skirmish) == null, "D3 没有该模式 ⇒ null（不是抛）");
        var picked = h.GetActiveEvent(PlayModes.Classic);
        Ok(picked != null && picked.EventState == LiveOpState.Active, "D4 取到的那条 `EventState` 确实是 Active");

        // ---- D5 按类型 ----
        Ok(ReferenceEquals(h.GetActiveEvent(typeof(FakeOtherEvent)), e5), "D5 `GetActiveEvent(typeof(FakeOtherEvent))` = 那个子类型那条");
        Ok(ReferenceEquals(h.GetActiveEvent<FakeOtherEvent>(), e5), "D6 `GetActiveEvent<FakeOtherEvent>()` 同一结果（泛型那半只是强转）");
        Ok(h.GetActiveEvent(typeof(GameModesHandler)) == null,
           "D7 ★灭自证：问一个没有任何事件是的类型 ⇒ null（类型闸真的在）");

        // ---- D8 排位接口那条递归支 ----
        GameModesHandler.RankedEventInterfaceType = typeof(FakePlayEvent);
        GameModesConfigSource.MainRankedType = null;
        Ok(h.GetActiveEvent(typeof(FakePlayEvent)) == null,
           "D8 问排位接口、但远端 `mainRankedType` 拿不到 ⇒ null（原版这一支直接返回 null，**不**退化成普通 LINQ）");

        GameModesConfigSource.MainRankedType = typeof(FakeOtherEvent);
        Ok(ReferenceEquals(h.GetActiveEvent(typeof(FakePlayEvent)), e5),
           "D9 `mainRankedType` 指向 FakeOtherEvent ⇒ 递归一次拿到 e5（原版的递归那一跳）");

        GameModesConfigSource.MainRankedType = typeof(FakePlayEvent);   // = 接口本身 ⇒ 不递归，走普通 LINQ（该支**无模式闸**）
        Ok(ReferenceEquals(h.GetActiveEvent(typeof(FakePlayEvent)), e4),
           "D10 `mainRankedType` 就是接口本身 ⇒ 不递归、落回普通 LINQ（原版 type 路径只有 Is(g,t) ∧ Active ∧ 优先级降序 ⇒ Duel 的 e4(p50) 也是 FakePlayEvent、胜 Classic 的 e2(p9)）");

        GameModesHandler.ResetForTest();
    }

    // ==================================================================
    //  E · 注册表接线（`LiveOpsManager.Instance` → `GetHandler<GameModesHandler>()`）
    //     判据：原版 `PracticeEvent.OnMatchFinished` 的第一句就是
    //     `LiveOpsManager.GetHandler<GameModes>().SetResultCount("Practice", result.battleWinner)`
    //     ⇒ 「拿得到 handler」这条链断了，前面 12 条等于挂空。
    // ==================================================================
    static void RunRegisteredHandler()
    {
        Debug.Log(P + "--- E · 注册表接线（LiveOpsManager）---");

        var mgr = LiveOpsManager.Instance;                      // 建单例（顺手登记内建 handler）
        Ok(mgr != null, "E1 `LiveOpsManager.Instance` 建得出来");

        var registered = LiveOpsManager.GetHandler<GameModesHandler>();
        Ok(registered != null, "E1 ★`GetHandler<GameModesHandler>()` **非 null**（= 真注册进注册表了）");

        var keys = LiveOpsManager.HandlerKeys;
        bool hasLocal = false, hasGameModes = false;
        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] == LiveOpsLocalHandler.LocalHandlerKey) hasLocal = true;
            if (keys[i] == "GameModes") hasGameModes = true;
        }
        Ok(hasGameModes, "E2 `HandlerKeys` 里有 `\"GameModes\"`");
        Ok(hasLocal, "E2 ★`\"local\"` 仍在（新 handler 只**追加**，没顶掉已有那个）");

        if (registered == null) return;

        // E3：原版 `PracticeEvent.OnMatchFinished` 第一句的落地路径（装一次空 sharedData 之后记账）
        var t = registered.SetupEvents(new List<ILiveOps>(), new List<ILiveOps>(), LiveOpsJson.Parse("{}"));
        if (!t.IsCompleted) t.Wait();
        Ok(registered.ResultCounterData != null, "E3（前提）注册表里那个 handler 装载后有 `ResultCounterData`");

        registered.SetResultCount("Practice", BattleWinner.Player);
        ResultValues v;
        Ok(registered.ResultCounterData != null
           && registered.ResultCounterData.ResultCounter.TryGetValue("Practice", out v) && v != null && v.Won == 1,
           "E3 ★`SetResultCount(\"Practice\", Player)` 记进注册表里那个实例（原版 `PracticeEvent` 那一跳，账 584 的 GameModes 半）");
    }

    // ==================================================================
    //  F · 草稿入口闸（账 585 的那一半：原版 `PracticeEvent.OnMatchWon` ⇒ `demoData.canEnterDraftMode = true`）
    //     闸本体 = `DraftEventBase.CanEnterDraftMode`（R2d 建）；**生产者 = 本批的 `GameModesHandler`**。
    //     原版判据：`Everguild.LiveOps/PracticeEvent.cs:95-101`（置 true + 上传）。
    // ==================================================================
    static void RunDraftGate()
    {
        Debug.Log(P + "--- F · 草稿入口闸（注入给 DraftEventBase<,> 的闭合类型）---");

        var savedUnlocked = GameModesHandler.DraftModeUnlocked;
        try
        {
            // F1 ★灭自证：**未注入**态必须与 R2d 改前逐字一致（`CanvasEnterDraftMode == null ⇒ 允许`）
            FakeDraftEvent.ResetInjectionForTest();
            Ok(FakeDraftEvent.CanEnterDraft, "F1 ★未注入 ⇒ `CanEnterDraft` = true（= R2d 改前的默认态，⛔ 我们没把这支改坏）");

            // F2 注入（点名闭合类型：闸是泛型类上的 static，每个闭合类型各一份）
            GameModesHandler.InstallDraftGate<PlayEventSaveGame, DraftEventData>();
            Ok(FakeDraftEvent.CanEnterDraftMode != null,
               "F2 `InstallDraftGate<PlayEventSaveGame, DraftEventData>()` ⇒ 该闭合类型的闸非 null（生产者 = `GameModesHandler`）");

            // F3 兜底值 = 放行
            GameModesHandler.DraftModeUnlocked = true;
            Ok(FakeDraftEvent.CanEnterDraft, "F3 `DraftModeUnlocked = true` ⇒ 允许进入（本批的兜底态）");

            // F4 ★灭自证：闸真的在（活读口，不是快照）
            GameModesHandler.DraftModeUnlocked = false;
            Ok(!FakeDraftEvent.CanEnterDraft, "F4 ★灭自证：`DraftModeUnlocked = false` ⇒ `CanEnterDraft` = false（拒绝）");

            // F5 首胜解锁口
            new GameModesHandler().NotifyPracticeMatchWon();
            Ok(GameModesHandler.DraftModeUnlocked && FakeDraftEvent.CanEnterDraft,
               "F5 `NotifyPracticeMatchWon()` ⇒ 置 true（原版 `demoData.canEnterDraftMode = true`；上传那半是 P3）");
        }
        finally
        {
            GameModesHandler.DraftModeUnlocked = savedUnlocked;
            FakeDraftEvent.ResetInjectionForTest();        // 恢复「未注入」，⛔ 不留测试态
        }
    }

    // ==================================================================
    //  G · 启动链（两处调用点：`ShellRuntime.Awake`（Shell.unity = Build Settings 索引 0）
    //     + `MainMenuRuntime.Awake`（进主菜单）→ 同一个 `LiveOpsManager.EnsureStarted()`）
    //     判据：原版 liveops 注册表由 `PlayerDataManager.InitializeLiveOpsAndConfig` → `Unpack(HandlerData)`
    //     → `BuildHandlers` 驱动（`PlayerDataManager.cs:4572`），本机没有服务器 ⇒ 只做「建实例 + 登记」半步。
    //     手牌文本档那份判据见 `MainMenuRuntime.ReadTextInHandFormatAtBoot` 的注释（两份 `.c` 现读）。
    // ==================================================================
    static void RunBootWiring()
    {
        Debug.Log(P + "--- G · 启动链接线（ShellRuntime 先跑、MainMenuRuntime 再跑）---");

        // G1 ★灭自证的前提：先把「启动过」复位 ⇒ 不跑 Awake 时单例必须仍是 null
        LiveOpsManager.ResetBootForTest();
        Ok(LiveOpsManager.InstanceOrNull == null, "G1 ★复位后单例是 null（证明下面那条不是「本来就建好了」）");

        var goShell = new GameObject("~E11dShell");
        var goMenu = new GameObject("~E11dMenu");
        int saved = SettingsWindow.TextInHand.Value;          // 先记玩家真档（收尾要还原）
        try
        {
            // ---- 第一处：壳（Shell.unity = Build Settings 索引 0，比主菜单更早）----
            // ⚠️ 编辑模式下 `AddComponent` **不会**触发 `Awake` ⇒ 手动发一次（CLAUDE.md 已记的那个坑）
            LiveOpsManager first;
            try
            {
                var shell = goShell.AddComponent<ShellRuntime>();
                shell.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);
                first = LiveOpsManager.InstanceOrNull;
                Ok(first != null,
                   "G2 ★`ShellRuntime.Awake` 跑过 ⇒ `LiveOpsManager.Instance` 不再是 null（把 ShellRuntime 里那句注掉这条必红）");
            }
            catch (Exception e)
            {
                first = null;
                Fail++;
                Debug.LogError(P + "   ✗ G2 `ShellRuntime.Awake` 在编辑器里跑不起来（这条**没验到**）：" + e);
            }

            // ---- 第二处：主菜单（再跑一次，必须不重建）----
            SettingsWindow.TextInHand.RestoreForTest(2);      // 造一个已知档：2 = 仅卡名
            var mm = goMenu.AddComponent<MainMenuRuntime>();
            mm.SendMessage("Awake", SendMessageOptions.DontRequireReceiver);

            Ok(first != null && ReferenceEquals(LiveOpsManager.InstanceOrNull, first),
               "G3 ★两处都调也只初始化一次：`ShellRuntime` 先跑、`MainMenuRuntime` 再跑 ⇒ **同一个实例**（不是重建的）");
            Ok(LiveOpsManager.GetHandler<GameModesHandler>() != null,
               "G4 注册表里有 `GameModesHandler` ⇒ `GetHandler<…>()` 取得到（原版 `PracticeEvent` 那种取法才有东西可取）");
            Ok(CardView.HandTextFormat == 2,
               "G5 ★开机读手牌文本档：`TextInHand = 2` ⇒ `CardView.HandTextFormat == 2`（把 Awake 里那句注掉这条必红）");

            var a = LiveOpsManager.EnsureStarted();
            var b = LiveOpsManager.EnsureStarted();
            Ok(ReferenceEquals(a, b) && ReferenceEquals(a, LiveOpsManager.InstanceOrNull),
               "G6 `EnsureStarted()` 幂等：重复调用返回同一个单例（`Awake` 被多次触发也不出事）");
        }
        finally
        {
            SettingsWindow.TextInHand.RestoreForTest(saved);   // 还原玩家的真档
            CardView.SetHandTextFormat(saved);                  // `RestoreForTest` 只写内存、不应用 ⇒ 这里补应用
            UnityEngine.Object.DestroyImmediate(goMenu);
            UnityEngine.Object.DestroyImmediate(goShell);
        }
    }

    // ==================================================================
    //  夹具
    // ==================================================================
    static GameModesHandler MakeHandler(string sharedJson)
    {
        var j = LiveOpsJson.Parse(sharedJson);
        Ok(j != null, "（夹具前提）sharedData JSON 解析成功：" + sharedJson);
        var h = new GameModesHandler();
        var t = h.SetupEvents(new List<ILiveOps>(), new List<ILiveOps>(), j);
        if (!t.IsCompleted) t.Wait();
        return h;
    }

    /// <summary>活着的（Permanent ⇒ 恒 Active）或已死（OnDate + 窗口在过去）的事件。</summary>
    static FakePlayEvent MakeEvent(string id, PlayModes mode, int priority, bool dead)
    {
        var ev = new FakePlayEvent { mode = mode };
        var data = new FakePlayEventData
        {
            eventId = id,
            enabled = true,
            priority = priority,
            drawData = new EventElements { ShowWindowOnTrigger = false, ShowWindowOnExpire = false },
        };
        if (dead)
        {
            DateTime now = LiveOpsClock.NowUtc;
            data.activationType = LiveOpsActivationType.OnDate;
            data.dates = new LiveOpsEventTimeData
            {
                startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-3)),
                endDate = LiveOpsEventTimeData.ToMs(now.AddHours(-2)),   // FinalDate 在过去 ⇒ Expired（总之 ≠ Active）
                expirable = false,
            };
        }
        else
        {
            data.activationType = LiveOpsActivationType.Permanent;      // ⇒ GetState 短路 Active
        }
        ev.LoadEventData(data, new PlayEventSaveGame(), null);
        return ev;
    }

    static FakeOtherEvent MakeOtherEvent(string id, PlayModes mode, int priority)
    {
        var ev = new FakeOtherEvent { mode = mode };
        var data = new FakePlayEventData
        {
            eventId = id,
            enabled = true,
            priority = priority,
            activationType = LiveOpsActivationType.Permanent,
            drawData = new EventElements { ShowWindowOnTrigger = false, ShowWindowOnExpire = false },
        };
        ev.LoadEventData(data, new PlayEventSaveGame(), null);
        return ev;
    }

    // ==================================================================
    //  断言小工具（与 `E13` / `E10` 同形：本类自己记一份 Pass/Fail）
    // ==================================================================
    static bool Ok(bool cond, string msg)
    {
        if (cond) { Pass++; Debug.Log(P + $"   ✓ {msg}"); return true; }
        Fail++; Debug.LogError(P + $"   ✗ {msg}"); return false;
    }

    static bool Eq<T>(T got, T want, string msg)
    {
        return Ok(EqualityComparer<T>.Default.Equals(got, want), msg + $"（实得 {got} / 期望 {want}）");
    }
}

// ======================================================================
//  夹具事件（只为给 `EventPlayMode` / `MatchFinished` 一个真身；
//  原版这两个是 `IPlayEvent` 的接口成员，本机接口未建 —— 见 `GameModesHandler.cs` 文件头差异 ①）
// ======================================================================
sealed class FakePlayEventData : PlayEventData { }

/// <summary>⚠️ 不是 sealed：<see cref="FakeOtherEvent"/> 要继承它（用来验「按类型挑事件」那一条）。</summary>
class FakePlayEvent : PlayEvent<PlayEventSaveGame, FakePlayEventData>
{
    public PlayModes mode;

    public override PlayModes EventPlayMode { get { return mode; } }

    /// <summary>原版 `PlayEvent.GetMatchmakingFilter`（抽象）—— 本夹具不参与撮合，返回 null。</summary>
    public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost) { return null; }

    // ── 断言用记录 ─────────────────────────────────────────────
    public int finishedCalls;
    public RankedMatchResult lastResult;

    /// <summary>覆盖结算口：记一笔并把回调**原样转发**（真身 `PlayEvent.MatchFinished` 会把回调交给
    /// `OnMatchFinished`；这里只要证明 `GameModes.CombatFinished` 的转发与「只分派一次」这两件事）。</summary>
    public override void MatchFinished(RankedMatchResult result, Action<OnlineMatchResult> onCompleted)
    {
        finishedCalls++;
        lastResult = result;
        if (onCompleted != null) onCompleted(new OnlineMatchResult { success = true });
    }
}

sealed class FakeOtherEvent : FakePlayEvent { }

/// <summary>只为拿到 `DraftEventBase&lt;TS,TD&gt;` 的**一个闭合类型** —— R2d 的草稿闸是**泛型类上的 static**
/// （`Shell/DraftEventBase.cs:340`）⇒ 每个闭合类型各一份，不点名类型就没法注入、也没法断言。
/// ⚠️ 它不是产品类（生产上的具体子类由轮抽线落地）。</summary>
sealed class FakeDraftEvent : DraftEventBase<PlayEventSaveGame, DraftEventData>
{
    /// <summary>夹具用：`PlayModes` 里**没有**「轮抽」这一档（原版也没有）⇒ 取一个占位值，本夹具不参与按模式挑选。</summary>
    public override PlayModes EventPlayMode { get { return PlayModes.Campaign; } }

    public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost) { return null; }
}
