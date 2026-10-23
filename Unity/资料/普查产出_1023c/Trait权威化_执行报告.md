# Trait 全量权威化（去兜底）· 执行报告

> 执行代理 **T1**（可写）。日期 **2026-10-23**。仓库根 `d:/4`。
> ⛔ 未跑 Unity 批处理 / 未动 git 写操作 / 未改 `CLAUDE.md` · `项目任务.md` · 任何 `任务总表.md`。
> ✅ 唯一跑过的检查 = `TMPDIR=/tmp/wf_t1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（跑了 4 次，最后一次在所有改动之后）。

---

## 〇、一句话结论

用户指令「安卓存档有 CardTrait SO，把兜底之类的东西都去掉，**全面使用 CardTrait SO**」**已落地**：

- `Resources/card_vfx_by_card.json` 的 `es[].c` **只剩 `authoritative` 一种**（改前 17 条 `inferred-name` + 5 条 `unknown` = **0 条**）；
- 那 22 条**一条不落**记进新的 `notPlayed[]`（16 条「删重复」+ 6 条「无依据」），运行期**逐条 `LogWarning` 出声**；
- 5 条 `penitence` **不再进播放表**，而它们的 `PenitenceEffect` 在卡 SO 的 `ability` 档**仍在**（我们删的是「无依据的触发行」，不是那张卡的动画）；
- `TraitFrames`（状态框两本）与 `TraitParticles`（FromCode 粒子）**改成由 `Resources/trait_effect_bindings.json` 驱动** —— 这张表由**原版 `CardTrait` SO** 生成，**C# 里再没有一条手写的 trait → prefab 绑定**；每行都能追到 `DefinedTrait / TriggerType / effectAnim GUID / CardAnim / prefab / playIfWarlord / overrideByArmies`。
- AnimFXCheck 里新增 **24 条断言**（4 组），其中 **6 条是「灭自证」的反向断言**（手写表被数据推翻的那几条）。

---

## 一、判据（从哪来，逐条可复核）

| 判据 | 路径 | 用在哪 |
|---|---|---|
| **CardTrait SO 普查** | `资料/普查产出_1023b/安卓存档_CardTraitSO普查.md` | 全局口径：73 `CardTrait` / 75 `TraitEffect` / 77 `TraitPair`，安卓 1.35.0 与 PC 关服前**逐字段 0 差异** |
| **CardTrait SO 导出** | `数据/游戏数据/trait_collection.json`（生成器 `工具/export_trait_collection.py`） | 新增绑定表的**唯一来源** |
| **GUID → CardAnim** | `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` | 生成器**逐行交叉校验**：68/68 相符、**0 处不一致** |
| **CardAnim → prefab** | `数据/索引/anim_address_map.json` 的 `cardanim_to_asset` | 两跳解出 prefab；68/68 行有解、**0 行解不出** |
| **卡 SO 全字段** | `资料/普查产出_1018/allcards_1350/cards_raw.json`（1,351 张） | 判「这条推断是不是**重复记账**」：这张卡的 SO 有没有引用**同一条 CardAnim**（guid 比） |
| **反编译（第一权威）** | `d:/2/tools/decomp_full/BattleCardUI__DisplayStatusAnim.c`（314 行，本次逐句读了尾段 `:297-310`） | 证实 `0x186`(flying) / `0x3d4`(vanguard) 的 OnPlay 框原版**真的演**（所以数据驱动后新增的 13 个 OnPlay trait 不是我们多做的） |
| 633 条升权威的前情 | `资料/普查产出_1023b/633条inferred升权威_执行报告.md` | `inferred-keyword` 归零（638→0/633+5）的口径与遗留 |

---

## 二、59 条 `inferred-name` 逐条判（两条出路，⛔ 没有第三条）

判据 = **这张卡的卡 SO 有没有引用同一条 CardAnim**（guid 精确比，不是名字比）。

| 出路 | 条数 | 含义 |
|---|---|---|
| **删重复**（`outcome: "duplicate"`） | **39** | 卡 SO **已把同一条 CardAnim** 以 `authoritative` 记在**别的桶**上 ⇒ 这条是按名字猜出来的**第二次记账**，删掉（否则同一件会播两次） |
| **无依据不播**（`outcome: "unbacked"`） | **25** | 卡 SO 与 `CardTrait` SO **都没有引用**这条 CardAnim ⇒ 无权威依据，**不播**（⛔ 不拿名字相近的顶替） |
| 合计 | **64** | = 59 `inferred-name` + 5 `unknown`；**仍留推断 = 0** |

**其中真正进过运行期表的 22 条**（其余 20 条在 `_unclassified`，改前也不进运行期）—— 这 22 条就是运行期 `notPlayed[]` 的内容：

| 卡 | 事件 | CardAnim → prefab | 出路 |
|---|---|---|---|
| `30007162…` Lord of the Storm | `hit` | `Lord_of_the_storm_hit` → `Sau_LordOfTheStorm` | 删重复（卡 SO 说它属于 `ThisCardPlayed` = `play`） |
| `68909cf0…` Jammed Communications | `hit` | `Jammed Communications Damage` | 删重复（SO：`ability`/`OtherCardPlayed`） |
| `BL42` Traitors' Hate | `play` | `Traitors' Hate Board` | 删重复（同桶、同 prefab） |
| `BL55` Black Crusade | `play` | `BlackCrusade_battleground` | 删重复（SO 有 4 条权威行） |
| `SAU26` Plasmancer | `attack_ranged` | `Plasmancer_Damage` | 删重复（SO：`customRangedAttackParticles`） |
| `SAU50` Dimensional Breach | `play` | `Dimensional_Breach_Battleground` | 删重复 |
| `SAU50` Dimensional Breach | `deploy` | `Dimensional_Breach_Spawn` | 删重复（SO：`play`） |
| `SOR8` Arco-flagellant | `attack_melee` | `Arco-Flagellant Strike` | 删重复（SO：`ability`/`Rage`） |
| `SOR51` Divine Intervention | `play` | `Divine_Intervention_Battleground` | 删重复（SO：`ability`/`UseFaith`） |
| `TL72` Tyranid Invasion | `deploy` | `TyranidInvasion_Spawn` | 删重复（SO：`play`） |
| `d8a14171…` Relentless March | `deploy` | `RelentlessMarchSummon` | 删重复（SO：`play`） |
| `ASH86` Forewarned | `play` | `Forewarned Board` → `Cosmic Serpent` | **无依据不播**（全库 1,351 张卡 SO 里**没有一张**引用这条 CardAnim） |
| `AM30` Manticore | `play` | `Manticore Board Down` | **无依据不播**（同上；SO 里是 `Artillery Ground Manticore 2x spread`） |
| `AM34` Valkyrie | `attack_ranged` | `Valkyrie_MultiLaser_Hover_Target` | **无依据不播**（SO 只被 `AM75 Valkyrie Drop` 引用；AM34 的权威件是 `BulletImpact_ValkyrieMissile`） |
| `a84a5b49…` Master Interromancer | `hit` | `Master Interromancer Hit` | **无依据不播** |
| `EC29` Heldrake | `attack_melee` | `HeldrakeStrike` | **无依据不播**（SO 只被 `BL53 Heldrake Strike` 引用；EC29 的权威件是 `EC Heldrake Attack`） |
| `SAU3` Flayed One | `deploy` | `Flayed One Summon` | **无依据不播**（SO 只被 `SAU69 Flayed Ones Pack` 引用） |
| `SOR8/10/21/27/32`（5 条 `penitence`） | `trigger` | `PenitenceTraitTrigger` → `PenitenceEffect` | **删重复**（卡 SO 的 `ability` 桶 `cardAbilities[0].abilityLogic[0].cardAnimations[0]` 就是它，`Rage(130)`） |

> ② 的验收就是这么过的：**5 条 `penitence` 全部出表** + **`ability` 档仍拿得到 `PenitenceEffect`**（`AnimFXCheck` 里正反两条断言成对）。
>
> 🔴 **2026-10-23 就地订正（铁律 5，TR 只读调查推翻）：上一句的推论「`ability` 档还在 ⇒ 照样播」在**消费链上不成立**。**
> 引擎发的那一条是 `RuleCore.cs:7943 FireTriggerOnBoard(..., KeywordTable.Penitence)`，事件 = `EvtKind.Trigger`；表现层 `BattleDriver.cs:18033` 取 `e.Keyword` → `VfxMap.cs:1184` → `TryResolveTriggerPicks`，而 `VfxMap.cs:1949-1960` 的 `IsRoutedTriggerName("penitence")` = **false**（白名单 `:1816-1857` 没有它） ⇒ target=null ⇒ **只查 `trigger` 桶**。
> 结果：`SOR8` 的 trigger 桶只剩 `FlankEffect` ⇒ **播错件**；`SOR10/21/27/32` trigger 桶为空 ⇒ **这一拍什么都不播**。而且那条 ability 候选的 `TriggerName` 是 **`Rage`**，就算放行 `penitence` 也还要一条 `penitence→Rage` 归一。
> 判据/修法 → `普查产出_1023c/Trait_pentitence与静音桶溯源.md` §〇 · 账 → 特效线 `新账-86`。

---

## 三、读数的前后对比（实测，⛔ 不是估）

| 项 | 改前 | 改后 |
|---|---|---|
| 全量件 `stats.entriesByConf` | `{inferred-name 59, authoritative 4582, unknown 5}` | **`{authoritative 4582}`** |
| 全量件 `stats.cardsByConf` | `{authoritative 1127, inferred-name 42, unknown 5}` | **`{authoritative 1127}`** |
| 运行期 `es[].c` | `{authoritative 2672, inferred-name 17, unknown 5}` | **`{authoritative 2672}`** |
| 运行期 `entryCount` | 2694 | **2672**（−22） |
| 运行期 `notPlayedCount` | （无这个键） | **22**（16 duplicate / 6 unbacked） |
| `hit` 桶 | 4 条 | **1 条**（`BL20`） |
| `deploy` 桶 | 5 条 | **1 条**（`SOR33`） |
| `attack_melee` 桶 | 13 条 | **11 条** |
| `trigger` 桶（卡数） | 424 | **420** |
| `stats.inferenceGone` | —— | `before {inferred-name 59, unknown 5} · droppedDuplicate 39 · markedUnbacked 25 · leftInferred 0` |
| `VfxMap.CardTableUnitAttackEntries / UnitDamagedEntries / UnitAnimEntries` | 12 / **4** / 16 | **12 / 1 / 13** |
| `VfxMap.CardTableMultiEntries / CardsCandidates / Entries` | 500 / 2672 / 2017 | **500 / 2672 / 1995** |

---

## 四、`TraitFrames` / `TraitParticles`：从「手写表」到「数据驱动」

### 4·1 新增的绑定表 `Resources/trait_effect_bindings.json`

生成器 **`工具/gen_trait_effect_bindings.py`**（新文件，`--check` 可只校验不写，`--generated-at` 可固定时间戳）。
现读读数：

```text
rows=75  withPrefab=68  unresolved=7   (effects=75 · guidChecked=68 · guidMismatch=0 · overrides=10)
byTriggerType = {2:18, 3:21, 6:7, 5:10, 0:13, 1:6}
```

每行字段：`trait / definedTrait / triggerType / triggerName / effectGuid / cardAnim / prefab / playIfWarlord / unresolved / overrides[]`（`overrides[]` = `army / effectGuid / cardAnim / prefab`）。

- **7 条 `unresolved`** 全部是「原版 `effectAnim` **本来就是空引用**」（`combo` · `daemonicAllegiance` · `firstStrike` · `large` · `mekaniak` · `piercing` · `resolution`）⇒ **不是缺口**（铁律 11 第 ① 种）。
- **68/68 条 prefab 都在效果库里**（`效果库报告.tsv` 逐名核过；10 条 `overrideByArmies` 的变体件也全在库）。
- **幂等**：同 `--generated-at` 连跑两遍，产物 **md5 相同**。

### 4·2 `TraitFrames.cs`（C# 里手写绑定已清零）

| 改前 | 改后 |
|---|---|
| `PlayFrames`（手写 4 行：stealth/swarm/vanguard/slay） | **删掉** —— 由数据里 `TriggerType == 2 (OnPlay)` 的 16 行派生 |
| `TriggerEffectBindings`（手写 4 行数组） | **删掉** —— 由数据里 `TriggerType ∈ {3,6}` 的 25 行派生 |
| —— | `EnsureBindings()`（`:239`）懒加载 + 失败 `LogError` + `BindingsProblem` |
| —— | `BindingsFor(trait, trigger)`（`:341`）= 两本字典的唯一查询口；`TriggerBindingsFor` 转发它 |

**数据推翻/改判的四条（★ = AnimFXCheck 里有反向断言，抄回去就红）**：

| trait | 改前（手写） | 改后（数据） | 判据 |
|---|---|---|---|
| `stealth` | `StealthEffect`（OnPlay） | **不变** `StealthEffect` | `Stealth.asset` 唯一一条 OnPlay → `StealthTraitPlay` |
| `swarm` | `Swarm_Trigger_OnTarget`（OnPlay） | ★ **OnPlay 那本没有它**（`null`） | `Swarm.asset` 只有 **FromCode(5)** 一条；原版**没有** OnPlay effect |
| `vanguard` | `VanguardIdleEffect`（OnPlay） | **不变** | `Vanguard.asset` 唯一 OnPlay → `VanguardIdleAnim` |
| `slay` | `SlayEffect`（OnPlay，「附魔加身演一次」） | ★ **OnPlay = null；触发那本 = `SlayEffect`** | `Slay.asset` 唯一 TraitEffect 是 **Trigger(3)** → `Slay_Effect_target` |

**另外 13 个 trait 的 OnPlay 框第一次接上**（数据里本来就有，只是手写表没写）：`flying`(77 张) · `shield`(13) · `camouflage`(21) · `stun`/`blind`/`vulnerable`/`huntMark`/`markerlight`/`pindown`/四个 `markOf*`（这几档池内 0 张卡带）。
**反编译佐证**：`BattleCardUI__DisplayStatusAnim.c:297-310` 专门处理 `0x186`(flying) 与 `0x3d4`(vanguard) 两个 trait 的**状态动画**（连带 `ToggleFlying`）⇒ 原版确实为 flying 演 OnPlay 框，⛔ 不是我们多加的。

**顺带被数据扩大的两条口径（如实记，行为变了）**：

- `DedupesTrigger` 现在对 **7 个 trait** 为真（`bloodthirst` + 新加的 `agenda`/`duty`/`ecstasy`/`huntMark`/`pray`/`uprising`，它们各有一条 6）—— 手写表当时只有 `bloodthirst`。
- `BlocksForWarlord`（督军不播）现在对 **`oath` / `talent` / `ecstasy`** 为真（数据里这三条的 `playIfWarlord = false`）。

### 4·3 `TraitParticles.cs`（FromCode 那档）

表从「按名字反推的 7 行」变成 **数据里 `TriggerType == 5` 的 10 行**（复用 `TraitFrames` 那次载入，⛔ 不复读第二份 JSON）：

| trait | 改前（手写） | 改后（数据） | 说明 |
|---|---|---|---|
| `sniper` / `markerlight` / `swarm` / `synapse` / `longrange` | 同 | **不变** | 手写表这几条是对的 |
| `stomp` | `StompEffect` | ★ **`Attack_Stomp`** | 原版 FromCode → `BluntImpact_Stomp_Forward`；`StompEffect` **不是任何一条 TraitEffect 的 prefab** |
| `ferocity` | `FerocityEffect` | **`Ferocity_RecallToDeck`** | `FerocityEffect` 是它 **Start(0)** 那条的 prefab（另一档） |
| `huntMark` | **故意留白（不绑）** | ★ **`HuntMark_DamageEffect`** | 原版 `HuntMark.asset` **确实有一条 FromCode(5)**（`HuntMark Damage Anim`）—— 原来的「宁可留白」是按名字找不到绑定 |
| `shuriken` / `teleport` | 无 | **`ShurikenEffect` / `Teleport Trait Summon`** | 新增 |

⚠️ **`stomp` / `ferocity` 换成不同 prefab 是可见的画面变化**（`BattleDriver.PlayTraitParticles` 的 `EvtKind.Attack` 名单里有 `stomp`、`EvtKind.Ability` 查 `ferocity`）—— 见 §七·1。

---

## 五、`VfxMap.cs`：消费端上闸（第二道）

| 位置 | 改动 |
|---|---|
| `EnsureLoaded` 的逐行循环（`:828-844`） | **只收 `c == authoritative`**；其余 `CardTableNonAuthoritativeRows++` + 每次会话每条 (卡,事件) 一条 `LogWarning` ⇒ **正常恒 0**（数据侧已清空，这是防回退的闸） |
| `EnsureLoaded` 尾段（`:905-`） | 新增 `notPlayed[]` 处理：`CardTableNotPlayed++` + **逐条 `LogWarning`**（上限 24 条，其余汇总一行）+ 建 `IsNotPlayed` 索引 |
| 新增 API | `CardTableNotPlayed` · `CardTableNonAuthoritativeRows` · `NotPlayedCardIds()` · `IsNotPlayed(idOrName, evt)` |
| 新增形状 | `RtNotPlayed { id, nm, fa, ev, n, a, outcome, why }`；`RtFile.notPlayed / notPlayedCount` |
| 文档订正（铁律 5） | 「`attack_melee` 里另外 2 条 inferred-name 留在旧桶」→ 那两条**已不在表里**；`UnitDamaged` 4 → **1**；`ByCard`/`EnsureLoaded` 的「推断档平票只留第一条」→ **非权威档一律不进表**；`TryResolveTriggerPicks` 那段「我们的 `inferred-*` 档」→ 那几条已升 `authoritative`、**结论不变**（不在 `AnimTimes` 里 ⇒ 仍被丢掉） |

未改任何公开方法签名（`Resolve` / `ResolveAll` / `ResolveAllTimed` / `ResolvePerCard` / `EnsureLoaded` / `ConfAuthoritative` 全部原样）。

---

## 六、断言清单（`AnimFXCheck.CheckTraitAuthority`，`:720`；共 26 条 Check）

跑法（**主对话 / 唯一 Unity 实例**）：`-executeMethod AnimFXCheck.Run`。

| # | 断言 | 怎么让它真红（灭自证） |
|---|---|---|
| 前提 | 逐卡表载进来了（`CardTableCards > 1000`） | 删/改名 `Resources/card_vfx_by_card.json` |
| ① | 读得出 >2000 行 | —— 防「读成 0 行自己绿」 |
| ① | **非 `authoritative` 行 = 0**（附 conf 分布） | 删掉生成器里 `strip_unbacked_inference(...)` 调用、或删 `EnsureLoaded` 的第二道闸 ⇒ 22 条回来 |
| ② | `CardTableNonAuthoritativeRows == 0` | 同上 |
| ② | `notPlayed[]` ≥5 条且**每条都带 `why`** | 不写 `why` / 不记账 ⇒ 红（「不许静默」的落地） |
| ② | 5 张 penitence 的 `trigger` 档都在 `notPlayed` 里（5/5） | 塞回事件桶 ⇒ 红 |
| ② | 这 5 张**没有任何** `PenitenceEffect` 从 `trigger` 播出去（0 条） | 去掉「删重复」那一步 ⇒ 5 条回来 |
| ② | **灭自证**：同一件 `PenitenceEffect` 在 `ability` 档仍拿得到（5/5） | 「一怒之下把整张卡的动画删了」⇒ 上一条绿、**这条红** |
| ③ | 绑定表载进来了 | 删 `Resources/trait_effect_bindings.json` |
| ③ | 数据规模：75 行 / OnPlay 16 / 触发 25 / FromCode 10（阈 60/10/20/5） | 生成器读错源 ⇒ 规模掉 |
| ③ | 空引用的行**一条都没进两本字典**（数据里 5 条这种行、混进字典 0 条） | 删 `EnsureBindings` 里的 `if (Prefab.Length == 0) continue;` |
| ③ | **逐行**：数据里每条 (trait·TT·GUID·DefinedTrait·prefab) 都能在两本字典里原样查到（0 条对不上） | 把 `EnsureBindings` 换回任何手写表 |
| ③ | ★ `stealth` OnPlay = `StealthEffect` | 换绑 ⇒ 红 |
| ③ | ★ `vanguard` OnPlay = `VanguardIdleEffect` | 换成 `Vanguard Frame Animated VAT` ⇒ 红 |
| ③ | ★ **`swarm` OnPlay = null** | 把手写表那行 `{Swarm, "Swarm_Trigger_OnTarget"}` 抄回来 ⇒ 立刻红 |
| ③ | ★ **`slay` OnPlay = null · 触发 = `SlayEffect`** | 把「附魔加身演一次」那条抄回 OnPlay 那本 ⇒ 红 |
| ③ | `oath`：1 条 Trigger(3) · 1275 · `playIfWarlord=false` · `Oath_Effect` | 任一项不符 ⇒ 红 |
| ③ | `bloodthirst` 两行（3→`_Gain` · 6→`_Idle`，按资产顺序） | 只留一行 / 换成 `BloodThirstEffect` |
| ③ | 绑定表每件 prefab 都在效果库里（0 件缺） | 改名 / 重导 prefab 不重建效果库 |
| ④ | FromCode 表条数 == 数据里 tt==5 的条数（10） | 表被写死成旧 7 行 ⇒ 红 |
| ④ | 数据里每条 FromCode 行的 prefab 与 `PrefabFor` 逐条相等（0 条不等） | 抄回手写表 ⇒ 立刻对不上 |
| ④ | ★ `stomp` FromCode = `Attack_Stomp` | 抄回 `StompEffect` ⇒ 红 |
| ④ | ★ `huntMark` FromCode = `HuntMark_DamageEffect`（改前是 null） | 改回留白 ⇒ 红 |
| ④ | `ferocity` FromCode = `Ferocity_RecallToDeck` | 抄回 `FerocityEffect` ⇒ 红 |

### 6·1 这些断言的期望值已**离线核过一遍**（静态模拟，不是实跑）

用 python 按 C# 载入语义重放：`onplay 16 / trigger 25 / fromcode 10 / mismatch 0 / emptyOnBook 5 / leak 0`；字面量全部相符（`stealth→StealthEffect` · `vanguard→VanguardIdleEffect` · `swarm→None` · `slay→None/SlayEffect` · `oath→Oath_Effect` · `bloodthirst→[(3,_Gain),(6,_Idle)]` · `stomp→Attack_Stomp` · `huntmark→HuntMark_DamageEffect` · `ferocity→Ferocity_RecallToDeck`）。**但真绿要等 Unity 跑**（红线：本次不许跑）。

---

## 七、⚠️ 运行时后果 / 要主对话拍板或派人验收的

1. 🔴 **画面会变的四处（都要真机看）**：
   a. **新增 OnPlay 状态框**：`flying` 77 张 / `camouflage` 21 张 / `shield` 13 张，在 `TraitFrames.SyncKeywords` **首轮同步**时会各挂一张框（原版是「trait 加身那一刻」才演；我们的 `SyncKeywords` 是「集合对差」，首轮 `_prev` 为空 ⇒ **开局就把所有关键词当新加的**）。
      ⚠️ 这是**本次唯一有视觉风险**的一处：`FlyingEffect` / `CamouflageEffect` / `ShieldTraitEffect_Idle` 三件在开局同时挂到 111 张卡上，可能与该处的既有表现重复（`BattleDriver` 自带 flying 逻辑）。**要么真机确认 `DisplayStatusAnim` 的开局时刻本来就该演，要么把首轮同步单独 gate 掉**（那是 `BattleDriver`/`TraitFrames` 的时序问题，不在本次「去兜底」的口径内 —— 我**没有**擅自加 gate）。
   b. **`slay` 的框从「附魔加身」挪到「真的击杀触发」**（15 张卡带 slay；引擎 `RuleCore.cs:5484 FireTriggerAt(…, KeywordTable.Slay, …)` 会发）。
   c. **`swarm` 不再有 OnPlay 框**（10 张卡）；它的 FromCode 粒子照旧在 Trigger 事件上播。
   d. **`stomp` → `Attack_Stomp`、`ferocity` → `Ferocity_RecallToDeck`**（都是权威件，但换了 prefab）。
2. 🔴 **`hit` 桶 4→1、`deploy` 桶 5→1**：`UnitDamaged` 技能动画只剩 `BL20`；`deploy` 只剩 `SOR33`。那 4 张卡的「召唤动画」被判定属于 **`play`（ThisCardPlayed）**，改成在出牌那一拍播（权威），不是消失。
3. ⚠️ **`overrideByArmies` 仍未消费**（10 条：`shield` 4 · `invulnerable` 6）。`TraitFrames.PrefabFor` 没有阵营参数，加参数要动 `CardView`/`BattleDriver`（不在白名单）⇒ 真值已完整留在 `TraitEffectBinding.Overrides`，缺口照旧（与 `633条inferred升权威_执行报告.md` §七·3 是同一个缺口）。

---

## 八、⛔ 必须由别人改的（`BattleScene.cs` 不在我的白名单，我一条都没动）

我这次让 **3 条**现成断言转红（**都是因为它们的期望值被数据推翻，不是实现错**）。逐条给替换文本（行号 = 我改完 `VfxMap`/`TraitFrames` 时的现读，可能已被并行代理挪动 —— 用文字锚点找）：

| 位置（锚点） | 现状 | 应改成 |
|---|---|---|
| `BattleScene.cs:8447` `Check(TraitParticles.PrefabFor("huntmark") == null, "★ huntMark 故意留白…")` | 断「留白」 | `Check(TraitParticles.PrefabFor("huntmark") == "HuntMark_DamageEffect", …)`；理由：原版 `HuntMark.asset` **有** FromCode(5) `HuntMark Damage Anim`（判据 → 本报告 §4·3 / `trait_effect_bindings.json`）。**或者直接删掉这条**（`AnimFXCheck` 已用反向断言覆盖） |
| `BattleScene.cs:10338` `Check(CardTableUnitAttackEntries == 12 && CardTableUnitDamagedEntries == 4 && CardTableUnitAnimEntries == 16, …)` | 12 / 4 / 16 | **12 / 1 / 13** |
| `BattleScene.cs:10356` `Check(udCards.Length == 4 && udHit == 4, …)` | 4 / 4 | **1 / 1** |
| `BattleScene.cs:10335-10337` 注释「另外 3 条是按名字对上的（`inferred-name`）」 | 已过期 | 3 条里 2 条判为**重复**、1 条**无依据不播**（见本报告 §二） |

**其余 BattleScene 相关断言我逐条对过、都仍然成立**：`T13` 那组（`MultiEntries==t13AuthMulti`、`MultiEntriesAll==MultiEntries+t13NonAuthMulti`、`dropA/dropB`、`authWt`、`Candidate>Entries`）、状态框那组（`stealth`/`vanguard`/`codex`/`oath`/`bloodthirst` 两行）、`uaCards.Length==11 && uaHit==11`、`ResolveAll(Hit…)` 那条已改成 `perCardOnly`（`oldHitLeft/oldMeleeLeft == 0` ✓）。

---

## 九、改动清单（逐文件）

| 文件 | ±行 | 内容 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Core/TraitFrames.cs` | **+273 / −190** | 手写两本表 → 数据驱动；新增 `BindingRow/BindingDoc/TraitEffectOverride` + `EnsureBindings` + `BindingsFor` + 查询 API；`TraitEffectBinding` 加 `Overrides`；文件头改判（swarm/slay 移除的理由与判据） |
| `MyGame/Assets/CardPresentation/Core/TraitParticles.cs` | **+80 / −20** | 手写 7 行 → 数据驱动 10 行（复用 `TraitFrames.RowsOfTriggerType(5)`）；新增 `BindingCount/Loaded/Keys/ResetForTest`；文件头改判（huntMark/stomp/ferocity 三条） |
| `MyGame/Assets/CardPresentation/Core/VfxMap.cs` | **+165 / −28** | 第二道闸（只收 authoritative）+ `notPlayed` 出声 + 4 个新 API + 文档订正 |
| `MyGame/Assets/WarpforgeArena1/Editor/AnimFXCheck.cs` | **+277 / −0** | 新增 `CheckTraitAuthority()`（26 条断言）+ `Describe()` + Run 里一句调用（文件仍是**纯 CRLF**，986→1004 行，CRLF/LF 计数相等） |
| `工具/gen_card_vfx_by_card.py` | **+202 / −22** | 新增 `cardanim_guid_by_name` + **`strip_unbacked_inference`**（两条出路）+ `stats.inferenceGone` + 运行时 `notPlayed[]` + `_headline`/`_conventions` 里那段「推断档保留」的过期说法订正 |
| `工具/gen_trait_effect_bindings.py` | **新文件（约 290 行）** | 从 `trait_collection.json` + `anim_address_map.json` + `guid_map_bundles.tsv` 生成绑定表（含 GUID↔名字交叉校验、幂等、`--check`） |
| `MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json` **（新）** | 27,665 B / 75 行 | 新数据资产（+ 同名 `.meta`，guid `5b1df4e20362cbb9fefd84873d4015a8`） |
| `MyGame/Assets/CardPresentation/Resources/card_vfx_by_card.json` | 1 行（单行 JSON） | 重新生成（395,186 B · 1098 卡 · 2672 条 · `notPlayedCount 22`） |
| `数据/游戏数据/card_vfx_by_card.json` | **+884 / −568** | 重新生成（3,156,733 B） |

**行尾核对**（`git diff --numstat`，铁律 §二）：四个 `.cs` 的改动行数都远小于文件行数 ⇒ **没有翻 CRLF/LF**；`AnimFXCheck.cs` 二进制复核 `CRLF 1004 / LF 1004`（纯 CRLF）；两份 JSON 均 **0 CRLF**（生成器本来写 LF）。

**幂等**：`gen_card_vfx_by_card.py` 固定 `--generated-at` 连跑两遍 ⇒ 两份产物 md5 完全相同、日志逐行相同；`gen_trait_effect_bindings.py` 同。

---

## 十、没查清 / 没做（如实记）

1. **`penitence` 这个关键词从哪来，仍未查清**（沿用普查 §六·4）：73 个 `CardTrait` 里没有它，但 5 张卡的**卡 SO `ability` 桶**确实引用了 `PenitenceTraitTrigger`（`Rage(130)`）。本次只做了「删掉无依据的触发行、保留卡 SO 那一行」，**没有**去追「谁给它加的 `Penitence` 关键词」。
2. **`overrideByArmies` 的 10 条变体**仍然没在运行期按阵营取（见 §七·3）—— 判据齐、缺一次签名扩展。
3. **开局首轮 `SyncKeywords` 该不该演 OnPlay 框**：没有原版实况证据（`DisplayStatusAnim` 的调用点 `CardScript.AddEffect` 只在「加 trait」时调，而我们是集合对差）。**需要真机/原版实况**，本次没查到判据 ⇒ **没有动**（见 §七·1a）。
4. **没有跑任何 Unity 自检**（红线）。断言是否真绿、会不会有别的宿主红，**以主对话实跑为准**；本报告 §8 是**静态推的**必然红项。
5. `工具/gen_ability_anim_durations.py` 的已知噪声（`633条inferred升权威_执行报告.md` §七·5：它会捞进关键词天赋行、记进 `unresolved`）**本次没动** —— 不在白名单，且与本件不冲突（详见 §十一·3）。

---

## 十一、顺手发现（⛔ 没改）

1. **`TraitParticles` 的注释里那条「`SniperTraitFromCode` 等四件是名字逐字对上」的表格与实际 `prefab` 列不一致**（表里写的是 prefab 名、注释写的是 CardAnim 名）。本次改数据驱动后这段已重写，但**同族写法**（注释举 CardAnim 名、代码给 prefab 名）在别的文件里可能还有 —— 建议下一个人顺手统一口径。
2. **`BattleDriver.cs:18956` 的注释**仍写「`huntMark` … prefab 判据不足 ⇒ 整条没接（宁可留白，不猜）」—— 这条**已被数据推翻**（`HuntMark_DamageEffect` 有据）。它在白名单外，我没改；建议改成「已按 `CardTrait` SO 绑上，触发时机仍是 `…InTarget` 那条链（未接）」。
3. **`工具/gen_ability_anim_durations.py:137`** 只按 `conf == "authoritative"` 过滤 `byCard[*].ability[]` ⇒ 会把关键词天赋行（有 `a`、没有 `abilitySlot`/`assetBundle`）捞进 `unresolved`。本件之后这类行**没变多**，但那条过滤建议加 `src != "cardtrait-so"`。
4. **`_unclassified` 桶现在只剩 `_assetRefs` 之外的空桶**（25 条无依据行被移走后，43 张卡的 `_unclassified` 变空）——生成器仍在写这个键（空数组），全量件体积上无害；**没有**清理，因为下一个人可能要拿它当「按名字对不上的名单」。
5. **`VfxMap` 的 `hit` 桶现在只有 1 条** —— 那条 `movedToUnitDamaged` 的分支仍然对 `Hit` **无条件**生效（不看 `authoritative`），现在恒等于「全部」；语义正确，但**注释里**「`hit` 那 4 条同理」已改，别再按 4 条理解。
