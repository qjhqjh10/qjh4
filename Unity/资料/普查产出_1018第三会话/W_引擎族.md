# W_引擎族 · 执行写手报告（第三会话 · 2026-10-18）

**写手**：引擎族执行代理（`general-purpose`）
**白名单**：`Core/RuleCore.cs` · `Core/EffectResolver.cs` · `Editor/RuleEngineTest.cs`
（⛔ 一个字节都没碰别的文件；⛔ 没跑 Unity；⛔ 没动 git；⛔ 没改正本）
**类型检查**（`TMPDIR=/tmp/wf_eng bash 工具/typecheck.sh`，每次改完都跑）：
**运行时 0 错 / 编辑器 0 错**（最后一次）。
**行尾**：`git diff --numstat` = `128/15`（RuleCore）· `88/6`（EffectResolver）· `378/0`（RuleEngineTest）
—— 都是**行内增删**，不是整篇翻（三个文件分别 4935 / 7401 / 20259 行）。

---

### ① `A947` —— 结论：**已改（一半）· 常量的归一化表项做不了（不在白名单）**
- **改了哪**：`RuleCore.cs`
  - 新增 **`RuleCore.Noncombatant = "noncombatant"`**（public const）+ **`AttackBannedByTraits(UnitState)`**
    （两道禁令的**唯一判定口**；`IsValidTarget` 与 `CanAttackNow` **都调它**）。
  - `RuleCore.IsValidTarget`：原来那句 `attacker.Has(KeywordTable.CantAttack)` → 换成调共用口。
  - `RuleCore.CanAttackNow`：**新增**那一句（原来**一道禁令都没有**）。
- **判据**：`CardScript__CanAttackNow.c:25`（`HasCurrentTrait(card, 0x96 = 150 = cantattack)`）·
  `:27`（`0x370 = 880 = noncombatant`）—— 两道在**同一个查询口**里连着读，任一成立就 `goto LAB_1805e792d`。
- **配了断言**：`TestA947NoncombatantAttackBan`
  - Ⓐ 夹具 `UnitState.AddKeyword("noncombatant", 1)`（**运行时授予**，见下「做不了的那一半」）：
    先断「没禁攻时两道口都 = OK」（前提，否则下面那条是假绿）→ 再断两道口都 = `ErrNoAttack`。
    🧨 **改坏法**：删 `|| u.Has(Noncombatant)`（或让 `CanAttackNow` 自己重写一遍字符串比较并漏掉它）⇒ 实得 OK ⇒ 红。
  - Ⓑ 回归：真词面 `Can't Attack`（经 `Normalize` → `cantattack`）两道口照旧拒。
- **🔴 做不了的那一半（请调度台分流）**：判据要求在 `CardDef.cs` 的 `KeywordTable` 加常量**并进
  `Prefixes` 前缀表**（`CardDef.cs:2500` 那一张）——**那不在我的白名单**。
  ⚠️ 后果是**具体可复现的**：`Normalize("noncombatant")` 现在返回 `null` ⇒ 卡数据里写这个关键词会被
  `CardDef` 构造器 `NoteDropped` **静默丢掉** ⇒ 就算有了常量，**卡面也表达不了它**。
  今天全池 0 张卡带它，所以**没有行为缺口**；要真按原版用（将来有卡面授予它）**必须补那张前缀表**。
  📌 建议：把 `RuleCore.Noncombatant` 与 `Prefixes` 表项**一起**归并进 `KeywordTable`（那时删掉我这处常量）。

---

### ②a `A858` —— 结论：**已改（注释改写 + 断言 + 变残骸那一支补上）**
- **改了哪**：`RuleCore.CleanupDeaths`
  - `ctx.DiedThisTurn++` **从 `else` 那一支提到「残骸/离场」分支之前**（**两支都计**，仍只写一处）。
  - 上面那段「**没查清**」的注释**整段改写**成判据链。
- **判据（现读方法体，逐跳）**：
  - `CardScript__TriggerUnitBacklashActions.c:82` = `*(param_1 + 0x250) = <globalVars 的回合号 0x3f8..0x40c>`。
    🔴 **关键细节（这次读出来的）**：这句在 `:70-71` 的 `if (BattleManager != 0)` **之内**、
    而在 `:52-69` 的 `if (HasCurrentTrait(0x78 = backlash))` **之外** ⇒ **每一次死亡都写**，
    **不是只有带反噬的单位**才写。
  - 它被**两个分支都调**：`CardScript__CheckIfDead.c:135`（死亡支）与 `:141`（**变残骸支**，
    在 `AddTransformIntoRemnant`(`:143`) 之前）⇒ **翻面成残骸也算「本回合死了一个」**。
  - 位置：两支都在**从棋盘移除之前、进墓地之前**，更在**反噬结算之前** ⇒ 我们**不下移**（结论与账上一致）。
- **配了断言**：`TestDiedThisTurnDuringBacklash`
  - **②** 反噬正文 `for each one that dies`（`CountScope == "died"` 读 `ctx.DiedThisTurn`）：
    探针 1 血 0 攻、被 3 攻打死后反噬要**打 3 点**（击杀者 10 → 7）。
    🧨 改坏法 = 把 `DiedThisTurn++` 挪到 `FireTriggerAt(…Backlash)` 之后 ⇒ 反噬读到 0 ⇒ 实得 10 ⇒ 红。
  - **③** 变残骸支：`Remnant` 单位被杀 → **先断它真的翻面留在格位上**（前提）→ 再断 `DiedThisTurn == 1`。
    🧨 改坏法 = 把 `++` 留在 `else`（只算真离场）⇒ 实得 0 ⇒ 红。
- **没查清**：无（两条判据都是**正读方法体**得出的）。

---

### ②b `A962` ③ —— 结论：**未改（读不确凿）· 只补了注释**
- **改了哪**：`RuleCore.CleanupDeaths` 两处**纯注释**：`ctx.Emit(EvtKind.Death…)` 那一跳按裁定标
  「这是**我们表现层**的，原版没有对应物，位置不经判据」；`BroadcastWhen(Die)` 上面写进**那次复核的次序表**。
- **次序表（现读，逐跳）**：

  | 环节 | 原版落点 | 能判吗 |
  |---|---|---|
  | 反噬**入队** | `CardScript__TriggerUnitBacklashActions.c:181`（`AddAutoActionToQueue`）；调用点 `CheckIfDead.c:135/:141` + `ResolveDestroyUnit.c:371/:703` | ✔ 最早 |
  | 从棋盘移除 | `BattleManager._ResolveMinionDeath_d__454__MoveNext.c:56` → `:61` → `:119` `StartCoroutine(UnitDeath)` | ✔ 之后 |
  | 反噬**结算** | 入队那条 action 何时被取出执行 | ✖ **读不到** |
  | 别的卡**监听** | `BattleManagerSupport__BroadcastDeadUnit`（`_ResolveBacklash_d__451__MoveNext.c:144`） | ✖ **起点读不到** |
  | 进墓地 | `CardScript__GoToCemetery.c:17`（只在 `_UnitDeath_d__446__MoveNext.c:152`） | ✔ 最后 |

- **为什么读不确凿（这是结论，不是推托）**：`_ResolveBacklash_d__451`（含 `BroadcastDeadUnit`）与
  `_ResolveMinionDeath_d__454`（含 `UnitDeath`）**全库都查不到调用者**
  （`grep ResolveBacklash` / `ResolveMinionDeath` 只命中它们自己的文件）⇒ 两者都是**协程入口，
  启动点的方法体在 dump 里缺失** ⇒ 「反噬的 action 被取出执行」在 `BroadcastDeadUnit` 之前还是之后
  **读不出来**。⇒ 按简报口径：**一行都不改**，维持现状、如实标着。
- **配了断言**：无（按裁定）。**要收口这一格**需要**实况**（在 `BroadcastDeadUnit` 与反噬 action 上各加探针）
  ⇒ 建议挂进 `资料/真Play待验清单.md`。
- ⚠️ 既有的 `TestBacklash` 仍断「Death 事件排在 Backlash 之前」—— **那跳是表现层的**，不受影响。

---

### ③ `A974` —— 结论：**已改（两处都下移）**
- **改了哪（按符号）**：
  - `RuleCore.PlayCard`：`ctx.NotePlayed(p, inst, false)` 从 `BoardSlots.Insert` 之后**挪到**
    `BroadcastWhen(ctx, WhenEventKind.Play, p, card, unit)` **之后**（仍在 `BroadcastWhen(…Deploy…)` 之前）。
  - `RuleCore.PlayTactic`（写在 `EffectResolver.cs`）：`ctx.NotePlayed(p, inst, true)` **挪到**
    `BroadcastWhen(ctx, WhenEventKind.Play, p, card, null)` **之后**；`ps.SecretsPlayed++` **留在原位**
    （原版次序就是 `:1274 AddSecretPlayed` → `:1426 广播` → `:1503 AddPlayCardAction`）。
- **判据**：`BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c` 的 `BroadcastCardPlayed`（`:1426`）
  在 `CemeteryManager__AddPlayCardAction.c` 的唯一调用点（`:1503`）**之前**（两处行号**逐行核过**）。
- **配了断言**：`TestA974NotePlayedAfterPlayBroadcast`
  - ① **扫描器反制**（合成样本：新次序→1 / 旧次序→0 / 两句话都不在→−1；⛔ 不做这步，恒返回 1 也会全绿）。
  - ② **真文件**：`RuleCore.cs` 与 `EffectResolver.cs` 各扫一遍（`StripCsComments` 去掉注释后比首次出现位置）。
    🧨 改坏法 = 两句换回去 ⇒ 0；缺任一句 ⇒ −1。
  - ③ 回归：挪位后那一笔**照样记得上、且只一笔**（打一张单位卡 ⇒ `PlayedCards` +1、记的是刚打出的那张）。
- ⚠️ **如实标着**：全池**没有任何东西**在 `Play` 广播期间读 `PlayedCards` ⇒ 两层次序**状态相同**，
  所以这条只能是**源码次序**判据（先例 = `TestS7BroadcastBeforeBookkeeping`，理由同）。
- **`A920` 复跑**：按简报要求**需要调度台在同步点跑 `RuleEngineTest.Run`**（我没跑 Unity）。
  静态复核：`TestPlayedCardsExcludesCompanionAndTide` 量的是 `PlayCard` **返回之后**的计数差，
  而 `NotePlayed` 全仓 2 个调用点、挪位没有增删 ⇒ 读数不变；本批新加的 ③ 也把它再钉一遍。

---

### ④ `A976` —— 结论：**已改**
- **改了哪**：`RuleCore.ResolveOps`（写在 `EffectResolver.cs`）**入口**，挨着 `LastChosenCard = null;`
  之后加 `ctx.LastCreated.Clear();`（与 `DrawnThisResolve` / `LastHandTarget(s)` / `LastTargets` 同一个窗口）。
  同文件 `DoLowerCost` 的「`(指代上一张)`」那段**引错的注释就地订正**（原文写「`LastCreated` 在 `DoCreate` 入口清」）。
- **判据**：原版 `AbilityLogic.SetPreviousAbilityTargets.c:19-28`（**每一条能力先 `Clear()` 再 `AddRange()`**）
  ⇒ 没有「槽里还留着上一张卡的」这回事；`it/they` 一律指**上一条能力**。
- **配了断言**：`TestA976LastCreatedNotCarriedAcrossCards`
  - **① 状态判别式（最直接）**：先往 `ctx.LastCreated` 放一份 → 跑一次**不含 create/return** 的结算
    （`Gain 1 Energy`）→ 断槽已空。🧨 删那句 `Clear()` ⇒ 实得 1 ⇒ 红。
  - **② 行为侧（真卡 `Company Master` DA31）**：槽里种一份 5 费陈旧指代 → `RuleCore.Draw`
    ⇒ 断**抽到的那张** `CostOf == 4`、**陈旧那份 == 5`。🧨 删 `Clear()` ⇒ 前者 5 / 后者 4（= 改之前的错）⇒ 双红。
    ⚠️ 陈旧值是**直接种进槽里**的（真链路上它就是上一张 `Create … in your hand` 造出来的那一份）——
    这样断言的变量只有一个。**如实标着**：这也意味着本条**不覆盖**「同一条 resolve 内先 create 后 draw」。
- **一号靶复跑**：`Master of Manoeuvre`（`RuleEngineTest.cs` 的 `TestHandInstanceEffects` 一族）与 `T_Return`
  都是**同一次 resolve 内 op1 写 / op2 读** ⇒ 入口清不影响；**需要调度台在同步点跑 `RuleEngineTest.Run` 确认**。

---

### ⑤ `A963`③（止血件）—— 结论：**已改（共用件没动，调用点传新 criteria）**
- **先回答简报点名要问的**：`HandTroopCriteria` **是共用件** —— `grep` 实测 **3 个调用点**
  （`GrantHandBuff` 的两处：**先登记记录** + **贴到当前手牌**；`BroadcastHandWhen` 的「没写主语」那一处）
  **外加** `RuleCore.SetupCardInHand` 那一侧消费它登记的记录。
  前两处服务的正是卡面写 `all Beasts in your hand` 那一族（`GOF81 Beast Snagga Nob`）⇒ **动它就是把那几张卡改坏**。
  ⇒ **按简报：在调用点传一个收窄后的新 criteria**。
- **改了哪**：`EffectResolver.cs`（= `partial class RuleCore` 的那半个文件）
  - 新增 **`HandSelfCriteria(string cardName)`**（= `Kind="unit"` + **`NameFilter = 那一份牌自己的卡名`**），
    定义紧挨着 `HandTroopCriteria`，注释里写清「为什么不能改共用件」「这是止血、判据在远端、将来怎么补」。
  - `BroadcastHandWhen` 的 `HandListenerSelfOp` 那一段：`HandTroopCriteria` → **`HandSelfCriteria(c.Name)`**。
- **判据**：`PlayerHand__AddHandEffect.c:88-139`（`:90-110` 先收记录、`:112-139` 按 `targetCriteria @+0x20` 逐张贴）·
  `PlayerHand__SetupCardInHand.c:58/71`（后进手牌的牌也按同一份 criteria 补）。
- **配了断言**：`TestA963HandListenerSelfDoesNotSpread`
  - 夹具 = 手牌监听器 `When you draw a card, gain +1 Attack`（在手里）。
  - 前提三条：监听器真在手里 · 它自己吃上 1 条（机制活着）· 记录登记上了 · 手里有别的牌。
  - 主断：再抽一张 ⇒ **别的牌 `HandEffects` 全为 0**。
    🧨 改坏法 = 把调用点换回 `HandTroopCriteria` ⇒ 记录又发给每个后进手牌的部队 ⇒ 实得 ≥1 ⇒ 红。
  - 对照：**同名的它自己照样吃得上**（`>= 2`）⇒ 证明「只砍扩散，没把机制砍掉」。
- ⚠️ **如实标着（写在代码注释里）**：`HandEffectFits` 只拿得到 `CardDef` ⇒ 收窄的**下限就是「同名卡」**
  （第二份同名 `Rubric Marine` 进手牌仍会吃上）；而且 `BL15` 的真判据（`buffSelf(40)` vs `buffHand(50)`）
  在**本地缺失的 `allcards` bundle** ⇒ **可能比原版少给**。`allcards` 到位后读那张卡的 `targetCriteria`，
  若为 `buffHand` 就把这一个调用点换回去（一处改动）。

---

### ⑥ `HandEffectExpired` 的 `0x82` 注释 —— 结论：**已订正**
- **改了哪**：`RuleCore.HandEffectExpired` 的方法头注释 —— 原来写「`0x82` 是哪个词条**本笔没查清**，
  我们**没有**实现这一条」⇒ 改成 **`0x82` = 130 = `DefinedTrait.jam`**（`DefinedTrait.cs:13`），
  词表名 `KeywordTable.Jam`，**而且这一支已经实现**（同方法体 `:3572-3582`，原版落点
  `PlayerHand__UpdateCardEffects.c:235`）。⛔ 两处说法打架比没有更糟（原注释与下面的实现自相矛盾）。
- **顺带补一条**：原版 `untilEnemyTurnStart // +0x34` 那一档我们**产不出**（`ExtractDuration` 只认两档）
  —— 那是**没有生产者**，不是漏接，写清楚了。

---

### ⑦ 判据链 —— 结论：**已做**
本次触及的每一段都留了「原版方法名 + 结论 + 出处（`文件:行号`）」：
`AttackBannedByTraits` / `Noncombatant`（A947）· `CleanupDeaths` 的 `DiedThisTurn` 段与 `Emit`/`Die` 两段（A858 · A962③）·
`PlayCard` 的两处（A974）· `ResolveOps` 入口与 `DoLowerCost`（A976）· `HandSelfCriteria`（A963③）·
`HandEffectExpired`（⑥）· 5 条新测试的头注释（含「🧨 改坏法」）。

---

## 顺手发现（都没改，按调度台口径报上来分流）

1. 🔴 **`EffectResolver.cs` 里写的其实是 `public static partial class RuleCore`** —— 全仓约定按**文件名**
   （`EffectResolver.DoDouble` 之类）称呼，但**测试里不能写 `EffectResolver.X`**（我第一次写就编不过，
   `CS0103`）。**建议记进文档**：跨文件引用一律写 `RuleCore.*`。同理 `HandTroopCriteria` 等「EffectResolver 的」成员
   实际都是 `RuleCore` 的成员。
2. 🔴 **`BroadcastDeadUnit` 的真身比账上写的重得多**：它**不是「广播一个 kind」**，而是
   **遍历场上每一张牌、对每张各调一次 `CardScript.ResolveDeadCard`**（`BroadcastDeadUnit.c:147/206/262/369`）。
   ⇒ 原版「别的卡监听这次死亡」= **让每张牌重跑一遍死亡反应**，不是「发事件给监听器」。
   我们这边是「`WhenTriggers` 事件层」的同构物 ⇒ 机制不同但语义可对齐；**这一条值得写进 A962③ 的账**。
3. 🆕 **`TriggerUnitBacklashActions` 有两个调用来源**：`CheckIfDead`（我们已复刻那条路）
   与 **`BattleManager__ResolveDestroyUnit.c:371/:703`**。我们的 `CleanupDeaths` **只有「伤害致死」一条入口**
   （`Hurt` → `CleanupDeaths`）⇒ 「被 `destroy` 类效果直接摧毁」在原版走的是**另一条链**，
   而那条链上**也有反噬入队**。⚠️ 我**没查实**我们的 `destroy` 效果是否收敛到同一条路
   ⇒ **留待分流**（可能是真缺口，也可能是同构）。
4. `BattleManager__AddAutoActionToQueue` 有**第三个参数**（队列类型 `0/1/2`，选 `+0x350` / `+0x368` … 三张不同的表）
   —— 反噬走 `0`、`ResolveDestroyUnit` 另一处走 `1`。**这是「入队」那一步的一条未使用信息**，记下来备用。
5. ⚠️ **模型差异（如实记，不是缺陷）**：原版是**每张卡一个 `turnDied`**（`CardScript +0x250`），我们是**一个全局
   `ctx.DiedThisTurn`**。今天「count 'died'」的语义等价（每次死亡都记一笔），但**原版那个 `CountScope` 到底怎么汇总**
   我没查 ⇒ 若将来要做「逐卡」语义（例如 `a troop that died this turn` 点名到具体某一张），得回头读
   `EffectSpec` 对应那一支原版方法。

---

## 需要调度台在同步点做的事

1. 🔴 **跑 `RuleEngineTest.Run`**（本批只动引擎 + 它自己的自检宿主 ⇒ 按铁律 12 只需这一条；
   ⛔ 建议**串行**，Unity 单实例）。重点看：`TestA920`/`TestPlayedCardsExcludesCompanionAndTide`（A974 依赖）、
   `TestHandInstanceEffects` 里 `Master of Manoeuvre` 与 `T_Return`（A976 一号靶）、
   `TestG3EngineRealDiffs`（手牌效果一族，⑤ 会碰它附近的路径）、`TestDeathAccountsAfterBacklash`、
   以及本批新加的 5 条。
2. ⚠️ **本批新加的 5 条断言我一次都没跑过**（不能跑 Unity）—— 头一次跑若红，**优先按「断言自己写错」查**
   （夹具的牌序 / `Battle` 起手页数 / 解析是否认得那句正文），**不要先怀疑实现**。特别是我在测试里写死的几条**前提**
   （`Gain 1 Energy` 解析得出 · `Backlash: Deal 3 damage … for each one that dies` 解析得出 ·
   `Company Master` 在卡池里 · 抽到的是 `A976Big`）—— 它们**本来就是设计成「红了先看它」**的。
3. 📌 **A947 的归一化表项**（`CardDef.cs` 的 `KeywordTable.Prefixes` + `Noncombatant` 常量）**还没做**
   —— 那文件不在我的白名单，需要另派一个写手（或在归并常量时一起做）。

（报告 ≤150 行，至此结束。）
