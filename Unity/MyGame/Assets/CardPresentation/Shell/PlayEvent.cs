// PlayEvent.cs —— **对战类活动基类**（原版 `PlayEvent<TSave,TData>` / `PlayEventSaveGame` /
//                  `PlayEventData` / `ArmyStatsV3` / `LastOpponentInfo` / `RankedMatchResult` /
//                  `OnlineMatchResult` 的等价实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §二（`PlayEvent<TSave,TData>` 的实例表：PracticeEvent ·
//     TutorialEvent，Ranked/FastMode/Draft 都继承它）+ §四 `PlayEvent<TSave,` 的逐方法表（18 条 C）
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 行 258-276（U08 工作项：存档模型 → 胜负/平推进 →
//     默认卡组与禁卡表 → 练习局收尾）
//   · 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/PlayEvent.cs`（泛型壳，见下面差异①）
//     与 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/PlayEventSaveGame.cs` / `Everguild.LiveOps/ArmyStatsV3.cs` /
//     `Everguild.LiveOps/PlayEventData.cs` / `LastOpponentInfo.cs` / `RankedMatchResult.cs` /
//     `OnlineMatchResult.cs` / `BattleWinner.cs`（**这些不是泛型类，体是完整的**）
//   · `MatchFinished` 的三支分派体读的是 ILSpy 那份**可读的**（`PlayEvent.cs:976`）——
//     它是非泛型代码，Cpp2IL 没把实参抹掉
//
// 🔴 **本类只建框架层，一行窗都没改**（B09 的独占文件只有 `LiveOpsEvent.cs` / `PlayEvent.cs` /
//    `FastModeBaseEvent.cs`）：`Shell/LiveOpsEventWindow.cs` 五个活动窗仍走 `LiveOpsLocalCatalog`。
//
// ⚠️ **与原版的如实差异（⛔ 不是「已经一样」，逐条写在这里）**：
//   ① **泛型方法体在本机只有「壳」**：`PlayEvent<T,T>` 的方法体在 `decomp_generic_trial/out/` **没有导出**
//      （那里只导了 `FastModeBaseEvent<,>`），ILSpy 那份被 Cpp2IL 抹成 `(nint)0` 的非法实参。
//      ⇒ 本类里**标注了「读法待核」的那些跳**（`StartMatch` 的防御卡分支）是按 ILSpy 的**控制流形状**复刻的，
//      实参可能反了；其余跳的语义有 `判据/泛型与启动链.md` §四 的逐方法表 + DisplayClass lambda 的 `.c` 佐证。
//   ② **没有服务器**：原版 `SetGamemodeDefaultDeck` / `RecordMatchFinished` / `OnCancelMatch` 三处云脚本
//      一律换成**本地落地 + 出声**（`SaveEventProgress` 写 `LiveOpsSaveStore`；`RecordMatchFinished`
//      直接写进度并立刻回调，**没有重试**）。⛔ 这不是「等价」，是**如实降级**。
//   ③ **卡组是 `string` id 不是 `CardDeck` 对象**：原版这一层拿 `CardDeck`（`AssetLocator.GetAsset<CardDeck>` /
//      `InventoryManager.GetInventory<CardDeck>()`）。我们的卡组唯一 id = `CollectionData.DeckInfo.Name`
//      （`Shell/CollectionData.cs:44` 写着「本轮兼作 id」）⇒ 本类只认 id，取卡的事走
//      <see cref="IPlayEventDeckSource"/>（⚠️ 那是**我们自己设计**的口，见那里的注释）。
//   ④ **`PlayEventData.IsCardBanned(RawCardScript)` → `IsCardBanned(string cardId)`**：我们没有
//      `RawCardScript` 这一类（卡 id 就是 `CardDef.Id`）。语义照抄：**缓存为 null ⇒ 恒 false**。
//
// 🔶 本文件里带 `🔶 自己设计` 标记的块 = 原版本机判据拿不到 / 必须我们自己定口径的地方（铁律 18）。

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 原版 `enum BattleWinner`
    /// <summary>原版 `BattleWinner`（`BattleWinner.cs`，显式值逐个抄）：`Undefined=0 · Player=10 ·
    /// Enemy=20 · Draw=30`。⛔ 不是 0/1/2 —— `MatchFinished` 的分派就是比这三个数。</summary>
    public enum BattleWinner
    {
        Undefined = 0,
        Player = 10,
        Enemy = 20,
        Draw = 30,
    }

    // ============================================================ 原版 `enum CardArmy`
    /// <summary>原版 `CardArmy`（`d:/2/tools/il2cpp_out/dump.cs`）：`Neutral=0 · Ultramarines=10 ·
    /// Goff=20 · SaimHann=30 · Sautekh=40 · BlackLegion=50 · Leviathan=60 · TauEmpire=70 ·
    /// Sororitas=80 · Genestealers=90 · AstraMilitarum=100 · DarkAngels=110 · EmperorsChildren=120 ·
    /// SpaceWolves=130`。值**逐个抄**（原版就是 10 一档）。</summary>
    public enum CardArmy
    {
        Neutral = 0,
        Ultramarines = 10,
        Goff = 20,
        SaimHann = 30,
        Sautekh = 40,
        BlackLegion = 50,
        Leviathan = 60,
        TauEmpire = 70,
        Sororitas = 80,
        Genestealers = 90,
        AstraMilitarum = 100,
        DarkAngels = 110,
        EmperorsChildren = 120,
        SpaceWolves = 130,
    }

    /// <summary>原版 `RankedV3ArmyState`（`RankedV3ArmyState.cs`）：**位标志** `None=0 · Boosted=1 ·
    /// Shielded=2 · Featured=4`。`FastModeBaseEvent.GetArmyState` 返回的就是它（实测返回 0 / 1 / 5）。</summary>
    public enum RankedV3ArmyState
    {
        None = 0,
        Boosted = 1,
        Shielded = 2,
        Featured = 4,
    }

    /// <summary>原版 `enum PlayModes`（`dump.cs:46188`）。**只有本批用得到的名字**抄了进来，
    /// 值的出处 = 同一段注释（`Shell/DailyData.cs:1439-1443` 已逐值核过一遍）。</summary>
    public enum PlayModes
    {
        Classic = 0,
        Duel = 1,
        PracticeLodge = 2,
        Dungeon = 3,
        Tutorial = 4,
        CutScene = 5,
        OfflinePractice = 6,
        ClosedDeck = 7,
        Campaign = 8,
        TutorialReplay = 9,
        Replay = 10,
        RankedFriendly = 11,
        OwnDeckTraining = 12,
        Skirmish = 13,
        Battle4Warpforge = 14,
    }

    // ============================================================ 原版 `ArmyStatsV3`
    /// <summary>原版 `ArmyStatsV3`（`Everguild.LiveOps/ArmyStatsV3.cs`，字段逐个抄）：
    /// `wins` / `losses` / `draws` / `lastPlayedDeck`（`LoadableReference&lt;CardDeck&gt;`）。
    /// <para>⚠️ 差异：`lastPlayedDeck` 是资产引用，我们没有等价物 ⇒ 存**卡组 id 字符串**
    /// （= `CollectionData.DeckInfo.Name`，见文件头差异③）。</para>
    /// <para>🔴 `GetArmyStats` 的缺省值是 **`new ArmyStatsV3()` 而不是 null**（判据 `.c`
    /// `GetArmyStats__1a0fbe0.c`：`GetValueOrDefault(dict, army, new ArmyStatsV3())`）—— 照抄。</para></summary>
    [Serializable]
    public class ArmyStatsV3
    {
        public int wins;
        public int losses;
        public int draws;
        public string lastPlayedDeckId;

        /// <summary>原版 `TotalGames`：`draws + losses + wins`（`.c` 里就是这三项相加）。</summary>
        public int TotalGames { get { return draws + losses + wins; } }

        /// <summary>原版 `LastPlayedDeck =&gt; lastPlayedDeck`（== null 表示没打过）。</summary>
        public string LastPlayedDeckId { get { return lastPlayedDeckId; } }
    }

    /// <summary>存档里「阵营 → 战绩」的一格。
    /// <para>🔶 自己设计（非原版）：原版是 `Dictionary&lt;CardArmy, ArmyStatsV3&gt; armyStats`，
    /// **`JsonUtility` 不序列化 Dictionary**（`判据/日常与活动.md` §二 Q2 那批存档坑的同一族）
    /// ⇒ 改成 `List&lt;ArmyStatsEntry&gt;`，读口 `GetArmyStats(army)` 语义不变（缺省 new）。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    [Serializable]
    public class ArmyStatsEntry
    {
        public CardArmy army;
        public ArmyStatsV3 stats;
    }

    // ============================================================ 原版 `LastOpponentInfo`
    /// <summary>原版 `LastOpponentInfo`（字段逐个抄）—— 上一局对手的速览，`PlayEventSaveGame.lastOpponentInfo` 存的就是它。
    /// <para>⚠️ 差异：原版实现 `IBeforeRead`（`OnBeforeRead(JToken)` 里读 `RankedGame` 决定要不要补字段）；
    /// 我们没有 Newtonsoft（`Shell/LiveOpsEvent.cs` 文件头差异①）⇒ 那一跳**没有搬**，字段照抄。</para></summary>
    [Serializable]
    public class LastOpponentInfo
    {
        public string OpponentId;
        public string OpponentName;
        public string PlayerHeroId;
        public string OpponentHeroId;
        public string OpponentAlliance;
        public string PlayerAlliance;
        /// <summary>本局结果：`1` = 胜 / `0` = 负 / `2` = 平（`FastModeBaseEvent.OnMatchWon/Lost/Draw`
        /// 的 `.c` 里分别写 `[info+0x40] = 1 / 0 / 2`）。</summary>
        public int Result;
        public int Season;
        public int Rating;
        public int Xp;
        public int Level;
        public int RankedGame;
    }

    // ============================================================ 原版 `PlayEventSaveGame`
    /// <summary>原版 `PlayEventSaveGame`（`PlayEventSaveGame.cs`，字段逐个抄）。
    /// <para>🔴 **`joinState` 是 private 字段 + 只读属性 `JoinState` + 写口 `UpdateLocalJoinState`**
    /// （原版就是这么封的，⛔ 别把 setter 开出来 —— 写口只有一条）。⚠️ 但**序列化**上加了 `[SerializeField]`（见字段那行的 🔶：原版靠 Newtonsoft 带私有字段，我们 JsonUtility 不带 ⇒ 不标就丢）。</para>
    /// <para>⚠️ 差异：`armyStats` 是 `Dictionary` → 见 <see cref="ArmyStatsEntry"/> 的 🔶 说明。</para></summary>
    [Serializable]
    public class PlayEventSaveGame : LiveOpsSave
    {
        /// <summary>原版嵌套枚举 `PlayEventSaveGame.JoinStates`：`NotJoined=0 · Joined=1 · Finished=2 · Expired=3`。</summary>
        public enum JoinStates
        {
            NotJoined = 0,
            Joined = 1,
            Finished = 2,
            Expired = 3,
        }

        [SerializeField] private JoinStates joinState;   // 🔶 自己设计（非原版）：joinState 序列化 · 原版靠 Newtonsoft 带私有字段，我们用 JsonUtility 需显式 [SerializeField] · 调度台 2026-10-29 裁定（不加 ⇒ 存档读回后 `JoinState` 恒 `NotJoined`）

        public string defaultDeckId;
        public int randomSeed;
        public bool matchStarted;
        public int numberOfMatchesWon;
        public int numberOfMatchesLost;
        public int numberOfMatchesDraw;
        public LastOpponentInfo lastOpponentInfo;
        public int currentRating;
        public List<ArmyStatsEntry> armyStats = new List<ArmyStatsEntry>();

        /// <summary>原版 `TotalMatchCount`：`draws + lost + won`。</summary>
        public int TotalMatchCount { get { return numberOfMatchesDraw + numberOfMatchesLost + numberOfMatchesWon; } }

        public JoinStates JoinState { get { return joinState; } }

        /// <summary>原版 `GetArmyStats(CardArmy)`：**缺省返回 `new ArmyStatsV3()`**（⛔ 不是 null）。
        /// 判据 = `PlayEventSaveGame.cs` 里那一句 `GetValueOrDefault(armyStats, army, new ArmyStatsV3())`。</summary>
        public virtual ArmyStatsV3 GetArmyStats(CardArmy army)
        {
            if (armyStats != null)
            {
                for (int i = 0; i < armyStats.Count; i++)
                {
                    var e = armyStats[i];
                    if (e != null && e.army == army) return e.stats ?? new ArmyStatsV3();
                }
            }
            return new ArmyStatsV3();
        }

        /// <summary>取（必要时建）该阵营那一格 —— 写进度时用。
        /// 🔶 自己设计（非原版）：「缺省 new 且不写回」在 C# 里没法当引用用（原版 `Dictionary` 的
        /// `GetValueOrDefault` 给的也是**临时对象**，所以原版写进度时必然自己 `Add`）。原版 `OnMatchWon`
        /// 那几条走的是 `GameModes` handler 的计数、不是这里，故本方法只是给我们本地落地用的写口。
        /// 调度台 C1 2026-10-27 定。</summary>
        public ArmyStatsV3 EnsureArmyStats(CardArmy army)
        {
            if (armyStats == null) armyStats = new List<ArmyStatsEntry>();
            for (int i = 0; i < armyStats.Count; i++)
            {
                var e = armyStats[i];
                if (e != null && e.army == army)
                {
                    if (e.stats == null) e.stats = new ArmyStatsV3();
                    return e.stats;
                }
            }
            var fresh = new ArmyStatsV3();
            armyStats.Add(new ArmyStatsEntry { army = army, stats = fresh });
            return fresh;
        }

        /// <summary>原版 `UpdateLocalJoinState(JoinStates)`：**只改本地字段**（原版的注释就叫 local；
        /// 写服务器是另一条路 `ChangeState` 的云脚本）。</summary>
        public void UpdateLocalJoinState(JoinStates state) { joinState = state; }
    }

    // ============================================================ 原版 `RankedMatchResult`
    /// <summary>原版 `RankedMatchResult`（字段逐个抄；构造 `(currentRating, currentXp)` →
    /// `startingRating` / `startingPlayerXP` / `resultSaved=false` / `finishedProcessingResult=false`）。
    /// <para>⚠️ 差异：原版还有 `BattleResult battleResult` 与 `OnlineMatchResult onlineMatchResult`
    /// （后者在构造里被 `new` 出来当一个「服务器回执的槽」）。我们本地没有服务器回执 ⇒
    /// **只留 `battleWinner` / 两个里程碑 / `duration` 四个被消费的字段**，那两个对象不建
    /// （如实记：这不是等价，是「本地没有那一层」）。</para></summary>
    public class RankedMatchResult
    {
        public bool resultSaved;
        public int targetMilestone;
        public int enemyMilestone;
        public bool finishedProcessingResult;
        public float duration;
        public BattleWinner battleWinner;
        public int startingPlayerXP;
        public int startingRating;

        public RankedMatchResult(int currentRating, int currentXp)
        {
            startingRating = currentRating;
            startingPlayerXP = currentXp;
            resultSaved = false;
            finishedProcessingResult = false;
        }

        /// <summary>原版 `SetBattleResult(BattleResult, BattleWinner)` —— 我们只有后半段（没有 `BattleResult`）。</summary>
        public void SetBattleWinner(BattleWinner winner) { battleWinner = winner; }

        /// <summary>原版 `GetWinner()`：Player ⇒ **1f** · Enemy ⇒ **0f** · 其余 ⇒ **0.5f**
        /// （🔴 平局在这里是 0.5，**不是 2.0** —— 2.0 是 `MatchFinished` 传给 `OnMatchFinished` 的
        /// `matchWon` 参数，两者是不同的量，⛔ 别混）。</summary>
        public float GetWinner()
        {
            if (battleWinner == BattleWinner.Player) return 1f;
            if (battleWinner == BattleWinner.Enemy) return 0f;
            return 0.5f;
        }
    }

    /// <summary>原版 `OnlineMatchResult`（`OnlineMatchResult.cs`）：`EndMatchResult EndMatchData` +
    /// `List&lt;BattleLogMatch&gt; BattleLogData` + `Dictionary&lt;string,int&gt; VictoryCounters` ——
    /// **全部是服务器结算的返回体**。
    /// <para>🔶 自己设计（非原版）：本地没有服务器（用户边界：P3 不复刻）⇒ 只留**三个回调要用的量**
    /// （成功与否 / 人话错误 / 新分）。⛔ 别把它当「服务器的等价物」—— 原版那三个字段一个都没搬。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    public class OnlineMatchResult
    {
        public bool success;
        public string error;
        public int newRating;
        /// <summary>🔶 自己设计（非原版）：原版 `OnlineMatchResult.EndMatchData`（`EndMatchResult`，服务器结算返回体）。
        /// 本机无服务器（P3）⇒ 只搬 `FastModeBaseEvent.OnMatchProcessSuccess` 读到的那三格
        /// （`NewRating(+0x14)` · `armyStats(+0x40)` · boost 表 `(+0x50)`）。填值属「服务器相关」⇒ 按 §18·0 打 🔶。
        /// 账 272 · B96_WB 2026-10-29。</summary>
        public EndMatchData EndMatchData;
    }

    /// <summary>🔶 自己设计（非原版）：原版 `EndMatchResult` 的子集（只搬 `OnMatchProcessSuccess` 读的三格）。
    /// 判据 = `FastModeBaseEvent_object__object___OnMatchProcessSuccess__1a110c0.c` ·
    /// `cpp2il/out_cpp2il_dev_ilspy/FastModeBaseEvent.cs:1553`（ILSpy 逐名）：`NewRating` ·
    /// `ArmyStats`（`SaveGame.armyStats = …` 直赋）· `RatingBoostState`（`RankedScoreBoostSave[]`，逐个更新 `RankedScoreBoost`）。
    /// ⚠️ `ArmyStats` 用本工程 `List&lt;ArmyStatsEntry&gt;`（与 `PlayEventSaveGame.armyStats` 同型、才能直赋；原版是 `Dictionary&lt;CardArmy,ArmyStatsV3&gt;`）。
    /// 账 272 · B96_WB 2026-10-29。</summary>
    public class EndMatchData
    {
        public int NewRating;                            // +0x14
        public List<ArmyStatsEntry> ArmyStats;           // +0x40
        public RankedScoreBoostSave[] RatingBoostState;  // +0x50
    }

    /// <summary>🔶 自己设计（非原版）：原版 `RankedScoreBoostSave` 的子集（本机无该类 · 只在注释里出现过）。
    /// 字段序照 `RankedScoreBoostData`（`preventScoreLoss/scoreBoostAmount/matchesCount`，判据见
    /// `FastModeBaseEvent.cs:107`）。只作为 `EndMatchData.RatingBoostState` 的元素类型 + 按 `id` 命中用。
    /// 账 272 · B96_WB 2026-10-29。</summary>
    public class RankedScoreBoostSave
    {
        public string id;
        public bool preventScoreLoss;
        public int scoreBoostAmount;
        public int matchesCount; public Dictionary<int, int> matchCount;   // 🔴原版的**唯一字段** = `Dictionary<CardArmy,int> matchCount`（`dump.cs` TypeDefIndex 3584；`GetWins(army)` 读它）；上面两格是 `RankedScoreBoostData` 的字段、**不是** Save 的 ⇒ 一并留着只作数据面，⛔ 别当 Save 形状。B98_H5 2026-10-29（零行数变化：追加在同一物理行）
    }

    /// <summary>原版 `MatchData`（对局数据）**只搬 `MakeOpponentInfo` 读的那几格**。
    /// <para>🔶 自己设计（非原版）：我们的大厅没有 `MatchData` 对象（联机是自有 P2P 协议，`Shell/Net`），
    /// 这里只定义「组对手信息需要什么」。调度台 C1 2026-10-27 定。</para></summary>
    public class MatchData
    {
        public string opponentId;
        public string opponentName;
        public string opponentHeroId;
        public string opponentAlliance;
        public string playerHeroId;
        public string playerAlliance;
        public int season;
    }

    /// <summary>原版 `RoomOptions`（Photon 的建房选项）—— **本类只用它的 `CustomRoomProperties`
    /// 与 `CustomRoomPropertiesForLobby` 两格**（`SetCustomMatchOptions` 那两条的唯一读者）。
    /// <para>🔶 自己设计（非原版）：本机没有 Photon（联机是我们自有的 P2P，`Shell/Net`），
    /// 且 `MsgStart` 里**没有 eventId / ELO / 连败**这些字段（账 row 275/568 的差异列写着）⇒
    /// 只留一个最小的键值袋 + 键名表，**没有把 Photon 那一层假装搬过来**。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    public class RoomOptions
    {
        /// <summary>原版 `CustomRoomProperties`（`Dictionary&lt;object,object&gt;`）。</summary>
        public readonly Dictionary<string, object> CustomRoomProperties = new Dictionary<string, object>();
        /// <summary>原版 `CustomRoomPropertiesForLobby`（要同步到大厅的键名表）。</summary>
        public string[] CustomRoomPropertiesForLobby;

        public void Set(string key, object value) { CustomRoomProperties[key] = value; }
        public object Get(string key)
        {
            object v;
            return CustomRoomProperties.TryGetValue(key, out v) ? v : null;
        }
    }

    // ============================================================ 原版 `PlayEventData`
    /// <summary>原版 `PlayEventData`（`Everguild.LiveOps/PlayEventData.cs`）：
    /// `bannedCards`（`BannedCardsSO`）+ 两个运行时缓存 `bannedCardsCache` / `admisionPrices` / `skullsRewards`。
    /// <para>🔴 `IsCardBanned` 的**反直觉**语义（照抄）：**缓存为 null ⇒ 恒 `false`** ——
    /// 也就是说「没配禁卡表」和「禁卡表是空的」在行为上**一样**（都放行）。</para>
    /// <para>🔴 `OnAfterRead` 每次都**先把缓存置 null** 再重建（反序列化后一致性靠它）。</para>
    /// <para>⚠️ 差异：原版 `IsCardBanned(RawCardScript card)` 取 `card.GetID()`；我们直接收卡 id 字符串
    /// （文件头差异④）。`admisionPrices` / `skullsRewards` 属别的 C 行（入场费/骷髅奖励），**本批不搬**。</para></summary>
    public abstract class PlayEventData : LiveOpsEventData
    {
        private HashSet<string> bannedCardsCache;

        /// <summary>原版 `BannedCardsSO.BannedCardIds` 的等价物（我们存裸 id 表，没有那个 SO）。
        /// ⚠️ 差异：原版是资产引用 + `OnAfterRead` 时才拉 id 表；我们直接存表。</summary>
        public List<string> bannedCardIds;

        /// <summary>原版 `IsCardBanned(RawCardScript card)`。</summary>
        public bool IsCardBanned(string cardId)
        {
            if (bannedCardsCache == null) return false;
            if (cardId == null) return false;          // 原版这里 NRE（我们出声不崩，见 LiveOpsEvent 同族口径）
            return bannedCardsCache.Contains(cardId);
        }

        /// <summary>原版 `OnAfterRead(JToken)`：**先清缓存**，再按 `bannedCards` 重建（null ⇒ 缓存留 null）。</summary>
        public void OnAfterRead()
        {
            bannedCardsCache = null;
            if (bannedCardIds == null) return;
            var set = new HashSet<string>();
            for (int i = 0; i < bannedCardIds.Count; i++)
                if (!string.IsNullOrEmpty(bannedCardIds[i])) set.Add(bannedCardIds[i]);
            bannedCardsCache = set;
        }
    }

    // ============================================================ 卡组取用口（我们自己的）
    /// <summary>「本类要的那几格卡组信息」的取用口。
    /// <para>🔶 自己设计（非原版）：原版这一层直接拿 `CardDeck`（`AssetLocator.GetAsset&lt;CardDeck&gt;(id)` /
    /// `InventoryManager.GetInventory&lt;CardDeck&gt;().GetItemInstances(true)`）。我们的卡组住在
    /// `CollectionData`（`static DeckLibrary`，没有 `AssetLocator`、也没有资产引用）⇒ 只能注入。
    /// **不注入时**：本类的 `GetEventDeck` 一族**不会静默返回 null**，而是出声并返回 null（红线：不许静默失败）。
    /// 调度台 C1 2026-10-27 定。</para></summary>
    public interface IPlayEventDeckSource
    {
        int Count { get; }
        /// <summary>第 i 套的 id（我们的 id = `CollectionData.DeckInfo.Name`）。</summary>
        string IdAt(int i);
        /// <summary>第 i 套的阵营（督军决定的那个 `CardArmy`；查不到给 `Neutral`）。</summary>
        CardArmy ArmyAt(int i);
        /// <summary>第 i 套的模式（`PlayModes`）。</summary>
        PlayModes GameModeAt(int i);
        /// <summary>第 i 套**能不能拿出去打**（原版 `DeckUtility.ValidateDeck(deck, out _)` 的等价物；
        /// 我们 = `DeckRules.Validate(...)` 通过）。</summary>
        bool IsPlayableAt(int i);
        /// <summary>按 id 找下标；没有 ⇒ **−1**。</summary>
        int IndexOfId(string id);
    }

    /// <summary>全局唯一那份取用口（`null` = 还没接上）。
    /// 🔶 自己设计（非原版）：原版是单例 `InventoryManager`，我们是静态注入点（与 `CardDatabase` /
    /// `CollectionData` 同族的做法）。调度台 C1 2026-10-27 定。</summary>
    public static class PlayEventDecks
    {
        public static IPlayEventDeckSource Source;

        /// <summary>自检口（⚠️ 只给断言用）。</summary>
        public static void ResetForTest() { Source = null; }
    }

    // ============================================================ 原版 `PlayEvent<TSave,TData>`
    /// <summary>原版 `PlayEvent&lt;TSave,TData&gt; : LiveOpsEvent&lt;TSave,TData&gt;` ——
    /// **对战类活动**的共同基类（`PracticeEvent` / `TutorialEvent` 直接继承；`Ranked` / `FastMode` / `Draft`
    /// 再往下一层）。判据 → `判据/泛型与启动链.md` §二 的状态→实例表。
    /// <para>本类负责三件事：① 事件级对局状态（`JoinStates`）② 一局打完的分派（胜/负/平）③ 活动卡组取用。</para></summary>
    public abstract partial class PlayEvent<TSave, TData> : LiveOpsEvent<TSave, TData>
        where TSave : PlayEventSaveGame
        where TData : PlayEventData
    {
        /// <summary>原版 `abstract PlayModes EventPlayMode` —— 每个活动声明「我打的是哪个模式」。
        /// 它同时是 `GetEventDeck` 那一族**筛卡组**的判据（模式对不上就不算）。</summary>
        public abstract PlayModes EventPlayMode { get; }

        /// <summary>原版 `protected virtual bool UseMatchMaking =&gt; true`（本批只有一个读者：
        /// `StartMatch` 里「要不要走匹配」那一跳）。</summary>
        protected virtual bool UseMatchMaking { get { return true; } }

        /// <summary>原版 `ShowInMainMenu` 的 PlayEvent 覆盖：`base.ShowInMainMenu &amp;&amp; EventState == Active`。
        /// 🔴 比基类**多一个状态闸** —— 不是「data 说显示就显示」（判据 `PlayEvent.cs:260`：
        /// `if (!base.ShowInMainMenu) return false; return (EventState - 2) == 0;`）。</summary>
        public override bool ShowInMainMenu
        {
            get
            {
                if (!base.ShowInMainMenu) return false;
                return EventState == LiveOpState.Active;
            }
        }

        /// <summary>原版 `ChangeState(JoinStates)`：**改本地 joinState**（`PlayEvent.cs:282` 读到的是
        /// `SaveGame + 0x30` 那一格 = `joinState`）。⚠️ 原版同名方法在子类里还会打云脚本；基类这一跳只写本地。</summary>
        protected void ChangeState(PlayEventSaveGame.JoinStates stateToChange)
        {
            if (SaveGame == null)
            {
                Debug.LogWarning("[PlayEvent] ChangeState：没有存档 ⇒ 不写（原版这一跳 NRE）");
                return;
            }
            SaveGame.UpdateLocalJoinState(stateToChange);
        }

        /// <summary>原版 `public new PlayEventSaveGame GetBaseSave()` —— 类型化的读口。</summary>
        public new PlayEventSaveGame GetBaseSave() { return SaveGame; }

        /// <summary>原版 `public new PlayEventData GetBaseData()` —— 类型化的读口。</summary>
        public new PlayEventData GetBaseData() { return Data; }

        /// <summary>原版 `Initialize()`（`PlayEvent.cs:359`，ILSpy 的控制流可读）：
        /// <code>
        /// base.Initialize();                    // LiveOpsEvent&lt;TSave,TData&gt;.Initialize()（排期刷新）
        /// if (SaveGame == null) return;
        /// if (EventState &gt;= Finished) return;   // 已结束/已过期的活动不查续局
        /// if (Handler != null &amp;&amp; Handler.IsFirstInitialization) CheckPreviousMatch();
        /// </code>
        /// 🔴 `IsFirstInitialization` 这个闸是**原版的**（`LiveOpsHandler&lt;IPlayEvent&gt;.IsFirstInitialization`），
        /// 它保证「本进程第一次装载这个 handler」才查续局（否则每次刷新都弹一次）。</summary>
        public override void Initialize()
        {
            base.Initialize();
            if (SaveGame == null) return;
            if ((int)EventState >= (int)LiveOpState.Finished) return;
            if (Handler == null) return;                       // 原版这一跳 NRE；我们跳过并出声
            if (!Handler.IsFirstInitialization) return;
            CheckPreviousMatch();
        }

        /// <summary>原版 `CheckPreviousMatch()`（`PlayEvent.cs:433`，控制流可读；DisplayClass12_0 的 `.c` 佐证）：
        /// <code>
        /// if (SaveGame == null) return;
        /// if (!SaveGame.matchStarted(+0x44) || SaveGame.lastOpponentInfo(+0x58) == null) return;   // ★ 2026-10-28 补：matchStarted 这口闸在 null 判据**之前**
        /// LogWarning("Found ongoing match data");
        /// key = Result==1 ? "Demo/MainMenu/OngoingMatchVictory"
        ///     : Result==2 ? "Demo/MainMenu/OngoingMatchDraw"
        ///     :             "Demo/MainMenu/OngoingMatch";
        /// MenuActions.QueueAction(() => 弹窗(key));        // 先排到菜单上（不打断当前流程）
        /// MatchFinished(new RankedMatchResult(0,0){ battleWinner = Undefined }, null);
        /// </code>
        /// ⇒ 语义 = **上次退出时对局没结算 ⇒ 按「未知结果」结算一次并提示玩家**。
        /// ⚠️ 差异：我们还没有 `MenuActions` 的等价物（那是菜单/外壳线）⇒ 改成 <see cref="OnQueueMenuPopup"/>
        /// 这个可注入的口（null 时**只记日志**，⛔ 不静默）。</summary>
        protected virtual void CheckPreviousMatch()
        {
            if (SaveGame == null || !SaveGame.matchStarted) return;   // ★ 原版两口闸（+0x44 先、+0x58 后），⛔ 别只留 null 那口
            var info = SaveGame.lastOpponentInfo;
            if (info == null) return;
            Debug.LogWarning("[PlayEvent] Found ongoing match data（'" + GetID() + "' 上一次对局没结算）");
            string key;
            BattleWinner winner;
            if (info.Result == 1) { winner = BattleWinner.Player; key = "Demo/MainMenu/OngoingMatchVictory"; }
            else if (info.Result == 2) { winner = BattleWinner.Draw; key = "Demo/MainMenu/OngoingMatchDraw"; }
            else { winner = BattleWinner.Enemy; key = "Demo/MainMenu/OngoingMatch"; }
            if (OnQueueMenuPopup != null) OnQueueMenuPopup(key);
            else Debug.LogWarning("[PlayEvent] 没有接 OnQueueMenuPopup ⇒ 续局提示 '" + key + "' 只记日志"
                                + "（原版走 MenuActions.QueueAction）");
            var result = new RankedMatchResult(0, 0);
            result.battleWinner = BattleWinner.Undefined;      // 🔴 原版这里就是 Undefined，不是上面算出来的 winner
            MatchFinished(result, null);
        }

        /// <summary>原版 `StartMatch()`（`PlayEvent.cs:520`）：
        /// <code>
        /// deck = GetEventDeck();
        /// if (!ValidateEventDeck(deck, true)) { ShowPopUp("MenuDeck/Error/InvalidDeck"); return; }
        /// if (DeckUtility.HasDefensiveCards(deck) == HaveItInInventory) { ShowPopUp("MenuDeck/Error/HasNotAssignedDefensiveCard"); return; }
        /// StartMatchNoCheck(deck);
        /// </code>
        /// <para>🔴 **两条错误文案就是这一条 C 的缺口**（账 row 268 的差异列），文案键逐字照抄 ILSpy。
        /// ⚠️ 读法待核（文件头差异①）：「有防御卡在库里 ⇒ 反过来提示『没分配防御卡』」这个分支按 ILSpy
        /// 的 `if (== HaveItInInventory) { ShowPopUp(HasNotAssignedDefensiveCard); }` 形状复刻；
        /// `PlayEvent&lt;,&gt;` 的机器码本机没导出（`decomp_generic_trial/out` 只有 `FastModeBaseEvent`）⇒
        /// ⛔ 别把它当成「已坐实」，改之前先补导出。
        /// ⚠️ 弹出交给 <see cref="OnErrorPopup"/>（我们的窗没有 `WindowsManager.ShowPopUp` 那条链）。</summary>
        public virtual void StartMatch()
        {
            string deckId = GetEventDeck();
            if (!ValidateEventDeck(deckId, true))
            {
                PopupError("MenuDeck/Error/InvalidDeck");
                return;
            }
            if (HasDefensiveCardInInventory(deckId))
            {
                PopupError("MenuDeck/Error/HasNotAssignedDefensiveCard");
                return;
            }
            StartMatchNoCheck(deckId);
        }

        /// <summary>原版 `StartMatchNoCheck(CardDeck)`：`SetAsCurrentlyPlayingEvent(this)` → `if (!UseMatchMaking) return;`
        /// → 开 `SearchingMatchWindowDemo` →（在联盟里时排 `AlliancesManager.GetCurrentGroup`）→ `CreatePlayerBattleData` → `FindMatch`。
        /// 🔴 **2026-10-29 就地订正（B94_Z4 · 铁律 5）**：原写「= `ChangeState(Joined)` + 记 `matchStarted` + 抖 `randomSeed`」**不成立** ——
        /// `_tmp_view/decomp_b91/PlayEvent_object__object_.StartMatchNoCheck__1127900.c` 全文**不写 `+0x30/+0x40/+0x44`** 任何一格。
        /// ⚠️ 本仓因为**没有匹配界面**，这里只做「事件级状态那三件」当**本地等价**（`joinState = Joined` + `matchStarted = true`
        /// + `randomSeed` 兜底），并把落盘走 `SaveEventProgress`；⛔ 别再把这三句当成原版行为。
        /// 🔶 自己设计（非原版）：`randomSeed` 取 `Environment.TickCount`；原版这一格**从未被写**（恒 0），本地抖它是为了让「同一局重连能复现」。</summary>
        public virtual void StartMatchNoCheck(string deckId)
        {
            if (SaveGame == null) return;
            ChangeState(PlayEventSaveGame.JoinStates.Joined);
            SaveGame.matchStarted = true;
            if (SaveGame.randomSeed == 0) SaveGame.randomSeed = Environment.TickCount;
            SaveEventProgress(null, null);
            Debug.Log("[PlayEvent] StartMatch：'" + GetID() + "' 开局，卡组 id = " + (deckId ?? "<null>")
                    + "（本地等价：只落事件级状态；拉对局是战斗/联机线）");
        }

        /// <summary>原版 `OnMatchFound(string otherPlayerName)`（判据 = 2026-10-28 补导的 `decomp_b91/PlayEvent_object__object_.OnMatchFound__1127660.c`，逐句）：
        /// `save.matchStarted(+0x44)=true` → 日志 `MatchFoundCallback, otherPlayerName X, OpponentId Y` → `save.lastOpponentInfo(+0x58) = MakeOpponentInfo(matchData)`（`matchData` = `PlayerDataManager.singletonManager.matchData`，本地等价 → <see cref="PlayEventMatchContext.Current"/>；存档 null 时原版 NRE，我们出声后只打日志）。</summary>
        protected virtual void OnMatchFound(string otherPlayerName)
        {
            if (SaveGame != null) { SaveGame.matchStarted = true; SaveGame.lastOpponentInfo = MakeOpponentInfo(PlayEventMatchContext.Current); SaveEventProgress(null, null); } else Debug.LogWarning("[PlayEvent] OnMatchFound：没有存档（原版这一跳 NRE）⇒ 两句没写");   // 🆕 `SaveEventProgress` = 写穿（原版这两句之后由服务器持久化，本地落地 = 本地存档）
            Debug.Log("[PlayEvent] MatchFoundCallback, otherPlayerName " + (otherPlayerName ?? "<null>") + ", OpponentId " + (PlayEventMatchContext.Current != null ? PlayEventMatchContext.Current.opponentId : "<null>"));
        }

        /// <summary>原版 `MakeOpponentInfo(MatchData)`（判据 = `decomp_generic_trial/out_b94/PlayEvent_object__object_.MakeOpponentInfo__11273c0.c`，逐格）：
        /// `OpponentId(+0x10)` / `OpponentName(+0x18 = enemyRawBattleData.deckName)` / `PlayerHeroId(+0x20)` / `OpponentHeroId(+0x28)` / `Result(+0x40)=0` /
        /// `RankedGame(+0x58)` / `Season(+0x44)`（原版那格是 `int?`）。
        /// 🔴 **2026-10-29（账 561 · 两把钥匙定名）**：后两格原版的两个 getter 已解出，**不是"未解"** ——
        /// `.c:38-46` 先把 `this` 转成接口 `DAT_1842ad4f0`（= **`IRankedEvent`**，`(this as IRankedEvent)`），再两次接口分派
        /// `FUN_1800021f0(idx, IRankedEvent, obj)`：
        /// · `idx = 4` → `RankedGame`：`klass` 槽 `4` = **`IRankedEvent.get_IsRanked()`（bool）** ⇒ `? 1 : 0`（`dump.cs:130546-130547`）
        /// · `idx = 1` → `Season`：`klass` 槽 `1` = **`IRankedEvent.get_SeasonNumber()`（int）** ⇒ 包成 `int?`（`dump.cs:130537-130538`）
        /// （钥匙口径 = `资料/全量反编译_入口与用法.md` §「VA 反汇编的两把通用钥匙」：`FUN_1800021F0(idx,…)` 的 `idx` = `dump.cs` 的 `Slot:`）</summary>
        protected virtual LastOpponentInfo MakeOpponentInfo(MatchData matchData)
        {
            var info = new LastOpponentInfo();
            if (matchData == null) return info;
            info.OpponentId = matchData.opponentId;
            info.OpponentName = matchData.opponentName;
            info.OpponentHeroId = matchData.opponentHeroId;
            info.OpponentAlliance = matchData.opponentAlliance;
            info.PlayerHeroId = matchData.playerHeroId;
            info.PlayerAlliance = matchData.playerAlliance;
            // 两跳 = `IRankedEvent.get_IsRanked()`（Slot 4，bool → ?1:0）与 `IRankedEvent.get_SeasonNumber()`（Slot 1，int）
            // —— 见上方 summary 的「两把钥匙定名」。等价物：`RankedData` 就是我们的排位活动状态（`IsRanked` / `SeasonNumber`
            // 与那两个 getter 同义）。⚠️ 残差（如实记）：原版有 `this is IRankedEvent` 那道闸（非排位 PlayEvent ⇒ RankedGame=0、
            // Season=null）；本机没有 `IRankedEvent` 接口（见 `GetEventDeck` 第 ③ 支那条注释）⇒ 无法表达那道闸，
            // 非排位事件若走到这里会读到全局 `RankedData`。排位活动的两格与 getter 逐义一致。
            info.RankedGame = RankedData.IsRanked ? 1 : 0; info.Season = RankedData.SeasonNumber;   // 账 561（B96_WB 2026-10-29）：getter 已定名并撤 🔶（见上两段注释）
            return info;
        }

        /// <summary>原版 `CancelMatch()`（判据 = `decomp_b91/PlayEvent_object__object_.CancelMatch__11268a0.c`）：
        /// `save.lastOpponentInfo(+0x58) = **new LastOpponentInfo()**`（不是 null！）→ `save.matchStarted(+0x44) = false` → `CustomDebug.Log("CancelMatch")` → 云脚本 `0x448 OnCancelMatch{eventId}`。
        /// ⚠️ 本地等价：前两句照抄 + 出声；云脚本那一跳没有服务器（P3）⇒ 出声跳过。🔴 赋**非 null** 正是为了让 `CheckPreviousMatch` 的 null 判据失效、改由 `matchStarted` 表达状态（三口闸一起改）。</summary>
        public virtual void CancelMatch()
        {
            if (SaveGame != null)
            {
                SaveGame.lastOpponentInfo = new LastOpponentInfo();   // ★ 原版赋的是**新建空对象**，不是 null（见上）
                SaveGame.matchStarted = false; SaveEventProgress(null, null);   // 🆕 写穿：原版这两句之后由云脚本 `0x448 OnCancelMatch` 在服务端持久化（P3）⇒ 本地落地 = 写本地存档
            }
            Debug.Log("[PlayEvent] CancelMatch：'" + GetID() + "'（原版会打 CloudHandle.OnCancelMatch，本地没有服务器）");
        }

        /// <summary>原版 `SetCustomMatchOptions(RoomOptions options, int currentElo)`（`PlayEvent.cs:1088`）：
        /// **virtual 空体**（基类什么都不写；各活动子类往 `CustomRoomProperties` 里塞自己的键）。
        /// ✅ 硬判据：ILSpy 那一支的体就是 `{ }`（`FastModeBaseEvent` 才覆盖它）。⛔ 别在基类加逻辑。</summary>
        public virtual void SetCustomMatchOptions(RoomOptions options, int currentElo) { }

        /// <summary>原版 `OnMatchFinished(matchWon, result, callback)` —— **virtual 空体**，
        /// 子类自己写结算（`FastModeBaseEvent` 覆盖它）。⛔ 别在基类加逻辑。</summary>
        protected virtual void OnMatchFinished(float matchWon, RankedMatchResult result, Action<OnlineMatchResult> callback) { }

        /// <summary>原版 `MatchFinished(RankedMatchResult, Action&lt;OnlineMatchResult&gt;)`（`PlayEvent.cs:976`，**逐条照抄**）：
        /// <code>
        /// float matchWon = 0;
        /// switch (result.battleWinner) {
        ///     case Player: OnMatchWon();  matchWon = 1f; break;
        ///     case Enemy:  OnMatchLost(); matchWon = 0f; break;
        ///     case Draw:   OnMatchDraw(); matchWon = 2f; break;   // ★ 平局 = 2.0
        /// }
        /// OnMatchFinished(matchWon, result, onCompleted);
        /// </code>
        /// 🔴 **两处反直觉**：① 平局的分值 `matchWon = 2.0f`（不是 0.5）——
        /// 里程碑按「2 格」推进就是从这里来的；② `battleWinner` 是 `Undefined/其它` 时**三支都不进**，
        /// 但仍会调 `OnMatchFinished(0, …)`（`CheckPreviousMatch` 的续局结算就走这条）。</summary>
        public virtual void MatchFinished(RankedMatchResult result, Action<OnlineMatchResult> onCompleted)
        {
            if (result == null)
            {
                Debug.LogError("[PlayEvent] MatchFinished：result 是 null（原版这一跳 NRE）⇒ 放弃本次结算");
                return;
            }
            float matchWon = 0f;
            if (result.battleWinner == BattleWinner.Player) { OnMatchWon(); matchWon = 1f; }
            else if (result.battleWinner == BattleWinner.Enemy) { OnMatchLost(); matchWon = 0f; }
            else if (result.battleWinner == BattleWinner.Draw) { OnMatchDraw(); matchWon = 2f; }
            OnMatchFinished(matchWon, result, onCompleted);
        }

        /// <summary>原版 `ValidateEventDeck(CardDeck deck, bool validateOwnership = true)`（`PlayEvent.cs:1017`）：
        /// <code>
        /// if (!DeckUtility.ValidateDeck(deck, out _, validateOwnership)) return false;
        /// return !GameStaticData.CheckHiddenCardsInDeck(deck);
        /// </code>
        /// ⇒ 两跳：**构筑合法** ∧ **不含隐藏卡**。
        /// ⚠️ 我们的等价物 = `DeckRules.Validate(...)`（构筑那一跳，判据同源）+ <see cref="IPlayEventDeckSource.IsPlayableAt"/>
        /// （**隐藏卡**那一跳本机没有等价物 ⇒ 它在 `IsPlayableAt` 里由实现方决定，⛔ 本类不假装已经判了）。</summary>
        public virtual bool ValidateEventDeck(string deckId, bool validateOwnership = true)
        {
            var src = PlayEventDecks.Source;
            if (src == null)
            {
                Debug.LogError("[PlayEvent] ValidateEventDeck：没有注入 IPlayEventDeckSource ⇒ 无法校验卡组 '"
                             + (deckId ?? "<null>") + "'（原版走 DeckUtility.ValidateDeck）");
                return false;
            }
            int i = string.IsNullOrEmpty(deckId) ? -1 : src.IndexOfId(deckId);
            if (i < 0) return false;                 // 卡组不存在 ⇒ 原版 ValidateDeck 也返 false
            return src.IsPlayableAt(i);
        }

        /// <summary>原版 `DeckUtility.HasDefensiveCards(deck) == HaveItInInventory` 那一跳的等价物。
        /// ⚠️ 本机防御卡是**我们补的**（`预组卡组线` 记忆：原版是 null）⇒ 本地**永远当作「没缺」**（返回 false），
        /// 让 `StartMatch` 不因为一个我们自造的概念把玩家挡在外面。
        /// 🔶 自己设计（非原版）：原版这一跳判的是「防御卡在不在背包 / 有没有分配」，我们没有那条链。
        /// 调度台 C1 2026-10-27 定。</summary>
        protected virtual bool HasDefensiveCardInInventory(string deckId) { return false; }

        /// <summary>原版 `OnMatchWon()`：`SaveGame.numberOfMatchesWon += 1`，然后
        /// **`if (lastOpponentInfo != null) lastOpponentInfo.Result = 1`**
        /// （判据 `PlayEvent.cs:1028`：`v11 = save+0x48` 加 1、`v24 = save+0x58` 非 null 时写 `1`）。</summary>
        protected virtual void OnMatchWon()
        {
            if (SaveGame == null) return;
            SaveGame.numberOfMatchesWon += 1;
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 1;
        }

        /// <summary>原版 `OnMatchLost()`：`numberOfMatchesLost += 1` + `lastOpponentInfo.Result = 0`。</summary>
        protected virtual void OnMatchLost()
        {
            if (SaveGame == null) return;
            SaveGame.numberOfMatchesLost += 1;
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 0;
        }

        /// <summary>原版 `OnMatchDraw()`：`numberOfMatchesDraw += 1` + `lastOpponentInfo.Result = 2`
        /// （判据三条 `.c`：`OnMatchWon/Lost/Draw__1a11660/1a11070/1a109f0.c` 里写的分别是
        /// `[info+0x40] = 1 / 0 / 2`）。</summary>
        protected virtual void OnMatchDraw()
        {
            if (SaveGame == null) return;
            SaveGame.numberOfMatchesDraw += 1;
            if (SaveGame.lastOpponentInfo != null) SaveGame.lastOpponentInfo.Result = 2;
        }

        /// <summary>原版 `abstract GetMatchmakingFilter(currentElo, tryCount, gamesLost)`：每个活动给自己的
        /// 匹配过滤串。没有实现的活动**必须**给出自己的（⛔ 别在基类编一个默认串）。</summary>
        public abstract string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost);

        /// <summary>原版 `GetEventDeck()`（判据 = 2026-10-28 补导的 `decomp_b91/PlayEvent_object__object_.GetEventDeck__1127000.c`，95 行，逐支）：
        /// <code>
        /// if (SaveGame == null) NRE;
        /// ① if (!string.IsNullOrWhiteSpace(save.defaultDeckId(+0x38))) { deck = AssetLocator.GetAsset(...);
        ///        if (deck != null &amp;&amp; deck.GameMode == EventPlayMode &amp;&amp; ValidateDeck(deck)) return deck; }
        /// ② if (&lt;handler != null&gt;) return 所有卡组.FirstOrDefault(b__26_0);   // 模式对 ∧ 可打（★ 这一支**直接 return**，不进 ③）
        /// ③ return GameModes.GetActiveEvent&lt;IRankedEvent&gt;().GetEventDeck();  // ★ 只在 ② 的 handler 为空时才走
        /// </code>
        /// ⚠️ 本地等价：②/③ 之间那道闸（handler 是不是空）我们没有对应物 ⇒ ③ 放在 ② 扫表之后当**兜底**
        /// （多出来的行为如实记在账 281 的差异列）。
        /// ③ 的取口 = <see cref="PlayEventRankedDeck.Source"/>（本机没有 `IRankedEvent` 接口、也没有 `GameModes` 单例
        /// ⇒ 由排位线注入；null 时出声一次再回 null，⛔ 不静默）。</summary>
        public virtual string GetEventDeck()
        {
            var src = PlayEventDecks.Source;
            if (src == null)
            {
                Debug.LogError("[PlayEvent] GetEventDeck：没有注入 IPlayEventDeckSource ⇒ 取不到卡组");
                return null;
            }
            if (SaveGame != null && !string.IsNullOrEmpty(SaveGame.defaultDeckId))
            {
                int i = src.IndexOfId(SaveGame.defaultDeckId.Trim());
                if (i >= 0 && src.GameModeAt(i) == EventPlayMode && src.IsPlayableAt(i))
                    return src.IdAt(i);
            }
            // ② 原版这一支 = 「扫全表取 FirstOrDefault(b__26_0)」（模式对 ∧ 可打）；★ 原版 ② 有结果时**直接 return**，不进 ③
            for (int i = 0; i < src.Count; i++)
                if (GetEventDeckPredicate(src.IdAt(i))) return src.IdAt(i);
            return GetEventDeckRankedOrNull(src);      // ③ 原版 `GameModes.GetActiveEvent<IRankedEvent>().GetEventDeck()`
        }

        /// <summary>原版 `GetEventDeck(CardArmy army)`（`PlayEvent.cs:1286`）：
        /// <code>
        /// stats = SaveGame.GetArmyStats(army);
        /// deck  = stats.LastPlayedDeck;
        /// if (deck != null &amp;&amp; deck.GameMode == EventPlayMode) return deck;
        /// return 背包.FirstOrDefault(d =&gt; d.Army == army &amp;&amp; d.GameMode == EventPlayMode);
        /// </code>
        /// 🔴 **第二支不再校验卡组**（predicate 只有「阵营 ∧ 模式」两条 —— 判据 = DisplayClass27_0 的 `.c`：
        /// `CardDeck__get_Army(x) == army &amp;&amp; CardDeck__get_GameMode(x) == EventPlayMode`）。照抄。</summary>
        public virtual string GetEventDeck(CardArmy army)
        {
            var src = PlayEventDecks.Source;
            if (src == null)
            {
                Debug.LogError("[PlayEvent] GetEventDeck(army)：没有注入 IPlayEventDeckSource ⇒ 取不到卡组");
                return null;
            }
            if (SaveGame != null)
            {
                var stats = SaveGame.GetArmyStats(army);
                if (stats != null && !string.IsNullOrEmpty(stats.LastPlayedDeckId))
                {
                    int j = src.IndexOfId(stats.LastPlayedDeckId);
                    if (j >= 0 && src.GameModeAt(j) == EventPlayMode) return src.IdAt(j);
                }
            }
            for (int i = 0; i < src.Count; i++)
                if (src.ArmyAt(i) == army && src.GameModeAt(i) == EventPlayMode) return src.IdAt(i);
            return null;
        }

        /// <summary>原版 `SetDefaultDeck(CardDeck deck, bool upload = true)`（`PlayEvent.cs:1371`）：
        /// `SaveGame.defaultDeckId = deck.deckId`；`if (upload &amp;&amp; !IsNullOrEmpty(defaultDeckId)) 打云脚本`.
        /// ⚠️ 本地等价：写字段 + **落盘**（`SaveEventProgress`）；「云脚本」那一跳换成出声。</summary>
        public void SetDefaultDeck(string deckId, bool upload = true)
        {
            if (SaveGame == null)
            {
                Debug.LogWarning("[PlayEvent] SetDefaultDeck：没有存档 ⇒ 不写（原版 NRE）");
                return;
            }
            SaveGame.defaultDeckId = deckId;
            if (upload && !string.IsNullOrEmpty(deckId)) UploadDefaultDeck();
            SaveEventProgress(null, null);
        }

        /// <summary>原版 `UploadDefaultDeck()`（`PlayEvent.cs:1398`）：`if (IsNullOrEmpty(defaultDeckId)) return;`
        /// 然后打 `CloudHandle.SetGamemodeDefaultDeck{eventId, deckId}`。
        /// ⚠️ 本地没有云脚本 ⇒ **只出声 + 落盘**（⛔ 不假装上传成功）。</summary>
        public void UploadDefaultDeck()
        {
            if (SaveGame == null) return;
            if (string.IsNullOrEmpty(SaveGame.defaultDeckId)) return;
            Debug.Log("[PlayEvent] UploadDefaultDeck：'" + GetID() + "' 默认卡组 = " + SaveGame.defaultDeckId
                    + "（原版打 CloudHandle.SetGamemodeDefaultDeck；本地没有服务器 ⇒ 只落本地存档）");
            SaveEventProgress(null, null);
        }

        /// <summary>原版 `GetEventDeck` 里那个匿名谓词 `b__26_0(CardDeck d)`（`PlayEvent.cs:1438`）：
        /// <code>
        /// d != null &amp;&amp; d.GameMode == EventPlayMode &amp;&amp; DeckUtility.ValidateDeck(d, out error)
        /// </code>
        /// 🔴 **它比 `GetEventDeck(army)` 的那个谓词多一条 `ValidateDeck`** —— 两条别混。</summary>
        public bool GetEventDeckPredicate(string deckId)
        {
            var src = PlayEventDecks.Source;
            if (src == null || string.IsNullOrEmpty(deckId)) return false;
            int i = src.IndexOfId(deckId);
            if (i < 0) return false;
            return src.GameModeAt(i) == EventPlayMode && src.IsPlayableAt(i);
        }

        // ── 两个可注入的口（我们自己的；原版走 WindowsManager / MenuActions）
        /// <summary>原版 `WindowsManager.ShowPopUp(key, localizeTexts: true, …)` 的等价口。
        /// 🔶 自己设计（非原版）：我们弹窗走 `Shell/*Window` 自己的链，没有全局 `WindowsManager`。
        /// null 时**只记日志**（⛔ 不静默失败：日志里带文案键）。调度台 C1 2026-10-27 定。</summary>
        public static Action<string> OnErrorPopup;

        /// <summary>原版 `MenuActions.QueueAction(...)` 的等价口（`CheckPreviousMatch` 的续局提示用）。
        /// 🔶 自己设计（非原版）：同 <see cref="OnErrorPopup"/>。调度台 C1 2026-10-27 定。</summary>
        public static Action<string> OnQueueMenuPopup;

        /// <summary>把文案键交给弹窗口（没有口就出声）。</summary>
        protected void PopupError(string locKey)
        {
            if (OnErrorPopup != null) { OnErrorPopup(locKey); return; }
            Debug.LogWarning("[PlayEvent] 要弹的提示 '" + locKey + "' 没有接弹窗口（原版 WindowsManager.ShowPopUp）");
        }
    }

    /// <summary>「当前对局数据」那一格（原版 `PlayerDataManager.singletonManager.matchData`）的本地替身。
    /// 🔶 自己设计（非原版）：原版 `OnMatchFound` 从 `PlayerDataManager.singletonManager.matchData`（`+0x20`）取
    /// `MatchData` 再交给 `MakeOpponentInfo`（判据 = `decomp_b91/PlayEvent_object__object_.OnMatchFound__1127660.c`）；
    /// 本地没有那一格（大厅/联机是我们自有的 P2P，`MatchData` 本身也是我们的替身，见它的注释）
    /// ⇒ 由大厅/联机线在「匹配成功」时写进来。null ⇒ `MakeOpponentInfo(null)` 回一份**全默认**的 `LastOpponentInfo`
    /// （原版这一跳 NRE，我们出声不崩）。
    /// ⚠️ 放在**非泛型**静态类里：泛型类的 `static` 是**按闭合类型各一份**（`GameModesHandler.cs:443` 记的那条坑），
    /// 那种「全局一格」放泛型类里会静默变成三份。A3 2026-10-28 裁定。</summary>
    public static class PlayEventMatchContext
    {
        /// <summary>当前这一场的 `MatchData`（`null` = 还没有/未知）。</summary>
        public static MatchData Current;

        /// <summary>自检口：清干净（否则下一个自检会读到上一场的数据）。</summary>
        public static void ResetForTest() { Current = null; }
    }

    /// <summary>原版 `PlayEvent.GetEventDeck()` **第 ③ 支**（`GameModes.GetActiveEvent&lt;IRankedEvent&gt;().GetEventDeck()`）的取口。
    /// 🔶 自己设计（非原版）：本机没有 `IRankedEvent` 接口、也没有 `GameModes` 单例（`GetActiveEvent` 要实例状态）
    /// ⇒ 由排位线注入「排位活动当前那副卡组的 id」。null ⇒ **出声一次**再返回 null（⛔ 不静默失败）。
    /// ⚠️ 同 <see cref="PlayEventMatchContext"/>：放非泛型静态类，避免「按闭合类型各一份」。
    /// A3 2026-10-28 裁定（判据：`decomp_b91/PlayEvent_object__object_.GetEventDeck__1127000.c` 末段）。</summary>
    public static class PlayEventRankedDeck
    {
        /// <summary>`() =&gt; 排位活动当前卡组的 id`（认不出/没有 ⇒ 回 null/空串）。</summary>
        public static Func<string> Source;

        /// <summary>取 ③ 那一支的卡组；没接就出声一次并回 null（= 原版 `GetActiveEvent&lt;IRankedEvent&gt;()` 为 null 那一支）。</summary>
        public static string TryGet()
        {
            if (Source == null)
            {
                if (!_warned) { _warned = true; Debug.LogWarning("[PlayEvent] GetEventDeck：③ 原版这一支问 `GameModes.GetActiveEvent<IRankedEvent>().GetEventDeck()`，"
                                                              + "本地没有那条链（`PlayEventRankedDeck.Source` 没接）⇒ 回 null"); }
                return null;
            }
            string deck = Source();
            if (string.IsNullOrEmpty(deck) && !_warned)
            {
                _warned = true;
                Debug.LogWarning("[PlayEvent] GetEventDeck：③ 取口接上了，但排位活动当下没有返回卡组 ⇒ 回 null");
            }
            return string.IsNullOrEmpty(deck) ? null : deck;
        }

        static bool _warned;

        /// <summary>自检口：清注入口 + 告警去重。</summary>
        public static void ResetForTest() { Source = null; _warned = false; }
    }
}

// ============================================================ `IPlayEvent` 接口（B92·N1 · 2026-10-28）
// 判据 = `cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/IPlayEvent.cs`（**整条接口逐行现读**，13 个成员）：
//   `IPlayEvent : ILiveOps` —— `PlayModes EventPlayMode`(get) · `PlayEventSaveGame.JoinStates JoinState`(get) ·
//   `int CurrentGamesLost`(get) · `void StartMatch()` · `void CancelMatch()` ·
//   `void MatchFinished(RankedMatchResult, Action<OnlineMatchResult>)` · `string GetMatchmakingFilter(int,int,int)` ·
//   `void SetCustomMatchOptions(RoomOptions,int)` · `void SetDefaultDeck(CardDeck,bool)` · `CardDeck GetEventDeck()` ·
//   `CardDeck GetEventDeck(CardArmy)` · `new PlayEventData GetBaseData()` · `new PlayEventSaveGame GetBaseSave()`。
//
// 🔴 **建它的代价**（账 row 60 的差异列）：缺接口 ⇒ `GameModesHandler.GetActiveEvent(PlayModes)` 只能**反射**读
//    `EventPlayMode`、`GameModesHandler.CombatFinished` 只能**反射**调 `MatchFinished`。
//    建好之后那两处可以换回强类型（⚠️ 本批**没换**：`GameModesHandler.cs` 不在 B92·N1 白名单，交给 N5）。
//
// ⚠️ **与原版的两处如实差异**（⛔ 不是「已经一样」）：
//   ① `CardDeck` → **`string`**：本工程卡组没有 `CardDeck` 资产对象，唯一 id = `CollectionData.DeckInfo.Name`
//      （文件头差异③；`GetEventDeck()` / `GetEventDeck(CardArmy)` / `SetDefaultDeck` 三处同此）。
//   ② `JoinState` / `CurrentGamesLost` 在原版是**接口的默认实现**（Cpp2IL 在两处都写
//      `NoteDecompilerIssue("Method not found @…")`，但 `CurrentGamesLost` 的控制流可读 =
//      `LiveOpsManager.GetHandler<GameModes>() … return handler.LossStreak`）——
//      我们改成**接口只声明、实现落在类上**（C# 8 的默认接口成员在 IL2CPP 上有风险，⛔ 别改成默认实现）。
namespace CardPresentation
{
    /// <summary>原版 `IPlayEvent`：对战类活动的接口面（`PracticeEvent` / `TutorialEvent` 直接实现；
    /// `Ranked` / `FastMode` / `Draft` 经由 `PlayEvent&lt;,&gt;` 继承）。</summary>
    public interface IPlayEvent : ILiveOps
    {
        /// <summary>原版 `PlayModes EventPlayMode { get; }` —— 同时是 `GetEventDeck` 一族筛卡组的判据。</summary>
        PlayModes EventPlayMode { get; }
        /// <summary>原版 `JoinState`（默认实现读 `SaveGame + 0x30` = `PlayEventSaveGame.joinState`）。</summary>
        PlayEventSaveGame.JoinStates JoinState { get; }
        /// <summary>原版 `CurrentGamesLost`（默认实现 = `LiveOpsManager.GetHandler&lt;GameModes&gt;().LossStreak`）。</summary>
        int CurrentGamesLost { get; }
        void StartMatch();
        void CancelMatch();
        void MatchFinished(RankedMatchResult result, Action<OnlineMatchResult> callback);
        string GetMatchmakingFilter(int currentElo, int tryCount, int gamesLost);
        void SetCustomMatchOptions(RoomOptions options, int currentElo);
        /// <summary>⚠️ 差异①：原版是 `CardDeck`。</summary>
        void SetDefaultDeck(string deckId, bool upload = true);
        /// <summary>⚠️ 差异①：原版是 `CardDeck`。</summary>
        string GetEventDeck();
        /// <summary>⚠️ 差异①：原版是 `CardDeck`。</summary>
        string GetEventDeck(CardArmy army);
        new PlayEventData GetBaseData();
        new PlayEventSaveGame GetBaseSave();
    }

    // ── 类那半：`IPlayEvent` 要求的两个成员（其余成员 `PlayEvent<,>` 上早已存在）
    // 🔴 追加在文件末尾（⛔ 不插进上面任何类体）—— 账按 `文件:行号` 引用，插中间会让下游落点整体漂移。
    public abstract partial class PlayEvent<TSave, TData> : IPlayEvent
        where TSave : PlayEventSaveGame
        where TData : PlayEventData
    {
        /// <summary>原版 `IPlayEvent.JoinState`（接口默认实现）：读 `SaveGame.joinState`（`+0x30` 那一格，
        /// 已由 `PlayEventSaveGame.joinState` 逐字段对过）。`SaveGame == null` ⇒ 原版 NRE，我们出声 + `NotJoined`。</summary>
        public PlayEventSaveGame.JoinStates JoinState
        {
            get
            {
                if (SaveGame == null)
                {
                    Debug.LogWarning("[PlayEvent] JoinState：活动 '" + (GetID() ?? "<null>")
                                   + "' 没有存档（原版这一跳 NRE）⇒ 返回 NotJoined");
                    return PlayEventSaveGame.JoinStates.NotJoined;
                }
                return SaveGame.JoinState;
            }
        }

        /// <summary>原版 `IPlayEvent.CurrentGamesLost`（接口默认实现，控制流可读）：
        /// `var h = LiveOpsManager.GetHandler&lt;GameModes&gt;(); if (h != null) return h.LossStreak;` 否则 NRE。
        /// 本地：`LiveOpsManager.GetHandler&lt;GameModesHandler&gt;()`；取不到 ⇒ 出声 + 0（原版 NRE）。
        /// ⚠️ 原版的 `GameModes` 型参 = 我们的 `GameModesHandler`（同名族，见 `Shell/GameModesHandler.cs:53`）。</summary>
        public int CurrentGamesLost
        {
            get
            {
                var h = LiveOpsManager.GetHandler<GameModesHandler>();
                if (h == null)
                {
                    Debug.LogWarning("[PlayEvent] CurrentGamesLost：`LiveOpsManager.GetHandler<GameModesHandler>()` 是 null"
                                   + "（原版这一跳 NRE）⇒ 返回 0");
                    return 0;
                }
                return h.LossStreak;
            }
        }

        // ============================================================ ②/③ 分岔（账 557 的那一格 · 追加在类尾）
        // 判据（原版 `PlayEvent<,>.GetEventDeck()` 三支，逐句见上面 `GetEventDeck()` 的 doc）：
        //   ② `if (handler != null) return 全表.FirstOrDefault(b__26_0);`   ← **有 handler 就直接返回（可能是 null）**
        //   ③ `return GameModes.GetActiveEvent<IRankedEvent>().GetEventDeck();`  ← **只在 handler 为 null 时才走**
        // 🔴 改前我们**没有这道闸**：② 扫完没命中就无条件落到 ③ ⇒ 「活动 handler 在、但一副合格卡组都没有」那一档
        //    我们会去问排位 handler 要卡组，而原版返回 **null**（调用方拿到 null 走「没有该模式卡组」那条错误支）。
        //    ⇒ 现在按原版补：`Handler != null` 时 ② 的结果就是最终结果（= 上面那圈 `for` 已扫完的 `null`）。
        // ⚠️ `Handler` 是 `LiveOpsEvent<TSave,TData>.Handler`（`Initialize()` 里那句 `if (Handler == null) return;` 同源），
        //    ⛔ 不是另建一个「有没有排位层」的标志位。
        /// <summary>原版 `GetEventDeck()` 的 ②/③ 分岔（见上方注释；账 557）。</summary>
        string GetEventDeckRankedOrNull(IPlayEventDeckSource src)
        {
            if (Handler != null) return null;          // ② 已扫完没命中 ⇒ 原版就在这里返回 null，**不进 ③**
            return PlayEventRankedDeck.TryGet();       // ③ = `GameModes.GetActiveEvent<IRankedEvent>().GetEventDeck()`
        }
    }

    /// <summary>禁卡表的**写入口**（🆕 2026-10-29 · B96_MENU · 账 菜单/外壳:58 ①）。
    /// <para>原版由 `BannedCardsSO`（资产引用）+ `OnAfterRead` 填；我们**没有那个 SO**，所以给活动装载侧一个
    /// 显式写入口：灌 id 表 → 立刻重建缓存（等价于「读到一个带 `bannedCards` 的事件后跑 `OnAfterRead`」）。</para>
    /// <para>🔴 **数据来源仍是服务器/活动侧（P3 远端）**，本 build 判据空（`Shell/DeckSelectionPopup.cs:937-942`）⇒
    /// 生产路目前没人调用它；一旦活动线拿到禁卡表就调它。⛔ 别在里面编造 id 表 —— 只接受调用方给的。</para>
    /// <para>⚠️ 建成**扩展类追加在本文件末尾**（不是 `PlayEventData` 类体里）：该文件被账按 `文件:行号` 引用了几十个
    /// 落点（420..976 现读有效），往类体中间插成员会让它们整体漂移（铁律 18·b 零行变异）。</para></summary>
    public static class PlayEventDataExtensions
    {
        /// <summary>= 「读到一个带 `bannedCards` 的事件后跑 `OnAfterRead`」：设 id 表并重建缓存（null ⇒ 清表）。</summary>
        public static void SetBannedCards(this PlayEventData evt, IEnumerable<string> cardIds)
        {
            if (evt == null) return;
            evt.bannedCardIds = cardIds == null ? null : new System.Collections.Generic.List<string>(cardIds);
            evt.OnAfterRead();
        }
    }
}
