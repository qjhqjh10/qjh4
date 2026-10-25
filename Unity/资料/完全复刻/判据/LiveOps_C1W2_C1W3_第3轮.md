# LiveOps · C1-W2（弹窗排期）+ C1-W3（接 5 扇活动窗）—— 第 3 轮判据（真身已拿到 / 已落地部分）

- 生成：2026-10-26 · 批次 3 · 执行代理 **Q3**
- 输入：`判据/LiveOpsHandler_11方法_第2轮.md`（R7，654 行）· `报告/R7_…` · `报告/R12_LiveOps判据落地_第2轮.md` · `报告/R14_LiveOps数据与调度_第2轮.md`
- 新取证（本轮）：`d:/2/tools/decomp_generic_trial/out/`（G7 全量泛型导出）里**已有** `LiveOpsHandler<object>` 的四个方法产物；
  两个 lambda 在 `d:/2/tools/decomp_full/LiveOpsHandler.__c__DisplayClass19_0_object____ScheduleEventPopups_b__{0,1}.c`。
  ⇒ **C1-W2 不再「差一次 VA 反汇编」**（`ScheduleEventPopups` 的真身见 §一）。
- 落地：**已改** `Shell/LiveOpsHandler.cs` / `Shell/LiveOpsEvent.cs`（typecheck 0/0）。落地/未接边界见 §四。

---

## §一 `ScheduleEventPopups` 真身（VA `0x181018D60`）—— 逐句照抄

**证据链**（三层齐）：
1. **Ghidra 产物**：`decomp_generic_trial/out/LiveOpsHandler_object___ScheduleEventPopups__1018d60.c`（真身，控制流完整）；
2. **cpp2il 骨架**：`cpp2il/out_cpp2il_dev_ilspy/LiveOpsHandler.cs:1606`（方法体可读的那部分 + `Method not found @181019560 / @181017C80` 指明两个 lambda 的去向）；
3. **两个 lambda 实体**（`decomp_full/…<ScheduleEventPopups>b__0.c` / `b__1.c`）：
   - `b__0(Action onComplete)` = `ShowTriggerEventPopup(newEvent, drawData.TriggerPopupOptions(+0x30), onComplete)`
   - `b__1(Action onComplete)` = `ExpireEvent(newEvent, drawData.ExpirePopupOptions(+0x40), drawData.ShowWindowOnExpire(+0x38), onComplete)`

### 1·1 伪码（真身）

```csharp
private void ScheduleEventPopups()
{
    foreach (TEvent newEvent in events)                       // this.events (+0x20)
    {
        var drawData = newEvent.GetBaseData().drawData;       // ILiveOps slot 15 → LiveOpsEventData +0x38
        if (drawData == null) throw NRE;                      // 隐式（原版无空判）

        if (drawData.ShowWindowOnTrigger                      // EventElements +0x28
            && !newEvent.GetBaseSave().WasSeen                // ILiveOps slot 16 → LiveOpsEventPersistence +0x19
            && !handlerActions.ContainsKey(newEvent.GetID())) // IIdentifiable slot 0；+0x38
        {
            QueueEventPopup(newEvent, drawData.TriggerPopupOptions,
                onComplete => ShowTriggerEventPopup(newEvent, drawData.TriggerPopupOptions, onComplete));
            continue;                                         // ★ 排了触发弹窗 ⇒ 不再看过期支
        }

        if (newEvent.EventState == LiveOpState.Finished)       // ILiveOps slot 23 == 3（Finished，不是 Expired=4）
        {
            QueueEventPopup(newEvent, drawData.ExpirePopupOptions,
                onComplete => ExpireEvent(newEvent, drawData.ExpirePopupOptions, drawData.ShowWindowOnExpire, onComplete));
        }
    }
}
```

三个**必须照抄**的点：
1. 触发支三道闸 = `ShowWindowOnTrigger && !WasSeen && !已排过（按事件 id 去重）`；
2. **过期支判据是 `EventState == Finished(3)`**（不是 `Expired(4)`）—— 反汇编是 `FUN_1800021f0(0x17 = ILiveOps slot 23 = get_EventState)` 后 `== 3`；
3. 触发支排上就 `continue` ⇒ **同一事件不会同时排触发弹窗和过期弹窗**。

### 1·2 `QueueEventPopup`（VA `0x181018490`）

```csharp
private void QueueEventPopup(ILiveOps newEvent, PopupOptions options, Action<Action> eventAction)
{
    if (handlerActions.ContainsKey(newEvent.GetID())) return;   // 按事件 id 去重
    var menu   = SingletonBehaviour<MenuActions>.Instance;
    var data   = newEvent.GetBaseData();
    var filter = data.filter;                                   // LiveOpsEventData +0x30
    string[] groups = filter.eventGroups.Select(r => r.TargetId).ToArray();   // EventFilter +0x28
    bool forceRun = data.drawData.ForceRun;                     // EventElements +0x49
    var action = menu.QueueActionWithCallback(
        eventAction,
        options.ActionRule,                                     // PopupOptions +0x14
        newEvent.Priority,                                      // ILiveOps slot 21 = get_Priority（**事件自己的**，不是 options）
        groups,
        options.TriggerBeforeOnboardingEnds,                    // +0x18
        forceRun,
        false,                                                  // recurring
        /*menuFilter*/ <static default = null>);
    handlerActions.Add(newEvent.GetID(), action);               // Dictionary.Add
}
```

### 1·3 `ShowTriggerEventPopup`（VA `0x181019560`）

```csharp
private void ShowTriggerEventPopup(ILiveOps newEvent, PopupOptions options, Action onComplete)
{
    if (!handlerActions.ContainsKey(newEvent.GetID())) return;   // 没排过 ⇒ 什么都不做
    handlerActions.Remove(newEvent.GetID());
    CustomDebug.Log(<"…"> + newEvent.GetID());
    if (options != null)
    {
        var reference = LiveOpsAssetUtility.GetComponentReference(newEvent, options.PopupLabel);   // +0x10
        var window    = SingletonBehaviour<WindowsManager>.Instance
                            .OpenWindow(reference, newEvent, closeAll: false, default);
        if (options.MarkOnScreenClose)                           // +0x19
            window.<+0x60> += () => newEvent.MarkAsSeen();       // ILiveOps slot 12；+0x60 = GameWindow.OnCloseWindow
        if (onComplete != null) onComplete();                    // ★ 开窗后【立刻】回调（不等关窗）
    }
}
```

### 1·4 `ExpireEvent`（VA `0x181017C80`）

```csharp
private void ExpireEvent(ILiveOps newEvent, PopupOptions options, bool showExpirePopup, Action onComplete)
{
    if (!handlerActions.ContainsKey(newEvent.GetID())) return;
    handlerActions.Remove(newEvent.GetID());
    CustomDebug.Log(<"…"> + newEvent.GetID());

    if (!showExpirePopup) { newEvent.Expire(HandleOnComplete); return; }   // ILiveOps slot 2

    if (options != null)
    {
        var reference = LiveOpsAssetUtility.GetComponentReference(newEvent, options.PopupLabel);
        var window    = WindowsManager.Instance.OpenWindow(reference, newEvent, closeAll: false, default);
        if (options.MarkOnScreenClose)
            window.<+0x60> += () => newEvent.Expire(null);
        HandleOnComplete();                                     // ★ 弹窗支【立刻】走完成回调（不等关窗）—— ⛔ 别改成关窗后
    }
}
// HandleOnComplete = Signal.Raise(new EventExpiredSignal(id)); onComplete?.Invoke();
```

> ⚠️ **反常次序**（判据件 §四·5）：弹窗支开窗后**立刻**走完成回调，不是等窗口关。反汇编确实如此，**⛔ 别按「关闭后才回调」改**。
> 两处「立刻回调」的原因未查（推测：过期/触发先结算、窗口只是告知），但**行为照抄**。

### 1·5 `ClearEvents` 的两个口（判据件 §一 #1 / §五·5）

```csharp
foreach (var a in handlerActions.Values) MenuActions.ReleaseAction(a);   // ① 撤【全局静态】菜单队列
foreach (var e in events)        e.Dispose();
foreach (var e in blockedEvents) e.Dispose();                            // ③ 封锁表也 Dispose
events.Clear(); blockedEvents.Clear(); handlerActions.Clear();           // ④ 三段清空
```
🔴 `MenuActions.ReleaseAction` 是**全局静态**（`actions.Remove(action)`）⇒「排期表清空」≠「菜单队列撤销」，**两个口都要做**。

---

## §二 C1-W2 的数据判据（13 份 SO 真值，R14 已抄进目录层）

| 字段 | 原版偏移 | 13 份 SO 真值 | 谁读 |
|---|---|---|---|
| `EventElements.ShowWindowOnTrigger` | +0x28 | Parent 事件 = 1，其余 0 | `ScheduleEventPopups` 触发支 |
| `EventElements.TriggerPopupOptions` | +0x30 | `{PopupLabel 20, ActionRule 1, TriggerBeforeOnboardingEnds 0, MarkOnScreenClose 1}`（13/13 相同） | `QueueEventPopup` / `ShowTriggerEventPopup` |
| `EventElements.ShowWindowOnExpire` | +0x38 | Scoring 事件 = 1，其余 0 | `ExpireEvent` 的 `showExpirePopup` |
| `EventElements.ExpirePopupOptions` | +0x40 | `{PopupLabel 21, 同上}`（13/13 相同） | `QueueEventPopup` / `ExpireEvent` |
| `EventElements.HideContainer` | +0x48 | 13/13 = 0 | C1-W4 主菜单容器（暂无读者） |
| `EventElements.ForceRun` | +0x49 | 13/13 = 0 | `QueueEventPopup` → `MenuActions` |
| `EventFilter.eventGroups` | +0x28 | **有值**（每组 `TargetId`） | `QueueEventPopup` 的组去重 —— ⚠️ **本地目录层没抄** |

- 真值表 → `报告/R14_LiveOps数据与调度_第2轮.md` §4·3（逐份现读，双重校验 13/13）。
- `PopupLabel` = `EventComponentReferenceType` 的取值（触发 20 / 过期 21）；原版用它 `LiveOpsAssetUtility.GetComponentReference(ev, label)` 解析弹窗 prefab。

---

## §三 C1-W3「接 5 扇活动窗」—— 判据 + 数据缺口（**如实：数据受阻，不是没做**）

### 3·1 五扇窗与它们的类（本项目已建）

| 活动 | 窗口类 | 现在的开窗入口 |
|---|---|---|
| 排位 Ranked | `Shell/RankedEventWindow : LiveOpsEventWindow` | `MainMenuRuntime.cs:2677` `WindowsManager.OpenByRef("RankedEventWindowV2", …)` |
| 遭遇 Skirmish | `Shell/SkirmishEventWindow : LiveOpsEventWindow` | `MainMenuRuntime.cs:2671` `WindowsManager.OpenByRef("SkirmishModeEventWindow", …)` |
| Raid 团队 | `Shell/RaidEventWindow : GameWindow` | `WindowsManager.OpenRaidEvent()` / `OpenEventHub(EventWindowKind.Raid)` |
| Expansion Pass 战令 | `Shell/ExpansionPassEventWindow : GameWindow` | `WindowsManager.OpenExpansionPassEvent()` / `OpenEventHub(…ExpansionPass)` |
| Energy 单机 | `Shell/EnergySinglePlayerOnlyEventWindow : GameWindow` | `WindowsManager.OpenEnergyEvent()` |

### 3·2 原版那条链（判据 → `Shell/WindowsManager.cs` `OpenEventHub` 的头注，逐句现读）

`NavigationPanelController.BuildEventButtons`（每个 live 事件 instantiate 一颗钮）→ `OpenEventButton.ButtonClick`
→ `WindowsManager.OpenWindow(LiveOpsAssetUtility.GetComponentReference(ev, 10 = MainWindow), ev)`
→ 那颗钮是 toggle，关回来走 `ToggleValueChange(false)` → 窗的 `Close()`。
即：**「事件实例 → 它的 MainWindow prefab」是唯一映射**（10 = `EventComponentReferenceType.MainWindow`）。

### 3·3 为什么「接」现在受阻（数据，不是设计）

1. **本地 13 份 SO 没有这 5 类事件**：`LiveOpsLocalCatalog.Events` 的全是 `Parent` / `Scoring` / `PremiumItemOffer`
   （`Kind ∈ {Parent=0, Scoring=1, PremiumItemOffer=2}`；`FastMode=3` 的 `FastModeEvents` 表**空**）。
   ⇒ 排位 / 遭遇 / Raid / ExpansionPass / Energy 这 5 类**一个本地实例都没有**（判据 → `LiveOpsLocalCatalog.cs` 的 `LiveOpsLocalKind` 注释 + `FastModeEvents` 空表）。
2. **`EventComponents` 只解到 GUID，没解到 prefab 名**：`LiveOpsLocalEvent.Components` = `key(EventComponentReferenceType) → GUID`，
   而 `LiveOpsAssetUtility.GetComponentReference` 要把 (ev, label) 解析成 **prefab 引用**。GUID→prefab 名这一步**没做**（R12/R14「没查清」）。
3. `GetComponentReference(ev, 10)` 的 `10 = MainWindow` 已核（`OpenEventHub` 头注），但**没有**「事件 → 哪扇窗类」的静态表可查 ——
   原版靠 prefab 引用，我们只能按 `Kind` 猜。⛔ 不猜（铁律 2）。

### 3·4 能落地的「接」是什么（判据 / 改动请求，⛔ 不在本代理白名单）

- **本代理已落地的缝**：`LiveOpsPopupPresenter.Open(ev, options) → GameWindow`（`Shell/LiveOpsHandler.cs` 文件尾）——
  `ShowTriggerEventPopup` / `ExpireEvent` 把「解析并开窗」这一跳交给它；**装上真实解析器即接通**，空则出声不出窗。
- **改动请求 ①（`Shell/WindowsManager.cs`，别人白名单）**：加一个 `OpenEventMainWindow(ILiveOps ev)`，
  内部走 `GetComponentReference(ev, 10)` 的等价物（GUID→prefab）→ `OpenWindow`。
- **改动请求 ②（`Shell/LiveOpsLocalCatalog.cs` 数据侧 / 远端 C1-W4）**：把 `EventComponents` 的 GUID 经
  `Unity/数据/索引/guid_map.tsv` 解成 prefab 名（同 R12 报告 §六·1 的 priority 搬法）。
- **改动请求 ③（`Shell/LiveOpsEventWindow.cs` 族）**：窗的 `SetupData`/`LiveOp` 改吃 `ILiveOps` 实例（当前吃离线 `LiveOpsLocalEvent`）。
- **数据前提**：要有这 5 类事件的**真实例**（远端 liveops 载荷 / C1-W4）。本地 13 份 SO 全是 Parent/Scoring/Offer ⇒ **造不出**。

> 结论（铁律 11 例外①）：C1-W3 的「接」**不是「影响小不做」，是「原版判据在远端 / 数据本地没有」**。
> 可落地的那一半（presenter 缝）**已落**；完整接通要等远端事件载荷 + GUID→prefab 解析（C1-W4）。

---

## §四 本轮落地状态（`Shell/LiveOpsHandler.cs` / `Shell/LiveOpsEvent.cs`，typecheck 0/0）

| 件 | 状态 | 落点 |
|---|---|---|
| `ScheduleEventPopups` 真身 | ✅ 已落地（§一逐句） | `LiveOpsHandler.cs:274` |
| `QueueEventPopup` | ✅ 已落地 | `LiveOpsHandler.cs:334` |
| `ShowTriggerEventPopup` | ✅ 已落地 | `LiveOpsHandler.cs:378` |
| `ExpireEvent` | ✅ 已落地（含「立刻回调」反常次序） | `LiveOpsHandler.cs:419` |
| `handlerActions` 改存句柄 | ✅ `Dictionary<string, MenuAction>` | `LiveOpsHandler.cs:153` |
| `ClearEvents` ① 撤菜单队列 | ✅ `MenuActions.ReleaseAction` 逐个撤 | `LiveOpsHandler.cs:608` |
| `MenuActions` 等价物 | ✅ 全新（见 `判据/LiveOps_MenuActions_第3轮.md`） | `LiveOpsHandler.cs:1205` |
| `EventElements` / `PopupOptions` | ✅ 数据类 | `LiveOpsEvent.cs:119 / :141` |
| `LiveOpsEventData.drawData` | ✅ 字段 + `MapData` 装配 | `LiveOpsEvent.cs:170` / `LiveOpsHandler.cs` `MapData` |
| `Expire` / `MarkAsSeen` / `ToggleEventActivation` | ✅ ILiveOps slot 2/12/13 落地 | `LiveOpsEvent.cs:375 / :397 / :422` |
| 弹窗 **prefab 解析** | ⏳ **未接**（GUID→prefab 没做）→ `LiveOpsPopupPresenter` 缝 | `LiveOpsHandler.cs:1379` |
| `filter.eventGroups` 数据 | ⏳ **未搬**（SO 有、目录层没抄）→ `LiveOpsEventFilter.eventGroupIds` 恒空 | `LiveOpsEvent.cs:95` |
| C1-W3 五扇窗接通 | ⏳ **数据受阻**（无 5 类事件 + 无 prefab 解析），缝已落 | §三 |

---

## §五 没查清 / 还差什么

1. **`LiveOpsAssetUtility.GetComponentReference` 的完整体**：只确认它按 `(ev, label)` 返回组件引用；内部「按 label 在
   `EventComponents` 里找」的细节没逐条读。⇒ 还差：读 `LiveOpsAssetUtility__GetComponentReference.c` 全集 + `EventComponentReferenceType` 枚举值表。
2. **`WindowsManager.OpenWindow(reference, ev, closeAll:false, default)` 的 `default(GameWindowOptions)` 是什么**：R7 §四·1 已记「多份同形产物未逐条对签名」。不影响「开一扇窗、把 ev 当 data」这一层。
3. **`EventExpiredSignal` 的订阅方**：cpp2il 读出 `new EventExpiredSignal(id)` + `Signal.Raise`；**谁订阅**没查（原版 37 个 `Signal.Raise` 里有没有它、有没有 Register）。我们目前用 `LiveOpsSignals.EventExpired` 承载，**零订阅方**（如实记）。
4. **`filter.eventGroups` 的组注册表**：`EventFilter.AddOrRemoveFromGroups` 的语义没读全（组 → 事件集合的注册与封锁）。属 C1-W2 的「组去重」支，数据侧未搬 ⇒ 暂为空。
5. **`LiveOpsPopupPresenter` 装上后，窗关时 `MarkAsSeen`/`Expire(null)` 的时序**：原版把委托挂 `window+0x60`（`GameWindow.OnCloseWindow`）；我们已有同名 `Action OnCloseWindow`，但**没跑过实机**验证「Hide 时 Invoke 一次」与本链一致。
