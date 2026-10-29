// B97_GEN2_Assert.cs — 第九十七会话 · 执行代理 `B97_GEN2`（2026-10-29）
//
// ============================ 它盯的是什么（账 `泛型与启动链_补读.tsv` 的 20 条）============================
// 本批 20 条 = **8 条 B + 12 条 C**，判据 → `资料/完全复刻/判据/B96_小域19_分诊.md` · `B96_菜单未分类44_分诊.md`。
// 本文件只钉**我能在这里独立证的那几件**（其余靠机制面/账改，见 `报告/B97_GEN2.md` 逐条）：
//
//   ① **`DynamicList<T>` 的 12 个成员（账 `:725`–`:736` 那 12 条 C）** —— 类本体由 `B97_MENU` 建在
//      `Shell/DynamicList.cs`（同一批的 `菜单_外壳:142/143/149/150/151/154/155` 七条是**同一批成员的重复账**）。
//      行为断言在 `B97_MENU_Assert.CheckB97MenuDynamicList()`（DL-0…DL-10c，已存在）⇒ **本文件不重复**，
//      只钉「**12 个成员的名字/签名与原版逐个对得上**」（= 这 12 条账真正的落点判据）。
//      判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DynamicList.cs`
//        `GetList:198` · `ListType:203` · `GetIndexType:211` · `Get<T1>(T):228` · `Get<T1>(int):269` ·
//        `GetAll<T1>:321` · `Add:555` · `Inser:577` · `IndexOf:582` · `Clear:593` · `Remove:605` · `RemoveAt:610`
//        ＋ 基类 `DynamicList` 的两条**无体**抽象（`.cs:642-647`，账 `菜单_外壳:148/153` 的 D）。
//
//   ② **`ConfigProvider<T>` 的缓存两跳（账 `:577` `UnpackInternal` / `:578` `OnDestroy`）** ——
//      原版体（`decomp_generic_trial/out_b94/ConfigProvider_object_.{UnpackInternal__1682730,OnDestroy__16826e0}.c`）：
//      `UnpackInternal` 命中 ⇒ `<cache>.Write(1)`；`OnDestroy` ⇒ `config != null` 时 `<cache>.Clear(2)`。
//      宿主类 `FUN_180002340` 未解（R-M §四 G4）⇒ 本地等价 = `Shell/ConfigProvider.cs` 的 `ConfigCache`
//      （🔶 自己设计，代码里已打标）。这里钉**那两跳的语义**：写进去能读回、清掉就没了。
//
//   ③ **`LiveOpsHandler.RefreshHandler(LiveopsHandlerData)`（账 `:216`）本批补的两跳**
//      —— 判据 = `decomp_full/LiveOpsHandler._RefreshHandler_d__29_object___MoveNext.c`（271 行，现读）：
//         · `await Task.Yield()` 的**条件**：`.c` 里三跳同址（ILSpy 注 `Method not found @1806FB110` ×3）
//           ⇒ 直接读二进制：RVA `0x6FB110` 的机器码 = `32 c0 c3`（`xor al,al; ret`，恒 `false`）
//           ⇒ **三跳全 false ⇒ 无条件 Yield**。
//         · `await <虚调用 +0x2d8>` = **Slot 26 `InitializeHandler()`**：vtable 步长 = `VirtualInvokeData` 的
//           **16 字节**，三个独立锚点互证 ——
//             `0x2a8 ↔ Slot 23 RefreshHandler(data)`（`_d__26:88` / `_d__27:33` 传的实参就是 `handlerCache`+0x60）
//             `0x2e8 ↔ Slot 27 GetEvents(bool)`（`ForgeHandler.HasAnyArmyRewardsToCollect:16` /
//                                              `RankedScoreBoostHandler.GetBoostsFor:21` / `Shops.GetAllOffers:12` /
//                                              `RewardEventHandler…b__0:36` 四处都传 bool）
//             `0x278 ↔ Slot 20 get_HandlerKey`（拼 `"[" + HandlerKey + "] Handler Refreshed"` 那一句）
//           ⇒ `0x2d8 = 0x2a8 + 3×16` = Slot 26。⚠️ 旧注释「`Slot: 5 = GetBaseEvents` 的 `0x2e8` 矛盾」是**归错槽**：
//           `0x2e8` 是 Slot 27（`GetEvents(bool)`），不是 Slot 5。
//
// ============================ 怎么接（⚠️ 本代理不动宿主 `RuleEngineTest.cs`）============================
// `RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里加**一个块**：
//       // 🆕 2026-10-29（调度台代接线）：B97_GEN2 —— DynamicList 12 成员签名 · ConfigProvider 缓存两跳 ·
//       //   RefreshHandler 的 Task.Yield + InitializeHandler（账 泛型与启动链_补读:216/577/578/725-736）。
//       { CheckB97Gen2(); }
//   建议插在 `{ CheckB97W1Grants(); }` 那一行**之后**（同一批、同一节）。
//   🔴 与 `B97_W1_Assert.cs` 同一条理由：**故意不用 `Step(...)`、方法名不带 `Test`/`Report` 前缀** ——
//      `Step(...)` 会让 `_ranCases`（`RuleEngineTest.cs:1379`）记账、`VerifyRunCaseCoverage()` 拿反射清单对账，
//      而 `工具/gen_diag_runner.py` 只认「8 空格缩进的顶层语句」⇒ 两者都得跟着改。块调用两条都不碰。
//
// ============================ 🔴 灭自证（每条「什么改动能让它红」）============================
//   · `DynamicList<T>` 里把 `Inser` 改名成 `Insert`、或把形参次序改成 `(int, T)` ⇒ DL12-8 条红。
//   · 删掉任一成员（如 `RemoveAt`） ⇒ 对应那一条红（找不到方法 = null）。
//   · `ConfigCache.Write` 改成空体 ⇒ CFG-1 红；`Clear` 改成空体 ⇒ CFG-2 红。
//   · `RefreshHandler` 删掉 `await Task.Yield();` ⇒ RH-2 红（同步上下文 Post 计数从 1 掉到 0）。
//   · `RefreshHandler` 删掉 `await InitializeHandler();` ⇒ RH-3 红（`InitCalls` 从 1 掉到 0）。
//   · 把 `InitializeHandler()` 挪到 `EventsRefreshed` 之后 ⇒ RH-4 的顺序断言红。
//
// ============================ 覆盖边界（本文件没做的）============================
//   · **不跑** Unity / 任何宿主（红线）⇒ 本文件只保证「编得过 + 逻辑自洽」，跑绿由调度台在同步点做。
//   · `DynamicList` 的**行为**（增删/索引/类型过滤）不在这里重复钉 —— `B97_MENU_Assert.cs` 的 DL-0…DL-10c 已覆盖。
//   · 账 `:137/:138`（`ToggleCollectionFilter<T,TOption>` 的两条 lambda）**不在本文件** —— 分诊结论 = 「口径障碍、
//     无代码可做」，⚠️ 见报告 §三：**不能为凑 A 建一个没人调的空壳**。
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CardPresentation;

public static partial class RuleEngineTest
{
    /// <summary>本件的唯一入口（由宿主 `Run()` 里那一个 `{ … }` 块调；⛔ 不用 `Test` 前缀，见文件头）。</summary>
    public static void CheckB97Gen2()
    {
        Section("🆕 B97_GEN2：DynamicList<T> 12 成员 · ConfigProvider 缓存两跳 · RefreshHandler 的 Yield + InitializeHandler");
        B97Gen2DynamicListMembers();
        B97Gen2ConfigProviderCache();
        B97Gen2RefreshHandler();
    }

    // ==================================================================
    //  ① 12 个成员的**名字 + 签名**（账 泛型与启动链_补读:725-736）
    //     期望值一律照 `out_cpp2il_dev_ilspy/DynamicList.cs` 与 `dump.cs:115653-115848` 写死字面量
    // ==================================================================
    static void B97Gen2DynamicListMembers()
    {
        var t = typeof(DynamicList<object>);

        // 基类那两条「原版没有体」的抽象（账 菜单_外壳:148/153 的 D —— 建了子类也不许把它们改档）
        var baseT = typeof(DynamicList);
        Check(baseT.IsAbstract, true, "DL12-0a 基类 `DynamicList` 是抽象类（`.cs:642-647`）");
        Check(baseT.GetMethod("ListType").IsAbstract, true, "DL12-0b `DynamicList.ListType()` 是**抽象**（原版无 RVA ⇒ 无体，⛔ 别给它写实现）");
        Check(baseT.GetMethod("GetIndexType").IsAbstract, true, "DL12-0c `DynamicList.GetIndexType(int)` 是**抽象**（同上）");

        // `:725` GetList（`.cs:198` = `return list;`）
        var getList = t.GetMethod("GetList", Type.EmptyTypes);
        Check(getList != null, true, "DL12-1a `GetList()` 存在（`.cs:198`）");
        Check(getList != null ? getList.ReturnType : null, typeof(IReadOnlyList<object>),
              "DL12-1b `GetList()` 返回 `IReadOnlyList<T>`（**返回那一张表本身**，不是副本）");

        // `:726` ListType（`.cs:203` = `return typeof(T);`，override）
        var listType = t.GetMethod("ListType", Type.EmptyTypes);
        Check(listType != null ? listType.ReturnType : null, typeof(Type), "DL12-2a `ListType()` 返回 `Type`（`.cs:203`）");
        Check(listType != null && listType.GetBaseDefinition().DeclaringType == baseT, true,
              "DL12-2b `ListType()` 是**基类那条的 override**（`GetBaseDefinition` 指回 `DynamicList`）");

        // `:727` GetIndexType(int)（`.cs:211` = `list[index]?.GetType()`，override）
        var idxType = t.GetMethod("GetIndexType", new[] { typeof(int) });
        Check(idxType != null ? idxType.ReturnType : null, typeof(Type), "DL12-3a `GetIndexType(int)` 返回 `Type`（`.cs:211`）");
        Check(idxType != null && idxType.GetBaseDefinition().DeclaringType == baseT, true,
              "DL12-3b `GetIndexType(int)` 是基类那条的 override");

        // `:728` / `:729` 两个 `Get<T1>`（`.cs:228` / `.cs:269`）—— 都是泛型方法、约束 `where T1 : class, T`
        var gets = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.Name == "Get" && m.IsGenericMethodDefinition).ToList();
        Check(gets.Count, 2, "DL12-4a `Get<T1>` **恰好两个重载**（`.cs:228` 按元素 / `.cs:269` 按下标）");
        var getByElem = gets.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(object));
        var getByIndex = gets.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(int));
        Check(getByElem != null, true, "DL12-4b `Get<T1>(T element)` 存在（`.cs:228`）");
        Check(getByIndex != null, true, "DL12-4c `Get<T1>(int index)` 存在（`.cs:269`）");
        Check(getByElem != null && getByElem.ReturnType.IsGenericParameter, true,
              "DL12-4d `Get<T1>` 返回 **T1**（`as T1`，不是 T）");
        // 约束 `where T1 : class, T` —— `T` 在 `DynamicList<object>` 上实例化成 `object`
        var cons = getByElem != null ? getByElem.GetGenericArguments()[0].GetGenericParameterConstraints() : null;
        Check(cons != null && cons.Length == 1 && (cons[0] == typeof(object) || cons[0].IsGenericParameter), true,
              "DL12-4e `Get<T1>` 的泛型约束里含 `T`（实例化成 `object`）—— 原版 `where T1 : class, T`");

        // `:730` GetAll<T1>（`.cs:321` = `Where(e => e is T1).Select(e => e as T1).ToList()`）
        var getAll = t.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                      .FirstOrDefault(m => m.Name == "GetAll" && m.IsGenericMethodDefinition);
        Check(getAll != null, true, "DL12-5a `GetAll<T1>()` 存在（`.cs:321`）");
        Check(getAll != null ? getAll.GetParameters().Length : -1, 0, "DL12-5b `GetAll<T1>()` **无参**");
        Check(getAll != null ? getAll.ReturnType.GetGenericTypeDefinition() : null, typeof(List<>),
              "DL12-5c `GetAll<T1>()` 返回 `List<T1>`（原版 `ToList()` 出来的是一张新表）");

        // `:731` Add（`.cs:555`）
        var add = t.GetMethod("Add", new[] { typeof(object) });
        Check(add != null, true, "DL12-6 `Add(T)` 存在且**返回 void**（`.cs:555` = `list.Add`）");
        Check(add != null ? add.ReturnType : null, typeof(void), "DL12-6b `Add` 返回类型 = `void`（原版 `List.Add` 的返回值不存在）");

        // `:732` Inser（`.cs:577`）—— 🔴 名字就是拼错的 `Inser`；形参次序 **(element, index)**
        var inser = t.GetMethod("Inser");
        Check(inser != null, true, "DL12-7a `Inser` 存在（**方法名照原版拼写**，⛔ 别改成 `Insert`）");
        Check(t.GetMethod("Insert") == null, true, "DL12-7b 本类**没有** `Insert`（原版二进制里就是 `Inser`）");
        if (inser != null)
        {
            var ps = inser.GetParameters();
            Check(ps.Length, 2, "DL12-7c `Inser` 两个形参");
            Check(ps[0].ParameterType == typeof(object) && ps[1].ParameterType == typeof(int), true,
                  "DL12-7d 形参次序 = **(element, index)**（原版形参就是反的，⛔ 别「顺手改对」）");
        }

        // `:733` IndexOf（`.cs:582`）
        var indexOf = t.GetMethod("IndexOf", new[] { typeof(object) });
        Check(indexOf != null ? indexOf.ReturnType : null, typeof(int), "DL12-8 `IndexOf(T)` 返回 `int`（`.cs:582`）");

        // `:734` Clear（`.cs:593`）
        var clear = t.GetMethod("Clear", Type.EmptyTypes);
        Check(clear != null ? clear.ReturnType : null, typeof(void), "DL12-9 `Clear()` 返回 `void`（`.cs:593`）");

        // `:735` Remove（`.cs:605`）
        var remove = t.GetMethod("Remove", new[] { typeof(object) });
        Check(remove != null ? remove.ReturnType : null, typeof(void),
              "DL12-10 `Remove(T)` 返回 **void**（原版把 `List.Remove` 的 bool **丢掉**，`.cs:605`）");

        // `:736` RemoveAt（`.cs:610`）
        var removeAt = t.GetMethod("RemoveAt", new[] { typeof(int) });
        Check(removeAt != null ? removeAt.ReturnType : null, typeof(void), "DL12-11 `RemoveAt(int)` 返回 `void`（`.cs:610`）");

        // 原版 ctor（`.cs:615`）= `list = new List<T>()` ⇒ 新建出来必须**非 null 的空表**
        // （这一条与 B97_MENU 的 DL-0 同义，但这里只用「不抛 + Count==0」，不重复它的行为断言）
        var fresh = new DynamicList<object>();
        Check(fresh.Count, 0, "DL12-12 新建即空表（原版 ctor 里就 `new List<T>()`，`.cs:615`）");
        Check(ReferenceEquals(fresh.GetList(), fresh.GetList()), true,
              "DL12-12b `GetList()` 两次拿到同一张表（`.cs:198` = `return list`）");
    }

    // ==================================================================
    //  ② `ConfigProvider` 的缓存两跳（账 泛型与启动链_补读:577 / :578）
    //     判据 = `out_b94/ConfigProvider_object_.UnpackInternal__1682730.c`（命中 ⇒ `<cache>.Write(1)`）
    //          + `…OnDestroy__16826e0.c`（`config != null` ⇒ `<cache>.Clear(2)`）
    // ==================================================================
    sealed class B97Gen2FakeConfig : IConfigData
    {
        public string ConfigKey { get { return "B97Gen2_FakeConfig"; } }
    }

    sealed class B97Gen2FakeProvider : IConfigProvider
    {
        public readonly B97Gen2FakeConfig Instance = new B97Gen2FakeConfig();
        public string Key { get { return Instance.ConfigKey; } }
        public IConfigData Config { get { return Instance; } }
        public Type ConfigType { get { return typeof(B97Gen2FakeConfig); } }
        public void Initialize(Dictionary<string, string> titleData) { }
    }

    static void B97Gen2ConfigProviderCache()
    {
        ConfigCache.ResetForTest();

        // 前提（硬检查）：没写过 ⇒ 读不到（否则下面两条测不出东西）
        Check(ConfigCache.Get<B97Gen2FakeConfig>(), null, "CFG-0 前提：缓存空的时候 `Get<T>()` = null");

        // `UnpackInternal` 命中那一跳：`<cache>.Write` ⇒ 之后 `ConfigManager.GetConfig<T>()` 的等价口读得回
        var p = new B97Gen2FakeProvider();
        ConfigCache.Write(p);
        Check(ReferenceEquals(ConfigCache.Get<B97Gen2FakeConfig>(), p.Instance), true,
              "CFG-1 `UnpackInternal` 命中后的 `<cache>.Write(1)` ⇒ `Get<T>()` 拿回**同一个实例**");

        // `OnDestroy` 那一跳：`config != null` ⇒ `<cache>.Clear`
        ConfigCache.Clear(typeof(B97Gen2FakeConfig));
        Check(ConfigCache.Get<B97Gen2FakeConfig>(), null,
              "CFG-2 `OnDestroy` 的 `<cache>.Clear(2)` ⇒ 再读回 null（`Shell/ConfigProvider.cs:156` 那个口）");

        // `IConfigData` 的两个接口默认实现 = 空体（原版接口上就是空 `{ }`，行为零）
        var cfg = new B97Gen2FakeConfig();
        Check(cfg.ConfigKey, "B97Gen2_FakeConfig", "CFG-3 `IConfigData.ConfigKey` 是唯一的数据成员（`ConfigProvider.Key` 读的就是它）");
        ((IConfigData)cfg).AfterUnpack(); ((IConfigData)cfg).CleanUp();   // 原版接口默认实现就是空体 ⇒ 不抛、无副作用（默认实现只能经接口引用调）
        Check(true, true, "CFG-3b `AfterUnpack()` / `CleanUp()` 调得出、无副作用（原版接口默认实现 = 空体）");

        ConfigCache.ResetForTest();        // 收尾：别把测试实例留给别的宿主
    }

    // ==================================================================
    //  ③ `LiveOpsHandler.RefreshHandler(LiveopsHandlerData)`（账 泛型与启动链_补读:216）
    //     🔴 两个关键点：`await Task.Yield()`（条件恒 false ⇒ 恒 Yield）与 `await InitializeHandler()`
    // ==================================================================
    /// <summary>探针 handler：`LiveOpsHandler` 在我们工程里是**纯 C# 类**（不是 MonoBehaviour，
    /// 见 `LiveOpsHandler.cs` 文件头差异 ①）⇒ 可以直接 `new`，⛔ 不需要 `AddComponent`。</summary>
    sealed class B97Gen2ProbeHandler : LiveOpsHandler
    {
        public override string HandlerKey { get { return "B97Gen2Probe"; } }

        public int ValidateCalls;
        public int SetupCalls;
        public int InitCalls;
        public int EventsRefreshedCalls;
        /// <summary>调用顺序（灭自证：把 `InitializeHandler` 挪到 `EventsRefreshed` 之后 ⇒ 顺序变）。</summary>
        public readonly List<string> Seq = new List<string>();

        // 这两条会把「排弹窗 / 反序列化共享块」带进来 —— 纯逻辑自检里一律空实现（不碰 UI / 资产）
        protected override void ScheduleEventPopups() { }
        protected override void DeserializeSharedData(LiveOpsJson sharedData) { }

        protected override Task SetupEvents(List<ILiveOps> allEvents) { SetupCalls++; Seq.Add("setup"); return Task.CompletedTask; }

        public override Task ValidateData(LiveopsHandlerData data) { ValidateCalls++; Seq.Add("validate"); return base.ValidateData(data); }

        public override Task InitializeHandler() { InitCalls++; Seq.Add("init"); return base.InitializeHandler(); }

        /// <summary>`RefreshHandler(LiveopsHandlerData)` 是 `protected` ⇒ 由探针开一个公开口。</summary>
        public Task CallRefresh(LiveopsHandlerData d) { return RefreshHandler(d); }

        public void HookEventsRefreshed() { EventsRefreshed += () => { EventsRefreshedCalls++; Seq.Add("events"); }; }
    }

    /// <summary>只数「同步上下文被 Post 了几次」的上下文。
    /// 为什么这样能测出 `await Task.Yield()`：全部被 await 的 Task 在本夹具里都**同步完成**
    /// （`ValidateData`/`SetupEvents`/`InitializeHandler` 全是 `Task.CompletedTask`）⇒ 只有 `Task.Yield()`
    /// 会产生一次「跳到同步上下文」的 Post。**删掉那句 ⇒ 计数从 1 掉到 0**（灭自证）。</summary>
    sealed class B97Gen2CountingContext : SynchronizationContext
    {
        public int Posts;
        public override void Post(SendOrPostCallback d, object state) { Posts++; d(state); }
    }

    static void B97Gen2RefreshHandler()
    {
        var prev = SynchronizationContext.Current;
        var ctx = new B97Gen2CountingContext();
        var mgr = new LiveOpsManager();          // 纯 C# 类（`public sealed class LiveOpsManager`），不是 MonoBehaviour
        var h = new B97Gen2ProbeHandler();
        h.HookEventsRefreshed();
        h.RegisterManager(mgr);
        SynchronizationContext.SetSynchronizationContext(ctx);
        Task t = null;
        try
        {
            // 载荷非 null（`RawEvents` 空表）⇒ 走「真建 handler」那条路，不会被 data==null 的守卫提前返回
            t = h.CallRefresh(new LiveopsHandlerData(null, new List<RawEvent>()));
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
            // 收尾：把探针自己塞进静态注册表（`LiveOpsManager.HandlerLookup[type]`）的那一项摘掉。
            // ⛔ 别调 `ResetHandlers()` / `ResetForTest()` —— 那会把内建的 `local` / `GameModes` 一起清掉。
            var f = typeof(LiveOpsManager).GetField("HandlerLookup", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = f != null ? f.GetValue(null) as System.Collections.IDictionary : null;
            if (dict != null) dict.Remove(typeof(B97Gen2ProbeHandler));
        }

        Check(t != null, true, "RH-0 前提：`RefreshHandler(data)` 返回了一个 Task");
        Check(t != null && t.IsCompleted, true,
              "RH-1 `RefreshHandler(data)` 在夹具里同步跑完（三个 await 都是 `Task.CompletedTask` + 我们内联跑 Post）");
        Check(ctx.Posts >= 1, true,
              "RH-2 原版的 `await Task.Yield()` **真的在**（同步上下文被 Post 过 ≥1 次；删掉那句 ⇒ 变 0）");
        Check(h.ValidateCalls, 1, "RH-2b 前置：`BuildHandler` 里的 `ValidateData` 走了一次（证明不是被守卫提前返回）");
        Check(h.InitCalls, 1,
              "RH-3 原版 `await <虚调用 +0x2d8>` = Slot 26 `InitializeHandler()` ⇒ 刷新后**恰好一次**（删掉那句 ⇒ 变 0）");
        Check(h.EventsRefreshedCalls, 1, "RH-3b `EventsRefreshed` 触发一次（原版在 InitializeHandler 之后、Signal 之前）");
        Check(string.Join(",", h.Seq.ToArray()), "validate,setup,init,events",
              "RH-4 顺序 = `ValidateData → SetupEvents → InitializeHandler → EventsRefreshed`（原版 `.c` 的真实序）");

        // data == null ⇒ 我们**有意**出声返回（原版这里会 NRE）：确保两跳一个都不跑（不许静默进 Yield）
        var ctx2 = new B97Gen2CountingContext();
        var h2 = new B97Gen2ProbeHandler();
        h2.RegisterManager(mgr);
        SynchronizationContext.SetSynchronizationContext(ctx2);
        Task t2 = null;
        try { t2 = h2.CallRefresh(null); }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(prev);
            var f = typeof(LiveOpsManager).GetField("HandlerLookup", BindingFlags.NonPublic | BindingFlags.Static);
            var dict = f != null ? f.GetValue(null) as System.Collections.IDictionary : null;
            if (dict != null) dict.Remove(typeof(B97Gen2ProbeHandler));
        }
        Check(t2 != null && t2.IsCompleted, true, "RH-5 前提：没有载荷时 `RefreshHandler(null)` 也同步返回");
        Check(ctx2.Posts, 0, "RH-5b `data == null` ⇒ 一声不吭地返回、**不 Yield**（我们的守卫；原版这里 NRE）");
        Check(h2.InitCalls, 0, "RH-5c `data == null` ⇒ **不调** `InitializeHandler()`");
    }
}
