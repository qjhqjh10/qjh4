// LiveOpsSharedData.cs —— **跨事件共享数据层**（原版 `SharedEventDataManager` / `SharedEventData` / `SharedDataContext`
//                         + `Newtonsoft.Json.Linq.JObject` 的最小替代品 `LiveOpsJson`）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：`资料/完全复刻/判据/日常与活动.md` §二 Q1 的**第二个结论**——
//   「`SetupEvents` 的第三个参数 `JObject sharedData` 是**跨事件共享的一份 JSON**，每个事件从它里面读公共配置
//     （不是各自 SO 里带）。我们这边**没有**共享数据块 ⇒ 必须新补一层。」
//   原版真身：`d:/2/tools/decomp_full/SharedEventDataManager__Initialize.c` / `…__Unpack.c` / `…__GetContext_object_.c`
//             + `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/SharedEventData.cs`（`[Serializable] { EnergyData energyData; }`）
//   消费点（原版）：`LiveOpsHandler.DeserializeSharedData` 读 `sharedData["NextRefresh"]`（`LiveOpsHandler.cs:2523`）。
//
// 🔴 **为什么必须现在一起定**：它牵动 `ILiveOpsHandler.SetupEvents(events, blockedEvents, **sharedData**)` 的**三参签名**
//   （`ILiveOpsHandler.cs:71`）。后补 = 改接口 = 5 扇窗的调用点全部返工。
//
// ⚠️ **与原版的如实差异**：
//   ① 原版 `JObject` 是 Newtonsoft 的；工程**没有** Newtonsoft（`Packages/manifest.json` 无
//      `com.unity.nuget.newtonsoft-json`，判据 → `Shell/SaveCenter.cs` 文件头同一句）⇒ 自带一个最小 JSON 对象
//      <see cref="LiveOpsJson"/>（**只支持** object/array/string/number/bool/null 六种，够用）。
//   ② 原版 `SharedEventDataManager.Initialize()` 靠 `GetComponentsInChildren&lt;SharedDataContext&gt;()`
//      （context 是挂在 prefab 上的组件）；我们没有那种 prefab ⇒ 显式注册（同 `LiveOpsManager.Register` 的口径）。
//   ③ 原版 `SharedEventData` 的字段类型是 `EnergyData`（能量配置：max / rechargeAmount / cooldown=300000ms）。
//      本地**没有那份数据**（判据 `判据/日常与活动.md` §三：C1-W7「能量 + `SharedEventDataManager.GetContext<T>()`」是 P2，休眠）
//      ⇒ 字段先放 `LiveOpsJson`（原样保留 JSON），**不做强类型 EnergyData**。⛔ 别照猜填 max/cooldown。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace CardPresentation
{
    // ============================================================ 最小 JSON 对象（替代 Newtonsoft 的 JObject）
    /// <summary>一个**最小 JSON 对象**：只做「读某个键 / 写某个键 / 序列化 / 解析」。
    /// <para>值的类型只可能是：`string` · `long`（整数）· `double`（小数）· `bool` · `null` ·
    /// <see cref="LiveOpsJson"/> · `List&lt;object&gt;`。</para>
    /// <para>⛔ **不许**拿它当通用序列化器（`JsonUtility` 才是工程的落盘口）；它只为 `sharedData` 这一块存在。</para></summary>
    public sealed class LiveOpsJson
    {
        readonly Dictionary<string, object> _v;
        readonly List<string> _order;      // 保留插入序（序列化时按它写，便于人读与逐字节比对）

        LiveOpsJson() { _v = new Dictionary<string, object>(); _order = new List<string>(); }

        public static LiveOpsJson NewObject() { return new LiveOpsJson(); }

        public int Count { get { return _v.Count; } }
        public IEnumerable<string> Keys { get { return _order; } }
        public bool Contains(string key) { return key != null && _v.ContainsKey(key); }

        /// <summary>读一个整数（原版 `JToken.Value&lt;long&gt;()`）。缺失 / 类型不对 ⇒ false。</summary>
        public bool TryGetLong(string key, out long value)
        {
            value = 0L;
            object o;
            if (key == null || !_v.TryGetValue(key, out o) || o == null) return false;
            if (o is long) { value = (long)o; return true; }
            if (o is double) { value = (long)(double)o; return true; }
            if (o is string)
            {
                long l; if (long.TryParse((string)o, NumberStyles.Integer, CultureInfo.InvariantCulture, out l)) { value = l; return true; }
                double d; if (double.TryParse((string)o, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) { value = (long)d; return true; }
            }
            return false;
        }

        public bool TryGetString(string key, out string value)
        {
            value = null;
            object o;
            if (key == null || !_v.TryGetValue(key, out o)) return false;
            value = o as string;
            return value != null;
        }

        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            object o;
            if (key == null || !_v.TryGetValue(key, out o)) return false;
            if (o is bool) { value = (bool)o; return true; }
            return false;
        }

        /// <summary>取一个子对象（原版 `JObject` 里再取 `JObject`）。</summary>
        public LiveOpsJson GetObject(string key)
        {
            object o;
            return (key != null && _v.TryGetValue(key, out o)) ? o as LiveOpsJson : null;
        }

        public void Set(string key, string value) { Put(key, value); }
        public void Set(string key, long value) { Put(key, value); }
        public void Set(string key, bool value) { Put(key, value); }
        public void Set(string key, LiveOpsJson value) { Put(key, value); }

        void Put(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) { Debug.LogError("[LiveOpsJson] 键是空的 ⇒ 拒绝写入"); return; }
            if (!_v.ContainsKey(key)) _order.Add(key);
            _v[key] = value;
        }

        // ── 序列化 ──────────────────────────────────────────────────────────────
        public string ToJson()
        {
            var sb = new StringBuilder();
            Write(sb, this);
            return sb.ToString();
        }

        static void Write(StringBuilder sb, object v)
        {
            if (v == null) { sb.Append("null"); return; }
            var obj = v as LiveOpsJson;
            if (obj != null)
            {
                sb.Append('{');
                bool first = true;
                for (int i = 0; i < obj._order.Count; i++)
                {
                    string k = obj._order[i];
                    if (!first) sb.Append(',');
                    first = false;
                    sb.Append(Quote(k)).Append(':');
                    Write(sb, obj._v[k]);
                }
                sb.Append('}');
                return;
            }
            var list = v as List<object>;
            if (list != null)
            {
                sb.Append('[');
                for (int i = 0; i < list.Count; i++) { if (i > 0) sb.Append(','); Write(sb, list[i]); }
                sb.Append(']');
                return;
            }
            if (v is string) { sb.Append(Quote((string)v)); return; }
            if (v is bool) { sb.Append(((bool)v) ? "true" : "false"); return; }
            if (v is long) { sb.Append(((long)v).ToString(CultureInfo.InvariantCulture)); return; }
            if (v is double) { sb.Append(((double)v).ToString("R", CultureInfo.InvariantCulture)); return; }
            sb.Append(Quote(v.ToString()));
        }

        static string Quote(string s)
        {
            var sb = new StringBuilder(s.Length + 2);
            sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
            return sb.ToString();
        }

        // ── 解析 ────────────────────────────────────────────────────────────────
        /// <summary>解析一段 JSON。**接受对象或数组**：数组会被包成 `{"items": [...]}`（原版的 handler 载荷
        /// 两种形态都有 —— `RawLiveOpsHandlerPersistence` 的 ctor 里就是 `JArray` / `JObject` 两条支）。
        /// 解析失败 ⇒ **null + LogError**（⛔ 不静默给空对象）。</summary>
        public static LiveOpsJson Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) { Debug.LogError("[LiveOpsJson] Parse：文本是空的"); return null; }
            int pos = 0;
            try
            {
                object v = ParseValue(text, ref pos);
                SkipWs(text, ref pos);
                if (pos != text.Length) throw new FormatException("尾部有多余内容 @" + pos);
                var obj = v as LiveOpsJson;
                if (obj != null) return obj;
                var list = v as List<object>;
                if (list != null)
                {
                    var wrap = NewObject();
                    wrap.Put("items", list);
                    return wrap;
                }
                throw new FormatException("顶层不是对象也不是数组");
            }
            catch (Exception e)
            {
                Debug.LogError("[LiveOpsJson] Parse 失败：" + e.Message + "（前 80 字：" + (text.Length > 80 ? text.Substring(0, 80) : text) + "）");
                return null;
            }
        }

        static object ParseValue(string s, ref int i)
        {
            SkipWs(s, ref i);
            if (i >= s.Length) throw new FormatException("意外结尾");
            char c = s[i];
            if (c == '{') return ParseObject(s, ref i);
            if (c == '[') return ParseArray(s, ref i);
            if (c == '"') return ParseString(s, ref i);
            if (c == 't') { Expect(s, ref i, "true"); return true; }
            if (c == 'f') { Expect(s, ref i, "false"); return false; }
            if (c == 'n') { Expect(s, ref i, "null"); return null; }
            return ParseNumber(s, ref i);
        }

        static LiveOpsJson ParseObject(string s, ref int i)
        {
            var obj = NewObject();
            i++;                                   // '{'
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return obj; }
            while (true)
            {
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != '"') throw new FormatException("对象键必须是字符串 @" + i);
                string k = ParseString(s, ref i);
                SkipWs(s, ref i);
                if (i >= s.Length || s[i] != ':') throw new FormatException("缺冒号 @" + i);
                i++;
                obj.Put(k, ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("对象未闭合");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == '}') { i++; return obj; }
                throw new FormatException("对象里缺逗号 @" + i);
            }
        }

        static List<object> ParseArray(string s, ref int i)
        {
            var list = new List<object>();
            i++;                                   // '['
            SkipWs(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return list; }
            while (true)
            {
                list.Add(ParseValue(s, ref i));
                SkipWs(s, ref i);
                if (i >= s.Length) throw new FormatException("数组未闭合");
                if (s[i] == ',') { i++; continue; }
                if (s[i] == ']') { i++; return list; }
                throw new FormatException("数组里缺逗号 @" + i);
            }
        }

        static string ParseString(string s, ref int i)
        {
            i++;                                   // '"'
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) throw new FormatException("转义未完成");
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("\\u 不足 4 位");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw new FormatException("未知转义 \\" + e);
                }
            }
            throw new FormatException("字符串未闭合");
        }

        static object ParseNumber(string s, ref int i)
        {
            int start = i;
            while (i < s.Length && ("+-0123456789.eE".IndexOf(s[i]) >= 0)) i++;
            string num = s.Substring(start, i - start);
            if (num.Length == 0) throw new FormatException("不是数字 @" + start);
            long l;
            if (num.IndexOf('.') < 0 && num.IndexOf('e') < 0 && num.IndexOf('E') < 0
                && long.TryParse(num, NumberStyles.Integer, CultureInfo.InvariantCulture, out l))
                return l;
            double d;
            if (double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return d;
            throw new FormatException("数字解析失败 '" + num + "'");
        }

        static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\r' || s[i] == '\n')) i++;
        }

        static void Expect(string s, ref int i, string lit)
        {
            if (i + lit.Length > s.Length || string.CompareOrdinal(s, i, lit, 0, lit.Length) != 0)
                throw new FormatException("期望 '" + lit + "' @" + i);
            i += lit.Length;
        }

        public override string ToString() { return ToJson(); }
    }

    // ============================================================ 共享载荷（原版 `SharedEventData`）
    /// <summary>原版 `SharedEventData`（`[Serializable] { EnergyData energyData; }`）。
    /// ⚠️ 差异 ③：`EnergyData` 本地无数据 ⇒ 用 <see cref="LiveOpsJson"/> 原样装（不猜 max/cooldown）。</summary>
    [Serializable]
    public sealed class SharedEventData
    {
        /// <summary>原版 `public EnergyData energyData`。</summary>
        public LiveOpsJson energyData;
    }

    // ============================================================ 一个共享块（原版 `SharedDataContext` 抽象体）
    /// <summary>原版 `SharedDataContext`（`SharedEventDataManager` 的子件）：每种共享数据一个 context，
    /// 负责把自己的那一段 JSON 解成强类型。**本批只有抽象骨架**（原版具体子类本地无数据 ⇒ 不建空的）。</summary>
    public abstract class LiveOpsSharedContext
    {
        /// <summary>这个 context 认得的数据类型（用作 `SharedContexts` 的键）。</summary>
        public abstract Type SharedType { get; }
        /// <summary>原版 `SharedDataContext.Unpack(payload)`（`SharedEventDataManager__Unpack.c` 里 vtable +0x178 那一跳）。</summary>
        public abstract void Unpack(LiveOpsJson payload);
    }

    // ============================================================ 共享数据管理（原版 `SharedEventDataManager`）
    /// <summary>跨事件共享数据的**唯一入口**。原版是 MonoBehaviour 单例（`Initialize()` 从子件收集 context）；
    /// 我们同样保留 `Initialize()` 这个名字与「收集」语义，只是收集源换成显式注册（差异 ②）。</summary>
    public static class LiveOpsSharedDataManager
    {
        /// <summary>原版 `Dictionary&lt;Type, SharedDataContext&gt; SharedContexts`（静态字段）。
        /// 原版由 `GetComponentsInChildren&lt;SharedDataContext&gt;()` + `ToDictionary` 建（`SharedEventDataManager__Initialize.c`）。</summary>
        static readonly Dictionary<Type, LiveOpsSharedContext> SharedContexts = new Dictionary<Type, LiveOpsSharedContext>();

        /// <summary>最近一次 `Unpack` 的载荷（原版写到 `this + 0x28`）。**这是共享块的唯一正本**。</summary>
        public static SharedEventData Persistence { get; private set; }

        /// <summary>原版 `Initialize()`（`:148`）：收集 context → 建成 `Type → context` 的字典。
        /// ⚠️ 差异 ②：没有 `GetComponentsInChildren` ⇒ 这里接收一个显式列表（传 null = 只清空重来）。</summary>
        public static void Initialize(IEnumerable<LiveOpsSharedContext> contexts)
        {
            SharedContexts.Clear();
            if (contexts != null)
            {
                foreach (var c in contexts)
                {
                    if (c == null || c.SharedType == null) continue;
                    SharedContexts[c.SharedType] = c;
                }
            }
            Debug.Log("[LiveOpsSharedData] Initialize：登记了 " + SharedContexts.Count + " 个共享 context"
                    + (SharedContexts.Count == 0 ? "（原版靠 prefab 子件收集；本地没有那种 prefab ⇒ 0 是当前真实状态）" : ""));
        }

        /// <summary>原版 `public T GetContext&lt;T&gt;()`（`SharedEventDataManager__GetContext_object_.c`）：
        /// `SharedContexts[typeof(T)]` 之后 `castclass T`（取不到 ⇒ null）。</summary>
        public static T GetContext<T>() where T : class
        {
            LiveOpsSharedContext c;
            if (SharedContexts.TryGetValue(typeof(T), out c)) return c as T;
            Debug.LogWarning("[LiveOpsSharedData] GetContext<" + typeof(T).Name + ">()：**没有登记**这个 context"
                           + "（原版同样是 null，但它至少有 prefab 子件；我们是显式注册表为空）");
            return null;
        }

        /// <summary>原版 `Unpack(SharedEventData persistence)`（`:49`，**真身** `SharedEventDataManager__Unpack.c`）：
        /// <code>
        /// this.persistence = persistence;                  // 字段 +0x28
        /// if (SharedContexts == null) return;
        /// var ctx = GetContext&lt;&lt;EnergyContext&gt;&gt;();      // ← 具体类型读不出（vtable 泛型实例）
        /// if (ctx == null) return;
        /// foreach (var item in persistence.&lt;某列表&gt;) ctx.Unpack(item);   // vtable +0x178
        /// </code>
        /// ⚠️ 「哪个 context、遍历哪个列表」反编译读不出 ⇒ 我们按**登记表全量分发**：
        /// 每个 context 都拿到整份 `energyData`（`SharedContexts` 为空时只出声，不做别的）。
        /// 这是**有意差异**（账 row 699 记 B）。</summary>
        public static void Unpack(SharedEventData persistence)
        {
            Persistence = persistence;
            if (SharedContexts.Count == 0)
            {
                Debug.LogWarning("[LiveOpsSharedData] Unpack：共享 context 表是空的 ⇒ 没有任何东西被解（原版至少有一个 `EnergyContext`）");
                return;
            }
            var payload = persistence == null ? null : persistence.energyData;
            foreach (var kv in SharedContexts)
            {
                var c = kv.Value;
                if (c == null) continue;
                try { c.Unpack(payload); }
                catch (Exception e) { Debug.LogError("[LiveOpsSharedData] context '" + (c.SharedType == null ? "?" : c.SharedType.Name) + "' 的 Unpack 抛异常：" + e); }
            }
        }

        public static int ContextCountForTest { get { return SharedContexts.Count; } }

        public static void ResetForTest()
        {
            SharedContexts.Clear();
            Persistence = null;
        }
    }

    // ============================================================ 原版 `RankedSharedData`（`GameModes` +0x80 / +0x88）
    /// <summary>原版 `RankedSharedData`（`RankedSharedData.cs`，字段逐个抄；偏移见 `dump.cs:62900`）：
    /// `previousLeaderboard(+0x10)` · `lastPlayedSeason(+0x18)` · `lastPlayedEvent(+0x20)` ·
    /// `HighestPosition(+0x28)` · `SeasonPositions(+0x30)` · `isHuman(+0x38)` ·
    /// `highestScoreSeason(+0x3C)` · `lossStreak(+0x40)`。
    /// <para>🔴 **构造默认值是原版的语义，别删**（`RankedSharedData__ctor.c`）：
    /// `HighestPosition = new LeaguePositionRecord { Position = int.MaxValue }`（= 「还没有名次」）、
    /// `SeasonPositions = new Dictionary&lt;int,int&gt;()`。</para>
    /// <para>⚠️ 差异：原版由 `JsonWrapper.DeserializeObject&lt;RankedSharedData&gt;`（Newtonsoft）反序列化；
    /// 我们**没有 Newtonsoft**，而 `JsonUtility` **不支持 `Dictionary`** ⇒ 本类自带 <see cref="FromJson"/>，
    /// 逐格从 <see cref="LiveOpsJson"/> 读数（**JSON 形状与原版逐字一致**，⛔ 没有把字段改成 List）。</para></summary>
    [Serializable]
    public class RankedSharedData
    {
        /// <summary>原版 `previousLeaderboard`（string，+0x10）。</summary>
        public string previousLeaderboard;
        /// <summary>原版 `lastPlayedSeason`（int，+0x18）。</summary>
        public int lastPlayedSeason;
        /// <summary>原版 `lastPlayedEvent`（string，+0x20）。</summary>
        public string lastPlayedEvent;
        /// <summary>原版 `HighestPosition`（`LeaguePositionRecord`，+0x28）。默认 `Position = int.MaxValue`。</summary>
        public LeaguePositionRecord HighestPosition;
        /// <summary>原版 `SeasonPositions`（`Dictionary&lt;int,int&gt;`，+0x30）。JSON 里是 `{"<季>": 分}` 的对象。</summary>
        public Dictionary<int, int> SeasonPositions;
        /// <summary>原版 `isHuman`（bool，+0x38）：由 `GameModes.SetIsHuman()` 置 true。</summary>
        public bool isHuman;
        /// <summary>原版 `highestScoreSeason`（int，+0x3C）。</summary>
        public int highestScoreSeason;
        /// <summary>原版 `lossStreak`（int，+0x40）：由 `GameModes.SetLossStreak(int)` 写。</summary>
        public int lossStreak;

        /// <summary>原版 ctor（`RankedSharedData__ctor.c`）：两格默认值**必须**在这里给。</summary>
        public RankedSharedData()
        {
            HighestPosition = new LeaguePositionRecord { Position = int.MaxValue };
            SeasonPositions = new Dictionary<int, int>();
        }

        /// <summary>原版 `JsonWrapper.DeserializeObject&lt;RankedSharedData&gt;(token)` 的等价物。
        /// <para>`j == null` ⇒ **返回 null**（原版 `DeserializeObject(null)` 也为 null，调用方据此走回落链）。</para></summary>
        public static RankedSharedData FromJson(LiveOpsJson j)
        {
            if (j == null) return null;
            var d = new RankedSharedData();                 // 先拿 ctor 的默认值（原版同样先跑 ctor 再覆盖）
            string s; long l; bool b;

            if (j.TryGetString("previousLeaderboard", out s)) d.previousLeaderboard = s;
            if (j.TryGetLong("lastPlayedSeason", out l)) d.lastPlayedSeason = (int)l;
            if (j.TryGetString("lastPlayedEvent", out s)) d.lastPlayedEvent = s;
            if (j.TryGetLong("highestScoreSeason", out l)) d.highestScoreSeason = (int)l;
            if (j.TryGetLong("lossStreak", out l)) d.lossStreak = (int)l;
            // `isHuman`：Newtonsoft 会把 1/0 也转成 bool ⇒ 两档都读。
            if (j.TryGetBool("isHuman", out b)) d.isHuman = b;
            else if (j.TryGetLong("isHuman", out l)) d.isHuman = l != 0;

            var pos = j.GetObject("HighestPosition");
            if (pos != null)
            {
                var rec = new LeaguePositionRecord { Position = int.MaxValue };
                if (pos.TryGetLong("Position", out l)) rec.Position = (int)l;
                if (pos.TryGetLong("Rating", out l)) rec.Rating = (int)l;
                if (pos.TryGetLong("Season", out l)) rec.Season = (int)l;
                d.HighestPosition = rec;
            }

            var seasons = j.GetObject("SeasonPositions");
            if (seasons != null)
            {
                foreach (var key in seasons.Keys)
                {
                    int season;
                    if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out season)) continue;
                    if (!seasons.TryGetLong(key, out l)) continue;
                    d.SeasonPositions[season] = (int)l;
                }
            }
            return d;
        }
    }

    /// <summary>原版 `LeaguePositionRecord`（`LeaguePositionRecord.cs`）：`Position` / `Rating` / `Season` 三个 int。
    /// <para>🔴 默认 `Position = int.MaxValue` 是 `RankedSharedData` 的 ctor 给的（不是本类的）—— 本类保持全 0。</para></summary>
    [Serializable]
    public class LeaguePositionRecord
    {
        public int Position;
        public int Rating;
        public int Season;
    }

    /// <summary>原版 `ResultValues`（`ResultValues.cs`，偏移 `Won +0x10 / Loss +0x14 / Draw +0x18`）。
    /// ⚠️ 字段名是**大写开头**的（原版就是这样），JSON 键也就是 `Won`/`Loss`/`Draw`。</summary>
    [Serializable]
    public class ResultValues
    {
        public int Won;
        public int Loss;
        public int Draw;
    }

    /// <summary>原版 `ResultCounterSharedData`（`dump.cs:62920`）：`ResultCounter` = `Dictionary&lt;string, ResultValues&gt;`
    /// （把「模式 → 胜负平计数」存成一张表）。
    /// <para>⚠️ 差异同 `RankedSharedData`：没有 Newtonsoft、`JsonUtility` 不吃 `Dictionary` ⇒ 自带 <see cref="FromJson"/>
    /// （JSON 形状逐字照原版：`{"ResultCounter":{"Classic":{"Won":1,"Loss":2,"Draw":0}}}`）。</para></summary>
    [Serializable]
    public class ResultCounterSharedData
    {
        /// <summary>原版 `ResultCounter`（`Dictionary&lt;string, ResultValues&gt;`）。</summary>
        public Dictionary<string, ResultValues> ResultCounter;

        public ResultCounterSharedData()
        {
            ResultCounter = new Dictionary<string, ResultValues>();
        }

        /// <summary>原版 `ResultCounterSharedData.GetValuesForMode(string)`（`ResultCounterSharedData__GetValuesForMode.c`）：
        /// **取不到就新建一条并返回它**（不是返回 null）；`ResultCounter` 为 null ⇒ 原版返回 null（随后 NRE）。
        /// <para>⚠️ 差异：`playMode` 为 null 时原版 `Dictionary` 抛 `ArgumentNullException`，我们返回 null 并出声
        /// （调用方 <see cref="CardPresentation.GameModesHandler.SetResultCount"/> 会 LogError）。</para></summary>
        public ResultValues GetValuesForMode(string playMode)
        {
            if (ResultCounter == null) return null;
            if (playMode == null)
            {
                Debug.LogError("[LiveOps] ResultCounterSharedData.GetValuesForMode：playMode 是 null"
                             + "（原版 Dictionary 会抛 ArgumentNullException）⇒ 返回 null");
                return null;
            }
            ResultValues v;
            if (ResultCounter.TryGetValue(playMode, out v)) return v;
            v = new ResultValues();
            ResultCounter[playMode] = v;
            return v;
        }

        /// <summary>原版 `JsonWrapper.DeserializeObject&lt;ResultCounterSharedData&gt;(整份 sharedData)` 的等价物。
        /// 🔴 读的是**顶层** `ResultCounter` 那一块（原版就是拿整份 sharedData 去反序列化，不是取子键）。
        /// <para>`j == null` ⇒ 返回 null；`ResultCounter` 缺失 ⇒ 空表（原版同样得到空表）。</para></summary>
        public static ResultCounterSharedData FromJson(LiveOpsJson j)
        {
            if (j == null) return null;
            var d = new ResultCounterSharedData();
            var block = j.GetObject("ResultCounter");
            if (block == null) return d;
            foreach (var mode in block.Keys)
            {
                var cell = block.GetObject(mode);
                if (cell == null)
                {
                    Debug.LogError("[LiveOps] ResultCounterSharedData.FromJson：模式 '" + mode
                                 + "' 的值不是对象（原版 Newtonsoft 会抛）⇒ 跳过这一格");
                    continue;
                }
                var rv = new ResultValues();
                long l;
                if (cell.TryGetLong("Won", out l)) rv.Won = (int)l;
                if (cell.TryGetLong("Loss", out l)) rv.Loss = (int)l;
                if (cell.TryGetLong("Draw", out l)) rv.Draw = (int)l;
                d.ResultCounter[mode] = rv;
            }
            return d;
        }
    }
}
