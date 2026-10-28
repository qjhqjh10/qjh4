// RankedEventBase.cs —— **排位活动的基类**（原版 `RankedEventBase<TSave,TData> : PlayEvent<TSave,TData>`）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `RankedEventBase<TSave,`（C 4 条，全 P1）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 同类名下那 4 行
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/RankedEventBase.cs`（272 行，**整类有真方法体**）；
//   配套数据类同目录 `RankedBaseData.cs`（**12 个字段 + 缺省值全部可读**）/ `IRankedData.cs` / `RankedV3ArmyState.cs`；
//   字段表 = `dump.cs:130887`。
//
// 🔴 **`RankedBaseData` 的四个「缺省 10 / 20 / 2 / 2500 / 15000」不是我们挑的** —— 判据是
//   `RankedBaseData.cs` 的构造函数（逐个字段赋值）：`lossShieldCount = 2` · `boostedFactionCount = 10` ·
//   `boostedFactionBonus = 10` · `defaultFactionBoostCount = 10` · `defaultFactionBonus = 20` ·
//   `legendaryElo = 2500`（字段初始化器）· `maxEloCap = 15000`（字段初始化器）。
//
// 🔴 **`GetArmyState` 在本类是「恒 None」** —— 判据：`RankedEventBase.GetArmyState` 的方法体**只有一句**
//   `return RankedV3ArmyState.None;`（真身是 `virtual`，由 `RankedV3Event` 覆盖）。
//   ⛔ 别在这里自己算「哪些阵营被加成」（那是子类的活）。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版 `GetDivisionByPoints` 是 **abstract**（`RankedV3Event` / `RankedV2Event` 各实现一套）；
//      我们这一层**给了默认实现**，转调 `RankedData.DivisionOf(points)`（那套算法已在
//      `Shell/RankedData.cs` 里按 `RankedV3Event__GetDivisionByPoints.c` 逐条复刻过）
//      ⇒ 子类不写也能工作；要做 V2 那套就覆盖它。**这是「多给了一个默认」，不是「和原版一样」**。
//   ② `GameStaticData.unrankedMultiplayer` 我们没有 ⇒ 落在 `RankedData.UnrankedMultiplayer`（现成的等价口）。
//   ③ `PlayEvent` 的两个抽象成员（`EventPlayMode` / `GetMatchmakingFilter`）本类**不实现**
//      （原版也是留给 `RankedV2/V3` 的）—— 所以本类保持 abstract。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `IRankedData`（`IRankedData.cs`，**5 个成员**）：
    /// `SeasonNumber` / `LegendaryElo` / `LeaderboardKey` / `GetBoostedFactionCount(CardArmy)` +
    /// 继承自 `IPlayEventData` 的那部分。</summary>
    public interface IRankedData
    {
        int SeasonNumber { get; }
        int LegendaryElo { get; }
        string LeaderboardKey { get; }
        int GetBoostedFactionCount(CardArmy army);
    }

    /// <summary>原版 `RankedBaseData : PlayEventData, IRankedData`（`RankedBaseData.cs`，**12 个字段 + 缺省值 + 两个方法**逐个照抄）。
    /// <para>⚠️ `rewardsPerTier` / `rewardsPerPosition` 是 `List&lt;MilestoneReward&gt;`（`{ milestone, rewards[] }`）——
    /// 本批 4 条账一行都不读它们 ⇒ 用 `(int milestone, CampaignData.RewardSpec[] rewards)` 的元组表存着，⛔ 没搬语义。</para></summary>
    [Serializable]
    public class RankedBaseData : PlayEventData, IRankedData
    {
        public int seasonNumber;
        public string leaderboard;
        public int legendaryElo = 2500;            // ★ 字段初始化器（原版就是 2500）
        public int maxEloCap = 15000;              // ★ 字段初始化器
        public int[] ratingDifferenceForMatchMaking;
        public List<(int milestone, CampaignData.RewardSpec[] rewards)> rewardsPerTier = new List<(int, CampaignData.RewardSpec[])>();
        public List<(int milestone, CampaignData.RewardSpec[] rewards)> rewardsPerPosition = new List<(int, CampaignData.RewardSpec[])>();
        public int lossShieldCount = 2;            // ★ 构造函数里写死
        public CardArmy[] boostedFactions;
        public int boostedFactionCount = 10;       // ★ 构造函数
        public int boostedFactionBonus = 10;       // ★ 构造函数
        public int defaultFactionBoostCount = 10;  // ★ 构造函数
        public int defaultFactionBonus = 20;       // ★ 构造函数

        public int LegendaryElo { get { return legendaryElo; } }
        public int MaxEloCap { get { return maxEloCap; } }
        public int[] RatingDifferenceForMatchMaking { get { return ratingDifferenceForMatchMaking; } }
        public int SeasonNumber { get { return seasonNumber; } }
        public string LeaderboardKey { get { return leaderboard; } }
        public int LossShieldCount { get { return lossShieldCount; } }

        /// <summary>原版 `bool IsFactionFeatured(CardArmy faction)`：`boostedFactions.Contains(faction)`。</summary>
        public bool IsFactionFeatured(CardArmy faction)
        {
            if (boostedFactions == null) return false;
            for (int i = 0; i < boostedFactions.Length; i++) if (boostedFactions[i] == faction) return true;
            return false;
        }

        /// <summary>原版 `int GetBoostedFactionCount(CardArmy faction)`（**逐句**）：
        /// <code>
        /// if (!boostedFactions.Contains(faction)) return defaultFactionBoostCount;
        /// return Math.Max(boostedFactionCount, defaultFactionBoostCount);
        /// </code></summary>
        public int GetBoostedFactionCount(CardArmy faction)
        {
            if (!IsFactionFeatured(faction)) return defaultFactionBoostCount;
            return Math.Max(boostedFactionCount, defaultFactionBoostCount);
        }
    }

    /// <summary>原版 `RankedEventBase&lt;TSave,TData&gt; : PlayEvent&lt;TSave,TData&gt;`
    /// （约束 `TSave : PlayEventSaveGame` / `TData : RankedBaseData`）。
    /// <para>它把「打一局」那一层（`PlayEvent`）补成「按积分算段位 + 排位开关 + 按阵营的战绩加成」。</para></summary>
    public abstract class RankedEventBase<TSave, TData> : PlayEvent<TSave, TData>, IRankedEvent
        where TSave : PlayEventSaveGame
        where TData : RankedBaseData
    {
        // ============================================================ 读口（全部从 data / 存档派生）
        /// <summary>原版 `public string LeaderboardKey`（`+0x18 → +0x90`）：`GetBaseData().leaderboard`。</summary>
        public string LeaderboardKey { get { var d = Data; return d == null ? null : d.LeaderboardKey; } }

        /// <summary>原版 `public virtual int SeasonNumber`（`+0x18 → +0x88`）：`Data.seasonNumber`。</summary>
        public virtual int SeasonNumber { get { var d = Data; return d == null ? 0 : d.SeasonNumber; } }

        /// <summary>原版 `public int CurrentScore`（`RankedEventBase.cs:54`，方法体可读）：
        /// `PlayEventExtensions.GetCurrentStats(this).value`（= 排行榜里「我这条」的分）。
        /// ⚠️ 我们没有排行榜（服务器）⇒ 落在 `RankedData.Points`（本地那一份，`RankedData.cs` 的现成口）。
        /// <para>⚠️ 这是**有意差异**：原版读的是榜单快照、我们读本地存档；两者在「单机自己打」时同值，
        /// 接到真排行榜时要把这里换成榜单读口。</para></summary>
        public int CurrentScore { get { return (int)RankedData.Points; } }

        /// <summary>原版 `public DateTime SeasonEndDate`（`+0x28 → +0x18`，`SupportMethods.MillisSinceEpochToDateTime`）：
        /// `Data.dates.endDate` 换成 `DateTime`。
        /// ⚠️ 任一为 null 原版 NRE ⇒ 我们返回 `DateTime.MinValue` 并出声。</summary>
        public DateTime SeasonEndDate
        {
            get
            {
                var d = Data == null ? null : Data.dates;
                if (d == null)
                {
                    Debug.LogError("[Ranked] SeasonEndDate：没有 dates（原版这一跳 NRE）⇒ MinValue");
                    return DateTime.MinValue;
                }
                return LiveOpsEventTimeData.FromMs(d.endDate);
            }
        }

        /// <summary>原版 `public int LegendaryElo`（方法体可读）：
        /// `if (Data != null) return Data.legendaryElo; return 2500;` —— **没有 data 时也回 2500**（照抄）。</summary>
        public int LegendaryElo
        {
            get
            {
                var d = Data;
                if (d != null) return d.LegendaryElo;
                return 2500;
            }
        }

        /// <summary>原版 `public bool IsLegendaryElo`（方法体可读，那一串位运算是编译器把比较展开）：
        /// 语义 = `CurrentScore &gt;= LegendaryElo`。
        /// <para>⚠️ 反汇编里还有一条「`+0x20`（Data）为 null ⇒ 拿 `-2500` 参与比较」的支 —— 那是 `Data == null`
        /// 时的降级路径（同 `LegendaryElo` 回 2500 的做法）；我们按 `CurrentScore &gt;= LegendaryElo` 一条写，
        /// `Data == null` 时 `LegendaryElo` 已经是 2500 ⇒ 同一结果。</para></summary>
        public bool IsLegendaryElo { get { return CurrentScore >= LegendaryElo; } }

        /// <summary>原版 `public int MaxEloCap`（`+0x18 → +0x9C`）：`Data.maxEloCap`（缺省 15000）。
        /// ⚠️ 没有 data 时原版 NRE ⇒ 我们回 <see cref="RankedBaseData"/> 的缺省 15000 并出声一次。</summary>
        public int MaxEloCap
        {
            get
            {
                var d = Data;
                if (d != null) return d.MaxEloCap;
                Debug.LogError("[Ranked] MaxEloCap：没有 data（原版这一跳 NRE）⇒ 用缺省 15000");
                return 15000;
            }
        }

        /// <summary>原版 `public virtual bool IsRanked { get; protected set; }`（`+0x28` 那一个 bool 字段）。
        /// 我们的唯一真值在 `RankedData.IsRanked`（本地存档）⇒ 读写都走它（**只有一条写路径**，见
        /// <see cref="ToggleRankedMode"/>；⛔ 别在本类再留一个同义字段）。</summary>
        public virtual bool IsRanked
        {
            get { return RankedData.IsRanked; }
            protected set { RankedData.ToggleRankedMode(value); }
        }

        /// <summary>原版 `public new IRankedData GetBaseData()`（`RankedEventBase.cs:221`）：
        /// `GetData&lt;RankedBaseData&gt;() as IRankedData`（ILSpy 只剩 isinst 的骨架）。
        /// 我们的 `Data` 已经是 `TData : RankedBaseData`（它也实现了 `IRankedData`）⇒ 直接返回。</summary>
        public IRankedData GetBaseData() { return Data; }

        /// <summary>原版 `public virtual RankedV3ArmyState GetArmyState(CardArmy army,
        /// IReadOnlyList&lt;RankedScoreBoostEvent&gt; extraBoosts = null)`（`RankedEventBase.cs:216`）：
        /// **全部内容 `return RankedV3ArmyState.None;`** —— 真语义在 `RankedV3Event` 的覆盖里（见文件头 🔴）。
        /// ⚠️ 差异：第二参类型 `RankedScoreBoostEvent` → 我们的 `RankedBoostEvent`（`Shell/RankedData.cs` 已有）。</summary>
        public virtual RankedV3ArmyState GetArmyState(CardArmy army, IReadOnlyList<RankedBoostEvent> extraBoosts = null)
        {
            return RankedV3ArmyState.None;
        }

        /// <summary>原版 `public void ToggleRankedMode(bool isRanked)`（`RankedEventBase.cs:256`，**全部内容可读**）：
        /// <code>
        /// IsRanked = isRanked;                                  // +0x28
        /// GameStaticData.unrankedMultiplayer = !isRanked;
        /// PlayerDataManager.singletonManager.SavePlayerData();
        /// </code>
        /// ✅ 这一条**已经有落点**：`RankedData.ToggleRankedMode(bool)`（`Shell/RankedData.cs`）做的正是这三句
        /// （第三句 = 本地落盘 `ranked.json`）。⇒ 本方法**只写 `IsRanked` 这一格**（它转调 `RankedData`），
        /// ⛔ 不写第二份落盘逻辑。
        /// <para>⚠️ 顺序要注意：原版先写**事件对象自己的** `IsRanked`，再写全局标志。我们两格都在
        /// `RankedData` 里（同一个存档），一次写完 —— 等价。</para></summary>
        public void ToggleRankedMode(bool isRanked)
        {
            IsRanked = isRanked;          // 唯一写路径（见 IsRanked 的注释）
        }

        /// <summary>原版 `public abstract RankedDivision GetDivisionByPoints(long points)`（`RankedEventBase.cs:264`，**抽象**）。
        /// <para>⚠️ 差异 ①：这里给了**默认实现**，转调 `RankedData.DivisionOf(points)`
        /// （`RankedV3Event__GetDivisionByPoints.c` 那一套已逐条复刻在 `Shell/RankedData.cs`）。
        /// 子类要 V2 那套就覆盖它。</para>
        /// <para>🔴 返回值用工程里已有的 `RankedDivision`（命名空间级 struct，不是 `RankedData` 的嵌套类；
        /// 原版 `RankedDivision` 是 record，我们的 struct 多一个 `Known` 标志 × 阈值表缺失 ——
        /// 那张表是 **TitleData 远程配置**，本地没有，判据见 `RankedData.WhyNoRankTable`）。</para></summary>
        public virtual RankedDivision GetDivisionByPoints(long points)
        {
            return RankedData.DivisionOf(points);
        }
    }

    // ============================================================ 原版 `IRankedEvent`（账 其他:779）
    /// <summary>原版 `IRankedEvent`（`dump.cs:130514`，`TypeDefIndex 3541`）：
    /// `IRankedEvent : IOwnDeckPlayEvent, IPlayEventWithLeaderboard, IPlayEvent, ILiveOps, IIdentifiable, IContainerProvider`。
    /// <para>本仓只搬了**能落地的那一半**：① 父接口只留 `ILiveOps`（其余 5 个本仓未建，见下 ⚠️）；
    /// ② 成员 = 原版 9 个具体成员里 `RankedEventBase&lt;,&gt;` 已实现的那 8 个 +
    /// <see cref="RankedEventBase{TSave,TData}.SeasonNumber"/>（本条账要的那一格）。
    /// ⚠️ **缺 1 个成员**：原版还有 `RankedDivision GetCurrentRankedDivision()`（接口**默认实现**，
    /// `out_cpp2il_dev_ilspy/Everguild.LiveOps/IRankedEvent.cs:144-152`）—— 它的体里两次 `Method not found`
    /// （`@1800021F0` / `@180002280`）⇒ **没有判据**，本仓不编（⛔ 不假装有）。</para>
    /// <para>⚠️ 与原版的差异（⛔ 不是「已经一样」）：原版 6 个父接口在本仓只有 `ILiveOps` 存在；
    /// 另 5 个（`IOwnDeckPlayEvent` / `IPlayEventWithLeaderboard` / `IPlayEvent` / `IIdentifiable` / `IContainerProvider`）
    /// 本仓未建 ⇒ 这里只声明 `ILiveOps`（`GetActiveEvent&lt;T&gt;` 的泛型约束要它）。
    /// 🔴 **放置位置**：原版该接口与 `IRankedData` 同址（文件前部）；我们**放文件尾**，只为**不移动已有成员的行号**
    /// （`RankedEventBase.cs` 有账落点，插在中间会让 `check_ledger_points` 报漂移 —— `CLAUDE.md` §三 那条）。</para></summary>
    public interface IRankedEvent : ILiveOps
    {
        DateTime SeasonEndDate { get; }
        int SeasonNumber { get; }
        int LegendaryElo { get; }
        bool IsLegendaryElo { get; }
        bool IsRanked { get; }
        int MaxEloCap { get; }
        new IRankedData GetBaseData();
        void ToggleRankedMode(bool value);
        RankedDivision GetDivisionByPoints(long points);
    }
}
