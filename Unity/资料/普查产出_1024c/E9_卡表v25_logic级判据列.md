# E9 · 卡表 v25 —— logic / op 级 `targetCriteria`（两列）

> 执行代理 `E9`（第七十三会话 · 2026-10-24）。白名单：`工具/gen_cards_engine.py` ·
> `Assets/RuleEngine/Resources/cards_engine.json` · `Assets/RuleEngine/Data/CardDatabase.cs` ·
> 新建 `Assets/RuleEngine/Editor/RuleEngineTest_EngineD4.cs` · 本报告。
> **没有**跑 Unity / ruleprobe，**没有**动 git / `CLAUDE.md` / `项目任务.md` / `Core/**`。

---

## 一、判据出处（权威顺序）

| 层 | 判据 |
|---|---|
| ① 全量反编译 | `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c:262` —— `GetTargets(param_2, *(param_1 + 0x28), param_3, 1, 0)`，`+0x28` = **这一行**的 `targetCriteria`（不是 ability 级那份）<br>`AbilityLogic__GetTargets.c` —— `+0x10`（`targetsAffected`）先选**候选池**（`self(20)` / `actingCard(80)` / `friendlyHero(120)` / `any(0)` / `allFiltered(10)` …）<br>`FilterMethods__CheckIfMeetsCriteria.c` —— 其余 35 格逐字段过滤 |
| ② 解包字段桩 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityLogic.cs:16-17`（`public TargetCriteria targetCriteria;`）<br>`TargetCriteria.cs` —— 38 个字段；`d:/2/Warpforge_code/Scripts/Assembly-CSharp/TargetsAffected.cs` —— 38 个枚举成员 |
| ③ 本批的总表 | `d:/4/Unity/资料/普查产出_1024c/A2_引擎战斗判据总表.md` **Q7**（结论 3/4：ability 级不缺、缺的是 op/logic 级 36 格；建议形状；复用 `criteria_set_of()`） |

`abilityContext` 那一问（Q7 结论 1/2）**本批不碰** —— 它 v14 起就有（现读 157 卡 / 170 行）。
⚠️ `资料/任务线/引擎线/任务总表.md:32` 与 `资料/普查产出_1024b/C3_剩余判据_引擎战斗.md:241` 的
「卡表缺 abilityContext」**是错的**，那两处**不在本代理白名单** ⇒ 由主对话按铁律 5 就地订正。

---

## 二、列形状（与 `A2` Q7 的**建议**形状有 2 处有意偏差）

```json
"activeAbilityTargets": [
  { "ab": -1,                       // -1 activeAbility / -2 alternativeAbility
    "lg": 0,                        // 该能力对象里 activeAbilityLogic 的下标
    "criteria": { "playerTurn": -1, "targetsAffected": 30, "chosenArmy": 0, …,
                  "filterSpecificCards": [], "excludeEnchantments": [] } } ],

"abilityTargetCriteria": [
  { "ab": 0,                        // cardAbilities 下标
    "trigger": 0,                   // 只作可读性
    "lg": 0,                        // 该 ability 里 abilityLogic 的下标
    "criteria": { …同上 37 键… } } ]
```

**偏差 ①：行里多了 `ab`（`cardAbilities` 下标）** —— `A2` Q7 建议 `{trigger, lg, …}`。
实测 **64 张池内卡**（真值 1351 张里 89 张）的两条 ability **共用同一个 `trigger`**，
其中至少 `Empyric Ambush`（`dc7e575c…`）两条同 trigger 的 criteria **值不同**（`targetsAffected` 30 vs 10、
`keywordFilter` 0 vs 5）⇒ `(trigger, lg)` **不是主键**，只按它对齐会**静默串行**。
`ab` 的语义与 `unitConditions` 逐字同词（主动那一列仍用 -1/-2）。

**偏差 ②：36 格装进子对象 `criteria`，不平铺在行上。**
平铺就必须在 C# 里**再抄一份** 36 格字段表；而 `CardDatabase.CountSetDto` 已经是这份表（`unitConditions[].countSet`
共用）⇒ 走子对象，C# 侧 36 格字段表**全仓仍只有一份**（铁律「两处写同一条规则 = 迟早不一致」）。
生成器侧仍是**同一份**字段表（`COUNT_SET_*` 元组 + `criteria_set_of()`），⛔ 没有第二份。

**36 格清单**（= `TargetCriteria` 38 字段 − `conditionsFilter` − `excludeCondition`；
后两格由 v15 的 `unitConditions` 落成那一行的 `cond` / `ex`）：

- **16 个整数**：`targetsAffected` · `chosenArmy` · `armyFilter` · `keywordFilter` · `traitsFilter` ·
  `maxTargets` · `targetSelectMethod` · `attackFilter` · `attackFilterOption` · `currentHealthFilter` ·
  `currentHealthFilterOption` · `manaCostFilter` · `manaCostFilterOption` · `rarityFilter` ·
  `rarityFilterOption` · `spellType`
- **18 个布尔**（落 0/1）：`friendlyUnits` · `enemyUnits` · `includeHero` · `includeMinions` ·
  `includeSpells` · `includeWhispers` · `includeWeapons` · `includeRemnants` · `includeHeroParts` ·
  `includeSelf` · `excludeNotSelf` · `excludeSimilarCards` · `excludeTargetted` ·
  `excludeFriendlyHero` · `excludeEnemyHero` · `excludeRace` · `excludeTrait` · `selectFromChooseResolution`
- **2 个引用列表**（`PPtr → uniqueId`）：`filterSpecificCards` · `excludeEnchantments`
- **加上 `playerTurn`** = 行内 **37 键**。`playerTurn` 在 `AbilityLogic.targetCriteria` 上**结构上没有**
  （`TargetCriteria.cs` 无此字段，那一格属于 `BoardCriteria`）⇒ `criteria_set_of(..., has_player_turn=False)`
  ⇒ 落 **-1** 且**不进** `set_bad`。⚠️ 不要把它读成 Both(0)。

---

## 三、生成器改动（`d:/4/Unity/工具/gen_cards_engine.py`）

| 行号 | 改动 |
|---|---|
| `1841-1849` | 新增两个 report 字典：`ACTIVE_ABILITY_TARGETS_REPORT` / `ABILITY_TARGET_CRITERIA_REPORT`（含 `set_bad` / `set_unresolved`，供 `criteria_set_of` 出声） |
| `1851-1894` | 新增 `active_ability_targets_of()` —— 按 `(-1 active, -2 alternative)`、再按行序；真值里没这张卡返回 `None`（不落列） |
| `1896-1940` | 新增 `ability_target_criteria_of()` —— 按 `cardAbilities` / 行序；`ab`/`trigger`/`lg` + `criteria` |
| `2208-2221` | 新增 `criteria_set_unknown()` —— 全 -1 / `null` 的一条 criteria（键序与 `criteria_set_of` 逐字相同，复用同一批 `COUNT_SET_*` 元组） |
| `2224-2262` | `criteria_set_of()` 加一条**坏行闸**：`cr` 不是 dict ⇒ 出声一次 `<criteria>` + 返回 `criteria_set_unknown()`（⛔ 不抛异常、⛔ 不报 36 条噪声）。今天 0 命中（现有四个调用点都先校验过 dict） |
| `4040-4050` | 逐卡装配处新增两列（`if _aat is not None` / `if _atc is not None` —— **`[]` 也落**，只有真值缺卡才不落） |
| `4520` | `"version": 24 → 25` |
| `4681-4702` | 版本说明块 v25（含「为什么带 `ab`」「为什么子对象」两条理由 + 三处判据） |
| `5745-5778` | `main()` 摘要：`目标判据①/②` 两段（张数 / 行数 / 有信息行 / 引用行 / `targetsAffected` 分布 / 坏值逐条） |
| `5780-5798` | **v25 自检**：两列 `criteria` 必须恰好 37 键、键序固定（`playerTurn` + 16 + 18 + 2）；不通过 ⇒ `_bad = True` ⇒ **一个字节都不写**（`--check` 报过期、写盘被拒） |

---

## 四、生成结果（现读）

| 项 | 值 |
|---|---|
| 卡表 | **version 25** · **count 1127** |
| `activeAbilityTargets` | 落值 **1127/1127** 张（每张卡都有；没行 = `[]`）· 有行 **132** 张 · **193 行**（active 192 / alternative 1） |
| `abilityTargetCriteria` | 落值 **1127/1127** 张 · 有行 **1008** 张 · **1616 行** / 1270 条 ability |
| criteria 行合计 | **1809 行 × 37 键**（自检 ✅） |
| `activeAbilityTargets` 的 `targetsAffected` 分布 | 30:69 · 20:51 · 10:30 · **0:17** · 90:7 · 65:6 · 150:3 · 110:3 · 50:2 · 60:2 · 51:1 · 180:1 · 120:1 |
| `abilityTargetCriteria` 的 `targetsAffected` 分布 | 10:399 · 30:322 · **0:286** · 20:236 · 120:67 · 150:54 · 65:53 · 180:43 · 90:38 · 50:28 · 80:23 · 100:17 · 110:14 · 60:12 · 140:7 · 130:5 · 52:3 · 113:3 · 240:2 · 200:1 · 201:1 · 106:1 · 51:1 |
| 引用列表非空 | active 列 **0 行**；普通列 **13 行**（全部解得出，`set_unresolved = 0`） |
| 坏值 / 解不出 | **0 / 0**（`bad` / `set_bad` / `set_unresolved` 三张表都空） |
| 表大小 | **1,463,954 B → 2,929,811 B**（+100.1%） |
| 幂等 | `--write` 两次 **md5 相同** = `860044b773370da0b70a0134d3a7463d`；`--check` 连跑两次都 `一致 ✅` / RC=0 |

**跨列对账（生成后逐卡现算，0 处不符）**：
- 每张卡 `len(abilityTargetCriteria)` == `Σ len(abilityTargets[i].ta)`，且 `(ab,lg)` 位置集合**逐格相同**；
- 每张卡 `len(activeAbilityTargets)` == `Σ len(activeAbilityLogic[i].ta)`，位置集合同理；
- 新列的 `trigger` 与 `abilityTargets[ab].trigger` 逐格相同。

---

## 五、DTO 与版本守卫（`d:/4/Unity/MyGame/Assets/RuleEngine/Data/CardDatabase.cs`）

| 行号 | 内容 |
|---|---|
| `398-432` | `CardDto` 新增两列 `activeAbilityTargets` / `abilityTargetCriteria`（`TargetCriteriaRowDto[]`），doc 写明形状、`ab` 为什么必须有、本批**无 CardDef 消费者** |
| `608-628` | 新增 `TargetCriteriaRowDto`（**两列共用**）：`ab`（缺键哨兵 `MissingIndex` = -99，因为 -1/-2 合法）/ `trigger`（-1 = 没有这一格）/ `lg`（-1 = 缺键）/ `criteria`（`CountSetDto`，⛔ 不再抄一份 36 格） |
| `1175-1183` | 版本守卫 `dto.version < 25` ⇒ `LogWarning`：两列会是 **`null` = 未知**，⛔ 不许读成「原版说全是默认、不过滤」；文案里点名 36 格、`AbilityLogic.cs:16-17`、`PlayAbility.c:262` |
| `1412` | `Parse()` 收尾（`ReportArmourDisagreements` 之后）调 `ReportTargetCriteriaColumns(dto)`，**只在 `version >= 25` 时** |
| `2338-2395` | `ReportTargetCriteriaColumns()`：一次装载打**一行** `Debug.Log`（两列各多少张 / 多少行）+ 结构校验（`ab`/`lg` 域、`criteria` 非 null、`targetsAffected`/`playerTurn` 不是 -99）。有坏点 ⇒ `LogError` 逐类计数 + 首个实例。**为什么要它**：这两列本批没有 `CardDef` 消费者，不打印就完全看不见 `JsonUtility` 有没有把子对象绑上（静默绑不上 = 两列白落） |

---

## 六、断言（`d:/4/Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineD4.cs`）

入口 `TestEngineD4()`（`public static partial class RuleEngineTest`）。**读原始 JSON**
（`Resources.Load<TextAsset>` + `JsonUtility.FromJson<CardDatabase.FileDto>`）—— 因为本批这两列**没有
`CardDef` 消费者**，从 `CardDatabase.Load()` 看不出来。

| 节 | 内容 | 期望值来源 | 灭自证 / 🧨 |
|---|---|---|---|
| `D4Shape` A1 | `version == 25` | 生成器产物 | 改回 24 ⇒ 红（新列会被守卫读成 null） |
| A2 | 两列 **`null` 张数 = 0** | 生成器（每张卡都落） | 改成「只落有行的卡」⇒ 红（缺列与没有行就分不开） |
| A3 | active 列 **193 行**；`ab ∈ {-1,-2}`；`lg ≥ 0`；`criteria` 非 null；`playerTurn == -1`；绑定探针（整数/布尔/引用各一格 ≠ -99/null） | 生成器现读 | 只落 `ta != 0` 的行 / 漏 alternative / 改传 `has_player_turn=True` / DTO 字段名不一致 ⇒ 红 |
| A4 | 普通列 **1616 行**；`ab ≥ 0`；`lg ≥ 0`；`trigger ≥ 0`；`playerTurn == -1`；绑定探针 | 生成器现读 | 同上 |
| `D4Counts` B1/B2 | 有行的卡 **132 / 1008** | 生成器现读 | 只给 `hasActiveAbility=1` 的 87 张落 ⇒ 红 |
| B3/B4 | `targetsAffected == 0`（`any`）的行 **17 / 286** —— **照落** | `GetTargets.c:190-192`（any ⇒ 其余 35 格失效）+ 生成器现读 | 按 `criteria_carries_targets` 那套把 0 行丢掉 ⇒ 红 |
| B5a-B5j | active 列逐格计数：`keywordFilter` 4 · `traitsFilter` 4 · `maxTargets>0` 21 · `friendlyUnits==1` 31 · `includeHero==1` 97 · `includeMinions==1` 119 · `includeRemnants==1` 13 · `selectFromChooseResolution==1` 8 · `rarityFilter` 1 · **引用列表 0** | 生成器现读（逐格） | 任一格没落 / 落成常量 / 把别处的引用串进来 ⇒ 红 |
| B6a-B6o | 普通列逐格计数：`keywordFilter` 117 · `traitsFilter` 60 · `maxTargets>0` 391 · `friendlyUnits` 556 · `includeHero` 336 · `includeMinions` 729 · `excludeTrait` 28 · `chosenArmy` 51 · `armyFilter` 50 · `includeRemnants` 163 · `selectFromChooseResolution` 79 · `spellType` 10 · `manaCostFilter` 21 · `rarityFilter` 2 · **引用行 13** | 生成器现读（逐格） | 同上 |
| `D4Alignment` C0-C3 | 逐卡：行数与位置集合 = 旧列 `ta` 的长度和 / 位置集合；`trigger` 与旧列同位置逐格一致；覆盖 1127 张 | 旧列（**独立于新列**的数据源） | `ab` 用错 / 只落 active 支 / trigger 抄错 ⇒ 红 |
| `D4ConcreteCards` D1 | `ASH1 Anvirr Keltoc`：`ab=-1, lg=0, targetsAffected=30, includeMinions=0` | 原版真值快照（`ASH1` 的 `activeAbilityLogic[0].targetCriteria`） | 整块偏移一行 ⇒ 红 |
| D2 | `ASH31 Winged Autarch`：`trigger=240, ta=10, traitsFilter=390, friendlyUnits/includeHero/includeMinions=1` | 真值快照 | 该格没落（-99/0）⇒ 红 |
| D3 | `EC33 Alluress` 的 `(ab=1, lg=0)`：`filterSpecificCards == ["EC32"]` | 真值（`RawCardScript` PPtr → `uniqueId`） | 拿 `m_PathID` 当 id / 换错解引用表 ⇒ 红 |
| D4 | `ASH46 Aerial Dominion`：同一 ability 的 `lg=0`（`keywordFilter=0, enemyUnits=1`）与 `lg=1`（`keywordFilter=10, maxTargets=0`）**不同** | 真值快照 | 按 ability 只落第一行 ⇒ 红 |
| **D5（灭自证核心）** | `Empyric Ambush`（`dc7e575c…`）**两条 ability 共用 trigger 0**，`(ab=0,lg=0)` 与 `(ab=1,lg=0)` 两行 `targetsAffected` **不同**（30 vs 10）⇒ **`(trigger, lg)` 不能当主键** | 真值快照 | 删掉行里的 `ab`，或消费端改用 `(trigger,lg)` ⇒ 这一对就分不开（这条断言就是为它写的） |
| `D4VersionGuard` E1-E3 | fake v24 表：**恰好 1 条**含 `activeAbilityTargets` 的警告，且文案含 `null` 与 `36`；fake v25 表：**0 条** | 守卫实现（行为契约） | 删守卫 ⇒ 0；写成 `<= 25` ⇒ 当前表天天刷警告 |

**现状**：`typecheck.sh`（`TMPDIR=/tmp/wf_e9`）两程序集 **0 错误**。
⚠️ **本文件还没接线**：`RuleEngineTest.cs` 的 `Run()` 要加一行 `Step(TestEngineD4);`，
`RuleEngineTest_Diag.cs` 要按工具重生成 —— 两份都不在本代理白名单，**请主对话补**。
补之前覆盖闸会红并点名 `TestEngineD4`（设计如此）。

---

## 七、消费端下一步（**另批做**，本批零行为变化）

1. **数据形状不用再改** —— 消费端直接读两列：
   - 主动能力：`CardDto.activeAbilityTargets` → `(ab, lg)` 对齐 `CardDef.ActiveAbilityLogicSpec` 的行；
   - 普通 ability：`CardDto.abilityTargetCriteria` → `(ab, lg)` 对齐 `CardDef.AbilityTargetSpec`（`EffectOp` 那批）。
   ⛔ **别用 `(trigger, lg)` 当键**（64 张池内卡会撞，见 §二/§六 D5）。
2. **DTO → `CardDef` 的映射**：`CardDef.CountSetSpec` 就是这 36 格＋`PlayerTurn` 的现成类型
   ⇒ **复用 `CardDatabase.MapCountSetDto(c, d, where, noPlayerTurn: true)`**
   （`CardDatabase.cs:1953`，全仓 `CountSetDto → CountSetSpec` 的**唯一**实现）—— ⛔ 别新写映射。
   落点：`CardDef.AbilityTargetSpec` 加一格（如 `TargetCriteria`）+ `CardDef.ApplyActiveAbilityLogic`
   给 `ActiveAbilityLogicSpec` 加一格；`CardDatabase` 在 `MapAbilityTargets` / `MapActiveAbilityLogic`
   旁边把 `criteria` 传进去（`CardDatabase.cs:1707` / `:1772` 那两处）。
3. **挂到 op 上**：`EffectText.AssignAbilityTargets`（`Core/EffectText.cs:1993` / `:2026` 那两道 G1 闸门）
   与 `CardDef.ApplyActiveAbilityLogic`（`Core/CardDef.cs:2520` / `:2588` 附近）**同一套闸门**
   再挂一格 `EffectOp.SubjectTargetCriteria`（`ConditionUnknown` / `null` = 读不到）。
4. **求值器**：新建一个与 `AbilityLogic__GetTargets.c` + `FilterMethods__CheckIfMeetsCriteria.c`
   逐字段对应的 `TargetCriteriaEval`（建议放 `Core/`，与 `ConditionEval` 同族）：
   先按 `targetsAffected` 选候选池（枚举 38 档，见 `TargetsAffected.cs`），再跑 35 格过滤；
   `targetsAffected == 0 (any)` ⇒ **忽略其余 35 格**（`GetTargets.c:190-192`）。
5. **接进结算**：`EffectResolver.ResolveTargets` 的 `else` 分支（`Core/EffectResolver.cs:1448-1480`）
   现在只认 `ta == 120`、其余退回「己方全体」并出声 —— 换掉它的正是这一列：
   `ta ∈ {0, 10}`（以及其它「需要过滤」的档）走新求值器；`SubjectTargetCriteria == null`（旧表/没挂上）
   ⇒ **保持现在的近似 + 出声**（⛔ 不许静默当默认 criteria）。
6. **`playerTurn`**：这一列上恒 `-1`（= 不适用）⇒ 交给 `MapCountSetDto(..., noPlayerTurn: true)` 之后是
   `CardDef.ConditionUnknown`，⛔ 不要当「Both(0)」用。

---

## 八、没查清（如实记）

1. **`conditionsFilter` / `excludeCondition` 与 `unitConditions` 的合成顺序没定**
   —— 同一个 `targetCriteria` 被拆成两处（36 格在这一列、`cond`/`ex` 在 v15 的 `unitConditions` 的
   `role: "target"` 行）。消费端要先读哪一处、`unitConditions` 的行是否**一定**存在（只有 `cond != 0` 才落）
   都没定。**判据方向**：`FilterMethods__MeetsConditionCriteria.c` 末段 `exclude ? !match : match`，
   但「两列怎么合」属于设计决定，本批不裁。
2. **`targetSelectMethod` / `maxTargets` 在我们 UI/选择链上的落点没查** —— 数据已落，消费端要不要
   真的弹「选 N 个」，取决于原版 `BattleCardUI` 那一族，本批没读。
   （`ta` 的 38 个枚举值**都核过**属于 `TargetsAffected`；上面两个分布里的 51/52/106/113/200/201/240
   都是合法成员，不是坏值。）
3. **`furtherMultiplier`（`fm`）与这一列的组合语义**：`PlayAbility.c:181-236` 里 `multiplier` 与
   `targetCriteria` 是**两份** criteria（前者数个数），本批只落后者；两者的乘/取先后没在本批复核。

---

## 九、顺手发现（只报不改）

1. 🟡 **卡表体积翻倍**：1,463,954 B → **2,929,811 B**（+1.47 MB / +100.1%）。
   `CardDatabase` 的注释写着 `FromJson` ＋ 1127 张卡**≈2.4 s**（一次性，之后走 `_cache`）——
   字节数翻倍后**装载耗时会上去**，建议下一批在 Unity 里实测一次 `CardDatabase.Load()` 的首载耗时
   并记进文档（本次**没有**独立基准）。⛔ 不建议为了瘦身改成「省略默认值」：那会破坏
   「缺省 / 值与默认」必须分得开的 `-1` 契约（v16 `countSet` 已经定过这条口径）。
2. 🟡 **`A2` Q7 的建议形状有 2 处需要改**（本批已按实测改了）：加 `ab`（64 张池内卡 `(trigger, lg)` 撞车）、
   36 格装子对象（避免 C# 抄第二份字段表）。建议主对话把 Q7 的「建议形状」那一段就地订正（铁律 5）。
3. 🟡 **文档订正（本代理无权改）**：`资料/任务线/引擎线/任务总表.md:32`（⑥）与
   `资料/普查产出_1024b/C3_剩余判据_引擎战斗.md:241` 仍写着「卡表缺 `abilityContext`」——
   实际 v14 起就有（现读 157 卡 / 170 行）。同一句在 `A2` 的「顺手发现 1」里也提过。
4. 🟢 **真值快照 vs 现池的口径差**：真值 1351 张里 `abilityLogic` 1873 行 / active 205 行；
   现池 1127 张对应 **1616 行 / 193 行**——差的那部分是池外卡（教程副本、附魔、进攻/防御卡）。
   对账时**别拿 1873 / 205 当池内基线**。
5. 🟢 `targetsAffected == 0 (any)` 的行不少（active 17 / 普通 286）：消费端读它们时**必须**先判 `0`
   再决定要不要看其余 35 格（`GetTargets.c:190-192`），否则会把「any」当成「一条过滤都不满足」。
