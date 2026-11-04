// 极简 UnityEngine 桩 —— 只为让 RuleEngine 在 net8 下跑得起来（离屏探针用，不进工程）
// B28：比 b14/b24 那两份多一个 `JsonUtility`（`CardDatabase.Parse` 要它才能吃 cards_engine.json）。
using System;
using System.Text.Json;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object m) { }
        // 🔴 2026-11-05（`B143-W1` · 调度台追加 2）：**本行原来是空实现**（`{ }`）⇒ 探针里**所有**走
        //   `UnityEngine.Debug.LogWarning` 的判据都是**静默**的（实测撞上：`B141-CHOOSECARD-ROW` 的
        //   「`ta=60` 的 `abilityTargetCriteria` 行读不到」那条 warning 在 `play` 里一个字都看不见 ⇒
        //   「警告没了」不能当证据、「警告该响」也永远看不见）。现在真打到 **stderr**（不污染 stdout 的读数行）。
        //   ⚠️ 判据：本改动**不进结算** ⇒ `play check` 的结算指纹必须**一字不变**（`2f640db5…`）；
        //      变了 = 有别的副作用，立刻回滚。⚠️ `Log` / `LogError` 仍是空实现（本批只按裁定改这一个）。
        public static void LogWarning(object m) { Console.Error.WriteLine("[RuleEngine/WARN] " + (m == null ? "null" : m.ToString())); }
        public static void LogError(object m) { }
    }
    public static class Random
    {
        static System.Random _r = new System.Random(12345);
        public static int Range(int a, int b) { return _r.Next(a, b); }
        public static float Range(float a, float b) { return (float)(a + _r.NextDouble() * (b - a)); }
        public static float value { get { return (float)_r.NextDouble(); } }
    }
    public class TextAsset { public string text; }
    /// <summary>探针不走 `Resources.Load`（那条路要真 Unity）；留着只为让 `CardDatabase.Load` 编得过。</summary>
    public static class Resources
    {
        public static T Load<T>(string path) where T : class { return null; }
    }
    /// <summary>只覆盖 `FromJson&lt;T&gt;`（`CardDatabase.Parse` 用的那一处）。字段名逐字对齐 Unity 的口径。</summary>
    public static class JsonUtility
    {
        static readonly JsonSerializerOptions Opt = new JsonSerializerOptions
        {
            IncludeFields = true,
            PropertyNameCaseInsensitive = false,
        };
        public static T FromJson<T>(string json) { return JsonSerializer.Deserialize<T>(json, Opt); }
    }

    /// <summary>🆕 2026-10-25（`R11`）：为把 `Data/SimpleAI.cs` 编进探针（`ai` 模式）而加的两个形状。
    /// 它只有一个用处 —— 让 `SimpleAI` 的**静态构造**（`InstallAiScorer`）编得过；
    /// 探针**不跑** Unity 的启动回调，所以这个特性在探针里**不会被执行**。
    /// ⛔ 别往这里加「行为」：桩只提供**形状**，语义一律以真 Unity 为准。</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    /// <summary>同上（只为编得过）。取值照 Unity：`SubsystemRegistration = 4`。</summary>
    public enum RuntimeInitializeLoadType
    {
        AfterSceneLoad = 0,
        BeforeSceneLoad = 1,
        AfterAssembliesLoaded = 2,
        BeforeSplashScreen = 3,
        SubsystemRegistration = 4,
    }

    // 🔴 2026-10-27 撤回了这里的 `Vector2` / `Vector3` 两个桩（第八十六会话曾临时补过）：
    //    `GameplayVariables.VarsGlobal` 那两条展示位公式已改成返回 `Core/VarsGlobalTypes.cs` 的
    //    `PxOffset` / `WorldPoint` ⇒ `Core/*.cs` 不再需要任何向量类型。
    //    ⛔ 别再补回来 —— 桩里每多一个 Unity 值类型，都是在给「`Core` 悄悄引 Unity」开后门
    //    （那条架构断言 `RuleEngineTest.TestCoreLayerPurity` 就该在 Unity 侧当场红，而不是被桩盖过去）。
}
