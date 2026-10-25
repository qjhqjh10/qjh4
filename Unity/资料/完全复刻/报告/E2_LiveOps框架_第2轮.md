# E2 · C1-W1「LiveOps 框架核心」· 第 2 轮（2026-10-25）

> 批次：第八十会话 · 完全复刻批次 1 · 执行代理 **E2**
> 白名单：`Assets/CardPresentation/Shell/LiveOps*.cs`（含新建）+ `资料/完全复刻/账/日常_奖励.tsv`
> 目标一句话：**把 LiveOps 框架核心建出来，让 5 扇活动窗将来能接「真事件实例」**（本轮只做框架 + 选择逻辑迁移，⛔ 不接窗）

---

## ① 结论

**一句话**：框架五件（事件/存档层 · 调度器 · 总管理 · 工厂 · 共享数据层）**从零建出来了**；
「当前活动」的选择逻辑**已从 `LiveOpsLocalCatalog` 挪进 handler**（唯一实现）；38 条 P1 **全部有结论**：
**A 20 · B 18 · C 0 · D 0**。`typecheck 0/0`。**没有任何一扇活动窗被改动**（接窗是 C1-W3/C3）。

| 项 | 数 |
|---|---|
| 新建文件 | **5 个 / 2,184 行**（`LiveOpsEvent.cs` 526 · `LiveOpsHandler.cs` 705 · `LiveOpsManager.cs` 356 · `LiveOpsEventFactory.cs` 183 · `LiveOpsSharedData.cs` 414） |
| 改已有文件 | **1 个**（`LiveOpsLocalCatalog.cs`：`TryPickEventIn` 改为转发 + 两个助手放开成 public） |
| 账 | `日常_奖励.tsv` **38 行**（C→A20/B18）⇒ 本域 **A 265→285 · B 84→102 · C 315→277** |
| 运行时断言 | **0 条**（批次红线：⛔ 不跑 Unity；本件也**没有自检宿主**） |

**两个「不能后补」的设计点 —— 已按判据落地（都进了 `SetupEvents` 三参签名）**：

1. **`blockedEvents` = 独立第二列表**，⛔ **不并进任何「过期」字段**。
   判据：`ILiveOpsHandler.cs:37-39,71`（`GetBaseEvents(includeBlockedEvents=false)` / `GetBlockedEvents()` /
   `SetupEvents(events, **blockedEvents**, sharedData)`）+ `LiveOpsHandler.cs:933/1325` 两份 `blockedEvents` 字段。
   语义 = **运营维屏蔽**（仍在列表里、`GetEvents()` 默认不返回、可被调试窗/`includeBlockedEvents:true` 列出），
   与**时间维的过期**是两件事（`判据/日常与活动.md` §二 Q1 原话）。
   落地：`LiveOpsHandler.blockedEvents`（`Shell/LiveOpsHandler.cs:146`）+ `LiveOpsLocalHandler.BlockedIds`（`:661` 附近）。

2. **`sharedData` = 跨事件共享块**（原版 `JObject`），⛔ 不是「各自 SO 里带」。
   判据：同一个 `SetupEvents` 的第三参 + 唯一已知消费点 `LiveOpsHandler.DeserializeSharedData`（原版 `:2523`）读 `sharedData["NextRefresh"]`。
   落地：`LiveOpsHandler.shared`（`Shell/LiveOpsHandler.cs:150`）+ `LiveOpsSharedDataManager`（`Shell/LiveOpsSharedData.cs:340`）。
   ⚠️ 工程**没有 Newtonsoft** ⇒ `JObject` 换成自带的 `LiveOpsJson`（`Shell/LiveOpsSharedData.cs:37`，最小 JSON 对象，支持 object/array/string/number/bool/null）。

**可达性（如实写）**：**现在没有任何玩家可见入口** —— 13 份 SO 全过期 + 主菜单 `EVENT` 导航钮自 2026-10-24 起整颗隐藏
⇒ **本次改动不会带来任何画面变化，它不是验收件**。⛔ 我没有为了让「看得见」去改导航钮或任何一扇窗。

---

## ② 判据（逐条出处；含「读不出来」的四条）

### 2·1 判据来源（按方案 §三 四级顺序取）

| 级 | 用了什么 | 位置 |
|---|---|---|
| 🥇 | ILSpy（`out_cpp2il_dev_ilspy`）—— **方法体可读的那部分逐条对照** | `LiveOpsHandler.cs`（3,504 行）· `LiveOpsManager.cs`（2,230）· `Everguild.LiveOps/LiveOpsEvent.cs`（1,104）· `LiveOpsEventFactory.cs`（139，**三个方法体全可读**）· `ILiveOpsHandler.cs`（84）· `LiveopsHelper.cs` |
| 🥈 | Ghidra `.c`（`decomp_full`）—— ① ILSpy 是骨架（async 状态机）② 交叉验证 | `LiveOpsHandler._SetupEvents_d__18…MoveNext.c`（182 行，**三参入口真身**）· `…_RefreshHandlerLocally_d__27…`（64）· `…_RefreshHandler_d__26…`（271）· `…_RefreshHandler_d__29…`（271）· `LiveOpsManager__GetHandler.c` / `__GetAllBlockedEvents.c` / `__ResetHandlers.c` / `_Unpack_d__15…` / `_BuildHandlers_d__16…` / `_InitializeHandlers_d__18…` / `_BuildHandler_d__17…`（472）· `SharedEventDataManager__Initialize.c` / `__Unpack.c` / `__GetContext_object_.c` |
| 字段表 | `out_il2csharp`（无方法体、**字段与偏移完整**）—— 把反编译里的 `this + 0xNN` 逐条对上 | `LiveOpsHandler_1.cs` · `LiveOpsManager.cs` · `LiveopsHandlerData.cs` · `RawEvent.cs` · `LiveOpsEventData.cs` · `EventTimeData.cs` · `LiveOpsEventPersistence.cs` · `SharedEventData.cs` · `LiveOpState.cs` |

**字段偏移自证**（三处独立命中同一套映射，可复查）：
`this+0x20 events`（`_SetupEvents` 写 `plVar1[4]`）· `+0x28 blockedEvents`（`[5]`）· `+0x30 shared`（`[6]`）·
`+0x38 handlerActions`（`ClearEvents` 第一句）· `+0x58 Manager`（`RegisterManager` 写 `this+88`）·
`+0x60 handlerCache`（`ValidateData` 写 `this+96`）· `+0x68 refreshSemaphore`（`RefreshHandler(d__29)` 等它）·
`+0x70 RefreshCount`（`InitializeHandler` 先 +1）。

### 2·2 逐条判据要点（38 条按类归并；完整一行在账里）

**`LiveOpsEvent<TSave,TData>`（8 条）** —— 全部照抄 `Everguild.LiveOps/LiveOpsEvent.cs`
- `GetBaseSave():652` / `GetBaseData():645` = `return SaveGame` / `return Data`
- `Initialize():671` = `if (Data!=null && SaveGame!=null) Handler.TryScheduleHandlerRefresh(milis)`
- `LoadEventData():701` = `Data=(TData)data; SaveGame=(TSave)saveGame; Handler=handler; if (Handler!=null) Handler.UpdateCachedSave(SaveGame);`
- `ValidateEventData():727` = `d=(TData)data; if(d==null) return false; formatedData=d; if(save!=null){s=(TSave)save; if(s!=null&&Validate(s)){formatedSave=s; return true;}} formatedSave=CreateNewSave(d); return true;`
- `CreateNewSave():882` / `UpdateData():903` / `UpdateSaveGame():944` / `ResetEventProgress():980`（三跳）
- 🔴 **顺带坐实两条反直觉语义**（写进代码注释，将来别推翻）：
  · `GetState` 三处是**严格小于**（`dateTime < currentTime`），不是 ≤；
  · `IsActive()` **`Finished(3)` 也算 true**（`(uint)(3-2)=1` 不大于 1 ⇒ 落到 `return true`），
    `Expired(4)` / `NotAvailable(-1)` 为 false，`Planned` 只在 `includePlanned` 时为 true。
  ⇒ **我们旧口径 `IsInPeriod = start ≤ now < end` 与原版不等价**（`Finished` 那一段是硬差异），已写进账里 `LiveOpsEvent.GetState` / `IsActive` 两行的差异列。

**`LiveOpsEventFactory`（3 条）** —— `LiveOpsEventFactory.cs` 三个方法体**全部可读**，逐句照抄（`New` 出实例 → 拿 `SaveType` → 反序列化 → `LoadEventData`）。

**`LiveOpsHandler<TEvent>`（16 条）** —— 关键三条：
- `SetupEvents(3 参):1597` 真身 = `shared = sharedData ?? new JObject(); events = ops.Cast<TEvent>().OrderBy(key).ToList(); this.blockedEvents = blockedEvents; await SetupEvents(events); DeserializeSharedData(shared); ScheduleEventPopups();`
- `TryScheduleHandlerRefresh:2535` 两道门 = ① 已排得**更早** ⇒ `Log` 后 return 不动；② `milis >= now` ⇒ 真排；③ 否则 `Log`「already past」丢弃
- `InitializeHandler:2698` = `RefreshCount += 1; foreach(e in events) e.Initialize();`
- `GetEvents/GetBlockedEvents/GetEvent` 全部可读（`GetEvent` = `GetEvents(false).FirstOrDefault(x=>x.GetID()==id)`，**不查 blocked**）。

**`LiveOpsManager`（9 条）** —— 关键三条：
- `BuildHandlers:2029` 真身 = 拿 handlers → `HandlerLookup = new Dictionary(len)` → 逐个 `HandlerLookup.Add(type,h)` + `TryGetValue(h.HandlerKey)` + `await BuildHandler(h, data, config)`
- `Unpack:2020` 真身 = `config = ConfigManager.GetConfig<FeatureConfig>(); await BuildHandlers(data, config); await InitializeHandlers(config);`（第二跳的 `InitializeHandlers` 是从状态机构造器名**确证**的）
- `GetHandler(Type):1764` = `TryGetValue` 命中就返回，否则 `LogWarning(前缀 + ReadableName(type) + 后缀)` 返回 null（⛔ 不抛）
- `GetAllBlockedEvents:1818` = `HandlerLookup.Values.SelectMany(h => h.GetBlockedEvents())`
- `Initialize:2009` 状态机只读出「`waitingForHandlers`(+0x30) 为真就一直 yield」

**`SharedEventDataManager`（2 条）** —— `Initialize:148` = `GetComponentsInChildren<SharedDataContext>()` → `ToDictionary` 落静态字段；
`Unpack:49` = 存 `this+0x28` → `GetContext<…>()` → `foreach (…) ctx.Unpack(item)`（vtable +0x178）。

### 2·3 🔴 **四条「读不出来」（⛔ 没把猜测写成「原版就是这样」）**

| # | 在哪 | 读不出什么 | 我们的处置（账里标 B） |
|---|---|---|---|
| 1 | `LiveOpsEvent.Initialize():671` | `milis = <某 int 字段>*3600000 + <某 long 字段>` 的**两个字段是谁** | 取 `SaveGame.triggerTime` 作调度时刻 |
| 2 | `LiveOpsEvent.CreateNewSave():882` | 「带 data 的初值」是对 TData 的虚调用（vtable +0x188），**取哪个成员** | 取 `data.eventId`（`LiveOpsEventPersistence` 里唯一对得上的字段） |
| 3 | `LiveOpsHandler.SetupEvents:1597` | `Enumerable.OrderBy(...)` 的**排序键** | 按 `GetID()` 序数序 |
| 4 | `LiveOpsHandler.DeserializeSharedData:2523` | `TryGetValue("NextRefresh")` 之后那一跳 ILSpy 写成 `Method not found @181019810` | 按位置判为 `ScheduleHandlerRefresh(MillisToDateTime(ms))` |

另有三条**「虚调用槽号对不上」**：`ResetHandlers` 的逐 handler 调用（vtable +7）、`RefreshHandlerLocally` 传给虚方法的参数（字段 +0x60）、
`BuildHandler` 里「哪些 RawEvent 算 blocked」—— 处置逐条写在账的差异列。

---

## ③ 改动清单（`文件:行号`）

### 3·1 新建（5 个）

| 文件 | 行数 | 里面有什么（关键声明行） |
|---|---|---|
| `Shell/LiveOpsEvent.cs` | 526 | `LiveOpState` / `LiveOpsActivationType` / `LiveOpsEventTimeData`(:62) / `LiveOpsEventData`(:86，含 `GetState`:115 `IsActive`:144) / `LiveOpsSave`(:160) / `ILiveOps`(:178) / `LiveOpsEvent`(:200) / `LiveOpsEvent<TSave,TData>`(:261) / `LiveOpsSaveBlob·Entry`(:448,:456) / `LiveOpsSaveStore`(:464，`Load`:474 `Save`:498) |
| `Shell/LiveOpsHandler.cs` | 705 | `LiveOpsClock`(:37) / `LiveOpsScheduledRefresh`(:50) / `LiveOpsScheduler`(:64) / `ILiveOpsHandler`(:114) / `LiveOpsHandler`(:137，`SetupEvents(3参)`:188 `RefreshHandlerLocally`:266 `TryScheduleHandlerRefresh`:328 `InitializeHandler`:371 `ClearEvents`:387 `ResetEvents`:403) / `LocalLiveOpsEvent`(:502) / `LiveOpsLocalHandler`(:577，`LoadFromCatalog`:600 **`SelectInTable`:629** `TrySelectActive`:676) |
| `Shell/LiveOpsManager.cs` | 356 | `RawEvent`(:32) / `LiveopsHandlerData`(:42) / `LiveOpsFeatureConfig`(:61，**最小占位**) / `LiveOpsManager`(:74，`ResetHandlers`:128 `GetHandler<T>`:143 `GetHandler(Type)`:151 `GetAllBlockedEvents`:180 `Initialize`:228 `Unpack`:249 `BuildHandlers`:268 `BuildHandler`:290 `InitializeHandlers`:329) |
| `Shell/LiveOpsEventFactory.cs` | 183 | `LiveOpsValidationStrategy`(:28) / `LiveOpsEventTypeMap`(:47，`TryGetEventFromData`:67 **沿 BaseType 向上找**) / `LiveOpsEventFactory`(:95，`CreateEvent`:101 `CreateEventFromRaw`:125 `DeserializePersistence`:149) |
| `Shell/LiveOpsSharedData.cs` | 414 | `LiveOpsJson`(:37，`Parse`:173) / `SharedEventData`(:320) / `LiveOpsSharedContext`(:329) / `LiveOpsSharedDataManager`(:340，`Initialize`:351 `GetContext<T>`:368 `Unpack`:388) |

### 3·2 改已有（1 个，**行数变短 11 行，但所有被账引用的行号都没动**）

`Shell/LiveOpsLocalCatalog.cs`
- `:681` `TryPickEventIn` 的**方法体换成转发**（`:686` 起 5 行注释 + 4 行代码）→ 调用 `LiveOpsLocalHandler.SelectInTable`，
  并把 `LiveOpsLocalHandler.Default.BlockedIds` 传进去。🔑 **这是「选择逻辑挪进 handler」的落地**，
  旧口径（kinds → IsInPeriod → has → Selectable → StartMs 最大者）**逐条不变**。
- `:988` `KindIn` · `:996` `KindNames`：`static` → `public static`（新实现要用，⛔ 不为它抄第二份）。
- ✅ **漂移核查**：本文件 1,083 → 1,075 行；账里 32 处引用**全部 ≤ 681**（实测见 §④），改动点之后**没有任何账引用** ⇒ **零漂移**。

### 3·3 账

`资料/完全复刻/账/日常_奖励.tsv` —— 38 行（筛 `状态=C ∧ 工作项 W1 ∧ 原版类 ∈ 6 类`），写回 `状态/我们的落点/差异/等级/轮次/备注`。
落点一律 `路径:行（声明名）` 形态（等价说明放第二个括号，按方案 §四）。
备份：`_tmp_view/e2shards/日常_奖励.tsv.e2bak`（**没放回账本目录**，同 N17 的做法；脚本：`_tmp_view/e2_ledger_update.py` · `…_fix1.py` · `…_fix2.py`）。

---

## ④ 断言 / 读数（**全部是现读**）

| 项 | 命令 | 读数 |
|---|---|---|
| 编译 | `TMPDIR=/tmp/wf_e2 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（收尾时；过程中一度有 4+5 条错，全在 **E6 的 `RankedDivisionChangeWindow.cs`** / **共用件 `Editor/ShellScene.cs`** / **E4 的 `Editor/NetBattleTest.cs`**，我一个文件都没碰，等它们落地后归零） |
| 形态闸 | `工具/check_ledger_points.py` | 日常_奖励 **A=285 · 通过 282 · 漂移 1 · 告警 5 · 失败 2**。🔴 **3 条不通过全都不在我的文件上**：漂移 = `RuleCore.cs:6112`（实际 6121，**E1 在改**）；失败 ×2 = `Shell/DraftModeWindow.cs:2309` 落点变空行（**E6 在改**）。**我 20 条 A 行全部通过** |
| 语义闸 | `工具/verify_annot.py` | 全仓 `缺注解 30`，**日常_奖励 0 条**（过程中报过一条 `GetHandler<T>()`，已补等价注解） |
| A×缺口矛盾 | `工具/check_a_vs_diff.py` | 强 0 · 弱 85（**日常_奖励 0 条**，其余是别的域） |
| 账进度 | `工具/ledger_progress.py` | 日常_奖励 **779 行：A 285 · B 102 · C 277 · D 115**；C 315→**277（−38）** |
| 行尾 | `io.open(p,'rb')` 数 `\r\n` vs `\n` | 5 个新文件 + `LiveOpsLocalCatalog.cs` **全 LF、0 个 CRLF**（与 `Shell/` 邻居一致，未翻转） |
| Express 自检 | —— | **0 条**。本件**没有自检宿主**（不碰 `Editor/*Scene.cs`），批次红线也不许跑 Unity。⚠️ 所以**本报告的「行为正确」全部是静态核对**，不是跑出来的 |

**静态核对做到了什么程度（这是本件唯一的「验」）**：
- 38 条落点逐条 `grep` 出**真实行号**（§③ 表与账里一致）；
- 字段偏移**三处独立自证**（§②末尾）；
- 两条反直觉语义（严格小于 / `Finished` 也算 active）**在代码注释里写死**，并指向原版行号。

---

## ⑤ 没查清（逐条写「还差什么」）

1. **`LiveOpsEvent.Initialize()` 的调度表达式** —— 差「那两个字段是谁」。坐实路径：在 `LiveOpsEventData` / `LiveopsHandlerData` 的真实字段表里按偏移 0x1C / 0x20 找那个 int 与 long（当前 `EventTimeData` **没有** int 字段 ⇒ 那个 int 很可能来自 `FeatureConfig`，而它的字段判据**整类都没解出来**）。
2. **`CreateNewSave` 的「data 初值」** —— 差 TData 的 vtable +0x188 是哪个成员。坐实路径：反汇编该槽位。
3. **`SetupEvents` 的排序键** —— 差 `Enumerable.OrderBy` 的 key selector（ILSpy 只写成 `FUN_180c99380`）。
4. **`ScheduleEventPopups` / `QueueEventPopup` / `ShowTriggerEventPopup` / `ExpireEvent` 的正文** —— 属 C1-W2（14 条 P2），**本批只建去重表**（`handlerActions`），真弹窗没做。
5. **`FeatureConfig`（远端配置）整类字段** —— 本地没有任何判据 ⇒ `LiveOpsFeatureConfig` 只是**最小占位**（只有 `IsHandlerEnabled`，且返回值是我们挑的 true）。⛔ 别在它上面继续加「看起来合理」的字段。
6. **`RawLiveOpsHandlerPersistence`（服务器载荷解析）** —— 它是 `JArray`/`JObject` 两条支 + 按 `eventId` 分组进两个字典；**输入是服务器内容，本地永远拿不到** ⇒ 本批**没有实现**（`LiveopsHandlerData` 只能被程序构造）。这条属于「远端挂起线」。
7. **`SharedEventDataManager.Unpack` 的分发对象** —— 差「取哪个 context、遍历哪个列表」。本地 `SharedDataContext` 子类 **0 个**（原版 `EnergyContext` 的数据也没有）⇒ 当前只出声。
8. **`LiveOpsManager.Initialize()` 的协程正文** —— 只读出「等 `waitingForHandlers`」；原版在这之后是否 `BuildHandlers` 一遍，状态机里没有。
9. **13 份 SO 的 `priority` / `location` / `showInMainMenu`** —— `LiveOpsLocalCatalog` 从没抄过这三列（C1-W4 的活）⇒ `LocalLiveOpsEvent.MapData` 只能给默认值，**事件在主菜单的排序/位置当前无数据**。
10. **⚠️ 一个可能被误读的点（已写进代码注释）**：`LiveOpsManager.Initialize()` 是**会死等**的协程（`waitingForHandlers` 初值 true）。本地没有服务器载荷 ⇒ 只有 `Unpack(...)` 或 `MarkHandlersReady()` 能放行。**⛔ 别把 `LiveOpsManager.Instance.Initialize()` 直接挂进启动链**，否则主菜单会卡在那一帧。

---

## ⑥ 顺手发现（⛔ 都没擅自改）

1. 🔴 **`SaveKeys.LiveOps`（`"LiveOps"`）这个键被本批占用了** —— 键表里它一直是「原版 LiveOps。【我们】**空缺**」（`SaveKeys.cs:175-176`），
   现在 `LiveOpsSaveStore` 用它装**全部事件的存档**（一个 key 一份 blob，每事件一条）。**E3 的存档体系（Step 3-5）要知道**：
   如果他们也打算往这个键写东西，**两边会互相覆盖且不报错**。→ **需要调度台裁定**（我倾向：E3 直接接管这个键的结构，我这边只经 `LiveOpsSaveStore` 读写，换 schema 时改一处）。
2. 🔴 **两个**「账本落点漂到别人文件」的真凭据（**不是我的错，但会让下一个人以为账坏了**）：
   `日常_奖励.tsv` 的 `PlayEvent.OnMatchLost / OnMatchWon` 落点 `Shell/DraftModeWindow.cs:2309` **现在是空行**（E6 在改那个文件）；
   `ChallengeLogMgr.LogAttack` 落点 `RuleEngine/Core/RuleCore.cs:6112` 实际在 6121（E1 在改）。⇒ **同步点跑一次 `--fix` 就回位**。
3. **`LiveOpsLocalCatalog` 的两套「现在」读口**：旧口 `LiveOpsLocalCatalog.NowUtc`（窗口缺省值用，32 条账指着它）与新的 `LiveOpsClock.NowUtc`（handler 用）。
   两者语义相同、**目前都是 `DateTime.UtcNow`**。将来接 `SceneInitializer` 的服务器时间时，**要一起换成同一口**（⛔ 现在不动旧口，避免 32 条账 + 5 扇窗一起返工）。
4. **`LocalLiveOpsEvent.MapData` 把 `previewable` 硬写成 false** —— 13 份 SO 里确实没有 `previewDate`。副作用：`GetState` 的 `OnPreview` 档**本地永远走不到**（`initialDate == startDate`）。这条对 C1-W2 有用（预览期弹窗做不出来）。
5. **`LiveOpsEventData.location` 存成 `int`** —— 原版是 `EventLocation` 枚举，**本地没解过它的值** ⇒ 我没编枚举名。谁要用（C1-W4）得先去解那个枚举。
6. **5 扇窗里有两扇完全不碰活动目录**：`RankedEventWindow.cs` 与 `EnergySinglePlayerOnlyEventWindow.cs` 对 `LiveOpsLocalCatalog` **0 引用**
   （它们的活动文案走基类缺省 = `LocalLiveOpsEventId` 为 null ⇒ 空态）。⇒ **接真事件时这两扇要先补「事件 id 从哪来」**，见 §交接。
7. **`LiveOpsManager.GetHandler<T>()` 与 `GetHandler(Type)` 同名不同参** ⇒ 形态闸会报「同名多声明 ⇒ 歧义，不自动改」（我已给两行分别标 A，并补了等价注解）。这是**预期噪声**，不是缺陷。
8. **`verify_annot.py` 口径提醒**：它把 `GetHandler<T>()`（原版方法名带泛型元数）与源码声明名 `GetHandler` 判成「异名」⇒ 需要 `（等价实现 = …）` 注解。以后凡泛型方法都要这么写，否则闸会红。
9. **`LiveOpsLocalCatalog` 里那句「两张表走同一个函数」的注释仍然成立** —— `TrySelectActiveFastMode` 走的 `TryPickEventIn` 现在转发到 `SelectInTable`，
   `FastModeEventsOverrideForTest` 的注入口**语义未变**（`E15` 那组自检不用改）。

---

## 交接给下一个代理

### A. 这一轮到底交付了什么（一句话可复制）

5 个新文件 + 1 处转发改造 + 38 行账；**框架只建不接窗**；`typecheck 0/0`；**无画面变化**。

### B. 「接窗」时（C1-W3 / C3）怎么用（**这是本批唯一的下游契约**）

```
// 1) 拿 handler（进程级单例；第一次访问会自动注册 LiveOpsLocalHandler.Default）
var mgr = LiveOpsManager.Instance;

// 2) 装本地 13 份 SO 成真事件实例（含过期；blocked 另算）
int n = LiveOpsLocalHandler.Default.LoadFromCatalog(LiveOpsClock.NowUtc, sharedJsonOrNull);

// 3) 「现在开着哪个活动」——唯一的挑选规则（原口径不变）
LocalLiveOpsEvent ev; string why;
if (LiveOpsLocalHandler.Default.TrySelectActive(LiveOpsLocalKind.Scoring, LiveOpsClock.NowUtc, out ev, out why))
{
    var data = ev.Data;            // LiveOpsEventData（eventId/enabled/dates/priority/…）
    var save = ev.SaveGame;        // LiveOpsSave（进度正本；落盘走 LiveOpsSaveStore）
    var local = ev.Local;          // 离线数据本体（取词条/图片还用它）
}

// 4) 运营屏蔽第二种「不可开」：给 handler 装 blockedEvents（⛔ 不是把 enabled 改 0）
await handler.SetupEvents(ops, blockedOps, sharedJson);   // 三参，一次定死

// 5) 后台刷新（离线）
LiveOpsScheduler.Tick(LiveOpsClock.NowUtc);   // 宿主每拍调；到点会自己喊 RefreshHandler()
```

### C. 5 扇窗「各需要什么 / 框架现在提供什么 / 还缺什么」

| 窗 | 现在从哪儿拿数据（**现状，一行没改**） | 框架现在提供 | 还缺（下一批） |
|---|---|---|---|
| **Raid** `RaidEventWindow.cs:473` | `LiveOpsLocalCatalog.SurfaceTerm(…)` + `NowUtc` | `TrySelectActive` 给出实例 → `Data` / `SaveGame` / `Local` | ① 该窗自己的 `LocalLiveOpsEventId`（现在没有）② Scoring 里程碑数据层（**C3-W3**） |
| **ExpansionPass** `ExpansionPassEventWindow.cs:595` | 同上 | 同上 | 同上 + 领奖钮状态机（`C3-W1`，落点不在本批白名单） |
| **Skirmish** `SkirmishEventWindow.cs:196/180` | `TrySelectActiveFastMode` / `HasActiveFastModeEvent`（**表空**） | `SelectInTable` 同一套规则（`FastModeEventsOverrideForTest` 注入口保留） | **FastMode 的真实 SO 本地不存在** ⇒ 只能靠合成事件验（判据 `普查产出_1024b/S1_…md`） |
| **Ranked** `RankedEventWindow.cs` | **0 引用**（走基类缺省 = 空态） | `TrySelectActive(Kind=Scoring/Parent)` | ① 事件 id 来源 ② `RankedEventBase` 的积分→段位（**C3**） |
| **Energy** `EnergySinglePlayerOnlyEventWindow.cs` | **0 引用** | `LiveOpsSharedDataManager.GetContext<T>()`（**context 0 个**） | `EnergyContext`（max / rechargeAmount / cooldown=300000ms）+ 那份数据（**C1-W7**，休眠） |
| 共用基类 `LiveOpsEventWindow.cs:1503/1509` · `ParentEventWindow.cs:709/984` · `BaseEventPopup.cs:254` · `BaseOfferPopup.cs:711` | `HelpTitleTerm` / `Find` / `TrySelectActive` / `TryResolvePopupLabels` / `TryResolvePremiumOffer` | 事件实例的 `Data` 已经有 `eventId`；`Local` 挂着词条/图片 | **C1-W3 的资产装载层**：`EventComponents` 的 GUID → prefab 名（`guid_map.tsv`），`UiContainer` / `LoadableRef<T>` |

### D. 下一轮开工前必须知道的 6 条

1. **`SaveKeys.LiveOps` 已归本线**（见 §⑥-1）。E3 若要用，先跟调度台对一次。
2. **`LiveOpsManager.Initialize()` 会死等**（§⑤-10）。启动链接它必须配 `Unpack` / `MarkHandlersReady`。
3. **未坐实的 4 条**（§②-2·3）都写在**代码注释 + 账的差异列**里，改之前先读那一行的注释，⛔ 别当「已知正确」。
4. **形状闸的 3 条不通过不是我的**（§④ / §⑥-2）；同步点 `--fix` 会回位。
5. **`LiveOpsLocalCatalog.NowUtc` 与 `LiveOpsClock.NowUtc` 是两条口**（§⑥-3）；接服务器时间时一起换。
6. **本件没有自检宿主**。若要补断言：新建 `Editor/LiveOpsAssert.cs`（不在本批任何人的白名单里，属于新面）+ 一个 `*Scene.Run` 挂钩；
   最省事的验点 = `SelectInTable` 的 6 档边界（含 `blockedIds` 生效 / 过期不入选）+ `LiveOpsJson.Parse↔ToJson` 往返。
