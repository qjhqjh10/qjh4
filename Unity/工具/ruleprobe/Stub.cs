// 极简 UnityEngine 桩 —— 只为让 RuleEngine 在 net8 下跑得起来（离屏探针用，不进工程）
// B28：比 b14/b24 那两份多一个 `JsonUtility`（`CardDatabase.Parse` 要它才能吃 cards_engine.json）。
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
}
