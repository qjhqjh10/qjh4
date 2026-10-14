# abilityLogic 一族 枚举词典 —— 报告（2026-10-14）

## 0. 产物与复算

- 词典：`d:/4/Unity/资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv`（838 行 + 表头）
- 提取脚本（可复算）：`python -I -X utf8 d:/4/Unity/工具/gen_ability_enum_dict.py`
- 判据（全只读）：真值 `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`；枚举/字段定义 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs`
- 列：`结构体` = **`类型名@路径`**（同一结构体在不同位置分开列，例 `TargetCriteria@multiplier`）· `出现次数` = 该结构体**实例**里该字段等于该值的次数（不是卡数）

## 1. 结论

1. 覆盖 = **真值里实际出现过的每一个取值**（先扫值、再翻名），不是「枚举定义里有什么」。核心（路径含 `abilityLogic`）**288 行 / 66 个 (路径,字段) 组合 / 19 个枚举类型**；连它上下紧邻的容器（`CardAbility` / `BoardCriteria` / `ActiveAbility` / 卡根）一起共 **838 行 / 354 组合 / 26 个枚举类型**。
2. `AbilityLogic` 实例共 **1782** 个 = `cardAbilities[].abilityLogic[]` **1622** + `activeAbility.activeAbilityLogic[]` **160**（`alternativeAbility` 无）。简报给的「1622 条」只数了第一处 —— 对账时别漏那 160。
3. **翻不出来的只有 2 个取值**（都不是漏翻，是原版数据里的遗留值）：`DamageCriteria.valueOptions = 8`（×34）· `CardEffect.healthValueOptions = 17`（×8）。三份独立枚举源（stub 镜像 / `Warpforge_tools/tmp/full_decomp` / `tools/il2cpp_out/dump.cs`）逐字一致，都**没有** 8 与 17；反编译 switch 里也没有这两支（见 §4）。
4. ⚠️ **简报里那句「`30` 很可能是『敌方单位』那一档」不成立** —— `TargetsAffected.target = 30`（`TargetsAffected.cs:6`），语义是「**本次能力的那个目标**」。**「敌方」不在这一格里**：它由**卡级** `cardTargetCriteria` 的 bool（`enemyUnits` / `includeHero` / `includeMinions`）决定（ASH15 实测，见 §5）。
5. 结构体真名是 **`TargetCriteria`**（不是 `CardTargetCriteria` / `AbilityTargetCriteria`）；`multiplier` 与 `targetCriteria` **是同一个类型**，只是位置不同。

## 2. 结构体叫什么、在哪

`AbilityLogic` = **`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityLogic.cs`**（[Serializable] class，:7）。它的 8 个子结构体逐个找到：

| 字段(AbilityLogic.cs 行) | 类型 | 类型定义在哪 |
|---|---|---|
| `chosenAbility` :9 | `AbilityEffect` | `AbilityEffect.cs:1` |
| `playersAffected` :14 | `PlayersAffected` | `PlayersAffected.cs:1` |
| `targetCriteria` :17 / `multiplier` :20 | **`TargetCriteria`** | `TargetCriteria.cs:6` |
| `damageCriteria` :25 | `DamageCriteria` | `DamageCriteria.cs:3` |
| `healCriteria` :28 | `HealCriteria` | `HealCriteria.cs:3` |
| `buffEffect` :31 | `CardEffect` | `CardEffect.cs:6` |
| `summonCriteria` :34 | `SummonCriteria` | `SummonCriteria.cs:5` |
| `handBuff` :37 | `HandEffect` | `HandEffect.cs:4` |
| `manaBuff` :40 | `ManaBuff` | `ManaBuff.cs:4` |

**排除的候选**（都查过、都不是）：全库 `grep "enum ChooseValueOptions"` 只此一处；`CardTargetCriteria` / `AbilityTargetCriteria` 这两个类名在全库**不存在**；`CardScript.cs` 里只有 `activeAbilityLoaded` 一个属性，**卡 SO 的数据类真身是 `RawCardScript`**（`RawCardScript : PlayerItem : ObtainableItem`，`cardAbilities` 在 `RawCardScript.cs:3`）。

## 3. 逐字段表（字段 → 枚举类型 → 真值里出现过的**取值数** → 字段声明出处）

**AbilityLogic**：`chosenAbility`→**AbilityEffect**(53 值, `AbilityLogic.cs:9`) · `playersAffected`→**PlayersAffected**(3 值, `:14`) · 非枚举(int)：`furtherMultiplier`(`:22`) · `chanceOfHappening`(`:44`)

**CardAbility**（= abilityLogic 的容器，触发/上下文/优先级）：`trigger`→**AbilityTrigger**(57 值, `CardAbility.cs:8`) · `abilityContext`→**AbilityContext**(5 值, `:10`) · `priority`→**ActionPriority**(4 值, `:12`) · 非枚举(int)：`triggerValue`(`:16`)

**BoardCriteria**：`playerTurn`→**PlayersAffected**(3 值, `BoardCriteria.cs:6`) · `countOptions`→**CountOptions**(1 值, `:10`) · `triggerIf`→**StatFilterOptions**(5 值, `:12`) · 非枚举(int)：`referenceValue`(`:14`)

**HandEffect**：`playersAffected`→**PlayersAffected**(3 值, `HandEffect.cs:8`) · 非枚举(int)：`numberOfUses`(`:14`)

**TargetCriteria**
| 字段 | 枚举类型 | 取值数 | 出处 |
|---|---|---|---|
| targetsAffected | TargetsAffected | 30 | TargetCriteria.cs:9 |
| chosenArmy | CardArmy | 11 | TargetCriteria.cs:42 |
| armyFilter | ArmyFilterOptions | 2 | TargetCriteria.cs:44 |
| keywordFilter | Keyword | 7 | TargetCriteria.cs:46 |
| traitsFilter | DefinedTrait | 18 | TargetCriteria.cs:51 |
| conditionsFilter | UnitConditions | 15 | TargetCriteria.cs:53 |
| targetSelectMethod | TargetSelectMethod | 5 | TargetCriteria.cs:64 |
| attackFilterOption | StatFilterOptions | 2 | TargetCriteria.cs:69 |
| currentHealthFilterOption | StatFilterOptions | 3 | TargetCriteria.cs:74 |
| manaCostFilterOption | StatFilterOptions | 4 | TargetCriteria.cs:79 |
| rarityFilterOption | StatFilterOptions | 2 | TargetCriteria.cs:83 |
| spellType | SpellType | 7 | TargetCriteria.cs:85 |
· 非枚举(int)：`maxTargets`(61) · `attackFilter`(67) · `currentHealthFilter`(72) · `manaCostFilter`(77) · `rarityFilter`(81) —— ⚠️ 这五个**是数值不是枚举**，别按名字去翻。

**DamageCriteria** / **HealCriteria**
| 字段 | 枚举类型 | 取值数 | 出处 |
|---|---|---|---|
| DamageCriteria.valueOptions | ChooseValueOptions | 8（**含 1 个未找到**） | DamageCriteria.cs:6 |
| DamageCriteria.damageType | DamageType | 3 | DamageCriteria.cs:12 |
| HealCriteria.valueOptions | ChooseValueOptions | 2 | HealCriteria.cs:6 |
· 非枚举(int)：`minDamage`(8) · `maxDamage`(10) · `minValue`(8) · `maxValue`(10)

**CardEffect**
| 字段 | 枚举类型 | 取值数 | 出处 |
|---|---|---|---|
| buffType | BuffType | 15 | CardEffect.cs:38 |
| trait | DefinedTrait | 25 | CardEffect.cs:40 |
| newRace | Keyword | 1 | CardEffect.cs:44 |
| meleeAttackValueOptions | ChooseValueOptions | 6 | CardEffect.cs:50 |
| rangeAttackValueOptions | ChooseValueOptions | 3 | CardEffect.cs:54 |
| armourValueOptions | ChooseValueOptions | 2 | CardEffect.cs:58 |
| healthValueOptions | ChooseValueOptions | 4（**含 1 个未找到**） | CardEffect.cs:62 |
| costValueOptions | ChooseValueOptions | 2 | CardEffect.cs:66 |
| markOfChaosType | MarkOfChaosType | 6 | CardEffect.cs:76 |
| daemonHostType | DaemonhostType | 1 | CardEffect.cs:78 |
| mutationType | MutationType | 1 | CardEffect.cs:80 |
· 非枚举(int)：`traitValue`(42) · `meleeAttackChange`(47) · `rangeAttackChange`(52) · `armourChange`(56) · `healthChange`(60) · `costChange`(64) · `randomValueChange`(68) · `enchantmentCardId`(100)

**SummonCriteria**
| 字段 | 枚举类型 | 取值数 | 出处 |
|---|---|---|---|
| createAtPos | PositionOption | 6 | SummonCriteria.cs:13 |
| moveToPos | PositionOption | 5 | SummonCriteria.cs:15 |

**ManaBuff**：**一个枚举字段都没有**（全是 `int` + `bool`）。**ActiveAbility**：也没有自己的枚举（`TargetCriteria` 那两格已在表中）。
· ActiveAbility 非枚举(int)：`minHeroLevel`(8) · `manaCost`(10) · `resolutionChoosingCount`(20)

**CardSO（卡根，顺手一并扫了）**
| 字段 | 枚举类型 | 取值数 | 出处 |
|---|---|---|---|
| attackDamageType | DamageType | 2 | RawCardScript.cs:50 |
| cardArmy | CardArmy | 12 | ObtainableItem.cs:19 |
| cardType | CardTypeOptions | 3 | RawCardScript.cs:37 |
| initiative | Initiative | 2 | RawCardScript.cs:89 |
| inventoryOptions | CardInventoryOptions | 3 | RawCardScript.cs:15 |
| keyword | Keyword | 8 | RawCardScript.cs:39 |
| showInCollectionOptions | ShowInCollectionOptions | 2 | RawCardScript.cs:13 |
| spellType | SpellType | 11 | RawCardScript.cs:35 |

## 4. ⚠️ 未找到定义的字段 / 取值

| 字段 | 取值 | 次数 | 搜过什么 | 实据 |
|---|---|---|---|---|
| `DamageCriteria.valueOptions` | **8** | 34 | 三份枚举源逐字比：`Assembly-CSharp/ChooseValueOptions.cs` · `d:/2/Warpforge_tools/tmp/full_decomp/ChooseValueOptions.cs` · `d:/2/tools/il2cpp_out/dump.cs:20865` —— **三份都只有 0,10,20,30,40,41,50,60,70,80,90,100,101,110,120,130,140,150,160,170,200~204,210** | 反编译 `AbilityLogic__GetValueFromCriteria.c`（调用点 `AbilityLogic__PlayAbility.c`）的分派里**没有 8 这一支**：`8` 落进 `if (param_7 < 0xb)` 且 `!= 10` ⇒ **`return minValue`**（不做随机）。⚠️ 这 34 条**全部** `damageType = 5(Random)`，且 min/max 都是区间（26 条 3–5、4 条 1–2…），与 `90 RandomizedOnResolution` 的语义重叠 ⇒ **推测**是旧版枚举值（改名/删值后旧卡 SO 没重存）。**这是推测，不是结论** |
| `CardEffect.healthValueOptions` | **17** | 8 | 同上三份源 | 8 处全在 `buffEffect`（`Cloud of Smoke` / `Attack Squig` / `Grot Bomb` / `Gretchin Mob` / `Scrag ’Em`），`healthChange = 3`、`buffType` 是 `addStats(1)` 或 `addTrait(3)`；反编译 `AbilityLogic__NewBuffStat.c` 的分派支只有 `0/10/20/30/40…`，**无 `0x11` 支**。**行为未追到底**，如实记「未找到」 |

## 5. 自检（端到端反证 · `ASH15 Howling Banshee`）

真值实读（卡名 `Howling Banshee` · `cardShortId 228` · `uniqueId ASH15`）：

- `cardAbilities[0].abilityLogic[0].chosenAbility = 130` → **`stun`**（`AbilityEffect.cs:27`）✅ 与卡面「Rally: Stun an enemy」一致。
- 同条 `targetCriteria.targetsAffected = 30` → **`target`**（`TargetsAffected.cs:6`）。
  ⛔ **不是「敌方单位」**：该 `TargetCriteria` 的 `enemyUnits/friendlyUnits/includeHero/includeMinions` **全是 0**。
- 「敌方」的判据在**卡级** `cardTargetCriteria` = `{targetsAffected:10 (allFiltered), enemyUnits:1, includeHero:1, includeMinions:1}` + `targeted=1` ⇒ 「从所有敌方单位（含督军与部队）里挑一个」。
- **推论（对账用）**：abilityLogic 里的 `targetsAffected` 只是「取谁」，**「敌/我/类别」那半在 `TargetCriteria` 的 bool + 卡级 `cardTargetCriteria` 里**。只读 `targetsAffected` 一定会误判。
- `cardRarity = 2` → `CardRarity.Rare`（`CardRarity.cs:4`）。**该字段在代码里声明成 `int`**（`ObtainableItem.cs:15`），靠 `SupportMethods.GetCardRarityFromInt` 按数值 `Enum.TryParse` 转（`PlayerItem__get_Rarity.c:15` + `SupportMethods__GetCardRarityFromInt.c`）⇒ 数值直映射成立。

## 6. 顺手发现（供主对话分流，我没动任何东西）

1. **`AbilityLogic` 实例数 1782 ≠ 简报的 1622** —— 另有 `activeAbility.activeAbilityLogic[]` 160 个（1192 张卡的 `activeAbility` 里 160 张有内容）。对账遗漏它 = 少 160 步。
2. **`ChooseValueOptions` 是「一个值 ≠ 一个意思」的高发区**：`valueOptions` 在 `DamageCriteria` / `HealCriteria` / `CardEffect` 的 5 个字段上共用同一枚举，同一个 `90` 在伤害里是「每次结算随机」、在治疗里也是 —— 但 `ThisCardAttack(40)` 用在**护甲**字段（`armourValueOptions=40` 未出现，`meleeAttackValueOptions=40` 出现 1 次）时读法要按属性名走。
3. **`TargetCriteria` 是复用最狠的结构体**：**23 条路径**用到它（一次 `AbilityLogic` 里最多 3 处：`targetCriteria` / `multiplier` / `handBuff.targetCriteria`；`CardAbility` 另有 4 处；卡级 `cardTargetCriteria` / `targetsAvailableCounter` / `choicesDefinition` + 两个 `countCriteria`）。⇒ **对账时按路径取，别按类型名取**。
4. **三个列表字段的「用没用」差异极大**（我实扫了 15472 个 `TargetCriteria` 实例 + 1782 个 `SummonCriteria`）：`excludeEnchantments` **0/15472 非空**（一次没用过）· `filterSpecificCards` **55/15472 非空**（0.36%）· `unitsToSummon` **308/1782 非空（219 张卡）**。
5. 🔴 **`SummonCriteria` 不只给「召唤」用** —— `unitsToSummon` 非空那 308 条里，**`createCardInHand(100)` 占 135 条**、`buffSelf(40)` 34 条、`doHeal(20)` 6 条、`buffOthers(30)` 30 条。这与 `AbilityLogic.cs:33` 的 Tooltip 自洽（"SummonUnit, CreateCard, Transform Self & Unit" 共用一个结构体）。⇒ 我们的引擎若把 `SummonCriteria` 只接在 summon 分支上，会**静默漏掉 135 条「造卡到手上」**。
6. **`AbilityTrigger` 有 57 个取值在真值里出现过**（枚举定义共 **97** 个）—— 本次最大的一个字段，也是引擎触发对账最该先看的一格。
7. **`ManaBuff` 零枚举**：`buffMana(60)` 那 69 条效果**没有任何枚举开关**，全靠 11 个 `bool`（`psychicMana` / `faithMana` / `questPoints` …）+ `manaAmount`。改成枚举会跑偏。
8. **`DefinedTrait` 定义 134 个值，真值里只出现 25 个（`CardEffect.trait`）+ 18 个（`TargetCriteria.traitsFilter`）**；`TargetsAffected` 定义 38 个、出现 30 个。⇒ 别拿「定义全集」当覆盖表。
8. **`DamageType` 在 `CardEffect` 里没有对偶字段**；但同一枚举被用在卡级 `attackDamageType`（`Attack(4)` 1138 / `Normal(0)` 54）。
9. **`SummonCriteria.isForEnemy` 是 bool、不是枚举** —— 「召唤给对方」这一层没有对应枚举名，别去找。
10. **脚本的 `--out` 参数可改输出路径**，TSV 是 CRLF + UTF-8（与仓库里其它 `.tsv` 一致）；脚本本身不写任何 `d:/2` / `Assets/`。
