# `MenuActions` 完整语义 + 我们的等价物 —— 第 3 轮判据

- 生成：2026-10-26 · 批次 3 · 执行代理 **Q3**
- 起因：R7 §四·3 / R12 §六·2 把 `ClearEvents` 第一跳（`MenuActions.ReleaseAction`）记成「改动请求」，理由是
  **全仓没有 `MenuActions` 等价物**。本轮把原版整类反编译读完（非泛型类，`decomp_full` 全在），并落地了等价物。
- 判据来源（全部只读 `d:/2/`）：
  - `d:/2/tools/il2cpp_out/dump.cs`：`MenuActions`(TypeDefIndex 1565) · `MenuActions.Action`(1560) · `ActionRule`(1566) 的字段/偏移/方法表；
  - `d:/2/tools/decomp_full/MenuActions__*.c`（49 个产物）：`ReleaseAction` / `QueueAction` / `QueueActionWithCallback` /
    `CanTrigger` / `TryTriggerAction` / `BlockGroups` / `Start` / `StopMenuActions` / `ResumeMenuActions` / `CheckActiveMenus` /
    `Routine` 状态机 `MoveNext` / 两个 DisplayClass；
  - `MenuActions.Action__get_IsEnqueued.c`。

---

## §一 原版 `MenuActions` 的完整语义（逐条现读）

### 1·1 静态状态（`MenuActions : SingletonBehaviour<MenuActions>`）

| 成员 | 偏移 | 语义 |
|---|---|---|
| `static List<Action> actions` | 0x0（静态） | **全局**待触发动作队列（唯一正本） |
| `Coroutine menuCheckingRoutine` | +0x20 | 正在跑的 `Routine()` 句柄（非空 = 已在轮询） |
| `bool wasActivatedThisSession` | +0x28 | 本会话是否已触发过（`Routine` 里 blockAllActions 时置位） |
| `bool blockAllActions` | +0x29 | 全封锁标记（某动作 `ActionRule=BlockAllActions` 触发后置位） |
| `List<string> usedGroups` | +0x30 | 已被占用的组名（组去重） |

### 1·2 `MenuActions.Action`（字段逐个照抄）

| 字段 | 偏移 | 语义 |
|---|---|---|
| `Callback` (Action) | +0x10 | 同步回调（二选一） |
| `MenuFilter` (Type[]) | +0x18 | 「只在这些窗类型里触发」白名单；`null` = 不限 |
| `ActionRule` | +0x20 | `DontBlock=0 / BlockActionsFromSameGroup=1 / BlockAllActions=2` |
| `Priority` (int) | +0x24 | 排队键（**降序**） |
| `TriggerBeforeOnboardingEnds` (bool) | +0x28 | `true` = onboarding 没结束也允许触发 |
| `Groups` (string[]) | +0x30 | 本动作所属组（与 `usedGroups` 交集非空 ⇒ 不触发） |
| `AsyncCallback` (Action<Action>) | +0x38 | 异步回调（二选一）：完成时回调传入的 `Action` |
| `ForceRun` (bool) | +0x40 | 强制跑（blockAllActions 时的豁免） |
| `Recurring` (bool) | +0x41 | 触发后不摘除 |

- 两个构造函数：`Action(Action callback, …)` 与 `Action(Action<Action> asyncCallback, …)`，其余 8 个参数同序。
- `IsEnqueued => actions.Contains(this)`。

### 1·3 `ReleaseAction`（`MenuActions__ReleaseAction.c`）—— **本类存在的核心**

```csharp
public static void ReleaseAction(MenuActions.Action action) => actions.Remove(action);   // 全局静态
```
🔴 是**静态表上的 `List.Remove`**，按**句柄**撤销。`LiveOpsHandler.ClearEvents` 第一句就是「对每个 handlerActions 的值调它」。

> ⚠️ **澄清「ClearEvents / 两个口」**：`MenuActions` **自己没有 `ClearEvents` / 批量 `Clear()`**（`dump.cs` 方法表里没有）——
> 全类只有 `ReleaseAction` 这一条撤销口。所谓「两个口」是 **`LiveOpsHandler.ClearEvents`** 里的两件事：
> ① `handlerActions.Clear()`（清**排期表**）与 ② `foreach(a in handlerActions.Values) MenuActions.ReleaseAction(a)`（撤**全局菜单队列**）。
> 只做 ① 不做 ② ⇒ 表空了但菜单队列里还排着，窗口照样会弹（原版不是这样）。这正是本轮落地 `MenuActions` 的动机。

### 1·4 `QueueAction` / `QueueActionWithCallback`（`MenuActions__QueueAction{,WithCallback}.c`）

两者结构相同，只是把入参分别写进 `Callback` / `AsyncCallback`：
```
var a = new MenuActions.Action(...);   // 填 9 个字段
actions.Add(a);                        // 全局静态表
CheckActiveMenus();                    // 没在跑 ⇒ StartCoroutine(Routine())
return a;                              // 返回【句柄】
```

### 1·5 `CheckActiveMenus`（`MenuActions__CheckActiveMenus.c`）

```
if (<某静态开关关闭> && !wasActivatedThisSession && menuCheckingRoutine == null)
    menuCheckingRoutine = StartCoroutine(Routine());
```

### 1·6 `Routine()`（`MenuActions._Routine_d__10__MoveNext.c`）—— 主循环

```
while (true) {
    var window = WindowsManager.Instance.currentWindow;                       // +0x58
    var ordered = actions.OrderByDescending(x => x.Priority);                 // ★ 稳定降序
    foreach (var action in ordered) {
        var c = new DisplayClass10_0 { action, <>4__this = this };
        if (blockAllActions) { wasActivatedThisSession = true; yield …; }     // 暂停（等场景切换）
        TryTriggerAction(this, action, window, ok => {                        // 完成回调 b__1
            if (!ok) return;
            if (action.ActionRule == BlockActionsFromSameGroup) usedGroups.AddRange(action.Groups);
            else if (action.ActionRule == BlockAllActions)    blockAllActions = true;
        });
    }
    …（若窗口变了 ⇒ 重跑；否则 yield 一帧）
}
```

### 1·7 `TryTriggerAction`（`MenuActions__TryTriggerAction.c`）+ `CanTrigger`

```
if (currentWindow == null) { onComplete(false); return; }                     // ★ 没有当前窗 ⇒ 一律不触发
if (action.Groups.Any(g => usedGroups.Contains(g))) { onComplete(false); return; }
if (!CanTrigger(action)) { onComplete(false); return; }
// CanTrigger = (!action.TriggerBeforeOnboardingEnds ? SegmentationController.CheckTag(0x14) : true)
//               && <某静态开关(true)>
if (action.MenuFilter.Contains(currentWindow.GetType())) {
    if (action.Callback != null) { action.Callback(); }
    else if (action.AsyncCallback != null) { action.AsyncCallback(<完成委托>); return; }
    if (!action.Recurring) actions.Remove(action);
    onComplete(true);
}
```

### 1·8 `BlockGroups` / `ResumeMenuActions` / `StopMenuActions` / `Start`

- `BlockGroups(groups)`：`usedGroups.AddRange(groups)`。
- `Start()`：订阅 `SceneTransitionStarted → StopMenuActions`、`SceneTransitionEnded → ResumeMenuActions`。
- `StopMenuActions()`：退订 + `StopCoroutine(menuCheckingRoutine)` + 置 null。
- `ResumeMenuActions()`：`usedGroups.Clear()` + `wasActivatedThisSession = blockAllActions = false` + 重新订阅 + `CheckActiveMenus()`。
  ⇒ **`blockAllActions` 的清零挂在「场景切换结束」上**。

---

## §二 我们落地的等价物（`Shell/LiveOpsHandler.cs` 文件尾）

| 原版 | 我们的 | 落点 |
|---|---|---|
| `enum ActionRule` | `ActionRule`（同值 0/1/2） | `LiveOpsHandler.cs:1148` |
| `MenuActions.Action` | `MenuAction`（9 字段 + `IsEnqueued`） | `LiveOpsHandler.cs:1160` |
| `MenuActions`（静态表 + 队列/撤销/组/规则） | `MenuActions`（`static class`） | `LiveOpsHandler.cs:1205` |
| `QueueAction` | `MenuActions.QueueAction(...)` → 返回 `MenuAction` 句柄 | `:1224` |
| `QueueActionWithCallback` | `MenuActions.QueueActionWithCallback(...)` | `:1239` |
| `ReleaseAction` | `MenuActions.ReleaseAction(MenuAction)` = `actions.Remove` | `:1262` |
| `BlockGroups` | `MenuActions.BlockGroups(string[])` | `:1270` 附近 |
| `Routine` 主循环 | `MenuActions.TryDrain()`（**同步** + 每帧泵重试） | `:1284` |
| `TryTriggerAction`/`CanTrigger` | `MenuActions.CanTriggerNow` + `Trigger` + `Complete` | `:1310+` |
| `ResumeMenuActions` 的清零 | `MenuActions.ResetBlocking()` | `:1278` 附近 |
| 协程泵 | `LiveOpsPump.OnUpdate()` 每帧调 `TryDrain()` | `LiveOpsHandler.cs` `LiveOpsPump` |

### 2·1 如实简化（⛔ 不是「已经一样」）

1. **同步 drain vs 协程**：原版每触发一条 `yield` 一帧；我们入队时 + 每帧泵各 drain 一次（同帧内尽量清空）。
   ⇒ 「推迟一拍」的效果只在「入队时条件不满足」时体现。**LiveOps 弹窗的两条动作本身是同步完成的**
   （`ShowTriggerEventPopup` / `ExpireEvent` 开窗后**立刻**回调），所以这一层差异对它们**无可见影响**。
2. **`blockAllActions` 的清零时机**：原版挂场景切换；我们提供 `ResetBlocking()` 手动清。目前 13 份 SO 没有任何
   `BlockAllActions` 的动作 ⇒ 不会卡。
3. **`CanTrigger` 的 onboarding / 静态开关**：用可注入 `MenuActions.OnboardingEnded`（默认 `true`）。装上真实判据前保持 true。
4. **不新建 MonoBehaviour / 不走 UGUI 协程**：等价物是纯 C# 静态类，由 `LiveOpsPump` 的 PlayerLoop 每帧推进。
   理由同 R14：动不了 `ShellRuntime.Update()`（别人白名单）。
5. **没有 `SingletonBehaviour<MenuActions>.Instance`**：原版是场景组件单例；我们是静态类（无实例状态需要序列化）。
   语义等价（静态表本来就是 `static`）。

### 2·2 与原版的**逐点对照**（可复查）

| 行为 | 原版 | 我们 | 一致？ |
|---|---|---|---|
| 按句柄撤销 | `actions.Remove(action)` | 同 | ✅ |
| 排期表清空 ≠ 撤销队列 | 两个口 | `handlerActions.Clear()` + 逐个 `ReleaseAction` | ✅ |
| 排序 | `OrderByDescending(Priority)`（稳定） | 同（挑最高、同优先级保入队序） | ✅ |
| 组去重 | 与 `usedGroups` 交集非空不触发 | 同 | ✅ |
| 窗类型白名单 | `MenuFilter.Contains(currentWindow.GetType())` | 同（`WindowsManager.Instance.currentWindow`） | ✅ |
| 无当前窗 | 一律不触发 | 同（`currentWindow == null` ⇒ false） | ✅ |
| ActionRule 应用时机 | 触发**之后**（完成回调里） | 同（`Complete`） | ✅ |
| Recurring | 触发后不摘除 | 同 | ✅ |
| 异步回调 | `AsyncCallback(onComplete)` | 同 | ✅ |
| 泵 | 协程每帧 | PlayerLoop 每帧 | ⚠️ 等价（同步 drain） |
| blockAllActions 清零 | 场景切换 | `ResetBlocking()` 手动 | ⚠️ 简化 |
| onboarding 闸 | `CheckTag(0x14)` | 注入钩子（默认 true） | ⚠️ 简化（可接） |
