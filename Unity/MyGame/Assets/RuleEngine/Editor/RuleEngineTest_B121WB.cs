// RuleEngineTest_B121WB.cs — 执行写手 `B121-WB`（2026-11-03）· `B120-E07`：**AI 难度「这一局真的按表挑了」端到端**
//
// ============================ 它盯的是什么 ============================
//   `B120-W8` 把原版 `AIBotsConfig.BotList` 那条链的**表 + 挑选式 + 接线**落地了，但它的 §七·5 如实记着
//   「**端到端「这一局真的按表挑了」没验** —— 那要真开一局 bot 对局（`Ctx.BotSeat >= 0` + LiveOps 已 Setup）」。
//   本文件补的正是那一条，外加 `B120-W8` 的「FTUE 三格不建死字段」这一条**机检**。
//
//   A 段：`SimpleAI.BotTierFtue` 的成员集 —— 原版那三格（`waitTime` / `justForRanked` /
//         `shouldUseFirstGameLogic`）**本地没有消费方 ⇒ 不建死字段**（建了就是死格）。
//   B 段：**真开局 + LiveOps 夹具** ⇒ `driver.aiDifficulty` 真的等于
//         `SimpleAI.SelectNormal(RankedData.Points 夹 int, lossStreak)`；换一份夹具（连败 0 ↔ 5）⇒ 难度**跟着变**。
//   C 段：FTUE 那一支同理 —— `GameMode.OfflinePractice`（→ `MatchType.PracticeOffline`，不在排除集里）
//         + 战绩夹具 `Practice.Won = 3` ⇒ 难度 = `SelectFtue(3)`。
//
// ============================ 判据 ============================
//   · 挑选式：`decomp_full/MatchData__GetNormalBotConfig.c:60-78`（`lossStreak ∈ [min,max] ∧ rank ∈ [min,max]`，
//     逐条 first-match、全不命中 ⇒ `[0]`）· `MatchData__GetFTUEBotConfig.c:46-72`（`winCount ==` 精确相等）
//   · FTUE 闸：`decomp_full/MatchData__IsInFTUEState.c:25-60`（排除集 `{10,20,110,120,170,180,150}` + `winCount ≤ max`）
//   · 量纲：`MatchData__GetNormalBotConfig.c:54` 的 `rank` = **排位分**（`IPlayEventWithLeaderboard` 槽 1 = `get_CurrentScore`），
//     ⛔ 不是段位号 ⇒ 我们的等价 = `RankedData.Points`
//   · 前置：`B120_W8` 的执行报告 `资料/完全复刻/报告/B120_W8_执行.md` §一/§二/§三
//
// ============================ 🔴 灭自证 ============================
//   🧨 B3：把 `AssignAiDifficultyForThisMatch()` 从 `Begin` 里删掉（或改成恒 `Normal`）⇒ B2/B3 红。
//   🧨 B4：让 `LossStreakForBotConfig()` 读死值（不看 LiveOps 夹具）⇒ 两份夹具给同一档 ⇒ B4 红。
//   🧨 C2：把 `IsInFtueState` 的排除集 / `≤ max` 改动 ⇒ C 段红（`Practice` 不该被排除）。
//   🧨 A2：给 `BotTierFtue` 加上那三个死字段 ⇒ A2 红。
//
// ============================ 覆盖边界（如实记）============================
//   · **表的区间与档位本身是 🔶 自己设计**（原版 `BotList` 在 PlayFab TitleData，关服后判据在远端）——
//     本件钉的是「**挑选式 + 接线 + 读数**」，⛔ 不是「与原版表逐值一致」。
//   · 本件会**往全局注册表里塞一个夹具 `GameModesHandler`** 并在 `finally` 里把原来那一份注册回去；
//     它还会**重开一局**（`driver.Begin`）⇒ 请接在宿主聚合行的**靠后**位置（建议 `B120_W8_Assert.Run()` 之后）。
//   · `driver == null`（没有活驱动）时 B/C 两段**报红而不是跳过**（⛔ 静默跳过 = 假绿）。
using System;
using System.Collections.Generic;
using System.Reflection;
using CardPresentation;
using RuleEngine;
using UnityEngine;

/// <summary>`B120-E07`：AI 难度**端到端**（真开局 + LiveOps 夹具）+ FTUE 三格「不建死字段」的机检。</summary>
public static class RuleEngineTest_B121WB
{
    static int _ok, _bad;
    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log("B121WB_AI ✓ " + what); }
        else { _bad++; Debug.LogError("B121WB_AI ✗ " + what); }
    }

    static void Group(string tag, Action body)
    {
        try { body(); }
        catch (Exception e)
        {
            Check(false, tag + " 组**抛异常**（本组后面的断言没跑到，⛔ 别读成通过）：" + e.GetType().Name + ": " + e.Message);
        }
    }

    /// <summary>宿主入口。`driver` 为 null ⇒ B/C 两段报红（它们要一台**真开过局**的驱动）。</summary>
    public static void Run(BattleDriver driver)
    {
        _ok = 0; _bad = 0;
        Debug.Log("B121WB_AI === B120-E07 AI 难度端到端（真开局 + LiveOps 夹具）===");
        Group("A·FTUE 三格不建死字段", A_FtueStructShape);
        Group("B·真开局 ⇒ Normal 表", () => B_NormalEndToEnd(driver));
        Group("C·真开局 ⇒ FTUE 表", () => C_FtueEndToEnd(driver));
        Debug.Log("B121WB_AI 合计：" + _ok + " 通过 / " + _bad + " 失败");
    }

    // ==================================================================
    //  A · `BotTierFtue` 的成员集：原版那三格**不建死字段**
    // ==================================================================
    static void A_FtueStructShape()
    {
        var t = typeof(SimpleAI.BotTierFtue);
        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        bool hasWin = false, hasKnobs = false;
        var names = new List<string>();
        for (int i = 0; i < fields.Length; i++)
        {
            names.Add(fields[i].Name);
            if (string.Equals(fields[i].Name, "WinCount", StringComparison.OrdinalIgnoreCase)) hasWin = true;
            if (string.Equals(fields[i].Name, "Knobs", StringComparison.OrdinalIgnoreCase)) hasKnobs = true;
        }

        Check(hasWin && hasKnobs,
              "A1 `BotTierFtue` 有挑选要用的两格（`WinCount` / `Knobs`）—— 实得 [" + string.Join(",", names.ToArray()) + "]");

        // 原版 `BotConfigFTUE` 还有 `waitTime` / `justForRanked` / `shouldUseFirstGameLogic`
        // —— 本地**没有消费方**（`B120_W8` 报告 §七·2）⇒ 建了就是死格。这条断言把它钉死。
        bool hasDead = false;
        for (int i = 0; i < fields.Length; i++)
        {
            string n = fields[i].Name;
            if (n.IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("justForRanked", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("shouldUseFirstGameLogic", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("firstGameLogic", StringComparison.OrdinalIgnoreCase) >= 0) hasDead = true;
        }
        Check(!hasDead,
              "A2 ★★ **`waitTime` / `justForRanked` / `shouldUseFirstGameLogic` 一个都没建**"
            + "（原版那三格的消费方不在难度链上；本地没有消费点 ⇒ 建了就是死字段，⛔ 别「先占位」）"
            + "｜🧨 加上任何一个 ⇒ 红");
    }

    // ==================================================================
    //  B · 真开局 ⇒ `SelectNormal(排位分, 连败)`
    // ==================================================================
    static void B_NormalEndToEnd(BattleDriver driver)
    {
        if (!CheckDriver(driver, "B")) return;

        var saved = LiveOpsManager.GetHandler<GameModesHandler>();
        try
        {
            int rank = ClampRank(RankedData.Points);

            // ---- 夹具①：Classic（→ `MatchType.RankedBot`，在 FTUE 排除集里）· 连败 5 ----
            RegisterFixture("{\"Classic\":{\"lossStreak\":5}}");
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261103, playMode: GameMode.Classic);
            Check(driver.Ctx != null && driver.Ctx.BotSeat >= 0,
                  "（前提·B1）真开了一局 bot 对局（`BotSeat` = " + (driver.Ctx != null ? driver.Ctx.BotSeat : -99) + "）"
                + " —— 没有电脑对手时 `SimpleAI` 不驱动、这个值不产生行为，本条不成立就说明夹具没生效");
            Check(driver.Ctx != null && driver.Ctx.PlayMode == GameMode.Classic,
                  "（前提·B1b）`Ctx.PlayMode == Classic`（实得 " + (driver.Ctx != null ? driver.Ctx.PlayMode.ToString() : "null") + "）");

            var expect5 = SimpleAI.SelectNormal(rank, 5);
            Check(driver.aiDifficulty == expect5,
                  "B2 ★★ **这一局真的按表挑了**：`aiDifficulty = " + driver.aiDifficulty + "` ≡ "
                + "`SelectNormal(" + rank + ", 5) = " + expect5 + "`（排位分取 `RankedData.Points` 夹 int）"
                + "｜🧨 把 `AssignAiDifficultyForThisMatch()` 从 `Begin` 删掉 / 改成恒 `Normal` ⇒ 红");
            Check(driver.aiDifficulty != AiDifficulty.Normal,
                  "B3 ★★ **灭自证**：难度 **≠ 兜底的 `Normal`**（实得 " + driver.aiDifficulty + "）"
                + " —— 证明它真的读了表，不是留在字段初值上");

            // ---- 夹具②：同一模式、连败改成 0 ⇒ 难度必须**跟着换** ----
            RegisterFixture("{\"Classic\":{\"lossStreak\":0}}");
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261103, playMode: GameMode.Classic);
            var expect0 = SimpleAI.SelectNormal(rank, 0);
            Check(driver.aiDifficulty == expect0,
                  "B4 ★★ **换一份夹具 ⇒ 难度跟着换**：`aiDifficulty = " + driver.aiDifficulty + "` ≡ "
                + "`SelectNormal(" + rank + ", 0) = " + expect0 + "`"
                + "｜🧨 让 `LossStreakForBotConfig()` 读死值（不看 LiveOps）⇒ 两局同一档 ⇒ 红");
            Check(expect5 != expect0 && driver.aiDifficulty != expect5,
                  "B5 ★ 两份夹具给的是**不同**档（连败 5 ⇒ " + expect5 + " · 连败 0 ⇒ " + expect0 + "）");
        }
        finally
        {
            if (saved != null) LiveOpsManager.Register(saved);      // 原样还原全局注册表（⛔ 别把夹具留给后面的人）
        }
    }

    // ==================================================================
    //  C · 真开局 ⇒ FTUE 表（按该模式胜场）
    // ==================================================================
    static void C_FtueEndToEnd(BattleDriver driver)
    {
        if (!CheckDriver(driver, "C")) return;

        var saved = LiveOpsManager.GetHandler<GameModesHandler>();
        try
        {
            RegisterFixture("{\"ResultCounter\":{\"Practice\":{\"Won\":3}}}");
            driver.Begin(BattleDriver.DefaultFactionA, BattleDriver.DefaultFactionB, 20261103, playMode: GameMode.OfflinePractice);
            Check(driver.Ctx != null && driver.Ctx.BotSeat >= 0,
                  "（前提·C1）练习局也是 bot 对局（`BotSeat` = " + (driver.Ctx != null ? driver.Ctx.BotSeat : -99) + "）");
            Check(driver.Ctx != null && driver.Ctx.PlayMode == GameMode.OfflinePractice,
                  "（前提·C1b）`Ctx.PlayMode == OfflinePractice`（实得 "
                + (driver.Ctx != null ? driver.Ctx.PlayMode.ToString() : "null") + "）");

            var mt = MatchTypes.Effective(driver.Ctx);
            Check(!SimpleAI.IsInFtueState(MatchType.Ranked, 3),
                  "（前提·C2）排除集生效：`MatchType.Ranked` 下 `IsInFtueState` 恒 false（判据 `MatchData__IsInFTUEState.c:25-60`）");
            Check(SimpleAI.IsInFtueState(mt, 3),
                  "（前提·C2b）本局 `matchType = " + mt + "` **不在排除集**且胜场 3 ≤ 上限 " + SimpleAI.FtueMaxWinCount
                + " ⇒ 走 FTUE 表");

            var expect = SimpleAI.SelectFtue(3);
            Check(driver.aiDifficulty == expect,
                  "C2 ★★ **FTUE 那一支也真的按表挑了**：`aiDifficulty = " + driver.aiDifficulty + "` ≡ "
                + "`SelectFtue(3) = " + expect + "`（战绩夹具 `Practice.Won = 3`）"
                + "｜🧨 把 FTUE 那一支删掉 / 把 `Practice` 塞进排除集 ⇒ 红");
            Check(SimpleAI.SelectFtue(3) != SimpleAI.SelectFtue(0),
                  "C3 ✋ 灭自证的一半：FTUE 表**按胜场变档**（`SelectFtue(0) = " + SimpleAI.SelectFtue(0)
                + "` ≠ `SelectFtue(3) = " + SimpleAI.SelectFtue(3) + "`）—— 否则 C2 那条等式证明不了「读了胜场」");
        }
        finally
        {
            if (saved != null) LiveOpsManager.Register(saved);
        }
    }

    /// <summary>前提闸：没有活驱动 ⇒ **报红**（⛔ 不静默跳过 —— 跳过就是假绿）。</summary>
    static bool CheckDriver(BattleDriver driver, string tag)
    {
        if (driver == null)
        {
            Check(false, tag + " 段**没跑**：宿主没传驱动（这一段的结论 = 未验证，⛔ 别读成通过）");
            return false;
        }
        return true;
    }

    /// <summary>把一份 `sharedData` JSON 包成 `GameModesHandler` 并登记（= `GameModesCheck.MakeHandler` 同一个口）。</summary>
    static void RegisterFixture(string sharedJson)
    {
        var j = LiveOpsJson.Parse(sharedJson);
        var h = new GameModesHandler();
        var t = h.SetupEvents(new List<ILiveOps>(), new List<ILiveOps>(), j);
        if (!t.IsCompleted) t.Wait();
        LiveOpsManager.Register(h);
    }

    /// <summary>原版 `IRankedEvent.CurrentScore` 是 `int`，我们用 `long` 存点 ⇒ 夹到 `[0, int.MaxValue]`
    /// （与 `BattleDriver.RankedScoreForBotConfig` 同一个口径；这里**只读**地重算一次，别去读私有方法的返回值）。</summary>
    static int ClampRank(long p)
    {
        if (p < 0L) return 0;
        return p > int.MaxValue ? int.MaxValue : (int)p;
    }
}
