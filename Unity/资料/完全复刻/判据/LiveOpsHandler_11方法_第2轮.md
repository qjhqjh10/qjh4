# LiveOpsHandler&lt;TEvent&gt; · 11 个「decomp_full 无产物」方法 —— 第 2 轮判据（方法体已拿到）

- 生成：2026-10-25（第八十会话 · 判据补强 · 代理 **R7**）
- 起因：G3 报「`LiveOpsHandler<TEvent>` 的 11 个普通方法在 `decomp_full` 里一条产物都没有
  （41 条产物全是状态机/lambda）」，E2 建框架时的**4 条「未坐实」**正卡在这一段。
- 结论：**11 个方法的方法体全部拿到**（0 条「没有」、0 条「只有部分」），
  并且 **4 条未坐实全部定案**，另**顺带查出 E2 侧 5 处真偏差**（其中 `ResetEvents` 整条语义是反的）。
- ⛔ 本文件**没有写 `D:/2/`**；所有取证都是只读（`script.json` / `GameAssembly.dll` / `decomp_full` / cpp2il 产物）。

---

## §〇 取证路径（这一节是「下次怎么复查」的钥匙）

### 0·1 三层证据，缺一不可

| 层 | 位置 | 本轮用途 | 局限 |
|---|---|---|---|
| **IL 层** | `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LiveOpsHandler.cs`（3,504 行） | 骨架 + 字段偏移（注记里自带 `this @ rcx (LiveOpsHandler\`1<TEvent>)+38` 这种**字节偏移**） | 大量 `Method not found @<addr>` / `Unmanaged memory load` ⇒ **控制流对、被调目标无名** |
| **Ghidra 层** | `D:/2/tools/decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`（182 行）等 41 条 | **async 真身**在这里（`SetupEvents` 的全部逻辑住状态机里） | 泛型类的**普通方法**整类没有产物 |
| **VA 反汇编层（本轮判决层）** | `D:/2/unity_run_ref/GameAssembly.dll` + `script.json` | 把 `Method not found @addr` 反查成真名、把 `call <未命名>` 认出来 | 线性反汇编，遇跳转不跟进（以 `ret` 为界） |

复跑脚本（本轮自己写的，放在 `d:/4/Unity/_tmp_view/r7/`）：
`dis11.py`（11 个方法 + 调用目标命名）· `dis_liveopsevent.py`（`LiveOpsEvent<T>` 四个方法）·
`dis_helpers.py`（两个 il2cpp 接口分派 helper + `ResetEvents` + `TryScheduleHandlerRefresh`）。
用 `python -I -X utf8` 直接跑，无第三方依赖（`capstone` 5.0.7 已在默认 python 里）。

### 0·2 🔑 通用钥匙：`script.json` 的 `Address` 列能反查「未命名被调目标」

`ScriptMethod[].Address` = **RVA**，`VA = 0x180000000 + Address`。本轮靠它把三条黑盒打通：

| 黑盒地址 | 反查结果 | 意义 |
|---|---|---|
| `call 0x180c99380` | **`System.Linq.Enumerable.OrderByDescending<object,int>`**（同址还有 `<KeyValuePair<object,int>,int>` 等） | ⇒ `SetupEvents` 的排序是**降序**，一次定案 |
| `Method not found @181019810` | **`LiveOpsHandler<object>.TryScheduleHandlerRefresh`** | ⇒ `DeserializeSharedData` 的「那一跳」= `TryScheduleHandlerRefresh`，**不是** `ScheduleHandlerRefresh` |
| `Method not found @181452130` | `Dictionary.ValueCollection.Enumerator.GetEnumerator` 一族（泛型共享体） | 报多个名字时**取共同语义**即可（这类是泛型共享体，不是一个方法有多个名字） |

⚠️ G3 记过工具 bug：`工具/rva_of_method.py` 对泛型名
（`LiveOpsHandler\u003Cobject\u003E$$GetBaseEvents`）匹配不到，但 `grep GetBaseEvents script.json` 能搜到。
⇒ **别只信工具，直接查 json**（本轮的脚本就是这么做的：先建 `Address → [Name…]` 全表，再查）。

### 0·3 🔑 第二把钥匙：il2cpp 的**接口方法分派**（`FUN_1800021F0` / `FUN_1803F17A0`）

```asm
; FUN_1800021F0(idx, ifaceClass, obj, [更多实参])  —— 解析 + 直接调用（tail jmp）
1800021FB  movzx r8d, cx                  ; r8d = idx
1800021FF  mov r11, [rbx]                 ; obj->klass
18000220A  cmp ax, [r11+0x12E]            ; klass->interfaceOffsets 个数
180002226  cmp [r9+rcx*8], rdx            ; 找 ifaceClass == rdx 的那一项
180002255  mov eax, [r9+rcx*8+8]          ; eax = interfaceOffsets[i].offset
18000225D  add eax, r8d                   ;        + idx
180002266  add rax, 0x138                 ; klass->vtable 基址 = **0x138**
18000226C  add rax, r11
18000226F  mov r8, [rax]                  ; methodPtr
180002272  mov rdx, [rax+8]               ; MethodInfo
18000227B  jmp r8
```

⇒ **`klass + 0x138 + (interfaceOffset + idx) * 16`**，每槽 16 字节 = `{methodPtr, MethodInfo}`。
`FUN_1803F17A0(obj, ifaceClass, idx)` 是同一件事的「只解析不调用」版（返回那个 16 字节槽的地址）。

**因此「`idx` 是什么方法」只要查 dump.cs 的 `Slot:` 列即可** —— 见 §0·4 的接口槽表。

### 0·4 已坐实的接口槽表（**权威 = `D:/2/tools/il2cpp_out/dump.cs`**，不是 cpp2il 的重排输出）

**`ILiveOps`**（TypeDefIndex 3395；`ILiveOps : IIdentifiable, IContainerProvider`）
| idx | 方法 | idx | 方法 |
|---|---|---|---|
| 0 | `LoadEventData(data, save, handler)` | 10 | `RefreshExpire(Action)` |
| **1** | **`Initialize()`** | 11 | `MarkAsOld()` |
| **2** | **`Expire(Action onComplete)`** | 12 | `MarkAsSeen()` |
| **3** | **`Dispose()`** | 13 | `ToggleEventActivation(bool,bool,bool)` |
| 4 | `IsActive()` | 14 | `OnEventTriggered()` |
| 5 | `CanActivate()` | **15** | **`GetBaseData()`** |
| 6 | `SelectEvent()` | **16** | **`GetBaseSave()`** |
| **7** | **`ResetEventProgress()`** | 17 | `UpdateSaveGame(persistence)` |
| 8 | `RegisterEventTriggers()` | **18** | **`GetEndDate(bool)`** |
| 9 | `CheckEventFilter()` | 19/20/**21**/22 | `get_SaveType` / `get_DataType` / **`get_Priority`** / `get_IsSegmented` |

**`ILiveOpsHandler`**（TypeDefIndex 1225）：`0 RegisterManager` · `1 GetBaseEvents` · `2 GetBlockedEvents` ·
`3 GetEvent` · `4 ValidateData` · `5 SetupEvents(3参)` · `6 InitializeHandler` · `7 ClearEvents` ·
`8 RefreshHandler()` · `9 RefreshHandlerLocally()` · **`10 TryScheduleHandlerRefresh`** · `11 UpdateCachedSave`

**`IIdentifiable`**：`0 GetID()`
**`IContainerProvider`**：`0 GetContainer()` · `1 ReleaseContainer()`

> 这两个 helper 的 `idx` 就是上表的 `Slot`；本轮用它一共产出 8 个独立印证点
> （`1`=Initialize、`3`=Dispose、`7`=ResetEventProgress、`13`=ToggleEventActivation、`15`=GetBaseData、
> `16`=GetBaseSave、`18`=GetEndDate、`21`=get_Priority、`10`(ILiveOpsHandler)=TryScheduleHandlerRefresh），
> **9 个全部与语义自洽** ⇒ 这张表可以直接当判据用。

### 0·5 字段偏移表（全部经反汇编印证）

**`LiveOpsHandler<TEvent>`**（cpp2il 注记里就是这些十六进制）

| off | 字段 | off | 字段 |
|---|---|---|---|
| `+0x20` | `List<TEvent> events` | `+0x50` | `ScheduledAction scheduledRefresh` |
| `+0x28` | `List<ILiveOps> blockedEvents` | `+0x58` | `LiveOpsManager Manager` |
| `+0x30` | `JObject shared` | `+0x60` | `LiveopsHandlerData handlerCache` |
| `+0x38` | `Dictionary<string, MenuActions.Action> handlerActions` | `+0x68` | `SemaphoreSlim refreshSemaphore` |
| `+0x40` | `Action EventsRefreshed` | `+0x70` | `int RefreshCount` |
| `+0x48` | `Action OnBeforeRefresh` | | |

**`LiveOpsEvent<TSave,TData>`**：`+0x10 Handler` · `+0x18 Data(TData)` · `+0x20 SaveGame(TSave)`
**`LiveOpsEventData`**（`IdentifiableSO`）：
`+0x18 eventId` · `+0x20 uploadToLive` · `+0x21 uploadToPreprod` · `+0x22 enabled` · `+0x24 activationType` ·
`+0x28 dates` · **`+0x30 filter`** · **`+0x38 drawData`** · **`+0x40 priority`** · `+0x44 location` ·
`+0x48 showInMainMenu` · `+0x50 mainMenuOptions`
**`EventFilter`**：`+0x10 army` · `+0x14 expansion` · **`+0x18 hasDuration`** · **`+0x1C eventDuration(=24)`** ·
`+0x20 hasCooldown` · `+0x24 eventCooldown(=168)` · **`+0x28 eventGroups`** · `+0x30 Triggers` · `+0x38 Conditions`
**`LiveOpsEventPersistence`**：`+0x10 eventId` · `+0x18 IsNew(=true)` · `+0x19 WasSeen` · `+0x1A IsActive` ·
**`+0x20 triggerTime(long)`** · `+0x28 expiredTime(long)`
**`EventElements`**：`+0x10 EventAssets` · `+0x18 EventComponents` · `+0x20 EventLabels` · `+0x28 ShowWindowOnTrigger` ·
`+0x30 TriggerPopupOptions` · `+0x38 ShowWindowOnExpire` · `+0x40 ExpirePopupOptions` · `+0x48 HideContainer` · **`+0x49 ForceRun`**
**`PopupOptions`**：`+0x10 PopupLabel` · **`+0x14 ActionRule`** · **`+0x18 TriggerBeforeOnboardingEnds`** · `+0x19 MarkOnScreenClose(=true)`
**`LiveopsHandlerData`**：`+0x10 SharedData` · **`+0x18 RawEvents`**
**`RawEvent`**：`+0x10 Data` · **`+0x18 Persistence`**

---

## §一 11 个方法逐条

> 每条格式：**签名 → 体（伪码 + 关键指令） → 语义 → E2 现状（`文件:行号`） → 差异 → 要改什么**。
> 「完整」= 全控制流可读、被调目标全部具名；本轮 **11/11 都是「完整」**。

### 1. `ClearEvents()` —— 完整

- **VA** `0x1810176B0`（RVA `0x10176B0`，size `0x520`）
- **体**

```csharp
public void ClearEvents()
{
    // ① 逐个把「菜单队列里排着的 action」撤掉（不是只清表）
    foreach (MenuActions.Action a in handlerActions.Values)   // [this+0x38]
        MenuActions.ReleaseAction(a);                          // static void ReleaseAction(Action) => actions.Remove(action)

    // ② 逐个 Dispose 事件
    foreach (ILiveOps e in events)  e.Dispose();               // [this+0x20]，ILiveOps slot 3
    // ③ 逐个 Dispose 被封锁的事件（**E2 漏了这一段**）
    foreach (ILiveOps e in blockedEvents) e.Dispose();         // [this+0x28]，ILiveOps slot 3

    // ④ 三段清空（顺序：events → blockedEvents → handlerActions）
    events.Clear(); blockedEvents.Clear(); handlerActions.Clear();
}
```

- **关键指令**
  - `0x1769F mov rcx,[r14+0x38]` → `call Dictionary.get_Values` → `ValueCollection.GetEnumerator` →
    循环里 `call MenuActions$$ReleaseAction`（`0x18069E5B0`）
  - 事件循环里 `0x17954 mov r8d,3` + `call 0x1803F17A0` ⇒ **ILiveOps slot 3 = `Dispose`**
  - `0x17AEA` 第二段同样 `mov r8d,3`（**blockedEvents 也是 Dispose**）
  - 收尾 `0x17B2D/0x17B53/0x17B80`：两处 `List.Clear` 的内联形状（`inc [rcx+0x1C]; mov [rcx+0x18],0; Array.Clear`）
    与 `Dictionary.Clear`（`0x18169E730`）
- **语义**：清事件的**三个口**（菜单排期 → 事件本体 → 封锁表），**先撤排期、后清列表**。
  `MenuActions.ReleaseAction` 是**全局静态**（`actions.Remove(action)`），
  ⇒ **「`handlerActions.Clear()`」≠「菜单队列已撤销」**，这两件事必须分别做。
- **E2 现状**：`Shell/LiveOpsHandler.cs:387-399`
- **差异**
  1. ❌ **缺 `MenuActions.ReleaseAction`**：E2 只 `handlerActions.Clear()`，菜单队列里排着的动作**不会被撤**。
  2. ❌ **缺对 `blockedEvents` 的 `Dispose`**：被封锁的事件**永远不会被释放**（泄漏 + 容器引用不退）。
  3. ⚠️ E2 对每个元素 `try/catch` 吞异常；原版不判空不 catch（异常直接冒）。**可保留**（更安全），但要记成「有意差异」。
- **要改什么**：补 ① 逐个撤排期（见 §三 #5，需先有 `MenuActions` 等价物）；② `blockedEvents` 也要 `Dispose`。

### 2. `DeserializeSharedData(JObject sharedData)` —— 完整

- **VA** `0x181017BD0`（size `0xB0`）
- **体**

```csharp
protected virtual void DeserializeSharedData(JObject sharedData)   // ⚠️ 形参 sharedData 从头到尾【没被读】
{
    // 唯一早退/抛点：读的是【字段 this.shared】（+0x30），不是形参
    if (this.shared == null) throw new NullReferenceException();   // 0x17C15 test rcx,rcx / je <throw>
    if (this.shared.TryGetValue("NextRefresh", out JToken v))
        this.TryScheduleHandlerRefresh(v.Value<long>());           // ← 0x17C5B call 0x181019810
}
```

- **关键指令**（这一条是「那一跳」的判决证据）
  ```
  0x17BE1  mov rdi, r8                 ; r8 = 泛型 MethodInfo（不是用户实参）
  0x17BE4  mov rbx, rcx                ; rbx = this
  0x17C08  mov rcx, [rbx + 0x30]       ; ★ this.shared  （rdx = 形参 sharedData，全程未读）
  0x17C1A  mov rdx, [0x184248C70]      ; "NextRefresh"
  0x17C2A  call Newtonsoft.Json.Linq.JObject$$TryGetValue
  0x17C3E  call Newtonsoft.Json.Linq.Extensions$$Value<long>
  0x17C43  mov rcx, [rdi + 0x20]       ; MethodInfo->klass（泛型上下文）
  0x17C5B  call 0x181019810            ; ★ = LiveOpsHandler<object>$$TryScheduleHandlerRefresh
  ```
- **语义**：从**共享数据字段**里取 `NextRefresh`（毫秒），交给 **`TryScheduleHandlerRefresh`**（带两道门），
  **不是**直接排期。`shared` 由 `SetupEvents` 先赋值（见 #10），所以这里的「字段 vs 形参」在实际调用链上等价 ——
  但**判据必须写成字段**（派生类重写时行为不同）。
- **E2 现状**：`Shell/LiveOpsHandler.cs:216-221`
- **差异**
  1. ❌ **「那一跳」猜错了**：E2 调 `ScheduleHandlerRefresh(LiveOpsEventTimeData.FromMs(ms))`，
     原版是 **`TryScheduleHandlerRefresh(ms)`**。差别是**两道门被绕过**：
     「已排得更早 ⇒ 只出声不动」与「已过点 ⇒ 丢弃并出声」。
     ⇒ 我们会在「旧排期更早」时**把已排的刷新推后**，在「已过点」时**排一个立刻到点的刷新**（原版丢弃）。
  2. ⚠️ E2 读形参 `sharedData`；原版读 `this.shared`。E2 额外 `if (sharedData == null) return;`（原版 NRE）。**可保留**，但要写清。
- **要改什么**：改调用目标为 `TryScheduleHandlerRefresh(ms)`；数据源改成 `this.shared`。

### 3. `ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete)` —— 完整

- **VA** `0x181017C80`（size `0x360`）
- **体**

```csharp
private void ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete)
{
    var c = new <>c__DisplayClass21_0 { newEvent = newEvent, onComplete = onComplete };

    string id = newEvent.GetID();                       // IIdentifiable slot 0
    if (!handlerActions.ContainsKey(id)) return;        // 没排过 ⇒ 什么都不做

    handlerActions.Remove(id);                          // Dictionary.Remove
    CustomDebug.Log(<"…"> + id);                        // 一行日志（串前置常量 + id）

    if (showExpirePopup)
    {
        var reference = LiveOpsAssetUtility.GetComponentReference(newEvent, options.PopupLabel); // [+0x10]
        var window    = SingletonBehaviour<WindowsManager>.Instance
                            .OpenWindow(reference, newEvent, closeAll: false, default(GameWindowOptions));
        if (options.MarkOnScreenClose)                  // [+0x19]，默认 true
            window.<+0x60> += () => c.newEvent.Expire(null);   // 局部函数 <ExpireEvent>b__1
        c.<ExpireEvent>g__HandleOnComplete|0();          // ← 弹窗支【立刻】走完成回调
    }
    else
    {
        newEvent.Expire(c.<ExpireEvent>g__HandleOnComplete|0);  // ← 非弹窗支把回调【交给 Expire】
    }
}

// 局部函数 1（`LiveOpsHandler.__c__DisplayClass21_0<object>__<ExpireEvent>b__1.c`）
void <ExpireEvent>b__1() => c.newEvent.Expire(null);       // ILiveOps slot 2

// 局部函数 2（`…__<ExpireEvent>g__HandleOnComplete_0.c`）
void g__HandleOnComplete()
{
    Signal.Raise(new <>f__AnonymousType0<object>(c.newEvent.GetID()));   // IIdentifiable slot 0
    if (c.onComplete != null) c.onComplete();
}
```

- **关键指令**：`0x17D97` 前 `mov rdx,[0x1842A4970]`（`IIdentifiable`）+ `xor ecx,ecx` ⇒ 取 `id`；
  `0x17DBB call 0x181750440`（`Dictionary.ContainsKey`）；`0x17DFD call 0x181770BB0`（`Dictionary.Remove`）；
  `0x17E56 call LiveOpsAssetUtility$$GetComponentReference`；`0x17E8C call WindowsManager$$OpenWindow`；
  `0x17F2C call 0x1803F3730`（写 `window+0x60`，即 `+=` 委托）；
  `0x17F46 call …g__HandleOnComplete|0`；非弹窗支 `0x17FB4 call 0x180001F90`，`ecx=2` + `ILiveOps` ⇒ `Expire(onComplete)`。
- **语义**：**只有「这张表里排过」的事件才会走过期流程**；顺序 = 出表 → 日志 → 弹窗/直接过期 → 完成回调。
- **E2 现状**：**未实现**（`LiveOpsHandler.cs` 里没有 `ExpireEvent`；`ScheduleEventPopups` 是空壳，见 `:229-244`）。
- **要改什么**：属 C1-W2；本条给出可直接照写的判据。

### 4. `GetBaseEvents(bool includeBlockedEvents = false)` —— 完整

- **VA** `0x181017FE0`（size `0x20`，**只有 5 条指令**）
- **体**

```csharp
public IEnumerable<ILiveOps> GetBaseEvents(bool includeBlockedEvents = false)
    => GetEvents(includeBlockedEvents);        // 尾跳，静态类型升到 IEnumerable<ILiveOps>
```

- **关键指令**
  ```
  0x17FE0  mov r8, [rcx]              ; klass
  0x17FE3  mov rax, [r8 + 0x2E8]      ; klass->vtable[slot 27].methodPtr
  0x17FEA  mov r8,  [r8 + 0x2F0]      ;                        .method
  0x17FF1  jmp rax                     ; ★ tail-call
  ```
  `vtable 基址 = 0x138`，每槽 16 字节 ⇒ `(0x2E8-0x138)/16 = 27` = **`GetEvents`**（dump.cs `Slot: 27`）。
  ⇒ 这一条**不是**「读不到」，而是**编译器把 `return GetEvents(b);` 编成了尾跳**。
  （G3 §4·3 把它判成「泛型分发跳板、逻辑静态拿不到」——**那个结论对 `GetBaseEvents` 这条而言是错的**，就地订正。）
- **语义**：`GetBaseEvents` = `GetEvents` 的「接口静态类型」视图，**没有独立逻辑**。
- **E2 现状**：`Shell/LiveOpsHandler.cs:440-443` —— **正确** ✅
- **差异**：无。

### 5. `GetEvent(string id)` —— 完整

- **VA** `0x181018000`（size `0x130`）
- **体**

```csharp
public virtual TEvent GetEvent(string id)
{
    var c = new <>c__DisplayClass46_0 { id = id };
    return GetEvents(false).FirstOrDefault(x => x.GetID() == c.id);   // IIdentifiable slot 0；不查 blocked
}
```

- **关键指令**：`0x1804B` 处 `new` 出 `<>c__DisplayClass46_0<TEvent>`（`[rgctx+0x178]` 是它的 Il2CppClass）；
  `0x18086 xor edx,edx; call [r8+0x2E8]` ⇒ **虚调用 slot 27 = `GetEvents(false)`**；
  末尾 `0x181122 jmp System.Linq.Enumerable$$FirstOrDefault<object>`。
  谓词实体 = `LiveOpsHandler.__c__DisplayClass46_0<object>__<GetEvent>b__0.c`：
  `String.Equals(FUN_1800021f0(0, IIdentifiable, x), c.id)`。
- **语义**：线性 `FirstOrDefault`，**只查 `events`**，没有就是 `null`（不抛、不 Log）。
- **E2 现状**：`Shell/LiveOpsHandler.cs:450-460` —— **正确** ✅（手写循环 + `e.GetID() == id`，等价）
- **差异**：E2 多一条 `string.IsNullOrEmpty(id) → null`。原版 `id=null` 时 `String.Equals(GetID(), null)` ⇒ 只有 `GetID()` 也是 null 才命中（实际永不命中）⇒ **等价**。

### 6. `GetEvents(bool includeBlockedEvents = false)` —— 完整

- **VA** `0x181018130`（size `0x80`）
- **体**

```csharp
public virtual IEnumerable<TEvent> GetEvents(bool includeBlockedEvents = false)
{
    if (!includeBlockedEvents) return events;                                   // ★ 原样返回【字段本身】（活引用）
    return events.Concat<ILiveOps>(blockedEvents).Cast<TEvent>();               // 懒式 Concat + Cast
}
```

- **关键指令**：`0x181164 mov rax,[rbx+0x20]`（events，直接作返回值）；分支 `0x181177 mov rdx,[rbx+0x28]` →
  `0x18117B call Enumerable$$Concat<object>` → `0x181195 call Enumerable$$Cast<object>`。
- **语义**：`false` 支返回**同一个 List 对象**（调用方改它 = 改 handler 的 events）；
  `true` 支是**延迟求值**的 `Concat + Cast`（元素不是 `TEvent` 会抛 `InvalidCastException`）。
- **E2 现状**：`Shell/LiveOpsHandler.cs:419-426`
- **差异**：E2 在 `true` 支**新建 List 并复制**（立即求值、不抛 Cast）。内容等价，**语义差异只有「延迟 vs 立即」**。
- **要改什么**：**可不改**；建议把注释从「原版是同义」改成「原版 = `Concat + Cast<TEvent>`（懒式），我们是立即拷贝（有意差异）」。

### 7. `HandleBeforeRefresh()` —— 完整

- **VA** `0x1810181B0`（size `0xA0`）
- **体**

```csharp
private void HandleBeforeRefresh()
{
    CustomDebug.Log("[" + this.HandlerKey + "] Before Refresh");   // HandlerKey = 抽象属性，vtable slot 20 (0x278)
    if (OnBeforeRefresh != null) OnBeforeRefresh();                // Action 调用
    this.BeforeRefresh();                                          // 虚方法 slot 24 (0x2B8)
}
```

- **关键指令**：`0x1811E7 mov rax,[rdx+0x278]; 0x1811EE mov rdx,[rdx+0x280]; call rax` ⇒ slot
  `(0x278-0x138)/16 = 20` = **`get_HandlerKey`**；`0x181215 call CustomDebug$$Log`；
  `0x18121A mov rax,[rbx+0x48]`（OnBeforeRefresh）→ `call [rax+0x18]`；收尾 `jmp [rax+0x2B8]` = slot 24 = `BeforeRefresh`。
- **语义**：日志 → 订阅 → 虚钩子。顺序**不可换**（日志在最前）。
- **E2 现状**：`Shell/LiveOpsHandler.cs:304-310` —— **正确** ✅
- **差异**：无。

### 8. `InitializeHandler()` —— 完整

- **VA** `0x181018250`（size `0x240`）
- **体**

```csharp
public virtual Task InitializeHandler()
{
    RefreshCount = RefreshCount + 1;              // ★ 先自增（`IsFirstInitialization` = RefreshCount==0）
    foreach (TEvent e in events) e.Initialize();  // ILiveOps slot 1
    return Task.CompletedTask;
}
```

- **关键指令**：`0x1829A inc dword [rbx+0x70]`（`+0x70` = RefreshCount，**在判空之前**）；
  `0x1829D mov r9,[rbx+0x20]`（events）；循环里 `0x18374 mov r8d,1` ⇒ ILiveOps slot 1 = `Initialize`；
  收尾从静态字段取 `Task.CompletedTask`（`[Task_klass+0xb8]+0x30`）。
- **语义**：计数 → 逐事件初始化 → 完成。**没有 try/catch**。
- **E2 现状**：`Shell/LiveOpsHandler.cs:371-383` —— **正确** ✅（多一层 try/catch，可保留，记为有意差异）
- **差异**：仅异常策略。

### 9. `QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action<Action> eventAction)` —— 完整

- **VA** `0x181018490`（size `0x430`）
- **体**

```csharp
private void QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action<Action> eventAction)
{
    string id = newEvent.GetID();                                  // IIdentifiable slot 0
    if (handlerActions.ContainsKey(id)) return;                    // ★ 按【事件 id】去重，不是按对象

    var menu   = SingletonBehaviour<MenuActions>.Instance;
    var data   = newEvent.GetBaseData();                           // ILiveOps slot 15
    var filter = data.filter;                                      // LiveOpsEventData +0x30
    string[] groups = filter.eventGroups                           // EventFilter +0x28 = List<LoadableReference<EventGroup>>
                        .Select(r => r.TargetId)                   // <>c.<QueueEventPopup>b__22_0
                        .ToArray();
    bool forceRun = data.drawData.ForceRun;                        // drawData(+0x38) +0x49

    MenuActions.Action action = menu.QueueActionWithCallback(
        eventAction,
        options.ActionRule,                                        // PopupOptions +0x14
        newEvent.Priority,                                         // ★ ILiveOps slot 21 = get_Priority
        groups,                                                    // [rsp+0x20]
        options.TriggerBeforeOnboardingEnds,                       // PopupOptions +0x18
        forceRun,                                                  // EventElements.ForceRun
        false,                                                     // recurring
        /*menuFilter*/ <静态默认>);

    handlerActions.Add(id, action);
}
```

- **关键指令**：`0x18543 call FUN_1800021f0(0, IIdentifiable, newEvent)` ⇒ `id`；
  `0x18561 ContainsKey`；`0x18586 mov eax,[rbx+0x14]`（ActionRule）、`0x185B7 movzx eax,byte[rbx+0x18]`（TriggerBeforeOnboardingEnds）；
  `0x185C2 call FUN_1800021f0(0xF, ILiveOps, newEvent)`（GetBaseData）→ `0x185FF mov rax,[rax+0x30]`（filter）→
  `0x18621 mov r15,[rax+0x28]`（eventGroups）→ `0x18791 Select` → `0x187A0 ToArray`；
  `0x185DD movzx r12d,byte[[GetBaseData]+0x38]+0x49`（ForceRun）；
  `0x1859C call FUN_1800021f0(0x15=21, ILiveOps, newEvent)` ⇒ **`Priority`** → 存 `[rsp+0xA8]` → 作 `r9d`；
  `0x18862 call MenuActions$$QueueActionWithCallback`；`0x18894 call Dictionary.Add`。
- **语义**：**id 去重 → 组 key 从 `filter.eventGroups` 的 `TargetId` 取 → 排队 → 记表**。
  优先级用的是**事件自己的 `Priority`**（不是 `options` 里的任何字段）。
- **E2 现状**：**未实现**。
- **要改什么**：属 C1-W2；本条可直接照写（注意我们要先决定是否复刻 `MenuActions` 的队列，见 §四·3）。

### 10. `SetupEvents(List<ILiveOps> ops, List<ILiveOps> blockedEvents, JObject sharedData)` —— 完整

> **真身不在这个方法里**：本方法只 `builder.Start(ref stateMachine)`；
> 全部逻辑在 `LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`（182 行，**Ghidra 完整产物**）。

- **VA** `0x181019390`（size `0x80`，只是 async 壳）
- **体**

```csharp
public Task SetupEvents(List<ILiveOps> ops, List<ILiveOps> blockedEvents, JObject sharedData)
{
    this.shared = sharedData ?? new JObject();                      // ★ 先赋 shared（+0x30）
    this.events = ops.Cast<TEvent>()                                // Enumerable.Cast（不是 OfType ⇒ 类型不对会抛）
                     .OrderByDescending(x => x.GetBaseData().Priority)   // ★★ 降序，key = priority
                     .ToList();                                    // 换【新 List】
    this.blockedEvents = blockedEvents;                             // ★ 直接替换引用（不排序、不筛、不判空）

    await SetupEvents(this.events);                                 // 虚方法 slot 25（基类 = Task.CompletedTask）
    DeserializeSharedData(this.shared);                             // 虚方法 slot 22
    ScheduleEventPopups();
}
```

- **关键指令**（来自 `_SetupEvents_d__18__MoveNext.c`）
  ```
  lVar7 = param_1[10] (= stateMachine.sharedData @0x28)
  if (lVar7 == 0) lVar7 = new JObject();
  plVar1[6] = lVar7                                  ; this.shared (+0x30)
  System_Linq_Enumerable__Cast<object>(ops, …)       ; Cast
  FUN_180c99380(cast, lVar7, rgctx[0x48])            ; ★ 0xC99380 = Enumerable.OrderByDescending<object,int>
  System_Linq_Enumerable__ToList<object>(…)          ; ToList
  plVar1[4] = <list>                                 ; this.events (+0x20)
  plVar1[5] = param_1[0xE]                            ; this.blockedEvents (+0x28) ← 直接赋值
  (*(*plVar1 + 0x2C8))(plVar1, plVar1[4], …)         ; slot 25 = SetupEvents(List<TEvent>)
  …await…
  (*(*plVar1 + 0x298))(plVar1, plVar1[6], …)         ; slot 22 = DeserializeSharedData
  LiveOpsHandler__ScheduleEventPopups(plVar1, …)
  ```
  **排序键的实体** = `LiveOpsHandler.__c_object____SetupEvents_b__18_0.c`：
  `i = ((ILiveOps)x).GetBaseData(); return *(int*)(i + 0x40);` ⇒ **`LiveOpsEventData.priority`**（`+0x40` 已由 §0·5 坐实）。
- **语义**：**按 `Priority` 降序**装事件；`blockedEvents` **原样换引用**；再走「子类装载 → 解析 shared → 排弹窗」三段。
- **E2 现状**：`Shell/LiveOpsHandler.cs:188-206`
- **差异**
  1. ❌ **排序键错**：E2 用 `GetID()` 的序数升序；原版是 **`Priority` 降序**（`LiveOpsEventData.priority`）。
  2. ⚠️ E2 `events.Clear() + AddRange`（复用同一 List）；原版是**赋一个新 List**。低风险（没人缓存这个 List 的引用时无差）。
  3. ⚠️ E2 `blockedEvents.Clear() + AddRange`；原版**直接赋引用**。低风险，可保留，但要写清。
  4. ✅ 三段顺序（`await SetupEvents` → `DeserializeSharedData` → `ScheduleEventPopups`）**已对**。
- **要改什么**：只改排序键（§三 #1）。

### 11. `UpdateCachedSave(LiveOpsEventPersistence saveGame)` —— 完整

- **VA** `0x181019CB0`（size `0x130`）
- **体**

```csharp
public void UpdateCachedSave(LiveOpsEventPersistence saveGame)
{
    var c = new <>c__DisplayClass9_0 { saveGame = saveGame };
    RawEvent e = handlerCache.RawEvents                                   // +0x60 → LiveopsHandlerData +0x18
                    .FirstOrDefault(x => x.Persistence != null            // RawEvent +0x18
                                      && x.Persistence.eventId == c.saveGame.eventId);
    if (e != null) e.Persistence = c.saveGame;
}
```

- **关键指令**：`0x19D56 mov rdi,[rdi+0x60]`（handlerCache）→ `0x19D66 mov rdi,[rdi+0x18]`（RawEvents）→
  `0x19D9C call Enumerable$$FirstOrDefault<object>` → 命中则 `0x19DAE mov [rax+0x18], rdx`（写回 `Persistence`）。
  谓词实体 = `LiveOpsHandler.__c__DisplayClass9_0<object>__<UpdateCachedSave>b__0.c`：
  `String.Equals([[rawEvent+0x18]+0x10], [[c.saveGame]+0x10])` ⇒ **比 `Persistence.eventId`**。
- **语义**：按**事件 id** 在缓存里找到那一条 `RawEvent`，把它的 `Persistence` 换成新存档。
- **E2 现状**：`Shell/LiveOpsHandler.cs:470-482`
- **差异**
  1. ❌ **比错字段**：E2 用 `r.Data.eventId`；原版用 **`r.Persistence.eventId`**。
     （多数时候两者相等，但 `Data.eventId` 是 SO 的 id、`Persistence.eventId` 是存档的 id —— 原版是**按存档 id 对齐**。)
  2. ⚠️ E2 有 `saveGame == null → return` 与 `handlerCache == null → return`（原版都是 NRE）。可保留。
- **要改什么**：`r.Persistence != null && r.Persistence.eventId == saveGame.eventId`。

---

## §二 E2 那 4 条「未坐实」的答案（全部定案）

### Q1 · `Initialize()` 里 `milis = <某 int 字段> * 3600000 + <某 long 字段>` 的**两个字段是谁**？

**答：`Data.filter.eventDuration`（int，单位=**小时**，默认 24）× 3600000ms + `SaveGame.triggerTime`（long，毫秒）。**

- 判据 = **VA 反汇编** `Everguild.LiveOps.LiveOpsEvent<object,object>$$Initialize` `0x181016E70`：

```asm
0x181016E95  mov rax,[rbx+0x18]      ; Data            (TData)
0x181016EA2  mov rcx,[rax+0x30]      ; Data.filter     (EventFilter)
0x181016EAF  cmp byte ptr [rcx+0x18],0   ; ★ filter.hasDuration   → ==0 则【直接 return】（唯一早退）
0x181016EB9  mov rcx,[rbx+0x20]      ; SaveGame        (TSave)
0x181016EC6  mov rbx,[rbx+0x10]      ; Handler         (ILiveOpsHandler)
0x181016ED7  imul eax, dword ptr [rdx+0x1C], 0x36EE80   ; ★ filter.eventDuration × 3,600,000
0x181016EF8  add rdi, qword ptr [rcx+0x20]              ; ★ + SaveGame.triggerTime
0x181016F31  call FUN_1803f17a0(Handler, ILiveOpsHandler, 0xA)  ; slot 10 = TryScheduleHandlerRefresh
```

```csharp
public override void Initialize()
{
    if (!Data.filter.hasDuration) return;                  // ★ 唯一早退条件（时长型活动才排期）
    long milis = (long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime;
    Handler.TryScheduleHandlerRefresh(milis);
    // ⚠️ Data / Data.filter / SaveGame / Handler 任一为 null ⇒ 原版是隐式 NRE（没有空判）
}
```

- 三重印证：`EventFilter.eventDuration` 默认值 **24**（`EventFilter.cs:31`）· `EventFilter.hasDuration`（`:29`）·
  同一公式在 `EventFilter.GetExpirationTime(ILiveOps)` 里**逐字重复**（`Everguild.LiveOps.EventFilter__GetExpirationTime.c`：
  `return (long)(*(int*)(param_1+0x1C) * 3600000) + *(long*)(save + 0x20);`，其中 `save = ev.GetBaseSave()` slot 16）。
- **E2 现状**：`Shell/LiveOpsEvent.cs:304-314`（调用点 `:313`） 用 `SaveGame.triggerTime` 单值调度 ⇒ **缺 `eventDuration*3600000` 这一项**。

### Q2 · `CreateNewSave(TData data)` 的**初值从哪来**？

**答：`Activator.CreateInstance<TSave>()` 的默认构造（字段初始化器 = `IsNew = true` 已生效）+ 只抄一格 `save.eventId = data.GetID()`。**

- 判据 = `0x181016CA0`：

```asm
0x181016CBC  call System.Activator$$CreateInstance<object>       ; rgctx[0x70] = TSave
0x181016CCF  mov r8,[rdx+0x188]  ; vtable slot (0x188-0x138)/16 = 5 → TData 的虚方法
0x181016CDD  call r8                                              ; ← 返回 string
0x181016CEC  mov [rcx], rax      ; save + 0x10 = LiveOpsEventPersistence.eventId
0x181016CF4  mov rax, rbx        ; return save
```

```csharp
public TSave CreateNewSave(TData data)
{
    TSave save = Activator.CreateInstance<TSave>();
    save.eventId = data.GetID();      // = data.eventId（LiveOpsEventData.GetID() 实体就是 return [this+0x18]）
    return save;                      // 其余（IsNew/WasSeen/IsActive/triggerTime/expiredTime）全是默认值
}
```

- 印证：`Everguild.LiveOps.LiveOpsEventData__GetID.c` = `return *(undefined8*)(param_1 + 0x18);`，
  而 `LiveOpsEventData` 的 `+0x18` 正是 `eventId`（§0·5）。
- 🔴 **`triggerTime` 不来自 `data.dates.startDate`** —— 新存档的 `triggerTime = 0`。谁写它？`UpdateData/UpdateSaveGame/RefreshExpire`
  那一路（本轮只顺带读了 `UpdateData`/`UpdateSaveGame`，见 §一·附）。⛔ 别在 `CreateNewSave` 里塞起始时间。
- **E2 现状**：`Shell/LiveOpsEvent.cs:380-390` —— `s.eventId = data.eventId` **等价 ✅**；
  多写的 `s.IsNew = true` 是冗余（无害）。**只需把「未坐实」注释改成「已坐实」。**

### Q3 · `SetupEvents` 的**排序键**？

**答：`x.GetBaseData().priority`（= `ILiveOps.Priority`），`OrderByDescending` —— 降序。**

- 判据（两条独立）：
  1. **`call 0x180c99380` 的 `script.json` 反查 = `System.Linq.Enumerable.OrderByDescending<object,int>`**
     （同址还有 `<KeyValuePair<object,int>,int>` / `<ValueTuple<Int32Enum,int,int>,int>`，都是降序整型 key）；
  2. **key selector 实体** `LiveOpsHandler.__c_object____SetupEvents_b__18_0.c`：
     `lVar2 = x.<ILiveOps slot 15>()`（=`GetBaseData`，已由 `[drawData@0x38]`、`[priority@0x40]` 双印证）
     → `return *(int*)(lVar2 + 0x40);`
- **E2 现状**：`Shell/LiveOpsHandler.cs:198-203` 用 `string.CompareOrdinal(GetID(), GetID())`（升序）⇒ **要改**。

### Q4 · `DeserializeSharedData` 的**那一跳**？

**答：`TryScheduleHandlerRefresh(shared["NextRefresh"].Value<long>())`** ——
不是 `ScheduleHandlerRefresh(DateTime)`；而且**数据源是字段 `this.shared`**，不是形参。

- 判据：`Method not found @181019810` → `script.json` 的 `Address 0x1019810` = **`LiveOpsHandler<object>$$TryScheduleHandlerRefresh`**；
  反汇编 `0x181017C5B call 0x181019810` 与 `0x181017C08 mov rcx,[rbx+0x30]`（`this.shared`）见 §一 #2。
- **E2 现状**：`Shell/LiveOpsHandler.cs:216-221` ⇒ **要改**（见 §三 #4）。

---

## §三 要改代码的清单（`文件:行号 | 现在怎样 | 应改成 | 判据 | 风险`）

| # | 文件:行号 | 现在怎样 | 应改成 | 判据 | 风险 |
|---|---|---|---|---|---|
| **1** | `Shell/LiveOpsHandler.cs:191-203`（`Sort` 在 `:197`） | `sorted.Sort((a,b)=>string.CompareOrdinal(a.GetID(), b.GetID()))`（**升序、按 id**） | `ops.Cast<ILiveOps>().OrderByDescending(x => x.GetBaseData()==null?0:x.GetBaseData().Priority).ToList()` | `OrderByDescending<object,int>`（`script.json` RVA `0xC99380`）+ `b__18_0` 读 `LiveOpsEventData+0x40` | **低**（只影响 `events` 的次序；弹窗排队顺序会变，属预期） |
| **2** | `Shell/LiveOpsHandler.cs:216-222`（调用点 `:221`） | `ScheduleHandlerRefresh(LiveOpsEventTimeData.FromMs(ms))` | `TryScheduleHandlerRefresh(ms)` | `0x17C5B call 0x181019810` | **中**：会启用「已排更早⇒不动」「已过点⇒丢弃」两道门；原来可能把刷新推后/排到立刻 |
| **3** | `Shell/LiveOpsHandler.cs:218` | 读形参 `sharedData` | 读字段 `this.shared`（形参保留、不用） | `0x17C08 mov rcx,[rbx+0x30]` | **低**（`SetupEvents` 已先赋 `shared`，实际等价；但派生类重写时会分叉） |
| **4** | `Shell/LiveOpsHandler.cs:389-399` | `handlerActions.Clear()` 后只对 `events` 逐个 `Dispose` | ① 先 `foreach(a in handlerActions.Values) MenuActions.ReleaseAction(a)`；② **`blockedEvents` 也要逐个 `Dispose`** | `0x176A3` 起（`ReleaseAction`）+ `0x17AEA mov r8d,3`（blockedEvents 也走 slot 3 = Dispose） | **中**：① 需先有 `MenuActions` 等价物（§四·3）；② 独立可做、低风险 |
| **5** | `Shell/LiveOpsHandler.cs:403-415` | `foreach(events) Dispose()` + `events.Clear()` + 日志 | **`foreach (var e in events) e.ResetEventProgress();`（只这一句；不清空、不 Dispose）** | `ResetEvents` `0x181018B90`：只读 `[this+0x20]`、循环体 `mov r8d,7`（ILiveOps slot 7 = `ResetEventProgress`）、**无任何 `List.Clear`** | **中高**：语义整条不同 ⇒ 现有调用点若依赖「清空」会断（须自查调用方） |
| **6** | `Shell/LiveOpsHandler.cs:478` | `if (r == null \|\| r.Data == null) continue; if (r.Data.eventId != saveGame.eventId) continue;` | `if (r == null \|\| r.Persistence == null) continue; if (r.Persistence.eventId != saveGame.eventId) continue;` | `…DisplayClass9_0…UpdateCachedSave_b__0.c`：`String.Equals([[x+0x18]+0x10], …)`，`RawEvent.Persistence` = `+0x18` | **低** |
| **7** | `Shell/LiveOpsEvent.cs:304-314`（调用点 `:313`） | `Handler.TryScheduleHandlerRefresh(SaveGame.triggerTime)` | `if (!Data.filter.hasDuration) return;` 然后 `Handler.TryScheduleHandlerRefresh((long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime)` | `0x181016E70`（§二 Q1） | **低**（`hasDuration` 为 true 时才多算一项；我们的 `EventFilter` 等价字段要对齐 `eventDuration`） |
| **8** | `Shell/LiveOpsEvent.cs:376-390` | 注释写「初值取哪个成员没坐实」 | 改成「**已坐实**：`save.eventId = data.GetID()`；`triggerTime` 不在这里写」 | `0x181016CA0` + `LiveOpsEventData__GetID.c` | **零**（只改注释） |
| **9** | `Shell/LiveOpsEvent.cs:393-404` | `var d = data as TData; if (d==null) 出声并保留旧 Data` | 原版 = **`if (data.GetType() != typeof(TData)) return;`** 再 `Data = (TData)data`（子类会被**忽略**，不是接受） | `0x181017250`：`GetType()` → `Type.op_Inequality(typeof(TData))` → `jne <return>` | **低**（我们目前没有子类 TData；但口径要写对） |
| **10** | `Shell/LiveOpsHandler.cs:419-426` | 注释写「原版是同义」 | 改成「原版 = `Concat<ILiveOps>(blocked).Cast<TEvent>()`（**懒式、会抛**）；我们立即拷贝（有意差异）」 | `0x181018130` | **零**（只改注释） |
| **11** | `Shell/LiveOpsHandler.cs:229-244` `ScheduleEventPopups` | 只建去重表、出声 | 原版真身（cpp2il **有体**，SEV4）→ 逐事件看 `drawData.ShowWindowOnTrigger` / `ShowWindowOnExpire` 调 `ShowTriggerEventPopup` / `ExpireEvent` | `LiveOpsHandler.cs:1606-…`（cpp2il）+ `ShowTriggerEventPopup` `0x181019560` | 属 C1-W2，**本轮只指路、未逐条反汇编** |
| **12** | `Shell/LiveOpsHandler.cs:371-383` `InitializeHandler` | 每个事件 `try/catch` | 原版无 catch | `0x181018250` | **零**（有意差异，保留 + 写清） |

> ⚠️ **本轮不改任何 `.cs`**（R7 只写判据/报告）。上表交给执行代理。

---

## §四 没查清（写清「还差什么」）

1. **`ExpireEvent` 弹窗支的 `OpenWindow` 第 4/5 参**：只读到 `closeAll: false` + 一个 `default` 结构；
   `WindowsManager.OpenWindow` 的完整重载表在 `decomp_full` 里是多份同形产物 ⇒ **未逐条对签名**。
   （不影响判定：「开一扇 `options.PopupLabel` 指向的窗、把 `newEvent` 当 data 传」这一层是坐实的。）
2. **`g__HandleOnComplete` 里 `Signal.Raise(new <>f__AnonymousType0<object>(id))` 那个匿名类型是什么**：
   只读到「new 一个单参匿名类型 + `Scaffold.Core.Events.Signal.Raise<T>`」。
   **还差**：`<>f__AnonymousType0` 的成员名（`LiveOpsHandlerRefreshedSignal` 只有 `Handler` 一格，形状不符）。
3. **`MenuActions` 是否要复刻**：`handlerActions` 的值类型原版是 `MenuActions.Action`（`QueueActionWithCallback` 返回的自定义类），
   而 E2 用的是 `System.Action`。要接 `MenuActions.ReleaseAction` 就必须先决定 `MenuActions` 队列本身做不做。
   **还差**：调度台拍板（属「C1-W2 弹窗排期」批次的边界）。
4. **`ScheduleEventPopups` 的真身**（cpp2il 有体、SEV4、41 行注记）**本轮没逐条反汇编** ——
   它不是「11 个无产物方法」之一，属 C1-W2。已给出入口地址与会话归属。
5. **`ExpireEvent` 弹窗支「先 `HandleOnComplete()` 再等窗口关」这一反常次序**：
   反汇编是这么排的（`0x17F31` 是 `+=` 之后的**顺序执行**），但**为什么这样设计**未查（可能是「过期先结算、窗口只是告知」）。
   ⛔ 别按「关闭后才回调」去改。

---

## §五 顺手发现（值钱、但不在本轮活里）

1. 🔴 **G3 §4·3 的定性对 `GetBaseEvents` 这条是错的，就地订正**：它写「泛型分发跳板，逻辑静态拿不到」。
   实际 `0x2E8/0x2F0` 是 **`klass->vtable[slot 27]` 的 `{methodPtr, MethodInfo}`**（`vtable 基址 = 0x138`、每槽 16 字节），
   slot 27 = **`GetEvents`** ⇒ `GetBaseEvents` = `return GetEvents(b);`（编译器编成尾跳）。**逻辑完全可读。**
2. 🔴 **`ResetEvents` 的语义与名字直觉相反**：它**不改列表**，只对每个事件调 `ResetEventProgress()`（slot 7）。
   「清空列表」是 `ClearEvents` 的事。E2 当前实现（Dispose + Clear）**两头都错**。
3. 🔑 **`FUN_1800021F0(idx, iface, obj)` / `FUN_1803F17A0(obj, iface, idx)` 的 `idx` 就是 dump.cs 的 `Slot`** ——
   以后再遇到「`Method not found` 的接口调用」，**查 dump.cs 的 `Slot:` 列一次就够**，不必再逐条猜。
   本轮用它产出的 9 个印证点全部自洽（§0·4）。
4. 🔑 **`klass` 的 vtable 基址 = `0x138`、每槽 16 字节** —— 于是 `[klass+0xNNN]` 这类偏移可以**直接换算成「第几号虚方法」**，
   再对 dump.cs 的 `Slot:` 列。本轮靠这条解开了 `HandleBeforeRefresh`（`0x278`=slot 20=`HandlerKey`、`0x2B8`=slot 24=`BeforeRefresh`）、
   `_SetupEvents` 状态机（`0x2C8`=slot 25、`0x298`=slot 22）、`UpdateSaveGame`（`0x3F8`=slot 44=`LoadEventData`）。
5. 🔴 **`MenuActions.ReleaseAction` 是全局静态**（`public static void ReleaseAction(Action a) => actions.Remove(a)`）——
   「排期表清空」≠「菜单队列撤销」。这是**两个口**，`ClearEvents` 两个都做。
6. 🔴 **`DeserializeSharedData` 的形参根本没用**（读的是字段 `this.shared`）—— 这是原版的一处「形参残留」，
   照抄时**必须写成字段**，否则派生类重写后行为会分叉。
7. 🟢 **同一条公式跨两个类**：`EventFilter.GetExpirationTime(ev)` 与 `LiveOpsEvent.Initialize()` 都是
   `filter.eventDuration * 3600000 + ev.GetBaseSave().triggerTime`。⇒ **「活动到期/下次刷新时刻」只有这一条算式**，
   可以当**验收尺子**：`hasDuration` 的活动，`Initialize` 排的点必须等于 `GetExpirationTime`。
8. 🟡 **`LiveOpsEvent<TSave,TData>`（泛型基类）整类的普通方法在 `decomp_full` 里也没有产物**
   （`Everguild.LiveOps.LiveOpsEvent__*` 只有**非泛型**基类的 27 条）。
   ⇒ 与 G3 报的「LiveOpsHandler 11 条缺失」**同族**：缺的是「**泛型类的普通方法**」这一类，不是某一个类。
   建议补导 `0x1016000`–`0x101A600` 整段（本轮要的 `Initialize`/`CreateNewSave`/`UpdateData`/`UpdateSaveGame` 都在里面）。

---

## §六 本次用到的坐标（复查入口）

| 用途 | 坐标 |
|---|---|
| `script.json`（RVA 真值） | `D:/2/tools/il2cpp_out/script.json`（`ScriptMethod[].Address` = RVA，`VA = 0x180000000 + Address`） |
| 二进制 | `D:/2/unity_run_ref/GameAssembly.dll` |
| IL 骨架 | `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LiveOpsHandler.cs`（3,504 行） |
| async 真身 | `D:/2/tools/decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`（182 行） |
| key selector | `D:/2/tools/decomp_full/LiveOpsHandler.__c_object____SetupEvents_b__18_0.c` |
| `UpdateCachedSave` 谓词 | `D:/2/tools/decomp_full/LiveOpsHandler.__c__DisplayClass9_0_object____UpdateCachedSave_b__0.c` |
| `ExpireEvent` 两个局部函数 | `…__c__DisplayClass21_0_object____ExpireEvent_b__1.c` · `…g__HandleOnComplete_0.c` |
| 接口槽表（权威） | `D:/2/tools/il2cpp_out/dump.cs` → `ILiveOps`(TypeDefIndex 3395) / `ILiveOpsHandler`(1225) / `IIdentifiable`(1267) / `IContainerProvider`(2915) 的 `Slot:` 列 |
| 复跑脚本 | `d:/4/Unity/_tmp_view/r7/{dis11,dis_liveopsevent,dis_helpers}.py` |
