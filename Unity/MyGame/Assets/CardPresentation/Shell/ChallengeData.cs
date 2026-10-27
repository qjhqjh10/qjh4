// ChallengeData.cs — 「挑战」引擎的**本地数据模型**（🆕 2026-10-27 · 执行代理 `B88_E3` · 账 `B22`）
//
// 它是谁的原版等价物：
//   · `ChallengeManager` / `ChallengeLogMgr`（`Everguild.UI`）里**本地可做**的那一半 ——
//     待处理练习挑战队列、挑战定义与进度、四类计数（`TroopKilled` / `LogHeal` / `LogAddEffect` / 督军掉血）。
//   · `MissionChallengeProgress.GetChallengeRefillTime` 的**补时**语义（「这份领完 → 下一次补时」）。
//   另一半（PlayFab 找人 / 私聊 Practice / 在线查询 / 决斗状态机）**是服务器链**，⛔ 不在本文件：
//     账里那 12 条 `ChallengeManager.*` 的落点分别在 `Shell/DuelPopupWindow.cs` 与 `Shell/ChatPanel.cs`
//     （本批**黑名单**），且它们全部以「服务端有对手/有私聊」为前提 ⇒ 归 **P3 服务器**那一档（见报告）。
//
// 🔴 **挑战内容只能自建**（调度台 2026-10-27 交办）：`assets_full` 90 个 bundle 全量搜
//    `ChallengeList` / `MissionChallenge` / `MissionsConfig` = **0 个资产**（只有 `MonoScript` 类定义）
//    ⇒ 原版的挑战清单**本地不存在**。下面 `Definitions` 里每一条都按铁律 18 打 `🔶 自己设计`。
//
// 与既有件的关系（⛔ 别再建第二份）：
//   · 红点/通知总线 = `UiBadgeNotificationManager`（`Shell/BadgeNotifications.cs`）——
//     本文件只**登记一个 provider**（`ChallengeMatchNotificationProvider`），不另建总线。
//   · 每日刷新倒计时 = `DailyData.NextResetAt` / `DailyData.ResetIn()` —— 本文件**不重算日界**，
//     挑战补时用的是**自己的**循环节奏（`RefillIntervalSeconds`，原版是服务端下发，本地只能自建）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    /// <summary>挑战的**计数口径**（= 原版 `ChallengeLogMgr` 的四条记录口 ＋ 生存回合）。
    /// ⚠️ 这张枚举是**我们自建**的（原版挑战种类来自服务端 `ChallengeList`，本地 0 资产）。</summary>
    public enum ChallengeKind
    {
        /// <summary>`ChallengeLogMgr.TroopKilled`：击杀的部队数。</summary>
        KillTroops = 0,
        /// <summary>`ChallengeLogMgr.LogHeal`：累计治疗量。</summary>
        HealAmount = 1,
        /// <summary>`ChallengeLogMgr.LogAddEffect`：施加的效果条数。</summary>
        AddEffects = 2,
        /// <summary>`BattleEventsController.CheckHealth` 的敌方那一半：对敌方督军造成的伤害。</summary>
        DamageEnemyWarlord = 3,
        /// <summary>生存回合数（「活过 N 回合」那一类）。</summary>
        SurviveTurns = 4,
    }

    /// <summary>一条挑战定义。
    /// 🔶 自己设计（非原版）：**整个类型都是我们挑的** —— 原版 `MissionChallenge` 由服务端下发
    /// （`assets_full` 90 个 bundle 全量搜 `ChallengeList`/`MissionChallenge`/`MissionsConfig` = **0 个资产**，
    /// 只有类定义），本机拿不到任何一条真实挑战 ⇒ 字段/数值/文案全部自定 · 调度台 2026-10-27 交办（账 B22）。</summary>
    public sealed class ChallengeDefinition
    {
        public string Id;
        public ChallengeKind Kind;
        public int Target;
        /// <summary>完成奖励的经验（原版由服务端给 `RewardInfo`；我们只留一个数，⛔ 不假装知道它的形状）。</summary>
        public int Xp;
        public string NameZh;
    }

    /// <summary>一条**待处理的练习挑战**（= 原版 `challengeManager.challenges` 里的一条）。
    /// <para>字段照原版确认框要印的东西取（`ChallengeButton.OnButtonClick` 的
    /// `Demo/FriendsMenu/ReceiveChallenge` 词条带「挑战名 + 模式词条」）⇒ 名字 + 模式。</para>
    /// <para>⚠️ 今天**没有生产者**：原版由服务端推下来，我们的 P2P 只有「地址进房」、没有邀约协议
    /// ⇒ 这个队列靠 `AddPending(...)` 手动驱动（测试 / 将来的 P2P 邀约）。**如实记，不是「已接」**。</para></summary>
    public sealed class PracticeChallenge
    {
        public string PlayerName;
        /// <summary>模式词条（原版 `Demo/...` 的 mode 串）；我们只留 key。</summary>
        public string ModeKey;
        /// <summary>是否对方先手（原版 `StartChallengeWithUser(..., playFirst, ...)`）。</summary>
        public bool PlayerFirst;
    }

    /// <summary>「挑战」的本地状态正本（唯一一份；⛔ 别在别处再存一份计数）。</summary>
    public static class ChallengeData
    {
        // ============================================================ ① 待处理练习挑战（`ChallengeButton` 的显隐判据）

        static readonly List<PracticeChallenge> _pending = new List<PracticeChallenge>();

        /// <summary>原版 `challengeManager.challenges`（**不是** `PendingPracticeMatches` 那个别名字段）。
        /// 判据 → 账 9 的备注：「显隐判据 = `PendingPracticeMatches.Count > 0`（不是 `challengeManager.challenges`）」。</summary>
        public static IReadOnlyList<PracticeChallenge> PendingPracticeMatches { get { return _pending; } }

        public static int PendingCount { get { return _pending.Count; } }

        /// <summary>收到一条练习挑战（原版由服务端推；本地 = P2P 邀约或测试）。
        /// 落队之后**必须**让红点重算 —— 走既有总线，⛔ 不另建。</summary>
        public static void AddPending(PracticeChallenge c)
        {
            if (c == null) return;
            _pending.Add(c);
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
        }

        /// <summary>撤掉一条（`CancelPracticeMatchAndRefresh` / `OnRemoveFriendDuel` 的本地那一半）。</summary>
        public static bool RemovePending(string playerName)
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                if (!string.Equals(_pending[i].PlayerName, playerName, StringComparison.Ordinal)) continue;
                _pending.RemoveAt(i);
                UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
                return true;
            }
            return false;
        }

        public static void ClearPending()
        {
            if (_pending.Count == 0) return;
            _pending.Clear();
            UiBadgeNotificationManager.Refresh(typeof(ChallengeMatch));
        }

        // ============================================================ ② 挑战清单（自建）

        /// <summary>🔶 自己设计（非原版）：清单与目标值**全是我们挑的** —— 原版 `ChallengeList` 由服务端下发、
        /// 本地 0 资产（判据见文件头）· 调度台 2026-10-27 交办（账 B22）。
        /// 逐条都对准 `ChallengeKind` 的一个真实计数口（⛔ 不是凭空摆一个数）。</summary>
        public static readonly IReadOnlyList<ChallengeDefinition> Definitions = new[]
        {
            new ChallengeDefinition { Id = "chal-kill-3",  Kind = ChallengeKind.KillTroops,         Target = 3, Xp = 100, NameZh = "击杀 3 个部队" },
            new ChallengeDefinition { Id = "chal-heal-5",  Kind = ChallengeKind.HealAmount,         Target = 5, Xp = 100, NameZh = "累计治疗 5 点" },
            new ChallengeDefinition { Id = "chal-fx-4",    Kind = ChallengeKind.AddEffects,         Target = 4, Xp = 100, NameZh = "施加 4 个效果" },
            new ChallengeDefinition { Id = "chal-dmg-10",  Kind = ChallengeKind.DamageEnemyWarlord,  Target = 10, Xp = 150, NameZh = "对敌方战将造成 10 点伤害" },
            new ChallengeDefinition { Id = "chal-turn-5",  Kind = ChallengeKind.SurviveTurns,        Target = 5, Xp = 100, NameZh = "活过 5 个回合" },
        };

        // ============================================================ ③ 四类计数（`ChallengeLogMgr` 的等价物）

        public static int TroopsKilled;
        public static int HealAmount;
        public static int EffectsAdded;
        public static int DamageEnemyWarlord;
        public static int SurviveTurns;

        /// <summary>按种类读当前计数值（挑战进度就看这一个口）。</summary>
        public static int CountOf(ChallengeKind k)
        {
            switch (k)
            {
                case ChallengeKind.KillTroops: return TroopsKilled;
                case ChallengeKind.HealAmount: return HealAmount;
                case ChallengeKind.AddEffects: return EffectsAdded;
                case ChallengeKind.DamageEnemyWarlord: return DamageEnemyWarlord;
                case ChallengeKind.SurviveTurns: return SurviveTurns;
                default: return 0;
            }
        }

        /// <summary>某条挑战的进度（0..Target）与是否完成。</summary>
        public static bool IsComplete(ChallengeDefinition d) { return d != null && CountOf(d.Kind) >= d.Target; }

        // ============================================================ ④ 补时（原版 `GetChallengeRefillTime` 的循环那一半）

        /// <summary>🔶 自己设计（非原版）：**补时间隔**—— 原版每份挑战的补时由服务端给
        /// （`MissionChallengeProgress.GetChallengeRefillTime` 读服务端时间戳），本地无判据 ⇒ 取 4 小时
        /// （与「每日 3 条任务」的节奏同量级，纯本地节奏）· 调度台 2026-10-27 交办（账 B22）。</summary>
        public const int RefillIntervalSeconds = 4 * 3600;

        static DateTime _refillBase = DateTime.MinValue;

        /// <summary>离下一次补时还有多少秒（`0` = 现在就该补）。⚠️ 原版那份 `.c` 我们读到的是
        /// 「读服务端时间戳 → 做差」，我们**没有服务端** ⇒ 用本地节奏对齐**语义**（可循环），
        /// ⛔ 不假装它是原版的数值。</summary>
        public static int GetChallengeRefillTime(DateTime now)
        {
            if (_refillBase == DateTime.MinValue) return 0;
            double used = (now - _refillBase).TotalSeconds;
            if (used < 0) return 0;
            int left = RefillIntervalSeconds - (int)(used % RefillIntervalSeconds);
            return left == RefillIntervalSeconds ? 0 : left;
        }

        /// <summary>领完/用掉一份 ⇒ 起算新的一轮（原版 loopable 语义）。</summary>
        public static void MarkRefillConsumed(DateTime now) { _refillBase = now; }

        // ============================================================ ⑤ 红点 provider（复用既有总线）

        /// <summary>「挑战」钮的红点源（原版 `ChallengeButton.Awake` 里 `new ChallengeMatch()` 那一条）：
        /// **每条待处理挑战发一条** `ChallengeMatch`（零字段 record，见 `BadgeNotifications.challenge` 段）。</summary>
        public sealed class ChallengeMatchNotificationProvider : INotificationProvider
        {
            public Type NotificationType { get { return typeof(ChallengeMatch); } }
            public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }
            public IEnumerable<BadgeNotification> CheckNotification()
            {
                for (int i = 0; i < _pending.Count; i++) yield return new ChallengeMatch();
            }
        }

        static ChallengeMatchNotificationProvider _provider;

        /// <summary>挂进徽标总线（幂等；`UiBadgeNotificationManager` 那侧是 `Dictionary` 去重不保证，
        /// 所以这里自己留一份引用防止重复登记）。</summary>
        public static void RegisterProviders()
        {
            if (_provider != null) return;
            _provider = new ChallengeMatchNotificationProvider();
            UiBadgeNotificationManager.Register(_provider);
        }

        public static void UnregisterProviders()
        {
            if (_provider == null) return;
            UiBadgeNotificationManager.Unregister(_provider);
            _provider = null;
        }

        // ============================================================ ⑥ 复位

        /// <summary>把整份挑战状态清干净（自检 / 换局用）。⚠️ 生产路径不调。</summary>
        public static void Reset()
        {
            _pending.Clear();
            TroopsKilled = 0; HealAmount = 0; EffectsAdded = 0;
            DamageEnemyWarlord = 0; SurviveTurns = 0;
            _refillBase = DateTime.MinValue;
        }
    }
}
