// LiveOpsFilter.cs —— 活动【信号总线 / 触发器 / 分组 / 位置 / 过滤器】那一层
//                    （原版 `Scaffold.Core.Events.{Signal,SignalBus,SignalLedger<T>}` · `EventTrigger` ·
//                     `SignalEventTrigger<T>` · `EventCondition` · `EventGroup` · `EventGroupConfig` ·
//                     `EventGroupAssetProvider` · `EventLocation`(+`EventLocationType`) · `EventFilter` 的行为那半 ·
//                     `ParentEventHandler.BeforeRefresh`)
//
// ============================ 这是什么 / 不是什么 ============================
// 🔴 **本文件与 `Shell/LiveOpsEvent.cs` 是同一个包（P1 · U01）的两半**：
//   · `Shell/LiveOpsEvent.cs` = 数据（`LiveOpsEventFilter` 的字段）+ `LiveOpsEvent`/`ILiveOps` 上新增的那批方法；
//   · **本文件** = 上面那些类型的**行为**（`EventFilter` 的 8 个方法写成 `LiveOpsEventFilter` 的 `partial` 另一半）。
//   ⇒ `LiveOpsEventFilter` 是 `partial`，**⛔ 别在别处再声明一次同名的类**（铁律 6：两处定义同一条规则 = 迟早不一致）。
//
// 判据（第一权威 = Ghidra 真身 `d:/2/tools/decomp_full/`，逐个体全在；骨架 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`）：
//   · `Everguild.LiveOps.EventFilter__{Validate,CheckEventGroups,AddOrRemoveFromGroups,RegisterTriggers,
//     CleanupTriggers,TryActivateEvent,GetExpirationTime,get_HasTriggers,_.ctor}.c`   ← 8 个体全在（本包核心）
//   · `EventGroup__{ctor,GetID,CanAddToGroup,AddEvent,RemoveEvent}.c` · `EventGroupConfig__{ctor,get_ConfigKey,get_Groups,GetAssets}.c`
//     · `EventGroupAssetProvider__{ctor,GetAssets}.c` · `EventLocation__{ctor,Match}.c`
//   · `Everguild.LiveOps.{EventTrigger,EventCondition}__.ctor.c`（只有 ctor ⇒ 两个抽象基类的形状取自 ILSpy 骨架）
//   · `ParentEventHandler__BeforeRefresh.c` + `ParentEventHandler.__c___BeforeRefresh_b__2_0.c`（谓词）
// 字段偏移（`LiveOpsHandler_11方法_第2轮.md` §0·5 那条链一路对上）：`EventFilter` = army +0x10 · expansion +0x14 ·
//   hasDuration +0x18 · eventDuration +0x1C · hasCooldown +0x20 · eventCooldown +0x24 · eventGroups +0x28 ·
//   Triggers +0x30 · Conditions +0x38；`EventGroup` = id +0x18 · MaxActiveEvents +0x20 · maxActiveEventsInMainMenu +0x24 ·
//   eventsInGroup +0x28；`EventLocation` = type +0x10 · parent +0x18。
//
// ⚠️ **四处「判据查不到 / 我们换载体」的如实差异**（⛔ 不是「已经一样」，逐条写死在这里）：
//   ① 🔴 **`SignalEventTrigger<T>` 的四个方法体在 Ghidra 侧 0 产物**（泛型类，`decomp_full` 无 `SignalEventTrigger*`；
//      `decomp_generic_trial/out` 641 个产物里也没有；`out_cpp2il_dev_ilspy` / `out_il2csharp` 两份都只有签名壳
//      —— `ILSpy` 体是 `Method not found @1818A6ED0 / @180DA0A00`，`il2csharp` 体是 `NotImplementedException`）。
//      ⇒ 本文件的 `SignalEventTrigger<T>` **按两条独立证据复原**：(a) ILSpy 里唯一没被吞的那段控制流
//      （`TryTrigger` 先做一次「类偏移 +198」的间接调用 = 抽象 `ValidateSignal`，再 invoke `callback`）；
//      (b) `decomp_full` 里**非泛型**的 `EventFilter.RegisterTriggers` 显示它调的是
//      `trigger.Register(Action)`（vtable +0x178 槽），且**同一个闭包回调**被逐条 trigger 复用
//      （`<>c__DisplayClass15_0` 里 `__this` + `liveOps` 两个字段 + 惰性 `_9__0`）。
//      ⇒ 形状 = `Register(cb)` 记下 `cb` 并把 `TryTrigger` 挂到总线上；`Unregister()` 摘掉。**具体泛型重载**
//      （`Signal.Register<T>(Action<T>)` vs `Register<T>(Action)`）**没读到**，我们按 `Action<T>`（唯一能拿到信号的）。
//      **还差什么**：`SignalEventTrigger<T>` 与 `Scaffold.Core.Events.Signal` 的 `-readOnly` 泛型重导（同 G6 那套办法）。
//   ② `Scaffold.Core.Events.{Signal,SignalBus,SignalLedger<T>}` 的**方法体也全是壳**（`d:/2/Warpforge_code/Scripts/
//      Scaffold.Core.Events/` 是**签名桩**，体是空的）。⇒ 本文件的 `LiveOpsSignal{,_Bus,_Ledger}` **按签名桩的
//      字段表 + 方法表复原**：`SignalBus` 有 `Dictionary<Type, ISignalLedger> _ledgers`；`SignalLedger<T>` 有
//      `_ledger` / `_typedListeners` / `_untypedListeners` / `_wrappedListeners` 四张表 + `GetListeners()` + `UpdateLedger()`。
//      **合并语义**（typed ∪ 包装过的 untyped）是**推的**，没读到体 ⇒ 标 🔶。
//   ③ `EventGroup` 原版是 **`ScriptableObject`（`IdentifiableSO`）**，`EventGroupConfig` 走 **`ConfigManager.GetConfig<T>()`**
//      （远端 TitleData 的 `"EventGroups"` 键）。我们**没有 ConfigManager、本地也没有 `EventGroups` 配置**
//      （`判据/LiveOps_C1W2_C1W3_第3轮.md` §五·4 记的就是这条）⇒ `EventGroup` 做成 `[Serializable]` **POCO**
//      （不是 SO），配置源换成 `EventGroupConfig.Current`（一个可注入的静态口）。⛔ **没有编组名**（铁律 2）。
//   ④ `EventLocation.parent` 原版是 `LoadableReference<ParentEventData>`（资产引用）；我们无资产 ⇒ 只留 `parentId`（= `TargetId` 那半）。
//
// 🔴 **本文件的一处「原版会 NRE，我们出声降级」**（与本工程 `LiveOpsEvent.cs` / `LiveOpsHandler.cs` 同一条既有口径）：
//   原版对 `data == null` / `filter == null` / `save == null` / `EventGroup.eventsInGroup == null` 一律**隐式 NRE**；
//   我们逐处 `Debug.LogError` 后返回一个安全值（不是静默）。**这是有意差异**，写进账的差异列。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ §1 信号总线（原版 `Scaffold.Core.Events.Signal` 一族）
    /// <summary>原版 `Scaffold.Core.Events.ISignal`（**空标记接口**，`ISignal.cs` 全文就一行 `interface ISignal { }`）。
    /// <para>⚠️ 改名（`ISignal` → `ILiveOpsSignal`）是**为了不撞库里其它同名类型**（`Cinemachine.ISignalSource6D` 一族）。
    /// 语义一字不差：**只做类型标签，没有任何成员**。</para></summary>
    public interface ILiveOpsSignal { }

    /// <summary>原版 `EventExpiredSignal`：活动过期/被停用时发出去的信号，成员 = **事件 id**。
    /// <para>判据 = `判据/LiveOps_C1W2_C1W3_第3轮.md` §一·4（`ExpireEvent` 的 `HandleOnComplete` 里
    /// `Signal.Raise(new EventExpiredSignal(id))`；cpp2il 现读 `new EventExpiredSignal(eventID)`）。</para>
    /// <para>🔴 **它是【唯一】我们从原版点名到的信号类型** —— 其余 6 个具体 `SignalEventTrigger&lt;T&gt;` 子类
    /// （`AddTagTrigger` / `WildcardTrigger` / `CampaignNodeTrigger` / `CampaignPointsTrigger` /
    /// `ItemReceivedSignal` / `ItemSpentSignal`）的信号类型属别的包（P11/P13），本包**不替它们建**。</para></summary>
    public sealed class EventExpiredSignal : ILiveOpsSignal
    {
        /// <summary>事件 id（= `ILiveOps.GetID()`）。</summary>
        public string EventId;

        public EventExpiredSignal() { }
        public EventExpiredSignal(string eventId) { EventId = eventId; }
        public override string ToString() { return "EventExpiredSignal(" + (EventId ?? "<null>") + ")"; }
    }

    /// <summary>原版 `Scaffold.Core.Events.Signals.ISignalLedger`（**非泛型**那一半：只按无参 `Action` 注册/退订）。
    /// 判据 = 签名桩 `Scaffold.Core.Events/Signals/ISignalLedger.cs`（`ClearLedger` / `Register(Action)` / `Unregister(Action)`）。</summary>
    public interface ILiveOpsSignalLedger
    {
        void ClearLedger();
        void Register(Action callback);
        void Unregister(Action callback);
        /// <summary>🔶 自己设计（非原版）：**只读**的 `ListenerCount` 口 —— 原版 `ISignalLedger` 只有 `ClearLedger/Register/Unregister`，判据里没有「数条数」的读口，而「漏摘」是本包已知风险（文件头）；只给自检/诊断，`Raise` 不读它。
        /// 调度台 W8P1 2026-10-26 裁定：加这一个读口，换取「漏摘」可断言。</summary>
        int ListenerCount { get; }
    }

    /// <summary>原版 `Scaffold.Core.Events.Signals.SignalLedger&lt;T&gt;`（一种信号类型一本台账）。
    /// <para>🔴 字段表**逐个照抄签名桩**（`SignalLedger.cs`）：`List&lt;Action&lt;T&gt;&gt; _ledger` ·
    /// `_typedListeners` · `List&lt;Action&gt; _untypedListeners` · `_wrappedListeners`。
    /// `_ledger` 是**派生的合并表**（`UpdateLedger()` 重建），`Raise` 读它。</para>
    /// <para>🔶 **自己设计（非原版）**：`UpdateLedger()` 的合并语义 —— 签名桩只给了四个字段名，**没给体**（Ghidra 侧
    /// `Scaffold.Core.Events` 全族 0 产物）⇒ 「`_ledger` = `_typedListeners` ∪ `_untypedListeners`（后者包装成 `Action&lt;T&gt;`）」
    /// 是按字段名与 `GetListeners()` 的返回类型推的。`_wrappedListeners` 只做诊断用（与 `_untypedListeners` 同序）。
    /// **调度台 W8P1 2026-10-26 裁定：按此形状落，判据缺口见文件头 ①②。**</para></summary>
    public sealed class LiveOpsSignalLedger<T> : ILiveOpsSignalLedger
        where T : ILiveOpsSignal
    {
        readonly List<Action<T>> _ledger = new List<Action<T>>();
        readonly List<Action<T>> _typedListeners = new List<Action<T>>();
        readonly List<Action> _untypedListeners = new List<Action>();
        readonly List<Action<T>> _wrappedListeners = new List<Action<T>>();

        /// <summary>原版 `Register(Action&lt;T&gt; callback)`（带信号的监听器）。重复注册**允许**（原版 `List.Add` 没有去重）。</summary>
        public void Register(Action<T> callback)
        {
            if (callback == null) { Debug.LogError("[LiveOps] Signal<" + typeof(T).Name + ">.Register(null) ⇒ 忽略（原版会加进表里、然后 Raise 时 NRE）"); return; }
            _typedListeners.Add(callback);
            UpdateLedger();
        }

        /// <summary>原版 `Register(Action callback)`（不带信号；`Raise` 时照样被调）。</summary>
        public void Register(Action callback)
        {
            if (callback == null) { Debug.LogError("[LiveOps] Signal<" + typeof(T).Name + ">.Register(null) ⇒ 忽略（同上）"); return; }
            _untypedListeners.Add(callback);
            UpdateLedger();
        }

        /// <summary>原版 `Unregister(Action&lt;T&gt; callback)`：`List.Remove`（**只摘第一条**，与原版 `Remove` 同义）。</summary>
        public void Unregister(Action<T> callback)
        {
            if (callback == null) return;
            if (!_typedListeners.Remove(callback))
            {
                // 原版 `List.Remove` 返回 false 时是**静默**的，而且「同一个 trigger 摘两次」在我们这里是**正常路径**
                // （`CleanupTriggers` 会被 `ClearEvents` 反复调到）⇒ 只按 Verbose 打过程日志，⛔ 不刷 Warning。
                if (LiveOpsSignals.Verbose)
                    Debug.Log("[LiveOps] Signal<" + typeof(T).Name + ">.Unregister：这条带信号的监听器不在台账里 ⇒ 没摘到"
                            + "（重复 Unregister / 从未 Register；原版 `List.Remove` 同样返回 false 而不出声）");
                return;
            }
            UpdateLedger();
        }

        /// <summary>原版 `Unregister(Action callback)`（不带信号那条）。</summary>
        public void Unregister(Action callback)
        {
            if (callback == null) return;
            if (!_untypedListeners.Remove(callback))
            {
                if (LiveOpsSignals.Verbose)
                    Debug.Log("[LiveOps] Signal<" + typeof(T).Name + ">.Unregister：这条无参监听器不在台账里 ⇒ 没摘到（同上）");
                return;
            }
            UpdateLedger();
        }

        /// <summary>原版 `ClearLedger()`：三张表全清（`_ledger` 是派生的，一起清）。</summary>
        public void ClearLedger()
        {
            _ledger.Clear();
            _typedListeners.Clear();
            _untypedListeners.Clear();
            _wrappedListeners.Clear();
        }

        /// <summary>原版 `GetListeners()`：返回**当前生效**的监听器表。
        /// <para>🔶 **自己设计（非原版）**：返回**副本**（原版大概率直接返回 `_ledger`）—— 理由：`Raise` 期间监听器可能
        /// 退订/新订，直接返回会被就地改（原版那样 `foreach` 会抛 `InvalidOperationException`）。副本 = 派发期间表冻结。
        /// 调度台 W8P1 2026-10-26 裁定。</para></summary>
        public List<Action<T>> GetListeners()
        {
            UpdateLedger();
            return new List<Action<T>>(_ledger);
        }

        /// <summary>自检口：生效监听器条数。</summary>
        public int ListenerCount { get { UpdateLedger(); return _ledger.Count; } }
        /// <summary>自检口：带信号的监听器条数（`_typedListeners.Count`，原版同名字段）。</summary>
        public int TypedCount { get { return _typedListeners.Count; } }
        /// <summary>自检口：不带信号的监听器条数（`_untypedListeners.Count`）。</summary>
        public int UntypedCount { get { return _untypedListeners.Count; } }

        /// <summary>重建派生表 `_ledger`（原版 `private void UpdateLedger()`）。</summary>
        void UpdateLedger()
        {
            _ledger.Clear();
            for (int i = 0; i < _typedListeners.Count; i++) _ledger.Add(_typedListeners[i]);
            _wrappedListeners.Clear();
            for (int i = 0; i < _untypedListeners.Count; i++)
            {
                Action cb = _untypedListeners[i];
                if (cb == null) continue;
                Action<T> wrapped = delegate (T _) { cb(); };
                _wrappedListeners.Add(wrapped);
                _ledger.Add(wrapped);
            }
        }
    }

    /// <summary>原版 `Scaffold.Core.Events.Signals.ISignalBroker`（签名桩逐条：`Register`/`Unregister` 各两个重载 +
    /// `UnregisterAll(Type)` + `Raise&lt;T&gt;(T)`）。</summary>
    public interface ILiveOpsSignalBroker
    {
        void Register<T>(Type type, Action<T> callback) where T : ILiveOpsSignal;
        void Register(Type type, Action callback);
        void Unregister<T>(Type type, Action<T> callback) where T : ILiveOpsSignal;
        void Unregister(Type type, Action callback);
        void UnregisterAll(Type type);
        void Raise<T>(T signal) where T : ILiveOpsSignal;
    }

    /// <summary>原版 `Scaffold.Core.Events.Signals.SignalBus : ISignalBroker`
    /// （字段 = `Dictionary&lt;Type, ISignalLedger&gt; _ledgers`；私有 `FetchLedger&lt;T&gt;()` / `FetchLedger(Type)` /
    /// `CreateLedger&lt;T&gt;()` / `CreateLedger(Type)` 逐个对位）。
    /// <para>🔴 **台账按 `Type` 索引**：`Raise&lt;T&gt;(signal)` 取 `typeof(T)` 那本；`Register(Type, …)` 用传进来的 `type` 当键
    /// （原版就是两套重载：带信号那套按泛型参数，不带信号那套按 `Type`）。</para></summary>
    public sealed class LiveOpsSignalBus : ILiveOpsSignalBroker
    {
        readonly Dictionary<Type, ILiveOpsSignalLedger> _ledgers = new Dictionary<Type, ILiveOpsSignalLedger>();

        /// <summary>原版 `Raise&lt;T&gt;(T signal)`：取本类型台账 → 逐个调。**没有监听器 ⇒ 什么都不做**（不是失败）。</summary>
        public void Raise<T>(T signal) where T : ILiveOpsSignal
        {
            var ledger = FetchLedger(typeof(T));
            if (ledger == null) return;
            var typed = ledger as LiveOpsSignalLedger<T>;
            if (typed != null)
            {
                var listeners = typed.GetListeners();          // 副本（见 LiveOpsSignalLedger.GetListeners 的 🔶）
                for (int i = 0; i < listeners.Count; i++)
                {
                    var cb = listeners[i];
                    if (cb == null) continue;
                    try { cb(signal); }
                    catch (Exception e)
                    {
                        // 🔶 自己设计（非原版）：原版大概率不 try/catch（一条监听器抛异常 = 后面全不跑）。
                        // 我们出声后继续 —— 理由：一个坏订阅方不该让「活动排期」整条链停摆（本工程「不许静默失败」的同一口径）。
                        Debug.LogError("[LiveOps] Signal<" + typeof(T).Name + "> 的监听器抛异常（已继续派发其余监听器）：" + e);
                    }
                }
                return;
            }
            var untyped = ledger as LiveOpsUntypedSignalLedger;
            if (untyped != null)
            {
                untyped.DispatchAll();                          // 降级台账：只有无参监听器
                return;
            }
            Debug.LogError("[LiveOps] Signal<" + typeof(T).Name + ">.Raise：台账存在但类型不认识 ⇒ 本次信号**没有派发**");
        }

        public void Register<T>(Type type, Action<T> callback) where T : ILiveOpsSignal
        {
            var t = type == null ? typeof(T) : type;
            var ledger = FetchOrCreateLedger(t);
            var typed = ledger as LiveOpsSignalLedger<T>;
            if (typed == null)
            {
                // 原版这里走 `CreateLedger(Type)`（**反射**造一个 `SignalLedger<T>`）。我们没有等价反射口 ⇒ 出声，⛔ 不静默丢订阅。
                Debug.LogError("[LiveOps] Signal.Register<" + typeof(T).Name + ">(type = " + t.Name + ")：台账已存在但类型不是 "
                             + "LiveOpsSignalLedger<" + typeof(T).Name + "> ⇒ **本次订阅被丢弃**"
                             + "（原版用 `SignalBus.CreateLedger(Type)` 反射建台账；我们没有那一跳 —— 见文件头 ②）");
                return;
            }
            typed.Register(callback);
        }

        public void Register(Type type, Action callback)
        {
            if (type == null) { Debug.LogError("[LiveOps] Signal.Register(null Type, …) ⇒ 忽略（原版会 NRE）"); return; }
            FetchOrCreateLedger(type).Register(callback);
        }

        public void Unregister<T>(Type type, Action<T> callback) where T : ILiveOpsSignal
        {
            var t = type == null ? typeof(T) : type;
            var ledger = FetchLedger(t);
            if (ledger == null)
            {
                Debug.LogWarning("[LiveOps] Signal.Unregister<" + typeof(T).Name + ">：台账不存在 ⇒ 没摘到（原版同样静默）");
                return;
            }
            var typed = ledger as LiveOpsSignalLedger<T>;
            if (typed == null)
            {
                Debug.LogError("[LiveOps] Signal.Unregister<" + typeof(T).Name + ">(type = " + t.Name + ")：台账类型不匹配 ⇒ 没摘到");
                return;
            }
            typed.Unregister(callback);
        }

        public void Unregister(Type type, Action callback)
        {
            if (type == null) return;
            var ledger = FetchLedger(type);
            if (ledger == null)
            {
                Debug.LogWarning("[LiveOps] Signal.Unregister(" + type.Name + ")：台账不存在 ⇒ 没摘到（原版同样静默）");
                return;
            }
            ledger.Unregister(callback);
        }

        /// <summary>原版 `UnregisterAll(Type type)`：整套台账清空（**不删台账本身**）。</summary>
        public void UnregisterAll(Type type)
        {
            if (type == null) return;
            var ledger = FetchLedger(type);
            if (ledger != null) ledger.ClearLedger();
        }

        /// <summary>原版 `private ISignalLedger FetchLedger(Type type)`：没有就**返回 null**（不建）。</summary>
        ILiveOpsSignalLedger FetchLedger(Type type)
        {
            ILiveOpsSignalLedger ledger;
            return _ledgers.TryGetValue(type, out ledger) ? ledger : null;
        }

        /// <summary>原版 `private ISignalLedger CreateLedger(Type type)`：**建**一本 `SignalLedger&lt;type&gt;`（原版用反射）。
        /// ⚠️ 我们**先试反射**（`MakeGenericType`，编辑器/单机 .NET 上可靠；IL2CPP 下若该实例化没被 AOT 编到会抛），
        /// 抛了就降级成<see cref="LiveOpsUntypedSignalLedger"/>（只收无参 `Action`，`Raise` 时照样被调）并**出声**。
        /// ⇒ 带信号的订阅请一律走 `Register&lt;T&gt;` 那条（它知道 `T`，不需要反射）。</summary>
        ILiveOpsSignalLedger CreateLedger(Type type)
        {
            ILiveOpsSignalLedger ledger = null;
            try
            {
                var open = typeof(LiveOpsSignalLedger<>);
                ledger = (ILiveOpsSignalLedger)Activator.CreateInstance(open.MakeGenericType(type));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LiveOps] SignalBus.CreateLedger(" + (type == null ? "?" : type.Name) + ")：反射建泛型台账失败 ⇒ "
                               + "降级成非泛型台账（只能收无参监听器；带信号订阅请走 `Register<T>`）。原因：" + e.Message);
            }
            if (ledger == null) ledger = new LiveOpsUntypedSignalLedger(type);
            _ledgers[type] = ledger;
            return ledger;
        }

        ILiveOpsSignalLedger FetchOrCreateLedger(Type type)
        {
            var l = FetchLedger(type);
            if (l != null) return l;
            return CreateLedger(type);
        }

        /// <summary>按 `T` 取/建**泛型**台账（`Signal.Register&lt;T&gt;(Action&lt;T&gt;)` 那条走这里）。</summary>
        internal LiveOpsSignalLedger<T> FetchOrCreateTypedLedger<T>() where T : ILiveOpsSignal
        {
            var existing = FetchLedger(typeof(T));
            var typed = existing as LiveOpsSignalLedger<T>;
            if (typed != null) return typed;
            if (existing != null)
            {
                Debug.LogError("[LiveOps] Signal<" + typeof(T).Name + ">：该类型已有一本**非泛型**台账（先被 `Register(Type, …)` 建过）"
                             + " ⇒ 带信号的订阅无家可归（原版靠反射统一成 `SignalLedger<T>`；见文件头 ②）");
                return null;
            }
            typed = new LiveOpsSignalLedger<T>();
            _ledgers[typeof(T)] = typed;
            return typed;
        }

        // ── 自检口 ─────────────────────────────────────────────────────────
        /// <summary>自检口：某个信号类型上**生效的**监听器条数（台账不存在 ⇒ 0）。</summary>
        public int ListenerCount<T>() where T : ILiveOpsSignal { return ListenerCount(typeof(T)); }
        /// <summary>自检口：按 `Type` 数监听器。</summary>
        public int ListenerCount(Type type)
        {
            if (type == null) return 0;
            var l = FetchLedger(type);
            return l == null ? 0 : l.ListenerCount;
        }
        /// <summary>自检口：台账本数（`_ledgers.Count`）。</summary>
        public int LedgerCount { get { return _ledgers.Count; } }
        /// <summary>自检口：清空所有台账（**只给断言用**）。</summary>
        public void ClearAll() { _ledgers.Clear(); }
    }

    /// <summary>🔶 自己设计（非原版）：只在 `CreateLedger(Type)` 反射失败时出现的**降级非泛型台账**（原版靠反射统一成 `SignalLedger&lt;T&gt;`，我们没有等价反射保证 · 调度台 W8P1 2026-10-26 裁定）：
    /// （原版不会出现：它靠反射把两种重载合并成同一个 `SignalLedger&lt;T&gt;`）。见文件头 ②。</summary>
    internal sealed class LiveOpsUntypedSignalLedger : ILiveOpsSignalLedger
    {
        readonly Type _type;
        readonly List<Action> _listeners = new List<Action>();
        public LiveOpsUntypedSignalLedger(Type type) { _type = type; }
        public int ListenerCount { get { return _listeners.Count; } }
        public void ClearLedger() { _listeners.Clear(); }
        public void Register(Action callback)
        {
            if (callback == null) { Debug.LogError("[LiveOps] Signal(" + (_type == null ? "?" : _type.Name) + ").Register(null) ⇒ 忽略"); return; }
            _listeners.Add(callback);
        }
        public void Unregister(Action callback) { if (callback != null) _listeners.Remove(callback); }
        /// <summary>派发一次（只有无参监听器 —— 本台账拿不到具体信号对象）。</summary>
        public void DispatchAll()
        {
            var snapshot = _listeners.ToArray();       // 副本：派发期间允许退订
            for (int i = 0; i < snapshot.Length; i++)
            {
                var cb = snapshot[i];
                if (cb == null) continue;
                try { cb(); }
                catch (Exception e) { Debug.LogError("[LiveOps] Signal(" + (_type == null ? "?" : _type.Name) + ") 的监听器抛异常（已继续）：" + e); }
            }
        }
    }

    /// <summary>原版 `Scaffold.Core.Events.Signal`（**静态门面**，10 个方法逐个对位签名桩 `Signal.cs`）：
    /// `Register&lt;T&gt;` ×2 · `Register&lt;T&gt;(Type, Action&lt;T&gt;)` · `Register(Type, Action)` ·
    /// `Unregister` 同四档 · `Raise&lt;T&gt;(T)` · `Raise&lt;T&gt;()`（`new()`）。
    /// <para>🔶 自己设计（非原版）：**旧事件桥**（两套信号口之间单向接线）—— 原版只有这一条总线，而我们的 `Shell/LiveOpsHandler.cs:1390` 的
    /// <see cref="LiveOpsSignals.EventExpired"/> 是**上一批**为 `Signal.Raise(new EventExpiredSignal(id))` 建的等价物
    /// （那一批还没有总线）。现在总线有了 ⇒ 把旧事件**单向喂进**总线（`LiveOpsSignals.EventExpired → Raise`），
    /// ⛔ 不反过来 —— 否则一次过期会被数两遍。
    /// ⚠️ `LiveOpsSignals.ResetForTest()` 会把桥摘掉（它 `EventExpired = null`）⇒ 自检里若两者一起用，
    /// 先 `ResetForTest()` 再 `LiveOpsSignal.ReinstallLegacyBridge()`（本文件 §7 的自检已经这么做了）。</para></summary>
    public static class LiveOpsSignal
    {
        static ILiveOpsSignalBroker _broker;
        static bool _bridgeInstalled;

        /// <summary>原版 `private static ISignalBroker broker`（惰性取；原版大概由 `SignalInitializer` 注入，我们直接 new）。</summary>
        public static ILiveOpsSignalBroker Broker
        {
            get
            {
                if (_broker == null)
                {
                    _broker = new LiveOpsSignalBus();
                    InstallLegacyBridge();
                }
                return _broker;
            }
        }

        /// <summary>强类型总线的便捷口（自检数监听器用；⛔ 生产别拿它绕开 `Register/Raise`）。</summary>
        public static LiveOpsSignalBus Bus { get { return Broker as LiveOpsSignalBus; } }

        public static void Register<T>(Action<T> callback) where T : ILiveOpsSignal
        {
            var bus = Bus;
            if (bus == null) { Debug.LogError("[LiveOps] LiveOpsSignal.Register：总线不是 LiveOpsSignalBus ⇒ 订阅丢失"); return; }
            var ledger = bus.FetchOrCreateTypedLedger<T>();
            if (ledger != null) ledger.Register(callback);
        }

        /// <summary>原版 `Register&lt;T&gt;(Action callback)`：不带信号的订阅（`Raise` 时照样被调）。</summary>
        public static void Register<T>(Action callback) where T : ILiveOpsSignal { Broker.Register(typeof(T), callback); }

        /// <summary>原版 `Register&lt;T&gt;(Type type, Action&lt;T&gt; callback)`：
        /// ⚠️ **只支持 `type == null || type == typeof(T)`**（其余会出声丢弃）—— 原版靠反射建台账，我们没有那一跳。</summary>
        public static void Register<T>(Type type, Action<T> callback) where T : ILiveOpsSignal
        {
            if (type == null || type == typeof(T)) { Register<T>(callback); return; }
            Debug.LogError("[LiveOps] LiveOpsSignal.Register<" + typeof(T).Name + ">(type = " + type.Name + ")："
                         + "跨类型订阅需要原版 `SignalBus.CreateLedger(Type)` 的反射（我们没有）⇒ 本次订阅被丢弃（见文件头 ②）");
        }

        public static void Register(Type type, Action callback) { Broker.Register(type, callback); }

        public static void Unregister<T>(Action<T> callback) where T : ILiveOpsSignal
        {
            var bus = Bus;
            if (bus == null) return;
            var ledger = bus.FetchOrCreateTypedLedger<T>();
            if (ledger != null) ledger.Unregister(callback);
        }

        public static void Unregister<T>(Action callback) where T : ILiveOpsSignal { Broker.Unregister(typeof(T), callback); }

        public static void Unregister<T>(Type type, Action<T> callback) where T : ILiveOpsSignal
        {
            if (type == null || type == typeof(T)) { Unregister<T>(callback); return; }
            Debug.LogError("[LiveOps] LiveOpsSignal.Unregister<" + typeof(T).Name + ">(type = " + type.Name + ")：跨类型退订同样没有反射口 ⇒ 没摘到");
        }

        public static void Unregister(Type type, Action callback) { Broker.Unregister(type, callback); }

        /// <summary>原版 `Raise&lt;T&gt;(T signal)`。</summary>
        public static void Raise<T>(T signal) where T : ILiveOpsSignal { Broker.Raise(signal); }

        /// <summary>原版 `Raise&lt;T&gt;() where T : ISignal, new()`。</summary>
        public static void Raise<T>() where T : ILiveOpsSignal, new() { Raise(new T()); }

        // ── 旧事件桥（我们加的，见类注释）──────────────────────────────────
        static void InstallLegacyBridge()
        {
            if (_bridgeInstalled) return;
            LiveOpsSignals.EventExpired += OnLegacyEventExpired;
            _bridgeInstalled = true;
        }

        static void OnLegacyEventExpired(string eventId)
        {
            // 只在**有订阅方**时才走总线（否则每次过期都白建一本台账 ⇒ `ListenerCount` 类断言会看见噪声）。
            var bus = Bus;
            if (bus == null || bus.ListenerCount<EventExpiredSignal>() == 0) return;
            bus.Raise(new EventExpiredSignal(eventId));
        }

        /// <summary>`LiveOpsSignals.ResetForTest()` 之后把桥接回来（见类注释的 ⚠️）。幂等。</summary>
        public static void ReinstallLegacyBridge()
        {
            LiveOpsSignals.EventExpired -= OnLegacyEventExpired;
            _bridgeInstalled = false;
            if (_broker != null) InstallLegacyBridge();
        }

        /// <summary>自检收尾用：丢总线 + 摘桥。</summary>
        public static void ResetForTest()
        {
            LiveOpsSignals.EventExpired -= OnLegacyEventExpired;
            _bridgeInstalled = false;
            _broker = null;
        }
    }

    // ============================================================ §2 触发器 / 条件（原版 `EventTrigger` / `SignalEventTrigger<T>` / `EventCondition`）
    /// <summary>原版 `[Serializable] public abstract class EventTrigger`（`EventTrigger.cs` 全文）：
    /// `public abstract void Register(Action callback);` + `public abstract void Unregister();` —— **就这两个成员**。
    /// <para>⚠️ **改名**（`EventTrigger` → `LiveOpsEventTrigger`，语义一字不差）：本工程所有类型都在**一个扁平命名空间**
    /// `CardPresentation` 里，而 5 个文件（`Battle/SkillPanel.cs` · `Battle/BattleLogPanel.cs` · `Battle/ChatPopupPanel.cs` ·
    /// `Battle/AttackSelector.cs` · `Battle/WfSlider.cs`）**不带限定地**用了 `UnityEngine.EventSystems.EventTrigger`
    /// （`AddComponent&lt;EventTrigger&gt;()` / `EventTrigger.Entry`）⇒ 原名一进来就把它们全打红（L0 实测 **24 条错**）。
    /// ⛔ 别为了「名字和原版一样」再撞回去。</para></summary>
    [Serializable]
    public abstract class LiveOpsEventTrigger
    {
        public abstract void Register(Action callback);
        public abstract void Unregister();
    }

    /// <summary>原版 `[Serializable] public abstract class SignalEventTrigger&lt;T&gt; : EventTrigger where T : ISignal`（我们叫 `LiveOpsEventTrigger`）
    /// （`SignalEventTrigger.cs`）。
    /// <para>成员逐个对位（签名桩 = `out_il2csharp/.../SignalEventTrigger.cs`）：
    /// `public T EventSignal;` · `private Action callback;` · `sealed override Register(Action)` ·
    /// `private void TryTrigger(T signal)` · `protected abstract bool ValidateSignal(T)` · `sealed override Unregister()`。</para>
    /// <para>✅ **方法体已在【第五源】找到并逐句核过**（2026-10-29 `B94_B4`）：泛型共享体
    /// `decomp_generic_trial/out_b94/SignalEventTrigger_object_.{Register__12549a0,Unregister__1254b90,TryTrigger__1254a40}.c`
    /// （旧记「四源全无」= 名单/产物口径问题，不是没有产物）⇒ 逐句结果见 <see cref="SignalEventTrigger{T}.Register"/> 的 doc。核心形状：
    /// <code>
    /// Register(cb) { callback = cb; Signal.Register&lt;T&gt;(TryTrigger); }
    /// TryTrigger(signal) { if (ValidateSignal(signal)) callback?.Invoke(); }
    /// Unregister() { Signal.Unregister&lt;T&gt;(TryTrigger); }
    /// </code>
    /// 三条独立证据：① ILSpy 里 `TryTrigger` **剩下的唯一**控制流 = 一次「类偏移 +198」的间接调用（抽象 `ValidateSignal`）后紧跟 `callback` 的 `invoke_impl`（⇒ 次序 = 先校验后回调，这条**有据**）；② `EventFilter.RegisterTriggers` 的体把**同一个闭包回调**逐条交给 `trigger.Register(Action)`（vtable 槽），且 `Unregister` 与它成对（`CleanupTriggers`）；③ 目标 API 形状对得上：本工程的 `LiveOpsSignal`（= 原版 `Scaffold.Core.Events.Signal`）**确实有** `Register<T>(Action<T>)` / `Unregister<T>(Action<T>)` 这一对（`LiveOpsFilter.cs:437` / `:459`），而 `TryTrigger` 的签名正好是 `Action<T>`。
    /// ⇒ 「信号 → 校验 → 回调」有据；**「挂在哪条泛型重载上」也已坐实**（2026-10-29 `B94_B4` 读泛型共享体：`Scaffold_Core_Events_Signal__Register<object>` / `__Unregister<object>` 各一次，委托是 `Action<object>(this.TryTrigger)` ⇒ 就是 `Signal<T>.Register/Unregister`，`<object>` 只是「引用类型共享同一份泛型代码」的印法）。</para>
    /// <para>⚠️ `EventSignal` 字段（原版 `public T EventSignal`）在**我们复原的这条链里不参与**判断 —— 原版它大概是给
    /// Inspector 里填「要监听哪个信号实例」的（`T : ISignal` 是空接口 ⇒ 没有可比的成员）。**⛔ 别拿它做过滤**；
    /// 过滤一律走 `ValidateSignal`。</para></summary>
    [Serializable]
    public abstract class SignalEventTrigger<T> : LiveOpsEventTrigger
        where T : ILiveOpsSignal
    {
        /// <summary>原版 `public T EventSignal;`（Inspector 里配的信号实例）。见类注释 ⚠️。</summary>
        public T EventSignal;
        /// <summary>原版 `private Action callback`。</summary>
        Action callback;

        protected SignalEventTrigger() { }

        /// <summary>原版 `sealed override Register(Action callback)` —— 逐句对齐泛型共享体（`out_b94/…Register__12549a0.c`）：① `*(this+0x18) = callback`（= 本行那句 `this.callback = callback`）② 取 `Signal<T>` 实例并 `Signal<T>.Register(new Action<T>(this.TryTrigger))` ⇒ 与本实现**逐句一致**。</summary>
        public sealed override void Register(Action callback)
        {
            this.callback = callback;
            LiveOpsSignal.Register<T>(TryTrigger);
        }

        /// <summary>原版 `sealed override Unregister()`：把 `TryTrigger` 从总线上摘掉 —— 逐句对齐泛型共享体（`out_b94/…Unregister__1254b90.c`：不碰 `callback`，只 `Signal<T>.Unregister(new Action<T>(this.TryTrigger))`）。
        /// ⚠️ 委托相等性靠 `Delegate.Equals`（同一 `this` + 同一方法）⇒ 两次方法组转换出来的委托相等，`Remove` 能摘到。</summary>
        public sealed override void Unregister()
        {
            LiveOpsSignal.Unregister<T>(TryTrigger);
        }

        /// <summary>原版 `private void TryTrigger(T signal)`：**先校验、再回调**（次序 = 文件头 ① 的证据）。</summary>
        void TryTrigger(T signal)
        {
            if (!ValidateSignal(signal)) return;
            if (callback != null) callback();
        }

        /// <summary>原版 `protected abstract bool ValidateSignal(T signal)`：这条信号算不算数。
        /// 例（`decomp_full` 侧读得到的具体子类）：`AddTagTrigger` = `signal.Tag == this.tag`；
        /// `WildcardTrigger` = `return true`。</summary>
        protected abstract bool ValidateSignal(T signal);
    }

    /// <summary>原版 `[Serializable] public abstract class EventCondition`（`EventCondition.cs` 全文）：
    /// `public abstract bool Validate(ILiveOps liveOps);` —— 一个成员。</summary>
    [Serializable]
    public abstract class EventCondition
    {
        public abstract bool Validate(ILiveOps liveOps);
    }

    // ============================================================ §3 活动分组（原版 `EventGroup` / `EventGroupConfig` / `EventGroupAssetProvider`）
    /// <summary>原版 `EventGroup : IdentifiableSO`（**ScriptableObject**，`EventGroup.cs`）。
    /// <para>字段逐个照抄（偏移 = 文件头那张表）：
    /// `private string id`(+0x18) · `private int MaxActiveEvents`(+0x20，**没有初始化器 ⇒ 默认 0**) ·
    /// `private int maxActiveEventsInMainMenu = -1`(+0x24) · `private List&lt;string&gt; eventsInGroup`(+0x28，ctor 里 new)。</para>
    /// <para>🔴 **`eventsInGroup` 装的是【事件 id 字符串】**，不是 `ILiveOps` —— 判据：`CanAddToGroup` 里
    /// `list.Contains(liveOps.GetID())`、`AddEvent` 里 `list.Add(eventToAdd.GetID())`（Ghidra 体逐条可读）。</para>
    /// <para>🔴 **`MaxActiveEvents` 默认 0 是个坑（照抄）**：`CanAddToGroup` 是 `if (MaxActiveEvents &gt;= 0) { if (count &lt; MaxActiveEvents) return true;
    /// return Contains(id); } return true;` ⇒ 默认（0）的组**永远装不进第一个事件**（`0 &lt; 0` 假 ⇒ 走 `Contains`)。
    /// 原版每个组都在 SO 里显式配了容量，所以看不出来。⛔ 别「顺手改成 -1」。</para>
    /// <para>⚠️ 我们做成 `[Serializable]` POCO（不是 ScriptableObject）：本地没有 `EventGroup` 资产、也没有 ConfigManager（文件头 ③）。</para></summary>
    [Serializable]
    public class EventGroup
    {
        /// <summary>原版 `private string id`（+0x18）。</summary>
        public string id;
        /// <summary>原版 `private int MaxActiveEvents`（+0x20）。**没有初始化器 ⇒ 默认 0**（见类注释的坑）。</summary>
        public int MaxActiveEvents;
        /// <summary>原版 `private int maxActiveEventsInMainMenu = -1`（+0x24）。**本包没有任何读者**（原版亦然：
        /// `CanAddToGroup` 只读 +0x20）—— 留给 P8「主菜单容量」那一包。⛔ 别拿它替 `MaxActiveEvents`。</summary>
        public int maxActiveEventsInMainMenu = -1;
        /// <summary>原版 `private List&lt;string&gt; eventsInGroup`（+0x28）：装的是**事件 id**。</summary>
        public List<string> eventsInGroup = new List<string>();

        /// <summary>原版 `GetID()`（`EventGroup__GetID.c` 全文 = `return *(this + 0x18)`）。</summary>
        public virtual string GetID() { return id; }

        /// <summary>原版 `CanAddToGroup(ILiveOps liveOps)`（`EventGroup__CanAddToGroup.c`，逐句）：
        /// <code>
        /// if (MaxActiveEvents &gt;= 0) {
        ///     var list = eventsInGroup;
        ///     if (list.Count &lt; MaxActiveEvents) return true;      // 还没满
        ///     return list.Contains(liveOps.GetID());              // 满了 ⇒ 只有「已经在组里」的能留下
        /// }
        /// return true;                                            // 容量为负 = 不限
        /// </code>
        /// ⚠️ `liveOps == null` / `eventsInGroup == null` 原版 **NRE**；我们出声并返回 `false`（= 装不进，最保守）。</summary>
        public bool CanAddToGroup(ILiveOps liveOps)
        {
            if (MaxActiveEvents >= 0)
            {
                var list = eventsInGroup;
                if (list == null)
                {
                    Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.CanAddToGroup：`eventsInGroup` 是 null"
                                 + "（原版会 NRE）⇒ 按「装不进」处理");
                    return false;
                }
                if (list.Count < MaxActiveEvents) return true;
                if (liveOps == null)
                {
                    Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.CanAddToGroup(null)"
                                 + "（原版会 NRE）⇒ 按「装不进」处理");
                    return false;
                }
                return list.Contains(liveOps.GetID());
            }
            return true;
        }

        /// <summary>原版 `AddEvent(ILiveOps eventToAdd)`（`EventGroup__AddEvent.c`，逐句）：
        /// <code>
        /// var list = eventsInGroup;
        /// if (list.Contains(eventToAdd.GetID())) return;      // ★ 去重（已在组里 ⇒ 什么都不做）
        /// list.Add(eventToAdd.GetID());
        /// </code>
        /// ⚠️ **不检查容量** —— 容量只在 `CanAddToGroup` 那道门上（原版就是这样，⛔ 别在这里补一道）。
        /// ⚠️ `eventToAdd == null` 原版 NRE；我们出声返回。</summary>
        public void AddEvent(ILiveOps eventToAdd)
        {
            if (eventToAdd == null)
            {
                Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.AddEvent(null)（原版会 NRE）⇒ 忽略");
                return;
            }
            var list = eventsInGroup;
            if (list == null)
            {
                Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.AddEvent：`eventsInGroup` 是 null（原版会 NRE）⇒ 忽略");
                return;
            }
            string key = eventToAdd.GetID();
            if (list.Contains(key)) return;
            list.Add(key);
        }

        /// <summary>原版 `RemoveEvent(ILiveOps eventToRemove)`（`EventGroup__RemoveEvent.c`）：`eventsInGroup.Remove(id)`
        /// —— **返回值被丢掉**（不在组里也不出声）。我们保留同样的静默（这里不是失败：`CleanupTriggers` 会无差别地对每个组摘一次）。</summary>
        public void RemoveEvent(ILiveOps eventToRemove)
        {
            if (eventToRemove == null) { Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.RemoveEvent(null)（原版会 NRE）⇒ 忽略"); return; }
            var list = eventsInGroup;
            if (list == null) { Debug.LogError("[LiveOps] EventGroup '" + (id ?? "<null>") + "'.RemoveEvent：`eventsInGroup` 是 null（原版会 NRE）⇒ 忽略"); return; }
            list.Remove(eventToRemove.GetID());
        }

        public override string ToString() { return "EventGroup(" + (id ?? "<null>") + ", max=" + MaxActiveEvents + ", n=" + (eventsInGroup == null ? 0 : eventsInGroup.Count) + ")"; }
    }

    /// <summary>原版 `EventGroupConfig : IConfigData, IAssetProvider&lt;EventGroup&gt;`（`EventGroupConfig.cs`）。
    /// 成员：`private List&lt;EventGroup&gt; groups`(+0x10) · `ConfigKey =&gt; "EventGroups"` · `Groups =&gt; groups` ·
    /// `GetAssets() =&gt; groups`。
    /// <para>🔴 **数据源本地 0 命中**：原版走 `ConfigManager.GetConfig&lt;EventGroupConfig&gt;()`，即远端 TitleData 的
    /// `"EventGroups"` 键（`判据/LiveOps_C1W2_C1W3_第3轮.md` §五·4 / 账 row 175）。我们**没有 ConfigManager** ⇒
    /// 换成 <see cref="Current"/> 这一个可注入的静态口（谁拿到配置谁装进来）。
    /// ⛔ **别在这里编组名**（铁律 2）；空的时候由 <see cref="EventGroupAssetProvider"/> 出声。</para></summary>
    [Serializable]
    public class EventGroupConfig
    {
        /// <summary>原版 `private List&lt;EventGroup&gt; groups`（+0x10；ctor 里 `new`）。</summary>
        public List<EventGroup> groups = new List<EventGroup>();

        /// <summary>原版 `ConfigKey =&gt; "EventGroups"`（`EventGroupConfig__get_ConfigKey.c` 返回的字面量就是 `"EventGroups"`）。</summary>
        public string ConfigKey { get { return "EventGroups"; } }

        /// <summary>原版 `Groups =&gt; groups`。</summary>
        public List<EventGroup> Groups { get { return groups; } }

        /// <summary>原版 `GetAssets() =&gt; groups`（`EventGroupConfig__GetAssets.c` = `return *(this + 0x10)`）。</summary>
        public IEnumerable<EventGroup> GetAssets() { return groups; }

        // ── 我们的「ConfigManager.GetConfig<EventGroupConfig>()」等价物 ──────────────
        /// <summary>🔶 自己设计（非原版）：`Current` 是我们加的静态注入口，替原版 `ConfigManager.GetConfig&lt;EventGroupConfig&gt;()`（远端 TitleData，本地 0 命中，判据拿不到） · 调度台 W8P1 2026-10-26 裁定。`null` = 本地没有这份配置（⛔ 别用空表冒充「有配置但没组」）。</summary>
        public static EventGroupConfig Current;

        /// <summary>装上配置（生产由远端载荷那条链接；自检直接摆合成组）。</summary>
        public static void Install(EventGroupConfig config) { Current = config; }

        /// <summary>按 id 找组（找不到 ⇒ null；`null` id 也 ⇒ null）。</summary>
        public static EventGroup Find(string groupId)
        {
            var cfg = Current;
            if (cfg == null || cfg.groups == null || string.IsNullOrEmpty(groupId)) return null;
            for (int i = 0; i < cfg.groups.Count; i++)
            {
                var g = cfg.groups[i];
                if (g != null && g.GetID() == groupId) return g;
            }
            return null;
        }

        public static void ResetForTest() { Current = null; }
    }

    /// <summary>原版 `EventGroupAssetProvider : AssetProvider&lt;EventGroup&gt;`（`EventGroupAssetProvider.cs`）：
    /// `GetAssets() =&gt; ConfigManager.GetConfig&lt;EventGroupConfig&gt;().groups`。
    /// <para>⚠️ 原版配置为 null 时**NRE**；我们出声后返回空表（不是静默：日志会说清「哪一份配置、为什么没有」）。</para></summary>
    public sealed class EventGroupAssetProvider
    {
        static readonly EventGroup[] Empty = new EventGroup[0];

        public IEnumerable<EventGroup> GetAssets()
        {
            var cfg = EventGroupConfig.Current;
            if (cfg == null)
            {
                Debug.LogError("[LiveOps] EventGroupAssetProvider.GetAssets：`EventGroupConfig` 没有装上"
                             + "（原版这里是 `ConfigManager.GetConfig<EventGroupConfig>()` → NRE；"
                             + "本地 TitleData `\"EventGroups\"` 0 命中，见账 row 175）⇒ 返回空表");
                return Empty;
            }
            return cfg.groups ?? (IEnumerable<EventGroup>)Empty;
        }
    }

    // ============================================================ §4 活动位置（原版 `EventLocation` / `EventLocationType`）
    /// <summary>原版 `EventLocationType`（`EventLocationType.cs` 全文，**值逐个照抄**）：`Main=0 · None=1 · Parent=2`。</summary>
    public enum EventLocationType
    {
        Main = 0,
        None = 1,
        Parent = 2,
    }

    /// <summary>原版 `[Serializable] public class EventLocation`（`EventLocation.cs`）：`public EventLocationType type;` +
    /// `public LoadableReference&lt;ParentEventData&gt; parent;` + `public bool Match(EventLocationType type, string id)`。
    /// <para>⚠️ `parent` 是**资产引用**（`TargetId` + 一个 `ParentEventData` 资产）；我们**没有那个资产** ⇒ 只留
    /// `parentId`（= `TargetId` 那半）。⛔ 别把它当「父事件的完整数据」。</para></summary>
    [Serializable]
    public sealed class EventLocation
    {
        /// <summary>原版 `public EventLocationType type`（+0x10）。</summary>
        public EventLocationType type;
        /// <summary>原版 `public LoadableReference&lt;ParentEventData&gt; parent`（+0x18）的 **TargetId 那半**。
        /// `null`/空 与 `TargetId == null` 等价（原版 `Match` 里两者都折成 `string.Empty`）。</summary>
        public string parentId;

        /// <summary>原版 `Match(EventLocationType type, string id)`（`EventLocation__Match.c`，逐句）：
        /// <code>
        /// if (id == null) id = string.Empty;                       // ← 原版取 String.Empty 静态
        /// var target = (parent == null) ? null : parent.TargetId;
        /// if (target == null) target = string.Empty;
        /// if (this.type != type) return false;                     // ★ 先比 type
        /// return string.Equals(id, target);                        // ★ 再比 id（序数比较）
        /// </code>
        /// 🔴 **两处 `null → ""` 的折叠照抄**：`Parent` 位置且 `parentId` 空时，传 `null` 或传 `""` **都算匹配**。</summary>
        public bool Match(EventLocationType type, string id)
        {
            string want = id == null ? string.Empty : id;
            string target = string.IsNullOrEmpty(parentId) ? string.Empty : parentId;
            if (this.type != type) return false;
            return string.Equals(want, target);
        }

        public override string ToString() { return "EventLocation(" + type + ", parent=" + (parentId ?? "<null>") + ")"; }
    }

    // ============================================================ §5 `EventFilter` 的**行为**那半（`partial` 另一半在 LiveOpsEvent.cs）
    /// <summary>原版 `EventFilter` 的 8 个方法，逐条照抄（Ghidra 体全在）。
    /// <para>🔴 **调用次序（照抄，⛔ 别调换）**：
    /// `Validate` = `CheckEventGroups()` &amp;&amp; `Conditions.All(c =&gt; c.Validate(this))`（短路：组不满就不再看条件）；
    /// `TryActivateEvent` = `Log` → `liveOps.CanActivate()` → `Validate()` → `Log` → `ToggleEventActivation(true,true,true)` → `liveOps.OnEventTriggered()`。</para>
    /// <para>🔴 **组怎么解析**：原版 `eventGroups` 是 `List&lt;LoadableReference&lt;EventGroup&gt;&gt;`（引用，惰性 `get_Reference()`）；
    /// 我们是 `string[] eventGroupIds` + <see cref="ResolveGroups"/>（查 <see cref="EventGroupConfig"/>）。
    /// 解析不到的 id **出声一次**（每 id 一次，⛔ 不刷屏）后**跳过** —— 这是**数据缺口**，不是「这个组不存在」。</para></summary>
    public partial class LiveOpsEventFilter
    {
        // ── 解析不到组的 id（每个只出声一次；静态 ⇒ 跨事件实例共享）──────────────
        static readonly HashSet<string> _warnedMissingGroupIds = new HashSet<string>();
        /// <summary>自检口：清「已出声」表。</summary>
        public static void ResetWarnedForTest() { _warnedMissingGroupIds.Clear(); }

        /// <summary>把 `eventGroupIds` 解析成组实例（顺序与 ids 一致；解析不到的**跳过**）。</summary>
        public EventGroup[] ResolveGroups()
        {
            if (eventGroupIds == null || eventGroupIds.Length == 0) return null;
            var list = new List<EventGroup>(eventGroupIds.Length);
            for (int i = 0; i < eventGroupIds.Length; i++)
            {
                string gid = eventGroupIds[i];
                if (string.IsNullOrEmpty(gid)) continue;
                var g = EventGroupConfig.Find(gid);
                if (g == null)
                {
                    if (_warnedMissingGroupIds.Add(gid))
                    {
                        Debug.LogWarning("[LiveOps] EventFilter.eventGroups 里的组 '" + gid + "' 在 `EventGroupConfig` 里找不到"
                                       + "（本地 TitleData `\"EventGroups\"` 0 命中，账 row 175）⇒ **这个组被跳过**；"
                                       + "过滤/分组/容量判定都看不见它（本条只声明一次）");
                    }
                    continue;
                }
                list.Add(g);
            }
            return list.Count == 0 ? null : list.ToArray();
        }

        /// <summary>原版 `public bool HasTriggers =&gt; Triggers != null &amp;&amp; Triggers.Count &gt; 0`
        /// （`EventFilter__get_HasTriggers.c`：`if (Triggers != null) return 0 &lt; DynamicList.get_Count();`
        /// ⇒ **`Triggers == null` 时原版会 NRE**；我们按 `false` 处理并**不出声**，因为本类字段初始化器给了空表）。</summary>
        public bool HasTriggers { get { return Triggers != null && Triggers.Count > 0; } }

        /// <summary>原版 `public long GetExpirationTime(ILiveOps liveOps)`（`EventFilter__GetExpirationTime.c`，逐句）：
        /// <code>
        /// var save = liveOps.GetBaseSave();
        /// if (save == null) throw NRE;
        /// return (long)(eventDuration * 3600000) + save.triggerTime;
        /// </code>
        /// 🔴 与 `LiveOpsEvent&lt;TSave,TData&gt;.Initialize()` 里那条算式**逐字相同**（同一份「下次刷新时刻」口径）。
        /// ⚠️ 原版是 **32 位乘法**（`eventDuration * 3600000` 在 int 里算）⇒ 超过 ~596 小时会溢出（照抄，见 `CanActivate` 的注释）。
        /// ⚠️ `save == null` 原版 NRE；我们出声返回 0。</summary>
        public long GetExpirationTime(ILiveOps liveOps)
        {
            if (liveOps == null) { Debug.LogError("[LiveOps] EventFilter.GetExpirationTime(null)（原版会 NRE）⇒ 返回 0"); return 0L; }
            var save = liveOps.GetBaseSave();
            if (save == null)
            {
                Debug.LogError("[LiveOps] EventFilter.GetExpirationTime：活动 '" + (liveOps.GetID() ?? "<null>") + "' 没有存档"
                             + "（原版会 NRE）⇒ 返回 0");
                return 0L;
            }
            int span = unchecked(eventDuration * 3600000);     // 原版就是 32 位乘法（`imul`）
            return (long)span + save.triggerTime;
        }

        /// <summary>原版 `public bool Validate(ILiveOps liveOps)`（`EventFilter__Validate.c`，逐句）：
        /// <code>
        /// if (!CheckEventGroups(liveOps)) return false;                  // ① 组容量/归属
        /// foreach (var c in Conditions) if (!c.Validate(liveOps)) return false;   // ② 条件全过
        /// return true;
        /// </code>
        /// ⚠️ `Conditions == null` 原版 NRE；我们出声并按「没有条件」处理（= 只由 ① 决定）。</summary>
        public bool Validate(ILiveOps liveOps)
        {
            if (!CheckEventGroups(liveOps)) return false;
            var conds = Conditions;
            if (conds == null)
            {
                Debug.LogError("[LiveOps] EventFilter.Validate：`Conditions` 是 null（原版会 NRE）⇒ 按「没有条件」处理"
                             + "（= 只由 CheckEventGroups 决定；⛔ 别把这里当成「条件全过」的等价物）");
                return true;
            }
            for (int i = 0; i < conds.Count; i++)
            {
                var c = conds[i];
                if (c == null) continue;
                if (!c.Validate(liveOps)) return false;
            }
            return true;
        }

        /// <summary>原版 `public bool CheckEventGroups(ILiveOps liveOps)`（`EventFilter__CheckEventGroups.c`，逐句）：
        /// <code>
        /// foreach (var r in eventGroups) if (!r.Reference.CanAddToGroup(liveOps)) return false;
        /// return true;
        /// </code>
        /// 🔴 **语义是「这个事件能不能进它所属的每一个组」**（不是「组里有没有它」）：容量满了且它不在组里 ⇒ false。
        /// `eventGroups` 为 null ⇒ 原版在 `if (*(this+0x28) == 0) throw NRE` 那一步崩；我们按「没有组 ⇒ true」处理并出声？——
        /// ⛔ 不：`eventGroups` 为 null 在我们这里 = **数据没抄**（不是「没有组」），所以**静默 true** 会让「组容量」这道门
        /// **看起来永远通过**。⇒ 本实现：`eventGroupIds == null` ⇒ 返回 `true` 但**不出声**（因为 13 份 SO 的这个字段
        /// 在本地目录层恒 null，出声会污染每一次 `Validate`）；**数据缺口的出声点在 `ResolveGroups` + 账 row 175**。</summary>
        public bool CheckEventGroups(ILiveOps liveOps)
        {
            var groups = ResolveGroups();
            if (groups == null) return true;
            for (int i = 0; i < groups.Length; i++)
            {
                if (!groups[i].CanAddToGroup(liveOps)) return false;
            }
            return true;
        }

        /// <summary>原版 `public void AddOrRemoveFromGroups(ILiveOps liveOps, bool addOrRemove)`（`EventFilter__AddOrRemoveFromGroups.c`，逐句）：
        /// <code>
        /// if (eventGroups == null) return;                 // ★ 唯一的早退
        /// foreach (var r in eventGroups) {
        ///     if (!addOrRemove) r.Reference.RemoveEvent(liveOps);   // ← 先判 !addOrRemove
        ///     else              r.Reference.AddEvent(liveOps);
        /// }
        /// </code>
        /// 调用点是 `LiveOpsEvent.ToggleEventActivation`（激活 ⇒ 进组；停用 ⇒ 出组）。</summary>
        public void AddOrRemoveFromGroups(ILiveOps liveOps, bool addOrRemove)
        {
            if (eventGroupIds == null || eventGroupIds.Length == 0) return;
            var groups = ResolveGroups();
            if (groups == null) return;
            for (int i = 0; i < groups.Length; i++)
            {
                if (addOrRemove) groups[i].AddEvent(liveOps);
                else groups[i].RemoveEvent(liveOps);
            }
        }

        /// <summary>原版 `public void RegisterTriggers(ILiveOps liveOps)`（`EventFilter__RegisterTriggers.c`，逐句）：
        /// <code>
        /// var closure = new &lt;&gt;c__DisplayClass15_0 { __this = this, liveOps = liveOps };
        /// if (closure.liveOps == null) throw NRE;
        /// Log("…" + <b>((ILiveOps)closure.liveOps).GetID()</b>);
        /// foreach (var t in Triggers) t.Register(closure.&lt;RegisterTriggers&gt;b__0);   // ← **同一个回调**，惰性建一次
        /// </code>
        /// 而 `b__0`（`…DisplayClass15_0___RegisterTriggers_b__0.c` 全文）=
        /// `this.__this.TryActivateEvent(this.liveOps)` ⇒ **信号到 ⇒ 试着激活这个事件**。
        /// 🔴 **`Triggers` 为 null / 空 ⇒ 原版直接 NRE**（`if ((this+0x30) != null &amp;&amp; list != null) {…} else throw`）。
        /// 我们按「空表 = 没有触发器」静默返回 —— 但**只在 `Triggers == null` 时**，因为那是数据层没抄，不是原版语义。</summary>
        public void RegisterTriggers(ILiveOps liveOps)
        {
            if (liveOps == null) { Debug.LogError("[LiveOps] EventFilter.RegisterTriggers(null)（原版会 NRE）⇒ 忽略"); return; }
            if (Triggers == null || Triggers.Count == 0) return;
            if (LiveOpsSignals.Verbose) Debug.Log("[LiveOps] 为活动 '" + (liveOps.GetID() ?? "<null>") + "' 注册 "
                                                 + Triggers.Count + " 条触发器");
            Action cb = delegate { TryActivateEvent(liveOps); };
            for (int i = 0; i < Triggers.Count; i++)
            {
                var t = Triggers[i];
                if (t == null) { Debug.LogWarning("[LiveOps] EventFilter.Triggers 第 " + i + " 项是 null ⇒ 跳过（原版会 NRE）"); continue; }
                t.Register(cb);
            }
        }

        /// <summary>原版 `public void CleanupTriggers(ILiveOps liveOps)`（`EventFilter__CleanupTriggers.c`，逐句）：
        /// <code>
        /// if (liveOps == null) return;                                  // ★ 早退（唯一的 null 检查）
        /// foreach (var r in eventGroups) { var g = r.Reference; if (g != null) g.RemoveEvent(liveOps); }
        /// if (Triggers != null &amp;&amp; Triggers.list != null)
        ///     foreach (var t in Triggers) t.Unregister();
        /// </code>
        /// 🔴 **两件事**：① 从**每个组**里摘掉本事件（不是「只摘 Add 过的那几个」）；② 逐条 trigger `Unregister`。
        /// 调用点 = `LiveOpsEvent.Dispose()`（`LiveOpsHandler.ClearEvents` 逐个 Dispose）。
        /// ⚠️ 组解析失败（数据缺口）时这里**不出声**——`ResolveGroups` 已经为本 id 出过一次声。</summary>
        public void CleanupTriggers(ILiveOps liveOps)
        {
            if (liveOps == null) return;
            var groups = ResolveGroups();
            if (groups != null)
            {
                for (int i = 0; i < groups.Length; i++) groups[i].RemoveEvent(liveOps);
            }
            if (Triggers != null && Triggers.Count > 0)
            {
                for (int i = 0; i < Triggers.Count; i++)
                {
                    var t = Triggers[i];
                    if (t == null) continue;
                    t.Unregister();
                }
            }
        }

        /// <summary>原版 `private void TryActivateEvent(ILiveOps liveOps)`（`EventFilter__TryActivateEvent.c`，逐句）：
        /// <code>
        /// Log("…" + liveOps.GetID());                    // 槽 0 = IIdentifiable.GetID
        /// if (liveOps.CanActivate()) {                    // 槽 5 = ILiveOps.CanActivate
        ///     if (Validate(liveOps)) {
        ///         Log("…" + liveOps.GetID());
        ///         liveOps.ToggleEventActivation(true, true, true);   // 槽 13
        ///         liveOps.OnEventTriggered();                        // 槽 14
        ///     }
        /// }
        /// </code>
        /// 🔴 **槽位复原**（`0x5` / `0xd` / `0xe` 三个字面量）：按 `ILiveOps` 的扁平方法表
        /// （0 `GetID` · 1 `GetContainer` · 2 `Expire` · 3 `Dispose` · 4 `IsActive` · **5 `CanActivate`** ·
        /// 6 `SelectEvent` · 7 `ResetEventProgress` · 8 `RegisterEventTriggers` · 9 `CheckEventFilter` · 10 `RefreshExpire` ·
        /// 11 `MarkAsOld` · 12 `MarkAsSeen` · **13 `ToggleEventActivation`** · **14 `OnEventTriggered`** · 15 `GetBaseData` …
        /// 与既有判据件用的 15/16/21/23 逐值吻合）⇒ 14 只能是 `OnEventTriggered`。
        /// **它在本工程基类是空方法**（`LiveOpsEvent__OnEventTriggered.c` = 共享空桩 `ret`），由具体子类覆盖；
        /// 我们如实留一个空 virtual（见 `LiveOpsEvent.OnEventTriggered`）。</summary>
        public void TryActivateEvent(ILiveOps liveOps)
        {
            if (liveOps == null) { Debug.LogError("[LiveOps] EventFilter.TryActivateEvent(null)（原版会 NRE）⇒ 忽略"); return; }
            if (LiveOpsSignals.Verbose) Debug.Log("[LiveOps] 收到信号：试着激活活动 '" + (liveOps.GetID() ?? "<null>") + "'");
            if (!liveOps.CanActivate()) return;                       // 冷却未到（CanActivate 自己会出声说明差多少）
            if (!Validate(liveOps)) return;                           // 组/条件不过
            if (LiveOpsSignals.Verbose) Debug.Log("[LiveOps] 条件满足 ⇒ 激活活动 '" + (liveOps.GetID() ?? "<null>") + "'");
            liveOps.ToggleEventActivation(true, true, true);
            liveOps.OnEventTriggered();
        }
    }

    // ============================================================ §6 `ParentEventHandler.BeforeRefresh`（原版 `ParentEventHandler : LiveOpsHandler<ParentEventData>`）
    /// <summary>原版 `ParentEventHandler`（`ParentEventHandler.cs`）在本工程的等价物：**只落 `BeforeRefresh` 那一个覆盖**
    /// （它自己那份 handler 的其余成员属 P8「主菜单活动容器」）。
    /// <para>🔴 **为什么是一个抽象子类而不是一个静态函数**：原版这一跳是 `protected override void BeforeRefresh()`
    /// —— 我们 `LiveOpsHandler.BeforeRefresh()` 也已经是 `protected virtual` 空实现（`Shell/LiveOpsHandler.cs:522`）
    /// ⇒ 形状对位。将来哪个 handler 管 Parent 事件，就继承本类（**改动请求见报告**，`LiveOpsHandler.cs` 不在本包白名单）。</para>
    /// <para>⚠️ 本类**当前没有被任何 handler 继承**（我们的 `LiveOpsLocalHandler` 一把抓所有 kind）⇒ 生产链路里
    /// `BeforeRefresh` 还是基类空实现。自检直接调 <see cref="LiveOpsParentEventRefresh.Run"/> 验行为。</para></summary>
    public abstract class ParentEventHandler : LiveOpsHandler
    {
        /// <summary>原版 `protected override void BeforeRefresh()`（`ParentEventHandler__BeforeRefresh.c`，逐句）：
        /// <code>
        /// if (!this.events.All(e =&gt; e.EventState != LiveOpState.Active)) return;   // ★ 只要有一个在期就什么都不做
        /// Log("…");                                                               // 原版字面量（`DAT_184281878`，未读）→ 我们自己写一句
        /// var nav = SingletonBehaviour&lt;NavigationPanelController&gt;.Instance;
        /// var eventsToggle = nav.GetToggle(NavigationPanelToggleType.Event);      // 5
        /// if (eventsToggle.toggle.isOn) nav.GetToggle(NavigationPanelToggleType.Main).toggle.isOn = true;   // 0
        /// nav.OnEventsRefreshed();
        /// </code>
        /// 谓词本体 = `ParentEventHandler.__c___BeforeRefresh_b__2_0.c`：`return e.EventState != 2;`（2 = `Active`）。
        /// 🔴 **语义**：Parent 这一类事件**全都不在期**时 —— ① 如果当前正停在「活动」页签，把导航点回「主」
        /// ② 通知导航重建活动钮。⛔ 不是「关掉 EVENT 灯」（那是 P8 的 C1-W6，另一条）。</para>
        /// <para>⚠️ `GetToggle` / `OnEventsRefreshed` 在我们这里**没有等价物**（`MainMenuRuntime` 只有 `SetNavToggle`
        /// 与「只重建高亮」的 `SelectNav`）⇒ 走 <see cref="LiveOpsParentEventRefresh"/> 的三个可注入钩子；
        /// 缺失的那个**出声**而不是假装做完。</para></summary>
        protected override void BeforeRefresh()
        {
            LiveOpsParentEventRefresh.Run(BeforeRefreshEvents);
        }

        /// <summary>原版 `BeforeRefresh` 读的是 **`this.events`** —— 而原版那个 handler 是
        /// `LiveOpsHandler&lt;ParentEventData&gt;`，**只装 Parent 族事件**。
        /// 🔶 自己设计（非原版）：我们是**合并 handler**（`LiveOpsLocalHandler` 一把抓 4 个 kind，
        /// 见 `Shell/LiveOpsHandler.cs:942`），`events` 里混着 Scoring / PremiumItemOffer / FastMode
        /// ⇒ 直接喂 `events` 会让谓词 `All(e =&gt; e.EventState != Active)` 被**别的族**的在期事件挡住
        /// （原版不会被挡）。所以留这一个取口：**默认 = 全部 `events`（= 原版形状）**，
        /// 合并 handler 覆盖它把范围收窄到 Parent 族。
        /// 为什么必须自己定：原版「一个 handler 一个事件族」的结构在我们的合并实现里不存在。
        /// 谁定的 + 日期：调度台 B92 2026-10-28。</summary>
        protected virtual IEnumerable<ILiveOps> BeforeRefreshEvents { get { return events; } }
    }

    /// <summary>`ParentEventHandler.BeforeRefresh` 的主体 + 三个钩子（原版直接摸 `NavigationPanelController`）。
    /// <para>钩子默认值：`SelectMainTab` = `MainMenuRuntime.SetNavToggle(Main)`（**我们有这一颗**）；
    /// `IsEventTabSelected` / `OnEventsRefreshed` **默认 null**（没有等价物）⇒ 前者按 `false`（= 不动导航，
    /// 与「EVENT 页签本就没开」同效），后者出声一次。</para></summary>
    public static class LiveOpsParentEventRefresh
    {
        /// <summary>🔶 自己设计（非原版）：`IsEventTabSelected` 是代替「原版直接摸 `NavigationPanelController.GetToggle(Event).toggle.isOn`」的**注入口**（本仓没有那个控制器 · 调度台 W8P1 2026-10-26 裁定）。`null` ⇒ 当作 `false`。</summary>
        public static Func<bool> IsEventTabSelected;
        /// <summary>原版 `nav.GetToggle(Main).toggle.isOn = true`（**写**）。默认走本仓共用口 `SetNavToggle`。</summary>
        public static Action SelectMainTab = SelectMainTabDefault;
        /// <summary>原版 `nav.OnEventsRefreshed()` —— 默认实现 = 让**主菜单**把左栏整棵重建（🆕 B89-E30 · 账 `L477`）。
        /// <para>为什么要有默认实现：我们这一侧的重建对象（`Navigation Panel`）是 **`MainMenuRuntime` 实例上的树**，
        /// 静态钩子只能在**调用那一刻**去取实例（同 `SelectMainTab = SelectMainTabDefault` 那条口径）。
        /// 之前 `null` ⇒ 生产里这一段**永远只打一条「本仓没有等价物」的告警**、活动钮从不重建。
        /// `null` 仍然合法（自检可以临时摘掉）。</para></summary>
        public static Action OnEventsRefreshed = OnEventsRefreshedDefault;

        /// <summary>自检口：`Run` 走了几次、最后一次的判定（`true` = 真的执行了导航那半）。</summary>
        public static int RunCount { get; private set; }
        public static bool LastRunActed { get; private set; }

        static bool _warnedNoMainButton;
        static bool _warnedNoEventsRefreshed;

        static void SelectMainTabDefault()
        {
            // 复用「点亮第 i 颗导航」的**唯一共用口**（⛔ 不另写一份点亮逻辑）。
            MainMenuRuntime.SetNavToggle(MainMenuRuntime.NavigationPanelToggleType.Main);
        }

        /// <summary>原版 `BeforeRefresh` 的主体。`evts` = handler 自己的事件表（原版 `this.events`）。
        /// 返回是否执行了「导航那半」（自检用）。</summary>
        public static bool Run(IEnumerable<ILiveOps> evts)
        {
            RunCount++;
            LastRunActed = false;
            if (evts == null)
            {
                Debug.LogError("[LiveOps] ParentEventHandler.BeforeRefresh：事件表是 null（原版会 NRE）⇒ 什么都不做");
                return false;
            }
            // ★ 谓词：只要还有任何一个事件是 Active(2) ⇒ 直接返回
            foreach (var e in evts)
            {
                if (e == null) continue;
                if (e.EventState == LiveOpState.Active) return false;
            }

            if (LiveOpsSignals.Verbose) Debug.Log("[LiveOps] Parent 事件全部不在期 ⇒ 把导航退回主菜单并通知活动钮刷新");

            bool eventTabOn = false;
            if (IsEventTabSelected != null) eventTabOn = IsEventTabSelected();
            if (eventTabOn)
            {
                if (SelectMainTab != null) SelectMainTab();
                else if (!_warnedNoMainButton)
                {
                    _warnedNoMainButton = true;
                    Debug.LogWarning("[LiveOps] ParentEventHandler.BeforeRefresh：活动页签开着，但 `SelectMainTab` 钩子被置空 ⇒ 导航**没退回去**（本条只声明一次）");
                }
            }

            if (OnEventsRefreshed != null) OnEventsRefreshed();
            else if (!_warnedNoEventsRefreshed)
            {
                _warnedNoEventsRefreshed = true;
                Debug.LogWarning("[LiveOps] ParentEventHandler.BeforeRefresh：`NavigationPanelController.OnEventsRefreshed` 在本仓**没有等价物**"
                               + " ⇒ 活动钮没有重建（原版这一步会按最新事件列表重建导航上的活动钮；改动请求见报告，本条只声明一次）");
            }
            LastRunActed = true;
            return true;
        }

        /// <summary>自检收尾用：钩子还原成默认、计数清零。</summary>
        public static void ResetForTest()
        {
            IsEventTabSelected = null;
            SelectMainTab = SelectMainTabDefault;
            OnEventsRefreshed = OnEventsRefreshedDefault;
            RunCount = 0;
            LastRunActed = false;
            _warnedNoMainButton = false;
            _warnedNoEventsRefreshed = false;
        }

        // ============================================================================================
        //  🆕 B89-E30（账 `L477` `NavigationPanelController.OnEventsRefreshed`）：默认实现
        //
        //  原版那一跳是 `nav.OnEventsRefreshed()` —— 拿**导航控制器单例**、把活动钮按最新事件表重建。
        //  我们这侧：
        //    · 原版的 `NavigationPanelController` 在我们这儿**没有对应的类**（判据 → 本类上面那段注释：
        //      我们只有 `MainMenuRuntime.SetNavToggle` 与「只重建高亮」的 `SelectNav`）；
        //    · 那棵导航面板树是 **`MainMenuRuntime` 实例**建的 ⇒ 等价物 = 「取实例，让它重建左栏」
        //      （`MainMenuRuntime.RebuildNavForLiveOpsRefresh`，内部走 `BuildNavigationPanel`，
        //       第 6 颗活动钮在那里按当前在期 Parent 事件重建 —— 见 `BuildEventNavButtons`）。
        //  ⚠️ **取不到实例**（战斗场景 / 直起自检 / 菜单还没建）⇒ **出声**（红线：不许静默失败），
        //     与原版「单例为 null 就 NRE」不同 —— 那一档我们在离线复刻里是常态，NRE 会打断整条刷新链。
        //  ⚠️ 形态与 `SelectMainTabDefault` **逐字同形**（静态默认实现 + 调时才解析实例），
        //     ⛔ 别改成「在 `Build()` 里 `+=` 订阅」：静态 Action 是**单值**，`+=` 会与默认实现串成两条 ⇒ 重建两次。
        // ============================================================================================
        static void OnEventsRefreshedDefault()
        {
            var mm = MainMenuRuntime.Instance;
            if (mm != null) { mm.RebuildNavForLiveOpsRefresh(); return; }
            Debug.LogWarning("[LiveOps] `OnEventsRefreshed`：场上没有 `MainMenuRuntime`（战斗 / 直起自检 / 菜单未建）"
                           + " ⇒ 活动钮**没重建**（原版这一步会按最新事件表重建导航上的活动钮）");
        }
    }

    // ============================================================ §7 自检（供 `ShellScene.Run` 调用）
    /// <summary>本包的验收口（施工单那四条 + 本包补的几条）。**不改 `Editor/ShellScene.cs`**（白名单外）⇒
    /// 断言以本类为唯一实现，宿主只要一句 `LiveOpsFilterSelfCheck.Run(Check);`。
    /// <para>🔴 **不写盘**：夹具事件覆盖 `SaveEventProgress` 空实现（基类就是空），`Handler` 为 null ⇒
    /// `ToggleEventActivation` 的 `forceReload` / `saveOnServer` 两跳都不会落到 `SaveCenter`。</para>
    /// <para>⚠️ **会打几条 `LogError` 是正常的**：⑧ 那一段故意让 `LiveOpsPopupPresenter.Open` 返回 `null`
    /// （好抓 `label`），于是 `OpenWindow` 走「引用解析不到」那一支 —— 那一支在原版里就是 `CustomDebug.LogError` 后 return
    /// （`LiveOpsEvent__OpenWindow.c` 的 default 分支）。**日志红 ≠ 断言红**。</para>
    /// <para>⚠️ 自检**自己**抛异常时只记一条失败（`catch`），不会把 `ShellScene.Run` 后面几千条断言带崩。</para></summary>
    public static class LiveOpsFilterSelfCheck
    {
        /// <summary>夹具：一个最小可用的活动实例（不依赖 `LiveOpsLocalCatalog`）。
        /// ⚠️ **不覆盖 `SaveEventProgress`**（基类空 ⇒ 不写盘）。</summary>
        sealed class FixtureEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
        {
            public FixtureEvent(string id) : this(id, null) { }
            public FixtureEvent(string id, ILiveOpsHandler handler)
            {
                var d = new LiveOpsEventData { eventId = id, activationType = LiveOpsActivationType.OnDate };
                var s = new LiveOpsSave { eventId = id, IsNew = true };
                LoadEventData(d, s, handler);
            }
            public LiveOpsEventData D { get { return Data; } }
            public LiveOpsSave S { get { return SaveGame; } }
        }

        /// <summary>夹具触发器：只记「被 Register 过几次、Unregister 过几次、回调被调过几次」。</summary>
        sealed class CountingTrigger : LiveOpsEventTrigger
        {
            public int RegisterCalls, UnregisterCalls, CallbackCalls;
            Action _cb;
            public override void Register(Action callback) { RegisterCalls++; _cb = callback; }
            public override void Unregister() { UnregisterCalls++; _cb = null; }
            public void Fire() { CallbackCalls++; if (_cb != null) _cb(); }
            public bool IsRegistered { get { return _cb != null; } }
        }

        /// <summary>夹具 handler：只为 `GetAllEvents()` / `ResetHandlers()` 提供一张**可清空**的事件表
        /// （`LiveOpsHandler` 基类的 `SetupEvents(ops, blocked, shared)` 就是原版那三参入口）。</summary>
        sealed class FixtureHandler : LiveOpsHandler
        {
            public override string HandlerKey { get { return "fixture"; } }
        }

        /// <summary>夹具共享 context：只记「`Unpack` 交给我的是哪一份对象」，用来区分
        /// 「传整份 `SharedEventData`」与「传 `energyData` 那一段 JSON」。</summary>
        sealed class FixtureSharedContext : LiveOpsSharedContext
        {
            public SharedEventData LastUnpacked;
            public override void UnpackData(SharedEventData sharedEventData) { LastUnpacked = sharedEventData; }
        }

        /// <summary>夹具信号触发器的「算不算数」判据：只有 `EventId == Want` 才算。</summary>
        sealed class CountingSignalTrigger : SignalEventTrigger<EventExpiredSignal>
        {
            public string Want;
            public int Fired;
            protected override bool ValidateSignal(EventExpiredSignal signal)
            {
                if (signal == null || signal.EventId != Want) return false;
                Fired++;
                return true;
            }
        }

        /// <summary>跑全部断言。`check(条件, 断言文案)` 由宿主提供（`ShellScene.Run` 的 `Check`）。
        /// 返回执行的断言条数。</summary>
        public static int Run(Action<bool, string> check)
        {
            if (check == null) { Debug.LogError("[LiveOps] LiveOpsFilterSelfCheck.Run(null) ⇒ 不跑"); return 0; }
            int n = 0;
            Action<bool, string> C = delegate (bool ok, string msg) { n++; check(ok, msg); };

            DateTime now = new DateTime(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);
            LiveOpsClock.NowProvider = delegate { return now; };
            var oldVerbose = LiveOpsSignals.Verbose;
            LiveOpsSignals.Verbose = false;                  // 自检不刷过程日志
            try
            {
                LiveOpsSignal.ResetForTest();
                LiveOpsSignals.ResetForTest();
                LiveOpsSignal.ReinstallLegacyBridge();
                EventGroupConfig.ResetForTest();
                LiveOpsEventFilter.ResetWarnedForTest();
                LiveOpsParentEventRefresh.ResetForTest();
                LiveOpsPopupPresenter.ResetForTest();

                // ── ① hasDuration=false ⇒ 不进排期表（`Initialize` 唯一早退条件）──────────────
                {
                    // 🔴 必须有 handler：`Initialize()` 里 `Handler == null` 会**先于**算式返回（原版也读 Handler，只是会 NRE）
                    // ⇒ 不给 handler 的话「PendingCount == 0」是**空断言**（灭自证的反面）。
                    var handler = new LiveOpsLocalHandler();
                    var ev = new FixtureEvent("A_no_duration", handler);
                    ev.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    ev.D.filter.hasDuration = false;
                    // ⚠️ 用**增量**判断：`LiveOpsScheduler` 是进程级静态，ShellScene.Run 里别的断言可能已经排过东西
                    int pendingBefore = LiveOpsScheduler.PendingCount;
                    ev.Initialize();
                    C(LiveOpsScheduler.PendingCount == pendingBefore,
                      "P1/① filter.hasDuration=false ⇒ `Initialize()` 不进排期表（`LiveOpsScheduler.PendingCount` 应保持 "
                      + pendingBefore + "，实 " + LiveOpsScheduler.PendingCount + "）");
                    ev.D.filter.hasDuration = true;
                    ev.D.filter.eventDuration = 24;
                    ev.S.triggerTime = LiveOpsEventTimeData.ToMs(now.AddHours(-1));
                    ev.Initialize();
                    C(LiveOpsScheduler.PendingCount == pendingBefore + 1,
                      "P1/① 反例（灭自证）：hasDuration=true ⇒ 排了一次刷新（应 " + (pendingBefore + 1)
                      + "，实 " + LiveOpsScheduler.PendingCount + "）");
                    LiveOpsScheduler.ResetForTest();
                }

                // ── ② 冷却门：now-triggerTime < eventCooldown*3600000 ⇒ CanActivate=false ────────
                {
                    var ev = new FixtureEvent("B_cooldown");
                    ev.D.filter.hasCooldown = false;
                    C(ev.CanActivate(), "P1/② `hasCooldown=false` ⇒ `CanActivate()` 恒真（不走冷却算式）");
                    ev.D.filter.hasCooldown = true;
                    ev.D.filter.eventCooldown = 168;                                     // 原版默认 168 小时
                    ev.S.triggerTime = LiveOpsEventTimeData.ToMs(now.AddHours(-1));       // 只过了 1 小时
                    C(!ev.CanActivate(), "P1/② 冷却未到（1h < 168h）⇒ `CanActivate()` 假");
                    ev.S.triggerTime = LiveOpsEventTimeData.ToMs(now.AddHours(-200));     // 过了 200 小时
                    C(ev.CanActivate(), "P1/② 冷却已过（200h > 168h）⇒ `CanActivate()` 真");
                    ev.S.triggerTime = 0L;                                               // 从没触发过
                    C(ev.CanActivate(), "P1/② `triggerTime=0`（从没触发）⇒ `CanActivate()` 真（elapsed = now 极大）");
                    ev.D.filter.eventCooldown = 0;                                        // 边界：0 小时
                    ev.S.triggerTime = LiveOpsEventTimeData.ToMs(now);
                    C(ev.CanActivate(), "P1/② `eventCooldown=0` 且 `elapsed=0` ⇒ 真（判据是严格小于 `elapsed < 0` 才假）");
                }

                // ── ③ 组容量：`MaxActiveEvents=1` ⇒ 第二个事件 CanAddToGroup=false ─────────────
                {
                    var g = new EventGroup { id = "g_cap1", MaxActiveEvents = 1 };
                    EventGroupConfig.Install(new EventGroupConfig { groups = new List<EventGroup> { g } });
                    var a = new FixtureEvent("C_first");
                    var b = new FixtureEvent("C_second");
                    C(g.CanAddToGroup(a), "P1/③ 空组容量 1 ⇒ 第一个事件 `CanAddToGroup` 真");
                    g.AddEvent(a);
                    C(!g.CanAddToGroup(b), "P1/③ 组已满（容量 1）⇒ 第二个事件 `CanAddToGroup` 假");
                    C(g.CanAddToGroup(a), "P1/③ 组已满但**自己已在组里** ⇒ `CanAddToGroup` 仍真（`Contains(id)` 那一支）");
                    C(g.eventsInGroup.Count == 1 && g.eventsInGroup[0] == "C_first",
                      "P1/③ `eventsInGroup` 装的是**事件 id 字符串**（应 [\"C_first\"]，实 [" + string.Join(",", g.eventsInGroup.ToArray()) + "]）");
                    g.RemoveEvent(a);
                    C(g.eventsInGroup.Count == 0, "P1/③ `RemoveEvent` 后组空（应 0，实 " + g.eventsInGroup.Count + "）");
                    // 原版「默认 0 容量」的坑：照抄
                    var g0 = new EventGroup { id = "g_default0" };
                    C(!g0.CanAddToGroup(a), "P1/③ 照抄原版坑：`MaxActiveEvents` 默认 0 ⇒ 连第一个事件都装不进（`0<0` 假 ⇒ 走 Contains）");
                    // 负容量 = 不限
                    var gn = new EventGroup { id = "g_unlimited", MaxActiveEvents = -1 };
                    for (int i = 0; i < 5; i++) gn.AddEvent(new FixtureEvent("D" + i));
                    C(gn.CanAddToGroup(new FixtureEvent("D99")), "P1/③ `MaxActiveEvents=-1` ⇒ 不限容量");
                }

                // ── ④ RegisterTriggers → CleanupTriggers ⇒ 总线登记数回 0（灭自证：只加不摘 ⇒ 红）──────
                {
                    LiveOpsSignal.ResetForTest();
                    LiveOpsSignals.ResetForTest();
                    LiveOpsSignal.ReinstallLegacyBridge();
                    EventGroupConfig.Install(new EventGroupConfig { groups = new List<EventGroup> { new EventGroup { id = "g4", MaxActiveEvents = -1 } } });

                    var ev = new FixtureEvent("E_trigger");
                    ev.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    ev.D.filter.eventGroupIds = new string[] { "g4" };
                    var trig = new CountingTrigger();
                    var sigTrig = new CountingSignalTrigger { Want = "E_trigger" };
                    ev.D.filter.Triggers = new List<LiveOpsEventTrigger> { trig, sigTrig };

                    int before = LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>();
                    ev.RegisterEventTriggers();
                    C(trig.RegisterCalls == 1, "P1/④ `RegisterEventTriggers()` 逐条 `trigger.Register`（应 1 次，实 " + trig.RegisterCalls + "）");
                    C(trig.IsRegistered, "P1/④ 自定义 trigger 拿到了回调");
                    C(LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() == before + 1,
                      "P1/④ `SignalEventTrigger` 把 `TryTrigger` 挂上了信号总线（应 +1，实 " + (LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() - before) + "）");

                    // 信号 → 校验 → 回调 → TryActivateEvent → ToggleEventActivation
                    C(!ev.S.IsActive, "P1/④ 前置：还没激活");
                    LiveOpsSignal.Raise(new EventExpiredSignal("not_me"));                 // 校验不过
                    C(sigTrig.Fired == 0 && !ev.S.IsActive,
                      "P1/④ **灭自证**：`ValidateSignal` 不过 ⇒ 不回调、不激活（`Fired` 应 0，实 " + sigTrig.Fired + "）");
                    LiveOpsSignal.Raise(new EventExpiredSignal("E_trigger"));              // 校验通过
                    C(sigTrig.Fired == 1, "P1/④ `ValidateSignal` 过 ⇒ 回调 1 次（实 " + sigTrig.Fired + "）");
                    C(ev.S.IsActive, "P1/④ `TryActivateEvent` 条件满足 ⇒ `ToggleEventActivation(true,…)` 把 `IsActive` 置真");
                    C(ev.S.triggerTime > 0L, "P1/④ 激活同时写了 `triggerTime`（原版 `triggerTime<1 || saveOnServer` 那一支）");
                    var g4 = EventGroupConfig.Find("g4");
                    C(g4.eventsInGroup.Contains("E_trigger"),
                      "P1/④ 激活 ⇒ `EventFilter.AddOrRemoveFromGroups(active=true)` 把自己加进组（组内 = [" + string.Join(",", g4.eventsInGroup.ToArray()) + "]）");
                    // 自定义（非信号）trigger 的回调 = **同一个** `EventFilter.TryActivateEvent(this)` 闭包
                    trig.Fire();
                    C(trig.CallbackCalls == 1 && g4.eventsInGroup.Count == 1,
                      "P1/④ 逐条 trigger 拿到的是**同一个** `TryActivateEvent` 闭包（手工触发 1 次；"
                      + "`AddEvent` 去重 ⇒ 组里仍 1 条，实 " + g4.eventsInGroup.Count + "）");

                    int afterRegister = LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>();
                    ev.Dispose();
                    C(trig.UnregisterCalls == 1, "P1/④② `Dispose()` ⇒ `CleanupTriggers` 逐条 `Unregister`（应 1 次，实 " + trig.UnregisterCalls + "）");
                    C(LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() == afterRegister - 1,
                      "P1/④ **灭自证**：`RegisterTriggers`→`CleanupTriggers` ⇒ 总线登记数回到注册前（" + (afterRegister - 1) + "，实 " + LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() + "）");
                    C(!g4.eventsInGroup.Contains("E_trigger"),
                      "P1/④② `CleanupTriggers` 还把自己从**每个组**里摘掉（组内 = [" + string.Join(",", g4.eventsInGroup.ToArray()) + "]）");
                }

                // ── ⑤ 过滤谓词正负例（`Validate` = 组门 && 条件全过）────────────────────
                {
                    var capGroup = new EventGroup { id = "g5", MaxActiveEvents = 1 };
                    EventGroupConfig.Install(new EventGroupConfig { groups = new List<EventGroup> { capGroup } });
                    var ev = new FixtureEvent("F_filter");
                    ev.D.filter.eventGroupIds = new string[] { "g5" };
                    C(ev.CheckEventFilter(), "P1/⑤ `filter.Validate` 正例：组未满 ⇒ `CheckEventFilter()` 真");
                    capGroup.AddEvent(new FixtureEvent("F_other"));
                    C(!ev.CheckEventFilter(), "P1/⑤ `filter.Validate` 负例：组满且自己不在组里 ⇒ `CheckEventFilter()` 假");
                    capGroup.RemoveEvent(new FixtureEvent("F_other"));

                    // filter == null ⇒ 原版直接 return true
                    var ev2 = new FixtureEvent("F_nofilter");
                    ev2.D.filter = null;
                    C(ev2.CheckEventFilter(), "P1/⑤ `data.filter == null` ⇒ `CheckEventFilter()` 真（原版这一支就是 `return 1`）");
                    ev2.D.filter = new LiveOpsEventFilter();

                    // 条件不过 ⇒ 假
                    var ev3 = new FixtureEvent("F_cond");
                    ev3.D.filter.Conditions = new List<EventCondition> { new FalseCondition() };
                    C(!ev3.CheckEventFilter(), "P1/⑤ `Conditions` 里有一条不过 ⇒ `CheckEventFilter()` 假（`Validate` 的短路）");
                }

                // ── ⑥ `EventLocation.Match` 正负例 ────────────────────────────────────
                {
                    var loc = new EventLocation { type = EventLocationType.Parent, parentId = "P1" };
                    C(loc.Match(EventLocationType.Parent, "P1"), "P1/⑥ `EventLocation.Match` 正例：同 type 同 id ⇒ 真");
                    C(!loc.Match(EventLocationType.Parent, "P2"), "P1/⑥ 负例：同 type 不同 id ⇒ 假");
                    C(!loc.Match(EventLocationType.Main, "P1"), "P1/⑥ 负例：不同 type ⇒ 假");
                    var locNull = new EventLocation { type = EventLocationType.Main, parentId = null };
                    C(locNull.Match(EventLocationType.Main, null) && locNull.Match(EventLocationType.Main, ""),
                      "P1/⑥ `null → \"\"` 折叠照抄：`parentId=null` 时传 `null` 或 `\"\"` 都算匹配");
                }

                // ── ⑦ 状态机：`RefreshExpire`（Finished/Expired ⇒ Expire）·`MarkAsOld` 幂等 ──────────
                {
                    var ev = new FixtureEvent("G_expire_finished");
                    // 🔴 要造 Finished(3) 而不是 Expired(4)：`GetState` 先看 `FinalDate`（= expirable ? expireDate : endDate）
                    // ⇒ 想走「endDate 已过但 expireDate 未到」那一支，必须 `expirable = true`。
                    ev.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-10)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),      // 已过
                        expirable = true,
                        expireDate = LiveOpsEventTimeData.ToMs(now.AddHours(3)),    // 未过 ⇒ FinalDate 未过
                    };
                    C(ev.EventState == LiveOpState.Finished, "P1/⑦ 夹具自检：endDate 已过 / expireDate 未过 ⇒ `EventState == Finished(3)`（实 " + ev.EventState + "）");
                    int cb = 0;
                    ev.S.IsActive = true;
                    ev.RefreshExpire(delegate { cb++; });
                    C(cb == 1 && !ev.S.IsActive, "P1/⑦ `RefreshExpire`：state=Finished ⇒ 立刻 `Expire(callback)`（回调 1 次、`IsActive` 置假）");

                    var evExp = new FixtureEvent("G_expire_expired");
                    evExp.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-10)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(-2)),
                        expirable = true,
                        expireDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),   // FinalDate 也过了 ⇒ Expired(4)
                    };
                    C(evExp.EventState == LiveOpState.Expired, "P1/⑦ 夹具自检：FinalDate 已过 ⇒ `EventState == Expired(4)`（实 " + evExp.EventState + "）");
                    int cbExp = 0;
                    evExp.RefreshExpire(delegate { cbExp++; });
                    C(cbExp == 1, "P1/⑦ `RefreshExpire` 另一支：state=Expired 也走 `Expire(callback)`（原版是 `Finished || Expired`）");

                    var ev2 = new FixtureEvent("G_expire_active");
                    ev2.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    int cb2 = 0;
                    ev2.RefreshExpire(delegate { cb2++; });
                    C(cb2 == 0, "P1/⑦ `RefreshExpire` 负例：state=Active ⇒ **不**回调（实 " + cb2 + "）");

                    var ev3 = new FixtureEvent("G_markold");
                    C(ev3.S.IsNew && !ev3.S.WasSeen, "P1/⑦ 新存档初值：`IsNew=true` / `WasSeen=false`");
                    ev3.MarkAsOld();
                    C(!ev3.S.IsNew && ev3.S.WasSeen, "P1/⑦ `MarkAsOld` ⇒ `IsNew=false` + `WasSeen=true`");
                    // 灭自证：判据是 `IsNew`（不是 `WasSeen`）⇒ 第二次必须**直接返回**
                    ev3.S.WasSeen = false;
                    ev3.MarkAsOld();
                    C(!ev3.S.WasSeen, "P1/⑦ `MarkAsOld` 早退：`IsNew` 已是 false ⇒ 直接返回、**不再写** `WasSeen`（实 WasSeen=" + ev3.S.WasSeen + "）");
                }

                // ── ⑧ `OpenWindow` 按 state 分派 10 / 20 / 21 ─────────────────────────
                {
                    var seen = new List<int>();
                    LiveOpsPopupPresenter.Open = delegate (ILiveOps e, PopupOptions o) { seen.Add(o == null ? -1 : o.PopupLabel); return null; };

                    var ev = new FixtureEvent("H_active");
                    ev.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = true,
                        expireDate = LiveOpsEventTimeData.ToMs(now.AddHours(3)),   // 留着：下面要把 endDate 拨到过去造 Finished(3)
                    };
                    ev.D.drawData = new EventElements
                    {
                        TriggerPopupOptions = new PopupOptions { PopupLabel = 20 },
                        ExpirePopupOptions = new PopupOptions { PopupLabel = 21 },
                    };
                    ev.OpenWindow();
                    C(seen.Count == 1 && seen[0] == 10, "P1/⑧ `OpenWindow`：state=Active ⇒ 主窗 label=10（实 " + (seen.Count > 0 ? seen[seen.Count - 1] : -99) + "）");

                    ev.D.dates.endDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1));       // endDate 过、FinalDate 未过 ⇒ Finished(3)
                    C(ev.EventState == LiveOpState.Finished, "P1/⑧ 夹具自检：拨 endDate 到过去后 ⇒ `EventState == Finished`（实 " + ev.EventState + "）");
                    ev.OpenWindow();
                    C(seen.Count == 2 && seen[1] == 21, "P1/⑧ `OpenWindow`：state=Finished ⇒ 过期弹窗 label=21（实 " + (seen.Count > 1 ? seen[1] : -99) + "）");

                    // `Expired(4)` 落到 **10**（不是 21）—— 原版 switch 的 default 支，⛔ 别「顺手补成 21」
                    ev.D.dates.expireDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1));
                    C(ev.EventState == LiveOpState.Expired, "P1/⑧ 夹具自检：expireDate 也拨过去 ⇒ `EventState == Expired`（实 " + ev.EventState + "）");
                    ev.OpenWindow();
                    C(seen.Count == 3 && seen[2] == 10, "P1/⑧ `OpenWindow`：state=Expired ⇒ **仍然 10**（原版 default 支；⛔ 不是 21）");

                    var ev2 = new FixtureEvent("H_preview");
                    ev2.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(2)),        // 还没开始
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(9)),
                        previewable = true,
                        previewDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),     // 预览期已开
                        expirable = false,
                    };
                    ev2.D.drawData = new EventElements { TriggerPopupOptions = new PopupOptions { PopupLabel = 20 } };
                    ev2.OpenWindow();
                    C(seen.Count == 4 && seen[3] == 20, "P1/⑧ `OpenWindow`：state=OnPreview ⇒ 触发弹窗 label=20（实 " + (seen.Count > 3 ? seen[3] : -99) + "）");
                    LiveOpsPopupPresenter.ResetForTest();
                }

                // ── ⑨ `ParentEventHandler.BeforeRefresh` 的两档 ────────────────────────
                {
                    LiveOpsParentEventRefresh.ResetForTest();
                    int mainClicks = 0, refreshed = 0;
                    LiveOpsParentEventRefresh.IsEventTabSelected = delegate { return true; };
                    LiveOpsParentEventRefresh.SelectMainTab = delegate { mainClicks++; };
                    LiveOpsParentEventRefresh.OnEventsRefreshed = delegate { refreshed++; };

                    var active = new FixtureEvent("I_active");
                    active.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    bool acted = LiveOpsParentEventRefresh.Run(new ILiveOps[] { active });
                    C(!acted && mainClicks == 0 && refreshed == 0,
                      "P1/⑨ `BeforeRefresh` 负例：还有 Active 事件 ⇒ 什么都不做（`All(e => state != Active)` 不成立）");

                    var finished = new FixtureEvent("I_finished");
                    finished.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-10)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        expirable = false,
                    };
                    acted = LiveOpsParentEventRefresh.Run(new ILiveOps[] { finished });
                    C(acted && mainClicks == 1 && refreshed == 1,
                      "P1/⑨ `BeforeRefresh` 正例：全都不在期 + 活动页签开着 ⇒ 导航退回主菜单 1 次 + `OnEventsRefreshed` 1 次"
                      + "（实 acted=" + acted + " main=" + mainClicks + " refresh=" + refreshed + "）");

                    mainClicks = 0; refreshed = 0;
                    LiveOpsParentEventRefresh.IsEventTabSelected = delegate { return false; };
                    LiveOpsParentEventRefresh.Run(new ILiveOps[] { finished });
                    C(mainClicks == 0 && refreshed == 1,
                      "P1/⑨ 活动页签**没开** ⇒ 不动导航，但 `OnEventsRefreshed` 照样调（原版 `OnEventsRefreshed` 在 if 外面）");
                    LiveOpsParentEventRefresh.ResetForTest();
                }

                // ── ⑩ 信号总线本身：typed / untyped / 退订 / 清理 ─────────────────────
                {
                    LiveOpsSignal.ResetForTest();
                    int typed = 0, untyped = 0;
                    Action<EventExpiredSignal> onTyped = delegate (EventExpiredSignal s) { typed++; };
                    Action onUntyped = delegate { untyped++; };
                    LiveOpsSignal.Register<EventExpiredSignal>(onTyped);
                    LiveOpsSignal.Register<EventExpiredSignal>(onUntyped);
                    C(LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() == 2,
                      "P1/⑩ 总线登记：typed 1 + untyped 1 = 2（实 " + LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() + "）");
                    LiveOpsSignal.Raise(new EventExpiredSignal("x"));
                    C(typed == 1 && untyped == 1, "P1/⑩ `Raise` 两种监听器各被调 1 次（typed=" + typed + " untyped=" + untyped + "）");
                    LiveOpsSignal.Unregister<EventExpiredSignal>(onUntyped);
                    C(LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() == 1, "P1/⑩ 退订 untyped 后剩 1");
                    LiveOpsSignal.Raise(new EventExpiredSignal("y"));
                    C(typed == 2 && untyped == 1, "P1/⑩ 退订生效：typed 2 / untyped 仍 1");
                    LiveOpsSignal.Unregister<EventExpiredSignal>(onTyped);
                    C(LiveOpsSignal.Bus.ListenerCount<EventExpiredSignal>() == 0, "P1/⑩ 全部退订后 = 0");
                    // 旧事件桥（`LiveOpsSignals.EventExpired` → 总线）
                    int bridged = 0;
                    Action<EventExpiredSignal> onBridge = delegate (EventExpiredSignal s) { bridged++; };
                    LiveOpsSignal.Register<EventExpiredSignal>(onBridge);
                    LiveOpsSignals.RaiseEventExpired("bridge_id");
                    C(bridged == 1, "P1/⑩ 桥：`LiveOpsSignals.RaiseEventExpired` ⇒ 总线上也收到 1 次（实 " + bridged + "）");
                    LiveOpsSignal.Unregister<EventExpiredSignal>(onBridge);
                }

                // ── ⑪ `LiveOpsManager` 的三条新口径（2026-10-28 B93_E12 · 账 410 / 415 / 407 / 420 / 162）────────
                //   判据全在 `Shell/LiveOpsManager.cs` 类尾那一段；这里只锁**行为**，不问实现。
                {
                    LiveOpsManager.ResetForTest();
                    // 🔴 `ResetForTest()` 会把 `LiveOpsClock` 也复位 ⇒ **必须重设 NowProvider**，否则下面算出来的状态
                    //    是按「真实现在」而不是夹具时间（本块要判 Active/Planned 两档，差一天结果就翻）。
                    LiveOpsClock.NowProvider = delegate { return now; };
                    var fh = new FixtureHandler();
                    var a = new FixtureEvent("L_parent");                  // location=Parent，目录里没有 ⇒ parentId = null
                    a.D.location = (int)EventLocationType.Parent;
                    a.D.showInMainMenu = true;  a.D.priority = 5;
                    a.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    var b = new FixtureEvent("L_main");                    // location=Main
                    b.D.location = (int)EventLocationType.Main;
                    b.D.priority = 9;                                      // 不在主菜单
                    // 🔴 补课（B93_FX6 · B93_E30 §四 补丁 B）：`FixtureEvent` 的 ctor 默认 `activationType = OnDate`（`:1192`），
                    //   而 `GetState` 对「OnDate 但没有 dates」走 NotAvailable(-1) 那一支（`Shell/LiveOpsEvent.cs:246-251`）
                    //   ⇒ b 会被 `GetEventsForDisplayingInMenu` 的 `state ∈ [OnPreview, Finished]` 闸筛掉（`Shell/LiveOpsManager.cs:566`），
                    //   下面两条「灭自证」就永远拿不到 b（实得 1 条）—— 那是**夹具漏填**，不是实现错。
                    //   给一段覆盖 `now` 的 dates 即可（照 a 那份写）。⛔ 别去改实现绕开状态闸。
                    b.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(-1)),
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),
                        expirable = false,
                    };
                    var c2 = new FixtureEvent("L_planned");                // 主菜单 + Planned(0) ⇒ 不该进菜单列表
                    c2.D.location = (int)EventLocationType.Parent;
                    c2.D.showInMainMenu = true; c2.D.priority = 99;
                    c2.D.dates = new LiveOpsEventTimeData
                    {
                        startDate = LiveOpsEventTimeData.ToMs(now.AddHours(1)),   // 还没开始 ⇒ Planned
                        endDate = LiveOpsEventTimeData.ToMs(now.AddHours(3)),
                        expirable = false,
                    };
                    fh.SetupEvents(new List<ILiveOps> { a, b, c2 }, new List<ILiveOps>(), null).GetAwaiter().GetResult();
                    LiveOpsManager.Register(fh);
                    // 前提硬检查（不是 `if (x != null) Check(...)`：注册不上就不该往下判）
                    var all = new List<ILiveOps>(LiveOpsManager.GetAllEvents());
                    C(all.Count == 3, "P2/⑪ 前提：夹具 handler 注册后 `GetAllEvents()` 有 3 条（实 " + all.Count + "）");

                    // 账 410：`GetAllEvents(EventLocationType, string parent)`
                    int parentOnly = 0, mainOnly = 0, withParentId = 0, nullParent = 0;
                    foreach (var e in LiveOpsManager.GetAllEvents(EventLocationType.Parent)) parentOnly++;
                    foreach (var e in LiveOpsManager.GetAllEvents(EventLocationType.Main)) mainOnly++;
                    foreach (var e in LiveOpsManager.GetAllEvents(EventLocationType.Parent, "P1")) withParentId++;
                    foreach (var e in LiveOpsManager.GetAllEvents(EventLocationType.Parent, null)) nullParent++;
                    C(parentOnly == 2, "P2/⑪ 账410 `location=Parent` ⇒ 2 条（实 " + parentOnly + "）");
                    C(mainOnly == 1, "P2/⑪ 账410 `location=Main` ⇒ 1 条（实 " + mainOnly + "）");
                    C(withParentId == 0, "P2/⑪ 账410 负例：`parent=\"P1\"` 但本地目录查不到该 id ⇒ 0 条（实 " + withParentId + "）");
                    C(nullParent == 2, "P2/⑪ 账410 `parent=null` ⇒ `null→\"\"` 折叠后仍匹配 parentId=null 的那 2 条（实 " + nullParent + "）");

                    // 账 415：`GetEventsForDisplayingInMenu()`（showInMainMenu ∧ state∈{1,2,3}，按 Priority **降序**且稳定）
                    var menu = LiveOpsManager.GetEventsForDisplayingInMenu();
                    C(menu.Count == 1, "P2/⑪ 账415 只有 `L_parent` 同时满足「主菜单 + Active」（`L_main` 不在主菜单、`L_planned` 是 Planned）⇒ 1 条（实 " + menu.Count + "）");
                    C(menu.Count == 1 && menu[0].Priority == 5, "P2/⑪ 账415 返回值带出 `Priority`（应 5，实 " + (menu.Count > 0 ? menu[0].Priority : -1) + "）");
                    // 灭自证：把 `L_main` 也放进主菜单并给它**更高**优先级 ⇒ 它必须排到**前面**（证明是降序，不是升序也不是随机）
                    b.D.showInMainMenu = true;
                    var menu2 = LiveOpsManager.GetEventsForDisplayingInMenu();
                    C(menu2.Count == 2 && menu2[0].Priority == 9 && menu2[1].Priority == 5,
                      "P2/⑪ 账415 灭自证：Priority 9 排在 5 **前面**（降序；实 [" + (menu2.Count > 0 ? menu2[0].Priority.ToString() : "?")
                      + "," + (menu2.Count > 1 ? menu2[1].Priority.ToString() : "?") + "]）");
                    b.D.priority = 1;                                         // 再换一次 ⇒ 顺序必须跟着翻
                    var menu3 = LiveOpsManager.GetEventsForDisplayingInMenu();
                    C(menu3.Count == 2 && menu3[0].Priority == 5 && menu3[1].Priority == 1,
                      "P2/⑪ 账415 灭自证②：Priority 换成 1 后顺序翻过来（实 [" + (menu3.Count > 0 ? menu3[0].Priority.ToString() : "?")
                      + "," + (menu3.Count > 1 ? menu3[1].Priority.ToString() : "?") + "]）");
                    b.D.showInMainMenu = false;

                    // 账 407：`IsTriggeredEvent`（= `data.filter.Triggers.Count > 0`，BuildHandler 分派的第二道闸）
                    C(!LiveOpsManager.IsTriggeredEvent(a), "P2/⑪ 账407 `IsTriggeredEvent`：没有触发器 ⇒ 假");
                    a.D.filter.Triggers = new List<LiveOpsEventTrigger> { new CountingTrigger() };
                    C(LiveOpsManager.IsTriggeredEvent(a), "P2/⑪ 账407 反例：`Triggers` 有 1 条 ⇒ 真（`BuildHandler` 走「让它自己激活」那一支）");
                    a.D.filter.Triggers.Clear();

                    // 账 162：`EventDataAssetProvider.GetAssets()` = `GetAllEvents().Select(e => e.GetBaseData())`
                    var assets = EventDataAssetProvider.GetAssets();
                    C(assets.Count == 3, "P2/⑪ 账162 `GetAssets()` 条数 = 事件条数（3，实 " + assets.Count + "）");
                    // 🔴 次序**不是夹具输入序**：`SetupEvents(List,…)` 先 `ops.OrderByDescending(PriorityOf)`
                    //   （判据 `Shell/LiveOpsHandler.cs:275`）⇒ 本块的**真序** = `[c2(99), b(9), a(5)]`
                    //   （排序在 `SetupEvents` 那一刻定死；下面把 b 的 priority 改成 1 也不会重排）。
                    //   ⇒ 原来断 `assets[0] == a.D` 是把「输入序」当成了「输出序」（B93_E30 §四 补丁 A）。
                    //   改成「与 `GetAllEvents().Select(GetBaseData())` **逐项同序同引用**」：既锁住真次序，也锁住「返回的是 data 本身、不是新对象」。
                    var evOrder = new List<ILiveOps>(LiveOpsManager.GetAllEvents());
                    bool sameSeqRef = assets.Count == evOrder.Count;
                    for (int k = 0; sameSeqRef && k < assets.Count; k++)
                        sameSeqRef = ReferenceEquals(assets[k], evOrder[k].GetBaseData());
                    C(sameSeqRef, "P2/⑪ 账162 `GetAssets()` 与 `GetAllEvents().Select(GetBaseData())` **逐项同序同引用**（实 "
                                  + assets.Count + "/" + evOrder.Count + "）");

                    // 账 420：`ResetHandlers()` 调的是 `ClearEvents()`（清列表），**不是** `ResetEvents()`（不清列表）
                    int beforeReset = new List<ILiveOps>(fh.GetBaseEvents(true)).Count;
                    C(beforeReset == 3, "P2/⑪ 账420 前提：`ResetHandlers()` 之前 handler 里有 3 条（实 " + beforeReset + "）");
                    LiveOpsManager.ResetHandlers();
                    int afterReset = new List<ILiveOps>(fh.GetBaseEvents(true)).Count;
                    C(afterReset == 0,
                      "P2/⑪ 账420 灭自证：`ResetHandlers()` 之后事件表**被清空**（= `ClearEvents` 槽 7；若误用 `ResetEvents`，这里会是 3，实 "
                      + afterReset + "）");
                    C(LiveOpsManager.HandlerCountForTest == 0, "P2/⑪ 账420 `ResetHandlers()` 尾句 `HandlerLookup.Clear()`（应 0，实 " + LiveOpsManager.HandlerCountForTest + "）");

                    // 账 698 / 699：`SharedEventDataManager` 键 = `GetType()`；`Unpack` 交给 context 的是**整份** `SharedEventData`
                    LiveOpsSharedDataManager.ResetForTest();
                    var ctx = new FixtureSharedContext();
                    LiveOpsSharedDataManager.Initialize(new List<LiveOpsSharedContext> { ctx });
                    C(LiveOpsSharedDataManager.ContextCountForTest == 1, "P2/⑪ 账698 前提：登记 1 个 context（实 " + LiveOpsSharedDataManager.ContextCountForTest + "）");
                    C(LiveOpsSharedDataManager.GetContext<FixtureSharedContext>() == ctx,
                      "P2/⑪ 账698 键 = `GetType()`：`GetContext<FixtureSharedContext>()` 取得到（原版 `ToDictionary(x => x.GetType())`）");
                    var persistence = new SharedEventData { energyData = LiveOpsJson.NewObject() };
                    LiveOpsSharedDataManager.Unpack(persistence);
                    C(ctx.LastUnpacked != null && ctx.LastUnpacked == persistence,
                      "P2/⑪ 账699 灭自证：context 收到的是**整份 `SharedEventData`**（同一个引用）；若误传 `energyData` 则这里拿到的会是另一个对象");
                    C(persistence.energyData != null && !ReferenceEquals(ctx.LastUnpacked, persistence.energyData),
                      "P2/⑪ 账699 灭自证②：交出去的**不是** `energyData` 那一段（两者都不是同一个对象）");
                    LiveOpsSharedDataManager.ResetForTest();
                    LiveOpsManager.ResetForTest();                        // 收尾：注册表回到「空」（本块进来时就是这个状态）
                }

                // ── ⑫ 夹具：`LiveOpsManager` 那两条新口径要用的最小实现 ─────────────────────
                //   ② `FixtureHandler` = 只为 `GetAllEvents()`/`ResetHandlers()` 提供一张可清空的事件表。
                //   ③ `FixtureSharedContext` = 只为记「`Unpack` 交给我的是哪一份对象」。
            }
            catch (Exception ex)
            {
                // 🔴 自检自己抛异常**不能把宿主带崩**（`ShellScene.Run` 后面还有几千条断言）⇒ 记一条失败，继续收尾。
                n++;
                check(false, "LiveOps 自检抛异常（后续断言未执行）：" + ex.GetType().Name + " · " + ex.Message);
            }
            finally
            {
                LiveOpsClock.ResetForTest();
                LiveOpsSignals.Verbose = oldVerbose;
                LiveOpsSignal.ResetForTest();
                LiveOpsSignals.ResetForTest();
                EventGroupConfig.ResetForTest();
                LiveOpsEventFilter.ResetWarnedForTest();
                LiveOpsParentEventRefresh.ResetForTest();
                LiveOpsPopupPresenter.ResetForTest();
                LiveOpsScheduler.ResetForTest();
            }
            return n;
        }

        /// <summary>夹具条件：恒不过。</summary>
        sealed class FalseCondition : EventCondition
        {
            public override bool Validate(ILiveOps liveOps) { return false; }
        }
    }

    // ============================================================ B93·N2 自检（2026-10-28 · **追加在文件尾**，⛔ 别插中间 —— 本文件有账按 `文件:行号` 钉着）
    /// <summary>`B93·N2` 那 14 行的断言（快模式 6 · `LiveOpsAssetUtility` 2 · 时间文案 2 · handler 2 · 信号触发器 2）。
    /// <para>宿主调用（粘到 `Editor/ShellScene.cs` 的「合计行之前」，见本类 `Run` 的说明）：
    /// <c>Debug.Log(P + "LiveOpsB93N2SelfCheck 跑 " + LiveOpsB93N2SelfCheck.Run((ok, msg) =&gt; CheckTrue(ok, msg)) + " 条断言");</c></para>
    /// <para>🔴 **每条都配灭自证**：把对应实现改回旧写法时，**必须至少有一条变红**（每条上方都写了「回退 ⇒ 哪条红」）。
    /// 🔴 **前提断言先于被测断言**（本地目录非空 / 引用表非空），⛔ 不用 `if (前提) Check(结论)` 那种会静默跳过的写法。</para></summary>
    public static class LiveOpsB93N2SelfCheck
    {
        // ── 夹具 ────────────────────────────────────────────────────────────────
        /// <summary>`FastModeBaseEvent` 的最小具体子类（`EventPlayMode` / `GetMatchmakingFilter` 已由基类实现 ⇒ 只需装数据）。</summary>
        sealed class FixtureFastMode : FastModeBaseEvent<FastModeSave, FastModeData>
        {
            public FixtureFastMode(FastModeData d, FastModeSave s) { LoadEventData(d, s, null); }
        }

        /// <summary>普通 `LiveOpsEvent` 的最小具体子类（时间文案两跳用）。</summary>
        sealed class FixtureEvent : LiveOpsEvent<LiveOpsSave, LiveOpsEventData>
        {
            public FixtureEvent(LiveOpsEventData d) { LoadEventData(d, new LiveOpsSave { eventId = d.eventId }, null); }
        }

        /// <summary>handler 夹具：`GetEvents` 被覆盖成一个**哨兵表** —— 用它证明 `GetBaseEvents` 走的是虚分派。</summary>
        sealed class FixtureHandler : LiveOpsHandler
        {
            public readonly List<ILiveOps> Stamp = new List<ILiveOps>();
            public int OverrideCalls;
            public override string HandlerKey { get { return "B93N2"; } }
            public override IEnumerable<ILiveOps> GetEvents(bool includeBlockedEvents = false)
            { OverrideCalls++; return Stamp; }
        }

        /// <summary>信号触发器夹具：只认 `EventId == Want` 的那条信号。</summary>
        sealed class FixtureSignalTrigger : SignalEventTrigger<EventExpiredSignal>
        {
            public string Want;
            public int Fired;
            protected override bool ValidateSignal(EventExpiredSignal signal)
            {
                if (signal == null || signal.EventId != Want) return false;
                Fired++;
                return true;
            }
        }

        // ── 数据夹具 ────────────────────────────────────────────────────────────
        static FastModeData MkData(string id, int[] eloDiffs, params int[] milestones)
        {
            var d = new FastModeData { eventId = id, activationType = LiveOpsActivationType.Permanent };
            d.ratingDifferenceForMatchMaking = eloDiffs;
            for (int i = 0; i < milestones.Length; i++)
                d.rewardsPerTier.Add(new MilestoneReward { milestone = milestones[i], rewardIds = new[] { id + ":" + i } });
            return d;
        }
        static FastModeSave MkSave(string id) { return new FastModeSave { eventId = id, IsActive = true }; }

        /// <summary>源码扫描口（用来给「接线了没有」下硬断言）。路径 = `Assets/CardPresentation/Shell/&lt;file&gt;`。</summary>
        static string ReadShellSource(string file)
        {
            try
            {
                string p = System.IO.Path.Combine(UnityEngine.Application.dataPath, "CardPresentation", "Shell", file);
                return System.IO.File.Exists(p) ? System.IO.File.ReadAllText(p) : null;
            }
            catch (Exception e) { Debug.LogError("[LiveOps] B93N2SelfCheck 读源码失败：" + e.Message); return null; }
        }

        /// <summary>跑全部断言。`check(条件, 断言文案)` 由宿主提供。返回执行的断言条数（打印它 = 自证真被跑到）。</summary>
        public static int Run(Action<bool, string> check)
        {
            if (check == null) { Debug.LogError("[LiveOps] LiveOpsB93N2SelfCheck.Run(null) ⇒ 不跑"); return 0; }
            int n = 0;
            Action<bool, string> C = delegate (bool ok, string msg) { n++; check(ok, msg); };

            DateTime now = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
            var oldNow = LiveOpsClock.NowProvider;
            LiveOpsClock.NowProvider = delegate { return now; };
            long t = LiveOpsEventTimeData.ToMs(now);
            var oldVerbose = LiveOpsSignals.Verbose;
            LiveOpsSignals.Verbose = false;
            try
            {
                // ══════════ ① `FastModeBaseEvent.Expire`：TryCollect 的回调 = `base.Expire(onComplete)` ══════════
                {
                    var d = MkData("fm_exp", new[] { 10, 20, 30 }, 50);
                    var s = MkSave("fm_exp");
                    s.UpdateLocalJoinState(PlayEventSaveGame.JoinStates.Joined);   // 不进早退支
                    s.localMilestoneScore = 0;                                     // 一档都够不到 ⇒ TryCollect 只回调
                    var ev = new FixtureFastMode(d, s);
                    bool completed = false;
                    ev.Expire(delegate { completed = true; });
                    C(completed, "①a Expire：`onComplete` 跑到（⛔ 旧实现也绿 ⇒ 它**不是**这一条的灭自证）");
                    // 🔴 灭自证：回退成 `TryCollect(onComplete)` ⇒ base.Expire 那半不跑 ⇒ ①b/①c 红
                    C(!s.IsActive, "①b Expire：`base.Expire` 那半**真跑了**（`IsActive` 被置 false）—— 回退成 `TryCollect(onComplete)` ⇒ 红");
                    C(s.expiredTime > 0, "①c Expire：`base.Expire` 写了 `expiredTime` —— 回退 ⇒ 红");

                    var d2 = MkData("fm_exp2", new[] { 10 }, 50);
                    var s2 = MkSave("fm_exp2");                                   // JoinState 默认 NotJoined ∧ score 0 ⇒ 早退
                    var ev2 = new FixtureFastMode(d2, s2);
                    int collected0 = s2.collectedRewards;
                    bool completed2 = false;
                    ev2.Expire(delegate { completed2 = true; });
                    C(completed2 && !s2.IsActive, "①d Expire 早退支（NotJoined ∧ CurrentScore==0）：直接 `base.Expire(onComplete)`");
                    C(s2.collectedRewards == collected0, "①e Expire 早退支：**不进** TryCollect（`collectedRewards` 一位没动）");
                }

                // ══════════ ② `SetCustomMatchOptions` ══════════
                {
                    var ev = new FixtureFastMode(MkData("fm_opt", new[] { 10, 20, 30 }), MkSave("fm_opt"));
                    ev.LocalDefeatStreak = 7;
                    var ro = new RoomOptions();
                    ev.SetCustomMatchOptions(ro, 1500);
                    C(ro.Get("C4") is int && (int)ro.Get("C4") == 1, "②a SetCustomMatchOptions：`C4` = **常量 1**");
                    C(ro.Get("C5") is int && (int)ro.Get("C5") == 7, "②b SetCustomMatchOptions：`C5` = 连败数（灭自证：与 ②a 交叉 —— 两格写反 ⇒ 至少一条红）");
                    var arr = ro.CustomRoomPropertiesForLobby;
                    C(arr != null && arr.Length == 5 && arr[0] == "C1" && arr[1] == "C2" && arr[2] == "C3" && arr[3] == "C4" && arr[4] == "C5",
                      "②c SetCustomMatchOptions：大厅键表逐字 = [C1,C2,C3,C4,C5]（原版字面量 0x429E1F8/0x429E4E0/0x429E5D8/0x429E6D0/0x429E7C8）");
                }

                // ══════════ ③ `GetMatchmakingFilter` ══════════
                {
                    MatchmakingInputs.ResetForTest();                              // 不注入 ⇒ C2/C3 两段不拼
                    var ev = new FixtureFastMode(MkData("fm_mm", new[] { 10, 20, 30 }), MkSave("fm_mm"));
                    string q0 = ev.GetMatchmakingFilter(1000, 0, 3);
                    string q1 = ev.GetMatchmakingFilter(1000, 1, 3);
                    string q2 = ev.GetMatchmakingFilter(1000, 2, 3);
                    C(q0.Contains("C1 < 1010") && q0.Contains("C1 >= 990"), "③a GetMatchmakingFilter：tryCount=0 ⇒ 档 0（δ=10）⇒ [990,1010]");
                    C(q1.Contains("C1 < 1020") && q1.Contains("C1 >= 980"), "③b GetMatchmakingFilter：tryCount=1 ⇒ 档 1（δ=20）⇒ [980,1020]");
                    C(q2.Contains("C1 < 1030") && q2.Contains("C1 >= 970"), "③c GetMatchmakingFilter：tryCount=2 ⇒ 档 2（δ=30）⇒ [970,1030]（灭自证：三档互不相同 ⇒ 写死 δ / 用 gamesLost 当下标 ⇒ 至少一条红）");
                    C(q0.Contains("C5 == 3"), "③d GetMatchmakingFilter：`gamesLost` 原样进 C5 段");
                    C(ev.GetMatchmakingFilter(5, 0, 0).Contains("C1 >= 0"), "③e GetMatchmakingFilter：下界钳到 0（5 − 10 ⇒ 0）");
                    // 灭自证（模态）：同一份数据、只改 tryCount ⇒ 三条串必须两两不同
                    C(q0 != q1 && q1 != q2 && q0 != q2, "③f GetMatchmakingFilter：三档串两两不同（写死任一档 ⇒ 红）");
                }

                // ══════════ ④ `TryCollect` 的三道闸 ══════════
                {
                    // 闸①：一档都够不到
                    var s1 = MkSave("fm_tc1"); s1.localMilestoneScore = 0;
                    var e1 = new FixtureFastMode(MkData("fm_tc1", new[] { 10 }, 50), s1);
                    int hit1 = 0; var oldCb = FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected;
                    FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = delegate { hit1++; };
                    try
                    {
                        e1.TryCollect(null);
                        C(hit1 == 0 && s1.collectedRewards == 0, "④a TryCollect 闸①（`milestone > CurrentScore` 全中）⇒ 不领、`collectedRewards` 不变");
                    }
                    finally { FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = oldCb; }

                    // 闸②：`pending <= collectedRewards`（没有新的可领）
                    var s2 = MkSave("fm_tc2"); s2.localMilestoneScore = 20; s2.collectedRewards = 2;
                    var e2 = new FixtureFastMode(MkData("fm_tc2", new[] { 10 }, 10, 20), s2);
                    int hit2 = 0;
                    FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = delegate { hit2++; };
                    try
                    {
                        e2.TryCollect(null);
                        C(hit2 == 0 && s2.collectedRewards == 2, "④b TryCollect 闸②（`pending == collectedRewards`）⇒ 不领、不推进（灭自证：「总是推进」⇒ 红）");
                    }
                    finally { FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = oldCb; }

                    // 闸③：有新可领 ⇒ 推进到 pending 且把新那几档的 rewardId 交出去
                    var s3 = MkSave("fm_tc3"); s3.localMilestoneScore = 20; s3.collectedRewards = 0;
                    var e3 = new FixtureFastMode(MkData("fm_tc3", new[] { 10 }, 10, 20, 30), s3);
                    CollectFastModeRewardsResult got3 = null;
                    FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = delegate (CollectFastModeRewardsResult r) { got3 = r; };
                    try
                    {
                        e3.TryCollect(null);
                        C(s3.collectedRewards == 2, "④c TryCollect 闸③：`collectedRewards` 推到 `pending`（= 2，⛔ 不是推到总档数 3）");
                        C(got3 != null && got3.rewardIds != null && got3.rewardIds.Length == 2,
                          "④d TryCollect 闸③：交出的 `rewardIds` **只**含新那两档（⛔ 不是三档全给）");
                        C(got3 != null && ReferenceEquals(got3.updatedPersistence, s3), "④e TryCollect 闸③：`updatedPersistence` = 本事件存档");
                    }
                    finally { FastModeBaseEvent<FastModeSave, FastModeData>.OnRewardsCollected = oldCb; }
                }

                // ══════════ ⑤ `GetTimeDescriptionTextKey` / `GetTimeDescriptionText` ══════════
                {
                    var evActive = new FixtureEvent(new LiveOpsEventData { eventId = "fmd_act", activationType = LiveOpsActivationType.Permanent });
                    string kA = evActive.GetTimeDescriptionTextKey();

                    var dFin = new LiveOpsEventData { eventId = "fmd_fin", activationType = LiveOpsActivationType.OnDate };
                    dFin.dates = new LiveOpsEventTimeData { startDate = t - 3000000, endDate = t - 10000, expirable = true, expireDate = t + 1000000 };
                    string kF = new FixtureEvent(dFin).GetTimeDescriptionTextKey();

                    var dPre = new LiveOpsEventData { eventId = "fmd_pre", activationType = LiveOpsActivationType.OnDate };
                    dPre.dates = new LiveOpsEventTimeData { startDate = t + 1000000, endDate = t + 2000000, previewable = true, previewDate = t - 10000, expirable = true, expireDate = t + 3000000 };
                    string kP = new FixtureEvent(dPre).GetTimeDescriptionTextKey();

                    var dExp = new LiveOpsEventData { eventId = "fmd_exp", activationType = LiveOpsActivationType.OnDate };
                    dExp.dates = new LiveOpsEventTimeData { startDate = t - 3000000, endDate = t - 2000000, expirable = true, expireDate = t - 1000 };
                    var evExp = new FixtureEvent(dExp);
                    string kX = evExp.GetTimeDescriptionTextKey();

                    C(kP == "Event_Description/General/TimeStartsIn", "⑤a Key：OnPreview(1) ⇒ `TimeStartsIn`（字面量 0x42AAA88）");
                    C(kA == "Event_Description/General/TimeEndsIn", "⑤b Key：Active(2) ⇒ `TimeEndsIn`（字面量 0x42AA898）");
                    C(kF == "Event_Description/General/TimeFinished", "⑤c Key：Finished(3) ⇒ `TimeFinished`（字面量 0x42AA990）");
                    C(kX == "", "⑤d Key：Expired(4) ⇒ 空串（字面量 0x42B80E0）");
                    C(kP != kA && kA != kF && kP != kF, "⑤e 三档键**两两不同**（灭自证：写死同一个键 ⇒ ⑤a/⑤b/⑤c 至少两条红）");
                    C(!string.IsNullOrEmpty(evActive.GetTimeDescriptionText()), "⑤f Text：键非空 ⇒ `Loc.T` 出来的显示文本非空（三档兜底）");
                    C(evExp.GetTimeDescriptionText() == "", "⑤g Text：键为空 ⇒ 空串（不走本地化）");
                }

                // ══════════ ⑥ `LiveOpsHandler.GetBaseEvents` = 虚转发 ══════════
                {
                    var h = new FixtureHandler();
                    var ev1 = new FixtureEvent(new LiveOpsEventData { eventId = "fmd_h1", activationType = LiveOpsActivationType.Permanent });
                    h.Stamp.Add(ev1);
                    var got = h.GetBaseEvents(false);
                    C(h.OverrideCalls > 0, "⑥a GetBaseEvents：走的是**虚方法** `GetEvents`（子类 override 被调用）—— 实现直接 `return events` ⇒ 红");
                    var list = new List<ILiveOps>(got);
                    C(list.Count == 1 && ReferenceEquals(list[0], ev1), "⑥b GetBaseEvents：返回的就是 `GetEvents` 给出的那一批（内容等价）");
                    C(h.GetBlockedEvents() != null, "⑥c GetBlockedEvents：非 null（原样返回字段）");
                }

                // ══════════ ⑦ `LiveOpsHandlerRefreshedSignal` + `RefreshHandler` 的发射跳 ══════════
                {
                    var taps = new List<LiveOpsHandlerRefreshedSignal>();
                    Action<LiveOpsHandlerRefreshedSignal> tap = delegate (LiveOpsHandlerRefreshedSignal s) { taps.Add(s); };
                    var h = new FixtureHandler();
                    LiveOpsSignal.Register<LiveOpsHandlerRefreshedSignal>(tap);
                    try
                    {
                        LiveOpsSignal.Raise(new LiveOpsHandlerRefreshedSignal(h));
                        C(taps.Count == 1 && ReferenceEquals(taps[0].Handler, h),
                          "⑦a LiveOpsHandlerRefreshedSignal：载荷 `Handler` 原样送达订阅方");
                    }
                    finally { LiveOpsSignal.Unregister<LiveOpsHandlerRefreshedSignal>(tap); }
                    LiveOpsSignal.Raise(new LiveOpsHandlerRefreshedSignal(h));
                    C(taps.Count == 1, "⑦b 退订后不再收到（`Register`/`Unregister` 成对）");
                    // 🔴 灭自证（结构）：光有信号类不算接线 —— 发射点必须真在 `RefreshHandler(LiveopsHandlerData)` 里
                    string src = ReadShellSource("LiveOpsHandler.cs");
                    C(src != null, "⑦c 前提：读得到 `Shell/LiveOpsHandler.cs` 源码（读不到 ⇒ 下面的扫描断言无意义）");
                    C(src != null && src.Contains("LiveOpsSignal.Raise(new LiveOpsHandlerRefreshedSignal(this))"),
                      "⑦d `RefreshHandler(LiveopsHandlerData)` 末尾**真的** raise 了这个信号（源码扫描）—— 删掉那一跳 ⇒ 红");
                }

                // ══════════ ⑧ `ReleaseEventAssets` ══════════
                {
                    int released = 0;
                    var oldRelease = LiveOpsAssetUtility.ReleaseAsset;
                    LiveOpsAssetUtility.ReleaseAsset = delegate (string g) { released++; };
                    try
                    {
                        LiveOpsAssetUtility.ReleaseEventAssets(null);
                        C(released == 0, "⑧a ReleaseEventAssets(null)：直接返回（原版 `.c:30` 的 `if (param_1 == 0) return`，**不是** NRE）");

                        var any = LiveOpsLocalCatalog.Events != null && LiveOpsLocalCatalog.Events.Count > 0 ? LiveOpsLocalCatalog.Events[0] : null;
                        C(any != null, "⑧b 前提：本地活动目录非空（否则下面两条无从谈起）");
                        if (any != null)
                        {
                            int expect = (any.Components != null ? any.Components.Length : 0) + (any.Assets != null ? any.Assets.Length : 0);
                            C(expect > 0, "⑧c 前提：'" + any.EventId + "' 的引用表非空（= " + expect + "；为 0 会让 ⑧d 空转绿）");
                            released = 0;
                            var inst = new FixtureEvent(new LiveOpsEventData { eventId = any.EventId, activationType = LiveOpsActivationType.Permanent });
                            LiveOpsAssetUtility.ReleaseEventAssets(inst);
                            C(released == expect, "⑧d ReleaseEventAssets：释放次数 = Components + Assets = " + expect + "（漏掉任一段 ⇒ 红）");
                        }
                    }
                    finally { LiveOpsAssetUtility.ReleaseAsset = oldRelease; }
                }

                // ══════════ ⑨ `SignalEventTrigger<T>.Register` / `Unregister` ══════════
                {
                    var trig = new FixtureSignalTrigger { Want = "b93n2-yes" };
                    int fired = 0;
                    trig.Register(delegate { fired++; });
                    try
                    {
                        LiveOpsSignal.Raise(new EventExpiredSignal("b93n2-no"));
                        C(fired == 0, "⑨a Register：`ValidateSignal` 挡掉不匹配的信号（**先校验后回调**）");
                        LiveOpsSignal.Raise(new EventExpiredSignal("b93n2-yes"));
                        C(fired >= 1, "⑨b Register：匹配的信号把回调打出来了");
                    }
                    finally { trig.Unregister(); }
                    int afterUnreg = fired;
                    LiveOpsSignal.Raise(new EventExpiredSignal("b93n2-yes"));
                    C(fired == afterUnreg, "⑨c Unregister：摘掉后不再回调（灭自证：只 Register 不 Unregister ⇒ 红）");
                }

                // ══════════ ⑩ `Expire` 的「组件 21 那扇窗」（`LiveOpsExpirePopup`，2026-10-28 B93·N2） ══════════
                {
                    C(!LiveOpsExpirePopup.HasComponent(null), "⑩a HasComponent(ExpirePopup)：`liveOps == null` ⇒ false（⛔ 不抛）");
                    var withExp = new FixtureEvent(new LiveOpsEventData { eventId = "ExpansionOrks1Scoring", activationType = LiveOpsActivationType.Permanent });
                    var without = new FixtureEvent(new LiveOpsEventData { eventId = "DarkZealots", activationType = LiveOpsActivationType.Permanent });
                    C(LiveOpsExpirePopup.HasComponent(withExp), "⑩b HasComponent(ExpirePopup)：有 key=21 的活动 ⇒ **true**（判据 = 本地目录里 `ExpansionOrks1Scoring` 带 `ExpirePopup`）");
                    C(!LiveOpsExpirePopup.HasComponent(without), "⑩c HasComponent(ExpirePopup)：没有 key=21 的活动 ⇒ **false**（灭自证：与 ⑩b 相反 ⇒ 恒真/恒假的实现必然红一条）");
                    C(LiveOpsExpirePopup.ExpirePopupKey == (int)LiveOpsComponentRef.ExpirePopup, "⑩d `ExpirePopupKey` = `LiveOpsComponentRef.ExpirePopup`（= 21，原版 `.c` 的字面量 `0x15`）");
                }
            }
            finally
            {
                LiveOpsClock.NowProvider = oldNow;
                LiveOpsSignals.Verbose = oldVerbose;
                LiveOpsContainers.ResetForTest();
            }
            return n;
        }
    }
}
