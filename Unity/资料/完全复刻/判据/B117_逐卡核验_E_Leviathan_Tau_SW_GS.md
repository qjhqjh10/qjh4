# B117 逐卡核验 E（Leviathan · TauEmpire · SpaceWolves · Genestealers）

> 只读核验代理 E · 2026-11-02 · 输入 = `判据/全卡核验_判定.tsv` 里本组 102 张「引擎判定 = 未验到」的卡（GS 24 · SW 25 · TAU 26 · TL 27）。
> 判据源：`RuleEngine/Resources/cards_truth.json`（效果树）+ `cards_engine.json`（`abilityTriggers` / `triggerAbilityIndices`）+ `RuleEngine/Core/*.cs`（实现面）。⛔ 没跑 Unity / ruleprobe。

## 一、结论速览

**本组 102 张：✅能执行 102 · 🟠真缺口 0 · ❓查不到 0。**
「未验到」是**测试电池覆盖缺口**（S3 分族 A1/A2/D/E/G），不是引擎缺口 —— 逐张回源后**每一张都能指到实现分支**。

**图例（下文「证据」列引用这些共用落点，⛔ 不逐行重抄）**

| 记号 | 共用落点 |
|---|---|
| **T0** `ThisCardPlayed(0)` | `RuleCore.cs:4222`（`FireTriggerOnBoard(Rally)` + `DispatchCardAbilityRows(...,0,...)`）；名表 `CardDef.cs:1917` |
| **T10** `OtherCardPlayed(10)` | `EffectResolver.cs:2724` `BroadcastWhen(Play)` → `:7639` `TN_OtherCardPlayed` |
| **T20** `WhileInPlay(20)` | `Aura.cs:180/582/804`（`UpdateWhileInPlay` 摘+重挂） |
| **T40** `TurnEnd(40)` | `RuleCore.cs:2630` `ResolveAtTurn(ctx,"turn_end")` |
| **T46** `Talent(46)` | `RuleCore.cs:13722` `SpawnTalents`（真值表 `TalentCardData.Resolve`）+ `:13759` `EmitTalentTrigger` |
| **T50** `UnitAttack(50)` | `RuleCore.cs:7166` `BroadcastWhen(Attack)` → `EffectResolver.cs:7642` |
| **T90** `OtherUnitDeath(90)` | `RuleCore.cs:10218` `BroadcastWhen(Die)` → `EffectResolver.cs:7641` |
| **T120** `Slay(120)` | `RuleCore.cs:6396` `FireTriggerAt(Slay)` |
| **T240** `WhileInPlayVariable(240)` | `Aura.cs:652/978`（重算，⛔ 不清费用档）+ `EffectResolver.cs:20241` |
| **T410** `FollowUpResolution(410)` | `RuleCore.cs:13036` `FireTriggerByNumber(410)` |
| **T515/T520** Markerlight/HuntMark | `EffectResolver.cs:7675/:7652` → `BroadcastWhen(GetsMarkerlight/GetsHuntMark)`（广播器 `:16732`） |
| **T580** `Strike(580)` | `RuleCore.cs:7058` `FireTriggerAt(Strike)` |
| **T620/630/726** Swarm/Synapse/TriggeredFerocity | `EffectResolver.cs:7714/:7720/:7711` → `BroadcastKeywordEvent`（`:2986` synapse 广播） |
| **T670/680/701** Ambush/Uprising/Sabotage | `RuleCore.cs:6521`（Ambush）· `RuleCore.cs:1556`+`FireUprising`（Uprising）· `EffectResolver.cs:7703`+`:4303`（Sabotage） |
| **TA** 目标选择 | `EffectText` 解析出 `EffectTargetSpec` → `EffectResolver.ResolveTargetsCore(:1469)`；`ta140/150` `RuleCore.cs:18025-18026` · `ta110` `:18011` · `ta60` `EffectResolver.cs:1446` |
| **BT** buffType 分派 | `EffectResolverPartial_B97W2.cs:205`（1/2/3/12/15/95/8 各有支）· 通用 `AddEffect` `EffectResolver.cs:12242` |
| **KW** 纯关键词卡 | armour `CardDef.cs:1356` · vanguard `RuleCore.cs:5573` · flying `:5711` · longrange `CounterAttack.cs:60` · flank `RuleCore.cs:3718` · pack `:5143` · stealth `ConditionEval.cs:525/533` · tide `CardInstance.cs:516` · shield `ConditionEval.cs:289` · blast `RuleCore.cs:7213` · camouflage `:6624` · ephemeral `RuleCore.cs:3007` · companion `CardDef.cs:4124` |

**按缺口类型聚类（0 条真缺口）**：T0 数据行 51 处 → 全部由正文 op 承担；战术/督军「trigger≠0」88 行的电池盲区 → 发射点逐条已接；`ta` 读数盲区 → `ResolveTargetsCore` 已支持本组全部 ta 码。

## 二、逐卡

| 卡id | 卡名 | 判定 | trigger | ta | buffType | 证据（文件:行号） | 备注 |
|---|---|---|---|---|---|---|---|
| GSC64 | Rusted Vent | ✅能执行 | 0 | 100/tc10·30/tc150 | 16/2 | `CreatePool.cs:1702`（逐字点名「锈蚀通风口」）· `EffectText.cs:6536`（`It costs 2 less`） | bt16 数据路未建，正文路覆盖（§三·1） |
| GSC26 | Aberrant Hypermorph | ✅能执行 | - | - | - | KW armour+vanguard（无一 ability 行，`triggerAbilityIndices=[]`） | 无效果树 |
| GSC29 | Metamorph Iconbearer | ✅能执行 | 0 | 100/tc180·30/tc150·100/tc140 | 1/2 | `RuleCore.cs:18025`（ta140 建池，`B116_W39` 专为 GSC29 补）· `EffectText.cs:6535` | ta140 池内仅 3 张 |
| GSC30 | Metamorph Leader | ✅能执行 | 240/680 | 30/tc50·30/tc10 | 2/1 | `Aura.cs:652` 逐字点名 GSC30（减价光环不清费用档）· T680 `RuleCore.cs:1556` | 电池 A1/A2 盲区 |
| GSC37 | Patriarch | ✅能执行 | 46/670/240 | 100·30/tc10·40/tc20 | 1/2 | `CardDef.cs:4366`（`costs N less for each card in enemy hand` → `CostPerEnemyHandCard`）· `RuleCore.cs:1035` 读点 · T46/T670 | Talent=Deadliest Killer 在池内 |
| GSC19 | Purestrain Genestealer | ✅能执行 | - | - | - | KW flank | 无效果树 |
| GSC40 | Backstab | ✅能执行 | 0/90 | 10/tc30·100 | - | `EffectText.cs:2652`（`If the target dies`→`PreviousTarget`）· `TryCreate` `:9177` · T0/T90 | — |
| 7c666a04c1b9e8b4e9a96247e9fe115a | Cult Propaganda | ✅能执行 | 40/0 | 100·3 | - | `EffectText.cs:5659-5660` 逐字点名（手牌陷阱 + 送对手手牌）· T40 | 电池 D 族盲区 |
| f45dd53721b645b4ab45d334edb986af | Deadliest Killer | ✅能执行 | 0/90 | 10/tc30·0/tc65 | - | T0 + `Deal 6 damage`／`draw a troop`（tc65=牌库 `CardPoolCriteriaFilter.cs:72`） | — |
| GSC55 | Eve of the Uprising | ✅能执行 | 0 | 270/tc60·30/tc150·470/tc150 | 2/2 | `EffectText.cs:4876`（`Return up to 5 … and lower their cost by 3` 逐字例）· `EffectText.cs:6579`（`died this game`） | 电池 A2 盲区 |
| d01a5ba5865ded4418194ea50e333b21 | Inscrutable Cunning | ✅能执行 | 0 | 360/tc180 | - | `EffectText.cs:3958`（`Choose one: A; B or C`）· `RuleCore.cs:17474`（ca360 在执行器闸内） | — |
| 68909cf02f472734ea391665e0d28e15 | Jammed Communications | ✅能执行 | 10/0 | 10/tc10·3 | - | `EffectText.cs:6840` 逐字点名（「When you play a Stratagem, your Warlord takes 1 damage」）· T10 | 电池 D 族盲区 |
| GSC39 | Lying in Wait | ✅能执行 | 0 | 140/tc30·30/tc30 | 95 | `EffectText.cs:4885`（`reduce its cost to 1`）· `EffectResolverPartial_B97W2.cs:460-490`（`case 95 setCost`） | — |
| a7bb23355a30d344e9d2decf976d91e1 | Poisoned Supplies | ✅能执行 | 40/0 | 10/tc10·3 | - | `EffectText.cs:4073/5659` 逐字点名 · T40 | 电池 D 族盲区 |
| GSC54 | Rampant Infestation | ✅能执行 | 0 | 50 | - | `EffectText.cs:8406` `ReSubjectCostLess`（`Your troops cost 3 less this turn`） | — |
| GSC74 | Rogue Informant | ✅能执行 | 0 | 0/tc65·30/tc150 | 3 | `EffectText.cs:11072`（`If it's a troop, give it …` 逐字例）· tc65 牌库 | — |
| GSC44 | Subterranean Ambush | ✅能执行 | 0 | 70/tc60 | - | `EffectText.cs:6579`（`died this battle`）· `EffectResolver.cs:1446`（ta60 墓地专路） | — |
| GSC49 | Telepathic Domination | ✅能执行 | 0 | 180/tc30·30/tc30 | 3/15 | `EffectText.cs:7942-7947`（`Take control of …` 唯一判据）· bt15 见 `EffectResolverPartial_B97W2.cs:678` | 账 `战斗逻辑驱动.tsv:167/541/713` 已 A 落地 |
| GSC68 | Atalan Jackal | ✅能执行 | 701 | 10/tc10 | - | `EffectResolver.cs:7703`（701 映射）+ `:4303`（SabotageCreated 发射） | — |
| GSC71 | Atalan Leader | ✅能执行 | 240/580 | 30/tc50·465/tc10 | 2/2 | `EffectText.cs:7687` 逐字点名（`Trigger the Ambush abilities of all friendly troops`）· T580 `RuleCore.cs:7058` | ENG-16① 回退不影响本卡 |
| GSC36 | Goliath Rockgrinder | ✅能执行 | - | - | - | KW armour+blast（`RuleCore.cs:7213`） | 无效果树 |
| GSC67 | Jackal Scout | ✅能执行 | 0 | 30/tc50 | 2 | `EffectText.cs:504-505` 逐字点名（`increase its cost by 1`，对手手牌） | — |
| GSC2 | Magus Uthrel Naas | ✅能执行 | 46/ACT | 100·20/tc30 | 1 | T46 → `Brood Prophet` 在池内（`TalentCardData.Resolve` 真值表 `unitsToSummon`）；`hasActiveAbility=0` ⇒ 主动能力**按原版短路**（`CardDefActiveAbility.cs:21-40`） | act 块是原版死数据 |
| GSC1 | Primus Saffa Rhiannor | ✅能执行 | 46/ACT | 100·10/tc30·462/tc20 | - | 同上（`Inscrutable Cunning` 在池内） | 同上 |
| SW1 | Logan Grimnar | ✅能执行 | 46/ACT | 100·420/tc65 | - | T46 → `High King of Fenris` 在池内；`Start the game with … in hand` = `startingCardInHand` 列（`Data/CardDatabase.cs` 挂载口） | 「Tyrnak and Fenrir」属开局手牌列，不属能力 |
| SW7 | Deathwolf Scout | ✅能执行 | 726 | 10/tc10 | 2 | `EffectResolver.cs:7711`（726→`TN_TriggeredFerocity`）+ `:7826` 同族闸 · KW camouflage `RuleCore.cs:6624` | 电池 D 族盲区 |
| SW9 | Fenrisian Wolf | ✅能执行 | 410/410/ALT | 40/tc20·238/tc30·320/tc20 | 1/1 | `RuleCore.cs:17474` 注释逐字点名 SW9（ca320 在执行器闸内）· T410 `RuleCore.cs:13036` | 强制攻击 `forceattack` = `EffectResolver.cs:1068/3278` |
| SW24 | Frost Claw Wulfen | ✅能执行 | - | - | - | KW pack `RuleCore.cs:5143` + armour | 无效果树 |
| SW15 | Fyrri Askar | ✅能执行 | 240 | 30/tc10 | 3 | `Aura.cs:76` 逐字点名（`Friendly units with Pack have Invulnerable during your turn`）· 时长 `Aura.cs:223/752` | — |
| SW16 | Long Fang | ✅能执行 | 50 | 10/tc110 | - | T50 `RuleCore.cs:7166`；ta110 相邻 `RuleCore.cs:18011` | — |
| SW30 | Terminator Rune Priest | ✅能执行 | 0 | 10/tc10 | - | T0 + `2-3 damage`（min/max 区间）；`traitsFilter:1260`（huntMark）`TargetCriteriaEval.cs` | — |
| SW31 | Wolf Guard Terminator | ✅能执行 | - | - | - | KW vanguard+armour | 无效果树 |
| SW14 | Wolf Priest | ✅能执行 | 90/0 | 20/tc10·20/tc30 | - | T90 `RuleCore.cs:10218`（观察者 90 那一族）+ T0；heal `EffectResolver` 通用 | — |
| SW17 | Wulfen | ✅能执行 | - | - | - | KW pack | 无效果树 |
| SW43 | Anvil of Endurance | ✅能执行 | 0/0 | 30/tc90·20/tc90 | 1/1 | `TryEitherOr`（`EffectText.cs:6064` 区，无 `choose` ⇒ 随机，符合 CLAUDE.md §三 口径）· bt1 addStats `:12396` | — |
| SW57 | Birth of a Saga | ✅能执行 | 0×6 | 0/tc65·250/tc150 | 3/1 | `EffectText.cs:394/4222`（`Each player deploys … from your deck` 逐字点名）· `:10103`（`deployed this way`） | 电池 A2 盲区 |
| SW53 | Embers of Prospero | ✅能执行 | 0/0 | 30/tc30·110/tc30 | 3/3 | `EffectText.cs:617`（`If it already had Hunt Mark, destroy it instead` 逐字例） | — |
| SW49 | Fenrisian Blizzard | ✅能执行 | 0/0 | 3/tc10·30/tc10·10/tc10 | 3 | `EffectText.cs:7288`（`blind` op）+ `DoBlind`；`RuleCore.cs` 清盲（`B-126` 只影响投影显示） | 电池 E 族盲区 |
| SW58 | Fenrisian Wolfpack | ✅能执行 | 0/240 | 70·40/tc20 | 2 | `活状态表 B-ENG-24`（`B115_W18`/`B116_W9` 已修 + 阶梯实测 9→3→0） | 已核销 |
| SW56 | Let Loose | ✅能执行 | 0/410 | 30/tc10·320/tc20·260/tc10 | 1/1/15 | `EffectText.cs:9728` 逐字点名（`… and attacks a random enemy`） | — |
| SW46 | Raid Tactics | ✅能执行 | 0 | 30/tc120 | 15 | `EffectText.cs:737` 逐字点名（`For the rest of this battle, when a friendly troop uses Ferocity, …`） | — |
| SW42 | Savage Legacy | ✅能执行 | 0/0 | 10/tc30·70 | - | `EffectText.cs:3823`（`If target dies, put in play …` = `deploy`）· T0 | — |
| b551c8351ebe5784faf9912b573eaf93 | Tempest's Wrath | ✅能执行 | 0 | 30/tc30 | 1 | bt1 addStats + `until your next turn` 时长；KW ephemeral `RuleCore.cs:3007` | — |
| SW36 | Bjorn the Fell-Handed | ✅能执行 | 20/ACT | 30/tc10·10/tc10 | 3 | `EffectText.cs:7800` 逐字点名（`When a friendly unit uses Ferocity, it stays in play`）· T20 `Aura.cs` | — |
| SW34 | Murderfang | ✅能执行 | 120 | 20/tc20·40/tc20·480/tc20 | 3/3 | T120 `RuleCore.cs:6396`；`gain Blood Thirst this turn` `EffectText.cs:2918` | 电池 D 族盲区 |
| SW32 | Stormwolf | ✅能执行 | 90 | 70 | - | T90；`deploy a Grey Hunter`（`SW10` 在池内） | — |
| SW35 | Venerable Dreadnought | ✅能执行 | 520 | 10/tc80·10/tc106 | - | `EffectResolver.cs:7652`（520 映射）· ta106/tc80（actingCard/targetIfMeets）`TargetCriteriaEval.cs:113` | 账 `战斗逻辑驱动.tsv:655` 已 A 落地 |
| SW2 | Njal Stormcaller | ✅能执行 | 46/726 | 100·30/tc50 | 2 | `EffectText.cs:8406`（`High Rune Priest costs 1 less this turn`）→ `EffectResolver` `DoLowerCost` 按名找（`CreatePool.FindByName`）；T46/T726 | 手牌按名降费 |
| SW3 | Ragnar Blackmane | ✅能执行 | 46/ACT | 100·60 | - | T46 → `War Howl` 在池内 | — |
| TAU27 | Broadside Battlesuit | ✅能执行 | - | - | - | KW armour+longrange+companion | 无效果树 |
| TAU11 | Crisis Battlesuit | ✅能执行 | - | - | - | KW flying `RuleCore.cs:5711` | 无效果树 |
| TAU17 | Crisis Bodyguard | ✅能执行 | - | - | - | KW shield+vanguard | 无效果树 |
| TAU26 | Stealth Shas'ui | ✅能执行 | - | - | - | KW stealth+companion | 无效果树 |
| TAU4 | Guardian Drone | ✅能执行 | - | - | - | KW vanguard | 无效果树 |
| TAU8 | Missile Drone | ✅能执行 | - | - | - | KW flying+blast | 无效果树 |
| TAU15 | Sniper Drone | ✅能执行 | 515 | 40/tc20 | 3 | `EffectText.cs:4184/8759` 逐字点名该句（⚠️ 注释把 id 写成 `TAU20`，见 §五）· T515 `EffectResolver.cs:7675` | — |
| TAU32 | Aun'Shi | ✅能执行 | 46/20 | 100·30/tc50 | 2 | T46 → `Serene Unifier` 在池内；T20 `Aura.cs` 常驻减价（`ReSubjectCostLess` `:8406`） | — |
| TAU14 | Fire Warrior Breacher | ✅能执行 | - | - | - | KW vanguard | 无效果树 |
| TAU23 | Breacher Shas'ui | ✅能执行 | - | - | - | KW vanguard | 无效果树 |
| TAU12 | Darkstrider | ✅能执行 | 46/0 | 100·30/tc100 | 3 | T46 → `Structural Analyzer` 在池内；T0 + `adjacent Drones`（ta100） | — |
| TAU5 | Fire Warrior | ✅能执行 | - | - | - | KW longrange | 无效果树 |
| TAU22 | Strike Team | ✅能执行 | - | - | - | KW companion `CardDef.cs:4124` + `PlayCompanions` | 无效果树 |
| TAU16 | Vespid Stingwings | ✅能执行 | - | - | - | KW flying+flank+armour | 无效果树 |
| TAU57 | Burgeoning Empire | ✅能执行 | 90/0 | 40/tc20·0 | 2/2 | `EffectText.cs:4172/8733`（`Lower cost by 1 when <事件>`）· T90 | 电池 A2 盲区 |
| TAU58 | Coordinated Engagement | ✅能执行 | 0/0 | 110/tc30·30/tc30 | 3 | `EffectText.cs:617`（`already had … destroy it instead`）· `SetRemoveMarkerlight`（账 `战斗逻辑驱动.tsv:904`） | — |
| 507f79fa60945d04e93fe806aca912b7 | Deploy Anchors | ✅能执行 | 0 | 30/tc30 | 1 | `EffectText.cs:6064` 区逐字引（`to a friendly Battlesuit or Vehicle` 的 or 不切句）· bt1 | — |
| TAU47 | Dynamic Offensive | ✅能执行 | 0 | 0/tc65·30/tc150 | 3 | `EffectText.cs:11019` 逐字点名（`If they are Battlesuits, give them Flank`） | — |
| TAU48 | Emergency Dispensation | ✅能执行 | 0 | 0/tc65·30/tc150 | 2 | `EffectText.cs:6535`（`Choose a troop in your deck. Draw it and lower its cost by 3` 逐字例） | — |
| TAU42 | Experimental Drones | ✅能执行 | 0 | 30/tc120 | 12 | `EffectText.cs:1040` 逐字点名（`… all Drones you deploy` → `Deployed` 旗 + `LastTargets`）；bt12 执行器 `EffectResolverPartial_B97W2.cs:678` | — |
| TAU51 | Saviour Protocols | ✅能执行 | 0 | 50·30/tc120·100/tc180 | 12/12 | `EffectText.cs:6671`（`add it to your hand`）· bt12 同上 | 有一行 buff 缺省（原版同） |
| 684196db974ab254090f39ef6e89868c | Shield Generator | ✅能执行 | 0 | 30/tc30 | 3 | bt3 addTrait（shield）`EffectResolver.cs:12242`；ephemeral `RuleCore.cs:3007` | — |
| TAU40 | Technological Supremacy | ✅能执行 | 0/410 | 30/tc30·320/tc20·30/tc90 | 1/1/3/3 | `EffectText.cs:11111` 逐字点名（`If it's a Vehicle or Battlesuit, give it Long Range`）· ca320 `RuleCore.cs:17474` | — |
| TAU31 | Hammerhead | ✅能执行 | - | - | - | KW longrange+companion | 无效果树 |
| TAU20 | Piranha | ✅能执行 | 20 | 30/tc50 | 2 | T20 `Aura.cs` + `ReSubjectCostLess` `EffectText.cs:8406` | — |
| TAU30 | Sky Ray Gunship | ✅能执行 | - | - | - | KW longrange | 无效果树 |
| TL38 | Hive Guard | ✅能执行 | - | - | - | KW vanguard | 无效果树 |
| TL67 | Barbgaunt | ✅能执行 | - | - | - | KW swarm+tide+blast | 无效果树 |
| TL19 | Broodlord | ✅能执行 | 630 | 450/tc20 | - | `EffectText.cs:4151/7916`（`it applies the effect twice` → `extratrigger`）· `EffectResolver.cs:1114` `DoExtraTrigger` | ca450 走正文路 |
| TL7 | Gargoyle | ✅能执行 | - | - | - | KW swarm+flying+tide+flank | 无效果树 |
| TL2 | Hormagaunt | ✅能执行 | - | - | - | KW swarm+tide 4 | 无效果树 |
| TL13 | Lictor | ✅能执行 | - | - | - | KW stealth | 无效果树 |
| TL60 | Neurogaunt Nodebeast | ✅能执行 | - | - | - | KW synapse+swarm+tide | 无效果树 |
| TL66 | Neurogaunt | ✅能执行 | - | - | - | KW synapse+swarm+tide | 无效果树 |
| TL65 | Neuroloid | ✅能执行 | - | - | - | KW synapse `EffectResolver.cs:2986` | 无效果树 |
| TL1 | Ripper Swarm | ✅能执行 | - | - | - | KW swarm+tide 3 | 无效果树 |
| TL61 | Stranded Termagant | ✅能执行 | - | - | - | 卡面无 desc、`triggerAbilityIndices=[]`、无 keywords ⇒ **无效果可跑**（与原版数据一致） | 电池无从测 |
| TL75 | Termagant Brood | ✅能执行 | 620 | 40/tc20 | 1 | `EffectResolver.cs:7714`+`:7826`（620 target 闸 `B115_W14` 已修，TL75 放行） | ENG-16② 已核销 |
| TL4 | Termagant | ✅能执行 | - | - | - | KW swarm+tide | 无效果树 |
| TL15 | Tyranid Prime | ✅能执行 | 620 | 30/tc30 | 1 | 同上（620 闸）；`活状态表 B-314`（`B116_W12/W22` 已修并核销） | 已核销 |
| TL5 | Tyranid Warrior | ✅能执行 | - | - | - | KW synapse | 无效果树 |
| TL12 | Tyrant Guard | ✅能执行 | - | - | - | KW vanguard+armour 2 | 无效果树 |
| TL10 | Von Ryan's Leaper | ✅能执行 | - | - | - | KW swarm+tide+stealth | 无效果树 |
| TL18 | Zoanthrope | ✅能执行 | 630 | 10/tc10 | - | `EffectResolver.cs:7720`+`:2986`（Synapse 广播） | 池内 630 仅 TL18/TL19/TL70 |
| TL69 | Neurotyrant | ✅能执行 | 10 | 100/tc80·30/tc150 | 25/95 | `EffectResolver.cs:4201` 逐字点名（`create an Ephemeral copy of it`）· `CreatePool.cs:104`（`ephemeral` 前缀）· bt25 `EffectResolverPartial_B97W2.cs:159` | T10 + `non-Ephemeral Stratagem` 过滤 |
| TL70 | Norn Emissary | ✅能执行 | 630 | 40/tc20 | 2 | `EffectText.cs:8684` 逐字点名（`Lower cost by 2 every time a friendly unit triggers Synapse`）· `:8733` | — |
| TL21 | Screamer-Killer | ✅能执行 | - | - | - | KW armour+blast | 无效果树 |
| 468db99b4b10d0745bab346df37d3fb2 | Feeding Frenzy | ✅能执行 | 0/90 | 10/tc30·30/tc10 | 1 | T0/T90 + `damaged enemy` 条件（`conditionsFilter`）· bt1 `EffectResolver.cs:12396` | — |
| TL47 | Hive Fleet Leviathan | ✅能执行 | 0 | 50·30/tc120·0/tc65 | 12 | bt12 附魔（`EffectResolverPartial_B97W2.cs:678`）；`Your troops cost 1 less for the rest of this battle` `EffectText.cs:8406` | 电池 A2 盲区 |
| TL50 | Infinite Biomorphologies | ✅能执行 | 0 | 260/tc50 | 16 | `EffectResolver.cs:5283`（`LeviathanEffectPool` 六项，逐字点名 TL50）· 手牌挂效果 `AttachEffectToHandInstances` `EffectResolver.cs:6389` | bt16 数据路未建，正文路覆盖（§三·1） |
| TL41 | Overwhelming Swarm | ✅能执行 | 0 | 50 | 1 | `EffectText.cs:8406` + 兵种筛 `SubtypeFilterOfKindNoun`（`活状态表 B-203` 已修，TL41 在列） | 已核销 |
| TL46 | Rapacious Hunger | ✅能执行 | 0/90 | 10/tc10·20/tc10 | 1/1 | `EffectText.cs:5292`（`for each one that dies`）· T90 | — |
| TL34 | Tervigon | ✅能执行 | 46/ACT | 100·10/tc30 | - | T46 → `Brood Progenitor` 在池内 | 同 GSC2（act 为原版死数据） |

## 三、真缺口清单（🟠）

**0 条「原版会执行、我们完全不结算」的缺口。** 只有一条**数据路**缺口（不影响可执行性，如实记账）：

1. **`buffType 16 choiceEnchantment` 数据路未建** —— `EffectResolverPartial_B97W2.cs:691-701` 显式「整档不结算」，理由 = 原版 `CardScript__AddEffect.c:830-932` 还有四组 `spellType` 旁支（`0x3c` 置 secretBuff · `0x78` 摘旧 quest · `0x82` 用掉+触发 · `0x8c/0xbe` 写 `lastMarkOfChaos`+广播）。
   · 受影响卡 = **GSC64**（`bt 16/2`）· **TL50**（`bt 16`）。
   · **为什么不算真缺口**：这两张卡的**正文 op 非空** ⇒ `RuleCore.FireTriggerRowsByChosenAbility`（`EffectResolver.cs:21078`）**不进入兜底**，`ApplyAbilityBuff` 这条路根本不跑；行为由正文路承担（GSC64 = `CreatePool.cs:1702` 造牌 + `It costs 2 less`；TL50 = `LeviathanEffectPool` 六项 + 手牌挂效果）。
   · 建议：记账即可，⛔ 别派写手（改了也没有消费者）。

## 四、查不到的（❓）

无。102 张全部指到了实现分支；三面（trigger / ta / buffType）本组用到的取值**没有一个**落在「无分支」档：
trigger 集合 {0,10,20,40,46,50,90,120,240,410,515,520,580,620,630,670,680,701,726} 全有发射点（§一）；ta 集合 {10,20,30,50,60,65,80,90,100,106,110,120,140,150,180,None} 全有解析支；buffType 集合 {1,2,3,12,15,16,25,95} 除 16 外全有支（16 见 §三）。

## 五、顺手发现

1. 🔴 **代码注释里的卡 id 写错了（铁律 5 就地记账）**：`EffectText.cs:4184` 与 `:8759` 写「出处：`TAU20 Sniper Drone` 的 `Gains Long Range when an enemy gains Markerlight`」，但现读 `cards_engine.json`：**`TAU15` = Sniper Drone**（该句正是 TAU15 的 desc），**`TAU20` = Piranha**（desc = `Your Drones cost 1 less.`）。⇒ 那两处注释的 id 应改 `TAU15`。只影响注释，**不影响解析**（注释只是旁证）。
2. **`EffectText.cs:5150/5160` 与 `CardDefCostPerKeyword.cs:32` 两处说法表面冲突、实际都对**：前者说 GSC37 的 `Costs 1 less for each card in enemy hand`「整句 unparsed、根本没进 `ClassifyCount`」，后者说它「走的是 `CostPerEnemyHandCard`」。真身 = 它**不走计数从句分类器**，而走 `CardDef.MatchStaticBattleRule` 的独立正则（`CardDef.cs:4366-4368`）⇒ `CostPerEnemyHandCard=1`（`RuleCore.cs:1035` 读点）。两处合看才完整，⛔ 别照前一处判「GSC37 降费没做」。
3. **本组 34 张「纯关键词卡」的 `triggerAbilityIndices` 是 `[]`** —— 它们在 `pool_effect_check` 里恒「未验到」是**结构性**的（没有效果树可测），⛔ 不是缺陷。建议 S3 生成器把这类卡直接判「无效果树 ⇒ 免测」，可一次消掉本组 34 行噪声。
4. **6 张督军（SW1/SW2/SW3/GSC1/GSC2/TL34）的 `activeAbility` 块是原版死数据**（`hasActiveAbility=0`，原版 `CardScript__ResolveActiveAbilityPlayed.c:22` 短路）—— 我们已按 `CardDefActiveAbility.cs:21-40` 同样短路 ⇒ **正确复刻**。但 S3 的 A1 族夹具仍在测「用主动能力」，对本组 6 张是**测不到真行为的空转**。
5. **`TAU51 Saviour Protocols` 有一行 `buff` 缺省（`buffType=None`）** —— 与原版数据一致（该行只带 `handBuff`），不是解析丢失。
