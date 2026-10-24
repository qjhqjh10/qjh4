# G12 · `activeAbilityLogic`（v23）接进引擎 —— 执行报告

> 引擎接线代理 `G12` · 2026-10-24
> 上游：`资料/普查产出_1024/D11_activeAbility列.md`（数据侧，132 张 / 193 行，版本 22→23）
> 前置判死：`资料/普查产出_1024/D10_abilityTargets管线.md`（**不能**并进 `abilityTargets`）
> 判据（第一权威）＝ 全量反编译 `d:/2/tools/decomp_full/` 逐行读过的那几处（行号见各节）。
> 类型检查：`TMPDIR=/tmp/wf_g12 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑过 3 次）。
> ⛔ 没跑 Unity 批处理、没动 git、没改正本、没越白名单。

---

## 〇 · 一句话结论

`activeAbilityLogic` 已经 **读得进来、选得出来、挂得上去**，但**只挂了「1 个能力对象 ↔ 1 行 logic ↔ 1 条 op」这一档（38 / 87 张 `gate=1` 的卡）**；
其余 **48 张留 `-1 = 读不到`**，因为**行数 ≠ op 数**（`ta` 数组是「目标集合」列表，不是卡面从句列表）——
按行号硬配会**静默贴错目标**，红线禁止。**还差什么**写在 §三·4。

---

## 一 · 逐项（做了什么 / 为什么没做）

### ① `CardDatabase`：DTO ＋ 映射 ＋ 版本守卫 —— ✅ 做完

文件 = **`Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs`**（⚠️ 简报白名单写的是 `Core/CardDatabase.cs`，
**那个路径不存在**；D11 §六 与实文件都是 `Data/`，本代理按实文件改，见 §六·1）。

| 位置 | 内容 |
|---|---|
| `CardDto.activeAbilityLogic`（新增字段） | `ActiveAbilityLogicDto[]`；doc 写清「每张卡都落 / 缺列 = 未知 / ⛔ 别并进 `abilityTargets`」 |
| `CardDatabase.ActiveAbilityLogicDto`（新类） | 字段名逐字 = JSON 键名 `ab` / `gate` / `trigger` / `needsLoading` / `ta` / `fm`；`ab`·`gate` 的缺键哨兵 = **`MissingIndex`（-99）**（理由：`-1` / `-2` 都是合法 `ab`、`-1` 也是合法 `gate` —— 与 `AbilityGateDto` 同一写法） |
| `version < 23` 守卫（照 `:982` 那处写法） | 「旧表没有 `activeAbilityLogic` 一列 ⇒ `CardDef.ActiveAbilityLogic` 是 **null = 未知** ⇒ 那批 op 的 `ActiveAbilityRow` / `SubjectTargetsAffected` / `AbilityFurtherMultiplier` 一律保持 **-1**（⛔ 不是「原版说没有目标」）」 |
| `MapActiveAbilityLogic(CardDto)` | `null` 进 `null` 出（**缺列 = 未知**）；逐行形状校验：`ab` ∉ {-1,-2} / `gate` ∉ {1,0,-1} ⇒ **出声丢整行**；`ta`·`fm` 格 `< -1` ⇒ 出声落 `-1`；`ta.Length != fm.Length` ⇒ 出声 + **两格置 null（未知）**（逐行对齐的前提没了） |
| `Parse` 里接线 | `activeAbilityLogic: MapActiveAbilityLogic(c)` ＋ `activeAbilityLogicVersioned: dto.version >= 23` |
| 挂载读数（一行 `Debug.Log`） | `有行的 132 张 / 193 行；挂到 op 上的 38 张…未挂：gate=0 45 · 行数≠op 数 48 · 无替代行动关键词 1 · 其它 0` —— **不逐卡刷屏**，但「没全接上」这件事**看得见** |

### ② `CardDef`：构造函数 ＋ `ActiveAbilityLogicVersioned`（`null` ≠ `[]`）—— ✅ 做完

| 位置（`Core/CardDef.cs`） | 内容 |
|---|---|
| 新嵌套类 `CardDef.ActiveAbilityLogicSpec` | `Ability` / `Gate` / `Trigger` / `NeedsLoading` / `TargetsAffected[]` / `FurtherMultiplier[]` ＋ `LogicCount` |
| 构造函数末尾两个新形参 | `IEnumerable<ActiveAbilityLogicSpec> activeAbilityLogic = null`、`bool activeAbilityLogicVersioned = false`（**追加在末尾** —— 全仓有位置传参的老调用点） |
| `readonly List<ActiveAbilityLogicSpec> _activeAbilityLogic` | **`null` = 这一列没落（未知）**；**非 null 空表 = 明确没有行**（现池 995 张） |
| `ActiveAbilityLogic` / `ActiveAbilityLogicVersioned` / `ActiveAbilityMap` | 前两个与 `AbilityTargets` / `AbilityTriggers` 同形；第三个是归位结论（§三） |
| `ActiveAbilityLogicGroup(int ab)` / `ActiveAbilityGroupFor(bool alternative)` | **按 `ab` 分组**（`-1` = `activeAbility`、`-2` = `alternativeAbility`，与 `unitConditions` 同词）；`null` 进 `null` 出 |
| `HasActiveAbility`（`:146`）/ `UsesAlternativeAbility`（`:206`） | **一个字没动**（简报要求） |

### ③ `EffectText`「卡面能力块 ↔ active 行」归位 —— 🟡 **只做了无歧义的那一档，其余停手**

- **做了**：`EffectOp.ActiveAbilityRow`（新字段，`-1` = 不属于 / 归不到）；`CardDef.ApplyActiveAbilityLogic`
  在 `CollectTriggerOps` **末尾**把 `ta` / `fm` 挂到**替代行动正文**那批 op 上（`TriggerOps(kw)` 的 op、
  或 `OathOps` 的 op —— **正是运行期真正被执行的那批对象**，见 §三·1）。
- **没做**：按「第 j 行 ↔ 第 j 条 op」把 87 张全接上。**判定与证据见 §三**（结论 = 没有判据，不硬配）。
- 卡面能力块那套 **G1/G2 闸门（`AssignAbilityTargets`）一个字没动** —— 它服务的是 `cardAbilities` 一族，
  与主动能力链是**两条独立的链**（D10 §三）。

### ④ `fm` 接进 `EffectOp.AbilityFurtherMultiplier` —— ✅ 做了（`EffectResolver` 一行没改）

- 挂点与 ③ 同一趟（`ApplyActiveAbilityLogic`）；**38 张全部拿到 `fm ≥ 0`**（`-1` 才代表未知）。
- **`EffectResolver.cs` 不需要改**：消费端**早就在位** —— `TryComputeRepeats` 里
  `int fm = op.AbilityFurtherMultiplier; if (fm > 1) { … }`（`Core/EffectResolver.cs:11787` 一带），
  规则是「**正文给过次数 ⇒ 只对账、不乘**」。本件只是把数据接到那个字段上。
  ⇒ 白名单里的 `EffectResolver.cs` **0 行改动**（它本来就是 CRLF，也就没行尾风险）。

### ⑤ 选 active / alternative 组 —— ✅ 接口做了；**运行期切换照旧用 `UsesAlternativeAbility`**

- `CardDef.ActiveAbilityGroupFor(bool alternative)`（`false` = 普通支 `ab=-1`，`true` = 替代支 `ab=-2`）。
- 🔴 **组的选择是运行期的事**（`RawCardScript__ResolveActiveAbility.c:18/26-31` ⇒ `BoardAnalysis.MeetsCriteria`），
  解析期**判不了**。⇒ 归位时要求「两个对象**逐格一致**」（今天只有 `SW21`，两组都是 `ta=[10]` / `fm=[0]`）；
  将来出现「两组 `ta` 不同」的卡 ⇒ `ActiveAbilityMap = "gap:groups-differ"`，op 留 `-1`（⛔ 不猜走哪组）。
- `UsesAlternativeAbility` **原样不动**（`CardDef.cs:206`）。

### ⑥ 断言（新分册）—— ✅ 建了；**`Step` 与 `Diag` 留给主对话**

`Editor/RuleEngineTest_G12.cs`（新文件，**LF**）＋ `.meta`（guid `da342ed7ec3947d0b3e4710523f1f45a`）。
入口 = `TestG12ActiveAbilityLogic`（子用例 `G12…` 打头，⛔ 不撞覆盖闸）。
⚠️ **`RuleEngineTest.cs` 的 `Step(...)` 与 `RuleEngineTest_Diag.cs` 的重生成都不在本代理白名单**
⇒ 主对话补之前，`Run()` 的覆盖闸会**红并点名** `TestG12ActiveAbilityLogic`（设计如此）。
⚠️ **本件的断言一条都没在 Unity 里跑过**（⛔ 白名单不许跑 Unity）；但**每一个期望值都经仓外探针实测**
（做法见 §五·2），不是纸面推的。

---

## 二 · 改了哪些文件（全部在白名单内）

| 文件 | 改动 | 行尾 |
|---|---|---|
| `RuleEngine/Data/CardDatabase.cs` | +DTO / `version<23` 守卫 / `MapActiveAbilityLogic` / `Parse` 接线 / 挂载读数 1 行 | **LF**（未翻，二进制核过 0 个 CRLF） |
| `RuleEngine/Core/CardDef.cs` | +`ActiveAbilityLogicSpec` / 两个形参 / 字段与访问器 / `ApplyActiveAbilityLogic` / `CollectTriggerOps` 末尾一行调用 | **LF**（未翻） |
| `RuleEngine/Core/EffectText.cs` | +`EffectOp.ActiveAbilityRow`（一个字段 + doc） | **LF**（未翻） |
| `RuleEngine/Editor/RuleEngineTest_G12.cs` `+.meta` | 新建（断言分册） | **LF** |
| `RuleEngine/Core/EffectResolver.cs` | **0 行**（消费端本来就在） | CRLF（没碰） |
| `工具/**` `数据/**` `CardPresentation/**` 两张正本 git | **0 行** | — |

---

## 三 · 第 ③ 步的判定与证据（本报告的核心）

### 3·1 这些块的 op 现在从哪来（读代码 + 仓外探针实测）

| 卡面写法 | op 收在哪 | 谁在运行期读它 |
|---|---|---|
| `Duty:` / `Pray:` / `Ferocity:` / `Agenda:` 块 | `CardDef.AddTriggerOp` → `EffectText.Parse(冒号后那半句)` → **`TriggerOps(kw)`** | `RuleCore.UseAlternativeCore` → `u.FxOps(keyword)`（`RuleCore.cs:10460` 一带） |
| **没有触发头的裸正文**（`AM5` `Deal 1 damage` + `keywords=['Vanguard','Duty']`） | `CollectBareKeywordBody` → `Parse(整条 desc)` → 也进 **`TriggerOps('duty')`** | 同上 |
| `Oath N: …` | `CollectOathOps`（`Parse(Desc)` 后按 `Cost>0 ∧ CostKind=='oath'` 筛）→ **`OathOps`** | `RuleCore.UseOathAbilityCore` → `EffectResolver.ResolveOathAbility` |
| `Talent: <名>`（51 张英雄） | 只有名字（`TalentName` / 造卡），**没有效果正文** | `RuleCore.SpawnTalents`（与主动能力链无关） |

⇒ **挂载点选对了**：`TriggerOps` / `OathOps` 里的 op **就是运行期被执行的那批对象**
（不是 `Parse(desc)` 另开的一份 —— 那两份是不同对象，贴上去不会被执行）。

### 3·2 能不能按「第 j 行 ↔ 第 j 条 op」硬配 —— **不能**（这是停手的理由）

判据 = 原版**一行的 `targetCriteria` 是一个目标集合**，不是一条卡面从句
（`AbilityLogic__PlayAbility.c:262` `GetTargets(manager, logic.targetCriteria, …)`；`GetTargets.c` 按
`targetsAffected` 分发一次返回**一批**目标）。现池实测两种形状都常见：

| 形状 | 实例（仓外探针实测，2026-10-24） |
|---|---|
| **1 条 op / 2 行** | `AM23 Bombast Field Gun`：`Deal 3-5 damage to an enemy and its adjacent units` ⇒ 行0 `ta=30`（玩家点的那个）、行1 **`ta=110`（它的相邻）** —— 两行都属**同一条** op |
| | 同形：`AM28 Chimera`（`0,0` / 1 条 op）· `DA18 Inner Circle Companion`（3 行 / 1 条 op）· `SOR17 Sister Superior`（2 行 / 1 条 op） |
| **1 行 / 2 条 op** | `AM34 Valkyrie`：行 `ta=10`（全体敌人）只覆盖 `Deal 2 damage` 那一半；`deploy` 那一半与它无关 |
| **行数 == op 数** | 51 张（例如 `SOR17` 2 行 / 1 op → 不等；`AM59` 2 / 2 · `AM35` 2 / 2 · `DA13` 2 / 2 相等） |

**量化（`gate=1` 的 87 张）**：行数（`ta` 元素）与 op 数**相等 51 张**、**不等 35 张**、卡上无替代关键词 1 张。
⇒ 「数字相等就算对上」只能覆盖 51 张，而它靠的是**一条没有判据的假设**；
`AM22 Primaris Psyker` 那种「1 条 `chooseone` op 对应 2 行（两个分支各一个目标集）」说明
**行是按目标集/分支切的**，等号随时可能是巧合。**按它硬配 = 静默贴错目标**（本仓红线）。

### 3·3 实际做的闸门（`CardDef.ApplyActiveAbilityLogic`）

任一不过 ⇒ **不挂 + 留 `-1` + 在 `ActiveAbilityMap` 记原因**（⛔ 不静默）：

| # | 闸门 | 理由 |
|---|---|---|
| ① | 只看 `gate == 1` 的对象（`gate == 0` → `gate-0` 不挂；`gate < 0` → `gap:gate-unknown`） | `hasActiveAbility == 0` 的卡整条路被 `CardScript__ResolveActiveAbilityPlayed.c:22` 短路 ⇒ 本 build **不消费** |
| ② | 卡上的替代行动关键词**恰好一个**（`duty`/`pray`/`ferocity`/`agenda`/`oath`） | 正文就收在那个键下（§3·1） |
| ③ | `ab=-1` 与 `ab=-2` 两组**都存在时逐格一致** | 走哪组是**运行期**决定（`UsesAlternativeAbility`），解析期判不了 |
| ④ | 那一组**恰好 1 个对象、1 行、1 条 op** | **唯一不需要任何行号假设**的一档（只有一种配对可能） |

### 3·4 结果与「还差什么」

| 结论（现池 132 张有行的卡） | 张数 |
|---|---|
| `attached`（挂上了 op） | **38** |
| `gap:shape`（行数 ≠ op 数 ⇒ 读不到） | **48** |
| `gate-0`（本 build 死数据 ⇒ 不挂） | **45** |
| `gap:no-keyword`（`SOR66`） | **1** |
| 其它闸门（`gate-unknown` / `groups-differ` / `two-keywords` / `no-ops` / `multi-object`） | **0** |

🔴 **要把剩下 48 张也接上，还差一列：逐行的 `chosenAbility`**（原版 `AbilityLogic.chosenAbility`，
`10 doDamage` / `20 …` / `40 …` / `462 …` 这一族）—— 有了它才能**按「这一行的能力种类」去配 op**，
而不是按位置。D10 §〇 已经点过这个缺口（「`activeAbilityLogic` 的能力内容我们整条没有按数据建模」），
现在**具体化成一条可执行的数据需求**。附带还缺（同一族，但 48 张里不一定都用得上）：
逐行的 `damageCriteria`（伤害数）与 `playersAffected`。
数据侧真值在 `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张 SO，字段齐全）——
**生成器加列是 D 线的活，不在本代理白名单**。

---

## 四 · 断言逐条（`RuleEngineTest_G12.cs`）＋ 灭自证

> 期望值全部写死字面量；每条带 🧨（什么改动能让它红）。下面只列**条数与要点**，原文见文件。

| 段 | 条数 | 钉什么 |
|---|---|---|
| ① `G12DataReadings` | 12 | 1127 张全带列 · `null` 0 张 · 有行 **132** / 空 **995** · 对象 **133**（`-1`:132 / `-2`:1）· logic 行 **193** · `gate=0` **45 张 / 52 行** · `fm>0` **109 格** · 每行 `ab`/`gate`/`trigger=400`/`ta`·`fm` 同长 违规 **0** |
| ② `G12NullVsEmpty` | 6 | v23 `[]` ⇒ **非 null 且 Count 0**、`Versioned=true`、`Map=""`；v22 缺列 ⇒ **null**、`Versioned=false`；**灭自证**：`emptyIsNull != oldIsNull`（「一律 null」「一律空表」两种简化都过不了） |
| ③ `G12OldTableWarns` | 3 | v22 ⇒ 点名 `activeAbilityLogic` 的 `LogWarning` **恰好 1 条**；警告文案含 `null` 与 `-1`；v23 ⇒ **0 条** |
| ④ `G12Attribution` | 21 | 正例 `AM38`（`attached` / `ActiveAbilityRow=0` / `ta=30` / `fm=0`）· **灭自证**：`AM38.cardAbilities[0].ta == 100 ≠ 30` 且 op 上挂的不是 100 · `AM16` `fm=2` 且 `Amount` 仍 2（不平方）· 反例 `SOR17`（`gap:shape` + op 无标记）· `SAU37`（`gate=0` → `gate-0`）· `SOR66`（`gap:no-keyword`）· `SW21`（两组一致 → `attached`，`ta=10`）· 汇总 **38 / 48 / 45 / 1 / 0** 且四档**加起来 = 132**（灭自证：没有卡落进「既没挂也没归因」） |
| ⑤ `G12GroupSelection` | 6 | `SW21` 两组各 1 个对象、普通支 `ta=10`；`AM5` 的替代组 = **空表（非 null）**；旧表两组都 **null** |

**本件的三条灭自证**（防「两边一起改回去」）：
1. `[]` vs 缺列必须落在 `null` 判定**两侧**（②）—— 把两种含义并成一个就红；
2. `AM38` 的 op 值必须**不等于**它自己 `cardAbilities` 的 `ta`（④）—— 「抄 cardAbilities」「混用两源」都红；
3. 四档计数相加必须 = 132（④）—— 有卡悄悄漏账就红。

---

## 五 · 读数 / 指纹影响

### 5·1 卡表 / 指纹

| 项 | 结论 |
|---|---|
| `cards_engine.json` | **没动**（D11 已落：`version 23` / `count 1127` / 1,453,225 B） |
| `PinnedPoolMd5` | 现读 `b6035055b993b33e9bcfa93bbce97d13`（D11 已重钉） ⇒ **不用再动** |
| `PinnedBaselineMd5` / `PinnedParseMd5` | **不用动**（已核判据，见下） |
| `play` 指纹 | **预期不变**（判据见下）；若 `play check` 计入 `ctx.Log` 才需重钉 —— 本代理**没跑** `play check`，留主对话裁决 |

**为什么解析指纹不动**：`工具/ruleprobe/Scan.cs:179` 走的是 `EffectText.Parse(desc, …, card: cardDef)`
（**整条 desc 的解析**），而本件的挂载只发生在 **`CardDef` 构造期的 `TriggerOps` / `OathOps` 那一份**；
`CanonParseMd5` 取的列（`Verb/ChooseSrc/Amount/Payload/Cost/Condition/CountRef/TargetBlock…`，`Scan.cs:180-215`）
**不含** `SubjectTargetsAffected` / `AbilityFurtherMultiplier` / `ActiveAbilityRow`。`layers` / `willrun` 也不打这三格。

**为什么 `play` 不变（数值上）**：
- `ta`（`SubjectTargetsAffected`）对**替代行动**这条路**今天零消费者** —— 唯一读它的
  `EffectResolver.ResolveTargets` 分支要求「`spec.Subjectless` ∧ **没有施放者** ∧ 没有点目标」；
  而替代行动 / 誓约的 op 一律**带单位**结算：
  · 替代行动 = `RuleCore.UseAlternativeCore` → `FireTriggerAt(ctx, u, keyword, …)`（`RuleCore.cs:10551`）
    → `FireTriggerDeferred` → `ResolveOps(ctx, owner, u, ops, …)`（`RuleCore.cs:11068`，**`u` 非 null**）；
  · 誓约 = `EffectResolver.ResolveOathAbility` → `ResolveOps(ctx, owner, unit, ops, "誓约能力", unit)`（`:7883`）。
  `source == null` 的调用点只有战术卡 / 常驻效果 / 手牌陷阱那几处（`:2283` / `:2403` / `:9153` / `:9467` / `:9482`），
  **没有一处吃主动能力链的正文**（D10 §3·1 已判死；本件复核了调用点）。
- `fm`（`AbilityFurtherMultiplier`）只在 **`fm > 1`** 时影响 `repeats`；本件的 21+17 = 38 处改动里
  **`fm` 只有 1 处从 `-1` 变成 `2`**（`AM16 Scout Sentinel`），而那处的正文本身也写着 `Draw 2`
  ⇒ 消费端判「正文给的次数 2 与原版 `fM` 2 一致 ⇒ 按正文」⇒ **只多一行日志、数值不变**。
  `fm` 从 `>1` 变成别的值：**0 处**。

### 5·2 改前的值 → 改后的值（仓外探针实测，逐条）

| 卡 / 键 | `ta` | `fm` | 说明 |
|---|---|---|---|
| `AM38`/duty | **100 → 30** | 0 → 0 | 改前挂的是 `cardAbilities[0]` 的值（错的），现在是 active 行真值 |
| `AM36`/duty | **65 → 50** | 0 → 0 | 同上（改前也是别处来的值） |
| `AM70`/duty | **30 → 60** | 0 → 0 | 同上 |
| `AM10`/duty | 0 → 10 · `AM3`/`AM1` | 0 → 30 | 改前 0 也是别处来的 |
| `AM2`/duty | 0 → 0 | **0 → 1** | `fm=1` 不影响结算（只有 `>1` 才乘） |
| `AM16`/duty | -1 → 0 | **-1 → 2** | 唯一 `fm>1` 的一处；正文同数 ⇒ 不乘（见 5·1） |
| 其余 15 处（`AM5`/`AM11`/`AM12`/`AM21`/`AM30`/`DA1`/`DA3`/`DA7`/`DA23`/`SW4`/`SW5`/`SW11`/`SW21`） | **-1 → 真值**（30/0/10/51/…/65） | -1 → 0 | 改前**读不到**，现在读到 |
| 17 张 `oath`（`UM89`…`UM108`） | **-1 → 真值**（10/20/30/60） | -1 → 0 | `CollectOathOps` 的 `Parse(Desc)` **不带 `card`** ⇒ 改前必然 -1 |

> 探针做法：仓外临时工程 `D:/tmp/wf_g12probe`（`dotnet` 直接编 `Core/*.cs` ＋ `Data/CardDatabase.cs`，
> 与 `工具/ruleprobe` 同一套源码、**不写仓库**）—— 见 §六·2。改前的值按
> `AssignAbilityTargets` 的**片段分支**规则复算（`abil.Count==1 ∧ ta.Length==ops.Count`）。

---

## 六 · 没查清 / 没做

1. 🔴 **48 张 `gate=1` 的卡仍是 `-1`（读不到）** —— 缺口 = 逐行 **`chosenAbility`**（＋ `damageCriteria` /
   `playersAffected`）这一列。**没有它就没有「行 ↔ op」的判据**，本代理不硬配（§三·4）。
2. **`activeAbility` 对象级 `targetCriteria`（不是 per-row）仍没有列**（D11 §七·3 / D10 §6·2·4）——
   它决定「这条主动技能能点哪些格子」（UI 高亮 / 合法性），**不影响本件的目标挂载**，本件未碰。
3. **`UsesAlternativeAbility` 的运行期结果没有进归位**：解析期判不了 ⇒ 只要求两组一致（今天 `SW21` 一致）。
   将来两组不同 ⇒ `gap:groups-differ`（op 保持未知）—— 这是**如实留白**，不是漏做。
4. **`needsLoading` / `trigger` 只落值、零消费**（`needsLoading` 现读 133 个对象全 0；`trigger` 恒 400 只作可读性）。
5. **`EffectOp.ActiveAbilityRow` 只标在 `TriggerOps` / `OathOps` 的 op 上**；`EffectText.Parse(desc, card:)`
   另开的那些 op **不标**（它们不是替代行动的结算对象）。⇒ 新写的探针**别拿 `Parse(desc)` 去查这个字段**。
6. **断言没跑过**（白名单不许跑 Unity）；`Step(...)` 与 `RuleEngineTest_Diag.cs` 的重生成留给主对话。
7. 未进原版实机（原版已关服）—— 与 D10 / D11 同一处境，判据停留在反编译层。

---

## 七 · 顺手发现（只报不改）

1. 🔴 **14 个替代行动 op 上挂着 `cardAbilities` 的 `ta`（来源是错的）** ——
   `AssignAbilityTargets` 的**片段分支**假定「整卡只有一条 `cardAbilities` ⇒ 这段正文属于它」，
   而 `Duty:` / `Pray:` / `Agenda:` 的正文**不属于** `cardAbilities`。实测（仓外探针）：
   `AM28 Chimera`/duty(10) · `DA65 Chaplain Gabutheron`/agenda ×2(10) ·
   `SOR6` · `SOR30` · `SOR15` · `SOR18` · `SOR20` · `SOR25` · `SOR68` · `SOR24` · `SOR17` · `SOR69` · `SOR35`（均 pray, 20）。
   另有 3 张被本件**顺带纠正**（`AM38` 100→30 · `AM36` 65→50 · `AM70` 30→60）。
   **今天无消费者**（同一支 `Subjectless ∧ source==null` 闸）⇒ 不算活缺陷，但**数据是错的**，
   建议另立账（修点在 `AssignAbilityTargets` 的片段分支：遇到替代行动关键词的正文应当**不挂**）。
   📌 本代理**没有**去清那 14 个值（本件的闸不覆盖那批卡；动它就是改另一条链的判据）。
2. 🟡 **`SOR66 Armorium Cherub`** 是**唯一**「`gate=1` 的 active 行 ∧ 卡上没有任何替代行动关键词」的卡
   （`keywords = Flying / Rally`，`ta=[30,20]`）⇒ 它的主动能力正文在我们的模型里**没有被任何键收下**。
   值得另立账核一下：原版这条主动能力（两条 logic）到底对应哪段正文。
3. 🟡 **10 张战术卡带 `Oath` 关键词但 `activeAbilityLogic == []`**（`UM109`–`UM118`）——
   那是**付费前缀**（`Oath N:` 当费用修饰），不是主动能力链 ⇒ 与数据自洽，**不需要动**。写在这里是免得下一个人当缺口查。
4. 🟢 **`ab=-2` 全池只有 `SW21` 一条**，且与 `ab=-1` 逐格相同 ⇒ ⑤ 的「两组一致」闸今天不会被触发，但这个闸**必须先有**（否则第一张两组不同的卡就会静默挑错一组）。
5. 🟢 简报白名单写的是 `RuleEngine/Core/CardDatabase.cs`，**实际文件在 `RuleEngine/Data/CardDatabase.cs`**
   （D11 §六 也是这么写的）⇒ 本代理按实文件改，`Core/` 下**没有**这个文件。
