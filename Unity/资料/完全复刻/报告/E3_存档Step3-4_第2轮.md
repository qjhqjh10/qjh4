# E3 · 存档体系 Step 3 / Step 4（第 2 轮）

> **代理**：E3（执行）· 2026-10-25 · 第八十会话 · 完全复刻批次 1
> **白名单（只动了这三个文件里的两个）**：`Shell/SaveCenter.cs` · `Shell/LegacySaveReader.cs`；`Shell/SaveKeys.cs` **一个字节没动**。
> **红线**：⛔ 没跑 Unity · ⛔ 没跑任何 Unity 自检宿主 · ⛔ 没动 git · ⛔ 没改 `CLAUDE.md` / `项目任务.md` / `资料/交接_*` / `账/*.tsv` / 别人的文件 · ⛔ 没删任何旧档（一个 `File.Delete` 都没加）。
> **断言全在工程外**：沿用 N6 的 dotnet 脚手架 `D:/tmp/wf_n6_verify/harness`（用户 2026-10-25 口径：复刻完成前暂停 Unity 自检）。
> 判据 = `资料/完全复刻/判据/存档体系_原版语义与施工方案.md`（下称「方案」）· `报告/N6_存档_第1轮.md`。

---

## 一、结论

| 步 | 完成度 | 一句话 |
|---|---|---|
| **Step 3 兜底** | ✅ **做完** | 坏档现在会**从 `.bak` 自动救回**（方案里「我们比原版强」的那一条，N6 只做了「隔离 + null」，**没有回退**）；缺 key 出声 / 逐 key 迁移沿用 N6；三条负例**用变异测试证明真的会红**（§四·2）。 |
| **Step 4 逐子系统切** | 🟡 **机制做完 · 真子系统 0 个切换** | 双写开关 / 登记表 / **逐字段 dump 对比（验收判据）** / 试点 / 切法设计 **全部做完**；真子系统的文件**一个都不在 E3 白名单**（`Inventory.cs` `DailyData.cs` … 都不是）⇒ 按简报改成 **改动请求**（§七·3，逐文件给了行号与改法）。 |
| **Step 5 关双写** | ⛔ **不适用（本轮不做）** | Step 5 的前提是「Step 4 已双写并验过」，而本轮**没有任何真子系统在双写** ⇒ 没有东西可关。开关本身已做好并有断言（`LegacyMirrorOn = false` ⇒ 只写中央档），**等真子系统切完随时可关**。 |

**三条最要紧的结论**

1. 🔴 **`profile.json` 绝不能登记双写** —— 中央 `CosmeticData` 只有 `avatarIndex/titleIndex` 两个字段，旧档有 **6 个**（含 `playerName` / `avatarBorderOn` / `achIds/achValues`）⇒ 一旦双写就是**用 2 个字段覆盖掉那份 6 字段的旧档 = 直接抹掉玩家名和成就进度**。已在 `LegacySaveReader.RegisterMirrors()` 里**硬编码跳过** `CosmeticOnly` 那一条，并写了理由。
2. 🔴 **双写失败必须让 `Flush()` 变成 `false`** —— 中央档写成、旧档没写成 = 两边不一致，回滚旧客户端会读到过期数据；当成功就是**静默半成功**（本项目红线）。
3. 🔴 **`_v` 信封的保留键**：payload 顶层自己带 `_v` 时套信封会产出**重复键**（谁赢取决于解析器）⇒ `SetRaw` 现在**直接拒绝**并出声。

---

## 二、判据（每条都能指到出处）

| 事实 | 出处 |
|---|---|
| 缺 key ⇒ `Log("No player data found for key X")` + 返回 null；**坏 JSON 没有 try/catch** | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/OnlineData.cs:145-193` |
| 「坏档回退」这一档**原版没有**（所以是本文件里**我们比原版强**的地方，代码注释里逐处标了） | 同上 |
| 原版的数据修复形态 = **按 key 探测 → 缺就补 → 不认识的忽略** | `PlayerDataManager.cs:11229`（`CheckForTutoriaDoneAndDataFix`） |
| 原版删档 = `File.Delete`（**敢删是因为云端有权威副本**）⇒ 我们不许删 | `PlayerDataManager.cs:10157-10172`（`ClearLocalPlayerData`） |
| 原子写先例（tmp → 换名） | `Shell/DailyData.cs:2011-2022`（`Persist`） |
| 落盘门先例（批处理不碰真档） | `Shell/Inventory.cs:404-408` · `Shell/DailyData.cs` 的 `StoreOn` |
| Step 4「双写期」与「切前切后 dump 逐字段相等」的**要求原文** | 方案 §七 Step 4 |
| Step 3 三条负例（删 key / 坏 JSON / `_v` 未来版本） | 方案 §七 Step 3 |
| 原版没有通用 JSON 库可借（工程无 Newtonsoft） | `MyGame/Packages/manifest.json`（无 `com.unity.nuget.newtonsoft-json`）；`JsonUtility` 只能反序列化到固定类型 |

---

## 三、改动清单（`文件:行号`）

### 3.1 `Shell/SaveCenter.cs`（731 → **1355** 行；`git diff --numstat` = **+629 / −5**）

| 行 | 是什么 | 属于 |
|---|---|---|
| `:210-221` | `GetRaw` 的坏档分支改成「**先试 `.bak` 回退**，救不回来才隔离」 | Step 3 |
| `:317-331` | `SetRaw`：payload 顶层带 `_v` ⇒ **拒绝**（防重复键） | Step 3（新发现的坑） |
| `:350-366` | `SetRaw` 写成功后 ⇒ **自动双写**（登记过的 key）；失败 `LogError` + `LastMirrorOk=false` | Step 4 |
| `:472-486` | `Flush()` 改成 `LastWriteOk && LastMirrorOk`（双写失败不许当成功） | Step 4 |
| `:489-500` | `ResetForTest()` 额外清双写表 / 救回计数 | Step 3/4 |
| `:756-771` | **新增** `QuarantineIfAllowed`：**落盘关着时不动玩家任何文件**（批处理纪律） | Step 3 |
| `:793-799` | **新增** `RecoverCount` / `LastRecoveredFrom` | Step 3 |
| `:806-861` | **新增** `TryRecoverFromBackup`：① 试备份（编号大的先试）→ ② 隔离坏档 → ③ **写回主档** | Step 3 |
| `:863-905` | **新增** `BackupCandidates`：只认 `<file>.bak` / `<file>.bak<数字>`，**降序** | Step 3 |
| `:913` | **新增** `LegacyMirrorOn`（**双写期总开关**） | Step 4 |
| `:918-919` | **新增** `LastMirrorOk` / `LastMirrorError` | Step 4 |
| `:926-963` | **新增** `RegisterLegacyMirror`（幂等 / 同 key 换路径拒绝 / 两 key 抢同一文件拒绝） | Step 4 |
| `:966-970` / `:973-980` / `:996-1004` | **新增** `UnregisterLegacyMirror` / `LegacyMirrorPathOf` / `MirrorKeys` | Step 4 |
| `:984-993` | **新增** `MirrorNow`（手动双写一次） | Step 4 |
| `:1009-1016` | **新增** `DiffWithLegacy(key)` = 「中央档 payload vs 旧档」逐字段对比 | Step 4 验收 |
| `:1021-1024` | **新增** `DiffPayload`（转发 `SaveJsonDiff.Diff`） | Step 4 验收 |
| `:1027-1054` | **新增** `DumpMirrorTable`（每条登记 + ✅/🔴 结论） | Step 4 |
| `:1057-1064` / `:1066-1070` | **新增** `MirrorToLegacy`（**写旧档不带 `_v`**）/ `SafeRead` | Step 4 |
| `:1086-1345` | **新增** 顶层 `public static class SaveJsonDiff`：`Diff` / `Flatten` / `Canonical` / `Quote` + 递归下降解析器 | Step 4 验收 |

### 3.2 `Shell/LegacySaveReader.cs`（308 → **421** 行；`+127 / −14`）

| 行 | 是什么 | 属于 |
|---|---|---|
| `:113-121` | `ImportAll` 改成遍历 `Entries` 表（**删掉 6 段手抄调用**） | Step 4（去重） |
| `:133-138` | 报告里加「**双写期诊断**：旧档比中央档新」那一节 | Step 4 |
| `:180` | `ImportProfileCosmetic` 改成吃 `(legacyPath, label, key)`（参数化，供表驱动） | Step 4（去重） |
| `:296-310` | **新增** `public struct Entry`（`Label/Rel/Key/CosmeticOnly/How`） | Step 4 |
| `:312-324` | **新增** `public static readonly Entry[] Entries` —— **「key ↔ 旧档」的唯一正本** | Step 4 |
| `:334-363` | **新增** `RegisterMirrors()`：把已知旧档登记进双写表；🔴 **`CosmeticOnly` 的跳过**（见 §一·1） | Step 4 |
| `:369-402` | **新增** `NewerLegacyReport()`：报「旧档比中央档新**且内容不同**」的 key（回滚旧客户端那条缝） | Step 4 |

### 3.3 没动的
* `Shell/SaveKeys.cs` —— **0 改动**（键表 51 条 + `_ours_` + 3 字符串 key 全部沿用 N6）。
* 任何其他 `.cs` / `.tsv` / 正本 / 交接文档。
* 玩家真存档目录（`C:/Users/qjh36/AppData/LocalLow/QJHWarpforge/WarpforgeReplica`）**只读过、没写过**：夹具是**复制到 `%TEMP%`** 的副本。

---

## 四、断言 / 读数

### 4.1 工程外脚手架（唯一能跑的东西）

```
cd D:/tmp/wf_n6_verify/harness && dotnet run -c Release
=== 结果：PASS 168 · FAIL 0 ===
```
（N6 那一轮是 **95 / 0**；本轮 **+73 条**，**N6 的 95 条一条没退**。）

新增/覆盖的组：

| 组 | 条数 | 覆盖 |
|---|---:|---|
| Step 3 回退 | 14 | ① 有备份 ⇒ 救回 + 写回主档 + 坏档隔离成 `.bak1` ② **两份备份取编号大的（新的）** ③ 备份是**未来版本 ⇒ 不采用** ④ 无 `_v` 的备份当 v1 救回 ⑤ 无备份 ⇒ 原行为（隔离 + null） ⑥ **救回之后照样过迁移链** |
| Step 3 批处理门 | 4 | 落盘关着 ⇒ 能读（内存里）、**坏档原地没动、备份没动、主档没写回** |
| Step 4 双写 | 36（含试点 7） | 登记幂等 / 冲突拒绝 / 两边都写 / **旧档不带 `_v`** / `DiffWithLegacy == ""` / `DumpMirrorTable` / **关开关后旧档不再更新** / **双写失败 ⇒ `LastMirrorOk=false` + `Flush()=false` + 出声** / `_v` 保留键拒绝 / **双写期诊断不误报** |
| Step 4 试点（含在上面 33 里） | 7 | 模拟 `InventoryStore`（形状逐字段照 `Shell/Inventory.cs` 的 `StoreDto/RowDto`）：**切前 dump → 切后中央档 payload 逐字段相等** + **切后旧档与切前 dump 逐字段相等** + 读回值对 + 漂移检测 |
| `SaveJsonDiff` | 19 | 键序不敏感 / `1`==`1.0`==`1e3` / 数组顺序**敏感** / 多字段/少字段 / `null`≠缺字段 / `{}`≠缺字段 / `[]`≠`{}` / 字符串空格 / `\u4e2d`==`中` / `"a.b"`≠嵌套 / `1`≠`"1"` / 坏 JSON 报左右 / 结尾多余内容 / 嵌套路径可读 / 中文原样 |

### 4.2 🔴 **变异测试：负例是真的会红，不是「我说会红」**

做法：把 `SaveCenter.cs` **备份 → 按下面的「改坏法」改一处 → 跑脚手架 → 还原（校验 sha256 完全一致）**。
脚本 `D:/tmp/wf_n6_verify/mutate_run.py`（脚手架，不在工程里）。**5 次变异，5 次都红，每次都还原到同一 sha**。

| 变异 | 「改坏法」= 改哪一句 | 结果 | 变红的关键断言 |
|---|---|---|---|
| **M1** ① 删 key | 缺失分支 `return null;` → `return "{}";` | **FAIL 2** | `返回 null` · `GetRaw 也返回 null` |
| **M2** ② 坏 JSON | `ProbeVersion` 的 `catch (Exception e) { problem = "不是合法 JSON：…" }` → `catch (Exception) { return 0; }` | **FAIL 14** | `主文件已挪走` · `隔离成 .bak` · `.bak 内容原样` · `第二份坏档用 .bak1` · Step3 的 ①②④⑤⑥ 连带 9 条 |
| **M3** ③ `_v` 未来版本 | `GetRaw` 的 `if (version > CurrentVersion(key))` → `… + 1000`，**且** `GuardNotFuture` 的 `if (v > CurrentVersion(key))` → `… + 1000` | **FAIL 9** | `Get 返回 null` · `Get 出声` · `Set/SetRaw/Delete 被拒` · `文件一个字节都没变` · `Exists 仍为 true` · `没读过也能挡住 Set` |
| **M4** 验收判据失灵 | `SaveJsonDiff.Diff` 的 `if (diffs == 0) return "";` → `return "";` | **FAIL 13** | `漂移检测…` · `关掉双写 ⇒ 两边不再相等` · `SaveJsonDiff` 组 11 条 |
| **M5** 双写整块失效 | `SetRaw` 里 `if (LegacyMirrorOn)` → `if (false)` | **FAIL 8** | `旧档也存在` · `★ DiffWithLegacy` · `双写表列出这一条` · `③ 双写失败 ⇒ …` 三条 |
| **M6** 双写期诊断只看 mtime | 删掉 `NewerLegacyReport` 里 `if (central != null && DiffPayload(central, legacy).Length == 0) continue;` | **FAIL 1** | `⑦ 正常同步时**不报**（只看 mtime 会误报）` —— 报出了「旧档 13:57:31Z 比中央档 13:57:31Z 新」这种**每一份健康存档都会命中**的假警报 |

> 📌 **M6 是这条诊断自己的 bug**：双写的顺序是「**先中央、后旧档**」⇒ 正常双写之后旧档 mtime **本来就更新**。
> 第一版 `NewerLegacyReport` 只比 mtime，**会在每一份健康存档上误报**（"看着很有依据"的那一类）。
> 修法 = **先比内容**（`DiffPayload == ""` ⇒ 直接跳过），再比 mtime。⛔ 别改回「只看 mtime」。
>
> 📌 **M4 值得单独看**：它证明「切前切后 dump 相等」这条判据**不是摆设** —— 判据本身坏掉时，13 条断言当场红。
> 📌 **M3 必须同时改两处**：只改读侧时写侧仍能挡住（`GuardNotFuture` 会重新探盘），这正是 N6 那条「不能靠读来加锁」的设计在起作用。

### 4.3 类型检查（秒级，`TMPDIR=/tmp/wf_e3 bash 工具/typecheck.sh`）

| 时间点 | 全局 `error CS` | 我的三个文件 |
|---|---:|---|
| 开工基线 | 25 | **0** |
| 第一次改完 `SaveCenter` | 25（**同一条集合**，全在 E1 的 `Core/UnitState.cs` / `RuleCore.cs`） | **0** |
| 第二次改完 `LegacySaveReader` | 8 | **0** |
| 中途 | 7 → 15（别人在飞的文件在变） | **0** |
| **✅ 收尾（最终一次）** | **0 / 0** | **0** |

> ✅ **最终 `TMPDIR=/tmp/wf_e3 bash 工具/typecheck.sh` = `运行时错误数: 0` / `编辑器错误数: 0`（真 0/0）**。
> 中途那几次 25 → 8 → 7 → 15 **全部是别的代理在飞时的临时状态**（`Editor/NetBattleTest.cs` = E4 · `Editor/ShellScene.cs` = 共用件 · `Shell/DraftModeWindow.cs` / `RankedDivisionChangeWindow.cs` = E6 · `Shell/LiveOpsEvent*.cs` = E2）——
> **从头到尾没有任何一条 `error CS` 落在** `SaveCenter.cs` / `SaveKeys.cs` / `LegacySaveReader.cs`（基线 25 条与最终 0 条可逐条核）。
> 📌 交接提示：如果主对话在**别人还在写**的时候跑 typecheck 看到红的，先按 `CLAUDE.md` 铁律 13·3 判「是不是我负责的文件」。

### 4.4 行尾（CLAUDE.md 那条「翻 LF/CRLF」的坑）

| 文件 | `\r\n` | `\n` | 结论 |
|---|---:|---:|---|
| `SaveCenter.cs` | 0 | 1355 | 纯 LF（N6 建的就是 LF，**没被翻**） |
| `LegacySaveReader.cs` | 0 | 409 | 纯 LF（同上） |
| `SaveKeys.cs` | 0 | 363 | 纯 LF（没动） |

> 全部用 **Edit 工具 + python `wb`** 改，**没有用 `sed -i`**。

---

## 五、没查清（如实，含「要查得做什么」）

| # | 没查清 | 影响 | 怎么补 |
|---|---|---|---|
| 1 | **`JsonUtility` 的真实语义**（本轮的 165 条断言跑的是 `System.Text.Json` 替身） | 替身对 `IncludeFields` / 未知字段忽略 / 坏 JSON 抛异常这三条与 Unity 一致；但 **null 字符串写法、字段序、对数组/嵌套类型的细节**可能有差 | Step 4 真接入前，在 **Unity 宿主里再跑一次**（用户口径：复刻完成后再恢复自检） |
| 2 | **`File.Replace` 在非 NTFS / 网络盘上的原子性** | 只在本机 NTFS 验过；兜底是「删旧+换名」（**不是原子**，会 `LogWarning`） | 目标机实测一次（N6 §五 #5 仍未解） |
| 3 | **`DailyLimits` / `CurrentDeck` / `LevelData` / `PlayerSeason` 的原版 schema** | 所以它们仍然「整份塞进一个 key」或空缺；**没有拆** | 同 N6 §五 #1；本轮**没有进展**（不属我的白名单，需要读原版 CloudScript 写链） |
| 4 | **`_ours_Inventory.<name>` 的两个真值大小写**（真机只出现过 `cards` / `shop`，都是小写） | Windows 文件名**大小写不敏感** ⇒ 若将来出现 `Shop` 与 `shop` 两个背包，`PathOf` 会撞成同一个文件（**两个 key 一个文件**，互相覆盖） | 键表侧加一条「大小写折叠后不许重复」的自检（⚠️ 现在**没有**）；或背包名统一小写 |
| 5 | **回滚旧客户端之后的倒灌策略** | 已做**诊断**（`NewerLegacyReport`：**内容不同 ∧ 旧档 mtime 更新**才报）但**不自动倒灌** —— 用旧档覆盖中央档要人拍板 | 定策略后加 `ImportAll(takeNewerLegacy: true)`（设计见 §七·4 的「缺口 B」） |
| 6 | **旧客户端的读口是否真的能吃我们双写进去的这份** | 我们写的是**原样 payload、不带 `_v`** ⇒ 形状与旧客户端自己写的一致；但**没有真跑旧客户端验过** | 真机：拿旧 build 读一次（`WarpforgeDecks.json` 最容易验） |
| 7 | **`profile.json` 那 4 个没迁的字段该归哪个 key（原版 schema）** | 直接决定了 `CosmeticData` **能不能登记双写**（现在不能） | 见方案 §六·1；`achIds/achValues → AchievementsData`、`playerName → NameChanges`、`avatarBorderOn → CosmeticData` |

---

## 六、顺手发现（该立的账 —— ⛔ 我没动 `账/*.tsv`，归 P 线）

1. 🔴 **「双写」有一个前提没写在方案里：`key` 的 payload 必须是旧档的【完整内容】**。
   只迁了一部分字段的 key（现在就是 `CosmeticData`）**一旦双写就会抹掉旧档里没迁的字段**。
   ⇒ 建议立一条通则：**Step 4 每个子系统的切换单里加一行「这个 key 的 payload 是否 = 旧档全文？」**，否则会静默丢数据。已在本文件 `RegisterMirrors` 里硬挡住第一条实例。

2. 🔴 **`LegacySaveReader` 的行号被我不小心挪了** —— 方案 §8.2 有三处按行号引用它：
   - `:115`（`DailyLimits` 本轮不拆）→ 现在 **`:315`**
   - `:119`（`current` 也在里面 / `CurrentDeck` 不拆）→ 现在 **`:319`**
   - `:120-121`（`PlayerSeason` payload 是我们自己的形状）→ 现在 **`:321`**
   原因：那 6 条 `How = …` 说明从**调用点**搬进了 `Entries` 表（去重，一处正本）。
   **判据文档那边需要谁去改一下这三处行号**（我无权改判据文档）。

3. 🟡 **`SaveCenter.DumpAll()` 的「键表里没有文件的 N 条」对 `_ours_` key 不可见** —— 它只遍历 `SaveKeys.All`（51 条原版 key），**自定 key（`_ours_*`）不在里面**，所以「缺哪些」那一行永远不列它们。不影响正确性（文件数那行是对的），但**排查会漏**。要改就改 `DumpAll`（我的白名单内），只是本轮没做（不在 Step 3/4 范围）。

4. 🟡 **`ImportProfileCosmetic` 生成的 payload 字段序 = DTO 声明序**（`avatarIndex, titleIndex`），而旧档是 `avatarIndex, titleIndex, avatarBorderOn, …`。逐字段比是相等的，**逐字节不等** —— 如果将来有人拿「逐字节相等」当判据会误报。判据请一律用 `DiffPayload`。

5. 🟡 **方案 §七 Step 3 写的「三条负例各一条断言」，N6 那一轮其实**没有**逐条写「改坏法」** —— 本轮补上了（§四·2 的 M1/M2/M3），并且是**用变异测试真的跑红**，不是文字声明。

6. 🟢 **脚手架本身被加固了两处**（`D:/tmp/wf_n6_verify/harness/Program.cs`，**工程外**）：
   `Sha()` 对不存在的文件返回 `<missing>`、`.bak 内容原样` 先判存在 —— 否则**变异测试会崩在半路**，只报 2 条红就中止（第一版就是这样）。

---

## 七、给 E2 / E4 的落盘用法（+ 切法设计 + 改动请求）

### 7.1 用法（API 已稳定，可直接用）

```csharp
using CardPresentation;

// ── 读 ────────────────────────────────────────────────────────────
string raw  = SaveCenter.GetRaw(SaveKeys.LiveOps);        // 已剥 `_v` 的 payload；缺/坏/未来版本 ⇒ null + 出声
MyDto dto   = SaveCenter.Get<MyDto>(SaveKeys.LiveOps);    // null 时**必须自己区分**「第一次跑」与「档丢了」
bool   has  = SaveCenter.Exists(SaveKeys.LiveOps);        // 纯事实（未来版本也算存在）

// ── 写（立即落盘，原子换名，双写期自动带旧档）────────────────────
bool ok = SaveCenter.Set(SaveKeys.LiveOps, dto);          // dto == null ⇒ 拒绝（别拿 null 当「清空」）
bool ok2= SaveCenter.SetRaw(SaveKeys.LiveOps, jsonText);  // 已拼好的 payload（首尾必须是 { }）
bool all= SaveCenter.Flush();                             // 上一次写（含双写）真的成了吗

// ── 改 schema（照原版 DataFix 形态：探测旧字段 → 补新字段 → 不认识的忽略）
SaveCenter.SetCurrentVersionForTest(...)  // ⛔ 只在自检里用
SaveCenter.RegisterMigration(SaveKeys.LiveOps, fromVersion: 1, payload => /* 返回新 payload */);
// 要在「本代码版本」上真正生效，还得把 CurrentVersion 提上去（正式口见 §七·4 的改动请求）

// ── 双写（Step 4 的切换点）────────────────────────────────────────
SaveCenter.RegisterLegacyMirror(SaveKeys.LiveOps, "<旧档绝对路径>");  // 幂等；登记后每次 Set* 自动双写
string diff = SaveCenter.DiffWithLegacy(SaveKeys.LiveOps);           // "" = 逐字段相等（**验收判据**）
Debug.Log(SaveCenter.DumpMirrorTable());                             // 全部登记的现状

// ── 红线（⛔ 别做）──────────────────────────────────────────────
//  · 别用 Delete 当「清空」—— 它只是改名到 .del-<时间戳>
//  · 别把 key 拼错求「大概能跑」—— 不在键表里当场拒绝并出声
//  · 新 key 一律先写进 `SaveKeys.cs`，原版没有的要带 `_ours_` 前缀
//  · payload 顶层**不许**自己带 `_v`（保留键，会被拒绝）
```

**`_ours_` key 的两个现成约定**：`SaveKeys.OursCardProgress`（`_ours_CardProgress`）· `SaveKeys.OursInventoryPrefix + name`（`_ours_Inventory.shop` / `_ours_Inventory.cards`）。

**给 E2（LiveOps）的特别提示**：`SaveKeys.LiveOps` 是**原版真 key**（枚举值 41），可以直接用；建议本批先只做 `GetRaw/SetRaw`，**先别登记双写**（LiveOps 现在没有旧档，登记只会多一次写盘失败的可能）。E2 若要「首次启动导入旧档」，走 §七·3 的改动请求，别自己改我的文件。

**给 E4（社交/聊天）的特别提示**：`SaveKeys.FriendsData` / `GuildData` / `BlockedPlayers` / `ReportsPlayers` / `LastMessageReadTime` 都已在键表里（原版 16/23/29/30/27）。聊天相关目前**没有任何落盘**，属「新增」而不是「迁移」⇒ 直接 `Set/Get`，**不需要双写**（没有旧档要对齐）。

### 7.2 切法设计（每个子系统都按这 5 步走，⛔ 不许跳）

```
① 切前 dump：把该子系统**旧档原文**存一份（cp 到临时目录）
② 登记双写：SaveCenter.RegisterLegacyMirror(<key>, <旧档绝对路径>)
③ 改写入点：把「自己 File.WriteAllText」换成 SaveCenter.SetRaw(key, 同一份 json)
              —— 双写由 SaveCenter 自动完成（中央档 + 旧档各一份），**不要两处都写**
④ 改读取点：先 SaveCenter.GetRaw(key)，null 再退回读旧档（双写期的过渡读法）
⑤ 验收：   SaveCenter.DiffPayload(①的 dump, SaveCenter.GetRaw(key)) == ""
           且 SaveCenter.DiffPayload(①的 dump, 旧档原文) == ""
           ⇒ **逐字段相等才算切完**（不是「看着差不多」）
```

⚠️ **顺序**（照方案 §七 Step 4，风险从低到高）：`InventoryStore` → `BattleLogData` → `DraftModeWindow` → `DailyData` → `TutorialV2`。
⚠️ 每切一个**单独一批**，别混在一起（出问题分不清是谁）。

### 7.3 改动请求（真子系统的文件不在 E3 白名单 ⇒ 逐条给了行号与改法）

| # | 文件:行 | 改什么 | 风险/注意 |
|---|---|---|---|
| **C1** | `Shell/Inventory.cs:429` | `File.WriteAllText(p, JsonUtility.ToJson(dto))` → 先 `string json = …ToJson(dto);` 再 `SaveCenter.RegisterLegacyMirror(SaveKeys.OursInventoryPrefix + name, p);` + `SaveCenter.SetRaw(SaveKeys.OursInventoryPrefix + name, json);` | **试点首选**（方案原定第一件）；`name` 必须是 ASCII（现在 `cards` / `shop` 都是） |
| **C1b** | `Shell/Inventory.cs:443-473` | `Load` 的读口：`string json = SaveCenter.GetRaw(key) ?? (File.Exists(p) ? File.ReadAllText(p) : null);` 其余解析不变 | 双写过渡读法；**别删旧读** |
| **C2** | `Shell/BattleLogData.cs:296` | 同上（key = `SaveKeys.BattleLogData`，旧档 = `StorePath`） | 注意它是 `ToJson(dto, true)`（**带缩进**）—— 不影响逐字段比较 |
| **C3** | `Shell/DraftModeWindow.cs:825` | 同上（key = `SaveKeys.ActiveEvent`） | 同上（`true` 缩进） |
| **C4** | `Shell/DailyData.cs:2011-2022` | 同上（key = `SaveKeys.DailyCounters`） | 🔴 **它现有的 tmp→Move 会被 `SaveCenter` 的 tmp→Replace 取代**；切换时**必须保住「原子写」这条性质**，并加一条「断电模拟」断言（`SaveCenter` 已有一条同型断言可抄） |
| **C5** | `RuleEngine/Data/DeckStore.cs:243-254` | 同上（key = `SaveKeys.PlayerDecks`） | 卡组是**最重要的进度**；切之前先拿真档验一遍 `DiffPayload` |
| **C6** | `Shell/RankedData.cs:194-205` | 同上（key = `SaveKeys.PlayerSeason`） | ⚠️ payload 是**我们自己的形状**（不是原版 schema），切了也只是「换个地方存」 |
| **C7** | `Shell/CardDetailPopup.cs:193`（`Save`） | 同上（key = `SaveKeys.OursCardProgress`） | ⚠️ 收藏线正在把「拥有数」往 `Inventories.Cards` 迁（`资料/完全复刻/报告/C_CardProgress归并_第1轮.md`）⇒ **先等那条线收口**，否则两份会打架 |
| **C8** | `Shell/ProfileData.cs` 的 `SaveDto` 写点 | ⛔ **先别切**。要先把 `playerName` / `avatarBorderOn` / `achIds/achValues` 也迁进中央档（§五·7），否则**不许登记双写** | 见 §一·1 |
| **C9** | `Shell/MenuTutorialFlow.cs:79-85`（`MarkCompleted`） | `PlayerPrefs.SetInt` 之后**再加**一句中央写：`SaveCenter.SetRaw(SaveKeys.TutorialV2, …)`，形状照原版 `TutorialV2SaveData { stages:[{stageId,isCompleted}] }`（判据：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TutorialV2SaveData.cs`，字段名 `stages` / `stageId` / `isCompleted` 逐字） | 🔴 **必须双写而不是单向**：`PlayerPrefs` 是**现有读路径**，只写中央会让两边漂；读取口也要能读中央。**别只做「一次性导入」**（会静默停在旧值） |
| **C10** | `Shell/ShellRuntime.cs:309`（`void Awake()`） | 加 `LegacySaveReader.ImportAll();` + `LegacySaveReader.RegisterMirrors();`（两者内部都判 `StoreOn`，批处理/自检自动 no-op） | 这是**唯一**让 Step 2 的导入真正跑起来的地方；不接的话 `ImportAll()` 永远没人调 |
| **C11** | `Shell/SaveCenter.cs` 的 `CurrentVersion` | 需要一个**正式**的「把某个 key 的版本提上去」的入口（现在只有 `SetCurrentVersionForTest`） | ⚠️ 归我（下一轮我来做）；本轮 Step 4 没真迁移，所以先没加 |

### 7.4 「缺口 B」：双写期遇到「回滚旧客户端」怎么办（**设计，未实现**）

```
现状：ImportAll() 的规则是「中央档已有 ⇒ 跳过（不覆盖）」⇒ 用户回滚旧客户端玩一轮、再回来，
      中央档会**停在旧值**（旧客户端只写旧档）。本轮只加了**诊断**（NewerLegacyReport），不自动倒灌。
建议：① 诊断升级成**开局提示**（「检测到旧版存档更新，是否用它覆盖？」）；
      ② 或加 `ImportAll(takeNewerLegacy: true)`：仅在「旧档 mtime > 中央档 mtime」时**先备份中央档**再导入。
      ⛔ 不管哪种，**都必须先备份中央档**（`SaveCenter.Delete` 那种 `.del-*` 改名即可复用）。
```

---

## 八、交接给下一个代理

**状态一句话**：Step 3 **做完并验红**（坏档能从 `.bak` 救回、批处理不动玩家文件）；Step 4 **机制 + 验收判据 + 试点做完，真子系统 0 个切换**（都在别人文件里，改动请求见 §七·3）；Step 5 **不适用**。

**下一个人该先做什么（按顺序）**

1. ✅ **typecheck 交回时已经是真 0/0**（见 §四·3）。主对话在同步点再跑一次即可确认；如果那时看到红，按铁律 13·3 先判是不是别人在飞的文件。
2. **接 C1（`Inventory.cs`）** —— 这是方案原定的**第一件试点**，改法已在 §七·2 / §七·3 写死；改完**必须**跑：
   `SaveCenter.DiffPayload(<切前 dump>, SaveCenter.GetRaw("_ours_Inventory.shop"))` == `""`。
   ⚠️ `WarpforgeInventory/` 在玩家真目录里**还不存在**（N6 §⑥·7 仍未变）⇒ 要么真跑一次商店购买写出它，要么在自检里用临时目录。
3. **接 C10（`ShellRuntime.Awake`）** —— 否则 `ImportAll()` 永远不会被调用，Step 2 的迁移等于没接。
4. **`SaveKeys.cs` 加一条自检**：**大小写折叠后 key 不许重复**（Windows 文件名大小写不敏感 ⇒ 两个 key 会撞成同一份档）。这是我在 §五·4 记下的洞，改在我白名单内，**下一轮我来或你来做都行**。
5. **`DumpAll()` 把 `_ours_*` 也算进「缺哪些」**（§六·3，小活）。

**踩过的坑（别重踩）**

* 🔴 **`CosmeticData` 不许双写**（2 字段 vs 旧档 6 字段 = 抹掉玩家名与成就）。通则见 §六·1。
* 🔴 **`SaveJsonDiff` 是本轮新写的通用 JSON 解析器**（工程没有 Newtonsoft）。它**数组顺序敏感**、数字按「最简十进制」归一 —— 别拿它当「宽松比较」，也别用它替代 `JsonUtility` 做反序列化。
* 🔴 **变异测试脚本 `D:/tmp/wf_n6_verify/mutate_run.py` 会**临时改写工程里的 `.cs`**（`SaveCenter.cs` / `LegacySaveReader.cs`，备份在 `%TEMP%`，跑完立刻还原并打印 sha256）。**跑它之前先确认没别人在改这两个文件**。
  本轮基线 sha256：`SaveCenter.cs` = `58f8d5fd9b5286cc5e4df1aca65feef15f703440377082ba7ecf4b1b1537069d` · `LegacySaveReader.cs` = `2eb2102261c4aec24c3fe3b57776f5e3f280a94611447d40f06d88347e5718d8`。
* 🔴 **双写期诊断不能只看 mtime**（M6）：双写顺序是「先中央、后旧档」⇒ 正常写完旧档 mtime 本来就更新。判据必须是「**内容不同 ∧ 旧档更新**」。
* ⚠️ **`LegacySaveReader` 的行号被挪过**（§六·2），判据文档里按行号引用的三处需要更新。
* ⚠️ N6 的脚手架对「文件不存在」不设防（`File.ReadAllText` 直接抛）⇒ 本轮已加固 `Sha()` 与两处 `File.ReadAllText`；**再加断言时请沿用这个写法**。

**没做的（明确边界）**

* 没跑 Unity、没跑任何自检宿主、没动 git、没改任何正本/账/判据文档。
* 没做 Step 5（没有真子系统在双写，无从关）。
* `CurrentVersion` 的正式提版入口（C11）没加。
* `TutorialV2` 的读取/写入（C9）**只给了设计**：单独做一半（只导入不双写）会**静默停在旧值**，所以我故意没做。
