// B108_A2_PlayerChain_Assert.cs — 执行代理 `B108_A2`（第一百零八会话 · 波 1 · AUD10 玩家主链）
//
// ============================ 它盯的是什么 ============================
//  `资料/完全复刻/判据/AUD10_玩家主链_侦查.md` §二/§三 里剩下的五条真缺口：
//    #1 结算从不调 `GameModes.CombatFinished`（且 `SetAsCurrentlyPlayingEvent` 0 生产调用点）
//    #3 `_rankedSettled` 不在 `Begin()` 复位 ⇒ 同一台 driver 第二局不再结算排位分
//    #5 教程关打完**不记完成位**（`CompletedCount()` 原来硬编码 0）
//    #7 三路切场景三种写法 + 「场景不可加载 ⇒ 无出口」
//    #8 `BeginTutorial` 三处失败只 `LogError` + `return` ⇒ 玩家停在空战场
//
//  🔴 **交接事实（现读）**：#1 / #3 / #5 / #7 的「改走 `LoadSceneWithFade`」/ #8 的调用点，
//     在 `B106_W2`（第一百零六会话）那一批**已经落地**。本件因此分两半：
//     ① **锁住已落地的**（防回退：A/B/C/E1-E4、D4/D5）；
//     ② 钉住本轮补的**残余**：#7「场景不可加载 ⇒ 有可见出口」（D3/D4e/D4f/D5d/D5e/D5f/D6）。
//
// ============================ 怎么接（⚠️ 本代理不动宿主）============================
//  `B108_A2_PlayerChain_Assert.Run();`
//  宿主里两行（同族先例 `B98H5_Assert` / `B106_W23_Assert`）：
//    `B108_A2_PlayerChain_Assert.Run(); pass += B108_A2_PlayerChain_Assert.Pass; fail += B108_A2_PlayerChain_Assert.Fail;`
//  · **不切场景 / 不写玩家存档 / 不改产品静态状态**（D6 / E5 用完都还原：`CardPlayedSignals.LocalSeat`、`Time.timeScale`）。
//  · E5 **必须在 `-batchmode` 下跑**（非批处理会真的弹错误框、把宿主卡住）—— 前提本身有一条硬断言，⛔ 不是静默跳过。
//
// ============================ 🔴 灭自证（哪一处改动会让哪一条红）============================
//  · A1：把 `Begin()` 里那个 `NotifyGameModesMatchStarted();` 删掉 ⇒ A1 红（且 #1 的「事件恒 null」立刻复发）。
//  · A3：把「取不到就显式写 null」删掉 ⇒ A3 红（上一局的事件会被下一局分派出去 = 静默错账）。
//  · A4b：把任一道排除闸（教程 / 教程回放 / 回放 / 决斗）删掉 ⇒ A4b 红。
//  · B1/B2：把 `Begin()` 里的 `_rankedSettled = false` 删掉 ⇒ B1/B2 红。
//  · C2：把 `CompletedCount()` 改回 `return 0;` ⇒ C2 红。
//  · D2：把 `ArenaByArmy.CanLoadScene` 改成恒真 ⇒ D2 红。
//  · D4e/D4f/D5e/D5f：把「载不入时的复位/弹框/清通道」删掉 ⇒ 对应条红（= 又回到「永久软锁 / 停在窗里毫无提示」）。
//  · D6：把 `ClearPendingBattleChannels()` 改成空方法 ⇒ D6b/D6c 红。
//  · E1b：把三处里任一处 `FailTutorialStartAndExit()` 改回裸 `return` ⇒ E1b 红。
//  · E5b/E5c：把 `FailTutorialStartAndExit()` 改回「只 `LogError`」⇒ E5b/E5c 红（`LeaveCount` 不动）。
//  · F1/F2/F4：把 `randomSeed` 那两处写加回去 ⇒ 红。F7：把 `Editor/MainMenuScene.cs` 的读者删掉 ⇒ 红。
//  · F10：把 `TakePendingPlayMode()` 挪到 `PlayReplay(...)` **之后** ⇒ 红。
//  · G1b/G3：把钩子里那句 `CloseAllWindows()` 删掉、或改成逐扇 `openWindows[i].Close()` ⇒ 红。
using System;
using System.IO;
using System.Text.RegularExpressions;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>执行代理 `B108_A2`：钉 AUD10 剩余五条真缺口（#1 / #3 / #5 / #7 / #8）。
/// <para>⛔ 期望值全是字面量（`3` / `-1` / 方法名本身），不从被测实现里读回来。</para></summary>
public static class B108_A2_PlayerChain_Assert
{
    static readonly CheckSink _sink = new CheckSink("[B108A2] ") { Near = MenuNearStyle.Compact2 };

    /// <summary>通过条数（宿主累加用）。</summary>
    public static int Pass { get { return _sink.Pass; } }
    /// <summary>失败条数（宿主累加用）。</summary>
    public static int Fail { get { return _sink.Fail; } }

    static void Tr(bool c, string msg) { MenuCheck.True(_sink, c, msg); }
    static void C<T>(T got, T want, string msg) { MenuCheck.Check(_sink, got, want, msg); }

    const string SrcDriver = "CardPresentation/Battle/BattleDriver.cs";
    const string SrcTutorial = "CardPresentation/Shell/TutorialModePopup.cs";

    // ============================================================ 入口
    public static void Run()
    {
        MenuCheck.Section(_sink, "B108_A2 · AUD10 玩家主链：#1 活动级结算 / #3 排位闸复位 / #5 教程完成位 / #7 三路切场景 / #8 教程开局失败出口");
        SectionA_CombatFinished();
        SectionB_RankedSettled();
        SectionC_TutorialProgress();
        SectionD_SceneSwitch();
        SectionE_TutorialStartFailExit();
        SectionF_Followups();
        SectionG_SceneSwitchClosesWindows();
        SectionH_OfflineBotName();
        Debug.Log(_sink.P + "B108_A2 合计：" + _sink.Pass + " 通过 / " + _sink.Fail + " 失败");
    }

    // ============================================================ A · #1 活动级结算两跳
    static void SectionA_CombatFinished()
    {
        MenuCheck.Section(_sink, "A · AUD10 #1：`GameModes.SetAsCurrentlyPlayingEvent` / `CombatFinished` 真接线");

        string src = Src(SrcDriver);
        Tr(src.Length > 1000, "A0 **前提**：源码读得到（`" + SrcDriver + "`）");
        if (src.Length < 1000) return;

        string begin = MemberSlice(src, "public void Begin(");
        Tr(begin.Length > 2000, "A0b **前提**：`Begin(` 的方法切片取到了（长度 > 2000）");
        Tr(begin.Contains("NotifyGameModesMatchStarted()"),
           "A1 开战那一拍调 `NotifyGameModesMatchStarted()`（= 原版 `PlayEvent.StartMatchNoCheck` 首句 "
         + "`GameModes.set_CurrentPlayingEvent(this)`，判据 `decomp_b91/…StartMatchNoCheck__1127900.c:64-66`）"
         + "｜删掉这一句 ⇒ 红，#1 的「`CurrentPlayingEvent` 恒 null」立刻复发");

        string start = MemberSlice(src, "void NotifyGameModesMatchStarted()");
        Tr(start.Length > 300, "A2a **前提**：`NotifyGameModesMatchStarted` 切片取到了");
        Tr(start.Contains("GetActiveEvent") && start.Contains("SetAsCurrentlyPlayingEvent"),
           "A2b 按本局模式号挑「正在生效」的那个活动并写进 `CurrentPlayingEvent`（`GameModes.GetActiveEvent` 的三道闸：模式对 ∧ Active ∧ 优先级最高）");
        Tr(start.Contains("SetAsCurrentlyPlayingEvent(null)"),
           "A3 ★**不伪造**：取不到事件时**显式写 null**（每局覆盖一次）—— 否则上一局留下的事件会被下一场无关对局的 "
         + "`CombatFinished` 分派出去（静默错账）｜删掉那一句 ⇒ 红");

        string fin = MemberSlice(src, "void NotifyGameModesCombatFinished()");
        Tr(fin.Length > 500, "A4a **前提**：`NotifyGameModesCombatFinished` 切片取到了");
        Tr(fin.Contains("MatchType.Tutorial") && fin.Contains("MatchType.TutorialReplay")
           && fin.Contains("MatchType.Replay") && fin.Contains("MatchType.Duel"),
           "A4b 原版四道排除闸逐条在（教程 100 / 教程回放 140 / 回放 160 / 决斗 150；"
         + "判据 `d:/2/tools/decomp_full/BattleManager__ProcessMatchResult.c:157-181` 的 `0x8c` / `0xa0` / `0x96` + `IsTutorialMatch`）");
        Tr(fin.Contains("gm.CombatFinished(result, null)"),
           "A4c 真的把结果交给活动级 `GameModes.CombatFinished(result, callback)`（原版 `:177` 那一跳）");

        Tr(src.Contains("_settleCount++; NotifyGameModesCombatFinished(); NotifyTutorialStageCompleted();"),
           "A5 调用点在**真打**那一支（`_replaySession` 的 else 支）—— 回放局不派发（`matchType 0xA0` 原版同样排除）");

        // A6 · 行为：`CurrentPlayingEvent == null` ⇒ 原版首句早退，不崩、不伪造
        var h = new GameModesHandler();
        Tr(h.CurrentPlayingEvent == null,
           "A6a **前提**：新建的 `GameModesHandler` 没有正在打的活动（`CurrentPlayingEvent == null`）");
        bool threw = false;
        try { h.CombatFinished(new RankedMatchResult(1000, 50) { battleWinner = BattleWinner.Player }, null); }
        catch (Exception) { threw = true; }
        Tr(!threw, "A6b ★行为：`CurrentPlayingEvent == null` 时 `CombatFinished` **不抛**（原版首句 `if (currentEvent == null) return;`）");
        Tr(h.CurrentPlayingEvent == null, "A6c ★行为：它**没有东西可分派**就返回（⛔ 不伪造一个事件去结算）");
    }

    // ============================================================ B · #3 排位闸复位
    static void SectionB_RankedSettled()
    {
        MenuCheck.Section(_sink, "B · AUD10 #3：`_rankedSettled` 在 `Begin()` 复位（本局的账不跨局）");

        string src = Src(SrcDriver);
        string begin = MemberSlice(src, "public void Begin(");
        Tr(begin.Length > 2000, "B0 **前提**：`Begin(` 切片取到了");
        Tr(begin.Contains("_rankedSettled = false"),
           "B1 `Begin()` 的清零块里有 `_rankedSettled = false`｜删掉它 ⇒ 同一台 driver 第二局**不再结算排位分**（AUD10 §二 #3 / §三 R4）");
        Tr(begin.Contains("_leaving = false; _rankedSettled = false;"),
           "B2 它和 `_leaving` 写在**同一条语句**里（= 与 `_settled` / `_replaySession` 同一条「本局的账」纪律）");
        Tr(!begin.Contains("_rankedSettled = true"),
           "B3 ★灭自证：`Begin()` 里**没有**把它置真（置真的只有结算那一处）");
        Tr(src.Contains("public bool RankedSettled"),
           "B4 读口 `RankedSettled` 还在（宿主/自检观测得到）");
    }

    // ============================================================ C · #5 教程完成位
    static void SectionC_TutorialProgress()
    {
        MenuCheck.Section(_sink, "C · AUD10 #5：教程完成位（读口走 `DemoDataStore`，写口 = 打完教程关）");

        string tut = Src(SrcTutorial);
        string cnt = MemberSlice(tut, "public static int CompletedCount()");
        Tr(cnt.Length > 60, "C0 **前提**：`CompletedCount()` 切片取到了");
        Tr(cnt.Contains("IsTutorialCompleted"),
           "C1 `CompletedCount()` 逐关读 `DemoData.IsTutorialCompleted(i)`（不再硬编码 0 ⇒ 玩家打完一关能看出进展）");
        Tr(!Regex.IsMatch(cnt, @"return\s+\d+\s*;"),
           "C2 ★灭自证：`CompletedCount()` 里**没有** `return <常量>;`（原来那一句 `return 0` 会让 `Completed: n/6` 恒 `0/6`）");

        string drv = Src(SrcDriver);
        string done = MemberSlice(drv, "void NotifyTutorialStageCompleted()");
        Tr(done.Length > 300, "C3a **前提**：`NotifyTutorialStageCompleted` 切片取到了");
        Tr(done.Contains("DemoDataStore.SetTutorial(index.ToString(), true)"),
           "C3b 打完教程关写完成位：`DemoDataStore.SetTutorial(index.ToString(), true)` —— 键口径 = 关号下标 `0..5`"
         + "（⛔ 不是菜单 FTUE 那套 `PrefDonePrefix + StageId`，那是**另一个**键空间）");
        Tr(done.Contains("Ctx.Winner != _me + 1"), "C4 只在**过关**时写（原版完成位只在过关时写）");
        Tr(done.Contains("Application.isBatchMode"), "C5 批处理不写（自检不该改玩家的进度存档）");

        // C6 · 行为：键空间（读口 `DemoData.IsTutorialCompleted` 用的就是 `index.ToString()`）
        var d = new DemoData();
        Tr(!d.IsTutorialCompleted(0), "C6a **前提**：出厂态第 0 关未完成");
        d.tutorialsCompleted["4"] = true;
        Tr(d.IsTutorialCompleted(4), "C6b ★行为：键 `\"4\"` ⇒ `IsTutorialCompleted(4)` = true（写口传的就是 `index.ToString()`）");
        Tr(!d.IsTutorialCompleted(5), "C6c ★灭自证：第 5 关仍是 false（⛔ 不是「有一格真就全真」）");
    }

    // ============================================================ D · #7 三路切场景
    static void SectionD_SceneSwitch()
    {
        MenuCheck.Section(_sink, "D · AUD10 #7：三路切场景同一口径 + 「场景不可加载 ⇒ 有可见出口」");

        // D1/D2 · 行为：闸的判据本身（`ArenaByArmy.CanLoadScene` 扫 Build Settings 场景表，批处理下也准）
        Tr(ArenaByArmy.CanLoadScene("MainMenu"),
           "D1 **前提/行为**：主菜单场景在 Build Settings 里（⛔ 别改用 `Application.CanStreamedLevelBeLoaded` —— "
         + "2026-09-25 实测它在 `-batchmode` 下把已登记的场景也判 false，`ArenaByArmy.cs:82-87`）");
        Tr(!ArenaByArmy.CanLoadScene("__B108A2_NoSuchScene__"),
           "D2 ★灭自证：一个不存在的场景名判 **false**（⇒ D1 不是恒真）");
        Tr(BattleDriver.CanSwitchToMainMenuScene(),
           "D3 战后离场那一跳的闸 `BattleDriver.CanSwitchToMainMenuScene()` = true");

        // D4 · 源码锁：`LeaveBattle`
        string src = Src(SrcDriver);
        string leave = MemberSlice(src, "public void LeaveBattle()");
        Tr(leave.Length > 500, "D4a **前提**：`LeaveBattle()` 切片取到了");
        Tr(leave.Contains("ShellRuntime.LoadSceneWithFade"),
           "D4b 离场走壳的统一入口 `ShellRuntime.LoadSceneWithFade`（与练习 / 遭遇 / 排位同一口径）");
        Tr(!Regex.IsMatch(leave, @"(?<!Everguild)SceneManager\.LoadScene\s*\("),
           "D4c ★原缺口：**没有**裸 `SceneManager.LoadScene(...)` 调用（三处切场景三种写法已收口；"
         + "日志里引用的原版 `EverguildSceneManager.LoadScene(...)` 不算）");
        Tr(leave.Contains("CanSwitchToMainMenuScene()"),
           "D4d 切之前先过闸（场景不在 Build Settings ⇒ 不走那条链）");
        Tr(leave.Contains("_leaving = false;"),
           "D4e ★可见出口：闸不过时**复位 `_leaving`**（原来它刚被置真 ⇒ 玩家点什么都没反应 = 永久软锁）｜删掉 ⇒ 红");
        Tr(leave.Contains("ShowErrorMessage"),
           "D4f ★可见出口：闸不过时弹错误框（玩家看得见「为什么回不去」）｜删掉 ⇒ 红");

        // D5 · 源码锁：`TutorialModePopup.PlayTutorial`
        string tut = Src(SrcTutorial);
        string play = MemberSlice(tut, "public void PlayTutorial()");
        Tr(play.Length > 300, "D5a **前提**：`PlayTutorial()` 切片取到了");
        Tr(play.Contains("ShellRuntime.LoadSceneWithFade"),
           "D5b 教程开局走同一条 `ShellRuntime.LoadSceneWithFade`");
        Tr(!Regex.IsMatch(play, @"(?<!Everguild)SceneManager\.LoadScene\s*\("),
           "D5c ★原缺口：**没有**裸 `SceneManager.LoadScene(...)` 调用");
        Tr(play.Contains("ArenaByArmy.CanLoadScene"),
           "D5d 切之前先判「这个场景载得入吗」");
        Tr(play.Contains("ClearPendingBattleChannels()"),
           "D5e ★可见出口：载不入时清掉两条静态通道（`tutorialIndex` / `GameMode.Tutorial`）｜删掉 ⇒ 残留的教程模式号串到下一局");
        Tr(play.Contains("ShowErrorMessage"),
           "D5f ★可见出口：载不入时弹错误框（⛔ 不停在窗里毫无提示）");

        // D6 · 行为：`ClearPendingBattleChannels()` 真的清两条通道
        int seat = CardPlayedSignals.LocalSeat;
        try
        {
            BattleDriver.SetPendingTutorialStage(3);
            BattleDriver.SetPendingPlayMode(GameMode.Tutorial);
            Tr(BattleDriver.PendingPlayMode == GameMode.Tutorial,
               "D6a **前提**：两条静态通道都先写进去了（关号 3 / `GameMode.Tutorial`）");
            BattleDriver.ClearPendingBattleChannels();
            Tr(BattleDriver.PendingPlayMode == null,
               "D6b ★行为：清完之后 `PendingPlayMode` = null（残留的 `Tutorial(4)` 不会串到下一局）｜清法改空 ⇒ 红");
            C(BattleDriver.TakePendingTutorialStage(), -1,
               "D6c ★行为：教程关号通道也清成 -1｜清法改空 ⇒ 红");
        }
        finally { CardPlayedSignals.LocalSeat = seat; }
    }

    // ============================================================ E · #8 教程开局失败出口
    static void SectionE_TutorialStartFailExit()
    {
        MenuCheck.Section(_sink, "E · AUD10 #8：`BeginTutorial` 三处失败 ⇒ 有可见出口（⛔ 不再是空战场）");

        string src = Src(SrcDriver);
        string begin = MemberSlice(src, "public void BeginTutorial(");
        Tr(begin.Length > 1000, "E1a **前提**：`BeginTutorial` 切片取到了");
        int n = Regex.Matches(begin, "FailTutorialStartAndExit\\s*\\(\\s*\\)").Count;
        C(n, 3, "E1b 三处早退（关卡数据 / 那关的牌 / 两副牌解不出来）**都**走 `FailTutorialStartAndExit()`"
              + "｜任一处改回裸 `return` ⇒ 红（玩家停在空战场，只能杀进程）");

        string fail = MemberSlice(src, "void FailTutorialStartAndExit()");
        Tr(fail.Length > 200, "E2a **前提**：`FailTutorialStartAndExit` 切片取到了");
        Tr(fail.Contains("ErrorPopupWindowController.ShowErrorMessage"), "E2b 弹错误框（可见出口之一，原版错误出口）");
        Tr(fail.Contains("LeaveBattle"), "E3 错误框的 OK 钮回调 = `LeaveBattle`（= 本类唯一的「回主菜单」那一跳）");
        Tr(fail.Contains("Application.isBatchMode"), "E4 批处理里直接离场（弹窗要人点 OK，自检里没人点）");

        // E5 · 行为：关号越界 ⇒ 真的走了出口（⚠️ 只在批处理下跑 —— 非批处理会真的弹窗把宿主卡住）
        Tr(Application.isBatchMode,
           "E5 **前提**：必须在 `-batchmode` 下跑（非批处理会真的弹错误框）—— 前提不成立这条直接红，⛔ 不是静默跳过");
        if (!Application.isBatchMode) return;

        var go = new GameObject("B108A2_BattleDriverProbe");
        int seat = CardPlayedSignals.LocalSeat;
        float ts = Time.timeScale;
        try
        {
            var drv = go.AddComponent<BattleDriver>();
            int before = drv.LeaveCount;
            Tr(!drv.LeaveRequested, "E5a **前提**：新驱动的 `LeaveRequested` = false（还没走过出口）");
            drv.BeginTutorial(-1);          // 关号越界 ⇒ `TutorialData.ByIndex(-1)` = null ⇒ 走失败支
            Tr(drv.LeaveCount == before + 1,
               "E5b ★行为：关号越界 ⇒ `BeginTutorial` **真的走了出口**（`LeaveBattle` 走过 1 次）｜改回「只 `LogError`」⇒ 红");
            Tr(drv.LeaveRequested,
               "E5c ★行为：出口闩 `_leaving` 已置真（与「停在空战场、`Ctx == null`、无任何 UI」正相反）");
            Tr(drv.Ctx == null,
               "E5d **前提**：这一局确实**没开局**（`Ctx == null`）⇒ 证明 E5b 走的是失败支，不是普通开局");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(go);
            CardPlayedSignals.LocalSeat = seat;
            Time.timeScale = ts;
        }
    }

    // ============================================================ F · 续办三件（调度台 2026-10-31 追加）
    static void SectionF_Followups()
    {
        MenuCheck.Section(_sink, "F · 续办：#1 `randomSeed` 写已删（18·0 复刻死支）/ #2 `LiveOpsEventWindow.StartedBattle` 不是死字段 / #3 回放支丢弃残留模式通道");

        // ---- F · ① `randomSeed`：原版客户端 0 写 0 读、我们 0 消费 ⇒ 两处写已删，字段留着（序列化契约）----
        string gm = Src("CardPresentation/Shell/GameModesHandler.cs");
        string pe = Src("CardPresentation/Shell/PlayEvent.cs");
        Tr(gm.Length > 1000 && pe.Length > 1000, "F0 **前提**：`GameModesHandler.cs` / `PlayEvent.cs` 源码读得到");
        if (gm.Length < 1000 || pe.Length < 1000) return;

        Tr(!Regex.IsMatch(gm, @"^\s*if \(s\.randomSeed == 0\)", RegexOptions.Multiline),
           "F1 ★`GameModesHandler.StartPracticeMatchNoCheck` **不再**抖 `randomSeed`（原来 `if (s.randomSeed == 0) s.randomSeed = Environment.TickCount;`）"
         + "｜加回去 ⇒ 红。判据：原版客户端 0 写 0 读（`PlayEventSaveGame` 5 份方法体无 `+0x40` 访问）+ 我们 0 消费 ⇒ 按 18·0 复刻死字段");
        Tr(!Regex.IsMatch(pe, @"^\s*if \(SaveGame\.randomSeed == 0\)", RegexOptions.Multiline),
           "F2 ★`PlayEvent.StartMatchNoCheck` **不再**抖 `randomSeed`｜加回去 ⇒ 红");
        Tr(pe.Contains("public int randomSeed;"),
           "F3 **序列化契约没破**：`PlayEventSaveGame.randomSeed` 字段仍在（⛔ 删字段会改存档 JSON 形状）");
        Tr(!Regex.IsMatch(gm + "\n" + pe, @"^\s*[A-Za-z_][A-Za-z0-9_.\[\]]*\.randomSeed\s*=[^=]", RegexOptions.Multiline),
           "F4 ★灭自证：两个文件里**没有任何一行代码**给 `.randomSeed` 赋值（注释里引用的旧代码不算 —— 它不以 `xxx.randomSeed =` 开头）");

        // ---- F · ② `LiveOpsEventWindow.StartedBattle`：原版没有这个成员；但**我们自检宿主在读** ⇒ 不是死字段 ----
        string low = Src("CardPresentation/Shell/LiveOpsEventWindow.cs");
        string mm = Src("CardPresentation/Editor/MainMenuScene.cs");
        Tr(low.Length > 1000 && mm.Length > 1000, "F5 **前提**：`LiveOpsEventWindow.cs` / `Editor/MainMenuScene.cs` 源码读得到");
        Tr(low.Contains("public bool StartedBattle"),
           "F6 `LiveOpsEventWindow.StartedBattle` 仍在（= 我们的自检口；原版 `LiveOpsEventWindow`/`BaseParentEventWindow` **没有这个成员**，"
         + "`cpp2il/…/LiveOpsEventWindow.cs` 全类只有 `LiveOp` 一个字段）");
        Tr(mm.Contains("sk.StartedBattle") && mm.Contains("rk.StartedBattle")
           && Regex.Matches(mm, @"\.StartedBattle\b").Count >= 4,
           "F7 ★`AUD10` §三 R8 说它「全仓无读者」**是错的**：`Editor/MainMenuScene.cs` 里现读 `sk.StartedBattle`（`var sk = SkirmishEventWindow.LastOpened;`，"
         + "`SkirmishEventWindow : LiveOpsEventWindow`）与 `rk.StartedBattle`（`RankedEventWindow`，同样继承）共 ≥ 4 处"
         + "⇒ 它是**被断言的件**，⛔ 别再判死字段/别删读者｜删掉那些读者 ⇒ 本条红");

        // ---- F · ③ 回放支丢弃残留的模式通道（模式号来自录像头，不来自菜单通道）----
        string drv = Src(SrcDriver);
        string start = MemberSlice(drv, "void Start()");
        Tr(start.Length > 1000, "F8 **前提**：`BattleDriver.Start()` 切片取到了");
        Tr(start.Contains("TakePendingPlayMode()"),
           "F9 ★回放支 `Start()` 里显式 `TakePendingPlayMode();`（丢掉上一场入口窗留下的模式号）"
         + "｜删掉 ⇒ 红：放一局录像会把残留的 `Tutorial(4)` / `ClosedDeck(7)` 带进这一局");
        {
            int a = start.IndexOf("TakePendingPlayMode()", StringComparison.Ordinal);
            int b = start.IndexOf("PlayReplay(pendingReplay)", StringComparison.Ordinal);
            Tr(a >= 0 && b >= 0 && a < b,
               "F10 ★次序判别式：`TakePendingPlayMode()` 必须排在 `PlayReplay(pendingReplay)` **之前**（先丢通道、再起播）"
             + "｜两句对调 ⇒ 红（通道在 `Begin` 里会被读成这一局的模式号）");
        }
        Tr(drv.Contains("int tutIndex = TakePendingTutorialStage(); var pendingPlayMode = TakePendingPlayMode();"),
           "F11 ⛔ **回放契约没被改**：`BeginFromDeckLibrary` 里那一句「先取再分支」原样保留（本件的改动只在 `Start()` 的回放支）");
    }

    // ============================================================ G · AUD03 #73：切场景收窗
    static void SectionG_SceneSwitchClosesWindows()
    {
        MenuCheck.Section(_sink, "G · AUD03 #73：切场景时窗口层要收（壳 DDOL ⇒ 不显式收 = 上一屏的窗还活着）");

        string sh = Src("CardPresentation/Shell/ShellRuntime.cs");
        Tr(sh.Length > 1000, "G0 **前提**：`ShellRuntime.cs` 源码读得到");
        if (sh.Length < 1000) return;

        string hook = MemberSlice(sh, "static void OnSceneLoadedCleanupToast(");
        Tr(hook.Length > 100, "G1a **前提**：`OnSceneLoadedCleanupToast` 切片取到了");
        Tr(hook.Contains("CloseAllWindows()"),
           "G1b ★切场景的**全局**钩子里调 `WindowsManager.CloseAllWindows()`（覆盖所有 `LoadScene` 路径：`LoadSceneWithFade` 与直调都算）"
         + "｜删掉这一句 ⇒ 红：壳 `DontDestroyOnLoad` ⇒ 进战斗 / 回主菜单时**上一屏的模式窗还活着**并盖在新场景上");
        Tr(hook.Contains("WindowsManager.Instance != null"),
           "G2 调之前判 `Instance != null`（壳被拆那一支不 NRE）");
        Tr(!hook.Contains("openWindows["),
           "G3 ★灭自证：**不是**逐扇 `openWindows[i].Close()` —— 那一圈最后一拍会 `NotifyClosed → ShowPreviousWindow`，"
         + "把主菜单**提前开回来**（判据 `WindowsManager.cs:695-703`）｜改成逐扇关 ⇒ 红");

        string ensure = MemberSlice(sh, "static void EnsureSceneCleanupHook()");
        Tr(ensure.Contains("sceneLoaded += OnSceneLoadedCleanupToast"),
           "G4 这只钩子确实被 `EnsureSceneCleanupHook()` 挂上（`Build()` 里调、`OnDestroy` 撤）—— 否则 G1b 那句永远不跑");
    }

    // ============================================================ H · AUD08：离线 bot 对手名
    static void SectionH_OfflineBotName()
    {
        MenuCheck.Section(_sink, "H · AUD08：离线 bot 对手名（原版 `MatchData.SetBotOpponent` 本地生成；我们原来恒空串）");

        // H1/H2 · 行为：摇 30 次，长度全在 [4,13]（原版 `GenerateName(4, Random.Range(0,10)+4)` ⇒ 上限 4..13）
        int n = 30, minLen = 99, maxLen = -1, empty = 0;
        var seen = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < n; i++)
        {
            string s = BattleDriver.RollOfflineFoeNameForTest();
            if (string.IsNullOrEmpty(s)) { empty++; continue; }
            seen.Add(s);
            if (s.Length < minLen) minLen = s.Length;
            if (s.Length > maxLen) maxLen = s.Length;
        }
        Tr(empty == 0, "H1 ★离线 bot 名**非空**（摇 " + n + " 次，" + empty + " 次空串）｜把 `FoeDisplayName()` 退回空串 ⇒ 红");
        Tr(minLen >= 4 && maxLen <= 13,
           "H2 ★长度全在 [4,13]（实测 " + minLen + ".." + maxLen + "；原版 `NameGenerator.GenerateName(4, UnityEngine.Random.Range(0,10)+4)`，"
         + "判据 `decomp_full/MatchData__SetBotOpponent.c:100-112`）｜把实参改成别的界 ⇒ 红");
        Tr(seen.Count >= 2,
           "H3 ★灭自证：`" + n + "` 次里出现了 `" + seen.Count + "` 个不同的名字（⛔ 不是常量）｜写死一个字符串 ⇒ 红");

        // H4-H6 · 源码锁：接线点 + 联机口径没被改
        string src = Src(SrcDriver);
        string begin = MemberSlice(src, "public void Begin(");
        Tr(begin.Contains("_foeDisplayName = null;"),
           "H4 `Begin()` 每局把 `_foeDisplayName` 清成 null（否则第二局沿用上一局的名字）｜删掉 ⇒ 红");
        Tr(src.Contains("_enemyText.SetText(PlateName(FoeDisplayName()))"),
           "H5 名牌那一格走 `FoeDisplayName()`（原版 `EnemyNameText` 收的就是这个名字）｜改回恒空 ⇒ 红");
        Tr(src.Contains("EnemyName = FoeDisplayName() ?? \"\""),
           "H6 战报 `EnemyName` 也走同一个口（⛔ 两处别各算一份）｜改回恒空 ⇒ 红");

        string foe = MemberSlice(src, "string FoeDisplayName()");
        Tr(foe.Length > 100 && foe.Contains("if (_net != null) return NetMatchmaking.FoeName;"),
           "H7 ★**联机局不被覆盖**：`FoeDisplayName()` 第一句就是 `if (_net != null) return NetMatchmaking.FoeName;`"
         + "（联机仍用对端真名）｜把它挪到生成之后 / 删掉 ⇒ 红");
        string roll = MemberSlice(src, "static string RollOfflineFoeName()");
        Tr(roll.Length > 60 && roll.Contains("UnityEngine.Random.Range(4, 14)"),
           "H8 用的是 **`UnityEngine.Random`** 且界为 `Range(4, 14)`（= 原版 `Range(0,10)+4`；⛔ 别换成 `System.Random` / `ctx.Rng`）");
    }

    // ============================================================ 工具
    static string Src(string assetRelativePath)
    {
        try
        {
            string p = Path.Combine(Application.dataPath, assetRelativePath);
            return File.Exists(p) ? File.ReadAllText(p) : "";
        }
        catch (Exception e) { Debug.LogWarning("[B108A2] 读源码失败 `" + assetRelativePath + "`：" + e.Message); return ""; }
    }

    /// <summary>从一条**方法声明**起截到下一个「8 空格缩进的类成员声明」之前。
    /// <para>⚠️ 只是「够用的近似切片」：本件只用它做 `Contains` / 计数，⛔ 不拿它判语法结构。</para></summary>
    static string MemberSlice(string src, string declMarker)
    {
        int i = src.IndexOf(declMarker, StringComparison.Ordinal);
        if (i < 0) return "";
        int from = i + declMarker.Length;
        var m = Regex.Match(src.Substring(from),
            "\n {8}(?:public|private|protected|internal|static|sealed|readonly|void|bool|int|float|string|double|long|IEnumerator|System\\.Collections|List<|Dictionary<)");
        int end = m.Success ? from + m.Index : src.Length;
        return src.Substring(i, end - i);
    }
}
