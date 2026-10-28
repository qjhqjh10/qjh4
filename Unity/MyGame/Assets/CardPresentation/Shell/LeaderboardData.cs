// LeaderboardData.cs — 排行榜的**本地数据**：① UI 行表（默认空）② **本地计分层**（本轮新增）
//
// ============================ 这个文件为什么存在 ============================
// 🔴 **原版的排行榜数据全在服务器**（`LeaderboardManager` / 各 `RankingDataProvider` 走 PlayFab），
//    本地**一条都取不到**。用户 2026-09-26 的口径是「**有什么复刻什么，具体的数据和排名这些可以空着**」
//    ⇒ 界面照原版建、**数据留空**（`LeaderboardWindow` 在空数据下就是原版那个「一行都没有」的样子 ——
//    四棵榜**都没有空态节点**，我们**不自己造一个**）。
//
// ---- 🔴 2026-10-25（代理 G 执行）：补上「本地计分」那一半 ----
// `LeaderboardManager.LocalScore` 是**离线唯一能算分的地方**（不是服务端拉取）：它把玩家自己的分
// **先在本地缓存里改掉、重排、重刷名次**，让 UI 立刻显示新分；真正的排行榜仍由服务端回包覆盖。
// 依据（ILSpy 方法体**可读**，逐句对过）：
//   · `LeaderboardManager__LocalScore(definition, value, overrideMode)`：
//       ① `board = GetCachedLeaderboard(def.leaderboardKey, 0)`；
//       ② `mode = overrideMode ?? def.scoringMode`；
//       ③ `UpdateEntry(def, board.currentEntry, mode, value)`；
//       ④ 若 `def.LocalRuntimeOptions["team"]` 存在且 `board.totals` 里**已有**该键 ⇒
//          `totals[team] = ApplyScoringMode(mode, totals[team], value)`（⚠️ 只更新**已存在**的键）；
//       ⑤ 在 `board.entries` 里找 `entryId == currentEntry.entryId` 那一条 → 再 `UpdateEntry`；
//       ⑥ **按 `value` 降序重排**，并把 `position = i + 1` 逐条重写；
//   · `LeaderboardManager__UpdateEntry(def, entry, mode, value)`：
//       无 `team` 选项 ⇒ `entry.value = Apply(mode, entry.value, value)`；
//       有 ⇒ `entry.armyScores[team] = Apply(mode, entry.GetScoreByTeam(team), value)`。
//   · `LeaderboardScoringModeUtils__ApplyScoringMode.c`（**伪码可读，逐分支抄**）：
//       `Increment(0) => score + newScore` · `SetIfHigher(1) => Math.Max(score, newScore)` ·
//       `SetValue(2) => newScore` · 其它 ⇒ `score`。
//
// ⚠️ **仍是服务器的**：`GetLeaderboard` / `PreloadLeaderboards` / `UnpackPlayfabValues`（拉取）、
//    `LeaderboardResult.totals` 的**初始值**（服务端算好下发；我们只维护「已存在键」的增量）。
//    🔴 签名级存疑（未核实）：`GetCachedLeaderboard(string key, long defaultValue = 0L)` 的
//    **默认值参数在反编译里是 `long`，而方法返回 `LeaderboardResult`** —— 这是 `//IL_` 类型丢失的典型形态；
//    本实现按「缓存没有就返回 null，由调用方新建」处理，⛔ 不照那个可能是错的签名硬编。
//
// ⚠️ **它不是「本地进度模拟」的最终口径** —— 「要不要给玩家攒本地分」是产品问题，
//    但 `LocalScore` 这个**机制**本身是原版就有的客户端行为（见上），所以先按原版落地。
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LeaderboardScoringMode`（枚举顺序**照 ILSpy 现读**：0/1/2 —— 与伪码分支逐个对上）。</summary>
    public enum LeaderboardScoringMode
    {
        /// <summary>0：累加（`score + newScore`）。</summary>
        Increment = 0,
        /// <summary>1：取高（`Math.Max(score, newScore)`）。</summary>
        SetIfHigher = 1,
        /// <summary>2：直接覆盖（`newScore`）。</summary>
        SetValue = 2,
    }

    /// <summary>原版 `LeaderboardDefinition` 的本地最小等价（只留本地计分用到的三个字段）。</summary>
    public sealed class LeaderboardDef
    {
        public string Key;
        public LeaderboardScoringMode ScoringMode = LeaderboardScoringMode.SetValue;
        /// <summary>`LocalRuntimeOptions["team"]`（有 ⇒ 分数记在 `armyScores[team]` 而不是 `value`）。</summary>
        public string Team;
    }

    /// <summary>原版 `LeaderboardEntry` 的本地等价。
    /// 字段偏移判据：`+0x48` = `armyScores`（`GetScoreByArmy/GetScoreByTeam` 读它、`UpdateEntry` 写它）、
    /// `+0x50` = `highestArmyScores`（`GetHighestScoreByArmy` 读它），键 = 阵营枚举 `ToString()` 或队伍串。</summary>
    public sealed class LocalLeaderboardEntry
    {
        public string EntryId;
        public string PlayerName;
        public long Value;
        public int Position;
        public bool IsPlayer;
        public readonly Dictionary<string, int> ArmyScores = new Dictionary<string, int>();
        public readonly Dictionary<string, int> HighestArmyScores = new Dictionary<string, int>();

        /// <summary>= `LeaderboardEntry.GetScoreByArmy`：`armyScores[army.ToString()]`，缺省用 `defaultValue`。
        /// ⚠️ 原版的键是 `CardArmy` **枚举名**；我们没有那个枚举（工程里阵营用整数 id）⇒ 键 = 整数的字符串。
        /// 键只在本地层内部用（写入 `UpdateEntry`、读出这里），⛔ 不会与存档/服务器交换。</summary>
        public int GetScoreByArmy(int army, int defaultValue = 0)
        {
            return GetScoreByTeam(army.ToString(), defaultValue);
        }

        /// <summary>= `LeaderboardEntry.GetHighestScoreByArmy`（读 +0x50 那张表）。</summary>
        public int GetHighestScoreByArmy(int army, int defaultValue = 0)
        {
            int v;
            return HighestArmyScores.TryGetValue(army.ToString(), out v) ? v : defaultValue;
        }

        /// <summary>= `LeaderboardEntry.GetScoreByTeam`：同一张 `armyScores` 字典、键是队伍串。</summary>
        public int GetScoreByTeam(string team, int defaultValue = 0)
        {
            int v;
            return (ArmyScores != null && team != null && ArmyScores.TryGetValue(team, out v)) ? v : defaultValue;
        }

        /// <summary>= `LeaderboardEntry.MakePlayerEntry(value, position)`（玩家的那一条：`IsPlayer = true`）。</summary>
        public static LocalLeaderboardEntry MakePlayerEntry(long value, int position)
        {
            return new LocalLeaderboardEntry
            {
                EntryId = "self", IsPlayer = true, Value = value, Position = position,
            };
        }
    }

    /// <summary>原版 `LeaderboardResult` 的本地等价。
    /// ⚠️ `Totals` 是**服务端下发**的预聚合表（`GetTotalByArmy/GetTotalByTeam` 在反编译里就是
    /// `GetValueOrDefault(dict, key, defaultValue)`，**不是**现场求和）⇒ 本类只做查表 + 维护已存在键。</summary>
    public sealed class LocalLeaderboardResult
    {
        public string Key;
        public LocalLeaderboardEntry CurrentEntry;
        public LocalLeaderboardEntry[] Entries = Array.Empty<LocalLeaderboardEntry>();
        public readonly Dictionary<string, long> Totals = new Dictionary<string, long>();
        public DateTime FetchUtc = DateTime.MinValue;

        /// <summary>= `LeaderboardResult.GetTotalByArmy`（查表 + 缺省值，**不求和**）。</summary>
        public long GetTotalByArmy(int army, long defaultValue = 0)
        {
            return GetTotalByTeam(army.ToString(), defaultValue);
        }

        /// <summary>= `LeaderboardResult.GetTotalByTeam`。</summary>
        public long GetTotalByTeam(string team, long defaultValue = 0)
        {
            long v;
            return (Totals != null && team != null && Totals.TryGetValue(team, out v)) ? v : defaultValue;
        }
    }

    /// <summary>排行榜的本地数据表（**默认全空**）+ 本地计分（`LocalScore`）。</summary>
    public static class LeaderboardData
    {
        // ============================================================ ① UI 行表（原有契约，别改语义）
        // 🔴 **key 用「榜 + 页签」两个维度**（原版就是这么分的：每个页签挂一个 provider，
        //    provider 各自向自己的服务器榜要数据）。别压成一个维度 —— 四棵榜的页签不重样。
        static readonly Dictionary<string, List<LeaderboardRowData>> _injected =
            new Dictionary<string, List<LeaderboardRowData>>();

        static readonly List<LeaderboardRowData> Empty = new List<LeaderboardRowData>();

        static string Key(LeaderboardKind kind, LeaderboardTab tab) { return kind + "/" + tab; }

        /// <summary>这一页的行。**本地默认 = 空表**（原版读服务器）。</summary>
        public static List<LeaderboardRowData> Rows(LeaderboardKind kind, LeaderboardTab tab)
        {
            List<LeaderboardRowData> l;
            return _injected.TryGetValue(Key(kind, tab), out l) ? l : Empty;
        }

        /// <summary>本地有没有任何数据 —— **窗口用它决定要不要出声**（不许静默）。</summary>
        public static bool HasAny { get { return _injected.Count > 0 || _cache.Count > 0; } }

        /// <summary>只给自检用：塞一批行进去（证明行族 builder 画得对）。</summary>
        public static void InjectForTest(LeaderboardKind kind, LeaderboardTab tab, List<LeaderboardRowData> rows)
        {
            _injected[Key(kind, tab)] = rows ?? new List<LeaderboardRowData>();
        }

        /// <summary>只给自检用：清掉（**必须清** —— 否则下一条断言量到的是上一条的数据）。</summary>
        public static void ClearForTest() { _injected.Clear(); }

        // ============================================================ ② 本地计分层（2026-10-25 新增）
        static readonly Dictionary<string, LocalLeaderboardResult> _cache =
            new Dictionary<string, LocalLeaderboardResult>();

        /// <summary>原版 `LeaderboardScoringModeUtils.ApplyScoringMode`（**逐分支 1:1**，判据见文件头）。
        /// ⚠️ 未知模式 ⇒ 原样返回旧值（原版就是 `return score;`，⛔ 别改成抛异常）。</summary>
        public static long ApplyScoringMode(LeaderboardScoringMode mode, long score, long newScore)
        {
            switch (mode)
            {
                case LeaderboardScoringMode.Increment: return score + newScore;
                case LeaderboardScoringMode.SetIfHigher: return Math.Max(score, newScore);
                case LeaderboardScoringMode.SetValue: return newScore;
                default: return score;
            }
        }

        /// <summary>= `LeaderboardManager.GetCachedLeaderboard(key, default)`。⚠️ 默认值参数类型在反编译里存疑
        /// （见文件头「签名级存疑」）⇒ 缓存没有就返回 `null`，由 `LocalScore` 新建。</summary>
        public static LocalLeaderboardResult GetCached(string key)
        {
            LocalLeaderboardResult r;
            return (!string.IsNullOrEmpty(key) && _cache.TryGetValue(key, out r)) ? r : null;
        }

        /// <summary>= `LeaderboardManager.GetCachedPlayerScore(key)`：缓存里自己那条的分（没有 ⇒ 0）。</summary>
        public static long GetCachedPlayerScore(string key)
        {
            var r = GetCached(key);
            return (r != null && r.CurrentEntry != null) ? r.CurrentEntry.Value : 0;
        }

        /// <summary>= `LeaderboardManager.InvalidateCache(key)`。</summary>
        public static void Invalidate(string key)
        {
            if (!string.IsNullOrEmpty(key)) _cache.Remove(key);
        }

        /// <summary>= `LeaderboardManager.UpdateEntry(definition, entry, mode, value)`：
        /// 无 `team` ⇒ 改 `entry.Value`；有 ⇒ 改 `entry.ArmyScores[team]`（**同一张表**，`GetScoreByTeam` 读它）。</summary>
        public static void UpdateEntry(LeaderboardDef def, LocalLeaderboardEntry entry,
                                       LeaderboardScoringMode mode, int value)
        {
            if (entry == null) return;
            string team = def != null ? def.Team : null;
            if (string.IsNullOrEmpty(team))
            {
                entry.Value = ApplyScoringMode(mode, entry.Value, value);
            }
            else
            {
                int old = entry.GetScoreByTeam(team, 0);
                entry.ArmyScores[team] = (int)ApplyScoringMode(mode, old, value);
            }
        }

        /// <summary>= `LeaderboardManager.LocalScore(definition, value, overrideMode)`（**六步逐句**，见文件头）。
        /// 返回缓存里的那张榜（调用方可以立刻重画）。</summary>
        public static LocalLeaderboardResult LocalScore(LeaderboardDef def, int value,
                                                        LeaderboardScoringMode? overrideMode = null)
        {
            if (def == null || string.IsNullOrEmpty(def.Key))
            {
                Debug.LogWarning("[Leaderboard] `LocalScore` 收到空定义/空 key ⇒ 什么都没做（⛔ 不静默建一张无名榜）");
                return null;
            }
            var board = GetCached(def.Key);
            if (board == null)
            {
                board = new LocalLeaderboardResult { Key = def.Key };
                board.CurrentEntry = LocalLeaderboardEntry.MakePlayerEntry(0, 0);
                // 🔴 **currentEntry 与 entries 里那条必须是两个对象**：原版第 ⑤ 步会**再**对
                //    `entries.FirstOrDefault(e => e.entryId == currentEntry.entryId)` 跑一次 `UpdateEntry`；
                //    若两者是同一对象，`Increment` 会被加两遍（原版里 currentEntry 是「我的名次」那一条、
                //    entries 是拉下来的一页，本身是两个实例）。⇒ 这里放一份**拷贝**。
                board.Entries = new[] { CloneEntry(board.CurrentEntry) };
                _cache[def.Key] = board;
            }
            if (board.CurrentEntry == null)
            {
                board.CurrentEntry = LocalLeaderboardEntry.MakePlayerEntry(0, 0);
                if (board.Entries == null || board.Entries.Length == 0)
                    board.Entries = new[] { CloneEntry(board.CurrentEntry) };
            }

            var mode = overrideMode ?? def.ScoringMode;

            // ③ 自己的那条（currentEntry）
            UpdateEntry(def, board.CurrentEntry, mode, value);

            // ④ 队伍总分：**只在键已存在时**更新（原版 `ContainsKey` 那一句；⛔ 不新造键）
            if (!string.IsNullOrEmpty(def.Team) && board.Totals.ContainsKey(def.Team))
                board.Totals[def.Team] = ApplyScoringMode(mode, board.Totals[def.Team], value);

            // ⑤ 在列表里找同 entryId 的那条，再更新一次
            var e = board.Entries != null
                ? board.Entries.FirstOrDefault(x => x != null && x.EntryId == board.CurrentEntry.EntryId)
                : null;
            if (e != null)
            {
                UpdateEntry(def, e, mode, value);

                // ⑥ 按 value 降序重排 + 重写 position（原版 OrderByDescending(...).ToArray() + `position = i+1`）
                board.Entries = board.Entries.Where(x => x != null)
                                             .OrderByDescending(x => x.Value)
                                             .ToArray();
                for (int i = 0; i < board.Entries.Length; i++) board.Entries[i].Position = i + 1;
            }
            return board;
        }

        /// <summary>浅拷贝一条（`ArmyScores`/`HighestArmyScores` 两张字典也复制）。
        /// 用途见 `LocalScore` 里「currentEntry 与 entries 不能是同一对象」那条注释。</summary>
        public static LocalLeaderboardEntry CloneEntry(LocalLeaderboardEntry e)
        {
            if (e == null) return null;
            var c = new LocalLeaderboardEntry
            {
                EntryId = e.EntryId, PlayerName = e.PlayerName, Value = e.Value, Position = e.Position, IsPlayer = e.IsPlayer,
            };
            foreach (var kv in e.ArmyScores) c.ArmyScores[kv.Key] = kv.Value;
            foreach (var kv in e.HighestArmyScores) c.HighestArmyScores[kv.Key] = kv.Value;
            return c;
        }

        /// <summary>按 key 计分（= `LeaderboardManager.LocalScore(string key, int value, mode?)`；
        /// 定义表在本地没有 `LeaderboardConfig` ⇒ 走调用方给的模式，缺省 `SetValue`）。</summary>
        public static LocalLeaderboardResult LocalScore(string key, int value,
                                                        LeaderboardScoringMode? overrideMode = null)
        {
            return LocalScore(new LeaderboardDef { Key = key, ScoringMode = overrideMode ?? LeaderboardScoringMode.SetValue },
                              value, overrideMode);
        }

        // ---- 自检 / 测试口
        /// <summary>直接塞一张榜进缓存（自检用：模拟「服务端刚下发了一页榜」）。</summary>
        public static void SetCachedForTest(LocalLeaderboardResult board)
        {
            if (board == null || string.IsNullOrEmpty(board.Key)) return;
            _cache[board.Key] = board;
        }

        public static void ResetScoringForTest() { _cache.Clear(); }

        // ============================================================ 追加区 · 2026-10-28（B93_D4 · 账 748）
        /// <summary>原版 `TwoSidesEvent.GetTotalStats()`（`decomp_full/TwoSidesEvent__GetTotalStats.c` **全文 24 行 / 3 分支**）——
        /// 逐句：
        /// <code>
        /// var mgr = SingletonBehaviour&lt;LeaderboardManager&gt;.Instance;        // `.c:` `SingletonBehaviour__get_Instance(DAT_1842be548)`
        /// if (this.eventData != null &amp;&amp; mgr != null)                        // `.c:` `this+0x18` 与 mgr 两个 != 0
        ///     mgr.GetCachedLeaderboard(this.eventData.&lt;+0x80&gt;, null);         // key 在 `eventData+0x80`
        /// else FUN_1803f47a0();                                            // = **throw NullReferenceException**
        /// </code>
        /// <para>🔴 判据（**为什么落到本类**）：`LeaderboardManager.GetCachedLeaderboard(key, default)` 在本工程的等价物
        /// = <see cref="GetCached"/>（同一张本地缓存 `_cache`，同一批 `LeaderboardData` 行族）。原版那 3 个分支里
        /// ① `mgr == null` 与 ② `eventData == null` 都落到 **throw NRE**；③ 才是那句取缓存。
        /// ⚠️ 我们**不抛**（抛了会把整个壳打断）：出声 + 返 null —— 与本文件其余「原版这一档 NRE」的写法同口径。</para>
        /// <para>⚠️ `key` 由调用方给：原版是 `TwoSidesEvent.eventData+0x80`，本工程的 `TwoSidesEvent`/`TwoSidesEventData`
        /// 还没建（账 `TwoSidesEvent` 那一族两处 C）⇒ 这一层只负责「判空 → 取缓存」那 3 个分支。</para></summary>
        public static LocalLeaderboardResult GetTotalStats(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                WarnOnce("no-key", "[LeaderboardData] `TwoSidesEvent.GetTotalStats`：没有事件数据（原版这一档 `this.eventData == null` ⇒ **throw NRE**）"
                                 + " ⇒ 返 null（我们不抛）");
                return null;
            }
            var r = GetCached(key);
            if (r == null) WarnOnce("miss:" + key, "[LeaderboardData] `GetTotalStats(" + key + ")`：缓存里还没有这张榜 ⇒ 返 null（原版 `GetCachedLeaderboard` 同一档）");
            return r;
        }

        static readonly HashSet<string> _told = new HashSet<string>(StringComparer.Ordinal);
        static void WarnOnce(string tag, string msg) { if (_told.Add(tag ?? "")) Debug.Log(msg); }

        /// <summary>自检：**灭自证**版断言集。
        /// 关键一条：**同一个 (old=5,new=3) 三种模式必须给出三个不同的结果**（8 / 5 / 3）——
        /// 任何「恒返回某个常量」的实现都不可能同时满足；再叠上重排与 position 的边界。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            ResetScoringForTest();
            long a = ApplyScoringMode(LeaderboardScoringMode.Increment, 5, 3);       // 8
            long b = ApplyScoringMode(LeaderboardScoringMode.SetIfHigher, 5, 3);     // 5（取高）
            long c = ApplyScoringMode(LeaderboardScoringMode.SetValue, 5, 3);        // 3
            if (a != 8 || b != 5 || c != 3)
            {
                why = "ApplyScoringMode(5,3) 三档应为 8/5/3，实际 " + a + "/" + b + "/" + c;
                return false;
            }
            if (ApplyScoringMode(LeaderboardScoringMode.SetIfHigher, 3, 9) != 9)
            { why = "SetIfHigher 反向（3,9）应为 9"; return false; }

            // 重排 + position：自己 10（从 5 起 Increment 加 5），对手 7 / 20
            var def = new LeaderboardDef { Key = "selfcheck/board", ScoringMode = LeaderboardScoringMode.SetValue };
            var board = new LocalLeaderboardResult { Key = def.Key };
            board.CurrentEntry = LocalLeaderboardEntry.MakePlayerEntry(5, 3);
            board.Entries = new[]
            {
                new LocalLeaderboardEntry { EntryId = "otherA", Value = 7, Position = 1 },
                CloneEntry(board.CurrentEntry),     // 同 entryId、**不同对象**（见 LocalScore 那条注释）
                new LocalLeaderboardEntry { EntryId = "otherB", Value = 20, Position = 2 },
            };
            SetCachedForTest(board);
            var res = LocalScore(def, 10, LeaderboardScoringMode.SetIfHigher);   // 自己 5→10
            if (res == null) { why = "LocalScore 返回 null"; return false; }
            if (res.CurrentEntry.Value != 10) { why = "自己的分应为 10，实际 " + res.CurrentEntry.Value; return false; }
            if (res.Entries.Length != 3 || res.Entries[0].Value != 20 || res.Entries[1].Value != 10
                || res.Entries[2].Value != 7)
            { why = "降序重排错（应为 20/10/7）"; return false; }
            for (int i = 0; i < res.Entries.Length; i++)
                if (res.Entries[i].Position != i + 1) { why = "position 未按 i+1 重写"; return false; }

            // 队伍分支：只改 ArmyScores[team]，value 不动；且 Totals 只在键已存在时更新
            var teamDef = new LeaderboardDef { Key = "selfcheck/team", ScoringMode = LeaderboardScoringMode.Increment, Team = "UM" };
            var tb = new LocalLeaderboardResult { Key = teamDef.Key };
            tb.CurrentEntry = LocalLeaderboardEntry.MakePlayerEntry(4, 1);
            tb.CurrentEntry.ArmyScores["UM"] = 100;
            tb.Entries = new[] { CloneEntry(tb.CurrentEntry) };
            SetCachedForTest(tb);
            var t2 = LocalScore(teamDef, 5);
            if (t2.CurrentEntry.Value != 4) { why = "队伍模式不该改 value"; return false; }
            if (t2.CurrentEntry.GetScoreByTeam("UM") != 105) { why = "队伍分应为 105，实际 " + t2.CurrentEntry.GetScoreByTeam("UM"); return false; }
            if (t2.Totals.ContainsKey("UM")) { why = "原版只更新已存在的队伍总分键，这里不该被新建"; return false; }

            ResetScoringForTest();
            return true;
        }
    }
}
