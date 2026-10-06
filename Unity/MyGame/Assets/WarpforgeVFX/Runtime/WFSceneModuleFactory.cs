// WFSceneModuleFactory.cs —— 把「原版类名」翻译成「**场景侧**哪个模块组件」
//   🆕 2026-10-14（A394）新建，形制**照抄** `WFModuleFactory`（卡侧那条线的那一份）。
//
// 为什么要有第二份：两条线的模块基类不同（`WFEffectModule` 收 `WarpforgeEffectPlayer`、
// `WFSceneModule` 收 `AnimFXController`，见 `WFSceneModule.cs` 文件头），
// **注册表也必须分开** —— 否则一个类名的查表会把另一条线的类拿出来（那是静默错）。
//
// 原版那 2346 个模块在数据里只留了个类名字符串（`AnimFXModuleScreenShake` …），运行时要把它们变成
// 真的组件。这张映射如果写成一个大 `switch`，**每加一个模块都要改这个文件** —— 多个人/多个代理
// 同时做就会撞车。⇒ 与卡侧同一条设计：**特性 + 反射自动登记**（每个模块类自己贴一个
// `[WFSceneModuleKind("原版类名")]`），**加模块只需要新建一个文件**，不用动这里，也不用动控制器。
//
// 🔴 **认不出的 kind 一定打警告**（每个 kind 只报一次，避免刷屏）——
//    本项目「不许静默失败」那条：装不上的模块要让人知道，不能让场景看起来「就是那样」。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>贴在一个 `WFSceneModule` 子类上，声明它对应原版的哪个类。</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class WFSceneModuleKindAttribute : Attribute
    {
        public readonly string Kind;
        public WFSceneModuleKindAttribute(string kind) { Kind = kind; }
    }

    public static class WFSceneModuleFactory
    {
        static Dictionary<string, Type> _map;

        static void EnsureMap()
        {
            if (_map != null) return;
            _map = new Dictionary<string, Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (Exception) { continue; }          // 有些程序集反射会抛，跳过就好
                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || !typeof(WFSceneModule).IsAssignableFrom(t)) continue;
                    foreach (var a in t.GetCustomAttributes(typeof(WFSceneModuleKindAttribute), false))
                    {
                        var kind = ((WFSceneModuleKindAttribute)a).Kind;
                        if (string.IsNullOrEmpty(kind)) continue;
                        if (_map.ContainsKey(kind))
                            Debug.LogWarning($"[WarpforgeVFX] **场景侧**模块 kind `{kind}` 被多个类登记" +
                                             $"（{_map[kind].Name} 与 {t.Name}）—— 用后登记的那个");
                        _map[kind] = t;
                    }
                }
            }
        }

        /// <summary>已经实现的**场景侧**模块 kind（自检 / 报告用）。</summary>
        public static IEnumerable<string> ImplementedKinds { get { EnsureMap(); return _map.Keys; } }

        public static bool IsImplemented(string kind)
        {
            EnsureMap();
            return !string.IsNullOrEmpty(kind) && _map.ContainsKey(kind);
        }

        static readonly List<string> _missing = new List<string>();
        /// <summary>**还没实现**、但数据里出现过的场景侧 kind（每个只报一次）。</summary>
        public static IReadOnlyList<string> MissingKinds { get { return _missing; } }

        /// <summary>建组件并 `Configure`。**kind 没实现 → 打一次警告 + 返回 null**（调用方继续，不中断）。
        /// ⚠️ 与卡侧 `WFModuleFactory.Create` 的**唯一差别**是基类与注册表；`def` 允许为 null
        /// （场景侧旁挂今天只带类名，见 `WFSceneModuleScreenShake.cs` 文件头）。</summary>
        public static WFSceneModule Create(GameObject host, string kind, WFModuleDef def = null)
        {
            if (host == null || string.IsNullOrEmpty(kind)) return null;
            EnsureMap();

            Type t;
            if (!_map.TryGetValue(kind, out t))
            {
                if (!_missing.Contains(kind))
                {
                    _missing.Add(kind);
                    Debug.LogWarning($"[WarpforgeVFX] 原版**场景侧**模块 `{kind}` **还没有实现** —— " +
                                     "装了它的场景对象会少这一层行为（不是静默失败，这条就是它）。" +
                                     $"想补：新建一个 `WFSceneModule` 子类并贴 " +
                                     $"[WFSceneModuleKind(\"{kind}\")]，见 WFSceneModuleFactory 的头注释。");
                }
                return null;
            }

            var m = host.AddComponent(t) as WFSceneModule;
            if (m == null) return null;
            // ⚠️ `Configure` 必须**在建出来之后立刻**调（各模块「一建好就动」的自动档放这里 ——
            //    见 `WFSceneModule.Configure` 那条约定）。传 null def 也照调：基类会安全跳过。
            m.Configure(def);
            return m;
        }

        public static void ResetMissing() { _missing.Clear(); }

        /// <summary>一个模块实例对应哪个原版 kind（自检用）。
        /// ⚠️ 别用「装了模块数 &gt; 0」当判据 —— 一个对象**可能带好几个模块**，
        /// 那样写会让「没实现的 kind」被别的模块顶成绿的（卡侧那份踩过这一下）。</summary>
        public static string KindOf(WFSceneModule m)
        {
            if (m == null) return null;
            foreach (var a in m.GetType().GetCustomAttributes(typeof(WFSceneModuleKindAttribute), false))
                return ((WFSceneModuleKindAttribute)a).Kind;
            return null;
        }
    }
}
