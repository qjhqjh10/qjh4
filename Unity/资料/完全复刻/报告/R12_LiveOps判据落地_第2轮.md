# R12 · LiveOps 判据落地（第 2 轮）—— 报告

- 会话：2026-10-25 · 第八十会话 · 批次 2 · 执行代理 **R12**
- 输入判据：`资料/完全复刻/判据/LiveOpsHandler_11方法_第2轮.md`（654 行，R7 产出）
  ＋ `资料/完全复刻/报告/R7_LiveOpsHandler判据_第2轮.md` §③（12 条改动清单）
- 独占白名单（只动这两个）：
  `Assets/CardPresentation/Shell/LiveOpsHandler.cs` · `Assets/CardPresentation/Shell/LiveOpsEvent.cs`
- 没有碰：`账/*.tsv`（只读；行号漂移表见 §四）· 正本 · git · Unity / 自检 · `D:/2/`
- 类型检查：`TMPDIR=/tmp/wf_r12 bash d:/4/Unity/工具/typecheck.sh` ⇒ **0 / 0**（见 §四 的两条瞬态干扰说明）

---

## §一 结论（一句话）

> **12 条全部落地：实改代码 8 条（#1 #2 #3 #4② #5 #6 #7 #9）、只改注释 4 条（#8 #10 #12 ＋ #11 指路）、
> 1 条（#4① `MenuActions.ReleaseAction`）因全仓无等价物而**记成改动请求**（按简报要求，⛔ 没自己造）。
> （#4 拆成 ①② 两条计数，所以 8+4+1 = 13 > 12 条编号。）
> 其中最危险的一条 —— `ResetEvents` 语义整条反了 —— 已订正，且**影响面已核：0 个调用方依赖「清空」。**
> 类型检查 0/0；玩家可见变化预期 = **0**（见 §四 可达性）。**

三条需要向上游说清的事：

1. **`#7` 需要一个新的数据前提**：原版 `Initialize()` 读 `Data.filter.hasDuration` / `eventDuration`，
   而我们**根本没有 `EventFilter` 这个类**、`LiveOpsEventData` 也没有 `filter` 字段。
   ⇒ 本批**补了最小等价物** `LiveOpsEventFilter`（只有 `hasDuration` / `eventDuration` 两格，
   默认值与原版逐字一致：`false` / `24` 小时），并在 `LiveOpsEventData` 上补 `filter` 字段（字段初始化器给非 null，
   ⛔ 不靠「恰好没人读」蒙过去）。**其余 filter 字段（`army`/`expansion`/`hasCooldown`/`eventCooldown`/
   `eventGroups`/`Triggers`/`Conditions`）属 C1-W2，没搬就是没搬。**
2. **`#1` 的排序键现在是对的，但实际是「空转」**：13 份 SO 里 `priority` 有真值（`0/3/5/42` 四档），
   而我们的 `LiveOpsLocalEvent` / `MapData` **没搬这一格** ⇒ `PriorityOf()` 全返回 0 ⇒ 排序退化成「保持输入序」。
   要让它真的生效，得在 `LiveOpsLocalCatalog.cs` 加字段（**不在我白名单**，见 §六·1）。
3. **`#4①` 的前提确认不存在**：全仓**没有** `MenuActions` 等价物（逐条 grep 见 §四·3）⇒ 按简报只做 ②。

---

## §二 判据（本轮引用到的原版坐标，一处不落）

| 用在哪 | 判据 |
|---|---|
| #1 排序 | `script.json` RVA `0xC99380` = `Enumerable.OrderByDescending<object,int>`；key selector `<>c.<SetupEvents>b__18_0` = `GetBaseData()` 之后读 `+0x40`（`LiveOpsEventData.priority`） |
| #2 / #3 | `0x17C08 mov rcx,[rbx+0x30]`（字段 `shared`，形参未读）· `0x17C5B call 0x181019810` → `script.json` 反查 `LiveOpsHandler<object>$$TryScheduleHandlerRefresh` |
| #4② | `0x17AEA mov r8d,3` + `call 0x1803F17A0` ⇒ `blockedEvents` 也走 `ILiveOps` slot 3 = `Dispose` |
| #5 | `ResetEvents` VA `0x181018B90`：只读 `[this+0x20]`、循环 `mov r8d,7`（slot 7 = `ResetEventProgress`）、**无任何 `List.Clear`** |
| #6 | `…DisplayClass9_0…<UpdateCachedSave>b__0.c`：`String.Equals([[x+0x18]+0x10], [[saveGame]+0x10])` ⇒ 比 `RawEvent.Persistence.eventId` |
| #7 | `0x181016E70`：`0x181016EAF cmp byte[filter+0x18],0`（`hasDuration`）· `0x181016ED7 imul eax,[filter+0x1C],0x36EE80`（`eventDuration` × 3,600,000）· `0x181016EF8 add rdi,[save+0x20]`（`triggerTime`）· `0x181016F31` slot 10 = `TryScheduleHandlerRefresh` |
| #8 | `0x181016CA0`：`Activator.CreateInstance<TSave>()` → TData 虚方法（= `GetID()`）→ `mov [rcx], rax` 写 `+0x10`（`eventId`） |
| #9 | `0x181017250`：`GetType()` → `Type.op_Inequality(typeof(TData))` → `jne <return>` |
| #12 | `0x181018250`：`inc dword [rbx+0x70]` → 循环 slot 1 = `Initialize` → `Task.CompletedTask`，**无 try/catch** |
| #7 补充判据（默认值） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Everguild.LiveOps/EventFilter.cs`：`public int eventDuration = 24;`（`hasDuration` 无初始化器 ⇒ 默认 `false`） |

---

## §三 改动清单（`文件:行号` = **改完之后的真实行号**）

### 3·0 逐条对照：12 条 → 做了 / 未做 / 为什么

| # | 判据件要求 | 结论 | 一句话理由 |
|---|---|---|---|
| **1** | 排序键：`GetID()` 序数升序 → `Priority` **降序** | ✅ **做了** | 改成 `ops.OrderByDescending(PriorityOf)`（LINQ 稳定排序，同原版）；⚠️ 实际是空转 —— `priority` 没搬进本地目录（§六·1） |
| **2** | `ScheduleHandlerRefresh(FromMs(ms))` → `TryScheduleHandlerRefresh(ms)` | ✅ **做了** | 两道门（已排更早⇒不动 / 已过点⇒丢弃）现在生效；本地 `shared` 永远没有 `NextRefresh` ⇒ **执行不到**（§4·6） |
| **3** | 读字段 `this.shared` 而不是形参 | ✅ **做了** | 形参保留但不用（与原版一致）；`SetupEvents` 已先赋 `shared` ⇒ 调用链等价，派生类重写时分叉 |
| **4①** | `foreach(a in handlerActions.Values) MenuActions.ReleaseAction(a)` | ❌ **未做（改动请求）** | **全仓没有 `MenuActions` 等价物**（§4·3 逐条核过）⇒ 按简报「没有就只做 ②，① 写进报告」；⛔ 没自己造 |
| **4②** | `blockedEvents` 也逐个 `Dispose` | ✅ **做了** | 低风险，直接补（`0x17AEA mov r8d,3`） |
| **5** | `ResetEvents` = 只 `foreach e.ResetEventProgress()` | ✅ **做了** | **grep 已核：0 个调用方依赖「清空」**（§4·4）⇒ 风险从「中高」降为「实际为零」；连带语义见 §六·3 |
| **6** | `r.Data.eventId` → `r.Persistence.eventId` | ✅ **做了** | 并补 `r.Persistence != null`；本地 `handlerCache` 恒 null ⇒ 执行不到 |
| **7** | 先 `if (!Data.filter.hasDuration) return;`，再 `eventDuration*3600000L + triggerTime` | ✅ **做了（含新增前提类）** | 我们**没有 `EventFilter`** ⇒ 先补最小等价物 `LiveOpsEventFilter` + `LiveOpsEventData.filter`（§一·1） |
| **8** | `CreateNewSave` 的注释改成「已坐实」 | ✅ **做了（只改注释）** | 写清「`triggerTime` 不在这里写、⛔ 别塞 `dates.startDate`」 |
| **9** | `data as TData` → 严格 `data.GetType() != typeof(TData)` | ✅ **做了** | 子类现在**被忽略**（原版口径）；我们额外出声（原版静默/ NRE） |
| **10** | `GetEvents` 的注释改成「懒式 Concat+Cast / 我们立即拷贝 = 有意差异」 | ✅ **做了（只改注释）** | 代码不动（内容等价） |
| **11** | `ScheduleEventPopups` 真身 —— **只留指路** | ⏭ **按简报不做，指路已写进代码 doc** | 属 C1-W2，还要跑一次 VA 反汇编；入口 `0x181019560`；⚠️ 反常次序别改 |
| **12** | `InitializeHandler` 的 try/catch 记成「有意差异」 | ✅ **做了（只改注释）** | 保留我们逐个 catch 的做法 |

### 3·1 `Assets/CardPresentation/Shell/LiveOpsHandler.cs`（705 → **777** 行）

| # | 位置（新行号） | 改动 | 内容 |
|---|---|---|---|
| — | `:26` | 新增 `using System.Linq;` | 为 `OrderByDescending`（稳定排序，与 `List.Sort` 不同） |
| **1** | `:191`（`SetupEvents` 三分支里的装载段 `:195-203`） | 排序键 | `List.Sort(string.CompareOrdinal(GetID()))`（**升序/按 id**）→ **`ops.OrderByDescending(PriorityOf)`**（`GetBaseData().priority` **降序**，稳定） |
| — | `:177-190` | 文档 | 把 `SetupEvents` 的逐句伪码改成含 `OrderByDescending(...priority)`；新增「有意差异：`Cast<TEvent>` 会抛 / 原版赋新 List」 |
| **2 / 3** | `:228`（体 `:229-240`） | 调用目标 + 数据源 | `if (sharedData == null) return;` + `sharedData.TryGetLong(...)` + `ScheduleHandlerRefresh(FromMs(ms))` → **读字段 `this.shared`** + **`TryScheduleHandlerRefresh(ms)`**（两道门生效）；`shared == null` 出声后返回（原版 NRE，记为有意差异） |
| **11（只指路）** | `:242-251`（`ScheduleEventPopups` 的 doc；声明在 `:252`） | 注释 | 补上交给 W2 的坐标：真身 = cpp2il `LiveOpsHandler.cs:1606`；`ShowTriggerEventPopup` = **`0x181019560`**；逐事件读 `drawData.ShowWindowOnTrigger`/`ShowWindowOnExpire`；⚠️ **`ExpireEvent` 弹窗支「开窗后立刻走完成回调」的反常次序 ⛔ 别改** |
| **12** | `:391-395`（`InitializeHandler` 的 doc；声明在 `:396`） | 注释 | 写明「原版无 try/catch，我们逐个 catch = **有意差异**（坏事件不断整条链）」 |
| **4②** | `:423`（体 `:425-441`，其中 ③ = `:433-439`） | 新增 | `blockedEvents` 逐个 `Dispose()`（原版走同一条 slot 3）；**#4① 未接**，doc 里写清「全仓无 `MenuActions` 等价物 ⇒ 记成改动请求」 |
| **5** | `:456`（体 `:458-464`） | **语义整体替换** | `foreach Dispose()` + `events.Clear()` + 日志 → **`foreach e.ResetEventProgress()`（只这一句；不清空、不 Dispose）**；doc 里留「2026-10-25 订正」痕迹 + 影响面核查结论 |
| **10** | `:468-475`（`GetEvents` 的 doc；声明在 `:476`） | 注释 | 改成「原版 = 懒式 `Concat<ILiveOps>().Cast<TEvent>()`（会抛）；我们立即拷贝 = **有意差异**」 |
| **6** | `:534`（体 `:535-546`，比较在 `:541-542`） | 比较字段 | `r.Data.eventId` → **`r.Persistence.eventId`**，并判 **`r.Persistence != null`** |
| — | `:562`（类尾新增） | 新成员（**追加在类尾**） | `static int PriorityOf(ILiveOps e)` = `e.GetBaseData().priority`（空对象按 0；原版是 NRE） |

### 3·2 `Assets/CardPresentation/Shell/LiveOpsEvent.cs`（526 → **575** 行）

| # | 位置（新行号） | 改动 | 内容 |
|---|---|---|---|
| **7（前提）** | `:84-101`（新增类） | **新增** | `[Serializable] public class LiveOpsEventFilter { public bool hasDuration; public int eventDuration = 24; }` —— 只搬 `Initialize` 用得到的两格，其余字段在 doc 里点名「属 C1-W2、没搬」 |
| **7（前提）** | `:119-121` | 新增字段 | `LiveOpsEventData.filter`（`LiveOpsEventFilter`，**字段初始化器给非 null**；原版靠 Unity 序列化非 null） |
| — | `:26-30`（文件头差异 ③） | 就地订正 | 原「`Initialize` 的两个字段读不出来、按 `triggerTime` 近似」**作废** → 改写成已坐实的算式与判据出处 |
| **7** | `:337`（体 `:338-351`） | 语义 | 加 **`if (!Data.filter.hasDuration) return;`**（原版唯一早退）＋ `milis = (long)Data.filter.eventDuration * 3600000L + SaveGame.triggerTime` → `TryScheduleHandlerRefresh(milis)` |
| **8** | `:415-421`（`CreateNewSave` 的 doc） | 注释 | 「未坐实」→「**已坐实**：`save.eventId = data.GetID()`；**`triggerTime` 不在这里写**（⛔ 别塞 `dates.startDate`）」 |
| **9** | `:443`（体 `:444-453`） | 判定口径 | `var d = data as TData; if (d == null)`（子类**被接受**）→ **`data.GetType() != typeof(TData)` ⇒ return**（子类**被忽略**，与原文一致） |

---

## §四 读数（可复查的数字 / 命令 / 输出）

### 4·1 类型检查（唯一允许的验证）

```
TMPDIR=/tmp/wf_r12 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

⚠️ **两次瞬态干扰（都不是本批的问题，按 CLAUDE.md 13·3 未去动别人的文件）**：

| 时刻 | 报错 | 判决 |
|---|---|---|
| 第 1 次（22:41） | `Shell/LegacySaveReader.cs(544,26) error CS0116` | 该文件 mtime **22:40:52**（= 我跑检查前 14 秒）⇒ **别的代理正在写**（半个方法写在 doc 注释后面）。隔 2 分钟重跑 **0/0** |
| 第 3 次（收尾前） | `RuleEngine/Data/AiScoring.cs(210/244/263)` 三条（`MightAct` / `ScoreFromJam` / `ScoreFromBuff` 未定义） | 该三文件是 **R11 的白名单**（`SimpleAI/AiScoring/AiTargets`）⇒ 别人在飞，**不是我的** |

⇒ **本人两个文件在「去掉别人在写的文件」之后是 0 错**；上面第 2 次（中间那次）就是干净的 0/0，
**收尾时（所有代码改动之后）又跑了一次，同样 0/0**（那一跑就是本节顶部贴的输出）。

### 4·2 行尾（没翻）

```
LiveOpsHandler.cs  50,427 B  CRLF=0  LF=777  bareCR=0
LiveOpsEvent.cs    35,188 B  CRLF=0  LF=575  bareCR=0
```

（两个文件都是**未跟踪的新文件**（`git status` = `??`），本来就是纯 LF；本轮全部用 Edit 工具，未用 `sed -i`。）

### 4·3 `MenuActions` 等价物 —— **不存在**（逐条看过的 6 处命中）

`grep -rn "MenuActions" Assets/` 一共 22 行，**全部是注释**，实体零个：

| 命中 | 实际内容 | 是不是等价物 |
|---|---|---|
| `Shell/LiveOpsHandler.cs:150` | 我们自己的字段 doc（原版类型名） | ❌ 只是注释 |
| `Shell/ToastNotifications.cs:47` / `:735` / `:1159` | `_scheduled` 队列 = `MenuActions.**QueueAction**` 的近似 | ⚠️ **只有「排队」没有「按句柄撤销」**：`_scheduled` 是 `Queue<T>`，没有 id/handle、没有 `Remove` 口 ⇒ **不能当 `ReleaseAction`** |
| `DailyRewardPopup.cs:45` · `WindowsManager.cs:833` · `CampaignTab.cs:1104` · `ForgeTab.cs:858` · `TutorialMessagePopup.cs:67-68` · `MissionsTab.cs:49` · `MenuTutorialStageDef.cs:125/153` · `MenuTutorialFlow.cs:13/26/27/322/675/690/714/873/917` | 全是 `DoMenuActions`（教程 stage 的**同名不同物**） | ❌ 无关 |

⇒ **#4① 记成改动请求**（见 §六·2），⛔ 没自己造一个 `MenuActions`。

### 4·4 `ResetEvents` 的影响面 —— **0 个调用方依赖「清空」**

`grep -rn "ResetEvents" Assets/` 只有 4 行、1 个调用点：

```
Shell/LiveOpsManager.cs:127   （doc）
Shell/LiveOpsManager.cs:136   concrete.ResetEvents();      ← 唯一调用方
Shell/LiveOpsHandler.cs:401/403/413  （本方法的 doc + 声明 + 旧日志）
```

而 `LiveOpsManager.ResetHandlers()`（`:128`）**自己 0 个调用方**（全仓 `grep "ResetHandlers"` 只有定义 + 注释）。
⇒ **#5 的风险从「中高」降为「实际为零」**；⚠️ 但有一条**连带语义变化**要记住（§六·3）。

### 4·5 13 份活动 SO 的真值（本轮为了 `#1`/`#7` 现读，判据 = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/*.json`）

| eventId | enabled | uploadToLive | **priority** | showInMainMenu | **hasDuration** | **eventDuration** |
|---|---|---|---|---|---|---|
| DarkZealots | 0 | 0 | 0 | 0 | 0 | 24 |
| ExpansionOrks1 | 1 | 1 | **3** | 0 | 0 | 24 |
| ExpansionOrks1_Premium_Item_Offer | 0 | 0 | 0 | 0 | 0 | 24 |
| ExpansionOrks1Scoring | 1 | 1 | **5** | 0 | 0 | 24 |
| ExpansionUM1 | 1 | 1 | **42** | **1** | 0 | 24 |
| ExpansionUM1_Premium_Item_Offer | 1 | 1 | 0 | 0 | 0 | 24 |
| ExpansionUM1Scoring | 1 | 1 | **5** | 0 | 0 | 24 |
| ECRelease | 1 | 1 | **3** | 0 | 0 | 24 |
| ECRelease_Premium_Item_Offer | 1 | 1 | 0 | 0 | 0 | 24 |
| ECReleaseScoring | 1 | 1 | **5** | 0 | 0 | 24 |
| SWRelease | 1 | 1 | **42** | **1** | 0 | 24 |
| SWRelease_Premium_Item_Offer | 1 | 1 | 0 | 0 | 0 | 24 |
| SWReleaseScoring | 1 | 1 | **5** | 0 | 0 | 24 |

- `hasDuration` 取值集合 = **{0}**（13/13）· `eventDuration` 取值集合 = **{24}**（13/13）· `priority` 取值集合 = **{0,3,5,42}**。
- ⇒ **#7 落地后，本地 13 个事件的 `Initialize()` 全部在第一道门就 return**（不排期）。这是**原版同义**
  （不是我们新增的缺口）；改动前是 `TryScheduleHandlerRefresh(0)` ⇒ 打一行「already past」再丢弃 —— 所以**只是少了一行日志**。

### 4·6 可达性（如实写）

| 面 | 现状 | 本批玩家可见变化 |
|---|---|---|
| 13 份活动 SO | **全部过期** + `EVENT` 导航钮已隐藏 | **0** |
| `#7` `Initialize` 排期 | 13/13 `hasDuration=0` ⇒ 不排期（原版同义） | 0（只少一行日志） |
| `#1` 排序 | `priority` 未搬进 `LiveOpsLocalEvent` ⇒ `PriorityOf` 全 0 ⇒ 稳定排序 = 原输入序 | **0**（要见效需 §六·1） |
| `#2`/`#3` `NextRefresh` | 本地 `shared` 恒为 `LiveOpsJson.NewObject()`（服务器载荷 `handlerData` 恒 null）⇒ **永远没有 `NextRefresh` 键** | 0 |
| `#6` `UpdateCachedSave` | `handlerCache` 离线恒 null ⇒ 提前返回 | 0 |
| `#4②` blockedEvents Dispose | `LoadFromCatalog` 传的是**空 blocked 列表** | 0 |
| `#5` `ResetEvents` | 唯一调用方自己 0 调用 | 0 |
| `#9` `UpdateData` | 全仓 `UpdateData(` 外部调用点 **0 个** | 0 |

⇒ **别为了「看得见」去改导航钮**（简报红线）。本批是**口径订正**，不是可见功能。

---

## §五 没查清（写清「还差什么」）

1. **`#4①` 的前置件 `MenuActions` 本体**：原版 `MenuActions` 需要一个**队列 + `QueueActionWithCallback(...)` + 静态
   `ReleaseAction(Action)`**。本轮的判据只覆盖了 `ReleaseAction` 的语义（`actions.Remove(action)`），
   **队列本体（去重规则、槽位、`MenuFilter`、recurring 那一路）尚未查**。
   ⇒ **还差**：C1-W2 开工时先派一条判据代理把 `MenuActions` 整类反汇编一遍（`dump.cs` 找 TypeDefIndex → `script.json` 拿地址）。
2. **`ShowTriggerEventPopup`（`0x181019560`）没逐条反汇编**（#11 只指路）。
3. **`EventFilter.GetExpirationTime(ILiveOps)`（`0x1808AAE90`）没读**：R7 §五·7 说它是 `Initialize` 算式的
   「验收尺子」，本轮没取它的完整指令 ⇒ **「两条算式逐字相等」这个断言现在还不能写成自检**。
4. **`RawEvent.Data` 与 `RawEvent.Persistence` 在实际链路里会不会分叉**：`#6` 改成比 `Persistence` 是照判据做的，
   但离线链路 `handlerCache` 恒 null ⇒ **这条改动在本地一次也执行不到**，**无法用运行结果证明**。
   ⇒ 只能等服务器载荷那条链（C1-W2/W4）通了再验。
5. **`EventFilter` 其余字段的数据来源**：本地 13 份 SO 的 `filter.army` / `expansion` / `eventGroups` **有值**，
   但我们的目录层没抄 ⇒ 将来 `CheckEventFilter` / `QueueEventPopup`（按 group 去重）**都没有数据可读**。

---

## §六 顺手发现（不修，交调度台分流）

1. 🔴 **`priority` / `showInMainMenu` / `location` 这三格「SO 有、我们没抄」—— 现在有**真值表**了**（§4·5）。
   影响：**`#1` 的排序键落地后是空转**（全 0）；`GetEventPriorityForMainMenu`（账 row 332）也还差这格。
   ⇒ **建议**：在 `Shell/LiveOpsLocalCatalog.cs` 的 `LiveOpsLocalEvent` 上补 `Priority` / `ShowInMainMenu`
   （`location` 是含 3 层 PPtr 的复合结构，可后做），并在 `LiveOpsHandler.MapData` 里映射。
   ⚠️ **不在我白名单**（`LiveOpsLocalCatalog.cs` 是别人的），所以只记不做。判据 = §4·5 那张表 + 逐份 SO 路径。
2. 🔴 **`#4①` 改动请求**（落到 `账/日常_奖励.tsv` row 381 的差异列即可）：
   「`ClearEvents` 少了逐个 `MenuActions.ReleaseAction(a)`；**前置件 = 先复刻 `MenuActions` 队列本体**；
   当前 `handlerActions` 的值恒 `null`，所以**即使接上也无可撤**」。
3. ⚠️ **`LiveOpsManager.ResetHandlers()` 的语义连带变化**：`ResetEvents` 订正后，它从「清空列表」变成
   **「逐个 `ResetEventProgress()`」** ⇒ `CreateNewSave` + `SaveEventProgress` ⇒ **会写 `SaveCenter`（每事件一份新档）**。
   它现在 **0 调用方**，所以暂无影响；将来若把它接到「重置活动」入口，**要知道它会写盘**。
4. 🔴 **`LiveOpsScheduler` 仍然没有生产接线点**：`grep -rn "LiveOpsScheduler" Assets/` 除本文件外**只有
   `LiveOpsManager.cs:349` 的 `ResetForTest()`** ⇒ **没有任何地方调 `Tick(utc)`** ⇒ 排出来的刷新**永远不会触发**
   （`LiveOpsHandler.cs` 文件头已如实写「本批还没有接线点」，本批没改变这一点）。
   叠加 `#7` 后本地更不会排期 ⇒ 整条「到点刷新」链路目前是**休眠**状态，属 C1-W2 的接线项。
5. 🟡 **`LiveOpsEventData` 没有 `drawData`（`EventElements`）**：`QueueEventPopup` 要读
   `drawData.ForceRun`（+0x49）与 `ShowWindowOnTrigger/Expire`（+0x20/+0x28）。
   ⇒ **W2 开工前必须先把 `EventElements` 的最小等价物建出来**，否则弹窗排期没有判据输入。
6. 🟢 **判据确认（不是缺陷，写下来免得下轮再查）**：原版 `ILiveOps` 有 `Priority` 属性（slot 21），
   我们的 `ILiveOps` 接口**没有**这个成员；但 `LiveOpsEvent.Priority => GetBaseData().priority` 已经等价，
   `PriorityOf()` 走 `GetBaseData().priority` ⇒ **不需要给接口加成员**（加了反而多一份要维护的口径）。
7. 🟡 **观察（不是结论）**：`#7` 之后 `Initialize()` 的第一道门是 `hasDuration`，而**本地 13 份 SO 全是 0**
   ⇒ 「活动到期时刻」这条算式在本地**完全没有被走到过**。R7 §五·7 想拿它当验收尺子，
   **前提是先有一个 `hasDuration=1` 的事件**（本地没有，需要造夹具或在 C1-W4 解出正式活动数据）。

---

## §七 交接给下一个代理

**做完了什么**

- 12 条全部处置：实改代码 **8 处**（#1 #2 #3 #4② #5 #6 #7 #9）、注释 **4 处**（#8 #10 #12 + #11 指路）、
  改动请求 **1 条**（#4① 依赖 `MenuActions`）。
- 新增两个东西：`LiveOpsEventFilter`（最小等价物）+ `LiveOpsEventData.filter` 字段；`PriorityOf()` 静态助手（在类尾）。
- 类型检查 **0/0**；行尾纯 LF、无裸 CR。

**文件动到哪**

- 改：`Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsHandler.cs`（705 → **777** 行）
       `Unity/MyGame/Assets/CardPresentation/Shell/LiveOpsEvent.cs`（526 → **575** 行）
- 新增：本报告。

**⚠️ 给 R1（账本）的行号漂移表 —— 我只读账，没改账**

`账/日常_奖励.tsv` 里 `Shell/LiveOpsHandler.cs` / `Shell/LiveOpsEvent.cs` 的落点全部偏移了。
`python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/日常_奖励.tsv`
已确认 11 条「漂移」+ 3 条「歧义（2 处同名声明，不自动改）」。**旧 → 新**：

| 类.方法 | 账里旧行 | 新行 | 类型 |
|---|---|---|---|
| `LiveOpsHandler.SetupEvents`（三参） | 188 | **191** | 漂移 |
| `LiveOpsHandler.DeserializeSharedData` | 216 | **228** | 漂移 |
| `LiveOpsHandler.RefreshHandler()` | 251 | **274** | 漂移 |
| `LiveOpsHandler.RefreshHandlerLocally` | 266 | **289** | 漂移 |
| `LiveOpsHandler.RefreshHandler(data)` | 276 | **299** | 漂移 |
| `LiveOpsHandler.HandleBeforeRefresh` | 304 | **327** | 漂移（**A 档**） |
| `LiveOpsHandler.TryScheduleHandlerRefresh` | 328 | **351** | 漂移（**A 档**） |
| `LiveOpsHandler.ScheduleHandlerRefresh` | 352 | **375** | 漂移（**A 档**） |
| `LiveOpsHandler.InitializeHandler` | 371 | **396** | 漂移（**A 档**） |
| `LiveOpsHandler.ClearEvents` | 387 | **423** | 漂移 |
| `LiveOpsHandler.ResetEvents` | 403 | **456** | 漂移 |
| `LiveOpsHandler.GetEvents`（若账里有） | 419 | **476** | 漂移 |
| `LiveOpsHandler.GetBaseEvents`（若账里有） | 440 | **497** | 漂移 |
| `LiveOpsHandler.GetBlockedEvents` | 446 | **503** | 漂移（**A 档**） |
| `LiveOpsHandler.GetEvent` | 450 | **507** | 漂移 |
| `LiveOpsHandler.UpdateCachedSave` | 470 | **534** | 漂移（**A 档**） |
| `LiveOpsHandler.RegisterManager` | 485 | **549** | 漂移（**A 档**） |
| `LiveOpsHandler.ValidateData` | 489 | **553** | 漂移（**A 档**） |
| `LiveOpsEvent.GetBaseSave` | 287 | **312** | **歧义**（同名 2 处：257 抽象 / 312 实现） |
| `LiveOpsEvent.GetEndDate` | 292 | **317** | 漂移 |
| `LiveOpsEvent.Initialize` | 304 | **337** | 漂移 |
| `LiveOpsEvent.LoadEventData` | 325 | **360** | 漂移 |
| `LiveOpsEvent.ValidateEventData` | 356 | **391** | 漂移 |
| `LiveOpsEvent.CreateNewSave` | 380 | **422** | 漂移 |
| `LiveOpsEvent.UpdateData` | 395 | **443** | 漂移 |
| `LiveOpsEvent.UpdateSaveGame` | 412 | **461** | **歧义**（同名 2 处：262 抽象 / 461 实现） |
| `LiveOpsEvent.ResetEventProgress` | 426 | **475** | **歧义**（同名 2 处：263 抽象 / 475 实现） |

⚠️ 那 3 条「歧义」`--fix` **不会自动改**，要人工确认「指向**实现**那一行」（= 上表的右列）。

**下一步（按顺序）**

1. **R1**：按上表更新 `账/日常_奖励.tsv`；顺带把 row 381（`ClearEvents`）的差异列改成「① 未接 `MenuActions`（前置件见 R12 报告 §六·2）；② blockedEvents Dispose ✅ 已补」，
   row 397（`ResetEvents`）差异列改成「✅ 语义已订正：只 `ResetEventProgress()`，不清空/不 Dispose」，
   row 322（`CreateNewSave`）/ row 339（`Initialize`）的「未坐实」改成「**已坐实**（判据 → `判据/LiveOpsHandler_11方法_第2轮.md` §二 Q1/Q2）」。
2. **调度台**：把 §六·1（`priority`/`showInMainMenu` 没搬）、§六·2（`MenuActions` 改动请求）、
   §六·4（`LiveOpsScheduler` 无 Tick 接线）、§六·5（`EventElements` 未建）四件事落账/派活。
3. **C1-W2 开工时**：先派一条**只读判据代理**把 `MenuActions` 整类反汇编一遍（§五·1），再动 `handlerActions` 的类型；
   否则 `QueueEventPopup` 写不出来（`handlerActions` 现在装的是 `System.Action`，原版装的是 `MenuActions.Action`）。

**踩过的坑（省下一轮）**

- 🔴 **`ResetEvents` 名字骗人**：像「Reset」就以为要清空 —— 实际**只对每个事件调 `ResetEventProgress()`**，列表原样保留。
  这条已经写进方法的 doc 里，下次别再照名字改。
- 🔴 **`#7` 不是「改一行算式」**：我们**没有 `EventFilter` 类**，`LiveOpsEventData` 也没有 `filter` 字段。
  简报只说「先 `if (!Data.filter.hasDuration) return;`」，照着写会**编不过**；必须先补最小等价物（本轮已补）。
- 🔴 **改前先算「这条改动本地会不会被执行到」**：本批 7 条实改里，**4 条在离线链路上一行都跑不到**
  （`#2`/`#3`/`#6`/`#4②`）。判据对 ≠ 可验，报告里要分开写（§4·6）。
- 🟡 **类型检查会被别人在写的文件污染**（本批撞了两次：`LegacySaveReader.cs` / `AiScoring.cs`）。
  判据：**错误全落在别人的文件里 ⇒ 那不是你的问题**，隔一会儿重跑；⛔ 别去改别人的文件。
