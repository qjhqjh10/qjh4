# B120-W22 · `LiveOpsEvent<TSave,TData>.ValidateEventData` 方法体补导 + 对账

> 只读补导代理产出。⛔ 未改任何 `.cs` / 资产 / 账 / 工程文件；⛔ 未改 `d:/2` 既有产物（只在 `D:/4/_tmp_view/ghidra_b120/` 新建）。
> 本报告只写「原版方法体怎么走 / 我们现码差在哪 / 还欠什么」。

## 0. 一句话结论

**导出成功（12/12，`fail=0`）。** 原版 `ValidateEventData` 的真身是：

```
data 必须 isinst 成 TData（失败 ⇒ return false）
save 非 null 时，其【运行时类型必须恰好等于 TSave】——不等 ⇒ return false（不是回落到新建存档！）
能用就 Validate(save) 过 ⇒ 复用；否则 CreateNewSave(data)（= new TSave + eventId = data.GetID()）
```

我们现码有两处 **❌ 不一致**：
① `ValidateEventData` 用 `as` 代替了原版的**精确类型闸**（原版不等就 `return false`，我们会静默新建一份存档并 `return true`）；
② 我们的 `LoadEventData` **完全没有调 `ValidateEventData`**（原版是**唯一调用点**），因此那道路径上的 `Validate(save)` 闸整条不存在。

> 当前产品主路径（`LocalLiveOpsEvent.Create` 显式调 `ValidateEventData`，且 `LiveOpsSaveStore.Load` 用 `typeof(PlayEventSaveGame)` 反序列化 ⇒ 类型恰好相等）**恰好不触发差异**；
> 但这是「路径对了」，不是「实现对了」——`UpdateSaveGame` / `ResetEventProgress` / `DraftLocalEvent` / 三处自检夹具都直接走 `LoadEventData`，那条路上我们与原件不同。

---

## 1. 导出过程（可复跑）

**产出的背景**：`B120-W18` 报的三条「仍查不到」之一。il2csharp 是 `NotImplementedException`、`out_cpp2il_dev_ilspy` 是 `NoteDecompilerIssue` 垃圾体，但 `D:/2/tools/il2cpp_out/dump.cs:126407-126413` 的 `GenericInstMethod` 给了 VA `0x1810173D0` / RVA `0x10173D0`。

### 1.1 名单（`D:/4/_tmp_view/ghidra_b120/wanted_b120.txt`，12 条）

| RVA | 名字 | 为什么要 |
|---|---|---|
| `0x10173D0` | `LiveOpsEvent<object, object>.ValidateEventData` | **本件主目标** |
| `0x1016F80` | `LiveOpsEvent<object, object>.LoadEventData` | **唯一调用点**（拿它才能知道返回值怎么用） |
| `0x1016CA0` | `LiveOpsEvent<object, object>.CreateNewSave` | 失败分支的落点 |
| `0x4DE0E0` | `LiveOpsEvent<object, object>.Validate` | 复用存档前的闸 |
| `0x1017250` / `0x1017340` / `0x1016E70` / `0x4B3630` / `0x1017600` | `UpdateData` / `UpdateSaveGame` / `Initialize` / `GetBaseData` / `get_IsSegmented` | 顺带交叉验证 + 顺手补 B120-W18 的第 ③ 条 |
| `0x1127900` / `0x1126A30` / `0x1127DF0` | `PlayEvent<object, object>.StartMatchNoCheck` / `CheckPreviousMatch` / `StartMatch` | 顺手补 B120-W18 的第 ① 条 |

### 1.2 脚本（副本，⛔ 没动 `D:/2/tools/decomp_generic_trial/DecompG7.java`）

- 拷自 `D:/2/tools/decomp_generic_trial/b95_scripts/DecompG95.java` → `D:/4/_tmp_view/ghidra_b120/DecompB120.java`（只改了类名一行）。
- 用 G95 而不是 G7 的理由：G95 在 `getFunctionAt()==null` 时有 `createFunction` / `getFunctionContaining` 两级兜底，只会多出日志、不改变既有行为（本次 `created=0 contained=0`，说明 12 条都直接命中）。

### 1.3 命令（`-readOnly` 已加；日志自报 `Processing read-only project file` ⇒ 2.6 GB 工程零写入）

```bash
export JAVA_HOME="D:\\2\\tools\\jdk-21.0.12.1+1"
cmd //c "D:\2\tools\ghidra_12.1.3_PUBLIC\support\analyzeHeadless.bat \
  D:\2\tools\ghidra_proj Warpforge -process GameAssembly.dll -noanalysis -readOnly \
  -scriptPath D:\4\_tmp_view\ghidra_b120 \
  -postScript DecompB120.java \
    D:\4\_tmp_view\ghidra_b120\wanted_b120.txt \
    D:\4\_tmp_view\ghidra_b120\out"
```

日志：`D:/4/_tmp_view/ghidra_b120/run_b120.log:42` → `DECOMP DONE ok=12 fail=0 created=0 contained=0`。

### 1.4 产物（`D:/4/_tmp_view/ghidra_b120/out/`，12 个 `.c` / 686 行）

| 文件 | 行数 | 首行名 | 判读 |
|---|---:|---|---|
| `LiveOpsEvent_object__object_.ValidateEventData__10173d0.c` | 72 | `FUN_1810173d0` | **名字含 `", "` ⇒ Ghidra 拒建符号**（文档 §G7 纪律 1），**体是它自己**（签名 6 参 + 两个 `out` + MethodInfo，与 dump.cs 对得上） |
| `LiveOpsEvent_object__object_.LoadEventData__1016f80.c` | 61 | `FUN_181016f80` | 同上 |
| `LiveOpsEvent_object__object_.CreateNewSave__1016ca0.c` | 21 | `FUN_181016ca0` | 同上 |
| `LiveOpsEvent_object__object_.Validate__4de0e0.c` | 7 | `BaseAddressableLibrary__ValidateLibraryLoad` | **ICF 折叠**（体 = `return 1`），语义对得上「基类 `Validate` 恒 true」 |
| `LiveOpsEvent_object__object_.GetBaseData__4b3630.c` | 7 | `AuthenticationValues__get_AuthGetParameters` | **ICF 折叠**（体 = `return *(this+0x18)`），语义对得上 |
| `LiveOpsEvent_object__object_.UpdateData__1017250.c` | 37 | `FUN_181017250` | 体自洽 |
| `LiveOpsEvent_object__object_.UpdateSaveGame__1017340.c` | 20 | `FUN_181017340` | 体自洽 |
| `LiveOpsEvent_object__object_.Initialize__1016e70.c` | 51 | `FUN_181016e70` | 体自洽 |
| `LiveOpsEvent_object__object_.get_IsSegmented__1017600.c` | 26 | `FUN_181017600` | 体自洽 |
| `PlayEvent_object__object_.StartMatchNoCheck__1127900.c` | 187 | `FUN_181127900` | 体自洽 |
| `PlayEvent_object__object_.CheckPreviousMatch__1126a30.c` | 80 | `FUN_181126a30` | 体自洽 |
| `PlayEvent_object__object_.StartMatch__1127df0.c` | 117 | `FUN_181127df0` | 体自洽 |

**两条辅助符号**（本项目已有判据，别重查）：
- `FUN_1803f47a0()` = IL2CPP 的 **null 检查抛 NRE**（`/* WARNING: Subroutine does not return */`）；
- `FUN_1803f3780()` = **castclass 失败抛 InvalidCastException**；
- `FUN_1803f3730()` = **GC write barrier**;
- `thunk_FUN_18042e360(x, T)` = **isinst**（`x is T ? x : null`）；
- `System_TimeSpan__op_Inequality` 在这里被 ICF 折叠成**「两个指针不相等」**，实参是**两个 `System.Type`** ⇒ 语义 = `Type.op_Inequality`（`save.GetType() != typeof(TSave)`）。

---

## 2. `ValidateEventData` 方法体逐句

文件：`D:/4/_tmp_view/ghidra_b120/out/LiveOpsEvent_object__object_.ValidateEventData__10173d0.c`

签名（`.c:3-4`，6 个实参 ⇒ 末位是 **MethodInfo\***，泛型实例方法必有）：
```c
undefined8 FUN_1810173d0(longlong *param_1,      // this
                         undefined8 param_2,      // LiveOpsEventData data
                         longlong   param_3,      // LiveOpsEventPersistence save
                         longlong  *param_4,      // out TData  formatedData
                         longlong  *param_5,      // out TSave  formatedSave
                         longlong   param_6)      // MethodInfo*
```

| `.c` 行 | 原版动作 | 读法 |
|---|---|---|
| `12-13` | `*param_4 = 0; *param_5 = 0;` | **先把两个 out 置 null**（在任何判断之前） |
| `14-17` | 从泛型上下文取 TData 的 `Il2CppClass*`，`[+0x135]&1 == 0` 则走 `FUN_1803f1fa0` | 这是 **il2cpp 的「类型已初始化」懒检查**（`class_init`），不是业务逻辑 |
| `18-21` | `lVar2 = isinst(data, TData); if (lVar2 == 0) return 0;` | **`data is TData` 失败 ⇒ `return false`**（不抛、不打日志） |
| `22-33` | `if (save != null) { if (save.GetType() != typeof(TSave)) return 0; }` | 🔴 **精确类型闸**：`Object.GetType()` + `GetTypeFromHandle(泛型槽)` + `op_Inequality`，**不等就直接 `return false`** |
| `34-35` | `*param_4 = lVar2;`（+ write barrier） | `formatedData = (TData)data` |
| `36-51` | `if (save != null) { s = isinst(save, TSave); if (this.Validate(s)) { ...; goto LAB; } }` | 虚调用 `vtable+0x4e8` = `Validate(TSave)`；过 ⇒ **直接复用这份存档**（`lVar2 = s`，跳到收尾） |
| `52-66` | `CreateNewSave` **被内联**：`Activator.CreateInstance<TSave>()` → 若 `formatedData==null` 或实例==null ⇒ `FUN_1803f47a0()`（**抛 NRE**）→ `新存档+0x10 = formatedData.GetID()`（`vtable+0x188`）+ write barrier | `0x10` = `LiveOpsSave.eventId`。**注意：这里没有 `IsNew = true` 的写入**（原版靠字段初始化器；反汇编也没有） |
| `67-70` | `*param_5 = lVar2; return 1;` | `formatedSave = …`；**成功返回 true** |

### 还原成 C#（逐字对照）

```csharp
public bool ValidateEventData(LiveOpsEventData data, LiveOpsEventPersistence save,
                              out TData formatedData, out TSave formatedSave)
{
    formatedData = default;
    formatedSave = default;
    if (!(data is TData d)) return false;                        // .c:18-21
    if (save != null && save.GetType() != typeof(TSave)) return false;  // .c:22-33 ← 我们缺这条
    formatedData = d;                                            // .c:34-35
    if (save != null)
    {
        var s = (TSave)save;                                     // .c:41（isinst，已被上一行闸保证成功）
        if (Validate(s)) { formatedSave = s; return true; }      // .c:42-49
    }
    formatedSave = CreateNewSave(formatedData);                  // .c:52-66（内联）
    return true;                                                 // .c:70
}
```

**对三个问题的直接回答：**

1. **校验什么**：① `data is TData`（否则 false）；② `save != null` 时 **`save.GetType() == typeof(TSave)` 精确相等**（否则 false）。
   **失败时行为 = 返回 `false`**，**不抛、不打日志**。唯一的抛点在「新建存档」分支里（`CreateInstance` 返回 null / `formatedData` 为 null ⇒ NRE；两者实际都不可达，因为上一行已保证 data 非 null 且 TSave 是类）。
2. **对玩法参数（`gameplayVariables` / 事件配置 / BotConfig）有没有影响**：**没有直接读写**。
   `LiveOpsEventData` 的字段里**根本没有 `gameplayVariables`**（`dump.cs` 现读：`eventId / enabled / activationType / dates / filter / drawData / priority / location / showInMainMenu / mainMenuOptions`）；它在子类 `PlayEventData+0x80` 上。
   本方法**只做类型闸 + 新建默认存档**，一个业务字段都不碰。
   **间接影响**：它是 `LoadEventData` 的**总闸** —— 返回 false ⇒ `Data` / `SaveGame` **一个都不赋值**（保持旧值/null）⇒ 下游读到 `Data.gameplayVariables` 时是 null/旧值。也就是说「玩法参数对不对」取决于**这道闸放不放行**，而不是被它改写。
3. **我们有没有对应的校验链**：有 `ValidateEventData`（`Shell/LiveOpsEvent.cs:831`），但**细节不一致**（见 §5）。
   `IAfterRead` / `OnAfterRead` **不是这条链**：原版是**序列化层**在反序列化后调的（`SerializationHelper.OnAfterRead<T>` ← `TypedSerializer.ReadJson` / `TypedDictionarySerializer.ReadJson`），与 `ValidateEventData` 无调用关系；我们只在 `Shell/PlayEvent.cs:1014`（`SetBannedCards` 的本地等价）调一次。

---

## 3. 顺带导出/坐实的四条

### 3.1 `LoadEventData` —— **`ValidateEventData` 的唯一调用点**（`…LoadEventData__1016f80.c`）

| `.c` 行 | 内容 |
|---|---|
| `16-19` | 静态初始化守卫（`class_init`） |
| `23-26` | `if (this == null) throw NRE` |
| **`27-28`** | **`Handler = handler;`** ← **最先写**，在验证之前 |
| **`29-31`** | `if (ValidateEventData(data, saveGame, out d, out s)) { … }` ← **唯一调用点**（直接调用 `FUN_1810173d0`） |
| `32-37` | 成功才写 `Data(+0x18) = d` / `SaveGame(+0x20) = s` |
| `38-57` | `if (Handler != null) Handler.UpdateCachedSave(SaveGame)`（走接口虚调用：按 `Il2CppClass*` 比对 `DAT_1842a6b58` 找到接口，取 `(接口内槽号 + 0xf) * 0x10 + 0x138 + class`；`0xf` = **该接口在 vtable 里的起点**，不是方法槽号） |

⇒ 原版 C#：
```csharp
public void LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame, ILiveOpsHandler handler)
{
    Handler = handler;
    if (ValidateEventData(data, saveGame, out var d, out var s))
    {
        Data = d;
        SaveGame = s;
        if (Handler != null) Handler.UpdateCachedSave(SaveGame);
    }
}
```
**失败 = 静默什么都不做**（连 `Handler` 都已经先写进去了）。

### 3.2 `CreateNewSave`（`…CreateNewSave__1016ca0.c`）
`Activator.CreateInstance<TSave>()` → `save.eventId(+0x10) = data.GetID()`（`vtable+0x188`）→ 任一 null 抛 NRE。
**没有 `IsNew = true` 的显式写**（`LiveOpsSave.IsNew` 的字段初始化器就是 `true`）。与 `ValidateEventData` 内联的那份**逐句一致**。

### 3.3 `Validate`（`…Validate__4de0e0.c`）
`return 1;`（ICF 折叠名字，体 3 行）⇒ 「基类恒 true」坐实。

### 3.4 `GetBaseData` / `UpdateData` / `UpdateSaveGame` / `Initialize` / `get_IsSegmented`
- `GetBaseData` = `return *(this+0x18)`（= `Data`）✅ 与我们一致。
- `UpdateData` = `if (data == null) throw NRE; if (data.GetType() != typeof(TData)) return; Data = isinst(data, TData)` ⇒ **精确类型 + 子类也不收**（我们注释已写对 ✅）。
- `UpdateSaveGame` = `s = isinst(saveGame, TSave); if (s != null) { SaveGame = s; LoadEventData(Data, SaveGame, Handler); }` ✅ 与我们一致（差异只在它内部那次 `LoadEventData` 会重跑 `ValidateEventData`）。
- `Initialize` = `Data != null && Data.filter(+0x30) != null` 才继续；`filter+0x18` 为 0 ⇒ return；否则 `Handler.<接口方法>(SaveGame+0x20 + Data.filter+0x1c * 3600000)`（接口虚调用，vtable 起点 0xa；**按形参（毫秒时间戳）推断为 `ScheduleHandlerRefresh(long triggerTime)`，名字未逐字对上**）。`*3,600,000` = **小时→毫秒**。
- `get_IsSegmented` = `Data == null ⇒ NRE`；`Data+0x24 != 0 ⇒ true`；否则 `Data+0x30(+filter)→+0x30` 走 `DynamicList.get_Count > 0`。

### 3.5 `PlayEvent<TSave,TData>.StartMatchNoCheck`（B120-W18 的第 ① 条，`…__1127900.c`，187 行）
主干：`GameModes.CurrentPlayingEvent = this` → 虚调用 `vtable+0x5e8`（返回 false 就 return）→ 开窗（`WindowsManager.OpenWindow`）→ `AlliancesManager.IsInGroup` 为真才 `MenuActions.QueueAction(...)` → `PlayerDataManager.CreatePlayerBattleData(playerData, deck, 0, 0)` → `MatchMakerManager.FindMatch(this, <13 字段的战斗数据结构>, deck, …)`。
**对 BotConfig 的影响：无** —— 它只把 `CreatePlayerBattleData` 的返回结构整块拷给 `FindMatch`，本方法内**没有任何 BotConfig 字段的读或写**；`FindMatch` 的两个额外实参是 MethodInfo 指针，不是配置。
（⚠️ 这是主干级读法，逐字段的 `PlayerBattleData` 13 项映射留给需要它的那条账；本条只是「能读到、日志可达」。）

---

## 4. 我们现码 vs 原版：逐条对账

对象：`D:/4/Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEvent.cs`

### 4.1 `ValidateEventData`（`:822-846`）

| # | 原版 | 我们 | 判定 |
|---|---|---|---|
| 1 | `formatedData = default; formatedSave = default;` 先置空 | `= null` 先置空 | ✅ 一致 |
| 2 | `if (!(data is TData d)) return false;` | `var d = data as TData; if (d == null) return false;` | ✅ 一致（等价） |
| 3 | **`if (save != null && save.GetType() != typeof(TSave)) return false;`** | **没有这道闸** —— 直接 `var s = save as TSave;` | ❌ **不一致** |
| 4 | `formatedData = d;` | 同 | ✅ |
| 5 | `if (save != null) { var s = (TSave)save; if (Validate(s)) { formatedSave = s; return true; } }` | `if (save != null) { var s = save as TSave; if (s != null && Validate(s)) {...} }` | 🟡 只差在：原版靠第 3 条已经保证 `(TSave)save` 必成功（失败早已 return false），我们靠多一个 `s != null` |
| 6 | `formatedSave = CreateNewSave(formatedData); return true;` | 同 | ✅ |

**具体的可观测差异**（同一输入，两边结果不同）：

| 输入 | 原版 | 我们 |
|---|---|---|
| `data` 是 TData，`save` 是 **`LiveOpsSave`（TSave 的父类实例）** 且 TSave=`PlayEventSaveGame` | **`return false`**；调用方拿到 `(null, null)` | **`return true`**，`formatedSave = 新建的 PlayEventSaveGame` |
| `data` 是 TData 子类实例 | `data is TData` ⇒ 通过（isinst 允许子类） | 同 ✅ |
| `save` 是 TSave 的**子类**实例 | `GetType() != typeof(TSave)` ⇒ **`return false`** | `as` 成功 ⇒ `return true` |

⇒ 原版是「**类型必须完全相等，否则这次装载整体失败**」，我们是「**类型对不上就悄悄换一份新存档**」。**语义不同，不是风格差异。**

### 4.2 `LoadEventData`（`:791-820`）

| # | 原版 | 我们 | 判定 |
|---|---|---|---|
| 1 | **`Handler = handler` 最先写**（验证之前） | `Handler = handler` 在最后 | 🟡 只在「早退路径」可见差异（原版早退时 Handler 已更新，我们不更新） |
| 2 | **`if (ValidateEventData(...))` 包住整个赋值** | **不调 `ValidateEventData`**，自己 `as` 之后直接写 | ❌ **不一致（结构级）** |
| 3 | 失败 ⇒ **静默什么都不做** | data 转不成 ⇒ `Debug.LogError` + `return`（出声，有意差异，已记） | 🟡 有意差异（可接受，但账里要写） |
| 4 | `saveGame` 转不成 ⇒ 由 `ValidateEventData` **返回 false**（不写 SaveGame） | `SaveGame = s(=null)`，**把已有的存档清成 null** | ❌ **不一致（有副作用）** |
| 5 | `UpdateCachedSave(SaveGame)` 只在成功分支 | 恒调（哪怕 SaveGame 被写成 null） | ❌ 由 4 带出 |

> 因 2 ⇒ 我们这条路上的 **`Validate(save)` 闸整条不生效**（原版每次 `LoadEventData` 都要过一遍）。
> 当前 `Validate` 基类恒 true 且**全仓无 `protected override bool Validate(TSave)`**，所以**今天没有可观测后果**；但任何子类一旦按原版覆盖 `Validate`，我们的三条路径（`UpdateSaveGame` / `ResetEventProgress` / 直接调用点）都会**绕过**它。

### 4.3 调用链对照（谁调谁）

| 调用方 | 原版 | 我们 |
|---|---|---|
| 工厂装载 | `LoadEventData(data, save, handler)`（`ValidateEventData` 在**里面**） | `LocalLiveOpsEvent.Create` **显式先调** `ValidateEventData`，再 `LoadEventData(fd, fs, handler)`（`Shell/LiveOpsHandler.cs:921-931`） |
| `UpdateSaveGame` | `LoadEventData(Data, SaveGame, Handler)` ⇒ 重跑 `ValidateEventData` | `LoadEventData(Data, SaveGame, Handler)` ⇒ **不跑** `ValidateEventData` |
| `ResetEventProgress` | `LoadEventData(Data, newSave, Handler)` | 同（`LiveOpsEvent.cs:923`） |
| `DraftLocalEvent` | —— | `_body.LoadEventData(data, save, null)`（`Shell/DraftLocalEvent.cs:286`） |

⇒ 结果是**同一个最终状态**（因为 `Create` 那条路显式补了一次），但**结构上不是原版那条路**，且三处直接调用点行为不同。

---

## 5. 影响面（要不要改、多急）

1. **产品主路径**：`LiveOpsSaveStore.Load(id, typeof(PlayEventSaveGame))` → `JsonUtility.FromJson(json, saveType)` **返回的就是 `PlayEventSaveGame`** ⇒ `GetType()` 与 `typeof(TSave)` 相等 ⇒ 原版那道闸**会放行**。所以我们今天的实际行为**与原版一致**。
2. **风险点 = 存档类型漂移**：若某天 `LoadFromCatalog` 的 `saveType` 退回基类（`LiveOpsEvent.cs:972` 的注释记着**过去就是这么写的**，2026-11-02 `B118_W5` 才改成子类），`Load` 出来的就是 `LiveOpsSave` ⇒ **原版会 `return false`、该事件整条不装载**；我们则会静默造一份新存档。**两种处置都不是好结局，但必须照抄原版那一种**（铁律 11 + §18·0）。
3. **`LoadEventData` 无 `ValidateEventData`**：今天的后果是 `SaveGame` 可能被写成 null（原版会保持旧值 + 不回调 `UpdateCachedSave`），以及 `Validate` 闸失效。
4. **对玩法参数 / BotConfig：无直接影响**（§2 第 2 问）。

**结论：❌不一致（两处）。** 建议按铁律 11 记账待修（不在本只读代理的权限内改码）。

---

## 6. 仍拿不到 / 未查清

| 项 | 状态 |
|---|---|
| `ValidateEventData` 的**其余具体化**（`<object,object>` 代表体已读；`DraftEventBase<X,Y>` / `MissionEvent<X,Y>` / `PlayEvent<X,Y>` 等是否逐字同体） | **未逐份导**。判据上它们是同一份 IL 的不同 instantiation，**应当**同体；但按「一个值≠全部情况」（铁律 5·c）**没有验证过**。要验就重复 §1.3，把 `GenericInstMethod` 里各具体化的 RVA 塞进名单（一趟几十秒）。 |
| `FUN_1810173d0` 的 `param_6` 泛型上下文**槽位表**（+0x08/+0x18/+0x28/+0x30/+0x38）的**完整结构** | **按用法反推**（+0x08=`TData` class、+0x18=`TSave` class、+0x28=`TSave` type handle、+0x30=`TData` type handle、+0x38=`ValidateEventData` 的 MethodInfo），**没有**逐字节读 `Il2CppGenericContext` 的定义来钉死。**不影响本件结论**（每个槽的语义都被它的用法唯一确定）。 |
| `FUN_1803f47a0` / `FUN_1803f3780` 的**精确导出名** | 沿用仓库既有判据（NRE / InvalidCastException throw helper），**本轮没再反汇编它们**。 |
| `PlayEvent.StartMatchNoCheck` 里 `PlayerBattleData` 13 字段到 `MatchMakerManager.FindMatch` 的**逐字段映射**、以及 `FindMatch` 的 BotConfig 形参 | **只读到「整块拷贝 + 两个 MethodInfo 实参」这一层**。它不在本件范围（`ValidateEventData` 的答案已经很确定），留给需要它的那条账。 |
| `LiveOpsEventData` 为什么没有 `gameplayVariables` | 已确认它在 `PlayEventData+0x80`；**没查**「哪些活动类型不带玩法参数」的全集列表。 |
