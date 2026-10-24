# G6 · `AM18` / `GOF4` 的共同根因（A/B/C/D 四件，2026-10-24）

> 执行代理 **G6**。判据源 = `资料/普查产出_1024/C6_最后三条判据.md` §1（**不重新论证**）。
> ⛔ 未跑 Unity、未动 git、未改两张正本。✅ 跑过：`typecheck.sh`（0 错）· `gen_cards_engine.py --check`（rc=0）·
> `--write` 两次写盘**逐字节相同**（`ab8c5238…`）· `ruleprobe.sh check / play all / card / seg / dbparse`（纯 dotnet）。
> ⚠️ **白名单里的 `D:/4/Unity/数据/游戏数据/cards_engine.json` 不是真身** —— 生成器的落点是
> `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`gen_cards_engine.py:34 DST`）。改的是后者。

---

## 〇 · 结论（一句话）

**A/B/C/D 四件全部落地**：池内数据多出 **131 条** `unitConditions` 行（**125 张卡**：`multi` 8 · `mult` 27 ·
`avail` 14 · `choice` 82）；`CountFor` 的盘面分支**按兵种筛**（载具/步兵/兽/机蜂/单位各有判据，且与手牌/抽牌
两路**收口成一处**）；触发层补上 **`repeats`**（整条 ability 按 `multiCountCriteria` 触发 N 遍 ＋ 每条 op 按
`TryComputeRepeats` 结算 N 遍）；`CardDatabase` 那条「本版没实现」的告警**改对**。
`play` 基线**实测变 5 张**（`ASH46` / `GSC75` / `GOF31` / `TL68` / `UM77`，逐张的「变多/变少」见 §五），
**`PinnedPoolMd5` 需重钉**（`fafc0f56…` → `ab8c5238…`）。

---

## 一 · A（数据：`gen_cards_engine.py`）

### 1·1 改了什么

| 位置 | 改动 |
|---|---|
| `工具/gen_cards_engine.py:1822-1846` | 新增 `CONDITION_ROLES_OPTIONAL_COND = ("mult","multi","avail","choice")` ＋ `criteria_carries_targets(cr)`（判据写在函数 doc 里） |
| `工具/gen_cards_engine.py:1993-2010`（`condition_rows_of.add()`） | `conditionsFilter == 0` 时：**四格**且 `targetsAffected != 0` ⇒ **照落行**（`cond = 0` / `name = null`）；其余 role / 其余情况**一字不动** |
| 同文件 `:1780-1790` / `:1977-1982` | 落值口径的注释同步（保留「改前是怎么丢的」） |

**为什么不是「四格无条件全落」**（与 `C6` §1·3 A 的字面有一处**收窄**，理由写死在函数 doc）：
`targetsAffected == 0 (any)` 在原版 `GetTargets` 里**直接返回 1 个候选、其余 35 格全部失效**
（`AbilityLogic__GetTargets.c:190-192`，`Any disregards all other filters`）⇒ 这种 criteria 对消费端
**只有「没有乘数 / 只触发一次」这一个含义 = 与缺行同义**。全落的话会多出 **`multi` 1254 行 ＋ `mult` 1574 行**
零信息行，而且**让每一张卡都「有 unitConditions」**，把 `EffectText.AssignAbilityIndices` 的归位闸整体挪位
（那不在本笔范围内）。⇒ 只落 `ta != 0` 的 —— 实测正好 **8 条 `multi` ＋ 27 条 `mult`**，与 `C6` §四·1 的数字**逐字对上**。

### 1·2 池内数据 diff（`/tmp` 上改前/改后逐卡比）

```
新增行 131 条 · 受影响卡 125 张
  multi  8 : ASH52 DA57 AM18 GOF46 GSC56 SOR45 SW58 SW51
  mult  27 : AM44 BL40 BL45 BL48 BL49 BL51 BL52 DA45 DA49 DA73 EC45 GOF4 GSC37 GSC35 GSC47 GSC59
             GSC75 SAU75 SOR49 SOR71 SOR74 SW58 UM7(×2 条) UM45 38871853…(Refuse to Yield) 8f2da557…(Alpha Warrior)
  avail 14 : DA56 DA73 GSC55 SAU57 20d0f445…(Da Red Waaagh) 9dcbe3d0…(Mekaniak) 9f4e1b7e…(More Dakka)
             98d3c657…(Ramshackle) 508002f5…(Wreckin' Ball) TAU47 UM62 UM63 c4ff2790…(Scryer's Gaze) UM61
  choice 82: 见 §1·4 的真值行清单（全部是 `-3,-1,choice,0,0`）
```
`unitConditions` 总量：**167 张 / 218 条 → 285 张 / 349 条**（+118 张 / +131 条）。

### 1·3 消费端要一起改的两处（已改）

* `CardDatabase.MapUnitConditions`：改前 `cond == 0` **一律丢弃并 LogError** ⇒ A 落下来的行会被它再丢一次。
  现在 `IsUnconditionalCriteriaRole(role)`（`:1742`）对四格放行，其余 role **照旧丢弃**（`:1782`）。
* `CardDef.ConditionOf` / `ConditionOf` 的 `0` 语义**不需要改**（`0 = 无条件`，本来就成立）。
  ⚠️ **`CardDef.cs:1875` 那行注释**（`Condition`「`0` = none（本列不落这种行）」）**已经过期**，
  但 `CardDef.cs` **不在本笔白名单** ⇒ 留给主对话一句话改掉（不影响行为）。

### 1·4 `RuleEngineTest.cs` 会红的断言（⛔ 该文件不在白名单，本报告**只给可粘贴的替换内容**）

`TestW64UnitConditions`（`RuleEngineTest.cs:34841`）里三条硬编码计数 + 一张 167 行真值表：

| 行 | 现写 | 应改成 |
|---|---|---|
| `:34856` | `Check(withRows, 167, …)` | **285** |
| `:34859` | `Check(rowTotal, 218, …)` | **349** |
| `:34871` | `Check(truth.Count, 167, …)` | **285** |

`W64ConditionsTruth` 表：**原有 167 行里 160 行不变**，下面 **125 行**要**替换/新增**
（格式与表完全一致：`id|ab,lg,role,cond,ex[;…]`，行内按字符串序）：

```
09bcb1b3b8794c4429d82d4b2e68fa1f|-3,-1,choice,0,0;-3,0,nontarget,10,0
141c5ed661a9e42bd9a793f0d2a1c682|-3,-1,choice,0,0
20d0f4451f16fc441836a02f99bf77da|-3,-1,avail,0,0
38871853fabb5244d8148766829230cd|0,1,mult,0,0
40667e346ae58a84dbda1033dff70ccb|-3,-1,choice,0,0
4959cd9e99597f74ca9af0bd65ae6307|-3,-1,choice,0,0
4d348cff71208c24d8e39252d913ff9d|-3,-1,choice,0,0
4de4fcfb5d7e0554a88b9f419a2cd7c2|-3,-1,choice,0,0
508002f5d7ee54f4686aefa2e6adce41|-3,-1,avail,0,0
65b2be828179515458a282cf1b027182|-3,-1,choice,0,0
6ed51d59b9f8ea24ab0dc7ceaef7956c|-3,-1,choice,0,0
8482077f309b08943b190f18b5874871|-3,-1,choice,0,0
8f2da557fe484ee4bbf427c13eccd4cb|-3,-1,cardtarget,40,0;0,2,mult,0,0
98d3c65702746b04baf7f5ead1a9e507|-3,-1,avail,0,0
9dcbe3d0c04b02f4fbb19974ae0e37b2|-3,-1,avail,0,0;-3,-1,choice,0,0
9f4e1b7ea9f00da458c6a859a4e958c8|-3,-1,avail,0,0
AM18|0,-1,multi,0,0;0,0,target,220,1
AM4|-3,-1,choice,0,0
AM44|0,0,mult,0,0;0,0,target,220,1
AM52|-3,-1,choice,0,0
AM62|-3,-1,choice,0,0
ASH41|-3,-1,choice,0,0
ASH52|1,-1,multi,0,0
ASH57|-3,-1,choice,0,0
ASH71|-3,-1,choice,0,0
ASH80|-3,-1,choice,0,0
ASH82|-3,-1,choice,0,0
BL13|-3,-1,choice,0,0
BL23|-3,-1,choice,0,0
BL29|-3,-1,choice,0,0
BL37|-3,-1,choice,0,0
BL38|-3,-1,choice,0,0
BL4|-3,-1,choice,0,0
BL40|0,0,mult,0,0
BL45|0,0,mult,0,0
BL48|0,1,mult,0,0
BL49|0,1,mult,0,0
BL51|0,1,mult,0,0
BL52|1,0,mult,0,0
BL56|-3,-1,choice,0,0
BL57|-3,-1,choice,0,0
DA1|-3,-1,choice,0,0
DA11|-3,-1,choice,0,0
DA16|-3,-1,choice,0,0
DA39|-3,-1,choice,0,0
DA40|-3,-1,choice,0,0
DA42|-3,-1,choice,0,0
DA44|-3,-1,choice,0,0
DA45|0,0,mult,0,0
DA46|-3,-1,choice,0,0
DA49|0,1,mult,0,0
DA56|-3,-1,avail,0,0
DA57|0,-1,multi,0,0
DA71|-3,-1,choice,0,0
DA73|-3,-1,avail,0,0;0,1,mult,0,0
DA75|-3,-1,choice,0,0
EC10|-3,-1,choice,0,0
EC45|0,1,mult,0,0
EC59|-3,-1,choice,0,0
EC61|-3,-1,choice,0,0
GOF34|-3,-1,choice,0,0
GOF4|0,0,mult,0,0
GOF46|1,-1,multi,0,0
GOF93|-3,-1,choice,0,0
GSC13|-3,-1,choice,0,0
GSC29|-3,-1,choice,0,0
GSC35|0,0,mult,0,0
GSC37|2,0,mult,0,0
GSC44|-3,-1,choice,0,0
GSC47|0,0,mult,0,0
GSC50|-3,-1,choice,0,0
GSC55|-3,-1,avail,0,0
GSC56|0,0,target,220,1;1,-1,multi,0,0;1,0,target,220,1
GSC59|0,0,mult,0,0
GSC65|-3,-1,choice,0,0
GSC67|-3,-1,choice,0,0
GSC74|-3,-1,choice,0,0
GSC75|0,0,target,220,1;1,0,mult,0,0;1,0,target,220,1
SAU39|-3,-1,choice,0,0
SAU45|-3,-1,choice,0,0
SAU57|-3,-1,avail,0,0
SAU74|-3,-1,choice,0,0
SAU75|0,1,mult,0,0
SOR45|0,-1,multi,0,0
SOR49|0,1,mult,0,0
SOR71|0,0,mult,0,0
SOR74|0,1,mult,0,0
SW1|-3,-1,choice,0,0
SW48|-3,-1,choice,0,0
SW51|1,-1,multi,0,0
SW58|0,-1,multi,0,0;1,0,mult,0,0
TAU47|-3,-1,avail,0,0
TAU48|-3,-1,choice,0,0
TAU51|-3,-1,choice,0,0
TL20|-3,-1,choice,0,0
TL49|-3,-1,choice,0,0
TL50|-3,-1,choice,0,0
UM109|-3,-1,choice,0,0
UM110|-3,-1,choice,0,0
UM111|-3,-1,choice,0,0
UM112|-3,-1,choice,0,0
UM114|-3,-1,choice,0,0
UM115|-3,-1,choice,0,0
UM116|-3,-1,choice,0,0;2,0,target,40,0
UM117|-3,-1,choice,0,0
UM118|-3,-1,choice,0,0
UM45|0,1,mult,0,0
UM61|-3,-1,avail,0,0;-3,-1,choice,0,0
UM62|-3,-1,avail,0,0
UM63|-3,-1,avail,0,0;-3,-1,choice,0,0
UM7|0,0,mult,0,0;0,1,mult,0,0
UM74|-3,-1,choice,0,0
UM97|-3,-1,choice,0,0
a51adc8f5c311584385c3117cfd919e9|-3,-1,choice,0,0
af98d423c7279a14c84c0d0382c53541|-3,-1,choice,0,0
c0ddba6cc45944f46a65f4fd93bcc0a5|-3,-1,choice,0,0
c4f5c920744030c4e84428dc5a0f1bf2|-3,-1,choice,0,0
c4ff279078605aa42942c8dd16b77147|-3,-1,avail,0,0
cbabeb234380634479feab1a1b4c733a|-3,-1,choice,0,0
cc1400347b15ee3449bc18bbdb9ddec9|-3,-1,choice,0,0
d01a5ba5865ded4418194ea50e333b21|-3,-1,choice,0,0
e9952f50921d2844a94d39ce883430f4|-3,-1,choice,0,0
f2b80d575cbf8f64597b991022beaf35|-3,-1,choice,0,0
f923701f207d23a4f81d6ff5891b4a0c|-3,-1,choice,0,0
fd67b47572987b84cb2990ea1ecfe982|-3,-1,choice,0,0
```

（这 125 行是**从新卡表现算**的 `W64FormatConditions` 格式；原表里其余 160 行原样不动。
上表里 **7 张是原表已有、内容变长**的：`Path of the Seer` / `Alpha Warrior` / `AM18` / `AM44` / `GSC56` / `GSC75` / `UM116`。）

> **其它测试点不受影响**（已核）：`_UnitRoles` 的 `UM77` / `GOF74` / `avail` / `choice` 四条引用的都是**具体卡的具体行**，
> 那几张的行**没变**；`EC58` / `TL46` 的 `Conditions.Count` 断言同理。

---

## 二 · B（计数：`EffectResolver.CountFor` 的盘面分支）

| 位置 | 改动 |
|---|---|
| `Core/EffectResolver.cs:9935` | **新增唯一判据** `CountKindMatches(CardDef, kind)` —— 返回 `bool?`（`null` = 这个词本版不认） |
| `Core/EffectResolver.cs:9900-9915`（盘面分支） | 逐格 `CountKindMatches`；**改前只筛 `troop`**（`vehicle/infantry/daemon/beast/drone/unit` 一个都不筛） |
| `Core/EffectResolver.cs:9815-9830`（手牌分支） | 原来那条 `if/else` 白名单链**删掉**，改调同一份（宽度略变宽：`infantry` 等现在也认） |
| `Core/EffectResolver.cs:12499`（`DrawnMatches`） | 第三份 `switch` **删掉**，改 `CountKindMatches(...) ?? true`（保留改前「认不出 ⇒ 都算」的兜底语义） |

口径（都写在 `CountKindMatches` 的 doc 里）：词表仍是 `CreatePool.KindWords` **那一张**；
三条本层补丁 = ① `troop`/`unit` 的差别**只在督军**（盘面分支另有 `kind == "troop" && u.IsWarlord ⇒ 不算`）；
② `unit` **含督军**（`Type == "hero"`）；③ 手牌/抽牌两路的 `spell` = **战术卡**（`Type == "tactic"`），
不是 `KindWords` 里那个 `subtype = Spell` 行。

**效果**：`AM18` 的 `for each friendly Vehicle in play` 现在真的只数载具（`own|vehicle|all`）；
`GOF31` / `ASH46` / `GSC75` 那三张 `Repeat … for each Vehicle` 的重复次数一起被修正（见 §五的 `play` 实测）。

---

## 三 · C（引擎：触发层补 `repeats`）

### 3·1 三个新件

| 件 | 位置 | 干什么 |
|---|---|---|
| `TryComputeRepeats` | `EffectResolver.cs:11725` | **出牌路与触发路共用的「这一条 op 结算几遍」**（正文 `CountRef` → 结构化 `mult` → `RepeatTimes` → `fM`，优先序照改前出牌路**原样搬**，日志逐句不变） |
| `TryStructuredMultiTrigger` | `EffectResolver.cs:11679` | **整条 ability 触发 N 遍**：按 `op.AbilityIndex` 取 `role="multi"` 行 → `CountEval.CountTargets(multiSet)` |
| 触发重载的执行段 | `EffectResolver.cs:6400-6502` | `abilityRepeats` 外层 × `opRepeats` 内层：`for (r < abilityRepeats) for (i < ops.Count) { … for (k < opRepeats) ResolveOne(…) }` |

出牌路那边**只是换成调用共用函数**（`EffectResolver.cs:542-556`），算法与日志**逐字未变** ⇒ `play` 基线里
除「兵种筛」与「触发层新增 repeats」以外**没有任何差异**（实测见 §五）。

### 3·2 三条**口径决定**（都是本笔自己的判断，逐条给出依据 + 反例）

1. **`targetsAffected == 0 (any)` ⇒ 结构化数据「不适用」，⛔ 不覆盖正文。**
   （`TryStructuredMultiTrigger` doc①、`TryStructuredMultiplier` doc①）
   原版 `any` 确实让 `local_178 = 1`，但它同时让**其余 35 格全部失效** ⇒ 不携带「数谁」。
   反例 = `GOF90 Wurrboy`：外层 ability 的 `multiCountCriteria` 与 `multiplier` **都是 `any(0)`**，
   真正的「按友方部队数」写在**它挂的那张附魔**（`648c49… Wurrboy`）的 `multiplier.ta = 10` 上
   （`cards_raw.json` 现读）。按数据返回 1 会把卡面 `for each friendly troop` **压成 1 遍**。
2. **`CountScope == "died"` 在触发层不重复。**（`EffectResolver.cs:6466-6485`）
   `OtherUnitDeath(90)` / `UnitDeathByAbility(180)` **本身就是逐死亡事件**发一次 ⇒ 再乘「本回合累计阵亡数」
   就是**平方**；且原版这两张卡的 `multiCountCriteria` 现读 `ta = 0 (any)`（`AM34` / `TL46`）= 数据也说不重复。
3. **`PerCount` 增量型（「an additional +1 for each X」）在触发层不重复。**
   那一档在出牌路是「**数值加 N 次**」（`:489-540`），触发路还没有这一档 ⇒ 用 `repeats` 顶会把「加 N 次」
   变成「跑 N 遍」。今天保持改前口径（1 遍）并**出声**（`StructuredConditionNotice("trigger-repeat-percount|…")`）。
   ⇒ 这是一条**如实留着的缺口**（`GOF4` 的 `+1` 就属这一档，所以它的修正走的是**结构化 `mult` 那条路**，见 §三·3）。

### 3·3 `mult` 的两处补丁（`GOF4` 的根因）

| 补丁 | 位置 | 为什么 |
|---|---|---|
| `filterSpecificCards` 那一档计数 | `CountMultiplierTargets` `:11945` / `CountSpecificCardsOnBoard` `:11965` | `CountEval.SetGap` 把 `filterSpecificCards` 判成「未实现」⇒ `GOF4` 的整个 `multiplier`（`filterSpecificCards = [自己]` ＋ `includeSelf = 0`）**永远取不到**。本笔按 `CheckIfMeetsCriteria.c` 逐句补「在场上 + 指定卡 + 阵营/卡种/自他闸 + cond」这五层；**只支持 `ta ∈ {10,55}`**，其余照实判不了。⛔ **等 `CountEval` 支持这一维后删掉这两段** |
| 归位兜底 `FindSoleMultiplierRow` `:11905` | 只在 **`op.AbilityIndex < 0`**（归位门没过）＋ **整卡只有一条 ability 且那条只有一条 logic**（`AbilityTargets` 判）＋ 唯一一条「有值」的 `mult` 行时生效 | `EffectText.AssignBlockIndicesOnly` 的片段门（整卡只有一条 ability）把 `GOF4`（`Tide 1. Rally: …` = 两个能力块）这类卡的触发正文**一律留在 `-1`** ⇒ 不兜底就取不到那条乘数。⛔ **等归位门补上后删掉** |

⚠️ **兜底是踩了两次坑之后才收紧的**（两次都被 `play all` 抓到，如实记）：
* 第 1 版「归不到就兜底」⇒ `GOF74` 的 `Give Armour 1 to your Warlord`（`(0,0)`，本不该有乘数）
  被兜到同卡 `(0,1)` 那条 `cond=40` 的乘数上 ⇒ **按 0 遍把 Armour 1 整条抹掉**（`play GOF74` 实测）。
  ⇒ 加闸「**只有 `AbilityIndex < 0` 才兜底**」。
* 第 2 版仍会把 `UM77` 的 `Deal 1-2 damage`（也是 `-1`，同卡有 2 条 ability）兜到 `(1,0)` 上 ⇒ 伤害 4 → 0。
  ⇒ 再加「**整卡单 ability 单 logic**」的结构闸。

---

## 四 · D（`CardDatabase.cs` 的告警）

`CardDatabase.cs:1855-1868`：原来那条「本版**没有实现** ability 级重复 ⇒ 这一格按 1 遍处理」**两半都过期**
（A 让数据落下来、C 让引擎实现了）⇒ 改成一条**数据形状校验**：`role == "multi"` 但 `multiSet == null`
（旧表 / 生成器漏落）⇒ `LogWarning`「触发层只能退回正文口径」。⛔ 不再是「缺口告警」。

---

## 五 · 15 张外溢逐卡（`C6` §1·5 的表，逐张判 + 实测）

**判据工具**：`cards_raw.json` 的 `multiCountCriteria` / `multiplier` 字段 +
`ABILITY_TRIGGER_KEYWORD`（`gen_cards_engine.py:1213-1236`：**240 / 410 没有映射** ⇒ 那两类触发层根本不发）。

| 卡 | 触发 | 补完后会多触发什么 | 依据 |
|---|---|---|---|
| **`AM18`** | 40 TurnEnd | **回合末打 N 次 3 点**（N = 友方载具数；0 台 ⇒ 0 次）—— **本批主目标** | `multi.ta=10`＋正文 `own|vehicle|all`（B 修好后才是载具） |
| `ASH52` | 600 | **不变**（战术卡没有 `SpiritOps`；600 正文走 `SpiritOps`，不是 `TriggerOps`） | `ABILITY_TRIGGER_KEYWORD[600]="spiritstone"` |
| `AM34` | 180 | **不变**（`died` 档**显式不重复**，见 §三·2②） | 逐死亡事件触发 |
| **`BL31`** | 580 Strike | **「Deal 1 damage to all enemies」按 Dark Pact 层数打 N 遍**（改前 1 遍） | `CountScope=darkpact` ⇒ `CountFor` 求和 = N |
| `BL52` | 240 | **不变**（240 `WhileInPlayVariable` **没有关键词映射** ⇒ 触发层不发） | 同上 |
| `GSC37` | 670 / 240 | **不变**（670 Ambush 正文无 `for each`；240 同上不发） | — |
| **`GOF90`** | 40 TurnEnd | **回合末打 N 次 1-2 点**（N = 友方部队数，含自己 —— `multi.includeSelf=1`） | 正文 `own|troop|all` |
| **`SOR71`** | 605（Rally 走关键词 `rally`） | **Rally 的「gain 1 ☀ for each enemy unit」按 N 次给**（改前 1 次） | 正文 `enemy|any|all` |
| `SW58` | 240 | **不变**（240 不发；出牌那条本来就是出牌路） | — |
| **`TL68`** | 46 Talent / Rally | **Rally 的 +2/+2/+1 按「受伤敌人数」遍**（改前 1 遍；0 个受伤 ⇒ 0） | 正文 `enemy|any|damaged` |
| `TL46` | 90 | **不变**（`died` 档不重复） | 同 `AM34` |
| `708c…` Smothering Decree | 90 | **不变**（`died` 档不重复） | 同 `AM34` |
| `GOF31` | 410 | **不变**（410 不发；它的 `Repeat …` 在**出牌路**，已经修好 —— 见下） | — |
| `EC42` | 410 | **不变**（410 不发） | — |
| `GOF104` | 410 | **不变**（410 不发） | — |

**另外一张不在那 15 张里、但会变**：**`UM77 Chairon`**（`Deal 1-2 damage … Repeat for each friendly troop
that died since your last turn`）—— 改前「`repeat` op **无条件跑一遍**」⇒ 打 2 次；现在按真实计数
（本回合没人死 = 0）⇒ **只打 1 次**。卡面语义正是「按死了几个重复几次」。

### `play` 基线实测 diff（`ruleprobe.sh play all`，改前/改后逐卡比）

```
全池 1127 行，只有 5 行变（其余逐字节相同）：
  ASH46 Aerial Dominion  dmg 15 → 6   other 10 → 7    「Repeat … every friendly Vehicle」：全池 4 个「伪载具」→ 真 1 台
  GSC75 Roaming Outriders dmg 5 → 2   other  8 → 7    「Repeat for each Vehicle …」：同上（4 → 1）
  GOF31 Cloud of Smoke   other 7 → 6  dmg 0 → 0       「Repeat this effect for each friendly Vehicle」
  TL68  Psychophage      other 5 → 5  selfPlace 0 → 1 「for each damaged enemy」：夹具里受伤敌人 0 ⇒ 不再白给一次
  UM77  Chairon          dmg 4 → 2    other 8 → 5     见上（repeat 按真实计数）
```
**⚠️ `GOF4` 的修正（+1 → +0）在 `play` 里看不见** —— 原因是**探针自己的读列**（见 §八·1）。
**⚠️ 这 5 行都要重钉 `工具/ruleprobe/基线/out_play_baseline.txt`**（`play check` 会红，属**预期**）。

---

## 六 · 断言（新分册 `Editor/RuleEngineTest_G6.cs`，入口 `TestG6MultiCountRepeats`）

> 🔴 **`RuleEngineTest.cs` 的 `Step(TestG6MultiCountRepeats)` 由主对话加**（本笔不碰那个文件）。

| 组 | 断言（逐条） | 灭自证 / 🧨 |
|---|---|---|
| ① `AM18` | **三档**：友方载具 0 / 1 / 3 台 ⇒ 回合末打 **0 / 3 / 9** 点（`RuleCore.EndTurn`） | 🧨 触发层没有 `repeats` ⇒ 恒 3；🧨 盘面不筛兵种 ⇒ 把 AM18 自己 + 督军数进去（`(N+2)×3`） |
| ② `GOF4` | ① 只有自己 ⇒ 攻击力 = 卡面 **2**（乘 0）；② 场上另有 1 张 ⇒ **3** | 🧨 `mult` 行没落 / `filterSpecificCards` 没补 / `actor` 没传 ⇒ ① 实得 3 |
| ③ 触发层 `repeats`（合成夹具） | 合成卡（`multi.ta=10`）＋ 3 个别的友方部队 ⇒ 打 **3** 点 | **灭自证 A**：同卡**去掉** `multi` 行 ⇒ **1** 点；**灭自证 B**：`ta = 0 (any)` ⇒ **1** 点（`any` 不携带信息） |
| ④ 兵种筛（`CountForTest`） | `own\|vehicle\|all`=**2** · `own\|infantry\|all`=**1** · `own\|beast\|all`=**1** · `own\|troop\|all`=**3**（不含督军）· `own\|any\|all`=**4**（含督军） | 🧨 退回「只筛 troop」⇒ vehicle 读 3；🧨 `troop`/`unit` 混成一个映射 ⇒ troop 读 4；🧨 `any` 也排督军 ⇒ 读 3 |

每条断言都在文件里带 `🧨`（什么改动能让它红）与**写死的字面量**（`0/3/9/2/3/1`），⛔ 不从实现常量反算。

---

## 七 · 要重钉的指纹 / 基线

| 项 | 现值 → 新值 |
|---|---|
| `工具/ruleprobe/Program.cs` 的 **`PinnedPoolMd5`** | `fafc0f56…`（实测全值 `fafc0f562db54f139b29891b119b8c7e`）→ **`ab8c5238a7077846a2dfab42278b2689`**（文件 1,416,841 B） |
| `工具/ruleprobe/基线/out_play_baseline.txt` | 需重钉：**5 行变**（§五逐行） |
| `PinnedParseMd5` | **本笔没有改解析层** ⇒ 不必动。（实测：用**旧卡池**跑 `check` 也是 40 行差异 + 1 条哨兵失败 ⇒ **那批红是先前就有的**，不是本笔引入 —— 见 §八·5） |
| 池指纹哨兵 | 现读「卡池 md5 与钉住的**不同** ⇒ 本哨兵**此时不判**」—— 重钉后恢复生效 |
| `RuleEngineTest.cs` 三条计数 + 真值表 | 见 §1·4（**该文件不在本笔白名单**） |

---

## 八 · 没查清 / 没做

1. 🔴 **`工具/ruleprobe/Program.cs:743-809` 有自己的一份 `unitConditions` 读列**（口径注释写着「与 `CardDatabase.MapUnitConditions` 同口径」），
   但它**仍然丢 `cond == 0`** ⇒ **探针看不见本笔新增的 131 行**：`card`/`play` 的读数里 `GOF4` 仍是 0 条条件
   （`play GOF4` 的 Rally 仍按正文 `own|any|all` 数 5 遍）。⇒ `play` 只验到了 B 与 C 的**正文那条腿**；
   **`GOF4` 的结构化那条腿只能靠 Unity 里的新分册验**。⛔ 该文件不在白名单，**请主对话同步更新**（否则探针与引擎口径继续分叉）。
2. ⚠️ **`TL68` / `GOF74` 的 `mult` 仍走正文口径**：它们的 op 归位不到 `(ab, lg)`，而兜底的两道闸
   （`AbilityIndex < 0` ＋ 整卡单 ability 单 logic）把它们挡在外面是**有意的**（否则会误伤 `UM77`/`GOF74` 的其它 op）。
   结构性根治 = `EffectText.AssignAbilityIndices` 的片段门（`EffectText.cs` **不在白名单**）⇒ 留账。
3. ⚠️ **`GSC75` 的正文解析缺陷（顺手发现，未改）**：`Repeat for each Vehicle in your hand` 被
   `EffectText.ClassifyCount` 判成 **`countScope=board`**（`own|vehicle|all`）—— ⓪b 那条手牌正则要求
   `in **the** your hand`（多个 `the`），"in your hand" 匹配不上 ⇒ 落到 ④ 盘面。现在是「数场上的载具」
   （改前是「数场上全体」）。判据：卡面原文 + 原版 `inHandFiltered(50)`。
4. ⚠️ **`GOF90` 的乘数在附魔卡上**（`648c49… Wurrboy`）：我们引擎不建模「附魔链」⇒ 走正文。这不是本笔引入的，
   但**它是「按数据返 1 会把正文压成 1 遍」这条口径决定的唯一实证**，记在这里备查。
5. ℹ️ **`ruleprobe.sh check` 现读红**（40 行解析差异 + 1 条输出指纹哨兵失败）—— 用**改前卡池**跑同一份代码
   **也是 40 行 + 1 条**，⇒ **先前就存在**（与本笔的 A/B/C/D 无关；成因大概率是同日另一位写手对
   `EffectText` 的改动）。附：`全池 1121 行`（基线时代的池张数）也早就与现池 1127 不同。
6. ℹ️ **`CardDef.cs:1875` 的注释过期**（`Condition`「`0` = none（本列不落这种行）」）—— 见 §1·3。
7. ℹ️ **本笔未跑 Unity** ⇒ 新分册的四组断言**尚未实跑**；`RuleEngineTest.Run` 的合计条数也未知。

---

## 九 · 顺手发现（⛔ 只报不改，由调度台分流）

1. **白名单路径写错两处**（都无害，但下次别照抄）：
   * `D:/4/Unity/数据/游戏数据/cards_engine.json` **不存在** —— 真身是
     `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`（`gen_cards_engine.py:34`）；
   * 任务简报说「`EffectResolver.cs` / `CardDatabase.cs` 为 CRLF；`gen_cards_engine.py` / `cards_engine.json` 纯 LF」——
     **实测（HEAD 与工作区都是）**：`EffectResolver.cs` = **CRLF** ✓、`CardDatabase.cs` = **LF**（简报说 CRLF）、
     `gen_cards_engine.py` = **CRLF**（简报说 LF）、`cards_engine.json` = **单行无换行**。
     本笔**没有翻任何文件的行尾**（改完逐个数过：CRLF 数 == LF 数 或全 LF，与 HEAD 一致）。
2. **`RuleEngineTest_UnitRoles.cs` 的 `N2RoleMult` 断言依赖「卡池只有 4 条 `mult` 行」的隐含前提？**
   实测**不依赖**：它按 `(ab,lg)` 取 `GOF74` 的那一条，本笔没有改动那一条。✅ 无需动。
3. **`play` 探针的 `nUcCards/nUcRows` 摘要行**（`Program.cs:1070`）与 `roleSet` 的「对账期望
   4/1/3/2=10」同样是**旧口径硬编码**（`Program.cs:1096`），A 之后应为 **mult 31 · multi 9 · avail 17 · choice 84**。
4. **`GSC75` 的 `lowercost` 与 `repeat` 是两条独立 op**：`repeat` 按载具数重复（可能 0 次时）**不会**连带
   「lower their cost by 1」—— `lowercost` 在 `repeat` 之后单独跑一次。卡面那句是「Repeat … **and** lower their cost」，
   今天的行为（降 1 费 1 次）是不是原版口径**没查**（判据要看 `lowercost` 的 `payload=(指代上一张)` 与
   `repeat` 的 `RepeatOps` 边界）⇒ 立账。
5. **`CountSpecificCardsOnBoard` 只支持 `ta ∈ {10,55}`**（在场上）：`filterSpecificCards` 落在其它集合族
   （手牌 / 墓地 / 牌库）时**如实判不了**并出声 —— 现池只有 `GOF4` 用到这一维，`ta=10`。
