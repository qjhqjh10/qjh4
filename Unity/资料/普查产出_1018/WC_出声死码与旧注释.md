# WC · 出声死码 + 三处旧注释

> 写手 `WC`。⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改白名单外任何文件（`RuleEngineTest.cs` 一个字没碰）。
> 判据来源 = `资料/普查产出_1018/DB_手牌效果登记6红.md` §6.1 / §6.3（只做这两条）+ 源码现读。
> 白名单 = `RuleCore.cs` · `EffectResolver.cs`(只注释) · `CardInstance.cs`(只注释)。

## 1. 改了什么（逐处）

### 1.1 `Core/RuleCore.cs:3638-3641`（`SetupCardInHand` 循环里）—— **(β) 死代码，唯一一处逻辑改动**

改前：
```csharp
bool isLoose;
if (!HandEffectFits(inst.Card, e, out isLoose)) continue;   // ← continue 排在计数之前
if (isLoose) loose++;
if (AttachHandEffectCopy(ctx, inst, e)) n++;
```
改后：
```csharp
bool isLoose;
bool fit = HandEffectFits(inst.Card, e, out isLoose);       // 先取值
if (isLoose) loose++;                                        // 再计数
if (!fit) continue;                                          // 后判去留
if (AttachHandEffectCopy(ctx, inst, e)) n++;
```
理由：`HandEffectFits` 只在 `{ loose = true; return false; }` 那一支置位 out ⇒ 旧写法里 `loose++` 只有**返回 true** 时才到达 ⇒ `loose` 恒 0 ⇒ 末尾 `if (loose > 0)` 那段「退路档自己说出来」**永远打不出来**（违反工程红线「不许静默失败」，也是 ⑨ 号那类回归能静默过收口的原因）。
**行为零变化**：`isLoose == true` 与 `fit == false` 是同一件事（证明见 §2 第 2 步）⇒ ③④ 的先后只影响「计不计数」，不影响 `n` / 返回值。
`+13/-1` 行（`RuleCore.cs` 整份：7 行循环注释 + 4 行 summary 订正 + 1 行真改动，减掉 1 行旧写法）。

### 1.2 `Core/RuleCore.cs:3606-3610`（同方法 `<summary>` 里那句）—— 旧话订正（铁律 5）

改前那句：「🔴 退路**会自己说出来**（`SetupCardInHand` 末尾那句日志）」。**当天它是假的**。
改后：保留原句 + 追加「⚠️ **2026-10-18 订正**：这句在当天是假的 —— 那句出声是死代码（`continue` 排在 `loose++` 之前 ⇒ `loose` 恒 0，全 4 MB 日志零命中），当天已按 `DB` §6.1 修好；现在它真的会出声」。
理由：铁律 5「发现记错了就地改掉 + 保留更正痕迹」；这句和 §6.3 那三处是**同一个过期口径**的第四个落点，不改就会留下「两份说法打架」。

### 1.3 `Core/EffectResolver.cs:2850-2859`（`AttachHandEffect` 的 `criteria` 形参）—— 旧话订正

改前：「`null` = 没有可判的规格（那时 `RuleCore.SetupCardInHand` **退回「单位卡」这一档**，见那儿）」—— **那个退路 `W5` 当天已删**。
改后：`null` ⇒ **这条记录【不补给】后进手牌的牌**（`HandEffectFits` 判不出来那支是 `return false` ⇒ `SetupCardInHand` 只出声、不贴牌）；追加「⚠️ 2026-10-18 订正：原来写 X，那个退路已经不存在了」+ 原版判据 `FilterMethods__CheckIfMeetsCriteria.c` 头两句（**criteria 缺失 ⇒ `LogError` + `return 0`**；只有「criteria 存在但为空」才 `return 1`）+ 「真对局里四个生产者全部传 spec ⇒ 本值只由自检默认值产生」。

### 1.4 `Core/EffectResolver.cs:3097-3103`（公开入口 `AttachHandEffectToInstances` 的同一形参，方法体在 `:3105`）—— 旧话订正

改前：「`null` = 没有可判的规格。」（**没写后果**）。
改后：补上后果那句（与 1.3 同一句话）+ 订正痕迹（旧口径还含「退回单位卡放行」那一档，`W5` 已收窄掉）。

### 1.5 `Core/CardInstance.cs:158-168`（`HandEffect.Target`，字段在 `:170`）—— 旧话订正

改前：「`null` = 这一条没有可判的规格（见 `RuleCore.SetupCardInHand` 的如实说明）」—— 不但没写后果，**指的还是一处已经改了口的说明**。
改后：`null` ⇒ **这份记录【不补给】后进手牌的牌** + 订正痕迹 + 原版判据 + 「本值只由自检默认值产生」。

> 📌 三处口径**逐字一致**（各带一句「真对局里四个生产者都传 spec ⇒ 本值只由自检默认值产生」），避免又长成三份不同说法。

## 2. 🔴 `loose > 0` 那段出声现在【怎么才可能触发】（控制流逐步）

行号均按**改动后**的文件（`RuleCore.cs`）。

**第 0 步 · 入口前置（都不拦这条输入）**
- `:3620-3623` `ctx` / `inst` / `inst.Card` 非空、`owner ∈ {0,1}`、`ctx.Players[owner] != null`（过了建场期）。
- `:3624` `reg = HandEffectRegistry(ctx, owner)`；`:3625` `if (reg.Count == 0) return 0;`
  ⇒ **必要前提 = 登记表 `PlayerState.HandEffectRecords` 里至少有一条 `Op != null && Op.Verb == "give"`、`RecordId` 未重复**（`HandEffectRegistry`，`:3680-3692`；去重那句在 `:3690`）。**旧代码也有这条前提**，不是新加的。

**第 1 步 · 循环体（`:3627-3641`，改动就在这四句）**
```csharp
if (e == null || e.Op == null) continue;                 // ①
bool isLoose;
bool fit = HandEffectFits(inst.Card, e, out isLoose);    // ② 返回值与 out 一次都拿到
if (isLoose) loose++;                                     // ③ 计数在 continue 之前
if (!fit) continue;                                       // ④
if (AttachHandEffectCopy(ctx, inst, e)) n++;              // ⑤
```

**第 2 步 · `HandEffectFits`（`:3711-3758`）的取值表 —— 逐支穷举**

| 输入 | 走到哪一支 | `out loose` | 返回 |
|---|---|---|---|
| `card == null` | `:3714` | false | false |
| **`e.Target == null`** | `:3724` `FromTarget(null)` → **null**（`CardCriteria.cs:115`）· `:3725` `hasSubtype=false` · `:3730` 的 `PrevAntecedent` 分支要求 `e.Target != null`（**不成立**）⇒ 落到 `:3748` | **true** | false |
| `e.Target != null` 但 `FromTarget` 得 null、无 `SubtypeFilter`、无 `PrevAntecedent`（例：`Kind=="prev"` 的 `it/them` **没有先行词** —— `CardCriteria.cs:119` `IsKindWord("prev")==false` ⇒ 整体 `IsEmpty` ⇒ `:126 return null`） | `:3748` | **true** | false |
| `crit != null && !crit.Matches(card)` | `:3749` | false | false |
| `hasSubtype` 且兵种不匹配 | `:3750-3756` | false | false |
| 三档全过 | `:3758` | false | **true** |

⇒ 🔴 **`out loose == true` 与「返回 false」是同一件事，且唯一来源是 `:3748` 那一支**（`:3713` 入口置 false，之后**只有** `:3748` 写 true，且它紧接着 `return false`）。
**推论 A（行为不变的证明）：** ③④ 互换次序**不可能**改变 `n`、也不改变返回值 —— 旧写法「返回 false ⇒ 直接 `continue`、不计数」与新写法「返回 false ⇒ 先计数、再 `continue`」在**贴牌**这一侧完全等价；唯一差别就是 `loose` 终于不是 0。
**推论 B（出声的必要输入）：** `loose` 变非 0 **只可能**由 `:3736` 触发 ⇒ 出声的输入条件 = 「这条记录我们判不出筛选条件」。

**第 3 步 · 一遍完整的触发走位**（取最短路径：登记表里 1 条 `Target == null` 的 `give` 记录）
1. `:3625` `reg.Count == 1`，不早退。
2. ① 该条 `Op != null`，不 `continue`。
3. ② `fit = false`、`isLoose = true`（上表第 2 行）。
4. ③ `loose` 0 → **1**（旧代码**永远到不了这一句**）。
5. ④ `!fit` ⇒ `continue`，**这一条不贴牌** ⇒ `n` 不加（**与旧代码一致**）。
6. 循环结束（不管后面还有没有条目，只要**至少有一条**是 `:3736` 那一支，`loose ≥ 1`）。
7. `:3649` `if (loose > 0)` ⇒ **成立** ⇒ `ctx.Log("（进手牌：「X」有 **N 条**效果的筛选条件我们**判不出来** ⇒ **这一趟一条都没给它**…")` 真的执行。

**第 4 步 · 这句话能到哪儿（可被自检/日志验到）**
- `ctx.Log` = `BattleContext.cs:1248-1252` 的 `Events.Add(message)`（带 `EventCapacity = 2000` 的环形裁剪，`BattleContext.cs:523`）。
- 自检读的**就是**这个通道 —— `RuleEngineTest.cs:8484` 那句注释：「判据读的是 `ctx.Events`（`BattleContext.Log` 就是往那儿写），不是屏幕输出」；失败时 `Events` 会被 dump 进运行日志（`DB` 代理就是靠 grep 那份 4 MB 日志拿到「旧写法 0 命中」的反证的）。
⇒ 修完之后，同样的输入会在 `ctx.Events` 里**出现**含「筛选条件我们」/「这一趟一条都没给它」的那一条。
⇒ 建议的出声断言（**由主对话在同步点决定要不要加、加在哪**，我按红线没碰 `RuleEngineTest.cs`）：在某条走 `SetupCardInHand` 且 `criteria = null` 的用例里断言 `ctx.Events.Exists(e => e.Contains("判不出来"))`（同族先例 = `RuleEngineTest.cs:3847-3850`「摘的时候出声」）。
**现成的最合适落点已找到**：②-1（`RuleEngineTest.cs:19301-19309`）就是那条 `criteria = null` 的用例 —— 我这次修完，**它的 `ctxA.Events` 里第一次出现这条出声**，而那一小段**没有任何读 `ctx.Events` 的断言** ⇒ 我这趟**不会**让它变红（我逐行读过 19296-19343：②-1 / ②-1b / ②-2 都只断 `HandEffects.Count` 与返回值）。

**第 5 步 · 反证：修之前【不可能】触发（证明这是真死码，不是「碰巧没触发」）**
旧代码 ② 是 `if (!HandEffectFits(..., out isLoose)) continue;` —— 返回 **false** 时直接 `continue` ⇒ ③ `if (isLoose) loose++;` **只可能在返回 true 时**到达；而返回 true 的唯一出口 `:3758` 位于 `:3748` **之后**，走到那儿 `loose` 已被入口 `:3713` 置 false 且**再没被写过** ⇒ 到达 ③ 时 `isLoose` **恒 false** ⇒ `loose` **恒 0** ⇒ `:3649` **恒不执行**。
两条独立证据互证：① 控制流（上一句）；② `DB` 只在 4.0 MB 运行日志里 grep「筛选条件我们」/「这一趟一条都没给它」= **0 命中**。

**第 6 步 · 这次修**没有**碰那道闸**（留给下一批的判别力保证）
`HandEffectFits:3748` 一个字没动 ⇒ 6 条红（④⑤⑥「后进手牌该吃上却得 0」）修完之后**仍然是 0**；②-1（`RuleEngineTest.cs:19305-19307` 期望 `0`）**依旧绿**。出声只**多**一条 `ctx.Events` 条目，不改变 `n` / 返回值 ⇒ **零玩法影响**。

## 3. 类型检查读数（原样贴）

命令：`TMPDIR=/tmp/wf_wc bash d:/4/Unity/工具/typecheck.sh`（独立 `TMPDIR`，按铁律 13·3）
```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⇒ **0 / 0**，无「别人半成品」造成的假错。

## 4. 行尾核对

`git diff --numstat -- <三个文件>`（对照各自总行数）：

| 文件 | +/− | 总行数 | 判定 |
|---|---|---|---|
| `MyGame/Assets/RuleEngine/Core/CardInstance.cs` | **8 / 1** | 357 | ✅ 没翻（翻了会是 ~357 对） |
| `MyGame/Assets/RuleEngine/Core/EffectResolver.cs` | **14 / 2** | 7332 | ✅ 没翻 |
| `MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **13 / 1** | 4896 | ✅ 没翻 |

全部用 Edit 工具改，⛔ 没用 `sed -i` / python 写。`git diff -U1` 逐行看过，改动**只有** §1 列的那五处。

## 5. 顺手发现（都没改）

1. 🔴 **出声文案现在会「说过头」** —— `RuleCore.cs:3651` 那句里写着「**这一趟一条都没给它**」，但 `loose` 数的是**判不出来的那几条**，而**同一张牌完全可能既有吃上的、又有判不出的**（例：它同时命中一条 `SubtypeFilter="Beast"` 的记录被贴牌 `n++`，又命中一条 `Kind=="prev"` 无先行词的记录进 `loose`）⇒ 那时 `n ≥ 1` 而文案仍说「一条都没给它」，**是我这次修出来的新可见错误**（旧写法恒 `loose == 0`，这句从不打印，所以从未显形）。
   建议改法（一句话，需主对话裁口径后再动）：把「⇒ **这一趟一条都没给它**」改成「⇒ **那 {loose} 条没给它**」。
   我**没动它** —— 超出简报给的「三行改法」，且属于文案口径，按 13·8 由主对话裁。
2. **`ctx.Events` 是 2000 条环形缓冲**（`BattleContext.cs:523/1251`）⇒ 出声断言要**紧跟**在被测动作之后读（长用例里那条可能被裁掉）；靠 dump 全量日志 grep 的方式不受影响（dump 的是当刻表）。
3. **`DB` §6.1 建议的出声断言还没落**（见 §2 第 4 步）—— 属于 `RuleEngineTest.cs`，本趟红线禁止，留给下一批。
4. 顺带确认（**不是缺陷，是给下一批的判据**）：`CardCriteria.FromTarget(null) == null` 由 `CardCriteria.cs:115` 的第一句保证，`Kind="prev"` 得 null 由 `:119 + :126` 保证 —— §2 第 2 步那张表是照这两处现读的，不是推测。
5. 🔴 **`RuleEngineTest.cs:4010-4011` 那条 🧨 判别力说明【已经失效】**（同族「过期口径」的第五个落点，**属 `RuleEngineTest.cs`，我按红线没碰**）：它写着「把 `HandEffectFits` 里那道兵种筛删掉 ⇒ `crit` 判不出来 ⇒ **走退路档放行** ⇒ 负例实得 1 ⇒ 红」。`W5` 收窄之后，删掉兵种筛 ⇒ 落到 `:3748` ⇒ `return false` ⇒ **负例仍是 0** ⇒ **那个变异改不出红了，这条 🧨 的判别力没了**（测试在变异下仍绿 = 它现在只能证明「没坏」，不能证明「在判」）。建议口径（留给下一批写手，需主对话裁）：把 🧨 改写成「删掉兵种筛 ⇒ 走 `:3748` 的『判不出来』档 ⇒ 负例仍 0、**但 `ctx.Events` 里会多一条出声** ⇒ 用出声断言抓它」。
6. **我这条新出声【不会】进「静默失效普查」**：那道普查（`RuleEngineTest.cs:4859-4950`，全 AI 对局扫事件）只收含 `没生效` / `没实现` / `没结算` / `判不了` / `数不出来` 之一的事件，而我这条文案里一个都不含（**「判不出来」不是「判不了」的子串** —— 判/不/出/来 vs 判/不/了）⇒ 不影响它那几条断言的计数。
   ⚠️ **另一面**：正因为它不含那五个词，**这道普查看不见它** —— 如果你希望「判不出条件 ⇒ 少给」也被普查统计，就得在文案里带上其中一个词（属于文案口径，由主对话裁，我没动）。
