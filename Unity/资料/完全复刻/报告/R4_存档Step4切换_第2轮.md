# R4 · 存档 Step 4「双写期」—— 真子系统切换（第 2 轮）

> **代理**：R4（执行）· 2026-10-25 · 第八十会话 · 完全复刻批次 1
> **白名单**：`Shell/{Inventory,BattleLogData,DailyData,RankedData,ShellRuntime,SaveCenter,SaveKeys,LegacySaveReader}.cs`
> **红线**：⛔ 没跑 Unity · ⛔ 没跑任何 Unity 自检宿主 · ⛔ 没动 git · ⛔ 没改正本 / `账/*.tsv` / 判据文档 · ⛔ 没删任何旧档（新增 `File.Delete` 0 处）
> **断言全部在工程外**（`D:/tmp/wf_n6_verify/harness`，用户 2026-10-25 口径：复刻完成前暂停 Unity 自检）
> 判据 = `资料/完全复刻/判据/存档体系_原版语义与施工方案.md`（下称「方案」）§七 Step 4 + `报告/E3_存档Step3-4_第2轮.md` §七·3 的 C1–C11

---

## 一、结论

| 项 | 结果 |
|---|---|
| **真子系统切换** | ✅ **5 个**：`InventoryStore`（**含读口**）· `BattleLogData` · `DailyData` · `RankedData` + **`ShellRuntime.Awake` 接线**（`ImportAll()` 从此真的会跑） |
| **验收判据** | 每个子系统 **两条 ★ 断言全绿**：「切后中央档 vs 切前 dump 逐字段相等」+「切后旧档 vs 切前 dump 逐字段相等」 |
| **补的两个洞** | ① **`Delete` 只删旧档** ⇒ 中央档还在 ⇒「删档后从空态起手」会变成假空（三个子系统都补了）② **`Shop`/`shop` 大小写折叠撞同一份档**（E3 §五·4 记的洞，本轮补上自检 + 两条运行时检查） |
| **Step 5** | ⛔ **本轮仍不做**（设计上「关开关即停」，已用断言证明 `LegacyMirrorOn=false` ⇒ 旧档不再更新、中央档照写） |
| **脚手架** | `PASS 236 · FAIL 0`（E3 交回时 168/0，本轮 **+68**；E3 那 168 条**一条没退**）· **4 次变异测试全部真的变红**，每次还原到同一 sha256 |
| **typecheck** | ✅ **`0/0`（真 0/0）** |

**三条最要紧的**

1. 🔴 **切换口必须有一条「退化分支」**：`SaveCenter.StoreOn` 与各子系统自己的 `StoreOn` **是两个独立的门**。既有自检的常态是**只注入子系统自己的 `OverrideDir`**（`SaveCenter.OverrideRoot` 从没人设过）⇒ 如果切换后无条件走 `SetRaw`，那一批自检会**突然一条都不落盘**，而且**全绿**（没人写盘就没人报错）。所以切换口在「中央档门关着」时**只写旧档**，与切换前逐字节同（§四·3 的 (a) 就是拿变异测试钉住这一支的）。
2. 🔴 **`Delete` 的三处都漏了中央档**（我自己查出来的，不是简报里的）：读口改成「先中央后旧档」之后，`DeleteStoreForTest` 只删旧档 ⇒ `ReloadForTest` 会从中央档把旧值读回来 ⇒ 「自检从空态起手」变成假空。三处已补 `SaveCenter.Delete`（**不真删**，改名 `.del-*`）。
3. 🔴 **`DeckStore.cs` 不在我的白名单里** —— 它在 `Assets/RuleEngine/Data/`（R2 的地盘），简报里写的 `Shell/DeckStore.cs` **全仓不存在**。⇒ C5 **没改文件**，只做了**预演验证**（用玩家真卡组档跑同一条判据：**零差异**，§四·2）。补丁形状见 §七。

---

## 二、判据（每条能指到出处）

| 事实 | 出处 |
|---|---|
| Step 4 = **双写期**：「每切一个：同时写旧文件 + 新 key 跑一轮自检，再删旧写入」 | 方案 §七 Step 4 |
| 验收判据 = **切前 dump 与切后中央档逐字段相等** | 方案 §七 Step 4（工具 = `SaveCenter.DiffPayload` / `SaveJsonDiff`） |
| 切法五步（dump→登记→改写入点→改读取点→验收） | `报告/E3_存档Step3-4_第2轮.md` §七·2 |
| C1 / C1b / C2 / C4 / C6 / C10 的**行号与改法** | 同上 §七·3（本轮的作业清单） |
| 🔴 `profile.json`（`CosmeticData`）**绝不能双写**：中央 2 字段 vs 旧档 6 字段 | `LegacySaveReader.cs:340-346`（E3 已硬跳过，`CosmeticOnly` 判定在 `:346`）· 本轮加了断言钉住 |
| `LiveOps` 归 LiveOps 域（E2）⇒ 双写登记表不登记它 | 调度台日志「本批裁定 1」（2026-10-25） |
| 落盘门（批处理不碰玩家真档）| `Shell/DailyData.cs:1793`（`StoreOn`）· `Shell/SaveCenter.cs:90` |
| Windows 文件名大小写不敏感 ⇒ `<key>.json` 会撞 | `SaveCenter.PathOf` 只做 `Path.Combine(root, key + ".json")`，**不转义** |
| 原子写先例 | `SaveCenter.AtomicWrite:685`（tmp → `File.Replace`，失败退回两步法并出声） |
| 切换前的五个写入点 | `Inventory.cs:429` · `BattleLogData.cs:296` · `DailyData.cs:2011-2022` · `RankedData.cs:194-205` · `ShellRuntime.cs:309` |
| 玩家真存档目录（只读过，一个字节没写） | `C:/Users/qjh36/AppData/LocalLow/QJHWarpforge/WarpforgeReplica`（§四·4 有 sha 对照） |

---

## 三、改动清单（`文件:行号`，全部为**当前**行号）

### 3.1 `Shell/SaveCenter.cs`（1355 → **1458** 行，+103 / −0；**新增成员全在类尾**）

| 行 | 是什么 |
|---|---|
| `:1072-1080` | 新增一节「Step 4 —— 子系统切换口」的**用法说明**（写/读各一行，⛔ 别在子系统里再写一次盘） |
| `:1082-1154` | **新增** `SaveThroughLegacy(key, legacyPath, payload, out error)` —— 切换口（写）。三支：① `StoreOn` 开 ⇒ 登记双写 + `SetRaw`（中央档带 `_v`、旧档不带）② `StoreOn` 关 ⇒ **只写旧档**（原子写，与切换前逐字节同）③ 双写失败 ⇒ 返回 false（不许静默半成功） |
| `:1098-1104` | 形状闸：payload 必须是 JSON 对象 —— **两条分支收一样的形状**（否则「关着」那一支会把垃圾写进旧档，而「开着」那一支会拒） |
| `:1116-1138` | 🔴 **登记路径以「调用方这次实际用的路径」为准**：登记表里若是另一条路径（`RegisterMirrors` 先按玩家真目录登记过、自检又注入了临时目录）⇒ 撤旧登新，**否则双写会写到玩家真档上** |
| `:1156-1173` | **新增** `ReadThroughLegacy(key, legacyPath)` —— 切换口（读）。先中央档（`Exists` → `GetRaw`），不在 / 坏档 / 未来版本 ⇒ 退回读旧档原文；`StoreOn` 关时**根本不看中央档**。⛔ 不自动倒灌 |

### 3.2 `Shell/SaveKeys.cs`（363 → **403** 行，+40 / −0）

| 行 | 是什么 |
|---|---|
| `:347-348` | `ValidateTable()` 末尾加一句 `ValidateCaseFolding()`（键表自检现在覆盖折叠撞车） |
| `:369-401` | **新增** `ValidateCaseFolding()`（`:375`）/ `ValidateCaseFolding(string[] keys)`（`:383`）—— 「**大小写折叠后不许重复**」。理由写在节头（`:365-372`）：Windows/macOS 文件名不敏感 ⇒ `Shop` 与 `shop` 落到**同一份 `<key>.json`**，后写的静默盖掉先写的 |

### 3.3 `Shell/LegacySaveReader.cs`（421 → **452** 行，+31）

| 行 | 是什么 |
|---|---|
| `:224-230` | `ImportInventoryDir`：枚举 `WarpforgeInventory/*.json` 之后先查折叠撞车，撞了就 `Fail()`（进报告的「失败」列） |
| `:354-359` | `RegisterMirrors`：同一处检查，撞了就 `LogError` |
| `:412-431` | **新增** `InventoryCaseFoldProblem(files)`（建出 `_ours_Inventory.<name>` 那一组 key 后转调 `SaveKeys.ValidateCaseFolding`） |

### 3.4 `Shell/Inventory.cs`（544 → **566** 行，+26 / −4）

| 行 | 是什么 | 切换单 |
|---|---|---|
| `:428-446` | `Save`：`File.WriteAllText` → `SaveCenter.SaveThroughLegacy(SaveKeys.OursInventoryPrefix + name, p, json, out werr)`（调用在 `:438`）；失败走原来的「`LastStoreError` + `LogWarning`」 | **C1** |
| `:461-470` | `Load`：`File.Exists` → `SaveCenter.ReadThroughLegacy(...)`（调用在 `:466`，⛔ 没删旧读） | **C1b** |
| `:492-506` | `Delete`：**顺手清中央档**（`SaveCenter.Delete`，调用在 `:504`，不真删）—— 否则删了旧档还会从中央档读回旧值 | 本轮自查 |

### 3.5 `Shell/BattleLogData.cs`（409 → **425** 行，+20 / −4）

| 行 | 是什么 | 切换单 |
|---|---|---|
| `:296-310` | `SaveNow`：→ `SaveThroughLegacy(SaveKeys.BattleLogData, StorePath, json, out werr)`（调用在 `:305`；仍是 `ToJson(dto, true)` 带缩进） | **C2** |
| `:246-252` | `LoadFromDisk`：→ `ReadThroughLegacy`（调用在 `:250`；`null` = 第一次跑，空表就是出厂态） | **C2** |
| `:336-339` | `DeleteStoreForTest`：顺手清中央档（调用在 `:339`） | 本轮自查 |

### 3.6 `Shell/DailyData.cs`（2301 → **2316** 行，+22 / −7）

| 行 | 是什么 | 切换单 |
|---|---|---|
| `:2015-2045` | `Persist`：`tmp→Delete→Move` 换成 `SaveThroughLegacy(SaveKeys.DailyCounters, path, json, out werr)`（调用在 `:2029`）；🔴 **原子写这条性质保住了**（`SaveCenter.AtomicWrite:685` 用的是 `tmp → File.Replace`，比原来的两步法**更强**）；失败 `LogError` + `return` | **C4** |
| `:1888-1896` | `EnsureLoaded`：→ `ReadThroughLegacy`（调用在 `:1892`） | **C4** |
| `:2121-2123` | `DeleteStoreForTest`：顺手清中央档（调用在 `:2123`） | 本轮自查 |

### 3.7 `Shell/RankedData.cs`（578 → **591** 行，+17 / −4）

| 行 | 是什么 | 切换单 |
|---|---|---|
| `:197-225` | `SaveNow`：→ `SaveThroughLegacy(SaveKeys.PlayerSeason, StorePath, json, out werr)`（调用在 `:214`） | **C6** |
| `:177-181` | `Load`：→ `ReadThroughLegacy`（调用在 `:180`） | **C6** |

> ⚠️ `RankedData` **本来就没有 `StoreOn` 那道门**（切换前就是无条件写）—— 本轮**没给它加**（会把批处理行为一起改掉），见 §五·3。

### 3.8 `Shell/ShellRuntime.cs`（1150 → **1163** 行，+13 / −0）

| 行 | 是什么 | 切换单 |
|---|---|---|
| `:315-326` | `Awake()` 末尾：`if (SaveCenter.StoreOn) { LegacySaveReader.ImportAll(); LegacySaveReader.RegisterMirrors(); }` —— **`ImportAll()` 全仓唯一的调用点**（不接这句，Step 2 那整套导入就是死代码）。顺序固定「**先导入、后登记**」 | **C10** |

### 3.9 没动的（明确边界）

* **`Assets/RuleEngine/Data/DeckStore.cs`（C5）** —— **不在白名单**（R2 的地盘；简报写的 `Shell/DeckStore.cs` 全仓不存在）。只做了预演验证。
* `Shell/DraftModeWindow.cs`（C3 · E6）· `Shell/CardDetailPopup.cs`（C7 · E6）· `Shell/ProfileData.cs`（C8）· `Shell/MenuTutorialFlow.cs`（C9 · R5）。
* 玩家真存档目录：**只读过**（§四·4 有 sha 对照，五个文件逐个没变）。

---

## 四、断言 / 读数

### 4.1 脚手架（唯一能跑的东西）

```
cd D:/tmp/wf_n6_verify/harness && dotnet run -c Release
=== 结果：PASS 236 · FAIL 0 ===
```
E3 交回时 **168 / 0** ⇒ 本轮 **+68 条**，**E3 那 168 条一条没退**。

新增分组：

| 组 | 条数 | 覆盖 |
|---|---:|---|
| **5 个子系统 × 9 条** | 45 | 每个子系统：★中央档 vs 切前 dump · ★旧档 vs 切前 dump · 顶层恰好一个 `_v` · 旧档不带 `_v` · `.tmp` 不残留 · `ReadThroughLegacy == 中央档` · `DiffWithLegacy == ""` · 与切前 dump 零差异（Daily 是「只有点名的 3 个新增字段」）· 写成功 |
| 双写登记表 | 4 | **登记 6 条**（profile 那条被硬跳过）· 🔴 `CosmeticData` **没登记** · 不在 `MirrorKeys()` · `DailyCounters` 的路径 = `LegacyRoot/WarpforgeDaily/daily.json` |
| 切换口分支 | 9 | (a) 落盘关着 ⇒ 只写旧档、**中央档一个字节都不建** ② Step 5 关开关 ⇒ 中央档照写、旧档不再更新 (c) 非 JSON 对象两分支都拒 (d) 中央档坏 ⇒ 退回旧档 + 隔离 `.bak` (e) 删档两半都清 ⇒ 读口真的返回 `null` |
| 键表折叠 | 4 | 键表本身无撞车 · `ValidateTable()` 仍通过 · ★`Shop`/`shop` 被抓出来 · 全小写不误报 |

### 4.2 ★ 逐子系统的 dump 对比结果（判据本体）

fixtures = **玩家真存档的副本**（只读复制到 `D:/tmp/wf_n6_verify/fixtures/`）：

| 文件 | sha256 |
|---|---|
| `daily.json` | `07ae2b2be9db2622eb983b8e88f14ce4efcfdc18a619662bc42dcb7b4a47234a` |
| `battlelog.json` | `b5a7100e61e73026fcc245fce6a1d75c067ed9f2ee243f5e83e43e718025bbf7` |
| `WarpforgeDecks.json` | `9491d62e699691255aed9e19f73afb2a03b073395da50bd4f2b740ee89900285` |
| `run.json` | `b6fb081d7efb72a113a05e4397de8471169b861fd369cd71ba2454223067d6db` |
| `profile.json` | `a6331c8d5be107c8ba9600319a5bb4bd53f591e08aacb49438bc7e7ab00039aa` |

| 子系统 | key | 切前 dump 来源 | ★中央档 vs 切前 dump | ★旧档 vs 切前 dump | 与历史旧档的差异 |
|---|---|---|---|---|---|
| `InventoryStore(shop)` | `_ours_Inventory.shop` | 合成（玩家目录里**没有** `WarpforgeInventory/`）| ✅ 相等 | ✅ 相等 | ✅ **零差异** |
| `BattleLogData` | `BattleLogData` | **真档**（7 条记录，4,751 B）| ✅ 相等 | ✅ 相等 | ✅ **零差异** |
| `DailyData` | `DailyCounters` | **真档**（3 行）| ✅ 相等 | ✅ 相等 | ✅ **只有预期 3 个新增字段**：`weeklySet` / `weeklyProgress`(=13，出厂默认) / `weeklyState` —— schema 是 2026-10-25 `N17` 才长的，**不是切换改坏的** |
| `DeckStore`（**预演·未改文件**） | `PlayerDecks` | **真档**（2 副卡组，1,550 B）| ✅ 相等 | ✅ 相等 | ✅ **零差异** |
| `RankedData` | `PlayerSeason` | 合成（玩家目录里**没有** `WarpforgeRanked/`）| ✅ 相等 | ✅ 相等 | ✅ **零差异** |

> **「切前 dump」在这张表里是什么意思**（⛔ 别读错）：
> * 有真档的四行 = **玩家盘上那一份原文**；
> * 「新 payload」= **切换后代码会写出的那一份**（同一份 `JsonUtility.ToJson(dto)`，DTO 逐字段照真类）；
> * 所以「★ 中央档 vs 切前 dump」比的是 **新 payload vs 中央档**（= 切换口有没有把内容改形），
>   而「与历史旧档的差异」比的是 **真档原文 vs 新 payload**（= 切换**有没有改变写出来的内容**）。
>   后者对 Daily 有 3 个字段、其余四行**零差异** —— 这说明**切换本身一个字段都没动**。

### 4.3 🔴 变异测试：负例真的会红（不是「我说会红」）

脚本 `D:/tmp/wf_n6_verify/mutate_run.py`（工程外；备份在 `%TEMP%`，跑完立刻还原并打印 sha256）。**本轮新加 4 条变异，4 条全红，4 次都还原到同一 sha。**

| 变异 | 「改坏法」 | 结果 | 变红的关键断言 |
|---|---|---|---|
| **M7** | `SaveThroughLegacy` 的「落盘关着」那一支改成 `return false`（不再写旧档）| **FAIL 2** | `(a) 落盘关着 ⇒ SaveThroughLegacy 仍成功` · `(a) 旧档被更新了` |
| **M8** | `MirrorToLegacy` 把信封一起写进旧档（`AtomicWrite(path, WrapEnvelope(...))`）| **FAIL 16** | 5 个子系统的「旧档**不带 `_v`**」/「★旧档 vs 切前 dump」/「`DiffWithLegacy == ""`」+ `(d)` 退回旧档那条（读到的带 `_v`） |
| **M9** | `RegisterMirrors` 去掉 `if (e.CosmeticOnly) continue;` | **FAIL 3** | `登记 6 条`（实际 7）· ★`CosmeticData` **没有**被登记 · 不在双写表里 |
| **M10** | `SaveKeys.ValidateCaseFolding(string[])` 永远 `return ""` | **FAIL 1** | ★`Shop` / `shop` 被抓出来 |

> 📌 **M8 值得单独看**：它是「一个改动打穿全部五个子系统」的那一类 —— 说明那 45 条不是同一个断言的五份拷贝，
> 而是**每个子系统各自一条真判据**（改坏一处，每一格都点名报出来）。

基线 sha256（跑完核对过，与变异前完全一致）：
`SaveCenter.cs` = `0336453f8eb9b2cb1ed46c2fd6c30d3c9f50c18650e344cf447613ae54f51174` ·
`SaveKeys.cs` = `b1a6b61d902a5870d4db300762928f2123ee42b948ebad081848efe59636c0ae` ·
`LegacySaveReader.cs` = `98d37c5833782d9c1433a5fb917a7a990655ae9cd44546f5373a8b22227891ff`

### 4.4 玩家真存档：**一个字节都没动**（跑完复核）

五个文件的 sha256 与 fixtures 副本**逐条相等**（见 §四·2 那张表），且目录里**没有**新建 `WarpforgeSave/`、`WarpforgeRanked/`：

```
total 13
drwxr-xr-x WarpforgeBattleLog / WarpforgeDaily / WarpforgeDraft / WarpforgeProfile / WarpforgeReplays
-rw-r--r-- WarpforgeDecks.json (1550) / WarpforgeNet.json (70)
```
（所有断言用的都是 `%TEMP%` 下的临时目录；`LegacyRootOverride` 只用来**登记路径**，登记不碰盘。）

### 4.5 类型检查（秒级，`TMPDIR=/tmp/wf_r4 bash 工具/typecheck.sh`）

| 时间点 | 全局 | 我的 8 个文件 |
|---|---:|---|
| 第一轮改完（5 子系统 + ShellRuntime + SaveCenter + SaveKeys） | 运行时 2 / 编辑器 54 —— **全在别人文件**：`Deck/DeckEditorState.cs`（R3）· `RuleEngine/Editor/*`（`CardDef.CountSetSpec/ConditionSpec`，R2/E1）| **0** |
| **✅ 收尾（最终一次）** | **运行时 0 / 编辑器 0（真 0/0）** | **0** |

### 4.6 行尾（`CLAUDE.md` 那条「翻 LF/CRLF」的坑）

全部用 **Edit 工具**改（⛔ 没用 `sed -i`），二进制读出来复核：

| 文件 | `\r\n` | `\n` | 结论 |
|---|---:|---:|---|
| `Inventory.cs` | 0 | 566 | 纯 LF |
| `BattleLogData.cs` | 0 | 425 | 纯 LF |
| `DailyData.cs` | 0 | 2316 | 纯 LF |
| `RankedData.cs` | 0 | 591 | 纯 LF |
| `ShellRuntime.cs` | 0 | 1163 | 纯 LF |
| `SaveCenter.cs` | 0 | 1458 | 纯 LF |
| `SaveKeys.cs` | 0 | 403 | 纯 LF |
| `LegacySaveReader.cs` | 0 | 452 | 纯 LF |

---

## 五、没查清（如实，含「要查得做什么」）

| # | 没查清 | 影响 | 怎么补 |
|---|---|---|---|
| 1 | **`JsonUtility` 的真语义**（236 条断言里凡涉及序列化的都跑在 `System.Text.Json` 替身上）| 替身对「`IncludeFields` / 忽略未知字段 / 坏 JSON 抛异常」三条与 Unity 一致；**字段序、`null` 字符串写法、数组/嵌套类型的细节**可能有差 | `SaveCenter` 那层不依赖这些（它只搬运字符串）；**Daily/BattleLog/Ranked 的 `ToJson` 形态**要在 Unity 宿主里再跑一次（用户口径：复刻完成后恢复自检） |
| 2 | **`File.Replace` 在非 NTFS / 网络盘上的原子性** | 本机 NTFS 验过；兜底是「删旧 + 换名」（**不是原子**，会 `LogWarning`）| 目标机实测一次（N6 §五 #5 仍未解） |
| 3 | **`RankedData` 没有落盘门**（切换前就没有）| 批处理里只要有人调 `SaveNow()` 就会写玩家真 `WarpforgeRanked/ranked.json`（**这是既有行为，不是本轮引入的**）。切换后它还多了一条：若**只**注入了 `SaveCenter.OverrideRoot`（没注入 `RankedData.OverrideDir`），双写会写到玩家真旧档上 | 给它补一道 `StoreOn`（会改批处理行为，**没有 Unity 自检验证前不擅自改**）；或约定「自检两个 root 一起注入」。**已记，未做** |
| 4 | **`ImportAll` 只会在真 Play 里跑** | 批处理/自检里 `SaveCenter.StoreOn == false` ⇒ 导入永远 no-op（这是设计，但意味着**「旧档导入」这条链在自动化路径上一次都没被端到端跑过**）| 真机跑一次（`-wfscene Shell`），看 `[LegacySaveReader]` 那份报告；或写一条「注入两个 root」的自检宿主 |
| 5 | **旧客户端能不能吃我们双写进去的那一份** | 我们写的是原样 payload、不带 `_v` ⇒ 形状与旧客户端自己写的一致；**没有真跑旧客户端验过** | 真机：拿旧 build 读一次（`WarpforgeDecks.json` 最容易验）。E3 §五·6 仍未解 |
| 6 | **回滚旧客户端之后的倒灌策略** | 只有诊断（`NewerLegacyReport`），**不自动倒灌** | E3 §七·4 的「缺口 B」，等人拍板 |

---

## 六、顺手发现（该立的账 —— ⛔ 我没有 `账/*.tsv` 的写权，归 R1/主对话）

1. 🔴 **「切换口必须有一条退化分支」这条通则，方案里没写** —— 根因是**两个落盘门是独立的**（`SaveCenter.StoreOn` vs 各子系统 `StoreOn`）。既有自检**只注入子系统自己的 `OverrideDir`**，因此任何「直接换成 `SaveCenter.SetRaw`」的写法都会让那批自检**静默不落盘**。建议写进方案 §七 Step 4，作为**每个切换单的必答项**：「这个子系统自己的门关着时，切换口写不写旧档？」

2. 🔴 **`Delete` 会漏中央档**（我改了三个子系统）—— **还没切的 `DraftModeWindow` / `CardDetailPopup` / `DeckStore` / `ProfileData` 将来切换时，必须同时改它们的删除口**，否则「删档后从空态起手」会变成**假空**。判据：`ReadThroughLegacy` 是「先中央后旧档」，所以「只删旧档」= 什么都没删。

3. 🟡 **`LegacySaveReader.RegisterMirrors()` 内部其实不判 `StoreOn`**（E3 的交接里写「两者内部都判 `StoreOn`」，**这句不成立**）—— 它只填内存登记表、不碰盘，但登记的是 `LegacyRoot`（默认玩家真目录）下的路径。本轮把 `StoreOn` 判断抬到 `ShellRuntime.Awake` 那一层（`if (SaveCenter.StoreOn) { … }`），**没有改 `RegisterMirrors` 本身**（改了会改变它的契约）。⇒ E3 那份报告/交接里这句话需要订正。

4. 🟡 **`DailyData` 的 `weeklyProgress` 出厂默认是 `13`**（`DailyData.cs:131`），而老档里没有 `weekly*` 三个字段 ⇒ **第一次落盘会凭空写出 `weeklyProgress:13`**。这是 `N17` 那一轮的既有行为（不是本轮引入），但它是「老玩家升级后被送 13 点周常进度」的那类**静默送数据**。⛔ 别拿它当「切换改坏的」。已记，未判——**归日常线**。

5. 🟢 **`DeckStore` 的切换是安全的**（预演结论）：真卡组档（2 副 / 1,550 B）走同一条判据 **零差异**。⇒ R2 只要按 §七 那张补丁改两处，风险很低。**但 `Dto` 里 `CardIds` 是 `List<string>`、`JsonUtility` 对空表写 `[]` 这一条仍然只在 Unity 里能验**（`DeckStore.cs:160-165` 自己标着这个前提）。

6. 🟢 **脚手架多了 5 份真档 fixtures**（`D:/tmp/wf_n6_verify/fixtures/`）。后续谁再加子系统切换，**直接拿它当切前 dump**，别再合成（合成的那两份 —— Inventory / Ranked —— 是因为玩家目录里真的没有那两个文件）。

---

## 七、交接给下一个代理

**状态一句话**：Step 4 的**机制 + 5 个真子系统**都在双写了，每个都有「切前 dump vs 切后两档」的两条 ★ 断言（脚手架 236/0、4 次变异全红）；Step 5 仍不做（关开关即停，已证）。

### 下一步（按风险从低到高）

| # | 谁 | 做什么 | 补丁形状（照 §3.4 抄） |
|---|---|---|---|
| **C5** | **R2**（`Assets/RuleEngine/**`）| `RuleEngine/Data/DeckStore.cs:243-254` 的 `SaveAll` + 读口 | 写：`var json = JsonUtility.ToJson(dto, true); if (!SaveCenter.SaveThroughLegacy(SaveKeys.PlayerDecks, Path, json, out var werr)) { error = werr; return false; }`；读：`var text = SaveCenter.ReadThroughLegacy(SaveKeys.PlayerDecks, Path); … FromJson<Dto>(text)`。**预演已验零差异**（§四·2）。⚠️ 还要看它的删除口（如果有）|
| **C3** | **E6**（`Shell/DraftModeWindow.cs`）| `:825` 写 + 读口，key = `SaveKeys.ActiveEvent` | 同上（`ToJson(dto, true)`）|
| **C7** | **E6**（`Shell/CardDetailPopup.cs:193`）| key = `SaveKeys.OursCardProgress` | ⚠️ **先等收藏线（`Inventories.Cards` 归并）收口**，否则两份会打架（E3 §七·3 已点名）|
| **C8** | 等拍板 | `ProfileData` | ⛔ **先别切**：要先把 `playerName` / `avatarBorderOn` / `achIds/achValues` 迁进中央档，否则双写会抹掉它们 |
| **C9** | **R5**（`Shell/MenuTutorialFlow.cs:79-85`）| `TutorialV2` | **必须双写而不是单向**（`PlayerPrefs` 是现有读路径）；只写中央会**静默停在旧值** |

### 踩过的坑（别重踩）

* 🔴 **两个落盘门是独立的**（见 §一·1 与 §六·1）。切任何一个子系统，先问「子系统自己的门关着时，我的切换口写不写旧档？」
* 🔴 **`Delete` 必须连中央档一起清**（§六·2）。
* 🔴 **`CosmeticData` 永远不许双写**（`profile.json` 6 字段 vs 中央 2 字段）—— 已有断言钉住（M9 就是拿它当靶子）。
* 🔴 **`SaveThroughLegacy` 会用「调用方这次给的路径」**改登记 —— 这是为了**自检不污染玩家真档**；⛔ 别改成「以登记表为准」。
* ⚠️ **`_ours_Inventory.<name>` 有大小写陷阱**：自检已加（`SaveKeys.ValidateCaseFolding` + 两条运行时检查），但**背包名本身**仍然建议一律小写（现在 `ShopData` 用的是 `shop` / `cards`）。
* ⚠️ **变异脚本现在会临时改 3 个文件**（`SaveCenter` / `SaveKeys` / `LegacySaveReader`）—— 跑之前确认没别人在动它们。

### 没做的（明确边界）

* 没跑 Unity、没跑任何自检宿主、没动 git、没改正本 / 账 / 判据文档。
* 没做 Step 5（`LegacyMirrorOn` 仍为 `true`）；`CurrentVersion` 的正式提版入口（C11）仍未加。
* C3 / C5 / C7 / C8 / C9 五个切换单**一个字没动**（不在白名单 / 有前置条件）。
* `RankedData` 的落盘门（§五·3）**没加** —— 会把批处理行为一起改掉，需要主对话先定口径。
