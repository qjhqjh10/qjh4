# R_SO与触发代码链_可行性（2026-10-17 · 只读普查）

> 任务：验证用户提出的「**uniqueId → 卡牌 SO → 数值 + 效果引用 → 与 `cards_engine.json` 对上 ⇒ 逆推卡是谁**」这条链是否成立、覆盖率多少。
> 方法：只读。读 `d:/2/新解包资源/assets_full/`（84 个 bundle 的 JSON 导出，24 万个对象）、
> `d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/`（现客户端 84 个 bundle + `catalog.bin`）、
> `d:/2/远端缓存_旧profile_1013/`（旧 profile 抢救出的 3 只远端包）、
> `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`（签名桩）、`d:/2/tools/il2cpp_out/dump.cs`、`d:/2/tools/decomp_full/`（26,282 个方法体）。
> **没有跑 Unity、没有改任何文件（本报告除外）。**

---

## 〇、四节结论先讲

| 节 | 一句话结论 |
|---|---|
| **一** | 卡 SO 的类是 **`RawCardScript : PlayerItem`**，`uniqueId` 与 `cardName` 都是它的**自有字段**（不是外部映射表）。本机**有 1,192 张完整卡 SO**，但只在**旧 profile 缓存**里（2025-07-30 快照），`assets_full` 里**一张都没有**。 |
| **二** | 5 张已知卡：**5/5 全程走通**（uniqueId → SO → 卡名 + 四格数值 + 效果链 → 与 `cards_engine.json` 逐字段相等）。5 个未知 id：**0/5 能走到 SO**（连一张都没有），只有 2/5 能拿到「引用文件」级别的旁证。 |
| **三** | 效果是**纯数据驱动**：`CardAbility{trigger, abilityLogic[]}` + `AbilityLogic{chosenAbility=AbilityEffect}`。**83 种效果 / 97 种触发**（快照实际用到 **53 / 57** 种），由 `AbilityLogic.PlayAbility`（2,878 行）一个 switch 分发到 **49 个 `BattleManager__Add*` 操作**。与我们的 `desc` 对得上（抽卡 97% · 治疗 98% · 消灭 100% · 眩晕 96% · 召唤 95% · 伤害 81%）。 |
| **四** | **本地直读覆盖率 = 0 / 118**（缺的那批卡的 SO 不在本地）。**只要拿到当前 build 的 `allcards` bundle，覆盖率 = 118 / 118 = 100%**（判据：SO 自带的 `uniqueId`+`cardName` 在 1,192 张里 **1,192/1,192 非空、0 重复**）。 |

---

## 一、卡 SO 长什么样、怎么认

### 1·1 类与出处（以实读到的 `m_Script` 类名为准）

| 项 | 值 | 出处 |
|---|---|---|
| **类名** | **`RawCardScript : PlayerItem : ObtainableItem, IPurchasableItem, IIdentifiable`** | 签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs:7`；当前 build 的 `dump.cs:23244`（`// TypeDefIndex: 458`） |
| **同包的另外两个类** | `AlternateArtCard` · `AddressablesBuildNumber` | 缓存 bundle 里的 3 个 `MonoScript` |
| **`m_Script` pathID** | **`8864902669260471755`** | 缓存 bundle 的 `MonoScript` 对象实读（`m_ClassName=RawCardScript`、`m_Namespace=""`、`m_AssemblyName="Assembly-CSharp"`） |
| **本机唯一的完整卡 SO 包** | `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1114a2de41519cac560a4bce4/dca75913f60923172e82862460139ede/__data`（**467,359 B**；1,199 个对象 = **1,195 MonoBehaviour + 3 MonoScript + 1 AssetBundle**） | UnityPy 实读；README → `d:/2/远端缓存_旧profile_1013/README.md` §二 |
| **已导出的 JSON** | `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（**1,195 条**：**1,192 `RawCardScript`** + 2 `AlternateArtCard` + 1 `AddressablesBuildNumber`） | 逐条按 `m_Script.m_PathID` 分派实读 |

**年代**：这份包是 **Unity 2022.3.38f1 / 2025-07-30** 的快照；现客户端是 **Unity 6000.2.6f2 / 2026-08-15**（判据见 `d:/4/Unity/资料/普查产出_第三十三会话/R9_卡牌id真值挖掘.md` §补）。**同一个 Steam 安装，隔了一次大版本重建。**

### 1·2 字段表（`RawCardScript`，当前 build 的偏移与快照逐字段一致）

| 类别 | 字段 | 行号 / 偏移 |
|---|---|---|
| **id** | **`uniqueId`（string，继承自 `PlayerItem`）** | `PlayerItem.cs:14` |
| **名字** | `cardName`（string）· `refNameId` · `nameModifier` | `RawCardScript.cs:21` · `dump.cs:23253`(0x60) |
| **本地化键** | `itemDescriptionKey` / `itemLoreKey`（`PlayerItem`）· `activeAbilityTextReferenceId` · `cardDescriptionTextReferenceId` · `cardRaceTextReferenceId` · `targetingTextReferenceId` | `RawCardScript.cs:169` / `:175` |
| **阵营** | **`cardArmy`（`CardArmy` 枚举，10=Ultramarines … 130=SpaceWolves）** | `PlayerItem.Rarity/Army`；枚举 `dump.cs:45634` |
| **数值四格** | **`manaCost` · `meleeAttack` · `rangeAttack` · `maxHealth` · `armour`** | `RawCardScript.cs:27/46/48/41/43`；`dump.cs:23256/23266/23267/23263/23264` |
| **类型** | `cardType`（`CardTypeOptions`：Minion=0/Hero=10/Tactic=20/Whispers=40）· `spellType`（`SpellType`，含 `DefensiveCard=210`）· `keyword`（`Keyword`：Infantry=5/Vehicle=10/…） | `dump.cs:45710` / `:45948` / `RawCardScript.cs:39` |
| **稀有度/扩展包** | `cardRarity`（`CardRarity`：None0/Common1/Rare2/Epic3/Legendary4/Special5）· `expansionPack`（`CardExpansionType`） | `PlayerItem.cs:8` |
| **效果（规则层）** | **`cardAbilities : List<CardAbility>`（内联序列化，不是指针）** · **`activeAbility : ActiveAbility`（内联）** · **`alternativeAbility : ActiveAbility`** · `cardTargetCriteria` · `targetsAvailableCounter` · `choicesDefinition` | `RawCardScript.cs:189/175/203`；`dump.cs:23339`(0x290) |
| **效果（表现层）** | `cardAnimations : List<AssetReferenceTyped<CardAnim>>`（GUID）· `summonAudio` / `attackAudio` / `deathAudio` / `customRangedAttackParticles` / `customDeathParticles`（全是 `AssetReferenceTyped` GUID） | `AbilityLogic.cs:11`；`RawCardScript.cs:17` 起 |
| **立绘** | **`cardImageReference : AssetReferenceTyped<Sprite>`（`m_AssetGUID`）** | `RawCardScript.cs:17` |
| **卡间指针** | `relatedCard1..4` · `referenceCard` · `secretIdentity` · `startingCardInHand` · `goSecondCardInHand` · `startingTroopsInPlay` · `ActiveAbility.loadingAbilityLogic` · `SummonCriteria.unitsToSummon`（**全是同包内直接 PPtr**） | `dump.cs:23276-23279`、`:23291-23293` |
| **trait（卡面橙字/关键词）** | `CardTraits : List<CardTraitData>`（`{name, _trait, _traitValue, _hide}`）· `ShowTraits` | `RawCardScript.cs:161`；`dump.cs:23325`(0x230) |

> ✅ **回答任务的四个小问**：有 `uniqueId`；有 `cardName` + 4 个本地化键；有 `cardArmy`；有数值四格（费用/近战/远程/生命 + 护甲）；
> 效果引用字段**有两层**——规则层是**内联结构体**（`cardAbilities` / `activeAbility`），表现层才是指针（`CardAnim` 的 AssetGUID）。

### 1·3 本机三种形态（这是全篇最关键的判据）

| 位置 | 有没有 `RawCardScript` | 判据 |
|---|---|---|
| `d:/2/新解包资源/assets_full/`（84 个 bundle 的 JSON 导出） | **0 张** | 全树 240,174 个 JSON 里 **含 `"cardAbilities"` 的文件 = 0 个**；含 `"uniqueId"` 的只有 `cosmeticsso`(1,238) / `prebuiltdecks`(236) / `menus`(4) / `duplicateassetisolationso`(2)，**没有一个是卡** |
| 现客户端 `StreamingAssets/aa/StandaloneWindows64/` | **0 张** | 目录里只有 84 个本地 bundle（最大的是 `*cardassets` / `*cardanims`，前者只有 Sprite/Texture2D/AudioClip，后者只有 `CardAnim`） |
| `catalog.bin` | **列出但不在本地** | `catalog.bin` 里写着 `allcards_assets_all_ea8bca4f5c6909d3124fd4975dc96173.bundle`、内部 bundle 名 `54bdb7b1114a2de41519cac560a4bce4`、`RawCardScript`、"`{CcdManager.EnvironmentName}/buckets/...`" 远端 URL 模板 —— 该文件磁盘上不存在 |
| **旧 profile 缓存** | **1,192 张** | 见 1·1 |

补充判据（防止「导出漏了」的假否定）：`assets_full/_stats.json` 里**所有 bundle 的 `fail` 都是 0**；有 fail 的只有 6 个内置文件（`resources.assets` 616 / `sharedassets0` 182 / `level0` 73 / 其余 34），合计 905 —— 而且那 6 个文件本来就只装内置资源。⇒ **不是「没导出来」，是本地根本没有这个包。**

### 1·4 怎么认（下次照这三条认，不会认错）

1. `m_Script.m_PathID == 8864902669260471755`（旧包）→ `m_ClassName == "RawCardScript"`；**别按文件名认**（1,195 个对象的资产名是 `33 EmperorsChildren-Heldrake` 这种带序号的）。
2. 字段指纹：同时有 `uniqueId` + `cardAbilities` + `cardImageReference` + `manaCost/meleeAttack/rangeAttack/maxHealth`。**只认 `uniqueId` 会把 1,238 个 Avatar/掉落表误当卡**（它们也有 `uniqueId`）。
3. 当前 build 的字段偏移与旧包**逐一相同**（`dump.cs:23244-23340` 对照 `RawCardScript.cs`）⇒ **旧包的解码器可以直接解新包**，不需要重新逆类型。

---

## 二、链路的端到端抽样（可行性证据）

### 2·1 五张已知卡（真值来自 `card_ids.json` 的 `mapping`，判据在旧 SO 里逐字段复核）

公共坐标：缓存 bundle = `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1114a2de41519cac560a4bce4/dca75913f60923172e82862460139ede/__data`；
导出 JSON = `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`；`guid_map.tsv` = `d:/4/Unity/资料/普查产出_第二十七会话/guid_map.tsv`。

| # | uniqueId | SO pathID | 卡名 | 阵营/类型 | 费用/近战/远程/生命/护甲 | 效果（`trigger → chosenAbility`） | 立绘 GUID → 资产 |
|---|---|---|---|---|---|---|---|
| 1 | `AM1` | `4233893157370540475` | Lord Solar Leontus | AstraMilitarum / Hero(10) | 2 / 2 / 2 / 40 / 0 | `Talent(46) → createCardInHand(100)`；主动技 `doDamage(10)` | `a2fa478c…` → `AstraMilitarum_warlord_Lord Solar Leontus`（`astramilitarumcardassets_assets_all.bundle`） |
| 2 | `SOR36` | `-9178711322392737160` | Exorcist | Sororitas / Minion(0) Vehicle | 10 / 7 / 10 / 10 / 1 | `UseFaith(605) → doDamage(10)`，`minDamage=maxDamage=5`、`abilityLogic.targetCriteria.targetsAffected=allFiltered(10)` + `enemyUnits=1/includeHero=1/includeMinions=1/includeRemnants=1`、`triggerValue=8` | `83b7d6f5…` → `Sororitas_veh_Exorcist` |
| 3 | `GOF105` | `8833053248056714638` | Monster Hunters | Goff / Tactic(20) | 4 / 0 / 0 / 0 / 0 | `ThisCardPlayed(0) → summonUnit(70)` ×2 条，`unitsToSummon[0] = GOF79 "Beast Snagga Boy"` | `4c1b0703…` → `Ork_BeastSnagga_strat_Monster Hunters` |
| 4 | `SAU23` | `-8393802315110128614` | Lychguard | Sautekh / Minion(0) Infantry | 5 / 5 / 0 / 5 / 2 | **无 `cardAbilities`**（白板），`CardTraits` 空 | `ac35fdb5…` → `Necron_Sautekh_inf_Lychguard` |
| 5 | `DA61` | `-7338547493207901965` | Plasma Generator | DarkAngels / Tactic(20) `spellType=DefensiveCard(210)` | 3 / 0 / 0 / 0 / 0 | `ThisCardPlayed(0) → buffMana(60)`，`playersAffected=Self(10)` | `fb20190f…` → `DarkAngels_strat_Plasma Generator` |

**与我们的卡表逐字段对**（`d:/4/Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`）：

| id | 卡名相等 | cost/atk/hp/ranged/armour 相等 | 类型 | 稀有度 |
|---|---|---|---|---|
| AM1 | ✅ | ✅ | hero ✅ | legendary = 4 ✅ |
| SOR36 | ✅ | ✅ | unit ✅ | rare = 2 ✅ |
| GOF105 | ✅ | ✅ | tactic ✅ | rare = 2 ✅ |
| SAU23 | ✅ | ✅ | unit ✅ | epic = 3 ✅ |
| DA61 | ✅ | ✅ | defence（spellType 210）✅ | special = 5 ✅ |

**⇒ 5/5 一一对上，零歧义。** 因为 SO 自己就带 `cardName`，「逆推」这一步其实是**直接读**，不是匹配。

**链路里还能顺出来的两跳**（证明包内引用是活的）：
- `AM1` 的 Talent `createCardInHand` → `summonCriteria.unitsToSummon[0] = PPtr(-7141399273149324622)` → 同包 `RawCardScript` **`c06453b330af61745beb5fa567ba863e` = "Lord Commander"**。
- `GOF105` → `PPtr(-9152277321431486712)` → **`GOF79` = "Beast Snagga Boy"**。
- 全包统计：`unitsToSummon` 的 PPtr **共 397 个，395 个能在同包解析（99.5%）**，只有 2 个解析不到。

### 2·2 五个未知 uniqueId（从 `card_ids.json` 的 116 条「快照里没有」里挑）

| # | uniqueId | 我们的卡池 | 本地能查到什么 | 能到 SO 吗 | 能读效果吗 | 能唯一对上吗 |
|---|---|---|---|---|---|---|
| 1 | `SW20` | Thunderwolf Cavalry · unit 3/3/4 远程3 | **10 个文件**：`DT SpaceWolves R3`（`cardRarity=3 Epic`、`cardArmy=130`）· 3 个 draft pack（`Colossal Thunderwolves`/`Towering Muscles`/`Tundra Stalkers`，`packTier=1/2/2`）· 6 副预组（`SW_SK_1`/`SW_SK_3`/`SW_SK_Starter`/`Deck_SpaceWolves_Starter`/`SpaceWolves_Deck1`/`SpaceWolves_Deck5`） | ❌ | ❌ | ❌ |
| 2 | `DA78` | Vengeful Brethren Intercessor · unit 2/2/3 | **0 个文件**（全 24 万 JSON 零命中） | ❌ | ❌ | ❌ |
| 3 | `UM87` | Bladeguard Ancient · unit 7/7/7 | 2 个文件：`DT UltraMarines R4 Expansion Only` / `DT Ultramarines R4`（`cardRarity=4 Legendary`） | ❌ | ❌ | ❌ |
| 4 | `GSC48` | **卡池里没有** | 0 个文件 | ❌ | ❌ | ❌（但卡名 `Backstab` 在快照里是 **`GSC40`** ⇒ 这个 id 是旧表偏移出来的**假 id**） |
| 5 | `SOR76` | **卡池里没有** | 0 个文件 | ❌ | ❌ | ❌（卡名 `Emperors Judgement` 快照里**完全没有**） |

> 说明：`DT` = `d:/2/新解包资源/assets_full/bundle_cosmeticsso_assets_all/MonoBehaviour/DT*.json`；
> `draft pack` = `bundle_draftpacks_assets_all/MonoBehaviour/*.json`；`预组` = `bundle_prebuiltdecks_assets_all/MonoBehaviour/*.json`。

### 2·3 抽样汇总（直接回答）

| 问 | 答 |
|---|---|
| 5 个未知里，几个能走到**卡 SO** | **0 / 5** |
| 几个能读到**效果引用** | **0 / 5** |
| 几个能靠「阵营前缀 + 数值 + 效果签名」**唯一**对上 `cards_engine.json` | **0 / 5**（连数值和效果都拿不到，「前缀」只能给阵营） |
| 5 个已知里，几个全程走通 | **5 / 5** |

**根因不是方法错，是「被问的那批卡，本机没有它们的 SO」**：1,192 张 SO 覆盖的是 **2025-07-30** 的卡池；未知的 118 张全是**那次快照之后新增**的（SW 68 · UM 15 短号 + 23 张无号 · DA 12）。

---

## 三、触发代码的分类清单

### 3·1 挂法（两层，都是「数据」，不是「每张卡一个类」）

```
RawCardScript
├── cardAbilities : List<CardAbility>          ← 内联（Rules 层）
│     ├── trigger : AbilityTrigger             ← 什么时候
│     ├── abilityContext / priority / triggerValue / playIcon
│     ├── actingCardCriteria / targetCriteria / boardCriteria / multiCountCriteria : TargetCriteria
│     ├── randomAbilityChoice : bool           ← true = 只随机触发其中一条
│     └── abilityLogic : List<AbilityLogic>    ← 做什么
│            ├── chosenAbility : AbilityEffect ← 效果枚举（分发键）
│            ├── playersAffected : PlayersAffected（Both0/Self10/Enemy20）
│            ├── damageCriteria { valueOptions, minDamage, maxDamage, damageType }
│            ├── healCriteria / buffEffect(CardEffect) / summonCriteria(unitsToSummon: RawCardScript[])
│            ├── handBuff(HandEffect) / manaBuff(ManaBuff)
│            ├── chanceOfHappening / furtherMultiplier / checkIfCasterInPlay
│            └── cardAnimations : List<AssetReferenceTyped<CardAnim>>   ← 表现层（GUID）
├── activeAbility : ActiveAbility              ← 主动技（英雄/单位「点一下」）
│     ├── manaCost / minHeroLevel / psychicCost / plasmaCost / targeted / needsLoading
│     └── activeAbilityLogic : List<AbilityLogic>   ← 与上面同一套 AbilityLogic
└── alternativeAbility : ActiveAbility         ← 替代技
```

**关键点：一张卡没有专属 C# 类。** 全部 1,192 张共用同一套执行器；卡的差异**只在数据**。
（复证：`AbilityLogic__PlayAbility.c` 里没有任何按卡名/卡 id 分支的代码，只有按 `chosenAbility` 数值的分支。）

### 3·2 效果分类表（`AbilityEffect`，83 个枚举值，快照用到 53 个）

枚举正本 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`（当前 build：`dump.cs:20645`，`// TypeDefIndex: 381`，值与桩逐条相同）。
分发入口 → `d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c`（**2,878 行**）；触发闸门 → `CardAbility__CanTriggerAbility.c`（143 行）。

| 行为族 | `AbilityEffect`（值） | 快照用量（逐值实测） | 反编译入口（可核） |
|---|---|---|---|
| **伤害** | `doDamage`(10) · `doDamageMultiRandom`(12) · `doDamageFast`(13) · `doDamageAndDestroySelf`(15) | **318**（全部是 10） | `BattleManager__DealMultiDamage.c`（115 行）· `AbilityLogic__PlayAbility.c` 的 `case 10` 分支 |
| **治疗** | `doHeal`(20) | **63** | 同上文件 `healCriteria` 分支 |
| **增益/减益** | `buffOthers`(30) · `buffOthersNotResetPool`(31) · `buffSelf`(40) · `buffHand`(50) | **526 / 1 / 178 / 25** | `BattleManager__AddEffect.c`(136) · `AddHandEffect` · `AddManaBuff.c`(81) |
| **抽卡/弃卡** | `drawCard`(0) · `drawUntilEqual`(330) · `discardFromHand`(160) · `discardWholeHand`(165) | **91 / 0 / 0 / 0**（后三个在快照里没有样本） | `AbilityLogic__AddDrawCard.c` · `BattleManager__AddDiscardFromHand.c` |
| **召唤/造卡** | `summonUnit`(70) · `summonUnitAndSetRef`(71) · `summonUnitAndTriggerEffect`(72) · `summonBuffedUnit`(75) · `summonUnitDelayed`(77) · `createCardInHand`(100/101) · `createCardInDeck`(270) | **94**（70）/ **169**（100）/ 其余 0 | `BattleManager__AddSummonUnit.c`(118) · `AbilityLogic__GetCardToSummon.c` · `AddNewCardToHand.c`(182) · `AddNewCardToDeck.c` |
| **变形/替换** | `transformSelf`(80/85) · `transformUnit`(90) · `exchangeSelfForMinion`(190) · `exchangeRandomFriendlyMinion`(200) | **全 0**（旧快照无样本，代码分支仍在） | `BattleManager__AddTransformCard.c` · `AddTransformCardWithBuff.c` · `AddTransformFromRemnant.c` |
| **攻击（近战/远程）** | `forceAttack`(230) · `forceAttackDontCountAttack`(231) · `forceAttackFriendly`(233) · `scriptedSelfAttack`(235) · `scriptedActingCardAttack`(236) · `scriptedTargetCardAttackDontCount`(237) · `scriptedSelfCardAttackDontCount`(238) | **全 0**（同上） | `BattleManager__AddScriptedAttack.c`(126) · `AddForceAttack.c` · `AddScriptedForceAttack.c` · `AddRedirectedAttack.c` |
| **控制** | `stun`(130) · `stunMultiplier`(135) · `jam`(120) · `destroy`(110) · `disappearUnit`(240) | **33 / 0 / 0 / 21 / 0** | `BattleManager__AddStun.c`(97) · `JamUnit.c`(79) |
| **移动/回手** | `recallToOwnerHand`(140) · `recallToCasterHand`(150) · `recallToDeck`(300/301) | **9 / 2 / 4 + 2** | `BattleManager__AddRecallToHand.c` / `AddRecallToDeck.c` |
| **偷/复制** | `stealFromHand`(170) · `stealFromDeck`(175) · `stealMinion`(180/185) · `copyUnitInCemetery`(210) | **0 / 0 / 1 + 1 / 0** | `BattleManager__AddStealFromHand.c` / `AddStealFromDeck.c` / `AddStealMinion.c` |
| **关键词触发族** | `triggerRelentless`(370) · `triggerSacrifice`(375) · `triggerRequiem`(390) · `triggerCourtesy`(400) · `triggerCodex`(410) · `triggerRegiment`(411) · `triggerSpiritStone`(440) · `triggerSynapse`(450) · `triggerMob`(460/461) · **`triggerPray`(462)** · `triggerAmbush`(465) · `triggerUprising`(466) · `triggerSlay`(467) · `triggerTeleport`(468) | **0 / 0 / 0 / 0 / 2 / 1 / 2 / 1 / 1+1 / 31 / 1 / 1 / 1 / 2** | 逐个对应 `BattleManager__AddTrigger*.c`（`AddTriggerPray.c` 83 行、`AddTriggerSpiritStone.c` 80、`AddTriggerSynapse.c` 83、`AddTriggerCodex.c`、`AddTriggerRegiment.c`、`AddTriggerSlay.c` …） |
| **遗物/祈祷/灵能（用户点名的修女 Pray）** | 见下面 3·4 | | |

**分发面有多大**：`AbilityLogic__PlayAbility.c` 里出现的**不同的 `BattleManager__Add*` 方法 = 49 个**（`AddSummonUnit` · `AddEffect` · `AddManaBuff` · `AddTriggerPray` · `AddTriggerSpiritStone` · `AddTransformCard` · `AddScriptedAttack` …）。这就是「效果 → 代码」的全部落点。

### 3·3 触发分类表（`AbilityTrigger`，97 个值，快照用到 57 个）

正本 → `AbilityTrigger.cs`（当前 build `dump.cs:20235`）。快照里出现频次最高的几类：

| 触发族 | 值 | 快照用量（逐值实测） | 说明 |
|---|---|---|---|
| 打出时 | `ThisCardPlayed`(0) | **652** | 绝大多数计策 |
| 天赋 | `Talent`(46) | **97** | 督军天赋 |
| 关键词触发 | `Codex`(145) · `Strike`(580) · `UseFaith`(605) · `UseSpiritStone`(600) · `Slay`(120) · `Artifice`(560) | **35 / 31 / 29 / 27 / 19 / 18** | 与 `CardTraits` 关键字一一对应 |
| 祈祷 | `Pray`(655) · `TriggeredPray`(660) · `OnCollectFaith`(665) | **23 / 3 / 1** | 修女/帝国信仰线 |
| 战场事件 | `OtherUnitDeath`(90) · `Unstable`(81) · `OtherUnitSummoned`(190) · `UnitAttack`(50) · `ThisUnitDeath`(80) · `UnitDamaged`(60) | **27 / 18 / 18 / 12 / 10 / 3** | 单位在场时 |
| 回合 | `TurnEnd`(40) · `TurnStart`(30) | **25 / 16** | |
| 持续 | `WhileInPlayVariable`(240) · `WhileInPlay`(20) | **28 / 8** | 光环 |
| 其他常用 | `FollowUpResolution`(410) · `EarnedSkull`(640) · `OtherCardPlayed`(10) | **24 / 17 / 14** | |
| 帝皇之子（EC）专属 | `Ecstasy`(715) · `Stimulation`(710) · `Cruelty`(720) | **10 / 10 / 9** | `Agenda(705)` / `Ferocity(725)` / `Oath(730)` 在旧快照 = **0** |

**触发判定逻辑**（`CardAbility__CanTriggerAbility.c` 实读）：
① `this.trigger == 传入 trigger`；② 特殊资源闸：`600 UseSpiritStone` / `605 UseFaith` 会去 `PlayerManager.GetCurrentSpiritStone/GetCurrentFaith` 比 `triggerValue`；
③ `FilterMethods__CheckIfMeetsCriteria`（515 行）过两张 `TargetCriteria`；④ `BoardAnalysis__MeetsCriteria`（155 行）过 `BoardCriteria`。

### 3·4 「Pray」这一条长什么样（用户点名的例子，可作为分类的样板）

```
CardAbility.trigger = 655 (Pray) / 660 (TriggeredPray) / 665 (OnCollectFaith)
  └─ AbilityLogic.chosenAbility = 462 (triggerPray)
       └─ BattleManager__AddTriggerPray.c (83 行)          ← 入队
            └─ BattleManager__ResolveTriggerPray.c (368 行) ← 结算
                 ├─ EntityScript__set_isPraying          ← 单位进入「祈祷中」
                 ├─ BattleManagerSupport__BroadcastUnitPray
                 └─ RawCardScript__OnTrigger → CardScript__TriggeredPray.c (14 行)
```
`AbilityLogic__PlayAbility.c` 里对应分支直接调 `BattleManager__AddTriggerPray`。
⇒ **「修女阵营的 Pray」在代码层是可以逐条盘出来的**，而且和卡面 `[[faith]]` / `Faith` 关键词对得上。

### 3·5 能否与 `cards_engine.json` 的 `desc` / `keywords` 对上（实测，非估计）

取 1,009 张 id 在快照里的卡（`cards_engine` 与快照都能读到），按「效果枚举 → 期望出现在 `desc` 里的词」逐一测：

| 效果 | 卡数 | `desc` 命中 | 命中率 |
|---|---|---|---|
| `destroy`(110) | 19 | 19 | **100%** |
| `doHeal`(20) | 54 | 53 | **98%** |
| `drawCard`(0) | 67 | 65 | **97%** |
| `stun`(130) | 25 | 24 | **96%** |
| `summonUnit`(70) | 65 | 62 | **95%** |
| `doDamage`(10) | 242 | 195 | **81%** |
| `buffOthers`(30) | 358 | 275 | **77%** |
| `buffSelf`(40) | 127 | 61 | 48% |
| `createCardInHand`(100) | 150 | 50 | 33% |

- 前六个基本可以直接当**判据**用；后两个低是因为（a）增益类文案千变万化（`+1 [Melee]` / `gets +1/+1` / `increase … by`），（b）`createCardInHand` 的文案常写成具体卡名或 `$$guid$$` 占位。
- **`keywords` 也能对上**：SO 的 `CardTraits[] {name,_trait,_traitValue}` → 我们的 `keywords`（例：`SOR36` 的 `{name:"blast",_trait:350,_traitValue:4}` = 我们的 `Blast 4`；`armour` 是独立 int 字段 = 我们的 `Armour 1`；`keyword` 枚举 = 我们的 `subtype`）。
- **中文文案也是按 id 编键的**：`d:/4/Unity/数据/游戏数据/官方本地化_en+zh-CN.json` 的 `Card_Description/SOR36` = `"[[armour]] 1. [[blast]] 4.<br>((Faith_cost_x,8)): ((Deal_x_damage_to_all_enemies,5))"`，与 `UseFaith→doDamage(5)` **逐项吻合**。⇒ 这是**第三条独立对账通道**（无需 SO，只要有 id）。

### 3·6 查不到的（如实写）

- **查不到**：任何一张卡的「专属 C# 方法」。全库按 `wolve` / `njal` / `ragnar` / `logan` 命名的方法体 = 0（`R9` 已核，本次复读 `d:/2/tools/decomp_full/` 未见反例）。
- **查不到**：`AbilityEffect` / `AbilityTrigger` 里 **30 个效果值 + 40 个触发值在旧快照里 0 次使用**（效果例：`transformUnit`、`forceAttack`、`discardFromHand`、`revealOrder`；触发例：`Courtesy`、`Survivor`、`Landing`、`Oath`、`Agenda`）—— 它们存在、代码分支也在 `PlayAbility` / `CanTriggerAbility` 里，但**没有样本卡可验证语义**。（精确清单 = 枚举全表 − 快照实际出现的 53 / 57 个。）
- **查不到**：`CardEffect`（buff 的细项）的完整枚举**没在签名桩里找到**（`CardEffect.cs` 不在 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`；`dump.cs` 里也没有同名类型）—— 它很可能是嵌套结构。**这一点没查清**，标注在此。
- 搜过的包/词：`assets_full` 全部 84 个 bundle 目录；`bundle_Waprforge_monoscripts`；`il2cpp_out/dump.cs`（42 MB）；签名桩全目录 `*CardEffect*` / `*Effect*`。

---

## 四、可行性结论与提取器设计

### 4·1 覆盖率（给可复核的判据，不拍脑袋）

先把「未知」的口径对齐（两套数字都对，但不是同一件事）：

| 口径 | 数量 | 判据 |
|---|---|---|
| `card_ids.json` 的 `mapping` 键 | **1,310** 行，其中 **1,194** 在快照里、**116** 不在 | 键是否 ∈ 快照 `uniqueId` 集合 |
| 卡池 `cards_engine.json` | **1,127** 张：**1,009** 张 id 在快照里 · **95** 张「原版格式但快照没有」· **23** 张「自造 id（带下划线）」 | `id ∈ 快照` / `id 形如 ^[A-Z]{2,4}\d+$` / `'_' in id` |
| **真正缺 id 的卡** | **118 = 95 + 23** | |

`116` 与 `118` 的差额有确凿解释：116 里有 **21 个 id 根本不在卡池**，其中 **19 个的卡名在快照里已经有别的真 id**
（例：`AM20 Astropath` 真 id `AM59` · `DA65 Grim Effigy` → `DA59` · `DA66 Plasma Generator` → `DA61` · `TL77 Neurogaunt` → `TL66` · 全表见下）；
剩下的 `116 − 21 = 95` 正好等于卡池里那 95 张。⇒ **`DA65`–`DA66`、`AM20`、`TL76`–`TL86` 这批是旧表偏移出来的假 id，不是「未知卡」。**

> 19 个假 id 的真值（可直接回填，本次只做可行性、不落库）：
> `AM20→AM59` · `AM77→AM76` · `BL76→BL75` · `BL77→BL74` · `DA65→DA59` · `DA66→DA61` · `GSC48→GSC40` · `GSC78→GSC75` · `SAU77→SAU76` · `TL76→TL65` · `TL77→TL66` · `TL78→TL67` · `TL79→TL75` · `TL80→TL68` · `TL82→TL69` · `TL83→TL70` · `TL84→TL71` · `TL85→TL72` · `TL86→TL73`
> （判据：`mapping[id]` 的卡名 == 快照里另一个 `uniqueId` 的 `cardName`，且在本表中**该前缀下唯一**。`SOR76`/`TAU76` 两条例外：卡名在快照里完全没有。）
> ⚠️ **这条只能证明「这个卡名在快照里已有真 id」，不能单独证明「旧表那个号不存在」** —— 原版理论上可能有两个同名对象（`BL76`/`BL77` 就各有一份 GUID 形态的同名对象）。
> 要坐实需要「新 id 是否被任何本地文件引用」这条旁证：这 19 个号在本机 **24 万个 JSON 里 0 命中**（见 2·2/附录 B 的同一套扫描），而它们的卡名在快照里有真 id ⇒ **作为待回填候选成立，作为定论还差一次新包核对**。

**覆盖率（两条路径，分开说）：**

| 路径 | 覆盖率 | 判据（可复核） |
|---|---|---|
| **A. 本地直读（不给新包）** | **0 / 118 = 0%** | 118 张卡在 `assets_full` 里 **0 个 `cardAbilities`**；在旧缓存 bundle 里 **0 个**（旧包 1,192 张的最高 `cardArmy` = **120**，没有 130 SpaceWolves；`expansionPack` 只到 **70**，没有更晚的包） |
| **B. 拿到当前 build 的 `allcards`** | **118 / 118 = 100%** | 旧包里 **1,192/1,192** 的 `RawCardScript` 同时有非空 `uniqueId` 与 `cardName`、**0 个重复 `uniqueId`** ⇒ id→卡名是**直接读**，不需要任何推断 |

**旁证覆盖（路径 A 唯一能拿到的东西，只是「id 出现过」，不是「id 是什么卡」）：**

| 有几种本地痕迹 | 数量 | 说明 |
|---|---|---|
| deck + draft + DT 三者都有 | 55 | 能同时拿到「稀有度 + 草稿包分组 + 出现在哪些卡组」 |
| draft + DT | 8 | |
| draft + DT + duplicateassetisolation | 4 | |
| deck + DT | 3 | |
| DT + duplicateassetisolation | 3 | |
| deck + draft | 2 | |
| 只有 deck | 1 | |
| **一个都没有** | **40** | 连 id 字符串在本地都不存在 |

> 顺带核过的两个「额外键」质量：
> · **DT → 稀有度可信**：36 个 `DT*.json` 里 267 个已知物品，`DT.cardRarity == 卡 SO.cardRarity` **267/267 全等**、`cardArmy` 也全对。
> · **draft pack 名 → 卡名**：771 个「3 张全部已知」的 pack 里，`packName` 恰好等于某张卡名的有 **218** 个，其中 **203（93%）那张卡就在本 pack 里** ⇒ 是一条**真信号**，但对 SW 的 47 个 pack **0 命中**（都是主题名，如 `Colossal Thunderwolves`，不是卡名）。
> · **draft pack 的 `packTier` 只给稀有度上界**：tier0 ⊂ {1,2}、tier1 ⊂ {1,2,3}、tier2 ⊂ {1,2,3,4}。

### 4·2 歧义来源（用数字说清为什么「光靠数值」不够）

- 1,192 张卡按 `(faction, cardType, spellType, cost, melee, ranged, hp, armour, keyword)` 去重后只剩 **689 个签名**；
  **643 张卡（54%）与至少一张别的卡共享同一个数值签名**。
- 最严重的桶：Goff 的 2 费计策（`GOF73/GOF31/GOF100/GOF71/GOF104/…`）全部 `(20, 20, 0, 2, 0,0,0,0,0)`；
  SaimHann 的 2 费计策同理。**只靠数值永远分不开。**
- 87 个 `cardName` 有重名（`The Rock` 有 4 个对象、`Cadia Stands` 2 个、`Drag it Down` 2 个）—— **名字也不是主键**。

⇒ **必须的额外区分键（按可用性排序）：**

| 键 | 来源 | 本地对 118 张可用吗 |
|---|---|---|
| **`cardImageReference`（立绘 AssetGUID）** | SO 字段 | ❌（没有 SO）。**但一旦有 SO，这一条就足够唯一**：`guid_map.tsv` 覆盖 1,190 个 id，且资产名本身带卡名（`Ork_BeastSnagga_strat_Monster Hunters`） |
| **完整效果签名**（trigger + 有序 `chosenAbility` + 数值 + 目标） | SO 的 `cardAbilities` | ❌（没有 SO） |
| **`cardRarity`** | DT 文件 | ✅ **73/116**（下表三个比例都是 **mapping 的 116 口径**；且 267/267 与 SO 一致） |
| **`expansionPack`**（扩展包） | SO 字段 | ❌；但 DT 的轮次名（R3/R4）能粗代 |
| **draft pack 分组** | draft 包 | ✅ **69/116**（弱约束，只能缩小候选集） |
| **预组卡组共现** | prebuiltdecks | ✅ **61/116**（其中 57 个出现在 `cardLibraryIds`，另 4 个只从 `deckHero.targetId` 拿到：`SW1`/`SW2`/`SW3`/`DA75`） |
| **本地化键 `Card_Name/<id>`** | `官方本地化_en+zh-CN.json` | ❌（1,065 个键里**没有 SW/UM87+/DA75+**，快照同步） |

### 4·3 提取器设计（建议的 `工具/card_id_resolver.py`）

**输入**：`uniqueId` 列表（或「全部快照里没有的 id」）。
**输出**：每个 id 一行 `{id, 卡名, 阵营, 数值, 效果签名, 证据路径, 置信度}`；置信度用 A/B/C 三档，**不许只给「大概」**。

| 步 | 读什么 | 产出 | 失败就算 C |
|---|---|---|---|
| **S0 · 载入** | 旧缓存 bundle（UnityPy）+ `cards_1195_2022时代.json` + `cards_engine.json` + `card_ids.json` + `guid_map.tsv` | 三个内存索引：`uniqueId→SO` / `cardName→id` / `id→我们卡` | bundle 读不到 ⇒ 整链降级为「只有 id 字符串」 |
| **S1 · 直接命中（A 档）** | 卡 SO 的 `uniqueId` + `cardName` | id → 卡名 + 全部数值 + `cardAbilities` | **A 档 = 本步命中，不需要任何推断** |
| **S2 · 名字反查（A 档）** | `cardName` 在 `cards_engine.json` 里唯一命中 | id → 我们池的卡 | 重名 ⇒ 转 B |
| **S3 · 旁证（B 档）** | DT 文件（稀有度/阵营）· draft pack（分组/tier）· prebuilt decks（共现）· `packName==卡名` 信号 | 候选集 + 每条证据的文件路径 | 候选 >1 ⇒ 仍 B，列出全部候选 |
| **S4 · 剩余（C 档）** | —— | **明确写「查不到」+ 列出搜过的包/词** | **⛔ 不许拿数值相近去猜**（铁律 2 / 项目红线） |

**S1 为什么是决定性的**：因为 `uniqueId` 就在 SO 上、`cardName` 也在同一个对象上 —— **不是匹配，是读**。
**S2 才是「与 `cards_engine.json` 对上」的那一步**，而且它只需要卡名（字符串），不需要数值。

**一个必须写进工具的守卫**：S2 命中后，**再用 `(cost, melee, ranged, hp, armour, cardType)` 复核一次**，不一致就降 B 并报差异 ——
这是防「同名不同卡」（87 组重名）静默错配的唯一办法。

### 4·4 风险 / 边界（哪些 id 这条路**结构上**走不通）

| 情形 | 量 | 为什么走不通 | 出路 |
|---|---|---|---|
| **卡 SO 不在本地**（当前全部 118 张） | **118 / 118** | `allcards` 是远端包；本地旧缓存是 2025-07-30 版 | ① 拿到当前 build 的 `allcards_assets_all`（判据：`catalog.bin` 里 `ea8bca4f5c6909d3124fd4975dc96173` 该文件）；② 否则只能靠旁证，**不保证唯一** |
| **id 本身是假 id**（旧表偏移） | ≥19 / 116 | 这类 id 在原版里可能**从来不存在**，拿新包也查不到 | 用 S2（卡名）反查真 id；本次已给出 19 条对应 |
| **GUID 形 uniqueId**（32 位 hex） | 259 / 1,192（22%） | **没有阵营前缀**，`UM/GOF/BL/...` 规则对它们完全无效 | 只能靠 SO 的 `cardArmy` 字段或本地化键；其中 **113 个已在我们的卡池里** |
| **重名卡** | 87 组 | 名字不是主键 | 以 `cardImageReference` GUID 或效果签名消歧 |
| **数值/效果完全相同的卡** | 643 张共享签名 | 数值不可区分 | 同上 |
| **`SOR76` / `TAU76` 这类「既不在池、名字也不在快照」** | 2 | 本地没有任何锚点 | 只能等新包 |

**另外两条要写清的边界（不是风险，是口径）：**

1. **`uniqueId` 前缀 → 阵营：规则成立，但只覆盖 78%。** 实测 933 个「前缀+数字」形态的 id **933/933 全部落在唯一阵营**（AM→AstraMilitarum 75、ASH→SaimHann 86、BL→BlackLegion 75、DA→DarkAngels 64、EC→EmperorsChildren 64、GOF→Goff 107、GSC→Genestealers 75、SAU→Sautekh 76、SOR→Sororitas 75、TAU→TauEmpire 75、TL→Leviathan 75、UM→Ultramarines 86）—— **零例外**。
   但另外 **259 个是 32 位 hex**（232 计策 / 17 单位 / 10 英雄），**前缀规则对它们不适用**。
2. **「将来新增卡复用触发代码」—— 成立，而且不需要动代码。** 新卡 = 新增一个 `RawCardScript` 资产 + 填 `cardAbilities` 数据；`AbilityEffect`/`AbilityTrigger` 是封闭枚举，现有 83+97 个值已覆盖全部行为。**只有当新卡要一种全新的行为时**才需要改 `AbilityLogic__PlayAbility` 的 switch 并扩枚举。

### 4·5 建议的三步（不越权，只给建议）

1. **先回填 19 条假 id**（4·1 那张表）—— 零成本、纯字符串、已有真值判据。（本次只做可行性，**没动任何数据**。）
2. **把「拿当前 build 的 `allcards`」重新列成一条明确待办**：本地缓存是 2025-07-30，**只差这一步，118 张就 100% 可解**。判据已经定位到 `catalog.bin` 里的 bundle 内容哈希 `ea8bca4f5c6909d3124fd4975dc96173`。
3. **提取器按 4·3 落地**（S1 直读 / S2 名字反查 / S3 旁证 / S4 明说查不到）—— **S1+S2 对现有 1,009 张是零风险的**，可以直接先把工具写出来跑通、再等新包。

---

## 附录 A · 本次用到的命令/坐标（可复跑）

```bash
# 1) 旧缓存 bundle 的类名（m_Script → MonoScript）
python -I -X utf8 -c "import UnityPy; e=UnityPy.load(r'd:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1114a2de41519cac560a4bce4/dca75913f60923172e82862460139ede/__data'); print([(o.path_id, o.read().m_ClassName) for o in e.objects if o.type.name=='MonoScript'])"
# 期望：[(8864902669260471755, 'RawCardScript'), (-3368977909917641325,'AlternateArtCard'), (-5832762458676164525,'AddressablesBuildNumber')]

# 2) 本地 24 万个 JSON 里有没有卡 SO（0 = 没有）
grep -rl '"cardAbilities"' --include=*.json d:/2/新解包资源/assets_full | wc -l

# 3) 现客户端 catalog 里的 allcards 条目
python -I -X utf8 -c "import re,io; b=io.open(r'd:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/catalog.bin','rb').read(); print(b.count(b'allcards'))"
```

## 附录 B · 5 个未知的逐条「引用文件」清单

```
SW20  ← bundle_cosmeticsso_assets_all/MonoBehaviour/DT SpaceWolves R3.json            (cardRarity=3, cardArmy=130)
      ← bundle_draftpacks_assets_all/MonoBehaviour/Colossal Thunderwolves.json       (packTier=1, cardIds=[SW41,SW6,SW20])
      ← bundle_draftpacks_assets_all/MonoBehaviour/Towering Muscles.json             (packTier=2, cardIds=[SW43,SW20,SW26])
      ← bundle_draftpacks_assets_all/MonoBehaviour/Tundra Stalkers.json              (packTier=2, cardIds=[SW12,SW20,SW54])
      ← bundle_prebuiltdecks_assets_all/MonoBehaviour/{SW_SK_1, SW_SK_3, SW_SK_Starter,
                                                       Deck_SpaceWolves_Starter, SpaceWolves_Deck1, SpaceWolves_Deck5}.json
DA78  ← （0 个文件）
UM87  ← bundle_cosmeticsso_assets_all/MonoBehaviour/DT UltraMarines R4 Expansion Only.json  (cardRarity=4, cardArmy=10)
      ← bundle_duplicateassetisolationso_assets_all/MonoBehaviour/DT Ultramarines R4.json    (cardRarity=4, cardArmy=10)
GSC48 ← （0 个文件）   ※ 卡名 "Backstab" 的真 id = GSC40
SOR76 ← （0 个文件）   ※ 卡名 "Emperors Judgement" 在快照里完全没有
```
