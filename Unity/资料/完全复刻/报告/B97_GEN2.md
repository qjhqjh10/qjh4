# B97_GEN2 报告 —— `账/泛型与启动链_补读.tsv` 的 8 条 B + 12 条 C

> 执行代理 `B97_GEN2` · 2026-10-29 · 只写：`CardPresentation/Shell/LiveOpsHandler.cs`（一处，零行数变化）·
> 新建 `CardPresentation/Editor/B97_GEN2_Assert.cs` · 账补丁 `账/_parts/B97_GEN2.tsv` · 本报告。
> ⛔ 未跑 Unity / ruleprobe / `play check`；⛔ 未动 git；⛔ 未改账主表 / 正本 / 别人的文件。
> `TMPDIR=/tmp/wf_gen2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（收尾前最后一次跑）。

---

## §一 结论总表（20 条一条不落）

| # | 账行 | 原版 类.方法 | 原档 | **现档** | 一句话 |
|---:|---|---|:--:|:--:|---|
| 1 | 泛型:137 | `ToggleCollectionFilter<T,TOption>.b__5_0(bool b)` | B | **B（保持）** | 行为已等价，**只被「A 需双侧方法级」口径挡住**；零代码可做，等调度台二选一 |
| 2 | 泛型:138 | `ToggleCollectionFilter<T,TOption>.b__7_2(T rc)` | B | **B（保持）** | 同上（同一份筛选链，四条同进同退） |
| 3 | 泛型:216 | `LiveOpsHandler<TEvent>.RefreshHandler(LiveopsHandlerData)` | B | **A** | 本批补齐原版那**两跳**：`await Task.Yield()`（条件恒 false，VA 实读）＋ `await InitializeHandler()`（`+0x2d8` = Slot 26，vtable 三锚点互证） |
| 4 | 泛型:232 | `MissionEvent<TS,TD>.RerollChallenge(MissionChallenge, Action)` | B | **A** | 等价物在 `DailyData.cs:2713`，**真调用点 `MissionRerollPopup.cs:487`**（「0 调用方」已过期） |
| 5 | 泛型:236 | `MissionEvent<TS,TD>.ReleaseContainer()` | B | **A** | 两分支逐句都在；「只缺 Addressables 容器层」已过期（`LiveOpsLocalCatalog.cs:1529`） |
| 6 | 泛型:577 | `ConfigProvider<T>.UnpackInternal(Dictionary<string,string>)` | B | **A** | 逐句等价（纯本地）；本批补断言 CFG-1 |
| 7 | 泛型:578 | `ConfigProvider<T>.OnDestroy()` | B | **A** | 逐句等价；本批补断言 CFG-2 |
| 8 | 泛型:759 | `DraftEventBase<TS,TD>.SetCustomMatchOptions(RoomOptions, int)` | B | **A（带 ⚠️）** | 纯函数逐字等价 + 已有断言 C1–C8；**0 消费者**（本仓轮抽单机开战、没有 Photon 房参数面）—— 若要「轮抽开 P2P 房」待调度台裁，裁「不开」可回退 B |
| 9–20 | 泛型:725–736 | `DynamicList<T>` 的 12 个成员 | C | **A（12/12）** | 类本体**已由 `B97_MENU` 建在 `Shell/DynamicList.cs` 并接线**；本批只**核对 + 补签名断言**，⛔ 没有另建第二份 |

**数字**：8 条 B ⇒ **6 A + 2 保持 B**；12 条 C ⇒ **12 A**。
**未清零的 2 条**（`:137/:138`）不是没查清，是**撞一条口径**（见 §三），需要调度台一句话。

---

## §二 逐条证据

### ① `:216` `RefreshHandler(LiveopsHandlerData data)` —— 本批唯一改了生产码的一条

原版真身（现读全文）：`d:/2/tools/decomp_full/LiveOpsHandler._RefreshHandler_d__29_object___MoveNext.c:1-271`。

原序（`.c` 逐句）：
`config = ConfigManager.GetConfig<FeatureConfig>()` → `await Task.Yield()` → `await refreshSemaphore.WaitAsync()`
→ `await Manager.BuildHandler(this, data, config)` → `await <虚调用 +0x2d8>` → `EventsRefreshed?.Invoke()`
→ `Signal.Raise(new LiveOpsHandlerRefreshedSignal(this))` → `CustomDebug.Log("[" + HandlerKey + "] Handler Refreshed")` → `sem.Release()`。

**旧账挂着的两条未闭合判据，本批解出来了：**

1. **`Task.Yield()` 的条件** —— `.c` 里是三跳**同址**（ILSpy 注 `Method not found @1806FB110` ×3）。
   直接读二进制（`d:/2/unity_run_ref/GameAssembly.dll`，PE 节表换算出 `il2cpp` 节内偏移）：
   **RVA `0x6FB110` 的机器码 = `32 c0 c3`** = `xor al,al; ret` ⇒ **恒返回 `false`** ⇒ 三跳全 false
   ⇒ **`await Task.Yield()` 是无条件的**（`.c` 里 `if (cVar3 == '\0')` 恒真）。
2. **`+0x2d8` 是哪个虚方法** —— vtable 步长是 `VirtualInvokeData` 的 **16 字节**（不是 8），
   三个**互相独立**的锚点把整条槽表钉死：
   | 偏移 | 槽 | 判据（独立来源） |
   |---|---|---|
   | `0x278` | Slot 20 `get_HandlerKey` | `_d__29` 里拿它拼 `"[" + HandlerKey + "] Handler Refreshed"`（`.c:245`） |
   | `0x2a8` | Slot 23 `RefreshHandler(LiveopsHandlerData)` | `_d__26:88` / `_d__27:33` 传的实参正是 `this+0x60`（`handlerCache`） |
   | `0x2e8` | Slot 27 `GetEvents(bool)` | `ForgeHandler.HasAnyArmyRewardsToCollect:16` · `RankedScoreBoostHandler.GetBoostsFor:21` · `Shops.GetAllOffers:12` · `RewardEventHandler…b__0:36` **四处都传 bool** |
   ⇒ `0x2d8 = 0x2a8 + 3×16` = **Slot 26 = `InitializeHandler()`**（`dump.cs:53589`）。
   **旧注释说的「`Slot: 5 = GetBaseEvents` 的 `0x2e8` 互相矛盾」是归错槽** —— `0x2e8` 是 Slot 27，不是 Slot 5。

**改法（`Shell/LiveOpsHandler.cs:566` / `:569`）**：完全按原版序、**零行数变化**（本文件 1707 行未变）：
```
566  var cfg = LiveOpsFeatureConfig.Current; await Task.Yield(); await refreshSemaphore.WaitAsync();   // 原版序：先取 config，再 Yield，再等互斥
569  await Manager.BuildHandler(this, data, cfg); await InitializeHandler();                            // 原版 +0x2d8 = Slot 26
```
`EventsRefreshed` / `Signal.Raise` / `Log` 一并挪进 `try` 块（原版它们在 `InitializeHandler` **之后**、`Release` **之前**）。

**如实标的差异（两条，都已写进代码注释）**：
- 原版异常路径**不** `Release`（没有 `try/finally`）⇒ 抛一次异常信号量永久占住；我们保留 `finally` 兜住。**未改**（改掉是拿死锁换「逐字相同」，不划算）。
- `data == null` 我们**出声返回**（原版 NRE）—— 这是既有差异，未变。

### ② `:232` `RerollChallenge(challenge, onComplete)`

- 原版：`Confirm` → **服务端** CloudScript 换一条 → `onComplete`（`decomp_full/…__RerollChallenge__102f210.c`）。
- 我们：`Shell/DailyData.cs:2713 RerollChallenge(int i)` ⇒ 一句 `return RerollDaily(i);`（本体 `:635`，就地换描述/目标/奖励/进度归零 + 落盘）。
- **真调用点** = `Shell/MissionRerollPopup.cs:487`（`Confirm()`；`:481-487` 那段注释记着 B95_D1 把调用从 `RerollDaily` 改成名字对齐入口）。
  ⇒ 旧账「别名 `DailyData.cs:2713` **0 调用方**」**已过期**，就地订正在账里。
- `onComplete` 等价 = `Close()` 之后的 `_onRerolled()`。
- 🔶 服务器相关自设计：**不扣 `RerollPrice`**（用户 2026-09-17 边界②「不做真实经济」）。

### ③ `:236` `ReleaseContainer()`

`Shell/MissionEvent.cs:387-396` 逐句对 `…_ReleaseContainer__102f1c0.c`：
`if (UseSmallContainer) { LiveOpsAssetUtility.UnloadComponents(this, SubContainer); return; } base.ReleaseContainer();`
（键 = `SubContainer`(1)，与 `GetContainer` 成对）。等价物 `Shell/LiveOpsLocalCatalog.cs:1529`（按 key 取引用 → 引用计数 −1 → 到 0 才走 `ReleaseAsset` 缝）。

### ④ `:577` / `:578` `ConfigProvider`

`Shell/ConfigProvider.cs:135`（`UnpackInternal`）与 `:156`（`OnDestroy`）逐句对上 `out_b94/ConfigProvider_object_.{UnpackInternal__1682730,OnDestroy__16826e0}.c`：
new T → 按 `Key` 取 JSON → 反序列化覆盖 → `ConfigCache.Write` → 返回；`config != null` ⇒ `ConfigCache.Clear`。
两处已记差异不变：`JsonUtility` 顶 Newtonsoft；缓存宿主 🔶（`FUN_180002340` 未解，R-M §四 G4）。

### ⑤ `:759` `SetCustomMatchOptions`

`Net/NetRuntime.cs:441` 的纯函数把 `.c` 的四个动作逐字复刻（C1 胜场 / C4 ELO / C5 连败 / lobby 5 键；`ELO<=800 && won<3 ⇒ ELO=0`），
断言 = `Editor/B94Y6_Assert.cs:170-190` 的 C1–C8。
⚠️ **缺口不是判据缺，是消费者缺**：原版写 Photon `RoomOptions.CustomRoomProperties`，本仓轮抽 `To Battle` 走
`BattleDriver.SetPendingPlayMode(GameMode.ClosedDeck)`（`Shell/DraftModeWindow.cs:22`）单机开战 ⇒ **没有房参数面**。
要接 = 自设计一套房参数映射（属「服务器相关 ⇒ 允许自设计」那一档）—— **等调度台裁「轮抽要不要开 P2P 房」**；
裁「不开」⇒ 本条按休眠处理、可回退 B，**不影响「方法本身已等价」这个结论**。

### ⑥ `:725`–`:736` `DynamicList<T>` 的 12 个成员（12 条 C）

- 类本体**已由 `B97_MENU` 建好**：`CardPresentation/Shell/DynamicList.cs`（本批**只读**，⛔ 未另建第二份）。
  逐成员落点：`GetList:88` · `ListType:94` · `GetIndexType:102` · `Get<T1>(T):110` · `Get<T1>(int):116` ·
  `GetAll<T1>:125` · `Add:132` · `Inser:140` · `IndexOf:146` · `Clear:155` · `Remove:162` · `RemoveAt:168`。
- **真消费者**（不是空壳）：`LiveOpsEventFilter.Triggers` / `.Conditions`（`Shell/LiveOpsEvent.cs:135/139` 的**静态类型已改成** `DynamicList<T>`）
  → `Shell/LiveOpsFilter.cs` 的 `RegisterTriggers` / `CleanupTriggers` / `Validate`。
- 每条都对上 `out_cpp2il_dev_ilspy/DynamicList.cs` 的方法体（`198/203/211/228/269/321/555/577/582/593/605/610`），
  `:732` 的 `Inser` **拼写与形参次序 `(element, index)` 照原版**（⛔ 别改）。
- 重复账提醒：`菜单_外壳:142/143/149/150/151/154/155` 是同一批成员的**另一份账**，但那份的「原版类」列写的是 **`DynamicList`**（不带 `<T>`）
  ⇒ `(原版类, 方法)` 键**不撞**（`check_patch_conflicts.py` 现跑：2 份补丁 0 冲突）。真复刻率的分母仍虚高 7 条，**建议下一轮账务并掉**。

---

## §三 没清零的 2 条：`收藏:137` / `收藏:138`（同 `泛型:137/138`）

**为什么不动代码**：分诊（`B96_小域19_分诊.md` §三、§八）的结论是「行为已等价，零代码可做」，
且明写「⛔ 别为凑 A 建一个没人调的空壳方法」。本批现读复核：
- `b__5_0`（SetupFilter 的 toggle 回调）等价物 = `Shell/CollectionWindow.cs:2857 ApplyFilter` 的 `SetFilter + OnChanged()`（= 原版 `NotifyFilterChange`）；
- `b__7_2`（Filter 的谓词）等价物 = `Shell/CollectionWindow.cs:2074 FilteredCosmoNames` ＋ `Deck/DeckEditorState.cs:568 VisibleCards`（五类逐类内联）。

**要收口只有一条路**（调度台二选一）：
- (a) **放宽口径**：接受「内联谓词所在方法」为 A 档落点 ⇒ `收藏:317` `收藏:318` `泛型:137` `泛型:138` **四条一起 A**；
- (b) **维持 B**：保留 2026-10-25 那条「A 需双侧方法级」的裁定，四条一起留 B。
⚠️ 第三方案（真建一个 `ToggleCollectionFilter<T,TOption>` 类并把筛选链改过去）**不建议**：那是把现成的 `DeckFilter`+`FilterPanelModel` 那一整套 UI 筛选件推翻重做，牵动面远大于收益，且会让「谓词从哪来」变成两份。

---

## §四 改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/LiveOpsHandler.cs` | `RefreshHandler(LiveopsHandlerData)` 补原版两跳 + 方法 doc 就地订正（549–552 / 566–575） | **1707 → 1707（零变化）** |
| `Assets/CardPresentation/Editor/B97_GEN2_Assert.cs` | **新建**：`CheckB97Gen2()`（DL12-* / CFG-* / RH-*） | 新文件 |
| `Unity/资料/完全复刻/账/_parts/B97_GEN2.tsv` | **新建**：20 行、11 列、首行表头 | 21 行 |

⚠️ **所有权提醒**：我动了 `CardPresentation/Shell/LiveOpsHandler.cs`（本轮在飞名单里**没有**它，`git status` 也是 clean 才动的手）。
⇒ 请调度台在 `文件所有权_当前.md` 里把这一个文件登记为已释放。

---

## §五 断言（`Editor/B97_GEN2_Assert.cs`）

**接线建议**：`RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里，**紧接 `{ CheckB97W1Grants(); }` 那一行之后**加：
```csharp
        // 🆕 2026-10-29（调度台代接线）：B97_GEN2 —— DynamicList 12 成员签名 · ConfigProvider 缓存两跳 ·
        //   RefreshHandler 的 Task.Yield + InitializeHandler（账 泛型与启动链_补读:216/577/578/725-736）。
        { CheckB97Gen2(); }
```
（与 `B97_W1_Assert.cs` 同一条理由：⛔ 不用 `Step(...)`、方法名不带 `Test`/`Report` 前缀 —— 那会牵动 `_ranCases` 与 `gen_diag_runner.py`。）

**三个关键「灭自证」信号**（都写在断言文案里）：
| 改动 | 哪条会红 |
|---|---|
| 删掉 `await Task.Yield();` | `RH-2`（同步上下文 `Post` 计数 1 → 0） |
| 删掉 `await InitializeHandler();` | `RH-3`（`InitCalls` 1 → 0） |
| 把 `InitializeHandler` 挪到 `EventsRefreshed` 之后 | `RH-4`（顺序串 `validate,setup,init,events` 变） |
| `Inser` 改名 `Insert` / 形参对调 | `DL12-7b` / `DL12-7d` |
| `ConfigCache.Write` / `Clear` 改空体 | `CFG-1` / `CFG-2` |

**技术选择（写进断言注释）**：`Task.Yield()` 的效果靠**一个只数 `Post` 的 `SynchronizationContext`** 观测 ——
夹具里被 await 的三个 Task 全部 `Task.CompletedTask`（同步完成）⇒ 只有 `Task.Yield()` 会产生一次上下文跳。
这样既不需要帧循环、也不会把 `BuildHandler` 甩到线程池上跑（`Post` 里**内联**执行续体）。
探针 `B97Gen2ProbeHandler` 直接 `new`（我们工程里 `LiveOpsHandler` 是**纯 C# 类**，不是 MonoBehaviour）；
收尾用反射把探针自己从 `LiveOpsManager.HandlerLookup` 里摘掉（⛔ 没调 `ResetHandlers()`，那会清掉内建的 `local`/`GameModes`）。

---

## §六 待接线 / 待裁定

1. **`:759` 的消费者**（`ComputeDraftMatchOptions`）：要在轮抽里开 P2P 房才需要；属服务器相关自设计 ⇒ 等调度台裁。
2. **`:137/:138` 的口径**：二选一（见 §三）—— 一句话就能收掉 4 条账。
3. **`DynamicList` 7 条重复键**（`菜单_外壳` ↔ `泛型与启动链_补读`）：键不撞、可各自合并，但复刻率分母虚高 7 条 ⇒ 建议并掉。
4. **`:216` 的指纹影响未验**：`Task.Yield()` 不写 `ctx.Events`（引擎侧无关），但 `InitializeHandler` 会跑 `e.Initialize()`
   ⇒ 若某事件 `Initialize` 里有副作用，**需要一次 `play check` 对指纹**（本批红线不跑 Unity）。

---

## §七 没查清 / 覆盖边界

- **`:216` 的「`GetConfig<FeatureConfig>()` 之前那三跳到底是什么」没查清**：只知道它们同址、恒返 `false`。
  本工程拿不到 `FeatureConfig` 的字段表（占位类只实现了 `IsHandlerEnabled`）⇒ 结论**只覆盖「在这份 build 上恒 Yield」**，不覆盖「原版源码为什么这么写」。
- **未跑任何宿主**：`RuleEngineTest.Run` / `ShellScene.Run` 一条都没跑（红线：不跑 Unity）⇒ 断言只保证编得过 + 逻辑自洽，跑绿由调度台在同步点做。
- **`:759` 的 P2P 房参数面**：本仓 `Net/` 下没有任何 room-properties 概念（`grep RoomProperties` 只命中那段注释）⇒ 「怎么映射」不是查判据能查出来的，是产品决策。
- **`:137/:138` 的第三方案代价评估**：只做了定性判断（会推翻 `DeckFilter`+`FilterPanelModel`），没做逐文件清点。

## §八 顺手发现（不改代码，报给调度台）

1. **`日常_奖励.tsv:395` 的注释也已过期**：它写「`await <虚调用，单参 = 字段 +0x60>`」并说「残余只有 `+0x60` 的字段名是不是 `handlerCache` 这一层命名对应」——
   现在 `:216` 的槽表已解（`0x2a8` = Slot 23 确认），那一层可以一并结案。
2. **`dump.cs` 的 `Slot` 编号 + `VirtualInvokeData` 16 字节步长**这条换算方法值得记进 `资料/全量反编译_入口与用法.md`：
   以前踩的坑（「Slot 编号与 vtable 偏移对不上」）根因就是**按 8 字节步长算**。判据模板：
   先找一个**签名能唯一确定**的空调用当锚点（这里是「传 `LiveopsHandlerData` 的只有 Slot 23」），再加第二个不同参数的锚点（`GetEvents(bool)`），两条一交叉就能把整张槽表定死。
3. **`Shell/LiveOpsManager.cs:67` 那条 `🔶 自己设计` 打标格式不合 铁律 18**（`IsHandlerEnabled` 那条写的是 `TODO(谁定的)`，没有「谁定的 + 日期」）——
   对照：`Shell/ConfigProvider.cs:35` 那条是合规的（`调度台 Y6 2026-10-29 定`）。建议顺手统一（本批未动，避免越界改别人的文件）。
