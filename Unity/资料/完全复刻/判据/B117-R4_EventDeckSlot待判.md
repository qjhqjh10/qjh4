# B117-R4 — `[SaveCenter] 缺 key '_ours_EventDeckSlot'` 待判

只读判据代理 `B117-WR4` · 2026-11-02 · 壳/活动域 · 只读，未改任何 `.cs` / 数据 / 账 / 活状态表

---

## 一、结论（一句话）

**① 本来就没这个档（不是缺陷）** —— 写口存在（`SaveEventDeckSlots`）、接线也对（`RefreshDeckColumn → RememberEventDeck`），
只是**被 `EventId` 恒 null 挡着**（类里自己写明「今天就全是这种」⇒ 休眠，不是坏掉）；读口拿到 null 后**按设计回落全局当前卡组**。
⇒ **不是「该写而没写」的存档缺口**，但**这条日志每启动一次就响一遍**，应照本仓既有先例加 `Exists` 闸压掉噪声。

⚠️ **另有一条真缺口（顺手发现，不是本条结论）**：**原版形状的活动卡组槽（`PlayEventSaveGame.defaultDeckId`）在本仓是死码** ——
字段/读写方法都在，但 **0 调用点**，且 `LiveOpsSaveStore.Load` 一律按**基类** `LiveOpsSave` 反序列化 ⇒ 那个字段**永远读不回来**；
而活着的却是另一份自定键 `_ours_EventDeckSlot` ⇒ **同一个槽位两套存储、其中一套是死的**。详见 §五。

---

## 二、直接证据：这个键是什么、谁读、谁写

| 项 | 位置 | 现读 |
|---|---|---|
| 键定义 | `CardPresentation/Shell/LiveOpsEventWindow.cs:2263` | `public const string EventDeckSlotKey = SaveKeys.OursPrefix + "EventDeckSlot";` |
| 键放行 | `Shell/SaveKeys.cs:307` + `:350` | `OursPrefix = "_ours_"`；`IsKnown` 对 `_ours_` 前缀**直接放行** ⇒ 不用改键表 |
| **读口** | `LiveOpsEventWindow.cs:2281`（在 `LoadEventDeckSlots()`，`:2277-2293`） | `SaveCenter.Get<EventDeckSlotFile>(EventDeckSlotKey)`；`null` ⇒ `return`（`:2282` 注释自陈「第一次跑 / 档丢了 / 坏档」） |
| **读口调用点** | `LiveOpsEventWindow.cs:551`（`Open()` 首句） | `LastOpened = this; LoadEventDeckSlots();` |
| **写口** | `LiveOpsEventWindow.cs:2296-2311`（`SaveEventDeckSlots()`） | `SaveCenter.Set(EventDeckSlotKey, f)`（`:2308`）；失败会 `LogWarning`（`:2309`） |
| **写口调用点** | `LiveOpsEventWindow.cs:2194`（在 `RememberEventDeck()`，`:2186-2195`） | `SaveEventDeckSlots();` |
| **写口上游** | `LiveOpsEventWindow.cs:1897`（`RefreshDeckColumn()` 收尾） | `OnDeckColumnChanged(); RememberEventDeck();` |
| **写口第一道闸** | `LiveOpsEventWindow.cs:2188` | `if (string.IsNullOrEmpty(EventId)) return;  // 没有事件实例 ⇒ 原版这一跳根本不存在` |
| `EventId` 声明 | `LiveOpsEventWindow.cs:2043`（文档 `:2040-2042`） | `public string EventId;` · 文档原文：「**由打开它的那一侧填**……**没填 = 本窗不是由事件实例开的（今天就全是这种）**」 |

### 2.1 生产路径上 `EventId` 到底会不会被填 —— **不会**

- 全仓 `EventId = ` 赋值点逐个看过（`CardPresentation/Shell` + `Editor`）：
  - **唯一落在 `LiveOpsEventWindow` 实例上的赋值 = `Editor/B104_P4_Assert.cs:318`（自检夹具）**；
  - `ParentEventWindow.cs:996` 的 `tab.EventId = ev.EventId;` 赋的是 **`EventTab`**（页签对象），**不是窗口**；
  - `LiveOpsEventWindowWithTabs.cs` 里 `EventId` **0 命中**。
- 子类也没有覆写那条虚口：`protected virtual string LocalLiveOpsEventId { get { return null; } }`（`:1520`），
  `RankedEventWindow.cs` / `SkirmishEventWindow.cs` 里 **`LocalLiveOpsEventId` 0 命中**。
- `MakeEventTab` 的两枝也印证：只有「本地目录里有**在期且可选中**事件」时才 `tab.EventId = ev.EventId`（`ParentEventWindow.cs:994-997`），
  否则走 `FallbackTabLabel` 兜底（`:1000-1002`）。
- 历史记录同口径：`资料/完全复刻/报告/B90_N3.md:64` —— 「两个子类……`EventId` 也恒 null ⇒ 本批新接的『每活动卡组槽』……**今天都是休眠的**（一旦有事件实例就自动生效）」。

**⇒ 推论**：`RememberEventDeck()` 在第一句就返回（`:2188`）⇒ `SaveEventDeckSlots()` **生产路径一次都不会跑** ⇒
`_ours_EventDeckSlot` 文件**永远不会被创建** ⇒ 每次进程里第一次开活动窗都必然命中 `SaveCenter` 的「缺 key」。

### 2.2 那条日志本身的行为（为什么它是 Warn 而不是 Error、且只响一次）

| 位置 | 现读 |
|---|---|
| `Shell/SaveCenter.cs:186-196` | 文件不存在 ⇒ `SpeakOnce("missing" + 竖线 + key, false, "[SaveCenter] 缺 key …）⇒ 返回 null。⛔ 不静默当默认值：调用方要自己区分「第一次跑」与「档丢了」")`（原文的 id 是 `"missing\|" + key`） |
| `Shell/SaveCenter.cs:770-777` | `SpeakOnce` 用 `_said.Add(id)` 去重 ⇒ **每进程同一条只响一次**，且 `isError=false` ⇒ `Debug.LogWarning` |
| `Shell/SaveCenter.cs:252-257` | `Exists(key)` = `File.Exists(PathOf(key))`（**不发声**） |
| **本仓既有先例** `Shell/SaveCenter.cs:1289` | 「⚠️ 先用 `Exists` 挡一道：直接 `GetRaw` 会在『第一次』时打一条『缺 key』的警告，**每启动一次响一遍**」 |

---

## 三、判据：原版这个槽位是什么、住在哪

| 出处 | 现读 |
|---|---|
| `d:/2/Warpforge_code/Scripts/Assembly-CSharp/PlayEventSaveGame.cs:15` | `public string defaultDeckId;` —— 原版 `PlayEventSaveGame : LiveOpsEventPersistence`（`:6`），**一活动一份** |
| `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/PlayEvent.cs:1371-1395` | `SetDefaultDeck(CardDeck deck, bool upload = true)`：把 `deck.deckId` 写进 `SaveGame.defaultDeckId`（`+0x38`），随后 `PlayfabWrapper.GenericCloudScriptHandler(CloudHandle.SetGamemodeDefaultDeck, payload)` |
| 同上 `:1399-1416` | `UploadDefaultDeck()`：同样的 `CloudHandle.SetGamemodeDefaultDeck` |
| 本仓同名实现 | `Shell/PlayEvent.cs:185`（字段）· `:742-753`（`GetEventDeck()` 读）· `:788-798`（`SetDefaultDeck`）· `:803-812`（`UploadDefaultDeck`） |

**判读**：原版这一格**既有本地字段、也有服务器同步**（PlayFab CloudScript）。
按现行口径（`CLAUDE.md` §四 · 铁律 18·0），**服务器相关内容允许本地模拟** ⇒ 我们自定一个本地键落盘**这件事本身是合规的**，
不属于「原版本地该写而我们没写」。

---

## 四、该改什么（结论 ① 的配套动作）

### 4.1 建议改（压噪声，1 行，零语义变化）

**落点**：`CardPresentation/Shell/LiveOpsEventWindow.cs:2278`（`_eventDeckSlotLoaded = true;` 之后、`Get` 之前）

```csharp
// 照 SaveCenter.cs:1289 那条先例：这个键在「没有事件实例」时【本来就不会被建】，
// 直接 Get 会让每个进程首开活动窗都打一条「缺 key」⇒ 先 Exists 挡一道（不发声）。
if (!SaveCenter.Exists(EventDeckSlotKey)) return;
```

- 效果：`null` 语义**完全不变**（`Get` 原本也是返回 null），只是不再刷那条 Warn。
- ⚠️ 不要改成「`Exists` 为假就报错」——那就把「第一次跑」误判成「档丢了」。
- ⚠️ 也别直接把 `SaveCenter` 那条 Warn 降级：它是**全局四档出声**约定的一部分（`SaveCenter.cs:188-195`），
  降它会把别的 51 个键的「档丢了」一起变哑。**要在调用方挡，不在 SaveCenter 改。**

### 4.2 不建议在本条做

- ⛔ 不要为了让这条日志消失而**给 `EventId` 编一个值**（那会凭空造出原版此刻不存在的「活动实例」语义）。
- ⛔ 不要为了让日志消失而**先写一份空表**（`LiveOpsEventWindow.cs:2183-2184` 注释已经写明「⛔ 别把『空』写成一条记录」）。

### 4.3 账 / 活状态表该怎么写

- **`B117-R4` 可判 ①（不是缺陷）**，备注写：
  「写口 `LiveOpsEventWindow.cs:2296` 存在且接线正确（`:1897 → :2194`）；`EventId` 今日恒 null（类内文档 `:2042`、报告 `B90_N3.md:64`）⇒ 写口休眠 ⇒ 首次开窗必打 Warn（`SaveCenter.cs:191`，`SpeakOnce` 每进程一次）。建议按 `SaveCenter.cs:1289` 先 `Exists` 挡一道。」
- 若采纳 §4.1：改完把 `:2278` 作为落点写进账（**注意别插进已有成员之间**，该文件是 `partial` 且账按行号钉）。
- ⚠️ **本代理只读，未改活状态表**；由调度台按上面文字核销。

---

## 五、顺手发现（真缺口，建议单独立账，⛔ 本条不动手）

**同一个槽位在本仓有两套存储，其中一套是死码**：

| 事实 | 证据 |
|---|---|
| 原版形状的实现**已存在**：`PlayEventSaveGame.defaultDeckId` + `PlayEvent.GetEventDeck/SetDefaultDeck` | `Shell/PlayEvent.cs:185` · `:742` · `:788` |
| 但**调用点 = 0**（全仓只有定义 + 接口声明，`PlayEvent.cs:933`） | `grep -rn "SetDefaultDeck\|GetEventDeck"` 只命中定义与注释 |
| 而且**即使写进去也读不回来**：读盘一律按**基类**反序列化 | `Shell/LiveOpsHandler.cs:973` `LiveOpsSaveStore.Load(local.EventId, typeof(LiveOpsSave))`；`LiveOpsEvent.cs:975` `JsonUtility.FromJson(e.json, saveType)` ⇒ 具体子类字段（`defaultDeckId`）被丢掉 |
| 活着的却是自定键 | `LiveOpsEventWindow.cs:2263` 的 `_ours_EventDeckSlot`（本已标 `🔶 自己设计`，见 `:2255` 附近注释） |
| 活动存档层其实**已经有了**（且已接线） | `LiveOpsEvent.cs:953-956`（`LiveOpsSaveStore.Key = SaveKeys.LiveOps`）· `LiveOpsHandler.cs:910`（`Save`）· `:973`（`Load`）· `SaveKeys.cs:186`（= 原版 `PlayerDataKey` 第 41 项） |

**为什么算缺口**：`LiveOpsEventWindow.cs:2255` 的注释写着「原版那格在活动存档里，**我们没有活动存档**」——
**这句已与事实不符**（我们有 `LiveOpsSaveStore`，且 `PlayEventSaveGame` 就在里面）。属于 `CLAUDE.md` 铁律 5 那种「文档记错了」，
也是铁律 18·0「默认复刻原版」的偏离面：能用原版形状时不该另开一份自定键。

**若要修（建议方向，需单独派活 + 全量自检）**：
1. `LiveOpsHandler.cs:973` 改成按**具体存档类型**反序列化（`local` 上得有类型信息；否则 `defaultDeckId` 永远丢）；
2. `LiveOpsEventWindow` 的 `RememberEventDeck` / `EventDeckIndex` 改走 `PlayEvent.SetDefaultDeck` / `GetEventDeck`；
3. 迁移旧 `_ours_EventDeckSlot` 数据后再删键。
⚠️ 第 1 步是**跨面改动**（13 份事件的存档类型），别与本条的 1 行噪声修复混在一批。

---

## 六、边界（本条没覆盖到什么）

- **没跑 Unity / 没起 player**：以上全部是静态读代码 + 静态读原版反编译，**未在真机复验**「改完日志是否真不再响」。
- **没看真存档目录里是否有 `_ours_EventDeckSlot`**：搜过 `C:/Users/qjh36/AppData/LocalLow/**/WarpforgeSave`（存在该目录）下
  `*EventDeckSlot*` = **0 命中**，与 §2.1「写口生产路径不触发」自洽；但**没有打开别的 profile 目录逐个核**（`LocalLow/Everguild/…` 是原版存档，不是我们的）。
- **`PlayEvent` 实例在运行时到底有没有被建**：只查到 `FastModeBaseEvent` / `DraftEventBase` 两个抽象子类与自检夹具 `FakePlayEvent`；
  没深追 `LocalLiveOpsEvent` 与 `PlayEvent` 是否同一条继承链（这点只影响 §五 的修法成本，不影响本条结论）。
- **搜过的词**：`_ours_EventDeckSlot` · `OursEventDeckSlot` · `EventDeckSlot` · `defaultDeckId` · `DefaultDeckId` · `SetDefaultDeck` ·
  `GetEventDeck` · `UploadDefaultDeck` · `LocalLiveOpsEventId` · `LiveOpsSaveStore` · `缺 key`（含复数 / 大小写变体已一并试过）。
