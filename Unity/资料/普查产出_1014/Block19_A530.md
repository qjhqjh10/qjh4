# Block19 · A530 —— A410 的**联机 / 重连两档措辞**，落到联机自检里（`Editor/NetBattleTest.cs`）

> 写手：Block19（一件活一个代理）· 2026-10-13
> 独占文件 = `Assets/CardPresentation/Editor/NetBattleTest.cs`（**唯一动过的文件**，`git diff --numstat` = **+133 / −0**）
> ⛔ `Editor/NetSelfTest.cs` **一个字没动**（本件不需要它）· ⛔ 没碰 `Net/*`、`BattleDriver` 等生产文件
> 判据原文 = `资料/普查产出_1013/批次计划_1013.md` §六·B **A530**（该文件 `:217`）+ W-B1 交件报告 `WB1_战斗驱动三笔.md` §五·2
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读的 `git status` / `git diff` / `git show` / `git diff --numstat`）
> ⛔ 没改 `CLAUDE.md` / `项目任务.md` / 任何 `资料/*.md`（本报告除外）· ⛔ 没越白名单
> ✅ 类型检查：**改完立刻跑，共 3 次，全 0**（`TMPDIR=/tmp/wf_b19 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**）
> 行尾：改前 `git show HEAD:… | file -b -` = 纯 LF（按字节数过：`CRLF 0 / LF 470`），改后 `CRLF 0 / LF 603` ⇒ **没翻行尾**；全程用 Edit 工具

---

## 一、结论

**做了。** A410 那句日志的**三档**里，`Editor/BattleScene.cs` 的 A410 段只跑得到**档③（放录像）**；
本件在 `NetBattleTest.Run` 末尾新开 §8 一段（**9 条断言**），把**档①（联机开局）/ 档②（重连重建）**两档
**按产品入口**验起来 —— 而且两趟都不是「复述逻辑」，是**真调那两个入口**：

| 档 | 本件怎么走到的（**产品入口**） | 判据 |
|---|---|---|
| **档①** 联机开局 | `NetPendingBattle.Current = FromStart(...)` → **`probe.BeginFromDeckLibrary()`**（它的联机那一支在**方法最前面**：`Take()` ≠ null ⇒ 整条走开局包、立刻 `return`） | `[Net] 联机开局：` |
| **档②** 重连重建 | `probe.AttachNet(nb)` → **`probe.NetReplay(start, actions)`**（生产里 `NetBattle` 收到 `resume` 时调的正是它） | `[Net] 联机重建（重连）：` |
| 档③ 放录像 | **本件不重复验** —— `Editor/BattleScene.cs` 的 A410 段已经验了「不是联机开局」+「是回放开局」（还带指纹对账）⇒ 同一条规矩不写第二份 | —— |

**账目口径**：`清单_A表全量.md` 把 A530 的宿主写成 `Editor/NetBattleTest.cs` —— **核对成立**（见 §四·1 的**前提查证**：
那句日志在 `BattleDriver` 里，本自检的 `BareHost` 走的是另一条路、压根碰不到驱动 ⇒ 只能靠本文件**自己造一台裸驱动**）。

---

## 二、改了什么（`Assets/CardPresentation/Editor/NetBattleTest.cs`，行号 = 本件收工**现读**，会漂）

| 落点 | 内容 |
|---|---|
| **`:360-449`** | **§8 新增**：A530 那两档的成套断言（档① 5 条 + 档② 4 条）+ 一台裸 `BattleDriver` 探针（`GameObject("A530_OpeningLabelProbe")`），`finally` 里 `AttachNet(null)` + `DestroyImmediate` 收摊 |
| **`:556-570`** | **新增** `CaptureLogs(Action, out Exception)` —— 本仓现成的抓法（`Application.logMessageReceived`，同 `Editor/BattleScene.cs` 的 A410 段 / `Editor/SettingsScene.cs` 的 `CaptureErrors`）。**异常不外抛**，走 `out` 交给调用方；钩子在 `finally` 里摘 |
| **`:572-582`** | **新增** `HasLog(lines, prefix)` —— `StartsWith`（**不是** `Contains`）。`Contains` 会被别的句子里顺带提到的词命中 ⇒ 那样写「改坏了也可能照样绿」 |
| **`:584-596`** | **新增** `NoteProbeThrow(Exception, string)` —— 那两趟**预期**会在 `Begin(...)` 里中断，**如实打出来但不当失败**（理由见 §四·2） |

⛔ **生产文件一个字没碰**（下面 §五·1 那条要调度台转派的例外也不是「不改不行」）。

---

## 三、断言清单（**9 条**：判据 4 条 · 夹具/前提 5 条）

**怎么抓**：两趟调用各用 `CaptureLogs` 圈起来（`try/finally` 摘钩子），只认**行首**的标签。

### 档① —— `NetBattleTest.cs:399-420`（走 `BeginFromDeckLibrary()`）

| # | 种类 | 断什么 | 改坏法（必须红） |
|---|---|---|---|
| ① | 夹具 | `FromStart(...)` 非 null | 造不出开局包 ⇒ 红（**这一格同时是「没落到读玩家卡组那条路」的前半**，见下条） |
| ② | 夹具 | 起手 `probe.AiShouldDriveOpponent` = true（= **`_net == null`** 的**公开证人**） | ——（它让下面 ④ 变成**能分状态**的：`_net == null` 时「联机开局」只可能来自 `attachNet` 那一支） |
| ③ | 夹具 | 抓到的日志里**没有** `[Battle] 本局模式：` | 开局包没摆进去 ⇒ `BeginFromDeckLibrary` 会**落到 `PickSavedDeck()` 去读玩家的真卡组**（那一支才会打这句）⇒ 自检**不该碰玩家的真卡组** |
| ④ | **判据** | 有 `[Net] 联机开局：`（**全角冒号**，逐字与生产标签比过） | 把日志删掉 ⇒ 红（红线：不许静默失败） |
| ⑤ | **判据** | **没有** `[Net] 联机重建` 且**没有** `[Replay] 回放开局` | 三档互斥写坏 ⇒ 红 |

### 档② —— `NetBattleTest.cs:422-441`（走 `NetReplay()`）

| # | 种类 | 断什么 | 改坏法（必须红） |
|---|---|---|---|
| ⑥ | 前提 | 挂上联机层后 `!probe.AiShouldDriveOpponent`（= **`_net != null`**）—— **这一格正是档② 与档③ 的唯一分界** | —— |
| ⑦ | 前提 | 有 `[Net] 重连重放：`（`NetReplay` 自己那句）⇒ 证明**走到的确实是重连那条入口** | —— |
| ⑧ | **判据** | 有 `[Net] 联机重建（重连）：` | 二值分流 ⇒ **红**；把档② 也写成「联机开局」⇒ **红**；删日志 ⇒ **红** |
| ⑨ | **判据** | **没有** `[Net] 联机开局：` 且**没有** `[Replay] 回放开局` | 🧨 **后者就是 A410 报告点名的那条陷阱**：只按 `attachNet` 二值分流 ⇒ 重连被说成「放录像」（`NetReplay` 传的也是 `attachNet: false`）⇒ **红** |

**「灭自证」自检**（本工程那条红线）：④⑧ 的期望值**不是我算出来的数**，是**驱动源码里那两个字符串字面量**；
我用脚本把**生产侧 `kindNet` 三段字面量 + 全角冒号**与**本文件里的断言前缀**逐字比过（脚本比对 ⇒ 两组都命中）。
⑤⑨ 是**反向**断言（「没说成另外两档」）—— 光有正向那一条的话，把标签**改成恒真**也可能绿。

**「弱断言/空转」自检**：①②③⑥⑦ 都是**夹具前提**，**不当判据**用；它们要钉的是「这一趟真的走了那条路」。
③ 是其中唯一**有牙**的（它咬「有没有掉进读玩家卡组那条路」）。
⚠️ 如实标一处：②⑥ 读的是**刚 `AddComponent` 出来的对象**（`_net` 只被 `AttachNet` 写），
**单独看接近恒真** ⇒ 它的价值是「让 ④ 成为**能分状态**的断言」的**前提记录**，不是独立判据。

---

## 四、两处必须说清的取舍（审查代理多半会问）

### 1. 为什么是「裸驱动 + 预期中断」，而不是把整台战场建起来

- **那句日志在 `BattleDriver.BeginFromPendingCore` 里**（现读 `Battle/BattleDriver.cs:1786` 的 `kindNet` 三段 + 紧接着的 `Debug.Log`），
  而本自检原有的 `BareHost` 走的是**另一条路**（裸 `BattleContext`）⇒ **同一个文件里既有的联机链路一条都碰不到它**。
- **建一台完整驱动**（`interaction` / 棋盘 / 手牌 / 两台相机 …）是 `Editor/BattleScene.cs` 的活，
  而那条自检**实测 7m56s**（铁律 12 那张表）⇒ **不能为了两句措辞把它拖进来**；
  `Editor/BattleScene.cs` 也是 A556 记的**全局瓶颈宿主**（同一时刻只能一个写手）。
- 🔴 **关键事实**：**那句日志在 `Begin(...)` 【之前】就打出来了** —— 源码顺序是
  `string kindNet = …` → `Debug.Log($"{kindNet}：…")` → `Begin(...)`。
  而裸驱动没有 `interaction`（`Begin` 里**紧跟 `BuildHud()` 之后**那几处 `interaction.…` ——
  `CanDropAtSlot = …` / `DropLandingSlot = …` / `OnDropPreview -= …` —— **都没有守卫**）
  ⇒ `Begin` 会中断在那一处。
  **本段要的只是那句日志，它已经在手上** ⇒ **那个异常不当失败**，但**如实打出来**（`NoteProbeThrow`，红线：不许静默）。
  ⚠️ **反过来说**：要是哪天真不炸了（有人补了守卫 / 真的建起了装置），`NoteProbeThrow` 也**照实说**（那更好，不是问题）。
- **收摊干净**：两趟跑完 `AttachNet(null)` ⇒ `OnDestroy` 只走 `DetachStaticHooks()` 那一支
  （**不走**「离开战场」那条 ⇒ 不会去碰 `NetRuntime` / `NetMatchmaking`），再 `DestroyImmediate`（批处理下 `Destroy` 不生效）。
  本段**放在 §7 负例之后**（`NetBattleTest.cs` 最末），所以它那点全局副作用落不到别的节身上。

### 2. 「前提」查证过（这一条 A530 的原报告只是**猜**宿主，我核了）

| 问 | 答（都是现读，不是推断） |
|---|---|
| 档② 的分界 `_net != null` 稳不稳？ | **稳**。`BattleDriver` 里 `_net` **只有 `AttachNet` 一个写点**（全文件按 `_net =`（带尾空格）grep，只命中 `:184` 的定义行），生产路径**从不清回 null** —— 唯一的例外是**自检探针**（`Editor/BattleScene.cs` 的 `driver.AttachNet(null)`）。⚠️ 这条**以前没被记过**（W-B1 报告只写「没有任何地方把它置回 null」，**我核了：成立**，并补上那个唯一例外） |
| 档① 能不能真走到产品入口？ | **能**。`BeginFromDeckLibrary` 的联机那一支在**方法最前面**（`SetMySeat(0)` → `NetPendingBattle.Take()` → `if (pbNet != null) { BeginFromPendingCore(pbNet, attachNet: true, …); return; }`）⇒ 挨不到后面那条读玩家卡组的路 |
| 抓日志这套在批处理下行不行？ | 本仓既有范式（`Editor/BattleScene.cs` 的 A410 段 / `Editor/SettingsScene.cs` 的 `CaptureErrors` / `CollectionScene.cs:2189` 的注释把它称作「本仓现成的『出声』断言范式」）。⚠️ **本件没跑 Unity ⇒ 这是「沿用既有范式」，不是我的实测** |

---

## 五、顺手发现（⛔ 只报不改）

1. 🟡 **`BattleDriver` 没有 `_net` 的只读口** —— 外面要判「联机层挂没挂」只能拿 `AiShouldDriveOpponent`（= `_net == null`）当**反向证人**
   （`Editor/BattleScene.cs` 那条联机探针也是这么用的）⇒ 语义要**绕一下**读，容易读反。
   建议将来加一个 `public bool HasNet { get { return _net != null; } }` 一类的口（**本件没加**：动点在白名单外）。
2. 🔴 **有另一个写手正在改 `Battle/BattleDriver.cs`**（就是 `Block12_BattleDriver族.md` 那位，A531/A651/A659/A660）：
   本件作业期间它的 mtime 变过、我引用的行号**当场漂了 13 行**（`interaction.OnDropPreview` 那处 2231 → 2244）、
   `git diff --numstat` = **152 / 23**；收工前它**又变了一次**（`md5`：`a5a9266…` → `e425126…`，两次相隔约 3 分钟）
   ⇒ **它还在飞**，同步点要按「在飞」处理。⇒ 我在注释与报告里**一律不钉死 driver 侧行号**（只写「物证片段 + 现读值」）。
   **它没碰 `kindNet`**（grep 零命中，收工前又核了一遍：`:1786-1788` 三段字面量原样在位）⇒ **不构成撞车**。
3. 🟡 **`Begin` 里那几处 `interaction.…` 无守卫**（紧跟 `BuildHud()`：`CanDropAtSlot = …` / `DropLandingSlot = …` / `OnDropPreview -= …`）
   —— 本件把它**当特性用**（正是靠它在 `Begin` 之前拿到日志）。⚠️ 若将来有人想让 `Begin` 在「没有装置」时也安全，
   那要**另开一笔账**（⛔ 别顺手把守卫加上 —— 那会让本段的两趟**真的跑完 `Begin`**，日志照样在，但副作用面大得多）。
4. 🟢 **本件没有发现新的实现缺陷**。

---

## 六、没做完的（+ 为什么）

1. ⚠️ **没跑 Unity**（简报红线）⇒ 这 9 条断言**只做了静态核对**。**预期**：改前全绿（A410 的改动已在库里，
   `Battle/BattleDriver.cs:1786` 的三段标签现读在位）；把标签改回二值分流 ⇒ 档② 的 ⑧⑨ **必红**。
   ⚠️ **这是预测、不是实测** —— 按铁律 12 攒批，**由调度台在同步点跑 `NetBattleTest.Run`**。
   🔴 **万一红了，第一件事是分清是哪一类**：(α) 断言写错 / (β) 实现缺陷 / (γ) 本批回归 / (δ) **夹具前提不成立** ——
   本件最可能的是 **(δ)**：两个候选前提 —— ① 批处理下 `Application.logMessageReceived` 不触发（那就**档①② 一起红**，
   而不是只有一条）；② `Begin` 的中断点**跑到了日志之前**（不可能：日志在 `Begin` 调用**之前**那一行；
   但若有人挪了那两行的顺序，本段会**因为「日志没了」而红** —— 那本身也是有效的红）。
2. **档③ 没有在本文件重复验**（`Editor/BattleScene.cs` 的 A410 段已覆盖）—— **有意为之**，理由见 §一。
   ⛔ 这不是「不做」，是**不做第二份**（同一条规矩写两处迟早不一致）。
3. **本件没有改任何生产文件**，也没有要求生产侧配合 —— 也就是说：**A530 到此清账**，
   不需要再往 `Editor/BattleScene.cs`（瓶颈宿主）上挂东西。

---

## 七、自检影响面（给调度台排复跑用）

- 动的是**一条自检宿主本身**（`Editor/NetBattleTest.cs`）⇒ 按铁律 12 的判据：**复跑 `NetBattleTest.Run` 这一条**即可。
- ⚠️ 本件**没有**新增对 `Assets/CardPresentation/Net/`、`BattleDriver` 的引用之外的依赖（只读了它们的**公开口**：
  `NetPendingBattle.FromStart` / `NetBattle.Attach` / `BattleDriver.AttachNet` / `BeginFromDeckLibrary` / `NetReplay` / `AiShouldDriveOpponent`）。
  ⇒ **不牵动** `RuleEngineTest` / 其它宿主。
- 新增运行开销：两趟 `BuildHud()`（裸驱动，跑到 `interaction` 那一处就中断）—— 相对 `NetBattleTest.Run` 现有的
  换牌 / 脚本对打 / 两次掉线重连（20s 超时各一处）**属于零头**。
