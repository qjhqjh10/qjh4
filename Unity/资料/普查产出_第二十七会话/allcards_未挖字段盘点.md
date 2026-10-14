# allcards（原版 1195 张卡 SO 全字段）· **未挖字段盘点**

> 数据源：`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`（38.6 MB，JSON 数组；包 = `allcards_assets_all`，
> Unity `2022.3.38f1` / **2025-07-30** 快照）。抽取脚本（可复算）：`d:/4/Unity/工具/_mine_allcards_fields.py`。
> 字段语义 = **第一权威**：反编译方法体 `d:/2/tools/decomp_full/` ＋ 类签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/`。

## 〇、结论（三句话）

1. **1194 个元素（+1 个 `AddressablesBuildNumber`）= 935 张【短码 id】收藏卡 ＋ 259 张【GUID id】不可收藏卡**
   （异画/文本变体/战场环境卡/生成卡：药剂·破坏·秘仪）。**259 张的 `inventoryOptions` 全部 = `10 NotInInventory`。**
2. 未挖字段里 **有真值、而且我们工程正在猜** 的共 **12 条**；另有 **27 个字段整包全空**（原版自己就没写，不是我们没拿到）。
3. 全包里 **`extraScoreFrom*`（7 个）与 `manualValueInPlay`/`overrideDefaultValueInPlay` 全是 0** ⇒ 这一族**没有数据可换**（见 §三）。

---

## 一、三个高优先项（每项：是什么 · 我们工程对应哪处 · 下一步）

### ① `inventoryOptions`（**332** 张非 0）＋ `showInCollectionOptions`（Hide **255** 张）—— 替掉 `DeckRules.IsEffectOnly`

- **是什么**：`CardInventoryOptions { InInventory=0, CantAddToDeck=5, NotInInventory=10 }`（桩 `CardInventoryOptions.cs:1-6`）·
  `ShowInCollectionOptions { Hide=0, Show=5, OnlyIfOwned=10 }`（桩 `ShowInCollectionOptions.cs:1-6`）。
- **我们工程对应**：`RuleEngine/Core/DeckRules.cs:190-207` 的 `IsEffectOnly(subtype)` —— 注释自己写着
  「**`inventoryOptions == CantAddToDeck(5)`…而那个字段本地解包产物里根本没有 ⇒ 我们改用 `Subtype`**」，且**是用户 2026-09-27 拍的口径**。
- **原版怎么用（可引）**：`DeckUtility__ValidateDeckCards.c:68-72` → `inventoryOptions != 0` ⇒ deckError **4**；
  `:73-77` → `showInCollectionOptions != 5` ⇒ deckError **4**。
- **数据**：短码卡 `0×860 · 5×72 · 10×1`；**259 张 GUID 卡全部 = 10**。`=5` 的 **72 张 ⊆ spellType 200（进攻 36）/ 210（防御 36）**。
- 🔴 **与现有口径冲突（要用户裁，不是我能改）**：我们的 `IsEffectOnly` 命中的 6+4+5 = **15 张**是 spellType `260 CombatElixir / 230 Sabotage / 150 Secret`，
  它们**全是 GUID 卡、flags = `NotInInventory`（不是 `CantAddToDeck`）**；而原版打 `CantAddToDeck` 的是 **72 张战场环境卡**。**两边不重合。**
- **下一步**：`工具/gen_cards_engine.py` 加两列（源 = 上面那份 JSON）→ `cards_engine.json` → `CardDef` 加 `InvOpt`/`ShowColl` →
  `DeckRules.Validate` 第 ⑥b 步改成「读字段」；`IsEffectOnly` 留作**兜底**并注明。

### ② `relatedCard1..4`（**196 / 31 / 9 / 4** 张，共 **240** 条边）—— 替掉 `RelatedCards.Find`

- **是什么**：卡自带的「相关卡」引用（同包内 PPtr）。
- **我们工程对应**：`CardPresentation/Core/RelatedCards.cs` —— 文件头写着「**我们这套是替身**」（`:35`）、
  「**这条是我们的口径，不是从原版证出来的**」（`:127`）；`RuleEngine/Core/RuleCore.cs:2688` 写着「**`relatedCard1` 本地拿不到**」。
- **实据（本轮现做）**：用 UnityPy 读 `54bdb7b1…/__data` 建 `m_PathID → uniqueId` 表后逐条解 —— **240 条边全部解出，0 条失败**。
  例：`SAU37 Orikan the Diviner → cbabeb234380634479feab1a1b4c733a` · `DA46 The Rock → DA6` · `AM51 Cadia Stands → AM5`。
- **下一步**：照 `数据/游戏数据/warlord_goes_second.json`（同一种「同包 PPtr 按 `m_PathID` 解 id」的现成链）出 `related_cards.json`
  → `CardDef` 加 4 槽 → `RelatedCards.Find` 改成「有真值读真值，替身只兜底」。判据出处 = `RawCardScript__GetRelatedCards.c`（读 `+0xE8/0xF0/0xF8/0x100`）。

### ③ `nonTargetScoringCriteria`（**355** 张）＋ `cardTargetScoringCriteria`（**325** 张）—— 原版逐卡手写的 AI 评分表

- **是什么**：`List<ScoringCriteria>`，每条形如
  `{baseValueFriend, baseValueEnemy, multiplyByFriend, multiplyByEnemy, scoringOption, targetCriteria, multiplierCriteria, damageCriteria, healCriteria, buffEffect, handEffect, codexEffect, comboCard, comboCardFirst}`（桩 `ScoringCriteria.cs:32-58`）。
- **我们工程对应**：`RuleEngine/Data/SimpleAI.cs:718` 自己写着「**那些值在服务端 ⇒ 我们从自己解析出来的 `EffectOp` 推**」（`ScoreOps`/`ScoreOp`）。
- **原版怎么用（可引）**：`AI__ScoreFromPlayingNow.c:36-66` 按 `targeted` 走 `rawCard+0x2e8`（cardTarget 表）或 `+0x2f0`（nonTarget 表）
  → 逐条 `AI__ScoreFromCriteria.c`，按 `scoringOption` 分派 **22 种**算法（枚举全文 `ScoringCriteria.cs:8-30`：FixedValue/Count/SumValueInPlay/…/ValueOfChangeWeapon）。
  ⚠️ 偏移链相邻已核：`+0x2c0 overrideDefaultValueInPlay · +0x2c4 manualValueInPlay · +0x2c8..+0x2e0 extraScoreFrom* · +0x2e8 cardTarget 表 · +0x2f0 nonTarget 表`。
- **数据**：全包 `{1 条: 352 张, 2 条: 3 张}` / `{1 条: 319 张, 2 条: 6 张}`；**我们池子里有 206 + 151 张**。
- **下一步**：① 出 `ai_scoring.json`（卡 id → 判据数组，照 `ScoringCriteria` 全展开）② **先只读对账**
  （拿我们的 `ScoreOp` 结果 vs 原版 `baseValue*`/`multiplyBy*` 逐卡比，列出「原版有表、我们靠推」的那 206+151 张的差异）
  ③ 再决定 `ScoreOps` 换成读表还是表+兜底。⚠️ 这是**语义改动**，别一步替换。

---

## 二、逐字段总表（按「有意义张数」降序；分母见备注）

> 口径：**有意义 = 非 `null`/空串/空数组，且不是「全默认」（0 / False / 空引用 / 全 0 结构）**。
> 只看「非空」会把 `extraScoreFromDamaging = 0.0`（1192 张全 0）读成「人人都有值」。脚本 `--tsv` 给全量。
> `m_Enabled`·`m_GameObject`·`m_Script`·`m_Name` 是 Unity 序列化头，不算内容字段，不列。

| 字段 | 有意义张数 | 取值样例 | 是什么 | 我们工程对应处 / 是否在猜 | 优先度 |
|---|---|---|---|---|---|
| `cardRarity` | 1188 / 1194（另 6 张 = 0 None） | 2×315 · 1×314 · 3×226 · 4×202 · 5×131 | `CardRarity {None0,Common1,Rare2,Epic3,Legendary4,Special5}`（桩 `CardRarity.cs`） | 我们已有 `rarity`（`cards_engine.json`）—— **值是否一致未逐卡核** | 中 |
| `attackDamageType` | 1138 / 1192（=4 Attack） | `4`×1138 · `0`×54（54 张多为督军） | `DamageType` 枚举（桩 `DamageType.cs`，13 值） | **没用**（我们无伤害类型概念） | 中 |
| `showInCollectionOptions` | 937（=5 Show）· 255（=0 Hide） | `5` ×937 · `0` ×255 | 是否进收藏 | **没用** —— 见 §一①  | **高** |
| `cardShortId` | 901 / 1192 非零 | 1…914，860 种 | 短数字 id（老 id 方案？） | **没用**；可能与 `card_ids.json` 那个错 id 列有关（`A970`） | 中 |
| `useOverDraw` | 605 / 1192（=1） | `1`×605 · `0`×587 | 立绘溢出卡框的**逐卡 opt-in** | `CardView.cs:1950,4911` 写着「**值在服务器（我们拿不到）**」⇒ 现成兜底 | **高** |
| `nonTargetScoringCriteria` | **355** / 1192 | 1 条×352 · 2 条×3 | 原版 AI 评分表 | `SimpleAI.cs:718` —— **在推** | **高**（§一③） |
| `inventoryOptions` | 332 / 1192 非零 | `5`×72 · `10`×260 | 库内状态 | `DeckRules.cs:190-207` —— **在猜** | **高**（§一①） |
| `cardTargetScoringCriteria` | **325** / 1192 | 1 条×319 · 2 条×6 | 同上（指定目标那支） | **在推** | **高**（§一③） |
| `cantBeRandomlyCreated` | 301 / 1192（=1） | 228 张 GUID 卡 + 73 张短码卡 | 「不能被随机生成」 | **没用**；`CreatePool` 的候选池是**按卡面 subtype 推**的 ⇒ 该字段是原版闸门 | 高 |
| `targeted` | 218 / 1192（=1） | `1`×218 | 这张牌要选目标 | 我们**从效果解析推**（`SimpleAI` 引 `EntityScript__get_targeted`）；字段本身没用 | 中 |
| `expansionPack` | 201 / 1194 非零 | 65×42 · 40×18 · 20×16 …（0 = 基础 993 张） | `CardExpansionType`（桩 `CardExpansionType.cs`：10 LeviathanDrop1 … 80 DarkAngelsDrop1） | **没用**；商店/卡包/收藏筛选要用 | 中高 |
| `relatedCard1/2/3/4` | **196 / 31 / 9 / 4** | `DA46 → DA6` · `AM51 → AM5` | 卡自带「相关卡」PPtr | `RelatedCards.cs` —— **自认是替身** | **高**（§一②） |
| `itemDescriptionKey` | 83 / 1194 | `Item_Description/OffenceCardExplanation`×47 · `…/DefenceCardExplanation`×36 | 卡面「说明行」的 **I2 键** | **没用**；⚠️ **现在能解了**（`数据/游戏数据/官方本地化_en+zh-CN.json`：Offence = 「可选效果…先手可选激活」/ Defence = 「后手加入起手牌」） | 高 |
| `refNameId` | 70 / 1192（**全在 GUID 卡上**） | `NormalOffensiveConditions`×12 · `TL50`×6 · `d01a5ba5…`×3 | 卡名的 **I2 键后缀**（`Card_Name/<refNameId>`） | **没用**；⇒ 这些卡**显示名 ≠ `cardName`**（例：`cardName`="Normal Conditions (Black Legion)"，显示 = `Card_Name/NormalOffensiveConditions` = "Normal Conditions"） | 中 |
| `fullHeroSpriteReference` | 59 / 1192 | 空引用×1133 | 督军**整幅立绘**（备用图） | 只在 `TutorialModePopup.cs` 见过 `fullHeroSprite` | 中 |
| chat 音族（`defaultChatSound` 等 12 个） | 各 57–59 / 1192 | 每族 50–51 种 GUID | 督军**表情语音**的 12 个槽 | `chatMessagesReferenceId` 用了；**12 个音槽没用**（语音线只做了 8 族触发） | 中 |
| `hasActiveAbility` | 58 / 1192（=1） | 卡 48 单位 + 10 督军 | 有主动技能 | `RuleCore.cs` / `SimpleAI.cs` 已在用 —— **值一致性未核** | 中 |
| `eventSpriteName` | 54 / 1192（44 张短码） | 全 54 张都 = `"Warlord_events_Loken"` | 督军「事件」立绘的 sprite 名 | **没用**；⚠️ 54 张值**全同** ⇒ 多半是**图集名**、单图按 id 取（**未查清**） | 中 |
| `customIntroChatData` | 53 / 1192 | `[{opponentWarlord:"SAU35", customSound:{…}}]` | 督军**对特定对手**的开场语音 | **没用**（联机开场少一层） | 中 |
| `targetingTextReferenceId` | 44 / 1192 | `chooseTarget`×10 · `deal3Dmg`×5 · `stunEnemy`×5 | 选目标时那行提示的 **I2 键** | **没用**；🔴 **键前缀实测 = `Card_Targeted/`**（30 个键全在表里，例 `Card_Targeted/giveFlank`） | 高 |
| `altArtFromId` | 36 / 1192 | 全 36 张都 = `"DefensiveCardLore"` | 异画来源标记 | **没用**；语义**未查清**（与 `alternateArts` 两回事） | 低 |
| `targetsAvailableCounter` | 23 / 1192 | `TargetCriteria` 结构 | 「还能选几个目标」的计数条件 | **没用**；`showTargetsAvailableInPlay` 全 0 ⇒ 本快照**没启用** | 低 |
| `customIntroByArmyChatData` | 10 / 1192 | `[{army:60, customSound:{…}}]` | 按**阵营**的开场语音 | **没用** | 低 |
| `customIntroReferenceId` | 7 / 1192 | `ASH3` · `BL35` · `TL34` … | 开场语音的文本键（= 另一张卡的 id） | **没用** | 低 |
| `blockActionsWhileResolving` | 4 / 1192（=1） | `SAU49 Resurrection Orb` · `GSC49 Telephatic Domination` · `Path of the Seer` · `TL27 Exocrine` | 结算中冻结操作 | **没用** | 中 |
| `activeAbilityTextReferenceId` | 3 / 1192 | `SOR30_new` · `AM68` · `SOR25_new` | 主动技文本的 **I2 键**（`Card_Abilities/<v>`，已现核可解） | **没用**（`SOR25_new/SOR30_new` 是 **`<id>_new` 改写版**） | 中 |
| `summonAnimDelayed` | 1 / 1192 | 1 张 | 召唤动画延后 | **没用** | 低 |
| `deckCriteria` · `normalAbilityBoardCriteria` · `attackScoringCriteria` · `specialAttackScoringCriteria` | **0**（结构在、**逐字段全默认**） | `{multiplyByFriend:1.0, multiplyByEnemy:-1.0, scoringOption:0}` = 纯 ctor 默认（`ScoringCriteria___ctor.c`） | 见左 | 原版**没写值** ⇒ 不是「我们没拿到」 | —— |
| `extraScoreFrom*`（7 个）· `manualValueInPlay` · `overrideDefaultValueInPlay` | **0**（1192 张**全 0**） | `0.0` / `0` | 原版 AI 的逐卡加权 | **无数据可换**（见 §三） | 低 |
| 全空一族（**27** 个） | **0** | —— | `itemLoreKey`·`knightCriterias`·`nameModifier`·`primarchReferenceId`·`startingTroopsInPlay`·`targetingText`·`transformedWarlordId`·`cardRaceTextReferenceId`·`secretIdentity`·`currentAlternateArt`·`chatAudioReferenceCard`·`referenceCard`·`targetedSpellAnim`·`nonTargetedSpellAnim`·`questTimes`·`initial{Hand,Mana,Psychic,Plasma,SpiritStone}Modifier`·`_quest`·`_stratagem`·`emptyCard`·`autoAssign`·`starterHero`·`specialBossMusic`·`hasAlternativeAbility`·`alternativeAbility` | 原版**自己就没写** ⇒ 不是「我们没拿到」 | 低 |
| 另 2 个非卡对象：`AlternateArtCard` | 2 | `AA_HB_DarkAngels_war_Azrael`(`#HB_DA1`) · `AA_HB_BlackLegion_war_Abaddon the Despoiler`(`#HB_BL34`) | 异画对象（字段 `style=5 HammerAndBolter` · `originalCard` · `cardImage` · `fullWarlordImage` · `attackAnim`） | 我们只有 `alternateArts`/`currentAlternateArt` 两个引用；**异画对象自身字段没用** | 低 |

---

## 三、`extraScoreFrom*` / `*ScoringCriteria` 那一族 · 单独一节

**结论：字段结构是真的（原版 AI 确实读它们），但【这个快照里没有数据】。** 逐条实证：

1. **它真被读**（偏移链相邻已核，非推测）：
   - `AI__ScoreFromBuffedAttack.c:94-95` → `攻击者 raw+0x2d4` + `目标 raw+0x2d8`
   - `AI__ScoreFromJam.c:16` → `raw+0x2dc` · `AI__ScoreFromKeepingTheCard.c:61` → `raw+0x2e0`
   - `AI__GetCardReferenceValueInPlay.c:8-9` → `raw+0x2c0` / `raw+0x2c4`；`AI__ScoreFromReturnToHand.c:12-16` 同两个偏移
   - 与声明序对齐后 = `overrideDefaultValueInPlay(0x2c0) · manualValueInPlay(0x2c4) · extraScoreFromDamaging(0x2c8) · Killing(0x2cc) · Healing(0x2d0) · Attacking(0x2d4) · TargetingOnAttack(0x2d8) · Jamming(0x2dc) · KeepingCard(0x2e0)`（桩 `RawCardScript.cs:205-221`）。
2. **公式已还原**：`GetCardReferenceValueInPlay = overrideDefaultValueInPlay ? 2×manualValueInPlay + K : 2×manaCost + K`
   （`K = DAT_1834b2bb8`，**本轮没读那个常量**）。
3. **实测数据 = 空**：`extraScoreFrom*` 7 个 + `manualValueInPlay` 在 **1192 张上全是 0**；`overrideDefaultValueInPlay` **0 张为 1**。
   ⇒ **不能拿这一族「换成原版真值」—— 没有真值。**
4. **能换的是两个 `*ScoringCriteria` 列表**（`cardTargetScoringCriteria` 325 张 / `nonTargetScoringCriteria` 355 张），
   它们的**子字段**有真数据（689 条判据）：`scoringOption` 631 条非默认、`targetCriteria` 582、`multiplyByFriend` 510、
   `baseValueFriend` 169、`multiplyByEnemy` 154、`damageCriteria` 122、`baseValueEnemy` 8、`healCriteria` 7、`multiplierCriteria` 3；
   `codexEffect` / `comboCard` / `comboCardFirst` **0 条**非默认。
   `scoringOption` 分布：`{2 SumValueInPlay 230 · 1 Count 208 · 3 SumValueOfDamage 100 · 0 FixedValue 58 · 35 ValueOfDrawingCard 47 · 13 16 · 5 10 · 4 9 · 25 5 · 14 3 · 12 2 · 6 1}`。
5. **另外两条「相关但不在卡上」的**：`AI__ValueOfTraits.c` 从 **`CardTraitCollection` 资产**取 `GetTrait(id)` 的
   `+0x28`（float 分值）—— 那是**另一份资产**，不在 allcards 里（我们 `SimpleAI.cs:675` 的 `TraitScore` 表已是它的替身）。
   `AI__ValueOfEnchantments.c` 同理（走 enchantment 的 `CardEffect`）。

---

## 四、⛔ 判不了的字段（逐条 + 搜过什么）

| 字段 | 卡在哪 | 搜过什么 |
|---|---|---|
| `cantBeRandomlyCreated` 的**读取点** | 语义自明（桩 `RawCardScript.cs:11` 有声明），但**找不到读它的方法体** | `decomp_full` 全库无同名访问器（名字被剥）· 桩里只有声明 · 按 `-i random` 列出的 30 个方法名逐一看过（只有 `GetRandomCardback/CardTier`、`MatchData.SetBotDeckRandomCardLevels` 几个，都不是）· `BattleCardManager__CreateCard.c` 逐行扫过未命中 |
| `eventSpriteName` 为什么 54 张**值全同** | `"Warlord_events_Loken"` 54 张一模一样 ⇒ 不像逐卡字段 | 只看了 JSON 取值分布；**没去解包资源里找同名 sprite** |
| `altArtFromId` = `"DefensiveCardLore"`×36 | 语义不明（与 `alternateArts` 引用无关） | `RawCardScript.cs` 桩里只在声明处出现；**没找到读取点**；I2 表里也没有该词条 |
| `cardShortId` 与 `card_ids.json`（`A970` 那个错 id）是否同一套 | 只知取值 1…914、860 种 | 没去比 `数据/游戏数据/card_ids.json` |
| `refNameId` 的**读取点** | 推断来自 `RawCardScript__GetCardNameTerm.c`（`+0x110` 参照卡 → 否则用某字符串当键），但**没能把 `+0x68` 这个偏移钉到 `refNameId` 上** | 读了 `GetCardNameTerm.c` / `GetLocalizedCardName.c`；**偏移反推有几处对不上**（`+0x58` 我们另记成 `useOverDraw`）⇒ 不硬下结论 |
| `DAT_1834b2bb8`（`GetCardReferenceValueInPlay` 的那个常数） | 没读字面量 | 没走 `资料/战斗规则与数值_出处.md` §三 那套 RVA→文件偏移 |

---

## 五、顺手发现（**由主对话分流，我不动**）

1. 🔴 **我们的 187 个「名样式 id」= 原版那 259 张 GUID 卡的自造命名**：按名字 1:1 对上 **90/187**
   （`ASH_Wrath_of_Khaine` ↔ GUID `33eabec1…"Wrath of Khaine"`；`ASH_Path_of_the_Seer` ↔ `09bcb1b3…`，
   后者正是 `blockActionsWhileResolving=1` 的四张之一）。GUID 卡**全部** `inventoryOptions=10 / showInCollectionOptions=0`。
   ⇒ 「换成原版 GUID id」能一次拿到 flags，但**会动到卡组/录像/哨兵**（`A1760` 刚钉过指纹）——要大改的话要单独排。
2. **我们有、allcards 没有的 98 张短码卡**（`EC16 Screamer Kakophonist`、`TL66 Neurogaunt`、`TAU38 Stormsurge`… 60 张是 spellType 卡）
   —— 老快照缺的是这些；**不是**「我们有 289 张是自造的」（那 289 = 187 名样式 + 102 新卡）。
3. `Card_Name/` 有 1065 条、`Card_Description/` 1098 条、`Card_Abilities/` 85 条、`Card_Targeted/` 30 条、
   `Warlord_Lore/` 1042 条 —— **`cardDescriptionTextReferenceId`（43 张，我们已在用）的值同样是 I2 键**
   （`Card_Description/<值>`），现在**可以拿官方 zh-CN 逐条核**我们自译的那 43 条。
4. `Item_Description/*` 那两条（83 张卡）**文案已解出**：进攻卡 =「可与双方玩家交互的可选效果，你在此战场先手时可选择激活」，
   防御卡 =「你在此战场后手时，这张牌加入你的起手牌」——**正好是我们 `goesSecond` 那套机制的原版说明文字**。
5. **本包 3 个 m_Script**：`8864902669260471755` ×1192（`RawCardScript`）· `-3368977909917641325` ×2（`AlternateArtCard`）·
   `-5832762458676164525` ×1（`AddressablesBuildNumber`，`VersionNumber=1`）。
6. `attackDamageType = 0 Normal` 的 54 张**大多是督军**（`SAU37/SOR3/GOF27/ASH74/UM76/…`），其余 1138 张 = `4 Attack`——
   **如果以后要做「伤害类型 × 护甲/易伤」，这是逐卡判据**。
