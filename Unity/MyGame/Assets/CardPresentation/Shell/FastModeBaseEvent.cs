// FastModeBaseEvent.cs —— **快模式（遭遇/阵营战绩）活动基类**（原版 `FastModeBaseEvent<TS,TD>` /
//                          `FastModeSave` / `FastModeData` / `MilestoneReward` /
//                          `CollectFastModeRewardsResult` 的等价实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §二（实例表：`RankedFastMode` / `UnrankedFastModeEvent`）
//     与 §四 `FastModeBaseEvent<TS,` 的逐方法表（15 条 C）
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 行 258-276（U08 工作项 ①~⑦）
//   · 🥇 **真方法体在泛型重导里**（这是本批唯一一处「泛型体重导齐了」的类）：
//     `d:/2/tools/decomp_generic_trial/out/FastModeBaseEvent_object__object___<方法>__<VA>.c`
//     —— 14 个方法逐条读过（`GetArmyStats` / `GetArmyState` / `GetBaseData` / `GetBaseSave` /
//     `Expire` / `OnMatchDraw` / `OnMatchFinished` / `OnMatchFound` / `OnMatchLost` /
//     `OnMatchProcessSuccess` / `OnMatchWon` / `RecordMatchFinished` / `TryCollect` /
//     `GetMatchmakingFilter` / `GetSqlFilter` / `GetEventDeck`×2）
//   · 类型字段表：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/FastModeSave.cs` / `FastModeData.cs` /
//     `MilestoneReward.cs` / `CollectFastModeRewardsResult.cs` / `Everguild.LiveOps/RankedV3ArmyState.cs` /
//     `Everguild.LiveOps/RankedScoreBoostEvent.cs` / `Everguild.LiveOps/RankedScoreBoostData.cs`
//
// 🔴 **本类一行窗都没改**（B09 的独占文件只有 `LiveOpsEvent.cs` / `PlayEvent.cs` / `FastModeBaseEvent.cs`）。
//    账里那 16 条 `FastModeBaseEvent` 的 `N17` 备注写的是「本地无 FastMode 事件实例」—— **那仍然成立**：
//    本类只是把**事件层**建起来，没有一个 SO 会变成 `FastModeBaseEvent` 实例（本地 13 份 SO 里没有快模式活动）。
//
// ⚠️ **与原版的如实差异（⛔ 不是「已经一样」）**：
//   ① **三处服务器一跳全换成本地落地**：`RecordMatchFinished`(CloudHandle 0x457) ·
//      `OnMatchFinished`(0x17f) · `TryCollect`/`Expire`(0x455)。原版是「打云脚本 → 等成功回调 →
//      再落地」，**带重试**；我们 = **直接写本地进度 + 立刻回调**（`判据/B9_日常C_施工单.tsv` 行 258 施工步骤③
//      明确要求这么做）。⛔ 这不是等价：原版的重试/防重复提交那两层语义**没有**。
//   ② **排行榜没有**：`CurrentScore` 原版读 `PlayEventExtensions.GetCurrentStats(this).value`
//      （服务器排行榜条目）；我们的 P2P 局没有排行榜 ⇒ 见 `FastModeSave.localMilestoneScore` 的 🔶 注释。
//   ③ **匹配过滤串没有消费方**：原版返回一段 PlayFab SQL（`GetSqlFilter` 拼 `rating>=… and …`），
//      喂给大厅匹配；我们的联机是自有 P2P 协议（`Net/*`），**没有过滤层** ⇒ 本类只负责**按原版算式算上下界**
//      并拼出可读串（判据：`GetMatchmakingFilter__1a10120.c` 的 `cur − delta…cur + delta` 与
//      `gamesLost % arr.Length` 取档）。
//
// 🔶 本文件里带 `🔶 自己设计` 标记的块 = 判据拿不到 / 必须我们自己定口径的地方（铁律 18）。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 原版 `MilestoneReward`
    /// <summary>原版 `MilestoneReward`（`MilestoneReward.cs`）：`int milestone` + `RewardInfo[] rewards`。
    /// <para>🔶 自己设计（非原版）：`RewardInfo` 属**奖励/背包线**（`ItemInfo&lt;ObtainableItem&gt;` 一系），
    /// 本工程的奖励分发还没有那一层 ⇒ 这里只存**奖励 id 表**（`TryCollect` 唯一会读的就是「有几档、哪些档到了」）。
    /// 判据缺口：`RewardInfo` 的字段表在 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RewardInfo.cs` 可读，
    /// 但搬它会牵进 `ItemInfo`/`ObtainableItem`/`RewardTier` 整族 —— 不属本批（B09）的独占文件。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    [Serializable]
    public class MilestoneReward
    {
        public int milestone;
        public string[] rewardIds;
    }

    /// <summary>原版 `CollectFastModeRewardsResult`（字段逐个抄）：`RewardInfo[] rewards` +
    /// `FastModeSave updatedPersistence`。⚠️ 差异同 <see cref="MilestoneReward"/>（`string[] rewardIds`）。</summary>
    [Serializable]
    public class CollectFastModeRewardsResult
    {
        public string[] rewardIds;
        public FastModeSave updatedPersistence;
    }

    /// <summary>原版 `FastModeSave`（`FastModeSave.cs`，字段逐个抄）：`PlayEventSaveGame` + `int collectedRewards`
    /// （= **已领过的档数**，`TryCollect` 用它和「当前分数够得到的档数」比）。
    /// <para>🔶 自己设计（非原版）：`localMilestoneScore` 是我们加的**本地里程碑分数**
    /// （原版 `CurrentScore` 读服务器排行榜条目 `value`，本机没有排行榜 —— 用户口径：服务器等级 P3 不复刻）。
    /// 加它是为了让「里程碑能不能领」这条链**本地可跑、可断言**；⛔ 别把它当成原版字段。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    [Serializable]
    public class FastModeSave : PlayEventSaveGame
    {
        public int collectedRewards;
        public int localMilestoneScore;
    }

    /// <summary>原版 `FastModeData : PlayEventData`（`FastModeData.cs`，字段逐个抄）：
    /// `leaderboard`（键）/ `rewardsPerTier`（里程碑档表）/ `ratingDifferenceForMatchMaking`（ELO 档位差）/
    /// `maxEloCap`（**构造里硬写 15000**）。
    /// <para>🔴 `maxEloCap` 的初值 15000 在**构造函数**里（`.ctor` 的 `.c` 可见），不是字段初始化器 —— 我们照抄成初始化器。</para></summary>
    public class FastModeData : PlayEventData
    {
        public string leaderboard;
        public List<MilestoneReward> rewardsPerTier = new List<MilestoneReward>();
        public int[] ratingDifferenceForMatchMaking;
        public int maxEloCap = 15000;

        public string LeaderboardKey { get { return leaderboard; } }
        public int MaxEloCap { get { return maxEloCap; } }
        public int[] RatingDifferenceForMatchMaking { get { return ratingDifferenceForMatchMaking; } }
        public IReadOnlyList<MilestoneReward> RewardsPerTier { get { return rewardsPerTier; } }
    }

    /// <summary>原版 `RankedScoreBoostEvent`（`Everguild.LiveOps/RankedScoreBoostEvent.cs` 是
    /// `LiveOpsEvent&lt;RankedScoreBoostSave, RankedScoreBoostData&gt;` 的子类）—— **本类只搬
    /// `FastModeBaseEvent.GetArmyState` 读得到的那三格**。
    /// <para>🔶 自己设计（非原版）：那整个事件类属 **B11 排位加成线**（本批的独占文件里没有它）。
    /// 这里只留三个只读位 + 一个 `IsArmyAffectedAndHasBoostsLeft(army)`，好让 `GetArmyState` 的**算式**
    /// 能照原版写出来。⛔ 别拿它当 `RankedScoreBoostEvent` 的复刻 —— 它是**最小替身**。
    /// 调度台 C1 2026-10-27 定。</para>
    /// <para>判据（原版那两个谓词）：`GetArmyState__1a0f8d0.c` ——
    /// 谓词① = `e.IsArmyAffectedAndHasBoostsLeft(army)`（`RankedScoreBoostEvent.cs:119`）；
    /// 谓词② = 读 `e.Data + 0x64`（= `RankedScoreBoostData.scoreBoostAmount`，字段序见
    /// `RankedScoreBoostData.cs`：`preventScoreLoss/scoreBoostAmount/matchesCount`）并判 `&gt; 0`（⚠️ 该条的
    /// 「取反/边界」在 `.c` 里是 `flag2 == flag` 那套符号比较，**读法待核**，如实记）。</para></summary>
    public class RankedScoreBoostEvent
    {
        /// <summary>`RankedScoreBoostData.affectedArmies`（哪些阵营吃这个加成）。</summary>
        public CardArmy[] affectedArmies;
        /// <summary>`RankedScoreBoostData.scoreBoostAmount`（= 原版 `Data + 0x64`）。</summary>
        public int scoreBoostAmount;
        /// <summary>`RankedScoreBoostData.matchesCount` 减已用场次（原版 `IsArmyAffectedAndHasBoostsLeft` 判的就是
        /// 「这个阵营还吃得到加成吗」）。⚠️ 替身：由调用方直接给布尔。</summary>
        public bool hasBoostsLeft;

        /// <summary>原版 `IsArmyAffectedAndHasBoostsLeft(army)` = 「这个阵营在 affectedArmies 里」∧「还有剩余次数」。</summary>
        public bool IsArmyAffectedAndHasBoostsLeft(CardArmy army)
        {
            if (!hasBoostsLeft || affectedArmies == null) return false;
            for (int i = 0; i < affectedArmies.Length; i++)
                if (affectedArmies[i] == army) return true;
            return false;
        }
    }

    // ============================================================ 原版 `FastModeBaseEvent<TS,TD>`
    /// <summary>原版 `FastModeBaseEvent&lt;TS,TD&gt; : PlayEvent&lt;TS,TD&gt;` —— **快模式活动**（阵营战绩 + 里程碑）。
    /// <para>它比 `PlayEvent` 多三件：①**按阵营**的战绩表（`ArmyStatsV3`）②**里程碑奖励**（`RewardsPerTier` +
    /// `collectedRewards`）③**连败驱动**的里程碑推进（平局按 **2 格**走）。</para></summary>
    public abstract class FastModeBaseEvent<TS, TD> : PlayEvent<TS, TD>
        where TS : FastModeSave
        where TD : FastModeData
    {
        /// <summary>原版 `override PlayModes EventPlayMode =&gt; PlayModes.Skirmish`（快模式 = 遭遇战模式 13）。
        /// ✅ 硬判据：`FastModeBaseEvent.cs:407` 与 `GetEventPlayMode__1a121f0.c` 一致。</summary>
        public override PlayModes EventPlayMode { get { return PlayModes.Skirmish; } }

        /// <summary>原版 `LeaderboardKey =&gt; Data.LeaderboardKey`（`this+0x18` = `Data`，`+0x88` = `leaderboard`）。</summary>
        public string LeaderboardKey { get { return Data == null ? null : Data.LeaderboardKey; } }

        /// <summary>原版 `virtual int CurrentScore =&gt; PlayEventExtensions.GetCurrentStats(this).value`
        /// —— **排行榜里我这局的分数**（服务器）。
        /// <para>🔶 自己设计（非原版）：本机没有排行榜（P3）⇒ 读 `FastModeSave.localMilestoneScore`
        /// （本地里程碑分数）。⛔ 别把它当「服务器分数的等价物」—— 原版那个值是**跨设备/跨局累计**的。
        /// 调度台 C1 2026-10-27 定。</para></summary>
        public virtual int CurrentScore { get { return SaveGame == null ? 0 : SaveGame.localMilestoneScore; } }

        /// <summary>原版 `new FastModeData GetBaseData()` / `new FastModeSave GetBaseSave()` —— 类型化读口。</summary>
        public new FastModeData GetBaseData() { return Data; }
        public new FastModeSave GetBaseSave() { return SaveGame; }

        /// <summary>原版 `virtual ArmyStatsV3 GetArmyStats(CardArmy army)`（`GetArmyStats__1a0fbe0.c`）：
        /// <code>
        /// return GetValueOrDefault(SaveGame.armyStats, army, new ArmyStatsV3());   // ★ 缺省 new，不是 null
        /// </code>
        /// ✅ 判据：`.c` 里 `thunk_FUN_18042f4a0(...)` 先 `new` 再取 —— 缺省对象**先造出来**。
        /// ⚠️ 原版 `SaveGame == null` ⇒ 这一跳 NRE；我们出声返回 `new`（坏数据不崩）。</summary>
        public virtual ArmyStatsV3 GetArmyStats(CardArmy army)
        {
            if (SaveGame == null)
            {
                Debug.LogWarning("[FastMode] GetArmyStats：没有存档（原版这一跳 NRE）⇒ 返回空战绩");
                return new ArmyStatsV3();
            }
            return SaveGame.GetArmyStats(army);
        }

        /// <summary>原版 `virtual RankedV3ArmyState GetArmyState(CardArmy army, IReadOnlyList&lt;RankedScoreBoostEvent&gt; extraBoosts = null)`
        /// （`GetArmyState__1a0f8d0.c`，逐跳照抄）：
        /// <code>
        /// if (extraBoosts == null) extraBoosts = RankedScoreBoostHandler.GetBoostsFor(army).Where(谓词①).ToArray();
        /// if (extraBoosts.Any(谓词①)) {                    // 谓词① = e.IsArmyAffectedAndHasBoostsLeft(army)
        ///     if (extraBoosts.Any(谓词②)) return Boosted|Featured;   // 5
        ///     return Boosted;                                        // 1
        /// }
        /// return None;                                              // 0
        /// </code>
        /// 🔴 **返回 5 不是 4**：`5 = Boosted(1) | Featured(4)`（`.c` 里写死 `uVar6 = 5` / `= 1` / `= 0`）。
        /// ⚠️ 谓词②（`Data.scoreBoostAmount &gt; 0`）的边界读法**待核**，见 <see cref="RankedScoreBoostEvent"/>。</summary>
        public virtual RankedV3ArmyState GetArmyState(CardArmy army, IReadOnlyList<RankedScoreBoostEvent> extraBoosts = null)
        {
            if (extraBoosts == null)
            {
                var all = FastModeBoosts.GetBoostsFor(army);
                if (all == null) return RankedV3ArmyState.None;
                var filtered = new List<RankedScoreBoostEvent>();
                for (int i = 0; i < all.Count; i++)
                    if (all[i] != null && all[i].IsArmyAffectedAndHasBoostsLeft(army)) filtered.Add(all[i]);
                extraBoosts = filtered;
            }
            bool any = false, featured = false;
            for (int i = 0; i < extraBoosts.Count; i++)
            {
                var b = extraBoosts[i];
                if (b == null || !b.IsArmyAffectedAndHasBoostsLeft(army)) continue;
                any = true;
                if (b.scoreBoostAmount > 0) featured = true;
            }
            if (!any) return RankedV3ArmyState.None;
            return featured
                 ? (RankedV3ArmyState)((int)RankedV3ArmyState.Boosted | (int)RankedV3ArmyState.Featured)
                 : RankedV3ArmyState.Boosted;
        }

        /// <summary>原版 `OnMatchWon()`（`OnMatchWon__1a11660.c`）：
        /// <code>
        /// 记一次「本局战果」（.c 第一跳是 `FUN_1811277b0(this, &lt;Data 的某格+0x1b8&gt;)`）;
        /// if (SaveGame != null &amp;&amp; SaveGame.lastOpponentInfo != null) lastOpponentInfo.Result = 1;
        /// </code>
        /// ⚠️ **「哪一格 +1」读不出来**（泛型共享跳，实参被抹）；能确认的只有 `Result = 1`。
        /// 我们的本等价：**按阵营战绩 +1**（`ArmyStatsV3.wins`）+ `Result = 1` + 里程碑分数 +1
        /// （🔶 里程碑分数那一跳是自设计，见 `FastModeSave.localMilestoneScore`）。
        /// </summary>
        protected override void OnMatchWon()
        {
            if (SaveGame == null) return;
            RecordLocalOutcome(1);
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 1;
        }

        /// <summary>原版 `OnMatchLost()`：`lastOpponentInfo.Result = 0`（同族的 `.c` 写 `[info+0x40] = 0`）。</summary>
        protected override void OnMatchLost()
        {
            if (SaveGame == null) return;
            RecordLocalOutcome(-1);
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 0;
        }

        /// <summary>原版 `OnMatchDraw()`：`lastOpponentInfo.Result = 2`。
        /// 🔴 **平局按「2 格」推进**（`MatchFinished` 给的 `matchWon = 2f`；见 `PlayEvent.MatchFinished`）。</summary>
        protected override void OnMatchDraw()
        {
            if (SaveGame == null) return;
            RecordLocalOutcome(0);
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 2;
        }

        /// <summary>本地「记一局结果」的**唯一写口**（⛔ 别在三处各写一份）。
        /// 🔶 自己设计（非原版）：原版胜/负/平分别走 `GameModes.SetResultCount` / `SetLossStreak`
        /// 与 `Data` 的里程碑跳（那些都是 **handler/服务器状态**）；我们本地没有 `GameModes` handler 实例，
        /// 只有一份存档 ⇒ 统一写进 `ArmyStatsV3` + `localMilestoneScore`，口径：
        /// **胜 +1 格 / 平 +2 格 / 负 0 格**（「平局按 2 格」是原版明写的，胜 1 格取自 `MatchFinished` 的 `matchWon=1f`）。
        /// 调度台 C1 2026-10-27 定。</summary>
        private void RecordLocalOutcome(int kind)
        {
            var stats = SaveGame.EnsureArmyStats(CurrentArmy);
            if (kind > 0) { stats.wins += 1; SaveGame.localMilestoneScore += 1; }
            else if (kind < 0) { stats.losses += 1; }
            else { stats.draws += 1; SaveGame.localMilestoneScore += 2; }
        }

        /// <summary>本事件当前在打哪个阵营（原版从 `MatchData` / 当前卡组督军推；`RecordMatchFinished` 也带它）。
        /// 🔶 自己设计（非原版）：原版这条从 `PlayerDataManager.matchData` 取；我们把它做成可设属性。
        /// 调度台 C1 2026-10-27 定。</summary>
        public CardArmy CurrentArmy { get; set; }

        /// <summary>原版 `OnMatchFound(otherPlayerName)` 的 FastMode 覆盖（`OnMatchFound__1a10e60.c`）：
        /// 记日志 + 建 `LastOpponentInfo` 存进存档 + 两个回调（`MatchFoundCallback` / 一个 `Action&lt;string&gt;`）。
        /// ⚠️ 差异：没有 `MatchFoundCallback` 那一层（大厅线）⇒ 只存 `LastOpponentInfo.OpponentName` 并出声。</summary>
        protected override void OnMatchFound(string otherPlayerName)
        {
            base.OnMatchFound(otherPlayerName);
            if (SaveGame == null) return;
            if (SaveGame.lastOpponentInfo == null) SaveGame.lastOpponentInfo = new LastOpponentInfo();
            SaveGame.lastOpponentInfo.OpponentName = otherPlayerName;
        }

        /// <summary>原版 `OnMatchFinished(float matchWon, RankedMatchResult result, Action&lt;OnlineMatchResult&gt; callback)`
        /// （`OnMatchFinished__1a10a40.c`，逐跳照抄）：
        /// <code>
        /// suspicious = PlayerDataManager 当前玩家的「可疑对局」标志（.c 读 [player+0xb4]）;
        /// GameModes.SetResultCount(handler, …, result.targetMilestone);
        /// switch (result.battleWinner) {
        ///   Player(10): SetLossStreak(0);
        ///   Enemy(20):  if (!suspicious) SetLossStreak(GetLossStreak() + 1);
        ///   Draw(30):   matchWon = 2f;  if (!suspicious) SetLossStreak(GetLossStreak() + 1);
        /// }
        /// 打云脚本 RecordMatchFinished(0x17f) {eventId, matchWon, targetMilestone, opponentId…}
        ///   → HandleSuccess → PlayerDataManager.ProcessMatchResult → callback
        ///   → HandleRetry(err)   // 失败重试
        /// </code>
        /// 🔴 **平局也 +连败**（`Draw` 支里也 `SetLossStreak(+1)`）—— 这条在 `判据/泛型与启动链.md` §四 里
        /// 没写，是 `.c` 里读出来的，照抄。
        /// ⚠️ 本地等价：`SetResultCount` / `SetLossStreak` 那两个 handler 状态我们没有 ⇒
        /// 记进 `FastModeSave`（`lossStreak` 用 `defeatStreak` 语义的本地字段，见 <see cref="LocalDefeatStreak"/>），
        /// 然后**直接回调**（`RecordMatchFinished` 那一跳，无重试）。
        /// </summary>
        protected override void OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback)
        {
            if (SaveGame == null || result == null) return;
            bool suspicious = SuspiciousGame;
            switch (result.battleWinner)
            {
                case BattleWinner.Player: LocalDefeatStreak = 0; break;
                case BattleWinner.Enemy: if (!suspicious) LocalDefeatStreak = LocalDefeatStreak + 1; break;
                case BattleWinner.Draw:
                    matchWon = 2f;                                    // ★ 原版这一支会把 matchWon 改写成 2
                    if (!suspicious) LocalDefeatStreak = LocalDefeatStreak + 1;
                    break;
            }
            // 原版签名是 `RecordMatchFinished(int matchWon, …)`（账 row 186 逐字），而 `matchWon` 从
            // `MatchFinished` 下来是 float ⇒ 原版这一跳有隐式收窄，我们显式转（平局 2f → 2）。
            RecordMatchFinished((int)matchWon, result.targetMilestone, result.enemyMilestone, callback);
        }

        /// <summary>原版读的「本局可疑（疑似作弊）」标志（`PlayerDataManager` 当前玩家 `+0xb4`）。
        /// 🔶 自己设计（非原版）：我们没有那条判定链（反作弊属 P3）⇒ 恒 `false`，但口留着。
        /// 调度台 C1 2026-10-27 定。</summary>
        protected virtual bool SuspiciousGame { get { return false; } }

        /// <summary>本地连败计数（原版 = `GameModes.LossStreak`，服务器 handler 状态）。
        /// 🔶 自己设计（非原版）：见上一段的差异说明。调度台 C1 2026-10-27 定。</summary>
        public int LocalDefeatStreak { get; set; }

        /// <summary>原版 `OnMatchProcessSuccess(OnlineMatchResult result)`（`OnMatchProcessSuccess__1a110c0.c`）：
        /// <code>
        /// 云脚本 0x180（成就/统计上报）;
        /// LeaderboardManager.LocalScore(Data.LeaderboardKey, result.EndMatchData.NewRating, …);
        /// SaveGame.matchStarted      = false;      // +0x44
        /// SaveGame.lastOpponentInfo  = null;       // +0x58
        /// SaveGame.currentRating     = NewRating;  // +0x60
        /// if (EndMatchData.armyStats.Count &gt; 0) SaveGame.armyStats = EndMatchData.armyStats;   // +0x68
        /// if (EndMatchData.&lt;boost 表&gt; 有内容) 逐个更新 RankedScoreBoost;
        /// </code>
        /// ⇒ 语义 = **服务器确认后**才把「这局」落进存档（我方 `matchStarted` 清、对手信息清、分数更新）。
        /// ⚠️ 本地等价：这些「服务器确认」的字段里，`NewRating` / `armyStats` 本来就在我们自己的
        /// `RecordMatchFinished` 里写完了 ⇒ 这里只做**清局内态 + 落盘 + 本地榜计分**（`LeaderboardData.LocalScore` 已接），并出声说明「没有服务器确认」。</summary>
        protected virtual void OnMatchProcessSuccess(OnlineMatchResult result)
        {
            if (SaveGame == null) return;
            SaveGame.matchStarted = false;
            SaveGame.lastOpponentInfo = null;
            if (result != null) { SaveGame.currentRating = result.newRating; if (!string.IsNullOrEmpty(Data.LeaderboardKey)) LeaderboardData.LocalScore(Data.LeaderboardKey, result.newRating, LeaderboardScoringMode.SetValue); }   // 原版同刻还有 `LeaderboardManager.LocalScore(key, NewRating, mode=2)`（`OnMatchProcessSuccess__1a110c0.c`）
            SaveEventProgress(null, null);
            Debug.Log("[FastMode] OnMatchProcessSuccess：'" + GetID() + "' 局内态已清、存档已落"
                    + "（原版这一跳是『服务器确认后』才走的；本地没有服务器 ⇒ 由 RecordMatchFinished 直接调进来）");
        }

        /// <summary>原版 `Expire(Action onComplete)`（`Expire__1a0f700.c`，逐跳照抄）：
        /// <code>
        /// if (SaveGame == null) NRE;
        /// if (SaveGame.JoinState == NotJoined(0) &amp;&amp; &lt;虚方法 0x758&gt;() == 0) { base.Expire(onComplete); return; }
        /// if (LiveOpsAssetUtility.HasComponent(this, 0x15 /*组件类型 21*/))
        ///     WindowsManager.OpenWindow(LiveOpsAssetUtility.GetComponentReference(this, 0x15), this, …);
        /// TryCollect(() =&gt; onComplete());        // ← 过期前先把能领的里程碑领掉
        /// </code>
        /// ⚠️ 「组件 21 那扇窗」我们没有（活动窗是 `LiveOpsEventWindow` 一族，按类分不是按组件引用）
        /// ⇒ 那一跳**没搬**，出声记一条。
        /// 🔴 `<虚方法 0x758>` **已坐实 = `CurrentScore`**（2026-10-28 W7）：`RankedFastMode__GetCurrentRankedDivision.c` 把它的返回值直接当 points 传进 `GetDivisionByPoints`；
        /// `…TryCollect_b__0.c` = `virt() &lt; r.milestone` ⇒ 同一个量。故第二闸 = `CurrentScore == 0`，⛔ 不是「能不能开」。</summary>
        public override void Expire(Action onComplete)
        {
            if (SaveGame == null)
            {
                Debug.LogError("[FastMode] Expire：没有存档（原版这一跳 NRE）⇒ 不做事");
                return;
            }
            if (SaveGame.JoinState == PlayEventSaveGame.JoinStates.NotJoined && CurrentScore == 0)
            {
                base.Expire(onComplete);
                return;
            }
            Debug.Log("[FastMode] Expire：'" + GetID() + "' 过期结算（原版会先开组件 21 那扇窗并领掉里程碑）");
            TryCollect(onComplete);
        }

        /// <summary>原版 `TryCollect(Action onComplete = null)`（`TryCollect__1a11d20.c`，逐跳照抄）：
        /// <code>
        /// rewards = Data.RewardsPerTier;
        /// if (rewards == null) { onComplete?.Invoke(); return; }
        /// if (rewards.All(r =&gt; r.milestone &gt; CurrentScore)) { onComplete?.Invoke(); return; }   // 一档都够不到
        /// pending = rewards.Count(r =&gt; r.milestone &lt;= CurrentScore);
        /// if (SaveGame == null) → NRE;
        /// if (pending &lt;= SaveGame.collectedRewards) { onComplete?.Invoke(); return; }            // 没有新的可领
        /// ShowPopUp(...); 云脚本 CollectFastModeRewards(0x455) → OnCollectSuccess:
        ///     UpdateSaveGame(result.updatedPersistence); RewardService.Collect(result.rewards); onCollected → onComplete
        /// 失败支 → ErrorPopupWindowController.ShowErrorMessage(GenericConnectionError)
        /// </code>
        /// 🔴 **判据的两条闸**：`milestone &gt; CurrentScore` 才算「还没到」；`pending &gt; collectedRewards` 才算「有新可领」。
        /// ⚠️ 本地等价：没有服务器 ⇒ 直接把 `collectedRewards` 推到 `pending` 并把「本次可领」的奖励 id 交出去
        /// （<see cref="OnRewardsCollected"/>），成功回调立刻走（**没有重试、没有防重复提交**）。
        /// </summary>
        public void TryCollect(Action onComplete = null)
        {
            if (Data == null)
            {
                if (onComplete != null) onComplete();
                return;
            }
            var rewards = Data.RewardsPerTier;
            if (rewards == null)
            {
                if (onComplete != null) onComplete();
                return;
            }
            int score = CurrentScore;
            bool anythingReachable = false;
            int pending = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                var r = rewards[i];
                if (r == null) continue;
                if (r.milestone <= score) { anythingReachable = true; pending++; }
            }
            if (!anythingReachable)
            {
                if (onComplete != null) onComplete();
                return;
            }
            if (SaveGame == null)
            {
                Debug.LogError("[FastMode] TryCollect：没有存档（原版这一跳 NRE）⇒ 回调照走，不领");
                if (onComplete != null) onComplete();
                return;
            }
            if (pending <= SaveGame.collectedRewards)
            {
                if (onComplete != null) onComplete();       // 没有新的可领（原版同样只回调）
                return;
            }
            // 本地等价：把「新可领的那几档」记下来 + 推进 collectedRewards + 落盘
            var newly = new List<string>();
            int seen = 0;
            for (int i = 0; i < rewards.Count; i++)
            {
                var r = rewards[i];
                if (r == null || r.milestone > score) continue;
                seen++;
                if (seen > SaveGame.collectedRewards && r.rewardIds != null)
                    for (int k = 0; k < r.rewardIds.Length; k++) newly.Add(r.rewardIds[k]);
            }
            SaveGame.collectedRewards = pending;
            SaveEventProgress(null, null);
            var collect = new CollectFastModeRewardsResult
            {
                rewardIds = newly.ToArray(),
                updatedPersistence = SaveGame,
            };
            Debug.Log("[FastMode] TryCollect：'" + GetID() + "' 可领 " + newly.Count + " 件（档数 " + pending
                    + "；原版打 CollectFastModeRewards 云脚本 + 有重试，本地直接落地）");
            if (OnRewardsCollected != null) OnRewardsCollected(collect);
            if (onComplete != null) onComplete();
        }

        /// <summary>原版 `private void RecordMatchFinished(int matchWon, int targetMilestone, int enemyMilestone,
        /// Action&lt;OnlineMatchResult&gt; callbackOnSuccess)`（`RecordMatchFinished__1a116b0.c`，逐跳照抄）：
        /// <code>
        /// if (SaveGame == null || SaveGame.lastOpponentInfo == null) NRE;      // ★ 对手信息必须已经在
        /// rating = PlayerDataManager 当前玩家分数（默认 200）;  suspicious = [player+0xb4];
        /// CustomDebug.Log(string.Format(…, matchWon));
        /// 云脚本 RecordMatchFinished(0x457) {eventId, matchWon, targetMilestone, enemyMilestone, rating,
        ///                                  suspicious, 对手各字段…}
        ///   → HandleSuccess(result): PlayerDataManager.ProcessMatchResult(this, matchType, result, …) → callback
        ///   → HandleRetry(err)
        /// </code>
        /// 🔴 **`lastOpponentInfo == null` 原版直接 NRE**（不是「跳过」）—— 我们出声 + 不结算（坏数据不崩）。
        /// ⚠️ 本地等价：写进度（已在 `OnMatchWon/Lost/Draw` 里写过）+ **立刻** `OnMatchProcessSuccess` +
        /// `callbackOnSuccess(new OnlineMatchResult{success=true})`。**没有重试**。
        /// </summary>
        private void RecordMatchFinished(int matchWon, int targetMilestone, int enemyMilestone,
                                        Action<OnlineMatchResult> callbackOnSuccess)
        {
            if (SaveGame == null) return;
            if (SaveGame.lastOpponentInfo == null)
            {
                Debug.LogError("[FastMode] RecordMatchFinished：没有 lastOpponentInfo（原版这一跳 NRE）⇒ 本局不结算。"
                             + " eventId = " + GetID());
                return;
            }
            Debug.Log("[FastMode] RecordMatchFinished：'" + GetID() + "' matchWon = " + matchWon
                    + " · targetMilestone = " + targetMilestone + " · enemyMilestone = " + enemyMilestone
                    + " · rating = " + SaveGame.currentRating
                    + "（原版打 RecordMatchFinished 云脚本 0x457；本地直接落地、无重试）");
            var ok = new OnlineMatchResult { success = true, newRating = SaveGame.currentRating };
            OnMatchProcessSuccess(ok);
            if (callbackOnSuccess != null) callbackOnSuccess(ok);
        }

        /// <summary>原版 `override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)`
        /// （`GetMatchmakingFilter__1a10120.c`，逐跳照抄；🔴 2026-10-28 W7 复读订正两处，见下两行判据）：
        /// <code>
        /// arr = Data.RatingDifferenceForMatchMaking;                       // 非 null 才有过滤
        /// delta = arr[tryCount % 3];  // ★ 是 **tryCount**、且模 **常量 3**（⛔ 不是 gamesLost、不是 arr.Length）
        /// min = max(0, currentElo - delta);
        /// max = currentElo + delta;
        /// if (tryCount == 2) { ranked = Data as IRankedData; if (ranked != null &amp;&amp; ranked.LegendaryElo &lt;= currentElo) { max = Data.MaxEloCap; min = Math.Min(min, ranked.LegendaryElo); } }
        /// isRanked = 数据接口槽 4 的 bool;  group = AlliancesManager.CurrentGroupId;
        /// blacklist = PlayerDataManager.GetSuspiciousIds().Append(自己的 id);
        /// return "…" + GetSqlFilter(max, min, group, isRanked, blacklist, gamesLost);
        /// </code>
        /// ⚠️ 差异（如文件头差异③）：原版返回**PlayFab SQL 串**喂匹配器；我们 P2P 没有过滤层 ⇒
        /// 本方法算**上下界**并按原版语义拼一可读串（`rating&gt;=min and rating&lt;=max`）、**没有消费方**（如实记）。
        /// ⚠️ 仍未搬：`GetSqlFilter` 的联盟 / 黑名单两段（本机没有那几条线）。
        /// 判据①：`.c` 里 `param_3`（= **tryCount**，IL2CPP 实参序 currentElo/tryCount/gamesLost）既做 `%3` 下标又做 `==2` 闸；
        /// 判据②：收紧上界的两量 = `RankedBaseData` 字段序 `legendaryElo(+0x98)` / `maxEloCap(+0x9c)`（与 Ghidra 偏移逐一对上）。</summary>
        public override string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost)
        {
            var arr = Data == null ? null : Data.RatingDifferenceForMatchMaking;
            if (arr == null || arr.Length == 0) { Debug.LogWarning("[FastMode] GetMatchmakingFilter：'" + GetID() + "' 没有 ratingDifferenceForMatchMaking（原版此处 IndexOutOfRange）"); return ""; }
            int idx = tryCount % 3;                          // 🔴 原版 = tryCount 模**常量 3**（⛔ 不是 gamesLost / arr.Length）
            if (idx < 0) idx += 3;                           // C# 的 % 对负数给负值（原版无此支，兜一下）
            if (idx >= arr.Length)                           // 原版此处抛 IndexOutOfRange ⇒ 档表应 ≥3 项
            {
                Debug.LogWarning("[FastMode] GetMatchmakingFilter：" + GetID() + " 的档表只有 " + arr.Length + " 项（原版要求 ≥3）");
                idx = arr.Length - 1;
            }
            int delta = arr[idx];
            int min = currentElo - delta; if (min < 0) min = 0;
            int max = currentElo + delta;
            var ranked = tryCount == 2 ? Data as IRankedData : null;   // 原版 = GetData<IRankedData>()：普通快模式 null ⇒ 跳过这一支
            if (ranked != null && ranked.LegendaryElo <= currentElo) { max = Data.MaxEloCap; if (ranked.LegendaryElo < min) min = ranked.LegendaryElo; }
            return BuildMatchmakingSql(min, max, gamesLost);   // 原版尾段 = `GetSqlFilter(max, min, group, isRanked, blacklist, gamesLost)` + 一句 Log
        }

        /// <summary>原版 `override void SetCustomMatchOptions(RoomOptions options, int currentElo)`
        /// （`SetCustomMatchOptions__1a11ae0.c` + ILSpy `:817`，逐句）：
        /// <code>
        /// options.CustomRoomProperties["C4"] = 1;
        /// options.CustomRoomProperties["C5"] = GameModes.GetHandler&lt;GameModes&gt;().LossStreak;
        /// options.CustomRoomPropertiesForLobby = new[]{ "C1", "C2", "C3", "C4", "C5" };
        /// </code>
        /// ⇒ 语义 = 把这局的「连败数」带进房间属性，供匹配器按连败放宽/收紧。
        /// <para>⚠️ 差异（账 row 275 的差异列）：我们的 `MsgStart` **没有** eventId/连败/ELO 字段 ⇒
        /// 这些键**写进对象也没有消费方**（如实记：算对了、用不上）。`LossStreak` 用本地的
        /// <see cref="LocalDefeatStreak"/>（原版是 `GameModes` handler 状态）。</para>
        /// <para>🔴 `C4` 的值在原版是**常量 1**（不是"已开局"之类可推的量 —— `.c` 里就是 `local_res18[0]=1`），照抄。</summary>
        public override void SetCustomMatchOptions(RoomOptions options, int currentElo)
        {
            if (options == null) return;
            options.Set("C4", 1);
            options.Set("C5", LocalDefeatStreak);
            options.CustomRoomPropertiesForLobby = new[] { "C1", "C2", "C3", "C4", "C5" };
        }

        /// <summary>原版 `override CardDeck GetEventDeck()` / `GetEventDeck(CardArmy)` 的 FastMode 覆盖 ——
        /// 🔴 **2026-10-28（B92·N1）就地改掉旧说法**「基类那两条已经就是这个语义，不再覆盖」：**不成立**。
        /// 逐句现读 `decomp_generic_trial/out/FastModeBaseEvent_object__object___GetEventDeck__1a0fdf0.c`（50 行）与 `…_GetEventDeck__1a0ff80.c`（54 行）⇒ 与 `PlayEvent` 那两条有**两处真实差异**：
        /// ① 无参版**没有**第三支（`GameModes.GetActiveEvent&lt;IRankedEvent&gt;()`）；
        /// ② 带参版拿到 `ArmyStatsV3.LastPlayedDeck` 后**直接 return**（不校验 `GameMode`）。⇒ 见类尾那两个 override。
        /// 本方法保留：只是「按阵营取」的显式转出口，便于调用点写清意图。</summary>
        public string GetEventDeckForArmy(CardArmy army) { return GetEventDeck(army); }

        /// <summary>原版 `_003CGetEventDeck_003Eb__12_0(CardDeck d)`（DisplayClass10_0 的 `b__0`）：
        /// `d != null &amp;&amp; d.Army == army &amp;&amp; d.GameMode == EventPlayMode`。</summary>
        public bool GetEventDeckArmyPredicate(string deckId, CardArmy army)
        {
            var src = PlayEventDecks.Source;
            if (src == null || string.IsNullOrEmpty(deckId)) return false;
            int i = src.IndexOfId(deckId);
            if (i < 0) return false;
            return src.ArmyAt(i) == army && src.GameModeAt(i) == EventPlayMode;
        }

        /// <summary>原版 `GetSqlFilter(...)`（`GetSqlFilter__1a103d0.c`）的等价物：把上下界/联盟/黑名单拼成串。
        /// 🔴 **2026-10-28（B92·N1c）就地重写**：真体**已全解**（见类尾 6 参重载）。本 2 参重载只剩「只拼评级区间」的便利调用，
        /// ⛔ 判据请用 6 参那个（原版就是 6 参：`max, min, guildId, isRanked, blacklistedUsers, gamesLost`）。
        /// 本重载只服务「只想看评级区间」的调用；⛔ 别把它当原版行为（原版没有 2 参形态）。</summary>
        protected static string GetSqlFilter(int maxRating, int minRating)
        {
            return GetSqlFilter(maxRating, minRating, null, false, null, 0);
        }

        /// <summary>领到奖励时的出口（原版走 `RewardService.Collect` + `GameAnalytics.RecordTransaction`）。
        /// 🔶 自己设计（非原版）：奖励分发线不在本批 ⇒ 只给一个可注入的回调。调度台 C1 2026-10-27 定。</summary>
        public static Action<CollectFastModeRewardsResult> OnRewardsCollected;

        // ============================================================ 🆕 2026-10-28（B92·N1 · 账 row 4 / row 5）
        //   FastMode 对 `GetEventDeck()` / `GetEventDeck(CardArmy)` 的**覆盖**（原版是 override，不是继承基类语义）。
        //   判据（**真体现读，泛型体**，`decomp_generic_trial/out/`）：
        //     · `FastModeBaseEvent_object__object___GetEventDeck__1a0fdf0.c`（50 行，形参 = (this, 泛型上下文)）
        //         `save = this+0x20; if (save == null) NRE;`
        //         `id = save+0x38 (defaultDeckId);`
        //         `if (id != null && id.Trim().Length > 0) { deck = AssetLocator.GetAsset(id);
        //              if (deck != null && deck.GameMode == EventPlayMode && ValidateDeck(deck, out _, true)) return deck; }`
        //         `return AssetLocator.GetAssets<CardDeck>().FirstOrDefault(<b__12_0 谓词>);`   ← ★ **到此结束**
        //     · `…_GetEventDeck__1a0ff80.c`（54 行，形参 = (this, **CardArmy army**, 泛型上下文)）
        //         `stats = GetArmyStats(army);  deck = stats.LastPlayedDeck;`
        //         `if (deck != null) return deck;`                       ← ★ **不校验 GameMode**
        //         `return InventoryManager.GetInventory<CardDeck>().GetItemInstances(true).FirstOrDefault(<b__0 谓词>);`
        //   两个谓词（逐行现读）：
        //     · `b__12_0`（`…_GetEventDeck_b__12_0__1a12120.c`）= `d.GameMode == EventPlayMode && DeckUtility.ValidateDeck(d, out _, true)`
        //     · `b__0`（`FastModeBaseEvent.__c__DisplayClass10_0_object__object____GetEventDeck_b__0.c`）=
        //        `d.Army == army && d.GameMode == EventPlayMode`
        //   🔴 与 `PlayEvent` 那两条的差异（⛔ **别**当成「基类已覆盖」而删掉这两个 override）：
        //     ① 无参版**没有**第三支 `GameModes.GetActiveEvent<IRankedEvent>().GetEventDeck()`；
        //     ② 带参版对 `LastPlayedDeck` **不校验 GameMode**（`PlayEvent` 的版本校验）。
        //   ⚠️ 照抄原版的「Trim 只用于判空」：判空用 `Trim()`，查表用**原值**（原版就是这么喂 `AssetLocator.GetAsset`）。

        /// <summary>原版 FastMode `override CardDeck GetEventDeck()`（判据见上）。
        /// 本地等价：`defaultDeckId`（判空用 Trim、查表用原值）→ 扫全表（模式对 ∧ 可打）。
        /// ⚠️ **没有** `PlayEventRankedDeck` 那一支（原版 FastMode 也没有）。</summary>
        public override string GetEventDeck()
        {
            var src = PlayEventDecks.Source;
            if (src == null)
            {
                Debug.LogError("[FastMode] GetEventDeck：没有注入 IPlayEventDeckSource ⇒ 取不到卡组");
                return null;
            }
            if (SaveGame != null && !string.IsNullOrEmpty(SaveGame.defaultDeckId)
                                 && SaveGame.defaultDeckId.Trim().Length > 0)
            {
                int i = src.IndexOfId(SaveGame.defaultDeckId);
                if (i >= 0 && src.GameModeAt(i) == EventPlayMode && src.IsPlayableAt(i))
                    return src.IdAt(i);
            }
            for (int i = 0; i < src.Count; i++)
                if (GetEventDeckPredicate(src.IdAt(i))) return src.IdAt(i);
            return null;                       // ★ 无参版到此结束（原版没有 ranked 那一支）
        }

        /// <summary>原版 FastMode `override CardDeck GetEventDeck(CardArmy army)`（判据见上）：
        /// 先认 `ArmyStatsV3.LastPlayedDeck`（**不校验模式**，原版就是这么直返），否则扫全表
        /// （`Army == army ∧ GameMode == EventPlayMode`，**不带 ValidateDeck**）。</summary>
        public override string GetEventDeck(CardArmy army)
        {
            var src = PlayEventDecks.Source;
            if (src == null)
            {
                Debug.LogError("[FastMode] GetEventDeck(army)：没有注入 IPlayEventDeckSource ⇒ 取不到卡组");
                return null;
            }
            if (SaveGame != null)
            {
                var stats = SaveGame.GetArmyStats(army);
                if (stats != null && !string.IsNullOrEmpty(stats.LastPlayedDeckId))
                {
                    // 原版 `LastPlayedDeck` 是**已解析的资产引用**：卡组被删掉时它解析成 null ⇒ 落到扫表那一支。
                    // 我们的等价判据 = 「这个 id 还在卡组表里」（`IndexOfId`）；⛔ 但**不校验 GameMode**（原版不校验）。
                    int j = src.IndexOfId(stats.LastPlayedDeckId);
                    if (j >= 0) return src.IdAt(j);
                }
            }
            for (int i = 0; i < src.Count; i++)
                if (GetEventDeckArmyPredicate(src.IdAt(i), army)) return src.IdAt(i);
            return null;
        }

        // ============================================================ 🆕 2026-10-28（B92·N1c · 账 row 6 / row 41）
        //   `GetSqlFilter` 的**真体**（原版 6 参）＋ 它的两个私有依赖。
        //   判据（**全部现读**，两条源互相印证）：
        //     · `decomp_generic_trial/out/FastModeBaseEvent_object__object___GetSqlFilter__1a103d0.c`（205 行，VA 0x181A103D0）
        //        —— 结构：`new StringBuilder()` → `Append(<串>)` → `Append(param_1 maxRating)` → `Append(<串>)` → `Append(param_2 minRating)`
        //        → `Where(param_5 blacklistedUsers, x => !IsNullOrEmpty(x))` 逐个 `Append(<串>) / Append(x) / Append(<串>)`
        //        → `if (!IsNullOrEmpty(param_3 guildId)) { Append(<串>); Append(GetShortStringGuild(guildId)); Append(<串>); }`
        //        → `Append(<串>); Append(param_4 isRanked);` → `if (!FeatureConfig.ShouldDisableNewMatchMaking()) { Append(<串>); Append(param_6 gamesLost); }`
        //        → `return sb.ToString()`（虚槽 0x168 = `StringBuilder.ToString`）。
        //     · `cpp2il/out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:946`（**ILSpy 把 4 个字符串字面量保住了**，`.c` 里它们只剩 `DAT_`）：
        //        `Append("C1 < ")` / `Append(" AND C1 >= ")` / `Append(" AND C2 != \"")` / `Append("\"")`（两处）/
        //        `Append(" AND C3 != \"")` / `Append(" AND C4 == ")` / `Append(" AND C5 == ")`。
        //   🔴 这 4 个 `DAT_`（`0x18429e2f0` / `0x1842c7ea0` / `0x1842c7fa0` / `0x1842c81a0`）是 IL2CPP 的
        //      **metadata-usage 令牌**（`.data` 里存 `0xA000xxxx`，运行时由 `il2cpp_codegen_initialize_method` 填成 `Il2CppString*`），
        //      ⛔ 拿 `工具/read_literal.py` 读不出来（它只读数值），`stringliteral.json` 也**不带地址** ⇒ 只能从 ILSpy 那份取字面量。
        //   🔴 差异（如实记，⛔ 不是「已经一样」）：
        //      ① `guildId` 来源 = 原版 `AlliancesManager.Instance.CurrentGroupId` —— 本机**没有联盟线** ⇒ 走注入点，不注入 = null（C3 段不拼 + 出声一次）。
        //      ② `blacklistedUsers` = 原版 `PlayerDataManager.Instance.GetSuspiciousIds()` **再 Append 自己的 playerId**（`+0x1c8`）——
        //         本机既无作弊名单也无「自己的 playerId」这一格 ⇒ 走注入点，不注入 = null（C2 段不拼 + 出声一次）。
        //      ③ `isRanked` = 原版 `(Data as IRankedData).<接口槽 4>`（`FUN_1800021f0(4,…)`，**那个成员的函数名未解**；
        //         用 `Data is IRankedData` 顶 —— 语义同（C4 ↔ `SetCustomMatchOptions` 写的 `C4 = 1`），但**这一格是近似、未坐实**）。
        //      ④ `FeatureConfig.ShouldDisableNewMatchMaking()` 读**远端** FeatureConfig（`FeatureConfig__ShouldDisableNewMatchMaking.c`：
        //         `ConfigManager.GetConfig<FeatureConfig>() == null ⇒ false`，否则取 `+0x70`）⇒ 服务器已关服 ⇒ 取「拿不到 config」那一支
        //         = `false`（= 新匹配启用 ⇒ **加** C5 段）。共用 `DraftEventBase.DisableNewMatchMaking` 这一格（⛔ 别另立第二份开关）。

        /// <summary>原版 `SupportMethods.GetShortStringGuild(string)`（`decomp_full/SupportMethods__GetShortStringGuild.c`，真体 2 句）：
        /// `return param_1.Substring(param_1.Length - 6);` —— **取最后 6 个字符**。
        /// ⚠️ 长度 &lt; 6 时原版抛 `ArgumentOutOfRangeException`（`Substring` 自己抛）；我们同样让它抛（⛔ 不兜底，兜了就不是原版行为）。</summary>
        internal static string GetShortStringGuild(string guildId)
        {
            return guildId.Substring(guildId.Length - 6);
        }

        /// <summary>原版 `FeatureConfig.ShouldDisableNewMatchMaking()` 的取值（见上面差异④）：
        /// 走 <see cref="MatchmakingInputs.DisableNewMatchMaking"/> 那**一格全局开关**。
        /// 🔴 为什么不用 `DraftEventBase.DisableNewMatchMaking`：那个类**是泛型**（`DraftEventBase&lt;TS,TD&gt;`）⇒
        /// 泛型类里的 `static` 是**按闭合类型各一份**（本仓已记过这条坑）⇒ 它不是全局一格，⛔ 不能当共用开关。</summary>
        static bool ShouldDisableNewMatchMaking() { return MatchmakingInputs.DisableNewMatchMaking; }

        /// <summary>原版 `private static string GetSqlFilter(int maxRating, int minRating, string guildId, bool isRanked,
        /// IEnumerable&lt;string&gt; blacklistedUsers, int gamesLost)`（`.c` 205 行 ＋ ILSpy 字面量，逐段照抄）。
        /// <para>拼出来的形状：`C1 &lt; {max} AND C1 &gt;= {min}[ AND C2 != "{x}"…][ AND C3 != "{guildLast6}"][ AND C4 == {isRanked?1:0}][ AND C5 == {gamesLost}]`</para>
        /// <para>🔴 与 `DraftEventBase` 的同名方法**不是同一条**：那一条的区间是 `C1 &lt; {won+2n} AND C1 &gt; {won-2n}`（**严格大于**、中心是胜场数），
        /// 这一条是 `C1 &lt; {max} AND C1 &gt;= {min}`（**大于等于**、中心是 ELO）。⛔ 别合并。</para></summary>
        protected static string GetSqlFilter(int maxRating, int minRating, string guildId, bool isRanked,
                                            System.Collections.Generic.IEnumerable<string> blacklistedUsers, int gamesLost)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("C1 < ").Append(maxRating).Append(" AND C1 >= ").Append(minRating);
            if (blacklistedUsers != null)
            {
                foreach (var value in blacklistedUsers)          // 原版谓词 `x => !string.IsNullOrEmpty(x)` 写在调用方的 Where 里
                {
                    if (string.IsNullOrEmpty(value)) continue;
                    sb.Append(" AND C2 != \"").Append(value).Append("\"");
                }
            }
            if (!string.IsNullOrEmpty(guildId))
                sb.Append(" AND C3 != \"").Append(GetShortStringGuild(guildId)).Append("\"");
            sb.Append(" AND C4 == ").Append(isRanked ? 1 : 0);
            if (!ShouldDisableNewMatchMaking())
                sb.Append(" AND C5 == ").Append(gamesLost);
            return sb.ToString();
        }

        /// <summary>`GetMatchmakingFilter` 的尾段（原版那 6 行 `.c`：`GetSqlFilter(…6 参)` → `CustomDebug.Log("GetMatchmakingFilter Ranked with Filter: " + sql)` → return）。
        /// <para>四个外部输入的取口见 <see cref="MatchmakingInputs"/>；缺哪个就**出声一次**并跳过对应那一段（⛔ 不静默）。</para></summary>
        protected string BuildMatchmakingSql(int minRating, int maxRating, int gamesLost)
        {
            string group = null;
            if (MatchmakingInputs.CurrentGroupId != null) group = MatchmakingInputs.CurrentGroupId();
            else WarnOnceSql("CurrentGroupId", "原版 `AlliancesManager.Instance.CurrentGroupId`（本机没有联盟线）", "C3 段");

            System.Collections.Generic.List<string> blacklist = null;
            if (MatchmakingInputs.SuspiciousIds != null || MatchmakingInputs.OwnPlayerId != null)
            {
                blacklist = new System.Collections.Generic.List<string>();
                if (MatchmakingInputs.SuspiciousIds != null)
                    foreach (var id in MatchmakingInputs.SuspiciousIds()) if (!string.IsNullOrEmpty(id)) blacklist.Add(id);
                if (MatchmakingInputs.OwnPlayerId != null)
                {
                    var me = MatchmakingInputs.OwnPlayerId();
                    if (!string.IsNullOrEmpty(me)) blacklist.Add(me);        // 原版无条件 Append 自己的 playerId（`+0x1c8`）
                }
            }
            else WarnOnceSql("SuspiciousIds", "原版 `PlayerDataManager.Instance.GetSuspiciousIds()` + 自己的 playerId", "C2 段");

            bool isRanked = Data is IRankedData;                             // ⚠️ 差异③：近似（原版取 IRankedData 的接口槽 4）
            string sql = GetSqlFilter(maxRating, minRating, group, isRanked, blacklist, gamesLost);
            Debug.Log("GetMatchmakingFilter Ranked with Filter: " + sql);    // 原版字面量（ILSpy `FastModeBaseEvent.cs:939`）
            return sql;
        }

        static bool _warnedSqlC2, _warnedSqlC3;
        static void WarnOnceSql(string port, string what, string seg)
        {
            if (port == "SuspiciousIds") { if (_warnedSqlC2) return; _warnedSqlC2 = true; }
            else { if (_warnedSqlC3) return; _warnedSqlC3 = true; }
            Debug.Log("[FastMode] GetMatchmakingFilter：没有接「" + port + "」⇒ " + seg + " 不拼"
                    + "（" + what + "，本地没有那几条线；原版会拼进 SQL 过滤串）");
        }
    }

    /// <summary>`FastModeBaseEvent.GetMatchmakingFilter` 那四个「原版从别的单例拿」的输入（B92·N1c）。
    /// 🔶 自己设计（非原版）：原版直接摸 `AlliancesManager.Instance.CurrentGroupId` 与
    /// `PlayerDataManager.Instance.GetSuspiciousIds()` ＋ 自己的 playerId —— 本机**没有联盟线 / 没有作弊名单 / 没有 playerId 那一格**
    /// （关服 ⇒ 判据在远端；联盟与社交是 P2/P3）⇒ 做成可注入的静态委托，未注入就出声并跳过对应 SQL 段。
    /// 谁定的 + 日期：调度台 B92 2026-10-28。</summary>
    public static class MatchmakingInputs
    {
        /// <summary>原版 `AlliancesManager.Instance.CurrentGroupId`（`.c`：`AlliancesManager__get_CurrentGroupId`）。</summary>
        public static Func<string> CurrentGroupId;
        /// <summary>原版 `PlayerDataManager.Instance.GetSuspiciousIds()`（`.c`：`PlayerDataManager__GetSuspiciousIds`）。</summary>
        public static Func<System.Collections.Generic.IEnumerable<string>> SuspiciousIds;
        /// <summary>原版 `PlayerDataManager` 的 `+0x1c8` 那格（自己的 playerId，原版**无条件** Append 进黑名单 ⇒ 不会匹配到自己）。</summary>
        public static Func<string> OwnPlayerId;
        /// <summary>原版 `FeatureConfig.ShouldDisableNewMatchMaking()`（`FeatureConfig__ShouldDisableNewMatchMaking.c` 真体：
        /// `ConfigManager.GetConfig&lt;FeatureConfig&gt;() == null ⇒ false`，否则取 `<+0x70>`）。服务器已关服 ⇒ 取「拿不到 config」那一支 = `false`。
        /// 🔶 自己设计（非原版）：那一格来自**远端配置**，判据在远端；留一个静态可写位给「将来接上配置」用（⛔ 没判据别改成 true）。
        /// 谁定的 + 日期：调度台 B92 2026-10-28。</summary>
        public static bool DisableNewMatchMaking = false;

        /// <summary>自检口（⚠️ 只给断言用）。</summary>
        public static void ResetForTest() { CurrentGroupId = null; SuspiciousIds = null; OwnPlayerId = null; }
    }

    /// <summary>排位加成事件表的取用口（原版 `LiveOpsManager.GetHandler&lt;RankedScoreBoostHandler&gt;().GetBoostsFor(army)`）。
    /// 🔶 自己设计（非原版）：那个 handler 属 B11；本批只留一个注入点 + 「没注入 ⇒ 空表、出声一次」。
    /// 调度台 C1 2026-10-27 定。</summary>
    public static class FastModeBoosts
    {
        public delegate IReadOnlyList<RankedScoreBoostEvent> BoostsProvider(CardArmy army);

        public static BoostsProvider Provider;
        private static bool _warnedOnce;

        public static IReadOnlyList<RankedScoreBoostEvent> GetBoostsFor(CardArmy army)
        {
            if (Provider != null) return Provider(army);
            if (!_warnedOnce)
            {
                _warnedOnce = true;
                Debug.Log("[FastMode] 没有注入 FastModeBoosts.Provider ⇒ 加成表按空处理"
                        + "（原版走 LiveOpsManager.GetHandler<RankedScoreBoostHandler>().GetBoostsFor）");
            }
            return null;
        }

        /// <summary>自检口（⚠️ 只给断言用）。</summary>
        public static void ResetForTest() { Provider = null; _warnedOnce = false; }
    }
}
