// WFModuleEvent.cs — 原版 `AnimFXModuleEvent` 的对应物
//   **1 实例**（`VanguardIdleEffect`，`eventsToFire[0].whereToFire = 5 (Exit)`）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§4**（3 个方法体：`.ctor` 空 / `Initialize` / `Exit`；
//   `FireEvent.TryFireEvent` 被内联进这两个里）
//   · 两个方法体**同构**：遍历 `eventsToFire(+0x38)`，对每个 `FireEvent`
//     当 `whereToFire(+0x18) == 时机` **且** `eventToFire(+0x10) != null` 时 `UnityEvent.Invoke()`
//   · `Initialize` 匹配 **0**（`ActionStart.Initialize`）、`Exit` 匹配 **5**（`ActionStart.Exit`）
//   · ⚠️ **只有这两个时机有方法体** ⇒ `DoDestroy(10)` / `Manual(15)` 在原版**没有分支**
//     （照原样不实现；真出现那两个值会打一次性警告 —— 见下）
//   · ⚠️ 它读的是**每条自己的 `whereToFire`**，不是基类那个 `actionStart`（本类不读基类那个字段）
//   · 数据里 `whereToFire` **只有 `5` 这一个取值**（出货数据）
//
// 照方法体还原了什么：
//   · 两个时机（Initialize / Exit）逐一比对 `whereToFire` 并 `Invoke()`
//   · `eventToFire == null` 时**不 Invoke**（原版就是有这个判空）
//   · 条目从数据装出来（`eventsToFire[i].whereToFire`），手挂模块可以直接在 Inspector 里接 UnityEvent
//
// 我们定的（显式标出）：
//   · 原版数组元素为 null 会走 IL2CPP null 检查**抛异常**（块2 §4：「没有静默跳过」）。
//     我们改成**警告 + 跳过** —— 本项目的红线是「不许静默失败」，不是「必须崩」。
//
// 🔴 未还原（一条，是**这一类的全部内容**）：
//   **这个 UnityEvent 到底调什么，查不到** —— 原版 dump 里 `m_Calls` 被截断
//   （块2 §4 与 §10「还没查到的三处」①："要看 prefab 原始 JSON 逐条读 `m_Calls`，本次没做"）。
//   我们只有 `whereToFire=5` 这一个事实 ⇒ 数据装出来的条目**标记成 `fromData`**，
//   到点触发时**打警告 + 计入 `UnknownCount`、绝不 `Invoke()` 一个空壳**（空壳 Invoke 什么都不做
//   = 静默失败，正是本项目的红线）。**也绝不猜它调什么**（猜 = 静默错一次）。
//   要接：调 `WireDataEvent(i, 你的 UnityEvent)`（之后那条就不再算未还原）。
//   要查清原版调的什么：进原版把 `VanguardIdleEffect` 跑出来看（块2 §4 的建议），
//   或回 prefab 原始 JSON 读 `m_Calls`。
using UnityEngine;
using UnityEngine.Events;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleEvent")]
    public class WFModuleEvent : WFEffectModule
    {
        /// <summary>原版内嵌类 `FireEvent`。</summary>
        [System.Serializable]
        public class FireEvent
        {
            [Tooltip("原版 `whereToFire`：到哪个时机触发。原版只实现了 Initialize(0) 与 Exit(5) 两档")]
            public WFActionStart whereToFire = WFActionStart.Initialize;

            [Tooltip("原版 `eventToFire`。**数据装出来的条目这里是 null**（原版 dump 截断了 m_Calls，" +
                     "不知道它调什么）⇒ 只有手挂模块才用得着它")]
            public UnityEvent eventToFire = new UnityEvent();

            /// <summary>这条是不是从原版数据装出来的（= 内容丢失、只能报警的那一类）。</summary>
            [System.NonSerialized] public bool fromData;
        }

        public FireEvent[] eventsToFire = new FireEvent[0];

        /// <summary>真正 `Invoke()` 过几次。</summary>
        public static int FiredCount;
        /// <summary>因为「不知道它调什么」而丢掉的次数（>0 = 有行为没还原）。</summary>
        public static int UnknownCount;
        public static void ResetDiagnostics() { FiredCount = 0; UnknownCount = 0; }

        bool _warnedUnknown, _warnedNoBranch;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);                  // actionStart（本类不读它，但基类字段要填上）
            eventsToFire = ReadEvents(def);
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            WarnAboutUnbranchableEntries();
            Fire(WFActionStart.Initialize, "Initialize");
        }

        public override void Exit()
        {
            Fire(WFActionStart.Exit, "Exit");
        }

        /// <summary>给**从数据装出来**的那条补一个 UnityEvent（它原来调的什么查不到，只能由调用方定）。
        /// 调过之后这条就不再算「未还原」，到点会真的 `Invoke()`。</summary>
        public void WireDataEvent(int index, UnityEvent e)
        {
            if (eventsToFire == null || index < 0 || index >= eventsToFire.Length) return;
            if (e == null) return;
            eventsToFire[index].eventToFire = e;
            eventsToFire[index].fromData = false;
            _warnedUnknown = false;      // 让人还能看到别的条目的未还原警告
        }

        /// <summary>原版 `FireEvent.TryFireEvent` 内联出来的那段。</summary>
        void Fire(WFActionStart when, string from)
        {
            if (eventsToFire == null) return;
            for (int i = 0; i < eventsToFire.Length; i++)
            {
                var e = eventsToFire[i];
                if (e == null)
                {
                    // 原版这里没有保护（会 NRE）；我们警告 + 跳过（见文件头「我们定的」）
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleEvent：`eventsToFire[{i}]` 是空的" +
                                     "（原版会在这里抛 NullReferenceException，我们跳过）");
                    continue;
                }
                if (e.whereToFire != when) continue;

                if (e.fromData || e.eventToFire == null)
                {
                    // 从原版数据装出来的条目：**内容查不到** ⇒ 宁可报「未还原」，也绝不 Invoke 一个空壳
                    // （空壳 Invoke 什么都不做 = 静默失败，正是本项目的红线）
                    UnknownCount++;
                    if (!_warnedUnknown)
                    {
                        _warnedUnknown = true;
                        Debug.LogWarning($"🔴 未还原：《{EffectName}》的 AnimFXModuleEvent 到 `{from}` 时机要触发的" +
                                         " UnityEvent **内容查不到**（原版 dump 里 `m_Calls` 被截断，块2 §4/§10）" +
                                         "⇒ 这条事件被丢掉。数据里只有 `whereToFire` 一个事实，" +
                                         "我们**不猜**它调什么。要接就调 `WireDataEvent(i, 你的 UnityEvent)`。"
                                         + (e.fromData ? "" : "（这条是手挂的，在 Inspector 里接一个 UnityEvent 就好）"));
                    }
                    continue;
                }

                e.eventToFire.Invoke();
                FiredCount++;
            }
        }

        /// <summary>原版**没有** `DoDestroy(10)` / `Manual(15)` 两档的方法体（块2 §4）——
        /// 数据里每条都在这几档时，这条事件永远不会被触发，报一声（不静默）。</summary>
        void WarnAboutUnbranchableEntries()
        {
            if (eventsToFire == null || _warnedNoBranch) return;
            for (int i = 0; i < eventsToFire.Length; i++)
            {
                var e = eventsToFire[i];
                if (e == null) continue;
                if (e.whereToFire == WFActionStart.Initialize || e.whereToFire == WFActionStart.Exit) continue;
                _warnedNoBranch = true;
                Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleEvent 有条目 `eventsToFire[{i}].whereToFire=" +
                                 $"{e.whereToFire}` —— 原版**只有 Initialize(0) 与 Exit(5) 两个方法体**（块2 §4）" +
                                 "⇒ 这一档永远不会被触发（我们照原版不实现）。");
                return;
            }
        }

        /// <summary>把 `eventsToFire[i].whereToFire` 装出来。
        /// ⚠️ 只能按 `keys` 里的下标扫，不能用 `GetList("eventsToFire")` ——
        ///    扁平化后的键是 `eventsToFire[0].whereToFire`，**没有 `eventsToFire[0]` 这个键**。
        ///    而 `eventToFire` 因为内容被截断**根本不在数据里**（所以装出来是 null）。</summary>
        static FireEvent[] ReadEvents(WFModuleDef def)
        {
            if (def == null || def.keys == null) return new FireEvent[0];
            const string pre = "eventsToFire[";
            var idxs = new System.Collections.Generic.SortedSet<int>();
            foreach (var k in def.keys)
            {
                if (string.IsNullOrEmpty(k) || !k.StartsWith(pre, System.StringComparison.Ordinal)) continue;
                int close = k.IndexOf(']', pre.Length);
                if (close < 0) continue;
                int i;
                if (int.TryParse(k.Substring(pre.Length, close - pre.Length), out i)) idxs.Add(i);
            }
            if (idxs.Count == 0) return new FireEvent[0];

            var arr = new FireEvent[idxs.Count];
            int n = 0;
            foreach (var i in idxs)
            {
                var e = new FireEvent
                {
                    whereToFire = (WFActionStart)def.GetInt(pre + i + "].whereToFire"),
                    fromData = true,
                };
                // 数据里没有可重建的 UnityEvent 内容 ⇒ 置 null，让 Fire() 走「未还原」那条路（不静默）
                e.eventToFire = null;
                arr[n++] = e;
            }
            return arr;
        }
    }
}
