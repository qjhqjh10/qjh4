# B117 逐卡核验 C（DarkAngels · Goff）

> 输入 = `判据/全卡核验_判定.tsv` 里 `引擎判定 = 未验到` 的 **68 张**（DA 34 + GOF 34）。
> 交叉读数 = `_tmp_view/w41s3b/after/pool_diff4.tsv`（1701 行，逐 `(卡, chosenAbility)` 的**实测**判定 + **闸**）。
> 代码面 = `RuleEngine/Core/*.cs` · `RuleEngine/Data/*.cs`（只读，未改一行）。

## 一、结论速览

**本组 68 张：✅ 68 · 🟠 0 · ❓ 0。**

- **一条都不是「引擎没有分支」**：`pool_diff4.tsv` 里本组每一条「未验到」行的 **第 9 列都写着一条**「闸：…」，
  逐条读下来**全部是夹具/读数档位**，没有一条写「无该分支 / 落 default / 静默跳过」。归类（按闸聚类）：
  - **A 电池只覆盖 `unit`**（tactic/defence 的 88+ 行族）：本组 12 张（`GOF104 ca30` · `GOF102 ca30` · `GOF31 ca260` · `708c… ca20` · `3ebb… ca30` …）。
  - **C 电池 `steps` 没走通**（`play=0,oath=14,selfChip=…`）：本组 9 张（`DA8` · `DA22` · `DA24` · `DA6` · `GOF20` · `GOF25` · `GOF81` · `GOF82` · `DA66`）。
  - **D 电池不触发该 trigger**（695 / 700 / 645）：本组 5 张（`DA5` · `DA12` · `DA34` · `DA15` · `DA66` · `GOF68`）。
  - **E `ta` 读数看不见**（150 / 50 / 60 / 65）：本组 10 张（`3e99…` · `DA56` · `DA40` · `DA72` · `GOF39` · `GOF85` · `GOF94` · `GOF100` · `GOF102` · `GOF107`）。
  - **F 目标/条件过滤**（`selectFromChooseResolution` / `keywordFilter` / `armyFilter`）：本组 7 张（`DA16` · `DA11` · `DA46` · `DA42` · `GOF103` · `GOF102` · `GOF107`）。
  - **G 附魔注册**（`buffType 12/15`，效果在后续事件）：本组 2 张（`DA55` · `DA42`）。
  - **I 按计数结算 / multiplier 恒 0**：本组 2 张（`GOF4` · `GOF104 ca380`）。
  - **K 同卡其它效果噪声**：本组 8 张（`DA1` · `DA2` · `DA3` · `DA28` · `DA33` · `DA29` · `DA38` · `DA43` · `GOF90` 的 `ca3`）。
  - **L 夹具结构上不适用**（`ephemeral` / `defence`）：`DA61` · `20d0…` · `GOF53`。
  - **M 出牌失败**（`rc=18`）：**5 张** —— `7c45…`(Master of Manoeuvre) · `5080…`(Wreckin' Ball) · `98d3…`(Ramshackle) · `9dcb…`(Mekaniak) · `9f4e…`(More Dakka)。**见 §五①，本组给出推定根因**。
  - **N 造牌 `unitsToSummon` 为空**：`DA44` · `DA72`（`ca 270`，要选的那张在运行期才定）。

### 判据图例（下表「证据」列用的短码）
- **T0** `ThisCardPlayed` = `Core/ActionPump.cs:1099`（Rally→ThisCardPlayed）＋ `Core/CardAbilityDispatch_B116.cs:124`（卡级派发行）。
- **T46** `Talent` = `Core/CardDef.cs:1920` ＋ `Core/EffectText.cs:3410`（`Talent: <名>` 造牌）＋ `Core/RuleCore.cs:13722`（`SpawnTalents`）。
- **T120** `Slay` = `Core/ActionPump.cs:1100` ＋ `Core/RuleCore.cs:6271`（击杀判定与派发，唯一口）。
- **T300** `Mob` = `Core/ActionPump.cs:1104` ＋ `Core/EffectResolver.cs:7737`（`Mob`→`OtherCardMob` 645 那一跳）。
- **T50/90/190/260/TurnEnd** = `WhenEvent` 事件层：`Core/EffectResolver.cs:7642`(Attack) · `:7641`(Die) · `:7640`(Deploy) · `:7644`(Draw)；
  正文结算入口 = `FireBoardWhenListener` `Core/EffectResolver.cs:10397`；棋盘单位回合起止 = `ResolveAtTurn` `Core/EffectResolver.cs:8896`。
- **T81** `Unstable` = `Core/RuleCore.cs:10195`（抬事件）/`:13625`（Blast+自毁）/`:18235`（死亡那一跳）。
- **T410** `FollowUpResolution` = `Core/EffectResolver.cs:20679` ＋ `Core/RuleCore.cs:13036`。
- **T490** `QuestTriggered` = `Core/EffectResolver.cs:13548-13550`（督军卡上的 490 能力，`TriggerOps("questtriggered")`）。
- **T695/T700** = `Core/EffectResolver.cs:7701`（`GainQuest`→695）/`:7702`（`CreatesSecret`→700），广播点 `:13243` / `:13602`。
- **TA** = `Core/TargetCriteriaEval.cs:102-260`（按 `targetsAffected` 的早返 switch + 常规 36 格）＋ 建池侧 `Core/EffectResolver.cs:1446`（`ta=60`）/`:1469`（`ta=140/150/200/201`）。
- **BF** = `Core/EffectResolver.cs:1064-1143` 的 `EffectDispatch` 表（`deal` 1064 · `heal` 1073 · `draw` 1074 · `return` 1086 · `blind` 1094 · `extratrigger` 1114 · `give` 1122 · `gainquest` 1128 · `chooseone` 1132 · `persist` 1137 · `create` 1140 · `deploy` 1141 · `lowercost` 1142 · `repeat` 1143）。
- **CA** = `chosenAbility` 执行面；数值表 = `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityEffect.cs`，族映射 = `Core/CardDef.cs:3493`。
- **AB** = 主动能力 = `Core/RuleCore.cs:15496`（`UseActiveAbilityCore`，闸 `ActiveAbilityAvailable`）＋ 替代行动 `UseAlternative`（`agenda` 一族）。

## 二、逐卡

| 卡id | 卡名 | 判定 | trigger | ta | buffType | 证据（文件:行号） | 备注 |
|---|---|---|---|---|---|---|---|
| DA5 | Apothecary | ✅能执行 | 0,695 | 30,80 | — | T0/T695；TA 30/80；BF heal 1073 | 695 是夹具闸（D） |
| DA16 | Captain | ✅能执行 | 0 | 65 | — | T0；`decktop` EffectResolver.cs:5097；CA 420=setCardDeckToTop | 闸 F：`selectFromChooseResolution`+牌库只读 id |
| DA11 | Chaplain | ✅能执行 | 0 | 65 | — | 同 DA16 | 同 DA16 |
| DA24 | Company Champion | ✅能执行 | 120 | 10 | — | T120；`targetSelectMethod=1 highestAttack` CountEval.cs:962-980；BF deal 1064 | 闸 C（电池没抬起 Slay） |
| DA28 | Company Master | ✅能执行 | 260 | 80 | 2 costChange | T260 EffectResolver.cs:7644；BF lowercost 1142 | 闸 K |
| DA12 | Company Veteran | ✅能执行 | 695 | 20 | — | T695；TA 20（self）；BF heal 1073 | 闸 D |
| DA8 | Reiver | ✅能执行 | 120 | — | — | T120；BF draw 1074 | 闸 C（`softAtk=skip(would-kill)`） |
| DA6 | Sergeant Taaman | ✅能执行 | 120 | — | mana+1 | T120；BF gainquest 1128 | 闸 C |
| DA22 | Techmarine | ✅能执行 | 50 | 20,80 | mana+1 | T50；TA 20/80；BF heal+gainquest | 闸 C |
| 3e9960… | Convoke the Circle | ✅能执行 | 0 | 150 | 2 costChange | T0；BF draw 1074 + lowercost 1142；TA 150 EffectResolver.cs:1469 | 闸 E（`ta=150` 读数看不见） |
| DA56 | Desperate Mission | ✅能执行 | 0 | 65,150 | 2 costChange | T0；`drawtype` + lowercost | 闸 E |
| DA43 | Grim Resolve | ✅能执行 | 0 | 10,20,30 | mana+1 | T0；`deal`+`If target dies` WhenEvent.cs 条件 | 闸 K |
| DA40 | Inner Circle | ✅能执行 | 0 | 65,150 | 2 costChange | `decktop` 5097 + lowercost | 闸 E+F |
| a84a5b… | Master Interromancer | ✅能执行 | 0,90 | 20,30 | mana+1 | T90；`deal`+`If target dies` | 已 对（pool_diff4） |
| 7c45a6… | Master of Manoeuvre | ✅能执行 | 0 | 30 | 2 costChange | BF `return` 1086 + lowercost 1142；CA 140=recallToOwnerHand | **闸 M `rc=18`**，见 §五① |
| DA61 | Plasma Generator | ✅能执行 | 0 | 20 | mana+1(createMana) | BF `gainenergy`；TA 20 | 闸 L（`defence`） |
| DA42 | Reconnaissance Mission | ✅能执行 | 0 | 50 | 15（singleUse） | TA 50 `spec.HandOnly` 1469；附魔注册 `Core/EnchantmentExecutor_B115.cs:189`（`buffType∈{15,16}`） | 闸 F+G（效果在后续事件） |
| DA44 | Secret Agenda | ✅能执行 | 0 | 180 | — | `todeck` 专用支 `EffectResolver.cs:5117-5152`（含 `CreatesSecret` 广播）+ 1152 | 闸 N（选的那张运行期定） |
| 708c03… | Smothering Decree | ✅能执行 | 0,90 | 10 | — | T90；`deal` + `for each … dies heal` | 已 对 ca10；ca20 闸 A |
| dd535f… | Supreme Grand Master | ✅能执行 | 0 | 10 | 1 melee/ranged | BF `give` 1122；`create … in your hand` 1140 | 已 对 |
| DA46 | The Rock | ✅能执行 | 0 | 180 | — | `Choose one:` 解析 `EffectText.cs:3958/5948` → BF `chooseone` 1132；TA 180（`[]` ⇒ 不筛） | 闸 F；`ca 360` 卡级层出声不跑（`CardAbilityDispatch_B116.cs:315`），**正文层已覆盖，无双发** |
| DA55 | Wages of Retribution | ✅能执行 | 0 | 120 | 12（常驻） | BF `persist` 1137（`For the rest of this battle` `EffectText.cs:3864`）；TA 120 不筛 | 闸 G |
| DA29 | Darkshroud | ✅能执行 | 410 | 10,20,30,90 | 3 addTrait | T410；AB `UseActiveAbilityCore` 15496（走 `agenda` 替代行动）；BF `give` 1122 | 已 对 ca30/325；ca60 闸 K |
| DA33 | Nephilim Jetfighter | ✅能执行 | 0 | 10,20,30 | mana+1 | `If it has Flying … instead` 条件换值（`EffectText.cs:14680` 那族 `targethaskw`） | 已 对两行 |
| DA15 | Ravenwing Champion | ✅能执行 | 0,700 | 10 | mana+1 | T700 7702 + `:5143`/`:13602` 广播；`WhenEventKind.Play` 7644 | 闸 D（700） |
| DA14 | Ravenwing Speeder | ✅能执行 | 410 | 10,30,90 | mana+1 | T410；AB 走 `agenda`；`If the target survives` 条件（`EffectText.cs:7481/7561`） | 已 对 ca10/325 |
| DA38 | Repulsor | ✅能执行 | 0 | 10,20,30 | mana+1 | `deal` 1064 + `Blast` `RuleCore.cs:7213` + `If the target dies`（`EffectText.cs:1821` 明列 DA38） | 已 对 ca10 |
| DA34 | Unforgiven Redemptor | ✅能执行 | 695 | 10 | — | T695；`Armour` `RuleCore.DamageAfterReduction` | 闸 D |
| DA3 | Asmodai | ✅能执行 | 46,490 | 20 | mana+1 | T46+T490；AB 15496；CA 100=createCardInHand `DoCreate` 1140 | 闸 K（督军宿主多跑） |
| DA1 | Azrael | ✅能执行 | 46,490 | 20,65 | — | 同 DA3；`decktop` 5097 | 已 对 ca100；ca270/420 闸 K |
| DA2 | Belial | ✅能执行 | 46,120,490 | 65 | — | T46+T120+T490；`drawtype`（troop） | 已 对 ca0/100 |
| DA72 | Vengeful Brethren | ✅能执行 | 0 | 30,150 | 1 melee+health | `Create a copy of it at the top of your deck` `EffectText.cs:9167`；BF `give` | 闸 N（`ca270` 选的那张运行期定） |
| DA66 | Ravenwing Ballistus Dreadnought | ✅能执行 | 10,700 | 10 | — | T700 7702 + `Play`(10) 7639；`Armour` | 闸 C+D |
| DA75 | Master Zacharial | ✅能执行 | 46,490 | 20,50 | mana+1 | T46+T490；AB `UseActiveAbilityCore`；`return` 1086 | 已 对 ca0/100/300/60 |
| GOF93 | Hunta Rig | ✅能执行 | 0,410 | 20,50,150,201 | — | `ta=201` 引用表 `EffectResolver.cs:1469`+`SetReferenceCards`；`CA 250=autoPlayFromHand` `EffectResolver.cs:4064`；`CA 235=scriptedSelfAttack`（`This troop attacks it`→`forceattack` 1068）；`Stomp` `RuleCore.cs:7244` | 已 对 ca235/250/320 |
| GOF94 | Painboss | ✅能执行 | 0,46 | 60 | — | T46；`reanimate` 1144（墓地随机野兽）；`ta=60` 墓地池 1446 | 闸 E（墓地读数） |
| GOF81 | Thump Gun Beast Snagga | ✅能执行 | 300 | 20 | 3 addTrait(blast) | T300；`give`/`gain`（本回合 Blast 1） | 闸 C |
| GOF90 | Wurrboy | ✅能执行 | 40,81 | 10 | 3 addTrait | TurnEnd `ResolveAtTurn` 8896；T81 10195；`for each friendly troop` multiplier `CountEval` | 已 对 ca10/260；`ca 3 = doOnlyVfx`（`AbilityEffect.cs:4`，正是「只播特效」），闸 K |
| GOF100 | Cyber-augmentation | ✅能执行 | 0 | 50,150 | 1 melee+3 flank | TA 50（手牌池）+ `give` 1122 + 随机挑（`targetSelectMethod=0`） | 闸 E+F（手牌属性不在读数） |
| GOF103 | Da Hunt is On | ✅能执行 | 0 | 30 | 3 flank | `deal` + `The next … you play` `EffectText.cs:447`（handBuff `limitedUses` 8697） | 已 对 ca10；ca50 闸 F |
| GOF104 | Drag it Down | ✅能执行 | 0,410 | 10,20,30,50,90 | 1 healthChange | T410；`for each friendly unit` multiplier `CountEval`；`CA 380 setReferenceCard` `CardAbilityDispatch_B116.cs:196` | 已 对 ca260/320/380 中 2 条；ca30 闸 A |
| GOF107 | Fasta Than Yooz | ✅能执行 | 0 | 10,150 | 2 costChange | `Create <N> random <kind> in your hand` `EffectText.cs:9165` + `CreatePool.Resolve`；lowercost 1142 | 已 对 ca100；ca30 闸 E+F（`chosenArmy`/`armyFilter`） |
| GOF102 | Thundering Stampede | ✅能执行 | 0,410 | 20,50,65 | 2 costChange | `drawtype`（Beast）+ lowercost；T410 | 已 对 ca0；ca30 闸 A；ca320 闸 F（`keywordFilter`） |
| GOF82 | Beast Snagga Nob | ✅能执行 | 40 | 50 | 1 melee | TurnEnd `ResolveAtTurn` 8896 + 手牌池 TA 50 | 闸 C |
| GOF85 | Snakebite Nob | ✅能执行 | 0 | 50 | 1 melee+health | 手牌池 TA 50 + `give` 1122 | 闸 E+F |
| GOF68 | Big Choppa Nob | ✅能执行 | 645 | 80 | — | `When … triggers Mob` → `WhenEventKind.Triggers("mob")` `WhenEvent.cs:1395`；645 名映射 7737；`it triggers an additional time` → BF `extratrigger` 1114；`CA 461=triggerMobAdditional` | 闸 D |
| GOF15 | Burna Boy | ✅能执行 | 0 | 10 | — | `If you control a Spanner or Mekboy Gazmek` → `controlcount` `EffectResolver.cs:14688` + `ParseControlAsk` `:16103` + `UnitMatchesControlAlt` | 闸 F |
| GOF1 | Grot | ✅能执行 | — | — | — | 无 ability，只有 trait `tide`（`RuleCore.cs:4257` 潮涌复制） | 无效果可跑 |
| GOF8 | Hornhelmz' Boy | ✅能执行 | — | — | — | 同上；`desc` 为空（数据侧，见 §五④） | 无效果可跑 |
| GOF21 | Mekboy Gazmek | ✅能执行 | 46,300 | 50 | 2 costChange | T46+T300；`lowercost` 1142；`Talent: Mekaniak` → SpawnTalents 13722 | 已 对 ca100；ca30 闸 K |
| GOF55 | Ork Boy | ✅能执行 | — | — | — | 只有 `tide` | 无效果可跑 |
| GOF4 | Slugga Boy | ✅能执行 | 0 | 20 | 1 melee | `for each other friendly Slugga Boy` → multiplier（`CountEval` 按 `filterSpecificCards`） | 闸 I |
| GOF6 | Stormboy | ✅能执行 | — | — | — | 只有 `tide`+`flying` | 无效果可跑 |
| GOF20 | Veteran Stormboy | ✅能执行 | 190 | 80 | 3 flank | T190 7640 + `FireBoardWhenListener` 10397；`give` 1122 | 闸 C |
| GOF31 | Cloud of Smoke | ✅能执行 | 0,410 | 10,20 | 3 addTrait(blind) | `blind` 1094 `DoBlind` 4510；`Repeat this effect for each …` → BF `repeat` 1143 | 已 对 ca30/320；ca260 闸 A |
| GOF37 | Da Green Horde | ✅能执行 | 0 | — | 1+2 | handBuff（`lowercost` 1142） | 闸 O（只给某一类牌减费）；见 §五③ |
| 20d0f4… | Da Red Waaagh | ✅能执行 | 0 | 65,150 | 1 melee | `drawtype`(troop)+`give` 1122；TA 150 1469 | 已 对 ca0；ca30 闸 E+L |
| GOF53 | Extractor Rig | ✅能执行 | 0 | — | 2 costChange | handBuff + `The next … costs N less` `EffectText.cs:447` | 闸 O+L；见 §五② |
| 3ebb15… | Greatest Warboss | ✅能执行 | 0,410 | 10,20 | 1 melee+ranged | `deploy` 1141 + `summonAllTargets` `CardDef.cs:9151` + `give` 1122 | 已 对 ca70/320；ca30 闸 A |
| 9dcbe3… | Mekaniak | ✅能执行 | 0 | 180 | 1 melee | `Give a Kustom Job of your choice` `EffectText.cs:6480-6507`（全池唯一 1 处，已实现） | **闸 M `rc=18`**，见 §五① |
| 9f4e1b… | More Dakka | ✅能执行 | 0 | 30 | 1 ranged+3 flank | `give` 1122 + `3 flank` | **闸 M `rc=18`**，见 §五① |
| 98d3c6… | Ramshackle | ✅能执行 | 0 | 30 | 1 health | `give`（healthChange） | **闸 M `rc=18`**，见 §五① |
| b4d6d1… | Sawbonez | ✅能执行 | 0 | 10,30 | 1 melee | `deal` 1064 + `If the target survives, heal 1-5 to it`（`EffectText.cs:7561` 明列 Sawbonez） | 已 对 ca10；ca20 闸 F |
| GOF39 | Tide of Muscle | ✅能执行 | 0 | 65,150 | 1 melee | `drawtype`×2 + `give` | 已 对 ca0；ca30 闸 E |
| 508002… | Wreckin' Ball | ✅能执行 | 0 | 30 | 1 melee+3 concussive | `give` 1122（`concussive` GivePayload.cs:164） | **闸 M `rc=18`**，见 §五① |
| GOF24 | Battlewagon | ✅能执行 | 81,300 | 10,50 | 2+3 | T81 10195；T300；`lowercost` 1142（`manaCostFilter`） | 已 对 ca10；ca30 闸 K |
| GOF25 | Gorkanaut | ✅能执行 | 81 | 10 | 3 addTrait | T81 10195/13625；`Armour` 减伤 | 闸 C |
| GOF69 | Mega-Blasta Deffkopta | ✅能执行 | 0,81 | 10 | 3 addTrait | T81；`Create a Grot Bomb in your hand` → `create` 1140 | 已 对 ca100；ca10 闸 K |

> 全表 `chosenAbility` 值（0/3/10/20/30/40/50/60/70/100/140/235/250/260/270/300/320/325/360/380/420/461）
> 逐个对过 `AbilityEffect.cs`：`3=doOnlyVfx`、`235=scriptedSelfAttack`、`250=autoPlayFromHand`、`360=chooseOne`、
> `420=setCardDeckToTop`、`461=triggerMobAdditional` —— **没有一个是「原版有、我们没有」的效果种类**；
> 元 op（320/325/360/480）走 `CardAbilityDispatch_B116.cs:105-165` 的 B1..B8 判定，**正文层已覆盖的不双发**
> （B7 双保险 `CardAbilityDispatch_B116.cs:231`）。

## 三、真缺口清单（🟠）

**本组 0 条。** 逐张核过三个面（trigger 接收集 / `ta` 档 / `buffType` 执行器），**没有任何一张落到「无分支 / 落 default / 静默跳过」**。

⚠️ 边界（如实标）：本报告只回答「**引擎有没有这条分支**」，**没有**回答「分支的语义与原版逐值一致」——
后者需要 `ruleprobe play` 或逐族隔离夹具（本组按红线未跑）。

## 四、查不到的（❓）

无。所有 `ta` / `buffType` / `ca` 值都在代码里找到了明确落点。

## 五、顺手发现

1. 🔴 **M 族『出牌失败』的根因推定**（S3 表把它列「根因未查清」，`S3未验到_分族与夹具.md` §一 M 行）：
   5 张 —— `7c45…` Master of Manoeuvre · `5080…` Wreckin' Ball · `98d3…` Ramshackle · `9dcb…` Mekaniak · `9f4e…` More Dakka，
   **共同点 = 卡面都要点「一个友方 Vehicle」**（`Return a friendly Vehicle` / `Give … to a friendly Vehicle`）。
   `rc=18` = `RuleCodes.ErrNoTargetAvailable`（`Core/RuleCodes.cs:52`：战术卡一个合法目标都没有）⇒
   **推定 = 电池的场/手牌里没有 Vehicle**，不是引擎缺陷。**未实证**（没跑夹具）；验证办法 = 电池预置一辆友方 Vehicle 再跑这 5 张。
2. `GOF53 Extractor Rig` 的数据面与卡面不一致：`abilitySummon` 是 `unitsToSummon=["GOF1","GOF1"], summonAllTargets=1`
   （造 2 个 Grot），而卡面只有 `Your next Vehicle costs 2 less`。我们跑文本层（不会造 Grot）⇒ **行为跟随卡面**，但数据侧值得查一次。
3. `GOF37 Da Green Horde` 同样是数据/卡面不一致：卡面只有 `Your troops cost 1 less this turn`，
   而 `abilityBuffs` 里还有一条 `rangeAttackChange: 2`。文本层只跑卡面那一句。
4. `GOF8 Hornhelmz' Boy` 的 `desc` 与 `descZh` **都是空串**，且没有任何 ability（只有 `tide 910:2`）。
   引擎侧「无效果可跑」是正常的；但**卡面文本为空**这件事属数据侧（`cardface_fixes.json` 那条链），不在本报告范围。
5. `DA46 The Rock` 的 `ca 360`（`chooseOne`）**卡级派发层是「出声不跑」**（`CardAbilityDispatch_B116.cs:315` 的 `NoteCardAbilityRowPending`），
   实际效果由**正文层** `Choose one:` → `chooseone` op 承担。⇒ 这条不是缺口，但**将来若有人「补」了 360 的卡级执行器，会变成双发**，
   建议在那一处留一条注释（本次未改代码）。
