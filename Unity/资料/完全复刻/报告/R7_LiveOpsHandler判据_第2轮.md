# R7 · LiveOpsHandler 11 方法判据补齐 —— 报告（第 2 轮）

- 会话：2026-10-25 · 第八十会话 · 判据补强 · 代理 **R7**
- 产出判据：`资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md`（下文简称 **判据件**）
- 只读取证：**没有向 `D:/2/` 写任何东西**；没改 `.cs` / `账/*.tsv` / 正本 / git；没跑 Unity。

---

## §一 结论（一句话）

> **11 个「`decomp_full` 无产物」的方法，方法体全部拿到（11/11 完整，0 条「部分」、0 条「没有」）；
> E2 那 4 条「未坐实」全部定案；并顺带查出 E2 侧 6 处真偏差（其中 `ResetEvents` 整条语义是反的、`blockedEvents` 从未被 Dispose）。**

补充两条**里程碑**：

1. **G3 的一个结论被就地推翻**：`GetBaseEvents` 被 G3 §4·3 判成「泛型分发跳板、逻辑静态拿不到」。
   实际那 5 条指令是 **`return GetEvents(b);` 的尾跳**（`klass->vtable[slot 27]`）。
   ⇒ **它不是缺口**；G3 报的「20 条真缺口」里应扣掉这一条。
2. **找到两把通用钥匙**（比这 11 个方法本身更值钱）：
   - `FUN_1800021F0(idx, iface, obj)` / `FUN_1803F17A0(obj, iface, idx)` 的 `idx` **就是 `dump.cs` 的 `Slot:`**；
   - `klass` 的 **vtable 基址 = `0x138`、每槽 16 字节** ⇒ `[klass+0xNNN]` 可直接换算「第几号虚方法」。
   本轮靠这两条解开了 9 个接口分派点 + 4 个虚方法槽，**全部自洽**（判据件 §0·3 / §0·4）。

---

## §二 11 个方法的清点（每个都是「拿到完整逻辑」）

| # | 方法 | VA | 结论 | 一句话语义 |
|---|---|---|---|---|
| 1 | `ClearEvents()` | `0x1810176B0` | **完整** | 撤菜单排期（`MenuActions.ReleaseAction`）→ `events` 逐个 `Dispose` → **`blockedEvents` 也逐个 `Dispose`** → 三段 `Clear()` |
| 2 | `DeserializeSharedData(JObject)` | `0x181017BD0` | **完整** | 读**字段** `this.shared` 的 `"NextRefresh"` → **`TryScheduleHandlerRefresh(ms)`**（形参 unused） |
| 3 | `ExpireEvent(…,bool,Action)` | `0x181017C80` | **完整** | 只在 `handlerActions` 里排过才走：出表 → 日志 → （弹窗 `+= () => e.Expire(null)` / 直接 `e.Expire(HandleOnComplete)`）→ `HandleOnComplete`（Raise 信号 + onComplete） |
| 4 | `GetBaseEvents(bool)` | `0x181017FE0` | **完整** | `return GetEvents(b);`（尾跳 vtable slot 27） |
| 5 | `GetEvent(string)` | `0x181018000` | **完整** | `GetEvents(false).FirstOrDefault(x => x.GetID() == id)`（不查 blocked） |
| 6 | `GetEvents(bool)` | `0x181018130` | **完整** | `b ? events.Concat(blockedEvents).Cast<TEvent>() : events`（返回**字段本身**） |
| 7 | `HandleBeforeRefresh()` | `0x1810181B0` | **完整** | `Log("["+HandlerKey+"] Before Refresh")` → `OnBeforeRefresh?.Invoke()` → `BeforeRefresh()` |
| 8 | `InitializeHandler()` | `0x181018250` | **完整** | `RefreshCount++` → `foreach(events) e.Initialize()` → `Task.CompletedTask` |
| 9 | `QueueEventPopup(…, Action<Action>)` | `0x181018490` | **完整** | id 去重 → `filter.eventGroups.Select(r=>r.TargetId).ToArray()` → `QueueActionWithCallback(eventAction, options.ActionRule, **newEvent.Priority**, groups, options.TriggerBeforeOnboardingEnds, drawData.ForceRun, false, null)` → 记表 |
| 10 | `SetupEvents(List,List,JObject)` | `0x181019390`（真身在状态机 `.c`，182 行） | **完整** | `shared = sharedData ?? new JObject()` → **`events = ops.Cast<TEvent>().OrderByDescending(x=>x.Priority).ToList()`** → `blockedEvents = blockedEvents`（换引用）→ `await SetupEvents(events)` → `DeserializeSharedData(shared)` → `ScheduleEventPopups()` |
| 11 | `UpdateCachedSave(persistence)` | `0x181019CB0` | **完整** | `handlerCache.RawEvents.FirstOrDefault(x => x.**Persistence**.eventId == saveGame.eventId)` → 命中则换 `Persistence` |

**附（因 4 条未坐实涉及而顺带取的，也都完整）**：
`LiveOpsEvent<TSave,TData>.Initialize()` `0x181016E70` · `CreateNewSave(TData)` `0x181016CA0` ·
`UpdateData(LiveOpsEventData)` `0x181017250` · `UpdateSaveGame(persistence)` `0x181017340` ·
`SetupEvents(List<TEvent>)` base `0x1810194D0`（= `Task.CompletedTask`）· **`ResetEvents()` `0x181018B90`（查出真缺陷，见 §四 #5）**。

---

## §三 E2 那 4 条「未坐实」的答案

### Q1 · `Initialize()` 的 `hours*3600000` 是什么单位／哪两个字段
**`Data.filter.eventDuration`（int，单位＝小时，默认 24）× 3,600,000 ms ＋ `SaveGame.triggerTime`（long ms）**，
且**只有 `Data.filter.hasDuration == true` 才排期**（这是唯一早退条件）。
证据：`0x181016EAF cmp byte[filter+0x18],0`（`hasDuration`）/ `0x181016ED7 imul eax,[filter+0x1C],0x36EE80`（`eventDuration`）/
`0x181016EF8 add rdi,[save+0x20]`（`triggerTime`）。
三重印证：同一公式在 `EventFilter.GetExpirationTime(ILiveOps)` 里**逐字重复**。

### Q2 · `CreateNewSave` 的初值来源
**`Activator.CreateInstance<TSave>()`（默认构造，`IsNew = true` 由字段初始化器生效）＋ 只抄一格 `save.eventId = data.GetID()`。**
`GetID()` 的实体就是 `return [this+0x18]`（= `LiveOpsEventData.eventId`）。
🔴 **`triggerTime` 不来自 `dates.startDate`** —— 新存档 `triggerTime = 0`；写它的是 `RefreshExpire/UpdateSaveGame` 那一路。
⇒ **E2 现有的 `s.eventId = data.eventId` 已经等价 ✅**（只需改注释）。

### Q3 · `SetupEvents` 的排序键
**`x.GetBaseData().priority`（= `ILiveOps.Priority`），`OrderByDescending` —— 降序。**
两条独立判据：① `call 0x180c99380` 反查 `script.json` = **`Enumerable.OrderByDescending<object,int>`**；
② key selector 实体 `<>c.<SetupEvents>b__18_0` = `GetBaseData()`（ILiveOps slot 15）读 `+0x40`（= `LiveOpsEventData.priority`）。

### Q4 · `DeserializeSharedData` 的那一跳
**`TryScheduleHandlerRefresh(shared["NextRefresh"].Value<long>())`**（`0x181017C5B call 0x181019810`），
**不是** `ScheduleHandlerRefresh(DateTime)`；且数据源是**字段 `this.shared`**（`0x181017C08 mov rcx,[rbx+0x30]`），形参完全没用。

---

## §四 需要改代码的清单

> 全部落在 `Shell/LiveOps*.c{s → s}` 那 5 个文件（本批只涉 2 个）。**本轮不改**，交执行代理。

| # | 文件:行号 | 现在怎样 | 应改成 | 判据 | 风险 |
|---|---|---|---|---|---|
| **1** | `Shell/LiveOpsHandler.cs:191-203`（`Sort`:197） | `Sort(string.CompareOrdinal(GetID()))`——**升序、按 id** | `ops.Cast<ILOps>().OrderByDescending(x => x.GetBaseData()?.Priority ?? 0).ToList()` | `OrderByDescending<object,int>`（RVA `0xC99380`）+ `b__18_0` 读 `+0x40` | 低（只改次序；弹窗排队顺序会变，属预期） |
| **2** | `Shell/LiveOpsHandler.cs:216-222`（调用点:221） | `ScheduleHandlerRefresh(FromMs(ms))` | **`TryScheduleHandlerRefresh(ms)`** | `0x17C5B → 0x181019810` | **中**：会启用「已排更早⇒不动」「已过点⇒丢弃」两道门；原来可能把刷新推后 / 排一个立刻到点的刷新 |
| **3** | `Shell/LiveOpsHandler.cs:218` | 读形参 `sharedData` | 读字段 `this.shared` | `0x17C08 mov rcx,[rbx+0x30]` | 低（`SetupEvents` 已先赋 `shared`，实际等价） |
| **4** | `Shell/LiveOpsHandler.cs:389-399` | 只 `handlerActions.Clear()`；只 `events` 逐个 `Dispose` | ① 逐个 `MenuActions.ReleaseAction(a)`；② **`blockedEvents` 也逐个 `Dispose`** | `0x176A3`+`ReleaseAction`；`0x17AEA mov r8d,3`（blockedEvents 走 slot 3 = Dispose） | 中（① 依赖 `MenuActions` 是否复刻）；② 可独立做、低风险 |
| **5** | `Shell/LiveOpsHandler.cs:403-415` | `foreach Dispose()` + `events.Clear()` + 日志 | **只留 `foreach (var e in events) e.ResetEventProgress();`**（不清空、不 Dispose） | `0x181018B90`：只读 `[this+0x20]`、循环 `mov r8d,7`（slot 7 = `ResetEventProgress`）、**无 `List.Clear`** | **中高**：语义整条反了；须先 grep 现有调用点是否依赖「清空」 |
| **6** | `Shell/LiveOpsHandler.cs:478` | `r.Data.eventId` | **`r.Persistence.eventId`**（且判 `r.Persistence != null`） | `…DisplayClass9_0…b__0.c`：`[[x+0x18]+0x10]`，`RawEvent.Persistence`=`+0x18` | 低 |
| **7** | `Shell/LiveOpsEvent.cs:304-314`（调用点:313） | `TryScheduleHandlerRefresh(SaveGame.triggerTime)` | 先 `if (!Data.filter.hasDuration) return;`，再 `(long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime` | `0x181016E70` | 低 |
| **8** | `Shell/LiveOpsEvent.cs:376-390` | 注释「未坐实：取哪个成员」 | 「已坐实：`save.eventId = data.GetID()`；`triggerTime` 不在此写」 | `0x181016CA0` + `LiveOpsEventData__GetID.c` | 零（只改注释） |
| **9** | `Shell/LiveOpsEvent.cs:393-404` | `data as TData`（子类被接受） | `if (data.GetType() != typeof(TData)) return;` 再 `Data = (TData)data` | `0x181017250`：`GetType()` → `op_Inequality(typeof(TData))` → `jne <return>` | 低（我们暂无子类 TData，但口径要写对） |
| **10** | `Shell/LiveOpsHandler.cs:419-426` | 注释「原版是同义」 | 「原版 = `Concat<ILiveOps>().Cast<TEvent>()`（**懒式、会抛**）；我们立即拷贝 = 有意差异」 | `0x181018130` | 零（只改注释） |
| **11** | `Shell/LiveOpsHandler.cs:229-244` `ScheduleEventPopups` | 只建去重表 + 出声 | 真身在 cpp2il（SEV4，有体）：按 `drawData.ShowWindowOnTrigger / ShowWindowOnExpire` 调 `ShowTriggerEventPopup` / `ExpireEvent` | `LiveOpsHandler.cs:1606…`（cpp2il）+ `ShowTriggerEventPopup 0x181019560` | 属 C1-W2；**本轮只指路、未逐条反汇编** |
| **12** | `Shell/LiveOpsHandler.cs:371-383` `InitializeHandler` | 每事件 `try/catch` 吞异常 | 原版无 catch | `0x181018250` | 零（有意差异，保留 + 写清） |

**已经正确的（别动）**：`GetBaseEvents`(`:440-443`) · `GetEvents`(`:419-426` 实现) · `GetBlockedEvents`(`:446`) ·
`GetEvent`(`:450-460`) · `HandleBeforeRefresh`(`:304-310`) · `InitializeHandler` 结构 · `SetupEvents(List<TEvent>)` base ·
`TryScheduleHandlerRefresh` / `ScheduleHandlerRefresh`（E2 已按 `.c` 逐条照抄）。

---

## §五 没查清（+ 还差什么）

1. **`ExpireEvent` 弹窗支的 `OpenWindow` 第 4/5 参**：只读到 `closeAll:false` + 一个 `default` 结构。
   **还差**：`WindowsManager.OpenWindow` 各重载的逐签名对照（`decomp_full` 里是同形多份产物）。
   不影响「开 `options.PopupLabel` 那扇窗、`newEvent` 当 data」这一层结论。
2. **`g__HandleOnComplete` 里 `Signal.Raise(new <>f__AnonymousType0<object>(id))` 的匿名类型是什么**：只读到类型名 + 单参。
   **还差**：该匿名类型的成员名（`LiveOpsHandlerRefreshedSignal` 只有 `Handler` 一格，形状不符）。
3. **`MenuActions` 要不要复刻**：原版 `handlerActions` 的值是 `MenuActions.Action`
   （`QueueActionWithCallback` 返回的自定义类），E2 用的是 `System.Action`。
   要接 `MenuActions.ReleaseAction`（§四 #4①）**必先决定 `MenuActions` 队列本身做不做** ⇒ **要调度台拍板**（属 C1-W2 边界）。
4. **`ScheduleEventPopups` 的真身**：cpp2il **有体**（SEV4、41 行注记），**本轮没逐条反汇编**（不在 11 条里）。
   **还差**：一次 VA 反汇编（入口已知 `LiveOpsHandler.cs:1606` 附近 / 41 行注记）。
5. **`ExpireEvent` 弹窗支的反常次序**（弹窗支**立刻**走 `HandleOnComplete`，而非等窗口关闭）：
   反汇编确实如此（`0x17F31` 是 `+=` 之后的**顺序执行**），但**为什么**未查。
   ⛔ **别按「关闭后才回调」去改**。

---

## §六 顺手发现

1. 🔴 **G3 §4·3 对 `GetBaseEvents` 的定性错了，已就地订正**（判据件 §一 #4）：
   `klass+0x2E8/0x2F0` = `vtable[slot 27]` 的 `{methodPtr, methodInfo}`（基址 `0x138`、每槽 `0x10`），
   而 slot 27 = `GetEvents` ⇒ 这 5 条指令就是 **`return GetEvents(b);` 的尾跳**。**逻辑完全可读，不是缺口。**
2. 🔴 **`ResetEvents` 语义与名字直觉相反**：只 `foreach(e) e.ResetEventProgress()`，**不清空列表、不 Dispose**。
   这条 E2 整条做反了，而且**做反得很「像对的」**（Dispose + Clear 看着才像 Reset）—— 值得进「已知的坑」。
3. 🔑 **接口分派 `idx` = dump.cs 的 `Slot:`**：以后所有 `FUN_1800021F0(idx, iface, obj)` /
   `FUN_1803F17A0(obj, iface, idx)` 都能**一次查表定名**，不必逐条猜。本轮 9 个印证点全自洽。
4. 🔑 **`klass` vtable 基址 `0x138` / 槽宽 `0x10`**：`[klass+0xNNN]` → 虚方法号 → dump.cs `Slot:`。
   本轮靠它解开 `HandleBeforeRefresh`(`0x278`=slot20=`HandlerKey`, `0x2B8`=slot24=`BeforeRefresh`)、
   `_SetupEvents` 状态机(`0x2C8`=25, `0x298`=22)、`UpdateSaveGame`(`0x3F8`=44=`LoadEventData`)。
5. 🔴 **`MenuActions.ReleaseAction` 是全局静态**（`actions.Remove(a)`）⇒「排期表清空」≠「菜单队列撤销」，`ClearEvents` **两个口都做**。
6. 🔴 **`DeserializeSharedData` 的形参根本没用**（读字段 `this.shared`）—— 原版的一处「形参残留」，照抄必须写成字段。
7. 🟢 **同一条算式跨两个类**：`EventFilter.GetExpirationTime(ev)` 与 `LiveOpsEvent.Initialize()` 都是
   `filter.eventDuration*3600000 + ev.GetBaseSave().triggerTime` ⇒ **「下次刷新/到期时刻」全工程只有这一条算式**，
   可当**验收尺子**：`hasDuration` 的活动，`Initialize` 排的点必须等于 `GetExpirationTime`。
8. 🟡 **`LiveOpsEvent<TSave,TData>`（泛型基类）整类的普通方法在 `decomp_full` 也没有产物**
   （`Everguild.LiveOps.LiveOpsEvent__*` 只有**非泛型**基类的 27 条）。
   ⇒ 与 G3 报的「LiveOpsHandler 11 条缺失」**同族**：缺的是「**泛型类的普通方法**」这一类，不是某个类。
   建议补导 `0x1016000`–`0x101A600` 整段（本轮要的 4 个都在里面）。
9. 🟡 **`SetupEvents` 的 `Cast` 不是 `OfType`** —— 元素类型不对会**抛** `InvalidCastException`（原版是有意的严格）。
   我们实现成 `new List(ops)` 时**不抛**，属静默放宽（低风险，但要记）。

---

## §七 交接给下一个代理

**做完了什么**
- 11/11 方法体拿到，判据落盘 `资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md`（含逐方法伪码 + 关键指令 + 字段/槽位两张真值表）。
- E2 的 4 条「未坐实」全部定案（`eventDuration*3600000+triggerTime` / `save.eventId=GetID()` / `Priority 降序` / `TryScheduleHandlerRefresh`）。
- 12 条「要改代码」的清单（§四），其中 **6 条是真偏差**（#1 #2 #4 #5 #6 #7）。

**下一步（按顺序）**
1. **执行代理**照 `§四` 改 `Shell/LiveOpsHandler.cs` + `Shell/LiveOpsEvent.cs`。
   ⚠️ 做 #5（`ResetEvents`）之前**先 grep 调用点**（`ResetEvents(`），确认没人依赖「清空」。
   ⚠️ 做 #4① 之前**先由调度台拍板 `MenuActions` 复刻与否**（§五·3）。
2. 改完 `bash d:/4/Unity/工具/typecheck.sh`（秒级）。
3. `ScheduleEventPopups` 真身（§四 #11 / §五·4）另派一件活；入口 `LiveOpsHandler.cs:1606`。

**文件动到哪**
- 新增：`资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md` · 本报告。
- 脚本（可删）：`d:/4/Unity/_tmp_view/r7/{dis11,dis_liveopsevent,dis_helpers}.py`（`python -I -X utf8` 直接跑，无三方依赖）。
- **没有碰**任何 `.cs` / `账/*.tsv` / 正本 / git / `D:/2/`。

**踩过的坑（省下一轮）**
- 🔴 别把 `FUN_180c99380` 当成含糊的「某个 LINQ 方法」——`script.json` 一反查就是 `OrderByDescending<object,int>`。
- 🔴 别用 cpp2il 的**成员排序**推接口槽序（它是重排过的）；**只用 dump.cs 的 `Slot:` 列**。
- 🔴 泛型类的 `r8` 常常是**隐藏的泛型 `MethodInfo`**，不是用户实参（`DeserializeSharedData` 的形参就是这么「看着有、其实没读」）。
- 🔴 线性反汇编遇 `ret` 要停（本轮的脚本在 `ret` 后遇非 `ret` 即 break）；`ExpireEvent` 的另一支在 `0x181017F6A`，得单独从一个地址起反汇编。
