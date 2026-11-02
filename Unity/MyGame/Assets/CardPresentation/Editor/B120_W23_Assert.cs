// B120_W23_Assert.cs — 执行代理 `B120-W23`（2026-11-02）· `LiveOpsEvent<TSave,TData>` 的
//                      `ValidateEventData` 精确类型闸 + `LoadEventData` 走总闸
//
// ============================ 它盯的是什么 ============================
//  原版方法体是 `B120-W22` 补导出来的（`_tmp_view/ghidra_b120/out/LiveOpsEvent_object__object_.ValidateEventData__10173d0.c`），
//  本次按它改了两处（判据全文 → `资料/完全复刻/报告/B120_W22_ValidateEventData.md`）：
//    ① `ValidateEventData` 的 **save 精确类型闸**：`save.GetType() != typeof(TSave) ⇒ return false`
//       （改前是 `save as TSave` ⇒ 类型对不上就悄悄新建一份存档并 `return true`；**语义不同，不是风格差异**）。
//    ② `LoadEventData` **改为走总闸**：`Handler = handler` 先写 → `if (ValidateEventData(...))` 才写 `Data`/`SaveGame`
//       + `UpdateCachedSave`；失败时**一个字段都不写**（改前会把 `SaveGame` 清成 null）。
//
// ============================ 断言分组 ============================
//  A 组 · `data` 那一侧的闸（isinst ⇒ **收子类**）：
//     A1 `data == null` ⇒ false、两个 out 均 null
//     A2 `data` 是 TData 的**父类**实例 ⇒ false、两个 out 均 null
//     A3 `data` 是 TData 的**子类**实例 ⇒ true（isinst 允许子类 —— 与 B 组形成对照，⛔ 别把两侧统一）
//  B 组 · `save` 那一侧的**精确类型闸**（🔴 本件核心）：
//     B0 `save` 恰好是 TSave ⇒ true，且 `formatedSave` **就是**传入那一份（复用，不新建）
//     B1 `save` 是 TSave 的**父类**实例 ⇒ **false**、两个 out 均 null
//     B2 `save` 是 TSave 的**子类**实例 ⇒ **false**（子类也不收）；在两个不同的 TSave 上各验一次
//  C 组 · 类型对但 `Validate` 不过 ⇒ 新建
//     C1 ⇒ true，`formatedSave` 是**新实例**（≠ 传入那份），`eventId` 抄自 data、`IsNew = true`
//  D 组 · `LoadEventData` 走总闸（原版顺序）
//     D1 正常装载：`Data`/`SaveGame` 写入 + `UpdateCachedSave` 调 1 次（实参 = 新 SaveGame）
//     D2 失败装载：`Data`/`SaveGame` **保持旧值**（同一引用，⛔ 不被清成 null）、`Handler` **已写**、`UpdateCachedSave` 不调
//  E 组 · 源码锁（与 A–D 互补的第二道）
//     E1 源码里必须有精确闸的**代码形态** `save != null && save.GetType() != typeof(TSave)) return false;`
//        （⚠️ 只锁片段名 `save.GetType() != typeof(TSave)` 不够 —— 注释里也有，删了代码也还是绿）
//     E2 源码里**不许**再有旧写法 `s != null && Validate(s)`
//     E3 源码里 `LoadEventData` 必须调 `ValidateEventData(... out fd, out fs)`
//
// ============================ 灭自证（🧨 结构上不可能同时满足）============================
//  🧨 1（B1/B2）：把精确闸改回 `save as TSave` ⇒ B1/B2 **必红**（父类 save ⇒ `as` 得 null ⇒ 落到新建并 `return true`；
//     子类 save ⇒ `as` 成功 ⇒ `return true`），而 A3（data 子类 ⇒ true）仍然绿 ⇒ **不能靠「一律 false」蒙混**。
//  🧨 2（D2）：把 `LoadEventData` 改回「不调总闸、自己 `as` 后直接写」⇒ D2 **必红**
//     （`SaveGame` 会被写成 null、`Data` 会被换掉），而 D1 仍然绿。
//  🧨 3（C1 vs B0）：C1 要求「类型对但 Validate 不过 ⇒ **新实例**」，B0 要求「类型对且 Validate 过 ⇒ **同一实例**」——
//     两者是同一观测量（`ReferenceEquals(formatedSave, 传入)`）上的**反相**结果 ⇒ 把两条路径做成同一条必有一条红。
//  🧨 4（E 组）：源码锁与行为断言**互相独立** —— 行为全绿但源码留着旧写法（比如旧分支没删干净）⇒ E 组红。
//
// ============================ 怎么接 ============================
//  纯内存夹具（⛔ 不建场景对象 · 不读存档 · 不写 PlayerPrefs · 不碰 Unity 时序）。
//  挂法（由调度台决定）：在某个宿主的聚合行**同一物理行**追加
//      B120_W23_Assert.Run(); _sink.Pass += B120_W23_Assert.Pass; _sink.Fail += B120_W23_Assert.Fail;
//  （⚠️ 同一物理行追加前先看该行第一个 `//` —— 追加文本必须落在它之前。）
//  判绿红：`grep "^B120W23"` 的**合计行**；逐条按**行首** `B120W23 ✓` / `B120W23 ✗` 数
//  （⛔ 别 `grep -c ✗` —— `✗` 会出现在断言文案里）。
//
//  ⚠️ **D2 会刻意触发一条生产日志**：`[LiveOps] LoadEventData：ValidateEventData 拒收本次装载 …`（`Debug.LogError`）。
//     那是**被测分支的「有意差异」**（原版静默、我们出声），⛔ **不是自检失败**；判绿红只看 `B120W23 ✗` 的行首。
//     若某宿主的判绿逻辑是「有任何 LogError 就红」，把它挂到该宿主之前先确认这一点（其余断言一条 LogError 都不产生）。

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using CardPresentation;

/// <summary>执行代理 `B120_W23`：`ValidateEventData` 的精确类型闸 + `LoadEventData` 走总闸。</summary>
public static class B120_W23_Assert
{
    static int _pass, _fail;

    /// <summary>通过条数（宿主可读）。</summary>
    public static int Pass { get { return _pass; } }
    /// <summary>失败条数（宿主可读）。</summary>
    public static int Fail { get { return _fail; } }

    static void Check(bool ok, string msg)
    {
        if (ok) { _pass++; Debug.Log("B120W23 ✓ " + msg); }
        else { _fail++; Debug.LogError("B120W23 ✗ " + msg); }
    }

    // ============================================================ 夹具类型
    // ⚠️ 全部是**纯 POCO**（`LiveOpsEventData` / `LiveOpsSave` 都是非 sealed 的普通类，无参构造可用）。

    /// <summary>`TData`：比 `LiveOpsEventData` 深一层 —— 这样「传父类实例」才可能失败。</summary>
    class ProbeData : LiveOpsEventData { }          // 2026-11-03：去掉 `sealed`（子类用来验「精确类型闸不收子类」）
    /// <summary>`TData` 的**子类**（A3 用例：isinst **收**子类）。</summary>
    sealed class ProbeDataChild : ProbeData { }
    /// <summary>`TSave`：比 `LiveOpsSave` 深一层 —— 这样「传父类实例」才可能失败。</summary>
    class ProbeSave : LiveOpsSave { }               // 2026-11-03：同上，去掉 `sealed`（子类用例）
    /// <summary>`TSave` 的**子类**（B2 用例：精确闸**不收**子类）。</summary>
    sealed class ProbeSaveChild : ProbeSave { }

    /// <summary>`Validate` 恒 true（基类实现）。</summary>
    sealed class Probe : LiveOpsEvent<ProbeSave, ProbeData> { }
    /// <summary>`Validate` 恒 false（C1 用例：类型对、校验不过 ⇒ 新建）。</summary>
    sealed class RejectProbe : LiveOpsEvent<ProbeSave, ProbeData>
    {
        /// <summary>原版 `Validate` 是 virtual，子类按自己的存档结构判 ⇒ 这里恒 false。</summary>
        protected override bool Validate(ProbeSave save) { return false; }
    }
    /// <summary>`TSave = LiveOpsSave`（基类）—— B2 用它证明「子类也不收」与 TSave 深浅无关。</summary>
    sealed class BaseSaveProbe : LiveOpsEvent<LiveOpsSave, LiveOpsEventData> { }

    /// <summary>最小 `ILiveOpsHandler`：只记 `UpdateCachedSave`，其余全是空转（失败分支不该调到它们）。</summary>
    sealed class StubHandler : ILiveOpsHandler
    {
        public int UpdateCachedCalls;
        public LiveOpsSave LastCached;

        public string HandlerKey { get { return "b120w23_probe"; } }
        public bool IsFirstInitialization { get { return false; } }
        // 用显式 add/remove 避免 CS0067（字段式事件「从未使用」警告）
        public event Action EventsRefreshed { add { } remove { } }

        public void RegisterManager(LiveOpsManager manager) { }
        public IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false) { return new ILiveOps[0]; }
        public IEnumerable<ILiveOps> GetBlockedEvents() { return new ILiveOps[0]; }
        public ILiveOps GetEvent(string id) { return null; }
        public Task ValidateData(LiveopsHandlerData data) { return Task.FromResult(0); }
        public Task SetupEvents(List<ILiveOps> events, List<ILiveOps> blockedEvents, LiveOpsJson sharedData) { return Task.FromResult(0); }
        public Task InitializeHandler() { return Task.FromResult(0); }
        public void ClearEvents() { }
        public Task RefreshHandler() { return Task.FromResult(0); }
        public Task RefreshHandlerLocally() { return Task.FromResult(0); }
        public void TryScheduleHandlerRefresh(long milis) { }
        public void UpdateCachedSave(LiveOpsSave saveGame) { UpdateCachedCalls++; LastCached = saveGame; }
    }

    // ============================================================ 入口
    public static void Run()
    {
        _pass = _fail = 0;

        var p1 = new Probe();            // TSave = ProbeSave, TData = ProbeData, Validate ⇒ true
        var rej = new RejectProbe();     // 同上，Validate ⇒ false
        var baseSaveEv = new BaseSaveProbe();

        ProbeData fd; ProbeSave fs;
        LiveOpsEventData fdBase; LiveOpsSave fsBase;

        // ---------------------------------------------------------- A 组：data 一侧（isinst ⇒ 收子类）
        {
            bool r = p1.ValidateEventData(null, null, out fd, out fs);
            Check(!r && fd == null && fs == null,
                  "A1 data == null ⇒ false，且两个 out 均 null（原版 .c:18-21 的 isinst 失败支）");
        }
        {
            // 父类实例（TData = ProbeData，传 LiveOpsEventData）⇒ 不是 ProbeData
            bool r = p1.ValidateEventData(new LiveOpsEventData { eventId = "A2" }, null, out fd, out fs);
            Check(!r && fd == null && fs == null,
                  "A2 data 是 TData 的**父类**实例 ⇒ false，两个 out 均 null（⛔ 不因 data 失败就修改 save）");
        }
        {
            // 子类实例 ⇒ isinst 成功（与 B2 的 save 侧形成对照）
            var child = new ProbeDataChild { eventId = "A3" };
            bool r = p1.ValidateEventData(child, null, out fd, out fs);
            Check(r && ReferenceEquals(fd, child) && fs != null,
                  "A3 data 是 TData 的**子类**实例 ⇒ true 且 formatedData 就是它（isinst 允许子类）");
        }

        // ---------------------------------------------------------- B 组：save 精确类型闸（本件核心）
        {
            var save = new ProbeSave { eventId = "B0" };
            var data = new ProbeData { eventId = "B0" };
            bool r = p1.ValidateEventData(data, save, out fd, out fs);
            Check(r && ReferenceEquals(fs, save) && ReferenceEquals(fd, data),
                  "B0 save 恰好是 TSave 且 Validate 过 ⇒ true，formatedSave **就是**传入那一份（复用，不新建）");
        }
        {
            var data = new ProbeData { eventId = "B1" };
            var parentSave = new LiveOpsSave { eventId = "B1" };     // TSave 的父类实例
            bool r = p1.ValidateEventData(data, parentSave, out fd, out fs);
            Check(!r && fd == null && fs == null,
                  "B1 save 是 TSave 的**父类**实例 ⇒ false、两个 out 均 null（改前这里会 `return true` + 悄悄新建一份）");
        }
        {
            var data = new ProbeData { eventId = "B2a" };
            var childSave = new ProbeSaveChild { eventId = "B2a" };  // TSave 的子类实例
            bool r = p1.ValidateEventData(data, childSave, out fd, out fs);
            Check(!r && fd == null && fs == null,
                  "B2a save 是 TSave 的**子类**实例 ⇒ false（GetType() **完全相等**才放行；改前 `as` 会成功并 return true）");
        }
        {
            // 同一件事换个 TSave 深浅：TSave = LiveOpsSave（基类），传 ProbeSave 子类
            var data = new LiveOpsEventData { eventId = "B2b" };
            var save = new ProbeSave { eventId = "B2b" };
            bool r = baseSaveEv.ValidateEventData(data, save, out fdBase, out fsBase);
            Check(!r && fdBase == null && fsBase == null,
                  "B2b TSave = LiveOpsSave（基类）时，传它的子类 ProbeSave 同样 ⇒ false（闸与 TSave 深浅无关）");
        }
        {
            // save == null ⇒ 仍成功（原版 .c:52-66 的「新建」支），eventId 抄自 data
            var data = new ProbeData { eventId = "Bnull" };
            bool r = p1.ValidateEventData(data, null, out fd, out fs);
            Check(r && fs != null && fs.eventId == "Bnull" && fs.IsNew,
                  "Bnull save == null ⇒ true 且新建一份（eventId = data.eventId、IsNew = true）");
        }

        // ---------------------------------------------------------- C 组：类型对但 Validate 不过 ⇒ 新建
        {
            var data = new ProbeData { eventId = "C1" };
            var oldSave = new ProbeSave { eventId = "C1", WasSeen = true };
            bool r = rej.ValidateEventData(data, oldSave, out fd, out fs);
            Check(r && fs != null && !ReferenceEquals(fs, oldSave) && fs.eventId == "C1" && fs.IsNew && !fs.WasSeen,
                  "C1 类型恰好相等但 Validate 不过 ⇒ true 且 formatedSave 是**新实例**（≠ 传入那份，eventId 正确、IsNew = true）");
        }

        // ---------------------------------------------------------- D 组：LoadEventData 走总闸
        {
            var data = new ProbeData { eventId = "D1" };
            var save = new ProbeSave { eventId = "D1" };
            var h = new StubHandler();
            p1.LoadEventData(data, save, h);
            Check(ReferenceEquals(p1.Data, data) && ReferenceEquals(p1.SaveGame, save) && ReferenceEquals(p1.Handler, h),
                  "D1 正常装载：Data / SaveGame / Handler 三格都写入");
            Check(h.UpdateCachedCalls == 1 && ReferenceEquals(h.LastCached, save),
                  "D1b 正常装载：UpdateCachedSave 调 1 次且实参 = 新 SaveGame（原版 .c:38-57）");
        }
        {
            // 先正常装一次，拿到旧值
            var d0 = new ProbeData { eventId = "D2-old" };
            var s0 = new ProbeSave { eventId = "D2-old" };
            p1.LoadEventData(d0, s0, null);
            var h = new StubHandler();
            // 再装一次**类型不符**的 save（父类实例）⇒ 失败分支
            var d1 = new ProbeData { eventId = "D2-new" };
            var badSave = new LiveOpsSave { eventId = "D2-new" };
            p1.LoadEventData(d1, badSave, h);

            Check(ReferenceEquals(p1.Data, d0),
                  "D2 失败装载：Data **保持旧值**（改前会把 Data 换成新的 —— 因为改前没有总闸）");
            Check(ReferenceEquals(p1.SaveGame, s0) && p1.SaveGame != null,
                  "D2b 失败装载：SaveGame **保持旧值且不为 null**（改前会 `SaveGame = (TSave)null` ⇒ 清成 null）");
            Check(ReferenceEquals(p1.Handler, h),
                  "D2c 失败装载：Handler **已写**（原版 `.c:27-28` 在最前；这条同时锁住「Handler 赋值在验证之前」的顺序）");
            Check(h.UpdateCachedCalls == 0,
                  "D2d 失败装载：UpdateCachedSave **不调**（原版 `.c:38-57` 整段在 if 真支里）");
        }

        // ---------------------------------------------------------- E 组：源码锁（与 A–D 互补）
        // 🔴 前提断言与结论断言**分开写**（⛔ 别用 `if (前提) Check(结论)` —— 那样前提不成立时结论会静默跳过、
        //    日志里只剩「✓」而实际一条没跑；见 CLAUDE.md「自检里的 if (x != null) Check(...)」那条）。
        string src = "";
        bool srcOk = false;
        try
        {
            src = File.ReadAllText(Path.Combine(Application.dataPath, "CardPresentation/Shell/LiveOpsEvent.cs"));
            srcOk = true;
        }
        catch (Exception ex) { Debug.LogError("[B120W23] 读不到 LiveOpsEvent.cs：" + ex.Message); }

        Check(srcOk, "E0 源码可读（`CardPresentation/Shell/LiveOpsEvent.cs`）");
        // ↑ 硬前提：srcOk=false 时下面三条仍会**真的跑**（E1/E3 必红，E2 因空串而假绿 —— 那条本来也只是「旧写法已删」的弱锁）
        // 🔎 E1 刻意锁「代码形态」而不是只锁片段名：doc 注释里也写着 `save.GetType() != typeof(TSave)`，
        //    只锁片段名的话，**把代码改回 `as` 但留着注释** 也能绿（假绿）。这里要求 `&&`（注释里那份是 `&amp;&amp;` 转义）。
        Check(src.Contains("save != null && save.GetType() != typeof(TSave)) return false;"),
              "E1 源码（代码行，非注释）里有精确闸 `save != null && save.GetType() != typeof(TSave)) return false;`");
        Check(!src.Contains("s != null && Validate(s)"),
              "E2 旧写法 `s != null && Validate(s)` 已不存在（改前的 `as` 分支必须删干净）");
        Check(src.Contains("ValidateEventData(data, saveGame, out fd, out fs)"),
              "E3 `LoadEventData` 确实调了总闸（`ValidateEventData(data, saveGame, out fd, out fs)`）");

        Debug.Log("B120W23 合计：通过 " + _pass + " · 失败 " + _fail);
    }
}
