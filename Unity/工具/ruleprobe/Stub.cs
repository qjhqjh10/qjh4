// 极简 UnityEngine 桩 —— 只为让 RuleEngine 在 net8 下跑得起来（离屏探针用，不进工程）
// B28：比 b14/b24 那两份多一个 `JsonUtility`（`CardDatabase.Parse` 要它才能吃 cards_engine.json）。
using System;
using System.Text.Json;

namespace UnityEngine
{
    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
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
