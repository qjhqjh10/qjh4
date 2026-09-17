// WFModuleFactory.cs —— 把「原版类名」翻译成「我们这边哪个模块组件」
//
// 原版那 2346 个模块在数据里只留了个类名字符串（`AnimFXModuleScreenShake` …），
// 运行时要把它们变成真的组件。这张映射如果写成一个大 `switch`，**每加一个模块都要改这个文件**
// —— 多个人/多个代理同时做就会撞车。
//
// ⇒ 改成**特性 + 反射自动登记**：每个模块类自己贴一个 `[WFModuleKind("原版类名")]`，
//    工厂第一次用的时候扫一遍所有已加载程序集，自己把表建起来。
//    **加模块只需要新建一个文件**，不用动这里，也不用动播放器。
//
// 🔴 **认不出的 kind 一定打警告**（每个 kind 只报一次，避免刷屏）——
//    这是本项目「不许静默失败」那条：装不上的模块要让人知道，不能让效果看起来「就是那样」。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>贴在一个 `WFEffectModule` 子类上，声明它对应原版的哪个类。</summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class WFModuleKindAttribute : Attribute
    {
        public readonly string Kind;
        public WFModuleKindAttribute(string kind) { Kind = kind; }
    }

    public static class WFModuleFactory
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
                    if (t == null || t.IsAbstract || !typeof(WFEffectModule).IsAssignableFrom(t)) continue;
                    foreach (var a in t.GetCustomAttributes(typeof(WFModuleKindAttribute), false))
                    {
                        var kind = ((WFModuleKindAttribute)a).Kind;
                        if (string.IsNullOrEmpty(kind)) continue;
                        if (_map.ContainsKey(kind))
                            Debug.LogWarning($"[WarpforgeVFX] 模块 kind `{kind}` 被多个类登记" +
                                             $"（{_map[kind].Name} 与 {t.Name}）—— 用后登记的那个");
                        _map[kind] = t;
                    }
                }
            }
        }

        /// <summary>已经实现的模块 kind（自检 / 报告用）。</summary>
        public static IEnumerable<string> ImplementedKinds { get { EnsureMap(); return _map.Keys; } }

        public static bool IsImplemented(string kind)
        {
            EnsureMap();
            return !string.IsNullOrEmpty(kind) && _map.ContainsKey(kind);
        }

        static readonly List<string> _missing = new List<string>();
        /// <summary>**还没实现**、但数据里出现过的 kind（每个只报一次）。自检据此判「模块层缺多少」。</summary>
        public static IReadOnlyList<string> MissingKinds { get { return _missing; } }

        /// <summary>建组件并 `Configure`。**kind 没实现 → 打一次警告 + 返回 null**（调用方继续，不中断）。</summary>
        public static WFEffectModule Create(GameObject host, WFModuleDef def)
        {
            if (host == null || def == null || string.IsNullOrEmpty(def.kind)) return null;
            EnsureMap();

            Type t;
            if (!_map.TryGetValue(def.kind, out t))
            {
                if (!_missing.Contains(def.kind))
                {
                    _missing.Add(def.kind);
                    Debug.LogWarning($"[WarpforgeVFX] 原版模块 `{def.kind}` **还没有实现** —— " +
                                     "装了它的效果会少这一层行为（不是静默失败，这条就是它）。" +
                                     $"想补：新建一个 `WFEffectModule` 子类并贴 " +
                                     $"[WFModuleKind(\"{def.kind}\")]，见 WFModuleFactory 的头注释。");
                }
                return null;
            }

            var m = host.AddComponent(t) as WFEffectModule;
            if (m == null) return null;
            m.Configure(def);
            return m;
        }

        public static void ResetMissing() { _missing.Clear(); }

        /// <summary>一个模块实例对应哪个原版 kind（自检用）。
        /// ⚠️ 别用「装了模块数 &gt; 0」当判据 —— 一个效果**通常带好几个模块**，
        /// 那样写会让「没实现的 kind」被别的模块顶成绿的（本项目的断言踩过这一下）。</summary>
        public static string KindOf(WFEffectModule m)
        {
            if (m == null) return null;
            foreach (var a in m.GetType().GetCustomAttributes(typeof(WFModuleKindAttribute), false))
                return ((WFModuleKindAttribute)a).Kind;
            return null;
        }
    }
}
