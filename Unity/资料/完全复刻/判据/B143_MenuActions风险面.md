# B143 · `MenuActions` 空 `menuFilter` 改默认值的风险面（只读 · 2026-11-05 · `B143-W4`）

> 背景：`B143-W7` 把 `LiveOpsHandler.cs:1300/1315` 的 `MenuFilter` 默认从「原样存（null = 不筛）」改成
> **`(menuFilter == null || menuFilter.Length == 0) ? new[] { typeof(MainMenuWindow) } : menuFilter`**
> （判据 = 原版 `cpp2il/out_il2csharp/Assembly-CSharp/MenuActions.cs:151-165`）。
> ⛔ 本文件只读：**没改代码、没跑 Unity**。

## 结论（一句话）

**可放行（不回滚）**：这一改动**与原版一致** —— 原版两个 `QueueAction*` 的签名都是
**`params Type[] menuFilter`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/MenuActions.cs:161/164`）⇒
**省略实参 = `Array.Empty<Type>()`**，由函数体换成 `[typeof(MainMenuWindow)]`；原版**没有 null 分支**。
我们三处调用传的 `null` / 省略 / 空表，改后都等价于原版。
⚠️ **但 `RaidEvent.cs:774` 另有一个真缺陷**：漏了原版的第 4 个实参 `groups: ["ScoringEvent"]` ⇒
`recurring:true` 的动作**没有组可锁**，改后一旦可达就会**每帧重触发**（详见 §四）。

---

## 一、三处语义（现读）

`CanTriggerNow`（`LiveOpsHandler.cs:1380-1394`）的判据链：`blockAllActions` → `usedGroups` → **`wm.currentWindow` 非空** → **`win.GetType()` ∈ `MenuFilter`** → onboarding。
`currentWindow` = **最上面那扇窗**（`WindowsManager.cs:89`，`OpenWindow` 两支都写它；`TopWindow` 同义）——
⇒ 主菜单上**叠了任何弹窗**时，`currentWindow` 是那个弹窗，不是 `MainMenuWindow`。

| 站点 | 现读行 | 现在传的 filter | 改后语义 |
|---|---|---|---|
| `LiveOpsHandler.QueueAction` 默认 | `LiveOpsHandler.cs:1300` | 由形参 | `null`/空表 ⇒ `[MainMenuWindow]`；非空表原样 |
| `LiveOpsHandler.QueueActionWithCallback` 默认 | `LiveOpsHandler.cs:1315` | 由形参 | 同上 |
| `LiveOpsHandler.QueueEventPopup` | `LiveOpsHandler.cs:414-422` | **`null`**（`:422` `// menuFilter`） | **只在 `MainMenuWindow` 为当前窗时触发**（其余窗 / 无窗 ⇒ 等） |
| `GameModesHandler.InitializePracticeForMenuStart` | `GameModesHandler.cs:791-799` | **省略**（⇒ null） | 同上 |
| `RaidEvent.Initialize` | `RaidEvent.cs:774` | **`Array.Empty<Type>()`** | 同上；⚠️ 另漏 `groups`（§四） |

> 只有这三处**生产**调用点会拿到「空 filter ⇒ 默认」。
> 其余调用点（`DailyData.cs:2473/2480/2554`）都**显式**传 `new[] { typeof(MainMenuWindow) }` ⇒ 一个字节不变。

---

## 二、受影响路径表（穷举「主菜单外也会走到」）

| # | 入口 / 场景 | 走到哪处 | 排队时当前窗是谁 | 改后行为 | 判定 |
|---|---|---|---|---|---|
| 1 | 主菜单 `Start()` → `MainMenuRuntime.cs:60` → `IngestPracticeResults()` → `GameModesHandler.cs:637` | `GameModesHandler.cs:791`（练习未收尾提示） | `MainMenuWindow`（`Build()` 之后、`Start` 里）或**尚无窗** | 当前窗就是主菜单 ⇒ 当场/下一帧触发；无窗 ⇒ 等主菜单开 | **✅ 不受影响**（最多晚一帧；原版 `PracticeEvent.Initialize` 传 `Array.Empty<Type>()` 同样等主菜单） |
| 2 | 启动装配 `LiveOpsLocalBoot.BootLocalLiveOps`（`[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`，`LiveOpsHandler.cs:1510-1517`）→ `LoadFromCatalog` → `SetupEvents`（`:978`）→ `ScheduleEventPopups`（`:282`）→ `QueueEventPopup` | `LiveOpsHandler.cs:414`（活动触发/过期弹窗） | **尚无窗**（场景刚加载） | 入队；等 `MainMenuWindow` 成为当前窗后逐条触发 | **✅ 不受影响**（原版同样排队等主菜单） |
| 3 | 活动被触发：`EventFilter.TryActivateEvent`（`LiveOpsFilter.cs:1011-1019`）→ `ToggleEventActivation(true,true,true)`（`LiveOpsEvent.cs:514`）→ `RefreshHandlerLocally` → `SetupEvents` → `ScheduleEventPopups` | `LiveOpsHandler.cs:414` | **可能是战斗 / 任意窗**（触发信号不挑场景） | 不在战斗中弹；等回主菜单再弹 | **✅ 不受影响，且修掉「战斗中弹活动窗」**（原版 filter 同样是 `MainMenuWindow`） |
| 4 | 活动窗 / 突袭窗 / 商店 / 奖励窗 / 收藏 / 锻造 / 战役 打开期间排上的活动弹窗 | `LiveOpsHandler.cs:414` | 那些窗自己（`currentWindow`） | **推迟**到它们关掉、主菜单回前台 | **✅ 不受影响**（原版同：`TryTriggerAction` 只看 `CurrentWindow`）；⛔ 不会「不再触发」——`LiveOpsPump.OnUpdate` 每帧 `TryDrain()`（`LiveOpsHandler.cs:1146-1151`，`BeforeSceneLoad` 自动装，生产每帧跑） |
| 5 | 主菜单上叠着**消息弹窗 / 奖励窗**（`ShowPopUp` 等） | 以上三处全部 | 弹窗 | 推迟到弹窗关 | **✅ 不受影响**（原版同） |
| 6 | `RaidEvent.Initialize`（`RaidEvent.cs:769-775`） | `RaidEvent.cs:774` | 任意 | 只在主菜单触发；⚠️ **递归 + 无组 ⇒ 每帧重触发** | **🟠 会受影响（需补 groups，不是回滚）** |

> **离线生产链的可达性（重要）**：
> · 第 2 条**会真跑**（`LiveOpsLocalBoot` 在 `AfterSceneLoad` 自动调用，`LoadFromCatalog` 里同步 `SetupEvents`）。
> · 第 6 条**离线不跑**：`RaidEvent.Initialize` 只由 `LiveOpsHandler.InitializeHandler()`（`LiveOpsHandler.cs:657`）调，而后者只经 `LiveOpsManager.Unpack → InitializeHandlers`；本工程**没有 `Unpack` 的生产调用点**（`EnsureStarted` 只建单例，`LiveOpsManager.cs:385-410`）⇒ 今天零可观测，**自检 / 将来接服务器时才会现形**。

---

## 三、与原版对照（三处 filter 全部一致）

| 出处 | 原版实读 | 结论 |
|---|---|---|
| `MenuActions.QueueAction` / `QueueActionWithCallback` 签名 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/MenuActions.cs:161` / `:164` —— `params Type[] menuFilter` | **省略 = 空表**；函数体 `if (menuFilter.Length == 0) menuFilter = new[]{ typeof(MainMenuWindow) }`（`cpp2il/out_il2csharp/Assembly-CSharp/MenuActions.cs:151-165`）；**无 null 分支** |
| `PracticeEvent.Initialize` | `decomp_full/Everguild.LiveOps.PracticeEvent__Initialize.c:45-47`：`uVar3 = System_Array__Empty<…>(); MenuActions__QueueAction(…, uVar3, 0)` | **空表** ⇒ `[MainMenuWindow]`。✅ 我们 `GameModesHandler.cs:791` 省略 ⇒ 同 |
| `LiveOpsHandler.QueueEventPopup` | `decomp_generic_trial/out/LiveOpsHandler_object___QueueEventPopup__1018490.c:104-108`：`QueueActionWithCallback(…, 0, **(…)(lVar8+0xb8), 0)` —— 那个位置是**静态空 `Type[]`**（同 `Array.Empty`） | ✅ 我们传 `null` ⇒ 同（W7 的 null 分支使之等价） |
| `RaidEvent.Initialize` | `cpp2il/out_il2csharp/Assembly-CSharp/Everguild/LiveOps/RaidEvent.cs:120-131`：`QueueAction(action, BlockActionsFromSameGroup, 0, textArray2 /* ["ScoringEvent"] */, 0, 0, 1 /*recurring*/, Array.Empty<Type>())` | filter ✅ 同；**`groups` 我们漏了**（§四） |

> 备注：我们的 `QueueAction*` 形参写的是 `Type[] menuFilter = null`（不是 `params`），靠 W7 的 null 分支补齐 —— 行为等价。
> 若想更贴原版可改成 `params`，但那会让现有显式 `null` 调用点变成「一个 `null` 元素」的数组，**不建议**动。

---

## 四、结论 + 补丁建议

### 4·1 `menuFilter` 默认值改动：**可放行（无需回滚）**
三处 filter 语义都与原版一致；所有「主菜单外的窗」只会**推迟**到回主菜单（原版本来就该这样），
不存在「永远不触发」—— 因为 `LiveOpsPump` 每帧 `TryDrain()`。

### 4·2 `RaidEvent.cs:774` **需补 `groups`**（🟠 真缺陷，W7 让它从「死」变「活」而暴露）
现读：

```csharp
_refreshAction = MenuActions.QueueAction(RefreshScoreValues, ActionRule.BlockActionsFromSameGroup,
                                         recurring: true, menuFilter: System.Array.Empty<System.Type>());
```
缺第 4 个实参。原版是 `groups = ["ScoringEvent"]`（`out_il2csharp …/RaidEvent.cs:125-131`）。
后果：`Complete` 时 `BlockActionsFromSameGroup` 只往 `usedGroups` 加 `a.Groups`，而 `Groups == null` ⇒ **什么也锁不住**；
`Recurring` 又不摘除 ⇒ 每次 `TryDrain`（**每帧**）都会再触发一次 `RefreshScoreValues`。

**建议补丁（一行、零行数变化）**：

```csharp
            _refreshAction = MenuActions.QueueAction(RefreshScoreValues, ActionRule.BlockActionsFromSameGroup, 0, new[] { ScoringGroup }, false, false, true, System.Array.Empty<System.Type>());
```

⚠️ 如实记：原版 `Initialize` 先 `BlockGroups(["ScoringEvent"])` 再排这条带同组的动作 ⇒ 补上 groups 后，
该动作在 `usedGroups` 被清（`MenuActions.ResetBlocking()` = 原版 `ResumeMenuActions`）之前**不会触发** ——
这是**原版自身的形状**（那条 refresh 本来就要等菜单动作恢复）。
若复核认为「原版这条其实靠别的路径清组」，那就该把 `recurring: true` 一并复核；⛔ 但**不能**保持现在「无组 + 递归」。

### 4·3 可选保真（非必需）
`GameModesHandler.cs:791` / `LiveOpsHandler.cs:422` 可把「省略 / `null`」写成 `System.Array.Empty<System.Type>()`，
纯保真、行为不变；不做也不影响正确性（W7 的 null 分支已兜住）。

---

## 五、还差什么

1. **没跑 Unity**：§二 的「推迟一帧 / 每帧重触发」都是**静态推导**；真机 / 自检首跑时看
   `[Raid] Refreshing Scoring Event Values.` 的**出现频率**即可实证 4·2。
2. **`RaidEvent.Initialize` 离线无生产调用点**（`Unpack` 未接）⇒ 4·2 今天零可观测；`LiveOpsFilter.cs:1276/1283`
   的自检夹具会调 `ev.Initialize()`，**首跑那两条断言时要留意控制台是否刷屏**。
3. **未查清**：`MenuActions.ResetBlocking()` 在生产里由谁调（等价原版 `ResumeMenuActions`）—— 本文件没追；
   若生产从不调，则补 groups 后这条 raid refresh 会**永不触发**（与原版「等 ResumeMenuActions」同形，但要看我们有没有那个时机）。
4. 本文件只覆盖**空 filter**这条改动；`MenuActions` 的其它如实简化（同步 drain / 无场景总线）见 `LiveOpsHandler.cs:1267-1271`，不在本次范围。
