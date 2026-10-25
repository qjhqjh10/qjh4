// LiveOpsHandler.cs —— **活动调度器**（原版 `LiveOpsHandler<TEvent>` + `ILiveOpsHandler` 的等价实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：`资料/完全复刻/判据/泛型与启动链.md` §三 #1「活动调度器」+ §四 `LiveOpsHandler<TEvent>` 的逐方法表；
//   `资料/完全复刻/判据/日常与活动.md` §二 Q1（`blockedEvents` / `sharedData` **不能后补**）。
// 原版真身（反编译，机器可读）：
//   · `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LiveOpsHandler.cs`（方法体可读的那部分逐条对照）
//   · `d:/2/tools/decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`（**三参入口的真身**）
//   · `…/LiveOpsHandler._RefreshHandlerLocally_d__27_*.c` · `…/LiveOpsHandler._RefreshHandler_d__26_*.c`
//   · `…/ILiveOpsHandler.cs`（接口成员顺序与签名）
// 字段表（偏移逐条对上反编译里的 `this + 0xNN` 读法，见每个字段的注释）——
//   `events`(+0x20) · `blockedEvents`(+0x28) · `shared`(+0x30) · `handlerActions`(+0x38) ·
//   `EventsRefreshed`(+0x40) · `OnBeforeRefresh`(+0x48) · `scheduledRefresh`(+0x50) · `Manager`(+0x58) ·
//   `handlerCache`(+0x60) · `refreshSemaphore`(+0x68) · `RefreshCount`(+0x70)
//
// ⚠️ **与原版的三处如实差异**（⛔ 不是「已经一样」）：
//   ① 原版是 `LiveOpsHandler<TEvent> : MonoBehaviour`（hander 是**挂在 prefab 上的组件**，
//      `LiveOpsManager.HandlerKeys` 靠 `GetComponentsInChildren<ILiveOpsHandler>()` 找它们）。
//      我们是**纯 C# 类**（工程里没有那种 prefab，也没接 `SceneInitializer`）⇒ 由 `LiveOpsManager` 用注册表持有。
//   ② 原版刷新时刻靠 `SingletonBehaviour<ActionScheduler>.Instance.Schedule(...)`（Unity 时间驱动）；
//      我们是 <see cref="LiveOpsScheduler"/>（纯 C#，由宿主每帧/每拍调 `Tick(utc)` 推进）。
//   ③ `ScheduleEventPopups()` 里的**弹窗排期**属 C1-W2（14 条 P2）⇒ 本批只落地「同一 id 只排一次」的记账表，
//      真正的弹窗/过期窗由 W2 覆盖。**它出声**（日志一行条数），⛔ 不静默。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 时钟（原版 `PlayerDataManager.CurrentServerDateTime`）
    /// <summary>「现在」的唯一读口。原版取的是**服务器时间**（`PlayerDataManager.singletonManager.CurrentServerDateTime()`）；
    /// 关服后本地没有服务器 ⇒ 用本机 UTC，但**留一个可注入口**（自检把时间钉死，⛔ 别在断言里等真实时间）。
    /// <para>⚠️ 与 `LiveOpsLocalCatalog.NowUtc` **同一语义、两处读口**：catalog 那个是「窗口缺省值」的旧口
    /// （⛔ 不许改，32 条账指着它），新代码一律走本类。</para></summary>
    public static class LiveOpsClock
    {
        /// <summary>注入「现在」（`null` = 本机 UTC）。自检用完记得还原成 `null`。</summary>
        public static Func<DateTime> NowProvider;

        public static DateTime NowUtc { get { return NowProvider != null ? NowProvider() : DateTime.UtcNow; } }

        /// <summary>自检收尾用：还原成真实时间。</summary>
        public static void ResetForTest() { NowProvider = null; }
    }

    // ============================================================ 刷新调度（原版 `ActionScheduler` 的等价物）
    /// <summary>一次「到点刷新」的句柄（原版 `ScheduledAction`）。</summary>
    public sealed class LiveOpsScheduledRefresh
    {
        public DateTime DueUtc;
        public Action Callback;
        /// <summary>已被释放（原版 `ActionScheduler.Release`）⇒ 到点也不触发。</summary>
        public bool Released;
        /// <summary>已经触发过。</summary>
        public bool Fired;
        public override string ToString() { return "refresh@" + DueUtc.ToString("yyyy-MM-dd HH:mm:ss") + "Z"; }
    }

    /// <summary>纯 C# 的到点调度器（替代原版 `SingletonBehaviour&lt;ActionScheduler&gt;`）。
    /// <para>🆕 **2026-10-25（R14）：生产接线点已经接上了** —— <see cref="LiveOpsPump"/> 把本类挂到 Unity 的
    /// `Application.update` 上，每秒最多 `Tick` 一次（此前全仓**没有任何地方调 `Tick`** ⇒ 排出来的刷新永不触发）。</para></summary>
    public static class LiveOpsScheduler
    {
        static readonly List<LiveOpsScheduledRefresh> _pending = new List<LiveOpsScheduledRefresh>();

        /// <summary>排一次到点回调（原版 `ActionScheduler.Schedule(refreshTime, action)`）。</summary>
        public static LiveOpsScheduledRefresh Schedule(DateTime dueUtc, Action callback)
        {
            var h = new LiveOpsScheduledRefresh { DueUtc = dueUtc, Callback = callback };
            _pending.Add(h);
            return h;
        }

        /// <summary>释放一个已排的刷新（原版 `ActionScheduler.Release`）。重复释放安全。</summary>
        public static void Release(LiveOpsScheduledRefresh handle)
        {
            if (handle == null) return;
            handle.Released = true;
            _pending.Remove(handle);
        }

        /// <summary>推进：把 `utcNow` 之前（含）到点的都触发一次，返回触发条数。
        /// ⚠️ 回调是**同步调用**的（原版也一样，`Action` 没有 await）。</summary>
        public static int Tick(DateTime utcNow)
        {
            int fired = 0;
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var h = _pending[i];
                if (h == null) { _pending.RemoveAt(i); continue; }
                if (h.Released) { _pending.RemoveAt(i); continue; }
                if (h.DueUtc > utcNow) continue;
                _pending.RemoveAt(i);
                h.Fired = true;
                fired++;
                try { if (h.Callback != null) h.Callback(); }
                catch (Exception e) { Debug.LogError("[LiveOps] 刷新回调抛异常：" + e); }
            }
            return fired;
        }

        public static int PendingCount { get { return _pending.Count; } }

        public static void ResetForTest() { _pending.Clear(); }
    }

    // ============================================================ 接口（原版 `ILiveOpsHandler`，16 个成员）
    /// <summary>原版 `ILiveOpsHandler`（`ILiveOpsHandler.cs`，成员顺序逐个照抄；`GetEvent(string)` 是**默认实现**）。
    /// <para>🔴 **三参签名是硬约束**（`ILiveOpsHandler.cs:71`）：`SetupEvents(events, blockedEvents, sharedData)` ——
    /// `blockedEvents`（运营维）与 `sharedData`（跨事件共享块）**不能后补**，见 `判据/日常与活动.md` §二 Q1。</para>
    /// <para>⚠️ 原版 `sharedData` 是 `Newtonsoft.Json.Linq.JObject`；工程没有 Newtonsoft ⇒ 换 <see cref="LiveOpsJson"/>。</para></summary>
    public interface ILiveOpsHandler
    {
        string HandlerKey { get; }
        bool IsFirstInitialization { get; }
        event Action EventsRefreshed;

        void RegisterManager(LiveOpsManager manager);
        IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false);
        IEnumerable<ILiveOps> GetBlockedEvents();
        ILiveOps GetEvent(string id);
        Task ValidateData(LiveopsHandlerData data);
        Task SetupEvents(List<ILiveOps> events, List<ILiveOps> blockedEvents, LiveOpsJson sharedData);
        Task InitializeHandler();
        void ClearEvents();
        Task RefreshHandler();
        Task RefreshHandlerLocally();
        void TryScheduleHandlerRefresh(long milis);
        void UpdateCachedSave(LiveOpsSave saveGame);
    }

    // ============================================================ 调度器基类（原版 `LiveOpsHandler<TEvent>`）
    /// <summary>活动调度器。一个 handler 管**一类**事件（`HandlerKey` 是索引）。
    /// 与原版逐条对应的方法见每个方法上的判据注释；未实现的（弹窗/过期窗/容器）**逐条写在文件头差异 ③**。</summary>
    public abstract class LiveOpsHandler : ILiveOpsHandler
    {
        // ── 字段（偏移照反编译：括号里是原版 `this + 0xNN`）────────────────────────
        /// <summary>本 handler 装载的事件（原版 `List&lt;TEvent&gt; events`，+0x20）。</summary>
        protected readonly List<ILiveOps> events = new List<ILiveOps>();
        /// <summary>**被运营屏蔽**的事件（原版 `List&lt;ILiveOps&gt; blockedEvents`，+0x28）。
        /// 🔴 语义 = 「**仍在列表里，但不可开**」（`GetBaseEvents()` 默认不返回它）。
        /// ⛔ **与「过期」不是一回事**：过期是**时间维**、blocked 是**运营维**（`判据/日常与活动.md` §二 Q1）。
        /// 消费点：`DebugEventTogglerWindow`（调试窗列全部含 blocked）· `IRankedEvent` / `ShopOfferAssetProvider`（取全量）。</summary>
        protected readonly List<ILiveOps> blockedEvents = new List<ILiveOps>();
        /// <summary>跨事件共享的一份 JSON（原版 `JObject shared`，+0x30）：每个事件从它里面读公共配置，
        /// **不是各自 SO 里带**。原版消费点 = `DeserializeSharedData()` 读 `NextRefresh`。</summary>
        protected LiveOpsJson shared;
        /// <summary>弹窗排期表（原版 `Dictionary&lt;string, MenuActions.Action&gt; handlerActions`，+0x38）：
        /// key = 事件 id，value = 已排的动作。**已排过的 id 不再排第二次**（去重的唯一正本）。</summary>
        protected readonly Dictionary<string, Action> handlerActions = new Dictionary<string, Action>();
        /// <summary>最近一次排上的刷新句柄（原版 `ScheduledAction scheduledRefresh`，+0x50）。</summary>
        protected LiveOpsScheduledRefresh scheduledRefresh;
        /// <summary>所属管理器（原版 `LiveOpsManager Manager`，+0x58；由 `RegisterManager` 写）。</summary>
        protected LiveOpsManager Manager;
        /// <summary>服务端下发的 handler 载荷缓存（原版 `LiveopsHandlerData handlerCache`，+0x60；由 `ValidateData` 写）。</summary>
        protected LiveopsHandlerData handlerCache;
        /// <summary>刷新互斥（原版 `readonly SemaphoreSlim refreshSemaphore`，+0x68）：同一 handler 不许并发刷新。</summary>
        readonly SemaphoreSlim refreshSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>原版 `Action EventsRefreshed`（+0x40）：刷新完成后通知。</summary>
        public event Action EventsRefreshed;
        /// <summary>原版 `Action OnBeforeRefresh`（+0x48）：刷新**之前**的通知。</summary>
        public event Action OnBeforeRefresh;

        /// <summary>原版 `RefreshCount`（+0x70，protected setter）。</summary>
        public int RefreshCount { get; protected set; }

        /// <summary>原版 `IsFirstInitialization =&gt; RefreshCount == 0`（`ILiveOpsHandler.IsFirstInitialization`）。</summary>
        public bool IsFirstInitialization { get { return RefreshCount == 0; } }

        public abstract string HandlerKey { get; }

        // ── 装载（三参入口 —— 本批最重要的一条）──────────────────────────────────
        /// <summary>原版 `SetupEvents(List&lt;ILiveOps&gt; ops, List&lt;ILiveOps&gt; blockedEvents, JObject sharedData)`
        /// （`LiveOpsHandler.cs:1597` / VA `0x181019390`）。**真身**（`_SetupEvents_d__18...MoveNext.c`，182 行，逐句）：
        /// <code>
        /// shared = (sharedData == null) ? new JObject() : sharedData;
        /// events = ops.Cast&lt;TEvent&gt;().OrderByDescending(x => x.GetBaseData().priority).ToList();  // ← **Priority 降序**
        /// this.blockedEvents = blockedEvents;                        // ← **原样**装进去，不排序、不 Cast
        /// await SetupEvents(events);                                 // ← 虚方法（子类装载）
        /// DeserializeSharedData(shared);                             // ← 读 shared["NextRefresh"] → TryScheduleHandlerRefresh
        /// ScheduleEventPopups();
        /// </code>
        /// 🔴 **三点照抄**：① `blockedEvents` 只装不筛（不可开是**读的时候**决定的，不是装的时候）；
        /// ② 装完 events 之后才解析 shared 与排弹窗；③ 排序键是 **`Priority` 降序**（判据 §一 #10 / §二 Q3）。
        /// ⚠️ 有意差异：原版 `.Cast&lt;TEvent&gt;()`（类型不对会**抛**）与「赋一个新 List」——
        /// 我们是 `ILiveOps` 直装 + 复用同一份 `events`（内容等价，低风险；见判据 §一 #10 差异 2/3）。</summary>
        public async Task SetupEvents(List<ILiveOps> ops, List<ILiveOps> blockedEvents, LiveOpsJson sharedData)
        {
            shared = sharedData ?? LiveOpsJson.NewObject();
            events.Clear();
            if (ops != null)
            {
                // 🔴 排序键 = `GetBaseData().priority`（`LiveOpsEventData` +0x40），**降序**
                // （判据 → `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §一 #10 / §二 Q3：
                //  `0xC99380` 在 `script.json` 里是 `Enumerable.OrderByDescending<object,int>`，
                //  key selector `<SetupEvents>b__18_0` = `GetBaseData()` 之后读 `+0x40`）。
                // 原版 `OrderByDescending` 是**稳定**排序 ⇒ 这里同样用 LINQ（同优先级保持输入序）。
                foreach (var e in ops.OrderByDescending(PriorityOf)) events.Add(e);
            }
            this.blockedEvents.Clear();
            if (blockedEvents != null) this.blockedEvents.AddRange(blockedEvents);

            await SetupEvents(events);
            DeserializeSharedData(shared);
            ScheduleEventPopups();
        }

        /// <summary>原版 `protected virtual Task SetupEvents(List&lt;TEvent&gt; allEvents)`（`:2693`）：
        /// 基类**什么都不做**（`Task.CompletedTask`）；子类在这里装载自己的东西。</summary>
        protected virtual Task SetupEvents(List<ILiveOps> allEvents) { return Task.CompletedTask; }

        /// <summary>原版 `protected virtual void DeserializeSharedData(JObject sharedData)`（`:2523`，VA `0x181017BD0`）：
        /// <code>
        /// if (this.shared == null) throw new NullReferenceException();          // 读的是**字段** +0x30
        /// if (this.shared.TryGetValue("NextRefresh", out v))
        ///     this.TryScheduleHandlerRefresh(v.Value&lt;long&gt;());                // ← **不是** ScheduleHandlerRefresh(DateTime)
        /// </code>
        /// 🔴 **两点坐实**（判据 → `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §一 #2 / §二 Q4）：
        /// ① 数据源是**字段 `this.shared`**（`0x17C08 mov rcx,[rbx+0x30]`），形参 `sharedData` **全程未被读**
        ///    （原版一处「形参残留」；`SetupEvents` 已先赋 `shared`，实际调用链等价，但派生类重写时会分叉）；
        /// ② 那一跳是 **`TryScheduleHandlerRefresh(ms)`**（`0x17C5B call 0x181019810` 在 `script.json` 里反查 =
        ///    `LiveOpsHandler&lt;object&gt;$$TryScheduleHandlerRefresh`）—— 带「已排更早⇒不动」「已过点⇒丢弃」两道门。
        /// <para>⚠️ 有意差异：原版 `this.shared == null` 直接 **NRE**；我们出声后返回（坏 shared 不该让游戏崩）。</para></summary>
        protected virtual void DeserializeSharedData(LiveOpsJson sharedData)
        {
            var src = this.shared;                       // 🔴 字段，不是形参
            if (src == null)
            {
                Debug.LogError("[LiveOps][" + HandlerKey + "] DeserializeSharedData：字段 `shared` 是 null"
                             + "（原版这里会 NRE）⇒ 不解析 NextRefresh。判据 `0x17C15 test rcx,rcx / je <throw>`");
                return;
            }
            long ms;
            if (src.TryGetLong("NextRefresh", out ms))
                TryScheduleHandlerRefresh(ms);           // 两道门都在里面（⛔ 别改成 ScheduleHandlerRefresh）
        }

        /// <summary>原版 `private void ScheduleEventPopups()`（`:1606`）：给每个事件排一次「活动开始/结束」弹窗，
        /// 用 `handlerActions` 按事件 id 去重。
        /// <para>🔴 **本批（C1-W1）只落地去重记账表**：真正的弹窗（`ShowTriggerEventPopup` / `ExpireEvent` /
        /// `QueueEventPopup`）属 C1-W2 的 14 条 P2 ⇒ **由 W2 覆盖本方法**。这里**出声**一行（排了几条 / 跳过几条），
        /// ⛔ 不静默当成「已经做完」。</para>
        /// <para>📍 **交给 W2 的指路**（判据件 §三 #11 / §四·4）：真身 = cpp2il `LiveOpsHandler.cs:1606` 附近（**有体**、SEV4、41 行注记）；
        /// 入口地址 `ShowTriggerEventPopup` = **`0x181019560`**；逐事件读 `data.drawData.ShowWindowOnTrigger` /
        /// `ShowWindowOnExpire`（`EventElements` +0x28 / +0x38）再调 `ShowTriggerEventPopup` / `ExpireEvent`。
        /// ⚠️ `ExpireEvent` 弹窗支有一条**反常次序**（开窗后**立刻**走完成回调，不等窗口关）—— 反汇编确实如此，
        /// ⛔ **别按「关闭后才回调」去改**（判据件 §四·5）。</para></summary>
        protected virtual void ScheduleEventPopups()
        {
            int queued = 0, skipped = 0;
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                string id = ev == null ? null : ev.GetID();
                if (string.IsNullOrEmpty(id)) { skipped++; continue; }
                if (handlerActions.ContainsKey(id)) { skipped++; continue; }
                handlerActions[id] = null;      // W2 会把真正的 Action 填进这一格
                queued++;
            }
            Debug.Log("[LiveOps][" + HandlerKey + "] ScheduleEventPopups：记账 " + queued + " 条、跳过 " + skipped
                    + " 条（**真正的弹窗排期属 C1-W2**，本批只建去重表）");
        }

        // ── 刷新 ────────────────────────────────────────────────────────────────
        /// <summary>原版 `public Task RefreshHandler()`（`:2638`）。**真身**（`_RefreshHandler_d__26...MoveNext.c`）：
        /// `Log("[key] Refreshing")` → `HandleBeforeRefresh()` → `await GenericCloudScriptHandlerAsync(0x402, {handlerKey})`
        /// → `await RefreshHandler(拿回来的 handlerData)`。
        /// <para>🔴 **离线口径**：没有服务器 ⇒ 不发请求，**出声后转本地刷新**（`RefreshHandlerLocally`）。
        /// ⛔ 不是静默跳过（账 row 393 的差异列就写着「离线应跳过并出声，不许静默」）。</para></summary>
        public virtual Task RefreshHandler()
        {
            Debug.Log("[LiveOps][" + HandlerKey + "] 刷新请求：**本地没有服务器**（原版走 PlayFab CloudScript 0x402）"
                    + " ⇒ 不发请求，转本地刷新。判据 `LiveOpsHandler.cs:2638`");
            return RefreshHandlerLocally();
        }

        /// <summary>原版 `public Task RefreshHandlerLocally()`（`:2647`）。**真身**（`_RefreshHandlerLocally_d__27...MoveNext.c`）：
        /// <code>
        /// HandleBeforeRefresh();
        /// await &lt;虚调用, 单参 = 字段 +0x60&gt;;   // ← 见下面的「⚠️」
        /// </code>
        /// ⚠️ 那个虚调用取的是 `this+0x60` = `handlerCache`，而我们按语义走
        /// `RefreshHandler(handlerCache)`（`protected virtual Task RefreshHandler(LiveopsHandlerData)`）——
        /// **两读法只差一次参数取值，未坐实**，账里标了 B。</summary>
        public virtual Task RefreshHandlerLocally()
        {
            HandleBeforeRefresh();
            return RefreshHandler(handlerCache);
        }

        /// <summary>原版 `protected virtual Task RefreshHandler(LiveopsHandlerData data)`（`:2656`）。
        /// **真身**（`_RefreshHandler_d__29...MoveNext.c`）：`refreshSemaphore.WaitAsync()` →
        /// `ConfigManager.GetConfig&lt;FeatureConfig&gt;()` → `await Manager.BuildHandler(this, data, config)` → `Release()`。
        /// ⚠️ 没有载荷（`data == null`）⇒ **出声返回**（原版这里会 NRE，因为 `manager.BuildHandler` 要 data）。</summary>
        protected virtual async Task RefreshHandler(LiveopsHandlerData data)
        {
            if (data == null)
            {
                Debug.LogWarning("[LiveOps][" + HandlerKey + "] 刷新：**没有 handler 载荷**（handlerCache = null；"
                               + "原版由服务器下发）⇒ 不重建事件列表。离线想看活动请用 `LiveOpsLocalHandler` 的本地表。");
                return;
            }
            if (Manager == null)
            {
                Debug.LogError("[LiveOps][" + HandlerKey + "] 刷新：Manager 是 null（`RegisterManager` 没调过）⇒ 无法建 handler");
                return;
            }
            await refreshSemaphore.WaitAsync();
            try
            {
                await Manager.BuildHandler(this, data, LiveOpsFeatureConfig.Current);
            }
            finally
            {
                refreshSemaphore.Release();
            }
            var h = EventsRefreshed;
            if (h != null) h();
        }

        /// <summary>原版 `private void HandleBeforeRefresh()`（`:2665`）：`Log("[key] Before Refresh")`
        /// → 有订阅就 `OnBeforeRefresh()` → `BeforeRefresh()`。**逐条照抄**。</summary>
        protected void HandleBeforeRefresh()
        {
            Debug.Log("[" + HandlerKey + "] Before Refresh");
            var h = OnBeforeRefresh;
            if (h != null) h();
            BeforeRefresh();
        }

        /// <summary>原版 `protected virtual void BeforeRefresh()`（`:2689`）：基类空（子类可在此清缓存）。</summary>
        protected virtual void BeforeRefresh() { }

        /// <summary>原版 `public void TryScheduleHandlerRefresh(long milis)`（`:2535`）。**真身**：
        /// <code>
        /// var now  = CurrentServerDateTime();
        /// var when = MillisToDateTime(milis);
        /// if (scheduledRefresh != null &amp;&amp; scheduledRefresh.When &lt;= when) {
        ///     Log("[key]Trying to schedule a refresh to a time older to the already scheduled Reset in "
        ///         + fmt(旧-now) + " vs the proposed in " + fmt(when-now));
        ///     return;                                  // ① 已排得更早 ⇒ **不动**，只出声
        /// }
        /// if (milis &gt;= currentServerTime) { ScheduleHandlerRefresh(when); return; }   // ② 还没到点 ⇒ 真排
        /// Log("[key] Trying to schedule a refresh to a time already past {when} - {now}");  // ③ 过点 ⇒ 丢弃并出声
        /// </code>
        /// 🔴 **两道门都照抄**：已排更早 ⇒ 打日志不动；已过点 ⇒ 丢弃并出声（⛔ 都不静默）。</summary>
        public void TryScheduleHandlerRefresh(long milis)
        {
            DateTime now = LiveOpsClock.NowUtc;
            DateTime when = LiveOpsEventTimeData.FromMs(milis);
            if (scheduledRefresh != null && !scheduledRefresh.Released)
            {
                if (scheduledRefresh.DueUtc <= when)
                {
                    Debug.Log("[" + HandlerKey + "]Trying to schedule a refresh to a time older to the already scheduled Reset in "
                            + FormatSpan(scheduledRefresh.DueUtc - now) + " vs the proposed in " + FormatSpan(when - now));
                    return;
                }
            }
            long nowMs = LiveOpsEventTimeData.ToMs(now);
            if (milis >= nowMs) { ScheduleHandlerRefresh(when); return; }
            Debug.Log("[" + HandlerKey + "] Trying to schedule a refresh to a time already past "
                    + when.ToString("yyyy-MM-dd HH:mm:ss") + "Z - " + now.ToString("yyyy-MM-dd HH:mm:ss") + "Z");
        }

        /// <summary>原版 `private void ScheduleHandlerRefresh(DateTime refreshTime)`（`:2612`）。**真身**：
        /// `if (scheduledRefresh != null) ActionScheduler.Instance.Release(scheduledRefresh);`
        /// → `Log("[key] Scheduling a refresh in " + fmt(refreshTime - now))`
        /// → `scheduledRefresh = ActionScheduler.Instance.Schedule(refreshTime, &lt;刷新回调&gt;)`。
        /// 刷新回调（原版 `&lt;ScheduleHandlerRefresh&gt;b__25_0`，`:3487`）= 「起一个 `RefreshHandler()` 的 Task」（丢弃）。</summary>
        protected void ScheduleHandlerRefresh(DateTime refreshTime)
        {
            if (scheduledRefresh != null) LiveOpsScheduler.Release(scheduledRefresh);
            DateTime now = LiveOpsClock.NowUtc;
            Debug.Log("[" + HandlerKey + "] Scheduling a refresh in " + FormatSpan(refreshTime - now));
            scheduledRefresh = LiveOpsScheduler.Schedule(refreshTime, () => { var _ = RefreshHandler(); });
        }

        static string FormatSpan(TimeSpan t)
        {
            // 原版用 `TimeHelper.FormatTimeSpan`（本地没有那个类的判据 ⇒ 我们用等价的 d.hh:mm:ss）。
            if (t.Ticks < 0) t = TimeSpan.Zero;
            return ((int)t.TotalDays) + "." + t.Hours.ToString("00") + ":" + t.Minutes.ToString("00") + ":" + t.Seconds.ToString("00");
        }

        // ── 初始化 / 清理 ───────────────────────────────────────────────────────
        /// <summary>原版 `public unsafe virtual Task InitializeHandler()`（`:2698` / VA `0x181018250`）。**真身**：
        /// `RefreshCount += 1;` → `foreach (var e in events) e.Initialize();` → `Task.CompletedTask`。
        /// 🔴 **先加计数再遍历**（`IsFirstInitialization` 靠 `RefreshCount == 0`，第一拍才算「首次」）。
        /// <para>⚠️ **有意差异**（判据 → §三 #12）：原版**没有 try/catch**（一个事件抛 ⇒ 整条链断、后续事件不 Initialize）；
        /// 我们逐个 catch + 出声 ⇒ 一个坏事件不影响其余。保留这条差异（坏数据不该让整条刷新链断掉），⛔ 不是「已经一样」。</para></summary>
        public virtual Task InitializeHandler()
        {
            RefreshCount = RefreshCount + 1;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e == null) continue;
                try { e.Initialize(); }
                catch (Exception ex) { Debug.LogError("[LiveOps][" + HandlerKey + "] 事件 '" + e.GetID() + "' 的 Initialize 抛异常：" + ex); }
            }
            return Task.CompletedTask;
        }

        /// <summary>原版 `public unsafe void ClearEvents()`（`:2791` / VA `0x1810176B0`，size `0x520`）：
        /// <code>
        /// foreach (var a in handlerActions.Values) MenuActions.ReleaseAction(a);   // ① 撤菜单排期
        /// foreach (var e in events)        e.Dispose();                            // ② ILiveOps slot 3
        /// foreach (var e in blockedEvents) e.Dispose();                            // ③ **封锁表也要 Dispose**
        /// events.Clear(); blockedEvents.Clear(); handlerActions.Clear();           // ④ 三段清空
        /// </code>
        /// 🔴 **顺序照抄**：先撤排期、后清列表。
        /// <para>⚠️ **① 还没接**：原版 `MenuActions.ReleaseAction` 是**全局静态**（`actions.Remove(action)`），
        /// 「`handlerActions.Clear()`」≠「菜单队列已撤销」。我们全仓**没有 `MenuActions` 等价物**
        /// （`Shell/ToastNotifications.cs` 的 `_scheduled` 只是 `QueueAction` 的近似，**没有按句柄撤销的口**），
        /// 且本批 `handlerActions` 的值全是 `null`、没有真排期 ⇒ **① 记成改动请求**（先复刻 `MenuActions` 才能接），
        /// ⛔ 不自己造一个。判据 → 判据件 §一 #1 / §四·3。</para>
        /// <para>⚠️ 有意差异：原版不判空、不 catch；我们逐个判空 + catch 出声。</para></summary>
        public virtual void ClearEvents()
        {
            if (handlerActions.Count > 0) handlerActions.Clear();      // ① 的占位：只清表（撤销见上面的 ⚠️）
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e == null) continue;
                try { e.Dispose(); }
                catch (Exception ex) { Debug.LogError("[LiveOps][" + HandlerKey + "] 事件 Dispose 抛异常：" + ex); }
            }
            for (int i = 0; i < blockedEvents.Count; i++)              // ③ 原版这里也走 ILiveOps slot 3
            {
                var e = blockedEvents[i];
                if (e == null) continue;
                try { e.Dispose(); }
                catch (Exception ex) { Debug.LogError("[LiveOps][" + HandlerKey + "] 封锁事件 Dispose 抛异常：" + ex); }
            }
            events.Clear();
            blockedEvents.Clear();
        }

        /// <summary>原版 `public unsafe void ResetEvents()`（`:3005` / VA `0x181018B90`，size `0x130`）：
        /// <code>
        /// foreach (var e in events) e.ResetEventProgress();     // 只这一句（ILiveOps slot 7）
        /// </code>
        /// 🔴 **不清空列表、不 Dispose** —— 反汇编只读 `[this+0x20]`、循环体 `mov r8d,7`（slot 7 = `ResetEventProgress`），
        /// **全程没有任何 `List.Clear`**。「清空列表」是 <see cref="ClearEvents"/> 的事（**两个方法语义整条不同**）。
        /// <para>🔴 **2026-10-25 订正**：本方法原来实现成「逐个 Dispose + `events.Clear()`」，**语义整条反了**
        /// （判据 → `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` §一 #5；R7 报告 §六·2 记成「值得进已知的坑」——
        /// 名字像 Reset 就以为要清空，看着很「像对的」）。
        /// 影响面已核：全仓唯一调用方 = `LiveOpsManager.ResetHandlers()`（`:136`，而它自己 0 个调用方）
        /// ⇒ **没有任何地方依赖「清空」**。</para>
        /// <para>⚠️ 有意差异：原版不判空、不 catch；我们逐个判空 + catch 出声。</para></summary>
        public virtual void ResetEvents()
        {
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e == null) continue;
                try { e.ResetEventProgress(); }
                catch (Exception ex) { Debug.LogError("[LiveOps][" + HandlerKey + "] 事件 ResetEventProgress 抛异常：" + ex); }
            }
        }

        // ── 读口 ────────────────────────────────────────────────────────────────
        /// <summary>原版 `public virtual IEnumerable&lt;TEvent&gt; GetEvents(bool includeBlockedEvents = false)`（`:3093` / VA `0x181018130`）：
        /// <code>
        /// if (!includeBlockedEvents) return events;                              // ★ 原样返回【字段本身】（活引用）
        /// return events.Concat&lt;ILiveOps&gt;(blockedEvents).Cast&lt;TEvent&gt;();        // 懒式、元素类型不对会抛
        /// </code>
        /// <para>⚠️ **有意差异**（判据 → §一 #6）：原版 `true` 支是**懒式** `Concat + Cast&lt;TEvent&gt;`
        /// （立即求值发生在枚举时，且元素不是 `TEvent` 会抛 `InvalidCastException`）；
        /// 我们**立即拷贝**到一个新 List（不抛、不延迟）。内容等价，差别只在「延迟 vs 立即」与「严格 vs 放宽」。</para></summary>
        public virtual IEnumerable<ILiveOps> GetEvents(bool includeBlockedEvents = false)
        {
            if (!includeBlockedEvents) return events;
            var all = new List<ILiveOps>(events.Count + blockedEvents.Count);
            all.AddRange(events);
            all.AddRange(blockedEvents);
            return all;
        }

        /// <summary>原版 `public virtual IEnumerable&lt;T&gt; GetEvents&lt;T&gt;(bool includeBlockedEvents = false)`（`:3113`）：
        /// 同一份源再 `.OfType&lt;T&gt;()`。</summary>
        public virtual IEnumerable<T> GetEvents<T>(bool includeBlockedEvents = false) where T : class, ILiveOps
        {
            var src = GetEvents(includeBlockedEvents);
            var outp = new List<T>();
            foreach (var e in src) { var t = e as T; if (t != null) outp.Add(t); }
            return outp;
        }

        /// <summary>原版 `public IEnumerable&lt;ILiveOps&gt; GetBaseEvents(bool includeBlockedEvents = false)`（`:3135`）：
        /// 就是 `GetEvents(includeBlockedEvents)` 的 **ILiveOps 静态类型版**（原版是「虚方法转发」）。</summary>
        public IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false)
        {
            return GetEvents(includeBlockedEvents);
        }

        /// <summary>原版 `public IEnumerable&lt;ILiveOps&gt; GetBlockedEvents()`（`:3149`）：**原样**返回 `blockedEvents`。</summary>
        public IEnumerable<ILiveOps> GetBlockedEvents() { return blockedEvents; }

        /// <summary>原版 `public virtual TEvent GetEvent(string id)`（`:3156`）：
        /// `GetEvents(false).FirstOrDefault(x =&gt; x.GetID() == id)`。⚠️ **不查 blocked**（`includeBlockedEvents:false`）。</summary>
        public virtual ILiveOps GetEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e != null && e.GetID() == id) return e;
            }
            // 原版走 FirstOrDefault ⇒ 没有就是 null（不抛、不 LogWarning）。⛔ 这里也不用 LogError 刷屏。
            return null;
        }

        /// <summary>原版 `public T GetEvent&lt;T&gt;(string id)`（`:3210`）：`GetEvent(id) as T`。</summary>
        public T GetEvent<T>(string id) where T : class, ILiveOps { return GetEvent(id) as T; }

        // ── 缓存 / 管理器 ───────────────────────────────────────────────────────
        /// <summary>原版 `public void UpdateCachedSave(LiveOpsEventPersistence saveGame)`（`:1538` / VA `0x181019CB0`）：
        /// <code>
        /// var e = handlerCache.RawEvents.FirstOrDefault(x =&gt; x.Persistence != null
        ///                                                   &amp;&amp; x.Persistence.eventId == saveGame.eventId);
        /// if (e != null) e.Persistence = saveGame;
        /// </code>
        /// 🔴 **比的是 `RawEvent.Persistence.eventId`（+0x18 → +0x10），不是 `RawEvent.Data.eventId`**
        /// （判据 → §一 #11：谓词实体 `<>c__DisplayClass9_0.&lt;UpdateCachedSave&gt;b__0` 读 `[[x+0x18]+0x10]`）。
        /// 2026-10-25 已按判据订正（原来比的是 `r.Data.eventId`：多数时候两值相等，但原版是**按存档 id 对齐**）。
        /// <para>⚠️ 本地 `handlerCache` 在离线链路里恒为 null ⇒ 实际**什么都不做**（真实的空转，⛔ 不是「已完成」）。
        /// ⚠️ 有意差异：原版 `saveGame == null` / `handlerCache == null` 都是 NRE，我们提前返回。</para></summary>
        public virtual void UpdateCachedSave(LiveOpsSave saveGame)
        {
            if (saveGame == null) return;
            if (handlerCache == null || handlerCache.RawEvents == null) return;
            for (int i = 0; i < handlerCache.RawEvents.Count; i++)
            {
                var r = handlerCache.RawEvents[i];
                if (r == null || r.Persistence == null) continue;
                if (r.Persistence.eventId != saveGame.eventId) continue;
                r.Persistence = saveGame;
                return;
            }
        }

        /// <summary>原版 `public void RegisterManager(LiveOpsManager manager)`（`:1580`）：`Manager = manager;`（字段 +0x58）。</summary>
        public void RegisterManager(LiveOpsManager manager) { Manager = manager; }

        /// <summary>原版 `public virtual Task ValidateData(LiveopsHandlerData data)`（`:1588`）：
        /// `handlerCache = data; return Task.CompletedTask;`（字段 +0x60）。</summary>
        public virtual Task ValidateData(LiveopsHandlerData data) { handlerCache = data; return Task.CompletedTask; }

        /// <summary>自检口：当前缓存的 handler 载荷（只读）。</summary>
        public LiveopsHandlerData HandlerCacheForTest { get { return handlerCache; } }
        /// <summary>自检口：弹窗排期表条数（去重表的唯一正本）。</summary>
        public int PopupScheduleCountForTest { get { return handlerActions.Count; } }

        /// <summary>原版 `SetupEvents` 的排序 key selector（`<>c.&lt;SetupEvents&gt;b__18_0`）：
        /// `x.GetBaseData()` 之后读 `LiveOpsEventData.priority`（+0x40）；空对象一律按 0 参与排序（原版是 NRE，我们降级）。</summary>
        static int PriorityOf(ILiveOps e)
        {
            var d = e == null ? null : e.GetBaseData();
            return d == null ? 0 : d.priority;
        }
    }

    // ============================================================ 我们的具体事件实例（本地目录 13 份 SO）
    /// <summary>把一个 `LiveOpsLocalEvent`（离线活动数据）包成**真事件实例**：
    /// `TSave = LiveOpsSave`、`TData = LiveOpsEventData`（字段从离线数据映射过来）。
    /// <para>⚠️ 映射只搬**已经解出来**的字段（eventId / enabled / uploadToLive / 起止 / expirable / expireDate /
    /// priority / showInMainMenu / location.type）。
    /// 🆕 **2026-10-25（R14）**：`priority` / `showInMainMenu` / `location` 三格已从 SO 补抄进
    /// `LiveOpsLocalEvent`（真值 → `资料/完全复刻/报告/R14_LiveOps数据与调度_第2轮.md` §四）。
    /// ⛔ **`location.parent` 的 Unity 引用（`genericObjectReference` / `reference` 的 PathID）没有对应资产 ⇒ 不搬**；
    /// `LiveOpsEventData.location` 只存 `EventLocationType` 那个 int（原版就是这个字段的枚举那半，
    /// parent 那半在 `EventLocation.parent` 上 —— 我们**没有** `EventLocation` 类，见 §五）。
    /// `drawData`（`EventElements`）同样**没有等价类** ⇒ 那 4 个 bool 暂存 `LiveOpsLocalEvent`
    /// （`ForceRun` / `ShowWindowOnTrigger` / `ShowWindowOnExpire` / `HideContainer`），留给 C1-W2 接弹窗。</para></summary>
    public sealed class LocalLiveOpsEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
    {
        /// <summary>离线数据本体（窗口取词条/图片还要用它；C1-W3 接资产时从它转）。</summary>
        public LiveOpsLocalEvent Local { get; private set; }

        /// <summary>⚠️ **只给工厂用**（`Activator.CreateInstance` 要无参构造，判据 = `LiveOpsEventFactory.NewEvent`）。
        /// 造出来的实例是空的（`Local == null`），必须随后 `LoadEventData` 才能用。</summary>
        public LocalLiveOpsEvent() { }

        public LocalLiveOpsEvent(LiveOpsLocalEvent local) { Local = local; }

        /// <summary>把离线数据映射成框架的 `LiveOpsEventData`。</summary>
        public static LiveOpsEventData MapData(LiveOpsLocalEvent local)
        {
            if (local == null) return null;
            return new LiveOpsEventData
            {
                eventId = local.EventId,
                enabled = local.Enabled,
                uploadToLive = local.UploadToLive,
                activationType = LiveOpsActivationType.OnDate,
                // 🆕 R14：排序键（`SetupEvents` 的 `OrderByDescending(priority)`）与主菜单角标都读这三格。
                priority = local.Priority,
                showInMainMenu = local.ShowInMainMenu,
                location = local.LocationType,          // = SO `location.type`（EventLocationType 的 int）
                dates = new LiveOpsEventTimeData
                {
                    startDate = local.StartMs,
                    endDate = local.EndMs,
                    expirable = local.Expirable,
                    expireDate = local.ExpireMs,
                    previewable = false,          // 本地 13 份 SO 没有 previewDate（字段表里就没抄到）
                },
            };
        }

        /// <summary>装上 data/save 之后**顺便把离线数据本体找回来**（工厂走 `Activator` 时 `Local` 是 null）。
        /// 这一步是我们加的（原版没有「离线目录」这东西）：`Local` 只用于取词条/图片（C1-W3）。</summary>
        public override void LoadEventData(LiveOpsEventData data, LiveOpsSave saveGame, ILiveOpsHandler handler)
        {
            base.LoadEventData(data, saveGame, handler);
            if (Local == null && Data != null && !string.IsNullOrEmpty(Data.eventId))
                Local = LiveOpsLocalCatalog.Find(Data.eventId);
        }

        /// <summary>把**本地存档**写回 `SaveCenter`（原版由子类写服务器；离线等价物见文件头差异 ②）。</summary>
        public override void SaveEventProgress(Action onSuccess, Action onFail)        {
            bool ok = LiveOpsSaveStore.Save(SaveGame);
            if (ok) { if (onSuccess != null) onSuccess(); }
            else
            {
                Debug.LogError("[LiveOps] 活动 '" + GetID() + "' 的进度落盘失败：" + LiveOpsSaveStore.LastError);
                if (onFail != null) onFail();
            }
        }

        /// <summary>从离线数据 + 存档装出一个实例（= 原版 `LiveOpsEventFactory.CreateEvent` 的本地等价）。
        /// `save` 传 null ⇒ 走 `ValidateEventData` 的「新建存档」那一支。</summary>
        public static LocalLiveOpsEvent Create(LiveOpsLocalEvent local, LiveOpsSave save, ILiveOpsHandler handler)
        {
            var ev = new LocalLiveOpsEvent(local);
            var data = MapData(local);
            LiveOpsEventData fd; LiveOpsSave fs;
            if (!ev.ValidateEventData(data, save, out fd, out fs))
            {
                Debug.LogError("[LiveOps] 本地事件 '" + (local == null ? "<null>" : local.EventId) + "' 的 data 校验失败（不该发生）");
                return null;
            }
            ev.LoadEventData(fd, fs, handler);
            return ev;
        }
    }

    // ============================================================ 我们的具体 handler（本地目录）
    /// <summary>本地目录驱动的 handler：事件 = `LiveOpsLocalCatalog.Events` 那 13 份，
    /// `blockedEvents` / `shared` 由调用方给（默认都空）。
    /// <para>🔴 **「当前活动」的选择逻辑就住在这里**（`SelectInTable` 是唯一实现）—— 这是
    /// `判据/日常与活动.md` §四 第 3 步「把选择逻辑从 catalog 挪进 handler」的落地：
    /// `LiveOpsLocalCatalog.TryPickEventIn` 现在**只做转发**，⛔ 不再有第二份挑选规则。</para></summary>
    public sealed class LiveOpsLocalHandler : LiveOpsHandler
    {
        /// <summary>原版每个 handler 有自己的一把 key（`HandlerKey`）。本地目录只有一套 ⇒ `"local"`。</summary>
        public const string LocalHandlerKey = "local";

        public override string HandlerKey { get { return LocalHandlerKey; } }

        static LiveOpsLocalHandler _default;
        /// <summary>进程级默认实例（`LiveOpsManager` 注册它；窗口/自检都从这儿取）。</summary>
        public static LiveOpsLocalHandler Default
        {
            get
            {
                if (_default == null) _default = new LiveOpsLocalHandler();
                return _default;
            }
        }

        /// <summary>自检收尾用：丢掉默认实例（下一次 `Default` 会新建，事件列表是空的）。</summary>
        public static void ResetForTest() { _default = null; }

        /// <summary>按离线数据装出全部事件实例（**含已过期的** —— 原版 `events` 也含过期，只是不可开）。
        /// 返回装了几个。</summary>
        public int LoadFromCatalog(DateTime utc, LiveOpsJson sharedData = null)
        {
            var ops = new List<ILiveOps>();
            var table = LiveOpsLocalCatalog.Events;
            for (int i = 0; i < table.Count; i++)
            {
                var local = table[i];
                if (local == null) continue;
                var save = LiveOpsSaveStore.Load(local.EventId, typeof(LiveOpsSave)) as LiveOpsSave;
                var ev = LocalLiveOpsEvent.Create(local, save, this);
                if (ev != null) ops.Add(ev);
            }
            // 三参入口同步走一遍（内部已经 await 完，但因为全是同步分支，这里可以直接 Wait）
            SetupEvents(ops, new List<ILiveOps>(), sharedData).GetAwaiter().GetResult();
            Debug.Log("[LiveOps] LoadFromCatalog：本地装了 " + ops.Count + " 个活动实例（含过期；blocked = "
                    + blockedEvents.Count + "）");
            return ops.Count;
        }

        /// <summary>某个本地事件出厂时的「该不该开」判据（原版 `enabled &amp;&amp; uploadToLive`）。</summary>
        static bool IsSelectable(LiveOpsLocalEvent e) { return e != null && e.Selectable; }

        /// <summary>🔴 **唯一的挑选规则**（原口径 = `LiveOpsLocalCatalog.TryPickEventIn` 那段，逐条搬过来）：
        /// ① 类型在 `kinds` 里 ② `IsInPeriod(utc)` ③ 额外条件 `has` ④ 不在 `blockedIds` 里
        /// ⑤ `Selectable`（`enabled &amp;&amp; uploadToLive`）⑥ 多个满足取 `StartMs` 最大者。
        /// **⛔ 过期事件永不入选**（`IsInPeriod` 那道闸）。
        /// <para>⚠️ 与**原版**的差异（如实记）：原版挑事件走 `GetState` 的 6 档 + `EventFilter`；
        /// 我们用 `IsInPeriod`（`start ≤ now &lt; end`）⇒ **`Finished`/`Expired` 的边界与原版不同**
        /// （原版 `IsActive` 连 `Finished` 都算 true，见 `LiveOpsEventData.IsActive` 的注释）。</para></summary>
        public static bool SelectInTable(IReadOnlyList<LiveOpsLocalEvent> table, LiveOpsLocalKind[] kinds, DateTime utc,
                                         Func<LiveOpsLocalEvent, bool> has, ICollection<string> blockedIds,
                                         out LiveOpsLocalEvent ev, out string why)
        {
            ev = null;
            string blocked = null;
            string blockedByServer = null;
            if (table != null)
            {
                for (int i = 0; i < table.Count; i++)
                {
                    var e = table[i];
                    if (e == null) continue;
                    if (!LiveOpsLocalCatalog.KindIn(e.Kind, kinds)) continue;
                    if (!e.IsInPeriod(utc)) continue;
                    if (has != null && !has(e)) continue;
                    if (blockedIds != null && blockedIds.Contains(e.EventId)) { blockedByServer = e.EventId; continue; }
                    if (!IsSelectable(e)) { blocked = e.EventId; continue; }   // 在期但 SO 说没启用/没上过 live
                    if (ev == null || e.StartMs > ev.StartMs) ev = e;
                }
            }
            if (ev != null) { why = null; return true; }
            why = blockedByServer != null
                ? "本地目录里在期的事件只有 `" + blockedByServer + "`，但它被**运营屏蔽**（`blockedEvents`，见 `ILiveOpsHandler.SetupEvents`）⇒ 不选"
                : blocked != null
                ? "本地目录里在期的事件只有 `" + blocked + "`，但它的 SO 是 `enabled=0`/`uploadToLive=0` ⇒ 不选"
                : "本地 " + (table == null ? 0 : table.Count) + " 个活动实例里**没有** `" + LiveOpsLocalCatalog.KindNames(kinds)
                  + "` 类、且在期（" + utc.ToString("yyyy-MM-dd HH:mm:ss") + " UTC）、并满足该读口条件的事件";
            return false;
        }

        /// <summary>本 handler 的 blocked id 集合（从 `blockedEvents` 现算；`SelectInTable` 用）。</summary>
        public ICollection<string> BlockedIds
        {
            get
            {
                var set = new HashSet<string>();
                for (int i = 0; i < blockedEvents.Count; i++)
                {
                    var e = blockedEvents[i];
                    if (e != null && !string.IsNullOrEmpty(e.GetID())) set.Add(e.GetID());
                }
                return set;
            }
        }

        /// <summary>「现在开着哪个事件」——**实例版读口**（给将来的窗用；本批不接窗）。</summary>
        public bool TrySelectActive(LiveOpsLocalKind kind, DateTime utc, out LocalLiveOpsEvent ev, out string why)
        {
            ev = null;
            var kinds = new[] { kind };
            var mine = new List<LiveOpsLocalEvent>();
            for (int i = 0; i < events.Count; i++)
            {
                var inst = events[i] as LocalLiveOpsEvent;
                if (inst == null || inst.Local == null) continue;
                mine.Add(inst.Local);
            }
            LiveOpsLocalEvent dataEv;
            if (!SelectInTable(mine, kinds, utc, null, BlockedIds, out dataEv, out why)) return false;
            for (int i = 0; i < events.Count; i++)
            {
                var inst = events[i] as LocalLiveOpsEvent;
                if (inst != null && inst.Local != null && inst.Local.EventId == dataEv.EventId) { ev = inst; return true; }
            }
            why = "内部不一致：挑中了 '" + dataEv.EventId + "' 但事件列表里找不到它的实例";
            return false;
        }

        /// <summary>「现在开着哪个事件」——**数据版读口**（返回离线数据；`LiveOpsLocalCatalog` 的旧口转发到这儿）。</summary>
        public static bool TrySelectActiveData(LiveOpsLocalKind[] kinds, DateTime utc, Func<LiveOpsLocalEvent, bool> has,
                                               ICollection<string> blockedIds, out LiveOpsLocalEvent ev, out string why)
        {
            return SelectInTable(LiveOpsLocalCatalog.Events, kinds, utc, has, blockedIds, out ev, out why);
        }
    }

    // ============================================================ 生产 Tick 接线点（2026-10-25 · R14）
    /// <summary>把 <see cref="LiveOpsScheduler"/> 接到 Unity **自带的**每帧泵 `PlayerLoop`（`Update` 阶段）上。
    ///
    /// <para>**为什么需要它**：`LiveOpsScheduler` 是纯 C# 静态类，2026-10-25 现查 —— 全仓
    /// `grep -rn "LiveOpsScheduler" Assets/` 只有 `Schedule` / `Release` / `ResetForTest` 三个调用，
    /// **没有任何地方调 `Tick(utc)`** ⇒ <see cref="LiveOpsEvent{TData,TSave}.Initialize"/> 排出来的刷新
    /// **永远不会触发**（整条「到点刷新」链路休眠）。</para>
    ///
    /// <para>**泵选的是哪一条**（2026-10-25 实测）：本轮白名单只有 `LiveOpsHandler.cs` / `LiveOpsLocalCatalog.cs` /
    /// `LiveOpsEvent.cs` ⇒ 动不了 <c>ShellRuntime.Update()</c>（那才是**工程内**最合适的现成泵）。
    /// ⇒ 用 **Unity 自带的 PlayerLoop**：把本类作为一个子系统插进 `UnityEngine.PlayerLoop.Update` 阶段
    /// （`UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop()` → 插 → `SetPlayerLoop`）。
    /// ⛔ **不是新建 MonoBehaviour**、⛔ **不是 `[ExecuteAlways]`**、不需要往任何场景挂东西；
    /// 安装点 = `[RuntimeInitializeOnLoadMethod]`（同族先例：`WarpforgeVFX/Runtime/WarpforgeEffectHook.cs:15` ·
    /// `Core/WarpforgeAudio.cs:40` · `Core/PlayerBoot.cs:67`）。
    /// ⚠️ **为什么不是 `Application.update`**：Unity 6000.3 的 `UnityEngine.Application` **没有 `update` 事件**
    /// （实测 `error CS0117: Application 未包含 update 的定义`）；能用的等价物只有 `Application.onBeforeRender`，
    /// 但它**依赖渲染**（不渲染就不走）⇒ 选 PlayerLoop（每帧必走、与渲染无关）。</para>
    ///
    /// <para>⚠️ **覆盖范围**：player 与编辑器 **Play 模式**都有 PlayerLoop；**批处理 / 编辑模式不跑 PlayerLoop**
    /// （那是 `EditorApplication.update`，⛔ 两条泵别混 —— 见 `ShellRuntime.cs:346` 那次踩坑）。
    /// 想改用**工程内**的泵（更省一次 PlayerLoop 注入），只需在 `ShellRuntime.Update()` 里加一行
    /// `LiveOpsPump.ManualTick();` —— 那一行在别人的白名单里，本轮没动。</para>
    ///
    /// <para>🔴 **两条隔离（都为了「不静默改掉自检结论」）**：
    /// ① **有假时间注入时泵完全不动**（`LiveOpsClock.NowProvider != null`）。自检把「现在」钉死之后
    ///    由它自己 `Tick(...)`；否则泵会拿假时间抢先触发，把「显式 Tick 触发 1 条」变成 **0 条**。
    /// ② **节流**：每 <see cref="MinIntervalSeconds"/> 秒最多 Tick 一次 —— `Tick` 只遍历一张小表，
    ///    但没必要每帧读一次系统时钟（`DateTime.UtcNow`）。</para></summary>
    public static class LiveOpsPump
    {
        /// <summary>最小 Tick 间隔（秒）。原版 `ActionScheduler` 是每帧检查；这里 1 秒足够（活动刷新是「小时/天」尺度）。</summary>
        public const float MinIntervalSeconds = 1f;

        static bool _installed;
        static float _nextTickAt;

        /// <summary>本类是否已插进 PlayerLoop（自检 / 诊断读口）。</summary>
        public static bool Attached { get { return _installed; } }

        /// <summary>进 Play / 启动时自动安装一次（`BeforeSceneLoad` = 场景加载前，早于任何活动窗 `Awake`）。</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install() { Attach(); }

        /// <summary>插进 PlayerLoop 的 `Update` 阶段。**幂等**：先查有没有，再决定插不插
        /// （「域重载关掉」时 PlayerLoop 与静态字段都可能跨 Play 存活 ⇒ 不查就会越插越多）。</summary>
        public static void Attach()
        {
            var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            if (ContainsPump(loop)) { _installed = true; return; }
            if (!InsertInto(loop, typeof(UnityEngine.PlayerLoop.Update)))
            {
                Debug.LogError("[LiveOps] 没找到 PlayerLoop 的 `Update` 阶段 ⇒ 到点刷新不会自动触发"
                             + "（影响：活动不会到点刷新；自检的显式 Tick 不受影响）");
                return;
            }
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
            _installed = true;
        }

        /// <summary>从 PlayerLoop 摘掉（自检 / 手动接管时间时用）。重复调用安全。</summary>
        public static void Detach()
        {
            var loop = UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveFrom(loop))
            {
                UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);
            }
            _installed = false;
        }

        /// <summary>手动推进一次（给「工程内泵」用：将来 `ShellRuntime.Update()` 直接调这一行即可；
        /// 与自动泵的隔离/节流规则**完全一致**，两处都接也不会双触发 —— `_nextTickAt` 是同一个）。</summary>
        public static void ManualTick() { OnUpdate(); }

        static void OnUpdate()
        {
            if (LiveOpsClock.NowProvider != null) return;      // ← 隔离 ①：自检钉死了「现在」⇒ 泵让位
            float now = Time.unscaledTime;
            if (now < _nextTickAt) return;                     // ← 隔离 ②：节流
            _nextTickAt = now + MinIntervalSeconds;
            LiveOpsScheduler.Tick(LiveOpsClock.NowUtc);
        }

        // ---- PlayerLoop 增删（`PlayerLoopSystem` 是 struct，但它挂在数组里 ⇒ 改数组元素就地生效）----
        static bool ContainsPump(UnityEngine.LowLevel.PlayerLoopSystem sys)
        {
            var subs = sys.subSystemList;
            if (subs == null) return false;
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i].type == typeof(LiveOpsPump)) return true;
                if (ContainsPump(subs[i])) return true;
            }
            return false;
        }

        static bool InsertInto(UnityEngine.LowLevel.PlayerLoopSystem sys, Type phase)
        {
            var subs = sys.subSystemList;
            if (subs == null) return false;
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i].type == phase)
                {
                    var inner = subs[i].subSystemList;
                    var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner == null ? 0 : inner.Length);
                    if (inner != null) list.AddRange(inner);
                    list.Add(new UnityEngine.LowLevel.PlayerLoopSystem { type = typeof(LiveOpsPump), updateDelegate = OnUpdate });
                    subs[i].subSystemList = list.ToArray();
                    return true;
                }
                if (InsertInto(subs[i], phase)) return true;
            }
            return false;
        }

        static bool RemoveFrom(UnityEngine.LowLevel.PlayerLoopSystem sys)
        {
            var subs = sys.subSystemList;
            if (subs == null) return false;
            for (int i = 0; i < subs.Length; i++)
            {
                if (subs[i].type == typeof(LiveOpsPump))
                {
                    var inner = subs[i].subSystemList ?? new UnityEngine.LowLevel.PlayerLoopSystem[0];
                    var list = new List<UnityEngine.LowLevel.PlayerLoopSystem>(inner);
                    list.RemoveAll(s => s.type == typeof(LiveOpsPump));
                    subs[i].subSystemList = list.ToArray();
                    return true;
                }
                if (RemoveFrom(subs[i])) return true;
            }
            return false;
        }
    }
}
