# 报告 · A 档跨账审计（落点是否方法级）

> 代理 G · 2026-10-25 · **只读审计，未改任何一份别人的 TSV**
> 起因：调度台裁定「**A 不是「这个类我们做了」，是「这个方法我们逐条对过」**」。
> 我先按这条把自己的六份账收紧完（A 969 → 247），现在用**同一套三步判法**审其它账。

## 〇 判法

1. 落点必须是**方法声明行**，或**数据表项行**（`new Entry(` / 数组初值 / `{ "key" }`）。
2. 不是 ⇒ 在同一落点文件里按方法名（或备注里点的名字）找声明行；找到 ⇒ 「文件级(可补行号)」。
3. 仍没有 ⇒ 记问题（注释 / 函数中间 / 文件级 / 间接指认 / 文件不存在 / 行号越界）。
4. **不用**「全仓唯一同名」兜底（实测把 `OnDestroy` 认到 `BattleMusic.cs:262`、`CleanUp` 认到 `BadgesNotifications.cs` 这种别的类上）。
5. **只判「落点是否方法级」，不判行为对不对**（那是 owner 的事）。
6. 「**＋被多方法共用**」= 同一类里 ≥3 个方法指向**同一个** `文件:行` —— 那行最多只可能是其中一个的实现。
7. 文件名只给 basename 的（`SimpleAI.cs:1237`）按全工程唯一 basename 解析；解析不出记「文件不存在」——**这一类可能是我的解析问题，owner 一核就知道**。

⚠️ `数据表项(需确认是否纯查表)`：调度台口径 = **纯查表算 A**；若原版该方法还有分支/兜底/按阵营分叉 ⇒ 光指表不够，仍是 B。

> **审计时点**：2026-10-25 晚（快照）。期间有 owner 在改自己的账（`战斗逻辑驱动` A 146→151、`战斗界面_HUD` A 422→423），
> 所以**行数以账为准**，本表只保证「那一刻的落点质量」。
>
> **问题类型与你给的分类的对应**：`注释` · `函数中间` · `被多方法共用` · `文件级` · `间接指认` 是你点名的五类；
> 我额外细分两处让 owner 能直接动手：`文件级(可补行号)`（同一文件里有同名声明 ⇒ 补 `:行` 即可）、
> `仅类.方法(可补行号)`（落点写了 `Class.Method` 但没给文件行 ⇒ 已算出该方法的声明行）。

## 一 总览（按调度台给的优先顺序）

| # | 账 | A 总数 | 方法级 OK | 有问题 | 注释 | 函数中间 | 指向别的方法/字段 | 文件级(可补行号) | 文件级 | 间接指认 | 数据表项(需确认是否纯查表) | 被多方法共用 | 整类行 | 行号越界 | 文件不存在 | 仅类.方法(可补行号) | 仅类.方法(解析不到) | 仅类/描述(无方法) |
|---:|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | `战斗逻辑驱动.tsv` | 151 | 20 | **131** | 10 | 12 | 0 | 4 | 1 | 2 | 0 | 5 | 0 | 0 | 0 | 71 | 8 | 18 |
| 2 | `特效动画.tsv` | 95 | 56 | **39** | 7 | 11 | 21 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 3 | `菜单_外壳.tsv` | 344 | 89 | **255** | 25 | 13 | 19 | 73 | 80 | 0 | 0 | 5 | 0 | 0 | 0 | 22 | 7 | 11 |
| 4 | `战斗界面_HUD.tsv` | 423 | 81 | **342** | 98 | 54 | 37 | 31 | 50 | 8 | 1 | 5 | 0 | 5 | 0 | 21 | 1 | 31 |
| 5 | `联机.tsv` | 9 | 2 | **7** | 2 | 4 | 1 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 | 0 |
| 6 | `卡牌面_渲染.tsv` | 108 | 0 | **108** | 0 | 0 | 0 | 5 | 7 | 1 | 0 | 0 | 0 | 0 | 0 | 37 | 12 | 46 |
| 7 | `卡组编辑.tsv` | 177 | 0 | **177** | 0 | 0 | 0 | 19 | 43 | 0 | 0 | 0 | 0 | 0 | 0 | 94 | 7 | 14 |
| 8 | `收藏.tsv` | 141 | 0 | **141** | 1 | 0 | 0 | 49 | 3 | 0 | 0 | 0 | 0 | 0 | 0 | 81 | 1 | 6 |
| 9 | `商店.tsv` | 114 | 1 | **113** | 0 | 0 | 0 | 29 | 80 | 0 | 0 | 0 | 0 | 0 | 0 | 1 | 2 | 1 |
| 10 | `轮抽.tsv` | 99 | 0 | **99** | 0 | 0 | 0 | 2 | 1 | 11 | 0 | 0 | 0 | 0 | 0 | 19 | 1 | 65 |
| 11 | `战役.tsv` | 68 | 0 | **68** | 0 | 0 | 0 | 1 | 3 | 5 | 0 | 0 | 0 | 0 | 0 | 33 | 0 | 26 |
| 12 | `锻造.tsv` | 55 | 27 | **28** | 0 | 8 | 5 | 4 | 4 | 0 | 0 | 0 | 0 | 0 | 0 | 5 | 0 | 2 |
| — | **合计** | **1784** | **276** | **1508** | 143 | 102 | 83 | 217 | 272 | 27 | 1 | 15 | 0 | 5 | 0 | 384 | 39 | 220 |

> 只审有 A 的账（`音频_语音` / `本地化` A = 0）。我自己那六份账已收紧过（A 969 → 247），不在本表。

### 怎么用这份清单（按修复成本分三档）

| 档 | 类型 | 条数 | 要做的事 |
|---|---|---:|---|
| 🟢 一行就能改 | `文件级(可补行号)` + `仅类.方法(可补行号)` | **601** | 按「建议」列补 `:行`（声明行已经算好了） |
| 🟡 要重新指一次 | `注释` + `函数中间` + `指向别的方法/字段` + `被多方法共用` | **343** | 打开文件找**本方法**的声明行；找不到 ⇒ 退 B |
| 🔴 等于没落点 | `文件级` + `仅类/描述(无方法)` + `仅类.方法(解析不到)` + `间接指认` | **558** | 补真实落点，或退 B（这批是 A 里最空的） |
| — | `数据表项(需确认是否纯查表)` + `行号越界` | 6 | 纯查表保留 A / 有分支补分支落点 / 越界重指 |

🔴 **最该先看的三个账**：`战斗界面_HUD`（A 423 / 问题 342）· `菜单_外壳`（344 / 255）· `战斗逻辑驱动`（151 / 131 —— 它的 A 只占全账 10%，但落点几乎全是 `Class.Method` 形式，缺的往往只是行号）。
✅ **最干净的一个**：`特效动画`（A 95 / 问题 39，其中 56 条真方法级）—— 它的写法可以当别的账的模板。

## 战斗逻辑驱动.tsv（A 151，问题 131）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `BattleManager.BattleFinished()` | `BattleDriver.ShowEndPanelAndFreezeInput + Ctx.Winner（+ 对局记录那一条 `_rec.r…` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:30754` |
| `BattleManager.CanAttackCard(CardScript card, bool allowAutoActions)` | `RuleCore.CanAttackNow / AttackBannedByTraits` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5499` |
| `BattleManager.CanUseWaystone(CardScript parentCard, bool isPlayerAsking, bool showTi…` | `RuleCore.CanCollectWaystone` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:11838` |
| `BattleManager.CardsPlayedInTurn()` | `BattleContext.CardsPlayedInTurn` | 仅类.方法(可补行号) | `RuleEngine/Core/BattleContext.cs:1031` |
| `BattleManager.CheckIfAttackKillsTarget(CardScript attacker, CardScript target)` | `RuleCore.AttackKillsTarget` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5429` |
| `BattleManager.DeadHero(bool isPlayerDying, BattleResult battleResult)` | `RuleCore.CheckWinner（含双方同时倒下 ⇒ Draw 那一道门）+ BattleDriver 的收局` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2630` |
| `BattleManager.DestroyUnit(CardScript actingCard, CardScript targetCard, int priority…` | `EffectResolver.DoDestroy / RuleCore.CleanupDeaths（同步结算）` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:3943` |
| `BattleManager.DestroyUnitSilent(CardScript destroyUnit)` | `RuleCore.CleanupDeaths（移除 + 让位）+ SweepHandEffectsOnRemoval（双方手牌那一趟）+ A…` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:9065` |
| `BattleManager.DrawCardFromDeck(bool isPlayerDeck, CardScript targetCard)` | `RuleCore.DrawCore / MoveDeckTopToHand` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2835` |
| `BattleManager.GetUnusedMana(bool isPlayer)` | `PlayerState.Energy` | 仅类.方法(可补行号) | `RuleEngine/Core/PlayerState.cs:74` |
| `BattleManager.GetWinnerAfterBattleEnd(bool isPlayerDying, BattleResult battleResult)` | `RuleCore.CheckWinner` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2630` |
| `BattleManager.IsDuringMulligan()` | `BattleContext.MulliganOpen` | 仅类.方法(可补行号) | `RuleEngine/Core/BattleContext.cs:987` |
| `BattleManager.IsPlayerTurn()` | `BattleContext.Active` | 仅类.方法(可补行号) | `RuleEngine/Core/BattleContext.cs:336` |
| `BattleManager.IsThereValidUnitTarget(CardScript targetedUnitBeingPlayed)` | `RuleCore.PlayTargetPool` | 仅类.方法(解析不到) | — |
| `BattleManager.IsValidAttackTarget(CardScript attacker, CardScript target, bool showT…` | `RuleCore.IsValidTarget (RuleCore.cs:5162，2026-10-25 R1 已按原版补 IsDefende…` | 注释 | `RuleEngine/Core/RuleCore.cs:5185` |
| `BattleManager.MaxCardsInHand()` | `GameplayVariables.handLimit` | 仅类.方法(可补行号) | `RuleEngine/Core/GameplayVariables.cs:286` |
| `BattleManager.NeedToChooseMinionTarget(CardScript actingCard)` | `BattleDriver.NeedsUnitTargetAfterDeploy（卡属性那一半）+ TryBeginPlayTargetPic…` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:7657` |
| `BattleManager.NextTurn(bool playerTurn)` | `RuleCore.BeginTurn → BeginTurnCore（RuleCore.cs:1600-2060）` | 注释 | `RuleEngine/Core/RuleCore.cs:1589` |
| `BattleManager.ResolveDrawCard(BattleAction battleAction)` | `RuleCore.Draw` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2812` |
| `BattleManager.ResolveEndTurnEnchantment(bool isPlayer)` | `RuleCore.ResolveEndTurnEnchantment` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2670` |
| `BattleManager.ResolveExecuteSwarm(BattleAction battleAction)` | `RuleCore.SwarmMergeNow` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:4417` |
| `BattleManager.ResolveSummonUnit(BattleAction battleAction)` | `RuleCore.DeployFree` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:4574` |
| `BattleManager.ResolveTriggerPray(BattleAction battleAction)` | `BroadcastWhen(Prays)/TriggersPrefix` | 仅类/描述(无方法) | — |
| `BattleManager.ResolveTriggerSlay(BattleAction battleAction)` | `RuleCore.FireTriggerAt(Slay) (RuleCore.cs:6003)` | 函数中间 | `RuleEngine/Core/RuleCore.cs:11996` |
| `BattleManager.ResolveTriggerSpiritStone(BattleAction battleAction)` | `BroadcastWhen(SpiritAbility)` | 仅类/描述(无方法) | — |
| `BattleManager.ResolveTriggerSynapse(BattleAction battleAction)` | `TriggerKeywordOfKind → Synapse` | 仅类/描述(无方法) | — |
| `BattleManager.ResolveUseWaystone(BattleAction battleAction)` | `RuleCore.CollectWaystone` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:11860` |
| `BattleManager.TurnStartCardDraw(bool playerTurn)` | `RuleCore.BeginTurnCore 的抽牌段（含 Sentinel / skipCardDraw 闸）` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:1623` |
| `BattleManagerSupport.BroadcastUnitDuty(BattleManager manager, CardScript unitDuty)` | `EffectResolver.TriggerKeywordOfKind → TN_TriggeredDuty + TriggersPrefi…` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:7095` |
| `BattleManagerSupport.BroadcastUnitFerocity(BattleManager manager, CardScript unitFerocity)` | `EffectResolver.TriggerKeywordOfKind → TN_TriggeredFerocity` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:7093` |
| `BattleManagerSupport.BroadcastUnitLoseStealth(BattleManager manager, CardScript unitLoseSte…` | `BroadcastKeywordEvent(WhenEventKind.LosesStealth)（RuleCore.cs:2036 / :…` | 函数中间 | `RuleEngine/Core/RuleCore.cs:1909(所在方法)` |
| `BattleManagerSupport.BroadcastUnitObtainedMarkerlight(BattleManager manager, CardScript uni…` | `BroadcastKeywordEvent(WhenEventKind.GetsMarkerlight) (EffectResolver.c…` | 注释 | `RuleEngine/Core/EffectResolver.cs:10529` |
| `BattleManagerSupport.BroadcastUnitPray(BattleManager manager, CardScript unitPray)` | `BroadcastWhen(WhenEventKind.Prays) 2 处 + TriggerKeywordOfKind→TN_Trigg…` | 仅类.方法(解析不到) | — |
| `BattleManagerSupport.BroadcastUnitSpiritStone(BattleManager manager, CardScript unitsp)` | `BroadcastWhen(WhenEventKind.SpiritAbility) (EffectResolver.cs:961)` | 函数中间 | `RuleEngine/Core/EffectResolver.cs:9447` |
| `BattleManagerSupport.BroadcastUnitStunned(BattleManager manager, CardScript stunnedUnit, Ca…` | `BroadcastKeywordEvent(WhenEventKind.GetsStun)（EffectResolver.cs:4055 /…` | 注释 | `RuleEngine/Core/EffectResolver.cs:10529` |
| `BattleManagerSupport.BroadcastUnitSwarm(BattleManager manager, CardScript playedUnit, CardS…` | `RuleCore.SwarmMergeNow → FireTriggerAlways(Swarm) + TriggersPrefix` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:4417` |
| `BattleManagerSupport.BroadcastUnitSynapse(BattleManager manager, CardScript synapseUnit)` | `EffectResolver.TriggerKeywordOfKind → TN_TriggeredSynapse` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:7107` |
| `BattleManagerSupport.BroadcastWhileInPlay(BattleManager manager, bool unitJustSummoned, Car…` | `RuleCore.EmitWhileInPlayPass（EffectResolver.cs:7597，由 ActionPump.cs:10…` | 注释 | `RuleEngine/Core/EffectResolver.cs:7736` |
| `CardScript.ActivateTraitsOnSummonOrEnchantment(bool onSummon, bool playSound)` | `RuleCore.ActivateTraitsOnSummonOrEnchantment` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:3442` |
| `CardScript.AddArmour(int armourToAdd)` | `UnitState.SyncKeywordState（加侧）+ UnitState.RemoveKeyword/RemoveAll（减侧，`…` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:1007` |
| `CardScript.AddTraitSilently(DefinedTrait traitToAdd, bool hideTrait, int defaultI…` | `UnitState.AddKeyword / Has（`_keywords` 字典：同名键天然只有一条）` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:954` |
| `CardScript.AlreadyContainsEffect(CardEffect effect, bool heroCheck)` | `EffectResolver.IsSameHandEffect（2026-10-25 逐字段实现）+ AttachHandEffectCop…` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:5466` |
| `CardScript.CanActNow()` | `RuleCore.CanActNow` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5586` |
| `CardScript.CanAttackNow()` | `RuleCore.CanAttackNow` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5499` |
| `CardScript.CanUseAbilityNow()` | `RuleCore.CanUseAbility` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:10659` |
| `CardScript.CanUseOathAbility()` | `RuleCore.CanUseOathAbility` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:11216` |
| `CardScript.CanUseSpiritStone()` | `RuleCore.CanUseFaithAbility/付费段` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:11337` |
| `CardScript.ChangeAttackType(AttackTypes desiredAttackType, bool playSound)` | `RuleCore.SetCurrentAttackType` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5626` |
| `CardScript.ChooseAttackTypeAutomatically()` | `RuleCore.ChooseAttackTypeAutomatically` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5675` |
| `CardScript.ContainsTrait(DefinedTrait trait)` | `UnitState.Has（`_keywords` 字典）` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:181` |
| `CardScript.DrawCard(Vector3 pos, Vector3 newScale, float timeToDraw)` | `RuleCore.Draw` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2812` |
| `CardScript.EnoughPendingDamageToDie()` | `RuleCore.EnoughPendingDamageToDie (RuleCore.cs:7297)` | 函数中间 | `RuleEngine/Core/RuleCore.cs:7346` |
| `CardScript.ExecuteSwarm(CardScript buffedUnit, Action swarmFinished)` | `RuleCore.SwarmMergeNow (RuleCore.cs:4394)` | 函数中间 | `RuleEngine/Core/RuleCore.cs:4417` |
| `CardScript.GoToCemetery()` | `RuleCore.MinionDeathBookkeepingDeferred（弃牌堆 + 阵亡登记）` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:9725` |
| `CardScript.IsCurrentTurn()` | `BattleContext.Active` | 仅类.方法(可补行号) | `RuleEngine/Core/BattleContext.cs:336` |
| `CardScript.IsDefender(CardScript attacker, AttackTypes attType)` | `RuleCore.IsValidTarget 的 IsDefenderUnit（2026-10-25 按原版 `CardScript.IsD…` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5185` |
| `CardScript.IsInPlay()` | `RuleCore.IsUnitInPlay / IsOnBoardOrDying` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:10302` |
| `CardScript.IsInPlayOrDying()` | `RuleCore.IsUnitInPlayOrDying` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:10326` |
| `CardScript.IsOnBoard()` | `RuleCore.BoardOccupantByInstance` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5108` |
| `CardScript.IsUnit()` | `CardDef.IsUnit` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:5952` |
| `CardScript.ProcessCompanion()` | `RuleCore.PlayCompanions` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:4191` |
| `CardScript.ProcessTide()` | `RuleCore.SpawnTideCopies` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:4112` |
| `CardScript.ReactToCollectFaith(CardScript unitCollected)` | `BroadcastWhen(GainFaith)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToCollectQuestPoints(CardScript unitCollected)` | `BroadcastWhen(GainQuest)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToCollectSpiritStone(CardScript unitCollected)` | `BroadcastWhen(GainSpirit)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToSabotageCreated(CardScript unitCollected)` | `BroadcastWhen(CreatesSabotage)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToSecretCreated(CardScript unitCollected)` | `BroadcastWhen(CreatesSecret)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToUnitHuntMarked(CardScript unitMarked)` | `BroadcastWhen(GetsHuntMark)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToUnitMarkerlight(CardScript unitMarkerlight)` | `BroadcastWhen(GetsMarkerlight)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToUnitReanimated(CardScript unitReanimated)` | `BroadcastWhen(Reanimated)` | 仅类/描述(无方法) | — |
| `CardScript.ReactToUnitStunned(CardScript stunnedUnit, CardScript actingUnit)` | `BroadcastWhen(GetsStun)` | 仅类/描述(无方法) | — |
| `CardScript.RemoveDropPod(bool isDestroyed)` | `RuleCore.OpenDropPod (RuleCore.cs:7472)` | 函数中间 | `RuleEngine/Core/RuleCore.cs:7521` |
| `CardScript.RemoveTrait(DefinedTrait newTrait)` | `UnitState.RemoveAll（+ 本轮补的 `RecomputeActAndAttack` 那一跳）` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:1042` |
| `CardScript.RemoveTraitAndEffects(DefinedTrait traitToRemove)` | `UnitState.RemoveAll（关键词与授予正文一起撤：`_grantedOps`/`_grantedText` 那本账）` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:1042` |
| `CardScript.RemoveTraitSilently(DefinedTrait traitToRemove)` | `UnitState.RemoveAll（`SyncKeywordState` 之外不做表现同步；图/框由 `Badges`/`TraitFr…` | 仅类.方法(可补行号) | `RuleEngine/Core/UnitState.cs:1042` |
| `CardScript.ResetSummonSickness()` | `RuleCore.ResetSummonSickness` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:3608` |
| `CardScript.SendToCemetery()` | `RuleCore 的死亡收尾（前两条 = 表现层，第三跳 = 上面那一行）` | 仅类/描述(无方法) | — |
| `CardScript.SetHealthToZero()` | `RuleCore.Hurt / CleanupDeaths 的置零路径` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:8568` |
| `CardScript.TriggerSlay()` | `RuleCore.cs:6003 FireTriggerAt(Slay)` | 函数中间 | `RuleEngine/Core/RuleCore.cs:11996` |
| `CardScript.TriggerSpentAllMana(CardScript actingCard)` | `RuleCore.FireSpentAllMana` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:12566` |
| `CardScript.TriggeredDuty(CardScript dutyUnit)` | `EffectResolver.TriggerKeywordOfKind → TN_TriggeredDuty` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:7095` |
| `CardScript.TriggeredFerocity(CardScript ferocityUnit)` | `EffectResolver.TriggerKeywordOfKind → TN_TriggeredFerocity` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:7093` |
| `CardScript.TriggeredPray(CardScript prayUnit)` | `…→ TN_TriggeredPray` | 仅类/描述(无方法) | — |
| `CardScript.TriggeredSwarm(CardScript playedUnit, CardScript buffedUnit)` | `…→ TN_TriggeredSwarm` | 仅类/描述(无方法) | — |
| `CardScript.TriggeredSynapse(CardScript synapseUnit)` | `…→ TN_TriggeredSynapse` | 仅类/描述(无方法) | — |
| `CardScript.UpdateSummonSicknessAnims()` | `RuleCore.RecomputeActAndAttack（原版 `UpdateSummonSicknessAnims.c:19-21` …` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:3377` |
| `CardScript.UpdateWhileInPlay(bool unitJustSummoned)` | `RuleCore.EmitWhileInPlayPass（信号那一半）+ Aura.Recompose（数值那一半）+ 事件层 WhileI…` | 仅类.方法(可补行号) | `RuleEngine/Core/Aura.cs:611` |
| `CardTraitCollection.GetTraitFromAbilityTrigger(AbilityTrigger abilityTrigger)` | `EffectResolver.TriggerKeywordOfKind（2026-10-25 R2 补齐 Mob/Codex/Regimen…` | 仅类.方法(解析不到) | — |
| `EntityScript.ActivatesNoReturnLongRangeAttack(CardScript attackedCard, bool isAttac…` | `CounterAttack.NoCounter ③ (CounterAttack.cs:60)` | 函数中间 | `RuleEngine/Core/CounterAttack.cs:43` |
| `EntityScript.ActivatesNoReturnSniperAttack(CardScript attackedCard, bool isAttacker…` | `CounterAttack.NoCounter ④ (CounterAttack.cs:64)` | 函数中间 | `RuleEngine/Core/CounterAttack.cs:43` |
| `EntityScript.ActivatesTerror(CardScript targetCard)` | `CounterAttack.NoCounter ② (CounterAttack.cs:53)` | 函数中间 | `RuleEngine/Core/CounterAttack.cs:43` |
| `EntityScript.HasVanguardTrait(CardScript attacker, AttackTypes attType)` | `RuleCore.IsValidTarget 的 IsDefenderUnit（2026-10-25 R1 已按此改）` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:5185` |
| `EntityScript.IsShowingTrait(DefinedTrait trait)` | `Core/Badges.cs（徽标）+ Core/TraitFrames.cs（状态框）—— 表现层；引擎侧无该字段、无需有` | 文件级 | — |
| `MinionManager.AvailableLeftSlot()` | `BoardSlots.RoomOnSide (BoardSlots.cs:87)` | 被多方法共用 | — |
| `MinionManager.AvailableRightSlot()` | `BoardSlots.RoomOnSide (BoardSlots.cs:87)` | 被多方法共用 | — |
| `MinionManager.AvailableSlotsCount()` | `BoardSlots.RoomOnSide 两侧相加（BoardSlots.cs:87）` | 被多方法共用 | — |
| `MinionManager.GetAdjacentRightTroop(CardScript refMinion)` | `BoardSpec.AdjacentSlots + Aura.cs:842` | 函数中间 | `RuleEngine/Core/Aura.cs:841(所在方法)` |
| `MinionManager.GetAdjacentUnits(CardScript refMinion)` | `BoardSpec.AdjacentSlots` | 仅类.方法(可补行号) | `RuleEngine/Core/BoardSpec.cs:61` |
| `MinionManager.GetUnitList()` | `PlayerState.Units()（格号序）/ UnitsByPlayOrder()（进场序）—— 按调用点各取所需` | 仅类.方法(可补行号) | `RuleEngine/Core/PlayerState.cs:172` |
| `MinionManager.IsAvailableSlotLeft(int numberOfSlotsNeeded)` | `BoardSlots.RoomOnSide (BoardSlots.cs:87)` | 被多方法共用 | — |
| `MinionManager.IsAvailableSlotRight(int numberOfSlotsNeeded)` | `BoardSlots.RoomOnSide (BoardSlots.cs:87)` | 被多方法共用 | — |
| `MinionManager.IsThereDefender(CardScript attacker, AttackTypes attType)` | `RuleCore.IsValidTarget 的局部谓词 `IsDefenderUnit`（RuleCore.cs，2026-10-25 R…` | 文件级(可补行号) | `RuleEngine/Core/RuleCore.cs:5185` |
| `MinionManager.MoveMinionIntoSlot(CardScript card, int pos, bool skipMovingMinion, bo…` | `BoardSlots.Insert + BattleDriver 让位` | 仅类.方法(可补行号) | `RuleEngine/Core/BoardSlots.cs:307` |
| `MinionManager.SummonMinion(CardScript card, int summonSlot, float timeToMove, bool d…` | `RuleCore.DeployFree (RuleCore.cs:4551) + BoardSlots.Insert` | 注释 | `RuleEngine/Core/RuleCore.cs:4574` |
| `PlayerHand.AddDrawnCardToHand(CardScript card)` | `RuleCore.DrawCore → AddCardToHandWithinLimit` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:2835` |
| `PlayerHand.CanDrawTurnStartCard(bool removeEffectAfter)` | `RuleCore.HasSentinelInPlay + ConsumeSkipCardDrawHandEffect (RuleCore.c…` | 文件级(可补行号)＋被多方法共用(3) | `RuleEngine/Core/RuleCore.cs:1263` |
| `PlayerHand.GetBaseCardsInHandCount(bool isPlayer)` | `CardInstance.cs:86 的起手公式 + GameplayVariables.startingHand` | 注释＋被多方法共用(3) | — |
| `PlayerHand.GetHeroExtraCardsCount(RawCardScript rawHeroCard)` | `CardDef.InitialHandModifier + CardInstance.cs:86` | 注释＋被多方法共用(3) | `RuleEngine/Core/CardInstance.cs:395` |
| `PlayerHand.GetSecondExtraCardsCount(bool goesFirst)` | `GameplayVariables.SecondExtraCards（经典 0 / 遭遇 …）+ CardInstance.cs:86 的 …` | 注释＋被多方法共用(3) | — |
| `PlayerHand.IsThereSentinel()` | `RuleCore.HasSentinelInPlay (RuleCore.cs)` | 文件级(可补行号)＋被多方法共用(3) | `RuleEngine/Core/RuleCore.cs:1263` |
| `PlayerHand.UpdateCardEffects(bool endOfTurn)` | `RuleCore.HandEffectExpired + ExpireHandBuffs + SweepHandEffectsOnRemov…` | 文件级(可补行号)＋被多方法共用(3) | `RuleEngine/Core/RuleCore.cs:10242` |
| `RawCardScript.ContainsRelatedCards()` | `CardDef.RelatedIds / RelatedCardMap` | 仅类.方法(解析不到) | — |
| `RawCardScript.GetAbilitiesForTrigger(CardScript thisCard, AbilityTrigger trigger, bo…` | `CardDef.abilityTriggers / GetAbilitiesForTrigger（我们的 ability 表）` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:875` |
| `RawCardScript.GetRelatedCards()` | `RelatedCardMap / CardDef.RelatedIds` | 仅类.方法(解析不到) | — |
| `RawCardScript.HasAbilityType(AbilityTrigger trigger)` | `CardDef.AbilityTriggers（v17 列）` | 仅类.方法(解析不到) | — |
| `RawCardScript.HasDefaultTrait(DefinedTrait trait)` | `CardDef.Keywords（卡面那一份）` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:5998` |
| `RawCardScript.HasPlayedThirdCardInTurn()` | `RuleCore 的 PlayedThirdCardInTurn + CardDef 的相应 trigger` | 仅类.方法(可补行号) | `RuleEngine/Core/RuleCore.cs:149` |
| `RawCardScript.IsEnviromentaleffect()` | `CardDef.Type == 环境效果 / OffensiveCardMap` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:84` |
| `RawCardScript.IsTargetedCard()` | `CardDef.Targeted（v19 列 `targeted`）` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:324` |
| `RawCardScript.IsUnit()` | `CardDef.IsUnit` | 仅类.方法(可补行号) | `RuleEngine/Core/CardDef.cs:5952` |
| `RawCardScript.OnMinionDeath(BattleManager manager, CardScript thisCard, CardScript a…` | `BroadcastWhen(WhenEventKind.Die)` | 仅类.方法(解析不到) | — |
| `RawCardScript.OnTacticPlayed(BattleManager manager, CardScript thisCard, CardScript …` | `WhenEvent.Play 的卡类型筛` | 仅类.方法(可补行号) | `RuleEngine/Core/WhenEvent.cs:133` |
| `RawCardScript.OnTrigger(AbilityTrigger trigger, BattleManager manager, CardScript th…` | `EffectResolver.TriggerKeywordOfKind + BroadcastWhen 分发` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:9447` |
| `RawCardScript.OnTrigger(AbilityTrigger trigger, BattleManager manager, CardScript th…` | `同上` | 间接指认 | — |
| `RawCardScript.OnTrigger(AbilityTrigger trigger, BattleManager manager, CardScript th…` | `同上` | 间接指认 | — |
| `RawCardScript.UpdateWhileInPlay(BattleManager manager, CardScript thisCard, CardScri…` | `同上（RuleCore.EmitWhileInPlayPass + WhenEvent.WhileInPlay）` | 仅类.方法(解析不到) | — |
| `RawCardScript.UsesFaith()` | `EffectResolver 付费段（faith）` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:11880` |
| `RawCardScript.UsesSpiritStone()` | `EffectResolver 付费段（spirit）` | 仅类.方法(可补行号) | `RuleEngine/Core/EffectResolver.cs:8022` |
| `SupportMethods.HasToTransformIntoRemnant(CardScript cardScript)` | `RuleCore.CleanupDeaths（`u.Has(Remnant) \|\| u.Has(Waystone)`，RuleCore.cs…` | 注释 | `RuleEngine/Core/RuleCore.cs:9065` |
| `SupportMethods.IsNecronRemnant(CardScript cardScript)` | `ConditionEval 里的 necron 残骸判据` | 仅类/描述(无方法) | — |
| `SupportMethods.IsSpellType(CardTypeOptions cardType)` | `RuleCodes/CanPlayCard 的「是不是法术」判据` | 仅类/描述(无方法) | — |

## 特效动画.tsv（A 95，问题 39）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `AnimFXController.DoDestroy()` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:480` | 函数中间 | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:609` |
| `AnimFXController.Exit()` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:427` | 注释 | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:556` |
| `AnimFXController.OnEnable()` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:314（Init 定寿命）+ :452（Dest…` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:581` |
| `AnimFXController.PlaySound(AudioCue audioCue)` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:671（PlayOneSound）→ `WFSo…` | 函数中间 | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:225` |
| `AnimFXController.Update()` | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:647（TickSounds）` | 注释 | `WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs:475` |
| `AnimFXInstanceParticleAdjacent.DelayActivation()` | `WarpforgeVFX/Runtime/WFModuleInstanceParticleAdjacent.cs:198 段（`_timer…` | 指向别的方法/字段 | — |
| `AnimFXModuleAnimation.AnimationExitEnd()` | `WarpforgeVFX/Runtime/WFModuleAnimation.cs:204 段 + `ModuleTick`（按 clip.…` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFModuleAnimation.cs:220` |
| `AnimFXModuleCollisions.UpdateTargetCollisionPlanePosition(Transform targetPlane, Transform ac…` | `WarpforgeVFX/Runtime/WFModuleCollisions.cs:594 段（ModuleTick 内）` | 指向别的方法/字段 | — |
| `AnimFXModuleEvent.TryFireEvent(ActionStart actionStart)` | `WarpforgeVFX/Runtime/WFModuleEvent.cs:222（`Fire()`）` | 指向别的方法/字段 | — |
| `AnimFXModulePostProcess.AnimEventSetOriginalLUTNoTransition()` | `WarpforgeVFX/Runtime/WFModulePostProcess.cs:444 + CardPresentation/Bat…` | 函数中间 | `WarpforgeVFX/Runtime/WFModulePostProcess.cs:476` |
| `AnimFXModulePostProcess.DoBloomAim()` | `WarpforgeVFX/Runtime/WFModulePostProcess.cs:337（组请求）+ CardPresentation…` | 注释 | `WarpforgeVFX/Runtime/WFModulePostProcess.cs:367` |
| `AnimFXModuleScaleByTarget.ChangeShapeAngle()` | `WarpforgeVFX/Runtime/WFModuleScaleByTarget.cs:130 段` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFModuleScaleByTarget.cs:186` |
| `AnimFXModuleScreenShake.OnEnable()` | `WarpforgeVFX/Runtime/WFModuleScreenShake.cs:134` | 指向别的方法/字段 | — |
| `AnimFXModuleTransformModifier.ApplyModification(Transform currentTransform)` | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:524` | 函数中间 | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:540` |
| `AnimFXModuleTransformModifier.ApplyTransforms()` | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:516` | 函数中间 | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:532` |
| `AnimFXModuleTransformModifier.Exit()` | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:581` | 函数中间 | `WarpforgeVFX/Runtime/WFModuleTransformModifier.cs:597` |
| `AnimFXModuleTween.OnDisable()` | `WarpforgeVFX/Runtime/WFModuleTween.cs:303（DoDestroy 清 `_fired`）` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFModuleTween.cs:291` |
| `AnimFXParticleCollisionNotifier.Register(AnimFXModuleCollisions.CollisionAndParticles.ParticleCollisio…` | `WarpforgeVFX/Runtime/WFModuleParticleCollisionNotifier.cs:74` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFModuleParticleCollisionNotifier.cs:59` |
| `AnimInfo.GetAnimDuration()` | `Battle/CardAnimClips.cs:329` | 指向别的方法/字段 | — |
| `BlinkGraphic.Start()` | `CardPresentation/Shell/BlinkGraphic.cs:166` | 指向别的方法/字段 | — |
| `BlinkGraphic.Update()` | `CardPresentation/Shell/BlinkGraphic.cs:184` | 指向别的方法/字段 | — |
| `CamerasConversionHelper.ConvertPositionBetweenCameras(Camera cameraOrigin, Camera cameraDestin…` | `WarpforgeVFX/Runtime/WFModuleMoveParticlesToTarget.cs:304（UpdateTarget…` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFModuleMoveParticlesToTarget.cs:283` |
| `Card3DAnimationController.OnDestroy()` | `Battle/Card3DAnimationController.cs:421` | 指向别的方法/字段 | — |
| `Card3DAnimationController.Start()` | `Battle/Card3DAnimationController.cs:162` | 指向别的方法/字段 | — |
| `DelayTween.GetTween()` | `Core/UnitTweenTable.cs:233` | 函数中间 | `CardPresentation/Core/UnitTweenTable.cs:221(所在方法)` |
| `EnviromentalEffectCardsSO.GetEnvCardList(CardArmy useArmy, bool shouldUseOffensive, bool justOwn…` | `Core/OffensiveCards.cs:98（ChoicesOwnedAt）` | 指向别的方法/字段 | — |
| `LUTBlender.DoBlend(float blendValue)` | `CardPresentation/Battle/BattlePostFx.cs:276` | 函数中间 | `CardPresentation/Battle/BattlePostFx.cs:269(所在方法)` |
| `MaterialFlickerEffect.Update()` | `WarpforgeVFX/Runtime/WFMaterialFlicker.cs:82` | 指向别的方法/字段 | `WarpforgeVFX/Runtime/WFMaterialFlicker.cs:79` |
| `ParticleSystemAreaSpawner.OnEnable()` | `CardPresentation/Battle/ParticleSystemAreaSpawner.cs:85` | 指向别的方法/字段 | `CardPresentation/Battle/ParticleSystemAreaSpawner.cs:77` |
| `ResetBodyTween.GetTween()` | `Core/UnitTweenTable.cs:451` | 函数中间 | `CardPresentation/Core/UnitTweenTable.cs:450(所在方法)` |
| `ScaleTween.GetTween()` | `Core/UnitTweenTable.cs:358` | 注释 | `CardPresentation/Core/UnitTweenTable.cs:335(所在方法)` |
| `ScenarioMaterialFader.CacheOriginalMaterials()` | `CardPresentation/Battle/ScenarioMaterialFader.cs:98` | 函数中间 | `CardPresentation/Battle/ScenarioMaterialFader.cs:156` |
| `ScenarioMaterialFader.RestoreOriginalMaterials()` | `CardPresentation/Battle/ScenarioMaterialFader.cs:117` | 注释 | `CardPresentation/Battle/ScenarioMaterialFader.cs:175` |
| `ShakeTween.GetTween()` | `Core/UnitTweenTable.cs:440` | 函数中间 | `CardPresentation/Core/UnitTweenTable.cs:433(所在方法)` |
| `TraitEffect.GetEffect(CardArmy cardArmy)` | `Core/TraitFrames.cs:87` | 注释 | — |
| `TweenInfoAnimationBase.GetTween(CardScript source, CardScript target)` | `Core/UnitTweenTable.cs:239-260` | 注释 | `CardPresentation/Core/UnitTweenTable.cs:221(所在方法)` |
| `TweenInfoBase.GetTween()` | `Core/UnitTweenTable.cs:172（Build）` | 指向别的方法/字段 | — |
| `TweenInfoBase.GetTween(CardScript source, CardScript target)` | `Core/UnitTweenTable.cs:323（Target）` | 指向别的方法/字段 | — |
| `UnitTweenSO.BuildSequence(CardScript source, CardScript target, Action onSequenceS…` | `Core/UnitTweenTable.cs:172 + Core/UnitTweenRuntime.cs:92` | 指向别的方法/字段 | — |

## 菜单_外壳.tsv（A 344，问题 255）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `AlternateArtPanel.ForceSetNonAlternateArtCard(RawCardScript card)` | `Shell/CardDetailPopup.cs BuildAltArt(:848 区段) / AltArtTexture(:1454)` | 指向别的方法/字段＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1519` |
| `AlternateArtPanel.GetCurrentSelectedCard()` | `Shell/CardDetailPopup.cs 的 `artOverride`` | 文件级＋被多方法共用(7) | — |
| `AlternateArtPanel.Initialize(BasicCardUI cardUI, CardDisplayOptions options, Action<RawC…` | `Shell/CardDetailPopup.cs BuildAltArt（含 `HasAltArtStyle` ⇒ 整块关）` | 文件级(可补行号)＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1519` |
| `AlternateArtPanel.NavigationButtonClick(bool isLeft)` | `Shell/CardDetailPopup.cs BuildAltArrow` | 文件级(可补行号)＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1710` |
| `AlternateArtPanel.Refresh()` | `Shell/CardDetailPopup.cs:848 BuildAltArt（每次开窗现算）` | 指向别的方法/字段 | `CardPresentation/Shell/CardDetailPopup.cs:1519` |
| `AlternateArtPanel.SetStyle(AlternateArtSyle style, Sprite image)` | `Shell/CardDetailPopup.cs AltArtTexture/BuildAltArt` | 文件级(可补行号)＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1509` |
| `AlternateArtPanel.Start()` | `Shell/CollectionWindow.AltArtCards（同一份 7 张表）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2202` |
| `AlternateArtPanel.SwapStyle(bool isLeft)` | `Shell/CardDetailPopup.cs BuildAltArrow（左右钮）` | 文件级(可补行号)＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1710` |
| `AlternateArtPanel.ToggleButtons(bool toggle)` | `Shell/CardDetailPopup.cs BuildAltArrow 显隐（无风格时整块关，A856）` | 文件级(可补行号)＋被多方法共用(7) | `CardPresentation/Shell/CardDetailPopup.cs:1710` |
| `AttackTypesButtonsController.Initialize(Action<AttackTypes> onPointerEnter)` | `Battle/AttackSelector.cs:819 Build / :1561 OnHoverEnter` | 指向别的方法/字段 | `CardPresentation/Battle/AttackSelector.cs:1561` |
| `BlobShadowController.Start()` | `Core/BlobShadow.cs（floorY / target）` | 文件级 | — |
| `BlobShadowController.Update()` | `Core/BlobShadow.cs 的 Update 段（maxYForAlpha0 / scaleMultiplierByHeight）` | 文件级(可补行号) | `CardPresentation/Core/BlobShadow.cs:209` |
| `CardAlternateArtDrawer.Draw(AlternateArtCard item, int quantity)` | `Shell/ItemDrawer.cs PickDrawer/ItemTypeSet（异画走卡面/卡背图）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `CardAlternateArtDrawer.GetItemName(AlternateArtCard item)` | `Shell/ItemDrawer.cs 名字层（ItemSpec.Label）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:450` |
| `CardAlternateArtDrawer.HandlePointerClick()` | `Shell/ItemDrawerView.HandlePointerClick` | 仅类.方法(解析不到) | — |
| `CardDrawer.Draw(RawCardScript item, int quantity)` | `Shell/ItemDrawer.cs Draw（卡类走卡面/图）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `CardDrawer.GetItemName(RawCardScript item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `CardDrawer.HandlePointerClick()` | `Shell/ItemDrawerView.HandlePointerClick（本轮补）` | 仅类.方法(解析不到) | — |
| `CardbackDrawer.Draw(CosmeticItemCardback item, int quantity)` | `Shell/ItemDrawer.cs（卡背类走卡背图）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `CardbackDrawer.GetItemName(CosmeticItemCardback item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `ChangeNameWindow.Awake()` | `Shell/ProfileTab.cs:1293 起 BuildNameWindow` | 注释 | `CardPresentation/Shell/ProfileTab.cs:1295` |
| `ChangeNameWindow.ChangeNameButtonClick()` | `Shell/ProfileTab.cs CommitNameWindow` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/ProfileTab.cs:1490` |
| `ChangeNameWindow.Close()` | `Shell/ProfileTab.cs CancelNameWindow` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/ProfileTab.cs:1498` |
| `ChangeNameWindow.Open()` | `Shell/ProfileTab.cs OpenNameWindow（:1449 日志）` | 函数中间＋被多方法共用(4) | `CardPresentation/Shell/ProfileTab.cs:1443` |
| `ChangeNameWindow.ToggleChangeFree(bool option)` | `Shell/ProfileTab.cs（恒走首次免费那条）` | 文件级＋被多方法共用(4) | — |
| `ChangeNameWindow.ValidateInput(TMP_InputField inputField)` | `Shell/ProfileTab.cs:429-437（25/3 实读）` | 注释 | `CardPresentation/Shell/ProfileTab.cs:426(所在方法)` |
| `ChooseNamePopup.ClickChooseName()` | `Shell/ProfileTab.cs CommitNameWindow（改名窗确认，:1392 那颗钮）` | 函数中间＋被多方法共用(4) | `CardPresentation/Shell/ProfileTab.cs:1490` |
| `ChooseNamePopup.DelaySizeFix()` | `Shell/ProfileTab.cs 改名窗排版（ForceRelayout 口径）` | 文件级＋被多方法共用(4) | — |
| `ChooseNamePopup.Initialize()` | `Shell/ProfileTab.cs:1293-1460 改名窗建件` | 注释 | `CardPresentation/Shell/ProfileTab.cs:1288(所在方法)` |
| `ChooseNamePopup.ResetCanClick()` | `Shell/ProfileTab.cs 改名钮可点闸` | 文件级＋被多方法共用(4) | — |
| `ChooseNamePopup.Setup(Action<string> doneAction, Action closeAction)` | `Shell/ProfileTab.cs 改名窗 Open/Commit/Cancel 回调` | 文件级＋被多方法共用(4) | — |
| `ChooseNamePopup.ToFocus()` | `Shell/GameWindow.cs 前台支` | 文件级 | — |
| `ChooseNamePopup.ValidateInput(TMP_InputField inputField)` | `Shell/ProfileTab.cs:429-437` | 注释 | `CardPresentation/Shell/ProfileTab.cs:426(所在方法)` |
| `ContainerDrawer.Draw(ShopContainerBase item, int quantity)` | `Shell/ItemDrawer.cs（宝箱/容器档）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `ContainerDrawer.GetItemName(ShopContainerBase item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `CosmeticDrawer.Draw(CosmeticItem item, int quantity)` | `Shell/ItemDrawer.cs（装饰档）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `CosmeticDrawer.GetItemName(CosmeticItem item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `DragAndDropItem.GetMousePositionInWoldSpace(Vector3 mousePosition)` | `Core/LayoutSpace.ScreenToWorld（Shell/Core 通用换算）` | 仅类.方法(可补行号) | `CardPresentation/Core/LayoutSpace.cs:147` |
| `DragAndDropItem.GetScreenPositionWithZValue(Vector3 screenPosition)` | `Core/LayoutSpace 换算` | 仅类/描述(无方法) | — |
| `Draggable.Initialize(T item)` | `Core/DraggableController.cs:116 abstract Initialize；实现见 Core/CardPrevi…` | 指向别的方法/字段 | `CardPresentation/Core/DraggableController.cs:118` |
| `DropTableContainer.GetID()` | `Shell/ShopData.cs（本地商品数据）` | 文件级 | — |
| `DuelPopupWindow.OnClassicButtonPressed()` | `Shell/DuelPopupWindow.cs DuelButton→OnMode("classic")` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DuelPopupWindow.cs:231` |
| `DuelPopupWindow.OnCloseButtonPressed()` | `Shell/DuelPopupWindow.cs 关窗钮 → Close()` | 文件级＋被多方法共用(4) | — |
| `DuelPopupWindow.OnSkirmishButtonPressed()` | `Shell/DuelPopupWindow.cs DuelButton→OnMode("skirmish")` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DuelPopupWindow.cs:231` |
| `DuelPopupWindow.Open()` | `Shell/DuelPopupWindow.cs Open` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DuelPopupWindow.cs:131` |
| `EverguildButton.Awake()` | `Shell/WindowButton.cs:Collect（懒收集）/ :545 ApplyTintInstant` | 被多方法共用 | — |
| `EverguildButton.DoStateTransition(SelectionState state, bool instant)` | `Shell/WindowButton.cs:107 State/:128 SetSelected/:485-530/:545 ApplyTi…` | 指向别的方法/字段 | `CardPresentation/Shell/WindowButton.cs:128` |
| `EverguildButton.OnEnable()` | `Shell/WindowButton.cs（命中走几何；灰度由 Interactable/SetSoftDisabled 写、材质随件保留）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildButton.RemoveClickListeners()` | `Shell/WindowButton.cs:17 public System.Action onClick（置 null 即清）` | 指向别的方法/字段 | — |
| `EverguildButton.SwitchMaterial(bool interactable)` | `Shell/WindowButton.cs RefreshGray（`Everguild/UI/Greyscale` + 原材质还原）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildButtonHelper.DoMaterialRefresh(List<EverguildButtonMaterialModifier> graphicsToChan…` | `Shell/WindowButton.cs RefreshGray（逐件换 sharedMaterial + 还原）` | 文件级(可补行号) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildButtonHelper.GetGraphicsInChildren(GameObject target, ref List<EverguildButtonMater…` | `Shell/WindowButton.cs Collect（收 ImageQuad + target + TintOn extras）` | 文件级(可补行号) | `CardPresentation/Shell/WindowButton.cs:577` |
| `EverguildButtonStateFollower.Disabled()` | `Shell/WindowButton.cs RefreshGray` | 文件级(可补行号) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildButtonStateFollower.Enabled()` | `Shell/WindowButton.cs RefreshGray(false)` | 文件级(可补行号) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildInput.GetKey(KeyCode key, bool ignoreInputField)` | `Shell/PointerLayer.cs 键盘入口` | 文件级＋被多方法共用(3) | — |
| `EverguildInput.GetKeyDown(KeyCode key, bool ignoreInputField)` | `Shell/PointerLayer.cs:710 一帧键盘入口` | 注释 | `CardPresentation/Shell/PointerLayer.cs:643(所在方法)` |
| `EverguildInput.GetKeyUp(KeyCode key, bool ignoreInputField)` | `Shell/PointerLayer.cs 键盘入口` | 文件级＋被多方法共用(3) | — |
| `EverguildInput.IsPointerOverUIObject(int pointerId)` | `Shell/PointerLayer.cs 命中表` | 文件级＋被多方法共用(3) | — |
| `EverguildInput.Process()` | `Shell/PointerLayer.cs:710 一帧输入入口` | 注释 | `CardPresentation/Shell/PointerLayer.cs:643(所在方法)` |
| `EverguildTextMeshPro.Awake()` | `Core/TmpFont.cs（字体面表）+ Battle/Label.cs SetFace` | 文件级(可补行号) | `CardPresentation/Battle/Label.cs:1117` |
| `EverguildTextMeshPro.CheckFOntInPreset()` | `Core/TmpFont.cs 字体资产表` | 文件级 | — |
| `EverguildTextMeshPro.Refresh()` | `Core/TmpFont + Label.SetFace + ForceRelayout` | 仅类.方法(可补行号) | `CardPresentation/Battle/Label.cs:1117` |
| `EverguildTextMeshPro.UpdateTextSizeSmallScreen()` | `Shell/TransformScalerBySmallScreenUI.cs（整窗缩放）+ Core/TmpFont` | 文件级 | — |
| `EverguildToggle.PlayButtonSound()` | `Shell/WindowButton.cs:924` | 指向别的方法/字段 | — |
| `EverguildToggle.RefreshVisuals()` | `调用点换图（SettingsWindow.cs:2021 / MuteChat 两组图）` | 函数中间 | `CardPresentation/Shell/SettingsWindow.cs:2014(所在方法)` |
| `EverguildToggle.ToggleGreyscale(bool enabled)` | `Shell/WindowButton.cs RefreshGray（Greyscale 材质）` | 文件级(可补行号) | `CardPresentation/Shell/WindowButton.cs:697` |
| `EverguildToggle.ToggleSprite(bool isOn)` | `调用点 `SetNormalTex(onTex)`（Shell/SettingsWindow.cs:2021）` | 函数中间 | `CardPresentation/Shell/SettingsWindow.cs:2014(所在方法)` |
| `EverguildTooltipItem.Awake()` | `Core/Tooltip.cs:252 Build` | 指向别的方法/字段 | — |
| `EverguildTooltipItem.GetPivotPosition(AnchorPositions anchorPosition)` | `Core/Tooltip.cs 贴边翻转` | 文件级 | — |
| `EverguildTooltipItem.Hide(Action onComplete)` | `Core/Tooltip.cs:336 Hide / :344 FinishFade` | 函数中间 | `CardPresentation/Core/Tooltip.cs:392` |
| `EverguildTooltipTrigger.Hide()` | `Core/Tooltip.cs:336 Hide` | 函数中间 | `CardPresentation/Core/Tooltip.cs:392` |
| `EverguildTooltipTrigger.OnPointerExit(PointerEventData eventData)` | `Core/Tooltip.cs:336` | 函数中间 | `CardPresentation/Core/Tooltip.cs:313(所在方法)` |
| `EverguildTooltipTrigger.Setup(string text, bool localize, string title)` | `Core/Tooltip.cs Show(body)` | 文件级(可补行号) | `CardPresentation/Core/Tooltip.cs:307` |
| `EverguildTooltipTrigger.Toggle(bool option)` | `Shell/WindowButton onEnter/onExit 钩子（各调用点挂/摘）` | 仅类.方法(可补行号) | `CardPresentation/Shell/WindowButton.cs:51` |
| `ExpansionPassPointDrawer.Draw(ExpansionPassPoints item, int quantity)` | `Shell/ItemDrawer.cs（点数/货币档）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `ExpansionPassPointDrawer.DrawForArmy(CardArmy army)` | `Shell/ItemDrawer.cs + Core/CardArt.FlavorBg` | 文件级＋被多方法共用(3) | — |
| `ExpansionPassPointDrawer.GetItemName(ExpansionPassPoints item)` | `Shell/ItemDrawer.cs 名字层` | 文件级＋被多方法共用(3) | — |
| `FlareScenarioToggler.DoScenarioBlend(ScenarioBlendOptions scenarioBlendOptions)` | `Battle/FlareScenarioToggler.cs（同名件，逐值）` | 文件级(可补行号) | `CardPresentation/Battle/FlareScenarioToggler.cs:35` |
| `FlareScenarioToggler.Initialize()` | `Battle/FlareScenarioToggler.cs` | 文件级 | — |
| `GameWindow.Close()` | `Shell/GameWindow.cs:692 Close（守卫/PlayCloseSound/NotifyClosed 次序，判据逐句）` | 函数中间 | `CardPresentation/Shell/GameWindow.cs:705` |
| `GameWindow.Hide()` | `Shell/GameWindow.cs:679 Hide（本轮补 OnCloseWindow，末句 try/catch）` | 注释 | `CardPresentation/Shell/GameWindow.cs:690` |
| `GameWindow.Open()` | `Shell/GameWindow.cs:586 Open + :562 ApplySmallScreenScale（挪进 TryOpen 两…` | 注释 | `CardPresentation/Shell/GameWindow.cs:587` |
| `GameWindow.SetupData(object data, GameWindowOptions options)` | `Shell/GameWindow.cs:583 SetupData(data)` | 注释 | `CardPresentation/Shell/GameWindow.cs:584` |
| `GameWindow.ToggleESC(bool option)` | `Shell/GameWindow.cs:85 public bool closeOnEsc（调用点直接赋值）` | 指向别的方法/字段 | — |
| `GameWindow.TryOpen(object data, GameWindowOptions options)` | `Shell/GameWindow.cs:482 TryOpen(data) → :513` | 注释 | `CardPresentation/Shell/GameWindow.cs:483` |
| `GameWindowWithTabs.ChangeTab(WindowTabBase tab)` | `Shell/GameWindowWithTabs.cs:ChangeTab(WindowTabBase) → ChangeTabCO` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:140` |
| `GameWindowWithTabs.ChangeTabCO(WindowTabBase from, WindowTabBase to)` | `Shell/GameWindowWithTabs.cs:ChangeTabCO（同页只重开；换页 CloseTab+TryOpenTab+O…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:181` |
| `GameWindowWithTabs.Close()` | `Shell/GameWindowWithTabs.cs:Close（base.Close() 后 CurrentTabView.CloseT…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:204` |
| `GameWindowWithTabs.ForceCloseTabs()` | `Shell/GameWindowWithTabs.cs:ForceCloseTabs（当前页 CloseTab + 全部 SetActive…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:211` |
| `GameWindowWithTabs.OnTabChange()` | `Shell/GameWindowWithTabs.cs:OnTabChange（protected virtual，默认空；ChangeTa…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:201` |
| `GameWindowWithTabs.Open()` | `四扇窗 Open 各调 `EnsureTabsSetup()` (= lazy `SetupTabs`) + 原有 `Click(start…` | 仅类/描述(无方法) | — |
| `GameWindowWithTabs.SetupTabs()` | `Shell/GameWindowWithTabs.cs:SetupTabs（`tabButtons.Initialize` + Type→t…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/GameWindowWithTabs.cs:80` |
| `GeneralMenuController.ActivateLoadingFade(bool activate)` | `Shell/ShellRuntime.cs:203 CoFadeBackAfterLoad + LoadingMenu` | 注释 | `CardPresentation/Shell/ShellRuntime.cs:204` |
| `GeneralMenuController.Awake()` | `Shell/ShellRuntime.Awake + Shell/MainMenuRuntime.Build` | 仅类.方法(可补行号) | `CardPresentation/Shell/ShellRuntime.cs:309` |
| `GeneralMenuController.Initialize()` | `Shell/ShellRuntime.Build + MainMenuRuntime.Build(:401)` | 仅类.方法(可补行号) | `CardPresentation/Shell/ShellRuntime.cs:611` |
| `GeneralMenuController.RemoveHighlights()` | `Shell/TutorialPointerUi.cs:148 Hide` | 函数中间 | `CardPresentation/Shell/TutorialPointerUi.cs:171` |
| `GeneralMenuController.ShowHighlight(Vector3 position)` | `Shell/TutorialPointerUi.cs:125 Show(posPx)` | 指向别的方法/字段 | `CardPresentation/Shell/TutorialPointerUi.cs:140` |
| `GeneralTab.OnSetup()` | `Shell/SettingsWindow.cs General 页` | 文件级 | — |
| `GeneralTab.TouchInputToggle(bool value)` | `Shell/SettingsWindow.cs（设置项）+ Battle/TouchInputManager` | 文件级 | — |
| `GenericArmyItemDrawer.Draw(GenericArmyItem item, int quantity)` | `Shell/ItemDrawer.cs（Resource 档）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `GenericArmyItemDrawer.HandlePointerClick()` | `Shell/ItemDrawerView.HandlePointerClick` | 仅类.方法(解析不到) | — |
| `GenericOptionsPanel.Open()` | `Shell/GenericOptionsPanel.cs Open` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/GenericOptionsPanel.cs:272` |
| `GenericOptionsPanel.SetButtons(IEnumerable<Button> buttons, bool localizeText)` | `Shell/GenericOptionsPanel.cs Context 项表（Create/Show 时建钮）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/GenericOptionsPanel.cs:209` |
| `GenericOptionsPanel.SetPosition(Transform targetPosition)` | `Shell/GenericOptionsPanel.cs` | 文件级＋被多方法共用(4) | — |
| `GenericOptionsPanel.SetTitle(string text, bool localizeText)` | `Shell/GenericOptionsPanel.cs` | 文件级＋被多方法共用(4) | — |
| `GenericOptionsPanel.Start()` | `Shell/GenericOptionsPanel.cs:240 Create（建件）` | 指向别的方法/字段 | — |
| `GraphicsTab.AutoZoomClick(bool option)` | `Shell/SettingsWindow.cs 图像页（AutoZoom 行）` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.CardsTextInHandDropdownChange(int chosenCardTextsInHand)` | `Shell/SettingsWindow.cs 图像页（CardsTextInHand 项）` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.ConfigureSuperSamplingVisibility()` | `Shell/SettingsWindow.cs 图像页（Vync/FPS 行号两套表 3/4 ↔ 4/5）` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.FPSLimitValueChanged(float fpsValue)` | `Shell/SettingsWindow.cs 图像页 FPS 滑块` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.GraphicsQualityDropdownChange(int chosenQuality)` | `Shell/SettingsWindow.cs Quality Selector` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.OnEnable()` | `Shell/SettingsWindow.cs 页签切换刷新` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.OnSetup()` | `Shell/SettingsWindow.cs 建件 + 行显隐` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.SmallScreenToggleClick(bool option)` | `Shell/TransformScalerBySmallScreenUI.cs（开关）` | 文件级 | — |
| `GraphicsTab.SuperSamplingToggleClick(bool option)` | `Shell/SettingsWindow.cs 图像页（SuperSampling 项）` | 文件级＋被多方法共用(9) | — |
| `GraphicsTab.VSyncToggleClick(bool option)` | `Shell/SettingsWindow.cs 图像页` | 文件级＋被多方法共用(9) | — |
| `ItemDisplayWindow.Open()` | `Shell/ItemDisplayWindow.cs:208 Create + ctx` | 指向别的方法/字段 | `CardPresentation/Shell/ItemDisplayWindow.cs:227` |
| `ItemDrawerConfig.GetDrawingOptions(Type type)` | `Shell/ItemDrawer.cs PickDrawer + ItemTypeSet 表` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:471` |
| `ItemInformationPanel.Initialize(ObtainableItem Item, ItemDrawerOptions Options)` | `Shell/ItemDisplayWindow.Create(ctx)（本轮接线）` | 仅类.方法(可补行号) | `CardPresentation/Shell/ItemDisplayWindow.cs:208` |
| `ItemInformationPanel.SetText(TextMeshProUGUI text, string key, bool localize)` | `Core/Loc.T + Label.SetText` | 仅类.方法(可补行号) | `CardPresentation/Battle/Label.cs:367` |
| `LoadingMenu.Awake()` | `Shell/LoadingMenu.cs:126 Build` | 指向别的方法/字段 | `CardPresentation/Shell/LoadingMenu.cs:120` |
| `MainMenuWindow.ClickChooseName(string newName)` | `Shell/ProfileTab.cs 改名链（内嵌改名窗 L_NameHit）+ Shell/ProfileData.PlayerName` | 文件级(可补行号) | `CardPresentation/Shell/ProfileTab.cs:104` |
| `MainMenuWindow.Close()` | `Shell/MainMenuWindow.cs Close（GameWindow 基类）` | 文件级 | — |
| `MainMenuWindow.ESCPressed()` | `Shell/PointerLayer ESC 分发` | 仅类/描述(无方法) | — |
| `MainMenuWindow.Hide()` | `Shell/MainMenuWindow Hide（基类）` | 仅类/描述(无方法) | — |
| `MainMenuWindow.Initialize()` | `Shell/MainMenuRuntime.Build + WindowsManager.SetBaseMenu` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:401` |
| `MainMenuWindow.OpenChooseNameMenu()` | `Shell/ProfileTab.cs 改名窗开/关链` | 文件级 | — |
| `MainMenuWindow.SetupChat()` | `Shell/ChatPanel.cs + Shell/MainMenuRuntime 聊天预览` | 文件级 | — |
| `MainMenuWindow.ToBackground()` | `Shell/MainMenuWindow override ToBackground` | 仅类/描述(无方法) | — |
| `MediaTab.MusicValueChange(float newValue)` | `Shell/SettingsWindow.cs 音频页` | 文件级＋被多方法共用(7) | — |
| `MediaTab.OnOpen()` | `Shell/SettingsWindow.cs 页签切换` | 文件级＋被多方法共用(7) | — |
| `MediaTab.OnSetup()` | `Shell/SettingsWindow.cs 建件` | 文件级＋被多方法共用(7) | — |
| `MediaTab.OnWindowModeChanged(int option)` | `Shell/SettingsWindow.cs 音频页 PC 段（WindowMode 项）` | 文件级＋被多方法共用(7) | — |
| `MediaTab.SetupVideoSettings()` | `Shell/SettingsWindow.cs（PlatformBasedComponents 等价）` | 文件级＋被多方法共用(7) | — |
| `MediaTab.SoundFXValueChange(float newValue)` | `Shell/SettingsWindow.cs 音频页` | 文件级＋被多方法共用(7) | — |
| `MediaTab.VoiceOversValueChange(float newValue)` | `Shell/SettingsWindow.cs 音频页` | 文件级＋被多方法共用(7) | — |
| `MenuLoadingController.Awake()` | `Shell/LoadingMenu.cs:126 Build` | 指向别的方法/字段 | `CardPresentation/Shell/LoadingMenu.cs:120` |
| `MenuSprites.GetCrateItemsCounterSprite(int crateTier)` | `Core/CardArt.MenuUi 图名表（各窗按图名取）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardArt.cs:870` |
| `MenuSprites.GetFriendlyWorldSprite()` | `Core/ArmyIcons + CardArt` | 仅类/描述(无方法) | — |
| `MenuSprites.GetWorldSprite(int playerTier)` | `Core/RarityColors / ArmyIcons 图名表` | 仅类/描述(无方法) | — |
| `NavigationPanelController.Awake()` | `Shell/MainMenuRuntime.BuildNavigationPanel（导航钮表）` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:1031` |
| `NavigationPanelController.GetToggle(NavigationPanelToggleType type)` | `Shell/MainMenuRuntime 导航钮表索引` | 仅类/描述(无方法) | — |
| `NavigationPanelController.SetMainToggle(GameWindow gameWindow)` | `Shell/MainMenuRuntime.SetNavToggle` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:863` |
| `NavigationPanelController.SetToggle(NavigationPanelToggleType type)` | `Shell/MainMenuRuntime.SetNavToggle` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:863` |
| `NavigationPanelController.Toggle(bool option, bool withAnimation)` | `Shell/MainMenuRuntime.TickNavPanelTween（导航入场补间）+ 显隐` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:1529` |
| `OpenCloseTransition.InAnimation()` | `Shell/ShellRuntime.cs:203 CoFadeBackAfterLoad` | 注释 | `CardPresentation/Shell/ShellRuntime.cs:204` |
| `OpenCloseTransition.LoadingAnimation()` | `Shell/LoadingMenu.cs` | 文件级 | — |
| `OpenCloseTransition.OutAnimation()` | `Shell/ShellRuntime.cs:119` | 注释 | — |
| `OpenWindowButton.Awake()` | `Shell/MainMenuRuntime 导航接线（OpenByRef）+ WindowButton.Bind` | 仅类.方法(可补行号) | `CardPresentation/Shell/WindowButton.cs:325` |
| `OpenWindowButton.ButtonClick()` | `Shell/MainMenuRuntime.OnNavClick` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:1371` |
| `OpenWindowButton.CloseWindow()` | `Shell/WindowsManager.CloseAllWindows/GameWindow.Close` | 仅类.方法(可补行号) | `CardPresentation/Shell/WindowsManager.cs:605` |
| `OpenWindowButton.OpenWindow()` | `Shell/MainMenuRuntime.OpenByRef(..., closeAll)` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:1619` |
| `PopUpGameWindow.ConfigurePopUp(string text, bool localizeTexts, bool closeOnEsc)` | `Shell/PopUpGameWindow.cs + WindowsManager.ShowPopUp` | 文件级(可补行号) | `CardPresentation/Shell/PopUpGameWindow.cs:290` |
| `PopUpGameWindow.SetText(string text, bool localizeTexts)` | `Shell/PopUpGameWindow.cs SetText` | 文件级 | — |
| `PracticeArmySelectionButton.Awake()` | `Shell/PracticeModePopup.cs ArmyHl* 三件 + ConfigureArmyFilterButtons` | 文件级＋被多方法共用(4) | — |
| `PracticeArmySelectionButton.Highlight(bool state)` | `Shell/PracticeModePopup.cs ArmyHl*（`UI_Deck_button_click` 高亮层）` | 文件级＋被多方法共用(4) | — |
| `PracticeArmySelectionButton.Initialize(CardArmy army, PracticeModePopup selectionMenu)` | `Shell/PracticeModePopup.cs 阵营筛选钮建表` | 文件级＋被多方法共用(4) | — |
| `PracticeArmySelectionButton.OnArmySelected()` | `Shell/PracticeModePopup.ChangeSelectedArmy` | 仅类.方法(解析不到) | — |
| `PracticeArmySelectionButton.ToggleHighlightPlayerDeck(bool state)` | `Shell/PracticeModePopup.cs `IsPlayerDeck`/`Button border` 显隐` | 文件级＋被多方法共用(4) | — |
| `PromptPopup.OnCancel()` | `Shell/PromptPopup.cs 取消钮` | 文件级＋被多方法共用(3) | — |
| `PromptPopup.OnSubmit()` | `Shell/PromptPopup.cs 提交钮` | 文件级＋被多方法共用(3) | — |
| `PromptPopup.Open()` | `Shell/PromptPopup.cs:97 Create/Open` | 指向别的方法/字段 | `CardPresentation/Shell/PromptPopup.cs:115` |
| `PromptPopup.SetMessage(string error, bool localize)` | `Shell/PromptPopup.cs 正文` | 文件级＋被多方法共用(3) | — |
| `RandomCardDrawer.Draw(DropTableItem item, int quantity)` | `Shell/ItemDrawer.cs Wildcard/随机卡档` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `RandomCardDrawer.GetItemName(DropTableItem item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `RandomCardIconDrawer.Draw(DropTableItem item, int quantity)` | `Shell/ItemDrawer.cs WildcardIcon` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `RandomCardIconDrawer.GetItemName(DropTableItem item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `ReferralPopupWindow.Open()` | `Shell/ReferralPopupWindow.cs Open` | 文件级(可补行号) | `CardPresentation/Shell/ReferralPopupWindow.cs:310` |
| `ResourcesBarController.GetCounter(GameCurrency currency)` | `Shell/TopBar.cs 的资源计数器表（建表序）` | 文件级 | — |
| `ResourcesBarController.Initialize(GameCurrency[] currenciesToShow)` | `Shell/TopBar.cs CounterSpecs（crystals·blackStones·gold 建表序）` | 文件级 | — |
| `ScenarioGenericObjectToggler.DoScenarioBlend(ScenarioBlendOptions blendOptions)` | `Battle/ScenarioGenericObjectToggler.cs（同名件）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/ScenarioGenericObjectToggler.cs:20` |
| `ScenarioGenericObjectToggler.SetTargetFade(float targetValue, float blendTime)` | `Battle/ScenarioGenericObjectToggler.cs` | 文件级＋被多方法共用(4) | — |
| `ScenarioGenericObjectToggler.Start()` | `Battle/ScenarioGenericObjectToggler.cs` | 文件级＋被多方法共用(4) | — |
| `ScenarioGenericObjectToggler.Update()` | `Battle/ScenarioGenericObjectToggler.cs` | 文件级＋被多方法共用(4) | — |
| `SceneChanger.DoChange()` | `Shell/ShellRuntime.LoadSceneWithFade + DeckRuntime 切场` | 仅类.方法(可补行号) | `CardPresentation/Shell/ShellRuntime.cs:120` |
| `SceneChanger.Transition()` | `Shell/ShellRuntime 淡入淡出` | 仅类/描述(无方法) | — |
| `SceneTransition.InAnimation()` | `Shell/ShellRuntime.cs:203 CoFadeBackAfterLoad（淡入）` | 注释 | `CardPresentation/Shell/ShellRuntime.cs:204` |
| `SceneTransition.LoadingAnimation()` | `Shell/LoadingMenu.cs（转 cog/skull）` | 文件级 | — |
| `SceneTransition.OutAnimation()` | `Shell/ShellRuntime.cs:119 LoadSceneWithFade（淡出）` | 注释 | `CardPresentation/Shell/ShellRuntime.cs:120` |
| `ScreenAspectRatioController.AdjustCameraViewport()` | `Battle/ScreenAspectRatioController.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/ScreenAspectRatioController.cs:169` |
| `ScreenAspectRatioController.RenderPipelineManagerOnbeginCameraRendering(ScriptableRenderContext ar…` | `Battle/ScreenAspectRatioController.cs 的 URP 钩子` | 文件级＋被多方法共用(4) | — |
| `ScreenAspectRatioController.SetCamerasRect(Rect viewport)` | `Battle/ScreenAspectRatioController.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/ScreenAspectRatioController.cs:201` |
| `ScreenAspectRatioController.Start()` | `Battle/ScreenAspectRatioController.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/ScreenAspectRatioController.cs:132` |
| `ScrollViewFocusFunctions.FocusOnItem(ScrollRect scrollView, RectTransform item)` | `Shell/MenuScroll.cs:315 + 调用点 CampaignTab.cs:894 / ForgeTab.cs:835,846…` | 指向别的方法/字段 | — |
| `SearchingMatchWindowDemo.Awake()` | `Shell/SearchingMatchPopup.cs:224 Build / :209 Attach` | 指向别的方法/字段 | `CardPresentation/Shell/SearchingMatchPopup.cs:209` |
| `SearchingMatchWindowDemo.CancelSearchButtonOnClick()` | `Shell/SearchingMatchPopup.cs Cancel 钮` | 文件级(可补行号) | `CardPresentation/Shell/SearchingMatchPopup.cs:343` |
| `SearchingOpponentWindow.CancelMatchMatchmaking()` | `Shell/SearchOpponentManager.cs Cancel` | 文件级 | — |
| `SearchingOpponentWindow.Close()` | `Shell/SearchingOpponentWindow.cs Close` | 文件级(可补行号) | `CardPresentation/Shell/SearchingOpponentWindow.cs:342` |
| `SearchingOpponentWindow.Open()` | `Shell/SearchingOpponentWindow.cs Open` | 文件级(可补行号) | `CardPresentation/Shell/SearchingOpponentWindow.cs:376` |
| `SelectCardButtonFrame.Awake()` | `Shell/CardChoicePanel.cs（按索引 + 卡视图表）` | 文件级＋被多方法共用(3) | — |
| `SelectCardButtonFrame.CardClicked()` | `Shell/CardChoicePanel.cs TogglePick` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Battle/CardChoicePanel.cs:924` |
| `SelectCardButtonFrame.Initialize(BasicCardUI cardUI, ChooseCardMenu chooseCardMenu)` | `Shell/CardChoicePanel.cs:662 Open（逐张绑回调）` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:655(所在方法)` |
| `SelectCardButtonFrame.SelectButtonOnClick()` | `Shell/CardChoicePanel.cs:902 TogglePick` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:924` |
| `SelectCardButtonFrame.ToggleSelected(bool option)` | `Shell/CardChoicePanel.cs ApplyPicks` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Battle/CardChoicePanel.cs:975` |
| `SettingsMenu.Awake()` | `Shell/SettingsWindow.cs Build + 绑钮` | 文件级 | — |
| `SettingsMenu.Close()` | `Shell/GameWindow.Close（SettingsWindow）` | 仅类.方法(可补行号) | `CardPresentation/Shell/GameWindow.cs:705` |
| `SettingsMenu.ExitGamePopup()` | `Shell/MainMenuRuntime.EscapePressed（退出游戏弹窗三文案键）` | 仅类.方法(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:1654` |
| `Shade.SwitchShade(bool switchOn, bool useAnim)` | `Shell/Shade.cs:40 SetAlpha / :70 StepTo（+ defaultTimeToSwitch=0.5）` | 被多方法共用 | — |
| `Shade.SwitchShade(bool switchOn, bool useAnim, float timeToSwitch, float tar…` | `Shell/Shade.cs:18 onAlphaLevel + :70 StepTo` | 指向别的方法/字段 | `CardPresentation/Shell/Shade.cs:70` |
| `Shade.SwitchShadeToAlpha(bool switchOn, bool useAnim, float targetAlpha)` | `Shell/Shade.cs:40 SetAlpha` | 被多方法共用 | — |
| `Shade.finish()` | `Shell/Shade.cs:40 SetAlpha / :70 StepTo（批处理直接落终值）` | 被多方法共用 | — |
| `SimpleSceneTransition.InAnimation()` | `Shell/ShellRuntime.cs:203` | 注释 | `CardPresentation/Shell/ShellRuntime.cs:120(所在方法)` |
| `SimpleSceneTransition.OutAnimation()` | `Shell/ShellRuntime.cs:119` | 注释 | — |
| `TabButtons.Initialize(GameWindowWithTabs window)` | `Shell/TabButtons.cs:Initialize（关母版 + 订阅 `OnTabChanged` + 消费 firstTime）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/TabButtons.cs:72` |
| `TabButtons.Toggle(bool state, WindowTabBase tab)` | `Shell/TabButtons.cs:Toggle / ToggleForTest（firstTime 闸 → `window.Chang…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/TabButtons.cs:124` |
| `TabButtons.UpdateTab(WindowTabBase tab)` | `Shell/TabButtons.cs:UpdateTab（同步 CurrentType / CurrentVisualIndex；无 to…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/TabButtons.cs:147` |
| `TitleDrawerHorizontal.ToggleHighlight(bool toggle)` | `Shell/ItemDrawerView.ToggleHighlight（本轮补）` | 仅类.方法(解析不到) | — |
| `TitleDrawerHorizontal.ToggleLock(bool toggle)` | `Shell/ItemDrawerView.ToggleLock（本轮补；= ItemDrawerView.ToggleClaimedWarn…` | 仅类.方法(解析不到) | — |
| `TitleTab.SetTitle(CosmeticItemTitle item)` | `Shell/ProfileData.TitleIndex/TitleName（当前称号这一半有）` | 仅类.方法(解析不到) | — |
| `ToastNotification.ForceUpdateLayout()` | `Shell/ToastNotifications.cs 的 banner 重排` | 文件级 | — |
| `ToastNotification.Initialize(object obj)` | `Shell/ToastNotifications.cs Context + 显示` | 文件级(可补行号) | `CardPresentation/Shell/ToastNotifications.cs:206` |
| `ToastNotificationController.GetToastNotificationPrefab(ToastNotificationType toastType)` | `Shell/ToastNotifications.cs（按 Type 选样式，代码建）` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/ToastNotifications.cs:190` |
| `ToastNotificationController.ScheduleNotification(ToastNotificationContext context)` | `Shell/ToastNotifications.cs 入队` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/ToastNotifications.cs:737` |
| `ToastNotificationController.ShowNotification(ToastNotificationContext context)` | `Shell/ToastNotifications.cs Show` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/ToastNotifications.cs:217` |
| `ToastNotificationController.ShowNotificationInternal(ToastNotificationContext context)` | `Shell/ToastNotifications.cs 内部展示` | 文件级＋被多方法共用(5) | — |
| `ToastNotificationController.TryShowNotification(ToastNotificationContext context)` | `Shell/ToastNotifications.cs 上限/去重` | 文件级＋被多方法共用(5) | — |
| `ToggleDisplay.Setup(bool toggled, string textTerm, Sprite sprite)` | `Shell/WindowButton.cs:128/:451（SetSelected + SetNormalTex，如 SettingsWi…` | 指向别的方法/字段 | `CardPresentation/Shell/WindowButton.cs:451` |
| `TransformScalerBySmallScreenUI.Initialize()` | `Shell/TransformScalerBySmallScreenUI.cs:53 Initialize` | 注释 | `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs:130` |
| `TransformScalerBySmallScreenUI.LateUpdate()` | `Shell/TransformScalerBySmallScreenUI.cs:17` | 注释 | `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs:137` |
| `TransformScalerBySmallScreenUI.OnEnable()` | `Shell/TransformScalerBySmallScreenUI.cs:12` | 注释 | `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs:135` |
| `TransformScalerBySmallScreenUI.SetScale(float scaleToUse)` | `Shell/TransformScalerBySmallScreenUI.cs:47` | 函数中间 | `CardPresentation/Shell/TransformScalerBySmallScreenUI.cs:122` |
| `UIBakeScreen.CreateRT()` | `Shell/UIBakeScreen.cs CreateRT` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/UIBakeScreen.cs:156` |
| `UIBakeScreen.OnDestroy()` | `Shell/UIBakeScreen.cs OnDestroy` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/UIBakeScreen.cs:284` |
| `UIBakeScreen.ResolutionChanged()` | `Shell/UIBakeScreen.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/UIBakeScreen.cs:278` |
| `UIBakeScreen.Start()` | `Shell/UIBakeScreen.cs` | 文件级＋被多方法共用(5) | — |
| `UIBakeScreen.Toggle(bool option)` | `Shell/UIBakeScreen.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/UIBakeScreen.cs:201` |
| `UICulling.GetWorldRect(RectTransform rectTransform)` | `Core/LayoutSpace / MenuDraw PxRect 体系` | 仅类/描述(无方法) | — |
| `UIErrorMessageItem.LayoutRefresh()` | `Battle/ErrorMessageBanner.cs（错误横幅）` | 文件级＋被多方法共用(3) | — |
| `UIErrorMessageItem.Show(string textToShow, Color color, bool localize)` | `Battle/ErrorMessageBanner.cs Show + Shell/MessageToast.cs:212 ShowErro…` | 被多方法共用 | — |
| `UIErrorMessageItem.Toggle(bool option)` | `Battle/ErrorMessageBanner.cs / MessageToast` | 文件级＋被多方法共用(3) | — |
| `UIGradientUtils.Bilerp(Color a1, Color a2, Color b1, Color b2, Vector2 t)` | `Battle/ImageQuad.SetCornerColors（四角顶点色）` | 仅类.方法(可补行号) | `CardPresentation/Battle/ImageQuad.cs:517` |
| `UIGradientUtils.CompensateAspectRatio(Rect rect, Vector2 dir)` | `Core/CardArt.cs:766 Gradient(angleDeg)（同一条补偿）` | 注释 | `CardPresentation/Core/CardArt.cs:767` |
| `UIGradientUtils.RotationDir(float angle)` | `Core/CardArt.cs:766（角度按 (sin,cos) 口径）` | 注释 | `CardPresentation/Core/CardArt.cs:743(所在方法)` |
| `UrlButton.Initialize(string url)` | `Shell/SettingsWindow.cs:2737（内联 URL 动作；唯一对外动作 = OpenURL）` | 函数中间 | `CardPresentation/Shell/SettingsWindow.cs:2728(所在方法)` |
| `UrlButton.OnClick()` | `Shell/SettingsWindow.cs:2737 Application.OpenURL` | 函数中间 | `CardPresentation/Shell/SettingsWindow.cs:2728(所在方法)` |
| `WelcomePopup.Close()` | `Shell/WelcomePopup.cs Close` | 文件级＋被多方法共用(3) | — |
| `WelcomePopup.OnClose()` | `Shell/WelcomePopup.cs 关闭链` | 文件级＋被多方法共用(3) | — |
| `WelcomePopup.Open()` | `Shell/WelcomePopup.cs Open` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/WelcomePopup.cs:138` |
| `WhereToGetCardsWindow.Awake()` | `Shell/WhereToGetCardsPopup.cs Build` | 文件级＋被多方法共用(4) | — |
| `WhereToGetCardsWindow.Close()` | `Shell/WhereToGetCardsPopup.cs Close` | 文件级＋被多方法共用(4) | — |
| `WhereToGetCardsWindow.Open()` | `Shell/WhereToGetCardsPopup.cs Open（sections/context）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/WhereToGetCardsPopup.cs:272` |
| `WhereToGetCardsWindow.Reset()` | `Shell/WhereToGetCardsPopup.cs Reset` | 文件级＋被多方法共用(4) | — |
| `WildcardDrawer.Draw(Wildcard item, int quantity)` | `Shell/ItemDrawer.cs:260 DrawerWildcard / Wildcard()` | 注释 | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `WildcardDrawer.GetItemName(Wildcard item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `WildcardIconDrawer.Draw(Wildcard item, int quantity)` | `Shell/ItemDrawer.cs WildcardIcon()` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `WildcardIconDrawer.GetItemName(Wildcard item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `WindowHeaderWithBackButton.Initialize(string title, Action OnBackButtonPressed)` | `各窗标题 + ShowPreviousWindow 回调` | 仅类/描述(无方法) | — |
| `WindowHolder.OnDisable()` | `Shell/WindowHolder.cs（OnDisable/OnEnable 成对）` | 文件级(可补行号) | `CardPresentation/Shell/WindowHolder.cs:30` |
| `WindowTabBase.Awake()` | `Shell/WindowTabBase.cs（未写 Awake —— 原版也是空实现 `WindowTabBase.cs:60-62`）` | 函数中间＋被多方法共用(5) | `CardPresentation/Shell/WindowTabBase.cs:55(所在方法)` |
| `WindowTabBase.CloseTab()` | `Shell/WindowTabBase.cs:CloseTab（`SetActive(false)` → `OnClose()`；不清 Cu…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/WindowTabBase.cs:64` |
| `WindowTabBase.OnClose()` | `Shell/WindowTabBase.cs:OnClose（`public virtual`；`FriendsTab` 已改 `overr…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/WindowTabBase.cs:72` |
| `WindowTabBase.OnOpen()` | `Shell/WindowTabBase.cs:OnOpen（各页 override）` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/WindowTabBase.cs:32` |
| `WindowTabBase.TryOpenTab()` | `Shell/WindowTabBase.cs:TryOpenTab（先 SetActive(true)、再 OnOpen）` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/WindowTabBase.cs:55` |
| `WindowsManager.Init()` | `Shell/WindowsManager.cs Awake/EnsureHost` | 文件级(可补行号) | `CardPresentation/Shell/WindowsManager.cs:99` |
| `WindowsManager.Update()` | `Shell/PointerLayer.cs（输入）+ WindowsManager.TopWindow` | 文件级(可补行号) | `CardPresentation/Shell/PointerLayer.cs:384` |

## 战斗界面_HUD.tsv（A 423，问题 342）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `ActiveSkillDisplay.ClickDown()` | `Battle/SkillPanel.cs:910` | 函数中间 | `CardPresentation/Battle/SkillPanel.cs:906(所在方法)` |
| `ActiveSkillDisplay.ClickUp()` | `Battle/SkillPanel.cs:922 + BattleDriver.cs:14601` | 函数中间 | `CardPresentation/Battle/SkillPanel.cs:916(所在方法)` |
| `ActiveSkillDisplay.DisableUsableLight()` | `Battle/SkillPanel.cs:1522 ApplyColors` | 指向别的方法/字段 | `CardPresentation/Battle/SkillPanel.cs:1557` |
| `ActiveSkillDisplay.InitialSetup()` | `Battle/SkillPanel.cs:682 Build` | 指向别的方法/字段 | — |
| `ActiveSkillDisplay.OnTouchDown(Vector2 originalTouchPosition)` | `Battle/SkillPanel.cs:904 OnSkillPointerDown` | 注释 | `CardPresentation/Battle/SkillPanel.cs:906` |
| `ActiveSkillDisplay.OnTouchExit(Vector2 originalTouchPosition)` | `Battle/SkillPanel.cs:932` | 函数中间 | `CardPresentation/Battle/SkillPanel.cs:930(所在方法)` |
| `ActiveSkillDisplay.OnTouchUpAsButton(Vector2 originalTouchPosition)` | `Battle/SkillPanel.cs:922 + BattleDriver.cs:14601` | 函数中间 | `CardPresentation/Battle/SkillPanel.cs:916(所在方法)` |
| `ActiveSkillDisplay.PointerExit()` | `Battle/SkillPanel.cs:932` | 函数中间 | `CardPresentation/Battle/SkillPanel.cs:930(所在方法)` |
| `ActiveSkillDisplay.StopDisplayingSkillFromManager()` | `Battle/SkillPanel.cs:954 + BattleDriver.cs:14638` | 指向别的方法/字段 | — |
| `BasicCardUI.CardClicked()` | `BattleDriver 展示窗入口 + CardDisplayWindow.Show` | 仅类.方法(可补行号) | `CardPresentation/Battle/CardDisplayWindow.cs:1189` |
| `BasicCardUI.ChangedLanguage()` | `Core/CardText.cs（按语档取词）` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.ColliderBigSize(bool value)` | `Hand/CardInteraction.cs 几何命中矩形（无 Collider2D）` | 文件级＋被多方法共用(4) | — |
| `BasicCardUI.DisableCollider()` | `Hand/CardInteraction.cs 重排闸（0.2s）` | 文件级＋被多方法共用(4) | — |
| `BasicCardUI.FadeCard(float newAlpha, float timeToFade, float delay)` | `Core/CardFeel.cs / CardTween.cs` | 文件级 | — |
| `BasicCardUI.GetRarity()` | `Core/CardView.cs rarity` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.OnDescriptionShow()` | `无（战斗卡面不展开描述，与原版同）` | 间接指认 | — |
| `BasicCardUI.OnTouchDown(Vector2 originalTouchPosition)` | `Hand/CardInteraction.cs PressCandidate / BattleDriver BoardPress` | 文件级＋被多方法共用(4) | — |
| `BasicCardUI.OnTouchUpAsButton(Vector2 originalTouchPosition)` | `Hand/CardInteraction.cs Release + BoardPress` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Hand/CardInteraction.cs:1304` |
| `BasicCardUI.RefreshCardVisualEffects(CardTier cardTier)` | `Core/CardView.cs SetFace / 档位框` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Core/CardView.cs:2269` |
| `BasicCardUI.RemoveHealthAndAttack()` | `Core/CardView.cs 数值层显隐` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetAltHealth(int newHealth)` | `Core/CardView.cs 生命数字` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetCardAlpha(float newAlpha)` | `Core/CardView.cs α/材质` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetCardImage()` | `Core/CardArt.cs` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetCardImage(Sprite sprite)` | `Core/CardArt.cs` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetCardRarityStars()` | `Core/CardView.cs 无星形层（rarity 只喂框/高亮）` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetCardTextInHand(bool option)` | `Core/CardView.cs CardUse/手牌档` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetCardTextsExcludingStats(RawCardScript card)` | `Core/CardText.cs + CardView 文本层` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetDescription(string description)` | `Core/CardText.cs` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetEmptyRarityStars()` | `Core/CardView.cs 无星形层（等价于清空）` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.SetImageOverDraw(CardImageController.ImageInFrameSize imageInFrameSize…` | `Core/CardArt.cs OverDraw / CardView.ArtDrawSize` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetImageSprite(Sprite sprite, CardTypeOptions cardType)` | `Core/CardArt.cs` | 文件级＋被多方法共用(3) | — |
| `BasicCardUI.SetLayer(int layerNumber)` | `Core/CardView.cs:2222 SetLayer` | 注释 | `CardPresentation/Core/CardView.cs:2248` |
| `BasicCardUI.SetRawCardData(RawCardScript card, CardTier? tier, bool showLore, int …` | `Core/CardView.cs SetData（CardData/CardDef）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Core/CardView.cs:3266` |
| `BasicCardUI.SetupCollider(Action<GameObject> callback, bool mouseDownCallback, int…` | `Battle/BattleDriver 手搓命中 + uGUI 钮（无 Collider2D）` | 仅类/描述(无方法) | — |
| `BasicCardUI.SetupCreatedByText(string newText)` | `BattleDriver DisplayCreatedByText 区段` | 仅类/描述(无方法) | — |
| `BasicCardUI.SimplifiedOff()` | `Core/CardView CardUse 档` | 仅类/描述(无方法)＋被多方法共用(3) | — |
| `BasicCardUI.SimplifiedOn()` | `Core/CardView CardUse 档` | 仅类/描述(无方法)＋被多方法共用(3) | — |
| `BasicCardUI.TintCard(Color newColor, float time)` | `Core/CardHighlight.cs 整卡着色` | 文件级 | — |
| `BasicCardUI.ToggleCardBack(bool option)` | `Core/CardView.cs 正面/背面` | 文件级＋被多方法共用(10) | — |
| `BasicCardUI.ToggleOpenCardDisplayOnTouch(bool option)` | `BattleDriver OnCardTapped + CardInteraction 闸` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:12614` |
| `BasicCardUI.ToggleSimplifiedCardView(bool option, CardTextsInHandOptions cardsText…` | `Core/CardView CardUse 档` | 仅类/描述(无方法)＋被多方法共用(3) | — |
| `BasicCardUI.ToggleStats(bool activate)` | `Core/CardView 数值层` | 仅类/描述(无方法) | — |
| `BattleCameraSreenSize.DoLensShift(float desiredLensShiftY, bool instant)` | `Battle/BattleCameraSreenSize.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/BattleCameraSreenSize.cs:284` |
| `BattleCameraSreenSize.Initialize(bool instant)` | `Battle/BattleCameraSreenSize.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/BattleCameraSreenSize.cs:208` |
| `BattleCameraSreenSize.ResolutionHasChanged()` | `Battle/BattleCameraSreenSize.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/BattleCameraSreenSize.cs:184` |
| `BattleCameraSreenSize.Start()` | `Battle/BattleCameraSreenSize.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/BattleCameraSreenSize.cs:167` |
| `BattleCardManager.AddCardToDeck(CardScript card, List<CardScript> deck, int posInDeck)` | `BattleDriver 牌库 = 引擎 PlayerState.Deck（List）` | 仅类.方法(可补行号) | `RuleEngine/Core/PlayerState.cs:19` |
| `BattleCardManager.CreateCard(RawCardScript rawCardData, Transform parent, bool isPlayerY…` | `BattleDriver 建 CardView（Hand/Board）` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:1707` |
| `BattleCardManager.CreateDeck(List<RawCardScript> cardList, Transform parent, bool isPlay…` | `BattleDriver 牌堆视图` | 仅类/描述(无方法) | — |
| `BattleCardManager.GetAllCardsInGame()` | `BattleDriver _cards / Ctx 棋盘+手牌` | 仅类/描述(无方法) | — |
| `BattleCardManager.GetCardFromUniqueId(int uniqueId)` | `BattleDriver CardView.Inst 反查` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:1226` |
| `BattleCardManager.RegisterCardInPlay(CardScript card)` | `BattleDriver Board/CardView 注册` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:1707` |
| `BattleCardUI.BattleCardSetup(CardArmy mainArmy, bool isPlayer)` | `BattleDriver 卡背（CardbackFace）` | 仅类/描述(无方法) | — |
| `BattleCardUI.CardHandToBoardAnimationFinished()` | `BattleDriver CardHandToBoard 收尾` | 仅类/描述(无方法) | — |
| `BattleCardUI.ChangeCardToMinion(bool doAnim)` | `Core/CardView.cs 3D body + Board/BoardLayout.cs:141` | 注释 | `CardPresentation/Core/CardView.cs:121(所在方法)` |
| `BattleCardUI.ChangeCardToRemnant(bool toRemnant, Action onComplete)` | `Core/CardView.cs:2496 SetRemnantBody` | 注释 | `CardPresentation/Core/CardView.cs:2524` |
| `BattleCardUI.ChangeHighlightToSelectColor()` | `Core/CardHighlight.cs 选中色` | 文件级 | — |
| `BattleCardUI.ChangeToHighlightColor()` | `Core/CardHighlight.cs` | 文件级 | — |
| `BattleCardUI.ChangedLanguage()` | `Core/CardText.cs` | 文件级 | — |
| `BattleCardUI.CleanStatusAnims(bool clearAll)` | `Core/TraitFrames.cs Clear/Clean（两本字典）` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Core/TraitFrames.cs:929` |
| `BattleCardUI.CreateRemnantBody()` | `BattleDriver CreateRemnantBody + CardView.SetRemnantBody` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:2524` |
| `BattleCardUI.DamageReceived(int damageValue)` | `Battle/PopNumber.cs + BattleDriver` | 文件级 | — |
| `BattleCardUI.DelayedResetMaterial(float time)` | `BattleDriver 材质复位（异步补间）` | 仅类/描述(无方法) | — |
| `BattleCardUI.DestroyActivateAbilityAnim()` | `无（原版该件恒 null）` | 间接指认 | — |
| `BattleCardUI.DestroyCard()` | `BattleDriver 卡阵亡/消散链（timeToDissolveCard×2）` | 仅类/描述(无方法) | — |
| `BattleCardUI.DestroyRemnantBody()` | `Core/CardView.cs SetRemnantBody(false)` | 文件级(可补行号) | `CardPresentation/Core/CardView.cs:2524` |
| `BattleCardUI.DisplayCreatedByText(CardStateOptions newState)` | `BattleDriver DisplayCreatedByText` | 仅类/描述(无方法) | — |
| `BattleCardUI.DisplayStatusAnim(DefinedTrait trait, bool playSound)` | `Core/TraitFrames.cs DisplayStatus` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Core/TraitFrames.cs:790` |
| `BattleCardUI.DisplayTriggerAnim(DefinedTrait trait, bool playSound)` | `Core/TraitFrames.cs DisplayTrigger` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Core/TraitFrames.cs:883` |
| `BattleCardUI.FadeCardItems(float newAlpha, float timeToDissolve)` | `Core/CardFeel.cs` | 文件级 | — |
| `BattleCardUI.FinishMulligan()` | `Battle/MulliganPanel.cs + BattleDriver EndMulligan` | 文件级 | — |
| `BattleCardUI.GetSortingOrder()` | `Battle/HudCanvas.cs OrderZOf / CardView.Queue` | 文件级(可补行号) | `CardPresentation/Battle/HudCanvas.cs:445` |
| `BattleCardUI.GetTraitListForCardIcons(RawCardScript cardToDisplay)` | `Core/Badges.cs` | 文件级＋被多方法共用(3) | — |
| `BattleCardUI.HideExtraEffects()` | `Core/CardView.cs:2411-2416 SetFace 非场上支 + :7390 SetCanAct；伤害走 Battle/P…` | 函数中间 | `CardPresentation/Core/CardView.cs:2269` |
| `BattleCardUI.IsEnemySecret(CardScript cardScript)` | `BattleDriver 敌方手牌恒卡背` | 仅类/描述(无方法) | — |
| `BattleCardUI.PlayBackToHandAnimation()` | `BattleDriver 回手补间` | 仅类/描述(无方法) | — |
| `BattleCardUI.RemoveStatusAnim(DefinedTrait trait, int param)` | `Core/TraitFrames.cs RemoveStatus` | 文件级＋被多方法共用(5) | — |
| `BattleCardUI.RemoveSummonSickness()` | `BattleDriver.cs:24987 区段` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:24981(所在方法)` |
| `BattleCardUI.RestoreOriginalMaterial()` | `BattleDriver 材质复位` | 仅类/描述(无方法) | — |
| `BattleCardUI.SetAsMulliganSelected(bool isSelected)` | `Battle/MulliganPanel.cs 标记` | 文件级 | — |
| `BattleCardUI.SetCardImageTo3DBase()` | `BattleDriver SetCardImageTo3DBase + Card3DAnimationController` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:23245` |
| `BattleCardUI.SetCardMaterial(Material material, bool alsoSetCardImage)` | `BattleDriver 材质切换` | 仅类/描述(无方法) | — |
| `BattleCardUI.SetCardMaterialByCardTier()` | `Core/CardView 档位框 + 3D MatCap` | 仅类/描述(无方法) | — |
| `BattleCardUI.SetEnemySecret(CardScript cardScript)` | `无（敌方陷阱/密令的卡面文字我们不出）` | 间接指认 | — |
| `BattleCardUI.SetObjectVisibility(CardStateOptions newState)` | `BattleDriver 状态→可见性 + Core/CardView` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:1707` |
| `BattleCardUI.SetRawCardData(CardScript cardScript)` | `BattleDriver 建卡 + CardView.SetData` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:3266` |
| `BattleCardUI.SetSortingLayer(string layerName)` | `Battle/HudCanvas.cs SortingLayer 常量` | 文件级 | — |
| `BattleCardUI.SetSortingOrder(int newOrder)` | `CardView.SetQueue + HudCanvas.Add` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:1648` |
| `BattleCardUI.SetVanguardStealth(bool activeStealth, bool waitForFlicker)` | `无（原版该协程空转）` | 间接指认 | — |
| `BattleCardUI.SetupStatusFrame(GameObject newFrame)` | `Core/TraitFrames.cs（实例化到 effectsAnchor）` | 文件级＋被多方法共用(5) | — |
| `BattleCardUI.ShouldShowCardback(CardStateOptions newState)` | `BattleDriver 状态→卡背` | 仅类/描述(无方法) | — |
| `BattleCardUI.ShowAvailableTargets()` | `BattleDriver ShowAvailableTargets` | 仅类/描述(无方法) | — |
| `BattleCardUI.ShowBoardObjects(bool show)` | `Core/CardView 3D↔2D` | 仅类/描述(无方法) | — |
| `BattleCardUI.ShowEphemeral()` | `BattleDriver.cs:25661 ShowEphemeral(Ctx.IsEphemeral)` | 注释 | `CardPresentation/Battle/BattleDriver.cs:25639(所在方法)` |
| `BattleCardUI.ShowPlaceholderSprite()` | `Core/CardArt.cs 占位图` | 文件级 | — |
| `BattleCardUI.ShowStealth()` | `Core/Badges.cs:420 stealth 走词条图标` | 函数中间 | `CardPresentation/Core/Badges.cs:374(所在方法)` |
| `BattleCardUI.StartSummonSicknessAnim()` | `BattleDriver.cs:14152-14160 displaySummonSickness` | 注释 | `CardPresentation/Battle/BattleDriver.cs:14145(所在方法)` |
| `BattleCardUI.StopAnimsInPlay()` | `BattleDriver/AnimFXController 停止` | 仅类/描述(无方法) | — |
| `BattleCardUI.StopReflected()` | `无（原版框恒 null）` | 间接指认 | — |
| `BattleCardUI.StopShowingStealth()` | `同上（原版框恒 null，SetVanguardStealth 亦空转）` | 仅类/描述(无方法) | — |
| `BattleCardUI.StopSummonSicknessAnim()` | `BattleDriver.cs:24987 区段` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:24981(所在方法)` |
| `BattleCardUI.ToggleBody3D(bool optionBody, bool? optionEffectAnchor)` | `Core/CardView.cs 3D body / BoardLayout` | 文件级 | — |
| `BattleCardUI.ToggleFlying(bool option)` | `Battle/FlyingCardController.cs + BattleDriver SyncUnitFlying` | 文件级 | — |
| `BattleCardUI.ToggleOverrideSorting(bool option)` | `CardView.SetQueue / 子画布` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:1648` |
| `BattleCardUI.ToggleStats(bool activate, bool activateArmour)` | `Core/CardView 数值层` | 仅类/描述(无方法) | — |
| `BattleCardUI.ToggleTraitIcons(bool option)` | `Core/Badges.cs` | 文件级＋被多方法共用(3) | — |
| `BattleCardUI.UpdateCardback(bool isPlayer)` | `BattleDriver 卡背（友/敌两套）` | 仅类/描述(无方法) | — |
| `BattleCardUI.UpdateRaceWithEffects(List<CardEffect> activeEffects)` | `Core/CardText.cs + CardView 种族行` | 文件级 | — |
| `BattleCardUI.UpdateTraitIcons()` | `Core/Badges.cs 刷新` | 文件级＋被多方法共用(3) | — |
| `BattleErrorUIManager.UpdateReconnectWindow(int seconds)` | `Net/NetBattle.cs:690-693 · Net/NetRuntime.cs:136-146` | 注释 | `CardPresentation/Net/NetBattle.cs:664(所在方法)` |
| `BattleHud.AlliancePanelOpenButton()` | `BattleDriver.cs:5627 ClickAllianceHud` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5743` |
| `BattleHud.Awake()` | `BattleDriver BuildHud（无单例，HUD 归驱动）` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:27037` |
| `BattleHud.CloseAction()` | `BattleDriver.cs:5741 HandleSettingsEsc` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5857` |
| `BattleHud.DisplayMultiCardView(List<RawCardScript> cardsToDisplay, string window…` | `Battle/MultiCardDisplay.cs + BattleDriver.cs:2216` | 行号越界 | — |
| `BattleHud.DisplayOffensiveCards()` | `BattleDriver.cs:5814 HandleOffensiveButton` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5930` |
| `BattleHud.DisplayOvertime()` | `BattleDriver.cs:2024 加时 splash` | 注释 | `CardPresentation/Battle/BattleDriver.cs:1990(所在方法)` |
| `BattleHud.DoResetCameraZoom()` | `BattleDriver 复位相机钮 → CombatCameraZoom` | 仅类/描述(无方法) | — |
| `BattleHud.GetUIAnchor(PositionOption desiredPosition, bool isPlayer)` | `BattleDriver.cs:3765 UiAnimAnchor + Battle/HudCanvas.cs:744` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:3881` |
| `BattleHud.Initialize()` | `BattleDriver BuildHud / BuildHudExtras` | 仅类/描述(无方法) | — |
| `BattleHud.InitializeProfile()` | `BattleDriver.cs:10638 SetTitle/SetAvatar` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:10966` |
| `BattleHud.IsSettingsPopupOpen()` | `BattleDriver.cs:5747` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:5743(所在方法)` |
| `BattleHud.OffensiveButtonClicked()` | `BattleDriver.cs:5814` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5799(所在方法)` |
| `BattleHud.OffensiveButtonTransform()` | `BattleDriver.cs:2173 _offensiveBtn` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleHud.OpenSettingsPopup()` | `BattleDriver.cs:5572` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:5571(所在方法)` |
| `BattleHud.PauseButtonClicked()` | `BattleDriver.cs:7826 OnReplayButtonClicked` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:8143` |
| `BattleHud.PlayButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `BattleHud.ReplayButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `BattleHud.SetOffensiveButtonVisible(EnviromentalEffectCardsSO enviromentalEffect…` | `BattleDriver.cs:4945 ApplyOffensiveButtonVisibility` | 注释 | `CardPresentation/Battle/BattleDriver.cs:9468` |
| `BattleHud.SetProfileInfo(DeckAndWarlordData rawData, PlayerProfileUIController p…` | `BattleDriver.cs:10638-10664, 27849` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:10630(所在方法)` |
| `BattleHud.SettingsButtonOnClick()` | `BattleDriver.cs:5572` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:5571(所在方法)` |
| `BattleHud.Setup()` | `Battle/ReplayBar.cs + BattleDriver.cs:4862` | 行号越界 | `CardPresentation/Battle/ReplayBar.cs:330` |
| `BattleHud.StepButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `BattleHud.ToggleResetAutoCameraZoom(bool option)` | `BattleDriver.cs:2324 _fxCameraReset` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleHud.ToggleWithAnimation(bool option, bool useAnimation, bool animHand)` | `Battle/BattleHudAnim.cs + BattleDriver.cs:1683` | 行号越界 | — |
| `BattleHud.TryGetAnchor(PositionOption desiredPosition, bool isPlayer)` | `BattleDriver.cs:3765` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:3751(所在方法)` |
| `BattleHud.TryOpenSettings()` | `BattleDriver.cs:5572` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:5571(所在方法)` |
| `BattleHud.Update()` | `BattleDriver.cs:5741` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13562` |
| `BattleHud.isEmptyOffensiveCard(EnviromentalEffectCardsSO enviromentalEffectCards…` | `BattleDriver.cs:2170 + ApplyOffensiveButtonVisibility` | 注释 | `CardPresentation/Battle/BattleDriver.cs:9468` |
| `BattleScoreIcon.Check(bool state)` | `Battle/BattleDriver.cs:30428` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:30427(所在方法)` |
| `BattleScoreIcon.Config(HealthThresholdData data)` | `Battle/BattleDriver.cs:30410-30428` | 注释 | `CardPresentation/Battle/BattleDriver.cs:30403(所在方法)` |
| `BattleScoreManager.CheckThresholds(EnemyHealthChangeSignal signal)` | `BattleDriver.cs:2274-2310 _foeSkullCount 事件驱动` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleScoreManager.GetEnemySkullCount(int ownLife)` | `BattleDriver.cs:2280（DeckRules.SkullsFor 逐字等价）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleScoreManager.GetSkullCount()` | `BattleDriver.cs:2305 _foeSkullCount` | 指向别的方法/字段 | — |
| `BattleScoreManager.GrantSkull(int skullCount)` | `BattleDriver.cs:2274-2310` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleScoreManager.Initialize()` | `BattleDriver BuildHud + DeckRules.SkullThresholds` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:123` |
| `BattleScoreUiManager.Initialize(HealthThresholdData[] milestones)` | `BattleDriver.cs:2475 _skullIcon/_skullScore` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2450(所在方法)` |
| `BattleScoreUiManager.OnReachMilestone(int milestone)` | `Battle/BattleDriver.cs:30428` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:30427(所在方法)` |
| `BattleScoreUiManager.SetMilestones(IEnumerable<HealthThresholdData> milestones)` | `DeckRules.SkullThresholds` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:123` |
| `BattleScoreUiManager.UpdateMilestonesCount(int milestoneIndex)` | `BattleDriver.cs:2274-2310` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleScorer.Initialize()` | `BattleDriver.cs:2274-2310` | 注释 | `CardPresentation/Battle/BattleDriver.cs:2139(所在方法)` |
| `BattleSettingsWindow.Awake()` | `Battle/SettingsPanel.cs:1051-1180 Build` | 数据表项(需确认是否纯查表) | — |
| `BattleSettingsWindow.ClickSkipTutorial()` | `BattleDriver SkipTutorialFromSettings` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:17170` |
| `BattleSettingsWindow.CloseBackgroundButton()` | `Battle/SettingsPanel.cs:1321 OnCloseClicked` | 函数中间 | `CardPresentation/Battle/SettingsPanel.cs:1328` |
| `BattleSettingsWindow.CloseButtonOnClick()` | `Battle/SettingsPanel.cs:1321` | 函数中间 | `CardPresentation/Battle/SettingsPanel.cs:1316(所在方法)` |
| `BattleSettingsWindow.CloseWindow()` | `BattleDriver.cs:5741 HandleSettingsEsc + SettingsPanel.Hide` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5857` |
| `BattleSettingsWindow.ESCPressed()` | `BattleDriver.cs:5741` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5735(所在方法)` |
| `BattleSettingsWindow.OnAutoZoomChanged(bool toggleValue)` | `Battle/SettingsPanel.cs:1325 ToggleAutoZoomFromPanel` | 函数中间 | `CardPresentation/Battle/SettingsPanel.cs:2378` |
| `BattleSettingsWindow.OnMuteChatChanged(bool toggleValue)` | `Battle/SettingsPanel.cs:1328 ToggleMuteChatFromPanel + ChatMuted` | 指向别的方法/字段 | `CardPresentation/Battle/SettingsPanel.cs:2201` |
| `BattleSettingsWindow.RegisterToClose(Action closeAction)` | `Battle/SettingsPanel.cs OnClose/CloseAction` | 文件级 | — |
| `BattleSettingsWindow.ResignButtonOnClick()` | `Battle/SettingsPanel.cs:1297 OnResign → BattleDriver.Forfeit` | 指向别的方法/字段 | `CardPresentation/Battle/SettingsPanel.cs:105` |
| `BattleSettingsWindow.SkipTutorialButtonOnClick()` | `BattleDriver SkipTutorialFromSettings` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:17170` |
| `BattleTipController.AreTipsOnQueue()` | `BattleDriver.cs:15248 _tipQueue.Count` | 注释 | `CardPresentation/Battle/BattleDriver.cs:15214(所在方法)` |
| `BattleTipController.AreTooMuchTips()` | `BattleDriver.cs:1911 TipQueueMax=3` | 注释 | `CardPresentation/Battle/BattleDriver.cs:1904` |
| `BattleTipController.Awake()` | `BattleDriver BuildHud + :1878 _tipText/_tipBg` | 仅类/描述(无方法) | — |
| `BattleTipController.DisplayTip(bool opaqueBackground, bool bgColorRed)` | `BattleDriver.cs:15266 TickTipBanner（TipDuration=1.7）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:15604` |
| `BattleTipController.NotifyCantDoAction(string tipText, bool bgColorRed)` | `BattleDriver.cs:7419 NotifyCantDoAction` | 注释 | `CardPresentation/Battle/BattleDriver.cs:7535` |
| `BattleTipController.SetBGColor()` | `BattleDriver.cs:1919-1923 TipBgColor / TipBgOpaqueColor（0.6 / 1.0 实读）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:1922` |
| `BattleTipController.ShowHeadsUpMessage(string tipText, bool opaqueBackground, bool bgColor…` | `BattleDriver.cs:15220 SetHint / :15225 SetHintOpaque` | 注释 | `CardPresentation/Battle/BattleDriver.cs:280` |
| `BattleTipController.TipSpamming(string tipText)` | `BattleDriver.cs:15231 去重（比队尾）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:15214(所在方法)` |
| `CardDisplayAttackTypeButton.DisableTemporary()` | `Battle/AttackSelector.cs:537 TickHover + :550 _safeLeft` | 被多方法共用 | — |
| `CardDisplayAttackTypeButton.DoPointerOverButtonActions()` | `Battle/AttackSelector.cs:537 TickHover + :496 MarkHoverPicked + :508 P…` | 被多方法共用 | — |
| `CardDisplayAttackTypeButton.Initialize(AttackTypesButtonsController registerController)` | `Battle/AttackSelector.cs:819 Build / :721 UseHost` | 指向别的方法/字段 | `CardPresentation/Battle/AttackSelector.cs:721` |
| `CardDisplayAttackTypeButton.SetButtonValue(int customValue)` | `Battle/AttackSelector.cs:1470-1530 · Battle/BattleDriver.cs:14430` | 函数中间 | `CardPresentation/Battle/AttackSelector.cs:1465(所在方法)` |
| `CardDisplayAttackTypeButton.StartSafeTouch()` | `Battle/AttackSelector.cs:537 TickHover（_safeLeft 计时）` | 被多方法共用 | — |
| `CardDisplayWindow.AddCardsListeners()` | `Battle/CardDisplayWindow.cs Build（uGUI 钮）` | 文件级 | — |
| `CardDisplayWindow.CardOverdrawHandle(RawCardScript card, int cardUIIndex)` | `Battle/CardDisplayWindow.cs:1458-1464 · :1604-1605 · Core/CardView.cs:…` | 注释 | `CardPresentation/Battle/CardDisplayWindow.cs:1434(所在方法)` |
| `CardDisplayWindow.CardSwapFinished(BasicCardUI newCardInFront)` | `Battle/CardDisplayWindow.cs:1565 SwapToFront + :1667 RefreshLore` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardDisplayWindow.cs:1569` |
| `CardDisplayWindow.CardUIClicked()` | `Battle/BattleDriver.cs:12609（HitSlot → `_cardDisplay.SwapToFront(slot)…` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:12581(所在方法)` |
| `CardDisplayWindow.ChangeCardPosition(BasicCardUI cardToFront)` | `Battle/CardDisplayWindow.cs:1565` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardDisplayWindow.cs:1540(所在方法)` |
| `CardDisplayWindow.DisableAllCards()` | `Battle/CardDisplayWindow.cs:1224（Show 先 HideCore）· :1315-1340（全拆）` | 函数中间 | `CardPresentation/Battle/CardDisplayWindow.cs:1315` |
| `CardDisplayWindow.DisplayCardEffects(CardScript thisCard)` | `BattleDriver 传 EffectRow → CardDisplayWindow.cs:1221` | 指向别的方法/字段 | `CardPresentation/Battle/CardDisplayWindow.cs:832` |
| `CardDisplayWindow.GetEffectsToDisplay(CardScript thisCard)` | `Battle/CardDisplayWindow.cs:851-878（RowsOf）· Battle/BattleDriver.cs:12…` | 指向别的方法/字段 | — |
| `CardDisplayWindow.InitializeCards(RawCardScript card, CardTier tier, int warlordHealthMo…` | `Battle/CardDisplayWindow.cs:1221` | 指向别的方法/字段 | — |
| `CardDisplayWindow.InitializeRelatedCards(RawCardScript card)` | `Battle/CardDisplayWindow.cs:1407 RelatedCards.Find` | 函数中间 | `CardPresentation/Battle/CardDisplayWindow.cs:1402(所在方法)` |
| `CardDisplayWindow.Open()` | `Battle/CardDisplayWindow.cs:414 Create / :1206 Show` | 指向别的方法/字段 | `CardPresentation/Battle/CardDisplayWindow.cs:1189` |
| `CardDisplayWindow.RelatedCardUIClicked(BasicCardUI cardClicked)` | `Battle/CardDisplayWindow.cs:1565` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardDisplayWindow.cs:1540(所在方法)` |
| `CardDisplayWindow.ResetOrder()` | `Battle/CardDisplayWindow.cs:1315-1340 · :1419-1470` | 指向别的方法/字段 | — |
| `CardDisplayWindow.SetCardLore(BasicCardUI card)` | `Battle/CardDisplayWindow.cs:1667 RefreshLore` | 注释 | `CardPresentation/Battle/CardDisplayWindow.cs:1676` |
| `CardDisplayWindow.ShowBattleCard(CardScript card, int warlordHealthModifier)` | `BattleDriver 展示窗 → CardDisplayWindow.cs:1206` | 指向别的方法/字段 | — |
| `CardDisplayWindow.TryInitializeCard(RawCardScript relatedCard, int index)` | `Battle/CardDisplayWindow.cs:1406-1410` | 注释 | `CardPresentation/Battle/CardDisplayWindow.cs:1402(所在方法)` |
| `CemeteryLogGroup.ClearActions()` | `BattleDriver.cs:11058 _logEntries.Clear()` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11035(所在方法)` |
| `CemeteryLogGroup.CreateCemeteryCard(RawCardScript cardData, bool isPlayer, int activeAb…` | `Battle/BattleDriver.cs:11612（TickLogCard 单张悬停卡）/ 11636（建卡）` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:11596(所在方法)` |
| `CemeteryLogGroup.CreateMainCard(RawCardScript cardData, bool isPlayer, int activeAbilit…` | `Battle/BattleDriver.cs:11612` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:11596(所在方法)` |
| `CemeteryLogGroup.CreateTargetCard(int index, RawCardScript cardData, bool isPlayer, int…` | `Battle/BattleDriver.cs:11612` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:11596(所在方法)` |
| `CemeteryLogGroup.DisplayAction(CemeteryAction cemeteryAction)` | `Battle/BattleDriver.cs:11150（RefreshBattleLog 一动作一行）+ 11612` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryLogGroup.GetCemeteryCardLocalPos(int index)` | `Battle/BattleLogPanel.cs:710（RowCardLocalPos）` | 指向别的方法/字段 | — |
| `CemeteryManager.AddAbilityActionToCemetery(BattleAction battleAction)` | `BattleDriver.cs:11188 case Ability` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.AddActionToCemetery(CemeteryAction newAction)` | `BattleDriver.cs:11058-11304 → :11304 SetEntries` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11035(所在方法)` |
| `CemeteryManager.AddAttackActionToCemetery(BattleAction battleAction, DamageInfo attack…` | `BattleDriver.cs:11166 case Attack` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.AddCardToCemetery(CardScript card)` | `RuleEngine/Core/PlayerState.cs:21（Discard）+ Core/EffectResolver.cs:256…` | 指向别的方法/字段 | — |
| `CemeteryManager.AddDisplayedCardActionToCemetery(BattleAction battleAction)` | `BattleDriver.cs:11228 case Display` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.AddExecuteSecretOrderAction(CardScript actingCard, CardScript enchantm…` | `BattleDriver.cs:11272 case SecretOrder` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11245(所在方法)` |
| `CemeteryManager.AddExitAmbushActionToCemetery(CardScript ambushCard)` | `BattleDriver.cs:11254 case AmbushExit` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11245(所在方法)` |
| `CemeteryManager.AddPlayCardAction(BattleAction battleAction)` | `BattleDriver.cs:11138 case Play` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.AddPlayDropSupplyAction(RawCardScript dropSupply, bool isPlayer)` | `Battle/BattleDriver.cs:11380（EvtKind.Play 支）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11245(所在方法)` |
| `CemeteryManager.AddWaystoneActionToCemetery(BattleAction battleAction)` | `BattleDriver.cs:11197 case CollectWaystone` | 指向别的方法/字段 | — |
| `CemeteryManager.ChangedLanguage()` | `BattleDriver.cs:11058-11304 每次开面板重建` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11035(所在方法)` |
| `CemeteryManager.ClickCardLink()` | `BattleDriver.cs:11526 TryClickLogRow + :11455 展示` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11834` |
| `CemeteryManager.DisplayCard(RawCardScript rawCard, string createdByText)` | `BattleDriver.cs:11455 悬停/点击展示卡` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11440(所在方法)` |
| `CemeteryManager.DisplayCardTraits(RawCardScript card)` | `—（原版空方法，无落点需要）` | 间接指认 | — |
| `CemeteryManager.DisplayCemeteryActions()` | `BattleLogPanel.SetEntries + BattleDriver.cs:11304` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11245(所在方法)` |
| `CemeteryManager.GetActionText(CemeteryAction cemeteryAction, int actionIndex)` | `BattleDriver.cs:11138-11285` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.GetActionWithParams(string term, CemeteryAction cemeteryAction, int ac…` | `BattleDriver 同一段` | 仅类/描述(无方法) | — |
| `CemeteryManager.GetCardsPlayed(CardScript thisCard, CardScript actingCard, CardScript …` | `RuleEngine/Core/BattleContext.cs:1305（PlayedCards 流水表）+ Core/EffectRes…` | 指向别的方法/字段 | — |
| `CemeteryManager.GetCemeterySize()` | `BattleDriver.cs:11117（取最近 10 条）` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11109(所在方法)` |
| `CemeteryManager.GetFirstParamText(CemeteryAction cemeteryAction, int actionIndex)` | `BattleDriver CemeteryAmbushedTroopTerm` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleDriver.cs:11213` |
| `CemeteryManager.HideCemeteryLogBtn()` | `BattleDriver.cs:5609 ClickCemeteryHud 显隐` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:5725` |
| `CemeteryManager.SetEmptyText()` | `—（13/13 场该字段为 null，无落点需要）` | 间接指认 | — |
| `CemeteryManager.Setup()` | `BattleLogPanel.Create + BattleDriver BuildHud` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleLogPanel.cs:775` |
| `CemeteryManager.SetupCommonElements(BattleAction battleAction, ref CemeteryAction newC…` | `BattleDriver 行构造 + Entry.Side` | 仅类.方法(解析不到) | — |
| `CemeteryManager.ShowCemeteryLogBtn()` | `BattleDriver.cs:5609 + :1699` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:5571(所在方法)` |
| `CemeteryManager.StopDisplayingCemetery()` | `BattleLogPanel.Hide + BattleDriver` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleLogPanel.cs:1120` |
| `CemeteryManager.Update()` | `BattleLogPanel.LateUpdate + BattleDriver 每帧` | 仅类.方法(可补行号) | `CardPresentation/Battle/BattleLogPanel.cs:674` |
| `ChooseCardMenu.Awake()` | `Battle/CardChoicePanel.cs:313 _picked/_cards` | 指向别的方法/字段 | — |
| `ChooseCardMenu.ClickChooseDone()` | `Battle/CardChoicePanel.cs:625 OnUguiDone + BattleDriver.cs:10463` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:626` |
| `ChooseCardMenu.CreateCardUI(RawCardScript card, int index, int totalCards, bool isEnv…` | `Battle/CardChoicePanel.cs:404 Build (CardView.Create)` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardChoicePanel.cs:684` |
| `ChooseCardMenu.CreateDisplayCard(RawCardScript card, int index, int totalCards, bool …` | `Battle/CardChoicePanel.cs:404 + :799 BuildFeedback` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardChoicePanel.cs:821` |
| `ChooseCardMenu.GetTittleText(string uniqueId)` | `Battle/ChoosePanel.cs:218 + BattleDriver.cs:10319` | 指向别的方法/字段 | — |
| `ChooseCardMenu.OnDisable()` | `—（uGUI 生命周期，无落点需要）` | 间接指认 | — |
| `ChooseCardMenu.OnEnable()` | `Battle/CardChoicePanel.cs:625` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:609(所在方法)` |
| `ChooseCardMenu.ProcessChooseDone()` | `BattleDriver.cs:10463 OnChooseDone` | 注释 | `CardPresentation/Battle/BattleDriver.cs:10769` |
| `ChooseCardMenu.SelectCard(BasicCardUI card)` | `Battle/CardChoicePanel.cs:902 TogglePick(Single)` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:924` |
| `ChooseCardMenu.SetUpTitleText(string uniqueId)` | `BattleDriver.cs:10319 / ChoosePanel.cs:218` | 注释 | `CardPresentation/Battle/BattleDriver.cs:10261(所在方法)` |
| `ChooseCardMenu.SetupChooseCardsUi(List<RawCardScript> chooseCards, bool isEnviromenta…` | `Battle/CardChoicePanel.cs:404 + :662 Open` | 注释＋被多方法共用(3) | `CardPresentation/Battle/CardChoicePanel.cs:377(所在方法)` |
| `ChooseCardMenu.ShowChooseCardElements(bool showY)` | `Battle/CardChoicePanel.cs:662 Open / Close + ShadeAlpha 0.8` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:246` |
| `ChooseCardMenu.ShowChooseCards(bool showY)` | `Battle/CardChoicePanel.cs LayOutCards/Close` | 文件级(可补行号) | `CardPresentation/Battle/CardChoicePanel.cs:747` |
| `ChooseCardMenu.ToogleChooseMenuVisibility()` | `Battle/CardChoicePanel.cs:633 OnUguiEye` | 注释 | `CardPresentation/Battle/CardChoicePanel.cs:655` |
| `ChooseCardMenu.Update()` | `Battle/CardChoicePanel.cs Update（2026-10-25 本轮补）` | 文件级(可补行号) | `CardPresentation/Battle/CardChoicePanel.cs:645` |
| `ClockManager.ChangedLanguage()` | `BattleDriver.cs:13730 UpdateClockLabel` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:14060` |
| `ClockManager.ClockRunning()` | `BattleDriver.cs:13617 TickClock 前置` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13949` |
| `ClockManager.CreateCountdownAnim()` | `BattleDriver.cs:13537 PlayCountdownAnim` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13865` |
| `ClockManager.EndCountdown()` | `BattleDriver.cs:13689-13700` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:13562(所在方法)` |
| `ClockManager.EndTurn(bool timeOutFlag)` | `BattleDriver.cs:13699 EndPlayerTurn(fromUi, timeOut)` | 注释 | `CardPresentation/Battle/BattleDriver.cs:15763` |
| `ClockManager.EndTurnClick()` | `BattleDriver.cs:5640 ClickEndTurnHud / :13699` | 注释 | `CardPresentation/Battle/BattleDriver.cs:5759` |
| `ClockManager.GetTotalTime()` | `BattleDriver.cs:2699 TurnSecondsForThisMatch` | 指向别的方法/字段 | — |
| `ClockManager.LogPlayerAction(BattleAction battleAction)` | `BattleDriver.cs:315 _actionsThisTurn++` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:302(所在方法)` |
| `ClockManager.SetEndTurnText(bool isPlayer)` | `BattleDriver.cs:13730 UpdateClockLabel` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:14060` |
| `ClockManager.SetEnemyTurnUI()` | `BattleDriver.cs:13730 + ClickEndTurnHud 闸` | 注释＋被多方法共用(3) | `CardPresentation/Battle/BattleDriver.cs:5759` |
| `ClockManager.SetNoActionsUi()` | `BattleDriver.cs:16874 RunOutOfActions` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:16867(所在方法)` |
| `ClockManager.SetStartUi()` | `BattleDriver.cs:13514 ResetClock` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13842` |
| `ClockManager.ShouldUpdateSecondsPassed(float secondsPassed, float secondsPassedReal…` | `Battle/BattleDriver.cs:13962（UpdateClockLabel，每帧 CeilToInt 写整数秒）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13949(所在方法)` |
| `ClockManager.StartCountdown()` | `BattleDriver.cs:13655 倒计时翻转支` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13562(所在方法)` |
| `ClockManager.StartTimer()` | `BattleDriver.cs:13514 ResetClock` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13842` |
| `ClockManager.Update()` | `BattleDriver.cs:13617 TickClock` | 注释 | `CardPresentation/Battle/BattleDriver.cs:13562` |
| `CombatAutoZoom.ForceRefresh()` | `Battle/CombatAutoZoom.cs:214 ForceRefresh` | 函数中间 | `CardPresentation/Battle/CombatAutoZoom.cs:266` |
| `CombatAutoZoom.Initialize()` | `Battle/CombatAutoZoom.cs Initialize` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:250` |
| `CombatAutoZoom.OnDisable()` | `Battle/CombatAutoZoom.cs OnDisable` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:313` |
| `CombatAutoZoom.OnEnable()` | `Battle/CombatAutoZoom.cs OnEnable` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:310` |
| `CombatAutoZoom.OnMinionNumberChanged(int numberOfMinionsInSide, bool isPLayer)` | `Battle/CombatAutoZoom.cs 回调` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:342` |
| `CombatAutoZoom.ResetCameraZoomUIAction()` | `Battle/CombatAutoZoom.cs ResetCameraZoomUIAction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:266` |
| `CombatAutoZoom.SetZoomLevel(int numberOfMinionsInSide, bool force)` | `Battle/CombatAutoZoom.cs SetZoomLevel` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/CombatAutoZoom.cs:361` |
| `CombatCameraZoom.ApplyZoom(Vector2 drag)` | `Battle/CombatCameraZoom.cs:851 区段 ApplyZoom` | 函数中间 | `CardPresentation/Battle/CombatCameraZoom.cs:848(所在方法)` |
| `CombatCameraZoom.CorrectLensShift(Vector2 desiredLensShift, Vector2 correction)` | `Battle/CombatCameraZoom.cs:881 区段内的 CorrectLensShift` | 指向别的方法/字段 | `CardPresentation/Battle/CombatCameraZoom.cs:892` |
| `CombatCameraZoom.GetCameraFrustumWorldBoundsWithShift(Vector2 desiredShift)` | `Battle/CombatCameraZoom.cs:883 起` | 指向别的方法/字段 | `CardPresentation/Battle/CombatCameraZoom.cs:918` |
| `CombatCameraZoom.GetWorldPositionUnderMouse()` | `Battle/CombatCameraZoom.cs PointerOverUi + 同名段` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/CombatCameraZoom.cs:728` |
| `CombatCameraZoom.GetWorldPositionViewportSpace(Vector2 viewportPoint)` | `Battle/CombatCameraZoom.cs 同段` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/CombatCameraZoom.cs:732` |
| `CombatCameraZoom.OnCameraSensorSizeChanged(Vector2 newSensorSize)` | `Battle/CombatCameraZoom.cs 订阅回调` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/CombatCameraZoom.cs:972` |
| `CombatCameraZoom.OnCameraShiftChanged(Vector2 newLensShift)` | `Battle/CombatCameraZoom.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Battle/CombatCameraZoom.cs:968` |
| `EndBattleDoors.ShowRewards(IPlayEvent gameMode, EndMatchResult grantedResult, int sku…` | `Battle/EndPanel.cs:663-667（0 个藏整行、只亮前 N 个）` | 函数中间 | `CardPresentation/Battle/EndPanel.cs:613(所在方法)` |
| `EverguildTooltipManager.Awake()` | `Core/Tooltip.cs:252 Build` | 指向别的方法/字段 | — |
| `EverguildTooltipManager.DisposeTooltip()` | `Core/Tooltip.cs:336 Hide / :344 FinishFade` | 函数中间 | `CardPresentation/Core/Tooltip.cs:392` |
| `EverguildTooltipManager.Hide()` | `Core/Tooltip.cs:336` | 函数中间 | `CardPresentation/Core/Tooltip.cs:392` |
| `EverguildTooltipManager.ShowTraitTooltip(EverguildTraitTooltipData data, GameObject toolTipTri…` | `Core/Tooltip.cs:653（LayoutTrait：75×75 图标+标题+正文）+ BattleDriver.cs:30954…` | 注释 | `CardPresentation/Core/Tooltip.cs:638(所在方法)` |
| `FlyingCardController.Awake()` | `Battle/FlyingCardController.cs:99 构造 + :113 SetHome` | 指向别的方法/字段 | `CardPresentation/Battle/FlyingCardController.cs:113` |
| `FlyingCardController.FlyingState()` | `Battle/FlyingCardController.cs:189 Update` | 指向别的方法/字段 | — |
| `FlyingCardController.InTransitionToFlyState()` | `Battle/FlyingCardController.cs State` | 文件级(可补行号) | `CardPresentation/Battle/FlyingCardController.cs:105` |
| `FlyingCardController.InTransitionToLandState()` | `Battle/FlyingCardController.cs State` | 文件级(可补行号) | `CardPresentation/Battle/FlyingCardController.cs:105` |
| `FlyingCardController.OnDisable()` | `Battle/FlyingCardController.cs:135（ChangeState(Landed) 复位）· Battle/Bat…` | 指向别的方法/字段 | — |
| `HoverManager.Awake()` | `Core/Tooltip.cs:231（Ensure）/ :252（Build）` | 指向别的方法/字段 | — |
| `HoverManager.CreateMiniTip()` | `Core/Tooltip.cs:252-267（Build）` | 指向别的方法/字段 | — |
| `HoverManager.DisplayTip()` | `Core/Tooltip.cs:307-337（Show）` | 指向别的方法/字段 | — |
| `HoverManager.OnMouseEnter()` | `Battle/BattleDriver.cs:30904-30939 / 30954` | 注释 | `CardPresentation/Battle/BattleDriver.cs:30883(所在方法)` |
| `HoverManager.OnMouseExit()` | `Core/Tooltip.cs:374（Hide→0.3s 淡出）` | 注释 | `CardPresentation/Core/Tooltip.cs:372(所在方法)` |
| `HoverManager.OnMouseOver()` | `Core/Tooltip.cs:269（Update）/ BattleDriver.cs:30893` | 指向别的方法/字段 | — |
| `ManaManager.SetAccumulationMana(bool active)` | `BattleDriver.cs:28529-28536（0<manaAccumulation 才 SetActive）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:28523(所在方法)` |
| `ManaManager.SetFaithMana(int faithValue)` | `BattleDriver.cs:10796-10797 + :30201` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:10788(所在方法)` |
| `ManaManager.SetMana(int totalMana, int availableMana)` | `BattleDriver.cs:27061-27085 能量数字` | 注释 | `CardPresentation/Battle/BattleDriver.cs:27054(所在方法)` |
| `ManaManager.SetQuestPoints(int questPointsValue, int totalQuestPoints, bool silent…` | `BattleDriver.cs:10720 ShowsQuestPoints + _qpTextMe` | 注释 | `CardPresentation/Battle/BattleDriver.cs:187` |
| `ManaManager.SetSpiritStoneMana(int spiritStoneMana)` | `BattleDriver.cs:2001 _myStoneText` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:1990(所在方法)` |
| `ManaManager.ToggleCurrentMana(bool activate, int availableMana, int totalMana)` | `BattleDriver.cs:27085` | 注释 | `CardPresentation/Battle/BattleDriver.cs:27069(所在方法)` |
| `ManaManager.ToggleFaithMana(bool showY, int initialValue)` | `BattleDriver.cs:10796` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:10788(所在方法)` |
| `ManaManager.ToggleQuestPoints(bool showY, int totalPoints, int initialValue, bool …` | `Battle/BattleDriver.cs:30466-30467 + :10940 QuestPointsVisible + :3093…` | 注释 | `CardPresentation/Battle/BattleDriver.cs:11035` |
| `ManaManager.ToggleSpiritStoneMana(bool showY, int initialValue)` | `BattleDriver.cs:2001 + ShowsSpiritStone` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:11065` |
| `ManaTypeHolder.SetAccumulationMana(bool active)` | `BattleDriver.cs:28529` | 注释 | `CardPresentation/Battle/BattleDriver.cs:28523(所在方法)` |
| `ManaTypeHolder.SetMana(int value, bool silently)` | `BattleDriver.cs:30201 区段` | 注释 | `CardPresentation/Battle/BattleDriver.cs:30157(所在方法)` |
| `ManaTypeHolder.SetMana(int value, int value2, bool silently)` | `BattleDriver 能量数字` | 仅类/描述(无方法) | — |
| `ManaTypeHolder.Toggle(bool option, string altTipString)` | `BattleDriver.cs:10796 / 28529` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:10788(所在方法)` |
| `ManaTypeHolder.ToggleCurrentMana(bool activate, int currentMana, int totalMana)` | `BattleDriver 能量数字` | 仅类/描述(无方法) | — |
| `ManaTypeHolder.ToggleManaAccumulation(bool option, Action OnComplete)` | `BattleDriver.cs:28529` | 注释 | `CardPresentation/Battle/BattleDriver.cs:28523(所在方法)` |
| `ManaTypeQPHolder.SetMana(int value, bool silently)` | `BattleDriver.cs:10720` | 注释 | `CardPresentation/Battle/BattleDriver.cs:10709(所在方法)` |
| `ManaTypeQPHolder.ToggleCounter(bool option, GameObject counter, bool silently)` | `BattleDriver.cs:10720` | 注释 | `CardPresentation/Battle/BattleDriver.cs:10709(所在方法)` |
| `MulliganManager.ActivateMulligan(bool goFirst)` | `Battle/MulliganPanel.cs:960 Open + :159 SetTurnText` | 指向别的方法/字段 | `CardPresentation/Battle/MulliganPanel.cs:159` |
| `MulliganManager.ClickMulliganDone()` | `BattleDriver.cs:8060 HandleMulligan 闸` | 注释 | `CardPresentation/Battle/BattleDriver.cs:8375` |
| `MulliganManager.ClickReleaseHideMulligan()` | `Battle/MulliganPanel.cs:973 SetVisible` | 被多方法共用 | — |
| `MulliganManager.ProcessMulliganDone()` | `Battle/MulliganPanel.cs:960 Open/Close + OnDone` | 指向别的方法/字段 | `CardPresentation/Battle/MulliganPanel.cs:254` |
| `MulliganManager.SetMulliganTimer(int secondsLeftCounter)` | `BattleDriver.cs:7663 _mulligan.SetDoneText("0:0"+s)` | 注释 | `CardPresentation/Battle/BattleDriver.cs:7657(所在方法)` |
| `MulliganManager.SetWaitingForEnemy()` | `Battle/WaitBanner.cs + BattleDriver ShowWaitBanner` | 文件级 | — |
| `MulliganManager.Setup()` | `Battle/MulliganPanel.cs Build/Open` | 文件级 | — |
| `MulliganManager.ShowMulliganCards(bool showY)` | `Battle/MulliganPanel.cs:973 SetVisible` | 被多方法共用 | — |
| `MulliganManager.ShowMulliganElements(bool showY)` | `Battle/MulliganPanel.cs:973 + ShadeAlpha 0.8` | 指向别的方法/字段＋被多方法共用(3) | — |
| `MulliganManager.StopMulligan()` | `Battle/MulliganPanel.cs:967 Close` | 指向别的方法/字段 | — |
| `MulliganManager.Update()` | `BattleDriver.cs:8070 键盘支` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:13562` |
| `PracticeModePopup.BattleButtonOnClick()` | `BattleDriver.cs:4391-4397 Begin(... foeDeck: TakePendingOpponentDeck()…` | 注释 | `CardPresentation/Battle/BattleDriver.cs:4815` |
| `PracticeModePopup.ChangePlayText()` | `Shell/PracticeModePopup.cs:58 + :1500-1502` | 注释 | — |
| `PracticeModePopup.LoadSavedDeck(string savedDeck)` | `Shell/PracticeModePopup.cs:1258（Open 用 CollectionData.CurrentIndex()）` | 注释 | `CardPresentation/Shell/PracticeModePopup.cs:1241(所在方法)` |
| `PracticeModePopup.OnDestroy()` | `Shell/PracticeModePopup.cs:1354/1371/1420/1510-1513` | 函数中间 | `CardPresentation/Shell/PracticeModePopup.cs:1323(所在方法)` |
| `RadioMessageScript.AnimateText()` | `Battle/UnitChatPanel.cs:1219-1240 · :1325-1350` | 注释 | `CardPresentation/Battle/UnitChatPanel.cs:1216(所在方法)` |
| `RadioMessageScript.DisplayTipText(string tipLocalizedText)` | `Battle/UnitChatPanel.cs 文本层` | 文件级＋被多方法共用(4) | — |
| `RadioMessageScript.FadeElements(float targetAlpha)` | `Battle/UnitChatPanel.cs 淡入淡出` | 文件级＋被多方法共用(4) | — |
| `RadioMessageScript.SetupCardImage(Sprite speakerSprite)` | `Battle/UnitChatPanel.cs 卡面图` | 文件级＋被多方法共用(4) | — |
| `RadioMessageScript.ShowRadioMessage(SmallTipData smallTipData, Sprite speakerSprite)` | `BattleDriver.cs:6699-6715 ShowChatBox(panel=2 RadioChat)` | 函数中间 | `CardPresentation/Battle/BattleDriver.cs:6691(所在方法)` |
| `RadioMessageScript.StopAnims()` | `Battle/UnitChatPanel.cs 停表` | 文件级＋被多方法共用(4) | — |
| `ReplayHud.Initialize()` | `Battle/ReplayBar.cs + BattleDriver.cs:1050` | 行号越界 | — |
| `ReplayHud.PauseButtonClicked()` | `BattleDriver.cs:7826 OnReplayButtonClicked` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:8143` |
| `ReplayHud.PlayButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `ReplayHud.ReplayButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `ReplayHud.Setup()` | `Battle/ReplayBar.cs Setup + BattleDriver.cs:4862` | 行号越界 | `CardPresentation/Battle/ReplayBar.cs:330` |
| `ReplayHud.StepButtonClicked()` | `BattleDriver.cs:7826` | 函数中间＋被多方法共用(4) | `CardPresentation/Battle/BattleDriver.cs:7821(所在方法)` |
| `ScenarioEnvironmentConditionsManager.ApplyDefaultEnvironment(bool instant)` | `Battle/EnvironmentApplier.cs` | 文件级＋被多方法共用(6) | — |
| `ScenarioEnvironmentConditionsManager.ApplyEnvironment(ScenarioEnvironmentConditionSO environmentConditions,…` | `Battle/EnvironmentApplier.cs Apply + BlendT` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/EnvironmentApplier.cs:138` |
| `ScenarioEnvironmentConditionsManager.Awake()` | `Battle/EnvironmentApplier.cs` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Battle/EnvironmentApplier.cs:120` |
| `ScenarioEnvironmentConditionsManager.DisableCurrenEnvironment(bool instant)` | `Battle/EnvironmentApplier.cs` | 文件级＋被多方法共用(6) | — |
| `ScenarioEnvironmentConditionsManager.Register(IScenarioEnvironmentBlendeable blendeableToRegister)` | `Battle/EnvironmentApplier.cs Register` | 文件级＋被多方法共用(6) | — |
| `ScenarioEnvironmentConditionsManager.SetRegisteredBlendeablesState(ScenarioBlendOptions options)` | `Battle/EnvironmentApplier.cs + ScenarioBlendables.cs` | 文件级＋被多方法共用(6) | — |
| `SceneCameraManager.GetUICamera()` | `Battle/HudCanvas.cs Ensure(cam) + LayoutSpace.Cam` | 文件级(可补行号) | `CardPresentation/Battle/HudCanvas.cs:90` |
| `SceneCameraManager.TryFindCameraWithTag(string cameratag, out Camera foundCamera)` | `Battle/HudCanvas.cs / LayoutSpace` | 文件级 | — |
| `TargetReticleController.AutoMovingCrosshair()` | `Battle/TargetReticle.cs:563 Aim / :698 UpdateArc` | 指向别的方法/字段 | `CardPresentation/Battle/TargetReticle.cs:698` |
| `TargetReticleController.Initialize()` | `Battle/TargetReticle.cs:363 Build + :503 floorY` | 指向别的方法/字段 | — |
| `TargetReticleController.MoveCrosshair(Vector3 originPositionBoardSpace, Vector3 targetPosition…` | `Battle/TargetReticle.cs:563 Aim` | 指向别的方法/字段 | — |
| `TouchInputManager.Update()` | `Battle/TouchInputManager.cs` | 文件级(可补行号) | `CardPresentation/Battle/TouchInputManager.cs:163` |
| `TouchInputManager.UpdateDrag(Vector2 currentPos)` | `Battle/TouchInputManager.cs TouchDragDelta` | 文件级(可补行号) | `CardPresentation/Battle/TouchInputManager.cs:276` |
| `UIPCSkinManager.Awake()` | `Battle/BattleDriver.cs:27476（HUD 几何/字号按实况值建）` | 注释 | `CardPresentation/Battle/BattleDriver.cs:27457(所在方法)` |

## 联机.tsv（A 9，问题 7）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `BattleCommsManager.PUNReceiveChatMessage(int messageIndex, PhotonMessageInfo mi)` | `Net/NetBattle.cs:1289-1334 case NetKind.Chat（四道闸：解不出 / seat==MySeat 不回…` | 函数中间 | `CardPresentation/Net/NetBattle.cs:49` |
| `BattleCommsManager.RemoteSendPlayerAction(PlayerAction actionToSend)` | `Net/NetBattle.cs:970-992 OnLocalAction（主机 ⇒ 入 Log + 广播 Applied；客机 ⇒ 发 …` | 函数中间 | `CardPresentation/Net/NetBattle.cs:991` |
| `BattleCommsManager.SendChatMessage(ChatMessage messageIndex)` | `Net/NetBattle.cs:1053-1066 OnLocalChat → 只发 5…10 的判据在 Net/NetChat.cs:8…` | 函数中间 | `CardPresentation/Net/NetBattle.cs:1074` |
| `BattleCommsManager.SendForfeit()` | `Net/NetBattle.cs:1037-1041 OnLocalResign → NetKind.Resign；宿主 Battle/Ba…` | 函数中间 | `CardPresentation/Net/NetBattle.cs:1058` |
| `BattleNetworkManager.SetOpponentDisconnected()` | `Net/NetBattle.cs:385-407 HandlePeerLost → :643-666 StartReconnectCount…` | 注释 | `CardPresentation/Net/NetBattle.cs:406` |
| `SearchOpponentManager.CancelSearchForDisconnect()` | `Net/NetMatchmaking.cs:205-236 HandleLobbyPeerLost（提示行 + NetRuntime.Not…` | 指向别的方法/字段 | `CardPresentation/Net/NetMatchmaking.cs:217` |
| `SearchOpponentManager.StartBattle()` | `Net/NetMatchmaking.cs:780-784 seed → BattleDriver.FirstSeatForSeed + A…` | 注释 | `CardPresentation/Net/NetMatchmaking.cs:846` |

## 卡牌面_渲染.tsv（A 108，问题 108）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `BoardTraitIcon.Awake()` | `Core/CardView.cs PulseBadge（归位）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Core/CardView.cs:4045` |
| `BoardTraitIcon.DOFade(float targetAlpha, float time)` | `Core/CardView.cs FadeBadges` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Core/CardView.cs:8010` |
| `BoardTraitIcon.HighlightIcon()` | `Core/CardView.cs PulseBadge` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Core/CardView.cs:4045` |
| `BoardTraitIcon.Initialize(Sprite traitIcon, int counter, bool traitEnabled)` | `Core/CardView.cs BuildBadgeSlot / SetBadges` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Core/CardView.cs:3815` |
| `BoardTraitIcon.Toggle(bool option)` | `CardView Show()/enabled（SetBadges）` | 仅类/描述(无方法) | — |
| `BoardTraitIcon.ToggleCounter(bool option)` | `CardView SetBadges（counter>0 才写字）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:3629` |
| `BoardTraitIcon.UpdateCounter(int counter)` | `CardView SetBadges` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:3629` |
| `BoardTraitIcon._003CDOFade_003Eb__18_0()` | `FadeBadges 的 DOTween.To getter` | 仅类/描述(无方法) | — |
| `BoardTraitIcon._003CDOFade_003Eb__18_1(float x)` | `FadeBadges 的 DOTween.To setter` | 仅类/描述(无方法) | — |
| `Card2DController.Awake()` | `Core/CardView.cs SDF 材质初始化（_outlineMat/_shadowMat）` | 文件级 | — |
| `Card2DController.ChangeHighlightColor(Color color, bool isCardback, bool instant)` | `CardView SetOutline` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:10260` |
| `Card2DController.ChangeShadowColor(Color color, bool isCardback, bool instant)` | `CardView ApplyShadowColor` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:6180` |
| `Card2DController.SetCardBack(Sprite cardBackSprite, Sprite cardBackShadow)` | `Core/CardbackFace.cs + CardView.SetFace(Back)` | 文件级 | — |
| `Card2DController.SetCardVisibility(CardVisibility cardVisibility)` | `CardView SetFace(Front/Back)` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:2269` |
| `Card2DController.SetFrame(Sprite frameSprite, Sprite highlightSprite)` | `CardArt.Frame / CardArt.Sdf` | 仅类.方法(可补行号) | `CardPresentation/Core/CardArt.cs:40` |
| `Card2DController.Toggle(bool option)` | `CardView Show()/gameObject.SetActive` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:2482` |
| `Card2DController.ToggleCardShadow(bool option, bool instant)` | `CardView ApplyShadowColor` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:6180` |
| `Card2DController.ToggleHighlight(bool option, bool isCardback, bool instant)` | `CardView ApplyRimColor` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:8246` |
| `CardCollider.Awake()` | `Battle/BattleDriver 指针管线 + Hand/CardInteraction / MenuScroll` | 仅类/描述(无方法) | — |
| `CardCollider2D.Initialize()` | `CardView EnsureHitCollider / SyncHitCollider` | 仅类/描述(无方法) | — |
| `CardCollider2D.ResetCollider()` | `CardView SyncHitCollider（常态档）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:9294` |
| `CardCollider2D.SetColliderInHand()` | `CardView HitRect（in-hand 档）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:9355` |
| `CardCollider2D.SetColliderNotInHand(CardStateOptions cardState)` | `CardView 按 CardFace 切档 + 命中层过滤` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:2218` |
| `CardCollider2D.Toggle(bool option)` | `CardView _hitCollider.enabled` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:9255` |
| `CardFramesSO.GetReferences(CardTypeOptions cardTypeOptions, out AssetReferenceTyped…` | `CardArt.Frame / CardArt.Sdf` | 仅类.方法(可补行号) | `CardPresentation/Core/CardArt.cs:40` |
| `CardHighlight.ChangeFrameColor(Color targetColor, float seconds)` | `CardView ApplyRimColor` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:8246` |
| `CardHighlight.ChangeState(HighlightStates newState)` | `CardView SetHighlight + Core/CardHighlight.cs 状态表` | 文件级 | — |
| `CardHighlight.ChangeTargetScale(Vector3 targetScale, float time)` | `CardView SetHighlightScale` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:10234` |
| `CardHighlight.HighlightAttackTypes(AttackTypes attackType)` | `Battle/AttackSelector（`HighlightIfNeeded` / `HighlightSelectedAttackTy…` | 仅类/描述(无方法) | — |
| `CardHighlight.SetCombatHighlights()` | `CardView SetWillDie(false)` | 仅类/描述(无方法) | — |
| `CardHighlight.SetRemnantHighlight(SpriteRenderer remnantHighlight)` | `CardView `SetRemnantLight`（找残骸体里的 `RemnantLight` SpriteRenderer 并涂主环色）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:2980` |
| `CardHighlight.Start()` | `CardView 出生路径` | 仅类/描述(无方法) | — |
| `CardHighlight.ToggleCombatPreviewHighlight(CardScript otherCard, bool isAttacker, bo…` | `Battle/BattleDriver `SetReticleTarget` + RuleCore 伤害条目 + CardView `Set…` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:8391` |
| `CardHighlight.ToggleFrames(bool state)` | `CardView 环层 enabled` | 仅类/描述(无方法) | — |
| `CardHighlight._003CChangeFrameColor_003Eb__28_0()` | `ApplyRimColor 的补间回调` | 仅类/描述(无方法) | — |
| `CardImageController.Awake()` | `CardView SDF 材质初始化（_outlineMat/instancedMaterial）` | 仅类/描述(无方法) | — |
| `CardImageController.ChangeSize(ImageInFrameSize size, bool instant)` | `CardView SetArtBig` | 仅类/描述(无方法) | — |
| `CardImageController.ChangeState(ImageInFrameSize stateToChange, bool instant)` | `CardView SetArtBig / Build（art 层）` | 仅类/描述(无方法) | — |
| `CardImageController.DoChange(Vector3 targetScale, Vector4 targetClamp, bool instant, bool …` | `CardView BigState 补间（`BigStateTweenDuration 0.3 / Ease.OutBack / Apply…` | 仅类.方法(解析不到) | — |
| `CardImageController.SetImage(Sprite sprite, CardTypeOptions cardTypeOptions)` | `CardView SetArt / CardArt` | 仅类/描述(无方法) | — |
| `CardImageController.SetOriginalMaterial()` | `CardView 材质复位` | 仅类/描述(无方法) | — |
| `CardServices.CheckForCardUpgrade(RawCardScript card, out CardTierConfig.CardTierInf…` | `CardProgress `CanUpgrade`（`Spares >= NeedCopies`）` | 仅类/描述(无方法) | — |
| `CardServices.CraftCard(RawCardScript card, Action onSuccess, Action<CustomError> on…` | `Shell/CardDetailPopup CraftNow/TryCraft` | 仅类/描述(无方法) | — |
| `CardServices.RegisterCardTier(RawCardScript card, CardTier tier)` | `CardProgress.Level（缺省 1 + 存档）` | 仅类.方法(解析不到) | — |
| `CardServices.UpgradeCard(RawCardScript targetCard, Action onSuccess, Action<string>…` | `CardDetailPopup DoUpgrade（C 域）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CardDetailPopup.cs:2273` |
| `CardTextCountersController.Awake()` | `CardView BuildFaceStatTexts / _stats` | 仅类/描述(无方法) | — |
| `CardTextCountersController.CacheData()` | `CardView 图层缓存` | 仅类/描述(无方法) | — |
| `CardTextCountersController.ChangeFontsColor(Color targetColor, bool useOriginalColor)` | `CardView ApplyTint / ApplyStatFlashes` | 仅类/描述(无方法) | — |
| `CardTextCountersController.DoColorChange(int currentValue, int baseValue)` | `CardView StatColorVsBase` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:12930` |
| `CardTextCountersController.HideStat()` | `CardView StatShownFor` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:12949` |
| `CardTextCountersController.ResetHide()` | `CardView 每次刷新复位` | 仅类/描述(无方法) | — |
| `CardTextCountersController.SetText(int newAmount)` | `CardView SetStatText（7 参等价）` | 仅类/描述(无方法) | — |
| `CardTextCountersController.SetText(int newAmount, int currentValue, int baseValue, bool doColorCh…` | `CardView SyncStatLayers / BumpStat` | 仅类/描述(无方法) | — |
| `CardTextCountersController.Toggle(bool option, bool cardSimplifiedView)` | `CardView `ComputeTextState` + `SmallScreenUI`` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextCountersController.Toggle(bool value, bool simplifiedView)` | `同上（重载）` | 间接指认 | — |
| `CardTextCountersController.ToggleSimplifiedView(bool option)` | `CardView ComputeTextState` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.ChangeState(CardNameAndDescriptionTypes stateToChange)` | `CardView ComputeTextState` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.ChangeToInitialState()` | `CardView ComputeTextState（回初始态）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.SetArmy(string army)` | `CardView 不画阵营行` | 仅类/描述(无方法) | — |
| `CardTextsController.SetCardTextLayout(string cardDescription)` | `CardView ComputeTextState` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.SetDescription(string cardDescription)` | `CardView BuildTextLayers` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:4861` |
| `CardTextsController.SetRace(string race)` | `CardView SubtypeLine/RaceTerm` | 仅类/描述(无方法) | — |
| `CardTextsController.SetStartingState(string cardDescription, string race, CardTypeOptions …` | `CardView ComputeTextState 的初始态` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.SetTexts(string cardName, string cardDescription, CardTypeOptions card…` | `CardView BuildTextLayers` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:4861` |
| `CardTextsController.SetTexts(string cardName, string cardDescription, string race, string …` | `CardView BuildTextLayers` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:4861` |
| `CardTextsController.ToggleSimplifiedView(bool option, CardTextsInHandOptions cardTextsInHa…` | `CardView ComputeTextState` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Core/CardView.cs:5097` |
| `CardTextsController.ToggleState()` | `CardView BuildTextLayers（按态开关件）` | 仅类.方法(可补行号) | `CardPresentation/Core/CardView.cs:4861` |
| `CardTrait.GetNewTrait(DefinedTrait trait, bool hideTrait, int defaultValue)` | `RuleEngine/CardDef（无 `_hide`，行为等价）` | 仅类/描述(无方法) | — |
| `CardTrait.IsActive(CardScript card)` | `RuleEngine 各词条的生效判据` | 仅类/描述(无方法) | — |
| `CardTrait.ShowingTrait(CardScript card, DefinedTrait trait)` | `CardDef 词条列表` | 仅类/描述(无方法) | — |
| `CardTraitData.GetValueString()` | `Badges.ValueSuffix / 角标数字` | 仅类.方法(解析不到) | — |
| `ScenarioEnvironmentConditionSO.ApplyAmbientColor(bool instant)` | `Battle/EnvironmentApplier.Apply` | 仅类.方法(可补行号) | `CardPresentation/Battle/EnvironmentApplier.cs:138` |
| `ScenarioEnvironmentConditionSO.ApplyFog(bool instant)` | `Battle/EnvironmentApplier.Apply` | 仅类.方法(可补行号) | `CardPresentation/Battle/EnvironmentApplier.cs:138` |
| `ScenarioEnvironmentConditionSO.BackToDefault()` | `EnvironmentApplier（切 defaultEnvironment）` | 仅类/描述(无方法) | — |
| `ScenarioEnvironmentConditionSO.DisableEnvironment(UnityEngine.Object caller, bool instant)` | `EnvironmentApplier（撤环境）` | 仅类/描述(无方法) | — |
| `ScenarioEnvironmentConditionSO.EnableEnvironment(bool instant)` | `EnvironmentApplier（换环境）` | 仅类/描述(无方法) | — |
| `ScenarioEnvironmentConditionSO.ObjectFinishedDisableBlend()` | `EnvironmentApplier（计数归零销毁）` | 仅类/描述(无方法) | — |
| `ScenarioEnvironmentConditionSO.ToggleFog()` | `EnvironmentApplier（fog 开关）` | 仅类/描述(无方法) | — |
| `ScenarioEnvironmentConditionSO.TryGetClip(string filterCodeToCheck, out AnimationClip clipToReturn)` | `Core/EnvironmentConditions.AnimToChange + GUID 查表` | 仅类.方法(解析不到) | — |
| `ScenarioEnvironmentConditionSO._003CApplyFog_003Eb__20_4()` | `EnvironmentApplier（补间完成）` | 仅类/描述(无方法) | — |
| `TextTooltipController.Awake()` | `Core/Tooltip + Core/Tooltip.TipText` | 仅类.方法(解析不到) | — |
| `TextTooltipController.CheckForLinkCollision()` | `Core/Tooltip（悬停命中检测）` | 仅类/描述(无方法) | — |
| `TextTooltipController.CheckLinkID(string linkID)` | `Core/Tooltip（换条目才刷新）` | 仅类/描述(无方法) | — |
| `TextTooltipController.GetFaithTooltip(string linkid)` | `TipText.Trait("faith")` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetMeleeTooltip(string linkid)` | `TipText.Melee` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetQPTooltip(string linkid)` | `TipText.Trait("questpoints") → 规范键 `quest`` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetRangedTooltip(string linkid)` | `TipText.Ranged` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetSabotageTooltip(string linkid)` | `TipText.Trait("sabotage")` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetSpiritStoneTooltip(string linkId)` | `TipText.Trait("spiritstone")` | 仅类.方法(解析不到) | — |
| `TextTooltipController.GetTraitTooltip(string linkId)` | `TipText.Trait` | 仅类.方法(解析不到) | — |
| `TextTooltipController.Hide()` | `Tooltip.Hide` | 仅类.方法(可补行号) | `CardPresentation/Core/Tooltip.cs:392` |
| `TextTooltipController.InitializeTooltips()` | `TipText（trait 全枚举 + 6 个显式键）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnDeselect(BaseEventData eventData)` | `Tooltip（失焦即隐）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnDisable()` | `Tooltip（关时隐藏）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnEnable()` | `Tooltip（复位悬停态）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnPointerEnter(PointerEventData eventData)` | `Tooltip（进入即检测）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnPointerExit(PointerEventData eventData)` | `Tooltip（离开即隐）` | 仅类/描述(无方法) | — |
| `TextTooltipController.OnSelect(BaseEventData eventData)` | `Tooltip（选中即检测）` | 仅类/描述(无方法) | — |
| `TextTooltipController.Show(ToolTipData toolTipData)` | `Tooltip.Show（Trait 两栏版式）` | 仅类.方法(可补行号) | `CardPresentation/Core/Tooltip.cs:307` |
| `TextTooltipController.Toggle(bool option)` | `Tooltip（启用/停用）` | 仅类/描述(无方法) | — |
| `TextTooltipController.Update()` | `Tooltip.Update（逐帧命中）` | 仅类.方法(可补行号) | `CardPresentation/Core/Tooltip.cs:269` |
| `UICardInfoItem.Awake()` | `Deck/DeckRuntime（格子的建/点）` | 仅类/描述(无方法) | — |
| `UICardInfoItem.ButtonOnClick()` | `Deck/DeckRuntime / Shell/CardDetailPopup` | 仅类/描述(无方法) | — |
| `UICardInfoItem.OnDestroy()` | `Deck/DeckRuntime.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Deck/DeckRuntime.cs:1196` |
| `UICardInfoItem.Reset()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(5) | — |
| `UICardInfoItem.SetCost()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(5) | — |
| `UICardInfoItem.SetCount(int amount)` | `Deck/DeckRuntime.cs（x{n} 计数）` | 文件级＋被多方法共用(5) | — |
| `UICardInfoItem.ToggleDataState()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(5) | — |

## 卡组编辑.tsv（A 177，问题 177）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `CardDeck.AddCard(RawCardScript card)` | `DeckEditorState.TryAddCard（首张定阵营那条在 AddCard/Hero 那两支里）` | 仅类.方法(解析不到) | — |
| `CardDeck.AddDefensiveCard(RawCardScript card)` | `DeckEditorState.SetDefensive` | 仅类.方法(解析不到) | — |
| `CardDeck.AddHero(RawCardScript card)` | `DeckEditorState.SetWarlord` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:453` |
| `CardDeck.AreThereInvalidArmyCards()` | `DeckRules.Validate 的 WrongFaction 那一条` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:443` |
| `CardDeck.CanAddCard(RawCardScript card, out string error)` | `DeckRules.Validate + CopyLimit + LegendaryTotalLimit + SameFaction + D…` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:443` |
| `CardDeck.CanAddDefensive(RawCardScript card, out string error)` | `DeckEditorState.TryAddDefensive` | 仅类.方法(解析不到) | — |
| `CardDeck.CanAddHero(RawCardScript card, out string error)` | `DeckEditorState（督军格唯一）` | 仅类/描述(无方法) | — |
| `CardDeck.CardCopiesInDeck(RawCardScript card)` | `DeckEditorState.CountOf（按 id；异画折算 = 我们异画共用同一 CardDef）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:219` |
| `CardDeck.CardCount(bool countHero)` | `DeckEditorState.DeckCount` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:385` |
| `CardDeck.CopyDeck(CardDeck deckToCopy)` | `PlayerDeck 整份拷贝（DeckLibrary.CommitCurrent）` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:335` |
| `CardDeck.DeckBasicSetup(string pDeckName, RawCardScript pDeckHero, string pDeck…` | `RuleEngine/Core/DeckRules.cs 的 PlayerDeck（id 表示，无 RawCardScript 对象）+ D…` | 文件级(可补行号) | `RuleEngine/Core/DeckRules.cs:400` |
| `CardDeck.DeserializeDeckString(string deckString)` | `DeckLibrary.ImportString` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:452` |
| `CardDeck.EmptySlotsCount()` | `DeckEditorState.MaxDeckCount − DeckCount` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:386` |
| `CardDeck.GetDeckCardback()` | `Core/CardArt.DeckCardback(id, faction)` | 仅类.方法(可补行号) | `CardPresentation/Core/CardArt.cs:983` |
| `CardDeck.GetEmptyDeck()` | `DeckLibrary.NewUnsaved / PlayerDeck 构造` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:187` |
| `CardDeck.GetLibraryInFull()` | `DeckRuntime.DeckEntries()` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4189` |
| `CardDeck.Has(RawCardScript card)` | `DeckEditorState.CountOf > 0` | 仅类.方法(解析不到) | — |
| `CardDeck.LegendaryCardsInDeck()` | `DeckRules 的传说总数校验` | 仅类/描述(无方法) | — |
| `CardDeck.RemoveCard(RawCardScript card)` | `DeckEditorState.TryRemove` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:471` |
| `CardDeck.RemoveDefensiveCard()` | `DeckEditorState.SetDefensive(null)` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:479` |
| `CardDeck.RemoveNoValidArmyCards()` | `DeckRules.RemoveInvalidCards` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:400` |
| `CardDeck.Serialize(CardDeck deck)` | `DeckLibrary.ExportString` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:429` |
| `CardDeck.TryAddCard(RawCardScript card)` | `DeckEditorState.TryAddCard` | 仅类.方法(解析不到) | — |
| `CardDeck.TryAddCard(RawCardScript card, out string error)` | `DeckEditorState.TryAddCard（按 Type 分派）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:28` |
| `CardDeck.TryAddDefensive(RawCardScript card, out string error)` | `DeckEditorState.SetDefensive` | 仅类.方法(解析不到) | — |
| `CardDeck.TryAddHero(RawCardScript card, out string error)` | `DeckEditorState.SetWarlord` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:453` |
| `CardDeck._003CGetDeckCardback_003Eb__56_0(CosmeticItem x)` | `CardbackTable / CardArt.DeckCardback` | 仅类.方法(可补行号) | `CardPresentation/Core/CardArt.cs:983` |
| `CollectionDeck.Awake()` | `Shell/MenuDraw.cs::DeckCell（共用件）+ CollectionWindow.BuildDeckCell` | 文件级＋被多方法共用(3) | — |
| `CollectionDeck.Config(CardDeck item, GameplayVariablesData gameplayVariablesData)` | `Shell/MenuDraw.cs::DeckCell（共用件）+ CollectionWindow.BuildDeckCell` | 文件级＋被多方法共用(3) | — |
| `CollectionDeck.HoverHighlight(bool state)` | `MenuDraw.DeckCell` | 仅类.方法(可补行号) | `CardPresentation/Shell/MenuDraw.cs:3225` |
| `CollectionDeck.OnDestroy()` | `Shell/MenuDraw.cs::DeckCell（共用件）+ CollectionWindow.BuildDeckCell` | 文件级＋被多方法共用(3) | — |
| `DeckArmyFilter.FilterItem(CardDeck deck)` | `Shell/CollectionWindow._deckFacFilter（Deck 抽屉阵营格）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4693` |
| `DeckCollectionTab.GetCollection()` | `CollectionWindow.FilteredDeckIndices` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckCollectionTab.OnItemSelected(CollectionItem<CardDeck> item, PointerEventData.InputBu…` | `Shell/CollectionWindow（Deck 页）+ Core/FilterPanelModel（Deck 抽屉）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:480` |
| `DeckCollectionTab.OnOpen()` | `CollectionWindow.BuildDeckList` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4161` |
| `DeckCollectionTab.RefreshCollection()` | `CollectionWindow.RebuildDeckListNow` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5370` |
| `DeckCollectionTab.SetEventType(IOwnDeckPlayEvent eventToAssign)` | `Shell/CollectionWindow（Deck 页）+ Core/FilterPanelModel（Deck 抽屉）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:480` |
| `DeckCollectionTab._003CGetCollection_003Eb__7_0(CardDeck deck)` | `CollectionWindow.FilteredDeckIndices` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckCosmeticDrawer.Initialize(CardDeck deck)` | `DeckRuntime.RefreshCosmetics + EquipCardback` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:3516` |
| `DeckEditingHeader.SetInstructions(string text)` | `Shell/DeckEditingWindow.cs 的页头` | 文件级 | — |
| `DeckEditingPanel.AddCard(RawCardScript card, bool withAnim)` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ChangeName(string name)` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.CheckCardSlotDrag(PointerEventData eventData, GameObject source)` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.Drop(RawCardScript card, bool withAnim)` | `Deck/DeckRuntime.cs` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Deck/DeckRuntime.cs:4093` |
| `DeckEditingPanel.Initialize(CardDeck deck)` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.RemoveCard(RawCardScript card)` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.Start()` | `Deck/DeckRuntime.cs` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Deck/DeckRuntime.cs:1198` |
| `DeckEditingPanel.ToggleCosmetics()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ToggleDeckInfo()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.ToggleDeckList()` | `Deck/DeckRuntime.cs` | 文件级＋被多方法共用(10) | — |
| `DeckEditingPanel.UpdateDrawers()` | `DeckRuntime（InfoTab/DeckListPage/Cosmo 三态）` | 仅类/描述(无方法) | — |
| `DeckEditingStateController.Initialize(DeckEditingWindow editingWindow)` | `DeckRuntime（InfoTab / DeckListPage / Cosmetics 三态）` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckCosmetic(bool state)` | `DeckRuntime（InfoTab / DeckListPage / Cosmetics 三态）` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckInfo(bool state)` | `DeckRuntime（InfoTab / DeckListPage / Cosmetics 三态）` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingStateController.ToggleDeckList(bool state)` | `DeckRuntime（InfoTab / DeckListPage / Cosmetics 三态）` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `DeckEditingWindow.CheckCardDrag(PointerEventData eventData, GameObject source)` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.CheckCosmeticDrag(PointerEventData eventData, GameObject source)` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.Close()` | `DeckRuntime.BackToMenu / 窗关` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:5254` |
| `DeckEditingWindow.ConfirmDiscard()` | `DeckRuntime.ShowDiscardChangesPopUp` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:5670` |
| `DeckEditingWindow.ESCPressed()` | `DeckRuntime（ESC 走 SaveAndSay）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:5546` |
| `DeckEditingWindow.FilterToDeck()` | `DeckRuntime.RefreshFilters + DeckArmyIcon（NoArmyFaction = Ultramarines…` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:7169` |
| `DeckEditingWindow.GetCardCollection()` | `DeckEditorState.VisibleCards（WarlordGatedPool）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:529` |
| `DeckEditingWindow.GetCosmeticCollection()` | `DeckRuntime.RefreshCosmetics + CardbackTable` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:3516` |
| `DeckEditingWindow.OnCardClick(CollectionItem<RawCardScript> card, PointerEventData.Input…` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.OnCosmeticClick(CollectionItem<CosmeticItemCardback> cosmetic, Pointer…` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.OpenCardInformation(RawCardScript card)` | `DeckRuntime.OpenCardWindow（本轮补上 SetSprite(hdr_army, 卡的阵营)）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:8369` |
| `DeckEditingWindow.Refresh()` | `DeckRuntime.RefreshAll` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4136` |
| `DeckEditingWindow.RefreshCards()` | `DeckRuntime.RefreshPool + RefreshHeader + CheckFocusedArmy（本轮）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:2980` |
| `DeckEditingWindow.Start()` | `DeckRuntime.Build` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:1276` |
| `DeckEditingWindow.ToggleDisplay(bool showCards)` | `DeckRuntime 的页 tab` | 仅类/描述(无方法) | — |
| `DeckEditingWindow.ToggleShowOwnedCards(bool show)` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.ToggleShowOwnedCosmetics(bool show)` | `Deck/DeckRuntime.cs（内容）+ Shell/DeckEditingWindow.cs（窗壳）` | 文件级＋被多方法共用(6) | — |
| `DeckEditingWindow.TryClose()` | `DeckRuntime.TryClose + ShowDiscardChangesPopUp` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:5660` |
| `DeckEditingWindow.TryOpen(object data, GameWindowOptions options)` | `DeckRuntime.Build(lib) + CollectionData.TakePendingNewDeck` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:1276` |
| `DeckEditingWindow.TrySaveDeck()` | `DeckRuntime.SaveAndSay → CommitDeck（Validate 不过则不提交并出声）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:5546` |
| `DeckEditingWindow.UpdateDoneButton(bool firstTime)` | `DeckRuntime（Done 钮高亮；合法性由 Validate 那一步挡）` | 仅类/描述(无方法) | — |
| `DeckEditingWindow.UploadDeck()` | `DeckLibrary.CommitCurrent / Insert` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:335` |
| `DeckEditingWindow._003CConfirmDiscard_003Eb__45_1(GameObject GameObject)` | `DeckRuntime 的对应处 / DeckRules.Validate` | 仅类.方法(可补行号)＋被多方法共用(4) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckEditingWindow._003CGetCardCollection_003Eb__32_1(RawCardScript c)` | `DeckRuntime 的对应处 / DeckRules.Validate` | 仅类.方法(可补行号)＋被多方法共用(4) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckEditingWindow._003CGetCardCollection_003Eb__32_4(RawCardScript c)` | `DeckRuntime 的对应处 / DeckRules.Validate` | 仅类.方法(可补行号)＋被多方法共用(4) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckEditingWindow._003CTrySaveDeck_003Eb__42_1(GameObject _)` | `DeckRuntime 的对应处 / DeckRules.Validate` | 仅类.方法(可补行号)＋被多方法共用(4) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckEditorCollectionDisplay.CheckFocusedArmy()` | `DeckRuntime.CheckFocusedArmy（本轮新增，含 PoolRefL/T/R/B 常量）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4921` |
| `DeckEditorCollectionDisplay.DrawCell(CollectionCard card)` | `Deck/DeckRuntime.RefreshPool + CheckFocusedArmy（本轮新增）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:2980` |
| `DeckEditorCollectionDisplay.OnListDrag(Vector2 listPos)` | `DeckRuntime.UiScrollPool → RefreshPool → RefreshHeader → CheckFocusedA…` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:6205` |
| `DeckEditorCollectionDisplay.RefreshCells()` | `Deck/DeckRuntime.RefreshPool + CheckFocusedArmy（本轮新增）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:2980` |
| `DeckEditorCollectionDisplay.RefreshCollection()` | `DeckRuntime.RefreshHeader 尾部 CheckFocusedArmy` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4745` |
| `DeckEditorCollectionDisplay.SetCell(ICell cell, int index)` | `Deck/DeckRuntime.RefreshPool + CheckFocusedArmy（本轮新增）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:2980` |
| `DeckEditorCollectionDisplay.SetEditingDeck(CardDeck deck)` | `DeckRuntime.Build / State.LoadDeck` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:1276` |
| `DeckInfoControls.Initialize(DeckInfoPopup popup, DeckInfoContext context, bool isPlayer…` | `Shell/DeckInfoPopup.cs（OnOption / DeleteInteractable 的 `1 < 卡组数`）` | 文件级(可补行号) | `CardPresentation/Shell/DeckInfoPopup.cs:596` |
| `DeckInfoControls.OnDisable()` | `Shell/DeckInfoPopup.cs（OnOption / DeleteInteractable 的 `1 < 卡组数`）` | 文件级(可补行号) | `CardPresentation/Shell/DeckInfoPopup.cs:596` |
| `DeckInfoDrawer.Initialize(CardDeck deck)` | `Shell/DeckInfoPopup.cs（卡列表）` | 文件级 | — |
| `DeckInfoPopup.CanImportDeck(CardDeck cardDeck)` | `Shell/DeckInfoPopup.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DeckInfoPopup.cs:859` |
| `DeckInfoPopup.CardSelected(RawCardScript card)` | `DeckInfoPopup 的卡列表点击 → CardDetailPopup` | 仅类/描述(无方法) | — |
| `DeckInfoPopup.Close()` | `Shell/DeckInfoPopup.cs` | 文件级＋被多方法共用(4) | — |
| `DeckInfoPopup.DeleteDeck()` | `DeckInfoPopup.OnOption("Delete") + DeleteConfirmPopup` | 仅类.方法(可补行号) | `CardPresentation/Shell/DeckInfoPopup.cs:2333` |
| `DeckInfoPopup.EditDeck()` | `CollectionWindow.GoEdit（CollectionData.PendingEditDeck）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5409` |
| `DeckInfoPopup.MakeDeckString()` | `DeckLibrary.ExportString` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:429` |
| `DeckInfoPopup.OnWarlordClick()` | `DeckInfoPopup（督军钮）` | 仅类/描述(无方法) | — |
| `DeckInfoPopup.Open()` | `Shell/DeckInfoPopup.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/DeckInfoPopup.cs:1234` |
| `DeckInfoPopup.SelectPracticeOpponentDeck()` | `PracticeModePopup.PickPrebuilt` | 仅类.方法(可补行号) | `CardPresentation/Shell/PracticeModePopup.cs:2693` |
| `DeckInfoPopup.ShareDeck()` | `DeckRuntime.CopyDeckToClipboard / ShareCopiedText` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:6014` |
| `DeckInfoPopup.ShareDeckOnChat(Transform optionsMountPoint)` | `Shell/DeckInfoPopup.cs` | 文件级＋被多方法共用(4) | — |
| `DeckInfoPopup.StartPracticeMatch(CardDeck deck)` | `PracticeModePopup + BattleDriver` | 仅类/描述(无方法) | — |
| `DeckInfoPopup.ToggleDrawers(bool showList)` | `DeckInfoPopup.ToggleDrawers` | 仅类.方法(解析不到) | — |
| `DeckInfoPopup._003CDeleteDeck_003Eb__27_2(GameObject _)` | `DeckInfoPopup.DeleteDeckNow / ShowMessagePopUp 回调` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/DeckInfoPopup.cs:2436` |
| `DeckInfoPopup._003CDeleteDeck_003Eb__27_3(string _)` | `DeckInfoPopup.DeleteDeckNow / ShowMessagePopUp 回调` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/DeckInfoPopup.cs:2436` |
| `DeckInfoPopup._003CDeleteDeck_003Eg__Delete_007C27_0()` | `DeckInfoPopup.DeleteDeckNow / ShowMessagePopUp 回调` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/DeckInfoPopup.cs:2436` |
| `DeckInventory.GetItems()` | `DeckLibrary.Decks` | 仅类.方法(可补行号) | `RuleEngine/Data/DeckLibrary.cs:141` |
| `DeckListDrawer.AddCardToList(RawCardScript card, bool reorder, bool withAnim)` | `DeckRuntime.RefreshDeckList / DeckEntries / SetCount` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.CleanPrevious()` | `DeckRuntime.RefreshDeckList` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.ConfigPrefab(UICardInfoItem cardInfo)` | `DeckRuntime.RefreshDeckList` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.Initialize(CardDeck cardDeck, DeckListOptions options)` | `DeckRuntime.RefreshDeckList + DeckEntries / PrebuiltDecks` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.Initialize(List<RawCardScript> cardList, DeckListOptions options)` | `DeckRuntime.RefreshDeckList + DeckEntries / PrebuiltDecks` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.Initialize(PrebuiltDeck cardDeck, DeckListOptions options)` | `DeckRuntime.RefreshDeckList + DeckEntries / PrebuiltDecks` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.RemoveCardFromList(RawCardScript card)` | `DeckRuntime.RefreshDeckList / DeckEntries / SetCount` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.Reorder()` | `DeckRuntime.DeckEntries 的排序（费→名→id）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4189` |
| `DeckListDrawer.SetCount()` | `DeckRuntime（`N/max` 页头 + 每行 `x/den`）` | 仅类/描述(无方法) | — |
| `DeckListDrawer.SetOptions(DeckListOptions options)` | `DeckRuntime.RefreshDeckList` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer.ShowEmptySlotsIfNeeded()` | `Deck/DeckRuntime.cs `DeckRows()` + `RefreshDeckList` 的 `emptySlot` 支（槽…` | 文件级(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4260` |
| `DeckListDrawer.SpawnCardInfo(RawCardScript card, bool withAnim)` | `DeckRuntime.RefreshDeckList / DeckEntries / SetCount` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckRuntime.cs:4288` |
| `DeckListDrawer._003CSpawnCardInfo_003Eb__21_0(RawCardScript c)` | `DeckRuntime.DeckEntries` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:4189` |
| `DeckNameFilter.FillToggleList(List<CollectionFilterToggle> currentList, int maxElemen…` | `Shell/CollectionWindow._deckNameFilter + FilteredDeckIndices` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckNameFilter.Filter(List<CardDeck> collection)` | `CollectionWindow.FilteredDeckIndices` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckNameFilter.SetupFilter(CollectionFilterController<CardDeck> controller)` | `Shell/CollectionWindow._deckNameFilter + FilteredDeckIndices` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckNameFilter._003CSetupFilter_003Eb__1_0(string s)` | `Shell/CollectionWindow._deckNameFilter + FilteredDeckIndices` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4902` |
| `DeckSelectionPopup.Initialize(IEnumerable<CardDeck> decks, CardDeck selectedDeck, DeckInf…` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.OnDestroy()` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.Open()` | `Shell/DeckSelectionPopup.cs` | 文件级(可补行号)＋被多方法共用(8) | `CardPresentation/Shell/DeckSelectionPopup.cs:397` |
| `DeckSelectionPopup.Select(CardDeck deck)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.ToggleDecksDifficultyLabels(bool option)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.TryOpen(object data, GameWindowOptions options)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionPopup.UpdateInfo(CollectionItem<CardDeck> deck, PointerEventData.InputButton…` | `Shell/DeckSelectionPopup.cs` | 文件级(可补行号)＋被多方法共用(8) | `CardPresentation/Shell/DeckSelectionPopup.cs:804` |
| `DeckSelectionPopup._003CTryOpen_003Eb__10_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(8) | — |
| `DeckSelectionTabController.Awake()` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Filter(IEnumerable<CardDeck> decks)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.RandomizeDeck()` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Refresh()` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.ShowOwnDecks(bool state)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.ShowPrebuiltDecks(bool state)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController.Start()` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CAwake_003Eb__6_5(string _)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CFilter_003Eb__12_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CRandomizeDeck_003Eb__7_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CShowOwnDecks_003Eb__10_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckSelectionTabController._003CShowPrebuiltDecks_003Eb__11_0(CardDeck d)` | `Shell/DeckSelectionPopup.cs` | 文件级＋被多方法共用(12) | — |
| `DeckTypeCounterDrawer.Initialize(CardDeck deck)` | `DeckRuntime.SetTypeCounter（TcTermTroops / TcTermStratagems）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckRuntime.cs:2463` |
| `DeckUtility.GetDeckLibrary(CardDeck deck)` | `DeckRules.Validate 内部先建同一份计数` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckUtility.RemoveCardFromDeck(RawCardScript card, CardDeck deck, [In] ref List<Ra…` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.RemoveInvalidCards(CardDeck deck)` | `DeckRules.RemoveInvalidCards（未拥有那一条不适用）` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.ToLocalizedString(DeckError error)` | `DeckRules.Describe + Loc.T` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:578` |
| `DeckUtility.ToRawLocalizationString(DeckError error)` | `DeckRules.Describe 的键表` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:578` |
| `DeckUtility.TryRemoveCardsNotOwned(CardDeck deck, List<RawCardScript> cardsToRemov…` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.TryRemoveInvalidCards(CardDeck deck, List<RawCardScript> removedCards,…` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.TryRemoveInvalidHero(CardDeck deck, List<RawCardScript> removedCards, …` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.ValidateDeck(CardDeck deck, out DeckError error, bool validateOwnershi…` | `DeckRules.Validate（我们的 `validateOwnership` 恒不必要 = 全解锁）` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:443` |
| `DeckUtility.ValidateDeckCards(CardDeck deck, Dictionary<RawCardScript, int> deckLi…` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.ValidateDeckOwnership(CardDeck deck, Dictionary<RawCardScript, int> de…` | `RuleEngine/Core/DeckRules.cs（Validate / RemoveInvalidCards / Describe …` | 文件级(可补行号)＋被多方法共用(6) | `RuleEngine/Core/DeckRules.cs:400` |
| `DeckUtility.ValidateDeckWarlord(CardDeck deck, out DeckError error)` | `DeckRules.Validate 的 NoWarlord / WarlordNotHero` | 仅类.方法(可补行号) | `RuleEngine/Core/DeckRules.cs:443` |
| `ImportDeckPopup.OnDisable()` | `Shell/ImportDeckPopup.cs` | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.OnEnable()` | `Shell/ImportDeckPopup.cs` | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.OnError(string error)` | `Shell/ImportDeckPopup.cs` | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.TryImport()` | `ImportDeckPopup.TryImport（已改）` | 仅类.方法(可补行号) | `CardPresentation/Shell/ImportDeckPopup.cs:367` |
| `ImportDeckPopup.TryImportDeck(CardDeck deck, Action onImport)` | `Shell/ImportDeckPopup.cs（校验通过即入库）` | 文件级＋被多方法共用(4) | — |
| `ImportDeckPopup.ValidateField(string value)` | `ImportDeckPopup` | 仅类/描述(无方法) | — |
| `SelectDeckGameModeMenuContainer.OnClick()` | `CollectionWindow.ChooseDeckMode` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4549` |
| `SelectDecksTab.BackButtonClicked()` | `CollectionWindow.ShouldReturnToDeckModePage / BackButtonClick` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4617` |
| `SelectDecksTab.ChooseTabToOpen()` | `CollectionWindow.SetDeckModeChrome（离线等价：固定两张模式卡）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:4525` |
| `SelectDecksTab.CreateDeck()` | `CollectionData.CreateDeck + PendingNewDeck + GoNewDeck` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionData.cs:320` |
| `SelectDecksTab.ImportDeck()` | `Shell/CollectionWindow 的 Import 钮 → ImportDeckPopup` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5358` |
| `SelectDecksTab.OnClickDeckCollection(IOwnDeckPlayEvent deckPlayEvent)` | `Shell/CollectionWindow.cs 的 Deck 页（BuildDeckList / BuildDeckModePage /…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4161` |
| `SelectDecksTab.ReturnFromImport(PlayModes? gameMode)` | `CollectionData 回程意图（DeckExitSource）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionData.cs:441` |
| `SelectDecksTab.Setup(GameWindow window)` | `CollectionWindow.Build` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:3777` |
| `SelectDecksTab.ToggleSelectDeck(IOwnDeckPlayEvent deckPlayEvent)` | `Shell/CollectionWindow.cs 的 Deck 页（BuildDeckList / BuildDeckModePage /…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4161` |
| `SelectDecksTab.ToggleSelectGameModeDeck(IEnumerable<IOwnDeckPlayEvent> gameModeDecks)` | `Shell/CollectionWindow.cs 的 Deck 页（BuildDeckList / BuildDeckModePage /…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4161` |
| `SelectGameModeDeckTab.DeckContainerClicked(UiContainer<LiveOpsEvent> UIcontainer)` | `Shell/CollectionWindow.BuildDeckModePage（离线等价：固定两张模式卡）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4351` |
| `SelectGameModeDeckTab.Initialize(IEnumerable<IOwnDeckPlayEvent> gameModeEventsThatNeedsDeckS…` | `Shell/CollectionWindow.BuildDeckModePage（离线等价：固定两张模式卡）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4351` |
| `SelectGameModeDeckTab.Reset()` | `Shell/CollectionWindow.BuildDeckModePage（离线等价：固定两张模式卡）` | 仅类.方法(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:4351` |

## 收藏.tsv（A 141，问题 141）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `AlternateArtCardCollectionTab.Awake()` | `Shell/CollectionWindow.cs BuildStylesPage / ChangeStyle / ShownAltArts…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionWindow.cs:2318` |
| `AlternateArtCardCollectionTab.ChangeStyleButton(bool isLeft)` | `CollectionWindow.ChangeStyle(int dir)（dir=-1/+1，环形回绕）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2442` |
| `AlternateArtCardCollectionTab.ConfigureStyle(int indexStyleToUse, bool updateCollectionDisplay)` | `CollectionWindow.ChangeStyle + RebuildStylesPage（logo 走 StyleLogo）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2442` |
| `AlternateArtCardCollectionTab.GetCollection()` | `CollectionWindow.ShownAltArts()（复用 DeckEditorState.VisibleCards 的同一套排序…` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2268` |
| `AlternateArtCardCollectionTab.OnItemSelected(CollectionItem<RawCardScript> item, PointerEventData.In…` | `CollectionWindow.OpenCardDetail（菜单版 CardDetailPopup）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5297` |
| `AlternateArtCardCollectionTab.ToggleShowOwned(bool show)` | `Shell/CollectionWindow.cs BuildStylesPage / ChangeStyle / ShownAltArts…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionWindow.cs:2318` |
| `AlternateArtCardCollectionTab._003CAwake_003Eb__7_0()` | `Shell/CollectionWindow.cs BuildStylesPage / ChangeStyle / ShownAltArts…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionWindow.cs:2318` |
| `AlternateArtCardCollectionTab._003CAwake_003Eb__7_1()` | `Shell/CollectionWindow.cs BuildStylesPage / ChangeStyle / ShownAltArts…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionWindow.cs:2318` |
| `AlternateArtCardCollectionTab._003CGetCollection_003Eb__11_0(RawCardScript c)` | `Shell/CollectionWindow.cs BuildStylesPage / ChangeStyle / ShownAltArts…` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionWindow.cs:2318` |
| `CardArmyFilter.ClearArmyFilter()` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardArmyFilter.ClearFilters()` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardArmyFilter.FillToggleList(List<CollectionFilterToggle> currentList, int listLimit…` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardArmyFilter.FilterItem(RawCardScript card)` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardArmyFilter.FilterToArmy(CardArmy army)` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardArmyFilter.SetFilterToDeck(CardDeck deck)` | `Core/FilterPanelModel.cs（阵营 13 格）+ Deck/DeckEditorState.Filter.Faction` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:90` |
| `CardBackCosmeticArmyFilter.FilterItem(CosmeticItemCardback cosmetic)` | `Shell/CollectionWindow.FilteredCosmoNames → Core/CardbackTable.NamesFo…` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2068` |
| `CardCollectionDisplay.CheckFocusedArmy()` | `CollectionWindow.CheckFocusedArmy（本轮新增，含 WcRefL/T/R/B 常量）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5098` |
| `CardCollectionDisplay.OnListDrag(Vector2 listPos)` | `CollectionWindow: CardsScroll.OnChanged = { RebuildCardsCells; CheckFo…` | 仅类.方法(解析不到) | — |
| `CardCollectionDisplay.RefreshCells()` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CardCollectionDisplay.RefreshCollection()` | `CollectionWindow.RebuildCardsCells 尾部 CheckFocusedArmy（批处理无帧 ⇒ 不延迟）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CardCollectionDisplay.SetCell(ICell cell, int index)` | `CollectionWindow.RebuildCardsCells + BuildCardsCounter` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CardCollectionDisplay.UpdateCardVisuals(CollectionCard card)` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CardCollectionFilterController.SetFiltersToDeck(CardDeck deck)` | `Core/FilterPanelModel.cs（CostRowVisible / HasWarlord）+ Deck/DeckRuntim…` | 文件级(可补行号) | `CardPresentation/Core/FilterPanelModel.cs:601` |
| `CardCollectionTab.GetCards()` | `CollectionWindow.CardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:188` |
| `CardCollectionTab.GetCollection()` | `Deck/DeckEditorState.VisibleCards + SortForDisplay` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:529` |
| `CardCollectionTab.OnItemSelected(CollectionItem<RawCardScript> item, PointerEventData.In…` | `CollectionWindow.OpenCardDetail（含 SetWildcardArmy(card.Faction)）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5297` |
| `CardCollectionTab.Setup(GameWindow window)` | `CollectionWindow.BuildCardsPage（WcArmyDefault=Ultramarines）+ 抽屉 Owned …` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1002` |
| `CardCollectionTab.ToggleShowOwned(bool show)` | `CollectionWindow.RefreshCardsAfterFilter` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2881` |
| `CardCostFilter.FilterItem(RawCardScript card)` | `Deck/DeckEditorState.ClampPoolCost + Filter.Cost/CostMax` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:509` |
| `CardCostFilter.SetFilterToDeck(CardDeck deck)` | `Deck/DeckEditorState.ClampPoolCost + Filter.Cost/CostMax` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:509` |
| `CardInventory.Add(IIdentifiable item, int amount, string instanceId, Dictionary<stri…` | `Shell/Inventory.cs `Inventory<T>.Add`（卡牌背包 = `Inventories.Cards`）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/Inventory.cs:62` |
| `CardInventory.Create(IIdentifiable item, string instanceId, Dictionary<string, strin…` | `Shell/Inventory.cs `Inventory<T>.Create`（卡牌背包 = `Inventories.Cards`）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/Inventory.cs:127` |
| `CardInventory.UpdateItemCollection()` | `Shell/Inventory.cs `Inventory<T>.UpdateItemCollection`（卡牌背包 = `Invento…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/Inventory.cs:249` |
| `CardNameFilter.CheckInputTextSearch(List<RawCardScript> collection, string text)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.ClearFilters()` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.FillToggleList(List<CollectionFilterToggle> currentList, int maxElemen…` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.Filter(List<RawCardScript> collection)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.RemoveBetween(string sourceString, string startTag, string endTag)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.SetupFilter(CollectionFilterController<RawCardScript> controller)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter.TryMatchName(string query, string comparison)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardNameFilter._003CSetupFilter_003Eb__1_0(string s)` | `Deck/DeckEditorState.NameSearchMatch（本轮补「剥 <> 标记」与「稀有度」，删掉我们自加的「按费用数字搜…` | 仅类.方法(可补行号)＋被多方法共用(8) | `CardPresentation/Deck/DeckEditorState.cs:621` |
| `CardRarityFilter.FillToggleList(List<CollectionFilterToggle> currentList, int listLimit…` | `Core/FilterPanelModel.RarityKeys + Deck/DeckEditorState.Filter.Rarity` | 仅类.方法(可补行号) | `CardPresentation/Core/FilterPanelModel.cs:379` |
| `CardRarityFilter.FilterItem(RawCardScript card)` | `Core/FilterPanelModel.RarityKeys + Deck/DeckEditorState.Filter.Rarity` | 仅类.方法(可补行号) | `CardPresentation/Core/FilterPanelModel.cs:379` |
| `CardTypeFilter.ClearFilters()` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardTypeFilter.FillToggleList(List<CollectionFilterToggle> currentList, int listLimit…` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardTypeFilter.FilterItem(RawCardScript card)` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardTypeFilter.SetFilterToDeck(CardDeck deck)` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardTypeFilter.ShowAll()` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardTypeFilter.ShowOnlyHero()` | `Core/FilterPanelModel.TypeKeys + Deck/DeckEditorState.Filter.Type` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Core/FilterPanelModel.cs:416` |
| `CardbackCollectionDisplay.SetCell(ICell cell, int index)` | `Shell/CollectionWindow.RebuildCosmoCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1874` |
| `CardbackCollectionTab.GetCollection()` | `Shell/CollectionWindow（Cosmetics 页）+ Core/CardbackTable + Shell/ItemDi…` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.OnItemSelected(CollectionItem<CosmeticItemCardback> item, PointerEvent…` | `Shell/CollectionWindow（Cosmetics 页）+ Core/CardbackTable + Shell/ItemDi…` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.Setup(GameWindow window)` | `Shell/CollectionWindow（Cosmetics 页）+ Core/CardbackTable + Shell/ItemDi…` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CardbackCollectionTab.ToggleShowOwned(bool show)` | `Shell/CollectionWindow（Cosmetics 页）+ Core/CardbackTable + Shell/ItemDi…` | 仅类/描述(无方法)＋被多方法共用(4) | — |
| `CollectionCard.AnimateIn()` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.Config(RawCardScript item, GameplayVariablesData gameplayVariablesData…` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.HoverHighlight(bool state)` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.OnDestroy()` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.OnRectTransformDimensionsChange()` | `Shell/CollectionWindow.cs `RebuildCardsCells` 的 `scale`（`CardsCellH * …` | 文件级(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.SelectedHighlight(bool state)` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.Start()` | `Shell/CollectionWindow.RebuildCardsCells / PlayCardAnimateIn / PunchCa…` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCard.UpdateCardVisuals(bool isValid, bool isOwned, string counterText)` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionCardCounter.Set(string counterText, bool setGreyscale)` | `CollectionWindow.BuildCardsCounter` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:353` |
| `CollectionCosmetic.Config(CosmeticItemCardback item, GameplayVariablesData gameplayVariab…` | `Shell/CollectionWindow.RebuildCosmoCells + Core/CardbackFace/CardView …` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1874` |
| `CollectionCosmetic.HoverHighlight(bool state)` | `Shell/CollectionWindow.RebuildCosmoCells + Core/CardbackFace/CardView …` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1874` |
| `CollectionDisplay.GetItemCount()` | `CollectionWindow.CardsVisibleCount` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1729` |
| `CollectionDisplay.GetItemObject(T item)` | `Shell/CollectionWindow.cs `CardsCells` / `CardsCellDef(i)`` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:188` |
| `CollectionDisplay.GetItems()` | `Shell/CollectionWindow.cs `CardsCells`（可见序格表）` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:188` |
| `CollectionDisplay.Initialize(List<T> collection, GameplayVariablesData gameplayVariables…` | `Shell/CollectionWindow 的四个页（自建网格 + MenuScroll），无泛型基类` | 仅类/描述(无方法) | — |
| `CollectionDisplay.OnListDrag(Vector2 listPos)` | `CollectionWindow（Cards 页直接调 CheckFocusedArmy）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5098` |
| `CollectionDisplay.RefreshCells()` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionDisplay.RefreshCollection()` | `CollectionWindow.RefreshCardsAfterFilter/RebuildCardsCells + RefreshCa…` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2881` |
| `CollectionDisplay.Select(CollectionItem<T> collectable, PointerEventData.InputButton but…` | `Shell/CollectionWindow.cs `AddHit(... → OpenCardDetail)`` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Shell/CollectionWindow.cs:5297` |
| `CollectionDisplay.SetCell(ICell cell, int index)` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionDisplay.ToggleFilters(bool state)` | `CollectionWindow.ToggleFilters/ApplyDrawerSlide` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:2792` |
| `CollectionDisplay.UpdateCollection(List<T> collection)` | `Shell/CollectionWindow 的四个页（自建网格 + MenuScroll），无泛型基类` | 仅类/描述(无方法) | — |
| `CollectionFilter.ClearFilters()` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.FillToggleList(List<CollectionFilterToggle> currentList, int maxElemen…` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.Filter(List<T> collection)` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.NotifyChange()` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.OnFilterChange()` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilter.SetupFilter(CollectionFilterController<T> controller)` | `Core/FilterPanelModel + Deck/DeckEditorState.Filter（无基类，按行为对照）` | 仅类.方法(可补行号)＋被多方法共用(6) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `CollectionFilterController.Filter(List<T> collection)` | `Deck/DeckEditorState.VisibleCards（跨筛选 AND）` | 仅类.方法(可补行号) | `CardPresentation/Deck/DeckEditorState.cs:529` |
| `CollectionFilterController.NotifyFilterChange()` | `Core/FilterPanelModel.cs（每次重建 cell 列表 ⇒ chip 随之一并重建）` | 文件级 | — |
| `CollectionFilterToggle.ForceOff()` | `Core/FilterPanelModel.Cell + Deck/DeckRuntime.RefreshFilterCells` | 仅类.方法(可补行号)＋被多方法共用(4) | `CardPresentation/Core/FilterPanelModel.cs:647` |
| `CollectionFilterToggle.HideLabel()` | `Core/FilterPanelModel.Cell + Deck/DeckRuntime.RefreshFilterCells` | 仅类.方法(可补行号)＋被多方法共用(4) | `CardPresentation/Core/FilterPanelModel.cs:647` |
| `CollectionFilterToggle.Initialize(FilterOptions option)` | `Core/FilterPanelModel.Cell + Deck/DeckRuntime.RefreshFilterCells` | 仅类.方法(可补行号)＋被多方法共用(4) | `CardPresentation/Core/FilterPanelModel.cs:647` |
| `CollectionFilterToggle.SetValue(bool value, bool notify)` | `Core/FilterPanelModel.Cell + Deck/DeckRuntime.RefreshFilterCells` | 仅类.方法(可补行号)＋被多方法共用(4) | `CardPresentation/Core/FilterPanelModel.cs:647` |
| `CollectionItem.AnimateIn()` | `CollectionWindow.PlayCardAnimateIn` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1482` |
| `CollectionItem.Config(T item, CollectionDisplay<T> display, bool animate, bool isSele…` | `CollectionWindow.RebuildCardsCells` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionItem.Config(T item, GameplayVariablesData gameplayVariablesData)` | `CollectionWindow.RebuildCardsCells（Create/SetData）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:1510` |
| `CollectionItem.HoverHighlight(bool state)` | `CollectionWindow.AttachCardHover / MenuDraw.DeckCell` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5025` |
| `CollectionItem.OnPointerClick(PointerEventData eventData)` | `CollectionWindow.AddHit（WindowButton.onClick）` | 仅类.方法(可补行号) | `CardPresentation/Shell/WindowButton.cs:17` |
| `CollectionItem.OnPointerEnter(PointerEventData eventData)` | `CollectionWindow.AttachCardHover（onEnter）/ MenuDraw.DeckCell 1.05 缩放` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5025` |
| `CollectionItem.OnPointerExit(PointerEventData eventData)` | `CollectionWindow.AttachCardHover（onExit）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5025` |
| `CollectionItem.SelectedHighlight(bool state)` | `CollectionWindow.PunchCardCell` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5168` |
| `CollectionScreen.BackButtonClick()` | `Shell/CollectionWindow（MainMenuSubmenuWindow）+ Shell/MainMenuRuntime（导…` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MainMenuRuntime.cs:1941` |
| `CollectionScreen.OnDisable()` | `Shell/CollectionWindow（MainMenuSubmenuWindow）+ Shell/MainMenuRuntime（导…` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MainMenuRuntime.cs:1941` |
| `CollectionScreen.OnEnable()` | `Shell/CollectionWindow（MainMenuSubmenuWindow）+ Shell/MainMenuRuntime（导…` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MainMenuRuntime.cs:1941` |
| `CollectionScreen.Open()` | `Shell/CollectionWindow（MainMenuSubmenuWindow）+ Shell/MainMenuRuntime（导…` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MainMenuRuntime.cs:1941` |
| `CollectionScreen.ReturnFromImportDeck(PlayModes? gameMode)` | `Shell/CollectionWindow（MainMenuSubmenuWindow）+ Shell/MainMenuRuntime（导…` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MainMenuRuntime.cs:1941` |
| `CollectionTab.GetCollection()` | `Shell/CollectionWindow（四页 builder）+ Shell/CollectionTabPage.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionTabPage.cs:30` |
| `CollectionTab.GetItems()` | `Shell/CollectionWindow（四页 builder）+ Shell/CollectionTabPage.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionTabPage.cs:30` |
| `CollectionTab.OnItemSelected(CollectionItem<T> item, PointerEventData.InputButton bu…` | `Shell/CollectionWindow（四页 builder）+ Shell/CollectionTabPage.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionTabPage.cs:30` |
| `CollectionTab.OnOpen()` | `Shell/CollectionWindow（四页 builder）+ Shell/CollectionTabPage.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionTabPage.cs:51` |
| `CollectionTab.Setup(GameWindow window)` | `Shell/CollectionWindow（四页 builder）+ Shell/CollectionTabPage.cs` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/CollectionTabPage.cs:56` |
| `GameModesIconCollection.GetIcon(PlayModes? playMode)` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `GameModesIconCollection.Release()` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `GameModesIconCollection.Release(PlayModes? playMode)` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `GameModesIconCollection.ReleaseAll()` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `GameModesIconCollection.TryGetIcon(PlayModes playModeToCheck, out Sprite iconToReturn)` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `GameModesIconCollection.TryRelease(PlayModes playModeToCheck)` | `Shell/MenuDraw.GameModeIconFile（静态映射：0→classic / 13→skirmish）` | 仅类.方法(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/MenuDraw.cs:3180` |
| `Inventory.Add(IIdentifiable item, int amount, string instanceId, Dictionary<stri…` | `Shell/Inventory.cs `Inventory<T>.Add`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:62` |
| `Inventory.Create(IIdentifiable item, string instanceId, Dictionary<string, strin…` | `Shell/Inventory.cs `Inventory<T>.Create`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:127` |
| `Inventory.Get(Func<T, bool> predicate)` | `Shell/Inventory.cs `Inventory<T>.Get`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:142` |
| `Inventory.Get(IIdentifiable item, bool autoCreate)` | `Shell/Inventory.cs `Inventory<T>.Get`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:142` |
| `Inventory.Get(string id)` | `Shell/Inventory.cs `Inventory<T>.Get`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:142` |
| `Inventory.GetItemInstances(bool useQuantity)` | `Shell/Inventory.cs `Inventory<T>.GetItemInstances`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:188` |
| `Inventory.GetItems()` | `Shell/Inventory.cs `Inventory<T>.GetItems`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:184` |
| `Inventory.GetWhere(Func<T, bool> predicate)` | `Shell/Inventory.cs `Inventory<T>.GetWhere`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:200` |
| `Inventory.OnItemCountModify(T item, int amount)` | `Shell/Inventory.cs `Inventory<T>.OnItemCountModify`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:212` |
| `Inventory.OnItemCountModifyInternal(IIdentifiable itemChanged, int amountChanged…` | `Shell/Inventory.cs `Inventory<T>.OnItemCountModifyInternal`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:229` |
| `Inventory.RegisterSubset(Type type, InventorySubset set)` | `Shell/Inventory.cs `Inventory<T>.RegisterSubset`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:252` |
| `Inventory.TryGetItem(Func<T, bool> predicate, out IItemInfo<T> item)` | `Shell/Inventory.cs `Inventory<T>.TryGetItem`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:161` |
| `Inventory.TryGetItem(string id, out IItemInfo<T> item)` | `Shell/Inventory.cs `Inventory<T>.TryGetItem`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:161` |
| `Inventory.UpdateItemCollection()` | `Shell/Inventory.cs `Inventory<T>.UpdateItemCollection`` | 文件级(可补行号)＋被多方法共用(11) | `CardPresentation/Shell/Inventory.cs:249` |
| `InventorySubset.Add(IItemInfo<IIdentifiable> value)` | `Shell/Inventory.cs `InventorySubset<T>.Add`` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/Inventory.cs:62` |
| `InventorySubset.AddRange(IEnumerable<IItemInfo<IIdentifiable>> items)` | `Shell/Inventory.cs `InventorySubset<T>.AddRange`` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/Inventory.cs:343` |
| `InventorySubset.Get(string id)` | `Shell/Inventory.cs `InventorySubset<T>.Get`` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/Inventory.cs:142` |
| `InventorySubset.TryGetValue(string id, out IItemInfo<IIdentifiable> item)` | `Shell/Inventory.cs `InventorySubset<T>.TryGetValue`` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/Inventory.cs:326` |
| `ToggleCollectionFilter.FillToggleList(List<CollectionFilterToggle> currentList, int listLimit…` | `Core/FilterPanelModel.cs `AppendChips`（chip = 同一批 cell 的值拷贝）+ `Click` …` | 文件级(可补行号) | `CardPresentation/Core/FilterPanelModel.cs:829` |
| `ToggleCollectionFilter.Filter(List<T> collection)` | `Deck/DeckEditorState.cs `DeckFilter.*Ok`（并集）+ Core/FilterPanelModel.Cl…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckEditorState.cs:241` |
| `ToggleCollectionFilter.FilterItem(T card)` | `Deck/DeckEditorState.cs `DeckFilter.*Ok`（并集）+ Core/FilterPanelModel.Cl…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckEditorState.cs:97` |
| `ToggleCollectionFilter.OnFilterChange()` | `Deck/DeckEditorState.cs `DeckFilter.*Ok`（并集）+ Core/FilterPanelModel.Cl…` | 文件级(可补行号)＋被多方法共用(3) | `CardPresentation/Deck/DeckEditorState.cs:97` |
| `WhereToGetCardsSection.Awake()` | `Shell/WhereToGetCardsPopup.cs（折叠箭头 `QToggle` / `ArtToggleOn·Off` / `To…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/WhereToGetCardsPopup.cs:163` |
| `WhereToGetCardsSection.Initialize(WhereToGetExpansionCardsData.ExpansionCardsData data, Actio…` | `Shell/WhereToGetCardsPopup.cs `Build()` + `SectionData/SectionHeight/G…` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/WhereToGetCardsPopup.cs:444` |
| `WhereToGetCardsSection.OnDestroy()` | `Shell/WhereToGetCardsPopup.cs（窗销毁即释放，无监听残留）` | 文件级＋被多方法共用(6) | — |
| `WhereToGetCardsSection.Release()` | `Shell/WhereToGetCardsPopup.cs（资源随窗树销毁）` | 文件级＋被多方法共用(6) | — |
| `WhereToGetCardsSection.Reset()` | `Shell/WhereToGetCardsPopup.cs（每次 `Show` 重建段）` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/WhereToGetCardsPopup.cs:281` |
| `WhereToGetCardsSection.SetVisibility(bool option)` | `Shell/WhereToGetCardsPopup.cs 折叠（`:54` 说明 + `:160-166` 两态图/色）` | 注释＋被多方法共用(6) | — |
| `WildcardDisplay.Initialize(CardArmy army)` | `CollectionWindow.SetWildcardArmy` | 仅类.方法(可补行号) | `CardPresentation/Shell/CollectionWindow.cs:5042` |

## 商店.tsv（A 114，问题 113）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `BaseOfferPopup.Close()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OnDisable()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OnEnable()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.OnOfferTimerEnded()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.Open()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级(可补行号)＋被多方法共用(14) | `CardPresentation/Shell/BaseOfferPopup.cs:668` |
| `BaseOfferPopup.OverrideVisualRewards(List<RewardInfo> rewards)` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.ProcessPurchase()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级(可补行号)＋被多方法共用(14) | `CardPresentation/Shell/BaseOfferPopup.cs:1050` |
| `BaseOfferPopup.Refresh()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetAvailableText(bool hasLimit, int purchasedCount)` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetBadgeText()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetEventData(IShopOffer liveOps)` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.SetTimer()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup.ToFocus()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级＋被多方法共用(14) | — |
| `BaseOfferPopup._003CProcessPurchase_003Eg__Callback_007C29_0()` | `Shell/BaseOfferPopup.cs（1184 行）` | 文件级(可补行号)＋被多方法共用(14) | `CardPresentation/Shell/BaseOfferPopup.cs:1050` |
| `CardShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs（Cards 页）` | 文件级 | — |
| `ContainerOfferEvent.OpenOfferContainer(string instanceId, Action<IReadOnlyList<RewardInfo>…` | `Shell/BoosterPackOpenWindow.cs `Create(...)` + `Shell/ShopWindow.DoBuy…` | 文件级 | — |
| `ContainerOfferEvent.TryPurchaseOffer(Action<IReadOnlyList<RewardInfo>> onComplete, Action …` | `Shell/ShopWindow.DoBuy + Shell/BaseOfferPopup.ProcessPurchase` | 仅类.方法(可补行号) | `CardPresentation/Shell/BaseOfferPopup.cs:1050` |
| `CosmeticsShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs（Cosmetics 页）` | 文件级 | — |
| `GeneralOfferPopupDrawer.DoReset()` | `Shell/BaseOfferPopup.cs + Shell/ItemDrawer.cs` | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawer.DrawItemReward(ItemDrawer itemDrawer, ObtainableItem itemToDraw, ref L…` | `Shell/BaseOfferPopup.cs + Shell/ItemDrawer.cs` | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawer.DrawRewards(IEnumerable<RewardInfo> rewards)` | `Shell/BaseOfferPopup.cs + Shell/ItemDrawer.cs` | 文件级＋被多方法共用(3) | — |
| `GeneralOfferPopupDrawerBase.DoReset()` | `Shell/BaseOfferPopup.cs（奖励区）` | 文件级＋被多方法共用(4) | — |
| `GeneralOfferPopupDrawerBase.DrawRewards(IEnumerable<RewardInfo> rewards)` | `Shell/BaseOfferPopup.cs（奖励区）` | 文件级＋被多方法共用(4) | — |
| `GeneralOfferPopupDrawerBase.Initialize(ShopOfferBase shopOfferData)` | `Shell/BaseOfferPopup.cs（奖励区）` | 文件级＋被多方法共用(4) | — |
| `GeneralOfferPopupDrawerBase.ReleaseAssets()` | `Shell/BaseOfferPopup.cs（奖励区）` | 文件级＋被多方法共用(4) | — |
| `InAppOfferContainer.OnClick()` | `Shell/ShopWindow.DoBuy（容器档 → 开包窗）` | 仅类.方法(解析不到) | — |
| `InAppOfferContainer.OnInitialize(IShopOffer offer)` | `Shell/OfferContainer.cs（`Content` + `BaseOfferPopup.Show(prefab, conte…` | 文件级 | — |
| `InAppOfferContainer.Refresh()` | `Shell/OfferContainer.cs（价格钮刷新）` | 文件级 | — |
| `ItemShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs（Items 页）` | 文件级 | — |
| `ItemShopTab.DoOfferReordering(ref IReadOnlyList<IShopOffer> offers)` | `Shell/ShopTabPage.cs（Items 页）` | 文件级 | — |
| `PreviewOfferButton.Initialize(IShopOffer offer)` | `Shell/OfferContainer.cs（点开详情）` | 文件级 | — |
| `PriceDisplay.FixLayout()` | `Shell/ShopTabPage.cs 价格文本（固定矩形，无 LayoutGroup）` | 文件级＋被多方法共用(4) | — |
| `PriceDisplay.Setup(CurrencyValue value)` | `Shell/ShopTabPage.cs（金币图标 + 价格文本）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PriceDisplay.Setup(Price value)` | `Shell/ShopTabPage.cs（`o.Price` 串 + 图标）` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PriceDisplay.Setup(string buttonText, Sprite iconSprite)` | `ShopTabPage 的价格文本（`--` 与图标有、`<s>` 没有）` | 仅类/描述(无方法) | — |
| `PriceDisplay.SetupFree()` | `Shell/ShopTabPage.cs 的价格文本（`o.Free ⇒ Loc.T("MainMenu/Free")`）；开关字段 = `…` | 文件级＋被多方法共用(4) | — |
| `PriceDisplay.TryInsertStrikeThrough()` | `Shell/ShopTabPage.cs 的价格文本（`o.StrikeThrough ⇒ "<s>"+txt`）；开关字段 = `Shop…` | 文件级＋被多方法共用(4) | — |
| `PriceDisplayButton.Setup(CurrencyValue value, bool disableIfNotEnoughCurrency)` | `Shell/ShopTabPage.cs 的购买钮` | 文件级(可补行号) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PriceDisplayButton.Setup(IShopOffer shopEvent)` | `Shell/ShopTabPage.cs 的购买钮` | 文件级(可补行号) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PriceDisplayButton.Setup(Price value, bool disableIfNotEnoughCurrency)` | `Shell/ShopTabPage.cs 的购买钮` | 文件级(可补行号) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PriceDisplayButton.Setup(string buttonText)` | `Shell/ShopTabPage.cs 的购买钮` | 文件级(可补行号) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `PuchasePremiumArmyContainer.Click(bool enabledOption)` | `Shell/PurchasePremiumWindow.cs（阵营格）` | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.Initialize(PurchasePremiumWindow.PremiumArmyPurchaseData data, Purchas…` | `Shell/PurchasePremiumWindow.cs（阵营格）` | 文件级(可补行号)＋被多方法共用(5) | `CardPresentation/Shell/PurchasePremiumWindow.cs:384` |
| `PuchasePremiumArmyContainer.OnDisable()` | `Shell/PurchasePremiumWindow.cs（阵营格）` | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.OnEnable()` | `Shell/PurchasePremiumWindow.cs（阵营格）` | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyContainer.TryHighlight(CardArmy cardArmy)` | `Shell/PurchasePremiumWindow.cs（阵营格）` | 文件级＋被多方法共用(5) | — |
| `PuchasePremiumArmyInfo.Initialize(PurchasePremiumWindow.PremiumArmyPurchaseData armyData, Pur…` | `Shell/PurchasePremiumWindow.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/PurchasePremiumWindow.cs:384` |
| `PuchasePremiumArmyInfo.OnDisable()` | `Shell/PurchasePremiumWindow.cs` | 文件级＋被多方法共用(4) | — |
| `PuchasePremiumArmyInfo.OnEnable()` | `Shell/PurchasePremiumWindow.cs` | 文件级＋被多方法共用(4) | — |
| `PuchasePremiumArmyInfo.PurchaseButtonClick()` | `Shell/PurchasePremiumWindow.cs` | 文件级(可补行号)＋被多方法共用(4) | `CardPresentation/Shell/PurchasePremiumWindow.cs:924` |
| `PurchasePremiumWindow.ClearCurrentArmyContainers()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.CloseButtonClick()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.FetchData()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.FocusOnArmy(CardArmy cardArmy)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级(可补行号)＋被多方法共用(13) | `CardPresentation/Shell/PurchasePremiumWindow.cs:418` |
| `PurchasePremiumWindow.Initialize()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级(可补行号)＋被多方法共用(13) | `CardPresentation/Shell/PurchasePremiumWindow.cs:384` |
| `PurchasePremiumWindow.OnDisable()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnEnable()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnPurChaseCompleted(string _)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.OnPurchasedFailed(string purchaseID)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.SelectArmy(PremiumArmyPurchaseData armyData)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级(可补行号)＋被多方法共用(13) | `CardPresentation/Shell/PurchasePremiumWindow.cs:446` |
| `PurchasePremiumWindow.TryOpen(object data, GameWindowOptions options)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.TryPurchase(Action<string> onPurchase, Action<string> onFailPurchase)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow.TryPurchase(PremiumArmyPurchaseData purchaseArmyData)` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchasePremiumWindow._003COnEnable_003Eb__11_0()` | `Shell/PurchasePremiumWindow.cs（1087 行）` | 文件级＋被多方法共用(13) | — |
| `PurchaseableOfferContainer.OnClick()` | `Shell/ShopWindow.DoBuy` | 仅类.方法(解析不到) | — |
| `PurchaseableOfferContainer.OnInitialize(IShopOffer offer)` | `Shell/OfferContainer.cs` | 文件级 | — |
| `PurchaseableOfferContainer.Refresh()` | `Shell/OfferContainer.cs` | 文件级 | — |
| `ShopContainer.Open(string instanceId, Action<IReadOnlyList<RewardInfo>> onComplete, …` | `Shell/BoosterPackOpenWindow.cs `Create`` | 文件级(可补行号) | `CardPresentation/Shell/BoosterPackOpenWindow.cs:397` |
| `ShopContainerV2.OnAfterPurchase(IShopOffer offer, IReadOnlyList<RewardInfo> rewards, A…` | `Shell/BaseOfferPopup.cs `ProcessPurchase()`` | 文件级(可补行号) | `CardPresentation/Shell/BaseOfferPopup.cs:1050` |
| `ShopContainerV2.Open(string instanceId, Action<IReadOnlyList<RewardInfo>> onComplete, …` | `Shell/BoosterPackOpenWindow.cs `Create`` | 文件级(可补行号) | `CardPresentation/Shell/BoosterPackOpenWindow.cs:397` |
| `ShopItemContainer.Initialize(object payload)` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnDisable()` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnEnable()` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.OnPurchase(bool success)` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.SetRefreshCallback(Action callback)` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer.TryPurchase()` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer._003CTryPurchase_003Eb__2_0()` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopItemContainer._003CTryPurchase_003Eb__2_1()` | `Shell/ShopTabPage.cs（每格）+ Shell/OfferContainer.cs` | 文件级＋被多方法共用(8) | — |
| `ShopOfferContainer.CallRefreshCallback()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnClick()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnDestroy()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnInitialize(IShopOffer shopItem)` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnOfferTimerEnded()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OnPurchase(bool success)` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.OpenContainer()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.Refresh()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.RefreshExpire()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetAvailableText(bool hasLimit, int purchasedCount)` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetBadgeText()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer.SetRefreshCallback(Action callback)` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopOfferContainer._003CRefreshExpire_003Eb__15_0()` | `Shell/OfferContainer.cs（1424 行）` | 文件级＋被多方法共用(13) | — |
| `ShopStore.GetBaseData()` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopStore.GetBaseSave()` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopStore.GetPurchaseCount(string id)` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopStore.Initialize()` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopStore.RemoveFinishedOffer()` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopStore.TryPurchase(IShopOffer offer, Action onComplete, Action onError)` | `Shell/ShopData.cs（三页签的静态表）` | 文件级＋被多方法共用(6) | — |
| `ShopTab.CheckTimer(out DateTime timer)` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.CreateOffers()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.DoOfferReordering(ref IReadOnlyList<IShopOffer> offers)` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.OnOpen()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.RefreshAfterExpire()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.RefreshOffers()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.SetEventTab(ILiveOps liveOps)` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.SetStore()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopTab.Setup(GameWindow window)` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:238` |
| `ShopTab.ToFocus()` | `Shell/ShopTabPage.cs（Setup / Buy / Dump）` | 文件级(可补行号)＋被多方法共用(10) | `CardPresentation/Shell/ShopTabPage.cs:864` |
| `ShopWindow.Close()` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级＋被多方法共用(6) | — |
| `ShopWindow.CreateStoreTab(IStore store)` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级＋被多方法共用(6) | — |
| `ShopWindow.GetStartingTab()` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级＋被多方法共用(6) | — |
| `ShopWindow.OnShopRefresh()` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级＋被多方法共用(6) | — |
| `ShopWindow.Open()` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级(可补行号)＋被多方法共用(6) | `CardPresentation/Shell/ShopWindow.cs:142` |
| `ShopWindow.SetupTabs()` | `Shell/ShopWindow.cs（+ Shell/ShopTabPage.cs 的三个页签）` | 文件级＋被多方法共用(6) | — |

## 轮抽.tsv（A 99，问题 99）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `DraftEventBase.FinishRun(Action onFinish)` | `Shell/DraftModeWindow.cs `FinishRun`` | 文件级(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2244` |
| `DraftEventBase.GetBaseData()` | `DraftModeWindow（`DraftEventData` 等价常量）` | 仅类/描述(无方法) | — |
| `DraftEventBase.GetBaseSave()` | `DraftModeWindow `DraftRunSave`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:716` |
| `DraftEventBase.GetCardsFromPacks(DraftEventSaveGame desiredSaveGame)` | `Shell/DraftPacks.cs` | 文件级 | — |
| `DraftEventBase.GetEventDeck()` | `DraftModeWindow `BuildClosedDeck`（DraftPacks）` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftPacks.cs:372` |
| `DraftEventBase.GetPossibleWarlords()` | `DraftModeWindow（战将池）` | 仅类/描述(无方法) | — |
| `DraftEventBase.GetRandomPack(RawCardScript warlord, int packCount)` | `DraftModeWindow 发牌 / DraftPacks` | 仅类/描述(无方法) | — |
| `DraftEventBase.GetRandomWarlords()` | `DraftModeWindow（选战将）` | 仅类/描述(无方法) | — |
| `DraftEventBase.LoadEventData(LiveOpsEventData data, LiveOpsEventPersistence saveGame,…` | `DraftModeWindow `ResumeRun`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1088` |
| `DraftEventBase.OnMatchFinished(float matchWon, RankedMatchResult result, Action<Onlin…` | `DraftModeWindow `IngestBattleResults`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2258` |
| `DraftEventBase.OnMatchWon()` | `DraftModeWindow `UpdateState`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2190` |
| `DraftEventBase.PickPack(PrebuiltPack pack, Action onSuccess, Action onFail)` | `DraftModeWindow（选包）` | 仅类/描述(无方法) | — |
| `DraftEventBase.PickWarlord(RawCardScript warlord, Action onSuccess, Action onFail)` | `DraftModeWindow（选战将）` | 仅类/描述(无方法) | — |
| `DraftEventBase.ReRollPack(Action onSuccess, Action onFail)` | `DraftModeWindow:2971 `ReRollPacks`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2971` |
| `DraftEventBase.ResetCollectedDraft(Action onComplete)` | `DraftModeWindow（重置本轮）` | 仅类/描述(无方法) | — |
| `DraftEventBase.SaveCards(TS desiredSaveGame, List<LoadableReference<RawCardScript>> c…` | `DraftModeWindow `SaveRunNow`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:812` |
| `DraftEventBase.SelectEvent()` | `DraftModeWindow `Open`` | 仅类/描述(无方法) | — |
| `DraftEventBase.UpdateState()` | `DraftModeWindow:2190 `UpdateState`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2190` |
| `DraftEventBase.ValidateEventDeck(CardDeck deck, bool validateOwnership)` | `DraftModeWindow（轮抽卡组不走常规构筑校验）` | 仅类/描述(无方法) | — |
| `DraftEventBase._003CGetPossibleWarlords_003Eb__20_1(RawCardScript warlord)` | `DraftModeWindow（战将过滤）` | 仅类/描述(无方法) | — |
| `DraftEventBase._003CGetRandomWarlords_003Eb__19_1(RawCardScript wl)` | `DraftModeWindow（战将池过滤）` | 仅类/描述(无方法) | — |
| `DraftEventData.GetReRollCosts(int currentReRolls)` | `DraftModeWindow `ReRollCost`（规则照抄）` | 仅类/描述(无方法) | — |
| `DraftMenuContainer.DoOperationsOnDraftState(DraftModeState newDraftState)` | `Shell/MainMenuRuntime.cs `RefreshDraftModeCard`（每帧比状态）` | 文件级(可补行号) | `CardPresentation/Shell/MainMenuRuntime.cs:3153` |
| `DraftMenuContainer.OnDestroy()` | `MainMenuRuntime（节点随窗销毁）` | 仅类/描述(无方法) | — |
| `DraftMenuContainer.OnDraftStateChange(DraftModeState newDraftState)` | `MainMenuRuntime（轮询替代）` | 仅类/描述(无方法) | — |
| `DraftMenuContainer.OnInitialize(LiveOpsEvent playEvent)` | `MainMenuRuntime（入口建时取一次）` | 仅类/描述(无方法) | — |
| `DraftMenuContainer.SetVictoryCounters()` | `MainMenuRuntime 胜负计数（`String.Format("{0}/{1}", CurrentStage, draftRoun…` | 仅类.方法(解析不到) | — |
| `DraftModeDeckInfoEnergyBalancePanel.Initialize(CardDeck cardDeck)` | `DraftModeWindow `BuildDeckInfoPanel` 的能量页（`_deckEnergyLabel` / `_cards…` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoEnergyBalancePanel.ToggleCardCounter(bool option)` | `DraftModeWindow `showCardCounter` 参数` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoPanel.Initialize(IDraftEvent draft, bool showCardCounter)` | `DraftModeWindow `BuildDeckInfoPanel`（三 toggle 页）` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1818` |
| `DraftModeDeckInfoPanel.Start()` | `DraftModeWindow（`Start` 等价 = 建时刷）` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoPanel.ToggleAllianceInfo(bool value)` | `DraftModeWindow 联盟页 toggle` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoPanel.ToggleCardList(bool value)` | `DraftModeWindow 卡表页 toggle` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoPanel.ToggleEnergyInfo(bool value)` | `DraftModeWindow 能量页 toggle` | 仅类/描述(无方法) | — |
| `DraftModeDeckInfoPanel.UpdateData()` | `DraftModeWindow `UpdateData`（胜场/败场/卡组）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.Awake()` | `DraftModeWindow `Awake`/`Build`` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.CloseButtonOnClick()` | `DraftModeWindow `Close`` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.GetAvailableWarlords()` | `DraftModeWindow（从 `DraftPacks` 取战将池）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.GetMenuWarlordImage()` | `DraftModeWindow（菜单战将立绘）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.GetStartingTab()` | `DraftModeWindow（`GetTabFromState(current)`）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.GetTabFromState(DraftModeState state)` | `DraftModeWindow `GoToState` 的路由表` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2101` |
| `DraftModeMenuDemo.GoToCurrentState()` | `DraftModeWindow `UpdateState` 后切页` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2190` |
| `DraftModeMenuDemo.GoToState(DraftModeState state)` | `DraftModeWindow `GoToState(state)`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:2101` |
| `DraftModeMenuDemo.OnDestroy()` | `DraftModeWindow（窗销毁即解绑）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.Open()` | `DraftModeWindow `Open`` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.ReleaseMenuWarlordImage()` | `DraftModeWindow（无立绘释放）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.ResetEventProgress()` | `DraftModeWindow（重置入口）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.TempGetCardBack()` | `DraftModeWindow（卡背图已建）` | 仅类/描述(无方法) | — |
| `DraftModeMenuDemo.ToggleDeckInfo(bool state)` | `DraftModeWindow `ToggleDeckInfo`` | 仅类/描述(无方法) | — |
| `DraftModeMenuOngoingState.OnDisable()` | `DraftModeWindow（进行页按钮随页销毁）` | 仅类/描述(无方法) | — |
| `DraftModeMenuOngoingState.OnEnable()` | `同上` | 间接指认 | — |
| `DraftModeMenuOngoingState.OnOpen()` | `DraftModeWindow `BuildOngoingPage`/`UpdateState`` | 仅类/描述(无方法) | — |
| `DraftModeMenuOngoingState.PlayButtonOnClick()` | `DraftModeWindow `ResignButtonClick`（确认弹窗）」` | 仅类/描述(无方法) | — |
| `DraftModeMenuOngoingState.PlayDebugButtonOnClick()` | `同上（确认分支）` | 间接指认 | — |
| `DraftModeMenuOngoingState.Resign()` | `DraftModeWindow `PlayButtonOnClick`（进对局）` | 仅类/描述(无方法) | — |
| `DraftModeMenuOngoingState._003CResignButtonClick_003Eb__16_0(GameObject _)` | `含在 89` | 间接指认 | — |
| `DraftModeMenuOngoingState._003CResign_003Eb__17_0()` | `含在 90` | 间接指认 | — |
| `DraftModeMenuPayStateDemo.ChangeState(States stateToChange)` | `DraftModeWindow `BuildPayPage` + 状态切页` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1224` |
| `DraftModeMenuPayStateDemo.FreeButtonOnClick()` | `DraftModeWindow 免费按钮` | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.OnClose()` | `DraftModeWindow（关页）` | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.OnDisable()` | `同上` | 间接指认 | — |
| `DraftModeMenuPayStateDemo.OnEnable()` | `同上` | 间接指认 | — |
| `DraftModeMenuPayStateDemo.OnOpen()` | `DraftModeWindow `BuildPayPage`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1224` |
| `DraftModeMenuPayStateDemo.SetAllButtonsInteractable(bool interactable)` | `DraftModeWindow（只有免费一颗）` | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.ToFocus()` | `DraftModeWindow（页聚焦）` | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo.Update()` | `DraftModeWindow `Update` 的免费按钮刷新` | 仅类/描述(无方法) | — |
| `DraftModeMenuPayStateDemo._003CFreeButtonOnClick_003Eb__24_0()` | `含在 97` | 间接指认 | — |
| `DraftModeMenuPayStateDemo._003CFreeButtonOnClick_003Eb__24_1()` | `含在 97` | 间接指认 | — |
| `DraftModeOngoingStageVisualization.ToggleContent(bool option)` | `DraftModeWindow `RebuildStageMarks`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1714` |
| `DraftModeSelectPacksStateDemo.InitWithSaveGameData()` | `DraftModeWindow `BuildPacksPage`/`OnOpen`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1559` |
| `DraftModeSelectPacksStateDemo.OnDestroy()` | `DraftModeWindow（页销毁即清）` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.OnOpen()` | `DraftModeWindow `OpenPacksPage`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.OnPackConfirmed()` | `DraftModeWindow `OnPackConfirmed`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.OnPackFailed()` | `DraftModeWindow `OnPackFailed`（弹窗 + 重开包）` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.OnReRollFailed()` | `DraftModeWindow `OnReRollFailed`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.OnReRollSuccess()` | `DraftModeWindow `OnReRollSuccess`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.PackPreview(PrebuiltPack selectedPack)` | `DraftModeWindow `ReRollClicked`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.PackSelected(PrebuiltPackUI selectedPack)` | `DraftModeWindow `RefreshReRollPrice`（规则照抄）` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.ReRollClicked()` | `DraftModeWindow `PackSelected`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.RefreshReRollPrice()` | `DraftModeWindow `PackPreview`（多卡展示窗）` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.ResetPacksContainer()` | `DraftModeWindow `ResetPacksContainer`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.ShowPacks()` | `DraftModeWindow `ShowPacks`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo.UpdateDeckInfoPanel()` | `DraftModeWindow `UpdateDeckInfoPanel`` | 仅类/描述(无方法) | — |
| `DraftModeSelectPacksStateDemo._003CPackSelected_003Eb__27_0()` | `含在 126` | 间接指认 | — |
| `DraftModeSelectPacksStateDemo._003CShowPacks_003Eb__26_0()` | `含在 129` | 间接指认 | — |
| `DraftModeWarlordSelectionStateDemo.CheckWarlordSelectionFinished()` | `DraftModeWindow `BuildWarlordPage` 的 continue 按钮绑定` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1258` |
| `DraftModeWarlordSelectionStateDemo.ContinueButtonOnClick()` | `DraftModeWindow（页销毁）` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.OnDestroy()` | `DraftModeWindow `OpenWarlordPage`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.OnOpen()` | `DraftModeWindow `ContinueButtonOnClick`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.OnWarlordFailed()` | `DraftModeWindow `CheckWarlordSelectionFinished`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.OnWarlordPicked()` | `DraftModeWindow `OnWarlordPicked`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.SelectWarlord(RawCardScript card)` | `DraftModeWindow `OnWarlordFailed`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.Setup(GameWindow window)` | `DraftModeWindow `SelectWarlord`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo.ToggleContinueButton(bool option)` | `DraftModeWindow `ToggleContinueButton`` | 仅类/描述(无方法) | — |
| `DraftModeWarlordSelectionStateDemo._003CContinueButtonOnClick_003Eb__13_0()` | `含在 139` | 间接指认 | — |
| `DraftRunDisplay.Awake()` | `DraftModeWindow `BuildOngoingPage`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1631` |
| `DraftRunDisplay.Initialize(IDraftEvent draft)` | `DraftModeWindow（`_victoriesNum`/`_stageCounter`/格）` | 仅类/描述(无方法) | — |
| `DraftRunDisplay.OnDestroy()` | `DraftModeWindow（页销毁）` | 仅类/描述(无方法) | — |
| `DraftRunDisplay.SetupStageMarks(List<DraftModeOngoingStageVisualization> listToInit, i…` | `DraftModeWindow `RebuildStageMarks`` | 仅类.方法(可补行号) | `CardPresentation/Shell/DraftModeWindow.cs:1714` |

## 战役.tsv（A 68，问题 68）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `ArmySelector.ClearListeners()` | `Shell/CampaignTab.cs（直接方法调用，无事件表）` | 文件级 | — |
| `ArmySelector.Initialize(IEnumerable<CardArmy> armies, CardArmy selectedArmy, ArmySe…` | `CampaignTab `Build()` 的阵营条 + `SetAllowClickWhenAlreadySelected`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:183` |
| `ArmySelector.OnDestroy()` | `CampaignTab（条目随窗销毁；无事件订阅）` | 仅类/描述(无方法) | — |
| `ArmySelector.Refresh()` | `CampaignTab `Refresh()` / `RefreshNodes()`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:957` |
| `ArmySelector.SelectArmy(CardArmy army)` | `CampaignTab `SelectArmy(army)`` | 仅类/描述(无方法) | — |
| `ArmySelector.ToggleAllowClickWhenAlreadySelected(bool option)` | `CampaignTab `SetAllowClickWhenAlreadySelected(bool)`` | 仅类/描述(无方法) | — |
| `ArmySelector.ToggleArmyInteractable(CardArmy army, bool interactable, bool allArmie…` | `CampaignTab `LockArmySelector(army)`` | 仅类/描述(无方法) | — |
| `BattleArenaByArmySO.GetBattleArena(CardArmy army)` | `WarpforgeArena1/Runtime/ArenaByArmy.cs` | 文件级 | — |
| `BattleArenaByArmySO.TryGetBattleArenaName(CardArmy targetCardArmy, out string arenaName)` | `ArenaByArmy `OriginalNameFor(army)`` | 仅类/描述(无方法) | — |
| `Campaign.CollectRewards(int nodeIndex, CampaignNodeInfo node, RewardTier tier, …` | `CampaignData `Claim` + `CampaignRewardWindow`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:196` |
| `Campaign.GetCampaignNodes()` | `CampaignData `Nodes`（47 节点，脚本从 SO 抄出）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:68` |
| `Campaign._003CCollectRewards_003Eg__OnFail_007C11_1(string error)` | `CampaignRewardWindow/CampaignData（`why` 串 + 日志）` | 仅类/描述(无方法) | — |
| `Campaign._003Cget_AvailableNodes_003Eb__7_0(CampaignNodeInfo x)` | `CampaignData `StateOf/IsUnlocked`（现算）` | 仅类/描述(无方法) | — |
| `Campaign._003Cget_AvailableNodes_003Eb__7_1(CampaignNodeInfo x)` | `CampaignData `Claimable`（点固定 ⇒ 恒真）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:185` |
| `CampaignArmyUIDecorator.CheckFillColor()` | `CampaignTab `ArmyBarCompletedTint` + 进度条三件` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:99` |
| `CampaignArmyUIDecorator.Start()` | `CampaignTab `BuildArmyItems`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:790` |
| `CampaignHandler.GetCampaign(CardArmy army)` | `CampaignData（单套 UM + `HasContent`）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:271` |
| `CampaignHandler.GetCurrentCampaign()` | `CampaignData `Selected`（缺省 UM）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:303` |
| `CampaignHandler.SetSelectedArmy(CardArmy army)` | `CampaignData `Select`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:304` |
| `CampaignHeaderDisplay.OnDisable()` | `CampaignTab（按钮随窗销毁）` | 仅类/描述(无方法) | — |
| `CampaignHeaderDisplay.OnEnable()` | `CampaignTab `Build()`/`BuildPremiumPanel`（建时绑）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:1087` |
| `CampaignNode.Collect(RewardTier tier)` | `CampaignData `Claim`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:196` |
| `CampaignNode.Connect(CampaignNode[] nodes)` | `CampaignTab `BuildLine(i,j)`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:692` |
| `CampaignNode.OnClick()` | `CampaignTab `Build(node)` 的命中区` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:591` |
| `CampaignNode.SetLineColors(Color lineColor)` | `CampaignTab `BuildLine` 末句 `SetTint(StateColor(源节点状态))`` | 仅类/描述(无方法) | — |
| `CampaignNode.SetNodeStyle()` | `CampaignTab `StateColor/PremiumColor`` | 仅类/描述(无方法) | — |
| `CampaignNode.Setup(CampaignNodeInfo node, NodeState state, Campaign campaign)` | `CampaignTab `Build(node)` / CampaignData` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:591` |
| `CampaignNode.Unlock()` | `CampaignData `IsUnlocked`（现算）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:163` |
| `CampaignPointDrawer.Draw(CampaignPoints item, int quantity)` | `Shell/ItemDrawer.cs（`CampaignPoints` 类型已登记）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `CampaignPointDrawer.GetItemName(CampaignPoints item)` | `ItemDrawer 的 item 名` | 仅类.方法(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:450` |
| `CampaignRewardsWindow.CenterHolder(RectTransform holder)` | `Shell/CampaignRewardWindow.cs `CenterHolder`` | 文件级 | — |
| `CampaignRewardsWindow.ConfigureIsPreviewState(bool isPreview)` | `CampaignRewardWindow `ConfigureIsPreviewState`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignRewardWindow.cs:979` |
| `CampaignRewardsWindow.ESCPressed()` | `CampaignRewardWindow（ESC 关窗）` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.OnDisable()` | `CampaignRewardWindow（关时解绑）` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.OnEnable()` | `CampaignRewardWindow（开时绑）` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.Open()` | `CampaignRewardWindow `Build()`/`Open()`` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.SetBaseButton()` | `CampaignRewardWindow `SetBaseButton` 分支` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.SetPremiumButton()` | `CampaignRewardWindow `SetPremiumButton` 分支` | 仅类/描述(无方法) | — |
| `CampaignRewardsWindow.UnlockClicked(RewardTier tier)` | `CampaignRewardWindow `UnlockClicked` → `context.OnCollect(tier)`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignRewardWindow.cs:97` |
| `CampaignRewardsWindow._003CSetBaseButton_003Eb__21_0()` | `含在 63` | 间接指认 | — |
| `CampaignRewardsWindow._003CSetBaseButton_003Eb__21_1()` | `含在 63` | 间接指认 | — |
| `CampaignRewardsWindow._003CSetPremiumButton_003Eb__22_0()` | `含在 64` | 间接指认 | — |
| `CampaignTrack.ClearTrack()` | `CampaignTab `ClearTrackContent()`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:512` |
| `CampaignTrack.ConnectNodes()` | `CampaignTab `BuildTrack()` 的连线先建 + `BuildLine`` | 仅类/描述(无方法) | — |
| `CampaignTrack.GetNode(int index)` | `CampaignTab `_nodeTf[index]`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:579` |
| `CampaignTrack.GetNode(string nodeId)` | `CampaignTab（按 id 找 `CampaignData.IndexOf`）` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:122` |
| `CampaignTrack.GetNodePosition(CampaignNodeInfo data, Vector2 offset, float ratio)` | `CampaignTab `NodeRect(i)`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:567` |
| `CampaignTrack.GetNodeState(CampaignNodeInfo nodeData, List<CampaignNodeInfo> collect…` | `CampaignData `StateOf`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:175` |
| `CampaignTrack.Initialize(Campaign campaign)` | `CampaignTab `BuildTrack()`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:442` |
| `CampaignUnlockButton.OnClick()` | `CampaignRewardWindow 领取钮` | 仅类/描述(无方法) | — |
| `CampaignUnlockButton.OnDisable()` | `CampaignRewardWindow（关时解绑）` | 仅类/描述(无方法) | — |
| `CampaignUnlockButton.OnEnable()` | `CampaignRewardWindow（开时绑）` | 仅类/描述(无方法) | — |
| `CampaignUnlockButton.SetAsClaimed()` | `CampaignRewardWindow `SetAsClaimed` 支` | 仅类/描述(无方法) | — |
| `CampaignUnlockButton.SetAsFreeClaim(bool canUnlock)` | `CampaignRewardWindow `SetAsFreeClaim` 支` | 仅类/描述(无方法) | — |
| `CampaignUnlockButton.ToggleTexts(bool state)` | `CampaignRewardWindow `ToggleTexts`` | 仅类/描述(无方法) | — |
| `CampaignWindowTab.ChangeArmy(CardArmy army)` | `CampaignTab `SelectArmy/Refresh`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:899` |
| `CampaignWindowTab.ClickChangeArmy(CardArmy army)` | `CampaignTab `SelectArmy`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:899` |
| `CampaignWindowTab.GetTutorialTrackArmyNode(int index)` | `CampaignTab `TutorialTrackNode`` | 仅类/描述(无方法) | — |
| `CampaignWindowTab.LockArmySelectorByArmy(CardArmy selectedArmy)` | `CampaignTab `LockArmySelector`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:199` |
| `CampaignWindowTab.OnNodeClicked(CampaignNode node)` | `CampaignTab `BuildContext(i)` + 开 `CampaignRewardWindow`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:1016` |
| `CampaignWindowTab.OnOpen()` | `CampaignTab `OnOpen()`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:122` |
| `CampaignWindowTab.OnSetup()` | `CampaignTab `Setup()/Build()`` | 仅类/描述(无方法) | — |
| `CampaignWindowTab.ToFocus()` | `CampaignTab `FocusSelectedArmy`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignTab.cs:889` |
| `CampaignWindowTab.TryCollect(CampaignNode node, RewardTier tier)` | `CampaignTab `ClaimForTest` 路径 / `CampaignData.Claim`` | 仅类.方法(可补行号) | `CardPresentation/Shell/CampaignData.cs:196` |
| `CompleteCampaign.OnInitialize()` | `CampaignData `NotifyCampaignCompletionIfAllClaimed`（Claim 后直写成就）` | 仅类/描述(无方法) | — |
| `CompleteCampaign.OnReset()` | `同上（无注册即无需反注册）` | 间接指认 | — |
| `CompleteCampaign.OnTrigger(CompleteCampaignSignal obj)` | `CampaignData `AllNodesClaimed` + `ProfileData.SetAchievementValueAtLea…` | 仅类.方法(可补行号) | `CardPresentation/Shell/ProfileData.cs:563` |
| `CompleteCampaign._003COnInitialize_003Eb__3_0(Campaign x)` | `同上` | 间接指认 | — |

## 锻造.tsv（A 55，问题 28）

| 类.方法 | 落点 | 问题类型 | 可补的声明行（建议） |
|---|---|---|---|
| `BoosterInfoPopup.Close()` | `Shell/BoosterInfoPopup.cs Close（GameWindow 基类）` | 文件级 | — |
| `BoosterInfoPopup.ToFocus()` | `Shell/GameWindow 前台支 + BoosterInfoPopup` | 仅类/描述(无方法) | — |
| `BoosterPackOpenWindow.BoosterPackOpen()` | `Shell/BoosterPackOpenWindow.cs:397 Open / :413 Show / :787 Reveal` | 指向别的方法/字段 | `CardPresentation/Shell/BoosterPackOpenWindow.cs:413` |
| `Forge.GetArmy()` | `Shell/ForgeData.Selected（每阵营一份数据）` | 仅类.方法(可补行号) | `CardPresentation/Shell/ForgeData.cs:118` |
| `ForgeHandler.CheckNotification()` | `Shell/BadgeNotifications.cs ForgeNotificationProvider（本轮补）` | 文件级(可补行号) | `CardPresentation/Shell/BadgeNotifications.cs:103` |
| `ForgeHandler.HasAnyArmyAnyRewardCollected()` | `Shell/ForgeData.StateAt(army,lv)==Collected 任一条` | 仅类.方法(可补行号) | `CardPresentation/Shell/ForgeData.cs:148` |
| `ForgePointDrawer.Draw(ForgePoints item, int quantity)` | `Shell/ItemDrawer.cs（点数档）` | 文件级(可补行号) | `CardPresentation/Shell/ItemDrawer.cs:840` |
| `ForgePointDrawer.GetItemName(ForgePoints item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `ForgePointIconDrawer.Draw(ForgePoints item, int quantity)` | `Shell/ForgeData.ForgePointIcon + ItemDrawer 图标档` | 仅类.方法(可补行号) | `CardPresentation/Shell/ForgeData.cs:190` |
| `ForgePointIconDrawer.DrawForArmy(CardArmy army)` | `Shell/ForgeData.cs:190` | 指向别的方法/字段 | — |
| `ForgePointIconDrawer.GetItemName(ForgePoints item)` | `Shell/ItemDrawer.cs 名字层` | 文件级 | — |
| `ForgeRewardCurrentLevelPoints.Awake()` | `Shell/ForgeTab.cs:918 PointsLabel（每格那行 `cur/next`）` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:917(所在方法)` |
| `ForgeRewardCurrentLevelPoints.Initialize(string points, CardArmy cardArmy)` | `Shell/ForgeTab.cs:918-926（PointsText + ForgePointIcon）` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:917(所在方法)` |
| `ForgeRewardItemContainer.ActiveButton()` | `Shell/ForgeTab.cs:922 只对 ToCollect 格建 Claim 钮` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:917(所在方法)` |
| `ForgeRewardItemContainer.ClickClaim()` | `Shell/ForgeTab.cs BuildCell 的 Claim 钮 → ForgeData.Claim` | 文件级(可补行号) | `CardPresentation/Shell/ForgeTab.cs:858` |
| `ForgeRewardItemContainer.ConfigurePoints(int currentPoints, int previousNeededPoints, int point…` | `Shell/ForgeTab.cs:918 PointsText + ForgeData.Bar01` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:917(所在方法)` |
| `ForgeRewardItemContainer.SetBarFill(float percentage)` | `Shell/ForgeTab.cs:895-905（ProgressionBar 宽）` | 指向别的方法/字段 | — |
| `ForgeRewardSelector.ClearData()` | `Shell/ForgeTab.cs:782-800（清 _trackContent 重建）` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:779(所在方法)` |
| `ForgeRewardSelector.CollectRewardClicked(ForgeRewardItemContainer rewardItem)` | `Shell/ForgeTab.cs BuildCell Claim 钮` | 文件级(可补行号) | `CardPresentation/Shell/ForgeTab.cs:858` |
| `ForgeRewardSelector.GetForgeItemContainer(int index)` | `Shell/ForgeTab BuildCell 的格表` | 仅类.方法(可补行号) | `CardPresentation/Shell/ForgeTab.cs:858` |
| `ForgeRewardSelector.Initialize(Forge forgeEvent)` | `Shell/ForgeTab.cs:753 Refresh / :741 SelectArmy` | 指向别的方法/字段 | `CardPresentation/Shell/ForgeTab.cs:741` |
| `ForgeRewardSelector.RewardCollected(RewardInfo[] rewards)` | `Shell/ForgeTab.cs Refresh 尾部（收特效/重建）` | 文件级 | — |
| `ForgeSelectedArmyInfo.RefreshLevel(int currentLevel, int maxLevel)` | `Shell/ForgeTab.cs:763 LevelText` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:753(所在方法)` |
| `ForgeWindowTab.Awake()` | `Shell/ForgeTab.cs:273 Setup→Build/:353 Build` | 指向别的方法/字段 | — |
| `ForgeWindowTab.GetAllForgeEvents()` | `Shell/ForgeData.Armies` | 仅类.方法(可补行号) | `CardPresentation/Shell/ForgeData.cs:38` |
| `ForgeWindowTab.GetForgeEventByArmy(CardArmy typeFilter)` | `Shell/ForgeData（每阵营字典）` | 仅类/描述(无方法) | — |
| `ForgeWindowTab.GetForgeItem(int index)` | `Shell/ForgeTab.cs:812 TutorialClaimButton / BuildCell 格表` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:858` |
| `ForgeWindowTab.RefreshLevel()` | `Shell/ForgeTab.cs:763 LevelText + 771 _readyRoot` | 函数中间 | `CardPresentation/Shell/ForgeTab.cs:753(所在方法)` |

## 末 给各 owner 的处置建议

1. **`文件级(可补行号)`** —— 最省事：按「可补的声明行」直接补 `:行`，落点就成立（行为对不对另说）。
2. **`注释` / `函数中间`** —— 必须改真落点：`注释` 多半是「照着原版写了注释、没改代码」；`函数中间` 多半是「判据写在这个函数的某一行」。
2b. **`指向别的方法/字段`** —— 落点那一行**确实是一条声明，但不是这个方法的**（常常是同一个文件里相邻/同族的方法）。要么改指本方法的声明行，要么退 B。
3. **`数据表项(需确认是否纯查表)`** —— 纯查表保留 A；有分支/兜底 ⇒ 补分支落点或退 B。
4. **`被多方法共用`** —— 一行最多只对应一个方法，其余补各自行号或退 B。
5. **`整类行`（`*`）** —— 按新口径不能是 A。
6. **`间接指认`**（落点写「— / 无 / 见某某账」）—— 等于没落点 ⇒ 补真实落点或退 B。
7. **`文件不存在` / `行号越界`** —— 先核是不是路径写法问题（我只按 `Assets/` 与 `Assets/CardPresentation/` 两个根解析）；确实不存在就是落点失效。

