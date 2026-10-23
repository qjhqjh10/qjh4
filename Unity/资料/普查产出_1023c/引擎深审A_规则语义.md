# 引擎深审 A · 规则语义（我们的规则引擎 vs 原版）

- 审查方式：**只读**静态比对。第一判据 = `d:/2/tools/decomp_full/`（方法体）＋ `d:/2/tools/il2cpp_out/dump.cs`（字段/枚举）；第二判据 = 解包/原版 SO 真值 `Unity/资料/普查产出_1018/allcards_1350/cards_raw.json`。⛔ 未跑 Unity / player / ruleprobe。
- 我方实现 = `Unity/MyGame/Assets/RuleEngine/{Core,Data}/`；卡池现读 = `RuleEngine/Resources/cards_engine.json`（`count = 1127`）。
- 🔴 判据纪律：本项目大量注释把「我们自己上一版 Godot 复刻 `rule_core.gd`」当旁证 —— 本报告一律**不采信为原版判据**，只按反编译/原始 SO 判。

---

## 一、总表

| 区块 | 审到的机制 | 一致 | 不一致 | 判据不足 |
|---|---:|---:|---:|---:|
| ① 伤害与死亡链 | 15 | 13 | 2 | 0 |
| ② 触发与队列 | 11 | 9 | 1 | 1 |
| ③ 条件与目标筛选 | 11 | 6 | 4 | 1 |
| ④ 回合流程与资源 | 11 | 11 | 0 | 0 |
| **合计** | **48** | **39** | **7** | **2** |

「不一致」按严重度：**P0 = 0 · P1 = 1 · P2 = 6**（明细见 §三）。

---

## 二、逐条

### ① 伤害与死亡链

| # | 机制 | 原版行为（判据） | 我们的实现（文件:行号） | 判定 | 影响面 | 建议 |
|---|---|---|---|---|---|---|
| 1·1 | 伤害管线次序 | 盾/闪避/无敌并列挡下 → 空投舱 → 堡垒 → 护甲 → 扣血 → 易伤第二发（`CardScript._ReceiveDamage_d__381__MoveNext.c:56-183,188-257`） | `RuleCore.ApplyDamageCore`（`RuleCore.cs:7448` 起；分档注释 `:7513-7589`） | 一致 | 全体 | — |
| 1·2 | 护甲公式与守卫 | `damageValue = max(1, dmg − armour)`，且 `armour<1` / `dmg<1` 整段跳过；**残骸不减甲**（`…MoveNext.c:64-98`） | `DamageAfterReduction`（`RuleCore.cs:6485-6525`） | 一致 | 全体 | — |
| 1·3 | 堡垒 / 空投舱 | 堡垒整份吃掉、打穿只走溢出、**两路都不减甲**（`…MoveNext.c:126-183`） | `ApplyDamageCore` 空投舱/堡垒两段（`RuleCore.cs:7591-7760`） | 一致 | 卡池 0 张带 bastion/droppod（不可达） | — |
| 1·4 | 易伤是「另一发」 | `CurrentVulnerable>0` 时造**独立** `DamageInfo(量=V, 类型=7/8)` 再走一遍管线（`…MoveNext.c:188-257`） | `VulnerableRaw` / `VulnerableDamagePart` / `DamageAfterReduction`（`RuleCore.cs:6429-6600`） | 一致（近似：我方折进同一次调用、只发一条事件，原版两条） | 29 张带 vulnerable | 事件条数近似如实记着即可 |
| 1·5 | 判死口径 | `EnoughPendingDamageToDie`：`state==5` ⇒ 真；否则 `health>0 \|\| survivor!=0` 时看 pending 列表，**否则为真**（`CardScript__EnoughPendingDamageToDie.c:31-80`） | `RuleCore.EnoughPendingDamageToDie`（`RuleCore.cs:6742`）＝ `Health<=0 && Survivor<=0`；预览侧另有 `WouldKillByEntries`（`:6801`） | 一致（我方伤害现结 ⇒ 无 pending 队列，等价） | 全体 | — |
| 1·6 | 反击伤害取值 | `GetCombatDamage(守方, 本次打法)`；`>0` 才 ReceiveDamage（`BattleManager._ResolveAttack_d__438__MoveNext.c:1112-1147`） | `counterAtk = FieldAttack(..., ranged)`（`RuleCore.cs:6189`）；`Hurt(... ignoreBastion:true)`（`:6227`） | 一致 | 全体 | — |
| 1·7 | **反击豁免四道** | ①攻方 `sneakAttack(20)` **无条件**；②`ActivatesTerror`（`terror(520)` ∧ 目标是单位 ∧ 目标 `currentCombatAttack` **<** 攻方）；③Sniper（远程 ∧ 会击杀 ∧ 目标无保护/幸存者）；④LongRange（远程 ∧ 目标是单位）（`BattleManager._ResolveAttack…:871-895`；`EntityScript__ActivatesTerror.c` / `ActivatesNoReturnSniperAttack.c` / `ActivatesNoReturnLongRangeAttack.c`） | **只做了 ③④**：`ranged && Has(LongRange)` / `ranged && Has(sniper) && targetDied`（`RuleCore.cs:6199-6201`） | **不一致** | 现池 `sneakAttack`/`terror` **0 张**（`cards_engine.json` 现扫）⇒ 今天不可观测 | 按铁律 11 补齐两道（`sneakAttack` 无条件免反；`terror` 比 `currentCombatAttack`） |
| 1·8 | Blast | `CurrentBlast>0`，对目标相邻逐个独立 `ReceiveDamage`（deathType 15）（`BattleManager__ResolveTraitAdjacentDamage.c`；`ShouldTriggerAdjacentDamage.c`） | `RuleCore.cs:6122-6148`（相邻表算一次、同侧、含督军） | 一致 | 41 张带 blast | — |
| 1·9 | Stomp | 攻方 `stomp(0x4d8)` ∧ **目标裸血 `+0x68 < 0`** ∧ 相邻表非空；溢出 = 超出的血（`ShouldTriggerStompDamage.c:20-36`；`ResolveTraitStompDamage.c`） | `targetDied && dealt > hpBefore && adjUnits.Count>0`（`RuleCore.cs:6150-6183`） | 一致（`dealt>hpBefore` ⇔ `血<0`） | 11 张带 stomp | — |
| 1·10 | 多目标伤害结算 | `_ResolveMultiDamage` 逐目标 `ReceiveDamage`，每个目标自己 CheckIfDead（`…d__456__MoveNext.c:44-170`） | `SimultaneousDamage` 批内延后死亡、批末 `FlushDeaths` | 一致（我方死亡延后但「待死仍占格」等价） | 全体 | — |
| 1·11 | 死亡判定/翻面 | 血≤0 ⇒ 先问 Survivor；否则 `state=5` → `TriggerUnitBacklashActions`（**入队**，优先级取 ability）／remnant 走 `AddTransformIntoRemnant`（`CardScript__CheckIfDead.c:108-165`） | `CleanupDeaths`（`RuleCore.cs:8300-8440`） | 一致 | 全体 | — |
| 1·12 | 死亡结算次序 | 反噬/Unstable → 死亡广播 → 进墓地；`turnDied` 在**入队时**写（`CardScript__TriggerUnitBacklashActions.c:82`） | `DiedThisTurn++` 在反噬前、弃牌/`DeadUnits` 在反噬后（`RuleCore.cs:8583-8660,8840-8935`） | 一致 | 全体 | — |
| 1·13 | 猎杀标记致死结算 | 唯一闸 = 观察者==死者 ∧ `CurrentHuntMark>0`（`CardScript__TriggerOnMinionDeath.c:33-95`） | `CleanupDeaths` 猎杀段（`RuleCore.cs:8493-8521`） | 一致 | 带 huntmark 的卡 | — |
| 1·14 | 击杀记账 | `DamageInfo.actingCard` + deathType 参与 `Slay` 记账 | `killer`/`actingCard` 随 `PendingDeath` 到 `FlushDeaths`（`RuleCore.cs:8288-8300`） | 一致 | 全体 | — |
| 1·15 | 死亡类型（170/175/180 一族） | `UnitDeathByAbility` 等是**独立 ability**（SO 里 `trigger=180`，如 `AM9/AM34/SOR29/UM99`） | 我方按正文「If it dies …」当**同一能力的 post-check**（`EffectResolver.cs:10221-10380`） | 一致（语义等价；原版那条的 ctx 可到 50） | 4 张 | 见 §四 判据不足① |

### ② 触发与队列

| # | 机制 | 原版行为（判据） | 我们的实现 | 判定 | 影响面 | 建议 |
|---|---|---|---|---|---|---|
| 2·1 | 四条道 + 分道 | 0→Player · 1→Instant · 2→Interrupt · 3→DelayedInstant；非法值 LogError 丢弃（`BattleManager__AddAutoActionToQueue.c:47-233`） | `ActionQueue`（`ActionQueue.cs:157-280`） | 一致 | 全体 | — |
| 2·2 | 消费序 | Interrupt → Instant → DelayedInstant → Player（`BattleManager__NextActionInQueue.c:11-28`；`ResolveActionFromQueue.c:32-44`） | `DequeueOrder = {2,1,3,0}`（`ActionQueue.cs:167`） | 一致 | 全体 | — |
| 2·3 | 不抢占 / 每拍一条 | `resolvingActionFlag(+0x280)` 为真不泵（`Update.c:1271-1299`） | `Resolving` 闸 + `ActionScope`（`ActionPump.cs:75-131,654-760`） | 一致 | 全体 | — |
| 2·4 | 入队前 abilityContext | `CanTriggerAbility` → `IsRightContext` 六档；**0/80/145/220/500 五格跳过**（`CardAbility__CanTriggerAbility.c:88-143`） | `EnqueueAbility` 入队时判（`ActionPump.cs:175-217`）；跳过名单 `TriggerSkipsAbilityContext`（`:403-417`） | 一致（五格逐字对上） | 全体 | — |
| 2·5 | 出队时主动取消 | 只取消 `actionType==1` 且载体==离场者；**保留 destroyUnit(45)/recallToHand(26)**（`BattleManager__CancelEffectsFromDisappearingUnitFromQueue.c`） | 只取消「载体被换成残骸/transform」（`ActionPump.cs:490-506,704-717`） | 一致（jam/removeEffect 两路未建模、现池 0 可达） | 全体 | — |
| 2·6 | **触发优先级数据** | `CardAbility.priority` 1484×Instant / 6×Interrupt / 4×Delayed / 2×Player（`cards_raw.json` 现读） | `cards_engine.json` 的 `triggerPriority` **只有 4 条（全 Delayed）**；生成器只映射 21 个关键词，`WhenEvent`/回合/打出族**没有键**（`工具/gen_cards_engine.py:1189-1270`） | **不一致** | **池内 3 条**：`DA12 Company Veteran`(695, prio2) · `BL72 Accursed Helbrute`(510, prio2) · `DA14 Ravenwing Speeder`(410, prio0) ⇒ 全部落 Instant。⚠️ `DA14` 特殊：我方 `FollowUpSchedulerOf` 把它列为「无 320/325 调度者 ⇒ 原版也不会被这一拍触发」（`EffectResolver.cs:6062-6065`），**能力本体是否该响仍未闭合**（其 SO 里这条 410 的 logic 是 `buffMana(60)`，与卡面 Agenda 正文不符）⇒ 这条先按「待核」 | 把 `WhenEvent`/`ResolveAtTurn`/打出族也接进 priority（见 §三 P1） |
| 2·7 | 额外触发（"additional time"） | Mob/Regiment 等「再来一次」 | `QueuedAction.ExtraTrigger` + `ctx.ExtraTriggerDepth`（`ActionPump.cs:729-751`） | 一致 | 相关卡 | — |
| 2·8 | 广播受众 | 棋盘双方 + 双方手牌（`Broadcast*` 一族） | `BroadcastWhen` 快照 + 两跳手牌（`EffectResolver.cs:6120-6260`） | 一致 | 全体 | — |
| 2·9 | 关键词触发自排除 | `Sacrifice/Mob/Swarm` 自排除；`Ferocity/Duty/Synapse` 不自排除（机器码 `op_Inequality` 条数） | `WhenEvents.SelfExcludesOnKeywordTrigger`（`WhenEvent.cs:1663-1680`） | 一致（按项目已核表；本会话未重数机器码） | 12 处 | — |
| 2·10 | 600/605 资源闸 | `CanTriggerAbility` 里 600/605 查 `SpiritStone/Faith >= 卡面阈值`，且仅当「本体 == 触发者」时查资源 | `CanUseSpiritStone` / `CanUseFaithAbility`（`RuleCore.cs:10037-10075`） | 一致 | 29+26 张 | — |
| 2·11 | 泵点时机 | `Update` 在 `duringBattle \|\| duringScriptedActions` 且非 resolving 时泵 | `PumpActionQueues` 在 `ActionScope` 退出时泵（`ActionPump.cs:654-760`） | 一致（脚本动作那一档未建模、教程链另有） | 全体 | — |

### ③ 条件与目标筛选

| # | 机制 | 原版行为（判据） | 我们的实现 | 判定 | 影响面 | 建议 |
|---|---|---|---|---|---|---|
| 3·1 | `conditionsFilter` 35 值 | `UnitConditions` 35 个成员；switch 在 `FilterMethods__MeetsConditionCriteria.c:150-361` | `ConditionEval.KindOf`（`ConditionEval.cs:52-89`）：Ok 27 / ModelGap 5 / OriginalUnhandled 3 | **部分不一致**（5 个判不了） | 240/250/280/320/330 **池内 0 行**（现扫 218 行 unitConditions）⇒ 今天不可观测；遇到时**目标池不筛**（`Meets` 返回 null ⇒ 出声但放行） | 补齐 5 档（需先建附魔 spellType / `_quest` / 装填层） |
| 3·2 | `targetdies` / `targetsurvives` | 220 ± `excludeCondition`（`MeetsConditionCriteria.c:252-259`） | `ConditionKindToUnitCondition`（`EffectResolver.cs:10073-10074`）＋ `IsPostCheckConditionKind`（`:10095`）；本会话 R1 刚修 | 一致 | 124 行 cond=220 | 已修，保持 |
| 3·3 | **`countCriteria` 消费** | `BoardCriteria{countCriteria,countOptions,triggerIf,referenceValue}`，`CountOptions{AllTargets=0, EnemiesMinusAllies=10}`，`StatFilterOptions` 12 档（`dump.cs:20128-20136,20837-20862`） | 数据落全了（`CardDef.ConditionSpec.CountOption/StatOption/ReferenceValue/CountSet`），但**没有任何求值消费者**（`grep` 全仓只赋值/取值器）；`count` 族仍走正文（`RuleEngineTest.cs:34792-34819` 自认） | **不一致** | **池内 4 张**：`AM43 Recon Operation` / `EC58 Vengeful Surge` / `SOR42 Rite of Restoration` / `SOR70 Sororitas Rhino`（正文兜底，其中 EC58 已声明「表达不出」） | 补 `count` 求值（遍历 CountSet + 比较三参数）；先对 4 张逐张核正文兜底是否同值 |
| 3·4 | `IsLegalPick` ↔ `ResolveTargets` | 同一 `TargetCriteria` | 两边共用 `ApplyTargetFilters`（`EffectResolver.cs:1614-1631` vs `:1645`） | 一致 | 全体 | — |
| 3·5 | `actingCardCriteria` 维度 | 20+ 维（兵种/牌型/阵营/攻血费稀有度/条件/自我排除/enchantment…）（`FilterMethods__CheckIfMeetsCriteria.c`） | `WhenEvent` 主语只支持 side/kind/keyword/name（`WhenEvent.cs:1500-1612`） | **结构性不一致** | 现池**只有 2 条**用了未建模维度：`DA66`(spellType=150，正文按 subtype 兜住) · `DA22`(cond=220，正文按「survives」兜住) | 逐张核这 2 张；其余暂等价 |
| 3·6 | `TargetCriteria`（部署/改费/手牌层） | 同上 20+ 维 | `CardCriteria` 只有 Kind/Keyword/Name/Warlord 4 维（`CardCriteria.cs:38-104`） | **不一致（结构性）** | 部署触发 / 持续改费 / 手牌效果三条路；本会话未逐卡量化 | 先出「哪些卡用了未建模维度」的普查，再补维度 |
| 3·7 | 「本局/本回合阵亡」 | 每实例 `turnDied(+0x250)`，`HasDiedThisTurn` **等于**全局回合号；`SinceLastTurn` 允许 `turnDied+1`（`CardScript__HasDiedThisTurn.c` / `HasDiedSinceLastTurn.c`） | `DiedInTurn`（`ConditionEval.cs:503-514`）按 **`CardDef` 引用**匹配 `DeadUnits`，不按 `CardInstance` | **不一致** | `EC39 Eternal Servitude` / `SOR42 Rite of Restoration` 在「同名两份、一份本回合死、一份更早死」时可捞错那一份；`UM77` 只计数（不受影响） | 把 `DeadUnits` 的匹配键从 CardDef 换成实例（`CardInstance` 或 `InstanceId`） |
| 3·8 | 弃牌堆部署「died this game」 | 原版按阵亡流水/墓地准入 | `DoDeployOnce` 仍用 `ps.Discard` + `type==unit` 充数（`EffectResolver.cs:2784-2790`），而注释说「我们没有阵亡账」——**已过期**（`DeadUnits` 已存在，`:5172` 已在用） | **不一致（注释与实现都过期）** | `GSC55 Eve of the Uprising`（died this game）· `EC39`/`SOR42`（since last turn） | 改用 `DeadUnits` + `DeathTurn` 过滤；同步订正过期注释（铁律 5） |
| 3·9 | 目标挑选 | `lowestHealth=240` 走 `GetLowestHealthUnit`（**严格最小、取第一个、排除 pending 致死**）（`BattleManager__GetLowestHealthUnit.c:120-190`） | `PickMost` 分支（`EffectResolver.cs:1465-1486`）按 `ctx.Rng`? 否 —— 严格 `<` 取首个 | 一致 | `SAU70`/`SAU73` 等 | — |
| 3·10 | 兵种/关键词/卡名筛 | `CheckIfMeetsCriteria` 的 `include*/exclude*` + `keywordFilter/traitsFilter/filterSpecificCards` | `ApplyTargetFilters`（`EffectResolver.cs:1645-1730`） | 一致（subset，见 3·5/3·6） | 全体 | — |
| 3·11 | `TargetsAffected` 覆盖 | 30 档（`dump.cs:20777-20818`），池内出现 23 档 | 正文解析出 `EffectTargetSpec`（不是按 `abilityTargets.ta` 驱动）；`ta` 只作为结构化补充 | 判据不足（本会话未逐档核 23 档的解析覆盖） | 见 §四 判据不足② | 建议下一批做「23 档 × 解析产物」对照普查 |

### ④ 回合流程与资源

| # | 机制 | 原版行为（判据） | 我们的实现 | 判定 | 影响面 | 建议 |
|---|---|---|---|---|---|---|
| 4·1 | 回合开始次序 | 维护费/能量 → 逐卡（解疲劳→开舱→stealth 到期→晕/瞎闸→`state==5` 整卡跳过）→ TurnStart 触发 → 抽牌 → 天赋（`PlayerManager__TurnStart.c:12-124`；`CardScript__OnTurnStart.c:85-171`；`_NextTurn_d__395:338-371`） | `BeginTurnCore`（`RuleCore.cs:1485-2030`） | 一致（逐卡 vs 整段两处已如实标为近似） | 全体 | — |
| 4·2 | 加时 | `IsOvertime` 由 `overtimeTurn <= turnCounter`（全局回合号）；加时回合起手多抽 1（`_NextTurn:191-207` + `TurnStartCardDraw` 的 `+0x258` 读点） | `ctx.Turn >= Vars.overtimeTurn` → `IsOvertime`；`OvertimeExtraDraw=1`（`RuleCore.cs:1625-1660`；`DeckRules.cs:83-95`） | 一致 | 经典/冲突两模式 | — |
| 4·3 | 维护费 | `CurrentMaintenance` 求和 → `UseNormalEnergy`（不足则清零 + 触发 SpentAllMana）（`PlayerManager__TurnStart.c:68-124`） | `upkeep` 求和 → `SpendNormalMana`（`RuleCore.cs:1579-1597`） | 一致 | 池内 0 张带 maintenance | — |
| 4·4 | 后手起始法力补贴 | `addedManaSecond` 写后手、只一次、`unusedMana += saved`（不抬上限）（`_NextTurn:296-352`） | 同形（`RuleCore.cs:1599-1623`） | 一致 | 值为 0（不可达） | — |
| 4·5 | 精神石 600 / 信仰 605 | `PlayerManager.GetCurrentSpiritStone/Faith` + 阈值；不扣费 | `PlayerState.SpiritStones/Faith`；`CanUse*`（`RuleCore.cs:10037`） | 一致 | 29/26 张 | — |
| 4·6 | 手牌上限 | 进手牌**之前**判：满则**这一张**进墓地、手牌不动（`PlayerHand._AddDrawnCardToHand_d__37:113-140`） | `AddCardToHandWithinLimit`（`RuleCore.cs:2812-2830`） | 一致 | 全体 | — |
| 4·7 | 疲劳 | 牌库空 ⇒ 疲劳 +1、督军挨等量伤（type 2） | `MoveDeckTopToHand`（`RuleCore.cs:2731-2760`） | 一致 | 全体 | — |
| 4·8 | 哨戒（sentinel） | 该方场上任一单位带 `0x29e(670)` ⇒ **回合开始抽牌整段跳过**（`PlayerHand__IsThereSentinel.c`；`BattleManager__TurnStartCardDraw.c:29-46`） | `HasSentinelInPlay`（`RuleCore.cs:1197-1203`）+ 抽牌闸（`:1752`） | 一致 | 池内 0 张 | — |
| 4·9 | 狂暴（berzerk） | 有可攻击目标时必须攻击才允许结束回合（`GetPendingBerzerkUnit` / `ResolveEndTurn:33-137`） | `PendingBerzerkUnits` / `HasPendingBerzerkAttack` / `ResolveBerzerkAttacks`（`RuleCore.cs:1288-1460`） | 一致 | 池内 0 张 | — |
| 4·10 | 牌库顶 / 洗回 | 抽牌取 `deck[0]`（顶）；新牌 `Insert(0)`；洗回后同样（`BattleManager__DrawCardFromDeck.c` / `AddInitialCardToDeck.c`） | `Deck` 末尾当顶（镜像约定）、`Shuffle` 在 `FinishMulligan` 后（`RuleCore.cs:2763-2791,425-430`） | 一致（本会话刚修的「洗回在触发结算之前」已对齐） | 全体 | — |
| 4·11 | 花光能量触发 | `UseNormalEnergy` 扣到 0 → `TriggerSpentAllMana`（641） | `SpendNormalMana` 统一口（`RuleCore.cs:1573-1597`） | 一致 | 出牌/技能/维护费 | — |

---

## 三、修复清单（按严重度）

### P1（顺序/语义真的会变，判据已齐）
1. **【2·6】触发优先级在 `WhenEvent` 一族上缺失** —— 判据已齐（`cards_raw.json` 的 `priority` + `ActionPriority` 分道）。
   - 缺口：`triggerPriority` 只按 21 个「有正文关键词」的 trigger 落值；`0/10/30/40/80/90/190/410/510/695` 等走 `WhenEvent`/`ResolveAtTurn` 的族**无键 ⇒ 一律 Instant**。
   - 池内实证 3 条：`DA12`(695→应 Interrupt) · `BL72`(510→应 Interrupt) · `DA14`(410→应 Player)。
   - 做法：给 `WhenEventKind`/触发名补一张 `trigger 名 → priority` 数据列（生成器从 `cards_raw.json` 按 `(卡, trigger, 有 logic)` 落值），`ActionPump.ListenerLane` 读它，⛔ 不要按名字猜。`GOF97` 的「空 logic 的 priority=0」必须继续排除。
   - 验收：同一批里「quest point 触发 + 其他 Instant 触发」的顺序与「后手 Player 道最后结算」两条判别式。

### P2（今天不可观测 / 冷门可达；判据已齐，按铁律 11 要做）
2. **【1·7】反击豁免补 `sneakAttack(20)` 与 `terror(520)`** —— 判据已齐；池内 0 张，补完加一条夹具。
3. **【3·7】`DiedInTurn` 改按实例匹配** —— 判据已齐；影响 `EC39`/`SOR42`。
4. **【3·8】弃牌堆部署改用 `DeadUnits` + `DeathTurn`**，并订正 `EffectResolver.DoDeploy` 的过期注释（「没有阵亡账」已不成立）。
5. **【3·3】补 `count` 族求值** —— 判据已齐（`BoardCriteria` 五个字段 + 两个枚举）；4 张卡，先核正文兜底是否同值再改。
6. **【3·1】补 5 个 ModelGap 条件** —— 判据方向已齐（`HasSecretOrders`/`HasQuests`/`DoesAbilityRequireLoading`/`_quest`/`ward·tainted`），但都依赖**未建子系统**（附魔 spellType / 装填位 / trait 140·160）。池内 0 行 ⇒ 先立账不排期。
7. **【3·6】`CardCriteria` 维度补齐** —— 判据已齐但需先普查「哪些卡用到未建模维度」；本会话未量化 ⇒ 先做普查再改。

### P0
无。（本轮没有发现「影响每一局、且判据已齐」的差异。）

---

## 四、没查清（还差什么）

1. **`UnitDeathByAbility(180)` 的发射点**：我在 `decomp_full` 里把 `OnTrigger` / `TriggerAbilities` / `GetAbilitiesForTrigger` 的调用点都数过（分别 18 / 20 / 4 处），**没有一处传 180**；`BattleManager__ResolveDestroyUnit.c` 也没有。⇒ 我们按「正文 post-check」实现是**行为等价推断**，不是逐行判据。要查清：`BattleManager__GetDeathType` → 死亡广播链里 180 的发射（怀疑在未反编译出的 `BattleManagerSupport` 某个 `Broadcast*`，或 `CemeteryManager`）。影响 `AM9/AM34/SOR29/UM99`。
2. **`TargetsAffected` 23 档 × 正文解析覆盖**：池内出现 23 个取值（0/10/20/30/50/51/52/60/65/80/90/100/106/110/113/120/130/140/150/180/200/201/240）。本会话只抽核了 `51/52/106/113/200/201` 的卡面短语能被解析器认（`in play and in hand` / `enemy hand` 等），**没有逐档做「ta → 解析产物」对照**。要查清：跑一次全池「每张卡的 abilityTargets.ta 是否能由该卡正文解析复现」的普查。
3. **`terror` 的 `currentCombatAttack` 口径**：`EntityScript__ActivatesTerror.c` 读 `get_currentCombatAttack`，该属性的方法体未读（当前 build 里是 `currentCombatAttack` 还是「按 attackType 取近战/远程」没核）⇒ 补 1·7 时要先读它。
4. **`CardScript__GetDeathType` 全表**：只读了调用点（`ResolveMultiDamage` / `_ResolveAttack`），没有逐死亡类型对照我方 `killer/actingCard` 的口径；目前靠 `TryCreditKill` 的「放行两档」近似。
5. **`Triggered*` 自排除**：`WhenEvent.cs` 那张表是项目早先按机器码 `op_Inequality` 条数核的，本会话**没有重数**（只读了它引用的 RVA 表），按「项目已核」采信。

---

## 五、附：本会话可直接复核的锚点

- 触发道缺口：`cards_engine.json` 内 `triggerPriority` 只有 4 条（`Reclaim the Stars`/`Canoptek Spyder`/`Death from Above`/`Damaged Hexmark`）；`cards_raw.json` 中池内还有 `DA12(695,2)` · `BL72(510,2)` · `DA14(410,0)`。
- 反击豁免：`RuleCore.cs:6199-6201` 只有 `LongRange` / `sniper` 两项。
- 阵亡条件：`ConditionEval.cs:503-514` 用 `ReferenceEquals(d.Card, card)`。
- 弃牌堆部署：`EffectResolver.cs:2784-2790` 注释与 `DeadUnits` 已存在（`:5172`）矛盾。
