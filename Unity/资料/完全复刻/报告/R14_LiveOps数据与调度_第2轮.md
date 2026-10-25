# R14 · LiveOps 数据与调度（第 2 轮）—— 报告

- 会话：2026-10-25 · 第八十会话 · 批次 2 · 执行代理 **R14**
- 输入：R12 报告 §六·1（`priority`/`showInMainMenu` 没搬）· §六·4（`LiveOpsScheduler` 无 Tick 接线）
- 独占白名单（**只动这三个**）：`Assets/CardPresentation/Shell/LiveOpsLocalCatalog.cs` ·
  `LiveOpsHandler.cs` · `LiveOpsEvent.cs`
- 没有碰：`账/*.tsv`（只读）· 正本 · git · Unity / 自检 · `D:/2/` · `ShellRuntime.cs`（别人的）
- 类型检查：`TMPDIR=/tmp/wf_r14 bash d:/4/Unity/工具/typecheck.sh` ⇒ **0 / 0**

---

## §一 结论（一句话）

> **两件「整条链路休眠」的缺口都接上了**：
> ① 13 份 SO 的 `priority` / `showInMainMenu` / `location` **逐份照 SO 真值搬进** `LiveOpsLocalEvent`，
>    并在 `LocalLiveOpsEvent.MapData` 映射进 `LiveOpsEventData` ⇒ `PriorityOf()` 不再恒 0，
>    R12 接好的 `OrderByDescending(priority)` **真生效**（此前是空转）；
> ② `LiveOpsScheduler` **有了生产 Tick 点**：新增 `LiveOpsPump`，把自己插进 Unity **自带**的
>    `PlayerLoop` 的 `Update` 阶段（`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` 安装），
>    每秒最多 `Tick` 一次。
> ③ 顺手把 `drawData` 里 **W2 真正要读的 4 个 bool 也抄了**（它们**有数据源**，不是「无数据源」）。
> **玩家可见变化 = 0**（见 §4·6：13 场活动全过期 + EVENT 导航钮已隐藏 + `location`/`showInMainMenu` 目前零消费者）。

三条要向上游说清的事：

1. **`Application.update` 在 Unity 6000.3 里不存在** —— 我第一版按它写，实测
   `error CS0117: Application 未包含 update 的定义`。⇒ 改用 **PlayerLoop 注入**（见 §二·3）。
   ⛔ 以后别再照网上教程写 `Application.update`。
2. **泵装在 `LiveOpsHandler.cs` 的文件尾（新类 `LiveOpsPump`），不装 `LiveOpsScheduler` 里面** ——
   这样 `LiveOpsHandler.cs` 账本落点（全部 < 590 行）**一行都不漂**。`LiveOpsScheduler` 只改了一行 doc。
3. **`LiveOpsLocalCatalog.cs` 的账本落点漂了**（我加了字段 + 枚举 + 表头），并且**新增 1 条「失败」行**
   （`EventDataAssetProvider.GetAssets()` 的 `:642 Events` 现在落在注释行上）⇒ 见 §4·6 的旧→新对照，**给 R1**。

---

## §二 判据（本轮用到的原版坐标，一处不落）

| 用在哪 | 判据 |
|---|---|
| `priority` | 13 份 SO JSON 的 `priority` 字段（例 `bundle_cosmeticsso_assets_all/MonoBehaviour/Expansion UM1 Parent Event.json`）。反编译侧 = R12 §二 #1：`<>c.<SetupEvents>b__18_0` 读 `LiveOpsEventData +0x40` |
| `showInMainMenu` | 同批 SO 的 `showInMainMenu`；反编译 = `LiveOpsEvent.cs:75`（`LiveOpsEvent.ShowInMainMenu => baseData.showInMainMenu`）、`:90`（`Priority`） |
| `location` | SO 的 `location.type` + `location.parent.targetId`。原版类型 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EventLocation.cs`（`public EventLocationType type; public LoadableReference<ParentEventData> parent;`）· `EventLocationType.cs`（`Main, None, Parent`）· `LoadableReference.cs`（`genericObjectReference` / `targetId` / `reference` 三格） |
| `drawData.*` | 同批 SO 的 `drawData.{ForceRun, ShowWindowOnTrigger, ShowWindowOnExpire, HideContainer, TriggerPopupOptions, ExpirePopupOptions, showPreviousPrice, previousPrice}` |
| **Tick 泵** | 原版没有「泵」这个概念 —— 它靠 `SingletonBehaviour<ActionScheduler>.Instance.Schedule(...)`（Unity 时间驱动，见 `LiveOpsHandler.cs` 文件头差异 ②）。我们的等价物 = 每帧推进 `LiveOpsScheduler.Tick(utc)` |
| `Application.update` 不存在 | 本轮**实测**：`typecheck` 报 `CS0117`（Unity 6000.3.23f1 的 `UnityEngine.Application` 无 `update` 成员）。可用的同类只有 `Application.onBeforeRender`（依赖渲染，未采用） |
| PlayerLoop 可行 | `UnityEngine.CoreModule.dll` 里 `GetCurrentPlayerLoop` / `SetPlayerLoop` 均在（本轮 `strings` 核过）；类型在 `UnityEngine.LowLevel`（`PlayerLoop` / `PlayerLoopSystem`），阶段 `UnityEngine.PlayerLoop.Update` |

---

## §三 改动清单（`文件:行号` = **改完之后的真实行号**）

### 3·1 `Assets/CardPresentation/Shell/LiveOpsLocalCatalog.cs`（1075 → **1135** 行）

| 位置（新行号） | 改动 | 内容 |
|---|---|---|
| `:13-20` | 文件头 | 新增第 ④ 类问题「`eventId → 排期/入口字段`」，写明这 8 格此前没抄的后果 |
| `:253-266` | 注释块 | 8 格的**判据清单**（SO 字段名逐条）+ `EventLocationType` 取值 + 「PPtr 那两格没搬」+ 两组 `*PopupOptions` 常量（13/13 相同，写下来省 W2 一轮） |
| `:265` | **新增字段** | `public int Priority;`（= SO `priority`） |
| `:267` | **新增字段** | `public bool ShowInMainMenu;`（= SO `showInMainMenu`） |
| `:269` | **新增字段** | `public int LocationType;`（= SO `location.type`） |
| `:272` | **新增字段** | `public string LocationParentId;`（= SO `location.parent.targetId`） |
| `:274` | **新增字段** | `public bool ForceRun;`（= SO `drawData.ForceRun`） |
| `:276` | **新增字段** | `public bool ShowWindowOnTrigger;` |
| `:278` | **新增字段** | `public bool ShowWindowOnExpire;` |
| `:280` | **新增字段** | `public bool HideContainer;` |
| `:286-294` | **新增枚举** | `LiveOpsLocationType { Main = 0, None = 1, Parent = 2 }`（**逐值照官方枚举**；只给读数用，本地目录里存 int） |
| `:1065-1096` | `E(...)` 助手 | 形参在 `expireMs` 之后插入 8 格（顺序 = 上表；声明在 `:1075`），函数体逐格赋值；注释写清「①~⑧ 各对应 SO 哪个字段」 |
| `:305 / :321 / :340 / :360 / :384 / :404 / :423 / :449 / :468 / :487 / :511 / :534 / :553` | **13 个调用点** | 每份 SO 的第二行追加 8 个实参（直接传给 `E(...)`，**没有额外的包装类**；值见 §4·3） |

### 3·2 `Assets/CardPresentation/Shell/LiveOpsHandler.cs`（789 → **925** 行；⚠️ R12 报告写 777，实测本轮开工时已是 789）

| 位置（新行号） | 改动 | 内容 |
|---|---|---|
| `:65-67` | doc | `LiveOpsScheduler` 的「本批还没有接线点」**已过期** ⇒ 改成「生产接线点 = `LiveOpsPump`」 |
| `:570-580` | doc | `LocalLiveOpsEvent` 的「priority/location/showInMainMenu 没抄过 ⇒ C1-W4 才补」**已过期** ⇒ 改成已抄 + 说明 `location.parent` 与 `EventElements` 的落点 |
| `:593`（体 `:596-614`，新增赋值在 `:603-605`） | **`MapData`** | `priority = local.Priority` · `showInMainMenu = local.ShowInMainMenu` · `location = local.LocationType` |
| `:789-925` | **新增类 `LiveOpsPump`** | 生产 Tick 泵（详见 3·3） |

### 3·3 `LiveOpsPump` 的逐条落点（`LiveOpsHandler.cs`）

| 行 | 成员 | 作用 |
|---|---|---|
| `:818` | `public static class LiveOpsPump` | 类声明（**追加在文件尾** ⇒ 本文件既有账本落点零漂移） |
| `:821` | `MinIntervalSeconds = 1f` | 节流间隔（**我们挑的**，见 §五·5） |
| `:827` | `Attached` | 诊断读口 |
| `:830-831` | `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)] Install()` | 进 Play / 启动时自动装 |
| `:835-847` | `Attach()` | `GetCurrentPlayerLoop()` → `ContainsPump` 幂等判 → `InsertInto(..., typeof(UnityEngine.PlayerLoop.Update))` → `SetPlayerLoop` |
| `:850-858` | `Detach()` | 反向摘除（自检要手动接管时间时用） |
| `:862` | `ManualTick()` | **给「工程内泵」留的一行口子**：将来 `ShellRuntime.Update()` 里加 `LiveOpsPump.ManualTick();` 即可改为工程内泵（⛔ 在别人白名单里，本轮没动） |
| `:864-871` | `OnUpdate()` | 隔离 ① `NowProvider != null ⇒ 直接 return`（自检钉死时间时泵让位）· 隔离 ② 1 秒节流 · `:870 LiveOpsScheduler.Tick(LiveOpsClock.NowUtc)` |
| `:874-884` / `:886-901` / `:903-919` | `ContainsPump` / `InsertInto` / `RemoveFrom` | PlayerLoop 树遍历（`PlayerLoopSystem` 是 struct，但挂在数组里 ⇒ 改数组元素就地生效） |

### 3·4 `Assets/CardPresentation/Shell/LiveOpsEvent.cs`（575 → **584** 行）

| 位置（新行号） | 改动 | 内容 |
|---|---|---|
| `:107-115` | doc | `drawData` / `mainMenuOptions` 那段：**`mainMenuOptions` 无数据源**；`drawData` 的 4 个 bool 已抄进 `LiveOpsLocalEvent`（逐条点名），`EventLabels/Assets/Components` 对应 `Labels/Assets/Components`；`EventElements` 类**仍未建** |
| `:128-132` | doc | `location` 字段：原版是 `EventLocation{type, parent}` 整体，我们**只存 `type`**；13 份取值已抄进 `LocationType`、`parent` 在 `LocationParentId`，⛔ 没搬进 `LiveOpsEventData` |

---

## §四 读数

### 4·1 类型检查（唯一允许的验证）

```
TMPDIR=/tmp/wf_r14 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

⚠️ 中途有 2 轮**别人在写**的瞬态错误（`CollectionWindow.cs(4969…5003)` 的 `_deckDragLive` / `BuildDeckCell`
等 5 条 = **R13 的白名单**），按 CLAUDE.md 13·3 **没去动**，隔一轮重跑即 0/0。

### 4·2 行尾（没翻）

```
LiveOpsLocalCatalog.cs  74,107 B  CRLF=0  LF=1135  bareCR=0
LiveOpsHandler.cs       59,542 B  CRLF=0  LF=925   bareCR=0
LiveOpsEvent.cs         36,170 B  CRLF=0  LF=584   bareCR=0
```

（三个文件本来就纯 LF；全程 Edit 工具 + 一次 `wb` 写入（只做 `PlayerLoopSystem` 全限定名替换），**未用 `sed -i`**。）

### 4·3 13 份 SO 的真值（**逐份现读**；判据 = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/<SoName>.json`）

| eventId | `priority` | `showInMainMenu` | `location.type` | `location.parent.targetId` | `drawData` Force / Trig / Expire / Hide |
|---|---|---|---|---|---|
| DarkZealots | 0 | 0 | 0 (Main) | — | 0 / **1** / 0 / 0 |
| ExpansionOrks1 | **3** | 0 | 0 (Main) | — | 0 / **1** / 0 / 0 |
| ExpansionOrks1_Premium_Item_Offer | 0 | 0 | 2 (Parent) | `ExpansionOrks1` | 0 / 0 / 0 / 0 |
| ExpansionOrks1Scoring | **5** | 0 | 2 (Parent) | `ExpansionOrks1` | 0 / 0 / **1** / 0 |
| ExpansionUM1 | **42** | **1** | 0 (Main) | — | 0 / **1** / 0 / 0 |
| ExpansionUM1_Premium_Item_Offer | 0 | 0 | 2 (Parent) | `ExpansionUM1` | 0 / 0 / 0 / 0 |
| ExpansionUM1Scoring | **5** | 0 | 2 (Parent) | `ExpansionUM1` | 0 / 0 / **1** / 0 |
| ECRelease | **3** | 0 | 0 (Main) | — | 0 / **1** / 0 / 0 |
| ECRelease_Premium_Item_Offer | 0 | 0 | 2 (Parent) | `ECRelease` | 0 / 0 / 0 / 0 |
| ECReleaseScoring | **5** | 0 | 2 (Parent) | `ECRelease` | 0 / 0 / **1** / 0 |
| SWRelease | **42** | **1** | 0 (Main) | — | 0 / **1** / 0 / 0 |
| SWRelease_Premium_Item_Offer | 0 | 0 | 2 (Parent) | `SWRelease` | 0 / 0 / 0 / 0 |
| SWReleaseScoring | **5** | 0 | 2 (Parent) | `SWRelease` | 0 / 0 / **1** / 0 |

- 取值集合：`priority ∈ {0,3,5,42}`（42 = UM1 & SWRelease 两个 Parent）· `showInMainMenu ∈ {0,1}`（只有那两份 Parent = 1）·
  `location.type ∈ {0,2}`（Parent 事件 = 0 Main；Scoring / Premium = 2 Parent）· `drawData` 规律 = **Parent ⇒ Trig=1；Scoring ⇒ Expire=1；其余全 0**。
- 与 R12 §4·5 的 `priority` / `showInMainMenu` 两列**逐格一致**（交叉核对过）。
- **这 8 格的 13×8 = 104 个值，全部由两个独立脚本核过**：
  ① 解析 `LiveOpsLocalCatalog.cs` 的 `E(...)` 实参 ↔ 手工期望表 ⇒ **13/13 一致**；
  ② 同一批实参 ↔ **直接读 13 份 SO JSON** ⇒ **13/13 一致**（脚本 `r14_cross.py`，见 §4·4）。

### 4·4 「哪一行代码读它」的落点（数据 → 消费者，一条链）

| 数据 | 落盘处 | 谁读 |
|---|---|---|
| `priority` | `LiveOpsLocalCatalog.cs:265`（字段）← `:305…:553`（13 个 `E(...)` 实参） | `LiveOpsHandler.cs:603`（`MapData` 写进 `LiveOpsEventData.priority`）→ `:562 PriorityOf()` → `:202 OrderByDescending(PriorityOf)` |
| `showInMainMenu` | `:267` ← 13 个 `E(...)` | `LiveOpsHandler.cs:604` → `LiveOpsEvent.cs:257 LiveOpsEvent.ShowInMainMenu`（⚠️ **目前零消费者**，见 §六·1） |
| `location.type` | `:269` ← 13 个 `E(...)` | `LiveOpsHandler.cs:605` → `LiveOpsEventData.location`（⚠️ **目前零消费者**） |
| `location.parent.targetId` | `:272` ← 13 个 `E(...)` | **暂无读者**（原版 `EventLocation.Match` 才对它用；我们没有那个方法，见 §五·2） |
| `drawData` 4 个 bool | `:274/:276/:278/:280` ← 13 个 `E(...)` | **暂无读者**（C1-W2 的 `QueueEventPopup` 会读，见 §六·2） |

### 4·5 Tick 接线 —— 静态可查判据

```bash
# 生产 Tick 调用点：改前 = 0 条（R12 §六·4 已核），改后 = 1 条
grep -rn "LiveOpsScheduler\.Tick" d:/4/Unity/MyGame/Assets --include=*.cs
#   → CardPresentation/Shell/LiveOpsHandler.cs:870:            LiveOpsScheduler.Tick(LiveOpsClock.NowUtc);
#     （另有 1 条命中在 :811，是 doc 里的 `LiveOpsPump.ManualTick();` 说明，不是调用点）
```

- **安装点**（`RuntimeInitializeOnLoadMethod`）只有 1 处：`LiveOpsHandler.cs:830-831`。
- **幂等**：`Attach()` 先 `ContainsPump` 再插；`Detach()` 反向摘除。域重载关掉时不会越插越多。
- **隔离**：`LiveOpsClock.NowProvider != null`（自检钉死时间）⇒ 泵整帧 return，**不会抢在自检的显式 `Tick` 前面触发**。
- ⚠️ **没法用运行结果证明**（本轮不许跑 Unity / 不许自检）：实际是否插进 PlayerLoop 只能等真机/Play 验收
  （验收法：Play 里读 `LiveOpsPump.Attached == true`，或断点看 `PlayerLoop.GetCurrentPlayerLoop()` 里有没有 `typeof(LiveOpsPump)`）。

### 4·6 可达性（如实写）

| 面 | 现状 | 本批玩家可见变化 |
|---|---|---|
| 13 份活动 SO | **全部过期** + `EVENT` 导航钮已隐藏 | **0** |
| `priority` 排序 | 现在**真生效**（值 42/5/3/0 齐全） | **0**（`events` 的内部顺序变了：UM1 · SWRelease · 4×Scoring · 2×Release · 5×Offer/DarkZealots —— 但列表没有可见 UI） |
| `showInMainMenu` | 已搬 + `LiveOpsEvent.ShowInMainMenu` 可读 | **0**（该属性**零消费者**；`GetEventPriorityForMainMenu` 还没复刻） |
| `location` | 已搬 `type` + `parentId` | **0**（零消费者） |
| `drawData` 4 bool | 已搬 | **0**（C1-W2 才用） |
| 到点刷新 | 泵已接（Play 模式每秒 Tick 一次） | **0**（`hasDuration = 0` ⇒ 13/13 不排期，原版同义；`shared.NextRefresh` 恒空） |

⇒ **别为了「看得见」去改 EVENT 导航钮**（简报红线）。本批是**数据补齐 + 接线**，不是可见功能。

### 4·7 账本行号漂移（**给 R1，我只读账没改账**）

`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv`
改前 `通过 257 / 漂移 26 / 失败 2` ⇒ 改后 **`通过 253 / 漂移 29 / 失败 3`**（新增的这部分全是我这三个文件）。

**① `LiveOpsHandler.cs` —— 零新增漂移。** 我的两处代码改动（`MapData` 在 593、`LiveOpsPump` 在文件尾 818+）
**都在 R12 那张漂移表的最后一处（`ValidateData` 553）之后** ⇒ R12 报告 §七 给的那张「旧→新」表**原样有效**。

**② `LiveOpsEvent.cs` —— R12 表里的每一行 +9**（我在 `:107-115` / `:128-133` 各加了几行注释）：

| 方法 | R12 表 | **现在** |
|---|---|---|
| `GetBaseSave` | 312 | **321** |
| `GetEndDate` | 317 | **326** |
| `Initialize` | 337 | **346** |
| `LoadEventData` | 360 | **369** |
| `ValidateEventData` | 391 | **400** |
| `CreateNewSave` | 422 | **431** |
| `UpdateData` | 443 | **452** |
| `UpdateSaveGame` | 461 | **470** |
| `ResetEventProgress` | 475 | **484** |

**③ `LiveOpsLocalCatalog.cs` —— 全部落点都要动**，位移是**分段的**（因为我在不同位置加了东西）：

- **行号 < 13**：不变。
- **13 ≤ 行号 < 250**：**+4**（文件头加了 4 行）。例：`Term` 109→**113** · `Components` 177→**181** · `EndUtc` 180→**184** · `ExpireUtc` 181→**185** · `IsInPeriod` 188→**192** · `IsExpired` 194→**198** · `TryGetLabel` 196→**200** · `FindAsset` 231→**235**。
- **250 ≤ 行号 ≤ ~1020**：**+46**。例：`Events` 642→**688** · `Find` 645→**691** · `TrySelectActive` 659/663→**705/709** · `TryPickEventIn` 681→**727** · `FillArgs` 979→**1025**。
- **行号 > 1020**：**+52**（`E(...)` 助手自己也长了 6 行）。例：`public string Term;`（`LiveOpsLocalSurfaceValue`）1069→**1129**。

🔴 **新增 1 条「失败」行**：`EventDataAssetProvider.GetAssets()` 的落点写的是 `Shell/LiveOpsLocalCatalog.cs:642 Events`，
而 **642 现在是注释行** ⇒ 机检报「注释行」。**跑 `--fix` 会把它改到 688**（或手工改）。

---

## §五 没查清（写清「还差什么」）

1. **`location.parent` 的 Unity 引用两格没搬**：SO 里是 `genericObjectReference{m_FileID,m_PathID}` + `reference{m_FileID,m_PathID}`。
   我们离线目录里**没有 ParentEventData 资产**，所以只抄了 `targetId` 字符串。
   ⇒ **还差**：如果 C1-W2 要按 `EventLocation.Match(type,id)` 判「这个子活动挂在哪」，用 `LocationType + LocationParentId` 就够；
   若要**真解析资产**，得先把 `cosmeticsso` 里 ParentEventData 的 PathID 映射建出来（没有判据说需要）。
2. **`location` 的真实消费点没查**：本轮只确认了我们自己**零消费者**；原版谁读 `EventLocation` 只查到
   `Shops.cs:314/332`（`EventLocationType` + `EventLocation.Match`）与 `LiveOpsEventData.location` 字段本身，
   **没有把 `Shops` 那条链读完**。⇒ 还差一次 `grep "\.location"` 在原版反编译里的完整清点。
3. **`EventElements` 类本体仍未建**：本轮只把 4 个 bool 落了地。`TriggerPopupOptions` / `ExpirePopupOptions`
   （= `{PopupLabel 20/21, ActionRule 1, TriggerBeforeOnboardingEnds 0, MarkOnScreenClose 1}`，13/13 相同）
   **只写进了注释，没落成字段** —— 等 W2 建 `EventElements` 等价类时一起做（⛔ 别在 `LiveOpsLocalEvent` 上再堆两个结构体）。
4. **PlayerLoop 注入没有运行期证据**（本轮不许跑 Unity）。静态只能证明「代码路径存在 + 编译通过」。
   真机验收法见 §4·5 末。
5. **1 秒节流是「我们挑的」，不是原版参数**：原版 `ActionScheduler` 的真实 tick 频率（每帧？按时间片？）
   **没查**（要读 `ActionScheduler` 的反编译）。选 1 秒的理由：活动刷新是小时/天尺度，秒级足够。
6. **`LiveOpsManager.ResetForTest()` 只清 `_pending`，不停泵**：将来若在 **Play 模式**下跑自检，
   泵仍在跑（有 `NowProvider` 隔离兜底，但 `ResetForTest` **不会调 `LiveOpsPump.Detach()`** —— 因为
   `LiveOpsManager.cs` 不在本轮白名单）。⇒ 还差：谁拥有「自检收尾」那一行，要定个归属。

---

## §六 顺手发现（不修，交调度台分流）

1. 🔴 **`LiveOpsEvent.ShowInMainMenu` / `LiveOpsEventData.location` 现在都还是「零消费者」** ——
   数据搬进来了，但**读的人还没复刻**：`GetEventPriorityForMainMenu`（账 row 332 附近）还差这一格。
   ⇒ 本批只是把**输入**补齐了；要看得见，得等主菜单活动容器那条线。
2. 🔴 **`drawData` 的 4 个 bool 现在有数据、没读者** —— C1-W2 建 `QueueEventPopup` 时**直接读
   `LiveOpsLocalEvent`**（`ForceRun` / `ShowWindowOnTrigger` / `ShowWindowOnExpire` / `HideContainer`），
   ⛔ 别再新建第二份表（铁律 6）。
3. 🟡 **`Editor/SkirmishWindowAssertE15.cs:115` 用对象初始化器造 `LiveOpsLocalEvent`** ——
   新加的 8 个字段在那里**全是默认值（0/false/null）**，不是真值。目前那个夹具只测 Skirmish 窗口，
   **不受影响**；但**下一个人别把它当「13 份 SO 的样本」**。
4. 🟡 **`LiveOpsLocalCatalog.cs:642` 的账本落点现在落在注释上**（§4·7·③），机检已报「失败」——
   属于**行号过期**，不是内容错（铁律：「表格坏了 ≠ 内容丢了」）。
5. 🟢 **判据确认（不是缺陷）**：`priority` 的 13 份取值 **没有一份是负数**，`OrderByDescending` 不涉及负数排序；
   `LiveOpsLocalEvent` 新字段名与 `LiveOpsEventData` **同名**（`Priority` / `ShowInMainMenu` / `LocationType`），
   映射一一对应、没有换算（⛔ 别在 `MapData` 里再加减什么）。
6. 🟡 **`Application.update` 这条坑值得写进知识库**：Unity 6000.3 没有它；同类教程满天飞。
   已知可用替代：`Application.onBeforeRender`（依赖渲染）/ `PlayerLoop` 注入（本批采用）/ MonoBehaviour `Update`。

---

## §七 交接给下一个代理

**做完了什么**

- ① `priority` / `showInMainMenu` / `location(type+parentId)` / `drawData` 4 个 bool —— **13 份 × 8 格全部照 SO 搬齐**
  （双重校验：手写期望表 13/13 · 直读 SO JSON 13/13），并在 `MapData` 映射。`PriorityOf` 不再恒 0。
- ② `LiveOpsScheduler` 有生产 Tick 点：`LiveOpsPump`（PlayerLoop 注入，`RuntimeInitializeOnLoadMethod` 安装），
  1 秒节流 + 两条隔离。静态判据：`grep -rn "LiveOpsScheduler\.Tick" Assets/` = **1**（`LiveOpsHandler.cs:870`）。
- ③ `drawData` 如实处置：**有数据源**（不是「无数据源」），4 个 bool 已抄；`EventElements` 类仍缺，已在
  `LiveOpsEvent.cs:107-115` 写清「数据在哪、去哪儿读」。
- 类型检查 **0/0**；三文件纯 LF、无裸 CR。

**文件动到哪**

- 改：`Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsLocalCatalog.cs`（1075 → **1135** 行）
- 改：`Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsHandler.cs`（789 → **925** 行）
- 改：`Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEvent.cs`（575 → **584** 行）
- 新增：本报告。

**下一步（按顺序）**

1. **R1**：按 §4·7 更新 `账/日常_奖励.tsv`（`LiveOpsLocalCatalog.cs` 四段位移 + `LiveOpsEvent.cs` 全部 +9；
   `LiveOpsHandler.cs` 沿用 R12 那张表）。`--fix` 能自动改 A 档，**但那 3 条「歧义」（`GetBaseSave` / `UpdateSaveGame` /
   `ResetEventProgress` 同名 2 处）仍需人工指到「实现」那一行**（321 / 470 / 484）。
2. **调度台**：把 §六·1（`showInMainMenu`/`location` 零消费者）、§六·2（`drawData` 有数据没读者）、
   §五·6（`ResetForTest` 不停泵）三件事落账/派活。
3. **C1-W2 开工时**：① 建 `EventElements` 等价类（4 个 bool 从 `LiveOpsLocalEvent` 取，别新建表）；
   ② 若要按「活动挂在哪」筛，用 `LocationType + LocationParentId`；③ 弹窗排期**不要**用 `ScheduleAction`（会推队尾）。
4. **真机/Play 验收**（属铁律 12 的验收，不是本批）：`LiveOpsPump.Attached == true`；
   `PlayerLoop.GetCurrentPlayerLoop()` 里能找到 `typeof(LiveOpsPump)`。

**踩过的坑（省下一轮）**

- 🔴 **`Application.update` 在 Unity 6000.3 不存在**（CS0117）。要么 `Application.onBeforeRender`（依赖渲染），
  要么 PlayerLoop 注入。⛔ 别照网上教程写。
- 🔴 **`PlayerLoop` / `PlayerLoopSystem` 在 `UnityEngine.LowLevel`，阶段类型在 `UnityEngine.PlayerLoop`** ——
  两套命名空间，混了就是 `CS0246`（本批实际踩了一次）。
- 🔴 **`PlayerLoopSystem` 是 struct，但它挂在数组里 ⇒ 直接改 `subs[i].subSystemList` 就地生效**，
  不需要把整棵树拷回（`GetCurrentPlayerLoop()` 返回的是根 struct 的副本，可子数组是同一个引用）。
- 🟡 **往 `LiveOpsLocalCatalog.cs` 加字段就一定会让账本落点漂**（37 条 B/C 行 + 6 条 A 行）——
  本次按 CLAUDE.md「新成员追加到类尾」放在 `LiveOpsLocalEvent` 类尾，把 **< 250 行** 的落点位移压到只 +4。
  下一个人再动这个文件，**记得同批报漂移表**。
- 🟡 **账本检查只在 `--fix` 之后才可信**：本轮新增的那 1 条「失败」纯粹是行号过期所致（落在注释行上）。
