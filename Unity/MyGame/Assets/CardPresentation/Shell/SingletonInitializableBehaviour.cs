// SingletonInitializableBehaviour.cs — 原版 `SingletonInitializableBehaviour<T>`（「场景单例 + 初始化机」基类）。
//
// ============================ 判据（第一权威 = 反汇编，本轮现读）============================
// 🔴 ILSpy / il2csharp 的这四个方法体**全是 mangled**（只有 `NoteDecompilerIssue`），本轮改用
//    **`d:/2/tools/decomp_generic_trial/out/`** 的四份真体（Ghidra VA 级）：
//    `SingletonInitializableBehaviour_object__{get_Instance__1258820, Awake__1258550,
//     OnInitializationComplete__1258730, OnDestroy__1258650}.c`
//   · `get_Instance()`：`if (instance == null) { LogWarning(<DAT_1842b5240 格式串>)；
//       instance = FindObjectOfType<T>(); if (instance == null) LogError(<DAT_1842538a8>) } return instance;`
//     ⚠️ **两条格式串的文本没解出来**（`{0}` 是 `typeof(T)`，这是 `.c` 里逐句可见的，正文查不到）。
//   · `Awake()`：`if (Instance != null && Instance != this) Destroy(this);`
//   · `OnInitializationComplete()`：`base.OnInitializationComplete(); instance = (T)this;`
//     （`.c` 里基类那一跳被 IL2CPP 包装成 `NetworkingPeer__OnMessage`，按 RVA 对回 `InitializableBehaviour.OnInitializationComplete`）
//   · `OnDestroy()`：`if (Instance == this) { Destroy(this); instance = null; }`
//     🔴 **原版真的在 `OnDestroy` 里再 `Destroy(this)` 一次 —— 照抄，⛔ 别「修好」**（铁律：原版自己的怪写法也照抄）
//
// ⚠️ **零消费者**：我们的 manager 全是纯 C# 静态单例（`LiveOpsManager.Instance` / `ToastNotifications` …），
//   没有一个是 MonoBehaviour ⇒ 本基类目前只有它自己。按 A 尺子 ①（零调用者、上游在 B/C）判 A；
//   上游 = 原版那批 `SingletonInitializableBehaviour<T>` 派生者（`ChallengeManager` / `ChatGlobalManager` …）
//   → 账 `未分类_其他` 第 77–79 行。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `SingletonInitializableBehaviour&lt;T&gt; : InitializableBehaviour`（`T : MonoBehaviour`）。
    /// 见文件头判据。⛔ 泛型 MonoBehaviour 不可挂到 GameObject 上（原版也同样只能当基类）。</summary>
    public abstract class SingletonInitializableBehaviour<T> : InitializableBehaviour where T : MonoBehaviour
    {
        static T instance;

        /// <summary>原版 `public static T Instance`。**懒找**：null 就 `FindObjectOfType`，仍 null 出 Error。
        /// <para>⚠️ 与本仓既有 `Shell/LiveOpsManager.cs.Instance`（纯 C# 静态单例）**不是一回事**，⛔ 别互替。</para></summary>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    // ⚠️ 原版这里是一条 `CustomDebug.LogWarning(<格式串>)`，正文未解（只有 `{0}` = typeof(T) 可见）。
                    Debug.LogWarning("[SingletonInitializableBehaviour<" + typeof(T).Name + ">] `Instance` 为 null ⇒ 现场找一个（原版同样会喊一声）");
                    // ⚠️ 语义对齐 `FindObjectOfType<T>()`（**只找激活对象**）⇒ 用 `FindFirstObjectByType<T>(Exclude)`。
                    instance = Object.FindFirstObjectByType<T>(FindObjectsInactive.Exclude);
                    if (instance == null)
                        Debug.LogError("[SingletonInitializableBehaviour<" + typeof(T).Name + ">] 找不到实例（原版这里 LogError）");
                }
                return instance;
            }
        }

        /// <summary>当前是否已经有实例（自检口；不触发懒找）。</summary>
        public static bool HasInstanceForTest { get { return instance != null; } }

        /// <summary>原版 `private void Awake()`：重复实例**自毁**。</summary>
        void Awake()
        {
            if (Instance != null && Instance != (T)(object)this) Destroy(this);
        }

        /// <summary>原版 `public override void OnInitializationComplete()`：`base` 之后把自己登记为单例。</summary>
        public override void OnInitializationComplete()
        {
            base.OnInitializationComplete();
            instance = (T)(object)this;
        }

        /// <summary>原版 `private void OnDestroy()`：**只有自己是登记的那个**才清空。
        /// 🔴 原版在 `== this` 支里**再 `Destroy(this)` 一次**（在 OnDestroy 里调 Destroy 是 no-op，但照抄）。</summary>
        void OnDestroy()
        {
            if (Instance != (T)(object)this) return;
            Destroy(this);          // 原版照抄（⚠️ 这一句在 OnDestroy 里没有实际效果，⛔ 别删、也别「修正」）
            instance = null;
        }

#if UNITY_EDITOR
        // ============================================================ 结构性自检钩子（B92_N6b）
        /// <summary>结构性自检（不依赖场景/资产）。返回**问题清单**，空 = 通过。
        /// <para>⚠️ 它**故意不调** `Instance`（那会触发 `FindObjectOfType` + 日志）。</para></summary>
        public static System.Collections.Generic.List<string> SelfCheck()
        {
            var errs = new System.Collections.Generic.List<string>();
            var t = typeof(SingletonInitializableBehaviour<>);
            if (t.BaseType != typeof(InitializableBehaviour))
                errs.Add("基类不是 `InitializableBehaviour`（原版是）");
            if (!t.IsAbstract) errs.Add("不是 abstract（原版是）");
            var g = t.GetGenericArguments()[0];
            if (!typeof(MonoBehaviour).IsAssignableFrom(g)) errs.Add("泛型约束不是 `T : MonoBehaviour`（原版是）");
            var inst = t.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (inst == null || inst.PropertyType != g) errs.Add("缺 `public static T Instance`（原版有）");
            var oic = t.GetMethod("OnInitializationComplete", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (oic == null || oic.DeclaringType != t) errs.Add("没覆写 `OnInitializationComplete()`（原版覆写了）");
            foreach (var name in new[] { "Awake", "OnDestroy" })
            {
                var mi = t.GetMethod(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                if (mi == null) errs.Add("缺方法 `" + name + "`（原版有）");
            }
            return errs;
        }
#endif
    }
}
