// RaidEvent.cs —— **活动线 `B14`：突袭（Raid）事件的数据 + 行为层**（2026-10-27 · 执行代理 C3）
//
// ============================ 这是什么 / 不是什么 ============================
// 原版真身（逐类对照）：
//   · `Everguild.LiveOps.RaidEvent : ScoringEvent<ScoringEventPersistence, RaidEventData>`
//     —— 事件实例：里程碑表 + 个人分/社区分 + 取档三件 + 领奖事务
//   · `RaidMilestone : ScoringMilestone`（档位定义：个人门槛 / 社区门槛 / 价格 / 奖励）
//   · `RaidRewardSelector.RaidEventMilestoneInfo`（一档打包成「两段 ScoringDataInfo + 价格 + 奖励 + 档号 + 已领」）
//   · `RaidEventRewardItemContainer.GetState`（奖励格 **5 档**派生：纯逻辑）
//   · `RaidProgressClaimButtonController`（领奖钮 **6 档** stateDefinitions + 两价格 + 点击）
//   · `RaidProgressBarScaleAndPositionController`（按档给个人进度条加位置/尺寸偏移）
//   · `RaidHeaderController.UpdateText`（页头**两档**：阶段 0 藏 `Ends in:`；宽限期 ⓘ = 事件日期 < 服务器时间）
//
// 🔴 **本文件只放「数据 + 纯算式 + 本地等价行为」**，一棵 UI 树都不建（建树在 `Shell/RaidEventWindow.cs`）。
//    理由：这些算式原版是**组件方法**，我们没有 uGUI 布局与 prefab 实例化运行时 ⇒ 拆成**纯函数**
//    才能被自检逐档跑（施工单的验收口径就是「5 档 GetState 用合成输入各造一次」）。
//
// 🔴🔴 **共用基类一律复用 `U04` 那一批，⛔ 本文件一个都不重写**（施工单 B14 的风险列原话：
//    「与 ExpansionPass 共用 U04 的进度条/领奖钮基类 ⇒ 必须 U04 先落，否则会各写一份（两处同一条规则 = 迟早不一致）」）：
//      · `ScoringDataInfo` / `ScoringMilestone` / `ScoringEventData` / `ScoringEventPersistence` /
//        `ScoringEvent<TS,TD>` / `IInventoryProbe` → `Shell/ScoringEvent.cs`
//      · `EventProgressBarController`（`PercentageToNextLevel` / `SetBarFill` / 端帽 / `ScoreObject`）
//        → `Shell/EventProgressBarController.cs`
//      · `BaseEventProgressClaimButtonController<TState>`（`StateOption` / `ApplyState` / `Initialize` /
//        `CollectionClick`）→ `Shell/BaseEventProgressClaimButtonController.cs`
//      · 奖励统一用 `CampaignData.RewardSpec`（= 原版 `RewardInfo` 的本地那一份，⛔ 不新造第 N 个）
//
// ============================ 判据（全部本地现读 · 2026-10-27）============================
// ① **机器码级**（第一权威）：`d:/2/tools/decomp_full/` 下的
//      `Everguild.LiveOps.RaidEvent__GetCurrentMilestoneIndex.c` / `__GetScoringInfoFromCurrentMilestone.c`
//      `RaidEventRewardItemContainer__GetState.c` / `__Initialize.c` / `__OnCollect.c` / `__ToggleHighlightEffect.c` / `__TestHighlighOn.c`
//      `RaidRewardSelector__CreateRewards.c` / `__CreateMilestoneInfo.c` / `__Initialize.c` / `__RefreshData.c` / `__Reset.c`
//      `RaidProgressClaimButtonController__Initialize.c` / `__CollectionClick.c` / `__ToggleCollectButtonInteractable.c`
//      `RaidProgressBarScaleAndPositionController__SetPositionAndScale.c`
//      `RaidHeaderController__UpdateText.c` / `RaidEventTab__SetEventTab.c`
//      `LiveOpsUtility__GetDateForEventStage.c`
// ② **骨架**（`RaidEvent` 的方法体在 `decomp_full` 里**没有**独立 `.c` —— 它挂在泛型基类那一路，
//    故本类取 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/RaidEvent.cs`（有真方法体）+
//    上面那几份 `.c` 交叉核。⚠️ cpp2il 丢字段名（`object obj = 0` 那一片）⇒ 凡按偏移推出来的都在行内标了「还原」）。
// ③ **字段表**：`d:/2/warpforge_code/Scripts/Assembly-CSharp/Everguild/LiveOps/{RaidMilestone,ScoringMilestone,RaidEventData,ScoringEventData}.cs`
//    ＋ `ScoringDataInfo.cs` / `CurrencyValue.cs` / `ItemInfo.cs`。偏移读数（Ghidra 实读）：
//    `ScoringDataInfo` = Current(+0x10) / Previous(+0x14) / Next(+0x18)；
//    `ScoringMilestone` = ScoreToUnlock(+0x10) / LimitRewardQuantity(+0x14)；
//    `RaidMilestone` = CostToClaim(+0x18) / DiscountedPrice(+0x20) / Reward(+0x28) / CollectiveScoreToUnlock(+0x30)；
//    `RaidEventMilestoneInfo` = PlayerScoringInfo(+0x10) / CommunityScoringInfo(+0x18) / NormalPrice(+0x20) /
//                               DiscountedPrice(+0x28) / CurrentRewards(+0x30) / Level(+0x38) / Collected(+0x3C)
//    （`RaidRewardSelector.RaidEventMilestoneInfo__.ctor.c` 逐字段实读）。
// ④ **prefab 序列化实读**（`d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/`）：
//    · `MonoBehaviour_6944932261852371964.json` = `RaidProgressClaimButtonController.stateDefinitions`
//      （6 档 × 6 个 target 的 `isEnabled`，见 <see cref="RaidClaimButtonLogic"/> 那张表）
//    · `MonoBehaviour_3159737194665565180.json`（= Raid 个人进度条的
//      `RaidProgressBarScaleAndPositionController`）与 `MonoBehaviour_342415802560839103.json`（Expansion Pass 那份）
//      **两张偏移表逐位相同**：`positionOffsetCurrent(-74.34,0)` · `scaleOffsetCurrent(-72.31,0)` ·
//      `positionOffsetPrevious(0,0)` · `scaleOffsetPrevious(-72.31,0)`
//
// ============================ 🔴 数据缺口（已查实，⛔ 不编）============================
// `RaidEvent.Milestones` / `PlayerScore` / `GlobalScore` **全部来自远端**（LiveOps 事件 SO + PlayFab 排行榜）。
// 本地 13 份活动 SO 里**一条 Raid 里程碑都没有**（全是过期的 Expansion Pass）⇒ 本类在**生产数据下恒空表**：
//   · `Milestones` = 空 ⇒ `RaidRewardSelectorLogic.CreateMilestoneInfos` 出 **0** 张（= 原版「为空就一件不建」）
//   · `PlayerScore` / `GlobalScore` = 本地排行榜缓存（`Shell/LeaderboardData.cs`）里那个榜，缺省 **0**
// ⇒ 验收只能**合成数据**（`RaidEvent.CreateForTest` / `InjectForTest`），与施工单一致。
//
// ============================ 与原版的差异（逐条如实，⛔ 不冒充）============================
// ① **排行榜**：原版 `LeaderboardManager.GetCachedLeaderboard(name,0).currentEntry.value / .total`（服务器）；
//    本地等价 = `LeaderboardData.GetCached(key)`（同一形状的本地缓存，`Shell/LeaderboardData.cs` 已建）。
//    ⚠️ 我们的缓存**没有 `total` 这一个字段**（只有 `Totals` 按阵营/队伍的表）⇒ `GlobalScore` 走
//    `GetTotalByTeam(榜名)` 那一格；取不到 = 0（并出声一次）。
// ② **领奖（`PurchaseMilestone` / `OnRewardCollected`）**：原版是 PlayFab CloudScript 事务 +
//    `InventoryManager` 库存 + `Scaffold.Core.Events.Signal` 总线。本地没有后端、没有库存层
//    （§四 P3 边界：服务器侧不复刻）⇒ 走**本地等价**（解锁判据照抄 + 进程内「已领」集合 + 我们自己的信号）
//    并**出声**。⛔ 不静默假装领到了。
// ③ **`_collectedLocal` 不落盘**：原版「已领」由服务器库存推导（`InventoryManager.HasItem/GetOwnedCount`）；
//    我们落盘要往 `LiveOpsSave` 加字段，而那是 `B09` 的文件（本批不许动）⇒ 先放进程内集合，**出声**记这笔账。
// ④ **`Dispose` / `Expire`**：原版 `Dispose` = `base + Signal.Unregister + MenuActions.ReleaseAction`（**全搬**）；
//    `Expire` 原版先「把没领的奖励领掉」（服务器）再走 base ⇒ 我们**出声跳过**领奖那一跳、base 照走。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 货币（原版 `CurrencyValue`）
    /// <summary>原版 `CurrencyValue`（`CurrencyValue.cs`：`GameCurrency Currency` + `int Amount`）。
    /// <para>⚠️ 我们工程里**没有** `GameCurrency` 枚举（只有 `Shell/MainMenuRuntime.cs` 那排 `Cur*` 常量）⇒ 类型用 `int` 存值。
    /// 值表（`dump.cs:46136` TypeDefIndex 957）：`gold 0` · `crystals 10` · `blackStones 20` · `realMoney 30` ·
    /// `free 40` · `unavailable 50` · **`raidMedals 60`** · `gachaTickets 70` · `campaignPoints 71` · `energy 72`。</para></summary>
    [Serializable]
    public sealed class RaidCurrencyValue
    {
        public int Currency;   // = `GameCurrency`
        public int Amount;
        public RaidCurrencyValue() { }
        public RaidCurrencyValue(int currency, int amount) { Currency = currency; Amount = amount; }
        public override string ToString() { return Amount + "@" + Currency; }
    }

    // ============================================================ 一档里程碑（原版 `RaidMilestone : ScoringMilestone`）
    /// <summary>原版 `RaidMilestone`（`Everguild/LiveOps/RaidMilestone.cs`）—— 字段逐个抄，偏移见文件头 ③。
    /// <para>前两格（`ScoreToUnlock` / `LimitRewardQuantity`）在基类 <see cref="ScoringMilestone"/> 里
    /// （U04 已落，`private` + `protected SetRaw`）⇒ 本类**不重声明**，只加 Raid 自己那四格。</para>
    /// <para>`Rewards` 在原版是 `override IReadOnlyList&lt;RewardInfo&gt; Rewards`（字段 `reward` 单件包成表）⇒
    /// 我们给一个只读视图（**一次性构造，不改**）。</para></summary>
    [Serializable]
    public sealed class RaidMilestone : ScoringMilestone
    {
        /// <summary>`RaidMilestone.CostToClaim`（+0x18；原序列化名 `normalPrice`）。</summary>
        public RaidCurrencyValue CostToClaim = new RaidCurrencyValue();
        /// <summary>`RaidMilestone.DiscountedPrice`（+0x20）。</summary>
        public RaidCurrencyValue DiscountedPrice = new RaidCurrencyValue();
        /// <summary>`RaidMilestone.Reward`（+0x28）—— 单件奖励（原版 `RewardInfo`，本地 = `CampaignData.RewardSpec`）。</summary>
        public CampaignData.RewardSpec Reward;
        /// <summary>`RaidMilestone.CollectiveScoreToUnlock`（+0x30）—— 社区分门槛（`[Header("Raid")]`）。</summary>
        public int CollectiveScoreToUnlock;

        public RaidMilestone() { }

        /// <summary>造一档（自检/本地里程碑表用；原版由 Unity 反序列化写）。
        /// ⚠️ 基类那两格只能经 `SetRaw` 写（U04 的口，⛔ 别绕）。</summary>
        public RaidMilestone(int scoreToUnlock, int limitRewardQuantity, int collectiveScoreToUnlock,
                             CampaignData.RewardSpec reward = default(CampaignData.RewardSpec),
                             RaidCurrencyValue costToClaim = null, RaidCurrencyValue discountedPrice = null)
        {
            SetRaw(scoreToUnlock, limitRewardQuantity);
            CollectiveScoreToUnlock = collectiveScoreToUnlock;
            Reward = reward;
            CostToClaim = costToClaim ?? new RaidCurrencyValue();
            DiscountedPrice = discountedPrice ?? new RaidCurrencyValue();
        }

        /// <summary>原版 `RaidMilestone.Rewards`（`reward` 包成表）。空 id ⇒ 空表（⛔ 不返回 null）。</summary>
        public override IReadOnlyList<CampaignData.RewardSpec> Rewards
        {
            get
            {
                if (string.IsNullOrEmpty(Reward.Id)) return NoRewards;
                return new[] { Reward };
            }
        }
        static readonly CampaignData.RewardSpec[] NoRewards = new CampaignData.RewardSpec[0];
    }

    // ============================================================ 事件数据（原版 `RaidEventData : ScoringEventData`）
    /// <summary>原版 `RaidEventData`（`Everguild/LiveOps/RaidEventData.cs`）:
    /// `leadboardName`（原文拼写就是少一个 `r`）+ `List&lt;RaidMilestone&gt; scoringMilestones`。
    /// <para>父类 `ScoringEventData`（U04 已落）已带 `scoringItemId` / `howToScoreDescriptionKey` + 抽象
    /// `ScoringMilestones` ⇒ 本类只补 Raid 那两格并实现那个抽象属性。</para></summary>
    [Serializable]
    public sealed class RaidEventData : ScoringEventData
    {
        public string leaderboardName;
        public List<RaidMilestone> scoringMilestones = new List<RaidMilestone>();

        /// <summary>原版 `ScoringEventData.ScoringMilestones`（抽象属性）：`List&lt;RaidMilestone&gt;` 的原样视图。</summary>
        public override IReadOnlyList<ScoringMilestone> ScoringMilestones
        {
            get { return scoringMilestones == null ? (IReadOnlyList<ScoringMilestone>)NoMilestones : scoringMilestones; }
        }
        static readonly RaidMilestone[] NoMilestones = new RaidMilestone[0];

        /// <summary>同一张表的**强类型**读口（Raid 自己那一路要用 `CollectiveScoreToUnlock`）。</summary>
        public IReadOnlyList<RaidMilestone> Milestones
        {
            get { return scoringMilestones == null ? (IReadOnlyList<RaidMilestone>)NoMilestones : scoringMilestones; }
        }
    }

    // ============================================================ 信号（原版 `CollectRewardsSignal` + `Signal` 总线）
    /// <summary>原版 `CollectRewardsSignal`（成员 = 这一批被领走的 `RewardInfo[]`）。
    /// <para>🔶 自己设计（非原版）：**我们自己的信号载体** —— 原版走 `Scaffold.Core.Events.Signal.Register/Unregister`
    /// 那套全局总线，我们工程没有它（`Shell/LiveOpsHandler.cs` 的 `LiveOpsSignals` 只承 `EventExpired`）·
    /// 判据拿不到（`Signal` 是框架层，反编译里只有 `Register/Unregister/Raise` 三个转发） · 调度台 2026-10-27 授权按本地等价建。</para>
    /// <para>语义照抄：`RaidEvent.Initialize` 注册、`Dispose` 反注册；**谁发的奖谁 Raise**（本地 = `PurchaseMilestone`）。</para></summary>
    public sealed class CollectRewardsSignal
    {
        public IList<CampaignData.RewardSpec> Items;
        public CollectRewardsSignal(IList<CampaignData.RewardSpec> items) { Items = items; }
    }

    /// <summary>🔶 自己设计（非原版）：`Signal` 总线的**最小等价**（只管 `CollectRewardsSignal` 这一种）。
    /// 原版 = `Scaffold.Core.Events.Signal`（框架层，本机没有等价物）；判据拿不到；调度台 2026-10-27 授权。</summary>
    public static class RaidSignals
    {
        /// <summary>原版 `Signal.Register(OnRewardCollected)` 的那条广播。</summary>
        public static event Action<CollectRewardsSignal> CollectRewards;

        /// <summary>原版 `Signal.Raise(new CollectRewardsSignal(items))`。</summary>
        public static void RaiseCollectRewards(IList<CampaignData.RewardSpec> items)
        {
            var h = CollectRewards;
            if (h == null) return;
            try { h(new CollectRewardsSignal(items)); }
            catch (Exception e) { Debug.LogError("[Raid] CollectRewardsSignal 订阅方抛异常：" + e); }
        }
        /// <summary>自检收尾（订阅方不清会跨用例串味）。</summary>
        public static void ResetForTest() { CollectRewards = null; }
    }

    // ============================================================ `RaidRewardSelector` 的纯逻辑 + 一档信息
    /// <summary>原版 `RaidRewardSelector`（MonoBehaviour）里**与 UI 无关的那一半**：
    /// `Initialize` / `RefreshData` / `Reset` / `CreateMilestoneInfo` / `CreateRewards` 的**数据侧**。
    /// <para>UI 侧（Instantiate 容器）在 `Shell/RaidEventWindow.cs` 的 `RebuildRewards()`。</para></summary>
    public static class RaidRewardSelectorLogic
    {
        /// <summary>原版 `RaidRewardSelector.RaidEventMilestoneInfo`（字段偏移见文件头 ③）。
        /// 顺序 = 原版 ctor 的七个形参顺序，⛔ 别重排。</summary>
        public sealed class RaidEventMilestoneInfo
        {
            public readonly ScoringDataInfo PlayerScoringInfo;
            public readonly ScoringDataInfo CommunityScoringInfo;
            public readonly RaidCurrencyValue NormalPrice;
            public readonly RaidCurrencyValue DiscountedPrice;
            public readonly IReadOnlyList<CampaignData.RewardSpec> CurrentRewards;
            public readonly int Level;
            public readonly bool Collected;

            public RaidEventMilestoneInfo(ScoringDataInfo playerScoringInfo, ScoringDataInfo communityScoringInfo,
                                          RaidCurrencyValue normalPrice, RaidCurrencyValue discountedPrice,
                                          IReadOnlyList<CampaignData.RewardSpec> currentRewards, int level, bool collected)
            {
                PlayerScoringInfo = playerScoringInfo; CommunityScoringInfo = communityScoringInfo;
                NormalPrice = normalPrice; DiscountedPrice = discountedPrice;
                CurrentRewards = currentRewards; Level = level; Collected = collected;
            }
            public override string ToString()
            {
                return "Lv" + Level + " 个[" + PlayerScoringInfo + "] 社[" + CommunityScoringInfo
                     + "] 价" + NormalPrice + "/" + DiscountedPrice + (Collected ? " 已领" : "");
            }
        }

        /// <summary>原版 `CreateMilestoneInfo(milestoneDefinition, previousMilestoneDefinition,
        /// playerCurrentScore, communityCurrentScore, level)`（`RaidRewardSelector__CreateMilestoneInfo.c`）—— **逐句**：
        /// <code>
        /// prevIndividual  = prev != null ? prev.scoreToUnlock(+0x10)           : 0;
        /// prevCollective  = prev != null ? prev.collectiveScoreToUnlock(+0x30) : 0;
        /// playerInfo      = new ScoringDataInfo(playerCurrentScore,    prevIndividual, milestone.scoreToUnlock);
        /// communityInfo   = new ScoringDataInfo(communityCurrentScore, prevCollective, milestone.collectiveScoreToUnlock);
        /// collected       = 奖励「已经在我手上」（服务器库存；本地由调用方给）
        /// return new RaidEventMilestoneInfo(playerInfo, communityInfo, milestone.costToClaim(+0x18),
        ///        milestone.discountedPrice(+0x20), milestone.Rewards, level, collected);
        /// </code>
        /// <para>⚠️ 第三参（`NextLevelScore`）在 `.c` 里是 `*(undefined4 *)(lVar4 + 0x10)` —— 即**本档**门槛，
        /// 不是 0（cpp2il 那份把它印成了 `Date___ctor(...,0)`，以 `.c` 为准）。</para></summary>
        public static RaidEventMilestoneInfo CreateMilestoneInfo(RaidMilestone milestoneDefinition,
            RaidMilestone previousMilestoneDefinition, int playerCurrentScore, int communityCurrentScore,
            int level, bool collected)
        {
            if (milestoneDefinition == null)
            {
                Debug.LogError("[Raid] CreateMilestoneInfo：档位定义为 null（原版这一跳 NRE）⇒ 返回 null");
                return null;
            }
            int prevIndividual = previousMilestoneDefinition == null ? 0 : previousMilestoneDefinition.ScoreToUnlock;
            int prevCollective = previousMilestoneDefinition == null ? 0 : previousMilestoneDefinition.CollectiveScoreToUnlock;

            var playerInfo = new ScoringDataInfo(playerCurrentScore, prevIndividual, milestoneDefinition.ScoreToUnlock);
            var communityInfo = new ScoringDataInfo(communityCurrentScore, prevCollective,
                                                    milestoneDefinition.CollectiveScoreToUnlock);

            return new RaidEventMilestoneInfo(playerInfo, communityInfo,
                milestoneDefinition.CostToClaim, milestoneDefinition.DiscountedPrice,
                milestoneDefinition.Rewards, level, collected);
        }

        /// <summary>原版 `CreateRewards` 的**数据侧**（`RaidRewardSelector__CreateRewards.c` 循环体逐句）：
        /// <code>
        /// if (currentRewardItemContainers.Count &gt;= 1) { LogWarning(…); 全 Destroy + 清空 }
        /// for (i = 0; i &lt; raidEvent.Milestones.Count; i++) {
        ///     milestone = Milestones[i];
        ///     container = Instantiate(rewardItemContainer, contentAnchor);
        ///     currentRewardItemContainers.Add(container);
        ///     prev = (i &gt; 0) ? Milestones[i-1] : null;
        ///     info = CreateMilestoneInfo(milestone, prev, raidEvent.PlayerScore, raidEvent.GlobalScore, i);
        ///     container.Initialize(raidEvent.Data, this, info, infos[max(i-1,0)], isLast: i == Count-1);
        /// }
        /// </code>
        /// <para>🔴 第 0 档的 `previousMilestoneInfo` **不是 null** —— 原版把 `max(i-1,0)` 那一格传进去
        /// （`uVar18 = 0; if (i-1 &gt;= 0) uVar18 = i-1;`）⇒ 第 0 档的「上一档」= 它自己（窗口那一侧负责）。</para>
        /// <para>本方法只产出**信息表**；建视图由窗口做（我们不用 `Instantiate`）。
        /// 里程碑为空 ⇒ 返回空表（= 原版「一件都不建」）。</para></summary>
        public static List<RaidEventMilestoneInfo> CreateMilestoneInfos(RaidEvent raidEvent)
        {
            var list = new List<RaidEventMilestoneInfo>();
            if (raidEvent == null) return list;
            var milestones = raidEvent.Milestones;
            if (milestones == null || milestones.Count == 0) return list;

            int player = raidEvent.PlayerScore;
            int global = raidEvent.GlobalScore;
            for (int i = 0; i < milestones.Count; i++)
            {
                var m = milestones[i];
                var prev = i > 0 ? milestones[i - 1] : null;
                list.Add(CreateMilestoneInfo(m, prev, player, global, i, raidEvent.IsCollectedByIndex(i)));
            }
            return list;
        }
    }

    // ============================================================ 分数牌（`ScoreObject.Initialize` 的**钩子体**）
    /// <summary>`EventProgressBarController.ScoreObject.Initialize` 里「算显隐 + 拼文本」那半的**钩子体**。
    /// <para>🔴 U04 的 `EventProgressBarController.cs` 把这一跳留成了 `OnInitialize` 钩子（它的文件头差异 ②：
    /// 「真正画图标由窗体交回」）⇒ 这一半按原版算式填在这里，**不是**第二套进度条实现。</para>
    /// <para>判据 = `EventProgressBarController.ScoreObject__Initialize.c:26-88`（逐句）：
    /// <code>
    /// bool last = isLastMilestone || Current &lt; Next;              // ← 原版真的改写了形参（`param_4 = 1`）
    /// show = (last &amp;&amp; Current &gt;= Previous) || (Current == 0 &amp;&amp; Previous == 0);
    /// scoreText.enabled = show;  iconHolder.gameObject.SetActive(show);
    /// scoreText.text = string.Format("{0}/{1}", Current, Next);
    /// </code></summary>
    public static class RaidScoreObjectLogic
    {
        /// <summary>分数牌该不该画（见类注释的算式）。</summary>
        public static bool Visible(ScoringDataInfo info, bool isLastMilestone)
        {
            if (info == null) return false;
            bool last = isLastMilestone || info.CurrentScore < info.NextLevelScore;
            return (last && info.CurrentScore >= info.PreviousLevelScore)
                || (info.CurrentScore == 0 && info.PreviousLevelScore == 0);
        }

        /// <summary>`string.Format("{0}/{1}", Current, Next)`。
        /// ⚠️ 格式串在反编译里只剩一个字符串对象引用（`DAT_18426de28`，`read_literal` 读不了字符串对象）
        /// ⇒ **按 prefab 出厂文本 `300/400` + 两个实参 `Current`/`Next` 还原成 `{0}/{1}`**（还原，不是实读）。
        /// 🔴 原版**只**用 `Current` 与 `Next`（`Previous` 不参与）。</summary>
        public static string Text(ScoringDataInfo info)
        {
            if (info == null) return "";
            return info.CurrentScore.ToString() + "/" + info.NextLevelScore.ToString();
        }
    }

    // ============================================================ 奖励格 5 档（原版 `RaidEventRewardItemContainer.State` / `GetState`）
    /// <summary>原版 `RaidEventRewardItemContainer.State`（`Everguild/LiveOps/RaidEventRewardItemContainer.cs`）
    /// —— 五个值顺序照抄。</summary>
    public enum RaidRewardState
    {
        Locked = 0,
        PlayerUnlocked = 1,
        CommunityUnlocked = 2,
        TotallyUnlocked = 3,
        Collected = 4,
    }

    /// <summary>原版 `RaidEventRewardItemContainer` 的**纯逻辑**那一半（`GetState` + 高亮两跳）。</summary>
    public static class RaidRewardContainerLogic
    {
        /// <summary>原版 `GetState(collected, playerScoringInfo, communityScoringInfo)`
        /// （`RaidEventRewardItemContainer__GetState.c`，**逐支**）：
        /// <code>
        /// if (collected) return Collected;                       // 4
        /// bool playerLocked    = player.Current    &lt; player.Next;      // ⚠️ 比的是 Current vs Next
        /// bool communityLocked = community.Current &lt; community.Next;
        /// if (playerLocked) return communityLocked ? 0 (Locked) : 2 (CommunityUnlocked);
        /// else              return communityLocked ? 1 (PlayerUnlocked) : 3 (TotallyUnlocked);
        /// </code>
        /// 🔴 两处「锁」都只比 `Current &lt; Next` —— `Previous` 不参与；`collected` 优先于一切。
        /// ⚠️ 两个 info 任一为 null ⇒ 原版 NRE（`FUN_1803f47a0`）⇒ 我们出声并退化成 `Locked`。</summary>
        public static RaidRewardState GetState(bool collected, ScoringDataInfo playerScoringInfo,
                                               ScoringDataInfo communityScoringInfo)
        {
            if (collected) return RaidRewardState.Collected;
            if (playerScoringInfo == null || communityScoringInfo == null)
            {
                Debug.LogError("[Raid] GetState：player/community 的 ScoringDataInfo 有 null（原版这一跳 NRE）"
                             + "⇒ 按 Locked 处理");
                return RaidRewardState.Locked;
            }
            bool playerLocked = playerScoringInfo.CurrentScore < playerScoringInfo.NextLevelScore;
            bool communityLocked = communityScoringInfo.CurrentScore < communityScoringInfo.NextLevelScore;
            if (playerLocked) return communityLocked ? RaidRewardState.Locked : RaidRewardState.CommunityUnlocked;
            return communityLocked ? RaidRewardState.PlayerUnlocked : RaidRewardState.TotallyUnlocked;
        }

        /// <summary>`GetState` 的便捷重载：吃一档信息（`Collected` + 两段 info）。</summary>
        public static RaidRewardState GetState(RaidRewardSelectorLogic.RaidEventMilestoneInfo info)
        {
            if (info == null) return RaidRewardState.Locked;
            return GetState(info.Collected, info.PlayerScoringInfo, info.CommunityScoringInfo);
        }

        /// <summary>原版 `ToggleHighlightEffect(bool option)`（`__ToggleHighlightEffect.c`）的三跳计划：
        /// <code>
        /// highlightEffect.SetActive(option);
        /// if (!option) return;
        /// SoundManager.Play2D(soundOnCollect);      // ← AudioCue，2D
        /// highlightGlow.Play();                     // ← UIBorderGlow
        /// </code>
        /// ⚠️ 顺序照抄：**先 SetActive，再放音，再起 glow**；`option == false` 时后两跳**不做**。</summary>
        public struct HighlightPlan
        {
            /// <summary>`highlightEffect.SetActive(option)`（= `highlightReward` 那个 GameObject）。</summary>
            public bool Active;
            /// <summary>`SoundManager.Play2D(soundOnCollect)` —— 只在 `option == true` 时。</summary>
            public bool PlaySound;
            /// <summary>`UIBorderGlow.Play()` —— 只在 `option == true` 时。</summary>
            public bool PlayGlow;
        }

        public static HighlightPlan ToggleHighlightEffect(bool option)
        {
            return new HighlightPlan { Active = option, PlaySound = option, PlayGlow = option };
        }
    }

    // ============================================================ 领奖钮 6 档（原版 `RaidProgressClaimButtonController` + 基类）
    /// <summary>原版 `RaidProgressClaimButtonController`（`RaidProgressClaimButtonController.cs`）的 Raid 那一半：
    /// **6 档 stateDefinitions 的预制表** + 两价格 + `Interactable` 两跳。
    /// <para>🔴 基类 `ApplyState` / `Initialize` / `CollectionClick` **不在这里重写** —— 用 U04 的
    /// <see cref="BaseEventProgressClaimButtonController{TState}"/>（⛔ 各写一份 = 迟早不一致）。</para>
    /// <para>判据：`RaidProgressClaimButtonController__{Initialize,CollectionClick,ToggleCollectButtonInteractable}.c`
    /// + `BaseEventProgressClaimButtonController.cs`（cpp2il，基类体在 `decomp_full` 里没有独立产物 ——
    /// 泛型类方法不在名单里，见 CLAUDE.md「泛型类方法体从来没被导出」）。</para></summary>
    public static class RaidClaimButtonLogic
    {
        /// <summary>原版 `RaidEventRewardItemContainer.State` 那 6 个 target 的**节点名**（= 原版 target 名）。
        /// <para>🔴 六个 PPtr 的解（prefab `MonoBehaviour_6944932261852371964.json`）：
        /// A = `-5148159871162950660` → **`Level`** · B = `-3019609991380575236` → **`Lock`** ·
        /// C = `-5645340259280724996` → **`Collect Button`** · D = ` 4698971596324035580` → **`Button Price Display`** ·
        /// E = `-8048558007553336324` → **`Old Price Display`** · F = ` 1410151795689020412` → **`Claimed Text`**。
        /// ⚠️ 这张对照是**按语义反解**的（`Claimed Text` 只在 Collected 开 · `Old Price Display` 只在 3/4 开 ·
        /// `Collect Button` 只在 2/3/4 开 · `Level` 只在 0/1 开），与 2026-10-19 那份文件头记录**有一处不同**：
        /// 那边写「state 3 → ON Lock / Collect Button / Button Price Display」，**漏了 `Old Price Display`**
        /// （JSON 里 state 3 的 E 是 `isEnabled=1`）⇒ 已按 JSON 更正（铁律 5）。</para></summary>
        public static readonly string[] TargetNames =
            { "Level", "Lock", "Collect Button", "Button Price Display", "Old Price Display", "Claimed Text" };

        // 逐档 isEnabled（顺序 = TargetNames；prefab JSON 逐格实读）
        static readonly bool[] On0 = { true, true, false, true, false, false };  // Locked
        static readonly bool[] On1 = { true, true, false, false, false, false }; // PlayerUnlocked
        static readonly bool[] On2 = { false, true, true, true, false, false };  // CommunityUnlocked
        static readonly bool[] On3 = { false, true, true, true, true, false };   // TotallyUnlocked（⚠️ Old Price 也开）
        static readonly bool[] On4 = { false, true, true, false, true, true };   // Collected

        /// <summary>`enableLevelHighlight` 逐档实读：只有 state 0 是 0。</summary>
        public static bool LevelHighlightFor(RaidRewardState s) { return s != RaidRewardState.Locked; }
        /// <summary>`forceInteractableState` 逐档实读：只有 state 4 是 1。</summary>
        public static bool ForceInteractableFor(RaidRewardState s) { return s == RaidRewardState.Collected; }

        /// <summary>按 <paramref name="nodeOf"/>（节点名 → GameObject）建**六档 stateDefinitions**
        /// （原版是 prefab 序列化的 `stateDefinitions` 数组；本地由这段代码**逐格还原**）。
        /// <para>⚠️ `levelObject` / `priceDisplayButton` 传 `null`：我们没有 `ProgressRewardLevelObject` /
        /// `PriceDisplayButton` 两个 MonoBehaviour（基类那一跳会**出声并跳过**，见其 `ApplyState`）——
        /// 高亮那一下由窗口用 <see cref="LevelHighlightFor"/> 自己落。</para></summary>
        public static BaseEventProgressClaimButtonController<RaidRewardState>.GameObjectElementsStateDefinition[]
            BuildDefinitions(Func<string, GameObject> nodeOf)
        {
            var states = new[]
            {
                RaidRewardState.Locked, RaidRewardState.PlayerUnlocked, RaidRewardState.CommunityUnlocked,
                RaidRewardState.TotallyUnlocked, RaidRewardState.Collected,
            };
            var flags = new[] { On0, On1, On2, On3, On4 };
            var defs = new BaseEventProgressClaimButtonController<RaidRewardState>.GameObjectElementsStateDefinition[states.Length];
            for (int d = 0; d < states.Length; d++)
            {
                var objs = new BaseEventProgressClaimButtonController<RaidRewardState>.StateOption[TargetNames.Length];
                for (int i = 0; i < TargetNames.Length; i++)
                    objs[i] = new BaseEventProgressClaimButtonController<RaidRewardState>.StateOption
                    {
                        target = nodeOf != null ? nodeOf(TargetNames[i]) : null,
                        isEnabled = flags[d][i],
                    };
                defs[d] = new BaseEventProgressClaimButtonController<RaidRewardState>.GameObjectElementsStateDefinition
                {
                    state = states[d],
                    levelObject = null,
                    enableLevelHighlight = LevelHighlightFor(states[d]),
                    priceDisplayButton = null,
                    forceInteractableState = ForceInteractableFor(states[d]),
                    objects = objs,
                };
            }
            return defs;
        }

        /// <summary>`RaidProgressClaimButtonController.Initialize` 的价格那两跳（`__Initialize.c`）：
        /// <code>
        /// oldPriceDisplay.Setup(normalPrice);                                  // 划线原价恒 normalPrice
        /// collectButton.Setup(state != TotallyUnlocked ? normalPrice : discountedPrice, true);
        /// if (collectButton.Interactable) collectButton.Interactable = (state != Collected &amp;&amp; state != Locked);
        /// </code>
        /// 🔴 **只有 TotallyUnlocked(3) 显示折后价**（不是 2/3/4）；`Interactable` 那跳**只在原本可点时**才写。</summary>
        public static RaidCurrencyValue PriceToShow(RaidRewardState state, RaidCurrencyValue normal, RaidCurrencyValue discounted)
        {
            return state == RaidRewardState.TotallyUnlocked ? discounted : normal;
        }

        /// <summary>`Interactable` 的目标值：`state != Collected &amp;&amp; state != Locked`。</summary>
        public static bool InteractableFor(RaidRewardState state)
        {
            return state != RaidRewardState.Collected && state != RaidRewardState.Locked;
        }

        /// <summary>原版 `ToggleCollectButtonInteractable(bool)`（`__ToggleCollectButtonInteractable.c`）：
        /// `collectButton.Interactable = option`（`PriceDisplayButton.set_Interactable`）。</summary>
        public static bool ToggleCollectButtonInteractable(bool option) { return option; }
    }

    // ============================================================ 个人进度条的按档位移（原版 `RaidProgressBarScaleAndPositionController`）
    /// <summary>`RaidProgressBarScaleAndPositionController` + 基类 `BaseProgressBarScaleAndPositionController&lt;TState&gt;`。
    /// <para>判据（两份交叉）：`RaidProgressBarScaleAndPositionController__SetPositionAndScale.c`（Ghidra）+
    /// `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BaseProgressBarScaleAndPositionController.cs` +
    /// prefab MB `MonoBehaviour_3159737194665565180.json`（偏移值实读）。</para>
    /// <para>⚠️ 基类 `Initialize` 的执行顺序在 Ghidra 里**没有独立产物**（泛型基类）；现按
    /// `Shell/ExpansionPassEventWindow.cs` 已收口的那一份（`PersonalBarRect`，同一判据）**共用同一条结论**：
    /// Ⅰ 缓存出厂矩形 → Ⅱ **先按上一档**摆 → Ⅲ `level &gt; 0` 时再按当前档叠加。⛔ 别在这里另起一套顺序。</para></summary>
    public static class RaidBarPlacementLogic
    {
        /// <summary>原版 `SetPositionAndScale` 的唯一闸（`__SetPositionAndScale.c:12-14`）：
        /// `if (stateToApply &lt; 2) return;`（形参按 **uint** 比）⇒ **只有 `CommunityUnlocked(2)` / `TotallyUnlocked(3)` /
        /// `Collected(4)`** 才改位置与尺寸；`Locked(0)` / `PlayerUnlocked(1)` 一个字段都不动。</summary>
        public static bool Moves(RaidRewardState state) { return (int)state >= 2; }

        /// <summary>两张偏移表（prefab MB 实读；Raid 与 Expansion Pass 两份**逐位相同**）。</summary>
        public static readonly Vector2 PositionOffsetCurrent = new Vector2(-74.34f, 0f);
        public static readonly Vector2 ScaleOffsetCurrent = new Vector2(-72.31f, 0f);
        public static readonly Vector2 PositionOffsetPrevious = new Vector2(0f, 0f);
        public static readonly Vector2 ScaleOffsetPrevious = new Vector2(-72.31f, 0f);

        /// <summary>原版 `Initialize(currentState, previousState, level)` 的**可判部分**：
        /// 该按哪两档去摆（Ⅱ 永远跑；Ⅲ 只在 `level &gt; 0`）。</summary>
        public static void PlanSteps(int level, out bool applyPrevious, out bool applyCurrent)
        {
            applyPrevious = true;
            applyCurrent = level > 0;
        }
    }

    // ============================================================ 页头两档（原版 `RaidHeaderController.UpdateText`）
    /// <summary>原版 `RaidHeaderController.UpdateText()`（`RaidHeaderController__UpdateText.c`，逐支）。
    /// <para>🔴 **整段被「事件不为空」包着**（`if (*(longlong *)(param_1 + 0x50) != 0)`）——
    /// **没有事件 ⇒ 一个字段都不写**（保持 prefab 出厂态）。这一条决定我们「没有活动数据时」的画法。</para></summary>
    public static class RaidHeaderLogic
    {
        /// <summary>`LiveOpsUtility.GetDateForEventStage(事件, 阶段)`（`LiveOpsUtility__GetDateForEventStage.c`）：
        /// <code>
        /// var stages = data.dates.eventStages;              // +0x40
        /// long d = ElementAtOrDefault(stages, stage);       // 越界 = 0
        /// return d != 0 ? d : data.dates.endDate;           // ★ 0 视作「没有这一档」⇒ 落到结束时刻
        /// </code></summary>
        public static long GetDateForEventStage(LiveOpsEventData data, int stage)
        {
            var dates = data == null ? null : data.dates;
            if (dates == null) return 0L;
            var stages = dates.eventStages;
            long v = (stages != null && stage >= 0 && stage < stages.Count) ? stages[stage] : 0L;
            return v != 0L ? v : dates.endDate;
        }

        /// <summary>`UpdateText` 的三种结果：**要不要写**（没有事件就不写）、时间标题显不显、宽限期 ⓘ 显不显、
        /// 以及「倒计时到哪一刻」。</summary>
        public struct Plan
        {
            public bool HasEvent;         // 原版第一句那道闸
            public bool TimeTitleVisible; // 阶段 == 0 ⇒ false
            public bool GraceVisible;     // 事件日期 < 服务器时间
            public bool CountingToEnd;    // bVar7：日期已过 ⇒ 倒计时打到 endDate，否则打到该阶段日期
            public long CountdownToMs;
            public string TimeTitleTerm;  // EndsIn / StartsIn
        }

        /// <summary>算这一拍页头该长什么样。
        /// <code>
        /// if (event == null) return;                                  // ← 什么都不写
        /// if (stage == 0) { timeTitle.SetActive(false); gracePeriodTooltip.SetActive(false); return; }
        /// long date = GetDateForEventStage(event, stage);
        /// bool past = date &lt; serverNow;
        /// timeTitle.text = past ? EndsIn : StartsIn;
        /// timeRemaining.text = GetTimeStringFromDate(past ? event.GetEndDate(false /*FinalDate*/) : date);
        /// gracePeriodTooltip.SetActive(past);
        /// </code>
        /// <para>⚠️ **「阶段」这一格没有查清**：`__UpdateText.c` 读的是 `(事件 as ???)+0x24`（cpp2il 把类型身份丢了），
        /// 本机不能确证 `+0x24` 是哪一格，也没有任何本地数据写它 ⇒ 做成**显式入参** <c>eventStage</c>
        /// （窗口默认 0，= 原版「阶段 0 ⇒ 藏 timeTitle」那一支）。⛔ 不猜一个来源。</para></summary>
        public static Plan UpdateText(LiveOpsEventData data, int eventStage, long serverNowMs,
                                     string termEndsIn, string termStartsIn)
        {
            var p = new Plan { HasEvent = data != null, TimeTitleTerm = termEndsIn };
            if (data == null) return p;                    // 原版：没有事件 ⇒ 一个字段都不写

            if (eventStage == 0)
            {
                p.TimeTitleVisible = false; p.GraceVisible = false;
                return p;
            }
            long date = GetDateForEventStage(data, eventStage);
            bool past = date < serverNowMs;
            p.TimeTitleVisible = true;
            p.TimeTitleTerm = past ? termEndsIn : termStartsIn;
            p.GraceVisible = past;
            p.CountingToEnd = past;
            // 原版 `GetEndDate(false)` = `FinalDate`（`expirable ? expireDate : endDate`）
            p.CountdownToMs = past ? (data.dates != null ? data.dates.FinalDate : 0L) : date;
            return p;
        }

        /// <summary>倒计时文本：原版 `SupportMethods.GetTimeStringFromDate(date, 1, 1, 0)` —— 同一个格式器
        /// （我们的 <see cref="TimerDisplay.FormatTimeString"/> = `GetTimeString` 逐句移植）+ 三开关
        /// `(eventTimer: true, clampTo0: true, hide0Values: false)`。
        /// ⚠️ 三个开关的**位次**按 `GetTimeString(ts, eventTimer, clampTo0, hide0Values)` 还原（实参是 `1,1,0`）。</summary>
        public static string CountdownText(long targetMs, long serverNowMs)
        {
            var ts = LiveOpsEventTimeData.FromMs(targetMs) - LiveOpsEventTimeData.FromMs(serverNowMs);
            return TimerDisplay.FormatTimeString(ts, eventTimer: true, clampTo0: true, hide0Values: false);
        }
    }

    // ============================================================ 事件实例（原版 `RaidEvent`）
    /// <summary>原版 `Everguild.LiveOps.RaidEvent : ScoringEvent&lt;ScoringEventPersistence, RaidEventData&gt;`。
    /// <para>基类链：`ScoringEvent`（U04，`Shell/ScoringEvent.cs`）→ `LiveOpsEvent&lt;TS,TD&gt;` → `LiveOpsEvent`。
    /// ⇒ 本类只需实现 `PlayerScore`（抽象属性）与 `PurchaseMilestone`（抽象方法）＋自己那 13 个方法。</para>
    /// <para>⚠️ `ScoringEvent` 那层的 `IsMilestoneCollected` / `GetState(ScoringMilestone)` 属**别的批**
    /// （U04，不在 B14 的 33 行里）—— 本类只**覆盖** `IsMilestoneCollected` 那一半（本地无库存层，见文件头差异 ③）。</para></summary>
    public sealed class RaidEvent : ScoringEvent<ScoringEventPersistence, RaidEventData>
    {
        /// <summary>原版 `RaidEvent.refreshAction`（`private MenuActions.Action refreshAction`）——
        /// `Initialize` 排、`Dispose` 撤（`MenuActions.ReleaseAction`）。</summary>
        MenuAction _refreshAction;

        /// <summary>本地「已领」集合（⚠️ 见文件头差异 ③：原版由服务器库存推导；我们不落盘、出声）。</summary>
        readonly HashSet<int> _collectedLocal = new HashSet<int>();

        bool _warnedNoTotal;

        // ------------------------------------------------------------ 里程碑 / 分数
        /// <summary>原版 `Milestones =&gt; GetBaseData().ScoringMilestones`（`Enumerable.Cast&lt;&gt;().ToArray()`）——
        /// `Data` 为 null ⇒ 原版 NRE，我们出声返回空表。</summary>
        public IReadOnlyList<RaidMilestone> Milestones
        {
            get
            {
                var d = Data;
                if (d == null)
                {
                    Debug.LogError("[Raid] Milestones：没有 Data（原版这一跳 NRE）⇒ 返回空表");
                    return NoMilestones;
                }
                return d.Milestones;
            }
        }
        static readonly RaidMilestone[] NoMilestones = new RaidMilestone[0];

        /// <summary>排行榜名（原版 `RaidEventData.leadboardName`，拼写少一个 r —— 照抄）。</summary>
        public string LeaderboardName { get { return Data == null ? null : Data.leaderboardName; } }

        /// <summary>原版 `PlayerScore`（`RaidEvent.cs:230`，`override`）：
        /// `LeaderboardManager.Instance.GetCachedLeaderboard(榜名,0)?.currentEntry?.value ?? 0`。
        /// <para>本地等价 = `LeaderboardData.GetCachedPlayerScore(榜名)`（同一形状的本地缓存）。</para></summary>
        public override int PlayerScore
        {
            get
            {
                var key = LeaderboardName;
                long v = LeaderboardData.GetCachedPlayerScore(key);
                return (int)v;
            }
        }

        /// <summary>原版 `GlobalScore`（`RaidEvent.cs:262`）：`GetCachedLeaderboard(榜名,0)?.total ?? 0`。
        /// <para>⚠️ 我们的 <see cref="LocalLeaderboardResult"/> **没有 `total` 一格**（只有 `Totals` 按阵营/队伍的表）
        /// ⇒ 走 `GetTotalByTeam(榜名)`；取不到 = 0（**出声一次**，理由记在文件头差异 ①）。</para></summary>
        public int GlobalScore
        {
            get
            {
                var key = LeaderboardName;
                if (string.IsNullOrEmpty(key)) return 0;
                var board = LeaderboardData.GetCached(key);
                if (board == null) return 0;
                long total = board.GetTotalByTeam(key, 0L);
                if (total == 0L && !_warnedNoTotal)
                {
                    _warnedNoTotal = true;
                    Debug.Log("[Raid] `GlobalScore`（社区分）：本地排行榜缓存里没有这一榜的总分格"
                            + "（原版 `LeaderboardResult.total` 是服务端下发的预聚合值；我们只有 `Totals` 按阵营/队伍的表）"
                            + "⇒ 按 0 计。有真实数据时由 `LeaderboardData.SetCachedForTest`/`UpdateEntry` 灌。");
                }
                return (int)total;
            }
        }

        // ------------------------------------------------------------ 取档三件（纯算式，判据见方法头）
        /// <summary>原版 `GetCurrentMilestoneIndex()`（`Everguild.LiveOps.RaidEvent__GetCurrentMilestoneIndex.c`，**逐句**）：
        /// <code>
        /// var m = Milestones.LastOrDefault(x =&gt; PlayerScore &gt;= x.ScoreToUnlock);
        /// if (m == null) return 0;
        /// int idx = Milestones.GetIndexOf(m) + 1;
        /// int last = Milestones.Count - 1;
        /// return idx &lt; 0 ? 0 : (idx &gt; last ? last : idx);
        /// </code>
        /// 🔴 返回的是**下一个下标**（`idx+1`），并且**夹到 `[0, Count-1]`** —— 所以「最高档已达成」⇒ 停在最后一档。
        /// </summary>
        public int GetCurrentMilestoneIndex()
        {
            var list = Milestones;
            int found = -1;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && PlayerScore >= list[i].ScoreToUnlock) found = i;   // LastOrDefault 语义
            if (found < 0) return 0;
            int idx = found + 1;
            int last = list.Count - 1;
            if (idx < 0) return 0;
            return idx > last ? last : idx;
        }

        /// <summary>原版 `GetCurrentMilestone()`（`RaidEvent.cs:647`）：`ElementAtOrDefault(Milestones, GetCurrentMilestoneIndex())`。
        /// 空表 ⇒ null（原版 `FUN_180007240` 就是 `ElementAtOrDefault`，越界给 null）。</summary>
        public RaidMilestone GetCurrentMilestone()
        {
            var list = Milestones;
            int idx = GetCurrentMilestoneIndex();
            return (idx >= 0 && idx < list.Count) ? list[idx] : null;
        }

        /// <summary>原版 `GetScoringInfoFromCurrentMilestone()`
        /// （`Everguild.LiveOps.RaidEvent__GetScoringInfoFromCurrentMilestone.c`，**逐句**）：
        /// <code>
        /// int idx   = GetCurrentMilestoneIndex();
        /// var cur   = ElementAtOrDefault(Milestones, idx);
        /// if (cur == null) { LogError("[ERROR] Can't find the current milestone for the Expansion Pass Event.");   // ← 原版文案就是 Expansion Pass（复制粘贴）
        ///                    return new ScoringDataInfo(0,0,0); }
        /// var prev  = ElementAtOrDefault(Milestones, idx - 1);
        /// return new ScoringDataInfo(PlayerScore, prev != null ? prev.scoreToUnlock : 0, cur.scoreToUnlock);
        /// </code>
        /// 🔴 第三参是**本档门槛**（`*(int*)(cur+0x10)`），cpp2il 那份印成 0 —— 以 `.c` 为准。</summary>
        public ScoringDataInfo GetScoringInfoFromCurrentMilestone()
        {
            var list = Milestones;
            int idx = GetCurrentMilestoneIndex();
            var cur = (idx >= 0 && idx < list.Count) ? list[idx] : null;
            if (cur == null)
            {
                Debug.LogError("[ERROR] Can't find the current milestone for the Expansion Pass Event.");
                return new ScoringDataInfo(0, 0, 0);
            }
            var prev = (idx - 1 >= 0 && idx - 1 < list.Count) ? list[idx - 1] : null;
            return new ScoringDataInfo(PlayerScore,
                                       prev != null ? prev.ScoreToUnlock : 0,
                                       cur.ScoreToUnlock);
        }

        // ------------------------------------------------------------ 已领（本地等价，见文件头差异 ③）
        /// <summary>某一档是否已领 —— 原版由服务器库存推导（`ScoringEvent.IsMilestoneCollected`）；
        /// 本地 = **先看本进程记下的**（领过就在里），否则回落基类（= `IInventoryProbe`，本机恒 false + 出声）。</summary>
        public bool IsCollectedByIndex(int milestoneIndex)
        {
            if (_collectedLocal.Contains(milestoneIndex)) return true;
            var list = Milestones;
            return (milestoneIndex >= 0 && milestoneIndex < list.Count) && IsMilestoneCollected(list[milestoneIndex]);
        }
        public void MarkMilestoneCollectedForTest(int milestoneIndex) { _collectedLocal.Add(milestoneIndex); }
        public void ClearCollectedForTest() { _collectedLocal.Clear(); }

        // ------------------------------------------------------------ 生命周期（原版 `RaidEvent.Initialize` / `Dispose` / `Expire`）
        /// <summary>原版 `RaidEvent.Initialize()`（`RaidEvent.cs:377`，**逐句**）：
        /// <code>
        /// base.Initialize();
        /// Signal.Register(OnRewardCollected);
        /// MenuActions.Instance.BlockGroups(new[]{ "ScoringEvent" });
        /// refreshAction = MenuActions.Instance.QueueAction(() =&gt; {
        ///     CustomDebug.Log("Refreshing Scoring Event Values.");
        ///     LeaderboardManager.Instance.GetLeaderboard(leaderboardName, …, forceRefresh:false);
        /// }, ActionRule.BlockActionsFromSameGroup, …, menuFilter: new[]{"ScoringEvent"});
        /// </code>
        /// <para>⚠️ 第二跳的 `MenuFilter = {"ScoringEvent"}`（原版那段 `Type[] array2` 实际装的是**字符串**，
        /// cpp2il 把 `typeof` 那一段丢成了字符串数组）；本地的 `MenuAction.MenuFilter` 是 `Type[]` ⇒
        /// **不传 filter**（我们的 `MenuActions` 没有「按组名过滤窗类型」那一格），如实记差异。</para></summary>
        public override void Initialize()
        {
            base.Initialize();
            RaidSignals.CollectRewards += OnRewardCollected;
            MenuActions.BlockGroups(new[] { ScoringGroup });
            _refreshAction = MenuActions.QueueAction(RefreshScoreValues, ActionRule.BlockActionsFromSameGroup);
        }

        /// <summary>原版 `RaidEvent.Dispose()`（`RaidEvent.cs:421`，**逐句**）：
        /// `base.Dispose(); Signal.Unregister(OnRewardCollected); MenuActions.ReleaseAction(refreshAction);`</summary>
        public override void Dispose()
        {
            base.Dispose();
            RaidSignals.CollectRewards -= OnRewardCollected;
            MenuActions.ReleaseAction(_refreshAction);
            _refreshAction = null;
        }

        /// <summary>原版 `RaidEvent.Expire(Action onComplete)`（`RaidEvent.cs:429`）：
        /// 先让服务器「把没领的奖励领掉」（`CollectScoringRewards` → 回调里 `RewardService.Collect`），
        /// 回调里再 `base.Expire(onComplete)`。
        /// <para>⚠️ 领奖那一跳是服务端事务（§四 P3）⇒ 本地**出声跳过**、直接走 base（base = 遥测 skip + 停用 + 回调）。</para></summary>
        public override void Expire(Action onComplete)
        {
            Debug.LogWarning("[Raid] `Expire`：原版先让服务器把未领奖励领掉（`CollectScoringRewards` + `RewardService.Collect`）"
                           + "再走 base；本地没有后端 ⇒ **跳过领奖那一跳**，base 照走（停用 + 回调）。");
            base.Expire(onComplete);
        }

        /// <summary>`Initialize` 里那条排期动作（原版匿名 lambda）：
        /// `CustomDebug.Log("Refreshing Scoring Event Values."); LeaderboardManager.GetLeaderboard(榜名, forceRefresh:false)`。
        /// <para>本地等价：出声 + 把榜单缓存当作「已刷新」（没有远端可拉；⛔ 不伪造数据）。</para></summary>
        void RefreshScoreValues()
        {
            Debug.Log("[Raid] Refreshing Scoring Event Values.");
            var key = LeaderboardName;
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogWarning("[Raid] `GetLeaderboard` 跳过：这份 `RaidEventData` 没有 `leaderboardName`");
                return;
            }
            // 原版这一跳会向服务器要一页榜；本地没有远端 ⇒ 只读缓存（不新建、不改值）。
            var board = LeaderboardData.GetCached(key);
            if (board == null)
                Debug.Log("[Raid] 排行榜 `" + key + "` 本地缓存为空（原版这一跳会从服务器拉一页）⇒ 分数按 0 计。");
        }

        /// <summary>`Initialize` 里 `BlockGroups` 的那一组名（原版字面量 `"ScoringEvent"`）。</summary>
        public const string ScoringGroup = "ScoringEvent";

        // ------------------------------------------------------------ 领奖（原版 `PurchaseMilestone` / `OnRewardCollected`）
        /// <summary>原版 `PurchaseMilestone(milestoneIndex, onComplete, onError)`（`RaidEvent.cs:692`）：
        /// <code>
        /// var m = ElementAtOrDefault(Milestones, milestoneIndex);
        /// bool rewardUnlocked = PlayerScore &gt;= 0 &amp;&amp; GlobalScore &gt;= m.collectiveScoreToUnlock;
        /// PlayfabWrapper.CollectScoringReward(GetID(), milestoneIndex, rewardUnlocked, result =&gt; {
        ///     if (result != null) { InventoryManager.…; RewardService.Collect(result.rewards); …; onComplete?.Invoke(result.rewards); }
        ///     else onError?.Invoke(…);
        /// });
        /// </code>
        /// <para>🔴 **解锁判据照抄**：`PlayerScore &gt;= 0` 那道闸 + `GlobalScore &gt;= 社区门槛`。
        /// ⚠️ 本地等价（P3 边界的 b 档：UI 照原版、数据本地模拟 + 出声）：
        /// 解锁 ⇒ 标「已领」+ 广播 `CollectRewardsSignal`（= 原版 `RewardService.Collect` 之后那条信号）+ `onComplete(rewards)`；
        /// 未解锁 ⇒ `onError(原因)`（原版服务端拒绝也走这条）。</para></summary>
        public override void PurchaseMilestone(int milestoneIndex, Action<CampaignData.RewardSpec[]> onComplete,
                                               Action<string> onError)
        {
            var list = Milestones;
            var m = (milestoneIndex >= 0 && milestoneIndex < list.Count) ? list[milestoneIndex] : null;
            if (m == null)
            {
                var why = "里程碑下标越界：" + milestoneIndex + "（共 " + list.Count + " 档）";
                Debug.LogError("[Raid] PurchaseMilestone：" + why + "（原版 `ElementAtOrDefault` 得 null ⇒ 后面 NRE）");
                if (onError != null) onError(why);
                return;
            }
            int player = PlayerScore, global = GlobalScore;
            bool rewardUnlocked = player >= 0 && global >= m.CollectiveScoreToUnlock;
            Debug.Log("[Raid] `PurchaseMilestone`：原版是 PlayFab CloudScript 事务（服务器）⇒ 本地等价模拟。"
                    + "档 " + milestoneIndex + " · 个人分 " + player + " · 社区分 " + global
                    + "/" + m.CollectiveScoreToUnlock + " ⇒ " + (rewardUnlocked ? "已解锁" : "未解锁"));

            if (!rewardUnlocked)
            {
                if (onError != null) onError("社区分未达本档门槛（" + global + "/" + m.CollectiveScoreToUnlock + "）");
                return;
            }
            _collectedLocal.Add(milestoneIndex);
            var rewards = new List<CampaignData.RewardSpec>(m.Rewards);
            // 原版 `RewardService.Collect(rewards)` → 发奖 → 全局信号（我们这一层只发信号；发奖由消费方做）
            RaidSignals.RaiseCollectRewards(rewards);
            if (onComplete != null) onComplete(rewards.ToArray());
        }

        /// <summary>原版 `OnRewardCollected(CollectRewardsSignal signal)`（`RaidEvent.cs:453`）：
        /// <code>
        /// foreach (var item in signal.Items.Where(i =&gt; i.ItemId == "C4")) {       // C4 = raidMedals（`GameCurrency.raidMedals = 60`）
        ///     CustomDebug.Log("Adding points");
        ///     LeaderboardManager.Instance.LocalScore(leaderboardName, item.quantity, null);
        /// }
        /// </code>
        /// <para>⚠️ 增量取 `Quantity` —— cpp2il 在这一格丢成 `int value = default`（字段名没了），
        /// 按 `ItemInfo&lt;T&gt;` 里**唯一**的整型成员 `quantity` 还原（标「还原」，见文件头 ②）。</para>
        /// <para>⚠️ `LocalScore(key, value, mode:null)` ⇒ 走 <see cref="LeaderboardData.LocalScore(string,int,LeaderboardScoringMode?)"/>
        /// 的缺省模式（`SetValue`）—— 原版这里传的也是 null（`(LeaderboardScoringMode?)(object)0`）。</para></summary>
        void OnRewardCollected(CollectRewardsSignal signal)
        {
            if (signal == null || signal.Items == null) return;
            var key = LeaderboardName;
            for (int i = 0; i < signal.Items.Count; i++)
            {
                var item = signal.Items[i];
                if (item.Id != RaidMedalItemId) continue;
                Debug.Log("[Raid] Adding points");
                if (string.IsNullOrEmpty(key))
                {
                    Debug.LogWarning("[Raid] 领到突袭奖章但这份事件没有 `leaderboardName` ⇒ 这一笔分没记（原版会 NRE）");
                    continue;
                }
                LeaderboardData.LocalScore(key, item.Quantity, null);
            }
        }

        /// <summary>突袭奖章的 item id（原版 `RewardInfo.ItemId == "C4"` 那个字面量；`PurchaseMilestone` 里
        /// `CurrencyExtensions.ToCurrency(GameCurrency.raidMedals)` 同一路）。</summary>
        public const string RaidMedalItemId = "C4";

        // ------------------------------------------------------------ 自检注入（本地无数据，见文件头「数据缺口」）
        /// <summary>给自检用：塞一份 `RaidEventData`（含里程碑）。⛔ 生产路径**不调它**——
        /// 真数据只能来自 LiveOps（`LoadEventData`）。</summary>
        public void InjectForTest(RaidEventData data, string eventId = "RaidTest")
        {
            if (data == null) { Debug.LogError("[Raid] InjectForTest：data 为 null"); return; }
            if (string.IsNullOrEmpty(data.eventId)) data.eventId = eventId;
            LoadEventData(data, new ScoringEventPersistence { eventId = data.eventId }, Handler);
        }

        /// <summary>给自检用：造一份最小可用的事件（里程碑表 + 榜名）。</summary>
        public static RaidEvent CreateForTest(string leaderboardName, params RaidMilestone[] milestones)
        {
            var ev = new RaidEvent();
            var d = new RaidEventData
            {
                eventId = "RaidTest",
                leaderboardName = leaderboardName,
                scoringItemId = RaidMedalItemId,
            };
            if (milestones != null) d.scoringMilestones.AddRange(milestones);
            ev.InjectForTest(d);
            return ev;
        }
    }
}
