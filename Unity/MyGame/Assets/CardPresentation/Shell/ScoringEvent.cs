// ScoringEvent.cs —— **计分活动的数据层 + 里程碑状态机**（原版 `ScoringEvent<TS,TD>` / `ScoringMilestone` /
//                   `GenericScoringMilestone` / `ScoringEventData` / `ScoringEventPersistence` / `ScoringDataInfo` /
//                   `ScoringEventHandler` 的等价实现）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `ScoringEvent<TS,`（4 条 C）
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 账行 **691 / 692 / 693 / 694 / 695**（U04 那一组）
// 原版真身（机器可读的双份判据）：
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/ScoringEvent.cs`（**有真方法体**）
//   · 同目录 `ScoringMilestone.cs` / `ScoringEventData.cs` / `ScoringEventPersistence.cs`
//   · 同目录 `../GenericScoringMilestone.cs` / `../ScoringDataInfo.cs`
//   · `d:/2/tools/decomp_full/Everguild.LiveOps.ScoringEventHandler__CollectPoints.c`（VA 级，泛型体）
//
// 🔴 **`GetState` 是「三档」不是「四档」** —— 判据原文（`ScoringMilestone.cs:7-13`）：
//   `enum State { Locked=0, LockedBecauseNotPremium=1, Unlocked=2, Collected=3 }`。
//   `ScoringEvent.GetState` 自己只产出 **Locked / Unlocked / Collected** 三个值
//   （`LockedBecauseNotPremium` 由**扩张通行证**那类子类在别处产出，不在这一跳）。
//   ⚠️ 账的判据要点写的是「锁定/可买/可领/已领」四档 —— **那是语义分档，不是这个枚举**；
//   落地按枚举来（枚举只有四个值，其中一个不由本方法产出）。铁律 5：此处留痕。
//
// ⚠️ 与「奖励」有关的口径：`IsMilestoneCollected` 读的是**背包里有没有那件奖励**
//   （`InventoryManager.HasItem` / `GetOwnedCount`），**不是**一个「已领集合」。
//   我们还没有 `InventoryManager` 那一层（B26 的活）⇒ 走可注入的 `IInventoryProbe`，
//   默认实现 = **恒 false + 出声一次**（⛔ 不假装「已领」也不假装「没领」，见 `NoInventory` 的说明）。
using System;
using System.Collections.Generic;
using UnityEngine;

// 「一条奖励」的本地等价物（原版 `RewardInfo`）= 工程里已有的 `CampaignData.RewardSpec`。
// 🔴 这个别名**必须放在文件顶部**（命名空间成员之后不许再放 using ⇒ CS1529，2026-10-27 踩过一次）。
using RewardSpec = CardPresentation.CampaignData.RewardSpec;

namespace CardPresentation
{
    // ============================================================ 里程碑状态（原版 `ScoringMilestone.State`）
    /// <summary>原版 `ScoringMilestone.State`（嵌套枚举，**值逐个抄**）：`Locked=0 · LockedBecauseNotPremium=1 ·
    /// Unlocked=2 · Collected=3`。
    /// <para>🔴 `ScoringEvent.GetState` 只产出 `Locked` / `Unlocked` / `Collected` 三个；
    /// `LockedBecauseNotPremium` 由**付费档**（扩张通行证）在别处产出（见文件头）。</para></summary>
    public enum ScoringMilestoneState
    {
        Locked = 0,
        LockedBecauseNotPremium = 1,
        Unlocked = 2,
        Collected = 3,
    }

    /// <summary>原版 `GenericScoringMilestone.State`（**另一个类、另一套值**，别与上面那个混）：
    /// `Locked=0 · Unlocked=1 · Collected=2`。判据 = `GenericScoringMilestone.cs:5-10`。</summary>
    public enum GenericScoringMilestoneState
    {
        Locked = 0,
        Unlocked = 1,
        Collected = 2,
    }

    // ============================================================ 奖励（原版 `RewardInfo`）
    // 「一条奖励」的本地等价物 = **工程里已有的那一份**（`CampaignData.RewardSpec`，它本来就是照原版
    // `RewardInfo` 的 `{ item.targetId, quantity, rewardTier }` 建的三格）——
    // ⛔ 不新造第 N 个奖励结构（同一条数据一个类型，`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
    // 🔴 那个 `using RewardSpec = …` 别名**必须在文件顶部**（CS1529：命名空间成员之后不许再放 using）
    // ⇒ 见文件开头那三行（2026-10-27 就地为这条编译错改过一次，铁律 5 留痕）。

    // ============================================================ 进度信息（原版 `ScoringDataInfo`）
    /// <summary>原版 `ScoringDataInfo`（`ScoringDataInfo.cs`，**三个只读 int + 一个三参 ctor**，逐个照抄）：
    /// `CurrentScore` / `PreviousLevelScore` / `NextLevelScore`。
    /// <para>它是**进度条的唯一输入**：`PercentageToNextLevel` 就是
    /// `(Current−Previous) / (Next−Previous)`（见 `Shell/EventProgressBarController.cs`）。</para></summary>
    public class ScoringDataInfo
    {
        public int CurrentScore { get; private set; }
        public int PreviousLevelScore { get; private set; }
        public int NextLevelScore { get; private set; }

        public ScoringDataInfo(int currentScore, int previousLevelScore, int nextLevelScore)
        {
            CurrentScore = currentScore;
            PreviousLevelScore = previousLevelScore;
            NextLevelScore = nextLevelScore;
        }
    }

    // ============================================================ 里程碑（原版 `ScoringMilestone`）
    /// <summary>原版 `ScoringMilestone`（抽象，`ScoringMilestone.cs`，字段/属性/两个方法逐个照抄）。
    /// <para>`scoreToUnlock` / `limitRewardQuantity` 两格原版是 **private 字段 + 只读属性**；
    /// 我们保留 private 字段 + 只读属性，写口只有反序列化（`JsonUtility` 会直接写私字段）。</para>
    /// <para>⚠️ 差异：原版 `Rewards` 是 `IReadOnlyList&lt;RewardInfo&gt;` 的抽象属性，由
    /// `ExpansionMilestone`（`rewards[]` + `isPremium`）/ `RaidMilestone`（`reward` + `costToClaim` +
    /// `collectiveScoreToUnlock`）各自实现；我们这一层只要「一串奖励」，**子类由使用处提供**
    /// （本文件末尾给了 `SimpleScoringMilestone` 这个最小实现，供自检/本地里程碑表用）。</para></summary>
    [Serializable]
    public abstract class ScoringMilestone
    {
        [SerializeField] private int scoreToUnlock;
        [SerializeField] private int limitRewardQuantity;

        public int ScoreToUnlock { get { return scoreToUnlock; } }
        public int LimitRewardQuantity { get { return limitRewardQuantity; } }

        /// <summary>原版 `virtual bool IsLimited`：`limitRewardQuantity != 0`（那句位运算是编译器把
        /// `num != 0` 展开成符号判断，语义就是「非零」）。</summary>
        public virtual bool IsLimited { get { return limitRewardQuantity != 0; } }

        public abstract IReadOnlyList<RewardSpec> Rewards { get; }

        /// <summary>原版 `IsUnlocked(int playerScore)`：`playerScore &gt;= scoreToUnlock`（那句运算同样是
        /// 编译器把比较展开成符号位判断，语义 = **大于等于**）。</summary>
        public bool IsUnlocked(int playerScore) { return playerScore >= scoreToUnlock; }

        /// <summary>写口（我们这一层要能注入里程碑表；原版由 Unity 反序列化写）。
        /// ⛔ 不给公开 setter：只有构造/工厂能设（见 <see cref="SimpleScoringMilestone"/>）。</summary>
        protected void SetRaw(int scoreToUnlockValue, int limitRewardQuantityValue)
        {
            scoreToUnlock = scoreToUnlockValue;
            limitRewardQuantity = limitRewardQuantityValue;
        }
    }

    /// <summary>最小可用里程碑（原版没有这个类 —— 原版只有 `ExpansionMilestone` / `RaidMilestone` 两个具体子类）。
    /// <para>🔶 自己设计（非原版）：本地里程碑表没有 SO 可读（`ScoringMilestone` 是抽象类，13 份活动 SO 里
    /// 一个 `ScoringMilestone` 实例都没有），要在自检里合成「已领/可领/未达」三档就得有一个能 new 的实现。
    /// 它**只做数据容器**，语义（`IsUnlocked` / `IsLimited`）全部继承自基类，没有新增规则。
    /// 调度台 C2 2026-10-27 定。</para></summary>
    [Serializable]
    public class SimpleScoringMilestone : ScoringMilestone
    {
        [SerializeField] private RewardSpec[] rewards;

        public SimpleScoringMilestone() { }
        public SimpleScoringMilestone(int scoreToUnlock, int limitRewardQuantity, params RewardSpec[] rewards)
        {
            SetRaw(scoreToUnlock, limitRewardQuantity);
            this.rewards = rewards ?? Array.Empty<RewardSpec>();
        }

        public override IReadOnlyList<RewardSpec> Rewards { get { return rewards ?? Array.Empty<RewardSpec>(); } }
    }

    /// <summary>原版 `GenericScoringMilestone`（`GenericScoringMilestone.cs`，**三个公开字段**）：
    /// `state`（`State`，+0x10）/ `score`（+0x14）/ `rewards`。
    /// <para>🔴 它**不继承** `ScoringMilestone`（原版就是两个互不相干的类 ——
    /// 前者给「事件定义」用、后者给「客户端算好的里程碑进度」用）。⛔ 别合并。</para></summary>
    [Serializable]
    public class GenericScoringMilestone
    {
        public GenericScoringMilestoneState state;
        public int score;
        public RewardSpec[] rewards;
    }

    // ============================================================ 活动数据（原版 `ScoringEventData`）
    /// <summary>原版 `ScoringEventData`（抽象，`ScoringEventData.cs`，字段逐个照抄）：
    /// `scoringItem`（`LoadableReference&lt;ObtainableItem&gt;`）+ `howToScoreDescription`（`LocalizedString`）
    /// + 抽象 `ScoringMilestones`。
    /// <para>⚠️ 差异：`LoadableReference` 我们这一层没有（B32 的活）⇒ 存 `scoringItemId` 字符串；
    /// `LocalizedString` 换成裸词条 key（`Shell/Loc.cs` 的读口收 string）。</para></summary>
    [Serializable]
    public abstract class ScoringEventData : LiveOpsEventData
    {
        public string scoringItemId;
        public string howToScoreDescriptionKey;

        public abstract IReadOnlyList<ScoringMilestone> ScoringMilestones { get; }
    }

    /// <summary>原版 `ScoringEventPersistence`（**空类**，只是给泛型约束一个名字）。
    /// 判据 = `ScoringEventPersistence.cs:3`（`public class ScoringEventPersistence : LiveOpsEventPersistence {}`）。</summary>
    [Serializable]
    public class ScoringEventPersistence : LiveOpsSave { }

    // ============================================================ 背包探测口（原版 `InventoryManager`）
    /// <summary>原版 `IsMilestoneCollected` 要问背包「这件东西我有没有 / 有几个」
    /// （`InventoryManager.Instance.HasItem(CosmeticItem)` / `GetOwnedCount(RawCardScript)`）。
    /// 那一层属 B26（`Shell/Inventory.cs`），本批**不建** ⇒ 走这个可注入的口。
    /// <para>🔴 默认实现 <see cref="NoInventory"/> **不是「空背包」**，是「本机查不到」：
    /// 它每次出声一次并给 `false` / `0`。为什么不给「有」也不给「没」：给错任一侧都会让
    /// 里程碑按钮静默变成永远可领（或永远领不到）。</para></summary>
    public interface IInventoryProbe
    {
        bool HasItem(string itemId);
        int GetOwnedCount(string itemId);
    }

    /// <summary>没接背包时的兜底（见 <see cref="IInventoryProbe"/> 的说明）。
    /// 出声**只出一次**（每次调用都 Log 会刷屏，而里程碑列表一次要问几十遍）。</summary>
    public sealed class NoInventoryProbe : IInventoryProbe
    {
        bool warned;
        public bool HasItem(string itemId) { Warn(); return false; }
        public int GetOwnedCount(string itemId) { Warn(); return 0; }
        void Warn()
        {
            if (warned) return;
            warned = true;
            Debug.LogWarning("[ScoringEvent] 还没有背包层（`InventoryManager` 属 B26）⇒ "
                           + "`IsMilestoneCollected` 一律 false。这不等于「玩家没领过」，只等于「本机查不到」。"
                           + "接背包：`ScoringEvent.Inventory = <IInventoryProbe 实现>`");
        }
    }

    // ============================================================ 计分活动基类（原版 `ScoringEvent<TS,TD>`）
    /// <summary>原版 `ScoringEvent<TS,TD> : LiveOpsEvent&lt;TS,TD&gt;`（约束：`TS : ScoringEventPersistence`、
    /// `TD : ScoringEventData`）。
    /// <para>本类只做 4 件事（账 691-694）：类型化数据读口、里程碑状态、已领判定、购买/领取。
    /// `PlayerScore` 是抽象属性（原版 `public abstract int PlayerScore { get; }`）—— 具体活动各自实现
    /// （扩张通行证读 `ExpansionPassPoints`，突袭读集体分）。</para></summary>
    public abstract class ScoringEvent<TS, TD> : LiveOpsEvent<TS, TD>
        where TS : ScoringEventPersistence
        where TD : ScoringEventData
    {
        /// <summary>原版 `public abstract int PlayerScore { get; }`。</summary>
        public abstract int PlayerScore { get; }

        /// <summary>背包探测口。原版读 `InventoryManager.Instance`（单例）；我们可注入（自检要能造「已领」）。</summary>
        public static IInventoryProbe Inventory = new NoInventoryProbe();

        /// <summary>原版 `public new ScoringEventData GetBaseData()` —— 类型化读口。
        /// 判据 = `ScoringEvent.cs:12-45`（`GetData&lt;ScoringEventData&gt;()` 的 isinst 转换）。</summary>
        public new ScoringEventData GetBaseData() { return Data; }

        /// <summary>原版 `public virtual bool IsMilestoneCollected(ScoringMilestone milestone)`
        /// （`ScoringEvent.cs:49-135`，**控制流逐条照抄**）：
        /// <code>
        /// if (milestone == null) NRE;                       // 原版解引用 ⇒ 抛；我们出声 + false
        /// var reward = milestone.Rewards.First();           // 只看【第一条】奖励
        /// if (reward is CosmeticItem)  return InventoryManager.HasItem(cosmeticItem);
        /// if (reward is RawCardScript) {
        ///     int owned = InventoryManager.GetOwnedCount(card);
        ///     if (milestone.limitRewardQuantity > 0) return owned &gt;= milestone.limitRewardQuantity;
        /// }
        /// return false;
        /// </code>
        /// 🔴 **`limitRewardQuantity == 0` 时恒 `false`** —— 也就是说「不限量的里程碑」永远判「没领」，
        /// 这是原版行为（照抄，⛔ 别「修」成 true）。同理：**奖励不是这两类**（如纯货币）⇒ 也恒 false。
        /// ⚠️ 我们的 `reward.Item` 是 id 字符串（没有 `CosmeticItem` / `RawCardScript` 的类型区分）⇒
        /// **两类都走同一个 `IInventoryProbe`**（语义合并，见账的差异列）。</summary>
        public virtual bool IsMilestoneCollected(ScoringMilestone milestone)
        {
            if (milestone == null)
            {
                Debug.LogError("[ScoringEvent] IsMilestoneCollected：milestone 是 null（原版这一跳 NRE）⇒ false");
                return false;
            }
            var rewards = milestone.Rewards;
            if (rewards == null || rewards.Count == 0) return false;
            var first = rewards[0];
            if (string.IsNullOrEmpty(first.Id)) return false;
            if (milestone.LimitRewardQuantity > 0)
                return Inventory.GetOwnedCount(first.Id) >= milestone.LimitRewardQuantity;
            // 原版：不是 CosmeticItem / RawCardScript ⇒ 落到最后的 `return false`
            return false;
        }

        /// <summary>原版 `public virtual ScoringMilestone.State GetState(ScoringMilestone milestone)`
        /// （`ScoringEvent.cs:137-157`，**逐条照抄**）：
        /// <code>
        /// if (milestone == null) return Locked;
        /// if (IsMilestoneCollected(milestone)) return Collected;
        /// if (milestone.IsUnlocked(PlayerScore)) return Unlocked;
        /// return Locked;
        /// </code>
        /// ⚠️ **判「已领」在判「解锁」之前** —— 所以「分不够但背包里有」也报 `Collected`（照抄）。</summary>
        public virtual ScoringMilestoneState GetState(ScoringMilestone milestone)
        {
            if (milestone == null) return ScoringMilestoneState.Locked;
            if (IsMilestoneCollected(milestone)) return ScoringMilestoneState.Collected;
            if (milestone.IsUnlocked(PlayerScore)) return ScoringMilestoneState.Unlocked;
            return ScoringMilestoneState.Locked;
        }

        /// <summary>原版 `public abstract void PurchaseMilestone(int milestoneIndex, Action&lt;RewardInfo[]&gt; onComplete,
        /// Action&lt;string&gt; onError)`（`ScoringEvent.cs:47`，**抽象**）。
        /// 原版的实现都在子类里打云脚本（`ExpansionPassEvent` / `RaidEvent`）⇒ 本地等价由子类给
        /// （判据/口径见 `Shell/ExpansionPassEventWindow.cs` 与 `Shell/RaidEventWindow.cs` 那一侧）。
        /// <para>本类**不发明**一个默认实现（原版就是抽象）：没有服务器就没有「付费解锁」这件事，
        /// 硬编一个会变成「点了就白送」。</para></summary>
        public abstract void PurchaseMilestone(int milestoneIndex, Action<RewardSpec[]> onComplete, Action<string> onError);

        /// <summary>`EventProgressBarController` 要的那一格：**当前 + 上一档 + 下一档**
        /// （原版 `ScoringDataInfo`）。原版没有「从里程碑表算 progress info」的公共方法
        /// （每个窗体在自己那里组）⇒ 这里给一个**共用**的组法，免得三扇窗各写一份。
        /// <para>算法（照 `EventProgressBarController.PercentageToNextLevel` 的输入语义反推）：
        /// `Previous` = 已达成里**最大的**那个 `ScoreToUnlock`（没有则 0）；
        /// `Next` = 还没达成里**最小的**那个（没有则 **0** ⇒ 进度条整条关，见那条判据）。</para>
        /// <para>🔶 自己设计（非原版）：这是「组入参」的胶水，不是原版方法；算法只用了
        /// `ScoringMilestone.ScoreToUnlock` 与 `PlayerScore` 两个原版量。调度台 C2 2026-10-27 定。</para></summary>
        public ScoringDataInfo GetProgressInfo(IReadOnlyList<ScoringMilestone> milestones)
        {
            int cur = PlayerScore;
            int prev = 0, next = 0;
            bool hasPrev = false, hasNext = false;
            if (milestones != null)
            {
                for (int i = 0; i < milestones.Count; i++)
                {
                    var m = milestones[i];
                    if (m == null) continue;
                    if (m.IsUnlocked(cur)) { if (!hasPrev || m.ScoreToUnlock > prev) { prev = m.ScoreToUnlock; hasPrev = true; } }
                    else { if (!hasNext || m.ScoreToUnlock < next) { next = m.ScoreToUnlock; hasNext = true; } }
                }
            }
            if (!hasNext) next = 0;    // ⇒ 进度条 `Next == 0` 整条关（原版判据）
            return new ScoringDataInfo(cur, prev, next);
        }
    }

    // ============================================================ 计分 handler（原版 `ScoringEventHandler`）
    /// <summary>原版 `ScoringEventHandler : LiveOpsHandler&lt;IScoringEvent&gt;, IPointReceiver`
    /// （`ScoringEventHandler.cs`，74 行，**有真方法体**）。
    /// <para>原版唯一的方法 `CollectPoints(Points point, int amount)`（判据
    /// `decomp_full/Everguild.LiveOps.ScoringEventHandler__CollectPoints.c`，**控制流逐条照抄**）：
    /// <code>
    /// if (point == null) return;
    /// if (point is ExpansionPassPoints) {          // ← 只有这一支（其余点数族一律不动）
    ///     var ev = GetEvent&lt;ExpansionPassEvent&gt;(point.TargetId);
    ///     ev.&lt;score&gt; += amount;                    // 负数就是扣（asm 里是 +amount / -(-amount) 两支）
    /// }
    /// </code>
    /// 🔴 **只有 `ExpansionPassPoints` 这一支** —— 别顺手给别的点数族加分支（原版没有）。</para>
    /// <para>⚠️ 差异：`Points` 族（`Points&lt;T&gt;` / `CampaignPoints` / `ForgePoints` / `ExpansionPassPoints`）
    /// 属 B26（`Shell/Points.cs`），本批**不建** ⇒ 本方法收 `(string pointFamily, string pointTargetId, int)`，
    /// 由调用方把 `point` 拆开传。⚠️ 同样：`IScoringEvent` 接口与 `ExpansionPassEvent`（B15）都还没有 ⇒
    /// 落点走可注入的 `PointSink`（见字段注释），**默认 = 出声 + 不记**（⛔ 不静默）。</para></summary>
    public class ScoringEventHandler : LiveOpsHandler
    {
        /// <summary>原版 `public override string HandlerKey =&gt; "ScoringEvent"`（方法体可读，字面量在 `.c` 里）。</summary>
        public override string HandlerKey { get { return "ScoringEvent"; } }

        /// <summary>`(pointFamily, pointTargetId, amount) =&gt; 真的把分加进去`。
        /// 原版这一跳是 `GetEvent&lt;ExpansionPassEvent&gt;(id)` 再写分；我们没有 `ExpansionPassEvent`（B15）⇒ 注入。
        /// <para>🔴 默认 null 时**出声**（`NoSinkWarned` 保证只喊一次），⛔ 不静默吞掉积分。</para></summary>
        public static Action<string, string, int> PointSink;

        /// <summary>只认这一个点数族（原版 `point is ExpansionPassPoints` 的那一支）。
        /// 值 = 原版类名 `ExpansionPassPoints`。</summary>
        public const string ExpansionPassPointFamily = "ExpansionPassPoints";

        static bool NoSinkWarned;

        /// <summary>原版 `CollectPoints(Points point, int amount)`（`ScoringEventHandler.cs:20`）。
        /// ⚠️ 形状与原版一致：先 null 判、再族判、再转调。返回 `bool` 只为自检好断言
        /// （原版是 `void` —— 这一处不改语义：调用方可以完全忽略返回值）。</summary>
        public bool CollectPoints(string pointFamily, string pointTargetId, int amount)
        {
            if (string.IsNullOrEmpty(pointFamily)) return false;
            if (pointFamily != ExpansionPassPointFamily) return false;
            if (PointSink == null)
            {
                if (!NoSinkWarned)
                {
                    NoSinkWarned = true;
                    Debug.LogWarning("[ScoringEventHandler] CollectPoints：还没有接 `PointSink`（原版这一跳是 "
                                   + "`GetEvent<ExpansionPassEvent>(point.TargetId)` 再写分；`ExpansionPassEvent` 属 B15）"
                                   + "⇒ 本次积分（" + amount + "）**没有记上**。接法：`ScoringEventHandler.PointSink = (family, id, n) => …`");
                }
                return false;
            }
            PointSink(pointFamily, pointTargetId, amount);
            return true;
        }

        /// <summary>自检用复位（清注入 + 一次性告警位）。只影响静态状态，不碰事件表。</summary>
        public static void ResetForTest() { PointSink = null; NoSinkWarned = false; }
    }
}
