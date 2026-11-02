// SimpleAIPartial_B120W8.cs — 执行写手 `B120-W8`（2026-11-02）· AI 难度表（原版 `AIBotsConfig.BotList` 那条链的本地等价）
//
// ============================ 它是什么 ============================
// 原版「对手难度」不是玩家可见的开关，而是一张**远端配置表** `AIBotsConfig.BotList`：
//   每条 `BotConfig` = `skipChance / minSkips / maxSkips / minRank / maxRank / minLossStreak / maxLossStreak / botDecks`；
//   开局按「玩家排位分 + 连败」挑**第一条**命中的，全不命中用 `BotList[0]`。
// 本文件把**表结构与挑选算法**照原版搬进来；**区间与档位**是 🔶 自己设计（原版表在远端，本机 0 命中，见文末）。
//
// ============================ 判据（2026-11-02 现读）============================
//  · 字段 / 偏移：`d:/2/tools/decomp_full/AIBotsConfig.BotConfig__.ctor.c`
//      ＋ `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AIBotsConfig.cs:10-24`
//      `botDecks +0x10 · skipChance +0x18 · minSkips +0x1c · maxSkips +0x20 ·
//       minRank +0x24 · maxRank +0x28 · minLossStreak +0x2c · maxLossStreak +0x30`
//  · 挑选（Normal）：`decomp_full/MatchData__GetNormalBotConfig.c:60-78` ——
//      `lossStreak ∈ [minLossStreak, maxLossStreak] ∧ rank ∈ [minRank, maxRank]`，**逐条 first-match**；
//      一条都不命中 ⇒ `config.BotList.get_Item(0)`。
//  · 🔴 **`rank` 的真身 = 排位分（`IRankedEvent.CurrentScore`），不是段位号**：
//      `MatchData__GetNormalBotConfig.c:54` 的 `FUN_1800021f0(1, <IPlayEventWithLeaderboard>, <activeEvent>)`
//      是**接口槽 1** 调用 = `get_CurrentScore()`（`d:/2/tools/il2cpp_out/dump.cs` 的 `IPlayEventWithLeaderboard`：
//      槽 0 `get_LeaderboardKey` / 槽 1 `get_CurrentScore`；`IRankedEvent : IOwnDeckPlayEvent : IPlayEventWithLeaderboard`），
//      它的返回值直接拿去和 `minRank/maxRank` 比。
//      ⛔ 别把 `RankedData.CurrentDivision.Number`（段位号）当 `rank` 喂进来 —— 量纲不同。
//  · FTUE：`decomp_full/MatchData__GetFTUEBotConfig.c:46-72` —— `winCount == entry.winCount` **精确相等**、
//      逐条 first-match、全不命中 ⇒ `[0]`；闸 = `decomp_full/MatchData__IsInFTUEState.c`：
//      `matchType ∉ {110 ClosedDeck, 120 ClosedDeckBot, 10 Ranked, 20 RankedBot, 170 Unranked, 180 UnrankedBot, 150 Duel}`
//      且 `CurrentModeVictories ≤ max(表内 winCount)`。
//  · FastMode：`decomp_full/MatchData__GetFastModeBotConfig.c` —— 挑选式与 Normal 完全相同，表换成 `BotListFastMode`。
//  · 🔴 **谁消费这三个值**：`decomp_full/AI__TweakAvailableActions.c:65-71` 调
//      `MatchData.GetBotConfigMinSkips / MaxSkips / SkipChance`，而那三个入口**只分 FTUE / Normal 两支**
//      （`MatchData__GetBotConfigSkipChance.c:17-27`：`IsInFTUEState ? GetFTUEBotConfig() : GetNormalBotConfig()`）
//      ⇒ **`BotListFastMode` 不参与难度旋钮的挑选**（原版它服务的是挑牌那条链 `MatchData.GetBotDeck`，不在本轮范围）。
//
// ============================ 🔶 数值为什么是我们定的 ============================
//  `AIBotsConfig` 是 PlayFab TitleData / LiveOps 远端配置（`ConfigManager.GetConfig<AIBotsConfig>()`）。
//  本地 `d:/2/新解包资源/assets_full` 与 `d:/2/关服前存档` 全盘搜 `skipChance` / `BotListFTUE` / `AIBotsConfig`
//  = **0 个文件**（2026-10-26 现搜；同一结论写在 `SimpleAI.KnobsOf` 的 doc 里）。
//  ⇒ 按铁律 18·0（**服务器相关是唯一允许自设计的门类**）：表结构与挑选算法照原版，
//    **区间与档位 🔶 自己设计**；四个档的旋钮数值仍然**只在 `KnobsOf` 一处**
//    （`SuperEasy 0.9/3 · Easy 0.6/2 · Normal 0.3/1 · Hard 0/0`），本文件不另立旋钮值。
//  · 区间设计口径（判据不足，按本作排位口径推）：
//      ① **连败越多越简单**（原版字段名就是 `minLossStreak/maxLossStreak`）；
//      ② **排位分越高越难**；
//      ③ 分界线取 `1500` = 本作 `LegendaryElo 2500`（`RankedData.LegendaryEloFallback`）与
//         `newbieELO 800`（`DraftEventBase` 的 `newbieELO = 800`）之间的中位 —— 本地 `RankedData.Points`
//         在服务器不给 delta 时恒 0（`RankedData.WhyNoPointDelta`）⇒ 默认落在「低分段」那一半。
//  · 覆盖性：两档 rank × 三段 loss = **9 格全满覆盖且互不重叠**（断言 `B120_W8_Assert` 逐格钉住）。

namespace RuleEngine
{
    public static partial class SimpleAI
    {
        // ==================================================================
        //  ① 表结构（原版 `AIBotsConfig.BotConfig` / `BotConfigFTUE` 的本地等价）
        // ==================================================================

        /// <summary>原版 `AIBotsConfig.BotConfig` 的一条（`BotList` 的元素）。
        /// <para>`skipChance / minSkips / maxSkips` 三格**折成** <see cref="Knobs"/> 指向的难度档 ——
        /// 三格的真值只住在 <see cref="SimpleAI.KnobsOf"/>（唯一出处，⛔ 别在这里再写一份旋钮数）。</para>
        /// <para>字段顺序 / 语义逐字照原版：区间**双边闭合**（`min ≤ v ≤ max`，`MatchData__GetNormalBotConfig.c:60-67`）。</para></summary>
        public struct BotTier
        {
            /// <summary>原版 `minRank`（+0x24）—— 排位分下界（**含**）。</summary>
            public int MinRank;
            /// <summary>原版 `maxRank`（+0x28）—— 排位分上界（**含**）。</summary>
            public int MaxRank;
            /// <summary>原版 `minLossStreak`（+0x2c）—— 连败下界（**含**）。</summary>
            public int MinLossStreak;
            /// <summary>原版 `maxLossStreak`（+0x30）—— 连败上界（**含**）。</summary>
            public int MaxLossStreak;
            /// <summary>这一档喂给 <see cref="SimpleAI.KnobsOf"/> 的难度档（真值见那里）。</summary>
            public AiDifficulty Knobs;
        }

        /// <summary>原版 `AIBotsConfig.BotConfigFTUE` 的本地等价 —— **挑选只用 `winCount`**
        /// （`MatchData__GetFTUEBotConfig.c:64-68` 的 `iVar2 == *(int*)(entry + 0x18)`，精确相等）。
        /// <para>原版那条还有 `waitTime / justForRanked / shouldUseFirstGameLogic` 三格；它们的消费方不在本链上
        /// （`waitTime` = 开局前等待、`shouldUseFirstGameLogic` = 首局逻辑、`justForRanked` = 只对 Classic 生效的闸），
        /// 本地都还没有消费点 ⇒ **不建空字段**（建了就是死格）。</para></summary>
        public struct BotTierFtue
        {
            /// <summary>原版 `winCount`（+0x18）。</summary>
            public int WinCount;
            /// <summary>这一档喂给 <see cref="SimpleAI.KnobsOf"/> 的难度档。</summary>
            public AiDifficulty Knobs;
        }

        // ==================================================================
        //  ② 三张表
        // ==================================================================

        /// <summary>🔶 自己设计（非原版）：**Normal 表的六档区间与档位**。
        /// 为什么自定：原版表在远端 `AIBotsConfig`（本机 0 命中，见文件头）· 调度台 2026-11-02 裁定。
        /// <para>设计口径：**连败越多越简单、排位分越高越难**；分界 `1500` 见文件头 ③。</para>
        /// <para>覆盖性（断言逐格钉住）：`rank ∈ [0,1499]` × `loss ∈ {[0,0], [1,2], [3,∞)}`
        /// ＋ `rank ∈ [1500,∞)` × `loss ∈ {[0,1], [2,3], [4,∞)}` = **满覆盖、不重叠**。</para>
        /// <para>⚠️ 顺序 = 原版的 first-match 顺序（今天这套矩形互不重叠 ⇒ 顺序不影响结果，
        /// 但别改成「按档位排序」—— 那会让将来补重叠区间时行为取决于排序而不是表）。</para></summary>
        static readonly BotTier[] NormalBotTiers = new BotTier[]
        {
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 0, MaxLossStreak = 0,             Knobs = AiDifficulty.Normal },     // 低分段 · 没有连败 ⇒ 正常
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 1, MaxLossStreak = 2,             Knobs = AiDifficulty.Easy },       // 低分段 · 连败 1–2 ⇒ 简单
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 3, MaxLossStreak = int.MaxValue,  Knobs = AiDifficulty.SuperEasy },  // 低分段 · 连败 ≥3 ⇒ 最简
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 0, MaxLossStreak = 1,            Knobs = AiDifficulty.Hard },       // 高分段 · 连败 ≤1 ⇒ 困难
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 2, MaxLossStreak = 3,            Knobs = AiDifficulty.Normal },     // 高分段 · 连败 2–3 ⇒ 正常
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 4, MaxLossStreak = int.MaxValue, Knobs = AiDifficulty.Easy },       // 高分段 · 连败 ≥4 ⇒ 简单
        };

        /// <summary>🔶 自己设计（非原版）：**FastMode 表的六档**。
        /// 为什么自定：同上（`BotListFastMode` 在远端）。
        /// <para>为什么**结构上与 Normal 分开、值暂时相同**：原版确实是两张独立的表（`GetFastModeBotConfig` 查
        /// `BotListFastMode`，`GetNormalBotConfig` 查 `BotList`，`AIBotsConfig.cs` 里它们也是两个字段），
        /// 所以这里必须是**独立数组**；但两份远端值我们都拿不到 ⇒ 暂给同一套 🔶 设计。
        /// ⚠️ **它在原版不喂难度旋钮**（判据见文件头「谁消费」）⇒ 今天没有消费点，见 <see cref="SelectFastMode"/>。</para></summary>
        static readonly BotTier[] FastModeBotTiers = new BotTier[]
        {
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 0, MaxLossStreak = 0,             Knobs = AiDifficulty.Normal },
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 1, MaxLossStreak = 2,             Knobs = AiDifficulty.Easy },
            new BotTier { MinRank = 0,    MaxRank = 1499,        MinLossStreak = 3, MaxLossStreak = int.MaxValue,  Knobs = AiDifficulty.SuperEasy },
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 0, MaxLossStreak = 1,            Knobs = AiDifficulty.Hard },
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 2, MaxLossStreak = 3,            Knobs = AiDifficulty.Normal },
            new BotTier { MinRank = 1500, MaxRank = int.MaxValue, MinLossStreak = 4, MaxLossStreak = int.MaxValue, Knobs = AiDifficulty.Easy },
        };

        /// <summary>🔶 自己设计（非原版）：**FTUE 表的四档**（`winCount → 难度`）。
        /// 为什么自定：`BotListFTUE` 在远端。
        /// <para>口径：引导期**胜场越多越难**（0 胜 = 最简，3 胜 = 困难）；`winCount > 3` 时
        /// `IsInFtueState` 已返回 false（`winCount ≤ max(表内 winCount)` 是原版那道闸）⇒ 不会走到这张表的兜底。</para></summary>
        static readonly BotTierFtue[] FtueBotTiers = new BotTierFtue[]
        {
            new BotTierFtue { WinCount = 0, Knobs = AiDifficulty.SuperEasy },   // 一胜未得 ⇒ 最简
            new BotTierFtue { WinCount = 1, Knobs = AiDifficulty.Easy },        // 赢过 1 场 ⇒ 简单
            new BotTierFtue { WinCount = 2, Knobs = AiDifficulty.Normal },      // 赢过 2 场 ⇒ 正常
            new BotTierFtue { WinCount = 3, Knobs = AiDifficulty.Hard },        // 赢过 3 场 ⇒ 困难（FTUE 的最后一档）
        };

        // ==================================================================
        //  ③ 挑选（原版 `MatchData.GetNormalBotConfig` / `GetFastModeBotConfig` / `GetFTUEBotConfig`）
        // ==================================================================

        /// <summary>原版 `MatchData.GetNormalBotConfig()` 的挑选式（`MatchData__GetNormalBotConfig.c:60-78`）：
        /// 逐条 first-match，`lossStreak` 与 `rank` **都双边闭合**；**一条都不命中 ⇒ `[0]`**。
        /// <para><paramref name="rank"/> = 排位分（原版 `IRankedEvent.CurrentScore`，⛔ 不是段位号）；
        /// <paramref name="lossStreak"/> = `GameModes.LossStreak`（`RankedSharedData.lossStreak`）。
        /// 负数 / 越界输入没有表命中 ⇒ 走 `[0]`（与原版同一条兜底，不抛）。</para></summary>
        public static AiDifficulty SelectNormal(int rank, int lossStreak)
        {
            return SelectFromTiers(NormalBotTiers, rank, lossStreak);
        }

        /// <summary>原版 `MatchData.GetFastModeBotConfig()`（`MatchData__GetFastModeBotConfig.c`）：
        /// 挑选式与 <see cref="SelectNormal"/> **完全相同**，表换成 `BotListFastMode`。
        /// <para>⚠️ **当前没有消费点，这是如实的**：原版把这张表用在**挑牌**（`MatchData.GetBotDeck`）那条链上，
        /// 而难度旋钮那条链（`AI.TweakAvailableActions` → `MatchData.GetBotConfig*`）**只分 FTUE / Normal**
        /// （`MatchData__GetBotConfigSkipChance.c:17-27`）⇒ ⛔ 别为了「看起来接上了」把 Skirmish 接到这里 ——
        /// 那会造出原版没有的行为。</para></summary>
        public static AiDifficulty SelectFastMode(int rank, int lossStreak)
        {
            return SelectFromTiers(FastModeBotTiers, rank, lossStreak);
        }

        /// <summary>原版 `MatchData.GetFTUEBotConfig(...)` 的挑选式（`MatchData__GetFTUEBotConfig.c:46-72`）：
        /// `winCount` **精确相等**才命中，逐条 first-match；**一条都不命中 ⇒ `[0]`**。
        /// <para>⚠️ 原版那条还有一层 `justForRanked` 闸（`!justForRanked || playMode == Classic(0)`）——
        /// 我们的 FTUE 表**没有 `justForRanked=true` 的条目**（🔶 设计如此）⇒ 那一层闸恒真、不落地
        /// （落地就是一段恒真的死代码）；将来若补这样一条，闸要加在 <see cref="IsInFtueState"/> 里。</para></summary>
        public static AiDifficulty SelectFtue(int winCount)
        {
            if (FtueBotTiers.Length == 0) return AiDifficulty.Normal;
            for (int i = 0; i < FtueBotTiers.Length; i++)
                if (FtueBotTiers[i].WinCount == winCount) return FtueBotTiers[i].Knobs;
            return FtueBotTiers[0].Knobs;                       // 原版：全不命中 ⇒ `BotListFTUE[0]`
        }

        /// <summary>共用的 first-match 扫描（两条同构的原版挑选式只有表不同 ⇒ 只写一份）。
        /// <para>区间比较**双边闭合**，逐字照 `MatchData__GetNormalBotConfig.c:60-67`。</para></summary>
        static AiDifficulty SelectFromTiers(BotTier[] tiers, int rank, int lossStreak)
        {
            if (tiers == null || tiers.Length == 0) return AiDifficulty.Normal;
            for (int i = 0; i < tiers.Length; i++)
            {
                BotTier t = tiers[i];
                if (lossStreak >= t.MinLossStreak && lossStreak <= t.MaxLossStreak
                    && rank >= t.MinRank && rank <= t.MaxRank)
                    return t.Knobs;
            }
            return tiers[0].Knobs;                              // 原版：一条都不命中 ⇒ `BotList[0]`
        }

        // ==================================================================
        //  ④ FTUE 闸（原版 `MatchData.IsInFTUEState`，`MatchData__IsInFTUEState.c`）
        // ==================================================================

        /// <summary>FTUE 表里最大的 `winCount`（= 原版 `IsInFTUEState` 里那个「表内最大 winCount」）。
        /// 现在 = 3（见 <see cref="FtueBotTiers"/>）；表改了这里自动跟着变，⛔ 别写死。</summary>
        public static int FtueMaxWinCount
        {
            get
            {
                int m = 0;
                for (int i = 0; i < FtueBotTiers.Length; i++)
                    if (FtueBotTiers[i].WinCount > m) m = FtueBotTiers[i].WinCount;
                return m;
            }
        }

        /// <summary>原版 `MatchData.IsInFTUEState()`（`decomp_full/MatchData__IsInFTUEState.c:25-60`）：
        /// <code>
        /// config = ConfigManager.GetConfig&lt;AIBotsConfig&gt;();
        /// if (config == null) return false;                       // 我们：配置不存在于本地 ⇒ 用「表在 = 启用」代替（见下）
        /// if (config.&lt;+0x60&gt; != 0) return false;                  // 远端开关（本机拿不到 ⇒ 视为 0 = 启用）
        /// if (matchType ∈ {110, 120, 10, 20, 170, 180, 150}) return false;
        /// return CurrentModeVictories() &lt;= max(BotListFTUE[i].winCount);
        /// </code>
        /// <para>⚠️ 两处本地拿不到的量：`AIBotsConfig`（远端）与它 `+0x60` 那个开关字节。
        /// 处理口径 = **「FTUE 表存在 ⇒ 视为启用」**（表是我们建的，等于我们替这个开关拍了「开」）；
        /// 若将来拿到真配置，把那个开关接进本方法第一条（⛔ 别在别处再判一次）。</para>
        /// <para><paramref name="winCount"/> &lt; 0 = **取不到**（LiveOps 共享数据缺失）⇒ 判不了 FTUE ⇒ false
        /// （调用方按「退回 Normal 并出声」处理，⛔ 不静默当成 0 胜）。</para></summary>
        public static bool IsInFtueState(MatchType matchType, int winCount)
        {
            if (winCount < 0) return false;
            switch (matchType)
            {
                case MatchType.Ranked:         //  10
                case MatchType.RankedBot:      //  20
                case MatchType.ClosedDeck:     // 110
                case MatchType.ClosedDeckBot:  // 120
                case MatchType.Unranked:       // 170
                case MatchType.UnrankedBot:    // 180
                case MatchType.Duel:           // 150
                    return false;                                  // 原版排除集（`IsInFTUEState.c:31-34` 的五个数 + 退化对）
            }
            return winCount <= FtueMaxWinCount;
        }

        // ==================================================================
        //  ⑤ 原版 `MatchData.CurrentModeVictories()` 用的模式键（挑选 FTUE 的输入）
        // ==================================================================

        static bool _warnedUnsolvedResultKey;

        /// <summary>原版 `MatchData.CurrentModeVictories()`（`decomp_full/MatchData__CurrentModeVictories.c`）里
        /// 「本局模式 → `ResultCounterSharedData` 的键」那一张表：
        /// <list type="bullet">
        ///   <item>playMode 0 / 11 ⇒ `"Ranked"`（`DAT_1842a6c38`，实解见 `判据/B88_R10_基础设施58条施工单.md:208`）</item>
        ///   <item>playMode 6 / 12 ⇒ `"Practice"`（`DAT_1842922d0`，与 `PracticeEvent` 传给 `SetResultCount` 的是同一个常量）</item>
        ///   <item>playMode 13 ⇒ `"FastMode"`（`DAT_1842be6c0`，同上）</item>
        ///   <item>其它 ⇒ 原版**直接 `return 0`**（不查表）</item>
        /// </list>
        /// <para>⚠️ playMode 7 (`ClosedDeck`) / 14 (`Battle4Warpforge`) 在 `.c` 里也各查一张键，但那两个字面量
        /// **还没实解** ⇒ 这里返回 null（调用方按 0 处理 = 与「该模式没有记录」等价）并**出声一次**
        /// （⛔ 不猜一个字符串去查表 —— 猜错会静默读到别的模式的胜场）。</para>
        /// <returns>null = 这个模式在原版不查表 / 键未实解。</returns></summary>
        public static string ResultCounterKeyForMode(GameMode mode)
        {
            switch (mode)
            {
                case GameMode.Classic:        return "Ranked";      // 0
                case GameMode.RankedFriendly: return "Ranked";      // 11
                case GameMode.OfflinePractice: return "Practice";   // 6
                case GameMode.OwnDeckTraining: return "Practice";   // 12
                case GameMode.Skirmish:       return "FastMode";    // 13
                case GameMode.ClosedDeck:                           // 7
                case GameMode.Battle4Warpforge:                     // 14
                    if (!_warnedUnsolvedResultKey)
                    {
                        _warnedUnsolvedResultKey = true;
                        UnityEngine.Debug.LogWarning(
                            "[AI] `MatchData.CurrentModeVictories` 对 ClosedDeck(7) / Battle4Warpforge(14) 用的是两个"
                          + "**还没实解的字面量键**（`DAT_1842856b0` / `DAT_18428af70`）⇒ 本仓按「该模式没有胜场记录」处理（= 0），"
                          + "⛔ 不去猜键名查表（猜错会读到别的模式的胜场）。");
                    }
                    return null;
                default:              return null;                  // 原版 `return 0`
            }
        }

        // ==================================================================
        //  ⑥ 只读表口（**只给断言 / 诊断**，⛔ 生产别改表）
        // ==================================================================

        /// <summary>Normal 表（只读口）。⛔ 生产代码禁止改这张数组 —— 要调区间就改本文件里的字面量。</summary>
        public static BotTier[] NormalBotTierTable { get { return NormalBotTiers; } }
        /// <summary>FastMode 表（只读口）。</summary>
        public static BotTier[] FastModeBotTierTable { get { return FastModeBotTiers; } }
        /// <summary>FTUE 表（只读口）。</summary>
        public static BotTierFtue[] FtueBotTierTable { get { return FtueBotTiers; } }
    }
}
