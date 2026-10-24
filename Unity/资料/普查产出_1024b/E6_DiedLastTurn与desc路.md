# E6 · `DiedLastTurn` 生产侧无读者 · 整条 desc 路的替代行动块 · `Find` 未命中出声 + `Death.CardId`（2026-10-24）

> 执行代理 **E6**（有写权限）· 工程根 `d:/4/Unity`。
> 判据 = ① `d:/2/tools/decomp_full/` ② `d:/2/新解包资源/assets_full/`（本件未用到）③ 成品卡图（本件未用到）。
> ⛔ 没跑 Unity / 没跑 ruleprobe / 没动 git / 没改正本 / 没越白名单。
> ✅ `TMPDIR=/tmp/wf_e6 bash 工具/typecheck.sh`：**运行时 0 / 编辑器 0**（本件末次读数为 0/0；
> 之后别的代理（`E7`）正在写的 `BattleChatAssertE7.cs` 冒了 10 条错，全部集中在那一个文件 ⇒ 不是本件的）。

---

## 一 · 结论（三件）

| # | 件 | 裁定 | 一句话 |
|---|---|---|---|
| 1 | `BattleContext.DiedLastTurn` 只写不读 | **走 ②：原版没有独立用途 ⇒ 明确写成「只为自检保留」** | 原版「自上一回合以来」是**逐卡** `CardScript.turnDied` + `HasDiedSinceLastTurn`，**没有全局计数器**；真消费者只能是 `DeadUnits`+`DeathTurn`（它才表达得出 `own\|troop` 侧筛）。三处标注已改，判别式断言已加。**A1 那条自证断言要由主对话删**（白名单外）。 |
| 2 | `Parse(Desc)` 整条 desc 路仍给替代行动块贴 `cardAbilities` 的 `ta` | **缺口真实存在，但【生产侧没有任何读点】⇒ 只报 + 立账，不改代码** | `op.SubjectTargetsAffected` 在**全仓只有 3 个生产读点**，其中两个要求「无施放者」（`source == null`），第三个是 `ta ∈ {200,201}`。整条 desc 路的产物：**单位卡的走不到那两个无施主分支**（`ResolveOps` 恒传 `source = 该单位`），**非单位卡里带替代行动块的只有 9 张督军、它们从不出牌**。判据逐条见 §三。 |
| 3 | `CardDatabase.Find(pool,name,faction)` 静默 + `EvtKind.Death.CardId` 口径 | **✅ 两件都做完** | ① 两种落空（名字不在池里 / 在别的阵营）现在都 `LogWarning` 出声，**消息区分是哪一种**，去重键 `faction\|name`，读口 `FindMissWarningsForTest`；② 新增断言钉死 `EvtKind.Death.CardId = CardDef.Name`（用**id≠name** 的夹具，⛔ 不是拿两边相同的夹具自证）。 |

---

## 二 · 件 1：判据与改动

### 2·1 为什么「原版确有独立用途」不成立（反编译现读）

| 事实 | 出处 |
|---|---|
| 「自上一回合以来」是**逐卡**的一位 | `CardScript__set_turnDied.c`（`*(param_1 + 0x250) = param_2`）· `CardScript__get_turnDied.c` |
| 它的判据 = 本回合或上一回合（带一个 4 字节 `ObscuredInt` 读法） | `CardScript__HasDiedSinceLastTurn.c:19-48`（`iVar1 == iVar4` ⇒ true；否则 `iVar1 + 1 == iVar4`） |
| 全量反编译里**没有**「全局上一回合阵亡数」这种方法/字段 | `ls d:/2/tools/decomp_full/ \| grep -i "died\|death"` 只有上表三个 + 各死亡流程；`grep -l set_turnDied *.c` = **只有它自己**（写点被内联进别的函数） |
| `UM77` 那一档在原版是**墓地筛选**（`inCemeteryFiltered(60)`），不是一个计数 | 我们的落地注释：`EffectResolver.cs:9887-9905`（`DeadUnits` + `DeathTurn ∈ {Turn, Turn-1}`） |

⇒ `DiedLastTurn` 是**我们为「上一回合那一半」造的一格**，而真正需要它的那份语义（`own|troop` / `enemy|…` 侧筛）它表达不了（不分阵营、不分兵种）。
**所以：⛔ 不能把它接进 `UM77`（接 = 静默丢掉侧筛，数到对面死的兵）。**

### 2·2 改动（都在白名单内）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/RuleCore.cs:1557-1575` | 写点上方那段注释**就地订正**（铁律 5）：写明「这一格**生产侧没有任何读者**」「只为自检保留」「真消费者是 `DeadUnits`+`DeathTurn`」「⛔ 别把 `UM77` 改成读它」+ 反编译判据 + 判别式断言在哪。**行为 0 变化**（只改注释，写的那一行原样）。 |
| `RuleEngine/Core/EffectResolver.cs:9897-9907` | `diedsincelastturn` 分支上方加 `⛔ 不许改用 ctx.DiedLastTurn` 的**理由块**（侧筛会丢 / 原版是逐卡 `turnDied` / 指向判别式断言）。**行为 0 变化**。 |
| `CardPresentation/Editor/RuleEngineTest_EngineE6.cs` | `E6T1DiedLastTurnNotProductionInput()`（4 条断言 + 2 条前提，见 §五）。 |

### 2·3 要主对话做（白名单外，逐条）

1. **删掉** `RuleEngine/Editor/RuleEngineTest_EngineA1.cs:166` 那一条：
   ```csharp
   Check(ctx.DiedLastTurn, 1, "★ ② `DiedLastTurn` 滚动到 1（`BeginTurnCore` 在清零前搬过去）"
                            + "｜🧨 漏掉那次滚动 ⇒ 实得 0 ⇒ 下面那条红");
   ```
   —— 它是**自证**（生产侧没人读这一格，唯一读者就是它自己）。删掉它不会失去覆盖：
   **下一条**（`:168`，`diedsincelastturn` 仍数得到 1）才是真判据；本件 `E6T1` 又从**两侧**钉住了
   「读的是 `DeadUnits` 而不是这一格」。
2. **`BattleContext.cs:604-621` 的 doc** 末段（「写点唯一 = …」那段）补一句：
   「🔴 2026-10-24（`E6`）：**生产侧没有读者**，本格只为 `EngineA1` 的自检保留；
   真消费者 = `DeadUnits` + `DeathUnit.DeathTurn`（`EffectResolver.cs` 的 `diedsincelastturn` 分支）；
   ⛔ 别把它接进 `UM77`（表达不了 `own|troop` 侧筛）。」
   （不改也行 —— `RuleCore.cs` 写点和 `EffectResolver.cs` 读点两处注释已经把这件事写在**会被读到的地方**。）
3. `RuleEngineTest.Run()` 里加一行 `Step(TestEngineE6);`（不加 ⇒ `:1157` 那条覆盖闸会点名它）。

---

## 三 · 件 2：判据 —— 「整条 desc 路归出来的 op 有没有被引擎消费？」

### 3·1 `ta` 的生产读点：全仓只有 3 处（这是本件的判据骨架）

`grep -rn SubjectTargetsAffected --include=*.cs MyGame/Assets` ⇒ **读**只有三处，**其余全是写点/注释/自检**：

| 读点 | 生效条件 | 整条 desc 路的产物能走到吗 |
|---|---|---|
| `EffectResolver.cs:1330`（`ta == 200 / 201` → `referencedCards`） | `source != null`（读施放者的引用表） | ⚠️ **能**（单位触发 op 的 `source` 就是那个单位）⇒ 只有**当真把 `ta` 落成 200/201** 时才有害 |
| `EffectResolver.cs:1479-1482`（`ta ∈ {30,220}` 或 `<0` 才认玩家点的目标） | `spec.Subjectless` ∧ `source == null` | ❌ 走不到（单位 / 英雄的 op 恒有施放者） |
| `EffectResolver.cs:1496-1515`（`ta == 120` → 施放方督军；其余 → 「己方全体」近似 + 出声） | 同上 | ❌ 同上 |

写点 = `EffectText.cs:1959 / :1992`（`AssignAbilityTargets` 的两条分支）· `CardDef.cs:2508 / :2576`（active 行覆盖）。

### 3·2 整条 desc（`IsWholeDesc` 为真）且**传了 `card:`** 的调用点：只有 6 处

| 调用点 | 产物去哪 | 会不会「结算」 |
|---|---|---|
| `EffectResolver.cs:1807`（`CanPlayTactic`） | 只 `PickTarget` / `IsLegalPick` / `TargetCondition` | 不结算（`ta` 不读） |
| `EffectResolver.cs:2115`（`PlayTacticCore`） | `ResolveOps` **真结算** | ✅ 会 —— **但只有非单位卡**（`RuleCore.cs:3034`：`if (!card.IsUnit) return CanPlayTactic(...)`） |
| `EffectResolver.cs:4570`（`PlayerChooseOps`） | 面板 ask 点（`BattleDriver` 只读 `Verb`/`Payload`/`AbilityIndex`） | 不结算（`ta` 不在 `CardPresentation/**` 任何读点里） |
| `EffectResolver.cs:4666`（`DoChooseEffect` ④-a） | 被选中那张卡的 `desc` **真结算** | ✅ 会 —— 但池子只有 4 张登记卡（`Righteous Fury` / `Master of Arms` / `Paragon of Ultramar` / `Mekaniak` 同组），**全是无替代行动关键词的战术卡** |
| `SimpleAI.cs:467`（`TargetSideOf`） | 只 `PickTarget`；且 `if (card == null \|\| card.IsUnit) return -1` | 不结算 |
| `CardDef.cs:3048`（`CollectBareKeywordBody` → `_triggerOps`） | **真结算**（触发路 `ResolveOps(ctx, owner, u, ops, kw)`，`source = u`） | ✅ 会 —— 见 3·3 |

### 3·3 「卡面上带替代行动块、且真的会走整条 desc 路」的那些卡，实测怎么落

`cards_engine.json` 现读（1127 张）：

| 形状 | 张数 | 结果 |
|---|---|---|
| **非单位卡**里 desc 带 `Duty:`/`Pray:`/`Ferocity:`/`Agenda:`/`Oath:` 块 | **9 张，全是 `type:"hero"`** | `PlayTacticCore` 走不到（督军从不在手牌里）；`DoChooseEffect` 池子里没有。⇒ **不可达** |
| 单位卡里 **`keywords` 有替代行动词、但 desc 无 `<词>:` 前缀**（⇒ 走 `CollectBareKeywordBody` → 整条 desc） | 11 张（`AM5/AM11/AM16/AM23/DA18/DA29/DA71/SOR13/SW4/SW11/SW21`） | **10 张的 `abilityTargets` 是空表 ⇒ `AssignAbilityTargets` 第 3 句就 `return`**（`EffectText.cs:1943`），一个 `ta` 都不贴。剩 `SOR13` 贴上 `ta=20`，而 20 **不在** `{30,220,120,200,201}` 任何一档 ⇒ 上面那 3 个读点**一个都不进**（1496 那支只在 `source == null` 走，触发路的 `source` 是该单位） |
| 单位卡 desc **带** `<词>:` 前缀（`SOR6` 那类） | 走 `AddTriggerOp` → **片段**分支（E1 已加闸） | 整条 desc 的产物只剩 `PlayerChooseOps` 这一路（ask 点，`ta` 不读） |

**⇒ 结论：这条路上的 `ta` 今天【没有任何生产读者】。**
⇒ 按简报的判据走「没有 ⇒ 给证据 + 立账」，**本件没有改 `AssignAbilityTargets`**。

### 3·4 立账（建议原文，主对话可原样贴进 `§29·b`）

> **新账（E6·2026-10-24）整条 desc 路的替代行动块仍贴 `cardAbilities.ta`** —— 判据缺口 = `EffectText.AssignAbilityTargets` 的 **full 分支**（`EffectText.cs:1945-1967`）没有 E1 加在**片段**分支上的那道 `alternativeActionBody` 闸；实测可达面：`SOR13`（`ta=20`，`kill` 不掉但也不消费）、合成探针（`ta=25`）。
> **为什么现在无害**：`op.SubjectTargetsAffected` 的 3 个生产读点都要求「无施放者」或 `ta∈{200,201}`（见 `资料/普查产出_1024b/E6_DiedLastTurn与desc路.md` §三）。
> **什么时候会变成真缺陷**：① 某张「裸写替代行动正文」的单位卡拿到 `ta ∈ {200,201}`（⇒ 触发路会去读 `source.ReferencedCards`）；② 出现一张**战术/防御卡**带替代行动块（⇒ `PlayTacticCore` 直接结算）。
> **要做的**（判据已足，只剩动手）：给 full 分支加同一口径的闸 —— 判「这一块的头是不是替代行动词」（`KeywordTable.Duty/Pray/Ferocity/Agenda/Oath`），是则**不贴** `ta`/`fm`；同时把 `E6T2` 里那条 `== 25` 改成 `== -1`。
> **回归口**：`RuleEngineTest_EngineE6.cs` 的 `E6T2`（合成表，不依赖真卡数据）。

---

## 四 · 件 3：改动

| 文件 | 改动 |
|---|---|
| `RuleEngine/Data/CardDatabase.cs:2296-2360` | `Find(pool,name,faction)`：落空时 `NoteFactionFindMiss`（**返回值语义不变，仍 `null`**）。消息里带 **`pool` 条数 / `name` / `faction`**，并**区分两种落空**（「池里根本没有这个名字」 vs 「有 N 张同名、阵营都不是 X（首张在 Y）」）。去重键 `faction\|name`。新增读口 `FindMissWarningsForTest`（去重后的喊过几次）＋ 池子是惰性迭代器时抛异常也不会把查找搞崩（那一支也出声）。 |
| `CardPresentation/Editor/RuleEngineTest_EngineE6.cs` | `E6T3FindMissAndDeathCardId()`（9 条断言 + 前提）。 |

**为什么留一个读口而不让自检去数日志**：`Application.logMessageReceived` 会被别的用例抢/过滤（E1/E4 都踩过），静态计数是确定性的。

---

## 五 · 断言（全部写死字面量；每条带 🧨）

文件：`MyGame/Assets/CardPresentation/Editor/RuleEngineTest_EngineE6.cs`（+`.meta`，guid `8fdcc5f06899473796980fab476e71b8`）。
入口 `public static void TestEngineE6()`（`public static partial class RuleEngineTest`，与 `E2` 同形）。

| 段 | 条数 | 钉什么 | **灭自证** |
|---|---|---|---|
| `E6T1DiedLastTurnNotProductionInput` | 4 + 3 前提 | 写点还在（`DiedLastTurn == 1`）· 真消费者数出 1 · **把它写成 999 读数不变** · **清 `DeadUnits` ⇒ 0（而这一格仍是 1）** | 两条反向读数（「写脏 999 仍 1」 vs 「清真来源 ⇒ 0」）**结构上不可能同时由「读这一格」实现** ⇒ 谁把 `diedsincelastturn` 接到 `DiedLastTurn` 上必红 |
| `E6T2WholeDescAltBlockHazard` | 7 + 3 前提 | 合成表（`desc="Deal 3 damage"`、`keywords=["Duty"]`、`abilityTargets.ta=25`、`activeAbilityLogic.ta=30`）：整条 desc 产物 **25** · 运行期那条 **30** · 两份**不是同一个对象** · `ActiveAbilityMap=attached` | ① 谁把结算改去吃整条 desc 的产物（或删掉按种类覆盖）⇒ 30 变 25 ⇒ 红；② 谁把 `_triggerOps` 直接指向 `Parse(Desc)` 的返回 ⇒ `ReferenceEquals` 变 true ⇒ 红。**给 full 分支加闸（推荐的修法）会让 `25` 变 `-1`** —— 那条断言的文案里写明了「那时改成 `-1` 并把账核销」，⛔ 不是删掉它 |
| `E6T3FindMissAndDeathCardId` | 9 + 3 前提 | 名字不在池里 ⇒ 喊 1 次 · 同 `(name,faction)` 再问 ⇒ **仍 1**（去重）· 阵营不匹配 ⇒ null **且喊 1 次** · 阵营对得上 ⇒ 找得到 · `EvtKind.Death.CardId == "E6 Death Name"` **且 `!= "E6_DEATH_ID"`** | ① 删 `NoteFactionFindMiss` ⇒ 计数 0 ⇒ 红；② 去掉去重 ⇒ 2 ⇒ 红；③ `Emit(..., u.Name)` 改成 `u.Card.Id` ⇒ `CardId` 变 `E6_DEATH_ID` ⇒ 红。**夹具的 id 与 name 故意不同** —— 用 `Unit(...)` 那种（两者相同）断不出这一格 |

---

## 六 · 没查清 / 没做（写清「卡在哪、回来的条件」）

1. **本件所有断言未实跑**（红线：不跑 Unity）⇒ §五 的 🧨 只是「改坏就红」的**设计**，需主对话在同步点跑 `RuleEngineTest.Run` 才算数。
2. ⚠️ **`E6T2` 的合成表依赖两个实现细节**（① `Duty` 不在 `BodyKeywords` 里加标记 op（只有 `Pray` 加）；② `Deal 3 damage` 只产 1 条 op ⇒ G2 `ta.Length(1) == op 数(1)` 成立）。若实跑时 `25` 没出现，**先看这两个前提**（`EffectText.cs:3138-3152` 的 Pray 标记 op 就是为什么 `SOR6` 的整条 desc 路天然贴不上）。
3. ⚠️ **督军那 9 张的「块序 ≠ `cardAbilities` 序」没修**（顺手发现 1）—— 它可能让整条 desc 路把**另一条 ability 的 `ta`** 贴到这块上（真错位，不只是来源错）。要修得先有「触发头 ↔ `AbilityTrigger` 枚举」的映射表（`Pray=655 / Duty=662 / Ferocity=725 / Agenda=705 / Talent=46` 现成），判据够但没有落地位置（`AssignAbilityTargets` 只有 `segHead: bool`，拿不到段文本）。**已立账**，与本件的账同族。
4. **`AbilityFurtherMultiplier`（`fm`）未单独核查**：它与 `ta` 同源同闸（`EffectText.cs:1960-1965 / :1988-1993`），但它的生产读点（`EffectResolver` 的 `repeats`）**不需要 `source == null`** ⇒ 一旦整条 desc 路的 `fm` 落到替代行动块上、而那条 op 被结算，**就是真影响**。今天同样不可达（同 §3·3 的那张表：10/11 张 `abilityTargets` 空表），但**它的门槛比 `ta` 低** ⇒ 这笔账上要写进来（§3·4 已含）。
5. **原版实机未验**（原版已关服），判据停在反编译 + 卡表两层。

---

## 七 · 顺手发现（⛔ 只报不改）

1. 🔴 **督军的 `abilityTargets` 顺序不是卡面文字顺序**：`SOR65` 的 desc 印 `Pray: … . Talent: …`，而 `abilityTargets = [{trigger:46 Talent, ta:[0]}, {trigger:655 Pray, ta:[20]}]` —— **数组序是 `cardAbilities` 的序列化顺序**。⇒ 整条 desc 路按「块下标 == ability 下标」对齐时，**第 0 块（Pray）拿到的是 Talent 行的 `ta`**。今天因为督军从不出牌而不可见，但这是「块序假设」本身不成立的**数据级证据**（与 E1 §十·4 同族，比「来源错」更硬）。
2. 🟡 **`CardDatabase.Find(pool, name)`（两参版）也静默** —— 本件按简报只动三参版。两参版没有阵营信息可判断，出声的价值低于噪音（它被 `CreatePool` / `EffectResolver` 当「顺便查一下」用），**建议保持不出声**，写在这里备查。
3. 🟡 **`CardDatabase.Find` 的三参版在 `CardPresentation` 侧有一批调用点**（`BattleDriver.cs:6212/11458/19197/21097/21835` · `VfxMap.cs:1288`）—— 本件的光声会让「本局阵营里没有这张卡」这一档**首次可见**。若同步点跑 `BattleScene.Run` 时看到这批告警，**那是新信息的披露，⛔ 不是新引入的缺陷**（去重后条数有上界）。
4. ℹ️ `E7`（另一个代理）正在写的 `CardPresentation/Editor/BattleChatAssertE7.cs` 在本件末次 typecheck 时有 **10 条**编译错（`CS0023`/`CS0029`，全是把 `void` 当值用）—— **与白名单无关、不是本件的**，按铁律 13·3 只报不改。本件自己那一次读到的是 **0/0**。

---

## 八 · 改动清单（全部在白名单内）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/RuleCore.cs` | 写点上方注释就地订正（**行为 0 变化**） |
| `RuleEngine/Core/EffectResolver.cs` | `diedsincelastturn` 分支加「⛔ 别改用 `DiedLastTurn`」理由块（**行为 0 变化**） |
| `RuleEngine/Data/CardDatabase.cs` | `Find(pool,name,faction)` 未命中出声 + 去重表 + `FindMissWarningsForTest` |
| `CardPresentation/Editor/RuleEngineTest_EngineE6.cs`（新建，+`.meta`） | `TestEngineE6()`，3 段 20 条断言 |
| `资料/普查产出_1024b/E6_DiedLastTurn与desc路.md` | 本报告 |

行尾：`EffectResolver.cs` 保持 **CRLF**（13550/13550，改完现数）；`CardDatabase.cs` / `RuleCore.cs` / 新文件 = **LF**。
