// LiveOpsManager.cs —— **活动总管理**（原版 `LiveOpsManager` : `AsyncInitializableBehaviour` 的等价实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：`资料/完全复刻/判据/泛型与启动链.md` §四 `LiveOpsHandler<TEvent>` 的「调用者」一段
//   + 账 `日常_奖励.tsv` 里 `LiveOpsManager` 那 9 条 P1（`BuildHandler` / `BuildHandlers` / `GetAllBlockedEvents` /
//   `GetHandler(Type)` / `GetHandler<T>()` / `Initialize` / `InitializeHandlers` / `ResetHandlers` / `Unpack`）。
// 原版真身（反编译，机器可读）：
//   · `d:/2/tools/decomp_full/LiveOpsManager__.ctor.c` · `…__Initialize.c` · `…__BuildHandlers.c` · `…__BuildHandler.c`
//   · `…/LiveOpsManager._Unpack_d__15__MoveNext.c` · `…_BuildHandlers_d__16__MoveNext.c` · `…_InitializeHandlers_d__18__MoveNext.c`
//   · `…/LiveOpsManager__GetHandler.c` / `…__GetHandler_object_.c` / `…__GetAllBlockedEvents.c` / `…__ResetHandlers.c`
//   · 字段表：`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/LiveOpsManager.cs`（`static Dictionary<Type,ILiveOpsHandler>
//     HandlerLookup` · `[SerializeField] readonly LiveOpsEventFactory eventFactory`(0x28) · `[SerializeField] bool waitingForHandlers`(0x30)）
//
// ⚠️ **与原版的如实差异**（⛔ 不是「已经一样」）：
//   ① 原版 `HandlerLookup` 的内容来自 **`GetComponentsInChildren<ILiveOpsHandler>()`**（handler 是挂在 prefab 上的组件）。
//      我们没有那种 prefab/场景挂接 ⇒ 由 <see cref="Register"/> 显式注册（`LiveOpsLocalHandler.Default` 是唯一一个）。
//      **`HandlerKeys` 的那句 `GetComponentsInChildren` 因此没有等价物** —— 这条差异写进账里。
//   ② 原版 `Instance` 是 `SingletonBehaviour<LiveOpsManager>`（场景里的组件）；我们是进程级懒加载单例。
//   ③ `LiveOpsFeatureConfig`（原版 `Everguild.LiveOps.Config.FeatureConfig`，远端配置）**字段判据没解出来**
//      ⇒ 只留一个**最小占位**（见该类注释），`BuildHandler` 里只用来判「这个 handler 开不开」。⛔ 别把它当成已复刻。
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 事件对（原版 `RawEvent`）
    /// <summary>原版 `RawEvent`：`{ LiveOpsEventData Data; LiveOpsEventPersistence Persistence; }`
    /// （字段偏移 +0x10 / +0x18；`Data` 只读、`Persistence` 可写 —— 后者正是 `UpdateCachedSave` 换的那一格）。</summary>
    public sealed class RawEvent
    {
        public LiveOpsEventData Data { get; private set; }
        public LiveOpsSave Persistence { get; set; }
        public RawEvent(LiveOpsEventData data, LiveOpsSave persistence) { Data = data; Persistence = persistence; }
    }

    // ============================================================ handler 载荷（原版 `LiveopsHandlerData`）
    /// <summary>原版 `LiveopsHandlerData`：`{ JObject SharedData; List&lt;RawEvent&gt; RawEvents; }`（字段 +0x10 / +0x18，
    /// 两个 setter 都是 private）。**它 = 服务器给某一个 handler 的那一包数据**。</summary>
    public sealed class LiveopsHandlerData
    {
        /// <summary>跨事件共享块（原版 `JObject`）⇒ `<see cref="LiveOpsJson"/>`。</summary>
        public LiveOpsJson SharedData { get; private set; }
        /// <summary>本 handler 下的事件对（数据 + 存档）。</summary>
        public List<RawEvent> RawEvents { get; private set; }

        public LiveopsHandlerData(LiveOpsJson sharedData, List<RawEvent> rawEvents)
        {
            SharedData = sharedData;
            RawEvents = rawEvents;
        }
    }

    // ============================================================ 远端配置（原版 `FeatureConfig`）—— **最小占位**
    /// <summary>🔴 **占位，判据未解**：原版是 `Everguild.LiveOps.Config.FeatureConfig`（远端配置，`ConfigManager.GetConfig&lt;FeatureConfig&gt;()`），
    /// 它的字段/方法在本地**没有解出来**（`判据` 里也没有它的方法表）。
    /// <para>这里只提供 `BuildHandler` 真正用到的那一个判定（「这个 handler 开不开」），默认恒 true。
    /// ⛔ 别在本类上继续加「看起来合理」的字段 —— 那就成了「凭印象设计」（铁律 3）。</para></summary>
    public sealed class LiveOpsFeatureConfig
    {
        public static readonly LiveOpsFeatureConfig Default = new LiveOpsFeatureConfig();
        /// <summary>当前生效的一份（启动链将来从远端配置建；现在恒 <see cref="Default"/>）。</summary>
        public static LiveOpsFeatureConfig Current = Default;

        /// 🔶 自己设计（非原版）：`IsHandlerEnabled` 恒 true · 原版真值在远端配置里 · TODO(谁定的)｜原注：<summary>某个 handler 是否启用。⚠️ **口径是我们挑的**（原版真值在远端配置里）——账里已标。</summary>
        public bool IsHandlerEnabled(string handlerKey) { return true; }
    }

    // ============================================================ 总管理（原版 `LiveOpsManager`）
    /// <summary>活动总管理：持有 handler 注册表 + 事件工厂，负责「拿服务器载荷 → 建 handler → 初始化 → 注册」。
    /// <para>只读口（`GetHandler` / `GetAllBlockedEvents`）是**静态**的（原版 `HandlerLookup` 是 static 字段）。</para></summary>
    public sealed class LiveOpsManager
    {
        /// <summary>handler 注册表（原版 `private static Dictionary&lt;Type, ILiveOpsHandler&gt; HandlerLookup`）。</summary>
        static readonly Dictionary<Type, ILiveOpsHandler> HandlerLookup = new Dictionary<Type, ILiveOpsHandler>();

        /// <summary>原版 `[SerializeField] private readonly LiveOpsEventFactory eventFactory`（+0x28）。</summary>
        public readonly LiveOpsEventFactory eventFactory = new LiveOpsEventFactory();

        /// <summary>原版 `[SerializeField] private bool waitingForHandlers`（+0x30，ctor 里置 **true**）：
        /// `Initialize()` 协程**阻塞等**它变 false（= handlers 建齐）。</summary>
        bool waitingForHandlers = true;

        bool _initialized;

        static LiveOpsManager _instance;
        /// <summary>进程级单例（原版 `SingletonBehaviour&lt;LiveOpsManager&gt;`；见文件头差异 ②）。🔴 建单例时登记**内建 handler**（原版靠 `GetComponentsInChildren&lt;ILiveOpsHandler&gt;()` 发现它们，差异 ①）：`LiveOpsLocalHandler.Default`（key `"local"`）→ `new GameModesHandler()`（key `"GameModes"`），**注册序不变**（新的只**追加**在后面；🆕 2026-10-28 B89-E11b；判据 = 原版 `GameModes : LiveOpsHandler&lt;IPlayEvent&gt;` 由启动链 `BuildHandlers` 建成并登记，`LiveOpsManager.cs:2029`）。</summary>
        public static LiveOpsManager Instance
        {
            get { if (_instance == null) { _instance = new LiveOpsManager(); Register(LiveOpsLocalHandler.Default); Register(new GameModesHandler()); } return _instance; }
        }

        // ── 注册表读写 ──────────────────────────────────────────────────────────
        /// <summary>我们额外需要的口（原版靠 `GetComponentsInChildren` 发现 handler；见文件头差异 ①）。
        /// 幂等：同一个 handler 重复注册只会覆盖（`HandlerLookup[type] = handler`，与原版 `BuildHandler` 尾那一句同义）。</summary>
        public static void Register(ILiveOpsHandler handler)
        {
            if (handler == null) return;
            HandlerLookup[handler.GetType()] = handler;
        }

        /// <summary>当前单例（没建过就是 null；自检要看「建过没有」时用它）。</summary>
        public static LiveOpsManager InstanceOrNull { get { return _instance; } }

        /// <summary>原版 `public IReadOnlyList&lt;string&gt; HandlerKeys`（`:1726` 上方）：
        /// `GetComponentsInChildren&lt;ILiveOpsHandler&gt;().Select(h =&gt; h.HandlerKey).ToList()`。
        /// ⚠️ 我们换成「注册表里有哪些 key」（差异 ①）。</summary>
        public static IReadOnlyList<string> HandlerKeys
        {
            get
            {
                var list = new List<string>();
                foreach (var kv in HandlerLookup) if (kv.Value != null) list.Add(kv.Value.HandlerKey);
                return list;
            }
        }

        /// <summary>原版 `public static void ResetHandlers()`（`:1648`，**真身** `LiveOpsManager__ResetHandlers.c`）：
        /// <code>
        /// var snapshot = HandlerLookup.Values.ToList();
        /// foreach (var h in snapshot) h.ClearEvents();      // ← 接口槽 **7**
        /// HandlerLookup.Clear();
        /// </code>
        /// 🔴 **2026-10-28（B93_E12）坐实：那个虚调用是 `ClearEvents()`，不是 `ResetEvents()`。** 判据 = `d:/2/tools/il2cpp_out/dump.cs:53773`
        /// 的 `ILiveOpsHandler` 槽表（`Slot: 7` = `void ClearEvents()`；`ResetEvents()` **根本不在接口上**，它是类方法）+ Ghidra 侧
        /// `…ResetHandlers.c` 的 `(offset + 7) * 0x10` 且只传 `this`+MethodInfo 两个实参。⚠️ 两者语义整条不同（`ClearEvents` = 撤排期 +
        /// 逐个 `Dispose` + 清三表；`ResetEvents` = 逐个 `ResetEventProgress()` 不清表）—— 原来按「名字像 Reset」取错了，已改。</summary>
        public static void ResetHandlers()
        {
            var snapshot = new List<ILiveOpsHandler>(HandlerLookup.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var h = snapshot[i];
                if (h == null) continue;
                h.ClearEvents();                 // 原版 `ILiveOpsHandler` 槽 7（dump.cs:53773）
            }
            HandlerLookup.Clear();
        }

        /// <summary>原版 `public static T GetHandler&lt;T&gt;()`（`:1726`）：`(T)GetHandler(typeof(T))`。</summary>
        public static T GetHandler<T>() where T : class, ILiveOpsHandler
        {
            return GetHandler(typeof(T)) as T;
        }

        /// <summary>原版 `public static ILiveOpsHandler GetHandler(Type type)`（`:1764`，**真身** `LiveOpsManager__GetHandler.c`）：
        /// `if (HandlerLookup.TryGetValue(type, out h)) return h;` 否则
        /// `LogWarning(&lt;前缀&gt; + ReflectionHelper.ReadableName(type) + &lt;后缀&gt;)` 并返回 null。</summary>
        public static ILiveOpsHandler GetHandler(Type type)
        {
            if (type == null) return null;
            ILiveOpsHandler h;
            if (HandlerLookup.TryGetValue(type, out h)) return h;
            Debug.LogWarning("[LiveOpsManager] Cannot find LiveOps handler for type '" + type.Name + "'"
                           + "（判据 `LiveOpsManager.cs:1764`：取不到只 LogWarning，⛔ 不抛）");
            return null;
        }

        // ── 汇总读口 ────────────────────────────────────────────────────────────
        /// <summary>原版 `GetAllEvents(includeBlockedEvents)`（`LiveOpsManager__GetAllEvents.c`）：
        /// `HandlerLookup.Values.SelectMany(h =&gt; h.GetBaseEvents(includeBlockedEvents))`。</summary>
        public static IEnumerable<ILiveOps> GetAllEvents(bool includeBlockedEvents = false)
        {
            var outp = new List<ILiveOps>();
            foreach (var kv in HandlerLookup)
            {
                var h = kv.Value;
                if (h == null) continue;
                var it = h.GetBaseEvents(includeBlockedEvents);
                if (it == null) continue;
                foreach (var e in it) if (e != null) outp.Add(e);
            }
            return outp;
        }

        /// <summary>原版 `public IEnumerable&lt;ILiveOps&gt; GetAllBlockedEvents()`（`:1818`，**真身**
        /// `LiveOpsManager__GetAllBlockedEvents.c`）：`HandlerLookup.Values.SelectMany(h =&gt; h.GetBlockedEvents())`。</summary>
        public static IEnumerable<ILiveOps> GetAllBlockedEvents()
        {
            var outp = new List<ILiveOps>();
            foreach (var kv in HandlerLookup)
            {
                var h = kv.Value;
                if (h == null) continue;
                var it = h.GetBlockedEvents();
                if (it == null) continue;
                foreach (var e in it) if (e != null) outp.Add(e);
            }
            return outp;
        }

        /// <summary>原版 `GetEvents(includeBlockedEvents)`（`LiveOpsManager__GetEvents.c`）：
        /// 汇总各 handler 的 base 事件再按条件筛（`Where`）。我们这里只有「是否含 blocked」一个条件。</summary>
        public static IEnumerable<ILiveOps> GetEvents(bool includeBlockedEvents = false)
        {
            return GetAllEvents(includeBlockedEvents);
        }

        /// <summary>原版 `GetEvents&lt;T&gt;(includeBlockedEvents)`：同一份源再按类型筛。</summary>
        public static IEnumerable<T> GetEvents<T>(bool includeBlockedEvents = false) where T : class, ILiveOps
        {
            var outp = new List<T>();
            foreach (var e in GetAllEvents(includeBlockedEvents)) { var t = e as T; if (t != null) outp.Add(t); }
            return outp;
        }

        /// <summary>原版 `GetEvent(string id)`（`LiveOpsManager__GetEvent.c`）：跨 handler 找第一条 id 相同的。</summary>
        public static ILiveOps GetEvent(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var kv in HandlerLookup)
            {
                var h = kv.Value;
                if (h == null) continue;
                var e = h.GetEvent(id);
                if (e != null) return e;
            }
            return null;
        }

        // ── 启动 / 装载 ─────────────────────────────────────────────────────────
        /// <summary>原版 `public IEnumerator Initialize()`（`:2009`，**真身** `LiveOpsManager._Initialize_d__14__MoveNext.c`）：
        /// 协程在 `waitingForHandlers`（+0x30）为 true 时**一直 yield**（`AsyncInitializableBehaviour` 的等待语义）。
        /// 🔴 **别在没有 `Unpack` 的链路上调它** —— 本地没有服务器载荷 ⇒ `waitingForHandlers` 永远是 true ⇒ **死等**。
        /// （原版同样会死等；它的载荷由 `PlayerDataManager` 的登录链灌进来。）</summary>
        public IEnumerator Initialize()
        {
            if (_initialized) yield break;
            _initialized = true;
            if (waitingForHandlers)
                Debug.Log("[LiveOps] LiveOpsManager.Initialize：等 handlers 建齐（`waitingForHandlers = true`）。"
                        + "本地没有服务器载荷 ⇒ 必须由 `Unpack(...)` 或 `MarkHandlersReady()` 放行，⛔ 否则这条协程会一直等下去。");
            while (waitingForHandlers) yield return null;
            Debug.Log("[LiveOps] LiveOpsManager.Initialize：handlers 就绪（" + HandlerLookup.Count + " 个）");
        }

        /// <summary>放行 `Initialize()` 的等待（=「handlers 已经建齐」）。原版由 `InitializeHandlers` 的收尾置位。</summary>
        public void MarkHandlersReady() { waitingForHandlers = false; }

        /// <summary>原版 `public Task Unpack(Dictionary&lt;string, LiveopsHandlerData&gt; dataByHandlerKey)`（`:2020`）。
        /// **真身**（`_Unpack_d__15__MoveNext.c`）：
        /// <code>
        /// var config = ConfigManager.GetConfig&lt;FeatureConfig&gt;();
        /// await BuildHandlers(dataByHandlerKey, config);
        /// await InitializeHandlers(config);
        /// </code></summary>
        public async Task Unpack(Dictionary<string, LiveopsHandlerData> dataByHandlerKey)
        {
            var config = LiveOpsFeatureConfig.Current;
            await BuildHandlers(dataByHandlerKey, config);
            await InitializeHandlers(config);
        }

        /// <summary>原版 `public Task BuildHandlers(Dictionary&lt;string, LiveopsHandlerData&gt;, FeatureConfig)`（`:2029`）。
        /// **真身**（`_BuildHandlers_d__16__MoveNext.c`）：
        /// <code>
        /// var handlers = GetComponentsInChildren&lt;ILiveOpsHandler&gt;();
        /// HandlerLookup = new Dictionary&lt;Type, ILiveOpsHandler&gt;(handlers.Length);
        /// foreach (var h in handlers) {
        ///     HandlerLookup.Add(h.GetType(), h);
        ///     handlerData = dataByHandlerKey.TryGetValue(h.HandlerKey, out d) ? d : null;
        ///     await BuildHandler(h, handlerData, config);
        /// }
        /// </code>
        /// ⚠️ 我们的「handlers 从哪来」是注册表（差异 ①）⇒ 这里遍历的是**已注册**的 handler。</summary>
        public async Task BuildHandlers(Dictionary<string, LiveopsHandlerData> dataByHandlerKey, LiveOpsFeatureConfig config)
        {
            var handlers = new List<ILiveOpsHandler>(HandlerLookup.Values);
            HandlerLookup.Clear();
            for (int i = 0; i < handlers.Count; i++)
            {
                var h = handlers[i];
                if (h == null) continue;
                HandlerLookup[h.GetType()] = h;
                LiveopsHandlerData d = null;
                if (dataByHandlerKey != null) dataByHandlerKey.TryGetValue(h.HandlerKey, out d);
                await BuildHandler(h, d, config);
            }
        }

        /// <summary>原版 `public Task BuildHandler(ILiveOpsHandler handler, LiveopsHandlerData handlerData, FeatureConfig)`（`:2038`）。
        /// **真身**（`_BuildHandler_d__17__MoveNext.c`）：`await &lt;handler 侧装填&gt;` → 用 `eventFactory` 逐个造事件
        /// （`RawEvents` 里的每一对）→ `await handler.SetupEvents(...)` → **`HandlerLookup[handler.GetType()] = handler`**。
        /// <para>🔴 2026-10-28 B91_W7 现读订正（原写「两处推断」，其中两处现已坐实/改写）：① **顺序**：原版先
        /// **`OrderByDescending(raw =&gt; raw.Data.priority)`**（`.c` 的遍历源 = `Enumerable.OrderByDescending`，选择器 = 闭包 `b__17_0`）⇒ 已补；
        /// ② **逐事件特性闸**：原版每条读 `FeatureConfig.CheckFeature(config, raw.Data.eventId)`，没开就**整条跳过** ⇒ 已补（本地无远端配置 ⇒ 放行，见 `EventFeatureGate`）；
        /// ③ ✅ **2026-10-28（B93_E12）live/blocked 分派已定案**（槽 4/8/9/13 全部解出，见方法体内那一段）⇒ 按 `.c` 逐句照抄。</para></summary>
        public async Task BuildHandler(ILiveOpsHandler handler, LiveopsHandlerData handlerData, LiveOpsFeatureConfig config)
        {
            if (handler == null) { Debug.LogError("[LiveOps] BuildHandler：handler 是 null"); return; }
            var cfg = config ?? LiveOpsFeatureConfig.Current;
            if (!cfg.IsHandlerEnabled(handler.HandlerKey))
            {
                Debug.LogWarning("[LiveOps] BuildHandler：handler '" + handler.HandlerKey + "' 被 FeatureConfig 关掉了 ⇒ 不建");
                return;
            }
            await handler.ValidateData(handlerData);

            var ops = new List<ILiveOps>();
            var blockedOps = new List<ILiveOps>();
            var raws = handlerData == null ? null : new List<RawEvent>(handlerData.RawEvents ?? new List<RawEvent>()); if (raws != null) raws.Sort((a, b) => Priority(b).CompareTo(Priority(a)));   // 原版 `OrderByDescending(raw => raw.Data.priority)`（b__17_0）：**先按优先降序再逐个造**
            if (raws != null)
            {
                for (int i = 0; i < raws.Count; i++)
                {
                    var r = raws[i];
                    if (r == null || r.Data == null) continue; if (!EventFeatureEnabled(r.Data.eventId)) continue;   // 原版逐事件闸：`FeatureConfig.CheckFeature(config, raw.Data.eventId)`
                    var ev = eventFactory.CreateEvent(r.Data, r.Persistence, handler, LiveOpsValidationStrategy.Strict);
                    if (ev == null)
                    {
                        Debug.LogWarning("[LiveOps] BuildHandler('" + handler.HandlerKey + "')：事件 '"
                                       + (r.Data.eventId ?? "?") + "' 造不出实例（`LiveopsHelper.TryGetEventFromData` 没匹配到事件类）⇒ 跳过");
                        continue;
                    }
                    // 🔴 2026-10-28（B93_E12）**已坐实**（下面三条槽号全部来自 `d:/2/tools/il2cpp_out/dump.cs:126037` 的 ILiveOps 槽表）：
                    //   Slot 4 = `bool IsActive()` · Slot 8 = `RegisterEventTriggers()` · Slot 9 = `CheckEventFilter()` · Slot 13 = `ToggleEventActivation(bool,bool,bool)`
                    //   判据 = `decomp_full/LiveOpsManager._BuildHandler_d__17__MoveNext.c:186-222`：
                    //     if (!ev.IsActive())                     { ev.RegisterEventTriggers(); blockedEvents.Add(ev); }
                    //     else if (IsTriggeredEvent(ev) || ev.CheckEventFilter()) { events.Add(ev); ev.ToggleEventActivation(true, false, false); }
                    //     else                                    { blockedEvents.Add(ev); }
                    //   （`IsTriggeredEvent` = `data.filter.Triggers.Count > 0`，判据 `decomp_full/LiveOpsUtility__IsTriggeredEvent.c`）
                    //   `events`/`blockedEvents` 的归属：`.c` 的两张表是 `param_1+0x14` / `+0x16`，`SetupEvents(events, blockedEvents, shared)`
                    //   按接口槽 5 的形参序传的是 `(+0x14, +0x16, data.SharedData)` ⇒ `+0x14` = events、`+0x16` = blockedEvents（与上面三条 Add 一致）。
                    if (!ev.IsActive()) { ev.RegisterEventTriggers(); blockedOps.Add(ev); }
                    else if (IsTriggeredEvent(ev) || ev.CheckEventFilter()) { ops.Add(ev); ev.ToggleEventActivation(true, false, false); }
                    else blockedOps.Add(ev);
                }
            }
            // 🔴 2026-10-28（B93_E12）补这一跳：原版 `_BuildHandler_d__17__MoveNext.c` 在 `SetupEvents` **之前**调
            //   `ILiveOpsHandler` 槽 0 = `RegisterManager(LiveOpsManager)`（`(*(code*)*puVar8)(plVar2, lVar9)`，
            //   `lVar9` = `LiveOpsManager` 的单例）。
            //   ⚠️ 原来**全仓没有一处调它** ⇒ `LiveOpsHandler.Manager` 恒 null ⇒ `RefreshHandler` 每次都在
            //   「Manager 是 null ⇒ 无法建 handler」那条错误支上返回（静默的功能死路，`grep RegisterManager` 只有声明）。
            handler.RegisterManager(this);
            await handler.SetupEvents(ops, blockedOps, handlerData == null ? null : handlerData.SharedData);
            HandlerLookup[handler.GetType()] = handler;
        }

        /// <summary>原版 `public Task InitializeHandlers(FeatureConfig config)`（`:2066`）。
        /// **真身**（`_InitializeHandlers_d__18__MoveNext.c`）：`foreach (var h in &lt;handlers&gt;) await h.InitializeHandler();`
        /// （收尾把 `waitingForHandlers` 放行 —— 见 `MarkHandlersReady`）。
        /// ⚠️ 参数 `config` 在原版那个状态机里**没被用到**（只取 handlers 集合），照抄（保留签名、不用它）。</summary>
        public async Task InitializeHandlers(LiveOpsFeatureConfig config)
        {
            var snapshot = new List<ILiveOpsHandler>(HandlerLookup.Values);
            for (int i = 0; i < snapshot.Count; i++)
            {
                var h = snapshot[i];
                if (h == null) continue;
                await h.InitializeHandler();
            }
            waitingForHandlers = false;
        }

        // ── 自检口 ──────────────────────────────────────────────────────────────
        /// <summary>自检收尾：清空注册表 + 丢掉单例 + 让 `Initialize()` 的等待恢复出厂（下次会重新等）。</summary>
        public static void ResetForTest()
        {
            HandlerLookup.Clear();
            _instance = null;
            LiveOpsLocalHandler.ResetForTest();
            LiveOpsFeatureConfig.Current = LiveOpsFeatureConfig.Default;
            LiveOpsScheduler.ResetForTest();
            LiveOpsClock.ResetForTest();
        }

        /// <summary>注册表条数（自检读）。</summary>
        public static int HandlerCountForTest { get { return HandlerLookup.Count; } }

        // ============================================================ 启动链（🆕 2026-10-28 B89-E11c）
        static bool _bootLogged;

        /// <summary>**启动链的唯一入口**（幂等）：把单例建起来 ⇒ 内建 handler 登记进注册表
        /// （`LiveOpsLocalHandler` key `"local"` → `GameModesHandler` key `"GameModes"`，见 <see cref="Instance"/>）。
        /// <para>🔴 **为什么必须有这个口**：2026-10-28 实测 `Instance` **全仓 0 个调用方** ⇒ 整个注册表从来没被填过，
        /// `GetHandler&lt;…&gt;()` 永远是 null（原版那些 `LiveOpsManager.GetHandler&lt;GameModes&gt;()` 的调用形状一条都接不上）。</para>
        /// <para>**原版的等价位置**：`PlayerDataManager` 的登录链 —— `InitializeLiveOpsAndConfig()` → 拿服务器载荷
        /// （`liveOps.Unpack(result.HandlerData)`，`out_cpp2il_dev_ilspy/PlayerDataManager.cs:4572`）→ `BuildHandlers` 建出并登记。
        /// 本机**没有服务器载荷** ⇒ 这里只做「建实例 + 登记内建 handler」那半步，`BuildHandlers` / `Unpack` 不跑
        /// （`LiveOpsLocalBoot.BootLocalLiveOps` 那条 `RuntimeInitializeOnLoadMethod` 已经把 13 份本地活动装进 local handler 了）。</para>
        /// <para>⚠️ **幂等**：重复调用返回同一个实例、日志只打一次（`Awake` 可能被多次触发）。
        /// ⚠️ **失败出声**：建不出来时 `LogError`（⛔ 不静默 —— 静默的后果就是「UI 空白而没人知道为什么」）。</para></summary>
        public static LiveOpsManager EnsureStarted()
        {
            try
            {
                var m = Instance;
                if (m == null)
                {
                    Debug.LogError("[LiveOps] EnsureStarted：`Instance` 建不出来 ⇒ liveops 注册表是空的，"
                                 + "所有 `GetHandler<…>()` 都会返回 null（原版这里是 `SingletonBehaviour<LiveOpsManager>` 的 scene 单例）");
                    return null;
                }
                if (!_bootLogged)
                {
                    _bootLogged = true;
                    Debug.Log("[LiveOps] 启动链：单例已建，注册表 = [" + string.Join(", ", HandlerKeys) + "]"
                            + "（原版这一步由 `PlayerDataManager.InitializeLiveOpsAndConfig` → `Unpack(HandlerData)` → `BuildHandlers` 驱动；"
                            + "本地没有服务器载荷 ⇒ 不跑 `BuildHandlers`/`Unpack`；本地活动的装载见 `LiveOpsLocalBoot`）");
                }
                return m;
            }
            catch (Exception e)
            {
                Debug.LogError("[LiveOps] EnsureStarted 抛异常（注册表可能只填了一半）：" + e);
                return null;
            }
        }

        /// <summary>自检口：把「启动过」的标记与单例一起复位，让 <see cref="EnsureStarted"/> 能被**重新证明**跑过
        /// （⛔ 只给断言用；注册表 `HandlerLookup` 不动 —— 重复登记本来就幂等）。</summary>
        public static void ResetBootForTest()
        {
            _instance = null;
            _bootLogged = false;
        }

        // ============================================================ 🆕 2026-10-28（B91_W7 · 账 406 · BuildHandler 那两跳补口）
        //   ⚠️ 追加在**类尾**：本文件有多条账按 `文件:行号` 钉着（128 / 164 / 210 / 290），往上插一行就会集体漂移。

        /// <summary>**逐事件**特性闸的可注入口（原版 = `FeatureConfig.CheckFeature(config, raw.Data.eventId)`；
        /// 判据 `d:/2/tools/decomp_full/LiveOpsManager._BuildHandler_d__17__MoveNext.c:158-162`
        /// —— `raw.Data` 是 `+0x10`、`eventId` 是 `LiveOpsEventData` 的 `+0x18`，两跳都在 `.c` 里读得到）。
        /// <para>🔶 自己设计（非原版）：原版那份 `FeatureConfig` 来自远端配置；本机没有 ⇒ 默认**放行**，
        /// 需要时由宿主注入一个按 eventId 判定的函数。⛔ 不编一套本地开关表（那会把「没配」伪装成「配了」）。
        /// 调度台 2026-10-28 定。</para></summary>
        public static Func<string, bool> EventFeatureGate;

        /// <summary>`EventFeatureGate` 的空实现：没注入 ⇒ 恒 true（= 原版「这个特性开着」那一档）。</summary>
        static bool EventFeatureEnabled(string eventId)
        {
            if (EventFeatureGate == null) return true;
            return EventFeatureGate(eventId);
        }

        /// <summary>`OrderByDescending(raw =&gt; raw.Data.priority)` 的取值器（`Data` 为 null 的排到最后）。</summary>
        static int Priority(RawEvent r)
        {
            if (r == null || r.Data == null) return int.MinValue;
            return r.Data.priority;
        }

        // ============================================================ 🆕 2026-10-28（B92·N1 · 账 row 22 / 23）
        //   `GetEvent(Type, string)` / `GetEvents(Type, string[])` —— **按 handler 类型**取（原版那两个重载）。
        //   判据（**现读**，不是推定）：
        //     · `cpp2il/out_cpp2il_dev_ilspy/LiveOpsManager.cs` 的 `GetEvent(Type t, string id)`（DisplayClass7_0）：
        //         `var h = GetHandler(t); if (h == null) NRE; return <h.GetBaseEvents(false)>.FirstOrDefault(x => x.GetID() == id);`
        //       —— 那个「source」在 ILSpy 里丢了，但同一文件 `GetEvents(Type,string[])` 里同一个 call site
        //       （`Method not found @1800122E0`）在 `.c` 里有实参：`FUN_1800122e0(1, …, handler, 0)` ⇒ **`ILiveOpsHandler.GetBaseEvents(false)`**。
        //     · `decomp_full/LiveOpsManager__GetEvents.c`（39 行，**真体**）逐句：
        //         `h = GetHandler(t); if (h != null) return Where(h.GetBaseEvents(false), x => ids.Contains(x.GetID()));` 否则 NRE。
        //   🔴 **别把它们和 `GetEvent(string id)` 混为一条**：那个是 `GetAllEvents().FirstOrDefault(...)` 跨 handler
        //      （`decomp_full/LiveOpsManager__GetEvent.c:25-28` 现读，**只收 1 个形参** ⇒ 它对应的是 string 那个重载；
        //       同名的两个重载在 Ghidra 里同名，文件只剩一份，⛔ 别拿它当 Type 重载的判据）。
        //   ⚠️ 差异：原版 `h == null` ⇒ NRE；我们出声 + 返回 null / 空表（同本类 `GetHandler` 的口径）。

        /// <summary>原版 `public static ILiveOps GetEvent(Type t, string id)`（`:1918` 一带，DisplayClass7_0）。
        /// `t` = handler 的**运行时类型**（`HandlerLookup` 的键就是 `h.GetType()`）。</summary>
        public static ILiveOps GetEvent(Type t, string id)
        {
            var h = GetHandler(t);
            if (h == null)
            {
                Debug.LogError("[LiveOps] GetEvent(" + (t == null ? "<null>" : t.Name) + ", " + (id ?? "<null>")
                             + ")：那个类型的 handler 没注册（原版这一跳 NRE）⇒ 返回 null");
                return null;
            }
            var list = h.GetBaseEvents(false);
            if (list == null) return null;
            if (string.IsNullOrEmpty(id)) return null;         // 空 id 不匹配任何事件（原版 `text == id` 同解）
            foreach (var e in list) if (e != null && e.GetID() == id) return e;
            return null;
        }

        /// <summary>原版 `public static IEnumerable&lt;ILiveOps&gt; GetEvents(Type t, string[] ids)`（`:1977` 一带）。
        /// 判据 = `decomp_full/LiveOpsManager__GetEvents.c`（39 行真体）：`Where(x =&gt; ids.Contains(x.GetID()))`。</summary>
        public static IEnumerable<ILiveOps> GetEvents(Type t, string[] ids)
        {
            var outp = new List<ILiveOps>();
            var h = GetHandler(t);
            if (h == null)
            {
                Debug.LogError("[LiveOps] GetEvents(" + (t == null ? "<null>" : t.Name)
                             + ", …)：那个类型的 handler 没注册（原版这一跳 NRE）⇒ 返回空表");
                return outp;
            }
            if (ids == null) return outp;                       // 原版 `Contains(null, …)` 会 NRE；空表同解
            var list = h.GetBaseEvents(false);
            if (list == null) return outp;
            foreach (var e in list)
            {
                if (e == null) continue;
                var id = e.GetID();
                for (int i = 0; i < ids.Length; i++)
                    if (ids[i] == id) { outp.Add(e); break; }
            }
            return outp;
        }

        // ============================================================ 🆕 2026-10-28（B93_E12 · 账 row 410 / 415 / 407）
        //   ⚠️ 追加在**类尾**：本文件有多条账按 `文件:行号` 钉着（128 / 164 / 210 / 290），往上插一行就会集体漂移。
        //   判据（**现读**）：`cpp2il/out_cpp2il_dev_ilspy/LiveOpsManager.cs:1842-1935`（两个重载的 C# 形状）+
        //                     `decomp_full/LiveOpsManager__GetAllEvents.c`（第 2 个重载的机器码）+
        //                     `decomp_full/LiveOpsManager__GetEventsForDisplayingInMenu.c` 与 4 个 `b__19_*` 闭包。

        /// <summary>原版 `public static IEnumerable&lt;ILiveOps&gt; GetAllEvents(EventLocationType location, string parent = null)`
        /// （`LiveOpsManager.cs:1842` / `decomp_full/LiveOpsManager__GetAllEvents.c`）：
        /// <code>
        /// return GetAllEvents().Where(ev =&gt; ev.GetBaseData().location.Match(location, parent));
        /// </code>
        /// 🔴 **判据逐跳**：`.c` 里 `FUN_1800021f0(0xf, …)` = `ILiveOps` 槽 **15** = `GetBaseData()`（`dump.cs:126037`）；
        /// 取到 `data + 0x48`（`location`，`LiveOpsEventData.cs:38` 那个 `EventLocation` 字段），
        /// 然后 `(loc + 0x10) == location` 且 `string.Equals(parent ?? "", loc.parent?.TargetId ?? "")`。
        /// 那两句**就是 `EventLocation.Match` 的体**（`cpp2il/out_cpp2il_dev_ilspy/EventLocation.cs`，我们已在
        /// `Shell/LiveOpsFilter.cs` 复刻为 `EventLocation.Match`）⇒ 这里直接调它，⛔ 别在本地再抄一遍比较式。
        /// <para>⚠️ **数据面的等价差异**（如实记）：原版 `data.location` 是 `EventLocation` 整个对象（含 `parent` 资产引用）；
        /// 我们的 `LiveOpsEventData.location` **只存 `type` 那个 int**（见该字段注释）⇒ `parentId` 从本地目录
        /// `LiveOpsLocalEvent.LocationParentId` 补（判据 = `LiveOpsLocalCatalog` 的 `location.parent.targetId` 那一列）。
        /// 两步都是**同一份 SO 数据的两个列**，合起来与原版那个对象等价。</para></summary>
        public static IEnumerable<ILiveOps> GetAllEvents(EventLocationType location, string parent = null)
        {
            var outp = new List<ILiveOps>();
            foreach (var e in GetAllEvents())
            {
                if (e == null) continue;
                var d = e.GetBaseData();
                if (d == null) continue;                 // 原版这一支 NRE；我们跳过（同一口径见本类 `GetHandler` 的差异说明）
                var local = LiveOpsLocalCatalog.Find(d.eventId);
                var loc = new EventLocation
                {
                    type = (EventLocationType)d.location,                       // 原版 `data.location.type`
                    parentId = local == null ? null : local.LocationParentId,   // 原版 `data.location.parent.TargetId`
                };
                if (!loc.Match(location, parent)) continue;
                outp.Add(e);
            }
            return outp;
        }

        /// <summary>原版 `public static IOrderedEnumerable&lt;LiveopsMainMenuData&gt; GetEventsForDisplayingInMenu()`
        /// （`LiveOpsManager.cs:2075` / `decomp_full/LiveOpsManager__GetEventsForDisplayingInMenu.c`）：
        /// <code>
        /// GetAllEvents()
        ///   .Where(e =&gt; e.ShowInMainMenu)                              // b__19_0 → ILiveOps 槽 25 = get_ShowInMainMenu
        ///   .Where(e =&gt; (int)e.EventState is 1 or 2 or 3)              // b__19_1 → 槽 23 = get_EventState（OnPreview/Active/Finished）
        ///   .Select(e =&gt; e.GetMainMenuData())                          // b__19_2 → 槽 26 = GetMainMenuData
        ///   .OrderByDescending(d =&gt; d.Priority);                       // b__19_3 → 读 `LiveopsMainMenuData + 0x18`
        /// </code>
        /// 🔴 **四个闭包全部反查到槽号**（`dump.cs:126037` 的 `ILiveOps` 槽表 + `b__19_3` 的 `.c` 读 `param_2 + 0x18`
        /// = `LiveopsMainMenuData.Priority`）。`OrderByDescending` 的判据：那个调用点用的辅助函数 `FUN_180c99380`
        /// 与 `BuildHandler` 里 `OrderByDescending(raw =&gt; raw.Data.priority)` 是**同一个函数**（同一个 `FUN_181a60060` 建键选择器），
        /// 且 `GameModes__GetActiveEvent.c` 用同一函数后 `FirstOrDefault()` 取到的是**最高优先**那个 ⇒ 是降序，⛔ 不是升序。
        /// <para>⚠️ 返回类型：原版 `IOrderedEnumerable&lt;LiveopsMainMenuData&gt;`，我们回 `List&lt;LiveopsMainMenuData&gt;`
        /// （本工程这几个文件不用 LINQ；排序用**带序号的稳定排序**等价 `OrderByDescending` 的稳定性 —— 同优先级保持源次序）。</para></summary>
        public static List<LiveopsMainMenuData> GetEventsForDisplayingInMenu()
        {
            var keyed = new List<KeyValuePair<int, LiveopsMainMenuData>>();
            int seq = 0;
            foreach (var e in GetAllEvents())
            {
                if (e == null) continue;
                if (!e.ShowInMainMenu) continue;                       // 槽 25
                int state = (int)e.EventState;                          // 槽 23
                if (!(state >= (int)LiveOpState.OnPreview && state <= (int)LiveOpState.Finished)) continue;
                var data = e.GetMainMenuData();                         // 槽 26
                if (data == null)
                {
                    Debug.LogWarning("[LiveOps] GetEventsForDisplayingInMenu：活动 '" + (e.GetID() ?? "<null>")
                                   + "' 的 GetMainMenuData() 给了 null（原版这一跳会 NRE）⇒ 跳过它");
                    continue;
                }
                keyed.Add(new KeyValuePair<int, LiveopsMainMenuData>(seq++, data));
            }
            // 稳定降序：同 Priority 时按源次序（等价 LINQ `OrderByDescending` 的稳定语义）
            keyed.Sort((a, b) =>
            {
                int c = b.Value.Priority.CompareTo(a.Value.Priority);
                return c != 0 ? c : a.Key.CompareTo(b.Key);
            });
            var outp = new List<LiveopsMainMenuData>(keyed.Count);
            for (int i = 0; i < keyed.Count; i++) outp.Add(keyed[i].Value);
            return outp;
        }

        /// <summary>原版 `LiveOpsUtility.IsTriggeredEvent(ILiveOps)`（`decomp_full/LiveOpsUtility__IsTriggeredEvent.c`，**逐句**）：
        /// <code>
        /// var data = ev.GetBaseData();      // ILiveOps 槽 15
        /// return data.filter.Triggers.Count &gt; 0;    // data + 0x30 = filter，filter + 0x30 = Triggers（`DynamicList`）
        /// </code>
        /// 用途 = `BuildHandler` 的 live/blocked 分派（「这个事件挂了触发器 ⇒ 让它自己决定什么时候激活」）。</summary>
        public static bool IsTriggeredEvent(ILiveOps e)
        {
            if (e == null) return false;
            var d = e.GetBaseData();
            if (d == null || d.filter == null || d.filter.Triggers == null) return false;
            return d.filter.Triggers.Count > 0;
        }
    }
}
