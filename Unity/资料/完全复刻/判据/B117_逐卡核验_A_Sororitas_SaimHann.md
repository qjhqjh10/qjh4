# B117 逐卡核验 A（Sororitas · SaimHann）

> 核验代理 A · 2026-11-02 · 只读代码、只写本文件 · 未跑 Unity / ruleprobe / git。
> 输入：`全卡核验_判定.tsv` 里本组 87 行「引擎判定 = 未验到」（Sororitas 45 · SaimHann 42）。

## 一、结论速览

**本组 87 张：✅ 86 · 🟠 1 · ❓ 0。**

- **引擎的主执行路径是「卡面 `desc` → op 流」，不是 `chosenAbility`/`buffType` 数值行**
  （`CardDef` 构造 → `EffectText.Parse` → `EffectResolver.EffectDispatch`，`Core/EffectResolver.cs:1061`；
  数值行的消费者 `FireTriggerRowsByChosenAbility` 自己写明**只在正文 op 为空时**才兜底，`EffectResolver.cs:21078`）。
  ⇒ 判一张卡「能不能发挥作用」，主判据是 **desc 解析出 op + 那个动词有 handler + 那个时机有人广播**。
- **85/87 张 desc 都解析出 ≥1 条 op**（`_tmp_view/w41s3b/after/all_dump.tsv` 的 `faceOps`，全部 `playRc=0 played=1`）。
  两例外：`ASH61 Weapons Platform`（原版就是白板：`desc`/`abilities`/`keywords` 全空）、
  `Pray Enchantment`（附魔模板，`manaCost=-1` ⇒ `playRc=20 = ErrNotPlayable` 是**原版就许的**，`RuleCodes.cs:88`）。
- ⇒ 本组 `pool_diff4` 的「未验到」**绝大多数是电池抬不起那个时机**（闸自陈：`trigger 605/655/130/660 电池不触发`、
  `电池只覆盖 unit（本卡 type=tactic）`、`需要玩家在弹窗里选`），**不是引擎缺分支**。逐条已在 §二「备注」标出。
- **唯一真缺口 = `Daemonbreaker` 的 `ephemeral`**（§三·1）。
- **数值行层另有一档无执行器**（`buffType 2 changeCost` / `buffType 4 removeTrait` / `ta 320/325/440/462`），
  但**这批卡全部有正文覆盖**，属「行层缺执行器、功能不缺」——见 §三·2（不计入 🟠）。

聚类（一行一条）：

聚类：纯关键词卡 17（8 个关键词机制全在）· 信仰 `N ☀:` 前缀 26（`EffectText.cs:8514`）·
祈祷 `Pray:`/`When … prays` 12（`RuleCore.cs:12159` / `:12295`）· 忏悔 `Penitence:` 5（`RuleCore.cs:9235`）·
灵石 `N [Spirit Stone]:` 15（`CardDef.SpiritOps`）· 电池抬不起的「主动能力」桶 4（`gate=0` ⇒ **原版也不消费**，
`RuleCore.cs:15573`，非缺口）· `ephemeral` 只在 `cardTraits` 1（🟠 `Daemonbreaker`）。

## 二、逐卡

关键词卡的「证据」= 该关键词的机制落点；其余 = 主解析器 handler + 时机广播点。

| 卡id | 卡名 | 判定 | trigger | ta | buffType | 证据（文件:行号） | 备注 |
|---|---|---|---|---|---|---|---|
| ASH63 | Dire Avenger Exarch | ✅ | 0 | 30 | 4 | `EffectText.cs:4536`（并列子句 lose Stealth）；Rally 口 `RuleCore.cs:4222` | bt4 行层无 case，正文覆盖 |
| ASH62 | Dire Avenger | ✅ | — | — | — | 关键词 shuriken `RuleCore.cs:6922`；waystone `RuleCore.cs:10020` | 电池只覆盖 ability/trigger |
| ASH22 | Fire Dragon | ✅ | — | — | — | blast `RuleCore.cs:7224`；waystone `:10020` | 同上 |
| ASH4 | Guardian Defender | ✅ | — | — | — | shuriken `:6922`；waystone `:10020` | 同上 |
| ASH79 | Orian Laratharjos | ✅ | 46,647 | 100,30 | 3 | `WhenEvent.cs:1334`（loses stealth）＋广播 `RuleCore.cs:6620`；天赋 `RuleCore.cs:13753` | 电池不触发 647 |
| ASH6 | Ranger | ✅ | — | — | — | stealth `RuleCore.cs:5531`；sniper `CounterAttack.cs:64`；waystone `:10020` | 关键词卡 |
| ASH5 | Storm Guardian | ✅ | — | — | — | shuriken `RuleCore.cs:6922`；waystone `:10020` | 关键词卡 |
| ASH23 | Striking Scorpion Exarch | ✅ | — | — | — | shuriken `:6922`；stealth `RuleCore.cs:5531`；waystone `:10020` | 关键词卡 |
| ASH11 | Striking Scorpion | ✅ | — | — | — | 同上 | 关键词卡 |
| ASH7 | Veteran Guardian | ✅ | — | — | — | shuriken `:6922`；waystone `:10020` | 关键词卡 |
| ASH61 | Weapons Platform | ✅ | — | — | — | 原版白板（`desc`/`abilities`/`keywords` 全空）；`playRc=0 played=1` | 无效果可执行，非缺口 |
| ASH31 | Winged Autarch | ✅ | 240 | 30 | 1 | 光环 `Aura.cs:154`（`your other`）＋`:219`（`have` 正则）＋`:466`（`Flying`→Keyword） | `WhileInPlayVariable` 不广播，光环走常驻层 |
| ASH75 | Yrlla the Huntress | ✅ | 291 | 10 | — | `WhenEvent.cs:1246`（receives stun）＋广播 `RuleCore.cs:7448` | 电池不触发 291 |
| ASH84 | Ahnakh-Yth Shrine | ✅ | 0,600 | 30,100 | 3 | `EffectText.cs:9167`（create a copy of X in hand）；灵石 `CardDef.SpiritOps` | 电池不触发 600 |
| ASH48 | Cosmic Serpent | ✅ | 0,600 | 440 | — | `EffectText.cs:7716`（trigger the abilities requiring Spirit Stones）→ `DoTriggerAbility` `EffectResolver.cs:1120` | ta440 属「登记触发」族，正文覆盖 |
| ASH71 | Craftworld Convergence | ✅ | 0 | 360 | 1 | `EffectText.cs:5963`（TryChooseOne）＋ `gainspirit` handler `EffectResolver.cs:1135` | 电池「需要玩家选」 |
| ASH39 | Deadly Ambush | ✅ | 0 | 30,130 | 3 | `EffectText.cs:6257`（Give X or Stun Y = 随机二选一）；stun `EffectResolver.cs:4373` | 电池「卡面 or」 |
| ASH64 | Devoted of Khaine | ✅ | 0,600 | 30 | 1 | give+repeat：`DoGive` `EffectResolver.cs:1122` / `DoRepeat` 表内 | 电池不触发 600 |
| ASH49 | Eldritch Storm | ✅ | 0,600 | 10 | — | `DoDeal` `EffectResolver.cs:1063` + repeat | 同上 |
| ASH85 | Fate Inescapable | ✅ | 240,0 | 40,30 | 2,3 | `EffectText.cs:8900`（this costs N less if…）＋ vulnerable 载荷 `GivePayload.cs:135` | bt2 行层无落点，正文覆盖 |
| ASH36 | Fire and Fade | ✅ | 0 | 140,30 | 2 | `return` `EffectResolver.cs:1097`；`DoLowerCost` `:15553` | bt2 行层无落点 |
| ASH86 | Forewarned | ✅ | 0,600 | 30 | 1,3 | `DoGive` `:1122`（`Also give it Shield` 用 prev 目标） | 电池不触发 600 |
| ASH52 | Hosts of the Dead | ✅ | 0,600 | 70 | — | `EffectText.cs:4266`（Spend all your Spirit Stones）＋ `Deploy` `EffectResolver.cs:1141` | 同上 |
| ASH40 | Nightshade Interceptors | ✅ | 0 | 30 | 1 | `EffectTargetSpec.Kind` 含 `vehicle`（`EffectText.cs:1083`）；`CreatePool.cs:1399` 认 `vehicle` | `playRc=18` = 夹具无合法目标（量具限制） |
| ASH53 | Reclaim the Stars | ✅ | 0,600 | 0,30 | 2 | `DoDraw` `EffectResolver.cs:1074`；指代降费 `:15569` 读 `DrawnThisResolve` | bt2 行层无落点 |
| 851f73daf747eb642934a16429b391db | Storm of Silence | ✅ | 0,600 | 30,130 | 1 | `DoStun` `EffectResolver.cs:4373` + give | 电池不触发 600 |
| ASH43 | Sudden Assault | ✅ | 0 | 260 | — | `EffectText.cs:4048`（eachunitdeal）→ `EffectResolver.cs:1066` | 电池「EnemyUnit 需点目标」 |
| ASH54 | Wailing Doom | ✅ | 0,600 | 10 | — | `DoDeal` `:1063`；`units adjacent to the target` 由 `EffectText.cs:9888` 认 | 电池不触发 600 |
| ASH41 | Webway Gate | ✅ | 0,600 | 0,30 | 2 | `TryChooseCard` `EffectText.cs:6562`（deck 来源）＋ `DoLowerCost` `:15553` | 需玩家选牌 |
| ASH73 | Whirling Death | ✅ | 0,410 | 10,325 | — | `EffectText.cs:10988`（If the target dies）→ `ConditionHolds` `EffectResolver.cs:14634` | ta325 行层「出声不跑」，正文覆盖 |
| ASH44 | Wild Host | ✅ | 0 | 100,30 | 2 | `DoCreate` `EffectResolver.cs:1140`；`They cost 1 less` 指代 `EffectText.cs:11572` | keywordFilter/armyFilter 由 TargetCriteriaEval |
| ASH38 | Will of Asuryan | ✅ | 0,600 | 30,0 | 3 | `DoGive` `:1122`（Vanguard）+ `DoDraw` `:1074` | 电池不触发 600 |
| d3e99cb18b8cbba4081486f57f510f75 | Witchfire | ✅ | 0,600 | 10 | 3 | `DoDeal` `:1063` + repeat；ephemeral `BattleContext.cs:1543` | 电池不触发 600 |
| 33eabec155f628440a426a4ba480ec24 | Wrath of Khaine | ✅ | 0,600 | 30 | 1 | `DoGive` `:1122` + repeat | 同上 |
| ASH81 | Bright Lance Vyper | ✅ | 455,410 | 320,40 | 3,1 | `WhenEvent.cs:1364`（trigger spirit stone ability）＋广播 `EffectResolver.cs:16790` | ta320 行层只对战术派，正文覆盖 |
| ASH27 | Falcon | ✅ | — | — | — | flying `RuleCore.cs:5712`；shuriken `:6922` | 关键词卡 |
| ASH68 | Night Spinner | ✅ | — | — | — | flying `:5712`；sniper `CounterAttack.cs:64`；blast `RuleCore.cs:7224` | 关键词卡 |
| ASH10 | Shroud Runner | ✅ | — | — | — | flying `:5712`；flank `RuleCore.cs:3679`；sniper `CounterAttack.cs:64` | 关键词卡 |
| ASH18 | Vyper | ✅ | — | — | — | flying `:5712`；shuriken `:6922` | 关键词卡 |
| ASH26 | War Walker | ✅ | — | — | — | blast `RuleCore.cs:7224`；armour `DamageAfterReduction` `RuleCore.cs:7608` | 关键词卡 |
| ASH9 | Windrider | ✅ | — | — | — | flying `:5712`；flank `:3679`；shuriken `:6922` | 关键词卡 |
| ASH1 | Anvirr Keltoc | ✅ | 46 | 100,10 | — | 天赋 `RuleCore.cs:13753`（SpawnTalents） | `activeAbility.gate=0` ⇒ 原版不消费（`RuleCore.cs:15573`） |
| SOR71 | Adelaide the Serene | ✅ | 0,605 | 60,40,70,462 | 3 | 信仰前缀 `EffectText.cs:8514`；`faithcheck` 条件 `EffectResolver.cs:14947`；计数 `for each` `EffectText.cs:5041` | `gate=0`；电池无 Rally 时机 |
| SOR26 | Aestred-Thurga | ✅ | 655,660 | 40,30,462 | 15,3,1 | `Prays` 广播 `RuleCore.cs:12295`；Pray 正文 `:12159`；附魔 `Enchantment.cs:755` | bt15 = 祈祷附魔，手写等价物 |
| SOR8 | Arco-flagellant | ✅ | 130 | 10 | — | `FireTriggerOnBoard(Penitence)` `RuleCore.cs:9235` | 电池不触发 130 |
| SOR6 | Battle Sister Gunner | ✅ | 655 | 40,10,462 | 15,1 | `UseAlternative` `RuleCore.cs:12159` + `DoDeal` `EffectResolver.cs:1063` | 电池不触发 655 |
| SOR23 | Canoness | ✅ | 605 | 40,30,462 | 1,3 | 信仰前缀 `EffectText.cs:8514`；`DoGive` `EffectResolver.cs:1122` | `gate=0`；电池不触发 605 |
| SOR34 | Celestian Superior | ✅ | 655,605 | 40,70,462 | 15,1 | Pray `RuleCore.cs:12159`；`Deploy` `EffectResolver.cs:1141` | 电池不触发 |
| SOR7 | Crusader | ✅ | 605 | 40 | 1 | 信仰前缀 `EffectText.cs:8514`；`DoGive` `:1122` | 电池不触发 605 |
| SOR21 | Death Cult Assassin | ✅ | 130 | 10 | — | `RuleCore.cs:9235`；`DoDeal` `:1063`（random） | 电池不触发 130 |
| SOR11 | Dominion | ✅ | — | — | — | blast `RuleCore.cs:7224` | 关键词卡 |
| SOR29 | Ephrael Stern | ✅ | 655,180 | 40,60,10,462 | 15,3 | Pray `RuleCore.cs:12159`；`If target dies` `EffectText.cs:10988` → `EffectResolver.cs:14634` | 电池不触发 |
| SOR9 | Preacher | ✅ | 46,605 | 100,40 | 3 | 天赋 `RuleCore.cs:13753`；信仰前缀 `EffectText.cs:8514` | 电池不触发 605 |
| SOR67 | Sacred Rose Sister | ✅ | 655,605 | 40,462 | 15,1,3 | Pray `RuleCore.cs:12159`；两档信仰 `EffectText.cs:8514` | 电池不触发 |
| SOR19 | Seraphim | ✅ | 605 | 40 | 1 | 信仰前缀 `EffectText.cs:8514`；`DoGive` `:1122` | 电池不触发 605 |
| SOR31 | Dogmata | ✅ | 0 | 10,60 | — | Rally `RuleCore.cs:4222`；`targetdiesorfriendly` `EffectText.cs:10980` | 电池无 Rally 时机 |
| SOR4 | Sister Novitiate | ✅ | 655,605 | 40,70,462 | 15,1 | Pray `RuleCore.cs:12159`；Deploy `EffectResolver.cs:1141` | 电池不触发 |
| SOR17 | Sister Superior | ✅ | 655 | 40,10,462 | 15,1 | Pray `RuleCore.cs:12159`；`DoDeal` `:1063` | 电池不触发 655 |
| SOR10 | Sister Repentia | ✅ | 130,90 | 40,60 | 1 | Penitence `RuleCore.cs:9235`；`Kills` 广播 `RuleCore.cs:6401` | 电池不触发 130/90 |
| SOR37 | Triumph of St. Katherine | ✅ | 655,660 | 40,30,10,462 | 15,1,2 | Pray `RuleCore.cs:12159`；`equal to your Faith` `EffectText.cs:3486` | bt2 行层无落点，正文覆盖 |
| SOR69 | Zephyrim Superior | ✅ | 655 | 40,0,462 | 15 | Pray `RuleCore.cs:12159`；`drawtype` `EffectResolver.cs:1183` | 电池不触发 655 |
| SOR28 | Zephyrim | ✅ | — | — | — | flying `RuleCore.cs:5712`；flank `:3679` | 关键词卡 |
| SOR56 | Beacon of Faith | ✅ | 0,605 | 30 | 1 | `DoGive` `EffectResolver.cs:1122`；信仰前缀 `EffectText.cs:8514` | 电池不触发 605 |
| SOR53 | Blade of Faith | ✅ | 0,605 | 10 | — | `DoDeal` `:1063` + repeat | 同上 |
| e01f4d54e8a474d96b5a1167cc28eebf | Daemonbreaker | 🟠 | 0,605 | 30 | 1,3 | 主体效果 ✅ `DoGive` `:1122`；`ephemeral` **无落点** —— 见 §三·1 | **真缺口：回合末不会被移出** |
| SOR75 | Devout Serenity | ✅ | 0 | 30,20 | 3 | `EffectText.cs:1205`（`Each friendly unit that is Praying heals 3`）；`DoHeal` `EffectResolver.cs:3722` | 电池夹具无祈祷单位 |
| SOR58 | Devout Warriors | ✅ | 0,605 | 0 | — | `drawtype` `EffectResolver.cs:1183` | 电池不触发 605 |
| SOR51 | Divine Intervention | ✅ | 0,605 | 110 | 8 | `EffectText.cs:10122`（highest attack 选法）；destroy `EffectResolver.cs:1087`；bt8 `EffectResolverPartial_B97W2.cs:498` | 电池不触发 605 |
| SOR54 | Fiery Conviction | ✅ | 0,605 | 30 | 1 | `GivePayload.cs:279`（`might` → `attack`）；`DoGive` `:1122` | 电池不触发 605 |
| 8901e35c5de30eb48804da6651cfb132 | Fury of the Righteous | ✅ | 0,605 | 10 | 1 | `DoDeal` `:1063`；ephemeral `BattleContext.cs:1543` | 电池不触发 605 |
| 4d348cff71208c24d8e39252d913ff9d | Hymn of Battle | ✅ | 0 | 360 | 1 | `TryChooseOne` `EffectText.cs:5963` | 电池「需要玩家选」 |
| SOR48 | Imperial Creed | ✅ | 0,605 | 130,60 | — | `DoStun` `EffectResolver.cs:4373`；`refill` `:1137` | 电池不触发 605 |
| 65cfec7d2bc719040b3b4b88622931b2 | Lead the Righteous | ✅ | 0,605 | 70 | 3 | `Deploy` `EffectResolver.cs:1141` | 电池不触发 605 |
| SOR47 | Miraculous Feat | ✅ | 0,605 | 30 | 3,4 | `paidmod`（Extend effect）`EffectText.cs:1647` | bt4 行层无 case，正文覆盖 |
| SOR39 | Prayer | ✅ | 0 | 30,60 | 3 | `DoGive` `:1122`；`has Pray` 条件 → `targethaskw` `EffectText.cs:11111` | 电池「有条件」 |
| SOR50 | Relics of St. Katherine | ✅ | 0,605 | 60,0 | 3 | `gainfaith` `EffectResolver.cs:1134`；`DoDraw` `:1074` | 电池不触发 605 |
| f104d4c21abe84546abc9365d9eda8dd | Righteous Repugnance | ✅ | 0,605 | 30,20 | 1 | `DoGive` `:1122`；`DoHeal` `:3722`；`instead` 换值 `PeelInstead` | 电池不触发 605 |
| SOR42 | Rite of Restoration | ✅ | 0 | 70 | — | `EffectText.cs:6577`（died since your last turn）＋ `:6668`（deploy it）＋ `DeadCandidates` `EffectResolver.cs:4835` | 夹具不铺坟场 |
| SOR52 | Spirit of the Martyr | ✅ | 0,90 | 10 | — | 条件重复 `EffectText.cs:8449`；`DoDeal` `:1063` | 电池不触发 90 |
| SOR55 | Trial of Suffering | ✅ | 0,605 | 10,30 | 1 | `DoDeal` `:1063` + `DoGive` `:1122` + `instead` | 电池不触发 605 |
| SOR72 | Castigator | ✅ | 660 | 10,70,462 | — | `Prays` 广播 `RuleCore.cs:12295`；天气 `DoDeal` `:1063` | `gate=0`；电池不触发 660 |
| SOR32 | Mortifier | ✅ | 130 | 10 | — | `RuleCore.cs:9235`；`DoDeal` `:1063`（enemy warlord） | 电池不触发 130 |
| SOR38 | Paragon Warsuit | ✅ | 665,605 | 10,40 | 3 | `WhenEvent.cs:1033`（gain faith）＋广播 `EffectResolver.cs:13259` | 电池不触发 665 |
| SOR27 | Penitent Engine | ✅ | 130 | 40 | 3 | `RuleCore.cs:9235`；`DoGive` `:1122`（Flank + Blast 2） | 电池不触发 130 |
| SOR70 | Sororitas Rhino | ✅ | 40 | 70,462 | — | `PrayedOnly` 条件 `EffectText.cs:11026`；`atturn` `EffectResolver.cs:1140`；Deploy `:1141` | 电池「净差 0」= 夹具无祈祷单位 |
| SOR44 | Moment of Grace | ✅ | 0,605 | 30,0 | 1 | `DoGive` `:1122`；`DoDraw` `:1074` | 电池不触发 605 |
| 9218576fd3e26554db5b4c239a36dbc7 | Pray Enchantment | ✅ | 60,31 | 340,60 | 3 | 手写等价链 `Enchantment.cs:755`（AttachPray）/ `:783`（RemovePray）；判据链 `EffectResolver.cs:13290-13305` | `playRc=20` 是原版哨兵（`RuleCodes.cs:88`），非缺陷 |

## 三、真缺口清单（🟠）

### 1. `Daemonbreaker`（`e01f4d54e8a474d96b5a1167cc28eebf`）—— `ephemeral` 不生效

- **缺什么**：这张卡**回合结束时不会从手牌消失**（原版会）。
- **数据面**：`cards_engine.json` 里 `keywords = []`、`desc` 也不含 `Ephemeral`；
  只有 `cardTraits = ["Ephemeral"]`（`cards_truth.json` 的 `cardTraits` 同样是 `ephemeral` / `_trait 5`）。
- **引擎那行长什么样**：`CardDef.Has(string keyword)` = `return _keywords.ContainsKey(keyword);`
  （`Core/CardDef.cs:6140-6143`），而 `_keywords` 只由 `KeywordTable.Parse(keywords)` 灌（`Core/CardDef.cs:1318`）；
  `cardTraits` 走的是**另一套**（`AttachCardTraits`，`Core/CardDef.cs:6412`）。
  消费者 `BattleContext.IsEphemeral` = `inst.Card.Has("ephemeral") || inst.EphemeralMarked`（`Core/BattleContext.cs:1546`）
  ⇒ 两个来源**都不成立** ⇒ `SweepEphemeral`（`Core/RuleCore.cs:3007`，调用点 `:2821`）扫不到它。
- **旁证（量具也看见了）**：`_tmp_view/w41s3b/after/all_dump.tsv` 的 `fixtureFit` 列，
  另三张真 ephemeral 卡（`Fury of the Righteous` / `Witchfire` / `Righteous Repugnance`）都是 `ephemeral`，
  **只有 Daemonbreaker 是空**。
- **原版该做什么**：卡面 `Ephemeral` ⇒ 回合结束若还在手牌 ⇒ **移出游戏**（不是弃置）。
- **判据出处**：关键词语义 `Core/CardDef.cs:8306-8312`（`Implemented` 的 `"ephemeral"` 那段，规则书 `:183` / `:229`）；
  清扫口 `Core/RuleCore.cs:3007`。
- **本代理没做的**：没说该改数据还是改引擎（`_keywords` 与 `cardTraits` 的分工是 `B8_Q` 裁定过的口径，`CardDatabase.cs:1730`）
  ⇒ 交调度台裁；**⛔ 本代理零改动**。

### 2. 数值行层无执行器（**不计 🟠：功能由正文覆盖**）

`ApplyAbilityBuff` 的 `switch (b.BuffType)`（`Core/EffectResolverPartial_B97W2.cs:215`）覆盖
`1 / 2 / 3 / 4 / 5 / 6 / 10 / 11 / 25 / 30 / 40 / 45 / 70 / 85 / 90 / 95` + `default`（8 / 12 / 15 / 16）。

- **`buffType 2 changeCost`**：`case 2`（`:273`）**只出声、不改状态**。本组 6 张（ASH85/ASH36/ASH53/ASH41/ASH44/SOR37）
  **全部由正文 `lowercost` 覆盖**（`Core/EffectResolver.cs:15553`）。
- **`buffType 4 removeTrait`**：**switch 里没有这一档** ⇒ 落 `default` ⇒ 出声「本表尚未实现」。本组 2 张
  （ASH63「lose Stealth and Camouflage」、SOR47「Extend effect」）**全部由正文覆盖**（`EffectText.cs:4536` / `:1647`）。
- **`ta 320/325/440/462`**：分别由 `CardAbilityDispatch_B116.cs`（只对战术派）、正文、`triggerability` 兜住；
  本组 ASH73 / ASH81 / ASH48 的**正文都在**。
- ⇒ 这一档**今天对玩家不可观测**（`FireTriggerRowsByChosenAbility` 只在正文 op 为空时才走，`EffectResolver.cs:21078`），
  列出仅供账目对齐。

## 四、查不到的（❓）

**无。** 本组 87 张的每个效果节点都找到了落点。

## 五、顺手发现（⛔ 未改动任何文件）

1. **`ASH5 Storm Guardian` 的 `desc` 与 `descZh` 不一致**：英文只有 `Shuriken 1`（无 Waystone），
   中文是 `[[waystone]]。<br>[[shuriken]] 1`。`keywords` 列**含 Waystone** ⇒ 引擎行为取 `keywords`，是对的；
   但卡面英文缺那一行 —— 可能是官方英文表的抄录缺口，值得核对 `desc` 数据侧。
   同族：`ASH18 Vyper`（英文只有 `Shuriken 3`，中文含 `[[flying]]`）、`ASH26 War Walker`（英文 `Armour. Blast` 无数值）。
2. **`SOR71 Adelaide the Serene` 的 `activeAbility` 块在 `cards_truth.json` 里存在，但 `cards_engine.json` 的
   `activeAbilityLogic[0].gate = 0`** ⇒ 原版不消费。`pool_diff4` 把它当「未验到」报了 2 行；
   这两行应当**从「未验到」里剔掉**（同 `SOR23` / `SOR72` / `SOR70` / `ASH1`）。这不是卡的问题，是量具口径。
3. **`SOR42 Rite of Restoration` 的 `pool_diff4` 备注「ta=60：目标在墓地」把 `targetCriteria.targetsAffected` 当成了 `chosenAbility`** —— 它的 `chosenAbility` 是 `70`（summonUnit），`60` 是目标档。两者是**两个轴**，备注里的「ta=」前缀容易让后来的会话读错。
4. **`ASH85 Fate Inescapable` 的 AB1 有 `buffType 3 + trait 480 + costChange -1`**：`costChange` 在
   `case 3`（`EffectResolverPartial_B97W2.cs:291`）**不会被应用**（只有 `case 1` 那一行提到 costChange 也未建）。
   卡面 `Give Vulnerable 2 to an enemy troop` 没有降费 ⇒ 这一格疑似**数据噪声**，不影响判断，
   但若将来有人按数据行结算会多降 1 费。
5. **`SOR10 Sister Repentia` 的 `abilityTriggers` 是 `90 OtherUnitDeath`，卡面写的是
   `When this unit kills an enemy`**。原版靠 `actingCardCriteria{targetsAffected:20}`（= 自己）把 90 收窄成「我杀的」；
   我们走正文 `Kills`（`RuleCore.cs:6401`）。**两条路等价**，但账里若按 trigger 号对账会误判成「trigger 不符」。
