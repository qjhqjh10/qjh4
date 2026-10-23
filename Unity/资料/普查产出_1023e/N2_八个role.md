# N2 · `新账-95/97` 的 8 个 `unitConditions` role 逐条判定与处置（2026-10-24）

> 执行代理 **N2**（有写权限）。白名单内只动了 `Assets/RuleEngine/**`、`工具/gen_cards_engine.py`、
> `工具/ruleprobe/Program.cs`（`ruleprobe*` 属白名单）与**本报告**。
> ⛔ 未动 `CardPresentation/**` · `WarpforgeVFX/**` · `CLAUDE.md` · `项目任务.md` · 任何 `任务总表.md` · git ·
> **任何 `Pinned*Md5` 常量**（只报告「该重钉成什么」，见 §五）。
> ⛔ 未跑 Unity（红线）；`.cs` 改完每次都跑了 `TMPDIR=/tmp/wf_n2 bash 工具/typecheck.sh`。

---

## 〇、验收读数（本批实跑）

| 检查 | 读数 |
| --- | --- |
| `TMPDIR=/tmp/wf_n2 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（最后一次；中途报过 3 条错，**全在 `Editor/RuleEngineTest_EntryPump.cs`** = N1 当时正在写的文件 ⇒ 按简报「不是你改的段落别动」，未碰，随后自动消失） |
| `bash 工具/ruleprobe.sh check` | **rc=0** · 解析差异 **0 行** · 改名 0 · 新增/消失 0 · 哨兵 **0 条失败** · ⑦ **输出指纹 `31d298eb…` 与钉住的一致**（= **解析输出逐字节没动**） |
| `bash 工具/ruleprobe.sh play all dump` | 改前（02:41）↔ 改后（02:56）**全池 1127 张逐卡真打**：**只有 `GSC55` 一张不同**，且**不是本批引起** —— 见 §五·①（`RuleCore.cs`/`RuleCodes.cs` 在 02:45:51~02:45:56 被**另一个代理**改过；我 02:48 的中途快照里它就已经变了，而那一版**尚未含**本批任何行为相关改动） |
| `python 工具/gen_diag_runner.py --check` | ✅ **逐字节相同**（309 条用例；本批新增 1 条，已重生成） |
| Unity 自检 | ⛔ **未跑**（红线：子代理不跑 Unity）⇒ **本批新增的 5 组断言尚未执行过**，等调度台在同步点跑 `RuleEngineTest.Run` |

行尾复核（逐字节数 `\r\n` 与 `\n`）：`gen_cards_engine.py` **CRLF 5370/5370**、`EffectResolver.cs`
**CRLF 12583/12583**、`RuleEngineTest.cs` **CRLF 36662/36662** —— **三份 CRLF 文件都没被翻**；
其余（`CardDef.cs` / `CardDatabase.cs` / `CountEval.cs` / `EffectText.cs` / `Program.cs` /
`RuleEngineTest_Diag.cs` / 新文件）本来就 **LF**，也没翻。**没有「整篇重写」**。

---

## 一、逐 role 表（语义 / 消费点 / 处置 / 断言 / 判据出处）

| role | 行数 | 原版语义 | 原版消费点 | 本批判定 | 我们这边 |
| --- | --- | --- | --- | --- | --- |
| **`mult`** | **4**（`GOF74`·`TAU75`·`TL68`·`UM77`） | `abilityLogic[].multiplier`（一条 `TargetCriteria`）＝**效果数值按「满足条件的单位个数」放大**：`targetsAffected == any(0)` ⇒ 1；`210/220` ⇒ **累加 `traitsFilter` 的值**；其余 ⇒ `GetTargets()` 的表长；随后 `if (0 < furtherMultiplier) n *= furtherMultiplier` | `AbilityLogic__PlayAbility.c:185-236`（`local_178` 那一支；下游 `:671/:708/:839/:918/…` 全是 `local_178`） | ✅ **接上**（数据 + 消费端 + 断言） | 生成器把 `multiplier` 落成 **`multiplierSet`（36 格）**；消费者 `EffectResolver.TryStructuredMultiplier`（`:11092`）→ `CountEval.CountTargets`（`:135`） |
| **`multi`** | **1**（`UM77`） | `CardAbility.multiCountCriteria`（`+0x40`）＝**`targetsAffected != 0` 时整条 ability 触发 N 遍**（`N` = `GetTargets(multiCountCriteria)` 个数）；`0` ⇒ **只触发一次** | `RawCardScript__TriggerAbilities.c:246-258`（`GetTargets(...)` 之后 `for (i < list.Count) TriggerAbility(...)`） | ✅ **数据接上 + 闸门如实实现**；**ability 级重复本版未实现**（今天**零可达**，见 §三·②） | 生成器落 **`multiSet`（36 格）**；`CardDatabase.MapUnitConditions` 在 `targetsAffected != 0` 时 **`LogWarning` 出声**（`:1826-1842` 那段）。`UM77` 的 `targetsAffected = 0` ⇒ **与原版一致：触发 1 次**，行为不变 |
| **`avail`** | **3**（`EC39`·`SAU11`·`SOR42`） | `RawCardScript.targetsAvailableCounter`（`+0x298`）＝**UI 上显示「有几个可点目标」** | `BattleCardUI__ShowAvailableTargets.c:42` → `CardScript.GetTargetsAvailable`（**唯一读点**） | ⏸ **引擎侧不实现**（判据：原版没有任何规则消费点）+ **数据落全** + **「已覆盖」的证明写成断言** | 生成器落 **`availSet`（36 格）**；**不建引擎消费者**。三张卡的同一条条件**同时**落在 ability 级 `target` 行上 ⇒ 那批单位**已经**被筛过（断言钉住） |
| **`choice`** | **2**（`EC39`·`SOR42`） | `RawCardScript.choicesDefinition`（`+0x2A0`）＝**选牌候选池**（`targetsAffected` 决定候选域，`conditionsFilter` 筛，`maxTargets` = 面板摆几张） | `BattleManager__GetChoiceFullPool.c:28-35` → `GetTargets(choicesDefinition)`；面板张数 `GetChoiceOptions.c:40-45` | ✅ **接上（用正文那条等价路径）+ 数据落全 + 一致性断言** | 生成器落 **`choiceSet`（36 格）**；两张卡的正文解析出 `choosecard/dead/{CDS:since_last_turn}`（= 墓地 + 自上个回合起），与 `choiceSet`（`inCemeteryFiltered` + `diedSinceLastTurn(222)`）**指向同一批候选** ⇒ 不新建第二条消费者 |
| **`nontarget`** | **20** | `CardScript.nonTargetScoringCriteria[]`（`ScoringCriteria.targetCriteria`）＝**AI 评分的「非目标侧」加分** | `AI__ScoreFromPlayingNow.c:54` → `AI__ScoreFromCriteria.c` | ✅ **已有消费者，本批做「两份数据源一致」的断言**（⛔ 不再接第二个消费者） | 数据在 **`Resources/ai_scoring.json`**（`nonTargetScoringCriteria[].targetCriteria.conditionsFilter`）；消费者 = `AiScoring.ScoreFromPlayingNow` → `AiTargets.MeetsCondition` → `ConditionEval` |
| **`targetscore`** | **6** | `cardTargetScoringCriteria[]`（同上，**目标侧**加分；先过 `CheckIfMeetsCriteria(card, target, target, criteria)`） | `AI__ScoreFromPlayingNow.c:32-46` | ✅ 同 `nontarget` | 同上（`cardTargetScoringCriteria`） |
| `target` | 162 | — | — | ✅ 早接（A1 之前） | — |
| `acting` | 1 | — | — | ✅ 早接 | — |
| `count` | 6 | — | — | ✅ `新账-97 ④` 已接 | — |
| `cardtarget` | 13 | — | — | ✅ A1 已接 | — |

**合计本批处置 = 4 + 1 + 3 + 2 + 20 + 6 = 36 行 / 8 个 role**（台账写「49 行」是「含 `cardtarget` 13 行」的口径；
`cardtarget` 已由 A1 接掉 ⇒ **本批范围就是这 36 行**，现读逐 role 计数 =
`mult 4 · multi 1 · avail 3 · choice 2 · nontarget 20 · targetscore 6`，与生成器摘要逐字吻合）。

---

## 二、数据侧：新增了什么、为什么必须加、加了之后行为变没变

### 2·1 `cards_engine.json` v21 → **v22**（只加「键」，不改任何既有键）

`unitConditions` 的 `mult` / `multi` / `avail` / `choice` **四行**各带一个**整条 criteria 对象**：

| 行 | 新键 | 形状 |
| --- | --- | --- |
| `mult` | `multiplierSet` | **36 格，与 `countSet` 逐字同形**（`playerTurn` 恒 `-1`，见下） |
| `multi` | `multiSet` | 同上 |
| `avail` | `availSet` | 同上 |
| `choice` | `choiceSet` | 同上 |

* **为什么必须落**：`cond` 只回答「条件是什么」，**回答不了「数谁 / 候选池是谁」**。
  这与 `count` 在 v16 前缺 `countSet` 是**同一个缺口**（A1 报告 §2·4·1 的判据）。
* 🔴 **`playerTurn` 结构上不适用**这四格（它是 `BoardCriteria` 独有的，这四格是纯 `TargetCriteria`）
  ⇒ 生成器落 `-1` **且不进 `set_bad`**；`CardDatabase.MapCountSetDto(noPlayerTurn: true)` 那边
  **反过来**：这四格**带着** 0/10/20 才是坏数据（出声，⛔ 不静默当闸）。
* 🔴 **`null` = 未知**（旧表 / 这一格不是那个 role），⛔ **不是「默认 criteria」**
  （全 0 的 `targetsAffected = any` 是一个**具体的**选择）。
* 生成器：`count_set_of` 上提为 **`criteria_set_of`**（同一套 36 格字段表，⛔ 别为每个 role 各写一份），
  `count_set_of` 降为它的一层薄壳。

### 2·2 卡表 diff（结构性核对，`--write` 之后跑的脚本对账）

| 项 | 读数 |
| --- | --- |
| `version` | 21 → **22** |
| 卡数 / id 集合 | 1127 → 1127 · **id 集合完全相同** |
| 顶层其它键 | **0 处变化**（`source` / `note` / 各报告表逐字节相同） |
| **内容变了的卡** | **恰好 7 张**：`EC39`·`SOR42`（avail+choice）·`SAU11`（avail）·`GOF74`·`TAU75`·`TL68`·`UM77`（mult；`UM77` 另带 multi） |
| 每张卡变了的键 | **只有 `unitConditions` 一列**，其余列 0 漂移 |
| 文件大小 | 1,295,933 B → **1,303,454 B**（+7.5 KB） |
| 现行 md5 | **`fafc0f562db54f139b29891b119b8c7e`**（⇒ **`PinnedPoolMd5` 待重钉**，见 §五·②） |

### 2·3 行为影响：**全池零变化**

`play all dump` 改前/改后逐卡真打，**1127 张里 0 张因本批而变**（唯一不同的 `GSC55` 归因见 §五·①）。
四张 `mult` 卡在夹具里都数出 0（夹具的敌人全是满血，`cond 40 damaged` 一个都不命中），
而**正文给的那个计数同样是 0** ⇒ 结构化值与正文值**相等**，不产生差异 —— 这正是「接上但零行为变化」的
预期形状（也说明这一格**今天还没进入「数据纠正正文」的状态**；`UM77` 的 `diedSinceLastTurn` vs 正文
`DiedThisTurn` 那个**潜在**偏差见 §五·③）。

---

## 三、逐条「接了什么 / 断言是什么 / 什么能让它红」

### ① `mult` —— **接了，且是唯一有行为潜力的那一格**

* **数据**：`multiplierSet`（36 格）＋ 既有 `abilityMultipliers`（`furtherMultiplier`，v20 已接）。
* **消费端三处**（`Core/EffectResolver.cs`）：
  1. `TryStructuredMultiplier`（`:11092`）：按 `(op.AbilityIndex, op.LogicIndex)` 取 `mult` 行 →
     `CountEval.CountTargets`（`targetsAffected == 0` ⇒ 1；`210/220` ⇒ **如实判不了**，见 §四·①）。
  2. `ResolveOps` 的 **`for each` 增量型**那一支（`op.PerCount > 0`）：结构化值**优先**，与正文计数
     不一致时 `ctx.Log` 出声。
  3. `ResolveOps` 的 **`for each` 重复型**那一支（`repeats`）：同上。
* 🔴 **为什么「结构化优先」而不是「正文优先」**：`furtherMultiplier` 那一格是「卡面印死的常数」
  （所以 A12 的口径是「正文给过数就只对账、不乘」）；`multiplier` 不同 —— 它决定**数谁**，
  正文只是它的转写。**数据 > 文本**是本工程 B-16 起就定的方向。
* 🔴 **配套改动（否则这一格一条都取不到）**：`EffectText.AssignAbilityIndices` 原来在
  「一条 `role=target` 行都没有」时**直接 `return`** ⇒ `EffectOp.AbilityIndex` 永远是 `-1`
  ⇒ `TryGetCondition(-1,-1,"mult")` 恒失败。现改为：没有 `target` 行时走
  **`AssignBlockIndicesOnly`（新，只标 `(ab, lg)`，⛔ 不碰 `TargetCondition`）**。
  受这条新路径影响的 = **7 张**（`GOF74`/`TAU75`/`TL68` `mult` · `AM43`/`EC58`/`SOR70` `count` ·
  `DA22` 只带 `acting` 行）；是否真标上还各过自己的块数闸（`DA22` 的 `ab=1`）。
  **这 7 张在 `play all dump` 里全部零变化**（逐卡真打对过）。
* **断言**（`Editor/RuleEngineTest_UnitRoles.cs` → `N2RoleMult`，两条判别式成对）：
  * A：**2 个受伤敌人 + 1 个满血敌人** ⇒ 乘数 **2**（督军 `MaxHealth` **+2**）。
    🧨 数「全部敌人」⇒ 3；不看 `conditionsFilter` ⇒ 3；结构化乘数没接上 ⇒ 1（`repeats` 初值）。
  * B：**3 个敌人都满血** ⇒ 乘数 **0** ⇒ **一遍都不结算**（`MaxHealth` 不变）。
    🧨 忽略条件 ⇒ 3。
  * 两条都先把正文的 `for each` 从句**摘掉**（`CountRef/CountScope/PerCount` 清空）——
    否则量的是正文那条路，**结构乘数根本没被考到**（自证）。

### ② `multi` —— **数据接上 + 闸门如实实现；ability 级重复本版未实现（今天零可达）**

* `UM77` 的 `multiCountCriteria.targetsAffected = 0 (any)` ⇒ 原版 `TriggerAbilities.c:246-252`
  那一支 `goto` 到「**只触发一次**」 ⇒ **这张卡这一格今天不是重复源**；真正的重复来自
  `mult` 行（`inCemeteryFiltered(60)`）。两条一起断，才能分清「哪一格在驱动遍数」。
* **没有实现的那一半**：`targetsAffected != 0` ⇒ 「整条 ability 触发 N 遍」。
  这是**触发点的结构性改动**（不是某个 verb 内的事），今天**池内 0 行**命中。
  ⛔ 不静默：`CardDatabase.MapUnitConditions` 一旦读到这种行就 **`LogWarning`**（逐卡一次）。
* **断言**（`N2RoleMulti`）：`multiSet.targetsAffected == 0` · `friendlyUnits=1` · `includeMinions=1` ·
  `includeHero=0` · `cond=222`；而 `multiplierSet.targetsAffected == 60`。
  🧨 把两格弄反 / 把 0 当成「数一遍」⇒ 红。

### ③ `avail` —— **引擎不实现（判据写死）+ 数据落全 + “已覆盖”写成断言**

* **判据（为什么引擎不做）**：全量反编译里 `targetsAvailableCounter`(`+0x298`) 的读点**只有 UI**：
  `BattleCardUI__ShowAvailableTargets.c:34`（`ShouldShowTargetsAvailable` 闸）`:39`（`CardScript.GetTargetsAvailable`）
  `:42`（读 `RawCardScript + 0x298`）。**没有任何 `BattleManager` / `AbilityLogic` 读它** ⇒ 它不是规则数据。
  UI 那一半属 `CardPresentation`（本代理白名单外）⇒ **记成 UI 线的活，不是引擎缺口**。
* **数据落全**（`availSet`），并**证明不是「漏筛」**：三张卡的同一条条件
  （`EC39` 222 / `SAU11` 340 / `SOR42` 222）**同时**出现在 ability 级 `target` 行上 ⇒ 那批单位
  已经被 `target` 那条路筛过。
* **断言**（`N2RoleAvail`）：逐卡 `availSet != null` · `maxTargets == 0`（`targetsAvailableCounter`
  没有这一格）· **`avail.Condition` 在 `role=target` 的 `cond` 集合里**。
  🧨 把 `choicesDefinition` 误读成 `targetsAvailableCounter` ⇒ `maxTargets` 实得 3（红）；
  🧨 两处条件漂移 ⇒ 第二条红（那时「已覆盖」的结论就不成立）。

### ④ `choice` —— **正文那条等价路径已实现该池；本批补数据 + 一致性断言**

* `EC39`/`SOR42`：`choicesDefinition = {60 inCemeteryFiltered, friendly, minions, cond 222, maxTargets 3}`；
  正文 = `choosecard/dead/friendly troop/…/{CDS:since_last_turn}`（`ChooseDeadScope = since_last_turn`
  ⇔ `conditionsFilter = diedSinceLastTurn(222)`）。
* **不新建第二条消费者**（两处写同一条规则 = 迟早不一致）；改为**把两处的一致性钉成断言**。
* **断言**（`N2RoleChoice`）：`choiceSet.maxTargets == 3` · `targetsAffected == 60` · `cond == 222` ·
  `friendlyUnits == 1`；且正文解析出 `choosecard/ChooseSrc=="dead"` + `ChooseDeadScope=="since_last_turn"`。
  🧨 与 `availSet` 弄反 ⇒ `maxTargets` 实得 0（红）；🧨 解析层丢掉 `since_last_turn`
  （候选变成「本局阵亡」）⇒ 第二条红。

### ⑤ `nontarget` / `targetscore` —— **消费者早就在，本批把「两份数据源一致」钉住**

* **`ScoringCriteria` 的分值列并不是「缺数据列」**：真值里有
  `baseValueFriend / baseValueEnemy / multiplyByFriend / multiplyByEnemy / scoringOption`，
  且**已经落到 `Resources/ai_scoring.json`**（`gen_ai_scoring.py` 产），
  消费者 = `AiScoring.ScoreFromPlayingNow` → `ScoreFromCriteria` → `AiTargets`。
  ⇒ 这三格**不需要**加进 `cards_engine.json`（⛔ 别为了「统一」再落一份）。
* **断言**（`N2RoleScoring`）：
  1. 池内 `nontarget + targetscore` **恰好 26 条**（20 + 6）；
  2. 每一条的 `(cond, ex)` 与 `ai_scoring.json` 同下标那条
     `targetCriteria.(conditionsFilter, excludeCondition)` **逐条相同**（26/26）
     🧨 两份数据源漂移 ⇒ 红（红时打出第一条差异）；
  3. **消费者真的在按条件筛**：`ASH47` 的 `nontarget` = `Count × multiplyByFriend(3)`、条件 `canAttack(10)`
     ⇒ 把 **2 个友方单位**打成 `Exhausted`，分数必须**少 6**。
     🧨 评分不看 `conditionsFilter` ⇒ 差值 0；🧨 `multiplyByFriend` 没用上 ⇒ 差值 2。
     （⚠️ 用**差值**而不是绝对值：夹具先手不保证是 0，绝对值里含督军那一份，差值把它消掉。）

---

## 四、没接的为什么（逐条写清，⛔ 不是「不做」）

### ① `mult` 的 `targetsAffected ∈ {210, 220}` 分支 —— **本版未实现（池内 0 行）**

原版 `PlayAbility.c:196-231`：这两档不是「数个数」，而是
`local_178 = Σ GetCurrentTraitValueWithModifiers(target, multiplier.traitsFilter)`。
`CountEval.CountTargets` 遇到这两档**返回 `null` + 出声**（⛔ 不拿 count 顶 —— 那会把
「每个 N 层标记」静默变成「几个目标」）。池内 4 条 `mult` 行全是 `10` / `60` ⇒ 今天零影响。

### ② `multi` 的「整条 ability 触发 N 遍」—— **本版未实现（池内 0 行 · 已触发即出声）**

判据齐（`TriggerAbilities.c:246-258`），但它改的是**触发点**而不只是某个 verb；
今天池内 `multi` 行**只有 `UM77` 一条、`targetsAffected = 0`** ⇒ 原版本来就只触发一次。
⚠️ **顺手发现**：池内另有 **8 张卡**的 `multiCountCriteria.targetsAffected != 0`
（`GSC56` 50 · `GOF46` 10 · `ASH52` 161 · `SW58` 10 · `AM18` 10 · `SOR45` 10 · `DA57` 10 · `SW51` 150）——
它们**没有 `unitConditions` 行**（`conditionsFilter = 0`），所以不在这 49 行里，但**同样是 `multi` 这一格**。
见 §五·④。

### ③ `avail` —— **引擎不实现（原版就没有规则消费点）**

见 §三·③。这一条按铁律 11 的**例外①**（原版本身没有该行为）处置，**不是「成本高所以不做」**。

### ④ `choice` 的「候选池按 criteria 生成」这一形状 —— **今天已由正文覆盖，未另建**

我们的 `choosecard` 只有 `specific`（按 id 列表）/ `dead` / `deck` / `hand` / `played` / `enemyhand`
六个候选域；`choicesDefinition` 若要**逐字段**驱动（`friendlyUnits` / `inCemeteryFiltered` / …），
就得把六域重写成「criteria 驱动」。**池内 2 张卡的 criteria 形状恰好等价于已有的 `dead + since_last_turn`**
⇒ 今天**没有行为缺口**（断言钉着）。⚠️ 真出现新形状（例如 `targetsAffected = 50 inHandFiltered`）时，
`choosecard` 那边会走它自己的正文路径；那时**先看 §三·④ 那条断言会不会红**。

---

## 五、顺手发现（⛔ 本批没动手，交调度台分流）

### ① `GSC55` 在 `play all dump` 里变了 —— **不是本批引起**

`before`（02:41，本批**一个字节都没改之前**）↔ `final`（02:56）只有 `GSC55 Eve of the Uprising` 一行不同：
`other 6 → 0` · `changes 10 → 4`。**归因证据**：① 我 02:48 的中途快照（只含 `CardDef`/`CardDatabase`/
`CountEval`/`EffectResolver` 的**非行为**改动）里它**已经变了**；② `Core/RuleCore.cs`（02:45:56）与
`Core/RuleCodes.cs`（02:45:51）在那两个快照之间被**另一个代理**改过；③ `GSC55` 的
`unitConditions` **是空表**，本批三条改动路径（`mult` 行 / `CountTargets` / `AssignBlockIndicesOnly`）
**对它一条都不触发**。⇒ 记给调度台归因（很可能是「弃牌堆充本局阵亡」那条账 `新账-97 ③` 的修复）。

### ② 🔴 **`PinnedPoolMd5` 需要重钉** → `fafc0f562db54f139b29891b119b8c7e`

* 卡表 v21 → v22（§2·1），**只有这一颗该重钉**：
  `PinnedBaselineMd5` = `61ccfb36e70694ae24c5a7e46195e41c` **没动**（基线文件未被改写）；
  `PinnedParseMd5` = `31d298eb7d6394e3861e2c82d1d17095` **没动且当次现算就是它**
  （`check` 的 ⑦ 输出指纹 ✓ = **解析输出逐字节没变**）。
* 🔴 本代理**没有**改任何 `Pinned*Md5`（简报红线）⇒ `ruleprobe check` 现在会打
  「⚠️ [基线指纹] 卡池的 md5 与钉住的**不同** ⇒ 本哨兵此时不判」（**不计 rc**，rc 仍 0）。
* 另需注意：`工具/ruleprobe/基线/out_baseline.txt` 在 `git diff` 里有 **1 行**变化 —— **不是本批**
  （本代理只跑过 `check` / `play … dump`，**没跑 `scan`**；`scan` 才会写基线）。

### ③ `UM77`：「died since last turn」被正文计成「本回合阵亡」——**潜在偏差，本批未改**

* 正文 `Repeat for each friendly troop that died since your last turn` 被解析成
  `repeat` op 的 `CountRef = "unit"` / `CountScope = "died"`，而 `CountScope == "died"` 的实现是
  **`ctx.DiedThisTurn`**（`EffectResolver.cs:9168`，回合开始清零，`RuleCore.cs:1518`）。
* 原版这一格是 `multiplier = inCemeteryFiltered + cond 222 diedSinceLastTurn`
  （`ConditionEval` 的 222 = **本回合或上一回合**阵亡）。
* 本批的 `mult` 消费者**已经把结构化值接上**，但 `UM77` 的 `mult` 行是 `ab = 1`、
  而这张卡的 `desc` 只有**一个能力块** ⇒ `blocks.Count(1) <= maxAb(1)` 过不了 G2 ⇒
  **不标下标** ⇒ 结构化乘数取不到 ⇒ **仍走正文的「本回合」口径**。
  ⇒ **这是一条真实的小行为缺口**（对手回合末被打死、我方回合打出 `Chairon` 时少算一次），
  **本批没有改**（改它要么动 G2 闸、要么给「卡上唯一一条 `mult` 行」加兜底猜测 —— 两条都不能在本批
  无判据地做）。判据已备，建议单独立账。

### ④ `multiCountCriteria.targetsAffected != 0` 的 **8 张卡**（见 §四·②末）

`AM18`(10) · `ASH52`(161) · `DA57`(10) · `GOF46`(10) · `GSC56`(50) · `SOR45`(10) · `SW51`(150) · `SW58`(10)
—— 它们**不在** `unitConditions` 的 49 行里（`conditionsFilter = 0` ⇒ 生成器不落行），
但原版语义是「整条 ability 触发 N 遍」。我们的正文对其中大多数有 `for each` 对应写法，
**是否逐张等价本批没核**（尤其 `SW51` 的 `previousAbilityFiltered(150)` 与
`ASH52` 的 `currentSpiritStone(161)` 两条，`CountEval` 都没实现这两档）。
（另 6 张卡带 `multiCountCriteria` 但 `targetsAffected = 0`：`AM44` · `ASH52` ab0 · `GOF104`×3 ·
`GOF90` · `GSC75` · `UM77` —— 按原版闸门**本来就只触发一次**，无缺口。）
⇒ 建议单独立账（「`multi` 全量：8 张卡逐张核」，判据已在本报告 §一/§三·②）。

### ⑤ `TargetsAffected = 150 (previousAbilityFiltered)` —— `CountEval` 未实现

`multi`（`SW51`）与 `cardAbilities[].multiCountCriteria` 之外，`150` 也在 `abilityTargets` 里出现过
54 次；`CountSet` 对它返回 `null`（如实）。它不是本批新增的缺口，记一笔。

### ⑥ `ruleprobe` 的「只编 `Core/*.cs`」这条限制有个坑（本批踩到并已修）

`ruleprobe.csproj` **只显式编 `RuleEngine/Core/*.cs`**，`Data/CardDatabase.cs` **不在里面**
（探针用自己的 `LoadPool()` 读 JSON）。⇒ **`CardDef` 上新增的列如果只在 `CardDatabase` 里映射，
探针侧恒为 `null`** —— 本批第一版就是这样：引擎侧 `MultiplierSet` 有值、探针里恒 `null`，
`play` 读数**看不出这条消费端在不在干活**（差点把「接了」误判成「没接」）。
已在 `工具/ruleprobe/Program.cs` 补 `ParseRoleSet`（与 `CardDatabase.RoleSet` 同口径）并加读数行
`unitConditions[roleSet]: … = 10（对账期望 mult 4 · multi 1 · avail 3 · choice 2 = 10）`。
⚠️ 同族提醒：**以后往 `CardDef` 加「有消费端的列」，`tools/ruleprobe/Program.cs` 的 `LoadPool` 必须同步**，
否则探针与引擎口径分叉（`A1911`/`A1959` 那一族）。

---

## 六、改动清单（逐文件）

| 文件 | 行尾 | 改了什么 |
| --- | --- | --- |
| `工具/gen_cards_engine.py` | **CRLF** | `count_set_of` 上提为 `criteria_set_of`（36 格唯一一份；`count_set_of` 降为薄壳）+ `condition_rows_of` 给四个 role 落 `multiplierSet`/`multiSet`/`availSet`/`choiceSet` + `report["role_set"]` 计数 + 摘要行 + `version 21 → 22` 与 v22 注释 |
| `Assets/RuleEngine/Resources/cards_engine.json` | LF | **重生成**（v22 · 7 张卡的 `unitConditions` 各多一个键） |
| `Core/CardDef.cs` | LF | `ConditionSpec` 新增 `MultiplierSet` / `MultiSet` / `AvailSet` / `ChoiceSet`（各带判据 doc） |
| `Data/CardDatabase.cs` | LF | `UnitConditionDto` 四个字段 + `RoleSet`（错位出声、缺=null）+ `MapCountSetDto` 的 `noPlayerTurn` 分支 + `version < 22` 守卫 + `multi.targetsAffected != 0` 的 `LogWarning` |
| `Core/CountEval.cs` | LF | 新 `CountTargets`（`any(0)` ⇒ 1 · 210/220 ⇒ null+gap · 其余走既有 `CountSet`） |
| `Core/EffectResolver.cs` | **CRLF** | `ResolveOps` 两处乘数接管（`PerCount` 支 / `repeats` 支）+ 新 `TryStructuredMultiplier` |
| `Core/EffectText.cs` | LF | `AssignBlockIndices`（从原 `full` 分支抽出）+ `AssignBlockIndicesOnly`（无 `target` 行时的**只标块下标**路径）+ `rows.Count == 0` 那一段 |
| `Editor/RuleEngineTest_UnitRoles.cs`（**新**）+ `.meta` | LF | 5 组断言（§三） |
| `Editor/RuleEngineTest.cs` | **CRLF** | `Run()` 里 **1 行** `Step(TestN2UnitConditionRoles);` |
| `Editor/RuleEngineTest_Diag.cs` | LF | `gen_diag_runner.py` 重生成（309 条，逐字节自洽） |
| `工具/ruleprobe/Program.cs` | LF | `LoadPool` 里 `ParseRoleSet` 四格 + `nUcRoleSetRows` 读数 + `version < 22` 守卫 + 摘要行 |

⛔ **一个字节都没动**：`CardPresentation/**`、`WarpforgeVFX/**`、`CLAUDE.md`、`项目任务.md`、
任何 `任务总表.md`、`资料/历史/**`、git、任何 `Pinned*Md5`、`工具/` 下除
`gen_cards_engine.py` / `ruleprobe/**` 之外的一切（含 `gen_ai_scoring.py` / `gen_diag_runner.py` ——
后者只**执行**、未修改）。
