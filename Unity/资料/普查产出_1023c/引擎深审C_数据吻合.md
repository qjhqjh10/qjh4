# 引擎深审 C · `cards_engine.json` ↔ 引擎/表现层 数据吻合

> 只读审查（2026-10-23）。**没跑 Unity / player / ruleprobe**（后台有批处理）。
> 判据 = 生成器 `Unity/工具/gen_cards_engine.py`（4,852 行）· 数据 `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` ·
> 原件 `Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张当前 build SO）·
> `d:/2/新解包资源/assets_full/` · 消费端 `RuleEngine/**` + `CardPresentation/**`。
> 所有数字都是**本轮现读**（脚本在 `_tmp_view/*.py`，用 `python -I`）。

## 〇、现读数（现读，别抄旧文档）

| 项 | 值 |
|---|---|
| 卡池张数 | **1,127**（`count` 字段 = 数组长度 = 1127，一致） |
| 表 version | **17** |
| 池内 id 在 `cards_raw.json`（1,351）里命中 | **1127 / 1127**（0 张缺） |
| 顶层列（card 对象键） | **39** |
| 派生/嵌套子字段 | `animTimes` 13 键 × 1,466 行（873 张）· `abilityTargets` 2 键 × 1,270 行 · `unitConditions` 10 键 × 218 行（167 张）· `abilityContext` 4 键 · `triggerPriority` 2 键 · `countSet` 37 键 · 顶层 `talentNameIds` 186 行 |
| DTO 里声明但表里没有的列 | `skipDraws` / `initialHandModifier` / `initialManaModifier` / `initialSpiritStoneModifier`（4 个，休眠机制，缺键 = 0/空表） |

---

## 一、结论（先说数）

| 类 | 条数 | 说明 |
|---|---|---|
| **真漂移** | **1** | `goesSecond` 缺 6 位督军（`EC55/EC56/EC57/SW1/SW2/SW3`）。**其余 38 个顶层列与原件逐张 0 漂移**（见 §三） |
| **`新账-85`（已知样本）** | **误判** | `animTimes` 与 **GUID 链到的那份 CardAnim** 逐字段 1,466/1,466 一致；被拿来比的是 `Master Interromancer Hit`，那是 **`card_vfx_by_card.json` 里判为 `unbacked`、生产永不播** 的同 prefab 名替身。**⛔ 不要照它改 `animTimes`** |
| **死列（无任何运行期消费者）** | **10** | `cardTraits` · `attackDamageType` · `cardShortId` · `cantBeRandomlyCreated` · `expansionPack` · `refNameId` · `itemDescriptionKey` · `eventSpriteName` · `targeted` · `countSet`（子字段） |
| **半死 / 作者期才用** | 3 | `fullHeroSpriteReference`（只在生成 `CardArt._deckArtById` 那一刻当判据，运行期不读）· 顶层 `note`（DTO 里根本没有）· `count` / `source`（只作 version 守卫与信息） |
| **口径不一致（生成器 vs 消费端）** | **6** | 见 §四（最重的 = 引擎拿**动画行**当「这条 ability 在不在」的闸） |
| **数据退化（口径一致但机制空转）** | 2 | `initiative` 全池恒 20 · `eventSpriteName` 全空 |

---

## 二、消费率总表（逐列）

> 「读点」= 非注释的**生产**代码（不含 `Editor/RuleEngineTest*` 自检、不含 `CardDef.cs` 自身）。
> 数字 = 该列对应的 `CardDef` 属性在 `RuleEngine/**` + `CardPresentation/**` 的命中处数（重复计入，仅供量级参考）。

| 列 | 读点（文件:行号 · 数量） | 结论 |
|---|---|---|
| `id` | `Core/BattleContext.cs:642` 等 · 260 | **在用**（`CardDatabase.Parse` 主键；缺 id 直接跳过） |
| `name` | `Core/ActionPump.cs:204` 等 · 749；`CreatePool.BuildNameIndex` | **在用**（显示 + 按名指目标） |
| `type` | `Core/CardCriteria.cs:172` 等 · 119 | **在用**（unit/tactic/hero/defence） |
| `cost` | `Core/CreatePool.cs:179` 等 · 76；`RuleCore.CostOf` | **在用** |
| `attack` | `Core/EffectResolver.cs:2415` 等 · 71 | **在用** |
| `health` | `Core/EffectResolver.cs:1295` 等 · 100 | **在用** |
| `ranged` | `Core/ConditionEval.cs:267` / `Data/SimpleAI.UseRanged` · 22 | **在用** |
| `keywords` | `CardDef.KeywordTable.Parse/Normalize` | **在用**（707 张非空；**91 个前缀里 0 条认不出**，静态核过） |
| `desc` | `Core/CreatePool.cs:1154` · `EffectText` 解析 · 45 | **在用**（引擎唯一效果正文来源） |
| `faction` | `Core/CreatePool.cs:177` 等 · 111 | **在用** |
| `rarity` | `Core/DeckRules.cs:496` / `Data/DeckBuilder.cs:98` · 46 | **在用** |
| `subtype` | `Core/ConditionEval.cs:484` / `CreatePool.cs:1139` · 21 | **在用** |
| `armour` | `Core/CardDef.cs:812`（`ArmourFromPool` → 写回 `_keywords[armour]`） | **在用** |
| `cardTraits` | **0**（只有 `Editor/RuleEngineTest.cs:2554` 与注释） | **死列**（`CardDto` 自述「本列不喂引擎」，实测属实） |
| `inventoryOptions` | `Core/DeckRules.cs:203,281,299` · 6 | **在用** |
| `showInCollectionOptions` | `Core/DeckRules.cs:281,299` / `Deck/DeckEditorState.cs:574` · 3 | **在用** |
| `initiative` | `Core/FirstSeatRules.cs:63,90` · 2 | **在用但空转**（全池恒 20 ⇒ `ByInitiative` 恒 `null`） |
| `attackDamageType` | **0** | **死列**（引擎整条没有伤害类型概念） |
| `cardShortId` | **0** | **死列**（文档也写了「⛔ 不许拿它当卡 id」） |
| `useOverDraw` | `Battle/CardDisplayWindow.cs:1394` / `Shell/CardDetailPopup.cs:903` · 2 | **在用** |
| `cantBeRandomlyCreated` | **0** | **死列** + **疑似缺口**（166 张 =1，而 `CreatePool.Resolve` 无等价闸；原版读取点未证） |
| `expansionPack` | **0** | **死列** |
| `refNameId` | **0**（全池 1,127 张全 `""`） | **死列**（原件同为空：非空那 100 条**都不在**我们池里，核过） |
| `itemDescriptionKey` | **0** | **死列**（39 张防御卡有值，无消费） |
| `fullHeroSpriteReference` | 运行期 **0**；`Core/CardArt.cs:459/504` 只是**注释里的判据**，表是 C# 硬编码 | **半死**（作者期用过一次，运行期不读） |
| `eventSpriteName` | **0** 且**全池 1,127 张全 `""`** | **死列 + 数据退化**（见 §四·6） |
| `targeted` | **0** | **死列**（`AiScoring.cs:70` 那个 `dto.targeted` 是**另一份**数据，不是本列） |
| `choicesTargetsAffected` | `CardDef.cs:564`（`HasSpecificCardChoice`）→ `EffectText.cs:1801` | **在用** |
| `choicesMaxTargets` | 同上 | **在用** |
| `choicesSpecificCardIds` | `Core/EffectText.cs:1813` | **在用** |
| `animTimes` | 引擎：`Core/EffectResolver.cs:6281-6293`（Turn* 闸）· 表现：`Battle/BattleDriver.cs:17904` / `VfxMap` / `CardAnimClips` | **在用（双端）** |
| `abilityTriggers` | `Core/ConditionEval.cs:342`（条件 241/242） | **在用** |
| `abilityTargets` | `Core/EffectText.cs:1474 → 1663`（两道闸） | **在用（有条件）** |
| `nameZh` / `descZh` | `CardText.cs:752` / `DeckEditorState.cs:522` / `BattleDriver.cs:21694` | **在用（显示层）** |
| `unitConditions` | `Core/EffectText.cs:1626` → `EffectResolver.ResolveTargets` | **部分在用**（见下） |
| `abilityContext` | `Core/ActionPump.cs:301,302` / `Core/RuleCore.cs:10718` | **在用** |
| `goesSecond` | `Core/CreatePool.cs:695` / `Core/RuleCore.cs:757` | **在用（缺 6 人）** |
| `triggerPriority` | `Core/ActionPump.cs:615` | **在用**（4 张卡） |
| `talentNameIds`（顶层） | `Data/CardDatabase.cs:611-620` → `TalentCardData.SetNameMap` | **在用** |
| `version` / `count` / `source` / `note`（顶层） | `version` 只做 `< N` 出声守卫；`count`/`source`/`note` **0 读**（`note` 连 DTO 都没有） | **元数据（`note` 纯体积）** |

### 子字段级

| 子字段 | 消费 | 结论 |
|---|---|---|
| `animTimes.a/.n` | `CardAnimClips`（键 = `a`）· `VfxMap` join | 在用（**⛔ 按 `n` 查会错，见 §四·4**） |
| `animTimes.tn` / `.tg` | `HasAnimTrigger` / `CardAnimTime.Trigger` | 在用（引擎 Turn* 闸靠它） |
| `animTimes.d/s/m/e/u/t/o` | `CardAnimClips` / `VfxMap` | 在用 |
| `animTimes.sd`（startDelay） | **0** | 落值无消费（原版用途也没查到 ⇒ 判据不足） |
| `animTimes.rc` | **0**（表现层读的是 `card_vfx_by_card.json` 里另一份 `rc`） | 冗余落值 |
| `abilityTargets.trigger` | 只作可读性（消费端按下标对齐） | 半用 |
| `abilityTargets.ta` | `EffectText` | 在用 |
| `unitConditions.ab/lg/role/cond/ex` | `EffectResolver`（仅 `role ∈ {target, acting}`） | 部分：**163 / 218 行**在已接的 role 里 |
| `unitConditions.countSet`（37 键） | **0**（`CardDef.TryGetCountSet` **无调用点**） | **死子列**（6 行 `count`、1 行 `multi` 读不出「数谁」） |

---

## 三、漂移表（数据 vs 原件）

### 3·1 逐列对账（1,127 张全量，`cards_raw.json` 的 `full_raw` 为原件）

| 列 | 不一致 | 判定 |
|---|---|---|
| `health` ← `maxHealth` | **0** | 一致 |
| `attack` ← `meleeAttack` | **0** | 一致 |
| `cost` ← `manaCost` | **0** | 一致（含 4 张 `-1` 附魔模板） |
| `ranged` ← `rangeAttack` | **0** | 一致（原本以为「不动」，实测原件本来就相等） |
| `armour` | **0** | 一致（生成器走 PnP 层，结果与原件 `armour` 全同） |
| `inventoryOptions` / `showInCollectionOptions` | **0 / 0** | 一致 |
| `initiative` | **0** | 一致（原件 1,349×20 + 2×30，那 2 张不在池里） |
| `attackDamageType` / `cardShortId` / `useOverDraw` / `cantBeRandomlyCreated` / `expansionPack` / `refNameId` / `itemDescriptionKey` / `targeted` | **0**（逐列） | 一致 |
| `cardTraits` | **0**（含 `TRAIT_DISPLAY` 显示写法逐条重建比） | 一致 |
| `choicesTargetsAffected` / `choicesMaxTargets` / `choicesSpecificCardIds` | **0 / 0 / 0** | 一致 |
| `abilityTriggers` | **0** | 一致 |
| `abilityTargets` | **0** | 一致 |
| `abilityContext` | **0**（157 张） | 一致 |
| `unitConditions` + `countSet` | **0**（218 行逐字段） | 一致 |
| `triggerPriority` | **0**（4 张） | 一致 |
| `animTimes`（含 `a/n/d/sd/s/m/e/u/t/tg/o`） | **0 / 1,466 行** | **与 GUID 链到的 CardAnim 原件逐字段一致** |
| `type` / `faction` / `rarity` | 映射 1:1（`cardType` 0/10/20 → unit/hero/tactic|defence · `cardArmy` 13→13 · `cardRarity` 0/1→common…5→special，**97.5% 单射、0 例外**） | 一致 |
| `name` | **40 张**与原件 `cardName` 不同 | **设计如此**（2026-10-18 官方显示名）；其中 `e59a…` 原件的 `cardName` 本身是错的（资产 `m_Name` = Skyborne Deployment，`cardName` 却写 Path of the Warrior） |
| `keywords` | 5 条真值 trait 没进 `keywords` | **分层设计**（4 条被 `cardface_fixes` 人工层覆盖：`AM72 slay` / `SAU7 unstoppable` / `GOF57 flank` / `e01f… ephemeral`；1 条 `UM100 oathDouble` 引擎未登记） |
| `eventSpriteName` | **1,127 / 1,127 都是 `""`** | **数据退化**：当前 build 的 `full_raw` **没有这个键**（0/1351），生成器只能写空；旧快照的 54 条 `Warlord_events_Loken` 已不可得 |

### 3·2 `goesSecond` —— 唯一真漂移

| 列 | 卡/行 | 数据里的值 | 原件值 | 影响 | 判定 |
|---|---|---|---|---|---|
| `goesSecond` | `EC55 Lucius The Eternal` · `EC56 Lord Kaphrael` · `EC57 Xarahan, Daemon Prince` · `SW1 Logan Grimnar` · `SW2 Njal Stormcaller` · `SW3 Ragnar Blackmane` | `""`（= 没落） | `goSecondCardInHand` PPtr = `8888490285459141610`（→ **EC59 Vial Tanks**）/ `-6738157117769915668`（→ **SW59 Orbital Defences**），**两张都在池里** | 这 6 位督军**后手开局拿不到防御卡**（`RuleCore.GoesSecondCard` 第 ③ 档「出声 + 不给卡」） | **真漂移**（中间表 `数据/游戏数据/warlord_goes_second.json` 是 2025-07-30 旧快照、只有 55 位，EC / SW 两族整族不在） |

### 3·3 `新账-85` 复核 —— **误判**，并给出「同类还有多少」

| 项 | 实测 |
|---|---|
| `cards_engine` 该行 | `a84a5b49f0ca9144585058db6987c2a0` 的 `animTimes[0]` = `a=Atk_SmiteLightning_white_Down` · `s=0.65` · `m=0` · `u=0.65` |
| 它 GUID 链到谁 | `cardAbilities[0].abilityLogic[0].cardAnimations[0]` 的 GUID `39d3e084…` → `bundle_spacemarinesdarkangelscardanims/MonoBehaviour/Atk_SmiteLightning_white_Down.json` ⇒ `timeAtStartPos=0.65` · `timeMoving=0` ⇒ **逐字段一致** |
| 账里拿来比的 `Master Interromancer Hit` 是谁 | GUID `9d7fa450…`，**`cards_raw.json` 全库 0 处引用**；`card_vfx_by_card.json` 的 `notPlayed` 里写着 `outcome=unbacked`「卡 SO 与 CardTrait SO 都没有引用它 ⇒ 无权威依据、**不播**」 |
| 生产实际播的 | `card_vfx_by_card.json` → `play` = `Atk_SmiteLightning_white_Down`（0.65）；`ability` = `Trait_GainQuest_self`（0.7） |
| ⇒ 结论 | **`animTimes` 是对的**；账里那句「谁按 `animTimes` 算时长就会多算 0.35 s」**不成立**（0.65 与 0.3 属于**两条不同的 CardAnim**，只是 prefab 同名） |

**同类统计（这次会踩坑的形状）**：

| 指标 | 数 |
|---|---|
| `cardanim_clips.json` 里 prefab 名 → **多张 CardAnim 共用**的组 | **184 组**（其中 **70 组**参数不同） |
| 池内「生产会播的 (卡, 事件) 条目」中，prefab 名落在这 184 组里的 | **586 / 2,672 条目 · 424 张卡**（只要有谁**按 prefab 名**查时长，这 586 条就会取错） |
| `card_vfx_by_card.json` 的 `notPlayed` 里 **`unbacked`（按名字猜出来的）** | **6 条**；其中**与别的 CardAnim 共用 prefab 且参数不同**的 = **3 条**（`Master Interromancer Hit` · `Forewarned Board` · `Manticore Board Down`）—— 这 3 条就是「下一个 `新账-85`」 |

> 🔴 旁注：`Editor/BattleScene.cs:33326-33348` 那张「16 个受害桶」自检表里，第 13 行 `Master Interromancer Hit → 0.3` 断的是**一条生产永不播的 CardAnim**。0.3 对那条资产本身没错，但它**证明不了任何生产行为**（自证/死值的同类问题）。真正该断的是 `play` 那件 `Atk_SmiteLightning_white_Down → 0.65`。

---

## 四、口径不一致（生成器 vs 消费端，逐条）

1. **🔴 引擎拿「动画行在不在」当「这条 ability 在不在」的闸（影响行为）**
   `CardDef.HasAnimTrigger` 是 `EmitCardTrigger` / `EmitTurnTrigger` / `EmitFollowUpResolution` 的**唯一闸**（`EffectResolver.cs:6145/6110/6293`）。
   而真值是 `abilityTriggers`（原版 `RawCardScript.HasAbilityType`，就是 `cardAbilities[].trigger` 的集合）。
   实测缺口（池内 1,127 张，按 `abilityTriggers` vs `animTimes[].tg` 现比）：

   | trigger | 有该 ability 的卡 | 有动画行的卡 | **静默不发的卡** |
   |---|---|---|---|
   | 410 `FollowUpResolution` | 30 | 24 | **6** |
   | 31 `TurnStartPlayer` | 7 | 3 | **4** |
   | 30 `TurnStart` | 5 | 3 | **2** |
   | 40/41/500/600/605 | 11/1/4/29/26 | 全中 | 0 |

   例：`GOF86 Unleashed Tramplasquig` / `UM88 Valius Paxor` / `TAU3 Commander O'Maios` / `BL35 Haarken Worldclaimer` 的 `animTimes` 是**空表**，可它们的 `desc` 里有回合起止正文 ⇒ 正文照跑，**那一拍 `EvtKind.Trigger` 从不发**（表现层收不到，监听者也收不到）。代码注释把它写成「发了也没东西播」——**它不只是播不播的问题**，`EmitListenerTrigger` 那条链也走同一个闸。

2. **`unitConditions` 只接了 2 个 role**
   `RoleTakesUnitSubject` 只认 `target` / `acting`（163/218 行）；`count`(6) / `multi`(1) / `mult`(4) / `avail`(3) / `choice`(2) / `cardtarget`(13) / `nontarget`(20) / `targetscore`(6) **共 55 行无消费**。另有 **36 张卡**的条件行**全部**落在这些未接的 role 上（等于这一列对它们完全没生效）。

3. **`countSet` 是死子列**
   v16 落 37 格 / `CardDef.TryGetCountSet` 有实现，但**全仓 0 个调用点**（只有 `RuleEngineTest.cs:34797` 的注释提到）⇒ `EC58` / `SOR70` 那两条 `count` 条件**仍然读不出「数谁」**（生成器注释里「v16 落的是 countSet，消费端下一波才切」在消费端还没切）。

4. **`a` / `n` 双键风险（数据侧已经防住，消费端要守）**
   同一个 prefab 名被多张 CardAnim 共用（184 组 / 70 组参数不同）；`cardanim_clips.json` 头注释明确「键 = `a`，⛔ 不是 prefab 名」，`CardAnimClips.PlacementOf` 也确实按 `a`。**唯一按 prefab 名查时长的地方** = `VfxMap.SharedRangedVfxDuration`（7 条，注释里逐条说明了为什么能这么查）。这条保持现状即可，但 **任何新代码按 `n` 查 `cardanim_clips` 都会错**（影响面 586 条目 / 424 卡）。

5. **`keywords` 的「只补空」与「人工层整列覆盖」会打架**
   生成器 `merge_truth_traits` 的注释（`gen_cards_engine.py:2028-2032`）说「本笔放行的后果：`Unstoppable 1 张（SAU7）`」——**实测 SAU7 `Canoptek Wraith` 的 `keywords` = `['Remnant']`、`Unstoppable` 没进去**：因为 ⑤ 层 `cardface_fixes.json` 整列覆盖成了 `["Remnant"]`。⇒ **那一句注释是过期/错的**（铁律 5）。同类：`AM72 slay`、`GOF57 flank`、`e01f… ephemeral` 也是被人工层覆盖掉的（PNp 优先，**这是既定口径，不是缺陷**，但注释要订正）。

6. **`eventSpriteName` 生成器写的是「原件已经没有的字段」**
   `A1799_FIELDS` 仍从当前快照取 `eventSpriteName`，而 `cards_raw.json` 的 `full_raw` **0/1351 有这个键** ⇒ 全池 1,127 张写 `""`。`CardDatabase.cs:217` 的注释还写着「44 张值全同 `Warlord_events_Loken`」——**过期**（那是 2025-07-30 旧快照的读数）。

7. **`initiative` 数据退化 ⇒ `FirstSeatRules.ByInitiative` 永远走不到分叉**
   原件 1,349×`20` / 2×`30`（那 2 张不在池里）⇒ 池内 1,127 张**全是 20**。`ByInitiative` 只在 `>` / `<` 时返回 0/1，其余 `null` ⇒ **先攻这一档对每一局都是平局**，永远落到 `BattleDriver.FirstSeatForSeed` 的掷硬币。口径没错（数据事实），但「按 initiative 定先后手」这条机制**今天等于没接**。

---

## 五、修复清单

### P0（先订正认知，别让下一个人照错账改数据）

1. **`新账-85` 改判为「误判」**（账目 + `BattleScene.cs:33340-33343` 那条注释）：
   `animTimes` 与 GUID 链到的 `Atk_SmiteLightning_white_Down`（`s=0.65`）**逐字段一致**；
   `Master Interromancer Hit`（0.3）是 `card_vfx_by_card.json` 判为 `unbacked` 的**同 prefab 名替身，生产不播**。
   **⛔ 不要为了「对上 0.3」去改 `cards_engine.json` 或生成器** —— 那会把正确值改错。
2. **`BattleScene.cs` 16 桶自检**：把 `Master Interromancer Hit → 0.3` 换成生产真播的那条
   （`play` 桶 `Atk_SmiteLightning_white_Down → 0.65`），否则这条断言永远是「死值自证」。

### P1（真缺口，判据在手）

3. **`goesSecond` 补 6 位督军**：`EC55/EC56/EC57/SW1/SW2/SW3`。
   判据 = `cards_raw.json` 的 `full_raw.goSecondCardInHand.m_PathID` → `EC59 Vial Tanks` / `SW59 Orbital Defences`（**两张都在池里**）。
   做法：重跑 `工具/gen_warlord_goes_second.py`（换当前 1,351 快照）→ `gen_cards_engine.py` → **`ruleprobe check` rc=0**。
4. **`EmitCardTrigger` 的闸要不要换判据（要拍板）**：410 缺 6 张、Turn* 缺 6 张。
   数据侧现成判据 = `abilityTriggers`（v17 已落、与原件 0 漂移）。改法有两条：① 闸改成 `abilityTriggers` 含该 trigger；
   ② 双闸（`abilityTriggers` 有 **或** `HasAnimTrigger`）并**出声**记「有 ability 没动画」。⛔ 别直接删闸（会掉进「多播」那条坑，见 `EffectResolver.cs:5983`）。

### P2（落值但没消费 / 口径要写清）

5. `countSet`：接进 `EffectResolver`（`EC58` / `SOR70` 两条 `count`），或明确写「v16 只落值、消费端未切」。
6. `unitConditions` 的 8 个未接 role（55 行）：先落账（`targetscore` / `cardtarget` / `nontarget` 是 AI 评分面，另属一条）。
7. `cantBeRandomlyCreated`（166 张 =1）：`CreatePool.Resolve` 无等价闸 —— **判据不足**（原版读取点未找到），先记账、别自己编语义。
8. `eventSpriteName`：当前快照没这个字段 ⇒ 要么删列、要么从旧快照补（**注意旧快照更旧**）；同时订正 `CardDatabase.cs:217` 的「44 张」。
9. `initiative` 恒 20：写进文档「先攻档今天等价于无数据」，别再当「已接的机制」。
10. `keywords` 5 条真值 trait 未进：4 条是人工层覆盖（**保留**，但要订正 `gen_cards_engine.py:2028-2032` 那句「SAU7 Unstoppable 会进」）；`UM100 oathDouble` 保持跳过（引擎未登记）。
11. 顶层 `note`（约 2 KB，`FileDto` 里没有）—— 不影响功能，可考虑不落运行期 JSON。
12. 死列 10 条（`cardTraits` / `attackDamageType` / `cardShortId` / `expansionPack` / `itemDescriptionKey` / `targeted` / …）：
    **都是「原版有真值、引擎没机制」**，按铁律 11 **要做**，但今天**没有判据能证明原版怎么用** ⇒ 逐条落账、等机制。

> ⚠️ **改数据一律走正规通道**：改 `gen_cards_engine.py`（或它的上游 `gen_warlord_goes_second.py`）→ 重跑 →
> `ruleprobe check` **rc=0** → 再跑受影响的 Unity 宿主。**⛔ 不要手改 `cards_engine.json`。**

---

## 六、没查清的（如实写）

1. **`abilityTargets` / `unitConditions` 的实际生效张数**：要过「整卡能力块数 == ability 数 ∧ 块内 op 数 == logic 数」两道闸，
   闸门判定在 C# 解析期，**不跑引擎算不出**（本轮后台 Unity 在跑，按红线没跑）⇒ 只能给「落值规模」（1,008 张 / 163 行）。
2. **`animTimes.sd`（`startDelay`）原版用在哪**：`decomp_full` 里没找到读它的方法（`GetAnimDuration` 明确不含它）⇒ **判据不足**，不敢说「没用」。
3. **`cantBeRandomlyCreated` 的原版消费点**：`allcards_未挖字段盘点.md` 也写「读取点没找到」⇒ 未证。
4. **`subtype` 没有原件可对**：当前快照 `full_raw` 无兵种字段，来源是 OCR/`cardface_fixes` ⇒ 本轮**无法对账**（不是「一致」）。
5. **`keywords` / `desc` 的 PnP 层**：判据在 `d:/2/Warpforge部队卡片/` 成品图，本轮没逐张开图（1,127 张不在本次范围内），只核了「引擎 91 个前缀 0 条认不出」。

---

*脚本：`_tmp_view/drift_check.py` · `drift_check2.py` · `ac_check.py` · `anim_check2.py` · `anim_n_check.py` · `cond_check.py` · `tp_check.py` · `traits_check.py` · `kw_drop.py` · `consume_probe.py` · `consume_probe2.py`*
