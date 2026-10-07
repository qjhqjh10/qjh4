# Backlash 条件句解析失败 —— 只读诊断（2026-10-17）

## 结论（一句话 + α/β/γ/δ）

**根因 = 夹具自己把两套文法串在了一句话里**：`RuleEngineTest.cs:16444` 的正文
`If a friendly troop died this turn, Damage 3 EnemyWarlord` 里，
`If …, <正文>` 这层壳是 **`EffectText`（原版卡面文法）**，
而载荷 `Damage 3 EnemyWarlord` 是 **`EffectSpec`（我们自己设计的 26 张卡的封闭文法）**。
`CardDef.AddTriggerOp` 只把触发正文喂给 `EffectText.Parse`（`CardDef.cs:1164`），
`EffectText` **根本不认 `Damage N Target` 这种语序**（它认的是 `Deal N damage to …`）⇒ 正文解析 0 条 op；
退路 `FireTriggerAt` 的第二条路 `Card.Effect`（`EffectSpec.Parse`）又因为首词是 `If` 而归 null
（`EffectSpec.cs:91` 要求 `parts[0] ∈ {damage,heal,draw}`）⇒ **两条路同时 null** ⇒
`TriggerOps("backlash") == null`，反噬一次都不发生。

**判 α**（夹具/断言一侧自己错），不是 β、不是 γ。
—— `CheckTrue(...)` 那条护栏本身是对的，而且**它按设计干了活**：把一条本来会「假绿」的用例钉成了红。

## 证据（文件:行号 或 日志行号）

1. 失败点（日志栈逐字指出行号）：
   - `_tmp_view/ruleengine.log:29595` ✗ 护栏 —— 栈 `TestDeathAccountsAfterBacklash () at RuleEngineTest.cs:16445`
   - `_tmp_view/ruleengine.log:29713` ✗ ★② —— 栈 `TestDeathAccountsAfterBacklash () at RuleEngineTest.cs:16481`
2. 夹具正文：`RuleEngineTest.cs:16443-16444`（`Unit(name,cost,atk,hp, params string[] kws)`，第 5 参 = keywords 条目）。
3. 触发正文的解析口：`CardDef.cs:1103 AddTriggerOp` → `CardDef.cs:1164 var ops = EffectText.Parse(body, out _, out _);`
   （**只此一条路**，没有 `EffectSpec` 兜底）。
4. 封闭文法那一侧：`CardDef.cs:112 var spec = EffectSpec.Parse(text);`；`EffectSpec.cs:81/91`
   （`parts[0]` 必须是 `damage|heal|draw`，`If` 直接判死）。
5. 结算层的取法：`RuleCore.cs:3459 FireTriggerAt` —— `:3463 ops = u.FxOps(keyword)`（= `Card.TriggerOps`），
   `:3464 spec = u.Effect(keyword)`（= `Card.Effect` / EffectSpec）；`:3466` `if (ops == null && spec == null) return false;`
   —— **这就是为什么两条都 null 时反噬「静默不发生」**。
6. 条件壳本身**没问题**：`EffectText.cs:3087 TryIf` 认 `If <条件>, <效果>`；
   `EffectText.cs:7329` 把 `died this turn` 归一成 `"deaths"`；
   `EffectResolver.cs:5392 case "deaths"` 判 `DeadUnits` 里同回合、同主人、`MatchesKind(卡,"troop")`。
   条件与 `Deal …` 合用时会正确挂到 op 上（见下面离线实测）。

### 离线实测（net8 + UnityEngine 桩，`D:/tmp/wf_backlash_probe/`，**没占 Unity**）

`Program.cs` 探针 A（解析层）与探针 B（端到端复刻 ①/② 两块布局）：

| 正文 | `TriggerOps` | `Effect`(EffectSpec) | ① 期望 30 | ② 期望 27 |
|---|---|---|---|---|
| **夹具原文** `If … this turn, Damage 3 EnemyWarlord` | **null** | **null** | 30 PASS | **30 FAIL** ← 与实跑逐字一致 |
| **候选 1** `If … this turn, Deal 3 damage to the enemy Warlord` | 1 op（cond=`deaths`） | null | 30 PASS | **27 PASS** |
| 候选 1b `If … this turn, Deal 3 damage to EnemyWarlord` | 1 op（cond=`deaths`） | null | 30 PASS | **27 PASS** |
| 候选 2 `Damage 3 EnemyWarlord`（去掉条件） | **null** | DMG3 FOE | **27 FAIL** | **24 FAIL** |
| 对照 `Deal 4 damage to all enemies`（本文件 :16502 在跑） | 1 op | null | —— | —— |

⇒ 候选 1 与 1b 让两条断言**都变绿**；候选 2 会让 ① 从「没打出来」变成「打出来了」，**把这条测试的语义整个反过来**。

## 影响面（数字）

命令（离线，不占 Unity；两条都在 `D:/tmp/wf_backlash_probe/`）：

```
cd D:/tmp/wf_backlash_probe
dotnet run --project wf_backlash_probe.csproj -- census   # 全池普查（下面这张表）
dotnet run --project wf_backlash_probe.csproj             # 端到端复刻 ①/② 四组对照（上一节那张表）
```

读 `Assets/RuleEngine/Resources/cards_engine.json`（**1126 张**），扫 `desc` 分句 + `keywords` 数组：

| 量 | 数字 |
|---|---|
| 全池张数 | **1126** |
| `desc` 里「`<触发关键词>: If …`」形状的正文条数 | **13**（涉及 **13 张卡**） |
| 其中**能解析出 op** 的 | **13 / 13**（解析不出 **0**） |
| `keywords` 数组里「`关键词: If …`」形状的条目 | **0** |
| 全池 `关键词:` 后面跟 **EffectSpec 句法**（`Damage/Heal/Draw N Target`）的条目 | **0** |
| 以 `If ` 开头的卡面分句总数（不分来源，参考） | 73 |

涉及那 13 张：`Attilan Rough Rider` · `Deathmark` · `Beast Snagga Boy` · `Squighog Boyz` · `Burna Boy` ·
`Trukk Boy` · `Warbiker` · `Adelaide the Serene` · `Blackmane Inceptor` ·
`Fire Warrior Marksman` · `Kroot Carnivore` · `Lone-spear` · `Bladeguard Sergeant`。

**⇒ 「`关键词: If …`」这个形状全池 13 处、100% 解析得出，不是解析器的缺口。**
**⇒ 出问题的是「触发正文里用 EffectSpec 语序」—— 全池 0 条。这是一个只发生在夹具里的形状。**

补充两条口径：
- `descZh` **不进解析器**：`CardDef.cs` 里 `DescZh` 只出现两次（`:34` 属性 · `:79` 赋值），
  `CollectTriggerOps` 读的只有 `Desc`（`CardDef.cs:322`）。所以「中文写了条件」不影响解析，也不必普查。
- 夹具文件里**两套文法并存**是这次踩坑的土壤：`Editor/RuleEngineTest.cs` 里既有
  `"Rally: Damage 2 EnemyWarlord"`（:16302 / :16394，走 `EffectSpec` 那条**退路**）
  又有 `"Backlash: Deal 4 damage to all enemies"`（:16502，走 `EffectText`）。
  两者**只在「没有条件」时可以互换** —— 一加 `If …`，退路就没了（`EffectSpec` 不支持条件）。

## 原版判据（decomp_full 的 文件:行号）

`D:/2/tools/decomp_full/CardScript__TriggerUnitBacklashActions.c`（190 行）：
- `:55` `RawCardScript__GetAbilitiesForTrigger(*(rawCard + 0x28), entity, 0x50, 0, 0)`
  —— 按 **trigger id `0x50`** 取能力；`0x50 = 80`，
  桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs` 里 `ThisUnitDeath = 80`。
- `:62` `BoardAnalysis__MeetsCriteria(globalVars, *(ability + 0x38), entity, 0, entity, 0, 0)`
  —— **条件从句是能力自带的一份结构化 criteria，在触发当刻现判**。
- `:181` `BattleManager__AddAutoActionToQueue(...)` —— 入队执行，**它自己一次都不解析文本**。

正文本身也是**结构化数据**，不是文本：
- `CardAbility.cs`（桩）：`AbilityTrigger trigger` · `AbilityContext abilityContext` ·
  **`BoardCriteria boardCriteria`**（=「If …」那一层）· `List<AbilityLogic> abilityLogic`（= 正文那一层）。
- `AbilityLogic.cs`（桩）：`AbilityEffect chosenAbility` · `DamageCriteria damageCriteria` ·
  `HealCriteria healCriteria` · `TargetCriteria targetCriteria` · `SummonCriteria summonCriteria` …
- `BoardCriteria.cs`（桩）：`PlayersAffected playerTurn` · `TargetCriteria countCriteria` ·
  `CountOptions countOptions` · `StatFilterOptions triggerIf` · `int referenceValue`。

**⇒ 原版没有「把 `Backlash:` 后面那段正文变成 ops」这一步** —— 卡做出来时就已经是
`trigger + boardCriteria + abilityLogic` 三块数据；条件从句落在 `boardCriteria`（`MeetsCriteria` 现判），
效果落在 `abilityLogic`。**「条件在文本里」这件事在原版不存在**，我们那套
`EffectText` + `EffectCondition` 是**从卡面文字重新推出来的**（自研），所以「两套文法边界在哪」
**只能由我们自己的实现回答，原版帮不上**。

**⇒ 由此可判 β 不成立**：没有任何「原版会解析、我们不会」的东西被漏掉；
反过来，全池 0 张卡写这种形状。

## 最小改法（候选，**均未动手**）

**候选 1（推荐 · 一行）**：`RuleEngineTest.cs:16444` 的字符串改成

```csharp
"Backlash: If a friendly troop died this turn, Deal 3 damage to the enemy Warlord"
```

- 实测：① 与 ② 两条**都变绿**（见上表）。条件仍由 `deaths` 那一支现判 —— 这正是这条测试要量的东西（「反噬看不到自己已阵亡」）。
- 连带影响：**零**（只改夹具字符串，不动 `Core/`）。同文件 :16446 的失败文案里那半句引用也要一起改（否则文案与正文对不上）。
- 也可写 `… to EnemyWarlord`（候选 1b，实测同样绿）—— 但 `the enemy Warlord` 更贴卡面写法，建议用 1。

**候选 2（**不要做**）**：去掉条件句、只留 `Damage 3 EnemyWarlord`。
- 实测：① 由 30 变成 **27 FAIL**、② 由 27 变成 **24 FAIL** —— 因为这条路没有条件能力，**反噬会在第一只探针上也照打**，
  而「① 量的是『看不到自己已阵亡』」正是这条测试的全部意义。**改了等于把测试测反。**

**候选 3（不建议 · 判据为空）**：让 `AddTriggerOp`（`CardDef.cs:1164`）在 `EffectText.Parse` 之外
再兜底试 `EffectSpec.Parse`。
- 能解决本例，但：① 全池 **0 条**卡用这个形状（普查数字在上面）；② `EffectSpec` **没有任何条件能力**，
  兜底进来也只能是无条件的效果；③ 等于在**触发正文**这条路上并列两套文法，
  而 `FireTriggerAt`（`RuleCore.cs:3463-3466`）已经承担了「两条路二选一」的判据 ——
  再加一处就变成**两处写同一条规则**（工程红线）。
- 若一定要「让夹具也能写 EffectSpec 语序」，更干净的是**改夹具**（候选 1），不是改引擎。

**候选 4（不建议）**：让 `CardDef.TriggerOps` 自己在 `EffectText` 失败时返回 `EffectSpec` 包出来的 op。
- 会把 `TriggerOps` 的语义从「原版卡面正文」偷换成「任何一种解析得出来的东西」；
  `FxOps`（`UnitState.cs:342`）「卡上原生优先、否则用挂上去的那份」这条判据也会被搅浑；
  且同样解决不了「带条件」那一半。**收益 0。**

## 判 α/β/γ/δ

- **(α) 断言/夹具自己错 —— 就是这个。** 具体是夹具的**输入字符串**用错了文法，
  不是 `CheckTrue` 的期望值错（那条护栏是对的）。
  换个说法也可写成 **(δ) 夹具前提不成立**（前提「`EffectText` 认 `Damage 3 EnemyWarlord`」为假）——
  α 与 δ 在这里是同一件事的两面。
- **(β) 否。** 判据：全池 1126 张里这种形状 **0 条**；原版**根本不解析文本**（见上一节），
  没有「原版会、我们不会」的缺口。
- **(γ) 否 —— 不是本批回归。** 三条证据：
  ① `TestDeathAccountsAfterBacklash` **整个函数是本批新加的**（`git show HEAD:…RuleEngineTest.cs | grep` 零命中；
     `git diff` 里 `+ Step(TestDeathAccountsAfterBacklash);` 与 `+ static void TestDeathAccountsAfterBacklash()` 都在新增侧）；
  ② 定义 ADD 触发器正文的那个文件 **`Core/CardDef.cs` 本批根本没改**（`git diff --stat` 空）；
  ③ `Core/EffectText.cs` 本批的 12 个 hunk 全在 `:225-266` 与 `:4078-4360`（选牌 / 造副本那一族），
     `TryIf`(`:3087`) 与 `ReDeal`(`:4376`) **一个字符没动**；
     `git show HEAD:…EffectText.cs` 里 `ReDeal` 的正则逐字相同（HEAD `:4305`）、
     也没有任何 `Damage N Target` 的处理（grep 计数 0）⇒ **这套文法在本批前后完全一致**。
  ④ 附带：本函数是 `Step(` 列表里的**第 89 个**（`RuleEngineTest.cs:256`），
     而上一轮崩在「第 89 个用例」—— 上一轮这条**很可能压根没跑到**，本轮是第一次真跑。这不改变结论（文法没变）。
- **同形夹具只有这一处**：`grep -nE '"[^"]*(Rally|…|Ecstasy)[^"]*:\s*[^"]*If [^"]*Damage [0-9]' Editor/RuleEngineTest.cs`
  只命中 `:16443` 与 `:16446`（后者是失败文案）。**没有第二处要一起修。**

## 没查清的

- **上一轮那条 `ArgumentOutOfRangeException`（约 `:16354`）与本次红的关系**：本轮日志
  `_tmp_view/ruleengine.log` 里 `grep -n "ArgumentOutOfRangeException"` **零命中** ⇒ 本轮已不复现。
  但它上一轮具体崩在哪一行、与本函数是否同源，**我没查清**（本函数的 `Unit(...)` 调用跨两行写法
  与本文件别处一致，没看出索引越界的形状；`git diff` 里那条 `+404` 的注释把「先索引再断长度」
  的教训挂在别处）。**没查清就是没查清，别当成已解。**
- **候选 3/4 的「改引擎」路线我只做了静态推理**（没建过带兜底的探针）—— 因为推荐路线是候选 1，
  不想在判据为空的方向上多花时间。若调度台决定走那条，需要新起一个探针验证「带条件的正文怎么表达」。
- **本仓自研的 `EffectText` / `EffectCondition` 与卡面的完备对应**不在本次范围（本次只回答「为什么这两条红」）。
