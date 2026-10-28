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
//   ③ ✅ **2026-10-26（Q3）起 `ScheduleEventPopups` 已是真身**（原批 ③「属 C1-W2、只落地去重表」那条**作废**）：
//      `ScheduleEventPopups` / `QueueEventPopup` / `ShowTriggerEventPopup` / `ExpireEvent` 四个方法按 Ghidra 反编译逐句照抄，
//      `handlerActions` 改存 `MenuAction` 句柄，并复刻了 `MenuActions` 等价物（含静态 `ReleaseAction`）。
//      ⚠️ 唯一未接的一跳 = **弹窗 prefab 解析**（`LiveOpsAssetUtility.GetComponentReference`）：本地只有 GUID 没有 prefab 名
//      ⇒ 经 `LiveOpsPopupPresenter` 缝注入，钩子为空时出声、不出窗（排期链照走）。
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
        /// <summary>原版 `ScheduledAction.TargetDateTime__BackingField`（由 epoch 毫秒回算；与 <see cref="DueUtc"/> 同值）。
        /// 判据 = `decomp_full/ActionScheduler__Schedule.c`：`*(lVar5+0x18) = MillisSinceEpochToDateTime(TargetTime)`。</summary>
        public DateTime TargetDateTime;
        /// <summary>原版 `ScheduledAction.ActionId__BackingField`（调用方没给时 = `Guid.NewGuid().ToString()`）。
        /// 用途 = 同一 id 重复排期时**替换旧的**（判据 `ActionScheduler__Schedule.c` 的去重段）。</summary>
        public string ActionId;
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

        /// <summary>排一次到点回调 —— **本重载 = 原版 `ActionScheduler.Schedule(DateTime dateTime, Action action, string actionId = null)`**
        /// （判据 = `decomp_full/ActionScheduler__Schedule.c` 前 30 行 + `out_cpp2il_dev_ilspy/ActionScheduler.cs:76`）：
        /// 原版把 `DateTime` 按 `new DateTime(1970,1,1,…)` 换算成 **epoch 毫秒**后**转调 `Schedule(long)`** —— 下面两行就是那一步。
        /// ⚠️ cpp2il 那份在换算的尾段有一处 `Jump target not found`，换算方向（`dateTime - 1970` → 毫秒）由可见的 4 句定。
        /// <para>`actionId` 非空时走重载去重（同一 id 先前排过 ⇒ 旧的被移除并 `LogWarning`）。</para></summary>
        public static LiveOpsScheduledRefresh Schedule(DateTime dueUtc, Action callback, string actionId = null)
        {
            long millis = (long)(dueUtc.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            return Schedule(millis, callback, actionId);
        }

        /// <summary>释放一个已排的刷新（原版 `ActionScheduler.Release`）。重复释放安全。</summary>
        public static void Release(LiveOpsScheduledRefresh handle)
        {
            if (handle == null) return;
            handle.Released = true;
            _pending.Remove(handle);
        }

        /// <summary>推进一次 —— **每拍最多弹 1 条**，返回触发条数（0 或 1）。
        /// <para>判据 = 原版 `ActionScheduler.Update()`（`decomp_full/ActionScheduler__Update.c`，逐句）：
        /// `var a = scheduledActions.FirstOrDefault(); if (a != null &amp;&amp; a.TargetTime &lt;= currentServerTime)
        /// { scheduledActions.Remove(a); a.Callback(); }` ⇒ 只看**队首**（队列按 `TargetTime` 排序 ⇒ 队首 = 最早那条）、
        /// **每次只弹一条**。</para>
        /// <para>⚠️ 回调是**同步调用**的（原版也一样，`Action` 没有 await）。</para>
        /// <para>⚠️ 我们的泵 <see cref="LiveOpsPump"/> 是**1 秒一拍**（原版每帧）⇒ 同刻多条到期时我们是「1 秒一条」、
        /// 原版是「1 帧一条」，可见差 ≈ 后续刷新延迟 N 秒（活动刷新是小时/天尺度 ⇒ 无可见后果）。
        /// 🔶 自己设计（非原版）：泵的 1 秒节流（`LiveOpsPump.MinIntervalSeconds`）· 判据 = 原版每帧检查，本仓按开销自定 · 调度台 2026-10-28</para></summary>
        public static int Tick(DateTime utcNow)
        {
            if (_pending.Count == 0) return 0;
            var h = _pending[0];
            if (h == null || h.Released) { _pending.RemoveAt(0); return 0; }   // 残渣：只清、不触发
            if (h.DueUtc > utcNow) return 0;                                   // 队首还没到 ⇒ 后面更没到（已排序）
            _pending.RemoveAt(0);
            h.Fired = true;
            try { if (h.Callback != null) h.Callback(); }
            catch (Exception e) { Debug.LogError("[LiveOps] 刷新回调抛异常：" + e); }
            return 1;
        }

        public static int PendingCount { get { return _pending.Count; } }

        public static void ResetForTest() { _pending.Clear(); }

        // ---- ↓ 2026-10-28（B89-E15）追加到类尾：原版 `ActionScheduler` 的三个成员 ----

        /// <summary>原版 `ActionScheduler.Schedule(long dateTime, Action action, string actionId = null)` 的**真身**
        /// （判据 = `decomp_full/ActionScheduler__Schedule.c` 全文，逐句；行数 158）：
        /// <list type="number">
        /// <item>`dateTime &lt; TimeHelper.ServerTime` ⇒ **直接 `return null`、不入队**（不是「排一个立即执行的」）。</item>
        /// <item>建 `ScheduledAction`：`Callback`(+0x28) · `TargetTime`(+0x20) · `TargetDateTime = MillisSinceEpochToDateTime(TargetTime)`(+0x18)。</item>
        /// <item>`ActionId`(+0x10) = 传入的 `actionId`，为空则 `Guid.NewGuid().ToString()`。</item>
        /// <item>**只在传入 actionId 非空时**（`.c` 判的是 `param_4 != 0`）去重：`Any(同 id)` ⇒
        ///   `Debug.LogWarning("Action with ID " + id + " already exists in the queue. Replacing it.")` ⇒ `First(同 id)` 移除旧的。</item>
        /// <item>入队后 `List.Sort(按 TargetTime)`（`.c` 末尾 `System_Collections_Generic_List&lt;object&gt;__Sort`）。</item>
        /// </list></summary>
        public static LiveOpsScheduledRefresh Schedule(long dateTimeMs, Action callback, string actionId = null)
        {
            if (dateTimeMs < NowMillis()) return null;      // ① 过点的不排（原版 `return 0`）
            var h = new LiveOpsScheduledRefresh
            {
                DueUtc = EpochMsToUtc(dateTimeMs),          // ②
                Callback = callback,
                ActionId = string.IsNullOrEmpty(actionId) ? System.Guid.NewGuid().ToString() : actionId,   // ③
            };
            h.TargetDateTime = h.DueUtc;
            if (!string.IsNullOrEmpty(actionId))            // ④ 只有**显式给了** id 才去重
            {
                int at = _pending.FindIndex(x => x != null && string.Equals(x.ActionId, h.ActionId, StringComparison.Ordinal));
                if (at >= 0)
                {
                    Debug.LogWarning("Action with ID " + h.ActionId + " already exists in the queue. Replacing it.");
                    _pending.RemoveAt(at);
                }
            }
            _pending.Add(h);
            _pending.Sort((x, y) => x == null || y == null ? 0 : x.DueUtc.CompareTo(y.DueUtc));   // ⑤
            return h;
        }

        /// <summary>原版 `ActionScheduler.ScheduleRelative(TimeSpan relativeTime, Action action, string actionId = null)`
        /// （判据 = `decomp_full/ActionScheduler__ScheduleRelative.c`：`currentServerTime + relativeTime.TotalMilliseconds`
        /// → 转调 `Schedule(long)`；`.c` 里那一段的 `timeSpan` 是默认值 ⇒ 可见的只有「`TotalMilliseconds` 加当前时刻」这一句）。</summary>
        public static LiveOpsScheduledRefresh ScheduleRelative(TimeSpan relativeTime, Action callback, string actionId = null)
        {
            return Schedule(NowMillis() + (long)relativeTime.TotalMilliseconds, callback, actionId);
        }

        /// <summary>`TimeHelper.ServerTime` / `PlayerDataManager.currentServerTime` 的等价物（**epoch 毫秒**）。
        /// ⚠️ 仍然读 <see cref="LiveOpsClock"/>（「现在」的唯一读口 —— 自检钉时间时两条路一起钉）。</summary>
        static long NowMillis()
        {
            return (long)(LiveOpsClock.NowUtc.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
        }

        /// <summary>原版 `SupportMethods.MillisSinceEpochToDateTime`（`ActionScheduler__Schedule.c` 里那一跳）。</summary>
        static DateTime EpochMsToUtc(long ms)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
        }
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
        /// key = 事件 id，value = 已排的动作。**已排过的 id 不再排第二次**（去重的唯一正本）。
        /// <para>🆕 **2026-10-26（Q3）**：值类型从 `System.Action` 换成 <see cref="MenuAction"/>（原版就是
        /// `MenuActions.Action`）—— 只有这样 `ClearEvents` 才能对每个句柄调 `MenuActions.ReleaseAction`（撤销菜单排期）。</para></summary>
        protected readonly Dictionary<string, MenuAction> handlerActions = new Dictionary<string, MenuAction>();
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

        /// <summary>原版 `private void ScheduleEventPopups()`（VA `0x181018D60`，size `0x?`；Ghidra 产物
        /// `decomp_generic_trial/out/LiveOpsHandler_object___ScheduleEventPopups__1018d60.c`）—— **2026-10-26（Q3）已拿到真身**，
        /// 逐句照抄（两个 lambda 的实体也读到了：`<ScheduleEventPopups>b__0` = `ShowTriggerEventPopup`、
        /// `b__1` = `ExpireEvent`，见 `decomp_full/LiveOpsHandler.__c__DisplayClass19_0_object____ScheduleEventPopups_b__{0,1}.c`）：
        /// <code>
        /// foreach (TEvent newEvent in events)                       // ILiveOps slot 15 = GetBaseData
        /// {
        ///     var drawData = newEvent.GetBaseData().drawData;       // LiveOpsEventData +0x38
        ///     if (drawData == null) throw NRE;                      // 隐式
        ///     if (drawData.ShowWindowOnTrigger                      // EventElements +0x28
        ///         &amp;&amp; !newEvent.GetBaseSave().WasSeen                 // slot 16 → +0x19
        ///         &amp;&amp; !handlerActions.ContainsKey(newEvent.GetID()))  // IIdentifiable slot 0
        ///     {
        ///         QueueEventPopup(newEvent, drawData.TriggerPopupOptions,          // +0x30
        ///             onComplete =&gt; ShowTriggerEventPopup(newEvent, drawData.TriggerPopupOptions, onComplete));
        ///         continue;                                          // ★ 排了触发弹窗就【不再】看过期支
        ///     }
        ///     if (newEvent.EventState == LiveOpState.Finished)       // ILiveOps slot 23 == 3
        ///         QueueEventPopup(newEvent, drawData.ExpirePopupOptions,            // +0x40
        ///             onComplete =&gt; ExpireEvent(newEvent, drawData.ExpirePopupOptions, drawData.ShowWindowOnExpire, onComplete));
        /// }
        /// </code>
        /// 🔴 **三个判据要点**（判据件 §一·附 · Q3 本轮新取证）：
        /// ① 触发支的三道闸 = `ShowWindowOnTrigger &amp;&amp; !WasSeen &amp;&amp; !已排过`（**按事件 id 去重**）；
        /// ② **过期支只在 `EventState == Finished(3)` 时排**（不是 `Expired(4)`！反汇编 `FUN_1800021f0(0x17=slot23) == 3`）；
        /// ③ 触发支排上就 `continue` —— **同一个事件不会同时排触发弹窗与过期弹窗**。
        /// <para>⚠️ 有意差异：原版 `data` / `drawData` 为 null 直接 NRE；我们出声后跳过该事件（坏数据不让整条链崩）。</para></summary>
        protected virtual void ScheduleEventPopups()
        {
            int queued = 0;
            for (int i = 0; i < events.Count; i++)
            {
                var newEvent = events[i];
                if (newEvent == null) continue;
                var data = newEvent.GetBaseData();
                var drawData = data == null ? null : data.drawData;
                if (drawData == null)
                {
                    Debug.LogError("[LiveOps][" + HandlerKey + "] ScheduleEventPopups：事件 '"
                                 + (newEvent.GetID()) + "' 的 `drawData` 是 null（原版这里 NRE）⇒ 跳过该事件的排期");
                    continue;
                }
                if (drawData.ShowWindowOnTrigger)
                {
                    var save = newEvent.GetBaseSave();
                    if (save != null && !save.WasSeen)
                    {
                        string id = newEvent.GetID();
                        if (!handlerActions.ContainsKey(id))
                        {
                            var ev = newEvent;                       // 捕获循环局部（每轮新变量）
                            var opts = drawData.TriggerPopupOptions;
                            QueueEventPopup(newEvent, opts, onComplete => ShowTriggerEventPopup(ev, opts, onComplete));
                            queued++;
                            continue;                                // ★ 排了触发弹窗 ⇒ 不再看过期支
                        }
                    }
                }
                // ILiveOps slot 23 = get_EventState（= GetBaseData().GetState(NowUtc)）；== Finished(3)
                if (data != null && data.GetState(LiveOpsClock.NowUtc) == LiveOpState.Finished)
                {
                    var ev = newEvent;
                    var opts = drawData.ExpirePopupOptions;
                    bool showExpirePopup = drawData.ShowWindowOnExpire;
                    QueueEventPopup(newEvent, opts, onComplete => ExpireEvent(ev, opts, showExpirePopup, onComplete));
                    queued++;
                }
            }
            Debug.Log("[LiveOps][" + HandlerKey + "] ScheduleEventPopups：排入菜单队列 " + queued + " 条"
                    + "（触发支 = `ShowWindowOnTrigger&&!WasSeen&&!已排`；过期支 = `EventState==Finished`）");
        }

        /// <summary>原版 `private void QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action&lt;Action&gt; eventAction)`
        /// （VA `0x181018490`，Ghidra `LiveOpsHandler_object___QueueEventPopup__1018490.c`）：
        /// <code>
        /// if (handlerActions.ContainsKey(newEvent.GetID())) return;        // 按事件 id 去重
        /// var menu   = SingletonBehaviour&lt;MenuActions&gt;.Instance;
        /// var data   = newEvent.GetBaseData();
        /// var filter = data.filter;                                        // LiveOpsEventData +0x30
        /// string[] groups = filter.eventGroups.Select(r =&gt; r.TargetId).ToArray();
        /// bool forceRun = data.drawData.ForceRun;                          // EventElements +0x49
        /// var action = menu.QueueActionWithCallback(eventAction, options.ActionRule, newEvent.Priority,
        ///                                             groups, options.TriggerBeforeOnboardingEnds, forceRun, recurring:false, menuFilter:null);
        /// handlerActions.Add(newEvent.GetID(), action);
        /// </code>
        /// <para>⚠️ 判据细节：优先级用的是**事件自己的 `Priority`**（ILiveOps slot 21 = `get_Priority`），不是 options 里的。
        /// `groups` 我们的等价物 = `filter.eventGroupIds`（本地目录还没抄 ⇒ 空组，见 <see cref="LiveOpsEventFilter.eventGroupIds"/>）。</para></summary>
        protected virtual void QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action<Action> eventAction)
        {
            if (newEvent == null) { Debug.LogError("[LiveOps] QueueEventPopup(null)"); return; }
            string id = newEvent.GetID();
            if (handlerActions.ContainsKey(id)) return;

            var data = newEvent.GetBaseData();
            var filter = data == null ? null : data.filter;
            string[] groups = filter != null && filter.eventGroupIds != null ? filter.eventGroupIds : new string[0];
            bool forceRun = data != null && data.drawData != null && data.drawData.ForceRun;
            int priority = data == null ? 0 : data.priority;

            var action = MenuActions.QueueActionWithCallback(
                eventAction,
                options == null ? ActionRule.DontBlock : options.ActionRule,
                priority,
                groups,
                options != null && options.TriggerBeforeOnboardingEnds,
                forceRun,
                false,          // recurring
                null);          // menuFilter

            handlerActions.Add(id, action);
            Debug.Log("[LiveOps][" + HandlerKey + "] QueueEventPopup：事件 '" + id + "' 已排入菜单队列"
                    + "（优先级 " + priority + " · 组 " + groups.Length + " · forceRun=" + forceRun + "）");
        }

        /// <summary>原版 `private void ShowTriggerEventPopup(ILiveOps newEvent, PopupOptions options, Action onComplete)`
        /// （VA `0x181019560`，Ghidra `LiveOpsHandler_object___ShowTriggerEventPopup__1019560.c`）：
        /// <code>
        /// if (!handlerActions.ContainsKey(newEvent.GetID())) return;     // 没排过 ⇒ 什么都不做
        /// handlerActions.Remove(newEvent.GetID());
        /// CustomDebug.Log("…" + newEvent.GetID());
        /// if (options != null) {
        ///     var reference = LiveOpsAssetUtility.GetComponentReference(newEvent, options.PopupLabel);  // +0x10
        ///     var window = SingletonBehaviour&lt;WindowsManager&gt;.Instance.OpenWindow(reference, newEvent, closeAll:false, default);
        ///     if (options.MarkOnScreenClose)                    // +0x19
        ///         window.&lt;+0x60&gt; += () =&gt; newEvent.MarkAsSeen();   // ILiveOps slot 12
        ///     if (onComplete != null) onComplete();             // ★ 开窗后【立刻】回调（不等关窗）
        /// }
        /// </code>
        /// <para>⚠️ 离线缺口：`LiveOpsAssetUtility.GetComponentReference` 需要「事件 → 弹窗 prefab」的资产引用，
        /// 本地只解到 GUID、没解到 prefab 名 ⇒ 交给 <see cref="LiveOpsPopupPresenter"/> 装；钩子为空时出声 + 不出窗，
        /// 但**仍然走完成回调**（原版拿不到窗会 NRE，我们不让排期链卡死）。</para></summary>
        protected virtual void ShowTriggerEventPopup(ILiveOps newEvent, PopupOptions options, Action onComplete)
        {
            if (newEvent == null) { Debug.LogError("[LiveOps] ShowTriggerEventPopup(null)"); return; }
            string id = newEvent.GetID();
            if (!handlerActions.ContainsKey(id)) return;
            handlerActions.Remove(id);
            Debug.Log("[LiveOps][" + HandlerKey + "] 触发弹窗：" + id);
            if (options != null)
            {
                var window = LiveOpsPopupPresenter.Open == null ? null : LiveOpsPopupPresenter.Open(newEvent, options);
                if (window == null)
                {
                    Debug.LogWarning("[LiveOps] 触发弹窗：没有可用的弹窗 prefab 解析器（`LiveOpsPopupPresenter.Open` 未装）"
                                   + " ⇒ 事件 '" + id + "' 的触发弹窗未真正显示（原版走 `LiveOpsAssetUtility.GetComponentReference`）");
                }
                else if (options.MarkOnScreenClose)
                {
                    window.OnCloseWindow += () => newEvent.MarkAsSeen();   // 原版写的是 window+0x60（= GameWindow.OnCloseWindow）
                }
                if (onComplete != null) onComplete();                    // ★ 立刻（照抄原版反常次序）
            }
        }

        /// <summary>原版 `private void ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete)`
        /// （VA `0x181017C80`，Ghidra `LiveOpsHandler_object___ExpireEvent__1017c80.c`）：
        /// <code>
        /// if (!handlerActions.ContainsKey(newEvent.GetID())) return;      // 没排过 ⇒ 什么都不做
        /// handlerActions.Remove(newEvent.GetID());
        /// CustomDebug.Log("…" + newEvent.GetID());
        /// if (!showExpirePopup) { newEvent.Expire(HandleOnComplete); return; }   // ILiveOps slot 2
        /// if (options != null) {
        ///     var reference = LiveOpsAssetUtility.GetComponentReference(newEvent, options.PopupLabel);
        ///     var window = WindowsManager.Instance.OpenWindow(reference, newEvent, closeAll:false, default);
        ///     if (options.MarkOnScreenClose)
        ///         window.&lt;+0x60&gt; += () =&gt; newEvent.Expire(null);
        ///     HandleOnComplete();                 // ★ 弹窗支【立刻】走完成回调（不等关窗）—— 判据件 §四·5，⛔ 别改成关窗后
        /// }
        /// // HandleOnComplete = Signal.Raise(new EventExpiredSignal(id)); onComplete?.Invoke();
        /// </code>
        /// <para>⚠️ 离线缺口同 <see cref="ShowTriggerEventPopup"/>：弹窗解析交给 <see cref="LiveOpsPopupPresenter"/>。
        /// 信号总线原版是 `Scaffold.Core.Events.Signal`，我们等价物 = <see cref="LiveOpsSignals.EventExpired"/>。</para></summary>
        protected virtual void ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete)
        {
            if (newEvent == null) { Debug.LogError("[LiveOps] ExpireEvent(null)"); return; }
            string id = newEvent.GetID();
            if (!handlerActions.ContainsKey(id)) return;
            handlerActions.Remove(id);
            Debug.Log("[LiveOps][" + HandlerKey + "] 过期弹窗：" + id);

            Action handleOnComplete = () =>
            {
                LiveOpsSignals.RaiseEventExpired(id);       // 原版：Signal.Raise(new EventExpiredSignal(id))
                if (onComplete != null) onComplete();
            };

            if (!showExpirePopup)
            {
                newEvent.Expire(handleOnComplete);          // 非弹窗支：把回调交给 Expire
                return;
            }
            if (options != null)
            {
                var window = LiveOpsPopupPresenter.Open == null ? null : LiveOpsPopupPresenter.Open(newEvent, options);
                if (window == null)
                {
                    Debug.LogWarning("[LiveOps] 过期弹窗：没有可用的弹窗 prefab 解析器（`LiveOpsPopupPresenter.Open` 未装）"
                                   + " ⇒ 事件 '" + id + "' 的过期弹窗未真正显示（原版走 `LiveOpsAssetUtility.GetComponentReference`）");
                }
                else if (options.MarkOnScreenClose)
                {
                    window.OnCloseWindow += () => newEvent.Expire(null);
                }
                handleOnComplete();                         // ★ 立刻（判据：弹窗支不等关窗）
            }
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
        /// ✅ 2026-10-28 W7 坐实（`…_RefreshHandlerLocally_d__27_object___MoveNext.c:44`）：`(**(code**)(*this + 0x2a8))(this, plVar1[0xc], …)` —— 虚槽 `0x2a8` = `RefreshHandler(LiveopsHandlerData)`，
        /// 第 1 实参读的就是 **this+0x60**（`plVar1[0xc]`）⇒ 与我们传的 `handlerCache` 是同一个槽（⛔ 原判词「只差一次取值，未坐实」已作废）。
        /// 残余只有「+0x60 的字段名是不是 `handlerCache`」这一层命名对应（布局对不上 dump）⇒ 账可从 B 提到「等价」。</summary>
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
        /// <para>🆕 **① 已于 2026-10-26（Q3）接上**：原版 `MenuActions.ReleaseAction` 是**全局静态**
        /// （`actions.Remove(action)`）—— 「`handlerActions.Clear()`」≠「菜单队列已撤销」，是**两个口**。
        /// 我们已在文件尾复刻了 `MenuActions` 等价物（`QueueAction` / `QueueActionWithCallback` 返回句柄 +
        /// 静态 `ReleaseAction`），本方法逐个撤排期。判据 → 判据件 §一 #1 / §四·3 · `LiveOpsHandler_11方法_第2轮.md` §五·5。</para>
        /// <para>⚠️ 有意差异：原版不判空、不 catch；我们逐个判空 + catch 出声。</para></summary>
        public virtual void ClearEvents()
        {
            // ① 原版：`foreach (MenuActions.Action a in handlerActions.Values) MenuActions.ReleaseAction(a);`
            //    `ReleaseAction` 是**全局静态**（`actions.Remove(action)`）⇒ 把菜单队列里排着的动作**逐个撤掉**。
            //    🆕 2026-10-26（Q3）：`MenuActions` 等价物已落地（文件尾），这一跳**已接**（原「改动请求」收口）。
            foreach (var a in handlerActions.Values) MenuActions.ReleaseAction(a);
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
            handlerActions.Clear();
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
                activationType = local.ActivationType,
                // 🆕 R14：排序键（`SetupEvents` 的 `OrderByDescending(priority)`）与主菜单角标都读这三格。
                priority = local.Priority,
                showInMainMenu = local.ShowInMainMenu,
                location = local.LocationType,          // = SO `location.type`（EventLocationType 的 int）
                // 🆕 2026-10-26（Q3）：`drawData`（EventElements）—— 排期/弹窗的判据输入。
                // 4 个 bool 从 `LiveOpsLocalEvent` 的同名字段取（R14 已逐份照 SO 抄好）；
                // 两组 PopupOptions 是 13 份 SO 的**固定取值**（逐份相同）：Trigger={20, ActionRule 1, 0, true} / Expire={21, 同上}
                // 判据 → `LiveOpsLocalCatalog.cs` §drawData 注释 + R14 报告 §五·3。
                drawData = new EventElements
                {
                    ShowWindowOnTrigger = local.ShowWindowOnTrigger,
                    ShowWindowOnExpire = local.ShowWindowOnExpire,
                    HideContainer = local.HideContainer,
                    ForceRun = local.ForceRun,
                    TriggerPopupOptions = new PopupOptions
                    {
                        PopupLabel = 20,
                        ActionRule = ActionRule.BlockActionsFromSameGroup,   // 原版值 = 1
                        TriggerBeforeOnboardingEnds = false,
                        MarkOnScreenClose = true,
                    },
                    ExpirePopupOptions = new PopupOptions
                    {
                        PopupLabel = 21,
                        ActionRule = ActionRule.BlockActionsFromSameGroup,   // 原版值 = 1
                        TriggerBeforeOnboardingEnds = false,
                        MarkOnScreenClose = true,
                    },
                },
                dates = new LiveOpsEventTimeData
                {
                    startDate = local.StartMs,
                    endDate = local.EndMs,
                    expirable = local.Expirable,
                    expireDate = local.ExpireMs,
                    previewable = local.Previewable, previewDate = local.PreviewDateMs,   // 13 份 SO 实测 previewable 全 0
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

        /// <summary>⚠️ 2026-10-27 更正：原来写「按离线数据装出全部事件实例（含已过期的 —— 原版 `events` 也含过期）」。实际**装载口要过滤**：原版客户端拿到的 `events` 是**服务端下发的「当前有效」集合**，客户端自己不过滤（`ScheduleEventPopups` 也证明它不查日期）⇒ 我们替代服务端，就必须在**装载口**（不是判定支）把 `Expired` / `NotAvailable` 滤掉。判据与逐条理由 → 类尾 `LiveOpsLocalBoot.ShouldLoadEvent`。
        /// 返回装了几个。</summary>
        public int LoadFromCatalog(DateTime utc, LiveOpsJson sharedData = null)
        {
            var ops = new List<ILiveOps>();
            var table = LiveOpsLocalCatalog.Events;
            for (int i = 0; i < table.Count; i++)
            {
                var local = table[i];
                if (local == null || !LiveOpsLocalBoot.ShouldLoadEvent(local, utc)) continue;   // 🔶 自己设计（非原版）：原版由服务端下发「当前有效活动」集合，本地用 `GetState` 过滤替代 · 判据 = `LiveOpsEventData.GetState`/`IsActive` + `ScheduleEventPopups` 不查日期 · 调度台 2026-10-27 裁定
                var save = LiveOpsSaveStore.Load(local.EventId, typeof(LiveOpsSave)) as LiveOpsSave;
                var ev = LocalLiveOpsEvent.Create(local, save, this);
                if (ev != null) ops.Add(ev);
            }
            // 三参入口同步走一遍（内部已经 await 完，但因为全是同步分支，这里可以直接 Wait）
            SetupEvents(ops, new List<ILiveOps>(), sharedData).GetAwaiter().GetResult();
            Debug.Log("[LiveOps] LoadFromCatalog：本地装了 " + ops.Count + " 个活动实例（**已按状态过滤**："
                    + "滤掉 Expired/NotAvailable；判据 LiveOpsLocalBoot.ShouldLoadEvent；blocked = " + blockedEvents.Count + "）");
            return ops.Count;
        }

        /// <summary>某个本地事件出厂时的「该不该开」判据（原版 `enabled &amp;&amp; uploadToLive`）。</summary>
        static bool IsSelectable(LiveOpsLocalEvent e) { return e != null && e.Selectable; }

        /// <summary>🔴 **唯一的挑选规则**（原口径 = `LiveOpsLocalCatalog.TryPickEventIn` 那段，逐条搬过来）：
        /// ① 类型在 `kinds` 里 ② **`GetState(utc) == Active`** ③ 额外条件 `has` ④ 不在 `blockedIds` 里 ⑤ `Selectable` ⑥ `StartMs` 最大者。
        /// **⛔ `Finished`/`Expired`/`Planned`/`OnPreview`/`NotAvailable` 一律不入选**（= 原版建按钮那一档）。
        /// ⚠️ 本表 13 份事件的 `activationType` 全是 `OnDate`（`Shell/LiveOpsState.cs` 的字段注释有逐份判据）。
        /// <para>🆕 **2026-10-27（B88_J3）改口径**：原为 `IsInPeriod`（`start ≤ now &lt; end`），缺 `activationType` 短路 / `Expired` 档 / 严格小于边界。
        /// 判据 = `decomp_full/Everguild.LiveOps.LiveOpsEventData__GetState.c` + `out_cpp2il_dev_ilspy/NavigationPanelController.cs:433`（`!= Active` ⇒ 不建按钮）。
        /// ⚠️ 别换成 `IsActive()` —— 它连 `Finished` 也算 true（那是「该刷新/该收尾」的口径）。</para></summary>
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
                    if (!e.IsCurrentlyActive(utc)) continue;   // 原版：只有 EventState == Active 才建按钮/入选
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
            // 🆕 2026-10-26（Q3）：菜单动作队列的重试探口（原版靠 `MenuActions.Routine` 协程驱动；
            // 我们的等价物在这里每帧重试一次 —— 只有「入队时条件还不满足」的动作会留到下一帧）。
            // 放在时间隔离之前：它不依赖 `LiveOpsClock`，自检里也该照常推进。
            MenuActions.TryDrain();
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

    // ============================================================ 菜单动作队列（原版 `MenuActions` 的等价物 · 2026-10-26 Q3）
    /// <summary>原版 `ActionRule`（`MenuActions.Action.ActionRule`，显式值逐个抄）：
    /// `DontBlock=0 · BlockActionsFromSameGroup=1 · BlockAllActions=2`。
    /// 判据 = `d:/2/tools/il2cpp_out/dump.cs` 的 `enum ActionRule`（TypeDefIndex 1566）。</summary>
    public enum ActionRule
    {
        DontBlock = 0,
        BlockActionsFromSameGroup = 1,
        BlockAllActions = 2,
    }

    /// <summary>原版 `MenuActions.Action`（`dump.cs` TypeDefIndex 1560；字段逐个抄）—— **一条已排/待排的菜单动作**。
    /// <para>原版两个构造函数：接 `Action callback`（同步）与接 `Action&lt;Action&gt; asyncCallback`（异步、完成后回调）。
    /// `Priority` 降序排队；`Groups` 与 `usedGroups` 交集非空则不触发；`MenuFilter` = 「只在这些窗类型里触发」的白名单；
    /// `ActionRule` 决定触发**之后**怎么封锁后续动作；`Recurring` = 触发后不摘除。</para>
    /// <para>`IsEnqueued` = 原版 `MenuActions.actions.Contains(this)`（全局静态表）。</para></summary>
    public sealed class MenuAction
    {
        public Action Callback;                          // +0x10
        public Type[] MenuFilter;                        // +0x18
        public ActionRule ActionRule;                    // +0x20
        public int Priority;                             // +0x24
        public bool TriggerBeforeOnboardingEnds;         // +0x28
        public string[] Groups;                          // +0x30
        public Action<Action> AsyncCallback;             // +0x38
        public bool ForceRun;                            // +0x40
        public bool Recurring;                           // +0x41

        public bool IsEnqueued { get { return MenuActions.Contains(this); } }
        public override string ToString()
        {
            return "MenuAction(prio=" + Priority + " rule=" + ActionRule + " groups=" + (Groups == null ? 0 : Groups.Length)
                 + (Callback != null ? " sync" : " async") + (Recurring ? " recurring" : "") + ")";
        }
    }

    /// <summary>原版 `MenuActions : SingletonBehaviour&lt;MenuActions&gt;` 的**等价物**（2026-10-26 · Q3）。
    ///
    /// <para>**为什么要有它**：原版 `LiveOpsHandler.ClearEvents` 的第一句是
    /// `foreach (var a in handlerActions.Values) MenuActions.ReleaseAction(a);` —— 而 `ReleaseAction` 是
    /// **全局静态**（`actions.Remove(action)`）。「排期表清空」≠「菜单队列撤销」，是**两个口**；没有这个等价物，
    /// `ClearEvents` 的第一跳就只能空着（R12 §六·2 的改动请求）。</para>
    ///
    /// <para>**逐条照抄的语义**（判据 = `d:/2/tools/decomp_full/MenuActions__*.c` 全量现读，2026-10-26）：
    /// <list type="bullet">
    /// <item>`QueueAction` / `QueueActionWithCallback`：建一条 <see cref="MenuAction"/> 塞进**静态** `actions`，
    ///       返回**句柄**（原版返回 `MenuActions.Action`，调用方拿它去 `ReleaseAction`）；</item>
    /// <item>`ReleaseAction(action)`：`actions.Remove(action)` —— **按句柄撤销**（本类存在的唯一理由）；</item>
    /// <item>`BlockGroups(groups)`：往 `usedGroups` 里塞组名（原版 `BlockGroups` = `usedGroups.AddRange`）；</item>
    /// <item>触发顺序：`OrderByDescending(x =&gt; x.Priority)`（**稳定**）；条件 = 「组不冲突」+「当前窗类型在 `MenuFilter` 里」+
    ///       「`TriggerBeforeOnboardingEnds` 或 onboarding 已结束」+「有当前窗」；</item>
    /// <item>触发**之后**：`BlockActionsFromSameGroup` ⇒ 本动作的组塞进 `usedGroups`；
    ///       `BlockAllActions` ⇒ 置 `blockAllActions`；非 `Recurring` 触发后从 `actions` 摘除。</item>
    /// </list></para>
    ///
    /// <para>⚠️ **如实简化（⛔ 不是「已经一样」）**：
    /// ① 原版是 **协程**（`Routine`）驱动，触发一条 yield 一帧；我们是**同步 drain**（入队时 + 每帧泵各重试一次）。
    /// ② `blockAllActions` 的**清零时机**原版挂在场景切换（`StopMenuActions`/`ResumeMenuActions`）上；我们没有那条总线
    ///    ⇒ 提供 <see cref="ResetBlocking"/> 手动清（目前 13 份 SO 没有任何 `BlockAllActions` 动作，不会卡）。
    /// ③ `CanTrigger` 里原版那两道「onboarding 完成 / 某静态开关」判据，我们用可注入的 <see cref="OnboardingEnded"/>（默认 true）。</para>
    /// </summary>
    public static class MenuActions
    {
        static readonly List<MenuAction> actions = new List<MenuAction>();
        static readonly List<string> usedGroups = new List<string>();
        static bool blockAllActions;
        static bool draining;

        /// <summary>onboarding 是否已结束（原版 `CanTrigger` 的 `SegmentationController.CheckTag(0x14)`）。
        /// 默认 `true`（离线没有 onboarding 阶段状态）；⛔ 别为了「看着像」改成 false。</summary>
        public static Func<bool> OnboardingEnded = () => true;

        /// <summary>原版 `Routine` 的排序键 `&lt;&gt;9__10_0`：`x =&gt; x.Priority`（降序）。此处只暴露条数。</summary>
        public static int Count { get { return actions.Count; } }
        public static bool IsBlockingAll { get { return blockAllActions; } }

        public static bool Contains(MenuAction a) { return a != null && actions.Contains(a); }

        /// <summary>原版 `QueueAction(Action action, ActionRule rule=0, int priority=0, string[] groups,
        /// bool triggerBeforeOnboardingEnds=false, bool forceRun=false, bool recurring=false, Type[] menuFilter)`。</summary>
        public static MenuAction QueueAction(Action action, ActionRule rule = ActionRule.DontBlock, int priority = 0,
            string[] groups = null, bool triggerBeforeOnboardingEnds = false, bool forceRun = false,
            bool recurring = false, Type[] menuFilter = null)
        {
            var m = new MenuAction
            {
                Callback = action, ActionRule = rule, Priority = priority, Groups = groups,
                TriggerBeforeOnboardingEnds = triggerBeforeOnboardingEnds, ForceRun = forceRun,
                Recurring = recurring, MenuFilter = menuFilter,
            };
            Enqueue(m);
            return m;
        }

        /// <summary>原版 `QueueActionWithCallback(Action&lt;Action&gt; action, …)` —— 异步回调版（LiveOps 弹窗走这一支）。</summary>
        public static MenuAction QueueActionWithCallback(Action<Action> action, ActionRule rule = ActionRule.DontBlock, int priority = 0,
            string[] groups = null, bool triggerBeforeOnboardingEnds = false, bool forceRun = false,
            bool recurring = false, Type[] menuFilter = null)
        {
            var m = new MenuAction
            {
                AsyncCallback = action, ActionRule = rule, Priority = priority, Groups = groups,
                TriggerBeforeOnboardingEnds = triggerBeforeOnboardingEnds, ForceRun = forceRun,
                Recurring = recurring, MenuFilter = menuFilter,
            };
            Enqueue(m);
            return m;
        }

        static void Enqueue(MenuAction m)
        {
            if (m == null) return;
            actions.Add(m);
            TryDrain();      // 原版：QueueAction 末尾调 CheckActiveMenus（没有在跑就起协程）
        }

        /// <summary>原版 `public static void ReleaseAction(MenuActions.Action action) => actions.Remove(action);`
        /// —— **全局静态、按句柄撤销**。重复释放安全。</summary>
        public static void ReleaseAction(MenuAction action)
        {
            if (action == null) return;
            actions.Remove(action);
        }

        /// <summary>原版 `BlockGroups(string[] groups)`：`usedGroups.AddRange(groups)`。</summary>
        public static void BlockGroups(string[] groups)
        {
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++) if (!string.IsNullOrEmpty(groups[i])) usedGroups.Add(groups[i]);
        }

        /// <summary>清掉「已用组」与「全封锁」标记（等价原版 `ResumeMenuActions` 里对 `usedGroups`/`blockAllActions` 的清零）。</summary>
        public static void ResetBlocking()
        {
            usedGroups.Clear();
            blockAllActions = false;
        }

        /// <summary>推进一次队列（原版 `Routine` 的主循环体）。可重入保护：drain 过程中再调直接返回。
        /// 每条动作在**一次** drain 里最多触发一次（防 `Recurring` 自旋）。</summary>
        public static void TryDrain()
        {
            if (draining || MenuActionsPause.Paused) return;      // 🆕 B90-N3：原版 `StopMenuActions` 期间队列不跑
            draining = true;
            try
            {
                var triggeredThisPass = new HashSet<MenuAction>();
                while (true)
                {
                    MenuAction pick = null;
                    for (int i = 0; i < actions.Count; i++)
                    {
                        var a = actions[i];
                        if (a == null || triggeredThisPass.Contains(a)) continue;
                        if (!CanTriggerNow(a)) continue;
                        if (pick == null || a.Priority > pick.Priority) pick = a;   // 降序；同优先级保持入队序（稳定）
                    }
                    if (pick == null) break;
                    triggeredThisPass.Add(pick);
                    Trigger(pick);
                }
            }
            finally { draining = false; }
        }

        /// <summary>触发条件（原版 `TryTriggerAction` + `CanTrigger` 的合并）：
        /// 组不冲突 · 有当前窗且其类型在 `MenuFilter` 里 · `TriggerBeforeOnboardingEnds || OnboardingEnded()`。
        /// ⚠️ 原版 `currentWindow == null` ⇒ 一律不触发（`UnityEngine.Object.op_Equality` 那一支）；照抄。</summary>
        static bool CanTriggerNow(MenuAction a)
        {
            if (blockAllActions && !a.ForceRun) return false;      // 原版 Routine：blockAllActions 时暂停（ForceRun 豁免）
            if (a.Groups != null)
            {
                for (int i = 0; i < a.Groups.Length; i++)
                    if (!string.IsNullOrEmpty(a.Groups[i]) && usedGroups.Contains(a.Groups[i])) return false;
            }
            var wm = WindowsManager.Instance;
            var win = wm == null ? null : wm.currentWindow;
            if (win == null) return false;                         // 原版：没有当前窗 ⇒ 不触发
            if (a.MenuFilter != null && Array.IndexOf(a.MenuFilter, win.GetType()) < 0) return false;
            if (!a.TriggerBeforeOnboardingEnds && OnboardingEnded != null && !OnboardingEnded()) return false;
            return true;
        }

        static void Trigger(MenuAction a)
        {
            if (a.Callback != null)
            {
                try { a.Callback(); }
                catch (Exception e) { Debug.LogError("[MenuActions] 动作回调抛异常：" + e); }
                Complete(a);
            }
            else if (a.AsyncCallback != null)
            {
                a.AsyncCallback(() => Complete(a));       // 原版 b__1：完成时摘除 + 应用 ActionRule + onComplete(true)
            }
            else
            {
                Complete(a);                              // 没有回调：直接算完成
            }
        }

        static void Complete(MenuAction a)
        {
            if (a == null) return;
            if (!a.Recurring) actions.Remove(a);
            if (a.ActionRule == ActionRule.BlockActionsFromSameGroup && a.Groups != null)
            {
                for (int i = 0; i < a.Groups.Length; i++) if (!string.IsNullOrEmpty(a.Groups[i])) usedGroups.Add(a.Groups[i]);
            }
            else if (a.ActionRule == ActionRule.BlockAllActions)
            {
                blockAllActions = true;
            }
            TryDrain();                                    // 原版：onComplete(true) ⇒ 协程继续下一轮
        }

        /// <summary>自检收尾：清空队列与封锁状态。</summary>
        public static void ResetForTest()
        {
            actions.Clear();
            usedGroups.Clear();
            blockAllActions = false;
            draining = false;
        }
    }

    // ============================================================ 弹窗呈现缝（原版 `LiveOpsAssetUtility.GetComponentReference` + `WindowsManager.OpenWindow`）
    /// <summary>把「事件 + `PopupOptions.PopupLabel`」解析成**要开的窗**（原版
    /// `WindowsManager.OpenWindow(LiveOpsAssetUtility.GetComponentReference(ev, label), ev, closeAll:false, default)`）。
    /// <para>⚠️ **2026-10-27 更正**：原写「本地只解到 `EventComponents` 的 GUID、没解到 prefab 名」——**不成立**；GUID→prefab 名本地**可解**：`bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 `m_Container` 给 GUID→`m_PathID`，再按 `GameObject/*.json` 的 `m_Component` 反查根名（实读表 → 类尾 `LiveOpsLocalBoot` 的 doc + `判据/B88_P1两条判据.md` §3.3）。
    /// 真正的离线差异只剩「原版由 Addressables 在**那次调用**里异步加载 prefab，我们同步建窗」。
    /// ⇒ 这一跳仍然是可注入的缝：**钩子为空时 `ShowTriggerEventPopup` / `ExpireEvent` 出声、不出窗，但排期链照走**
    /// （原版拿不到窗会 NRE，我们不让整条刷新链卡死）；生产装配点 = 类尾 `LiveOpsLocalBoot.BootLocalLiveOps`。</para>
    /// <para>装上真实解析器时：返回一个**已 Open 的 `GameWindow`**（`ShowTriggerEventPopup` 会给它挂 `OnCloseWindow += MarkAsSeen`）。</para></summary>
    public static class LiveOpsPopupPresenter
    {
        /// <summary>(事件, 弹窗选项) → 已开的窗（返回 null / 未装 ⇒ 不出窗）。</summary>
        public static Func<ILiveOps, PopupOptions, GameWindow> Open;
        public static void ResetForTest() { Open = null; }
    }

    // ============================================================ 信号（原版 `Scaffold.Core.Events.Signal` + `EventExpiredSignal`）
    /// <summary>原版 `ExpireEvent.g__HandleOnComplete` 里 `Signal.Raise(new EventExpiredSignal(id))` 的等价物。
    /// <para>`EventExpiredSignal` 的成员是**事件 id**（cpp2il 现读：`new EventExpiredSignal(eventID)`）。我们工程没有
    /// `Scaffold.Core.Events.Signal` 总线 ⇒ 用静态事件承载；**目前没有订阅方**（如实记）。</para></summary>
    public static class LiveOpsSignals
    {
        /// <summary>某个活动过期的通知（参数 = 事件 id）。</summary>
        public static event Action<string> EventExpired;
        /// <summary>是否打印「离线跳过」这类过程日志（诊断用；默认 true）。</summary>
        public static bool Verbose = true;
        public static void RaiseEventExpired(string eventId)
        {
            var h = EventExpired;
            if (h != null)
            {
                try { h(eventId); }
                catch (Exception e) { Debug.LogError("[LiveOps] EventExpired 订阅方抛异常：" + e); }
            }
        }
        public static void ResetForTest() { EventExpired = null; Verbose = true; }
    }

    // ============================================================ 生产装配（2026-10-27 · B88 · 判据 J4）
    /// <summary>把「本地活动目录 → handler 装载 → 弹窗呈现」这条链**在生产里装起来**。
    ///
    /// <para>**为什么需要它**（2026-10-27 现查，两处缺口、缺一不可）：
    /// ① `LiveOpsPopupPresenter.Open` 全仓**只有自检赋值**（`Shell/LiveOpsFilter.cs:1206/1420/1461/1545`）
    ///    ⇒ `ShowTriggerEventPopup`(`Shell/LiveOpsHandler.cs:387`) 与 `ExpireEvent`(`:440`) 两个分支永远走「出声、不出窗」；
    /// ② 上游 `LiveOpsLocalHandler.LoadFromCatalog`（`Shell/LiveOpsHandler.cs:897`）**0 调用者** ⇒ `events` 空
    ///    ⇒ `ScheduleEventPopups` 排 0 条 ⇒ 连 `ShowTriggerEventPopup` 的 `ContainsKey` 都过不去。</para>
    ///
    /// <para>判据链（原版）：`SetupEvents` 尾 → `ScheduleEventPopups`
    /// （`decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c:170-171`）→ `QueueEventPopup`
    /// → `MenuActions` 协程（`MenuActions__CheckActiveMenus.c:30,35` + `MenuActions__TryTriggerAction.c:37-60`）
    /// → `ShowTriggerEventPopup`（`decomp_generic_trial/out/LiveOpsHandler_object___ShowTriggerEventPopup__1019560.c:39-47`：
    /// `LiveOpsAssetUtility.GetComponentReference(ev, options.PopupLabel(+0x10))` → `WindowsManager.OpenWindow(…)`）
    /// → `GetComponentReference` = `ev.Components(+0x38)` 里 `FirstOrDefault(c => c.Label == label)`，
    /// **找不到 ⇒ `CustomDebug.LogWarning` + 返回 null**（`decomp_full/Everguild.LiveOps.LiveOpsAssetUtility__GetComponentReference.c`
    /// + `…__GetReference_Int32Enum__object_.c`）。</para>
    ///
    /// <para>🔴 **GUID → prefab 名本地可解**（⚠️ 2026-10-27 更正：原记录写「只解到 GUID、没解到 prefab 名」）：
    /// `bundle_menus_assets_all/AssetBundle/AssetBundle_1.json` 的 **`m_Container`** 直接给「AssetReference GUID → `m_PathID`」，
    /// 再按 `GameObject/*.json` 的 `m_Component` 反查根名（`m_Name`）。实读：
    /// `e4c251967ff1a79468e8bd25b86b15b8 = Base Event Popup` ·
    /// `44c1eb3a347b840fd9e2e67ba85b7d02 = Full Screen Event Announcement Variant` ·
    /// `be526b22c0924f54991b776d86261fed = Base Expiration Popup` ·
    /// `2d7ec1a46a19245798bf28a5d1a030bb = Event Help Popup Variant`（label 30，**不经本缝**，走 `LiveOpsEventWindow.OpenHelp`）。
    /// 逐条 → `资料/完全复刻/判据/B88_P1两条判据.md` §3.3。</para></summary>
    public static class LiveOpsLocalBoot
    {
        /// <summary>已装过（`LoadFromCatalog` 的幂等闸）。编辑器里 `RuntimeInitializeOnLoadMethod` 每次 Play 都会跑一次。</summary>
        static bool _booted;

        /// <summary>启动装配（= 原版「服务端下发 payload → `LiveOpsManager.BuildHandler` → `SetupEvents`」那一跳的离线等价）。
        /// <para>时机 = `AfterSceneLoad`（同形先例 `Shell/SettingsWindow.cs:6413`）；**只跑一次**，不接帧泵 ——
        /// 菜单队列的每帧重试由同文件的 `LiveOpsPump`（`:1044` 那个 `BeforeSceneLoad` 安装）负责，⛔ 别在这里再装一条泵。</para></summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void BootLocalLiveOps()
        {
            if (_booted) return;
            _booted = true;
            LiveOpsPopupPresenter.Open = OpenEventPopup;                        // ① 弹窗呈现缝（原版 `GetComponentReference` + `WindowsManager.OpenWindow`）
            LiveOpsLocalHandler.Default.LoadFromCatalog(LiveOpsClock.NowUtc);   // ② 装载（内部含 ③ 状态过滤，见 ShouldLoadEvent）
        }

        /// <summary>自检 / 诊断用：允许下一次重新装配。</summary>
        public static void ResetForTest() { _booted = false; }

        // 🔶 自己设计（非原版）：装载口的**状态过滤**（原版客户端不做这件事）· 为什么必须自己定：原版 `events` 是**服务端下发的「当前有效」集合**，客户端不筛（`ScheduleEventPopups` 只读 `ShowWindowOnTrigger`/`WasSeen`/已排/`EventState`），我们替代服务端就必须在**投喂口**滤掉无效活动 —— 改判定支会造出原版没有的行为 · 调度台 2026-10-27 裁定（「日期过滤放在数据装载口，⛔ 不许改 `ScheduleEventPopups` 的判定支」）
        /// <summary>装载口的状态过滤：**滤掉 `Expired(4)` 与 `NotAvailable(-1)`**，其余照原版保留。
        ///
        /// <para>**为什么滤这两档**：
        /// · `Expired(4)` —— `GetState` 的过期边界是 `expirable ? expireDate : endDate`
        ///   （`decomp_full/Everguild.LiveOps.LiveOpsEventData__GetState.c:20-38`）；已经彻底结束的活动原版根本收不到。
        /// · `NotAvailable(-1)` = **`enabled == false`** —— `GetState` 第一句就是
        ///   `if (*(char *)(this + 0x22) == '\0') return -1;`（同上 `:16-18`），`IsActive` 也读同一个 `+0x22`
        ///   （`…__IsActive.c:7-10`）⇒ 客户端把它当「不可激活」。而 `ScheduleEventPopups` **不查 `enabled`**
        ///   ⇒ 真让它进 `events`，`Dark Zealots`（`enabled=0` 且 `ShowWindowOnTrigger=1`）会**弹一个原版不会弹的公告**。
        /// ⇒ 按「不可激活 ⇒ 服务端不下发」在装载口排除。</para>
        ///
        /// <para>**保留**：`Finished(3)`（过期弹窗是原版行为：`ScheduleEventPopups` 的过期支只在 `EventState == Finished` 排，
        /// 判据 `…__ScheduleEventPopups__1018d60.c:57`）；`Planned(0)` / `OnPreview(1)` 也照原版保留（本机没有这种数据，
        /// ⛔ 不额外发明筛法）。</para>
        ///
        /// <para>⚠️ **还没查清**：`enabled=false` 在**服务端**那一侧到底「下不下发」，本地**没有判据**（那是服务端逻辑，反编译里没有）。
        /// 上面能证的只有客户端侧（`GetState`/`IsActive` 一律当不可激活 + `ScheduleEventPopups` 不查它）。</para></summary>
        public static bool ShouldLoadEvent(LiveOpsLocalEvent local, DateTime utc)
        {
            if (local == null) return false;
            var data = LocalLiveOpsEvent.MapData(local);          // 纯 POCO：不建存档、不登记 handler
            var state = data == null ? LiveOpState.NotAvailable : data.GetState(utc);
            return state != LiveOpState.Expired && state != LiveOpState.NotAvailable;
        }

        /// <summary>GUID → 我们已建的弹窗变体（值 = 2026-10-27 实读的 prefab 根名，见类注释那张表）。</summary>
        static readonly Dictionary<string, EventPopupVariant> _popupVariantByGuid = new Dictionary<string, EventPopupVariant>
        {
            { "e4c251967ff1a79468e8bd25b86b15b8", EventPopupVariant.BaseEvent },               // Base Event Popup
            { "44c1eb3a347b840fd9e2e67ba85b7d02", EventPopupVariant.FullScreenAnnouncement },  // Full Screen Event Announcement Variant
            { "be526b22c0924f54991b776d86261fed", EventPopupVariant.BaseExpiration },          // Base Expiration Popup
        };

        // 🔶 自己设计（非原版）：原版 `GetComponentReference` 返回 `AssetReference`，由 Addressables 在**那次调用**里异步加载 prefab；我们没有 Addressables ⇒ 同步按 GUID 建我们自己的 `BaseEventPopup` · 为什么必须自己定：GUID↔prefab 名已实测（类注释），但「实例化路径」本地没有原版实现可抄 · 调度台 2026-10-27 裁定（施工清单 → `资料/完全复刻/判据/B88_P1两条判据.md` §3.5）
        /// <summary>= 原版 `LiveOpsAssetUtility.GetComponentReference(ev, label)` + `WindowsManager.OpenWindow(reference, ev, false, default)`。
        /// 返回**已经 Open** 的窗（`ShowTriggerEventPopup` 会往上挂 `OnCloseWindow += MarkAsSeen`；没开就挂 = 永不触发）。
        /// <para>词条从**这个事件自己的 `Labels`** 取（tag = 0/1，过期变体 10/11）——
        /// ⛔ 别用 `LiveOpsLocalCatalog.TryResolvePopupLabels`：它挑的是「当前在期的那个事件」，会把 A 事件的文案贴到 B 事件上。</para></summary>
        public static GameWindow OpenEventPopup(ILiveOps ev, PopupOptions opts)
        {
            if (ev == null || opts == null) { Debug.LogError("[LiveOps] OpenEventPopup(null) —— 原版这里会 NRE"); return null; }

            // 第 8 步等价：ev.Components 里按 PopupLabel 找 GUID（原版 FirstOrDefault(c => c.Label == label)）
            var local = (ev as LocalLiveOpsEvent)?.Local;
            string guid = null;
            if (local != null && local.Components != null)
                for (int i = 0; i < local.Components.Length; i++)
                    if (local.Components[i].Key == opts.PopupLabel) { guid = local.Components[i].Value; break; }

            EventPopupVariant variant;
            if (string.IsNullOrEmpty(guid) || !_popupVariantByGuid.TryGetValue(guid, out variant))
            {
                // 原版同形：找不到组件 ⇒ LogWarning + 返回 null（不开窗）
                Debug.LogWarning("[LiveOps] 事件 '" + ev.GetID() + "' 没有 label=" + opts.PopupLabel + " 的组件"
                               + "（或它的 prefab 不在本地表里）⇒ 不开窗（原版 `GetComponentReference` 找不到时 LogWarning + 返回 null）");
                return null;
            }

            int titleTag = variant == EventPopupVariant.BaseExpiration ? (int)LiveOpsLabelRef.EndTitle : (int)LiveOpsLabelRef.Title;
            int descTag = variant == EventPopupVariant.BaseExpiration ? (int)LiveOpsLabelRef.EndDescription : (int)LiveOpsLabelRef.Description;
            string tTerm = null, dTerm = null; string[] tArgs = null, dArgs = null;
            LiveOpsLocalLabel tl, dl;
            if (local != null && local.TryGetLabel(titleTag, out tl)) { tTerm = tl.Term; tArgs = LiveOpsLocalCatalog.ResolveArgs(tl.Args); }
            if (local != null && local.TryGetLabel(descTag, out dl)) { dTerm = dl.Term; dArgs = LiveOpsLocalCatalog.ResolveArgs(dl.Args); }

            var wm = WindowsManager.Get();     // ⛔ 别直读 `Instance`：切到主菜单之后它是 null（见 `WindowsManager.Get` 的 doc）
            if (wm == null) { Debug.LogError("[LiveOps] 没有 WindowsManager ⇒ 开不了事件弹窗（原版走 `WindowsManager.Instance.OpenWindow`）"); return null; }
            var win = BaseEventPopup.Create(wm, variant, tTerm, dTerm, ev.GetID(), tArgs, dArgs);
            wm.OpenWindow(win);                // 同族先例：`LiveOpsEventWindow.OpenHelp` 那一跳
            Debug.Log("[LiveOps] 事件弹窗 ⇒ `" + BaseEventPopup.RootNameOf(variant) + "`（事件 " + ev.GetID() + " · label " + opts.PopupLabel + "）");
            return win;
        }
    }
}
// ============================================================ 🆕 2026-10-28（B90 · N3，追加在文件尾）
//   **原版 `MenuActions.StopMenuActions(string sceneTarget)` / `ResumeMenuActions(string scene)` 的等价物。**
//
//   判据（第一权威 = 反汇编方法体，逐句）：
//   · `MenuActions__StopMenuActions.c`：① 从 `WindowsManager.Instance` 的**场景加载委托**(`+0x70`) 上
//     `Delegate.Remove` 掉这一跳；② `StopCoroutine(this+0x20)`（在跑的那条 `Routine`）；③ `this+0x20 = null`。
//     ⇒ 语义 = 「**这个场景期间队列不要再跑**」。
//   · `MenuActions__ResumeMenuActions.c`：① `this+0x28` 那两字节清零（= `blockAllActions` 一族）；
//     ② `usedGroups(this+0x30).Clear()`；③ 把委托 `Delegate.Combine` 回 `+0x70`；④ 收尾 `CheckActiveMenus`
//     ⇒ 语义 = 「清掉封锁记忆 + 重新开始排」。
//
//   ⚠️ **如实简化（⛔ 不是「已经一样」）**：原版那对**委托**挂在「某个具体场景加载完」上（参数 `sceneTarget`
//     就是那条的过滤键）；我们仓里没有同一根场景加载总线接得到那个位置 ⇒ 这里落成**一个显式闸**
//     （`Paused`）+ 记住「为哪个场景停的」，由 `TryDrain` 首句判它（`Shell/LiveOpsHandler.cs` 的 `MenuActions.TryDrain`）。
//     ⇒ 行为上「Stop 之后入队/每帧泵都不触发、Resume 之后照旧触发」与原版一致；差别只在**谁是恢复的触发者**
//     （原版 = 场景加载完成；我们 = 调用方显式 `ResumeMenuActions`）。
//   ⛔ 没做成 `MenuActions` 的成员：那会插在 `MenuActions` 类中间/类尾，而类尾之后还有别的类挂着账的落点
//     （`日常_奖励.tsv` 指进 `LiveOpsHandler.cs:1538/1561/1580/1588/1597`）⇒ 加行会让它们整体漂移。
namespace CardPresentation
{
    /// <summary>菜单动作队列的「暂停」闸（原版 `MenuActions.StopMenuActions` / `ResumeMenuActions`）。
    /// 判据与两处如实简化 → 文件尾那段注释。</summary>
    public static class MenuActionsPause
    {
        /// <summary>暂停中（= 原版 `StopMenuActions` 已把场景委托摘掉、协程也停了）。`MenuActions.TryDrain` 首句判它。</summary>
        public static bool Paused { get; private set; }
        /// <summary>为哪个场景停的（原版 `sceneTarget` 实参）。诊断/断言用。</summary>
        public static string PausedFor { get; private set; }

        /// <summary>原版 `MenuActions.StopMenuActions(string sceneTarget)` —— 摘场景委托 + 停协程。
        /// 我们的等价物 = 置闸（`TryDrain` 首句看它）+ 记住场景名。幂等（原版 `Delegate.Remove` 也是幂等）。</summary>
        public static void StopMenuActions(string sceneTarget)
        {
            if (Paused && PausedFor == sceneTarget) return;
            Paused = true;
            PausedFor = sceneTarget;
            Debug.Log("[MenuActions] 暂停菜单动作（原版 `StopMenuActions`）—— 场景 `"
                    + (sceneTarget ?? "<null>") + "` 期间队列不跑（已排的 " + MenuActions.Count + " 条留在队列里）");
        }

        /// <summary>原版 `MenuActions.ResumeMenuActions(string scene)` —— `blockAllActions` 清零 +
        /// `usedGroups.Clear()` + 挂回场景委托 + `CheckActiveMenus`。我们的等价物 =
        /// 清闸 → `MenuActions.ResetBlocking()`（就是那两句）→ `TryDrain()`（= `CheckActiveMenus`）。</summary>
        public static void ResumeMenuActions(string scene)
        {
            bool was = Paused;
            Paused = false;
            PausedFor = null;
            MenuActions.ResetBlocking();      // 原版 Resume 的头两句（`blockAllActions` + `usedGroups` 清零）
            MenuActions.TryDrain();           // 原版末句 `CheckActiveMenus`
            Debug.Log("[MenuActions] 恢复菜单动作（原版 `ResumeMenuActions`，场景 `" + (scene ?? "<null>")
                    + "`）—— 之前是" + (was ? "暂停" : "未暂停") + "，队列现 " + MenuActions.Count + " 条");
        }

        /// <summary>自检收尾（⚠️ 只给断言用）。</summary>
        public static void ResetForTest() { Paused = false; PausedFor = null; }
    }
}
