# B88_E3 · 「引擎 · 动作泵与 AI」（P0 · 账 B03）施工报告 — 第八十八会话波 1（2026-10-27）

> **代理**：`E3`（有写权）· **独占文件（本批实际动过的）**：
> `RuleEngine/Data/SimpleAI.cs`（**1 行 1→1** ＋ 第 123 行 `class`→`partial`）·
> **新建** `RuleEngine/Data/SimpleAI.ActiveEntry.cs`（兄弟分册）·
> `RuleEngine/Editor/RuleEngineTest_EngineA1.cs`（只加断言）· `RuleEngine/Editor/RuleEngineTest_EngineF8.cs`（只加断言）
> **账补丁**：`账/_parts/B88_E3.tsv`（**2 行 · 11 列** · 全是「更新」不是「新增」，`merge_ledger_parts.py --dry --only B88_E3` 实测 **更新 2 / 新增 0**）
> ⛔ 未跑 Unity / `-executeMethod` · ⛔ 未动 git · ⛔ 未改正本与账主表 · ⛔ 未重钉 `Pinned*Md5`
> ⚠️ **一处白名单例外（已如实标）**：`工具/ruleprobe/ruleprobe.csproj` 加了 **1 行** `<Compile Include>`（见 §五 ①）。

---

## 〇 一句话结论

**两个已知缺口都补上了**：① **AI 现在会枚举第 ④ 档主动能力**（原版通用那一格 `activeAbilityLogic`；
池内唯一「五缺」的 `SOR66 Armorium Cherub` 从前在 AI 手里**永不发动**）；② **`IsRightContext` 的
fail-closed 支有断言了**（外加一条 AI 枚举的正/负对照断言）。
**6 条 C 账行今天一条都没能升 A** —— 残余**全部**落在本批白名单之外的文件里（`RuleCore.cs` / `EffectResolver.cs` /
`CardDef.cs` / 远端 liveops 数据），逐条给了移交落点与可达性读数（§二）。

| 项 | 读数 |
|---|---|
| `typecheck`（`TMPDIR=/tmp/wf_s88e3`） | **0 / 0**（运行时 0 · 编辑器 0） |
| `ruleprobe check` | **rc=0**（全池 1121 张 · 解析差异 0 行 · 哨兵失败 0 条） |
| `ruleprobe play check` | **rc=1**（指纹 `1ada22c2…` ≠ 基线 `03b6591f…`；**归因见 §三** —— 同批别的写手正在改 `RuleCore.cs` / `EffectResolver.cs` / `CardDef.cs` / `Aura.cs`，我 23:22 的**改动前基线是 rc=0**） |
| `SimpleAI.cs` 行数 | **2228 → 2228**（零漂移；账里 41 处 `SimpleAI.cs:行` 全部不需要重定位） |
| 账补丁 | 2 行（`BattleManager.GetAvailableActions` · `Core.CardAbility.IsRightContext`） |

---

## 一 做了什么

### 1·1 缺口①：AI 不枚举第 ④ 档主动能力 → **已接**

**改前事实**（现跑，不是回忆）：`bash 工具/ruleprobe.sh ai actions SOR66` 的动作表 17 条 =
`playCard 4 · attackMelee 6 · attackRanged 6 · endTurn 1` ⇒ **0 条 `activeAbility`**。
判据侧两条既有记录把成因写得很清楚：`报告/A9_主动能力按钮.md` §3·2（「AI 手里的 SOR66 永远不会主动能力」）
＋ `报告/A11_联机与回放路由.md` §④·2（「将来 AI 若枚举第 ④ 档，打分那支会读空表」）。

**判据**：原版 `AI.GetAvailableActions` 对 `activeAbility`(5) **产一条候选**；打分只读 `ActiveAbility`
自己的两列 —— `AI.GetActionScore` case 5 先 `nonTargetScoringCriteria(+0x58)`、再（`targetCard != null` 时）
`cardTargetScoringCriteria(+0x50)`。全文 → `判据/AI打分与动作选择_第2轮.md` §2·1 的 `activeAbility` 支。

**为什么「打分那支会读空表」这条今天不成立**：我们的 `SimpleAI.ScoreAbility` 早已切到原版数据
（`SimpleAI.cs:721-730` → `AiScoring.ScoreActiveAbility`，`Data/AiScoring.cs:670-698`，**次序与原版同形**：
先 nonTarget、target != null 才跑 cardTarget）。⇒ **缺的只有候选产出**，打的是一行代码的行。

**改动**（两处，`SimpleAI.cs` 零行数变化）：

| 文件 | 行 | 改了什么 |
|---|---|---|
| `Data/SimpleAI.cs` | `:421` | `if (spec == null) continue;` → `if (spec == null) { AddActiveAbilityEntry(ctx, me, s, list); continue; }`（**1→1**）。这个位置正是「`u.Ability == null`」的分叉点 = 前三条等价入口都不适用的那一格 |
| `Data/SimpleAI.cs` | `:123` | `public static class SimpleAI` → `public static partial class SimpleAI`（**1→1**）—— 新口写进兄弟分册，**本文件零行数变化**（否则 41 处 `SimpleAI.cs:行` 落点一起漂） |
| `Data/SimpleAI.ActiveEntry.cs` | 新文件 88 行 | `AddActiveAbilityEntry(...)`：入门谓词（见下）→ `CanStartActiveAbility` 六道闸 →「要不要点目标」（`CanUseActiveAbility(…, -1) == ErrTarget`）→ 每个合法目标一条 / 一条不带目标 |

**新口的四条口径**：

1. **入门谓词 = 与表现层同一个判据**：`HasActiveAbility == 1` ∧ `!u.HasAbility` ∧
   `RuleCore.AlternativeActionKeyword(u) == null` ∧ `u.Card.OathOps.Count == 0`
   —— 逐条照 `CardPresentation/Battle/BattleDriver.cs:36148` 的 `IsActiveAbilityEntry`。
   ⚠️ **少一条就会给 86 张「已经有入口」的卡多产一条重复动作**（现池 87 张 `hasActiveAbility==1`，
   只有 `SOR66` 五缺）。
2. **候选侧 = 己方**：引擎那一口 `RuleCore.CanUseActiveAbility` 的 `targetOwner` 写死成施放者（`RuleCore.cs:15341`
   的 `CanUseAbilityTarget(ctx, p, u, p, targetSlot)`）⇒ **⛔ 别照「普通主动技」那支扫敌方**（会一条候选也产不出来）。
3. **判不了不猜、也不挡死**：`ResolveActiveAbilityRows` 返回 null 时照旧产候选，由结算侧
   `RuleCore.UseActiveAbility` 出声 + `ErrUnimplemented`（与表现层「钮给不给」同一条口径）。
4. **动作仍走 `SimpleAI.ExecuteAction`**（铁律：录像/联机只认这一个记账口）—— 它的 `ActiveAbility` 支
   早已由 `A11` 接好：`AltKeyword == AltActiveAbility` ⇒ `RuleCore.UseActiveAbility`（`SimpleAI.cs:1691-1694`
   ＋ 类尾 `UseAbilityOrActive`），**本批一个字节没动**。

**为什么这条能让 AI 真的用起来（不是「产了条 0 分动作」）**：`ai_scoring.json` 里 `SOR66` 的
`activeAbility.nonTargetScoringCriteria` 有 1 条（`scoringOption 2` = `SumValueInPlay`、
`multiplyByFriend 1.0`、`targetCriteria.friendlyUnits = true`）⇒ `ScoreFromCriteria` 会把**己方小兵的在场上价值求和**
（现读 `Data/AiScoring.cs:173-179` 的 case 2）。⚠️ 这只是「有分」，**它不是 0 分**是我读代码得到的，
**没跑实局**（见 §四 ①）。

### 1·2 缺口②：`IsRightContext` 新分支的断言 → **已加**（`Editor/RuleEngineTest_EngineA1.cs`）

挂在**既有用例** `A1MissingAbilityContext()` 末尾（⛔ 不新开 `Test*`：`Run()` 的 `RunCaseNames` ↔ 反射对账
要求每个 `Test*` 各有一条 `Step`，而 `RuleEngineTest.cs` 不在本批白名单）。

四条断言（每条带 🧨 ＋ 一条**硬前提**，⛔ 不是 `if (前提) Check(结论)`）：

| 断言 | 期望 | 🧨 什么能让它红 |
|---|---|---|
| `IsRightContext(ctx,0,3,Play,u.Instance)` | `true`（**前提**） | 夹具没摆上 / 传错 `Instance` ⇒ 后面几条的 false 就不算证据 |
| `IsRightContext(ctx,0,3,999,…)` | **`false`**（原版 fail-closed，`CardAbility__IsRightContext.c:84-90`） | 把 `default:` 支改回 `return true`（= 账 679 之前那一版） |
| `IsRightContext(ctx,0,3,7,…)` | `false` | `default:` 改成「按区间就近归类」 |
| `IsRightContext(ctx,0,3,-2,…)` | `false` | 早退条件从 `context == -1` 放宽成 `context < 0` |
| `IsRightContext(ctx,0,3,-1,…)` | **`true`**（🔶 自己设计：`-1` = 「我们没落这一列」的数据缺口，保护 `Core/Enchantment.cs:906` 那条不过闸的调用点；调度台 2026-10-27 裁定保留） | 把 `-1` 也一起 fail-closed ⇒ 那批附魔的 `AboutToAttack` 不再取消攻击 |

⚠️ **没加「日志里有那句 `LogWarning`」的断言** —— 批处理里取证不稳定，加了就是假红。

### 1·3 顺带给 AI 枚举加了断言（`Editor/RuleEngineTest_EngineF8.cs`）

挂在既有用例 `TestF8EnginePlayOrder()`（该文件是 AI 侧引擎判据的家；新增的是**辅助方法** `F8ActiveAbilityEntryEnumerated`
—— ⛔ 不用 `Test*` 打头，否则 `Run()` 对账漂）。三档：

1. **正面**：④ 档夹具（`hasActiveAbility=1` ＋ `activeAbilityLogic` 一行 `ta=30 / chosen={30,462}`）⇒
   动作表里有 `Kind=activeAbility(5)` ∧ `AltKeyword="active"` 的行，落在**载体格**上、`TargetP == 0`（己方）；
   附三条硬前提（`hasActiveAbility==1` / 前三条口都不存在 / `CanStartActiveAbility == OK`）。
2. **负对照 A**：同一份夹具 `hasActiveAbility = 0` ⇒ **0 条**。⚠️ **判别力如实标**：这条由**两道闸的合取**成立
   （谓词 ∧ 引擎 `CanStartActiveAbility` 的 `ErrNoAbility`）⇒ **只删谓词那一行它仍是 0、不会红**；真能让它红的是
   「两道闸一起放宽」。⛔ 断言文案里已经把这句话写进去了，别当它是「谓词那行的守门员」。
3. **负对照 B**：带 `duty` 的同类卡 ⇒ **有 `duty` 候选、没有第 ④ 档候选**（🧨 删掉「替代行动已存在就排除」
   ⇒ 现池 86 张会各多一条重复动作）—— **这条是真有判别力的那一条**。

⚠️ **这两组断言我一条都没跑过**（红线：本代理不跑 Unity）⇒ 它们是「写好的、待调度台跑」的状态，
`RuleEngineTest.Run` 是唯一判据。我在写之前逐条核过夹具前提（见 §四 ① 的说明）。

---

## 二 六条 C 账行：逐条为什么今天仍不能升 A

`ledger_slice.py --file SimpleAI --status C` = 1 行 · `--file ActionPump --status C` = 5 行 ⇒ **共 6 行**（= 简报里的「约 8 条」实际是 6）。
**一条都没有能落的**，原因**全部**是「判据要动的文件在本批白名单之外」，⛔ 不是「影响小」也不是「成本高」：

| 账行 | 方法 | 残余 = 卡在哪 | 移交 |
|---|---|---|---|
| 172 | `AddTransformCardWithBuff` | 整条 `transformUnit` 机制未建：出队体等价函数（换卡面/换定义 + `keepEffects`/`buffEffect`）需「把一张在场单位换成另一张卡」的核心口 → `RuleCore.cs`；产生它的动词分派 → `EffectResolver.cs`；变身字段 → `CardDef.cs`。⛔ 在 `ActionPump` 里加一个没有发射口的解析器 = 死代码（本文件类尾 §八 `synapse` 先例） | **B01 + B02 + B05** |
| 178 | `AddTriggerCourtesy` | 本件侧（入队/出队/三槽/关键词映射）**逐句对上**；残余 ① `CardDef.Prefixes` 缺 `courtesy` 前缀 + `KeywordTable` 无 `Courtesy` 常量（写这个词会被 `Normalize` 静默丢）② 原版唯一调用点 `AbilityLogic__PlayAbility.c:2102` 的 `chosenAbility 400` 分派 | ① **B05** ② **B02** |
| 183 | `AddTriggerRelentless` | 与 178 **同一份入口**（两份 `.c` 去掉常数后完全一致）⇒ 残余只剩 `KeywordTable` 常量（`relentless` 前缀 B8_D 已登记）＋ `chosenAbility 370` 分派 | ① **B05** ② **B02** |
| 669 | `BroadcastWhileInPlay` | 场上半**在位**（`ActionPump.cs:143` 的 `Auras.RefreshInPlayNumericAndKeywords`，在 `EmitWhileInPlayPass(:157)` 之前）；残余 = **手牌半**（原版对双方手牌逐张 `UpdateWhileInPlay` ⇒ `AddHandEffect`）：要 `CardDef` 收 per-count 自降费列 + `RuleCore.CostOf` 读它。现读 4 张卡可达（`ASH85`/`GSC37` 已被等价覆盖，`BL52`/`SW58` 的手牌价**确实没减**——A6 已记账） | **B05 + B01** |
| 676 | `BoardAnalysis.MeetsCriteria` | 残余 ③ `RandomChance(8)` 掷法（等 P-04 全局随机源）· ⑥ 卡级 `countCriteria.conditionsFilter` 要四处同批改（`CardDef.BoardGateSpec` / `CardDef.UsesAlternativeAbility` / `CardDatabase.MapBoardGate` / `工具/gen_cards_engine.py`）—— 现读池内 2 张该值均 0 | ③ **P-04** · ⑥ **B05 + B02** |
| 48 | `BotConfigFTUE.GetBotDecks` | **数据在远端 liveops**（本地 0 个文件，2026-10-26 已三源搜证）；FTUE 的 bot 配置/胜场状态机未建（界面那半已建）。时间限定 = 本 build | 远端 / 教程线 |

**本批我为这 6 行做了什么**：逐条复核「残余是否仍成立」＋把结论写进补丁与 §三。**没有为了「有产出」去碰别人的文件。**
> 🔴 补丁里我**只更新了 2 行**（下面 §二之一），没有把上面 6 行重打一遍 —— 理由：它们今天的内容是 `A6`（2026-10-27）
> 刚写并已合并的，我若整行重写只会把**别的代理同时写进去的修订盖回去**（这正是 2026-10-26 那次事故的形状）。
> 我这轮对这 6 行**没有任何新事实**，故不动它们。

### 2·1 补丁里的 2 行

| 行 | 为什么改 |
|---|---|
| `BattleManager.GetAvailableActions(bool isPlayer, bool allowAutoActions)`（状态 **B**，落点 `SimpleAI.cs:316-451`） | 这是 `EnumerateActions` 对应的账行 —— 本轮把「第 ④ 档不枚举」这一块**补上了**，差异/备注各追加一段（含残余四段未核、谓词是第二份副本、读数口缺口）。**状态不动**（原版那 370 行里其余四段仍未逐段核，不配升 A）。 |
| `Core.CardAbility.IsRightContext(CardScript thisCard)`（状态 **A**，A6 本轮已升） | 只追加备注：fail-closed 支**现在有断言了**（哪四条、钉的是返回值不是日志）。**状态/落点不动。** |

---

## 三 验收读数（含 `play check` rc=1 的归因）

| 项 | 命令 | 读数 |
|---|---|---|
| 秒级类型检查 | `TMPDIR=/tmp/wf_s88e3 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0**（最后一次；中途两次红全部落在 `CardPresentation/Shell/TutorialModePopup.cs:775` 的 `ItemDone` 重名 —— **别人的文件、别人在写**，按铁律 13·3 不当自己的错去改） |
| 卡池读取 | `bash 工具/ruleprobe.sh check` | **rc=0** · 全池 1121 张 · 解析差异 0 行 · 哨兵失败 0 条 |
| 结算对拍 | `bash 工具/ruleprobe.sh play check` | **rc=1** —— 现算指纹 `1ada22c2c287445df2dcad1c35c317b225` vs 基线 `03b6591f93dd9bf6f8649624553d6666`；①-a **1 张卡变了**（`GOF57`：`selfPlace 0→1`）· ①-b **32 张卡变了**（`AM11/AM12/AM28/AM30/AM34/AM35/AM38/AM59/AM68 …`，全部差在 `net` 与 `netFull` 的**多出来一段**） |
| AI 动作表 | `bash 工具/ruleprobe.sh ai actions SOR66` | 改前/改后 **都是 17 条**、`[按 Kind 计数] attackMelee 6 · attackRanged 6 · playCard 4 · endTurn 1` —— **看不见变化**（原因见下） |

### 3·1 🔴 `play check` rc=1 **不是本笔造成的**（证据链，供调度台复核）

1. **我动手之前**（23:22:05）跑过一次：**rc=0**、`✓ 基线文件的内容与钉住的一致（03b6591f…）`。
2. 我的改动**结构上到不了结算层**：全仓 `SimpleAI.EnumerateActions` 的调用点只有
   `CardPresentation/Editor/NetBattleTest.cs:1821`、`RuleEngine/Editor/RuleEngineTest.cs:28786/28788/28805/28825`、
   `SimpleAI` 内部几处、以及 `工具/ruleprobe/AiProbe.cs`；**`PlayProbe` 一次都不调**（本轮 grep 现核）⇒
   `play check` 的 1129 张逐卡结算**不会经过我改的那一行**。
3. **同批别的写手正在改引擎**（文件 mtime 现读，全部晚于我 23:22 的绿基线）：
   `Core/EffectResolver.cs` 23:25 · `Core/CardDef.cs` 23:25 · `Core/Aura.cs` 23:27 · `Core/RuleCore.cs` 23:28。
   差异本身的形状也指向他们：变化的列是 `net` / `netFull` 里**多出来的第三段事件**
   （`✚ 落场/生成 P0[5] …`、`✚ 手牌 P0 +「filler14」`、`✚ 弃牌 P1 +「FIX_…」`），
   以及 `GOF57` 的 `selfPlace`；这些是**结算正文**的口，不是 AI 的口。
4. ⇒ **判据**：请调度台在「没有 `.cs` 写手」的窗口重跑 `play check`；若仍 rc=1，再按上面那 4 个文件的
   `git diff` 逐笔归因。**我没有重钉基线**（红线），也**没有**去改别人的文件来把绿凑回来。

### 3·2 ⚠️ `ai actions` 看不见我这笔：**读数口本身有个结构性缺口**（顺手发现，见 §五 ②）

`AiProbe` 的夹具把**被测卡放在手牌**、盘上只有一张探针用 `AVFILL` ⇒ 第 ④ 档是**盘上单位**的入口，
所以这个读数口**结构上看不到它**。⇒ 本轮 ④ 档的唯一机器判据是 §1·3 那组断言（待 `RuleEngineTest.Run`）。

---

## 四 没查清的部分（如实标）

1. **④ 档「AI 真的会挑它、结算真的成功」没跑过实局**。我能确证的只有三件：引擎口是通的
   （`BattleDriver.DoResolve` → `RuleCore.UseActiveAbility`，`A9` 已验）；打分数据在（`SOR66` 的
   `nonTargetScoringCriteria` = `SumValueInPlay` friendly ⇒ **非 0**）；候选的产出逻辑（读完代码＋写了断言）。
   **没确证**：一局里 SOR66 在场时 AI 的实际选择（要跑 `RuleEngineTest.Run` 或真 Play）。
2. **断言的两处风险点（我逐条核过，但没跑）**：
   · `A1` 那组依赖 `Battle(...)`＋`Place(...)` 的老夹具语义（`Place` 会用 `ctx.NewInstance`／`TakePlayOrder`）——
     与同文件既有断言同源，风险低；
   · `F8` 的负对照 B 依赖「`duty` 关键词的合成卡真的产出 `duty` 候选」（既有 `RuleEngineTest.cs` 的「⑦ 动作表覆盖」
     用例已证），我加了一条**前提断言**把它先钉住 —— 若前提不成立，会**先红前提**而不是给出假绿。
3. **`AddActiveAbilityEntry` 的入门谓词是第二份副本**（`BattleDriver.IsActiveAbilityEntry` 是第一份）。
   收口方式明确（`RuleCore` 开一个 public 谓词、两边转发），但 `RuleCore.cs` 属 `E1` ⇒ **未做**，已进账（§二之一 的差异列）。
4. `play check` 的 33 张变化**没有逐卡归因到具体代理**（只到文件 mtime 与差异形状）—— 那是调度台在无写手窗口的活。

---

## 五 改动清单 + 顺手发现

### 5·1 改动清单（文件 : 行）

| 文件 | 改了什么 |
|---|---|
| `MyGame/Assets/RuleEngine/Data/SimpleAI.cs` | `:123` `class`→`partial`（1→1）· `:421` 1→1（调新口）。**行数 2228 → 2228，零漂移**；`CRLF 0 / LF 2228 / 裸 CR 0` |
| `MyGame/Assets/RuleEngine/Data/SimpleAI.ActiveEntry.cs` | **新文件 88 行**（LF · 无裸 CR）· `public static partial class SimpleAI` · `AddActiveAbilityEntry(...)` |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineA1.cs` | `A1MissingAbilityContext()` 末尾 +36 行（4 条断言 ＋ 1 条硬前提 ＋ 判据注释） |
| `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineF8.cs` | `TestF8EnginePlayOrder()` 里 +1 行调用 · 文件尾 +144 行（`F8ActiveAbilityEntryEnumerated`） |
| `工具/ruleprobe/ruleprobe.csproj` | **+1 行** `<Compile Include="…/Data/SimpleAI.ActiveEntry.cs" />`（**白名单例外**，理由见 §五 ②） |
| `资料/完全复刻/账/_parts/B88_E3.tsv` | 补丁 2 行（表头 11 列逐字照主表；`--dry --only B88_E3` ⇒ 更新 2 / 新增 0；`check_patch_conflicts.py` 里**没有** `B88_E3` 参与的重复键） |
| `资料/完全复刻/报告/B88_E3_动作泵与AI.md` | 本文件 |

### 5·2 顺手发现（⛔ 都没越界改，请调度台分流）

1. 🔴 **`工具/ruleprobe/ruleprobe.csproj` 关了 `EnableDefaultCompileItems`** ⇒ 以后任何**新建**的 `.cs`
   只要被编进探针的文件引用（本轮：`SimpleAI.cs:421` 调兄弟分册的方法），`ruleprobe` 会在**构建期**报
   `CS0103` 并**拒绝用旧 dll 跑**（这一条设计是对的）。为了不让**所有人的**读数口一起坏掉，我加了 1 行；
   但它确实在白名单外 ⇒ 请调度台确认。**更好的长期做法**：给 `Data/*.cs` 用通配（只排掉 `DeckStore` 那种要
   `persistentDataPath` 的），这样每次新开 partial 都不用动 csproj。
2. ⚠️ **`ruleprobe ai actions` 结构上看不见「盘上单位」的候选**（夹具把被测卡放手牌）⇒ 第 ④ 档
   （以及任何「只有上场的单位才产」的候选）在这个口上**不可观测**。建议给 `AiProbe` 加一档
   `ai board <卡id>`（把被测卡直接摆上棋盘再枚举）。**白名单外，未做。**
3. ⚠️ **`play check` 正被同批写手搅动**（33 张卡的 `net/netFull` 多出一段事件）。这不是缺陷报告，
   只是提醒：**同步点重跑之前先确认没有 `.cs` 写手**（`RuleCore.cs` / `EffectResolver.cs` / `CardDef.cs` / `Aura.cs`
   在 23:25–23:28 之间都还在被写）。
4. ℹ️ **`CardPresentation/Shell/TutorialModePopup.cs:775` 与 `:203` 的 `ItemDone` 重名**（`CS0102`：
   一个 `static readonly PxRect ItemDone` 字段 + 一个 `string ItemDone()` 方法）—— 我 23:24 与 23:26 两次类型检查都撞上它，
   23:30 那次已修好。**提醒那个代理：改完记得跑 typecheck**（那一版会让**所有人**的编辑器程序集编不出来）。
5. ℹ️ **口径提示给后面的代理**：`SimpleAI.ScoreAbility` 对**没有 `ai_scoring` 条目**的卡（自检夹具）走的是
   `ScoreOps` 兜底，而 `AltKeyword == "active"` 时 `TriggerOps("active")` 恒 `null` ⇒ **那一路恒 0 分**。
   这是**预期**的（原版 `GetActiveAbility()==null` ⇒ warning + 0 分），但如果将来有真卡缺 `ai_scoring` 条目，
   AI 就会「枚举得出来、永远不选」—— 那时该补的是**数据**，⛔ 不是在 `ScoreAbility` 里编一个分数。

---

# 第二批（调度台 2026-10-27 追加）

## 一 读数口盲区已补：`ai board <卡id>`（`工具/ruleprobe/AiProbe.cs`）

**为什么加**：`ai actions` 的夹具把**被测卡放手里** ⇒ 只有上场单位才产的候选（第 ④ 档就是这种）
在那个口上**结构上不可观测**（`ai actions SOR66` 改前改后都是 17 条、无 `activeAbility`）。

**新档做三件事**（`bash 工具/ruleprobe.sh ai board SOR66`，rc=0）：
① 把被测卡**直接摆上盘**（`ProbeSlot=3`；友方小兵 5；敌方 3/5；督军 4）→ 打动作表 + 逐条 `ScoreAction`；
② 单挑出**第 ④ 档候选**，用 `AiScoring.TraceCriteria` 打**打分逐条贡献**；
③ 在**同 seed 的第二份夹具**上强制 `SimpleAI.ExecuteAction` 那条候选，打前后局面。

### 1·1 实测读数（2026-10-27 23:35）

```
[④ 档五道读数] HasActiveAbility=1 · HasAbility=0 · 替代行动=- · 誓约 ops=0
               · CanStartActiveAbility=0(OK) · CanUseActiveAbility(-1)=10(ErrTarget ⇒ 要选目标)
[① 动作表] 共 25 条 · [按 Kind 计数] attackMelee 9 · attackRanged 9 · playCard 3 · **activeAbility 3** · endTurn 1
  [21] activeAbility(5) slot=3 →P0@3 alt=active   [22] →P0@4   [23] →P0@5     ← **改前 = 0 条**
[② 第 ④ 档候选] 3 条（目标侧 = **己方** 3/4/5 号格：自己 / 督军 / 友方小兵）
  显式喂 dto 的真实分：@3 = **3** · @4 = **1** · @5 = **5**（`ability.nonTarget[0] option=2 ⇒ v`）
[③c 若 dto 在位（= Unity 里）] ④ 档最高 = 5 vs 其余动作最高 = 3（playCard）⇒ **AI 会挑 ④ 档**
[③b 实打] ExecuteAction = **True**
  前：P0[3]Armorium Cherub 1/0/2 · 能量 20
  后：P0[3]Armorium Cherub 1/**2**/2 **已行动** · 能量 20 · ctx.Events = 4
```

**三条结论**：
1. **AI 会用它**：Unity 里 `AiScoringData` 在位 ⇒ ④ 档候选拿 **5 分**（= `SumValueInPlay` 打在「5 号格友方小兵」上 = `2×2+1`），
   高于本夹具里所有攻击（最高 1.3）与出牌（3）⇒ `SelectBestAction` 会挑它。⚠️ 这条是**由两条读数推出来**的交叉比较，
   **不是**离屏跑了一遍决策（离屏 `AiScoringData` 恒 null ⇒ 那条路把 ④ 档算 0）。
2. **结算真的成功**：`ExecuteAction = True`；局面变化与判据吻合 —— 目标（自己）**远程 0→2**
   （`buffOthers(30)`，与 `A9` 报告 §2·2 的「目标远程 2→4 净 +2」同一个 op）、
   发动者 **`Exhausted` 假→真**（`ResolveActiveAbilityPlayed` 的动作经济）、`Events = 4`；
   能量没变是因为 `ActiveAbilityManaCost` 未知（`CanStartActiveAbility` 那条「未知 ⇒ 不收费、不拦」的口径）。
3. 🔴 **离屏桩的口径差（新发现，已写进读数文案里）**：`SimpleAI.ScoreAbility` 走 `AiScoringData.Get(card)`，
   而 `AiScoringData.EnsureLoaded` 用 `Resources.Load<TextAsset>` —— 离屏是桩、恒 null ⇒ 产品那条路
   在探针里退化成 `ScoreOps(TriggerOps("active"))` = **恒 0**。所以新档加了 `[显式喂 dto]` 一行
   （把同一份 `ai_scoring.json` 的 dto **显式**喂给**同一个函数** `AiScoring.ScoreActiveAbility`）。
   ⚠️ 同族盲区：**任何**依赖 `AiScoringData` 的读数在离屏都是 0 —— 以后看到「分恒 0」先查这一条。

## 二 白名单增量

| 文件 | 改了什么 |
|---|---|
| `工具/ruleprobe/AiProbe.cs` | 新档 `ai board <卡id>`（`RunBoard` / `BuildBoardFixture` / `BoardSlotOf` / `BoardLine`）；`Run()` 里 +1 行分发；用法串补一档。⛔ `RunActions` 一个字没动（老输出零变化） |
| `工具/ruleprobe/ruleprobe.csproj` | 上一批已加的 1 行 `Compile Include`（本批未再动） |

## 三 B22 挑战引擎批次（`日常_奖励`，调度台追加）

**取行**：`ledger_slice --domain 日常_奖励 --status C` = **121 行**；与挑战相关的共 **19 条**。分堆如下：

| 分堆 | 条数 | 处置 |
|---|---|---|
| `BattleEventsController.CheckHealth` / `Update` | 2 | 🔴 **归 `B88_E5`（已判 A）** —— 等价实现在 `BattleDriver.UpdateHud` 的 `_foeSkullHpSeen` / `_myWarlordMinHp` 内联缓存。⛔ **我不重算血量差**（同一规则第二份） |
| `ChallengeButton`（Awake/Initialize/OnButtonClick/OnSignalRaise/Toggle） | 5 | 模型侧已建 ⇒ 4 条升 **B**；`OnButtonClick` 留 **C**（窗在 `DuelPopupWindow.cs` = 黑名单） |
| `ChallengeLogMgr`（LogAddEffect / LogHeal / TroopKilled） | 3 | 计数口 + 消费端已建 ⇒ **B**（发射点在结算侧，白名单外） |
| `MissionChallengeProgress.GetChallengeRefillTime` | 1 | 循环补时语义已建 ⇒ **B**（数值自设计） |
| `ChallengeManager.*`（练习邀约 / 私聊 / 在线查询 / 决斗状态机） | 12 | **全部留 C** —— 前提是「服务端有对手/有私聊」，落点在 `ChatPanel.cs`（E9）/`DuelPopupWindow.cs`（黑名单）⇒ 归 **P3 服务器**那一档 |

### 3·1 新建两个文件（白名单内）

| 文件 | 行 | 内容 |
|---|---|---|
| `CardPresentation/Shell/ChallengeData.cs` | 217 | 待处理练习挑战队列（`PendingPracticeMatches`，**显隐唯一判据**）· 挑战定义（🔶 自建 5 条）· 五格计数（`TroopsKilled`/`HealAmount`/`EffectsAdded`/`DamageEnemyWarlord`/`SurviveTurns`，**唯一正本**）· 循环补时（`GetChallengeRefillTime` ＋ `MarkRefillConsumed`）· `ChallengeMatchNotificationProvider`（**复用**既有 `UiBadgeNotificationManager`，⛔ 没另建总线） |
| `CardPresentation/Shell/ChallengeEngine.cs` | 147 | 记数口（`TroopKilled(bool isTroop)` / `LogHeal(int)` / `LogAddEffect()` / `OnWarlordDamaged` / `SurviveTurn`）＝ **`ChallengeLogMgr` 的等价物**；挑战钮模型（`Initialize` / `Toggle` / `Visible` / `OnSignalRaise` / `Attach` / `Detach`）；进度（`ProgressOf`）；复位 |

### 3·2 四条口径（避免「建了信号没人收」与「重复施工」）

1. 🔴 **`CheckHealth`/`Update` 不重复施工**：协调者点名的那三条（`Initialize` / `CheckHealth` / `Update`）里，
   后两条已在 `B88_E5` 的 A 级落点里 ⇒ 本批**只在补丁里引用一次**，且 `ChallengeEngine` **不重算血量差**
   （只留 `OnWarlordDamaged(bool,int)` 这个**计数**口）。
2. 🔴 **计数口有消费端**：每条记录都落到 `ChallengeData` 的五格，由 `ChallengeEngine.ProgressOf` ＋
   `ChallengeData.IsComplete` 读（挑战进度）⇒ **不是「只建信号没人收」**；同时 `UiBadgeNotificationManager.Refresh`
   让读进度的界面重算（复用既有总线，⛔ 无第二条总线）。
3. ⛔ **没有 drain `ctx.Signals`**：那是表现层的地盘（`DrainSignals` 搬完即空 ⇒ 在这里搬会静默偷走特效信号）。
4. 🔴 **`TroopKilled(isTroop)` 的 `false` 不计数** —— 督军倒下不算「部队击杀」（原版那一条只服务 `TroopsKilledCount` 类挑战）。
5. 🔶 **挑战内容全自建**：`assets_full` 90 bundle 全量搜 `ChallengeList`/`MissionChallenge`/`MissionsConfig` = **0 资产**
   （只有类定义）⇒ 清单/目标值/补时间隔（4 小时）全部按铁律 18 打 `🔶 自己设计`。

### 3·3 补丁与验收

* `账/_parts/B88_E3b.tsv`：**9 行**（8 升 B ＋ 1 行留 C）。`merge_ledger_parts.py --dry --only B88_E3b` ⇒ **更新 9 / 新增 0**；
  `check_patch_conflicts.py` 里 **`B88_E3b` 0 条**冲突（先前的 2 条 `CheckHealth`/`Update` 冲突已随「让给 E5」消除）。
* `typecheck`（`TMPDIR=/tmp/wf_s88e3b`）⇒ **0 / 0**。
* ⚠️ **8 条 B 的共同残余**（都写进了各自备注）：**发射点/视图侧在别人文件里** ——
  计数发射点 = `RuleCore`/`EffectResolver`/`BattleDriver`（E1/E2/E5）；`SetActive` = `Shell/TopBar.cs`；确认框 = `DuelPopupWindow.cs`（黑名单）。
  本批交付的是**机制与数据（唯一正本）**，⛔ 没有为了「有数」去改别人的文件或编一个假源。
* ⚠️ **没跑 Unity**（红线）⇒ 这三个新件的运行时行为**没有任何断言/实机验证**（`ChallengeData`/`ChallengeEngine` 属表现层外壳，
  宿主自检是 `ShellScene.Run`，由调度台在同步点跑）。
* `play check` 在 23:40 仍是 **rc=1**（指纹 `a919fdeb…`，比 23:29 的 `1ada22c2…` 又变了 ⇒ 同批写手仍在改引擎）。
  本批两个新文件在 `CardPresentation/Shell/`、**不在 `ruleprobe.csproj` 的编译项里** ⇒ 结构上不可能影响该指纹。

## 四 第三批：`日常_奖励` 剩余 C（本轮只做了 `TopBar` 那一簇的「挑战钮」）

### 4·1 本轮落地：挑战钮的**视图半边**（账 24/25/28/29 → **A**）

* `Shell/TopBar.cs`：`if (challenge != null) challenge.gameObject.SetActive(false);` ⇒ `WireChallengeButton(challenge.gameObject)`
  ＋ `Parts.ChallengeRoot`（新字段，追加在 `Parts` 末尾）＋ 两个静态成员 `_challengeRoot` / `_challengeApply` ＋
  `WireChallengeButton(...)`（`TopBar.cs:336`）＋ `ApplyChallengeVisible(bool)`。
* 口径：**判据不复制** —— TopBar 只做「`ChallengeEngine.Visible` 变了就 `SetActive`」那一跳；
  判据在 `ChallengeEngine.Initialize()`（= `PendingPracticeMatches.Count > 0`，账 9 备注）。
  重复 Build 只留一份订阅（`-=` 再 `+=`）；`ChallengeEngine.Attach()` 幂等。
* 出厂态**仍是关**（本地 0 条待处理挑战）⇒ 与 `A1523①` 的两条实况判据（dump `activeSelf False` ＋ 实拍该格 max 亮度 30）**一致**。
* 账 26（`OnButtonClick`）**仍 C**：确认框在 `Shell/DuelPopupWindow.cs`（**黑名单**），且「收到挑战」要服务端/邀约协议 ⇒ 归 **P3**。

### 4·2 施工清单（⛔ 不在本批白名单/黑名单里，交调度台统一派）

**① `ChallengeEngine` 五格计数的「发射点」**（每处 **1 行**，都是「结算那一刻」的回调点）：

| 计数 | 该在哪加 | 加什么 |
|---|---|---|
| `TroopsKilled` | `RuleEngine/Core/RuleCore.cs` 的 `CleanupDeaths`（死者离场那一段，E1 的地盘） | `ChallengeEngine.TroopKilled(!dead.IsWarlord)` |
| `HealAmount` | `RuleEngine/Core/EffectResolver.cs` 治疗结算成功处（E2） | `ChallengeEngine.LogHeal(实际治疗量)` |
| `EffectsAdded` | 同上，`ApplyBuff`/附魔挂载成功处（E2） | `ChallengeEngine.LogAddEffect()` |
| `DamageEnemyWarlord` | `RuleCore` 督军受伤处（E1） | `ChallengeEngine.OnWarlordDamaged(false, 实际伤害)` |
| `SurviveTurns` | 回合开始（`RuleCore.BeginTurn`，E1） | `ChallengeEngine.SurviveTurn()` |
| 复位 | 开局建局处（`BattleDriver` / `RuleCore.NewBattle`，E5/E1） | `ChallengeEngine.Reset()` |

⚠️ 这 6 处**都不许在 `BattleDriver.UpdateHud` 那条线里再比一次血量差**（E5 的内联是唯一一处，协调者已确认）。

**② `BoostTimerPanel`（账 20-23，4 行 · 落点 `TopBar.cs（加成剩余时间）`）** —— 整条 Boost 线未建（全仓 `BoostItem`/`BoostTimerPanel`/`BoostMenu` 零命中）。
判据已知：`UpdateTimer()` 的三档格式 **≥4 天 ⇒ `n d` · ≥1 小时 ⇒ `hh:mm:ss` · 否则 `mm:ss`**；
`SetDescription` / `SetTimer` 各一格 `Label`，`Init(BoostItem)` 取一件 boost。
⚠️ **数据源是服务端**（boost 由 PlayFab 发）⇒ 只能靠自建 `BoostItem`（🔶）＋ 计时；**建议与「不做真实经济」口径一起裁**。

**③ `OpenEventButton`（账 546/549/550，3 行 · `TopBar` + `MainMenuRuntime`）**：
`Initialize` 的红点（`EventComponents[EventBadge]` 逐条 `AddNotificationType`）· `SetButtonInfo` 的**标题**
（原版 `NavigationBarTitle ?? Title`，`LiveOpsLocalCatalog` 已有 13 份 SO 的标题键）· `overIcon` · `ToggleValueChange`
（反选时关 `currentWindow`）。⚠️ 其中 `MainMenuRuntime.cs` 本批**不在白名单**，且它此刻正被别人改（见 §五）。

**④ 其余（本轮未动）**：`EnergySinglePlayerOnlyEventWindow`(7) · `RaidEventWindow`(4) · `LiveOpsEventWindow`(3) ·
`DailyData`(3) · `DailyRewardPopup`(2) · `PracticeModePopup`(3) · `Ranked/SkirmishEventWindow`(4) · **58 条无落点**
（绝大多数是 LiveOps 服务器件 / `LiveOpsAssetUtility` 一族 / Event 角标绘制 —— 先看 `判据/B88_无落点C_判据总表.tsv` 里 **域 = 日常_奖励** 的结论）。

### 4·3 本轮读数

* `typecheck`（`TMPDIR=/tmp/wf_s88e3c`）：**两次都红，且错误全部落在 `CardPresentation/Shell/MainMenuRuntime.cs`**
  （先是 `TryGetEventNavInfo` 找不到，再是 `:3802` 的实参类型不匹配 —— **那个文件正被别人写**，按铁律 13·3 不当自己的错去改）。
  ⇒ 本批的 `TopBar.cs` / `ChallengeEngine.cs` **编译态未最终确认**，交调度台在该文件稳定后跑一次 `typecheck`。
* 补丁：`账/_parts/B88_E3b.tsv` **就地升级**（4 行 B→A），仍是 9 行 · `--dry --only B88_E3b` ⇒ 更新 9 / 新增 0 · 冲突 0。
  ⛔ 没有另开 `B88_E3c` 去写同样这 4 行（那会造成跨补丁同键、谁的字典序赢是巧合不是语义）。
