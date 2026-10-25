# CardTrait 词条权威表（原版 ScriptableObject 逐条）

> **判据来源**：原版 `sharedassets0.assets` 里的 `CardTrait` ScriptableObject（69 个）＋ `CardTraitOath/Duty/Ferocity/Collection` 变体；这些对象正是 R1 里「解不出」的 905 个之一，本轮用 IL2CPP 生成类型树解出（`工具/ttread.py`，逐对象 byte_size 校验通过）。
> **中文名**：`Data/本地化` 里的官方词条 `Card_Trait/<英文小写>`（`官方本地化_en+zh-CN.json`）。⛔ 表里没有一个格子来自我们自己的常量。
> **我们工程里的实现**：`"<英文名>"` 字面量在 `MyGame/Assets/**/*.cs` 里出现的文件（只列文件名，不判断语义是否一致 —— 那是下一轮逐条对的事）。

共 **74** 条：CardTrait 69 · CardTraitDuty 1 · CardTraitFerocity 1 · CardTraitOath 2 · CardTraitCollection 1

## 一、总表

| # | 英文键 | 中文名 | 类 | 图标 sprite | stackable | aIScore | 有图标 | 在场图标 | 在场动画 | 效果链 | 带此词条的卡 | 我们工程里的字面量出现处 |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `Agenda` | 议程 | CardTrait | `Atlas_trait_icon_agenda` | 10 | 0.0 | 1 | 1 | 1 | TriggerOnPlay | 13 | AttackSelector.cs, BattleDriver.cs, ArmyIcons.cs, Badges.cs |
| 2 | `Ambush` | 伏击 | CardTrait | `Atlas_trait_icon_ambush` | 10 | 0.0 | 0 | 0 | 0 | End | 12 | BattleDriver.cs, CardAnimClips.cs, ReplayStore.cs, Badges.cs |
| 3 | `Armour` | 盔甲 | CardTrait | `Atlas_trait_icon_armour` | 10 | 0.0 | 1 | 0 | 0 | Start | 81 | BattleDriver.cs, CardDisplayWindow.cs, Badges.cs, CardIcons.cs |
| 4 | `Artifice` | 工匠 | CardTrait | `Atlas_trait_icon_artifice` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 19 | Badges.cs, CardText.cs, Loc.cs, VfxMap.cs |
| 5 | `Backlash` | 反击 | CardTrait | `Atlas_trait_icon_backlash` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 6 | BattleDriver.cs, ReplayStore.cs, Badges.cs, CardText.cs |
| 6 | `Blast` | 爆炸 | CardTrait | `Atlas_trait_icon_blast` | 10 | 0.0 | 1 | 1 | 0 | （无） | 41 | BattleDriver.cs, BattleSfx.cs, Badges.cs, CardText.cs |
| 7 | `Blind` | 失明的 | CardTrait | `Atlas_trait_icon_blind` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | BattleDriver.cs, Badges.cs, CardIcons.cs, CardText.cs |
| 8 | `BloodThirst` | 嗜血 | CardTrait | `Atlas_trait_icon_bloodThirst` | 10 | 0.0 | 1 | 1 | 0 | Trigger → TriggerOnPlay → End | 0 | BattleDriver.cs, Badges.cs, CardIcons.cs, Loc.cs |
| 9 | `Camouflage` | 伪装 | CardTrait | `Atlas_trait_icon_camouflage` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 21 | Badges.cs, Loc.cs, TraitFrames.cs, VfxMap.cs |
| 10 | `CantAttack` | 不能攻击 | CardTrait | `Atlas_trait_icon_cantAttack` | 10 | 0.0 | 1 | 1 | 1 | Start | 0 | BattleDriver.cs, Badges.cs, CardIcons.cs, Loc.cs |
| 11 | `Codex` | 圣典 | CardTrait | `Atlas_trait_icon_codex` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 31 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 12 | `Combo` | 组合 | CardTrait |  | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | BattleDriver.cs, Badges.cs, TraitFrames.cs, Aura.cs |
| 13 | `Companion` | 伙伴 | CardTrait | `Atlas_trait_icon_companion` | 10 | 0.0 | 1 | 0 | 0 | Start | 8 | Badges.cs, CardText.cs, Loc.cs, RelatedCards.cs |
| 14 | `Concussive` | 震荡的 | CardTrait | `Atlas_trait_icon_concussive` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 6 | Badges.cs, CardIcons.cs, CardText.cs, Tooltip.cs |
| 15 | `Cruelty` | 残忍 | CardTrait | `Atlas_trait_icon_Cruelty` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 9 | Badges.cs, Loc.cs, VfxMap.cs, BattleVfxAssertE3.cs |
| 16 | `DaemonicAllegiance` | — | CardTrait |  | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | Badges.cs, TraitFrames.cs, AnimFXCheck.cs |
| 17 | `Duty` | 职责 | CardTraitDuty | `Atlas_trait_icon_duty` | 10 | 0.0 | 1 | 1 | 1 | TriggerOnPlay | 23 | AttackSelector.cs, BattleDriver.cs, ArmyIcons.cs, Badges.cs |
| 18 | `Ecstasy` | 狂喜 | CardTrait | `Atlas_trait_icon_Ecstasy` | 10 | 0.0 | 1 | 1 | 0 | TriggerOnPlay | 10 | Badges.cs, CardText.cs, Loc.cs, VfxMap.cs |
| 19 | `Ephemeral` | 短暂的 | CardTrait | `Atlas_trait_icon_ephemeral` | 10 | 0.0 | 1 | 0 | 0 | （无） | 104 | BattleDriver.cs, Badges.cs, CardArt.cs, CardHighlight.cs |
| 20 | `Fast` | 快速 | CardTrait | `Atlas_trait_icon_fast` | 10 | 0.0 | 1 | 0 | 1 | Start | 0 | BattleDriver.cs, Badges.cs, CardFeel.cs, CardText.cs |
| 21 | `Ferocity` | 凶残 | CardTraitFerocity | `Atlas_trait_icon_ferocity` | 10 | 0.0 | 1 | 1 | 1 | Start → FromCode | 9 | AttackSelector.cs, BattleDriver.cs, ArmyIcons.cs, Badges.cs |
| 22 | `FirstStrike` | 先发制人 | CardTrait |  | 10 | 0.0 | 1 | 1 | 0 | Start | 0 | Badges.cs, TraitFrames.cs, AnimFXCheck.cs |
| 23 | `Flank` | 侧翼 | CardTrait | `Atlas_trait_icon_flank` | 10 | 0.0 | 1 | 0 | 0 | Start | 42 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 24 | `Flying` | 飞行 | CardTrait | `Atlas_trait_icon_flying` | 10 | 0.0 | 0 | 0 | 1 | OnPlay | 77 | BattleDriver.cs, Card3DAnimationController.cs, FlyingCardController.cs, Badges.cs |
| 25 | `Frenzied` | 毁灭者 | CardTrait | `Atlas_trait_icon_frenzied` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 0 | Badges.cs, CardIcons.cs, CardText.cs, Tooltip.cs |
| 26 | `HuntMark` | 猎杀标记 | CardTrait | `Atlas_trait_icon_huntMark` | 10 | 0.0 | 0 | 0 | 1 | TriggerOnPlay → Start → OnPlay → FromCode → End | 0 | BattleDriver.cs, Badges.cs, CardView.cs, Loc.cs |
| 27 | `Invulnerable` | 无懈可击 | CardTrait | `Atlas_trait_icon_invulnerable` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 28 | `Large` | 大型 | CardTrait |  | 10 | 0.0 | 1 | 1 | 0 | Trigger | 0 | BattleDriver.cs, Label.cs, Badges.cs, CardView.cs |
| 29 | `LongRange` | 远程 | CardTrait | `Atlas_trait_icon_longrange` | 10 | 0.0 | 1 | 1 | 0 | FromCode → End | 0 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 30 | `MarkOfChaos` | 黑暗契约 | CardTrait | `Atlas_trait_icon_markOfChaos` | 20 | 0.0 | 1 | 1 | 0 | （无） | 0 | Badges.cs, CardIcons.cs, CardText.cs, CardView.cs |
| 31 | `MarkOfKhorne` | 血之黑暗契约 | CardTrait | `Atlas_trait_icon_markOfKhorne` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | Badges.cs, CardIcons.cs, IconSetup.cs, CreatePool.cs |
| 32 | `MarkOfNurgle` | 坚韧黑暗契约 | CardTrait | `Atlas_trait_icon_markOfNurgle` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | Badges.cs, CardIcons.cs, IconSetup.cs, CreatePool.cs |
| 33 | `MarkOfSlaanesh` | 过度黑暗契约 | CardTrait | `Atlas_trait_icon_markOfSlaanesh` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | Badges.cs, CardIcons.cs, CardText.cs, Tooltip.cs |
| 34 | `MarkOfTzeentch` | 命运黑暗契约 | CardTrait | `Atlas_trait_icon_markOfTzeentch` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | Badges.cs, CardIcons.cs, VfxMap.cs, IconSetup.cs |
| 35 | `Markerlight` | 标记灯 | CardTrait | `Atlas_trait_icon_markerlight` | 10 | 0.0 | 0 | 0 | 1 | Start → OnPlay → FromCode → End | 0 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 36 | `Mekaniak` | — | CardTrait |  | 10 | 0.0 | 1 | 1 | 0 | Trigger | 0 | BattleDriver.cs, Badges.cs, TraitFrames.cs, CardDef.cs |
| 37 | `Mob` | 暴群 | CardTrait | `Atlas_trait_icon_mob` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 16 | BattleDriver.cs, CombatCameraZoom.cs, TouchInputManager.cs, Badges.cs |
| 38 | `NotDestroyRemnant` | — | CardTrait | `Atlas_trait_icon_flying` | 10 | 0.0 | 0 | 0 | 1 | （无） | 0 | Badges.cs, RuleCore.cs, RuleEngineTest.cs |
| 39 | `NotFerocity` | — | CardTrait | `Atlas_trait_icon_ferocity` | 10 | 0.0 | 0 | 0 | 1 | （无） | 0 | **（无）** |
| 40 | `Oath` | 誓言 | CardTraitOath | `Atlas_trait_icon_oath` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 24 | AttackSelector.cs, BattleDriver.cs, AbilityAnimDurations.cs, ArmyIcons.cs |
| 41 | `OathAdditionalEffect` | — | CardTraitOath | `Atlas_trait_icon_oath` | 10 | 0.0 | 0 | 0 | 0 | （无） | 0 | **（无）** |
| 42 | `Pack` | 群体 | CardTrait | `Atlas_trait_icon_pack` | 10 | 0.0 | 1 | 0 | 0 | Start | 10 | AnimFXController.cs, BattleCameraSreenSize.cs, BattleDriver.cs, BattleEventSystem.cs |
| 43 | `Piercing` | — | CardTrait |  | 10 | 0.0 | 1 | 1 | 0 | Start | 0 | Badges.cs, TraitFrames.cs, AnimFXCheck.cs |
| 44 | `PinDown` | 压制 | CardTrait | `Atlas_trait_icon_pindown` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | AttackSelector.cs, BattleDriver.cs, Badges.cs, Loc.cs |
| 45 | `Pray` | 祈祷 | CardTrait | `Atlas_trait_icon_pray` | 10 | 0.0 | 1 | 1 | 1 | Trigger → TriggerOnPlay | 23 | AttackSelector.cs, BattleDriver.cs, ArmyIcons.cs, Badges.cs |
| 46 | `Rage` | 忏悔 | CardTrait | `Atlas_trait_icon_rage` | 10 | 0.0 | 1 | 1 | 0 | （无） | 0 | BattleDriver.cs, BattleLogPanel.cs, Badges.cs, CardIcons.cs |
| 47 | `Rally` | 集结 | CardTrait | `Atlas_trait_icon_rally` | 10 | 0.0 | 1 | 0 | 0 | Start | 105 | BattleDriver.cs, Badges.cs, CardIcons.cs, CardText.cs |
| 48 | `Regeneration` | 再生 | CardTrait | `Atlas_trait_icon_regeneration` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 9 | Badges.cs, CardText.cs, Loc.cs, VfxMap.cs |
| 49 | `Regiment` | 团 | CardTrait | `Atlas_trait_icon_regiment` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 13 | Badges.cs, CardText.cs, Loc.cs, VfxMap.cs |
| 50 | `Relentless` | 无情 | CardTrait | `Atlas_trait_icon_relentless` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 0 | Badges.cs, BattleScene.cs, IconSetup.cs, FTUESelectDeckWindow.cs |
| 51 | `Remnant` | 残余 | CardTrait | `Atlas_trait_icon_remnant` | 10 | 0.0 | 1 | 1 | 1 | （无） | 27 | AnimFXController.cs, BattleDriver.cs, RemnantSfx.cs, WarpforgeVatDriver.cs |
| 52 | `Resolution` | 决议 | CardTrait | `Atlas_trait_icon_resolution` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 0 | ArenaRuntimeLoader.cs, BattleCameraSreenSize.cs, BattleDriver.cs, ChatPopupPanel.cs |
| 53 | `Sentry` | 哨兵 | CardTrait | `Atlas_trait_icon_sentry` | 10 | 0.0 | 1 | 1 | 1 | （无） | 3 | BattleDriver.cs, ChatPopupPanel.cs, ErrorMessageBanner.cs, Badges.cs |
| 54 | `Shield` | 盾牌 | CardTrait | `Atlas_trait_icon_shield` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 13 | BattleDriver.cs, Badges.cs, CardbackFace.cs, CardText.cs |
| 55 | `Shuriken` | 星镖 | CardTrait | `Atlas_trait_icon_shuriken` | 10 | 0.0 | 1 | 1 | 0 | FromCode | 11 | Badges.cs, Loc.cs, TraitParticles.cs, BattleScene.cs |
| 56 | `Slay` | 消灭 | CardTrait | `Atlas_trait_icon_slay` | 10 | 0.0 | 1 | 1 | 0 | Trigger | 16 | BattleDriver.cs, CardAnimOrder.cs, MulliganPanel.cs, AbilityAnimDurations.cs |
| 57 | `Sniper` | 狙击手 | CardTrait | `Atlas_trait_icon_sniper` | 10 | 0.0 | 1 | 1 | 0 | FromCode | 11 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 58 | `Stealth` | 隐身 | CardTrait | `Atlas_trait_icon_stealth` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 31 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 59 | `Stimulation` | 刺激 | CardTrait | `Atlas_trait_icon_stimulation` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 10 | Badges.cs, CardText.cs, Loc.cs, VfxMap.cs |
| 60 | `Stomp` | 践踏 | CardTrait | `Atlas_trait_icon_stomp` | 10 | 0.0 | 1 | 1 | 0 | FromCode → Trigger | 9 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 61 | `Strike` | 攻击 | CardTrait | `Atlas_trait_icon_strike` | 10 | 0.0 | 1 | 1 | 1 | Trigger | 31 | BattleDriver.cs, Badges.cs, CardIcons.cs, CardText.cs |
| 62 | `Stun` | 眩晕 | CardTrait | `Atlas_trait_icon_stun` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | BattleDriver.cs, BattleSfx.cs, ChoosePanel.cs, Badges.cs |
| 63 | `Swarm` | 虫群 | CardTrait | `Atlas_trait_icon_swarm` | 10 | 0.0 | 1 | 1 | 1 | FromCode | 10 | BattleDriver.cs, Badges.cs, CardArt.cs, CardView.cs |
| 64 | `Synapse` | 突触节点 | CardTrait | `Atlas_trait_icon_synapse` | 10 | 0.0 | 1 | 1 | 1 | FromCode | 13 | BattleDriver.cs, Badges.cs, CardText.cs, Loc.cs |
| 65 | `Talent` | 才能 | CardTrait | `Atlas_trait_icon_talent` | 10 | 0.0 | 1 | 0 | 0 | Trigger | 96 | BattleDriver.cs, Badges.cs, CardText.cs, CardView.cs |
| 66 | `Teleport` | 传送 | CardTrait | `Atlas_trait_icon_teleport` | 10 | 0.0 | 1 | 0 | 0 | FromCode | 7 | BattleDriver.cs, Badges.cs, CardFeel.cs, CardHighlight.cs |
| 67 | `Tide` | 潮汐 | CardTrait | `Atlas_trait_icon_tide` | 10 | 0.0 | 1 | 0 | 0 | Start | 23 | AttackSelector.cs, BattleDriver.cs, Badges.cs, CardArt.cs |
| 68 | `Unstable` | 不稳定 | CardTrait | `Atlas_trait_icon_unstable` | 10 | 0.0 | 1 | 1 | 0 | End | 18 | BattleDriver.cs, ReplayStore.cs, Badges.cs, Loc.cs |
| 69 | `Unstoppable` | — | CardTrait |  | 10 | 0.0 | 1 | 1 | 1 | （无） | 1 | Badges.cs, CardDef.cs, ConditionEval.cs, RuleCore.cs |
| 70 | `Uprising` | 起义 | CardTrait | `Atlas_trait_icon_uprising` | 10 | 0.0 | 1 | 0 | 0 | TriggerOnPlay | 9 | MultiCardDisplay.cs, Badges.cs, Loc.cs, VfxMap.cs |
| 71 | `Vanguard` | 先锋 | CardTrait | `Atlas_trait_icon_vanguard` | 10 | 0.0 | 1 | 0 | 1 | OnPlay | 41 | BattleDriver.cs, CardAnimClips.cs, CardDisplayWindow.cs, FlyingCardController.cs |
| 72 | `Vulnerable` | 易受伤害 | CardTrait | `Atlas_trait_icon_vulnerable` | 10 | 0.0 | 1 | 1 | 1 | OnPlay | 0 | BattleDriver.cs, Badges.cs, CardText.cs, EventTiming.cs |
| 73 | `Waystone` | 灵魂石 | CardTrait | `Atlas_trait_icon_waystone` | 10 | 0.0 | 1 | 1 | 1 | （无） | 25 | AnimFXController.cs, BattleDriver.cs, RemnantSfx.cs, Badges.cs |
| 74 | `TraitsCollection` | — | CardTraitCollection |  | None | None | None | None | None | （无） | 0 | TraitFrames.cs, TraitParticles.cs, VfxMap.cs |

## 二、效果链逐条（`_traitEffects` 原文）

### `Agenda` — 议程

- **TriggerOnPlay**：effectAnim=AssetRef(a8c3f891…)；TriggerType=6；playIfWarlord=1

### `Ambush` — 伏击

- **End**：effectAnim=AssetRef(4ae02eb9…)；TriggerType=1；playIfWarlord=1
- customObject：`2415b100e3152f045abcf4c815c93e91`

### `Armour` — 盔甲

- **Start**：effectAnim=AssetRef(a4088dc3…)

### `Artifice` — 工匠

- **Trigger**：effectAnim=AssetRef(13a7b7da…)；TriggerType=3；playIfWarlord=1

### `Backlash` — 反击

- **Trigger**：effectAnim=AssetRef(42d0bc7d…)；TriggerType=3；playIfWarlord=1

### `Blast` — 爆炸

- （`_traitEffects` 为空）

### `Blind` — 失明的

- **OnPlay**：effectAnim=AssetRef(ddaaf29c…)；TriggerType=2；playIfWarlord=1

### `BloodThirst` — 嗜血

- **Trigger**：effectAnim=AssetRef(874dd680…)；TriggerType=3；playIfWarlord=1
- **TriggerOnPlay**：effectAnim=AssetRef(5a60dbe6…)；TriggerType=6；playIfWarlord=1
- **End**：effectAnim=AssetRef(2b63fd56…)；TriggerType=1；playIfWarlord=1
- customObject：`fb07f52246771d5489569abf9c95df92`

### `Camouflage` — 伪装

- **OnPlay**：effectAnim=AssetRef(cd3972f0…)；TriggerType=2；playIfWarlord=1

### `CantAttack` — 不能攻击

- **Start**：effectAnim=AssetRef(31da3414…)；playIfWarlord=1

### `Codex` — 圣典

- **Trigger**：effectAnim=AssetRef(9869f9d0…)；TriggerType=3；playIfWarlord=1

### `Combo` — 组合

- **OnPlay**：TriggerType=2

### `Companion` — 伙伴

- **Start**：effectAnim=AssetRef(3fce8dde…)

### `Concussive` — 震荡的

- **Trigger**：effectAnim=AssetRef(20e1db36…)；TriggerType=3；playIfWarlord=1

### `Cruelty` — 残忍

- **Trigger**：effectAnim=AssetRef(9c0734f3…)；TriggerType=3；playIfWarlord=1
- customObject：`564290f63a8850048864c34c05945934`

### `DaemonicAllegiance`

- **OnPlay**：TriggerType=2；playIfWarlord=1

### `Duty` — 职责

- **TriggerOnPlay**：effectAnim=AssetRef(a8c3f891…)；TriggerType=6；playIfWarlord=1

### `Ecstasy` — 狂喜

- **TriggerOnPlay**：effectAnim=AssetRef(42abf1e9…)；TriggerType=6
- customObject：`42abf1e9d0e87d64b8e8e91ba150ad9b`

### `Ephemeral` — 短暂的

- （`_traitEffects` 为空）

### `Fast` — 快速

- **Start**：effectAnim=AssetRef(6144c1d4…)；playIfWarlord=1

### `Ferocity` — 凶残

- **Start**：effectAnim=AssetRef(0e5d5614…)
- **FromCode**：effectAnim=AssetRef(fa13471f…)；TriggerType=5

### `FirstStrike` — 先发制人

- **Start**：playIfWarlord=1

### `Flank` — 侧翼

- **Start**：effectAnim=AssetRef(8c379c5d…)；playIfWarlord=1

### `Flying` — 飞行

- **OnPlay**：effectAnim=AssetRef(a81f3470…)；TriggerType=2；playIfWarlord=1

### `Frenzied` — 毁灭者

- **Trigger**：effectAnim=AssetRef(6a3ebaf2…)；TriggerType=3；playIfWarlord=1

### `HuntMark` — 猎杀标记

- **TriggerOnPlay**：effectAnim=AssetRef(0a6f7af3…)；TriggerType=6；playIfWarlord=1
- **Start**：effectAnim=AssetRef(95778df2…)；playIfWarlord=1
- **OnPlay**：effectAnim=AssetRef(b664ea5d…)；TriggerType=2；playIfWarlord=1
- **FromCode**：effectAnim=AssetRef(e8dd2159…)；TriggerType=5；playIfWarlord=1
- **End**：effectAnim=AssetRef(8569ddf9…)；TriggerType=1；playIfWarlord=1

### `Invulnerable` — 无懈可击

- **OnPlay**：effectAnim=AssetRef(5d246ebf…)；overrideByArmies=[6 项]；TriggerType=2；playIfWarlord=1

### `Large` — 大型

- **Trigger**：TriggerType=3；playIfWarlord=1

### `LongRange` — 远程

- **FromCode**：effectAnim=AssetRef(64178449…)；TriggerType=5；playIfWarlord=1
- **End**：effectAnim=AssetRef(7e8ca71a…)；TriggerType=1；playIfWarlord=1

### `MarkOfChaos` — 黑暗契约

- （`_traitEffects` 为空）

### `MarkOfKhorne` — 血之黑暗契约

- **OnPlay**：effectAnim=AssetRef(abe2e28b…)；TriggerType=2；playIfWarlord=1

### `MarkOfNurgle` — 坚韧黑暗契约

- **OnPlay**：effectAnim=AssetRef(e3bd6529…)；TriggerType=2；playIfWarlord=1

### `MarkOfSlaanesh` — 过度黑暗契约

- **OnPlay**：effectAnim=AssetRef(1918dba5…)；TriggerType=2；playIfWarlord=1

### `MarkOfTzeentch` — 命运黑暗契约

- **OnPlay**：effectAnim=AssetRef(0e6be8de…)；TriggerType=2；playIfWarlord=1

### `Markerlight` — 标记灯

- **Start**：effectAnim=AssetRef(3bdfa049…)；playIfWarlord=1
- **OnPlay**：effectAnim=AssetRef(7ed23fc3…)；TriggerType=2；playIfWarlord=1
- **FromCode**：effectAnim=AssetRef(047f05d1…)；TriggerType=5；playIfWarlord=1
- **End**：effectAnim=AssetRef(e22d23a8…)；TriggerType=1；playIfWarlord=1

### `Mekaniak`

- **Trigger**：TriggerType=3；playIfWarlord=1

### `Mob` — 暴群

- **Trigger**：effectAnim=AssetRef(2a21bd10…)；TriggerType=3；playIfWarlord=1

### `NotDestroyRemnant`

- （`_traitEffects` 为空）

### `NotFerocity`

- （`_traitEffects` 为空）

### `Oath` — 誓言

- **Trigger**：effectAnim=AssetRef(e88772cb…)；TriggerType=3

### `OathAdditionalEffect`

- （`_traitEffects` 为空）

### `Pack` — 群体

- **Start**：effectAnim=AssetRef(098d35eb…)；playIfWarlord=1

### `Piercing`

- **Start**：playIfWarlord=1

### `PinDown` — 压制

- **OnPlay**：effectAnim=AssetRef(58c04a01…)；TriggerType=2；playIfWarlord=1

### `Pray` — 祈祷

- **Trigger**：effectAnim=AssetRef(96ce731b…)；TriggerType=3；playIfWarlord=1
- **TriggerOnPlay**：effectAnim=AssetRef(3a47c27d…)；TriggerType=6；playIfWarlord=1

### `Rage` — 忏悔

- （`_traitEffects` 为空）

### `Rally` — 集结

- **Start**：effectAnim=AssetRef(f21e512a…)；playIfWarlord=1

### `Regeneration` — 再生

- **Trigger**：effectAnim=AssetRef(3f3a5ba3…)；TriggerType=3；playIfWarlord=1

### `Regiment` — 团

- **Trigger**：effectAnim=AssetRef(d65e4c8c…)；TriggerType=3；playIfWarlord=1

### `Relentless` — 无情

- **Trigger**：effectAnim=AssetRef(e162a156…)；TriggerType=3；playIfWarlord=1

### `Remnant` — 残余

- （`_traitEffects` 为空）

### `Resolution` — 决议

- **Trigger**：TriggerType=3；playIfWarlord=1

### `Sentry` — 哨兵

- （`_traitEffects` 为空）
- customObject：`50b0ea0f4948557479de1e3db6c3d27e`

### `Shield` — 盾牌

- **OnPlay**：effectAnim=AssetRef(66b7b2b3…)；overrideByArmies=[4 项]；TriggerType=2；playIfWarlord=1

### `Shuriken` — 星镖

- **FromCode**：effectAnim=AssetRef(7d2ab149…)；TriggerType=5
- customObject：`cc61d807ec8234b3082ec764f07b4808`

### `Slay` — 消灭

- **Trigger**：effectAnim=AssetRef(7dc58e5b…)；TriggerType=3；playIfWarlord=1

### `Sniper` — 狙击手

- **FromCode**：effectAnim=AssetRef(254794f7…)；TriggerType=5；playIfWarlord=1

### `Stealth` — 隐身

- **OnPlay**：effectAnim=AssetRef(beab83fd…)；TriggerType=2；playIfWarlord=1

### `Stimulation` — 刺激

- **Trigger**：effectAnim=AssetRef(06c55347…)；TriggerType=3；playIfWarlord=1

### `Stomp` — 践踏

- **FromCode**：effectAnim=AssetRef(f91c06ce…)；TriggerType=5；playIfWarlord=1
- **Trigger**：effectAnim=AssetRef(04b0c6e3…)；TriggerType=3；playIfWarlord=1

### `Strike` — 攻击

- **Trigger**：effectAnim=AssetRef(3d21ccdc…)；TriggerType=3；playIfWarlord=1

### `Stun` — 眩晕

- **OnPlay**：effectAnim=AssetRef(be723eb2…)；TriggerType=2；playIfWarlord=1

### `Swarm` — 虫群

- **FromCode**：effectAnim=AssetRef(f387e589…)；TriggerType=5；playIfWarlord=1
- customObject：`6fdb2cebf898a4d36bce5ab60cef8589`

### `Synapse` — 突触节点

- **FromCode**：effectAnim=AssetRef(f587b0eb…)；TriggerType=5

### `Talent` — 才能

- **Trigger**：effectAnim=AssetRef(a36c3ce6…)；TriggerType=3

### `Teleport` — 传送

- **FromCode**：effectAnim=AssetRef(50728df9…)；TriggerType=5

### `Tide` — 潮汐

- **Start**：effectAnim=AssetRef(5af24b17…)；playIfWarlord=1

### `Unstable` — 不稳定

- **End**：effectAnim=AssetRef(5d7fa473…)；TriggerType=1

### `Unstoppable`

- （`_traitEffects` 为空）

### `Uprising` — 起义

- **TriggerOnPlay**：effectAnim=AssetRef(0c1819e9…)；TriggerType=6；playIfWarlord=1

### `Vanguard` — 先锋

- **OnPlay**：effectAnim=AssetRef(45746aad…)；TriggerType=2；playIfWarlord=1

### `Vulnerable` — 易受伤害

- **OnPlay**：effectAnim=AssetRef(9bb41fe7…)；TriggerType=2；playIfWarlord=1

### `Waystone` — 灵魂石

- （`_traitEffects` 为空）

### `TraitsCollection`

- （`_traitEffects` 为空）

## 三、三类待判（本轮只给判据，⛔ 不替下一轮下结论）

### 3.1 我们**完全没有**（.cs 里连子串都搜不到）

2 条：`NotFerocity`、`OathAdditionalEffect`

### 3.2 我们**有名字、但只出现在显示层**（Loc.cs / VfxMap.cs / 测试夹具里，规则层 `RuleEngine/` 与 `Battle/` 里没有）—— 这一类最可能「有图标没规则 / 语义不同」

4 条：

- `DaemonicAllegiance`（无中文）→ Badges.cs, TraitFrames.cs, AnimFXCheck.cs
- `FirstStrike`（先发制人）→ Badges.cs, TraitFrames.cs, AnimFXCheck.cs
- `Piercing`（无中文）→ Badges.cs, TraitFrames.cs, AnimFXCheck.cs
- `TraitsCollection`（无中文）→ TraitFrames.cs, TraitParticles.cs, VfxMap.cs

### 3.3 规则层有名字的（`RuleEngine/` 或 `Battle/`）—— 语义是否一致，下一轮逐条比

68 条：`Agenda`、`Ambush`、`Armour`、`Artifice`、`Backlash`、`Blast`、`Blind`、`BloodThirst`、`Camouflage`、`CantAttack`、`Codex`、`Combo`、`Companion`、`Concussive`、`Cruelty`、`Duty`、`Ecstasy`、`Ephemeral`、`Fast`、`Ferocity`、`Flank`、`Flying`、`Frenzied`、`HuntMark`、`Invulnerable`、`Large`、`LongRange`、`MarkOfChaos`、`MarkOfKhorne`、`MarkOfNurgle`、`MarkOfSlaanesh`、`MarkOfTzeentch`、`Markerlight`、`Mekaniak`、`Mob`、`NotDestroyRemnant`、`Oath`、`Pack`、`PinDown`、`Pray`、`Rage`、`Rally`、`Regeneration`、`Regiment`、`Relentless`、`Remnant`、`Resolution`、`Sentry`、`Shield`、`Shuriken`、`Slay`、`Sniper`、`Stealth`、`Stimulation`、`Stomp`、`Strike`、`Stun`、`Swarm`、`Synapse`、`Talent`、`Teleport`、`Tide`、`Unstable`、`Unstoppable`、`Uprising`、`Vanguard`、`Vulnerable`、`Waystone`

### 3.4 两条附注

- **`cards_engine.json` 里没有任何一张卡带它**（30 条）：`Blind`、`BloodThirst`、`CantAttack`、`Combo`、`DaemonicAllegiance`、`Fast`、`FirstStrike`、`Frenzied`、`HuntMark`、`Invulnerable`、`Large`、`LongRange`、`MarkOfChaos`、`MarkOfKhorne`、`MarkOfNurgle`、`MarkOfSlaanesh`、`MarkOfTzeentch`、`Markerlight`、`Mekaniak`、`NotDestroyRemnant`、`NotFerocity`、`OathAdditionalEffect`、`Piercing`、`PinDown`、`Rage`、`Relentless`、`Resolution`、`Stun`、`Vulnerable`、`TraitsCollection`。
  ⚠️ 这**不等于**「原版不用」—— 可能只出现在别的数据源（预组卡组 / 教程 / 遭遇战参数），也可能我们判过不做。**「判过 D」这件事 SO 里没有记录，要查台账**（本表不替你判）。
- **查不到官方词条 `Card_Trait/<name>`**（8 条）：`DaemonicAllegiance`、`Mekaniak`、`NotDestroyRemnant`、`NotFerocity`、`OathAdditionalEffect`、`Piercing`、`Unstoppable`、`TraitsCollection`。
  ⚠️ 只是「这个键不存在」，**不是「原版没有中文」** —— 可能键名规则不同（大小写/前缀），下一轮用 I2 全表按英文值反查一次再定。

### 3.5 字段层面的两条提醒（⛔ 别过度解读）

- 🔴 **`_stackable` 不是「层数」，是枚举 `CardTrait.TraitStackType`**（`dump.cs` TypeDefIndex 404）：`replace = 0` · **`add = 10`** · **`count = 20`**。实测 **72 条 = 10（add）**、**1 条 = 20（count，`MarkOfChaos`）**、1 条空（`TraitsCollection` —— 它不是词条，是集合 SO）。⇒ 本 build 里**所有词条都是 add 叠加**，只有 `MarkOfChaos` 是 count。⛔ 别把 10 读成「10 层」。
- `_aIScore` **74/74 条全是 0.0**（`TraitsCollection` 那条字段为空）—— 本 build 里 AI 评分**不走**这个字段。

