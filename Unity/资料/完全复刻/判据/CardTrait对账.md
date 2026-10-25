# CardTrait 对账（原版 74 条词条 SO ↔ 我们的实现）

> **判据链**：原版 `sharedassets0.assets` 的 `CardTrait` SO（R1 用 IL2CPP 生成树解出）＋ 枚举语义取 `d:/2/tools/il2cpp_out/dump.cs` ＋ 我们侧的 `数据/游戏数据/trait_collection.json`、`Resources/trait_effect_bindings.json`、`Core/TraitFrames.cs`、`Core/TraitParticles.cs`。
> ⛔ 判定里**没有一个格子拿我们的常量当判据**；「我们实现了没有」一律指到 `文件:行` 或数据文件。

## §0 先厘清：`CardTrait` SO 到底定义了什么（不然会对错东西）

`dump.cs` 里 `CardTrait : ScriptableObject` 只有 8 个字段：`_traitIcon / _hasInPlayAnim / _hasIcon / `_isInPlayIcon / _stackable / _aIScore / customObject / _traitEffects`。
**它是「表现元数据」，不是规则定义** —— 规则在代码里（`DefinedTrait` + `CardScript`/`BattleManager`）。
所以本表对的是三件事：**① 图标与显示开关 ② 每一条 `TraitEffect` 落在哪个触发档、放哪个 CardAnim ③ 有没有阵营覆盖 / customObject**。

| 枚举 | 值 | 出处 |
|---|---|---|
| `EffectTriggerType` | `Start=0 · End=1 · OnPlay=2 · Trigger=3 · OnHand=4 · FromCode=5 · TriggerOnPlay=6` | `dump.cs` TypeDefIndex 405 |
| `TraitStackType` | `replace=0 · add=10 · count=20` | `dump.cs` TypeDefIndex 404 |

🔴 **`_stackable` 不是层数**：72 条 = 10（`add`）、1 条 = 20（`count`，`MarkOfChaos`）。⛔ 别把 10 读成「10 层」。

## §1 触发档 → 我们的实现（一次说清，逐条表里只引用）

| 档 | 原版语义 | 我们的落点 |
|---|---|---|
| Start(0) | 卡进场/状态刷新时**直接演**（不过 `playIfWarlord` 门） | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） |
| End(1) | （枚举有，但**没有任何调用点**） | —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） |
| OnPlay(2) | 状态框「加身」那本字典（播完写表、不重播） | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） |
| Trigger(3) | 状态框「触发」那本字典（**每次都演**、不写表） | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） |
| OnHand(4) | （枚举有，本 build 数据里 0 条） | —（**原版数据里没有 OnHand(4)**） |
| FromCode(5) | 从代码播的粒子（`ActivateTraitParticlesFromCode`，硬编码找 5） | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） |
| TriggerOnPlay(6) | 状态框「触发」那本字典 + **写去重表**（一辈子只演第一次） | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） |

原版数据里 6 档实际出现次数（`trait_collection.json` 现数）：0 13 · 1 6 · 2 18 · 3 21 · 5 10 · 6 7。
**`OnHand(4)` 一条都没有** —— 原版声明了枚举但本 build 不用。

## §2 逐条对账（74 条）

| # | 英文键 | 中文 | definedTrait | SO 触发档（→ 动画） | 图标 | 我们的数据 | 我们的代码落点 | 判定 | 台账档 | 差在哪 / 证据 |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `Agenda` | 议程 | 1235 | TriggerOnPlay(6)→DutyAnim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | — | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 2 | `Ambush` | 伏击 | 1200 | End(1)→Ambush Effect | 有 | `trait_collection.json` ✓ | —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） | ① 一致 | A×1 B×2 | 只声明 `End(1)`；原版三处调用点都**不处理 End(1)** ⇒ 双方都不播（我们留空 + 出声） |
| 3 | `Armour` | 盔甲 | 1020 | Start(0)→ArmourTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | B×1 | 触发槽全部落在泛型分派里（Start） |
| 4 | `Artifice` | 工匠 | 1040 | Trigger(3)→ArtificeTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 5 | `Backlash` | 反击 | 120 | Trigger(3)→BacklashTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | D×1 | 触发槽全部落在泛型分派里（Trigger） |
| 6 | `Blast` | 爆炸 | 350 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | — | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 7 | `Blind` | 失明的 | 975 | OnPlay(2)→BlindTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 8 | `BloodThirst` | 嗜血 | 220 | Trigger(3)→BloodThirstGainAnim · TriggerOnPlay(6)→BloodThirstIdleAnim · End(1)→BloodThirstLoseAnim | 有 | `trait_collection.json` ✓ | —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） ; `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） ; `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | — | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 9 | `Camouflage` | 伪装 | 985 | OnPlay(2)→CamouflageTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 10 | `CantAttack` | 不能攻击 | 150 | Start(0)→CantAttackAnim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 11 | `Codex` | 圣典 | 1000 | Trigger(3)→CodexTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | C×3 | 触发槽全部落在泛型分派里（Trigger） |
| 12 | `Combo` | 组合 | 1130 | OnPlay(2)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | B×1 | 原版该槽 `effectAnim` 是**空引用**（OnPlay）⇒ 原版也不播，不是缺口 |
| 13 | `Companion` | 伙伴 | 1180 | Start(0)→CompanionTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 14 | `Concussive` | 震荡的 | 930 | Trigger(3)→ConcussiveTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 15 | `Cruelty` | 残忍 | 1255 | Trigger(3)→CrueltyTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 16 | `DaemonicAllegiance` | — | 1100 | OnPlay(2)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 原版该槽 `effectAnim` 是**空引用**（OnPlay）⇒ 原版也不播，不是缺口 |
| 17 | `Duty` | 职责 | 1220 | TriggerOnPlay(6)→DutyAnim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | A×1 | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 18 | `Ecstasy` | 狂喜 | 1250 | TriggerOnPlay(6)→EcstasyTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | — | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 19 | `Ephemeral` | 短暂的 | 5 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | A×2 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 20 | `Fast` | 快速 | 40 | Start(0)→FastTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 21 | `Ferocity` | 凶残 | 1265 | Start(0)→FerocityAnim · FromCode(5)→Ferocity Recall to deck | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） ; `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | A×1 | FromCode(5) 已绑：`Ferocity_RecallToDeck`（`trait_effect_bindings.json`） |
| 22 | `FirstStrike` | 先发制人 | 630 | Start(0)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 原版该槽 `effectAnim` 是**空引用**（Start）⇒ 原版也不播，不是缺口 |
| 23 | `Flank` | 侧翼 | 460 | Start(0)→FlankTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 24 | `Flying` | 飞行 | 390 | OnPlay(2)→FlyingTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | D×1 | 触发槽全部落在泛型分派里（OnPlay） |
| 25 | `Frenzied` | 毁灭者 | 680 | Trigger(3)→FrenziedTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 26 | `HuntMark` | 猎杀标记 | 1260 | TriggerOnPlay(6)→HuntMark Start Anim · Start(0)→HuntMark Stack Anim · OnPlay(2)→HuntMark Idle Anim · FromCode(5)→HuntMark Damage Anim · End(1)→HuntMark End Anim | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） ; —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） ; `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） ; `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） ; `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | B×1 | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 27 | `Invulnerable` | 无懈可击 | 310 | OnPlay(2)→InvulnerableTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | A×1 | ~~曾判语义不同~~ → **已改判一致**：`BattleDriver.cs:26529/26548` 已改成用**卡自己的** `CardFaction`（`TraitFrames.cs:103-105` 确认 `ArmyId` 读的就是这一格） |
| 28 | `Large` | 大型 | 940 | Trigger(3)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | C×1 | 原版该槽 `effectAnim` 是**空引用**（Trigger）⇒ 原版也不播，不是缺口 |
| 29 | `LongRange` | 远程 | 1160 | FromCode(5)→LongRangeAnim · End(1)→LongRangeTraitEnd | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） ; `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | A×2 | FromCode(5) 已绑：`LongRange_GainTrait`（`trait_effect_bindings.json`） |
| 30 | `MarkOfChaos` | 黑暗契约 | 330 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | B×2 C×1 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 31 | `MarkOfKhorne` | 血之黑暗契约 | 1060 | OnPlay(2)→MarkOfKhorneTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 32 | `MarkOfNurgle` | 坚韧黑暗契约 | 1070 | OnPlay(2)→MarkOfNurgleTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 33 | `MarkOfSlaanesh` | 过度黑暗契约 | 1080 | OnPlay(2)→MarkOfSlaaneshTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 34 | `MarkOfTzeentch` | 命运黑暗契约 | 1090 | OnPlay(2)→MarkOfTzeenchTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 35 | `Markerlight` | 标记灯 | 1170 | Start(0)→MarkerlightTraitStart · OnPlay(2)→MarkerlightTraitPlay · FromCode(5)→MarkerlightTraitFromCode · End(1)→MarkerlightTraitEnd | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） ; —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） ; `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） ; `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | — | FromCode(5) 已绑：`MarkerlightTriggerDamage`（`trait_effect_bindings.json`） |
| 36 | `Mekaniak` | — | 950 | Trigger(3)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 原版该槽 `effectAnim` 是**空引用**（Trigger）⇒ 原版也不播，不是缺口 |
| 37 | `Mob` | 暴群 | 920 | Trigger(3)→MobTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | B×1 | 触发槽全部落在泛型分派里（Trigger） |
| 38 | `NotDestroyRemnant` | — | 1190 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | B×1 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 39 | `NotFerocity` | — |  | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | — | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 40 | `Oath` | 誓言 | 1275 | Trigger(3)→Oath_Effect | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | C×2 | 触发槽全部落在泛型分派里（Trigger） |
| 41 | `OathAdditionalEffect` | — |  | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | — | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 42 | `Pack` | 群体 | 610 | Start(0)→Pack OnPlay Anim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | A×6 D×2 | 触发槽全部落在泛型分派里（Start） |
| 43 | `Piercing` | — | 900 | Start(0)→∅ | 原版无 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 原版该槽 `effectAnim` 是**空引用**（Start）⇒ 原版也不播，不是缺口 |
| 44 | `PinDown` | 压制 | 970 | OnPlay(2)→PinDownTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | B×1 | 触发槽全部落在泛型分派里（OnPlay） |
| 45 | `Pray` | 祈祷 | 1185 | Trigger(3)→Pray_Start · TriggerOnPlay(6)→Pray_Idle | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） ; `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | A×1 | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 46 | `Rage` | 忏悔 | 280 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | — | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 47 | `Rally` | 集结 | 10 | Start(0)→RallyTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 48 | `Regeneration` | 再生 | 1030 | Trigger(3)→RegenerationTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 49 | `Regiment` | 团 | 1225 | Trigger(3)→RegimentAnim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | B×1 | 触发槽全部落在泛型分派里（Trigger） |
| 50 | `Relentless` | 无情 | 450 | Trigger(3)→RelentlessTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 51 | `Remnant` | 残余 | 1050 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | A×3 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 52 | `Resolution` | 决议 | 840 | Trigger(3)→∅ | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 原版该槽 `effectAnim` 是**空引用**（Trigger）⇒ 原版也不播，不是缺口 |
| 53 | `Sentry` | 哨兵 | 1280 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | — | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 54 | `Shield` | 盾牌 | 80 | OnPlay(2)→ShieldTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | A×1 | ~~曾判语义不同~~ → **已改判一致**：`BattleDriver.cs:26529/26548` 已改成用**卡自己的** `CardFaction`（`TraitFrames.cs:103-105` 确认 `ArmyId` 读的就是这一格） |
| 55 | `Shuriken` | 星镖 | 1120 | FromCode(5)→ShurikenTraitFromCode | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | D×1 | FromCode(5) 已绑：`ShurikenEffect`（`trait_effect_bindings.json`） |
| 56 | `Slay` | 消灭 | 260 | Trigger(3)→Slay_Effect_target | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 57 | `Sniper` | 狙击手 | 990 | FromCode(5)→SniperTraitFromCode | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | A×1 | FromCode(5) 已绑：`SniperPreEffect`（`trait_effect_bindings.json`） |
| 58 | `Stealth` | 隐身 | 70 | OnPlay(2)→StealthTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | A×2 C×1 | 触发槽全部落在泛型分派里（OnPlay） |
| 59 | `Stimulation` | 刺激 | 1245 | Trigger(3)→StimulateTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 60 | `Stomp` | 践踏 | 1240 | FromCode(5)→BluntImpact_Stomp_Forward · Trigger(3)→BluntImpact_Stomp_Down | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） ; `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | — | FromCode(5) 已绑：`Attack_Stomp`（`trait_effect_bindings.json`） |
| 61 | `Strike` | 攻击 | 1110 | Trigger(3)→StrikeTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 62 | `Stun` | 眩晕 | 100 | OnPlay(2)→StunTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | B×1 | 触发槽全部落在泛型分派里（OnPlay） |
| 63 | `Swarm` | 虫群 | 650 | FromCode(5)→SwarmTraitFromCode | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | A×2 | FromCode(5) 已绑：`Swarm_Trigger_OnTarget`（`trait_effect_bindings.json`） |
| 64 | `Synapse` | 突触节点 | 1150 | FromCode(5)→SynapseTraitFromCode | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | — | FromCode(5) 已绑：`SynapseEffect`（`trait_effect_bindings.json`） |
| 65 | `Talent` | 才能 | 845 | Trigger(3)→TalentTraitTrigger | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典，**每次触发都播**、不写去重表） | ① 一致 | — | 触发槽全部落在泛型分派里（Trigger） |
| 66 | `Teleport` | 传送 | 1230 | FromCode(5)→TeleportCardAnim | 有 | `trait_collection.json` ✓ + `trait_effect_bindings.json` ✓ | `Core/TraitParticles.cs` + `Resources/trait_effect_bindings.json`（数据驱动） | ① 一致 | D×1 | FromCode(5) 已绑：`Teleport Trait Summon`（`trait_effect_bindings.json`） |
| 67 | `Tide` | 潮汐 | 910 | Start(0)→TideTraitStart | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:372`（DisplayStatus → Start 支） | ① 一致 | — | 触发槽全部落在泛型分派里（Start） |
| 68 | `Unstable` | 不稳定 | 1010 | End(1)→UnstableTraitEnd | 有 | `trait_collection.json` ✓ | —（**原版无调用点**：`DisplayStatusAnim`/`DisplayTriggerAnim`/`RemoveTrait` 都不处理 End(1)） | ① 一致 | — | 只声明 `End(1)`；原版三处调用点都**不处理 End(1)** ⇒ 双方都不播（我们留空 + 出声） |
| 69 | `Unstoppable` | — | 180 | （无） | 原版无 | `trait_collection.json` ✓ | — | ① 一致 | D×1 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 70 | `Uprising` | 起义 | 1210 | TriggerOnPlay(6)→Uprising Effect | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:860-894`（TriggerFrames 字典 + **只有 6 写去重表**） | ① 一致 | — | ~~曾判语义不同~~ → **已改判一致**：原版 `CreateAnim.c` 的写表是**实例化回调**（`Instantiate` 后紧接着 invoke、`CardAnimController__Initialize` 在其后），与我们同步写等价 |
| 71 | `Vanguard` | 先锋 | 980 | OnPlay(2)→VanguardIdleAnim | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | A×1 C×1 | 触发槽全部落在泛型分派里（OnPlay） |
| 72 | `Vulnerable` | 易受伤害 | 480 | OnPlay(2)→VulnerableTraitPlay | 有 | `trait_collection.json` ✓ | `Core/TraitFrames.cs:394`（PlayFrames 字典，播完写表、不重播） | ① 一致 | — | 触发槽全部落在泛型分派里（OnPlay） |
| 73 | `Waystone` | 灵魂石 | 1140 | （无） | 有 | `trait_collection.json` ✓ | — | ① 一致 | A×1 C×1 | SO 里 `_traitEffects` 为空 ⇒ 原版不播任何词条特效，我们也没有对应槽 |
| 74 | `TraitsCollection` | — |  | （无） | 原版无 | `trait_collection.json` ✓ | — | ④ 特殊（不是词条） | — | `TraitsCollection` 是**集合 SO**（77 条 trait 对），不是一条词条 |

## §3 四档汇总

| 档 | 数量 |
|---|---|
| ① 一致 | 73 |
| ④ 特殊（不是词条） | 1 |

## §4 例外与「取法不同但等价」（逐条带证据）

0. 🔴 **改判记录（2026-10-25，保留痕迹）**：下面两条曾经判「② 语义不同」，**已按一手判据改判「① 一致」** ——
   - **`TriggerOnPlay(6)` 去重表写入时机**（7 条：`Agenda/BloodThirst/Duty/Ecstasy/HuntMark/Pray/Uprising`）：曾判「原版在播完回调里写、我们同步写 ⇒ 被打断时行为不同」。**推翻**：`d:/2/tools/decomp_full/BattleManager__CreateAnim.c` 里 `Instantiate` 之后**紧接着**`(**(code **)(param_8 + 0x18))(...)` 调那个回调，而 `CardAnimController__Initialize` 在它**之后** ⇒ 那是**实例化回调**、不是「播完」回调，与我们同步写**等价**。边界（如实标）：原版可能先等 CardAnim 的 `delay` 才开始「实例化」—— 能核到的 `Pray_Idle` 三个时间字段全是 0，其余本地缺该时间表 ⇒ **未逐条核**。
   - **`overrideByArmies` 的阵营来源**（2 条：`Shield`/`Invulnerable`）：曾判「原版读卡自己的 army、我们读拥有者阵营 ⇒ 跨阵营可能选错件」。**已修**：`BattleDriver.cs:26529-26530` 现读 `u.Card.Faction`（空才回落拥有者）并在 `:26548` 写进 `CardData.faction`；`Core/TraitFrames.cs:103-105` 现读确认 `ArmyId` 取的就是这一格 ⇒ 改判一致。

0b. 🔴 **`NotFerocity` / `OathAdditionalEffect` 两个口径不矛盾**：`r5` 说「我们完全没有」指的是**`.cs` 里连子串都搜不到**；但它们在本表判 **① 一致**，因为 **SO 里 `_traitEffects` 是空的** ⇒ **没有行为可实现**（不是「我们漏了」）。两句话分别成立：**代码没提** + **原版也没行为**。（另：这两条的 `definedTrait` **也是 null** —— 它们不在 134 条 `DefinedTrait` 枚举里，是纯标记用 SO。）

1. **`Ambush` 的 `customObject`** —— 原版取 `CardTrait.customObject`（= `GenestealerCults_special_Ambush`，`Sprite`，在 `genestealercultscardassets` 包里，GUID `2415b100e3152f045abcf4c815c93e91`，已由 `资料/普查产出_1018/cardanim_guid_map/guid_map_bundles.tsv` 解出）；我们走**另一条等价路**：从 `AmbushEffect` prefab 的材质 `Card 3d Dissolve Blend Image Ambush` 读 `_CardImage` （`Core/CardView.cs` 的 `FaceDownArtTexture()`）—— **同一张贴图**。🔴 但 `CardView.cs:7060` 那句「**CardTrait 资产本身本地没有**（全库 grep `_traitIcon`/`customObject` 0 命中）」**是错的**：那 74 个资产就在 `sharedassets0.assets`，当年 grep 不到是因为它们正好是 **R1 里「解不出」的那 905 个**（没有类型树 ⇒ 导出 JSON 时全失败）。⇒ 现在**可以**改走直连（`CardTrait.customObject`），是否改由后续批次定；**行为目前等价**。
2. **`berzerk` / `dodge`** —— `TraitCollection` 里有这一对，但**没有对应的 CardTrait 资产**（`trait_collection.json` 的 `nullTraitConfigPairs`）= **原版本身没有 SO**，不是我们的缺口。
3. **7 条 `effectAnim` 空引用**（`trait_effect_bindings.json` 的 `unresolvedRows`）：原版那条 effect 本来就**没有动画**，我们留空即正确（⛔ 别猜替身）。逐条见绑定表。
4. **`End(1)` 六条**（`ambush / bloodThirst / huntMark / longrange / markerlight / unstable`）：原版三处调用点（`DisplayStatusAnim` / `DisplayTriggerAnim` / `RemoveTrait`）**都不处理 End(1)** ⇒ 双方都不播。
5. **`overrideByArmies`（`Invulnerable` 6 条 / `Shield` 4 条）** —— 我们**已消费**：`Core/TraitFrames.cs:84` + `:156` 那一段（判据 = 原版 `TraitEffect.GetEffect(cardArmy)`）。
6. 🔴 **4 个 `customObject` GUID 在 `trait_collection.json` 里标为「未解」** （`Ambush / BloodThirst / Cruelty / Swarm`）—— **现在都能解**（`guid_map_bundles.tsv` 有完整行）：`GenestealerCults_special_Ambush`(Sprite) · `BloodThirstEffect_Gain`(GameObject) · `CrueltyEffect`(GameObject) · `Swarm Card Merge VFX`(GameObject)。其中 `Swarm Card Merge VFX` **我们的代码已经在用**（`Battle/BattleDriver.cs:23591`）。

## §5 反向：原版有枚举、但没有 SO 的 57 条

`DefinedTrait` 枚举 **134** 条，其中 **77 条有 SO**（73 个资产 + 2 个空配置 + 集合本身），**57 条没有 SO** —— 这些是**原版本身没给表现元数据**的 trait（例如 `sneakAttack` `syphon` `jam` `ward` `poisoned` …）。⛔ 别把这 57 条当成「我们没做」。

完整名单：`sneakAttack`、`syphon`、`jam`、`ward`、`wardWb`、`tainted`、`coward`、`poisoned`、`dropPod`、`unstunnable`、`doubleRage`、`corrupt`、`giantKiller`、`maintenance`、`poisonous`、`activeAbility`、`psychicAbility`、`plasmaAbility`、`passiveAbility`、`whileInPlayAbility`、`survivor`、`survivorSpent`、`transient`、`sacrifice`、`reload`、`notCollectable`、`secret`、`terror`、`trap`、`secretOrder`、`secretIdentity`、`fear`、`meltdown`、`massive`、`resistant`、`psyker`、`reactor`、`precognition`、`psychicPower`、`plasmaPower`、`sentinel`、`spikes`、`horns`、`daemonhost`、`summonRift`、`quest`、`questCompleted`、`requiem`、`landing`、`mutation`、`chaosOnly`、`chooseOne`、`bastion`、`mission`、`nonCombatant`、`support`。

## §6 反向：我们卡表里有、原版 `DefinedTrait` 里没有的

把 `cards_engine.json` 的 `cardTraits` 逐条按 `空格/:/.` 切开取首段，与 134 条 `DefinedTrait` 比：**50 个 token 里 45 个直接命中**，剩 5 个：

| token | 卡数 | 判定 | 证据 |
|---|---|---|---|
| `Long` | 13 | **拆分伪影** | 原版是 `Card_Trait/longrange` = `Long Range`（官方词条） |
| `Blood` | 2 | **拆分伪影** | 原版是 `Card_Trait/bloodThirst` = `Blood Thirst` |
| `Can't` | 1 | **拆分伪影** | 原版是 `DefinedTrait.cantAttack` = `Can't Attack` |
| `Destroyer` | 10 | **需核** | `DefinedTrait` 无此项、`Card_Trait/*` 也无；只在**粉丝规则书**里出现（`Warpforge_Offline_Rulebook_1_5-3_*.md`）⇒ 要么是卡面正文被误抽成 trait，要么是原版有但枚举名不同 |
| `Penitence` | 5 | **需核** | 同上（规则书有、枚举与词条表都没有） |

## §7 台账里已经判过的（⛔ 别重复立项）

把 `资料/完全复刻/账/*.tsv` 的 `状态` 列按 trait 名回查（只读，没改账）：

| 台账档 | trait（括号 = 命中行数） |
|---|---|
| **C 该做没做**（已在账上，别重复立项） | `Codex`(C×3)、`Large`(C×1)、`MarkOfChaos`(B×2 C×1)、`Oath`(C×2)、`Stealth`(A×2 C×1)、`Vanguard`(A×1 C×1)、`Waystone`(A×1 C×1) |
| D 不适用（已给理由） | `Backlash`(D×1)、`Flying`(D×1)、`Pack`(A×6 D×2)、`Shuriken`(D×1)、`Teleport`(D×1)、`Unstoppable`(D×1) |
| B 已实现未核 | `Ambush`(A×1 B×2)、`Armour`(B×1)、`Combo`(B×1)、`HuntMark`(B×1)、`MarkOfChaos`(B×2 C×1)、`Mob`(B×1)、`NotDestroyRemnant`(B×1)、`PinDown`(B×1)、`Regiment`(B×1)、`Stun`(B×1) |
| A 已对齐 | `Ambush`(A×1 B×2)、`Duty`(A×1)、`Ephemeral`(A×2)、`Ferocity`(A×1)、`Invulnerable`(A×1)、`LongRange`(A×2)、`Pack`(A×6 D×2)、`Pray`(A×1)、`Remnant`(A×3)、`Shield`(A×1)、`Sniper`(A×1)、`Stealth`(A×2 C×1)、`Swarm`(A×2)、`Vanguard`(A×1 C×1)、`Waystone`(A×1 C×1) |

> ⚠️ 台账的 `状态` 是**方法级**的（一行 = 一个原版方法），不是「trait 级」—— 一个 trait 命中 A 不代表它的每一条语义都对齐，只代表**提到它的那些方法**里有一条判过 A。本表只用来**避免重复立项**，不能当完成度。

⇒ **第三档「我们完全没有」= 0**（74 条原版词条我们都有数据落点）；**第五档「我们有但原版没有」= 2 个 token 待核**（`Destroyer` / `Penitence`，都是**卡表侧的字符串**，不是实现）。

