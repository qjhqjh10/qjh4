# B146 · attackDamageType 判据（2026-11-05）

> **结论先行**：`attackDamageType` 在**原版战斗结算里也不被消费** —— 原版把攻击伤害类型**硬编码为 `Attack(4)`**
> （`BattleManager__ResolveAttackDamage.c:28`）。原版唯一的读点是 **AI 打分预览**，而该处对 `0/4` 两值结果完全一样
> （只用 `IsVulnerableDamage`，只认 7/8）。⇒ S3 记的「1,096 张有值 / 引擎整条没有伤害类型概念」是**判据本身错（半句）**：
> 数据缺口真、**机制缺口假**；我们引擎自 2026-10-29（`B94_B13`）**已有伤害类型概念**且已用于广播与成就。
> **施工建议：不建枚举、不改结算，只订正 4 处过期文字**（§五末）。

## 一、原版字段与取值

| 项 | 值 | 出处 |
|---|---|---|
| 字段 | `RawCardScript.attackDamageType`，类型 `DamageType`，偏移 **`+0xAC`** | `dump.cs:23268` |
| 枚举 | `DamageType`（TypeDefIndex 923），13 个值 | `dump.cs:45507-45524` |
| 默认值 | 构造时 `attackDamageType = DamageType.Attack` | ILSpy `RawCardScript.cs:2556`（`.c` 未单独出产物） |

**枚举 13 值 + 生产点计数**（ILSpy 全 2 程序集 `grep "DamageType.X"`；`Vulnerable` 用算术 `+7` 构造，故按名 0 次）：

| 值 | 名 | 命中 | 含义 |
|---|---|---|---|
| 0 | `Normal` | 13 | 无归类伤害：手里捏的 shuriken/sentry/markerlight、猎杀标记、哨戒、随机伤害归一后 |
| 1 | `Ranged` | **0** | 无生产点（见 §八） |
| 2 | `Undersupplied` | 1 | 疲劳伤害（`BattleManager._ResolveFatigue…:134`） |
| 3 | `Ability` | 1 | 能力/法术伤害（`AbilityLogic__PlayAbility.c:1561` 显式覆写） |
| 4 | `Attack` | 13 | **近战/远程战斗伤害**（字面量，见 §四） |
| 5 | `Random` | 3 | 待掷：`valueOptions=RandomizedOnResolution` 时由 `GetAdjustedDamage` 掷；否则归 `Normal` |
| 6 | `Blast` | 3 | 溅射（trait adjacent） |
| 7 | `Vulnerable` | 0 按名 | 易伤第二发（物理）：算术 `(type==4?1:0)+7` |
| 8 | `VulnerableCombat` | 1 | 易伤第二发（战斗来源）：同上 +8 |
| 9 | `Poison` | **0** | 无生产点（见 §八） |
| 10 | `RandomPoison` | **0** | 无生产点（见 §八） |
| 11 | `Stomp` | 1 | 践踏 |
| 12 | `RandomAbility` | 1 | 待掷（能力）→ 掷后归 `Ability(3)` |

⇒ 它是**「这一次伤害是什么来源」的标签**，不是「物理/灵能/毒/火」的元素分类。决定三件事：
① 触发「被攻击伤害」还是「被能力伤害」那一族事件；② 易伤额度是否生效（7/8 不再叠易伤）；③ 若干关键词是否响。

**数据真值（现读 2026-11-05）**：
- `_tmp_view/allcards_export/cards_raw.json`（1,351 张 SO）：**`4` 1,295 张 / `0` 56 张**；56 张 `0` **全部**是 `cardType=10 Hero`（另 10 张 Hero 是 `4`）。
- `cards_engine.json`（池内 1,142 张）：**`4` 1,096 / `0` 46**。分类：unit 586/4 · tactic 459/4 · defence 39/4 · enchantment 2/4 · hero 46/0 + 10/4。
  ⇒ S3 那句「1,096 张有值」= **全部非督军卡恒为 4**，只有督军分 0/4 两档。

## 二、读点表（影响什么）

**A. 真正读 `attackDamageType` 这个字段的地方（全库只有 3 个文件）**

| # | 位置 | 函数 | 用它做什么 | 实际影响 |
|---|---|---|---|---|
| 1 | ILSpy `AI.cs:1399,1404,1407,1412,1419`（`.c` `AI__ScoreFromBuffedAttack.c:34/42/52/60/67`） | `AI.ScoreFromBuffedAttack` | 填给 `DamageInfo` 的 damageType，喂 `ScoreFromDamagingBuffedUnit` | **零**：该函数只在 `AI.cs:1519/1556` 用 `IsVulnerableDamage(t)`，`0/4` 都是 false |
| 2 | ILSpy `EntityScript.cs:120-128`（`.c` `EntityScript__get_attackDamageType.c`） | 属性 getter | 转发 `rawCardData.attackDamageType` | **0 调用者**（ILSpy 两程序集 + `decomp_full` + prefab/assets 三处 grep） |
| 3 | ILSpy `BasicCardUI.cs:174-182`（`.c` `BasicCardUI__get_AttackDamageType.c`） | 属性 getter | 同上 | **0 调用者**；`assetripper_out/ExportedProject` 与 `assets_full` grep `AttackDamageType` 只命中脚本本身 ⇒ **没有 UI 图标** |

**B. `DamageType` 这个轴本身的读点（与「该字段」区分；决定要不要建枚举）**

| 位置 | 函数 | 读法 | 影响 |
|---|---|---|---|
| `CardScript.cs:6391`（`GetAdjustedDamage.c`） | `GetAdjustedDamage` | `Random→Normal`、`RandomAbility→Ability(3)`；其余原样透传 | 掷骰归一；**不按类型改数值/护甲** |
| `CardScript__ResolveDamageDealt.c:50,66` | `ResolveDamageDealt` | `damageType == 4` | **syphon 回血 / poisonous 上毒** 只在「攻击伤害」时响 |
| `CardScript__ResolveDamageDealt.c:191-208` | 同上 | `switch(type)`：`0/3`→触发 `185 UnitDamagedByAbility`；`4/6`→`187 UnitDamagedByAttack`；`4` 且 `actingCard==this` 才 concussive | **「被攻击/被能力」两族事件的唯一分流轴** |
| `CardScript._ReceiveDamage…:193-232,287-291` | `ReceiveDamage` 协程 | `IsVulnerableDamage`；易伤第二发类型 = `(type==4?1:0)+7` | 易伤额度只在非易伤伤害上加；第二发不叠易伤、走另一条 `DamageInfo` |
| `BattleManagerSupport__BroadcastUnitDamaged.c:31` | `BroadcastUnitDamaged` | `type == 4` 时才额外把伤害投递给 `actingCard`（哪怕它已不在场） | 保证攻方自己的 syphon/poisonous 在反击里也响 |
| `BattleManager.cs:7485/7493/7502`（`.c` 同名） | `ResolveAttackDamage` / `ResolveTraitAdjacentDamage` / `ResolveTraitStompDamage` | **生产点**：`Attack(4)` / `Blast(6)` / `Stomp(11)` 全是**字面量** | — |
| `BattleManager.cs:8153` | 死亡类型 | `VulnerableCombat` | 死在易伤第二拍时的 deathType |
| `BattleManager.cs:23819-23860` | `CheckIfAttackKillsTarget` | 构造 `List<DamageType>` 时**写字面量 4** | 预判能否击杀 |
| `CardHighlight.cs:164-235` | `CardHighlight`（UI 预览） | 自己造 `List<DamageType>`：战斗=4 / shuriken·markerlight=0 / 能力=3 | 悬停「会不会死」高亮；**不读 attackDamageType** |
| `AI.cs:2698` + `AI.cs:1519/1556` | `ScoreFromCriteria` / `ScoreFromDamagingBuffedUnit` | 能力 `DamageCriteria.damageType`；只判 7/8 | AI 打分 |
| `AbilityLogic.cs:463-628` | `PlayAbility` | 能力伤害类型取 `damageCriteria.damageType`；`Random` 未配 `RandomizedOnResolution` 时归 `Normal`；`case 12` 支显式覆 `Ability(3)` | 能力伤害分流 |
| `GameStaticData.cs:565` | `IsVulnerableDamage` | `(dmg - 7) <= 1` | 全库唯一易伤判据 |
| `DamageDeltToEnemyUnit.cs:86,96,230` | 任务/成就 `OnDamageDelt` | 小兵/督军：`attackType` 匹配 **且 `damageType==4`**；`anyDamageType=0` 时还要 `== this.damageType` | 成就 `Ach_86`（Stomp）用；`Ach_2/3/4` 的攻击类型闸 |
| `CardScript.cs:7070-7127` | `EnoughPendingDamageToDie…` | 把 `List<DamageType>` 一起传下去 | ⚠️ **`.c` 签名只有 2 个参数**（`CardScript__EnoughPendingDamageToDieWithDamageValues.c:1`，`param_3` 出现 0 次）⇒ **该列表在方法体内未被使用** |

## 三、写点表

| 写点 | 值 | 说明 |
|---|---|---|
| `RawCardScript` 构造（ILSpy `RawCardScript.cs:2556`） | `Attack(4)` | 唯一代码写点 |
| SO 序列化 `+0xAC` | 4 / 0 | `cards_raw.json`：1295 / 56；`cards_engine.json`：1096 / 46 |
| 运行时 buff / 效果 | **无** | 全库 `attackDamageType =` 只命中构造；`CardEffect`/`BuffType` 无此字段 |

## 四、与近战/远程/现有 DamageType 的关系

- `meleeAttack(+0xA4)` / `rangeAttack(+0xA8)` = **打多少**；`attackDamageType(+0xAC)` = **这一下算什么类型**。三者是**并列字段**，
  但结算路径**只读前两个**：`ResolveAttackDamage` 用 `GetCombatDamage(attackType)` 取数值，类型**硬编码 `4`**（`.c:28`）。
- `DamageCriteria.damageType(+0x1C)` 是**另一处**同类标签（能力伤害自带），数据里是 `abilityDamage[].type`：`0` 1,485 行 / `5` 133 行 / `3` 3 行。
- 我们自己的 `DamageKind`（`AchievementTally.cs:40-50`，9 档）是**第三条轴**（自建）；`RuleCore.DamageTypeOfKind`（`:17220`）
  显式映射到原版 `DamageType`，**⛔ 数值撞车**（`DamageKind.Shuriken==4` ≠ `DamageType.Attack==4`）。

## 五、我们的现状与缺口

✅ **已经有的（不是缺口）**：
- 伤害类型**概念与落地**：`RuleCore.DamageTypeOfKind`（`:17220-17233`）→ `BroadcastWhen(…, damageType:)`（`:9596`）→
  手牌两跳 `185/187`（`BroadcastUnitDamagedHandHops`）；易伤 7/8 与第二发在 `CardScript` 等价物里已接（`EventTiming.cs:111/290/851`）。
- 成就侧：`AchievementTally.DamageKind` + `ProfileData.DamageKindOf`（`:722-733`），`Ach_86` 按 `damageType==11` 认。
- 数据侧：`attackDamageType` **已被解析**（`CardDatabase.cs:215`，`JsonUtility` 照旧读入），只是 `[Obsolete]` 标记「无消费者」。
- 能力伤害类型 `abilityDamage[].type` 已消费（`CardDef.AbilityDamageSpec.Type:6811`），但**只用于** 5/10/12 的逐目标掷（`valueOptions==90`）。

🟠 **真实缺口（只有两条，且都零可观测）**：
1. 能力伤害我们恒发 `DamageType 3 (Ability)`，原版数据里 1,485 行是 `0 (Normal)`。**行为等价**：`0/3` 在
   `ResolveDamageDealt` 同一 `case`、`IsVulnerableDamage` 同为 false、`DamageDeltToEnemyUnit` 对战术卡不看类型。
2. AI 预览没带 damage type（`AiScoring.BestAttackScore`）。因为 `0/4` 在 `IsVulnerableDamage` 下同值，**也是零可观测**。

⚠️ **需要就地订正的四句（铁律 5；本报告只读、未改）**：
- `RuleEngine/Data/CardDatabase.cs:190-193`「我们引擎**整条没有伤害类型概念**」→ **已过期**（2026-10-29 `B94_B13` 起有）；
- `CardDatabase.cs:204`「`attackDamageType = 0` …（**54 张**，多为督军）」→ 现读 **46 张（池）/ 56 张（SO）**，且**全是督军**；
- `判据/B146_S3_数据列消费覆盖.md:66` 把本列列为「真缺口候选」→ 应改为「原版同款不使用，**非缺口**」；
- `活状态表.md:477` `B146-S3-ATTACKDAMAGETYPE`（🟠 真缺口候选）→ 应核销为「判据本身错」；
- `判据/B91_P3_战斗数据列缺口.md:120`「施工动作：把 DamageInfo 带上伤害类型」→ 可以保留为**可选的形式复刻**，但必须写明**零行为**。

## 六、最小落地模型（不建 / 建，推荐）

**方案 1（推荐）· 不建**：一行代码不改。只做 §五 的五处文字订正 + 本报告入账。
- 成本 0；风险 0；断言 0 条；伤害数值 / 图标 / AI / 成就**全部维持现状 = 与原版一致**。
- 依据：原版结算**不读**该字段（§四）；唯一读点是 inert 的 AI 预览（§二 A-1）。

**方案 2 · 只形式复刻 AI 读点**（如果坚持「原版读了就照抄」）：在 `RuleEngine/Data/AiScoring.cs` 的
`BestAttackScore` 族里，把 `DamageInfo` 第 4 参换成 `card.AttackDamageType`（先去掉该字段的 `[Obsolete]` 或另开只读属性）。
- 成本：约 3-5 行 + 1 条断言；**行为零变化**（`0/4` 在 `IsVulnerableDamage` 下同值）。
- 验收口：`bash 工具/ruleprobe.sh ai <卡id>`（AI 不进 `play all dump`，dump 逐字节不变**不代表**没生效）。
- 断言：可断「`AttackDamageType ∈ {0,4}` 的卡，AI 分数与硬编码 4 完全一致」——**这条断言将永远为真**，价值主要是防将来误用。

**方案 3 · 在战斗结算里用它** —— ⛔ **不要做**。若把 `EmitHit` 的近战/远程类型改成 `card.AttackDamageType`，
46 张 `0` 的督军会把攻击伤害发成 `185 UnitDamagedByAbility`，并失去 syphon / poisonous / concussive 三条**原版有效**的行为
⇒ **制造偏差**，与「照原版硬编码 4」相反。

## 七、可达性与可观测差异

- 分布：`4` 1,096 张（全部非督军 + 10 督军）· `0` 46 张（督军）。**只有督军能看出两档**。
- 今天（不建）与原版的可观测差异：**0** —— 伤害数值、187/185 分流、易伤、成就 `Ach_2/3/4/86`、AI 决策、UI 全部相同。
- 若按方案 3 误接：**46 名督军**（约占卡池 4%）的攻击伤害分类翻转，且 syphon/poisonous/concussive 静默失效
  （`ResolveDamageDealt.c:50/66/206` 三道 `==4` 闸同时不命中）——这是**唯一**能观测到该字段的方式，也是**不该出现**的方式。
- 若原版哪天出现 `attackDamageType ∈ {7,8}` 的卡，AI 预览才会分歧；**当前数据里没有**。

## 八、没查清的

1. `DamageType.Ranged(1)` / `Poison(9)` / `RandomPoison(10)` **在 ILSpy 与 `.c` 里都找不到生产点**（按名 0 次；
   `.c` 里 `DamageInfo___ctor` 第 4 实参只出现 `0/2/4/6`）。可能是 ILSpy 丢体的方法里的遗留，也可能是死值 ——
   与本条无关，但列进「已知未覆盖面」备查（判「死值」需 VA 级反汇编，本次未做）。
2. 46 督军 `0` / 10 督军 `4` 的**设计意图**（是否旧版本遗留、是否曾配不同攻击类型）本地无判据。
3. 是否有**非代码绑定**（Animator 属性路径 / UnityEvent 字符串）读 `AttackDamageType`：已在
   `assetripper_out/ExportedProject` 与 `assets_full` 全树 grep 该串，**只命中脚本本体**；Animator 的 property path 未逐条穷举（低风险）。
4. `cards_engine.json` 当前 `count = 1,142`（CLAUDE.md 记的 1,127 已过期），本报告按现读。
