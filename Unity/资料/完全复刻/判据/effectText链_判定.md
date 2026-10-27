# `effectText` 链 判定（`abilityLogic[].buffEffect.effectText` → `Card_Abilities/<键>` → 卡面「受到以下影响」右列）

> 查判据代理 **W73EFFT** · 2026-10-27 · **只读**（未改任何 `.cs` / `.py` / `.json` 数据 / 账 / 资产）
> 数据源：`d:/4/_tmp_view/allcards_export/cards_raw.json`（现 build 的 allcards，**1,351 张卡 SO**，`full_raw` = 逐对象 typetree）
> 官方表：`MyGame/Assets/CardPresentation/Resources/official_loc_13lang.json`（**列式**：`keys` / `en` / `zh`，现读 **10,862** 键）
> 反编译：`d:/2/tools/decomp_full/`（`.c` 为第一权威）+ `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（交叉）
> 复现脚本：`d:/4/_tmp_view/w73/*.py`（全部 `python -I -X utf8` 跑，只读）

> 🔴 **本文与三份既有判据的分工（⛔ 别重复查，也别打架）**
> · `判据/CardEffectsToShow_判据.md` + `判据/GetCardEffectsToShow_施工细则.md` —— **项源 / 过滤 / 序 / 5 槽**那半边（`+0x108` 列表哪些条进表）。**本文不重推**，只交叉引用。
> · 本文 = **文本那半边**：哪一格文本、走哪个键、键在不在、我们缺什么。
> · `判据/activeAbilityTextReferenceId_真值.md` §三 已把 **31 个 `effectText` 值各自归属哪张卡**列过一遍（那是对的，本文 §二 与它逐条一致，并补齐了「同卡多挂点」与 buffType/时窗/附魔引用）。

---

## §一 字段与链

### 1·1 字段声明（`dump.cs` 亲读）

| 字段 | 归属类 | 偏移 | 声明 |
|---|---|---|---|
| `buffEffect` | `AbilityLogic` | `+0x50` | `public CardEffect buffEffect;`（`dump.cs:19471`；`[Tooltip("Buff Self & Others")]`） |
| `handBuff` | `AbilityLogic` | `+0x60` | `public HandEffect handBuff;`（`dump.cs:19473`；`[Tooltip("BuffHand")]`） |
| `cardEffect` | `HandEffect` | —— | `HandEffect.cardEffect`（`compact_hand_buff` 取的就是它） |
| `effectName` | `CardEffect` | `+0x20` | `public string effectName;`（`dump.cs:119067`；`[Tooltip("Leave empty to use name of card")]`） |
| **`effectText`** | **`CardEffect`** | **`+0x28`** | **`public string effectText;`**（`dump.cs:119068`） |

> ⚠️ **同一个 `+0x28` 在两处出现，语义不同，别混**：
> · `CardEffect +0x28` = **`effectText`（string，卡 SO 里的键后缀）**
> · `CardEffectItem +0x28` = **`effectText`（`TextMeshProUGUI`，屏幕上那一格的标签）**
> （`dump.cs:34109` `public TextMeshProUGUI effectText; // 0x28`）
> ⇒ `GameStaticData.GetEffectDesc` 读的是**前者**（`GameStaticData__GetEffectDesc.c:64`），`CardEffectItem.LoadEffect` 写的是**后者**（`CardEffectItem__LoadEffect.c:107/153` 的 `plVar6 = *(param_1 + 0x28)`）。

### 1·2 完整链（逐段，带 `文件:行号`）

```
卡 SO   abilityLogic[j].buffEffect.effectText            (或 handBuff.cardEffect.effectText)
  ↓ 战斗期：AbilityLogic.PlayAbility 造一份运行期副本
CardEffect 实例（含 effectText）
  ↓ 挂到某张卡的 +0x108（附魔族）/ 走 AddHandEffect（handBuff）
EntityScript.activeEffects  // +0x108   List<CardEffect>
  ↓ 玩家点开这张卡的展示窗
CardDisplayWindow.ShowBattleCard → DisplayCardEffects → GetEffectsToDisplay
  ↓ 每个槽
CardEffectItem.LoadEffect(effect, isPlayer)
  ↓ 右列文本
GameStaticData.GetEffectDesc(effect)
  ↓
term = "Card_Abilities/" + effect.effectText
LocalizationManager.GetTranslation(term) → SupportMethods.ModifyLocalization
  ↓ 取不到
return effect.effectText          ← 【原版把 id 原样印到屏幕上】
```

逐段出处：

| # | 事 | 文件:行号 |
|---|---|---|
| ① | `buffEffect` 造运行期副本 | `AbilityLogic__PlayAbility.c:837` / `:917` / `:1187` / `:1337` / `:2341` —— `CardEffect__GetNewInstanceOfThisEffect(*(this + 0x50))` |
| ② | `handBuff` 造副本 + **置 `isHandBuff = true`** | `AbilityLogic__PlayAbility.c:1393-1394`（取 `+0x60` 副本）· **`:1405` `*(*(lVar11+0x10) + 0xa8) = 1;`**（= `isHandBuff`；🔴 **ILSpy 丢了这处 `= true` 写点，只有 `.c` 有**） |
| ③ | handBuff 交给战斗管理器 | `AbilityLogic__PlayAbility.c:1414` / `:1428` —— `BattleManager__AddHandEffect(...)` |
| ④ | `buffEffect` 挂到目标卡 | `CardScript__AddEffect` 里读 `+0x108` 并追加（`CardScript__AddEffect.c:107` 起）· 附魔三档（12/15/16）走 `AddEnchantment`（`Enchantment.cs` 文件头已逐行核过） |
| ⑤ | 展示窗入口 | `CardDisplayWindow__ShowBattleCard.c:28` —— `DisplayCardEffects(param_1, param_2)` |
| ⑥ | 拿列表 | `CardDisplayWindow__DisplayCardEffects.c:34` —— `EntityScript__GetCardEffectsToShow(card)`；后者遍历 `+0x108`（`EntityScript__GetCardEffectsToShow.c:34`） |
| ⑦ | 逐槽装载 | `CardDisplayWindow__DisplayCardEffects.c:69-72` —— `CardEffectItem__Enable` → `CardEffectItem__LoadEffect(slot, effect, isPlayer)` |
| ⑧ | **右列取文本** | **`CardEffectItem__LoadEffect.c:107`**（普通支）· **`:153`**（buffType ∈ {8 markOfChaos, 17 lastMarkOfChaos} 那一支走完自己的拼串后落到这里）—— 都是 `GameStaticData__GetEffectDesc(effect)` |
| ⑨ | 左列「谁给的」 | `CardEffectItem__LoadEffect.c:116`（`GetEffectName`，effectName 非空时）· `:135`（否则用 `enchantingCard.rawCardData` 的卡名）· 两者都空 ⇒ 左列写空串（**`:127`** `set_text(DAT_1842b80e0)` = `""`） |
| ⑩ | **键拼法 + 缺键行为** | **`GameStaticData__GetEffectDesc.c:64`**（`IsNullOrEmpty(effect+0x28)`）→ **`:66`** `Concat("Card_Abilities/", effectText)` → `:75` `ModifyLocalization` → `:76` 判空 → **`:80` `return *(param_1 + 0x28)`（= 原样印 id）** |

`"Card_Abilities/"` 这个前缀常量**已解析**（不是推测）：
`DAT_1842cd940` → RVA `0x42CD940` → `stringliteral.json` 里就是 `"Card_Abilities/"`。
（同族：`0x42CDA40` = `"Card_Abilities/Ability_Name_"`（技能名，批 A）· `0x42CDB40` = `"Card_Abilities/GenericSiegeText"`（常量）· `0x42CDC40` = `"Card_Description/"`（卡面正文，批 A）· `0x42CDE40` = `"Card_Name/"`（`GetEffectName` 用））

### 1·3 「什么时候显示」—— 两条挂点的差别**只在施加对象，不在显示口径**

- **两者最终都变成一张卡身上的 `CardEffect` 记录**，进 `EntityScript.activeEffects // +0x108`，**显示路径完全相同**（同一个 `CardEffectItem.LoadEffect`）。
- **`buffEffect`**：作用于能力的**目标**（`targetCriteria` 选出来的那些卡）—— 挂在**棋盘上的卡**身上。
- **`handBuff.cardEffect`**：作用于**手牌**（`HandEffect` 自带 `targetCriteria` + `playersAffected`，走 `BattleManager.AddHandEffect`）—— 挂在**手牌卡**身上。
  ⚠️ **手牌卡被展示时原版会不会印这张表**：`CardDisplayWindow` 的两条正规入口是 `ShowCard`（收藏/手牌，吃 `RawCardScript`）与 `ShowBattleCard`（吃 `CardScript`）；`DisplayCardEffects` **只被 `ShowBattleCard` 调**（`CardDisplayWindow__ShowBattleCard.c:28`，全树唯一调用点）。⇒ **手牌那张卡正常打开时不出效果表**；它的 `handBuff` 要等到那张卡**上场之后**、且记录仍挂着时才会被看到。
  （我们 `BattleDriver.cs:13059-13069` 的注释已记过同一条，**本次复核仍成立**。）
- **同一张卡的同一处 `(ab, lg)` 可以两边都有**：`TAU51` 的 `abilityLogic[0]` 同时有 `buffEffect.effectText` 与 `handBuff.cardEffect.effectText`（都是 `SaviorProtocolsEffect`）⇒ **数据列必须能表达「同一个 (ab,lg) 有两条」**（本文 §五 的列形状按 `src` 区分）。

---

## §二 逐卡逐挂点真值表（**全表 38 行，一次列全**）

口径：只算 **`cardAbilities[i].abilityLogic[j]`** 这两处；`activeAbility` / `alternativeAbility` / `*ScoringCriteria` **实测 0 条非空**（见 §二·2 覆盖率）。
"附魔?" 列 = 该 `CardEffect.enchantment`（`+0x88`）PPtr 是否非空。

| # | 卡 id | 卡名 | 挂点路径 | `effectText` | buffType | trait | 时窗 | 附魔? |
|---|---|---|---|---|---|---|---|---|
| 1 | `61046837c330b48978b4af5201230b6e` | Helspear Assault | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectHelspear` | 15 singleUseEnchantment | 0 | 本回合结束撤（untilEndOfTurn=1） | 是 |
| 2 | `6899b199f5e36144ebbdd7ed9d9e7afe` | Uge Choppa | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectUgeChoppa` | 15 singleUseEnchantment | 0 | 本回合结束撤（untilEndOfTurn=1） | 是 |
| 3 | `84d4d192e0f57a34a90ac0f6172c3358` | Ferocious Rage | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectFerocious` | 15 singleUseEnchantment | 0 | 本回合结束撤（untilEndOfTurn=1） | 是 |
| 4 | `9176318eaf469354e858f51af2333aa3` | Litany of Despair | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectLitanyOfDispair` | 15 singleUseEnchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 5 | `AM51` | Cadia Stands | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectCadiaStands` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 6 | `ASH72` | Graceful Avoidance | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectGracefulAvoidance` | 15 singleUseEnchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 7 | `BL75` | Idolatrous Despoilers | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectIdolatrous` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 8 | `DA55` | Wages of Retribution | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectWagesRetribution` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 9 | `DA74` | Hunters of Heretics | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectHuntersHeretics` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 10 | `EC38` | Pledge to the Dark Prince | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectPledge` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 11 | `EC51` | Coterie of the Conceited | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectCoetrie` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 12 | `GOF106` | Power of the Waaagh! | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectPowerWaaagh` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 13 | `GOF71` | Krump da Gitz | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectKrump` | 12 enchantment | 0 | 本回合结束撤（untilEndOfTurn=1） | 是 |
| 14 | `GOF91` | Beastboss on Squigosaur | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectBeastBoss` | 15 singleUseEnchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 15 | `GSC45` | Underground Network | `cardAbilities[0].abilityLogic[1].handBuff.cardEffect` | `EffectUndergroudndNetwork` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 16 | `GSC51` | Living Icon | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectLivingIcon` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 17 | `GSC51` | Living Icon | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectLivingIcon` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 18 | `GSC53` | Enhanced Aggression | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectEnhancedAggression` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 19 | `GSC55` | Eve of the Uprising | `cardAbilities[0].abilityLogic[2].buffEffect` | `EffectDisplayCardsAddToDeck` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 20 | `SAU53` | Undying Legions | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectUndyingLegions` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 21 | `SAU55` | Immortal Pride | `cardAbilities[0].abilityLogic[2].handBuff.cardEffect` | `EffectImmortalPride` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 22 | `SOR57` | Hallowed Martyrs | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectHallowed` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 23 | `SW36` | Bjorn the Fell-Handed | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectFellhanded` | 3 addTrait | 1270 | 无 until* 位（战斗内常驻） | 否 |
| 24 | `SW40` | Bjorn's Shrine | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectBjornShrine` | 3 addTrait | 1270 | 本回合结束撤（untilEndOfTurn=1） | 否 |
| 25 | `SW46` | Raid Tactics | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectRaidTactics` | 15 singleUseEnchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 26 | `TAU42` | Experimental Drones | `cardAbilities[0].abilityLogic[0].buffEffect` | `ExperimentalDronesEffect` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 27 | `TAU51` | Saviour Protocols | `cardAbilities[0].abilityLogic[0].buffEffect` | `SaviorProtocolsEffect` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 否 |
| 28 | `TAU51` | Saviour Protocols | `cardAbilities[0].abilityLogic[0].handBuff.cardEffect` | `SaviorProtocolsEffect` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 29 | `TAU51` | Saviour Protocols | `cardAbilities[0].abilityLogic[1].buffEffect` | `SaviorProtocolsEffect` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 30 | `TAU51` | Saviour Protocols | `cardAbilities[0].abilityLogic[1].handBuff.cardEffect` | `SaviorProtocolsEffect` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 31 | `TAU51` | Saviour Protocols | `cardAbilities[0].abilityLogic[2].handBuff.cardEffect` | `SaviorProtocolsEffect` | 2 changeCost | 0 | 无 until* 位（战斗内常驻） | 否 |
| 32 | `TL47` | Hive Fleet Leviathan | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectHiveFleet` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 33 | `TL47` | Hive Fleet Leviathan | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectHiveFleet` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 34 | `TL47` | Hive Fleet Leviathan | `cardAbilities[0].abilityLogic[2].buffEffect` | `EffectHiveFleet` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 35 | `UM113` | Duty's End | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectDutysEnd` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 36 | `UM44` | Armoured Support | `cardAbilities[0].abilityLogic[0].buffEffect` | `EffectArmouredSupport` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 37 | `UM73` | Oath of Moment | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectOath` | 12 enchantment | 0 | 无 until* 位（战斗内常驻） | 是 |
| 38 | `c433e3dde4952cc448220908226127ae` | Master Outrider | `cardAbilities[0].abilityLogic[1].buffEffect` | `EffectMasterOutrider` | 15 singleUseEnchantment | 0 | 本回合结束撤（untilEndOfTurn=1） | 是 |

### 2·1 分组小计（现读，可复现）

| 维度 | 分布 |
|---|---|
| 挂点 | `buffEffect` **33** · `handBuff.cardEffect` **5** |
| buffType | `12 enchantment` **22** · `15 singleUseEnchantment` **8** · `2 changeCost` **6** · `3 addTrait` **2** |
| 时窗 | `untilEndOfTurn=1` **6** · 无 until* 位 **32** |
| 附魔引用（`+0x88` 非空） | **29** / 38（缺的 9 条 = 6×`changeCost` + 2×`addTrait` + 1×`enchantment 但无附魔卡`（`TAU51` `abilityLogic[0].buffEffect`）） |
| 卡 | **31 张**（`GSC51` 2 处 · `TAU51` 5 处 · `TL47` 3 处，其余各 1 处） |
| 池内 | **31 / 31 全在池**（按 `cards_engine.json` 的 `id` 逐张命中；其中 **5 张是 GUID 形态 id**（`61046837…` / `6899b199…` / `84d4d192…` / `9176318e…` / `c433e3dd…`），其余 26 张是短码 id） |

### 2·2 覆盖率（为什么只有这 38 条）

单树（`full_raw`，**不是**递归两份）扫「同时有 `effectText`/`buffType`/`extrinsic` 三键」的对象 = **8,620 个 `CardEffect` 实例**，其中 `effectText` 非空 = **38**。分布：

| 容器 | 非空 `effectText` |
|---|---|
| `full_raw.cardAbilities[].abilityLogic[].buffEffect` | **33** |
| `full_raw.cardAbilities[].abilityLogic[].handBuff.cardEffect` | **5** |
| `full_raw.activeAbility.activeAbilityLogic[]` | **0**（204 个实例全空） |
| `full_raw.alternativeAbility.*` | **0** |
| `full_raw.attackScoringCriteria` / `nonTargetScoringCriteria[]` / `cardTargetScoringCriteria[]`（含 `activeAbility` 下那两组） | **0**（一共 3,000+ 个实例全空） |

> ⇒ **AI 评分表里的 `buffEffect` / `handEffect.cardEffect` 从来没有 `effectText`** —— 别因为「字段名一样」就把它们也当成这条链的输入。

---

## §三 键可达性（哪些印 id）

### 3·1 全部 31 个值 → 官方键 → 文本（现读，en+zh）

**31/31 全部命中**，**没有一条会印 id**。zh 列也 31/31 非空。

| # | 键（`Card_Abilities/` 后的部分） | 出现行数 | 可达 | 英文值 | 中文值 |
|---|---|---|---|---|---|
| 1 | `EffectArmouredSupport` | 1 | ✅ | For the rest of the match, give [[armour]] 1 to Vehicles you put in play | 在剩下的比赛中，给你上场的载具增加 [[armour]] 1 |
| 2 | `EffectBeastBoss` | 1 | ✅ | [[slay]]: Gain [[bloodThirst]] this turn | [[slay]]: 本回合获得 [[bloodThirst]] |
| 3 | `EffectBjornShrine` | 1 | ✅ | The next time it uses [[ferocity]] this turn, it stays in play | 在本回合中，它下次使用[[ferocity]]时，它将保持在场 |
| 4 | `EffectCadiaStands` | 1 | ✅ | For the rest of this battle, at the start of your turn, deploy a Shock Trooper | 在此次战斗的剩余时间里，每当你的回合开始时，部署一名震荡兵 |
| 5 | `EffectCoetrie` | 1 | ✅ | At the start of your turn, create a random Combat Elixir in your hand | 在你的回合开始时，随机在你的手中创建一瓶战斗药剂 |
| 6 | `EffectDisplayCardsAddToDeck` | 1 | ✅ | These cards have returned to your deck | 这些张牌已返回到你的卡组 |
| 7 | `EffectDutysEnd` | 1 | ✅ | [[backlash]]: Trigger the [[codex]] ability of all friendly units | [[backlash]]: 触发所有友方单位的 [[codex]] 能力 |
| 8 | `EffectEnhancedAggression` | 1 | ✅ | [[strike]]: ((Draw_a_card)) | [[strike]]: ((Draw_a_card)) |
| 9 | `EffectFellhanded` | 1 | ✅ | When a friendly unit uses [[ferocity]], it stays in play | 当友方单位使用[[ferocity]]时，它将继续留在游戏中 |
| 10 | `EffectFerocious` | 1 | ✅ | [[slay]]: Draw a Beast | [[slay]]: 抽一张野兽牌 |
| 11 | `EffectGracefulAvoidance` | 1 | ✅ | [[backlash]]: Return to your hand | [[backlash]]: 返回到你的手中 |
| 12 | `EffectHallowed` | 1 | ✅ | [[rage]]: ((Deal_x_damage_to_two_random_enemy_troops,1)) | [[rage]]: ((Deal_x_damage_to_two_random_enemy_troops,1)) |
| 13 | `EffectHelspear` | 1 | ✅ | [[slay]]: ((Deploy_x,$$BL2$$)) | [[slay]]: ((Deploy_x,$$BL2$$)) |
| 14 | `EffectHiveFleet` | 3 | ✅ | Your troops cost 1 less for the rest of this battle | 本场战斗剩余时间内，你的军队的费用减少1 |
| 15 | `EffectHuntersHeretics` | 1 | ✅ | [[backlash]]: Your opponent gains ((QuestPoints2)) | [[backlash]]：你的对手获得 ((QuestPoints2)) |
| 16 | `EffectIdolatrous` | 1 | ✅ | For the rest of this battle, when an enemy troop is deployed, it gains [[vulnerable]] 1 | 在这场战斗的剩余时间里，当一个敌方军队被部署时，它获得 [[vulnerable]] **8** |
| 17 | `EffectImmortalPride` | 1 | ✅ | For the rest of this battle, your Warlords' Talent costs 1 less | 在这场战斗的剩余时间里，你的战将的才能花费减少 1 |
| 18 | `EffectKrump` | 1 | ✅ | All troops you play this turn gain [[flank]] | 在本回合中，你出动的所有军队都获得[[flank]] |
| 19 | `EffectLitanyOfDispair` | 1 | ✅ | [[backlash]]: Give a [[markOfChaos]] to an enemy troop | [[backlash]]：向一支敌方军队赠送 [[markOfChaos]] |
| 20 | `EffectLivingIcon` | 2 | ✅ | [[strike]]: Lower the cost of a random troop in hand by 1 | [[strike]]：将手中随机一支军队的费用降低 1 点 |
| 21 | `EffectMasterOutrider` | 1 | ✅ | [[slay]]: Create a random Sabotage in the enemy hand | [[slay]]：在敌方手牌中随机生成一张破坏牌 |
| 22 | `EffectOath` | 1 | ✅ | [[strike]]: Trigger this troop's [[codex]] ability | [[strike]]：触发此军队的[[codex]]能力 |
| 23 | `EffectPledge` | 1 | ✅ | [[slay]]: Heal 2 and gain a [[markOfSlaanesh]] | [[slay]]：恢复 2 点生命值并获得一个[[markOfSlaanesh]] |
| 24 | `EffectPowerWaaagh` | 1 | ✅ | [[mob]]: Gain ((Melee_attack_icon_x,+1)) until your next turn | [[mob]]：在下一个己方回合之前获得 ((Melee_attack_icon_x,+1)) |
| 25 | `EffectRaidTactics` | 1 | ✅ | When a friendly troop uses [[ferocity]], deal 2 damage to the enemy Warlord | 当友方军队使用[[ferocity]]时，对敌方战将造成2点伤害 |
| 26 | `EffectUgeChoppa` | 1 | ✅ | [[slay]]: ((Heal_x,3)) | [[slay]]: ((Heal_x,3)) |
| 27 | `EffectUndergroudndNetwork` | 1 | ✅ | For the rest of this battle, `<link=sabotage><sprite name=Atlas_trait_icon_sabotage>Sabotage</link>` cards in the enemy hand cost 1 more | 在本场战斗的剩余时间内，`<link=sabotage><sprite name=Atlas_trait_icon_sabotage>Sabotage</link>`卡牌在敌方手中的费用增加1点 |
| 28 | `EffectUndyingLegions` | 1 | ✅ | For the rest of this battle, when a friendly troop becomes a Remnant it gains [[shield]] | 在此次战斗的剩余时间里，当一支友方军队变成残余时，它会获得[[shield]] |
| 29 | `EffectWagesRetribution` | 1 | ✅ | For the rest of this battle, when an enemy dies, gain ((QuestPoints1)) | 在此次战斗的剩余时间里，每当一个敌人死亡，获得 ((QuestPoints1)) |
| 30 | `ExperimentalDronesEffect` | 1 | ✅ | For the rest of this battle, give [[shield]] to all Drones you deploy | 在此次战斗剩余时间内，向你部署的所有无人机赠予 [[shield]] |
| 31 | `SaviorProtocolsEffect` | 5 | ✅ | For the rest of this battle, your Drones cost 1 less | 在此次战斗剩余时间内，你的无人机的花费减少 1 |

> 🔴 **顺手发现（原版本地化自身的 bug，见 §七）**：`EffectIdolatrous` **英文写 `vulnerable 1`、中文写 `vulnerable 8`** —— 31 条里**唯一一条 en/zh 数字不一致**（脚本 `diffnum.py` 逐条比对数字集）。这不是我们抄错，是官方表两列本来就打架。

### 3·2 `Card_Abilities/*` 这 120 条键的**完整归属**（三条链共用同一个前缀）

| 归属 | 条数 | 键的来源 |
|---|---|---|
| **本链（`effectText`）** | **31** | `Card_Abilities/` + `effectText` |
| **批 A（`activeAbilityTextReferenceId ?? uniqueId`）** | **86** | `Card_Abilities/` + 那个 id（`GetLocalizedAbilityDesc` / `GetDescAndAbilityRaw`） |
| **真正没用上的** | **3** | `EffectBoltPistol`（全快照 `"BoltPistol"` **0 命中** = 真孤儿）· `SOR25` / `SOR30`（**被 `SOR25_new`/`SOR30_new` 顶掉** —— 那两张卡的 `activeAbilityTextReferenceId` 非空 ⇒ `?? uniqueId` 的回落支永远不触发，属**被遮蔽**而非孤儿） |
| 合计 | **120** | ✅ 与官方表 `Card_Abilities/*` 计数一致 |

⇒ **31 + 86 + 3 = 120**，**没有第四类**。
（`Card_Abilities/Ability_Name_<id>` 与 `Card_Abilities/GenericSiegeText` 不是叶子键，已在计数里排除，它们是「前缀 + 再拼 id」和「常量键」。）

> ⚠️ 更正既有说法：`activeAbilityTextReferenceId_真值.md` §3·1 把 120 拆成「30 `Effect*` + `ExperimentalDronesEffect` + `SaviorProtocolsEffect` + `SOR25_new` + `SOR30_new` + 86 卡 id」——**分项对，但「30 条 Effect」这个措辞会漏掉 `ExperimentalDronesEffect`/`SaviorProtocolsEffect` 其实也是本链的**（本文已把它们算进 31）。**30 = 29 有主的 `Effect*` + `EffectBoltPistol` 孤儿**。

### 3·3 缺键时的行为（判据，供不确定时兜底）

`GameStaticData.GetEffectDesc.c:76-80`：`ModifyLocalization` 的结果**仍为空** ⇒ `return *(param_1 + 0x28)` = **把 `effectText` 原样返回**。
⇒ **屏幕上会出现 `EffectBoltPistol` 这种 id**。
⇒ 复刻口径：**照抄**（缺键印 id），⛔ 别自己换成空串或别的兜底 —— 那是原版可见行为。
（对比：批 A 的 `GetLocalizedAbilityDesc` **没有**这个兜底档 —— 它缺键返回的是 `GetTranslation` 的空串/键名，两条链在这点上**不同**。）

---

## §四 我们工程里的对应物（三源逐个点，⛔ 只查一份不算）

### 4·1 三源现读

| 文件 | `effectText` | `buffEffect` | `handBuff` | `cardEffect` | 说明 |
|---|---|---|---|---|---|
| `MyGame/Assets/RuleEngine/Resources/cards_engine.json`（**运行时表**，v34 / 1,128 张） | **0** | **0** | **0** | **0** | 运行时**完全没有**这一族列 |
| `MyGame/Assets/RuleEngine/Resources/cards_truth.json`（1,351 张，**判据/对拍用，非运行时**） | **38** | 0 | 66 | 1,416 | **38 条非空全在**，路径 = `abilities[].logic[].buff.effectText` / `abilities[].logic[].handBuff.cardEffect.effectText`（`effectName` 也有 16 条） |
| `d:/4/_tmp_view/allcards_export/cards_raw.json` 的 `full_raw`（SO 真值） | 13,012 次出现（= 8,620 个 `CardEffect` 实例 × key 名） | 6,506 | 4,156 | 6,506 | 原始来源 |

> 🔴 **这是本次最要紧的一条更正**：既有记录写「`effectText` 这条链**整条没有**」——**对 `cards_engine.json` 成立**，但**数据抽取这一步其实早就做完了**：`工具/gen_cards_truth.py` 的 `compact_buff()` 已经把 `effectText`/`effectName` 抽进 `cards_truth.json`，**38/38 条与 SO 真值逐条一致**（本文 §二 就是从 `full_raw` 独立重抽、与 truth 逐条比过）。
> ⇒ 缺口**不在「抽不到」**，在**「没进运行时表 + 没有运行期载体 + 没有消费端」**。这比「整条没有」轻一档，施工量也小一档。

### 4·2 代码侧（`grep` 现状）

| 位置 | 现状 |
|---|---|
| `RuleEngine/Core/CardDef.cs` | **没有**任何 effectText / buffEffect / handBuff 属性（`grep "public .* { get"` 全列过）。最接近的是 `Ability`（`EffectSpec`，**解析结果**，不是原版字段）与 `AbilityTargets/AbilityMultipliers/AbilityTargetCriteria`（**per-(ab,lg) 的既有列族**，见 §五） |
| `RuleEngine/Core/Enchantment.cs` | `ActiveCardEffect` = 我们版 `CardEffect`，但**只承载附魔 12/15/16 那一族**，字段 = `EnchantmentCard/BuffType/Used/Permanent/Source/ActingUnit/Enchantment/Trait` —— **没有 `EffectText` / `EffectName` / `MarkOfChaosType`** |
| `RuleEngine/Core/UnitState.cs:1637` | `TempBuff{IsKeyword,Name,Value,Owner,UntilMyNextTurn,Src,SourceCard,Mod,TraitRec}` —— **没有 `EffectText`** |
| `CardPresentation/Battle/CardDisplayWindow.cs:851` | `RowsOf(IReadOnlyList<UnitState.TempBuff>)` —— **项源只有 TempBuff**；右列 `What` 走 `CardText.KeywordDisplay` / `Battle/Effect/Change*OneTurn` 两条，**完全不读 `Card_Abilities/*`** |
| `CardPresentation/Battle/BattleDriver.cs:13080` | 生产端唯一调用点：`_cardDisplay.Toggle(..., CardDisplayWindow.RowsOf(u.TempBuffs))` |
| `CardPresentation/Battle/CardDisplayWindow.cs:71-95` | 5 个 `EnchanterText` 槽 + 5 个 `EffectText` 槽**已经建好**（`CardWinBox.EffSlots`），排版/字号/对齐都照原版 —— **只差喂真实文本** |
| `CardPresentation/Battle/SkillPanel.cs:2183-2255` | **已有的 `Card_Abilities/*` 取词三件套**：`AbilityRaw(key)`（三档兜底 `Loc.T` + 缺键返回 null + 字体闸）· `AbilityMarked`（= `ModifyLocalization`，走 `CardIcons.RewriteMarks`）· `AbilityTerm`（两者合成）。**目前是 `SkillPanel` 的私有静态方法** |
| `CardPresentation/Battle/BattleDriver.cs:13645` / `CardPresentation/Editor/BattleScene.cs:15761` | `EvtKind.ShowCardsWindow` 的标题：**已用** `Card_Abilities/EffectDisplayCardsAddToDeck`（走 `Loc.T(e.Effect)`） |
| `RuleEngine/Core/EffectResolver.cs:7762` | `ShowCardsWindowOf(card)` —— **硬编码** `case "GSC55": return "EffectDisplayCardsAddToDeck"`，注释里自称「因为 cards_engine 没有 abilityLogic 列，只能先硬编码」 |

⇒ **一句话**：**数据抽得到、官方词条取得到、展示槽建好了 —— 缺的是「把这三段接起来」的那根线**（运行期记录带上 `EffectText`，展示窗右列改读它）。

---

## §五 施工规格（**只写，本次一个字没改**）

### 5·1 生成器 `工具/gen_cards_engine.py` —— 新一列 `abilityEffectTexts`

形状沿用**既有 per-(ab,lg) 列族**（`abilityTargetCriteria: [{"ab","trigger","lg","criteria"}]`，见生成器 `:2030-2060` / `:1953-1954`），**不新造寻址方式**：

```jsonc
// cards_engine.json 每张卡新增一列（只写非空项 ⇒ 全池合计 38 行）
"abilityEffectTexts": [
  { "ab": 0, "lg": 1, "src": "buff", "text": "EffectHelspear" },
  { "ab": 0, "lg": 0, "src": "hand", "text": "SaviorProtocolsEffect" }
]
```

- **`src` 必须留在列里**：`TAU51` 的 `abilityLogic[0]` 两处都是 `SaviorProtocolsEffect`，只靠 `(ab,lg)` 分不开。
- **`trigger` 可省**（`ab` 已是 `cardAbilities` 下标，`lg` 是 `abilityLogic` 下标，二元组唯一）。
- 数据源：`full_raw.cardAbilities[i].abilityLogic[j].buffEffect.effectText` 与 `...handBuff.cardEffect.effectText`，**只收非空**。
- **同时收 `effectName`**（16 条非空 / 4 个值）—— 它是左列「谁给的」的**第一档**（§1·2 ⑨），与 `text` 同表同列最省事：`{ "ab":0, "lg":0, "src":"buff", "text":"…", "name":"TL47" }`。
- **必须 bump `version`**（现读 **34**）—— `CardDatabase.cs` 的 version 守卫靠它出声。
- ⚠️ **加列必须同时看 `CardDef` 的 `…Versioned` 三态**（本项目既有惯例：`AbilityTargetsVersioned` / `ActiveAbilityLogicVersioned` 等），否则「列缺失」会被当成「全池空」静默吞掉。

### 5·2 `RuleEngine/Core/CardDef.cs`

```csharp
public sealed class AbilityEffectText { public int Ab; public int Lg; public bool IsHand;
                                        public string Text; public string Name; }
public IReadOnlyList<AbilityEffectText> AbilityEffectTexts { get { ... } }
public bool AbilityEffectTextsVersioned { get; private set; }   // 三态，与既有列族同款
public AbilityEffectText EffectTextOf(int ab, int lg, bool isHand);   // 找不到 ⇒ null
```

### 5·3 运行期载体（**三处，按 §二·1 的分布各管一段**）

| 载体 | 覆盖行数 | 要加什么 | 判据 |
|---|---|---|---|
| `Enchantment.cs` 的 `ActiveCardEffect` | **29**（bt 12/15 且有附魔卡引用） | `public string EffectText;` + `public string EffectName;`（挂载时从 `CardDef.EffectTextOf(ab,lg,src)` 带过来） | 这就是原版 `CardEffect +0x28/+0x20`；本类已是「我们版 CardEffect」 |
| `UnitState.TempBuff` | **6**（`untilEndOfTurn=1` 那 6 行） | `public string EffectText; public string EffectName;` | 原版限时那条也在 `+0x108` 里（本条 `TempBuff.TraitRec` 的 doc 已用同一条判据） |
| `changeCost` / `addTrait` 的**记录** | **8**（6×bt2 + 2×bt3） | 这两族在我们引擎里**没有可枚举的 effect 记录**（cost mod 在 `CostMods`、trait 在 `TraitEffects`），**要先决定要不要给它们开一条「可展示记录」** | ⚠️ **这一格是本文没查透的部分**，见 §七 |

### 5·4 消费端（两处）

1. **`CardDisplayWindow.RowsOf(...)` 的右列 `What`**（`CardDisplayWindow.cs:851`）：
   - 若该行对应的运行期记录带 `EffectText` ⇒ `What = AbilityTerm("Card_Abilities/" + effectText)`；
   - 否则**回落**到现有 `CardText.KeywordDisplay` / `Battle/Effect/Change*OneTurn`（⛔ 不要删旧路 —— `TempBuff` 里绝大多数条不带 `EffectText`）。
   - 🔶 **这是「我们自己设计」的一处回落顺序**（原版没有「回落」这个概念：原版 `effectText` 非空就直接用它，为空才走 `case` 分派）。按原版实现时应写成：`GetEffectDesc(effect)` 的第一支就是 `if (effectText != "") return Translate("Card_Abilities/"+effectText);` —— 等价语义，**回落 = 空 `effectText` 那一支**，不是我们发明的。
2. **把 `SkillPanel` 那三件套提成共用件**（⛔ 别复制第二份 —— 铁律「两处写同一条规则 = 迟早不一致」）：
   `AbilityRaw/AbilityMarked/AbilityTerm` 现在是 `SkillPanel` 私有静态，`CardDisplayWindow` 与（将来）`EffectResolver.ShowCardsWindowOf` 都要用 ⇒ **抽到 `Core/Loc.cs`**（如 `Loc.AbilityText(string key)` / `Loc.AbilityRaw(string key)`），`SkillPanel` 改成转调。
   ⚠️ 抽的时候**连字体闸一起搬**（`CardText.Zh == false` 时取英文列）—— 那条闸是「中文档但中文字体缺失 ⇒ 印方块」的挡板，只搬一半会在另一条路上复发。
3. **`EffectResolver.ShowCardsWindowOf`（`:7762`）可以去掉硬编码**：有了 `abilityEffectTexts` 之后，`case "GSC55"` 那张表改成「扫本卡 `abilityEffectTexts` 里 `chosenAbility == 470` 的那条」，⛔ 但**保留那张硬编码表作为回退**直到数据列上线（现在的 `TestW62EntryPump` 正钉着它）。
4. **左列「谁给的」也要换口径**（⚠️ 这条**今天没人做**，容易被漏）：
   原版 `CardEffectItem.LoadEffect` 是三档 —— ① `effectName` 非空 ⇒ `GetEffectName`（→ `Card_Name/<effectName>`）② 否则 `enchantingCard(+0x18).rawCardData` 的卡名 ③ 都空 ⇒ **空串**（`CardEffectItem__LoadEffect.c:116 / :135 / :126-131`）。
   我们现在 `RowsOf` 用的是 `TempBuff.SourceCard`（**施加它的那张卡名**，`CardDisplayWindow.cs:869-873`）—— 语义接近但**不是原版那三档**（原版优先印的是 `effectName`，其次是**附魔卡**自己的名字）。
   ⇒ 要照原版做，需要 `effectName` 一起进列（§5·1 已包含），并在记录上带 `EffectName` / `EnchantmentCard`。

### 5·5 验收口（可写成断言）

| # | 断言 | 判据 |
|---|---|---|
| ① | 池内 `abilityEffectTexts` 非空行 = **38**，`distinct text` = **31** | 本文 §二 |
| ② | 38 行里**每一条**的 `Loc.T("Card_Abilities/"+text) != key`（即全部有词条，**一条都不印 id**） | 本文 §三 |
| ③ | `EffectBoltPistol` **不在**任何卡的列里 | 本文 §3·2 |
| ④ | `EffectTextOf(0,1,false)`（Helspear）= `EffectHelspear`；`EffectTextOf` 对非 38 行返回 `null` | —— |
| ⑤ | 展示窗：给一张挂了 `EffectHelspear` 附魔的卡开窗 ⇒ `EffectWhat(0) == Loc.T("Card_Abilities/EffectHelspear")` | `CardDisplayWindow.EffectWhat(i)` 已有读口（`CardDisplayWindow.cs:333`） |

---

## §六 与「批 A」能不能合并成一次重生成

**批 A 指**：`cardDescriptionTextReferenceId`（44 非空）· `activeAbilityTextReferenceId`（3 非空）· `targetingTextReferenceId`（51 非空）。

| 维度 | 批 A | 本链 | 能否同批 |
|---|---|---|---|
| 数据源 | 卡 SO 上的**卡级标量**（`full_raw.<field>`） | **per-(ability,logic) 数组** | ✅ 同一次读 SO 就能都拿到（同一个 `full_raw`，同一趟生成） |
| 生成器改动 | 3 个卡级列 | 1 个数组列 | ✅ 同一次 `gen_cards_engine.py` 跑完，**一次 bump version** |
| `CardDef` 改动 | 3 个 `string` 属性 | 1 个 `IReadOnlyList` + 查找口 | ✅ 互不影响 |
| 消费端 | 卡面正文 / 技能面板（`BattleDriver.FaceTextFull` / `SkillPanel`） | 战斗展示窗效果行 | ⚠️ **不同面** ⇒ 断言要分开写 |
| 验证口径 | 改的是**现有可见文本**（10 张池卡的正文会变，见批 A 判据 §5·3） | **新增一条链**（现在一行都不显示） | ⚠️ 对拍脚本分开 |

**结论：✅ 可以合并成一次重生成**（一次 `gen_cards_engine.py` + 一次 version bump + 一次 `ruleprobe check` / 全池对拍），**但断言与对拍脚本必须分两套**：
- 批 A 的验收 = 「10 张卡的正文变对了」；
- 本链的验收 = §5·5 那 5 条。

⚠️ **顺序建议**：先落批 A（它改的是**已经显示着**的文本，错了玩家马上看得见），再落本链（新增，错了是「该显示没显示」）。同一 commit 也行，但**分开跑对拍**，否则一条红分不清是谁的。

⚠️ **合并前先确认一件事**（本文没查）：`gen_cards_engine.py` 现读 **7,191 行**、`version = 34`；批 A 与本文都要动它 ⇒ **必须同一个写手**（铁律 13·3 一个文件一个写手）。

---

## §七 还差什么（如实记，⛔ 不拿猜测填空）

1. **运行期项源模型不统一**（**硬阻塞**，非本文能解）
   `判据/CardEffectsToShow_判据.md` §7.2 已结案：原版 `+0x108` 是**一条** list，我们拆成了 `ActiveCardEffect`(附魔) / `HandEffects`(手牌) / `TempBuff`(限时) / `Aura` / `Persistent` **4~5 套异构模型**，没有一处能给出「当前这张卡身上所有生效效果」的可枚举投影。
   ⇒ **`EffectText` 挂在哪一格，取决于那条投影先落地**。本文只负责「文本怎么取」，不替那条投影做决定。
   📌 **可独立先做的部分**：`EffectText` 字段本身（§5·2/5·3）+ 数据列（§5·1）**不依赖**项源统一 —— 两者可以并行。

2. **8 条 `changeCost` / `addTrait` 的 effectText 在【我们】这边没有载体**（原版那半边已查实）
   - ✅ **原版那半边已查实（本文补读）**：`CardScript__AddEffect.c` 的 `switch(buffType)` 里
     `case 1/2/10` 合并后追加（`.c:400-417`，`case 2` = `changeCost`）· `case 3` 追加（`caseD_3` 段 `.c:461-498`，`case 3` = `addTrait`）
     ⇒ **这 8 条原版同样进 `+0x108`、同样会被展示**。（`switchD_1805e5668_caseD_7` 只对**未处理**的 buffType 打 `LogWarning` 且**不追加** —— 2 与 3 都不落在那一档。）
   - ⛔ **我们这半边仍是缺口**：这 8 行落到 **cost mod / trait effect**，而这两族在我们引擎里**没有「可枚举的 effect 记录」**（`CostMods` 是算式、`TraitEffects` 是聚合）⇒ 展示层拿不到「这条 effect 带什么文本」。
   - ⇒ 结论：**要做**（铁律 11），但依赖 §七·1 的统一投影；**不能靠「附魔? = 否」推「不该显示」**。

3. **`effectName`（左列「谁给的」）的 4 个值「能解析」已核，缺的只是一处**
   现读 `effectName` 非空 16 条，值只有 `TL47` / `TL49` / `TL50` / `TAU51` 四个，**全部** `Card_Name/<值>` 命中（`TL49`→Hyper-adaptation · `TL50`→Infinite Biomorphologies · `TL47`→Hive Fleet Leviathan · `TAU51`→Saviour Protocols）。
   ✅ **已补核**：这 4 个 id **全部在池**（`cards_engine.json` 的 `id` 逐张命中，`cardType` 都是 20）。
   ⚠️ **还没查清**：`GetEffectName` 的**第二档**（第一档 `Card_Name/<effectName>` 取不到时，去 `fullCardCollection` 里按 `Predicate` 反查一张卡）—— 那个 `Predicate` 是 `<>c__DisplayClass232_0`（`GameStaticData.__c__DisplayClass232_0___GetEffectName_b__0.c`），本文**没展开**。
   ⇒ 影响面：**只影响左列文本**，与右列（`effectText`）和键可达性无关；且现数据 16 条**第一档全部命中**，第二档今天走不到。

4. **官方表 `EffectIdolatrous` 的 en/zh 数字打架（1 vs 8）** —— 原版本地化自己的 bug。
   ⇒ **还没查清**：复刻应照 **en(1)** 还是 **zh(8)**？
   建议判据：卡的实际结算值（`trait=0`、`traitValue=0`，实际数值由附魔卡 `Z-…-Idolatrous…_enchantment` 的 `buffEffect` 决定 —— **本文没查那张附魔卡的数值**）。**要么补查那张附魔卡，要么由调度台裁定「照抄 zh 列」**。⛔ 别自己挑一个。

5. **`GSC51` / `TL47` / `TAU51` 的同卡多挂点，是不是「同一效果挂多次」还是「三种不同触发」**
   三张卡的 `effectText` 在多个 `(ab,lg)` 上重复（`GSC51`×2 / `TL47`×3 / `TAU51`×5）。本文只证明了**值相同**，**没查**原版这些挂点会不会在**同一张卡上叠出多条展示行**（`GetCardEffectsToShow` 是逐条加、**不按 effectText 去重** ⇒ 大概率叠；但那属于 `CardEffectsToShow_判据.md` 的面，本文不重复判）。

---

## 附：复现命令（只读）

```bash
# §二 逐行真值（38 行）
python -I -X utf8 d:/4/_tmp_view/w73/extract.py          # → d:/4/_tmp_view/w73/effecttext_rows.json
python -I -X utf8 d:/4/_tmp_view/w73/rows_detail.py      # 补 buffType/trait/时窗/附魔引用

# §二·2 覆盖率（8,620 个 CardEffect 实例 / 38 非空）
python -I -X utf8 d:/4/_tmp_view/w73/count_all.py

# §三 键可达性 + en/zh
python -I -X utf8 d:/4/_tmp_view/w73/loc.py
python -I -X utf8 d:/4/_tmp_view/w73/classify.py         # 120 条 Card_Abilities 三分类
python -I -X utf8 d:/4/_tmp_view/w73/diffnum.py          # en/zh 数字不一致（1 条）

# §四 三源
python -I -X utf8 d:/4/_tmp_view/w73/three_src.py
python -I -X utf8 d:/4/_tmp_view/w73/truth3.py           # cards_truth 里那 38 条
```

> ⚠️ 所有脚本都是**二进制/UTF-8 显式读**，路径写死在 `.py` 源码里（本机 `python -c "…中文路径…"` 会在 argv 解码处失败）。
