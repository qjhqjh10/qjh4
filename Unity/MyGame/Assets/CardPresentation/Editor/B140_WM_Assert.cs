// B140_WM_Assert.cs — 执行代理 `B140_WM` 的断言宿主（2026-11-04）
//
// 覆盖「回合 / 计时」两件（判据全部来自第一权威反编译）：
//   A. `matchType` 按模式取 + `ClockManager.GetTotalTime` 三步（原版 `ClockManager__GetTotalTime.c:10-36`）
//      · matchType 来源 = `MatchData..ctor` + `MatchData__SetBotOpponent.c:27-41`（有电脑时翻转一次）
//      · 秒数：练习(50) **600** · 遭遇(200/210) **60** · 排位(10/20) **60** · EventAI(80) **240**
//   B. 教程 / 战役局**不跑回合时钟**（原版 `ClockManager__StartTimer.c:14-33` 在置数之前就 return）
//      · 练习 / 遭遇**照跑**（反向·防一刀切）
//
// 用法（⚠️ 要**主对话**跑 —— 子代理不许 `-executeMethod`）：
//   unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.23f1/Editor/Unity.exe" -batchmode -quit \
//     -projectPath "D:\4\Unity\MyGame" -executeMethod B140_WM_Assert.Run -logFile -
//   （Unity 路径按本机现读，见 `CLAUDE.md` §二）
//
// 判绿红：`grep "^B140_WM"` 的**合计行**；逐条按**行首** `B140_WM ✓` / `B140_WM ✗` 数
//   （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
// 灭自证（改坏法 → 哪一组红）：
//   · 把 `ApplyMatchTypeFromContext()` 改成 `matchType = 80` ⇒ **A 组**红（练习/遭遇/排位三条）。
//   · 把 `ResetClock` 里那道 `ClockSuppressedForThisMatch ? 0f : …` 去掉 ⇒ **B 组**红（教程/战役两条）。
//   · 把 `TurnSecondsFor` 的 `80 → 240` 或 `50 → 600` 删掉 ⇒ **A 组**红。
//
// 宿主状态要求：**零前提** —— 不读场景、不读存档、不读真对局；每个夹具自建临时根并在收尾 `DestroyImmediate`。
//   ⚠️ 只装**最小 `Ctx`**（`PlayMode` / `Active`），`Ctx.Active = 1` 与 `_me`(0) 不同 ⇒ `ResetClock`
//      不会播 `StartTurn`、不会抬头（那两件不属本单，也不该被这里带出来）。
using UnityEngine;
using CardPresentation;
using RuleEngine;

public static class B140_WM_Assert
{
    static int _pass, _fail;

    public static int Pass { get { return _pass; } }
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B140_WM ✓ " + msg); }
        else { _fail++; Debug.LogError("B140_WM ✗ " + msg); }
    }

    static void CheckNear(float got, float want, float tol, string msg)
    {
        Check(Mathf.Abs(got - want) <= tol,
              msg + "（期望 " + want.ToString("F1") + " ± " + tol.ToString("F4")
                  + "；实得 " + got.ToString("F1") + "）");
    }

    public static void Run()
    {
        _pass = 0; _fail = 0;
        try
        {
            A_MatchTypeAndSeconds();
            B_ClockGate();
        }
        catch (System.Exception ex)
        {
            _fail++;
            Debug.LogError("B140_WM ✗ 夹具异常：" + ex);
        }
        Debug.Log("B140_WM 合计 " + _pass + " 通过 / " + _fail + " 失败");
    }

    // ==================================================================
    //  A · matchType 按模式取 + GetTotalTime 三步
    // ==================================================================
    //  判据：`ClockManager__GetTotalTime.c:10-36` —— ① `clockTimeLimit` → ② 缩时标志 ⇒ `clockTimeLimitReduced`
    //        → ③ `matchType == 0x50`(80) ⇒ 240 · `== 0x32`(50) ⇒ 600（**覆盖在缩时之后**）。
    //  🧨 把 `ApplyMatchTypeFromContext()` 改回 `matchType = 80` ⇒ A4 那一组红。
    static void A_MatchTypeAndSeconds()
    {
        // ---- A1 模式号 → matchType（原版 `MatchData..ctor` 派发表 + `SetBotOpponent` 翻转）----
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.OfflinePractice, false) == 50,
              "A1 练习(OfflinePractice) ⇒ matchType **50**");
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.Skirmish, false) == 200,
              "A2 遭遇(Skirmish) ⇒ matchType **200**（无电脑）");
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.Skirmish, true) == 210,
              "A2b 遭遇 + 电脑 ⇒ **210**（原版 `MatchData.SetBotOpponent` 翻转一次）");
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.Classic, false) == 10,
              "A3 排位(Classic) ⇒ matchType **10**（无电脑）");
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.Classic, true) == 20,
              "A3b 排位 + 电脑 ⇒ **20**（同上翻转）");
        Check(BattleDriver.MatchTypeForPlayMode(GameMode.Dungeon, false) == 80,
              "A4 EventAI(Dungeon) ⇒ matchType **80**");

        // ---- A2 秒数逐值对原版（`turnSeconds` = 60 · `reducedTurnSeconds` = 10）----
        CheckNear(BattleDriver.TurnSecondsFor(50, false, 60f, 10f), 600f, 1e-4f, "A5 练习(50) ⇒ **600 s**");
        CheckNear(BattleDriver.TurnSecondsFor(200, false, 60f, 10f), 60f, 1e-4f, "A6 遭遇(200) ⇒ **60 s**");
        CheckNear(BattleDriver.TurnSecondsFor(210, false, 60f, 10f), 60f, 1e-4f, "A6b 遭遇+电脑(210) ⇒ **60 s**");
        CheckNear(BattleDriver.TurnSecondsFor(10, false, 60f, 10f), 60f, 1e-4f, "A7 排位(10) ⇒ **60 s**");
        CheckNear(BattleDriver.TurnSecondsFor(20, false, 60f, 10f), 60f, 1e-4f, "A7b 排位+电脑(20) ⇒ **60 s**");
        CheckNear(BattleDriver.TurnSecondsFor(80, false, 60f, 10f), 240f, 1e-4f, "A8 EventAI(80) ⇒ **240 s**");
        // ③ 次序：覆盖排在缩时**之后**（原版同序）⇒ 80 那一档缩时也压不动它；10 那一档缩时立刻生效
        CheckNear(BattleDriver.TurnSecondsFor(80, true, 60f, 10f), 240f, 1e-4f,
                  "A9 缩时标志对 80 **无效**（matchType 覆盖在缩时之后 —— 原版同序）");
        CheckNear(BattleDriver.TurnSecondsFor(10, true, 60f, 10f), 10f, 1e-4f,
                  "A10 缩时标志对 10 **生效** ⇒ 10 s");

        // ---- A3 接线：`Begin` 那句 `ApplyMatchTypeFromContext()` 走的就是上面这两件 ----
        //  ⚠️ 这里调的是**产品那一个赋值口**（`ApplyMatchTypeFromContextForTest` → `ApplyMatchTypeFromContext`），
        //     不是另写一份表达式 —— 所以把它改成 `matchType = 80` 这三条会红。
        var root = new GameObject("B140_WM_A");
        var drv = root.AddComponent<BattleDriver>();
        try
        {
            var cases = new[]
            {
                new { Mode = GameMode.OfflinePractice, ExpectMt = 50,  ExpectSec = 600f, Name = "练习" },
                new { Mode = GameMode.Skirmish,        ExpectMt = 200, ExpectSec = 60f,  Name = "遭遇" },
                new { Mode = GameMode.Classic,         ExpectMt = 10,  ExpectSec = 60f,  Name = "排位" },
                new { Mode = GameMode.Dungeon,         ExpectMt = 80,  ExpectSec = 240f, Name = "EventAI" },
            };
            foreach (var c in cases)
            {
                var ctx = new BattleContext(0);
                ctx.PlayMode = c.Mode;            // `BotSeat` 留 -1 ⇒ 不翻转（单机练习那条对照）
                ctx.Active = 1;                   // ≠ `_me`(0) ⇒ `SetCtxForTest` 里那次 `ResetClock` 不播 StartTurn / 不抬头
                drv.SetCtxForTest(ctx);           // 已有夹具口（它会顺带 `ResetClock()` 一次）
                drv.ApplyMatchTypeFromContextForTest();
                drv.SimulateResetClockForTest();  // 算完号再拨一次表，确保读到的是按模式算出来的时长
                Check(drv.matchType == c.ExpectMt,
                      $"A11 {c.Name}：`Begin` 那一句算出的 matchType = **{drv.matchType}**（期望 {c.ExpectMt}）"
                    + "｜🧨 把它改回 `matchType = 80` ⇒ 练习/遭遇/排位三条红");
                CheckNear(drv.TurnSecondsForThisMatch, c.ExpectSec, 1e-3f,
                          $"A12 {c.Name}：本局时长 = **{drv.TurnSecondsForThisMatch:F0} s**（原版 GetTotalTime）");
            }
        }
        finally { Object.DestroyImmediate(root); }
    }

    // ==================================================================
    //  B · 教程 / 战役局不跑回合时钟；练习 / 遭遇照跑
    // ==================================================================
    //  判据：`ClockManager__StartTimer.c:14-33` —— `IsCampaignLike` ∨ `IsTutorialMatch` ⇒ **置数之前 return**
    //        ⇒ 表不走、无倒计时、不会自动结束回合（我们的等价物 = `_clockLeft` 停在 0，`TickClock` 早退）。
    //  🧨 把 `ResetClock` 里那道闸去掉 ⇒ 教程/战役两条「ClockLeft == 0」红。
    static void B_ClockGate()
    {
        // ---- B1 不跑表的两档（教程 100 · 战役 130）----
        foreach (var mode in new[] { GameMode.Tutorial, GameMode.Campaign })
        {
            var root = new GameObject("B140_WM_B_off_" + mode);
            var drv = root.AddComponent<BattleDriver>();
            try
            {
                var ctx = new BattleContext(0);
                ctx.PlayMode = mode;
                ctx.Active = 1;                    // ≠ `_me`(0) ⇒ 不播 StartTurn、不抬头（本组只验时钟）
                drv.SetCtxForTest(ctx);            // 已有夹具口（顺带 `ResetClock()` 一次）
                drv.ApplyMatchTypeFromContextForTest();
                drv.SimulateResetClockForTest();   // 算完号再拨一次表（本组要验的就是这一次的落点）
                // 前提（硬检查，⛔ 不用 `if (前提) Check(结论)`）：
                Check(drv.ClockSuppressedForThisMatch,
                      $"B1 前提：{mode}（matchType {(int)ctx.MatchType}）确实是「不跑表」那一档"
                    + "（原版 `IsCampaignLike`∨`IsTutorialMatch`）");
                CheckNear(drv.ClockLeft, 0f, 1e-4f,
                          $"B2 {mode}：`ResetClock` 后**时钟没启**（ClockLeft = {drv.ClockLeft:F1} s）"
                        + "｜🧨 删掉 `ResetClock` 里那道闸 ⇒ 变 60 ⇒ 红");
                Check(drv.ClockCountingDown == false, $"B3 {mode}：不在 15 s 倒计时那一段（表没走过，不会自动结束回合）");
            }
            finally { Object.DestroyImmediate(root); }
        }

        // ---- B2 照跑表的两档（练习 50 · 遭遇 200）----
        foreach (var mode in new[] { GameMode.OfflinePractice, GameMode.Skirmish })
        {
            var root = new GameObject("B140_WM_B_on_" + mode);
            var drv = root.AddComponent<BattleDriver>();
            try
            {
                var ctx = new BattleContext(0);
                ctx.PlayMode = mode;
                ctx.Active = 1;
                drv.SetCtxForTest(ctx);
                drv.ApplyMatchTypeFromContextForTest();
                drv.SimulateResetClockForTest();
                float want = BattleDriver.TurnSecondsFor(drv.matchType, false, drv.turnSeconds, drv.reducedTurnSeconds);
                Check(!drv.ClockSuppressedForThisMatch,
                      $"B4 前提：{mode}（matchType {(int)ctx.MatchType}）**不在**「不跑表」那两档");
                Check(want > 0f, $"B5 前提：{mode} 的期望时长 > 0（实得 {want:F1} s）");
                CheckNear(drv.ClockLeft, want, 1e-3f,
                          $"B6 {mode}：时钟**照跑**（ClockLeft = {drv.ClockLeft:F1} s = GetTotalTime 算出来的 {want:F1} s）");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
