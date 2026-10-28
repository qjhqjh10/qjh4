# B89-E11 · `GameModes` 12 条（R10 §三）

> 执行代理 `B89-E11` · 2026-10-28 · 账域 = `其他_基础设施` · 账行 = 主表第 215–226 行（按**方法名**现读确认，逐条对得上）
> 判据 = `判据/B88_R10_基础设施58条施工单.md` §三（3.1 状态表 / 3.2 `DeserializeSharedData` / 3.3 `SetResultCount` / 3.4 数据类）
> 原版体 = `cpp2il/out_cpp2il_dev_ilspy/GameModes.cs`（控制流可读）＋ `decomp_full/GameModes__*.c`（常量与分支的最终判据）

---

## §1 逐条结论

| 账行 | 原版判据 | 改动（文件:符号名） | 状态建议 | P3 桩的出声方式 |
|---:|---|---|---|---|
| 215 `CombatFinished(RankedMatchResult, Action<OnlineMatchResult>)` | `GameModes.cs:526-552` · `GameModes__CombatFinished.c:1-47` · 闭包 `GameModes.__c__DisplayClass35_0___CombatFinished_b__0.c` | `Shell/GameModesHandler.cs:163`（CombatFinished） | **A**（差异见账：反射调 `MatchFinished` + 回调里 `r.PlayEvent` 那一格未搬） | —（非 P3） |
| 216 `DeserializeSharedData(JObject)` | `GameModes.cs:458-524` · `GameModes__DeserializeSharedData.c:1-96`（`Classic`/`Ranked`/`FastMode` 三个字面量实解） | `Shell/GameModesHandler.cs:187`（DeserializeSharedData）＋ `Shell/LiveOpsSharedData.cs:427`（RankedSharedData）/ `:520`（ResultCounterSharedData） | **A** | — |
| 217 `GetActiveEvent(PlayModes)` | `GameModes.cs:596-659` · `GameModes__GetActiveEvent.c` · 三个 lambda 的 `.c`（槽 0 = EventPlayMode / 槽 23 = EventState==2 / 槽 21 = Priority） | `Shell/GameModesHandler.cs:228` | **A**（`EventPlayMode` 走反射；排序键走 `GetEventPriorityForMainMenu()`） | — |
| 218 `GetActiveEvent(Type)` | `GameModes.cs:661-754` · `GameModes__GetActiveEvent.c`（`ConfigManager.GetConfig<T>()` + 两次 `Type.op_Inequality` + 递归） | `Shell/GameModesHandler.cs:263` | **B** —— 三条支都已对齐，但 **入口类型 `IRankedEvent` 本机未建**（注入点 `:432` 默认 null ⇒ 该支不可进入）；远端配置注入点 `GameModesConfigSource(:40)` 默认 null | — |
| 219 `GetActiveEvent<T>()` | `GameModes.cs:562-594` · `GameModes__GetActiveEvent_object_.c` | `Shell/GameModesHandler.cs:301` | **A**（约束写 `class, ILiveOps`） | — |
| 220 `InitializeHandler()` | `GameModes.cs:409-416`（kickoff）· `GameModes._InitializeHandler_d__30__MoveNext.c:1-70`（`await base` → `PlayfabWrapper.GenericCloudScriptHandler<object>`） | `Shell/GameModesHandler.cs:398` | **B**（本地半段真做，服务器半段 P3） | `Debug.Log`：说明「基类那一跳已跑；CloudScript 回包是 P3，未实现」 |
| 221 `SetAsCurrentlyPlayingEvent(IPlayEvent)` | `GameModes.cs:554-560` · `GameModes__SetAsCurrentlyPlayingEvent.c`（全文一句 store） | `Shell/GameModesHandler.cs:315` | **A** | — |
| 222 `SetIsHuman()` | `GameModes.cs:418-422` · `GameModes__SetIsHuman.c`（`+0x80==0 ⇒ throw`） | `Shell/GameModesHandler.cs:324`（＋读口 `IsHuman(:87)`） | **A** | — |
| 223 `SetLossStreak(int)` | `GameModes.cs:424-428` · `GameModes__SetLossStreak.c` | `Shell/GameModesHandler.cs:339`（＋读口 `LossStreak(:75)`） | **A** | — |
| 224 `SetResultCount(string, BattleWinner)` | `GameModes.cs:430-456` · `GameModes__SetResultCount.c`（`0xa/0x14/0x1e` 立即数实读）· `ResultCounterSharedData__GetValuesForMode.c` | `Shell/GameModesHandler.cs:364`（＋建表口 `LiveOpsSharedData.cs:534`） | **A** | — |
| 225 `_003CInitializeHandler_003Eb__30_0(RankedSharedData)` | `GameModes.cs:756-762` · 同一条 MoveNext 链 | `Shell/GameModesHandler.cs:413`（ApplyServerRankedSharedData） | **B**（P3 桩，0 调用方） | `Debug.LogWarning`：说明它是 P3 落地口、原版由 CloudScript 回调调用、本地无服务器 |
| 226 `_003C_003En__0()` | `GameModes.cs:764-768` | `Shell/GameModesHandler.cs:398`（`InitializeHandler` 第一句 `base.InitializeHandler()`） | **A**（编译器蹦床，不单独建方法） | —（随 220） |

**施工形状**：在 `Shell/LiveOpsHandler.cs` 旁边新增 `Shell/GameModesHandler.cs`（`GameModesHandler : LiveOpsHandler`，`HandlerKey = "GameModes"`），⛔ 没有另起第二套 liveops；数据类只往 `Shell/LiveOpsSharedData.cs` **追加**，已有成员一格未动。

### 三条「判据 vs 实现」的关键取舍（都已在代码注释里写死）

1. **`SetResultCount` 的 `default` 支必须排在 `GetValuesForMode` 之前。** `GetValuesForMode` 取不到会**新建一条**（`ResultCounterSharedData__GetValuesForMode.c`）⇒ 顺序反了，未知 winner 也会往表里塞一格空记录（静默、且没有报错）。已按 `.c` 把 switch 的语义提到前面：`winner ∉ {10,20,30} ⇒ 立刻 return，连表都不读`。
2. **`FastMode` 没有 Ranked 级回落**（差异点最容易被「顺手统一」写错）；第三条是**整份** `sharedData` 反序列化，**不是**取子键 —— 两条都配了灭自证断言（A2 / A5）。
3. **`CombatFinished` 末尾那句 `CurrentPlayingEvent = null` 是原版的**（`GameModes__CombatFinished.c` 里 `param_1+0x78 = 0`）⇒ 同一局只会分派一次。配了 C4 灭自证。

---

## §2 typecheck 读数 + 写好的断言

```
TMPDIR=/tmp/wf_B89E11 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```
（跑了两遍：写完两个 `.cs` 一次、改完断言一次；两次都是 0/0。⛔ 没跑 Unity / ⛔ 没跑 ruleprobe。）

**断言宿主** = `MyGame/Assets/CardPresentation/Editor/GameModesCheck.cs`（与 `Editor/ProfileDataAssertE13.cs` 同形：`public static void Run()` + 自带 `Pass`/`Fail`）。
**51 处 `Ok`/`Eq` 调用**（含 7 条夹具前提），四节：

- **A 节 `DeserializeSharedData`（20 条）**：A1 `Classic` 在就取它（含 8 格字段与 `SeasonPositions` 的 `{"3":15}` ⇒ `<3,15>`）；A2 只有 `Ranked` ⇒ 回落，**且 FastSharedData 不许被 Ranked 污染**（灭自证）；A3 都没有 ⇒ `new` + ctor 默认值 `HighestPosition.Position == int.MaxValue`；A4 顶层 `ResultCounter` 解成字典；A5 **`ResultCounter` 埋在 `Ranked` 子对象里读不到**（灭自证「取整份」）。
- **B 节 `SetResultCount`（9 条）**：胜/负/平各 +1 且不动另外两格；**B4 灭自证：`Undefined` 不许建 'Duel' 这一格**；B5 合法 winner + 新 mode ⇒ 自动建格并记 1。
- **C 节 setter + `CombatFinished`（9 条）**：`IsHuman` / `LossStreak` 读口跟着变；`CombatFinished` 分派一次、分派的是**同一份** `RankedMatchResult`、回调被转发、`CurrentPlayingEvent` 被清空；**C4 灭自证：清空后再来一次不再分派**。
- **D 节 `GetActiveEvent`（10 条）**：模式闸 / Active 闸（p99 但已过期那条不许被选中）/ 优先级降序 / 类型闸（`typeof(GameModesHandler)` ⇒ null，灭自证）/ 泛型那半 / 排位接口三态（拿不到配置 ⇒ null、递归一次、等于接口本身 ⇒ 落回 LINQ）。

⚠️ 夹具 = `FakePlayEvent : PlayEvent<PlayEventSaveGame, FakePlayEventData>`（只为给 `EventPlayMode` / `MatchFinished` 一个真身；原版那两个是 `IPlayEvent` 的接口成员）。⛔ 夹具不碰玩家存档、不建场景。

---

## §3 顺手发现（⛔ 我一条都没自己改）

1. **`ILiveOps` 接口缺 `Priority` 与 `ShowInMainMenu` 两个成员。** 原版 `Everguild.LiveOps/ILiveOps.cs` 有它们（`Priority` = 声明序 2、`ShowInMainMenu` = 6）。我们的 `LiveOpsEvent` 类上**都有**实现，只是接口里没声明 ⇒ 本批只能拿 `GetEventPriorityForMainMenu()` 顶替排序键（同一格 `data.priority`），而「主菜单要不要显示」在接口上根本问不出来（必须 downcast）。**建议**：给 `Shell/LiveOpsEvent.cs` 的 `ILiveOps` 补这两条（类已实现 ⇒ 零行为变化、零改调用点）。`Shell/LiveOpsEvent.cs` 不在本批白名单。
2. **`IPlayEvent` 接口整个没建**（成员：`EventPlayMode` / `JoinState` / `CurrentGamesLost` / `StartMatch` / `CancelMatch` / `MatchFinished` / `GetMatchmakingFilter` / `SetCustomMatchOptions` / `SetDefaultDeck` / `GetEventDeck()` / `GetEventDeck(CardArmy)` / `new GetBaseData()` / `new GetBaseSave()`）。`PlayEvent<TSave,TData>` 上**大多已有同名公开成员** ⇒ 建接口 = 纯声明 + 让它 implement，风险低。建好之后本批的两处反射（`TryGetPlayMode` / `TryFinishMatch`）可以换回强类型调用。
3. **`OnlineMatchResult` 缺 `PlayEvent` 字段**（原版 `+0x28`）：原版 `CombatFinished` 的回调包会写它（`GameModes.__c__DisplayClass35_0___CombatFinished_b__0.c`）。我们的 `OnlineMatchResult` 是 P3 最小子集（`success/error/newRating`）⇒ 那一格没搬。真要做 FastMode 结算时会被消费（**消费方未查**）。
4. **`GameModesHandler` 目前没有注册进 `LiveOpsManager`**（`new GameModesHandler()` 的接线点在 `Shell/LiveOpsManager.cs` / `Shell/LiveOpsLocalCatalog.cs` —— 本批白名单外，且归 B89-R2b）。⇒ 今天它是**可直接 new 的纯 C# 类**，但运行时链路还没接上；`LiveOpsManager.GetHandler<GameModesHandler>()` 取不到。
5. **`GameModes` 还有 4 个派生读口不在本批 12 行内**：`PreviousSeasonClassicLeaderboardKey`（`:317`）/ `PreviousSeasonFastLeaderboardKey`（`:330`）/ `LastPlayedSeason`（`:343`，两表取 `Math.Max`）/ `CurrentSeasonNumber`（`:392`，依赖 `IRankedEvent` ⇒ 与账 218 同一处阻塞）。同属这个类，建议立账。
6. **账 226（`n__0`）建议并入 220**：它是编译器蹦床，单独立行会让「方法总数」虚高 1。
7. `GameModesConfig`（远端）除 `mainRankedType` 外还有 `rankedRanks` + `GetRankData(division,rank)` / `GetRankDataByPoints(mmr)` 两个方法（`.c` 已读，不在本批 12 行内）—— 若将来做段位显示，判据已经在 `cpp2il/out_cpp2il_dev_ilspy/GameModesConfig.cs`。

---

## §4 没查清（如实写「没查清」，不猜）

- **`GameModesConfig.mainRankedType` 的远端真值**：本地 0 命中（远端 TitleData）⇒ 只知道「有没有配」这件事决定 `GetActiveEvent(typeof(IRankedEvent))` 的走向，**不知道实际配的是哪个类型**。
- **`OnlineMatchResult.PlayEvent`（+0x28）在结算链里被谁消费**：本批只确认了「谁写它」，没查消费者（属 P3 服务端结算，判据在别的账域）。
- **`SetResultCount` 的真正调用方**（哪个界面/结算在打完一局后记账）：属别的账行，没查。

---

## §5 交接（要给主对话加什么）

1. **`-executeMethod` 入口**：`GameModesCheck.Run`（编辑器程序集，全局命名空间，与 `ProfileDataAssertE13` 同形）。想并进总数就在宿主收尾处加：
   ```csharp
   GameModesCheck.Run();
   pass += GameModesCheck.Pass; fail += GameModesCheck.Fail;
   ```
   ⚠️ 它是**纯逻辑断言**（不建场景、不跑帧循环、不写盘）⇒ 挂在 `RuleEngineTest.Run` 或任一一个宿主里都行，**不需要新开 Unity 宿主**。
2. **接线待办（本批白名单外）**：把 `new GameModesHandler()` 注册进 `LiveOpsManager`（判据：原版靠 `GetComponentsInChildren<ILiveOpsHandler>()` 找 handler；我们的等价物 = `LiveOpsManager` 的注册表）。不接 ⇒ `LiveOpsManager.GetHandler<GameModesHandler>()` 一直是 null。
3. **反射的两处替换点**（等 `PlayEvent.cs` 补 `IPlayEvent`）：
   - `GameModesHandler.TryGetPlayMode` ⇒ `g is IPlayEvent p && p.EventPlayMode == mode`
   - `GameModesHandler.TryFinishMatch` ⇒ `((IPlayEvent)e).MatchFinished(result, callback)`
4. **账补丁**：`账/_parts/B89_E11.tsv`（11 列表头已带，12 行，`轮次` = `B89-E11`；`方法` 列与主表**逐字相同**，可直接按 `--only B89_E11` 合并）。已跑 `工具/check_patch_conflicts.py`：**本补丁无跨补丁重复键**（报告的 3 条重复在 `B89_E2c` 与 `B89_E3` 之间，与本批无关）。
5. **状态汇总**：9 条本地项 → **A ×8 / B ×1**（218 因 `IRankedEvent` 未建降 B）；3 条 P3 → **B ×2（220/225）+ A ×1（226，随 220）**。合计 A=9 · B=3 · C=0 · D=0。
