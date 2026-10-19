# W57 · 战斗线 `T09-FIX4` —— `BattleEventSystem` 收养既有实例时校验输入模块

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-FIX4`（执行）
> **判据入口** `资料/普查产出_1019/W57_战斗线_T09_审查.md:90-92` —— 审计里的编号是 **`P3-2`**
> （派工简报写的「`P3-9` 后半」= 同一条：`Ensure()` 收养既有实例时**不校验模块**；汇总表另见 `:24`）。
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑 `*.Run` / 未跑 `_run_8_checks.sh`；只跑了秒级 typecheck。
> 未动 git、未改正本、未删文件。**只改了一个文件**（白名单）。

---

## 一、改前 / 改后

**文件**：`Unity/MyGame/Assets/CardPresentation/Battle/BattleEventSystem.cs`（唯一改动文件；LF · 246 → 356 行 · 14,654 → 22,280 B）

⚠️ 该文件**尚未入 git**（`git status --porcelain` = `??`，T09-A1 昨天新建、还没提交）⇒ **`git diff --numstat` 无输出是预期的**；
行尾用二进制读数复核：`CRLF 0 / LF 356`，**没有整篇翻**。

### 改前（问题）

- 两条收养路都是**无条件收养**：
  - ① `EventSystem.current`（改前 `:103-109`）：`Instance = cur; SyncRaycasters(); return;`；
  - ② 场景里已有的（改前 `:118-130`）：`Instance = pick; EnsureCurrentInEditMode(...); ...`。
  **都不看模块类型**。
- `Ready`（模块类型 + 没有别的模块）**只有自检断言在读**（`Editor/BattleScene.cs:28152`）——
  Play 模式下**没有任何东西**会拦「收养到一只挂 `StandaloneInputModule` 的 `EventSystem`」。
- 后果：`activeInputHandler = 1`（New-only，`ProjectSettings/ProjectSettings.asset:935`）下旧模块
  **一个事件都收不到、而且不报错** ⇒ 整个 HUD **静默全废**（审计原文：「若将来有任何东西先建了一只
  `StandaloneInputModule` 的 EventSystem」）。

### 改后（逐处）

1. **`Ensure()` 的两条收养路都调 `EnsureCompatibleModules(...)`**（`:134`、`:154`）：
   - **合规**（≥1 只 `InputSystemUIInputModule`，且**没有**任何别的 `BaseInputModule`）⇒ **零动作、不出声**
     （正常路径行为零变化 —— 现建路 ③ 本来就只会挂对的模块）；
   - **不合规** ⇒ **移除**所有非 `InputSystemUIInputModule` 的模块；**缺了就补一只**
     （`AddComponent<InputSystemUIInputModule>()`；Play 模式那一刻派 `OnEnable`
     ⇒ `HasNoActions() ⇒ AssignDefaultActions()`，立刻可用），并 `Debug.LogWarning` **一次**
     （日志里把拿到的模块类型名原样写出来，便于定位「是谁先建了一只挂错模块的 EventSystem」）。
   - 移除口径：**Play** `enabled = false`（当场退出 `EventSystem.Update` 的模块挑选）+ `Destroy`；
     **编辑** `DestroyImmediate`（与 `ResetForTest` 同一条口径）。编辑模式**不碰 `enabled`** ——
     模块的 `OnDisable` 会调 `m_EventSystem.UpdateModules()`（`BaseInputModule.cs:116-119`），
     而编辑模式 `OnEnable` 没跑过 ⇒ `m_EventSystem` 还是 null，白挨一条 NRE 没有意义。
2. **`Ready` 的判据抽成唯一一处** `ModulesAreCompatible(es)`（`Ready` 与修复共用同一个口 ——
   防「两处写同一条规则 = 迟早不一致」）。`Ready` **语义不变**：`Instance != null`
   且 ≥1 只 `InputSystemUIInputModule`、且没有别的 `BaseInputModule`。
3. **`InstanceCount` / `ResetForTest` / `SyncRaycasters` 一字未动**（语义保持不变）。
4. **新增 Editor-only 只读探针**（`#if UNITY_EDITOR`）：
   - `ModuleTypeNameForTest` —— `Instance` 上按组件序**第一只**输入模块的类型名；没有实例/没有模块 ⇒ `"none"`；
   - `ModuleTypeNamesForTest` —— **全部**输入模块的类型名（逗号分隔、按组件序）；没有 ⇒ `"none"`。
   两条都是**纯读**、不改任何状态（`Ready` 只答「合不合规」，这两条答「到底是什么」）。
   ⚠️ 断言接线（`Editor/BattleScene.cs`）**不在本代理白名单**（T09-FIX3 在改）⇒ **没有动**。

### 为什么不用「停用再激活 GameObject」去逼 `OnEnable`

那会改 `EventSystem.current` 的登记次序（`OnDisable` 从 `m_EventSystems` 摘掉、`OnEnable` **追加到表尾**，
`EventSystem.cs:409-412 / 429-443`），还可能顺手激活别人关着的那只。
编辑模式**不需要**模块初始化：自检只直接调 `EventSystem.RaycastAll`（走 `RaycasterManager`，不经过模块），
而 Play 模式进场时 Unity 会重新派一遍 `OnEnable`。

---

## 二、判据（出处）

| # | 判据 | 出处 |
|---|---|---|
| 1 | New-only ⇒ 必须 `InputSystemUIInputModule`；旧模块**一个事件都收不到且不报错** | `ProjectSettings/ProjectSettings.asset:935`（`activeInputHandler: 1`）· `资料/普查产出_1019/W57_战斗线_T09迁移计划.md:40-41`、`:284-285`（R6） |
| 2 | 本任务要收的审计原文 | `资料/普查产出_1019/W57_战斗线_T09_审查.md:90-92`（编号 **P3-2**）、汇总表 `:24` |
| 3 | `AddComponent` 即可用（默认动作自动绑） | `Library/PackageCache/com.unity.inputsystem@7a4e1a2a8194/InputSystem/Runtime/Plugins/UI/InputSystemUIInputModule.cs:1639-1656`（`OnEnable` → `if (HasNoActions()) AssignDefaultActions()`；`AssignDefaultActions` 定义在 `:1541`） |
| 4 | 模块 OnEnable / OnDisable | `.../InputSystemUIInputModule.cs:1639`（OnEnable）· `:1657`（OnDisable，含 `UnassignActions`） |
| 5 | `EventSystem.current` 与模块表的实现 | `.../com.unity.ugui@27635d171b1a/Runtime/UGUI/EventSystem/EventSystem.cs:27`（`m_EventSystems`）· `:34-47`（`current`）· `:131-141`（`UpdateModules`）· `:409-412`（OnEnable）· `:429-443`（OnDisable）· `:472-520`（Update 挑模块） |
| 6 | 模块的 OnEnable/OnDisable 会 `UpdateModules()` | `.../EventSystem/InputModules/BaseInputModule.cs:109-119` |
| 7 | 编辑模式 `AddComponent` 不派 `Awake/OnEnable`、**激活那一刻才派** | 本文件头（`资料/已知的坑.md:1125` · `资料/普查产出_1011/W3_子2.md` §2·4③） |
| 8 | 本仓只有**一条** EventSystem 创建路 | `grep -rn "AddComponent<EventSystem>\|new GameObject(\"EventSystem\")" Assets --include=*.cs` ⇒ 只命中本文件；调用点唯一 = `Battle/BattleDriver.cs:18311` |
| 9 | 改前收养**不校验** | `BattleEventSystem.cs` 改前 `:103-109`（①）/ `:118-130`（②） |

---

## 三、没查清 / 没做（明说）

1. **`Ready` 与本次修复都不检查模块的 `enabled` 状态**（只看类型与有无）。旧 `Ready` 也是这个口径
   （`GetComponent` 同样返回 disabled 组件）⇒ 为守「`Ready` 语义不变」**没有加**。
   残留情形：EventSystem 上是一只**被停用的** `InputSystemUIInputModule` + 一只旧模块 ⇒ 修掉旧模块之后
   `Ready` 返回 true 但事件仍不通。**判据未要求、未改**；要补得同时改 `Ready` 语义（建议另立账）。
2. **修复路一条都没实跑过**（本波口径 = 只做不验）。其中**编辑模式 `DestroyImmediate` 一个组件时会不会派 `OnDisable`**
   **没实测**：若派、且该模块从未 `OnEnable`（⇒ `m_EventSystem` 为 null）会打一条 NRE。
   按本仓既有口径「回调由激活触发」（`W3_子2` §2·4③）推断**不会派**，因此编辑那一支既不碰 `enabled`、也没有额外防护。
3. **收养一只「未激活的」EventSystem**（`FindObjectsInactive.Include` 找得到）是**改前就有的行为**：
   `Instance` 会指向它、`Ready` 为 true，但它在激活前不处理事件。本次**没有改**这条语义（超出本任务判据）；
   补上去的模块在未激活 GO 上也不会立刻 `OnEnable`（激活时才会）。如需收口，**建议另立账**。
4. **`P3-3`（`ResetForTest()` 全仓无人调用）不在本任务范围**，未动。
5. **没有新增 / 修改任何断言**（`Editor/BattleScene.cs` 属 T09-FIX3 的文件所有权）。
   探针已备好（`ModuleTypeNameForTest` / `ModuleTypeNamesForTest`），接线请调度台安排。

---

## 四、typecheck（唯一跑过的检查）

```
TMPDIR=/tmp/wf_t09fix4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 已核 `Assembly-CSharp.csproj` 的 `DefineConstants` **含 `UNITY_EDITOR`** ⇒ 那两条 `#if UNITY_EDITOR` 探针
  **真编进去了**（不是被条件编译跳过）。
- 未跑 Unity 批处理 / `*.Run` / player（本波口径）。
- 行尾复核：`CRLF 0 / LF 356`（二进制读数）。
