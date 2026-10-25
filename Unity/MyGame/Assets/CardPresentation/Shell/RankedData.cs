// RankedData.cs — 排位数据层（原版 `RankedEventBase` / `RankedV3Event` / `RankedRank` / `RankedDivisionV3` 的**本地半边**）
//
// ============================ 这是什么、不是什么 ============================
// 🔴 **原版排位分（MMR）与每局增减都在服务器**：客户端只把
//    `{eventId, matchWon, targetMilestone, enemyMilestone, ownName, isRanked?, isSuspicious, seasonNumber, opponentId …}`
//    打进 CloudScript（`RankedV3Event__OnMatchFinished` / `__RecordMatchFinished`，判据见下），
//    **回包才是真正的新分**。⇒ 本类**不发明 ELO 公式**：每局增减来自可注入的 `PointDelta`，
//    默认没有（= 0 + 出声一次），理由写在 `WhyNoPointDelta`。
//
// ✅ **能 1:1 复刻的那一半**（都在客户端代码里，判据逐行可查）：
//   ① 积分 → 段位（`RankedV3Event__GetDivisionByPoints.c` 的 C# 方法体**可读**，见 `DivisionOf`）；
//   ② `LegendaryElo` = 数据值，缺省 **2500**（`RankedEventBase.LegendaryElo` 可读片段：`return 0; … return 2500;`）；
//   ③ 平局记 **2**、胜 1、负 0（`RankedV3Event.OnMatchFinished`：`Draw` 支 `matchWon = 2f`）；
//   ④ 连败：胜 ⇒ 归零；负/平 ⇒ +1；`suspiciousGame` 时不记（同一处方法体）；
//   ⑤ 赛季过期：`CurrentScore != 0 && LastPlayedSeason != 当前赛季` ⇒ 走「赛季结算弹窗」那条（`RankedV3Event.Expire`）。
//
// ❌ **不可及（如实标注，不猜）**：
//   · **`GameModesConfig.rankedRanks`（MMR 阈值表）是 TitleData 远程配置**
//     （`ConfigManager.GetConfig<GameModesConfig>()` → `ConfigProvider<T>.UnpackInternal(Dictionary<string,string> titleData)`；
//      缺失时它自己 `Debug.Log("Couldn't find Config of type …")` 并返回 null）。
//     本地三处都查过：`assets_full` 全盘 grep `listedRanks`/`correspondingMMR` **0 命中**、
//     `assetripper_out/ExportedProject` 里只有 `RankedRank.cs` 桩、`素材/Warpforge原版/` 无该 SO
//     ⇒ **`Ranks` 默认为空，由 `SetRanksForTest` 注入**（自检用合成表验证算法）。
//   · **每局 MMR 增减**：服务端 CloudScript（见上）⇒ `PointDelta` 可注入，默认 null。
//   · **`preventLosingRank` / `preventLosingSeals`**：表里有这两个字段，但**客户端方法体里读不到使用点**
//     （只在 `RankedRank` 声明里出现）⇒ 本类只**透传**，不据此改分（注释在 `ApplyMatchResult`）。
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RankedRank`（`RankedRank.cs` 六个字段，逐字段照抄）。
    /// 它的数据源是 `GameModesConfig.rankedRanks`（TitleData 远程配置，本地没有）。</summary>
    [Serializable]
    public class RankedRank
    {
        public int division;
        public int rank;
        public int sealsRequiredToUpdate;
        public int correspondingMMR;
        public bool preventLosingRank;
        public bool preventLosingSeals;
    }

    /// <summary>原版 `RankedDivisionV3` 的本地等价（字段逐条对：`DivisionNumber/RankNumber/SealCount/TotalSeals/IsLegendary/CorrespondingMMR`
    /// + 两个标签 key `Ranked/Division/{n}` / `Ranked/Rank/{n}`，见 `RankedV3Event__GetDivisionByPoints`）。</summary>
    public struct RankedDivision
    {
        /// <summary>`false` = 没有阈值表 ⇒ 下面这些字段**没有意义**（调用方别画）。</summary>
        public bool Known;
        public int Number;              // DivisionNumber
        public int RankNumber;          // RankNumber
        public int SealCount;           // 当前段内已得印章数
        public int TotalSeals;          // 本段升级所需印章数
        public int CorrespondingMMR;    // 本段起始 MMR
        public bool IsLegendary;        // points >= LegendaryElo
        public string DivisionLabelKey; // "Ranked/Division/<Number>"
        public string RankLabelKey;     // "Ranked/Rank/<RankNumber>"

        public override string ToString()
        {
            return Known
                ? "D" + Number + "-R" + RankNumber + " seals " + SealCount + "/" + TotalSeals
                  + (IsLegendary ? " (Legendary)" : "") + " @" + CorrespondingMMR
                : "<unknown: no rank table>";
        }
    }

    /// <summary>一局的结算结果（= 原版 `OnlineMatchResult` 的本地等价，只留我们算得出的部分）。</summary>
    public struct RankedMatchScore
    {
        public int MatchWon;        // 1 胜 / 0 负 / 2 平（原版 `matchWon`）
        public int LossStreak;      // 结算后的连败
        public long PointsBefore, PointsAfter;
        public bool DeltaKnown;     // 有没有真的拿到本局增减（false = 服务器才有）
        public bool Suspicious;     // 原版 `suspiciousGame`：不记连败/不结算
    }


    /// <summary>原版 `RankedV3ArmyState`（**Flags**，值照抄：None0 / Boosted1 / Shielded2 / Featured4）。</summary>
    [Flags]
    public enum RankedArmyFlags { None = 0, Boosted = 1, Shielded = 2, Featured = 4 }

    /// <summary>原版 `RankedScoreBoostEvent` 的本地最小等价（加分活动：哪些阵营、剩几次、胜场记录）。
    /// 数据源在服务器（RankedScoreBoostData/Save）⇒ 默认空表，`SetBoostsForTest` 注入。</summary>
    [Serializable]
    public class RankedBoostEvent
    {
        public string Id;
        public int[] Armies;                          // 受影响阵营（原版 IsArmyAffected）
        public int[] WinsByArmy;                      // 与 Armies 同长：该阵营已用胜场（原版 save.GetWins）
        public int[] BoostsLeftByArmy;                // 与 Armies 同长：剩余次数（原版 HasBoostsLeft）
        public bool GrantsShield;                     // 原版 Shielded 那一支（`x` 的某个字段；predicate 体已丢）
        public bool Includes(string eventId) { return !string.IsNullOrEmpty(Id) && Id == eventId; }
        public bool Affects(int army) { return Array.IndexOf(Armies ?? Array.Empty<int>(), army) >= 0; }
        public int IndexOf(int army) { return Array.IndexOf(Armies ?? Array.Empty<int>(), army); }
        public int WinsFor(int army) { int i = IndexOf(army); return (i >= 0 && WinsByArmy != null && i < WinsByArmy.Length) ? WinsByArmy[i] : 0; }
        public int BoostsLeftFor(int army) { int i = IndexOf(army); return (i >= 0 && BoostsLeftByArmy != null && i < BoostsLeftByArmy.Length) ? BoostsLeftByArmy[i] : 0; }
    }

    /// <summary>排位数据层。**唯一一份**：段位换算、连败、赛季、本地点数都在这里。</summary>
    public static class RankedData
    {
        // ============================================================ 判据常量
        /// <summary>`RankedEventBase.LegendaryElo` 的缺省值（方法体可读片段：数据值非 0 用它，否则 `return 2500`）。</summary>
        public const int LegendaryEloFallback = 2500;

        public const string WhyNoRankTable =
            "判据不可及：`GameModesConfig.rankedRanks`（MMR 阈值表 / 每段印章数）是 **TitleData 远程配置**" +
            "（`ConfigManager.GetConfig<GameModesConfig>()` → `ConfigProvider<T>.UnpackInternal(Dictionary<string,string> titleData)`）；" +
            "本地 assets_full / AssetRipper 导出 / 素材目录三处都没有该表 ⇒ 默认空表，用 `SetRanksForTest` 注入";

        public const string WhyNoPointDelta =
            "判据不可及：每局 MMR 增减在**服务器 CloudScript**（`RankedV3Event.OnMatchFinished` 只把 " +
            "{eventId, matchWon, milestones, isRanked, isSuspicious, season, opponentId} 打进云脚本，回包才是新分）" +
            "⇒ 客户端默认 delta = 0（不编公式）；要启用就设 `PointDelta`";

        // ============================================================ 可注入
        static RankedRank[] _ranks = Array.Empty<RankedRank>();
        static int _legendaryElo = LegendaryEloFallback;
        /// <summary>每局增减（服务端才有）。`null` = 不可及（delta 记 0 并出声一次）。</summary>
        public static Func<BattleLogData.Outcome, long> PointDelta;
        static bool _warnedNoTable, _warnedNoDelta;

        public static RankedRank[] Ranks { get { return _ranks; } }
        public static int LegendaryElo { get { return _legendaryElo; } }
        public static bool HasRankTable { get { return _ranks != null && _ranks.Length > 0; } }

        // ============================================================ 状态（= 原版 RankedV3Save / GameModes 的本地等价）
        static bool _loaded;
        static long _points;
        static bool _isRanked;
        static int _lossStreak;
        static int _season = 1;
        static int _lastPlayedSeason = 1;     // 0 = 还没打过任何赛季（原版 `LastPlayedSeason == 0` 那一支）
        static int _savedTier;                // = 原版 `AllianceTrophySave.tier` 的排位版（`CurrentTier` 用 Math.Max）

        public static long Points { get { Load(); return _points; } }
        public static bool IsRanked { get { Load(); return _isRanked; } }
        public static int LossStreak { get { Load(); return _lossStreak; } }
        public static int SeasonNumber { get { Load(); return _season; } }
        public static int LastPlayedSeason { get { Load(); return _lastPlayedSeason; } }
        public static int SavedTier { get { Load(); return _savedTier; } }

        // ============================================================ 存档（本地等价；原版在服务器）
        public static string OverrideDir;     // 自检用
        static string StoreDir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                    ? Path.Combine(Application.persistentDataPath, "WarpforgeRanked")
                    : OverrideDir;
            }
        }
        public static string StorePath { get { return Path.Combine(StoreDir, "ranked.json"); } }

        [Serializable]
        class Save
        {
            public int version = 1;
            public long points;
            public bool isRanked;
            public int lossStreak;
            public int season = 1;
            public int lastPlayedSeason = 1;
            public int savedTier;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            try
            {
                // 🆕 Step 4 双写期过渡读法：**先中央档、再旧档**（`ReadThroughLegacy` 内部判 `SaveCenter.StoreOn`
                // ⇒ 批处理/自检照旧只读旧档那一份）。⛔ 别删旧读。
                string text = SaveCenter.ReadThroughLegacy(SaveKeys.PlayerSeason, StorePath);
                if (text == null) return;
                var s = JsonUtility.FromJson<Save>(text);
                if (s == null) return;
                _points = s.points; _isRanked = s.isRanked; _lossStreak = s.lossStreak;
                _season = s.season <= 0 ? 1 : s.season;
                _lastPlayedSeason = s.lastPlayedSeason;
                _savedTier = s.savedTier;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Ranked] 读存档失败（" + StorePath + "）：" + e.Message + " ⇒ 用默认值");
            }
        }

        /// <summary>写盘。⚠️ 原版这一步是**上传服务器**（`RankedV3Event.RecordMatchFinished` 的云脚本）；
        /// 本地等价 = 写 `persistentDataPath/WarpforgeRanked/ranked.json`，形状照原版 save。</summary>
        public static void SaveNow()
        {
            Load();
            try
            {
                var s = new Save
                {
                    points = _points, isRanked = _isRanked, lossStreak = _lossStreak,
                    season = _season, lastPlayedSeason = _lastPlayedSeason, savedTier = _savedTier,
                };
                string json = JsonUtility.ToJson(s, true);
                // 🆕 2026-10-25（执行代理 R4 · 存档 Step 4「双写期」）：**不再自己写盘** —— 走
                // `SaveCenter.SaveThroughLegacy`：中央档 `PlayerSeason`（带 `_v`）+ 旧档 `WarpforgeRanked/ranked.json`
                // （**不带 `_v`**）各一份。
                // ⚠️ 本类**没有** `StoreOn` 那道门（切换前就是无条件写）；切换口内部仍判 `SaveCenter.StoreOn`：
                //    关着（批处理）⇒ 退回「只写旧档」⇒ **与切换前的行为逐字同**（只是换名变原子了）。
                string werr;
                if (!SaveCenter.SaveThroughLegacy(SaveKeys.PlayerSeason, StorePath, json, out werr))
                {
                    // 不许静默：写不进去要说出来
                    Debug.LogWarning("[Ranked] 存档写盘失败（" + StorePath + "）：" + werr);
                }
            }
            catch (Exception e)
            {
                // 不许静默：写不进去要说出来
                Debug.LogWarning("[Ranked] 存档写盘失败（" + StorePath + "）：" + e.Message);
            }
        }

        // ============================================================ 积分 → 段位（1:1 复刻）
        /// <summary>原版 `RankedV3Event.GetDivisionByPoints(long)`（方法体可读）。逐步对应：
        /// <code>
        /// int legendaryElo = LegendaryElo;
        /// if (points >= legendaryElo) idx = ranks.Count - 1;
        /// else idx = ranks.FindLastIndex(r => r.correspondingMMR &lt;= points);
        /// var rank = ranks[idx];
        /// int nextMmr = (idx + 1 &lt; ranks.Count) ? ranks[idx + 1].correspondingMMR : legendaryElo;
        /// int seals = Math.Max(rank.sealsRequiredToUpdate, 1);
        /// int step  = Math.Max((nextMmr - rank.correspondingMMR) / seals, 1);
        /// SealCount = (points - rank.correspondingMMR) / step;
        /// TotalSeals = rank.sealsRequiredToUpdate;
        /// IsLegendary = points >= legendaryElo;
        /// </code>
        /// ⚠️ **两处已知偏离**（都出声，不静默）：
        /// ① `FindLastIndex` 返回 -1（points 低于表里最小 MMR）时，原版直接 `ranks[-1]`（会抛）——
        ///    我们**夹到 0** 并出声一次（阈值表缺表头 MMR=0 的兜底）。
        /// ② 没有阈值表时返回 `Known = false`（原版永远有表），调用方据此不画或退回演示值。</summary>
        public static RankedDivision DivisionOf(long points)
        {
            Load();
            var d = new RankedDivision();
            if (!HasRankTable)
            {
                if (!_warnedNoTable) { _warnedNoTable = true; Debug.LogWarning("[Ranked] " + WhyNoRankTable); }
                return d;      // Known = false
            }
            int len = _ranks.Length;
            int idx;
            if (points >= _legendaryElo)
            {
                idx = len - 1;
            }
            else
            {
                idx = -1;
                for (int i = len - 1; i >= 0; i--)
                    if (_ranks[i].correspondingMMR <= points) { idx = i; break; }
                if (idx < 0)
                {
                    idx = 0;
                    Debug.LogWarning("[Ranked] 积分 " + points + " 低于阈值表最小 MMR（" + _ranks[0].correspondingMMR +
                                     "）—— 原版 `FindLastIndex` 返回 -1 会炸；我们夹到第 0 段。阈值表应含一条 MMR=0 的表头");
                }
            }
            var rank = _ranks[idx];
            int nextMmr = (idx + 1 < len) ? _ranks[idx + 1].correspondingMMR : _legendaryElo;
            int sealsTotal = Math.Max(rank.sealsRequiredToUpdate, 1);
            int step = Math.Max((nextMmr - rank.correspondingMMR) / sealsTotal, 1);
            long gained = points - rank.correspondingMMR;
            d.Known = true;
            d.Number = rank.division;
            d.RankNumber = rank.rank;
            d.SealCount = (int)(gained / step);
            d.TotalSeals = rank.sealsRequiredToUpdate;
            d.CorrespondingMMR = rank.correspondingMMR;
            d.IsLegendary = points >= _legendaryElo;
            d.DivisionLabelKey = "Ranked/Division/" + rank.division;
            d.RankLabelKey = "Ranked/Rank/" + rank.rank;
            return d;
        }

        /// <summary>当前段位（= 原版 `RankedV3Event.GetCurrentRankedDivision()` = `DivisionOf(CurrentScore)`）。</summary>
        public static RankedDivision CurrentDivision { get { return DivisionOf(Points); } }

        // ============================================================ 对局结算
        /// <summary>原版 `matchWon`：胜 **1** · 负 **0** · 平 **2**
        /// （`PlayEvent.MatchFinished` 三分支 / `RankedV3Event.OnMatchFinished` 的 `Draw` 支 `matchWon = 2f`）。</summary>
        public static int MatchWonValue(BattleLogData.Outcome outcome)
        {
            switch (outcome)
            {
                case BattleLogData.Outcome.Victory: return 1;
                case BattleLogData.Outcome.Draw: return 2;
                default: return 0;
            }
        }

        /// <summary>结算一局（原版客户端**能**做的那部分）：
        /// ① `matchWon` 1/0/2；② 胜 ⇒ 连败归零，负/平 ⇒ 连败 +1，`suspiciousGame` ⇒ **不记连败**；
        /// ③ 点数取 `PointDelta`（服务端才有；没有就 0 + 出声）。
        /// <para>🔴 **`preventLosingRank` / `preventLosingSeals` 不在这里生效** —— 它们的读取点在客户端方法体里查不到
        /// （只在 `RankedRank` 声明里出现），按原版口径应当由服务端在扣分时用；⛔ 别在这里自己发明一条保护规则。</para></summary>
        public static RankedMatchScore ApplyMatchResult(BattleLogData.Outcome outcome, bool suspicious = false)
        {
            Load();
            var r = new RankedMatchScore();
            r.Suspicious = suspicious;
            r.MatchWon = MatchWonValue(outcome);
            r.PointsBefore = _points;

            if (!suspicious)
            {
                if (outcome == BattleLogData.Outcome.Victory) _lossStreak = 0;
                else _lossStreak++;
            }

            if (PointDelta != null)
            {
                long delta = PointDelta(outcome);
                _points = Math.Max(0, _points + delta);   // 夹 0 是我们的选择：原版在服务端（不发负数下去）
                r.DeltaKnown = true;
            }
            else
            {
                if (!_warnedNoDelta) { _warnedNoDelta = true; Debug.LogWarning("[Ranked] " + WhyNoPointDelta); }
            }

            r.PointsAfter = _points;
            r.LossStreak = _lossStreak;
            SaveNow();
            return r;
        }

        // ============================================================ 模式 / 赛季
        /// <summary>原版 `RankedEventBase.ToggleRankedMode(bool)`：`IsRanked = isRanked;`
        /// 并同步 `GameStaticData.unrankedMultiplayer = !isRanked`（可读片段）。
        /// 我们没有 `GameStaticData` ⇒ 把 `!isRanked` 作为 `UnrankedMultiplayer` 暴露给调用方。</summary>
        public static void ToggleRankedMode(bool ranked)
        {
            Load();
            _isRanked = ranked;
            SaveNow();
            Debug.Log("[Ranked] 模式 = " + (ranked ? "Ranked" : "Unranked") +
                      "（原版 `RankedEventBase.IsRanked`）· `UnrankedMultiplayer` = " + (!ranked));
        }

        public static bool UnrankedMultiplayer { get { return !IsRanked; } }

        /// <summary>本赛季过期检查（= 原版 `RankedV3Event.Expire` 的判定那一半）：
        /// `CurrentScore != 0 && LastPlayedSeason == 0`？ —— 原版读的是 **`GameModes.LastPlayedSeason`**，
        /// 与 `SeasonNumber` 不同（`Expire` 里是 `if (lastPlayedSeason == 0) { 拉榜 → 开赛季切换弹窗 }`）。
        /// ⇒ 本地等价：`LastPlayedSeason != SeasonNumber` 且还没结算过（`LastPlayedSeason == 0` 视作「从没结算」）。
        /// <para>⚠️ 原版 `Expire` 里那次 `LeaderboardManager.GetLeaderboard(…, 1000)` 是**服务器**（我们不做，见 `LeaderboardData`）。</para></summary>
        public static bool ShouldShowSeasonChange
        {
            get
            {
                Load();
                return _points != 0 && _lastPlayedSeason == 0;
            }
        }

        /// <summary>赛季切换弹窗被看过之后调（= 原版 `OpenSeasonChangePopup` 的完成回调里那一步）。</summary>
        public static void MarkSeasonCollected(int season)
        {
            Load();
            _lastPlayedSeason = season <= 0 ? _season : season;
            SaveNow();
            Debug.Log("[Ranked] 赛季结算已看过 ⇒ LastPlayedSeason = " + _lastPlayedSeason + "（赛季 " + _season + "）");
        }

        /// <summary>换季（升级赛季号并把 `LastPlayedSeason` 归 0 = 「赛季变更待结算」）。</summary>
        public static void AdvanceSeason(int newSeason)
        {
            Load();
            _season = newSeason <= 0 ? _season + 1 : newSeason;
            _lastPlayedSeason = 0;
            _savedTier = 0;
            SaveNow();
            Debug.Log("[Ranked] 进入赛季 " + _season + "（`LastPlayedSeason = 0` ⇒ 下次 `Expire` 会走赛季结算）");
        }

        // ============================================================ 段位显示（UI 的单一入口）
        /// <summary>UI 该显示哪个段位号：
        /// 有阈值表 ⇒ `DivisionOf(Points)`；没有 ⇒ 退回**演示值** `RankedDivisionInfo.DemoDivisionNumber`
        /// （那条「演示值」的口径在 `RankedDivisionInfo.DemoDivisionNumber` 的 doc 里，⛔ 别在这里再写第二份）。</summary>
        public static int DisplayDivisionNumber
        {
            get
            {
                var d = DivisionOf(Points);
                return d.Known ? d.Number : RankedDivisionInfo.DemoDivisionNumber;
            }
        }

        /// <summary>段位号 → 图名（走 `RankedDivisionInfo.DivisionArtName` 那张**唯一**表）。</summary>
        public static string DisplayDivisionArt
        {
            get { return RankedDivisionInfo.DivisionArtName(DisplayDivisionNumber); }
        }

        // ============================================================ 自检 / 测试口
        /// <summary>注入阈值表（自检用；也供将来真表能拿到时接进来）。
        /// ⚠️ 表里的 `division/rank` 是**原版字段值**，`sealsRequiredToUpdate/correspondingMMR` 由调用方给。</summary>
        public static void SetRanksForTest(RankedRank[] ranks, int legendaryElo = LegendaryEloFallback)
        {
            _ranks = ranks ?? Array.Empty<RankedRank>();
            _legendaryElo = legendaryElo <= 0 ? LegendaryEloFallback : legendaryElo;
            _warnedNoTable = false;
        }

        public static void SetPointsForTest(long points) { Load(); _points = Math.Max(0, points); SaveNow(); }
        public static void SetLossStreakForTest(int v) { Load(); _lossStreak = Math.Max(0, v); }
        public static void SetSeasonForTest(int season, int lastPlayed) { Load(); _season = season; _lastPlayedSeason = lastPlayed; }

        public static void ResetForTest()
        {
            _ranks = Array.Empty<RankedRank>();
            _legendaryElo = LegendaryEloFallback;
            PointDelta = null;
            _points = 0; _isRanked = false; _lossStreak = 0; _season = 1; _lastPlayedSeason = 1; _savedTier = 0;
            _loaded = true;
            _warnedNoTable = _warnedNoDelta = false;
        }

        // ============================================================ 加分活动 / 阵营状态（原版 RankedScoreBoost* / GetArmyState）
        static RankedBoostEvent[] _boosts = Array.Empty<RankedBoostEvent>();
        static int[] _featuredArmies = Array.Empty<int>();
        static readonly Dictionary<int, int> _wins = new Dictionary<int, int>();
        static readonly Dictionary<int, int> _losses = new Dictionary<int, int>();
        public static string LastOpponentName { get; private set; }

        public static RankedBoostEvent[] Boosts { get { return _boosts; } }

        public static void SetBoostsForTest(RankedBoostEvent[] boosts) { _boosts = boosts ?? Array.Empty<RankedBoostEvent>(); }
        public static void SetFeaturedArmiesForTest(int[] armies) { _featuredArmies = armies ?? Array.Empty<int>(); }
        public static void SetArmyRecordForTest(int army, int wins, int losses)
        {
            _wins[army] = Math.Max(0, wins); _losses[army] = Math.Max(0, losses);
        }

        /// <summary>= 原版 `RankedBaseData.GetBoostedFactionCount(army)`：影响该阵营的加分活动数。</summary>
        public static int GetBoostedFactionCount(int army)
        {
            int n = 0;
            for (int i = 0; i < _boosts.Length; i++) if (_boosts[i] != null && _boosts[i].Affects(army)) n++;
            return n;
        }

        /// <summary>= 原版 `RankedBaseData.IsFactionFeatured(army)`（本周特色阵营；数据源在服务器 ⇒ 可注入）。</summary>
        public static bool IsFactionFeatured(int army) { return Array.IndexOf(_featuredArmies, army) >= 0; }

        /// <summary>= 原版 `RankedScoreBoostHandler.GetBoostsFor(evt|eventId)`（取某活动挂的加成）。</summary>
        public static RankedBoostEvent[] GetBoostsFor(string eventId)
        {
            var list = new System.Collections.Generic.List<RankedBoostEvent>();
            for (int i = 0; i < _boosts.Length; i++)
                if (_boosts[i] != null && _boosts[i].Includes(eventId)) list.Add(_boosts[i]);
            return list.ToArray();
        }

        /// <summary>= 原版 `RankedV3Event.GetArmyState(army, extraBoosts)`：
        /// `!IsLegendaryElo ⇒ None`；`wins >= GetBoostedFactionCount` 且**有该阵营的加成** ⇒ `Boosted`；
        /// `IsFactionFeatured` ⇒ `Featured`；`losses >= 0` 且**有加成带 Shield** ⇒ `Shielded`。
        /// <para>⚠️ 原版里 `Boosted` 那一支还看「该阵营在该活动里的已用胜场 &gt; 0」等两个 **predicate 体**
        /// （ILSpy 里丢类型）⇒ 条件式按**可读片段**复刻；这一条已登记在 `判据/泛型与启动链.md` §六·b。</para></summary>
        public static RankedArmyFlags ArmyState(int army, RankedBoostEvent[] extraBoosts = null)
        {
            Load();
            if (!_legendaryEloKnown) return RankedArmyFlags.None;   // = 原版 `if (!IsLegendaryElo) return None;`
            var src = extraBoosts ?? _boosts;
            int wins = _wins.TryGetValue(army, out var w) ? w : 0;
            int losses = _losses.TryGetValue(army, out var l) ? l : 0;
            var flags = RankedArmyFlags.None;
            if (wins >= GetBoostedFactionCount(army) && AnyBoost(src, army, shieldOnly: false)) flags |= RankedArmyFlags.Boosted;
            if (IsFactionFeatured(army)) flags |= RankedArmyFlags.Featured;
            if (losses >= 0 && AnyBoost(src, army, shieldOnly: true)) flags |= RankedArmyFlags.Shielded;
            return flags;
        }

        static bool AnyBoost(RankedBoostEvent[] src, int army, bool shieldOnly)
        {
            for (int i = 0; i < (src ?? Array.Empty<RankedBoostEvent>()).Length; i++)
            {
                var b = src[i];
                if (b == null || !b.Affects(army)) continue;
                if (shieldOnly) { if (b.GrantsShield) return true; }
                else if (b.WinsFor(army) > 0) return true;
            }
            return false;
        }

        /// <summary>原版 `IsLegendaryElo`（= `LegendaryElo` 数据非 0；缺省 2500 ⇒ 恒 true）。
        /// 这里保守地按「有点数表/有段位」判断：没有阈值表 && Points==0 ⇒ 仍按 true（与原版缺省一致）。</summary>
        static bool _legendaryEloKnown = true;

        /// <summary>= 原版 `RankedV2Event.GetDivisionByPoints`：**同一张 `rankedRanks` 表**，但「传奇线」是**写死的 2500**
        /// （方法体可读：`if (points >= 2500) idx = last`），**不读** `LegendaryElo` 数据字段。</summary>
        public static RankedDivision DivisionOfV2(long points)
        {
            int saved = _legendaryElo;
            _legendaryElo = 2500;
            try { return DivisionOf(points); }
            finally { _legendaryElo = saved; }
        }

        /// <summary>= 原版 `ProfileRankingSection.HasPlayerReachedLegendaryDivision(highestElo, …)` 的本地判定：
        /// 最高分 ≥ LegendaryElo（或段位号 = 表尾）。</summary>
        public static bool HasReachedLegendaryDivision(int highestElo)
        {
            if (highestElo >= LegendaryElo) return true;
            var d = DivisionOf(highestElo);
            return d.Known && d.IsLegendary;
        }

        /// <summary>= 原版 `PlayEvent.OnMatchFound` 的一跳：记下对手名（原版 `LastOpponentInfo`）。</summary>
        public static void NoteOpponent(string playerName)
        {
            LastOpponentName = playerName;
            Debug.Log("[Ranked] 记对手：" + (playerName ?? "<空>") + "（= 原版 `LastOpponentInfo`）");
        }

        /// <summary>自检：**一条灭自证**的断言集 —— 用注入的合成表跑算法边界。
        /// 返回 true 表示全过；`why` = 第一条失败的原因（全过时为空）。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            _loaded = true;            // 自检隔离：不碰存档（`Load()` 会读 `Application.persistentDataPath`）
            var saved = _ranks;
            int savedLegend = _legendaryElo;
            try
            {
                // 合成表：4 段，MMR 0/1000/2000/3000；每段要 10 个印章；传奇线 4000
                _ranks = new[]
                {
                    new RankedRank { division = 1, rank = 1, sealsRequiredToUpdate = 10, correspondingMMR = 0 },
                    new RankedRank { division = 2, rank = 1, sealsRequiredToUpdate = 10, correspondingMMR = 1000 },
                    new RankedRank { division = 3, rank = 1, sealsRequiredToUpdate = 10, correspondingMMR = 2000 },
                    new RankedRank { division = 4, rank = 1, sealsRequiredToUpdate = 10, correspondingMMR = 3000 },
                };
                _legendaryElo = 4000;

                var d0 = DivisionOf(0);
                if (!d0.Known || d0.Number != 1 || d0.SealCount != 0 || d0.IsLegendary)
                    { why = "points=0 应为 D1 印 0（实际 " + d0 + "）"; return false; }

                var d999 = DivisionOf(999);
                var d1000 = DivisionOf(1000);
                // 🔴 灭自证：**阈值两侧必须落不同段** —— 若有人把查表改成「恒返回某一段」，
                //    这条与下面 d2000 那条不可能同时成立。
                if (d999.Number == d1000.Number)
                    { why = "999 与 1000 落在同段（查表没按 correspondingMMR 走）"; return false; }
                if (d1000.Number != 2) { why = "1000 应为 D2（实际 D" + d1000.Number + "）"; return false; }

                // 段内印章：D2 起点 1000、下一段 2000、需要 10 个 ⇒ step = 100；1500 ⇒ 5 个
                var d1500 = DivisionOf(1500);
                if (d1500.SealCount != 5 || d1500.TotalSeals != 10)
                    { why = "1500 应为 D2 印 5/10（实际 " + d1500 + "）"; return false; }

                var d3999 = DivisionOf(3999);
                var d4000 = DivisionOf(4000);
                if (d3999.IsLegendary) { why = "3999 不该是传奇"; return false; }
                if (!d4000.IsLegendary || d4000.Number != 4)
                    { why = "4000 应为 D4 且 IsLegendary（实际 " + d4000 + "）"; return false; }

                // 没有表 ⇒ Known=false（而不是编一个段位）
                _ranks = Array.Empty<RankedRank>();
                if (DivisionOf(1234).Known) { why = "没有表却返回了 Known=true"; return false; }

                // matchWon：平局必须是 2（不是 0）
                if (MatchWonValue(BattleLogData.Outcome.Draw) != 2 ||
                    MatchWonValue(BattleLogData.Outcome.Victory) != 1 ||
                    MatchWonValue(BattleLogData.Outcome.Defeat) != 0)
                    { why = "matchWon 映射不是 1/0/2"; return false; }
                return true;
            }
            finally
            {
                _ranks = saved;
                _legendaryElo = savedLegend;
            }
        }
    }
}
