// GameModesHandler.cs —— **游戏模式 handler**（原版 `GameModes : LiveOpsHandler<IPlayEvent>`）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：`资料/完全复刻/判据/B88_R10_基础设施58条施工单.md` §三（`GameModes` 12 条：9 本地 / 3 P3）。
//   原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/GameModes.cs`（**控制流可读，逐方法照抄**）
//             ＋ `d:/2/tools/decomp_full/GameModes__*.c`（常量/分支的最终判据：`SetResultCount` 的 `0x14/0x1e`、
//               `DeserializeSharedData` 的三个键字面量、`CombatFinished` 末尾那句清空）
//   账行：`账/其他_基础设施.tsv` 第 215–226 行（`方法` 列 = 本节 12 个方法）。
//
// 🔴 **不另起第二套 liveops**：本类继承 `Shell/LiveOpsHandler.cs` 的 `LiveOpsHandler`（= 原版 `LiveOpsHandler<TEvent>`
//   的非泛型等价物），数据类 `RankedSharedData` / `ResultCounterSharedData` 住在 `Shell/LiveOpsSharedData.cs`。
//
// ⚠️ **与原版的如实差异（都不是「已经一样」）**：
//   ① 原版 `IPlayEvent` 接口（`EventPlayMode` / `MatchFinished` …）本机**还没建**：`PlayEvent<TSave,TData>` 上只有
//      **同名公开成员**、没有接口声明。本类的 `GetActiveEvent(PlayModes)` / `CombatFinished` 要经接口调它 ⇒
//      这两处走**成员反射**（读同一格，带缓存）。等 `Shell/PlayEvent.cs` 补上 `IPlayEvent` 后应换回强类型调用。
//   ② 原版 `CurrentPlayingEvent` / `RankedSharedData` … 是 `IPlayEvent` / 具体数据类；我们的 `ILiveOps` 上**没有**
//      `Priority` 属性（只有 `GetEventPriorityForMainMenu()`，读的是同一格 `data.priority`）⇒ 排序键用后者。
//   ③ 原版 `DeserializeSharedData` 用 Newtonsoft 的 `JsonWrapper.DeserializeObject<T>`；我们没 Newtonsoft
//      ⇒ 两个数据类自带 `FromJson(LiveOpsJson)`（JSON 形状逐字照原版，见 `LiveOpsSharedData.cs` 的注释）。
//   ④ 原版 `GameModesConfig`（`ConfigManager.GetConfig<GameModesConfig>()` 的 `mainRankedType`）是**远端**配置；
//      本地没有那份资产 ⇒ 由 <see cref="GameModesConfigSource"/> 注入（默认 null = 本地确实没有）。
//   ⑤ `InitializeHandler` / `b__30_0` / `n__0` 是**服务器回包链（P3）** ⇒ 只落地本地那半，服务器半段**留桩 + 出声**。
//   ⑥ 原版 `CombatFinished` 的回调包里还会写 `onlineMatchResult.PlayEvent`（`+0x28`）；我们的 `OnlineMatchResult`
//      是 P3 的最小子集（`success/error/newRating`），**没有那一格** ⇒ 这一跳没搬（如实记，不假装等价）。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `ConfigManager.GetConfig&lt;GameModesConfig&gt;()` 里本批**用得到的那一格**（远端 TitleData）。
    /// <para>🔶 自己设计（非原版）：原版是 `IConfigData` 资产 + `TypeReference mainRankedType`（默认 `typeof(IRankedEvent)`）；
    /// 本地**没有那份配置资产**（远端下发，判据 §三·3.1 行 4）⇒ 只留一个注入点。
    /// `null` = 「本地确实拿不到」—— `GetActiveEvent(Type)` 走到那一支时会**出声后返回 null**（原版那里是 NRE）。
    /// 执行代理 B89-E11 2026-10-28（简报授权：判据在远端）。</para></summary>
    public static class GameModesConfigSource
    {
        /// <summary>原版 `GameModesConfig.mainRankedType.Type`（`null` = 拿不到）。</summary>
        public static Type MainRankedType;

        /// <summary>自检收尾用。</summary>
        public static void ResetForTest() { MainRankedType = null; }
    }

    /// <summary>原版 `GameModes`（`GameModes.cs:13`，`HandlerKey = "GameModes"`）：管「对战类活动」的那一个 handler
    /// —— 当前正在打的活动 / 按模式与类型挑活动 / 共享数据里的排位与战绩计数。
    /// <para>字段偏移照反编译：`CurrentPlayingEvent`(+0x78) · `RankedSharedData`(+0x80) ·
    /// `FastSharedData`(+0x88) · `ResultCounterData`(+0x90)。</para></summary>
    public sealed class GameModesHandler : LiveOpsHandler
    {
        /// <summary>原版 `public override string HandlerKey =&gt; "GameModes"`（`.c` 里是那个字符串常量）。</summary>
        public override string HandlerKey { get { return "GameModes"; } }

        // ============================================================ 四个字段（+0x78 / +0x80 / +0x88 / +0x90）
        /// <summary>原版 `CurrentPlayingEvent`（`IPlayEvent`，+0x78，protected set）。
        /// 写口只有两个：<see cref="SetAsCurrentlyPlayingEvent"/>、<see cref="CombatFinished"/>（末尾清空）。</summary>
        public ILiveOps CurrentPlayingEvent { get; private set; }

        /// <summary>原版 `RankedSharedData`（+0x80，private set）：共享数据里 `Classic`→`Ranked`→`new` 那一条回落链的产物。</summary>
        public RankedSharedData RankedSharedData { get; private set; }

        /// <summary>原版 `FastSharedData`（+0x88，private set）：只读 `FastMode` 一键，**没有 Ranked 级回落**。</summary>
        public RankedSharedData FastSharedData { get; private set; }

        /// <summary>原版 `ResultCounterData`（+0x90，private set）：**整份** sharedData 反序列化出来的战绩计数表。</summary>
        public ResultCounterSharedData ResultCounterData { get; private set; }

        /// <summary>原版 `LossStreak =&gt; RankedSharedData.lossStreak`（`:362`）—— 同族里 <see cref="SetLossStreak"/> 的读口；
        /// 消费方 = `IPlayEvent.CurrentGamesLost`（`IPlayEvent.cs`）。
        /// <para>⚠️ 差异：`RankedSharedData` 为 null 时原版 NRE，我们出声 + 返回 0（同 `SetLossStreak` 的口径）。</para></summary>
        public int LossStreak
        {
            get
            {
                var d = RankedSharedData;
                if (d != null) return d.lossStreak;
                Debug.LogError("[GameModes] LossStreak：`RankedSharedData` 是 null（原版这里 NRE）⇒ 返回 0");
                return 0;
            }
        }

        /// <summary>原版 `IsHuman =&gt; RankedSharedData.isHuman`（`:377`）—— <see cref="SetIsHuman"/> 的读口。</summary>
        public bool IsHuman
        {
            get
            {
                var d = RankedSharedData;
                if (d != null) return d.isHuman;
                Debug.LogError("[GameModes] IsHuman：`RankedSharedData` 是 null（原版这里 NRE）⇒ 返回 false");
                return false;
            }
        }

        // ============================================================ 原版 `IPlayEvent` 的两格（见文件头差异 ①）
        static readonly Dictionary<Type, PropertyInfo> _playModeProps = new Dictionary<Type, PropertyInfo>();
        static readonly Dictionary<Type, MethodInfo> _matchFinishedMethods = new Dictionary<Type, MethodInfo>();

        static readonly Type[] MatchFinishedParams = new Type[] { typeof(RankedMatchResult), typeof(Action<OnlineMatchResult>) };

        /// <summary>读事件上的 `EventPlayMode`（原版 `IPlayEvent.EventPlayMode`，槽 0）。
        /// 反射的原因见文件头差异 ①；每个**类型**只查一次（查不到也缓存，避免每条事件刷一次日志）。</summary>
        static bool TryGetPlayMode(ILiveOps e, out PlayModes mode)
        {
            mode = default(PlayModes);
            if (e == null) return false;
            var t = e.GetType();
            PropertyInfo p;
            if (!_playModeProps.TryGetValue(t, out p))
            {
                p = t.GetProperty("EventPlayMode", BindingFlags.Public | BindingFlags.Instance);
                if (p != null && p.PropertyType != typeof(PlayModes)) p = null;
                _playModeProps[t] = p;
                if (p == null)
                    Debug.LogWarning("[GameModes] 事件类型 '" + t.Name + "' 没有公开的 `EventPlayMode` 属性"
                                   + "（原版是 `IPlayEvent` 的接口成员）⇒ 它不参与 `GetActiveEvent(PlayModes)` 的筛选");
            }
            if (p == null) return false;
            var v = p.GetValue(e, null);          // 原版同格：`IPlayEvent.get_EventPlayMode()`
            if (!(v is PlayModes)) return false;
            mode = (PlayModes)v;
            return true;
        }

        /// <summary>调事件上的 `MatchFinished(RankedMatchResult, Action&lt;OnlineMatchResult&gt;)`
        /// （原版 `IPlayEvent.MatchFinished`）。反射的原因见文件头差异 ①。</summary>
        static bool TryFinishMatch(ILiveOps e, RankedMatchResult result, Action<OnlineMatchResult> callback)
        {
            if (e == null) return false;
            var t = e.GetType();
            MethodInfo m;
            if (!_matchFinishedMethods.TryGetValue(t, out m))
            {
                m = t.GetMethod("MatchFinished", BindingFlags.Public | BindingFlags.Instance, null, MatchFinishedParams, null);
                _matchFinishedMethods[t] = m;
            }
            if (m == null)
            {
                Debug.LogError("[GameModes] CombatFinished：事件类型 '" + t.Name + "' 上没有 "
                             + "`MatchFinished(RankedMatchResult, Action<OnlineMatchResult>)`（原版是 `IPlayEvent` 的接口成员）"
                             + "⇒ **这一局的结算没有被分派出去**");
                return false;
            }
            m.Invoke(e, new object[] { result, callback });
            return true;
        }

        // ============================================================ 账 215 · CombatFinished（原版 `:526`）
        /// <summary>原版 `CombatFinished(RankedMatchResult result, Action&lt;OnlineMatchResult&gt; callback = null)`。
        /// 真身（`GameModes__CombatFinished.c` 逐句）：
        /// <code>
        /// var currentEvent = CurrentPlayingEvent;            // +0x78 读进闭包
        /// if (currentEvent == null) return;
        /// var wrapped = (OnlineMatchResult r) =&gt; { r.PlayEvent = currentEvent; callback?.Invoke(r); };
        /// currentEvent.MatchFinished(result, wrapped);
        /// CurrentPlayingEvent = null;                        // ★ 末尾**清空**（`.c: param_1+0x78 = 0`）
        /// </code>
        /// 🔴 **末尾那句清空是原版的**，不是我们加的 —— 所以同一局**只会分派一次**（第二次 `CurrentPlayingEvent == null` 直接 return）。
        /// <para>⚠️ 差异 ⑥：`r.PlayEvent = currentEvent` 那一格我们的 `OnlineMatchResult` 没有（P3 最小子集）⇒ 未搬。</para></summary>
        public void CombatFinished(RankedMatchResult result, Action<OnlineMatchResult> callback = null)
        {
            var currentEvent = CurrentPlayingEvent;
            if (currentEvent == null) return;
            TryFinishMatch(currentEvent, result, callback);
            CurrentPlayingEvent = null;                 // ★ 原版末尾清空（⛔ 别挪到 if 里）
        }

        // ============================================================ 账 216 · DeserializeSharedData（原版 `:458`）
        /// <summary>原版 `protected override void DeserializeSharedData(JObject sharedData)`（真身
        /// `GameModes__DeserializeSharedData.c`，键字面量 `DAT_184233dd8`=`"Classic"`、`DAT_1842a6c38`=`"Ranked"`、
        /// `DAT_1842be6c0`=`"FastMode"`）：
        /// <code>
        /// base.DeserializeSharedData(sharedData);                       // 基类：读 `NextRefresh` → 排刷新
        /// RankedSharedData = Deserialize&lt;RankedSharedData&gt;(sharedData["Classic"]) ??   // ① 先 Classic
        ///                    Deserialize&lt;RankedSharedData&gt;(sharedData["Ranked"])  ??   // ② 再回落 Ranked
        ///                    new RankedSharedData();                                  // ③ 最后 new
        /// FastSharedData   = Deserialize&lt;RankedSharedData&gt;(sharedData["FastMode"]) ??
        ///                    new RankedSharedData();                                  // ★ 没有 Ranked 级回落
        /// ResultCounterData = Deserialize&lt;ResultCounterSharedData&gt;(sharedData) ??    // ★ 拿**整份**反序列化
        ///                     new ResultCounterSharedData();
        /// </code>
        /// 🔴 **两条最容易做错的**：① 回落链**只对 Ranked 生效**；② 第三条取的是**整份** `sharedData`（不是子键）。
        /// <para>⚠️ 差异（同族口径）：原版 `sharedData == null` 直接 **NRE**；我们出声后返回（坏数据不该让游戏崩）。</para></summary>
        protected override void DeserializeSharedData(LiveOpsJson sharedData)
        {
            // 基类那一跳先走（原版第一句就是它；读的是**字段** `shared`，不是形参 —— 见基类注释）
            base.DeserializeSharedData(sharedData);

            if (sharedData == null)
            {
                Debug.LogError("[GameModes] DeserializeSharedData：sharedData 是 null"
                             + "（原版这一跳 NRE）⇒ 三个数据类都不写");
                return;
            }

            // ① Classic → ② Ranked → ③ new（回落链**只对 Ranked 生效**）
            var ranked = RankedSharedData.FromJson(sharedData.GetObject("Classic"));
            if (ranked == null) ranked = RankedSharedData.FromJson(sharedData.GetObject("Ranked"));
            if (ranked == null) ranked = new RankedSharedData();
            RankedSharedData = ranked;                                      // +0x80

            // FastMode：**没有** Ranked 级回落
            var fast = RankedSharedData.FromJson(sharedData.GetObject("FastMode"));
            if (fast == null) fast = new RankedSharedData();
            FastSharedData = fast;                                          // +0x88

            // 第三条：**整份** sharedData（不是取子键）
            var counter = ResultCounterSharedData.FromJson(sharedData);
            if (counter == null) counter = new ResultCounterSharedData();
            ResultCounterData = counter;                                    // +0x90
        }

        // ============================================================ 账 217 · GetActiveEvent(PlayModes)（原版 `:596`）
        /// <summary>原版 `GetActiveEvent(PlayModes playMode)`（`GameModes__GetActiveEvent.c` 逐句）：
        /// <code>
        /// GetEvents()
        ///   .Where(g =&gt; g.EventPlayMode == playMode)     // 槽 0 = IPlayEvent.get_EventPlayMode
        ///   .Where(g =&gt; g.EventState == 2)               // 槽 23 = get_EventState，2 = LiveOpState.Active
        ///   .OrderByDescending(g =&gt; g.Priority)          // 槽 21 = get_Priority（= data.priority）
        ///   .FirstOrDefault();
        /// </code>
        /// 🔴 三道闸缺一不可：**模式对** ∧ **正在生效** ∧ **优先级最高**。没有 ⇒ **null**（不是抛）。
        /// <para>⚠️ 差异 ②：我们的 `ILiveOps` 上没有 `Priority` 属性 ⇒ 排序键走 `GetEventPriorityForMainMenu()`
        /// （同一格 `data.priority`，判据见 `LiveOpsEvent.cs` 那条注释）。⚠️ 差异 ①：`EventPlayMode` 走反射。</para></summary>
        public ILiveOps GetActiveEvent(PlayModes playMode)
        {
            return GetEvents()
                .Where(g =>
                {
                    PlayModes m;
                    return g != null && TryGetPlayMode(g, out m) && m == playMode;
                })
                .Where(g => g.EventState == LiveOpState.Active)
                .OrderByDescending(g => g.GetEventPriorityForMainMenu())
                .FirstOrDefault();
        }

        // ============================================================ 账 218 · GetActiveEvent(Type)（原版 `:661`）
        /// <summary>原版 `GetActiveEvent(Type gameModeType)`。真身（`GameModes__GetActiveEvent.c`）：
        /// <code>
        /// var config = ConfigManager.GetConfig&lt;GameModesConfig&gt;();
        /// if (gameModeType == typeof(IRankedEvent)) {
        ///     if (config == null || config.mainRankedType == null) return null;     // 原版：goto → NRE
        ///     var t = config.mainRankedType.Type;
        ///     if (t != null &amp;&amp; t != typeof(IRankedEvent)) {
        ///         var r = GetActiveEvent(t);                                        // ★ 递归
        ///         if (r != null) return r;
        ///     }
        /// }
        /// return GetEvents()
        ///   .Where(g =&gt; ReflectionHelper.Is(g, gameModeType))     // = 运行时类型/接口匹配
        ///   .Where(g =&gt; g.EventState == LiveOpState.Active)
        ///   .OrderByDescending(g =&gt; g.Priority)
        ///   .FirstOrDefault();
        /// </code>
        /// 🔴 那个 `typeof(IRankedEvent)` 的特殊支**只在问排位接口时进**；问别的类型时直接走 LINQ。
        /// <para>⚠️ 差异 ④：本地没有 `IRankedEvent`（接口未建）也没有远端 `GameModesConfig`
        /// ⇒ 入口由 <see cref="RankedEventInterfaceType"/> 注入（默认 null = 这一支**进不去**，
        /// 与「本机没有任何调用方能传进那个类型」等价）；`mainRankedType` 由 <see cref="GameModesConfigSource"/> 注入。</para></summary>
        public ILiveOps GetActiveEvent(Type gameModeType)
        {
            if (gameModeType == null)
            {
                Debug.LogError("[GameModes] GetActiveEvent(Type)：gameModeType 是 null（原版这一跳 NRE）⇒ 返回 null");
                return null;
            }

            var rankedInterface = RankedEventInterfaceType;
            if (rankedInterface != null && gameModeType == rankedInterface)
            {
                var mainRankedType = GameModesConfigSource.MainRankedType;
                if (mainRankedType == null)
                {
                    // 原版：`config == null || config.mainRankedType == null` ⇒ 直接返回 null（NRE），**不**走 LINQ
                    Debug.LogError("[GameModes] GetActiveEvent：问的是排位接口，但远端 `GameModesConfig.mainRankedType`"
                                 + " 本地拿不到（原版这一支直接返回 null / NRE）⇒ 返回 null");
                    return null;
                }
                if (mainRankedType != rankedInterface)
                {
                    var nested = GetActiveEvent(mainRankedType);
                    if (nested != null) return nested;
                }
                // mainRankedType == 排位接口本身 ⇒ 原版同样落到下面那条 LINQ（不做递归）
            }

            return GetEvents()
                .Where(g => g != null && gameModeType.IsInstanceOfType(g))     // 原版 `ReflectionHelper.Is(g, t)`
                .Where(g => g.EventState == LiveOpState.Active)
                .OrderByDescending(g => g.GetEventPriorityForMainMenu())
                .FirstOrDefault();
        }

        // ============================================================ 账 219 · GetActiveEvent<T>()（原版 `:562`）
        /// <summary>原版 `public T GetActiveEvent&lt;T&gt;() where T : IPlayEvent`：
        /// `GetActiveEvent(typeof(T))` 之后强转（转不成 ⇒ 原版抛；我们返回 null）。
        /// <para>约束写成 `class, ILiveOps`：原版是接口 `T : IPlayEvent` —— 我们的等价接口未建（差异 ①）⇒ 用 `ILiveOps`。</para></summary>
        public T GetActiveEvent<T>() where T : class, ILiveOps
        {
            var e = GetActiveEvent(typeof(T));
            if (e == null) return null;
            var typed = e as T;
            if (typed == null)
                Debug.LogError("[GameModes] GetActiveEvent<" + typeof(T).Name + ">()：取到的事件是 '"
                             + e.GetType().Name + "'，转不成目标类型（原版这一跳抛）⇒ 返回 null");
            return typed;
        }

        // ============================================================ 账 221 · SetAsCurrentlyPlayingEvent（原版 `:554`）
        /// <summary>原版 `SetAsCurrentlyPlayingEvent(IPlayEvent playEvent)`：**纯字段写** `+0x78 = playEvent`
        /// （`GameModes__SetAsCurrentlyPlayingEvent.c` 全文只有一句 store）。</summary>
        public void SetAsCurrentlyPlayingEvent(ILiveOps playEvent)
        {
            CurrentPlayingEvent = playEvent;
        }

        // ============================================================ 账 222 · SetIsHuman（原版 `:418`）
        /// <summary>原版 `SetIsHuman()`：`RankedSharedData.isHuman(+0x38) = true`
        /// （`GameModes__SetIsHuman.c`：`if (+0x80 == 0) throw;`）。
        /// <para>⚠️ 差异（同族）：`RankedSharedData` 为 null 时原版 NRE，我们出声后返回。</para></summary>
        public void SetIsHuman()
        {
            var d = RankedSharedData;
            if (d == null)
            {
                Debug.LogError("[GameModes] SetIsHuman：`RankedSharedData` 是 null（原版这里 NRE）⇒ 不写");
                return;
            }
            d.isHuman = true;
        }

        // ============================================================ 账 223 · SetLossStreak（原版 `:424`）
        /// <summary>原版 `SetLossStreak(int lossStreak)`：`RankedSharedData.lossStreak(+0x40) = value`
        /// （`GameModes__SetLossStreak.c`）。
        /// <para>⚠️ 差异（同族）：`RankedSharedData` 为 null 时原版 NRE，我们出声后返回。</para></summary>
        public void SetLossStreak(int lossStreak)
        {
            var d = RankedSharedData;
            if (d == null)
            {
                Debug.LogError("[GameModes] SetLossStreak：`RankedSharedData` 是 null（原版这里 NRE）⇒ 不写");
                return;
            }
            d.lossStreak = lossStreak;
        }

        // ============================================================ 账 224 · SetResultCount（原版 `:430`）
        /// <summary>原版 `SetResultCount(string mode, BattleWinner winner)`。真身（`GameModes__SetResultCount.c`，
        /// 三个立即数 `0xa / 0x14 / 0x1e` 实读）：
        /// <code>
        /// switch (winner) {
        ///   case Player(10): ResultCounterData.GetValuesForMode(mode).Won (+0x10) += 1; return;
        ///   case Enemy (20): ResultCounterData.GetValuesForMode(mode).Loss(+0x14) += 1; return;
        ///   case Draw  (30): ResultCounterData.GetValuesForMode(mode).Draw(+0x18) += 1; return;
        ///   default: return;                       // ★ 其它值**什么都不读**、直接 return（不抛）
        /// }
        /// </code>
        /// 🔴 **`default` 支必须在 `GetValuesForMode` 之前**：`GetValuesForMode` 会**建一条新记录**
        /// （判据 `ResultCounterSharedData__GetValuesForMode.c`）—— 顺序反了，未知 winner 也会往表里塞一格空的。
        /// <para>⚠️ 差异（同族）：`ResultCounterData` 为 null / 取不到值时原版 NRE，我们出声后返回。</para></summary>
        public void SetResultCount(string mode, BattleWinner winner)
        {
            // ★ default 支：原版这一支**不碰** ResultCounterData（连读都没有）
            if (winner != BattleWinner.Player && winner != BattleWinner.Enemy && winner != BattleWinner.Draw) return;

            var data = ResultCounterData;
            if (data == null)
            {
                Debug.LogError("[GameModes] SetResultCount：`ResultCounterData` 是 null（原版这里 NRE）⇒ 不计数");
                return;
            }
            var v = data.GetValuesForMode(mode);
            if (v == null)
            {
                Debug.LogError("[GameModes] SetResultCount：模式 '" + (mode ?? "<null>")
                             + "' 取不到 `ResultValues`（原版这里 NRE）⇒ 不计数");
                return;
            }
            switch (winner)
            {
                case BattleWinner.Player: v.Won += 1; break;
                case BattleWinner.Enemy: v.Loss += 1; break;
                case BattleWinner.Draw: v.Draw += 1; break;
            }
        }

        // ============================================================ 账 220 · InitializeHandler（P3 桩）
        /// <summary>原版 `public override Task InitializeHandler()`（`:409`）：异步体 =
        /// **`await base.InitializeHandler()`** → `PlayfabWrapper.GenericCloudScriptHandler&lt;object&gt;(…)`
        /// （`GameModes._InitializeHandler_d__30__MoveNext.c:70`）→ 回包里拿 `RankedSharedData`，交给
        /// `<InitializeHandler>b__30_0` 写进 `+0x80`。
        /// <para>🔴 **P3（原版服务器 / PlayFab CloudScript）**：本地没有服务器 ⇒ **只跑基类那一跳**
        /// （`RefreshCount += 1` + 逐个事件 `Initialize()`），服务器半段**留桩 + 出声**。
        /// ⛔ 不许把它改成「假装已经拿到回包」。</para></summary>
        public override Task InitializeHandler()
        {
            var t = base.InitializeHandler();
            Debug.Log("[GameModes] InitializeHandler：基类那一跳已跑（RefreshCount + 事件 Initialize）；"
                    + "原版随后的 `PlayfabWrapper.GenericCloudScriptHandler`（服务器回包 RankedSharedData）"
                    + " 是 **P3 = 原版服务器**，本地没有 ⇒ 未实现（回包写口见 `ApplyServerRankedSharedData`）");
            return t;
        }

        // ============================================================ 账 225 · b__30_0（P3 桩）
        /// <summary>原版 `<InitializeHandler>b__30_0(RankedSharedData result)`（`:756`）：**一行** ——
        /// `this+0x80 = result`（服务器回包写进 `RankedSharedData`）。
        /// <para>🔴 **P3 桩**：服务器链本地不存在 ⇒ 本方法**当前没有调用方**。保留它是因为它是那一跳的**唯一写口**
        /// （`RankedSharedData` 的 setter 是 private）—— 将来接本地模拟时，调用点只有这一个。
        /// ⛔ 别给它编一个「本地也能拿到」的回包。</para></summary>
        public void ApplyServerRankedSharedData(RankedSharedData result)
        {
            Debug.LogWarning("[GameModes] ApplyServerRankedSharedData：这是 P3（服务器回包）的落地口 —— "
                           + "原版由 `InitializeHandler` 的 CloudScript 回调调用；本地没有服务器，**当前无调用方**。"
                           + "本次写 " + (result == null ? "null（原版同样直接写 null）" : "服务器回包"));
            RankedSharedData = result;                  // 原版：`this+0x80 = result`
        }

        /// <summary>账 226 · `&lt;InitializeHandler&gt;n__0()`：编译器生成的蹦床，体就是
        /// `return base.InitializeHandler();`（`GameModes.cs:764`）。
        /// 我们的 <see cref="InitializeHandler"/> 第一句 `base.InitializeHandler()` 就是它 —— **不单独建方法**
        /// （原版这个成员没有独立行为，建出来会是一条永不执行、也没有语义的重复体）。</summary>

        // ============================================================ 注入点（差异 ④）
        /// <summary>原版 `typeof(IRankedEvent)`（`GetActiveEvent(Type)` 里那个比较的类型）。
        /// <para>TODO（欠账，⛔ 不是「自己设计」）：`IRankedEvent` 在**原版里确凿存在**（`dump.cs:57719`
        /// `RankedFastMode …, IRankedEvent, …`；`:62773` `[Inherits(typeof(IRankedEvent), …)]`），本仓**还没建**这个接口
        /// （`Shell/PlayEvent.cs` 只有 `PlayEvent<TSave,TData>` 的同名公开成员）⇒ 本注入点只是**占位**。
        /// 待办：建 `IRankedEvent` ⇒ 直接 `typeof(IRankedEvent)`，**删掉这一格**。
        /// `null` = 排位特殊支不可进入。执行代理 B89-E11 2026-10-28 · `B89_RXb` 改成 TODO。</para></summary>
        public static Type RankedEventInterfaceType;

        /// <summary>自检收尾用（⛔ 只给断言）。</summary>
        public static void ResetForTest()
        {
            RankedEventInterfaceType = null;
            GameModesConfigSource.ResetForTest();
            _playModeProps.Clear();
            _matchFinishedMethods.Clear();
            // 草稿闸（账 585 的那一半）：只是把值还原成兜底 true。
            // ⚠️ **装在那个闭合泛型上的委托清不掉**（`DraftEventBase<TS,TD>` 的 static 是**按闭合类型各一份**，
            //    本类不知道将来哪个闭合类型 ⇒ 由调用方用该类型的 `ResetInjectionForTest()` 自己清）。
            DraftModeUnlocked = true;
        }

        // ============================================================ 账 585 的那一半 · 练习首胜解锁草稿（草稿入口闸）
        /// <summary>草稿（轮抽）入口的开关值。判据 = 原版 `Everguild.LiveOps/PracticeEvent.cs:95-101`：
        /// <code>DemoData demoData = PlayerDataManager.singletonManager.demoData;
        ///       demoData.canEnterDraftMode = true;
        ///       PlayerDataManager.singletonManager.UploadOnlineDemoData();</code>
        /// ⇒ 真正的那一位住在 `PlayerDataManager.demoData.canEnterDraftMode`（**首胜前为 false**），
        /// `PracticeEvent.OnMatchWon()` 只做「置 true + 上传」。
        /// <para>🔶 自己设计（非原版）：**默认值 `true`** —— 判据**是可读的**（⚠️ 2026-10-28 `B89_RXb` 订正：
        /// 原来写「本机没有 `demoData` 这一层 / 全仓 `canEnterDraft` 0 命中」，那只对 `decomp_full` 成立；
        /// `DemoData.canEnterDraftMode` 在 `dump.cs:79362`、解锁路径在 `cpp2il/…/PracticeEvent.cs:97-101`），
        /// 是**我们还没实现** `DemoData` 与 `PracticeEvent`（账 584/585 仍为 C）。照原版默认 `false` 接上，
        /// 轮抽会变成**谁都进不去且不可恢复**（原版没有的死状态）⇒ 兜底放行。
        /// 等练习首胜落地后翻成 `false` 即逐字复刻（接口不变）。
        /// 执行代理 B89-E11b 2026-10-28 · `B89_RXb` 改口径。</para></summary>
        public static bool DraftModeUnlocked = true;

        /// <summary>把「草稿入口闸」注入给 `DraftEventBase&lt;TS,TD&gt;`（原版这一位由 `PlayerDataManager` 提供，
        /// 见 <see cref="DraftModeUnlocked"/>）。**值 = `DraftModeUnlocked` 的活读口**（不是快照）。
        /// <para>🔴 **为什么是泛型方法**：`DraftEventBase&lt;TS,TD&gt;.CanEnterDraftMode` 是**泛型类上的 static** ⇒
        /// 每个**闭合类型各一份**（R2d 是这么建的，`Shell/DraftEventBase.cs:340`），所以注入必须点名闭合类型。
        /// 本机目前**没有任何具体子类**（`Shell/DraftEventBase.cs:346` 明写「本文件不给具体子类」）⇒
        /// **生产上今天还注入不了**（没有那个 static 实例）。等轮抽线的具体子类落地时，那一行就是：
        /// <c>GameModesHandler.InstallDraftGate&lt;DraftEventSaveGame, DraftEventData&gt;();</c>
        /// </para><para>⚠️ 待接（账 585）：调用点 0 个。⛔ 别为了「装作接上了」在这里编一个闭合类型。</para></summary>
        public static void InstallDraftGate<TS, TD>()
            where TS : PlayEventSaveGame
            where TD : DraftEventData
        {
            DraftEventBase<TS, TD>.CanEnterDraftMode = () => DraftModeUnlocked;
            Debug.Log("[GameModes] 草稿入口闸已注入 `DraftEventBase<" + typeof(TS).Name + "," + typeof(TD).Name
                    + ">.CanEnterDraftMode`（值 = `DraftModeUnlocked`，原版那一位在 `PlayerDataManager.demoData.canEnterDraftMode`）");
        }

        /// <summary>原版 `PracticeEvent.OnMatchWon()`（`PracticeEvent.cs:95-101`）的落地口：**练习首胜 ⇒ 解锁草稿**。
        /// <para>⚠️ 当前 **0 调用方**：本机没有 `PracticeEvent` 类，练习流程的胜局回调在
        /// `Shell/PracticeModePopup.cs` / `Battle/BattleDriver.cs`（账 584/585 仍为 C）⇒ 这是**待接点**。
        /// 原版第二句 `UploadOnlineDemoData()`（上传）是 P3，本地没有服务器。</para></summary>
        public void NotifyPracticeMatchWon()
        {
            DraftModeUnlocked = true;                       // 原版：demoData.canEnterDraftMode = true
            Debug.LogWarning("[GameModes] 练习首胜 ⇒ 解锁草稿入口（原版还会 `PlayerDataManager.UploadOnlineDemoData()`，"
                           + "本地没有服务器 = P3）");
        }

        // ⚠️ **本类没有构造函数**：草稿闸的注入必须点名闭合类型（见上面 `InstallDraftGate<TS,TD>`），
        //    而本机还没有那个子类 ⇒ 不在构造里装作装上了。这一整段都放在类尾，前面成员的 `文件:行号` 落点不受影响。

        // ============================================================ 账 776 / 777 / 778 · 上赛季读口族（原版 `:317` / `:330` / `:343`）
        // 判据（🥇 现读 2026-10-28，`cpp2il/out_cpp2il_dev_ilspy/GameModes.cs:317-360`）：
        //   · `get_PreviousSeasonClassicLeaderboardKey()` = `RankedSharedData(+0x80).previousLeaderboard`   （null ⇒ NRE）
        //   · `get_PreviousSeasonFastLeaderboardKey()`    = `FastSharedData(+0x88).previousLeaderboard`     （null ⇒ NRE）
        //   · `get_LastPlayedSeason()` = `Math.Max(FastSharedData(+0x88).lastPlayedSeason, RankedSharedData(+0x80).lastPlayedSeason)`
        // 🔴 最容易写错的两点：① Fast 那条读 **FastSharedData**（+0x88），**不是** RankedSharedData；
        //    ② LastPlayedSeason 是**两表取 Max**，不是只取 Ranked 一张。
        // ⚠️ 差异（同 `LossStreak` / `IsHuman`）：数据类为 null 时原版 NRE，我们出声后返回兜底（null / 0）。

        /// <summary>原版 `GameModes.PreviousSeasonClassicLeaderboardKey`（`GameModes.cs:317`）：
        /// `<see cref="RankedSharedData"/>.previousLeaderboard`（+0x80 → 该表 +0x10）。
        /// <para>⚠️ 差异：`RankedSharedData` 为 null 时原版 NRE，我们出声后返回 null。</para></summary>
        public string PreviousSeasonClassicLeaderboardKey
        {
            get
            {
                var d = RankedSharedData;
                if (d != null) return d.previousLeaderboard;
                Debug.LogError("[GameModes] PreviousSeasonClassicLeaderboardKey：`RankedSharedData` 是 null（原版这里 NRE）⇒ 返回 null");
                return null;
            }
        }

        /// <summary>原版 `GameModes.PreviousSeasonFastLeaderboardKey`（`GameModes.cs:330`）：
        /// **`FastSharedData`（+0x88）**.previousLeaderboard —— ⛔ **不是** `RankedSharedData`。
        /// <para>⚠️ 差异：`FastSharedData` 为 null 时原版 NRE，我们出声后返回 null。</para></summary>
        public string PreviousSeasonFastLeaderboardKey
        {
            get
            {
                var d = FastSharedData;
                if (d != null) return d.previousLeaderboard;
                Debug.LogError("[GameModes] PreviousSeasonFastLeaderboardKey：`FastSharedData` 是 null（原版这里 NRE）⇒ 返回 null");
                return null;
            }
        }

        /// <summary>原版 `GameModes.LastPlayedSeason`（`GameModes.cs:343`）：
        /// `Math.Max(FastSharedData.lastPlayedSeason, RankedSharedData.lastPlayedSeason)` —— **两表取 Max**
        /// （不是只取 Ranked 那张）。
        /// <para>⚠️ 差异：任一表为 null 时原版 NRE，我们把 null 那半按 int 兜底（`int.MinValue`）后仍取 Max 并出声。</para></summary>
        public int LastPlayedSeason
        {
            get
            {
                var fast = FastSharedData;
                var ranked = RankedSharedData;
                if (fast != null && ranked != null)
                    return Math.Max(fast.lastPlayedSeason, ranked.lastPlayedSeason);
                Debug.LogError("[GameModes] LastPlayedSeason：`FastSharedData`/`RankedSharedData` 有为 null 的"
                             + "（原版这里 NRE）⇒ 按现存那半兜底取 Max");
                if (fast != null) return fast.lastPlayedSeason;
                if (ranked != null) return ranked.lastPlayedSeason;
                return 0;
            }
        }
    }
}
