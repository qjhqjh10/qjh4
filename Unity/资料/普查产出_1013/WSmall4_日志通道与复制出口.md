# WSmall4 · 日志通道统一（A610）+ 复制出口（A611）

> **白名单内动过 3 个文件**：`Shell/CollectionData.cs`（A611 本体 + 出口）·
> `Shell/DeckInfoPopup.cs`（A610 三支 + A611 的调用点）· `Editor/CollectionScene.cs`（**只加我这一节断言**）。
> **没跑 Unity、没动 git、没改两张正本。** 类型检查跑 **1 次** ⇒ **运行时 0 / 编辑器 0**（§七）。
>
> **行尾**（**二进制**读 —— 文本模式会把 `\r\n` 折成 `\n`、两个数恒相等）：
> 三个文件改前改后**全是纯 LF**（`\r\n` 计数恒 `0`）⇒ **一个都没翻**。
> **`git diff --numstat`**（含同批别人未提交的改动，逐项拆账见 §七）：
> `Editor/CollectionScene.cs 574/0` · `Shell/CollectionData.cs 144/17` · `Shell/DeckInfoPopup.cs 34/5`。
>
> 🔴 **先说一条要调度台过目的**：**A610 的「三支」落地后是【两句】** ——
> `OnOption` 里原本三句失败日志 = 删失败 1 句 + **复制失败 2 句**（A548 那个按 `DeckCount()` 分支的写法），
> 而 **A611 的目标正是把那两句并成一句**（打出口的原话）⇒ 两笔账在**同一行**上，
> 合并后只剩一句、通道是 `LogWarning`。**我按「A611 打原话」优先**（这也是简报 §二 的验收标准 2a），
> 若调度台要的是「三句都在、只换级别」，那就与 A611 的验收标准打架 —— 见 §五·6。

---

## 一、结论

| 验收条目 | 做没做 | 一句话 |
|---|---|---|
| A611：给 `DuplicateDeck` 补 `LastDuplicateError`（照 `LastDeleteError` 的形状） | ✅ | `CollectionData.cs:182` 新增出口（清空 → **先判返回值 `null`** 分越界 → 再判 `Lib.LastError` 分没落盘 → 两支各一条 `LogWarning`），**与 A503 的 `LastDeleteError` 逐条同形**；调用点 `DeckInfoPopup.cs:1298` **直接打原话**，A548 那个「按 `DeckCount()` 变没变」的**间接判据已删** |
| A610：`OnOption` 那三支失败 `Debug.Log` ⇒ `LogWarning` | ✅ | 删失败（`DeckInfoPopup.cs:1281`）+ 复制失败（`:1298`，A611 后由 2 句并成 1 句）· ⛔ **成功那两句没动**（仍是 `Debug.Log`）· ⛔ **没扩大**到 `OnButton` / `Blocked` / 别处 |
| A610 **动前的 grep**（谁在数日志 / 会不会被打破） | ✅ | **9 个文件 / 27 处** `logMessageReceived +=`（改前 26 处）逐处看过：**没有一处**按级别计数或按级别断言 —— 要么**完全忽略 `type`**，要么**按内容过滤**（`cond.Contains("…")`），而那些内容前缀**都碰不到** `[DeckInfo] 删卡组失败：` / `[DeckInfo] 复制失败：` ⇒ **不会被打破**（逐处表见 §三·一） |
| 断言（35 条）：**分得清复制失败两支** · 有改坏法 · 不自证 | ✅（**未跑**） | `Editor/CollectionScene.cs:5451-5647` 新增一节 **35 条**：**控制组（好路径）+ 两个探针（越界 / 写不进去）成对**，两支各配**互斥的否定断言**；A610 那几条**断的就是日志级别**（`LogType`）。⚠️ **一条都没跑**（Unity 全局串行、只有主对话能跑）⇒ §五·1 |
| `TMPDIR=/tmp/wf_wsmall4 typecheck` **0 错** | ✅ | **0/0**（**没有**「错误集中在别人文件上」那种情况）⇒ §七 |

**判据来源**：① `资料/普查产出_1013/WSmall3_选中断言与两条尾巴.md` §六·2（A610）/ §六·3（A611 的出处）·
② `资料/普查产出_1013/WSmall1_Deck两尾巴.md` §五·5（A611 的原始出处）·
③ **`Shell/CollectionData.cs` 现读的 `LastDeleteError` `:145` + `DeleteDeck` `:156-167`**（A503 的成品 = 形状范本）·
④ `RuleEngine/Data/DeckLibrary.cs` 的 `Duplicate:105-115` / `Save():155-161` / `SaveOrWarn():180-188`。
**没走原版反编译**：原版卡组存在**服务器**上（`CardDeck.syncedToServer` / `deckId`，见 `DeckLibrary.cs` 文件头）
⇒ **本地这条链没有原版对应物**；本笔判据 = 本项目红线「⛔ 不许静默失败」+ **A503 已经定下的那套口径**
（A610 的「失败=警告级」也是**本仓口径**，⛔ 不是原版判据）。⛔ **没另立一套**。

---

## 二、A611 改动（出口形状 · 照的是 `LastDeleteError` 的哪几行 · 调用点改了哪两处）

**落点（改完后现读行号）**：`Shell/CollectionData.cs` 的 `LastDuplicateError` `:182` ·
`DuplicateDeck` `:190-213`（清 `:192` · 取 `before` `:193` · 越界支 `:195-200` · 没落盘支 `:201-207` · `return d.Name` `:208`）。

### 二·一 逐条对齐范本（`DeleteDeck`，A503 的成品）

| 形状要素 | 范本（`DeleteDeck` `:156-167`） | 我这一半（`DuplicateDeck` `:190-213`） |
|---|---|---|
| 出口 | `public static string LastDeleteError { get; private set; } = "";`（`:145`） | **逐字同形** `LastDuplicateError`（`:182`） |
| 开头清 | `LastDeleteError = "";` | 同 |
| 分支判据 | **观测状态**：`Lib.Decks.Count` 变没变（⛔ 不读 `LastError`，`DeckLibrary.Delete` 越界那条**早退且不清 `LastError`**） | ★ **一处差别**：**先判 `DeckLibrary.Duplicate` 自己的返回值 `d == null`**（见二·二·1）—— 同一个「早退不清 `LastError`」的坑靠**次序**躲开 |
| 两支文案 | `"删了，但**没写进存档**：" + SaveFailReason() + "（…重启它又回来）"` / `"**没删** —— 下标 N 越界（库里现在 M 套）"` | `"复制出来了，但**没写进存档**：" + SaveFailReason() + "（内存里已经多了一套、盘上还在 ⇒ 重启它就没了）"` / `"**没复制** —— 下标 N 越界（库里现在 M 套）"` |
| 出声 | 一支共用末尾一条 `Debug.LogWarning("[CollectionData] 删卡组失败：" + LastDeleteError)` | 同前缀同形 `"[CollectionData] 复制卡组失败：" + LastDuplicateError`（写在两支里，因为中间夹着 `return`） |
| 兜底措辞 | 复用已有的 `SaveFailReason()`（`:231`） | **复用同一个 `SaveFailReason()`**（⛔ 没新写文案） |
| 返回值 | `bool`（内存删掉 **且** 落盘成功）；失败时**是哪一种**看 `LastDeleteError` | **契约不变**：仍是「新卡组名 / 空串」；失败时**是哪一种**看 `LastDuplicateError` |

⚠️ **没有按 `Select`/`CreateDeck`/`ImportDeck` 那几处的形状改**：那几处是「**返回值装不下落盘成败**」
⇒ 所以 A611 **没有**去改 `DuplicateDeck` 的返回类型（那会打断 `CollectionScene.cs:5153/5177` 等既有调用点与断言），
只**加**一条出口 —— 与 A600 对 `Select` 的处置**同一个思路**（加出口 ≠ 改契约）。

### 二·二 两处与范本**不同**的地方（如实交代，都不是「另立一套」）

1. 🔴 **判据用「返回值 `d == null`」，不是「`Decks.Count` 变没变」** —— 理由是 `DeckLibrary.Duplicate`
   的返回值**忠实**：越界时 `:107` 早退 `return null`，**不可能**「返回了对象却没插进列表」。
   ⛔ 但**次序是硬要求**：必须先判 `d == null` **再**读 `Lib.LastError` ——
   因为越界那条**不清 `LastError`**（与 `Delete` 同形），先读会拿到**上一次**的旧值、
   把「越界」误报成「没落盘」。这一点我写进了 `<summary>`（`:184-189`），并**没有**去改 `DeckLibrary`。
   ⚠️ 仍取了 `before = Lib.Decks.Count`，**只用于文案里的「库里现在 M 套」**，**不参与分支**。
2. **出声前缀把卡组名丢了**（如实在此记）：原来那条警告是
   `[CollectionData] 复制卡组「<新名>」：内存里**已经有了**，但**没写进存档**：…`，
   现在是 `[CollectionData] 复制卡组失败：复制出来了，但**没写进存档**：…`（照 `LastDeleteError` 的字形）。
   **信息没丢**：① 紧接着上面**本来就有** `[DeckLibrary] Duplicate 的内存改动**已生效**，但**落盘失败**：…`
   （A398 起的 `SaveOrWarn`，它带着原因）；② 副本名本来就是 `源名 + " 副本"`（`DeckLibrary.cs:110` 的 `UniqueName`）。
   ⛔ 若调度台认为名字必须留在这一行，改法 = 把两支的前缀都写成 `"[CollectionData] 复制卡组失败："` 不变、
   在**没落盘那一支**把 `d.Name` 拼进 `LastDuplicateError` —— **一行**，但那就与 `LastDeleteError` 不再逐字同形。

### 二·三 调用点改了哪两处（`Shell/DeckInfoPopup.cs`）

| # | 位置（**现读行号**） | 改前 | 改后 |
|---|---|---|---|
| 1 | `:1288-1298`（`OnOption` 的 `Duplicate` 支） | `int dupBefore = CollectionData.DeckCount();` + `else if (DeckCount() > dupBefore) Log("…没写进存档…")` + `else Log("…越界，**没复制**")` | **删掉那两行分支与 `dupBefore`**，改成一句 `else Debug.LogWarning("[DeckInfo] 复制失败：" + CollectionData.LastDuplicateError);` |
| 2 | `:1290-1297` 的注释块 | 写着「判据 = **调用前后 `DeckCount()` 变没变**」（那曾是**实现**） | **就地订正（铁律 5）**：保留 A548 的来由，补 A611 那句「现在有出口了 ⇒ 打原话，上面那个间接判据**已删**」+ A610 那句「通道统一成 `LogWarning`」—— ⛔ 不留一句「判据 = …」的过期说法（两份说法打架比没有更糟） |

**`DuplicateDeck` 的全部调用点（全仓 grep，一条不漏）**：`Shell/DeckInfoPopup.cs:1288`（本件改了）·
`Editor/CollectionScene.cs:5153` / `:5177`（**W-E4 的 A503 节**，只钉「回名字 / 回空串」⇒
**契约没变、不受影响**，本件**一行没碰**）· `RuleEngine/Data/DeckLibrary.cs:177` 只是**注释**里提到它。

---

## 三、A610 改动（改了哪三支 · 动前的 grep 结果）

**改了三句（实现后 = 两句，见 §五·6）**，全在 `Shell/DeckInfoPopup.cs` 的 `OnOption(string key)` 里：

| # | 位置（**现读行号**） | 改前 | 改后 |
|---|---|---|---|
| ① | `:1281`（`Delete` 支失败） | `Debug.Log("[DeckInfo] 删卡组失败：" + CollectionData.LastDeleteError);` | `Debug.LogWarning(...)` —— **只换通道，正文一字不动** |
| ② | `:1297`（`Duplicate` 支失败 · 没落盘） | `Debug.Log("[DeckInfo] 复制失败：复制出来了，但**没写进存档**（原因见上面那条 `[CollectionData]` 警告）");` | 与 ③ **并成一句** `Debug.LogWarning("[DeckInfo] 复制失败：" + CollectionData.LastDuplicateError);`（A611） |
| ③ | `:1298`（`Duplicate` 支失败 · 越界） | `Debug.Log("[DeckInfo] 复制失败：下标 " + DeckIndex + " 越界，**没复制**");` | 同上（合法输入只剩这一支的出口） |

⛔ **没扩大**：`OnButton` 的 `Select Deck`（`:1191`，A600 加的那句）**没动**；
成功那两句（`:1275` 的「已删除该卡组」· `:1289` 的「已复制成「…」」）**没动**；
`Blocked()`（`:675`，本来就是 `LogWarning`）**没动**；**别的方法一个字没动**。

### 三·一 动前 grep：谁在数日志 / 会不会被打破

**怎么做**：全仓 `grep -rn "logMessageReceived" --include=*.cs MyGame/Assets/`（**读，不改**），
再逐个把 handler 体读出来，判三件事：**① 忽略不忽略 `type` ② 是不是按内容过滤 ③ 有没有按条数计数的断言**。

**全仓共 9 个文件 / 27 处 `+=` 注册点**（改前 26 处 —— 第 27 处是**本节新加的 `hLv`**；⚠️ 与本简报里
「已知 7 个文件 / 23 处」对不上，**我以实测为准**，那个数字大概是更早一轮的口径）：

| 文件 | `+=` 处数 | 会不会被 A610 打破 | 判据 |
|---|---|---|---|
| `Editor/BattleScene.cs` | 8 | ❌ 不会 | 全部**按内容**过滤：`:210` `dwCounter`（DOTween 计数，读正文）· `:2851/2876` 要 `LogType.Warning && cond.Contains("[WaitBanner]")` · `:10758` 要 `LogType.Warning` **且**内容含特定前缀 · 其余只往表里收正文 |
| `Editor/CollectionScene.cs` | 6（改前 5） | ❌ 不会 | `:2124/2129` 要 `t == LogType.Warning && c.Contains("`Txt` 的 `basis`")` · `:2438/2443` 要 `Contains("`HitOn` 的 `basis`")` · `:5236`（A600/A601 的 `ClickHit`）**完全不看 `type`** · **`:5485` = 本件新加的** |
| `Editor/ShellScene.cs` | 4 | ❌ 不会 | `:1935/1964/2067/2096` 收 `Warning ‖ Error`，但断言只 `Exists(m => m.Contains("TryOpen` 早退"))` ⇒ **内容过滤**，与 `[DeckInfo]` 无关 |
| `Editor/RewardsScene.cs` | 3 | ❌ 不会 | 要 `LogType.Warning && cond.Contains("border 比图还大")` / `Contains("[ItemDrawer]")` |
| `Editor/SettingsScene.cs` | 2 | ❌ 不会 | 只收 `Error ‖ Exception` ⇒ 本件把 `Log` 变 `Warning` **更不会被收** |
| `Editor/DeckScene.cs` | 1 | ❌ 不会 | `:3832` `sink` 忽略 `type`、按内容断言（A547 那节） |
| `Editor/ShopScene.cs` | 1 | ❌ 不会 | 要 `LogType.Warning && cond.Contains("没有奖励表")` |
| `Core/ClickLog.cs` | 1 | ⚠️ **会记到级别、但没有断言读它** | `:89-94` 把日志存成 `"[" + type + "] " + msg`；**唯一的断言**在 `ShellScene.cs:3281-3287`，只 `Contains("测试件")` / `Contains("这一条应当出现在")` ⇒ **不读级别前缀**（⚠️ 副作用：真点一下时点击记录里的 `[Log]` 会变成 `[Warning]` —— 那是**更准**，不是缺陷） |
| `WarpforgeArena1/Editor/BundleSmokeTest.cs` | 1 | ❌ 不会 | 只是把 `Error‖Warning‖Exception` **重打一遍**，不做断言、且不碰 `DeckInfoPopup` |

🔑 **最硬的一条判据**：全仓 `Assets/` 里 `LogType.Log` 只出现 **3 次**，**全是我这次新加的**
（`CollectionScene.cs:5533/5550/5585`）⇒ **改前没有任何一处**断言「这条是普通级」，
**A610 的两句在结构上不可能打破既有断言**（它既不改条数、也不改正文，只改级别）。
**结论 = 不会被打破 ⇒ 我做了**（没有「只报不改」的条目）。

---

## 四、断言清单（落点 `Editor/CollectionScene.cs:5451-5647`，宿主 = **`CollectionScene.Run`** 的最末尾）

**落点细节**：整节在 `Run()` **最末尾**（`int total = _pass + _fail;` 之前），`Section(...)` 在 `:5470`。
**它上面**依次是 W-E2 的 A404 段（`:4031-4090`）、W-E4 的 A503 节（`:5135-5212`）、WSmall3 的 A600/A601 两节
（`:5213-5449`）—— **那三块我一行都没碰**（复核：`git diff -U0` 仍然只有两个 hunk，见 §七）。

**两个新 helper（`:5482-5498`）**：`lvMsg` / `lvLevel` 一对表 + `LvCount(mark, contains, ty)` /
`LvFirst(mark, contains)`；`Application.LogCallback hLv` 在**整节**期间挂着（`:5485` 挂 / `:5643` 摘）。
⚠️ 点击仍然复用 WSmall3 那份 `ClickHit`（**只抓正文**）—— 那一份不在本件白名单、**一行没动**；
两份 handler 同时挂着不冲突（`ClickHit` 自己加、自己摘）。

**探针（两态是环境造的，不是抄实现）**：
① **越界** —— 开完窗再把 `DeckIndex` 改成 `DeckCount()+7`（`DeckLibrary.Duplicate` 早退、**根本不碰盘**）⇒
   可以**在好路径上**量，**零副作用**；
② **写不进去** —— `DeckStore.OverridePath` 拐到 `…/__wf_a611_no_such_dir__/x.json`（父目录不存在 ⇒
   `File.WriteAllText` 抛 `DirectoryNotFoundException` ⇒ `SaveAll` 回 false，`error` = `"存档写入失败：" + e.Message`）。
⚠️ **切到坏路径时【不调】`ResetForTest()`**（那会按新路径读成**空库**、连开窗都开不了）——
内存里那份库**要留着**，落盘走的是**调用那一刻**的 `OverridePath`（这一步与 WSmall3 §四·一 同口径）。
🔴 **夹具那条存档一个字节都没被碰过**：控制组把夹具**复制**成 `_wf_a611_probe.json`（同一目录）、
`ResetForTest()` 后所有**成功**的写盘都落在副本上，收尾删副本（WSmall3/WE4 两节是直接在夹具上改完再改回来；
本件用了更硬的做法）。

| # | 断什么（简写） | 怎么分辨两种状态 | 改坏法 ⇒ 红 |
|---|---|---|---|
| 1-2 | （前提）探针目录存在 · 夹具那条存档在 | —— | 目录/文件没了 ⇒ 下面全是假绿（这两条自己先红） |
| 3 | （前提）副本里 **1 ≤ 套数 < 114** | —— | `Duplicate` 那颗钮的 `interactable` 为假 ⇒ 点了不生效 |
| 4-6 | （控制组）好路径 `DuplicateDeck(0)` ⇒ 回**名字** · `LastDuplicateError` **空串** · 库里 **+1** | **好路径 vs 坏路径**在同一节里各断一次 | 实现写成「恒回空串」/「成功也写错误」⇒ 红 |
| 7 | （控制组）……而且**真写进了**探针副本（`File.Exists`） | —— | 好路径其实写不进去 ⇒ 挡住下面几条的假绿 |
| 8 | （前提）那扇窗的 `Duplicate` **可点** | —— | 不可点 ⇒ 下面那一下被 `Blocked` 拦掉、**静默变成「什么都没发生」** |
| 9-10 | （控制组）好路径点 `Duplicate` ⇒ 「已复制成『…』」**恰好一条且级别是 `Log`**、**没有**「复制失败」 · 库里 **+1** | 同一份实现只翻「路径」一个输入 | 成功那句也改成 `LogWarning` ⇒ 红；那一支写成恒报失败 ⇒ 红 |
| 11-13 | ★ **A610**：删失败（越界支）⇒ 恰好一条 `[DeckInfo] 删卡组失败：`**且级别 = `Warning`** · 级别为 `Log` 的**0 条** · 一套没少 | **两种级别互斥**（弱断言分不出） | 退回 `Debug.Log` ⇒ 11+13 一起红 |
| 14-17 | ★ **A611 越界支**：回**空串** · （前提）出口里**真有人话** · 说「**没复制**」且**不含**「没写进存档」 · 库里**一套没多** | **越界 vs 没落盘**两种成因分开断 | 两支合成一句 ⇒ 16 红 —— **那正是原来的缺陷** |
| 18-21 | ★ **A610+A611 调用点（越界支）**：恰好一条**警告级**的「复制失败」· ★ 打的是 `LastDuplicateError` 的**原话** · **不许**再说「已复制成」· 库里一套没多 | 有 vs 没有；两句**互斥** | 退回 A548 那个按 `DeckCount()` 分支的写法（**不含**出口原话）⇒ 19 红；退回 `Debug.Log` ⇒ 18 红 |
| 22-23 | （前提）那条路径**确实写不进去** · 写不进去时**带了原因** | —— | 挡「空串恒真」的假绿 |
| 24-28 | ★ **A611 没落盘支**：回**空串** · 含「**没写进存档**」且**不含**「越界」 · **带上了 `SaveAll` 那条 `catch` 的运行时原话** · 内存里**真的多了一套** · **两支是两句不同的话**（`eRange != eSave`） | **成功 vs 失败**；「随便说一句」vs「把原因说出来」；★ 钉住语义：「失败」专指**没落盘**（内存改了、盘上没改）—— 与 A503 **同一条语义锚** | 把失败做成「回滚掉那一套」⇒ 27 红；原因换成写死的文案 ⇒ 26 红；两支合成一句 ⇒ 28 红 |
| 29-33 | ★ **A610+A611 调用点（没落盘支）**：恰好一条**警告级** · 打的是**原话** · **带上了运行时真原因** · **不含**「越界」 · 内存里确实多了一套 | 同上；★ A548 那一版这里写的是「原因见上面那条 `[CollectionData]` 警告」⇒ **不含**真原因 | 退回旧写法（含 A548 的 `DeckCount()` 分支）⇒ 30/31 一起红 |
| 34-35 | （收尾）`OverridePath` 还回夹具那条 · 探针**副本**删掉了 | —— | 没还原 ⇒ 污染夹具（也会让后一节读成错库） |

**预期读数**：`_pass` **+35**；日志里会多出**故意造的**失败行
（`[CollectionData] 删卡组失败：…` · `[DeckInfo] 删卡组失败：…` · `[CollectionData] 复制卡组失败：…` ×2 ·
`[DeckInfo] 复制失败：…` ×2 · `[DeckLibrary] Duplicate …落盘失败`）—— **那是预期输出，不是自检红**。

### 四·一 为什么**不算自证 / 不是弱断言**（派活必查行三条自查）

- **两条探针是环境造的**（不存在的父目录 / 失效下标），**期望值来自契约**（`DuplicateDeck` 的返回值 +
  A503 那套「两种人话」的口径），**不是抄实现的输出** ⇒ 实现改了它就红。
- **控制组与探针成对**：「恒报失败 / 恒回空串」与「恒报成功 / 恒返名字」两种坏实现**各有对应的一条能红**
  —— 只断失败那一半会被「恒报失败」蒙过（#4-6/#9-10 vs #14-21 / #24-33）。
- **「报的是真原因」那两条（#26 · #31）拿的是运行时值**：`DeckLibrary.Load().Save()` 在坏路径上**独立**跑出来的
  `LastError`（`DirectoryNotFoundException` 的原文），**不是**我们源码里的常量 ⇒ **不是同义反复**。
  ⚠️ 两条 `Contains` 前面都**先钉了「出口里真有人话」**（#15 / #23 —— 空串会让 `Contains("")` **恒真**）。
- **「打原话」那两条（#19 · #30）咬的正是 A611**：它们比的是 `CollectionData.LastDuplicateError`
  （**另一个文件的出口**，运行时才知道的串）⇒ A548 那个按 `DeckCount()` 分支的写法**含不了**它。
- **「两支是两句不同的话」（#16 · #25 · #28）专治原来的形状**：改前那两句**在两种情况下都打**，
  合成一句照样「看着有依据」。
- ★ **灭自证（防「两边一起改回去」）**：把 `CollectionData` 的两支与 `DeckInfoPopup` 那一句**一起改回**旧写法
  ⇒ #16/#19/#25/#28/#30 **一起红**（判据与被打者**不在同一个文件、也不是同一个表达式**）。
  把 `DeckStore.SaveAll` 弄成永远失败 ⇒ 两个控制组（#4-10）全红。
- **A610 那几条（#9 · #11-12 · #18 · #29）断的是日志级别**：期望值（失败=Warning / 成功=Log）来自
  **调度台定的口径**，⛔ 不是原版判据、**也不是**抄实现 —— 它是一对**结构上互斥**的状态
  （同一句话不可能既是 `Log` 又是 `Warning`），**改回 `Debug.Log` 必红**。
- ⚠️ **派活必查行三条**：① **不自证** —— 见上；② **弱断言** —— 每个「两支」都配**两条互斥的否定断言**
  （「不许说没写进存档」/「不许说越界」/「不许说已复制成」/「级别不许是 Log」）；
  ③ **没有**用 `!RectOfUnion` / 「一个 quad 都没有」那种形状 —— 本节的断言全是**日志内容 + 级别 + 计数**，
  与渲染层无关。

---

## 五、没查清 / 没做的（⛔ 不猜、不静默）

1. 🔴 **这 35 条断言【没跑过】** —— 本批口径是「A 表清零前不跑自检 / Unity 全局串行、只有主对话能跑」，
   本件只过了**类型检查**。收口那次 `CollectionScene.Run` 若红，**最可能的五条**（按可能性排）：
   ① **`DeckInfoPopup` 的节点取不到**（`Opt_Duplicate` / `Opt_Delete`）⇒ `pOk.Opt(...)` 给 `null` ⇒
      `ClickHit(null)` **什么都不点** ⇒ #9/#11/#18/#29 全红（它们是其余断言的入口，**先看这四条**）；
   ② **`win.Manager` 在那一刻不可用**（本节的四个 `DeckInfoPopup.Create` 都走它）⇒ 开窗那一串全红；
   ③ **`File.Copy` 失败**（夹具那条不是普通文件 / 被占用）⇒ #3 起全红 —— 我没有别的兜底，**如实记**；
   ④ **`__wf_a611_no_such_dir__` 被谁建出来了**（只可能是别的节或人手建的）⇒ #22 自己红、挡住下面；
   ⑤ **`n0` 越界**（副本里 0 套或 ≥114 套）⇒ #3 红。
   ⚠️ **另一类只有本件会有的**：`LvCount(..., LogType.Log) == 1`（#9）**依赖批处理下 `logMessageReceived`
   的 `type` 参数真的是 `LogType.Log`**。本仓的**先例**：`CollectionScene.cs:2124` 那条
   `t == LogType.Warning` 的断言**早就在 HEAD 里**、近期自检全绿 ⇒ 这条通道在批处理下**确实会触发**、
   且**级别是准的**（不是我在赌）。⛔ 但**我没跑过**，如实记。
2. 🔴 **`DuplicateDeck` 的出口【多了一条静态状态】**：`LastDuplicateError` 与 `LastDeleteError` / `LastSelectError`
   一样是**静态**的，**只在被调用的那一刻刷新**。⇒ 谁要拿它当判据，**必须紧接着那次调用读**，
   别跨别的方法读（本节的断言都是紧接着读的）。
3. **退出文案里丢了卡组名**（见 §二·二·2）—— **如实记**，改法也给了（一行），等调度台裁。
4. **只测了一种失败条件**（父目录不存在）：磁盘满 / 权限 / 只读归档**没测**（不确定、不适合进自检）——
   `DeckStore.SaveAll` 对它们走**同一条 `catch`**（照 A398 报告 §五·4 的口径，不是新结论）。
5. **没做回滚**（内存改了、盘上没改）：与 A503 一致，A398 在数据层已定过语义
   ⇒ ⛔ **我不自己发明**一套「失败就撤内存」。危险点靠**出声**顶住（出口 + 两条 `LogWarning`）。
6. 🔴 **一处口径冲突要调度台裁**：简报说 A610 要改「`OnOption` 里那**三支**」，而 A611 要「调用点**直接打原话**」
   —— 复制失败那**两句**在 A548 的写法下是**按 `DeckCount()` 分支**才产生的，A611 之后**只剩一句**
   （`else Debug.LogWarning("[DeckInfo] 复制失败：" + LastDuplicateError)`，`LastDuplicateError` 自己
   已经分成两种人话）。⇒ **最终实现是「两句」（删失败 1 + 复制失败 1），但覆盖的仍是原来那三种情况**。
   若调度台要的是字面上的「三句都在、只换级别」，那 A611 的「打原话」就得放弃（那两句会重新变成**猜**）——
   **我没自行取舍**：我按 A611 的验收标准做，并把这件事写在这里。
7. **没有断言去咬 `OnButton` 里那句 `Select Deck` 失败的通道**（`:1191` 仍是 `Debug.Log`）——
   它不在 A610 的授权范围内（简报明说 `OnOption` 三支、⛔ 别扩大）⇒ 见 §六·1，**只报不改**。
8. **没查**：`DeckInfoPopup` 之外还有谁把 `DuplicateDeck` 的空串当成「越界」用（A611 让空串的含义更清楚了）；
   全仓只有 `CollectionScene.cs:5153/5177` 两处，它们只钉「回名字 / 回空串」⇒ 无影响。

---

## 六、顺手发现（⛔ 本件只报不改）

1. 🔴 **`Shell/DeckInfoPopup.cs:1191` 的 `[DeckInfo] 选中卡组失败：` 仍是 `Debug.Log`** ——
   A611 落地后，它成了**本窗唯一一处「失败用普通级」**的地方（A610 把 `OnOption` 的两个失败支全变成 `Warning`，
   `Blocked()` 与 `CollectionData` 本来就是 `Warning`）⇒ **口径又不齐了**。
   WSmall3 当时给它用 `Log` 是为了与 A548 那两句「同通道」，**而那两句现在已经不是 `Log` 了** ⇒
   那条理由**已失效**。改法 = **一行**（`Debug.Log` → `Debug.LogWarning`）。
   ⛔ **越本件白名单**（那是 A600/WSmall3 的地盘、且简报明说「别扩大」）⇒ 没动。
2. 🔴 **`RuleEngine/Data/DeckLibrary.cs:176-179` 的注释已经过期**（会误导下一个会话）：
   它写着「`Shell/CollectionData.cs` 的 `CreateDeck` / `DuplicateDeck` / `ImportDeck` **既不读返回值、
   也不读 `LastError`** ⇒ 写盘失败时玩家看到的是「操作成功」」—— **A503/A611 起全都读了**
   （`DuplicateDeck` 现在读返回值 **且** 读 `LastError`）。⛔ 那个文件（`RuleEngine/`）**不在我白名单** ⇒ 只报不改。
   ⚠️ 这条按铁律 5·b 应该进 `项目任务.md` §三（**由调度台落盘**）。
3. **A611 之后，WSmall3 那两条断言（`CollectionScene.cs:5426` / `:5436`）鉴别力下降**（**仍成立、不会红**）：
   `[DeckInfo] 复制失败：` 现在拼的是出口原话（「**没复制** —— 下标…」/「复制出来了，但**没写进存档**：…」）
   ⇒ 「含没复制 / 含没写进存档 / 不含另一个」**照样满足**。**能咬住 A611 的是我新加的那两条「打原话」**
   （`#19` / `#30` —— 退回 A548 的 `DeckCount()` 写法就红，那两个写法**关键词一样**）。
   ⇒ 建议：**别把 WSmall3 那两条当成 A611 的回归网**（它们只是 A548 的回归网）。
4. **`Application.logMessageReceived` 全仓 27 处注册点里，`CollectionScene.cs` 占了 6 处**（改前 5）——
   `A600/A601` 那节一份（忽略级别）+ 本件一份（连级别一起抓）。⚠️ 同一段里挂两份 handler
   **不是缺陷**，但后面谁再往这个宿主里加「抓日志」的断言，**先看看有没有现成的 helper 可用**
   （本件的 `LvCount` / `LvFirst` 与 WSmall3 的 `ClickHit` / `HitCount` / `FirstHit` 都在 `Run()` 的局部作用域里，
   **出了这一节就没了**）。
5. **`DeckStore.SaveAll` 写的是 `Path + ".tmp"` 再 `Move`**（`DeckStore.cs:107-111`）⇒ 本件的探针**不会**
   在坏路径上留下半个坏存档，也**不会**在好路径上留 `.tmp`（收尾只需删副本那一个文件）——
   顺着探针看的，**不是缺陷、是好的那一半**，记下来免得以后有人「修」它。

---

## 七、类型检查结果 + 行尾 + 改动量

```
$ TMPDIR=/tmp/wf_wsmall4 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

跑了 **1 次**（三处都改完之后），**0/0**，**没有**出现「错误集中在别人文件上」的情况
（同批若有别人在写 `Editor/MainMenuScene.cs` 之类，本件这次没撞上）。

**行尾**（**二进制**读 —— `b.count(b'\r\n')` 对比 `b.count(b'\n')`；⚠️ 文本模式读会把 `\r\n` 折成 `\n`、
两个数恒相等）：

| 文件 | 改后 `\r\n` / `\n` | 判定 |
|---|---|---|
| `Shell/CollectionData.cs` | `0 / 273` | ✅ 纯 LF（没翻） |
| `Shell/DeckInfoPopup.cs` | `0 / 1506` | ✅ 纯 LF（没翻） |
| `Editor/CollectionScene.cs` | `0 / 5672` | ✅ 纯 LF（没翻） |

**`git diff --numstat`**（改完立刻看 —— ⚠️ **这三个文件里都还带着同批别人未提交的改动**，
所以数字**不是**我一个人的；按相邻报告的拆账反推）：

```
574   0  Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs   ← W-E2 60 + W-E4 78 + WSmall3 238 + 我 198
144  17  Unity/MyGame/Assets/CardPresentation/Shell/CollectionData.cs     ← W-E4 78/14 + WSmall3(A600) 40/3 + 我 26/0
 34   5  Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs      ← WSmall1 26/3 + WSmall3 9/2 + 我 −1/0
```

**我的两个 hunk 的位置与行数（复核用）**：`git diff -U0` 给出**仍然只有两条** ——
`@@ -5074,0 +5135,514 @@`（= W-E4 的 A503 节 **78** 行 + WSmall3 的 **238** 行 + 我这一整块 **198** 行）
与 `@@ -4030,0 +4031,60 @@`（= W-E2 的 A404 段）—— **一条不多一条不少**，
说明我**只**加了自己那一块、**没有**碰过那三块（W-E2 `:4031-4090` · W-E4 `:5135-5212` ·
WSmall3 `:5213-5449` 现读仍在、无重复）。

**`DeckInfoPopup.cs` 少了一行（`34/5` 对 WSmall3 当时的 `35/5`）**：删掉 `int dupBefore = …`（1 行）、
复制失败由 2 句并成 1 句（−1）、A610 备注 +1 ⇒ **净 −1**，与预期吻合（不是被谁覆盖）。

**影响面（决定收口要跑哪几条）**：全仓碰 `CollectionData` 的自检宿主 = `Editor/CollectionScene.cs` ·
`Editor/DeckScene.cs` · `Editor/MainMenuScene.cs`（`grep -l "CollectionData\."`）。
**本件的断言只住在 `CollectionScene.Run`** ⇒ 收口**至少**要跑 **`CollectionScene.Run`**；
`DeckInfoPopup` 的**产品代码**（A610 那两句）被 `MainMenuScene` / `DeckScene` 间接用到与否见 WSmall3 §七 的
同一条结论 ⇒ **建议一起跑 `CollectionScene.Run` + `MainMenuScene.Run`**（`DeckScene.Run` 顺带）。
