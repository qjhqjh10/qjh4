// DraftLocalEvent.cs —— 🔶 轮抽活动的【本地模拟实例】（原版由服务端下发；关服后判据在远端）
//
// ============================ 它解决的问题（三行账同一根）============================
// `资料/完全复刻/账/轮抽.tsv` 里有三行一直卡在 B，卡的都是**同一件事**：
//   · 行 6   `DraftEventBase.Expire(Action onComplete)` —— 方法体逐条在位（`Shell/DraftEventBase.cs:277`），
//            缺的是「**活动到期**」这个**服务端事件时刻**：`Resources/draft_packs.json` 里
//            `endAt` / `endDate` / `expireDate` / `startDate` 各 0 命中 ⇒ 离线 build 永不过期 ⇒ 这条链走不到。
//   · 行 114 `DraftModeMenuPayStateTimed.OnOpen()` —— Timed 付费页族已建（`Shell/DraftModeWindow.cs:4297`
//            `BuildTimedFamily`，gate `:4299`），但 `TimedDraftLocal.EnableForDemo` **生产 0 调用点**
//            ⇒ `TimedDraftLocal.Enabled` 在 player 里永远 false ⇒ Timed 族永不建。
//   · 行 147 `DraftModeWarlordSelectionStateTimed.OnOpen()` —— `specificArmies` 过滤在位（`:4315-4321`），
//            残差只剩**标题幅面** `LiveOpsAssetUtility.GetLabel(eventData, Title)` 的词条
//            （它住在远端事件的 `EventLabels` 里，本地 13 份活动 SO 目录没有 draft）。
// 三行的共同缺口 = **本地没有一份 draft 活动实例**（没有 eventId / 没有到期时刻 / 没有 specificArmies / 没有 Title 词条）。
// 本文件造的就是那一份，并在启动时**出声**说明「这是本地模拟，不是原版下发」。
//
// ============================ 🔶 自己设计的边界（必须与复刻区分开）============================
// 用户 2026-10-28 定：**唯一的自设计门类 = 与服务器有关**（关服后判据在远端）。
// 这条正好落在那门类里 —— 下列每一项**都是我们挑的**，⛔ 不是原版值：
//   · `eventId` / 活动的起止时刻 / `expireDate` —— 原版由 PlayFab/CCD 下发（本地 0 命中）。
//   · `specificArmies[2]` / 两阵营总分 —— 原版 `TimedDraftData`（服务端）+ 排行榜缓存（服务端）。
//   · `Title` 词条 —— 原版来自事件 SO 的 `drawData.EventLabels`；本模拟把它**指向一条既有词条**
//     `Demo/DraftMode/SelectWarlord`（页面本来就印的那句），所以**画面文字没有被编**；
//     「这份事件把 Title 指到哪个词条」这一格是我们挑的。
//   · Premium 价 / 货币键 / 事件图名 —— 沿用 `TimedDraftLocal` 里已有的 🔶 值（`Shell/DraftModeWindow.cs`）。
//   · 数据的**载体**：`Resources/draft_local_event.json`（独立文件）。⛔ **一个字节都不改** `draft_packs.json`
//     —— 那是原版卡包数据。
//
// ============================ 判据（这条链在原版里怎么走）============================
// 原版：服务端 → `LiveOpsHandler.ExpireEvent`（`Shell/LiveOpsHandler.cs:487`，非弹窗支 `:503`
// `newEvent.Expire(handleOnComplete)`）→ `ILiveOps.Expire`（本工程实现 = `DraftEventBase.Expire`）。
// 排期闸 = `LiveOpsHandler.ScheduleEventPopups`（`:373` 起）那一句
// `data.GetState(LiveOpsClock.NowUtc) == LiveOpState.Finished`。
// 本地等价物（本文件）：同一句判据（`Finished`）+ 同一条回调形状
// （`LiveOpsSignals.RaiseEventExpired(id)` 再 `onComplete`，= `ExpireEvent` 里的 `handleOnComplete`），
// 只是**驱动源**从「服务端下发」换成「本机时钟 + 每个 `Tick` 查一次」。
// ⚠️ 有意差异：原版要求事件先被 `QueueEventPopup` 排过（`handlerActions.ContainsKey(id)`）才会走到
// `ExpireEvent`；我们这一份**不经过 `LiveOpsHandler`**（那份 handler 里没有任何 draft 事件、也不在本代理的白名单内），
// 所以直接调 `Expire` —— 这一跳在本文件里如实写清，⛔ 不当成「原版也这样」。
//
// ============================ 谁在用 ============================
// · `Shell/DraftLocalEventPump.cs` —— player 启动时装泵，每秒 `Tick()` 一次（到期时刻到点即走 `Expire` 链）。
// · `Shell/DraftModeWindow.cs` 的 `ApplyTimedWarlordTitle()`（Timed 选将页标题幅面，行 147）。
// · `TimedDraftLocal`（`Shell/DraftModeWindow.cs:4402`）—— 本模拟在装载时调 `EnableForDemo` 并灌入
//   `specificArmies` / 总分 / Premium 价 / 事件图（行 114 要的那个「生产调用点」）。
// · 断言 → `Editor/B98K5_Assert.cs`。
//
// ⚠️ 与 `TimedDraftLocal.Enabled` 的关系：**没有改它的默认值**（仍是 `false`）；本模拟开着时会在装载时
// 调 `EnableForDemo` 把它置真。关掉模拟（`SimulationEnabled = false` + `InvalidateCache()`）就回到
// 「player 里 Timed 族永不建」的旧状态 —— 断言用它做灭自证。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>🔶 轮抽活动的本地模拟实例（服务器已关服 ⇒ 判据在远端；见文件头「自己设计的边界」）。
    /// <para>它做三件事：① 造一份 `DraftEventBase&lt;DraftEventSave, DraftEventData&gt;` 的具体实例（原版那两个子类的替身）；
    /// ② 按本机时钟判到期并走 `Expire` 链（= 原版「服务端到期事件」的等价物）；③ 把 Timed 族要的那几格灌进
    /// <see cref="TimedDraftLocal"/>，并提供选将页标题（`EventLabels[Title]`）的取值口。</para></summary>
    public static class DraftLocalEvent
    {
        // ============================================================ 数据 / 开关
        /// <summary>`Assets/CardPresentation/Resources/draft_local_event.json`。
        /// ⛔ 与 `draft_packs.json` 分开（后者是原版卡包数据，本模拟一个字节都不动）。</summary>
        public const string ResourcePath = "draft_local_event";

        /// <summary>本文件认得的模拟表版本。</summary>
        public const int SupportedVersion = 1;

        /// <summary>模拟总开关（**默认 true** = player 里活动实例存在）。
        /// <para>关掉它 = 回到「本地没有任何 draft 活动实例」的旧状态（到期链不跑、Timed 族不建）。
        /// ⚠️ 它 **不是** 「关掉轮抽模式」—— 模式本身照常能玩。</para>
        /// <para>🔴 写成属性（不是裸字段）是为了**开关一变就丢缓存**：否则「关着装了一次、再打开」会命中
        /// `_loaded = true` 而**不再建活动实例**（静默、且第 1 眼看不出来）。</para></summary>
        public static bool SimulationEnabled
        {
            get { return _simEnabled; }
            set
            {
                if (_simEnabled == value) return;
                _simEnabled = value;
                InvalidateCache();       // 换状态 ⇒ 下一次访问重新装载（保证「关掉 ⇒ 真的没有」）
            }
        }
        static bool _simEnabled = true;

        /// <summary>模拟数据（= 一份「服务端下发的 draft 事件」的替身）。字段含义见各字段注释。</summary>
        [Serializable]
        public class Spec
        {
            public int version = SupportedVersion;
            /// <summary>🔶 这份数据是我们造的（给人看的说明，不参与逻辑）。</summary>
            public string note;
            /// <summary>🔶 eventId（原版由服务端下发；这次轮抽的 `DraftEventData.eventId`）。</summary>
            public string eventId = "LocalSimDraftEvent";
            /// <summary>🔶 这一场是 **Timed 变体**（原版 `TimedDraftEvent`）还是 Demo 变体（原版 `DraftEvent`）。
            /// `true` ⇒ 装载时开 `TimedDraftLocal`（Timed 付费页族 / 选将过滤 / 标题幅面）。</summary>
            public bool timed = true;
            /// <summary>🔶 事件 `EventLabels[Title]` 指向的词条键。默认指向页面本来就印的那一条
            /// （`Demo/DraftMode/SelectWarlord`）⇒ **画面文字没有被编**。</summary>
            public string titleTerm = "Demo/DraftMode/SelectWarlord";
            /// <summary>🔶 词条取不到时的兜底文案（默认空 = 不兜底，保留页面既有标题）。</summary>
            public string titleLiteral;
            /// <summary>🔶 原版 `TimedDraftData.specificArmies[2]`（服务端）。Timed 选将页按它过滤卡池。</summary>
            public string[] specificArmies = { "Ultramarines", "Goff" };
            /// <summary>🔶 两阵营总分（原版 `LeaderboardManager.GetCachedLeaderboard(...).GetTotalByArmy`，服务端）。</summary>
            public int[] armyTotals = { 0, 0 };
            /// <summary>🔶 事件图（原版 `drawData.EventAssets`）。取不到就出声不画。</summary>
            public string eventImage = "40K_Chat_icon_Alliance_v2";
            /// <summary>🔶 Premium 报名价（原版在服务端）。</summary>
            public int premiumCost = 150;
            /// <summary>🔶 Premium 10x 报名价。</summary>
            public int premiumTenCost = 1500;
            /// <summary>🔶 扣哪种货币（`Wallet` 的键）。</summary>
            public string premiumCurrency = "40k_topmarquee_currency_crystal";
            /// <summary>🔶 开始时刻 = **本进程启动时刻 + 这个偏移（分钟）**。负 = 已经开始了。
            /// 🔴 为什么用「相对启动」而不是写死绝对时刻：写死的话一旦过期，每次启动都立刻 Expire，
            /// 玩家一开游戏就被关窗。相对偏移是这份模拟唯一稳的锚。</summary>
            public int startOffsetMinutes = -60;
            /// <summary>🔶 结束时刻 = 启动 + 这个偏移（分钟）。过了它 `GetState = Finished`
            /// ⇒ 就是原版「活动到期」那一刻（`LiveOpsHandler.ScheduleEventPopups` 的过期支判据）。</summary>
            public int endOffsetMinutes = 180;
            /// <summary>🔶 `dates.expirable`（原版字段）：`true` ⇒ 用 `expireDate`（`FinalDate`）当最终时刻。</summary>
            public bool expirable = true;
            /// <summary>🔶 最终时刻 = 启动 + 这个偏移（分钟）。过了它 `GetState = Expired`（原版这时事件已被摘掉，不再走过期弹窗）。</summary>
            public int expireOffsetMinutes = 240;
            /// <summary>🔶 **演示/自检旋钮**：&gt;0 ⇒ 启动后这么多秒**强制**走一次「活动到期」链（不等 `endOffsetMinutes`）。
            /// 默认 0 = 不强制（正常按 `endOffsetMinutes` 那个时刻）。⛔ 它不是原版概念。</summary>
            public int forceExpireAfterSeconds = 0;
        }

        // ============================================================ 具体事件类（原版两个子类的替身）
        /// <summary>原版 `DraftEvent` / `TimedDraftEvent` 的本地替身：只补原版抽象成员
        /// `PlayModes EventPlayMode`（判据 = `DraftEventBase.get_EventPlayMode` 真体全文 `return 7`
        /// ⇒ `PlayModes.ClosedDeck`，见账 `轮抽:169`）。其余全部继承 <see cref="DraftEventBase{TS,TD}"/>。
        /// <para>⚠️ 非 MonoBehaviour ⇒ 不需要「类名 == 文件名」的脚本资产（嵌在静态类里只是省一个文件）。</para></summary>
        public sealed class Body : DraftEventBase<DraftEventSave, DraftEventData>
        {
            /// <summary>原版 `DraftEventBase&lt;TS,TD&gt;.EventPlayMode` = `PlayModes.ClosedDeck`(7)。</summary>
            public override PlayModes EventPlayMode { get { return PlayModes.ClosedDeck; } }
        }

        // ============================================================ 状态
        static bool _loaded;
        static Spec _spec;
        static Body _body;
        static LiveOpsLocalEvent _labelCarrier;
        static DateTime _bootUtc;

        /// <summary>进程启动（= 本模拟装载）那一刻的 UTC。所有「相对启动」的偏移都锚在它上面。</summary>
        public static DateTime BootUtc { get { EnsureLoaded(); return _bootUtc; } }

        /// <summary>模拟数据（读不到 = 用代码里的默认值 + 出声；⛔ 不静默）。</summary>
        public static Spec Current { get { EnsureLoaded(); return _spec; } }

        /// <summary>活动实例（原版 `ILiveOps` 那一格）。</summary>
        public static Body Event { get { EnsureLoaded(); return _body; } }

        /// <summary>这次「活动到期」已经走过一次（原版：事件被摘掉后不再重复过期）。</summary>
        public static bool ExpireFired { get; private set; }

        /// <summary>`Expire` 被调了几次（断言读口；正常最多 1）。</summary>
        public static int ExpireCount { get; private set; }

        /// <summary>标题幅面最后取到的**词条键**（断言读口；`null` = 走的兜底/空态）。</summary>
        public static string LastTitleTerm { get; private set; }

        /// <summary>标题幅面最后取到的**显示文字**（断言读口）。</summary>
        public static string LastTitleText { get; private set; }

        /// <summary>当前状态（原版 `LiveOpsEventData.GetState(now)`，六档状态机的本地落地）。</summary>
        public static LiveOpState State
        {
            get
            {
                EnsureLoaded();
                var data = _body == null ? null : _body.GetBaseData();
                return data == null ? LiveOpState.NotAvailable : data.GetState(LiveOpsClock.NowUtc);
            }
        }

        /// <summary>Timed 族此刻该不该建（= 模拟事件存在且声明自己是 Timed 变体；`TimedDraftLocal.Enabled` 另算）。
        /// 只给断言/诊断用 —— 生产侧的闸仍是 `TimedDraftLocal.Enabled`（装载时由本类置真）。</summary>
        public static bool TimedFamilyActive
        {
            get { EnsureLoaded(); return SimulationEnabled && _spec != null && _spec.timed; }
        }

        /// <summary>本模拟的起止时刻（毫秒，UTC）。⚠️ 全是我们按 `startOffsetMinutes` 等偏移算出来的（🔶）。</summary>
        public static long StartMs { get { EnsureLoaded(); return _startMs; } }
        public static long EndMs { get { EnsureLoaded(); return _endMs; } }
        public static long ExpireMs { get { EnsureLoaded(); return _expireMs; } }
        static long _startMs, _endMs, _expireMs;

        // ============================================================ 装载
        /// <summary>装一次（幂等）。关掉模拟时**什么都不建** —— 只打一条说明（不静默）。</summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            if (!SimulationEnabled)
            {
                Debug.Log("[Draft] 🔶 本地模拟 draft 活动：**已关闭**（`DraftLocalEvent.SimulationEnabled = false`）"
                        + " ⇒ 回到「本地没有任何 draft 活动实例」的旧状态：到期链不跑、Timed 族不建");
                return;
            }

            _bootUtc = DateTime.UtcNow;
            _spec = LoadSpec();
            if (_spec == null)
            {
                Debug.LogWarning("[Draft] 🔶 本地模拟 draft 活动：数据读不到 ⇒ 用代码里的默认 `Spec`（⛔ 不静默）");
                _spec = new Spec();
            }
            _startMs = ToMs(_bootUtc.AddMinutes(_spec.startOffsetMinutes));
            _endMs = ToMs(_bootUtc.AddMinutes(_spec.endOffsetMinutes));
            _expireMs = ToMs(_bootUtc.AddMinutes(_spec.expireOffsetMinutes));
            if (_endMs <= _startMs)
                Debug.LogError("[Draft] 🔶 本地模拟 draft 活动：`endOffsetMinutes`(" + _spec.endOffsetMinutes
                             + ") 不大于 `startOffsetMinutes`(" + _spec.startOffsetMinutes + ") ⇒ 这份数据是坏的"
                             + "（`GetState` 会立刻 Finished）。改 `Resources/draft_local_event.json`");

            BuildBody();
            ApplyToTimedLocal();

            Debug.Log("[Draft] 🔶 本地模拟 draft 活动「" + _spec.eventId + "」："
                    + "开始 " + _bootUtc.AddMinutes(_spec.startOffsetMinutes).ToString("yyyy-MM-dd HH:mm")
                    + "Z / 到期 " + _bootUtc.AddMinutes(_spec.endOffsetMinutes).ToString("yyyy-MM-dd HH:mm") + "Z"
                    + (_spec.expirable ? " / 最终 " + _bootUtc.AddMinutes(_spec.expireOffsetMinutes).ToString("yyyy-MM-dd HH:mm") + "Z" : "")
                    + "；当前状态 = " + State
                    + " —— **这份活动是我们本地造的，不是原版服务端下发**（服务器已关服 ⇒ 判据在远端；"
                    + "数据见 `Resources/" + ResourcePath + ".json`）");
        }

        static Spec LoadSpec()
        {
            var ta = Resources.Load<TextAsset>(ResourcePath);
            if (ta == null)
            {
                Debug.LogWarning("[Draft] 读不到 `Resources/" + ResourcePath + ".json` —— 🔶 本地模拟的活动数据缺文件"
                               + "（会用代码里的默认值顶；⛔ 不影响原版卡包表 `draft_packs.json`）");
                return null;
            }
            Spec s;
            try { s = JsonUtility.FromJson<Spec>(ta.text); }
            catch (Exception e)
            {
                Debug.LogError("[Draft] `" + ResourcePath + ".json` 解析失败：" + e.Message + " ⇒ 用代码默认值");
                return null;
            }
            if (s == null)
            {
                Debug.LogError("[Draft] `" + ResourcePath + ".json` 解析成 null ⇒ 用代码默认值");
                return null;
            }
            if (s.version != SupportedVersion)
                Debug.LogWarning("[Draft] `" + ResourcePath + ".json` 的 version=" + s.version
                               + " ≠ " + SupportedVersion + "（本类不认的新列会被 `JsonUtility` 静默丢掉）");
            if (string.IsNullOrEmpty(s.eventId))
                Debug.LogError("[Draft] `" + ResourcePath + ".json` 没有 `eventId` —— 原版 `GetID()` 会返空 ⇒ 建议补上");
            return s;
        }

        static void BuildBody()
        {
            var data = new DraftEventData
            {
                eventId = _spec.eventId,
                enabled = true,
                uploadToLive = true,
                activationType = LiveOpsActivationType.OnDate,   // ★ 走日期驱动的六档状态机（不是 Permanent/OneTime 短路）
                dates = new LiveOpsEventTimeData
                {
                    startDate = _startMs,
                    endDate = _endMs,
                    expirable = _spec.expirable,
                    expireDate = _expireMs,
                },
            };
            var save = new DraftEventSave
            {
                eventId = _spec.eventId,
                isPremium = false,       // 🔶 模拟事件按**免费档**（付费档要服务器收奖励；用户 2026-10-24「轮抽不做奖励」）
            };
            _body = new Body();
            _body.LoadEventData(data, save, null);   // handler = null：本模拟**不经过** `LiveOpsHandler`（见文件头「有意差异」）
        }

        static void ApplyToTimedLocal()
        {
            if (_spec == null) return;
            if (!_spec.timed)
            {
                Debug.Log("[Draft] 🔶 本地模拟 draft 活动：Demo 变体（`timed = false`）⇒ **不开** Timed 付费页族"
                        + "（原版这两个变体由服务端事件类型决定）");
                return;
            }
            var a = _spec.specificArmies;
            if (a == null || a.Length < 2 || string.IsNullOrEmpty(a[0]) || string.IsNullOrEmpty(a[1]))
            {
                Debug.LogWarning("[Draft] 🔶 本地模拟 draft 活动：`specificArmies` 不足两格 ⇒ 不开 Timed 族"
                               + "（原版 `TimedDraftData.specificArmies[2]` 是服务端值）");
                return;
            }
            TimedDraftLocal.EnableForDemo(a[0], a[1]);      // ★ 行 114 缺的那个「生产调用点」
            if (_spec.armyTotals != null && _spec.armyTotals.Length >= 2)
                TimedDraftLocal.Totals = new[] { _spec.armyTotals[0], _spec.armyTotals[1] };
            if (_spec.premiumCost > 0) TimedDraftLocal.PremiumCost = _spec.premiumCost;
            if (_spec.premiumTenCost > 0) TimedDraftLocal.PremiumTenCost = _spec.premiumTenCost;
            if (!string.IsNullOrEmpty(_spec.premiumCurrency)) TimedDraftLocal.PremiumCurrency = _spec.premiumCurrency;
            if (!string.IsNullOrEmpty(_spec.eventImage)) TimedDraftLocal.EventImage = _spec.eventImage;
            Debug.Log("[Draft] 🔶 本地模拟 draft 活动是 **Timed 变体** ⇒ 开 Timed 族（原版由服务端 `TimedDraftEvent` 决定）："
                    + "specificArmies = [" + a[0] + ", " + a[1] + "]"
                    + " · 总分 = [" + (_spec.armyTotals != null && _spec.armyTotals.Length >= 2 ? _spec.armyTotals[0] : 0)
                    + ", " + (_spec.armyTotals != null && _spec.armyTotals.Length >= 2 ? _spec.armyTotals[1] : 0) + "]"
                    + "（排行榜真值在服务端，本地 0）");
        }

        // ============================================================ 到期（= 原版「服务端到期事件」的等价物）
        /// <summary>每秒调一次（<see cref="DraftLocalEventPump"/>）。判据与他 `LiveOpsHandler.ScheduleEventPopups`
        /// 的过期支**同一句**：`data.GetState(NowUtc) == LiveOpState.Finished`。
        /// <para>`Expired`（`FinalDate` 也过了）⇒ 原版这时事件已被摘掉、**不走**过期弹窗 ⇒ 这里只出声并停表。</para></summary>
        public static void Tick()
        {
            if (!SimulationEnabled) return;
            EnsureLoaded();
            if (_spec == null || _body == null || ExpireFired) return;

            var now = LiveOpsClock.NowUtc;
            if (_spec.forceExpireAfterSeconds > 0 && now >= _bootUtc.AddSeconds(_spec.forceExpireAfterSeconds))
            {
                FireExpire("演示旋钮 `forceExpireAfterSeconds = " + _spec.forceExpireAfterSeconds + "` 到点（🔶 非原版概念）");
                return;
            }

            var data = _body.GetBaseData();
            if (data == null) return;
            var st = data.GetState(now);
            if (st == LiveOpState.Finished)
            {
                FireExpire("事件状态 = Finished（`endDate` 已过、`FinalDate` 未到）= 原版 `LiveOpsHandler.ScheduleEventPopups` 的过期支判据");
                return;
            }
            if (st == LiveOpState.Expired)
            {
                ExpireFired = true;      // 只在 `Expired` 档停机（原版这时事件已被摘掉）
                Debug.Log("[Draft] 🔶 本地模拟 draft 活动已 `Expired`（`FinalDate` 也过了）⇒ 原版此时事件已从 liveops 列表摘掉，"
                        + "不再走过期弹窗；本模拟不再重复检查");
            }
        }

        /// <summary>立刻走一次「活动到期」（演示 / 自检口；已走过就不再走）。返回是否真的触发了。</summary>
        public static bool ForceExpire(string reason)
        {
            EnsureLoaded();
            if (!SimulationEnabled || _body == null || ExpireFired) return false;
            FireExpire(string.IsNullOrEmpty(reason) ? "手动触发（演示 / 自检）" : reason);
            return true;
        }

        static void FireExpire(string reason)
        {
            ExpireFired = true;
            ExpireCount++;
            string id = _body == null ? null : _body.GetID();
            Debug.Log("[Draft] 🔶 本地模拟「活动到期」时刻到了 —— " + reason
                    + " ⇒ 走 `Expire` 链（原版：服务端到期 → `LiveOpsHandler.ExpireEvent` → `ILiveOps.Expire`）");
            // 回调形状 = `LiveOpsHandler.ExpireEvent` 里的 `handleOnComplete`（非弹窗支 `:503`）
            Action handleOnComplete = () =>
            {
                LiveOpsSignals.RaiseEventExpired(id);
                Debug.Log("[Draft] 到期完成回调（= `ExpireEvent` 的 `handleOnComplete`："
                        + "`Signal.Raise(new EventExpiredSignal(id))` + `onComplete`）—— id='" + (id ?? "<null>") + "'");
            };
            _body.Expire(handleOnComplete);
        }

        // ============================================================ 标题幅面（行 147 的残差）
        /// <summary>Timed 选将页的标题文字（原版 `title = LiveOpsAssetUtility.GetLabel(eventData, Title)`，
        /// 判据 `DraftModeWarlordSelectionStateTimed.OnOpen`）。取不到 = 空串 + 出声（⛔ 不编词条、⛔ 不印键名）。
        /// <para>⚠️ 取不到时**返回空串**（= 保留页面既有标题），调用方据此决定动不动那颗标签。</para></summary>
        public static string TitleText()
        {
            EnsureLoaded();
            string why;
            var carrier = LabelCarrier();
            if (carrier != null)
            {
                var label = LiveOpsAssetUtility.GetEventLabel(carrier, LiveOpsLabelRef.Title);   // ★ 原版那一跳（取不到它会出声）
                if (label != null && !string.IsNullOrEmpty(label.Term))
                {
                    string text = LiveOpsLocalCatalog.FillArgs(label.Term, LiveOpsLocalCatalog.ResolveArgs(label.Args));
                    if (!string.IsNullOrEmpty(text))
                    {
                        LastTitleTerm = label.Term; LastTitleText = text;
                        return text;
                    }
                    why = "词条 `" + label.Term + "` 在本地三处表里都是空";
                }
                else why = "模拟事件的 `EventLabels` 里没有 Title（`titleTerm` 为空？）";
            }
            else why = "模拟事件没建出来（`SimulationEnabled = false`？）";

            if (_spec != null && !string.IsNullOrEmpty(_spec.titleLiteral))
            {
                LastTitleTerm = null; LastTitleText = _spec.titleLiteral;
                Debug.LogWarning("[Draft] 标题幅面：事件词条取不到（" + why + "）⇒ 用 `spec.titleLiteral` 兜底"
                               + "（🔶 本地模拟文案，⛔ 不是原版词条）");
                return _spec.titleLiteral;
            }
            LastTitleTerm = null; LastTitleText = null;
            Debug.LogWarning("[Draft] 标题幅面：事件词条取不到（" + why + "）且没有兜底 ⇒ 保留页面既有标题"
                           + "（原版 `GetLabel(Title)` 取不到时也是空态；⛔ 不编词条）");
            return "";
        }

        /// <summary>模拟事件在**标签系统**里的载体（`LiveOpsLocalEvent` 只为承载 `EventLabels` ——
        /// 原版这层是事件 SO 自己的 `drawData.EventLabels`）。
        /// ⚠️ `Kind` 只能填既有值（`LiveOpsLocalKind` 里**没有 draft 这一档**，本模拟用 `Parent` 占位并在此写明）。</summary>
        static LiveOpsLocalEvent LabelCarrier()
        {
            if (_spec == null || string.IsNullOrEmpty(_spec.titleTerm)) return null;
            if (_labelCarrier != null) return _labelCarrier;
            _labelCarrier = new LiveOpsLocalEvent
            {
                EventId = _spec.eventId,
                SoName = "🔶 本地模拟（无 SO）",
                Kind = LiveOpsLocalKind.Parent,     // ⚠️ 占位：`LiveOpsLocalKind` 没有 draft 这一档
                Enabled = true,
                UploadToLive = true,
                StartMs = _startMs,
                EndMs = _endMs,
                Expirable = _spec.expirable,
                ExpireMs = _expireMs,
                Labels = new[]
                {
                    new LiveOpsLocalLabel { Key = (int)LiveOpsLabelRef.Title, Term = _spec.titleTerm },
                },
            };
            return _labelCarrier;
        }

        // ============================================================ 自检口
        /// <summary>清掉缓存并重新装（编辑器里改完 json / 断言换一份 `Spec` 时用）。**不改** `TimedDraftLocal` 的现值。</summary>
        public static void InvalidateCache()
        {
            _loaded = false; _spec = null; _body = null; _labelCarrier = null;
            ExpireFired = false; ExpireCount = 0;
            LastTitleTerm = null; LastTitleText = null;
            _startMs = _endMs = _expireMs = 0;
        }

        /// <summary>自检收尾：清缓存 + 打开开关 + 重新装一遍（`TimedDraftLocal` 由调用方自己存/还原）。</summary>
        public static void ResetForTest()
        {
            SimulationEnabled = true;
            InvalidateCache();
        }

        static long ToMs(DateTime utc) { return LiveOpsEventTimeData.ToMs(utc); }
    }
}
