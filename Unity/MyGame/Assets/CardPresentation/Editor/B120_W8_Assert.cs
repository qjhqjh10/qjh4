// B120_W8_Assert.cs — 执行写手 `B120-W8`（2026-11-02）· AI 难度表（原版 `AIBotsConfig.BotList` 链的本地等价）
//
// ============================ 它盯的是什么 ============================
//  A 段 · 表结构（Normal / FastMode）：每行区间合法 · 两两不重叠 · **满覆盖且每点唯一命中**（边界点集采样）
//  B 段 · `KnobsOf` 一致：四个难度档的旋钮值仍是 `0.9/3 · 0.6/2 · 0.3/1 · 0/0`（🔶 自定值，钉住别静默改）
//  C 段 · FTUE：`SelectFtue(winCount)` 按 `winCount` 精确命中 · 全不命中取 `[0]` · `FtueMaxWinCount`
//  D 段 · FTUE 闸 `IsInFtueState`：原版那条七档排除集 + `winCount ≤ max(表内 winCount)` + 取不到(-1) ⇒ false
//  E 段 · 模式键 `ResultCounterKeyForMode`（原版 `MatchData.CurrentModeVictories` 那张表）
//  F 段 · **接线**（源码级）：`BattleDriver` 里 `aiDifficulty = SimpleAI.SelectNormal(` 恰好 1 处、
//         `SimpleAI.SelectFtue(` 恰好 1 处、`AssignAiDifficultyForThisMatch();` 与 `_playMode = Ctx.PlayMode;` **同一行**、
//         `SimpleAI.SelectFastMode(` **0 处**（原版不把 FastMode 表喂难度旋钮 —— 见文件头判据）
//
// ============================ 🔴 灭自证（改哪一处会让哪一条红）============================
//  🧨 把 `SelectNormal` 改成「恒返回 `[0]`」⇒ A-A4 / A-A5 红（`SelectNormal(3000,0)` 应当是 `Hard`、`SelectNormal(0,0)` 应当是 `Normal`，
//     两个点会**相等**），同时 Z-1/Z-2 也红。
//  🧨 把某条区间的上界改到与下一条重叠（例：`0..1499` 改成 `0..1500`）⇒ A-A3 红（边界点上命中 2 条）。
//  🧨 把某条区间的下界改小留下空隙（例：`1500..` 改成 `1600..`）⇒ A-A3 红（命中 0 条 ⇒ 只能落到 `[0]` 兜底）。
//  🧨 把某条的 `Knobs` 换成另一个档（例：低分段无连败 Normal→Easy）⇒ A-A4 的「矩形内中点返回本行档位」红。
//  🧨 把 `KnobsOf` 的数值改掉（例：`Hard` 的 `SkipChance` 改成 10）⇒ B 段红。
//  🧨 把 `SelectFtue` 的兜底 `[0]` 改成 `Normal` ⇒ C2 红。
//  🧨 把 `IsInFTUEState` 的排除集删掉 ⇒ D1 红；把 `winCount ≤ max` 改成 `< max` ⇒ D2 红。
//  🧨 把 `AssignAiDifficultyForThisMatch();` 从 `Begin` 那条物理行删掉 ⇒ F3 红（表再好也没人用）。
//  🧨 把 Skirmish 接到 `SimpleAI.SelectFastMode` ⇒ F4 红（**原版不接**，见 E/F 段的判据）。
//
// ============================ 覆盖边界（如实记）============================
//  · **不跑 Unity 帧循环、不需要 `BattleDriver` 实例**：A–E 段是纯静态函数 / 表，F 段读源码文本。
//  · **不断言「这一局真的按表挑出来了」**：那要真开一局 bot 对局（`Ctx.BotSeat >= 0` + LiveOps 已 Setup），
//    本件只钉「表 + 挑选式 + 接线在」。宿主 `BattleScene` 里已有的开局对局覆盖 `Begin` 链本身。
//  · **值本身是 🔶 自己设计**（原版表在远端，本机 0 命中）⇒ B 段钉的是「别静默改」，不是「与原版一致」。
using System;
using System.Collections.Generic;
using System.IO;
using RuleEngine;
using UnityEngine;
using MatchType = RuleEngine.MatchType;   // 🆕 2026-11-02：`System.IO.MatchType` 与 `RuleEngine.MatchType` 同名（CS0104）

/// <summary>执行写手 `B120-W8`：`AIBotsConfig` 难度表 / 挑选式 / FTUE 闸 / 接线 的读数口。
/// <para>⛔ 本件**只读**（不改表、不开局、不写棋盘、不碰随机数）。</para></summary>
public static class B120_W8_Assert
{
    const string Prefix = "[B120W8] ";
    static int _ok, _bad;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _ok; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _bad; } }

    static void Check(bool ok, string what)
    {
        if (ok) { _ok++; Debug.Log(Prefix + "✓ " + what); }
        else { _bad++; Debug.LogError(Prefix + "✗ " + what); }
    }

    /// <summary>本件唯一入口（宿主 `BattleScene.Run` 的聚合行里 `B120_W8_Assert.Run(); pass += …`）。</summary>
    public static void Run()
    {
        _ok = 0; _bad = 0;
        Debug.Log(Prefix + "=== B120-W8：AI 难度表（`AIBotsConfig` 的本地等价）开始 ===");
        CheckNormalTable();
        CheckFastModeTable();
        CheckKnobsConsistency();
        CheckFtue();
        CheckFtueGate();
        CheckResultCounterKeys();
        CheckDriverWiring();
        Debug.Log(Prefix + "=== B120-W8 合计：" + _ok + " 通过 / " + _bad + " 失败 ===");
    }

    // ==================================================================
    //  A · Normal 表：结构 + 覆盖 + first-match 语义
    //    判据 = `decomp_full/MatchData__GetNormalBotConfig.c:60-78`（逐条 first-match、双边闭合、全不命中 ⇒ `[0]`）
    // ==================================================================
    static void CheckNormalTable()
    {
        var t = SimpleAI.NormalBotTierTable;
        Check(t != null && t.Length == 6, "A-A0（前提）Normal 表是 6 档（🔶 自己设计：低分段 3 档 + 高分段 3 档），实得 "
            + (t == null ? "null" : t.Length.ToString())
            + "｜🧨 把两档合成一档 / 补一档 ⇒ 红（同时 A-A3 的覆盖会变）");
        if (t == null || t.Length == 0) return;

        bool rangeOk = true;
        for (int i = 0; i < t.Length; i++)
            if (t[i].MinRank > t[i].MaxRank || t[i].MinLossStreak > t[i].MaxLossStreak) rangeOk = false;
        Check(rangeOk, "A-A1 ★ 每行的区间都合法（`minRank ≤ maxRank` ∧ `minLossStreak ≤ maxLossStreak`）"
            + "｜🧨 把某一行的上界写到下界之前 ⇒ 红（那一行永远命中不了）");

        Check(CountOverlaps(t) == 0, "A-A2 ★ 六条矩形**两两不重叠**（rank 与 loss 两个轴都相交才算重叠），实得重叠对 = "
            + CountOverlaps(t)
            + "｜🧨 把 `0..1499` 改成 `0..1500` 这类「扩一格」⇒ 红（同一个点会命中两条）");

        CheckCoverage("A-A3", t);

        // 矩形内部中点 ⇒ 必须返回本行的档位（= first-match 扫到它的那一条）
        bool insideOk = true; string firstBad = null;
        for (int i = 0; i < t.Length; i++)
        {
            int r = Mid(t[i].MinRank, t[i].MaxRank);
            int l = Mid(t[i].MinLossStreak, t[i].MaxLossStreak);
            var got = SimpleAI.SelectNormal(r, l);
            if (got != t[i].Knobs)
            {
                insideOk = false;
                if (firstBad == null) firstBad = "第 " + i + " 行(rank=" + r + ", loss=" + l + ") 实得 " + got + " / 期望 " + t[i].Knobs;
            }
        }
        Check(insideOk, "A-A4 ★ 每条矩形**内部中点**都必须返回**本行的档位**（逐行）"
            + (insideOk ? "" : " —— " + firstBad)
            + "｜🧨 把某一行的 `Knobs` 换成别的档 / 让两条矩形重叠（先命中的那条会赢）⇒ 红");

        // 两个具体格（灭自证：「恒返回 [0]」会让这两条**相等**）
        var lowNoLoss = SimpleAI.SelectNormal(0, 0);
        var highNoLoss = SimpleAI.SelectNormal(3000, 0);
        Check(lowNoLoss == AiDifficulty.Normal, "A-A5a ★ `SelectNormal(0, 0)` = `Normal`（低分段 · 没有连败），实得 " + lowNoLoss);
        Check(highNoLoss == AiDifficulty.Hard, "A-A5b ★ `SelectNormal(3000, 0)` = `Hard`（高分段 · 没有连败），实得 " + highNoLoss);
        Check(lowNoLoss != highNoLoss, "A-A5c ★ 两个点**不许同档**（A5a/A5b 的合取）"
            + "｜🧨 把 `SelectNormal` 改成「恒返回 `[0]`」（= 原版兜底那一格）⇒ 3000 分也变 `Normal` ⇒ 本条 + A5b 同时红");

        // 单调性两条（strict）：段位越高越难 · 连败越多越简单
        Check((int)SimpleAI.SelectNormal(3000, 0) > (int)SimpleAI.SelectNormal(0, 0),
            "A-A6a ★ 单调一：同连败下，排位分高 ⇒ 档位**更硬**（`(int)Hard > (int)Normal`）"
            + "｜🧨 把高低分段的档位对调 ⇒ 红");
        Check((int)SimpleAI.SelectNormal(0, 4) < (int)SimpleAI.SelectNormal(0, 0),
            "A-A6b ★ 单调二：同分段下，连败越多 ⇒ 档位**更软**（`(int)SuperEasy < (int)Normal`）"
            + "｜🧨 把连败维度删掉（所有行 loss 都写 `0..int.MaxValue`）⇒ 两条相等 ⇒ 红");

        // 原版兜底：越界 / 负数输入 ⇒ `[0]`
        Check(SimpleAI.SelectNormal(-5, 0) == t[0].Knobs && SimpleAI.SelectNormal(0, -1) == t[0].Knobs,
            "A-A7 ★ 越界输入（`rank < 0` / `lossStreak < 0`）⇒ 落到 **`[0]`**（原版「一条都不命中取 `BotList[0]`」，不抛）"
            + "｜🧨 把兜底改成抛异常 / 返回 `Normal` ⇒ 红");
    }

    // ==================================================================
    //  A' · FastMode 表：结构与 Normal 同构（**独立数组**，原版就是两张表）
    // ==================================================================
    static void CheckFastModeTable()
    {
        var t = SimpleAI.FastModeBotTierTable;
        Check(t != null && t.Length == 6, "A-F0（前提）FastMode 表是**独立**的 6 档数组，实得 "
            + (t == null ? "null" : t.Length.ToString())
            + "｜🧨 把它改成 `= SimpleAI.NormalBotTierTable`（引用同一份）⇒ 红（原版是 `BotListFastMode` 与 `BotList` 两张表）");
        if (t == null || t.Length == 0) return;

        bool rangeOk = true;
        for (int i = 0; i < t.Length; i++)
            if (t[i].MinRank > t[i].MaxRank || t[i].MinLossStreak > t[i].MaxLossStreak) rangeOk = false;
        Check(rangeOk, "A-F1 ★ FastMode 每行区间合法");
        Check(CountOverlaps(t) == 0, "A-F2 ★ FastMode 六条矩形两两不重叠，实得重叠对 = " + CountOverlaps(t));
        CheckCoverage("A-F3", t);
        Check(SimpleAI.SelectFastMode(0, 0) == t[0].Knobs && SimpleAI.SelectFastMode(3000, 0) == t[3].Knobs,
            "A-F4 ★ `SelectFastMode` 走的是**它自己那张表**（低分段 → 第 0 行 · 高分段 → 第 3 行）"
            + "｜🧨 把 `SelectFastMode` 改成调 `SelectFromTiers(NormalBotTiers, …)` ⇒ 今天值相同、本条仍绿（如实记："
            + "两表今天同值，只能钉住「查的是第几张表的行」这种结构；真要区分得等拿到原版值得）");
    }

    // ==================================================================
    //  B · `KnobsOf` 一致（🔶 自定值：钉住别静默改）
    // ==================================================================
    static void CheckKnobsConsistency()
    {
        var se = SimpleAI.KnobsOf(AiDifficulty.SuperEasy);
        var e  = SimpleAI.KnobsOf(AiDifficulty.Easy);
        var n  = SimpleAI.KnobsOf(AiDifficulty.Normal);
        var h  = SimpleAI.KnobsOf(AiDifficulty.Hard);
        Check(se.MinSkips == 0 && se.MaxSkips == 3 && se.SkipChance == 90, "B-1 ★ `KnobsOf(SuperEasy)` = 0 / 3 / 90，实得 "
            + se.MinSkips + " / " + se.MaxSkips + " / " + se.SkipChance + "｜🧨 改 `KnobsOf` 的常量 ⇒ 红（🔶 自定值，⛔ 别静默改）");
        Check(e.MinSkips == 0 && e.MaxSkips == 2 && e.SkipChance == 60, "B-2 ★ `KnobsOf(Easy)` = 0 / 2 / 60，实得 "
            + e.MinSkips + " / " + e.MaxSkips + " / " + e.SkipChance);
        Check(n.MinSkips == 0 && n.MaxSkips == 1 && n.SkipChance == 30, "B-3 ★ `KnobsOf(Normal)` = 0 / 1 / 30，实得 "
            + n.MinSkips + " / " + n.MaxSkips + " / " + n.SkipChance);
        Check(h.MinSkips == 0 && h.MaxSkips == 0 && h.SkipChance == 0, "B-4 ★ `KnobsOf(Hard)` = 0 / 0 / 0（不砍任何动作），实得 "
            + h.MinSkips + " / " + h.MaxSkips + " / " + h.SkipChance);
        Check(se.SkipChance > e.SkipChance && e.SkipChance > n.SkipChance && n.SkipChance > h.SkipChance,
            "B-5 ★ 四档的 `SkipChance` **严格递减**（90 > 60 > 30 > 0）—— 「越简单砍得越狠」这条结构不许变"
            + "｜🧨 把任意两档写成同值 ⇒ 红（难度梯度消失）");

        // Normal 表必须把四个档位都用上（否则等于丢了两档）
        var used = new HashSet<AiDifficulty>();
        var t = SimpleAI.NormalBotTierTable;
        for (int i = 0; i < t.Length; i++) used.Add(t[i].Knobs);
        Check(used.Count == 4, "B-6 ★ Normal 表**四个档位全用到**（实得不同档位数 = " + used.Count + "）"
            + "｜🧨 把某一档的区间并给邻居 ⇒ 只剩 3 档 ⇒ 红");
    }

    // ==================================================================
    //  C · FTUE 表与挑选
    //    判据 = `decomp_full/MatchData__GetFTUEBotConfig.c:46-72`（`winCount` 精确相等、全不命中 ⇒ `[0]`）
    // ==================================================================
    static void CheckFtue()
    {
        var t = SimpleAI.FtueBotTierTable;
        Check(t != null && t.Length == 4, "C-0（前提）FTUE 表是 4 档（winCount 0/1/2/3），实得 "
            + (t == null ? "null" : t.Length.ToString()) + "｜🧨 增删一档 ⇒ 红");
        if (t == null || t.Length == 0) return;

        bool exact = true; string firstBad = null;
        for (int i = 0; i < t.Length; i++)
        {
            var got = SimpleAI.SelectFtue(t[i].WinCount);
            if (got != t[i].Knobs) { exact = false; if (firstBad == null) firstBad = "winCount=" + t[i].WinCount + " 实得 " + got + " / 期望 " + t[i].Knobs; }
        }
        Check(exact, "C-1 ★ 每个 `winCount` 都命中**自己那一行**（原版是 `==` 精确相等，不是区间）"
            + (exact ? "" : " —— " + firstBad)
            + "｜🧨 把挑选改成 `<=` 的 first-match ⇒ winCount=3 会命中第 0 行 ⇒ 红");

        Check(SimpleAI.SelectFtue(t[t.Length - 1].WinCount + 7) == t[0].Knobs,
            "C-2 ★ 表里没有的 `winCount` ⇒ 取 **`[0]`**（原版 `BotListFTUE[0]` 兜底）"
            + "｜🧨 把兜底改成 `Normal` / 改成最后一行 ⇒ 红");

        int maxWin = 0;
        for (int i = 0; i < t.Length; i++) if (t[i].WinCount > maxWin) maxWin = t[i].WinCount;
        Check(SimpleAI.FtueMaxWinCount == maxWin && maxWin == 3,
            "C-3 ★ `FtueMaxWinCount` = 表内最大 `winCount`（现读 3），实得 " + SimpleAI.FtueMaxWinCount
            + "｜🧨 把它写死成别的数（不与表同步）⇒ 红");

        bool mono = true;
        for (int i = 1; i < t.Length; i++)
            if ((int)t[i].Knobs < (int)t[i - 1].Knobs) mono = false;
        Check(mono, "C-4 ★ FTUE 档位随 `winCount` **不递减**（胜得越多越难）"
            + "｜🧨 把 0 胜写成 Hard、3 胜写成 SuperEasy ⇒ 红");
    }

    // ==================================================================
    //  D · FTUE 闸（原版 `MatchData.IsInFTUEState`，`MatchData__IsInFTUEState.c`）
    // ==================================================================
    static void CheckFtueGate()
    {
        MatchType[] excluded =
        {
            MatchType.Ranked, MatchType.RankedBot,
            MatchType.ClosedDeck, MatchType.ClosedDeckBot,
            MatchType.Unranked, MatchType.UnrankedBot,
            MatchType.Duel,
        };
        bool ok = true; string bad = null;
        for (int i = 0; i < excluded.Length; i++)
            if (SimpleAI.IsInFtueState(excluded[i], 0)) { ok = false; bad = excluded[i].ToString(); }
        Check(ok, "D-1 ★ 原版排除集七档（10/20/110/120/170/180/150）**一律不进 FTUE**（即使 0 胜）"
            + (ok ? "" : " —— " + bad + " 竟然进了")
            + "｜🧨 删掉 `IsInFtueState` 里的某个 `case` ⇒ 红（原版 `IsInFTUEState.c:31-34` 那串比较）");

        MatchType[] included =
        {
            MatchType.PracticeOffline, MatchType.EventAI, MatchType.Practice,
            MatchType.Tutorial, MatchType.Campaign, MatchType.TutorialReplay,
            MatchType.FastMode, MatchType.FastModeBot,
        };
        bool inOk = true; string inBad = null;
        for (int i = 0; i < included.Length; i++)
        {
            if (!SimpleAI.IsInFtueState(included[i], 0)) { inOk = false; if (inBad == null) inBad = included[i] + "(win=0)"; }
            if (!SimpleAI.IsInFtueState(included[i], SimpleAI.FtueMaxWinCount)) { inOk = false; if (inBad == null) inBad = included[i] + "(win=max)"; }
        }
        Check(inOk, "D-2 ★ 非排除档在 `winCount ∈ [0, max]` 内**都进 FTUE**（双边闭合：`≤` 不是 `<`）"
            + (inOk ? "" : " —— " + inBad)
            + "｜🧨 把 `<=` 改成 `<` ⇒ `win = max` 那一次红");

        Check(!SimpleAI.IsInFtueState(MatchType.PracticeOffline, SimpleAI.FtueMaxWinCount + 1),
            "D-3 ★ `winCount > max` ⇒ **不进 FTUE**（原版 `IsInFTUEState` 的 `winCount ≤ max(表内)` 那道闸），实得 "
            + SimpleAI.IsInFtueState(MatchType.PracticeOffline, SimpleAI.FtueMaxWinCount + 1)
            + "｜🧨 把 `IsInFtueState` 改成恒 true ⇒ 红（老玩家也永远吃新手配置）");

        Check(!SimpleAI.IsInFtueState(MatchType.PracticeOffline, -1),
            "D-4 ★ `winCount < 0`（= LiveOps 数据取不到）⇒ **判不了 FTUE** ⇒ false（由调用方退回 `Normal` 并出声）"
            + "｜🧨 把 -1 当 0 处理 ⇒ 红（取不到数据时会静默把 AI 变最简）");
    }

    // ==================================================================
    //  E · 模式键（原版 `MatchData.CurrentModeVictories` 那张表）
    //    字面量实解 = `判据/B88_R10_基础设施58条施工单.md:208`（`DAT_1842a6c38` = "Ranked" · `DAT_1842be6c0` = "FastMode"）
    // ==================================================================
    static void CheckResultCounterKeys()
    {
        Check(SimpleAI.ResultCounterKeyForMode(GameMode.Classic) == "Ranked", "E-1 ★ `Classic(0)` 的胜场键 = `\"Ranked\"`"
            + "（原版 `.c` 里 playMode 0/11 共用同一个常量 `DAT_1842a6c38`，实解 = `Ranked`），实得 "
            + (SimpleAI.ResultCounterKeyForMode(GameMode.Classic) ?? "null")
            + "｜🧨 改成 `\"Classic\"` ⇒ 红（那是 `DeserializeSharedData` 的键，不是胜场计数表的键）");
        Check(SimpleAI.ResultCounterKeyForMode(GameMode.RankedFriendly) == "Ranked", "E-2 ★ `RankedFriendly(11)` 与 `Classic(0)` 同键");
        Check(SimpleAI.ResultCounterKeyForMode(GameMode.OfflinePractice) == "Practice"
           && SimpleAI.ResultCounterKeyForMode(GameMode.OwnDeckTraining) == "Practice",
            "E-3 ★ `OfflinePractice(6)` / `OwnDeckTraining(12)` 的键 = `\"Practice\"`（= `PracticeEvent` 传给 `SetResultCount` 的那个字面量）");
        Check(SimpleAI.ResultCounterKeyForMode(GameMode.Skirmish) == "FastMode", "E-4 ★ `Skirmish(13)` 的键 = `\"FastMode\"`");
        Check(SimpleAI.ResultCounterKeyForMode(GameMode.Replay) == null,
            "E-5 ★ 原版**没有列**的模式（例 `Replay(10)`）⇒ null（= 原版那条 `return 0` 不查表）"
            + "｜🧨 给它编一个键 ⇒ 红（会去读别的模式的胜场）");
    }

    // ==================================================================
    //  F · 接线（源码级）—— 表再对，不接在 `Begin` 上也等于没做
    // ==================================================================
    static void CheckDriverWiring()
    {
        string p = Path.Combine(Application.dataPath, "CardPresentation/Battle/BattleDriver.cs");
        Check(File.Exists(p), "F-0（前提）源码文件在：" + p + "（⛔ 读不到就红，别改成跳过）");
        if (!File.Exists(p)) return;
        string src = File.ReadAllText(p);

        Check(CountOf(src, "aiDifficulty = SimpleAI.SelectNormal(") == 1,
            "F-1 ★ `BattleDriver` 里 `aiDifficulty = SimpleAI.SelectNormal(` **恰好 1 处**，实得 "
            + CountOf(src, "aiDifficulty = SimpleAI.SelectNormal(")
            + "｜🧨 删掉那一句（难度又变回恒 `Normal`）⇒ 0 处 ⇒ 红；在第二处也赋值 ⇒ ≥2 ⇒ 红（两个写点迟早不一致）");

        Check(CountOf(src, "SimpleAI.SelectFtue(") == 1,
            "F-2 ★ `SimpleAI.SelectFtue(` 在驱动里**恰好 1 处**，实得 " + CountOf(src, "SimpleAI.SelectFtue(")
            + "｜🧨 删掉 FTUE 那一支（新手引导直接吃 Normal 表）⇒ 红");

        int at = src.IndexOf("_playMode = Ctx.PlayMode;", StringComparison.Ordinal);
        bool sameLine = false;
        if (at >= 0)
        {
            int end = src.IndexOf('\n', at);
            if (end < 0) end = src.Length;
            string line = src.Substring(at, end - at);
            sameLine = line.IndexOf("AssignAiDifficultyForThisMatch();", StringComparison.Ordinal) >= 0;
        }
        Check(sameLine, "F-3 ★ 挑选入口 `AssignAiDifficultyForThisMatch();` 与 `_playMode = Ctx.PlayMode;` **在同一物理行**"
            + "（= `Begin` 里那一行；⚠️ 必须落在该行第一个 `//` 之前 —— 落到 `//` 之后会被注释吞掉）"
            + "｜🧨 把那半句删掉 / 挪到注释之后 ⇒ 红");

        Check(CountOf(src, "SimpleAI.SelectFastMode(") == 0,
            "F-4 ★ 驱动里**不许**出现 `SimpleAI.SelectFastMode(`（实得 " + CountOf(src, "SimpleAI.SelectFastMode(") + "）"
            + "｜🧨 把 13 Skirmish 接到 FastMode 表 ⇒ 红 —— **原版不接**："
            + "`MatchData.GetBotConfigSkipChance/MinSkips/MaxSkips` 只分 FTUE / Normal（`MatchData__GetBotConfigSkipChance.c:17-27`），"
            + "`BotListFastMode` 服务的是挑牌链；接上 = 造出原版没有的行为");
    }

    // ==================================================================
    //  工具
    // ==================================================================

    /// <summary>两轴区间都相交 = 矩形重叠（闭区间：贴边不算重叠，因为 `a.Max + 1 == b.Min` 才是邻居）。</summary>
    static int CountOverlaps(SimpleAI.BotTier[] t)
    {
        int n = 0;
        for (int i = 0; i < t.Length; i++)
            for (int j = i + 1; j < t.Length; j++)
            {
                bool rankHit = t[i].MinRank <= t[j].MaxRank && t[j].MinRank <= t[i].MaxRank;
                bool lossHit = t[i].MinLossStreak <= t[j].MaxLossStreak && t[j].MinLossStreak <= t[i].MaxLossStreak;
                if (rankHit && lossHit) n++;
            }
        return n;
    }

    static int CountMatches(SimpleAI.BotTier[] t, int rank, int loss)
    {
        int n = 0;
        for (int i = 0; i < t.Length; i++)
            if (loss >= t[i].MinLossStreak && loss <= t[i].MaxLossStreak
                && rank >= t[i].MinRank && rank <= t[i].MaxRank) n++;
        return n;
    }

    /// <summary>闭区间中点（用 `long` 算，避开 `MaxRank == int.MaxValue` 时的溢出）。</summary>
    static int Mid(int lo, int hi)
    {
        long m = (long)lo + ((long)hi - lo) / 2L;
        if (m > int.MaxValue) m = int.MaxValue;
        return (int)m;
    }

    /// <summary>满覆盖 + 唯一命中：取所有区间端点及其两侧一格（±1）做笛卡尔积采样 ——
    /// 矩形分区的「覆盖率 / 重叠」只会在端点处变化 ⇒ 这个点集足以判定。</summary>
    static void CheckCoverage(string tag, SimpleAI.BotTier[] t)
    {
        var ranks = new List<long>(); var losses = new List<long>();
        ranks.Add(0L); losses.Add(0L);
        for (int i = 0; i < t.Length; i++)
        {
            ranks.Add(t[i].MinRank); ranks.Add(t[i].MaxRank);
            losses.Add(t[i].MinLossStreak); losses.Add(t[i].MaxLossStreak);
            if (t[i].MinRank > 0) ranks.Add(t[i].MinRank - 1L);
            if (t[i].MinLossStreak > 0) losses.Add(t[i].MinLossStreak - 1L);
            if (t[i].MaxRank < int.MaxValue) ranks.Add(t[i].MaxRank + 1L);
            if (t[i].MaxLossStreak < int.MaxValue) losses.Add(t[i].MaxLossStreak + 1L);
            ranks.Add(int.MaxValue); losses.Add(int.MaxValue);
        }
        int samples = 0, bad = 0; string firstBad = null;
        for (int a = 0; a < ranks.Count; a++)
            for (int b = 0; b < losses.Count; b++)
            {
                int r = (int)ranks[a], l = (int)losses[b];
                int n = CountMatches(t, r, l);
                samples++;
                if (n != 1)
                {
                    bad++;
                    if (firstBad == null) firstBad = "(rank=" + r + ", loss=" + l + ") 命中 " + n + " 条";
                }
            }
        Check(bad == 0, tag + " ★ **满覆盖且每点唯一命中**：端点 × 端点（含 ±1）共 " + samples + " 个采样点，异常 " + bad
            + (bad == 0 ? "" : " · 首个 " + firstBad)
            + "｜🧨 某条区间「扩一格」与邻居重叠 ⇒ 命中 2 ⇒ 红；某条区间「缩一格」留下空隙 ⇒ 命中 0 ⇒ 红"
            + "（命中 0 时只能落到 `[0]` 兜底 = 等于没设计）");
    }

    static int CountOf(string s, string needle)
    {
        if (string.IsNullOrEmpty(s) || string.IsNullOrEmpty(needle)) return 0;
        int n = 0, at = 0;
        while (true)
        {
            at = s.IndexOf(needle, at, StringComparison.Ordinal);
            if (at < 0) return n;
            n++; at += needle.Length;
        }
    }
}
