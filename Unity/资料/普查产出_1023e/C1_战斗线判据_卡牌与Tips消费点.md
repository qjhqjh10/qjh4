# C1 · 战斗线判据 —— 卡牌与 Tips 消费点（只读普查，2026-10-23/24）

> 本文件只回答「**判据是什么、具体值多少、出处在哪、怎么落**」。⛔ 本轮没有改任何代码/数据/正本。
> 格式：`编号 | 查的是什么 | 判据结论（具体值） `|` 出处 `|` 状态 `|` 照着做时怎么落 `。`

---

## 一、`T21` 逐卡专属动画触发面（四条全部现核）

### C1-C01 ~ C1-C04

| 编号 | 查的是什么 | 判据结论（具体值） | 出处（文件:行号 / 资产路径+字段） | 状态 | 照着做时怎么落 |
|---|---|---|---|---|---|
| **C1-C01** | `T21①` 手牌 / 弃牌堆**锚落点**：`EvtKind.Trigger` 缺「非棋盘落点」通道 —— 到底缺哪种落点 | **原行的前提要改**：`T21` 那批「不可达」行的 **410 动画**，落点全是**棋盘可解**的：`sp ∈ {2 abilityTarget, 3 friendlyHero, 4 enemyHero, 14 nextFriendlyMinionSpot}` —— **没有一条**要手牌/弃牌堆锚。真正的缺口是 **`sp=2` 缺「目标侧+格号」**（我们的 `Trigger` 只有 `Player+Slot`，语义还是「己方第几格」）；`sp=3/4/14` 只靠「这张卡属于谁」（事件里已有 `Player`）就能解。<br>**原版落点表**（`PositionOption` → 解析到哪个 Transform，`AbilityLogic.GetPos`）：`0 self`=施放卡自己的 `GetEffectAnchor`；`1 cardTarget`=`AbilityData+0x20` 卡的锚；`2 abilityTarget`=目标卡锚（无则退 `AbilityData+0x20`）；`3 friendlyHero`/`9`/default=本方英雄 `GetEffectAnchor`；`4 enemyHero`=对方英雄；`5 boardCenter`=`manager+0x1d0`；`6/7/8/10/11`=`BattleHud.GetUIAnchor(option, isPlayer=false)`；`12/13`=法力 UI 锚；`14–17`=`CreateTempAbilityTransform(side, k)`（下一个空位）；`18/19 friendly/enemyHand`=`BattleManager.GetPlayerHand(side).transform`；`50/55 friendly/enemyDeck`=`GetDeckManager(side).transform`；`61/71…`=灵能/灵魂石/信仰法力锚；**`150/155 friendly/enemyHandWorld`=`BattleManager.FriendlyHandAnchor` / `EnemyHandAnchor`**（按 ability 拥有方取「自己那侧 / 对面那侧」） | `dump.cs:25161-25208`（`PositionOption` 全枚举，`friendlyHandWorld=150` / `enemyHandWorld=155`）；`AbilityLogic__GetPos.c:75-100`（150/155 → `get_FriendlyHandAnchor`/`get_EnemyHandAnchor`）`:144-170`（50/55 deck）`:237`（case 7 cemetery → `BattleHud.GetUIAnchor`）`:290-305`（18/19 hand）`:307`（0x32 deck）；`BattleHud__GetUIAnchor.c`（枚举表 `(option, isPlayerOnly, transform, altTransform)`）；`BattleHud.cs:181`（`GetUIAnchor(PositionOption, bool isPlayer=false)`）；`AbilityLogic__PlayAnim.c:198-247`（`start/end/pointTowards` 三次 `GetPos` → `AddCardAnim`） | **已闭合（口径修正）**：`Trigger` 需要的是**目标侧+格号 + 侧别**，不是「手牌锚」 | 给 `EvtKind.Trigger` 加 `TargetPlayer/TargetSlot`（或直接带 `sp/ep/pt` 三个原始 option 让表现层解）；`sp=2` 用目标、`3/4/14` 只靠侧别 |
| **C1-C01·补** | 那到底哪些动画**真**要手牌/牌库锚？（数据普查） | 扫 `cardanim_clips.json` **1,067 行**：`sp/ep` 命中特殊锚的 **19 行**——<br>· **`19 enemyHand`（7 行）**：`AeldariRecall_target_UI`(ASH82) · `Explosion Hand Center UI 0,5s`(GOF93) · `Explosion Hand Center UI NoWait` · `Explosion_Debris_EnemyHand`(GSC76) · `Hand_Buff_Enemy_GSC`(GSC45) · `Recon Enemy Hand`(DA42) · `Recon Enemy Hand Purple`(GSC67)<br>· **`18 friendlyHand`（2 行）**：`Hand_Buff_Friendly_GSC`(GSC54) · `Hand_Buff_UM_AllCards`(1 张 id 为哈希的卡)<br>· **`50 friendlyDeck`（9 行）**：`DeckBuff*` 全族（UM62/UM85 · BL51 · GSC74 · DA40/DA49/DA56/SAU74/SAU75 · DA1/DA11/DA16/SW1 · SW19/SW44/SW47/SW61/SW8 · TAU47/TAU48/TAU51/TAU57）<br>· **`155 enemyHandWorld`（1 行）**：`Atk_BulletImpact_Ork Missiles Harpoon Hand`(GOF93，`sp=0 ep=155 pt=155`)<br>· **`61 friendlySpiritStoneMana`（1 行）**：`Eldar_TriggerSpiritstone_Mana`(ASH82)<br>· **`150 friendlyHandWorld`（0 行）**、**`cemetery 7`（0 行）**、`nextToCemetery 10`（0 行）⇒ **当前卡池的卡动画一条都没用墓地/手牌世界锚** | 数据：`数据/游戏数据/cardanim_clips.json`（键 = CardAnim 名，`sp/ep/pt`）＋ `数据/游戏数据/card_vfx_by_card.json`（卡→CardAnim 名）；本表由 `工具` 侧同一份数据扫出（可复跑） | **已闭合** | 这 19 行才需要「手牌/牌库锚」通道；T21 那批 410 行**不需要** |
| **C1-C01·补2** | `T21` 的 E/F 族（手牌陷阱 / 弃牌堆常驻）到底缺哪种落点 | 逐卡核了 7 张：**4 张的触发动画是 `sp=0 self`，而它们的「自己」在手里** ⇒ 需要「**这张卡自己的手牌视图/锚**」：`Jammed Communications`（`Card_Buff_Sabotage_self` sp=0；另有 `Jammed Communications Damage` sp=3/pt=3）· `Master Interromancer`（`Trait_GainQuest_self` sp=0）· `Cult Propaganda`（sp=0 + `CreateCard_GSC` sp=5）· `Poisoned Supplies`（sp=0 + `Toxic Vapors Damage quick` sp=2）。**另外 3 张不需要手牌锚**：`Lord of the Storm`（`CreateCard_Necrons` sp=5 boardCenter）· `Spirit of the Martyr`（sp=2 目标）· `Feeding Frenzy`（sp=2 目标） | `cards_raw.json` + `card_vfx_by_card.json`（id：Jammed=`68909cf0…` · Master Interromancer=`a84a5b49…` · Lord of the Storm=`30007162…` · SOR52 · Feeding Frenzy=`468db99b…` · Cult Propaganda=`7c666a04…` · Poisoned Supplies=`a7bb2335…`）；锚语义 `AbilityLogic__GetPos.c:183-200`（`case 0` self → `AbilityData+0x10` 卡的 `GetEffectAnchor`） | **已闭合** | 这 4 张要「self = 手里那张卡」的锚；表现层需要按 cardId 找手牌视图（我们的 `AnimAnchors.HandAnchorOf` 已有） |
| **C1-C02** | `T21②` `UM118` / `UM116`：`playChosenCardAbility(360)` 会不会**间接**调度 `followAfterResolution(410)` | **不会**。360 的分支只做两件事：`PayCardPlayedCostOath(...)` + `RawCardScript.OnTriggerAlt(card, 0, ...)`（**trigger 0**，即「打出这张卡」）。全库 `AddFollowAfterResolution` **只有一个调用点**（325 分支尾部）；410 的触发只有一条链：`BattleManager.ResolveFollowAfterResolution → CardScript.FollowUpResolutionPlayed → RawCardScript.OnTrigger(0x19a=410)`。⇒ 没有 320/325 的 410 能力**原版也不会播** | `AbilityLogic__PlayAbility.c:1994-2067`（`if (iVar8 != 0x168)` = 360 分支：`PayCardPlayedCostOath` + `OnTriggerAlt(...,0,...)`）；`:2454-2477`（唯一的 `Contains` 闸 + `AddFollowAfterResolution`）；`CardScript__FollowUpResolutionPlayed.c:5-8`；`BattleManager__ResolveFollowAfterResolution.c:380`；`dump.cs:20710`（`playChosenCardAbility = 360`）· `dump.cs:20704`（`followAfterResolution = 320`）· `dump.cs:20705`（`followResolutionIfPreviousMeetsCriteria = 325`） | **已闭合**（⇒ 我们对 `UM118`/`UM116`/`DA29`/`DA14`/`SW9` **不发** 410 是**正确的**） | 保持现状（不发）；把这 5 张标成「原版同样孤立」，别再当待办追 |
| **C1-C03** | `T21③` `ASH73` 的 410 **前置条件** | 325 的语义是**条件式排队**：<br>`targets = GetTargets(本 logic 的 targetCriteria)`；`refCard = pooledPossibleTargets[0]`（**上一个 ability 的目标**，由 `SetPreviousAbilityTargets` 在上一拍末尾存下）；<br>· `Contains(targets, refCard)` == **false** ⇒ 日志 **"Ref card not valid. Not following up."** → 只播本 logic 自己的动画（ASH73 这条 **没有** cardAnimations）→ **不排队**；<br>· == **true** ⇒ 日志 **"Ref card valid, following up after resolution"** → `AddFollowAfterResolution(410)`。<br>**ASH73 的判据**：325 的 `targetCriteria = allFiltered(10) + friendlyUnits(1) + enemyUnits(1) + includeHero/Minions(1) + **conditionsFilter = 220**`；`UnitConditions.dealtKillDmg = 220` 的**实际求值 = `CardScript.EnoughPendingDamageToDie(card)`**（「这张单位身上的待结算伤害够它死」）⇒ **闸门 = 目标是否会被这一发打死**（与卡面 "If the target dies" 一致）。410 那一侧 `targetCriteria = adjacentToTargetIfMeetsCriteria(110) + includeRemnants(1) + maxTargets=2` | `AbilityLogic__PlayAbility.c:2454-2477`；字符串字面量 `0x42b2a98` / `0x42b2b98`（`stringliteral.json` 现读）；`AbilityLogic__SetPreviousAbilityTargets.c`（调用点 `PlayAbility.c:2745`）；`AbilityLogic__GetTargets.c:850-891`（`previousAbilityTargets=140/150` 用 `pooledPossibleTargets`）；`FilterMethods__MeetsConditionCriteria.c:253-256`（`param_2 == 0xdc` → `EnoughPendingDamageToDie`）；`dump.cs:20748`（`dealtKillDmg = 220`）；`cards_raw.json` ASH73 `cardAbilities[0].abilityLogic[1]`（chosenAbility 325 · conditionsFilter 220）· `cardAbilities[1]`（trigger 410） | **已闭合** | 发 410 前加闸：**目标 HasPendingLethalDamage**（我们引擎已有 `conditionsFilter 220` 的求值，`MeetsUnitCondition` 覆盖 40/50/220/243/244）；现在是无条件发 ⇒ **会多播**。⚠️ 注意判定时刻 = 325 解析时（伤害已入 pending、单位还没真死） |
| **C1-C04** | `T21④` `TurnBeforeEnd(41)` 与 `TurnEnd(40)` 的**先后** | **41 严格早于 40**：`ResolveEndTurn` 先 `BroadcastTurnBeforeEnd`（:385），**同帧无等待**再 `BroadcastTurnEnd`（:433）。两次广播的**遍历顺序相同**：`manager+0x470`（场上单位，按列表序）→ **当前玩家手牌** → **对手手牌**；且 `BroadcastTurnBeforeEnd` 对**场上单位**要求 `IsInPlay`，对两侧手牌不过滤 | `BattleManager__ResolveEndTurn.c:385,433`；`BattleManagerSupport__BroadcastTurnBeforeEnd.c:20-58`；`BattleManagerSupport__BroadcastTurnEnd.c:20-58`（场上 + `IsInPlay` / 尸骸特例） | **已闭合** | 把 `ResolveAtTurn("turn_end")` 拆成**两趟**：先整趟 `TurnBeforeEnd`（单位→我方手牌→对方手牌），再整趟 `TurnEnd`（同序）；现在是同一批按「手牌→棋盘槽位序」跑 ⇒ 顺序与原版不同 |

---

## 二、`新账-58` 24 条 `Battle/Tips/*` 的**原版消费点**（逐条）

> **结论：24/24 全部有原版消费点**（不是「原版也没有」）。机制统一：消费点取词条 → `I2.Loc.LocalizationManager.GetTranslation/GetTermTranslation(key)` → `BattleTipController.NotifyCantDoAction(hud, text)`（内部：**若当前没有语音在播**先 `VoiceLinesController.DisplayLocalChatMessage(3,1)`，**然后一定** `ShowHeadsUpMessage`；`NotifyCantDoAction.c:5-14`），少数两处直接 `ShowHeadsUpMessage`。
> 词条键全表 = `stringliteral.json` 里 `Battle/Tips/` 开头那 **24 条**（地址 0x42894C0–0x428AAB0）。
> ⚠️ **口径**：**消费点（哪个文件哪一行、拿哪个 key 去 `Loc`）是逐条钉死的**；括号里的「触发条件」只按 key 名 + 已读到的片段写 —— 前 10 条攻击类都落在 `BattleManager__IsValidAttackTarget.c` 的同一棵嵌套条件树里（`:34-46` 是 reason→tip 的分派表，条件在 `:60-240`），**那棵树没有逐档读完**（已读到的：`:69` 近战攻击力≤0 · `:84` 远程≤0 · `:207-210` `frenzied(0x2a8)`+`CheckIfAttackKillsTarget`）。

| 编号 | 查的是什么 | 判据结论（具体值：原版消费点 + 触发条件） | 出处（文件:行号） | 状态 | 照着做时怎么落 |
|---|---|---|---|---|---|
| **C1-C05-01** | `Battle/Tips/AttackBlock` | `IsValidAttackTarget` 的攻击被挡分支（非上面几档的兜底档） | `BattleManager__IsValidAttackTarget.c:42`（引用）`:173`（发提示） | 已闭合 | 出牌/攻击被拒时按因分档，本档=攻击被挡 |
| **C1-C05-02** | `AttackBlockFlying` | 目标 `flying` 挡近战 | `BattleManager__IsValidAttackTarget.c:46` / `:316` | 已闭合 | 同上 |
| **C1-C05-03** | `AttackBlockStealth` | 目标潜行/不可选中 | `BattleManager__IsValidAttackTarget.c:37` / `:329` | 已闭合 | 同上 |
| **C1-C05-04** | `AttackGiantKiller` | `CheckIfAttackKillsTarget` 为假（巨人杀手类：打不死就不合法） | `BattleManager__IsValidAttackTarget.c:41` / `:304`（配合 `CheckIfAttackKillsTarget`） | 已闭合 | 同上（与 `W64·B-15` 同源） |
| **C1-C05-05** | `CantAttackCoward` | 攻方带 coward 类限制 | `BattleManager__IsValidAttackTarget.c:36` / `:280` | 已闭合 | 同上 |
| **C1-C05-06** | `CantAttackDestroyer` | 攻方是毁灭者（`frenzied` 0x2a8）且这一击打不死 | `BattleManager__IsValidAttackTarget.c:43` / `:255` | 已闭合 | 同上 |
| **C1-C05-07** | `CantAttackFlank` | 目标带 flank（侧翼） | `BattleManager__IsValidAttackTarget.c:34` / `:291` | 已闭合 | 同上 |
| **C1-C05-08** | `CantAttackTainted` | 目标被污染 | `BattleManager__IsValidAttackTarget.c:35` / `:198` | 已闭合 | 同上 |
| **C1-C05-09** | `CantAttackWithZeroMeleeAttack` | 近战攻击力 ≤ 0 却要近战 | `BattleManager__IsValidAttackTarget.c:38` / `:69` | 已闭合 | 同上 |
| **C1-C05-10** | `CantAttackWithZeroRangedAttack` | 远程攻击力 ≤ 0 却要远程 | `BattleManager__IsValidAttackTarget.c:44` / `:84` | 已闭合 | 同上 |
| **C1-C05-11** | `DamageFatigue` | 疲劳伤害**我方**那一条 | `BattleManager._ResolveFatigue_d__587__MoveNext.c:35` / `:112`（`param_1+0x20 != 0` 支） | 已闭合 | 抽牌库空→疲劳时给提示 |
| **C1-C05-12** | `DamageFatigueEnemy` | 疲劳伤害**敌方**那一条 | 同上 `:34` / `:110`（`param_1+0x20 == 0` 支） | 已闭合 | 同上（敌方疲劳） |
| **C1-C05-13** | `DragToTarget` | 手牌拖出去松手落空 / 瞄准落空 | `BattleManager__Update.c:86` / `:669` / `:1209` | 已闭合（**我们已接**） | 无需再做 |
| **C1-C05-14** | `GoFirst` | 换牌阶段「你先手」 | `MulliganManager__ActivateMulligan.c:23` / `:35` | 已闭合（**我们已接**） | 无需再做（⚠️ 与 `Battle/Mulligan/secondTurn` 是两条不同键） |
| **C1-C05-15** | `HandFull` | 抽牌时手牌已满 ⇒ 牌进墓地 + 提示 | `PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:27`/`:134`；`PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:18`/`:59`；`_AddCardNotDrawnToHandAtIndex_d__40__MoveNext.c:18`/`:60`（后两处走 `ShowHeadsUpMessage`） | 已闭合 | 我们引擎有手牌上限（`DeckRules.*HandLimit`），接提示即可 |
| **C1-C05-16** | `InvalidTarget` | 主动技能/选目标落空 | `BattleManager._ResolvePlayActiveAbility_d__479__MoveNext.c:49`/`:399`；`BattleManager__FinishedChoosingAbilityTargetByClick.c:12`/`:25` | 已闭合 | 两个消费点都要 |
| **C1-C05-17** | `MuteEnemyChat` | 屏蔽对手聊天时点对方聊天 | `VoiceLinesController__DisplayWarlordChatMessage.c:32`/`:101` | 已闭合 | HUD 那颗静音钮是最独立的落点 |
| **C1-C05-18** | `NoTargetAvailable` | 可出牌但没有合法目标（目标化法术/技能） | `BattleManager__CanPlayCard.c:21`/`:150`/`:153`；`BattleManager__TryUsingActiveAbility.c:12`/`:36`；`...OfCard.c:13`/`:37` | 已闭合 | 我们现有一处 `Phrase("NO LEGAL TARGET")` 语义正对，但原版点在「出牌被拒」 |
| **C1-C05-19** | `NotEnoughMana` | 法力不够 | `BattleManager__CanPlayCard.c:24`/`:93`/`:96`（`!HasEnoughMana`）；`CanUseActiveAbility.c:31`/`:78`；`ReadyToUseActiveAbility.c:24`/`:69` | 已闭合 | 对 `RuleCodes.ErrCost`，可 1:1 先做 |
| **C1-C05-20** | `NotEnoughRoom` | 部队卡但场上没有空位 | `BattleManager__CanPlayCard.c:22`/`:115`/`:118`（`!MinionManager.IsAvailableSlot(1)`） | 已闭合 | 我们的 `ErrSlot` 语义更宽，要先拆码 |
| **C1-C05-21** | `NotYourTurn` | 不是你的回合 | `BattleManager__CanPlayCard.c:23`/`:162`/`:165`；`BattleManager__CanUseWaystone.c:19`/`:39` | 已闭合 | 对 `RuleCodes.ErrNotTurn`，可 1:1 |
| **C1-C05-22** | `PendingBerzerk` | 结束回合时还有狂战士强制攻击没打 | `BattleManager__EndTurnClick.c:33`/`:89`（找可用攻击目标 → `DisplayTriggerAnim(0x168)` + 提示 + 指向那张卡） | 已闭合 | 我们有狂战强制攻击机制，接这一档 |
| **C1-C05-23** | `PleaseWait` | 还有阻塞动作没结算完 | `BattleManager__CanPlayCard.c:20`/`:39`（`manager+0x350` 队列 > 0 ⇒ `ShowHeadsUpMessage`）；`BattleManager__WaitingForBlockingAction.c:16`/`:130` | 已闭合 | 我们现在只有 `PlayerInputFrozen` 一个布尔 ⇒ 要分两档 |
| **C1-C05-24** | `UnitNotReady` | 点了一个已行动/不能行动的单位 | `BattleManager__CanUseActiveAbility.c:29`/`:125`（判据字段 `CardScript+0x230` = `canAct`） | 已闭合（**我们已接**） | 无需再做 |

---

## 三、本文件的判定小结（一句话）

- **T21①**：原行「缺手牌/弃牌堆锚」的前提只对 **4 张手牌陷阱** 成立；410 那批全在棋盘上（`sp=2/3/4/14`），真正缺的是「目标侧+格号」。
- **T21②**：360 **不会**间接调度 410 ⇒ `UM118/UM116/DA29/DA14/SW9` 不发 410 是**对的**。
- **T21③**：325 是**条件**队列，ASH73 的闸门 = **目标待结算伤害够它死**（`conditionsFilter 220`）；我们现在无条件发 ⇒ **会多播**。
- **T21④**：`TurnBeforeEnd` 严格早于 `TurnEnd`，同帧、遍历序 = 单位 → 当前玩家手牌 → 对手手牌。
- **24 条 Tips**：**全部**都能在原版找到消费点（逐条 file:line 已列），不存在「原版也没有」的条目。
