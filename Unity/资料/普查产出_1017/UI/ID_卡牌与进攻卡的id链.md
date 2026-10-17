# ID · 卡牌 id → 效果/数值 的链，以及进攻卡的 id

> 只读普查（2026-10-17）。判据优先级：① 原版反编译 `d:/2/tools/decomp_full/` → ② 解包/远端快照
> `d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（1,195 张全字段 `RawCardScript`）→ ③ 枚举签名桩
> `d:/2/Warpforge_code/Scripts/Assembly-CSharp/*.cs`。⛔ 未跑 Unity。

## 一、结论摘要

1. **「从触发代码逆推效果/数值」——不能（代码只给「何时」）；「顺着 id 读卡 SO 的数据」——能（这是读数据，不是逆推）。**
   - 触发代码（`RawCardScript.OnTrigger` / `TriggerAbilities` / `CardAbility.CanTriggerAbility`）是**全卡共用的门/派发器**，
     只拿一个 `trigger` 整数来筛 `cardAbilities[]`，**没有任何一张卡的效果写在代码里**（`RawCardScript__TriggerAbilities.c:108` 调
     `CardAbility__CanTriggerAbility`；`CardAbility__CanTriggerAbility.c:71` 只做 `cardAbility.trigger == param_2` 的相等判断）。
   - **效果**在数据里：`cardAbilities[].abilityLogic[].chosenAbility`（`AbilityEffect` 枚举）+ 各类 criteria 块。
   - **数值**分两处：卡本身的三围/费用在卡 SO 的 `manaCost / meleeAttack / rangeAttack / maxHealth / armour`；
     效果里的数在 `abilityLogic[]` 的 `damageCriteria / healCriteria / buffEffect / manaBuff / chanceOfHappening / multiplier` 里。
   - **边界（详见 §三）**：① 有的效果在 `activeAbility.activeAbilityLogic`（不在 `cardAbilities`）② 有的效果通过 PPtr 转到**另一张卡 SO**
     （造牌 / 召唤 / 附魔）③ 有的数只以枚举语义存在（`buffType` / `trait` / `keywordFilter`），必须查枚举表 ④ 2022 快照缺新卡、
     且至少见过一条「文本与快照结构对不上」⑤ 卡面文本是展示层，原版**不解析**它。
2. **进攻卡有 id，且与普通卡同一套字段（`RawCardScript.uniqueId`）；但它是「双 SO」结构。**
   - 基卡：短 id（UM49/UM50/UM51），`spellType=200`（`OffensiveCard`）、`cardType=20`（`Tactic`）。
   - `_enchantment` 副本：**32 位 hex GUID 形 id**（如 `b432e12e60e66a94fa032cd62011d2e1`），真正的持续效果在它身上。
   - **排序 = `EnvironmentalEffectCardsSO` 每个阵营的 `offensiveCards[]` 数组顺序（固定 3 槽）**，不是 id 序、不是卡名序、
     也不是 PnP 编号序；`forgeLevel` 4/25/45（UM 第三槽是 40）。UM 三槽 = UM50 / UM51 / UM49，直接反证「不是 id 序」。

## 二、问题一：UM 一张卡的逐跳链

### 2·1 原版链（示例 `UM83 Valtus`）

| 跳 | 从 | 到 | 字段 / 文件 | 证据 |
|---|---|---|---|---|
| 1 | `id = "UM83"` | 卡 SO | `RawCardScript.uniqueId`；`m_Name = "7 Ultramarines-Valtus"`，`cardName = "Valtus"` | 快照 `cards_1195_2022时代.json` 的 UM83 行；`uniqueId` 在 1,194 条里**无重复**（另 1 条为 null） |
| 2 | 卡 SO | **数值** | `manaCost=9 · meleeAttack=9 · rangeAttack=9 · maxHealth=9 · armour=1` | 同上；`Card_Description/UM83` = “[[armour]] 1. When a friendly unit attacks, deal 3 damage to the target of the attack” |
| 3 | 卡 SO | 触发门 | `cardAbilities[0].trigger = 50`（`AbilityTrigger.UnitAttack`） | 同上；`AbilityTrigger.cs:14` `UnitAttack = 50` |
| 4 | 触发门 | 效果类型 | `cardAbilities[0].abilityLogic[0].chosenAbility = 10`（`AbilityEffect.doDamage`） | `AbilityEffect.cs:5` `doDamage = 10` |
| 5 | 效果类型 | 效果数值 | `damageCriteria = {minDamage:3, maxDamage:3, damageType:0}`；`targetCriteria.targetsAffected = 30`（`target`） | 快照同一条 `abilityLogic[0]`；`TargetsAffected.cs` `target = 30` |
| 6 | 触发时机 | 代码入口 | 友方单位被攻击结算时 `CardScript__ResolveUnitAttacked` 发 `OnTrigger(..., 0x32, ...)`（0x32 = 50） | `d:/2/tools/decomp_full/CardScript__ResolveUnitAttacked.c:29` |
| 7 | 代码入口 | 通用派发 | `OnTrigger` → `TriggerAbilities` → 逐条 `CardAbility.CanTriggerAbility` 比 `trigger` → `AbilityLogic.PlayAbility` 按 `chosenAbility` 分派 | `RawCardScript__OnTrigger.c:15` · `RawCardScript__TriggerAbilities.c:108` · `CardAbility__CanTriggerAbility.c:71` · `AbilityLogic__PlayAbility.c:286`（`iVar8 = *(int*)(param_1+0x10)`）→ `:293/368/588…`（0xa5=165 / 0xaf=175 / 0xa0=160，全是 `AbilityEffect` 值） |
| 8 | 效果数值 | 玩家看到的字 | `Card_Description/<uniqueId>`（I2 词条）；**展示层，不参与结算** | `官方本地化_en+zh-CN.json` 的 `Card_Description/UM83`；原版不解析 desc 的判据见 §三 |

> 🔴 **`trigger` 只是门**：UM83 的「什么时候」= `UnitAttack(50)`；「做什么、多少」= `doDamage(10) + 3/3 + target`。
> 只读代码（不读卡 SO）只能得到「会调用 50 号门」，得不到 3 点伤害、也得不到「打攻击目标」。

### 2·2 第二例：一张卡多条能力 + PPtr 链（`UM12 Antaro Chronus`）

| 能力 | `trigger` | 数据 | 读出来的效果 |
|---|---|---|---|
| A | `46`（`Talent`） | `chosenAbility = 100`（`createCardInHand`）+ `summonCriteria.unitsToSummon[0].m_PathID = 8456088793087619101` | 造一张卡；该 PathID 解析为 `Z-NC UM-Spear of Macragge`，`uniqueId = d94f91051ecfe014f97577ff573571aa`（`Card_Description/d94f…` = “[[ephemeral]] / Draw a Vehicle and give it [[armour]] 2”） |
| B | `190`（`OtherUnitSummoned`） | `actingCardCriteria.keywordFilter = 10`（`Keyword.Vehicle`）；`chosenAbility = 30`（`buffOthers`）；`buffEffect.buffType = 3`（`addTrait`）· `trait = 460`（`DefinedTrait.flank`） | “When you deploy a Vehicle, give it Flank”（与卡面一致） |

- PPtr 解析判据：用 UnityPy 读原版卡包 `d:/2/远端缓存_旧profile_1013/Unity缓存_按哈希/54bdb7b1…/…/__data`（1,195 个 MonoBehaviour，PathID 唯一）。
- 枚举表：`Keyword.cs` `Vehicle = 10`；`BuffType.cs` `addTrait = 3`；`DefinedTrait.cs` `flank = 460`。
- ⚠️ **若只看 UM12 这一张卡，你只知道「造一张卡」，不知道那张卡做什么** —— 必须顺着 PPtr 再读一张 SO。

### 2·3 我们这边的链（`UM83 Valtus`）

| 跳 | 从 | 到 | 字段 / 文件 |
|---|---|---|---|
| 1 | `id = "UM83"` | 我们卡表行 | `cards_engine.json` 的 `id`（`CardDef.Id`，牌组/存档/联机/卡面缓存都以它认牌） |
| 2 | 卡表行 | 数值 | `cost=9 · attack=9 · health=9 · ranged=9 · armour=1`（`CardDatabase.cs:207-330` 逐列塞进 `CardDef`） |
| 3 | 卡表行 | 效果文本 | `desc = "Armour 1. When a friendly unit attacks, deal 3 damage to the target of the attack"` |
| 4 | 效果文本 | 结构化 op | `CardDef` 构造里 `CollectTriggerOps`（`CardDef.cs:386`）→ `EffectText.Parse`（`EffectText.cs:1144`）；`EffectText.cs:759` 明确引 `AbilityTrigger.UnitAttack=50`、`:8194` 逐字点名 Valtus |
| 5 | 结算 | `EffectOp` | `desc` 是**我们唯一的原版效果输入**；原版 `cardAbilities` 在我们的普通卡路径上**不参与结算**（以下 §五 的 3 张桥例外） |

## 三、能不能从触发代码逆推 —— 效果 / 数值 分开判

### 3·1 触发代码是什么
- `RawCardScript.OnTrigger` 是**通用入口**（`RawCardScript__OnTrigger.c:15` 直接转 `TriggerAbilities(param_2)`）。
- `TriggerAbilities` 遍历 `RawCardScript +0x290` 的 `cardAbilities[]`（`RawCardScript__TriggerAbilities.c:47`），逐条调 `CardAbility.CanTriggerAbility`。
- `CanTriggerAbility` **第一句**就是 `if (cardAbility.trigger != param_2) return 0;`（`CardAbility__CanTriggerAbility.c:71`）。
- `HasPlayedThirdCardInTurn` 就是这条判据的谓词版：遍历 `+0x290` 的列表，命中 `cardAbilities[].trigger == 0xf` 就返回真（`RawCardScript__HasPlayedThirdCardInTurn.c:32`）。
- ⇒ **它是门，不是效果本身**；同一段代码服务所有卡。

### 3·2 效果
- ✅ **能读到**：`abilityLogic[].chosenAbility` 就是效果枚举（`AbilityEffect.cs` 现读 83 个值），配合 `targetCriteria` 读出「对谁」。
- ✅ **一个卡可以有多条**：`cardAbilities[]` 逐条；一条 `abilityLogic[]` 还可以有多条（`playersAffected` / `chanceOfHappening` 每条独立）。
- ⚠️ **两处容器**：`cardAbilities[].abilityLogic[]`（触发式）与 `activeAbility.activeAbilityLogic[]`（主动/Duty 式）。
  实测 `AM35 Leman Russ Vanquisher`：`cardAbilities = []`，但 `activeAbilityLogic` 有两条 `doDamage`（12 / 8）——
  **只扫 `cardAbilities` 会把它误判成「没有效果」**（快照 AM35 行）。

### 3·3 数值
- ✅ 卡本身：`manaCost / meleeAttack / rangeAttack / maxHealth / armour`（直接字段）。
- ✅ 效果数：`damageCriteria.minDamage/maxDamage` · `healCriteria` · `buffEffect.*Change` · `manaBuff.manaAmount` ·
  `chanceOfHappening`（正例：`Z-UM-Orbital Bombardment_enchantment` 的 `chanceOfHappening=10` + `damageCriteria=3`，与
  `Card_Description/UM50` 的「10% 几率所有单位受 3 点」逐字对上）。
- 🔴 **推不出的边界（反例）**：
  1. **数不在这一条记录里**：`GOF99 Skrag Every Stash!` 两条 `abilityLogic` 都是 `chosenAbility=30`（buffOthers），
     `buffEffect.buffType=3/4`（addTrait/removeTrait）、`trait=980/70`、`traitValue=0`；**没有 `minDamage`**。
     卡面/我们的 `desc` 写 “Deal 1 damage to all enemies…” ⇒ 这个 `1` **不能从该记录推出**（记录里没有这个数）。
     （⚠️ 快照是 2025-07-30 的旧 build；本报告只说「这份数据里推不出」，不断言当前 build。）
  2. **数在另一张 SO**：UM12 的造牌、GOF52 的 `summonCriteria.unitsToSummon`（重复两次的同一 PathID）、进攻卡的
     `buffEffect.enchantment` PPtr —— 不跟引用就只知道「造/召/附魔」，不知道数值。
  3. **枚举语义**：`trait`/`buffType`/`keywordFilter` 的数值要查 `DefinedTrait.cs`/`BuffType.cs`/`Keyword.cs`；
     查错表会「看着对、实际错」（例：`trait=460` 只有查表才知道是 `flank`）。
  4. **展示文本不是数据**：原版 `desc` 只做字符串拼接、从不解析（`EffectText.cs:20-58` 文件头；正面判据
     `RawCardScript__GetDescAndAbilityRaw.c:8-33`）。因此「文本里写的数」可能来自别的 SO，甚至与旧快照不符（GOF99）。
  5. **快照缺卡**：UM 有 37 张（按 `cardName`/`m_Name` 精确名）在 1,195 张快照里查不到（`Guilliman`/`Bladeguard Ancient`/
     `Indomitus`/`Stormhawk` 等子串也 0 命中）⇒ 这些卡本机**没有结构化 ability 数据**，只能走文本。
- **结论**：**效果/数值「能」从卡 SO 数据逐字段读出；「不能」从触发代码本身逆推。** 代码给时机与派发，数据给内容与数字。

## 四、问题二：进攻卡的 id 与排序

### 4·1 它是什么
- 进攻卡也是 `RawCardScript`：`spellType = 200`（`SpellType.OffensiveCard`，`SpellType.cs:24`）、`cardType = 20`（`CardTypeOptions.Tactic`）。
- **双 SO**：
  - **基卡**：`Z-<Army>-<名>`，短 id（UM50 等），能力是 `trigger=0`（ThisCardPlayed）→ `chosenAbility=30`（buffOthers）→
    `buffEffect.buffType=12`（`enchantment`）→ `buffEffect.enchantment` PPtr。
  - **`_enchantment` 副本**：`Z-<Army>-<名>_enchantment`，**32 位 hex GUID id**；真正的每回合效果在它身上。
- 绑定表 = `EnvironmentalEffectCardsSO`（`bundle_duplicateassetisolationso_assets_all`）：每阵营
  `{army, emptyOffensive, defaultEnviromentalEffectVFX, offensiveCards[3], defensiveCards[3]}`；
  `offensiveCards[i] = {offensiveCard(PPtr), forgeLevel, enviromentalEffectVFX(GUID)}`（`gen_offensive_cards.py:13-14`）。

### 4·2 它有没有 id、是哪种 id
- **有**，就是 `RawCardScript.uniqueId`（与普通卡同一字段、同一命名空间）。全快照 `spellType==200` 共 **84** 条：
  基卡 37（36 短 id + 1 GUID）、`_enchantment` 33（全 GUID）、`Normal Conditions` 14（全 GUID）。
- 这些 id 都在 `card_ids.json` 的 `mapping` 里（如 `UM50→Orbital Bombardment`、`b432e…→Orbital Bombardment`），
  但**不在我们的 `cards_engine.json` 卡池**：它们在 PnP 源表里是 `hasStats=false` 的噪音行（无卡面/无三围），
  被 `gen_cards_engine.py:1677-1681` 整条跳过（`Orbital Bombardment`/`Thunderstorm`/`Dawn Attack` 等行 `cost/atk/hp` 全 null、`ocrSrc` null）。

### 4·3 怎么排的（证据）
- **每个阵营一个 `enviromentalEffects[]` 条目（按 `army` 枚举），条目内 `offensiveCards[]` 固定 3 槽**；
  `gen_offensive_cards.py:338/510-515` 用 `enumerate(...)` 写 `slot = i+1`；我们的扁平表 `gen_offensive_cards_flat.py:24-26`
  写 `idx = i`（0/1/2）。
- **不是 id 序**（UM 实测）：slot1=`Z-UM-Orbital Bombardment`（UM50）· slot2=`Z-UM-Thunderstorm`（UM51）·
  slot3=`Z-UM-Aerial Clash`（UM49）。其它阵营同样跳号：Goff 50/51/54、SaimHann 59/58/60、Tau 59/61/60、Leviathan 58/57/59。
- **不是卡名序、不是 PnP 编号序**：`offensive_cards.json` 的 `notes` 明写「卡槽顺序 ≠ PnP 编号顺序」。
- **与 `forgeLevel` 同向**：13 阵营都是 4/25/45；UM 第三槽原版就是 **40**（`gen_offensive_cards.py:604` 的 note）。
- **PPtr 解析实测**：用 UnityPy 读原版卡包，`EnvironmentalEffectCardsSO` 的 52 个 PPtr（13×3 + 13 空卡）**解析出 48 个**；
  未解析的 4 个**全是 SpaceWolves（army 130）**—— 该阵营是快照之后新增的，1,195 张里没有它的卡（只搜过这份卡包与这份快照）。
- 解析结果全部是**基卡**（不是 `_enchantment`）：UM slot1 → path `8694475542261297809` → `Z-UM-Orbital Bombardment`（UM50）。

### 4·4 我们现状
- `CardPresentation/Resources/OffensiveCards.json`：每卡**没有 id 字段**，字段 = `idx / name / nameFrom / envSO / prefab / face / forgeLevel`；
  键是 `envSO`（消费件 `CardPresentation/Core/OffensiveCards.cs`，`Choices()` 给它前面插空卡）。
- `数据/游戏数据/offensive_cards.json`：源表带 `cardRef`（原始 PPtr）但 `cardName` **一律 null**（PPtr 在外部文件、旧导出没有 externals 表）
  ⇒ 卡名是「SO 名/贴图名」反推的**候选**（`cardName_candidate_src` 标明来路）。
- `RuleEngine/Resources/offensive_card_abilities.json`：键 = `envSO`，值里有 `soName/uniqueId/name/nameZh/abilities`；
  只收 `trigger==15`（`PlayedThirdCardInTurn`）的两张（Asteroid Zone / Empyric Rift，`OffensiveCardData.cs:4-23`）。
  ⚠️ 已知：`offensive_cards.json` 的 `_fields` **没有 `trigger`** ⇒ 过去「`trigger` 0 命中」的论据是零证据力。

### 4·5 与问题一的关系
- **同一条「id → 卡 SO → cardAbilities → abilityLogic」链**，同一个 `uniqueId` 命名空间、同一套枚举。
- **差一跳**：普通卡的效果就在自己 SO 上；进攻卡的基卡只有「打出一张附魔」，
  真正效果在 `buffEffect.enchantment` PPtr 指向的 `_enchantment` SO 上。示例（Orbital Bombardment）：
  `UM50` → `buffType=12/enchantment` → `b432e12e60e66a94fa032cd62011d2e1` → `trigger=30`（TurnStart）+
  `chosenAbility=10` + `chanceOfHappening=10` + `damageCriteria=3/3` + `targetsAffected=10`（allFiltered，双方英雄+小兵）。
- 另外它**不进卡组**：基卡 `inventoryOptions=5`（`CantAddToDeck`）、`showInCollectionOptions=5`（Show）；
  `_enchantment` 是 `inventoryOptions=10`（NotInInventory）、`show=0`（Hide）。

## 五、我们的现状（字段名逐条 + 现在按哪个键连）

- **`cards_engine.json`**：`version=9 · count=1127`。字段（现读第一张全列）：
  `id · name · type · cost · attack · health · ranged · keywords · desc · faction · rarity · subtype · armour · cardTraits ·
  inventoryOptions · showInCollectionOptions · initiative · attackDamageType · cardShortId · useOverDraw ·
  cantBeRandomlyCreated · expansionPack · refNameId · itemDescriptionKey · fullHeroSpriteReference · eventSpriteName ·
  targeted · nameZh · descZh`（督军另有 `goesSecond`）。
- **`card_ids.json` 的 `mapping` 怎么来**（`card_ids.json` 的 `note`）：2026-10-13 由抢救出的原版远端包 `allcards`
  的 **`uniqueId → cardName`** 整表换成真值（935 条覆盖、853 条替换）；2026-10-14 把 259 条 32 位 GUID 形 `uniqueId` 也收进来。
  现读 1,310 行 = 1,049 条「前缀+数字」 + 259 条 GUID + 2 条 `#HB_` 标记；其中「前缀+数字」= 933 条真值 + 116 条旧表残留。
- **我们按哪个键连**：**`CardDef.Id` = `cards_engine.json` 的 `id`**（牌组/存档/联机/缓存都用它）。
  `id` 由 `gen_cards_engine.py` 三路取：① `card_id_truth_index.json`（真值索引，`gen_cards_engine.py:871`）→
  ② `card_ids.json` 按卡名反查、按阵营前缀挑（`:850`）→ ③ 自造 `前缀_卡名`（`:830`）。
  **数值**取同一条卡表行；**效果**取 `desc`（`EffectText.Parse`），`keywords` 里带触发前缀的条目兜底（`CardDef.cs:761-772`）。
- **数据来源分层**：`cards_engine.json` 的基础是 `card_stats.json`（`gen_cards_engine.py:30`），后者的 `desc`/三围来自 **PnP 成品卡图 OCR**
  （每行有 `ocrSrc` 指向 `d:/2/Warpforge部队卡片/…png`，如 UM83 → `Warpforge_07_Valtus-2.png`）；真值快照只补 PnP 不印/不可靠的列
  （稀有度/inventory/initiative/cardTraits/护甲兜底等，`gen_cards_engine.py:807` 的 `TRUTH_CARDS_SRC`）。
- **当前池的 id 形态**（现读 1,127 张）：`前缀+数字` 991 · `32hex GUID` 113 · 自造（带下划线）23。
  其中**在真值索引 `base` 里的 1,003 张**，不在的 124 张 = 100 条「短 id 但旧表残留」+ 23 条自造 + 1 条 GUID（`e59ac9e4…`，不在 `base`）。
  **UM 134 张**：真值 96（83 短 + 13 GUID）· 短 id 旧表残留 15（UM87/88/89/90/91/92/93/94/95/96/97/98/99/100/101）· 自造 23。
  ⇒ **「前缀+数字」不等于「原版真值」**；UM87–UM101 这一批是快照之后的新卡，本机没有它们的原版 SO。
- **`cardShortId` 不是 id**：快照 1,195 条里只有 862 个不同值、291 个 0、37 个重复；我们只把它当一列数据（UM83=473）。
- **普通卡路径不用 `cardAbilities`**；但有 3 张**真值桥**确实用了原版结构化数据：
  ① `talent_cards.json`（89 条，键 = 我们卡池 id，值 = 原版 `trigger==46` 的 `unitsToSummon` 候选；UM12 → `d94f…`，
     `TalentCardData.cs:8-11`）② `offensive_card_abilities.json`（键 = `envSO`）③ `related_cards.json`（原版 `relatedCard1..4`）。
  除此之外，效果一律走 `desc` 文本解析。

## 六、还没查清的（⛔ 不猜）

1. **2022 快照 vs 当前 build 的逐卡一致性未核**：目前只证明「结构可读」；至少见到一条文本/数据对不上（GOF99）。
   要逐卡判「快照结构 = 当前 build」需要当前 build 的卡 SO（本机没有；远端 CCD 已关）。
2. **GOF99 那 1 点伤害在当前 build 到底怎么编码**（trait 980 的运行时语义？还是文本过期？）—— 未查清。
   查过：快照 GOF99 全部 `abilityLogic` 字段、`BuffType.cs`、`DefinedTrait.cs`；记录里没有伤害数字。
3. **每卡是否有 `CustomAbilityLogic` 资产**：`d:/2/tools/decomp_full/CustomAbilityLogic__PlayAbility.c` 存在，
   但快照的 124 个字段里没有可读的「每卡自定义逻辑」引用位；未逐卡核 assets，**不下结论**。
4. **SpaceWolves 的 3 张进攻卡 + 空卡**：`EnvironmentalEffectCardsSO` 的 4 个 PPtr 在 2022 卡包里解析不到（该阵营不在快照）；
   它们的 id / 效果本机**查不到**。搜过：该卡包全部 1,195 个 MonoBehaviour 的 PathID、快照的 `cardName`/`m_Name`。
5. **`_enchantment` 与基卡的 33 组对应是否 1:1 全对**：本报告只逐跳核了 UM 三组（Orbital Bombardment / Thunderstorm / Aerial Clash）
   与两张 `trigger==15` 的进攻卡；其余阵营未逐张核 PPtr。
