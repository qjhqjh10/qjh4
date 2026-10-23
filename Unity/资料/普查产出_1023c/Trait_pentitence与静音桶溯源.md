# Trait · `penitence` 关键词 + 4 个静音桶 溯源

> 调查代理 **TR**（只读）。日期 **2026-10-23**。仓库根 `d:/4`。
> ⛔ 未跑 Unity / player / `-executeMethod`；⛔ 未改任何源码 / 数据 / 生成器 / 台账 / 正本；⛔ 未动 git。
> 判据优先级：`d:/2/tools/decomp_full/`（第一）→ `d:/2/新解包资源/assets_full/` → 卡 SO 导出。

---

## 〇、一句话结论

1. **`penitence` 不是 CardTrait**，它是 **`rage`（`DefinedTrait = 280`）的官方显示名** —— 卡 SO 的 `CardTraits[]` 里写的是 `rage`，`cards_engine.json` 的 `keywords` 按显示名表把它印成 `Penitence`。原版 **没有**任何叫 penitence 的 CardTrait / TraitEffect。
2. **`rage` 的 `traitEffects` 是空数组** ⇒ 原版**没有任何 trait 视觉**要绑。它唯一的表现是**卡自己的 ability 动画** `PenitenceTraitTrigger → PenitenceEffect`（`AbilityTrigger.Rage = 130`）。
3. 🔴 **但本轮「去兜底」之后那 5 条 `penitence` 实际播不出来**：它们被挪进 `ability` 桶，而 `EvtKind.Trigger` 的归口白名单 `IsRoutedTriggerName` 不收 `penitence`、且 target 比的是 `CardAnimTime.TriggerName = "Rage"` —— **两者永不相交**。报告里「用 ability 档照样播」不成立（见 §D）。
4. **4 个 `unbacked` 桶**：逐条核过 —— 卡 SO 在该拍**确实没有**动画字段，猜测的 CardAnim **全库 1,351 张卡 SO 零引用**、`CardTrait` SO 也零引用 ⇒ **删得对，不该恢复**（§B）。
5. **overrides 10 条数据齐**（shield 4 / invulnerable 6，和 `trait_collection.json` 逐条相同、10 件 prefab 全在效果库）；**缺的是消费侧**：`TraitFrames.PrefabFor(trait, trigger)` 没有 army 参数（§C）。

---

## A. `penitence` 这个关键词到底从哪来

### A·1 它是 `rage` 的显示名（每一层都有出处）

| 层 | 事实 | 出处 |
|---|---|---|
| 卡 SO | 5 张卡的 `CardTraits[]` 写的是 **`rage`**（`_trait = 280`），**没有** penitence | `资料/普查产出_1018/allcards_1350/cards_raw.json`（SOR8=`[flank(460), rage(280)]` · SOR10/SOR27/SOR32=`[rage]` · SOR21=`[camouflage(985), rage]`） |
| 枚举 | `DefinedTrait.rage = 280` · `flank = 460` · `camouflage = 985`；**全枚举无 `penitence`** | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:26 / :46 / :96` |
| 卡池显示名 | `TRAIT_DISPLAY["rage"] = "Penitence"` | `Unity/工具/gen_cards_engine.py:2002`（同段注释 `:1986-1987` 写清判据 = 官方本地化 + PnP `SOR10` 亲读） |
| 卡池产物 | `cards_engine.json` SOR8 = `keywords:["Flank","Penitence"]`；SOR10/27/32 = `["Penitence"]`；SOR21 = `["Camouflage","Penitence"]` | `Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json` |
| 官方本地化 | **`Card_Trait/rage` = `["Penitence","忏悔"]`**；`Card_Trait/penitence` **0 条** | `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`（`terms["Card_Trait/rage"]`） |
| 我们的 Loc | 手写了一条**别名键** `Card_Trait/penitence` = 忏悔/Penitence（原版没有这个键） | `Unity/MyGame/Assets/CardPresentation/Core/Loc.cs:2668` |
| 卡面图标别名 | `{ "penitence", "rage" }` / 反向 `{ "rage", "penitence" }`（卡面写 Penitence、画 rage 那枚图） | `Core/Badges.cs:237` · `:281` |

**结论**：`penitence` 是**显示层改名**，规范键（引擎里）就是 `penitence`（`CardDef.cs:4737 Penitence = "penitence"`；`rage` 只是第二拼法，`CardDef.cs:5759`）。`Keyword` 枚举（`Keyword.cs`）是**单位类型**（Infantry=5/Vehicle=10…），SOR8 的 `keyword:5` 与 penitence 无关。

### A·2 原版别处有没有定义 —— 搜过的词与文件（⛔ 不是「只写没有」）

| 搜什么 | 结果 |
|---|---|
| `d:/2/tools/decomp_full/` 全 26k 个 `.c`：`grep -rli "penitence"` | **0 命中**（`"Rage"` 字面也 0 命中） |
| `d:/2/新解包资源/assets_full/` 文件名 `*penitence*` | **5 个**：`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/PenitenceEffect.json` · `.../Texture2D/Penitence_Icon_VFX.png` · `bundle_cardanimsgeneral_assets_all/MonoBehaviour/PenitenceTraitTrigger.json` · `bundle_soundcollection_assets_all/AudioClip/Penitence.wav` · `.../MonoBehaviour/Trait Penitence.json` |
| 三个相关 bundle 的 JSON 内容 grep `Penitence` | 只有**各自本体**：`PenitenceEffect.json` · `PenitenceTraitTrigger.json` · `Trait Penitence.json`（+ 两张 Material，是 PenitenceEffect 的材质） |
| `Trait Penitence.json` 是什么 | 是 **`AudioCue`**（`clipList=[Penitence.wav]`、`minPitch .95/maxPitch 1.05`），**不是** CardTrait、不带任何视觉 |
| CardTrait SO（`sharedassets0.assets` 导出） | 73 个 `CardTrait`，**无 penitence**；`name=rage` 存在但 `traitEffects: []` |  `Unity/数据/游戏数据/trait_collection.json:1832-1852` |
| 官方本地化 | 只有 `Card_Trait/rage`；**无 `Card_Trait/penitence`** | 同上 |

### A·3 它该不该驱动动画 —— **不该**（原版无 TraitEffect）

- `rage` 的 `traitEffects` = **空数组**（`trait_collection.json:1852`）⇒ 原版 `CardTraitCollection.GetTraitEffectObject(trait, triggerType, army)` 对它**任何一档都取不到东西**。
  - 反编译链：`BattleManager__GetTraitEffect.c:5` → `CardTraitCollection__GetTraitEffectObject.c:18`（先按 triggerType 取 TraitEffect）→ `:38/:49`（再按 army 挑 override / 回落 base）。
- 唯一会播 `PenitenceEffect` 的引用是**卡自己的 ability**：`cardAbilities[*].abilityLogic[0].cardAnimations[0]`，能力 trigger = `Rage(130)`。
  - 逐卡证据（6 张，含池外 1 张）：`SOR8/SOR10/SOR21/SOR27/SOR32` + **`752141bb…` = `Hallowed Martyrs`（Sororitas 附魔，cardType=20，不在池内）**，GUID 均为 `230c929a…` → `PenitenceTraitTrigger`。
  - 播法：`AbilityLogic__PlayAnim.c:45-46`（遍历 ability 的 `cardAnimations` 列表）→ `:232/:273 BattleManager.AddCardAnim`。
- `PenitenceEffect` 自带音效：AnimFX `sounds[0].sound = "Trait Penitence"` → `Penitence.wav`（`Unity/数据/游戏数据/animfx_modules.json` 的 `PenitenceEffect` 段 · `animfx_components.json` 同）。⇒ **播 prefab 才有声**，不播 = 连声音也没了。

**⇒ `trait_effect_bindings.json` 里没有 `penitence` 行是【对的】**（不是漏了）；缺的不是数据，是消费侧的归口（§D）。

---

## B. 4 个 `unbacked` 桶：权威源再挖一层

### B·0 卡 SO 的**全部**动画字段（逐字段清点，避免「只量一种情况」）

`RawCardScript` 里所有可能挂动画的字段（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RawCardScript.cs`）：
`:52 summonAnimDelayed` · `:60 customRangedAttackParticles` · `:62 customDeathParticles` · `:64 remnantBodyPrefab` · `:91 targetedSpellAnim` · `:93 nonTargetedSpellAnim` · `:187 whileInPlayAnim(bool)`；`CardScript` 侧还有**共享**的 `:1332 normalSummon` / `:1335 legendarySummon` / `:1374 attackHitSmallParticles` / `:1377 attackHitBigParticles` / `:1398 rangedAttackParticlesSO`（后五个在 **CardPrefab**，不是逐卡）。
逐卡 `cardAbilities[].trigger` 用 `AbilityTrigger` 枚举（`AbilityTrigger.cs:3 ThisCardPlayed=0 · :14 UnitAttack=50 · :15 UnitDamaged=60 · :19 OtherUnitDeath=90 · :23 Rage=130 · :68 Reanimated=570`）。

### B·1 `AM30 Manticore` — `play`（unbacked，不播）
- 卡 SO：`abilityCount=0`、`hasActiveAbility=1`；`cardAbilities` **为空** ⇒ **没有 `ThisCardPlayed(0)` 能力**。
- 它**有**的：`customRangedAttackParticles` = `Atk_Artillery_manticore`（→ `attack_ranged` 桶）；`activeAbility.activeAbilityLogic[0].cardAnimations[0..2]` = `DutyAnim` / `Artillery Up Manticore 2x` / `Artillery Ground Manticore 2x spread`（→ `ability` 桶，主动技能用的时候播）。
- 猜的 `Manticore Board Down`（CardAnim guid `2bb742a0…`）：**1,351 张卡 SO 引用数 = 0**；它的 prefab `Artillery Manticore down 1x`（guid `e3fb0804…`）只被 CardAnim `Manticore Board Down` + `Artillery Ground Big` 引用，两者都**不被任何卡引用**；`trait_collection.json` 命中 0。
- 原版这一拍：`ThisCardPlayed` 没有能力 ⇒ **没有逐卡播放件**（登场淡入走共享 `normalSummon`，见 B·5）。
- **判定**：确认原版无引用 ⇒ 不播**正确**。

### B·2 `EC29 Heldrake` — `attack_melee`（unbacked，不播）
- 卡 SO：`cardAbilities[0]` trigger=`ThisCardPlayed(0)` → `Rally EC Heldrake`（→ `play` 桶）；`customRangedAttackParticles` = `EC Heldrake Attack`（→ `attack_ranged` 桶）。**没有 `UnitAttack(50)` 能力**，也**没有近战专用字段**（RawCardScript 里根本没有 melee 字段）。
- 猜的 `HeldrakeStrike`（CardAnim guid `39dca211…`）：**1 张卡引用 = `BL53`**（Black Legion 的 `Heldrake Strike` 卡）；`trait_collection.json` 命中 0。
- 原版这一拍：近战走 `CardScript._AttackMeleeAnim_d__357__MoveNext.c` —— 只有**攻击音 + 位移 tween**，**没有任何 `AddCardAnim`/CardAnim 调用**（全文 grep `CardAnim` 0 命中）⇒ 近战**没有逐卡动画**。远程才有：`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:151-167` = 有 `customRangedAttackParticles` 就用它，否则 `RangedAttackParticlesByClanSO.GetRangedClanParticles(clan)`。
- **判定**：无权威源 ⇒ 不播**正确**；EC29 的远程件 `EC Heldrake Attack` 仍在 `attack_ranged` 桶。

### B·3 `SAU3 Flayed One` — `deploy`（unbacked，不播）
- 卡 SO：`summonAnimDelayed` = **空 GUID**（`{"m_AssetGUID":""}`）；`cardAbilities[0]` trigger=`Reanimated(570)` → `FastTraitStart`（→ `ability` 桶）；`customDeathParticles` = `Necrons death explosion` + `remnantBodyPrefab` = `RemnantBody3D Necrons`（→ `death` 桶）。
- 猜的 `Flayed One Summon`（CardAnim guid `53f3e4ca…`）：**1 张卡引用 = `SAU69 Flayed Ones Pack`**；`trait_collection.json` 命中 0。
- 原版这一拍：逐卡 `summonAnimDelayed` 空 ⇒ 只剩**共享** `CardScript.normalSummon` / `legendarySummon`（`CardScript.cs:1332/:1335`；运行期表 `generic.byEvent.deploy` = `Invoke Minion Card Fade Default/Legendary`，`card_vfx_by_card.json` 的 `generic` 段）。
- **判定**：无权威源 ⇒ 不播**正确**。

### B·4 `a84a5b49… Master Interromancer` — `hit`（unbacked，不播）
- 卡 SO：`cardAbilities[0]` trigger=`ThisCardPlayed(0)` → `Atk_SmiteLightning_white_Down`（→ `play` 桶）；`cardAbilities[1]` trigger=`OtherUnitDeath(90)` → `Trait_GainQuest_self`（→ `ability` 桶）；**没有 `UnitDamaged(60)` 能力**，也没有 `customDeathParticles` / 远程件。
- 猜的 `Master Interromancer Hit`（CardAnim guid `7a4f21e3…`）：**1,351 张卡 SO 引用数 = 0**；`trait_collection.json` 命中 0。它落的 prefab = `Psychic_Lightning_down_Intense_white`（guid `9d7fa450…`），而**同一件 prefab 也被 `Atk_SmiteLightning_white_Down` 引用**（后者 = 这张卡的 `play` 权威件）⇒ **同一件视觉没丢，只是挪到正确的那一拍（play）**。
- 原版这一拍：`hit` 桶 = `UnitDamaged(60)` 的**技能动画**，这张卡没有 ⇒ 逐卡无件；受击通用粒子是共享 `CardPrefab.attackHitSmallParticles`（`CardScript.cs:1374`；`generic.byEvent.hit` = `AttackHitSmall`）。
- **判定**：无权威源 ⇒ 不播**正确**。

### B·5 四个事件「原版到底怎么取动画」—— 一张对照表

| 我们的事件 | 原版取法（反编译） | 逐卡字段 | 共享兜底 |
|---|---|---|---|
| `play` | `AbilityTrigger.ThisCardPlayed(0)` 的能力 → `AbilityLogic.PlayAnim` 遍历 `cardAnimations`（`AbilityLogic__PlayAnim.c:45-46`） | `cardAbilities[]` | **没有**（运行期表 `generic.byEvent` 无 `play`） |
| `attack_melee` | 近战协程只有音效+位移，**无 CardAnim**（`CardScript._AttackMeleeAnim_d__357__MoveNext.c` 全 313 行无 `CardAnim`/`AddCardAnim`） | **无字段** | `attackHitSmallParticles`（命中那一拍，CardPrefab） |
| `attack_ranged` | 有 `customRangedAttackParticles` 用它，否则按 clan 表（`…ResolveAttackRangedAnim…MoveNext.c:151-167`） | `customRangedAttackParticles` | `RangedAttackParticlesByClanSO` |
| `deploy` | 共享 `normalSummon`/`legendarySummon`（`CardScript.cs:1332/:1335`） | `summonAnimDelayed` | `Invoke Minion Card Fade Default/Legendary` |
| `hit` | `UnitDamaged(60)` 能力 → `PlayAnim` | `cardAbilities[]` | `attackHitSmallParticles/Big`（`CardScript.cs:1374/:1377`，且原版两档**同 pathID**） |

⚠️ `cardanim_clips.json` / `card_anim_map.json` **都是按 CardAnim 名建的表，不含「卡 × 事件」**（`cardanim_clips.json` 元数据 `what` 明写「键 = `a`（CardAnim 名）」；`card_anim_map.json` 是 `阵营 → CardAnim → animInfo`）⇒ **不能**用它们证明「某张卡在这一拍播哪件」。被删的 4 件在两张表里都**有**行（说明是**真资产、只是没卡引用**），不是拼错名。

---

## C. `trait_effect_bindings.json` 的 10 条 `overrides`

| 项 | 结论 | 出处 |
|---|---|---|
| 条数 | `invulnerable` 6（army 50/70/60/90/100/130）· `shield` 4（army 40/70/80/120）| `Resources/trait_effect_bindings.json` rows 里 `overrides` 非空的 2 行 |
| 和原版一致 | **逐条相同**（army / effectGuid / effectAnim 三列都对）| `数据/游戏数据/trait_collection.json` 的 `invulnerable` / `shield` → `traitEffects[0].overrideByArmies` |
| prefab 解析 | **10/10 都解得出**，且 10 件都在效果库里（`InvulnerableEffectDark/Tau/Tyranid/GSC/Guard/SW` · `ShieldTraitEffect_Idle {TAU,Necron,Sororitas,Emperor's Children} Variant` 各 1 行）| `数据/索引/anim_address_map.json` 的 `cardanim_to_asset` · `MyGame/Assets/WarpforgeVFX/效果库报告.tsv` |
| base 也对 | `InvulnerableTraitPlay→InvulnerableEffect` · `ShieldTraitPlay→ShieldTraitEffect_Idle` | 同上 |
| **缺哪一侧** | 🔴 **消费侧**：`TraitFrames.PrefabFor(trait, trigger)` **没有 army 参数**；唯一生产调用点 `BattleDriver.cs:20426 tf.SyncKeywords(u.Keywords.Keys)` 只传关键词串 | `Core/TraitFrames.cs:416` · `:71-73 / :164-165`（代码自己写着「运行期还没消费」） |
| 原版语义（判据） | `TraitEffect.GetEffect(army)`：命中 `overrideByArmies[].army` 就用变体，**否则回落 base**（`TraitEffect__GetEffect.c:13-41`，`:18` 是回落、`:32` 是 army 比较）| `d:/2/tools/decomp_full/TraitEffect__GetEffect.c` |
| 今天可见影响 | `shield` 池内 13 张，落在「有变体」阵营的 **6 张**（EmperorsChildren 1 · TauEmpire 5）现在播的是 base；另 7 张（AstraMilitarum 2 · BlackLegion 1 · DarkAngels 2 · Goff 1 · SpaceWolves 1）**本来就该播 base**。`invulnerable` 池内 **0 张**（数据要留，铁律 11）| `cards_engine.json` 按 `keywords` 统计 |

**⇒ 「缺哪一侧」= 不是数据缺，是 `PrefabFor` 少一个阵营形参 + 两个调用点（`CardView` / `BattleDriver`）。**

---

## D. 顺带查出的**新问题**（比 A/B/C 更要紧）

### D·1 🔴 5 条 `penitence` 现在**实际不播** —— 报告「用 ability 档照样播」不成立

链条（逐段有出处）：

1. 引擎发 Penitence 触发：`RuleCore.HurtCore` → `FireTriggerOnBoard(ctx, u, KeywordTable.Penitence)`（`RuleCore.cs:7943`），`KeywordTable.Penitence = "penitence"`（`CardDef.cs:4737`）。
2. 事件带的关键词就是它：`ctx.Emit(EvtKind.Trigger, …, keyword: keyword, …)`（`RuleCore.cs:10651-10653`）。
3. 表现层：`BattleDriver.PlaySignal` 取 `triggerKeyword = e.Keyword`（`BattleDriver.cs:18033`）、`EvtKind.Trigger` → `evt = VfxMap.Trigger` + `perCardOnly = true`（`:18022-18025`）。
4. `VfxMap.ResolveAllTimed` 对 `Trigger` 走 `TryResolveTriggerPicks`（`VfxMap.cs:1184-1186`）。
5. 目标 trigger 名的判定（`VfxMap.cs:1949-1960`）：
   - `keyword = "penitence"`，`IsRoutedTriggerName("penitence")` = **false**（白名单 `:1816-1857`，**没有** Rage / Penitence）；
   - `else if` 只在 **keyword 为空**时才按名字推断 ⇒ **走不到**；
   - ⇒ `target == null` ⇒ `picks = trig`（**只回 `trigger` 桶**）、`return hasTrig`。
6. 结果（现读运行期表）：
   - `SOR8`：`trigger` 桶 = `FlankEffect` ⇒ 在**忏悔那一拍播 `FlankEffect`**（错件）；
   - `SOR10 / SOR21 / SOR27 / SOR32`：`trigger` 桶**空** ⇒ `hasTrig=false` ⇒ `perCardOnly` 返回空（`VfxMap.cs:1192`）⇒ **什么都不播**。
7. 想让它播的两道坎（**两道都要过**）：① `IsRoutedTriggerName` 得放行 `penitence`；② 放行后 `target="penitence"`，而候选钉出来的 `CardAnimTime.TriggerName` = **`"Rage"`**（`cards_engine.json` 的 `animTimes[0].tn = "Rage"`，5 张全一样），`NameIs("Rage","penitence")` = false ⇒ **仍然筛不出来**。必须有一条 `penitence → Rage` 的归一（或生成器把 `AbilityTrigger.Rage` 归到 `trigger` 桶）。

**⇒ `notPlayed` 里把 5 条标成 `outcome:"duplicate"`（`Resources/card_vfx_by_card.json` 的 `notPlayed[]`）在数据上是对的，但「它已经在 ability 桶 ⇒ 不会丢」这个推论在消费链上不成立。**

### D·2 其余 4 条 `unbacked` 没有同类问题
`hit` 桶由 `PlayUnitSkillAnim(e, VfxMap.UnitDamaged, …)` 消费（`BattleDriver.cs:18011`），`play` 由 `EvtKind.Ability/Play` 各自桶消费（`:17268 / :17250`），`attack_melee` 由 `EvtKind.Attack` 消费（`:17252`），`deploy` 由 `:17251` + `:19622`。这些桶都**按自己那个事件直接取**，不经过 trigger 归口 ⇒ 删掉 guess 就是「这一拍没有逐卡件」，语义正确。

### D·3 `Hallowed Martyrs`（池外）同样脆弱
`752141bb…`（Sororitas 附魔，**不在池内**）的 `cardAbilities[1]` 也引用 `PenitenceTraitTrigger`（`cards_raw.json`）。若将来进池，同样会踩 D·1。

---

## E. 没查清 / 判据空

1. **没有原版实况截图**证明「Rage 触发时 PenitenceEffect 真的播」—— 结论是**反编译链 + 卡 SO 引用**推的（`RawCardScript.TriggerAbilities → AbilityLogic.PlayAnim`），⛔ 不是实拍。
2. `SOR8` 那一拍是否真的会播到 `FlankEffect`，依赖 `trigger` 桶粗粒度行为（本报告按代码静态推）；**未进游戏验证**。
3. `invulnerable` 池内 **0 张** ⇒ 就算把 overrides 接上，今天也**看不到**差别（只能靠断言验收）。
4. `Penitence.wav` / `Trait Penitence` 这条音效我们**有没有接**（AudioCue 播放链）—— 本报告只确认了「它绑在 `PenitenceEffect` prefab 内」，**没有**去核我们 AnimFX 音效模块是否真会放它。

---

## F. 还该做什么（按能不能做分档）

**能修（判据齐，建议排进下一批）**
1. **修 D·1**：让 5 张卡的 `PenitenceEffect` 在 Penitence 触发时播出来。最小改动面 = `VfxMap.TryResolveTriggerPicks` 的 target 归一（`penitence → Rage`）+ `IsRoutedTriggerName` 放行；**或**在 `工具/gen_card_vfx_by_card.py` 的 `TRIGGER_EVENT` 把 `AbilityTrigger.Rage` 归到 `trigger` 桶（会更粗、SOR8 会双播 Flank，需主对话裁）。⛔ 改前先补一条断言：`VfxMap.ResolveAllTimed("trigger", "Sororitas", "SOR10", animTimes, perCardOnly:true, keyword:"penitence")` 必须含 `PenitenceEffect`。
2. **修 D·3 的数据面**：`Hallowed Martyrs` 若进池，一并覆盖（同一处修法自然覆盖）。
3. **接 C 的 overrides**：给 `TraitFrames.PrefabFor/PrefabsFor` 加 `army`（int），改 `BattleDriver.cs:20426` + `CardView` 侧调用；先补断言（`shield`+`TauEmpire(70)` ⇒ `ShieldTraitEffect_Idle TAU Variant`、`invulnerable`+`BlackLegion(50)` ⇒ `InvulnerableEffectDark`、未知阵营回落 base）。

**该改文档（铁律 5，主对话写正本）**
4. `资料/普查产出_1023c/Trait权威化_执行报告.md:68` 那句「**用 `ability` 档照样播**」→ 改成「数据在 `ability` 桶，**但 `Trigger` 归口目前取不到**（见本报告 §D）」；§二表格里 5 条 `penitence` 的 `outcome` 至少加注「duplicate-but-unreachable」。
5. `Trait权威化_执行报告.md:252`（§十·1「penitence 来源未查清」）可结案 → 指向本报告 §A。

**判据空 / 无需做**
6. 4 个 `unbacked` 桶**不需要恢复**：本报告 §B 已逐条证明「该拍无字段 + 全库零引用」；可把 `notPlayed[]` 的 `why` 补上「搜过 1,351 张卡 SO + 73 CardTrait 的 traitEffects」（更可核，不改行为）。
7. `rage` 没有 TraitEffect ⇒ `trait_effect_bindings.json` **不该**加 penitence 行（别为了「补全」去造一行）。

**需要远端/真机**
8. 想拿「原版 Rage 触发时到底播哪一件」的**实拍**只剩远端/真机一条路（本地原版已关服，见 `资料/真Play待验清单.md`）。
