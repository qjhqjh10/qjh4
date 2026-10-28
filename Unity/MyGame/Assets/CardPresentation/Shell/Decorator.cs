// Decorator.cs — 原版 `Decorator<T>` / `IDecorable` / `IDecorator<T>`（**可装饰对象的「值变了就刷新」基类**）。
//
// ============================ 判据（第一权威 = 反编译，本轮现读）============================
//   · `out_cpp2il_dev_ilspy/Decorator.cs`（四方法全文 + 抽象 `OnObjectUpdated`）：
//       `public abstract class Decorator<T> : MonoBehaviour, IDecorator<T> where T : IDecorable`
//       `Awake()`               = `decorable = GetComponent<T>();`（写 `this+0x20`；ILSpy 里那一步是
//                                 `Method not found @180C64AA0` = 泛型 `GetComponent<T>`）
//       `OnEnable()`            = `decorable.Updated += OnDecorableUpdated;`（`this+0x20 != null` 才挂）
//       `OnDisable()`           = `decorable.Updated -= OnDecorableUpdated;`
//       `OnDecorableUpdated()`  = 转调虚方法 `OnObjectUpdated(decorable)`（ILSpy: `Indirect call … +188`）
//       `public abstract void OnObjectUpdated(T decorable);`
//   · 唯一子类 = `CampaignArmyUIDecorator : Decorator<ArmyItemContainer>`（**1 个 prefab 实例**：`Campaign Army Item Button`）
//   · 🥇 泛型表 `账/泛型与启动链_补读.tsv` 612–616 已判 **A**，落点 `Shell/CampaignTab.cs.OnArmyAmountChanged`；
//     本文件是**补基类本体**（原版有这个方法，我们原来只有它的行为内联）
//
// ⚠️ **零消费者（原版也没有别的子类）**：`grep "Decorator<"` 全仓 = 0（本文件之前）。按 A 尺子 ①
//    「零调用者、上游在 B/C」⇒ 判 A；上游 = `CampaignTab.cs` 的内联行为（账 `未分类_其他` 第 55–58 行）。
//    ⛔ **不改活路径**：`CampaignTab.OnArmyAmountChanged` 保持现状（要改成子类属重构，得单独批）。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `IDecorable`：可被装饰的对象只要求「值变了会喊一声」。
    /// <para>⚠️ 判据缺一片：`IDecorable` 的完整成员表在 ILSpy 里只剩 `Updated` 这一个事件（`OnDecorableUpdated`
    /// 的签名 `(object, EventArgs)` 与 `+=`/`-=` 两处调用都指向 `event EventHandler`）⇒ **只复刻这一格**，
    /// ⛔ 别按名字再编别的成员。</para></summary>
    public interface IDecorable
    {
        /// <summary>值变了（原版 `event EventHandler Updated`）。</summary>
        event EventHandler Updated;
    }

    /// <summary>原版 `IDecorator&lt;T&gt;`（`Decorator&lt;T&gt;` 唯一实现的接口，`out_cpp2il_dev_ilspy/Decorator.cs` 的类头）。</summary>
    public interface IDecorator<T> where T : IDecorable
    {
        void OnObjectUpdated(T decorable);
    }

    /// <summary>原版 `Decorator&lt;T&gt; : MonoBehaviour, IDecorator&lt;T&gt;`。见文件头判据。</summary>
    public abstract class Decorator<T> : MonoBehaviour, IDecorator<T> where T : IDecorable
    {
        /// <summary>原版 `protected T decorable`（`this+0x20`）。</summary>
        protected T decorable;

        /// <summary>原版 `Awake()`：`decorable = GetComponent&lt;T&gt;();`。
        /// ⚠️ 原版这一跳**不判空**（拿不到就一直是 null，`OnEnable` 那一句 `!= null` 才兜住）。</summary>
        void Awake()
        {
            decorable = GetComponent<T>();
        }

        /// <summary>原版 `OnEnable()`：`decorable.Updated += OnDecorableUpdated;`（`decorable != null` 才挂）。</summary>
        void OnEnable()
        {
            if (decorable == null) return;
            decorable.Updated += OnDecorableUpdated;
        }

        /// <summary>原版 `OnDisable()`：`decorable.Updated -= OnDecorableUpdated;`（同上一道判空）。</summary>
        void OnDisable()
        {
            if (decorable == null) return;
            decorable.Updated -= OnDecorableUpdated;
        }

        /// <summary>原版 `OnDecorableUpdated(object sender, EventArgs e)`：**只转调** `OnObjectUpdated(decorable)`
        /// （ILSpy 里那一跳是 `Indirect call: [v72 … (Il2CppClass&lt;Decorator`1&lt;T&gt;&gt;)+188]`）。
        /// ⚠️ 原版**不判** `decorable == null`（`OnEnable` 已挡住 null 才挂）；我们判一下并静默返回
        /// （唯一后果 = 坏数据时少一次刷新，⛔ 与「值变了却不刷」不是同一件事）。</summary>
        void OnDecorableUpdated(object sender, EventArgs e)
        {
            if (decorable == null) return;
            OnObjectUpdated(decorable);
        }

        /// <summary>原版 `public abstract void OnObjectUpdated(T decorable);` —— 子类在这里刷新自己。</summary>
        public abstract void OnObjectUpdated(T decorable);

#if UNITY_EDITOR
        // ============================================================ 结构性自检钩子（B92_N6b · 由 Editor 宿主调用）
        /// <summary>结构性自检（不依赖场景/资产）。返回**问题清单**，空 = 通过。
        /// <para>判据只核「原版有的形状在不在」——⛔ 不核行为（行为要么真机看，要么等有子类）。</para></summary>
        public static System.Collections.Generic.List<string> SelfCheck()
        {
            var errs = new System.Collections.Generic.List<string>();
            var t = typeof(Decorator<>);
            if (!typeof(MonoBehaviour).IsAssignableFrom(t)) errs.Add("Decorator<T> 不是 MonoBehaviour（原版是）");
            if (!t.IsAbstract) errs.Add("Decorator<T> 不是 abstract（原版是）");
            if (!typeof(IDecorator<>).MakeGenericType(t.GetGenericArguments()[0]).IsAssignableFrom(t))
                errs.Add("Decorator<T> 没实现 IDecorator<T>（原版实现了）");
            var m = t.GetMethod("OnObjectUpdated", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (m == null || !m.IsAbstract) errs.Add("`OnObjectUpdated(T)` 不存在或不是 abstract");
            foreach (var name in new[] { "Awake", "OnEnable", "OnDisable", "OnDecorableUpdated" })
            {
                var mi = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (mi == null) errs.Add("缺方法 `" + name + "`（原版有）");
            }
            var ev = typeof(IDecorable).GetEvent("Updated");
            if (ev == null || ev.EventHandlerType != typeof(EventHandler))
                errs.Add("`IDecorable.Updated` 不是 `event EventHandler`（原版是）");
            var f = t.GetField("decorable", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (f == null || !f.FieldType.IsGenericParameter) errs.Add("字段 `decorable` 不存在或不是 T（原版是 `protected T`）");
            return errs;
        }
#endif
    }
}
