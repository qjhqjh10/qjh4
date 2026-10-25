// LiveOpsEventFactory.cs —— **活动实例工厂**（原版 `LiveOpsEventFactory` 的等价实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：账 `日常_奖励.tsv` 里 `LiveOpsEventFactory` 那 3 条 P1 + `判据/泛型与启动链.md` §二「状态→实例表」
//   （`LiveOpsEvent<TSave,TData>` 的 10 个具体子类：Campaign · ChildEvent · ContainerOfferEvent · Forge ·
//    ParentEvent · RankedScoreBoostEvent · RewardEvent · ShopOfferEvent · ShopOfferEventV2 · UnknownLiveOpsEvent）。
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/LiveOpsEventFactory.cs`（三个方法体**全部可读**）
//   + `…/LiveopsHelper.cs` 的 `TryGetEventFromData` / `GetEventFromData`（dataType → eventType 的字典，**沿基类向上找**）。
//
// ============================ 三个方法（逐个照抄）============================
//   `CreateEvent(LiveOpsEventData data, string persistenceRaw, ILiveOpsHandler handler, ValidationStrategy strategy)`
//       → `Type t = LiveopsHelper.GetEventFromData(data.GetType()); if (t == null) return null;`
//         `var ev = ReflectionHelper.New<LiveOpsEvent>(t);`
//         `var save = DeserializePersistence(persistenceRaw, ev.SaveType, strategy);`
//         `ev.LoadEventData(data, save, handler); return ev;`
//   `CreateEvent(LiveOpsEventData data, LiveOpsEventPersistence persistence, ILiveOpsHandler handler, ValidationStrategy strategy)`
//       → 同上，只是存档已经是对象（不反序列化）。
//   `DeserializePersistence(string persistenceRaw, Type type, ValidationStrategy strategy)`
//       → `var p = (LiveOpsEventPersistence)JsonWrapper.DeserializeObject(persistenceRaw, type);`
//         `return (p is LiveOpsEventPersistence) ? p : null;`
//
// ⚠️ **与原版的如实差异**：
//   ① `ReflectionHelper.New<T>(Type)` 我们没有 ⇒ 用 `Activator.CreateInstance(eventType)`（语义相同：无参构造）。
//   ② `JsonWrapper.DeserializeObject`（Newtonsoft）我们没有 ⇒ 用 `JsonUtility.FromJson(raw, type)`
//      （⚠️ `JsonUtility` 只认 `[Serializable]` + public 字段，我们的 `LiveOpsSave` 满足；但**不支持 null/缺失字段的默认值语义**，
//      原版 Newtonsoft 会给类字段填默认值，`JsonUtility` 落成 0/false ⇒ 这里有一处**真实语义差**，写在账里）。
//   ③ `ValidationStrategy` 在原版这三个方法体里**一次都没被读到**（ILSpy 可证）⇒ 我们保留参数、不用它，
//      并在 `DeserializePersistence` 里显式 `if (strategy == Strict) 类型不对就出声`（原版失败只是返回 null，不 Log）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ValidationStrategy`（`Everguild.Utils`）：**这三个方法体里没用到**（见文件头差异 ③），
    /// 我们只借它决定「解析失败要不要出声」。</summary>
    public enum LiveOpsValidationStrategy
    {
        /// <summary>宽松：解析失败返回 null（原版行为）。</summary>
        Lenient = 0,
        /// 🔶 自己设计（非原版）：`Strict` 策略额外 LogError · 原版 `ValidationStrategy` 这三个方法体没用到，我们只借它决定是否出声（原版判据拿不到） · TODO(谁定的)｜原注：<summary>严格：解析失败**额外 LogError**（我们加的，便于定位坏载荷；⛔ 不改变返回值）。</summary>
        Strict = 1,
    }

    /// <summary>活动实体的 `dataType → eventType` 映射（原版 `LiveopsHelper.EventTypeDataDictionary`）。
    /// 原版是「扫全程序集建表」，我们**显式登记**（工程里目前只有一个具体事件类 <see cref="LocalLiveOpsEvent"/>）。
    /// ⛔ 别在这里凭类名猜原版那 10 个子类 —— 它们各自的 `TSave` 类型本地没有判据。</summary>
    public static class LiveOpsEventTypeMap
    {
        static readonly System.Collections.Generic.Dictionary<Type, Type> _map =
            new System.Collections.Generic.Dictionary<Type, Type>();

        static LiveOpsEventTypeMap()
        {
            // 我们的唯一具体事件类：data = LiveOpsEventData（基类），event = LocalLiveOpsEvent。
            _map[typeof(LiveOpsEventData)] = typeof(LocalLiveOpsEvent);
        }

        /// <summary>登记一条映射（将来接 `ExpansionPassEvent` / `RaidEvent` 等具体子类时用）。</summary>
        public static void Register(Type dataType, Type eventType)
        {
            if (dataType == null || eventType == null) return;
            _map[dataType] = eventType;
        }

        /// <summary>原版 `LiveopsHelper.TryGetEventFromData(dataType, out eventType)`（`LiveopsHelper.cs:153`，**逐条照抄**）：
        /// **沿 `BaseType` 向上找**，任一层命中即返回该层的映射；一路到 null 都没命中 ⇒ false。</summary>
        public static bool TryGetEventFromData(Type dataType, out Type eventType)
        {
            eventType = null;
            if (dataType == null) return false;
            for (Type t = dataType; t != null; t = t.BaseType)
            {
                if (_map.TryGetValue(t, out eventType)) return true;
            }
            eventType = null;
            return false;
        }

        /// <summary>原版 `LiveopsHelper.GetEventFromData(dataType)`：`TryGet…` 的简化版（取不到 = null）。</summary>
        public static Type GetEventFromData(Type dataType)
        {
            Type t;
            return TryGetEventFromData(dataType, out t) ? t : null;
        }

        /// <summary>自检收尾：恢复出厂映射。</summary>
        public static void ResetForTest()
        {
            _map.Clear();
            _map[typeof(LiveOpsEventData)] = typeof(LocalLiveOpsEvent);
        }
    }

    /// <summary>活动实例工厂（原版 `LiveOpsEventFactory`，三个方法逐条见文件头）。</summary>
    public sealed class LiveOpsEventFactory
    {
        /// <summary>原版 `CreateEvent(LiveOpsEventData data, **LiveOpsEventPersistence** persistence, ILiveOpsHandler handler, ValidationStrategy strategy)`。
        /// <para>⚠️ 原版两个重载在我们这里**合并成一个**：`persistenceRaw`（string）那一版由
        /// <see cref="CreateEventFromRaw"/> 承担 —— 因为 C# 里 `string` 与 `LiveOpsSave` 不会重载歧义。
        /// 这样调用点按「手里有什么」挑，语义各归各。</para></summary>
        public ILiveOps CreateEvent(LiveOpsEventData data, LiveOpsSave persistence, ILiveOpsHandler handler,
                                    LiveOpsValidationStrategy strategy = LiveOpsValidationStrategy.Lenient)
        {
            if (data == null)
            {
                Debug.LogError("[LiveOpsFactory] CreateEvent：data 是 null ⇒ 返回 null（原版这一步会 NRE，我们出声降级）");
                return null;
            }
            Type eventType;
            if (!LiveOpsEventTypeMap.TryGetEventFromData(data.GetType(), out eventType))
            {
                Debug.LogWarning("[LiveOpsFactory] CreateEvent：data 类型 '" + data.GetType().Name
                               + "' 没有对应的事件类（原版 `LiveopsHelper.TryGetEventFromData` 同样返回 null）");
                return null;
            }
            var ev = NewEvent(eventType);
            if (ev == null) return null;
            ev.LoadEventData(data, persistence, handler);
            return ev;
        }

        /// <summary>原版 `CreateEvent(LiveOpsEventData data, **string persistenceRaw**, ILiveOpsHandler handler, ValidationStrategy strategy)`：
        /// 多一跳 `DeserializePersistence(persistenceRaw, ev.SaveType, strategy)`。
        /// 🔴 **注意顺序照抄**：先 `New` 出实例（为了拿 `SaveType`），**再**反序列化 —— 不是先反序列化。</summary>
        public ILiveOps CreateEventFromRaw(LiveOpsEventData data, string persistenceRaw, ILiveOpsHandler handler,
                                           LiveOpsValidationStrategy strategy = LiveOpsValidationStrategy.Lenient)
        {
            if (data == null)
            {
                Debug.LogError("[LiveOpsFactory] CreateEventFromRaw：data 是 null ⇒ 返回 null");
                return null;
            }
            Type eventType;
            if (!LiveOpsEventTypeMap.TryGetEventFromData(data.GetType(), out eventType))
            {
                Debug.LogWarning("[LiveOpsFactory] CreateEventFromRaw：data 类型 '" + data.GetType().Name + "' 没有对应的事件类");
                return null;
            }
            var ev = NewEvent(eventType);
            if (ev == null) return null;
            var save = DeserializePersistence(persistenceRaw, ev.SaveType, strategy);
            ev.LoadEventData(data, save, handler);
            return ev;
        }

        /// <summary>原版 `private LiveOpsEventPersistence DeserializePersistence(string persistenceRaw, Type type, ValidationStrategy strategy)`：
        /// `JsonWrapper.DeserializeObject(raw, type)` 之后**再做一次 `is LiveOpsEventPersistence` 检查**（不是就直接 null）。
        /// ⚠️ 差异 ②：`JsonUtility` 的默认值语义比 Newtonsoft 弱（缺字段落成 0/false，Newtonsoft 保留类字段初值）。</summary>
        public LiveOpsSave DeserializePersistence(string persistenceRaw, Type type, LiveOpsValidationStrategy strategy)
        {
            if (string.IsNullOrEmpty(persistenceRaw) || type == null) return null;
            object parsed;
            try { parsed = JsonUtility.FromJson(persistenceRaw, type); }
            catch (Exception e)
            {
                if (strategy == LiveOpsValidationStrategy.Strict)
                    Debug.LogError("[LiveOpsFactory] DeserializePersistence('" + type.Name + "') 失败：" + e.Message);
                return null;
            }
            var save = parsed as LiveOpsSave;
            if (save == null && strategy == LiveOpsValidationStrategy.Strict)
                Debug.LogError("[LiveOpsFactory] DeserializePersistence：'" + type.Name + "' 解出来的东西不是 LiveOpsSave");
            return save;
        }

        /// <summary>原版 `ReflectionHelper.New&lt;LiveOpsEvent&gt;(eventType)` ⇒ `Activator.CreateInstance(eventType)`（差异 ①）。</summary>
        static ILiveOps NewEvent(Type eventType)
        {
            try
            {
                var ev = Activator.CreateInstance(eventType) as ILiveOps;
                if (ev == null)
                    Debug.LogError("[LiveOpsFactory] '" + eventType.Name + "' 不是 ILiveOps（或没有无参构造）⇒ 造不出实例");
                return ev;
            }
            catch (Exception e)
            {
                Debug.LogError("[LiveOpsFactory] Activator.CreateInstance('" + eventType.Name + "') 抛异常：" + e.Message);
                return null;
            }
        }
    }
}
